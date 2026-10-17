#nullable enable
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>
    /// En qué niveles sale el banner de entrada a un mundo (07 §2.2, regla transversal 1; 02 §6; glosario §8 y §10, clave
    /// <c>world_banner</c>): «Mundo {n}: {nombre} · {país} · {río}», presentado por Capi, al cargar el primer nivel de cada mundo
    /// desde el 2 (11, 41, 81, 141 y, con el mundo 6 publicado desde C1a, 201, «Mundo 6: Oriente · Ecuador · Napo + lagunas
    /// negras de Cuyabeno»; después 301, 421…, según <see cref="WorldTable"/>), antes de la intro y solo en
    /// el primer intento. El nivel 1 no lo lleva (decisión del dueño del 2026-10-11: allí el primer contacto es el tutorial,
    /// 06 §2.1 regla 3). Regla pura, sin asignaciones; quién la aplica y cuándo, en <c>LevelFlow.Cards.cs</c>.
    /// </summary>
    public static class WorldBanners
    {
        /// <summary>Primer mundo con banner: el 2 (el 1 empieza con el tutorial).</summary>
        public const int FirstBannerWorld = 2;

        /// <summary>
        /// Mundo cuyo banner sale al cargar el nivel <paramref name="level"/> (primer nivel de un mundo ≥ 2 de la primera vuelta,
        /// 1-1.000), o 0 si ese nivel no lleva banner.
        /// </summary>
        public static int WorldAt(int level)
        {
            if (level <= 1 || level > WorldTable.LastLevel)
            {
                return 0;
            }

            int world = WorldTable.WorldOf(level);
            return world >= FirstBannerWorld && WorldTable.FirstLevel(world) == level ? world : 0;
        }

        /// <summary>El nivel <paramref name="level"/> lleva banner de mundo (el primero de un mundo desde el 2).</summary>
        public static bool ShowsOn(int level)
        {
            return WorldAt(level) != 0;
        }
    }
}
