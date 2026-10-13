#nullable enable
using System.Collections;
using System.Diagnostics;
using AF.Ads;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de la tarea T52 (arreglos de la prueba de la tanda 2 en el teléfono; <c>[Explicit]</c>, no forman
    /// parte de la suite), en el rango 90-99:
    /// <list type="bullet">
    /// <item><description><c>90_banner_l41_130</c>, <c>90b_banner_l41_100</c>, <c>90c_banner_l41_130_16x9</c> y
    /// <c>90d_banner_l141_130</c>: el banner de mundo con la escala de fuente de 02 §12 (pregunta 43);</description></item>
    /// <item><description><c>92_l135_aviso_burbujas_despues</c> (3 filas, con las dos burbujas de dos líneas: el compromiso),
    /// <c>92b_l135_aviso_sin_burbujas</c> (encogido sobre la bandeja, sin tapar la repisa ni la ficha «?×n»),
    /// <c>93_1fila_aviso_burbuja_despues</c> y <c>93b_l31_aviso_burbuja_despues</c> (observación 1); las de antes del cambio,
    /// con el sufijo <c>_antes</c>, se sacaron con el código de <c>f64c19a</c>;</description></item>
    /// <item><description><c>94_remolino_l103_chorro</c> y <c>95_remolino_l140_chorro</c> (la cuia tocada se traga el chorro)
    /// y <c>94b_remolino_l103_recibe</c> y <c>95b_remolino_l140_recibe</c> (la pareja se ilumina al recibir; observación
    /// 5).</description></item>
    /// </list>
    /// Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.PhoneFixesSnapshotTests</c>
    /// sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class PhoneFixesSnapshotTests
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
        public IEnumerator CaptureToastAndBubbles()
        {
            string dir = SnapshotKit.Prepare();

            // El 135 (3 filas) sin semillas ni video: «Te faltan 150» de «+1 cuia» y «Te faltan 120» de Revelar, y el aviso.
            yield return LoadWithBubbles(135, 0, 0);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.View!.ShowRevealMissing(120);
            PlayModeHelpers.Step(boot, 400);
            yield return SaveNow(dir, "92_l135_aviso_burbujas_despues", boot);

            // El 135 con semillas: sin burbujas, el aviso encogido sobre la bandeja, sin tapar la repisa ni la ficha «?×n».
            yield return LoadWithBubbles(135, 0, 1000);
            boot = GameBootstrap.Current!;
            boot.View!.ShowRevealMissing(120);
            PlayModeHelpers.Step(boot, 400);
            yield return SaveNow(dir, "92b_l135_aviso_sin_burbujas", boot);

            // 1 fila: el tablero del 6 (5 cuias) como nivel 46, sin semillas: «Te faltan 150» de «+1 cuia».
            yield return LoadWithBubbles(46, 40, 0);
            boot = GameBootstrap.Current!;
            boot.View!.ShowRevealMissing(70);
            PlayModeHelpers.Step(boot, 400);
            yield return SaveNow(dir, "93_1fila_aviso_burbuja_despues", boot);

            // 2 filas: el 31 (8 cuias).
            yield return LoadWithBubbles(31, 0, 0);
            boot = GameBootstrap.Current!;
            boot.View!.ShowRevealMissing(70);
            PlayModeHelpers.Step(boot, 400);
            yield return SaveNow(dir, "93b_l31_aviso_burbuja_despues", boot);
        }

        [UnityTest]
        public IEnumerator CaptureBannerFontScale()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureBanner(dir, 41, 1f, "90b_banner_l41_100");
            yield return CaptureBanner(dir, 41, 1.3f, "90_banner_l41_130");
            yield return CaptureBanner(dir, 141, 1.3f, "90d_banner_l141_130");
            GameScreen.SimulatedScreen = HudLabelTests.Screen640; // 16:9 (1080 × 1920 px): 438,75 × 780 dp
            yield return CaptureBanner(dir, 41, 1.3f, "90c_banner_l41_130_16x9");
        }

        [UnityTest]
        public IEnumerator CaptureRemolinoPours()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureRemolino(dir, 103, "94");
            yield return CaptureRemolino(dir, 140, "95");
        }

        [UnityTest]
        public IEnumerator DiagnoseBannerWaitAfterVictory()
        {
            // Diagnóstico de la observación 4 (AF_T52 en el log): lo que cuesta pintar la superficie de madera de 328 × 281 dp en
            // el hilo principal y el wait_ms del banner del 41 al entrar con «Siguiente» tras 1,5 s de victoria del 40. Con el
            // código de f64c19a: 221,9 ms y wait_ms 234 (la superficie, pintada al abrirse el banner); con T52, ≈ 20 ms (veta
            // más rápida, los mismos píxeles) y wait_ms ≈ 0 (la pinta el trabajo del arte, en otro hilo).
            var art = new ProceduralArt("white");
            var watch = Stopwatch.StartNew();
            art.ModalSurfaceFor(328f, 281f);
            watch.Stop();
            Debug.Log("AF_T52 modal_surface_328x281_ms " + watch.Elapsed.TotalMilliseconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
            art.Dispose();

            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtTimeoutMsOverride = 60000f;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(40, new MemoryProgressStore(40, 39));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.View!.CardArt.ReleaseBannerArt(); // el arte es compartido entre escenas: nada del banner de otra captura
            if (boot.Flow!.Phase == FlowPhase.LevelCard)
            {
                boot.TapAnywhere();
            }

            VictorySetup.WinToVictoryScreen(boot);
            Debug.Log("AF_T52 pending_at_victory " + boot.View!.CardArt.BannerArtPending);
            yield return SnapshotKit.Wait(1.5f);
            Debug.Log("AF_T52 pending_after_wait " + boot.View.CardArt.BannerArtPending);
            Assert.That(boot.PressNext(), Is.True);
            for (int ms = 0; ms < 5000 && boot.Flow.LevelNumber != 41; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            for (int ms = 0; ms < 2000 && boot.View.LastWorldBannerArtProbe == null; ms += PlayModeHelpers.FrameMs)
            {
                yield return null;
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Debug.Log("AF_T52 banner_art_after_next " + boot.View.LastWorldBannerArtProbe);
            TestContext.WriteLine("AF_T52 banner_art_after_next " + boot.View.LastWorldBannerArtProbe);
        }

        // El banner del nivel level, con la escala de fuente scale, ya quieto (Capi presentando y Rosa fuera).
        private static IEnumerator CaptureBanner(string dir, int level, float scale, string name)
        {
            yield return CardSetup.Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.WorldBannerView.Visible, Is.True, "nivel " + level);
            view.BannerFontScale = scale;
            PlayModeHelpers.TickExact(boot, 1100);
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_T52 captura " + name + ": título «" + view.WorldBannerView.TitleText + "», lugar «" + view.WorldBannerView.PlaceText + "», "
                + view.WorldBannerView.Panel.rect.height.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " dp de alto");
        }

        private static IEnumerator CaptureRemolino(string dir, int level, string prefix)
        {
            yield return MechanicTestKit.LoadPlaying(level);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.ThroughRemolino);
            Assert.That(path, Is.Not.Null, "vertido por el Remolino en el " + level);
            MechanicTestKit.PlayPrefix(boot, path!);
            GameScreen view = boot.View!;
            PourTimings timings = view.Timings;
            TapResult poured = MechanicTestKit.Tap(boot, path![path.Length - 1]);
            Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured));
            int partner = poured.Pour.Receiver;
            float mid = (timings.TravelMs * BoardAnimator.StreamStartFraction) + BoardAnimator.StreamGrowMs + (0.5f * timings.DoseMs);
            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt(mid) + PlayModeHelpers.FrameMs);
            yield return SaveNow(dir, prefix + "_remolino_l" + level + "_chorro", boot);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Receive, partner, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Receive, partner, 0.55f);
            yield return SaveNow(dir, prefix + "b_remolino_l" + level + "_recibe", boot);
            MechanicTestKit.StepToIdle(boot, 4000);
        }

        // Nivel level (con el desplazamiento offset de LaunchOptions.LevelOffsetOverride, si no es 0) con seeds semillas, sin
        // video ni Luciérnagas y en juego con la vista quieta.
        private static IEnumerator LoadWithBubbles(int level, int offset, int seeds)
        {
            LaunchOptions.Reset();
            FunnelSetup.Hooks();
            if (offset != 0)
            {
                LaunchOptions.LevelOffsetOverride = offset;
            }

            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
        }

        private static IEnumerator SaveNow(string dir, string name, GameBootstrap boot)
        {
            SnapshotKit.Attach(boot);
            yield return null;
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_T52 captura " + name);
        }
    }
}
