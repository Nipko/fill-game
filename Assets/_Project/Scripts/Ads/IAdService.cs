#nullable enable
using System;

namespace AF.Ads
{
    /// <summary>Cómo terminó un anuncio.</summary>
    public enum AdOutcome
    {
        /// <summary>Intersticial visto y cerrado.</summary>
        Shown = 0,

        /// <summary>Recompensado visto hasta el final: se concede la recompensa.</summary>
        Rewarded = 1,

        /// <summary>Recompensado cerrado antes del final: sin recompensa.</summary>
        Skipped = 2,

        /// <summary>No había anuncio (<c>no_fill</c>).</summary>
        NoFill = 3,

        /// <summary>Error al mostrarlo (<c>error</c>).</summary>
        Failed = 4,

        /// <summary>Las reglas no lo permiten (<see cref="AdController"/>; ver <see cref="AdResult.Reason"/>).</summary>
        Blocked = 5,
    }

    /// <summary>
    /// Proveedor de anuncios (GDD 08 §2.1 principio 6 y §2.2 <c>AF.Ads</c>). En la fase 2 (D-008) solo existe
    /// <see cref="FakeAdService"/>, sin SDK ni red; AppLovin MAX entrará después detrás de esta misma interfaz, con la
    /// inicialización tras el consentimiento y la edad (08 §2.3), la precarga y la concesión con SSV (08 §10.2). Las reglas
    /// de cadencia y topes no son suyas: son de <see cref="AdPolicy"/>. Se usa desde el hilo principal.
    /// </summary>
    public interface IAdService
    {
        /// <summary>Hay un anuncio en pantalla.</summary>
        bool IsShowing { get; }

        /// <summary>Hay un anuncio cargado para <paramref name="placement"/>; sin él, el botón de video se oculta (05 §6.2).</summary>
        bool IsReady(AdPlacement placement);

        /// <summary>
        /// Muestra el anuncio de <paramref name="placement"/> y llama una sola vez a <paramref name="onDone"/> al terminar:
        /// <see cref="AdOutcome.Shown"/> (intersticial), <see cref="AdOutcome.Rewarded"/> o <see cref="AdOutcome.Skipped"/>
        /// (recompensado), <see cref="AdOutcome.NoFill"/> o <see cref="AdOutcome.Failed"/>. Puede llamarla antes de volver.
        /// </summary>
        void Show(AdPlacement placement, Action<AdOutcome> onDone);
    }
}
