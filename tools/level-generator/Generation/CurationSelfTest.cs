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

        Console.WriteLine(failures == 0 ? "self-test curation: todo PASS" : $"self-test curation: {failures} FAIL");
        return failures == 0 ? 0 : 1;
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
