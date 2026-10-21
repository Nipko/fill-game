#nullable enable
using AF.Boosters;
using AF.GameFlow;

namespace AF.UI
{
    /// <summary>
    /// Pororoca · Barajar desde la bandeja de la campaña (bloque B2; 04 §2.4): <see cref="IShuffleActions"/> sin tocar
    /// <c>GameBootstrap.cs</c> ni <see cref="IGameActions"/>. La hoja de confirmación ya la pidió la pantalla
    /// (<c>GameScreen.Shuffle.cs</c>). El cobro, los topes y el guardado son de <c>LevelFlow.UseShuffle</c>, en su propio lote de
    /// guardado; aquí se termina antes el revelado de la Luciérnaga en marcha, como en Marea y la Cuia extra. Las cifras son las
    /// de 08 §9 (<see cref="ShuffleConfig.Defaults"/>): aún no hay <c>AF.RemoteConfig</c>.
    /// </summary>
    public sealed partial class GameBootstrap : IShuffleActions
    {
        /// <inheritdoc />
        public bool PressShuffle()
        {
            LevelFlow? flow = InLevel ? _flow : null;
            if (flow == null || !flow.HasLevel || !flow.Shuffle.CanUse)
            {
                return false;
            }

            _screen?.SettleReveal();
            ShuffleOutcome? outcome = flow.UseShuffle();
            return outcome.HasValue;
        }
    }
}
