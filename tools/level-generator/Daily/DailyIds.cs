// Identificadores del pool del Reto del río (03 §16; 08 §4.1; esquema schemas/af-level-1.schema.json, rama kind = daily):
//   - id   d{b}_n{nnnn}                      (patrón ^d[1-3]_n[0-9]{4}$), p. ej. d2_n0417;
//   - seed amazonfill:v1:daily:b{b}:d{ddd}   (patrón ^amazonfill:v1:daily:b[1-3]:d[0-9]{3}$), p. ej. amazonfill:v1:daily:b2:d417;
// con b = banda 1-3 y el día del pool 0-728 con ceros a la izquierda: cuatro cifras en el id y tres en la semilla (03 §16).
// El pool tiene 729 días por banda (R-Q2b, decidida por el dueño el 2026-10-19: 81 × 9, para que el mundo 1 + (día mod 9)
// siga a la fecha en todas las vueltas); los patrones del esquema no cambian.
// El padding es exacto: d2_n417 o amazonfill:v1:daily:b2:d0417 no son válidos, porque el hash de la semilla (Prng.SeedFrom,
// FNV-1a sobre los bytes UTF-8 del texto) cambiaría y el tablero no se reproduciría (glosario §11; 03 §17 #10).
// Lo prueba --self-test daily (DailyCommand).
using System.Globalization;
using System.Text.RegularExpressions;
using AF.Core.Determinism;

namespace AmazonFill.Tools.LevelGenerator.Daily;

internal static partial class DailyIds
{
    public const int MinBand = 1;
    public const int MaxBand = 3;

    /// <summary>Días del pool por banda: 729 = 81 × 9 (R-Q2b), 3 × 729 = 2.187 tableros (03 §16 decía 730).</summary>
    public const int Days = 729;

    /// <summary>Copia del patrón del esquema (allOf[0].then…id.pattern); --self-test daily comprueba que son iguales.</summary>
    public const string IdPattern = "^d[1-3]_n[0-9]{4}$";

    /// <summary>Copia del patrón del esquema (allOf[0].then…seed.pattern); --self-test daily comprueba que son iguales.</summary>
    public const string SeedPattern = "^amazonfill:v1:daily:b[1-3]:d[0-9]{3}$";

    /// <summary><c>d{b}_n{nnnn}</c>, p. ej. <c>d2_n0417</c>.</summary>
    public static string Id(int band, int day)
    {
        Validate(band, day);
        return "d" + band.ToString(CultureInfo.InvariantCulture) + "_n" + day.ToString("D4", CultureInfo.InvariantCulture);
    }

    /// <summary><c>amazonfill:v1:daily:b{b}:d{ddd}</c>, p. ej. <c>amazonfill:v1:daily:b2:d417</c>.</summary>
    public static string Seed(int band, int day)
    {
        Validate(band, day);
        return "amazonfill:v1:daily:b" + band.ToString(CultureInfo.InvariantCulture) + ":d" + day.ToString("D3", CultureInfo.InvariantCulture);
    }

    /// <summary>Semilla numérica del tablero: <see cref="Prng.SeedFrom"/> de <see cref="Seed"/>.</summary>
    public static ulong SeedValue(int band, int day) => Prng.SeedFrom(Seed(band, day));

    /// <summary>Lee un id con el padding exacto y la banda y el día en rango; false si no lo es.</summary>
    public static bool TryParseId(string? id, out int band, out int day) => TryParse(id, IdRegex(), out band, out day);

    /// <summary>Lee una semilla con el padding exacto y la banda y el día en rango; false si no lo es.</summary>
    public static bool TryParseSeed(string? seed, out int band, out int day) => TryParse(seed, SeedRegex(), out band, out day);

    /// <summary>La semilla del tablero con ese id (y al revés), o null si el texto no es válido.</summary>
    public static string? SeedOfId(string? id) => TryParseId(id, out int b, out int d) ? Seed(b, d) : null;

    public static string? IdOfSeed(string? seed) => TryParseSeed(seed, out int b, out int d) ? Id(b, d) : null;

    /// <summary>Mensaje del error, o null si la banda (1-3) y el día (0-728) son válidos.</summary>
    public static string? Problem(int band, int day)
    {
        if (band < MinBand || band > MaxBand)
        {
            return string.Create(CultureInfo.InvariantCulture, $"banda {band} fuera de {MinBand}-{MaxBand} (03 sec. 16)");
        }

        if (day < 0 || day >= Days)
        {
            return string.Create(CultureInfo.InvariantCulture, $"dia {day} fuera de 0-{Days - 1} (03 sec. 16)");
        }

        return null;
    }

    public static void Validate(int band, int day)
    {
        string? problem = Problem(band, day);
        if (problem is not null)
        {
            throw new ArgumentOutOfRangeException(band < MinBand || band > MaxBand ? nameof(band) : nameof(day), problem);
        }
    }

    private static bool TryParse(string? text, Regex regex, out int band, out int day)
    {
        band = 0;
        day = 0;
        if (text is null)
        {
            return false;
        }

        Match m = regex.Match(text);
        if (!m.Success)
        {
            return false;
        }

        band = int.Parse(m.Groups["b"].Value, NumberStyles.None, CultureInfo.InvariantCulture);
        day = int.Parse(m.Groups["d"].Value, NumberStyles.None, CultureInfo.InvariantCulture);
        if (Problem(band, day) is not null)
        {
            band = 0;
            day = 0;
            return false;
        }

        return true;
    }

    // Los mismos patrones con grupos; \z (no $) para no aceptar un salto de línea final; [0-9] y no \d (solo cifras ASCII).
    [GeneratedRegex(@"^d(?<b>[1-3])_n(?<d>[0-9]{4})\z", RegexOptions.CultureInvariant)]
    private static partial Regex IdRegex();

    [GeneratedRegex(@"^amazonfill:v1:daily:b(?<b>[1-3]):d(?<d>[0-9]{3})\z", RegexOptions.CultureInvariant)]
    private static partial Regex SeedRegex();
}
