#nullable enable
using System.Collections.Generic;
using AF.GameFlow;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>Lo que TiTino hace en una victoria según el alto de su caja (propuesta de TiTino d.4).</summary>
    internal enum SlothVictoryMode : byte
    {
        /// <summary>No sale: caja de menos de <see cref="SlothVictory.GestureBoxDp"/> dp, 3 filas de cuias o sin arte.</summary>
        None = 0,

        /// <summary>Solo el gesto, a <see cref="SlothVictory.GestureSizeDp"/> dp como mucho (caja de 56-103 dp).</summary>
        Gesture = 1,

        /// <summary>Gesto a <see cref="SlothVictory.SpeakSizeDp"/> dp como mucho y sitio para el bocadillo (caja de ≥ 104 dp).</summary>
        Speak = 2,
    }

    /// <summary>
    /// TiTino en la victoria (tarea TI6a; propuesta de TiTino d.4 y paso 6a de (i); 02 §6, fila TiTino; 07 §5.4, filas 0,60,
    /// 1,10 y 4,00; D-013, T1-A y T6-A; D-016): el rig provisional (<see cref="SlothRig"/>) colgado de su rama a la izquierda de la caja
    /// libre entre la cabecera y la cima de las cuias tras ShiftToClear, y su bocadillo a la derecha.
    /// <list type="bullet">
    /// <item><b>Caja</b> (<see cref="ModeFor"/>): ≥ <see cref="SpeakBoxDp"/> dp, gesto y bocadillo; de <see cref="GestureBoxDp"/> a
    /// 103, solo el gesto; menos, o con 3 filas de cuias, no sale. TiTino se encoge lo justo para caber entero en la caja con su
    /// margen (<see cref="SizeFor"/>), con todo lo que mueve su gesto (la garra alzada, la vuelta de <c>sloth_cheer_big</c> y
    /// las hojas que caen).</item>
    /// <item><b>Entrada</b> [P]: su rama entra desde el borde izquierdo en <see cref="EnterMs"/> ms al abrirse la pantalla de
    /// victoria (la fila 0,80, con la subida del panel), con el gesto (<c>sloth_cheer</c> o <c>sloth_cheer_big</c>) desde el
    /// primer fotograma; con Reducir movimiento, quieto en su sitio y con el fundido de 150 ms del rig. 07 §5.4 y el paso 6a
    /// piden la fila 0,60 (200 ms antes que el panel), pero la caja solo se conoce al abrirse la victoria: depende del alto del
    /// panel (la Cosecha doble, que se decide ahí con el anuncio listo; el chip del nivel siguiente, que solo se reserva con la
    /// fase VICTORY_SCREEN; la cinta y la ranura), que fija lo que sube el tablero con ShiftToClear. Desfase de 200 ms
    /// pendiente del dueño (TI6a).</item>
    /// <item><b>Bocadillo</b>: a los <see cref="BubbleInMs"/> ms de la pantalla de victoria (la fila 1,10), vidrio esmerilado
    /// con borde de madera clara (D-003), ancho de <see cref="BubbleMaxWidthDp"/> dp como mucho, texto a
    /// <see cref="BubbleTextSp"/> sp en ≤ <see cref="BubbleMaxLines"/> líneas, que baja hasta 12 sp antes de pasar a otra
    /// línea y nunca de ahí (02 §12; al 130 %, la caja crece hacia arriba). Se desvanece a los <see cref="BubbleOutMs"/> ms (la
    /// fila 4,00) o con «Siguiente» (<see cref="DismissBubble"/>).</item>
    /// <item>Nunca toca la cinta, las cuias, el saldo, la Crecida, la ranura ni los botones: todo queda dentro de la caja.
    /// Ningún gráfico es <c>raycastTarget</c> y la capa no bloquea toques. No retrasa «Siguiente» ni nada del panel.</item>
    /// </list>
    /// <para>Perezoso: hasta la primera victoria en la que sale no se crea ni el rig ni el bocadillo. Sin el arte del rig (release
    /// sin el PNG final: <see cref="ProceduralArt.SlothRigArt()"/>), no sale nunca. Nada asigna memoria por fotograma; el texto
    /// del bocadillo se mide al enseñarlo.</para>
    /// </summary>
    internal sealed class SlothVictory
    {
        /// <summary>Caja con la que cabe el bocadillo (propuesta de TiTino d.4) [P].</summary>
        public const float SpeakBoxDp = 104f;

        /// <summary>Caja mínima para que TiTino salga, solo con el gesto (d.4) [P].</summary>
        public const float GestureBoxDp = 56f;

        /// <summary>
        /// Tamaño S de TiTino con bocadillo, como mucho (c.4 y d.4: 72 dp en la victoria) [P]. Con todo lo que mueve su gesto
        /// (<see cref="RigUpS"/> + <see cref="RigDownS"/> = 1,48 S) y su margen, 72 dp piden una caja de ≥ 114,6 dp: con la
        /// caja mínima de <see cref="SpeakBoxDp"/> (104 dp) se encoge a 64 dp (<see cref="SizeFor"/>). Desviación de d.4
        /// pendiente del dueño (TI6a): subir el umbral a 115 dp o aceptar 64.
        /// </summary>
        public const float SpeakSizeDp = 72f;

        /// <summary>
        /// Tamaño S de TiTino solo con el gesto, como mucho (c.4 y d.4: 48 dp) [P]. 48 dp piden una caja de ≥ 79,1 dp: con la
        /// caja mínima de <see cref="GestureBoxDp"/> (56 dp) se encoge a 32 dp. Desviación de d.4 pendiente del dueño (TI6a):
        /// subir el umbral a 80 dp o aceptar 32.
        /// </summary>
        public const float GestureSizeDp = 48f;

        /// <summary>
        /// Lo que sube el rig por encima de la línea de su rama, en S: la garra alzada de <c>sloth_cheer</c> con su grosor y la
        /// media rama (SlothRigTests y SlothVictoryTests lo comprueban con las piezas de verdad).
        /// </summary>
        public const float RigUpS = 0.34f;

        /// <summary>Lo que baja el rig por debajo de la línea de su rama, en S: el torso colgado y las hojas que caen.</summary>
        public const float RigDownS = 1.14f;

        /// <summary>Lo que se aparta el rig a la izquierda de su raíz, en S: el cuerpo a media vuelta de <c>sloth_cheer_big</c>.</summary>
        public const float RigLeftS = 1.04f;

        /// <summary>Media rama, en S: el bocadillo empieza donde acaba (<see cref="SlothArt.BranchWidthS"/> = 1,5).</summary>
        public const float BranchHalfS = 0.75f;

        /// <summary>Margen de TiTino y del bocadillo dentro de la caja, arriba y abajo (dp) [P].</summary>
        public const float InsetDp = 4f;

        /// <summary>Margen con el borde izquierdo de la pantalla en la vuelta de <c>sloth_cheer_big</c> (dp) [P].</summary>
        public const float LeftEdgeDp = 2f;

        /// <summary>Margen del bocadillo con el borde derecho de la pantalla (dp; d.4: 16) [P].</summary>
        public const float RightEdgeDp = 16f;

        /// <summary>Hueco entre el final de la rama y el bocadillo (dp) [P].</summary>
        public const float BubbleGapDp = 6f;

        /// <summary>
        /// Entrada de la rama desde el borde izquierdo: 0,3 s (07 §5.4), desde que se abre la pantalla de victoria (la fila 0,80,
        /// no la 0,60: la caja depende del panel, que se coloca ahí) [P], así que la rama llega a su sitio a la vez que sale el
        /// bocadillo (fila 1,10).
        /// </summary>
        public const float EnterMs = 300f;

        /// <summary>
        /// El bocadillo sale a los 300 ms de la pantalla de victoria, que se abre con la subida del panel (la fila 0,80): es la
        /// fila 1,10 de 07 §5.4 (02 §6, fila TiTino).
        /// </summary>
        public const float BubbleInMs = 300f;

        /// <summary>El bocadillo se va a los 3.200 ms de la pantalla de victoria: la fila 4,00 (02 §6, fila TiTino).</summary>
        public const float BubbleOutMs = 3200f;

        /// <summary>Fundido de entrada y de salida del bocadillo (sin él con Reducir movimiento) [P].</summary>
        public const float BubbleFadeMs = 150f;

        /// <summary>Ancho máximo del bocadillo (d.4: 360 − 2 × 16 − 72 − 16 = 240 dp) [P].</summary>
        public const float BubbleMaxWidthDp = 240f;

        /// <summary>Texto del bocadillo (02 §6: 14 sp; nunca menos de <see cref="UiBuilder.MinTextSp"/>).</summary>
        public const float BubbleTextSp = 14f;

        /// <summary>Líneas del bocadillo a su tamaño (02 §6: ≤ 2); si no caben ni a 12 sp, crece hacia arriba.</summary>
        public const int BubbleMaxLines = 2;

        /// <summary>Aire del texto a cada lado dentro del bocadillo (dp) [P].</summary>
        public const float BubblePadXDp = 12f;

        /// <summary>Aire del texto arriba y abajo dentro del bocadillo (dp) [P].</summary>
        public const float BubblePadYDp = 7f;

        /// <summary>Grosor del borde de madera clara del bocadillo (dp) [P].</summary>
        public const float BubbleRimDp = 2f;

        // Pico del bocadillo hacia TiTino: un cuadrado girado 45° de este lado (dp), medio fuera del bocadillo; asoma media
        // diagonal del cuadrado con su borde.
        private const float TailDp = 12f;
        private const float TailOutDp = (TailDp + (2f * BubbleRimDp)) * 0.7072f;

        // Holgura del ancho ajustado al texto (dp): la de redondeo de TextMeshPro, para que no salte de línea.
        private const float FitSlackDp = 2f;

        // Vidrio esmerilado (GlassRim casi opaco: el texto se lee sobre cualquier fondo) y madera clara (D-003).
        private static readonly Color GlassColor = new Color(0xEA / 255f, 0xF7 / 255f, 0xF3 / 255f, 0.96f);
        private static readonly Color RimColor = Palette.FrameBevel;

        private readonly RectTransform _layer;
        private readonly ProceduralArt _art;
        private SlothRig? _rig;
        private bool _artMissing;
        private RectTransform? _bubble;
        private CanvasGroup? _bubbleGroup;
        private Image? _bubbleRim;
        private Image? _bubblePlate;
        private Image? _tailRim;
        private Image? _tailPlate;
        private TextMeshProUGUI? _label;
        private float _scale = 1f;
        private float _clockMs = -1f;
        private float _bubbleDismissMs = -1f;
        private bool _reduceMotion;
        private bool _hasText;
        private SlothPoseId _pose;
        private float _top;
        private float _bottom;
        private float _canvasW;
        private float _rootX;
        private float _rootY;
        private float _bubbleLeft;
        private float _bubbleTop;
        private float _bubbleWidth;
        private float _bubbleHeight;
        private string? _measuredText;
        private float _measuredMaxWidth = -1f;
        private float _measuredScale = -1f;
        private float _measuredWidth;
        private float _measuredHeight;
        private float _measuredSize;
        private int _measuredLines;

        /// <summary>Crea la capa de TiTino (vacía, sin toques) dentro de <paramref name="parent"/>, a pantalla completa.</summary>
        public SlothVictory(RectTransform parent, ProceduralArt art)
        {
            _art = art;
            _layer = UiBuilder.Rect("SlothVictory", parent);
            CanvasGroup group = _layer.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            _layer.gameObject.SetActive(false);
        }

        /// <summary>Capa de TiTino (a pantalla completa, pivote en el centro, como el lienzo).</summary>
        public RectTransform Layer => _layer;

        /// <summary>Lo que hace TiTino en la victoria de ahora (<see cref="SlothVictoryMode.None"/> si no se ve).</summary>
        public SlothVictoryMode Mode { get; private set; }

        /// <summary>TiTino se ve.</summary>
        public bool Visible => _layer.gameObject.activeSelf && _rig != null && _rig.Visible;

        /// <summary>El rig (null hasta la primera victoria en la que sale, o sin arte).</summary>
        public SlothRig? Rig => _rig;

        /// <summary>Tamaño S de TiTino ahora (dp; 0 si no se ve).</summary>
        public float SizeDp => Visible ? _rig!.Size : 0f;

        /// <summary>Milisegundos desde que se abrió la pantalla de victoria con TiTino (−1 si no se ve).</summary>
        public float ClockMs => _clockMs;

        /// <summary>Gesto que hace (<c>sloth_cheer</c> o <c>sloth_cheer_big</c>).</summary>
        public SlothPoseId Pose => _pose;

        /// <summary>Línea de la rama: x (dp desde la izquierda) en este momento de la entrada.</summary>
        public float RootX => _rig != null ? _rig.Root.anchoredPosition.x : 0f;

        /// <summary>Línea de la rama en reposo: x (dp desde la izquierda).</summary>
        public float RestX => _rootX;

        /// <summary>Línea de la rama: y (dp desde arriba).</summary>
        public float RootY => _rootY;

        /// <summary>Esta victoria lleva bocadillo (aunque aún no se vea o ya se haya ido).</summary>
        public bool HasBubble => _hasText && Mode == SlothVictoryMode.Speak;

        /// <summary>El bocadillo está a la vista (con alfa &gt; 0).</summary>
        public bool BubbleShown => _bubble != null && _bubble.gameObject.activeSelf && _bubbleGroup!.alpha > 0f;

        /// <summary>Opacidad del bocadillo (0 si no hay).</summary>
        public float BubbleAlpha => _bubble != null && _bubble.gameObject.activeSelf ? _bubbleGroup!.alpha : 0f;

        /// <summary>Texto del bocadillo («» si no hay).</summary>
        public string BubbleText => _label != null && HasBubble ? _label.text : string.Empty;

        /// <summary>Texto del bocadillo (tests de desbordes; null hasta el primer bocadillo).</summary>
        public TextMeshProUGUI? BubbleLabel => _label;

        /// <summary>Líneas del texto del bocadillo a su tamaño (0 sin bocadillo).</summary>
        public int BubbleLines => HasBubble ? _measuredLines : 0;

        /// <summary>Tamaño del texto del bocadillo (dp; 0 sin bocadillo).</summary>
        public float BubbleFontDp => HasBubble ? _measuredSize : 0f;

        /// <summary>Rectángulo del bocadillo con su borde y su pico (dp, y desde arriba; vacío sin bocadillo).</summary>
        public Rect BubbleRect => HasBubble
            ? Rect.MinMaxRect(_bubbleLeft - TailOutDp, _bubbleTop - BubbleRimDp, _bubbleLeft + _bubbleWidth + BubbleRimDp,
                _bubbleTop + _bubbleHeight + BubbleRimDp)
            : default;

        /// <summary>Canto de arriba de la caja de esta victoria (dp desde arriba).</summary>
        public float BoxTop => _top;

        /// <summary>Canto de abajo de la caja de esta victoria (dp desde arriba).</summary>
        public float BoxBottom => _bottom;

        /// <summary>
        /// Lo que hace TiTino con una caja de <paramref name="boxDp"/> dp y <paramref name="rows"/> filas de cuias (d.4): con 3
        /// filas, nada (07 §5.1: no queda franja libre); si no, según el alto.
        /// </summary>
        public static SlothVictoryMode ModeFor(float boxDp, int rows)
        {
            if (rows >= 3 || boxDp < GestureBoxDp)
            {
                return SlothVictoryMode.None;
            }

            return boxDp >= SpeakBoxDp ? SlothVictoryMode.Speak : SlothVictoryMode.Gesture;
        }

        /// <summary>
        /// Tamaño S de TiTino en una caja de <paramref name="boxDp"/> dp con el modo <paramref name="mode"/>: el del modo
        /// (<see cref="SpeakSizeDp"/> o <see cref="GestureSizeDp"/>) o, si no cabe entero con su margen, el que cabe.
        /// </summary>
        public static float SizeFor(SlothVictoryMode mode, float boxDp)
        {
            if (mode == SlothVictoryMode.None)
            {
                return 0f;
            }

            float target = mode == SlothVictoryMode.Speak ? SpeakSizeDp : GestureSizeDp;
            float fit = (boxDp - (2f * InsetDp)) / (RigUpS + RigDownS);
            return Mathf.Max(1f, Mathf.Min(target, Mathf.Floor(fit)));
        }

        /// <summary>
        /// Si el bocadillo con <paramref name="text"/> cabe en la caja de <paramref name="top"/> a <paramref name="bottom"/> (dp
        /// desde arriba) en un lienzo de <paramref name="canvasW"/> dp, con TiTino en el modo <see cref="SlothVictoryMode.Speak"/>.
        /// Mide el texto (crea el bocadillo la primera vez): solo al decidir la victoria, nunca por fotograma.
        /// </summary>
        public bool BubbleFits(string text, float top, float bottom, float canvasW)
        {
            float box = bottom - top;
            if (ModeFor(box, 1) != SlothVictoryMode.Speak || string.IsNullOrEmpty(text))
            {
                return false;
            }

            EnsureBubble();
            float s = SizeFor(SlothVictoryMode.Speak, box);
            float rootX = RestXFor(s);
            float maxWidth = BubbleWidthLimit(rootX, s, canvasW);
            if (maxWidth < (2f * BubblePadXDp) + 48f)
            {
                return false;
            }

            Measure(text, maxWidth);
            return _measuredHeight + (2f * InsetDp) <= box;
        }

        /// <summary>
        /// Enseña a TiTino en una victoria: la caja de <paramref name="top"/> a <paramref name="bottom"/> (dp desde arriba) con
        /// <paramref name="rows"/> filas, el gesto <paramref name="gesture"/> y, si cabe, el bocadillo con <paramref name="text"/>
        /// (null: sin frase). false (y no sale) si la caja no da o no hay arte.
        /// </summary>
        public bool Show(float top, float bottom, int rows, SlothGesture gesture, string? text, float canvasW, bool reduceMotion)
        {
            SlothVictoryMode mode = ModeFor(bottom - top, rows);
            if (mode == SlothVictoryMode.None || gesture == SlothGesture.None || !EnsureArt())
            {
                Hide();
                return false;
            }

            _reduceMotion = reduceMotion;
            _pose = gesture == SlothGesture.CheerBig ? SlothPoseId.CheerBig : SlothPoseId.Cheer;
            _hasText = text != null && mode == SlothVictoryMode.Speak;
            if (_hasText)
            {
                EnsureBubble();
                _label!.text = text;
            }

            _clockMs = 0f;
            _bubbleDismissMs = -1f;
            _layer.gameObject.SetActive(true);
            Place(top, bottom, canvasW, mode);
            _rig!.Play(_pose, reduceMotion);
            Apply();
            return Mode != SlothVictoryMode.None;
        }

        /// <summary>
        /// La caja cambió con la victoria a la vista (la Cosecha doble recoloca el panel y el tablero, o la pantalla cambió de
        /// tamaño): TiTino se recoloca, se encoge o se va, y el bocadillo se va si ya no cabe. No mide nada si no cambió.
        /// </summary>
        public void Relayout(float top, float bottom, int rows, float canvasW)
        {
            if (_clockMs < 0f || (Mathf.Abs(top - _top) < 0.5f && Mathf.Abs(bottom - _bottom) < 0.5f && Mathf.Abs(canvasW - _canvasW) < 0.5f))
            {
                return;
            }

            SlothVictoryMode mode = ModeFor(bottom - top, rows);
            if (mode == SlothVictoryMode.None)
            {
                Hide();
                return;
            }

            Place(top, bottom, canvasW, mode);
            Apply();
        }

        /// <summary>Avanza el reloj de la victoria: la entrada de la rama, el gesto y el bocadillo. Sin asignaciones.</summary>
        public void Tick(float dtMs)
        {
            if (_clockMs < 0f || _rig == null)
            {
                return;
            }

            _clockMs += Mathf.Max(0f, dtMs);
            _rig.Tick(dtMs);
            Apply();
        }

        /// <summary>«Siguiente» (AD_GATE) o «Inicio»: el bocadillo se desvanece desde ya (si aún no salió, ya no sale).</summary>
        public void DismissBubble()
        {
            if (_clockMs >= 0f && _bubbleDismissMs < 0f)
            {
                _bubbleDismissMs = _clockMs;
                Apply();
            }
        }

        /// <summary>Oculta a TiTino y su bocadillo (fuera de la victoria).</summary>
        public void Hide()
        {
            Mode = SlothVictoryMode.None;
            _clockMs = -1f;
            _hasText = false;
            if (_bubble != null && _bubble.gameObject.activeSelf)
            {
                _bubble.gameObject.SetActive(false);
            }

            _rig?.SetVisible(false);
            if (_layer.gameObject.activeSelf)
            {
                _layer.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Añade las cajas de lo que se ve de TiTino (sus piezas) y del bocadillo, en dp del lienzo con la y desde arriba (tests de
        /// solapes; <paramref name="space"/>, una capa a pantalla completa con el pivote en el centro).
        /// </summary>
        public void CollectRects(RectTransform space, List<Rect> rects, bool bubble)
        {
            if (!Visible)
            {
                return;
            }

            _rig!.CollectRects(space, rects);
            if (bubble && BubbleShown)
            {
                rects.Add(BubbleRect);
            }
        }

        /// <summary>Añade los gráficos de TiTino y del bocadillo (tests de raycast).</summary>
        public void CollectGraphics(List<Graphic> graphics)
        {
            _rig?.CollectGraphics(graphics);
            if (_bubble != null)
            {
                graphics.Add(_bubbleRim!);
                graphics.Add(_bubblePlate!);
                graphics.Add(_tailRim!);
                graphics.Add(_tailPlate!);
                graphics.Add(_label!);
            }
        }

        /// <summary>El grupo de la capa bloquea toques (siempre false).</summary>
        public bool BlocksRaycasts => _layer.GetComponent<CanvasGroup>().blocksRaycasts;

        // x de la línea de la rama en reposo con TiTino a s dp: la vuelta de sloth_cheer_big no sale de la pantalla.
        private static float RestXFor(float s)
        {
            return LeftEdgeDp + (RigLeftS * s);
        }

        // Ancho máximo del bocadillo con la rama en rootX: del final de la rama al margen derecho, y como mucho 240 dp.
        private static float BubbleWidthLimit(float rootX, float s, float canvasW)
        {
            float left = rootX + (BranchHalfS * s) + BubbleGapDp + TailOutDp;
            return Mathf.Min(BubbleMaxWidthDp, canvasW - RightEdgeDp - left);
        }

        private void Place(float top, float bottom, float canvasW, SlothVictoryMode mode)
        {
            _top = top;
            _bottom = bottom;
            _canvasW = canvasW;
            float box = bottom - top;
            float s = SizeFor(mode, box);
            if (!Mathf.Approximately(_rig!.Size, s))
            {
                _rig.SetArt(_rig.Art, s);
            }

            // TiTino cuelga arriba de la caja, bajo la cabecera, con su garra alzada dentro del margen.
            _rootX = RestXFor(s);
            _rootY = top + InsetDp + (RigUpS * s);
            Mode = mode;

            if (_hasText && mode == SlothVictoryMode.Speak)
            {
                float maxWidth = BubbleWidthLimit(_rootX, s, canvasW);
                Measure(_label!.text, maxWidth);
                if (_measuredHeight + (2f * InsetDp) > box || maxWidth < (2f * BubblePadXDp) + 48f)
                {
                    _hasText = false; // ya no cabe (la caja cambió): la frase se pierde, nunca se cruza con nada
                }
                else
                {
                    _bubbleWidth = _measuredWidth;
                    _bubbleHeight = _measuredHeight;
                    _bubbleLeft = _rootX + (BranchHalfS * s) + BubbleGapDp + TailOutDp;
                    _bubbleWidth = Mathf.Min(_bubbleWidth, canvasW - RightEdgeDp - _bubbleLeft);

                    // A la altura de la cabeza de TiTino y dentro de la caja; si crece, hacia arriba.
                    float head = _rootY + (0.28f * s);
                    float maxTop = bottom - InsetDp - _bubbleHeight;
                    _bubbleTop = Mathf.Clamp(head - (_bubbleHeight * 0.5f), top + InsetDp, maxTop);
                    UiBuilder.Place(_bubble!, _bubbleLeft, _bubbleTop, _bubbleWidth, _bubbleHeight);
                    float tailY = Mathf.Clamp(head - _bubbleTop, TailDp, _bubbleHeight - TailDp);
                    PlaceTail(_tailRim!.rectTransform, tailY, TailDp + (2f * BubbleRimDp));
                    PlaceTail(_tailPlate!.rectTransform, tailY, TailDp);
                }
            }
            else
            {
                _hasText = false;
            }
        }

        private static void PlaceTail(RectTransform rt, float y, float size)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(0f, -y);
            rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        // La entrada de la rama y el bocadillo en el instante de ahora. Sin asignaciones.
        private void Apply()
        {
            if (_rig == null || _clockMs < 0f)
            {
                return;
            }

            float x = _rootX;
            if (!_reduceMotion && _clockMs < EnterMs)
            {
                // Desde fuera del borde izquierdo: la punta derecha de la rama asoma al empezar.
                float from = -(BranchHalfS * _rig.Size) - (RigLeftS * _rig.Size);
                x = Mathf.LerpUnclamped(from, _rootX, Ease.OutCubic(_clockMs / EnterMs));
            }

            _rig.Place(x, _rootY);

            if (_bubble == null)
            {
                return;
            }

            float alpha = BubbleAlphaAt(_clockMs);
            bool on = HasBubble && alpha > 0f;
            if (_bubble.gameObject.activeSelf != on)
            {
                _bubble.gameObject.SetActive(on);
            }

            if (on && !Mathf.Approximately(_bubbleGroup!.alpha, alpha))
            {
                _bubbleGroup.alpha = alpha;
            }
        }

        // Opacidad del bocadillo a los ms de la victoria: entra a los 1,10 s y se va a los 4 s o al cerrar la victoria; con
        // Reducir movimiento, sin fundidos.
        private float BubbleAlphaAt(float ms)
        {
            if (!HasBubble || ms < BubbleInMs)
            {
                return 0f;
            }

            float outAt = _bubbleDismissMs >= 0f ? Mathf.Min(_bubbleDismissMs, BubbleOutMs) : BubbleOutMs;
            if (_reduceMotion)
            {
                return ms < outAt ? 1f : 0f;
            }

            float fadeIn = Mathf.Clamp01((ms - BubbleInMs) / BubbleFadeMs);
            float fadeOut = 1f - Mathf.Clamp01((ms - Mathf.Max(outAt, BubbleInMs)) / BubbleFadeMs);
            if (_bubbleDismissMs >= 0f && _bubbleDismissMs < BubbleInMs)
            {
                return 0f; // se cerró la victoria antes de que saliera: ya no sale
            }

            return Mathf.Min(fadeIn, fadeOut);
        }

        /// <summary>
        /// Crea el rig la primera vez que hace falta, con la regla del arte (sin arte final en release, nunca: false). Crea el
        /// atlas provisional la primera vez (en el Editor y en desarrollo).
        /// </summary>
        public bool EnsureArt()
        {
            if (_rig != null)
            {
                return true;
            }

            if (_artMissing)
            {
                return false;
            }

            SlothArt? art = _art.SlothRigArt();
            if (art == null)
            {
                _artMissing = true;
                return false;
            }

            _rig = SlothRig.Create(_layer, art, SpeakSizeDp);
            if (_bubble != null)
            {
                _bubble.SetAsLastSibling(); // el bocadillo, delante de las hojas que caen
            }

            return _rig != null;
        }

        // El bocadillo, la primera vez que hace falta: borde de madera clara, vidrio esmerilado, pico hacia TiTino y el texto.
        private void EnsureBubble()
        {
            if (_bubble != null)
            {
                return;
            }

            _scale = UiBuilder.TextScale;
            _bubble = UiBuilder.Rect("Bubble", _layer);
            _bubbleGroup = _bubble.gameObject.AddComponent<CanvasGroup>();
            _bubbleGroup.blocksRaycasts = false;
            _bubbleGroup.interactable = false;
            _tailRim = UiBuilder.Image("TailRim", _bubble, null, RimColor);
            _bubbleRim = UiBuilder.Sliced("Rim", _bubble, _art.RoundedRect, RimColor);
            _bubbleRim.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(_bubbleRim.rectTransform);
            _bubbleRim.rectTransform.offsetMin = new Vector2(-BubbleRimDp, -BubbleRimDp);
            _bubbleRim.rectTransform.offsetMax = new Vector2(BubbleRimDp, BubbleRimDp);
            _tailPlate = UiBuilder.Image("Tail", _bubble, null, GlassColor);
            _bubblePlate = UiBuilder.Sliced("Plate", _bubble, _art.RoundedRect, GlassColor);
            _bubblePlate.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(_bubblePlate.rectTransform);
            _label = UiBuilder.Label("Text", _bubble, TextStyle.PlainBold, string.Empty, BubbleTextSp, Palette.UiInk, TextAlignmentOptions.Center);
            _label.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.Stretch(_label.rectTransform);
            _label.rectTransform.offsetMin = new Vector2(BubblePadXDp, BubblePadYDp);
            _label.rectTransform.offsetMax = new Vector2(-BubblePadXDp, -BubblePadYDp);
            _bubble.gameObject.SetActive(false);
        }

        // Mide el bocadillo con text en maxWidth dp (con su aire): de 14 sp hacia abajo, el primer tamaño en el que cabe en 2
        // líneas, nunca por debajo de 12 sp; si ni así cabe, 12 sp y las líneas que hagan falta (crece hacia arriba). Ancho
        // ajustado al texto si va en una línea. En caché por texto, ancho y escala.
        private void Measure(string text, float maxWidth)
        {
            if (ReferenceEquals(text, _measuredText) && Mathf.Approximately(maxWidth, _measuredMaxWidth) && Mathf.Approximately(_scale, _measuredScale))
            {
                ApplyMeasuredSize();
                return;
            }

            TextMeshProUGUI label = _label!;
            label.enableAutoSizing = false;
            float inner = Mathf.Max(1f, maxWidth - (2f * BubblePadXDp));
            float max = BubbleTextSp * _scale;
            float min = UiBuilder.MinTextSp * _scale;
            float size = max;
            int lines = 1;
            Vector2 block = Vector2.zero;
            for (float sp = BubbleTextSp; sp >= UiBuilder.MinTextSp - 0.01f; sp -= 0.5f)
            {
                size = Mathf.Max(min, sp * _scale);
                label.fontSize = size;
                block = label.GetPreferredValues(text, inner, float.PositiveInfinity);
                lines = LinesOf(label, block.y);
                if (lines <= BubbleMaxLines)
                {
                    break;
                }
            }

            float single = label.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x;
            float textWidth = lines <= 1 ? Mathf.Min(inner, single + FitSlackDp) : inner;
            _measuredText = text;
            _measuredMaxWidth = maxWidth;
            _measuredScale = _scale;
            _measuredSize = size;
            _measuredLines = lines;
            _measuredWidth = Mathf.Ceil(textWidth + (2f * BubblePadXDp));
            _measuredHeight = Mathf.Ceil(block.y + (2f * BubblePadYDp));
            ApplyMeasuredSize();
        }

        private void ApplyMeasuredSize()
        {
            if (!Mathf.Approximately(_label!.fontSize, _measuredSize))
            {
                _label.fontSize = _measuredSize;
            }
        }

        // Líneas que ocupa un bloque de height dp con el tamaño de ahora: el alto de una línea y el avance entre dos.
        private static int LinesOf(TextMeshProUGUI label, float height)
        {
            float one = label.GetPreferredValues("Ág", float.PositiveInfinity, float.PositiveInfinity).y;
            float two = label.GetPreferredValues("Ág\nÁg", float.PositiveInfinity, float.PositiveInfinity).y;
            float advance = Mathf.Max(1f, two - one);
            return 1 + Mathf.Max(0, Mathf.RoundToInt((height - one) / advance));
        }
    }
}
