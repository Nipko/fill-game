#nullable enable
using System;
using System.Collections.Generic;

namespace AF.LiveOps
{
    /// <summary>
    /// Los avisos locales de la 1.0 (06 §8.3 con la lista de 06 §11, fila 1.0: Reto listo, Crecida te espera, Bitácora sin sellar,
    /// Canoas llenas, Nuevos niveles y Regreso D7/D14/D30), más «Racha de Retos» como variante de «Reto listo» y el Regreso D60 de
    /// 06 §9, fila «Churn» (preguntas B6-Q4 y B6-Q5 de la nota). Los de la 1.2 y la 2.0 (evento, liga, premio por reclamar, canoa
    /// prestada) no están. El orden de los valores no es la prioridad: esa es <see cref="NotificationTypes.Priority"/>.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>Reto listo: 09:30 local, si no jugó hoy.</summary>
        DailyReady = 0,

        /// <summary>Racha de Retos: el Reto listo del día 7 de una racha (día 6 de 7 ganado) [P].</summary>
        DailyStreak = 1,

        /// <summary>Bitácora sin sellar: 20:00 local, si hay casilla pendiente.</summary>
        LogbookPending = 2,

        /// <summary>Crecida te espera: <c>streak_reminder_hours</c> (36) sin victoria, con racha ≥ 3.</summary>
        StreakWaiting = 3,

        /// <summary>Canoas llenas: solo si salió con 0 canoas, al llegar a 5/5.</summary>
        CanoesFull = 4,

        /// <summary>Nuevos niveles: a quien terminó el contenido, a los <c>next_batch_eta_days</c>.</summary>
        NewLevels = 5,

        /// <summary>Regreso D7.</summary>
        ComebackD7 = 6,

        /// <summary>Regreso D14 (variante del D7 con el nombre del mundo).</summary>
        ComebackD14 = 7,

        /// <summary>Regreso D30.</summary>
        ComebackD30 = 8,

        /// <summary>Regreso D60 (06 §9, fila «Churn») [P].</summary>
        ComebackD60 = 9,
    }

    /// <summary>Identificadores, claves de texto y prioridad de <see cref="NotificationType"/>.</summary>
    public static class NotificationTypes
    {
        /// <summary>Número de tipos.</summary>
        public const int Count = 10;

        /// <summary>Clave del título de todos los avisos (TiTino firma los avisos, D-013; 06 §8.3) [P].</summary>
        public const string TitleKey = "push_title";

        // type de af_push_sent / af_push_open (08 §8.2: «tipo del catálogo de 06 §8.3»; los nombres son [P]).
        private static readonly string[] Ids =
        {
            "daily_ready", "daily_streak", "logbook_pending", "streak_waiting", "canoes_full", "new_levels", "comeback_d7",
            "comeback_d14", "comeback_d30", "comeback_d60",
        };

        private static readonly string[] BodyKeys =
        {
            "push_daily_ready", "push_daily_streak", "push_logbook_pending", "push_streak_waiting", "push_canoes_full",
            "push_new_levels", "push_comeback_d7", "push_comeback_d14", "push_comeback_d30", "push_comeback_d60",
        };

        // 06 §8.2, «Prioridad si coinciden»: premio > evento > Reto listo > Bitácora > Crecida te espera > canoas llenas > nuevos
        // niveles, aplicada dentro de cada grupo. El Regreso no figura: va el primero [P] (es el aviso del regalo de 06 §9). Los
        // puntuales (Crecida, canoas, nuevos niveles) ganan a los diarios (Reto listo, Racha, Bitácora) el día que caen [P]
        // (B6-Q9 (b)): con el Reto diario cada día, si no, no saldrían nunca. Mayor = antes.
        private static readonly int[] Priorities = { 30, 30, 20, 60, 50, 40, 100, 100, 100, 100 };

        /// <summary>Todos los tipos, en orden.</summary>
        public static IReadOnlyList<NotificationType> All { get; } = Array.AsReadOnly(new[]
        {
            NotificationType.DailyReady, NotificationType.DailyStreak, NotificationType.LogbookPending, NotificationType.StreakWaiting,
            NotificationType.CanoesFull, NotificationType.NewLevels, NotificationType.ComebackD7, NotificationType.ComebackD14,
            NotificationType.ComebackD30, NotificationType.ComebackD60,
        });

        /// <summary>Valor de <c>type</c> en <c>af_push_sent</c> y <c>af_push_open</c>.</summary>
        public static string ToId(this NotificationType type)
        {
            return Ids[Index(type)];
        }

        /// <summary>Clave del texto del aviso.</summary>
        public static string BodyKey(this NotificationType type)
        {
            return BodyKeys[Index(type)];
        }

        /// <summary>Prioridad (mayor = gana cuando dos coinciden el mismo día).</summary>
        public static int Priority(this NotificationType type)
        {
            return Priorities[Index(type)];
        }

        /// <summary>
        /// Es un aviso puntual (Crecida te espera, Canoas llenas, Nuevos niveles): cae una vez a su hora y no espera al día alterno
        /// sin abrir [P].
        /// </summary>
        public static bool IsOneOff(this NotificationType type)
        {
            return type == NotificationType.StreakWaiting || type == NotificationType.CanoesFull || type == NotificationType.NewLevels;
        }

        /// <summary>Es un aviso de Regreso (06 §9): no se para a los 30 días y gana a los demás.</summary>
        public static bool IsComeback(this NotificationType type)
        {
            return type == NotificationType.ComebackD7 || type == NotificationType.ComebackD14
                || type == NotificationType.ComebackD30 || type == NotificationType.ComebackD60;
        }

        /// <summary>Lee un <c>type</c> de analítica; false si no es ninguno.</summary>
        public static bool TryParse(string? id, out NotificationType type)
        {
            for (int i = 0; i < Ids.Length; i++)
            {
                if (string.Equals(Ids[i], id, StringComparison.Ordinal))
                {
                    type = (NotificationType)i;
                    return true;
                }
            }

            type = NotificationType.DailyReady;
            return false;
        }

        private static int Index(NotificationType type)
        {
            int i = (int)type;
            if (i < 0 || i >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return i;
        }
    }
}
