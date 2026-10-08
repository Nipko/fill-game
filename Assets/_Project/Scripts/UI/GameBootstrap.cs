#nullable enable
using System;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace AF.UI
{
    /// <summary>
    /// Arranque de la escena <c>Game</c> del prototipo v0 (docs/dev/prototipo-v0.md c): lee el paquete de niveles
    /// (TextAsset), genera los gráficos provisionales, construye la pantalla y el <see cref="LevelFlow"/> y lo hace
    /// avanzar cada fotograma. Sus métodos públicos de input son los mismos que llaman los toques y los tests PlayMode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        /// <summary>Tope del paso de tiempo por fotograma (evita saltos tras un tirón).</summary>
        private const float MaxDeltaMs = 100f;

        [SerializeField]
        private TextAsset? levelPack;

        private static ProceduralArt? s_art;
        private static string? s_artKey;

        private ProceduralArt? _art;
        private GameScreen? _screen;
        private LevelFlow? _flow;
        private float _msCarry;
        private bool _skipNextDelta;

        /// <summary>Instancia activa (una por escena).</summary>
        public static GameBootstrap? Current { get; private set; }

        /// <summary>Flujo del nivel (null hasta <c>Start</c>).</summary>
        public LevelFlow? Flow => _flow;

        /// <summary>Pantalla (null hasta <c>Start</c>).</summary>
        public GameScreen? View => _screen;

        /// <summary>Paquete de niveles asignado.</summary>
        public TextAsset? LevelPackAsset => levelPack;

        /// <summary>Asigna el paquete de niveles (escena generada por <c>GameSceneBuilder</c>).</summary>
        public void Configure(TextAsset pack)
        {
            levelPack = pack;
        }

        /// <summary>Toque en una cuia (02 §3.1).</summary>
        public TapResult TapCuia(int index)
        {
            return _flow == null ? TapResult.Ignored(index, -1) : _flow.TapCuia(index);
        }

        /// <summary>Toque en el fondo (C7).</summary>
        public void TapBackground()
        {
            _flow?.TapBackground();
        }

        /// <summary>Toque en cualquier punto durante la intro o la celebración.</summary>
        public bool TapAnywhere()
        {
            return _flow != null && _flow.TapAnywhere();
        }

        /// <summary>Marea (bandeja o modal). C8: primero quita la selección (<see cref="LevelFlow.PressUndo"/>).</summary>
        public bool PressUndo()
        {
            return _flow != null && _flow.PressUndo();
        }

        /// <summary>Volver a la orilla (cabecera o modal). C8: primero quita la selección (<see cref="LevelFlow.PressRestart"/>).</summary>
        public bool PressRestart()
        {
            return _flow != null && _flow.PressRestart();
        }

        /// <summary>Siguiente (pantalla de victoria).</summary>
        public bool PressNext()
        {
            return _flow != null && _flow.Next();
        }

        private void Awake()
        {
            Current = this;
            Application.targetFrameRate = 60; // 08 §5.4
            DevProbe.Initialize();
            EnsureEventSystem();
        }

        private void Start()
        {
            if (levelPack == null)
            {
                Debug.LogError("[AF.GameBootstrap] falta el paquete de niveles (TextAsset). Regenera la escena con AF.UI.Editor.GameSceneBuilder.CreateGameScene.");
                enabled = false;
                return;
            }

            LevelPack pack;
            try
            {
                pack = LevelPackReader.Read(levelPack.text);
            }
            catch (LevelFormatException e)
            {
                // Un paquete roto no deja la pantalla en blanco con una excepción en Start: se registra y se para.
                Debug.LogError("[AF.GameBootstrap] el paquete de niveles no es válido: " + e.Message);
                enabled = false;
                return;
            }

            _art = SharedArt(pack.WaterBase);
            var probe = new DevProbe();
            _screen = new GameScreen(
                transform,
                _art,
                probe,
                i => TapCuia(i),
                () => TapBackground(),
                () => TapAnywhere(),
                () => PressUndo(),
                () => PressRestart(),
                () => PressNext());
            IProgressStore progress = LaunchOptions.ProgressStoreOverride ?? new PlayerPrefsProgressStore();
            try
            {
                // rules_pour_mode (D-001): se resuelve una vez al arrancar y se aplica en cada carga de nivel.
                _flow = new LevelFlow(pack, progress, _screen, GameConfig.TapSwitchSelection, LaunchOptions.ResolvePourMode());
            }
            catch (NotSupportedException e)
            {
                Debug.LogError("[AF.GameBootstrap] " + e.Message);
                enabled = false;
                return;
            }

            _screen.Bind(_flow);

            int requested = LaunchOptions.StartLevelOverride;
            if (requested <= 0)
            {
                requested = LaunchOptions.ReadIntentLevel();
            }

            if (requested > 0)
            {
                _flow.Load(_flow.ResolveLevel(requested));
            }
            else
            {
                _flow.LoadSaved();
            }

            _skipNextDelta = true;
        }

        private void Update()
        {
            if (_flow == null || _screen == null)
            {
                return;
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime * 1000f, MaxDeltaMs);
            if (_skipNextDelta)
            {
                _skipNextDelta = false;
                dt = 0f;
            }

            _screen.Tick(dt);
            _msCarry += dt;
            int whole = (int)_msCarry;
            _msCarry -= whole;
            _flow.Tick(whole, _screen.IsViewIdle);
        }

        private void OnApplicationPause(bool paused)
        {
            // 02 §14: al volver de segundo plano la vista salta al modelo sin animar y la selección se limpia.
            if (!paused && _flow != null && _screen != null)
            {
                _screen.SnapToModel();
                _flow.ClearSelection();
            }
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
