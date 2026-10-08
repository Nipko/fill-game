#nullable enable
using System;

namespace AF.Ads
{
    /// <summary>Qué hace <see cref="FakeAdService"/> al pedirle un anuncio (para pruebas y builds de desarrollo).</summary>
    public enum FakeAdFill
    {
        /// <summary><c>fill</c>: hay anuncio y se muestra la pantalla «Anuncio simulado».</summary>
        Fill = 0,

        /// <summary><c>no_fill</c>: no hay anuncio (el botón de video se oculta).</summary>
        NoFill = 1,

        /// <summary><c>error</c>: hay anuncio cargado pero falla al mostrarlo.</summary>
        Error = 2,
    }

    /// <summary>Cómo se cerró la pantalla «Anuncio simulado».</summary>
    public enum SimulatedAdClose
    {
        /// <summary>Tras su tiempo: el intersticial se vio y el recompensado da su premio.</summary>
        Completed = 0,

        /// <summary>Antes de tiempo (solo tiene efecto en un recompensado: sin premio).</summary>
        ClosedEarly = 1,
    }

    /// <summary>Lo que la UI enseña en la pantalla «Anuncio simulado».</summary>
    public sealed class SimulatedAd
    {
        /// <summary>Rótulo de la pantalla.</summary>
        public const string Label = "Anuncio simulado";

        internal SimulatedAd(AdPlacement placement, TimeSpan closeEnabledAfter, TimeSpan? rewardAfter)
        {
            Placement = placement;
            CloseEnabledAfter = closeEnabledAfter;
            RewardAfter = rewardAfter;
        }

        /// <summary>Ubicación (la UI muestra su ID, p. ej. <c>rv_extra_canoe</c>).</summary>
        public AdPlacement Placement { get; }

        /// <summary>Formato.</summary>
        public AdFormat Format => Placement.Format();

        /// <summary>Cuándo se habilita «Cerrar» (cuenta atrás visible).</summary>
        public TimeSpan CloseEnabledAfter { get; }

        /// <summary>Cuándo se gana el premio (solo recompensados; null en el intersticial).</summary>
        public TimeSpan? RewardAfter { get; }
    }

    /// <summary>
    /// La pantalla «Anuncio simulado» que pinta la UI (B2-4, <c>AdOverlay</c>): rótulo, ubicación y cuenta atrás. Llama a
    /// <c>onClosed</c> una sola vez, al cerrarse.
    /// </summary>
    public interface ISimulatedAdPresenter
    {
        /// <summary>Muestra <paramref name="ad"/> y espera a que se cierre.</summary>
        void Present(SimulatedAd ad, Action<SimulatedAdClose> onClosed);
    }

    /// <summary>Presentador sin pantalla para tests y para el arranque sin UI: cierra al instante con el resultado fijado.</summary>
    public sealed class ImmediateAdPresenter : ISimulatedAdPresenter
    {
        /// <summary>Crea el presentador.</summary>
        public ImmediateAdPresenter(SimulatedAdClose close = SimulatedAdClose.Completed)
        {
            Close = close;
        }

        /// <summary>Cómo se cierran los anuncios.</summary>
        public SimulatedAdClose Close { get; set; }

        /// <summary>Anuncios presentados.</summary>
        public int Presented { get; private set; }

        /// <summary>El último presentado, o null.</summary>
        public SimulatedAd? Last { get; private set; }

        /// <inheritdoc />
        public void Present(SimulatedAd ad, Action<SimulatedAdClose> onClosed)
        {
            Presented++;
            Last = ad;
            onClosed(Close);
        }
    }

    /// <summary>
    /// Anuncios simulados de la fase 2 (D-008, aprobado por el dueño el 2026-10-08): sin SDK, sin red y sin
    /// identificadores; ningún dato sale del dispositivo. Pide a la UI la pantalla «Anuncio simulado»
    /// (<see cref="ISimulatedAdPresenter"/>) y espera su cierre: el intersticial habilita «Cerrar» a los 5 s (05 §6.1) y el
    /// recompensado da el premio a los 3 s [P], cuando también se puede cerrar. <see cref="Fill"/> simula
    /// <c>fill</c>, <c>no_fill</c> y error.
    /// </summary>
    public sealed class FakeAdService : IAdService
    {
        /// <summary>«Cerrar» del intersticial a los 5 s (05 §6.1: cierre habilitado a los 5 s).</summary>
        public static readonly TimeSpan InterstitialCloseAfter = TimeSpan.FromSeconds(5);

        /// <summary>Duración del recompensado simulado [P] (plan B1-3).</summary>
        public static readonly TimeSpan RewardedDuration = TimeSpan.FromSeconds(3);

        private readonly ISimulatedAdPresenter _presenter;

        /// <summary>Crea el servicio con la pantalla <paramref name="presenter"/>.</summary>
        public FakeAdService(ISimulatedAdPresenter presenter, FakeAdFill fill = FakeAdFill.Fill)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            Fill = fill;
        }

        /// <summary>Qué pasa al pedir un anuncio.</summary>
        public FakeAdFill Fill { get; set; }

        /// <inheritdoc />
        public bool IsShowing { get; private set; }

        /// <inheritdoc />
        public bool IsReady(AdPlacement placement)
        {
            placement.ToId(); // valida la ubicación
            return Fill != FakeAdFill.NoFill;
        }

        /// <inheritdoc />
        public void Show(AdPlacement placement, Action<AdOutcome> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            bool rewarded = placement.IsRewarded();
            if (IsShowing || Fill == FakeAdFill.Error)
            {
                onDone(AdOutcome.Failed);
                return;
            }

            if (Fill == FakeAdFill.NoFill)
            {
                onDone(AdOutcome.NoFill);
                return;
            }

            var ad = rewarded
                ? new SimulatedAd(placement, RewardedDuration, RewardedDuration)
                : new SimulatedAd(placement, InterstitialCloseAfter, null);
            bool closed = false;
            IsShowing = true;
            _presenter.Present(ad, close =>
            {
                if (closed)
                {
                    return;
                }

                closed = true;
                IsShowing = false;
                onDone(!rewarded ? AdOutcome.Shown : close == SimulatedAdClose.Completed ? AdOutcome.Rewarded : AdOutcome.Skipped);
            });
        }
    }
}
