#nullable enable
using System;
using AF.Save;

namespace AF.Economy
{
    /// <summary>
    /// Estado de las canoas en un instante (04 §7.1), ya con la regeneración aplicada: lo que muestran la cabecera, el
    /// inicio y Sin canoas («n/5» y «Tu próxima canoa vuelve en mm:ss»). Es un valor calculado: leerlo no cambia el perfil.
    /// </summary>
    public readonly struct CanoesStatus
    {
        /// <summary>Crea el estado.</summary>
        public CanoesStatus(int count, int max, DateTime? nextRegenAt, DateTime? infiniteUntil, DateTime now)
        {
            Count = count;
            Max = max;
            NextRegenAt = nextRegenAt;
            InfiniteUntil = infiniteUntil;
            Now = now;
        }

        /// <summary>Canoas disponibles.</summary>
        public int Count { get; }

        /// <summary>Tope (<c>canoes_max</c>).</summary>
        public int Max { get; }

        /// <summary>Cuándo vuelve la próxima canoa (UTC), o null si están todas.</summary>
        public DateTime? NextRegenAt { get; }

        /// <summary>Fin de las Canoas infinitas (UTC), o null.</summary>
        public DateTime? InfiniteUntil { get; }

        /// <summary>Instante del cálculo (UTC).</summary>
        public DateTime Now { get; }

        /// <summary>Están todas (no corre el cronómetro).</summary>
        public bool IsFull => Count >= Max;

        /// <summary>Canoas infinitas activas: un fallo duro no gasta canoa (no quitan los anuncios: D-008).</summary>
        public bool IsInfinite => InfiniteUntil.HasValue && InfiniteUntil.Value > Now;

        /// <summary>Tiempo hasta la próxima canoa (cero si están todas).</summary>
        public TimeSpan TimeToNext => NextRegenAt.HasValue && NextRegenAt.Value > Now ? NextRegenAt.Value - Now : TimeSpan.Zero;

        /// <summary>Tiempo que queda de Canoas infinitas (cero si no hay).</summary>
        public TimeSpan InfiniteRemaining => IsInfinite ? InfiniteUntil!.Value - Now : TimeSpan.Zero;
    }

    /// <summary>
    /// Reglas de las canoas (04 §7.1), sobre <see cref="CanoesData"/> del perfil:
    /// <list type="bullet">
    /// <item>tope <c>canoes_max</c> (5) y una canoa cada <c>canoes_regen_min</c> (25 min) mientras falten;</item>
    /// <item>un solo ciclo de regeneración, como Magic Sort (ficha §4.5): el cronómetro arranca al bajar del tope, sigue
    /// corriendo aunque se gasten más canoas y vuelve a empezar al llegar cada una; al llenarse se para
    /// (<c>next_regen_at</c> = null);</item>
    /// <item>el tiempo corre aunque la app esté cerrada: la regeneración se calcula con la hora del <c>IClock</c>;</item>
    /// <item>las Canoas infinitas se suman (Magic Sort: 15 + 30 min = 45 min, ficha §4.5) y, mientras duran, un fallo
    /// duro no gasta canoa.</item>
    /// </list>
    /// Si el reloj del dispositivo retrocede, no se regenera nada hasta alcanzar <c>next_regen_at</c>; el contraste con la
    /// hora del servidor (<c>sec_clock_max_drift_h</c>, 04 §7.1 "Reloj") llegará con el backend.
    /// </summary>
    public static class CanoeRules
    {
        /// <summary>Estado en <paramref name="now"/> sin tocar <paramref name="data"/>.</summary>
        public static CanoesStatus Peek(CanoesData data, DateTime now, int max, TimeSpan regen)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Compute(data.Count, data.NextRegenAt, now, max, regen, out int count, out DateTime? next);
            return new CanoesStatus(count, max, next, data.InfiniteUntil, now);
        }

        /// <summary>
        /// Escribe en <paramref name="data"/> la regeneración acumulada hasta <paramref name="now"/> y borra unas Canoas
        /// infinitas ya vencidas. Devuelve si cambió algo.
        /// </summary>
        internal static bool Regenerate(CanoesData data, DateTime now, int max, TimeSpan regen)
        {
            Compute(data.Count, data.NextRegenAt, now, max, regen, out int count, out DateTime? next);
            bool changed = count != data.Count || next != data.NextRegenAt;
            data.Count = count;
            data.NextRegenAt = next;
            if (data.InfiniteUntil.HasValue && data.InfiniteUntil.Value <= now)
            {
                data.InfiniteUntil = null;
                changed = true;
            }

            return changed;
        }

        /// <summary>Gasta una canoa (ya regenerado hasta <paramref name="now"/>). Si baja del tope, arranca el cronómetro.</summary>
        internal static void Spend(CanoesData data, DateTime now, int max, TimeSpan regen)
        {
            data.Count--;
            if (data.Count < max && !data.NextRegenAt.HasValue)
            {
                data.NextRegenAt = now + regen;
            }
        }

        /// <summary>Suma <paramref name="canoes"/> canoas; al llegar al tope, el cronómetro se para.</summary>
        internal static void Add(CanoesData data, int canoes, int max)
        {
            data.Count += canoes;
            if (data.Count >= max)
            {
                data.NextRegenAt = null;
            }
        }

        /// <summary>Llena hasta el tope (Llenar canoas, 04 §7.3).</summary>
        internal static void Fill(CanoesData data, int max)
        {
            if (data.Count < max)
            {
                data.Count = max;
            }

            data.NextRegenAt = null;
        }

        /// <summary>Alarga las Canoas infinitas <paramref name="duration"/> desde su fin, o desde ahora si no hay.</summary>
        internal static void ExtendInfinite(CanoesData data, DateTime now, TimeSpan duration)
        {
            DateTime start = data.InfiniteUntil.HasValue && data.InfiniteUntil.Value > now ? data.InfiniteUntil.Value : now;
            data.InfiniteUntil = DateTime.SpecifyKind(start + duration, DateTimeKind.Utc);
        }

        private static void Compute(int count, DateTime? next, DateTime now, int max, TimeSpan regen, out int newCount, out DateTime? newNext)
        {
            newCount = count;
            if (count >= max)
            {
                newNext = null;
                return;
            }

            if (!next.HasValue)
            {
                // Estado incoherente (faltan canoas sin cronómetro): el ciclo empieza ahora.
                newNext = DateTime.SpecifyKind(now + regen, DateTimeKind.Utc);
                return;
            }

            if (now < next.Value)
            {
                newNext = next;
                return;
            }

            long periods = 1 + ((now - next.Value).Ticks / regen.Ticks);
            if (count + periods >= max)
            {
                newCount = max;
                newNext = null;
                return;
            }

            newCount = count + (int)periods;
            newNext = DateTime.SpecifyKind(next.Value + TimeSpan.FromTicks(regen.Ticks * periods), DateTimeKind.Utc);
        }
    }
}
