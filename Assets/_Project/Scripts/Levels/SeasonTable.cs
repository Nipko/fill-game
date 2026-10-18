#nullable enable
using System;

namespace AF.Levels
{
    /// <summary>
    /// Temporada de un ciclo de 20 niveles (D-009, punto 6; Q11 A de D-007; 03 §12): aguas altas (A) o aguas bajas (B). Solo
    /// cambia la LUT del fondo, sin cartel (07 §2.1). <see cref="None"/>: el nivel no tiene temporada (1-20, fuera de la tabla
    /// o sin tabla), y el fondo queda como hoy.
    /// </summary>
    public enum Season
    {
        /// <summary>Sin temporada (1-20, fuera de la tabla o sin tabla).</summary>
        None = 0,

        /// <summary>A: aguas altas.</summary>
        HighWater = 1,

        /// <summary>B: aguas bajas.</summary>
        LowWater = 2,
    }

    /// <summary>
    /// Tabla de temporada del cliente <c>af-seasons/1</c> (<c>Assets/_Project/Data/Levels/seasons_v1.json</c>, generada por
    /// <c>tools/level-generator --seasons</c>, tarea SE1): la temporada de cada ciclo de 20, copiada del calendario versionado
    /// (pregunta 32 de docs/GDD.md §6.3, decidida el 2026-10-13: (b), D-020; 03 §12; 08 «Preguntas abiertas» 12). No toca el
    /// formato del pack. Los ciclos van seguidos desde <see cref="FirstLevel"/> (≡ 1 mod 20) hasta <see cref="LastLevel"/>; hoy,
    /// 21-300, y P7 la amplía hasta el 700. La lee <see cref="SeasonTableReader"/>. <see cref="SeasonOf"/> no asigna memoria.
    /// </summary>
    public sealed class SeasonTable
    {
        /// <summary>Formato que lee el cliente.</summary>
        public const string Format = "af-seasons/1";

        /// <summary>Niveles de un ciclo (03 §12).</summary>
        public const int CycleLength = 20;

        private readonly Season[] _cycles;

        /// <summary>
        /// Crea la tabla con la temporada de cada ciclo, en orden, desde el ciclo que empieza en <paramref name="firstLevel"/>.
        /// </summary>
        /// <exception cref="ArgumentException">El primer ciclo no empieza en 1 mod 20, no hay ciclos o alguno no es A ni B.</exception>
        public SeasonTable(string source, int firstLevel, Season[] cycles)
        {
            if (cycles == null || cycles.Length == 0)
            {
                throw new ArgumentException("La tabla de temporada necesita al menos un ciclo.", nameof(cycles));
            }

            if (firstLevel < 1 || (firstLevel - 1) % CycleLength != 0)
            {
                throw new ArgumentException("El primer ciclo tiene que empezar en 1, 21, 41…", nameof(firstLevel));
            }

            for (int i = 0; i < cycles.Length; i++)
            {
                if (cycles[i] != Season.HighWater && cycles[i] != Season.LowWater)
                {
                    throw new ArgumentException("Cada ciclo es de aguas altas (A) o de aguas bajas (B).", nameof(cycles));
                }
            }

            Source = source ?? string.Empty;
            FirstLevel = firstLevel;
            _cycles = (Season[])cycles.Clone();
        }

        /// <summary>Las especificaciones de las que se copió (campo <c>source</c>).</summary>
        public string Source { get; }

        /// <summary>Primer nivel del primer ciclo (hoy, el 21).</summary>
        public int FirstLevel { get; }

        /// <summary>Último nivel del último ciclo.</summary>
        public int LastLevel => FirstLevel + (_cycles.Length * CycleLength) - 1;

        /// <summary>Ciclos de la tabla.</summary>
        public int CycleCount => _cycles.Length;

        /// <summary>Temporada del ciclo <paramref name="index"/> (0 = el que empieza en <see cref="FirstLevel"/>).</summary>
        public Season CycleAt(int index)
        {
            return index >= 0 && index < _cycles.Length ? _cycles[index] : Season.None;
        }

        /// <summary>
        /// Temporada del nivel <paramref name="level"/>: la de su ciclo de 20, o <see cref="Season.None"/> antes del primer ciclo
        /// (1-20) y después del último. Sin asignaciones.
        /// </summary>
        public Season SeasonOf(int level)
        {
            if (level < FirstLevel)
            {
                return Season.None;
            }

            int index = (level - FirstLevel) / CycleLength;
            return index < _cycles.Length ? _cycles[index] : Season.None;
        }

        /// <summary>Letra de la temporada en la tabla y en las sondas: <c>A</c>, <c>B</c> o null sin temporada.</summary>
        public static string? Letter(Season season)
        {
            switch (season)
            {
                case Season.HighWater:
                    return "A";
                case Season.LowWater:
                    return "B";
                default:
                    return null;
            }
        }
    }
}
