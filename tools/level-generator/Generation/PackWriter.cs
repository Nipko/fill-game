// Empaquetado af-level/1 con pack_schema 2 (08 §4.1 y §4.4; D-007): un pack por mundo y vuelta, mundo_{mm}_v1.json.
//   - claves en orden estable (el del ejemplo de 08 §4.1), InvariantCulture y números con decimales fijos
//     (bot_fail con 3, branching, traps y D con 2);
//   - UTF-8 sin BOM y LF; minificado (03 §2) con un nivel por línea [P] para que los diffs se lean;
//   - sin solution, safe_path ni qa (08 §4.1).
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

/// <summary>Objeto JSON con orden de claves estable.</summary>
internal sealed class JObj : List<KeyValuePair<string, object?>>
{
    public void Add(string key, object? value) => Add(new KeyValuePair<string, object?>(key, value));
}

/// <summary>Número ya formateado (decimales fijos).</summary>
internal sealed record JNum(string Text);

/// <summary>Un nivel listo para escribir.</summary>
internal sealed record PackLevel(LevelPlan Plan, Candidate Chosen, List<ReliefBoard> Relief);

internal static class PackWriter
{
    public const string GeneratorVersion = "1.0.0";
    public const string SchemaFile = "schemas/af-level-1.schema.json";
    public const int PackSchema = 2;

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static string Fixed(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero).ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    public static byte[] Bytes(string text) => Utf8NoBom.GetBytes(text);

    public static string PackId(WorldInfo world, int lap) => string.Create(CultureInfo.InvariantCulture, $"mundo_{world.World:00}_v{lap}");

    public static string Write(CampaignSpec spec, WorldInfo world, IReadOnlyList<PackLevel> levels)
    {
        var header = new JObj
        {
            { "pack_schema", PackSchema },
            { "schema_file", SchemaFile },
            { "pack_id", PackId(world, spec.Lap) },
            { "kind", "campaign" },
            { "generator", new JObj { { "tool", "level-generator" }, { "version", GeneratorVersion } } },
            { "world", world.World },
            { "world_id", world.WorldId },
            { "lap", spec.Lap },
            { "water_base", world.WaterBase },
            { "pool", spec.WorldPool(world).Select(t => (object?)t.Id).ToList() },
            { "k_min", levels.Min(l => l.Chosen.K) },
            { "k_max", levels.Max(l => l.Chosen.K) },
            { "max_risk_pairs", levels.Max(l => l.Plan.MaxRiskPairs) },
            { "cap_default", Cuia.DefaultCapacity },
            { "empties_default", world.First <= spec.TwoEmptiesUntil ? 2 : 1 },
            { "cycle20", true },
        };

        var sb = new StringBuilder();
        string head = Min(header);
        sb.Append(head, 0, head.Length - 1).Append(",\"levels\":[\n");
        for (int i = 0; i < levels.Count; i++)
        {
            sb.Append(Min(Level(spec, levels[i])));
            sb.Append(i + 1 < levels.Count ? ",\n" : "\n");
        }

        sb.Append("]}\n");
        return sb.ToString();
    }

    public static JObj Level(CampaignSpec spec, PackLevel pl)
    {
        LevelPlan plan = pl.Plan;
        Candidate c = pl.Chosen;
        BoardData b = c.Board;
        var gen = new JObj
        {
            { "tool", "level-generator" },
            { "version", GeneratorVersion },
            { "source", c.Source },
        };
        if (c.Source != "fill")
        {
            gen.Add("depth", c.Depth);
        }

        gen.Add("candidates", c.Attempt + 1);
        var difficulty = new JObj
        {
            { "label", c.MeasuredLabel },
            { "bot_fail", new JNum(Fixed(c.Bot, 3)) },
            { "par", c.Par },
            { "branching", new JNum(Fixed(c.Branching, 2)) },
            { "traps", new JNum(Fixed(c.Traps, 2)) },
            { "complexity", c.Complexity },
            { "D", new JNum(Fixed(c.D, 2)) },
            { "band", plan.Band },
        };
        return new JObj
        {
            { "schema", "af-level/1" },
            { "id", plan.Id },
            { "level", plan.Level },
            { "world", plan.World.World },
            { "lap", spec.Lap },
            { "seed", plan.SeedText },
            { "gen", gen },
            { "water_base", plan.World.WaterBase },
            { "palette", c.Tints.Palette.Select(t => (object?)t.Id).ToList() },
            { "units_per_color", Units(c.State, c.Tints.Palette) },
            { "caps", b.Caps.Select(x => (object?)x).ToList() },
            { "cuias", Cuias(b.Cuias) },
            { "attrs", Attrs(b.Attrs) },
            { "murky", b.Hidden.Length > 0 },
            { "hidden", b.Hidden.Select(h => (object?)new List<object?> { h[0], h[1] }).ToList() },
            { "difficulty", difficulty },
            { "risk_pairs", c.Tints.RiskPairs },
            { "intro_mechanic", plan.Intro },
            { "skippable", plan.Intro is null },
            { "hint_supported", true },
            { "relief", pl.Relief.Select(r => (object?)Relief(r)).ToList() },
        };
    }

    private static JObj Relief(ReliefBoard r)
    {
        var o = new JObj { { "type", r.Type } };
        if (r.Removed is int removed)
        {
            o.Add("removed", removed);
        }

        o.Add("caps", r.Board.Caps.Select(x => (object?)x).ToList());
        o.Add("cuias", Cuias(r.Board.Cuias));
        o.Add("attrs", r.Board.HasMechanics ? Attrs(r.Board.Attrs) : null);
        o.Add("par", r.Par);
        o.Add("bot_fail", new JNum(Fixed(r.BotFail, 3)));
        return o;
    }

    private static object Units(BoardState state, IReadOnlyList<TintInfo> palette)
    {
        var odd = palette.Where(t => state.UnitsOf(t.Engine) != UnitsPerColor.DefaultUnits).OrderBy(t => t.Engine).ToList();
        if (odd.Count == 0)
        {
            return UnitsPerColor.DefaultUnits;
        }

        var o = new JObj();
        foreach (TintInfo t in odd)
        {
            o.Add(t.Engine.ToString(CultureInfo.InvariantCulture), state.UnitsOf(t.Engine));
        }

        return o;
    }

    private static List<object?> Cuias(int[][] cuias) =>
        cuias.Select(c => (object?)c.Select(x => (object?)x).ToList()).ToList();

    private static List<object?> Attrs(MechanicSpec[] attrs) => attrs.Select(a => a.IsNone ? null : (object?)Attr(a)).ToList();

    private static JObj Attr(MechanicSpec a)
    {
        var o = new JObj { { "type", MechanicTypes.ToKey(a.Type) } };
        if (a.Key != 0)
        {
            o.Add("key", a.Key);
        }

        if (a.Partner >= 0)
        {
            o.Add("partner", a.Partner);
        }

        if (a.N != 0)
        {
            o.Add("n", a.N);
        }

        if (a.K != 0)
        {
            o.Add("k", a.K);
        }

        return o;
    }

    /// <summary>JSON minificado.</summary>
    public static string Min(object? value)
    {
        var sb = new StringBuilder();
        WriteMin(sb, value);
        return sb.ToString();
    }

    private static void WriteMin(StringBuilder sb, object? value)
    {
        switch (value)
        {
            case null:
                sb.Append("null");
                break;
            case JObj o:
                sb.Append('{');
                for (int i = 0; i < o.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    Str(sb, o[i].Key);
                    sb.Append(':');
                    WriteMin(sb, o[i].Value);
                }

                sb.Append('}');
                break;
            case List<object?> list:
                sb.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    WriteMin(sb, list[i]);
                }

                sb.Append(']');
                break;
            case bool b:
                sb.Append(b ? "true" : "false");
                break;
            case int i:
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
                break;
            case JNum n:
                sb.Append(n.Text);
                break;
            case string s:
                Str(sb, s);
                break;
            default:
                throw new ArgumentException("Tipo JSON no admitido: " + value.GetType().Name);
        }
    }

    private static void Str(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (char ch in s)
        {
            switch (ch)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                default:
                    if (ch < 0x20 || ch > 0x7E)
                    {
                        sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(ch);
                    }

                    break;
            }
        }

        sb.Append('"');
    }
}
