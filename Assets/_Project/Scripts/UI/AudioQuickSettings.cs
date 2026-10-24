#nullable enable
using System.Collections.Generic;
using AF.Save;

namespace AF.UI
{
    /// <summary>
    /// El ajuste de audio que se elige desde Ajustes o con los botones de un toque (D-046, punto 2; 07 §9.4): de dónde llega
    /// (<c>source</c> de <c>af_settings_change</c>, 08 §8.2) y qué se puede cambiar desde cada sitio. Las filas de Ajustes cambian
    /// Sonido, Música y Vibración; la pausa y el inicio solo Sonido y Música (la Vibración se queda en Ajustes, y nada va en la
    /// barra de juego). Todos escriben el mismo campo de <c>settings</c> (08 §4.3), así que siempre coinciden. Puro: lo usa
    /// <c>GameBootstrap.ChooseAudio</c>.
    /// </summary>
    internal static class AudioQuickSettings
    {
        /// <summary><c>source</c> de un cambio desde una fila de Ajustes.</summary>
        public const string SourceSettings = "settings";

        /// <summary><c>source</c> de un cambio desde los botones del menú de pausa.</summary>
        public const string SourcePause = "pause";

        /// <summary><c>source</c> de un cambio desde los botones del inicio.</summary>
        public const string SourceHome = "home";

        /// <summary>Los valores de <c>source</c> de 08 §8.2, en su orden.</summary>
        public static readonly IReadOnlyList<string> Sources = new[] { SourceSettings, SourcePause, SourceHome };

        /// <summary>
        /// <paramref name="key"/> (<see cref="SettingsScreen.SoundId"/>, <see cref="SettingsScreen.MusicId"/> o
        /// <see cref="SettingsScreen.HapticsId"/>) se puede cambiar desde <paramref name="source"/>.
        /// </summary>
        public static bool Allowed(string key, string source)
        {
            bool quick = key == SettingsScreen.SoundId || key == SettingsScreen.MusicId;
            switch (source)
            {
                case SourceSettings: return quick || key == SettingsScreen.HapticsId;
                case SourcePause:
                case SourceHome: return quick;
                default: return false;
            }
        }

        /// <summary>
        /// Escribe <paramref name="on"/> en el campo de <paramref name="key"/> de <paramref name="settings"/> si se puede desde
        /// <paramref name="source"/> (<see cref="Allowed"/>); <paramref name="changed"/> dice si cambió (hay que guardar). False, sin
        /// tocar nada, si no se puede.
        /// </summary>
        public static bool Apply(SettingsData settings, string key, bool on, string source, out bool changed)
        {
            changed = false;
            if (!Allowed(key, source))
            {
                return false;
            }

            switch (key)
            {
                case SettingsScreen.SoundId:
                    changed = settings.Sound != on;
                    settings.Sound = on;
                    return true;
                case SettingsScreen.MusicId:
                    changed = settings.Music != on;
                    settings.Music = on;
                    return true;
                default:
                    changed = settings.Haptics != on;
                    settings.Haptics = on;
                    return true;
            }
        }
    }
}
