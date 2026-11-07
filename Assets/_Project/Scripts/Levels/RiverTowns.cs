#nullable enable
using System;

namespace AF.Levels
{
    /// <summary>
    /// Viñeta por código de un pueblo de paso (<c>passing[].vignette</c> de <c>af-river-map/2</c>; D-069, M2-2; propuesta del Mapa v2
    /// §4.2). <see cref="Landmark"/> no sale de los datos: es la ciudad con hito de las paradas con héroe. Nunca una maloca.
    /// </summary>
    public enum RiverTownVignette
    {
        /// <summary>Sin viñeta: una boca o unos rápidos (el punto de la 1.0).</summary>
        None = 0,

        /// <summary>Muelle y palafitos (<c>stilts</c>).</summary>
        Stilts = 1,

        /// <summary>Iglesia y plaza (<c>church</c>).</summary>
        Church = 2,

        /// <summary>Puerto con barco de línea (<c>port</c>).</summary>
        Port = 3,

        /// <summary>Mercado del muelle (<c>market</c>; nunca «mercado flotante»).</summary>
        Market = 4,

        /// <summary>Ciudad con hito: solo en las paradas que la tienen (no en los datos de <c>passing[]</c>).</summary>
        Landmark = 5,
    }

    /// <summary>Tier de un punto de paso (<c>passing[].tier</c>; D-069, M2-2) [P].</summary>
    public enum RiverTownTier
    {
        /// <summary>Una boca o unos rápidos (<c>mark</c>), sin viñeta.</summary>
        Mark = 0,

        /// <summary>Pueblo (<c>town</c>).</summary>
        Town = 1,

        /// <summary>Ciudad (<c>city</c>: los puertos grandes de la propuesta §4.2).</summary>
        City = 2,
    }

    /// <summary>
    /// Excursión local de un tramo de 0 km (<c>leg.excursion</c> de <c>af-river-stops/2</c>; D-069, P5 (a); propuesta del Mapa v2
    /// §4.4) [P]: la canoa sale de la parada (<see cref="S0"/>, su <c>at</c>) por <see cref="Line"/> hasta <see cref="S1"/> y vuelve
    /// por la otra mitad del cauce (un lazo sobre el agua, <see cref="RiverTowns.ExcursionPoint"/>). Inmutable.
    /// </summary>
    public sealed class RiverExcursion
    {
        /// <summary>Crea la excursión (el lector ya la validó).</summary>
        public RiverExcursion(int line, float s0, float s1)
        {
            Line = line;
            S0 = s0;
            S1 = s1;
        }

        /// <summary>Índice de la línea.</summary>
        public int Line { get; }

        /// <summary>Donde sale y vuelve (la parada).</summary>
        public float S0 { get; }

        /// <summary>Donde da la vuelta.</summary>
        public float S1 { get; }
    }

    /// <summary>
    /// Reglas sin motor de los pueblos de paso (D-069, M2-2; propuesta del Mapa v2 §4.1, §4.4 y §9.5): cuándo se pasa un pueblo (de
    /// <c>max_level</c>, sin campo nuevo en el guardado), qué rótulos salen (para evitar ruido) y dónde va la canoa en la excursión
    /// local. Sin asignaciones.
    /// </summary>
    public static class RiverTowns
    {
        /// <summary>Ancho del lazo de la excursión, en fracción de la distancia de la parada a la vuelta [P].</summary>
        public const float ExcursionLoopWidth = 0.22f;

        /// <summary>
        /// Nivel con cuya victoria se pasa un pueblo a la fracción <paramref name="f"/> del tramo de la parada <paramref name="n"/>
        /// (propuesta §4.1, regla 3): <c>10(n − 1) + ⌈10 f⌉</c>, al menos el primero del tramo.
        /// </summary>
        public static int PassLevel(int n, float f)
        {
            int step = (int)Math.Ceiling((10.0 * f) - 1e-6);
            step = step < 1 ? 1 : step > 10 ? 10 : step;
            return (RiverStopCatalog.LevelsPerStop * (n - 1)) + step;
        }

        /// <summary>El pueblo del tramo <paramref name="n"/> a la fracción <paramref name="f"/> ya se pasó con <paramref name="maxLevel"/>.</summary>
        public static bool Passed(int n, float f, int maxLevel)
        {
            return maxLevel >= PassLevel(n, f);
        }

        /// <summary>
        /// Tramo en que está la canoa con <paramref name="maxLevel"/> niveles ganados: el de la parada siguiente mientras se juega
        /// (<c>max / 10 + 1</c>) o, justo al llegar a la parada k (<c>max = 10k</c>), el k.
        /// </summary>
        public static int CanoeLeg(int maxLevel)
        {
            int max = maxLevel < 0 ? 0 : maxLevel;
            int arrived = max / RiverStopCatalog.LevelsPerStop;
            return max % RiverStopCatalog.LevelsPerStop == 0 ? arrived : arrived + 1;
        }

        /// <summary>
        /// Nivel de detalle de los rótulos (propuesta §9.5): un pueblo del tramo <paramref name="townLeg"/> lleva rótulo si está a un
        /// tramo o menos de la canoa (<see cref="CanoeLeg"/>); los demás, solo su viñeta.
        /// </summary>
        public static bool TownLabelShown(int townLeg, int maxLevel)
        {
            int d = townLeg - CanoeLeg(maxLevel);
            return d >= -1 && d <= 1;
        }

        /// <summary>
        /// Rótulo de una parada (propuesta §9.5): solo la última alcanzada (<c>max / 10</c>) y la siguiente.
        /// </summary>
        public static bool StopLabelShown(int stop, int maxLevel)
        {
            int last = (maxLevel < 0 ? 0 : maxLevel) / RiverStopCatalog.LevelsPerStop;
            return stop == last || stop == last + 1;
        }

        /// <summary>
        /// Punto de la excursión <paramref name="excursion"/> en la fracción <paramref name="u"/> (0-1) de la vuelta, en unidades del
        /// marco: por el eje de su línea de S0 a S1 y vuelta (<c>(1 − cos 2πu) / 2</c>), apartado del eje por la normal un
        /// <c>sin 2πu</c> de <see cref="ExcursionLoopWidth"/> × la distancia de la parada a la vuelta: a la ida por una orilla y a la
        /// vuelta por la otra; empieza y acaba en la parada. Sin asignaciones.
        /// </summary>
        public static void ExcursionPoint(RiverMap map, RiverExcursion excursion, float u, out float x, out float y)
        {
            RiverLine line = map.Lines[excursion.Line];
            float t = u < 0f ? 0f : u > 1f ? 1f : u;
            double phase = 2.0 * Math.PI * t;
            float along = (float)((1.0 - Math.Cos(phase)) * 0.5);
            float s = excursion.S0 + ((excursion.S1 - excursion.S0) * along);
            line.PointAt(s, out x, out y);
            line.PointAt(excursion.S0, out float ax, out float ay);
            line.PointAt(excursion.S1, out float bx, out float by);
            float chord = (float)Math.Sqrt(((bx - ax) * (bx - ax)) + ((by - ay) * (by - ay)));
            float eps = Math.Abs(excursion.S1 - excursion.S0) * 0.02f;
            if (eps <= 0f || chord <= 0f)
            {
                return;
            }

            float dir = excursion.S1 >= excursion.S0 ? 1f : -1f;
            line.PointAt(s + (dir * eps), out float px, out float py);
            line.PointAt(s - (dir * eps), out float qx, out float qy);
            float tx = px - qx;
            float ty = py - qy;
            float len = (float)Math.Sqrt((tx * tx) + (ty * ty));
            if (len <= 0f)
            {
                return;
            }

            float off = (float)Math.Sin(phase) * ExcursionLoopWidth * chord;
            x += -ty / len * off;
            y += tx / len * off;
        }
    }
}
