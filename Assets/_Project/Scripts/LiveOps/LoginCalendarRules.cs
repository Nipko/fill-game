#nullable enable
using System;
using System.Globalization;
using AF.Economy;

namespace AF.LiveOps
{
    /// <summary>
    /// Qué comodines puede recibir el jugador ya (D-033, añadido del 2026-10-20, punto 3: «Comodín bloqueado: se sustituye por el
    /// siguiente desbloqueado del mismo valor»). <c>AF.LiveOps</c> no ve <c>AF.Boosters</c>: quien llama lo calcula con los niveles
    /// de desbloqueo de 08 §9 (<c>hint_unlock_level</c> 12, <c>extra_cuia_unlock_level</c> 8 y <c>lvl_murky_from_level</c> 100 de la
    /// Luciérnaga, 04 §2.5) y el nivel que el jugador va a jugar (<see cref="AtLevel"/>). Marea se tiene desde el nivel 1 (04
    /// §2.1): nunca está bloqueada. Es un valor.
    /// </summary>
    public readonly struct LoginCalendarUnlocks : IEquatable<LoginCalendarUnlocks>
    {
        /// <summary>Crea el estado.</summary>
        public LoginCalendarUnlocks(bool hint, bool reveal, bool extraCuia)
        {
            Hint = hint;
            Reveal = reveal;
            ExtraCuia = extraCuia;
        }

        /// <summary>Todo desbloqueado.</summary>
        public static LoginCalendarUnlocks All => new LoginCalendarUnlocks(true, true, true);

        /// <summary>Morpho desbloqueado.</summary>
        public bool Hint { get; }

        /// <summary>Luciérnaga desbloqueada.</summary>
        public bool Reveal { get; }

        /// <summary>Cuia extra desbloqueada.</summary>
        public bool ExtraCuia { get; }

        /// <summary>
        /// Lo desbloqueado en el nivel <paramref name="level"/> (el siguiente de la campaña, <c>max_level</c> + 1 [P], como el nivel
        /// efectivo del Reto, R-Q8): cada comodín, desde su nivel de desbloqueo.
        /// </summary>
        public static LoginCalendarUnlocks AtLevel(int level, int hintUnlockLevel, int revealUnlockLevel, int extraCuiaUnlockLevel)
        {
            return new LoginCalendarUnlocks(level >= hintUnlockLevel, level >= revealUnlockLevel, level >= extraCuiaUnlockLevel);
        }

        /// <summary><paramref name="kind"/> se puede recibir ya (Marea siempre; la Pororoca no la da la Bitácora).</summary>
        public bool IsUnlocked(BoosterKind kind)
        {
            switch (kind)
            {
                case BoosterKind.Hint: return Hint;
                case BoosterKind.Reveal: return Reveal;
                case BoosterKind.ExtraCuia: return ExtraCuia;
                default: return true;
            }
        }

        /// <inheritdoc />
        public bool Equals(LoginCalendarUnlocks other)
        {
            return Hint == other.Hint && Reveal == other.Reveal && ExtraCuia == other.ExtraCuia;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is LoginCalendarUnlocks other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (Hint ? 1 : 0) | (Reveal ? 2 : 0) | (ExtraCuia ? 4 : 0);
        }
    }

    /// <summary>Estado de la Bitácora para el inicio (tarjeta con punto, 06 §4.2) y para la cola de takeovers.</summary>
    public enum LoginCalendarState
    {
        /// <summary>No existe para el jugador: apagada (<c>biz_feature_login_calendar</c>) o antes de <c>live_unlock_level_daily</c>.</summary>
        Hidden = 0,

        /// <summary>Hay casilla esperando, pero hoy aún no terminó ningún nivel (06 §4.2: «haber jugado ≥1 nivel ese día local»).</summary>
        Waiting = 1,

        /// <summary>La casilla de hoy se puede sellar: punto en la tarjeta y takeover al entrar al inicio (D-033, punto 5).</summary>
        Ready = 2,

        /// <summary>Hoy ya se selló una página: la siguiente, mañana (o el reloj va hacia atrás).</summary>
        Sealed = 3,
    }

    /// <summary>
    /// Reglas de la Bitácora del río «1 de 3» (06 §4.2; 05 §2.3; D-029 #17 B; Q-M1 (a) con el diseño de D-033, añadido del
    /// 2026-10-20, puntos 1-7), sin motor, sin reloj y sin estado propio:
    /// <list type="number">
    /// <item><description>30 casillas que se sellan una por día local, <b>secuenciales y sin reinicio</b>: si el jugador falta un
    /// día, la siguiente casilla sigue esperando; al sellar la 30 empieza otro ciclo (<see cref="Advance"/>).</description></item>
    /// <item><description>Condición: haber terminado ≥ 1 nivel hoy, de campaña o del Reto, ganado o perdido (D-033, punto 5,
    /// <c>played_date</c> [P]); y la Bitácora desbloqueada (<c>live_unlock_level_daily</c> = 10, con su interruptor).</description></item>
    /// <item><description>Una página por día: con <c>last_claim_date</c> = hoy no se sella otra, y con el reloj atrasado
    /// (<c>last_claim_date</c> en el futuro) tampoco: el día solo avanza.</description></item>
    /// <item><description>Las tres opciones de la casilla, con los comodines bloqueados sustituidos por el siguiente desbloqueado
    /// del mismo valor y nunca dos opciones iguales (<see cref="ResolveOptions"/>; punto 3).</description></item>
    /// </list>
    /// </summary>
    public static class LoginCalendarRules
    {
        /// <summary>Lo que sustituye a una unidad bloqueada al final de la cadena (D-033, punto 3): «2 Marea + 40».</summary>
        public const int TerminalUndo = 2;

        /// <summary>Semillas de la sustitución final (D-033, punto 3).</summary>
        public const int TerminalSeeds = 40;

        /// <summary>Prefijo del id del marco de país [P] (<c>frame_w01</c>…<c>frame_w09</c>; Q-B6).</summary>
        public const string FramePrefix = "frame_w";

        private static readonly BoosterKind[] ChainHint = { BoosterKind.ExtraCuia };
        private static readonly BoosterKind[] ChainReveal = { BoosterKind.Hint, BoosterKind.ExtraCuia };
        private static readonly BoosterKind[] ChainNone = new BoosterKind[0];

        /// <summary>La Bitácora existe para el jugador: encendida y con <paramref name="maxLevel"/> ≥ <paramref name="unlockLevel"/>.</summary>
        public static bool IsAvailable(bool enabled, int maxLevel, int unlockLevel)
        {
            return enabled && maxLevel >= unlockLevel;
        }

        /// <summary>Estado de la Bitácora hoy (<paramref name="todayKey"/>, <c>yyyy-MM-dd</c>).</summary>
        public static LoginCalendarState StateOf(LoginCalendarRecord record, string todayKey, bool available)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (!available)
            {
                return LoginCalendarState.Hidden;
            }

            // yyyy-MM-dd se ordena igual como texto que como fecha.
            if (record.LastClaimDate != null && string.CompareOrdinal(record.LastClaimDate, todayKey) >= 0)
            {
                return LoginCalendarState.Sealed;
            }

            return string.Equals(record.PlayedDate, todayKey, StringComparison.Ordinal) ? LoginCalendarState.Ready : LoginCalendarState.Waiting;
        }

        /// <summary>
        /// Apunta que hoy se terminó un nivel. Devuelve si cambió algo (solo la primera vez del día, o con el reloj adelantado);
        /// con el reloj atrasado (un <c>played_date</c> posterior) no lo cambia.
        /// </summary>
        public static bool MarkPlayed(LoginCalendarRecord record, string todayKey)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (record.PlayedDate != null && string.CompareOrdinal(record.PlayedDate, todayKey) >= 0)
            {
                return false;
            }

            record.PlayedDate = todayKey;
            return true;
        }

        /// <summary>
        /// <c>days_missed_before</c> de <c>af_calendar_claim</c> (08 §8.2): días locales sin sellar entre el último sello y hoy (0 la
        /// primera vez o si se selló ayer).
        /// </summary>
        public static int DaysMissedBefore(LoginCalendarRecord record, DateTime today)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (record.LastClaimDate == null
                || !DateTime.TryParseExact(record.LastClaimDate, "yyyy'-'MM'-'dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime last))
            {
                return 0;
            }

            int gap = (int)(today.Date - last.Date).TotalDays - 1;
            return gap > 0 ? gap : 0;
        }

        /// <summary>Sella la casilla de hoy: <c>next_day</c> + 1 (y otro ciclo tras la 30) y <c>last_claim_date</c> = hoy.</summary>
        public static void Advance(LoginCalendarRecord record, string todayKey)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            record.Sanitize();
            record.LastClaimDate = todayKey;
            if (record.NextDay >= LoginCalendarTable.Days)
            {
                record.NextDay = 1;
                record.Cycle = record.Cycle < int.MaxValue ? record.Cycle + 1 : int.MaxValue;
            }
            else
            {
                record.NextDay++;
            }
        }

        /// <summary>
        /// Las tres opciones de <paramref name="cell"/> para el jugador, en orden semillas / comodín / mezcla, en
        /// <paramref name="into"/> (3 huecos): cada unidad bloqueada pasa al siguiente comodín desbloqueado del mismo valor (Morpho
        /// → Cuia extra → «2 Marea + 40»; Luciérnaga → Morpho → Cuia extra → «2 Marea + 40» [P], Q-B5) y, si una opción queda igual
        /// que otra anterior, la sustitución de esa opción da un paso más (D-033, punto 3: «nunca dos opciones iguales»). El valor
        /// se conserva porque los cuatro sustitutos valen 100. Sin asignaciones.
        /// </summary>
        public static void ResolveOptions(LoginCalendarCell cell, LoginCalendarUnlocks unlocks, LoginCalendarReward[] into)
        {
            if (cell == null)
            {
                throw new ArgumentNullException(nameof(cell));
            }

            if (into == null || into.Length < LoginCalendarTable.Choices)
            {
                throw new ArgumentException("Hacen falta 3 huecos.", nameof(into));
            }

            for (int i = 0; i < LoginCalendarTable.Choices; i++)
            {
                LoginCalendarReward option = cell.Option((LoginCalendarChoice)i);
                LoginCalendarReward resolved = Substitute(option, unlocks, 0);
                if (HasLocked(option, unlocks))
                {
                    // Como mucho tantos pasos como la cadena más larga (Luciérnaga: 3).
                    for (int step = 1; step <= 3 && CollidesWithEarlier(resolved, into, i); step++)
                    {
                        resolved = Substitute(option, unlocks, step);
                    }
                }

                into[i] = resolved;
            }
        }

        /// <summary>La opción <paramref name="choice"/> de <paramref name="cell"/> para el jugador (<see cref="ResolveOptions"/>).</summary>
        public static LoginCalendarReward ResolveOption(LoginCalendarCell cell, LoginCalendarChoice choice, LoginCalendarUnlocks unlocks)
        {
            var three = new LoginCalendarReward[LoginCalendarTable.Choices];
            ResolveOptions(cell, unlocks, three);
            return three[(int)choice];
        }

        /// <summary>
        /// <c>reward_type</c> de <c>af_calendar_claim</c> [P] (Q-B8): <c>seeds</c> si el premio recibido solo trae semillas,
        /// <c>booster</c> si solo trae comodines y <c>mix</c> si trae las dos cosas. La opción tocada va en <c>choice</c>.
        /// </summary>
        public static string RewardType(LoginCalendarReward reward)
        {
            if (reward.Stock.IsZero)
            {
                return "seeds";
            }

            return reward.Seeds > 0 ? "mix" : "booster";
        }

        /// <summary>Id del marco de país del mundo <paramref name="world"/> (1-9) [P]: <c>frame_w01</c>…</summary>
        public static string FrameId(int world)
        {
            if (world < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(world));
            }

            return FramePrefix + world.ToString("00", CultureInfo.InvariantCulture);
        }

        // La opción con cada unidad bloqueada sustituida; extraSteps salta más candidatos desbloqueados de su cadena.
        private static LoginCalendarReward Substitute(LoginCalendarReward option, LoginCalendarUnlocks unlocks, int extraSteps)
        {
            int seeds = option.Seeds;
            StockDelta stock = StockDelta.Of(BoosterKind.Undo, option.Stock.Get(BoosterKind.Undo));
            for (int k = 0; k < BoosterKinds.Count; k++)
            {
                var kind = (BoosterKind)k;
                if (kind == BoosterKind.Undo)
                {
                    continue;
                }

                int units = option.Stock.Get(kind);
                if (units == 0)
                {
                    continue;
                }

                if (unlocks.IsUnlocked(kind))
                {
                    stock = stock.Plus(kind, units);
                    continue;
                }

                BoosterKind[] chain = Chain(kind);
                int skip = extraSteps;
                bool placed = false;
                for (int c = 0; c < chain.Length; c++)
                {
                    if (!unlocks.IsUnlocked(chain[c]))
                    {
                        continue;
                    }

                    if (skip > 0)
                    {
                        skip--;
                        continue;
                    }

                    stock = stock.Plus(chain[c], units);
                    placed = true;
                    break;
                }

                if (!placed)
                {
                    stock = stock.Plus(BoosterKind.Undo, TerminalUndo * units);
                    seeds = checked(seeds + (TerminalSeeds * units));
                }
            }

            return new LoginCalendarReward(seeds, stock);
        }

        private static bool HasLocked(LoginCalendarReward option, LoginCalendarUnlocks unlocks)
        {
            for (int k = 0; k < BoosterKinds.Count; k++)
            {
                var kind = (BoosterKind)k;
                if (option.Stock.Get(kind) > 0 && !unlocks.IsUnlocked(kind))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CollidesWithEarlier(LoginCalendarReward reward, LoginCalendarReward[] resolved, int count)
        {
            for (int j = 0; j < count; j++)
            {
                if (resolved[j].Equals(reward))
                {
                    return true;
                }
            }

            return false;
        }

        private static BoosterKind[] Chain(BoosterKind kind)
        {
            switch (kind)
            {
                case BoosterKind.Hint: return ChainHint;
                case BoosterKind.Reveal: return ChainReveal;
                default: return ChainNone; // Cuia extra → «2 Marea + 40»
            }
        }
    }
}
