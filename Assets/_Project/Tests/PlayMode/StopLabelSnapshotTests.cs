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
    /// Capturas del rótulo del lugar (tarea M2.3; D-013, punto 8; propuesta del Mapa §10.5, punto 5): <c>18_stop_label_l31</c>
    /// (el 31, 2 filas: «Floresta Nacional do Tapajós · Brasil» sobre el horizonte, en la franja del panorama, con la silueta
    /// provisional de la parada) y <c>18b_stop_label_3filas</c> (el 131, 11 cuias en 3 filas: «El río frontera · Colombia y
    /// Perú» como placa bajo el HUD, con aire sobre las cuias de la fila 1), con el rótulo ya entrado. Sirven para mirar la
    /// legibilidad (crema con contorno, ≥ 4,5:1), los solapes con el HUD, el héroe y las cuias, y la línea de D-003. Cada captura
    /// escribe en el log su línea <c>AF_STOP_LABEL</c> con la sonda. Explícito: necesita gráficos (<c>unity test &lt;repo&gt;
    /// --mode PlayMode --filter AF.Tests.PlayMode.StopLabelSnapshotTests</c> sin <c>-nographics</c>); los PNG quedan en
    /// <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas del rótulo del lugar (M2.3); necesitan dispositivo gráfico.")]
    public sealed class StopLabelSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.StopLabelsOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.StopLabelsOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureStopLabels()
        {
            string dir = SnapshotKit.Prepare();
            yield return Capture(dir, 31, "18_stop_label_l31", false);
            yield return Capture(dir, 131, "18b_stop_label_3filas", true);
            Assert.That(File.Exists(Path.Combine(dir, "18b_stop_label_3filas.png")), Is.True);
        }

        private static IEnumerator Capture(string dir, int level, string name, bool plate)
        {
            GameScreen.SimulatedScreen = new Vector4(SnapshotKit.Width, SnapshotKit.Height, 96f, 48f);
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            GameScreen view = boot.View!;
            for (int t = 0; t < 8000 && !view.StopLabelView.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.StopLabelView.Visible, Is.True, name);
            Assert.That(view.StopLabelView.OnPlate, Is.EqualTo(plate), name);
            PlayModeHelpers.Step(boot, 400); // la entrada de 150 ms y las etiquetas del HUD ya apartadas
            Assert.That(view.StopLabelView.Alpha, Is.EqualTo(1f), name);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
            Rect r = view.StopLabelView.Rect;
            Debug.Log("AF_STOP_LABEL " + name + ": " + r.ToString("0.0") + ", horizonte "
                + view.HorizonY.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " dp, HUD "
                + view.HudBottomDp.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " dp, cuias de la fila 1 "
                + (view.Layout!.BlockTop + BoardLayout.Lift).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " dp, "
                + view.StopLabelView.Label.fontSize.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " dp de texto, "
                + view.LastStopLabelProbe);
        }
    }
}
