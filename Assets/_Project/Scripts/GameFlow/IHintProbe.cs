#nullable enable
using AF.Boosters;

namespace AF.GameFlow
{
    /// <summary>
    /// Sonda de desarrollo de Morpho (<c>AF_PERF morpho</c>; MO1): la implementa <c>AF.UI.DevProbe</c> y la recibe
    /// <see cref="LevelFlow.HintProbe"/>. Una llamada por respuesta entregada al jugador, en el hilo principal.
    /// </summary>
    public interface IHintProbe
    {
        /// <summary>
        /// Morpho respondió <paramref name="result"/> en el nivel <paramref name="level"/>, <paramref name="waitMs"/> ms (reloj del
        /// flujo) después de pedirla, con el corte <paramref name="timeoutMs"/> (<c>hint_timeout_ms</c>).
        /// </summary>
        void Morpho(in HintResult result, int level, int waitMs, int timeoutMs);
    }
}
