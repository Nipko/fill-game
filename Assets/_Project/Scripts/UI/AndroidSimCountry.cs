#nullable enable
using System;
using AF.Platform;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// País de la SIM en Android (Q-T3 (a) de D-033; bloque B12): <c>TelephonyManager.getSimCountryIso()</c> por el JNI de Unity,
    /// sin plugin y <b>sin permiso</b> (no necesita <c>READ_PHONE_STATE</c>). Vive en <c>AF.UI</c> porque necesita el motor, como
    /// <see cref="AndroidDeviceLocale"/>. Sin SIM, en el Editor o si el JNI falla, <c>""</c>: entonces manda la región del teléfono
    /// y, si tampoco, el perfil <c>default</c> (<see cref="ConsentCountry.Resolve"/>). Se lee una vez por arranque.
    /// </summary>
    public sealed class AndroidSimCountry : ISimCountry
    {
        private const string LogPrefix = "[AF.AndroidSimCountry] ";
        private string? _cached;

        /// <summary>La lectura de esta plataforma: la de Android en Android y «sin SIM» en el resto (el Editor).</summary>
        public static ISimCountry ForPlatform()
        {
            return Application.platform == RuntimePlatform.Android ? new AndroidSimCountry() : ManualSimCountry.None;
        }

        /// <inheritdoc />
        public string CountryIso => _cached ??= Read();

        private static string Read()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return string.Empty;
            }

#if UNITY_ANDROID
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject? activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject? telephony = activity?.Call<AndroidJavaObject>("getSystemService", "phone"))
                {
                    string? iso = telephony?.Call<string>("getSimCountryIso");
                    return string.IsNullOrWhiteSpace(iso) ? string.Empty : iso!.Trim();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogPrefix + "no se pudo leer el país de la SIM: " + e.Message + "; se usa la región del teléfono.");
                return string.Empty;
            }
#else
            return string.Empty;
#endif
        }
    }
}
