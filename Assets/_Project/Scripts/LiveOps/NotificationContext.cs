#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;

namespace AF.LiveOps
{
    /// <summary>Cómo se escribe un marcador de un aviso.</summary>
    public enum NotificationArgKind
    {
        /// <summary>Cifra con el formato del idioma (<c>Loc.Number</c>: semillas, victorias).</summary>
        Number = 0,

        /// <summary>Número sin separador de miles (un nivel).</summary>
        Digits = 1,

        /// <summary>Texto ya resuelto de la tabla (el nombre de un mundo, un premio).</summary>
        Text = 2,
    }

    /// <summary>Un marcador con nombre de un aviso (<c>.claude/rules/localizacion.md</c>: nunca posicionales).</summary>
    public readonly struct NotificationArg : IEquatable<NotificationArg>
    {
        private NotificationArg(string name, NotificationArgKind kind, long integer, string? text)
        {
            Name = name;
            Kind = kind;
            Integer = integer;
            Text = text;
        }

        /// <summary>Nombre del marcador, sin llaves.</summary>
        public string Name { get; }

        /// <summary>Cómo se escribe.</summary>
        public NotificationArgKind Kind { get; }

        /// <summary>Valor numérico (<see cref="NotificationArgKind.Number"/> y <see cref="NotificationArgKind.Digits"/>).</summary>
        public long Integer { get; }

        /// <summary>Texto (<see cref="NotificationArgKind.Text"/>).</summary>
        public string? Text { get; }

        /// <summary>Cifra con formato del idioma.</summary>
        public static NotificationArg Number(string name, long value) => new NotificationArg(name, NotificationArgKind.Number, value, null);

        /// <summary>Número sin separadores.</summary>
        public static NotificationArg Digits(string name, long value) => new NotificationArg(name, NotificationArgKind.Digits, value, null);

        /// <summary>Texto ya resuelto.</summary>
        public static NotificationArg Of(string name, string text) => new NotificationArg(name, NotificationArgKind.Text, 0, text);

        /// <inheritdoc />
        public bool Equals(NotificationArg other)
        {
            return Name == other.Name && Kind == other.Kind && Integer == other.Integer && Text == other.Text;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is NotificationArg other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ((int)Kind * 397) ^ Integer.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name + "=" + (Kind == NotificationArgKind.Text ? Text : Integer.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Un aviso del plan, aún sin texto (<see cref="NotificationPlanner"/>).</summary>
    public sealed class PlannedNotification
    {
        internal PlannedNotification(
            NotificationType type, string bodyKey, int dayIndex, DateTime fireAtUtc, DateTime fireAtLocal, IReadOnlyList<NotificationArg> args)
        {
            Type = type;
            BodyKey = bodyKey;
            DayIndex = dayIndex;
            FireAtUtc = fireAtUtc;
            FireAtLocal = fireAtLocal;
            Args = args;
        }

        /// <summary>Tipo.</summary>
        public NotificationType Type { get; }

        /// <summary>Días locales desde hoy (0 = hoy).</summary>
        public int DayIndex { get; }

        /// <summary>Identificador estable: <c>tipo × 1000 + día</c>.</summary>
        public int Id => ((int)Type + 1) * 1000 + DayIndex;

        /// <summary>Hora de entrega, UTC.</summary>
        public DateTime FireAtUtc { get; }

        /// <summary>Hora de entrega, local del dispositivo (con el desfase del momento de planificar).</summary>
        public DateTime FireAtLocal { get; }

        /// <summary>Clave del texto (la del tipo; el Regreso sin nombre de mundo usa la del D7 [P]).</summary>
        public string BodyKey { get; }

        /// <summary>Marcadores (como mucho 3, <c>LocArgs.MaxCount</c>).</summary>
        public IReadOnlyList<NotificationArg> Args { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Type.ToId() + "@" + FireAtLocal.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// El estado del jugador al pasar a segundo plano, que es todo lo que el planificador necesita (06 §8.2, «Estado real»: cada
    /// aviso nombra algo que existe ahora). Lo rellena el arranque con los servicios de cada sistema; los que aún no existen
    /// (la Bitácora, B4) quedan apagados y no generan avisos. Las cifras ya vienen de su fuente única (ver <see cref="PushConfig"/>).
    /// </summary>
    public sealed class NotificationContext
    {
        /// <summary>Ahora, UTC.</summary>
        public DateTime NowUtc { get; set; }

        /// <summary>Desfase local del dispositivo (<c>IClock.LocalOffset</c>); se toma fijo para todo el plan [P].</summary>
        public TimeSpan LocalOffset { get; set; }

        /// <summary>El Reto del río está desbloqueado (<c>live_unlock_level_daily</c>, kill switch y pool).</summary>
        public bool DailyUnlocked { get; set; }

        /// <summary>Hoy ya se jugó el Reto (empezado, pendiente o sellado: la tarjeta no dice «Jugar»).</summary>
        public bool DailyPlayedToday { get; set; }

        /// <summary>Racha de Retos ganados seguidos (<c>daily_challenge.streak_days</c>).</summary>
        public int DailyStreakDays { get; set; }

        /// <summary>Último día ganado del Reto (<c>yyyy-MM-dd</c>, <c>daily_challenge.last_claimed_date</c>), o null.</summary>
        public string? DailyLastWinDate { get; set; }

        /// <summary>La Bitácora existe y está desbloqueada (B4; hasta entonces, false).</summary>
        public bool LogbookUnlocked { get; set; }

        /// <summary>Hoy ya se selló la Bitácora.</summary>
        public bool LogbookClaimedToday { get; set; }

        /// <summary>El premio de la próxima casilla, ya escrito (marcador <c>{reward}</c>); null = sin aviso de Bitácora.</summary>
        public string? LogbookReward { get; set; }

        /// <summary>La Crecida está activa (desde <c>streak_unlock_level</c> y con su kill switch).</summary>
        public bool StreakActive { get; set; }

        /// <summary><c>streak.wins</c>.</summary>
        public int StreakWins { get; set; }

        /// <summary>Hora de la última victoria (<c>stats.last_win_at</c> [P]), UTC; null si no se sabe.</summary>
        public DateTime? LastWinAt { get; set; }

        /// <summary><c>streak_reminder_hours</c> (36).</summary>
        public int StreakReminderHours { get; set; } = 36;

        /// <summary>Las canoas están en juego (desde <c>rescue_min_level</c>).</summary>
        public bool CanoesActive { get; set; }

        /// <summary>Estado de las canoas al salir.</summary>
        public CanoesStatus Canoes { get; set; }

        /// <summary><c>canoes_regen_min</c> como intervalo.</summary>
        public TimeSpan CanoesRegen { get; set; } = TimeSpan.FromMinutes(25);

        /// <summary>El jugador alcanzó el último nivel publicado.</summary>
        public bool ContentEnd { get; set; }

        /// <summary><c>live_content_end_message.next_batch_eta_days</c> (14).</summary>
        public int NextBatchEtaDays { get; set; } = 14;

        /// <summary>Nombre del mundo río arriba, ya escrito (marcador <c>{name}</c>); null = sin aviso de Nuevos niveles.</summary>
        public string? NextWorldName { get; set; }

        /// <summary>El nivel en que lo dejó (<c>max_level</c> + 1; marcador <c>{level}</c> del Regreso).</summary>
        public int CurrentLevel { get; set; } = 1;

        /// <summary>Nombre del mundo en que está, ya escrito (marcador <c>{world}</c> del Regreso D14, D30 y D60), o null.</summary>
        public string? CurrentWorldName { get; set; }

        /// <summary>Semillas del Regalo de regreso (<c>live_comeback_gift.seeds</c>; marcador <c>{seeds}</c>).</summary>
        public int ComebackSeeds { get; set; } = 200;

        /// <summary>
        /// Hora UTC en que las canoas llegan a su tope si se sale ahora, o null si ya están llenas, son infinitas o no se regeneran:
        /// el siguiente +1 (<see cref="CanoesStatus.NextRegenAt"/>) más un intervalo por cada canoa que falte después.
        /// </summary>
        public DateTime? CanoesFullAtUtc()
        {
            CanoesStatus c = Canoes;
            if (c.IsFull || c.IsInfinite || !c.NextRegenAt.HasValue || CanoesRegen <= TimeSpan.Zero)
            {
                return null;
            }

            int missingAfterNext = Math.Max(0, c.Max - c.Count - 1);
            return c.NextRegenAt.Value + TimeSpan.FromTicks(CanoesRegen.Ticks * missingAfterNext);
        }
    }
}
