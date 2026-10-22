#nullable enable
using System;
using AF.Platform;
using UnityEngine;
using UnityEngine.Accessibility;

namespace AF.UI
{
    /// <summary>
    /// Escalas de accesibilidad del teléfono Android (<see cref="ISystemScale"/>; bloque B9; 02 §12, fila «Tamaño de texto»):
    /// <list type="bullet">
    /// <item>la escala de fuente, de <see cref="AccessibilitySettings.fontScale"/> (el módulo <c>com.unity.modules.accessibility</c>,
    /// que ya está en el manifiesto: sin paquete, plugin ni permiso nuevos);</item>
    /// <item>la escala de duración de animaciones, <c>Settings.Global.ANIMATOR_DURATION_SCALE</c> por el JNI de Unity
    /// (<see cref="AndroidJavaClass"/>; lectura de un ajuste global, sin permiso).</item>
    /// </list>
    /// Vive en <c>AF.UI</c> porque necesita el motor (<c>AF.Platform</c> no lo tiene), como <see cref="AndroidDeviceLocale"/>. Si
    /// una lectura falla, da <see cref="float.NaN"/> y lo anota una vez en el log. Fuera de Android (el Editor),
    /// <see cref="ForPlatform"/> da la implementación falsa (<see cref="FakeSystemScale"/>: 100 % y animaciones normales). Se lee
    /// en el momento en que se pide (las dos lecturas son baratas, pero asignan): quien la usa guarda el valor al arrancar.
    /// </summary>
    public sealed class AndroidSystemScale : ISystemScale
    {
        private const string LogPrefix = "[AF.AndroidSystemScale] ";
        private const string AnimatorDurationScaleKey = "animator_duration_scale";

        private bool _warnedFont;
#if UNITY_ANDROID
        private bool _warnedAnimator;
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
        public float AnimatorDurationScale => ReadAnimatorDurationScale();

        /// <summary>
        /// Las escalas de esta plataforma: <see cref="AndroidSystemScale"/> en Android y <see cref="FakeSystemScale"/> (100 % y
        /// animaciones normales) en el resto, el Editor incluido.
        /// </summary>
        public static ISystemScale ForPlatform()
        {
            return Application.platform == RuntimePlatform.Android ? new AndroidSystemScale() : new FakeSystemScale();
        }

        // Settings.Global.getFloat(activity.getContentResolver(), "animator_duration_scale", 1f); NaN si falla.
        private float ReadAnimatorDurationScale()
        {
#if UNITY_ANDROID
            if (Application.platform != RuntimePlatform.Android)
            {
                return float.NaN;
            }

            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject? activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject? resolver = activity?.Call<AndroidJavaObject>("getContentResolver"))
                using (var global = new AndroidJavaClass("android.provider.Settings$Global"))
                {
                    return resolver == null ? float.NaN : global.CallStatic<float>("getFloat", resolver, AnimatorDurationScaleKey, 1f);
                }
            }
            catch (Exception e)
            {
                if (!_warnedAnimator)
                {
                    _warnedAnimator = true;
                    Debug.LogWarning(LogPrefix + "no se pudo leer " + AnimatorDurationScaleKey + ": " + e.Message + ".");
                }

                return float.NaN;
            }
#else
            return float.NaN;
#endif
        }
    }
}
