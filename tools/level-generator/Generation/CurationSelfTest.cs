// --self-test curation (P7a de la tanda 4): pruebas del archivo de decisiones del dueño (formato [P] af-curation-decisions/1,
// CurationDecisions) y de cómo las usa el informe de curación (CurationReport), sin proyecto de tests (las pruebas de tools/ van
// como fixtures y modos --self-test). Es el mecanismo del que depende «Curación del dueño anotada» (09 §0.1, fila de
// level-pipeline-dev: «El dueño aprueba la curación de retos e hitos de cada paquete»; 09 §0.3): si se rompe, la decisión del
// dueño desaparece o se aplica a otra celda. Usa specs/campaign_v2.json, specs/curation_v2.json y los fixtures
// specs/tests/curation_*.json, sin generar ningún tablero: los niveles 201-300 son sintéticos (el LevelPlan de la especificación
// con un slot vacío), que es todo lo que miran CurationReport.Rows, Check, StateText y DecisionText. Comprueba:
//   - que Load rechaza, con su mensaje, la clave desconocida (raíz y entrada), "format" erróneo, otra "spec", la entrada sin
//     "cell", un "level" que no es entero o es ≤ 0, un nivel fuera de la especificación (200 y 701 en campaign_v2, 201-700),
//     una "decision" fuera de la lista, la fecha inválida, «sustituir» y «cambiar la celda» sin "note" y el nivel repetido; y
//     que --spec con un archivo así sale con código 2 antes de generar;
//   - que el archivo versionado (specs/curation_v2.json) y el fixture válido se leen;
//   - las filas de la tabla del dueño en 201-300: los 30 retos e hitos y el primer tablero de cada estreno (243 y 283);
//   - que Check da error con un nivel del rango que no tiene fila (201, recuperación; 244, segundo tablero del estreno), aviso con
//     una decisión para otra celda (280) y nada con un nivel de la especificación fuera de los generados (450);
//   - los textos de la última columna («aceptar (fecha)», «sustituir (fecha): nota» con «|» escapada y los saltos de línea
//     aplanados, «pendiente» y «pendiente (la decisión del … era para «…»)») y del estado: «pendiente (n de m niveles
//     decididos)», «aceptada (m de m niveles)» y «anotada, con cambios pedidos en …».
//   - «Dónde va la Tinaja» (P7b; CurationReport.TinajaChecks) con 321-340 sintéticos en las dos variantes: en sus 8 celdas
//     («sí», «ninguno», 8 de 8), en p1, p2 o p19 (aviso en negrita), 9 en el ciclo de estreno («pasa del tope») y en la b
//     («la salida B no la lleva» sin ninguna, «debería ser 0» con una); el ciclo de estreno sale del plan del 323;
//   - cuándo sale esa sección (CurationReport.ShowsTinaja: con 321-340 en las dos variantes, no con 201-300 ni 301-320) y las
//     filas de su tabla (CurationReport.TinajaRows): las 8 celdas de la a en orden, y las del 323 y el 340 con la celda de la a,
//     el respaldo de la b (Ma y Br+Ba (MD)), la celda publicada con o sin la Tinaja y las dosis «n (n − 1)» o la raya.
// Imprime ASCII (PASS / FAIL) y sale con 0 si todo pasa, 1 si algo falla y 2 si no encuentra la especificación. Desde la raíz
// del repositorio.
using System.Globalization;
using AmazonFill.Tools.LevelGenerator.Campaign;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal static class CurationSelfTest
{
    public const string CampaignV2 = "tools/level-generator/specs/campaign_v2.json";
    public const string Versioned = "tools/level-generator/specs/curation_v2.json";
    public const string Valid = "tools/level-generator/specs/tests/curation_valid.json";
    public const string NotARow = "tools/level-generator/specs/tests/curation_not_a_row.json";
    public const string BelowSpec = "tools/level-generator/specs/tests/curation_level_below_spec.json";

    /// <summary>Fixtures que Load tiene que rechazar y el motivo que debe decir el mensaje (no otro).</summary>
    public static readonly (string Path, string Reason)[] Rejected =
    [
        ("tools/level-generator/specs/tests/curation_unknown_root_key.json", "clave desconocida 'comment'"),
        ("tools/level-generator/specs/tests/curation_unknown_entry_key.json", "decisions[0]: clave desconocida 'reason'"),
        ("tools/level-generator/specs/tests/curation_bad_format.json", "\"format\" debe ser \"af-curation-decisions/1\""),
        ("tools/level-generator/specs/tests/curation_other_spec.json", "\"spec\" (campaign_v1.json) no es la especificacion que se genera (campaign_v2.json)"),
        ("tools/level-generator/specs/tests/curation_no_cell.json", "decisions[0]: falta \"cell\""),
        ("tools/level-generator/specs/tests/curation_level_string.json", "decisions[0]: \"level\" debe ser un entero positivo"),
        ("tools/level-generator/specs/tests/curation_level_fraction.json", "decisions[0]: \"level\" debe ser un entero positivo"),
        ("tools/level-generator/specs/tests/curation_level_zero.json", "decisions[0]: \"level\" debe ser un entero positivo"),
        (BelowSpec, "decisions[0]: nivel 200 fuera de la especificacion (201-700)"),
        ("tools/level-generator/specs/tests/curation_level_above_spec.json", "decisions[0]: nivel 701 fuera de la especificacion (201-700)"),
        ("tools/level-generator/specs/tests/curation_bad_decision.json", "decisions[0]: \"decision\" debe ser \"aceptar\", \"sustituir\", \"cambiar la celda\""),
        ("tools/level-generator/specs/tests/curation_bad_date.json", "decisions[0]: \"date\" debe ser AAAA-MM-DD"),
        ("tools/level-generator/specs/tests/curation_replace_without_note.json", "decisions[0]: \"sustituir\" lleva \"note\" con el motivo"),
        ("tools/level-generator/specs/tests/curation_change_cell_without_note.json", "decisions[0]: \"cambiar la celda\" lleva \"note\" con el motivo"),
        ("tools/level-generator/specs/tests/curation_repeated_level.json", "decisions[1]: nivel 206 repetido"),
    ];

    /// <summary>Retos (p6, p9, p12, p15 y p18) e hitos (p20) de 201-300 y, después, los primeros tableros de estreno.</summary>
    private static readonly int[] RowsW06 =
    [
        206, 209, 212, 215, 218, 220, 226, 229, 232, 235, 238, 240, 246, 249, 252, 255, 258, 260,
        266, 269, 272, 275, 278, 280, 286, 289, 292, 295, 298, 300, 243, 283,
    ];

    public static int Run()
    {
        int failures = 0;
        void Check(bool ok, string what)
        {
            Console.WriteLine(AsciiText.Of((ok ? "PASS " : "FAIL ") + what));
            failures += ok ? 0 : 1;
        }

        CampaignSpec spec;
        try
        {
            spec = CampaignSpec.Load(CampaignV2);
        }
        catch (CampaignSpecException e)
        {
            Console.Error.WriteLine(AsciiText.Of("level-generator --self-test curation: " + e.Message + " (ejecuta desde la raiz del repositorio)"));
            return 2;
        }

        // 1. Rechazos de Load.
        foreach ((string path, string reason) in Rejected)
        {
            Rejects(Check, spec, path, reason);
        }

        BelowSpecExitCode(Check, spec);

        // 2. Lecturas válidas y filas de la tabla del dueño (201-300, sintéticas).
        CurationDecisions? versioned = TryLoad(Check, spec, Versioned);
        CurationDecisions? valid = TryLoad(Check, spec, Valid);
        CurationDecisions? notARow = TryLoad(Check, spec, NotARow);
        List<GeneratedLevel> levels = Synthetic(spec, 201, 300);
        List<GeneratedLevel> rows = CurationReport.Rows(levels);
        Check(rows.Select(l => l.Plan.Level).SequenceEqual(RowsW06),
            "filas de la tabla del dueño en 201-300: los 30 retos e hitos y los estrenos 243 y 283 (" + rows.Count.ToString(CultureInfo.InvariantCulture) + ")");
        if (versioned is null || valid is null || notARow is null)
        {
            Console.WriteLine($"self-test curation: {failures} FAIL");
            return 1;
        }

        Check(versioned.ByLevel.Count == 0 && CurationReport.StateText(rows, versioned) == "**pendiente** (0 de 32 niveles decididos)"
              && CurationReport.StateText(rows, null) == "**pendiente** (0 de 32 niveles decididos)",
            "specs/curation_v2.json sin decisiones (o sin --decisions): estado pendiente, 0 de 32");
        Check(valid.ByLevel.Keys.Order().SequenceEqual([206, 243, 246, 266, 280, 450]) && valid.ByLevel[243].Note == "estreno con un ejemplar"
              && valid.ByLevel[206].Note is null,
            "fixture válido: seis decisiones, la note opcional en aceptar");

        // 3. Check: errores, celdas caducadas y niveles fuera de los generados.
        (List<string> errors, List<string> stale) = CurationReport.Check(valid, levels);
        Check(errors.Count == 0, "Check del fixture válido sin errores (" + string.Join("; ", errors) + ")");
        Check(stale.Count == 1 && stale[0] == "decision del nivel 280 (2026-10-15) para la celda 'Pi+Ba (MD)', que ahora es 'Pi+Br (MD)': vuelve a pendiente",
            "Check: la decisión del 280 para otra celda es un aviso y vuelve a pendiente (" + string.Join("; ", stale) + ")");
        Check(!errors.Concat(stale).Any(m => m.Contains("450", StringComparison.Ordinal)),
            "Check: el 450 (de campaign_v2, fuera de 201-300) se ignora");
        (List<string> rowErrors, List<string> rowStale) = CurationReport.Check(notARow, levels);
        Check(rowStale.Count == 0 && rowErrors.SequenceEqual([
                "decision del nivel 201, que no es reto, hito ni primer tablero de estreno",
                "decision del nivel 244, que no es reto, hito ni primer tablero de estreno"]),
            "Check: decidir el 201 (recuperación) o el 244 (segundo tablero del estreno) es un error (" + string.Join("; ", rowErrors) + ")");
        (List<string> outErrors, List<string> outStale) = CurationReport.Check(notARow, Synthetic(spec, 301, 320));
        Check(outErrors.Count == 0 && outStale.Count == 0, "Check: con 301-320 generados, las decisiones del 201 y el 244 se ignoran");

        // 4. Última columna y estado.
        Dictionary<int, GeneratedLevel> byLevel = levels.ToDictionary(l => l.Plan.Level);
        Check(CurationReport.DecisionText(byLevel[206], valid) == "aceptar (2026-10-16)", "columna: aceptar (fecha)");
        Check(CurationReport.DecisionText(byLevel[243], valid) == "aceptar (2026-10-16): estreno con un ejemplar", "columna: aceptar con note");
        Check(CurationReport.DecisionText(byLevel[246], valid) == "sustituir (2026-10-16): otro del slot \\| mide Difícil segunda línea",
            "columna: sustituir con la note, la barra escapada y el salto de línea CRLF aplanado");
        Check(CurationReport.DecisionText(byLevel[266], valid) == "cambiar la celda (2026-10-17): pasar a Pi+Ma sin el Brote",
            "columna: cambiar la celda con el salto de línea LF aplanado");
        Check(CurationReport.DecisionText(byLevel[280], valid) == "pendiente (la decisión del 2026-10-15 era para «Pi+Ba (MD)»)",
            "columna: la decisión para otra celda vuelve a pendiente y dice la celda que vio el dueño");
        Check(CurationReport.DecisionText(byLevel[209], valid) == "pendiente" && CurationReport.DecisionText(byLevel[209], null) == "pendiente",
            "columna: sin entrada (o sin --decisions), pendiente");
        Check(CurationReport.StateText(rows, valid) == "**pendiente** (4 de 32 niveles decididos)",
            "estado: pendiente con 4 de 32 (206, 243, 246 y 266; el 280 caducado y el 450 no cuentan)");

        CurationDecisions allAccepted = Decide(rows, l => CurationDecisions.Accept);
        Check(CurationReport.StateText(rows, allAccepted) == "**aceptada** (32 de 32 niveles)", "estado: aceptada con las 32 filas en aceptar");
        CurationDecisions withChanges = Decide(rows, l => l.Plan.Level switch
        {
            280 => CurationDecisions.ChangeCell,
            246 => CurationDecisions.Replace,
            _ => CurationDecisions.Accept,
        });
        Check(CurationReport.StateText(rows, withChanges) == "**anotada, con cambios pedidos** en 246, 280",
            "estado: anotada, con cambios pedidos en 246 y 280 (en el orden de la tabla)");
        CurationDecisions oneStale = Decide(rows, l => CurationDecisions.Accept, staleLevel: 300);
        Check(CurationReport.StateText(rows, oneStale) == "**pendiente** (31 de 32 niveles decididos)",
            "estado: con una celda caducada (300) vuelve a pendiente, 31 de 32");

        // 5. Dónde va la Tinaja (P7b): las comprobaciones con 321-340 sintéticos en las dos variantes.
        TinajaPlacement(Check, spec);

        Console.WriteLine(failures == 0 ? "self-test curation: todo PASS" : $"self-test curation: {failures} FAIL");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>Celdas con la Tinaja de 321-340 en la variante a de campaign_v2.json (tabla de D2, d.5.3; D-019: estreno en el 323).</summary>
    private static readonly int[] TinajaCellsW07 = [323, 324, 325, 327, 329, 332, 338, 340];

    /// <summary>
    /// «Dónde va la Tinaja» (CurationReport.TinajaChecks) con 321-340 sintéticos (el plan de cada nivel y si se publica con la
    /// Tinaja), en las dos variantes de campaign_v2.json: a) en sus 8 celdas, «sí», «ninguno» y 8 de 8; b) en p1, p2 o p19, el
    /// aviso en negrita (y fuera del calendario); c) 9 en el ciclo de estreno, «pasa del tope»; d) en la b, sin ninguna, «la
    /// salida B no la lleva», y con una, «debería ser 0». El ciclo de estreno (321-340) sale del plan del 323; con 301-320
    /// generados no hay línea del ciclo de estreno ni apariciones.
    /// </summary>
    private static void TinajaPlacement(Action<bool, string> check, CampaignSpec a)
    {
        CampaignSpec b = CampaignSpec.Load(CampaignV2, CampaignSpec.VariantB);
        List<(LevelPlan Plan, bool Tinaja)> Mark(CampaignSpec s, int first, int last, IEnumerable<int> tinaja) =>
            Enumerable.Range(first, last - first + 1).Select(n => (s.Plan(n), tinaja.Contains(n))).ToList();
        void Expect(CampaignSpec s, IEnumerable<int> tinaja, string[] expected, string what, int first = 321, int last = 340)
        {
            List<string> lines = CurationReport.TinajaChecks(s, a, Mark(s, first, last, tinaja));
            check(lines.SequenceEqual(expected), what + " (" + string.Join(" / ", lines) + ")");
        }

        const string Cells = "Apariciones por ciclo (calendario de la a): 321-340: 8 (323, 324, 325, 327, 329, 332, 338, 340).";
        const string Only = "- Tinaja solo donde la pone el calendario: sí.";
        const string NonePure = "- Nunca en p1, p2 ni p19: ninguno.";
        check(a.Plan(323).Position == 3 && a.Plan(340).Position == 20 && b.Plan(323).Cell.Literal == "Ma" && b.Plan(340).Cell.Literal == "Br+Ba (MD)",
            "campaign_v2: el 323 es p3 y el 340 p20; la b pone Ma en el 323 y Br+Ba (MD) en el 340");

        Expect(a, TinajaCellsW07,
            [Cells, Only, NonePure, "- Ciclo de estreno 321-340: 8 niveles publicados con la Tinaja (como mucho 8)."],
            "Tinaja (a): en sus 8 celdas de 321-340, sí, ninguno y 8 de 8");
        Expect(a, [323, 324, 325, 327, 329, 332, 321, 339],
            [Cells, "- Tinaja solo donde la pone el calendario: **no**: 321, 339.", "- Nunca en p1, p2 ni p19: **321, 339**.",
             "- Ciclo de estreno 321-340: 8 niveles publicados con la Tinaja (como mucho 8)."],
            "Tinaja (a): en p1 (321) y p19 (339), fuera del calendario y aviso en negrita");
        Expect(a, [322, 323],
            [Cells, "- Tinaja solo donde la pone el calendario: **no**: 322.", "- Nunca en p1, p2 ni p19: **322**.",
             "- Ciclo de estreno 321-340: 2 niveles publicados con la Tinaja (como mucho 8)."],
            "Tinaja (a): en p2 (322), aviso en negrita");
        Expect(a, [.. TinajaCellsW07, 326],
            [Cells, "- Tinaja solo donde la pone el calendario: **no**: 326.", NonePure,
             "- Ciclo de estreno 321-340: 9 niveles publicados con la Tinaja (como mucho 8): **pasa del tope**."],
            "Tinaja (a): 9 en el ciclo de estreno, pasa del tope");
        Expect(b, [],
            [Cells, Only, NonePure, "- Ciclo de estreno 321-340: 0 niveles publicados con la Tinaja (como mucho 8).",
             "- Variante b: 0 niveles con la Tinaja (la salida B no la lleva)."],
            "Tinaja (b): sin ninguna, la salida B no la lleva");
        Expect(b, [323],
            [Cells, "- Tinaja solo donde la pone el calendario: **no**: 323.", NonePure,
             "- Ciclo de estreno 321-340: 1 nivel publicado con la Tinaja (como mucho 8).",
             "- Variante b: 1 nivel con la Tinaja: **debería ser 0**."],
            "Tinaja (b): con una en el 323 (Ma en la b), fuera del calendario y debería ser 0");
        Expect(a, [], ["Apariciones por ciclo (calendario de la a): ninguna.", Only, NonePure],
            "Tinaja (a) con 301-320 generados: sin apariciones ni línea del ciclo de estreno", first: 301, last: 320);

        TinajaTable(check, a, b);
    }

    /// <summary>
    /// Cuándo sale «Dónde va la Tinaja» (CurationReport.ShowsTinaja) y sus filas (CurationReport.TinajaRows), con 321-340
    /// sintéticos en las dos variantes de campaign_v2.json: la sección sale con 321-340 en la a y en la b, y no con 201-300 ni
    /// con 301-320; hay una fila por cada una de las 8 celdas con la Tinaja de la a, en su orden; la fila del 323 y la del 340
    /// dicen la celda de la a (★Ti y Ti+Ho (MD), D-027), el respaldo de la b (Ma y Br+Ba (MD)), la celda publicada con o sin la Tinaja y
    /// las dosis «n (n − 1)» (la máscara tapa todas las capas menos la boca) o «—» sin medir.
    /// </summary>
    private static void TinajaTable(Action<bool, string> check, CampaignSpec a, CampaignSpec b)
    {
        static int K(int level) => level == 340 ? 6 : 3;
        static int Cuias(int level) => level == 340 ? 9 : 5;
        List<CurationReport.TinajaLevel> Levels(CampaignSpec s, int first, int last, Func<int, bool> tinaja, Func<int, int?> doses) =>
            Enumerable.Range(first, last - first + 1).Select(n => new CurationReport.TinajaLevel(s.Plan(n), tinaja(n), doses(n), K(n), Cuias(n))).ToList();
        IEnumerable<LevelPlan> Plans(CampaignSpec s, int first, int last) => Enumerable.Range(first, last - first + 1).Select(s.Plan);
        string Level(string row) => row.Split('|')[1].Trim();
        string Show(List<string> rows) => string.Join(" / ", rows);

        check(CurationReport.ShowsTinaja(a, Plans(a, 321, 340)) && CurationReport.ShowsTinaja(b, Plans(b, 321, 340)),
            "Tinaja: la sección sale con 321-340 en la a (celdas con Ti) y en la b (celdas con respaldo)");
        check(!CurationReport.ShowsTinaja(a, Plans(a, 201, 300)) && !CurationReport.ShowsTinaja(b, Plans(b, 201, 300))
              && !CurationReport.ShowsTinaja(a, Plans(a, 301, 320)) && !CurationReport.ShowsTinaja(b, Plans(b, 301, 320)),
            "Tinaja: la sección no sale con 201-300 ni con 301-320, en ninguna de las dos variantes");

        // a: todas las celdas con la tinaja, con 3 dosis en el estreno y 5 en el hito.
        int? DosesA(int n) => n switch { 323 => 3, 340 => 5, _ => 4 };
        List<string> rowsA = CurationReport.TinajaRows(a, a, Levels(a, 321, 340, n => TinajaCellsW07.Contains(n), DosesA));
        check(rowsA.Select(Level).SequenceEqual(TinajaCellsW07.Select(n => n.ToString(CultureInfo.InvariantCulture))),
            "Tinaja (a): una fila por cada una de las 8 celdas de 321-340, en orden (" + Show(rowsA) + ")");
        check(rowsA.Count == 8 && rowsA[0] == "| 323 | 3 | estreno | ★Ti | Ma | ★Ti: con la Tinaja | 3 (2) | 3 | 5 |",
            "Tinaja (a): fila del 323, celda ★Ti, respaldo Ma, publicado con la Tinaja y 3 dosis (2 ocultas)");
        check(rowsA.Count == 8 && rowsA[7] == "| 340 | 20 | hito | Ti+Ho (MD) | Br+Ba (MD) | Ti+Ho (MD): con la Tinaja | 5 (4) | 6 | 9 |",
            "Tinaja (a): fila del 340, celda Ti+Ho (MD), respaldo Br+Ba (MD), con la Tinaja y 5 dosis (4 ocultas)");
        List<string> rowsA340 = CurationReport.TinajaRows(a, a, Levels(a, 340, 340, _ => false, _ => null));
        check(rowsA340.SequenceEqual(["| 340 | 20 | hito | Ti+Ho (MD) | Br+Ba (MD) | Ti+Ho (MD): sin la Tinaja | — | 6 | 9 |"]),
            "Tinaja (a): el 340 publicado sin la Tinaja y sin medir dice «sin la Tinaja» y la raya en las dosis (" + Show(rowsA340) + ")");

        // b: las mismas 8 filas, con la celda del respaldo publicada sin la tinaja y sin dosis.
        List<string> rowsB = CurationReport.TinajaRows(b, a, Levels(b, 321, 340, _ => false, _ => null));
        check(rowsB.Select(Level).SequenceEqual(TinajaCellsW07.Select(n => n.ToString(CultureInfo.InvariantCulture))),
            "Tinaja (b): las mismas 8 filas, las celdas con la Tinaja de la a (" + Show(rowsB) + ")");
        check(rowsB.Count == 8 && rowsB[0] == "| 323 | 3 | comodidad | ★Ti | Ma | Ma: sin la Tinaja | — | 3 | 5 |",
            "Tinaja (b): fila del 323 (comodidad en la b, sin el estreno), celda ★Ti de la a y Ma publicada sin la Tinaja");
        check(rowsB.Count == 8 && rowsB[7] == "| 340 | 20 | hito | Ti+Ho (MD) | Br+Ba (MD) | Br+Ba (MD): sin la Tinaja | — | 6 | 9 |",
            "Tinaja (b): fila del 340, celda Ti+Ho (MD) de la a y Br+Ba (MD) publicada sin la Tinaja");
        check(CurationReport.TinajaRows(a, a, Levels(a, 201, 300, _ => false, _ => null)).Count == 0
              && CurationReport.TinajaRows(b, a, Levels(b, 201, 300, _ => false, _ => null)).Count == 0,
            "Tinaja: sin filas con 201-300 en las dos variantes");
    }

    /// <summary>El rechazo cuenta solo si el mensaje dice el motivo esperado (<paramref name="reason"/>), no otro.</summary>
    private static void Rejects(Action<bool, string> check, CampaignSpec spec, string path, string reason)
    {
        try
        {
            CurationDecisions.Load(path, spec.Path, spec.FirstLevel, spec.LastLevel);
            check(false, "rechazo: " + path + " se lee");
        }
        catch (CurationDecisionsException e)
        {
            check(e.Message.Contains(reason, StringComparison.Ordinal), $"rechazo: {e.Message}");
        }
    }

    /// <summary>--spec con --decisions mal formado sale con código 2 antes de generar (y dice el motivo por stderr).</summary>
    private static void BelowSpecExitCode(Action<bool, string> check, CampaignSpec spec)
    {
        string dir = Path.Combine(Path.GetTempPath(), "af-curation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        TextWriter error = Console.Error;
        var captured = new StringWriter(CultureInfo.InvariantCulture);
        int code;
        try
        {
            Console.SetError(captured);
            code = CampaignRunner.Run(spec.Path, "201-220", dir, null, check: true, curationPath: Path.Combine(dir, "curation.md"), decisionsPath: BelowSpec);
        }
        finally
        {
            Console.SetError(error);
            Directory.Delete(dir, recursive: true);
        }

        check(code == 2 && captured.ToString().Contains("nivel 200 fuera de la especificacion (201-700)", StringComparison.Ordinal),
            "--spec --curation --decisions con el nivel 200 sale con 2 antes de generar (codigo " + code.ToString(CultureInfo.InvariantCulture) + ")");
    }

    private static CurationDecisions? TryLoad(Action<bool, string> check, CampaignSpec spec, string path)
    {
        try
        {
            CurationDecisions d = CurationDecisions.Load(path, spec.Path, spec.FirstLevel, spec.LastLevel);
            check(d.Path == path, "lectura: " + path);
            return d;
        }
        catch (CurationDecisionsException e)
        {
            check(false, "lectura: " + e.Message);
            return null;
        }
    }

    /// <summary>Niveles sintéticos: el plan de la especificación con un slot vacío (sin tablero, que el informe del dueño no mira aquí).</summary>
    private static List<GeneratedLevel> Synthetic(CampaignSpec spec, int first, int last)
    {
        var levels = new List<GeneratedLevel>();
        for (int n = first; n <= last; n++)
        {
            LevelPlan plan = spec.Plan(n);
            levels.Add(new GeneratedLevel { Plan = plan, Slot = new SlotResult(plan, [], 0, [], false, 0) });
        }

        return levels;
    }

    /// <summary>Una decisión por fila con la celda actual (o con otra en <paramref name="staleLevel"/>), con note si no es aceptar.</summary>
    private static CurationDecisions Decide(List<GeneratedLevel> rows, Func<GeneratedLevel, string> decision, int staleLevel = 0)
    {
        var byLevel = new Dictionary<int, CurationDecision>();
        foreach (GeneratedLevel l in rows)
        {
            string d = decision(l);
            string cell = l.Plan.Level == staleLevel ? l.Plan.Cell.Literal + " (antes)" : l.Plan.Cell.Literal;
            byLevel.Add(l.Plan.Level, new CurationDecision(l.Plan.Level, cell, d, "2026-10-16", d == CurationDecisions.Accept ? null : "motivo"));
        }

        return new CurationDecisions { Path = "memoria", ByLevel = byLevel };
    }
}
