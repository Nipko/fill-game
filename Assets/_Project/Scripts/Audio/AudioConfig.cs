#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// Claves <c>audio_*</c> de Remote Config que lee el audio (08 §9, la única tabla de defaults; tarea AU2). Hasta que exista
    /// <c>AF.RemoteConfig</c> en el cliente valen sus defaults; se pueden cambiar en los tests (y, después, desde Remote Config)
    /// dentro de su rango de 08 §9, fuera del cual se recortan. <see cref="Reset"/> vuelve a los defaults.
    /// </summary>
    public static class AudioConfig
    {
        /// <summary><c>audio_music_percussion_layers</c> por defecto (08 §9): 2.</summary>
        public const int MusicPercussionLayersDefault = 2;

        /// <summary>Máximo de <c>audio_music_percussion_layers</c> (08 §9: rango 0-3); también las capas que se generan.</summary>
        public const int MusicPercussionLayersMax = 3;

        /// <summary><c>audio_ambient_rain_scale</c> por defecto (08 §9): 1,0.</summary>
        public const double AmbientRainScaleDefault = 1.0;

        /// <summary>Máximo de <c>audio_ambient_rain_scale</c> (08 §9: rango 0-2).</summary>
        public const double AmbientRainScaleMax = 2.0;

        private static int s_layers = MusicPercussionLayersDefault;
        private static double s_rainScale = AmbientRainScaleDefault;

        /// <summary>
        /// <c>audio_music_percussion_layers</c> (08 §9; 07 §9.3): capas de percusión suave que entran al sellar cuias, como mucho.
        /// Se recorta a 0-3.
        /// </summary>
        public static int MusicPercussionLayers
        {
            get => s_layers;
            set => s_layers = Math.Max(0, Math.Min(MusicPercussionLayersMax, value));
        }

        /// <summary>
        /// <c>audio_ambient_rain_scale</c> (08 §9; 07 §9.1): multiplicador de la probabilidad de lluvia de cada nivel (el alias
        /// retirado <c>ambient_rain_probability_scale</c> de 07 dice qué multiplica). Se recorta a 0-2.
        /// </summary>
        public static double AmbientRainScale
        {
            get => s_rainScale;
            set => s_rainScale = double.IsNaN(value) ? AmbientRainScaleDefault : Math.Max(0.0, Math.Min(AmbientRainScaleMax, value));
        }

        /// <summary>Vuelve a los defaults de 08 §9.</summary>
        public static void Reset()
        {
            s_layers = MusicPercussionLayersDefault;
            s_rainScale = AmbientRainScaleDefault;
        }
    }
}
