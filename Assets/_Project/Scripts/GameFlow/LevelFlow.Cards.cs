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
    public sealed partial class LevelFlow
    {
        private LevelCardInfo? _card;
        private int _cardIntroMs;
        private bool _cardIntroShort;
        private string? _cardRiverType;
        private int _cardBannerMs;

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
        /// Cierra la tarjeta previa (1 toque en cualquier punto) y arranca la intro que esperaba (con el cartel «El río bajó»
        /// antes, si el intento sirve la variante aliviada). False fuera de LEVEL_CARD. <see cref="TapAnywhere"/>,
        /// <see cref="TapCuia"/> y <see cref="TapBackground"/> también la cierran.
        /// </summary>
        public bool DismissLevelCard()
        {
            if (Phase != FlowPhase.LevelCard)
            {
                return false;
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
            LevelCardInfo? card = LevelCardInfo.For(_level, _levelNumber, economy, _session.Initial.HasHidden, revealGifted);
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

        private void ResetLevelCard()
        {
            _card = null;
            _cardRiverType = null;
            _cardBannerMs = 0;
        }
    }
}
