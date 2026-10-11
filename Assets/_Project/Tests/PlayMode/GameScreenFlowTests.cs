#nullable enable
using System.Collections;
using System.Reflection;
using AF.Ads;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
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
    /// Pantalla de nivel tras las olas 3 y 4 del paso 1 (limpieza L2a), con el catálogo real a 360 × 780: la pausa ya está bajo
    /// el velo de la tarjeta previa y no aparece de golpe al cerrarla; el touch-down no eleva ninguna cuia en el modo de
    /// selección de la Luciérnaga (04 §2.5); al volver de segundo plano la luciérnaga en vuelo salta al modelo (02 §14); la
    /// ranura Revelar va gris y sin precio fuera de PLAYING, como la Cuia extra; la cuia elevada pasa delante del HUD y vuelve
    /// al tablero sin saltar; las repisas se deslizan con su fila durante el relayout de «+1 cuia» (02 §4.1); y (L3) la
    /// luciérnaga genera sus sprites al cargar un nivel turbio, no en el toque del primer revelado, cuando las piezas de la
    /// cuia turbia ya están registradas en su Canvas.
    /// </summary>
    public sealed class GameScreenFlowTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator LevelCard_PauseAlreadyUnderTheVeil()
        {
            // 43 (estreno de la Hoja): tarjeta previa de 1 toque. La pausa y la bandeja (con «+1 cuia» desde el 8) ya se ven bajo el
            // velo, que se lleva todos los toques; al cerrar la tarjeta no aparece nada de golpe.
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(43, new MemoryProgressStore(43, 42));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 300);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(view.LevelCard.Visible, Is.True);
            Assert.That(view.PauseButton.gameObject.activeInHierarchy, Is.True, "la pausa ya está, bajo el velo");
            Assert.That(view.ExtraCuiaButton.gameObject.activeInHierarchy, Is.True, "«+1 cuia» también");
            Transform pauseLayer = view.PauseButton.transform.parent;
            Assert.That(view.LevelCard.Root.parent, Is.SameAs(pauseLayer.parent));
            Assert.That(view.LevelCard.Root.GetSiblingIndex(), Is.GreaterThan(pauseLayer.GetSiblingIndex()), "el velo de la tarjeta, por encima");

            // Inerte bajo el velo: sin poder salir ni reiniciar, la pausa no se abre.
            Assert.That(flow.CanQuit || flow.CanRestart, Is.False);
            view.PauseButton.onClick.Invoke();
            Assert.That(view.Pause.Visible, Is.False);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));

            // 1 toque cierra la tarjeta: la pausa sigue donde estaba.
            var tap = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(view.LevelCard.Dim.gameObject, tap, ExecuteEvents.pointerClickHandler);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.PauseButton.gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator RevealSelecting_TouchDownLiftsNothing()
        {
            // 100: cuia 1 = [6], que puede ser origen. En el modo de selección el toque elige la cuia que se revela: el
            // touch-down no la eleva ni un fotograma (la elevación de vista previa de 02 §3.1 es para elegir el origen).
            yield return LoadPlaying(100, FunnelSetup.Profile(0));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            const int plain = 1;
            Assert.That(PourRules.CanBeSource(flow.Session.Current, plain), Is.True);
            Assert.That(flow.Session.Current[plain].HiddenMask, Is.EqualTo(0));
            Assert.That(boot.PressReveal(), Is.True);
            Assert.That(flow.RevealSelecting, Is.True);

            CuiaView cuia = view.Views[plain];
            Vector2 inside = ScreenCenter(cuia);
            ExecuteEvents.Execute(cuia.gameObject, new PointerEventData(EventSystem.current) { position = inside }, ExecuteEvents.pointerDownHandler);
            Assert.That(cuia.IsLifted, Is.False, "04 §2.5: nada sube en el modo de selección");
            PlayModeHelpers.Step(boot, 3 * PlayModeHelpers.FrameMs);
            Assert.That(cuia.IsLifted, Is.False);
            Assert.That(cuia.BodyOffset.y, Is.EqualTo(0f).Within(0.001f), "el cuerpo no se ha movido de su repisa");

            // Al soltar dentro: una cuia sin ocultas cancela la selección sin seleccionar nada (T10).
            ExecuteEvents.Execute(cuia.gameObject, new PointerEventData(EventSystem.current) { position = inside }, ExecuteEvents.pointerUpHandler);
            Assert.That(flow.RevealSelecting, Is.False);
            Assert.That(flow.Selected, Is.EqualTo(TapController.NoSelection));
            Assert.That(cuia.IsLifted, Is.False);

            // Fuera del modo de selección, el touch-down sí la eleva (02 §3.1); soltar fuera cancela.
            ExecuteEvents.Execute(cuia.gameObject, new PointerEventData(EventSystem.current) { position = inside }, ExecuteEvents.pointerDownHandler);
            Assert.That(cuia.IsLifted, Is.True);
            ExecuteEvents.Execute(cuia.gameObject, new PointerEventData(EventSystem.current) { position = new Vector2(-50f, -50f) }, ExecuteEvents.pointerUpHandler);
            Assert.That(cuia.IsLifted, Is.False);
        }

        [UnityTest]
        public IEnumerator BackFromBackground_FireflySnapsToTheModel()
        {
            // 100: la Luciérnaga regalada revela la cuia 0 = [10*, 8*, 6]. Con la luciérnaga en vuelo, volver de segundo plano
            // (OnApplicationPause(false)) salta al modelo sin animar (02 §14): la cuia queda revelada y la cola lo sabe.
            yield return LoadPlaying(100, FunnelSetup.Profile(0));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            const int murky = 0;
            Assert.That(boot.PressReveal(), Is.True);
            Assert.That(boot.TapCuia(murky).Kind, Is.EqualTo(TapKind.Ignored), "el toque elige la cuia");
            Assert.That(boot.Flow!.Session.Current[murky].HiddenMask, Is.EqualTo(0), "el modelo ya está revelado");
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.RevealAnimating, Is.True);
            Assert.That(view.RevealFirefly.Flying, Is.True, "la luciérnaga vuela");
            Assert.That(view.Views[murky].ShownCuia.HiddenMask, Is.Not.EqualTo(0), "mientras vuela, la cuia sigue turbia");

            ApplicationPause(boot, false);
            Assert.That(view.RevealAnimating, Is.False, "el revelado termina en el acto");
            Assert.That(view.RevealFirefly.Flying, Is.False);
            Assert.That(view.Views[murky].ShownCuia.HiddenMask, Is.EqualTo(0), "la cuia, ya revelada");
            Assert.That(view.Animator.ShownOf(murky).HiddenMask, Is.EqualTo(0), "y la cola lo sabe");
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Firefly_SpritesAndPartsReadyWhenAMurkyLevelLoads()
        {
            // Revisión de L2b: el cuerpo y las alas de la luciérnaga no se generan en el fotograma del toque del primer revelado,
            // sino al cargar un nivel de Aguas turbias con ocultas (RevealFireflyFx.Prewarm), como los discos y los iconos de
            // MurkyMarks.Prewarm; el primer vuelo solo pone esos sprites en sus imágenes. Un nivel sin ocultas no los pide.
            yield return LoadPlaying(7, FunnelSetup.Profile(0));
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.Tray.RevealShown, Is.False, "nivel 7: sin Aguas turbias");
            Assert.That(view.RevealFirefly.SpritesReady, Is.False, "sin ocultas no se genera nada");

            yield return LoadPlaying(100, FunnelSetup.Profile(0));
            GameBootstrap boot = GameBootstrap.Current!;
            view = boot.View!;
            const int murky = 0;
            Assert.That(view.Tray.RevealShown, Is.True, "nivel turbio con ocultas");
            Assert.That(view.RevealFirefly.SpritesReady, Is.True, "los sprites, generados al cargar el nivel");
            Assert.That(view.RevealFirefly.BodySprite, Is.Null, "aún sin poner en sus imágenes: lo hace el primer vuelo");

            // Los discos y los iconos de fruta de la cuia turbia también están ya creados y registrados en su Canvas
            // (MurkyMarks.Prewarm, al cargar el nivel): mostrarlos a mitad del revelado no hace crecer el registro de UGUI.
            var marks = view.Views[murky].Murky;
            Assert.That(marks.FireflyPartsReady, Is.True, "las piezas de la Luciérnaga, preparadas al cargar");
            for (int k = 0; k < view.Views[murky].Capacity; k++)
            {
                Assert.That(GraphicPool.IsRegistered(marks.FruitDisc(k)), Is.True, "disco " + k + ", registrado en su Canvas");
                Assert.That(GraphicPool.IsRegistered(marks.FruitImage(k)), Is.True, "icono " + k + ", registrado en su Canvas");
            }

            Assert.That(boot.PressReveal(), Is.True);
            Assert.That(boot.TapCuia(murky).Kind, Is.EqualTo(TapKind.Ignored), "el toque elige la cuia");
            Assert.That(view.RevealFirefly.Flying, Is.True, "sale del botón");
            Assert.That(view.RevealFirefly.BodySprite!.name, Is.EqualTo("firefly_body"), "el primer vuelo pone el cuerpo ya generado");
            Assert.That(view.RevealFirefly.WingsSprite!.name, Is.EqualTo("firefly_wings"));
        }

        [UnityTest]
        public IEnumerator RevealSlot_GreyWithoutPriceOutsidePlaying()
        {
            // 106 con 50 semillas, sin stock ni video: en la intro (fuera de PLAYING) la ranura va gris y sin precio, «Te faltan
            // {n}» ni video, como la Cuia extra; en PLAYING, el precio en rojo con su aviso (revisión de T14).
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(50));
            yield return PlayModeHelpers.LoadGame(106, new MemoryProgressStore(106, 105));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 100);
            BoosterTray tray = boot.View!.Tray;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(tray.RevealShown, Is.True, "nivel turbio con ocultas");
            Assert.That(tray.RevealButton.interactable, Is.False, "gris");
            Assert.That(tray.RevealPriceShown, Is.EqualTo(0), "sin precio");
            Assert.That(tray.RevealShowsMissing, Is.False, "sin «Te faltan {n}»");
            Assert.That(tray.RevealShowsVideo, Is.False);
            Assert.That(tray.RevealBadgeText, Is.Empty);
            Assert.That(tray.ExtraPriceShown, Is.EqualTo(0), "la Cuia extra, igual");
            Assert.That(tray.ExtraShowsMissing, Is.False);

            StepUntilPlaying(boot);
            PlayModeHelpers.Step(boot, 50); // la bandeja se repinta en el Tick de la vista que sigue a PLAYING
            Assert.That(tray.RevealButton.interactable, Is.True);
            Assert.That(tray.RevealPriceShown, Is.EqualTo(120), "reveal_seed_price");
            Assert.That(tray.RevealShowsMissing, Is.True);
            Assert.That(tray.RevealMissingText, Is.EqualTo("Te faltan 70"));

            // Con video y sin saldo, el «+1» con el icono de video solo llega con PLAYING.
            LaunchOptions.AdFillOverride = null;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(106, new MemoryProgressStore(106, 105));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 100);
            tray = boot.View!.Tray;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(tray.RevealShowsVideo, Is.False);
            Assert.That(tray.RevealPriceShown, Is.EqualTo(0));
            Assert.That(tray.RevealShowsMissing, Is.False);
            StepUntilPlaying(boot);
            PlayModeHelpers.Step(boot, 50);
            Assert.That(tray.RevealShowsVideo, Is.True);

            // Con stock (el regalo del 100), su «+N» también fuera de PLAYING, en gris, como la Cuia extra con stock.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(100, new MemoryProgressStore(100, 99));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 100);
            tray = boot.View!.Tray;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(tray.RevealBadgeText, Is.EqualTo("+1"));
            Assert.That(tray.RevealButton.interactable, Is.False);
            Assert.That(tray.RevealPriceShown, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Lluvia194_LiftedSkyDrawsInFrontOfTheBadge()
        {
            // Captura 48c (revisión de T09): en el 194 (3 filas a 360 × 780) la Lluvia está en la fila 1 y su cielo (los 32 dp sobre
            // la boca, con la nube) entra en la fila del HUD al elevarla, donde la insignia «Nivel 194» lo tapaba. Mientras está
            // elevada, la cuia se dibuja delante del HUD (detrás de la capa de vertido y de la pausa); al bajar, vuelve a su sitio
            // en el tablero, entre las capas de mecánicas.
            yield return HudSetup.LoadPlaying(194);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            int rain = boot.Flow!.Session.Counter.CuiaIndex;
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            Assert.That(view.Layout.RowOf[rain], Is.EqualTo(0), "la Lluvia en la fila 1");
            Assert.That(HudSetup.FillIfEmpty(boot, rain), Is.True, "empieza vacía: antes de elevarla recibe un vertido");
            CuiaView cuia = view.Views[rain];
            Assert.That(cuia.Root.parent, Is.SameAs(view.BoardContainer), "en reposo, en el tablero");
            float mouth = -cuia.FootPosition.y - (cuia.HeightU * cuia.Unit);
            Rect badge = VictorySetup.CanvasRect(view.LevelBadgeRect, (RectTransform)view.Canvas.transform);
            Assert.That(mouth - CuiaView.LiftDp - CuiaView.TouchHeadroomDp, Is.LessThan(badge.yMax), "elevada, su cielo entra bajo la insignia");

            // Cambia de padre en el primer fotograma tras el toque (SyncLiftFront) sin saltar: con un fotograma de 0 ms la
            // elevación no avanza, así que su cuerpo sigue en el mismo punto del lienzo, ahora bajo LiftFront (revisión de L2a).
            Vector2 rest = CanvasPoint(view, cuia);
            Assert.That(boot.TapCuia(rain).Kind, Is.EqualTo(TapKind.Selected));
            Vector2 beforeSwap = CanvasPoint(view, cuia);
            Assert.That(cuia.Root.parent, Is.SameAs(view.BoardContainer), "el toque aún no la cambia de capa");
            view.Tick(0f);
            Assert.That(cuia.Root.parent, Is.SameAs(view.LiftFront), "delante del HUD desde el primer fotograma");
            Assert.That(Vector2.Distance(CanvasPoint(view, cuia), beforeSwap), Is.LessThan(0.01f), "al pasar a LiftFront, el cuerpo no salta (dp)");
            PlayModeHelpers.Step(boot, 300);
            Assert.That(cuia.Root.parent, Is.SameAs(view.LiftFront), "elevada, delante del HUD");
            int lift = view.LiftFront.GetSiblingIndex();
            Assert.That(view.LiftFront.parent, Is.SameAs(view.Hud.parent));
            Assert.That(lift, Is.GreaterThan(view.Hud.GetSiblingIndex()), "delante del HUD (la insignia)");
            Assert.That(lift, Is.LessThan(view.PourFront.GetSiblingIndex()), "detrás de la capa de vertido");
            Assert.That(lift, Is.LessThan(view.PauseButton.transform.parent.GetSiblingIndex()), "y de la pausa");
            Assert.That(view.LiftFront.anchoredPosition, Is.EqualTo(view.BoardContainer.anchoredPosition), "alineada con el tablero");

            // Al bajar vuelve al tablero sin saltar: en el fotograma del cambio de padre el cuerpo se mueve menos que en el
            // anterior (la bajada frena al llegar, ease-in) y acaba en el punto de antes de elevarla.
            Assert.That(boot.TapCuia(rain).Kind, Is.EqualTo(TapKind.Deselected));
            Vector2 last = CanvasPoint(view, cuia);
            float lastStep = float.PositiveInfinity;
            bool returned = false;
            for (int t = 0; t < 400 && !returned; t += 4)
            {
                PlayModeHelpers.TickExact(boot, 4);
                Vector2 now = CanvasPoint(view, cuia);
                float step = Vector2.Distance(now, last);
                returned = cuia.Root.parent == view.BoardContainer;
                if (returned)
                {
                    Assert.That(step, Is.LessThanOrEqualTo(lastStep + 0.01f), "al volver al tablero, el cuerpo no salta (" + step + " dp tras " + lastStep + ")");
                }

                lastStep = step;
                last = now;
            }

            Assert.That(returned, Is.True, "vuelve al tablero al terminar de bajar");
            PlayModeHelpers.Step(boot, 400);
            Assert.That(cuia.IsLifted, Is.False);
            Assert.That(cuia.Root.parent, Is.SameAs(view.BoardContainer), "abajo, de vuelta en el tablero");
            Assert.That(Vector2.Distance(CanvasPoint(view, cuia), rest), Is.LessThan(0.01f), "en el punto de antes de elevarla (dp)");
            int back = view.MechanicBackLayer.GetSiblingIndex();
            Assert.That(cuia.Root.GetSiblingIndex(), Is.EqualTo(back + 1 + rain), "en su sitio entre las capas de mecánicas");
            Assert.That(view.MechanicFrontLayer.GetSiblingIndex(), Is.EqualTo(back + 1 + view.Views.Length));

            // 160 (10 cuias, 2 filas): el cielo de la Lluvia elevada de la fila 1 queda bajo el HUD y la cuia no cambia de capa.
            yield return HudSetup.LoadPlaying(160);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            rain = boot.Flow!.Session.Counter.CuiaIndex;
            Assert.That(view.Layout!.Rows, Is.EqualTo(2));
            Assert.That(view.Layout.RowOf[rain], Is.EqualTo(0));
            Assert.That(HudSetup.FillIfEmpty(boot, rain), Is.True);
            cuia = view.Views[rain];
            mouth = -cuia.FootPosition.y - (cuia.HeightU * cuia.Unit);
            badge = VictorySetup.CanvasRect(view.LevelBadgeRect, (RectTransform)view.Canvas.transform);
            Assert.That(mouth - CuiaView.LiftDp - CuiaView.TouchHeadroomDp, Is.GreaterThanOrEqualTo(badge.yMax), "su cielo, bajo el HUD");
            Assert.That(boot.TapCuia(rain).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.Step(boot, 300);
            Assert.That(cuia.Root.parent, Is.SameAs(view.BoardContainer), "en el tablero también elevada");
            boot.TapCuia(rain);
        }

        [UnityTest]
        public IEnumerator ExtraCuia_ShelvesSlideWithTheirRow()
        {
            // 109 (Remolino, 10 cuias en 2 filas de 5): con la Cuia extra pasan a 5/6 y la cuia se estrecha; el bloque sigue
            // centrado, así que el pie de la fila 1 se queda y el de la 2 sube. Durante los 300 ms del relayout cada repisa va con
            // su fila, como el canal del Remolino que se dibuja sobre ella (revisión de T16).
            yield return LoadPlaying(109, FunnelSetup.Profile(500));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            int old = view.Views.Length;
            int rows = view.Layout!.Rows;
            Assert.That(rows, Is.EqualTo(2));
            float startFoot = view.Layout.RowFootY[rows - 1];
            AssertShelvesOnTheirRow(view, old, "antes");
            Assert.That(boot.PressExtraCuia(), Is.True, "Cuia extra con semillas");
            Assert.That(view.Relayouting, Is.True);
            AssertShelvesOnTheirRow(view, old, "al empezar el relayout");
            PlayModeHelpers.Step(boot, 6 * PlayModeHelpers.FrameMs);
            Assert.That(view.Relayouting, Is.True);
            AssertShelvesOnTheirRow(view, old, "a mitad del relayout");
            PlayModeHelpers.Step(boot, 700);
            Assert.That(view.Relayouting, Is.False);
            AssertShelvesOnTheirRow(view, view.Views.Length, "al terminar");
            Assert.That(Mathf.Abs(view.Layout!.RowFootY[rows - 1] - startFoot), Is.GreaterThan(1f), "el pie de la última fila se movió: si no, no habría nada que seguir");
        }

        // La cara de arriba de cada repisa va 3 dp sobre el pie de las cuias de su fila (guía 4.5), mire donde mire el relayout;
        // solo las count primeras cuias (la añadida entra en su sitio, sin deslizarse).
        private static void AssertShelvesOnTheirRow(GameScreen view, int count, string when)
        {
            BoardLayoutResult l = view.Layout!;
            for (int r = 0; r < l.Rows; r++)
            {
                var shelf = (RectTransform)view.BoardContainer.Find("Shelf" + r);
                Assert.That(shelf.gameObject.activeSelf, Is.True, "repisa " + r);
                float top = -shelf.anchoredPosition.y;
                int cuias = 0;
                for (int i = 0; i < count && i < view.Views.Length; i++)
                {
                    if (l.RowOf[i] == r)
                    {
                        float foot = -view.Views[i].FootPosition.y;
                        Assert.That(top, Is.EqualTo(foot - 3f).Within(0.01f), "repisa " + r + " y pie de la cuia " + i + " " + when);
                        cuias++;
                    }
                }

                Assert.That(cuias, Is.GreaterThan(0), "la fila " + r + " tiene cuias " + when);
            }
        }

        private static IEnumerator LoadPlaying(int level, Profile profile)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            StepUntilPlaying(boot);
            PlayModeHelpers.Step(boot, 50);
        }

        // Salta la intro y avanza hasta PLAYING con la vista quieta (como mucho 4 s).
        private static void StepUntilPlaying(GameBootstrap boot)
        {
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        // El mensaje de Unity de la vuelta de segundo plano (GameBootstrap.OnApplicationPause, privado).
        private static void ApplicationPause(GameBootstrap boot, bool paused)
        {
            MethodInfo? method = typeof(GameBootstrap).GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "GameBootstrap.OnApplicationPause(bool)");
            method!.Invoke(boot, new object[] { paused });
        }

        private static Vector2 ScreenCenter(CuiaView view)
        {
            RectTransform body = view.Body;
            Vector3 world = body.TransformPoint(body.rect.center);
            return RectTransformUtility.WorldToScreenPoint(null, world);
        }

        // Punto del cuerpo de la cuia en el lienzo (dp), el mismo sea cual sea su padre.
        private static Vector2 CanvasPoint(GameScreen view, CuiaView cuia)
        {
            return view.Canvas.transform.InverseTransformPoint(cuia.Body.position);
        }
    }
}
