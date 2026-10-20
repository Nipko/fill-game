#nullable enable
using System.Collections;
using System.Globalization;
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
    /// (el 31, 2 filas: «Floresta Nacional do Tapajós · Brasil» como placa clavada al marco, bajo el héroe de la parada; D-030,
    /// pregunta 9 (b); tarea DC24), <c>18i_stop_label_1fila</c> (lo mismo con 1 fila: el 31 con un paquete de prueba de 5 cuias)
    /// y <c>18b_stop_label_3filas</c> (el 131, 11 cuias en 3 filas: «El río frontera · Colombia y
    /// Perú» como placa bajo el HUD, con aire sobre las cuias de la fila 1), con el rótulo ya entrado; y, de la tarea FX4
    /// (observación 3 de la prueba de la ola 2 en el teléfono: la placa y la cuia elevada se rozaban),
    /// <c>18c_stop_label_l171</c> (el 171: la Hoja de la cuia 1 no deja sitio sobre toda la fila 1, así que la placa va a la
    /// misma altura, de lado, sobre las cuias 2 y 3; revisión de FX4), <c>18d_stop_label_l171_elevada</c> (lo mismo con la cuia 0
    /// elevada, que no está bajo la placa), <c>18e_stop_label_l191</c> (el 191, clavada al marco sobre la fila 1 en reposo,
    /// corchos incluidos), <c>18f_stop_label_l171_muelle</c> (el 171 solo con el muelle, la placa centrada en su hueco: el sitio
    /// de reserva cuando ni de lado cabe) y, de su revisión final, <c>18g_stop_label_l131_elevada</c> y
    /// <c>18h_stop_label_l191_elevada</c> (la cuia 0 elevada, con su corcho, y la placa ya ida, sin restos de ella ni de su
    /// contorno: el caso del roce en el teléfono). Sirven para mirar la
    /// legibilidad (crema con contorno, ≥ 4,5:1), los solapes con el HUD, el héroe y las cuias, y la línea de D-003. Cada captura
    /// escribe en el log su línea <c>AF_STOP_LABEL</c> con la sonda y lo más alto de la fila 1 como lo mide FX4 (y, de lado,
    /// el tramo libre). Explícito: necesita gráficos (<c>unity test &lt;repo&gt;
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
            GameScreen.StopLabelSpotsOverride = GameScreen.StopLabelSpot.All;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureStopLabels()
        {
            string dir = SnapshotKit.Prepare();
            yield return Capture(dir, 31, "18_stop_label_l31", "plate");
            LaunchOptions.LevelPackJsonOverride = StopLabelTests.OneRowPack31();
            yield return Capture(dir, 31, "18i_stop_label_1fila", "plate");
            LaunchOptions.LevelPackJsonOverride = null;
            Assert.That(File.Exists(Path.Combine(dir, "18i_stop_label_1fila.png")), Is.True);
            yield return Capture(dir, 131, "18b_stop_label_3filas", "plate");
            Assert.That(File.Exists(Path.Combine(dir, "18b_stop_label_3filas.png")), Is.True);
            yield return Capture(dir, 171, "18c_stop_label_l171", "plate_side");
            yield return Capture(dir, 171, "18d_stop_label_l171_elevada", "plate_side", true);
            yield return Capture(dir, 191, "18e_stop_label_l191", "plate");
            yield return Capture(dir, 131, "18g_stop_label_l131_elevada", "plate", true);
            yield return Capture(dir, 191, "18h_stop_label_l191_elevada", "plate", true);
            GameScreen.StopLabelSpotsOverride = GameScreen.StopLabelSpot.Dock;
            yield return Capture(dir, 171, "18f_stop_label_l171_muelle", "dock");
            Assert.That(File.Exists(Path.Combine(dir, "18f_stop_label_l171_muelle.png")), Is.True);
            Assert.That(File.Exists(Path.Combine(dir, "18h_stop_label_l191_elevada.png")), Is.True);
        }

        // Con liftCuia0, la cuia 0 elevada: de lado (el 171) no está bajo la placa, que sigue a la vista; clavada al marco (el 131
        // y el 191) sí, así que la placa ya se ha ido (stop_label_off, lift) y la captura enseña la cuia elevada, con su corcho,
        // sin nada encima.
        private static IEnumerator Capture(string dir, int level, string name, string mode, bool liftCuia0 = false)
        {
            string? lastProbe = null; // la última sonda AF_STATE stop_label o stop_label_off
            Application.LogCallback capture = (message, stack, type) =>
            {
                if (message.StartsWith("AF_STATE", System.StringComparison.Ordinal) && message.Contains("\"event\":\"stop_label"))
                {
                    lastProbe = message;
                }
            };
            Application.logMessageReceived += capture;
            try
            {
                yield return CaptureCore(dir, level, name, mode, liftCuia0, () => lastProbe);
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }
        }

        private static IEnumerator CaptureCore(string dir, int level, string name, string mode, bool liftCuia0, System.Func<string?> lastProbe)
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
            Assert.That(view.StopLabelMode, Is.EqualTo(mode), name);
            PlayModeHelpers.Step(boot, 400); // la entrada de 150 ms y las etiquetas del HUD ya apartadas
            Assert.That(view.StopLabelView.Alpha, Is.EqualTo(1f), name);
            StringAssert.Contains("\"event\":\"stop_label\"", lastProbe(), name + ": salió y aún está a la vista");
            if (liftCuia0)
            {
                boot.TapCuia(0);
                PlayModeHelpers.Step(boot, 300);
                Assert.That(view.Views[0].IsLifted, Is.True, name);
                if (mode == "plate_side")
                {
                    Assert.That(view.StopLabelView.Visible, Is.True, name + ": la cuia 0 no está bajo la placa: sigue a la vista");
                    StringAssert.Contains("\"event\":\"stop_label\"", lastProbe(), name);
                }
                else
                {
                    Assert.That(view.StopLabelView.Visible, Is.False, name + ": la cuia 0 elevada está bajo la placa: se ha ido");
                    StringAssert.Contains("\"event\":\"stop_label_off\"", lastProbe(), name + ": la última sonda, stop_label_off");
                    StringAssert.Contains("\"level\":" + level + ",\"reason\":\"lift\"", lastProbe(), name);
                }
            }

            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
            Rect r = view.StopLabelView.Rect;
            CultureInfo inv = CultureInfo.InvariantCulture;
            BoardLayoutResult l = view.Layout!;

            // La fila 1 como la mide FX4 al colocar la placa: lo más alto de sus cuias en reposo, corchos y piezas de las
            // mecánicas incluidos; de lado, además, el tramo libre en el que se centró.
            float top = float.MaxValue;
            int at = -1;
            for (int i = 0; i < view.Views.Length && i < l.RowOf.Length; i++)
            {
                float rest = view.StopLabelRestTopOf(i);
                if (l.RowOf[i] == 0 && !float.IsNaN(rest) && rest < top)
                {
                    top = rest;
                    at = i;
                }
            }

            string rowOne = at < 0 ? "sin medir" : top.ToString("0.0", inv) + " dp (cuia " + at.ToString(inv) + ")";
            Vector2 free = view.StopLabelSideSpan;
            if (!float.IsNaN(free.x))
            {
                rowOne += ", tramo libre de lado " + free.x.ToString("0.0", inv) + "-" + free.y.ToString("0.0", inv) + " dp";
            }

            Debug.Log("AF_STOP_LABEL " + name + ": " + r.ToString("0.0") + (view.StopLabelView.Visible ? "" : " (oculta)") + ", horizonte "
                + view.HorizonY.ToString("0.0", inv) + " dp, héroe " + view.HeroRect.ToString("0.0") + ", HUD "
                + view.HudBottomDp.ToString("0.0", inv) + " dp, canto interior del marco "
                + (l.PanelY + view.PanelFrameDp).ToString("0.0", inv) + " dp, lo más alto de la fila 1 "
                + rowOne + ", "
                + view.StopLabelView.Label.fontSize.ToString("0.0", inv) + " dp de texto, "
                + view.LastStopLabelProbe + (view.StopLabelView.Visible ? "" : "; " + lastProbe()));
        }
    }
}
