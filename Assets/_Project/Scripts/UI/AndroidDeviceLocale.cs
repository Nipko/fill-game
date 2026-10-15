#nullable enable
using System;
using AF.Platform;
using UnityEngine;

namespace AF.UI
{
    /// <summary>De dónde salió la etiqueta del teléfono de <see cref="AndroidDeviceLocale"/> (sonda <c>AF_STATE loc</c>).</summary>
    public enum DeviceLocaleSource
    {
        /// <summary>De <c>java.util.Locale.getDefault()</c> por el JNI de Unity (solo en Android).</summary>
        Jni = 0,

        /// <summary>De la cultura de .NET (<see cref="SystemDeviceLocale"/>): fuera de Android, o si el JNI no dio nada.</summary>
        Culture = 1,
    }

    /// <summary>
    /// Idioma y región del teléfono en Android (D-018, Q7-A y Q8-A; arquitectura de la tanda 4, §1; tarea L3):
    /// <c>java.util.Locale.getDefault().toLanguageTag()</c> por el JNI de Unity (<see cref="AndroidJavaClass"/>), sin plugin ni
    /// permiso. Vive en <c>AF.UI</c> porque necesita el motor (<c>AF.Platform</c> no lo tiene). Si el JNI falla o no da nada
    /// (o <c>und</c>), la cultura de .NET (<see cref="SystemDeviceLocale"/>), que en IL2CPP puede llegar vacía: la sonda
    /// <c>AF_STATE loc</c> enseña la etiqueta y de dónde salió para medirlo en el teléfono. Fuera de Android (el Editor),
    /// <see cref="ForPlatform"/> da directamente <see cref="SystemDeviceLocale"/>.
    /// </summary>
    public sealed class AndroidDeviceLocale : IDeviceLocale
    {
        private const string LogPrefix = "[AF.AndroidDeviceLocale] ";

        private readonly IDeviceLocale _fallback;

        /// <summary>Crea la lectura de Android con <paramref name="fallback"/> de respaldo (por defecto, la cultura de .NET).</summary>
        public AndroidDeviceLocale(IDeviceLocale? fallback = null)
        {
            _fallback = fallback ?? SystemDeviceLocale.Instance;
        }

        /// <summary>De dónde salió la última etiqueta leída (<see cref="LanguageTag"/>).</summary>
        public DeviceLocaleSource LastSource { get; private set; } = DeviceLocaleSource.Culture;

        /// <inheritdoc />
        public string LanguageTag
        {
            get
            {
                string tag = ReadJni();
                if (tag.Length > 0)
                {
                    LastSource = DeviceLocaleSource.Jni;
                    return tag;
                }

                LastSource = DeviceLocaleSource.Culture;
                return _fallback.LanguageTag ?? string.Empty;
            }
        }

        /// <summary>
        /// La lectura del idioma del teléfono de esta plataforma: <see cref="AndroidDeviceLocale"/> en Android y
        /// <see cref="SystemDeviceLocale"/> en el resto (el Editor).
        /// </summary>
        public static IDeviceLocale ForPlatform()
        {
            return Application.platform == RuntimePlatform.Android ? new AndroidDeviceLocale() : SystemDeviceLocale.Instance;
        }

        /// <summary>Nombre de la fuente de <paramref name="locale"/> en la sonda: <c>jni</c>, <c>culture</c> o <c>manual</c>.</summary>
        public static string SourceKey(IDeviceLocale locale)
        {
            if (locale is AndroidDeviceLocale android)
            {
                return android.LastSource == DeviceLocaleSource.Jni ? "jni" : "culture";
            }

            return locale is SystemDeviceLocale ? "culture" : "manual";
        }

        // java.util.Locale.getDefault().toLanguageTag(); "" si no es Android, si falla o si da «und» (sin idioma).
        private static string ReadJni()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return string.Empty;
            }

#if UNITY_ANDROID
            try
            {
                using (var localeClass = new AndroidJavaClass("java.util.Locale"))
                using (AndroidJavaObject? locale = localeClass.CallStatic<AndroidJavaObject>("getDefault"))
                {
                    string? tag = locale?.Call<string>("toLanguageTag");
                    if (string.IsNullOrWhiteSpace(tag) || string.Equals(tag, "und", StringComparison.OrdinalIgnoreCase))
                    {
                        return string.Empty;
                    }

                    return tag!.Trim();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogPrefix + "no se pudo leer java.util.Locale: " + e.Message + "; se usa la cultura de .NET.");
                return string.Empty;
            }
#else
            return string.Empty;
#endif
        }
    }
}
