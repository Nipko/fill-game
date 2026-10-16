#nullable enable
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de las filas Sonido, Música y Vibración de Ajustes (tarea AU1; 07 §9.4; glosario §10, <c>sound</c> / <c>music</c> /
    /// <c>haptics</c>), de <c>strings.csv</c>. Parcial de <see cref="UiText"/>.
    /// </summary>
    public static partial class UiText
    {
        /// <summary><c>sound</c> (glosario §10): «Sonido».</summary>
        public static string Sound => Loc.T(Keys.Sound);

        /// <summary><c>music</c> (glosario §10): «Música».</summary>
        public static string Music => Loc.T(Keys.Music);

        /// <summary><c>haptics</c> (glosario §10): «Vibración».</summary>
        public static string Haptics => Loc.T(Keys.Haptics);

        /// <summary>Claves de las filas de audio de Ajustes (parcial de <see cref="Keys"/>).</summary>
        public static partial class Keys
        {
            /// <summary><c>sound</c> (glosario §10).</summary>
            public const string Sound = "sound";

            /// <summary><c>music</c> (glosario §10).</summary>
            public const string Music = "music";

            /// <summary><c>haptics</c> (glosario §10).</summary>
            public const string Haptics = "haptics";
        }
    }
}
