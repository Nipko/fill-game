#nullable enable
using System;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Presentation.Mechanics;
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
    /// dosis siguen cayendo de una en una; solo se ahorran el retorno, la elevación y el traslado intermedios. No se encadena
    /// un vertido con pasos de mecánica (Remolino, Barro, otras cuias que cambian o Aguas turbias reveladas).</para>
    /// <para><b>Mecánicas</b> (arquitectura del paso 1, §4, ola 2): cada vertido llega como <see cref="PourVisual"/>, con los
    /// tableros antes y después y las etapas de R10. Si tiene pasos de mecánica (<see cref="MechanicStepPlan"/>), al terminar
    /// el caudal (<c>FlowEnd + StreamGrowMs</c>) los ejecuta su <see cref="MechanicStepQueue"/> por etapas, en el orden de R10
    /// con el punto fijo f′, y el trabajo deja el origen y la tocada en el estado previo a sus pasos
    /// (<see cref="StepStaging.JobEnd"/>). Las cuias que cambiaron sin paso saltan a su estado final al llegar el caudal. Un
    /// cambio solo de contador (la Lluvia o la cueva que cuentan un vertido útil) no retiene la cuia ni impide el encadenado: se
    /// repinta en el acto si ningún trabajo la usa y, si no, al terminar el último que la usa (lo pinta con el estado del
    /// modelo). Ninguna cuia queda desfasada del modelo y todas cuentan para la espera y el retraso máximo de 02 §3.2.
    /// <see cref="ShownOf"/> dice lo último que se pintó en cada vista; con la cola quieta, el modelo.</para>
    /// <para><b>Tablero de los overlays</b> (<see cref="ICuiaBoard"/>, contrato congelado desde la ola 3): <see cref="Bind"/>
    /// une cada vista al tablero y a las capas (<see cref="CuiaView.BindBoard"/>), <see cref="BoardLaidOut"/> sube
    /// <see cref="LayoutVersion"/> y avisa a los overlays, y las posiciones van en el espacio de las capas.</para>
    /// </remarks>
    public sealed class BoardAnimator : ICuiaBoard
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

        // Bits de las máscaras de cuias: 16 como máximo (R1).
        private const int MaskBits = BoardState.MaxCuias;

        private readonly Job[] _jobs = new Job[MaxJobs];
        private readonly bool[] _mark = new bool[MaxJobs];
        private readonly RectTransform _streamLayer;
        private readonly RectTransform _frontLayer;
        private readonly PourStream[] _streams;
        private readonly MechanicStepQueue _steps;
        private readonly StepEntry[] _plan = new StepEntry[MechanicStepPlan.MaxSteps];
        private CuiaView[] _views = Array.Empty<CuiaView>();
        private Cuia[] _shown = Array.Empty<Cuia>();
        private Cuia[] _model = Array.Empty<Cuia>();
        private bool[] _modelSealed = Array.Empty<bool>();
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

            _steps = new MechanicStepQueue(this);
        }

        /// <summary>
        /// Efectos de la caída de cada dosis (salpicadura y onda, guía visual v2 6.6); null = sin efectos. No cambian
        /// tiempos ni el modelo.
        /// </summary>
        public IBoardFx? Fx { get; set; }

        /// <summary>La cola está vacía, sin trabajos ni pasos de mecánica (la vista ya muestra el modelo).</summary>
        public bool IsIdle => _count == 0 && _steps.ActiveCount == 0;

        /// <summary>Cola de pasos de mecánica (diagnóstico y tests: registro, pasos en curso y presupuesto).</summary>
        public MechanicStepQueue Steps => _steps;

        /// <summary>Reloj de la cola (ms avanzados con <see cref="Tick"/>); lo usan los registros de pasos.</summary>
        public float Clock { get; private set; }

        /// <summary>
        /// Eventos sueltos de desarrollo (<c>DevProbe.Event(evento, campos JSON)</c>): hoy <c>step_budget</c>, cuando las últimas
        /// etapas de un plan saltan por el presupuesto desde el toque. Null = sin sonda (se escribe en el log en builds de
        /// desarrollo).
        /// </summary>
        public Action<string, string>? DevEvent { get; set; }

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
        /// Capa del tablero detrás de las cuias (arquitectura del paso 1, §6): lo que une cuias o ronda el tablero (el canal
        /// del Remolino, el cardumen…) y nunca va en el cuerpo de una cuia. Null hasta <see cref="Bind"/>; la usan los
        /// overlays de mecánica desde la ola 2.
        /// </summary>
        public RectTransform? BoardBackLayer { get; private set; }

        /// <summary>
        /// Capa del tablero delante de las cuias y detrás de la intro, los chorros y los efectos (arquitectura del paso 1, §6):
        /// el hilo de luz de la Liana, el pulso de la Luciérnaga… Null hasta <see cref="Bind"/>.
        /// </summary>
        public RectTransform? BoardFrontLayer { get; private set; }

        /// <summary>
        /// Factor de velocidad del nivel (<c>GameConfig.SpeedFactor</c>; 02 §4): multiplica las duraciones que la cola no
        /// recibe ya multiplicadas, las de los pasos de mecánica (desde la ola 2; con <see cref="ReduceMotion"/>, × 0,5). Las
        /// de <see cref="PourTimings"/> ya lo llevan, y el traslado inverso de Marea (<see cref="UndoTransferMs"/>, 04 §2.1)
        /// no cambia. 1 por defecto.
        /// </summary>
        public float SpeedFactor { get; set; } = 1f;

        /// <summary>Reducir movimiento (02 §4; 07 §7.8): los pasos de mecánica van a la mitad de tiempo y sin partículas.</summary>
        public bool ReduceMotion { get; set; }

        /// <summary>Factor de los pasos de mecánica antes del presupuesto: <see cref="SpeedFactor"/>, × 0,5 con <see cref="ReduceMotion"/>.</summary>
        public float StepSpeedBase => Mathf.Max(0.01f, SpeedFactor) * (ReduceMotion ? 0.5f : 1f);

        /// <summary>
        /// <c>anim_pour_dose_ms</c> sin el factor de velocidad (280 ms con los defaults de 08 §9): lo que dura la gota de la
        /// Lluvia y cada mitad del mordisco en sus pasos por defecto.
        /// </summary>
        public float DoseNominalMs => _timings.DoseMs / Mathf.Max(0.01f, SpeedFactor);

        /// <summary>Cuias asociadas.</summary>
        int ICuiaBoard.Count => _views.Length;

        /// <summary>
        /// Recolocaciones del tablero avisadas con <see cref="BoardLaidOut"/> (carga, cambio de pantalla y cada fotograma del
        /// relayout de una cuia añadida): los overlays comparan su copia para recolocarse.
        /// </summary>
        public int LayoutVersion { get; private set; }

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

        /// <summary>
        /// Asocia las vistas del nivel cargado, las duraciones y las capas de tablero (<see cref="BoardBackLayer"/> y
        /// <see cref="BoardFrontLayer"/>). Lo pintado en las vistas que siguen siendo las mismas (cuia añadida) se conserva en
        /// <see cref="ShownOf"/>; el de las vistas nuevas queda sin pintar hasta <see cref="ShowContent"/>.
        /// </summary>
        public void Bind(CuiaView[] views, PourTimings timings, int queueMaxLagPours, RectTransform? backLayer = null, RectTransform? frontLayer = null)
        {
            SnapAll();
            Cuia[] shown = _shown.Length == views.Length ? _shown : new Cuia[views.Length];
            Cuia[] model = _model.Length == views.Length ? _model : new Cuia[views.Length];
            bool[] modelSealed = _modelSealed.Length == views.Length ? _modelSealed : new bool[views.Length];
            for (int i = 0; i < views.Length; i++)
            {
                bool same = i < _views.Length && i < _shown.Length && ReferenceEquals(views[i], _views[i]);
                shown[i] = same ? _shown[i] : default;
                model[i] = same && i < _model.Length ? _model[i] : default;
                modelSealed[i] = same && i < _modelSealed.Length && _modelSealed[i];
            }

            _shown = shown;
            _model = model;
            _modelSealed = modelSealed;
            _views = views;
            _timings = timings;
            _maxLag = Mathf.Max(1, queueMaxLagPours);
            BoardBackLayer = backLayer;
            BoardFrontLayer = frontLayer;

            // Cada vista recibe el tablero y las capas antes de crear ningún overlay (contrato de la ola 3). Sin capas (pruebas
            // con una vista suelta), el contenedor de las cuias hace de capa.
            for (int i = 0; i < views.Length; i++)
            {
                RectTransform fallback = views[i].Root.parent as RectTransform ?? views[i].Root;
                views[i].BindBoard(this, backLayer ?? fallback, frontLayer ?? fallback);
            }
        }

        /// <summary>
        /// El tablero se recolocó (al final de cada maquetación y de cada fotograma del relayout, también tras añadir una
        /// cuia): sube <see cref="LayoutVersion"/> y avisa a los overlays de mecánica (<c>OnBoardLayout</c>).
        /// </summary>
        public void BoardLaidOut()
        {
            LayoutVersion++;
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].NotifyBoardLayout();
            }
        }

        /// <summary>Pie de la cuia <paramref name="i"/> en reposo, en el espacio de las capas de tablero.</summary>
        public Vector2 Foot(int i)
        {
            CuiaView view = _views[i];
            return ToLayer(view, view.Root.position);
        }

        /// <summary>Centro del cuerpo de la cuia <paramref name="i"/> en reposo, en el espacio de las capas de tablero.</summary>
        public Vector2 BodyCenter(int i)
        {
            CuiaView view = _views[i];
            return ToLayer(view, view.Root.TransformPoint(new Vector3(0f, 0.5f * view.HeightU * view.Unit, 0f)));
        }

        /// <summary>
        /// Ranura de la cuia <paramref name="i"/> (ancho de ranura, del pie hasta el área táctil sobre la boca), en el espacio
        /// de las capas de tablero.
        /// </summary>
        public Rect SlotRect(int i)
        {
            CuiaView view = _views[i];
            Rect r = view.Root.rect;
            Vector2 a = ToLayer(view, view.Root.TransformPoint(new Vector3(r.xMin, r.yMin, 0f)));
            Vector2 b = ToLayer(view, view.Root.TransformPoint(new Vector3(r.xMax, r.yMax, 0f)));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <inheritdoc />
        Cuia ICuiaBoard.ShownCuia(int i)
        {
            return ShownOf(i);
        }

        /// <summary>Pinta la dosis <paramref name="dose"/> de la cuia <paramref name="i"/> (<see cref="CuiaView.SetDose"/>).</summary>
        public void SetDose(int i, int dose, Color color, float fill, int pattern)
        {
            if ((uint)i < (uint)_views.Length)
            {
                _views[i].SetDose(dose, color, fill, pattern);
            }
        }

        /// <inheritdoc />
        void ICuiaBoard.ShowContent(int i, Cuia cuia, bool sealedNow)
        {
            ShowContentAt(i, cuia, sealedNow);
        }

        // Punto del mundo en el espacio local de la capa trasera (las dos capas comparten transform); sin capas, el del
        // contenedor de la cuia.
        private Vector2 ToLayer(CuiaView view, Vector3 world)
        {
            Transform space = (Transform?)BoardBackLayer ?? view.Root.parent ?? view.Root;
            Vector3 local = space.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        internal CuiaView? ViewAt(int i)
        {
            return (uint)i < (uint)_views.Length ? _views[i] : null;
        }

        /// <summary>
        /// Último <see cref="Cuia"/> que la cola pintó (o registró con <see cref="AdoptBoard"/>) en la vista
        /// <paramref name="index"/>: al quedar quieta, el del modelo. <c>default(Cuia)</c> si aún no se pintó.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">El índice no es una vista asociada.</exception>
        public Cuia ShownOf(int index)
        {
            if ((uint)index >= (uint)_shown.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice fuera de las vistas asociadas.");
            }

            return _shown[index];
        }

        /// <summary>
        /// Pinta <paramref name="cuia"/> en la vista <paramref name="index"/> sin animar (carga, cuia añadida, las demás cuias
        /// de Marea) y lo registra en <see cref="ShownOf"/>. Quien llama se asegura de que ningún trabajo de la cola la esté
        /// animando (<see cref="SnapAll"/> antes).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">El índice no es una vista asociada.</exception>
        public void ShowContent(int index, Cuia cuia, bool sealedNow)
        {
            if ((uint)index >= (uint)_views.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice fuera de las vistas asociadas.");
            }

            ShowContentAt(index, cuia, sealedNow);
            SetModel(index, cuia, sealedNow);
        }

        /// <summary>
        /// Registra <paramref name="board"/> como lo que muestran las vistas sin pintarlo: «Llenar y teñir» las pinta por su
        /// cuenta (intro y reinicio, 02 §2 INTRO) y al terminar deja ese tablero.
        /// </summary>
        /// <exception cref="ArgumentNullException">Falta el tablero.</exception>
        public void AdoptBoard(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            int n = Mathf.Min(board.Count, _shown.Length);
            for (int i = 0; i < n; i++)
            {
                _shown[i] = board[i];
                SetModel(i, board[i], board.IsSealed(i));
            }
        }

        /// <summary>
        /// Encola la animación de un vertido ya aplicado en el modelo (02 §3.2; arquitectura del paso 1, §4), o la encadena a
        /// un trabajo igual (mismo origen, destino y tinte) que aún no ha empezado o cuyo chorro sigue saliendo (ver la clase):
        /// <list type="bullet">
        /// <item><description>el traslado, la inclinación y el chorro van siempre a la cuia tocada (D-006);</description></item>
        /// <item><description>el relleno va al receptor; con Barro en su cima, empieza en <c>Count − 1</c>, sobre el barro, que
        /// se ve hasta que llega el chorro (02 §9.1 M5);</description></item>
        /// <item><description>por un Remolino la cuia tocada no se llena: al terminar el caudal la pareja salta a su estado
        /// final y, si se sella, con su animación (02 §9.1 M4);</description></item>
        /// <item><description>con pasos de mecánica (<see cref="MechanicStepPlan"/>), al terminar el caudal la cola de pasos
        /// los ejecuta por etapas (R10 con f′) en la cuia dueña de cada mecánica, y el trabajo deja el origen y la tocada en
        /// <see cref="StepStaging.JobEnd"/>: sin pasos sobre ellas, en su estado final;</description></item>
        /// <item><description>las demás cuias que cambiaron sin paso saltan a su estado final al terminar el caudal (con la
        /// animación de sellado si se sellaron en un vertido sin pasos); un cambio solo de contador no retiene la cuia (ver la
        /// clase);</description></item>
        /// <item><description>no se encadena un vertido con pasos, por un Remolino, con Barro en la cima del receptor, si
        /// cambian otras cuias (salvo contadores) o si el vertido revela Aguas turbias.</description></item>
        /// </list>
        /// Sin asignaciones.
        /// </summary>
        public void EnqueuePour(in PourVisual v)
        {
            BoardState before = v.Before;
            BoardState after = v.After;
            int source = v.Source;
            int target = v.Target;
            int receiver = v.Receiver;
            bool through = receiver != target;
            int tint = v.Tint;
            int steps = MechanicStepPlan.Build(in v, _plan);
            int planMask = 0;
            for (int e = 0; e < steps; e++)
            {
                planMask |= Bit(_plan[e].Index) | Bit(_plan[e].OtherIndex);
            }

            Cuia srcBefore = before[source];
            Cuia srcAfter = after[source];
            bool srcSealed = after.IsSealed(source);
            Cuia tgtBefore = before[target];
            Cuia tgtAfter = through ? tgtBefore : after[target];
            bool tgtSealed = through ? before.IsSealed(target) : after.IsSealed(target);
            if (steps > 0)
            {
                // El trabajo deja el origen y la tocada en el estado previo a sus pasos (StepStaging.JobEnd).
                if ((planMask & Bit(source)) != 0)
                {
                    StepStaging.JobEnd(in v, _plan, steps, source, out srcAfter, out srcSealed);
                }

                if ((planMask & Bit(target)) != 0)
                {
                    StepStaging.JobEnd(in v, _plan, steps, target, out tgtAfter, out tgtSealed);
                }
            }

            bool onMud = !through && tgtBefore.MudExposed;
            int others = BoardDiff.ChangedOthers(before, after, source, receiver);
            int counters = CounterOnly(before, after, others) & ~planMask;
            others &= ~counters;
            int extra = others | (through ? Bit(receiver) : 0) | (planMask & ~(Bit(source) | Bit(target)));
            int jump = extra & ~planMask;
            int extraSeal = 0;
            if (steps == 0)
            {
                for (int i = 0; i < MaskBits && i < after.Count; i++)
                {
                    if (((jump >> i) & 1) != 0 && after.IsSealed(i) && (i >= before.Count || !before.IsSealed(i)))
                    {
                        extraSeal |= 1 << i;
                    }
                }
            }

            bool chainable = steps == 0 && !through && !before[receiver].MudExposed && others == 0 && v.RevealedMask == 0;
            if (chainable && TryChain(source, target, v.Amount, tint, srcBefore, srcAfter, srcSealed, tgtBefore, tgtAfter, tgtSealed, after))
            {
                ChainedCount++;
                CommitModel(after, counters);
                return;
            }

            int plan = -1;
            if (steps > 0)
            {
                plan = _steps.Acquire(in v, _plan, steps);
                if (plan < 0)
                {
                    SnapOldestPlan();
                    plan = _steps.Acquire(in v, _plan, steps);
                }
            }

            Enqueue(new Job
            {
                Source = source,
                Target = target,
                Receiver = receiver,
                Amount = v.Amount,
                Tint = tint,
                IsUndo = false,
                Through = through,
                OnMud = onMud,
                Chainable = chainable,
                FillBase = onMud ? tgtBefore.Count - 1 : tgtBefore.Count,
                Extra = extra,
                Jump = jump,
                ExtraSeal = extraSeal,
                ExtraDone = jump == 0,
                Plan = plan,
                PlanMask = plan >= 0 ? planMask : 0,
                PlanDone = plan < 0,
                After = after,
                SrcBefore = srcBefore,
                SrcAfter = srcAfter,
                SrcSealedAfter = srcSealed,
                TgtBefore = tgtBefore,
                TgtAfter = tgtAfter,
                TgtSealedAfter = tgtSealed,
                Stream = -1,
            });
            CommitModel(after, counters);
        }

        /// <summary>
        /// Encola Marea (02 §14: la cola ya debe estar resuelta con <see cref="SnapAll"/>): las dosis vuelven de
        /// <paramref name="from"/> (la cuia tocada del vertido deshecho) a <paramref name="to"/> en 250 ms y la tapa salta
        /// (04 §2.1). Las demás cuias que cambiaron las pinta quien llama con <see cref="ShowContent"/>.
        /// </summary>
        public void EnqueueUndo(int from, int to, int amount, int tint, Cuia fromBefore, Cuia fromAfter, bool fromSealedAfter, Cuia toBefore, Cuia toAfter, bool toSealedAfter)
        {
            SetModel(from, fromAfter, fromSealedAfter);
            SetModel(to, toAfter, toSealedAfter);
            Enqueue(new Job
            {
                Source = from,
                Target = to,
                Receiver = to,
                Amount = amount,
                Tint = tint,
                IsUndo = true,
                FillBase = toBefore.Count,
                ExtraDone = true,
                Plan = -1,
                PlanDone = true,
                SrcBefore = fromBefore,
                SrcAfter = fromAfter,
                SrcSealedAfter = fromSealedAfter,
                TgtBefore = toBefore,
                TgtAfter = toAfter,
                TgtSealedAfter = toSealedAfter,
                Stream = -1,
            });
        }

        /// <summary>
        /// Lleva todos los trabajos y sus pasos de mecánica a su fotograma final (Marea, reinicio, vuelta de segundo plano,
        /// celebración y modal): cada paso en curso recibe <c>SnapStep</c> y cada cuia queda con el estado del modelo.
        /// </summary>
        public void SnapAll()
        {
            for (int i = 0; i < _count; i++)
            {
                Complete(i);
            }

            for (int i = 0; i < _count; i++)
            {
                _jobs[i] = default; // sin referencias a tableros viejos
            }

            _count = 0;
        }

        /// <summary>Avanza la cola <paramref name="dtMs"/> milisegundos.</summary>
        public void Tick(float dtMs)
        {
            Clock += dtMs;
            for (int i = 0; i < _count; i++)
            {
                _jobs[i].Age += dtMs;
            }

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
                    Advance(i, dtMs);
                }
            }

            Compact();
        }

        // Encadenado: el último trabajo que toca el origen o el destino (también como receptor u otra cuia que cambió) es un
        // vertido igual y encadenable (no Marea, sin Remolino, Barro, otras cuias ni revelado) que aún no ha empezado o cuyo
        // chorro sigue saliendo; las dosis nuevas se suman a él y su caudal se alarga. Sin asignaciones.
        private bool TryChain(int source, int target, int amount, int tint, Cuia sourceBefore, Cuia sourceAfter, bool sourceSealedAfter, Cuia targetBefore, Cuia targetAfter, bool targetSealedAfter, BoardState after)
        {
            if (amount <= 0)
            {
                return false;
            }

            int pair = (1 << source) | (1 << target);
            int last = -1;
            for (int i = _count - 1; i >= 0; i--)
            {
                if ((CuiaMask(ref _jobs[i]) & pair) != 0)
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
            if (job.IsUndo || !job.Chainable || job.Source != source || job.Target != target || job.Tint != tint || job.SrcDone || job.TgtDone)
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
            job.After = after;
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

            job.Age = 0f;

            _jobs[_count++] = job;

            // 02 §3.2: cada cuia del trabajo (origen, tocada, receptor y las demás que cambian) admite como mucho
            // queue_max_lag_pours animaciones pendientes.
            int mask = CuiaMask(ref job);
            for (int c = 0; c < MaskBits; c++)
            {
                if (((mask >> c) & 1) != 0)
                {
                    EnforceLag(c);
                }
            }
        }

        private void EnforceLag(int cuia)
        {
            int bit = 1 << cuia;
            while (true)
            {
                int involving = 0;
                int oldest = -1;
                for (int i = 0; i < _count; i++)
                {
                    if ((CuiaMask(ref _jobs[i]) & bit) != 0)
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
                    Complete(i);
                }
            }

            Compact();
        }

        // Si no queda sitio para un plan, salta el trabajo más antiguo que tiene uno (y los anteriores que comparten cuias).
        private void SnapOldestPlan()
        {
            for (int i = 0; i < _count; i++)
            {
                if (_jobs[i].Plan >= 0 && !_jobs[i].PlanDone)
                {
                    LagSnapCount++;
                    SnapThrough(i);
                    return;
                }
            }
        }

        // Cuias que toca el trabajo: origen, cuia tocada, receptor y las demás que saltan al estado final (Extra).
        private static int CuiaMask(ref Job job)
        {
            return (1 << job.Source) | (1 << job.Target) | (1 << job.Receiver) | job.Extra;
        }

        // Un trabajo empieza cuando ningún trabajo anterior retiene alguna de sus cuias (02 §3.2).
        private bool CanStart(int index)
        {
            int mask = CuiaMask(ref _jobs[index]);
            for (int i = 0; i < index; i++)
            {
                int shared = CuiaMask(ref _jobs[i]) & mask;
                if (shared == 0)
                {
                    continue;
                }

                for (int c = 0; c < MaskBits; c++)
                {
                    if (((shared >> c) & 1) != 0 && Holds(ref _jobs[i], c))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // El trabajo retiene la cuia hasta que su parte termina: el origen al volver, la tocada al terminar su relleno (o su
        // sellado), las demás cuias sin paso al saltar a su estado final y las cuias del plan al terminar su último paso.
        private bool Holds(ref Job job, int cuia)
        {
            int bit = 1 << cuia;
            if ((CuiaMask(ref job) & bit) == 0)
            {
                return false;
            }

            if (!job.Started)
            {
                return true;
            }

            return (job.Source == cuia && !job.SrcDone)
                || (job.Target == cuia && !job.TgtDone)
                || ((job.Jump & bit) != 0 && !job.ExtraDone)
                || (PlanPending(ref job) & bit) != 0;
        }

        // Cuias con pasos sin terminar en el plan del trabajo (todas las del plan antes de que empiece).
        private int PlanPending(ref Job job)
        {
            if (job.Plan < 0 || job.PlanDone)
            {
                return 0;
            }

            return job.PlanBegun ? _steps.PendingMask(job.Plan) : job.PlanMask;
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

        // Fin de caudal: cola del chorro, retorno del origen (07 §5.2: arranca cuando termina el chorro, cola incluida),
        // fin del destino (con el sellado de 07 §5.3 si lo hay) y salto de la pareja del Remolino y de las demás cuias al
        // llegar el caudal (con su sellado si lo hay).
        private void SetEnds(ref Job job)
        {
            job.StreamEnd = job.FlowEnd + _timings.TailMs;
            job.ReturnStart = Mathf.Max(_timings.TravelMs, job.StreamEnd);
            job.SrcEnd = job.ReturnStart + _timings.ReturnMs;
            job.TgtEnd = job.FlowEnd + StreamGrowMs + (job.TgtSealedAfter ? CuiaView.SealMs : 0f);
            job.ExtraStart = job.FlowEnd + StreamGrowMs;
            job.ExtraEnd = job.ExtraStart + (job.ExtraSeal != 0 ? CuiaView.SealMs : 0f);
        }

        private void Advance(int index, float dtMs)
        {
            ref Job job = ref _jobs[index];
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
                    FinishSource(index);
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
                    FinishTarget(index);
                }
                else
                {
                    // Por un Remolino la cuia tocada no se llena: las dosis entran por el fondo de la pareja (02 §9.1 M4).
                    if (!job.Through)
                    {
                        // Con Barro en la cima, el relleno empieza en su dosis, que se ve hasta que llega el chorro (M5).
                        int bottom = job.FillBase;
                        for (int i = 0; i < job.Amount; i++)
                        {
                            float fill = Mathf.Clamp01(pt - i);
                            if (i == 0 && job.OnMud && fill <= 0f)
                            {
                                continue;
                            }

                            t.SetDose(bottom + i, color, fill, job.Tint);
                        }
                    }

                    float flowArrived = job.FlowEnd + fillDelay;
                    if (job.TgtSealedAfter && time >= flowArrived)
                    {
                        t.SealAnimationAt(time - flowArrived);
                    }
                }
            }

            // Pareja del Remolino y demás cuias que cambiaron: saltan a su estado final al llegar el caudal y, si se sellaron,
            // la tapa cae con la animación de 07 §5.3 (desde la ola 2, pasos de mecánica en su orden).
            if (!job.ExtraDone)
            {
                if (time >= job.ExtraEnd)
                {
                    FinishExtras(index);
                }
                else if (time >= job.ExtraStart)
                {
                    if (!job.ExtraShown)
                    {
                        job.ExtraShown = true;
                        ShowExtras(index, false);
                    }

                    SealExtrasAt(ref job, time - job.ExtraStart);
                }
            }

            // Pasos de mecánica: empiezan al llegar el caudal (etapa (a)), tras el fin del destino y los saltos de este
            // fotograma; el revelado espera a que el origen vuelva (SrcDone, también en este fotograma).
            if (job.Plan >= 0 && !job.PlanDone && time >= job.ExtraStart)
            {
                float step = dtMs;
                if (!job.PlanBegun)
                {
                    job.PlanBegun = true;
                    float over = Mathf.Min(dtMs, time - job.ExtraStart);
                    _steps.Begin(job.Plan, job.Age - over, job.SrcDone ? 0f : job.SrcEnd - job.ExtraStart);
                    step = over;
                }

                if (_steps.Tick(job.Plan, step, job.SrcDone))
                {
                    _steps.Release(job.Plan);
                    job.PlanDone = true;
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

        private void FinishSource(int index)
        {
            ref Job job = ref _jobs[index];
            CuiaView s = _views[job.Source];
            if (job.Started && !job.IsUndo)
            {
                s.SetTongue(0f, Color.clear, 0);
                s.ReturnBody();
                s.Unlock();
            }

            job.SrcDone = true;
            PaintJobEnd(index, job.Source, job.SrcAfter, job.SrcSealedAfter);
            ReleaseStream(ref job);
        }

        private void FinishTarget(int index)
        {
            ref Job job = ref _jobs[index];
            job.TgtDone = true;
            PaintJobEnd(index, job.Target, job.TgtAfter, job.TgtSealedAfter);
        }

        // Fin de la pareja del Remolino y de las demás cuias sin paso: su estado final, sellado incluido. Va después del
        // destino, así que la cuia tocada por un Remolino que además cambió termina también en su estado final.
        private void FinishExtras(int index)
        {
            ShowExtras(index, true);
            _jobs[index].ExtraDone = true;
        }

        // Pinta el estado final de las cuias que saltan (Jump); con final = false, las que se sellan quedan aún sin tapa (la
        // pone SealExtrasAt).
        private void ShowExtras(int index, bool final)
        {
            ref Job job = ref _jobs[index];
            BoardState? after = job.After;
            if (after == null)
            {
                return;
            }

            int n = Mathf.Min(Mathf.Min(_views.Length, after.Count), MaskBits);
            for (int i = 0; i < n; i++)
            {
                int bit = 1 << i;
                if ((job.Jump & bit) == 0)
                {
                    continue;
                }

                bool sealedNow = after.IsSealed(i) && (final || (job.ExtraSeal & bit) == 0);
                if (final)
                {
                    PaintFinal(index, i, after[i], sealedNow);
                }
                else
                {
                    ShowContentAt(i, after[i], sealedNow);
                }
            }
        }

        // Fin de la parte del trabajo en el origen o la tocada: si el plan aún tiene pasos sobre ella, el estado previo a
        // ellos (JobEnd, ya en SrcAfter o TgtAfter); si no, su estado final.
        private void PaintJobEnd(int index, int cuia, Cuia state, bool sealedNow)
        {
            if ((PlanPending(ref _jobs[index]) & Bit(cuia)) != 0)
            {
                ShowContentAt(cuia, state, sealedNow);
            }
            else
            {
                PaintFinal(index, cuia, state, sealedNow);
            }
        }

        // Final de la cuia en el trabajo index: si un trabajo posterior la usa, su estado en este trabajo; si no, el último
        // estado del modelo (que incluye los contadores que cambiaron sin retenerla).
        private void PaintFinal(int index, int cuia, Cuia own, bool ownSealed)
        {
            int bit = Bit(cuia);
            for (int j = index + 1; j < _count; j++)
            {
                if ((CuiaMask(ref _jobs[j]) & bit) != 0)
                {
                    ShowContentAt(cuia, own, ownSealed);
                    return;
                }
            }

            if ((uint)cuia < (uint)_model.Length && _model[cuia].Capacity > 0)
            {
                ShowContentAt(cuia, _model[cuia], _modelSealed[cuia]);
            }
            else
            {
                ShowContentAt(cuia, own, ownSealed);
            }
        }

        // Fin de un paso de mecánica del plan slot en la cuia: con last, su final (PaintFinal); si no, el estado intermedio.
        internal void PaintStepEnd(int slot, int cuia, Cuia state, bool sealedNow, bool last)
        {
            if (!last)
            {
                ShowContentAt(cuia, state, sealedNow);
                return;
            }

            for (int i = 0; i < _count; i++)
            {
                if (_jobs[i].Plan == slot && !_jobs[i].PlanDone)
                {
                    PaintFinal(i, cuia, state, sealedNow);
                    return;
                }
            }

            ShowContentAt(cuia, state, sealedNow);
        }

        // step_budget (DevProbe.Event): las últimas etapas de un plan saltaron. Solo asigna cuando ocurre.
        internal void RaiseStepBudget(float ageMs, float plannedMs, float leftMs, float speed, int skipFrom)
        {
            string fields = "\"age_ms\":" + Mathf.RoundToInt(ageMs).ToString(CultureInfo.InvariantCulture)
                + ",\"planned_ms\":" + Mathf.RoundToInt(plannedMs).ToString(CultureInfo.InvariantCulture)
                + ",\"left_ms\":" + Mathf.RoundToInt(leftMs).ToString(CultureInfo.InvariantCulture)
                + ",\"speed\":" + speed.ToString("0.00", CultureInfo.InvariantCulture)
                + ",\"skip_from\":\"" + ((StepStage)skipFrom).ToString() + "\"";
            if (DevEvent != null)
            {
                DevEvent("step_budget", fields);
            }
            else if (Debug.isDebugBuild)
            {
                Debug.Log("AF_STATE {\"event\":\"step_budget\"," + fields + "}");
            }
        }

        // Último estado del modelo de la cuia (lo que debe verse con la cola quieta).
        private void SetModel(int index, Cuia cuia, bool sealedNow)
        {
            if ((uint)index < (uint)_model.Length)
            {
                _model[index] = cuia;
                _modelSealed[index] = sealedNow;
            }
        }

        // Registra el tablero del modelo tras un vertido y repinta en el acto los contadores que ningún trabajo usa (los
        // demás los pinta el último trabajo que usa la cuia, con PaintFinal).
        private void CommitModel(BoardState after, int counters)
        {
            int n = Mathf.Min(after.Count, _model.Length);
            for (int i = 0; i < n; i++)
            {
                SetModel(i, after[i], after.IsSealed(i));
            }

            for (int i = 0; i < n && counters != 0; i++)
            {
                int bit = Bit(i);
                if ((counters & bit) == 0)
                {
                    continue;
                }

                bool used = false;
                for (int j = 0; j < _count && !used; j++)
                {
                    used = (CuiaMask(ref _jobs[j]) & bit) != 0;
                }

                if (!used)
                {
                    ShowContentAt(i, _model[i], _modelSealed[i]);
                }
            }
        }

        // Cuias de mask que solo cambiaron el contador de su mecánica (Lluvia o cueva de la Piraña que cuentan un vertido
        // útil): mismo contenido, mismas ocultas, misma mecánica con sus campos y mismo sellado.
        private static int CounterOnly(BoardState before, BoardState after, int mask)
        {
            int result = 0;
            int n = Mathf.Min(Mathf.Min(before.Count, after.Count), MaskBits);
            for (int i = 0; i < n; i++)
            {
                if (((mask >> i) & 1) == 0)
                {
                    continue;
                }

                Cuia b = before[i];
                Cuia a = after[i];
                MechanicType type = b.Mechanic;
                if ((type != MechanicType.Lluvia && type != MechanicType.Pirana) || a.Mechanic != type)
                {
                    continue;
                }

                if (b.Count != a.Count || b.HiddenMask != a.HiddenMask || before.IsSealed(i) != after.IsSealed(i)
                    || b.MechanicKey != a.MechanicKey || b.MechanicN != a.MechanicN || b.MechanicK != a.MechanicK
                    || b.MechanicFlags != a.MechanicFlags || b.MechanicPairId != a.MechanicPairId)
                {
                    continue;
                }

                bool same = true;
                for (int k = 0; k < b.Count && same; k++)
                {
                    same = b.ColorAt(k) == a.ColorAt(k);
                }

                if (same)
                {
                    result |= 1 << i;
                }
            }

            return result;
        }

        private static int Bit(int index)
        {
            return (uint)index < (uint)MaskBits ? 1 << index : 0;
        }

        private void SealExtrasAt(ref Job job, float ms)
        {
            if (job.ExtraSeal == 0)
            {
                return;
            }

            int n = Mathf.Min(_views.Length, MaskBits);
            for (int i = 0; i < n; i++)
            {
                if (((job.ExtraSeal >> i) & 1) != 0)
                {
                    _views[i].SealAnimationAt(ms);
                }
            }
        }

        // Toda pintura final de la cola pasa por aquí: así ShownOf sabe lo último que se pintó en cada vista.
        private void ShowContentAt(int index, Cuia cuia, bool sealedNow)
        {
            if ((uint)index >= (uint)_views.Length)
            {
                return;
            }

            _views[index].ShowContent(cuia, sealedNow);
            if ((uint)index < (uint)_shown.Length)
            {
                _shown[index] = cuia;
            }
        }

        // Lleva el trabajo index a su final: origen, tocada y saltos, y su plan de pasos (SnapStep y estado final de cada cuia).
        private void Complete(int index)
        {
            ref Job job = ref _jobs[index];
            if (!job.SrcDone)
            {
                FinishSource(index);
            }

            if (!job.TgtDone)
            {
                FinishTarget(index);
            }

            if (!job.ExtraDone)
            {
                FinishExtras(index);
            }

            if (job.Plan >= 0 && !job.PlanDone)
            {
                _steps.Snap(job.Plan);
                job.PlanDone = true;
            }
        }

        private void Compact()
        {
            int w = 0;
            for (int i = 0; i < _count; i++)
            {
                if (_jobs[i].SrcDone && _jobs[i].TgtDone && _jobs[i].ExtraDone && _jobs[i].PlanDone)
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
            public int Receiver;
            public int Amount;
            public int Tint;
            public bool IsUndo;
            public bool Through;
            public bool OnMud;
            public bool Chainable;
            public int FillBase;
            public int Extra;
            public int Jump;
            public int ExtraSeal;
            public bool ExtraDone;
            public bool ExtraShown;
            public float ExtraStart;
            public float ExtraEnd;
            public BoardState? After;
            public Cuia SrcBefore;
            public Cuia SrcAfter;
            public bool SrcSealedAfter;
            public Cuia TgtBefore;
            public Cuia TgtAfter;
            public bool TgtSealedAfter;
            public int Plan;
            public int PlanMask;
            public bool PlanBegun;
            public bool PlanDone;
            public float Age;
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
