#nullable enable
using System;
using AF.Economy;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>Qué pre-prompt toca en una victoria.</summary>
    public enum PrePromptKind
    {
        /// <summary>Ninguno.</summary>
        None = 0,

        /// <summary>El primero: primera victoria de la sesión <c>push_preprompt_session</c> con el nivel 10 ya superado (06 §8.1, regla 1).</summary>
        First = 1,

        /// <summary>El segundo y último, tras un rechazo: hito 3 de la Crecida o 7 días, lo que ocurra primero (06 §8.1, regla 2).</summary>
        Second = 2,
    }

    /// <summary>Lo que se sabe en el momento de una victoria (campaña o Reto del río).</summary>
    public readonly struct PrePromptMoment
    {
        /// <summary>Crea el momento.</summary>
        /// <param name="sessions">Sesión en curso (<c>stats.sessions</c>).</param>
        /// <param name="maxLevelBeforeVictory">
        /// <c>max_level</c> antes de esta victoria: en campaña, el nivel ganado − 1; en el Reto, <c>max_level</c> (el Reto no lo
        /// cambia). Así la victoria del nivel 10 nunca lleva el pre-prompt (la secuencia del nivel 10, 06 §2.3).
        /// </param>
        /// <param name="streakWins">La Crecida tras la victoria (<c>streak.wins</c>).</param>
        /// <param name="nowUtc">Hora de la victoria.</param>
        public PrePromptMoment(int sessions, int maxLevelBeforeVictory, int streakWins, DateTime nowUtc)
        {
            Sessions = sessions;
            MaxLevelBeforeVictory = maxLevelBeforeVictory;
            StreakWins = streakWins;
            NowUtc = nowUtc;
        }

        /// <summary>Sesión en curso.</summary>
        public int Sessions { get; }

        /// <summary><c>max_level</c> antes de la victoria.</summary>
        public int MaxLevelBeforeVictory { get; }

        /// <summary>Crecida tras la victoria.</summary>
        public int StreakWins { get; }

        /// <summary>Hora, UTC.</summary>
        public DateTime NowUtc { get; }
    }

    /// <summary>
    /// Cuándo sale el pre-prompt propio de push (06 §8.1; 08 §8.2 <c>af_push_prompt</c>; 09 #29). Reglas puras:
    /// <list type="number">
    /// <item><b>Primero:</b> nunca en el arranque en frío; en la primera victoria de la sesión <c>push_preprompt_session</c> (2) o
    /// posterior con <c>push_preprompt_min_level</c> (10) ya superado antes de esa victoria. Si la 2.ª sesión empieza antes del 10,
    /// se espera a la primera victoria posterior al 10; la del 10 nunca (su secuencia, 06 §2.3).</item>
    /// <item><b>Segundo y último</b> (si el primero no acabó en avisos): en la primera victoria con la Crecida en 3 o más (el hito 3)
    /// o a partir de 7 días del primero, lo que ocurra antes [P: también en una victoria].</item>
    /// <item>Después, solo el acceso de Ajustes (<see cref="PushPermissionService.SetFromSettings"/>).</item>
    /// </list>
    /// Con los avisos ya concedidos, o bloqueados en el teléfono (el diálogo nativo ya no saldría), no hay pre-prompt [P].
    /// </summary>
    public static class PushPrePromptRules
    {
        /// <summary>Días desde el primer pre-prompt para el segundo (06 §8.1: «a los 7 días»; no es de 08 §9).</summary>
        public const int SecondAfterDays = 7;

        /// <summary>El hito de la Crecida que adelanta el segundo pre-prompt (06 §8.1: «el hito 3»).</summary>
        public const int SecondAtStreakWins = StreakRecord.HitSeeds;

        /// <summary>El pre-prompt que toca en <paramref name="moment"/>.</summary>
        public static PrePromptKind Due(PushSettingsRecord record, PushConfig config, PushPermission system, in PrePromptMoment moment)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (record.Granted || system == PushPermission.Blocked)
            {
                return PrePromptKind.None;
            }

            if (!record.PrePromptShown)
            {
                return moment.Sessions >= config.PushPrepromptSession && moment.MaxLevelBeforeVictory >= config.PushPrepromptMinLevel
                    ? PrePromptKind.First
                    : PrePromptKind.None;
            }

            if (record.SecondPromptShown)
            {
                return PrePromptKind.None;
            }

            if (moment.StreakWins >= SecondAtStreakWins)
            {
                return PrePromptKind.Second;
            }

            // Sin la hora del primero (perfil raro), solo el hito 3 lo adelanta. Un reloj atrasado no adelanta nada.
            return record.PrePromptAt.HasValue && moment.NowUtc - record.PrePromptAt.Value >= TimeSpan.FromDays(SecondAfterDays)
                ? PrePromptKind.Second
                : PrePromptKind.None;
        }
    }
}
