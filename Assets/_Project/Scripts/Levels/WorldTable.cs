#nullable enable
using System;

namespace AF.Levels
{
    /// <summary>
    /// Tramos de niveles de los 13 mundos en la primera vuelta (glosario §8: 1-10 Estuario, 11-40 Tapajós, 41-80 Manaos,
    /// 81-140 Trapecio, 141-200 Loreto, 201-300 Oriente, 301-420 Madre de Dios, 421-560 Madidi, 561-700 Ucayali, 701-820 Alto
    /// Negro, 821-920 Escudo de Guayana, 921-960 Invitados del Orinoco y 961-1.000 El origen). Los rangos son hipótesis [E] del
    /// glosario: si cambian, se cambian aquí y en el catálogo de fauna a la vez (el lector los contrasta). Sin asignaciones.
    /// </summary>
    public static class WorldTable
    {
        /// <summary>Mundos de la primera vuelta.</summary>
        public const int WorldCount = 13;

        /// <summary>Último nivel de la primera vuelta.</summary>
        public const int LastLevel = 1000;

        // Primer nivel de cada mundo (índice 0 = mundo 1).
        private static readonly int[] FirstLevels = { 1, 11, 41, 81, 141, 201, 301, 421, 561, 701, 821, 921, 961 };

        /// <summary>Primer nivel del mundo <paramref name="world"/> (1..13).</summary>
        /// <exception cref="ArgumentOutOfRangeException">El mundo no existe.</exception>
        public static int FirstLevel(int world)
        {
            if (world < 1 || world > WorldCount)
            {
                throw new ArgumentOutOfRangeException(nameof(world), world, "Los mundos van de 1 a 13 (glosario §8).");
            }

            return FirstLevels[world - 1];
        }

        /// <summary>Último nivel del mundo <paramref name="world"/> (1..13).</summary>
        public static int LastLevelOf(int world)
        {
            return world >= WorldCount ? LastLevel : FirstLevel(world + 1) - 1;
        }

        /// <summary>Mundo del nivel <paramref name="level"/> (1..1.000) en la primera vuelta.</summary>
        /// <exception cref="ArgumentOutOfRangeException">El nivel no está en 1..1.000.</exception>
        public static int WorldOf(int level)
        {
            if (level < 1 || level > LastLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "La primera vuelta va del nivel 1 al 1.000 (glosario §8).");
            }

            int world = 1;
            for (int w = 1; w < WorldCount; w++)
            {
                if (level >= FirstLevels[w])
                {
                    world = w + 1;
                }
            }

            return world;
        }
    }
}
