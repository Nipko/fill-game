#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Aviso opcional de <see cref="LevelFlow"/> para el banner de entrada a un mundo (07 §2.2, regla 1; 02 §6;
    /// <see cref="WorldBanners"/>). Como <see cref="ILevelCardListener"/>, el flujo lo consulta con
    /// <c>listener as IWorldBannerListener</c>: <see cref="ILevelFlowListener"/> no cambia, y una vista que no lo implementa no
    /// se entera (por eso el banner va apagado, <see cref="LevelFlow.WorldBannersEnabled"/>, salvo en el juego).
    /// </summary>
    public interface IWorldBannerListener
    {
        /// <summary>
        /// El banner del mundo <paramref name="world"/> se abre al cargar <paramref name="level"/>: llega tras
        /// <see cref="ILevelFlowListener.OnLevelLoaded"/> y los regalos del nivel, en LEVEL_CARD y antes de la tarjeta previa (si
        /// el nivel la tiene, <see cref="ILevelCardListener.OnLevelCard"/> llega al cerrar el banner) y de
        /// <see cref="ILevelFlowListener.OnIntroStarted"/>. Se cierra con <see cref="LevelFlow.DismissWorldBanner"/> (1 toque).
        /// </summary>
        void OnWorldBanner(int world, int level);
    }
}
