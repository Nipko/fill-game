#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Etapa de R10 en la que va un paso de mecánica (anexo B de la propuesta de D-007, con el punto fijo f′): la cola las
    /// ejecuta en este orden y una etapa empieza cuando termina el paso más largo de la anterior.
    /// </summary>
    public enum StepStage
    {
        /// <summary>(a) Revelado del origen, Barro lavado del receptor y recepción por el fondo de la pareja del Remolino.</summary>
        A = 0,

        /// <summary>(b) Aperturas por recepción: la Hoja cae y el Brote germina.</summary>
        B = 1,

        /// <summary>(c) Sellados del vertido (el receptor, cuando su sellado es un paso).</summary>
        C = 2,

        /// <summary>(d) Lianas que suelta un sellado de (c).</summary>
        D = 3,

        /// <summary>(e) Contadores: gotas de la Lluvia y mordisco de la Piraña.</summary>
        E = 4,

        /// <summary>
        /// (f) Condiciones: la suelta de la cuia de la Tinaja cuando la tinaja queda vacía (b.2 regla 8; TJ3): su cuia baja del
        /// jirau (<see cref="MechanicStepKind.Unhook"/>) y la tinaja se tumba (<see cref="MechanicStepKind.Upend"/>).
        /// </summary>
        F = 5,

        /// <summary>(c′) Sellados del punto fijo f′: la cuia que completa una gota o un mordisco.</summary>
        CPrime = 6,

        /// <summary>(d′) Lianas que solo suelta un sellado de (c′).</summary>
        DPrime = 7,
    }

    /// <summary>
    /// Un paso del plan: qué paso, en qué etapa, la cuia dueña de la mecánica que lo ejecuta (<see cref="Index"/>), la otra
    /// cuia (<see cref="OtherIndex"/>, −1 si no hay), el tinte y las dosis afectadas. Valor sin asignaciones.
    /// </summary>
    public readonly struct StepEntry
    {
        /// <summary>Crea el paso.</summary>
        public StepEntry(MechanicStepKind kind, StepStage stage, int index, int otherIndex, int tint, int layerMask)
        {
            Kind = kind;
            Stage = stage;
            Index = index;
            OtherIndex = otherIndex;
            Tint = tint;
            LayerMask = layerMask;
        }

        /// <summary>Paso.</summary>
        public MechanicStepKind Kind { get; }

        /// <summary>Etapa de R10.</summary>
        public StepStage Stage { get; }

        /// <summary>Cuia dueña de la mecánica, que ejecuta el paso.</summary>
        public int Index { get; }

        /// <summary>La otra cuia del paso (víctima, cuia tocada, cuia sellada con la llave), o −1.</summary>
        public int OtherIndex { get; }

        /// <summary>Tinte del paso (índice de motor; 0 si no aplica).</summary>
        public int Tint { get; }

        /// <summary>Dosis afectadas (bit k = dosis k desde el fondo).</summary>
        public int LayerMask { get; }

        /// <summary>El paso pinta la cuia <paramref name="cuia"/> (es su dueña o la otra).</summary>
        public bool Touches(int cuia)
        {
            return Index == cuia || OtherIndex == cuia;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Kind + "@" + Stage + "(" + Index + (OtherIndex >= 0 ? "," + OtherIndex : string.Empty) + ")";
        }
    }

    /// <summary>
    /// Plan de pasos de mecánica de un vertido (arquitectura del paso 1, §4; R10 por etapas del anexo B de D-007 con el punto
    /// fijo f′). Es una consulta pura sobre las máscaras de <see cref="PourEvents"/> y los dos tableros: no decide reglas, solo
    /// ordena lo que el núcleo ya aplicó para pintarlo en ese orden. Estático y sin asignaciones.
    /// </summary>
    /// <remarks>
    /// <para>Etapas: (a) Reveal en el origen, MudWash en el receptor y Receive en la pareja del Remolino; (b) Open de la Hoja
    /// o el Brote (también la hoja que abre el Brote de dos hojas sin germinar, <see cref="PourEvents.BroteLeafOpenedMask"/>;
    /// VA1b); (c) Seal; (d) Release de la Liana y de la Flor de vitória-régia (<see cref="ReleaseStepMask"/>; VA1b); (e) RainDrop, RainWait, RainEnd, Bite y BiteLost; (c′) Seal de lo que
    /// sella el punto fijo f′ tras una gota o un mordisco, <c>SealedMask ∩ (LluviaDropMask | PiranaCaveMask)</c>; (d′) Release
    /// de las Lianas que solo suelta un sellado de (c′). Entre (e) y (c′), la etapa (f) de condiciones: la suelta de la cuia de la
    /// Tinaja (<see cref="PourEvents.CucharonReleasedMask"/>; TJ3), con dos pasos a la vez, Unhook en su cuia y Upend en la tinaja
    /// (la otra cuia de cada uno es la de la pareja). No pasa por <see cref="ReleaseStepMask"/>, que es de (d) y (d′).</para>
    /// <para>Al sacar de la Tinaja, el Reveal de (a) va en la tinaja y lo ejecuta su overlay (la nueva boca, no agua turbia).</para>
    /// <para>(c′) no depende de <see cref="PourEvents.FixedPointPasses"/>: el núcleo cuenta como vuelta 1 la que sella la
    /// cuia de la gota (y como vuelta 1 también la que solo confirma una Liana soltada en (d)), así que la máscara es la que
    /// distingue los dos casos. Sin vueltas extra (0), una gota o un mordisco no pudieron sellar nada y (c′) y (d′) quedan
    /// vacías.</para>
    /// <para>Sellado del receptor: es un paso Seal de (c), o de (c′) si el receptor está en <c>LluviaDropMask</c>, cuando el
    /// vertido genera algún otro paso o el receptor tenía mecánica (el Matapi recibe así su <c>PlayStep(Seal)</c> y en el 83 la
    /// tapa cae antes de que se suelte la Liana); entonces el trabajo del vertido no lo sella. Si no, el sellado se queda en
    /// el trabajo (450 ms tras el caudal, 07 §5.3), como en un vertido normal.</para>
    /// <para>(d′) para la Liana con fruta: ninguna cuia sellada en (c) tiene la llave en la cima y alguna de (c′) sí. Para la
    /// Liana de cuenta (variante del 243 en adelante): las selladas de <c>Before</c> más las de (c) no llegan a <c>n</c>. Para la
    /// Flor de vitória-régia (403; VA1b): con las selladas de <c>Before</c> y las de (c) no están sus dos llaves.</para>
    /// <para>Despacho a la cuia dueña de la mecánica: Bite y BiteLost a la cueva (OtherIndex = la víctima); Receive a la
    /// pareja (OtherIndex = la cuia tocada); Release a la Liana (OtherIndex = la sellada con la llave, preferentemente la de
    /// su etapa; la Flor, con el tinte de esa cuia, la llave que la abre); RainDrop, RainWait y RainEnd a la Lluvia (RainEnd es
    /// la última gota, que también cae); Open a la Hoja o el Brote; Seal a la cuia; Reveal al origen y MudWash al receptor.</para>
    /// </remarks>
    public static class MechanicStepPlan
    {
        /// <summary>
        /// Pasos como mucho en un vertido (3 de (a), 2 de (b), 16 por cada etapa de sellados, Lianas y contadores y los 2 de la
        /// suelta de la Tinaja, que no se combina con contadores ni Lianas).
        /// </summary>
        public const int MaxSteps = 64;

        /// <summary>Número de etapas (<see cref="StepStage"/>).</summary>
        public const int StageCount = 8;

        /// <summary>
        /// Escribe en <paramref name="buffer"/> los pasos de <paramref name="v"/> en el orden de las etapas y devuelve cuántos
        /// son (0 si el vertido no tiene pasos o si <see cref="PourVisual.HasEvents"/> es false). Si el buffer se queda corto,
        /// los pasos que no caben se pierden (con <see cref="MaxSteps"/> nunca pasa). Sin asignaciones.
        /// </summary>
        /// <exception cref="ArgumentNullException">Falta el buffer.</exception>
        public static int Build(in PourVisual v, StepEntry[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (!v.HasEvents)
            {
                return 0;
            }

            PourEvents ev = v.Events;
            BoardState before = v.Before;
            BoardState after = v.After;
            int n = Math.Min(before.Count, after.Count);
            int recv = ev.Receiver;
            int cPrime = FixedPointSealMask(in v);
            int cSeal = ev.SealedMask & ~cPrime;
            int count = 0;

            // (a) Revelado de la nueva cima del origen, Barro lavado del receptor y recepción por el fondo de la pareja.
            for (int i = 0; i < n; i++)
            {
                if (Has(ev.RevealedMask, i))
                {
                    Cuia a = after[i];
                    int layers = before[i].HiddenMask & ~a.HiddenMask & LowMask(a.Count);
                    count = Add(buffer, count, new StepEntry(MechanicStepKind.Reveal, StepStage.A, i, -1, a.IsEmpty ? 0 : a.TopColor, layers));
                }
            }

            for (int i = 0; i < n; i++)
            {
                if (Has(ev.BarroWashedMask, i))
                {
                    int mud = Math.Max(0, before[i].Count - 1);
                    count = Add(buffer, count, new StepEntry(MechanicStepKind.MudWash, StepStage.A, i, -1, ev.Color, 1 << mud));
                }
            }

            if (ev.ThroughRemolino && (uint)recv < (uint)n)
            {
                count = Add(buffer, count, new StepEntry(MechanicStepKind.Receive, StepStage.A, recv, ev.Target, ev.Color, LowMask(ev.Amount)));
            }

            // (b) Aperturas por recepción (y la hoja del Brote de dos hojas que se abre sin germinar, VA1b).
            int opened = ev.HojaOpenedMask | ev.BroteOpenedMask | ev.BroteLeafOpenedMask;
            for (int i = 0; i < n; i++)
            {
                if (Has(opened, i))
                {
                    count = Add(buffer, count, new StepEntry(MechanicStepKind.Open, StepStage.B, i, -1, ev.Color, 0));
                }
            }

            // (c) Sellados; el del receptor solo si es un paso.
            bool receiverStep = ReceiverSealIsStep(in v);
            for (int i = 0; i < n; i++)
            {
                if (Has(cSeal, i) && (i != recv || receiverStep))
                {
                    count = Add(buffer, count, new StepEntry(MechanicStepKind.Seal, StepStage.C, i, -1, after[i].TopColor, 0));
                }
            }

            // (d) Lianas que suelta un sellado de (c).
            count = AddReleases(in v, StepStage.D, buffer, count);

            // (e) Contadores: la cuia de la Lluvia y la cueva de la Piraña.
            for (int i = 0; i < n; i++)
            {
                if (Has(ev.LluviaDropMask, i))
                {
                    MechanicStepKind kind = Has(ev.LluviaEndedMask, i) ? MechanicStepKind.RainEnd : MechanicStepKind.RainDrop;
                    int top = Math.Max(0, after[i].Count - 1);
                    count = Add(buffer, count, new StepEntry(kind, StepStage.E, i, -1, before[i].MechanicKey, 1 << top));
                }
                else if (Has(ev.LluviaWaitingMask, i))
                {
                    count = Add(buffer, count, new StepEntry(MechanicStepKind.RainWait, StepStage.E, i, -1, before[i].MechanicKey, 0));
                }

                if (Has(ev.PiranaCaveMask, i))
                {
                    int top = Math.Max(0, after[i].Count - 1);
                    count = Add(buffer, count, new StepEntry(MechanicStepKind.Bite, StepStage.E, i, Victim(in ev), after[i].IsEmpty ? 0 : after[i].TopColor, 1 << top));
                }
                else if (Has(ev.PiranaBiteLostMask, i))
                {
                    count = Add(buffer, count, new StepEntry(MechanicStepKind.BiteLost, StepStage.E, i, recv, 0, 0));
                }
            }

            // (f) Condiciones: la tinaja quedó vacía y su cuia baja del jirau (b.2 regla 8); la tinaja, a la vez, se tumba.
            for (int i = 0; i < n; i++)
            {
                if (Has(ev.CucharonReleasedMask, i))
                {
                    int jar = TinajaOf(before, ev.Source);
                    count = Add(buffer, count, new StepEntry(MechanicStepKind.Unhook, StepStage.F, i, jar, 0, 0));
                    if (jar >= 0)
                    {
                        count = Add(buffer, count, new StepEntry(MechanicStepKind.Upend, StepStage.F, jar, i, 0, 0));
                    }
                }
            }

            // (c′) y (d′): lo que sella o suelta el punto fijo tras la gota o el mordisco.
            for (int i = 0; i < n; i++)
            {
                if (Has(cPrime, i))
                {
                    count = Add(buffer, count, new StepEntry(MechanicStepKind.Seal, StepStage.CPrime, i, -1, after[i].TopColor, 0));
                }
            }

            count = AddReleases(in v, StepStage.DPrime, buffer, count);
            return count;
        }

        /// <summary>
        /// (c′): las cuias que sella el punto fijo f′ tras la gota o el mordisco, <c>SealedMask ∩ (LluviaDropMask |
        /// PiranaCaveMask)</c> (0 sin eventos o sin vueltas del punto fijo).
        /// </summary>
        public static int FixedPointSealMask(in PourVisual v)
        {
            if (!v.HasEvents)
            {
                return 0;
            }

            PourEvents ev = v.Events;
            if (ev.FixedPointPasses <= 0)
            {
                return 0;
            }

            return ev.SealedMask & (ev.LluviaDropMask | ev.PiranaCaveMask);
        }

        /// <summary>
        /// El receptor se sella en este vertido y su sellado es un paso Seal (de (c) o de (c′)): el vertido genera algún otro
        /// paso o el receptor tenía mecánica. Entonces el trabajo del vertido no lo sella.
        /// </summary>
        public static bool ReceiverSealIsStep(in PourVisual v)
        {
            if (!v.HasEvents)
            {
                return false;
            }

            PourEvents ev = v.Events;
            int recv = ev.Receiver;
            if (!Has(ev.SealedMask, recv))
            {
                return false;
            }

            if (Has(FixedPointSealMask(in v), recv) || v.Before[recv].Mechanic != MechanicType.None)
            {
                return true;
            }

            return HasOtherSteps(in v, recv);
        }

        /// <summary>
        /// Etapa en la que se suelta la Liana <paramref name="index"/> en este vertido: <see cref="StepStage.D"/> o
        /// <see cref="StepStage.DPrime"/> (ver la clase). Para una cuia que no se soltó, <see cref="StepStage.D"/>.
        /// </summary>
        public static StepStage ReleaseStage(in PourVisual v, int index)
        {
            PourEvents ev = v.Events;
            int cPrime = FixedPointSealMask(in v);
            if (cPrime == 0 || !Has(ReleaseStepMask(in v), index))
            {
                return StepStage.D;
            }

            BoardState before = v.Before;
            BoardState after = v.After;
            int cSeal = ev.SealedMask & ~cPrime;
            Cuia liana = before[index];
            if (liana.Mechanic == MechanicType.Flor)
            {
                // La Flor (VA1b): en (d) si con las selladas de Before y las de (c) ya están sus dos llaves; si no, la abre (c′).
                bool both = KeySealedByStageC(before, after, cSeal, index, liana.MechanicKey)
                    && KeySealedByStageC(before, after, cSeal, index, liana.MechanicKey2);
                return both ? StepStage.D : StepStage.DPrime;
            }

            if (liana.MechanicN == 0)
            {
                int key = liana.MechanicKey;
                bool inC = SealedWithTop(after, cSeal, index, key) >= 0;
                bool inCPrime = SealedWithTop(after, cPrime, index, key) >= 0;
                return !inC && inCPrime ? StepStage.DPrime : StepStage.D;
            }

            int sealedBefore = 0;
            for (int j = 0; j < before.Count; j++)
            {
                if (j != index && before.IsSealed(j))
                {
                    sealedBefore++;
                }
            }

            int sealedInC = PopCount(cSeal & ~(1 << index));
            return sealedBefore + sealedInC < liana.MechanicN ? StepStage.DPrime : StepStage.D;
        }

        /// <summary>Etapa del sellado de la cuia <paramref name="index"/> en este vertido: (c′) o (c).</summary>
        public static StepStage SealStage(in PourVisual v, int index)
        {
            return Has(FixedPointSealMask(in v), index) ? StepStage.CPrime : StepStage.C;
        }

        /// <summary>
        /// Cuias que se sueltan por un sellado en este vertido con un paso Release (etapa (d) o (d′)): las Lianas
        /// (<see cref="PourEvents.LianaReleasedMask"/>) y las Flores de vitória-régia (VA1b), que el núcleo solo marca en
        /// <see cref="PourEvents.ReleasedMask"/> (la cuia de la Tinaja también va ahí, pero baja en (f) y no es de aquí). 0 sin
        /// eventos. Sin asignaciones.
        /// </summary>
        public static int ReleaseStepMask(in PourVisual v)
        {
            if (!v.HasEvents)
            {
                return 0;
            }

            PourEvents ev = v.Events;
            int mask = ev.LianaReleasedMask;
            int rest = ev.ReleasedMask & ~mask;
            BoardState before = v.Before;
            for (int i = 0; rest != 0 && i < before.Count && i < 32; i++)
            {
                if (Has(rest, i) && before[i].Mechanic == MechanicType.Flor)
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        private static int AddReleases(in PourVisual v, StepStage stage, StepEntry[] buffer, int count)
        {
            PourEvents ev = v.Events;
            BoardState before = v.Before;
            BoardState after = v.After;
            int n = Math.Min(before.Count, after.Count);
            int cPrime = FixedPointSealMask(in v);
            int stageSeals = stage == StepStage.DPrime ? cPrime : ev.SealedMask & ~cPrime;
            int released = ReleaseStepMask(in v);
            for (int i = 0; i < n; i++)
            {
                if (!Has(released, i) || ReleaseStage(in v, i) != stage)
                {
                    continue;
                }

                if (before[i].Mechanic == MechanicType.Flor)
                {
                    count = Add(buffer, count, FlorRelease(in v, stage, stageSeals, i));
                    continue;
                }

                Cuia liana = before[i];
                int key = liana.MechanicN == 0 ? liana.MechanicKey : Cuia.NoColor;
                int other = key != Cuia.NoColor ? SealedWithTop(after, stageSeals, i, key) : LowestBit(stageSeals & ~(1 << i));
                if (other < 0 && key != Cuia.NoColor)
                {
                    other = SealedWithTopAnywhere(after, i, key);
                }

                count = Add(buffer, count, new StepEntry(MechanicStepKind.Release, stage, i, other, key, 0));
            }

            return count;
        }

        // Algún paso además del sellado del receptor (con su mecánica, los contadores que no cambian nada también cuentan:
        // RainWait y BiteLost son pasos del overlay).
        private static bool HasOtherSteps(in PourVisual v, int recv)
        {
            PourEvents ev = v.Events;
            if (ev.RevealedMask != 0 || ev.BarroWashedMask != 0 || ev.ThroughRemolino)
            {
                return true;
            }

            if ((ev.HojaOpenedMask | ev.BroteOpenedMask | ev.BroteLeafOpenedMask) != 0 || ReleaseStepMask(in v) != 0)
            {
                return true;
            }

            if ((ev.LluviaDropMask | ev.LluviaWaitingMask | ev.PiranaCaveMask | ev.PiranaBiteLostMask | ev.CucharonReleasedMask) != 0)
            {
                return true;
            }

            return (ev.SealedMask & ~(1 << recv)) != 0;
        }

        // Paso Release de la Flor index en la etapa stage (VA1b): la otra cuia es la sellada de esa etapa con una de sus llaves en
        // la cima (la segunda llave primero), o cualquiera sellada con una de ellas; el tinte, la llave de esa cuia (la que se
        // acaba de cumplir).
        private static StepEntry FlorRelease(in PourVisual v, StepStage stage, int stageSeals, int index)
        {
            BoardState after = v.After;
            Cuia flor = v.Before[index];
            int key = flor.MechanicKey;
            int key2 = flor.MechanicKey2;
            int other = SealedWithTop(after, stageSeals, index, key2);
            if (other < 0)
            {
                other = SealedWithTop(after, stageSeals, index, key);
            }

            if (other < 0)
            {
                other = SealedWithTopAnywhere(after, index, key2);
            }

            if (other < 0)
            {
                other = SealedWithTopAnywhere(after, index, key);
            }

            int tint = other >= 0 ? after[other].TopColor : key2;
            return new StepEntry(MechanicStepKind.Release, stage, index, other, tint, 0);
        }

        // La llave key de la Flor index ya está sellada antes de (d): sellada en Before o en la etapa (c) de este vertido (cSeal),
        // en una cuia con key en la cima.
        private static bool KeySealedByStageC(BoardState before, BoardState after, int cSeal, int index, int key)
        {
            for (int j = 0; j < after.Count; j++)
            {
                if (j == index)
                {
                    continue;
                }

                if (j < before.Count && before.IsSealed(j) && before[j].TopColor == key)
                {
                    return true;
                }

                if (Has(cSeal, j) && after.IsSealed(j) && after[j].TopColor == key)
                {
                    return true;
                }
            }

            return false;
        }

        // La tinaja de la pareja (una por nivel, b.2 regla 1): el origen si lo es (siempre al sacar); si no, la primera del
        // tablero de antes; −1 si no hay.
        private static int TinajaOf(BoardState before, int source)
        {
            if ((uint)source < (uint)before.Count && before[source].Mechanic == MechanicType.Tinaja)
            {
                return source;
            }

            for (int j = 0; j < before.Count; j++)
            {
                if (before[j].Mechanic == MechanicType.Tinaja)
                {
                    return j;
                }
            }

            return -1;
        }

        // La víctima del mordisco: siempre el receptor (PiranaBittenMask lo marca).
        private static int Victim(in PourEvents ev)
        {
            int bitten = LowestBit(ev.PiranaBittenMask);
            return bitten >= 0 ? bitten : ev.Receiver;
        }

        // Primera cuia de mask (sin except) sellada en after con la cima key; −1 si no hay.
        private static int SealedWithTop(BoardState after, int mask, int except, int key)
        {
            for (int j = 0; j < after.Count; j++)
            {
                if (j != except && Has(mask, j) && after.IsSealed(j) && after[j].TopColor == key)
                {
                    return j;
                }
            }

            return -1;
        }

        private static int SealedWithTopAnywhere(BoardState after, int except, int key)
        {
            for (int j = 0; j < after.Count; j++)
            {
                if (j != except && after.IsSealed(j) && after[j].TopColor == key)
                {
                    return j;
                }
            }

            return -1;
        }

        private static int Add(StepEntry[] buffer, int count, StepEntry entry)
        {
            if (count >= buffer.Length)
            {
                return count;
            }

            buffer[count] = entry;
            return count + 1;
        }

        internal static bool Has(int mask, int index)
        {
            return (uint)index < 32u && ((mask >> index) & 1) != 0;
        }

        internal static int LowMask(int bits)
        {
            return bits <= 0 ? 0 : bits >= 31 ? int.MaxValue : (1 << bits) - 1;
        }

        internal static int LowestBit(int mask)
        {
            for (int i = 0; i < 32; i++)
            {
                if (((mask >> i) & 1) != 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int PopCount(int mask)
        {
            int c = 0;
            while (mask != 0)
            {
                mask &= mask - 1;
                c++;
            }

            return c;
        }
    }
}
