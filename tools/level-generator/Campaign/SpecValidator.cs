// --validate-spec: comprueba la especificación de la campaña contra las reglas de la rotación (propuesta de D-007 d.2,
// d.3 y las «Comprobaciones de la tabla» de d.4), las del escalón de D-009 (c.3, c.4) y las de 02 §8-§9.2 que se
// pueden decidir sin generar tableros. Cada regla tiene un id estable (lo citan los fixtures de specs/tests/).
// La salida es ASCII (.claude/rules/ci.md).
using System.Globalization;
using System.Text;

namespace AmazonFill.Tools.LevelGenerator.Campaign;

internal sealed record SpecIssue(string Rule, int Level, string Message, bool Warning = false);

internal static class SpecValidator
{
    /// <summary>Reglas que comprueba el validador, con su fuente (las imprime --validate-spec).</summary>
    public static readonly (string Id, string Source)[] Rules =
    [
        ("CAL-COVER", "calendario completo del primer al ultimo nivel, sin huecos ni extras"),
        ("CAL-ROLE", "R solo en p1-p2, S solo en p19 y p1-p2/p19 puros (c.3, c.4; 22 = Cuia chica de 3 del canon)"),
        ("LABELS", "marcas (s)/(D)/(MD) en su posicion: 21-39 retos (s); 40 (D); 41-59 (D) en p6/p12/p18 y (s) en p9/p15; 60+ retos (D) e hito (MD) (D-009; c.3 regla 7)"),
        ("TITULAR-8", "cada titular sale 8 veces en su ciclo de debut (d.4)"),
        ("PURE-30", "al menos un 30 % de niveles puros por ciclo (d.2 regla 1)"),
        ("PURE-COMFORT", "al menos 3 de las 8 comodidades de p7-p17 puras (c.4)"),
        ("FORBIDDEN", "combinaciones prohibidas: turbias solo con filtros (Q14 A), un contador por nivel, Lluvia nunca con Pirana, Remolino nunca con Pirana [P] (d.3)"),
        ("MURKY-SLOT", "Aguas turbias solo en slots con etiqueta (T1; propuesta b.2)"),
        ("MURKY-2", "Aguas turbias como mucho 2 veces por ciclo (propuesta b.2)"),
        ("BEFORE-DEBUT", "ninguna mecanica ni capacidad antes de su debut (D-007; 02 sec. 8)"),
        ("DEBUT-LEVEL", "el primer tablero de cada mecanica y capacidad es su nivel de debut y lleva estrella (D-007; 02 sec. 8)"),
        ("DEBUT-SLOT", "ningun debut en un reto o un hito salvo las turbias en el 100; 2-3 tableros de estreno seguidos, con la mecanica sola (c.3 regla 6; 02 sec. 9.2 regla 2)"),
        ("MAX-2", "como mucho 2 mecanicas por nivel antes del 300; las capacidades no cuentan [P] (02 sec. 9.2 regla 4)"),
        ("RETURN", "cada titular vuelve como secundaria (reto o hito) en el ciclo siguiente (d.4)"),
        ("CAP-CYCLE", "una mecanica sale en 6 niveles por ciclo como mucho (8 en su ciclo de debut) (d.2 regla 2)"),
        ("NO-3-ROW", "una mecanica nunca en 3 niveles seguidos fuera de su estreno (d.2 regla 2)"),
        ("SEASONS", "temporadas A/B alternas, nunca mas de 2 ciclos seguidos iguales; el ciclo de debut toma la temporada de la mecanica (d.2 regla 7)"),
        ("GAP-120", "ninguna mecanica ni capacidad (Cuia chica, Cuia alta) pasa mas de 120 niveles sin volver (resumen, punto 6; d.2 regla 4; 02 sec. 8)"),
        ("FIRST-TIME", "al menos una primera vez por ciclo: estreno, tinte nuevo o primera combinacion (d.2 regla 3)"),
        ("CAPACITIES", "como mucho 2 Cuias chicas (con la cueva de la Pirana) y 1 Cuia alta por nivel antes del 200; Cuia alta de 6 solo en hitos desde el 141 (02 sec. 8)"),
        ("LIANA-CAPS", "la Liana (con fruta o de cuenta) y la Flor nunca van con Cuia chica, Cuia alta ni Pirana (capacidades mixtas, bloqueo C1-9 de AF.Core; K3b): el generador no puede cumplir la celda"),
    ];

    /// <summary>Hueco máximo entre dos apariciones de una mecánica o capacidad (resumen, punto 6; d.2 regla 4).</summary>
    public const int MaxGap = 120;

    public static List<SpecIssue> Validate(CampaignSpec spec)
    {
        var issues = new List<SpecIssue>();
        void Fail(string rule, int level, string message) => issues.Add(new SpecIssue(rule, level, message));

        // CAL-COVER
        for (int n = spec.FirstLevel; n <= spec.LastLevel; n++)
        {
            if (!spec.Calendar.ContainsKey(n))
            {
                Fail("CAL-COVER", n, "el calendario no tiene este nivel");
            }
        }

        foreach (int n in spec.Calendar.Keys.Where(n => n < spec.FirstLevel || n > spec.LastLevel))
        {
            Fail("CAL-COVER", n, "nivel fuera de la especificacion");
        }

        if (issues.Count > 0)
        {
            return issues;
        }

        List<CalendarCell> cells = Enumerable.Range(spec.FirstLevel, spec.LastLevel - spec.FirstLevel + 1).Select(n => spec.Calendar[n]).ToList();

        foreach (CalendarCell c in cells)
        {
            int n = c.Level;
            bool ramp = n >= spec.RampFirst && n <= spec.RampLast;
            int p = ramp ? 0 : CampaignSpec.PositionOf(n);
            SlotRole? role = ramp ? null : spec.Positions.First(r => r.Positions.Contains(p)).Role;

            // CAL-ROLE
            if (c.PureCode == "R" && role != SlotRole.Recovery)
            {
                Fail("CAL-ROLE", n, "R (recuperacion) fuera de p1-p2");
            }

            if (c.PureCode == "S" && role != SlotRole.Remanso)
            {
                Fail("CAL-ROLE", n, "S (remanso) fuera de p19");
            }

            if (role == SlotRole.Remanso && c.PureCode != "S")
            {
                Fail("CAL-ROLE", n, "p19 es un remanso puro (S)");
            }

            if (role == SlotRole.Recovery && c.PureCode != "R" && !(c.Star && c.Mechanics.Count == 0))
            {
                Fail("CAL-ROLE", n, "p1-p2 son recuperaciones puras (R)");
            }

            // LABELS
            string? expected = ExpectedMark(spec, n, p, role);
            if (c.Mark != expected)
            {
                Fail("LABELS", n, $"marca '{c.Mark ?? "-"}' y se espera '{expected ?? "-"}' en p{p}");
            }

            // FORBIDDEN (d.3; Q14 A)
            if (c.Has("Tu"))
            {
                foreach (MechanicInfo m in c.Mechanics.Where(m => m.Code != "Tu" && !m.IsFilter))
                {
                    Fail("FORBIDDEN", n, $"Aguas turbias con {m.Id} ({m.Kind}): solo con filtros (Q14 A)");
                }
            }

            if (c.Mechanics.Count(m => m.IsCounter) > 1)
            {
                Fail("FORBIDDEN", n, "dos contadores en el mismo nivel (d.3; Lluvia nunca con Pirana)");
            }

            if (c.Has("Re") && c.Has("Pi"))
            {
                Fail("FORBIDDEN", n, "Remolino con Pirana [P: la ultima cuia destino no esta definida; LevelValidation lo rechaza]");
            }

            // MURKY-SLOT
            if (c.Has("Tu") && c.Mark is not ("D" or "MD"))
            {
                Fail("MURKY-SLOT", n, "Aguas turbias en un slot sin etiqueta");
            }

            // BEFORE-DEBUT
            foreach (MechanicInfo m in c.Mechanics.Where(m => n < m.Debut))
            {
                Fail("BEFORE-DEBUT", n, $"{m.Id} antes de su debut ({m.Debut})");
            }

            foreach (CapacityInfo cap in c.Capacities.Where(cap => n < cap.Debut))
            {
                Fail("BEFORE-DEBUT", n, $"{cap.Code} antes de su debut ({cap.Debut})");
            }

            // MAX-2
            if (c.Mechanics.Count > 2 && n < 300)
            {
                Fail("MAX-2", n, $"{c.Mechanics.Count} mecanicas antes del 300");
            }

            // CAPACITIES
            int chicas = c.Capacities.Count(x => x.Id == "chica") + (c.Has("Pi") ? 1 : 0);
            int altas = c.Capacities.Count(x => x.Id == "alta");
            if (n < 200 && (chicas > 2 || altas > 1))
            {
                Fail("CAPACITIES", n, $"{chicas} Cuias chicas y {altas} Cuias altas (como mucho 2 y 1 antes del 200)");
            }

            if (c.HasCapacity("Al6") && (role != SlotRole.Milestone || n < 141))
            {
                Fail("CAPACITIES", n, "Cuia alta de 6 fuera de un hito desde el 141");
            }

            // LIANA-CAPS: AF.Core rechaza la Liana con capacidades mixtas (C1-9), y también la Liana de cuenta y la Flor (K3b;
            // MechanicCodes.IsMixedCapacityBlocked); la celda no se puede generar tal cual y el generador no quita en silencio lo
            // que pide el calendario.
            foreach (MechanicInfo m in c.Mechanics.Where(m => MechanicCodes.IsMixedCapacityBlocked(m.Code)))
            {
                if (c.Capacities.Count > 0 || c.Has("Pi"))
                {
                    string what = m.Code switch { "Ln" => "Liana de cuenta", "Fl" => "Flor", _ => "Liana" };
                    Fail("LIANA-CAPS", n, what + " con capacidades mixtas (" + c.Short + "): bloqueo C1-9 de AF.Core; mueve la capacidad o la Pirana a una celda sin " + what);
                }
            }
        }

        // Debuts: primer uso, estrella y slot.
        foreach (MechanicInfo m in spec.MechanicTable)
        {
            List<CalendarCell> uses = cells.Where(c => c.Mechanics.Contains(m)).ToList();
            if (uses.Count == 0)
            {
                if (m.Debut <= spec.LastLevel)
                {
                    Fail("DEBUT-LEVEL", m.Debut, $"{m.Id} no aparece en el calendario");
                }

                continue;
            }

            CalendarCell first = uses[0];
            if (first.Level != m.Debut || !first.Star)
            {
                Fail("DEBUT-LEVEL", first.Level, $"primer {m.Id} en el {first.Level}{(first.Star ? "" : " sin estrella")}; su debut es el {m.Debut}");
            }

            List<CalendarCell> stars = uses.Where(c => c.Star).ToList();
            foreach (CalendarCell s in stars)
            {
                int p = CampaignSpec.PositionOf(s.Level);
                SlotRole role = spec.Positions.First(r => r.Positions.Contains(p)).Role;
                bool murkyMilestone = m.Code == "Tu" && role == SlotRole.Milestone;
                if (role is SlotRole.Challenge or SlotRole.Milestone && !murkyMilestone)
                {
                    Fail("DEBUT-SLOT", s.Level, $"estreno de {m.Id} en un {(role == SlotRole.Challenge ? "reto" : "hito")}");
                }

                if (s.Mechanics.Count != 1 || s.Capacities.Count != 0)
                {
                    Fail("DEBUT-SLOT", s.Level, $"el estreno de {m.Id} lleva otras mecanicas o capacidades");
                }
            }

            bool consecutive = stars.Select(s => s.Level).SequenceEqual(Enumerable.Range(m.Debut, stars.Count));
            if (m.Code != "Tu" && (stars.Count < 2 || stars.Count > 3 || !consecutive))
            {
                Fail("DEBUT-SLOT", m.Debut, $"{m.Id}: {stars.Count} tableros de estreno; deben ser 2-3 seguidos desde el {m.Debut} (02 sec. 9.2 regla 2)");
            }

            foreach (CalendarCell c in uses.Where(c => c.Star && c.Level > m.Debut + 2))
            {
                Fail("DEBUT-SLOT", c.Level, $"estrella de {m.Id} fuera de sus tableros de estreno");
            }
        }

        foreach (CapacityInfo cap in spec.CapacityTable)
        {
            CalendarCell? first = cells.FirstOrDefault(c => c.Capacities.Contains(cap));
            if (first is null)
            {
                if (cap.Debut <= spec.LastLevel)
                {
                    Fail("DEBUT-LEVEL", cap.Debut, $"{cap.Code} no aparece en el calendario");
                }

                continue;
            }

            if (first.Level != cap.Debut || !first.Star && cap.Code != "Al6")
            {
                Fail("DEBUT-LEVEL", first.Level, $"primer {cap.Code} en el {first.Level}; su debut es el {cap.Debut} con estrella");
            }
        }

        // Ciclos completos de 20.
        foreach ((int start, List<CalendarCell> cycle) in Cycles(spec, cells))
        {
            string name = CycleName(start);
            int pure = cycle.Count(c => c.IsPure);
            if (pure * 10 < cycle.Count * 3)
            {
                Fail("PURE-30", start, $"ciclo {name}: {pure}/{cycle.Count} niveles puros (< 30 %)");
            }

            int[] comfortPositions = [7, 8, 10, 11, 13, 14, 16, 17];
            int pureComfort = cycle.Count(c => comfortPositions.Contains(CampaignSpec.PositionOf(c.Level)) && c.IsPure);
            if (pureComfort < 3)
            {
                Fail("PURE-COMFORT", start, $"ciclo {name}: {pureComfort} comodidades puras en p7-p17 (al menos 3)");
            }

            int murky = cycle.Count(c => c.Has("Tu"));
            if (murky > 2)
            {
                Fail("MURKY-2", start, $"ciclo {name}: {murky} niveles con Aguas turbias (como mucho 2)");
            }

            foreach (MechanicInfo m in spec.MechanicTable.Where(m => m.Code != "Tu"))
            {
                int count = cycle.Count(c => c.Mechanics.Contains(m));
                bool debutCycle = m.Debut >= start && m.Debut < start + 20;
                int cap = debutCycle ? 8 : 6;
                if (count > cap)
                {
                    Fail("CAP-CYCLE", start, $"ciclo {name}: {m.Id} en {count} niveles (como mucho {cap})");
                }

                bool titular = debutCycle && CampaignSpec.PositionOf(m.Debut) == 3;
                if (titular && count != 8)
                {
                    Fail("TITULAR-8", start, $"ciclo {name}: la titular {m.Id} sale {count} veces (8 en su ciclo de debut)");
                }

                if (titular && start + 20 <= spec.LastLevel)
                {
                    bool returns = cells.Any(c => c.Level >= start + 20 && c.Level < start + 40 && c.Mechanics.Contains(m)
                        && spec.Positions.First(r => r.Positions.Contains(CampaignSpec.PositionOf(c.Level))).Role is SlotRole.Challenge or SlotRole.Milestone);
                    if (!returns)
                    {
                        Fail("RETURN", start + 20, $"la titular {m.Id} del ciclo {name} no vuelve como secundaria en un reto o hito del ciclo {CycleName(start + 20)}");
                    }
                }
            }

            // FIRST-TIME: estreno, tinte nuevo o primera combinación de dos códigos.
            bool novelty = cycle.Any(c => c.Star || c.TintDebutName is not null);
            if (!novelty)
            {
                var seenBefore = new HashSet<string>(cells.Where(c => c.Level < start).SelectMany(Combinations));
                novelty = cycle.SelectMany(Combinations).Any(combo => !seenBefore.Contains(combo));
            }

            if (!novelty)
            {
                Fail("FIRST-TIME", start, $"ciclo {name}: ninguna primera vez (estreno, tinte o combinacion nueva)");
            }
        }

        // NO-3-ROW
        foreach (MechanicInfo m in spec.MechanicTable)
        {
            int run = 0;
            foreach (CalendarCell c in cells)
            {
                bool counts = c.Mechanics.Contains(m) && !c.Star;
                run = counts ? run + 1 : 0;
                if (run == 3)
                {
                    Fail("NO-3-ROW", c.Level, $"{m.Id} en 3 niveles seguidos ({c.Level - 2}-{c.Level}) fuera de su estreno");
                }
            }
        }

        // GAP-120: mecánicas y, por id, las capacidades (la Cuia alta de 5 y la de 6 son la misma Cuia alta).
        IEnumerable<(string Id, List<int> Levels)> tracked = spec.MechanicTable
            .Select(m => (m.Id, cells.Where(c => c.Mechanics.Contains(m)).Select(c => c.Level).ToList()))
            .Concat(spec.CapacityTable.Select(x => x.Id).Distinct()
                .Select(id => (id, cells.Where(c => c.Capacities.Any(x => x.Id == id)).Select(c => c.Level).ToList())));
        foreach ((string id, List<int> levels) in tracked)
        {
            for (int i = 1; i < levels.Count; i++)
            {
                if (levels[i] - levels[i - 1] > MaxGap)
                {
                    Fail("GAP-120", levels[i], $"{id} pasa {levels[i] - levels[i - 1]} niveles sin volver");
                }
            }

            if (levels.Count > 0 && spec.LastLevel - levels[^1] > MaxGap)
            {
                Fail("GAP-120", spec.LastLevel, $"{id} no vuelve desde el {levels[^1]}");
            }
        }

        // SEASONS
        List<int> starts = Cycles(spec, cells).Select(x => x.Start).ToList();
        string? prev = null;
        int same = 0;
        foreach (int s in starts)
        {
            if (!spec.Seasons.TryGetValue(s, out string? season) || season is not ("A" or "B"))
            {
                Fail("SEASONS", s, $"el ciclo {CycleName(s)} no tiene temporada A o B");
                continue;
            }

            same = season == prev ? same + 1 : 1;
            prev = season;
            if (same > 2)
            {
                Fail("SEASONS", s, $"{same} ciclos seguidos de temporada {season} (como mucho 2)");
            }

            foreach (MechanicInfo m in spec.MechanicTable.Where(m => m.Debut >= s && m.Debut < s + 20 && m.Season is "A" or "B"))
            {
                if (m.Season != season)
                {
                    Fail("SEASONS", s, $"el ciclo {CycleName(s)} estrena {m.Id} (temporada {m.Season}) y es de temporada {season}");
                }
            }
        }

        return issues;
    }

    private static IEnumerable<string> Combinations(CalendarCell c)
    {
        var codes = c.Mechanics.Select(m => m.Code).Concat(c.Capacities.Select(x => x.Code)).Order(StringComparer.Ordinal).ToList();
        for (int i = 0; i < codes.Count; i++)
        {
            for (int j = i + 1; j < codes.Count; j++)
            {
                yield return codes[i] + "+" + codes[j];
            }
        }
    }

    private static string? ExpectedMark(CampaignSpec spec, int level, int p, SlotRole? role)
    {
        if (role is null)
        {
            return null;
        }

        if (role == SlotRole.Milestone)
        {
            return level < spec.FirstHard ? "s" : level < spec.FirstVeryHard ? "D" : "MD";
        }

        if (role != SlotRole.Challenge)
        {
            return null;
        }

        if (level < spec.FirstHard)
        {
            return "s";
        }

        return level < spec.FirstVeryHard ? (spec.Labeled4159.Contains(p) ? "D" : "s") : "D";
    }

    private static IEnumerable<(int Start, List<CalendarCell> Cycle)> Cycles(CampaignSpec spec, List<CalendarCell> cells)
    {
        for (int start = spec.CycleFrom; start + 19 <= spec.LastLevel; start += 20)
        {
            yield return (start, cells.Where(c => c.Level >= start && c.Level < start + 20).ToList());
        }
    }

    private static string CycleName(int start) => string.Create(CultureInfo.InvariantCulture, $"{start}-{start + 19}");

    /// <summary>Resumen ASCII por ciclo: puros, retos, etiquetas objetivo y debuts.</summary>
    public static string Summary(CampaignSpec spec)
    {
        var sb = new StringBuilder();
        IReadOnlyDictionary<int, string> intro = spec.IntroLevels;
        var blocks = new List<(int First, int Last, string Name)> { (spec.RampFirst, spec.RampLast, "rampa") };
        for (int start = spec.CycleFrom; start <= spec.LastLevel; start += 20)
        {
            blocks.Add((start, Math.Min(start + 19, spec.LastLevel), "temporada " + (spec.Seasons.TryGetValue(start, out string? s) ? s : "-")));
        }

        foreach ((int first, int last, string name) in blocks)
        {
            List<CalendarCell> cells = Enumerable.Range(first, last - first + 1).Where(spec.Calendar.ContainsKey).Select(n => spec.Calendar[n]).ToList();
            int pure = cells.Count(c => c.IsPure);
            string retos = string.Join(" ", cells.Where(c => c.Mark is not null).Select(c => string.Create(CultureInfo.InvariantCulture, $"{c.Level}{(c.Mark is null ? "" : "(" + c.Mark + ")")}")));
            int d = cells.Count(c => c.Mark == "D");
            int md = cells.Count(c => c.Mark == "MD");
            var debuts = new List<string>();
            foreach (CalendarCell c in cells)
            {
                if (intro.TryGetValue(c.Level, out string? id))
                {
                    debuts.Add($"{id}@{c.Level}");
                }

                if (c.TintDebutName is not null)
                {
                    debuts.Add($"tinte {spec.Tints.First(t => t.NameEs == c.TintDebutName).Id}@{c.Level}");
                }

                foreach (CapacityInfo cap in c.Capacities.Where(cap => cap.Debut == c.Level && cap.Code != "Ch" && cap.Code != "Al"))
                {
                    debuts.Add($"{cap.Code}@{c.Level}");
                }
            }

            sb.Append(string.Create(CultureInfo.InvariantCulture,
                $"{first,3}-{last,3} {name,-12} puros {pure,2}/{cells.Count,2} ({100 * pure / Math.Max(1, cells.Count),3} %) | retos {retos,-40} | etiquetas objetivo D={d} MD={md} | debuts: {(debuts.Count == 0 ? "-" : string.Join(", ", debuts))}"))
                .Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>Ejecuta --validate-spec: 0 = cumple; 1 = alguna regla falla; 2 = especificación ilegible.</summary>
    public static int Run(string path)
    {
        CampaignSpec spec;
        try
        {
            spec = CampaignSpec.Load(path);
        }
        catch (CampaignSpecException e)
        {
            Console.Error.WriteLine(AsciiText.Of($"level-generator: {e.Message}"));
            return 2;
        }

        List<SpecIssue> issues = Validate(spec);
        Console.WriteLine(AsciiText.Of($"especificacion {spec.SpecId} ({path.Replace('\\', '/')}): niveles {spec.FirstLevel}-{spec.LastLevel}"));
        Console.Write(AsciiText.Of(Summary(spec)));
        foreach (SpecIssue issue in issues.OrderBy(i => i.Warning).ThenBy(i => i.Level).ThenBy(i => i.Rule, StringComparer.Ordinal))
        {
            Console.WriteLine(AsciiText.Of(string.Create(CultureInfo.InvariantCulture,
                $"{(issue.Warning ? "AVISO" : "FAIL ")} [{issue.Rule}] nivel {issue.Level}: {issue.Message}")));
        }

        int failures = issues.Count(i => !i.Warning);
        int warnings = issues.Count(i => i.Warning);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{Rules.Length} reglas; {failures} fallo(s), {warnings} aviso(s)"));
        if (spec.ExpectRule is not null)
        {
            bool present = issues.Any(i => !i.Warning && i.Rule == spec.ExpectRule);
            Console.WriteLine($"esperado: [{spec.ExpectRule}] {(present ? "presente" : "AUSENTE")}");
        }

        return failures == 0 ? 0 : 1;
    }
}

/// <summary>Texto ASCII para la consola (.claude/rules/ci.md).</summary>
internal static class AsciiText
{
    public static string Of(string text)
    {
        string t = text.Replace("→", "->").Replace("×", "x").Replace("≤", "<=").Replace("≥", ">=").Replace("§", "sec. ")
            .Replace("★", "*").Replace("−", "-").Replace("«", "\"").Replace("»", "\"");
        string decomposed = t.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
        {
            if (c < 128)
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
