#nullable enable
using System;
using AF.Ads;
using AF.Core.Rules;
using AF.IAP;
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
    /// tableros del tutorial, tarea B1-5; y, para probar el embudo y los anuncios en el teléfono [P], tarea B2-6:
    /// <c>af_seeds</c>, <c>af_canoes</c> y <c>af_ads_fill</c>) y los ganchos de los tests PlayMode (nivel de arranque, modo de
    /// vertido, progreso, perfil y reloj en memoria, desplazamiento y paquete de niveles). <see cref="GameBootstrap"/> los lee
    /// al arrancar; los tests los quitan con <see cref="Reset"/>.
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

        /// <summary>
        /// Extra de intent (int) de los builds de desarrollo [P] (tarea B2-6): fija las semillas del perfil del dispositivo de
        /// pruebas al arrancar (<c>--ei af_seeds 1000</c>). Nunca se lee en un build de release.
        /// </summary>
        public const string IntentExtraSeeds = "af_seeds";

        /// <summary>
        /// Extra de intent (int) de los builds de desarrollo [P] (tarea B2-6): fija las canoas del perfil (0 a
        /// <c>canoes_max</c>; <c>--ei af_canoes 1</c>) para recorrer el embudo hasta Sin canoas. Nunca en release.
        /// </summary>
        public const string IntentExtraCanoes = "af_canoes";

        /// <summary>
        /// Extra de intent (string) de los builds de desarrollo [P] (tarea B2-6): qué hace el anuncio simulado,
        /// <c>fill</c>, <c>no_fill</c> o <c>error</c> (<c>--es af_ads_fill no_fill</c>). Nunca en release.
        /// </summary>
        public const string IntentExtraAdsFill = "af_ads_fill";

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

        /// <summary>
        /// Pantalla «Anuncio simulado» que sustituye a <see cref="AdOverlay"/> (tests: <see cref="ImmediateAdPresenter"/> cierra
        /// al instante sin esperar los 3-5 s).
        /// </summary>
        public static ISimulatedAdPresenter? AdPresenterOverride { get; set; }

        /// <summary>Hoja «Compra simulada» que sustituye a <see cref="StoreSheet"/> (tests: <see cref="ImmediatePurchasePresenter"/>).</summary>
        public static ISimulatedPurchasePresenter? PurchasePresenterOverride { get; set; }

        /// <summary>Qué hace el anuncio simulado (tests: <c>no_fill</c>, error); null = hay anuncio.</summary>
        public static FakeAdFill? AdFillOverride { get; set; }

        /// <summary>
        /// Moneda de los precios simulados [P]: COP (05 §4.2; los wireframes de 04 §6.6 usan pesos colombianos) mientras no
        /// haya precios de la tienda real.
        /// </summary>
        public const string DefaultStoreCurrency = "COP";

        /// <summary>Quita los ganchos de test.</summary>
        public static void Reset()
        {
            AdPresenterOverride = null;
            PurchasePresenterOverride = null;
            AdFillOverride = null;
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
        /// Qué hace el anuncio simulado (tarea B2-6): el del test (<see cref="AdFillOverride"/>), si no el del extra
        /// <c>af_ads_fill</c> (solo builds de desarrollo en Android) y, si no, <see cref="FakeAdFill.Fill"/>. Un valor
        /// desconocido se trata como <c>fill</c> y se avisa en el log.
        /// </summary>
        public static FakeAdFill ResolveAdFill()
        {
            if (AdFillOverride.HasValue)
            {
                return AdFillOverride.Value;
            }

            string? value = ReadIntentString(IntentExtraAdsFill);
            if (value == null)
            {
                return FakeAdFill.Fill;
            }

            if (!TryParseAdFill(value, out FakeAdFill fill))
            {
                Debug.LogWarning("[AF.LaunchOptions] " + IntentExtraAdsFill + " desconocido (\"" + value + "\"): se usa fill.");
            }

            return fill;
        }

        /// <summary>Convierte <c>fill</c>, <c>no_fill</c> o <c>error</c> (05 §6.2; modos de <see cref="FakeAdService"/>).</summary>
        public static bool TryParseAdFill(string? value, out FakeAdFill fill)
        {
            switch (value)
            {
                case "fill":
                    fill = FakeAdFill.Fill;
                    return true;
                case "no_fill":
                    fill = FakeAdFill.NoFill;
                    return true;
                case "error":
                    fill = FakeAdFill.Error;
                    return true;
                default:
                    fill = FakeAdFill.Fill;
                    return false;
            }
        }

        /// <summary>
        /// Extras <c>af_seeds</c> y <c>af_canoes</c> de los builds de desarrollo [P] (tarea B2-6): los aplica al perfil antes de
        /// crear la cartera. Devuelve si cambió algo (quien llama guarda el perfil). Fuera de un build de desarrollo en Android
        /// no hace nada.
        /// </summary>
        public static bool ApplyDevWallet(Profile profile, IClock clock)
        {
            int seeds = ReadIntentInt(IntentExtraSeeds, -1);
            int canoes = ReadIntentInt(IntentExtraCanoes, -1);
            return ApplyDevWallet(profile, clock, seeds, canoes, AF.Economy.EconomyConfig.Defaults);
        }

        /// <summary>
        /// Pone <paramref name="seeds"/> semillas y <paramref name="canoes"/> canoas (−1 = no tocar) en el perfil, con el
        /// cronómetro de la próxima canoa en marcha si no están llenas (04 §7.1). Devuelve si cambió algo.
        /// </summary>
        public static bool ApplyDevWallet(Profile profile, IClock clock, int seeds, int canoes, AF.Economy.EconomyConfig config)
        {
            if (profile == null || clock == null || config == null)
            {
                throw new ArgumentNullException(profile == null ? nameof(profile) : clock == null ? nameof(clock) : nameof(config));
            }

            bool changed = false;
            if (seeds >= 0)
            {
                profile.Wallet.Seeds = seeds;
                changed = true;
            }

            if (canoes >= 0)
            {
                int count = Math.Min(canoes, config.CanoesMax);
                CanoesData data = profile.Wallet.Canoes;
                data.Count = count;
                data.NextRegenAt = count < config.CanoesMax ? clock.UtcNow + TimeSpan.FromMinutes(config.CanoesRegenMin) : (DateTime?)null;
                changed = true;
            }

            if (changed)
            {
                profile.EconomyTs = clock.UtcNow;
                Debug.LogWarning("[AF.LaunchOptions] perfil de desarrollo: " + IntentExtraSeeds + " = " + seeds + ", " + IntentExtraCanoes + " = " + canoes + " (−1 = sin cambio).");
            }

            return changed;
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
            return ReadIntentInt(name, 0);
        }

        /// <summary>
        /// Extra de intent entero <paramref name="name"/> (solo Android y solo en builds de desarrollo); <paramref name="missing"/>
        /// si no hay.
        /// </summary>
        public static int ReadIntentInt(string name, int missing)
        {
            if (!Debug.isDebugBuild || Application.platform != RuntimePlatform.Android)
            {
                return missing;
            }

#if UNITY_ANDROID
            try
            {
                using (AndroidJavaObject? intent = CurrentIntent())
                {
                    return intent == null ? missing : intent.Call<int>("getIntExtra", name, missing);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AF.LaunchOptions] no se pudo leer el extra " + name + ": " + e.Message);
                return missing;
            }
#else
            return missing;
#endif
        }

        /// <summary>
        /// Valor del extra de intent <c>af_rules_pour_mode</c> (solo Android y solo en builds de desarrollo); null si no
        /// hay.
        /// </summary>
        public static string? ReadIntentPourMode()
        {
            return ReadIntentString(IntentExtraPourMode);
        }

        /// <summary>Extra de intent de texto <paramref name="name"/> (solo Android y solo en builds de desarrollo); null si no hay.</summary>
        public static string? ReadIntentString(string name)
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
                    return intent == null ? null : intent.Call<string>("getStringExtra", name);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AF.LaunchOptions] no se pudo leer el extra " + name + ": " + e.Message);
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
