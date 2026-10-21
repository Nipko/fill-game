#nullable enable
using AF.GameFlow;
using AF.LiveOps;

namespace AF.UI
{
    /// <summary>
    /// Sondas de la Bitácora del río (B4; 08 §8.2; D-033, añadido del 2026-10-20). Hasta que exista <c>AF.Analytics</c> (B7), los
    /// eventos van al log de los builds de desarrollo (<see cref="Enabled"/>) como <c>AF_STATE</c>, con el formato de
    /// <see cref="LoginCalendarProbeFormat"/> (AF.LiveOps, probado allí) y el canal de siempre (<see cref="Write"/>):
    /// <list type="bullet">
    /// <item><c>calendar_claim</c> (<c>af_calendar_claim</c>) al sellar (<see cref="LogbookClaim"/>);</item>
    /// <item><c>calendar_state</c> [P] al arrancar, al apuntar el primer nivel del día, al cambiar el día y al sellar
    /// (<see cref="LogbookState"/>);</item>
    /// <item><c>takeover</c> [P] en cada entrada al inicio con la Bitácora en el juego (<see cref="LogbookTakeover"/>).</item>
    /// </list>
    /// Solo en el hilo principal (un único <see cref="LoginCalendarProbeFormat"/>). Solo claves e identificadores, nunca texto
    /// visible. Cada una devuelve la línea escrita, o null si no escribió nada.
    /// </summary>
    public sealed partial class DevProbe
    {
        private static readonly LoginCalendarProbeFormat s_logbook = new LoginCalendarProbeFormat();

        /// <summary><c>AF_STATE calendar_claim</c> tras sellar una casilla.</summary>
        public static string? LogbookClaim(LoginCalendarClaim claim)
        {
            return Enabled ? WriteDaily(s_logbook.Claim(NowMs(), claim)) : null;
        }

        /// <summary><c>AF_STATE calendar_state</c> [P] con la causa <paramref name="cause"/> (<see cref="LoginCalendarProbeFormat"/>).</summary>
        public static string? LogbookState(string cause, LoginCalendarState state, LoginCalendarRecord record)
        {
            return Enabled ? WriteDaily(s_logbook.State(NowMs(), cause, state, record.Cycle, record.NextDay, record.LastClaimDate, record.PlayedDate)) : null;
        }

        /// <summary><c>AF_STATE takeover</c> [P]: lo que salió (o <c>none</c>) al entrar al inicio, con la sesión y el cupo.</summary>
        public static string? LogbookTakeover(TakeoverKind kind, bool shown, TakeoverQueue queue, string source)
        {
            return Enabled ? WriteDaily(s_logbook.Takeover(NowMs(), TakeoverQueue.IdOf(kind), shown, queue.Session, queue.ShownThisSession, source)) : null;
        }
    }
}
