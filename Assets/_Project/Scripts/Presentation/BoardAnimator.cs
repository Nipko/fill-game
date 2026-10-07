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

        /// <summary>Aparición del chorro desde el labio (ms).</summary>
        public const float StreamGrowMs = 60f;

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

        /// <summary>La cola está vacía (la vista ya muestra el modelo).</summary>
        public bool IsIdle => _count == 0;

        /// <summary>Trabajos en cola o en curso.</summary>
        public int PendingCount => _count;

        /// <summary>Asocia las vistas del nivel cargado y las duraciones.</summary>
        public void Bind(CuiaView[] views, PourTimings timings, int queueMaxLagPours)
        {
            SnapAll();
            _views = views;
            _timings = timings;
            _maxLag = Mathf.Max(1, queueMaxLagPours);
        }

        /// <summary>Encola la animación de un vertido ya aplicado en el modelo.</summary>
        public void EnqueuePour(int source, int target, int amount, int tint, Cuia sourceBefore, Cuia sourceAfter, bool sourceSealedAfter, Cuia targetBefore, Cuia targetAfter, bool targetSealedAfter)
        {
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

        private void Enqueue(Job job)
        {
            if (_count == MaxJobs)
            {
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
            float dx = t.FootPosition.x - s.FootPosition.x;
            float dir;
            if (Mathf.Abs(dx) > 1f)
            {
                dir = dx > 0f ? 1f : -1f;
            }
            else
            {
                dir = t.FootPosition.x > _streamLayer.rect.width * 0.5f ? -1f : 1f;
            }

            float angle = -dir * TiltDeg;
            float rad = angle * Mathf.Deg2Rad;
            float length = s.HeightU * s.Unit;
            var lipVector = new Vector2(-length * Mathf.Sin(rad), length * Mathf.Cos(rad));
            Vector2 rim = t.FootPosition + new Vector2(0f, t.HeightU * t.Unit);
            Vector2 lipTarget = rim + new Vector2(-dir * 0.25f * t.Unit, LipClearanceU * t.Unit);
            job.PourOffset = lipTarget - lipVector - s.FootPosition;
            job.PourAngle = angle;

            job.DoseMs = _timings.DoseMs;
            job.StreamStart = _timings.TravelMs * StreamStartFraction;
            job.FlowEnd = job.StreamStart + (job.Amount * _timings.DoseMs);
            job.StreamEnd = job.FlowEnd + _timings.TailMs;
            job.ReturnStart = Mathf.Max(_timings.TravelMs, job.StreamEnd);
            job.SrcEnd = job.ReturnStart + _timings.ReturnMs;
            job.TgtEnd = job.FlowEnd + (job.TgtSealedAfter ? CuiaView.SealMs : 0f);
            job.Stream = AcquireStream();

            s.Root.SetAsLastSibling();
            _streamLayer.SetAsLastSibling();
        }

        private void Advance(ref Job job, float dtMs)
        {
            job.T += dtMs;
            float time = job.T;
            CuiaView s = _views[job.Source];
            CuiaView t = _views[job.Target];
            Color color = Palette.Tint(job.Tint);
            float p = Mathf.Clamp((time - job.StreamStart) / Mathf.Max(1f, job.DoseMs), 0f, job.Amount);

            if (!job.IsUndo && job.Stream >= 0)
            {
                PourStream stream = _streams[job.Stream];
                if (time >= job.StreamStart && time < job.StreamEnd)
                {
                    float u = t.Unit;
                    Vector2 lip = s.LipInContainer();
                    Vector2 surface = t.SurfaceInContainer();
                    var control = new Vector2(surface.x, lip.y);
                    float head = Mathf.Clamp01((time - job.StreamStart) / StreamGrowMs);
                    float tail = time > job.FlowEnd ? Mathf.Clamp01((time - job.FlowEnd) / Mathf.Max(1f, _timings.TailMs)) : 0f;
                    stream.Draw(lip, control, surface, tail, head, color, 0.16f * u, 0.10f * u);
                }
                else if (time >= job.StreamEnd)
                {
                    ReleaseStream(ref job);
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
                    if (!job.IsUndo)
                    {
                        s.SetPose(PoseOffset(ref job, time, s.Unit), PoseAngle(ref job, time));
                    }

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
                        t.SetDose(bottom + i, color, Mathf.Clamp01(p - i), job.Tint);
                    }

                    if (job.TgtSealedAfter && time >= job.FlowEnd)
                    {
                        t.SealAnimationAt(time - job.FlowEnd);
                    }
                }
            }
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
        }
    }
}
