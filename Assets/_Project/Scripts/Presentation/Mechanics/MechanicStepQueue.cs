#nullable enable
using System;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Lo que la cola de pasos registró de un paso (diagnóstico y tests): qué paso, de qué etapa, en qué cuias, el overlay al
    /// que se despachó (<see cref="MechanicType.None"/> si no había overlay: las marcas o la cuia sin mecánica), si se usó el
    /// paso por defecto de la cola, cuándo empezó y terminó (ms del reloj de <c>BoardAnimator</c>) y con qué factor.
    /// </summary>
    public readonly struct MechanicStepRecord
    {
        /// <summary>Crea el registro.</summary>
        public MechanicStepRecord(MechanicStepKind kind, StepStage stage, int index, int otherIndex, MechanicType owner, bool usedDefault, float startMs, float endMs, float speedFactor, bool snapped)
        {
            UsedDefault = usedDefault;
            Kind = kind;
            Stage = stage;
            Index = index;
            OtherIndex = otherIndex;
            Owner = owner;
            StartMs = startMs;
            EndMs = endMs;
            SpeedFactor = speedFactor;
            Snapped = snapped;
        }

        /// <summary>Paso.</summary>
        public MechanicStepKind Kind { get; }

        /// <summary>Etapa de R10.</summary>
        public StepStage Stage { get; }

        /// <summary>Cuia dueña de la mecánica.</summary>
        public int Index { get; }

        /// <summary>La otra cuia, o −1.</summary>
        public int OtherIndex { get; }

        /// <summary>Mecánica del overlay que recibió <c>PlayStep</c> (None si no había overlay: las marcas o una cuia sin mecánica).</summary>
        public MechanicType Owner { get; }

        /// <summary>La cola usó su paso por defecto (el overlay devolvió 0 o no había overlay).</summary>
        public bool UsedDefault { get; }

        /// <summary>Instante en que empezó (ms del reloj de la cola).</summary>
        public float StartMs { get; }

        /// <summary>Instante en que terminó (ms); el previsto mientras sigue en curso.</summary>
        public float EndMs { get; }

        /// <summary>Factor de velocidad del paso (nivel, Reducir movimiento y presupuesto).</summary>
        public float SpeedFactor { get; }

        /// <summary>Terminó antes de tiempo (SnapAll o salto por presupuesto).</summary>
        public bool Snapped { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Kind + "@" + Stage + "(" + Index.ToString(CultureInfo.InvariantCulture)
                + (OtherIndex >= 0 ? "," + OtherIndex.ToString(CultureInfo.InvariantCulture) : string.Empty) + ") "
                + StartMs.ToString("0", CultureInfo.InvariantCulture) + "-" + EndMs.ToString("0", CultureInfo.InvariantCulture)
                + (Owner != MechanicType.None ? " " + Owner : string.Empty) + (UsedDefault ? " default" : string.Empty)
                + (Snapped ? " snap" : string.Empty);
        }
    }

    /// <summary>
    /// Cola de pasos de mecánica de <c>BoardAnimator</c> (arquitectura del paso 1, §4; R10 por etapas con f′): ejecuta el plan
    /// de cada vertido (<see cref="MechanicStepPlan"/>) por etapas cuando termina su caudal. Una etapa empieza cuando termina
    /// el paso más largo de la anterior y los pasos de una misma etapa van a la vez. Arrays fijos y sin asignaciones (08 §5.4).
    /// </summary>
    /// <remarks>
    /// <para><b>Despacho.</b> Cada paso va al overlay de la mecánica que la cuia dueña tiene en <c>ctx.Before</c>
    /// (<see cref="CuiaOverlay.PlayStep"/>); si no hay overlay o devuelve 0, se usa el paso por defecto. Reveal y MudWash van a
    /// <c>CuiaView.PlayReveal</c> y <c>CuiaView.PlayMudWash</c> (las marcas). Al acabar cada paso,
    /// <see cref="CuiaOverlay.SnapStep"/> (o el de las marcas) y <c>ShowContent(ctx.After, ctx.AfterSealed)</c> en las dos
    /// cuias.</para>
    /// <para><b>El origen, primero en su ranura.</b> Ningún paso que pinte el origen (su <c>Index</c> o su
    /// <c>OtherIndex</c>) empieza antes de que vuelva a su ranura (<c>SrcDone</c>), igual que el Reveal: mientras vuela, el
    /// trabajo del vertido es quien lo pinta, y un paso que terminara antes (la gota de una Lluvia que es el origen, Q-B)
    /// quedaría debajo del estado con el que el trabajo lo devuelve. Los pasos de esa etapa que no lo tocan van a su hora y
    /// la etapa termina con el más largo de todos; la espera cuenta en el presupuesto.</para>
    /// <para><b>Pasos por defecto</b> (cifras canónicas × factor de velocidad): Reveal, lo anima MurkyMarks en
    /// <see cref="MurkyMarks.RevealRuleMs"/> = 1.200 ms [P] (b.2; sustituye a los 300 ms de 02 §4.1 y 07 §4.3, que se
    /// alinean en el paso 2); Seal, <c>SealAnimationAt</c> en 450 ms
    /// (07 §5.3); RainDrop y RainEnd, la gota sube como una dosis en <c>anim_pour_dose_ms</c> (280 ms); Bite, la víctima pierde
    /// la dosis de arriba en 280 ms y después la cueva la recibe en 280 ms, en un solo paso; MudWash, 0; los demás, salto.</para>
    /// <para><b>Presupuesto desde el toque.</b> Del toque a la vista quieta, como mucho <see cref="BudgetFromTapMs"/> =
    /// <c>MaxViewDrainWaitMs</c> − 200 = 2.800 ms (3.000 es el tope [P] del tech-lead de <c>GameConfig</c>, que no cambia; un
    /// test EditMode ata las dos cifras). Al llegar a (a) se calcula <c>restante</c> = 2.800 − ms desde el toque, y el fin
    /// previsto del plan con la espera <c>w</c> hasta que vuelva el origen, que no se acelera: con el factor <c>f</c>,
    /// <c>max(f · Σ A, w + f · B)</c>, donde <c>A</c> es el paso más largo de cada etapa (<c>StepDurationMs</c>) y <c>B</c>, el
    /// camino más largo que empieza en un paso que espera al origen (ese paso y las etapas siguientes). Si no cabe, el factor
    /// es el mayor que lo deja en <c>restante − 1 ms</c>, y nunca menos de 0,35: el milisegundo de margen
    /// (<see cref="BudgetSlackMs"/>) absorbe el redondeo de coma flotante, así que el plan acelerado acaba antes del
    /// presupuesto y nunca después. Si aun así no cabe, saltan las etapas que acabarían después de <c>restante</c> (sus pasos
    /// terminan en el acto), se cuenta en <see cref="BudgetOverruns"/> y se avisa con <c>step_budget</c> por
    /// <c>BoardAnimator.DevEvent</c>. Ese enlace está PENDIENTE: lo une a <c>DevProbe.Event</c> un gancho en el constructor de
    /// <c>GameScreen</c> (<c>_animator.DevEvent = _probe.Event;</c>) que añade la sesión principal entre olas (arquitectura
    /// del paso 1, §7); hasta entonces, sin sonda, no se escribe nada. Los pasos que esperan al origen empiezan en el instante
    /// previsto de su vuelta (o en el del fotograma que la nota, si fuera antes), así que el plan sigue el reloj previsto; la
    /// vista nota el final en el primer fotograma que llega a él, y ese menos de un fotograma lo absorben los 200 ms de margen
    /// hasta el tope de 3.000.</para>
    /// </remarks>
    public sealed class MechanicStepQueue
    {
        /// <summary>Planes a la vez como mucho (si se llenan, la cola salta el más antiguo).</summary>
        public const int MaxPlans = 16;

        /// <summary>
        /// Del toque a la vista quieta: <c>MaxViewDrainWaitMs</c> (3.000, [P] del tech-lead) − 200 ms de margen para que la
        /// celebración o el modal nunca corten un paso.
        /// </summary>
        public const float BudgetFromTapMs = 2800f;

        /// <summary>Factor mínimo del recorte del presupuesto: los pasos nunca van a más de 1/0,35 ≈ 2,9 veces su velocidad.</summary>
        public const float MinBudgetSpeed = 0.35f;

        /// <summary>
        /// Margen (ms) con el que el plan acelerado apunta por debajo del presupuesto: el redondeo de coma flotante nunca lo
        /// deja acabar después de <see cref="BudgetFromTapMs"/>.
        /// </summary>
        public const float BudgetSlackMs = 1f;

        /// <summary>Registros de pasos que se guardan (los más recientes).</summary>
        public const int LogCapacity = 128;

        private const byte Pending = 0;
        private const byte Running = 1;
        private const byte Done = 2;

        private readonly BoardAnimator _board;
        private readonly Plan[] _plans = new Plan[MaxPlans];
        private readonly MechanicStepRecord[] _log = new MechanicStepRecord[LogCapacity];
        private int _logStart;
        private int _logCount;

        internal MechanicStepQueue(BoardAnimator board)
        {
            _board = board;
            for (int i = 0; i < MaxPlans; i++)
            {
                _plans[i] = new Plan();
            }
        }

        /// <summary>Planes en cola o en curso.</summary>
        public int ActiveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < MaxPlans; i++)
                {
                    if (_plans[i].InUse)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>Pasos en curso ahora mismo.</summary>
        public int RunningCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < MaxPlans; i++)
                {
                    Plan p = _plans[i];
                    if (!p.InUse)
                    {
                        continue;
                    }

                    for (int e = 0; e < p.Count; e++)
                    {
                        if (p.State[e] == Running)
                        {
                            n++;
                        }
                    }
                }

                return n;
            }
        }

        /// <summary>Planes cuyas últimas etapas saltaron porque no cabían en el presupuesto desde el toque.</summary>
        public int BudgetOverruns { get; private set; }

        /// <summary>Planes que hubo que acelerar (factor &lt; 1) para caber en el presupuesto.</summary>
        public int BudgetScaled { get; private set; }

        /// <summary>Factor de velocidad del último plan que empezó (diagnóstico).</summary>
        public float LastSpeedFactor { get; private set; } = 1f;

        /// <summary>Registros guardados (como mucho <see cref="LogCapacity"/>).</summary>
        public int LogCount => _logCount;

        /// <summary>Registro <paramref name="i"/>, del más antiguo (0) al más reciente.</summary>
        public MechanicStepRecord LogAt(int i)
        {
            if ((uint)i >= (uint)_logCount)
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }

            return _log[(_logStart + i) % LogCapacity];
        }

        /// <summary>Vacía el registro.</summary>
        public void ClearLog()
        {
            _logStart = 0;
            _logCount = 0;
        }

        /// <summary>Hay un paso <paramref name="kind"/> en curso con dueña <paramref name="index"/> (−1: cualquiera).</summary>
        public bool IsRunning(MechanicStepKind kind, int index = -1)
        {
            return TryGetRunning(kind, index, out _, out _);
        }

        /// <summary>
        /// Paso <paramref name="kind"/> en curso (con dueña <paramref name="index"/>, o cualquiera con −1): devuelve los ms que
        /// lleva y su duración.
        /// </summary>
        public bool TryGetRunning(MechanicStepKind kind, int index, out float elapsedMs, out float durationMs)
        {
            for (int i = 0; i < MaxPlans; i++)
            {
                Plan p = _plans[i];
                if (!p.InUse || !p.Begun)
                {
                    continue;
                }

                for (int e = 0; e < p.Count; e++)
                {
                    if (p.State[e] == Running && p.Entries[e].Kind == kind && (index < 0 || p.Entries[e].Index == index))
                    {
                        elapsedMs = p.T - p.Start[e];
                        durationMs = p.Dur[e];
                        return true;
                    }
                }
            }

            elapsedMs = 0f;
            durationMs = 0f;
            return false;
        }

        // Reserva un plan con los pasos de entries; −1 si no queda sitio.
        internal int Acquire(in PourVisual v, StepEntry[] entries, int count)
        {
            for (int i = 0; i < MaxPlans; i++)
            {
                Plan p = _plans[i];
                if (p.InUse)
                {
                    continue;
                }

                p.InUse = true;
                p.Begun = false;
                p.Visual = v;
                p.Source = v.Source;
                p.Count = Math.Min(count, MechanicStepPlan.MaxSteps);
                p.Touched = 0;
                for (int e = 0; e < p.Count; e++)
                {
                    p.Entries[e] = entries[e];
                    p.State[e] = Pending;
                    p.Overlay[e] = null;
                    p.Log[e] = -1;
                    p.Touched |= Bit(entries[e].Index) | Bit(entries[e].OtherIndex);
                }

                return i;
            }

            return -1;
        }

        // Cuias que pinta el plan (dueñas y otras).
        internal int TouchedMask(int slot)
        {
            return _plans[slot].Touched;
        }

        // Cuias con pasos sin terminar (todas las del plan antes de empezar).
        internal int PendingMask(int slot)
        {
            Plan p = _plans[slot];
            if (!p.InUse)
            {
                return 0;
            }

            if (!p.Begun)
            {
                return p.Touched;
            }

            int mask = 0;
            for (int e = 0; e < p.Count; e++)
            {
                if (p.State[e] != Done)
                {
                    mask |= Bit(p.Entries[e].Index) | Bit(p.Entries[e].OtherIndex);
                }
            }

            return mask;
        }

        /// <summary>
        /// Estado de la cuia <paramref name="cuia"/> en el punto actual del plan <paramref name="slot"/>, ya empezado: el de
        /// después (<c>After</c>) si no le quedan pasos, y si no, el previo a su primera etapa sin terminar
        /// (<see cref="StepStaging.Pre"/>). Lo usa el fin del trabajo del vertido para no pintar nunca encima de pasos ya
        /// terminados. False (sin tocar nada) si el plan no está en marcha o no pinta esa cuia.
        /// </summary>
        internal bool StateAt(int slot, int cuia, out Cuia state, out bool sealedNow)
        {
            Plan p = _plans[slot];
            state = default;
            sealedNow = false;
            if (!p.InUse || !p.Begun || (p.Touched & Bit(cuia)) == 0)
            {
                return false;
            }

            int first = -1;
            for (int e = 0; e < p.Count; e++)
            {
                int s = (int)p.Entries[e].Stage;
                if (p.State[e] != Done && p.Entries[e].Touches(cuia) && (first < 0 || s < first))
                {
                    first = s;
                }
            }

            PourVisual v = p.Visual;
            if (first < 0)
            {
                state = v.After[cuia];
                sealedNow = v.After.IsSealed(cuia);
                return true;
            }

            state = StepStaging.Pre(in v, cuia, (StepStage)first);
            sealedNow = StepStaging.PreSealed(in v, cuia, (StepStage)first);
            return true;
        }

        // Empieza el plan al terminar el caudal (etapa (a)): estados de cada paso, overlays, duraciones nominales y
        // presupuesto. ageMs: ms desde el toque; sourceBackInMs: lo que falta para que el origen vuelva a su ranura (la espera
        // de los pasos que lo pintan, que cuenta en el presupuesto).
        internal void Begin(int slot, float ageMs, float sourceBackInMs)
        {
            Plan p = _plans[slot];
            p.Begun = true;
            p.T = 0f;
            p.Stage = 0;
            p.StageStart = 0f;
            p.SrcDoneAt = -1f;
            p.SrcBackAt = Mathf.Max(0f, sourceBackInMs);
            float speed = _board.StepSpeedBase;
            bool waits = false;
            for (int s = 0; s < MechanicStepPlan.StageCount; s++)
            {
                p.StageMax[s] = 0f;
                p.StageWaitMax[s] = -1f;
            }

            PourVisual v = p.Visual;
            for (int e = 0; e < p.Count; e++)
            {
                StepEntry se = p.Entries[e];
                StepStaging.StepStates(in v, p.Entries, p.Count, se.Index, se.Stage, out p.Before[e], out p.After[e], out p.AfterSealed[e]);
                if (se.OtherIndex >= 0)
                {
                    StepStaging.StepStates(in v, p.Entries, p.Count, se.OtherIndex, se.Stage, out p.OtherBefore[e], out p.OtherAfter[e], out p.OtherAfterSealed[e]);
                }
                else
                {
                    p.OtherBefore[e] = default;
                    p.OtherAfter[e] = default;
                    p.OtherAfterSealed[e] = false;
                }

                float nominal = NominalMs(p, e);
                p.Nominal[e] = nominal;
                int stage = (int)se.Stage;
                p.StageMax[stage] = Mathf.Max(p.StageMax[stage], nominal);
                if (WaitsForSource(p, e))
                {
                    p.StageWaitMax[stage] = Mathf.Max(p.StageWaitMax[stage], nominal);
                    waits = true;
                }
            }

            // Presupuesto desde el toque. Con el factor f, el plan acaba en max(f · ΣA, w + f · B) (ver la clase): PlannedEnd
            // lo calcula etapa a etapa igual que el recorrido de los saltos de abajo, así que un plan que cabe sin acelerar da
            // allí los mismos valores y nunca salta por redondeo.
            float remaining = BudgetFromTapMs - ageMs;
            float wait = waits ? p.SrcBackAt : 0f;
            float planned = PlannedEnd(p, speed, wait, MechanicStepPlan.StageCount);
            if (planned > remaining)
            {
                float target = remaining - BudgetSlackMs;
                float all = 0f;
                float waitPath = 0f;
                for (int s = MechanicStepPlan.StageCount - 1; s >= 0; s--)
                {
                    if (p.StageWaitMax[s] >= 0f)
                    {
                        waitPath = Mathf.Max(waitPath, p.StageWaitMax[s] + all); // ese paso y las etapas siguientes
                    }

                    all += p.StageMax[s];
                }

                // El mayor factor total que deja cada camino en target (la espera no se acelera).
                float fit = float.MaxValue;
                if (all > 0f)
                {
                    fit = Mathf.Max(0f, target) / all;
                }

                if (waits && waitPath > 0f)
                {
                    fit = Mathf.Min(fit, Mathf.Max(0f, target - wait) / waitPath);
                }

                float factor = Mathf.Max(MinBudgetSpeed, fit / speed);
                if (factor < 1f)
                {
                    speed *= factor;
                    BudgetScaled++;
                }
            }

            p.Speed = speed;
            LastSpeedFactor = speed;

            // Si aun así no cabe, saltan las etapas que acabarían después del presupuesto.
            p.SkipFrom = MechanicStepPlan.StageCount;
            for (int s = 0; s < MechanicStepPlan.StageCount; s++)
            {
                if (HasStage(p, s) && PlannedEnd(p, speed, wait, s + 1) > remaining)
                {
                    p.SkipFrom = s;
                    break;
                }
            }

            if (p.SkipFrom < MechanicStepPlan.StageCount)
            {
                BudgetOverruns++;
                _board.RaiseStepBudget(ageMs, planned, remaining, speed, p.SkipFrom);
            }
        }

        // Fin previsto (ms desde (a)) de las etapas anteriores a stages con el factor speed y la espera wait del origen, como
        // las ejecuta Tick: una etapa empieza al terminar la anterior y sus pasos que pintan el origen, además, no antes de
        // wait. Las etapas sin pasos no cuentan.
        private static float PlannedEnd(Plan p, float speed, float wait, int stages)
        {
            float end = 0f;
            for (int s = 0; s < stages; s++)
            {
                float start = end;
                end = start + (p.StageMax[s] * speed);
                if (p.StageWaitMax[s] >= 0f)
                {
                    end = Mathf.Max(end, Mathf.Max(start, wait) + (p.StageWaitMax[s] * speed));
                }
            }

            return end;
        }

        // El paso e pinta el origen (como dueña o como la otra cuia) o es el Reveal: no empieza antes de que el origen vuelva
        // a su ranura.
        private static bool WaitsForSource(Plan p, int e)
        {
            StepEntry se = p.Entries[e];
            return se.Kind == MechanicStepKind.Reveal || se.Touches(p.Source);
        }

        // Avanza el plan dtMs; devuelve true cuando ha terminado. srcDone: el origen ya volvió a su ranura.
        internal bool Tick(int slot, float dtMs, bool srcDone)
        {
            Plan p = _plans[slot];
            if (!p.InUse || !p.Begun)
            {
                return false;
            }

            p.T += dtMs;
            if (srcDone && p.SrcDoneAt < 0f)
            {
                // El instante previsto de la vuelta (el del presupuesto); el del fotograma si la nota antes.
                p.SrcDoneAt = Mathf.Min(p.T, p.SrcBackAt);
            }

            while (p.Stage < MechanicStepPlan.StageCount)
            {
                bool stageDone = true;
                float stageEnd = p.StageStart;
                for (int e = 0; e < p.Count; e++)
                {
                    if ((int)p.Entries[e].Stage != p.Stage)
                    {
                        continue;
                    }

                    if (p.State[e] == Pending)
                    {
                        bool waits = WaitsForSource(p, e);
                        if (waits && p.SrcDoneAt < 0f)
                        {
                            stageDone = false; // nada pinta el origen antes de que vuelva a su ranura (como el revelado)
                            continue;
                        }

                        StartEntry(p, e, waits ? Mathf.Max(p.StageStart, p.SrcDoneAt) : p.StageStart);
                    }

                    if (p.State[e] == Running)
                    {
                        if (p.T >= p.Start[e] + p.Dur[e])
                        {
                            FinishEntry(slot, p, e, false);
                        }
                        else
                        {
                            stageDone = false;
                            PaintDefault(p, e);
                        }
                    }

                    if (p.State[e] == Done)
                    {
                        stageEnd = Mathf.Max(stageEnd, p.Start[e] + p.Dur[e]);
                    }
                }

                if (!stageDone)
                {
                    break;
                }

                p.Stage++;
                p.StageStart = stageEnd;
            }

            return p.Stage >= MechanicStepPlan.StageCount;
        }

        // Termina el plan en el acto (SnapAll, salto por retraso): cada paso en curso con su SnapStep y cada cuia del plan en
        // su estado final. Libera el plan.
        internal void Snap(int slot)
        {
            Plan p = _plans[slot];
            if (!p.InUse)
            {
                return;
            }

            for (int e = 0; e < p.Count; e++)
            {
                if (p.State[e] == Running)
                {
                    StopEntry(p, e);
                    CloseLog(p, e, true);
                }

                p.State[e] = Done;
            }

            BoardState after = p.Visual.After;
            int touched = p.Touched;
            for (int c = 0; c < after.Count && c < 32; c++)
            {
                if (((touched >> c) & 1) != 0)
                {
                    _board.PaintStepEnd(slot, c, after[c], after.IsSealed(c), true);
                }
            }

            Release(slot);
        }

        // Libera el plan sin pintar nada.
        internal void Release(int slot)
        {
            Plan p = _plans[slot];
            p.InUse = false;
            p.Begun = false;
            p.Visual = default;
            for (int e = 0; e < p.Count; e++)
            {
                p.Overlay[e] = null;
            }

            p.Count = 0;
            p.Touched = 0;
        }

        // Duración nominal (ms, sin factor) del paso e: la del overlay, la de las marcas o la del paso por defecto.
        private float NominalMs(Plan p, int e)
        {
            StepEntry se = p.Entries[e];
            CuiaView? view = _board.ViewAt(se.Index);
            if (view == null)
            {
                return 0f;
            }

            switch (se.Kind)
            {
                case MechanicStepKind.Reveal:
                    return view.RevealStepMs(RevealStyle.Rule);
                case MechanicStepKind.MudWash:
                    return view.MudWashStepMs;
            }

            CuiaOverlay? overlay = p.Before[e].Mechanic != MechanicType.None ? view.OverlayOf(p.Before[e].Mechanic) : null;
            p.Overlay[e] = overlay;
            float ms = overlay != null ? overlay.StepDurationMs(se.Kind) : 0f;
            return ms > 0f ? ms : DefaultNominalMs(se.Kind);
        }

        // Paso por defecto (ms, sin factor).
        private float DefaultNominalMs(MechanicStepKind kind)
        {
            switch (kind)
            {
                case MechanicStepKind.Seal:
                    return CuiaView.SealMs;
                case MechanicStepKind.RainDrop:
                case MechanicStepKind.RainEnd:
                    return _board.DoseNominalMs;
                case MechanicStepKind.Bite:
                    return 2f * _board.DoseNominalMs;
                default:
                    return 0f;
            }
        }

        private void StartEntry(Plan p, int e, float start)
        {
            StepEntry se = p.Entries[e];
            CuiaView? view = _board.ViewAt(se.Index);
            bool skip = (int)se.Stage >= p.SkipFrom || view == null;
            float ms = 0f;
            p.Default[e] = false;
            p.UsesOverlay[e] = false;
            p.UsesMarks[e] = false;
            MechanicType owner = MechanicType.None;
            if (!skip)
            {
                switch (se.Kind)
                {
                    case MechanicStepKind.Reveal:
                        ms = view!.PlayReveal(se.LayerMask, RevealStyle.Rule, p.Speed);
                        p.UsesMarks[e] = true;
                        break;
                    case MechanicStepKind.MudWash:
                        ms = view!.PlayMudWash(se.LayerMask, p.Speed);
                        p.UsesMarks[e] = true;
                        break;
                    default:
                        CuiaOverlay? overlay = p.Overlay[e];
                        if (overlay != null)
                        {
                            MechanicStepContext ctx = Context(p, e);
                            owner = overlay.Type;
                            ms = overlay.PlayStep(in ctx);
                            p.UsesOverlay[e] = ms > 0f;
                        }

                        if (ms <= 0f)
                        {
                            ms = DefaultNominalMs(se.Kind) * p.Speed;
                            p.Default[e] = ms > 0f;
                        }

                        break;
                }
            }

            if (!skip)
            {
                AudioBoard.Step(se.Kind, owner); // AU1: el sonido de la mecánica, en el golpe que la dispara
            }

            p.Start[e] = start;
            p.Dur[e] = Mathf.Max(0f, ms);
            p.State[e] = Running;
            p.Log[e] = OpenLog(se, owner, p.Default[e], _board.Clock - (p.T - start), p.Dur[e], p.Speed, skip);
        }

        // Fotograma del paso por defecto en curso (sin asignaciones).
        private void PaintDefault(Plan p, int e)
        {
            if (!p.Default[e])
            {
                return;
            }

            StepEntry se = p.Entries[e];
            float dur = Mathf.Max(1f, p.Dur[e]);
            float t = Mathf.Clamp01((p.T - p.Start[e]) / dur);
            CuiaView? view = _board.ViewAt(se.Index);
            if (view == null)
            {
                return;
            }

            switch (se.Kind)
            {
                case MechanicStepKind.Seal:
                    view.SealAnimationAt(Mathf.Min(t * CuiaView.SealMs, CuiaView.SealMs - 0.01f));
                    break;

                case MechanicStepKind.RainDrop:
                case MechanicStepKind.RainEnd:
                {
                    int k = p.After[e].Count - 1;
                    if (k >= 0)
                    {
                        view.SetDose(k, Palette.Tint(se.Tint), t, se.Tint);
                    }

                    break;
                }

                case MechanicStepKind.Bite:
                {
                    Color color = Palette.Tint(se.Tint);
                    CuiaView? victim = _board.ViewAt(se.OtherIndex);
                    int kv = p.OtherBefore[e].Count - 1;
                    if (victim != null && kv >= 0)
                    {
                        victim.SetDose(kv, color, 1f - Mathf.Clamp01(t * 2f), se.Tint);
                    }

                    int kc = p.After[e].Count - 1;
                    if (kc >= 0)
                    {
                        view.SetDose(kc, color, Mathf.Clamp01((t * 2f) - 1f), se.Tint);
                    }

                    break;
                }
            }
        }

        // Fin de un paso: SnapStep del overlay o de las marcas y ShowContent(After) en las dos cuias.
        private void FinishEntry(int slot, Plan p, int e, bool snapped)
        {
            StopEntry(p, e);
            p.State[e] = Done;
            CloseLog(p, e, snapped);
            StepEntry se = p.Entries[e];
            int pending = PendingMask(slot);
            _board.PaintStepEnd(slot, se.Index, p.After[e], p.AfterSealed[e], ((pending >> se.Index) & 1) == 0);
            if (se.OtherIndex >= 0)
            {
                _board.PaintStepEnd(slot, se.OtherIndex, p.OtherAfter[e], p.OtherAfterSealed[e], ((pending >> se.OtherIndex) & 1) == 0);
            }
        }

        private void StopEntry(Plan p, int e)
        {
            if (p.UsesOverlay[e])
            {
                p.Overlay[e]?.SnapStep();
            }

            if (p.UsesMarks[e])
            {
                _board.ViewAt(p.Entries[e].Index)?.SnapMarks();
            }
        }

        private MechanicStepContext Context(Plan p, int e)
        {
            StepEntry se = p.Entries[e];
            CuiaView? view = _board.ViewAt(se.Index);
            return new MechanicStepContext(
                se.Kind,
                se.Index,
                se.OtherIndex,
                p.Before[e],
                p.After[e],
                p.AfterSealed[e],
                p.OtherBefore[e],
                p.OtherAfter[e],
                p.OtherAfterSealed[e],
                _board.Foot(se.Index),
                se.OtherIndex >= 0 ? _board.Foot(se.OtherIndex) : Vector2.zero,
                _board,
                _board.BoardBackLayer,
                _board.BoardFrontLayer,
                _board.Fx,
                view?.OverlayHost.Art,
                p.Speed,
                _board.ReduceMotion,
                se.Tint,
                se.LayerMask);
        }

        private static bool HasStage(Plan p, int stage)
        {
            for (int e = 0; e < p.Count; e++)
            {
                if ((int)p.Entries[e].Stage == stage)
                {
                    return true;
                }
            }

            return false;
        }

        private int OpenLog(StepEntry se, MechanicType owner, bool usedDefault, float startMs, float durMs, float speed, bool skipped)
        {
            int at;
            if (_logCount < LogCapacity)
            {
                at = (_logStart + _logCount) % LogCapacity;
                _logCount++;
            }
            else
            {
                at = _logStart;
                _logStart = (_logStart + 1) % LogCapacity;
            }

            _log[at] = new MechanicStepRecord(se.Kind, se.Stage, se.Index, se.OtherIndex, owner, usedDefault, startMs, startMs + durMs, speed, skipped);
            return at;
        }

        private void CloseLog(Plan p, int e, bool snapped)
        {
            int at = p.Log[e];
            if (at < 0)
            {
                return;
            }

            MechanicStepRecord r = _log[at];
            float end = snapped ? _board.Clock : r.StartMs + p.Dur[e];
            if (r.Kind == p.Entries[e].Kind && r.Index == p.Entries[e].Index)
            {
                _log[at] = new MechanicStepRecord(r.Kind, r.Stage, r.Index, r.OtherIndex, r.Owner, r.UsedDefault, r.StartMs, end, r.SpeedFactor, snapped || r.Snapped);
                _board.RaiseStepDone(in _log[at]); // AF_STATE "step" en desarrollo (BoardAnimator.DevEvent)
            }

            p.Log[e] = -1;
        }

        private static int Bit(int index)
        {
            return (uint)index < 32u ? 1 << index : 0;
        }

        // Un plan reservado: arrays fijos de MaxSteps pasos.
        private sealed class Plan
        {
            public readonly StepEntry[] Entries = new StepEntry[MechanicStepPlan.MaxSteps];
            public readonly byte[] State = new byte[MechanicStepPlan.MaxSteps];
            public readonly float[] Start = new float[MechanicStepPlan.MaxSteps];
            public readonly float[] Dur = new float[MechanicStepPlan.MaxSteps];
            public readonly float[] Nominal = new float[MechanicStepPlan.MaxSteps];
            public readonly bool[] Default = new bool[MechanicStepPlan.MaxSteps];
            public readonly bool[] UsesOverlay = new bool[MechanicStepPlan.MaxSteps];
            public readonly bool[] UsesMarks = new bool[MechanicStepPlan.MaxSteps];
            public readonly CuiaOverlay?[] Overlay = new CuiaOverlay?[MechanicStepPlan.MaxSteps];
            public readonly int[] Log = new int[MechanicStepPlan.MaxSteps];
            public readonly Cuia[] Before = new Cuia[MechanicStepPlan.MaxSteps];
            public readonly Cuia[] After = new Cuia[MechanicStepPlan.MaxSteps];
            public readonly bool[] AfterSealed = new bool[MechanicStepPlan.MaxSteps];
            public readonly Cuia[] OtherBefore = new Cuia[MechanicStepPlan.MaxSteps];
            public readonly Cuia[] OtherAfter = new Cuia[MechanicStepPlan.MaxSteps];
            public readonly bool[] OtherAfterSealed = new bool[MechanicStepPlan.MaxSteps];
            public readonly float[] StageMax = new float[MechanicStepPlan.StageCount];
            public readonly float[] StageWaitMax = new float[MechanicStepPlan.StageCount]; // −1: la etapa no espera al origen
            public bool InUse;
            public bool Begun;
            public PourVisual Visual;
            public int Source;
            public float SrcBackAt;
            public int Count;
            public int Touched;
            public float T;
            public int Stage;
            public float StageStart;
            public float SrcDoneAt = -1f;
            public float Speed = 1f;
            public int SkipFrom = MechanicStepPlan.StageCount;
        }
    }
}
