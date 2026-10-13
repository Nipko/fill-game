#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Cuánto se ve un aviso del tablero para poder leerlo (D-011, punto 2; prueba en el teléfono del 2026-10-11, §P2) [P]:
    /// <c>1.000 ms + 55 ms × caracteres visibles</c>, entre 3.000 y 7.000 ms. La usan el aviso (<c>Toast</c>: regalo de Capi,
    /// «¡Una cuia más!», «+1 Luciérnaga», «Te faltan {n} semillas») y la tarjeta «¡Miquito a la vista!» (título, nombre y dato,
    /// sumados sin concatenar). Quedan fuera el cartel «El río bajó» (2,0 s, 04 §2.7: <c>anim_river_lowered_banner_ms</c>) y el
    /// rótulo del intento, que tienen su propia duración. Los textos de los avisos no llevan etiquetas de TextMeshPro, así que
    /// los caracteres visibles son <see cref="string.Length"/>. Sin asignaciones: solo aritmética de enteros.
    /// </summary>
    public static class NoticeTiming
    {
        /// <summary>Parte fija (ms): lo que se tarda en ver que hay un aviso.</summary>
        public const int BaseMs = 1000;

        /// <summary>Por cada carácter visible (ms).</summary>
        public const int PerCharMs = 55;

        /// <summary>Lo menos que se ve un aviso (ms): con 36 caracteres o menos.</summary>
        public const int MinMs = 3000;

        /// <summary>Lo más que se ve un aviso (ms): desde 110 caracteres.</summary>
        public const int MaxMs = 7000;

        /// <summary>Duración por lectura de un aviso de <paramref name="visibleChars"/> caracteres visibles (ms).</summary>
        public static int ReadMs(int visibleChars)
        {
            if (visibleChars <= 0)
            {
                return MinMs;
            }

            // Desde 110 caracteres ya se pasa del máximo: así el producto nunca desborda.
            if (visibleChars >= (MaxMs - BaseMs) / PerCharMs + 1)
            {
                return MaxMs;
            }

            int ms = BaseMs + (PerCharMs * visibleChars);
            return ms < MinMs ? MinMs : (ms > MaxMs ? MaxMs : ms);
        }

        /// <summary>Duración por lectura de <paramref name="text"/> (null cuenta como vacío).</summary>
        public static int ReadMs(string? text)
        {
            return ReadMs(text == null ? 0 : text.Length);
        }

        /// <summary>
        /// Duración por lectura de un aviso de tres partes, como la tarjeta «¡Miquito a la vista!» (título, nombre y dato): la
        /// suma de sus longitudes, sin concatenar.
        /// </summary>
        public static int ReadMs(string? first, string? second, string? third)
        {
            int n = (first == null ? 0 : first.Length) + (second == null ? 0 : second.Length) + (third == null ? 0 : third.Length);
            return ReadMs(n);
        }
    }
}
