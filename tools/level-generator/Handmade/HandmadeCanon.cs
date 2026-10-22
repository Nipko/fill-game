// Datos canónicos del GDD que necesita el modo --handmade (tutorial hecho a mano, docs/dev/prototipo-v0.md b).
// Solo datos copiados del GDD citando su sección: las reglas (legal, cantidad, fútil, sellado, victoria, bloqueo) NO
// están aquí; se evalúan siempre con AF.Core (.claude/rules/core.md: tools nunca copian ni reimplementan reglas).
// Si el GDD cambia, este archivo se actualiza a mano citando la sección.
namespace AmazonFill.Tools.LevelGenerator.Handmade;

/// <summary>Un tinte de la paleta (glosario §9).</summary>
internal sealed record Tint(string Id, int Engine, string NameEs, int DebutLevel);

/// <summary>Una fila de la tabla del tutorial (02 §13).</summary>
internal sealed record TutorialRow(int Level, int Cuias, int Colors, int Empties, string[] Tints, int ParMin, int ParMax);

internal static class HandmadeCanon
{
    /// <summary>Glosario §9: ID de código, índice de motor, nombre ES y nivel de debut.</summary>
    public static readonly Tint[] Tints =
    [
        new("guanabana", 1, "Guanábana", 4),
        new("cupuacu", 2, "Copoazú", 81),
        new("tapereba", 3, "Taperebá", 2),
        new("aguaje", 4, "Aguaje", 1),
        new("urucum", 5, "Achiote", 41),
        new("camu_camu", 6, "Camu camu", 21),
        new("bacaba", 7, "Bacaba", 7),
        new("acai", 8, "Açaí", 1),
        new("jenipapo", 9, "Huito", 141),
        new("agua_tapajos", 10, "Agua del Tapajós", 11),
        new("lulo", 11, "Lulo", 111),
        new("araza", 12, "Arazá", 301),
        new("cacao", 13, "Cacao", 201),
        new("agua_negro", 14, "Agua del Río Negro", 61),
    ];

    /// <summary>Glosario §9: pares de riesgo (índices de motor).</summary>
    public static readonly (int A, int B)[] RiskPairs =
    [
        (4, 5), (5, 6), (6, 13), (13, 14), (14, 4), (3, 12), (12, 11), (11, 10), (8, 9), (1, 2),
    ];

    // Los 2 pares del simulador de daltonismo (glosario §9; 07 §3, regla 3; Q-B9-13 bis): 7-10 y 5-14. No cuentan en
    // max_pares_riesgo y en el tutorial no aparecen: sus tintes (1, 3, 4, 7 y 8) no forman ninguno, porque el 10 debuta en el
    // 11 y el 5 en el 41 (lo comprueba TutorialPackTests en EditMode).

    /// <summary>Glosario §9: <c>max_pares_riesgo</c> = 0 en los niveles 1-40.</summary>
    public const int MaxRiskPairs = 0;

    /// <summary>02 R1 y §8: cuia estándar (la Cuia chica debuta en el 14).</summary>
    public const int Capacity = 4;

    /// <summary>02 R3: <c>U_c</c> = 4 salvo fruta abundante (Cuia alta, 27).</summary>
    public const int UnitsPerColor = 4;

    /// <summary>docs/dev/prototipo-v0.md b: BFS exhaustivo hasta 8 cuias.</summary>
    public const int MaxBfsCuias = 8;

    /// <summary>02 §13: niveles 1-10 (cuias = colores + vacíos, tintes de la fila y par [P]).</summary>
    public static readonly TutorialRow[] Table0213 =
    [
        new(1, 3, 2, 1, ["acai", "aguaje"], 3, 3),
        new(2, 5, 3, 2, ["acai", "aguaje", "tapereba"], 4, 6),
        new(3, 5, 3, 2, ["acai", "aguaje", "tapereba"], 5, 7),
        new(4, 6, 4, 2, ["acai", "aguaje", "tapereba", "guanabana"], 7, 9),
        new(5, 6, 4, 2, ["acai", "aguaje", "tapereba", "guanabana"], 9, 11),
        new(6, 5, 4, 1, ["acai", "aguaje", "tapereba", "guanabana"], 10, 13),
        new(7, 7, 5, 2, ["acai", "aguaje", "tapereba", "guanabana", "bacaba"], 10, 13),
        new(8, 7, 5, 2, ["acai", "aguaje", "tapereba", "guanabana", "bacaba"], 12, 15),
        new(9, 7, 5, 2, ["acai", "aguaje", "tapereba", "guanabana", "bacaba"], 13, 16),
        new(10, 7, 5, 2, ["acai", "aguaje", "tapereba", "guanabana", "bacaba"], 14, 18),
    ];

    /// <summary>02 §13, nivel 1: estado inicial literal [açaí, açaí, açaí, aguaje], [aguaje, aguaje, aguaje, açaí], vacía.</summary>
    public static readonly int[][] Level1Literal = [[8, 8, 8, 4], [4, 4, 4, 8], []];

    // Cabecera del pack v0 (prototipo-v0 b; glosario §8 y §11: Estuario, mundo 1, vuelta 1, agua blanca).
    public const string SubsetOf = "af-level/1";
    public const string PackId = "tutorial_v0";
    public const string Kind = "campaign";
    public const int World = 1;
    public const string WorldId = "w01_estuario";
    public const int Lap = 1;
    public const string WaterBase = "white";

    /// <summary>Glosario §11: <c>m{mm}_v{v}_n{nnnn}</c>.</summary>
    public static string LevelId(int level) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"m{World:00}_v{Lap}_n{level:0000}");

    public static Tint? FindTint(string id)
    {
        foreach (Tint t in Tints)
        {
            if (t.Id == id)
            {
                return t;
            }
        }

        return null;
    }

    public static Tint TintByEngine(int engine)
    {
        foreach (Tint t in Tints)
        {
            if (t.Engine == engine)
            {
                return t;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(engine), engine, "Índice de motor sin tinte (glosario §9).");
    }

    public static TutorialRow? Row(int level)
    {
        foreach (TutorialRow r in Table0213)
        {
            if (r.Level == level)
            {
                return r;
            }
        }

        return null;
    }
}
