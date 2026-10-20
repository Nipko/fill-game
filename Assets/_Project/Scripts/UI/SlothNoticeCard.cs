#nullable enable
using System.Collections.Generic;
using AF.GameFlow;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Aviso de TiTino tras Volver a la orilla en los niveles 1-30 (tarea TI6b; D-016, T5-B; 04 §2.7; propuesta de TiTino d.5;
    /// guion de la intro §14): una tarjeta de la familia de la del miquito (<see cref="SightingCard"/>: placa de madera clara con
    /// su canto) con el retrato de TiTino de <see cref="PortraitDp"/> dp, que asiente con el gesto <c>sloth_encourage</c> (la
    /// cabeza del rig con el ladeo y la cabezada de la pose; quieto con Reducir movimiento), y su frase
    /// (<c>sloth_line_restart_*</c> o la n.º 21) a <see cref="TextSp"/> sp en ≤ <see cref="MaxLines"/> líneas, nunca menos de
    /// 12 sp: la tarjeta crece en alto. Dura lo que se tarda en leerla (<see cref="NoticeTiming.ReadMs(string)"/>, D-011), con
    /// fundido de entrada y salida; no captura toques (<c>raycastTarget</c> = false y <see cref="CanvasGroup.blocksRaycasts"/>
    /// = false). Dónde va y cuándo sale lo decide la cola de avisos (GameScreen.Notices.cs). Perezosa: la crea el primer aviso.
    /// <para>La misma tarjeta lleva la instrucción del tutorial de 1, 2, 3 y 6 (tarea TU1a; 02 §13), con el gesto
    /// <c>sloth_present</c> (<see cref="Show(Sprite, bool, SlothPoseId)"/>): el retrato ladea la cabeza hacia la instrucción. Las
    /// dos nunca coinciden: en esos niveles no hay aviso tras Volver a la orilla. En el tooltip de Marea del 3 (04 §2.1: TiTino
    /// señala el botón), la tarjeta lleva además una cola de <see cref="TailWidthDp"/> × <see cref="TailHeightDp"/> dp en su borde
    /// de abajo, del relleno de la placa y con su canto, con la punta sobre el eje del botón
    /// (<see cref="Place(float, float, float)"/>): señala el botón sea cual sea el ancho de la tarjeta (revisión de TU1a).</para>
    /// </summary>
    internal sealed class SlothNoticeCard
    {
        /// <summary>Alto mínimo de la tarjeta a la escala de texto 1 (dp), como la del miquito.</summary>
        public const float HeightDp = 60f;

        /// <summary>Lado del retrato (dp; propuesta de TiTino c.4: retrato de aviso de 40 dp).</summary>
        public const float PortraitDp = 40f;

        /// <summary>El canto de madera asoma este alto bajo la placa (dp).</summary>
        public const float EdgeDp = 3f;

        /// <summary>Texto de la frase (como el bocadillo de la victoria: 14 sp).</summary>
        public const float TextSp = 14f;

        /// <summary>Líneas de la frase como mucho a su tamaño (02 §6 y d.5: ≤ 2); si no caben ni a 12 sp, el aviso no sale.</summary>
        public const int MaxLines = 2;

        /// <summary>Ancho máximo de la tarjeta (dp), como la del miquito.</summary>
        public const float MaxWidthDp = 320f;

        /// <summary>Ancho de la base de la cola que señala el botón de Marea (dp; revisión de TU1a) [P].</summary>
        public const float TailWidthDp = 16f;

        /// <summary>Alto de la cola bajo la placa, sin su canto (dp; revisión de TU1a) [P].</summary>
        public const float TailHeightDp = 10f;

        /// <summary>Lo que baja la punta de la cola, con su canto, por debajo de la placa (dp).</summary>
        public const float TailReachDp = TailHeightDp + EdgeDp;

        /// <summary>
        /// El eje de la cola queda, como mucho, a este aire de cada lado de la tarjeta (dp; revisión de TU1a) [P]: lejos de las
        /// esquinas redondeadas.
        /// </summary>
        public const float TailInsetDp = 20f;

        private const float FadeMs = 200f;
        private const float TextX = 11f + PortraitDp + 10f;
        private const float PortraitCenterDp = 11f + (PortraitDp * 0.5f); // centro del retrato desde el borde izquierdo
        private const float TextRightPadDp = 14f;
        private const float PadYDp = 9f;
        private const float FitSlackDp = 2f;

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _plate;
        private readonly Image _edge;
        private readonly RectTransform _tail;
        private readonly PointerTailGraphic _tailEdge;
        private readonly PointerTailGraphic _tailPlate;
        private readonly Image _halo;
        private readonly Image _portrait;
        private readonly TextMeshProUGUI _text;
        private readonly float _scale;
        private float _leftMs = -1f;
        private float _totalMs;
        private float _clockMs;
        private bool _reduceMotion;
        private SlothPoseId _pose = SlothPoseId.Encourage;
        private float _height = HeightDp;
        private float _textHeight;
        private float _textWidth;
        private float _cardWidth;
        private float _left;
        private float _tailX = float.NaN;
        private int _lines;

        public SlothNoticeCard(Transform parent, ProceduralArt art)
        {
            _scale = UiBuilder.TextScale;
            _root = UiBuilder.Rect("SlothNotice", parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _edge = UiBuilder.Sliced("Edge", _root, art.RoundedRect, Palette.FamilyWood.Base);
            _edge.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(_edge.rectTransform);
            _edge.rectTransform.offsetMin = new Vector2(0f, -EdgeDp);
            _edge.rectTransform.offsetMax = new Vector2(0f, -EdgeDp);
            _plate = UiBuilder.Sliced("Plate", _root, art.RoundedRect, Palette.PanelLight);
            _plate.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(_plate.rectTransform);

            // La cola del tooltip de Marea: su canto (la misma madera que el de la placa, EdgeDp más abajo) y su relleno, encima;
            // oculta salvo que Place la pida.
            _tail = UiBuilder.Rect("Tail", _root);
            _tailEdge = TailPart("TailEdge", _tail, Palette.FamilyWood.Base);
            _tailEdge.rectTransform.offsetMin = new Vector2(0f, -EdgeDp);
            _tailEdge.rectTransform.offsetMax = new Vector2(0f, -EdgeDp);
            _tailPlate = TailPart("TailPlate", _tail, Palette.PanelLight);
            _tail.gameObject.SetActive(false);
            _halo = UiBuilder.Image("PortraitBack", _root, art.Disc, Palette.PanelLightLow);
            _portrait = UiBuilder.Image("Portrait", _root, null, Color.white);
            _portrait.preserveAspect = true;
            RectTransform face = _portrait.rectTransform;
            face.pivot = new Vector2(0.5f, 0.5f);
            _text = UiBuilder.Label("Text", _root, TextStyle.PlainBold, string.Empty, TextSp, Palette.FamilyWood.Deep, TextAlignmentOptions.Left);
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.enableAutoSizing = false;
            _root.gameObject.SetActive(false);
        }

        /// <summary>La tarjeta está a la vista.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Raíz (tests de exclusión).</summary>
        public RectTransform Root => _root;

        /// <summary>Opacidad actual del fundido.</summary>
        public float Alpha => _group.alpha;

        /// <summary>Duración de la tarjeta a la vista, con sus fundidos (ms; tests).</summary>
        public float DurationMs => _totalMs;

        /// <summary>Alto de la tarjeta (dp) con el último <see cref="Measure"/>.</summary>
        public float Height => _height;

        /// <summary>Frase que muestra.</summary>
        public string Text => _text.text;

        /// <summary>La frase (tests de tamaño).</summary>
        public TextMeshProUGUI Label => _text;

        /// <summary>Líneas de la frase con el último <see cref="Measure"/> (0 si no cabe).</summary>
        public int Lines => _lines;

        /// <summary>Retrato (tests).</summary>
        public Image Portrait => _portrait;

        /// <summary>Centro del retrato a lo ancho (dp desde la izquierda del lienzo) con el último <c>Place</c>.</summary>
        public float PortraitCenterXDp => _left + PortraitCenterDp;

        /// <summary>La tarjeta lleva la cola que señala un botón (el tooltip de Marea del 3).</summary>
        public bool TailVisible => _tail.gameObject.activeSelf;

        /// <summary>Eje de la punta de la cola (dp desde la izquierda del lienzo) con el último <c>Place</c>; NaN sin cola.</summary>
        public float TailCenterXDp => TailVisible ? _tailX : float.NaN;

        /// <summary>Rectángulo de la cola, sin su canto (tests y el poste del muelle).</summary>
        public RectTransform Tail => _tail;

        /// <summary>Gesto del retrato: <c>sloth_encourage</c> en el aviso del reinicio, <c>sloth_present</c> en el tutorial.</summary>
        public SlothPoseId Pose => _pose;

        /// <summary>
        /// Pone la frase y devuelve el alto de la tarjeta en un lienzo de <paramref name="canvasW"/> dp, o −1 si la frase no cabe
        /// en <see cref="MaxLines"/> líneas ni a 12 sp: de <see cref="TextSp"/> sp hacia abajo, el primer tamaño en el que cabe;
        /// con dos líneas, la caja más estrecha que las mantiene (equilibradas) y la tarjeta encogida a su texto, centrada. Al
        /// menos <see cref="HeightDp"/> por la escala. Mide una vez por aviso, nunca por fotograma.
        /// </summary>
        public float Measure(float canvasW, string text)
        {
            _text.text = text;
            float column = Mathf.Min(MaxWidthDp, canvasW - 32f) - TextX - TextRightPadDp;
            float min = UiBuilder.MinTextSp * _scale;
            float size = TextSp * _scale;
            int lines = int.MaxValue;
            Vector2 block = Vector2.zero;
            for (float sp = TextSp; sp >= UiBuilder.MinTextSp - 0.01f; sp -= 0.5f)
            {
                size = Mathf.Max(min, sp * _scale);
                _text.fontSize = size;
                block = _text.GetPreferredValues(text, column, float.PositiveInfinity);
                lines = LinesOf(_text, block.y);
                if (lines <= MaxLines)
                {
                    break;
                }
            }

            if (lines > MaxLines || column < 48f)
            {
                _lines = 0;
                return -1f;
            }

            float single = _text.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x;
            float w = lines <= 1 ? Mathf.Min(column, single + FitSlackDp) : Mathf.Min(column, BalancedWidth(_text, text, column, single, lines, block.y) + FitSlackDp);
            _lines = lines;
            _textWidth = Mathf.Ceil(w);
            _textHeight = Mathf.Ceil(block.y);
            _cardWidth = Mathf.Ceil(TextX + _textWidth + TextRightPadDp);
            _height = Mathf.Max(HeightDp * _scale, _textHeight + (2f * PadYDp));
            return _height;
        }

        /// <summary>
        /// Coloca la tarjeta centrada en un lienzo de <paramref name="canvasW"/> dp con su borde de arriba en <paramref name="top"/>
        /// (dp desde arriba), con el último <see cref="Measure"/>: el retrato y la frase, centrados a lo alto.
        /// </summary>
        public void Place(float canvasW, float top)
        {
            Place(canvasW, top, float.NaN);
        }

        /// <summary>
        /// Como <see cref="Place(float, float)"/>, pero, si <paramref name="pointX"/> no es NaN, señalando el punto
        /// <paramref name="pointX"/> (dp desde la izquierda) que queda debajo: el tooltip de Marea, con TiTino señalando su botón
        /// (tarea TU1a; 04 §2.1). La tarjeta se desplaza a lo ancho para acercar el retrato a <paramref name="pointX"/>, sin salir
        /// de los 16 dp de cada lado, y lleva la cola en su borde de abajo con la punta en <paramref name="pointX"/> (a
        /// <see cref="TailInsetDp"/> dp de cada lado como mucho): así señala el botón aunque el margen no deje llevar el retrato
        /// encima (una tarjeta ancha en un lienzo estrecho; revisión de TU1a).
        /// </summary>
        public void Place(float canvasW, float top, float pointX)
        {
            float w = Mathf.Min(Mathf.Min(MaxWidthDp, canvasW - 32f), _cardWidth);
            float left = (canvasW - w) * 0.5f;
            bool tail = !float.IsNaN(pointX);
            if (tail)
            {
                left = Mathf.Clamp(pointX - PortraitCenterDp, 16f, Mathf.Max(16f, canvasW - 16f - w));
            }

            _left = left;
            UiBuilder.Place(_root, left, top, w, _height);
            _tailX = float.NaN;
            if (tail)
            {
                // La base entra 1 dp en la placa (sin costura) y la punta queda TailHeightDp por debajo de ella.
                _tailX = Mathf.Clamp(pointX, left + TailInsetDp, left + Mathf.Max(TailInsetDp, w - TailInsetDp));
                UiBuilder.Place(_tail, _tailX - left - (TailWidthDp * 0.5f), _height - 1f, TailWidthDp, TailHeightDp + 1f);
            }

            if (_tail.gameObject.activeSelf != tail)
            {
                _tail.gameObject.SetActive(tail);
            }

            float face = (_height - PortraitDp) * 0.5f;
            UiBuilder.Place(_halo.rectTransform, 9f, face - 2f, PortraitDp + 4f, PortraitDp + 4f);
            RectTransform portrait = _portrait.rectTransform;
            portrait.anchorMin = new Vector2(0f, 1f);
            portrait.anchorMax = new Vector2(0f, 1f);
            portrait.pivot = new Vector2(0.5f, 0.5f);
            portrait.sizeDelta = new Vector2(PortraitDp, PortraitDp);
            portrait.anchoredPosition = new Vector2(11f + (PortraitDp * 0.5f), -(face + (PortraitDp * 0.5f)));
            UiBuilder.Place(_text.rectTransform, TextX, (_height - _textHeight) * 0.5f, _textWidth, _textHeight);
        }

        /// <summary>
        /// Muestra el aviso con el retrato <paramref name="portrait"/> (la cabeza del rig de TiTino) y la frase ya medida, el tiempo
        /// de leerla (<see cref="NoticeTiming.ReadMs(string)"/>), con <c>sloth_encourage</c> o, con Reducir movimiento, quieto.
        /// </summary>
        public void Show(Sprite portrait, bool reduceMotion)
        {
            Show(portrait, reduceMotion, SlothPoseId.Encourage);
        }

        /// <summary>
        /// Como <see cref="Show(Sprite, bool)"/>, con el gesto <paramref name="pose"/> del retrato (<c>sloth_present</c> en la
        /// instrucción del tutorial, TU1a).
        /// </summary>
        public void Show(Sprite portrait, bool reduceMotion, SlothPoseId pose)
        {
            int ms = NoticeTiming.ReadMs(_text.text);
            _portrait.sprite = portrait;
            _pose = pose;
            _reduceMotion = reduceMotion;
            _clockMs = 0f;
            _leftMs = ms;
            _totalMs = Mathf.Max(1f, ms);
            _group.alpha = 0f;
            ApplyGesture();
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling(); // dentro de la capa de avisos: sigue detrás de la capa de vertido
        }

        /// <summary>
        /// Adelanta la salida: el aviso se va ya con su fundido normal (desde el alfa de ahora, como mucho
        /// <see cref="FadeMs"/>), sin cortarse. Idempotente y sin asignaciones; oculto, no hace nada (el dedo guía del 2, DC6).
        /// </summary>
        public void Leave()
        {
            if (_leftMs < 0f)
            {
                return;
            }

            _leftMs = Mathf.Min(_leftMs, Mathf.Max(1f, _group.alpha * FadeMs));
        }

        public void Hide()
        {
            _leftMs = -1f;
            _root.gameObject.SetActive(false);
        }

        /// <summary>Avanza el fundido y el gesto. Sin asignaciones.</summary>
        public void Tick(float dtMs)
        {
            if (_leftMs < 0f)
            {
                return;
            }

            _leftMs -= dtMs;
            _clockMs += Mathf.Max(0f, dtMs);
            float shown = _totalMs - _leftMs;
            _group.alpha = Mathf.Clamp01(Mathf.Min(shown, _leftMs) / FadeMs);
            ApplyGesture();
            if (_leftMs <= 0f)
            {
                Hide();
            }
        }

        /// <summary>Añade los gráficos de la tarjeta (tests de raycast).</summary>
        public void CollectGraphics(List<Graphic> graphics)
        {
            graphics.Add(_plate);
            graphics.Add(_edge);
            graphics.Add(_tailEdge);
            graphics.Add(_tailPlate);
            graphics.Add(_halo);
            graphics.Add(_portrait);
            graphics.Add(_text);
        }

        // El retrato asiente con sloth_encourage (o ladea la cabeza hacia la instrucción con sloth_present): el ladeo de la cabeza
        // del rig (SlothRig.Evaluate, sin asignaciones); con Reducir movimiento, su reposo.
        private void ApplyGesture()
        {
            SlothPose pose = SlothRig.Evaluate(_pose, _clockMs, _reduceMotion);
            Quaternion tilt = Quaternion.Euler(0f, 0f, pose.HeadAngle);
            if (_portrait.rectTransform.localRotation != tilt)
            {
                _portrait.rectTransform.localRotation = tilt;
            }
        }

        // Un triángulo de la cola (sin raycast), estirado a su rectángulo.
        private static PointerTailGraphic TailPart(string name, RectTransform parent, Color color)
        {
            RectTransform rt = UiBuilder.Rect(name, parent);
            PointerTailGraphic g = rt.gameObject.AddComponent<PointerTailGraphic>();
            g.color = color;
            g.raycastTarget = false;
            return g;
        }

        private static float BalancedWidth(TextMeshProUGUI label, string text, float inner, float single, int lines, float height)
        {
            float lo = Mathf.Min(inner, Mathf.Max(1f, (single / lines) - 1f));
            float hi = inner;
            for (int i = 0; i < 12 && hi - lo > 0.5f; i++)
            {
                float mid = 0.5f * (lo + hi);
                if (label.GetPreferredValues(text, mid, float.PositiveInfinity).y <= height + 0.5f)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid;
                }
            }

            return hi;
        }

        private static int LinesOf(TextMeshProUGUI label, float height)
        {
            float one = label.GetPreferredValues("Ág", float.PositiveInfinity, float.PositiveInfinity).y;
            float two = label.GetPreferredValues("Ág\nÁg", float.PositiveInfinity, float.PositiveInfinity).y;
            float advance = Mathf.Max(1f, two - one);
            return 1 + Mathf.Max(0, Mathf.RoundToInt((height - one) / advance));
        }
    }
}
