#nullable enable
using System;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Abre en Android los ajustes de notificaciones de la app (<c>Settings.ACTION_APP_NOTIFICATION_SETTINGS</c> con
    /// <c>EXTRA_APP_PACKAGE</c>, API 26+, el <c>minSdk</c> del juego) y, si el teléfono no tiene esa pantalla, la ficha de la app
    /// (<c>ACTION_APPLICATION_DETAILS_SETTINGS</c>). Para la fila «Avisos» de Ajustes con los avisos bloqueados en el teléfono (06 §8.1:
    /// el diálogo nativo ya no sale). Por el JNI de Unity, sin plugin y <b>sin permiso</b>; vive en <c>AF.UI</c> porque necesita el
    /// motor, como <see cref="AndroidSimCountry"/>. Fuera de Android (el Editor, los tests) no hace nada y devuelve false.
    /// </summary>
    public static class AndroidAppSettings
    {
        private const string LogPrefix = "[AF.AndroidAppSettings] ";

        /// <summary>Abre los ajustes de notificaciones de la app; true si Android aceptó alguna de las dos pantallas.</summary>
        public static bool OpenNotificationSettings()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return false;
            }

#if UNITY_ANDROID
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject? activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity == null)
                    {
                        return false;
                    }

                    string package = activity.Call<string>("getPackageName");
                    if (TryStart(activity, Notifications(package)))
                    {
                        return true;
                    }

                    return TryStart(activity, Details(package));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogPrefix + "no se pudieron abrir los ajustes de la app: " + e.Message);
                return false;
            }
#else
            return false;
#endif
        }

#if UNITY_ANDROID
        private static AndroidJavaObject Notifications(string package)
        {
            var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APP_NOTIFICATION_SETTINGS");
            intent.Call<AndroidJavaObject>("putExtra", "android.provider.extra.APP_PACKAGE", package).Dispose();
            intent.Call<AndroidJavaObject>("addFlags", 0x10000000).Dispose(); // FLAG_ACTIVITY_NEW_TASK
            return intent;
        }

        private static AndroidJavaObject Details(string package)
        {
            using (var uriClass = new AndroidJavaClass("android.net.Uri"))
            using (AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + package))
            {
                var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uri);
                intent.Call<AndroidJavaObject>("addFlags", 0x10000000).Dispose();
                return intent;
            }
        }

        private static bool TryStart(AndroidJavaObject activity, AndroidJavaObject intent)
        {
            using (intent)
            {
                try
                {
                    activity.Call("startActivity", intent);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning(LogPrefix + "Android no abrió la pantalla: " + e.Message);
                    return false;
                }
            }
        }
#endif
    }
}
