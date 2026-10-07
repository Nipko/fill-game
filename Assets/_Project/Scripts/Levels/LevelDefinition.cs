#nullable enable
using System.Collections.Generic;
using System.Globalization;
using AF.Core.Board;

namespace AF.Levels
{
    /// <summary>
    /// Un nivel del paquete (formato <c>af-level/1</c>, GDD 08 §4.1; subconjunto v0 de docs/dev/prototipo-v0.md, b):
    /// tablero inicial explícito (<c>caps</c> y <c>cuias</c> de abajo arriba con el índice de motor del glosario §9)
    /// y el par del pipeline. Las soluciones nunca viajan en el paquete (08 §4.1).
    /// </summary>
    public sealed class LevelDefinition
    {
        private readonly int[] _caps;
        private readonly int[][] _cuias;
        private readonly string[] _palette;

        internal LevelDefinition(
            string id,
            int level,
            int world,
            string waterBase,
            string[] palette,
            int unitsPerColor,
            int[] caps,
            int[][] cuias,
            int par)
        {
            Id = id;
            Level = level;
            World = world;
            WaterBase = waterBase;
            _palette = palette;
            UnitsPerColor = unitsPerColor;
            _caps = caps;
            _cuias = cuias;
            Par = par;
        }

        /// <summary>Identificador <c>m{mm}_v{v}_n{nnnn}</c> (glosario §11).</summary>
        public string Id { get; }

        /// <summary>Número de nivel de campaña (1..).</summary>
        public int Level { get; }

        /// <summary>Mundo (glosario §8).</summary>
        public int World { get; }

        /// <summary>Agua base del mundo: <c>black</c>, <c>white</c> o <c>clear</c> (glosario §11).</summary>
        public string WaterBase { get; }

        /// <summary>IDs de tinte de la paleta del nivel (glosario §9).</summary>
        public IReadOnlyList<string> Palette => _palette;

        /// <summary><c>U_c</c> común a todos los colores del nivel (02 R3).</summary>
        public int UnitsPerColor { get; }

        /// <summary>Par: vertidos de la solución óptima del pipeline (03 §5).</summary>
        public int Par { get; }

        /// <summary>Número de cuias T (02 R1).</summary>
        public int CuiaCount => _caps.Length;

        /// <summary>Capacidad de la cuia <paramref name="index"/>.</summary>
        public int CapacityOf(int index)
        {
            return _caps[index];
        }

        /// <summary>Dosis de la cuia <paramref name="index"/>, de abajo arriba (copia).</summary>
        public int[] CopyDoses(int index)
        {
            return (int[])_cuias[index].Clone();
        }

        /// <summary>Tablero inicial del núcleo (un estado nuevo en cada llamada).</summary>
        public BoardState CreateBoard()
        {
            var cuias = new Cuia[_caps.Length];
            for (int i = 0; i < _caps.Length; i++)
            {
                cuias[i] = _cuias[i].Length == 0 ? Cuia.Empty(_caps[i]) : Cuia.Create(_caps[i], _cuias[i]);
            }

            return new BoardState(cuias);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "LevelDefinition(" + Id + ", T=" + _caps.Length.ToString(CultureInfo.InvariantCulture)
                + ", par=" + Par.ToString(CultureInfo.InvariantCulture) + ")";
        }
    }
}
