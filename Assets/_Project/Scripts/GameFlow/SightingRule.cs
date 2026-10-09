#nullable enable
using System;

namespace AF.GameFlow
{
    /// <summary>
    /// En qué nivel sale la tarjeta «¡Miquito a la vista!» (guion de la intro §4.5 y §14): en el primer nivel ≥
    /// <c>nivel_tarjeta</c> que esté libre, sin banner de mundo, sin Fruta nueva, sin debut de mecánica o de comodín y sin
    /// tutorial pendiente al empezar, con una sola tarjeta por nivel (prioridad tutorial → Fruta nueva → miquito). Regla pura y
    /// determinista: no guarda nada (persistir los avistamientos y emitir <c>af_fauna_sighted</c> exige migrar el perfil y la
    /// aprobación del dueño, Q11; PR aparte). Solo en el primer intento del nivel: los reintentos ya la vieron.
    /// </summary>
    public static class SightingRule
    {
        /// <summary>Primer nivel de cada mundo: lleva su banner de entrada (07 §2.1 y §2.2, regla 1).</summary>
        public static readonly int[] WorldBannerLevels = { 1, 11, 41, 81, 141, 201, 301, 421, 561, 701, 821, 921, 961 };

        /// <summary>Debut de cada tinte: lleva la tarjeta «Fruta nueva» (07 §3, columna «Debut»; 06 §4.5).</summary>
        public static readonly int[] FruitDebutLevels = { 1, 2, 4, 7, 11, 21, 41, 61, 81, 111, 141, 201, 301 };

        /// <summary>
        /// Hitos y debuts de comodines y mecánicas con su propia presentación (02 §13: Marea 3, Reto del río 7, Cuia extra 8,
        /// Mercado 10, contador de Marea y Cosecha doble 11, Morpho 12, Cuia chica 14, Cántaro 20, animación corta 21, Cuia alta
        /// 27, Pororoca 30, Canoas y Cuia de rescate 31; 03 §9 y 02 <c>mech_debut_level</c>: 43, 63, 83, 103, 123, 143 y 183).
        /// </summary>
        public static readonly int[] MilestoneLevels = { 3, 7, 8, 10, 11, 12, 14, 20, 21, 27, 30, 31, 43, 63, 83, 103, 123, 143, 183 };

        /// <summary>Duración de la tarjeta (guion §4.5: 2,5 s).</summary>
        public const int CardMs = 2500;

        /// <summary>El nivel ya tiene banner de mundo, Fruta nueva o un hito propio.</summary>
        public static bool IsBusy(int level)
        {
            return Array.IndexOf(WorldBannerLevels, level) >= 0 || Array.IndexOf(FruitDebutLevels, level) >= 0
                || Array.IndexOf(MilestoneLevels, level) >= 0;
        }

        /// <summary>
        /// Primer nivel libre desde <paramref name="cardLevel"/> (incluido). <paramref name="tutorialPending"/> marca los niveles
        /// con un tutorial pendiente al empezar (p. ej. el de Marea en el 5 si no salió en el 3); null = ninguno.
        /// </summary>
        public static int FirstFreeLevel(int cardLevel, Func<int, bool>? tutorialPending = null)
        {
            int level = Math.Max(1, cardLevel);
            for (int guard = 0; guard < 1000; guard++, level++)
            {
                if (!IsBusy(level) && (tutorialPending == null || !tutorialPending(level)))
                {
                    return level;
                }
            }

            return level;
        }

        /// <summary>
        /// La tarjeta de una especie con <paramref name="cardLevel"/> sale en <paramref name="level"/>: es su primer nivel
        /// libre, el primer intento y la especie no se ha nombrado ya en este nivel.
        /// </summary>
        public static bool ShowsCard(int level, int cardLevel, bool firstAttempt, Func<int, bool>? tutorialPending = null)
        {
            return firstAttempt && level >= cardLevel && level == FirstFreeLevel(cardLevel, tutorialPending);
        }
    }
}
