#nullable enable
using System;
using AF.GameFlow;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Opciones de arranque del nivel: el extra de intent <c>af_level</c> en builds de desarrollo
    /// (<c>adb shell am start ... --ei af_level N</c>, docs/dev/prototipo-v0.md c) y los ganchos de los tests PlayMode
    /// (nivel de arranque y progreso en memoria). <see cref="GameBootstrap"/> los lee al arrancar; los tests los quitan con
    /// <see cref="Reset"/>.
    /// </summary>
    public static class LaunchOptions
    {
        /// <summary>Extra de intent que elige el nivel de arranque.</summary>
        public const string IntentExtraLevel = "af_level";

        /// <summary>Nivel de arranque forzado por un test (0 = ninguno).</summary>
        public static int StartLevelOverride { get; set; }

        /// <summary>Progreso que sustituye a PlayerPrefs (tests).</summary>
        public static IProgressStore? ProgressStoreOverride { get; set; }

        /// <summary>Quita los ganchos de test.</summary>
        public static void Reset()
        {
            StartLevelOverride = 0;
            ProgressStoreOverride = null;
        }

        /// <summary>
        /// Nivel pedido por el extra de intent <c>af_level</c> (solo Android y solo en builds de desarrollo); 0 si no hay.
        /// </summary>
        public static int ReadIntentLevel()
        {
            if (!Debug.isDebugBuild || Application.platform != RuntimePlatform.Android)
            {
                return 0;
            }

#if UNITY_ANDROID
            try
            {
                // Sirve con Activity y con GameActivity (UnityPlayerGameActivity, entrada por defecto en Unity 6).
                // El objeto de la actividad lo cachea Unity: no se libera aquí.
                AndroidJavaObject activity = UnityEngine.Android.AndroidApplication.currentActivity;
                if (activity == null)
                {
                    return 0;
                }

                using (AndroidJavaObject intent = activity.Call<AndroidJavaObject>("getIntent"))
                {
                    if (intent == null)
                    {
                        return 0;
                    }

                    return intent.Call<int>("getIntExtra", IntentExtraLevel, 0);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AF.LaunchOptions] no se pudo leer el extra " + IntentExtraLevel + ": " + e.Message);
                return 0;
            }
#else
            return 0;
#endif
        }
    }
}
