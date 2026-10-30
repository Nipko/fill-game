#nullable enable
using System;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Manda la app a segundo plano, como hace Android con Atrás en la pantalla principal de una app (02 §3.5; observación 21 de la
    /// jornada 6, decisión del dueño del 2026-10-30: en el inicio sin nada encima, Atrás no sale ni pide confirmación).
    /// </summary>
    public interface IAppTaskPort
    {
        /// <summary>Lleva la tarea de la app al fondo; true si se pidió.</summary>
        bool MoveToBack();
    }

    /// <summary>
    /// En Android, <c>Activity.moveTaskToBack(true)</c> sobre la actividad de Unity por el JNI, sin plugin ni permisos (como
    /// <see cref="AndroidAppSettings"/>). Fuera de Android no hace nada y devuelve false.
    /// </summary>
    public sealed class AndroidAppTaskPort : IAppTaskPort
    {
        private const string LogPrefix = "[AF.AndroidAppTask] ";

        /// <inheritdoc />
        public bool MoveToBack()
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
                    return activity != null && activity.Call<bool>("moveTaskToBack", true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogPrefix + "no se pudo mandar la app a segundo plano: " + e.Message);
                return false;
            }
#else
            return false;
#endif
        }
    }

    /// <summary>Puerto falso del Editor y de los tests: cuenta las peticiones y no mueve nada.</summary>
    public sealed class FakeAppTaskPort : IAppTaskPort
    {
        /// <summary>Veces que se pidió mandar la app al fondo.</summary>
        public int Calls { get; private set; }

        /// <inheritdoc />
        public bool MoveToBack()
        {
            Calls++;
            return true;
        }
    }
}
