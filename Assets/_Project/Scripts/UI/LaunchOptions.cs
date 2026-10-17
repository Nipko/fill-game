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
    /// tableros del tutorial, tarea B1-5; para probar el embudo y los anuncios en el teléfono [P], tarea B2-6:
    /// <c>af_seeds</c>, <c>af_canoes</c> y <c>af_ads_fill</c>; <c>af_font_scale</c> [P], la escala de texto, tarea A1; y el
    /// idioma de desarrollo <c>af_lang</c>, tarea L2b; <c>af_mech_off</c>, el kill switch de mecánicas, tarea KS1; y
    /// <c>af_bench_morpho</c>, el bench de Morpho y de fotogramas, tarea PF1; <c>af_max_level</c>, el progreso para ver el Mapa del río, tarea M3.3;
    /// <c>af_dev_reject</c>, el rechazo comprobable, tarea AU3) y los ganchos de los tests PlayMode (nivel de arranque, modo de vertido,
    /// progreso, perfil y reloj en memoria, desplazamiento, paquete de niveles, escala de texto, idioma y tablas de textos).
    /// <see cref="GameBootstrap"/> los lee al arrancar (la escala, al crear los textos: <see cref="UiBuilder.TextScale"/>); los
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

        /// <summary>
        /// Extra de intent (int) de los builds de desarrollo [P] (tarea A1): la escala de texto global en tanto por ciento
        /// (<c>--ei af_font_scale 130</c>; <see cref="UiBuilder.TextScale"/>), para ver en el teléfono los textos al 130 % de
        /// 02 §12 mientras la escala de Android no llega al juego. Se lee una vez, al crear el primer texto. Nunca en release.
        /// </summary>
        public const string IntentExtraFontScale = "af_font_scale";

        // Escala del extra af_font_scale, leída una vez (NaN: aún no se leyó).
        private static float s_intentFontScale = float.NaN;

        /// <summary>
        /// Extra de intent (string) de los builds de desarrollo (D-018; tarea L2b): el idioma de la UI, primero en la resolución
        /// (dev &gt; guardado &gt; teléfono &gt; en), p. ej. <c>--es af_lang en-XA</c> (pseudolocalización: todo entre corchetes).
        /// Solo vale un idioma con la UI completa o en-XA; si no, el automático. Nunca en release.
        /// </summary>
        public const string IntentExtraLanguage = "af_lang";

        /// <summary>
        /// Extra de intent (string) de los builds de desarrollo (tarea KS1; D-020, pregunta 31 b): apaga los kill switches
        /// <c>mech_enabled.&lt;id&gt;</c> de 08 §9 de la lista separada por comas (<c>--es af_mech_off hoja,matapi</c>). En el
        /// teléfono solo prueba el respaldo sin override: <c>level_overrides</c> sale de la fuente local
        /// (<see cref="LevelOverrides.LocalSource"/>, <c>{}</c>), así que <see cref="LevelCatalog"/> sirve el publicado y escribe el
        /// aviso [P]. La sustitución con un override la prueban los tests EditMode (<see cref="LevelOverridesJsonOverride"/>). Un id
        /// desconocido se ignora y se avisa en el log. Nunca se lee en un build de release.
        /// </summary>
        public const string IntentExtraMechOff = "af_mech_off";

        /// <summary>
        /// Extra de intent (int) de los builds de desarrollo (tarea PF1; 03 §8, 08 §5.4, D-015 y pregunta 44 a): con
        /// <c>--ei af_bench_morpho 1</c>, <c>DevBenchRunner</c> (solo en desarrollo) mide Morpho (<c>SafeMoves</c> con el corte de
        /// <c>hint_timeout_ms</c>) en los tableros con tinaja y en los de Aguas turbias del 100 en adelante, y los fotogramas de
        /// los niveles que se juegan (líneas <c>AF_PERF morpho</c>, <c>morpho_summary</c> y <c>frames</c>). Nunca en release.
        /// </summary>
        public const string IntentExtraBenchMorpho = "af_bench_morpho";

        /// <summary>
        /// Bench de Morpho (<c>af_bench_morpho</c>) forzado por un test: true lo enciende y false lo apaga; null = el extra. Solo
        /// tests; fuera de desarrollo no cuenta (<see cref="ResolveBenchMorpho"/>).
        /// </summary>
        public static bool? BenchMorphoOverride { get; set; }

        /// <summary>
        /// Extra de intent (int) de los builds de desarrollo [P] (tarea M3.3): fija <c>progress.max_level</c> del perfil al
        /// arrancar y deja «Jugar» en el siguiente (<c>--ei af_max_level 58</c>: la pestaña del Mapa del río, con su punto, y el
        /// Mapa con la canoa a 8/10 del tramo de Manaos a Anavilhanas), para probar el Mapa en el teléfono sin jugar los niveles.
        /// Escribe esos números en el perfil del dispositivo de pruebas. Nunca se lee en un build de release.
        /// </summary>
        public const string IntentExtraMaxLevel = "af_max_level";

        /// <summary>
        /// Extra de intent (int) de los builds de desarrollo (tarea AU3; observación 4 de la prueba de la ola 4): con
        /// <c>--ei af_dev_reject 1</c>, <c>tap_switch_selection</c> vale false (02 §3.1, C5), así que tocar como destino una cuia
        /// que no puede recibir el vertido es un rechazo (sacudida y <c>sfx_illegal</c>, sin háptico) en lugar de cambiar la
        /// selección, y se puede provocar con <c>adb shell input tap</c>. Nunca se lee en un build de release.
        /// </summary>
        public const string IntentExtraDevReject = "af_dev_reject";

        /// <summary>
        /// <c>af_dev_reject</c> forzado por un test (true = rechazo en lugar de cambiar la selección); null = el extra. Solo tests;
        /// fuera de desarrollo no cuenta (<see cref="ResolveTapSwitchSelection"/>).
        /// </summary>
        public static bool? DevRejectOverride { get; set; }

        /// <summary>
        /// <c>af_max_level</c> simulado por un test (−1 = no tocar); null = el del intent. Solo tests: ningún extra de intent lo
        /// rellena.
        /// </summary>
        public static int? DevMaxLevelOverride { get; set; }

        /// <summary>
        /// Lista de mecánicas apagadas forzada por un test (el valor de <c>af_mech_off</c>: <c>"hoja,matapi"</c>); null = la del
        /// extra. Solo tests.
        /// </summary>
        public static string? MechOffOverride { get; set; }

        /// <summary>
        /// <c>level_overrides</c> (JSON <c>{slot: level_id}</c>, 08 §9) forzado por un test en lugar de la fuente local
        /// (<see cref="LevelOverrides.LocalSource"/>, vacía). Null = la fuente local. Solo tests: ningún extra de intent lo rellena,
        /// así que en un build de desarrollo <c>af_mech_off</c> no puede ejercitar la sustitución (solo el respaldo con aviso).
        /// </summary>
        public static string? LevelOverridesJsonOverride { get; set; }

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

        /// <summary>
        /// Carpeta (absoluta) del <c>LocalProfileStore</c> real en vez de <c>Application.persistentDataPath</c>: para probar el
        /// almacén del juego tal cual lo crea el arranque (revisión de L3: un perfil más nuevo no se sobrescribe tras elegir
        /// idioma) sin tocar el perfil del Editor. Con este gancho tampoco se migra <c>af_v0_level</c>. Null = la del juego. Solo
        /// tests; <see cref="ProfileStoreOverride"/> manda si están los dos.
        /// </summary>
        public static string? ProfileDirectoryOverride { get; set; }

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
        /// Simula un build de release (false) o de desarrollo (true) para la compuerta del arte de fauna
        /// (<c>GameScreen.FaunaArtAvailable</c>, guion de la intro §4.3 y §11); null = <c>Debug.isDebugBuild</c>. Solo tests:
        /// nada más lo lee.
        /// </summary>
        public static bool? DevelopmentBuildOverride { get; set; }

        /// <summary>
        /// Tarjeta previa de 1 toque (LEVEL_CARD, 02 §10) encendida (true) o apagada (false) por un test; null =
        /// <see cref="GameConfig.LevelCardsEnabledDefault"/>. <c>PlayModeHelpers</c> la pone a false salvo que el test la fije,
        /// así los tests que no tratan de la tarjeta no la ven. Solo tests: ningún extra de intent la rellena.
        /// </summary>
        public static bool? LevelCardsOverride { get; set; }

        /// <summary>
        /// true: el arte del banner de mundo se pinta en el acto al abrirlo, en el hilo principal, para los tests y las capturas
        /// que miden instantes exactos de su entrada; null o false: en un hilo del grupo, como en el juego
        /// (<c>WorldBannerView.AsyncArt</c>). Solo tests: ningún extra de intent la rellena.
        /// </summary>
        public static bool? BannerArtSyncOverride { get; set; }

        /// <summary>
        /// Fallo forzado en el arte del banner de mundo (<c>ProceduralArt.BannerArtFault</c>: en el hilo, en la cola o un
        /// trabajo que no acaba), para probar que el banner entra sin él; null = sin fallo, como en el juego. Solo tests: ningún
        /// extra de intent la rellena.
        /// </summary>
        public static AF.Presentation.BannerArtFault? BannerArtFaultOverride { get; set; }

        /// <summary>
        /// Tiempo límite del arte del banner de mundo (ms de reloj real; <c>WorldBannerView.ArtTimeoutLimitMs</c>); null =
        /// <c>WorldBannerView.ArtTimeoutMs</c> (800 ms [P]). Solo tests: ningún extra de intent la rellena.
        /// </summary>
        public static float? BannerArtTimeoutMsOverride { get; set; }

        /// <summary>
        /// Escala de texto global (<see cref="UiBuilder.TextScale"/>) forzada por un test (tarea A1; 02 §12: 1,3 para el 130 %);
        /// null = la del extra <c>af_font_scale</c> en desarrollo o, si no, 1. Se fija antes de cargar la escena: los textos ya
        /// creados no cambian. Fuera de 1..<see cref="UiBuilder.MaxTextScale"/> se lleva al borde (02 §12: por encima del 130 %,
        /// el tope). Solo tests: ningún extra de intent la rellena.
        /// </summary>
        public static float? FontScaleOverride { get; set; }

        /// <summary>
        /// Idioma de desarrollo forzado por un test (el de <c>af_lang</c>: «en-XA» para la pseudolocalización); null = ninguno.
        /// Solo cuenta en desarrollo (<see cref="Debug.isDebugBuild"/>, también en el Editor).
        /// </summary>
        public static string? LanguageOverride { get; set; }

        /// <summary>
        /// Tablas de textos que sustituyen a los TextAsset de la escena (tarea L2b, punto 4): para arrancar sin tablas o con una
        /// rota y comprobar que el juego construye la UI igual (<see cref="LocTables.FromTextsOrEmpty"/>). Null = las de la escena.
        /// Solo tests: ningún extra de intent lo rellena.
        /// </summary>
        public static LocTableTexts? LocTablesOverride { get; set; }

        /// <summary>
        /// Idioma del teléfono que sustituye a la lectura de la plataforma (<see cref="AndroidDeviceLocale.ForPlatform"/>; tarea L3):
        /// los tests con <c>ManualDeviceLocale</c> (un teléfono en pt-BR, sin región…). Null = el de la plataforma. Solo tests:
        /// ningún extra de intent lo rellena. <c>PlayModeHelpers</c> pone uno vacío si el test no fija ninguno, para que los
        /// textos y las cifras no dependan de la cultura del equipo.
        /// </summary>
        public static AF.Platform.IDeviceLocale? DeviceLocaleOverride { get; set; }

        /// <summary>
        /// Extras <c>af_seeds</c> y <c>af_canoes</c> simulados por un test (−1 = no tocar; revisión de L3: volver al inicio tras
        /// elegir idioma no los vuelve a aplicar). Null = los del intent. Solo tests.
        /// </summary>
        public static (int Seeds, int Canoes)? DevWalletOverride { get; set; }

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
            ProfileDirectoryOverride = null;
            ClockOverride = null;
            LevelOffsetOverride = null;
            LevelPackJsonOverride = null;
            DevelopmentBuildOverride = null;
            LevelCardsOverride = null;
            BannerArtSyncOverride = null;
            BannerArtFaultOverride = null;
            BannerArtTimeoutMsOverride = null;
            FontScaleOverride = null;
            LanguageOverride = null;
            LocTablesOverride = null;
            DeviceLocaleOverride = null;
            DevWalletOverride = null;
            MechOffOverride = null;
            LevelOverridesJsonOverride = null;
            BenchMorphoOverride = null;
            DevMaxLevelOverride = null;
            DevRejectOverride = null;
        }

        /// <summary>
        /// <c>tap_switch_selection</c> de la partida (02 §3.1, C5): el de <see cref="GameConfig.TapSwitchSelection"/> salvo en
        /// desarrollo con <c>af_dev_reject</c> = 1 (o <see cref="DevRejectOverride"/> en un test), que lo pone a false para poder
        /// provocar un rechazo (tarea AU3). Fuera de desarrollo, siempre el de <see cref="GameConfig"/>.
        /// </summary>
        public static bool ResolveTapSwitchSelection()
        {
            if (!Debug.isDebugBuild)
            {
                return GameConfig.TapSwitchSelection;
            }

            bool reject = DevRejectOverride ?? ReadIntentInt(IntentExtraDevReject) == 1;
            return reject ? false : GameConfig.TapSwitchSelection;
        }

        /// <summary>
        /// El bench de Morpho y de fotogramas (tarea PF1) arranca: el del test (<see cref="BenchMorphoOverride"/>) o, si no, el
        /// extra <c>af_bench_morpho</c> = 1 (solo builds de desarrollo en Android). Fuera de desarrollo, siempre false.
        /// </summary>
        public static bool ResolveBenchMorpho()
        {
            if (!Debug.isDebugBuild)
            {
                return false;
            }

            return BenchMorphoOverride ?? ReadIntentInt(IntentExtraBenchMorpho) == 1;
        }

        /// <summary>
        /// Kill switches de la partida (tarea KS1): los defaults compilados de <c>mech_enabled.*</c>
        /// (<see cref="GameConfig.MechEnabled"/>, todos true) con las mecánicas apagadas por el test (<see cref="MechOffOverride"/>)
        /// o, si no, por el extra <c>af_mech_off</c> (solo builds de desarrollo en Android). Un id desconocido se ignora y se avisa.
        /// </summary>
        public static MechanicSwitches ResolveMechSwitches()
        {
            string? list = MechOffOverride ?? ReadIntentString(IntentExtraMechOff);
            MechanicSwitches switches = GameConfig.MechEnabled.WithOffList(list, out string? unknown);
            if (unknown != null)
            {
                Debug.LogWarning("[AF.LaunchOptions] " + IntentExtraMechOff + ": mecánicas desconocidas (\"" + unknown + "\"); se ignoran.");
            }

            if (switches.AnyOff)
            {
                Debug.LogWarning("[AF.LaunchOptions] " + switches + " (" + IntentExtraMechOff + ", KS1).");
            }

            return switches;
        }

        /// <summary>
        /// Texto de <c>level_overrides</c> de la partida (tarea KS1): el del test (<see cref="LevelOverridesJsonOverride"/>) o, si
        /// no, la fuente local (<see cref="LevelOverrides.LocalSource"/>, <c>{}</c>) hasta que exista <c>AF.RemoteConfig</c>.
        /// </summary>
        public static string ResolveLevelOverridesJson()
        {
            return LevelOverridesJsonOverride ?? LevelOverrides.LocalSource;
        }

        /// <summary>
        /// Idioma de desarrollo (D-018; tarea L2b): el del test (<see cref="LanguageOverride"/>) o, si no, el del extra
        /// <c>af_lang</c> (solo builds de desarrollo en Android); null si no hay. Fuera de desarrollo, siempre null.
        /// </summary>
        public static string? ResolveDevLanguage()
        {
            if (!Debug.isDebugBuild)
            {
                return null;
            }

            string? value = LanguageOverride ?? ReadIntentString(IntentExtraLanguage);
            return string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
        }

        /// <summary>
        /// Escala de texto global de la partida (<see cref="UiBuilder.TextScale"/>): la del test (<see cref="FontScaleOverride"/>),
        /// si no la del extra <c>af_font_scale</c> (solo builds de desarrollo en Android, leída una vez) y, si no, 1; siempre
        /// entre 1 y <see cref="UiBuilder.MaxTextScale"/> (<see cref="ClampTextScale"/>). Sin asignaciones tras la primera vez.
        /// </summary>
        public static float ResolveTextScale()
        {
            if (FontScaleOverride.HasValue)
            {
                return ClampTextScale(FontScaleOverride.Value);
            }

            if (float.IsNaN(s_intentFontScale))
            {
                int percent = ReadIntentInt(IntentExtraFontScale, 0);
                s_intentFontScale = percent > 0 ? ClampTextScale(percent / 100f) : 1f;
                if (percent > 0)
                {
                    Debug.LogWarning("[AF.LaunchOptions] " + IntentExtraFontScale + " = " + percent + ": escala de texto " + s_intentFontScale.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ".");
                }
            }

            return s_intentFontScale;
        }

        /// <summary>
        /// La escala de texto dentro de 1..<see cref="UiBuilder.MaxTextScale"/> (02 §12: por encima del 130 %, el tope; por
        /// debajo de 1 los textos bajarían de 12 sp); un valor no finito, 1.
        /// </summary>
        public static float ClampTextScale(float scale)
        {
            if (float.IsNaN(scale) || float.IsInfinity(scale))
            {
                return 1f;
            }

            return Mathf.Clamp(scale, 1f, UiBuilder.MaxTextScale);
        }

        /// <summary>
        /// La partida enseña la tarjeta previa (<see cref="LevelFlow.LevelCardsEnabled"/>): la del test
        /// (<see cref="LevelCardsOverride"/>) o, si no, <see cref="GameConfig.LevelCardsEnabledDefault"/>.
        /// </summary>
        public static bool ResolveLevelCards()
        {
            return LevelCardsOverride ?? GameConfig.LevelCardsEnabledDefault;
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
        /// no hace nada, salvo con <see cref="DevWalletOverride"/> (tests).
        /// </summary>
        public static bool ApplyDevWallet(Profile profile, IClock clock)
        {
            (int Seeds, int Canoes)? simulated = DevWalletOverride;
            int seeds = simulated?.Seeds ?? ReadIntentInt(IntentExtraSeeds, -1);
            int canoes = simulated?.Canoes ?? ReadIntentInt(IntentExtraCanoes, -1);
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
        /// Extra <c>af_max_level</c> de los builds de desarrollo [P] (tarea M3.3): lo aplica al perfil antes de crear el progreso.
        /// Devuelve si cambió algo (quien llama guarda el perfil). Fuera de un build de desarrollo en Android no hace nada, salvo
        /// con <see cref="DevMaxLevelOverride"/> (tests).
        /// </summary>
        public static bool ApplyDevProgress(Profile profile)
        {
            int maxLevel = DevMaxLevelOverride ?? ReadIntentInt(IntentExtraMaxLevel, -1);
            return ApplyDevProgress(profile, maxLevel);
        }

        /// <summary>
        /// Pone <c>progress.max_level</c> = <paramref name="maxLevel"/> (recortado a 1.000; −1 = no tocar) y
        /// <c>progress.next_level</c> en el siguiente (como mucho el 1.000). Un nivel en curso guardado de otro nivel se quita, para
        /// que el arranque vaya al inicio con «Jugar» en ese nivel. <c>map_seen_level</c> no cambia (se lee recortado a
        /// <c>max_level</c>): con un perfil limpio, la pestaña sale con su punto. Devuelve si cambió algo.
        /// </summary>
        public static bool ApplyDevProgress(Profile profile, int maxLevel)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (maxLevel < 0)
            {
                return false;
            }

            int max = Math.Min(maxLevel, WorldTable.LastLevel);
            ProgressData progress = profile.Progress;
            progress.MaxLevel = max;
            progress.NextLevel = Math.Min(max + 1, WorldTable.LastLevel);
            if (profile.LevelState != null && profile.LevelState.Level != progress.NextLevel)
            {
                profile.LevelState = null;
            }

            Debug.LogWarning("[AF.LaunchOptions] perfil de desarrollo: " + IntentExtraMaxLevel + " = " + max + " (siguiente nivel " + progress.NextLevel + ").");
            return true;
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
