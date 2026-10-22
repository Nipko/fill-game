#nullable enable
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Tooltip del tutorial sin personaje (tarea B11): el tooltip «Reiniciar es gratis…» de los niveles 6 y 8 (02 §13; 06 §2.2;
    /// texto canon de 04 §2.7) y el de la Cosecha doble del 11 (06 §2.2: «presentado una sola vez con un tooltip sin personaje»;
    /// D-013, T1-A: con TiTino en la victoria, ninguna mascota presenta ni señala una oferta). El mismo vidrio esmerilado con borde
    /// de madera clara y el mismo pico que el bocadillo de TiTino (<see cref="SpeechBubble"/>, D-003), sin retrato ni tallo, y con
    /// el pico arriba o abajo: <see cref="PlaceAbove"/> lo deja encima de un punto (pico hacia abajo) y <see cref="PlaceBelow"/>,
    /// debajo (pico hacia arriba). No recibe toques. Gráfico provisional por código; cada fotograma solo cambia la opacidad
    /// (<see cref="Tick"/>, sin asignaciones); el texto se mide una vez por texto y ancho máximo (recolocarlo no lo vuelve a
    /// medir).
    /// </summary>
    internal sealed class TipBubble
    {
        /// <summary>Texto del tooltip (sp), el del bocadillo de TiTino (02 §6: 14 sp), con la escala de texto de 02 §12.</summary>
        public const float TextSp = SpeechBubble.TextSp;

        /// <summary>Fundido de entrada y de salida (ms) [P]: el de los avisos de texto.</summary>
        public const float FadeMs = 200f;

        private const float FitSlackDp = 2f;

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _tailShadow;
        private readonly Image _tailRim;
        private readonly Image _tailPlate;
        private readonly TextMeshProUGUI _label;
        private float _alpha;
        private float _target;

        // La medida del texto (ancho y alto del vidrio) para el ancho máximo con que se midió: se mide una vez por texto y ancho,
        // no cada vez que se recoloca (el ancla puede moverse en cada fotograma de una animación).
        private float _fitLimit = float.NaN;
        private float _fitWidth;
        private float _fitHeight;

        /// <summary>Crea el tooltip (oculto) bajo <paramref name="parent"/>, que se mide arriba a la izquierda en dp.</summary>
        public TipBubble(RectTransform parent, ProceduralArt art, string name)
        {
            _root = UiBuilder.Rect(name, parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _tailShadow = UiBuilder.Image("TailShadow", _root, null, new Color(0f, 0f, 0f, 0.15f));
            _tailRim = UiBuilder.Image("TailRim", _root, null, SpeechBubble.RimColor);
            Image rim = UiBuilder.Sliced("Rim", _root, art.RoundedRect, SpeechBubble.RimColor);
            rim.pixelsPerUnitMultiplier = 24f / 14f;
            rim.raycastTarget = false;
            UiBuilder.Stretch(rim.rectTransform);
            rim.rectTransform.offsetMin = new Vector2(-SpeechBubble.RimDp, -SpeechBubble.RimDp);
            rim.rectTransform.offsetMax = new Vector2(SpeechBubble.RimDp, SpeechBubble.RimDp);
            _tailPlate = UiBuilder.Image("Tail", _root, null, SpeechBubble.GlassColor);
            Image plate = UiBuilder.Sliced("Plate", _root, art.RoundedRect, SpeechBubble.GlassColor);
            plate.pixelsPerUnitMultiplier = 24f / 14f;
            plate.raycastTarget = false;
            UiBuilder.Stretch(plate.rectTransform);
            _label = UiBuilder.Label("Text", _root, TextStyle.PlainBold, string.Empty, TextSp, SpeechBubble.InkColor, TextAlignmentOptions.Center);
            _label.textWrappingMode = TextWrappingModes.Normal;
            _label.enableAutoSizing = false;
            _label.raycastTarget = false;
            UiBuilder.Stretch(_label.rectTransform);
            _label.rectTransform.offsetMin = new Vector2(SpeechBubble.PadXDp, SpeechBubble.PadYDp);
            _label.rectTransform.offsetMax = new Vector2(-SpeechBubble.PadXDp, -SpeechBubble.PadYDp);
            _tailShadow.raycastTarget = false;
            _tailRim.raycastTarget = false;
            _tailPlate.raycastTarget = false;
            _root.gameObject.SetActive(false);
        }

        /// <summary>El tooltip (el rectángulo del vidrio, sin el pico).</summary>
        public RectTransform Rect => _root;

        /// <summary>Está a la vista (también mientras se funde al entrar o al salir).</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Se está yendo (fundido de salida).</summary>
        public bool Leaving => Visible && _target <= 0f;

        /// <summary>Lo que dice ("" oculto).</summary>
        public string Text => Visible ? _label.text : string.Empty;

        /// <summary>Opacidad (0 oculto).</summary>
        public float Alpha => Visible ? _group.alpha : 0f;

        /// <summary>Punta del pico en dp del padre (arriba a la izquierda, y hacia abajo), tras colocarlo.</summary>
        public Vector2 TailTip { get; private set; }

        /// <summary>El pico apunta hacia arriba (<see cref="PlaceBelow"/>).</summary>
        public bool TailUp { get; private set; }

        /// <summary>
        /// Enseña <paramref name="text"/> y empieza el fundido de entrada (<see cref="FadeMs"/>; con <paramref name="instant"/>,
        /// Reducir movimiento, sin fundido). Colócalo después con <see cref="PlaceAbove"/> o <see cref="PlaceBelow"/>.
        /// </summary>
        public void Show(string text, bool instant)
        {
            _label.text = text;
            _label.fontSize = TextSp * UiBuilder.TextScale;
            _fitLimit = float.NaN; // texto nuevo: se vuelve a medir al colocarlo
            _target = 1f;
            _alpha = instant ? 1f : 0f;
            _group.alpha = _alpha;
            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
        }

        /// <summary>Empieza el fundido de salida (con <paramref name="instant"/>, se oculta ya).</summary>
        public void Leave(bool instant)
        {
            _target = 0f;
            if (instant)
            {
                Hide();
            }
        }

        /// <summary>Oculta el tooltip en el acto.</summary>
        public void Hide()
        {
            _target = 0f;
            _alpha = 0f;
            _group.alpha = 0f;
            _root.gameObject.SetActive(false);
        }

        /// <summary>Avanza el fundido. Sin asignaciones.</summary>
        public void Tick(float dtMs)
        {
            if (!Visible)
            {
                return;
            }

            float step = dtMs / FadeMs;
            _alpha = _target > _alpha ? Mathf.Min(_target, _alpha + step) : Mathf.Max(_target, _alpha - step);
            _group.alpha = _alpha;
            if (_target <= 0f && _alpha <= 0f)
            {
                _root.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Coloca el tooltip encima de (<paramref name="x"/>, <paramref name="y"/>) (dp del padre, arriba a la izquierda), con el
        /// pico hacia abajo y su punta en ese punto, ajustado al texto, como mucho <paramref name="maxWidth"/> dp de ancho y entre
        /// <paramref name="minX"/> y <paramref name="maxX"/>; si arriba no cabe, se baja lo justo para no pasar de
        /// <paramref name="minY"/>.
        /// </summary>
        public void PlaceAbove(float x, float y, float minX, float maxX, float minY, float maxWidth)
        {
            Measure(minX, maxX, maxWidth, out float width, out float height);
            float left = Left(x, width, minX, maxX);
            float top = Mathf.Max(minY + SpeechBubble.RimDp, y - SpeechBubble.TailOutDp - height);
            UiBuilder.Place(_root, left, top, width, height);
            float tailX = TailX(x - left, width);
            TailUp = false;
            TailTip = new Vector2(left + tailX, top + height + SpeechBubble.TailOutDp);
            PlaceTails(tailX, height);
        }

        /// <summary>
        /// Como <see cref="PlaceAbove"/>, pero debajo de (<paramref name="x"/>, <paramref name="y"/>), con el pico hacia arriba; si
        /// abajo no cabe, se sube lo justo para no pasar de <paramref name="maxY"/>.
        /// </summary>
        public void PlaceBelow(float x, float y, float minX, float maxX, float maxY, float maxWidth)
        {
            Measure(minX, maxX, maxWidth, out float width, out float height);
            float left = Left(x, width, minX, maxX);
            float top = Mathf.Min(y + SpeechBubble.TailOutDp, maxY - SpeechBubble.RimDp - height);
            UiBuilder.Place(_root, left, top, width, height);
            float tailX = TailX(x - left, width);
            TailUp = true;
            TailTip = new Vector2(left + tailX, top - SpeechBubble.TailOutDp);
            PlaceTails(tailX, 0f);
        }

        private void Measure(float minX, float maxX, float maxWidth, out float width, out float height)
        {
            float room = Mathf.Max(0f, maxX - minX - (2f * SpeechBubble.RimDp));
            float limit = Mathf.Max((2f * SpeechBubble.PadXDp) + 24f, Mathf.Min(maxWidth, room));
            if (limit != _fitLimit)
            {
                FitText(_label, limit - (2f * SpeechBubble.PadXDp), out float textWidth, out float textHeight);
                _fitWidth = Mathf.Ceil(textWidth + (2f * SpeechBubble.PadXDp));
                _fitHeight = Mathf.Ceil(textHeight + (2f * SpeechBubble.PadYDp));
                _fitLimit = limit;
            }

            width = _fitWidth;
            height = _fitHeight;
        }

        private static float Left(float x, float width, float minX, float maxX)
        {
            float lo = minX + SpeechBubble.RimDp;
            return Mathf.Clamp(x - (width * 0.5f), lo, Mathf.Max(lo, maxX - SpeechBubble.RimDp - width));
        }

        private static float TailX(float local, float width)
        {
            float corner = SpeechBubble.TailCornerDp;
            return Mathf.Clamp(local, Mathf.Min(corner, width * 0.5f), Mathf.Max(width - corner, width * 0.5f));
        }

        // El pico en el canto de abajo (yDown = alto) o de arriba (yDown = 0) del vidrio, con su borde y su sombra.
        private void PlaceTails(float tailX, float yDown)
        {
            float shadowY = yDown + (TailUp ? -SpeechBubble.TailShadowDp : SpeechBubble.TailShadowDp);
            PlaceTail(_tailShadow.rectTransform, tailX, shadowY, SpeechBubble.TailDp + (2f * SpeechBubble.RimDp) + (2f * SpeechBubble.TailShadowDp));
            PlaceTail(_tailRim.rectTransform, tailX, yDown, SpeechBubble.TailDp + (2f * SpeechBubble.RimDp));
            PlaceTail(_tailPlate.rectTransform, tailX, yDown, SpeechBubble.TailDp);
        }

        private static void PlaceTail(RectTransform rt, float x, float yDown, float size)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(x, -yDown);
            rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        // El ancho más estrecho (hasta inner) con el que el texto sigue en las mismas líneas, y su alto (como SpeechBubble).
        private static void FitText(TextMeshProUGUI label, float inner, out float textWidth, out float textHeight)
        {
            string text = label.text;
            float single = label.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x;
            if (single <= inner)
            {
                textWidth = Mathf.Min(inner, single + FitSlackDp);
                textHeight = label.GetPreferredValues(text, textWidth, float.PositiveInfinity).y;
                return;
            }

            textHeight = label.GetPreferredValues(text, inner, float.PositiveInfinity).y;
            float lo = Mathf.Max(1f, Mathf.Min(inner, single * 0.5f));
            float hi = inner;
            for (int i = 0; i < 12 && hi - lo > 0.5f; i++)
            {
                float mid = 0.5f * (lo + hi);
                if (label.GetPreferredValues(text, mid, float.PositiveInfinity).y <= textHeight + 0.5f)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid;
                }
            }

            textWidth = Mathf.Min(inner, hi + FitSlackDp);
        }
    }
}
