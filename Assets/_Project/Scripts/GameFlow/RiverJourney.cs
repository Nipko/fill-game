#nullable enable
using System;
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>
    /// El viaje por el Mapa del río en funciones puras (08 §2.2; D-013; contrato común del plan del Mapa, §0): la parada
    /// k = (L − 1) / 10 cubre los niveles <c>10k+1</c> … <c>10k+10</c> y se llega a ella al ganar el <c>10k</c>. Solo lee
    /// <see cref="WorldTable"/> y los números que recibe (<c>progress.max_level</c>, <c>progress.map_seen_level</c> y el nivel
    /// en juego); lo pendiente de animar entre <c>map_seen_level</c> y <c>max_level</c> («ponerse al día», propuesta del Mapa
    /// §4.2) son <see cref="HasUnseenArrival"/>, <see cref="CatchUpFrom"/> y <see cref="CatchUpMs"/> (M3.1b). Sin estado, sin
    /// motor y sin asignaciones. Los niveles fuera de 1..1.000 lanzan <see cref="ArgumentOutOfRangeException"/>, como
    /// <see cref="WorldTable"/>.
    /// </summary>
    public static class RiverJourney
    {
        /// <summary>Guía del Mapa en todos los mundos salvo el 7 y el 8: Rosa (glosario §3.2).</summary>
        public const string GuideRosa = "rosa";

        /// <summary>Guía del Mapa en los mundos 7 y 8: el bufeo boliviano, sin nombre propio (D-013, Q18-A; 07 §6.2).</summary>
        public const string GuideBufeo = "bufeo";

        /// <summary>
        /// Boyas que enciende como mucho «ponerse al día» (propuesta del Mapa §4.2): las de un tramo, 10, que a
        /// <see cref="MapConfig.CatchUpPerBuoyMs"/> = 150 ms caben justas en <see cref="MapConfig.CatchUpMaxMs"/> = 1,5 s.
        /// Con un tramo entero, la ventana siempre incluye la última llegada sin ver.
        /// </summary>
        public const int CatchUpMaxBuoys = RiverStopCatalog.LevelsPerStop;

        private const int LevelsPerStop = RiverStopCatalog.LevelsPerStop;

        // Mundos en los que guía el bufeo boliviano (Q18-A): Rosa se queda en los rápidos de Teotônio y vuelve en el viaje del 560.
        private const int FirstBufeoWorld = 7;
        private const int LastBufeoWorld = 8;

        /// <summary>Parada del nivel <paramref name="level"/>: (L − 1) / 10 (0 para 1-10, 1 para 11-20 … 99 para 991-1.000).</summary>
        /// <exception cref="ArgumentOutOfRangeException">El nivel no está en 1..1.000.</exception>
        public static int StopIndexOf(int level)
        {
            CheckLevel(level);
            return (level - 1) / LevelsPerStop;
        }

        /// <summary>
        /// La victoria del nivel <paramref name="won"/> es una llegada a parada: L ≥ 10 y múltiplo de 10. Es la cadencia; si esa
        /// parada existe y qué se ve lo deciden el catálogo, <see cref="MapMoments.ForVictory"/> y <see cref="VictoryLineArbiter"/>
        /// (en el 10 cuenta la llegada el Mapa).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">El nivel no está en 1..1.000.</exception>
        public static bool IsArrival(int won)
        {
            CheckLevel(won);
            return won >= LevelsPerStop && won % LevelsPerStop == 0;
        }

        /// <summary>
        /// Niveles que faltan para llegar a la siguiente parada jugando el nivel <paramref name="play"/>, contándolo
        /// («Faltan N»): 10 · (StopIndexOf(p) + 1) − p + 1. En el 58, 3 (58, 59 y 60); en el 60, 1; en el 51, 10.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">El nivel no está en 1..1.000.</exception>
        public static int LevelsLeft(int play)
        {
            return (LevelsPerStop * (StopIndexOf(play) + 1)) - play + 1;
        }

        /// <summary>
        /// Fracción llena del tramo en curso del Mapa con <paramref name="maxLevel"/> niveles ganados (<c>progress.max_level</c>):
        /// (m mod 10) / 10, salvo en los múltiplos de 10 desde el 10, que dan 1 (el tramo recién completado, hasta que se juega el
        /// siguiente). 0 con m = 0; 0,7 con m = 57.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">El valor no está en 0..1.000.</exception>
        public static float LegFill(int maxLevel)
        {
            if (maxLevel < 0 || maxLevel > WorldTable.LastLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLevel), maxLevel, "max_level va de 0 a 1.000 (glosario §8).");
            }

            int filled = maxLevel % LevelsPerStop;
            if (filled == 0)
            {
                return maxLevel >= LevelsPerStop ? 1f : 0f;
            }

            return filled / (float)LevelsPerStop;
        }

        /// <summary>
        /// La victoria del nivel <paramref name="won"/> cambia de mundo: L + 1 es el primer nivel de un mundo ≥ 2 (10, 40, 80,
        /// 140, 200, 300, 420, 560, 700, 820, 920 y 960). El 1.000 no: no hay nivel siguiente en la primera vuelta.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">El nivel no está en 1..1.000.</exception>
        public static bool IsWorldChange(int won)
        {
            CheckLevel(won);
            int next = won + 1;
            return next <= WorldTable.LastLevel && WorldTable.FirstLevel(WorldTable.WorldOf(next)) == next;
        }

        /// <summary>
        /// Hay una llegada a parada sin ver en el Mapa (el punto de la pestaña del inicio; 06 §4.7; 08 §4.3): con
        /// <paramref name="max"/> = <c>max_level</c> y <paramref name="seen"/> = <c>map_seen_level</c>, max ≥ 10 y
        /// max / 10 &gt; seen / 10, es decir, se ganó un <c>10k</c> después del último nivel visto. 9/10 y 19/20 dan true,
        /// 10/10 y 52/58 false. Un <paramref name="seen"/> mayor que <paramref name="max"/> cuenta como visto.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="max"/> no está en 0..1.000 o <paramref name="seen"/> es negativo.</exception>
        public static bool HasUnseenArrival(int seen, int max)
        {
            CheckSeenAndMax(seen, max);
            return max >= LevelsPerStop && max / LevelsPerStop > seen / LevelsPerStop;
        }

        /// <summary>
        /// Nivel desde el que «ponerse al día» enciende boyas al abrir el Mapa (propuesta del Mapa §4.2): se encienden en orden
        /// las de los niveles CatchUpFrom + 1 … <paramref name="max"/>, como mucho <see cref="CatchUpMaxBuoys"/> (= máx(seen,
        /// max − 10), con seen recortado a max). Con seen 52 y max 58, 52 (6 boyas); con 58 y 60, 58 (2 boyas, con la llegada
        /// del 60); con 0 y 58, 48 (10 boyas, con la llegada del 50); con seen ≥ max, max (nada que animar).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="max"/> no está en 0..1.000 o <paramref name="seen"/> es negativo.</exception>
        public static int CatchUpFrom(int seen, int max)
        {
            CheckSeenAndMax(seen, max);
            int from = Math.Min(seen, max);
            return Math.Max(from, max - CatchUpMaxBuoys);
        }

        /// <summary>
        /// Lo que dura «ponerse al día», en ms: (max − <see cref="CatchUpFrom"/>) · <see cref="MapConfig.CatchUpPerBuoyMs"/>,
        /// nunca más de <see cref="MapConfig.CatchUpMaxMs"/> (propuesta del Mapa §4.2: 0,15 s por boya y 1,5 s como mucho).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="max"/> no está en 0..1.000 o <paramref name="seen"/> es negativo.</exception>
        public static int CatchUpMs(int seen, int max)
        {
            int buoys = max - CatchUpFrom(seen, max);
            return Math.Min(buoys * MapConfig.CatchUpPerBuoyMs, MapConfig.CatchUpMaxMs);
        }

        /// <summary>
        /// Guía del Mapa en el mundo <paramref name="world"/> (D-013, Q18-A; P-M8: se deduce del mundo, sin campo en los datos):
        /// <see cref="GuideBufeo"/> en los mundos 7 y 8 y <see cref="GuideRosa"/> en el resto. Son identificadores, no texto.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">El mundo no está en 1..13.</exception>
        public static string GuideFor(int world)
        {
            if (world < 1 || world > WorldTable.WorldCount)
            {
                throw new ArgumentOutOfRangeException(nameof(world), world, "Los mundos van de 1 a 13 (glosario §8).");
            }

            return world >= FirstBufeoWorld && world <= LastBufeoWorld ? GuideBufeo : GuideRosa;
        }

        private static void CheckSeenAndMax(int seen, int max)
        {
            if (max < 0 || max > WorldTable.LastLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "max_level va de 0 a 1.000 (glosario §8).");
            }

            if (seen < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seen), seen, "map_seen_level no es negativo (08 §4.3).");
            }
        }

        private static void CheckLevel(int level)
        {
            if (level < 1 || level > WorldTable.LastLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "La primera vuelta va del nivel 1 al 1.000 (glosario §8).");
            }
        }
    }
}
