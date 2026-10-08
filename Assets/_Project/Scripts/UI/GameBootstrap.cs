#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace AF.UI
{
    /// <summary>
    /// Arranque de la escena <c>Game</c> (docs/dev/prototipo-v0.md c; 08 §2.3 en lo que ya existe): lee los paquetes de
    /// niveles (TextAsset) y arma el catálogo (tarea B1-5), carga el perfil local (<c>AF.Save</c>, migrando una vez el
    /// <c>af_v0_level</c> de v0), crea la cartera, genera los gráficos provisionales, construye las pantallas de inicio y de
    /// nivel, el <see cref="LevelFlow"/> y el <see cref="AppFlow"/> (tarea B1-6) y los hace avanzar cada fotograma. La primera
    /// vez entra directamente al nivel 1; después, a la pantalla de inicio. Sus métodos públicos de input son los mismos que
    /// llaman los toques y los tests PlayMode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour, IAppFlowListener
    {
        /// <summary>Tope del paso de tiempo por fotograma (evita saltos tras un tirón).</summary>
        private const float MaxDeltaMs = 100f;

        private const string LogPrefix = "[AF.GameBootstrap] ";

        [SerializeField]
        private TextAsset? levelPack;

        [SerializeField]
        private TextAsset[] extraLevelPacks = Array.Empty<TextAsset>();

        private static ProceduralArt? s_art;
        private static string? s_artKey;

        private ProceduralArt? _art;
        private GameScreen? _screen;
        private LobbyScreen? _lobby;
        private LevelFlow? _flow;
        private AppFlow? _app;
        private Wallet? _wallet;
        private Profile? _profile;
        private IProfileStore? _profileStore;
        private DevProbe? _probe;
        private float _msCarry;
        private bool _skipNextDelta;

        /// <summary>Instancia activa (una por escena).</summary>
        public static GameBootstrap? Current { get; private set; }

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

        /// <summary>Perfil cargado (null hasta <c>Start</c>).</summary>
        public Profile? Profile => _profile;

        /// <summary>Almacén del perfil (null hasta <c>Start</c>).</summary>
        public IProfileStore? ProfileStore => _profileStore;

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

        /// <summary>Asigna el paquete de niveles (escena generada por <c>GameSceneBuilder</c>).</summary>
        public void Configure(TextAsset pack)
        {
            Configure(new[] { pack });
        }

        /// <summary>
        /// Asigna los paquetes de niveles (08 §2.2: índice nivel → paquete; hoy <c>tutorial_v0</c>, en la integración también
        /// <c>mundo_02</c> … <c>mundo_05</c>). El primero va en <c>levelPack</c> y el resto en <c>extraLevelPacks</c>.
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

        /// <summary>Toque en una cuia (02 §3.1).</summary>
        public TapResult TapCuia(int index)
        {
            return _flow == null || !InLevel ? TapResult.Ignored(index, -1) : _flow.TapCuia(index);
        }

        /// <summary>Toque en el fondo (C7).</summary>
        public void TapBackground()
        {
            if (InLevel)
            {
                _flow?.TapBackground();
            }
        }

        /// <summary>Toque en cualquier punto durante la intro o la celebración.</summary>
        public bool TapAnywhere()
        {
            return _flow != null && InLevel && _flow.TapAnywhere();
        }

        /// <summary>Marea (bandeja o modal). C8: primero quita la selección (<see cref="LevelFlow.PressUndo"/>).</summary>
        public bool PressUndo()
        {
            return _flow != null && InLevel && _flow.PressUndo();
        }

        /// <summary>Volver a la orilla (cabecera o modal). C8: primero quita la selección (<see cref="LevelFlow.PressRestart"/>).</summary>
        public bool PressRestart()
        {
            return _flow != null && InLevel && _flow.PressRestart();
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

        /// <summary>«Jugar · Nivel {n}» (pantalla de inicio).</summary>
        public bool PressPlay()
        {
            return _app != null && _app.PressPlay();
        }

        /// <summary>Contador de canoas de la pantalla de inicio (con 0/5 abre Sin canoas).</summary>
        public bool PressCanoes()
        {
            return _app != null && _app.PressCanoes();
        }

        /// <summary>Volver desde «El río sigue creciendo» o Sin canoas.</summary>
        public bool PressBack()
        {
            return _app != null && _app.CloseOverlay();
        }

        /// <inheritdoc />
        public void OnAppScreenChanged(AppScreen screen)
        {
            // La pantalla de nivel se muestra antes de cargar el nivel (AppFlow llama a Load después).
            _screen?.SetVisible(screen == AppScreen.Level);
            _lobby?.Show(screen);
            _skipNextDelta = true;
        }

        private bool InLevel => _app == null || _app.Screen == AppScreen.Level;

        private void Awake()
        {
            Current = this;
            Application.targetFrameRate = 60; // 08 §5.4
            DevProbe.Initialize();
            EnsureEventSystem();
        }

        private void Start()
        {
            LevelCatalog? catalog = LoadCatalog();
            if (catalog == null)
            {
                enabled = false;
                return;
            }

            IClock clock = LaunchOptions.ClockOverride ?? SystemClock.Instance;
            IProfileStore store = LaunchOptions.ProfileStoreOverride ?? new LocalProfileStore(Application.persistentDataPath);
            Profile profile = LoadProfile(store, clock, LaunchOptions.ProfileStoreOverride == null, out bool firstRun);
            _profile = profile;
            _profileStore = store;
            _wallet = new Wallet(profile, store, clock);
            IProgressStore progress = LaunchOptions.ProgressStoreOverride ?? new ProfileProgressStore(profile, store);

            _art = SharedArt(catalog.WaterBase);
            _probe = new DevProbe();
            _screen = new GameScreen(
                transform,
                _art,
                _probe,
                i => TapCuia(i),
                () => TapBackground(),
                () => TapAnywhere(),
                () => PressUndo(),
                () => PressRestart(),
                () => PressNext(),
                () => PressHome());
            _screen.SetVisible(false);
            _lobby = new LobbyScreen(transform, _art, _probe, () => PressPlay(), () => PressCanoes(), () => { }, () => PressBack());
            try
            {
                // rules_pour_mode (D-005): se resuelve una vez al arrancar y se aplica en cada carga de nivel.
                _flow = new LevelFlow(catalog, progress, _screen, GameConfig.TapSwitchSelection, LaunchOptions.ResolvePourMode());
            }
            catch (NotSupportedException e)
            {
                Debug.LogError(LogPrefix + e.Message);
                enabled = false;
                return;
            }

            _screen.Bind(_flow);
            _app = new AppFlow(_flow, progress, _wallet, this);
            _lobby.Bind(_app);

            int requested = LaunchOptions.StartLevelOverride;
            if (requested <= 0)
            {
                requested = LaunchOptions.ReadIntentLevel();
            }

            if (requested > 0)
            {
                _app.StartAtLevel(requested);
            }
            else
            {
                _app.Start(firstRun);
            }

            _skipNextDelta = true;
        }

        private void Update()
        {
            if (_flow == null || _screen == null || _app == null)
            {
                return;
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime * 1000f, MaxDeltaMs);
            if (_skipNextDelta)
            {
                _skipNextDelta = false;
                dt = 0f;
            }

            if (_app.Screen != AppScreen.Level)
            {
                _lobby?.Tick(dt);
                return;
            }

            _screen.Tick(dt);
            _msCarry += dt;
            int whole = (int)_msCarry;
            _msCarry -= whole;
            _flow.Tick(whole, _screen.IsViewIdle);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                // Lo pendiente de la cartera (una escritura fallida) se reintenta al pasar a segundo plano.
                _wallet?.Flush();
                return;
            }

            // 02 §14: al volver de segundo plano la vista salta al modelo sin animar y la selección se limpia.
            if (_flow != null && _screen != null && InLevel)
            {
                _screen.SnapToModel();
                _flow.ClearSelection();
            }
        }

        // Paquetes de la escena (o el del test, LaunchOptions.LevelPackJsonOverride) → catálogo con el desplazamiento de
        // desarrollo (af_level_offset). Un paquete roto no deja la pantalla en blanco con una excepción en Start: se registra.
        private LevelCatalog? LoadCatalog()
        {
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

                LevelCatalog catalog = LevelCatalog.Create(packs, LaunchOptions.ResolveLevelOffset());
                if (!catalog.IsContiguous)
                {
                    Debug.LogError(LogPrefix + "faltan niveles en el catálogo: " + catalog.DescribeGaps() + ". Al llegar a ellos se verá «El río sigue creciendo».");
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
        // directamente al nivel 1. El af_v0_level de PlayerPrefs se migra una vez (B1-5), solo con el almacén real.
        private static Profile LoadProfile(IProfileStore store, IClock clock, bool migrateLegacy, out bool firstRun)
        {
            ProfileLoadResult result = store.Load();
            if (result.Error != null)
            {
                Debug.LogWarning(LogPrefix + "perfil (" + result.Status + "): " + result.Error);
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

        private void OnDestroy()
        {
            _wallet?.Flush();
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
