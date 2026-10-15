#nullable enable
using System.Collections;
using System.IO;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas del fondo de cada parada en el tablero (tarea M1.4; D-013, punto 9; propuesta del Mapa §10.6; VA-20 a VA-23 de
    /// direccion-visual-v2 §9), con las siluetas provisionales (solo en el Editor y en desarrollo; nunca en la ficha, las
    /// capturas de la tienda ni la UA): <c>02f_board_stop_&lt;nn&gt;</c> para las 20 paradas de 1-200, cada una en el tablero
    /// de 2 filas del nivel 7 (7 cuias, 4 + 3) puesto en el nivel <c>10n + 7</c> con el agua base de su mundo, más
    /// <c>02b_board_l1</c> (1 fila: el héroe entero sobre el horizonte) y <c>02f_board_stop_15_l151</c> (el 151 real, 3 filas:
    /// el panel empieza bajo el HUD y no queda franja). Cada captura escribe en el log su línea <c>AF_STOP</c> con la sonda.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.StopSnapshotTests</c>
    /// sin <c>-nographics</c>); los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas del fondo por parada (M1.4); necesitan dispositivo gráfico.")]
    public sealed class StopSnapshotTests
    {
        private static readonly string[] WorldIds = { string.Empty, "w01_estuario", "w02_tapajos", "w03_manaos", "w04_trapecio", "w05_loreto" };
        private static readonly string[] Waters = { string.Empty, "white", "clear", "black", "white", "white" };

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureEveryStop_OnATwoRowBoard()
        {
            string dir = SnapshotKit.Prepare();
            for (int n = 0; n < 20; n++)
            {
                int level = (10 * n) + 7;
                int world = WorldTable.WorldOf(level);
                LaunchOptions.LevelPackJsonOverride = PlayModeHelpers.Level7BoardPack(world, WorldIds[world], Waters[world], level);
                yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
                string name = "02f_board_stop_" + n.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                yield return CaptureBoard(dir, name, n);
            }

            LaunchOptions.LevelPackJsonOverride = null;
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            yield return CaptureBoard(dir, "02b_board_l1", 0);

            yield return PlayModeHelpers.LoadGame(151, new MemoryProgressStore(151, 150));
            yield return CaptureBoard(dir, "02f_board_stop_15_l151", 15);
            Assert.That(File.Exists(Path.Combine(dir, "02f_board_stop_19.png")), Is.True);
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
