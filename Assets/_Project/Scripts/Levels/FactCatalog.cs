#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AF.Levels
{
    /// <summary>Tipo de sujeto de una serie de Frutas del río (08 §4.6, campo <c>kind</c>) [P].</summary>
    public enum FactSeriesKind
    {
        /// <summary><c>tint</c>: un tinte de la paleta (glosario §9), fruta o agua. Más adelante, ríos y especies.</summary>
        Tint = 0,
    }

    /// <summary>Una fuente del catálogo del calendario (<c>sources</c>): el id que citan las filas de <c>facts.csv</c>. Inmutable.</summary>
    public sealed class FactSource
    {
        /// <summary>Crea la fuente (el lector ya la validó).</summary>
        public FactSource(string id, string title, string url)
        {
            Id = id;
            Title = title;
            Url = url;
        }

        /// <summary>Id de la fuente (<c>A1</c>, <c>Gb2</c>…), como lo cita el <c>context</c> de cada línea.</summary>
        public string Id { get; }

        /// <summary>Título legible (sin abrir el navegador desde el juego: la ficha lo muestra como texto).</summary>
        public string Title { get; }

        /// <summary>Dirección de la fuente, como texto.</summary>
        public string Url { get; }
    }

    /// <summary>
    /// Una serie de Frutas del río (<c>af-facts/1</c>; 08 §4.6; contrato 31): <see cref="Lines"/> líneas del sujeto
    /// <see cref="Subject"/> desde el nivel <see cref="FromLevel"/>, una cada <see cref="Step"/> niveles. Las claves de texto se
    /// derivan del sujeto (pregunta 49 (a), D-022; 08 §4.7): <c>fact.&lt;sujeto&gt;.&lt;kk&gt;</c> para la línea k,
    /// <c>fact.&lt;sujeto&gt;.tag</c> para la frase de «Fruta nueva» y <c>tint.&lt;sujeto&gt;.name</c> para el nombre visible; el
    /// <c>fact_id</c> de <c>af_river_fact_shown</c> es <c>&lt;sujeto&gt;.&lt;kk&gt;</c> (08 §8.2). Todas se preparan al crearla,
    /// para que consultarlas no asigne memoria. Inmutable.
    /// </summary>
    public sealed class FactSeries
    {
        private readonly string[] _keys;
        private readonly string[] _factIds;

        /// <summary>Crea la serie (el lector ya la validó).</summary>
        public FactSeries(string subject, FactSeriesKind kind, int fromLevel, int lines, int step)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (fromLevel < 1 || lines < 1 || lines > FactCatalog.MaxLines || step < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(lines), "Serie fuera de rango: desde " + fromLevel.ToString(CultureInfo.InvariantCulture)
                    + ", " + lines.ToString(CultureInfo.InvariantCulture) + " líneas, paso " + step.ToString(CultureInfo.InvariantCulture) + ".");
            }

            Subject = subject;
            Kind = kind;
            FromLevel = fromLevel;
            Lines = lines;
            Step = step;
            TagKey = FactCatalog.KeyPrefix + subject + ".tag";
            NameKey = FactCatalog.NamePrefix + subject + ".name";
            _keys = new string[lines];
            _factIds = new string[lines];
            for (int k = 1; k <= lines; k++)
            {
                string kk = k.ToString("00", CultureInfo.InvariantCulture);
                _factIds[k - 1] = subject + "." + kk;
                _keys[k - 1] = FactCatalog.KeyPrefix + _factIds[k - 1];
            }
        }

        /// <summary>Id del sujeto: el del tinte en el glosario §9 (<c>acai</c>, <c>urucum</c>, <c>agua_negro</c>…).</summary>
        public string Subject { get; }

        /// <summary>Tipo de sujeto.</summary>
        public FactSeriesKind Kind { get; }

        /// <summary>Nivel de la línea 1.</summary>
        public int FromLevel { get; }

        /// <summary>Número de líneas (20 por fruta, D-017 Q2-A).</summary>
        public int Lines { get; }

        /// <summary>Niveles entre dos líneas (1 = una por nivel; 2 = una cada dos niveles).</summary>
        public int Step { get; }

        /// <summary>Nivel de la última línea.</summary>
        public int LastLevel => FromLevel + ((Lines - 1) * Step);

        /// <summary>Primer nivel después del bloque de la serie (<c>from_level + lines × step</c>, 08 §4.6).</summary>
        public int EndLevel => FromLevel + (Lines * Step);

        /// <summary>Clave de la frase de la tarjeta «Fruta nueva» (<c>fact.&lt;sujeto&gt;.tag</c>).</summary>
        public string TagKey { get; }

        /// <summary>Clave del nombre visible del tinte (<c>tint.&lt;sujeto&gt;.name</c>).</summary>
        public string NameKey { get; }

        /// <summary>Clave de texto de la línea <paramref name="k"/> (1..<see cref="Lines"/>): <c>fact.&lt;sujeto&gt;.&lt;kk&gt;</c>.</summary>
        public string KeyAt(int k)
        {
            return _keys[CheckK(k)];
        }

        /// <summary><c>fact_id</c> de la línea <paramref name="k"/> en <c>af_river_fact_shown</c>: <c>&lt;sujeto&gt;.&lt;kk&gt;</c>.</summary>
        public string FactIdAt(int k)
        {
            return _factIds[CheckK(k)];
        }

        /// <summary>Nivel en que sale la línea <paramref name="k"/>.</summary>
        public int LevelAt(int k)
        {
            return FromLevel + (CheckK(k) * Step);
        }

        private int CheckK(int k)
        {
            if (k < 1 || k > Lines)
            {
                throw new ArgumentOutOfRangeException(nameof(k), k, "La serie " + Subject + " tiene " + Lines.ToString(CultureInfo.InvariantCulture) + " líneas.");
            }

            return k - 1;
        }
    }

    /// <summary>
    /// Calendario de Frutas del río <c>af-facts/1</c> (D-017; 08 §4.6; contrato 31; <c>Data/Content/frutas_del_rio_v1.json</c>):
    /// las series por orden de nivel, sin solaparse, y el catálogo de fuentes. Solo claves y números: los textos van en
    /// <c>facts.csv</c> (contrato 32). Lo lee <see cref="FactCatalogReader"/>; qué línea sale en cada nivel lo decide
    /// <c>AF.GameFlow.FactSchedule</c>. Inmutable.
    /// </summary>
    public sealed class FactCatalog
    {
        /// <summary>Valor de <c>format</c> que lee este cliente.</summary>
        public const string Format = "af-facts/1";

        /// <summary>Prefijo de las claves de texto de las líneas y de la frase de «Fruta nueva» (08 §4.7).</summary>
        public const string KeyPrefix = "fact.";

        /// <summary>Prefijo de la clave del nombre visible del tinte (08 §4.7).</summary>
        public const string NamePrefix = "tint.";

        /// <summary>Tope de líneas de una serie: la clave lleva k con dos cifras (<c>kk</c>).</summary>
        public const int MaxLines = 99;

        private readonly FactSeries[] _series;
        private readonly FactSource[] _sources;
        private readonly Dictionary<string, FactSource> _sourceById;
        private readonly Dictionary<string, FactSeries> _seriesBySubject;

        /// <summary>Crea el calendario (el lector ya lo validó: series ordenadas por nivel y sin solaparse).</summary>
        public FactCatalog(int version, int maxCharsEs, IReadOnlyList<FactSeries> series, IReadOnlyList<FactSource> sources)
        {
            if (series == null)
            {
                throw new ArgumentNullException(nameof(series));
            }

            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            Version = version;
            MaxCharsEs = maxCharsEs;
            _series = new FactSeries[series.Count];
            _seriesBySubject = new Dictionary<string, FactSeries>(StringComparer.Ordinal);
            for (int i = 0; i < _series.Length; i++)
            {
                _series[i] = series[i];
                _seriesBySubject.Add(series[i].Subject, series[i]);
            }

            _sources = new FactSource[sources.Count];
            _sourceById = new Dictionary<string, FactSource>(StringComparer.Ordinal);
            for (int i = 0; i < _sources.Length; i++)
            {
                _sources[i] = sources[i];
                _sourceById.Add(sources[i].Id, sources[i]);
            }
        }

        /// <summary>Versión del calendario (sube cuando el asesor cambia una línea: un <c>fact_id</c> nunca se reutiliza).</summary>
        public int Version { get; }

        /// <summary>Límite de caracteres de una línea en español (<c>max_chars_es</c>, 90).</summary>
        public int MaxCharsEs { get; }

        /// <summary>Series en orden de nivel.</summary>
        public IReadOnlyList<FactSeries> Series => _series;

        /// <summary>Fuentes en el orden del archivo.</summary>
        public IReadOnlyList<FactSource> Sources => _sources;

        /// <summary>La serie del sujeto <paramref name="subject"/>, o null.</summary>
        public FactSeries? SeriesOf(string subject)
        {
            return subject != null && _seriesBySubject.TryGetValue(subject, out FactSeries? s) ? s : null;
        }

        /// <summary>La fuente <paramref name="id"/>, o null.</summary>
        public FactSource? Source(string id)
        {
            return id != null && _sourceById.TryGetValue(id, out FactSource? s) ? s : null;
        }
    }
}
