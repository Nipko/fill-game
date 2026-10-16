// Informe de curación de un pack (P7a de la tanda 4; tools/out/curation_wNN.md): la propuesta de curación de los retos y los
// hitos que aprueba el dueño (09 §2.1, fila de level-pipeline-dev: «el dueño aprueba la curación de retos e hitos de cada
// paquete»; 09 §2.2: «propuesta de curación de los 30 retos e hitos que aprueba el dueño»; 03 §3: «un nivel en slot de reto
// cuyo bot_fail quede por debajo de 0,75 se publica sin etiqueta y la curación decide si se sustituye por otro del pool»).
// Lo escribe --spec con --curation, con los mismos niveles colocados que el pack: retos e hitos con su etiqueta medida por
// el bot ×40 frente a la objetivo (D-009), los hitos uno a uno, los tableros de estreno (02 §9.2 regla 2), las variantes de
// la 1.0 (la sección de CampaignReport) y la tabla de la decisión del dueño, que nace «pendiente».
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
    /// <summary>Decisión del dueño en un informe recién generado (09 §2.1).</summary>
    public const string Pending = "pendiente";

    public static string Build(
        CampaignSpec spec, List<GeneratedLevel> levels, Dictionary<RangeInfo, RangeStats> stats,
        List<(WorldInfo World, string Path, byte[] Bytes, int Levels)> packs, string levelsArg, string curationPath)
    {
        var sb = new StringBuilder();
        void W(string line = "") => sb.Append(line).Append('\n');
        string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);

        string specPath = spec.Path.Replace('\\', '/');
        string outDir = packs.Count == 0 ? "." : (Path.GetDirectoryName(packs[0].Path) ?? ".").Replace('\\', '/');
        string variantArg = spec.HasVariants ? F($" --variant {spec.Variant}") : "";
        string variantTitle = spec.HasVariants ? F($" (variante {spec.Variant})") : "";
        int first = levels.Min(l => l.Plan.Level);
        int last = levels.Max(l => l.Plan.Level);
        string worlds = string.Join(", ", levels.Select(l => l.Plan.World).Distinct().Select(w => F($"mundo {w.World} ({w.Name}, {w.First}-{w.Last})")));
        List<GeneratedLevel> picks = levels.Where(l => l.Plan.Role is SlotRole.Challenge or SlotRole.Milestone).ToList();

        W(F($"# Curación de {first}-{last}{variantTitle}: retos, hitos y estrenos para el dueño"));
        W();
        W(F($"Generado por `dotnet run --project tools/level-generator -c Release -- --spec {specPath} --levels {levelsArg}{variantArg} --out {outDir} --curation {curationPath.Replace('\\', '/')}` ")
            + "con los mismos niveles colocados que el pack: no se edita a mano (salvo la columna del dueño, abajo) y no lleva tiempos. "
            + "Niveles del " + worlds + ".");
        W();
        W("Fuentes: 09 §2.1 y §2.2 (el dueño aprueba la curación de los retos e hitos de cada paquete; propuesta de curación de los 30 "
          + "retos e hitos), 03 §3 (un reto por debajo de 0,75 se publica sin etiqueta y la curación decide si se sustituye por otro del "
          + "pool del slot), D-009 (etiqueta por el bot ×40 con la semilla del nivel: Difícil ≥ 0,75 y Muy difícil ≥ 0,90; retos en p6, "
          + "p9, p12, p15 y p18 e hito en p20; c.2: un reto o el hito fuera de su ventana se publica con la etiqueta que mide), 03 §7 "
          + "(banda por el cuantil q de D en el pool del rango), 03 §11-§12, 02 §9.2 regla 2 (estrenos) y la tabla de novedades de D2 "
          + "(d.5.3; D-023 punto 10). La columna «Propuesta» es [P]: la decide el dueño.");
        W();
        W("Compuerta automática (verde antes de pedir la curación): `dotnet run --project tools/level-qa -c Release -- "
          + string.Join(" ", packs.Select(p => p.Path.Replace('\\', '/')))
          + F($" --spec {specPath} --levels {levelsArg}`. Sus avisos QA-WINDOW (fuera de la ventana del slot) y QA-BAND-Q (fuera de la banda del rol) son los niveles con «revisar» o con nota de banda de este informe."));
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
            W("- Estrenos: " + string.Join("; ", debuts.GroupBy(l => l.Plan.Cell.Short).Select(g => F($"{g.Key} en {string.Join(", ", g.Select(l => l.Plan.Level))}")))
              + F($" ({debuts.Count(l => CampaignReport.DebutOk(l.Chosen!))} de {debuts.Count} con un ejemplar, 2 vacíos y bot_fail < 0,20)."));
        }

        // Retos e hitos.
        W();
        W("## Retos e hitos");
        W();
        W("Etiqueta = la que mide el bot ×40 y se publica (D-009); Objetivo = la de la celda (D) o (MD). q = cuantil de D en el pool del rango "
          + "(banda: recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95). «Otros del slot» = candidatos medidos del mismo slot, "
          + "sin rechazo, que miden la etiqueta objetivo o más: con ellos se puede sustituir el nivel al regenerar (03 §3), si la escalera del "
          + "ciclo lo admite.");
        W();
        W("| Nivel | p | Rol | Calendario | K | Cuias | Vacíos (libres) | Par | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Otros del slot | Propuesta [P] |");
        W("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (GeneratedLevel l in picks)
        {
            Candidate c = l.Chosen!;
            LevelPlan p = l.Plan;
            int band = Pool.BandIndex(c.Quantile);
            W(F($"| {p.Level} | {p.Position} | {p.RoleName} | {p.Cell.Literal} | {c.K} | {c.Board.Caps.Length} | {c.Empties} ({c.FreeEmpties}) | {c.Par} | {c.Bot:0.000} | {p.Window} | {LabelName(c.MeasuredLabel)} | {LabelName(p.TargetLabel)} | {c.D:0.00} | {c.Quantile:0.000} ({Pool.BandName(band)}) | {Alternatives(l)} | {Proposal(l, stats)} |"));
        }

        // Hitos, uno a uno.
        W();
        W("## Hitos");
        W();
        W("p20 de cada ciclo («Pico» de c.3): Muy difícil por el bot (≥ 0,90) y, por D, en banda de hito o, como mínimo, con par − 2K ≥ la "
          + "media del pool del rango (03 §7; QA-BAND-Q). Los nombres de hito de la celda son [P] y pasan por el asesor (D-023 punto 21): no van "
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
            W(F($"- Medida: par {c.Par} (2K = {2 * c.K}; par − 2K = {c.Par - (2 * c.K)}, media del pool {mean:0.0}), bot_fail {c.Bot:0.000} → {LabelName(c.MeasuredLabel)} (objetivo {LabelName(p.TargetLabel)}), D {c.D:0.00}, q {c.Quantile:0.000} (banda {Pool.BandName(band)}), ramificación {c.Branching:0.00}, trampas tempranas {c.EarlyTraps}."));
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
                W(F($"| {l.Plan.Level} | {l.Plan.Position} | {l.Plan.Cell.Literal} | {intro} | {(attrs.Length == 0 ? "—" : attrs)} | {CampaignReport.Instances(b)} | {CampaignReport.Empties(b)} | {c.Par} | {c.Bot:0.000} | {c.D:0.00} | {c.Quantile:0.000} ({Pool.BandName(Pool.BandIndex(c.Quantile))}) | {(CampaignReport.DebutOk(c) ? "cumple" : "**no cumple**")} |"));
            }
        }

        // Variantes de la 1.0 (la misma sección que el informe de la generación).
        if (levels.Any(l => l.Plan.Mechanics.Any(m => MechanicCodes.Variants.Contains(m.Code))))
        {
            CampaignReport.VariantsSection(spec, levels, W);
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
                W(F($"| {l.Plan.Level} | {l.Plan.RoleName} | {l.Plan.Cell.Literal} | {string.Join("; ", Flags(l))} |"));
            }
        }

        // Decisión del dueño.
        W();
        W("## Curación del dueño");
        W();
        W("Estado: **" + Pending + "** (09 §2.1). El dueño anota en la última columna su decisión por nivel: «aceptar», «sustituir» (otro candidato "
            + "del slot o del pool, 03 §3) o «cambiar la celda» (con el motivo). Esta columna es la única que se edita a mano y la decisión se "
            + "repite en el PR, porque regenerar el informe la devuelve a «pendiente». Si la curación pide cambios, se cambian la especificación o "
            + "el generador y se regeneran el pack, este informe y la comprobación de `level-qa`.");
        W();
        W("| Nivel | Rol | Calendario | Etiqueta / objetivo | Propuesta [P] | Decisión del dueño |");
        W("|---|---|---|---|---|---|");
        foreach (GeneratedLevel l in picks)
        {
            W(F($"| {l.Plan.Level} | {l.Plan.RoleName} | {l.Plan.Cell.Literal} | {LabelName(l.Chosen!.MeasuredLabel)} / {LabelName(l.Plan.TargetLabel)} | {ShortProposal(l, stats)} | {Pending} |"));
        }

        foreach (GeneratedLevel l in debuts.Where(l => l.Plan.Intro is not null))
        {
            W(F($"| {l.Plan.Level} | estreno | {l.Plan.Cell.Literal} | {LabelName(l.Chosen!.MeasuredLabel)} / — | {(CampaignReport.DebutOk(l.Chosen!) ? "aceptar" : "revisar")} | {Pending} |"));
        }

        return sb.ToString();
    }

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
            review.Add(F($"mide {LabelName(c.MeasuredLabel)} (bot_fail {c.Bot:0.000}) y apunta a {LabelName(p.TargetLabel)}: se publica con la etiqueta que mide (c.2; 03 §3); aceptar así o sustituir"));
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
            info.Add(F($"D en banda {Pool.BandName(band)} (q {c.Quantile:0.000}), con el mínimo del hito cumplido: par − 2K = {c.Par - (2 * c.K)} ≥ {stats[p.Range].ParExcess.Mean:0.0} (aviso QA-BAND-Q)"));
        }
        else if (p.Role == SlotRole.Challenge && band < 2 && CampaignSpec.LabelRank(c.MeasuredLabel) >= CampaignSpec.LabelRank(p.TargetLabel))
        {
            info.Add(F($"Difícil por el bot, con D en banda {Pool.BandName(band)} (q {c.Quantile:0.000})"));
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
