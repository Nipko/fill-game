#nullable enable

namespace AF.LiveOps
{
    /// <summary>Lo que abre un aviso al tocarlo (D-062; 06 §8.3, columna «Abre»).</summary>
    public enum NotificationDestination
    {
        /// <summary>El inicio sin el takeover de esa entrada (el takeover queda para la siguiente vuelta al inicio).</summary>
        Home = 0,

        /// <summary>El Reto del río: el tablero de hoy (Jugar o Seguir), como la tarjeta del inicio.</summary>
        DailyChallenge = 1,

        /// <summary>La libreta de la Bitácora con la casilla de hoy por sellar.</summary>
        Logbook = 2,
    }

    /// <summary>
    /// Encaminamiento de un aviso tocado (D-062: «un aviso abre lo que nombra y se salta el takeover del inicio»), sin motor y sin
    /// asignaciones. La tabla por tipo es <see cref="Destination"/>; <see cref="Route"/> aplica las caídas con el estado real del
    /// jugador al llegar al inicio (tras el consentimiento, la edad, la intro y cualquier modal legal, que van antes):
    /// <list type="bullet">
    /// <item><c>daily_ready</c> y <c>daily_streak</c> abren el Reto si hoy se puede jugar o seguir (tarjeta Jugar o Seguir); si
    /// está bloqueado por nivel, ya jugado hoy (sellado o pendiente de validar) o sin pool, caen al inicio.</item>
    /// <item><c>logbook_pending</c> abre la Bitácora si la casilla de hoy se puede sellar; si no, el inicio.</item>
    /// <item><c>streak_waiting</c>, <c>canoes_full</c>, <c>new_levels</c> y los Regreso (<c>comeback_d*</c>; su regalo ya se
    /// acreditó al empezar la sesión, 06 §9) llevan al inicio, sin entrar solos en un nivel [P].</item>
    /// <item>Un <c>type</c> que no es del catálogo, al inicio.</item>
    /// </list>
    /// </summary>
    public static class NotificationRouting
    {
        /// <summary>Valor de <c>dest</c> en la sonda <c>push_open</c> cuando el aviso no llegó a encaminarse (la app no estaba en el inicio, o se fue antes de llegar) [P].</summary>
        public const string DroppedId = "none";

        /// <summary>El destino de <paramref name="type"/> según la tabla de D-062, antes de las caídas.</summary>
        public static NotificationDestination Destination(this NotificationType type)
        {
            switch (type)
            {
                case NotificationType.DailyReady:
                case NotificationType.DailyStreak:
                    return NotificationDestination.DailyChallenge;
                case NotificationType.LogbookPending:
                    return NotificationDestination.Logbook;
                default:
                    return NotificationDestination.Home;
            }
        }

        /// <summary>
        /// El destino que se toma al llegar al inicio con el aviso <paramref name="openedType"/> (el <c>type</c> de
        /// <c>af_push_open</c>), con el estado del Reto (<paramref name="daily"/>, tras descartar la partida de otro día) y de la
        /// Bitácora (<paramref name="logbookPending"/>: la casilla de hoy se puede sellar).
        /// </summary>
        public static NotificationDestination Route(string? openedType, DailyCardState daily, bool logbookPending)
        {
            if (!NotificationTypes.TryParse(openedType, out NotificationType type))
            {
                return NotificationDestination.Home;
            }

            switch (type.Destination())
            {
                case NotificationDestination.DailyChallenge:
                    return daily == DailyCardState.Play || daily == DailyCardState.InProgress
                        ? NotificationDestination.DailyChallenge
                        : NotificationDestination.Home;
                case NotificationDestination.Logbook:
                    return logbookPending ? NotificationDestination.Logbook : NotificationDestination.Home;
                default:
                    return NotificationDestination.Home;
            }
        }

        /// <summary>Valor de <c>dest</c> en la sonda <c>push_open</c>: <c>home</c>, <c>daily</c> o <c>logbook</c> [P].</summary>
        public static string IdOf(NotificationDestination destination)
        {
            switch (destination)
            {
                case NotificationDestination.DailyChallenge: return "daily";
                case NotificationDestination.Logbook: return "logbook";
                default: return "home";
            }
        }
    }
}
