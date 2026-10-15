// Mecánicas (P-9 a P-11; GDD 03 §17 #6 y 02 §9.2): debut de D-007 anterior o igual al nivel, un solo ejemplar en los
// tableros de estreno (CycleChecks), llave presente y la del tutorial en el estreno, mecánica permitida por la celda del
// calendario y el tope de mecánicas por nivel de la especificación (02 §9.2 regla 4: 2 antes del 300 y 3 después, 3 solo en
// retos e hitos; mechanics_per_level, P6). El resto de reglas de cada mecánica (Barro en la capa 2-3, U_agua = 4 con las gotas,
// cueva de la Piraña, pares del Remolino…) las comprueba LevelValidation (QA-VALID), y los controles propios de las variantes
// de la 1.0, VariantChecks.
// Códigos del calendario (P6): los lee MechanicCodes.CodeOf, el mismo código que el generador, así que la Liana de cuenta (Ln)
// y el Brote de dos hojas (B2) no se confunden con la Liana con fruta (Li) y el Brote (Br), y la Hoja atada (Ha) y la Flor (Fl)
// tienen su código.
// Tinaja (M13; D-019; propuesta de la tinaja c.5; 02 §9.1 M13): la tinaja y su cuia son una sola mecánica, «Ti», con un
// solo ejemplar (02 §9.2 regla 4); sus ocultas no son Aguas turbias (solo cuenta «Tu» si hay ocultas fuera de la tinaja), y
// sus capacidades (5 y 2) no son Cuia alta ni Cuia chica (como la cueva de la Piraña). Sus controles propios están en
// TinajaChecks.
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class MechanicChecks
{
    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-MECH-DEBUT", "cada mecanica y capacidad con debut anterior o igual al nivel (D-007; 02 sec. 8)"),
        ("QA-MECH-SPEC", "las mecanicas del nivel estan en su celda del calendario (especificacion)"),
        ("QA-MECH-CELL", "cada mecanica y capacidad de la celda esta en el nivel (sin turbias por T1: aviso) (especificacion; 02 sec. 8)"),
        ("QA-GAP-120", "ninguna mecanica ni capacidad pasa mas de 120 niveles sin volver, sobre los niveles publicados (resumen, punto 6; d.2 regla 4)"),
        ("QA-MECH-KEY", "llave presente en el tablero (Hoja y Liana con fruta); agua del mundo en la Lluvia; llave del tutorial en el estreno (02 sec. 9.1; D-007)"),
        ("QA-MECH-COUNT", "mecanicas por nivel dentro del tope de la especificacion (mechanics_per_level; 02 sec. 9.2 regla 4: 2 antes del 300 y 3 despues, 3 solo en retos e hitos)"),
    ];

    /// <summary>La cuia lleva la tinaja o su cuia chica (M13): las dos piezas de una misma mecánica.</summary>
    public static bool IsTinajaPair(MechanicType type) => type is MechanicType.Tinaja or MechanicType.Cucharon;

    /// <summary>
    /// Dosis ocultas de Aguas turbias: las de <c>hidden</c> fuera de una tinaja (M13; propuesta de la tinaja c.5: «hay
    /// ocultos» ya no quiere decir «Aguas turbias»). Una posición con un índice fuera del tablero cuenta como turbia.
    /// </summary>
    public static int[][] MurkyHidden(BoardData b) =>
        b.Hidden.Where(h => h.Length < 1 || h[0] < 0 || h[0] >= b.Attrs.Length || b.Attrs[h[0]].Type != MechanicType.Tinaja).ToArray();

    /// <summary>
    /// La cuia <paramref name="i"/> es una capacidad de 02 §8 (Cuia chica o Cuia alta): capacidad distinta de 4 y no es la
    /// cueva de la Piraña, la tinaja ni su cuia chica (propuesta de la tinaja c.5: si no, contarían como Cuia alta y Cuia chica
    /// en QA-GAP-120, QA-MECH-CELL y QA-MECH-DEBUT).
    /// </summary>
    public static bool IsCapacityCuia(BoardData b, int i) =>
        b.Caps[i] != Cuia.DefaultCapacity && b.Attrs[i].Type != MechanicType.Pirana && !IsTinajaPair(b.Attrs[i].Type);

    /// <summary>Vacíos de 4 (o más) sin mecánica: los de la aritmética de 02 §8, sin contar la tinaja ni su cuia (c.3 punto 2).</summary>
    public static int FreeEmpties(BoardData b) =>
        Enumerable.Range(0, b.Cuias.Length).Count(i => b.Cuias[i].Length == 0 && b.Caps[i] >= Cuia.DefaultCapacity && b.Attrs[i].IsNone);

    /// <summary>
    /// Códigos de calendario de las mecánicas presentes (las capacidades no son mecánicas). La tinaja y su cuia dan «Ti»; «Tu»
    /// solo si hay dosis ocultas fuera de la tinaja (propuesta de la tinaja c.5).
    /// </summary>
    public static SortedSet<string> Codes(BoardData b)
    {
        var codes = new SortedSet<string>(StringComparer.Ordinal);
        foreach (MechanicSpec a in b.Attrs.Where(a => !a.IsNone))
        {
            // Un tipo fuera del calendario no lo publica PackReader (lo rechaza al leer), pero si llegara sale como «?» y lo
            // detiene QA-MECH-SPEC.
            codes.Add(MechanicTypes.IsDefined(a.Type) ? MechanicCodes.CodeOf(a) ?? "?" : "?");
        }

        if (b.Cuias.Any(c => c.Contains(Cuia.MudColor)))
        {
            codes.Add(MechanicCodes.Barro);
        }

        if (MurkyHidden(b).Length > 0)
        {
            codes.Add(MechanicCodes.Murky);
        }

        return codes;
    }

    /// <summary>
    /// Ejemplares de mecánica: un attr (el par del Remolino cuenta como uno, y la tinaja con su cuia también: c.5), cada dosis
    /// de Barro y cada cuia turbia (sin la tinaja).
    /// </summary>
    public static int Instances(BoardData b)
    {
        int remolino = b.Attrs.Count(a => a.Type == MechanicType.Remolino);
        int tinaja = Math.Max(b.Attrs.Count(a => a.Type == MechanicType.Tinaja), b.Attrs.Count(a => a.Type == MechanicType.Cucharon));
        int attrs = b.Attrs.Count(a => !a.IsNone && a.Type != MechanicType.Remolino && !IsTinajaPair(a.Type));
        int mud = b.Cuias.Sum(c => c.Count(x => x == Cuia.MudColor));
        int murky = MurkyHidden(b).Select(h => h.Length > 0 ? h[0] : -1).Distinct().Count();
        return attrs + (remolino / 2) + tinaja + mud + murky;
    }

    /// <summary>
    /// Capacidades distintas de 4 de las cuias que no son la cueva de la Piraña (02 §8) ni la tinaja y su cuia (propuesta de la
    /// tinaja c.5).
    /// </summary>
    public static IEnumerable<int> CapacityCaps(BoardData b) =>
        b.Caps.Where((c, i) => IsCapacityCuia(b, i));

    /// <summary>
    /// QA-GAP-120 sobre los niveles publicados: entre dos apariciones de una mecánica (por código del calendario) o de una
    /// capacidad (Cuia chica o Cuia alta, la de 5 y la de 6 juntas) no pasan más de 120 niveles, ni desde la última hasta el
    /// último nivel de la especificación. Solo cuenta un hueco cuando todos sus niveles están en los paquetes (un paquete
    /// suelto no da falsos fallos).
    /// </summary>
    public static IEnumerable<QaIssue> RunCampaign(QaContext ctx)
    {
        CampaignSpec spec = ctx.Spec;
        var byLevel = ctx.Levels.GroupBy(e => e.Level.Level).ToDictionary(g => g.Key, g => g.First().Level);
        var seen = new SortedDictionary<string, List<int>>(StringComparer.Ordinal);
        foreach (QaLevel lv in byLevel.Values.OrderBy(l => l.Level))
        {
            var ids = new SortedSet<string>(Codes(lv.Board), StringComparer.Ordinal);
            foreach (int cap in CapacityCaps(lv.Board))
            {
                ids.Add(cap < Cuia.DefaultCapacity ? "Cuia chica" : "Cuia alta");
            }

            foreach (string id in ids)
            {
                if (!seen.TryGetValue(id, out List<int>? list))
                {
                    list = [];
                    seen[id] = list;
                }

                list.Add(lv.Level);
            }
        }

        bool Covered(int from, int to) => Enumerable.Range(from, Math.Max(0, to - from + 1)).All(byLevel.ContainsKey);
        foreach ((string id, List<int> levels) in seen)
        {
            for (int i = 1; i < levels.Count; i++)
            {
                if (levels[i] - levels[i - 1] > SpecValidator.MaxGap && Covered(levels[i - 1], levels[i]))
                {
                    QaLevel at = byLevel[levels[i]];
                    yield return new QaIssue("QA-GAP-120", at.Pack, at.Level, string.Create(CultureInfo.InvariantCulture,
                        $"{id} pasa {levels[i] - levels[i - 1]} niveles sin volver (del {levels[i - 1]} al {levels[i]})"));
                }
            }

            int last = levels[^1];
            if (spec.LastLevel - last > SpecValidator.MaxGap && Covered(last, spec.LastLevel))
            {
                QaLevel at = byLevel[spec.LastLevel];
                yield return new QaIssue("QA-GAP-120", at.Pack, at.Level, string.Create(CultureInfo.InvariantCulture,
                    $"{id} no vuelve desde el {last} ({spec.LastLevel - last} niveles hasta el {spec.LastLevel})"));
            }
        }
    }

    public static IEnumerable<QaIssue> Run(QaContext ctx, LevelEval e)
    {
        QaLevel lv = e.Level;
        QaIssue Fail(string id, string msg) => new(id, lv.Pack, lv.Level, msg);
        CampaignSpec spec = ctx.Spec;
        BoardData b = lv.Board;
        SortedSet<string> codes = Codes(b);
        foreach (string code in codes)
        {
            MechanicInfo? m = spec.MechanicTable.FirstOrDefault(x => x.Code == code);
            if (m is not null && m.Debut > lv.Level)
            {
                yield return Fail("QA-MECH-DEBUT", string.Create(CultureInfo.InvariantCulture, $"{m.Id} antes de su debut ({m.Debut})"));
            }
        }

        // Capacidades (02 §8): la cueva de la Piraña no cuenta como Cuia chica, ni la tinaja y su cuia como Cuia alta y Cuia
        // chica (c.5).
        for (int i = 0; i < b.Caps.Length; i++)
        {
            int cap = b.Caps[i];
            if (!IsCapacityCuia(b, i))
            {
                continue;
            }

            CapacityInfo? c = spec.CapacityTable.FirstOrDefault(x => x.Cap == cap);
            if (c is not null && c.Debut > lv.Level)
            {
                yield return Fail("QA-MECH-DEBUT", string.Create(CultureInfo.InvariantCulture, $"cuia de capacidad {cap} antes de su debut ({c.Debut})"));
            }
        }

        // Tope de 02 §9.2 regla 4 con los niveles de la especificación (mechanics_per_level): sin plan (nivel fuera de la
        // especificación, que ya falla QA-BAND), el tope normal.
        int maxMechanics = spec.MaxMechanicsAt(lv.Level, e.Plan?.Role);
        if (codes.Count > maxMechanics)
        {
            string where = spec.RaisedMechanicsFrom is int from && lv.Level >= from
                ? string.Create(CultureInfo.InvariantCulture, $"desde el {from}, {spec.RaisedMaxMechanics} solo en retos e hitos")
                : spec.RaisedMechanicsFrom is int f2
                    ? string.Create(CultureInfo.InvariantCulture, $"antes del {f2}")
                    : "en esta especificacion";
            yield return Fail("QA-MECH-COUNT", string.Create(CultureInfo.InvariantCulture,
                $"{codes.Count} mecanicas ({string.Join("+", codes)}); como mucho {maxMechanics} ({where})"));
        }

        if (e.Plan is LevelPlan plan)
        {
            var allowed = plan.Mechanics.Select(m => m.Code).ToHashSet();
            foreach (string code in codes.Where(c => !allowed.Contains(c)))
            {
                yield return Fail("QA-MECH-SPEC", $"{code} no esta en la celda del calendario ({plan.Cell.Short})");
            }

            // Lo que pide la celda está en el nivel: el generador no quita nada en silencio. Única excepción, el respaldo de
            // T1 (02 §11): un reto turbio que no mide Difícil se publica sin turbias y lo registra el informe.
            foreach (MechanicInfo m in plan.Mechanics.Where(m => !codes.Contains(m.Code)))
            {
                yield return m.Code == "Tu"
                    ? new QaIssue("QA-MECH-CELL", lv.Pack, lv.Level, $"publicado sin Aguas turbias (respaldo de T1; celda {plan.Cell.Short})", Warning: true)
                    : Fail("QA-MECH-CELL", $"{m.Id} de la celda ({plan.Cell.Short}) no esta en el nivel");
            }

            int[] caps = CapacityCaps(b).ToArray();
            foreach (CapacityInfo cap in plan.Capacities.Where(c => !caps.Contains(c.Cap)))
            {
                yield return Fail("QA-MECH-CELL", string.Create(CultureInfo.InvariantCulture,
                    $"{cap.Code} (cuia de capacidad {cap.Cap}) de la celda ({plan.Cell.Short}) no esta en el nivel"));
            }
        }

        // Llaves.
        var units = new int[Cuia.MaxColor + 1];
        foreach (int c in b.Cuias.SelectMany(x => x))
        {
            units[c]++;
        }

        int water = lv.WaterBase == "black" ? LevelValidation.RioNegroWater : LevelValidation.TapajosWater;
        for (int i = 0; i < b.Attrs.Length; i++)
        {
            MechanicSpec a = b.Attrs[i];
            // La Liana de cuenta (n) no lleva llave (02 §9.1 M3); la Hoja atada y la Flor las comprueba VariantChecks.
            bool keyed = a.Type == MechanicType.Hoja || (a.Type == MechanicType.Liana && a.N == 0);
            if (keyed && (a.Key < Cuia.MinColor || a.Key > Cuia.MaxTintColor || units[a.Key] == 0))
            {
                yield return Fail("QA-MECH-KEY", string.Create(CultureInfo.InvariantCulture, $"cuia {i}: la llave {a.Key} no esta en el tablero"));
            }

            if (a.Type == MechanicType.Lluvia && a.Key != water)
            {
                yield return Fail("QA-MECH-KEY", string.Create(CultureInfo.InvariantCulture, $"cuia {i}: la Lluvia cae con el tinte {a.Key}; el agua del mundo ({lv.WaterBase}) es {water}"));
            }

            if (!a.IsNone && e.Plan is { DebutBoard: true } p)
            {
                MechanicInfo? m = p.Mechanics.FirstOrDefault(x => x.DebutKey is not null && x.Code == MechanicCodes.CodeOf(a));
                if (m is not null)
                {
                    int key = a.Type == MechanicType.Brote ? (b.Cuias[i].Length > 0 ? b.Cuias[i][^1] : 0) : a.Key;
                    int expected = spec.Tint(m.DebutKey!).Engine;
                    if (key != expected)
                    {
                        yield return Fail("QA-MECH-KEY", string.Create(CultureInfo.InvariantCulture, $"estreno de {m.Id} con la llave {key}; la del tutorial es {m.DebutKey} ({expected})"));
                    }
                }
            }
        }
    }
}
