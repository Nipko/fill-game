#nullable enable
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Solver.Search;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El dedo guía del tutorial (tarea DC6; D-028, pregunta 61 (a) «con P5»: P5 de decisiones.md §5, «Sí»; 02 §13, filas 1 y 2;
    /// 06 §2.2; 02 §4, D-002): una mano que va del origen al destino de un vertido, en bucle, sin capturar toques, hasta que el
    /// jugador hace ese vertido (la retira <c>GameScreen</c>). Componente reutilizable: no sabe de niveles ni de reglas; recibe el
    /// vertido (<see cref="Show"/>) y lo señala. Es una ayuda explícita del tutorial que solo señala el vertido de la lección:
    /// nunca resalta destinos al seleccionar (D-002) y no se enciende ni cambia con la selección.
    /// <list type="number">
    /// <item><description><b>Bucle</b> (<see cref="CycleMs"/>): aparece sobre el origen (<see cref="AppearMs"/>), lo toca (se hunde
    /// un poco y deja una onda en anillo, <see cref="PressMs"/>), viaja en arco por encima de las bocas al destino (<see cref="MoveMs"/>), lo toca, se queda
    /// (<see cref="HoldMs"/>), se funde (<see cref="FadeMs"/>) y espera (<see cref="GapMs"/>) antes de volver a empezar. Las
    /// duraciones son [P].</description></item>
    /// <item><description><b>Reducir movimiento</b> (07 §7.8): sin animación. La mano, fija sobre el origen, y una flecha de la boca
    /// del origen a la boca del destino; ni onda ni fundidos.</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Las imágenes se crean una vez en un Canvas anidado propio (moverlas no reconstruye el lienzo principal), en la capa de
    /// mecánicas delantera del tablero, y en coordenadas del contenedor del tablero (dp, las de <c>CuiaView.FootPosition</c>:
    /// origen arriba a la izquierda y hacia arriba), como <see cref="MorphoFx"/>; sigue a las cuias si se elevan. Ningún gráfico
    /// es <c>raycastTarget</c>. Cada fotograma solo coloca y tiñe: sin asignaciones (08 §5.4). La elección del vertido
    /// (<see cref="TryChoose"/>) usa el solver y solo se llama cuando cambia el tablero, nunca por fotograma.
    /// </remarks>
    internal sealed class TutorialFinger
    {
        /// <summary>Entrada de la mano sobre el origen (ms) [P].</summary>
        public const float AppearMs = 200f;

        /// <summary>Toque: la mano se hunde y deja una onda (ms) [P].</summary>
        public const float PressMs = 260f;

        /// <summary>Viaje del origen al destino (ms) [P].</summary>
        public const float MoveMs = 700f;

        /// <summary>La mano se queda sobre el destino tras tocarlo (ms) [P].</summary>
        public const float HoldMs = 300f;

        /// <summary>Fundido de salida al final de cada vuelta (ms) [P].</summary>
        public const float FadeMs = 250f;

        /// <summary>Pausa sin mano antes de la vuelta siguiente (ms) [P].</summary>
        public const float GapMs = 400f;

        /// <summary>Una vuelta entera del bucle (ms).</summary>
        public const float CycleMs = AppearMs + PressMs + MoveMs + PressMs + HoldMs + FadeMs + GapMs;

        /// <summary>Lo que se hunde la mano al tocar (fracción de su tamaño) [P].</summary>
        public const float PressDepth = 0.12f;

        /// <summary>
        /// Aire del punto más alto del arco del viaje sobre la boca más alta del origen y el destino (u) [P]: la yema pasa por
        /// encima de las bocas y no se apoya en las cuias del medio, que no son de la lección (revisión de DC6; D-002).
        /// </summary>
        public const float ArcClearU = 0.4f;

        /// <summary>Aire mínimo entre la yema, en lo alto del arco, y el borde interior del panel (dp) [P].</summary>
        public const float ArcCeilingAirDp = 8f;

        /// <summary>
        /// Altura del punto que toca la yema, desde el pie de la cuia (fracción de su alto) [P]: el cuerpo de la cuia, que es donde
        /// se toca para seleccionarla.
        /// </summary>
        public const float TapHeightFraction = 0.45f;

        /// <summary>Inclinación de la mano (grados, + a la izquierda): llega desde abajo a la derecha [P].</summary>
        public const float HandTiltDeg = 20f;

        /// <summary>Diámetro de la onda al empezar (dp) [P].</summary>
        public const float RippleStartDp = 16f;

        /// <summary>
        /// Diámetro de la onda al acabar (dp) [P]: el anillo de <see cref="ProceduralArt.FingerRippleDp"/>, con su trazo de
        /// <see cref="ProceduralArt.FingerRippleStrokeDp"/> dp a este tamaño (al empezar, más fino: el sprite se escala) [P].
        /// </summary>
        public const float RippleEndDp = ProceduralArt.FingerRippleDp;

        /// <summary>Alfa del anillo de papel de la onda al nacer (baja a 0) [P].</summary>
        public const float RippleAlpha = 0.9f;

        /// <summary>Alfa del contorno de tinta de la onda al nacer (baja a 0) [P].</summary>
        public const float RippleOutlineAlpha = 0.35f;

        /// <summary>Altura de la flecha de Reducir movimiento sobre el borde de la boca (u) [P].</summary>
        public const float ArrowAboveRimU = 0.5f;

        /// <summary>Grosor de la línea de la flecha (dp) [P].</summary>
        public const float ArrowShaftDp = 4f;

        /// <summary>Grosor del contorno de tinta de la línea de la flecha, por lado (dp) [P].</summary>
        public const float ArrowOutlineDp = 1.5f;

        /// <summary>
        /// Aire mínimo entre la punta de la flecha (su lado de arriba) y el borde interior del panel (dp) [P]: con cuias altas
        /// (el 1, T = 3) la flecha baja hasta dejarlo, para no pisar el marco (revisión de DC6; D-003 y D-004).
        /// </summary>
        public const float ArrowCeilingAirDp = 6f;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly Image _rippleOutline;
        private readonly Image _ripple;
        private readonly Image _shaftOutline;
        private readonly Image _shaft;
        private readonly Image _head;
        private readonly Image _hand;
        private bool _active;
        private bool _suspended;
        private bool _reduceMotion;
        private float _t;
        private Vector2 _srcTap;
        private Vector2 _dstTap;
        private Vector2 _srcRim;
        private Vector2 _dstRim;
        private float _u = 24f;
        private float _arcLift;
        private float _arcApexY;
        private float _rimTopY;

        /// <summary>Crea las imágenes (ocultas) en <paramref name="layer"/>, que cubre el contenedor del tablero.</summary>
        public TutorialFinger(RectTransform layer, ProceduralArt art)
        {
            _art = art;
            var go = new GameObject("TutorialFinger", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(layer, false);
            _root = (RectTransform)go.transform;
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);
            _root.anchoredPosition = Vector2.zero;
            _root.sizeDelta = Vector2.zero;
            _rippleOutline = NewImage("RippleOutline", null, new Vector2(0.5f, 0.5f));
            _ripple = NewImage("Ripple", null, new Vector2(0.5f, 0.5f));
            _shaftOutline = NewImage("ArrowShaftOutline", null, new Vector2(0f, 0.5f));
            _shaft = NewImage("ArrowShaft", null, new Vector2(0f, 0.5f));
            _head = NewImage("ArrowHead", null, new Vector2(1f, 0.5f));
            _hand = NewImage("Hand", null, ProceduralArt.FingerTipPivot);
            _hand.rectTransform.sizeDelta = new Vector2(ProceduralArt.FingerHandWidthDp, ProceduralArt.FingerHandHeightDp);
            _hand.rectTransform.localRotation = Quaternion.Euler(0f, 0f, HandTiltDeg);
            _head.rectTransform.sizeDelta = new Vector2(ProceduralArt.FingerArrowHeadWidthDp, ProceduralArt.FingerArrowHeadHeightDp);
            go.SetActive(false);
        }

        /// <summary>El dedo señala un vertido (aunque esté suspendido: <see cref="Shown"/> dice si se ve).</summary>
        public bool Active => _active;

        /// <summary>El dedo se ve: activo y no suspendido (una pausa, un modal o la intro de otro intento).</summary>
        public bool Shown => _active && !_suspended;

        /// <summary>Cuia de origen del vertido que señala (−1 si no hay).</summary>
        public int Source { get; private set; } = -1;

        /// <summary>Cuia de destino del vertido que señala (−1 si no hay).</summary>
        public int Target { get; private set; } = -1;

        /// <summary>Va sin animación (Reducir movimiento): mano fija sobre el origen y flecha al destino.</summary>
        public bool ReduceMotion => _reduceMotion;

        /// <summary>Raíz del dedo (tests de capas y capturas).</summary>
        public RectTransform Root => _root;

        /// <summary>La mano (AF_LAYOUT y tests).</summary>
        public Image Hand => _hand;

        /// <summary>La línea de la flecha de Reducir movimiento (tests).</summary>
        public Image ArrowShaft => _shaft;

        /// <summary>La punta de la flecha de Reducir movimiento (tests).</summary>
        public Image ArrowHead => _head;

        /// <summary>La onda del toque: el anillo de papel (tests).</summary>
        public Image Ripple => _ripple;

        /// <summary>El contorno de tinta del anillo de la onda (tests).</summary>
        public Image RippleOutline => _rippleOutline;

        /// <summary>
        /// Borde interior de arriba del panel del tablero (y en coordenadas del contenedor, hacia arriba; infinito = sin límite):
        /// ni la flecha ni lo alto del arco lo pasan. Lo pone <c>GameScreen</c> antes de cada fotograma.
        /// </summary>
        public float CeilingY { get; set; } = float.PositiveInfinity;

        /// <summary>La y de la yema en lo alto del arco del viaje (tests).</summary>
        public float ArcApexY => _arcApexY;

        /// <summary>La y de la boca más alta del origen y el destino (el borde de arriba de la cuia; tests).</summary>
        public float RimTopY => _rimTopY;

        /// <summary>La flecha se ve (solo con Reducir movimiento).</summary>
        public bool ArrowVisible => Shown && _shaft.gameObject.activeSelf;

        /// <summary>Posición de la yema (coordenadas del contenedor del tablero; tests).</summary>
        public Vector2 TipPosition => _hand.rectTransform.anchoredPosition;

        /// <summary>Alfa de la mano ahora (tests).</summary>
        public float HandAlpha => _hand.color.a;

        /// <summary>Punto que toca la yema en el origen, con la cuia como está (tests).</summary>
        public Vector2 SourceTap => _srcTap;

        /// <summary>Punto que toca la yema en el destino, con la cuia como está (tests).</summary>
        public Vector2 TargetTap => _dstTap;

        /// <summary>Milisegundos dentro de la vuelta del bucle (tests).</summary>
        public float CycleTimeMs => _t;

        /// <summary>
        /// Señala el vertido de <paramref name="source"/> a <paramref name="target"/> de <paramref name="views"/> desde el principio
        /// del bucle (o fijo, con <paramref name="reduceMotion"/>). Índices fuera del tablero: no hace nada.
        /// </summary>
        public void Show(int source, int target, CuiaView[] views, bool reduceMotion)
        {
            if ((uint)source >= (uint)views.Length || (uint)target >= (uint)views.Length || source == target)
            {
                Hide();
                return;
            }

            if (_hand.sprite == null)
            {
                _hand.sprite = _art.FingerHand;
                _head.sprite = _art.FingerArrowHead;
                _ripple.sprite = _art.FingerRipple;
                _rippleOutline.sprite = _art.FingerRippleOutline;
            }

            Source = source;
            Target = target;
            _reduceMotion = reduceMotion;
            _t = 0f;
            _active = true;
            _suspended = false;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _shaftOutline.gameObject.SetActive(reduceMotion);
            _shaft.gameObject.SetActive(reduceMotion);
            _head.gameObject.SetActive(reduceMotion);
            _ripple.gameObject.SetActive(!reduceMotion);
            _rippleOutline.gameObject.SetActive(!reduceMotion);
            Follow(views);
            Apply();
        }

        /// <summary>
        /// Oculta el dedo sin olvidar el vertido (<paramref name="suspended"/>) o lo vuelve a enseñar desde el principio del bucle.
        /// Sin vertido no hace nada.
        /// </summary>
        public void Suspend(bool suspended, CuiaView[] views)
        {
            if (!_active || suspended == _suspended)
            {
                return;
            }

            _suspended = suspended;
            _root.gameObject.SetActive(!suspended);
            if (!suspended)
            {
                _t = 0f;
                if ((uint)Source < (uint)views.Length && (uint)Target < (uint)views.Length)
                {
                    Follow(views);
                    Apply();
                }
            }
        }

        /// <summary>Retira el dedo en el acto (hecho el vertido, otro vertido, otro intento u otro nivel).</summary>
        public void Hide()
        {
            _active = false;
            _suspended = false;
            _t = 0f;
            Source = -1;
            Target = -1;
            _root.gameObject.SetActive(false);
        }

        /// <summary>Avanza <paramref name="dtMs"/> ms el bucle y sigue a las cuias de <paramref name="views"/>.</summary>
        public void Tick(float dtMs, CuiaView[] views)
        {
            if (!_active || _suspended)
            {
                return;
            }

            if ((uint)Source >= (uint)views.Length || (uint)Target >= (uint)views.Length)
            {
                Hide();
                return;
            }

            if (!_reduceMotion)
            {
                _t += Mathf.Max(0f, dtMs);
                if (_t >= CycleMs)
                {
                    _t -= CycleMs * Mathf.Floor(_t / CycleMs);
                }
            }

            Follow(views);
            Apply();
        }

        /// <summary>
        /// El vertido que señala el dedo para la lección <paramref name="lesson"/> sobre <paramref name="state"/> (D-028 con P5;
        /// 02 §13). Con el solver A* óptimo con el modo de vertido y la regla de fútiles de la partida (el de la comprobación del
        /// tooltip de Marea; el pack no guarda la solución):
        /// <list type="bullet">
        /// <item><description><see cref="FingerLesson.FirstPour"/> (el 1): el primer vertido de la solución desde
        /// <paramref name="state"/>.</description></item>
        /// <item><description><see cref="FingerLesson.PartialRun"/> (el 2): el vertido parcial de la lección
        /// (<see cref="TutorialInstruction.IsPartialRunLesson"/>) en cuanto es legal: el primero de la solución si lo es; si no, el
        /// primero (origen y luego destino, en orden de índice) que deja el tablero con solución. False mientras no haya ninguno
        /// legal (el tablero inicial del 2 está lleno: la lección llega tras el primer vertido). «El primero de la solución» casi
        /// nunca vale aquí: la solución óptima del 2 de <c>tutorial_v0</c> (par 6) no pasa por el parcial, que cuesta un vertido
        /// más; el dedo enseña la lección de 02 §13, no el camino más corto [P].</description></item>
        /// </list>
        /// False también si el solver no encuentra solución en su presupuesto (el del pipeline, 03 §5).
        /// </summary>
        public static bool TryChoose(FingerLesson lesson, BoardState state, PourMode mode, bool futileRule, AStarSolver solver, out Pour pour)
        {
            pour = default;
            if (lesson == FingerLesson.None || state.IsWon())
            {
                return false;
            }

            SolverOptions options = SolverOptions.ForPipeline(mode, state).WithFutileRule(futileRule);
            SolveResult solved = solver.Solve(state, options);
            bool hasFirst = solved.Status == SolveStatus.Solved && solved.MoveCount > 0;
            if (lesson == FingerLesson.FirstPour)
            {
                if (!hasFirst)
                {
                    return false;
                }

                pour = solved.GetMove(0);
                return true;
            }

            if (hasFirst)
            {
                Pour first = solved.GetMove(0);
                if (TutorialInstruction.IsPartialRunLesson(state, first.Source, first.Target, mode))
                {
                    pour = first;
                    return true;
                }
            }

            for (int s = 0; s < state.Count; s++)
            {
                for (int t = 0; t < state.Count; t++)
                {
                    if (!TutorialInstruction.IsPartialRunLesson(state, s, t, mode))
                    {
                        continue;
                    }

                    BoardState next = PourRules.Apply(state, s, t, mode);
                    if (next.IsWon() || solver.Solve(next, SolverOptions.ForPipeline(mode, next).WithFutileRule(futileRule)).Status == SolveStatus.Solved)
                    {
                        pour = new Pour(s, t, PourRules.Amount(state, s, t, mode));
                        return true;
                    }
                }
            }

            return false;
        }

        private Image NewImage(string name, Sprite? sprite, Vector2 pivot)
        {
            Image img = UiBuilder.Image(name, _root, sprite, Color.white);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = pivot;
            rt.sizeDelta = Vector2.zero;
            img.raycastTarget = false; // nunca captura toques: el tablero sigue recibiendo los del jugador
            return img;
        }

        // Vuelve a medir los puntos de los toques y de la flecha (las cuias se elevan al seleccionarlas).
        private void Follow(CuiaView[] views)
        {
            CuiaView src = views[Source];
            CuiaView dst = views[Target];
            _u = Mathf.Max(1f, dst.Unit);
            _srcTap = TapOf(src);
            _dstTap = TapOf(dst);
            _rimTopY = Mathf.Max(TopOf(src), TopOf(dst));

            // La flecha, a ArrowAboveRimU sobre las bocas, pero con la punta a ArrowCeilingAirDp del borde del panel.
            float arrowMax = CeilingY - (0.5f * ProceduralArt.FingerArrowHeadHeightDp) - ArrowCeilingAirDp;
            _srcRim = RimOf(src, arrowMax);
            _dstRim = RimOf(dst, arrowMax);

            // Lo alto del arco: ArcClearU sobre la boca más alta, sin pasar del borde del panel (ni bajar de los dos toques).
            float midY = 0.5f * (_srcTap.y + _dstTap.y);
            float apex = Mathf.Min(_rimTopY + (ArcClearU * _u), CeilingY - ArcCeilingAirDp);
            _arcApexY = Mathf.Max(apex, Mathf.Max(_srcTap.y, _dstTap.y));
            _arcLift = Mathf.Max(0f, _arcApexY - midY);
        }

        private static float TopOf(CuiaView view)
        {
            float u = Mathf.Max(1f, view.Unit);
            return view.FootPosition.y + view.BodyOffset.y + (view.HeightU * u);
        }

        private static Vector2 TapOf(CuiaView view)
        {
            float u = Mathf.Max(1f, view.Unit);
            return view.FootPosition + view.BodyOffset + new Vector2(0f, view.HeightU * TapHeightFraction * u);
        }

        private static Vector2 RimOf(CuiaView view, float maxY)
        {
            float u = Mathf.Max(1f, view.Unit);
            Vector2 p = view.FootPosition + view.BodyOffset + new Vector2(0f, (view.HeightU + ArrowAboveRimU) * u);
            p.y = Mathf.Min(p.y, maxY);
            return p;
        }

        // Pose del fotograma: la mano (posición, hundido y alfa), la onda y, con Reducir movimiento, la flecha.
        private void Apply()
        {
            RectTransform hand = _hand.rectTransform;
            if (_reduceMotion)
            {
                hand.anchoredPosition = _srcTap;
                hand.localScale = Vector3.one;
                SetAlpha(_hand, 1f);
                PlaceArrow();
                return;
            }

            float t = _t;
            float alpha = 1f;
            float press = 0f;
            float ripple = -1f;
            Vector2 pos = _srcTap;
            float pressEnd1 = AppearMs + PressMs;
            float moveEnd = pressEnd1 + MoveMs;
            float pressEnd2 = moveEnd + PressMs;
            float holdEnd = pressEnd2 + HoldMs;
            float fadeEnd = holdEnd + FadeMs;
            if (t < AppearMs)
            {
                alpha = t / AppearMs;
            }
            else if (t < pressEnd1)
            {
                ripple = (t - AppearMs) / PressMs;
                press = Mathf.Sin(Mathf.PI * ripple);
            }
            else if (t < moveEnd)
            {
                float k = (t - pressEnd1) / MoveMs;
                float e = k * k * (3f - (2f * k));
                pos = Vector2.Lerp(_srcTap, _dstTap, e) + new Vector2(0f, _arcLift * Mathf.Sin(Mathf.PI * e));
            }
            else if (t < pressEnd2)
            {
                pos = _dstTap;
                ripple = (t - moveEnd) / PressMs;
                press = Mathf.Sin(Mathf.PI * ripple);
            }
            else if (t < holdEnd)
            {
                pos = _dstTap;
            }
            else if (t < fadeEnd)
            {
                pos = _dstTap;
                alpha = 1f - ((t - holdEnd) / FadeMs);
            }
            else
            {
                pos = _dstTap;
                alpha = 0f;
            }

            hand.anchoredPosition = pos;
            float scale = 1f - (PressDepth * press);
            hand.localScale = new Vector3(scale, scale, 1f);
            SetAlpha(_hand, Mathf.Clamp01(alpha));
            if (ripple >= 0f)
            {
                float d = Mathf.Lerp(RippleStartDp, RippleEndDp, ripple);
                PlaceRipple(_rippleOutline.rectTransform, pos, d);
                PlaceRipple(_ripple.rectTransform, pos, d);
                SetAlpha(_rippleOutline, RippleOutlineAlpha * (1f - ripple), Palette.UiInk);
                SetAlpha(_ripple, RippleAlpha * (1f - ripple), Palette.UiPaper);
            }
            else
            {
                SetAlpha(_rippleOutline, 0f, Palette.UiInk);
                SetAlpha(_ripple, 0f, Palette.UiPaper);
            }
        }

        // La flecha de Reducir movimiento: de sobre la boca del origen a sobre la boca del destino, con su punta en el destino.
        private void PlaceArrow()
        {
            Vector2 from = _srcRim;
            Vector2 to = _dstRim;
            Vector2 d = to - from;
            float len = d.magnitude;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);
            float shaftLen = Mathf.Max(0f, len - (0.6f * ProceduralArt.FingerArrowHeadWidthDp));
            PlaceBar(_shaftOutline.rectTransform, from, shaftLen + ArrowOutlineDp, ArrowShaftDp + (2f * ArrowOutlineDp), rot);
            PlaceBar(_shaft.rectTransform, from, shaftLen, ArrowShaftDp, rot);
            SetAlpha(_shaftOutline, 1f, Palette.UiInk);
            SetAlpha(_shaft, 1f, Palette.UiPaper);
            RectTransform head = _head.rectTransform;
            head.anchoredPosition = to;
            head.localRotation = rot;
            SetAlpha(_head, 1f);
        }

        private static void PlaceRipple(RectTransform rt, Vector2 pos, float diameter)
        {
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(diameter, diameter);
        }

        private static void PlaceBar(RectTransform rt, Vector2 from, float length, float thickness, Quaternion rot)
        {
            rt.anchoredPosition = from;
            rt.sizeDelta = new Vector2(length, thickness);
            rt.localRotation = rot;
        }

        private static void SetAlpha(Image img, float alpha)
        {
            SetAlpha(img, alpha, Color.white);
        }

        private static void SetAlpha(Image img, float alpha, Color tint)
        {
            img.color = new Color(tint.r, tint.g, tint.b, alpha);
        }
    }
}
