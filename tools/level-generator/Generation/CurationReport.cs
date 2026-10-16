// Informe de curación de un pack (P7a de la tanda 4; tools/out/curation_wNN.md): la propuesta de curación de los retos y los
// hitos que aprueba el dueño (09 §0.1, tabla de roles, fila de level-pipeline-dev: «El dueño aprueba la curación de retos e
// hitos de cada paquete»; 09 §0.3, ritmo de trabajo: «propuesta de curación de los 30 retos e hitos que aprueba el dueño»;
// 03 §7: «un nivel en slot de reto cuyo bot_fail quede por debajo de 0,75 se publica sin etiqueta y la curación decide si se
// sustituye por otro del pool»). Lo prueba --self-test curation (CurationSelfTest).
// Lo escribe --spec con --curation, con los mismos niveles colocados que el pack: retos e hitos con su etiqueta medida por
// el bot ×40 frente a la objetivo (D-009), los hitos uno a uno, los tableros de estreno (02 §9.2 regla 2), las variantes de
// la 1.0 (la sección de CampaignReport) y la tabla de la decisión del dueño. La decisión no se escribe en el informe (que es
// generado): sale del archivo versionado de --decisions (CurationDecisions; p. ej. specs/curation_v2.json) y, sin él o sin
// entrada para el nivel, es «pendiente».
// Sin tiempos ni rutas absolutas (si se pasan relativas): dos ejecuciones dan los mismos bytes. La propuesta es [P]: la
// decide el dueño.
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AF.Core.Board;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal static class CurationReport
{
    /// <summary>Decisión del dueño en un informe recién generado (09 §0.1 y §0.3).</summary>
    public const string Pending = "pendiente";

    public static string Build(
        CampaignSpec spec, List<GeneratedLevel> levels, Dictionary<RangeInfo, RangeStats> stats,
        List<(WorldInfo World, string Path, byte[] Bytes, int Levels)> packs, string levelsArg, string curationPath,
        CurationDecisions? decisions = null)
    {
        var sb = new StringBuilder();
        void W(string line = "") => sb.Append(line).Append('\n');
        string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);

        string specPath = spec.Path.Replace('\\', '/');
        string outDir = packs.Count == 0 ? "." : (Path.GetDirectoryName(packs[0].Path) ?? ".").Replace('\\', '/');
        string variantArg = spec.HasVariants ? F($" --variant {spec.Variant}") : "";
        string variantTitle = spec.HasVariants ? F($" (variante {spec.Variant})") : "";
        string? decisionsPath = decisions?.Path.Replace('\\', '/');
        string decisionsArg = decisionsPath is null ? "" : " --decisions " + decisionsPath;
        int first = levels.Min(l => l.Plan.Level);
        int last = levels.Max(l => l.Plan.Level);
        string worlds = string.Join(", ", levels.Select(l => l.Plan.World).Distinct().Select(w => F($"mundo {w.World} ({w.Name}, {w.First}-{w.Last})")));
        List<GeneratedLevel> picks = levels.Where(l => l.Plan.Role is SlotRole.Challenge or SlotRole.Milestone).ToList();

        W(F($"# Curación de {first}-{last}{variantTitle}: retos, hitos y estrenos para el dueño"));
        W();
        W(F($"Generado por `dotnet run --project tools/level-generator -c Release -- --spec {specPath} --levels {levelsArg}{variantArg} --out {outDir} --curation {curationPath.Replace('\\', '/')}{decisionsArg}` ")
            + "con los mismos niveles colocados que el pack: no se edita a mano y no lleva tiempos (la decisión del dueño sale de "
            + (decisionsPath is null ? "`--decisions`, que aquí no se pasó" : "`" + decisionsPath + "`") + "). "
            + "Niveles del " + worlds + ".");
        W();
        W("Fuentes: 09 §0.1 (fila de level-pipeline-dev: el dueño aprueba la curación de retos e hitos de cada paquete) y §0.3 "
          + "(propuesta de curación de los 30 retos e hitos que aprueba el dueño), 03 §7 (un reto por debajo de 0,75 se publica sin "
          + "etiqueta y la curación decide si se sustituye por otro del pool del slot; banda por el cuantil q de D en el pool del rango), D-009 (etiqueta por el bot ×40 con la semilla del nivel: "
          + "Difícil ≥ 0,75 y Muy difícil ≥ 0,90; retos en p6, p9, p12, p15 y p18 e hito en p20; c.2: un reto o el hito fuera de su "
          + "ventana se publica con la etiqueta que mide), 03 §11-§12, 02 §9.2 regla 2 (estrenos) y la tabla de novedades de D2 "
          + "(d.5.3; D-023 punto 10). La columna «Propuesta» es [P]: la decide el dueño.");
        W();
        W("Compuerta automática (verde antes de pedir la curación): `dotnet run --project tools/level-qa -c Release -- "
          + string.Join(" ", packs.Select(p => p.Path.Replace('\\', '/')))
          + F($" --spec {specPath} --levels {levelsArg}{variantArg}`. ")
          + "Cada reto o hito con aviso QA-WINDOW (bot_fail fuera de la ventana del slot) sale aquí como «revisar», y cada "
          + "hito con aviso QA-BAND-Q (fuera de la banda de hito con el mínimo cumplido), como «revisar» o con nota de banda; los avisos "
          + "QA-BAND-Q de las recuperaciones no entran en este informe. QA-BAND-Q no mira los retos y level-qa no mira risk_adjacent: las notas "
          + "de banda de los retos y los «revisar» por risk_adjacent salen solo de este informe.");
        W();

        // Resumen.
        W("## Resumen");
        W();
        W("| Pack | Niveles | `pack_schema` | Bytes | SHA-256 |");
        W("|---|---|---|---|---|");
        foreach ((WorldInfo world, string path, byte[] bytes, int count) in packs)
        {
            W(F($"| `{path.Replace('\\', '/')}` | {count} | {CampaignReport.PackSchemaOf(world, levels)} | {bytes.Length} | `{Convert.ToHexStringLower(SHA256.HashData(bytes))}` |"));
        }

        int challenges = picks.Count(l => l.Plan.Role == SlotRole.Challenge);
        int milestones = picks.Count(l => l.Plan.Role == SlotRole.Milestone);
        int hit = picks.Count(l => CampaignSpec.LabelRank(l.Chosen!.MeasuredLabel) >= CampaignSpec.LabelRank(l.Plan.TargetLabel));
        List<GeneratedLevel> review = picks.Where(l => Reasons(l, stats).Review.Count > 0).ToList();
        W();
        W(F($"- Retos e hitos: {picks.Count} ({challenges} retos y {milestones} hitos); {hit} miden la etiqueta objetivo o más (D-009) y {picks.Count - hit} no."));
        W(F($"- Etiquetas publicadas en {first}-{last}: {levels.Count(l => l.Chosen!.MeasuredLabel == "hard")} Difícil y {levels.Count(l => l.Chosen!.MeasuredLabel == "very_hard")} Muy difícil de {levels.Count} niveles (03 §11: 25 % y 5 %)."));
        W(review.Count == 0
            ? "- Para revisar: ninguno; la propuesta es aceptar los retos e hitos tal como salen."
            : "- Para revisar: " + string.Join(", ", review.Select(l => F($"{l.Plan.Level} ({l.Plan.RoleName})"))) + ". El resto, aceptar.");
        List<GeneratedLevel> debuts = levels.Where(l => l.Plan.DebutBoard).ToList();
        if (debuts.Count > 0)
        {
            W("- Estrenos: " + string.Join("; ", debuts.GroupBy(l => l.Plan.Cell.Literal).Select(g => F($"{g.Key} en {string.Join(", ", g.Select(l => l.Plan.Level))}")))
              + F($" ({debuts.Count(l => CampaignReport.DebutOk(l.Chosen!))} de {debuts.Count} con un ejemplar, 2 vacíos y bot_fail < 0,20)."));
        }

        // Retos e hitos.
        W();
        W("## Retos e hitos");
        W();
        W("Etiqueta = la que mide el bot ×40 y se publica (D-009); Objetivo = la de la celda (D) o (MD). q = cuantil de D en el pool del rango "
          + "(banda: recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95). «Otros del slot» = candidatos medidos del mismo slot, "
          + "sin rechazo, que miden la etiqueta objetivo o más: con ellos se puede sustituir el nivel al regenerar (03 §7), si la escalera del "
          + "ciclo lo admite.");
        W();
        W("| Nivel | p | Rol | Calendario | K | Cuias | Vacíos (libres) | Par | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Otros del slot | Propuesta [P] |");
        W("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (GeneratedLevel l in picks)
        {
            Candidate c = l.Chosen!;
            LevelPlan p = l.Plan;
            int band = Pool.BandIndex(c.Quantile);
            W(F($"| {p.Level} | {p.Position} | {p.RoleName} | {p.Cell.Literal} | {c.K} | {c.Board.Caps.Length} | {c.Empties} ({c.FreeEmpties}) | {c.Par} | {c.Bot:0.000} | {p.Window} | {LabelName(c.MeasuredLabel)} | {LabelName(p.TargetLabel)} | {c.D:0.00} | {c.Quantile:0.000} ({BandText(band)}) | {Alternatives(l)} | {Proposal(l, stats)} |"));
        }

        // Hitos, uno a uno.
        W();
        W("## Hitos");
        W();
        W("p20 de cada ciclo («Pico» de c.3): Muy difícil por el bot (≥ 0,90; D-009) y, por D, en banda de hito (q > 0,95; 03 §7) o, como "
          + "mínimo, con par − 2K ≥ la media del pool del rango (criterio [P] del generador y de level-qa, revisión del 2026-10-09; README "
          + "del generador; QA-BAND-Q). Los nombres de hito de la celda son [P] y pasan por el asesor (D-023 punto 21): no van "
          + "al pack.");
        foreach (GeneratedLevel l in picks.Where(l => l.Plan.Role == SlotRole.Milestone))
        {
            Candidate c = l.Chosen!;
            LevelPlan p = l.Plan;
            int band = Pool.BandIndex(c.Quantile);
            double mean = stats[p.Range].ParExcess.Mean;
            W();
            W(F($"### {p.Level} · {p.Cell.Literal}"));
            W();
            W(F($"- Tablero: K {c.K}, {c.Board.Caps.Length} cuias ({CapsText(c.Board)}), {c.Empties} vacío(s) ({c.FreeEmpties} libre(s)); fuente `{c.Source}`."));
            W("- Mecánicas: " + MechanicsText(c) + ".");
            W(F($"- Medida: par {c.Par} (2K = {2 * c.K}; par − 2K = {c.Par - (2 * c.K)}, media del pool {mean:0.0}), bot_fail {c.Bot:0.000} → {LabelName(c.MeasuredLabel)} (objetivo {LabelName(p.TargetLabel)}), D {c.D:0.00}, q {c.Quantile:0.000} (banda {BandText(band)}), ramificación {c.Branching:0.00}, trampas tempranas {c.EarlyTraps}."));
            List<string> notes = Notes(l);
            if (notes.Count > 0)
            {
                W("- Notas de la generación: " + string.Join("; ", notes) + ".");
            }

            W("- Propuesta [P]: " + Proposal(l, stats) + ".");
        }

        // Estrenos.
        if (debuts.Count > 0)
        {
            W();
            W("## Tableros de estreno");
            W();
            W("02 §9.2 regla 2: el estreno va en 2-3 tableros fáciles (p3-p5) con la mecánica sola, un ejemplar, 2 vacíos y `bot_fail` < 0,20; el "
              + "primero lleva `intro_mechanic` (`skippable` = false).");
            W();
            W("| Nivel | p | Calendario | `intro_mechanic` | Mecánica | Ejemplares | Vacíos | Par | bot_fail | D | q (banda) | Regla 2 |");
            W("|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (GeneratedLevel l in debuts)
            {
                Candidate c = l.Chosen!;
                BoardData b = c.Board;
                string attrs = string.Join("; ", Enumerable.Range(0, b.Attrs.Length)
                    .Where(i => !b.Attrs[i].IsNone)
                    .Select(i => MechanicCodes.CodeOf(b.Attrs[i]) is string code && MechanicCodes.Variants.Contains(code)
                        ? CampaignReport.VariantAttr(b.Attrs[i], b.Cuias[i].Length)
                        : AttrText(b.Attrs[i])));
                string intro = l.Plan.Intro is null ? "—" : "`" + l.Plan.Intro + "`";
                W(F($"| {l.Plan.Level} | {l.Plan.Position} | {l.Plan.Cell.Literal} | {intro} | {(attrs.Length == 0 ? "—" : attrs)} | {CampaignReport.Instances(b)} | {CampaignReport.Empties(b)} | {c.Par} | {c.Bot:0.000} | {c.D:0.00} | {c.Quantile:0.000} ({BandText(Pool.BandIndex(c.Quantile))}) | {(CampaignReport.DebutOk(c) ? "cumple" : "**no cumple**")} |"));
            }
        }

        // Variantes de la 1.0 (la misma sección que el informe de la generación).
        if (levels.Any(l => l.Plan.Mechanics.Any(m => MechanicCodes.Variants.Contains(m.Code))))
        {
            CampaignReport.VariantsSection(spec, levels, W, cell => cell.Literal);
        }

        // Tinaja (P7b): dónde va y su respaldo de la salida B, y los filtros de cada nivel con tinaja (la sección del informe de
        // la generación). En la variante b, las celdas que en la a llevan la tinaja.
        if (levels.Any(l => l.Plan.Cell.Has("Ti") || spec.FallbackCells.ContainsKey(l.Plan.Level)))
        {
            TinajaPlacement(spec, levels, W);
            if (levels.Any(l => l.Chosen!.IsTinaja))
            {
                CampaignReport.TinajaSection(levels, W);
            }
        }

        // Otros niveles con notas.
        List<GeneratedLevel> others = levels.Where(l => l.Plan.Role is not (SlotRole.Challenge or SlotRole.Milestone) && Flags(l).Count > 0).ToList();
        W();
        W("## Otros niveles con avisos");
        W();
        if (others.Count == 0)
        {
            W("Ningún nivel fuera de los retos y los hitos queda fuera de su ventana, sin turbias por T1 ni con `risk_adjacent`.");
        }
        else
        {
            W("| Nivel | Rol | Calendario | Aviso |");
            W("|---|---|---|---|");
            foreach (GeneratedLevel l in others)
            {
                W(F($"| {l.Plan.Level} | {RoleText(l.Plan)} | {l.Plan.Cell.Literal} | {string.Join("; ", Flags(l))} |"));
            }
        }

        // Decisión del dueño.
        W();
        W("## Curación del dueño");
        W();
        List<GeneratedLevel> rows = Rows(levels);
        W("Estado: " + StateText(rows, decisions) + " (09 §0.1 y §0.3). La decisión por nivel es «aceptar», «sustituir» (otro candidato del slot o del pool, 03 §7) o "
            + "«cambiar la celda», con el motivo en los dos últimos casos. No se escribe en este informe, que es generado: va en "
            + (decisionsPath is null ? "el archivo de `--decisions` (formato [P] `" + CurationDecisions.Format + "`)" : "`" + decisionsPath + "`")
            + ", una entrada por nivel con el literal de la celda que vio el dueño, y la última columna la copia de ahí; si la celda cambia, "
            + "la decisión deja de valer y el nivel vuelve a «pendiente». Si la curación pide cambios, se cambian la especificación o el "
            + "generador y se regeneran el pack, este informe y la comprobación de `level-qa`.");
        W();
        W("| Nivel | Rol | Calendario | Etiqueta / objetivo | Propuesta [P] | Decisión del dueño |");
        W("|---|---|---|---|---|---|");
        foreach (GeneratedLevel l in rows)
        {
            bool pick = l.Plan.Role is SlotRole.Challenge or SlotRole.Milestone;
            string role = pick ? RoleText(l.Plan) : "estreno";
            string target = pick ? LabelName(l.Plan.TargetLabel) : "—";
            string proposal = pick ? ShortProposal(l, stats) : CampaignReport.DebutOk(l.Chosen!) ? "aceptar" : "revisar";
            W(F($"| {l.Plan.Level} | {role} | {l.Plan.Cell.Literal} | {LabelName(l.Chosen!.MeasuredLabel)} / {target} | {proposal} | {DecisionText(l, decisions)} |"));
        }

        return sb.ToString();
    }

    /// <summary>Apariciones como mucho de la tinaja en su ciclo de estreno, 321-340 (propuesta de la tinaja d.3, «C0»; D-019).</summary>
    public const int TinajaDebutCycleMax = 8;

    /// <summary>
    /// Dónde va la tinaja (P7b; D-019 Q-Ti1 A y Q-Ti3 A; propuesta de la tinaja d.3; tabla de D2, d.5.3 y d.5.4): cada nivel de
    /// los generados cuya celda la lleva en la variante «a», con su respaldo de la «b» y lo que se publica en este pack; las
    /// apariciones por ciclo (como mucho <see cref="TinajaDebutCycleMax"/> en el de su estreno) y las tres comprobaciones del
    /// encargo: tinaja solo donde la pone el calendario de D2, nunca en p1, p2 ni p19, y la variante b sin ninguna.
    /// </summary>
    private static void TinajaPlacement(CampaignSpec spec, List<GeneratedLevel> levels, Action<string> w)
    {
        void W(string line = "") => w(line);
        string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
        CampaignSpec a = spec.Variant == CampaignSpec.VariantA ? spec : CampaignSpec.Load(spec.Path, CampaignSpec.VariantA);
        List<GeneratedLevel> cells = levels.Where(l => a.Calendar.TryGetValue(l.Plan.Level, out CalendarCell? c) && c.Has("Ti")).ToList();
        W();
        W(F($"## Dónde va la Tinaja (variante {spec.Variant})"));
        W();
        W("D-019 (Q-Ti1 A: nunca en recuperación, p1-p2, ni en remanso, p19; Q-Ti3 A: estreno en el 323, en la 1.0 si pasa sus pruebas "
          + "y si Morpho con corte por tiempo y la Luciérnaga están listos en el cliente; si no, la salida B), la tabla de D2 (d.5.3, "
          + "con el respaldo de cada celda en d.5.4; D-023 punto 10) y la propuesta de la tinaja d.3 (ciclo de estreno 321-340, «C0»: "
          + F($"como mucho {TinajaDebutCycleMax} apariciones). La variante a es la del dueño; la b, la salida B, se guarda aparte. ")
          + F($"Las 5 dosis de los retos y el hito dependen de la compuerta de coste de Morpho, con el factor {TinajaWalks.MorphoDeviceFactor}× [P] ")
          + "del teléfono hasta medirlo en PF1 (pregunta 13): si PF1 lo cambia, este pack se regenera antes de integrarlo en el cliente (C1b).");
        W();
        W("| Nivel | p | Rol | Celda en la a | Respaldo en la b | Publicado en este pack | Dosis (ocultas) | K | Cuias |");
        W("|---|---|---|---|---|---|---|---|---|");
        foreach (GeneratedLevel l in cells)
        {
            Candidate c = l.Chosen!;
            string fallback = spec.FallbackCells.TryGetValue(l.Plan.Level, out CalendarCell? fb) ? fb.Literal : "—";
            string doses = c.TinajaCheck is TinajaCheckResult t ? F($"{t.Doses} ({t.Doses - 1})") : "—";
            string published = c.IsTinaja ? "con la Tinaja" : "sin la Tinaja";
            W(F($"| {l.Plan.Level} | {l.Plan.Position} | {RoleText(l.Plan)} | {a.Calendar[l.Plan.Level].Literal} | {fallback} | {l.Plan.Cell.Literal}: {published} | {doses} | {c.K} | {c.Board.Caps.Length} |"));
        }

        List<string> checks = TinajaChecks(spec, a, levels.Select(l => (l.Plan, l.Chosen!.IsTinaja)).ToList());
        W();
        W(checks[0]);
        W();
        foreach (string bullet in checks.Skip(1))
        {
            W(bullet);
        }
    }

    /// <summary>
    /// Las líneas de comprobación de «Dónde va la Tinaja» (P7b; lo prueba --self-test curation con niveles sintéticos): las
    /// apariciones por ciclo con el calendario de la a, y las viñetas «solo donde la pone el calendario» (la celda publicada,
    /// <c>Plan.Cell</c>, de la variante que se genera), «nunca en p1, p2 ni p19» (D-019 Q-Ti1 A), el tope de
    /// <see cref="TinajaDebutCycleMax"/> en el ciclo de estreno (si su primer nivel se generó) y, fuera de la a, «ninguna». El
    /// ciclo de estreno sale del plan del nivel de estreno (<c>Level - Position + 1</c>), no de una aritmética de ciclos.
    /// La primera línea es la de las apariciones; las siguientes, las viñetas (una por línea, en un solo párrafo).
    /// </summary>
    internal static List<string> TinajaChecks(CampaignSpec spec, CampaignSpec a, List<(LevelPlan Plan, bool Tinaja)> levels)
    {
        string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
        string Count(int n) => F($"{n} {(n == 1 ? "nivel" : "niveles")}");
        string Levels(IEnumerable<(LevelPlan Plan, bool Tinaja)> ls) => string.Join(", ", ls.Select(l => l.Plan.Level.ToString(CultureInfo.InvariantCulture)));
        var cells = levels.Where(l => a.Calendar.TryGetValue(l.Plan.Level, out CalendarCell? c) && c.Has("Ti")).ToList();
        var byCycle = cells.GroupBy(l => l.Plan.Level - l.Plan.Position + 1).OrderBy(g => g.Key).ToList();
        string appearances = "Apariciones por ciclo (calendario de la a): " + (byCycle.Count == 0 ? "ninguna" : string.Join("; ", byCycle.Select(g =>
            F($"{g.Key}-{g.Key + 19}: {g.Count()} ({Levels(g)})")))) + ".";
        var withTinaja = levels.Where(l => l.Tinaja).ToList();
        var outside = withTinaja.Where(l => !l.Plan.Cell.Has("Ti")).ToList();
        var pure = withTinaja.Where(l => l.Plan.Position is 1 or 2 or 19).ToList();
        var bullets = new List<string>
        {
            "- Tinaja solo donde la pone el calendario: " + (outside.Count == 0 ? "sí." : "**no**: " + Levels(outside) + "."),
            "- Nunca en p1, p2 ni p19: " + (pure.Count == 0 ? "ninguno." : "**" + Levels(pure) + "**."),
        };

        if (a.MechanicTable.FirstOrDefault(m => m.Code == "Ti") is MechanicInfo ti && ti.Debut >= a.FirstLevel && ti.Debut <= a.LastLevel
            && a.Plan(ti.Debut) is LevelPlan debut && !debut.Ramp)
        {
            int debutCycle = debut.Level - debut.Position + 1;
            if (levels.Any(l => l.Plan.Level == debutCycle))
            {
                int inDebutCycle = withTinaja.Count(l => l.Plan.Level >= debutCycle && l.Plan.Level < debutCycle + 20);
                bullets.Add(F($"- Ciclo de estreno {debutCycle}-{debutCycle + 19}: {inDebutCycle} {(inDebutCycle == 1 ? "nivel publicado" : "niveles publicados")} con la Tinaja (como mucho {TinajaDebutCycleMax})")
                  + (inDebutCycle > TinajaDebutCycleMax ? ": **pasa del tope**." : "."));
            }
        }

        if (spec.Variant != CampaignSpec.VariantA)
        {
            bullets.Add(F($"- Variante {spec.Variant}: {Count(withTinaja.Count)} con la Tinaja") + (withTinaja.Count == 0 ? " (la salida B no la lleva)." : ": **debería ser 0**."));
        }

        return [appearances, .. bullets];
    }

    /// <summary>Filas de la tabla del dueño: retos e hitos y, después, el primer tablero de cada estreno (el que lleva intro_mechanic).</summary>
    public static List<GeneratedLevel> Rows(List<GeneratedLevel> levels) =>
        levels.Where(l => l.Plan.Role is SlotRole.Challenge or SlotRole.Milestone)
            .Concat(levels.Where(l => l.Plan.DebutBoard && l.Plan.Intro is not null))
            .ToList();

    /// <summary>
    /// Decisiones de <paramref name="decisions"/> para niveles de <paramref name="levels"/> que no tienen fila en la tabla del dueño
    /// (errores) y decisiones tomadas para otra celda (avisos: el nivel vuelve a «pendiente»). Las de la especificación fuera de
    /// los niveles generados se ignoran (las de fuera de la especificación ya las rechaza CurationDecisions.Load). Mensajes en
    /// ASCII para la consola.
    /// </summary>
    public static (List<string> Errors, List<string> Stale) Check(CurationDecisions decisions, List<GeneratedLevel> levels)
    {
        var errors = new List<string>();
        var stale = new List<string>();
        Dictionary<int, GeneratedLevel> rows = Rows(levels).ToDictionary(l => l.Plan.Level);
        int first = levels.Min(l => l.Plan.Level);
        int last = levels.Max(l => l.Plan.Level);
        foreach (CurationDecision d in decisions.ByLevel.Values.OrderBy(d => d.Level))
        {
            if (d.Level < first || d.Level > last)
            {
                continue;
            }

            if (!rows.TryGetValue(d.Level, out GeneratedLevel? l))
            {
                errors.Add(string.Create(CultureInfo.InvariantCulture, $"decision del nivel {d.Level}, que no es reto, hito ni primer tablero de estreno"));
            }
            else if (d.Cell != l.Plan.Cell.Literal)
            {
                stale.Add(string.Create(CultureInfo.InvariantCulture, $"decision del nivel {d.Level} ({d.Date}) para la celda '{d.Cell}', que ahora es '{l.Plan.Cell.Literal}': vuelve a pendiente"));
            }
        }

        return (errors, stale);
    }

    /// <summary>
    /// Estado de la curación del paquete sobre las filas de la tabla del dueño (<see cref="Rows"/>): «pendiente (n de m niveles
    /// decididos)» mientras falte alguna decisión vigente, «aceptada» si todas aceptan y, si no, «anotada, con cambios pedidos en …».
    /// </summary>
    internal static string StateText(List<GeneratedLevel> rows, CurationDecisions? decisions)
    {
        List<GeneratedLevel> decided = rows.Where(l => Decision(l, decisions) is not null).ToList();
        List<GeneratedLevel> changes = decided.Where(l => Decision(l, decisions)!.Decision != CurationDecisions.Accept).ToList();
        return decided.Count < rows.Count
            ? string.Create(CultureInfo.InvariantCulture, $"**{Pending}** ({decided.Count} de {rows.Count} niveles decididos)")
            : changes.Count == 0
                ? string.Create(CultureInfo.InvariantCulture, $"**aceptada** ({rows.Count} de {rows.Count} niveles)")
                : "**anotada, con cambios pedidos** en " + string.Join(", ", changes.Select(l => l.Plan.Level.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Decisión vigente del nivel: la del archivo, si existe y es para la celda actual.</summary>
    private static CurationDecision? Decision(GeneratedLevel l, CurationDecisions? decisions) =>
        decisions is not null && decisions.ByLevel.TryGetValue(l.Plan.Level, out CurationDecision? d) && d.Cell == l.Plan.Cell.Literal ? d : null;

    /// <summary>Última columna de la tabla del dueño: la decisión vigente del nivel o «pendiente» (con la celda de la caducada).</summary>
    internal static string DecisionText(GeneratedLevel l, CurationDecisions? decisions)
    {
        if (Decision(l, decisions) is CurationDecision d)
        {
            return d.Decision + " (" + d.Date + ")" + (d.Note is null ? "" : ": " + MdCell(d.Note));
        }

        return decisions is not null && decisions.ByLevel.TryGetValue(l.Plan.Level, out CurationDecision? old)
            ? Pending + " (la decisión del " + old.Date + " era para «" + MdCell(old.Cell) + "»)"
            : Pending;
    }

    /// <summary>Texto libre dentro de una celda de tabla Markdown: sin saltos de línea y con «|» escapada.</summary>
    private static string MdCell(string text) => text.Replace("\r", "").Replace('\n', ' ').Replace("|", "\\|");

    /// <summary>Banda de D con tilde (el informe es UTF-8; Pool.BandName es ASCII para la consola y level-qa).</summary>
    private static string BandText(int band) => band == 0 ? "recuperación" : Pool.BandName(band);

    /// <summary>Rol con tilde (LevelPlan.RoleName es ASCII).</summary>
    private static string RoleText(LevelPlan p) => p.RoleName == "recuperacion" ? "recuperación" : p.RoleName;

    /// <summary>Motivos de la propuesta: los que piden revisar y los que solo se anotan.</summary>
    private static (List<string> Review, List<string> Info) Reasons(GeneratedLevel l, Dictionary<RangeInfo, RangeStats> stats)
    {
        string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
        Candidate c = l.Chosen!;
        LevelPlan p = l.Plan;
        var review = new List<string>();
        var info = new List<string>();
        if (CampaignSpec.LabelRank(c.MeasuredLabel) < CampaignSpec.LabelRank(p.TargetLabel))
        {
            review.Add(F($"mide {LabelName(c.MeasuredLabel)} (bot_fail {c.Bot:0.000}) y apunta a {LabelName(p.TargetLabel)}: se publica con la etiqueta que mide (c.2; 03 §7); aceptar así o sustituir"));
            if (Alternatives(l) == "0")
            {
                review.Add("ningún otro candidato del slot mide la etiqueta objetivo: sustituirlo pide más candidatos (otra ronda del generador) o cambiar la celda");
            }
        }
        else if (!c.InWindow)
        {
            review.Add(F($"bot_fail {c.Bot:0.000} fuera de la ventana {p.Window} del slot"));
        }

        if (c.Tints.RiskAdjacent > 0)
        {
            review.Add(F($"risk_adjacent: {c.Tints.RiskAdjacent} contacto(s) de tintes de un par de riesgo en una cuia (03 §4.5)"));
        }

        if (l.Slot.MurkyDropped)
        {
            review.Add("publicado sin Aguas turbias (T1): ningún candidato turbio entró en la ventana");
        }

        int band = Pool.BandIndex(c.Quantile);
        if (p.Role == SlotRole.Milestone && band < 3 && CampaignSpec.LabelRank(c.MeasuredLabel) >= CampaignSpec.LabelRank(p.TargetLabel))
        {
            info.Add(F($"D en banda {BandText(band)} (q {c.Quantile:0.000}), con el mínimo del hito cumplido: par − 2K = {c.Par - (2 * c.K)} ≥ {stats[p.Range].ParExcess.Mean:0.0} (aviso QA-BAND-Q)"));
        }
        else if (p.Role == SlotRole.Challenge && band < 2 && CampaignSpec.LabelRank(c.MeasuredLabel) >= CampaignSpec.LabelRank(p.TargetLabel))
        {
            info.Add(F($"Difícil por el bot, con D en banda {BandText(band)} (q {c.Quantile:0.000})"));
        }

        if (c.Murky)
        {
            info.Add(F($"turbia: {c.Board.Hidden.Length} dosis ocultas en {c.Board.Hidden.Select(h => h[0]).Distinct().Count()} cuia(s), {c.MaxRealities} realidad(es) compatibles con lo visible"));
        }

        return (review, info);
    }

    private static string Proposal(GeneratedLevel l, Dictionary<RangeInfo, RangeStats> stats)
    {
        (List<string> review, List<string> info) = Reasons(l, stats);
        string head = review.Count > 0 ? "**revisar**: " + string.Join("; ", review) : "aceptar";
        return info.Count == 0 ? head : head + " (" + string.Join("; ", info) + ")";
    }

    private static string ShortProposal(GeneratedLevel l, Dictionary<RangeInfo, RangeStats> stats) =>
        Reasons(l, stats).Review.Count > 0 ? "revisar" : "aceptar";

    /// <summary>Candidatos medidos del slot, sin rechazo y distintos del elegido, con la etiqueta objetivo o más.</summary>
    private static string Alternatives(GeneratedLevel l)
    {
        int rank = CampaignSpec.LabelRank(l.Plan.TargetLabel);
        int n = l.Slot.Candidates.Count(c => !ReferenceEquals(c, l.Chosen) && c.Measured && c.Reject is null && CampaignSpec.LabelRank(c.MeasuredLabel) >= rank);
        return n.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Notas de la generación del nivel elegido y de su slot (como la columna «Notas» del informe de la generación).</summary>
    private static List<string> Notes(GeneratedLevel l)
    {
        var notes = new List<string>(l.Chosen!.Config.Notes);
        notes.AddRange(l.Notes);
        if (l.Rounds > 0)
        {
            notes.Add(string.Create(CultureInfo.InvariantCulture, $"{l.Rounds} ronda(s) extra de candidatos"));
        }

        if (l.BandRounds > 0)
        {
            notes.Add(string.Create(CultureInfo.InvariantCulture, $"{l.BandRounds} ronda(s) de banda"));
        }

        return notes;
    }

    /// <summary>Avisos de un nivel que no es reto ni hito: fuera de ventana, sin turbias (T1), risk_adjacent o sin bajar de banda.</summary>
    private static List<string> Flags(GeneratedLevel l)
    {
        var flags = new List<string>();
        Candidate c = l.Chosen!;
        if (!c.InWindow)
        {
            flags.Add(string.Create(CultureInfo.InvariantCulture, $"bot_fail {c.Bot:0.000} fuera de la ventana {l.Plan.Window}"));
        }

        if (l.Slot.MurkyDropped)
        {
            flags.Add("sin Aguas turbias (T1)");
        }

        if (c.Tints.RiskAdjacent > 0)
        {
            flags.Add(string.Create(CultureInfo.InvariantCulture, $"risk_adjacent: {c.Tints.RiskAdjacent} contacto(s)"));
        }

        flags.AddRange(l.Notes);
        return flags;
    }

    private static string LabelName(string? label) => label switch
    {
        "hard" => "Difícil",
        "very_hard" => "Muy difícil",
        null => "—",
        _ => "Normal",
    };

    /// <summary>Capacidades del tablero agrupadas, de mayor a menor: «11 de 4, 1 de 2».</summary>
    private static string CapsText(BoardData b) =>
        string.Join(", ", b.Caps.GroupBy(c => c).OrderByDescending(g => g.Key)
            .Select(g => string.Create(CultureInfo.InvariantCulture, $"{g.Count()} de {g.Key}")));

    /// <summary>Mecánicas del tablero publicado: cada attr, el Barro (dosis) y las Aguas turbias (ocultas fuera de la tinaja).</summary>
    private static string MechanicsText(Candidate c)
    {
        BoardData b = c.Board;
        var parts = new List<string>();
        parts.AddRange(Enumerable.Range(0, b.Attrs.Length).Where(i => !b.Attrs[i].IsNone)
            .Select(i => MechanicCodes.CodeOf(b.Attrs[i]) is string code && MechanicCodes.Variants.Contains(code)
                ? CampaignReport.VariantAttr(b.Attrs[i], b.Cuias[i].Length)
                : AttrText(b.Attrs[i]) + string.Create(CultureInfo.InvariantCulture, $" (cuia de {b.Caps[i]})")));
        int mud = b.Cuias.Sum(x => x.Count(d => d == Cuia.MudColor));
        if (mud > 0)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"Barro: {mud} dosis"));
        }

        int[][] murky = b.Hidden.Where(h => b.Attrs[h[0]].Type != MechanicType.Tinaja).ToArray();
        if (murky.Length > 0)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture,
                $"Aguas turbias: {murky.Length} dosis ocultas en {murky.Select(h => h[0]).Distinct().Count()} cuia(s), {c.MaxRealities} realidad(es) compatibles con lo visible"));
        }

        return parts.Count == 0 ? "ninguna" : string.Join("; ", parts);
    }

    /// <summary>Forma corta de un attr que no es variante: «Ho key=4», «Li key=8», «Pi», «Re» (el Brote toma el color de su cima).</summary>
    private static string AttrText(MechanicSpec a)
    {
        string code = MechanicCodes.CodeOf(a) ?? "—";
        return a.Type switch
        {
            MechanicType.Hoja or MechanicType.Liana => string.Create(CultureInfo.InvariantCulture, $"{code} key={a.Key}"),
            _ => code,
        };
    }
}
