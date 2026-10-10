#nullable enable
using System.Collections;
using System.IO;
using System.Text;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión del Remolino y del Barro (T16; <c>[Explicit]</c>, no forman parte de la suite):
    /// <c>75_remolino_l103</c> (la pareja del 103 con su espiral y su canal), <c>75b_remolino_1fila</c> y
    /// <c>75c_remolino_3filas</c> (el canal con 1 y 3 filas, en paquetes de prueba), <c>76_remolino_receive_l103</c> (la espuma
    /// recorre el canal desde la cuia tocada) y <c>76b_remolino_receive_l103</c> (la racha sube por el fondo de la pareja con
    /// burbujas), <c>77_barro_l123</c> (el Barro en reposo) y <c>78_barro_wash_l123</c> y <c>78b_barro_wash_l123</c> (el barro se
    /// enturbia y se disuelve en la fruta nueva). Sirven para mirar a 48 dp la legibilidad de la pareja y del canal, los solapes
    /// con cuias y repisas, los colores y la línea D-003. Necesitan gráficos:
    /// <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.RemolinoBarroSnapshotTests</c> sin
    /// <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class RemolinoBarroSnapshotTests
    {
        private static readonly string[] TintIds =
        {
            "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo", "agua_tapajos",
            "lulo", "araza", "cacao", "agua_negro",
        };

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureRemolino()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadIdle(103, null);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Save(dir, "75_remolino_l103");
            SnapshotKit.LogBoard("75_remolino_l103", boot);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.ThroughRemolino);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            TapResult poured = MechanicTestKit.Tap(boot, path![path.Length - 1]);
            int partner = poured.Pour.Receiver;
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Receive, partner, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Receive, partner, 0.28f);
            SnapshotKit.Save(dir, "76_remolino_receive_l103");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Receive, partner, 0.7f);
            SnapshotKit.Save(dir, "76b_remolino_receive_l103");
            MechanicTestKit.StepToIdle(boot, 4000);
            Assert.That(File.Exists(Path.Combine(dir, "76b_remolino_receive_l103.png")), Is.True);
        }

        [UnityTest]
        public IEnumerator CaptureRemolinoRows()
        {
            string dir = SnapshotKit.Prepare();

            // 1 fila: 5 cuias (3 frutas y 2 vacías), pareja 1-4.
            int[][] one = { new[] { 1, 2, 3, 1 }, new[] { 2, 3, 1, 2 }, new[] { 3, 1, 2, 3 }, new int[0], new int[0] };
            yield return LoadIdle(103, Pack(one, 1, 4));
            SnapshotKit.Save(dir, "75b_remolino_1fila");
            SnapshotKit.LogBoard("75b_remolino_1fila", GameBootstrap.Current!);
            LaunchOptions.Reset();

            // 3 filas: 16 cuias (14 frutas en escalera y 2 vacías, 6 + 5 + 5), pareja de la fila 1 a la 3.
            var three = new int[16][];
            for (int i = 0; i < 16; i++)
            {
                three[i] = i < 14 ? new[] { (i % 14) + 1, ((i + 3) % 14) + 1, ((i + 6) % 14) + 1, ((i + 9) % 14) + 1 } : new int[0];
            }

            yield return LoadIdle(103, Pack(three, 2, 13));
            SnapshotKit.Save(dir, "75c_remolino_3filas");
            SnapshotKit.LogBoard("75c_remolino_3filas", GameBootstrap.Current!);
            Assert.That(File.Exists(Path.Combine(dir, "75c_remolino_3filas.png")), Is.True);
        }

        [UnityTest]
        public IEnumerator CaptureBarro()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadIdle(123, null);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Save(dir, "77_barro_l123");
            SnapshotKit.LogBoard("77_barro_l123", boot);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.BarroWashedMask != 0);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            Pour pour = path![path.Length - 1];
            MechanicTestKit.Tap(boot, pour);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.MudWash, pour.Target, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.MudWash, pour.Target, 0.2f);
            SnapshotKit.Save(dir, "78_barro_wash_l123");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.MudWash, pour.Target, 0.55f);
            SnapshotKit.Save(dir, "78b_barro_wash_l123");
            MechanicTestKit.StepToIdle(boot, 4000);
            Assert.That(File.Exists(Path.Combine(dir, "78b_barro_wash_l123.png")), Is.True);
        }

        // Carga el nivel (con el paquete de prueba json, si lo hay), salta la intro y deja la vista quieta, con la cámara de
        // las capturas.
        private static IEnumerator LoadIdle(int level, string? json)
        {
            if (json != null)
            {
                LaunchOptions.LevelPackJsonOverride = json;
            }

            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "nivel " + level);
            PlayModeHelpers.Step(boot, 300);
        }

        // Paquete pack_schema 2 de prueba con el nivel 103 (mundo 4) y la pareja de Remolino a-b.
        private static string Pack(int[][] cuias, int a, int b)
        {
            var used = new bool[TintIds.Length + 1];
            var caps = new StringBuilder();
            var body = new StringBuilder();
            var attrs = new StringBuilder();
            for (int i = 0; i < cuias.Length; i++)
            {
                string sep = i == 0 ? string.Empty : ", ";
                caps.Append(sep).Append('4');
                body.Append(sep).Append('[');
                for (int k = 0; k < cuias[i].Length; k++)
                {
                    used[cuias[i][k]] = true;
                    body.Append(k == 0 ? string.Empty : ", ").Append(cuias[i][k]);
                }

                body.Append(']');
                attrs.Append(sep).Append(i == a ? "{\"type\": \"remolino\", \"partner\": " + b + "}"
                    : i == b ? "{\"type\": \"remolino\", \"partner\": " + a + "}" : "null");
            }

            var palette = new StringBuilder();
            for (int c = 1; c < used.Length; c++)
            {
                if (used[c])
                {
                    palette.Append(palette.Length == 0 ? "\"" : ", \"").Append(TintIds[c - 1]).Append('"');
                }
            }

            return "{ \"pack_schema\": 2, \"pack_id\": \"test_remolino\", \"kind\": \"campaign\", \"world\": 4, \"world_id\": \"w04_trapecio\", "
                + "\"lap\": 1, \"water_base\": \"white\", \"levels\": [ { \"id\": \"m04_v1_n0103\", \"level\": 103, \"world\": 4, \"lap\": 1, "
                + "\"water_base\": \"white\", \"palette\": [" + palette + "], \"units_per_color\": 4, \"caps\": [" + caps + "], \"cuias\": ["
                + body + "], \"attrs\": [" + attrs + "], \"murky\": false, \"hidden\": [], \"difficulty\": { \"par\": 1 }, \"relief\": [] } ] }";
        }
    }
}
