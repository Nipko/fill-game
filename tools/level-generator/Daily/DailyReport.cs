// Informe .md de una generación del pool del Reto del río (R3; nota docs/dev/reto-del-rio.md §13). Lleva lo que el pack no
// lleva (03 §16: el pool se publica sin métricas): por banda, bot_fail, par, par/K y K (mínimo, mediana, media y máximo), los
// candidatos y los rechazos por motivo, los días fuera de ventana (R-Q11), los tableros con más pares de riesgo que el tope de
// su banda (R-Q1b), el uso del respaldo y los tiempos; después, una fila por tablero. Todo el texto con InvariantCulture.
// Los tiempos cambian de una ejecución a otra: el pack es determinista byte a byte, el informe no.
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AmazonFill.Tools.LevelGenerator.Daily;

internal static class DailyReport
{
    public static string Write(DailySpec spec, DailyRun run, string command, string packPath, byte[] packBytes)
    {
        var sb = new StringBuilder();
        void L(string line = "") => sb.Append(line).Append('\n');
        string F(double v, int d = 2) => v.ToString("F" + d.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        string I(long v) => v.ToString(CultureInfo.InvariantCulture);

        L($"# Reto del río: informe del pool `{spec.PackId}`");
        L();
        L("Generado por `tools/level-generator --daily` (tarea R3 del encargo `docs/dev/traspaso/spark-reto-del-rio.md`; nota de diseño `docs/dev/reto-del-rio.md` §13). No se edita a mano: se regenera con el comando.");
        L();
        L($"- Comando: `{command}`");
        L($"- Especificación: `{spec.Path}` ({spec.Name}); tintes y pares de riesgo de `{spec.TintsFrom}`.");
        L($"- Pack: `{packPath}`, {I(packBytes.Length)} bytes, SHA-256 `{Convert.ToHexStringLower(SHA256.HashData(packBytes))}`.");
        L($"- Tableros: {I(run.Boards.Count)}; errores: {I(run.Errors.Count)}.");
        L("- **Estado [P]:** la especificación está pendiente de R-Q1b (paleta de B2 con K = 9 y de B3, nota §1.1), de la fórmula del agua de R-Q2 y de R-Q11 (días fuera de ventana, nota §1.6). Este pool no se publica (R5) hasta que el dueño responda.");
        L();

        L("## Resumen por banda");
        L();
        L("| Banda | Tableros | K (tableros) | Ventana del bot | bot_fail mín · mediana · media · máx | par mín · mediana · media · máx | par/K mediana | Candidatos (media por día) | En ventana | Fuera de ventana | Con respaldo | Pares > tope |");
        L("|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (DailyBand band in spec.Bands)
        {
            List<DailyBoard> bs = run.Boards.Where(b => b.Band == band.Band).ToList();
            if (bs.Count == 0)
            {
                continue;
            }

            string ks = string.Join(", ", bs.GroupBy(b => b.K).OrderBy(g => g.Key).Select(g => $"{I(g.Key)}: {I(g.Count())}"));
            List<double> bots = bs.Select(b => b.Bot).Order().ToList();
            List<double> pars = bs.Select(b => (double)b.Par).Order().ToList();
            List<double> perK = bs.Select(b => (double)b.Par / b.K).Order().ToList();
            int tried = bs.Sum(b => b.Tried);
            L($"| B{I(band.Band)} | {I(bs.Count)} | {ks} | {F(band.BotFailMin)}-{F(band.BotFailMax)} | {F(bots[0], 3)} · {F(Median(bots), 3)} · {F(bots.Average(), 3)} · {F(bots[^1], 3)} | {I((long)pars[0])} · {F(Median(pars), 1)} · {F(pars.Average(), 1)} · {I((long)pars[^1])} | {F(Median(perK))} | {I(tried)} ({F((double)tried / bs.Count, 1)}) | {I(bs.Count(b => b.InWindow))} | {I(bs.Count(b => !b.InWindow))} | {I(bs.Count(b => b.UsedFallback))} | {I(bs.Count(b => b.OverRiskCap))} |");
        }

        L();
        L("## Rechazos por banda y motivo");
        L();
        L("Candidatos descartados antes del elegido (los «fuera de la ventana del bot» incluyen, en los días fuera de ventana, el que acabó elegido como el más cercano).");
        L();
        L("| Banda | Motivo | Candidatos |");
        L("|---|---|---|");
        foreach (IGrouping<int, DailyBoard> g in run.Boards.GroupBy(b => b.Band).OrderBy(g => g.Key))
        {
            var reasons = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> kv in g.SelectMany(b => b.Rejects))
            {
                reasons[kv.Key] = (reasons.TryGetValue(kv.Key, out int n) ? n : 0) + kv.Value;
            }

            if (reasons.Count == 0)
            {
                L($"| B{I(g.Key)} | (ninguno) | 0 |");
            }

            foreach (KeyValuePair<string, int> kv in reasons)
            {
                L($"| B{I(g.Key)} | {kv.Key} | {I(kv.Value)} |");
            }
        }

        L();
        L("## Días fuera de la ventana del bot (R-Q11) [P]");
        L();
        L("Ningún candidato entró en la ventana; se guardó el más cercano que A* resuelve con par >= 2K. `level-qa --daily` (R4) lo da como fallo.");
        L();
        List<DailyBoard> outside = run.Boards.Where(b => !b.InWindow).ToList();
        if (outside.Count == 0)
        {
            L("Ninguno.");
        }
        else
        {
            L("| id | K | Fuente | bot_fail | Ventana | Candidatos |");
            L("|---|---|---|---|---|---|");
            foreach (DailyBoard b in outside)
            {
                DailyBand band = spec.Band(b.Band);
                L($"| {b.Id} | {I(b.K)} | {b.Source} | {F(b.Bot, 3)} | {F(band.BotFailMin)}-{F(band.BotFailMax)} | {I(b.Tried)} |");
            }
        }

        L();
        L("## Tableros con más pares de riesgo que el tope de su banda (R-Q1b) [P]");
        L();
        L("La paleta de R-Q1 no admite ese K con el tope (nota §1.1): el tablero lleva el mínimo de pares posible con ese K; no cambian ni K ni la paleta.");
        L();
        var over = run.Boards.Where(b => b.OverRiskCap).GroupBy(b => (b.Band, b.K)).OrderBy(g => g.Key.Band).ThenBy(g => g.Key.K).ToList();
        if (over.Count == 0)
        {
            L("Ninguno.");
        }
        else
        {
            L("| Banda | K | Tope de la banda | Mínimo posible | Tableros | Pares por tablero |");
            L("|---|---|---|---|---|---|");
            foreach (var g in over)
            {
                DailyBoard first = g.First();
                string pairs = string.Join(", ", g.GroupBy(b => b.Palette.RiskPairs).OrderBy(p => p.Key).Select(p => $"{I(p.Key)}: {I(p.Count())}"));
                L($"| B{I(g.Key.Band)} | {I(g.Key.K)} | {I(first.RiskCap)} | {I(first.EffectiveRiskCap)} | {I(g.Count())} | {pairs} |");
            }
        }

        L();
        L("## Tiempos");
        L();
        L($"- Total (pared): {F(run.TotalMilliseconds / 1000.0, 1)} s con {I(run.Threads)} hilos.");
        foreach (KeyValuePair<int, long> kv in run.BandMilliseconds)
        {
            int n = run.Boards.Count(b => b.Band == kv.Key);
            long max = run.Boards.Where(b => b.Band == kv.Key).Max(b => b.Milliseconds);
            L($"- B{I(kv.Key)}: {F(kv.Value / 1000.0, 1)} s de CPU sumados en {I(n)} tableros ({F((double)kv.Value / n, 0)} ms por tablero; el más lento, {I(max)} ms).");
        }

        if (run.Errors.Count > 0)
        {
            L();
            L("## Errores");
            L();
            foreach (string e in run.Errors)
            {
                L($"- {e}");
            }
        }

        L();
        L("## Tableros");
        L();
        L("| id | K | Vacíos | Fuente | Candidato | bot_fail | par | Pares de riesgo | Contactos de riesgo | Mundo | Agua | Marcas |");
        L("|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (DailyBoard b in run.Boards)
        {
            var marks = new List<string>();
            if (!b.InWindow)
            {
                marks.Add("fuera de ventana");
            }

            if (b.UsedFallback)
            {
                marks.Add("respaldo");
            }

            if (b.OverRiskCap)
            {
                marks.Add("pares > tope");
            }

            L($"| {b.Id} | {I(b.K)} | {I(b.Empties)} | {b.Source} | {I(b.Attempt + 1)} | {F(b.Bot, 3)} | {I(b.Par)} | {I(b.Palette.RiskPairs)} | {I(b.Palette.RiskAdjacent)} | {I(b.World.World)} | {b.World.WaterBase} | {(marks.Count == 0 ? "" : string.Join(", ", marks))} |");
        }

        return sb.ToString();
    }

    private static double Median(List<double> sorted) =>
        sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[(sorted.Count / 2) - 1] + sorted[sorted.Count / 2]) / 2;
}
