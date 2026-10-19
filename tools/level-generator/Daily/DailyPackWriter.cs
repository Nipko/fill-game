// Pack del Reto del río daily_pool_v1 (R3; af-level/1 con kind = daily: 03 §16; 08 §4.1; esquema schemas/af-level-1.schema.json).
//   - cabecera: pack_schema 2 (el de los packs sin la Tinaja ni las variantes del 3: Generation/PackWriter.PackSchema; sin
//     mecánicas el pack valida con el 2), schema_file, pack_id, kind "daily", generator, pool (los tintes de todas las bandas
//     de la especificación: debut <= el mayor debut_until), k_min y k_max de los tableros escritos, max_risk_pairs (el mayor
//     de los tableros y de los topes de las bandas; el tope por banda lo comprueba level-qa --daily con la especificación),
//     cap_default 4 y empties_default 1 [P] (B2 y B3; B1 lleva 2 y cada tablero sus caps); sin world, world_id, lap ni
//     water_base (opcionales en daily: el pool mezcla mundos) ni cycle20 (el Reto no sigue el ciclo de 20);
//   - cada tablero, con las claves en el orden de PackWriter.Level y slot en lugar de level: schema, id, slot {band, day},
//     world, seed, gen {tool, version, source, depth (salvo fill), candidates}, water_base (R-Q2 [P]), palette,
//     units_per_color (4), caps, cuias, attrs (todo null: sin mecánicas), murky false, hidden [], risk_pairs,
//     intro_mechanic null, skippable false (sin Salto del Boto en el Reto: 06 §4.1; 04 §2.6 e), hint_supported true (Morpho
//     permitido: 06 §4.1; R-Q8) y relief [] (03 §16). Sin difficulty, solution, safe_path ni qa: las métricas solo van al
//     informe (DailyReport);
//   - tableros en orden de banda y día; JSON minificado con un tablero por línea [P] como PackWriter, UTF-8 sin BOM y LF,
//     números enteros en decimal (nunca hexadecimal).
using System.Text;
using AF.Core.Board;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;

namespace AmazonFill.Tools.LevelGenerator.Daily;

internal static class DailyPackWriter
{
    public const int PackSchema = PackWriter.PackSchema;
    public const int EmptiesDefault = 1;

    public static string FileName(DailySpec spec) => spec.PackId + ".json";

    public static string Write(DailySpec spec, DailyTints tints, IReadOnlyList<DailyBoard> boards)
    {
        if (boards.Count == 0)
        {
            throw new ArgumentException("el pack necesita al menos un tablero", nameof(boards));
        }

        DailyBand widest = spec.Bands.OrderByDescending(b => b.DebutUntil).First();
        var header = new JObj
        {
            { "pack_schema", PackSchema },
            { "schema_file", PackWriter.SchemaFile },
            { "pack_id", spec.PackId },
            { "kind", "daily" },
            { "generator", new JObj { { "tool", "level-generator" }, { "version", PackWriter.GeneratorVersion } } },
            { "pool", tints.Pool(widest).Select(t => (object?)t.Id).ToList() },
            { "k_min", boards.Min(b => b.K) },
            { "k_max", boards.Max(b => b.K) },
            { "max_risk_pairs", Math.Max(boards.Max(b => b.Palette.RiskPairs), spec.Bands.Max(b => b.MaxRiskPairs)) },
            { "cap_default", Cuia.DefaultCapacity },
            { "empties_default", EmptiesDefault },
        };

        var sb = new StringBuilder();
        string head = PackWriter.Min(header);
        sb.Append(head, 0, head.Length - 1).Append(",\"levels\":[\n");
        for (int i = 0; i < boards.Count; i++)
        {
            sb.Append(PackWriter.Min(Level(boards[i])));
            sb.Append(i + 1 < boards.Count ? ",\n" : "\n");
        }

        sb.Append("]}\n");
        return sb.ToString();
    }

    public static JObj Level(DailyBoard b)
    {
        var gen = new JObj
        {
            { "tool", "level-generator" },
            { "version", PackWriter.GeneratorVersion },
            { "source", b.Source },
        };
        if (b.Source != "fill")
        {
            gen.Add("depth", b.Depth);
        }

        gen.Add("candidates", b.Attempt + 1);
        return new JObj
        {
            { "schema", "af-level/1" },
            { "id", b.Id },
            { "slot", new JObj { { "band", b.Band }, { "day", b.Day } } },
            { "world", b.World.World },
            { "seed", b.SeedText },
            { "gen", gen },
            { "water_base", b.World.WaterBase },
            { "palette", b.Palette.Palette.Select(t => (object?)t.Id).ToList() },
            { "units_per_color", UnitsPerColor.DefaultUnits },
            { "caps", b.Board.Caps.Select(x => (object?)x).ToList() },
            { "cuias", b.Board.Cuias.Select(c => (object?)c.Select(x => (object?)x).ToList()).ToList() },
            { "attrs", b.Board.Attrs.Select(_ => (object?)null).ToList() },
            { "murky", false },
            { "hidden", new List<object?>() },
            { "risk_pairs", b.Palette.RiskPairs },
            { "intro_mechanic", null },
            { "skippable", false },
            { "hint_supported", true },
            { "relief", new List<object?>() },
        };
    }

    /// <summary>Comprueba lo que el pack promete sin el esquema: null si cumple; si no, el motivo.</summary>
    public static string? CheckBoard(DailyBoard b)
    {
        if (b.Board.Attrs.Any(a => !a.IsNone) || b.Board.Hidden.Length > 0)
        {
            return "lleva mecanicas u ocultas";
        }

        if (b.Board.Caps.Any(c => c != Cuia.DefaultCapacity) || b.Board.Caps.Length != b.K + b.Empties)
        {
            return "las cuias no son K + vacios de capacidad 4";
        }

        if (b.Board.Cuias.Count(c => c.Length == 0) != b.Empties || b.Board.Cuias.Any(c => c.Length != 0 && c.Length != Cuia.DefaultCapacity))
        {
            return "las cuias no son K llenas y los vacios de la banda";
        }

        if (b.Par < 2 * b.K)
        {
            return "par < 2K";
        }

        return b.Palette.Palette.Count != b.K ? "la paleta no tiene K tintes" : null;
    }
}
