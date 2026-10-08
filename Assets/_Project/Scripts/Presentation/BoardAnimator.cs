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
    /// <para><b>Tiempo de un vertido</b> (07 §5.2 y 02 §4.1, con los tiempos de Magic Sort de D-006): el origen vuelve a su
    /// ranura en <c>max(anim_travel_ms, 0,7 · anim_travel_ms + n · anim_pour_dose_ms + anim_pour_tail_ms) +
    /// anim_return_ms</c>, es decir, <b>870 ms para 1 dosis</b>, 1.150 con 2 y 1.430 con 3 con los defaults de 08 §9 (la
    /// elevación de 100 ms ocurre antes, en el toque del origen; Magic Sort: 0,9-1,0 s de la salida al reposo, ficha
    /// §2.6). El retorno arranca cuando termina el chorro, cola incluida, y va directo a la ranura con el rebote de 0,03 u
    /// de 07 §5.2. Ver <see cref="PourDurationMs"/>.</para>
    /// <para><b>Pose</b> (D-006, punto 1, como Magic Sort): el origen viaja en arco (ease-in-out) hasta dejar el centro de su
    /// boca sobre el eje del destino, <see cref="MouthCenterAboveRimU"/> sobre su borde, y desde ahí la boca no se mueve;
    /// al salir gira por el medio del cuerpo y al llegar, alrededor de la boca (el pivote sube por el eje durante el
    /// viaje). La inclinación sube sin pararse desde la salida (<see cref="TiltAt"/>:
    /// ≈ 13° cada 100 ms con 1 dosis de una cuia llena, como Magic Sort; pasa por ≈ 40° al terminar el viaje y solo frena
    /// al final) hasta <c>92° − 9° × dosis que quedan en el origen</c> (<see cref="TiltAfterDose"/>) al terminar cada
    /// dosis, y la última, al cortarse el chorro (cola incluida: el máximo de Magic Sort llega con el final del chorro). La
    /// vuelta es sinusoidal de entrada y salida (Magic Sort: −14°, −21°, −20° y −11° cada 100 ms). El cuerpo queda del
    /// lado del que viene; escala 1,00; el origen y su chorro se dibujan delante de todo (capa de vertido, sin
    /// techo).</para>
    /// <para><b>Encadenado</b> (propuesta [P] de 02 §3.2 y <c>docs/dev/decisiones.md</c> §2.1.6): un vertido con el mismo
    /// origen, destino y tinte que el último trabajo de esas cuias no se encola aparte si ese trabajo aún no ha empezado
    /// (se suma a él) o si su chorro sigue saliendo (el caudal se alarga <c>anim_pour_dose_ms</c> por dosis). Así tres
    /// vertidos A→B seguidos no superan nunca <c>queue_max_lag_pours</c> y no hay saltos (<see cref="LagSnapCount"/>). Las
    /// dosis siguen cayendo de una en una; solo se ahorran el retorno, la elevación y el traslado intermedios.</para>
    /// </remarks>
    public sealed class BoardAnimator
    {
        /// <summary>
        /// Inclinación de Magic Sort al llegar a la boca del destino, 300 ms después de salir: 38° (ficha §2.6, 3.100 ms;
        /// D-006). El origen no se para ahí (<see cref="TiltAt"/>): es el comienzo del tramo de inclinaciones en que se
        /// comprueba que la silueta cabe en el lienzo y el mínimo de <see cref="TiltAfterDose"/>.
        /// </summary>
        public const float ArrivalTiltDeg = 38f;

        /// <summary>
        /// Fracción final de cada tramo de inclinación en que el giro frena hasta la velocidad del tramo siguiente (0 en el
        /// último): 0,3 [P]. Magic Sort gira +12°, +12°, +14°, +14°, +10° y +4° cada 100 ms: casi constante y solo frena al
        /// final (ficha §2.6).
        /// </summary>
        public const float TiltBrakeFraction = 0.3f;

        /// <summary>
        /// Encadenado (solo en <c>single</c>): el salto de inclinación que deja la curva nueva al sumar dosis a un trabajo en
        /// marcha se reparte en 100 ms, sin que el origen dé un tirón.
        /// </summary>
        public const float ChainTiltBlendMs = 100f;

        /// <summary>Inclinación al terminar la última dosis con el origen vacío: 92° (D-006; Magic Sort, t = 250).</summary>
        public const float EmptyTiltDeg = 92f;

        /// <summary>Grados que resta cada dosis que queda en el origen: 9° (D-006: 66° con 3, ≈ 92° con 0).</summary>
        public const float TiltPerRemainingDoseDeg = 9f;

        /// <summary>
        /// Altura del centro de la boca del origen sobre el borde superior del destino (u) [P] (D-006): con el borde de fibra
        /// de <see cref="CuiaGeometry.RimHalfWidthU"/>, el punto más bajo de la boca queda a 0,30-0,51 u entre 38° y 92°
        /// (Magic Sort: 0,1-0,55 u).
        /// </summary>
        public const float MouthCenterAboveRimU = 0.85f;

        /// <summary>El chorro arranca al 70 % del traslado (02 §4.1).</summary>
        public const float StreamStartFraction = 0.7f;

        /// <summary>Marea: el agua vuelve a su cuia en 250 ms (04 §2.1, VFX).</summary>
        public const float UndoTransferMs = 250f;

        /// <summary>Altura del arco que traza el centro de la boca en el traslado ("arco, no línea recta", 07 §5.2), en u.</summary>
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

        /// <summary>
        /// Desplazamiento horizontal máximo del centro de la boca respecto al eje del destino (u) [P] para que la silueta del
        /// origen no salga del lienzo cuando no cabe por ningún lado (nivel 1, cuia del medio, con u = 32): dentro de la
        /// tolerancia de ±0,10 u de D-006.
        /// </summary>
        public const float MaxMouthNudgeU = 0.09f;

        /// <summary>Rebote del retorno a la ranura (u; 07 §5.2). El de Magic Sort (≈ 50 px) no está confirmado: no se copia.</summary>
        public const float ReturnBounceU = 0.03f;

        private const int SilhouetteRows = 24;
        private const int SilhouetteCols = 6;

        private const int MaxJobs = 64;

        private readonly Job[] _jobs = new Job[MaxJobs];
        private readonly bool[] _mark = new bool[MaxJobs];
        private readonly RectTransform _streamLayer;
        private readonly RectTransform _frontLayer;
        private readonly PourStream[] _streams;
        private CuiaView[] _views = Array.Empty<CuiaView>();
        private PourTimings _timings = new PourTimings(100f, 300f, 280f, 80f, 300f);
        private int _maxLag = 2;
        private int _count;

        /// <summary>
        /// Crea la cola con un grupo de chorros reutilizables dentro de <paramref name="streamLayer"/>. Mientras vierte, el
        /// cuerpo del origen y su chorro pasan a <paramref name="frontLayer"/>, la capa de vertido alineada con el contenedor
        /// del tablero y dibujada delante de las vecinas, del marco del panel y del HUD (D-006: sin techo, como Magic Sort en
        /// t = 250); al terminar vuelven a su sitio. Marea no la usa.
        /// </summary>
        public BoardAnimator(RectTransform streamLayer, RectTransform frontLayer, int streamPool = 6)
        {
            _streamLayer = streamLayer;
            _frontLayer = frontLayer;
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
        /// Ancho del lienzo en dp (el del layout, <c>GameScreen</c>), para elegir el lado del cuerpo; si es 0, el de la capa
        /// de chorros. No depende del tamaño real de la ventana (en batchmode puede no coincidir con la pantalla simulada).
        /// </summary>
        public float BoardWidth { get; set; }

        private float Width => BoardWidth > 0f ? BoardWidth : _streamLayer.rect.width;

        /// <summary>
        /// Tiempo (ms) desde que empieza la animación de un vertido de <paramref name="amount"/> dosis hasta que el origen
        /// vuelve a su ranura, con las duraciones <paramref name="timings"/>: 870, 1.150 y 1.430 ms para 1, 2 y 3 dosis con
        /// los defaults de 08 §9 tras D-006 (07 §5.2: el retorno empieza tras la cola del chorro). Si el destino se sella, su
        /// animación (450 ms, 07 §5.3) sigue tras el fin de caudal.
        /// </summary>
        public static float PourDurationMs(PourTimings timings, int amount)
        {
            float streamEnd = (timings.TravelMs * StreamStartFraction) + (Mathf.Max(1, amount) * timings.DoseMs) + timings.TailMs;
            return Mathf.Max(timings.TravelMs, streamEnd) + timings.ReturnMs;
        }

        /// <summary>
        /// Inclinación del origen (grados, sin signo) al terminar una dosis si le quedan <paramref name="remaining"/>:
        /// <c>92° − 9° × dosis que quedan</c> (D-006; 65° con 3, 74° con 2, 83° con 1 y 92° con 0), nunca menos de
        /// <see cref="ArrivalTiltDeg"/>.
        /// </summary>
        public static float TiltAfterDose(int remaining)
        {
            return Mathf.Max(ArrivalTiltDeg, EmptyTiltDeg - (TiltPerRemainingDoseDeg * Mathf.Max(0, remaining)));
        }

        /// <summary>
        /// Inclinación del origen (grados, sin signo) a los <paramref name="timeMs"/> ms de la salida, hasta que empieza la
        /// vuelta, en un vertido de <paramref name="amount"/> dosis de una cuia con <paramref name="before"/> (D-006, como
        /// Magic Sort; ficha §2.6). Pasa por 0° en la salida, por <c>TiltAfterDose(before − k)</c> al terminar cada dosis k
        /// (<paramref name="streamStartMs"/> + k · <paramref name="doseMs"/>) y, la última, al cortarse el chorro (cola
        /// <paramref name="tailMs"/> incluida), y se queda ahí. Cada tramo gira a velocidad constante y en su último
        /// <see cref="TiltBrakeFraction"/> frena hasta la velocidad media del tramo siguiente (hasta 0 el último): sin
        /// paradas a mitad. Con los defaults de 08 §9, 1 dosis de una cuia llena: 0°, 13,4°, 26,8°, 40,2°, 53,7°, 63,1° cada
        /// 100 ms y 65° a los 570 ms (Magic Sort: 0°, 12°, 24°, 38°, 52°, 62° y 66°). No decreciente y sin asignaciones.
        /// </summary>
        public static float TiltAt(float timeMs, float streamStartMs, float doseMs, float tailMs, int before, int amount)
        {
            int n = Mathf.Max(1, amount);
            float t0 = 0f;
            float a0 = 0f;
            for (int k = 1; k <= n; k++)
            {
                float t1 = TiltKeyTime(k, n, streamStartMs, doseMs, tailMs);
                float a1 = Mathf.Max(a0, TiltAfterDose(before - k));
                if (timeMs < t1)
                {
                    float span = Mathf.Max(1f, t1 - t0);
                    float rise = a1 - a0;
                    if (rise < 1e-4f)
                    {
                        return a0;
                    }

                    // Velocidad final: la media del tramo siguiente (0 en el último), normalizada a este tramo.
                    float rEnd = 0f;
                    if (k < n)
                    {
                        float t2 = TiltKeyTime(k + 1, n, streamStartMs, doseMs, tailMs);
                        float a2 = Mathf.Max(a1, TiltAfterDose(before - k - 1));
                        rEnd = Mathf.Clamp((a2 - a1) / Mathf.Max(1f, t2 - t1) * span / rise, 0f, 2f);
                    }

                    return a0 + (rise * BrakeProfile(Mathf.Clamp01((timeMs - t0) / span), rEnd));
                }

                t0 = t1;
                a0 = a1;
            }

            return a0;
        }

        // Instante del punto k de la curva de inclinación: fin de la dosis k; el último, fin del chorro (con la cola).
        private static float TiltKeyTime(int k, int n, float streamStartMs, float doseMs, float tailMs)
        {
            return streamStartMs + (k * doseMs) + (k == n ? tailMs : 0f);
        }

        // Tramo normalizado (0 → 1 en x de 0 a 1): velocidad constante c hasta 1 − b y frenado lineal hasta rEnd en x = 1,
        // con c tal que llega a 1 (c · (1 − b/2) + rEnd · b/2 = 1). Continua en posición y velocidad.
        private static float BrakeProfile(float x, float rEnd)
        {
            const float b = TiltBrakeFraction;
            float c = (1f - (b * rEnd * 0.5f)) / (1f - (b * 0.5f));
            float x0 = 1f - b;
            if (x <= x0)
            {
                return c * x;
            }

            float d = x - x0;
            return (c * x) + ((rEnd - c) * d * d / (2f * b));
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

            float oldTilt = 0f;
            if (job.Started)
            {
                if (job.T >= job.FlowEnd)
                {
                    return false; // el caudal ya se cortó (cae la cola o el origen vuelve): el vertido se encola aparte
                }

                oldTilt = JobTilt(ref job, job.T);
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

                // La curva de inclinación cambia con las dosis nuevas: la diferencia se reparte en ChainTiltBlendMs.
                job.TiltBias = 0f;
                job.TiltBias = oldTilt - JobTilt(ref job, job.T);
                job.BiasStart = job.T;
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
            job.Before = job.SrcBefore.Count;
            job.DoseMs = _timings.DoseMs;
            job.StreamStart = _timings.TravelMs * StreamStartFraction;
            job.FlowEnd = job.StreamStart + (job.Amount * _timings.DoseMs);
            job.TiltBias = 0f;
            SetEnds(ref job);

            // Pose al terminar el viaje: la inclinación de la curva en ese instante (≈ 40° con 1 dosis de una cuia llena),
            // con la boca ya sobre el destino. El lado y el ajuste de la boca se comprueban desde 38° (o desde la de
            // llegada, si fuera menor) hasta la final.
            float arrival = JobTilt(ref job, _timings.TravelMs);
            job.MouthTarget = PourMouth(job.Source, job.Target, job.SrcAfter.Count, Mathf.Min(ArrivalTiltDeg, arrival), out job.Dir);
            job.PivotStart = s.FootPosition + job.StartOffset + new Vector2(0f, 0.5f * s.HeightU * s.Unit);
            job.PourAngle = -job.Dir * arrival;
            job.PourOffset = PoseOffsetAt(ref job, arrival);
            job.Stream = AcquireStream();

            // D-006: el origen y su chorro se dibujan delante de todo mientras vierte (sin techo).
            s.MoveBodyTo(_frontLayer);
            if (job.Stream >= 0)
            {
                _streams[job.Stream].MoveTo(_frontLayer);
            }
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

                    // Cada dosis que empieza a caer salpica y agita la superficie del destino: una por dosis (D-005: un
                    // vertido mueve la racha que cabe; con el encadenado de single, también una por dosis).
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

        // Viaje: el cuerpo gira con la inclinación de TiltAt alrededor de un pivote que va en arco (ease-in-out) de su
        // centro, en la ranura, al punto sobre el destino, y que a la vez sube por el eje del centro a la boca: al salir gira
        // por el medio (como Magic Sort, ficha §2.6: 2.900-3.100 ms) y al llegar ya gira alrededor de la boca, que llega
        // parada y sigue quieta durante el caudal, sin tirones. Vuelta directa a la ranura, sinusoidal de entrada y salida,
        // con el rebote de 0,03 u (07 §5.2).
        private Vector2 PoseOffset(ref Job job, float time, float unit)
        {
            CuiaView s = _views[job.Source];
            if (time < job.ReturnStart)
            {
                float angle = -job.Dir * JobTilt(ref job, time);
                if (time < _timings.TravelMs)
                {
                    float e = Ease.InOut(time / Mathf.Max(1f, _timings.TravelMs));
                    Vector2 pivot = Vector2.Lerp(job.PivotStart, job.MouthTarget, e);
                    pivot.y += Mathf.Sin(Mathf.PI * e) * TravelArcU * unit;
                    float along = Mathf.Lerp(0.5f, 1f, e) * s.HeightU * unit;
                    return pivot - s.FootPosition - CuiaView.Rotate(new Vector2(0f, along), angle * Mathf.Deg2Rad);
                }

                return PoseOffsetFor(s, job.MouthTarget, angle);
            }

            float r = Ease.InOutSine((time - job.ReturnStart) / Mathf.Max(1f, _timings.ReturnMs));
            Vector2 back = Vector2.Lerp(PoseOffsetAt(ref job, JobTilt(ref job, job.ReturnStart)), Vector2.zero, r);
            back.y += Mathf.Sin(Mathf.PI * r) * ReturnBounceU * unit;
            return back;
        }

        private float PoseAngle(ref Job job, float time)
        {
            if (time < job.ReturnStart)
            {
                return -job.Dir * JobTilt(ref job, time);
            }

            float last = -job.Dir * JobTilt(ref job, job.ReturnStart);
            return Mathf.Lerp(last, 0f, Ease.InOutSine((time - job.ReturnStart) / Mathf.Max(1f, _timings.ReturnMs)));
        }

        // Inclinación (sin signo) del trabajo: TiltAt con sus dosis (con el encadenado de single, cada dosis añadida suma su
        // punto) más el resto del salto del encadenado, que se reparte en ChainTiltBlendMs.
        private float JobTilt(ref Job job, float time)
        {
            float tilt = TiltAt(time, job.StreamStart, job.DoseMs, _timings.TailMs, job.Before, job.Amount);
            if (job.TiltBias != 0f)
            {
                tilt += job.TiltBias * (1f - Mathf.Clamp01((time - job.BiasStart) / ChainTiltBlendMs));
            }

            return tilt;
        }

        // Desplazamiento del cuerpo del origen que deja el centro de su boca en job.MouthTarget con la inclinación tilt
        // (sin signo; el signo lo da el lado, job.Dir).
        private Vector2 PoseOffsetAt(ref Job job, float tilt)
        {
            CuiaView s = _views[job.Source];
            return PoseOffsetFor(s, job.MouthTarget, -job.Dir * tilt);
        }

        private static Vector2 PoseOffsetFor(CuiaView s, Vector2 mouth, float angleDeg)
        {
            return mouth - s.FootPosition - CuiaView.Rotate(new Vector2(0f, s.HeightU * s.Unit), angleDeg * Mathf.Deg2Rad);
        }

        // Centro de la boca del origen durante el vertido: sobre el eje del destino, MouthCenterAboveRimU sobre su borde.
        private static Vector2 MouthTargetFor(CuiaView t)
        {
            return t.FootPosition + new Vector2(0f, (t.HeightU + MouthCenterAboveRimU) * t.Unit);
        }

        private void FinishSource(ref Job job)
        {
            CuiaView s = _views[job.Source];
            if (job.Started && !job.IsUndo)
            {
                s.SetTongue(0f, Color.clear, 0);
                s.ReturnBody();
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

        // Lado del cuerpo (D-006): dir = +1 deja el cuerpo a la izquierda del destino (gira en sentido horario y la boca
        // apunta a la derecha), −1 a la derecha. El del que viene el origen; en la misma columna (|Δx| ≤ 1 dp), hacia el
        // centro del tablero; si de fromTilt (38°) a su ángulo final (con remainingAfter dosis en el origen) la silueta
        // saldría del lienzo y por el otro lado no, el otro [P]. Sin asignaciones.
        private float ChooseSide(int source, int target, int remainingAfter, float fromTilt)
        {
            CuiaView s = _views[source];
            CuiaView t = _views[target];
            float dx = t.FootPosition.x - s.FootPosition.x;
            float dir;
            if (Mathf.Abs(dx) > 1f)
            {
                dir = dx > 0f ? 1f : -1f;
            }
            else
            {
                dir = t.FootPosition.x > Width * 0.5f ? 1f : -1f;
            }

            Vector2 mouth = MouthTargetFor(t);
            float final = TiltAfterDose(remainingAfter);
            if (!InsideAlongThePour(s, source, mouth, dir, fromTilt, final) && InsideAlongThePour(s, source, mouth, -dir, fromTilt, final))
            {
                dir = -dir;
            }

            return dir;
        }

        // La silueta queda dentro del lienzo en todas las inclinaciones del vertido sobre el destino, de fromTilt a final
        // (cada 3° y la final).
        private bool InsideAlongThePour(CuiaView s, int source, Vector2 mouth, float dir, float fromTilt, float final)
        {
            for (float tilt = fromTilt; ; tilt += 3f)
            {
                float a = -dir * Mathf.Min(tilt, final);
                if (!SilhouetteInsideBoard(source, PoseOffsetFor(s, mouth, a), a))
                {
                    return false;
                }

                if (tilt >= final)
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Pose de vertido (desplazamiento del cuerpo y ángulo en grados, negativo = sentido horario) que toma el origen
        /// <paramref name="source"/> al verter en <paramref name="target"/> con el layout actual, con la inclinación
        /// <paramref name="tiltDeg"/> (sin signo; la de <see cref="TiltAt"/> al terminar el viaje, <see cref="TiltAfterDose"/>
        /// al terminar cada dosis) y el lado que elige la cola cuando al origen le quedan <paramref name="remainingAfter"/>
        /// dosis al terminar (D-006): el centro de la boca sobre el eje del destino, <see cref="MouthCenterAboveRimU"/> sobre
        /// su borde. Solo depende de la geometría, no del contenido, así que vale para cualquier pareja de cuias (diagnóstico
        /// y tests). Sin asignaciones.
        /// </summary>
        public void ComputePourPose(int source, int target, int remainingAfter, float tiltDeg, out Vector2 offset, out float angleDeg)
        {
            if ((uint)source >= (uint)_views.Length || (uint)target >= (uint)_views.Length || source == target)
            {
                throw new ArgumentOutOfRangeException(nameof(target), target, "Origen y destino: dos cuias distintas del tablero.");
            }

            Vector2 mouth = PourMouth(source, target, remainingAfter, ArrivalTiltDeg, out float dir);
            angleDeg = -dir * tiltDeg;
            offset = PoseOffsetFor(_views[source], mouth, angleDeg);
        }

        // Centro de la boca del vertido y lado del cuerpo: sobre el eje del destino, MouthCenterAboveRimU sobre su borde; si
        // en alguna inclinación de fromTilt (38°) a la final la silueta saldría del lienzo por los dos lados, se desplaza en
        // horizontal lo justo, como mucho MaxMouthNudgeU [P]. Sin asignaciones.
        private Vector2 PourMouth(int source, int target, int remainingAfter, float fromTilt, out float dir)
        {
            CuiaView s = _views[source];
            CuiaView t = _views[target];
            dir = ChooseSide(source, target, remainingAfter, fromTilt);
            Vector2 mouth = MouthTargetFor(t);
            float final = TiltAfterDose(remainingAfter);
            float width = Width;
            float left = 0f;
            float right = 0f;
            for (float tilt = fromTilt; ; tilt += 3f)
            {
                float a = -dir * Mathf.Min(tilt, final);
                SilhouetteExtent(source, PoseOffsetFor(s, mouth, a), a, out float minX, out float maxX, out _);
                left = Mathf.Max(left, -minX);
                right = Mathf.Max(right, maxX - width);
                if (tilt >= final)
                {
                    break;
                }
            }

            float limit = MaxMouthNudgeU * t.Unit;
            mouth.x += Mathf.Clamp(left - right, -limit, limit);
            return mouth;
        }

        /// <summary>
        /// La silueta exterior del origen <paramref name="source"/>, desplazada <paramref name="offset"/> y girada
        /// <paramref name="angleDeg"/> grados, queda dentro del lienzo (contenedor del tablero: 0 ≤ x ≤ ancho, y ≤ 0, con
        /// 0,5 dp de tolerancia). Muestreo de 25 × 7 puntos con los bordes incluidos. Sin asignaciones.
        /// </summary>
        public bool SilhouetteInsideBoard(int source, Vector2 offset, float angleDeg)
        {
            SilhouetteExtent(source, offset, angleDeg, out float minX, out float maxX, out float maxY);
            return minX >= -0.5f && maxX <= Width + 0.5f && maxY <= 0.5f;
        }

        // Extremos de la silueta exterior muestreada (25 × 7 puntos con los bordes), en coordenadas del contenedor.
        private void SilhouetteExtent(int source, Vector2 offset, float angleDeg, out float minX, out float maxX, out float maxY)
        {
            CuiaView s = _views[source];
            float u = s.Unit;
            float h = s.HeightU;
            int cap = s.Capacity;
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 foot = s.FootPosition + offset;
            minX = float.MaxValue;
            maxX = float.MinValue;
            maxY = float.MinValue;
            for (int r = 0; r <= SilhouetteRows; r++)
            {
                float y = (float)r / SilhouetteRows * h;
                float hw = CuiaGeometry.OuterHalfWidth(Mathf.Min(y, h - 0.001f), cap);
                for (int c = 0; c <= SilhouetteCols; c++)
                {
                    float x = -hw + ((float)c * 2f * hw / SilhouetteCols);
                    Vector2 p = foot + CuiaView.Rotate(new Vector2(x * u, y * u), rad);
                    minX = Mathf.Min(minX, p.x);
                    maxX = Mathf.Max(maxX, p.x);
                    maxY = Mathf.Max(maxY, p.y);
                }
            }
        }

        /// <summary>
        /// Pose al terminar el viaje (boca sobre el destino) del trabajo en curso que mueve <paramref name="source"/>
        /// (diagnóstico y tests).
        /// </summary>
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
            _streams[job.Stream].MoveTo(_streamLayer);
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
            public Vector2 MouthTarget;
            public Vector2 PivotStart;
            public float TiltBias;
            public float BiasStart;
            public float Dir;
            public int Before;
            public int Stream;
            public int Splashes;
        }
    }
}
