#nullable enable
using System.Collections;
using System.Globalization;
using System.IO;
using AF.Economy;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Compuerta de desbordes en en-XA (D-018 §3.7 y §6, nivel 2; 02 §12: hasta el 130 % sin desbordes a 360 dp; tarea L4): con
    /// la pseudolocalización (el español alargado un 35 % y entre corchetes; <see cref="LocPseudo"/>), a 360 × 640 dp (la
    /// pantalla 16:9 de 02 §5; <see cref="HudLabelTests.Screen640"/>) y con la fuente al 100 % y al 130 %, recorre las pantallas
    /// del inventario de L2b (<see cref="PseudoLocWalk"/>: el inicio, la tarjeta previa, el banner de mundo, la victoria, Aguas
    /// estancadas en sus ramas, ¿Seguro? y Nivel fallido) más la bandeja de comodines con sus tres ranuras (hallazgo 2 de la
    /// prueba en el teléfono del 2026-10-14: «[Déshácér ··]» se salía de su botón) y <b>falla</b> si algún texto visible se sale
    /// de su caja (o de la franja de la cinta «×3»/«×4»), no cabe ni al mínimo del autoajuste, baja de 12 sp (12 × escala dp)
    /// o sale sin corchetes (<see cref="LocInventory"/>). No necesita gráficos: corre con el resto de PlayMode.
    /// </summary>
    public sealed class PseudoLocOverflowTests
    {
        [TearDown]
        public void TearDown()
        {
            PseudoLocWalk.Release();
        }

        [UnityTest]
        public IEnumerator EnXA_360x640_NoTextOverflows([Values(100, 130)] int percent)
        {
            var inventory = new LocInventory(LocLocale.Pseudo, percent / 100f);
            yield return PseudoLocWalk.Run(LocLocale.Pseudo, percent / 100f, null, inventory);
            TestContext.WriteLine("AF_L4 en-XA al " + percent + " %: " + inventory.Scanned.ToString(CultureInfo.InvariantCulture) + " textos mirados, "
                + inventory.Problems.ToString(CultureInfo.InvariantCulture) + " con problemas");
            foreach (string note in inventory.Notes)
            {
                TestContext.WriteLine("AF_L4 aviso de contenido: " + note);
            }

            Assert.That(inventory.Scanned, Is.GreaterThan(80), "textos visibles mirados");
            Assert.That(inventory.Lines, Is.Empty, "textos que se salen o bajan de 12 sp en en-XA al " + percent + " %:\n" + string.Join("\n", inventory.Lines));
        }
    }

    /// <summary>
    /// Capturas de la compuerta de L4 (<see cref="PseudoLocOverflowTests"/>): las mismas pantallas en en-XA y en es-419, al 100 % y
    /// al 130 %, en <c>Logs/snapshots/&lt;idioma&gt;/l4_&lt;escala&gt;_*.png</c>, con el inventario de cada pasada
    /// (<c>l4_&lt;escala&gt;_inventory.txt</c>). Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.PseudoLocOverflowSnapshotTests</c> sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de L4 en en-XA y es-419; necesita dispositivo gráfico.")]
    public sealed class PseudoLocOverflowSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            PseudoLocWalk.Release();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator Capture([Values("en-XA", "es-419")] string locale, [Values(100, 130)] int percent)
        {
            string dir = SnapshotKit.Prepare(locale);
            string prefix = "l4_" + percent.ToString(CultureInfo.InvariantCulture) + "_";
            var inventory = new LocInventory(locale, percent / 100f);
            yield return PseudoLocWalk.Run(locale, percent / 100f, dir, inventory, prefix);
            string report = inventory.Write(dir, prefix + "inventory.txt");
            Debug.Log("AF_L4 " + locale + " al " + percent + " %: " + inventory.Scanned.ToString(CultureInfo.InvariantCulture) + " textos mirados, "
                + inventory.Problems.ToString(CultureInfo.InvariantCulture) + " con problemas; " + report);
            Assert.That(File.Exists(report), Is.True);
        }
    }

    /// <summary>
    /// El recorrido de las pantallas con texto del inventario de L2b (<see cref="PseudoLocInventoryTests"/>) y de la compuerta de
    /// L4 (<see cref="PseudoLocOverflowTests"/>) en un idioma y una escala de fuente, a 360 × 640 dp: en cada pantalla apunta sus
    /// textos en el <see cref="LocInventory"/> y, con carpeta, guarda la captura.
    /// </summary>
    internal static class PseudoLocWalk
    {
        /// <summary>
        /// Recorre el inicio (el 4, el 31 con canoas a medias y el 40 con la cinta del botón Jugar), la tarjeta previa (40, 100 y
        /// 183), el banner de mundo del 41, la bandeja de comodines del 100 en juego (Marea, «+1 cuia» con «Te faltan» y
        /// Revelar), la victoria del 11, Aguas estancadas del 31 (ramas A y B) y del 6 (rama C), ¿Seguro? y Nivel fallido en
        /// <paramref name="locale"/> y con la fuente a <paramref name="scale"/> (la global, <see cref="LaunchOptions.FontScaleOverride"/>,
        /// y la de la tarjeta y el banner). Con <paramref name="dir"/>, guarda cada captura en él (la cámara de
        /// <see cref="SnapshotKit"/>, que el llamador prepara); sin él, solo mira los textos (sin gráficos).
        /// </summary>
        public static IEnumerator Run(string locale, float scale, string? dir, LocInventory inventory, string prefix = "inv_")
        {
            FunnelSetup.Hooks(); // limpia los ganchos: el idioma, la pantalla y la escala van después
            LaunchOptions.LanguageOverride = locale == LocLocale.SourceLocale ? null : locale;
            if (Loc.IsConfigured && Loc.Locale != locale)
            {
                Loc.SetLocale(locale);
            }

            LaunchOptions.FontScaleOverride = scale;
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            var shot = new Shot(dir, inventory, prefix, scale);

            // Inicio: nivel 4 guardado con 1.240 semillas y, con af_level_offset = 30, el 31 con 3/5 canoas y su cronómetro.
            Profile p4 = Wallet.NewProfile(FunnelSetup.Clock);
            p4.Progress.NextLevel = 4;
            p4.Progress.MaxLevel = 3;
            p4.Wallet.Seeds = 1240;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p4));
            yield return shot.Take("inicio_l4", true);

            LaunchOptions.LevelOffsetOverride = 30;
            Profile p31 = FunnelSetup.Profile(2380, 3);
            p31.Progress.NextLevel = 31;
            p31.Progress.MaxLevel = 30;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p31));
            yield return shot.Take("inicio_l31", true);
            LaunchOptions.LevelOffsetOverride = null;

            // Inicio en el 40 (Difícil): el botón Jugar con el chip y la cinta «×3» del premio multiplicado (D-009; 07 §7.7).
            Profile p40 = Wallet.NewProfile(FunnelSetup.Clock);
            p40.Progress.NextLevel = 40;
            p40.Progress.MaxLevel = 39;
            p40.Wallet.Seeds = 1240;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p40));
            Assert.That(GameBootstrap.Current!.Lobby!.ChipRibbonText, Is.Not.Empty, "la cinta del botón Jugar en el 40");
            yield return shot.Take("inicio_l40", true);

            // Tarjeta previa: Difícil (40), el estreno combinado de Aguas turbias (100) y el de la Piraña (183).
            foreach (int level in new[] { 40, 100, 183 })
            {
                yield return CardSetup.Load(level);
                GameBootstrap card = GameBootstrap.Current!;
                Assert.That(card.View!.LevelCard.Visible, Is.True, "tarjeta del " + level);
                card.View.CardFontScale = scale;
                PlayModeHelpers.Step(card, 50);
                yield return shot.Take("tarjeta_l" + level.ToString(CultureInfo.InvariantCulture), false);
            }

            // Banner de mundo: el 41, ya quieto.
            yield return CardSetup.Load(41);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.View!.WorldBannerView.Visible, Is.True);
            boot.View.BannerFontScale = scale;
            PlayModeHelpers.TickExact(boot, 1100);
            yield return shot.Take("banner_l41", false);

            // Bandeja de comodines del 100 en juego, sin semillas: Marea, «+1 cuia» con su precio y «Te faltan {n}», Pista y Revelar
            // con la Luciérnaga regalada (hallazgo 2 de la prueba del 2026-10-14).
            LaunchOptions.LevelCardsOverride = false;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(100, new MemoryProgressStore(100, 99));
            boot = GameBootstrap.Current!;
            boot.TapAnywhere();
            GameBootstrap hud = boot;
            yield return PlayModeHelpers.WaitFor(() => hud.Flow!.Phase == FlowPhase.Playing && hud.View!.IsViewIdle, "el 100 en juego");
            yield return SnapshotKit.Wait(0.5f);
            Assert.That(boot.View!.Tray.SlotCount, Is.EqualTo(4), "Marea, «+1 cuia», Pista y Revelar");
            Assert.That(boot.View.Tray.ExtraMissingLabel.gameObject.activeInHierarchy, Is.True, "«Te faltan {n}» de «+1 cuia»");
            yield return shot.Take("bandeja_l100", false);

            // Victoria del 11 (tablero 1 con af_level_offset = 10): Cosecha y Cosecha doble.
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            boot = GameBootstrap.Current!;
            yield return WinRealTime(boot);
            yield return SnapshotKit.Wait(1.2f);
            yield return shot.Take("victoria_l11", false);

            // Aguas estancadas en el 31 (tablero 6 con af_level_offset = 25): rama A con 1.240 semillas, ¿Seguro? y Nivel fallido.
            yield return LoadFunnel(31, 1240);
            boot = GameBootstrap.Current!;
            yield return StuckRealTime(boot);
            yield return shot.Take("aguas_estancadas_l31_a", false);
            boot.PressGiveUp();
            yield return SnapshotKit.Wait(0.3f);
            yield return shot.Take("seguro_l31", false);
            boot.ConfirmGiveUp();
            yield return SnapshotKit.Wait(0.3f);
            yield return shot.Take("nivel_fallido_l31", false);

            // Rama B (sin saldo: Puñado y Balsa) y rama C (nivel 6, antes de las canoas).
            yield return LoadFunnel(31, 140);
            boot = GameBootstrap.Current!;
            yield return StuckRealTime(boot);
            yield return shot.Take("aguas_estancadas_l31_b", false);

            LaunchOptions.LevelOffsetOverride = null;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(310));
            yield return PlayModeHelpers.LoadGame(6, new MemoryProgressStore(6, 5));
            boot = GameBootstrap.Current!;
            yield return StuckRealTime(boot);
            yield return shot.Take("aguas_estancadas_l6_c", false);
        }

        /// <summary>Deshace <see cref="Run"/>: pantalla real, ganchos limpios y los textos otra vez en es-419.</summary>
        public static void Release()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            if (Loc.IsConfigured && Loc.Locale != LocLocale.SourceLocale)
            {
                LocSetUpFixture.ConfigureEs419();
            }
        }

        internal static IEnumerator LoadFunnel(int level, int seeds)
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.LevelCardsOverride = false;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds, 3));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
        }

        // Tablero 6: 3 → 4 deja Aguas estancadas; espera al modal y a que el panel termine de subir.
        internal static IEnumerator StuckRealTime(GameBootstrap boot)
        {
            boot.TapAnywhere();
            boot.TapCuia(3);
            boot.TapCuia(4);
            LevelFlow flow = boot.Flow!;
            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.StuckModal, "modal");
            yield return SnapshotKit.Wait(0.45f);
        }

        // Victoria con la solución del solver, en tiempo real.
        internal static IEnumerator WinRealTime(GameBootstrap boot)
        {
            boot.TapAnywhere();
            LevelFlow flow = boot.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            for (int i = 0; i < solution.MoveCount; i++)
            {
                boot.TapCuia(solution.GetMove(i).Source);
                boot.TapCuia(solution.GetMove(i).Target);
            }

            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.VictoryScreen, "victoria");
        }

        // Una pantalla: con carpeta, la pinta en la cámara de las capturas y guarda el PNG; siempre apunta sus textos.
        private sealed class Shot
        {
            private readonly string? _dir;
            private readonly LocInventory _inventory;
            private readonly string _prefix;

            public Shot(string? dir, LocInventory inventory, string prefix, float scale)
            {
                _dir = dir;
                _inventory = inventory;
                _prefix = prefix;
                Assert.That(inventory.Scale, Is.EqualTo(scale).Within(1e-5f), "el inventario mide con la escala del recorrido");
            }

            public IEnumerator Take(string name, bool lobby)
            {
                GameBootstrap boot = GameBootstrap.Current!;
                if (_dir != null)
                {
                    if (lobby)
                    {
                        SnapshotKit.AttachLobby(boot);
                    }
                    else
                    {
                        SnapshotKit.Attach(boot);
                    }

                    if (boot.AdOverlayView != null)
                    {
                        SnapshotKit.AttachOverlay(boot.AdOverlayView.Canvas);
                    }
                }

                yield return SnapshotKit.Wait(0.3f);
                if (_dir != null)
                {
                    SnapshotKit.Save(_dir, _prefix + name);
                }

                _inventory.Scan(_prefix + name);
            }
        }
    }

    /// <summary>
    /// Los repliegues de L4 que las cadenas de la compuerta (<see cref="PseudoLocOverflowTests"/>, a 438 dp de ancho) no alcanzan
    /// (revisión de L4): la ranura de la bandeja que llega a <see cref="BoosterTray.SlotMaxDp"/> y pone su etiqueta en dos líneas
    /// con el glifo más pequeño, y la píldora «Reiniciar» que crece hacia la izquierda y, si no basta, en dos líneas. Se fuerzan
    /// con una etiqueta larga (a 438 dp) y con en-XA a 360 dp de ancho (<see cref="HudLabelTests.Screen780"/>, el teléfono de
    /// 02 §5), al 100 % y al 130 %. Mira que la etiqueta quede en las líneas que tocan, a 12 sp (× escala) o más y sin salirse
    /// (<see cref="LocInventory.ProblemsOf"/>), que el glifo no baje de <see cref="BoosterTray.LabelMinIconDp"/> ni pise la
    /// tinta de la etiqueta, que la bandeja no salga de la pantalla (<see cref="BoosterTray.TrayEdgeDp"/>) y que la píldora no
    /// tape la columna del saldo (<see cref="GameScreen.RestartPillGapDp"/>), las cuias (con los 16 dp que suben al
    /// seleccionarlas) ni otro texto. No necesita gráficos.
    /// </summary>
    public sealed class LongLabelLayoutTests
    {
        // Etiquetas largas forzadas (dp a 12 sp con Nunito ExtraBold): «Deshacer la jugada» mide unos 109 y no cabe ni en la
        // ranura de 104; sus dos palabras (53) caben cada una en una línea también al 130 %. «Reiniciar el nivel» mide unos 94:
        // al 100 % cabe en una línea de la píldora ensanchada (hasta unos 115 dp a 438 de ancho); al 130 %, no.
        internal const string LongUndo = "Deshacer la jugada";
        internal const string LongRestart = "Reiniciar el nivel";

        // Título forzado de la victoria: a 22 sp no cabe en la franja de 256 dp, y sí en dos líneas.
        internal const string LongVictory = "¡Nivel completado, muy bien hecho!";

        // Etiquetas forzadas del rescate: a 12 sp (× escala) la primera no cabe en una línea junto al precio y sí en dos en la
        // cara de siempre; la segunda necesita tres o más, que no caben en la cara (ModalKit.CtaTwoLinesShare) y el botón crece.
        internal const string LongRescue = "Continuar ahora con una cuia más";
        internal const string LongerRescue = "Continuar la partida ahora mismo con una cuia de rescate más para seguir";

        [TearDown]
        public void TearDown()
        {
            PseudoLocWalk.Release();
        }

        [UnityTest]
        public IEnumerator Tray_LongLabel_WidestSlotThenTwoLines([Values(100, 130)] int percent)
        {
            float scale = percent / 100f;
            yield return LongLabelSetup.LoadLevel100(LocLocale.SourceLocale, scale, HudLabelTests.Screen640);
            GameScreen view = GameBootstrap.Current!.View!;
            BoosterTray tray = view.Tray;
            tray.UndoLabel.text = LongUndo;
            view.RelayoutForTests();
            yield return null;

            // Con las 4 ranuras del 100 (Pista desde el 12, MO2), a 438 dp ya manda la pantalla (91 dp), no el tope de 104.
            Assert.That(tray.SlotCount, Is.EqualTo(4));
            Assert.That(LongLabelSetup.SlotCap(view), Is.LessThan(BoosterTray.SlotMaxDp), "a 438 dp de ancho, con 4 ranuras, manda la pantalla");
            Assert.That(tray.SlotWidth, Is.EqualTo(LongLabelSetup.SlotCap(view)).Within(0.01f), "las ranuras se ensanchan hasta lo que deja la pantalla");
            LongLabelSetup.AssertSlot(view, tray.UndoLabel, tray.UndoGlyph, scale, 2, "Marea");
            LongLabelSetup.AssertSlot(view, tray.ExtraLabel, tray.ExtraGlyph, scale, 1, "Cuia extra");
            LongLabelSetup.AssertSlot(view, tray.HintLabel, tray.HintGlyph, scale, 1, "Pista");
            LongLabelSetup.AssertSlot(view, tray.RevealLabel, tray.RevealGlyph, scale, 1, "Revelar");
            LongLabelSetup.AssertTrayInside(view);
            Assert.That(tray.GrowDp, Is.InRange(0f, BoosterTray.MaxGrowDp), "lo que crecen las ranuras hacia abajo");
        }

        [UnityTest]
        public IEnumerator Restart_LongLabel_GrowsLeftThenTwoLines([Values(100, 130)] int percent)
        {
            float scale = percent / 100f;
            yield return LongLabelSetup.LoadLevel100(LocLocale.SourceLocale, scale, HudLabelTests.Screen640);
            GameScreen view = GameBootstrap.Current!.View!;
            view.RestartLabel.text = LongRestart;
            view.RelayoutForTests();
            yield return null;

            LongLabelSetup.AssertRestart(view, scale, percent == 100 ? 1 : 2);
        }

        [UnityTest]
        public IEnumerator EnXA_360_TrayAndRestart([Values(100, 130)] int percent)
        {
            // A 360 dp de ancho, la columna del saldo llega hasta el área de Reiniciar: la píldora no puede crecer hacia la
            // izquierda y «[Réíñíçíár ···]» va en dos líneas ya al 100 %. Con las 4 ranuras del 100 (Pista desde el 12, MO2),
            // «[Déshácér ··]» ya no cabe en una línea al 100 % y va en dos, en la ranura más ancha que deja la pantalla. Al 130 %,
            // «[Déshácér» (74,6 dp) solo cabe entero en la bandeja apretada (79 dp por ranura) con el aire de
            // BoosterTray.LabelTightInsetDp (revisión de MO2; pendiente del dueño con la opción recomendada aplicada).
            float scale = percent / 100f;
            yield return LongLabelSetup.LoadLevel100(LocLocale.Pseudo, scale, HudLabelTests.Screen780);
            GameScreen view = GameBootstrap.Current!.View!;
            BoosterTray tray = view.Tray;
            Assert.That(view.CanvasWidth, Is.EqualTo(360f).Within(0.5f), "lienzo de 360 dp de ancho");

            // Al 130 %, «[Réíñíçíár» no cabe en el área: la píldora crece y estrecha la columna del saldo, sin partir la palabra
            // (tarea FX5). Lo que sube la cuia al seleccionarla, aparte (RestartPill_360_KnownIssues).
            LongLabelSetup.AssertRestart(view, scale, 2, false);
            LongLabelSetup.AssertSlot(view, tray.UndoLabel, tray.UndoGlyph, scale, 2, "Marea");
            if (percent == 130)
            {
                Assert.That(tray.Compact, Is.True, "«[Déshácér» no cabía entero con la separación de siempre");
                Assert.That(tray.LabelInsetNowDp, Is.EqualTo(BoosterTray.LabelTightInsetDp), "ni con el aire de la ranura de 64 dp");
            }

            LongLabelSetup.AssertSlotFits(view, tray.ExtraLabel, tray.ExtraGlyph, scale, "Cuia extra");
            LongLabelSetup.AssertSlotFits(view, tray.HintLabel, tray.HintGlyph, scale, "Pista");
            LongLabelSetup.AssertSlotFits(view, tray.RevealLabel, tray.RevealGlyph, scale, "Revelar");
            Assert.That(tray.SlotWidth, Is.EqualTo(LongLabelSetup.SlotCap(view)).Within(0.01f), "las ranuras llegan a lo que deja la pantalla");
            LongLabelSetup.AssertTrayInside(view);
            Assert.That(tray.GrowDp, Is.InRange(0f, BoosterTray.MaxGrowDp), "lo que crecen las ranuras hacia abajo");
        }

        [UnityTest]
        public IEnumerator EnXA_360_Restart_ThreeRows([Values(100, 130)] int percent)
        {
            // El 200 a 360 × 780 (Muy difícil, 11 cuias en 3 filas: el panel pasa bajo la fila del HUD y la fila 1 empieza más
            // arriba; HudLabelTests): la píldora en dos líneas crece hacia abajo sin tapar las cuias (ni su sitio al subir) ni el
            // contador y las etiquetas de la banda pegada bajo la placa.
            float scale = percent / 100f;
            yield return LongLabelSetup.LoadLevel(200, LocLocale.Pseudo, scale, HudLabelTests.Screen780);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(3), "11 cuias: 3 filas a 360 × 780");
            LongLabelSetup.AssertRestart(view, scale, 2, false);
        }

        [UnityTest]
        public IEnumerator EnXA_Victory_RibbonTitle([Values(100, 130)] int percent)
        {
            // La victoria del 11 en en-XA a 360 × 640 (guía 5.5; revisión de L4): «[¡Ñívél çómplétádó! ······]» cabe en una línea
            // al 100 %; al 130 % no cabe a 22 sp × 1,3 y va en dos, con la hoja más alta (hasta 88 dp) y la Cosecha debajo.
            float scale = percent / 100f;
            yield return LongLabelSetup.LoadVictory11(LocLocale.Pseudo, scale);
            LongLabelSetup.AssertRibbon(GameBootstrap.Current!.View!, scale, percent == 100 ? 1 : 2);
        }

        [UnityTest]
        public IEnumerator Victory_LongTitle_TwoLinesTallerLeaf()
        {
            // Un título forzado que no cabe en una línea ni a 22 sp al 100 % (es-419): dos líneas a 22 sp, la hoja crece y el
            // panel baja con ella (guía 5.5: «la hoja crece hasta 88 dp de alto. Nunca se recorta»).
            yield return LongLabelSetup.LoadVictory11(LocLocale.SourceLocale, 1f);
            GameScreen view = GameBootstrap.Current!.View!;
            LongLabelSetup.AssertRibbon(view, 1f, 1);
            view.VictoryRibbonTitle.text = LongVictory;
            view.RelayoutForTests();
            yield return null;
            LongLabelSetup.AssertRibbon(view, 1f, 2);
        }

        [UnityTest]
        public IEnumerator Rescue_LongLabel_TwoLinesThenTallerButton([Values(100, 130)] int percent)
        {
            // «Continuar: +1 cuia» de Aguas estancadas (rama A del 31) y de ¿Seguro? con una etiqueta forzada (ModalKit.FitCtaLabel;
            // revisión de L4): en dos líneas a 12 sp (× escala) o más, sin tapar el precio; y, si sus líneas no caben en la cara
            // (tres o más), el botón crece a lo alto.
            float scale = percent / 100f;
            yield return LongLabelSetup.LoadStuck(LocLocale.SourceLocale, scale, 1240);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            StuckModal stuck = view.Stuck;
            Assert.That(stuck.RescueButton.gameObject.activeInHierarchy, Is.True, "el rescate en la rama A del 31");
            LongLabelSetup.AssertRescue(stuck.RescueButton, stuck.RescueLabel, stuck.RescuePriceRect, stuck.RescueFaceDp, scale, 1, 0f, "Aguas estancadas");
            float normal = ((RectTransform)stuck.RescueButton.transform).rect.height;

            stuck.RescueLabel.text = LongRescue;
            view.ShowStuckView(true);
            yield return null;
            LongLabelSetup.AssertRescue(stuck.RescueButton, stuck.RescueLabel, stuck.RescuePriceRect, stuck.RescueFaceDp, scale, 2, normal, "Aguas estancadas");

            stuck.RescueLabel.text = LongerRescue;
            view.ShowStuckView(true);
            yield return null;
            float tall = LongLabelSetup.AssertRescue(stuck.RescueButton, stuck.RescueLabel, stuck.RescuePriceRect, stuck.RescueFaceDp, scale, -3, normal, "Aguas estancadas");
            Assert.That(tall, Is.GreaterThan(normal + 0.5f), "Aguas estancadas: el botón crece con la etiqueta en tres líneas (" + normal.ToString("0.0", CultureInfo.InvariantCulture) + " dp de siempre)");

            GiveUpConfirm giveUp = view.GiveUp;
            giveUp.RescueLabel.text = LongerRescue;
            Assert.That(boot.PressGiveUp(), Is.True, "¿Seguro?");
            yield return SnapshotKit.Wait(0.3f);
            Assert.That(giveUp.Visible && giveUp.RescueButton.gameObject.activeInHierarchy, Is.True, "el rescate en ¿Seguro?");
            float tallGiveUp = LongLabelSetup.AssertRescue(giveUp.RescueButton, giveUp.RescueLabel, giveUp.RescuePriceRect, giveUp.RescueFaceDp, scale, -3, normal, "¿Seguro?");
            Assert.That(tallGiveUp, Is.GreaterThan(normal + 0.5f), "¿Seguro?: el botón crece con la etiqueta en tres líneas");
        }

        [UnityTest]
        public IEnumerator Offers_AirAroundLabels_130([Values("en-XA", "es-419")] string locale)
        {
            // Puñado y Balsa de la rama B del 31 al 130 % (revisión de L4; guía 5.x: botones con aire interior): la cara crece a lo
            // alto hasta que entre la tinta de la etiqueta y el canto de la cara quedan StuckModal.OfferLabelPadDp arriba y abajo
            // (6 dp o más, con el redondeo de la tinta).
            yield return LongLabelSetup.LoadStuck(locale, 1.3f, 140);
            StuckModal stuck = GameBootstrap.Current!.View!.Stuck;
            Assert.That(stuck.HandfulButton.gameObject.activeInHierarchy && stuck.BalsaButton.gameObject.activeInHierarchy, Is.True, "Puñado y Balsa en la rama B");
            LongLabelSetup.AssertOfferAir(stuck.HandfulLabel, stuck.OfferFaceDp, 1.3f, "Puñado");
            LongLabelSetup.AssertOfferAir(stuck.BalsaLabel, stuck.OfferFaceDp, 1.3f, "Balsa");
        }

        /// <summary>
        /// La insignia del nivel en cada idioma (D-030, pregunta 8 b; antes LevelBadge_EnXA_KnownIssue, explícito): la placa no
        /// crece, así que fuera del español el texto baja de 20 sp lo justo para caber, nunca por debajo de 12 sp (× escala; 02
        /// §12) ni recortado; en español al 100 % sigue a 20 sp o más (guía 3.5: sin cambio). Al 130 %, «Nivel 100» ya no cabía
        /// a 26 dp ni en español (su tinta se salía 2-4 dp de la caja de 106): ahora baja lo justo, como en los otros idiomas.
        /// Con el 100 (4 ranuras en la bandeja) y el 700, el número más largo del juego.
        /// </summary>
        [UnityTest]
        public IEnumerator LevelBadge_FitsAt12OrMore([Values(LocLocale.SpanishLatinAmerica, LocLocale.English, LocLocale.PortugueseBrazil, LocLocale.Pseudo)] string locale,
            [Values(100, 130)] int percent, [Values(100, 700)] int level)
        {
            float scale = percent / 100f;
            yield return LongLabelSetup.LoadLevel(level, locale, scale, HudLabelTests.Screen640);
            TextMeshProUGUI title = GameBootstrap.Current!.View!.LevelTitle;
            TestContext.WriteLine("AF_DC19 " + locale + " al " + percent + " %: «" + title.text + "» a " + (title.fontSize / scale).ToString("0.##", CultureInfo.InvariantCulture) + " sp");
            Assert.That(title.text, Does.Contain(level.ToString(CultureInfo.InvariantCulture)), "la insignia del " + level);
            Assert.That(title.fontSize, Is.GreaterThanOrEqualTo((LocInventory.BadgeMinSp * scale) - 0.01f), "la insignia a 12 sp (× escala) o más");
            if (locale == LocLocale.SpanishLatinAmerica && percent == 100)
            {
                Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(GameScreen.BadgeMinSp - 0.01f), "en español al 100 %, a 20 sp o más");
            }

            Assert.That(new LocInventory(Loc.Locale, scale).ProblemsOf(title), Is.Empty, "«" + title.text + "» cabe en la placa");
        }

        /// <summary>
        /// Lo que la píldora «Reiniciar» no resuelve a 360 dp de ancho sin rediseño (revisión de L4; pendiente del dueño, ver la
        /// entrega de L4): en los tableros de 3 filas la píldora en dos líneas («[Réíñíçíár ···]» en en-XA) baja hasta el sitio
        /// al que sube la cuia de arriba a la derecha al seleccionarla. Las palabras partidas («Reinici / ar» en es-419 al 130 %)
        /// ya no pasan: la píldora crece y estrecha la columna del saldo (tarea FX5); con el 100 (2 filas) y en es-419, este test
        /// pasa. Se deja escrito (explícito: con en-XA en el 200, hoy falla) para cuando el dueño decida.
        /// </summary>
        [UnityTest]
        [Explicit("Pendiente del dueño (revisión de L4): la píldora Reiniciar en dos líneas a 360 dp en 3 filas; hoy falla con en-XA en el 200.")]
        public IEnumerator RestartPill_360_KnownIssues([Values("en-XA", "es-419")] string locale, [Values(100, 200)] int level)
        {
            yield return LongLabelSetup.LoadLevel(level, locale, 1.3f, HudLabelTests.Screen780);
            LongLabelSetup.AssertRestart(GameBootstrap.Current!.View!, 1.3f, GameBootstrap.Current!.View!.RestartLabel.textInfo.lineCount, true);
        }
    }

    /// <summary>
    /// Capturas de <see cref="LongLabelLayoutTests"/> (<c>Logs/snapshots/&lt;idioma&gt;/l4r_*.png</c>): en-XA a 360 dp de ancho y
    /// las etiquetas forzadas a 438, al 100 % y al 130 %. Explícito: necesita gráficos.
    /// </summary>
    [Explicit("Capturas de los repliegues de L4; necesita dispositivo gráfico.")]
    public sealed class LongLabelSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            PseudoLocWalk.Release();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator Capture_EnXA_360([Values(100, 130)] int percent)
        {
            string dir = SnapshotKit.Prepare(LocLocale.Pseudo);
            yield return LongLabelSetup.LoadLevel100(LocLocale.Pseudo, percent / 100f, HudLabelTests.Screen780);
            yield return Take(dir, "l4r_" + percent.ToString(CultureInfo.InvariantCulture) + "_juego360_l100");
        }

        [UnityTest]
        public IEnumerator Capture_EnXA_360_ThreeRows([Values(100, 130)] int percent)
        {
            string dir = SnapshotKit.Prepare(LocLocale.Pseudo);
            yield return LongLabelSetup.LoadLevel(200, LocLocale.Pseudo, percent / 100f, HudLabelTests.Screen780);
            yield return Take(dir, "l4r_" + percent.ToString(CultureInfo.InvariantCulture) + "_juego360_l200");
        }

        [UnityTest]
        public IEnumerator Capture_Forced_438([Values(100, 130)] int percent)
        {
            string dir = SnapshotKit.Prepare(LocLocale.SourceLocale);
            yield return LongLabelSetup.LoadLevel100(LocLocale.SourceLocale, percent / 100f, HudLabelTests.Screen640);
            GameScreen view = GameBootstrap.Current!.View!;
            view.Tray.UndoLabel.text = LongLabelLayoutTests.LongUndo;
            view.RestartLabel.text = LongLabelLayoutTests.LongRestart;
            view.RelayoutForTests();
            yield return Take(dir, "l4r_" + percent.ToString(CultureInfo.InvariantCulture) + "_forzada_l100");
        }

        private static IEnumerator Take(string dir, string name)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            LongLabelSetup.Log(boot.View!, name);
            SnapshotKit.Save(dir, name);
        }
    }

    /// <summary>Carga y comprobaciones de <see cref="LongLabelLayoutTests"/>.</summary>
    internal static class LongLabelSetup
    {
        // Lo que sube una cuia al seleccionarla (guía 6.1: 16 dp, con el rebote hasta 17,6).
        private const float LiftDp = 17.6f;

        private const float Tol = 0.5f;

        /// <summary>
        /// El 100 en juego (Marea, «+1 cuia», Pista y Revelar en la bandeja; sin semillas) en <paramref name="locale"/>, con la fuente a
        /// <paramref name="scale"/> y en la pantalla <paramref name="screen"/>.
        /// </summary>
        public static IEnumerator LoadLevel100(string locale, float scale, Vector4 screen)
        {
            yield return LoadLevel(100, locale, scale, screen);
            Assert.That(GameBootstrap.Current!.View!.Tray.SlotCount, Is.EqualTo(4), "Marea, «+1 cuia», Pista y Revelar");
        }

        /// <summary>El nivel <paramref name="level"/> en juego, sin semillas, como <see cref="LoadLevel100"/>.</summary>
        public static IEnumerator LoadLevel(int level, string locale, float scale, Vector4 screen)
        {
            FunnelSetup.Hooks();
            LaunchOptions.LanguageOverride = locale == LocLocale.SourceLocale ? null : locale;
            if (Loc.IsConfigured && Loc.Locale != locale)
            {
                Loc.SetLocale(locale);
            }

            LaunchOptions.FontScaleOverride = scale;
            GameScreen.SimulatedScreen = screen;
            LaunchOptions.LevelCardsOverride = false;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.TapAnywhere();
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.Phase == FlowPhase.Playing && boot.View!.IsViewIdle, "el " + level + " en juego");
            yield return SnapshotKit.Wait(0.3f);
        }

        /// <summary>
        /// La victoria del 11 (tablero 1 con af_level_offset = 10; Cosecha y Cosecha doble) en <paramref name="locale"/>, con la
        /// fuente a <paramref name="scale"/> y a 360 × 640 dp, como la recorre <see cref="PseudoLocWalk"/>.
        /// </summary>
        public static IEnumerator LoadVictory11(string locale, float scale)
        {
            Hooks(locale, scale, HudLabelTests.Screen640);
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            yield return PseudoLocWalk.WinRealTime(GameBootstrap.Current!);
            yield return SnapshotKit.Wait(1.2f);
        }

        /// <summary>
        /// Aguas estancadas del 31 (tablero 6 con af_level_offset = 25) con <paramref name="seeds"/> semillas (1.240: rama A; 140:
        /// rama B, con Puñado y Balsa) en <paramref name="locale"/>, con la fuente a <paramref name="scale"/> y a 360 × 640 dp.
        /// </summary>
        public static IEnumerator LoadStuck(string locale, float scale, int seeds)
        {
            Hooks(locale, scale, HudLabelTests.Screen640);
            yield return PseudoLocWalk.LoadFunnel(31, seeds);
            yield return PseudoLocWalk.StuckRealTime(GameBootstrap.Current!);
        }

        // Ganchos de una carga: limpios, con el idioma, la escala de la fuente y la pantalla.
        private static void Hooks(string locale, float scale, Vector4 screen)
        {
            FunnelSetup.Hooks();
            LaunchOptions.LanguageOverride = locale == LocLocale.SourceLocale ? null : locale;
            if (Loc.IsConfigured && Loc.Locale != locale)
            {
                Loc.SetLocale(locale);
            }

            LaunchOptions.FontScaleOverride = scale;
            GameScreen.SimulatedScreen = screen;
        }

        /// <summary>
        /// El título de la cinta de victoria (guía 5.5; revisión de L4): en <paramref name="lines"/> líneas, a 22 sp (× escala) o
        /// más, sin desborde ni palabras partidas; la hoja de 72 dp con una línea y, con dos, más alta, sin pasar de 88; la tinta
        /// del título dentro de la hoja; y la hoja por encima de la fila de la Cosecha, sin pisarla.
        /// </summary>
        public static void AssertRibbon(GameScreen view, float scale, int lines)
        {
            TextMeshProUGUI title = view.VictoryRibbonTitle;
            string problems = new LocInventory(Loc.Locale, scale).ProblemsOf(title);
            Assert.That(title.textInfo.lineCount, Is.EqualTo(lines), "cinta: líneas de «" + title.text + "»");
            Assert.That(title.fontSize, Is.GreaterThanOrEqualTo((22f * scale) - 0.01f), "cinta: 22 sp (× escala) o más");
            Assert.That(problems, Is.Empty, "cinta: el título no se sale de su caja");
            AssertNoBrokenWord(title, "cinta");

            float h = view.VictoryRibbonHeight;
            if (lines == 1)
            {
                Assert.That(h, Is.EqualTo(72f).Within(0.01f), "cinta de 72 dp con el título en una línea");
            }
            else
            {
                Assert.That(h, Is.InRange(72f, 88f), "cinta de 72 a 88 dp con el título en dos líneas");
            }

            RectTransform ribbon = view.VictoryRibbonRect;
            Assert.That(ribbon.rect.height, Is.EqualTo(h).Within(0.01f), "la hoja mide lo que dice VictoryRibbonHeight");
            Rect ink = InkRect(title, ribbon);
            Assert.That(ink.xMin, Is.GreaterThanOrEqualTo(-Tol), "cinta: la tinta empieza dentro de la hoja");
            Assert.That(ink.xMax, Is.LessThanOrEqualTo(ribbon.rect.width + Tol), "cinta: la tinta acaba dentro de la hoja");
            Assert.That(ink.yMin, Is.GreaterThanOrEqualTo(-Tol), "cinta: la tinta no baja de la hoja (" + Describe(ink) + " en " + h.ToString("0.0", CultureInfo.InvariantCulture) + " dp)");
            Assert.That(ink.yMax, Is.LessThanOrEqualTo(h + Tol), "cinta: la tinta no sube de la hoja (" + Describe(ink) + " en " + h.ToString("0.0", CultureInfo.InvariantCulture) + " dp)");

            var root = (RectTransform)view.Canvas.transform;
            Rect leaf = DpRect(ribbon, root);
            Rect harvest = DpRect(view.VictoryHarvestRect, root);
            Assert.That(leaf.yMin, Is.GreaterThanOrEqualTo(harvest.yMax - Tol), "la hoja " + Describe(leaf) + " no pisa la fila de la Cosecha " + Describe(harvest));
            Debug.Log("AF_L4R cinta: «" + title.text + "» en " + lines + " líneas a " + title.fontSize.ToString("0.0", CultureInfo.InvariantCulture) + " dp; hoja de "
                + h.ToString("0.0", CultureInfo.InvariantCulture) + " dp; tinta " + Describe(ink));
        }

        /// <summary>
        /// El rescate «Continuar: +1 cuia» (ModalKit.FitCtaLabel; revisión de L4): su etiqueta en <paramref name="lines"/> líneas (con
        /// un número negativo, en tantas o más), a
        /// 12 sp (× escala) o más, sin desborde ni palabras partidas, con la tinta dentro de la cara del botón y sin tocar la
        /// etiqueta de precio; con <paramref name="normal"/> &gt; 0, el botón no baja del alto de siempre. <paramref name="faceDp"/>: el
        /// alto de su cara visible. Devuelve su alto (dp).
        /// </summary>
        public static float AssertRescue(Button button, TextMeshProUGUI label, RectTransform price, float faceDp, float scale, int lines, float normal, string what)
        {
            string problems = new LocInventory(Loc.Locale, scale).ProblemsOf(label);
            if (lines > 0)
            {
                Assert.That(label.textInfo.lineCount, Is.EqualTo(lines), what + ": líneas de «" + label.text + "»");
            }
            else
            {
                Assert.That(label.textInfo.lineCount, Is.GreaterThanOrEqualTo(-lines), what + ": líneas de «" + label.text + "»");
            }

            Assert.That(label.fontSize, Is.GreaterThanOrEqualTo((12f * scale) - 0.01f), what + ": 12 sp (× escala) o más");
            Assert.That(problems, Is.Empty, what + ": la etiqueta no se sale de su caja");
            AssertNoBrokenWord(label, what);

            var rt = (RectTransform)button.transform;
            float height = rt.rect.height;
            if (normal > 0f)
            {
                Assert.That(height, Is.GreaterThanOrEqualTo(normal - 0.01f), what + ": el botón no baja de su alto de siempre");
            }

            // La cara visible ocupa los faceDp de arriba de la imagen de la cara (debajo van el canto y la sombra).
            var face = (RectTransform)label.rectTransform.parent;
            float top = face.rect.height;
            Rect ink = InkRect(label, face);
            Assert.That(ink.yMin, Is.GreaterThanOrEqualTo(top - faceDp - Tol), what + ": la tinta " + Describe(ink) + " no baja de la cara de " + faceDp.ToString("0.0", CultureInfo.InvariantCulture) + " dp");
            Assert.That(ink.yMax, Is.LessThanOrEqualTo(top + Tol), what + ": la tinta no sube de la cara " + Describe(ink));
            Assert.That(ink.xMin, Is.GreaterThanOrEqualTo(-Tol), what + ": la tinta empieza dentro de la cara");
            Rect tag = DpRect(price, face);
            Assert.That(ink.xMax, Is.LessThanOrEqualTo(tag.xMin + Tol), what + ": la tinta " + Describe(ink) + " no toca el precio " + Describe(tag));
            Assert.That(ink.Overlaps(tag), Is.False, what + ": la etiqueta no se solapa con el precio");
            Debug.Log("AF_L4R " + what + ": «" + label.text + "» en " + label.textInfo.lineCount + " líneas a " + label.fontSize.ToString("0.0", CultureInfo.InvariantCulture)
                + " dp; botón de " + height.ToString("0.0", CultureInfo.InvariantCulture) + " dp");
            return height;
        }

        /// <summary>
        /// Una oferta de Aguas estancadas (Puñado o Balsa) con la escala de texto: sin desborde, a 12 sp (× escala) o más y con 6 dp o
        /// más entre la tinta de su etiqueta y el canto de arriba y de abajo de su cara visible, de <paramref name="faceDp"/> dp
        /// (revisión de L4).
        /// </summary>
        public static void AssertOfferAir(TextMeshProUGUI label, float faceDp, float scale, string what)
        {
            string problems = new LocInventory(Loc.Locale, scale).ProblemsOf(label);
            Assert.That(problems, Is.Empty, what + ": la etiqueta no se sale de su caja");
            Assert.That(label.fontSize, Is.GreaterThanOrEqualTo((12f * scale) - 0.01f), what + ": 12 sp (× escala) o más");
            // La cara visible ocupa los faceDp de arriba de la imagen de la cara (debajo van el canto y la sombra).
            var face = (RectTransform)label.rectTransform.parent;
            float top = face.rect.height;
            float h = faceDp;
            Rect ink = InkRect(label, face);
            float below = ink.yMin - (top - faceDp);
            float above = top - ink.yMax;
            Assert.That(below, Is.GreaterThanOrEqualTo(6f - Tol), what + ": aire bajo la tinta (" + below.ToString("0.0", CultureInfo.InvariantCulture) + " dp en una cara de " + h.ToString("0.0", CultureInfo.InvariantCulture) + ")");
            Assert.That(above, Is.GreaterThanOrEqualTo(6f - Tol), what + ": aire sobre la tinta (" + above.ToString("0.0", CultureInfo.InvariantCulture) + " dp en una cara de " + h.ToString("0.0", CultureInfo.InvariantCulture) + ")");
            Debug.Log("AF_L4R " + what + ": «" + label.text.Replace("\n", " / ") + "» a " + label.fontSize.ToString("0.0", CultureInfo.InvariantCulture) + " dp en una cara de "
                + h.ToString("0.0", CultureInfo.InvariantCulture) + " dp; tinta " + Describe(ink));
        }

        /// <summary>
        /// Ancho más grande que puede tomar cada ranura en esta pantalla (dp): el tope o lo que deja la pantalla, con la separación
        /// de siempre (16 dp de margen y 8 entre ranuras) o, con la bandeja apretada (MO2), la de <see cref="BoosterTray.CompactPadDp"/>
        /// y <see cref="BoosterTray.CompactGapDp"/>.
        /// </summary>
        public static float SlotCap(GameScreen view)
        {
            int n = view.Tray.SlotCount;
            float pad = view.Tray.Compact ? BoosterTray.CompactPadDp : 16f;
            float gap = view.Tray.Compact ? BoosterTray.CompactGapDp : 8f;
            float room = (view.CanvasWidth - (2f * BoosterTray.TrayEdgeDp) - (2f * pad) - ((n - 1) * gap)) / n;
            return Mathf.Max(64f, Mathf.Min(BoosterTray.SlotMaxDp, Mathf.Floor(room)));
        }

        /// <summary>Una ranura: líneas, tamaño, sin desborde, glifo de al menos el mínimo y por encima de la tinta de la etiqueta.</summary>
        public static void AssertSlot(GameScreen view, TextMeshProUGUI label, Image glyph, float scale, int lines, string what)
        {
            string problems = new LocInventory(Loc.Locale, scale).ProblemsOf(label);
            Assert.That(label.textInfo.lineCount, Is.EqualTo(lines), what + ": líneas de «" + label.text + "»");
            Assert.That(label.fontSize, Is.GreaterThanOrEqualTo((12f * scale) - 0.01f), what + ": 12 sp (× escala) o más");
            Assert.That(problems, Is.Empty, what + ": la etiqueta no se sale");
            AssertNoBrokenWord(label, what);

            Rect g = glyph.rectTransform.rect;
            Assert.That(g.width, Is.GreaterThanOrEqualTo(BoosterTray.LabelMinIconDp - 0.01f), what + ": glifo de " + g.width + " dp");

            // El borde de abajo del glifo, en el espacio de la etiqueta, por encima de la tinta de su primera línea.
            var corners = new Vector3[4];
            glyph.rectTransform.GetWorldCorners(corners);
            float glyphBottom = label.rectTransform.InverseTransformPoint(corners[0]).y;
            float inkTop = label.textBounds.max.y;
            Assert.That(glyphBottom, Is.GreaterThanOrEqualTo(inkTop - Tol), what + ": el glifo no pisa la etiqueta (" + glyphBottom.ToString("0.0", CultureInfo.InvariantCulture)
                + " dp sobre una tinta que llega a " + inkTop.ToString("0.0", CultureInfo.InvariantCulture) + ")");

            // La etiqueta dentro del ancho de su ranura, con aire a cada lado entre su tinta y el canto de la ranura (revisión de
            // L4: la cara es redondeada): BoosterTray.LabelSideInsetDp en la ranura ensanchada; en la de 64 dp, el de siempre.
            RectTransform faceRect = (RectTransform)label.rectTransform.parent;
            Rect face = faceRect.rect;
            Rect r = DpRect(label.rectTransform, faceRect);
            Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-Tol), what + ": la etiqueta empieza dentro de la ranura");
            Assert.That(r.xMax, Is.LessThanOrEqualTo(face.width + Tol), what + ": la etiqueta acaba dentro de la ranura");
            Rect ink = InkRect(label, faceRect);
            float air = view.Tray.SlotWidth > 64.01f ? view.Tray.LabelInsetNowDp : BoosterTray.LabelNarrowInsetDp;
            Assert.That(ink.xMin, Is.GreaterThanOrEqualTo(air - Tol), what + ": aire a la izquierda de la tinta (" + ink.xMin.ToString("0.0", CultureInfo.InvariantCulture) + " dp)");
            Assert.That(face.width - ink.xMax, Is.GreaterThanOrEqualTo(air - Tol), what + ": aire a la derecha de la tinta (" + (face.width - ink.xMax).ToString("0.0", CultureInfo.InvariantCulture) + " dp)");
        }

        /// <summary>Una ranura en una o dos líneas (las que salgan), con las comprobaciones de <see cref="AssertSlot"/>.</summary>
        public static void AssertSlotFits(GameScreen view, TextMeshProUGUI label, Image glyph, float scale, string what)
        {
            Canvas.ForceUpdateCanvases();
            label.ForceMeshUpdate(true, true);
            int lines = label.textInfo.lineCount;
            Assert.That(lines, Is.InRange(1, 2), what + ": líneas de «" + label.text + "»");
            AssertSlot(view, label, glyph, scale, lines, what);
        }

        /// <summary>Ninguna palabra partida por dentro: cada línea, salvo la última, acaba donde hay un espacio.</summary>
        public static void AssertNoBrokenWord(TMP_Text label, string what)
        {
            int line = LocInventory.BrokenWordLine(label);
            Assert.That(line, Is.EqualTo(0), what + ": «" + label.text + "» se parte por dentro de una palabra en la línea " + line);
        }

        /// <summary>La bandeja dentro de la pantalla, a <see cref="BoosterTray.TrayEdgeDp"/> de cada borde.</summary>
        public static void AssertTrayInside(GameScreen view)
        {
            Rect tray = view.Tray.BodyRect;
            Assert.That(tray.xMin, Is.GreaterThanOrEqualTo(BoosterTray.TrayEdgeDp - Tol), "la bandeja a TrayEdgeDp del borde izquierdo");
            Assert.That(tray.xMax, Is.LessThanOrEqualTo(view.CanvasWidth - BoosterTray.TrayEdgeDp + Tol), "la bandeja a TrayEdgeDp del borde derecho");
        }

        /// <summary>
        /// La píldora «Reiniciar»: líneas, tamaño y etiqueta sin desborde ni palabras partidas (tarea FX5); dentro de la pantalla; a
        /// <see cref="GameScreen.RestartPillGapDp"/> o más de la columna del saldo; sin tapar las cuias (con <paramref name="strict"/>,
        /// tampoco su sitio al subir) ni otros textos.
        /// </summary>
        public static void AssertRestart(GameScreen view, float scale, int lines, bool strict = true)
        {
            TextMeshProUGUI label = view.RestartLabel;
            string problems = new LocInventory(Loc.Locale, scale).ProblemsOf(label);
            Assert.That(label.textInfo.lineCount, Is.EqualTo(lines), "Reiniciar: líneas de «" + label.text + "»");
            Assert.That(label.fontSize, Is.GreaterThanOrEqualTo((12f * scale) - 0.01f), "Reiniciar: 12 sp (× escala) o más");
            Assert.That(problems, Is.Empty, "Reiniciar: la etiqueta no se sale");
            AssertNoBrokenWord(label, "Reiniciar"); // nunca, tampoco sin strict (tarea FX5)

            Rect pill = DpRect(view.RestartPillRect, view.Hud);
            Rect ink = InkRect(label, view.Hud);
            Assert.That(ink.xMin, Is.GreaterThanOrEqualTo(pill.xMin - Tol), "Reiniciar: la tinta empieza dentro de la píldora");
            Assert.That(ink.xMax, Is.LessThanOrEqualTo(pill.xMax + Tol), "Reiniciar: la tinta acaba dentro de la píldora");
            // A lo alto, como LocInventory: en una línea, la tinta puede asomar hasta un cuarto de su tamaño (la píldora de 16 dp
            // con la etiqueta de 12 sp, como en la guía 3.5); en dos, nada.
            float vTol = lines > 1 ? Tol : Mathf.Max(Tol, 0.25f * label.fontSize);
            Assert.That(ink.yMin, Is.GreaterThanOrEqualTo(pill.yMin - vTol), "Reiniciar: la tinta no baja de la píldora");
            Assert.That(ink.yMax, Is.LessThanOrEqualTo(pill.yMax + vTol), "Reiniciar: la tinta no sube de la píldora");
            Assert.That(pill.xMax, Is.LessThanOrEqualTo(view.CanvasWidth + Tol), "Reiniciar: la píldora no sale de la pantalla");

            WalletBar wallet = view.WalletBar;
            float walletRight = float.NegativeInfinity;
            if (wallet.Visible && wallet.SeedsRect.gameObject.activeInHierarchy)
            {
                walletRight = Mathf.Max(walletRight, DpRect(wallet.SeedsRect, view.Hud).xMax);
            }

            if (wallet.Visible && wallet.CanoesRect.gameObject.activeInHierarchy)
            {
                walletRight = Mathf.Max(walletRight, DpRect(wallet.CanoesRect, view.Hud).xMax);
            }

            Assert.That(walletRight, Is.GreaterThan(0f), "la columna del saldo se ve en el 100");
            Assert.That(pill.xMin, Is.GreaterThanOrEqualTo(walletRight + GameScreen.RestartPillGapDp - Tol), "Reiniciar: a RestartPillGapDp o más de la columna del saldo");

            foreach (CuiaView cuia in view.Views)
            {
                if (cuia == null || !cuia.isActiveAndEnabled)
                {
                    continue;
                }

                // Con strict, también el sitio al que sube la cuia al seleccionarla.
                Rect body = DpRect(cuia.Body, view.Hud);
                body.yMax += strict ? LiftDp : 0f;
                Assert.That(pill.Overlaps(body), Is.False, "Reiniciar: la píldora " + Describe(pill) + " tapa la cuia " + Describe(body) + (strict ? " (con lo que sube)" : string.Empty));
            }

            foreach (TMP_Text text in view.Canvas.GetComponentsInChildren<TMP_Text>(false))
            {
                if (text == label || string.IsNullOrWhiteSpace(text.text) || text.color.a < 0.01f || text.canvasRenderer.GetInheritedAlpha() < 0.01f)
                {
                    continue;
                }

                text.ForceMeshUpdate();
                if (text.textInfo.characterCount == 0)
                {
                    continue;
                }

                Rect other = InkRect(text, view.Hud);
                Assert.That(pill.Overlaps(other), Is.False, "Reiniciar: la píldora " + Describe(pill) + " tapa «" + text.text + "» " + Describe(other));
            }
        }

        /// <summary>Medidas de la píldora, la bandeja y la cuia más alta (líneas <c>AF_L4R</c>).</summary>
        public static void Log(GameScreen view, string name)
        {
            Rect pill = DpRect(view.RestartPillRect, view.Hud);
            float cuiaTop = float.NegativeInfinity;
            foreach (CuiaView cuia in view.Views)
            {
                if (cuia != null && cuia.isActiveAndEnabled)
                {
                    cuiaTop = Mathf.Max(cuiaTop, DpRect(cuia.Body, view.Hud).yMax);
                }
            }

            Debug.Log("AF_L4R " + name + ": píldora " + Describe(pill) + " (" + view.RestartLabel.textInfo.lineCount + " líneas); cuia más alta hasta y = "
                + cuiaTop.ToString("0.0", CultureInfo.InvariantCulture) + "; ranura " + view.Tray.SlotWidth.ToString("0.#", CultureInfo.InvariantCulture)
                + " dp; bandeja " + Describe(DpRect(view.Tray.TrayRect, view.Hud)));
        }

        // Rectángulo de rt en dp en el espacio de space (x desde su borde izquierdo, y desde su borde de abajo).
        internal static Rect DpRect(RectTransform rt, RectTransform space)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return FromCorners(corners, space);
        }

        // Tinta de un texto (textBounds) en dp en el espacio de space.
        internal static Rect InkRect(TMP_Text text, RectTransform space)
        {
            Bounds b = text.textBounds;
            var corners = new[]
            {
                text.transform.TransformPoint(new Vector3(b.min.x, b.min.y, 0f)),
                text.transform.TransformPoint(new Vector3(b.min.x, b.max.y, 0f)),
                text.transform.TransformPoint(new Vector3(b.max.x, b.max.y, 0f)),
                text.transform.TransformPoint(new Vector3(b.max.x, b.min.y, 0f)),
            };
            return FromCorners(corners, space);
        }

        private static Rect FromCorners(Vector3[] corners, RectTransform space)
        {
            Rect r = space.rect;
            Vector3 a = space.InverseTransformPoint(corners[0]);
            Vector3 c = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x) - r.xMin, Mathf.Min(a.y, c.y) - r.yMin, Mathf.Max(a.x, c.x) - r.xMin, Mathf.Max(a.y, c.y) - r.yMin);
        }

        private static string Describe(Rect r)
        {
            return "[x " + r.xMin.ToString("0.0", CultureInfo.InvariantCulture) + "-" + r.xMax.ToString("0.0", CultureInfo.InvariantCulture)
                + ", y " + r.yMin.ToString("0.0", CultureInfo.InvariantCulture) + "-" + r.yMax.ToString("0.0", CultureInfo.InvariantCulture) + "]";
        }
    }
}
