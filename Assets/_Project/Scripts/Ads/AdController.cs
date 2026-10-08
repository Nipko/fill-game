#nullable enable
using System;
using AF.Platform;
using AF.Save;

namespace AF.Ads
{
    /// <summary>Resultado de <see cref="AdController.ShowInterstitial"/> y <see cref="AdController.ShowRewarded"/>.</summary>
    public sealed class AdResult
    {
        internal AdResult(AdPlacement placement, AdOutcome outcome, AdBlockReason reason)
        {
            Placement = placement;
            Outcome = outcome;
            Reason = reason;
        }

        /// <summary>Ubicación.</summary>
        public AdPlacement Placement { get; }

        /// <summary>Cómo terminó.</summary>
        public AdOutcome Outcome { get; }

        /// <summary>Por qué no se mostró (con <see cref="AdOutcome.Blocked"/>; <see cref="AdBlockReason.None"/> en el resto).</summary>
        public AdBlockReason Reason { get; }

        /// <summary>Se ganó la recompensa: quien llamó la aplica (Marea +3, +1 canoa, Cosecha doble…) con <c>Wallet.Apply</c>.</summary>
        public bool IsRewarded => Outcome == AdOutcome.Rewarded;
    }

    /// <summary>
    /// Punto de entrada de los anuncios para el juego (GDD 08 §2.2 <c>AF.Ads</c>): junta las reglas
    /// (<see cref="AdPolicy"/>), el proveedor (<see cref="IAdService"/>) y lo que se anota (<see cref="AdLedger"/>,
    /// <see cref="AdSession"/>).
    /// <list type="bullet">
    /// <item>Victoria: <see cref="RecordLevelCompleted"/>; al tocar «Siguiente» (AD_GATE),
    /// <see cref="ShowInterstitial"/>, que nunca bloquea el avance: si las reglas no lo permiten o no hay anuncio, llama
    /// a <c>onDone</c> sin mostrar nada.</item>
    /// <item>Botones de video: <see cref="CheckRewarded"/> decide si se enseñan (sin fill, se ocultan) y
    /// <see cref="ShowRewarded"/> los muestra. La recompensa la aplica quien llama; aquí solo se anotan el tope diario, el
    /// global, <c>stats.rv_watched</c> y el cooldown.</item>
    /// </list>
    /// El resto del juego no pide anuncios por otra vía. Un solo hilo.
    /// </summary>
    public sealed class AdController
    {
        private readonly IAdService _service;
        private readonly IClock _clock;
        private bool _showing;

        /// <summary>Crea el controlador con sus piezas.</summary>
        public AdController(IAdService service, AdPolicy policy, AdLedger ledger, AdSession session, IClock clock)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            Ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            Session = session ?? throw new ArgumentNullException(nameof(session));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Reglas.</summary>
        public AdPolicy Policy { get; }

        /// <summary>Lo guardado en el perfil.</summary>
        public AdLedger Ledger { get; }

        /// <summary>La sesión en curso.</summary>
        public AdSession Session { get; }

        /// <summary>Hay un anuncio en pantalla.</summary>
        public bool IsShowing => _showing || _service.IsShowing;

        /// <summary>
        /// Crea el controlador sobre el perfil del jugador (el mismo que usa la cartera) con una sesión que empieza ahora.
        /// </summary>
        /// <param name="service">Proveedor (en la fase 2, <see cref="FakeAdService"/>).</param>
        /// <param name="profile">Perfil.</param>
        /// <param name="store">Almacén del perfil.</param>
        /// <param name="clock">Reloj.</param>
        /// <param name="isInstallSession">El perfil se creó en este arranque (sin intersticiales en esta sesión).</param>
        /// <param name="config">Cifras; por defecto, las de 08 §9.</param>
        public static AdController Create(
            IAdService service, Profile profile, IProfileStore store, IClock clock, bool isInstallSession, AdsConfig? config = null)
        {
            var ledger = new AdLedger(profile, store, clock);
            AdSession session = AdSession.Start(clock, isInstallSession);
            return new AdController(service, new AdPolicy(config ?? AdsConfig.Defaults, ledger, session, clock), ledger, session, clock);
        }

        /// <summary>Se ganó un nivel de la campaña: cuenta para la cadencia de intersticiales.</summary>
        public void RecordLevelCompleted()
        {
            Ledger.RecordLevelCompleted();
        }

        /// <summary>¿Se mostraría ahora el intersticial tras la victoria de <paramref name="level"/>? (reglas, pantalla libre y fill).</summary>
        public AdDecision CheckInterstitial(int level, AdContext context)
        {
            AdDecision decision = Policy.CheckInterstitial(level, context);
            return Available(decision, AdPlacement.InterstitialPostLevel);
        }

        /// <summary>¿Se enseña ahora el botón de video de <paramref name="placement"/>? (reglas, pantalla libre y fill).</summary>
        public AdDecision CheckRewarded(AdPlacement placement)
        {
            AdDecision decision = Policy.CheckRewarded(placement);
            return Available(decision, placement);
        }

        /// <summary>
        /// AD_GATE: muestra el intersticial tras la victoria de <paramref name="level"/> si las reglas lo permiten y llama a
        /// <paramref name="onDone"/> siempre (también sin anuncio), para pasar al nivel siguiente.
        /// </summary>
        public void ShowInterstitial(int level, AdContext context, Action<AdResult> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            const AdPlacement placement = AdPlacement.InterstitialPostLevel;
            AdDecision decision = CheckInterstitial(level, context);
            if (!decision.IsAllowed)
            {
                onDone(new AdResult(placement, AdOutcome.Blocked, decision.Reason));
                return;
            }

            _showing = true;
            _service.Show(placement, outcome =>
            {
                _showing = false;
                if (outcome == AdOutcome.Shown)
                {
                    Ledger.RecordInterstitialShown();
                    Session.RecordInterstitial(_clock.UtcNow);
                }

                onDone(new AdResult(placement, outcome, AdBlockReason.None));
            });
        }

        /// <summary>Muestra el recompensado de <paramref name="placement"/> y llama a <paramref name="onDone"/> al terminar.</summary>
        /// <exception cref="ArgumentException"><paramref name="placement"/> no es de recompensado.</exception>
        public void ShowRewarded(AdPlacement placement, Action<AdResult> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            AdDecision decision = CheckRewarded(placement);
            if (!decision.IsAllowed)
            {
                onDone(new AdResult(placement, AdOutcome.Blocked, decision.Reason));
                return;
            }

            _showing = true;
            _service.Show(placement, outcome =>
            {
                _showing = false;
                if (outcome == AdOutcome.Rewarded)
                {
                    Ledger.RecordRewarded(placement);
                }

                if (outcome == AdOutcome.Rewarded || outcome == AdOutcome.Skipped)
                {
                    Session.RecordRewarded(placement, _clock.UtcNow);
                }

                onDone(new AdResult(placement, outcome, AdBlockReason.None));
            });
        }

        private AdDecision Available(AdDecision decision, AdPlacement placement)
        {
            if (!decision.IsAllowed)
            {
                return decision;
            }

            if (IsShowing)
            {
                return new AdDecision(AdBlockReason.Busy);
            }

            return _service.IsReady(placement) ? decision : new AdDecision(AdBlockReason.NoFill);
        }
    }
}
