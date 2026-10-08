#nullable enable
using System;
using AF.Core.Rules;
using AF.GameFlow;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Opciones de arranque: los extras de intent de los builds de desarrollo (<c>af_level</c>, nivel de arranque,
    /// <c>adb shell am start ... --ei af_level N</c>, docs/dev/prototipo-v0.md c; y <c>af_rules_pour_mode</c>, modo de
    /// vertido, <c>--es af_rules_pour_mode single</c> para probar el modo de pruebas, D-005) y los ganchos de los tests
    /// PlayMode (nivel de arranque, modo de vertido, progreso en memoria y paquete de niveles). <see cref="GameBootstrap"/> los lee al arrancar; los tests los quitan con
    /// <see cref="Reset"/>.
    /// </summary>
    public static class LaunchOptions
    {
        /// <summary>Extra de intent que elige el nivel de arranque.</summary>
        public const string IntentExtraLevel = "af_level";

        /// <summary>
        /// Extra de intent (string) que sustituye a <c>rules_pour_mode</c> en builds de desarrollo hasta que exista
        /// <c>AF.RemoteConfig</c> (propuesta [P] de <c>docs/dev/decisiones.md</c> §2.1.7): con D-005 el modo por defecto es
        /// <c>"run"</c> y el extra sirve ahora para probar <c>"single"</c> sin tocar código. Nunca se lee en un build de
        /// release.
        /// </summary>
        public const string IntentExtraPourMode = "af_rules_pour_mode";

        /// <summary>Nivel de arranque forzado por un test (0 = ninguno).</summary>
        public static int StartLevelOverride { get; set; }

        /// <summary>Valor de <c>rules_pour_mode</c> forzado por un test (null = ninguno).</summary>
        public static string? PourModeOverride { get; set; }

        /// <summary>Progreso que sustituye a PlayerPrefs (tests).</summary>
        public static IProgressStore? ProgressStoreOverride { get; set; }

        /// <summary>
        /// Paquete de niveles (JSON <c>af-level/1</c> o su subconjunto v0) que sustituye al <c>TextAsset</c> de la escena:
        /// solo tests (p. ej. un tablero de 13 cuias para las filas de D-006). Null en producción: ningún extra de intent lo
        /// rellena y <see cref="Reset"/> lo limpia.
        /// </summary>
        public static string? LevelPackJsonOverride { get; set; }

        /// <summary>Quita los ganchos de test.</summary>
        public static void Reset()
        {
            StartLevelOverride = 0;
            PourModeOverride = null;
            ProgressStoreOverride = null;
            LevelPackJsonOverride = null;
        }

        /// <summary>
        /// Modo de vertido de la partida (D-005): el del test (<see cref="PourModeOverride"/>), si no el del extra
        /// <c>af_rules_pour_mode</c> (solo builds de desarrollo en Android) y, si no, el default compilado de
        /// <c>rules_pour_mode</c> (<see cref="GameConfig.RulesPourMode"/>, <c>"run"</c>, 08 §9). La conversión es la del
        /// núcleo (<see cref="PourModes.TryParse"/>): un valor desconocido se trata como <see cref="PourModes.Default"/>
        /// (<see cref="PourMode.Run"/>) y se avisa en el log.
        /// </summary>
        public static PourMode ResolvePourMode()
        {
            string? value = PourModeOverride;
            string origin = "test";
            if (value == null)
            {
                value = ReadIntentPourMode();
                origin = "extra " + IntentExtraPourMode;
            }

            if (value == null)
            {
                value = GameConfig.RulesPourMode;
                origin = "rules_pour_mode";
            }

            if (!PourModes.TryParse(value, out PourMode mode))
            {
                Debug.LogWarning("[AF.LaunchOptions] valor de modo de vertido desconocido (\"" + value + "\", " + origin
                    + "): se usa \"" + PourModes.ToKey(mode) + "\" (rules_pour_mode, D-005).");
            }

            return mode;
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
                using (AndroidJavaObject? intent = CurrentIntent())
                {
                    return intent == null ? 0 : intent.Call<int>("getIntExtra", IntentExtraLevel, 0);
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

        /// <summary>
        /// Valor del extra de intent <c>af_rules_pour_mode</c> (solo Android y solo en builds de desarrollo); null si no
        /// hay.
        /// </summary>
        public static string? ReadIntentPourMode()
        {
            if (!Debug.isDebugBuild || Application.platform != RuntimePlatform.Android)
            {
                return null;
            }

#if UNITY_ANDROID
            try
            {
                using (AndroidJavaObject? intent = CurrentIntent())
                {
                    return intent == null ? null : intent.Call<string>("getStringExtra", IntentExtraPourMode);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AF.LaunchOptions] no se pudo leer el extra " + IntentExtraPourMode + ": " + e.Message);
                return null;
            }
#else
            return null;
#endif
        }

#if UNITY_ANDROID
        // Sirve con Activity y con GameActivity (UnityPlayerGameActivity, entrada por defecto en Unity 6). El objeto de la
        // actividad lo cachea Unity: no se libera aquí; el intent sí lo libera quien lo pide.
        private static AndroidJavaObject? CurrentIntent()
        {
            AndroidJavaObject activity = UnityEngine.Android.AndroidApplication.currentActivity;
            return activity == null ? null : activity.Call<AndroidJavaObject>("getIntent");
        }
#endif
    }
}
