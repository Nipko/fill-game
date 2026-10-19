#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Ads;
using AF.Boosters;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.Levels;
using AF.Localization;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace AF.UI
{
    /// <summary>
    /// Arranque de la escena <c>Game</c> (docs/dev/prototipo-v0.md c; 08 §2.3 en lo que ya existe): lee los paquetes de
    /// niveles (TextAsset) y arma el catálogo (tarea B1-5), carga el perfil local (<c>AF.Save</c>, migrando una vez el
    /// <c>af_v0_level</c> de v0), crea la cartera, los anuncios y la tienda simulados (<c>AF.Ads</c> y <c>AF.IAP</c>, sin SDK:
    /// D-008), genera los gráficos provisionales, construye las pantallas de inicio y de nivel, el <see cref="LevelFlow"/> y el
    /// <see cref="AppFlow"/> y los hace avanzar cada fotograma. Configura los textos (<see cref="Loc"/>, con las tablas de
    /// <c>Data/Loc/</c>; tarea L2b) después de leer el perfil y antes de construir la UI. La primera vez entra directamente al
    /// nivel 1; con un nivel en curso guardado (<c>level_state</c>, tarea B2-5), lo reabre tal cual; si no, la pantalla de
    /// inicio. Conecta AD_GATE con los anuncios simulados (<see cref="InterstitialGate"/>, tarea B2-4) y la sonda de desarrollo
    /// con cada anuncio y compra. Sus métodos públicos de input (<see cref="IGameActions"/>) son los mismos que llaman los
    /// toques y los tests PlayMode.
    /// <para>Ajustes (tarea L3; <see cref="SettingsScreen"/>): se abre desde el engranaje del inicio y se cierra con su X o con el
    /// Atrás de Android. El idioma se resuelve al arrancar con el de desarrollo (<c>af_lang</c>) &gt; <c>settings.language</c>
    /// &gt; el del teléfono (<see cref="AndroidDeviceLocale"/>; en el Editor, <see cref="SystemDeviceLocale"/>) &gt; en, con el
    /// respaldo es-419 [P], y las cifras con la región del teléfono (es-CO sin ella [P]). Elegir un idioma lo guarda y, si cambia
    /// el de la UI, <see cref="Loc.SetLocale"/> lanza <see cref="Loc.Changed"/> (las cachés de <see cref="UiText"/> se vacían) y
    /// la UI entera se reconstruye volviendo al inicio: la escena se recarga y el arranque siguiente la vuelve a construir en el
    /// idioma nuevo (Q7-A), con Ajustes otra vez abierta (FXT). Los Patrones de fruta se guardan y se aplican al momento
    /// (<see cref="GameScreen.RefreshFruitPatterns"/>).</para>
    /// <para>Mapa del río (D-013; tarea M3.3; <see cref="RiverMapScreen"/>): con la geometría (<c>river_map_v1.json</c>) y las
    /// paradas en la escena, la pestaña del inicio sale desde <c>max_level</c> ≥ 10 y lo abre en la vista Mundo
    /// (<see cref="OpenRiverMap"/>); el ← de su cabecera y el Atrás de Android lo cierran (<see cref="CloseRiverMap"/>, que anota
    /// lo visto en <c>map_seen_level</c>) y su «Jugar» cierra y abre el nivel. La geometría se lee y se decodifica la primera vez
    /// que se abre (una sola vez por arranque); si no valida, se avisa y la pestaña desaparece.</para>
    /// <para>Secuencia del Mapa del 10 (06 §2.3; P-M4; tarea M4.2; <see cref="L10SequenceRule"/>): con el Mapa en la escena,
    /// «Siguiente» en la victoria del 10 pasa por AD_GATE y va al inicio, y cada entrada al inicio con <c>max_level</c> ≥ 10 y
    /// <c>map_seen_level</c> &lt; 10 (también tras Inicio en esa victoria o tras cerrar la app) abre encima el Mapa desde
    /// <see cref="MapOpenSource.L10"/> (<see cref="OnAppScreenChanged"/>), que hace la secuencia
    /// (<c>RiverMapScreen.Sequence.cs</c>). Al acabarla o saltarla se anota lo visto hasta el 10 (<see cref="MarkMapSeen"/>), así
    /// que sale una vez. Su «Jugar» abre el 11 y su ← deja el inicio. Los tests la apagan con
    /// <see cref="L10SequenceOverride"/>. Mientras el Mapa (opaco) está encima del inicio, la ventanita de casa no se ve
    /// (<see cref="HomeWindowCovered"/>): el saludo del día de TiTino (como mucho 1; D-016, punto 5) espera a que se cierre y
    /// no se gasta debajo de la secuencia sin verse.</para>
    /// <para>Más entradas del Mapa (D-013, punto 7; tarea M4.3; <c>RiverMapScreen.Modes.cs</c>): la pausa › «Mapa del río» lo
    /// abre encima del nivel (<see cref="MapOpenSource.Pause"/>, desde <c>max_level</c> ≥ 10), con «Volver al nivel», sin salir
    /// de él (04 §5.2: ni canoa, ni Crecida, ni <c>fails_level</c>); «Ver mapa» de la victoria del <c>10k</c>, encima de la
    /// victoria (<see cref="MapOpenSource.Victory"/>); y, al ganar el último nivel publicado, «Siguiente» lleva a «El río sigue
    /// creciendo» con el Mapa encima (<see cref="MapOpenSource.ContentEnd"/>; <see cref="AppFlow.ContentEndMapDue"/>), que hace
    /// el viaje del último tramo: al cerrarlo queda la pantalla de siempre. Los tests apagan el del fin de contenido con
    /// <see cref="ContentEndMapOverride"/>. Si la app deja la pantalla de debajo por otro camino, el Mapa se cierra antes.</para>
    /// <para>Pantalla de arranque (06 §2.2; D-016, T2-A; tarea TI6d; <see cref="BootSplash"/>): en cada arranque en frío, el río y
    /// el logotipo salen en el primer fotograma, antes de cargar nada, y TiTino entra en su embaúba en cuanto hay perfil y arte
    /// (<c>sloth_wake</c> en la primera apertura, <c>sloth_idle</c> en las demás; sin su arte final, en release, no sale). Al
    /// terminar la carga se corta y arranca <see cref="AppFlow"/>; solo en la primera apertura con TiTino se espera a que su
    /// gesto lleve 1,0 s a la vista, si cabe en el objetivo de 20 s del primer toque (<see cref="BootSplashTiming"/>). La sonda
    /// <c>AF_STATE boot</c> (<see cref="BootInfo"/>) lo mide, con el primer toque del jugador.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class GameBootstrap : MonoBehaviour, IAppFlowListener, IGameActions
    {
        /// <summary>Tope del paso de tiempo por fotograma (evita saltos tras un tirón).</summary>
        private const float MaxDeltaMs = 100f;

        private const string LogPrefix = "[AF.GameBootstrap] ";

        [SerializeField]
        private TextAsset? levelPack;

        [SerializeField]
        private TextAsset[] extraLevelPacks = Array.Empty<TextAsset>();

        [SerializeField]
        private TextAsset? faunaCatalog;

        [SerializeField]
        private TextAsset? riverStops;

        [SerializeField]
        private TextAsset? riverMap;

        [SerializeField]
        private TextAsset? fruitCalendar;

        [SerializeField]
        private TextAsset? seasonTable;

        [SerializeField]
        private TextAsset? stringsTable;

        [SerializeField]
        private TextAsset? factsTable;

        private static ProceduralArt? s_art;
        private static string? s_artKey;

        private ProceduralArt? _art;
        private GameScreen? _screen;
        private LobbyScreen? _lobby;
        private LevelFlow? _flow;
        private AppFlow? _app;
        private Wallet? _wallet;
        private AdController? _ads;
        private IStoreService? _store;
        private StoreLedger? _storeLedger;
        private GameServices? _services;
        private AdOverlay? _adOverlay;
        private StoreSheet? _storeSheet;
        private Profile? _profile;
        private IProfileStore? _profileStore;
        private ProfileSaveBatch? _saveBatch;
        private IDeviceLocale? _deviceLocale;
        private string _deviceTag = string.Empty;
        private SettingsScreen? _settingsScreen;
        private RiverMapScreen? _mapScreen;
        private IProgressStore? _progress;
        private RiverMap? _riverMapData;
        private RiverRoutes? _riverRoutes;
        private bool _riverMapFailed;

        // Reconstrucción de la UI tras elegir idioma (tarea L3): la escena se recarga y el arranque siguiente va al inicio con el
        // mismo perfil en memoria (ajustes, cartera y progreso de la sesión), aunque no se haya podido guardar (perfil en solo
        // lectura, 08 §4.4, o una escritura fallida), y con el mismo almacén: un LocalProfileStore nuevo no habría hecho Load y
        // no estaría bloqueado, así que el perfil en blanco de la sesión pisaría el profile.json más nuevo (08 §4.4: «no se
        // sobrescribe»; revisión de L3). Solo se reutiliza si es el almacén de este arranque (el del test o el de la misma
        // carpeta): una reconstrucción que no llegó a arrancar no se cuela en el arranque de otro test con otro almacén.
        private static bool s_rebuildToLobby;
        private static Profile? s_rebuildProfile;
        private static ProfileLoadStatus s_rebuildStatus;
        private static IProfileStore? s_rebuildStore;

        // Ajustes estaba abierta al elegir el idioma (FXT, observación 3 de la prueba de la ola 1): tras la reconstrucción sigue
        // abierta, en el idioma nuevo, sobre el inicio.
        private static bool s_rebuildReopenSettings;

        // Atrás de Android (Escape en el Input System; FXT, defecto 1 de la prueba de la ola 1): una acción enlazada a
        // <Keyboard>/escape, habilitada desde el arranque, cuenta cada pulsación en su evento. Leer
        // Keyboard.current.escapeKey.wasPressedThisFrame la perdía en frío: en Android el teclado aparece con el primer evento de
        // tecla, adb (y el sistema) mandan la bajada y la subida juntas, y la primera consulta de wasPressedThisFrame de un botón
        // compara el valor de ahora (ya suelto) con el del fotograma anterior (ButtonControl, Input System 1.20): solo desde la
        // segunda consulta sigue las pulsaciones de cada fotograma.
        private InputAction? _backAction;
        private int _backPresses;

        // Fallos del escritor de segundo plano ya registrados (T-D): lo recoja quien lo recoja, cada uno se avisa una vez.
        private int _reportedBackgroundFailures;
        private DevProbe? _probe;
        private BootSplash? _splash;
        private BootReport? _bootReport;
        private double _bootOrigin;
        private ProfileLockedNotice? _lockedNotice;
        private ProfileLoadStatus _profileStatus = ProfileLoadStatus.NotFound;
        private float _msCarry;
        private bool _skipNextDelta;

        /// <summary>Instancia activa (una por escena).</summary>
        public static GameBootstrap? Current { get; private set; }

        /// <summary>
        /// Secuencia del Mapa del 10 (tarea M4.2) encendida (true) o apagada (false) por un test; null = la del juego (encendida con
        /// el Mapa en la escena). Los tests PlayMode que no tratan de ella la apagan (<c>PlayModeHelpers.NoL10SequenceUnlessSet</c>):
        /// con <c>max_level</c> ≥ 10 y <c>map_seen_level</c> 0 entran al inicio y ganan el 10 como antes de que existiera.
        /// </summary>
        public static bool? L10SequenceOverride { get; set; }

        /// <summary>
        /// El Mapa del fin de contenido (tarea M4.3) encendido (true) o apagado (false) por un test; null = el del juego (encendido
        /// con el Mapa en la escena). Los tests PlayMode que no tratan de él lo apagan (<c>PlayModeHelpers.NoContentEndMapUnlessSet</c>):
        /// tras ganar el último nivel publicado, «El río sigue creciendo» sale sola, como antes de que existiera.
        /// </summary>
        public static bool? ContentEndMapOverride { get; set; }

        /// <summary>
        /// Pantalla de arranque (06 §2.2; tarea TI6d) mientras se ve: null antes de crearla, después de cortarla y en los
        /// arranques sin ella (<see cref="BootSplash.EnabledOverride"/>, reconstrucción por idioma).
        /// </summary>
        public BootSplash? Splash => _splash;

        /// <summary>
        /// Lo que pasó en el arranque con la pantalla de arranque (sonda <c>AF_STATE boot</c>; tarea TI6d): null hasta cortarla
        /// y en los arranques sin ella.
        /// </summary>
        public BootReport? BootInfo => _bootReport;

        /// <summary>Flujo del nivel (null hasta <c>Start</c>).</summary>
        public LevelFlow? Flow => _flow;

        /// <summary>Navegación entre pantallas (null hasta <c>Start</c>).</summary>
        public AppFlow? App => _app;

        /// <summary>Pantalla de nivel (null hasta <c>Start</c>).</summary>
        public GameScreen? View => _screen;

        /// <summary>Pantalla de inicio (null hasta <c>Start</c>).</summary>
        public LobbyScreen? Lobby => _lobby;

        /// <summary>Cartera (null hasta <c>Start</c>).</summary>
        public Wallet? Wallet => _wallet;

        /// <summary>Anuncios simulados (null hasta <c>Start</c>).</summary>
        public AdController? Ads => _ads;

        /// <summary>Tienda simulada (null hasta <c>Start</c>).</summary>
        public IStoreService? Store => _store;

        /// <summary>Servicios del embudo (null hasta <c>Start</c>).</summary>
        public GameServices? Services => _services;

        /// <summary>Pantalla «Anuncio simulado» del juego (null con un presentador de test).</summary>
        internal AdOverlay? AdOverlayView => _adOverlay;

        /// <summary>Hoja «Compra simulada» del juego (null con un presentador de test).</summary>
        internal StoreSheet? StoreSheetView => _storeSheet;

        /// <summary>Perfil cargado (null hasta <c>Start</c>).</summary>
        public Profile? Profile => _profile;

        /// <summary>Almacén del perfil (null hasta <c>Start</c>).</summary>
        public IProfileStore? ProfileStore => _profileStore;

        /// <summary>
        /// Lote de guardado que envuelve <see cref="ProfileStore"/>: todo lo que escribe el perfil pasa por él y cada acción del
        /// jugador lo escribe una sola vez (null hasta <c>Start</c>).
        /// </summary>
        public ProfileSaveBatch? SaveBatch => _saveBatch;

        /// <summary>
        /// El perfil del dispositivo no se puede usar ni pisar (<see cref="ProfileLoadStatus.TooNew"/> o
        /// <see cref="ProfileLoadStatus.MigrationFailed"/>; 08 §4.4): se juega con un perfil en blanco que no se guarda, se avisa
        /// con «Actualiza Tintalú» y no se ofrecen compras (medida mínima [P]; la pantalla de bloqueo la decide el dueño).
        /// </summary>
        public bool ProfileLocked => IsLocked(_profileStatus);

        /// <summary>Resultado de la carga del perfil.</summary>
        public ProfileLoadStatus ProfileStatus => _profileStatus;

        /// <summary>Aviso «Actualiza Tintalú» del perfil en solo lectura (null si el perfil se cargó bien).</summary>
        internal ProfileLockedNotice? LockedNotice => _lockedNotice;

        /// <summary>Primer paquete de niveles asignado.</summary>
        public TextAsset? LevelPackAsset => levelPack;

        /// <summary>Paquetes de niveles asignados, en orden (escena generada por <c>GameSceneBuilder</c>).</summary>
        public IReadOnlyList<TextAsset> LevelPackAssets
        {
            get
            {
                var list = new List<TextAsset>();
                if (levelPack != null)
                {
                    list.Add(levelPack);
                }

                for (int i = 0; i < extraLevelPacks.Length; i++)
                {
                    if (extraLevelPacks[i] != null)
                    {
                        list.Add(extraLevelPacks[i]);
                    }
                }

                return list;
            }
        }

        /// <summary>Catálogo de miquitos asignado (<c>miquitos_v0.json</c>; escena generada por <c>GameSceneBuilder</c>).</summary>
        public TextAsset? FaunaCatalogAsset => faunaCatalog;

        /// <summary>Catálogo de miquitos leído al arrancar (null = la intro siempre en modo dosel).</summary>
        public FaunaCatalog? Fauna { get; private set; }

        /// <summary>Asigna el catálogo de miquitos de la intro (escena generada por <c>GameSceneBuilder</c>).</summary>
        public void ConfigureFauna(TextAsset? catalog)
        {
            faunaCatalog = catalog;
        }

        /// <summary>Paradas del Mapa del río asignadas (<c>river_stops_v1.json</c>; escena generada por <c>GameSceneBuilder</c>).</summary>
        public TextAsset? RiverStopsAsset => riverStops;

        /// <summary>
        /// Paradas del Mapa del río leídas al arrancar (<c>af-river-stops/1</c>; D-013; 08 §4.5). Null si falta el archivo o no
        /// valida: se avisa y el juego sigue igual, sin el Mapa.
        /// </summary>
        public RiverStopCatalog? RiverStops { get; private set; }

        /// <summary>Asigna las paradas del Mapa del río (escena generada por <c>GameSceneBuilder</c>).</summary>
        public void ConfigureRiverStops(TextAsset? stops)
        {
            riverStops = stops;
        }

        /// <summary>Geometría del Mapa del río asignada (<c>river_map_v1.json</c>; escena generada por <c>GameSceneBuilder</c>).</summary>
        public TextAsset? RiverMapAsset => riverMap;

        /// <summary>Asigna la geometría del Mapa del río (escena generada por <c>GameSceneBuilder</c>).</summary>
        public void ConfigureRiverMap(TextAsset? map)
        {
            riverMap = map;
        }

        /// <summary>
        /// Geometría del Mapa del río (<c>af-river-map/1</c>; 08 §4.5), leída y decodificada la primera vez que se abre el Mapa
        /// (null hasta entonces, o si falta o no valida).
        /// </summary>
        public RiverMap? RiverMapData => _riverMapData;

        /// <summary>Rutas de la canoa entre paradas (los campos <c>at</c> y <c>leg</c> de las paradas), leídas con la geometría.</summary>
        public RiverRoutes? RiverRoutes => _riverRoutes;

        /// <summary>
        /// El Mapa del río se puede abrir: la escena trae su geometría y las paradas (válidas) y la geometría no falló al leerse.
        /// </summary>
        public bool RiverMapAvailable => riverMap != null && RiverStops != null && !_riverMapFailed;

        /// <summary>Pantalla del Mapa del río (null hasta <c>Start</c>).</summary>
        public RiverMapScreen? RiverMapView => _mapScreen;

        /// <summary>Calendario de Frutas del río asignado (<c>frutas_del_rio_v1.json</c>; escena generada por <c>GameSceneBuilder</c>).</summary>
        public TextAsset? FruitCalendarAsset => fruitCalendar;

        /// <summary>
        /// Calendario de Frutas del río leído al arrancar (<c>af-facts/1</c>; D-017; 08 §4.6), con la línea de cada nivel. Null si
        /// falta el archivo o no valida: se avisa y el juego sigue igual, sin líneas de fruta.
        /// </summary>
        public FactSchedule? FruitFacts { get; private set; }

        /// <summary>Asigna el calendario de Frutas del río (escena generada por <c>GameSceneBuilder</c>).</summary>
        public void ConfigureFruitCalendar(TextAsset? calendar)
        {
            fruitCalendar = calendar;
        }

        /// <summary>Tabla de temporada asignada (<c>seasons_v1.json</c>; escena generada por <c>GameSceneBuilder</c>; SE2).</summary>
        public TextAsset? SeasonTableAsset => seasonTable;

        /// <summary>
        /// Tabla de temporada leída al arrancar (<c>af-seasons/1</c>; pregunta 32 b, D-020; 03 §12; tarea SE2): la LUT de aguas
        /// altas o bajas de cada ciclo de 20 en el fondo del tablero y del inicio. Null si falta el archivo o no valida: se avisa y
        /// el juego sigue sin LUT de temporada, como antes.
        /// </summary>
        public SeasonTable? Seasons { get; private set; }

        /// <summary>Asigna la tabla de temporada (escena generada por <c>GameSceneBuilder</c>).</summary>
        public void ConfigureSeasons(TextAsset? table)
        {
            seasonTable = table;
        }

        /// <summary>Tabla de textos de la UI asignada (<c>strings.csv</c>; escena generada por <c>GameSceneBuilder</c>).</summary>
        public TextAsset? StringsTableAsset => stringsTable;

        /// <summary>Tabla de textos largos asignada (<c>facts.csv</c>; escena generada por <c>GameSceneBuilder</c>).</summary>
        public TextAsset? FactsTableAsset => factsTable;

        /// <summary>
        /// Idioma de la UI resuelto (al arrancar o al elegirlo en Ajustes) y de dónde salió (null hasta <c>Start</c>; sonda
        /// <c>AF_STATE loc</c>).
        /// </summary>
        public LocResolution? Locale { get; private set; }

        /// <summary>
        /// Idioma del teléfono en uso (<see cref="LaunchOptions.DeviceLocaleOverride"/> o <see cref="AndroidDeviceLocale.ForPlatform"/>;
        /// null hasta <c>Start</c>).
        /// </summary>
        public IDeviceLocale? DeviceLocale => _deviceLocale;

        /// <summary>Etiqueta que dio el teléfono al arrancar (<c>""</c> si no dio ninguna; sonda <c>AF_STATE loc</c>).</summary>
        public string DeviceTag => _deviceTag;

        /// <summary>Pantalla de Ajustes (null hasta <c>Start</c>).</summary>
        internal SettingsScreen? SettingsView => _settingsScreen;

        /// <summary>Asigna las tablas de textos (D-018; 08 §4.7; escena generada por <c>GameSceneBuilder</c>).</summary>
        public void ConfigureLocalization(TextAsset? strings, TextAsset? facts)
        {
            stringsTable = strings;
            factsTable = facts;
        }

        /// <summary>Asigna el paquete de niveles (escena generada por <c>GameSceneBuilder</c>).</summary>
        public void Configure(TextAsset pack)
        {
            Configure(new[] { pack });
        }

        /// <summary>
        /// Asigna los paquetes de niveles (08 §2.2: índice nivel → paquete; <c>tutorial_v0</c> y <c>mundo_02</c> … <c>mundo_06</c>,
        /// 1-300 desde C1a). El primero va en <c>levelPack</c> y el resto en <c>extraLevelPacks</c>.
        /// </summary>
        public void Configure(IReadOnlyList<TextAsset> packs)
        {
            if (packs == null || packs.Count == 0)
            {
                throw new ArgumentException("Hace falta al menos un paquete de niveles.", nameof(packs));
            }

            levelPack = packs[0];
            var extra = new TextAsset[packs.Count - 1];
            for (int i = 1; i < packs.Count; i++)
            {
                extra[i - 1] = packs[i];
            }

            extraLevelPacks = extra;
        }

        /// <summary>
        /// Toque en una cuia (02 §3.1). Tras un vertido, la sonda de desarrollo escribe lo que costó guardar (AF_PERF, T-D):
        /// <c>save_ms</c> es solo el hilo principal (serializar y entregar al escritor, o la escritura síncrona si la hubo:
        /// el vertido que gana es fin de nivel y sale con <c>sync</c> = true, sin salir del cómputo) y <c>bg_ms</c>, la última
        /// escritura terminada del escritor.
        /// </summary>
        public TapResult TapCuia(int index)
        {
            if (_flow == null || !InLevel)
            {
                return TapResult.Ignored(index, -1);
            }

            NoteFirstTouch(AppScreen.Level);
            ProfileSaveBatch? saves = _saveBatch;
            int writes = saves?.WriteCount ?? 0;
            TapResult result = _flow.TapCuia(index);
            if (saves != null)
            {
                ReportBackgroundFailures(saves);
            }

            if (result.Kind == TapKind.Poured && saves != null && DevProbe.Enabled)
            {
                int count = saves.WriteCount - writes;
                _probe?.SaveCost("pour", _flow.LevelNumber, count, saves.LastWriteMs, saves.LastBackgroundMs, count > 0 && !saves.LastWriteInBackground);
            }

            return result;
        }

        /// <summary>Toque en el fondo (C7).</summary>
        public void TapBackground()
        {
            if (InLevel && _flow != null)
            {
                NoteFirstTouch(AppScreen.Level);
                _flow.TapBackground();
            }
        }

        /// <summary>Toque en cualquier punto durante la intro o la celebración.</summary>
        public bool TapAnywhere()
        {
            if (_flow == null || !InLevel)
            {
                return false;
            }

            NoteFirstTouch(AppScreen.Level);
            return _flow.TapAnywhere();
        }

        /// <summary>
        /// Marea (bandeja o modal). C8: primero quita la selección (<see cref="LevelFlow.PressUndo"/>). Si la Luciérnaga aún
        /// está revelando, termina antes (la vista salta a lo revelado).
        /// </summary>
        public bool PressUndo()
        {
            if (_flow == null || !InLevel)
            {
                return false;
            }

            _screen?.SettleReveal();
            return _flow.PressUndo();
        }

        /// <summary>
        /// Volver a la orilla (cabecera o modal). C8: primero quita la selección (<see cref="LevelFlow.PressRestart"/>). Si la
        /// Luciérnaga aún está revelando, termina antes de que empiece la intro.
        /// </summary>
        public bool PressRestart()
        {
            if (_flow == null || !InLevel)
            {
                return false;
            }

            _screen?.SettleReveal();
            return _flow.PressRestart();
        }

        /// <summary>Siguiente (pantalla de victoria): N + 1, «El río sigue creciendo» o Sin canoas (<see cref="AppFlow.PressNext"/>).</summary>
        public bool PressNext()
        {
            return _app != null && _app.PressNext();
        }

        /// <summary>«Inicio» (pantalla de victoria) [P]: vuelve a la pantalla de inicio.</summary>
        public bool PressHome()
        {
            return _app != null && _app.PressHome();
        }

        /// <summary>
        /// «Cosecha doble: x2 por video» (<c>rv_double_harvest</c>, desde el 11; 04 §2.8): al terminar el anuncio, otra Cosecha
        /// igual con <c>Wallet.Apply</c>. False si no se ofrece o no hay video (sin fill el botón ni aparece).
        /// </summary>
        public bool PressDoubleHarvest()
        {
            LevelFlow? flow = _flow;
            AdController? ads = _ads;
            if (flow == null || ads == null || !InLevel || !flow.DoubleHarvestOffered || !ads.CheckRewarded(AdPlacement.DoubleHarvest).IsAllowed)
            {
                return false;
            }

            ads.ShowRewarded(AdPlacement.DoubleHarvest, r =>
            {
                if (r.IsRewarded)
                {
                    flow.ApplyDoubleHarvest();
                }
            });
            return true;
        }

        /// <summary>
        /// «+1 cuia» de la bandeja (04 §2.2): del stock o por 150 semillas. Solo con el tablero en juego (la oferta solo está
        /// lista con <see cref="LevelFlow.AcceptsBoardInput"/>): con la victoria o el bloqueo pendiente, la bandeja sigue
        /// habilitada (D-011, punto 1), pero el toque no hace nada, tampoco terminar antes de tiempo el revelado en marcha.
        /// </summary>
        public bool PressExtraCuia()
        {
            LevelFlow? flow = InLevel ? _flow : null;
            if (flow == null || !flow.AcceptsBoardInput)
            {
                return false;
            }

            _screen?.SettleReveal();
            TransactionResult? result = flow.UseExtraCuia();
            LogFunnelError(flow, result, "Cuia extra");
            return result != null && result.IsApplied;
        }

        /// <summary>
        /// Revelar · Luciérnaga de la bandeja (04 §2.5; tarea T14), en un lote de guardado: con stock, el modo de selección
        /// pagando con él; sin stock y con video (<c>rv_reveal</c>: fill, tope y espera), el anuncio
        /// (<see cref="PressRevealVideo"/>), aunque haya saldo (D-028, GDD §6.3 pregunta 23 a: video primero, como Marea); sin
        /// video y con <c>reveal_seed_price</c> semillas (120), pagando con ellas; sin nada, el aviso «Te faltan {n} semillas» [P], sin
        /// cobrar (el precio en rojo y «Te faltan {n}» ya se ven en la ranura, como en la Cuia extra). Abrir la selección no cobra:
        /// el cobro llega al tocar la cuia, después de aplicar y guardar (T10; 04 §2.5 d). Con la selección abierta, otro toque en
        /// el botón la cancela sin coste [P].
        /// </summary>
        public bool PressReveal()
        {
            LevelFlow? flow = InLevel ? _flow : null;
            if (flow == null || !flow.HasLevel)
            {
                return false;
            }

            using (Saves())
            {
                if (flow.RevealSelecting)
                {
                    return flow.CancelRevealSelection();
                }

                // Solo donde la Luciérnaga se puede usar: fuera de ahí (con la victoria o el bloqueo pendiente, D-011 punto 1; o
                // con la ranura gris mientras la luciérnaga sale de ella) el toque no hace nada, tampoco terminar antes de tiempo
                // el revelado en marcha.
                if (!flow.RevealAvailable)
                {
                    return false;
                }

                _screen?.SettleReveal();
                if (flow.RevealStock > 0)
                {
                    return flow.BeginRevealSelection(RevealPayment.Stock);
                }

                // D-028 (GDD §6.3, pregunta 23 a): sin stock, primero el video, como Marea; las semillas solo sin video.
                if (PressRevealVideo())
                {
                    return true;
                }

                int price = flow.RevealPrice;
                int missing = flow.Wallet?.Missing(price) ?? price;
                if (missing == 0)
                {
                    return flow.BeginRevealSelection(RevealPayment.Seeds);
                }

                _screen?.ShowRevealMissing(missing);
                return false;
            }
        }

        /// <summary>
        /// Video <c>rv_reveal</c> de Revelar (04 §2.5: «1 revelado»; 3 al día y 60 s de espera, <c>AdsConfig</c>): al terminar el
        /// anuncio, en un lote de guardado, +1 Luciérnaga al stock (<see cref="LevelFlow.GrantRevealVideoUnit"/>) y el modo de
        /// selección pagando con ella. Sin fill (o con el tope o la espera) devuelve false: el botón de video ni aparece.
        /// </summary>
        public bool PressRevealVideo()
        {
            LevelFlow? flow = _flow;
            AdController? ads = _ads;
            if (flow == null || ads == null || !InLevel || !flow.HasLevel || flow.RevealSelecting || !flow.RevealAvailable
                || !ads.CheckRewarded(AdPlacement.Reveal).IsAllowed)
            {
                return false;
            }

            _screen?.SettleReveal();
            ads.ShowRewarded(AdPlacement.Reveal, r =>
            {
                if (!r.IsRewarded)
                {
                    return;
                }

                using (Saves())
                {
                    // La unidad va al stock aunque ya no se pueda elegir cuia (p. ej. el nivel cambió): nunca se pierde.
                    if (flow.GrantRevealVideoUnit())
                    {
                        flow.BeginRevealSelection(RevealPayment.Stock);
                    }
                }
            });
            return true;
        }

        /// <summary>
        /// «+3 Deshacer · Ver video» (<c>rv_undo_refill</c>): al terminar el anuncio, +3 Mareas al stock. Solo donde Marea se puede
        /// usar, como la recarga con semillas: en PLAYING sin victoria ni bloqueo pendiente, o en Aguas estancadas (D-011, punto 1:
        /// con la victoria pendiente la bandeja sigue habilitada, pero el video no se abre).
        /// </summary>
        public bool PressUndoRefillVideo()
        {
            LevelFlow? flow = _flow;
            AdController? ads = _ads;
            if (flow == null || ads == null || !InLevel || !(flow.AcceptsBoardInput || flow.Phase == FlowPhase.StuckModal)
                || !ads.CheckRewarded(AdPlacement.UndoRefill).IsAllowed)
            {
                return false;
            }

            ads.ShowRewarded(AdPlacement.UndoRefill, r =>
            {
                if (r.IsRewarded)
                {
                    flow.GrantUndoRefill();
                }
            });
            return true;
        }

        /// <summary>«+3 Deshacer · 30 semillas».</summary>
        public bool PressUndoRefillSeeds()
        {
            TransactionResult? result = _flow != null && InLevel ? _flow.RefillUndoWithSeeds() : null;
            return result != null && result.IsApplied;
        }

        /// <summary>Recarga gratis de +1 Marea de la rama F [P].</summary>
        public bool PressUndoGift()
        {
            return _flow != null && InLevel && _flow.GrantUndoGift();
        }

        /// <summary>«Continuar: +1 cuia» en Aguas estancadas o en ¿Seguro?</summary>
        public bool PressRescue()
        {
            LevelFlow? flow = InLevel ? _flow : null;
            TransactionResult? result = flow?.BuyRescue();
            LogFunnelError(flow, result, "Cuia de rescate");
            return result != null && result.IsApplied;
        }

        // AF.GameFlow no tiene motor: el «no aplicado» que no es cosa del jugador (LevelFlow.LastFunnelError, p. ej. un
        // historial que no se pudo rehacer con la cuia añadida; no se cobró nada) se registra aquí, con el nivel.
        private static void LogFunnelError(LevelFlow? flow, TransactionResult? result, string action)
        {
            if (result == null)
            {
                LogFunnelError(flow, action, "no se cobró nada");
            }
        }

        private static void LogFunnelError(LevelFlow? flow, string action, string consequence)
        {
            if (flow != null && flow.LastFunnelError != null)
            {
                Debug.LogError(LogPrefix + "nivel " + flow.LevelNumber + ": " + action + " no aplicada (" + flow.LastFunnelError + "); " + consequence + ".");
            }
        }

        /// <summary>Puñado de semillas (<c>af_coins_2</c>) desde el modal sin saldo: compra simulada (04 §6.2 <c>PUÑADO_199</c>).</summary>
        public bool PressHandful()
        {
            return Purchase(StoreCatalog.Handful, PurchaseContext.Stuck, null);
        }

        /// <summary>
        /// Balsa de rescate (<c>af_rescue</c>): compra simulada; si se compra, la Cuia de rescate se aplica sin semillas (04 §6.2
        /// <c>BALSA_499</c>); si se cancela, cuenta como rechazo.
        /// </summary>
        public bool PressBalsa()
        {
            LevelFlow? flow = _flow;
            return Purchase(StoreCatalog.Rescue, PurchaseContext.Stuck, r =>
            {
                // La Balsa se cobra antes de aplicar la cuia: si el historial no se puede rehacer con ella (no ocurre en 1-200,
                // MurkyPersistenceTests), el resto del paquete ya se entregó y la Cuia de rescate no. Queda en el log; comprobarlo
                // antes de la compra (04 §2.5 d) queda para otra ola.
                if (r.AppliesRescue && flow != null && !flow.ApplyPaidRescue())
                {
                    LogFunnelError(flow, "Cuia de rescate de la Balsa", "la Balsa ya se cobró y el resto del paquete se entregó");
                }
            });
        }

        /// <summary>«Rendirse» [P] (desde el 31).</summary>
        public bool PressGiveUp()
        {
            return _flow != null && InLevel && _flow.PressGiveUp();
        }

        /// <summary>X o «Rendirse» en ¿Seguro?: fallo duro y Nivel fallido.</summary>
        public bool ConfirmGiveUp()
        {
            return _flow != null && InLevel && _flow.ConfirmGiveUp();
        }

        /// <summary>«Reintentar» en Nivel fallido (con 0 canoas, Sin canoas).</summary>
        public bool PressRetry()
        {
            return _flow != null && InLevel && _flow.PressRetry();
        }

        /// <summary>X de Nivel fallido: al inicio (06 §4.7).</summary>
        public bool PressLevelFailedClose()
        {
            return _flow != null && _app != null && _flow.Phase == FlowPhase.LevelFailed && _app.ExitToLobby();
        }

        /// <summary>«Canoa extra · Ver video» (<c>rv_extra_canoe</c>): +1 canoa al terminar el anuncio.</summary>
        public bool PressExtraCanoeVideo()
        {
            AdController? ads = _ads;
            Wallet? wallet = _wallet;
            if (ads == null || wallet == null || !ads.CheckRewarded(AdPlacement.ExtraCanoe).IsAllowed)
            {
                return false;
            }

            ads.ShowRewarded(AdPlacement.ExtraCanoe, r =>
            {
                if (r.IsRewarded)
                {
                    wallet.Apply(Transaction.AddCanoe());
                }
            });
            return true;
        }

        /// <summary>«Llenar canoas · 200 semillas» (hasta 5/5).</summary>
        public bool PressRefillCanoes()
        {
            return _wallet != null && _wallet.Apply(Transaction.Buy(SeedSink.CanoesRefill)).IsApplied;
        }

        /// <summary>«Canoas infinitas 1 h» (<c>af_infinite_canoes_1h</c>): compra simulada.</summary>
        public bool PressInfiniteCanoes()
        {
            return Purchase(StoreCatalog.InfiniteCanoes1h, PurchaseContext.NoCanoes, null);
        }

        /// <summary>«Volver al inicio» de Sin canoas (desde el nivel o desde el inicio).</summary>
        public bool PressNoCanoesHome()
        {
            if (_app == null)
            {
                return false;
            }

            if (_app.Screen == AppScreen.NoCanoes)
            {
                return _app.CloseOverlay();
            }

            return _flow != null && _flow.Phase == FlowPhase.NoCanoes && _app.ExitToLobby();
        }

        /// <summary>X de Sin canoas: vuelve a donde se abrió (Nivel fallido o el tablero; desde el inicio, al inicio).</summary>
        public bool PressNoCanoesClose()
        {
            if (_app != null && _app.Screen == AppScreen.NoCanoes)
            {
                return _app.CloseOverlay();
            }

            return _flow != null && InLevel && _flow.CloseNoCanoes();
        }

        /// <summary>
        /// Pausa › «Salir» (D-008): desde el 31, ya confirmado, fallo duro y Nivel fallido (−1 canoa si hay una en juego); en
        /// 1-30, al inicio sin coste.
        /// </summary>
        public bool PressQuit()
        {
            LevelFlow? flow = _flow;
            if (flow == null || _app == null || !InLevel || !flow.CanQuit)
            {
                return false;
            }

            return flow.QuitIsHardFail ? flow.ConfirmQuit() : _app.ExitToLobby();
        }

        /// <summary>«Jugar · Nivel {n}» (pantalla de inicio).</summary>
        public bool PressPlay()
        {
            if (_app == null || _app.Screen != AppScreen.Lobby)
            {
                return false;
            }

            NoteFirstTouch(AppScreen.Lobby);
            return _app.PressPlay();
        }

        /// <summary>Contador de canoas de la pantalla de inicio (con 0/5 abre Sin canoas).</summary>
        public bool PressCanoes()
        {
            return _app != null && _app.PressCanoes();
        }

        /// <summary>Volver desde «El río sigue creciendo» o Sin canoas.</summary>
        public bool PressBack()
        {
            return AudioBack(_app != null && _app.CloseOverlay()); // AU1: sfx_ui_back al volver
        }

        /// <summary>
        /// Engranaje de Ajustes del inicio (07 §7.6; tarea L3): abre <see cref="SettingsScreen"/> con los idiomas que tienen la UI
        /// completa (<see cref="LocLocale.Offered"/>; en desarrollo, también en-XA) y lo guardado. Solo desde el inicio.
        /// </summary>
        public bool PressSettings()
        {
            SettingsScreen? settings = _settingsScreen;
            Profile? profile = _profile;
            if (settings == null || profile == null || _app == null || _app.Screen != AppScreen.Lobby || settings.Visible)
            {
                return false;
            }

            settings.Show(LocLocale.Offered(Loc.Catalog, Loc.ShowUnapproved), profile.Settings.Language, profile.Settings.Patterns,
                profile.Settings.SlothMessages);
            settings.MarkAudio(profile.Settings.Sound, profile.Settings.Music, profile.Settings.Haptics); // AU1
            settings.MarkIdleHint(profile.Settings.IdleHint); // MO3
            _skipNextDelta = true;
            return true;
        }

        /// <summary>
        /// Atrás de Android (Escape en el Input System) fuera del nivel: el único camino de Atrás para las hojas del inicio (FXT).
        /// Con Ajustes abierta, la cierra (<see cref="CloseSettings"/>); en «El río sigue creciendo» o Sin canoas, vuelve al inicio
        /// como su botón Volver (<see cref="PressBack"/>). Con el Mapa del río abierto, cierra su tarjeta de la parada o su Muestrario
        /// (M4.1) y, si no hay, el Mapa, en cualquier pantalla, también encima del nivel (<see cref="CloseRiverMap"/>; M3.3). Fuera
        /// de eso, no hace nada en el nivel (02 §3; 04 §6.6: el embudo no se cierra con Atrás), ni con la hoja «Compra simulada», un anuncio o el aviso del perfil bloqueado delante (tampoco
        /// con el Mapa abierto debajo), ni en el inicio sin hoja.
        /// </summary>
        public bool PressAndroidBack()
        {
            if (_app == null || _app.Screen == AppScreen.Boot)
            {
                return false;
            }

            if ((_storeSheet != null && _storeSheet.Visible) || (_adOverlay != null && _adOverlay.Visible) || (_lockedNotice != null && _lockedNotice.Visible))
            {
                return false;
            }

            // La ficha de una fruta (F4), encima de la victoria o de Ajustes: Atrás la cierra y deja lo de debajo como estaba.
            if (FruitSheetOpen)
            {
                return CloseFruitSheet();
            }

            // El Mapa del río se cierra con Atrás, como con su ← (M3.3), también abierto encima del nivel (pausa, victoria).
            if (_mapScreen != null && _mapScreen.Visible)
            {
                return _mapScreen.CloseTopSheet() || CloseRiverMap(); // primero la tarjeta de la parada y el Muestrario (M4.1)
            }

            if (_app.Screen == AppScreen.Level)
            {
                return false;
            }

            if (_settingsScreen != null && _settingsScreen.Visible)
            {
                return CloseSettings();
            }

            return PressBack();
        }

        /// <inheritdoc />
        /// <remarks>
        /// La pestaña del inicio (<see cref="MapOpenSource.Lobby"/>) solo con el inicio a la vista y desde <c>max_level</c> ≥
        /// <see cref="RiverMapScreen.TabFromMaxLevel"/>; la pausa (<see cref="MapOpenSource.Pause"/>), con el nivel en juego (se
        /// puede pausar) y desde ese mismo <c>max_level</c>; «Ver mapa» (<see cref="MapOpenSource.Victory"/>), con la victoria a la
        /// vista; el fin de contenido (<see cref="MapOpenSource.ContentEnd"/>), con «El río sigue creciendo» (tarea M4.3). Las demás
        /// entradas (banner, Mapa del 10) las piden sus tareas. Todas se abren encima de lo que haya. La primera vez lee y decodifica
        /// la geometría. Escribe <c>AF_STATE map_open</c>.
        /// </remarks>
        public bool OpenRiverMap(MapOpenSource source)
        {
            RiverMapScreen? map = _mapScreen;
            AppFlow? app = _app;
            IProgressStore? progress = _progress;
            RiverStopCatalog? stops = RiverStops;
            if (map == null || app == null || progress == null || stops == null || map.Visible || app.Screen == AppScreen.Boot)
            {
                return false;
            }

            if (CommerceOpen || (_lockedNotice != null && _lockedNotice.Visible) || (_settingsScreen != null && _settingsScreen.Visible) || FruitSheetOpen)
            {
                return false;
            }

            if (source == MapOpenSource.Lobby && (app.Screen != AppScreen.Lobby || progress.MaxLevel < RiverMapScreen.TabFromMaxLevel))
            {
                return false;
            }

            if (!MapSourceOpenNow(source, app, progress))
            {
                return false; // la pausa, la victoria y el fin de contenido, solo con su pantalla a la vista (M4.3)
            }

            if (!EnsureRiverMap())
            {
                return false;
            }

            int play = app.PlayLevel;
            map.Open(source, _riverMapData!, _riverRoutes!, stops, progress, play, app.PlayLevelAvailable);
            if (app.Screen != AppScreen.Level)
            {
                // La ventanita de casa se va ya, antes del siguiente paso de TiTino en el inicio: con la secuencia del Mapa del 10,
                // en la misma llamada que hace entrar al inicio (revisión de M4.2).
                _lobby?.SetCommerceOpen(HomeWindowCovered);
            }

            int clamped = Mathf.Clamp(play, 1, WorldTable.LastLevel);
            _probe?.MapOpen(source, play, stops.StopOf(clamped), map.World, map.View, map.Zoom);
            _skipNextDelta = true;
            return true;
        }

        /// <inheritdoc />
        /// <remarks>Escribe <c>AF_STATE map_close</c> con lo que se hizo en el Mapa y el <c>map_seen_level</c> que queda.</remarks>
        public bool CloseRiverMap()
        {
            RiverMapScreen? map = _mapScreen;
            if (map == null || !map.Visible)
            {
                return false;
            }

            MapCloseStats stats = map.Close();
            IProgressStore? progress = _progress;
            if (progress != null)
            {
                MarkMapSeen(progress.MaxLevel);
            }

            _probe?.MapClose(stats, progress?.MapSeenLevel ?? 0);
            if (_app != null && _app.Screen != AppScreen.Level)
            {
                _lobby?.SetCommerceOpen(HomeWindowCovered); // la ventanita de casa vuelve con el inicio a la vista (revisión de M4.2)
            }

            _skipNextDelta = true;
            return true;
        }

        /// <inheritdoc />
        public bool MarkMapSeen(int level)
        {
            IProgressStore? progress = _progress;
            if (progress == null || level < 0)
            {
                return false;
            }

            int before = progress.MapSeenLevel;
            using (Saves())
            {
                progress.SaveMapSeenLevel(level);
            }

            return progress.MapSeenLevel != before;
        }

        // «Jugar» del Mapa: desde la pestaña del inicio o en la secuencia del Mapa del 10 (que vive sobre el inicio: «Jugar · Nivel
        // 11»; M4.2), cierra y abre el nivel (o Sin canoas, como el «Jugar» del inicio); abierto encima de otra pantalla (pausa,
        // victoria, banner o fin de contenido), solo cierra y se vuelve a ella: «Volver al nivel» desde la pausa y «Volver» desde la
        // victoria y el fin de contenido (M4.3; RiverMapScreen.Modes.cs).
        private void PressMapPlay()
        {
            RiverMapScreen? map = _mapScreen;
            if (map == null || !map.Visible)
            {
                return;
            }

            bool fromLobby = map.Source == MapOpenSource.Lobby || map.Source == MapOpenSource.L10;
            CloseRiverMap();
            if (fromLobby && _app != null && _app.Screen == AppScreen.Lobby)
            {
                PressPlay();
            }
        }

        // La entrada source se puede abrir ahora (M4.3): la pausa, con el nivel en juego (se puede pausar) y desde max_level ≥ 10
        // (como la pestaña); «Ver mapa», con la victoria a la vista; el fin de contenido, con «El río sigue creciendo». Las demás,
        // como antes (la pestaña la mira OpenRiverMap; el banner y el Mapa del 10, sus tareas).
        private bool MapSourceOpenNow(MapOpenSource source, AppFlow app, IProgressStore progress)
        {
            LevelFlow? flow = _flow;
            switch (source)
            {
                case MapOpenSource.Pause:
                    return app.Screen == AppScreen.Level && flow != null && flow.HasLevel && (flow.CanQuit || flow.CanRestart)
                        && progress.MaxLevel >= RiverMapScreen.TabFromMaxLevel;
                case MapOpenSource.Victory:
                    return app.Screen == AppScreen.Level && flow != null && flow.Phase == FlowPhase.VictoryScreen;
                case MapOpenSource.ContentEnd:
                    return app.Screen == AppScreen.ContentEnd;
                default:
                    return true;
            }
        }

        // La geometría del Mapa y las rutas de las paradas para el viaje de mundo del banner (M3.6; GameScreen.RiverGeometry): se
        // leen la primera vez que hacen falta (EnsureRiverMap), como al abrir el Mapa.
        private bool TryRiverGeometry(out RiverMap? map, out RiverRoutes? routes)
        {
            bool ok = EnsureRiverMap();
            map = ok ? _riverMapData : null;
            routes = ok ? _riverRoutes : null;
            return ok;
        }

        // Lee y decodifica la geometría del Mapa y las rutas de las paradas, una vez por arranque (08 §4.5; propuesta del Mapa §11.4:
        // las asignaciones de la carga, al abrir). Si no valida, un error con la ruta del valor y el Mapa queda sin pestaña.
        private bool EnsureRiverMap()
        {
            if (_riverMapData != null && _riverRoutes != null)
            {
                return true;
            }

            if (_riverMapFailed || riverMap == null || riverStops == null)
            {
                return false;
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            RiverMap data;
            RiverRoutes routes;
            try
            {
                data = RiverMapReader.Read(riverMap.text);
                routes = RiverMapReader.ReadRoutes(riverStops.text, data);
            }
            catch (LevelFormatException e)
            {
                Debug.LogError(LogPrefix + e.Message + " El juego sigue sin el Mapa del río.");
                _riverMapFailed = true;
                if (_lobby != null)
                {
                    _lobby.RiverMapAvailable = false;
                }

                if (_app != null)
                {
                    _app.L10MapAvailable = false; // sin Mapa, «Siguiente» en el 10 carga el 11 (M4.2)
                    _app.ContentEndMapAvailable = false; // y «El río sigue creciendo» sale sola (M4.3)
                }

                if (_screen != null)
                {
                    _screen.RiverMapOpenable = false; // ni «Mapa del río» en la pausa ni «Ver mapa» en la victoria (M4.3)
                }

                return false;
            }

            _riverMapData = data;
            _riverRoutes = routes;
            watch.Stop();
            if (DevProbe.Enabled)
            {
                Debug.Log("AF_PERF {\"event\":\"map_load\",\"lines\":" + data.Lines.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + ",\"routes\":" + routes.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + ",\"ms\":" + watch.ElapsedMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
            }

            return true;
        }

        /// <summary>X de Ajustes o Atrás de Android con Ajustes abierto: vuelve al inicio tal como estaba.</summary>
        public bool CloseSettings()
        {
            SettingsScreen? settings = _settingsScreen;
            if (settings == null || !settings.Visible)
            {
                return false;
            }

            settings.Hide();
            _probe?.SettingsClosed();
            _skipNextDelta = true;
            return true;
        }

        /// <summary>
        /// Ajustes › Idioma (D-018 Q7-A; tarea L3): guarda <paramref name="language"/> en <c>settings.language</c>
        /// (<see cref="SettingsData.LanguageAuto"/> o un idioma con la UI completa) y vuelve a resolver el idioma (desarrollo &gt;
        /// guardado &gt; teléfono &gt; en). Si cambia el de la UI, <see cref="Loc.SetLocale"/> lanza <see cref="Loc.Changed"/> y la UI
        /// se reconstruye volviendo al inicio (<see cref="RebuildUi"/>), con Ajustes otra vez abierta en el idioma nuevo (FXT). False
        /// si no es una opción.
        /// </summary>
        public bool ChooseLanguage(string language)
        {
            Profile? profile = _profile;
            string? value = SettingsData.NormalizeLanguage(language);
            if (profile == null || value == null)
            {
                return false;
            }

            if (value != SettingsData.LanguageAuto && !LocLocale.IsOffered(Loc.Catalog, value, Loc.ShowUnapproved))
            {
                return false; // solo los idiomas que ofrece Ajustes (UI completa; en-XA, solo en desarrollo)
            }

            if (!string.Equals(profile.Settings.Language, value, StringComparison.Ordinal))
            {
                profile.Settings.Language = value;
                SaveSettings(profile);
            }

            _settingsScreen?.Mark(value, profile.Settings.Patterns);
            _probe?.SettingsChange("language", value, null);
            LocResolution next = LocTables.Resolve(Loc.Catalog, LaunchOptions.ResolveDevLanguage(), value, _deviceTag, Debug.isDebugBuild);
            Locale = next;
            if (string.Equals(next.Locale, Loc.Locale, StringComparison.Ordinal))
            {
                return true; // la UI ya va en ese idioma (p. ej. «Automático» con el teléfono en español): nada que reconstruir
            }

            Loc.SetLocale(next.Locale); // Loc.Changed: UiText vacía sus cachés
            if (_deviceLocale != null)
            {
                _probe?.LocaleState(next, _deviceLocale, _deviceTag);
            }

            RebuildUi();
            return true;
        }

        /// <summary>
        /// Ajustes › Patrones de fruta (02 §12; GDD.md §2 #5; tarea L3): guarda <paramref name="patterns"/> en
        /// <c>settings.patterns</c> y lo aplica al momento al nivel cargado, sin recargarlo
        /// (<see cref="GameScreen.RefreshFruitPatterns"/>); el siguiente nivel lo toma al cargar.
        /// </summary>
        public bool ChoosePatterns(PatternsSetting patterns)
        {
            Profile? profile = _profile;
            if (profile == null || (patterns != PatternsSetting.Auto && patterns != PatternsSetting.On && patterns != PatternsSetting.Off))
            {
                return false;
            }

            if (profile.Settings.Patterns != patterns)
            {
                profile.Settings.Patterns = patterns;
                SaveSettings(profile);
            }

            GameScreen? screen = _screen;
            bool loaded = screen != null && screen.RefreshFruitPatterns();
            _settingsScreen?.Mark(profile.Settings.Language, patterns);
            _probe?.SettingsChange("patterns", DevProbe.PatternsKey(patterns), loaded ? screen!.FruitPatternsOn : (bool?)null);
            return true;
        }

        /// <summary>
        /// Ajustes › Mensajes de TiTino (D-016, punto 4; 02 §12; tarea TI3): guarda <paramref name="on"/> en
        /// <c>settings.sloth_messages</c> en el acto; apagado, TiTino solo hace gestos desde su siguiente aparición
        /// (<c>AF.GameFlow.SlothLines</c>). La sonda escribe <c>settings_change</c> con la clave <c>sloth_messages</c> (08 §8.2).
        /// </summary>
        public bool ChooseSlothMessages(bool on)
        {
            Profile? profile = _profile;
            if (profile == null)
            {
                return false;
            }

            if (profile.Settings.SlothMessages != on)
            {
                profile.Settings.SlothMessages = on;
                SaveSettings(profile);
            }

            _settingsScreen?.MarkSlothMessages(on);
            _probe?.SettingsChange(SettingsScreen.SlothMessagesId, on ? "true" : "false", null);
            return true;
        }

        // Los ajustes se guardan en el acto (escritura síncrona, como la cartera). Con el perfil en solo lectura (08 §4.4) no se
        // puede: quedan en memoria para esta sesión.
        private void SaveSettings(Profile profile)
        {
            if (_saveBatch != null && !_saveBatch.Durable.TrySave(profile) && !ProfileLocked)
            {
                Debug.LogWarning(LogPrefix + "no se pudieron guardar los ajustes: siguen en memoria y salen con el siguiente guardado.");
            }
        }

        /// <summary>
        /// Reconstruye toda la UI en el idioma nuevo volviendo al inicio (Q7-A; tarea L3): guarda el perfil, recarga la escena y
        /// el arranque siguiente construye todas las pantallas otra vez (con <see cref="Loc"/> ya en el idioma elegido) y va al
        /// inicio con el mismo perfil en memoria (no lo vuelve a leer del disco: con el perfil en solo lectura, lo jugado en la
        /// sesión solo vive ahí), sin el nivel de desarrollo (<c>af_level</c>) ni el de los tests y sin volver a aplicar los
        /// extras de la cartera (<c>af_seeds</c> y <c>af_canoes</c>): volver al inicio no es un arranque en frío. Si Ajustes estaba
        /// abierta, se vuelve a abrir sobre el inicio (FXT).
        /// </summary>
        private void RebuildUi()
        {
            s_rebuildToLobby = _profile != null;
            s_rebuildProfile = _profile;
            s_rebuildStatus = _profileStatus;
            s_rebuildStore = _profileStore;
            s_rebuildReopenSettings = _profile != null && _settingsScreen != null && _settingsScreen.Visible;
            _settingsScreen?.Hide(); // sin settings_closed: el arranque siguiente la vuelve a abrir en el idioma nuevo
            PersistNow("idioma");
            enabled = false; // nada más de este arranque: la escena se va
            Scene scene = gameObject.scene;
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
                return;
            }

#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.name, LoadSceneMode.Single);
#endif
        }

        /// <inheritdoc />
        public void OnAppScreenChanged(AppScreen screen)
        {
            // La ficha de una fruta vive sobre la victoria o sobre Ajustes: si la app cambia de pantalla por otro camino, se cierra.
            if (FruitSheetOpen)
            {
                CloseFruitSheet();
            }

            // El Mapa abierto desde la pestaña (o el del 10, M4.2) vive sobre el inicio: si la app deja el inicio por otro camino, se
            // cierra antes.
            if (screen != AppScreen.Lobby && _mapScreen != null && _mapScreen.Visible
                && (_mapScreen.Source == MapOpenSource.Lobby || _mapScreen.Source == MapOpenSource.L10))
            {
                CloseRiverMap();
            }

            // Lo mismo con el de la pausa y el de la victoria, que viven sobre el nivel, y con el del fin de contenido (M4.3).
            if (_mapScreen != null && _mapScreen.Visible && MapOutlivesItsScreen(_mapScreen.Source, screen))
            {
                CloseRiverMap();
            }

            // La pantalla de nivel se muestra antes de cargar el nivel (AppFlow llama a Load después).
            _screen?.SetVisible(screen == AppScreen.Level);
            _lobby?.Show(screen);
            AudioScreenChanged(screen); // AU2: el tema de menú fuera del nivel (GameBootstrap.Audio.cs)
            _skipNextDelta = true;

            // «Jugar» con un nivel que abre mundo (11, 41, 81 y 141, en el primer intento): el arte de su banner se pinta ya en
            // otro hilo, así que al entrar el banner suele salir en el acto (A4, D-012; GameScreen.PrefetchWorldBanner).
            if (screen == AppScreen.Lobby && _app != null)
            {
                _screen?.PrefetchWorldBanner(_app.PlayLevel);

                // La secuencia del Mapa del 10 (06 §2.3; P-M4; M4.2): tras «Siguiente» en la victoria del 10 o, si se pulsó Inicio o
                // se cerró la app, en la primera entrada al inicio con max_level ≥ 10 y map_seen_level < 10. Al acabarla se anota lo
                // visto hasta el 10: no se repite.
                if (_app.L10MapDue)
                {
                    OpenRiverMap(MapOpenSource.L10);
                }
            }

            // Fin de contenido tras ganar el último nivel publicado (06 §9; D-013, punto 7; M4.3): antes de «El río sigue creciendo»,
            // el Mapa encima con el viaje del último tramo; al cerrarlo queda la pantalla de siempre.
            if (screen == AppScreen.ContentEnd && _app != null && _app.ContentEndMapDue)
            {
                OpenRiverMap(MapOpenSource.ContentEnd);
            }
        }

        // El Mapa abierto desde source ya no tiene debajo su pantalla al pasar la app a screen (M4.3): la pausa y la victoria viven
        // sobre el nivel; el fin de contenido, sobre «El río sigue creciendo».
        private static bool MapOutlivesItsScreen(MapOpenSource source, AppScreen screen)
        {
            switch (source)
            {
                case MapOpenSource.Pause:
                case MapOpenSource.Victory:
                    return screen != AppScreen.Level;
                case MapOpenSource.ContentEnd:
                    return screen != AppScreen.ContentEnd;
                default:
                    return false;
            }
        }

        private bool InLevel => _app == null || _app.Screen == AppScreen.Level;

        // Hay una pantalla de pago delante: la hoja «Compra simulada» o un anuncio (y lo que lo rodea; propuesta del personaje
        // d.11). Las ofertas que vengan (Mochila de inicio, Mercado) se suman aquí. Sin asignar memoria.
        private bool CommerceOpen => (_storeSheet != null && _storeSheet.Visible) || (_adOverlay != null && _adOverlay.Visible);

        // La ventanita de casa del inicio no sale (LobbyScreen.SetCommerceOpen): con una pantalla de pago delante (D-013, punto 6)
        // o con el Mapa del río encima, que es opaco (revisión de M4.2). Con el Mapa, TiTino no se ve: su saludo del día (como
        // mucho 1; D-016, punto 5) no se decide debajo de la secuencia del Mapa del 10 (ni de la pestaña), sino al cerrarlo, en
        // la misma visita al inicio (o en la siguiente, si su «Jugar» va al nivel). Sin asignar memoria.
        private bool HomeWindowCovered => CommerceOpen || (_mapScreen != null && _mapScreen.Visible);

        // Lote de guardado de una acción del jugador: el perfil se escribe una vez al cerrarlo (revisión B2-5); sin lote, nada.
        private ProfileSaveScope Saves()
        {
            return _saveBatch == null ? default : _saveBatch.Begin();
        }

        // Compra simulada de sku (05 §4.1); lo comprado se acredita con Wallet.Apply dentro de la tienda.
        private bool Purchase(string sku, PurchaseContext context, Action<PurchaseResult>? onDone)
        {
            IStoreService? store = _store;
            if (store == null || store.IsPurchasing || ProfileLocked)
            {
                return false; // con el perfil en solo lectura lo comprado se perdería (08 §4.4): no se vende nada
            }

            int level = _flow != null && _flow.HasLevel ? _flow.LevelNumber : (_app?.OverlayLevel ?? 0);
            store.Purchase(new PurchaseRequest(sku, context, level), r =>
            {
                _probe?.Purchase(r, _wallet);
                onDone?.Invoke(r);
            });
            return true;
        }

        private void Awake()
        {
            Current = this;
            _bootOrigin = Application.isEditor ? Time.realtimeSinceStartupAsDouble : 0.0; // origen del arranque (TI6d): ver SinceStartupMs
            Application.targetFrameRate = 60; // 08 §5.4
            DevProbe.Initialize();
            EnsureEventSystem();

            // Atrás de Android, desde el arranque y aunque aún no exista el teclado (FXT): la acción se vuelve a enlazar sola
            // cuando aparece el dispositivo y cuenta la pulsación aunque la bajada y la subida lleguen en el mismo fotograma.
            _backAction = new InputAction("AndroidBack", InputActionType.Button, "<Keyboard>/escape");
            _backAction.performed += OnBackPerformed;
        }

        private void OnEnable()
        {
            _backPresses = 0;
            _backAction?.Enable();
        }

        private void OnDisable()
        {
            _backAction?.Disable();
            _backPresses = 0;
        }

        private void OnBackPerformed(InputAction.CallbackContext context)
        {
            _backPresses++;
        }

        // Arranque en frío (08 §2.3) con la pantalla de arranque delante (06 §2.2; tarea TI6d): se crea y se pinta un fotograma
        // antes de cargar nada; con el perfil y el arte leídos, TiTino entra en ella (sloth_wake la primera vez, sloth_idle las
        // demás) y se pinta otro fotograma; después se construye todo y, al terminar la carga, se corta (con el segundo de
        // sloth_wake de la primera apertura, si cabe: BootSplashTiming) y solo entonces arranca AppFlow, para que la intro del
        // nivel 1 no corra detrás. Sin la pantalla (tras la reconstrucción por idioma, que no es un arranque en frío, o apagada
        // por los tests: BootSplash.EnabledOverride) no hay ningún yield: todo ocurre en esta llamada, como antes.
        private IEnumerator Start()
        {
            if (!s_rebuildToLobby && BootSplash.Enabled)
            {
                _splash = new BootSplash(transform, BootSplash.SlothExpected); // ya compuesta con o sin TiTino: el logotipo no salta
                yield return null; // el río y el logotipo, a la vista antes de cargar
            }

            LevelCatalog? catalog = LoadCatalog();
            if (catalog == null)
            {
                CutSplash();
                enabled = false;
                yield break;
            }

            IClock clock = LaunchOptions.ClockOverride ?? SystemClock.Instance;
            string profileDirectory = LaunchOptions.ProfileDirectoryOverride ?? Application.persistentDataPath;

            // Tras elegir idioma en Ajustes, la escena se recargó (RebuildUi): se vuelve al inicio con el perfil de la sesión, tal
            // cual estaba en memoria (también con el perfil en solo lectura, que no se pudo guardar), con el mismo almacén (que
            // sigue bloqueado si lo estaba) y sin los extras de desarrollo de la cartera, que son del arranque en frío (revisión
            // de L3).
            IProfileStore? carriedStore = s_rebuildToLobby ? s_rebuildStore : null;
            bool sameStore = carriedStore != null && (LaunchOptions.ProfileStoreOverride != null
                ? ReferenceEquals(carriedStore, LaunchOptions.ProfileStoreOverride)
                : carriedStore is LocalProfileStore local && string.Equals(local.DirectoryPath, profileDirectory, StringComparison.Ordinal));
            Profile? carried = sameStore ? s_rebuildProfile : null;
            bool rebuilt = carried != null;
            bool reopenSettings = rebuilt && s_rebuildReopenSettings;
            ProfileLoadStatus carriedStatus = s_rebuildStatus;
            s_rebuildReopenSettings = false;
            s_rebuildToLobby = false;
            s_rebuildProfile = null;
            s_rebuildStore = null;
            IProfileStore store = rebuilt ? carriedStore! : (LaunchOptions.ProfileStoreOverride ?? new LocalProfileStore(profileDirectory));
            Profile profile;
            bool firstRun;
            if (carried != null)
            {
                profile = carried;
                _profileStatus = carriedStatus;
                firstRun = false;
            }
            else
            {
                bool migrateLegacy = LaunchOptions.ProfileStoreOverride == null && LaunchOptions.ProfileDirectoryOverride == null;
                profile = LoadProfile(store, clock, migrateLegacy, out firstRun, out _profileStatus);
                if (LaunchOptions.ApplyDevWallet(profile, clock))
                {
                    store.TrySave(profile); // extras af_seeds y af_canoes de los builds de desarrollo [P] (B2-6)
                }

                if (LaunchOptions.ApplyDevProgress(profile))
                {
                    store.TrySave(profile); // extra af_max_level de los builds de desarrollo [P] (M3.3)
                }
            }

            bool locked = IsLocked(_profileStatus);
            _profile = profile;
            _profileStore = store;

            // Textos (D-018; tareas L2b y L3): con el perfil leído y antes de construir ninguna pantalla. Desarrollo (af_lang) >
            // settings.language > teléfono > en, con el respaldo es-419, y las cifras con la región del teléfono.
            _deviceLocale = LaunchOptions.DeviceLocaleOverride ?? AndroidDeviceLocale.ForPlatform();
            _deviceTag = _deviceLocale.LanguageTag ?? string.Empty;
            LocResolution locale = SetUpTexts(profile.Settings.Language, _deviceTag);

            // Cartera, progreso, nivel en curso, anuncios y tienda guardan el mismo perfil por el lote: dentro de una acción del
            // jugador (vertido, victoria, rescate, rendición, compra, anuncio) el perfil se escribe una sola vez (revisión B2-5).
            // Con el perfil en disco (LocalProfileStore), el lote escribe en segundo plano (T-D): el vertido solo serializa en el
            // hilo principal. La cartera guarda por batch.Durable: cada mutación espera al escritor y escribe en síncrono.
            var batch = new ProfileSaveBatch(store);
            _saveBatch = batch;
            _wallet = new Wallet(profile, batch.Durable, clock);
            IProgressStore progress = LaunchOptions.ProgressStoreOverride ?? new ProfileProgressStore(profile, batch);
            _progress = progress;

            _art = SharedArt(catalog.WaterBase);
            _probe = new DevProbe { Settings = profile.Settings };
            if (_splash != null)
            {
                // TiTino en su embaúba (D-016, T2-A): con ui_sloth_enabled apagado (08 §9; el interruptor de cada gancho, como el
                // de la victoria) o sin su arte final (release), no sale y no hay espera. Reducir movimiento aún no es un ajuste
                // del jugador (07 §7.8): la pose se anima.
                if (BootSplash.SlothEnabled && _splash.ShowSloth(_art.SlothRigArt(), firstRun, reduceMotion: false))
                {
                    yield return null; // TiTino, a la vista mientras se construye el resto
                }
            }

            InstallAudio(profile.Settings); // AU1: audio provisional por código y hápticos (GameBootstrap.Audio.cs)
            _probe.LocaleState(locale, _deviceLocale, _deviceTag);

            // Anuncios y compras simulados (D-008 puntos 4 y 5; sin SDK, red ni identificadores): sus pantallas van en lienzos
            // propios por encima de todo; los tests las sustituyen por presentadores inmediatos (LaunchOptions).
            // Cada anuncio escribe su tipo y su ubicación en la sonda (AF_STATE ad y ad_closed, B2-4).
            // Al cerrar un anuncio o una compra, lo que se anota (registro, recompensa, compra y su efecto en el nivel) va en un
            // lote: una escritura del perfil.
            ISimulatedAdPresenter adPresenter = new ProbedAdPresenter(
                LaunchOptions.AdPresenterOverride ?? (_adOverlay = new AdOverlay(transform, _art)),
                _probe,
                () => _flow != null && _flow.HasLevel && InLevel ? _flow.LevelNumber : (_app?.OverlayLevel ?? 0),
                () => _wallet,
                batch);
            ISimulatedPurchasePresenter purchasePresenter = new BatchedPurchasePresenter(
                LaunchOptions.PurchasePresenterOverride ?? (_storeSheet = new StoreSheet(transform, _art)), batch);
            _ads = AdController.Create(new FakeAdService(adPresenter, LaunchOptions.ResolveAdFill()), profile, batch, clock, firstRun);
            _storeLedger = new StoreLedger(profile, _wallet, batch, clock);
            _store = new FakeStoreService(_storeLedger, purchasePresenter, LaunchOptions.DefaultStoreCurrency);
            _services = new GameServices(_wallet, _ads, _store, _storeLedger, GeoTier.T3, purchasesBlocked: locked, deviceLocale: _deviceLocale, settings: profile.Settings,
                mapProgress: progress);

            _screen = new GameScreen(transform, _art, _probe, this);
            _screen.SetVisible(false);
            Fauna = LoadFauna();
            _screen.Fauna = Fauna; // la especie de miquito de cada intro (guion de la intro §4.3)
            RiverStops = LoadRiverStops(riverStops);
            _screen.RiverStops = RiverStops; // paradas del Mapa del río (D-013): fondo, llegada y rótulo del lugar
            _screen.RiverGeometry = TryRiverGeometry; // la geometría del Mapa para el viaje de mundo del banner (M3.6)
            _screen.RiverMapOpenable = RiverMapAvailable; // «Mapa del río» en la pausa y «Ver mapa» en la victoria (M4.3)
            FruitFacts = LoadFruitFacts(fruitCalendar); // calendario de Frutas del río (D-017)
            _screen.FruitFacts = FruitFacts; // la línea de fruta de la victoria (F3)
            Seasons = LoadSeasons(seasonTable); // tabla de temporada del cliente (pregunta 32 b; SE2)
            _screen.Seasons = Seasons; // la LUT de aguas altas o bajas del fondo del tablero
            _lobby = new LobbyScreen(transform, _art, _probe, () => PressPlay(), () => PressCanoes(), () => PressSettings(), () => PressBack(), this);
            _lobby.RiverStops = RiverStops; // el sitio, «Faltan N» y la ventanita de casa en el inicio (M2.1)
            _lobby.Seasons = Seasons; // la LUT de temporada del fondo del inicio, como el tablero (SE2)
            _lobby.RiverMapAvailable = RiverMapAvailable; // la pestaña del Mapa del río, desde max_level ≥ 10 (M3.3)
            _mapScreen = new RiverMapScreen(transform, _art, _probe, () => CloseRiverMap(), () => PressMapPlay());
            _mapScreen.Fauna = Fauna; // el miquito del tramo en la tarjeta de la parada (M4.1)
            _mapScreen.FruitSeries = FruitFacts?.Catalog; // la fruta de la serie en la tarjeta de la parada (D-022; M4.1)
            _mapScreen.Seasons = Seasons; // la LUT de temporada en la tarjeta de la parada, como el fondo (07 §2.1; FX8)
            _mapScreen.LevelWater = level => _app?.Catalog.Find(level)?.WaterBase; // el agua del fondo de la tarjeta (M4.1)
            _mapScreen.L10SequenceEnded = () => MarkMapSeen(L10SequenceRule.Level); // la secuencia del Mapa del 10 no se repite (M4.2)
            _mapScreen.L10SlothVoice = () => _screen == null || _screen.SlothVoice; // su bocadillo, solo con voz (D-016, punto 4)
            _mapScreen.L10BubbleSaid = () => _screen?.SlothSession.RecordMapIntro(); // el saludo del inicio se aplaza (D-028, 60 b)
            _settingsScreen = new SettingsScreen(transform, _art, _probe, l => ChooseLanguage(l), p => ChoosePatterns(p), m => ChooseSlothMessages(m), () => CloseSettings());
            _settingsScreen.AudioToggled = (key, on) => ChooseAudio(key, on); // AU1: Sonido, Música y Vibración
            _settingsScreen.IdleHintToggled = on => ChooseIdleHint(on); // MO3: Pista por inactividad
            BuildFruitSheet(); // F4: la ficha de una fruta, desde la línea de la victoria y desde Ajustes (GameBootstrap.Fruit.cs)

            // rules_pour_mode (D-005): se resuelve una vez al arrancar y se aplica en cada carga de nivel.
            _flow = new LevelFlow(catalog, progress, _screen, LaunchOptions.ResolveTapSwitchSelection(), LaunchOptions.ResolvePourMode(), _wallet)
            {
                AdGate = new InterstitialGate(_ads, _probe, _wallet), // AD_GATE tras «Siguiente» (B2-4)
                StateStore = new ProfileLevelStateStore(profile, batch), // nivel en curso tras cada vertido (B2-5)
                SaveBatch = batch, // una escritura del perfil por acción del jugador
                Clock = clock, // Morpho (MO1): hint_timeout_ms y el enfriamiento, con el mismo reloj que la cartera
                HintProbe = _probe, // AF_PERF morpho de cada respuesta (MO1)

                // Tarjeta previa (LEVEL_CARD, 02 §10): LaunchOptions.LevelCardsOverride ?? GameConfig.LevelCardsEnabledDefault,
                // apagada hasta que exista su vista (T13).
                LevelCardsEnabled = LaunchOptions.ResolveLevelCards(),

                // Banner de entrada a un mundo (07 §2.2, regla 1): con el mismo interruptor que las tarjetas, porque usa su retención
                // (LEVEL_CARD) y su vista (GameScreen) y los tests PlayMode lo apagan igual (PlayModeHelpers).
                WorldBannersEnabled = LaunchOptions.ResolveLevelCards(),
            };
            _screen.Bind(_flow, _services);
            _app = new AppFlow(_flow, progress, _wallet, this)
            {
                L10MapAvailable = RiverMapAvailable && (L10SequenceOverride ?? true), // la secuencia del Mapa del 10 (M4.2)
                ContentEndMapAvailable = RiverMapAvailable && (ContentEndMapOverride ?? true), // el Mapa del fin de contenido (M4.3)
            };
            _lobby.Bind(_app, _services);

            int requested = rebuilt ? 0 : LaunchOptions.StartLevelOverride;
            if (requested <= 0 && !rebuilt)
            {
                requested = LaunchOptions.ReadIntentLevel();
            }

            if (_splash != null)
            {
                yield return FinishSplash(firstRun); // fin de la carga: se corta, con el segundo de sloth_wake si toca y cabe
            }

            if (requested > 0)
            {
                _app.StartAtLevel(requested);
            }
            else
            {
                // 02 §14: con un nivel en curso guardado se reabre tal cual (sin intro, con su modal), sin cobrar nada. Tras la
                // reconstrucción por idioma, el inicio (se venía de él: el nivel que se dejó ya no está en curso).
                _app.Start(firstRun && !rebuilt, profile.LevelState);
            }

            if (reopenSettings)
            {
                // Observación 3 de la prueba de la ola 1 (FXT; la [P] recomendada): elegir un idioma no cierra Ajustes. La UI se
                // rehízo en el idioma nuevo y Ajustes se vuelve a abrir sobre el inicio con lo guardado marcado (la opción elegida);
                // la tarjeta no tiene desplazamiento, así que se ve igual que antes. La sonda escribe otra vez AF_STATE settings y
                // su AF_LAYOUT (los textos y las cajas cambian); settings_closed sale solo al cerrarla.
                PressSettings();
            }

            if (locked)
            {
                // 08 §4.4: el perfil anterior se conserva intacto y esta sesión no se guarda; se avisa y no hay compras.
                _lockedNotice = new ProfileLockedNotice(transform, _art);
                _lockedNotice.Show(_profileStatus);
            }

            _skipNextDelta = true;
        }

        private void Update()
        {
            if (_splash != null)
            {
                _splash.Tick(Mathf.Min(Time.unscaledDeltaTime * 1000f, MaxDeltaMs)); // TiTino en la pantalla de arranque
            }

            if (_flow == null || _screen == null || _app == null || _app.Screen == AppScreen.Boot)
            {
                _backPresses = 0; // Atrás durante el arranque: aún no hay nada que cerrar
                return;
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime * 1000f, MaxDeltaMs);
            if (_skipNextDelta)
            {
                _skipNextDelta = false;
                dt = 0f;
            }

            _adOverlay?.Tick(Time.unscaledDeltaTime * 1000f);
            bool back = _backPresses > 0;
            _backPresses = 0;
            if (back)
            {
                AndroidBackWithAudio(); // AU1: PressAndroidBack con sfx_ui_back, o el háptico de Atrás bloqueado
            }

            // El Mapa del río, encima de lo que haya (M3.3): sin asignaciones con el Mapa quieto.
            if (_mapScreen != null && _mapScreen.Visible)
            {
                _mapScreen.Tick(dt);
            }

            if (_settingsScreen != null && _settingsScreen.Visible)
            {
                // Ajustes solo vive sobre el inicio (el Atrás de Android la cierra en PressAndroidBack).
                if (_app.Screen != AppScreen.Lobby)
                {
                    CloseSettings();
                }
                else
                {
                    _settingsScreen.Tick();
                }
            }

            // La ficha de una fruta (F4): encima de la victoria, la congela (ni la vista ni el flujo avanzan: ni la línea, ni el
            // avance tras la Cosecha doble, ni AD_GATE) hasta cerrarla, y al cerrarla se vuelve a la victoria tal como estaba. El
            // tiempo con la ficha abierta sí cuenta en el dismissed_ms de la línea (revisión de F4).
            if (TickFruitSheet(dt))
            {
                return;
            }

            if (_app.Screen != AppScreen.Level)
            {
                _app.RefreshNoCanoes();
                _lobby?.SetCommerceOpen(HomeWindowCovered); // ni en pantallas de pago (M2.1; D-013, punto 6) ni bajo el Mapa (M4.2)
                _lobby?.Tick(dt);
                return;
            }

            _screen.Tick(dt);
            _msCarry += dt;
            int whole = (int)_msCarry;
            _msCarry -= whole;
            _flow.Tick(whole, _screen.IsViewIdle);

            // D-011, punto 4: la victoria pasa sola tras la Cosecha doble por el camino de Siguiente, después del Tick del flujo
            // y no dentro del de la vista: N + 1 se carga entre dos fotogramas, como con un toque en Siguiente.
            if (_screen.TakeAutoNext())
            {
                PressNext();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                PersistNow("segundo plano");
                return;
            }

            // 02 §14: al volver de segundo plano la vista salta al modelo sin animar (también la luciérnaga en vuelo: la cuia
            // queda revelada) y la selección se limpia.
            if (_flow != null && _screen != null && InLevel)
            {
                _screen.SnapToModel();
                _screen.SettleReveal();
                _flow.ClearSelection();
            }

            // MO3 (02 §3.4; 04 §2.3): el tiempo fuera de la app no es inactividad en el tablero; la cuenta de la pista por
            // inactividad empieza de cero, como tras un toque.
            _screen?.IdleHintBackFromBackground();
        }

        // Tablas de textos de la escena (o las del test, LaunchOptions.LocTablesOverride) → Loc (D-018; 08 §4.7). Si faltan o no
        // se pueden leer, LocTables.FromTextsOrEmpty decide sin lanzar (Loc sin textos en es-419, el idioma de respaldo del
        // código, LocLocale.FallbackLocale: en desarrollo cada texto se ve como «[clave]» y en release, vacío; nunca un texto en
        // otro idioma) y aquí solo se registra el aviso o el error. El idioma: af_lang > savedLanguage (settings.language) >
        // deviceTag (el teléfono) > en, y las cifras con la región de deviceTag (tarea L3).
        private LocResolution SetUpTexts(string savedLanguage, string deviceTag)
        {
            bool development = Debug.isDebugBuild;
            string? devLanguage = LaunchOptions.ResolveDevLanguage();
            string? warning;
            string? error;
            LocTableTexts? test = LaunchOptions.LocTablesOverride;
            LocCatalog catalog = test != null
                ? LocTables.FromTextsOrEmpty(test.Strings, test.Facts, out warning, out error)
                : LocTables.FromAssetsOrEmpty(stringsTable, factsTable, out warning, out error);
            if (error != null)
            {
                Debug.LogError(LogPrefix + error);
            }

            if (warning != null)
            {
                Debug.LogWarning(LogPrefix + warning);
            }

            LocResolution resolution = LocTables.Configure(catalog, devLanguage, savedLanguage, deviceTag, development);
            if (devLanguage != null && resolution.Source != LocSource.Dev)
            {
                Debug.LogWarning(LogPrefix + LaunchOptions.IntentExtraLanguage + " = \"" + devLanguage + "\" no tiene la UI completa: se usa " + resolution.Locale + ".");
            }

            Locale = resolution;
            return resolution;
        }

        // Catálogo de miquitos de la escena (af-fauna/0). Si falta o no valida, la intro va siempre en modo dosel (sin tropa).
        private FaunaCatalog? LoadFauna()
        {
            if (faunaCatalog == null)
            {
                Debug.LogWarning(LogPrefix + "falta el catálogo de miquitos (TextAsset): la intro irá en modo dosel. Regenera la escena con AF.UI.Editor.GameSceneBuilder.CreateGameScene.");
                return null;
            }

            try
            {
                return FaunaCatalogReader.Read(faunaCatalog.text);
            }
            catch (FaunaFormatException e)
            {
                Debug.LogError(LogPrefix + e.Message);
                return null;
            }
        }

        // Paradas del Mapa del río de la escena (af-river-stops/1; 08 §4.5), como los miquitos: si falta el archivo, un aviso; si no
        // valida, un error con la ruta del valor. En los dos casos, null y el juego queda como sin el Mapa (nada visible depende aún
        // de ellas).
        internal static RiverStopCatalog? LoadRiverStops(TextAsset? stops)
        {
            if (stops == null)
            {
                Debug.LogWarning(LogPrefix + "faltan las paradas del Mapa del río (TextAsset): el juego sigue sin el Mapa. Regenera la escena con AF.UI.Editor.GameSceneBuilder.CreateGameScene.");
                return null;
            }

            try
            {
                return RiverStopCatalogReader.Read(stops.text);
            }
            catch (LevelFormatException e)
            {
                Debug.LogError(LogPrefix + e.Message + " El juego sigue sin el Mapa.");
                return null;
            }
        }

        // Tabla de temporada de la escena (af-seasons/1; SE1 y SE2), como las paradas del Mapa: si falta el archivo, un aviso; si no
        // valida, un error con la ruta del valor. En los dos casos, null y el juego sigue sin LUT de temporada (como antes).
        internal static SeasonTable? LoadSeasons(TextAsset? table)
        {
            if (table == null)
            {
                Debug.LogWarning(LogPrefix + "falta la tabla de temporada (TextAsset): el juego sigue sin LUT de temporada. Regenera la escena con AF.UI.Editor.GameSceneBuilder.CreateGameScene.");
                return null;
            }

            try
            {
                return SeasonTableReader.Read(table.text);
            }
            catch (LevelFormatException e)
            {
                Debug.LogError(LogPrefix + e.Message + " El juego sigue sin LUT de temporada.");
                return null;
            }
        }

        // Calendario de Frutas del río de la escena (af-facts/1; 08 §4.6), como las paradas del Mapa: si falta el archivo, un aviso;
        // si no valida, un error con la ruta del valor. En los dos casos, null y el juego sigue sin líneas de fruta.
        internal static FactSchedule? LoadFruitFacts(TextAsset? calendar)
        {
            if (calendar == null)
            {
                Debug.LogWarning(LogPrefix + "falta el calendario de Frutas del río (TextAsset): el juego sigue sin líneas de fruta. Regenera la escena con AF.UI.Editor.GameSceneBuilder.CreateGameScene.");
                return null;
            }

            try
            {
                return new FactSchedule(FactCatalogReader.Read(calendar.text));
            }
            catch (LevelFormatException e)
            {
                Debug.LogError(LogPrefix + e.Message + " El juego sigue sin líneas de fruta.");
                return null;
            }
        }

        // Paquetes de la escena (o el del test, LaunchOptions.LevelPackJsonOverride) → catálogo con el desplazamiento de
        // desarrollo (af_level_offset). Un paquete roto no deja la pantalla en blanco con una excepción en Start: se registra.
        // Leer los paquetes construye y valida cada tablero (base y relief): en desarrollo, la línea AF_PERF levels_load dice
        // cuánto cuesta (T02; si se nota en el teléfono, la validación pasará a ser perezosa).
        private LevelCatalog? LoadCatalog()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var packs = new List<LevelPack>();
            try
            {
                string? packJson = LaunchOptions.LevelPackJsonOverride;
                if (packJson != null)
                {
                    packs.Add(LevelPackReader.Read(packJson));
                }
                else
                {
                    IReadOnlyList<TextAsset> assets = LevelPackAssets;
                    for (int i = 0; i < assets.Count; i++)
                    {
                        packs.Add(LevelPackReader.Read(assets[i].text));
                    }
                }

                if (packs.Count == 0)
                {
                    Debug.LogError(LogPrefix + "faltan los paquetes de niveles (TextAsset). Regenera la escena con AF.UI.Editor.GameSceneBuilder.CreateGameScene.");
                    return null;
                }

                // Kill switch de mecánicas (KS1; D-020, pregunta 31 b): mech_enabled.* (defaults de GameConfig y af_mech_off en
                // desarrollo) y level_overrides (fuente local vacía hasta AF.RemoteConfig), con los ids buscados en los paquetes.
                MechanicSwitches switches = LaunchOptions.ResolveMechSwitches();
                LevelOverrides overrides = LevelOverrides.Create(LaunchOptions.ResolveLevelOverridesJson(), packs);
                for (int i = 0; i < overrides.Problems.Count; i++)
                {
                    Debug.LogWarning(LogPrefix + overrides.Problems[i]);
                }

                LevelCatalog catalog = LevelCatalog.Create(packs, LaunchOptions.ResolveLevelOffset(), overrides, switches, w => Debug.LogWarning(LogPrefix + w));
                watch.Stop();
                if (DevProbe.Enabled)
                {
                    Debug.Log("AF_PERF {\"event\":\"levels_load\",\"packs\":" + packs.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + ",\"levels\":" + catalog.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + ",\"ms\":" + watch.ElapsedMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
                }

                if (!catalog.IsContiguous)
                {
                    Debug.LogError(LogPrefix + "faltan niveles en el catálogo: " + catalog.DescribeGaps() + ". Al llegar a ellos se verá «El río sigue creciendo».");
                }

                if (switches.AnyOff)
                {
                    Debug.LogWarning(LogPrefix + switches + ": " + catalog.OverriddenLevels.Count + " niveles con override y "
                        + catalog.FallbackLevels.Count + " sin él (se sirven publicados, con aviso al cargarlos).");
                }

                if (catalog.LevelOffset > 0)
                {
                    Debug.LogWarning(LogPrefix + "desplazamiento de nivel de desarrollo: " + catalog.LevelOffset + " (niveles " + catalog.FirstLevel + "-" + catalog.LastLevel + ").");
                }

                return catalog;
            }
            catch (LevelFormatException e)
            {
                Debug.LogError(LogPrefix + "el paquete de niveles no es válido: " + e.Message);
                return null;
            }
        }

        // 08 §2.3: profile = Save.LoadLocal() ?? Profile.New(). La primera vez (no había perfil ni progreso de v0) se entra
        // directamente al nivel 1. El af_v0_level de PlayerPrefs se migra una vez (B1-5), solo con el almacén real. Con un
        // perfil más nuevo o una migración fallida (08 §4.4) el almacén no deja guardar: el perfil en blanco de esta sesión no
        // se guarda nunca (ProfileLocked).
        private static Profile LoadProfile(IProfileStore store, IClock clock, bool migrateLegacy, out bool firstRun, out ProfileLoadStatus status)
        {
            ProfileLoadResult result = store.Load();
            status = result.Status;
            if (result.Error != null)
            {
                Debug.LogWarning(LogPrefix + "perfil (" + result.Status + "): " + result.Error);
            }

            if (IsLocked(result.Status))
            {
                Debug.LogWarning(LogPrefix + "perfil en solo lectura (" + result.Status + "): se juega sin guardar y sin compras hasta actualizar la app (08 §4.4).");
            }

            Profile profile = result.Profile ?? Wallet.NewProfile(clock);
            bool isNew = result.Profile == null;
            firstRun = result.Status == ProfileLoadStatus.NotFound;
            if (migrateLegacy)
            {
                LegacyProgressOutcome outcome = ProgressMigration.MigrateV0(profile, isNew, store, new PlayerPrefsProgressStore());
                if (outcome == LegacyProgressOutcome.Migrated || outcome == LegacyProgressOutcome.MigratedNotSaved)
                {
                    firstRun = false;
                    Debug.Log(LogPrefix + "progreso de v0 (" + PlayerPrefsProgressStore.Key + ") migrado al perfil: siguiente nivel " + profile.Progress.NextLevel + (outcome == LegacyProgressOutcome.MigratedNotSaved ? " (sin guardar aún)." : "."));
                }
            }

            if (isNew && firstRun)
            {
                // Perfil de primera ejecución: se guarda ya, así la siguiente apertura va a la pantalla de inicio.
                store.TrySave(profile);
            }

            return profile;
        }

        private static bool IsLocked(ProfileLoadStatus status)
        {
            return status == ProfileLoadStatus.TooNew || status == ProfileLoadStatus.MigrationFailed;
        }

        // El arte procedural se genera una vez por sesión y por agua base (guía visual v2, 8: la generación al arrancar
        // cuesta CPU): recargar la escena o pasar de nivel no lo repite. Vive mientras dure la app.
        private static ProceduralArt SharedArt(string waterBase)
        {
            string key = waterBase;
            if (s_art == null || s_artKey != key)
            {
                s_art?.Dispose();
                s_art = new ProceduralArt(waterBase);
                s_artKey = key;
            }

            return s_art;
        }

        private void OnApplicationQuit()
        {
            PersistNow("cierre");
        }

        // 02 §14 y T-D: al pasar a segundo plano, al cerrar la app y al destruir la escena, el perfil queda en disco con un solo
        // tope de espera para todo el cierre (ProfileSaveBatch.SyncTimeoutMs). Lo pendiente de la cartera, los anuncios, la
        // tienda y el nivel en curso (una escritura fallida) se anota dentro de un lote: como todos guardan el mismo perfil,
        // sus Flush solo limpian su indicador y una sola escritura síncrona (la del cierre del lote o FlushSync, con el tiempo
        // que quede) lo lleva entero. Si no llega al disco, el lote lo sigue teniendo pendiente.
        private void PersistNow(string when)
        {
            ProfileSaveBatch? saves = _saveBatch;
            if (saves == null)
            {
                return;
            }

            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            using (saves.Begin())
            {
                _wallet?.Flush();
                _ads?.Ledger.Flush();
                _storeLedger?.Flush();
                (_flow?.StateStore as ProfileLevelStateStore)?.Flush(); // el nivel en curso ya está en disco
            }

            long elapsedMs = (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000L / System.Diagnostics.Stopwatch.Frequency;
            int left = (int)Math.Max(0L, saves.SyncTimeoutMs - elapsedMs);
            bool onDisk = saves.FlushSync(left);
            ReportBackgroundFailures(saves);
            if (!onDisk && !ProfileLocked)
            {
                Debug.LogWarning(LogPrefix + "el perfil no quedó en disco (" + when + "): sigue en memoria y sale con el siguiente guardado.");
            }
        }

        // T-D: un fallo del escritor de segundo plano se recoge en el hilo principal al siguiente guardado, sea cual sea (un
        // vertido, Marea, un comodín, un anuncio o PersistNow). Se compara con la última cifra registrada, no con una
        // instantánea del toque, para que ninguno pase en silencio.
        private void ReportBackgroundFailures(ProfileSaveBatch saves)
        {
            int failures = saves.BackgroundFailures;
            if (failures == _reportedBackgroundFailures)
            {
                return;
            }

            int missed = failures - _reportedBackgroundFailures;
            _reportedBackgroundFailures = failures;
            Debug.LogWarning(LogPrefix + "falló la escritura del perfil en segundo plano (fallos nuevos: " + missed + "); sigue en memoria y sale con el siguiente guardado: " + saves.LastBackgroundError);
        }

        // Fin de la carga con la pantalla de arranque delante (tarea TI6d): en la primera apertura con TiTino a la vista
        // (sloth_wake), se espera a que su gesto lleve BootSplashTiming.WakeMinVisibleMs a la vista si cabe en el objetivo de
        // 20 s; en las demás, y sin TiTino, se corta ya (no se añade espera). Deja el informe y la sonda AF_STATE boot.
        private IEnumerator FinishSplash(bool firstOpen)
        {
            BootSplash splash = _splash!;
            long loadMs = splash.AgeMs;
            long sinceStartup = SinceStartupMs();
            bool wake = splash.SlothVisible && splash.Sloth!.Pose == SlothPoseId.Wake;
            long need = BootSplashTiming.NeedMs(firstOpen, wake, (long)splash.SlothClockMs);
            int hold = BootSplashTiming.HoldMs(firstOpen, wake, sinceStartup, (long)splash.SlothClockMs);
            if (hold > 0)
            {
                // Hasta que el gesto lleve su segundo a la vista (reloj de la pose, el que ve el jugador), con un tope de tiempo
                // real por si los fotogramas van lentos.
                double until = Time.realtimeSinceStartupAsDouble + ((hold + HoldGuardMs) / 1000.0);
                while (_splash != null && splash.SlothClockMs < BootSplashTiming.WakeMinVisibleMs && Time.realtimeSinceStartupAsDouble < until)
                {
                    yield return null;
                }
            }

            // El reloj se lee una sola vez al cortar. Sin espera, el corte es el fin de la carga: hold_ms = 0 y splash_ms = load_ms
            // por construcción, aunque el reloj real haya pasado de milisegundo entre dos lecturas.
            long endMs = hold > 0 ? splash.AgeMs : loadMs;
            long endSinceStartup = hold > 0 ? SinceStartupMs() : sinceStartup;
            _bootReport = new BootReport
            {
                FirstOpen = firstOpen,
                SlothPose = splash.SlothVisible ? SlothRig.IdOf(splash.Sloth!.Pose) : null,
                LoadMs = loadMs,
                SplashMs = endMs,
                SlothMs = (long)splash.SlothClockMs,
                HoldMs = endMs - loadMs,
                WakeFits = need <= 0 || hold > 0,
                SplashEndMs = endSinceStartup,
            };
            CutSplash();
            _probe?.Boot(_bootReport);
        }

        // Tope de tiempo real por encima de la espera calculada (fotogramas lentos) [P].
        private const int HoldGuardMs = 250;

        private void CutSplash()
        {
            _splash?.Dispose();
            _splash = null;
        }

        // Desde el arranque (ms): en el teléfono, desde que arranca Unity (realtimeSinceStartup), sin el lanzamiento del proceso
        // ni el de la Activity, que van antes; en el Editor, que cuenta desde que se abrió el Editor, desde el Awake de esta escena
        // (_bootOrigin). Para el arranque en frío de 09 §6.1 (< 20 s) hay que sumarle lo que mide el sistema hasta Unity
        // (adb shell am start -W, TotalTime). Los tests le suman BootSplash.TestStartupOffsetMs (0 fuera de ellos).
        private long SinceStartupMs()
        {
            return (long)((Time.realtimeSinceStartupAsDouble - _bootOrigin) * 1000.0) + BootSplash.TestStartupOffsetMs;
        }

        // Primer toque del jugador tras la pantalla de arranque (sonda AF_STATE boot, first_touch_ms; 09 §6): una cuia, el fondo
        // o la intro del nivel, o «Jugar» en el inicio. Solo el primero, y solo en los arranques con la pantalla.
        private void NoteFirstTouch(AppScreen screen)
        {
            BootReport? report = _bootReport;
            if (report == null || report.FirstTouchMs >= 0)
            {
                return;
            }

            report.FirstTouchMs = SinceStartupMs();
            report.FirstTouchScreen = screen == AppScreen.Level ? "level" : "lobby";
            report.FirstTouchLevel = screen == AppScreen.Level && _flow != null && _flow.HasLevel ? _flow.LevelNumber : (_app?.PlayLevel ?? 0);
            _probe?.Boot(report);
        }

        private void OnDestroy()
        {
            CutSplash();
            PersistNow("destruir la escena");

            // Si la escena se va con el Mapa abierto (una recarga, RebuildUi), su Close no llega: el intervalo de pintado es global
            // y se quedaría en el de reposo del Mapa (30 fps) para el nivel siguiente. Sin tocar la pantalla, que puede estar ya
            // destruida en este punto.
            RiverMapScreen.RestoreFullRenderRate();
            if (_backAction != null)
            {
                _backAction.performed -= OnBackPerformed;
                _backAction.Dispose();
                _backAction = null;
            }

            if (Current == this)
            {
                Current = null;
            }
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            // Sin acciones asignadas, InputSystemUIInputModule toma las acciones por defecto al activarse.
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
