#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AF.Levels
{
    /// <summary>Un hueco del catálogo: los niveles <see cref="From"/>-<see cref="To"/> (ambos incluidos) no están en ningún paquete.</summary>
    public readonly struct LevelGap
    {
        /// <summary>Crea el hueco.</summary>
        public LevelGap(int from, int to)
        {
            From = from;
            To = to;
        }

        /// <summary>Primer nivel que falta.</summary>
        public int From { get; }

        /// <summary>Último nivel que falta.</summary>
        public int To { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return From == To
                ? From.ToString(CultureInfo.InvariantCulture)
                : From.ToString(CultureInfo.InvariantCulture) + "-" + To.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Catálogo de niveles de campaña: el índice nivel → paquete de GDD 08 §2.2 (<c>AF.Levels</c>) sobre uno o varios
    /// <see cref="LevelPack"/> (hoy <c>tutorial_v0</c>; en la integración, también <c>mundo_02</c> … <c>mundo_05</c>). Resuelve
    /// cada número de nivel con su definición y su paquete, dice cuál es el siguiente disponible y detecta huecos y
    /// repetidos. Si un nivel no está en el catálogo (después del último o en un hueco), quien juega ve «El río sigue
    /// creciendo» (08 §6.2; 06 §9).
    /// </summary>
    /// <remarks>
    /// <para><see cref="LevelOffset"/> (solo builds de desarrollo, extra <c>af_level_offset</c> [P], tarea B1-5) desplaza el
    /// número de campaña: con 30, el nivel 31 se juega con el tablero del nivel 1 del paquete. Sirve para probar las
    /// compuertas de 11, 22 y 31 con los tableros del tutorial. En producción vale 0.</para>
    /// <para>Inmutable. Las búsquedas son binarias y no asignan memoria.</para>
    /// </remarks>
    public sealed class LevelCatalog
    {
        /// <summary>Desplazamiento máximo admitido (cubre de sobra los 1.040 niveles de la vuelta 1, 06 §10).</summary>
        public const int MaxLevelOffset = 100000;

        private readonly LevelPack[] _packs;
        private readonly int[] _numbers;
        private readonly LevelDefinition[] _definitions;
        private readonly int[] _packOf;
        private readonly LevelGap[] _gaps;

        private LevelCatalog(LevelPack[] packs, int[] numbers, LevelDefinition[] definitions, int[] packOf, LevelGap[] gaps, int levelOffset)
        {
            _packs = packs;
            _numbers = numbers;
            _definitions = definitions;
            _packOf = packOf;
            _gaps = gaps;
            LevelOffset = levelOffset;
        }

        /// <summary>Desplazamiento del número de campaña respecto al de los paquetes (0 en producción).</summary>
        public int LevelOffset { get; }

        /// <summary>Paquetes, en el orden en que se dieron.</summary>
        public IReadOnlyList<LevelPack> Packs => _packs;

        /// <summary>Número de niveles.</summary>
        public int Count => _numbers.Length;

        /// <summary>Primer nivel de campaña disponible.</summary>
        public int FirstLevel => _numbers[0];

        /// <summary>Último nivel de campaña disponible.</summary>
        public int LastLevel => _numbers[_numbers.Length - 1];

        /// <summary>Huecos entre <see cref="FirstLevel"/> y <see cref="LastLevel"/>, de menor a mayor (vacío si no hay).</summary>
        public IReadOnlyList<LevelGap> Gaps => _gaps;

        /// <summary>Los niveles de <see cref="FirstLevel"/> a <see cref="LastLevel"/> están todos.</summary>
        public bool IsContiguous => _gaps.Length == 0;

        /// <summary>Agua base del primer nivel (07 §2.1): la del arte que se genera al arrancar.</summary>
        public string WaterBase => _packs[_packOf[0]].WaterBase;

        /// <summary>Catálogo de un solo paquete, sin desplazamiento.</summary>
        public static LevelCatalog FromPack(LevelPack pack)
        {
            if (pack == null)
            {
                throw new ArgumentNullException(nameof(pack));
            }

            return Create(new[] { pack });
        }

        /// <summary>
        /// Crea el catálogo con <paramref name="packs"/>. Un número de nivel repetido en dos paquetes (o dos veces en el
        /// mismo, que ya rechaza <see cref="LevelPackReader"/>) es un error de datos.
        /// </summary>
        /// <exception cref="ArgumentException">No hay paquetes o alguno es null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="levelOffset"/> fuera de 0..<see cref="MaxLevelOffset"/>.</exception>
        /// <exception cref="LevelFormatException">Un nivel está en dos paquetes.</exception>
        public static LevelCatalog Create(IReadOnlyList<LevelPack> packs, int levelOffset = 0)
        {
            if (packs == null || packs.Count == 0)
            {
                throw new ArgumentException("El catálogo necesita al menos un paquete.", nameof(packs));
            }

            if (levelOffset < 0 || levelOffset > MaxLevelOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(levelOffset), levelOffset, "Desplazamiento fuera de 0.." + MaxLevelOffset.ToString(CultureInfo.InvariantCulture) + ".");
            }

            var packArray = new LevelPack[packs.Count];
            int total = 0;
            for (int p = 0; p < packs.Count; p++)
            {
                packArray[p] = packs[p] ?? throw new ArgumentException("Un paquete es null.", nameof(packs));
                total += packArray[p].Count;
            }

            var numbers = new int[total];
            var entries = new int[total];
            var definitions = new LevelDefinition[total];
            var packOf = new int[total];
            int n = 0;
            for (int p = 0; p < packArray.Length; p++)
            {
                IReadOnlyList<LevelDefinition> levels = packArray[p].Levels;
                for (int i = 0; i < levels.Count; i++)
                {
                    numbers[n] = levels[i].Level + levelOffset;
                    entries[n] = n;
                    definitions[n] = levels[i];
                    packOf[n] = p;
                    n++;
                }
            }

            // Orden por número de campaña: entries lleva el índice original para reordenar definiciones y paquetes.
            Array.Sort(numbers, entries);
            var sortedDefinitions = new LevelDefinition[total];
            var sortedPackOf = new int[total];
            for (int i = 0; i < total; i++)
            {
                sortedDefinitions[i] = definitions[entries[i]];
                sortedPackOf[i] = packOf[entries[i]];
            }

            var gaps = new List<LevelGap>();
            for (int i = 1; i < total; i++)
            {
                if (numbers[i] == numbers[i - 1])
                {
                    throw new LevelFormatException("El nivel " + numbers[i].ToString(CultureInfo.InvariantCulture) + " está en los paquetes "
                        + packArray[sortedPackOf[i - 1]].PackId + " y " + packArray[sortedPackOf[i]].PackId + ".");
                }

                if (numbers[i] > numbers[i - 1] + 1)
                {
                    gaps.Add(new LevelGap(numbers[i - 1] + 1, numbers[i] - 1));
                }
            }

            return new LevelCatalog(packArray, numbers, sortedDefinitions, sortedPackOf, gaps.ToArray(), levelOffset);
        }

        /// <summary>El nivel de campaña <paramref name="level"/> está en el catálogo.</summary>
        public bool Contains(int level)
        {
            return IndexOf(level) >= 0;
        }

        /// <summary>Definición del nivel de campaña <paramref name="level"/>, o null si no está.</summary>
        public LevelDefinition? Find(int level)
        {
            int i = IndexOf(level);
            return i < 0 ? null : _definitions[i];
        }

        /// <summary>Paquete que contiene el nivel de campaña <paramref name="level"/>, o null si no está.</summary>
        public LevelPack? PackOf(int level)
        {
            int i = IndexOf(level);
            return i < 0 ? null : _packs[_packOf[i]];
        }

        /// <summary>Siguiente nivel de campaña (<paramref name="level"/> + 1) si está en el catálogo; si no, null (fin del contenido o hueco).</summary>
        public int? NextLevelAfter(int level)
        {
            if (level >= int.MaxValue)
            {
                return null;
            }

            int next = level + 1;
            return Contains(next) ? next : (int?)null;
        }

        /// <summary>Huecos como texto (<c>"3, 7-9"</c>) para el registro; vacío si no hay.</summary>
        public string DescribeGaps()
        {
            if (_gaps.Length == 0)
            {
                return string.Empty;
            }

            var parts = new string[_gaps.Length];
            for (int i = 0; i < _gaps.Length; i++)
            {
                parts[i] = _gaps[i].ToString();
            }

            return string.Join(", ", parts);
        }

        private int IndexOf(int level)
        {
            int i = Array.BinarySearch(_numbers, level);
            return i < 0 ? -1 : i;
        }
    }
}
