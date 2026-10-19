#nullable enable
using System;
using System.Collections;
using AF.Ads;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Bandeja de comodines en la escena (tarea B2-1; 07 §7.4; 04 §1.1, §2.1 y §2.2): en 1-7 solo Marea; desde el 8, Marea y
    /// Cuia extra con el regalo de Capi; la cuia añadida va a la fila más corta sin cambiar las filas (D-006); desde el 11,
    /// el cupo de Marea en el badge, y sin cupo la pregunta por el video como mucho cada 500 ms, sin asignaciones entre dos
    /// preguntas. Con la Luciérnaga (T14; 04 §2.5): 1, 2 y 3 ranuras de 64 dp con Revelar la última, recentradas, y los avisos
    /// «Te faltan {n}» de dos ranuras vecinas sin solaparse, a 12 sp (02 §12) y, si la cifra no cabe en una línea, en dos sobre
    /// su ranura, dentro del hueco entre el tablero y la bandeja. Con la victoria pendiente (D-011, punto 1), la bandeja se
    /// queda como estaba y sus toques no hacen nada (tampoco cortan el revelado en marcha); se apaga al empezar la
    /// celebración, con sus avisos calculados con el saldo de arriba (sin la Cosecha que aún no se ve), y la ranura de
    /// Revelar, sin ocultas, se va. En la pantalla de victoria, oculta, no se repinta mientras la Cosecha cuenta (sin
    /// asignaciones, 08 §5.4).
    /// Y la AF_LAYOUT level fiel (T-A y T-B de la prueba del 2026-10-09): en el 8, en frío y del 7 al 8, una sola desde la
    /// carga y ya con «+1 cuia»; tras «+1 cuia», una sola nueva al acabar el re-layout, con las N + 1 cuias en su sitio final.
    /// </summary>
    public sealed class BoosterTrayTests
    {
        private AfLayoutLog _layout = null!;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            _layout = new AfLayoutLog();
        }

        [TearDown]
        public void TearDown()
        {
            _layout?.Dispose();
            _layout = null!;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Levels1To7_OnlyMarea_From8_MareaAndTheGiftedExtraCuia()
        {
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.View!.Tray.SlotCount, Is.EqualTo(1), "en 1-7 la ranura de Cuia extra no existe y la bandeja se centra");
            Assert.That(boot.View.Tray.ExtraCuiaShown, Is.False);
            Assert.That(boot.View.Tray.UndoBadgeText, Is.Empty, "Marea sin contador en 1-10");

            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(8, new MemoryProgressStore(8, 7));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(view.ToastText, Is.Null, "D-011, punto 2: ningún aviso durante la intro");
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "el regalo de Capi espera su turno");
            boot.TapAnywhere();
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift), "04 §2.2: Capi trae una cuia en el 8, al habilitarse el toque");
            PlayModeHelpers.Step(boot, 50);
            Assert.That(view.Tray.SlotCount, Is.EqualTo(2));
            Assert.That(view.Tray.ExtraCuiaShown, Is.True);
            Assert.That(view.ExtraCuiaButton.gameObject.activeInHierarchy, Is.True, "la ranura existe y además se ve (T-A)");
            Assert.That(view.Tray.ExtraBadgeText, Is.EqualTo("+1"), "stock regalado");
            Assert.That(((RectTransform)view.ExtraCuiaButton.transform).rect.width, Is.GreaterThanOrEqualTo(48f));

            int rows = view.Layout!.Rows;
            int cuias = view.Views.Length;
            int layouts = _layout.AllOf("level", 8).Count;
            Assert.That(boot.PressExtraCuia(), Is.True);
            Assert.That(view.Views.Length, Is.EqualTo(cuias + 1));
            Assert.That(view.Relayouting, Is.True, "re-layout de 300 ms y entrada de 600 ms (02 §4.1)");
            Assert.That(_layout.AllOf("level", 8).Count, Is.EqualTo(layouts), "T-B: en t = 0 del re-layout no se publica (la cuia nueva medía 0 × 0)");
            PlayModeHelpers.Step(boot, 700);
            Assert.That(view.Relayouting, Is.False);
            Assert.That(view.Layout!.Rows, Is.EqualTo(rows), "las filas no cambian a mitad de nivel (D-006)");
            Assert.That(view.Views[cuias].Root.localScale.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(view.ExtraCuiaButton.interactable, Is.False, "1 por nivel");
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(0));

            // T-B: al acabar el re-layout sale una sola AF_LAYOUT level nueva, con las N + 1 cuias en su sitio final y la nueva
            // con su tamaño (antes salía en t = 0: las viejas en su sitio anterior y la nueva con w = h = 0).
            Assert.That(_layout.AllOf("level", 8).Count, Is.EqualTo(layouts + 1), "una sola AF_LAYOUT level nueva, al acabar el re-layout");
            string last = _layout.RequireLast("level", 8, "tras «+1 cuia»");
            RectInt[] published = AfLayoutLog.Cuias(last);
            Assert.That(published.Length, Is.EqualTo(cuias + 1), "N + 1 cuias");
            for (int i = 0; i < published.Length; i++)
            {
                RectInt now = AfLayoutLog.Published(view.Views[i].Root);
                Assert.That(published[i].width, Is.GreaterThan(0), "cuia " + i + ": w > 0");
                Assert.That(published[i].height, Is.GreaterThan(0), "cuia " + i + ": h > 0");
                Assert.That(published[i].x, Is.EqualTo(now.x).Within(2), "cuia " + i + ": x final");
                Assert.That(published[i].y, Is.EqualTo(now.y).Within(2), "cuia " + i + ": y final");
                Assert.That(published[i].width, Is.EqualTo(now.width).Within(2), "cuia " + i + ": w final");
                Assert.That(published[i].height, Is.EqualTo(now.height).Within(2), "cuia " + i + ": h final");
            }

            AfLayoutLog.AssertCenterInside(last, "undo", (RectTransform)view.UndoButton.transform);
            AfLayoutLog.AssertCenterInside(last, "extra_cuia", (RectTransform)view.ExtraCuiaButton.transform);
        }

        [UnityTest]
        public IEnumerator Level8_Cold_TheLevelLayoutBringsExtraCuia_FromTheFirstLine()
        {
            // T-A (prueba del 2026-10-09): la AF_LAYOUT level sale al final de OnLevelLoaded, después de los ganchos y con la
            // bandeja ya refrescada. En el 8 en frío sale una sola (antes, una primera sin «+1 cuia» y con Marea en el centro de
            // una bandeja de 1 ranura, y otra en el primer fotograma), y durante la INTRO «+1 cuia» ya está en la ranura 2.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(8, new MemoryProgressStore(8, 7));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(view.Tray.SlotCount, Is.EqualTo(2), "Marea y Cuia extra");
            Assert.That(view.ExtraCuiaButton.gameObject.activeInHierarchy, Is.True, "en la INTRO la ranura 2 no está vacía");
            AssertLevelLayouts(view, 8, "el 8 en frío");

            // Ya en juego no cambia nada: la misma línea sigue valiendo.
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            AssertLevelLayouts(view, 8, "el 8 en juego");
        }

        [UnityTest]
        public IEnumerator From7To8_TheSecondSlot_AndItsLayout_ComeWithTheLevel()
        {
            // T-A: en el paso del 7 al 8 (Siguiente) «+1 cuia» existe desde OnLevelLoaded, antes de cualquier fotograma, y la
            // AF_LAYOUT del 8 la trae desde la primera línea; en el primer fotograma se ve.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            Assert.That(view.Tray.ExtraCuiaShown, Is.False, "en el 7 no hay Cuia extra");
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 900);
            _layout.Clear();
            view.NextButton.onClick.Invoke();
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(8));
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(view.Tray.ExtraCuiaShown, Is.True, "la ranura ya existe al cargar el 8, sin esperar a un fotograma");
            Assert.That(view.Tray.SlotCount, Is.EqualTo(2));
            var eight = _layout.AllOf("level", 8);
            Assert.That(eight.Count, Is.GreaterThanOrEqualTo(1), "AF_LAYOUT level del 8 al cargarlo");
            Assert.That(AfLayoutLog.ButtonIds(eight[0]), Does.Contain("extra_cuia"), "la primera AF_LAYOUT del 8 trae «+1 cuia»: " + eight[0]);
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.ExtraCuiaButton.gameObject.activeInHierarchy, Is.True, "primer fotograma del 8: la ranura 2 no está vacía");
            AssertLevelLayouts(view, 8, "del 7 al 8");
        }

        [UnityTest]
        public IEnumerator TrayShownBeforeTheExtraCuiaSlotExists_TheSlotIsSeenAsSoonAsItAppears()
        {
            // T-A.1 (prueba del 2026-10-09), aislado: la bandeja se enseña (SetVisible(true)) cuando «+1 cuia» aún no existe y
            // la ranura aparece después sin otro SetVisible. Tiene que verse en cuanto aparece. Con la línea antigua de
            // BoosterTray.SetVisible (_extra.SetVisible(visible && _extra.Active)) la ranura se quedaba con «oculta» y Refresh la
            // activaba sin enseñarla: al reabrir la app, «+1 cuia» no se veía. Desde T-A.3 los flujos normales ya no llegan a
            // ese orden (Refresh corre en OnLevelLoaded, antes del SetVisible de SyncChrome), así que los tests de reabrir no lo
            // distinguen y este es el que protege el arreglo. Aquí: en la victoria del 7 (bandeja oculta, sin Cuia extra) se
            // enseña la bandeja y se pasa al 8, que la trae; se mira antes de cualquier fotograma, sin SyncChrome.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 900);
            Assert.That(view.Tray.ExtraCuiaShown, Is.False, "en el 7 la ranura «+1 cuia» no existe");
            view.Tray.SetVisible(true);
            Assert.That(view.UndoButton.gameObject.activeInHierarchy, Is.True, "la bandeja se ve");
            Assert.That(view.ExtraCuiaButton.gameObject.activeInHierarchy, Is.False, "y «+1 cuia» todavía no");

            view.NextButton.onClick.Invoke();
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(8));
            Assert.That(view.Tray.ExtraCuiaShown, Is.True, "la ranura aparece en OnLevelLoaded (T-A.3)");
            Assert.That(view.ExtraCuiaButton.gameObject.activeInHierarchy, Is.True, "T-A.1: aparece a la vista, sin esperar a otro SetVisible");
        }

        [UnityTest]
        public IEnumerator ExtraCuia_PauseOpenedDuringTheRelayout_TheLastLineIsStillThePause()
        {
            // T-B: el re-layout de «+1 cuia» (600 ms) no bloquea la entrada. Si a mitad se abre la Pausa, al acabar sale la línea
            // level con las N + 1 cuias (vale para el tablero cuando se cierre) y, detrás, otra vez la de la Pausa, que es lo que
            // se ve: la última línea nunca deja el toque debajo del modal.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(8, new MemoryProgressStore(8, 7));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            int cuias = view.Views.Length;
            int levels = _layout.AllOf("level", 8).Count;
            Assert.That(boot.PressExtraCuia(), Is.True);
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.Relayouting, Is.True, "a mitad del re-layout");

            view.PauseButton.onClick.Invoke();
            Assert.That(view.Pause.Visible, Is.True);
            string pause = _layout.RequireLast("pause", 8, "Pausa abierta");
            PlayModeHelpers.Step(boot, 700);
            Assert.That(view.Relayouting, Is.False);
            Assert.That(view.Pause.Visible, Is.True, "la Pausa sigue abierta");

            Assert.That(_layout.AllOf("level", 8).Count, Is.EqualTo(levels + 1), "una sola AF_LAYOUT level nueva, al acabar el re-layout");
            Assert.That(AfLayoutLog.Cuias(_layout.LastOf("level", 8)!).Length, Is.EqualTo(cuias + 1), "con las N + 1 cuias");
            Assert.That(_layout.AllOf("pause", 8).Count, Is.EqualTo(2), "la de la Pausa, al abrirse y repetida detrás de la level");
            Assert.That(_layout.RequireLast("pause", 8, "al acabar el re-layout"), Is.EqualTo(pause), "la última línea es la de la Pausa, igual");
        }

        [UnityTest]
        public IEnumerator From11_MareaShowsItsFreeQuota_AndWithoutQuotaOffersTheRefill()
        {
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            for (int i = 3; i >= 1; i--)
            {
                boot.TapCuia(0);
                boot.TapCuia(2);
                PlayModeHelpers.StepUntilIdle(boot, 3000);
                PlayModeHelpers.Step(boot, 20);
                Assert.That(view.Tray.UndoBadgeText, Is.EqualTo(i.ToString(System.Globalization.CultureInfo.InvariantCulture)), "cupo gratis en el badge");
                Assert.That(boot.PressUndo(), Is.True);
                PlayModeHelpers.StepUntilIdle(boot, 3000);
            }

            PlayModeHelpers.Step(boot, 20);
            Assert.That(view.Tray.UndoBadgeText, Is.EqualTo("+3"), "sin cupo: «+3» con el precio (sin fill no hay video)");
            Assert.That(boot.PressUndoRefillSeeds(), Is.False, "0 semillas: nada cambia");
        }

        [UnityTest]
        public IEnumerator MareaWithoutQuota_AsksForTheVideoEvery500ms_WithoutAllocating()
        {
            // Desde el 11, sin cupo ni stock, la bandeja ofrece «+3» con el icono de video si hay video de rv_undo_refill
            // (B2-1). Preguntar a los anuncios lee los contadores del día con la clave de la fecha local (IClock.LocalDateKey),
            // que crea una cadena cada vez: la bandeja pregunta como mucho cada BoosterTray.UndoVideoPollMs (500 ms, el plazo de
            // Revelar) y, entre dos preguntas, 20 fotogramas no asignan memoria (08 §5.4). Sin el límite preguntaría en cada
            // fotograma y la medida fallaría. La segunda carga mide (la primera calienta).
            LaunchOptions.LevelOffsetOverride = 10;
            for (int pass = 0; pass < 2; pass++)
            {
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
                yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
                GameBootstrap boot = GameBootstrap.Current!;
                PlayModeHelpers.TakeClock(boot);
                boot.TapAnywhere();
                PlayModeHelpers.Step(boot, 50);
                GameScreen view = boot.View!;
                BoosterTray tray = view.Tray;
                for (int i = 0; i < 3; i++)
                {
                    boot.TapCuia(0);
                    boot.TapCuia(2);
                    PlayModeHelpers.StepUntilIdle(boot, 3000);
                    Assert.That(tray.UndoVideoPollLeftMs, Is.EqualTo(0f), "con cupo no se pregunta por el video");
                    Assert.That(boot.PressUndo(), Is.True);
                    PlayModeHelpers.StepUntilIdle(boot, 3000);
                }

                PlayModeHelpers.Step(boot, 50);
                Assert.That(boot.Flow!.UndoStatus.HasQuota, Is.False, "sin cupo");
                Assert.That(tray.UndoBadgeText, Is.EqualTo("+3"));
                Assert.That(tray.UndoShowsVideo, Is.True, "con fill, el video");

                // Justo después de una pregunta, el plazo vuelve a UndoVideoPollMs.
                StepUntilUndoVideoAsked(boot, tray, 1000);
                Assert.That(tray.UndoVideoPollLeftMs, Is.GreaterThan(BoosterTray.UndoVideoPollMs - 8f), "acaba de preguntar");
                float left = tray.UndoVideoPollLeftMs;

                void Run20()
                {
                    for (int f = 0; f < 20; f++)
                    {
                        view.Tick(PlayModeHelpers.FrameMs);
                        boot.Flow!.Tick(PlayModeHelpers.FrameMs, view.IsViewIdle);
                    }
                }

                if (pass == 0)
                {
                    Run20();
                    yield return null;
                    continue;
                }

                Assert.That(Run20, Is.Not.AllocatingGCMemory(), "20 fotogramas con Marea sin cupo, entre dos preguntas");
                Assert.That(tray.UndoVideoPollLeftMs, Is.EqualTo(left - (20f * PlayModeHelpers.FrameMs)).Within(0.01f), "en esos 320 ms no volvió a preguntar");
                Assert.That(tray.UndoShowsVideo, Is.True);

                // Y no deja de preguntar: la siguiente pregunta llega a los 500 ms de la anterior.
                int more = StepUntilUndoVideoAsked(boot, tray, 1000);
                Assert.That((20f * PlayModeHelpers.FrameMs) + more, Is.EqualTo(BoosterTray.UndoVideoPollMs).Within(4f), "una pregunta cada 500 ms");
                Assert.That(tray.UndoShowsVideo, Is.True);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator WhileTheVictoryResolves_TheTrayStaysAsItWas_AndGoesGreyInTheCelebration()
        {
            // D-011, punto 1: con el último vertido la bandeja no da la victoria por hecha. Nivel 11 (tablero 1 con
            // af_level_offset = 10) con 100 semillas: Marea sin cupo ni stock ofrece «+3» con video (rv_undo_refill) y la Cuia
            // extra, el precio en rojo con «Te faltan 50». Mientras termina el último vertido (victoria pendiente), todo sigue
            // igual que antes del toque, y un toque en Marea o en la Cuia extra no hace nada: ni deshace, ni abre el video, ni
            // cobra (R15). En el primer fotograma de la celebración, la bandeja se apaga (y sigue a la vista).
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(100));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            BoosterTray tray = view.Tray;
            LevelFlow flow = boot.Flow!;
            for (int i = 0; i < 3; i++)
            {
                boot.TapCuia(0);
                boot.TapCuia(2);
                PlayModeHelpers.StepUntilIdle(boot, 3000);
                Assert.That(boot.PressUndo(), Is.True);
                PlayModeHelpers.StepUntilIdle(boot, 3000);
            }

            Assert.That(flow.UndoStatus.HasQuota, Is.False, "sin cupo ni stock");
            AF.Core.Rules.Pour last = VictorySetup.PlayAllButTheLastPour(boot);
            PlayModeHelpers.Step(boot, 50);

            // Antes del último toque.
            Assert.That(tray.UndoButton.interactable, Is.True, "sin cupo, Marea recarga");
            Assert.That(tray.UndoBadgeText, Is.EqualTo("+3"));
            Assert.That(tray.UndoShowsVideo, Is.True, "con fill, el video");
            Assert.That(tray.ExtraCuiaShown, Is.True);
            Assert.That(tray.ExtraCuiaButton.interactable, Is.True);
            Assert.That(tray.ExtraPriceShown, Is.EqualTo(150));
            Assert.That(tray.ExtraShowsMissing, Is.True);
            Assert.That(tray.ExtraMissingText, Is.EqualTo("Te faltan 50"));
            Assert.That(tray.RevealShown, Is.False, "nivel no turbio");
            Assert.That(tray.UndoButton.gameObject.activeInHierarchy, Is.True, "bandeja a la vista");
            int cuias = view.Views.Length;

            boot.TapCuia(last.Source);
            boot.TapCuia(last.Target);
            Assert.That(flow.IsResolvingWin, Is.True);
            int seeds = boot.Wallet!.Seeds;
            Assert.That(seeds, Is.EqualTo(120), "la Cosecha, ya cobrada");
            int pours = flow.Session.AppliedPours;
            int presented = FunnelSetup.AdsPresenter.Presented;
            int frames = 0;
            bool tapped = false;
            while (true)
            {
                Assert.That(frames, Is.LessThan(1000), "la victoria pendiente termina");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                if (!flow.IsResolvingWin)
                {
                    break;
                }

                frames++;
                string at = " (victoria pendiente, fotograma " + frames + ")";
                Assert.That(tray.UndoButton.interactable, Is.True, "Marea, como estaba" + at);
                Assert.That(tray.UndoBadgeText, Is.EqualTo("+3"), at);
                Assert.That(tray.UndoShowsVideo, Is.True, at);
                Assert.That(tray.ExtraCuiaButton.interactable, Is.True, "Cuia extra, como estaba" + at);
                Assert.That(tray.ExtraPriceShown, Is.EqualTo(150), at);
                Assert.That(tray.ExtraShowsMissing, Is.True, at);
                Assert.That(tray.ExtraMissingText, Is.EqualTo("Te faltan 50"), "con el saldo de antes de ganar" + at);
                Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(100), "el saldo, sin la Cosecha" + at);
                if (!tapped)
                {
                    // Un toque en Marea sin cupo y con video, y otro en la Cuia extra: nada cambia (R15).
                    tray.UndoButton.onClick.Invoke();
                    tray.ExtraCuiaButton.onClick.Invoke();
                    tapped = true;
                    Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(presented), "Marea no abre el video de rv_undo_refill");
                    Assert.That(flow.Session.AppliedPours, Is.EqualTo(pours), "ni deshace");
                    Assert.That(flow.Session.Status, Is.EqualTo(AF.Core.Session.LevelStatus.Won));
                    Assert.That(boot.Wallet.Stock.Get(BoosterKind.Undo), Is.EqualTo(0), "ni recarga");
                    Assert.That(boot.Wallet.Seeds, Is.EqualTo(seeds), "ni cobra");
                    Assert.That(view.Views.Length, Is.EqualTo(cuias), "la Cuia extra no se añade");
                    Assert.That(flow.IsResolvingWin, Is.True);
                }
            }

            Assert.That(frames, Is.GreaterThan(0), "el último vertido se anima con la victoria pendiente");
            Assert.That(tapped, Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Celebration));

            // Primer fotograma de la celebración: la vista lo pinta en su siguiente Tick. Apagada y a la vista.
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Celebration));
            Assert.That(tray.UndoButton.gameObject.activeInHierarchy, Is.True, "la bandeja sigue a la vista durante la celebración");
            Assert.That(tray.UndoButton.interactable, Is.False, "Marea gris en la celebración");
            Assert.That(tray.ExtraCuiaButton.interactable, Is.False, "Cuia extra gris en la celebración");
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(presented));
            Assert.That(boot.Wallet.Stock.Get(BoosterKind.Undo), Is.EqualTo(0));
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(seeds));
        }

        [UnityTest]
        public IEnumerator WithoutVideo_MareaKeepsItsMissingNotice_WithTheBalanceShown_DuringTheCelebration()
        {
            // D-011, punto 1: la bandeja calcula sus precios y avisos con el saldo que enseña el de arriba. Nivel 11 sin fill
            // (sin video de rv_undo_refill) con 10 semillas: Marea sin cupo ni stock enseña «+3» con el precio (30) y «Te faltan
            // 20». El último vertido cobra la Cosecha (+20, ya 30 semillas), pero durante la celebración el saldo de arriba
            // sigue en 10 y Marea, ya gris, sigue con «Te faltan 20»; con las 30 semillas de después de ganar, el aviso se iría
            // antes de que la Cosecha se vea.
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(10));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            BoosterTray tray = view.Tray;
            LevelFlow flow = boot.Flow!;
            for (int i = 0; i < 3; i++)
            {
                boot.TapCuia(0);
                boot.TapCuia(2);
                PlayModeHelpers.StepUntilIdle(boot, 3000);
                Assert.That(boot.PressUndo(), Is.True);
                PlayModeHelpers.StepUntilIdle(boot, 3000);
            }

            Assert.That(flow.UndoStatus.HasQuota, Is.False, "sin cupo ni stock");
            AF.Core.Rules.Pour last = VictorySetup.PlayAllButTheLastPour(boot);
            PlayModeHelpers.Step(boot, 50);
            Assert.That(tray.UndoBadgeText, Is.EqualTo("+3"));
            Assert.That(tray.UndoShowsVideo, Is.False, "sin fill no hay video");
            Assert.That(tray.UndoPriceShown, Is.EqualTo(30));
            Assert.That(tray.UndoMissingText, Is.EqualTo("Te faltan 20"));
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(10));

            boot.TapCuia(last.Source);
            boot.TapCuia(last.Target);
            Assert.That(flow.IsResolvingWin, Is.True);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(30), "la Cosecha, ya cobrada (eco_reward_normal = 20)");
            for (int frames = 0; flow.Phase != FlowPhase.Celebration; frames++)
            {
                Assert.That(frames, Is.LessThan(1000), "la victoria pendiente termina");
                Assert.That(tray.UndoMissingText, Is.EqualTo("Te faltan 20"), "victoria pendiente, fotograma " + frames);
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            // Toda la celebración, desde su primer fotograma (la vista lo pinta en su siguiente Tick): gris, con el aviso.
            int celebration = 0;
            while (true)
            {
                Assert.That(celebration, Is.LessThan(1000), "la celebración termina");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                if (flow.Phase != FlowPhase.Celebration)
                {
                    break;
                }

                celebration++;
                string at = " (celebración, fotograma " + celebration + ")";
                Assert.That(tray.UndoButton.gameObject.activeInHierarchy, Is.True, "la bandeja sigue a la vista" + at);
                Assert.That(tray.UndoButton.interactable, Is.False, "Marea gris" + at);
                Assert.That(tray.UndoPriceShown, Is.EqualTo(30), at);
                Assert.That(tray.UndoMissingText, Is.EqualTo("Te faltan 20"), "con el saldo que se ve arriba" + at);
                Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(10), "el saldo, sin la Cosecha" + at);
            }

            Assert.That(celebration, Is.GreaterThan(0), "fotogramas de celebración");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(30));
        }

        [UnityTest]
        public IEnumerator OnTheVictoryScreen_TheHiddenTrayDoesNotRepaintWhileTheHarvestCounts_WithMareasFreeQuota()
        {
            yield return HiddenTrayWhileTheHarvestCounts(true);
        }

        [UnityTest]
        public IEnumerator OnTheVictoryScreen_TheHiddenTrayDoesNotRepaintWhileTheHarvestCounts_WithoutQuotaNorVideo()
        {
            yield return HiddenTrayWhileTheHarvestCounts(false);
        }

        // D-011, punto 1, sin asignaciones por fotograma (08 §5.4). En la pantalla de victoria la bandeja está oculta y el saldo
        // de arriba recibe la Cosecha mientras la fila cuenta: lo que aún no enseña baja en cada fotograma. La bandeja no se
        // repinta por eso (GameScreen.RefreshHud solo le pasa la Cosecha pendiente en la celebración), ni en la cuenta de +0 a
        // +N (desde que el panel termina de subir, 250 ms) ni en la de +N a +2N tras la Cosecha doble; si se repintara, cada
        // repintado crearía cadenas. Se mira con BoosterTray.RefreshRepaints y no con Is.Not.AllocatingGCMemory: en el Editor,
        // TMP_Text.SetCharArray crea una cadena en cada llamada (solo en el Editor), y el saldo y la fila cuentan con ella.
        // Nivel 11 (tablero 1 con af_level_offset = 10) con 0 semillas. freeQuota: Marea con su cupo gratis (3), cuya insignia
        // crea una cadena en cada repintado; si no, sin cupo ni stock y sin video de rv_undo_refill (el tope del día,
        // ads_rv_cap_undo_refill = 4, ya visto; la Cosecha doble sí tiene el suyo), con «+3», el precio y «Te faltan {n}», que
        // además formatea y mide el aviso. El primer fotograma tras la Cosecha doble repinta una vez como mucho: el saldo cambió
        // (+N).
        private static IEnumerator HiddenTrayWhileTheHarvestCounts(bool freeQuota)
        {
            LaunchOptions.LevelOffsetOverride = 10;
            Profile profile = VictorySetup.Profile(0);
            if (!freeQuota)
            {
                profile.Daily.DateLocal = FunnelSetup.Clock.LocalDateKey();
                profile.Daily.RvUndoRefill = 4;
                profile.Daily.RvTotal = 4;
            }

            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            BoosterTray tray = view.Tray;
            LevelFlow flow = boot.Flow!;
            if (freeQuota)
            {
                Assert.That(flow.UndoStatus.FreeLeft, Is.EqualTo(3), "cupo gratis");
                Assert.That(tray.UndoBadgeText, Is.EqualTo("3"));
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    boot.TapCuia(0);
                    boot.TapCuia(2);
                    PlayModeHelpers.StepUntilIdle(boot, 3000);
                    Assert.That(boot.PressUndo(), Is.True);
                    PlayModeHelpers.StepUntilIdle(boot, 3000);
                }

                PlayModeHelpers.Step(boot, 50);
                Assert.That(flow.UndoStatus.HasQuota, Is.False, "sin cupo ni stock");
                Assert.That(boot.Ads!.CheckRewarded(AdPlacement.UndoRefill).Reason, Is.EqualTo(AdBlockReason.PlacementCap), "sin video de Marea hoy");
                Assert.That(tray.UndoBadgeText, Is.EqualTo("+3"));
                Assert.That(tray.UndoShowsVideo, Is.False);
                Assert.That(tray.UndoPriceShown, Is.EqualTo(30));
                Assert.That(tray.UndoMissingText, Is.EqualTo("Te faltan 30"));
            }

            int playing = tray.RefreshRepaints;
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(tray.RefreshRepaints, Is.GreaterThan(playing), "la bandeja se repinta al apagarse en la celebración: el contador cuenta");
            int harvest = flow.HarvestSeeds;
            Assert.That(harvest, Is.EqualTo(20), "eco_reward_normal");
            Assert.That(view.DoubleHarvestShown, Is.True, "la Cosecha doble, con su video");
            PlayModeHelpers.Step(boot, 256); // el panel sube en 250 ms; la fila aún no ha contado nada
            Assert.That(tray.UndoButton.gameObject.activeInHierarchy, Is.False, "la bandeja, oculta en la victoria");
            Assert.That(view.HarvestShown, Is.EqualTo(0));
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(0));

            // La cuenta de +0 a +N (400 ms, 02 §6), con el saldo de arriba subiendo con ella.
            int repaints = tray.RefreshRepaints;
            int changes = CountFrames(boot, view, tray, GameConfig.HarvestCountMs, repaints, "cuenta de +0 a +" + harvest);
            Assert.That(changes, Is.GreaterThanOrEqualTo(harvest / 2), "el saldo de arriba cambió en casi cada fotograma");
            Assert.That(view.HarvestShown, Is.EqualTo(harvest), "la fila contó entera");
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(harvest), "el saldo de arriba, con la Cosecha");

            view.DoubleHarvestButton.onClick.Invoke();
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(2 * harvest), "x2 exacto");
            Assert.That(view.AutoNextPending, Is.True);
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.HarvestShown, Is.InRange(harvest + 1, (2 * harvest) - 1), "la fila ya cuenta hacia +2N");
            Assert.That(tray.RefreshRepaints, Is.LessThanOrEqualTo(repaints + 1), "como mucho un repintado: el saldo cambió (+N)");
            repaints = tray.RefreshRepaints;
            changes = CountFrames(boot, view, tray, GameConfig.HarvestCountMs - PlayModeHelpers.FrameMs, repaints, "cuenta de +" + harvest + " a +" + (2 * harvest));
            Assert.That(changes, Is.GreaterThanOrEqualTo(harvest / 2), "el saldo de arriba cambió en casi cada fotograma");
            Assert.That(view.HarvestShown, Is.EqualTo(2 * harvest), "la fila contó entera");
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(2 * harvest));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "aún no avanza (400 + 600 ms)");
            Assert.That(flow.LevelNumber, Is.EqualTo(11));
            Assert.That(tray.UndoButton.gameObject.activeInHierarchy, Is.False);
        }

        // Avanza ms en fotogramas de 16 ms con la bandeja oculta: ningún repintado. Devuelve en cuántos cambió el saldo de arriba.
        private static int CountFrames(GameBootstrap boot, GameScreen view, BoosterTray tray, int ms, int repaints, string what)
        {
            int changes = 0;
            int shown = view.WalletBar.SeedsShown;
            for (int t = 0; t < ms; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                changes += view.WalletBar.SeedsShown != shown ? 1 : 0;
                shown = view.WalletBar.SeedsShown;
                Assert.That(tray.RefreshRepaints, Is.EqualTo(repaints), what + ", " + (t + PlayModeHelpers.FrameMs) + " ms: la bandeja oculta no se repinta");
            }

            return changes;
        }

        [UnityTest]
        public IEnumerator WhileARevealThatWinsResolves_TrayTapsDoNothing_AndRevealLeavesInTheCelebration()
        {
            // D-011, punto 1, con Revelar. Con la victoria pendiente, «+1 cuia» y Revelar se quedan como estaban, pero un toque no
            // hace nada: tampoco termina antes de tiempo el revelado en marcha (GameScreen.SettleReveal), que quitaría la ranura
            // y recentraría la bandeja. La luciérnaga solo vuela con la victoria pendiente si el revelado gana el nivel, y ningún
            // nivel de 1-200 lo tiene (RevealFlowTests.Reveal_ThatWins_FollowsThePourPath): nivel 13 de prueba con la cuia 0 =
            // [4*, Barro, 8]. El 8 sella la cuia 1, los tres 4 lavan el Barro ([4*, 4, 4, 4], llena pero sin sellar con la
            // oculta, R5) y la Luciérnaga, pagada con semillas, la revela: la cuia sella y el nivel queda ganado (R12) mientras la
            // luciérnaga vuela. La cara de Revelar ya no está pulsada (la selección se cerró antes de la victoria) y la ranura,
            // sin ocultas, está gris. En el primer fotograma de la celebración, la bandeja se apaga y la ranura de Revelar se va
            // (el revelado termina en el acto fuera de PLAYING). Sin video (no_fill): con él, Revelar lo abriría antes de cobrar
            // las semillas (D-028).
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGameWithPack(RevealThatWinsPack(), 13, new MemoryProgressStore(13, 12));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (flow.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            GameScreen view = boot.View!;
            BoosterTray tray = view.Tray;
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(1).Kind, Is.EqualTo(TapKind.Poured), "el 8 sella la cuia 1");
            Assert.That(boot.TapCuia(2).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Poured), "los tres 4 lavan el Barro");
            PlayModeHelpers.StepUntilIdle(boot, 5000);
            PlayModeHelpers.Step(boot, 50);
            Assert.That(flow.Session.Status, Is.EqualTo(AF.Core.Session.LevelStatus.Playing));
            Assert.That(flow.Session.Current[0].IsFull, Is.True);
            Assert.That(flow.Session.HiddenCount, Is.EqualTo(1), "la de abajo sigue oculta");
            Assert.That(tray.RevealShown, Is.True);
            Assert.That(tray.ExtraCuiaShown, Is.True);
            Assert.That(tray.ExtraCuiaButton.interactable, Is.True, "Cuia extra con 500 semillas");

            Assert.That(boot.PressReveal(), Is.True, "la Luciérnaga, con semillas");
            PlayModeHelpers.Step(boot, 50);
            Assert.That(flow.RevealSelecting, Is.True);
            Assert.That(tray.RevealPressed, Is.True, "la cara se queda abajo mientras se elige");
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Ignored), "el toque elige la cuia");
            Assert.That(flow.Session.Status, Is.EqualTo(AF.Core.Session.LevelStatus.Won), "el revelado gana");
            Assert.That(flow.IsResolvingWin, Is.True);
            Assert.That(view.RevealAnimating, Is.True, "la luciérnaga vuela con la victoria pendiente");
            Assert.That(tray.RevealShown, Is.True, "la luciérnaga sale de su botón");
            Assert.That(tray.RevealPressed, Is.False, "la selección se cerró antes de la victoria");
            Assert.That(tray.RevealRelief.Pressed, Is.False);
            Assert.That(tray.RevealButton.interactable, Is.False, "sin ocultas: gris");
            Assert.That(tray.ExtraCuiaButton.interactable, Is.True, "Cuia extra, como estaba");
            int seeds = boot.Wallet!.Seeds;
            int slots = tray.SlotCount;
            int cuias = view.Views.Length;
            int presented = FunnelSetup.AdsPresenter.Presented;

            // Un toque en «+1 cuia», en Revelar y en Marea: nada cambia.
            tray.ExtraCuiaButton.onClick.Invoke();
            tray.RevealButton.onClick.Invoke();
            tray.UndoButton.onClick.Invoke();
            Assert.That(view.RevealAnimating, Is.True, "el revelado sigue: no termina antes de tiempo");
            Assert.That(tray.RevealShown, Is.True, "la ranura no se va antes de tiempo");
            Assert.That(tray.SlotCount, Is.EqualTo(slots), "y la bandeja no se recentra");
            Assert.That(view.Views.Length, Is.EqualTo(cuias), "la Cuia extra no se añade");
            Assert.That(flow.RevealSelecting, Is.False, "Revelar no abre otra selección");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(seeds), "ni cobra");
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(presented), "ni abre un video");
            Assert.That(flow.IsResolvingWin, Is.True);

            // La cola está quieta: el flujo pasa a la celebración enseguida.
            int frames = 0;
            while (flow.Phase != FlowPhase.Celebration)
            {
                Assert.That(frames, Is.LessThan(1000), "la victoria pendiente termina");
                Assert.That(flow.IsResolvingWin, Is.True);
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                frames++;
            }

            // Primer fotograma de la celebración: la vista lo pinta en su siguiente Tick.
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Celebration));
            Assert.That(view.RevealAnimating, Is.False, "fuera de PLAYING el revelado termina en el acto");
            Assert.That(tray.RevealShown, Is.False, "sin ocultas, la ranura de Revelar se va");
            Assert.That(tray.UndoButton.gameObject.activeInHierarchy, Is.True, "la bandeja sigue a la vista durante la celebración");
            Assert.That(tray.UndoButton.interactable, Is.False, "Marea gris en la celebración");
            Assert.That(tray.ExtraCuiaButton.interactable, Is.False, "Cuia extra gris en la celebración");
            Assert.That(view.Views.Length, Is.EqualTo(cuias));
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(seeds));
        }

        [UnityTest]
        public IEnumerator MissingNotice_At12sp_AndWhatDoesNotFitGoesOnTwoLinesOverItsSlot()
        {
            // 02 §12: «Texto ≥ 12 sp»; el aviso va siempre a 12 sp (antes bajaba hasta 11) y lo que no cabe en una línea va en
            // dos. Nivel 106 a 360 × 780 dp con 50 semillas y sin video: Cuia extra (150) y Revelar (120) enseñan a la vez sus
            // avisos estrechos; «Te faltan 70» cabe en una línea y «Te faltan 100», no: su cifra baja a una segunda línea y la
            // placa crece hacia arriba, sobre su ranura y sin pisar el panel del tablero, que en el 106 llega tan abajo como
            // deja BoardLayout (el peor caso).
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(50));
            yield return PlayModeHelpers.LoadGame(106, new MemoryProgressStore(106, 105));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1000);
            GameScreen view = boot.View!;
            BoosterTray tray = view.Tray;
            Assert.That(tray.ExtraMissingWidth, Is.LessThan(72f), "estrechos: los dos a la vez");
            Assert.That(tray.ExtraMissingText, Is.EqualTo("Te faltan 100"));
            AssertNotice(tray.ExtraMissingLabel, 2, "«Te faltan 100» estrecho");
            AssertNotice(tray.RevealMissingLabel, 1, "«Te faltan 70» estrecho");
            AssertOverItsSlot(tray.ExtraMissingRect, (RectTransform)tray.ExtraCuiaButton.transform, "Cuia extra");
            AssertOverItsSlot(tray.RevealMissingRect, (RectTransform)tray.RevealButton.transform, "Revelar");
            Assert.That(WorldRect(tray.ExtraMissingRect).Overlaps(WorldRect(tray.RevealMissingRect)), Is.False, "sin solapes");
            AssertBelowTheBoard(view, tray.ExtraMissingRect, "«Te faltan 100» en dos líneas");
            Assert.That(tray.UndoBadgeText, Is.EqualTo("3"), "Marea, con su cupo en la insignia");
            AssertClearOfDisc(tray.ExtraMissingRect, tray.UndoBadgeRect, "«Te faltan 100» en dos líneas, junto a Marea");

            // Un precio de 4 cifras (fuera de los rangos de 08 §9; la API de la bandeja, la misma que llama GameScreen.Reveal):
            // la cifra baja a una segunda línea, la placa crece hacia arriba y el aviso sigue sobre su ranura sin pisar el de
            // la vecina.
            Assert.That(tray.RevealPriceRect.localScale.x, Is.EqualTo(1f), "con 3 cifras la etiqueta de precio no se encoge");
            tray.SetRevealBadge(0, 1500, 50, false, true, false);
            Assert.That(tray.RevealMissingText, Is.EqualTo("Te faltan 1.450"));
            AssertNotice(tray.RevealMissingLabel, 2, "«Te faltan 1.450» estrecho");
            AssertOverItsSlot(tray.RevealMissingRect, (RectTransform)tray.RevealButton.transform, "Revelar");
            Assert.That(WorldRect(tray.ExtraMissingRect).Overlaps(WorldRect(tray.RevealMissingRect)), Is.False, "sin solapes");
            AssertBelowTheBoard(view, tray.RevealMissingRect, "«Te faltan 1.450» en dos líneas");

            // La etiqueta «1.500» se encoge entera hasta caber en su paso de ranura (la cifra, a 12 sp o más) y no pisa la
            // de la vecina.
            Assert.That(tray.RevealPriceShown, Is.EqualTo(1500));
            Rect price = WorldRect(tray.RevealPriceRect);
            Rect slot = WorldRect((RectTransform)tray.RevealButton.transform);
            float dp = slot.width / 64f;
            Assert.That(price.width / dp, Is.LessThanOrEqualTo(70.01f), "dentro de su paso de ranura (64 + 8 − 2 dp)");
            Assert.That(price.center.x, Is.EqualTo(slot.center.x).Within(0.5f * dp), "centrada bajo su ranura");
            Assert.That(tray.RevealPriceRect.localScale.x * 16f, Is.GreaterThanOrEqualTo(12f - 0.01f), "la cifra, a 12 sp o más");
            Assert.That(price.Overlaps(WorldRect(tray.ExtraPriceRect)), Is.False, "sin pisar el precio de la Cuia extra");

            // Sin la vecina, el aviso ancho (104 dp) lleva las 4 cifras en una línea a 12 sp.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(200));
            yield return PlayModeHelpers.LoadGame(106, new MemoryProgressStore(106, 105));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1000);
            tray = boot.View!.Tray;
            Assert.That(tray.ExtraShowsMissing, Is.False, "con 200 semillas la Cuia extra se paga");
            tray.SetRevealBadge(0, 1500, 200, false, true, false);
            Assert.That(tray.RevealMissingText, Is.EqualTo("Te faltan 1.300"));
            Assert.That(tray.RevealMissingWidth, Is.GreaterThan(100f), "ancho");
            AssertNotice(tray.RevealMissingLabel, 1, "«Te faltan 1.300» ancho");
            AssertOverItsSlot(tray.RevealMissingRect, (RectTransform)tray.RevealButton.transform, "Revelar");
        }

        [UnityTest]
        public IEnumerator RevealSlot_OneTwoAndThreeSlots_RecentersAndKeepsTheCanonicalOrder()
        {
            // T14; 07 §7.4: Revelar va siempre la última a la derecha y, sin ella, la bandeja se recentra conservando el orden.
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;
            BoosterTray tray = view.Tray;
            AssertSlots(view, 1, tray.UndoButton);
            Assert.That(tray.RevealShown, Is.False, "nivel 7: sin Aguas turbias");

            // Marea y Revelar (solo la API de la bandeja: en la campaña no hay niveles turbios antes del 8).
            Assert.That(tray.SetRevealVisible(true), Is.True);
            AssertSlots(view, 2, tray.UndoButton, tray.RevealButton);
            Assert.That(tray.SetRevealVisible(true), Is.False, "sin cambio");
            Assert.That(tray.SetRevealVisible(false), Is.True);
            AssertSlots(view, 1, tray.UndoButton);
            Assert.That(tray.RevealButton.gameObject.activeInHierarchy, Is.False);

            // Nivel 100: Marea, Cuia extra, Pista (MO2) y Revelar, de 64 dp; al irse Revelar quedan las otras tres, recentradas.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(100, new MemoryProgressStore(100, 99));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            view = boot.View!;
            tray = view.Tray;
            AssertSlots(view, 4, tray.UndoButton, tray.ExtraCuiaButton, tray.HintButton, tray.RevealButton);
            Assert.That(tray.SetRevealVisible(false), Is.True);
            AssertSlots(view, 3, tray.UndoButton, tray.ExtraCuiaButton, tray.HintButton);
            Assert.That(tray.SetRevealVisible(true), Is.True);
            AssertSlots(view, 4, tray.UndoButton, tray.ExtraCuiaButton, tray.HintButton, tray.RevealButton);
        }

        [UnityTest]
        public IEnumerator MissingNotices_OfNeighbourSlots_DoNotOverlap()
        {
            // Nivel 106 con 50 semillas y sin video: Cuia extra (150) y Revelar (120) enseñan su precio en rojo y «Te faltan {n}»
            // a la vez; los dos avisos se estrechan a su paso de ranura y no se pisan.
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(50));
            yield return PlayModeHelpers.LoadGame(106, new MemoryProgressStore(106, 105));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1000);
            BoosterTray tray = boot.View!.Tray;
            Assert.That(tray.ExtraShowsMissing, Is.True);
            Assert.That(tray.RevealShowsMissing, Is.True);
            Assert.That(tray.RevealMissingText, Is.EqualTo("Te faltan 70"));
            Assert.That(WorldRect(tray.ExtraMissingRect).Overlaps(WorldRect(tray.RevealMissingRect)), Is.False, "sin solapes");
            Assert.That(tray.ExtraMissingWidth, Is.LessThan(72f));
            Assert.That(tray.RevealMissingWidth, Is.LessThan(72f));
        }

        // Nivel 13 de prueba (mundo 2) con el revelado que gana de RevealFlowTests.Reveal_ThatWins_FollowsThePourPath: la cuia 0
        // = [4*, Barro, 8] con la de abajo oculta, [8, 8, 8], [4, 4, 4] y una vacía. pack_schema 2, por el Barro (02 §9.1 M5).
        private static string RevealThatWinsPack()
        {
            string[] ids = { "acai", "aguaje" };
            int[][] board = { new[] { 4, AF.Core.Board.Cuia.MudColor, 8 }, new[] { 8, 8, 8 }, new[] { 4, 4, 4 }, new int[0] };
            string json = PlayModeHelpers.BoardPack(2, "w02_tapajos", "clear", 13, ids, board);
            string schema2 = json.Replace("\"subset_of\": \"af-level/1\"", "\"pack_schema\": 2");
            string murky = schema2.Replace("\"murky\": false, \"hidden\": []", "\"murky\": true, \"hidden\": [[0, 0]]");
            Assert.That(schema2, Is.Not.EqualTo(json), "el paquete de prueba cambió de forma");
            Assert.That(murky, Is.Not.EqualTo(schema2), "el paquete de prueba cambió de forma");
            return murky;
        }

        // Las AF_LAYOUT level del nivel: una sola desde la carga (ninguna obsoleta antes), con «+1 cuia», y el centro de Marea y
        // de «+1 cuia» dentro de lo que se ve ahora.
        private void AssertLevelLayouts(GameScreen view, int level, string what)
        {
            var lines = _layout.AllOf("level", level);
            Assert.That(lines.Count, Is.EqualTo(1), what + ": una sola AF_LAYOUT level, ninguna obsoleta antes:\n" + string.Join("\n", lines));
            string last = _layout.RequireLast("level", level, what);
            Assert.That(AfLayoutLog.ButtonIds(last), Does.Contain("undo").And.Contain("extra_cuia"), what);
            AfLayoutLog.AssertCenterInside(last, "undo", (RectTransform)view.UndoButton.transform);
            AfLayoutLog.AssertCenterInside(last, "extra_cuia", (RectTransform)view.ExtraCuiaButton.transform);
        }

        // n ranuras de 64 dp en el orden dado, con 8 dp entre ellas, y la bandeja (16 + n·64 + (n − 1)·8 + 16) centrada.
        private static void AssertSlots(GameScreen view, int n, params Button[] slots)
        {
            BoosterTray tray = view.Tray;
            float width = 32f + (n * 64f) + ((n - 1) * 8f);
            Assert.That(tray.SlotCount, Is.EqualTo(n));
            Assert.That(tray.Width, Is.EqualTo(width).Within(0.01f));
            float x = ((view.CanvasWidth - width) * 0.5f) + 16f;
            for (int i = 0; i < slots.Length; i++)
            {
                var rt = (RectTransform)slots[i].transform;
                Assert.That(slots[i].gameObject.activeInHierarchy, Is.True, rt.name);
                Assert.That(rt.rect.width, Is.EqualTo(64f).Within(0.01f), rt.name + ": 64 dp");
                Assert.That(rt.anchoredPosition.x, Is.EqualTo(x).Within(0.01f), rt.name + ": en su sitio, centrada y sin huecos");
                x += 72f;
            }
        }

        private static Rect WorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        // Avanza en fotogramas de 4 ms hasta que la bandeja vuelve a preguntar por el video de Marea (su plazo sube de golpe) y
        // devuelve los ms avanzados (falla pasado maxMs).
        private static int StepUntilUndoVideoAsked(GameBootstrap boot, BoosterTray tray, int maxMs)
        {
            bool asked = false;
            int t = 0;
            float last = tray.UndoVideoPollLeftMs;
            while (!asked && t < maxMs)
            {
                PlayModeHelpers.TickExact(boot, 4);
                t += 4;
                float now = tray.UndoVideoPollLeftMs;
                asked = now > last;
                last = now;
            }

            Assert.That(asked, Is.True, "la bandeja vuelve a preguntar por el video de Marea antes de " + maxMs + " ms");
            return t;
        }

        // La placa del aviso, en el hueco entre el tablero y la bandeja (02 §5; GameScreen): por debajo del panel del tablero
        // con 2 dp de aire o más. El 106 a 360 × 780 es el peor caso: su panel llega a 31,5 dp sobre la bandeja (BoardLayout), y
        // la placa de dos líneas, a 29,5 (BoosterTray: 29,5 dp de alto, a ras de la bandeja).
        private static void AssertBelowTheBoard(GameScreen view, RectTransform notice, string what)
        {
            Canvas.ForceUpdateCanvases();
            Rect plate = VictorySetup.CanvasRect(notice, (RectTransform)view.Canvas.transform);
            float panelBottom = view.Layout!.PanelBottom;
            Assert.That(plate.yMin - panelBottom, Is.GreaterThanOrEqualTo(2f - 0.01f), what + ": por debajo del tablero (su panel acaba en " + panelBottom + " dp y la placa empieza en " + plate.yMin + " dp)");
        }

        // La placa del aviso no toca el disco de una insignia (24 dp, arriba a la derecha de su ranura; asoma 8 dp sobre el
        // canto): del centro del disco a la placa hay al menos su radio.
        private static void AssertClearOfDisc(RectTransform notice, RectTransform disc, string what)
        {
            Canvas.ForceUpdateCanvases();
            Rect n = WorldRect(notice);
            Rect d = WorldRect(disc);
            Vector2 c = d.center;
            float radius = 0.5f * Mathf.Min(d.width, d.height);
            var closest = new Vector2(Mathf.Clamp(c.x, n.xMin, n.xMax), Mathf.Clamp(c.y, n.yMin, n.yMax));
            Assert.That(Vector2.Distance(closest, c), Is.GreaterThanOrEqualTo(radius * (1f - 0.001f)), what + ": sin tocar el disco de la insignia");
        }

        // El aviso, maquetado: a 12 sp (02 §12), las líneas dadas, entero y con el texto dentro de su placa.
        private static void AssertNotice(TextMeshProUGUI label, int lines, string what)
        {
            Canvas.ForceUpdateCanvases();
            label.ForceMeshUpdate(true, true);
            Assert.That(label.gameObject.activeInHierarchy, Is.True, what + " a la vista");
            Assert.That(label.fontSize, Is.EqualTo(12f).Within(0.01f), what + ": 12 sp");
            Assert.That(label.textInfo.lineCount, Is.EqualTo(lines), what + ": líneas");
            Assert.That(label.textInfo.characterCount, Is.EqualTo(label.text.Length), what + " entero");
            Rect r = label.rectTransform.rect;
            Bounds b = label.textBounds;
            Assert.That(b.min.x, Is.GreaterThanOrEqualTo(r.xMin - 0.5f), what + ": dentro de su placa por la izquierda");
            Assert.That(b.max.x, Is.LessThanOrEqualTo(r.xMax + 0.5f), what + ": dentro de su placa por la derecha");
            Assert.That(b.min.y, Is.GreaterThanOrEqualTo(r.yMin - 0.5f), what + ": dentro de su placa por abajo");
            Assert.That(b.max.y, Is.LessThanOrEqualTo(r.yMax + 0.5f), what + ": dentro de su placa por arriba");
        }

        // El aviso, centrado sobre su ranura y dentro de su paso (64 + 8 dp, si es estrecho) o de su ancho de 104 dp, encima del
        // canto de la ranura.
        private static void AssertOverItsSlot(RectTransform notice, RectTransform slot, string what)
        {
            Canvas.ForceUpdateCanvases();
            Rect n = WorldRect(notice);
            Rect s = WorldRect(slot);
            float dp = s.width / 64f; // unidades de mundo por dp
            Assert.That(n.center.x, Is.EqualTo(s.center.x).Within(0.5f * dp), what + ": centrado sobre la ranura");
            Assert.That(notice.rect.width, Is.LessThanOrEqualTo(104f + 0.01f), what);
            Assert.That(n.yMin, Is.GreaterThanOrEqualTo(s.yMax - (0.5f * dp)), what + ": encima del canto de la ranura");
        }
    }

    /// <summary>Ganchos comunes de los tests del embudo: anuncios y compras inmediatos y un reloj fijo.</summary>
    internal static class FunnelSetup
    {
        /// <summary>2026-10-08 15:00 UTC = 10:00 en Bogotá.</summary>
        public static readonly DateTime T0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);

        public static ManualClock Clock { get; private set; } = new ManualClock(T0, TimeSpan.FromHours(-5));

        public static ImmediatePurchasePresenter Purchases { get; private set; } = new ImmediatePurchasePresenter(true);

        public static ImmediateAdPresenter AdsPresenter { get; private set; } = new ImmediateAdPresenter();

        public static void Hooks()
        {
            LaunchOptions.Reset();
            Clock = new ManualClock(T0, TimeSpan.FromHours(-5));
            Purchases = new ImmediatePurchasePresenter(true);
            AdsPresenter = new ImmediateAdPresenter();
            LaunchOptions.ClockOverride = Clock;
            LaunchOptions.AdPresenterOverride = AdsPresenter;
            LaunchOptions.PurchasePresenterOverride = Purchases;
        }

        /// <summary>Perfil con <paramref name="seeds"/> semillas, <paramref name="canoes"/> canoas y el progreso hasta el 30.</summary>
        public static Profile Profile(int seeds, int canoes = 5)
        {
            Profile profile = Wallet.NewProfile(Clock);
            profile.Wallet.Seeds = seeds;
            profile.Wallet.Canoes.Count = canoes;
            if (canoes < 5)
            {
                profile.Wallet.Canoes.NextRegenAt = T0 + TimeSpan.FromMinutes(18) + TimeSpan.FromSeconds(42);
            }

            return profile;
        }

        /// <summary>Juega 3 → 4 en el tablero 6 del tutorial (Aguas estancadas) y espera al modal (R15).</summary>
        public static void GetStuck(GameBootstrap boot)
        {
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            for (int elapsed = 0; elapsed < 5000 && boot.Flow!.Phase != FlowPhase.StuckModal; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal));
            PlayModeHelpers.Step(boot, 300); // el panel termina de subir (250 ms)
        }
    }
}
