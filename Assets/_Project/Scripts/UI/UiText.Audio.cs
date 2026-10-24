#nullable enable
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de las filas Sonido, Música y Vibración de Ajustes (tarea AU1; 07 §9.4; glosario §10, <c>sound</c> / <c>music</c> /
    /// <c>haptics</c>) y las etiquetas de TalkBack de los botones de un toque Música y Sonido de la pausa y del inicio (D-046,
    /// punto 2; <see cref="AudioToggles"/>), de <c>strings.csv</c>. Parcial de <see cref="UiText"/>.
    /// </summary>
    public static partial class UiText
    {
        /// <summary><c>sound</c> (glosario §10): «Sonido».</summary>
        public static string Sound => Loc.T(Keys.Sound);

        /// <summary><c>music</c> (glosario §10): «Música».</summary>
        public static string Music => Loc.T(Keys.Music);

        /// <summary><c>haptics</c> (glosario §10): «Vibración».</summary>
        public static string Haptics => Loc.T(Keys.Haptics);

        /// <summary>
        /// Etiqueta de TalkBack del botón de un toque de Música (D-046; 02 §12: etiqueta funcional + estado): «Música: activada» o
        /// «Música: desactivada» [P]. Una frase completa por estado, sin pegar textos.
        /// </summary>
        public static string MusicA11y(bool on)
        {
            return Loc.T(on ? Keys.A11yMusicOn : Keys.A11yMusicOff);
        }

        /// <summary>Etiqueta de TalkBack del botón de un toque de Sonido: «Sonido: activado» o «Sonido: desactivado» [P].</summary>
        public static string SoundA11y(bool on)
        {
            return Loc.T(on ? Keys.A11ySoundOn : Keys.A11ySoundOff);
        }

        /// <summary>Claves de las filas de audio de Ajustes y de los botones de un toque (parcial de <see cref="Keys"/>).</summary>
        public static partial class Keys
        {
            /// <summary><c>sound</c> (glosario §10).</summary>
            public const string Sound = "sound";

            /// <summary><c>music</c> (glosario §10).</summary>
            public const string Music = "music";

            /// <summary><c>haptics</c> (glosario §10).</summary>
            public const string Haptics = "haptics";

            /// <summary>TalkBack del botón de Música encendida (D-046).</summary>
            public const string A11yMusicOn = "a11y_music_on";

            /// <summary>TalkBack del botón de Música apagada (D-046).</summary>
            public const string A11yMusicOff = "a11y_music_off";

            /// <summary>TalkBack del botón de Sonido encendido (D-046).</summary>
            public const string A11ySoundOn = "a11y_sound_on";

            /// <summary>TalkBack del botón de Sonido apagado (D-046).</summary>
            public const string A11ySoundOff = "a11y_sound_off";
        }
    }
}
