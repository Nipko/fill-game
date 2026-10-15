// --validate-spec: comprueba la especificación de la campaña contra las reglas de la rotación (propuesta de D-007 d.2,
// d.3 y las «Comprobaciones de la tabla» de d.4; d.5.1 y d.5.5 desde el 201), las del escalón de D-009 (c.3, c.4) y las de
// 02 §8-§9.2 que se pueden decidir sin generar tableros. Cada regla tiene un id estable (lo citan los fixtures de
// specs/tests/). La salida es ASCII (.claude/rules/ci.md).
//
// P4 de la tanda 4 (201-700, campaign_v2.json):
//   - las reglas que necesitan historia (GAP-120, NO-3-ROW, SEASONS, RETURN, FIRST-TIME y NOVELTY-60) cuentan también el
//     calendario de las especificaciones anteriores ("previous": campaign_v1, 11-200), pero solo fallan en los niveles de esta;
//   - una especificación con dos variantes (d.5.4) se comprueba entera en cada una, y cada variante con su propio registro de
//     combinaciones, calculado en memoria (d.5.6: nunca contra el archivo versionado);
//   - FIRST-TIME cuenta conjuntos de códigos del registro (CombinationsRegistry), no pares: un conjunto que ya salió, también
//     en 11-200, no es nuevo; NOVELTY-60 mide el hueco entre dos primeras veces seguidas (entradas del registro o tintes nuevos);
//   - los topes (MAX-2, CAPACITIES) y los «antes del N» salen de la especificación (mechanics_per_level, capacities_per_level).
// Con campaign_v1.json se aplican las 21 reglas de antes y dan lo mismo; FALLBACK, HITO-14 y NOVELTY-60 solo se aplican a las
// especificaciones que los necesitan (Applies).
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AmazonFill.Tools.LevelGenerator.Campaign;

internal sealed record SpecIssue(string Rule, int Level, string Message, bool Warning = false, string? Variant = null);

internal static class SpecValidator
{
    /// <summary>Reglas que comprueba el validador, con su fuente (las imprime --validate-spec).</summary>
    public static readonly (string Id, string Source)[] Rules =
    [
        ("CAL-COVER", "calendario completo del primer al ultimo nivel, sin huecos ni extras"),
        ("CAL-ROLE", "R solo en p1-p2, S solo en p19 y p1-p2/p19 puros (c.3, c.4; 22 = Cuia chica de 3 del canon)"),
        ("LABELS", "marcas (s)/(D)/(MD) en su posicion: 21-39 retos (s); 40 (D); 41-59 (D) en p6/p12/p18 y (s) en p9/p15; 60+ retos (D) e hito (MD) (D-009; c.3 regla 7)"),
        ("TITULAR-8", "cada titular sale 8 veces en su ciclo de debut (d.4); la Tinaja, 7 si su hito usa el respaldo (d.5.4; 03 sec. 12: como mucho 8)"),
        ("PURE-30", "al menos un 30 % de niveles puros por ciclo (d.2 regla 1)"),
        ("PURE-COMFORT", "al menos 3 de las 8 comodidades de p7-p17 puras (c.4)"),
        ("FORBIDDEN", "combinaciones prohibidas: turbias solo con filtros (Q14 A), un contador por nivel, Lluvia nunca con Pirana, Remolino nunca con Pirana [P] (d.3); not_with y only_with de la tabla y una variante nunca con su base (d.5.1)"),
        ("MURKY-SLOT", "Aguas turbias solo en slots con etiqueta (T1; propuesta b.2)"),
        ("MURKY-2", "Aguas turbias como mucho 2 veces por ciclo (propuesta b.2)"),
        ("BEFORE-DEBUT", "ninguna mecanica ni capacidad antes de su debut (D-007; 02 sec. 8)"),
        ("DEBUT-LEVEL", "el primer tablero de cada mecanica y capacidad es su nivel de debut y lleva estrella (D-007; 02 sec. 8)"),
        ("DEBUT-SLOT", "ningun debut en un reto o un hito salvo las turbias en el 100; 2-3 tableros de estreno seguidos, con la mecanica sola (c.3 regla 6; 02 sec. 9.2 regla 2)"),
        ("MAX-2", "mecanicas por nivel dentro del tope de mechanics_per_level: 2 antes del 300 y 3 despues, 3 solo en retos e hitos; las capacidades no cuentan [P] (02 sec. 9.2 regla 4; d.5.1)"),
        ("RETURN", "cada titular vuelve como secundaria (reto o hito) en el ciclo siguiente (d.4)"),
        ("CAP-CYCLE", "una mecanica sale en 6 niveles por ciclo como mucho (8 en su ciclo de debut) (d.2 regla 2)"),
        ("NO-3-ROW", "una mecanica nunca en 3 niveles seguidos fuera de su estreno (d.2 regla 2)"),
        ("SEASONS", "temporadas A/B alternas, nunca mas de 2 ciclos seguidos iguales; el ciclo de debut toma la temporada de la mecanica, salvo las variantes (d.2 regla 7)"),
        ("GAP-120", "ninguna mecanica ni capacidad (Cuia chica, Cuia alta) pasa mas de 120 niveles sin volver, contando las especificaciones anteriores (resumen, punto 6; d.2 regla 4; 02 sec. 8)"),
        ("FIRST-TIME", "al menos una primera vez por ciclo: estreno, tinte nuevo o entrada del registro de combinaciones, un conjunto de codigos nunca visto (d.2 regla 3; d.5.6)"),
        ("CAPACITIES", "Cuias chicas (con la cueva de la Pirana) y Cuias altas por nivel dentro de capacities_per_level (2 y 1 antes del 200); Cuia alta de 6 solo en hitos desde el 141 (02 sec. 8)"),
        ("LIANA-CAPS", "la Liana (con fruta o de cuenta) y la Flor nunca van con Cuia chica, Cuia alta ni Pirana (capacidades mixtas, bloqueo C1-9 de AF.Core; K3b): el generador no puede cumplir la celda"),
        ("FALLBACK", "con dos variantes, cada celda con una mecanica de una sola variante (la Tinaja) lleva su respaldo sin ella en fallback, y solo esas (d.5.4; D-019 salida B)"),
        ("HITO-14", "K >= 13 (14 cuias con 1 vacio) solo en hitos, con \"K = 13\" en la celda igual al k_plan, sin capacidades, Pirana ni Tinaja (03 sec. 11; d.5.1)"),
        ("NOVELTY-60", "nunca mas de novelty_max_gap (60) niveles entre dos primeras veces seguidas: entradas del registro de combinaciones o tintes nuevos, contando las especificaciones anteriores (D-009 punto 8; d.5 punto 5; d.5.6)"),
    ];

    /// <summary>Hueco máximo entre dos apariciones de una mecánica o capacidad (resumen, punto 6; d.2 regla 4).</summary>
    public const int MaxGap = 120;

    /// <summary>Cuias generadas como mucho (02 R1): con 1 vacío, K = 13 es el hito de 14 cuias (03 §11; d.5.1).</summary>
    public const int MaxCuias = 14;

    public const int HitoK = MaxCuias - 1;

    /// <summary>La regla se aplica a la especificación (las 21 de campaign_v1 siempre; las de 201-700 solo si las necesita).</summary>
    public static bool Applies(string rule, CampaignSpec spec) => rule switch
    {
        "FALLBACK" => spec.HasVariants,
        "HITO-14" => spec.KPlan.Any(r => Math.Max(Math.Max(r.Recovery, r.Comfort), Math.Max(r.Challenge, r.Milestone)) >= HitoK)
            || spec.Calendar.Values.Any(c => NoteK(c) is not null),
        "NOVELTY-60" => spec.NoveltyMaxGap is not null,
        _ => true,
    };

    public static List<SpecIssue> Validate(CampaignSpec spec)
    {
        var issues = new List<SpecIssue>();
        void Fail(string rule, int level, string message) => issues.Add(new SpecIssue(rule, level, message, Variant: spec.HasVariants ? spec.Variant : null));

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

        // Historia ("previous") más esta especificación, por nivel: la cuentan GAP-120, NO-3-ROW, FIRST-TIME y NOVELTY-60.
        List<CalendarCell> all = spec.CellsWithHistory();
        List<CombinationEntry> registry = CombinationsRegistry.Build(all, spec.VariantCodes);
        var registryLevels = registry.Select(e => e.Level).ToHashSet();
        bool hito14 = Applies("HITO-14", spec);

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

            // FORBIDDEN de la tabla (d.5.1): not_with, only_with y una variante con su base. Un mensaje por par de códigos y
            // motivo: not_with se lee en las dos direcciones (Fl+Ln da un solo fallo aunque cada una lleve a la otra en su
            // not_with) con el par en orden ordinal; only_with es de la mecánica que lo declara, y variant_of, de la variante.
            var forbidden = new SortedSet<string>(StringComparer.Ordinal);
            foreach (MechanicInfo m in c.Mechanics)
            {
                foreach (MechanicInfo other in c.Mechanics.Where(o => o.Code != m.Code))
                {
                    bool ordered = string.CompareOrdinal(m.Code, other.Code) < 0;
                    string pair = ordered ? $"{m.Code}+{other.Code}" : $"{other.Code}+{m.Code}";
                    if (m.NotWith.Contains(other.Code) || other.NotWith.Contains(m.Code))
                    {
                        (MechanicInfo lo, MechanicInfo hi) = ordered ? (m, other) : (other, m);
                        forbidden.Add($"{pair}: {lo.Id} y {hi.Id} no van juntas (not_with; d.5.1)");
                    }

                    if (m.OnlyWith is not null && !m.OnlyWith.Contains(other.Code))
                    {
                        forbidden.Add($"{pair}: {m.Id} solo va con {string.Join(", ", m.OnlyWith)} (only_with; d.5.1)");
                    }

                    if (m.VariantOf == other.Code)
                    {
                        forbidden.Add($"{pair}: una mecanica y su variante no van juntas ({other.Id} y {m.Id}; d.5.1, b.3)");
                    }
                }
            }

            foreach (string message in forbidden)
            {
                Fail("FORBIDDEN", n, message);
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

            // MAX-2 (02 §9.2 regla 4): el tope de la especificación para el rol del nivel.
            int maxMechanics = spec.MaxMechanicsAt(n, role);
            if (c.Mechanics.Count > maxMechanics)
            {
                Fail("MAX-2", n, $"{c.Mechanics.Count} mecanicas (como mucho {maxMechanics} en {RoleText(role)} del {n}; mechanics_per_level)");
            }

            // CAPACITIES
            int chicas = c.Capacities.Count(x => x.Id == "chica") + (c.Has("Pi") ? 1 : 0);
            int altas = c.Capacities.Count(x => x.Id == "alta");
            if (n < spec.CapacityLimitsBefore && (chicas > spec.MaxChicas || altas > spec.MaxAltas))
            {
                Fail("CAPACITIES", n, $"{chicas} Cuias chicas y {altas} Cuias altas (como mucho {spec.MaxChicas} y {spec.MaxAltas} antes del {spec.CapacityLimitsBefore})");
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

            // HITO-14 (03 §11; d.5.1): K = 13 + 1 vacío solo en hitos, declarado en la celda y sin capacidades, cueva ni Tinaja.
            if (hito14)
            {
                int? k = spec.KAt(n);
                int? noted = NoteK(c);
                if (noted is int nk && nk != k)
                {
                    Fail("HITO-14", n, $"la celda dice K = {nk} y el k_plan da K = {(k is null ? "-" : k.Value.ToString(CultureInfo.InvariantCulture))}");
                }

                if (k >= HitoK)
                {
                    if (role != SlotRole.Milestone)
                    {
                        Fail("HITO-14", n, $"K = {k} ({k + 1} cuias o mas) fuera de un hito");
                    }
                    else if (noted is null)
                    {
                        Fail("HITO-14", n, $"hito de K = {k} sin \"K = {k}\" en la celda ({c.Literal})");
                    }

                    if (c.Capacities.Count > 0 || c.Mechanics.Any(m => m.CaveCap > 0 || m.Code == MechanicCodes.Tinaja))
                    {
                        Fail("HITO-14", n, $"hito de 14 cuias con capacidades, la cueva de la Pirana o la Tinaja ({c.Short}; d.5.1)");
                    }
                }
            }
        }

        // FALLBACK (d.5.4): en cada variante, ninguna celda con una mecánica de la otra; en la «a» (la que se publica), cada celda
        // con una mecánica de una sola variante lleva su respaldo, y los respaldos solo en esas celdas y sin ella.
        if (spec.HasVariants)
        {
            foreach (CalendarCell c in cells)
            {
                foreach (MechanicInfo m in c.Mechanics.Where(m => !spec.MechanicTable.Contains(m)))
                {
                    Fail("FALLBACK", c.Level, $"{m.Id} no existe en la variante {spec.Variant} (only_variant {m.OnlyVariant}): la celda necesita su respaldo en fallback");
                }
            }

            if (spec.Variant == CampaignSpec.VariantA)
            {
                foreach (CalendarCell c in cells.Where(c => !spec.FallbackCells.ContainsKey(c.Level)))
                {
                    foreach (MechanicInfo m in c.Mechanics.Where(m => m.OnlyVariant is not null))
                    {
                        Fail("FALLBACK", c.Level, $"celda con {m.Id} (only_variant {m.OnlyVariant}) sin respaldo en fallback ({c.Literal}; d.5.4)");
                    }
                }

                foreach ((int level, CalendarCell fb) in spec.FallbackCells.OrderBy(kv => kv.Key))
                {
                    if (!spec.Calendar[level].Mechanics.Any(m => m.OnlyVariant is not null))
                    {
                        Fail("FALLBACK", level, $"respaldo '{fb.Literal}' de una celda sin mecanica de una sola variante ({spec.Calendar[level].Literal})");
                    }

                    foreach (MechanicInfo m in fb.Mechanics.Where(m => m.OnlyVariant is not null))
                    {
                        Fail("FALLBACK", level, $"el respaldo '{fb.Literal}' lleva {m.Id}, que es de una sola variante");
                    }
                }
            }
        }

        // Debuts: primer uso, estrella y slot. Con "previous", los debuts anteriores al primer nivel los comprueba la
        // especificación anterior (campaign_v1 para los de 11-200).
        bool OwnDebut(int debut) => spec.Previous is null || debut >= spec.FirstLevel;
        foreach (MechanicInfo m in spec.MechanicTable.Where(m => OwnDebut(m.Debut)))
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

        foreach (CapacityInfo cap in spec.CapacityTable.Where(cap => OwnDebut(cap.Debut)))
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
                if (titular && count != 8 && !MilestoneFallbackApplied(spec, cycle, m, count))
                {
                    Fail("TITULAR-8", start, $"ciclo {name}: la titular {m.Id} sale {count} veces (8 en su ciclo de debut)");
                }

                if (titular && start + 20 <= spec.LastLevel && !ReturnsIn(spec, cells, m, start + 20))
                {
                    Fail("RETURN", start + 20, $"la titular {m.Id} del ciclo {name} no vuelve como secundaria en un reto o hito del ciclo {CycleName(start + 20)}");
                }
            }

            // FIRST-TIME: estreno, tinte nuevo o entrada del registro (un conjunto de códigos nunca visto, d.5.6).
            bool novelty = cycle.Any(c => c.Star || c.TintDebutName is not null || registryLevels.Contains(c.Level));
            if (!novelty)
            {
                Fail("FIRST-TIME", start, $"ciclo {name}: ninguna primera vez (estreno, tinte o entrada nueva del registro de combinaciones)");
            }
        }

        // RETURN de las titulares que debutan en el último ciclo de la especificación anterior (183 Piraña → 201-220).
        foreach (MechanicInfo m in spec.MechanicTable.Where(m => m.Code != "Tu" && m.Debut < spec.FirstLevel && CampaignSpec.PositionOf(m.Debut) == 3))
        {
            int next = m.Debut - 2 + 20;
            if (next >= spec.FirstLevel && next + 19 <= spec.LastLevel && !ReturnsIn(spec, cells, m, next))
            {
                Fail("RETURN", next, $"la titular {m.Id} del ciclo {CycleName(next - 20)} no vuelve como secundaria en un reto o hito del ciclo {CycleName(next)}");
            }
        }

        // NO-3-ROW (con la historia: el 199-201 cuenta; solo falla en los niveles de esta especificación).
        foreach (MechanicInfo m in spec.MechanicTable)
        {
            int run = 0;
            foreach (CalendarCell c in all)
            {
                bool counts = c.Mechanics.Any(x => x.Code == m.Code) && !c.Star;
                run = counts ? run + 1 : 0;
                if (run == 3 && c.Level >= spec.FirstLevel)
                {
                    Fail("NO-3-ROW", c.Level, $"{m.Id} en 3 niveles seguidos ({c.Level - 2}-{c.Level}) fuera de su estreno");
                }
            }
        }

        // GAP-120: mecánicas y, por id, las capacidades (la Cuia alta de 5 y la de 6 son la misma Cuia alta), con la historia.
        IEnumerable<(string Id, List<int> Levels)> tracked = spec.MechanicTable
            .Select(m => (m.Id, all.Where(c => c.Mechanics.Any(x => x.Code == m.Code)).Select(c => c.Level).ToList()))
            .Concat(spec.CapacityTable.Select(x => x.Id).Distinct()
                .Select(id => (id, all.Where(c => c.Capacities.Any(x => x.Id == id)).Select(c => c.Level).ToList())));
        foreach ((string id, List<int> levels) in tracked)
        {
            for (int i = 1; i < levels.Count; i++)
            {
                if (levels[i] - levels[i - 1] > MaxGap && levels[i] >= spec.FirstLevel)
                {
                    Fail("GAP-120", levels[i], $"{id} pasa {levels[i] - levels[i - 1]} niveles sin volver");
                }
            }

            if (levels.Count > 0 && spec.LastLevel - levels[^1] > MaxGap)
            {
                Fail("GAP-120", spec.LastLevel, $"{id} no vuelve desde el {levels[^1]}");
            }
        }

        // SEASONS (la racha de temporadas sigue desde la especificación anterior).
        string? prev = null;
        int same = 0;
        foreach ((int s, string? season) in spec.CycleSeasonsWithHistory())
        {
            bool own = s >= spec.FirstLevel;
            if (season is not ("A" or "B"))
            {
                if (own)
                {
                    Fail("SEASONS", s, $"el ciclo {CycleName(s)} no tiene temporada A o B");
                }

                continue;
            }

            same = season == prev ? same + 1 : 1;
            prev = season;
            if (!own)
            {
                continue;
            }

            if (same > 2)
            {
                Fail("SEASONS", s, $"{same} ciclos seguidos de temporada {season} (como mucho 2)");
            }

            foreach (MechanicInfo m in spec.MechanicTable.Where(m => m.Debut >= s && m.Debut < s + 20 && m.Season is "A" or "B" && m.VariantOf is null))
            {
                if (m.Season != season)
                {
                    Fail("SEASONS", s, $"el ciclo {CycleName(s)} estrena {m.Id} (temporada {m.Season}) y es de temporada {season}");
                }
            }
        }

        // NOVELTY-60 (D-009 punto 8; d.5 punto 5): hueco entre dos primeras veces seguidas, desde la última de la historia.
        if (spec.NoveltyMaxGap is int maxNovelty)
        {
            List<int> firsts = NoveltyLevels(all, registry);
            for (int i = 1; i < firsts.Count; i++)
            {
                if (firsts[i] >= spec.FirstLevel && firsts[i] - firsts[i - 1] > maxNovelty)
                {
                    Fail("NOVELTY-60", firsts[i], $"{firsts[i] - firsts[i - 1]} niveles sin una primera vez ({firsts[i - 1]} -> {firsts[i]}; como mucho {maxNovelty})");
                }
            }

            if (firsts.Count == 0 || spec.LastLevel - firsts[^1] > maxNovelty)
            {
                Fail("NOVELTY-60", spec.LastLevel, $"{(firsts.Count == 0 ? "ninguna primera vez" : $"ninguna primera vez desde el {firsts[^1]}")} hasta el {spec.LastLevel} (como mucho {maxNovelty})");
            }
        }

        return issues;
    }

    /// <summary>Niveles con una primera vez para NOVELTY-60: entradas del registro y debuts de tinte, en orden.</summary>
    public static List<int> NoveltyLevels(IEnumerable<CalendarCell> cells, IEnumerable<CombinationEntry> registry) =>
        registry.Select(e => e.Level).Concat(cells.Where(c => c.TintDebutName is not null).Select(c => c.Level)).Distinct().Order().ToList();

    /// <summary>Hueco máximo entre dos primeras veces seguidas desde <paramref name="from"/> (para el resumen): (hueco, desde, hasta).</summary>
    public static (int Gap, int From, int To) MaxNoveltyGap(List<int> firsts, int from)
    {
        (int Gap, int From, int To) best = (0, 0, 0);
        for (int i = 1; i < firsts.Count; i++)
        {
            if (firsts[i] >= from && firsts[i] - firsts[i - 1] > best.Gap)
            {
                best = (firsts[i] - firsts[i - 1], firsts[i - 1], firsts[i]);
            }
        }

        return best;
    }

    /// <summary>
    /// TITULAR-8 admite 7 apariciones de una mecánica de una sola variante (la Tinaja) en su ciclo de debut si el hito del ciclo
    /// no la lleva: es el respaldo del hito 340 aplicado en la «a» (d.5.4: «el pipeline usa Br+Ba (MD) y la Tinaja se queda en 7
    /// apariciones en su ciclo»; 03 §12: «como mucho 8 niveles en el ciclo»). Las demás titulares siguen con 8 exactas (d.4).
    /// </summary>
    private static bool MilestoneFallbackApplied(CampaignSpec spec, List<CalendarCell> cycle, MechanicInfo m, int count) =>
        m.OnlyVariant is not null && count == 7
        && cycle.Any(c => spec.Positions.First(r => r.Positions.Contains(CampaignSpec.PositionOf(c.Level))).Role == SlotRole.Milestone
            && !c.Mechanics.Contains(m));

    private static bool ReturnsIn(CampaignSpec spec, List<CalendarCell> cells, MechanicInfo m, int cycleStart) =>
        cells.Any(c => c.Level >= cycleStart && c.Level < cycleStart + 20 && c.Mechanics.Contains(m)
            && spec.Positions.First(r => r.Positions.Contains(CampaignSpec.PositionOf(c.Level))).Role is SlotRole.Challenge or SlotRole.Milestone);

    /// <summary>K declarado en la celda («(MD; K = 13)»), o null.</summary>
    public static int? NoteK(CalendarCell c)
    {
        foreach (string note in c.Notes)
        {
            Match m = Regex.Match(note, @"^K\s*=\s*(\d+)$");
            if (m.Success)
            {
                return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    private static string RoleText(SlotRole? role) => role switch
    {
        null => "la rampa",
        SlotRole.Challenge => "un reto",
        SlotRole.Milestone => "un hito",
        SlotRole.Recovery => "una recuperacion",
        SlotRole.Remanso => "un remanso",
        _ => "una comodidad",
    };

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
        var blocks = new List<(int First, int Last, string Name)>();
        if (spec.HasRamp)
        {
            blocks.Add((spec.RampFirst, spec.RampLast, "rampa"));
        }

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

    /// <summary>
    /// Ejecuta --validate-spec: 0 = cumple; 1 = alguna regla falla; 2 = especificación ilegible. Una especificación con dos
    /// variantes se comprueba en las dos (d.5.4), cada una con su registro de combinaciones en memoria (d.5.6).
    /// </summary>
    public static int Run(string path)
    {
        var specs = new List<CampaignSpec>();
        try
        {
            CampaignSpec published = CampaignSpec.Load(path);
            foreach (string variant in published.Variants)
            {
                specs.Add(variant == published.Variant ? published : CampaignSpec.Load(path, variant));
            }
        }
        catch (CampaignSpecException e)
        {
            Console.Error.WriteLine(AsciiText.Of($"level-generator: {e.Message}"));
            return 2;
        }

        CampaignSpec main = specs[0];
        var issues = new List<SpecIssue>();
        string header = $"especificacion {main.SpecId} ({path.Replace('\\', '/')}): niveles {main.FirstLevel}-{main.LastLevel}";
        if (main.Previous is not null)
        {
            header += $"; historia: {string.Join(", ", main.Previous.SourceIds)} ({main.Previous.CellsWithHistory()[0].Level}-{main.Previous.LastLevel})";
        }

        if (main.HasVariants)
        {
            header += $"; variantes {string.Join(", ", main.Variants)} (publicada: {main.PublishedVariant})";
        }

        Console.WriteLine(AsciiText.Of(header));
        foreach (CampaignSpec spec in specs)
        {
            if (main.HasVariants)
            {
                Console.WriteLine($"-- variante {spec.Variant} --");
            }

            Console.Write(AsciiText.Of(Summary(spec)));
            if (spec.Previous is not null || spec.NoveltyMaxGap is not null)
            {
                List<CalendarCell> all = spec.CellsWithHistory();
                List<CombinationEntry> registry = CombinationsRegistry.Build(all, spec.VariantCodes);
                int before = registry.Count(e => e.Level < spec.FirstLevel);
                (int gap, int from, int to) = MaxNoveltyGap(NoveltyLevels(all, registry), spec.FirstLevel);
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"registro de combinaciones: {registry.Count} entradas ({before} antes del {spec.FirstLevel} y {registry.Count - before} en {spec.FirstLevel}-{spec.LastLevel}); hueco maximo entre primeras veces: {gap} ({from} -> {to})"));
            }

            issues.AddRange(Validate(spec));
        }

        foreach (SpecIssue issue in issues.OrderBy(i => i.Warning).ThenBy(i => i.Level).ThenBy(i => i.Rule, StringComparer.Ordinal)
                     .ThenBy(i => i.Variant, StringComparer.Ordinal))
        {
            string where = issue.Variant is null ? "" : $"variante {issue.Variant}, ";
            Console.WriteLine(AsciiText.Of(string.Create(CultureInfo.InvariantCulture,
                $"{(issue.Warning ? "AVISO" : "FAIL ")} [{issue.Rule}] {where}nivel {issue.Level}: {issue.Message}")));
        }

        int failures = issues.Count(i => !i.Warning);
        int warnings = issues.Count(i => i.Warning);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{Rules.Count(r => Applies(r.Id, main))} reglas; {failures} fallo(s), {warnings} aviso(s)"));
        if (main.ExpectRule is not null)
        {
            bool present = issues.Any(i => !i.Warning && i.Rule == main.ExpectRule && (main.ExpectVariant is null || i.Variant == main.ExpectVariant));
            string inVariant = main.ExpectVariant is null ? "" : $" en la variante {main.ExpectVariant}";
            Console.WriteLine($"esperado: [{main.ExpectRule}]{inVariant} {(present ? "presente" : "AUSENTE")}");
        }

        return failures == 0 ? 0 : 1;
    }
}

/// <summary>Entrada del registro de combinaciones publicadas (d.5.6): el primer nivel de un conjunto de códigos.</summary>
internal sealed record CombinationEntry(int Level, IReadOnlyList<string> Codes, string Kind);

/// <summary>
/// Registro de combinaciones publicadas (propuesta de D-009 d.2 regla 3 y d.5.6 [P]; 03 §17 comprobación 15): formato
/// <c>af-combinations/1</c> en <c>tools/level-generator/specs/combinations_registry_v1.json</c>, generado desde campaign_v1 y
/// campaign_v2 (la cadena de "previous") con la variante publicada; nunca a mano.
/// <list type="bullet">
/// <item><description>una entrada por conjunto de códigos de celda (mecánicas y capacidades, sin R, C, S, marcas, tintes ni
/// nombres), en el primer nivel donde sale; los códigos en orden ordinal y las entradas por nivel;</description></item>
/// <item><description>la entrada es el conjunto completo, no sus pares: un trío es nuevo si esos tres códigos nunca salieron
/// juntos; un conjunto que ya salió (también en 11-200) no vuelve a entrar;</description></item>
/// <item><description><c>kind</c>: <c>debut</c> (un código), <c>variant</c> (un código de una variante de la 1.0, Ln, Ha, B2 o
/// Fl) o <c>combination</c> (dos o más).</description></item>
/// </list>
/// UTF-8 sin BOM, LF y una entrada por línea: la misma especificación da los mismos bytes.
/// </summary>
internal static class CombinationsRegistry
{
    public const string Format = "af-combinations/1";

    public static List<CombinationEntry> Build(IEnumerable<CalendarCell> cellsInOrder, IReadOnlySet<string> variantCodes)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var entries = new List<CombinationEntry>();
        foreach (CalendarCell c in cellsInOrder.OrderBy(c => c.Level))
        {
            string[] codes = [.. c.Mechanics.Select(m => m.Code).Concat(c.Capacities.Select(x => x.Code)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            if (codes.Length == 0 || !seen.Add(string.Join("+", codes)))
            {
                continue;
            }

            string kind = codes.Length > 1 ? "combination" : variantCodes.Contains(codes[0]) ? "variant" : "debut";
            entries.Add(new CombinationEntry(c.Level, codes, kind));
        }

        return entries;
    }

    /// <summary>Los bytes del registro de la especificación (su variante y la cadena de "previous").</summary>
    public static byte[] Bytes(CampaignSpec spec)
    {
        List<CombinationEntry> entries = Build(spec.CellsWithHistory(), spec.VariantCodes);
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"format\": \"").Append(Format).Append("\",\n");
        sb.Append("  \"source\": [").Append(string.Join(", ", spec.SourceIds.Select(Quote))).Append("],\n");
        sb.Append("  \"variant\": ").Append(Quote(spec.Variant)).Append(",\n");
        sb.Append("  \"combinations\": [\n");
        for (int i = 0; i < entries.Count; i++)
        {
            CombinationEntry e = entries[i];
            sb.Append(string.Create(CultureInfo.InvariantCulture,
                $"    {{\"level\": {e.Level}, \"codes\": [{string.Join(", ", e.Codes.Select(Quote))}], \"kind\": {Quote(e.Kind)}}}"));
            sb.Append(i + 1 < entries.Count ? ",\n" : "\n");
        }

        sb.Append("  ]\n}\n");
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(sb.ToString());
    }

    private static string Quote(string s) => System.Text.Json.JsonSerializer.Serialize(s);

    /// <summary>
    /// Modo --registry: escribe (o, con --check, compara byte a byte sin escribir) el registro de la especificación con la
    /// variante <paramref name="variant"/> (null = la publicada). La especificación tiene que pasar --validate-spec en todas sus
    /// variantes, como en --validate-spec (d.5.4). Códigos: 0 = escrito o idéntico; 1 = difiere o la especificación no pasa;
    /// 2 = especificación ilegible.
    /// </summary>
    public static int Run(string specPath, string outPath, string? variant, bool check)
    {
        CampaignSpec spec;
        var failing = new List<string>();
        try
        {
            spec = CampaignSpec.Load(specPath, variant);
            foreach (string v in spec.Variants)
            {
                CampaignSpec each = v == spec.Variant ? spec : CampaignSpec.Load(specPath, v);
                if (SpecValidator.Validate(each).Any(i => !i.Warning))
                {
                    failing.Add(v);
                }
            }
        }
        catch (CampaignSpecException e)
        {
            Console.Error.WriteLine(AsciiText.Of($"level-generator --registry: {e.Message}"));
            return 2;
        }

        if (failing.Count > 0)
        {
            Console.Error.WriteLine($"level-generator --registry: la especificacion no pasa --validate-spec en la variante {string.Join(", ", failing)}; corrigela antes.");
            return 1;
        }

        byte[] bytes = Bytes(spec);
        int count = Build(spec.CellsWithHistory(), spec.VariantCodes).Count;
        string shown = outPath.Replace('\\', '/');
        if (check)
        {
            byte[]? onDisk = File.Exists(outPath) ? File.ReadAllBytes(outPath) : null;
            bool same = onDisk is not null && onDisk.AsSpan().SequenceEqual(bytes);
            Console.WriteLine($"--check: {shown} {(same ? "coincide" : "NO coincide")} con el registro de {spec.SpecId} (variante {spec.Variant}, {count} entradas)");
            return same ? 0 : 1;
        }

        string? dir = Path.GetDirectoryName(Path.GetFullPath(outPath));
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllBytes(outPath, bytes);
        Console.WriteLine($"registro: {shown} (variante {spec.Variant}, {count} entradas, {bytes.Length} bytes)");
        return 0;
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
