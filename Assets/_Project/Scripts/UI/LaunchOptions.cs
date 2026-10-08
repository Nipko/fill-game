#nullable enable
using System;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Opciones de arranque: los extras de intent de los builds de desarrollo (<c>af_level</c>, nivel de arranque,
    /// <c>adb shell am start ... --ei af_level N</c>, docs/dev/prototipo-v0.md c; <c>af_rules_pour_mode</c>, modo de
    /// vertido, <c>--es af_rules_pour_mode single</c> para probar el modo de pruebas, D-005; y <c>af_level_offset</c> [P],
    /// <c>--ei af_level_offset 30</c>, que desplaza los números de nivel para probar las compuertas de 11, 22 y 31 con los
    /// tableros del tutorial, tarea B1-5) y los ganchos de los tests PlayMode (nivel de arranque, modo de vertido, progreso,
    /// perfil y reloj en memoria, desplazamiento y paquete de niveles). <see cref="GameBootstrap"/> los lee al arrancar; los
    /// tests los quitan con <see cref="Reset"/>.
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

        /// <summary>
        /// Extra de intent (int) de los builds de desarrollo [P]: desplazamiento de los números de nivel
        /// (<see cref="LevelCatalog.LevelOffset"/>). Con 30, el tablero del nivel 1 del tutorial se juega como nivel 31 (canoas,
        /// rescate). Escribe esos números en el perfil del dispositivo de pruebas. Nunca se lee en un build de release.
        /// </summary>
        public const string IntentExtraLevelOffset = "af_level_offset";

        /// <summary>Nivel de arranque forzado por un test (0 = ninguno).</summary>
        public static int StartLevelOverride { get; set; }

        /// <summary>Valor de <c>rules_pour_mode</c> forzado por un test (null = ninguno).</summary>
        public static string? PourModeOverride { get; set; }

        /// <summary>Progreso que sustituye al del perfil (tests).</summary>
        public static IProgressStore? ProgressStoreOverride { get; set; }

        /// <summary>
        /// Almacén del perfil que sustituye a <c>profile.json</c> en <c>Application.persistentDataPath</c> (tests: así no
        /// tocan el perfil real del Editor). Con este gancho no se migra <c>af_v0_level</c> de PlayerPrefs.
        /// </summary>
        public static IProfileStore? ProfileStoreOverride { get; set; }

        /// <summary>Reloj que sustituye al del sistema (tests de canoas).</summary>
        public static IClock? ClockOverride { get; set; }

        /// <summary>Desplazamiento de nivel forzado por un test (null = ninguno).</summary>
        public static int? LevelOffsetOverride { get; set; }

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
            ProfileStoreOverride = null;
            ClockOverride = null;
            LevelOffsetOverride = null;
            LevelPackJsonOverride = null;
        }

        /// <summary>
        /// Desplazamiento de nivel de la partida: el del test (<see cref="LevelOffsetOverride"/>), si no el del extra
        /// <c>af_level_offset</c> (solo builds de desarrollo en Android) y, si no, 0. Un valor fuera de
        /// 0..<see cref="LevelCatalog.MaxLevelOffset"/> se trata como 0 y se avisa en el log.
        /// </summary>
        public static int ResolveLevelOffset()
        {
            int offset = LevelOffsetOverride ?? ReadIntentInt(IntentExtraLevelOffset);
            if (offset < 0 || offset > LevelCatalog.MaxLevelOffset)
            {
                Debug.LogWarning("[AF.LaunchOptions] " + IntentExtraLevelOffset + " fuera de 0.." + LevelCatalog.MaxLevelOffset + ": se usa 0.");
                return 0;
            }

            return offset;
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
            return ReadIntentInt(IntentExtraLevel);
        }

        /// <summary>Extra de intent entero <paramref name="name"/> (solo Android y solo en builds de desarrollo); 0 si no hay.</summary>
        public static int ReadIntentInt(string name)
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
                    return intent == null ? 0 : intent.Call<int>("getIntExtra", name, 0);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AF.LaunchOptions] no se pudo leer el extra " + name + ": " + e.Message);
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
