#nullable enable
using AF.Boosters;

namespace AF.GameFlow
{
    /// <summary>
    /// Avisos opcionales de <see cref="LevelFlow"/> para la Pororoca · Barajar (bloque B2; 04 §2.4). El flujo los consulta con
    /// <c>listener as IShuffleListener</c>, así que <see cref="ILevelFlowListener"/> no cambia. Se llaman de forma síncrona, con
    /// el modelo ya actualizado. El flujo que los produce (<c>LevelFlow.Shuffle.cs</c>) llega con el parche B2-flow, porque
    /// usa el barajado de <c>AF.Solver</c> (parche B2-solver).
    /// </summary>
    public interface IShuffleListener
    {
        /// <summary>
        /// La ola barajó el tablero (<paramref name="outcome"/> con su tablero, ya aplicado, guardado y cobrado con
        /// <paramref name="payment"/>): la vista anima la ola de 1,2 s, no saltable (04 §2.4 VFX). El historial de Marea ya está
        /// vacío (02 §14) y las capas ocultas, reveladas.
        /// </summary>
        void OnShuffled(ShuffleOutcome outcome, ShufflePayment payment);

        /// <summary>
        /// Ningún tablero se verificó: «La ola no pudo remover el río» y no se cobró nada (04 §2.4). El tablero no cambió.
        /// </summary>
        void OnShuffleFailed(ShuffleOutcome outcome);
    }
}
