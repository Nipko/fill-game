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
    /// queda como estaba y sus toques no hacen nada; se apaga al empezar la celebración.
    /// </summary>
    public sealed class BoosterTrayTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
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
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift), "04 §2.2: Capi trae una cuia en el 8");
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            Assert.That(view.Tray.SlotCount, Is.EqualTo(2));
            Assert.That(view.Tray.ExtraCuiaShown, Is.True);
            Assert.That(view.Tray.ExtraBadgeText, Is.EqualTo("+1"), "stock regalado");
            Assert.That(((RectTransform)view.ExtraCuiaButton.transform).rect.width, Is.GreaterThanOrEqualTo(48f));

            int rows = view.Layout!.Rows;
            int cuias = view.Views.Length;
            Assert.That(boot.PressExtraCuia(), Is.True);
            Assert.That(view.Views.Length, Is.EqualTo(cuias + 1));
            Assert.That(view.Relayouting, Is.True, "re-layout de 300 ms y entrada de 600 ms (02 §4.1)");
            PlayModeHelpers.Step(boot, 700);
            Assert.That(view.Relayouting, Is.False);
            Assert.That(view.Layout!.Rows, Is.EqualTo(rows), "las filas no cambian a mitad de nivel (D-006)");
            Assert.That(view.Views[cuias].Root.localScale.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(view.ExtraCuiaButton.interactable, Is.False, "1 por nivel");
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(0));
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
            Assert.That(!tray.RevealShown || !tray.RevealButton.interactable, Is.True, "Revelar, si está, gris");
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(presented));
            Assert.That(boot.Wallet.Stock.Get(BoosterKind.Undo), Is.EqualTo(0));
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

            // Nivel 100: Marea, Cuia extra y Revelar, de 64 dp; al irse Revelar quedan Marea y Cuia extra, recentradas.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(100, new MemoryProgressStore(100, 99));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            view = boot.View!;
            tray = view.Tray;
            AssertSlots(view, 3, tray.UndoButton, tray.ExtraCuiaButton, tray.RevealButton);
            Assert.That(tray.SetRevealVisible(false), Is.True);
            AssertSlots(view, 2, tray.UndoButton, tray.ExtraCuiaButton);
            Assert.That(tray.SetRevealVisible(true), Is.True);
            AssertSlots(view, 3, tray.UndoButton, tray.ExtraCuiaButton, tray.RevealButton);
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
