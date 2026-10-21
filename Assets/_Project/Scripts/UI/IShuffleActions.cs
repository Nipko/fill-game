#nullable enable

namespace AF.UI
{
    /// <summary>
    /// Punto de entrada de la Pororoca · Barajar (bloque B2; 04 §2.4), aparte de <see cref="IGameActions"/> para no tocar esa
    /// interfaz (la implementan <c>GameBootstrap</c> y el Reto del río, en <c>GameBootstrap.Shuffle.cs</c> y en
    /// <c>DailyChallengeScreen</c>, con el parche B2-ui). La pantalla la consulta con <c>actions as IShuffleActions</c>; sin
    /// ella, el botón no hace nada.
    /// </summary>
    public interface IShuffleActions
    {
        /// <summary>
        /// La ola, ya confirmada si hacía falta (la hoja de ≥ 200 semillas la abre la pantalla antes): termina el revelado en
        /// marcha y llama a <c>LevelFlow.UseShuffle</c>. False si no se pudo intentar (sin nivel o con el botón no listo).
        /// </summary>
        bool PressShuffle();
    }
}
