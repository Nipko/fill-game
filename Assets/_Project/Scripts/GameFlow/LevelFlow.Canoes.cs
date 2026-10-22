#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Tarjeta de las Canoas antes del nivel 31 (tarea B11; 02 §13: «31 Canoas + Cuia de rescate (tarjeta de 1 pantalla antes del
    /// nivel)»; 04 §7.1, «Debut»: «Nivel 31, con 5/5 y tarjeta»), con el texto que decidió el dueño (D-033, Q-E4 (a): «Canoas: si te
    /// rindes o sales del nivel, pierdes una. Vuelven solas cada 25 minutos.» [P]; el de 04 §7.1 quedó obsoleto con D-008). Es la
    /// tarjeta previa de 1 toque de un estreno sin etiqueta, como la de la Crecida del 25 (<c>LevelFlow.Streak.cs</c>), con el ID del
    /// sistema (<see cref="LevelCardInfo.CanoesIntro"/>, <c>lives</c>, glosario §6.3) en lugar de un <c>intro_mechanic</c> del
    /// paquete (<see cref="LevelCardInfo.ForCanoes"/>, junto a <see cref="LevelCardInfo.ForStreak"/>).
    /// <para>Sale en el primer nivel con canoas (<c>rescue_min_level</c>, 31: <c>Wallet.CanoesActive</c>), solo en la
    /// campaña y solo si las canoas cuentan: con <c>biz_feature_lives</c> apagado (D-038) no hay canoas en ninguna parte y no hay
    /// tarjeta. No sale al reintentar (intro corta) ni al reanudar, como las demás tarjetas; si el nivel ya tiene tarjeta (una
    /// etiqueta o un estreno), esa gana.</para>
    /// </summary>
    public sealed partial class LevelFlow
    {
        // La tarjeta de las Canoas del nivel que se carga, o null (ver la clase). La pide TryShowLevelCard tras la del nivel y la
        // de la Crecida (parche B11-flow).
        private LevelCardInfo? CanoesCardFor(bool isShort)
        {
            if (isShort || IsDaily || _wallet == null || _levelNumber != RescueMinLevel || !_wallet.CanoesActive(_levelNumber))
            {
                return null;
            }

            return LevelCardInfo.ForCanoes(_levelNumber);
        }
    }
}
