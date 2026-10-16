#nullable enable
using System;
using System.Collections.Generic;
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>
    /// Posición de un nivel en el calendario de Frutas del río (<see cref="FactSchedule.Progress"/>): la serie, la línea k de
    /// <see cref="Lines"/> («Açaí · 3/20», <c>fact_progress</c>) y sus claves ya preparadas. Sin serie (<see cref="IsValid"/>
    /// falso) si el nivel no lleva línea. Valor puro, sin texto: los textos los da <c>Loc.Fact</c> con <see cref="Key"/>.
    /// </summary>
    public readonly struct FactPosition
    {
        /// <summary>
        /// Crea la posición. <paramref name="k"/> va de 1 a las líneas de la serie: fuera de ese rango lanza
        /// <see cref="ArgumentOutOfRangeException"/> aquí, y no más tarde al leer <see cref="Key"/> o <see cref="FactId"/>.
        /// </summary>
        public FactPosition(FactSeries series, int k)
        {
            Series = series ?? throw new ArgumentNullException(nameof(series));
            if (k < 1 || k > series.Lines)
            {
                throw new ArgumentOutOfRangeException(nameof(k), k, "La línea k va de 1 a las líneas de la serie.");
            }

            K = k;
        }

        /// <summary>La serie, o null si el nivel no lleva línea.</summary>
        public FactSeries? Series { get; }

        /// <summary>Si el nivel lleva línea.</summary>
        public bool IsValid => Series != null;

        /// <summary>Línea k (1..<see cref="Lines"/>), o 0 sin serie.</summary>
        public int K { get; }

        /// <summary>Líneas de la serie (20), o 0 sin serie.</summary>
        public int Lines => Series?.Lines ?? 0;

        /// <summary>Última línea de la serie: el contador k/20 pasa a sello (D-017, Q5-A).</summary>
        public bool IsLast => Series != null && K == Series.Lines;

        /// <summary>Clave de texto de la línea (<c>fact.&lt;tinte&gt;.&lt;kk&gt;</c>), o null.</summary>
        public string? Key => Series?.KeyAt(K);

        /// <summary><c>fact_id</c> de <c>af_river_fact_shown</c> (<c>&lt;tinte&gt;.&lt;kk&gt;</c>), o null.</summary>
        public string? FactId => Series?.FactIdAt(K);

        /// <summary>Clave del nombre visible del tinte (<c>tint.&lt;tinte&gt;.name</c>), o null.</summary>
        public string? NameKey => Series?.NameKey;
    }

    /// <summary>
    /// Qué línea de Frutas del río toca en cada nivel y cuáles están reveladas (D-017; 08 §4.6; propuesta de Frutas §6.4),
    /// sin motor, sin azar, sin reloj, sin estado nuevo en el perfil y sin asignaciones por consulta:
    /// <list type="bullet">
    /// <item><description><see cref="KeyFor"/> (pasos 1 y 2 de §6.4): la serie con <c>from_level ≤ n &lt; from_level + lines ×
    /// step</c> (búsqueda binaria en las series ordenadas); con <c>d = n − from_level</c>, si <c>d</c> no es múltiplo de
    /// <c>step</c> no hay línea y, si lo es, <c>k = d / step + 1</c>. La cadencia de Remote Config
    /// (<c>ui_fruit_line_every_n_levels</c>, paso 3) y la prioridad de la victoria (TiTino &gt; llegada &gt; fruta, D-022) no son de
    /// aquí: las decide <see cref="VictoryLineArbiter"/>.</description></item>
    /// <item><description><see cref="IsRevealed"/> (paso 4): una línea está revelada si su nivel es ≤ <c>progress.max_level</c>
    /// (el más alto superado) o está en <c>progress.skipped_levels</c> (Salto del Boto, 04 §2.6: la línea no sale en ninguna
    /// victoria, pero la ficha la muestra). Una línea desplazada por otra de más prioridad también queda revelada.</description></item>
    /// </list>
    /// Que la clave tenga texto en el idioma del jugador (paso 5) lo decide <c>Loc.Fact</c>: si da «», no hay línea. Inmutable.
    /// </summary>
    public sealed class FactSchedule
    {
        private readonly FactSeries[] _series;
        private readonly int[] _from;
        private readonly Dictionary<string, int> _levelOfKey;

        /// <summary>Prepara el calendario de <paramref name="catalog"/> (ya validado por <see cref="FactCatalogReader"/>).</summary>
        public FactSchedule(FactCatalog catalog)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _series = new FactSeries[catalog.Series.Count];
            _from = new int[_series.Length];
            _levelOfKey = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < _series.Length; i++)
            {
                FactSeries s = catalog.Series[i];
                if (i > 0 && s.FromLevel < _series[i - 1].EndLevel)
                {
                    throw new ArgumentException("Las series del calendario van en orden de nivel y sin solaparse (" + s.Subject + ").", nameof(catalog));
                }

                _series[i] = s;
                _from[i] = s.FromLevel;
                for (int k = 1; k <= s.Lines; k++)
                {
                    _levelOfKey.Add(s.KeyAt(k), s.LevelAt(k));
                }
            }
        }

        /// <summary>El calendario.</summary>
        public FactCatalog Catalog { get; }

        /// <summary>
        /// Clave de texto de la línea del nivel <paramref name="level"/> (<c>fact.&lt;tinte&gt;.&lt;kk&gt;</c>, pregunta 49 (a)), o
        /// null si el nivel no lleva línea (fuera de toda serie, como 261-300 hasta C1a, o entre dos líneas con step &gt; 1). Sin
        /// asignaciones.
        /// </summary>
        public string? KeyFor(int level)
        {
            FactPosition p = Progress(level);
            return p.Series?.KeyAt(p.K);
        }

        /// <summary><c>fact_id</c> de la línea del nivel (<c>&lt;tinte&gt;.&lt;kk&gt;</c>; 08 §8.2), o null. Sin asignaciones.</summary>
        public string? FactIdFor(int level)
        {
            FactPosition p = Progress(level);
            return p.Series?.FactIdAt(p.K);
        }

        /// <summary>Serie y línea k del nivel (sin serie si no lleva línea). Sin asignaciones.</summary>
        public FactPosition Progress(int level)
        {
            int i = SeriesIndexOf(level);
            if (i < 0)
            {
                return default;
            }

            FactSeries s = _series[i];
            int d = level - s.FromLevel;
            if (d % s.Step != 0)
            {
                return default;
            }

            return new FactPosition(s, (d / s.Step) + 1);
        }

        /// <summary>Nivel en que sale la línea <paramref name="key"/>, o 0 si no es una línea del calendario. Sin asignaciones.</summary>
        public int LevelOf(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _levelOfKey.TryGetValue(key, out int level) ? level : 0;
        }

        /// <summary>
        /// Si la línea <paramref name="key"/> está revelada (ficha; §6.4, paso 4) con <paramref name="maxLevel"/> =
        /// <c>progress.max_level</c> y <paramref name="skipped"/> = <c>progress.skipped_levels</c> (null = ninguno). Falso si la
        /// clave no es una línea del calendario. Sin asignaciones.
        /// </summary>
        public bool IsRevealed(string key, int maxLevel, IReadOnlyList<int>? skipped)
        {
            return IsLevelRevealed(LevelOf(key), maxLevel, skipped);
        }

        /// <summary>Como <see cref="IsRevealed"/>, con el nivel de la línea (≤ 0 = ninguna). Sin asignaciones.</summary>
        public static bool IsLevelRevealed(int level, int maxLevel, IReadOnlyList<int>? skipped)
        {
            if (level <= 0)
            {
                return false;
            }

            if (level <= maxLevel)
            {
                return true;
            }

            if (skipped == null)
            {
                return false;
            }

            // Índice y no foreach: recorrer una interfaz con foreach asigna su enumerador.
            for (int i = 0; i < skipped.Count; i++)
            {
                if (skipped[i] == level)
                {
                    return true;
                }
            }

            return false;
        }

        // Última serie con from_level ≤ level, si el nivel cae en su bloque [from_level, from_level + lines × step); si no, -1.
        private int SeriesIndexOf(int level)
        {
            int lo = 0;
            int hi = _from.Length - 1;
            int found = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                if (_from[mid] <= level)
                {
                    found = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return found >= 0 && level < _series[found].EndLevel ? found : -1;
        }
    }
}
