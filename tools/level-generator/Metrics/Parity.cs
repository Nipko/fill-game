// --parity: paridad de las métricas con el prototipo (GDD 03 §17-§18; 09 R26: el PRNG cambia, así que la paridad se
// mide sobre los estados exportados). Sobre los 40 niveles de tools/out/levels_proto.json y
// tools/out_inverse/levels_proto.json se recalculan con AF.Core/AF.Solver par, complejidad, ramificación, trampas y la
// variante −1 color, y se comparan con lo que publicó el prototipo:
//   par y complejidad iguales en 40/40; ramificación y trampas iguales (2 decimales) en 38/40 o más, con las diferencias
//   explicadas; color y par de minus_color iguales donde el prototipo los tiene. bot_fail y D no se comparan (R26).
// Informe determinista (sin tiempos): tools/out/parity_report.md. Códigos: 0 = umbrales cumplidos; 1 = no; 2 = uso.
using System.Globalization;
using System.Text;
using System.Text.Json;
using AF.Core.Board;
using AmazonFill.Tools.LevelGenerator.Campaign;

namespace AmazonFill.Tools.LevelGenerator.Metrics;

internal static class Parity
{
    public static readonly string[] Sources = ["tools/out/levels_proto.json", "tools/out_inverse/levels_proto.json"];

    private sealed record Row(
        string Source, string Id, int K, int E, int Cuias,
        int ProtoPar, int Par, int ProtoComplexity, int Complexity,
        double ProtoBranching, double Branching, double SolverMovesBranching, double ProtoTraps, double Traps, int TrapPairs, int TrapUnknown,
        int? ProtoMinusColor, int? ProtoMinusPar, int? MinusColor, int? MinusPar);

    public static int Run(string reportPath)
    {
        var rows = new List<Row>();
        var metrics = new MetricsEngine();
        foreach (string source in Sources)
        {
            if (!File.Exists(source))
            {
                Console.Error.WriteLine($"level-generator: no existe {source} (ejecuta desde la raiz del repositorio)");
                return 2;
            }

            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(source, Encoding.UTF8));
            foreach (JsonElement level in doc.RootElement.GetProperty("levels").EnumerateArray())
            {
                rows.Add(Measure(metrics, source, level));
                Row r = rows[^1];
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{r.Source,-38} {r.Id}: par {r.ProtoPar}/{r.Par} compl {r.ProtoComplexity}/{r.Complexity} ramif {r.ProtoBranching:0.00}/{Ours(r.Branching, 2)} trampas {r.ProtoTraps:0.000}/{Ours(r.Traps, 3)} -1 color {Fmt(r.ProtoMinusColor)}({Fmt(r.ProtoMinusPar)})/{Fmt(r.MinusColor)}({Fmt(r.MinusPar)})"));
            }
        }

        int n = rows.Count;
        int parEq = rows.Count(r => r.ProtoPar == r.Par);
        int complexityEq = rows.Count(r => r.ProtoComplexity == r.Complexity);
        int branchingEq = rows.Count(r => Same2(r.ProtoBranching, r.Branching));
        int solverMovesEq = rows.Count(r => Same2(r.ProtoBranching, r.SolverMovesBranching));
        int trapsEq = rows.Count(r => Same2(r.ProtoTraps, r.Traps, 3));
        List<Row> withMinus = rows.Where(r => r.ProtoMinusColor is not null).ToList();
        int minusEq = withMinus.Count(r => r.ProtoMinusColor == r.MinusColor && r.ProtoMinusPar == r.MinusPar);
        int needed = n - 2;
        bool ok = parEq == n && complexityEq == n && branchingEq >= needed && trapsEq >= needed && minusEq == withMinus.Count;

        File.WriteAllBytes(reportPath, new UTF8Encoding(false).GetBytes(Report(rows, parEq, complexityEq, branchingEq, solverMovesEq, trapsEq, minusEq, withMinus.Count, ok)));
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"paridad: par {parEq}/{n}, complejidad {complexityEq}/{n}, ramificacion {branchingEq}/{n}, trampas {trapsEq}/{n} (>= {needed}), -1 color {minusEq}/{withMinus.Count} -> {(ok ? "PASS" : "FAIL")}"));
        Console.WriteLine($"informe: {reportPath.Replace('\\', '/')}");
        return ok ? 0 : 1;
    }

    private static Row Measure(MetricsEngine metrics, string source, JsonElement level)
    {
        string id = level.GetProperty("id").GetString() ?? "?";
        int[] caps = level.GetProperty("caps").EnumerateArray().Select(e => e.GetInt32()).ToArray();
        int[][] cuias = level.GetProperty("cuias").EnumerateArray().Select(c => c.EnumerateArray().Select(e => e.GetInt32()).ToArray()).ToArray();
        JsonElement diff = level.GetProperty("difficulty");
        JsonElement relief = level.GetProperty("relief");
        BoardState state = BoardBuilder.Build(caps, cuias).State
            ?? throw new InvalidOperationException($"{id}: tablero del prototipo no valido");
        ParResult par = metrics.Par(state);
        PathMetrics path = metrics.Path(state, par.Solution);
        (int Color, int Par)? minus = Relief.BestMinusColor(metrics, caps, cuias);
        int k = cuias.SelectMany(c => c).Distinct().Count();
        return new Row(
            source, id, k, cuias.Count(c => c.Length == 0), cuias.Length,
            diff.GetProperty("par").GetInt32(), par.Par,
            diff.GetProperty("complexity").GetInt32(), MetricsEngine.Complexity(state),
            diff.GetProperty("branching").GetDouble(), path.Branching, path.SolverMovesBranching,
            diff.GetProperty("traps").GetDouble(), path.Traps, path.TrapPairs, path.TrapUnknown,
            NullableInt(relief, "minus_color"), NullableInt(relief, "minus_color_par"),
            minus?.Color, minus?.Par);
    }

    private static int? NullableInt(JsonElement obj, string key) =>
        obj.TryGetProperty(key, out JsonElement v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;

    /// <summary>
    /// Iguales a 2 decimales: la cifra de C# se redondea primero como la publicó el prototipo (ramificación con 2 decimales,
    /// trampas con 3; Python redondea al par) y después las dos a 2 decimales.
    /// </summary>
    private static bool Same2(double proto, double ours, int protoDecimals = 2) =>
        Math.Round(proto, 2, MidpointRounding.ToEven)
        == Math.Round(Math.Round(ours, protoDecimals, MidpointRounding.ToEven), 2, MidpointRounding.ToEven);

    /// <summary>
    /// La cifra de C# tal como se compara en <see cref="Same2"/>: redondeada al par (<c>ToEven</c>) a los decimales con los
    /// que publicó el prototipo. El formato <c>0.00</c> redondea alejándose de cero (6,125 → 6,13) y el informe mostraría
    /// otra cifra que la comparada (6,12).
    /// </summary>
    private static string Ours(double ours, int protoDecimals) =>
        Math.Round(ours, protoDecimals, MidpointRounding.ToEven).ToString(protoDecimals == 2 ? "0.00" : "0.000", CultureInfo.InvariantCulture);

    private static string Fmt(int? v) => v?.ToString(CultureInfo.InvariantCulture) ?? "-";

    private static string Report(List<Row> rows, int parEq, int complexityEq, int branchingEq, int solverMovesEq, int trapsEq, int minusEq, int minusTotal, bool ok)
    {
        var sb = new StringBuilder();
        void W(string line = "") => sb.Append(line).Append('\n');
        int n = rows.Count;
        W("# Paridad de métricas con el prototipo (`level-generator --parity`)");
        W();
        W("Generado por `dotnet run --project tools/level-generator -- --parity`: no se edita a mano y no lleva tiempos (dos "
          + "ejecuciones dan los mismos bytes). Fuentes: GDD 03 §7 (métricas), §13 (variante −1 color), §17-§18 y 09 R26 "
          + "(paridad sobre los estados exportados: el PRNG del núcleo no es el MT19937 del prototipo, así que `bot_fail` y `D` "
          + "no se comparan). Las métricas se calculan con `AF.Core` y `AF.Solver` (A* w = 1 a 300.000 en `run`; trampas con A* "
          + "a 3.000 y 120 pares como máximo, muestreados por el índice entero ⌊k·n/120⌋ [P]; −1 color con A* a 100.000).");
        W();
        W($"- Niveles: {n} ({string.Join(" y ", Sources.Select(s => $"`{s}`"))}).");
        W(string.Create(CultureInfo.InvariantCulture, $"- Par igual: **{parEq}/{n}** (umbral {n}/{n})."));
        W(string.Create(CultureInfo.InvariantCulture, $"- Complejidad igual: **{complexityEq}/{n}** (umbral {n}/{n})."));
        W(string.Create(CultureInfo.InvariantCulture, $"- Ramificación igual a 2 decimales: **{branchingEq}/{n}** (umbral {n - 2}/{n}), contando los `MOVIMIENTOS` literales de 03 §5 (la definición de 03 §7). Con `|SolverMoves|`, que funde además orígenes y destinos idénticos, coincidirían {solverMovesEq}/{n}: por eso la métrica usa la definición literal [P]."));
        W(string.Create(CultureInfo.InvariantCulture, $"- Trampas iguales a 2 decimales: **{trapsEq}/{n}** (umbral {n - 2}/{n})."));
        W(string.Create(CultureInfo.InvariantCulture, $"- Color y par de −1 color iguales: **{minusEq}/{minusTotal}** (los niveles en los que el prototipo los publica)."));
        W($"- Resultado: **{(ok ? "PASS" : "FAIL")}**.");
        W();
        W("| Fuente | Nivel | K | E | Par proto / C# | Compl. | Ramif. proto / C# | Trampas proto / C# (pares) | −1 color proto / C# |");
        W("|---|---|---|---|---|---|---|---|---|");
        foreach (Row r in rows)
        {
            string branch = Same2(r.ProtoBranching, r.Branching) ? $"{r.ProtoBranching:0.00} / {Ours(r.Branching, 2)}" : $"**{r.ProtoBranching:0.00} / {Ours(r.Branching, 2)}**";
            string traps = Same2(r.ProtoTraps, r.Traps, 3) ? $"{r.ProtoTraps:0.000} / {Ours(r.Traps, 3)}" : $"**{r.ProtoTraps:0.000} / {Ours(r.Traps, 3)}**";
            string minus = $"{Fmt(r.ProtoMinusColor)} ({Fmt(r.ProtoMinusPar)}) / {Fmt(r.MinusColor)} ({Fmt(r.MinusPar)})";
            W(string.Create(CultureInfo.InvariantCulture,
                $"| `{r.Source}` | {r.Id} | {r.K} | {r.E} | {r.ProtoPar} / {r.Par} | {r.ProtoComplexity} / {r.Complexity} | {branch} | {traps} ({r.TrapPairs}{(r.TrapUnknown > 0 ? $", {r.TrapUnknown} sin decidir" : "")}) | {minus} |"));
        }

        W();
        W("## Decisiones para la paridad");
        W();
        W("1. **Ruta óptima de referencia.** Con varias soluciones de la misma longitud, la ramificación y las trampas dependen de "
          + "la ruta medida. `AF.Solver` expande en el orden canónico de las cuias y el prototipo en el orden real del estado "
          + "guardado. `Metrics/ReferencePath.cs` repite el `ASTAR` de 03 §5 en el orden del prototipo (encola `(f, g, llegada)`) con "
          + "las reglas y la heurística de `AF.Core`/`AF.Solver`; se usa solo si su ruta tiene la longitud del par de `AF.Solver` [P].");
        W("2. **Ramificación = `|MOVIMIENTOS(S)|` literal de 03 §5** (legales y no fútiles, un vacío por capacidad y mecánica y por "
          + "origen), que es la definición de 03 §7. `AF.Solver.SolverMoves` funde además orígenes y destinos idénticos y daría "
          + "otra cifra en los tableros con dos cuias iguales [P].");
        W("3. **Trampas:** los pares son esos `MOVIMIENTOS` en el orden real de cada estado de la ruta; con más de 120 se toma el "
          + "índice entero ⌊k·n/120⌋ [P] en lugar de `int(k·n/120.0)` del prototipo.");
        W("4. **Redondeo:** las cifras del prototipo están redondeadas por Python (al par en el punto medio); la comparación "
          + "redondea las de C# igual (`MidpointRounding.ToEven`) y la tabla imprime la cifra de C# ya redondeada así, la misma que "
          + "se compara.");
        W();
        W("## Diferencias");
        W();
        List<Row> diffs = rows.Where(r => !Same2(r.ProtoBranching, r.Branching) || !Same2(r.ProtoTraps, r.Traps, 3)).ToList();
        if (diffs.Count == 0)
        {
            W("Ninguna: ramificación y trampas coinciden a 2 decimales en todos los niveles.");
        }
        else
        {
            W("| Nivel | Ramif. proto / C# | Trampas proto / C# | Pares C# |");
            W("|---|---|---|---|");
            foreach (Row r in diffs)
            {
                W(string.Create(CultureInfo.InvariantCulture,
                    $"| {r.Id} (`{r.Source}`) | {r.ProtoBranching:0.00} / {Ours(r.Branching, 2)} | {r.ProtoTraps:0.000} / {Ours(r.Traps, 3)} | {r.TrapPairs} |"));
            }

            W();
            W("Causa: otra ruta óptima del mismo largo (la búsqueda de referencia no la encontró dentro del presupuesto, o el desempate "
              + "difiere en un estado reabierto), o un muestreo distinto con más de 120 pares.");
        }

        return sb.ToString();
    }
}
