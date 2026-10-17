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
    /// dos nunca coinciden: en esos niveles no hay aviso tras Volver a la orilla.</para>
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
        /// Como <see cref="Place(float, float)"/>, pero, si <paramref name="portraitX"/> no es NaN, desplazada a lo ancho para que
        /// el centro del retrato quede en <paramref name="portraitX"/> (dp desde la izquierda), sin salir de los 16 dp de cada
        /// lado: el tooltip de Marea, con TiTino señalando su botón (tarea TU1a; 04 §2.1).
        /// </summary>
        public void Place(float canvasW, float top, float portraitX)
        {
            float w = Mathf.Min(Mathf.Min(MaxWidthDp, canvasW - 32f), _cardWidth);
            float left = (canvasW - w) * 0.5f;
            if (!float.IsNaN(portraitX))
            {
                left = Mathf.Clamp(portraitX - PortraitCenterDp, 16f, Mathf.Max(16f, canvasW - 16f - w));
            }

            _left = left;
            UiBuilder.Place(_root, left, top, w, _height);
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
