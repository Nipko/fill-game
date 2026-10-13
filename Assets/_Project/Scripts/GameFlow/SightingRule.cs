#nullable enable
using System;

namespace AF.GameFlow
{
    /// <summary>
    /// En qué nivel sale la tarjeta «¡Miquito a la vista!» (guion de la intro §4.5 y §14): en el primer nivel ≥
    /// <c>nivel_tarjeta</c> que esté libre, sin banner de mundo, sin Fruta nueva, sin debut de mecánica o de comodín y sin
    /// tutorial pendiente al empezar, con una sola tarjeta por nivel (prioridad tutorial → Fruta nueva → miquito). Los niveles
    /// con banner salen de la única regla que los decide, <see cref="WorldBanners"/> (11, 41, 81, 141…; el 1 no lo lleva por
    /// decisión del dueño, pero ya está ocupado por la Fruta nueva y el tutorial). Regla pura y determinista: no guarda nada
    /// (persistir los avistamientos y emitir <c>af_fauna_sighted</c> exige migrar el perfil y la aprobación del dueño, Q11; PR
    /// aparte). Solo en el primer intento del nivel: los reintentos ya la vieron.
    /// </summary>
    public static class SightingRule
    {
        /// <summary>Debut de cada tinte: lleva la tarjeta «Fruta nueva» (07 §3, columna «Debut»; 06 §4.5).</summary>
        public static readonly int[] FruitDebutLevels = { 1, 2, 4, 7, 11, 21, 41, 61, 81, 111, 141, 201, 301 };

        /// <summary>
        /// Estrenos con su tutorial de una pantalla (D-007; 02 §9.1 y §11; 04 §2.5), en orden: Cuia chica 14, Cuia alta 27, Hoja
        /// 43, Matapi 63, Liana con fruta 83, Aguas turbias y la Luciérnaga regalada 100, Remolino 103, Barro 123, Lluvia 143,
        /// Brote 163 y Piraña 183. Única copia en el código (la usan también los tests del catálogo y las capturas de los
        /// estrenos); no se modifica.
        /// </summary>
        public static readonly int[] MechanicDebutLevels = { 14, 27, 43, 63, 83, 100, 103, 123, 143, 163, 183 };

        /// <summary>
        /// Hitos con su propia presentación (02 §13: Marea 3, Reto del río 7, Cuia extra 8, Mercado 10, contador de Marea y
        /// Cosecha doble 11, Morpho 12, Cántaro 20, animación corta 21, Pororoca 30, Canoas y Cuia de rescate 31) y todos los
        /// estrenos de <see cref="MechanicDebutLevels"/> (D-007 y 04 §2.5: 14, 27, 43, 63, 83, 100, 103, 123, 143, 163 y 183),
        /// en orden y sin repetidos. Se arma con <see cref="MechanicDebutLevels"/>: un estreno nuevo no puede quedarse fuera.
        /// </summary>
        public static readonly int[] MilestoneLevels = SortedUnion(new[] { 3, 7, 8, 10, 11, 12, 20, 21, 30, 31 }, MechanicDebutLevels);

        /// <summary>El nivel ya tiene banner de mundo (<see cref="WorldBanners.ShowsOn"/>), Fruta nueva o un hito propio.</summary>
        public static bool IsBusy(int level)
        {
            return WorldBanners.ShowsOn(level) || Array.IndexOf(FruitDebutLevels, level) >= 0
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

        // Unión ordenada y sin repetidos de dos listas (una vez, al iniciar el tipo).
        private static int[] SortedUnion(int[] a, int[] b)
        {
            var merged = new int[a.Length + b.Length];
            Array.Copy(a, merged, a.Length);
            Array.Copy(b, 0, merged, a.Length, b.Length);
            Array.Sort(merged);
            int n = 0;
            for (int i = 0; i < merged.Length; i++)
            {
                if (n == 0 || merged[n - 1] != merged[i])
                {
                    merged[n++] = merged[i];
                }
            }

            Array.Resize(ref merged, n);
            return merged;
        }
    }
}
