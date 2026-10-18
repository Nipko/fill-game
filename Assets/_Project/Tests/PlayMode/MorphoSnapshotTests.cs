#nullable enable
using System.Collections;
using System.IO;
using AF.Ads;
using AF.Boosters;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de Morpho · Pista (tarea MO2; <c>[Explicit]</c>, no forman parte de la suite), en
    /// <c>Logs/snapshots/es-419/mo2_*</c>, con un recorte a tamaño real (<c>*_zoom</c>) de cada una:
    /// <c>mo2_01_tray_l15</c> (3 ranuras: Marea, Cuia extra y Pista con su precio), <c>mo2_02_tray_l100</c> (4 ranuras, Revelar
    /// la última), <c>mo2_02b_tray_l100_130</c> (las 4 ranuras con la fuente al 130 % a 360 dp: la bandeja apretada),
    /// <c>mo2_03_tray_missing_l106</c> (tres «Te faltan {n}» a la vez, sin pisarse), <c>mo2_04_slot48</c> (la ranura a 48 dp: el
    /// glifo a 24 dp), <c>mo2_05_flight_l15</c> (la mariposa a mitad del vuelo al origen), <c>mo2_06_hop_l15</c> (el salto al
    /// destino con la estela), <c>mo2_07_rings_l15</c> (las dos cuias con su anillo), <c>mo2_07b_rings_reduced_motion_l15</c>
    /// (con Reducir movimiento, el primer fotograma tras la respuesta: la mariposa ya posada y los dos anillos),
    /// <c>mo2_08_card_*</c> (las cuatro respuestas sin vertido tras un vertido, con Deshacer habilitado, también al 130 %, y
    /// <c>mo2_08_card_no_exit_disabled_100</c> sin vertidos: Deshacer gris) y <c>mo2_09_unsupported_l15</c> (el globo de la ranura
    /// gris). Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.MorphoSnapshotTests</c>
    /// sin <c>-nographics</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class MorphoSnapshotTests
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
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureTray()
        {
            string dir = SnapshotKit.Prepare();
            yield return Load(15, FunnelSetup.Profile(500));
            GameBootstrap boot = GameBootstrap.Current!;
            BoosterTray tray = boot.View!.Tray;
            SnapshotKit.Save(dir, "mo2_01_tray_l15");
            SaveCrop(dir, "mo2_01_tray_l15_zoom", tray.TrayRect, 10f, 40f);

            var hint = (RectTransform)tray.HintButton.transform;
            hint.localScale = new Vector3(0.75f, 0.75f, 1f);
            SaveCrop(dir, "mo2_04_slot48_zoom", hint, 12f, 12f);
            hint.localScale = Vector3.one;

            yield return Load(100, FunnelSetup.Profile(500));
            boot = GameBootstrap.Current!;
            SnapshotKit.Save(dir, "mo2_02_tray_l100");
            SaveCrop(dir, "mo2_02_tray_l100_zoom", boot.View!.Tray.TrayRect, 10f, 40f);

            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            yield return Load(106, FunnelSetup.Profile(50));
            boot = GameBootstrap.Current!;
            SnapshotKit.Save(dir, "mo2_03_tray_missing_l106");
            SaveCrop(dir, "mo2_03_tray_missing_l106_zoom", boot.View!.Tray.TrayRect, 10f, 50f);
            Assert.That(File.Exists(Path.Combine(dir, "mo2_03_tray_missing_l106.png")), Is.True);

            // La bandeja apretada (revisión de MO2): las 4 ranuras con la fuente al 130 % a 360 dp. La última: la pantalla
            // simulada cambia la escala del lienzo de las capturas que vienen detrás.
            LaunchOptions.FontScaleOverride = 1.3f;
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.AdFillOverride = null;
            yield return Load(100, FunnelSetup.Profile(500));
            boot = GameBootstrap.Current!;
            Assert.That(boot.View!.Tray.Compact, Is.True, "la bandeja apretada");
            SnapshotKit.Save(dir, "mo2_02b_tray_l100_130");
            SaveCrop(dir, "mo2_02b_tray_l100_130_zoom", boot.View.Tray.TrayRect, 10f, 40f);
        }

        [UnityTest]
        public IEnumerator CaptureFlightAndRings()
        {
            string dir = SnapshotKit.Prepare();
            yield return Load(15, FunnelSetup.Profile(500));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            MorphoFx fx = view.MorphoFx;
            Assert.That(boot.PressHint(), Is.True);
            while (boot.Flow!.HintThinking)
            {
                PlayModeHelpers.TickExact(boot, 2);
                yield return null;
            }

            Assert.That(fx.Flying, Is.True);
            PlayModeHelpers.TickExact(boot, (int)(MorphoFx.FlightMs * 0.5f));
            SnapshotKit.Save(dir, "mo2_05_flight_l15");
            SaveCrop(dir, "mo2_05_flight_l15_zoom", fx.Butterfly, 60f, 60f);

            PlayModeHelpers.TickExact(boot, (int)((MorphoFx.FlightMs * 0.5f) + MorphoFx.PerchMs + (MorphoFx.HopMs * 0.5f)));
            Assert.That(fx.Hopping, Is.True);
            SnapshotKit.Save(dir, "mo2_06_hop_l15");
            SaveCrop(dir, "mo2_06_hop_l15_zoom", fx.Butterfly, 90f, 70f);

            PlayModeHelpers.TickExact(boot, (int)(MorphoFx.HopMs * 0.5f) + (int)MorphoFx.RingPeriodMs);
            Assert.That(fx.OnTarget, Is.True);
            SnapshotKit.Save(dir, "mo2_07_rings_l15");
            SaveCrop(dir, "mo2_07_rings_l15_zoom", fx.TargetRing.rectTransform, 80f, 30f);
            SaveCrop(dir, "mo2_07_rings_source_l15_zoom", fx.SourceRing.rectTransform, 80f, 30f);
        }

        [UnityTest]
        public IEnumerator CaptureReducedMotion()
        {
            // Con Reducir movimiento (07 §7.8), el primer fotograma tras la respuesta: sin vuelo, la mariposa aparece posada en el
            // destino con un fundido corto y los dos anillos se encienden a la vez, sin latir.
            string dir = SnapshotKit.Prepare();
            yield return Load(15, FunnelSetup.Profile(500));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            MorphoFx fx = view.MorphoFx;
            boot.Flow!.ReduceMotion = true;
            Assert.That(boot.PressHint(), Is.True);
            while (boot.Flow.HintThinking)
            {
                PlayModeHelpers.TickExact(boot, 2);
                yield return null;
            }

            Assert.That(fx.OnTarget && fx.SourceRinging && fx.TargetRinging, Is.True);
            PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
            SnapshotKit.Save(dir, "mo2_07b_rings_reduced_motion_l15");
            SaveCrop(dir, "mo2_07b_rings_reduced_motion_l15_zoom", fx.TargetRing.rectTransform, 80f, 30f);
        }

        [UnityTest]
        public IEnumerator CaptureAnswerCards([Values(100, 130)] int percent)
        {
            string dir = SnapshotKit.Prepare();
            LaunchOptions.FontScaleOverride = percent / 100f;
            yield return Load(15, FunnelSetup.Profile(500));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            if (percent == 100)
            {
                // Sin vertidos, Deshacer va gris: una sola captura de ese estado (revisión de MO2).
                view.OnHint(new HintResult(HintAnswer.NoExit, default, false, HintSolver.SafeMoves, 2, 0, 0, false, false));
                PlayModeHelpers.Step(boot, 50);
                yield return SnapshotKit.Wait(0.1f);
                Assert.That(view.HintAnswerCard.UndoEnabled, Is.False);
                SnapshotKit.Save(dir, "mo2_08_card_no_exit_disabled_100_l15");
                SaveCrop(dir, "mo2_08_card_no_exit_disabled_100_l15_zoom", view.HintAnswerCard.Panel, 10f, 10f);
                view.HintAnswerCard.CloseButton.onClick.Invoke();
            }

            // El caso real: Morpho responde sin vertido después de jugar, con Deshacer habilitado (relieve azul).
            var first = MorphoUiTests.OtherLegalPour(boot.Flow!.Session.Current, default);
            boot.TapCuia(first.Source);
            boot.TapCuia(first.Target);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(boot.Flow.Session.AppliedPours, Is.EqualTo(1));
            HintAnswer[] answers = { HintAnswer.NoExit, HintAnswer.NoSafePour, HintAnswer.NoSafePour, HintAnswer.NotFound };
            bool[] tinaja = { false, false, true, false };
            string[] names = { "no_exit", "no_safe_pour_cuia", "no_safe_pour_tinaja", "not_found" };
            for (int i = 0; i < answers.Length; i++)
            {
                view.OnHint(new HintResult(answers[i], default, tinaja[i], HintSolver.SafeMoves, 2, 0, 0, false, false));
                PlayModeHelpers.Step(boot, 50);
                yield return SnapshotKit.Wait(0.1f);
                Assert.That(view.HintAnswerCard.UndoEnabled, Is.True, names[i]);
                string name = "mo2_08_card_" + names[i] + "_" + percent + "_l15";
                SnapshotKit.Save(dir, name);
                SaveCrop(dir, name + "_zoom", view.HintAnswerCard.Panel, 10f, 10f);
            }
        }

        [UnityTest]
        public IEnumerator CaptureUnsupported()
        {
            string dir = SnapshotKit.Prepare();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGameWithPack(MorphoUiTests.UnsupportedPack(15), 15, new MemoryProgressStore(15, 14));
            yield return Ready(GameBootstrap.Current!);
            GameBootstrap boot = GameBootstrap.Current!;
            BoosterTray tray = boot.View!.Tray;
            tray.HintButton.onClick.Invoke();
            Assert.That(tray.HintTooltipVisible, Is.True);
            SnapshotKit.Save(dir, "mo2_09_unsupported_l15");
            SaveCrop(dir, "mo2_09_unsupported_l15_zoom", tray.TrayRect, 20f, 70f);
        }

        // Carga el nivel con el perfil, une la cámara de las capturas y avanza hasta Playing con la vista quieta.
        private static IEnumerator Load(int level, Profile profile)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            yield return Ready(GameBootstrap.Current!);
        }

        private static IEnumerator Ready(GameBootstrap boot)
        {
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 300);
            yield return null;
        }

        // Recorte a tamaño real (3 px/dp) alrededor de target, con marginX y marginY dp de margen.
        private static void SaveCrop(string dir, string name, RectTransform target, float marginX, float marginY)
        {
            Camera cam = SnapshotKit.CaptureCamera!;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture rt = cam.targetTexture;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = cam.WorldToScreenPoint(corners[i]);
                xMin = Mathf.Min(xMin, p.x);
                yMin = Mathf.Min(yMin, p.y);
                xMax = Mathf.Max(xMax, p.x);
                yMax = Mathf.Max(yMax, p.y);
            }

            int x0 = Mathf.Clamp(Mathf.FloorToInt(xMin - (marginX * 3f)), 0, rt.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(yMin - (marginY * 3f)), 0, rt.height - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(xMax + (marginX * 3f)), x0 + 1, rt.width);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(yMax + (marginY * 3f)), y0 + 1, rt.height);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(x1 - x0, y1 - y0, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(x0, y0, x1 - x0, y1 - y0), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }
    }
}
