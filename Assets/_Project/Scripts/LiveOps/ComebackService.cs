#nullable enable
using System;
using AF.Economy;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>Por qué no se dio el Regalo de regreso en una sesión que lo merecía.</summary>
    public enum ComebackBlock
    {
        /// <summary>No se bloqueó.</summary>
        None = 0,

        /// <summary>La hora del dispositivo se aleja de la del servidor más de <c>sec_clock_max_drift_h</c> (08 §10.1) [P].</summary>
        ClockDrift = 1,
    }

    /// <summary>Lo que pasó al volver (<see cref="ComebackService.OnSessionStart"/>).</summary>
    public sealed class ComebackOutcome
    {
        /// <summary>Crea el resultado (lo hace <see cref="ComebackService"/>; público para los tests de la sonda).</summary>
        public ComebackOutcome(ComebackSegment segment, int daysAway, int streakWins)
        {
            Segment = segment;
            DaysAway = daysAway;
            StreakWins = streakWins;
        }

        /// <summary>Segmento de 06 §9.</summary>
        public ComebackSegment Segment { get; }

        /// <summary>Días completos sin sesión (<c>af_comeback_gift.days_away</c>, 08 §8.2).</summary>
        public int DaysAway { get; }

        /// <summary>Se entregó el regalo (dispara <c>af_comeback_gift</c>).</summary>
        public bool Granted { get; set; }

        /// <summary>Por qué no se entregó, si tocaba.</summary>
        public ComebackBlock Block { get; set; }

        /// <summary>Semillas que entraron.</summary>
        public int Seeds { get; set; }

        /// <summary>Unidades de Cuia extra que entraron al stock.</summary>
        public int ExtraCuia { get; set; }

        /// <summary>Canoas que se añadieron hasta el tope (0 si ya estaban llenas, lo normal tras 7 días: se regeneran solas).</summary>
        public int CanoesAdded { get; set; }

        /// <summary>Alguna transacción se rechazó (no debería: solo suman).</summary>
        public bool AnyRejected { get; set; }

        /// <summary>
        /// La Crecida al volver (<c>streak.wins</c>): si es mayor que 0, el aviso dice «Tu Crecida de {n} sigue en pie» (06 §9; la
        /// Crecida no expira por reloj, 06 §4.3).
        /// </summary>
        public int StreakWins { get; }

        /// <summary>Recordatorio de una frase de TiTino y etiqueta honesta en el siguiente nivel («Perdido» y «Churn»).</summary>
        public bool ShowReminder => ComebackRules.GetsReminder(Segment);
    }

    /// <summary>
    /// Regalo de regreso (06 §9, filas «Dormido», «Perdido» y «Churn»; <c>live_comeback_gift</c>, 08 §9): al empezar una sesión tras
    /// <c>days</c> (7) o más sin sesión, 200 semillas + 1 Cuia extra al stock + canoas a su tope, todo por <see cref="Wallet.Apply"/>.
    /// La Hoja escudo activa no la toca: la repone el cambio de día de la Crecida (<c>StreakRecord.RollOver</c>, 06 §4.3) [P]. Sin
    /// Cuia de rescate ni Continuar, sin cambiar el tablero pendiente y sin misiones de pago (06 §9, «Reglas»).
    /// <para>
    /// <b>Nunca dos veces por la misma ausencia:</b> quien llama pasa la <see cref="SessionStart"/> de <see cref="SessionTracker"/>,
    /// que ya guardó <c>stats.last_seen_at</c> = ahora en el mismo perfil que la cartera antes de esta llamada; la cartera vuelve a
    /// guardar el perfil con cada transacción. Si la app muere a medias, se pierde lo que faltaba del regalo, nunca se repite.
    /// </para>
    /// </summary>
    public sealed class ComebackService
    {
        private readonly Wallet _wallet;
        private readonly IServerDate? _serverDate;

        /// <summary>Crea el servicio.</summary>
        /// <param name="wallet">Cartera de campaña (la del mismo perfil que <see cref="ISessionStore"/>).</param>
        /// <param name="config">Cifras (por defecto, las de 08 §9).</param>
        /// <param name="serverDate">Hora del servidor para el anti-reloj, o null (sin red: se acepta la hora local, 08 §10.1).</param>
        public ComebackService(Wallet wallet, ComebackConfig? config = null, IServerDate? serverDate = null)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Config = config ?? ComebackConfig.Defaults;
            Config.Validate();
            _serverDate = serverDate;
        }

        /// <summary>Cifras en uso.</summary>
        public ComebackConfig Config { get; }

        /// <summary>
        /// Decide y, si toca, entrega el regalo al empezar <paramref name="start"/>. <paramref name="streakWins"/> es la Crecida de
        /// ahora (para el aviso «Tu Crecida de {n} sigue en pie»).
        /// </summary>
        public ComebackOutcome OnSessionStart(in SessionStart start, int streakWins)
        {
            ComebackGift gift = Config.LiveComebackGift;
            int days = ComebackRules.DaysAway(start.Away);
            ComebackSegment segment = ComebackRules.Segment(days, in gift);
            var outcome = new ComebackOutcome(segment, days, Math.Max(0, streakWins));
            if (!ComebackRules.GetsGift(segment))
            {
                return outcome;
            }

            if (_serverDate != null && _serverDate.TryGetServerOffset(out TimeSpan offset) && Config.ClockSecurity.ExceedsMaxDrift(offset))
            {
                outcome.Block = ComebackBlock.ClockDrift;
                return outcome;
            }

            if (gift.Seeds > 0)
            {
                // earn_virtual_currency no tiene fuente «comeback» (08 §8.2): event [P], pregunta B6-Q6 de la nota.
                if (Applied(outcome, _wallet.Apply(Transaction.Earn(SeedSource.Event, gift.Seeds))))
                {
                    outcome.Seeds = gift.Seeds;
                }
            }

            if (gift.ExtraCuia > 0)
            {
                if (Applied(outcome, _wallet.Apply(Transaction.GrantStock(BoosterKind.ExtraCuia, gift.ExtraCuia))))
                {
                    outcome.ExtraCuia = gift.ExtraCuia;
                }
            }

            if (gift.RefillCanoes)
            {
                // Sin transacción de «llenar gratis» en la cartera: +1 hasta el tope (canoes_max, 5 por defecto).
                int max = Math.Max(0, _wallet.Config.CanoesMax);
                for (int i = 0; i < max && !_wallet.Canoes.IsFull; i++)
                {
                    TransactionResult added = _wallet.Apply(Transaction.AddCanoe());
                    if (!added.IsApplied)
                    {
                        break;
                    }

                    outcome.CanoesAdded++;
                }
            }

            outcome.Granted = true;
            return outcome;
        }

        private static bool Applied(ComebackOutcome outcome, TransactionResult result)
        {
            if (result.IsApplied)
            {
                return true;
            }

            outcome.AnyRejected = true;
            return false;
        }
    }
}
