#nullable enable
using AF.GameFlow;
using AF.LiveOps;

namespace AF.UI
{
    /// <summary>
    /// D-062 (observación 16 de la jornada 6): «un aviso abre lo que nombra y se salta el takeover del inicio». El <c>type</c> del
    /// aviso con el que se abrió la app (<see cref="NotificationService.OnForeground"/>) queda pendiente y se encamina en la primera
    /// entrada al inicio sin nada encima (la misma en la que saldría el takeover: <see cref="TickLogbook"/>), así que el
    /// consentimiento, la edad, la intro de la marca y los modales legales van antes. El destino lo decide
    /// <see cref="NotificationRouting.Route"/> (sin motor):
    /// <list type="bullet">
    /// <item>el Reto: el tablero de hoy, como la tarjeta del inicio (<see cref="OpenDaily"/>);</item>
    /// <item>la Bitácora: la libreta (<see cref="LogbookOpenSource.Push"/>, sin gastar el cupo de takeovers);</item>
    /// <item>el inicio: esa entrada sin takeover.</item>
    /// </list>
    /// En los tres casos la cola de takeovers no se toca: el takeover saltado sale en la siguiente vuelta al inicio. El aviso se
    /// descarta (<c>dest</c> = <c>none</c>), sin sacar al jugador de lo que estaba haciendo:
    /// <list type="bullet">
    /// <item>al volver de segundo plano con la app en otra pantalla que el inicio sin nada encima (un nivel, el Reto, Ajustes, la
    /// Bitácora…);</item>
    /// <item>si, antes de tomarse el destino, la app pasa por un nivel o por el Reto (<see cref="TickPushRoute"/>): por ejemplo,
    /// el arranque en frío que reabre el nivel en curso (<c>level_state</c>, 02 §14) o la primera vez, que empieza en el nivel 1;</item>
    /// <item>si sale a segundo plano antes de llegar al inicio (<see cref="DropPushRoute"/>);</item>
    /// <item>con la reconstrucción por idioma (<c>RebuildUi</c>): la escena nueva no hereda el aviso pendiente, que se pierde sin
    /// sonda (solo puede pasar si se cambia de idioma antes de llegar al inicio, en la práctica nunca).</item>
    /// </list>
    /// La secuencia del Mapa del 10 va antes: el destino espera a que acabe. La sonda <c>AF_STATE push_open</c> sale al decidirlo,
    /// una vez por aviso, con <c>type</c> y <c>dest</c>.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        private string? _pushRouteType;

        /// <summary>Hay un aviso tocado que espera a la entrada al inicio para encaminarse.</summary>
        internal bool PushRoutePending => _pushRouteType != null;

        /// <summary>Último destino tomado por un aviso (<c>daily</c>, <c>logbook</c>, <c>home</c> o <c>none</c>), o null (tests y sonda).</summary>
        public string? LastPushDestination { get; private set; }

        // Al abrir: en frío siempre se espera al inicio (tras la intro y el consentimiento); en una vuelta, solo si la app ya está
        // en el inicio sin nada encima.
        private void NotePushOpened(string? openedType, bool coldStart)
        {
            _pushRouteType = null;
            if (openedType == null)
            {
                return;
            }

            if (coldStart || (LogbookLobbyClear && (_logbookScreen == null || !_logbookScreen.Visible)))
            {
                _pushRouteType = openedType;
                return;
            }

            ReportPushOpen(openedType, NotificationRouting.DroppedId);
        }

        // Primera entrada al inicio con un aviso pendiente (TickLogbook): abre su destino o, si no está disponible, deja el inicio
        // sin takeover. La cola no cambia: lo saltado sale en la siguiente vuelta.
        private void TakePushRoute()
        {
            string? type = _pushRouteType;
            _pushRouteType = null;
            if (type == null)
            {
                return;
            }

            _skipLobbyTakeover = false; // esta entrada ya la consume el aviso
            LastTakeover = TakeoverKind.None;
            int maxLevel = _progress?.MaxLevel ?? 0;
            DailyCardState daily = DailyCardState.Hidden;
            if (_dailyService != null && _dailyScreen != null && !_dailyScreen.IsOpen && NotificationTypes.TryParse(type, out NotificationType t)
                && t.Destination() == NotificationDestination.DailyChallenge)
            {
                _dailyCardShown = false;
                RefreshDailyCard(); // descarta la partida de otro día (R-Q4) antes de leer el estado, como «Jugar · Reto del río»
                daily = _dailyService.CardState(maxLevel);
            }

            bool logbookPending = _logbook != null && _logbook.IsAvailable(maxLevel) && LogbookPending();
            NotificationDestination dest = NotificationRouting.Route(type, daily, logbookPending);
            if (dest == NotificationDestination.DailyChallenge && !OpenDaily(daily))
            {
                dest = NotificationDestination.Home;
            }
            else if (dest == NotificationDestination.Logbook && !OpenLogbook(LogbookOpenSource.Push))
            {
                dest = NotificationDestination.Home;
            }

            ReportPushOpen(type, NotificationRouting.IdOf(dest));
        }

        // Cada fotograma (TickSessions): con un aviso pendiente, la app en un nivel o en el Reto lo descarta (el arranque en frío que
        // reabre el nivel en curso, o la primera vez). Sin la Bitácora montada (LogbookOverride false o _logbookFailed), TickLogbook
        // no corre: el aviso se encamina aquí, con la misma condición de entrada al inicio (la Bitácora, si era su destino, cae al
        // inicio).
        private void TickPushRoute()
        {
            if (_pushRouteType == null || _app == null)
            {
                return;
            }

            if (_app.Screen == AppScreen.Level || (_dailyScreen != null && _dailyScreen.IsOpen))
            {
                DropPushRoute();
                return;
            }

            if ((_logbook == null || _logbookScreen == null) && LogbookLobbyClear && !_app.L10MapDue)
            {
                TakePushRoute();
            }
        }

        // Sale a segundo plano con el aviso aún sin encaminar: se descarta.
        private void DropPushRoute()
        {
            string? type = _pushRouteType;
            _pushRouteType = null;
            if (type != null)
            {
                ReportPushOpen(type, NotificationRouting.DroppedId);
            }
        }

        private void ReportPushOpen(string type, string dest)
        {
            LastPushDestination = dest;
            if (DevProbe.Enabled)
            {
                DevProbe.Write(SessionProbeFormat.PushOpen(SessionProbeMs(), type, dest));
            }
        }
    }
}
