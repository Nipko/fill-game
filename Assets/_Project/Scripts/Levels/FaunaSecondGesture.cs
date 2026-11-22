#nullable enable
using System;

namespace AF.Levels
{
    /// <summary>
    /// Segundo gesto de la especie (F13 de la propuesta de fauna, decidida en D-029 para la 1.1; añadido de D-029 del
    /// 2026-11-08): lo que hace toda la tropa en el reposo de ≈ 1,5 s tras la última suelta (D-011, punto 3), desde la mitad de
    /// su primer mundo largo (6-11), en lugar de los gestos de reposo de siempre. Sin arte nuevo de especie (D-052): las mismas
    /// piezas y otra pose. Campo <c>segundo_gesto</c> del catálogo <c>af-fauna/0</c> [P]. Gestos de animal, sin rasgos humanos
    /// (D-010, punto 6) y sin mirar ni señalar una cuia (D-002).
    /// </summary>
    public enum FaunaSecondGesture
    {
        /// <summary>Sin segundo gesto (especies de los mundos 1-5, 12 y 13).</summary>
        None = 0,

        /// <summary>
        /// <c>duo</c>: la pareja canta su dúo con las colas trenzadas: las dos cabezas suben a la vez y el pecho late al mismo
        /// compás (los titíes cantan a dúo sentados lado a lado [E]).
        /// </summary>
        Duet = 1,

        /// <summary><c>brincos</c>: cada miquito da dos brincos en su sitio de la rama, en ola de uno a otro (los calitrícidos saltan).</summary>
        Hops = 2,

        /// <summary>
        /// <c>colgado_de_la_cola</c>: suelta la mano de la liana, cuelga solo de la cola prensil con los brazos larguísimos
        /// abiertos y se mece más (los atélidos cuelgan de la cola con las manos libres).
        /// </summary>
        TailHang = 3,

        /// <summary><c>desperezo</c>: se despereza estirándose a lo largo de la rama, de cara a la noche que empieza (mono nocturno).</summary>
        Stretch = 4,

        /// <summary><c>vuelo</c>: un salto largo por la rama y vuelta a su sitio (el «mono volador»: más del 70 % de sus desplazamientos son saltos).</summary>
        Leap = 5,
    }

    /// <summary>
    /// Cuándo sale el segundo gesto (F13; añadido de D-029 del 2026-11-08) [P]: desde la mitad del primer mundo largo (6-11) de la
    /// especie (<see cref="MidLevel"/>: 251, 361, 491, 631, 761 y 871) hasta el final de su tramo, en niveles alternos con el
    /// primero: el nivel de la mitad lo estrena y, desde ahí, uno sí y uno no. Determinista por el número de nivel (sin azar ni
    /// hora). Sin asignaciones.
    /// </summary>
    public static class FaunaGestureRule
    {
        /// <summary>Primer mundo largo (glosario §8: 201-300).</summary>
        public const int FirstLongWorld = 6;

        /// <summary>Último mundo largo (821-920).</summary>
        public const int LastLongWorld = 11;

        /// <summary>Valores de <c>segundo_gesto</c> en el catálogo, en el orden de <see cref="FaunaSecondGesture"/> (el 0, sin gesto).</summary>
        private static readonly string[] Names = { string.Empty, "duo", "brincos", "colgado_de_la_cola", "desperezo", "vuelo" };

        /// <summary>El mundo es largo (6-11).</summary>
        public static bool IsLongWorld(int world)
        {
            return world >= FirstLongWorld && world <= LastLongWorld;
        }

        /// <summary>
        /// Nivel de la mitad del mundo <paramref name="world"/> (1..13): el primero de su segunda mitad, primer nivel + la mitad de
        /// los que tiene (201-300 → 251; 301-420 → 361; 421-560 → 491; 561-700 → 631; 701-820 → 761; 821-920 → 871).
        /// </summary>
        public static int MidLevel(int world)
        {
            int first = WorldTable.FirstLevel(world);
            return first + ((WorldTable.LastLevelOf(world) - first + 1) / 2);
        }

        /// <summary>
        /// Primer nivel del segundo gesto de una especie que vive en <paramref name="worlds"/>: la mitad de su primer mundo largo; 0
        /// si no vive en ninguno.
        /// </summary>
        public static int FromLevel(int[] worlds)
        {
            int best = 0;
            for (int i = 0; i < worlds.Length; i++)
            {
                if (IsLongWorld(worlds[i]))
                {
                    int mid = MidLevel(worlds[i]);
                    best = best == 0 ? mid : Math.Min(best, mid);
                }
            }

            return best;
        }

        /// <summary>
        /// El nivel <paramref name="level"/> lleva el segundo gesto de <paramref name="species"/>: tiene uno, el nivel está entre
        /// <see cref="FaunaSpecies.SecondGestureFrom"/> y el final del tramo, y le toca en la rotación (el estreno y, desde él, los
        /// alternos).
        /// </summary>
        public static bool UsesSecond(FaunaSpecies? species, int level)
        {
            if (species == null || species.SecondGesture == FaunaSecondGesture.None)
            {
                return false;
            }

            int from = species.SecondGestureFrom;
            return from > 0 && level >= from && level <= species.ToLevel && ((level - from) % 2) == 0;
        }

        /// <summary>
        /// Como <see cref="UsesSecond(FaunaSpecies?, int)"/> con el extra de desarrollo <c>af_troop_gesture</c>
        /// (<paramref name="forced"/>: 1 el primero, 2 el segundo si la especie lo tiene; 0 la regla).
        /// </summary>
        public static bool UsesSecond(FaunaSpecies? species, int level, int forced)
        {
            if (forced == 1)
            {
                return false;
            }

            if (forced == 2)
            {
                return species != null && species.SecondGesture != FaunaSecondGesture.None;
            }

            return UsesSecond(species, level);
        }

        /// <summary>Nombre del gesto en el catálogo y en la sonda (<c>duo</c>, <c>brincos</c>…; vacío sin gesto).</summary>
        public static string NameOf(FaunaSecondGesture gesture)
        {
            int i = (int)gesture;
            return i >= 0 && i < Names.Length ? Names[i] : string.Empty;
        }

        /// <summary>Gesto con el nombre <paramref name="name"/> del catálogo; false si no existe.</summary>
        public static bool TryParse(string name, out FaunaSecondGesture gesture)
        {
            for (int i = 1; i < Names.Length; i++)
            {
                if (string.Equals(Names[i], name, StringComparison.Ordinal))
                {
                    gesture = (FaunaSecondGesture)i;
                    return true;
                }
            }

            gesture = FaunaSecondGesture.None;
            return false;
        }
    }
}
