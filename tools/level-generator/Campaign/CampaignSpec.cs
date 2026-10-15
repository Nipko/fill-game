// Especificación de la campaña 11-200 (tools/level-generator/specs/campaign_v1.json): datos copiados de D-009, D-007,
// la propuesta (c.1-c.4, d.1-d.4) y el GDD vigente (02 §8, §9.2, §13; 03 §4.4, §4.5, §11, §12; glosario §8, §9, §11;
// 08 §9). Este archivo solo lee y deriva el plan de cada nivel; las reglas del juego viven en AF.Core y AF.Solver.
// Lo enlazan tools/level-generator y tools/level-qa (un solo lector de la especificación).
// Los códigos del calendario y su lectura sobre un attrs publicado (MechanicCodes) también viven aquí: con las variantes de la
// 1.0 (P5 de la tanda 4) un tipo de AF.Core ya no basta para saber el código (liana con n es Ln; brote con n = 2, B2).
// Una especificación puede «extender» otra (fixtures negativos de specs/tests/): "extends" apunta al archivo base y
// "calendar" o "seasons" sustituyen solo las entradas que traen; cualquier otra clave sustituye la del base.
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AF.Core.Mechanics;

namespace AmazonFill.Tools.LevelGenerator.Campaign;

/// <summary>Error de lectura de la especificación (código 2 en las herramientas).</summary>
internal sealed class CampaignSpecException(string message) : Exception(message);

/// <summary>Rol de un nivel en el ciclo de 20 de D-009 (propuesta c.3).</summary>
internal enum SlotRole
{
    Recovery,
    Comfort,
    Challenge,
    Remanso,
    Milestone,
}

/// <summary>Intervalo de fallo del bot con notación matemática: "[0.20, 0.60]", "[0.00, 0.30)".</summary>
internal sealed record Interval(double Lo, bool LoClosed, double Hi, bool HiClosed, string Text)
{
    private const double Eps = 1e-9;

    public bool Contains(double x) =>
        (LoClosed ? x >= Lo - Eps : x > Lo + Eps) && (HiClosed ? x <= Hi + Eps : x < Hi - Eps);

    /// <summary>Distancia de <paramref name="x"/> al intervalo (0 si está dentro): ordena candidatos fuera de ventana.</summary>
    public double Distance(double x) => Contains(x) ? 0 : Math.Min(Math.Abs(x - Lo), Math.Abs(x - Hi));

    public static Interval Parse(string text)
    {
        Match m = Regex.Match(text.Trim(), @"^([\[(])\s*([0-9.]+)\s*,\s*([0-9.]+)\s*([\])])$");
        if (!m.Success)
        {
            throw new CampaignSpecException($"intervalo no valido: '{text}' (se espera p. ej. [0.20, 0.60] o [0.00, 0.30))");
        }

        double lo = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        double hi = double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
        return new Interval(lo, m.Groups[1].Value == "[", hi, m.Groups[4].Value == "]", text.Trim());
    }

    public override string ToString() => Text;
}

internal sealed record TintInfo(string Id, int Engine, string NameEs, int Debut);

internal sealed record WorldInfo(int World, string WorldId, string Name, int First, int Last, string WaterBase, string WaterTint);

internal sealed record RangeInfo(
    int First, int Last, int KMin, int KMax, int CuiasMin, int CuiasMax, int Empties, int ParMin, int ParMax,
    int PctHardMin, int PctHardMax, int PctVeryHardMin, int PctVeryHardMax)
{
    public string Name => string.Create(CultureInfo.InvariantCulture, $"{First}-{Last}");
}

internal sealed record KPlanRow(int First, int Last, int Recovery, int Comfort, int Challenge, int Milestone);

/// <summary>
/// Fila fija de la rampa (02 §13, tabla única del arranque: niveles 11 y 12): K, vacíos, par [P] y ventana de fallo del bot.
/// El generador no relaja K ni los vacíos en estas filas y rechaza el candidato con el par fuera del rango; level-qa lo exige.
/// </summary>
internal sealed record RampRow(int Level, int K, int Empties, int ParMin, int ParMax, Interval Window)
{
    public bool ParOk(int par) => par >= ParMin && par <= ParMax;
}

internal sealed record PositionRule(int[] Positions, SlotRole Role, int Empties, string Source, string KKey, bool DebutSlot);

/// <summary>Mecánica del calendario (código de d.4) con su id, clase de Q14 A, debut de D-007 y temporada (d.1).</summary>
internal sealed record MechanicInfo(string Code, string Id, string Kind, int Debut, string Season, string? DebutKey, int K, int CaveCap)
{
    public bool IsFilter => Kind == "filter";

    public bool IsCounter => Kind == "counter";
}

internal sealed record CapacityInfo(string Code, string Id, int Cap, int Debut);

/// <summary>Celda del calendario d.4 ya interpretada.</summary>
internal sealed record CalendarCell(
    int Level,
    string Literal,
    bool Star,
    string? PureCode,
    IReadOnlyList<MechanicInfo> Mechanics,
    IReadOnlyList<CapacityInfo> Capacities,
    string? Mark,
    string? TintDebutName,
    IReadOnlyList<string> Notes)
{
    /// <summary>Puro (R, C o S): sin mecánica ni capacidad protagonista (d.4, «Comprobaciones de la tabla»).</summary>
    public bool IsPure => PureCode is not null;

    public bool Has(string code) => Mechanics.Any(m => m.Code == code);

    public bool HasCapacity(string code) => Capacities.Any(c => c.Code == code);

    /// <summary>Forma corta para informes: "Ho+Ch (D)".</summary>
    public string Short
    {
        get
        {
            string items = PureCode ?? string.Join("+", Mechanics.Select(m => m.Code).Concat(Capacities.Select(c => c.Code)));
            return (Star ? "*" : "") + items + (Mark is null ? "" : $" ({Mark})");
        }
    }
}

/// <summary>Plan de un nivel derivado de la especificación: lo que el generador produce y lo que level-qa exige.</summary>
internal sealed class LevelPlan
{
    public required int Level { get; init; }
    public required WorldInfo World { get; init; }
    public required RangeInfo Range { get; init; }

    /// <summary>Posición en el ciclo de 20 (1-20); 0 en la rampa 11-20.</summary>
    public required int Position { get; init; }
    public required bool Ramp { get; init; }
    public required SlotRole Role { get; init; }
    public required CalendarCell Cell { get; init; }
    public required int K { get; init; }
    public required int Empties { get; init; }

    /// <summary>Fuente del tablero base: walk, deep o fill (03 §4.4).</summary>
    public required string Source { get; init; }
    public required Interval Window { get; init; }

    /// <summary>Etiqueta máxima permitida en el slot: null (ninguna), "hard" o "very_hard".</summary>
    public required string? AllowedLabel { get; init; }

    /// <summary>Etiqueta objetivo del calendario: (D) → hard, (MD) → very_hard; null si no apunta a ninguna.</summary>
    public required string? TargetLabel { get; init; }

    /// <summary>Tablero de estreno (★) de una mecánica o capacidad.</summary>
    public required bool DebutBoard { get; init; }

    /// <summary>Fila fija de 02 §13 (niveles 11-12) o null.</summary>
    public RampRow? Row { get; init; }

    /// <summary>intro_mechanic del nivel (primer tablero de debut) o null.</summary>
    public required string? Intro { get; init; }
    public required int MaxRiskPairs { get; init; }
    public required string Season { get; init; }

    public IReadOnlyList<MechanicInfo> Mechanics => Cell.Mechanics;

    public IReadOnlyList<CapacityInfo> Capacities => Cell.Capacities;

    public string SeedText => string.Create(CultureInfo.InvariantCulture, $"amazonfill:v1:m{World.World:00}:v1:n{Level:0000}");

    public string Id => string.Create(CultureInfo.InvariantCulture, $"m{World.World:00}_v1_n{Level:0000}");

    /// <summary>Banda del JSON (03 §12; el remanso es comfort [P]: el esquema no tiene otro valor).</summary>
    public string Band => Role switch
    {
        SlotRole.Recovery => "recovery",
        SlotRole.Challenge => "challenge",
        SlotRole.Milestone => "milestone",
        _ => "comfort",
    };

    public string RoleName => Ramp ? "rampa" : Role switch
    {
        SlotRole.Recovery => "recuperacion",
        SlotRole.Comfort => DebutBoard ? "estreno" : "comodidad",
        SlotRole.Challenge => "reto",
        SlotRole.Remanso => "remanso",
        SlotRole.Milestone => "hito",
        _ => "?",
    };

    /// <summary>Cuantas mecánicas (no capacidades) lleva el nivel.</summary>
    public int MechanicCount => Mechanics.Count;

    /// <summary>
    /// Comodidad de referencia del remanso (propuesta c.4: p19 «puro, más fácil que la media de comodidad»): rol de comodidad
    /// del ciclo (p3-p5, p7-p8, p10-p11, p13-p14, p16-p17) que no es tablero de estreno. La usan la colocación y QA-REMANSO.
    /// </summary>
    public bool IsComfortReference => !Ramp && Role == SlotRole.Comfort && !DebutBoard;

    /// <summary>
    /// Se puede publicar fuera de la ventana del slot: solo los retos y el hito del ciclo (propuesta c.2: un reto que mide por
    /// debajo de su ventana se publica con la etiqueta que mide, sin chip; el generador lo registra). En recuperación,
    /// comodidad, estreno, remanso y rampa, un bot_fail fuera de la ventana falla el CI (03 §17 #3; 08 §4.1). La regla la
    /// aplican la colocación del generador y tools/level-qa (QA-WINDOW).
    /// </summary>
    public bool OutOfWindowAllowed => !Ramp && Role is SlotRole.Challenge or SlotRole.Milestone;
}

internal sealed class CampaignSpec
{
    public const string Star = "★";

    private readonly Dictionary<int, CalendarCell> _calendar = new();

    private CampaignSpec()
    {
    }

    public required string Path { get; init; }
    public required string SpecId { get; init; }
    public required int Lap { get; init; }
    public required int FirstLevel { get; init; }
    public required int LastLevel { get; init; }
    public required IReadOnlyList<TintInfo> Tints { get; init; }
    public required IReadOnlyList<(int A, int B)> RiskPairs { get; init; }
    public required IReadOnlyList<(int First, int Last, int Max)> MaxRiskPairsRows { get; init; }
    public required IReadOnlyList<WorldInfo> Worlds { get; init; }
    public required IReadOnlyList<RangeInfo> Ranges { get; init; }
    public required IReadOnlyList<KPlanRow> KPlan { get; init; }
    public required IReadOnlyList<PositionRule> Positions { get; init; }
    public required IReadOnlyList<MechanicInfo> MechanicTable { get; init; }
    public required IReadOnlyList<CapacityInfo> CapacityTable { get; init; }
    public required IReadOnlyDictionary<int, string> Seasons { get; init; }
    public required IReadOnlyDictionary<string, Interval> Windows { get; init; }
    public required int RampFirst { get; init; }
    public required int RampLast { get; init; }
    public required int RampK { get; init; }
    public required int RampEmpties { get; init; }
    public required Interval RampWindow { get; init; }
    public required IReadOnlyDictionary<int, RampRow> RampRows { get; init; }
    public required int CycleFrom { get; init; }
    public required int TwoEmptiesUntil { get; init; }
    public required int ChallengeTwoEmptiesUntil { get; init; }
    public required int FirstHard { get; init; }
    public required int FirstVeryHard { get; init; }
    public required int[] Labeled4159 { get; init; }
    public required double HardThreshold { get; init; }
    public required double VeryHardThreshold { get; init; }

    /// <summary>
    /// Tope de mecánicas distintas por nivel (02 §9.2 regla 4; D-009): <c>mechanics_per_level.max</c>, 2 si la especificación no
    /// lo trae (campaign_v1, 11-200, entera por debajo del 300).
    /// </summary>
    public int MaxMechanics { get; init; } = DefaultMaxMechanics;

    /// <summary>
    /// Primer nivel con el tope alto («3 después del 300», 02 §9.2 regla 4): <c>mechanics_per_level.raised_from_level</c>, o
    /// null si la especificación no lo trae (nunca sube). El tope alto solo vale en retos e hitos (<see cref="MaxMechanicsAt"/>).
    /// </summary>
    public int? RaisedMechanicsFrom { get; init; }

    /// <summary>Tope alto (<c>mechanics_per_level.raised_max</c>; 3 en 02 §9.2 regla 4).</summary>
    public int RaisedMaxMechanics { get; init; } = DefaultMaxMechanics;

    /// <summary>Tope por defecto de mecánicas por nivel (02 §9.2 regla 4: «máximo 2 mecánicas distintas por nivel»).</summary>
    public const int DefaultMaxMechanics = 2;

    /// <summary>Regla del fixture que se espera ver fallar (solo specs/tests), o null.</summary>
    public string? ExpectRule { get; init; }

    public IReadOnlyDictionary<int, CalendarCell> Calendar => _calendar;

    public static int PositionOf(int level) => ((level - 1) % 20) + 1;

    public TintInfo Tint(string id) => Tints.FirstOrDefault(t => t.Id == id)
        ?? throw new CampaignSpecException($"tinte desconocido '{id}' (glosario sec. 9)");

    public TintInfo TintByEngine(int engine) => Tints.FirstOrDefault(t => t.Engine == engine)
        ?? throw new CampaignSpecException($"indice de motor sin tinte: {engine}");

    public WorldInfo WorldOf(int level) => Worlds.FirstOrDefault(w => w.First <= level && level <= w.Last)
        ?? throw new CampaignSpecException($"el nivel {level} no cae en ningun mundo de la especificacion");

    public RangeInfo RangeOf(int level) => Ranges.FirstOrDefault(r => r.First <= level && level <= r.Last)
        ?? throw new CampaignSpecException($"el nivel {level} no cae en ningun rango de 03 sec. 11");

    public int MaxRiskPairsAt(int level)
    {
        foreach ((int first, int last, int max) in MaxRiskPairsRows)
        {
            if (first <= level && level <= last)
            {
                return max;
            }
        }

        throw new CampaignSpecException($"max_risk_pairs no cubre el nivel {level}");
    }

    public MechanicInfo Mechanic(string code) => MechanicTable.FirstOrDefault(m => m.Code == code)
        ?? throw new CampaignSpecException($"mecanica desconocida '{code}'");

    public MechanicInfo MechanicById(string id) => MechanicTable.First(m => m.Id == id);

    /// <summary>Tintes del pool del mundo con debut anterior o igual a <paramref name="level"/> (glosario §8-§9).</summary>
    public IReadOnlyList<TintInfo> PoolAt(int level) =>
        Tints.Where(t => t.Debut <= level).OrderBy(t => t.Engine).ToList();

    /// <summary>Tintes del pool del mundo (cabecera del pack): debut anterior o igual al último nivel del mundo.</summary>
    public IReadOnlyList<TintInfo> WorldPool(WorldInfo world) =>
        Tints.Where(t => t.Debut <= world.Last).OrderBy(t => t.Engine).ToList();

    public string SeasonOf(int level)
    {
        int cycleStart = level < CycleFrom ? CycleFrom : CycleFrom + (((level - CycleFrom) / 20) * 20);
        return Seasons.TryGetValue(cycleStart, out string? s) ? s : "-";
    }

    /// <summary>Primer nivel de debut (intro_mechanic) por id: chica 14, alta 27, hoja 43…</summary>
    public IReadOnlyDictionary<int, string> IntroLevels
    {
        get
        {
            var intro = new Dictionary<int, string>();
            foreach (MechanicInfo m in MechanicTable)
            {
                intro[m.Debut] = m.Id;
            }

            foreach (IGrouping<string, CapacityInfo> g in CapacityTable.GroupBy(c => c.Id))
            {
                intro[g.Min(c => c.Debut)] = g.Key;
            }

            return intro;
        }
    }

    public LevelPlan Plan(int level)
    {
        if (!_calendar.TryGetValue(level, out CalendarCell? cell))
        {
            throw new CampaignSpecException($"el calendario no tiene el nivel {level}");
        }

        WorldInfo world = WorldOf(level);
        RangeInfo range = RangeOf(level);
        bool ramp = level >= RampFirst && level <= RampLast;
        int position = ramp ? 0 : PositionOf(level);
        PositionRule? rule = ramp ? null : Positions.First(r => r.Positions.Contains(position));
        SlotRole role = ramp ? SlotRole.Comfort : rule!.Role;
        bool debutBoard = cell.Star && (cell.Mechanics.Count > 0 || cell.Capacities.Count > 0);
        bool mechanicDebutBoard = cell.Star && cell.Mechanics.Count > 0;

        int k;
        int empties;
        string source;
        Interval window;
        RampRow? row = ramp && RampRows.TryGetValue(level, out RampRow? r) ? r : null;
        if (ramp)
        {
            k = row?.K ?? RampK;
            empties = row?.Empties ?? RampEmpties;
            source = "walk";
            window = row?.Window ?? RampWindow;
        }
        else
        {
            KPlanRow kp = KPlan.FirstOrDefault(r => r.First <= level && level <= r.Last)
                ?? throw new CampaignSpecException($"k_plan no cubre el nivel {level}");
            k = rule!.KKey switch
            {
                "recovery" => kp.Recovery,
                "comfort" => kp.Comfort,
                "challenge" => kp.Challenge,
                "milestone" => kp.Milestone,
                _ => throw new CampaignSpecException($"k desconocido '{rule.KKey}'"),
            };
            empties = rule.Empties;
            if (level <= TwoEmptiesUntil || (role == SlotRole.Challenge && level <= ChallengeTwoEmptiesUntil) || mechanicDebutBoard)
            {
                empties = Math.Max(empties, 2);
            }

            // Tablero de estreno de una mecánica (03 §9, consecuencia 3) con 2 vacíos; el 100 (Aguas turbias) es un hito.
            if (mechanicDebutBoard && role == SlotRole.Milestone)
            {
                empties = rule.Empties;
            }

            source = rule.Source switch
            {
                "walk" => "walk",
                "deep" => "deep",
                "challenge" => level <= ChallengeTwoEmptiesUntil ? "fill" : (k >= 9 ? "deep" : "fill"),
                _ => throw new CampaignSpecException($"fuente desconocida '{rule.Source}'"),
            };
            window = WindowFor(level, position, role, debutBoard);
        }

        string? allowed = AllowedLabel(level, position, role, ramp);
        string? target = cell.Mark switch
        {
            "D" => "hard",
            "MD" => "very_hard",
            _ => null,
        };
        IntroLevels.TryGetValue(level, out string? intro);
        return new LevelPlan
        {
            Level = level,
            World = world,
            Range = range,
            Position = position,
            Ramp = ramp,
            Role = role,
            Cell = cell,
            K = k,
            Empties = empties,
            Source = source,
            Window = window,
            AllowedLabel = allowed,
            TargetLabel = target,
            DebutBoard = debutBoard,
            Row = row,
            Intro = intro,
            MaxRiskPairs = MaxRiskPairsAt(level),
            Season = SeasonOf(level),
        };
    }

    private Interval WindowFor(int level, int position, SlotRole role, bool debutBoard)
    {
        // Un tablero de estreno es fácil en cualquier posición que no sea el hito (02 §8: «la mecánica debuta en 2-3 tableros
        // fáciles»; 03 §12: < 20 % en debuts): también el de la Cuia chica de 3 en el p2 del 22.
        string key = role switch
        {
            SlotRole.Recovery => debutBoard ? "debut" : "recovery",
            SlotRole.Comfort => debutBoard ? "debut" : "comfort",
            SlotRole.Remanso => "remanso",
            SlotRole.Challenge => level <= FirstHard ? "challenge_21_40"
                : level < FirstVeryHard ? (Labeled4159.Contains(position) ? "challenge_41_59_labeled" : "challenge_41_59_soft")
                : "challenge_61",
            SlotRole.Milestone => level < FirstVeryHard ? "milestone_40" : "milestone_60",
            _ => throw new InvalidOperationException(),
        };
        return Windows.TryGetValue(key, out Interval? w) ? w : throw new CampaignSpecException($"falta la ventana '{key}' en cycle.windows");
    }

    /// <summary>Etiqueta máxima permitida en un slot (D-009; propuesta c.3, regla 7; 03 §11-§12).</summary>
    private string? AllowedLabel(int level, int position, SlotRole role, bool ramp)
    {
        if (ramp || level < FirstHard)
        {
            return null;
        }

        return role switch
        {
            SlotRole.Milestone => level < FirstVeryHard ? "hard" : "very_hard",
            SlotRole.Challenge => level < FirstVeryHard ? (Labeled4159.Contains(position) ? "hard" : null) : "hard",
            _ => null,
        };
    }

    /// <summary>Etiqueta medida (03 §7; la decide el bot) con los primeros niveles de 03 §11-§12.</summary>
    public string MeasuredLabel(int level, double botFail)
    {
        if (botFail >= VeryHardThreshold - 1e-9 && level >= FirstVeryHard)
        {
            return "very_hard";
        }

        return botFail >= HardThreshold - 1e-9 && level >= FirstHard ? "hard" : "normal";
    }

    /// <summary>
    /// Motivo si el nivel mide por encima de lo que su posición en la campaña admite (contrato 6; 03 §11-§12): ≥ 0,90 antes
    /// del primer Muy difícil (60) o ≥ 0,75 antes del primer Difícil (40). Null si no.
    /// </summary>
    public string? EarlyMeasure(int level, double botFail)
    {
        if (level < FirstVeryHard && botFail >= VeryHardThreshold - 1e-9)
        {
            return string.Create(CultureInfo.InvariantCulture, $"mide Muy dificil ({botFail:0.000}) antes del {FirstVeryHard}");
        }

        return level < FirstHard && botFail >= HardThreshold - 1e-9
            ? string.Create(CultureInfo.InvariantCulture, $"mide Dificil ({botFail:0.000}) antes del {FirstHard}")
            : null;
    }

    /// <summary>
    /// Tope de mecánicas distintas del nivel (02 §9.2 regla 4; D-009: «máximo 2 mecánicas distintas por nivel antes del 300 y 3
    /// después, 3 solo en retos e hitos»): el alto desde <see cref="RaisedMechanicsFrom"/> y solo en retos e hitos; si no, el
    /// normal. Los niveles salen de la especificación, no del código (P6 de la tanda 4).
    /// </summary>
    public int MaxMechanicsAt(int level, SlotRole? role) =>
        RaisedMechanicsFrom is int from && level >= from && role is SlotRole.Challenge or SlotRole.Milestone
            ? RaisedMaxMechanics
            : MaxMechanics;

    public static int LabelRank(string? label) => label switch
    {
        "hard" => 1,
        "very_hard" => 2,
        _ => 0,
    };

    // ---------------------------------------------------------------------------------------------------------------
    // Lectura
    // ---------------------------------------------------------------------------------------------------------------

    public static CampaignSpec Load(string path)
    {
        JsonObject root = LoadMerged(path, 0);
        try
        {
            return Build(path, root);
        }
        catch (Exception e) when (e is InvalidOperationException or FormatException or NullReferenceException or KeyNotFoundException or JsonException)
        {
            throw new CampaignSpecException($"{path}: especificacion mal formada ({e.Message})");
        }
    }

    private static JsonObject LoadMerged(string path, int depth)
    {
        if (depth > 3)
        {
            throw new CampaignSpecException($"{path}: demasiados 'extends' encadenados");
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            throw new CampaignSpecException($"no se puede leer {path}: {e.Message}");
        }

        if (node is not JsonObject obj)
        {
            throw new CampaignSpecException($"{path}: la especificacion debe ser un objeto JSON");
        }

        if (obj["extends"] is not JsonValue ext)
        {
            return obj;
        }

        string basePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)) ?? ".", ext.GetValue<string>());
        JsonObject merged = LoadMerged(basePath, depth + 1);
        foreach (KeyValuePair<string, JsonNode?> kv in obj)
        {
            if (kv.Key == "extends")
            {
                continue;
            }

            if ((kv.Key == "calendar" || kv.Key == "seasons") && kv.Value is JsonObject patch && merged[kv.Key] is JsonObject target)
            {
                foreach (KeyValuePair<string, JsonNode?> entry in patch)
                {
                    target[entry.Key] = entry.Value?.DeepClone();
                }

                continue;
            }

            merged[kv.Key] = kv.Value?.DeepClone();
        }

        return merged;
    }

    private static CampaignSpec Build(string path, JsonObject root)
    {
        var tints = root["tints"]!.AsArray().Select(t => new TintInfo(
            Str(t!, "id"), Int(t!, "engine"), Str(t!, "name_es"), Int(t!, "debut"))).ToList();
        var riskPairs = root["risk_pairs"]!.AsArray().Select(p => (p![0]!.GetValue<int>(), p[1]!.GetValue<int>())).ToList();
        var maxRisk = root["max_risk_pairs"]!.AsArray().Select(r => (r!["levels"]![0]!.GetValue<int>(), r["levels"]![1]!.GetValue<int>(), Int(r, "max"))).ToList();
        var worlds = root["worlds"]!.AsArray().Select(w => new WorldInfo(
            Int(w!, "world"), Str(w!, "world_id"), Str(w!, "name"), w!["levels"]![0]!.GetValue<int>(), w["levels"]![1]!.GetValue<int>(),
            Str(w, "water_base"), Str(w, "water_tint"))).ToList();
        var ranges = root["ranges"]!.AsArray().Select(r => new RangeInfo(
            r!["levels"]![0]!.GetValue<int>(), r["levels"]![1]!.GetValue<int>(),
            r["k"]![0]!.GetValue<int>(), r["k"]![1]!.GetValue<int>(),
            r["cuias"]![0]!.GetValue<int>(), r["cuias"]![1]!.GetValue<int>(),
            Int(r, "empties"),
            r["par"]![0]!.GetValue<int>(), r["par"]![1]!.GetValue<int>(),
            r["pct_hard"]![0]!.GetValue<int>(), r["pct_hard"]![1]!.GetValue<int>(),
            r["pct_very_hard"]![0]!.GetValue<int>(), r["pct_very_hard"]![1]!.GetValue<int>())).ToList();
        var kplan = root["k_plan"]!.AsArray().Select(r => new KPlanRow(
            r!["levels"]![0]!.GetValue<int>(), r["levels"]![1]!.GetValue<int>(),
            Int(r, "recovery"), Int(r, "comfort"), Int(r, "challenge"), Int(r, "milestone"))).ToList();
        JsonObject cycle = root["cycle"]!.AsObject();
        var positions = cycle["positions"]!.AsArray().Select(p => new PositionRule(
            p!["p"]!.AsArray().Select(x => x!.GetValue<int>()).ToArray(),
            ParseRole(Str(p, "role")),
            Int(p, "empties"),
            Str(p, "source"),
            Str(p, "k"),
            p["debut_slot"]?.GetValue<bool>() ?? false)).ToList();
        var windows = cycle["windows"]!.AsObject().ToDictionary(kv => kv.Key, kv => Interval.Parse(kv.Value!.GetValue<string>()));
        JsonObject labels = cycle["labels"]!.AsObject();
        var mechanics = root["mechanics"]!.AsArray().Select(m => new MechanicInfo(
            Str(m!, "code"), Str(m!, "id"), Str(m!, "kind"), Int(m!, "debut"), Str(m!, "season"),
            m!["debut_key"]?.GetValue<string>(), m["k"]?.GetValue<int>() ?? 0, m["cave_cap"]?.GetValue<int>() ?? 0)).ToList();
        foreach (MechanicInfo m in mechanics.Where(m => !MechanicCodes.IsKnown(m.Code)))
        {
            // El generador superpone y level-qa reconoce solo los códigos de MechanicCodes: otro código sería una mecánica sin
            // superposición que rechazaría todos los candidatos del slot.
            throw new CampaignSpecException($"{path}: codigo de mecanica desconocido '{m.Code}' (se admiten {string.Join(", ", MechanicCodes.All)})");
        }

        var capacities = root["capacities"]!.AsArray().Select(c => new CapacityInfo(
            Str(c!, "code"), Str(c!, "id"), Int(c!, "cap"), Int(c!, "debut"))).ToList();
        var seasons = root["seasons"]!.AsObject().ToDictionary(
            kv => int.Parse(kv.Key, CultureInfo.InvariantCulture), kv => kv.Value!.GetValue<string>());
        JsonObject rampNode = root["ramp"]!.AsObject();
        var rampRows = new Dictionary<int, RampRow>();
        if (rampNode["rows"] is JsonObject rows)
        {
            foreach (KeyValuePair<string, JsonNode?> kv in rows)
            {
                int level = int.Parse(kv.Key, NumberStyles.None, CultureInfo.InvariantCulture);
                JsonNode row = kv.Value!;
                rampRows[level] = new RampRow(
                    level, Int(row, "k"), Int(row, "empties"), row["par"]![0]!.GetValue<int>(), row["par"]![1]!.GetValue<int>(),
                    Interval.Parse(Str(row, "window")));
            }
        }

        var spec = new CampaignSpec
        {
            Path = path,
            SpecId = Str(root, "spec"),
            Lap = Int(root, "lap"),
            FirstLevel = Int(root, "first_level"),
            LastLevel = Int(root, "last_level"),
            Tints = tints,
            RiskPairs = riskPairs,
            MaxRiskPairsRows = maxRisk,
            Worlds = worlds,
            Ranges = ranges,
            KPlan = kplan,
            Positions = positions,
            MechanicTable = mechanics,
            CapacityTable = capacities,
            Seasons = seasons,
            Windows = windows,
            RampFirst = rampNode["levels"]![0]!.GetValue<int>(),
            RampLast = rampNode["levels"]![1]!.GetValue<int>(),
            RampK = Int(rampNode, "k"),
            RampEmpties = Int(rampNode, "empties"),
            RampWindow = Interval.Parse(Str(rampNode, "window")),
            RampRows = rampRows,
            CycleFrom = Int(cycle, "from_level"),
            TwoEmptiesUntil = Int(cycle, "empties_two_until_level"),
            ChallengeTwoEmptiesUntil = Int(cycle, "challenge_two_empties_until_level"),
            FirstHard = Int(labels, "first_hard"),
            FirstVeryHard = Int(labels, "first_very_hard"),
            Labeled4159 = labels["labeled_41_59"]!.AsArray().Select(x => x!.GetValue<int>()).ToArray(),
            HardThreshold = labels["thresholds"]!["hard"]!.GetValue<double>(),
            VeryHardThreshold = labels["thresholds"]!["very_hard"]!.GetValue<double>(),
            ExpectRule = root["expect_rule"]?.GetValue<string>(),
            MaxMechanics = root["mechanics_per_level"]?["max"]?.GetValue<int>() ?? DefaultMaxMechanics,
            RaisedMechanicsFrom = root["mechanics_per_level"]?["raised_from_level"]?.GetValue<int>(),
            RaisedMaxMechanics = root["mechanics_per_level"]?["raised_max"]?.GetValue<int>()
                ?? root["mechanics_per_level"]?["max"]?.GetValue<int>() ?? DefaultMaxMechanics,
        };

        if (spec.MaxMechanics < 1 || spec.RaisedMaxMechanics < spec.MaxMechanics)
        {
            throw new CampaignSpecException($"{path}: mechanics_per_level: max >= 1 y raised_max >= max");
        }

        foreach (KeyValuePair<string, JsonNode?> kv in root["calendar"]!.AsObject())
        {
            if (!int.TryParse(kv.Key, NumberStyles.None, CultureInfo.InvariantCulture, out int level))
            {
                throw new CampaignSpecException($"{path}: clave de calendario no numerica '{kv.Key}'");
            }

            spec._calendar[level] = spec.ParseCell(level, kv.Value!.GetValue<string>());
        }

        return spec;
    }

    /// <summary>
    /// Interpreta una celda literal de d.4: [★]items[ (marca)][ (tinte X)][ «nombre»][ [P]], con items R, C, S o códigos de
    /// mecánica y capacidad unidos con +.
    /// </summary>
    public CalendarCell ParseCell(int level, string literal)
    {
        string text = literal.Trim();
        bool star = text.StartsWith(Star, StringComparison.Ordinal);
        if (star)
        {
            text = text[Star.Length..];
        }

        Match head = Regex.Match(text, @"^([A-Za-z0-9+]+)(.*)$");
        if (!head.Success)
        {
            throw new CampaignSpecException($"nivel {level}: celda de calendario no valida '{literal}'");
        }

        string? pure = null;
        var mechs = new List<MechanicInfo>();
        var caps = new List<CapacityInfo>();
        foreach (string item in head.Groups[1].Value.Split('+'))
        {
            if (item is "R" or "C" or "S")
            {
                pure = item;
                continue;
            }

            MechanicInfo? m = MechanicTable.FirstOrDefault(x => x.Code == item);
            if (m is not null)
            {
                mechs.Add(m);
                continue;
            }

            CapacityInfo? c = CapacityTable.FirstOrDefault(x => x.Code == item);
            if (c is null)
            {
                throw new CampaignSpecException($"nivel {level}: codigo desconocido '{item}' en '{literal}'");
            }

            caps.Add(c);
        }

        if (pure is not null && (mechs.Count > 0 || caps.Count > 0 || head.Groups[1].Value.Contains('+')))
        {
            throw new CampaignSpecException($"nivel {level}: R, C y S van solos ('{literal}')");
        }

        string? mark = null;
        string? tint = null;
        var notes = new List<string>();
        string rest = head.Groups[2].Value;
        foreach (Match g in Regex.Matches(rest, @"\(([^)]*)\)"))
        {
            string inner = g.Groups[1].Value.Trim();
            string first = inner.Split(';')[0].Trim();
            if (first is "s" or "D" or "MD")
            {
                if (mark is not null)
                {
                    throw new CampaignSpecException($"nivel {level}: dos marcas de reto en '{literal}'");
                }

                mark = first;
                if (inner.Contains(';'))
                {
                    notes.Add(inner[(inner.IndexOf(';') + 1)..].Trim());
                }
            }
            else if (inner.StartsWith("tinte ", StringComparison.Ordinal))
            {
                tint = inner["tinte ".Length..].Trim();
                if (Tints.All(t => t.NameEs != tint))
                {
                    throw new CampaignSpecException($"nivel {level}: tinte desconocido '{tint}'");
                }
            }
            else
            {
                notes.Add(inner);
            }
        }

        Match name = Regex.Match(rest, "«([^»]*)»");
        if (name.Success)
        {
            notes.Add("nombre de hito: " + name.Groups[1].Value);
        }

        return new CalendarCell(level, literal, star, pure, mechs, caps, mark, tint, notes);
    }

    private static SlotRole ParseRole(string role) => role switch
    {
        "recovery" => SlotRole.Recovery,
        "comfort" => SlotRole.Comfort,
        "challenge" => SlotRole.Challenge,
        "remanso" => SlotRole.Remanso,
        "milestone" => SlotRole.Milestone,
        _ => throw new CampaignSpecException($"rol desconocido '{role}'"),
    };

    private static string Str(JsonNode node, string key) =>
        node[key]?.GetValue<string>() ?? throw new CampaignSpecException($"falta '{key}'");

    private static int Int(JsonNode node, string key) =>
        node[key]?.GetValue<int>() ?? throw new CampaignSpecException($"falta '{key}'");
}

/// <summary>
/// Códigos de mecánica del calendario (d.4; d.5.1 de la propuesta de D-007 para las variantes de la 1.0) y su lectura sobre un
/// <c>attrs[i]</c> publicado. Los códigos de las variantes son los [P] de la tarea P5 (Ln, Ha, B2 y Fl), los mismos que usa D2
/// en d.5.1 y d.5.3 de la propuesta (rehecha el 2026-10-14), así que no cambian:
/// <list type="bullet">
/// <item><description>Ln: Liana de cuenta, <c>{"type":"liana","n":1|2}</c> (02 §9.1 M3; debut 243; <c>intro_mechanic</c>
/// <c>liana_n</c> [P]);</description></item>
/// <item><description>Ha: Hoja atada, <c>{"type":"hoja_atada","key":c}</c> (b.3; debut 283; <c>hoja_atada</c>);</description></item>
/// <item><description>B2: Brote de dos hojas, <c>{"type":"brote","n":2}</c> (b.3; debut 343; <c>brote_n2</c> [P]);</description></item>
/// <item><description>Fl: Flor de vitória-régia, <c>{"type":"flor","key":c1,"key2":c2}</c> con c1 ≠ c2 (b.3; debut 403;
/// <c>flor</c>).</description></item>
/// </list>
/// Firma de <c>pack_schema</c> 3 (D-023, punto 5). Li y Br quedan para la Liana con fruta y el Brote de una hoja.
/// </summary>
internal static class MechanicCodes
{
    public const string Hoja = "Ho";
    public const string Matapi = "Ma";
    public const string Liana = "Li";
    public const string LianaCount = "Ln";
    public const string Murky = "Tu";
    public const string Remolino = "Re";
    public const string Barro = "Ba";
    public const string Lluvia = "Ll";
    public const string Brote = "Br";
    public const string BroteTwo = "B2";
    public const string Pirana = "Pi";
    public const string Tinaja = "Ti";
    public const string HojaAtada = "Ha";
    public const string Flor = "Fl";

    /// <summary><c>n</c> del Brote de dos hojas (b.3; firma de <c>pack_schema</c> 3: brote con n ∈ {1, 2}).</summary>
    public const int BroteTwoLeaves = 2;

    /// <summary>
    /// Todos los códigos que el generador sabe superponer, en orden ordinal. level-qa los lee con <see cref="CodeOf"/> (P6): Ha y
    /// Fl, y Ln y B2 distintos de Li y Br.
    /// </summary>
    public static readonly string[] All =
        [.. new[] { Hoja, Matapi, Liana, LianaCount, Murky, Remolino, Barro, Lluvia, Brote, BroteTwo, Pirana, Tinaja, HojaAtada, Flor }.Order(StringComparer.Ordinal)];

    /// <summary>Variantes de la 1.0 (b.3): las que exigen <c>pack_schema</c> 3 en su estreno (su <c>intro_mechanic</c>).</summary>
    public static readonly string[] Variants = [LianaCount, HojaAtada, BroteTwo, Flor];

    public static bool IsKnown(string code) => Array.IndexOf(All, code) >= 0;

    /// <summary>
    /// Bloqueo C1-9 de <c>AF.Core</c> (02 R16; pregunta 24 b): la Liana con fruta, la Liana de cuenta y la Flor (K3b) no van con
    /// capacidades mixtas, cueva de la Piraña incluida (LevelValidation las rechaza).
    /// </summary>
    public static bool IsMixedCapacityBlocked(string code) => code is Liana or LianaCount or Flor;

    /// <summary>Necesita cuias a medias en el tablero base (cuia de holgura; fill no las deja): el Brote y el de dos hojas.</summary>
    public static bool NeedsPartial(string code) => code is Brote or BroteTwo;

    /// <summary>
    /// Código del calendario de una entrada de <c>attrs</c> (la tinaja y su cuia dan Ti), o null sin mecánica. El Barro (Ba) y
    /// las Aguas turbias (Tu) no son <c>attrs</c>: los lee quien mira el contenido y <c>hidden</c>.
    /// </summary>
    public static string? CodeOf(MechanicSpec a) => a.Type switch
    {
        MechanicType.None => null,
        MechanicType.Hoja => Hoja,
        MechanicType.Brote => a.N == BroteTwoLeaves ? BroteTwo : Brote,
        MechanicType.Liana => a.N != 0 ? LianaCount : Liana,
        MechanicType.Matapi => Matapi,
        MechanicType.Remolino => Remolino,
        MechanicType.Lluvia => Lluvia,
        MechanicType.Pirana => Pirana,
        MechanicType.Tinaja or MechanicType.Cucharon => Tinaja,
        MechanicType.HojaAtada => HojaAtada,
        MechanicType.Flor => Flor,
        _ => throw new ArgumentOutOfRangeException(nameof(a), a.Type, "mecanica sin codigo de calendario"),
    };
}
