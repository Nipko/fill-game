#nullable enable
using System;

namespace AF.LiveOps
{
    /// <summary>Segmento de re-engagement de 06 §9 según los días sin sesión.</summary>
    public enum ComebackSegment
    {
        /// <summary>Menos de 3 días fuera: nada.</summary>
        None = 0,

        /// <summary>«En riesgo» (3 días sin sesión): nada especial al volver (06 §9).</summary>
        AtRisk = 1,

        /// <summary>«Dormido» (7-13 días): Regalo de regreso.</summary>
        Dormant = 2,

        /// <summary>«Perdido» (14-29 días): Regalo de regreso + recordatorio de una frase + etiqueta honesta en el siguiente nivel.</summary>
        Lost = 3,

        /// <summary>«Churn» (30 días o más): igual que «Perdido».</summary>
        Churn = 4,
    }

    /// <summary>
    /// Reglas puras del Regalo de regreso (06 §9). Los umbrales 3, 14 y 30 son los de la tabla de 06 §9; el de «Dormido» es
    /// <c>live_comeback_gift.days</c> (7). Los días sin sesión son periodos completos de 24 h desde <c>stats.last_seen_at</c> [P]
    /// (no días del calendario local: así no dependen de la zona horaria ni del cambio de hora).
    /// </summary>
    public static class ComebackRules
    {
        /// <summary>«En riesgo» desde 3 días (06 §9).</summary>
        public const int AtRiskFromDays = 3;

        /// <summary>«Perdido» desde 14 días (06 §9).</summary>
        public const int LostFromDays = 14;

        /// <summary>«Churn» desde 30 días (06 §9).</summary>
        public const int ChurnFromDays = 30;

        /// <summary>Días completos en <paramref name="away"/> (0 si es negativo).</summary>
        public static int DaysAway(TimeSpan away)
        {
            if (away <= TimeSpan.Zero)
            {
                return 0;
            }

            double days = Math.Floor(away.TotalDays);
            return days >= int.MaxValue ? int.MaxValue : (int)days;
        }

        /// <summary>Segmento de <paramref name="daysAway"/> con el umbral de «Dormido» de <paramref name="gift"/>.</summary>
        public static ComebackSegment Segment(int daysAway, in ComebackGift gift)
        {
            if (daysAway >= ChurnFromDays)
            {
                return ComebackSegment.Churn;
            }

            if (daysAway >= LostFromDays)
            {
                return ComebackSegment.Lost;
            }

            if (daysAway >= gift.Days)
            {
                return ComebackSegment.Dormant;
            }

            return daysAway >= AtRiskFromDays ? ComebackSegment.AtRisk : ComebackSegment.None;
        }

        /// <summary>El segmento recibe el Regalo de regreso («Dormido», «Perdido» y «Churn»).</summary>
        public static bool GetsGift(ComebackSegment segment)
        {
            return segment >= ComebackSegment.Dormant;
        }

        /// <summary>
        /// El segmento lleva el recordatorio de una frase de TiTino y la etiqueta honesta de dificultad en el siguiente nivel
        /// («Perdido» y «Churn», 06 §9).
        /// </summary>
        public static bool GetsReminder(ComebackSegment segment)
        {
            return segment >= ComebackSegment.Lost;
        }
    }
}
