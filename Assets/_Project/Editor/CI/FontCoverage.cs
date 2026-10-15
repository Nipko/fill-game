#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using AF.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Las costuras internas de AF.EditorTools (lecturas de las tablas inyectables, lista de fuentes de FontSetup) las prueban los
// tests EditMode (tarea B1).
[assembly: InternalsVisibleTo("AF.Tests.EditMode")]

namespace AF.EditorTools
{
    /// <summary>Familia tipográfica de un texto (dirección visual v2, 5.6): Nunito en el cuerpo y Baloo 2 en los títulos.</summary>
    [Flags]
    public enum GlyphFamily
    {
        /// <summary>Ninguna (no se usa en las tablas).</summary>
        None = 0,

        /// <summary>Nunito (Regular, Bold y ExtraBold): el cuerpo, los textos corridos, las ranuras y las píldoras.</summary>
        Body = 1,

        /// <summary>Baloo 2 (Bold y ExtraBold): cintas, insignias, títulos de modal y etiquetas de los CTA.</summary>
        Title = 2,

        /// <summary>Cualquiera de las dos: la UI puede pintar una clave en los dos estilos.</summary>
        Both = Body | Title,
    }

    /// <summary>Un texto de una tabla en un idioma, con su archivo, su línea y su clave (para los mensajes).</summary>
    public readonly struct LocTextEntry
    {
        /// <summary>Texto <paramref name="text"/> de <paramref name="key"/> en <paramref name="locale"/> (<paramref name="file"/>, línea <paramref name="line"/>).</summary>
        public LocTextEntry(string file, int line, string key, string locale, string text)
        {
            File = file ?? throw new ArgumentNullException(nameof(file));
            Line = line;
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Locale = locale ?? throw new ArgumentNullException(nameof(locale));
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        /// <summary>Archivo («strings.csv», «facts.csv», «glossary.csv»).</summary>
        public string File { get; }

        /// <summary>Línea del CSV donde empieza la fila.</summary>
        public int Line { get; }

        /// <summary>Clave (o término, en el glosario).</summary>
        public string Key { get; }

        /// <summary>Idioma de la columna (es-419, en o pt-BR).</summary>
        public string Locale { get; }

        /// <summary>El texto de la celda, sin comillas.</summary>
        public string Text { get; }

        /// <summary>«strings.csv:13 double_harvest_delta (es-419)».</summary>
        public string Where => File + ":" + Line.ToString(CultureInfo.InvariantCulture) + " " + Key + " (" + Locale + ")";
    }

    /// <summary>Resultado de <see cref="FontCoverage.Analyze"/>: los errores y los caracteres que necesita cada familia.</summary>
    public sealed class FontCoverageReport
    {
        internal FontCoverageReport(List<string> errors, string body, string title, int texts)
        {
            Errors = errors;
            BodyCharacters = body;
            TitleCharacters = title;
            Texts = texts;
        }

        /// <summary>Un error por línea: glifo que falta en una TTF de su familia, ✓ en el texto o ↑/↓ fuera de su clave.</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>Si no hay ningún error.</summary>
        public bool Ok => Errors.Count == 0;

        /// <summary>Caracteres visibles que tiene que tener Nunito, en orden de código y sin repetir.</summary>
        public string BodyCharacters { get; }

        /// <summary>Caracteres visibles que tiene que tener Baloo 2, en orden de código y sin repetir (incluye los del cuerpo).</summary>
        public string TitleCharacters { get; }

        /// <summary>Cuántos textos (celdas no vacías) se revisaron.</summary>
        public int Texts { get; }

        /// <summary>Los caracteres que necesita <paramref name="family"/> (<see cref="GlyphFamily.Title"/> o <see cref="GlyphFamily.Body"/>).</summary>
        public string CharactersFor(GlyphFamily family)
        {
            return family == GlyphFamily.Title ? TitleCharacters : BodyCharacters;
        }
    }

    /// <summary>
    /// Cobertura de las fuentes (D-018; propuesta de idiomas §4.1 y §4.3; tarea B1): todo carácter de las tablas de textos
    /// (<c>strings.csv</c>, <c>facts.csv</c> y, si existe, <c>glossary.csv</c>; en es-419, en y pt-BR) y todo carácter que generan
    /// el formateo de cifras (<see cref="LocNumber.Characters"/>) y la pseudolocalización (<see cref="LocPseudo.Characters"/>) tiene
    /// que estar en la TTF de su familia: Nunito para el cuerpo y Baloo 2 para los títulos. Así ningún texto llega al jugador
    /// como □ (el glifo ausente de TextMeshPro). Reglas fijas de la tarea:
    /// <list type="bullet">
    /// <item><description>una clave puede salir en cualquiera de las dos familias, así que sus caracteres van en las cinco TTF,
    /// salvo las de <see cref="TitleOnlyKeys"/>, que solo se pintan en Baloo 2;</description></item>
    /// <item><description>↑ y ↓ solo en <c>map_label_andes</c> y <c>map_label_atlantic</c> (glosario §10, P-M2; van en Baloo 2,
    /// que tiene esos glifos);</description></item>
    /// <item><description>✓ nunca va en el texto: es un icono procedural (M3.3; ninguna de las dos familias lo tiene).</description></item>
    /// </list>
    /// Lo usa <see cref="FontSetup"/> para calcular los glifos de los atlas.
    ///
    /// Uso en batchmode (sin ningún Editor abierto en la carpeta; <c>x86_64</c> en un equipo x64):
    ///   unity run &lt;repo&gt; --architecture arm64 --log-file Logs/font-coverage.log --non-interactive
    ///             -- -nographics -executeMethod AF.EditorTools.FontCoverage.CheckProject
    /// Códigos de salida (solo en batchmode): 0 OK · 4 excepción · 5 faltan glifos en una TTF o en un atlas generado, o hay
    /// caracteres prohibidos (los lista el log, una línea <c>[AF.FontCoverage] ERROR</c> por problema) · 6 las tablas de
    /// textos no se pueden leer.
    /// </summary>
    public static class FontCoverage
    {
        /// <summary>Carpeta de las tablas de textos (08 §4.7).</summary>
        public const string LocDir = "Assets/_Project/Data/Loc";

        /// <summary>Tabla de la UI (la misma ruta que <c>AF.UI.LocTables.StringsAssetPath</c>).</summary>
        public const string StringsPath = LocDir + "/" + LocCatalog.StringsFile;

        /// <summary>Tabla de los textos largos (la misma ruta que <c>AF.UI.LocTables.FactsAssetPath</c>).</summary>
        public const string FactsPath = LocDir + "/" + LocCatalog.FactsFile;

        /// <summary>Nombre del glosario de los traductores (L2c; opcional: si no existe, no cuenta).</summary>
        public const string GlossaryFile = "glossary.csv";

        /// <summary>Glosario de los traductores (L2c). No lo lee el juego, pero sus caracteres van en los atlas.</summary>
        public const string GlossaryPath = LocDir + "/" + GlossaryFile;

        /// <summary>Flecha hacia arriba (U+2191), solo en <see cref="ArrowKeys"/>.</summary>
        public const char ArrowUp = '↑';

        /// <summary>Flecha hacia abajo (U+2193), solo en <see cref="ArrowKeys"/>.</summary>
        public const char ArrowDown = '↓';

        /// <summary>Marca de verificación (U+2713): nunca en el texto; es un icono (M3.3).</summary>
        public const char CheckMark = '✓';

        /// <summary>Fuentes de Nunito (cuerpo), sin extensión (las TTF de <see cref="FontSetup.SourceDir"/>).</summary>
        public static readonly string[] BodyFonts = { "Nunito-ExtraBold", "Nunito-Bold", "Nunito-Regular" };

        /// <summary>Fuentes de Baloo 2 (títulos), sin extensión.</summary>
        public static readonly string[] TitleFonts = { "Baloo2-ExtraBold", "Baloo2-Bold" };

        /// <summary>Idiomas de las tablas cuyos caracteres van en los atlas (tanda 1 de idiomas; D-018).</summary>
        public static readonly string[] Locales = { LocLocale.SpanishLatinAmerica, LocLocale.English, LocLocale.PortugueseBrazil };

        /// <summary>
        /// Claves que solo se pintan en Baloo 2, en orden ordinal: <c>double_harvest_delta</c> («+20 → +40»), la etiqueta de la
        /// Cosecha doble con el estilo <c>CtaWater</c> (<c>GameScreen</c>; a Nunito le falta «→», propuesta de idiomas §4.1) [P],
        /// y los dos rótulos de orientación del Mapa (glosario §10, P-M2; M1.5).
        /// </summary>
        public static readonly string[] TitleOnlyKeys = { "double_harvest_delta", "map_label_andes", "map_label_atlantic" };

        /// <summary>Únicas claves que pueden llevar ↑ y ↓ (glosario §10, P-M2).</summary>
        public static readonly string[] ArrowKeys = { "map_label_andes", "map_label_atlantic" };

        private const string LogPrefix = "[AF.FontCoverage] ";

        // Máximo de textos citados por cada glifo que falta (el resto, «y N más»).
        private const int MaxCitations = 4;

        /// <summary>
        /// Comprueba las tablas del proyecto contra las TTF y los atlas generados contra las tablas (un carácter nuevo sin
        /// regenerar los atlas saldría como □: <see cref="FontSetup.AtlasGaps"/>) y, en batchmode, sale con el código del resultado.
        /// </summary>
        [MenuItem("Amazon Fill/Arte/Comprobar la cobertura de las fuentes")]
        public static void CheckProject()
        {
            int code;
            try
            {
                code = Run(true, out _);
            }
            catch (Exception e)
            {
                Debug.LogError(LogPrefix + "excepción: " + e);
                code = 4;
            }

            if (code == 0)
            {
                Debug.Log(LogPrefix + "OK: todos los caracteres de las tablas están en las TTF y en los atlas de su familia.");
            }
            else
            {
                Debug.LogError(LogPrefix + "FALLO (código " + code + ").");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }

        /// <summary>
        /// Lee las tablas, las analiza contra las TTF (y, con <paramref name="atlases"/>, contra los atlas generados) y registra cada
        /// error. Devuelve 0 (OK), 5 (faltan glifos o hay caracteres prohibidos) o 6 (una tabla no se puede leer);
        /// <paramref name="report"/> es null con 6.
        /// </summary>
        public static int Run(bool atlases, out FontCoverageReport? report)
        {
            return Run(atlases, out report, ReadProjectTables);
        }

        /// <summary>
        /// Como <see cref="Run(bool, out FontCoverageReport?)"/>, con los textos que devuelve <paramref name="read"/> (costura de
        /// los tests: una lectura que lanza <see cref="LocCsvException"/> prueba el código 6).
        /// </summary>
        internal static int Run(bool atlases, out FontCoverageReport? report, Func<IReadOnlyList<LocTextEntry>> read)
        {
            if (read == null)
            {
                throw new ArgumentNullException(nameof(read));
            }

            report = null;
            IReadOnlyList<LocTextEntry> texts;
            try
            {
                texts = read();
            }
            catch (Exception e) when (e is LocCsvException || e is IOException || e is FormatException)
            {
                Debug.LogError(LogPrefix + "no se pueden leer las tablas de textos: " + e.Message);
                return 6;
            }

            report = Analyze(texts, new TtfGlyphs().Has);
            var errors = new List<string>(report.Errors);
            if (atlases)
            {
                errors.AddRange(FontSetup.AtlasGaps(report));
            }

            for (int i = 0; i < errors.Count; i++)
            {
                Debug.LogError(LogPrefix + "ERROR " + errors[i]);
            }

            Debug.Log(LogPrefix + report.Texts + " textos; cuerpo (Nunito) " + report.BodyCharacters.Length
                + " caracteres, títulos (Baloo 2) " + report.TitleCharacters.Length + ".");
            return errors.Count == 0 ? 0 : 5;
        }

        /// <summary>
        /// Los textos no vacíos de las tablas del proyecto en <see cref="Locales"/>: <c>strings.csv</c> y <c>facts.csv</c> (con
        /// <see cref="LocTable"/>, que valida el formato) y <c>glossary.csv</c> si existe (con <see cref="LocCsv"/>: su cabecera es
        /// otra; cuentan las columnas con nombre de idioma). UTF-8 estricto (<see cref="LocCsv.Decode"/>).
        /// </summary>
        public static IReadOnlyList<LocTextEntry> ReadProjectTables()
        {
            var texts = new List<LocTextEntry>();
            AddTable(texts, LocTable.Parse(ReadFile(StringsPath, LocCatalog.StringsFile), LocCatalog.StringsFile));
            AddTable(texts, LocTable.Parse(ReadFile(FactsPath, LocCatalog.FactsFile), LocCatalog.FactsFile));
            if (File.Exists(DiskPath(GlossaryPath)))
            {
                AddGlossary(texts, LocCsv.Parse(ReadFile(GlossaryPath, GlossaryFile), GlossaryFile), GlossaryFile);
            }

            return texts;
        }

        /// <summary>Añade a <paramref name="texts"/> las celdas no vacías de <paramref name="table"/> en <see cref="Locales"/>.</summary>
        public static void AddTable(List<LocTextEntry> texts, LocTable table)
        {
            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            for (int r = 0; r < table.Count; r++)
            {
                for (int l = 0; l < Locales.Length; l++)
                {
                    string text = table.TextAt(r, Locales[l]);
                    if (text.Length > 0)
                    {
                        texts.Add(new LocTextEntry(table.File, table.LineAt(r), table.KeyAt(r), Locales[l], text));
                    }
                }
            }
        }

        /// <summary>
        /// Añade las celdas no vacías de un CSV con cabecera libre (el glosario): cuentan las columnas que se llaman como un
        /// idioma de <see cref="Locales"/>; la clave es la primera columna.
        /// </summary>
        public static void AddGlossary(List<LocTextEntry> texts, IReadOnlyList<LocCsvRow> rows, string file)
        {
            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            if (rows == null || rows.Count == 0)
            {
                return;
            }

            IReadOnlyList<string> header = rows[0].Fields;
            var columns = new List<int>();
            var names = new List<string>();
            for (int c = 0; c < header.Count; c++)
            {
                if (Array.IndexOf(Locales, header[c]) >= 0)
                {
                    columns.Add(c);
                    names.Add(header[c]);
                }
            }

            for (int r = 1; r < rows.Count; r++)
            {
                IReadOnlyList<string> fields = rows[r].Fields;
                string key = fields.Count > 0 ? fields[0] : string.Empty;
                for (int i = 0; i < columns.Count; i++)
                {
                    string text = columns[i] < fields.Count ? fields[columns[i]] : string.Empty;
                    if (text.Length > 0)
                    {
                        texts.Add(new LocTextEntry(file, rows[r].Line, key, names[i], text));
                    }
                }
            }
        }

        /// <summary>Familia en la que se pinta <paramref name="key"/>: <see cref="GlyphFamily.Title"/> en <see cref="TitleOnlyKeys"/> y, si no, las dos.</summary>
        public static GlyphFamily FamilyOf(string key)
        {
            return Contains(TitleOnlyKeys, key) ? GlyphFamily.Title : GlyphFamily.Both;
        }

        /// <summary>Las TTF de <paramref name="family"/> (<see cref="GlyphFamily.Body"/> o <see cref="GlyphFamily.Title"/>).</summary>
        public static string[] FontsOf(GlyphFamily family)
        {
            return family == GlyphFamily.Title ? TitleFonts : BodyFonts;
        }

        /// <summary>La familia de una de las cinco TTF (<see cref="GlyphFamily.None"/> si no es ninguna).</summary>
        public static GlyphFamily FamilyOfFont(string font)
        {
            if (Contains(TitleFonts, font))
            {
                return GlyphFamily.Title;
            }

            return Contains(BodyFonts, font) ? GlyphFamily.Body : GlyphFamily.None;
        }

        /// <summary>
        /// Analiza <paramref name="texts"/> con <paramref name="hasGlyph"/> (fuente sin extensión, carácter → si la TTF lo tiene):
        /// los caracteres prohibidos (✓; ↑ y ↓ fuera de <see cref="ArrowKeys"/>) y, por cada TTF de cada familia, los caracteres
        /// que le faltan, con los textos que los usan. Los caracteres de control (el salto de línea de dentro de una celda) no
        /// cuentan. Determinista: mismos textos, mismos errores en el mismo orden.
        /// </summary>
        public static FontCoverageReport Analyze(IReadOnlyList<LocTextEntry> texts, Func<string, char, bool> hasGlyph)
        {
            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            if (hasGlyph == null)
            {
                throw new ArgumentNullException(nameof(hasGlyph));
            }

            var errors = new List<string>();

            // Carácter → textos que lo usan, por familia (SortedDictionary: orden de código, sin depender del hash).
            var body = new SortedDictionary<char, List<string>>();
            var title = new SortedDictionary<char, List<string>>();
            for (int t = 0; t < texts.Count; t++)
            {
                LocTextEntry entry = texts[t];
                GlyphFamily family = FamilyOf(entry.Key);
                bool arrowsAllowed = Contains(ArrowKeys, entry.Key);
                string text = entry.Text;
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    if (char.IsControl(c))
                    {
                        continue;
                    }

                    if (c == CheckMark)
                    {
                        errors.Add(entry.Where + ": " + Describe(c) + " nunca va en el texto: es un icono procedural (M3.3).");
                        continue;
                    }

                    if ((c == ArrowUp || c == ArrowDown) && !arrowsAllowed)
                    {
                        errors.Add(entry.Where + ": " + Describe(c) + " solo va en " + string.Join(" y ", ArrowKeys) + " (glosario §10, P-M2).");
                        continue;
                    }

                    if ((family & GlyphFamily.Body) != 0)
                    {
                        Cite(body, c, entry.Where);
                    }

                    if ((family & GlyphFamily.Title) != 0)
                    {
                        Cite(title, c, entry.Where);
                    }
                }
            }

            AddGenerated(body, LocNumber.Characters, "LocNumber.Characters");
            AddGenerated(body, LocPseudo.Characters, "LocPseudo.Characters");
            AddGenerated(title, LocNumber.Characters, "LocNumber.Characters");
            AddGenerated(title, LocPseudo.Characters, "LocPseudo.Characters");

            CheckFonts(errors, BodyFonts, body, hasGlyph);
            CheckFonts(errors, TitleFonts, title, hasGlyph);
            return new FontCoverageReport(errors, Join(body), Join(title), texts.Count);
        }

        /// <summary>«→» (U+2192), para los mensajes.</summary>
        public static string Describe(char c)
        {
            return "«" + c + "» (U+" + ((int)c).ToString("X4", CultureInfo.InvariantCulture) + ")";
        }

        /// <summary>Ruta en disco de <paramref name="assetPath"/> («Assets/…»).</summary>
        public static string DiskPath(string assetPath)
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            return Path.Combine(root, assetPath);
        }

        private static string ReadFile(string assetPath, string file)
        {
            return LocCsv.Decode(File.ReadAllBytes(DiskPath(assetPath)), file);
        }

        private static void Cite(SortedDictionary<char, List<string>> chars, char c, string where)
        {
            if (!chars.TryGetValue(c, out List<string>? list))
            {
                list = new List<string>();
                chars.Add(c, list);
            }

            list.Add(where);
        }

        private static void AddGenerated(SortedDictionary<char, List<string>> chars, string generated, string source)
        {
            for (int i = 0; i < generated.Length; i++)
            {
                Cite(chars, generated[i], source);
            }
        }

        private static void CheckFonts(List<string> errors, string[] fonts, SortedDictionary<char, List<string>> chars, Func<string, char, bool> hasGlyph)
        {
            for (int f = 0; f < fonts.Length; f++)
            {
                foreach (KeyValuePair<char, List<string>> pair in chars)
                {
                    if (hasGlyph(fonts[f], pair.Key))
                    {
                        continue;
                    }

                    var sb = new StringBuilder();
                    sb.Append(fonts[f]).Append(".ttf no tiene ").Append(Describe(pair.Key)).Append(", que usa ");
                    int shown = Math.Min(MaxCitations, pair.Value.Count);
                    for (int i = 0; i < shown; i++)
                    {
                        sb.Append(i == 0 ? string.Empty : ", ").Append(pair.Value[i]);
                    }

                    if (pair.Value.Count > shown)
                    {
                        sb.Append(" y ").Append((pair.Value.Count - shown).ToString(CultureInfo.InvariantCulture)).Append(" más");
                    }

                    sb.Append('.');
                    errors.Add(sb.ToString());
                }
            }
        }

        private static string Join(SortedDictionary<char, List<string>> chars)
        {
            var sb = new StringBuilder(chars.Count);
            foreach (KeyValuePair<char, List<string>> pair in chars)
            {
                sb.Append(pair.Key);
            }

            return sb.ToString();
        }

        private static bool Contains(string[] values, string value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Glifos de las TTF de <see cref="FontSetup.SourceDir"/> leídos con el <see cref="FontEngine"/> de Unity (sin TMP ni
        /// paquetes): un carácter está si la tabla cmap de la fuente le da un glifo distinto de 0 (<c>.notdef</c>). Una instancia por
        /// análisis: el FontEngine tiene una sola cara cargada, que también cambian TMP y otras herramientas.
        /// </summary>
        public sealed class TtfGlyphs
        {
            private readonly Dictionary<string, Dictionary<char, bool>> _cache = new Dictionary<string, Dictionary<char, bool>>(StringComparer.Ordinal);
            private string? _loaded;

            /// <summary>Si la TTF <paramref name="font"/> (sin extensión) tiene un glifo para <paramref name="c"/>.</summary>
            public bool Has(string font, char c)
            {
                if (!_cache.TryGetValue(font, out Dictionary<char, bool>? known))
                {
                    known = new Dictionary<char, bool>();
                    _cache.Add(font, known);
                }

                if (known.TryGetValue(c, out bool has))
                {
                    return has;
                }

                if (!string.Equals(_loaded, font, StringComparison.Ordinal))
                {
                    string path = DiskPath(FontSetup.SourceDir + "/" + font + ".ttf");
                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException(LogPrefix + "no existe la fuente " + path + ".", path);
                    }

                    FontEngineError error = FontEngine.LoadFontFace(path);
                    if (error != FontEngineError.Success)
                    {
                        throw new IOException(LogPrefix + "FontEngine no puede abrir " + path + ": " + error + ".");
                    }

                    _loaded = font;
                }

                has = FontEngine.TryGetGlyphIndex(c, out uint index) && index != 0;
                known.Add(c, has);
                return has;
            }
        }
    }
}
