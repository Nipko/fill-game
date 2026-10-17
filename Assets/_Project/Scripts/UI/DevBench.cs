#nullable enable
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Levels;
using AF.Solver.Murky;
using AF.Solver.Search;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AF.UI
{
    /// <summary>
    /// Bench de desarrollo de la tarea PF1 (solo builds de desarrollo y el Editor: el archivo entero va tras
    /// <c>UNITY_EDITOR || DEVELOPMENT_BUILD</c>, así que un build de release ni lo compila): mide Morpho y los fotogramas en un
    /// teléfono de clase A para calibrar la compuerta de coste de Morpho (03 §8; 08 §5.4: Mali-G52 o Adreno 610; D-015,
    /// pregunta 44 a; propuesta de la tinaja c.9; K2).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><b>Morpho:</b> <see cref="SafeMoves"/> desde cero (memo vacío) en el tablero inicial de cada nivel
    /// elegido (<see cref="SelectBoards"/>: los de la Tinaja y los de Aguas turbias del <see cref="MurkyFromLevel"/> en
    /// adelante), con las opciones de Morpho (<c>hint_budget_nodes</c> = 40.000 por realidad y <c>hint_weight</c> = 2, 08 §9;
    /// con fútiles y sin su poda si hay tinaja, <see cref="SolverOptions.ForLevel"/>) y el corte de <c>hint_timeout_ms</c>
    /// (<see cref="HintTimeoutMs"/>, 150 ms de reloj real) con una <see cref="SolverCancellation"/> que marca un
    /// temporizador, como hará el cliente (03 §5). En el tablero inicial de los retos 329, 332 y 340 de la muestra de la
    /// tinaja, Morpho expande 8.298-10.606 nodos: las mismas consultas de la calibración del generador
    /// (<c>TinajaWalks.PcNodesPerMs</c>, <c>--self-test tinaja</c>).</description></item>
    /// <item><description><b>Líneas:</b> por nivel, <c>AF_PERF {"event":"morpho","level":…,"realities":…,"nodes":…,"ms":…,
    /// "outcome":…}</c> (<see cref="FormatMorpho"/>: la mediana de <see cref="TimedRuns"/> consultas tras una en frío,
    /// <c>cold_ms</c>); al final, <c>AF_PERF {"event":"morpho_summary",…}</c> (<see cref="FormatSummary"/>) con los nodos por
    /// milisegundo de las consultas pesadas (≥ <see cref="HeavyNodes"/> nodos) y el factor frente al PC del generador:
    /// <c>factor</c> = <see cref="PcNodesPerMs"/> ÷ nodos por ms del teléfono, el número que sustituye a
    /// <c>MorphoDeviceFactor</c> (8× [P]).</description></item>
    /// <item><description><b>Fotogramas</b> (el shader en los niveles de 3 filas): mientras se juega un nivel,
    /// <c>AF_PERF {"event":"frames","level":…,"rows":…,…}</c> (media, p95 y máximo del tiempo de fotograma, fps y tardíos de más
    /// de <see cref="FrameStats.LateMs"/>) cada <see cref="DevBenchSettings.FrameWindow"/> fotogramas y al
    /// cambiar de nivel (<see cref="FormatFrames"/>).</description></item>
    /// <item><description><b>Tableros:</b> los del catálogo del juego y los paquetes de la carpeta <see cref="BenchFolder"/>
    /// de <c>Application.persistentDataPath</c> (en el teléfono, <c>adb push</c> de la muestra de TJ1,
    /// <c>Tests/EditMode/Fixtures/pack3_sample.json</c>, y de la del generador, <c>tools/out/sample_tinaja/mundo_07_v1.json</c>,
    /// mientras los niveles con tinaja no estén en el juego); en el Editor, además, esas dos muestras del repositorio.</description></item>
    /// </list>
    /// Uso en el teléfono (dev.apk de <c>BuildDev.BuildAndroidDev</c>):
    /// <code>
    /// adb push Assets/_Project/Tests/EditMode/Fixtures/pack3_sample.json /sdcard/Android/data/com.amazonfill.dev/files/af_bench/
    /// adb push tools/out/sample_tinaja/mundo_07_v1.json /sdcard/Android/data/com.amazonfill.dev/files/af_bench/
    /// adb shell am start -S -n com.amazonfill.dev/com.unity3d.player.UnityPlayerGameActivity --ei af_bench_morpho 1 --ei af_level 106
    /// adb logcat -d -s Unity | grep -E "AF_PERF"
    /// </code>
    /// </remarks>
    public static class DevBench
    {
        /// <summary><c>hint_timeout_ms</c> por defecto (08 §9: 150, rango 100-300): el tiempo que tiene Morpho en el cliente.</summary>
        public const int HintTimeoutMs = 150;

        /// <summary><c>lvl_murky_from_level</c> por defecto (08 §9): Aguas turbias desde el 100.</summary>
        public const int MurkyFromLevel = 100;

        /// <summary>
        /// Nodos por milisegundo de Morpho en el PC del generador [E] (<c>TinajaWalks.PcNodesPerMs</c> de
        /// <c>tools/level-generator</c>; .NET 10 Release, el mínimo medido con el equipo cargado). Si cambia allí, cambia aquí.
        /// </summary>
        public const int PcNodesPerMs = 1000;

        /// <summary>Factor del teléfono que usa hoy la compuerta de coste [P] (<c>TinajaWalks.MorphoDeviceFactor</c>), hasta PF1.</summary>
        public const int AssumedDeviceFactor = 8;

        /// <summary>
        /// Nodos a partir de los cuales una consulta cuenta para el factor: las ligeras (decenas o cientos de nodos) miden sobre
        /// todo el coste fijo de enumerar realidades; las de la calibración del generador expanden 8.000-10.600.
        /// </summary>
        public const long HeavyNodes = 5000;

        /// <summary>Consultas medidas por nivel tras la de en frío (se informa la mediana).</summary>
        public const int TimedRuns = 3;

        /// <summary>Carpeta de paquetes extra dentro de <c>Application.persistentDataPath</c>.</summary>
        public const string BenchFolder = "af_bench";

        /// <summary>Muestra de TJ1 (relativa a <c>Application.dataPath</c>): los retos 323 y 329 con tinaja.</summary>
        public const string FixtureSample = "_Project/Tests/EditMode/Fixtures/pack3_sample.json";

        /// <summary>Muestra de la tinaja del generador (relativa a la raíz del proyecto): 323-340, con los retos 329, 332 y 340.</summary>
        public const string GeneratorSample = "tools/out/sample_tinaja/mundo_07_v1.json";

        /// <summary>
        /// Niveles que mide el bench, en orden de nivel y sin repetir número (gana el primer paquete que lo trae): los que llevan
        /// la Tinaja (<see cref="LevelDefinition.HasTinaja"/>) y los de Aguas turbias (<see cref="LevelDefinition.IsMurky"/> con
        /// dosis ocultas) desde <see cref="MurkyFromLevel"/>.
        /// </summary>
        public static List<LevelDefinition> SelectBoards(IEnumerable<LevelPack> packs)
        {
            if (packs == null)
            {
                throw new ArgumentNullException(nameof(packs));
            }

            var byLevel = new SortedDictionary<int, LevelDefinition>();
            foreach (LevelPack pack in packs)
            {
                for (int i = 0; i < pack.Count; i++)
                {
                    LevelDefinition def = pack.Levels[i];
                    bool murky = def.IsMurky && def.HiddenCount > 0 && def.Level >= MurkyFromLevel;
                    if ((def.HasTinaja || murky) && !byLevel.ContainsKey(def.Level))
                    {
                        byLevel.Add(def.Level, def);
                    }
                }
            }

            return new List<LevelDefinition>(byLevel.Values);
        }

        /// <summary>
        /// Opciones de Morpho por defecto (08 §9: <c>hint_budget_nodes</c> = 40.000 por realidad, <c>hint_weight</c> = 2, de
        /// <see cref="BoosterConfig.Defaults"/>), con fútiles y el vertido por tramo (D-005). <see cref="Measure"/> les añade
        /// <see cref="SolverOptions.ForLevel"/> y la bandera de corte.
        /// </summary>
        public static SolverOptions MorphoOptions()
        {
            BoosterConfig config = BoosterConfig.Defaults;
            return new SolverOptions(config.HintBudgetNodes, config.SolverWeight, true, PourMode.Run);
        }

        /// <summary>
        /// Una consulta de Morpho desde cero (memo vacío) en <paramref name="board"/>, el tablero inicial del nivel
        /// <paramref name="def"/>, con el corte de <paramref name="cancellation"/>: si <paramref name="timeoutMs"/> &gt; 0, un
        /// temporizador la marca a los <paramref name="timeoutMs"/> ms de reloj real (<c>hint_timeout_ms</c>); con 0, solo la
        /// marca quien llama (los tests). Una bandera por consulta: un temporizador que llega tarde no corta la siguiente.
        /// </summary>
        public static MorphoSample Measure(LevelDefinition def, BoardState board, SafeMoves safe, SolverOptions options, int timeoutMs, SolverCancellation cancellation)
        {
            if (def == null || board == null || safe == null || cancellation == null)
            {
                throw new ArgumentNullException(def == null ? nameof(def) : board == null ? nameof(board) : safe == null ? nameof(safe) : nameof(cancellation));
            }

            safe.ClearMemo();
            SolverOptions query = options.ForLevel(board).WithCancellation(cancellation);
            Timer? timer = timeoutMs > 0 ? new Timer(c => ((SolverCancellation)c!).Request(), cancellation, timeoutMs, Timeout.Infinite) : null;
            var watch = Stopwatch.StartNew();
            RealityStatus status;
            try
            {
                status = safe.Find(board, query, board);
            }
            finally
            {
                watch.Stop();
                timer?.Dispose();
            }

            SafeMovesOutcome outcome = status == RealityStatus.Ok ? safe.Outcome : SafeMovesOutcome.RealitiesRejected;
            return new MorphoSample(def.Level, def.Id, def.CuiaCount, def.Palette.Count, TinajaDoses(def), def.HiddenCount,
                safe.RealityCount, safe.Expanded, watch.Elapsed.TotalMilliseconds, outcome, safe.WasCancelled, safe.Count);
        }

        /// <summary>
        /// La medida de un nivel: una consulta en frío y <paramref name="runs"/> más; devuelve la mediana por tiempo con el
        /// tiempo en frío en <see cref="MorphoSample.ColdMs"/> y cuántas se cortaron en <see cref="MorphoSample.CutRuns"/>.
        /// </summary>
        public static MorphoSample MeasureLevel(LevelDefinition def, SafeMoves safe, SolverOptions options, int timeoutMs, int runs)
        {
            BoardState board = def.CreateBoard();
            MorphoSample cold = Measure(def, board, safe, options, timeoutMs, new SolverCancellation());
            int count = Math.Max(1, runs);
            var samples = new MorphoSample[count];
            int cut = cold.Cancelled ? 1 : 0;
            for (int i = 0; i < count; i++)
            {
                samples[i] = Measure(def, board, safe, options, timeoutMs, new SolverCancellation());
                cut += samples[i].Cancelled ? 1 : 0;
            }

            Array.Sort(samples, (a, b) => a.Ms.CompareTo(b.Ms));
            return samples[count / 2].WithRuns(cold.Ms, count, cut);
        }

        /// <summary>Dosis de la tinaja del nivel (0 si no la lleva).</summary>
        public static int TinajaDoses(LevelDefinition def)
        {
            for (int i = 0; i < def.Attrs.Count; i++)
            {
                if (def.Attrs[i].Type == MechanicType.Tinaja)
                {
                    return def.CopyDoses(i).Length;
                }
            }

            return 0;
        }

        /// <summary>Nombre del resultado en las líneas (<c>found</c>, <c>no_exit</c>, <c>some_reality_solvable</c>…).</summary>
        public static string OutcomeKey(SafeMovesOutcome outcome)
        {
            switch (outcome)
            {
                case SafeMovesOutcome.Found:
                    return "found";
                case SafeMovesOutcome.NoExit:
                    return "no_exit";
                case SafeMovesOutcome.SomeRealitySolvable:
                    return "some_reality_solvable";
                case SafeMovesOutcome.BudgetExceeded:
                    return "budget_exceeded";
                case SafeMovesOutcome.Won:
                    return "won";
                default:
                    return "realities_rejected";
            }
        }

        /// <summary>Línea <c>AF_PERF morpho</c> de un nivel.</summary>
        public static string FormatMorpho(in MorphoSample s, int timeoutMs)
        {
            var sb = new StringBuilder(256);
            sb.Append("AF_PERF {\"event\":\"morpho\",\"level\":").Append(I(s.Level))
                .Append(",\"id\":\"").Append(s.Id).Append('"')
                .Append(",\"realities\":").Append(I(s.Realities))
                .Append(",\"nodes\":").Append(s.Nodes.ToString(CultureInfo.InvariantCulture))
                .Append(",\"ms\":").Append(F(s.Ms))
                .Append(",\"outcome\":\"").Append(OutcomeKey(s.Outcome)).Append('"')
                .Append(",\"cancelled\":").Append(s.Cancelled ? "true" : "false")
                .Append(",\"safe\":").Append(I(s.Safe))
                .Append(",\"cuias\":").Append(I(s.Cuias))
                .Append(",\"colors\":").Append(I(s.Colors))
                .Append(",\"tinaja_doses\":").Append(I(s.TinajaDoses))
                .Append(",\"hidden\":").Append(I(s.Hidden))
                .Append(",\"cold_ms\":").Append(F(s.ColdMs))
                .Append(",\"runs\":").Append(I(s.Runs))
                .Append(",\"cut_runs\":").Append(I(s.CutRuns))
                .Append(",\"timeout_ms\":").Append(I(timeoutMs))
                .Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Línea <c>AF_PERF morpho_summary</c>: nodos por milisegundo de las consultas pesadas (≥ <see cref="HeavyNodes"/>; si
        /// no hay ninguna, de todas) y el factor frente al PC del generador (<see cref="PcNodesPerMs"/> ÷ esa cifra), con el
        /// teléfono (<c>SystemInfo</c>, leído en el hilo principal).
        /// </summary>
        public static string FormatSummary(IReadOnlyList<MorphoSample> samples, int timeoutMs, string device, string soc, string gpu)
        {
            long heavyNodes = 0;
            double heavyMs = 0;
            int heavy = 0;
            long allNodes = 0;
            double allMs = 0;
            double maxMs = 0;
            int cancelled = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                MorphoSample s = samples[i];
                allNodes += s.Nodes;
                allMs += s.Ms;
                maxMs = Math.Max(maxMs, s.Ms);
                cancelled += s.Cancelled ? 1 : 0;
                if (s.Nodes >= HeavyNodes)
                {
                    heavy++;
                    heavyNodes += s.Nodes;
                    heavyMs += s.Ms;
                }
            }

            long nodes = heavy > 0 ? heavyNodes : allNodes;
            double ms = heavy > 0 ? heavyMs : allMs;
            double rate = ms > 0 ? nodes / ms : 0;
            var sb = new StringBuilder(320);
            sb.Append("AF_PERF {\"event\":\"morpho_summary\",\"levels\":").Append(I(samples.Count))
                .Append(",\"heavy\":").Append(I(heavy))
                .Append(",\"nodes_per_ms\":").Append(F(rate))
                .Append(",\"pc_nodes_per_ms\":").Append(I(PcNodesPerMs))
                .Append(",\"factor\":").Append(rate > 0 ? F(PcNodesPerMs / rate) : "null")
                .Append(",\"assumed_factor\":").Append(I(AssumedDeviceFactor))
                .Append(",\"max_ms\":").Append(F(maxMs))
                .Append(",\"cancelled\":").Append(I(cancelled))
                .Append(",\"timeout_ms\":").Append(I(timeoutMs))
                .Append(",\"device\":\"").Append(Escape(device)).Append('"')
                .Append(",\"soc\":\"").Append(Escape(soc)).Append('"')
                .Append(",\"gpu\":\"").Append(Escape(gpu)).Append('"')
                .Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Media, p95 y máximo de los <paramref name="count"/> primeros tiempos de fotograma de <paramref name="frameMs"/> (ms),
        /// con <paramref name="scratch"/> (del mismo largo o más) para ordenar sin asignar. p95: el valor de la posición
        /// ⌈0,95 · n⌉ de los ordenados.
        /// </summary>
        public static FrameStats ComputeFrames(float[] frameMs, int count, float[] scratch)
        {
            if (count <= 0)
            {
                return default;
            }

            double sum = 0;
            float max = 0f;
            int slow = 0;
            for (int i = 0; i < count; i++)
            {
                float ms = frameMs[i];
                scratch[i] = ms;
                sum += ms;
                max = Mathf.Max(max, ms);
                slow += ms > FrameStats.LateMs ? 1 : 0;
            }

            Array.Sort(scratch, 0, count);
            int p95 = Math.Min(count - 1, Math.Max(0, (int)Math.Ceiling(0.95 * count) - 1));
            return new FrameStats(count, (float)(sum / count), scratch[p95], max, slow);
        }

        /// <summary>Línea <c>AF_PERF frames</c> de una ventana de fotogramas de un nivel.</summary>
        public static string FormatFrames(int level, int rows, int cuias, in FrameStats stats)
        {
            var sb = new StringBuilder(200);
            sb.Append("AF_PERF {\"event\":\"frames\",\"level\":").Append(I(level))
                .Append(",\"rows\":").Append(I(rows))
                .Append(",\"cuias\":").Append(I(cuias))
                .Append(",\"frames\":").Append(I(stats.Frames))
                .Append(",\"avg_ms\":").Append(F(stats.AvgMs))
                .Append(",\"p95_ms\":").Append(F(stats.P95Ms))
                .Append(",\"max_ms\":").Append(F(stats.MaxMs))
                .Append(",\"fps\":").Append(stats.AvgMs > 0f ? F(1000.0 / stats.AvgMs) : "0.00")
                .Append(",\"late\":").Append(I(stats.Slow))
                .Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Archivos de paquetes extra: los <c>*.json</c> de <see cref="BenchFolder"/> en <paramref name="persistentDataPath"/>
        /// (en orden ordinal) y, en el Editor (<paramref name="projectRoot"/> no nulo), las dos muestras del repositorio que
        /// existan (<see cref="FixtureSample"/> y <see cref="GeneratorSample"/>).
        /// </summary>
        public static List<string> ExtraPackFiles(string? persistentDataPath, string? projectRoot)
        {
            var files = new List<string>();
            if (!string.IsNullOrEmpty(persistentDataPath))
            {
                string folder = Path.Combine(persistentDataPath, BenchFolder);
                if (Directory.Exists(folder))
                {
                    string[] found = Directory.GetFiles(folder, "*.json");
                    Array.Sort(found, StringComparer.Ordinal);
                    files.AddRange(found);
                }
            }

            if (!string.IsNullOrEmpty(projectRoot))
            {
                string fixture = Path.Combine(projectRoot, "Assets", FixtureSample);
                string sample = Path.Combine(projectRoot, GeneratorSample);
                if (File.Exists(fixture))
                {
                    files.Add(fixture);
                }

                if (File.Exists(sample))
                {
                    files.Add(sample);
                }
            }

            return files;
        }

        /// <summary>Lee los paquetes de <paramref name="files"/>; uno que no se lee se avisa y se salta.</summary>
        public static List<LevelPack> ReadPacks(IReadOnlyList<string> files, Action<string> warn)
        {
            var packs = new List<LevelPack>();
            for (int i = 0; i < files.Count; i++)
            {
                try
                {
                    packs.Add(LevelPackReader.Read(File.ReadAllText(files[i])));
                }
                catch (Exception e)
                {
                    warn("[AF.DevBench] no se pudo leer " + files[i] + ": " + e.Message);
                }
            }

            return packs;
        }

        private static string I(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string F(double value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value!.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }

    /// <summary>Una medida de Morpho de <see cref="DevBench"/> en el tablero inicial de un nivel.</summary>
    public readonly struct MorphoSample
    {
        public MorphoSample(int level, string id, int cuias, int colors, int tinajaDoses, int hidden, int realities, long nodes,
            double ms, SafeMovesOutcome outcome, bool cancelled, int safe, double coldMs = 0, int runs = 1, int cutRuns = 0)
        {
            Level = level;
            Id = id;
            Cuias = cuias;
            Colors = colors;
            TinajaDoses = tinajaDoses;
            Hidden = hidden;
            Realities = realities;
            Nodes = nodes;
            Ms = ms;
            Outcome = outcome;
            Cancelled = cancelled;
            Safe = safe;
            ColdMs = coldMs;
            Runs = runs;
            CutRuns = cutRuns;
        }

        public int Level { get; }
        public string Id { get; }
        public int Cuias { get; }

        /// <summary>Colores del nivel (K de la paleta).</summary>
        public int Colors { get; }
        public int TinajaDoses { get; }
        public int Hidden { get; }
        public int Realities { get; }

        /// <summary>Estados expandidos por la consulta (<see cref="SafeMoves.Expanded"/>, desde cero).</summary>
        public long Nodes { get; }

        /// <summary>Milisegundos de reloj real de la consulta.</summary>
        public double Ms { get; }
        public SafeMovesOutcome Outcome { get; }

        /// <summary>La consulta se cortó por <c>hint_timeout_ms</c> (<see cref="SafeMoves.WasCancelled"/>).</summary>
        public bool Cancelled { get; }

        /// <summary>Vertidos seguros encontrados (<see cref="SafeMoves.Count"/>).</summary>
        public int Safe { get; }
        public double ColdMs { get; }
        public int Runs { get; }
        public int CutRuns { get; }

        /// <summary>La misma medida con el tiempo en frío y las consultas de <see cref="DevBench.MeasureLevel"/>.</summary>
        public MorphoSample WithRuns(double coldMs, int runs, int cutRuns)
        {
            return new MorphoSample(Level, Id, Cuias, Colors, TinajaDoses, Hidden, Realities, Nodes, Ms, Outcome, Cancelled, Safe, coldMs, runs, cutRuns);
        }
    }

    /// <summary>Tiempos de una ventana de fotogramas (<see cref="DevBench.ComputeFrames"/>).</summary>
    public readonly struct FrameStats
    {
        /// <summary>
        /// Fotograma tardío: más de 20 ms. El presupuesto es de 16,6 ms a 60 fps (08 §5.4) y el juego pide 60 fps
        /// (<c>Application.targetFrameRate</c>), así que cada fotograma dura unos 16,7 ms por la sincronía vertical; uno de más
        /// de 20 ms (un 20 % sobre el presupuesto) es uno que perdió su turno.
        /// </summary>
        public const float LateMs = 20f;

        public FrameStats(int frames, float avgMs, float p95Ms, float maxMs, int slow)
        {
            Frames = frames;
            AvgMs = avgMs;
            P95Ms = p95Ms;
            MaxMs = maxMs;
            Slow = slow;
        }

        public int Frames { get; }
        public float AvgMs { get; }
        public float P95Ms { get; }
        public float MaxMs { get; }

        /// <summary>Fotogramas tardíos (por encima de <see cref="LateMs"/>).</summary>
        public int Slow { get; }
    }

    /// <summary>Configuración de <see cref="DevBenchRunner"/>.</summary>
    public sealed class DevBenchSettings
    {
        /// <summary>Corte de cada consulta en ms de reloj real (0 = sin corte: los tests, que no miden tiempos).</summary>
        public int TimeoutMs { get; set; } = DevBench.HintTimeoutMs;

        /// <summary>Consultas medidas por nivel tras la de en frío.</summary>
        public int Runs { get; set; } = DevBench.TimedRuns;

        /// <summary>Añadir los paquetes del catálogo del juego (<see cref="GameBootstrap.Flow"/>), esperando a que exista.</summary>
        public bool UseGameCatalog { get; set; } = true;

        /// <summary>Segundos que se espera al catálogo del juego antes de medir sin él.</summary>
        public float CatalogWaitSeconds { get; set; } = 30f;

        /// <summary>Archivos de paquetes extra (<see cref="DevBench.ExtraPackFiles"/>).</summary>
        public List<string> ExtraPackFiles { get; set; } = new List<string>();

        /// <summary>Medir los fotogramas de los niveles que se juegan.</summary>
        public bool SampleFrames { get; set; } = true;

        /// <summary>Fotogramas por línea <c>AF_PERF frames</c> (unos 10 s a 60 fps).</summary>
        public int FrameWindow { get; set; } = 600;

        /// <summary>Fotogramas mínimos para escribir la ventana al cambiar de nivel.</summary>
        public int MinFrames { get; set; } = 30;

        /// <summary>Lo que se usa en el teléfono y en el Editor con <c>af_bench_morpho</c>.</summary>
        public static DevBenchSettings ForDevice()
        {
            string? root = Application.isEditor ? Path.GetDirectoryName(Application.dataPath) : null;
            return new DevBenchSettings { ExtraPackFiles = DevBench.ExtraPackFiles(Application.persistentDataPath, root) };
        }
    }

    /// <summary>
    /// Corre <see cref="DevBench"/> en el juego (tarea PF1): arranca solo con <c>af_bench_morpho</c> = 1 en un build de
    /// desarrollo (<see cref="LaunchOptions.ResolveBenchMorpho"/>), sin tocar la escena; mide Morpho en un hilo aparte (como el
    /// cliente, 03 §5) mientras el juego sigue, y los fotogramas en el hilo principal sin asignar memoria entre ventanas.
    /// </summary>
    public sealed class DevBenchRunner : MonoBehaviour
    {
        private const string LogPrefix = "[AF.DevBench] ";

        private readonly ConcurrentQueue<string> _lines = new ConcurrentQueue<string>();
        private DevBenchSettings _settings = new DevBenchSettings();
        private float _startedAt;
        private bool _morphoStarted;
        private volatile bool _morphoDone;
        private volatile bool _abort;
        private Thread? _thread;
        private float[] _frames = new float[600];
        private float[] _scratch = new float[600];
        private int _frameCount;
        private int _frameLevel;
        private int _frameRows;
        private int _frameCuias;
        private bool _skipFrame;

        /// <summary>Líneas escritas hasta ahora (también van al log), para los tests.</summary>
        public IReadOnlyCollection<string> Lines => _lines;

        /// <summary>Terminó la medida de Morpho (con su línea de resumen).</summary>
        public bool MorphoDone => _morphoDone;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!LaunchOptions.ResolveBenchMorpho())
            {
                return;
            }

            DevBenchRunner runner = Create(DevBenchSettings.ForDevice());
            DontDestroyOnLoad(runner.gameObject);
            Debug.LogWarning(LogPrefix + LaunchOptions.IntentExtraBenchMorpho + " = 1: bench de Morpho y de fotogramas (PF1).");
        }

        /// <summary>Crea el bench con <paramref name="settings"/> en un objeto nuevo (quien llama lo destruye).</summary>
        public static DevBenchRunner Create(DevBenchSettings settings)
        {
            var go = new GameObject("AF.DevBench");
            DevBenchRunner runner = go.AddComponent<DevBenchRunner>();
            runner.Configure(settings);
            return runner;
        }

        private void Configure(DevBenchSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            int window = Math.Max(1, settings.FrameWindow);
            _frames = new float[window];
            _scratch = new float[window];
            _startedAt = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            if (!_morphoStarted)
            {
                TryStartMorpho();
            }

            if (_settings.SampleFrames)
            {
                SampleFrame();
            }
        }

        private void TryStartMorpho()
        {
            var packs = new List<LevelPack>();
            if (_settings.UseGameCatalog)
            {
                LevelCatalog? catalog = GameBootstrap.Current != null ? GameBootstrap.Current.Flow?.Catalog : null;
                if (catalog == null && Time.realtimeSinceStartup - _startedAt < _settings.CatalogWaitSeconds)
                {
                    return; // el juego aún no ha leído sus paquetes
                }

                if (catalog != null)
                {
                    packs.AddRange(catalog.Packs);
                }
                else
                {
                    Emit(LogPrefix + "sin catálogo del juego tras " + _settings.CatalogWaitSeconds + " s: solo los paquetes extra.", true);
                }
            }

            _morphoStarted = true;
            // SystemInfo solo se lee en el hilo principal.
            string device = SystemInfo.deviceModel;
            string soc = SystemInfo.processorType + " x" + SystemInfo.processorCount.ToString(CultureInfo.InvariantCulture);
            string gpu = SystemInfo.graphicsDeviceName;
            var files = new List<string>(_settings.ExtraPackFiles);
            int timeoutMs = _settings.TimeoutMs;
            int runs = _settings.Runs;
            _thread = new Thread(() => RunMorpho(packs, files, timeoutMs, runs, device, soc, gpu))
            {
                IsBackground = true,
                Name = "AF.DevBench",
            };
            _thread.Start();
        }

        private void RunMorpho(List<LevelPack> packs, List<string> files, int timeoutMs, int runs, string device, string soc, string gpu)
        {
            try
            {
                packs.AddRange(DevBench.ReadPacks(files, w => Emit(w, true)));
                List<LevelDefinition> boards = DevBench.SelectBoards(packs);
                Emit(LogPrefix + "Morpho en " + boards.Count + " tableros (" + packs.Count + " paquetes, hint_timeout_ms = " + timeoutMs + ").", false);
                var safe = new SafeMoves();
                SolverOptions options = DevBench.MorphoOptions();
                var samples = new List<MorphoSample>(boards.Count);
                for (int i = 0; i < boards.Count && !_abort; i++)
                {
                    MorphoSample sample = DevBench.MeasureLevel(boards[i], safe, options, timeoutMs, runs);
                    samples.Add(sample);
                    Emit(DevBench.FormatMorpho(sample, timeoutMs), false);
                }

                Emit(DevBench.FormatSummary(samples, timeoutMs, device, soc, gpu), false);
            }
            catch (Exception e)
            {
                Emit(LogPrefix + "falló el bench de Morpho: " + e, true);
            }
            finally
            {
                _morphoDone = true;
            }
        }

        // Un fotograma más de la ventana del nivel en curso; al cambiar de nivel o de filas, o al llenarse, escribe la línea.
        // Sin asignaciones salvo al escribir.
        private void SampleFrame()
        {
            GameBootstrap? boot = GameBootstrap.Current;
            LevelFlow? flow = boot != null ? boot.Flow : null;
            bool playing = boot != null && flow != null && flow.HasLevel && (boot.App == null || boot.App.Screen == AppScreen.Level);
            int level = playing ? flow!.LevelNumber : 0;
            BoardLayoutResult? layout = playing && boot!.View != null ? boot.View.Layout : null;
            int rows = layout?.Rows ?? 0;
            if (level != _frameLevel || rows != _frameRows)
            {
                FlushFrames(_settings.MinFrames);
                _frameLevel = level;
                _frameRows = rows;
                _frameCuias = playing ? flow!.Level.CuiaCount : 0;
                _skipFrame = true; // el fotograma del cambio lleva la carga del nivel
                return;
            }

            if (level <= 0)
            {
                return;
            }

            if (_skipFrame)
            {
                _skipFrame = false;
                return;
            }

            _frames[_frameCount++] = Time.unscaledDeltaTime * 1000f;
            if (_frameCount == _frames.Length)
            {
                FlushFrames(1);
            }
        }

        private void FlushFrames(int minFrames)
        {
            if (_frameLevel > 0 && _frameCount >= minFrames)
            {
                FrameStats stats = DevBench.ComputeFrames(_frames, _frameCount, _scratch);
                Emit(DevBench.FormatFrames(_frameLevel, _frameRows, _frameCuias, stats), false);
            }

            _frameCount = 0;
        }

        private void Emit(string line, bool warning)
        {
            _lines.Enqueue(line);
            if (warning)
            {
                Debug.LogWarning(line);
            }
            else
            {
                Debug.Log(line);
            }
        }

        private void OnDestroy()
        {
            _abort = true;
            if (_settings.SampleFrames)
            {
                FlushFrames(_settings.MinFrames);
            }
        }
    }
}
#endif
