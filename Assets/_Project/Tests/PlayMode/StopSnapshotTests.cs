#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas del fondo por parada en tableros de 1 y de 3 filas (tarea M1.4; D-013, punto 9; propuesta del Mapa §10.6;
    /// VA-20 a VA-23 de direccion-visual-v2 §9), con las siluetas provisionales (solo en el Editor y en desarrollo; nunca en la
    /// ficha, las capturas de la tienda ni la UA): <c>02b_board_l1</c> (1 fila: el héroe entero sobre el horizonte) y
    /// <c>02f_board_stop_15_l151</c> (el 151 real, 3 filas: el panel empieza bajo el HUD y no queda franja). Las 20
    /// composiciones de 1-200 en el tablero de 2 filas (<c>02f_board_stop_&lt;nn&gt;</c>) las genera un solo fixture,
    /// <see cref="StopSheetSnapshotTests"/>, junto con la hoja de VA-20 que las reutiliza (revisión de M2.4: antes las escribían
    /// los dos y se pisaban). Cada captura escribe en el log su línea <c>AF_STOP</c> con la sonda.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.StopSnapshotTests</c>
    /// sin <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas del fondo por parada (M1.4); necesitan dispositivo gráfico.")]
    public sealed class StopSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureTheOneAndThreeRowBoards()
        {
            string dir = SnapshotKit.Prepare();
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            yield return CaptureBoard(dir, "02b_board_l1", 0);

            yield return PlayModeHelpers.LoadGame(151, new MemoryProgressStore(151, 150));
            yield return CaptureBoard(dir, "02f_board_stop_15_l151", 15);
        }

        private static IEnumerator CaptureBoard(string dir, string name, int stop)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            boot.TapAnywhere();
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.Phase == FlowPhase.Playing, "fin de la intro (" + name + ")");
            yield return SnapshotKit.Wait(0.3f);
            StopBackdrop b = boot.View!.StopBackdrop;
            Assert.That(b.Stop, Is.Not.Null, name);
            Assert.That(b.Stop!.N, Is.EqualTo(stop), name);
            SnapshotKit.Save(dir, name);
            Debug.Log("AF_STOP " + name + ": horizonte " + boot.View.HorizonY.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                + " dp, héroe " + b.HeroSource + " " + (b.Shape ?? "-") + ", " + boot.View.LastStopProbe);
        }
    }
}
