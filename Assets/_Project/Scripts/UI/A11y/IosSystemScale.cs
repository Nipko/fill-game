#nullable enable
using System;
using AF.Platform;
using UnityEngine;
using UnityEngine.Accessibility;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace AF.UI
{
    /// <summary>
    /// Escalas de accesibilidad del iPhone (<see cref="ISystemScale"/>; fase M2 de <c>docs/dev/traspaso/mac-ios.md</c>; 02 §12):
    /// <list type="bullet">
    /// <item>la escala de fuente, de <see cref="AccessibilitySettings.fontScale"/>, que en iOS sigue al Tamaño del texto
    /// (Dynamic Type) del sistema, como en Android con la escala de fuente;</item>
    /// <item>«Reducir movimiento» (Ajustes › Accesibilidad › Movimiento) por el plugin propio <c>Plugins/iOS/AfPlatform.mm</c>
    /// (<c>UIAccessibilityIsReduceMotionEnabled</c>): encendido es una escala de animaciones de 0, lo mismo que «Quitar
    /// animaciones» en Android (Q-B9-3); apagado, 1.</item>
    /// </list>
    /// Si una lectura falla, da <see cref="float.NaN"/> y lo anota una vez en el log, como <see cref="AndroidSystemScale"/>. La crea
    /// <see cref="AndroidSystemScale.ForPlatform"/> en el iPhone.
    /// </summary>
    public sealed class IosSystemScale : ISystemScale
    {
        private const string LogPrefix = "[AF.IosSystemScale] ";
        private bool _warnedFont;
        private bool _warnedMotion;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool AfReduceMotionEnabled();
#endif

        /// <inheritdoc />
        public float FontScale
        {
            get
            {
                try
                {
                    float scale = AccessibilitySettings.fontScale;
                    return scale > 0f ? scale : float.NaN;
                }
                catch (Exception e)
                {
                    if (!_warnedFont)
                    {
                        _warnedFont = true;
                        Debug.LogWarning(LogPrefix + "no se pudo leer la escala de fuente: " + e.Message + "; se usa el 100 %.");
                    }

                    return float.NaN;
                }
            }
        }

        /// <inheritdoc />
        public float AnimatorDurationScale
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                try
                {
                    return AfReduceMotionEnabled() ? 0f : 1f;
                }
                catch (Exception e)
                {
                    if (!_warnedMotion)
                    {
                        _warnedMotion = true;
                        Debug.LogWarning(LogPrefix + "no se pudo leer Reducir movimiento: " + e.Message + ".");
                    }

                    return float.NaN;
                }
#else
                _ = _warnedMotion;
                return float.NaN;
#endif
            }
        }
    }
}
