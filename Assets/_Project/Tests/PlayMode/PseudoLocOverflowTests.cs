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
using UnityEngine;
using UnityEngine.TestTools;

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

            // Bandeja de comodines del 100 en juego, sin semillas: Marea, «+1 cuia» con su precio y «Te faltan {n}», y Revelar
            // con la Luciérnaga regalada (hallazgo 2 de la prueba del 2026-10-14).
            LaunchOptions.LevelCardsOverride = false;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(100, new MemoryProgressStore(100, 99));
            boot = GameBootstrap.Current!;
            boot.TapAnywhere();
            GameBootstrap hud = boot;
            yield return PlayModeHelpers.WaitFor(() => hud.Flow!.Phase == FlowPhase.Playing && hud.View!.IsViewIdle, "el 100 en juego");
            yield return SnapshotKit.Wait(0.5f);
            Assert.That(boot.View!.Tray.SlotCount, Is.EqualTo(3), "Marea, «+1 cuia» y Revelar");
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

        private static IEnumerator LoadFunnel(int level, int seeds)
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.LevelCardsOverride = false;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds, 3));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
        }

        // Tablero 6: 3 → 4 deja Aguas estancadas; espera al modal y a que el panel termine de subir.
        private static IEnumerator StuckRealTime(GameBootstrap boot)
        {
            boot.TapAnywhere();
            boot.TapCuia(3);
            boot.TapCuia(4);
            LevelFlow flow = boot.Flow!;
            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.StuckModal, "modal");
            yield return SnapshotKit.Wait(0.45f);
        }

        // Victoria con la solución del solver, en tiempo real.
        private static IEnumerator WinRealTime(GameBootstrap boot)
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
}
