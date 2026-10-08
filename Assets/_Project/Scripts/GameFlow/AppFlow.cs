#nullable enable
using System;
using AF.Economy;
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>Pantalla de la app que gobierna <see cref="AppFlow"/> (08 §2.2 <c>AF.GameFlow</c>).</summary>
    public enum AppScreen
    {
        /// <summary>Arranque: aún no se ha decidido la primera pantalla.</summary>
        Boot = 0,

        /// <summary>Pantalla de inicio (Lobby, 06 §4.7).</summary>
        Lobby = 1,

        /// <summary>Pantalla de nivel (02 §2, la de <see cref="LevelFlow"/>).</summary>
        Level = 2,

        /// <summary>«El río sigue creciendo» (06 §9; 08 §6.2): el nivel pedido aún no está publicado.</summary>
        ContentEnd = 3,

        /// <summary>Sin canoas (04 §7.3): desde el 31, sin canoas ni Canoas infinitas.</summary>
        NoCanoes = 4,
    }

    /// <summary>Nombres de <see cref="AppScreen"/> para la línea <c>AF_STATE</c>.</summary>
    public static class AppScreenNames
    {
        /// <summary>Nombre en minúsculas (p. ej. <c>content_end</c>).</summary>
        public static string Of(AppScreen screen)
        {
            switch (screen)
            {
                case AppScreen.Boot: return "boot";
                case AppScreen.Lobby: return "lobby";
                case AppScreen.Level: return "level";
                case AppScreen.ContentEnd: return "content_end";
                case AppScreen.NoCanoes: return "no_canoes";
                default: return "unknown";
            }
        }
    }

    /// <summary>Vista de <see cref="AppFlow"/>: se llama de forma síncrona en cada cambio de pantalla, antes de cargar el nivel.</summary>
    public interface IAppFlowListener
    {
        /// <summary>La app pasa a <paramref name="screen"/>.</summary>
        void OnAppScreenChanged(AppScreen screen);
    }

    /// <summary>
    /// Navegación entre pantallas, sin motor (tarea B1-6; 08 §2.2 y §2.3; D-008 punto 6): Boot → Nivel 1 la primera vez, o
    /// Inicio; Inicio → «Jugar · Nivel {n}» → Nivel → Victoria; desde la victoria, Siguiente → N + 1 o Inicio → Inicio.
    /// <list type="bullet">
    /// <item><description>Si el nivel pedido no está en el catálogo (después del último publicado o en un hueco), se abre
    /// «El río sigue creciendo» (08 §6.2; 06 §9).</description></item>
    /// <item><description>Desde el 31 (<c>rescue_min_level</c>), Jugar o Siguiente sin canoas ni Canoas infinitas abre Sin canoas
    /// (04 §7.1 «Qué bloquea» y §7.3; 06 §4.7), y también tocar el contador de canoas en 0/5. Con una canoa, vuelve al inicio
    /// (<see cref="RefreshNoCanoes"/>).</description></item>
    /// <item><description>Desde el nivel se sale al inicio con <see cref="ExitToLobby"/> (Nivel fallido, Sin canoas o la pausa
    /// en 1-30).</description></item>
    /// <item><description>El intersticial tras Siguiente (AD_GATE, 05 §6.3) llega con B2-4: irá entre <see cref="PressNext"/>
    /// y la carga del nivel siguiente.</description></item>
    /// </list>
    /// No es seguro entre hilos.
    /// </summary>
    public sealed class AppFlow
    {
        private readonly LevelFlow _level;
        private readonly LevelCatalog _catalog;
        private readonly IProgressStore _progress;
        private readonly Wallet _wallet;
        private readonly IAppFlowListener _listener;

        /// <summary>Crea la navegación; no muestra nada hasta <see cref="Start"/> o <see cref="StartAtLevel"/>.</summary>
        public AppFlow(LevelFlow level, IProgressStore progress, Wallet wallet, IAppFlowListener listener)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _catalog = level.Catalog;
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            Screen = AppScreen.Boot;
        }

        /// <summary>Pantalla actual.</summary>
        public AppScreen Screen { get; private set; }

        /// <summary>Flujo del nivel.</summary>
        public LevelFlow Level => _level;

        /// <summary>Cartera (semillas y canoas del inicio).</summary>
        public Wallet Wallet => _wallet;

        /// <summary>Catálogo de niveles.</summary>
        public LevelCatalog Catalog => _catalog;

        /// <summary>
        /// Nivel del botón «Jugar · Nivel {n}»: el <c>next_level</c> guardado (08 §4.3), como mínimo el primero del catálogo
        /// (sin progreso, o con un desplazamiento de desarrollo).
        /// </summary>
        public int PlayLevel
        {
            get
            {
                int next = _progress.LoadNextLevel();
                return next < _catalog.FirstLevel ? _catalog.FirstLevel : next;
            }
        }

        /// <summary>El nivel de <see cref="PlayLevel"/> está publicado.</summary>
        public bool PlayLevelAvailable => _catalog.Contains(PlayLevel);

        /// <summary>Etiqueta de dificultad del nivel de <see cref="PlayLevel"/> (<c>normal</c> si no está publicado).</summary>
        public string PlayLevelLabel
        {
            get
            {
                LevelDefinition? def = _catalog.Find(PlayLevel);
                return def == null ? LevelDefinition.LabelNormal : def.DifficultyLabel;
            }
        }

        /// <summary>El contador de canoas se ve en el inicio: desde <c>rescue_min_level</c> (31; 06 §4.7).</summary>
        public bool CanoesVisible => _wallet.CanoesActive(PlayLevel);

        /// <summary>Nivel que abrió «El río sigue creciendo» o Sin canoas (0 si ninguno).</summary>
        public int OverlayLevel { get; private set; }

        /// <summary>Días estimados hasta los próximos niveles (<c>live_content_end_message.next_batch_eta_days</c>, 08 §9).</summary>
        public int ContentEndEtaDays => GameConfig.LiveContentEndNextBatchEtaDays;

        /// <summary>
        /// Primera pantalla (08 §2.3): la primera vez (<paramref name="firstRun"/>: no había perfil), directamente el nivel 1;
        /// si no, el inicio.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ya había arrancado.</exception>
        public void Start(bool firstRun)
        {
            RequireBoot();
            if (firstRun)
            {
                Enter(PlayLevel);
            }
            else
            {
                Go(AppScreen.Lobby);
            }
        }

        /// <summary>
        /// Arranca directamente en el nivel <paramref name="level"/> (o en el primero si no existe), sin pasar por el inicio
        /// ni por la compuerta de canoas: extra <c>af_level</c> de los builds de desarrollo y tests.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ya había arrancado.</exception>
        public void StartAtLevel(int level)
        {
            RequireBoot();
            Go(AppScreen.Level);
            _level.Load(_level.ResolveLevel(level));
        }

        /// <summary>«Jugar · Nivel {n}» en el inicio.</summary>
        public bool PressPlay()
        {
            return Screen == AppScreen.Lobby && Enter(PlayLevel);
        }

        /// <summary>Siguiente en la pantalla de victoria (sin auto-avance, 02 §6): N + 1, o la pantalla que lo impida.</summary>
        public bool PressNext()
        {
            if (Screen != AppScreen.Level || _level.Phase != FlowPhase.VictoryScreen)
            {
                return false;
            }

            return Enter(_level.LevelNumber + 1);
        }

        /// <summary>«Inicio» en la pantalla de victoria [P] (06 §4.7; D-008 punto 3): vuelve al inicio.</summary>
        public bool PressHome()
        {
            if (Screen != AppScreen.Level || _level.Phase != FlowPhase.VictoryScreen)
            {
                return false;
            }

            Go(AppScreen.Lobby);
            return true;
        }

        /// <summary>Toque en el contador de canoas del inicio: con 0/5 (sin Canoas infinitas) abre Sin canoas (06 §4.7).</summary>
        public bool PressCanoes()
        {
            if (Screen != AppScreen.Lobby || !CanoesVisible)
            {
                return false;
            }

            CanoesStatus canoes = _wallet.Canoes;
            if (canoes.IsInfinite || canoes.Count > 0)
            {
                return false;
            }

            OverlayLevel = PlayLevel;
            Go(AppScreen.NoCanoes);
            return true;
        }

        /// <summary>
        /// Salir del nivel al inicio (06 §4.7): X de Nivel fallido, «Volver al inicio» de Sin canoas o pausa › «Salir» en 1-30
        /// (sin coste; D-008). Desde el 31, «Salir» pasa antes por el fallo duro y Nivel fallido (<see cref="LevelFlow.ConfirmQuit"/>).
        /// </summary>
        public bool ExitToLobby()
        {
            if (Screen != AppScreen.Level)
            {
                return false;
            }

            Go(AppScreen.Lobby);
            return true;
        }

        /// <summary>
        /// Sin canoas abierta desde el inicio (Jugar o el contador): en cuanto hay una canoa (espera, video, semillas o compra)
        /// se vuelve al inicio, donde Jugar ya funciona [P] (04 §7.3 solo fija la vuelta a Nivel fallido). Devuelve si cambió.
        /// </summary>
        public bool RefreshNoCanoes()
        {
            if (Screen != AppScreen.NoCanoes || !_wallet.CanStartLevel(OverlayLevel > 0 ? OverlayLevel : PlayLevel))
            {
                return false;
            }

            OverlayLevel = 0;
            Go(AppScreen.Lobby);
            return true;
        }

        /// <summary>Volver desde «El río sigue creciendo» o Sin canoas: al inicio.</summary>
        public bool CloseOverlay()
        {
            if (Screen != AppScreen.ContentEnd && Screen != AppScreen.NoCanoes)
            {
                return false;
            }

            OverlayLevel = 0;
            Go(AppScreen.Lobby);
            return true;
        }

        private bool Enter(int level)
        {
            if (!_catalog.Contains(level))
            {
                OverlayLevel = level;
                Go(AppScreen.ContentEnd);
                return true;
            }

            if (!_wallet.CanStartLevel(level))
            {
                OverlayLevel = level;
                Go(AppScreen.NoCanoes);
                return true;
            }

            OverlayLevel = 0;
            Go(AppScreen.Level);
            _level.Load(level);
            return true;
        }

        private void Go(AppScreen screen)
        {
            Screen = screen;
            _listener.OnAppScreenChanged(screen);
        }

        private void RequireBoot()
        {
            if (Screen != AppScreen.Boot)
            {
                throw new InvalidOperationException("AppFlow ya arrancó.");
            }
        }
    }
}
