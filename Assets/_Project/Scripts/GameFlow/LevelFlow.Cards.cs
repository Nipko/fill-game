#nullable enable
using AF.Economy;

namespace AF.GameFlow
{
    /// <summary>
    /// Tarjeta previa de 1 toque (tarea T10 del paso 1; 02 §10; propuesta de D-009 §c.1): LOADING → LEVEL_CARD → INTRO en los
    /// niveles Difícil y Muy difícil y en los estrenos de una mecánica (donde el tutorial hace de tarjeta), con el contenido de
    /// <see cref="LevelCardInfo"/>. Va apagada por defecto (<see cref="LevelCardsEnabled"/>): la enciende <c>GameBootstrap</c>
    /// con <c>GameConfig.LevelCardsEnabledDefault</c>, que pasa a true cuando existe la vista (T13); así ningún build se queda
    /// en una fase sin vista. No sale al reanudar (02 §14) ni, por defecto, al reintentar (<see cref="LevelCardOnRetry"/> [P]).
    /// </summary>
    /// <remarks>
    /// Banner de entrada a un mundo (07 §2.2, regla 1; 02 §6; <see cref="WorldBanners"/>; decisión del dueño del 2026-10-11): en
    /// el primer nivel de cada mundo desde el 2 (11, 41, 81 y 141), al cargarlo con Jugar o Siguiente y solo en el primer
    /// intento (al entrar en él, no al volver a entrar en el mismo nivel tras salir al inicio), usa la misma retención: el flujo
    /// se queda en LEVEL_CARD con <see cref="WorldBanner"/> y la intro espera. Un toque lo cierra
    /// (<see cref="DismissWorldBanner"/>, o <see cref="DismissLevelCard"/> y los toques que la llaman) y el flujo sigue en
    /// LEVEL_CARD mientras sale (<see cref="WorldBannerExitMs"/>, <see cref="WorldBannerClosing"/>): solo al terminar la salida,
    /// si el nivel tiene además tarjeta previa, sale la tarjeta, y si no, arranca la intro (07 §5.1, 07:274: «El banner de mundo
    /// … se muestra antes de esta animación, no encima»; D-012, punto 2). Un segundo toque durante la salida la termina en el
    /// acto. Como es LEVEL_CARD, todo lo que espera a la tarjeta espera también al banner (los avisos y el regalo de la
    /// Luciérnaga, la pausa inerte bajo el velo). No sale al reanudar ni al reintentar. Va con su propio interruptor
    /// (<see cref="WorldBannersEnabled"/>, apagado por defecto), que el juego enciende junto con las tarjetas.
    /// </remarks>
    public sealed partial class LevelFlow
    {
        private LevelCardInfo? _card;
        private int _cardIntroMs;
        private bool _cardIntroShort;
        private string? _cardRiverType;
        private int _cardBannerMs;
        private int _worldBanner;
        private bool _bannerRevealGifted;
        private bool _bannerThenCard;
        private bool _bannerClosing;

        /// <summary>
        /// Hay tarjeta previa al cargar un nivel con etiqueta o con estreno. False por defecto: los tests del flujo y cualquier
        /// vista que no implemente <see cref="ILevelCardListener"/> no la ven.
        /// </summary>
        public bool LevelCardsEnabled { get; set; }

        /// <summary>
        /// [P] La tarjeta también sale al reintentar el mismo nivel (Volver a la orilla, «Reintentar» de Nivel fallido o la rama
        /// C), no solo al entrar con Jugar o Siguiente. Por defecto <see cref="GameConfig.LevelCardOnRetry"/> (false).
        /// </summary>
        public bool LevelCardOnRetry { get; set; } = GameConfig.LevelCardOnRetry;

        /// <summary>Tarjeta abierta (en LEVEL_CARD), o null.</summary>
        public LevelCardInfo? LevelCard => _card;

        /// <summary>
        /// Hay banner de mundo al entrar en el primer nivel de un mundo (<see cref="WorldBanners"/>). False por defecto: los
        /// tests del flujo no lo ven; <c>GameBootstrap</c> lo enciende con las tarjetas (las dos cosas necesitan la vista).
        /// </summary>
        public bool WorldBannersEnabled { get; set; }

        /// <summary>Mundo del banner abierto (en LEVEL_CARD, antes de la tarjeta previa), o 0 si no hay banner.</summary>
        public int WorldBanner => _worldBanner;

        /// <summary>
        /// Al cerrar el banner abierto saldrá la tarjeta previa (el nivel la tiene y las tarjetas están encendidas); false si
        /// arrancará la intro o si no hay banner. Lo calcula <see cref="TryShowWorldBanner"/> con la misma regla que
        /// <see cref="DismissWorldBanner"/> (<see cref="LevelCardInfo.For"/>), para la sonda <c>AF_STATE world_banner</c>.
        /// </summary>
        public bool WorldBannerThenCard => _worldBanner != 0 && _bannerThenCard;

        /// <summary>
        /// Salida del banner de mundo (ms): tras el toque, el flujo sigue este tiempo en LEVEL_CARD con el banner cerrándose
        /// (<see cref="WorldBannerClosing"/>) y solo después abre la tarjeta previa o arranca la intro, para que «Llenar y teñir»
        /// no empiece debajo del banner (07 §5.1). La fija la vista con la duración de su salida (Rosa se sumerge y la tarjeta se
        /// funde; <c>GameScreen.WorldBanner.cs</c>). 0 por defecto (los tests del flujo, sin vista): el toque lo cierra en el acto.
        /// </summary>
        public int WorldBannerExitMs { get; set; }

        /// <summary>
        /// El banner de mundo ya se tocó y está saliendo: sigue en LEVEL_CARD hasta que pasan <see cref="WorldBannerExitMs"/>
        /// (o hasta otro toque, que termina la salida en el acto). False sin banner o con el banner aún abierto.
        /// </summary>
        public bool WorldBannerClosing => _worldBanner != 0 && _bannerClosing;

        /// <summary>
        /// Cierra el banner de mundo (1 toque en cualquier punto). Con <see cref="WorldBannerExitMs"/> &gt; 0, el primer toque
        /// empieza la salida (sigue en LEVEL_CARD, <see cref="WorldBannerClosing"/>) y <see cref="Tick"/> la termina; un segundo
        /// toque durante la salida, o <see cref="WorldBannerExitMs"/> = 0, la termina en el acto: si el nivel tiene tarjeta previa,
        /// la abre (sigue en LEVEL_CARD); si no, arranca la intro que esperaba. False si no hay banner abierto.
        /// </summary>
        public bool DismissWorldBanner()
        {
            if (Phase != FlowPhase.LevelCard || _worldBanner == 0)
            {
                return false;
            }

            if (!_bannerClosing && WorldBannerExitMs > 0)
            {
                _bannerClosing = true; // la salida: la tarjeta previa o la intro, cuando termine (TickWorldBannerExit)
                _phaseMs = 0;
                return true;
            }

            FinishWorldBanner();
            return true;
        }

        /// <summary>
        /// Cierra la tarjeta previa (1 toque en cualquier punto) y arranca la intro que esperaba (con el cartel «El río bajó»
        /// antes, si el intento sirve la variante aliviada). Con el banner de mundo abierto, cierra el banner
        /// (<see cref="DismissWorldBanner"/>). False fuera de LEVEL_CARD. <see cref="TapAnywhere"/>, <see cref="TapCuia"/> y
        /// <see cref="TapBackground"/> también la cierran.
        /// </summary>
        public bool DismissLevelCard()
        {
            if (Phase != FlowPhase.LevelCard)
            {
                return false;
            }

            if (_worldBanner != 0)
            {
                return DismissWorldBanner(); // primero el banner; la tarjeta, si la hay, va después (otro toque)
            }

            string? riverType = _cardRiverType;
            int bannerMs = _cardBannerMs;
            int introMs = _cardIntroMs;
            bool isShort = _cardIntroShort;
            ResetLevelCard();
            if (riverType != null)
            {
                _listener.OnRiverLowered(riverType, bannerMs);
            }

            StartIntro(introMs, isShort);
            return true;
        }

        // LEVEL_CARD en lugar de la intro, si las tarjetas están encendidas y el nivel la tiene (02 §10: etiqueta o estreno).
        // La intro que habría empezado (y el cartel «El río bajó» del reintento con alivio) espera a DismissLevelCard.
        private bool TryShowLevelCard(int introMs, bool isShort, bool revealGifted, string? riverType, int bannerMs)
        {
            if (!LevelCardsEnabled || _level == null || _session == null)
            {
                return false;
            }

            EconomyConfig economy = _wallet?.Config ?? EconomyConfig.Defaults;
            LevelCardInfo? card = LevelCardInfo.For(_level, _levelNumber, economy, revealGifted);
            if (card == null)
            {
                return false;
            }

            _card = card;
            _cardIntroMs = introMs;
            _cardIntroShort = isShort;
            _cardRiverType = riverType;
            _cardBannerMs = bannerMs;
            Phase = FlowPhase.LevelCard;
            _phaseMs = 0;
            (_listener as ILevelCardListener)?.OnLevelCard(card);
            return true;
        }

        // En LEVEL_CARD con el banner de mundo saliendo: cuenta la salida y, al terminarla, sigue con la tarjeta previa o la intro.
        private void TickWorldBannerExit(int deltaMs)
        {
            if (!_bannerClosing || _worldBanner == 0)
            {
                return;
            }

            _phaseMs += deltaMs;
            if (_phaseMs >= WorldBannerExitMs)
            {
                FinishWorldBanner();
            }
        }

        // Fuera el banner: la tarjeta previa si el nivel la tiene (con el regalo de la Luciérnaga que esperaba) o la intro.
        private void FinishWorldBanner()
        {
            _worldBanner = 0;
            _bannerThenCard = false;
            _bannerClosing = false;
            int introMs = _cardIntroMs;
            bool revealGifted = _bannerRevealGifted;
            _bannerRevealGifted = false;
            if (!TryShowLevelCard(introMs, false, revealGifted, null, 0))
            {
                ResetLevelCard();
                StartIntro(introMs, false);
            }
        }

        // Banner de mundo en lugar de la tarjeta y de la intro, si está encendido, el nivel es el primero de un mundo (desde el 2) y
        // es el primer intento (firstEntry: se entra en el nivel, no se vuelve a entrar en el mismo tras salir al inicio). La intro
        // (y la tarjeta previa, con el regalo de la Luciérnaga) esperan a DismissWorldBanner.
        private bool TryShowWorldBanner(int level, bool firstEntry, int introMs, bool revealGifted)
        {
            int world = WorldBanners.WorldAt(level);
            if (!WorldBannersEnabled || !firstEntry || world == 0 || _session == null)
            {
                return false;
            }

            _card = null;
            _worldBanner = world;
            _bannerClosing = false;
            _bannerRevealGifted = revealGifted;
            _bannerThenCard = LevelCardsEnabled && _level != null
                && LevelCardInfo.For(_level, _levelNumber, _wallet?.Config ?? EconomyConfig.Defaults, revealGifted) != null;
            _cardIntroMs = introMs;
            _cardIntroShort = false;
            _cardRiverType = null;
            _cardBannerMs = 0;
            Phase = FlowPhase.LevelCard;
            _phaseMs = 0;
            (_listener as IWorldBannerListener)?.OnWorldBanner(world, level);
            return true;
        }

        private void ResetLevelCard()
        {
            _card = null;
            _cardRiverType = null;
            _cardBannerMs = 0;
            _worldBanner = 0;
            _bannerRevealGifted = false;
            _bannerThenCard = false;
            _bannerClosing = false;
        }
    }
}
