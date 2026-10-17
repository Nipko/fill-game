#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using AF.Solver.Murky;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Bench de desarrollo de la tarea PF1 (<see cref="DevBench"/> y <see cref="DevBenchRunner"/>; 03 §8, 08 §5.4, D-015 y
    /// pregunta 44 a): qué tableros mide (la tinaja de la muestra de TJ1 y Aguas turbias desde el 100), la consulta de Morpho
    /// con y sin corte, el formato de las líneas <c>AF_PERF morpho</c>, <c>morpho_summary</c> y <c>frames</c>, y el bench
    /// entero en el Editor con el juego en marcha. No mide tiempos: los tiempos son del teléfono.
    /// </summary>
    public sealed class DevBenchTests
    {
        private static readonly Regex MorphoLine = new Regex(
            "^AF_PERF \\{\"event\":\"morpho\",\"level\":(\\d+),\"id\":\"[^\"]+\",\"realities\":(\\d+),\"nodes\":(\\d+),\"ms\":\\d+\\.\\d\\d,\"outcome\":\"([a-z_]+)\",\"cancelled\":(true|false),.*\\}$");

        private static readonly Regex SummaryLine = new Regex(
            "^AF_PERF \\{\"event\":\"morpho_summary\",\"levels\":(\\d+),\"heavy\":(\\d+),\"nodes_per_ms\":\\d+\\.\\d\\d,\"pc_nodes_per_ms\":1000,\"factor\":(\\d+\\.\\d\\d|null),\"assumed_factor\":8,.*\\}$");

        private static readonly Regex FramesLine = new Regex(
            "^AF_PERF \\{\"event\":\"frames\",\"level\":(\\d+),\"rows\":(\\d+),\"cuias\":(\\d+),\"frames\":(\\d+),\"avg_ms\":\\d+\\.\\d\\d,\"p95_ms\":\\d+\\.\\d\\d,\"max_ms\":\\d+\\.\\d\\d,\"fps\":\\d+\\.\\d\\d,\"late\":\\d+,\"morpho_running\":(true|false)\\}$");

        private DevBenchRunner? _runner;

        private static string FixturePath => Path.Combine(Application.dataPath, DevBench.FixtureSample);

        private static string LevelsPath(string file) => Path.Combine(Application.dataPath, "_Project/Data/Levels", file);

        [TearDown]
        public void TearDown()
        {
            if (_runner != null)
            {
                Object.Destroy(_runner.gameObject);
                _runner = null;
            }

            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [Test]
        public void ResolveBenchMorpho_OffWithoutExtra_OnWithOverride_ResetClears()
        {
            Assert.That(LaunchOptions.ResolveBenchMorpho(), Is.False, "sin extra (el Editor no lee intents)");
            LaunchOptions.BenchMorphoOverride = true;
            Assert.That(LaunchOptions.ResolveBenchMorpho(), Is.True);
            LaunchOptions.BenchMorphoOverride = false;
            Assert.That(LaunchOptions.ResolveBenchMorpho(), Is.False);
            LaunchOptions.BenchMorphoOverride = true;
            LaunchOptions.Reset();
            Assert.That(LaunchOptions.BenchMorphoOverride, Is.Null);
            Assert.That(LaunchOptions.IntentExtraBenchMorpho, Is.EqualTo("af_bench_morpho"));
        }

        [Test]
        public void SelectBoards_TinajaAndMurkyFrom100_InLevelOrder_WithoutRepeats()
        {
            LevelPack fixture = LevelPackReader.Read(File.ReadAllText(FixturePath));
            LevelPack world4 = LevelPackReader.Read(File.ReadAllText(LevelsPath("mundo_04_v1.json")));
            LevelPack world3 = LevelPackReader.Read(File.ReadAllText(LevelsPath("mundo_03_v1.json")));

            List<LevelDefinition> boards = DevBench.SelectBoards(new[] { fixture, world4, world3, fixture });

            // Muestra de TJ1: la tinaja de 323 (3 dosis) y 329 (5 dosis, 8 colores); el resto lleva otras mecánicas sin ocultas.
            // Mundo 4: Aguas turbias en 100, 106, 115 y 135. Mundo 3 (41-80): sin Aguas turbias.
            Assert.That(boards.Select(b => b.Level), Is.EqualTo(new[] { 100, 106, 115, 135, 323, 329 }));
            LevelDefinition l329 = boards.Single(b => b.Level == 329);
            Assert.That(DevBench.TinajaDoses(l329), Is.EqualTo(5));
            Assert.That(l329.Palette.Count, Is.EqualTo(8));
            Assert.That(DevBench.TinajaDoses(boards.Single(b => b.Level == 100)), Is.EqualTo(0));
        }

        [Test]
        public void Measure_Level329WithoutTimeout_SafeMoveIn24Realities_SameNodesEveryTime()
        {
            LevelDefinition def = Fixture(329);
            var safe = new SafeMoves();

            MorphoSample first = DevBench.Measure(def, def.CreateBoard(), safe, DevBench.MorphoOptions(), 0, new SolverCancellation());
            MorphoSample second = DevBench.Measure(def, def.CreateBoard(), safe, DevBench.MorphoOptions(), 0, new SolverCancellation());

            Assert.That(first.Outcome, Is.EqualTo(SafeMovesOutcome.Found));
            Assert.That(first.Cancelled, Is.False);
            Assert.That(first.Safe, Is.GreaterThan(0));
            Assert.That(first.Realities, Is.EqualTo(24), "4 ocultas de la tinaja, 4! realidades");
            Assert.That(first.Nodes, Is.GreaterThanOrEqualTo(DevBench.HeavyNodes), "una consulta pesada, como las de la calibración");
            Assert.That(second.Nodes, Is.EqualTo(first.Nodes), "desde cero: los nodos son deterministas");
            Assert.That(first.TinajaDoses, Is.EqualTo(5));
            Assert.That(first.Cuias, Is.EqualTo(11));
            Assert.That(first.Colors, Is.EqualTo(8));
        }

        [Test]
        public void Measure_CutBeforeStarting_BudgetExceededAndCancelled()
        {
            LevelDefinition def = Fixture(329);
            var cut = new SolverCancellation();
            cut.Request();

            MorphoSample sample = DevBench.Measure(def, def.CreateBoard(), new SafeMoves(), DevBench.MorphoOptions(), 0, cut);

            Assert.That(sample.Cancelled, Is.True);
            Assert.That(sample.Outcome, Is.EqualTo(SafeMovesOutcome.BudgetExceeded), "respuesta 4: no cobra");
            Assert.That(sample.Safe, Is.EqualTo(0), "sin Pista parcial");
            Assert.That(sample.Nodes, Is.EqualTo(0));
        }

        [Test]
        public void MeasureLevel_ReportsRunsAndNoCuts_WithoutTimeout()
        {
            LevelDefinition def = Fixture(323);
            MorphoSample sample = DevBench.MeasureLevel(def, new SafeMoves(), DevBench.MorphoOptions(), 0, 3);

            Assert.That(sample.Runs, Is.EqualTo(3));
            Assert.That(sample.CutRuns, Is.EqualTo(0));
            Assert.That(sample.Outcome, Is.EqualTo(SafeMovesOutcome.Found));
            Assert.That(sample.Level, Is.EqualTo(323));
        }

        [Test]
        public void MeasureLevel_OnQueryGetsEachFlag_CuttingItCutsEveryRun()
        {
            LevelDefinition def = Fixture(329);
            var flags = new List<SolverCancellation>();

            MorphoSample sample = DevBench.MeasureLevel(def, new SafeMoves(), DevBench.MorphoOptions(), 0, 3, c =>
            {
                flags.Add(c);
                c.Request(); // como OnDestroy del runner: corta la consulta en curso
            });

            Assert.That(flags.Count, Is.EqualTo(4), "la de en frío y las 3 medidas");
            Assert.That(flags.Distinct().Count(), Is.EqualTo(4), "una bandera por consulta");
            Assert.That(sample.Cancelled, Is.True);
            Assert.That(sample.CutRuns, Is.EqualTo(4));
            Assert.That(sample.Nodes, Is.EqualTo(0));
        }

        [Test]
        public void FormatMorpho_AndSummary_CarryTheFields()
        {
            var heavy = new MorphoSample(329, "m07_v1_n0329", 11, 8, 5, 4, 24, 8000, 40.0, SafeMovesOutcome.Found, false, 10, 55.5, 3, 0);
            var light = new MorphoSample(100, "m04_v1_n0100", 10, 7, 0, 2, 2, 600, 3.0, SafeMovesOutcome.BudgetExceeded, true, 0, 4.0, 3, 1);

            string line = DevBench.FormatMorpho(heavy, 150);
            Match m = MorphoLine.Match(line);
            Assert.That(m.Success, Is.True, line);
            Assert.That(m.Groups[1].Value, Is.EqualTo("329"));
            Assert.That(m.Groups[2].Value, Is.EqualTo("24"));
            Assert.That(m.Groups[3].Value, Is.EqualTo("8000"));
            Assert.That(m.Groups[4].Value, Is.EqualTo("found"));
            Assert.That(line, Does.Contain("\"ms\":40.00").And.Contain("\"tinaja_doses\":5").And.Contain("\"cold_ms\":55.50").And.Contain("\"timeout_ms\":150"));
            Assert.That(DevBench.FormatMorpho(light, 150), Does.Contain("\"outcome\":\"budget_exceeded\",\"cancelled\":true").And.Contain("\"cut_runs\":1"));
            var odd = new MorphoSample(1, "a\"b\\c", 4, 2, 0, 0, 1, 0, 0.0, SafeMovesOutcome.Found, false, 0);
            Assert.That(DevBench.FormatMorpho(odd, 150), Does.Contain("\"id\":\"a\\\"b\\\\c\""), "el id va escapado, como los campos de SystemInfo");

            // Solo la consulta pesada cuenta: 8.000 nodos en 40 ms = 200 nodos/ms; 1.000 / 200 = factor 5.
            string summary = DevBench.FormatSummary(new[] { heavy, light }, 150, "SM-S938B", "ARM64 \"x8\"", "Adreno");
            Match s = SummaryLine.Match(summary);
            Assert.That(s.Success, Is.True, summary);
            Assert.That(s.Groups[1].Value, Is.EqualTo("2"));
            Assert.That(s.Groups[2].Value, Is.EqualTo("1"));
            Assert.That(s.Groups[3].Value, Is.EqualTo("5.00"));
            Assert.That(summary, Does.Contain("\"nodes_per_ms\":200.00").And.Contain("\"cancelled\":1").And.Contain("\"soc\":\"ARM64 \\\"x8\\\"\""));
        }

        [Test]
        public void ComputeFrames_AverageP95AndMax()
        {
            var ms = new float[25];
            for (int i = 0; i < 20; i++)
            {
                ms[i] = 20 - i; // 20, 19 … 1, desordenados
            }

            FrameStats stats = DevBench.ComputeFrames(ms, 20, new float[25]);

            Assert.That(stats.Frames, Is.EqualTo(20));
            Assert.That(stats.AvgMs, Is.EqualTo(10.5f).Within(1e-4f));
            Assert.That(stats.P95Ms, Is.EqualTo(19f), "⌈0,95 · 20⌉ = 19.º de los ordenados");
            Assert.That(stats.MaxMs, Is.EqualTo(20f));
            Assert.That(stats.Slow, Is.EqualTo(0), "ninguno pasa de 20 ms");
            Assert.That(ms[0], Is.EqualTo(20f), "no reordena la entrada");
            string line = DevBench.FormatFrames(106, 3, 11, stats, false);
            Assert.That(line, Does.Match(FramesLine.ToString()));
            Assert.That(line, Does.Contain("\"avg_ms\":10.50,\"p95_ms\":19.00,\"max_ms\":20.00,\"fps\":95.24,\"late\":0,\"morpho_running\":false}"));
            Assert.That(DevBench.FormatFrames(106, 3, 11, stats, true), Does.EndWith(",\"morpho_running\":true}"));
            ms[20] = 20.5f; // uno tardío
            Assert.That(DevBench.ComputeFrames(ms, 21, new float[25]).Slow, Is.EqualTo(1));
        }

        [Test]
        public void ExtraPackFiles_JsonOfTheBenchFolderInOrdinalOrder_AndTheRepositorySamplesInTheEditor()
        {
            string root = Path.Combine(Path.GetTempPath(), "af_devbench_" + System.Guid.NewGuid().ToString("N"));
            string folder = Path.Combine(root, DevBench.BenchFolder);
            Directory.CreateDirectory(folder);
            try
            {
                File.WriteAllText(Path.Combine(folder, "a.json"), "{}");
                File.WriteAllText(Path.Combine(folder, "Z.json"), "{}");
                File.WriteAllText(Path.Combine(folder, "notas.txt"), "x");

                List<string> device = DevBench.ExtraPackFiles(root, null);
                Assert.That(device.Select(f => Path.GetFileName(f)), Is.EqualTo(new[] { "Z.json", "a.json" }), "orden ordinal");

                string project = Path.GetDirectoryName(Application.dataPath)!;
                List<string> editor = DevBench.ExtraPackFiles(null, project);
                Assert.That(editor, Does.Contain(Path.Combine(project, "Assets", DevBench.FixtureSample)), "la muestra de TJ1");

                var warnings = new List<string>();
                List<LevelPack> packs = DevBench.ReadPacks(device, warnings.Add);
                Assert.That(packs, Is.Empty);
                Assert.That(warnings.Count, Is.EqualTo(2), "un paquete que no se lee se avisa y se salta");
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        /// <summary>
        /// El bench entero en el Editor sin corte (deterministas): el juego en el nivel 1 con un tablero de 13 cuias (3 filas en
        /// 360 × 780 dp, D-006), el catálogo del juego (ese paquete, sin Aguas turbias) y la muestra de TJ1 como paquete extra:
        /// una línea morpho por tablero (323 y 329, en orden), el resumen con una consulta pesada y una línea frames del nivel 1
        /// con 3 filas.
        /// </summary>
        [UnityTest]
        public IEnumerator Runner_FixtureWithoutTimeout_LinesForEachBoard_SummaryAndThreeRowFrames()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            yield return PlayModeHelpers.LoadGameWithPack(PlayModeHelpers.ThirteenCuiaPack(), 1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 2000);
            Assert.That(boot.View!.Layout!.Rows, Is.EqualTo(3));

            _runner = DevBenchRunner.Create(new DevBenchSettings
            {
                TimeoutMs = 0,
                Runs = 1,
                ExtraPackFiles = new List<string> { FixturePath },
                FrameWindow = 20,
                MinFrames = 5,
            });
            // Una ventana entera tras Morpho: la que lee el shader (morpho_running = false).
            yield return WaitUntil(() => _runner.MorphoDone && _runner.Lines.Any(l => l.Contains("\"morpho_running\":false")), 120f, "el bench de Morpho y una ventana de fotogramas sin él");

            string[] lines = _runner.Lines.ToArray();
            string[] morpho = lines.Where(l => l.Contains("\"event\":\"morpho\"")).ToArray();
            Assert.That(morpho.Select(l => MorphoLine.Match(l).Groups[1].Value), Is.EqualTo(new[] { "323", "329" }), string.Join("\n", lines));
            foreach (string line in morpho)
            {
                Match m = MorphoLine.Match(line);
                Assert.That(m.Success, Is.True, line);
                Assert.That(m.Groups[4].Value, Is.EqualTo("found"), line);
                Assert.That(m.Groups[5].Value, Is.EqualTo("false"), "sin corte: " + line);
            }

            Match summary = lines.Select(l => SummaryLine.Match(l)).Single(x => x.Success);
            Assert.That(summary.Groups[1].Value, Is.EqualTo("2"));
            Assert.That(summary.Groups[2].Value, Is.EqualTo("1"), "solo el 329 es una consulta pesada");

            Match frames = lines.Select(l => FramesLine.Match(l)).First(x => x.Success && x.Groups[5].Value == "false");
            Assert.That(frames.Groups[1].Value, Is.EqualTo("1"));
            Assert.That(frames.Groups[2].Value, Is.EqualTo("3"), "nivel de 3 filas");
            Assert.That(frames.Groups[3].Value, Is.EqualTo("13"));
            Assert.That(frames.Groups[4].Value, Is.EqualTo("20"));
        }

        /// <summary>
        /// El bench como en el teléfono, con <c>hint_timeout_ms</c> = 150 de reloj real y el catálogo del juego (los paquetes de
        /// la escena, con Aguas turbias en 100-195) más, en el Editor, las dos muestras del repositorio (tinaja en 323-340). Solo
        /// comprueba que corre y el formato (los tiempos del Editor no cuentan: el resultado puede ser un corte).
        /// </summary>
        [UnityTest]
        public IEnumerator Runner_AsOnThePhone_CatalogAndSamples_WritesMorphoLinesAndSummary()
        {
            yield return PlayModeHelpers.LoadGame(100, new MemoryProgressStore());

            DevBenchSettings settings = DevBenchSettings.ForDevice();
            settings.SampleFrames = false;
            Assert.That(settings.TimeoutMs, Is.EqualTo(DevBench.HintTimeoutMs));
            _runner = DevBenchRunner.Create(settings);
            yield return WaitUntil(() => _runner.MorphoDone, 180f, "el bench de Morpho con el catálogo del juego");

            string[] lines = _runner.Lines.ToArray();
            int[] levels = lines.Select(l => MorphoLine.Match(l)).Where(x => x.Success).Select(x => int.Parse(x.Groups[1].Value)).ToArray();
            Assert.That(levels, Is.SupersetOf(new[] { 100, 106, 195, 329, 340 }), string.Join("\n", lines));
            Assert.That(levels, Is.Ordered.Ascending);
            Assert.That(levels.All(l => l >= DevBench.MurkyFromLevel), Is.True);
            Assert.That(lines.Count(l => SummaryLine.IsMatch(l)), Is.EqualTo(1), string.Join("\n", lines));
        }

        private static LevelDefinition Fixture(int level)
        {
            LevelPack pack = LevelPackReader.Read(File.ReadAllText(FixturePath));
            return pack.Find(level) ?? throw new AssertionException("falta el nivel " + level + " en la muestra de TJ1");
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float seconds, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > seconds)
                {
                    Assert.Fail("Tiempo agotado esperando: " + what);
                }

                yield return null;
            }
        }
    }
}
