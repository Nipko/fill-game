#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Aviso opcional de <see cref="LevelFlow"/> para la tarjeta previa de 1 toque (02 §10; propuesta de D-009 §c.1). El flujo
    /// lo consulta con <c>listener as ILevelCardListener</c>, así que <see cref="ILevelFlowListener"/> no cambia y una vista
    /// que no lo implementa no se entera (por eso las tarjetas van apagadas, <see cref="LevelFlow.LevelCardsEnabled"/>, hasta
    /// que exista la suya).
    /// </summary>
    public interface ILevelCardListener
    {
        /// <summary>
        /// LEVEL_CARD: llega tras <see cref="ILevelFlowListener.OnLevelLoaded"/> y los regalos del nivel (Cuia extra del 8,
        /// Luciérnaga del 100), en lugar de <see cref="ILevelFlowListener.OnIntroStarted"/>, que llega al cerrar la tarjeta
        /// (<see cref="LevelFlow.DismissLevelCard"/>).
        /// </summary>
        void OnLevelCard(LevelCardInfo card);
    }
}
