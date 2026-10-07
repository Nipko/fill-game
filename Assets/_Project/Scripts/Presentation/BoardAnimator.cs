#nullable enable
using System;
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>Duraciones del vertido ya multiplicadas por el factor de velocidad (claves <c>anim_*</c> de 08 §9).</summary>
    public readonly struct PourTimings
    {
        /// <summary>Crea las duraciones (ms).</summary>
        public PourTimings(float liftMs, float travelMs, float doseMs, float tailMs, float returnMs)
        {
            LiftMs = liftMs;
            TravelMs = travelMs;
            DoseMs = doseMs;
            TailMs = tailMs;
            ReturnMs = returnMs;
        }

        /// <summary><c>anim_lift_ms</c>.</summary>
        public float LiftMs { get; }

        /// <summary><c>anim_travel_ms</c>.</summary>
        public float TravelMs { get; }

        /// <summary><c>anim_pour_dose_ms</c>.</summary>
        public float DoseMs { get; }

        /// <summary><c>anim_pour_tail_ms</c>.</summary>
        public float TailMs { get; }

        /// <summary><c>anim_return_ms</c>.</summary>
        public float ReturnMs { get; }
    }

    /// <summary>
    /// Cola visual de vertidos (GDD 02 §3.2; 07 §5.2): el modelo ya está actualizado y la vista lo persigue. Dos vertidos
    /// con cuias disjuntas se animan en paralelo; uno que toca una cuia ocupada espera a que esa cuia termine; si una
    /// cuia acumula más de <c>queue_max_lag_pours</c> animaciones pendientes, las anteriores saltan a su fotograma final.
    /// Sin asignaciones por fotograma (08 §5.4).
    /// </summary>
    /// <remarks>
    /// <para><b>Tiempo de un vertido</b> (07 §5.2 y 02 §4.1, sin cambios por D-001): el origen vuelve a su ranura en
    /// <c>max(anim_travel_ms, 0,7 · anim_travel_ms + n · anim_pour_dose_ms + anim_pour_tail_ms) + anim_return_ms</c>, es
    /// decir, <b>480 ms para 1 dosis</b> con los defaults de 08 §9 (la elevación de 120 ms ocurre antes, en el toque del
    /// origen). El retorno arranca cuando termina el chorro, cola incluida ("fin de caudal" y "+0,16 Retorno" del
    /// storyboard de 07 §5.2). Ver <see cref="PourDurationMs"/>.</para>
    /// <para><b>Encadenado</b> (propuesta [P] de 02 §3.2 y <c>docs/dev/decisiones.md</c> §2.1.6): un vertido con el mismo
    /// origen, destino y tinte que el último trabajo de esas cuias no se encola aparte si ese trabajo aún no ha empezado
    /// (se suma a él) o si su chorro sigue saliendo (el caudal se alarga <c>anim_pour_dose_ms</c> por dosis). Así tres
    /// vertidos A→B seguidos no superan nunca <c>queue_max_lag_pours</c> y no hay saltos (<see cref="LagSnapCount"/>). Las
    /// dosis siguen cayendo de una en una; solo se ahorran el retorno, la elevación y el traslado intermedios.</para>
    /// </remarks>
    public sealed class BoardAnimator
    {
        /// <summary>Inclinación del origen hacia el destino: 38° (02 §4.1).</summary>
        public const float TiltDeg = 38f;

        /// <summary>El labio del origen queda 0,6 u por encima del borde del destino (07 §5.2).</summary>
        public const float LipClearanceU = 0.6f;

        /// <summary>El chorro arranca al 70 % del traslado (02 §4.1).</summary>
        public const float StreamStartFraction = 0.7f;

        /// <summary>Marea: el agua vuelve a su cuia en 250 ms (04 §2.1, VFX).</summary>
        public const float UndoTransferMs = 250f;

        /// <summary>Altura del arco del traslado ("arco, no línea recta", 07 §5.2), en u.</summary>
        public const float TravelArcU = 0.4f;

        /// <summary>
        /// Aparición del chorro desde el labio (ms). El destino empieza a llenarse cuando la cabeza llega a su superficie,
        /// es decir, <see cref="StreamGrowMs"/> después del arranque del chorro (revisión visual del 2026-10-07).
        /// </summary>
        public const float StreamGrowMs = 60f;

        /// <summary>Grosor del chorro en el labio: 0,16 u (guía 6.5; revisión visual del 2026-10-07).</summary>
        public const float StreamWidthStartU = 0.16f;

        /// <summary>Grosor del chorro al llegar a la superficie del destino: 0,10 u (guía 6.5).</summary>
        public const float StreamWidthEndU = 0.10f;

        /// <summary>
        /// Punto de control de la Bézier del chorro: 0,35 u hacia fuera del labio por el lado bajo (revisión visual del
        /// 2026-10-07). El chorro sale del labio en esa dirección y se curva hacia la superficie del destino.
        /// </summary>
        public const float StreamControlU = 0.35f;

        /// <summary>Holgura del origen inclinado sobre el borde de las cuias vecinas (u; revisión visual del 2026-10-07).</summary>
        public const float NeighbourClearanceU = 0.3f;

        /// <summary>
        /// Elevación máxima del origen sobre la pose de 07 §5.2 (u): 1,0, así el labio queda entre 0,6 y 1,6 u sobre el borde
        /// del destino (revisión visual del 2026-10-07: con 5 u de margen la búsqueda lo subía 2,1 u para tapar menos y el
        /// chorro caía desde muy alto). A 38° el origen puede tapar algo de una vecina durante el vertido (≤ 400 ms).
        /// </summary>
        public const float MaxRaiseU = 1f;

        private const float RaiseStepU = 0.25f;
        private const float RaisePenalty = 0.4f;   // u² de tapado que compensa subir 1 u
        private const float LipShiftPenalty = 0.5f; // u² por u de desplazamiento del labio más allá de 0,25 u

        // Distancias (u) del centro del labio al eje del destino, hacia el origen (0,25 u es la pose de partida).
        private static readonly float[] LipShiftsU = { 0.25f, 0.45f, 0.65f };
        private const float FarSidePenalty = 0.5f; // u²: verter desde el otro lado del destino
        private const int SilhouetteRows = 24;
        private const int SilhouetteCols = 6;

        private const int MaxJobs = 64;

        private readonly Job[] _jobs = new Job[MaxJobs];
        private readonly bool[] _mark = new bool[MaxJobs];
        private readonly RectTransform _streamLayer;
        private readonly PourStream[] _streams;
        private CuiaView[] _views = Array.Empty<CuiaView>();
        private PourTimings _timings = new PourTimings(120f, 200f, 100f, 80f, 160f);
        private int _maxLag = 2;
        private int _count;

        /// <summary>Crea la cola con un grupo de chorros reutilizables dentro de <paramref name="streamLayer"/>.</summary>
        public BoardAnimator(RectTransform streamLayer, int streamPool = 6)
        {
            _streamLayer = streamLayer;
            _streams = new PourStream[streamPool];
            for (int i = 0; i < streamPool; i++)
            {
                _streams[i] = new PourStream(streamLayer, i);
            }
        }

        /// <summary>
        /// Efectos de la caída de cada dosis (salpicadura y onda, guía visual v2 6.6); null = sin efectos. No cambian
        /// tiempos ni el modelo.
        /// </summary>
        public IBoardFx? Fx { get; set; }

        /// <summary>La cola está vacía (la vista ya muestra el modelo).</summary>
        public bool IsIdle => _count == 0;

        /// <summary>Trabajos en cola o en curso.</summary>
        public int PendingCount => _count;

        /// <summary>
        /// Trabajos que la cola llevó a su fotograma final por superar <c>queue_max_lag_pours</c> o su capacidad: saltos
        /// visibles (diagnóstico y tests; criterio A9 de <c>docs/dev/decisiones.md</c>). <see cref="SnapAll"/> no cuenta.
        /// </summary>
        public int LagSnapCount { get; private set; }

        /// <summary>Vertidos que se sumaron a un trabajo igual en lugar de encolarse aparte (encadenado; diagnóstico y tests).</summary>
        public int ChainedCount { get; private set; }

        /// <summary>
        /// Techo para el labio del origen, en coordenadas del contenedor (y hacia arriba): el borde inferior del HUD. El
        /// origen nunca sube por encima al apartarse de las vecinas.
        /// </summary>
        public float TopLimitY { get; set; } = float.MaxValue;

        /// <summary>
        /// Tiempo (ms) desde que empieza la animación de un vertido de <paramref name="amount"/> dosis hasta que el origen
        /// vuelve a su ranura, con las duraciones <paramref name="timings"/>: 480 ms para 1 dosis con los defaults de 08 §9
        /// (07 §5.2: el retorno empieza tras la cola del chorro). Si el destino se sella, su animación (450 ms, 07 §5.3)
        /// sigue tras el fin de caudal.
        /// </summary>
        public static float PourDurationMs(PourTimings timings, int amount)
        {
            float streamEnd = (timings.TravelMs * StreamStartFraction) + (Mathf.Max(1, amount) * timings.DoseMs) + timings.TailMs;
            return Mathf.Max(timings.TravelMs, streamEnd) + timings.ReturnMs;
        }

        /// <summary>Asocia las vistas del nivel cargado y las duraciones.</summary>
        public void Bind(CuiaView[] views, PourTimings timings, int queueMaxLagPours)
        {
            SnapAll();
            _views = views;
            _timings = timings;
            _maxLag = Mathf.Max(1, queueMaxLagPours);
        }

        /// <summary>
        /// Encola la animación de un vertido ya aplicado en el modelo, o la encadena a un trabajo igual (mismo origen,
        /// destino y tinte) que aún no ha empezado o cuyo chorro sigue saliendo (ver la clase).
        /// </summary>
        public void EnqueuePour(int source, int target, int amount, int tint, Cuia sourceBefore, Cuia sourceAfter, bool sourceSealedAfter, Cuia targetBefore, Cuia targetAfter, bool targetSealedAfter)
        {
            if (TryChain(source, target, amount, tint, sourceBefore, sourceAfter, sourceSealedAfter, targetBefore, targetAfter, targetSealedAfter))
            {
                ChainedCount++;
                return;
            }

            Enqueue(new Job
            {
                Source = source,
                Target = target,
                Amount = amount,
                Tint = tint,
                IsUndo = false,
                SrcBefore = sourceBefore,
                SrcAfter = sourceAfter,
                SrcSealedAfter = sourceSealedAfter,
                TgtBefore = targetBefore,
                TgtAfter = targetAfter,
                TgtSealedAfter = targetSealedAfter,
                Stream = -1,
            });
        }

        /// <summary>
        /// Encola Marea (02 §14: la cola ya debe estar resuelta con <see cref="SnapAll"/>): las dosis vuelven de
        /// <paramref name="from"/> (el destino del vertido deshecho) a <paramref name="to"/> en 250 ms y la tapa salta.
        /// </summary>
        public void EnqueueUndo(int from, int to, int amount, int tint, Cuia fromBefore, Cuia fromAfter, bool fromSealedAfter, Cuia toBefore, Cuia toAfter, bool toSealedAfter)
        {
            Enqueue(new Job
            {
                Source = from,
                Target = to,
                Amount = amount,
                Tint = tint,
                IsUndo = true,
                SrcBefore = fromBefore,
                SrcAfter = fromAfter,
                SrcSealedAfter = fromSealedAfter,
                TgtBefore = toBefore,
                TgtAfter = toAfter,
                TgtSealedAfter = toSealedAfter,
                Stream = -1,
            });
        }

        /// <summary>Lleva todos los trabajos a su fotograma final (Marea, reinicio, vuelta de segundo plano).</summary>
        public void SnapAll()
        {
            for (int i = 0; i < _count; i++)
            {
                Complete(ref _jobs[i]);
            }

            _count = 0;
        }

        /// <summary>Avanza la cola <paramref name="dtMs"/> milisegundos.</summary>
        public void Tick(float dtMs)
        {
            for (int i = 0; i < _count; i++)
            {
                if (!_jobs[i].Started && CanStart(i))
                {
                    Start(ref _jobs[i]);
                }
            }

            for (int i = 0; i < _count; i++)
            {
                if (_jobs[i].Started)
                {
                    Advance(ref _jobs[i], dtMs);
                }
            }

            Compact();
        }

        // Encadenado: el último trabajo que toca el origen o el destino es un vertido igual (no Marea) que aún no ha
        // empezado o cuyo chorro sigue saliendo; las dosis nuevas se suman a él y su caudal se alarga. Sin asignaciones.
        private bool TryChain(int source, int target, int amount, int tint, Cuia sourceBefore, Cuia sourceAfter, bool sourceSealedAfter, Cuia targetBefore, Cuia targetAfter, bool targetSealedAfter)
        {
            if (amount <= 0)
            {
                return false;
            }

            int last = -1;
            for (int i = _count - 1; i >= 0; i--)
            {
                if (_jobs[i].Source == source || _jobs[i].Target == source || _jobs[i].Source == target || _jobs[i].Target == target)
                {
                    last = i;
                    break;
                }
            }

            if (last < 0)
            {
                return false;
            }

            ref Job job = ref _jobs[last];
            if (job.IsUndo || job.Source != source || job.Target != target || job.Tint != tint || job.SrcDone || job.TgtDone)
            {
                return false;
            }

            // El trabajo debe terminar justo donde empieza este vertido; si no, la vista se desincronizaría del modelo.
            if (!job.SrcAfter.Equals(sourceBefore) || !job.TgtAfter.Equals(targetBefore))
            {
                return false;
            }

            if (job.Started)
            {
                if (job.T >= job.FlowEnd)
                {
                    return false; // el caudal ya se cortó (cae la cola o el origen vuelve): el vertido se encola aparte
                }

                job.FlowEnd += amount * job.DoseMs;
            }

            job.Amount += amount;
            job.SrcAfter = sourceAfter;
            job.SrcSealedAfter = sourceSealedAfter;
            job.TgtAfter = targetAfter;
            job.TgtSealedAfter = targetSealedAfter;
            if (job.Started)
            {
                SetEnds(ref job);
            }

            return true;
        }

        private void Enqueue(Job job)
        {
            if (_count == MaxJobs)
            {
                LagSnapCount++;
                SnapThrough(0);
            }

            _jobs[_count++] = job;
            EnforceLag(job.Source);
            EnforceLag(job.Target);
        }

        private void EnforceLag(int cuia)
        {
            while (true)
            {
                int involving = 0;
                int oldest = -1;
                for (int i = 0; i < _count; i++)
                {
                    if (_jobs[i].Source == cuia || _jobs[i].Target == cuia)
                    {
                        involving++;
                        if (oldest < 0)
                        {
                            oldest = i;
                        }
                    }
                }

                if (involving <= _maxLag || oldest < 0)
                {
                    return;
                }

                LagSnapCount++;
                SnapThrough(oldest);
            }
        }

        // Lleva al final el trabajo index y todos los anteriores que comparten cuias con él (transitivamente), en orden.
        private void SnapThrough(int index)
        {
            int mask = CuiaMask(ref _jobs[index]);
            for (int i = 0; i < _count; i++)
            {
                _mark[i] = false;
            }

            _mark[index] = true;
            for (int i = index - 1; i >= 0; i--)
            {
                int m = CuiaMask(ref _jobs[i]);
                if ((m & mask) != 0)
                {
                    _mark[i] = true;
                    mask |= m;
                }
            }

            for (int i = 0; i <= index; i++)
            {
                if (_mark[i])
                {
                    Complete(ref _jobs[i]);
                }
            }

            Compact();
        }

        private static int CuiaMask(ref Job job)
        {
            return (1 << job.Source) | (1 << job.Target);
        }

        private bool CanStart(int index)
        {
            int source = _jobs[index].Source;
            int target = _jobs[index].Target;
            for (int i = 0; i < index; i++)
            {
                if (Holds(ref _jobs[i], source) || Holds(ref _jobs[i], target))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Holds(ref Job job, int cuia)
        {
            if (job.Source == cuia)
            {
                return !job.Started || !job.SrcDone;
            }

            if (job.Target == cuia)
            {
                return !job.Started || !job.TgtDone;
            }

            return false;
        }

        private void Start(ref Job job)
        {
            job.Started = true;
            job.T = 0f;
            CuiaView s = _views[job.Source];
            CuiaView t = _views[job.Target];
            if (job.IsUndo)
            {
                job.DoseMs = UndoTransferMs / Mathf.Max(1, job.Amount);
                job.StreamStart = 0f;
                job.FlowEnd = UndoTransferMs;
                job.StreamEnd = UndoTransferMs;
                job.ReturnStart = UndoTransferMs;
                job.SrcEnd = UndoTransferMs;
                job.TgtEnd = UndoTransferMs;
                if (s.Sealed)
                {
                    s.SetSealed(false); // la tapa salta: el sello se reabre (04 §2.1)
                }

                return;
            }

            s.Lock();
            job.StartOffset = s.BodyOffset;
            ChoosePourPose(ref job);

            job.DoseMs = _timings.DoseMs;
            job.StreamStart = _timings.TravelMs * StreamStartFraction;
            job.FlowEnd = job.StreamStart + (job.Amount * _timings.DoseMs);
            SetEnds(ref job);
            job.Stream = AcquireStream();

            s.Root.SetAsLastSibling();
            _streamLayer.SetAsLastSibling();
        }

        // Fin de caudal: cola del chorro, retorno del origen (07 §5.2: arranca cuando termina el chorro, cola incluida) y
        // fin del destino (con el sellado de 07 §5.3 si lo hay).
        private void SetEnds(ref Job job)
        {
            job.StreamEnd = job.FlowEnd + _timings.TailMs;
            job.ReturnStart = Mathf.Max(_timings.TravelMs, job.StreamEnd);
            job.SrcEnd = job.ReturnStart + _timings.ReturnMs;
            job.TgtEnd = job.FlowEnd + StreamGrowMs + (job.TgtSealedAfter ? CuiaView.SealMs : 0f);
        }

        private void Advance(ref Job job, float dtMs)
        {
            job.T += dtMs;
            float time = job.T;
            CuiaView s = _views[job.Source];
            CuiaView t = _views[job.Target];
            Color color = Palette.Tint(job.Tint);
            float p = Mathf.Clamp((time - job.StreamStart) / Mathf.Max(1f, job.DoseMs), 0f, job.Amount);

            // El destino se llena cuando la cabeza del chorro llega a su superficie (Marea no tiene chorro).
            float fillDelay = job.IsUndo ? 0f : StreamGrowMs;
            float pt = Mathf.Clamp((time - job.StreamStart - fillDelay) / Mathf.Max(1f, job.DoseMs), 0f, job.Amount);

            // La pose del origen se fija antes de dibujar el chorro: así el chorro sale del labio de este fotograma y no
            // del anterior (con fotogramas largos el retraso se veía como un chorro que nacía por encima del labio).
            if (!job.IsUndo && !job.SrcDone && time < job.SrcEnd)
            {
                s.SetPose(PoseOffset(ref job, time, s.Unit), PoseAngle(ref job, time));
            }

            if (!job.IsUndo && job.Stream >= 0)
            {
                PourStream stream = _streams[job.Stream];
                if (time >= job.StreamStart && time < job.StreamEnd)
                {
                    float u = t.Unit;
                    StreamCurve(s, t, out Vector2 lip, out Vector2 control, out Vector2 surface);
                    float head = Mathf.Clamp01((time - job.StreamStart) / StreamGrowMs);
                    float tail = time > job.FlowEnd ? Mathf.Clamp01((time - job.FlowEnd) / Mathf.Max(1f, _timings.TailMs)) : 0f;
                    stream.Draw(lip, control, surface, tail, head, color, StreamWidthStartU * s.Unit, StreamWidthEndU * u);
                    if (!job.SrcDone)
                    {
                        s.SetTongue(head * (1f - tail), color, job.Tint); // lengua por la pared inferior hasta el labio
                    }

                    // Cada dosis que empieza a caer salpica y agita la superficie del destino (una dosis por vertido,
                    // D-001; con el encadenado, una por dosis).
                    if (time >= job.StreamStart + StreamGrowMs)
                    {
                        int due = Mathf.Min(job.Amount, 1 + Mathf.FloorToInt(Mathf.Max(0f, p)));
                        while (job.Splashes < due)
                        {
                            job.Splashes++;
                            t.Ripple();
                            Fx?.Splash(surface, color, u);
                        }
                    }
                }
                else if (time >= job.StreamEnd)
                {
                    ReleaseStream(ref job);
                    if (!job.SrcDone)
                    {
                        s.SetTongue(0f, color, job.Tint);
                    }
                }
            }

            if (!job.SrcDone)
            {
                if (time >= job.SrcEnd)
                {
                    FinishSource(ref job);
                }
                else
                {
                    int top = job.SrcBefore.Count - 1;
                    for (int i = 0; i < job.Amount; i++)
                    {
                        s.SetDose(top - i, color, 1f - Mathf.Clamp01(p - i), job.Tint);
                    }
                }
            }

            if (!job.TgtDone)
            {
                if (time >= job.TgtEnd)
                {
                    FinishTarget(ref job);
                }
                else
                {
                    int bottom = job.TgtBefore.Count;
                    for (int i = 0; i < job.Amount; i++)
                    {
                        t.SetDose(bottom + i, color, Mathf.Clamp01(pt - i), job.Tint);
                    }

                    float flowArrived = job.FlowEnd + fillDelay;
                    if (job.TgtSealedAfter && time >= flowArrived)
                    {
                        t.SealAnimationAt(time - flowArrived);
                    }
                }
            }
        }

        /// <summary>
        /// Curva del chorro de <paramref name="s"/> a <paramref name="t"/> con la pose actual (revisión visual del
        /// 2026-10-07; guía 6.5): nace en el labio, en el canto interior del lado bajo, con el remate redondo tangente a él (el
        /// centro del remate queda medio grosor hacia fuera y medio grosor hacia dentro del labio, sin cuña que asome); el
        /// control está <see cref="StreamControlU"/> hacia fuera del labio y el final, en la superficie del líquido del
        /// destino (el fondo de la cavidad si está vacío), dentro de su boca.
        /// </summary>
        public static void StreamCurve(CuiaView s, CuiaView t, out Vector2 start, out Vector2 control, out Vector2 end)
        {
            float su = s.Unit;
            float half = 0.5f * StreamWidthStartU * su;
            Vector2 outward = s.PourOutward();
            Vector2 up = s.AxisUp();
            start = s.PourPointInContainer() + (outward * half) - (up * half);
            control = start + (outward * (StreamControlU * su));
            Vector2 surface = t.SurfaceInContainer();
            float mouth = (CuiaGeometry.InnerHalfWidth(CuiaGeometry.LipU(t.Capacity), t.Capacity) - 0.15f) * t.Unit;
            end = new Vector2(Mathf.Clamp(control.x, surface.x - mouth, surface.x + mouth), surface.y);
        }

        private Vector2 PoseOffset(ref Job job, float time, float unit)
        {
            if (time < _timings.TravelMs)
            {
                float s = Ease.InOut(time / Mathf.Max(1f, _timings.TravelMs));
                Vector2 o = Vector2.Lerp(job.StartOffset, job.PourOffset, s);
                o.y += Mathf.Sin(Mathf.PI * s) * TravelArcU * unit;
                return o;
            }

            if (time < job.ReturnStart)
            {
                return job.PourOffset;
            }

            float r = Ease.Out((time - job.ReturnStart) / Mathf.Max(1f, _timings.ReturnMs));
            Vector2 back = Vector2.Lerp(job.PourOffset, Vector2.zero, r);
            back.y += Mathf.Sin(Mathf.PI * r) * 0.03f * unit; // rebote de 0,03 u (07 §5.2)
            return back;
        }

        private float PoseAngle(ref Job job, float time)
        {
            if (time < _timings.TravelMs)
            {
                return Mathf.Lerp(0f, job.PourAngle, Ease.InOut(time / Mathf.Max(1f, _timings.TravelMs)));
            }

            if (time < job.ReturnStart)
            {
                return job.PourAngle;
            }

            return Mathf.Lerp(job.PourAngle, 0f, Ease.Out((time - job.ReturnStart) / Mathf.Max(1f, _timings.ReturnMs)));
        }

        private void FinishSource(ref Job job)
        {
            CuiaView s = _views[job.Source];
            if (job.Started && !job.IsUndo)
            {
                s.SetTongue(0f, Color.clear, 0);
                s.Unlock();
            }

            s.ShowContent(job.SrcAfter, job.SrcSealedAfter);
            job.SrcDone = true;
            ReleaseStream(ref job);
        }

        private void FinishTarget(ref Job job)
        {
            _views[job.Target].ShowContent(job.TgtAfter, job.TgtSealedAfter);
            job.TgtDone = true;
        }

        private void Complete(ref Job job)
        {
            if (!job.SrcDone)
            {
                FinishSource(ref job);
            }

            if (!job.TgtDone)
            {
                FinishTarget(ref job);
            }
        }

        private void Compact()
        {
            int w = 0;
            for (int i = 0; i < _count; i++)
            {
                if (_jobs[i].SrcDone && _jobs[i].TgtDone)
                {
                    continue;
                }

                if (w != i)
                {
                    _jobs[w] = _jobs[i];
                }

                w++;
            }

            for (int i = w; i < _count; i++)
            {
                _jobs[i] = default;
            }

            _count = w;
        }

        // Pose de vertido (07 §5.2: labio 0,6 u sobre el borde del destino; 38° de 02 §4.1) elegida para tapar lo menos
        // posible (revisiones visuales del 2026-10-07): se prueban los dos lados del destino, el centro del labio a 0,25,
        // 0,45 o 0,65 u del eje del destino hacia el origen (el canto por el que sale el chorro sigue sobre su boca) y
        // elevaciones de 0 a 1 u (el labio nunca pasa de 1,6 u sobre el borde del destino). Gana la de menor coste = área
        // tapada, ponderada (silueta del origen sobre el cuerpo de las demás, × 3, y sobre su hombro, cuello y la holgura
        // de 0,3 u por encima de su borde, × 2; dentro de la silueta del destino por debajo de su borde, × 3) + 0,4 u² por
        // u de elevación + 0,5 u² por u de desplazamiento del labio más allá de 0,25 u + 0,5 u² si vierte desde el lado
        // lejano. Sin asignaciones.
        private void ChoosePourPose(ref Job job)
        {
            CuiaView s = _views[job.Source];
            CuiaView t = _views[job.Target];
            float dx = t.FootPosition.x - s.FootPosition.x;
            float preferred;
            if (Mathf.Abs(dx) > 1f)
            {
                preferred = dx > 0f ? 1f : -1f;
            }
            else
            {
                preferred = t.FootPosition.x > _streamLayer.rect.width * 0.5f ? -1f : 1f;
            }

            float bestCost = float.MaxValue;
            float length = s.HeightU * s.Unit;
            Vector2 rim = t.FootPosition + new Vector2(0f, t.HeightU * t.Unit);
            int steps = Mathf.RoundToInt(MaxRaiseU / RaiseStepU);
            for (int side = 0; side < 2; side++)
            {
                float dir = side == 0 ? preferred : -preferred;
                float angle = -dir * TiltDeg;
                float rad = angle * Mathf.Deg2Rad;
                var lipVector = new Vector2(-length * Mathf.Sin(rad), length * Mathf.Cos(rad));
                for (int shift = 0; shift < LipShiftsU.Length; shift++)
                {
                    float lipShift = LipShiftsU[shift];
                    Vector2 lipTarget = rim + new Vector2(-dir * lipShift * t.Unit, LipClearanceU * t.Unit);
                    Vector2 baseOffset = lipTarget - lipVector - s.FootPosition;
                    float fixedCost = (LipShiftPenalty * (lipShift - LipShiftsU[0])) + (side == 1 ? FarSidePenalty : 0f);
                    for (int step = 0; step <= steps; step++)
                    {
                        float raise = step * RaiseStepU * s.Unit;
                        if (step > 0 && lipTarget.y + raise > TopLimitY)
                        {
                            break;
                        }

                        Vector2 offset = baseOffset + new Vector2(0f, raise);
                        float cost = PoseCost(job.Source, job.Target, offset, rad) + (RaisePenalty * step * RaiseStepU) + fixedCost;
                        if (cost < bestCost - 1e-4f)
                        {
                            bestCost = cost;
                            job.PourOffset = offset;
                            job.PourAngle = angle;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Área ponderada (u²) de la silueta del origen <paramref name="source"/>, desplazada <paramref name="offset"/> y
        /// girada <paramref name="radians"/>, que tapa a las demás cuias, sale del tablero por los lados o sube sobre el HUD.
        /// </summary>
        public float PoseCost(int source, int target, Vector2 offset, float radians)
        {
            CuiaView s = _views[source];
            float u = s.Unit;
            float h = s.HeightU;
            int cap = s.Capacity;
            Vector2 foot = s.FootPosition + offset;
            float width = _streamLayer.rect.width;
            float cost = 0f;

            // Muestreo con los bordes incluidos (regla del trapecio): el contorno es lo que toca a las vecinas.
            for (int r = 0; r <= SilhouetteRows; r++)
            {
                float y = (float)r / SilhouetteRows * h;
                float hw = CuiaGeometry.OuterHalfWidth(Mathf.Min(y, h - 0.001f), cap);
                if (hw <= 0f)
                {
                    continue;
                }

                float area = (2f * hw / SilhouetteCols) * (h / SilhouetteRows) * (r == 0 || r == SilhouetteRows ? 0.5f : 1f);
                for (int c = 0; c <= SilhouetteCols; c++)
                {
                    float x = -hw + ((float)c * 2f * hw / SilhouetteCols);
                    Vector2 p = foot + CuiaView.Rotate(new Vector2(x * u, y * u), radians);
                    cost += area * (c == 0 || c == SilhouetteCols ? 0.5f : 1f) * Occlusion(p, source, target, width);
                }
            }

            return cost;
        }

        /// <summary>Pose de vertido del trabajo en curso que mueve <paramref name="source"/> (diagnóstico y tests).</summary>
        public bool TryGetPourPose(int source, out Vector2 offset, out float angleDeg)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_jobs[i].Started && !_jobs[i].IsUndo && _jobs[i].Source == source)
                {
                    offset = _jobs[i].PourOffset;
                    angleDeg = _jobs[i].PourAngle;
                    return true;
                }
            }

            offset = Vector2.zero;
            angleDeg = 0f;
            return false;
        }

        // Peso del tapado en el punto p: 3 sobre el cuerpo (cavidad) de otra cuia, 2 sobre su hombro, cuello y la holgura
        // de 0,3 u sobre su borde; en el destino, 3 dentro de su silueta por debajo de su borde (el origen no se mete en su
        // boca ni lo tapa; fuera de la silueta, junto al cuello, no tapa nada) y nada por encima del borde; 3 fuera del
        // tablero o sobre el HUD. Con 0,4 por u de elevación, subir compensa en cuanto evita tapar algo.
        private float Occlusion(Vector2 p, int source, int target, float width)
        {
            if (p.x < 0f || p.x > width || p.y > TopLimitY)
            {
                return 3f;
            }

            float w = 0f;
            for (int j = 0; j < _views.Length; j++)
            {
                if (j == source)
                {
                    continue;
                }

                CuiaView v = _views[j];
                float vu = v.Unit;
                Vector2 f = v.FootPosition;
                if (p.x < f.x - vu || p.x > f.x + vu || p.y < f.y)
                {
                    continue;
                }

                if (j == target)
                {
                    // Destino: el origen no entra en su silueta (el chorro sí); por encima del borde, libre.
                    if (p.y <= f.y + (v.HeightU * vu) && Mathf.Abs(p.x - f.x) < CuiaGeometry.OuterHalfWidth((p.y - f.y) / vu, v.Capacity) * vu)
                    {
                        w = Mathf.Max(w, 3f);
                    }

                    continue;
                }

                float body = f.y + (CuiaGeometry.CavityTopU(v.Capacity) * vu);
                float top = f.y + ((v.HeightU + NeighbourClearanceU) * vu);
                if (p.y > top)
                {
                    continue;
                }

                w = Mathf.Max(w, p.y <= body ? 3f : 2f);
            }

            return w;
        }

        private int AcquireStream()
        {
            for (int i = 0; i < _streams.Length; i++)
            {
                if (!_streams[i].InUse)
                {
                    _streams[i].InUse = true;
                    return i;
                }
            }

            return -1;
        }

        private void ReleaseStream(ref Job job)
        {
            if (job.Stream < 0)
            {
                return;
            }

            _streams[job.Stream].Hide();
            _streams[job.Stream].InUse = false;
            job.Stream = -1;
        }

        private struct Job
        {
            public int Source;
            public int Target;
            public int Amount;
            public int Tint;
            public bool IsUndo;
            public Cuia SrcBefore;
            public Cuia SrcAfter;
            public bool SrcSealedAfter;
            public Cuia TgtBefore;
            public Cuia TgtAfter;
            public bool TgtSealedAfter;
            public bool Started;
            public bool SrcDone;
            public bool TgtDone;
            public float T;
            public float DoseMs;
            public float StreamStart;
            public float FlowEnd;
            public float StreamEnd;
            public float ReturnStart;
            public float SrcEnd;
            public float TgtEnd;
            public Vector2 StartOffset;
            public Vector2 PourOffset;
            public float PourAngle;
            public int Stream;
            public int Splashes;
        }
    }
}
