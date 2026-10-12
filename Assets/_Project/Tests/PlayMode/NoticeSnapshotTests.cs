#nullable enable
using System.Collections;
using System.IO;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de los avisos legibles (D-011, puntos 2 y 3; <c>[Explicit]</c>, no forman parte de la suite):
    /// <c>a2_01_capi_gift_l8</c> (el regalo de Capi al habilitarse el toque, con la tropa aún en la rama),
    /// <c>a2_02_sighting_l5</c>, <c>a2_02b_sighting_l42</c> y <c>a2_02c_sighting_l142</c> (la tarjeta «¡Miquito a la vista!»
    /// cuando la tropa ya se ha ido, en la franja del panorama; en el 42 tras la intro corta y en el 142 con sus 10 cuias en 2
    /// filas), <c>a2_02d_sighting_rows3_11</c> y <c>a2_02e_sighting_rows3_16</c> (un tablero de prueba de 3 filas en el 142, 11
    /// y 16 cuias: la franja mide &lt; 64 dp y la tarjeta va sobre el muelle o, si no cabe, no sale; el sufijo de la captura dice
    /// cuál) y <c>a2_03_toast_after_card_l5</c> (el aviso que esperaba a que se fuera la tarjeta). Sirven para mirar a 48 dp
    /// la legibilidad, los solapes con la tropa, las cuias, el HUD y la bandeja, y la línea de D-003. Necesitan gráficos:
    /// <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.NoticeSnapshotTests</c> sin <c>-nographics</c>; los
    /// PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class NoticeSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureNotices()
        {
            string dir = SnapshotKit.Prepare();

            // El regalo de Capi en el 8: sale al habilitarse el toque, abajo, mientras la tropa reposa arriba.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(8, new MemoryProgressStore(8, 7));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            StepToPlaying(boot);
            PlayModeHelpers.Step(boot, 800);
            Assert.That(boot.View!.ToastText, Is.EqualTo(UiText.ExtraCuiaGift));
            SnapshotKit.Save(dir, "a2_01_capi_gift_l8");

            // La tarjeta del miquito, cuando la tropa se ha ido, en la franja del panorama (larga en el 5, corta en el 42 y el 142
            // con 10 cuias en 2 filas).
            yield return CaptureSighting(dir, 5, "a2_02_sighting_l5");
            boot = GameBootstrap.Current!;

            // Un aviso que llega con la tarjeta a la vista sale cuando ella se va.
            boot.View!.ShowRevealMissing(70);
            Assert.That(boot.View.ToastText, Is.Null);
            for (int t = 0; t < 8000 && boot.View.Sighting.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 400);
            Assert.That(boot.View.ToastText, Is.EqualTo(UiText.RevealMissing(70)));
            SnapshotKit.Save(dir, "a2_03_toast_after_card_l5");

            yield return CaptureSighting(dir, 42, "a2_02b_sighting_l42");
            yield return CaptureSighting(dir, 142, "a2_02c_sighting_l142");
            Assert.That(File.Exists(Path.Combine(dir, "a2_02c_sighting_l142.png")), Is.True);

            // Con 3 filas (D-006: desde 11 cuias) la franja del panorama mide < 64 dp: la tarjeta va sobre el muelle, bajo la
            // última fila y por encima de la bandeja, o no sale si no cabe. Ningún nivel_tarjeta del catálogo tiene hoy 3 filas,
            // así que se mira con un tablero de prueba en el 142 (mundo 5). A 360 × 780 dp (2026-10-12) no cabe en ninguno de
            // los dos: el panel acaba en 628,5 dp con 11 cuias y en 596,1 dp con 16, y la tarjeta (60 dp, 4 dp bajo el panel)
            // tiene que acabar ≤ 8 dp sobre la bandeja (660 dp).
            yield return CaptureSightingOnThreeRows(dir, 11, "a2_02d_sighting_rows3_11");
            yield return CaptureSightingOnThreeRows(dir, 16, "a2_02e_sighting_rows3_16");
            LaunchOptions.LevelPackJsonOverride = null;
        }

        // El 142 con un tablero de prueba de 3 filas: espera a que la tropa se vaya y captura la tarjeta sobre el muelle
        // («_dock») o el tablero sin ella si no cabe («_nofit»).
        private static IEnumerator CaptureSightingOnThreeRows(string dir, int cuias, string name)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGameWithPack(ThreeRowPack(cuias), 142, new MemoryProgressStore(142, 141));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.Flow!.Load(142);
            StepToPlaying(boot);
            GameScreen view = boot.View!;
            Assert.That(view.Views.Length, Is.EqualTo(cuias), name);
            for (int t = 0; t < 5000 && view.Intro.TroopVisible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 3 * PlayModeHelpers.FrameMs);
            float hudBottom = view.Intro.Input.HudBottom;
            float panelTop = view.Intro.Input.PanelTop;
            float panelBottom = view.Intro.Input.PanelBottom;
            Assert.That(panelTop - hudBottom, Is.LessThan(64f), name + ": con 3 filas la franja mide < 64 dp");
            Assert.That(view.SightingPending, Is.False, name + ": la tropa ya se fue");
            string suffix;
            if (view.Sighting.Visible)
            {
                RectTransform root = view.Sighting.Root;
                float top = -root.anchoredPosition.y;
                Assert.That(top, Is.GreaterThanOrEqualTo(panelBottom), name + ": sobre el muelle, bajo la última fila");
                PlayModeHelpers.Step(boot, 400);
                suffix = "_dock";
            }
            else
            {
                Assert.That(view.NoticesQueued, Is.EqualTo(0), name + ": si no cabe, no sale ni espera");
                suffix = "_nofit";
            }

            SnapshotKit.Save(dir, name + suffix);
            SnapshotKit.LogBoard(name, boot);
            TestContext.WriteLine(name + suffix + ": franja " + (panelTop - hudBottom) + " dp, fondo del panel " + panelBottom + " dp, tarjeta "
                + (view.Sighting.Visible ? (-view.Sighting.Root.anchoredPosition.y).ToString(System.Globalization.CultureInfo.InvariantCulture) : "no"));
        }

        // Paquete de un nivel (el 142, mundo 5) con <cuias> cuias: cuias − 2 tintes de 4 dosis en escalera (la cuia i lleva los
        // tintes i … i + 3) y 2 vacías.
        private static string ThreeRowPack(int cuias)
        {
            string[] all = { "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo", "agua_tapajos", "lulo", "araza", "cacao", "agua_negro" };
            int tints = cuias - 2;
            var ids = new string[tints];
            System.Array.Copy(all, ids, tints);
            var board = new int[cuias][];
            for (int i = 0; i < cuias; i++)
            {
                board[i] = i < tints ? new[] { (i % tints) + 1, ((i + 1) % tints) + 1, ((i + 2) % tints) + 1, ((i + 3) % tints) + 1 } : new int[0];
            }

            return PlayModeHelpers.BoardPack(5, "w05_loreto", "white", 142, ids, board);
        }

        // Carga el nivel, deja la intro entera, espera a que la tropa se vaya y captura la tarjeta ya entrada (400 ms).
        private static IEnumerator CaptureSighting(string dir, int level, string name)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.Flow!.Load(level);
            StepToPlaying(boot);
            for (int t = 0; t < 5000 && !boot.View!.Sighting.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.View!.Sighting.Visible, Is.True, name);
            Assert.That(boot.View.Intro.TroopVisible, Is.False, name + ": la tropa ya se fue");
            PlayModeHelpers.Step(boot, 400);
            SnapshotKit.Save(dir, name);
            TestContext.WriteLine(name + ": " + boot.View.Sighting.Name + " · " + boot.View.Sighting.Fact + " · " + boot.View.Sighting.DurationMs + " ms");
        }

        private static void StepToPlaying(GameBootstrap boot)
        {
            for (int t = 0; t < 5000 && boot.Flow!.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }
    }
}
