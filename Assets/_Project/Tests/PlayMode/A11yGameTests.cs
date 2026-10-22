#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AF.Economy;
using AF.GameFlow;
using AF.Localization;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// B9 en la escena <c>Game</c> real (parche <c>B9-ui.diff</c>; 02 §3.1 C9, §4 y §12; 07 §3, regla 5, y §7.8; 08 §9 y §14.2):
    /// <list type="bullet">
    /// <item>C9: mantener 400 ms sobre una cuia enseña los nombres de sus dosis y al soltar no selecciona; un toque corto sigue
    /// siendo un toque; moverse 24 dp antes lo cancela (es el arrastre, C10).</item>
    /// <item>Reducir movimiento (<c>settings.reduce_motion</c>): los vertidos a la mitad de tiempo (<c>anim_reduce_motion_factor</c>),
    /// sin sacudida en el rechazo; en <c>auto</c>, el teléfono con «Quitar animaciones» lo enciende [P].</item>
    /// <item>Alto contraste (<c>settings.high_contrast</c>): el patrón al 35 %, al momento.</item>
    /// <item>Ajustes: las dos filas guardan el perfil y se aplican.</item>
    /// <item>La escala de fuente del sistema llega a la escala de texto, con el tope del 130 %.</item>
    /// <item>TalkBack: la selección se anuncia.</item>
    /// </list>
    /// </summary>
    public sealed class A11yGameTests
    {
        [TearDown]
        public void TearDown()
        {
            GameBootstrap.A11yHostOverride = null;
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator C9_Holding400ms_ShowsTheNames_AndReleasingDoesNotSelect()
        {
            GameBootstrap boot = null!;
            yield return LoadLevel1(SettingsScreenTests.ProfileAt(1), b => boot = b);
            B9LocPlay.Use();
            CuiaView view = boot.View!.Views[0];
            Press(view, Center(view));
            PlayModeHelpers.Step(boot, 384);
            Assert.That(boot.View.HoldNamesVisible, Is.False, "antes de 400 ms, nada");
            PlayModeHelpers.Step(boot, 32);
            Assert.That(boot.View.HoldNamesVisible, Is.True, "02 §3.1, C9: ≥ 400 ms");
            Assert.That(boot.View.HoldNamesText, Is.EqualTo(CuiaSpeech.Names(boot.Flow!.Session.Current, 0)));
            Assert.That(boot.View.HoldNamesText, Is.Not.Empty);
            Release(view, Center(view));
            Assert.That(boot.Flow.Selected, Is.EqualTo(TapController.NoSelection), "al soltar no selecciona");
            Assert.That(boot.Flow.Session.AppliedPours, Is.Zero);
            Assert.That(boot.View.HoldNamesVisible, Is.False, "al soltar, los nombres se van");
            PlayModeHelpers.Step(boot, 200);
            Assert.That(view.IsLifted, Is.False, "la cuia vuelve a su sitio");
        }

        [UnityTest]
        public IEnumerator C9_HoldingAnEmptyCuia_IsStillATap()
        {
            // Una cuia vacía no tiene nombres que enseñar: mantenerla no es C9 y soltar es el toque de siempre (aquí, el destino).
            GameBootstrap boot = null!;
            yield return LoadLevel1(SettingsScreenTests.ProfileAt(1), b => boot = b);
            int empty = FirstEmpty(boot);
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            CuiaView view = boot.View!.Views[empty];
            Press(view, Center(view));
            PlayModeHelpers.Step(boot, 450);
            Assert.That(boot.View.HoldNamesVisible, Is.False);
            Release(view, Center(view));
            Assert.That(boot.Flow!.Session.AppliedPours, Is.EqualTo(1), "el vertido 0 → vacía se hace");
        }

        [UnityTest]
        public IEnumerator C9_APauseOverTheNames_HidesThem_AndTheReleaseIsNotATap()
        {
            GameBootstrap boot = null!;
            yield return LoadLevel1(SettingsScreenTests.ProfileAt(1), b => boot = b);
            CuiaView view = boot.View!.Views[0];
            Press(view, Center(view));
            PlayModeHelpers.Step(boot, 416);
            Assert.That(boot.View.HoldNamesVisible, Is.True);
            boot.View.PauseButton.onClick.Invoke(); // otro dedo abre la pausa (no cambia la fase)
            PlayModeHelpers.Step(boot, (int)GameScreen.HoldCoverCheckMs + 32);
            Assert.That(boot.View.HoldNamesVisible, Is.False, "la pausa encima quita los nombres");
            Release(view, Center(view));
            Assert.That(boot.Flow!.Selected, Is.EqualTo(TapController.NoSelection), "al soltar no selecciona (ni debajo de la pausa)");
            Assert.That(boot.Flow.Session.AppliedPours, Is.Zero);
        }

        [UnityTest]
        public IEnumerator C9_AVeilInAnotherCanvasOverTheNames_HidesThem()
        {
            GameBootstrap boot = null!;
            yield return LoadLevel1(SettingsScreenTests.ProfileAt(1), b => boot = b);
            CuiaView view = boot.View!.Views[0];
            Press(view, Center(view));
            PlayModeHelpers.Step(boot, 416);
            Assert.That(boot.View.HoldNamesVisible, Is.True);

            // Un lienzo nuevo con velo encima (como el Mercado o un anuncio, que se crean al primer uso).
            var veil = new GameObject("A11yVeilCanvas", typeof(RectTransform), typeof(Canvas));
            veil.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            veil.GetComponent<Canvas>().sortingOrder = 100;
            UiBuilder.Stretch(UiBuilder.Image("Veil", veil.transform, null, new Color(0f, 0f, 0f, 0.5f), raycast: true).rectTransform);
            try
            {
                Canvas.ForceUpdateCanvases();
                PlayModeHelpers.Step(boot, (int)GameScreen.HoldCoverCheckMs + 32);
                Assert.That(boot.View.HoldNamesVisible, Is.False, "el velo de encima quita los nombres");
                Release(view, Center(view));
                Assert.That(boot.Flow!.Selected, Is.EqualTo(TapController.NoSelection), "y soltar sigue sin seleccionar");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(veil);
            }
        }

        [UnityTest]
        public IEnumerator C9_APauseBeforeTheNames_ShowsNone_AndTheHoldIsStillNotATap()
        {
            GameBootstrap boot = null!;
            yield return LoadLevel1(SettingsScreenTests.ProfileAt(1), b => boot = b);
            CuiaView view = boot.View!.Views[0];
            Press(view, Center(view));
            PlayModeHelpers.Step(boot, 160);
            boot.View.PauseButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 400);
            Assert.That(boot.View.HoldNamesVisible, Is.False, "con la pausa encima no salen los nombres");
            Release(view, Center(view));
            Assert.That(boot.Flow!.Selected, Is.EqualTo(TapController.NoSelection), "mantener 400 ms es C9, nunca un toque bajo el velo");
            Assert.That(boot.Flow.Session.AppliedPours, Is.Zero);
        }

        [UnityTest]
        public IEnumerator C9_AShortPressIsStillATap_AndMovingCancelsTheHold()
        {
            GameBootstrap boot = null!;
            yield return LoadLevel1(SettingsScreenTests.ProfileAt(1), b => boot = b);
            CuiaView view = boot.View!.Views[0];
            Press(view, Center(view));
            PlayModeHelpers.Step(boot, 160);
            Release(view, Center(view));
            Assert.That(boot.Flow!.Selected, Is.EqualTo(0), "C1: un toque corto selecciona");
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Deselected));
            PlayModeHelpers.Step(boot, 200);

            // Moverse 24 dp antes de los 400 ms: ya es el arrastre (C10), no un mantener pulsado.
            float px = A11yRules.HoldSlopDp * boot.View.Canvas.scaleFactor;
            Press(view, Center(view));
            Drag(view, Center(view) + new Vector2(0f, px + 2f));
            PlayModeHelpers.Step(boot, 600);
            Assert.That(boot.View.HoldNamesVisible, Is.False, "02 §3.1: «sin mover el dedo»");
            Release(view, Center(view));
        }

        [UnityTest]
        public IEnumerator ReduceMotion_HalvesThePour_AndRemovesTheShake()
        {
            GameBootstrap boot = null!;
            yield return LoadLevel1(SettingsScreenTests.ProfileAt(1), b => boot = b);
            Assert.That(boot.ReduceMotionNow, Is.False, "08 §9: ui_reduce_motion_default = false");
            int empty0 = FirstEmpty(boot);
            Assert.That(boot.TapCuia(empty0).Kind, Is.EqualTo(TapKind.Rejected));
            Assert.That(boot.View!.Views[empty0].Shaking, Is.True, "sin el ajuste, el rechazo se sacude (D-012)");
            PlayModeHelpers.Step(boot, 300);
            int normal = PourMs(boot);

            Profile reduced = SettingsScreenTests.ProfileAt(1);
            reduced.Settings.ReduceMotion = ReduceMotionSetting.On;
            yield return LoadLevel1(reduced, b => boot = b);
            Assert.That(boot.ReduceMotionNow, Is.True);
            Assert.That(boot.Flow!.ReduceMotion, Is.True, "LevelFlow.ReduceMotion: intro en fundido, sin partículas…");

            // Rechazo (C2: una cuia vacía no puede ser origen): sin sacudida (02 §12).
            int empty = FirstEmpty(boot);
            Assert.That(boot.TapCuia(empty).Kind, Is.EqualTo(TapKind.Rejected));
            Assert.That(boot.View!.Views[empty].Shaking, Is.False, "con Reducir movimiento, sin sacudidas");

            int fast = PourMs(boot);
            Assert.That(fast, Is.LessThan(normal * 0.75f), "02 §4: × anim_reduce_motion_factor (0,5): " + fast + " frente a " + normal + " ms");

            // Los pasos de mecánica, con el mismo factor y una sola vez (la gota y el mordisco parten de la dosis sin factores).
            BoardAnimator animator = boot.View.Animator;
            Assert.That(animator.StepSpeedBase, Is.EqualTo(0.5f).Within(1e-6f), "× anim_reduce_motion_factor, no un 0,5 fijo aparte");
            Assert.That(animator.DoseNominalMs, Is.EqualTo(GameConfig.AnimPourDoseMs).Within(0.5f), "sin el factor: no se aplica dos veces");
        }

        [UnityTest]
        public IEnumerator ReduceMotion_ReachesTheDailyBoardBeforeItIsBuilt()
        {
            // El Reto del río de verdad (como DailyChallengeTests): pool de prueba, almacén en memoria y reloj fijo; se abre la
            // tarjeta y se mira el tablero ya construido por StartDaily.
            var bogota = TimeSpan.FromHours(-5);
            var clock = new ManualClock(new DateTime(2026, 10, 19, 15, 0, 0, DateTimeKind.Utc), bogota);
            Profile reduced = Wallet.NewProfile(clock);
            reduced.Progress.MaxLevel = 25;
            reduced.Progress.NextLevel = 26;
            reduced.Settings.ReduceMotion = ReduceMotionSetting.On;
            reduced.Settings.HighContrast = true;
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = new AF.LiveOps.InMemoryDailyChallengeStore();
            try
            {
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(reduced));
                GameBootstrap boot = GameBootstrap.Current!;
                PlayModeHelpers.TakeClock(boot);
                Assert.That(boot.SetUpDaily(), Is.True);
                boot.DailyRunner!.enabled = false;
                Assert.That(boot.PressDailyCard(), Is.True, "se abre el Reto");
                DailyChallengeScreen daily = boot.DailyScreen!;
                Assert.That(daily.Flow, Is.Not.Null);
                Assert.That(daily.Flow!.ReduceMotion, Is.True, "el flujo del Reto, con Reducir movimiento desde el principio");
                Assert.That(daily.View!.Animator.ReduceMotion, Is.True, "el tablero se construyó ya con Reducir movimiento");
                Assert.That(daily.View.Animator.StepSpeedBase, Is.EqualTo(0.5f).Within(1e-6f));
                Assert.That(daily.View.HighContrast, Is.True);
                Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(0.35f).Within(1e-6f), "Alto contraste en el tablero del Reto");
                Assert.That(daily.View.A11y, Is.SameAs(boot.A11y));
            }
            finally
            {
                GameBootstrap.ResetDailyOverrides();
            }
        }

        [UnityTest]
        public IEnumerator ReduceMotion_NoRippleNoConfetti_AndTheMapToo()
        {
            // Sin el ajuste: la onda del agua al caer la dosis y el confeti de la celebración (control).
            GameBootstrap boot = null!;
            yield return LoadLevel1(SettingsScreenTests.ProfileAt(1), b => boot = b);
            Assert.That(PourRipples(boot), Is.True, "sin Reducir movimiento, la dosis agita la superficie (08 §5.2)");
            boot.View!.OnCelebrationStarted();
            PlayModeHelpers.Step(boot, 600);
            Assert.That(boot.View.Burst.Running, Is.True, "sin Reducir movimiento, confeti (07 §5.4)");

            // Con el ajuste (02 §12, fila «Movimiento»): ni onda ni confeti; el Mapa del río también lo recibe.
            Profile reduced = SettingsScreenTests.ProfileAt(1);
            reduced.Settings.ReduceMotion = ReduceMotionSetting.On;
            yield return LoadLevel1(reduced, b => boot = b);
            Assert.That(PourRipples(boot), Is.False, "la onda del shader se apaga");
            boot.View!.OnCelebrationStarted();
            PlayModeHelpers.Step(boot, 600);
            Assert.That(boot.View.Burst.Running, Is.False, "sin confeti");
            if (boot.RiverMapView != null)
            {
                Assert.That(boot.RiverMapView.ProgressReduceMotion, Is.True, "el Mapa del río, sin movimiento");
            }
        }

        [UnityTest]
        public IEnumerator ReduceMotion_Auto_FollowsThePhoneWithoutAnimations_ButTheChoiceWins()
        {
            LaunchOptions.SystemScaleOverride = new FakeSystemScale(1f, 0f);
            GameBootstrap boot = null!;
            yield return LoadLevel1(SettingsScreenTests.ProfileAt(1), b => boot = b);
            Assert.That(boot.ReduceMotionNow, Is.True, "[P] Q-B9-3: «Quitar animaciones» del teléfono con el ajuste en auto");
            Assert.That(boot.ChooseA11y(SettingsScreen.ReduceMotionId, false), Is.True);
            Assert.That(boot.ReduceMotionNow, Is.False, "elegido en Ajustes, manda la elección");
            Assert.That(boot.Flow!.ReduceMotion, Is.False);
            Assert.That(boot.Profile!.Settings.ReduceMotion, Is.EqualTo(ReduceMotionSetting.Off));
        }

        [UnityTest]
        public IEnumerator HighContrast_PutsThePatternAt35Percent_AtOnce()
        {
            GameBootstrap boot = null!;
            yield return LoadLevel1(SettingsScreenTests.ProfileAt(1), b => boot = b);
            Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(Palette.PatternOpacitySubtle).Within(1e-6f), "hasta el 40, 15 %");
            Assert.That(boot.ChooseA11y(SettingsScreen.HighContrastId, true), Is.True);
            Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(0.35f).Within(1e-6f), "07 §3, regla 5: Alto contraste, 35 %");
            Assert.That(boot.Profile!.Settings.HighContrast, Is.True);
            Assert.That(boot.ChooseA11y(SettingsScreen.HighContrastId, false), Is.True);
            Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(Palette.PatternOpacitySubtle).Within(1e-6f), "y vuelve al del nivel");
        }

        [UnityTest]
        public IEnumerator TalkBack_IconOnlyButtons_HaveTheirFunctionalLabel()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            B9LocPlay.Use();
            boot.A11y!.Refresh();
            A11yItem? gear = host.Find(UiText.Settings);
            Assert.That(gear, Is.Not.Null, "02 §12: el engranaje del inicio dice «Ajustes»");
            Assert.That(gear!.Invoke!(), Is.True);
            Assert.That(boot.SettingsView!.Visible, Is.True, "activarlo abre Ajustes");
            boot.A11y.Refresh();
            A11yItem? close = host.Find(A11yText.Close);
            Assert.That(close, Is.Not.Null, "la X de Ajustes dice «Cerrar»");
            Assert.That(host.Find(UiText.Sound)!.Selected, Is.True, "las filas dicen si están marcadas (Sonido, activado)");
            Assert.That(close!.Invoke!(), Is.True);
            Assert.That(boot.SettingsView.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Settings_TheTwoRows_SaveAndApply()
        {
            var store = new MemoryProfileStore(SettingsScreenTests.ProfileAt(4));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.A11yMarked(SettingsScreen.ReduceMotionId), Is.False);
            Assert.That(settings.A11yMarked(SettingsScreen.HighContrastId), Is.False);
            settings.A11yButton(SettingsScreen.ReduceMotionId).onClick.Invoke();
            settings.A11yButton(SettingsScreen.HighContrastId).onClick.Invoke();
            Assert.That(settings.A11yMarked(SettingsScreen.ReduceMotionId), Is.True);
            Assert.That(settings.A11yMarked(SettingsScreen.HighContrastId), Is.True);
            Assert.That(boot.Profile!.Settings.ReduceMotion, Is.EqualTo(ReduceMotionSetting.On));
            Assert.That(boot.Profile.Settings.HighContrast, Is.True);
            Assert.That(boot.Flow!.ReduceMotion, Is.True, "se aplica al momento");
            Assert.That(store.SaveCount, Is.GreaterThan(0), "se guarda en el acto");
            foreach (TMPro.TextMeshProUGUI text in settings.Texts())
            {
                Assert.That(text.fontSizeMin, Is.GreaterThanOrEqualTo(12f), text.name + ": 02 §12, ≥ 12 sp");
            }
        }

        [UnityTest]
        public IEnumerator SystemFontScale_ReachesTheTextScale_UpTo130Percent()
        {
            LaunchOptions.SystemScaleOverride = new FakeSystemScale(1.15f);
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            Assert.That(UiBuilder.TextScale, Is.EqualTo(1.15f).Within(1e-6f), "02 §12: la escala de fuente del sistema");
            LaunchOptions.Reset();
            LaunchOptions.SystemScaleOverride = new FakeSystemScale(2f);
            Assert.That(UiBuilder.TextScale, Is.EqualTo(UiBuilder.MaxTextScale).Within(1e-6f), "por encima del 130 %, ese tope");
            LaunchOptions.Reset();
            LaunchOptions.SystemScaleOverride = new FakeSystemScale(2f);
            LaunchOptions.FontScaleOverride = 1f;
            Assert.That(UiBuilder.TextScale, Is.EqualTo(1f), "el gancho de los tests manda");
        }

        [UnityTest]
        public IEnumerator TalkBack_TheSelectionIsAnnounced()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            GameBootstrap boot = null!;
            yield return LoadLevel1(SettingsScreenTests.ProfileAt(1), b => boot = b);
            B9LocPlay.Use();
            Assert.That(boot.A11y, Is.Not.Null);
            Assert.That(boot.View!.A11y, Is.SameAs(boot.A11y));
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            string fruit = CuiaSpeech.TopName(boot.Flow!.Session.Current, 0);
            Assert.That(host.Announcements, Has.Member(fruit + " seleccionado: elige destino"), "02 §12");
            boot.A11y!.Refresh();
            Assert.That(host.Items[0].Selected, Is.True);
            A11yItem cuia = host.Items[0];

            // Con la pausa abierta (un velo en el mismo lienzo), el tablero queda debajo: ni se publica ni se toca a través de ella.
            boot.View.PauseButton.onClick.Invoke();
            boot.A11y.Refresh();
            foreach (A11yItem item in host.Items)
            {
                Assert.That(item.Id, Does.Not.StartWith("cuia_"), "la pausa tapa el tablero");
            }

            Assert.That(cuia.Invoke!(), Is.False, "un nodo viejo de una cuia no toca a través de la pausa");
        }

        private static IEnumerator LoadLevel1(Profile profile, Action<GameBootstrap> done)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 8000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            done(boot);
        }

        // Un vertido legal del nivel 1, del toque al reposo (ms).
        private static int PourMs(GameBootstrap boot)
        {
            int empty = FirstEmpty(boot);
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(empty).Kind, Is.EqualTo(TapKind.Poured));
            return PlayModeHelpers.StepUntilIdle(boot, 5000);
        }

        // Un vertido legal del nivel 1 hasta el reposo: si en algún fotograma el destino tuvo la onda en marcha.
        private static bool PourRipples(GameBootstrap boot)
        {
            int empty = FirstEmpty(boot);
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(empty).Kind, Is.EqualTo(TapKind.Poured));
            CuiaView target = boot.View!.Views[empty];
            bool rippled = false;
            for (int t = 0; t < 4000 && !boot.View.IsViewIdle; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                rippled |= target.Rippling;
            }

            return rippled;
        }

        private static int FirstEmpty(GameBootstrap boot)
        {
            AF.Core.Board.BoardState state = boot.Flow!.Session.Current;
            for (int i = 0; i < state.Count; i++)
            {
                if (state[i].IsEmpty)
                {
                    return i;
                }
            }

            Assert.Fail("el nivel 1 tiene una cuia vacía");
            return -1;
        }

        private static void Press(CuiaView view, Vector2 at)
        {
            ExecuteEvents.Execute(view.gameObject, new PointerEventData(EventSystem.current) { position = at, pointerId = 0 }, ExecuteEvents.pointerDownHandler);
        }

        private static void Drag(CuiaView view, Vector2 at)
        {
            ExecuteEvents.Execute(view.gameObject, new PointerEventData(EventSystem.current) { position = at, pointerId = 0 }, ExecuteEvents.dragHandler);
        }

        private static void Release(CuiaView view, Vector2 at)
        {
            ExecuteEvents.Execute(view.gameObject, new PointerEventData(EventSystem.current) { position = at, pointerId = 0 }, ExecuteEvents.pointerUpHandler);
        }

        private static Vector2 Center(CuiaView view)
        {
            RectTransform body = view.Body;
            return RectTransformUtility.WorldToScreenPoint(null, body.TransformPoint(body.rect.center));
        }
    }
}
