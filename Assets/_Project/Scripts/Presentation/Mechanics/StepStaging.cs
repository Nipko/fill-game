#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Estados intermedios de un vertido, solo para pintar (arquitectura del paso 1, §3 y §4): el <see cref="Cuia"/> que
    /// muestra cada cuia antes de cada etapa de R10. Es interpolación de datos, no reglas: parte de <c>After[i]</c> y deshace,
    /// de la última etapa a la pedida, los efectos que el núcleo ya aplicó (<see cref="PourEvents"/>). El final de cada paso
    /// sigue mandando: el último paso de cada cuia deja <c>After[i]</c>. Estático y sin asignaciones.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Gota (e): quita la dosis de arriba y vuelve a los campos de mecánica de <c>Before[i]</c> (contador y
    /// gotas pendientes; con la última gota, la nube vuelve).</description></item>
    /// <item><description>Mordisco (e): la víctima recupera arriba la dosis que la cueva tiene de más en <c>After</c>; la cueva
    /// la pierde y vuelve a su contador de <c>Before</c>. Un mordisco perdido o una gota que espera solo devuelven el
    /// contador.</description></item>
    /// <item><description>Open (b) y Release (d o d′): el contenido de <c>After[i]</c> con la mecánica de
    /// <c>Before[i]</c>.</description></item>
    /// <item><description>Seal (c o c′): el contenido no cambia; <see cref="PreSealed"/> da false hasta su etapa.</description></item>
    /// <item><description>Receive (a): la pareja del Remolino vuelve a <c>Before[i]</c> (las dosis entran por el fondo en el
    /// paso).</description></item>
    /// <item><description>Reveal (a): la capa revelada es siempre la nueva cima y R7 no deja representar una cima oculta
    /// (<see cref="Cuia.Create(int, int[], int)"/> la rechaza), así que el contenido se queda como en <c>After</c> y el paso
    /// Reveal pinta turbias las capas de su <c>LayerMask</c> desde su primer fotograma (<c>MurkyMarks.PlayReveal</c>).
    /// La cola arranca ese paso en el mismo fotograma en que el origen vuelve a su ranura, así que la capa nunca se ve
    /// revelada antes de tiempo.</description></item>
    /// <item><description>MudWash (a): el contenido ya es el de después de la transferencia (la anima el
    /// trabajo).</description></item>
    /// </list>
    /// Las cuias con mecánica nunca tienen dosis ocultas en un nivel válido (D-007, Q14 A y validación de cada mecánica), así
    /// que los estados se construyen con <see cref="Cuia.RemoveTop"/> y <see cref="Cuia.AddOnTop"/>, que conservan el campo de
    /// mecánica bit a bit; si aun así hubiera ocultas, se usa <see cref="Cuia.Create(int, int[], int)"/> con
    /// <see cref="Cuia.WithMechanic"/> y buffers fijos.
    /// </remarks>
    public static class StepStaging
    {
        // Buffers de colores por longitud (0..6) para Cuia.Create en el camino raro con ocultas: sin asignaciones por paso.
        private static readonly int[][] s_colors =
        {
            new int[0], new int[1], new int[2], new int[3], new int[4], new int[5], new int[6],
        };

        /// <summary>
        /// Aspecto de la cuia <paramref name="index"/> antes de la etapa <paramref name="stage"/>: <c>After[i]</c> sin los
        /// efectos de esa etapa ni de las posteriores (ver la clase).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">El índice no es una cuia del tablero.</exception>
        public static Cuia Pre(in PourVisual v, int index, StepStage stage)
        {
            if ((uint)index >= (uint)v.After.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice fuera del tablero.");
            }

            Cuia cur = v.After[index];
            if (!v.HasEvents)
            {
                return cur;
            }

            for (int s = (int)StepStage.DPrime; s >= (int)stage; s--)
            {
                cur = Undo(in v, index, (StepStage)s, cur);
            }

            return cur;
        }

        /// <summary>
        /// La cuia <paramref name="index"/> está sellada antes de la etapa <paramref name="stage"/>: lo está en <c>After</c> y,
        /// si se selló en este vertido, su etapa de sellado (<see cref="MechanicStepPlan.SealStage"/>) ya pasó.
        /// </summary>
        public static bool PreSealed(in PourVisual v, int index, StepStage stage)
        {
            if ((uint)index >= (uint)v.After.Count || !v.After.IsSealed(index))
            {
                return false;
            }

            if (v.HasEvents && MechanicStepPlan.Has(v.Events.SealedMask, index) && stage <= MechanicStepPlan.SealStage(in v, index))
            {
                return false;
            }

            return true;
        }

        /// <summary>Primera etapa del plan con algún paso que pinta la cuia <paramref name="index"/>; −1 si ninguno.</summary>
        public static int FirstStage(StepEntry[] plan, int count, int index)
        {
            return NextStage(plan, count, index, -1);
        }

        /// <summary>
        /// Primera etapa posterior a <paramref name="afterStage"/> (−1 para empezar por el principio) con algún paso que pinta la
        /// cuia <paramref name="index"/>; −1 si ninguna.
        /// </summary>
        public static int NextStage(StepEntry[] plan, int count, int index, int afterStage)
        {
            int best = -1;
            for (int e = 0; e < count; e++)
            {
                int s = (int)plan[e].Stage;
                if (s > afterStage && plan[e].Touches(index) && (best < 0 || s < best))
                {
                    best = s;
                }
            }

            return best;
        }

        /// <summary>
        /// Dónde deja el trabajo del vertido la cuia <paramref name="index"/> (el origen o la tocada) si el plan tiene pasos
        /// pendientes sobre ella: el aspecto antes de su primera etapa. Devuelve false (y <c>After[i]</c>) si no tiene pasos.
        /// </summary>
        public static bool JobEnd(in PourVisual v, StepEntry[] plan, int count, int index, out Cuia cuia, out bool sealedNow)
        {
            int first = FirstStage(plan, count, index);
            if (first < 0)
            {
                cuia = v.After[index];
                sealedNow = v.After.IsSealed(index);
                return false;
            }

            cuia = Pre(in v, index, (StepStage)first);
            sealedNow = PreSealed(in v, index, (StepStage)first);
            return true;
        }

        /// <summary>
        /// Aspecto de la cuia <paramref name="index"/> antes y después del paso de la etapa <paramref name="stage"/>: antes,
        /// <see cref="Pre"/> de esa etapa; después, <see cref="Pre"/> de la siguiente etapa con pasos sobre ella, o
        /// <c>After[i]</c> si es la última.
        /// </summary>
        public static void StepStates(in PourVisual v, StepEntry[] plan, int count, int index, StepStage stage, out Cuia before, out Cuia after, out bool afterSealed)
        {
            before = Pre(in v, index, stage);
            int next = NextStage(plan, count, index, (int)stage);
            if (next < 0)
            {
                after = v.After[index];
                afterSealed = v.After.IsSealed(index);
                return;
            }

            after = Pre(in v, index, (StepStage)next);
            afterSealed = PreSealed(in v, index, (StepStage)next);
        }

        // Deshace sobre cur los efectos de la etapa stage en la cuia index.
        private static Cuia Undo(in PourVisual v, int index, StepStage stage, Cuia cur)
        {
            PourEvents ev = v.Events;
            Cuia before = v.Before[index];
            switch (stage)
            {
                case StepStage.DPrime:
                case StepStage.D:
                    if (MechanicStepPlan.Has(ev.LianaReleasedMask, index) && MechanicStepPlan.ReleaseStage(in v, index) == stage)
                    {
                        cur = WithMechanicOf(before, cur);
                    }

                    return cur;

                case StepStage.E:
                    return UndoCounters(in v, index, cur);

                case StepStage.B:
                    if (MechanicStepPlan.Has(ev.HojaOpenedMask | ev.BroteOpenedMask, index))
                    {
                        cur = WithMechanicOf(before, cur);
                    }

                    return cur;

                case StepStage.A:
                    if (ev.ThroughRemolino && index == ev.Receiver)
                    {
                        cur = WithMechanicOf(cur, before); // las dosis aún no han entrado por el fondo
                    }

                    return cur;

                default:
                    return cur; // (c) y (c′): el sellado no cambia el contenido
            }
        }

        private static Cuia UndoCounters(in PourVisual v, int index, Cuia cur)
        {
            PourEvents ev = v.Events;
            Cuia before = v.Before[index];
            if (MechanicStepPlan.Has(ev.LluviaDropMask, index) && cur.Count > 0)
            {
                cur = cur.RemoveTop(1);
            }

            if (MechanicStepPlan.Has(ev.PiranaCaveMask, index) && cur.Count > 0)
            {
                cur = cur.RemoveTop(1);
            }

            if (MechanicStepPlan.Has(ev.PiranaBittenMask, index) && !cur.IsFull)
            {
                int cave = MechanicStepPlan.LowestBit(ev.PiranaCaveMask);
                if (cave >= 0 && !v.After[cave].IsEmpty)
                {
                    cur = cur.AddOnTop(v.After[cave].TopColor, 1);
                }
            }

            // El contador (y las gotas pendientes) de la Lluvia o de la cueva vuelven a los de Before.
            MechanicType type = before.Mechanic;
            if ((type == MechanicType.Lluvia || type == MechanicType.Pirana) && ev.CountersAdvanced)
            {
                cur = WithMechanicOf(before, cur);
            }

            return cur;
        }

        /// <summary>
        /// El contenido de <paramref name="content"/> con el campo de mecánica de <paramref name="mechanic"/> (las dos son la
        /// misma cuia: misma capacidad). Sin asignaciones.
        /// </summary>
        public static Cuia WithMechanicOf(Cuia mechanic, Cuia content)
        {
            if (mechanic.Capacity != content.Capacity)
            {
                return content;
            }

            if (content.HiddenMask != 0)
            {
                return WithHiddenContent(mechanic, content);
            }

            // RemoveTop y AddOnTop conservan el campo de mecánica bit a bit.
            Cuia c = mechanic.Count > 0 ? mechanic.RemoveTop(mechanic.Count) : mechanic;
            for (int k = 0; k < content.Count; k++)
            {
                c = c.AddOnTop(content.ColorAt(k), 1);
            }

            return c;
        }

        // Camino raro (una cuia con mecánica y ocultas): Create con el buffer de su longitud y WithMechanic con los campos.
        private static Cuia WithHiddenContent(Cuia mechanic, Cuia content)
        {
            int[] colors = s_colors[content.Count];
            for (int k = 0; k < colors.Length; k++)
            {
                colors[k] = content.ColorAt(k);
            }

            Cuia c = Cuia.Create(content.Capacity, colors, content.HiddenMask);
            if (mechanic.Mechanic == MechanicType.None)
            {
                return c;
            }

            return c.WithMechanic(
                mechanic.Mechanic,
                mechanic.MechanicKey,
                mechanic.MechanicPairId,
                mechanic.MechanicN,
                mechanic.MechanicK,
                mechanic.MechanicCounter,
                mechanic.MechanicFlags);
        }
    }
}
