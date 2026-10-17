#nullable enable
using System;
using UnityEngine;

namespace AF.Audio.Unity
{
    /// <summary>
    /// Hápticos de Android por el JNI de Unity, sin plugin (tarea AU1; 02 §4; 07 §9.2; permiso <c>VIBRATE</c>, aprobado por el
    /// dueño el 2026-10-16, D-026): con API 29+, los efectos predefinidos (<c>VibrationEffect.createPredefined</c>:
    /// <c>EFFECT_TICK</c> para el sellado y Atrás bloqueado, <c>EFFECT_CLICK</c> para la victoria, Capi y el Boto); con API 26-28,
    /// un pulso de 20, 15 o 40 ms (<c>createOneShot</c> con la amplitud por defecto). Fuera de Android, o si el teléfono no tiene
    /// vibrador, no hace nada. Los objetos de Java se crean una vez; cada vibración solo pasa sus argumentos.
    /// </summary>
    public sealed class AndroidHaptics : IHaptics
    {
        private const int DefaultAmplitude = -1; // VibrationEffect.DEFAULT_AMPLITUDE

#if UNITY_ANDROID && !UNITY_EDITOR
        private readonly AndroidJavaObject? _vibrator;
        private readonly AndroidJavaClass? _effect;
        private readonly int _sdk;
#endif

        private AndroidHaptics()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    _sdk = version.GetStatic<int>("SDK_INT");
                }

                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    if (vibrator != null && vibrator.Call<bool>("hasVibrator"))
                    {
                        _vibrator = vibrator;
                        _effect = new AndroidJavaClass("android.os.VibrationEffect");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("AF.Audio: sin vibrador (" + e.Message + ").");
                _vibrator = null;
            }
#endif
        }

        /// <summary>Los hápticos de esta plataforma: los de Android en el teléfono; si no, <see cref="NullHaptics"/>.</summary>
        public static IHaptics ForPlatform()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var haptics = new AndroidHaptics();
            return haptics._vibrator != null ? haptics : (IHaptics)NullHaptics.Instance;
#else
            return NullHaptics.Instance;
#endif
        }

        /// <inheritdoc />
        public void Vibrate(HapticKind kind)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_vibrator == null || _effect == null)
            {
                return;
            }

            HapticStrength strength = Haptics.StrengthOf(kind);
            try
            {
                using (AndroidJavaObject effect = _sdk >= 29
                    ? _effect.CallStatic<AndroidJavaObject>("createPredefined", Haptics.PredefinedEffect(strength))
                    : _effect.CallStatic<AndroidJavaObject>("createOneShot", Haptics.FallbackMs(strength), DefaultAmplitude))
                {
                    _vibrator.Call("vibrate", effect);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("AF.Audio: no se pudo vibrar (" + e.Message + ").");
            }
#else
            _ = kind;
#endif
        }
    }

    /// <summary>
    /// Modo del timbre del teléfono (<c>AudioManager.getRingerMode</c> por JNI; 07 §9.4: «respeta el modo silencio del sistema»).
    /// En el Editor, siempre <see cref="RingerMode.Normal"/>.
    /// </summary>
    public sealed class AndroidRinger
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private readonly AndroidJavaObject? _audioManager;
#endif

        private AndroidRinger()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _audioManager = activity.Call<AndroidJavaObject>("getSystemService", "audio");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("AF.Audio: sin AudioManager (" + e.Message + ").");
                _audioManager = null;
            }
#endif
        }

        /// <summary>El de esta plataforma.</summary>
        public static AndroidRinger ForPlatform()
        {
            return new AndroidRinger();
        }

        /// <summary>El modo de ahora (Normal si no se puede leer).</summary>
        public RingerMode Read()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_audioManager == null)
            {
                return RingerMode.Normal;
            }

            try
            {
                int mode = _audioManager.Call<int>("getRingerMode");
                return mode == 0 ? RingerMode.Silent : mode == 1 ? RingerMode.Vibrate : RingerMode.Normal;
            }
            catch (Exception)
            {
                return RingerMode.Normal;
            }
#else
            return RingerMode.Normal;
#endif
        }
    }
}
