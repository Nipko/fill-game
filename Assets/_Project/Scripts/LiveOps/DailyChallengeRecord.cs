#nullable enable
using System;
using System.Collections.Generic;
using AF.Save;

namespace AF.LiveOps
{
    /// <summary>Valores de <c>af_daily_challenge.result</c> (08 §8.2) y de <c>daily_challenge.result</c> del perfil (08 §4.3).</summary>
    public enum DailyResult
    {
        /// <summary>Aún no hay resultado.</summary>
        None = 0,

        /// <summary><c>win</c>: tablero ganado.</summary>
        Win = 1,

        /// <summary><c>fail</c>: salir sin ganar ese día (R-Q9).</summary>
        Fail = 2,

        /// <summary><c>abandon</c>: partida descartada por cambio de día al reabrir (R-Q4; nota §8 y §12) [P].</summary>
        Abandon = 3,
    }

    /// <summary>Ids de <see cref="DailyResult"/> para la analítica y el perfil (08 §8.2).</summary>
    public static class DailyResults
    {
        /// <summary><c>win</c>, <c>fail</c>, <c>abandon</c> o <c>none</c>.</summary>
        public static string ToId(this DailyResult result)
        {
            switch (result)
            {
                case DailyResult.Win: return "win";
                case DailyResult.Fail: return "fail";
                case DailyResult.Abandon: return "abandon";
                default: return "none";
            }
        }
    }

    /// <summary>
    /// Partida del Reto en curso (nota §5 y §10, <c>in_progress</c> [P]): R-Q3, manda la fecha de inicio (se juega ese tablero
    /// y se cobra esa fecha aunque se gane pasada la medianoche); R-Q4, se reanuda al reabrir el mismo día y se descarta otro día.
    /// La fecha y la banda se congelan al empezar.
    /// </summary>
    public sealed class DailyPlayInProgress
    {
        /// <summary><c>date</c>: fecha local de inicio, <c>yyyy-MM-dd</c>.</summary>
        public string DateKey { get; set; } = "";

        /// <summary><c>band</c>: banda 1-3, congelada al empezar (nota §6).</summary>
        public int Band { get; set; }

        /// <summary><c>id</c>: tablero <c>d{b}_n{nnnn}</c> (03 §16), el sustituto si <c>daily_overrides</c> lo cambió.</summary>
        public string BoardId { get; set; } = "";

        /// <summary><c>level_state</c>: estado de la partida con el formato de 04 §10.1 (lo escribe la partida, R8).</summary>
        public LevelStateData State { get; set; } = new LevelStateData();

        /// <summary>Copia independiente (también de <see cref="State"/>).</summary>
        public DailyPlayInProgress Clone()
        {
            return new DailyPlayInProgress { DateKey = DateKey, Band = Band, BoardId = BoardId, State = CopyState(State) };
        }

        // Mismos campos que AF.GameFlow.LevelStateSnapshot.Copy (GameFlow no es visible desde aquí);
        // DailyChallengeStoreTests.Record_CloneCopiesEveryProperty comprueba por reflexión que no falta ninguno.
        internal static LevelStateData CopyState(LevelStateData state)
        {
            var copy = new LevelStateData
            {
                Level = state.Level,
                Seed = state.Seed,
                Variant = state.Variant,
                FailsLevel = state.FailsLevel,
                Pours = state.Pours,
                ExtraCuias = state.ExtraCuias,
                RescuesLevel = state.RescuesLevel,
                UndoFree = state.UndoFree,
                History = state.History,
                Stuck = state.Stuck,
            };
            copy.RevealedCuias.AddRange(state.RevealedCuias);
            return copy;
        }
    }

    /// <summary>
    /// Un cobro que aún puede cambiar al reconciliar (nota §7.2): la fecha cobrada y la racha que dejó, para saber al confirmarlo
    /// si cerraba una racha de 7 (R-Q7). [P]: la nota §5 lo guardaba como lista de fechas; con la racha al lado no hace falta
    /// reconstruirla.
    /// </summary>
    public readonly struct DailyClaimEntry : IEquatable<DailyClaimEntry>
    {
        /// <summary>Crea la entrada.</summary>
        public DailyClaimEntry(string dateKey, int streakDays)
        {
            DateKey = dateKey ?? throw new ArgumentNullException(nameof(dateKey));
            StreakDays = streakDays;
        }

        /// <summary><c>date</c>: fecha cobrada, <c>yyyy-MM-dd</c>.</summary>
        public string DateKey { get; }

        /// <summary><c>streak</c>: <c>streak_days</c> tras ese cobro.</summary>
        public int StreakDays { get; }

        /// <summary>Ese cobro cerró una racha completa de 7 (R-Q7: cada 7 días seguidos ganados).</summary>
        public bool ClosesStreak => StreakDays > 0 && StreakDays % DailyChallengeRules.StreakLength == 0;

        /// <inheritdoc />
        public bool Equals(DailyClaimEntry other)
        {
            return string.Equals(DateKey, other.DateKey, StringComparison.Ordinal) && StreakDays == other.StreakDays;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is DailyClaimEntry other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // Sin string.GetHashCode (varía entre procesos).
            return StreakDays * 31 + (DateKey == null ? 0 : DateKey.Length);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return DateKey + ":" + StreakDays;
        }
    }

    /// <summary>
    /// Bloque <c>daily_challenge</c> del perfil (08 §4.3 ampliado por R-Q5 y R-Q4; nota §10). Lo usa
    /// <see cref="DailyChallengeRules"/> a través de <see cref="IDailyChallengeStore"/>; su forma en el JSON del perfil es el
    /// enganche E4 (la hace el integrador).
    /// <code>
    /// "daily_challenge": { "last_date", "result", "streak_days", "last_claimed_date", "pending": [{date, streak}],
    ///                      "offline_claims": [{date, streak}], "stamps": {"month", "days"}, "in_progress" }
    /// </code>
    /// </summary>
    public sealed class DailyChallengeRecord
    {
        /// <summary><c>last_date</c> (08 §4.3): fecha del último resultado registrado.</summary>
        public string? LastDate { get; set; }

        /// <summary><c>result</c> (08 §4.3): último resultado, con los valores de 08 §8.2.</summary>
        public DailyResult Result { get; set; }

        /// <summary><c>streak_days</c> (08 §4.3): días seguidos ganados y cobrados (R-Q7).</summary>
        public int StreakDays { get; set; }

        /// <summary>
        /// <c>last_claimed_date</c> (R-Q5): <c>ultima_fecha_reclamada</c> de 06 §4.1. Solo avanza: tampoco retrocede al anular un
        /// cobro (nota §7.2), para que no se pueda volver a cobrar esa fecha.
        /// </summary>
        public string? LastClaimedDate { get; set; }

        /// <summary><c>pending</c> (R-Q5, «premio pendiente de validar»): cobros con red y desfase &gt; <c>sec_clock_max_drift_h</c>.</summary>
        public List<DailyClaimEntry> PendingClaims { get; } = new List<DailyClaimEntry>();

        /// <summary>
        /// <c>offline_claims</c> [P]: cobros pagados sin red desde la última reconciliación. Solo los usa la rama (b) de R-Q12
        /// (<see cref="DailyReconcileMode.VoidOfflineAfterServerDate"/>); la reconciliación la vacía en las dos ramas. Acotada a los
        /// cobros de los últimos <see cref="DailyChallengeRules.OfflineClaimsMaxDays"/> días [P]: sin red durante meses no crece
        /// sin límite (una entrada por día cobrado).
        /// </summary>
        public List<DailyClaimEntry> OfflineClaims { get; } = new List<DailyClaimEntry>();

        /// <summary><c>stamps.month</c> (R-Q5): mes de los sellos, <c>yyyy-MM</c>; null si aún no hay ninguno.</summary>
        public string? StampsMonth { get; set; }

        /// <summary><c>stamps.days</c> (R-Q5): días sellados de <see cref="StampsMonth"/> (1-31), en orden creciente.</summary>
        public List<int> StampDays { get; } = new List<int>();

        /// <summary><c>in_progress</c> [P] (R-Q4): partida en curso, o null.</summary>
        public DailyPlayInProgress? InProgress { get; set; }

        /// <summary>La fecha <paramref name="dateKey"/> tiene el premio pendiente de validar.</summary>
        public bool IsPending(string dateKey)
        {
            return IndexOf(PendingClaims, dateKey) >= 0;
        }

        /// <summary>
        /// La fecha <paramref name="dateKey"/> (<c>yyyy-MM-dd</c>) tiene su sello en el mes guardado (<see cref="StampsMonth"/> y
        /// <see cref="StampDays"/>). False si la clave no es una fecha o el sello se quitó al anular el cobro (nota §7.2).
        /// </summary>
        public bool IsStamped(string? dateKey)
        {
            if (StampsMonth == null || StampsMonth.Length != 7 || dateKey == null || dateKey.Length != 10
                || string.CompareOrdinal(dateKey, 0, StampsMonth, 0, 7) != 0 || dateKey[7] != '-')
            {
                return false;
            }

            int tens = dateKey[8] - '0';
            int units = dateKey[9] - '0';
            if (tens < 0 || tens > 3 || units < 0 || units > 9)
            {
                return false;
            }

            return StampDays.Contains(tens * 10 + units);
        }

        /// <summary>Copia independiente (listas y partida incluidas).</summary>
        public DailyChallengeRecord Clone()
        {
            var copy = new DailyChallengeRecord
            {
                LastDate = LastDate,
                Result = Result,
                StreakDays = StreakDays,
                LastClaimedDate = LastClaimedDate,
                StampsMonth = StampsMonth,
                InProgress = InProgress?.Clone(),
            };
            copy.PendingClaims.AddRange(PendingClaims);
            copy.OfflineClaims.AddRange(OfflineClaims);
            copy.StampDays.AddRange(StampDays);
            return copy;
        }

        internal static int IndexOf(List<DailyClaimEntry> entries, string dateKey)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].DateKey, dateKey, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
