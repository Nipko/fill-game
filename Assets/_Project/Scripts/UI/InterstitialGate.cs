#nullable enable
using System;
using AF.Ads;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;

namespace AF.UI
{
    /// <summary>
    /// AD_GATE del juego (02 §2; 05 §6.1 y §6.3; D-008 punto 4; tarea B2-4) sobre <see cref="AdController"/>, con los anuncios
    /// simulados de la fase 2 (sin SDK ni red): cada victoria cuenta para la cadencia
    /// (<see cref="AdController.RecordLevelCompleted"/>) y, tras «Siguiente», el intersticial se pide con el contexto
    /// <see cref="AdContext.VictoryScreen"/>, el único que lo admite. Las reglas (nivel ≥ 22, nunca en la sesión de
    /// instalación, cada 3 en 22-60 y cada 2 desde el 61, ≥ 150 s, calentamiento de 120 s, topes de 6 y 12, 24 h tras una
    /// compra y 60 s tras un recompensado, Sin anuncios) son de <see cref="AdPolicy"/>; las Canoas infinitas no cuentan.
    /// Escribe la decisión en la sonda (<c>ad_check</c>).
    /// </summary>
    internal sealed class InterstitialGate : IAdGate
    {
        private readonly AdController _ads;
        private readonly DevProbe _probe;
        private readonly Wallet _wallet;

        public InterstitialGate(AdController ads, DevProbe probe, Wallet wallet)
        {
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            _probe = probe ?? throw new ArgumentNullException(nameof(probe));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        /// <inheritdoc />
        public void RecordWin(int level)
        {
            _ads.RecordLevelCompleted();
        }

        /// <inheritdoc />
        public void Run(int wonLevel, Action onDone)
        {
            _probe.AdCheck(wonLevel, _ads.CheckInterstitial(wonLevel, AdContext.VictoryScreen), _wallet);
            _ads.ShowInterstitial(wonLevel, AdContext.VictoryScreen, _ => onDone());
        }
    }

    /// <summary>
    /// Envuelve la pantalla «Anuncio simulado» (la del juego, <see cref="AdOverlay"/>, o la inmediata de los tests) para que
    /// cada anuncio escriba en la sonda su tipo, su ubicación y cómo terminó (<c>ad</c> y <c>ad_closed</c>; tarea B2-4), con el
    /// nivel y el saldo. No cambia nada de lo que muestra ni de cuándo se cierra.
    /// </summary>
    internal sealed class ProbedAdPresenter : ISimulatedAdPresenter
    {
        private readonly ISimulatedAdPresenter _inner;
        private readonly DevProbe _probe;
        private readonly Func<int> _level;
        private readonly Func<Wallet?> _wallet;
        private readonly ProfileSaveBatch? _saves;

        /// <param name="saves">
        /// Lote de guardado: lo que se anota al cerrar el anuncio (registro de <c>AF.Ads</c>, recompensa y, tras AD_GATE, la
        /// carga del nivel siguiente) va en una sola escritura del perfil. Null = sin lote.
        /// </param>
        public ProbedAdPresenter(ISimulatedAdPresenter inner, DevProbe probe, Func<int> level, Func<Wallet?> wallet, ProfileSaveBatch? saves = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _probe = probe ?? throw new ArgumentNullException(nameof(probe));
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _saves = saves;
        }

        /// <summary>La pantalla envuelta.</summary>
        public ISimulatedAdPresenter Inner => _inner;

        /// <inheritdoc />
        public void Present(SimulatedAd ad, Action<SimulatedAdClose> onClosed)
        {
            _probe.Ad("ad", ad, "shown", _level(), _wallet());
            _inner.Present(ad, close =>
            {
                _probe.Ad("ad_closed", ad, close == SimulatedAdClose.Completed ? "completed" : "closed_early", _level(), _wallet());
                using (_saves == null ? default : _saves.Begin())
                {
                    onClosed(close);
                }
            });
        }
    }

    /// <summary>
    /// Envuelve la hoja «Compra simulada» (la del juego, <see cref="StoreSheet"/>, o la inmediata de los tests) para que lo que
    /// pasa al cerrarla (acreditar con <c>StoreLedger.Fulfill</c> y, con la Balsa de rescate, aplicar la Cuia de rescate en
    /// el nivel; o anotar el rechazo) vaya en una sola escritura del perfil: un cierre entre la compra y <c>level_state</c> no
    /// deja la Balsa cobrada con el modal abierto. No cambia nada de lo que muestra ni de cuándo se cierra.
    /// </summary>
    internal sealed class BatchedPurchasePresenter : ISimulatedPurchasePresenter
    {
        private readonly ISimulatedPurchasePresenter _inner;
        private readonly ProfileSaveBatch _saves;

        public BatchedPurchasePresenter(ISimulatedPurchasePresenter inner, ProfileSaveBatch saves)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _saves = saves ?? throw new ArgumentNullException(nameof(saves));
        }

        /// <summary>La hoja envuelta.</summary>
        public ISimulatedPurchasePresenter Inner => _inner;

        /// <inheritdoc />
        public void Present(SimulatedPurchase purchase, Action<bool> onClosed)
        {
            _inner.Present(purchase, confirmed =>
            {
                using (_saves.Begin())
                {
                    onClosed(confirmed);
                }
            });
        }
    }
}
