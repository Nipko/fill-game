#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AF.Economy
{
    /// <summary>Qué evento se resolvió.</summary>
    public enum StreakEvent
    {
        /// <summary>Ninguno.</summary>
        None = 0,

        /// <summary>Victoria.</summary>
        Victory = 1,

        /// <summary>Fallo duro (04 §5.2).</summary>
        HardFail = 2,
    }

    /// <summary>Por qué la Crecida no contó el evento.</summary>
    public enum StreakIgnore
    {
        /// <summary>Contó.</summary>
        None = 0,

        /// <summary>No es un nivel de campaña (Reto del río, eventos; 06 §4.3; Q-C2).</summary>
        NotCampaign = 1,

        /// <summary><c>biz_feature_win_streak</c> apagado (Q-C11).</summary>
        Disabled = 2,

        /// <summary>Nivel por debajo de <c>streak_unlock_level</c> (05 §2.4).</summary>
        Locked = 3,
    }

    /// <summary>
    /// Resultado de <see cref="StreakRules.OnVictory"/> o <see cref="StreakRules.OnHardFail"/>. Es un valor: devolverlo no asigna
    /// memoria. El estado ya está cambiado; lo que hay que pagar se pide con <see cref="AppendTransactions"/>.
    /// </summary>
    public struct StreakOutcome
    {
        /// <summary>Evento.</summary>
        public StreakEvent Event { get; internal set; }

        /// <summary>Por qué no contó (<see cref="StreakIgnore.None"/> si contó).</summary>
        public StreakIgnore Ignore { get; internal set; }

        /// <summary>La Crecida contó el evento.</summary>
        public bool Counted => Event != StreakEvent.None && Ignore == StreakIgnore.None;

        /// <summary>Nivel del evento.</summary>
        public int Level { get; internal set; }

        /// <summary>Barra antes del evento.</summary>
        public int WinsBefore { get; internal set; }

        /// <summary>Barra después (<c>streak.wins</c>).</summary>
        public int Wins { get; internal set; }

        /// <summary>Río crecido antes del evento.</summary>
        public bool RiverFullBefore { get; internal set; }

        /// <summary>Río crecido después.</summary>
        public bool RiverFull { get; internal set; }

        /// <summary>
        /// Victoria con río crecido <b>antes</b> de ella: la Cosecha se dobla sin anuncio (04 §2.8; Q-C1 (a)) y el botón x2 por
        /// vídeo no se ofrece (Q-C9 (a)). La victoria que paga el hito 10 no lo lleva.
        /// </summary>
        public bool FreeDoubleHarvest { get; internal set; }

        /// <summary>Hito alcanzado por la barra en esta victoria (3, 6 o 10), pagado o no; 0 si ninguno.</summary>
        public int Milestone { get; internal set; }

        /// <summary>El hito se pagó (no estaba ya pagado hoy con <c>streak_hits_once_per_day</c>).</summary>
        public bool MilestonePaid { get; internal set; }

        /// <summary>Semillas pagadas por el hito 3 (tras el tope).</summary>
        public int HitSeeds { get; internal set; }

        /// <summary>Semillas que el tope diario dejó sin pagar (hito 3 o Canasta del hito 6).</summary>
        public int CappedSeeds { get; internal set; }

        /// <summary>Canasta del hito 6 pagada, con sus semillas ya recortadas por el tope; <see cref="BasketContents.None"/> si no.</summary>
        public BasketContents Basket { get; internal set; }

        /// <summary>El hito 10 da 1 Cuia extra al stock.</summary>
        public bool ExtraCuia { get; internal set; }

        /// <summary>La barra llegó a 10 y volvió a 0 (con o sin Cuia extra).</summary>
        public bool BarEmptied { get; internal set; }

        /// <summary>Fallo duro cubierto por la Hoja escudo (<c>af_streak_shield_used</c>, <c>shield_used</c> de <c>af_level_fail</c>).</summary>
        public bool ShieldUsed { get; internal set; }

        /// <summary>El fallo duro vació una barra ≥ 1.</summary>
        public bool StreakLost { get; internal set; }

        /// <summary>El fallo duro apagó el río crecido.</summary>
        public bool RiverEnded { get; internal set; }

        /// <summary><c>streak_broken</c> de <c>af_level_fail</c>: se perdió la barra o el río (Q-C7 (a)).</summary>
        public bool StreakBroken => StreakLost || RiverEnded;

        /// <summary>Semillas de la Crecida pagadas en este evento (hito 3 + Canasta del hito 6).</summary>
        public int SeedsPaid => HitSeeds + Basket.Seeds;

        /// <summary>Semillas de la Crecida pagadas hoy tras el evento.</summary>
        public int SeedsToday { get; internal set; }

        /// <summary><c>reward_type</c> de <c>af_streak_milestone</c> (08 §8.2): <c>seeds</c>, <c>chest</c> o <c>extra_cuia+rio_crecido</c>; null sin hito pagado.</summary>
        public string? MilestoneRewardType
        {
            get
            {
                if (!MilestonePaid)
                {
                    return null;
                }

                switch (Milestone)
                {
                    case 3: return "seeds";
                    case 6: return StreakMilestones.Hit6Value;
                    case 10: return StreakMilestones.Hit10Value;
                    default: return null;
                }
            }
        }

        /// <summary><c>reward_value</c> de <c>af_streak_milestone</c>: las semillas pagadas, el <c>contents_json</c> de la Canasta o <c>1</c>.</summary>
        public string? MilestoneRewardValue
        {
            get
            {
                if (!MilestonePaid)
                {
                    return null;
                }

                switch (Milestone)
                {
                    case 3: return HitSeeds.ToString(CultureInfo.InvariantCulture);
                    case 6: return Basket.ContentsJson();
                    case 10: return ExtraCuia ? "1" : "0";
                    default: return null;
                }
            }
        }

        /// <summary>
        /// Añade a <paramref name="into"/> lo que la cartera debe aplicar, en orden: la Cosecha doble del río crecido
        /// (<see cref="Transaction.DoubleHarvest"/> con la etiqueta <paramref name="winTier"/> de la victoria), las semillas del
        /// hito 3 (<see cref="SeedSource.Streak"/>), la Canasta del hito 6 (sus semillas también con <see cref="SeedSource.Streak"/>,
        /// porque cuentan en el tope: Q-C13) y la Cuia extra del hito 10. Nada en un fallo duro ni en un evento que no contó.
        /// </summary>
        public void AppendTransactions(List<Transaction> into, RewardTier winTier)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            if (!Counted || Event != StreakEvent.Victory)
            {
                return;
            }

            if (FreeDoubleHarvest)
            {
                into.Add(Transaction.DoubleHarvest(Level, winTier));
            }

            if (HitSeeds > 0)
            {
                into.Add(Transaction.Earn(SeedSource.Streak, HitSeeds));
            }

            Basket.AppendTransactions(into, SeedSource.Streak);
            if (ExtraCuia)
            {
                into.Add(Transaction.GrantStock(BoosterKind.ExtraCuia, 1));
            }
        }
    }

    /// <summary>
    /// Reglas de la Crecida y la Hoja escudo (06 §4.3, con su pseudocódigo; 05 §2.4; 04 §6.2 <c>failHard</c> y §8; la escalera de
    /// <c>tools/econ_sim.py</c>, <c>on_win</c>/<c>on_fail</c>). Sin motor, sin reloj y sin azar: el día local lo da quien llama
    /// (<see cref="RollDay"/>, con <c>IClock.LocalDateKey()</c>), antes de cada evento. <see cref="OnVictory"/> y
    /// <see cref="OnHardFail"/> no asignan memoria.
    /// </summary>
    public sealed class StreakRules
    {
        /// <summary>Crea las reglas sobre <paramref name="record"/> (el estado de la Crecida, que cambia en su sitio).</summary>
        public StreakRules(StreakRecord record, StreakConfig? config = null, BasketConfig? basket = null)
        {
            Record = record ?? throw new ArgumentNullException(nameof(record));
            Config = config ?? StreakConfig.Defaults;
            Basket = basket ?? BasketConfig.Defaults;
        }

        /// <summary>El estado (<see cref="StreakRecord"/>: el mismo que guarda <see cref="IStreakStore"/>).</summary>
        public StreakRecord Record { get; }

        /// <summary>Cifras de la Crecida.</summary>
        public StreakConfig Config { get; }

        /// <summary>Cifras de la Canasta (para la del hito 6).</summary>
        public BasketConfig Basket { get; }

        /// <summary>
        /// Cambio de día local (06 §4.3 <c>on_app_open</c>; 05 §2.4 <c>reset_daily</c>): con un día posterior al guardado (o sin
        /// día), Hoja escudo disponible (una sola, aunque <c>streak_shield_per_day</c> valga 2: Q-C6 (a)), hitos pagados y
        /// semillas de hoy a 0. La barra y el río crecido no se tocan: no expiran por reloj (04 §8). El día solo avanza: con un
        /// día anterior (reloj atrasado) no se repone nada. Devuelve si cambió algo.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="todayKey"/> vacío.</exception>
        public bool RollDay(string todayKey)
        {
            if (string.IsNullOrEmpty(todayKey))
            {
                throw new ArgumentException("Falta el día local (yyyy-MM-dd).", nameof(todayKey));
            }

            // Una sola hoja aunque streak_shield_per_day valga 2 (Q-C6 (a)); el rango 1-2 de 08 §9 nunca da 0.
            return Record.RollOver(todayKey);
        }

        /// <summary>
        /// La Crecida cuenta en este nivel: campaña, <c>biz_feature_win_streak</c> encendido y nivel ≥ <c>streak_unlock_level</c>
        /// (para la barra del HUD: sin ella, ni barra ni x2 del río).
        /// </summary>
        public bool IsActive(int level, bool isCampaign)
        {
            return Check(level, isCampaign) == StreakIgnore.None;
        }

        /// <summary>
        /// La próxima victoria en este nivel llevaría la Cosecha doble sin anuncio (río crecido y Crecida activa): el botón x2
        /// por vídeo no se ofrece (Q-C9 (a)).
        /// </summary>
        public bool FreeDoubleHarvestOn(int level, bool isCampaign)
        {
            return Record.RiverFull && IsActive(level, isCampaign);
        }

        /// <summary>
        /// Victoria en el nivel <paramref name="level"/> (06 §4.3 <c>on_level_end</c>; 05 §2.4 <c>on_victory</c>): decide el x2
        /// del río crecido con el estado anterior, suma 1 a la barra y paga el hito 3 (300), el 6 (Canasta,
        /// <see cref="BasketRules.HitSix"/>) o el 10 (Cuia extra + río crecido; la barra vuelve a 0), cada uno una vez por día
        /// local con <c>streak_hits_once_per_day</c>, y con el tope <c>streak_daily_cap</c> solo sobre las semillas del 3 y de
        /// la Canasta del 6 (pago parcial: Q-C10 (a)). Fuera de campaña, con la Crecida apagada o por debajo del debut, no hace
        /// nada.
        /// </summary>
        public StreakOutcome OnVictory(int level, bool isCampaign)
        {
            StreakRecord s = Record;
            var o = new StreakOutcome
            {
                Event = StreakEvent.Victory, Level = RequireLevel(level), WinsBefore = s.Wins, RiverFullBefore = s.RiverFull,
                Wins = s.Wins, RiverFull = s.RiverFull, SeedsToday = s.SeedsToday,
            };
            o.Ignore = Check(level, isCampaign);
            if (o.Ignore != StreakIgnore.None)
            {
                return o;
            }

            o.FreeDoubleHarvest = s.RiverFull;
            s.Wins += 1;
            if (s.Wins == 3)
            {
                o.Milestone = 3;
                if (Payable(3))
                {
                    int paid = PayCapped(Config.StreakMilestones.Hit3Seeds, ref o);
                    o.HitSeeds = paid;
                    MarkPaid(3, ref o);
                }
            }
            else if (s.Wins == 6)
            {
                o.Milestone = 6;
                if (Payable(6))
                {
                    BasketContents basket = BasketRules.HitSix(level, Basket);
                    int paid = PayCapped(basket.Seeds, ref o);
                    o.Basket = basket.WithSeeds(paid);
                    MarkPaid(6, ref o);
                }
            }
            else if (s.Wins >= 10)
            {
                o.Milestone = 10;
                if (Payable(10))
                {
                    o.ExtraCuia = true;
                    MarkPaid(10, ref o);
                }

                s.RiverFull = true;
                s.Wins = 0;
                o.BarEmptied = true;
            }

            o.Wins = s.Wins;
            o.RiverFull = s.RiverFull;
            o.SeedsToday = s.SeedsToday;
            return o;
        }

        /// <summary>
        /// Fallo duro en el nivel <paramref name="level"/> (04 §5.2 con D-008; 04 §6.2 <c>failHard</c>; 05 §2.4
        /// <c>on_hard_fail</c>): con la barra ≥ 1 y la Hoja escudo disponible, se gasta la hoja y nada más; si no, la barra
        /// vuelve a 0 y el río crecido se apaga (con la barra a 0, el fallo apaga el río sin gastar la hoja). El fallo blando
        /// no llega aquí. Fuera de campaña, con la Crecida apagada o por debajo del debut, no hace nada.
        /// </summary>
        public StreakOutcome OnHardFail(int level, bool isCampaign)
        {
            StreakRecord s = Record;
            var o = new StreakOutcome
            {
                Event = StreakEvent.HardFail, Level = RequireLevel(level), WinsBefore = s.Wins, RiverFullBefore = s.RiverFull,
                Wins = s.Wins, RiverFull = s.RiverFull, SeedsToday = s.SeedsToday,
            };
            o.Ignore = Check(level, isCampaign);
            if (o.Ignore != StreakIgnore.None)
            {
                return o;
            }

            if (s.Wins >= 1 && s.ShieldAvailable)
            {
                s.ShieldAvailable = false;
                o.ShieldUsed = true;
            }
            else
            {
                o.StreakLost = s.Wins >= 1;
                o.RiverEnded = s.RiverFull;
                s.Wins = 0;
                s.RiverFull = false;
            }

            o.Wins = s.Wins;
            o.RiverFull = s.RiverFull;
            return o;
        }

        private StreakIgnore Check(int level, bool isCampaign)
        {
            if (!isCampaign)
            {
                return StreakIgnore.NotCampaign;
            }

            if (!Config.BizFeatureWinStreak)
            {
                return StreakIgnore.Disabled;
            }

            return level < Config.StreakUnlockLevel ? StreakIgnore.Locked : StreakIgnore.None;
        }

        private bool Payable(int milestone)
        {
            return !(Config.StreakHitsOncePerDay && Record.HitsToday.Contains(milestone));
        }

        private void MarkPaid(int milestone, ref StreakOutcome o)
        {
            if (!Record.HitsToday.Contains(milestone))
            {
                Record.HitsToday.Add(milestone);
            }
            o.MilestonePaid = true;
        }

        /// <summary>Paga hasta el hueco que deja el tope diario (<c>pay_streak_hit</c> de <c>econ_sim.py</c>) y devuelve lo pagado.</summary>
        private int PayCapped(int amount, ref StreakOutcome o)
        {
            int room = Math.Max(0, Config.StreakDailyCap - Record.SeedsToday);
            int paid = Math.Max(0, Math.Min(amount, room));
            Record.SeedsToday += paid;
            o.CappedSeeds += amount - paid;
            return paid;
        }

        private static int RequireLevel(int level)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "Los niveles empiezan en 1.");
            }

            return level;
        }
    }
}
