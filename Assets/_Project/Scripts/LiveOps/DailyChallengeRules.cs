#nullable enable
using System;
using System.Globalization;
using AF.Economy;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>Resultado de <see cref="DailyChallengeRules.Claim"/> (06 §4.1, <c>reclamar_reto</c>; nota §5).</summary>
    public enum ClaimOutcome
    {
        /// <summary>Sin premio: la fecha ya se cobró o el reloj va hacia atrás (<c>fecha ≤ ultima_fecha_reclamada</c>).</summary>
        NoReward = 0,

        /// <summary>Premio pagado: sin red, o con red y desfase ≤ <c>sec_clock_max_drift_h</c>.</summary>
        Confirmed = 1,

        /// <summary>«Pendiente de validar»: con red y desfase &gt; <c>sec_clock_max_drift_h</c>; se paga o se anula al reconciliar.</summary>
        Pending = 2,
    }

    /// <summary>
    /// Qué hace la reconciliación con los cobros pagados <b>sin red</b> (pregunta R-Q12 de la nota §1.6, pendiente del dueño; 08 §10.1:
    /// «sin red se acepta la fecha local y se reconcilia al reconectar»).
    /// </summary>
    public enum DailyReconcileMode
    {
        /// <summary>
        /// (a) [P, default]: no se retira nada; los cobros sin red se dan por buenos. Coherente con 08 §10.1, «Edición del perfil
        /// local»: en la 1.0 la economía de semillas es autoritativa en el cliente.
        /// </summary>
        KeepOfflineClaims = 0,

        /// <summary>
        /// (b): los cobros sin red con fecha posterior a la del servidor se anulan (se quita el sello y se rompe la racha); las
        /// semillas y la Pororoca ya pagadas no se retiran.
        /// </summary>
        VoidOfflineAfterServerDate = 1,
    }

    /// <summary>Detalle del último <see cref="DailyChallengeRules.Claim"/>: para la UI (+60, sello) y la sonda (R10).</summary>
    public readonly struct DailyClaimReport
    {
        /// <summary>Crea el informe.</summary>
        public DailyClaimReport(
            ClaimOutcome outcome, string dateKey, int streakDays, int seedsPaid, int shufflePaid, string? streakRewardSkipped)
        {
            Outcome = outcome;
            DateKey = dateKey;
            StreakDays = streakDays;
            SeedsPaid = seedsPaid;
            ShufflePaid = shufflePaid;
            StreakRewardSkipped = streakRewardSkipped;
        }

        /// <summary>Resultado.</summary>
        public ClaimOutcome Outcome { get; }

        /// <summary>Fecha reclamada (la de inicio de la partida, R-Q3).</summary>
        public string DateKey { get; }

        /// <summary><c>streak_days</c> tras el cobro (sin cambios con <see cref="ClaimOutcome.NoReward"/>).</summary>
        public int StreakDays { get; }

        /// <summary>Semillas pagadas ahora (0 si queda pendiente o sin premio).</summary>
        public int SeedsPaid { get; }

        /// <summary>Pororocas pagadas ahora por la racha de 7.</summary>
        public int ShufflePaid { get; }

        /// <summary>
        /// Valor de <c>live_daily_streak7_reward</c> que cerraba una racha pero no se pagó porque no es <c>"shuffle"</c> (el único
        /// premio que define 08 §9 / 04 §2.4: no se inventa otro). Null si no hubo nada que omitir.
        /// </summary>
        public string? StreakRewardSkipped { get; }
    }

    /// <summary>Resultado de <see cref="DailyChallengeRules.Reconcile"/>.</summary>
    public readonly struct DailyReconcileResult
    {
        /// <summary>Crea el resultado.</summary>
        public DailyReconcileResult(
            bool ran, int confirmed, int voided, int offlineKept, int offlineVoided, int seedsPaid, int shufflePaid,
            string? streakRewardSkipped)
        {
            Ran = ran;
            Confirmed = confirmed;
            Voided = voided;
            OfflineKept = offlineKept;
            OfflineVoided = offlineVoided;
            SeedsPaid = seedsPaid;
            ShufflePaid = shufflePaid;
            StreakRewardSkipped = streakRewardSkipped;
        }

        /// <summary>Hubo hora de servidor fiable y se reconcilió (si no, todo sigue igual).</summary>
        public bool Ran { get; }

        /// <summary>Pendientes confirmadas y pagadas.</summary>
        public int Confirmed { get; }

        /// <summary>Pendientes anuladas (a más de <see cref="DailyChallengeRules.PendingWindowDays"/> día de la fecha del servidor; R-Q12b).</summary>
        public int Voided { get; }

        /// <summary>Cobros sin red que se dan por buenos.</summary>
        public int OfflineKept { get; }

        /// <summary>Cobros sin red anulados (solo con <see cref="DailyReconcileMode.VoidOfflineAfterServerDate"/>).</summary>
        public int OfflineVoided { get; }

        /// <summary>Semillas pagadas al confirmar pendientes.</summary>
        public int SeedsPaid { get; }

        /// <summary>Pororocas pagadas al confirmar pendientes.</summary>
        public int ShufflePaid { get; }

        /// <summary>Como <see cref="DailyClaimReport.StreakRewardSkipped"/>.</summary>
        public string? StreakRewardSkipped { get; }

        /// <summary>Cobros resueltos en total.</summary>
        public int Total => Confirmed + Voided + OfflineKept + OfflineVoided;
    }

    /// <summary>
    /// Premio, anti-reloj y racha del Reto del río: <c>reclamar_reto</c> de 06 §4.1 adaptado a R-Q3, R-Q5 y R-Q7 (nota
    /// <c>docs/dev/reto-del-rio.md</c> §7), con la regla de 08 §10.1 en horas (<c>sec_clock_max_drift_h</c>). La hora sale solo de
    /// <see cref="IClock"/> y la del servidor de <see cref="IServerDate"/>; las fechas son claves <c>yyyy-MM-dd</c> que se comparan
    /// como texto ordinal (mismo orden que la fecha) y se pasan a fecha con <c>ParseExact</c> e InvariantCulture para restar días.
    /// <para>
    /// Orden de escritura (nota §5): primero se registra el cobro en el <see cref="DailyChallengeRecord"/> y se guarda, y después se
    /// paga con <see cref="Wallet.Apply"/>. Si el guardado falla, el registro sigue en memoria (<see cref="IDailyChallengeStore.PendingSave"/>)
    /// y otra reclamación de la misma fecha ya no paga: nunca se paga dos veces la misma fecha.
    /// </para>
    /// </summary>
    public sealed class DailyChallengeRules
    {
        /// <summary>Días seguidos ganados que dan la Pororoca (06 §4.1; R-Q7: cada racha completa).</summary>
        public const int StreakLength = 7;

        /// <summary>
        /// R-Q12b (a): un pendiente solo se valida si su fecha está a este número de días o menos de la fecha del servidor, hacia
        /// atrás o hacia delante; los demás se anulan.
        /// </summary>
        public const int PendingWindowDays = 1;

        /// <summary>Único valor de <c>live_daily_streak7_reward</c> que se sabe pagar: 1 Pororoca al stock (04 §2.4; 08 §9).</summary>
        public const string ShuffleReward = "shuffle";

        private const string DateFormat = "yyyy'-'MM'-'dd";
        private const string MonthFormat = "yyyy'-'MM";

        private readonly IDailyChallengeStore _store;
        private readonly Wallet _wallet;
        private readonly IClock _clock;
        private readonly IServerDate _server;
        private readonly DailyChallengeConfig _config;

        /// <summary>Crea las reglas.</summary>
        /// <param name="reconcileMode">Rama de R-Q12 (pendiente del dueño); por defecto (a) [P].</param>
        /// <exception cref="InvalidOperationException">La configuración no es válida.</exception>
        public DailyChallengeRules(
            IDailyChallengeStore store, Wallet wallet, IClock clock, IServerDate server, DailyChallengeConfig config,
            DailyReconcileMode reconcileMode = DailyReconcileMode.KeepOfflineClaims)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _config.Validate();
            ReconcileMode = reconcileMode;
        }

        /// <summary>Rama de R-Q12 en uso.</summary>
        public DailyReconcileMode ReconcileMode { get; }

        /// <summary>Detalle del último <see cref="Claim"/>.</summary>
        public DailyClaimReport LastClaim { get; private set; }

        /// <summary>
        /// <c>reclamar_reto</c> (nota §7.1) para la fecha local de inicio <paramref name="startDateKey"/> (R-Q3):
        /// <list type="number">
        /// <item>sin premio si <c>fecha ≤ LastClaimedDate</c> (ya cobrada o reloj atrasado; 06 §4.1);</item>
        /// <item>pendiente si hay red y |servidor − dispositivo| &gt; <c>sec_clock_max_drift_h</c> horas (08 §10.1); si no, confirmado;</item>
        /// <item>racha: +1 si <c>LastClaimedDate</c> es el día anterior, si no 1 (R-Q7);</item>
        /// <item>se registra (<c>LastClaimedDate</c>, <c>LastDate</c>, <c>Result</c>, sello del mes) y se guarda;</item>
        /// <item>si es confirmado, se pagan las semillas y, cada 7 días de racha, 1 Pororoca.</item>
        /// </list>
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="startDateKey"/> no es una fecha <c>yyyy-MM-dd</c>.</exception>
        public ClaimOutcome Claim(string startDateKey)
        {
            DateTime date = ParseDateKey(startDateKey, nameof(startDateKey));
            DailyChallengeRecord record = _store.Load();

            if (record.LastClaimedDate != null && string.CompareOrdinal(startDateKey, record.LastClaimedDate) <= 0)
            {
                LastClaim = new DailyClaimReport(ClaimOutcome.NoReward, startDateKey, record.StreakDays, 0, 0, null);
                return ClaimOutcome.NoReward;
            }

            bool online = _server.TryGetServerOffset(out TimeSpan offset);
            bool pending = online && _config.ClockSecurity.ExceedsMaxDrift(offset);

            bool consecutive = record.LastClaimedDate != null
                && ParseDateKey(record.LastClaimedDate, nameof(record.LastClaimedDate)) == date.AddDays(-1);
            int streak = consecutive ? record.StreakDays + 1 : 1;
            var entry = new DailyClaimEntry(startDateKey, streak);

            // 1) Registrar y guardar.
            record.StreakDays = streak;
            record.LastClaimedDate = startDateKey;
            record.LastDate = startDateKey;
            record.Result = DailyResult.Win;
            Stamp(record, date);
            if (pending)
            {
                record.PendingClaims.Add(entry);
            }
            else if (!online)
            {
                record.OfflineClaims.Add(entry);
            }

            record.InProgress = null;
            _store.Save(record);

            // 2) Pagar.
            if (pending)
            {
                LastClaim = new DailyClaimReport(ClaimOutcome.Pending, startDateKey, streak, 0, 0, null);
                return ClaimOutcome.Pending;
            }

            int seeds = PaySeeds();
            int shuffle = PayStreak(entry, out string? skipped);
            LastClaim = new DailyClaimReport(ClaimOutcome.Confirmed, startDateKey, streak, seeds, shuffle, skipped);
            return ClaimOutcome.Confirmed;
        }

        /// <summary>
        /// Reconciliación al tener hora de servidor (nota §7.2 [P]; 08 §10.1). Solo corre con red y con un desfase dentro de
        /// <c>sec_clock_max_drift_h</c>; si no, no cambia nada (<see cref="DailyReconcileResult.Ran"/> = false). La fecha del
        /// servidor es la local de <c>UtcNow + desfase + LocalOffset</c>.
        /// <list type="bullet">
        /// <item>R-Q12b (a): pendientes con |fecha − fecha del servidor| ≤ <see cref="PendingWindowDays"/> día: se validan y se pagan
        /// (60 y, si cerraban racha, la Pororoca).</item>
        /// <item>Los demás pendientes (más viejos o más adelantados): se anulan, sin semillas; se quita su sello y la racha se
        /// rompe en ese día (R-Q7: un día sin ganar la rompe).</item>
        /// <item>Cobros sin red: según <see cref="ReconcileMode"/> (R-Q12; el dueño eligió (a), no se retira nada).</item>
        /// </list>
        /// Con alguna fecha anulada, la racha (la del registro y la de cada pendiente que se paga, para la Pororoca) se corta en la
        /// última fecha anulada anterior: solo cuentan los días seguidos después de ella. R-Q12 (aclaración a): nada de lo ya pagado
        /// (semillas, Pororoca) se retira. <c>LastClaimedDate</c> nunca retrocede. Se registra y se guarda antes de pagar.
        /// </summary>
        public DailyReconcileResult Reconcile()
        {
            if (!_server.TryGetServerOffset(out TimeSpan offset) || _config.ClockSecurity.ExceedsMaxDrift(offset))
            {
                return default;
            }

            DateTime serverDate = (_clock.UtcNow + offset + _clock.LocalOffset).Date;
            string serverToday = ClockExtensions.FormatDateKey(serverDate);
            DailyChallengeRecord record = _store.Load();

            int count = record.PendingClaims.Count + record.OfflineClaims.Count;
            var toPay = new DailyClaimEntry[record.PendingClaims.Count];
            var voidedDates = new DateTime[count];
            int confirmed = 0;
            int voided = 0;
            for (int i = 0; i < record.PendingClaims.Count; i++)
            {
                DailyClaimEntry entry = record.PendingClaims[i];
                DateTime date = ParseDateKey(entry.DateKey, nameof(entry.DateKey));
                if (Math.Abs((date - serverDate).Days) <= PendingWindowDays)
                {
                    toPay[confirmed++] = entry;
                }
                else
                {
                    Unstamp(record, entry.DateKey);
                    voidedDates[voided++] = date;
                }
            }

            int offlineKept = 0;
            int offlineVoided = 0;
            for (int i = 0; i < record.OfflineClaims.Count; i++)
            {
                DailyClaimEntry entry = record.OfflineClaims[i];
                if (ReconcileMode == DailyReconcileMode.VoidOfflineAfterServerDate
                    && string.CompareOrdinal(entry.DateKey, serverToday) > 0)
                {
                    Unstamp(record, entry.DateKey);
                    voidedDates[voided + offlineVoided++] = ParseDateKey(entry.DateKey, nameof(entry.DateKey));
                }
                else
                {
                    offlineKept++;
                }
            }

            int allVoided = voided + offlineVoided;
            if (allVoided > 0 && record.LastClaimedDate != null)
            {
                record.StreakDays = StreakAfterVoids(
                    ParseDateKey(record.LastClaimedDate, nameof(record.LastClaimedDate)), record.StreakDays, voidedDates, allVoided);
            }

            for (int i = 0; i < confirmed && allVoided > 0; i++)
            {
                DailyClaimEntry entry = toPay[i];
                int streak = StreakAfterVoids(ParseDateKey(entry.DateKey, nameof(entry.DateKey)), entry.StreakDays, voidedDates, allVoided);
                toPay[i] = new DailyClaimEntry(entry.DateKey, streak);
            }

            record.PendingClaims.Clear();
            record.OfflineClaims.Clear();
            _store.Save(record);

            int seeds = 0;
            int shuffle = 0;
            string? skipped = null;
            for (int i = 0; i < confirmed; i++)
            {
                seeds += PaySeeds();
                shuffle += PayStreak(toPay[i], out string? s);
                skipped ??= s;
            }

            return new DailyReconcileResult(true, confirmed, voided, offlineKept, offlineVoided, seeds, shuffle, skipped);
        }

        /// <summary>
        /// Racha mostrada «{n}/7» (nota §7.3 [P]): viva si <c>LastClaimedDate</c> es hoy o ayer (fecha local), si no 0. Con hoy
        /// sellado, <c>((StreakDays − 1) mod 7) + 1</c> (el día de la Pororoca se ve 7/7); con hoy sin sellar,
        /// <c>StreakDays mod 7</c>.
        /// </summary>
        public int StreakShown()
        {
            DailyChallengeRecord record = _store.Load();
            if (record.LastClaimedDate == null || record.StreakDays <= 0)
            {
                return 0;
            }

            DateTime today = _clock.LocalDate();
            DateTime last = ParseDateKey(record.LastClaimedDate, nameof(record.LastClaimedDate));
            if (last == today)
            {
                return ((record.StreakDays - 1) % StreakLength) + 1;
            }

            return last == today.AddDays(-1) ? record.StreakDays % StreakLength : 0;
        }

        /// <summary>Lee una clave <c>yyyy-MM-dd</c> (InvariantCulture, calendario gregoriano).</summary>
        public static bool TryParseDateKey(string? dateKey, out DateTime date)
        {
            date = default;
            return dateKey != null && dateKey.Length == 10
                && DateTime.TryParseExact(dateKey, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        private static DateTime ParseDateKey(string? dateKey, string paramName)
        {
            if (!TryParseDateKey(dateKey, out DateTime date))
            {
                throw new ArgumentException("«" + dateKey + "» no es una fecha yyyy-MM-dd.", paramName);
            }

            return date;
        }

        // La racha de streak días seguidos que acaba en date, cortada en la última fecha anulada que cae dentro de ella (ese día
        // ya no cuenta como ganado): quedan los días entre esa fecha y date; 0 si se anuló date.
        private static int StreakAfterVoids(DateTime date, int streak, DateTime[] voidedDates, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (voidedDates[i] <= date)
                {
                    int after = (date - voidedDates[i]).Days;
                    if (after < streak)
                    {
                        streak = after;
                    }
                }
            }

            return streak;
        }

        private static void Stamp(DailyChallengeRecord record, DateTime date)
        {
            string month = date.ToString(MonthFormat, CultureInfo.InvariantCulture);
            if (!string.Equals(record.StampsMonth, month, StringComparison.Ordinal))
            {
                record.StampsMonth = month;
                record.StampDays.Clear();
            }

            int day = date.Day;
            int at = 0;
            while (at < record.StampDays.Count && record.StampDays[at] < day)
            {
                at++;
            }

            if (at == record.StampDays.Count || record.StampDays[at] != day)
            {
                record.StampDays.Insert(at, day);
            }
        }

        private static void Unstamp(DailyChallengeRecord record, string dateKey)
        {
            DateTime date = ParseDateKey(dateKey, nameof(dateKey));
            if (string.Equals(record.StampsMonth, date.ToString(MonthFormat, CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                record.StampDays.Remove(date.Day);
            }
        }

        private int PaySeeds()
        {
            int seeds = _config.LiveDailyChallengeReward.Seeds;
            if (seeds <= 0)
            {
                return 0;
            }

            return _wallet.Apply(Transaction.Earn(SeedSource.DailyChallenge, seeds)).IsApplied ? seeds : 0;
        }

        private int PayStreak(DailyClaimEntry entry, out string? skipped)
        {
            skipped = null;
            if (!entry.ClosesStreak)
            {
                return 0;
            }

            if (!string.Equals(_config.LiveDailyStreak7Reward, ShuffleReward, StringComparison.Ordinal))
            {
                skipped = _config.LiveDailyStreak7Reward;
                return 0;
            }

            return _wallet.Apply(Transaction.GrantStock(BoosterKind.Shuffle, 1)).IsApplied ? 1 : 0;
        }
    }
}
