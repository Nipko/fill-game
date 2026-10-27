#nullable enable
using AF.Platform;

namespace AF.UI
{
    /// <summary>
    /// Play Age Signals (T-L3 de <c>docs/legal/investigacion-legal-mundial.md</c> §3.2 y §11; D-057 y su añadido del 2026-10-27):
    /// la segunda fuente de la banda de edad, con <c>consent_age_signal_source</c> = <c>self_declared+play_age_signals</c> (08 §9).
    /// <list type="bullet">
    /// <item>Se pide una vez por arranque (por proceso), con la app abierta, al montar el consentimiento (<see cref="RequestAgeSignal"/>);
    /// la respuesta se recoge cada fotograma (<see cref="TickAgeSignals"/>), también durante el arranque.</item>
    /// <item>0-12 → bloqueo, como &lt;13, sin guardarlo en el perfil (la señal se vuelve a pedir en cada arranque; la respuesta se
    /// recuerda en el proceso para que la recarga de la escena arranque ya bloqueada); 13-15 y 16-17 → 13-17; sin señal o con error →
    /// la autodeclarada. Con las dos bandas, la más estricta (<see cref="AgeSignalPolicy"/>).</item>
    /// <item>Si llega tarde (con el juego en marcha), los flags se vuelven a aplicar a los anuncios, la tienda y la analítica; con 0-12,
    /// el bloqueo de siempre (<see cref="BlockSession"/>).</item>
    /// <item>T-L4: la banda de la señal nunca va a la analítica (<see cref="ConsentReport.AnalyticsAgeBand"/>).</item>
    /// </list>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        // Por proceso: la petición en curso y la respuesta (una recarga de la escena no vuelve a pedirla).
        private static IAgeSignals? s_ageSignals;
        private static AgeSignal? s_ageSignal;

        /// <summary>Tests: la fuente de la señal (por defecto, la de Android; en el editor, «sin señal»).</summary>
        internal static IAgeSignals? AgeSignalsOverride { get; set; }

        /// <summary>La señal recibida en este proceso (null si aún no llegó o no se pidió).</summary>
        internal static AgeSignal? LastAgeSignal => s_ageSignal;

        /// <summary>Olvida la petición y la respuesta de este proceso (tests).</summary>
        internal static void ResetAgeSignals()
        {
            AgeSignalsOverride = null;
            s_ageSignals = null;
            s_ageSignal = null;
        }

        /// <summary>
        /// Una señal de 0-12 ya recibida en este proceso: el arranque de la escena recargada no enciende nada que recoja o descargue
        /// (<see cref="DataServicesAllowedAtBoot"/>), como con &lt;13 guardado. Solo hay señal recordada si se pidió, es decir, si la
        /// fuente la incluía.
        /// </summary>
        private static bool SignalBlocksInThisProcess => s_ageSignal.HasValue && s_ageSignal.Value.Band == AgeBand.Under13;

        /// <summary>
        /// Con la fuente <c>self_declared+play_age_signals</c> (el default de 08 §9 desde D-057, añadido del 2026-10-27): aplica la
        /// señal ya recibida en este proceso o, si no se pidió, la pide. Con <c>self_declared</c> (desde Remote Config), nada.
        /// </summary>
        private void RequestAgeSignal(ConsentConfig config)
        {
            if (!AgeSignalPolicy.Enabled(config))
            {
                return;
            }

            if (s_ageSignal.HasValue)
            {
                _consentFlow?.ApplyAgeSignal(s_ageSignal.Value);
                return;
            }

            if (s_ageSignals != null)
            {
                return; // ya pedida en este proceso: la respuesta llega por TickAgeSignals
            }

            s_ageSignals = AgeSignalsOverride ?? AndroidAgeSignals.ForPlatform();
            s_ageSignals.Request();
        }

        /// <summary>Cada fotograma (también en el arranque): recoge la respuesta, si llegó. Sin asignaciones mientras no llega.</summary>
        private void TickAgeSignals()
        {
            IAgeSignals? source = s_ageSignals;
            if (source == null || !source.TryTake(out AgeSignal signal))
            {
                return;
            }

            s_ageSignal = signal;
            OnAgeSignal(signal);
        }

        private void OnAgeSignal(AgeSignal signal)
        {
            ConsentFlow? flow = _consentFlow;
            if (flow == null || ConsentBlocked)
            {
                return; // el flujo del arranque siguiente la aplica (RequestAgeSignal)
            }

            ConsentStep before = flow.Step;
            if (!flow.ApplyAgeSignal(signal))
            {
                return;
            }

            if (before == ConsentStep.None && flow.Mode == ConsentFlowMode.Boot && !_consentApplied)
            {
                return; // el arranque aún no llegó a StartConsentAtBoot: la tiene en cuenta allí
            }

            if (flow.Mode == ConsentFlowMode.Boot && (before == ConsentStep.Age || before == ConsentStep.Choices))
            {
                // La pantalla del arranque está abierta: con 0-12, WaitConsentAtBoot ve el bloqueo y lo aplica; con 13-17, la línea
                // de los anuncios de la pantalla de consentimiento cambia (sin el interruptor de personalizados).
                if (flow.Step == ConsentStep.Choices)
                {
                    ShowConsentStep();
                }

                ProbeConsent();
                return;
            }

            // Con una decisión vigente (o desde Ajustes): los flags otra vez; con 0-12, el bloqueo.
            _consentApplied = false;
            ApplyConsentFlags();
            if (flow.Step == ConsentStep.Blocked || flow.Flags.Blocked)
            {
                _consentScreen?.Hide();
                BlockSession();
                return;
            }

            ProbeConsent();
        }
    }
}
