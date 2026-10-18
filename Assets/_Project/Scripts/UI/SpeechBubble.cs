#nullable enable
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Bocadillo de TiTino suelto, con el pico hacia él (tarea M4.2: el Mapa del 10, 06 §2.3; D-013, punto 6: «su bocadillo (≤ 50
    /// caracteres) queda para sus frases propias»). Es el mismo bocadillo que el del inicio (<see cref="HomeSloth"/>) y el de la
    /// victoria: vidrio esmerilado casi opaco con borde de madera clara (D-003), texto en Nunito Bold de
    /// <see cref="TextSp"/> sp (02 §6) con la escala de texto (02 §12) y un pico (un cuadrado girado 45°, medio fuera) con su
    /// sombra de 1 dp. Se ajusta al texto (el ancho más estrecho con las mismas líneas, como mucho <c>maxWidth</c>) y crece hacia
    /// arriba desde el pico: <see cref="PlaceAbove(float, float, float, float, float, float)"/> lo deja encima de un punto, con el
    /// pico apuntándolo, sin salirse de los márgenes que se le den; si lo que habla queda más abajo, el pico sigue con un tallo
    /// del color del borde hasta él (revisión de M4.2: TiTino cuelga bajo la copa de su embaúba). No recibe toques. Gráfico
    /// provisional por código; nada de lo que hace por fotograma asigna memoria (<see cref="SetAlpha"/>): el texto y las medidas,
    /// solo al enseñarlo o al recolocarlo.
    /// </summary>
    internal sealed class SpeechBubble
    {
        /// <summary>Texto del bocadillo (sp; 02 §6: 14 sp, el de la victoria), con la escala de texto de 02 §12.</summary>
        public const float TextSp = HomeSloth.BubbleTextSp;

        /// <summary>Aire del texto a cada lado (dp), el del bocadillo del inicio.</summary>
        public const float PadXDp = HomeSloth.BubblePadXDp;

        /// <summary>Aire del texto arriba y abajo (dp), el del bocadillo del inicio.</summary>
        public const float PadYDp = HomeSloth.BubblePadYDp;

        /// <summary>Grosor del borde de madera clara (dp), el del bocadillo del inicio.</summary>
        public const float RimDp = HomeSloth.BubbleRimDp;

        /// <summary>Lado del pico, sin el borde (dp) [P]: el del inicio.</summary>
        public const float TailDp = 12f;

        /// <summary>Lo que el pico (con su borde) sale del bocadillo (dp): media diagonal del cuadrado girado.</summary>
        public const float TailOutDp = (TailDp + (2f * RimDp)) * 0.7072f;

        /// <summary>Sombra del pico (dp), la del inicio: 1 dp de #000 al 15 %.</summary>
        public const float TailShadowDp = HomeSloth.TailShadowDp;

        /// <summary>El pico no se acerca a menos de este tanto de una esquina del bocadillo (dp) [P]: el radio de sus esquinas.</summary>
        public const float TailCornerDp = 14f;

        /// <summary>Vidrio esmerilado casi opaco (D-003), el del bocadillo del inicio y de la victoria.</summary>
        public static readonly Color GlassColor = new Color(0xEA / 255f, 0xF7 / 255f, 0xF3 / 255f, 0.96f);

        /// <summary>Borde de madera clara (<see cref="Palette.FrameBevel"/>).</summary>
        public static readonly Color RimColor = Palette.FrameBevel;

        /// <summary>Tinta del texto.</summary>
        public static readonly Color InkColor = Palette.UiInk;

        private static readonly Color TailShadowColor = new Color(0f, 0f, 0f, 0.15f);

        // Holgura del ancho ajustado al texto (dp): la de redondeo de TextMeshPro, para que no salte de línea.
        private const float FitSlackDp = 2f;

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _stem;
        private readonly Image _tailShadow;
        private readonly Image _tailRim;
        private readonly Image _tailPlate;
        private readonly TextMeshProUGUI _label;

        /// <summary>Crea el bocadillo (oculto) bajo <paramref name="parent"/>, que se mide arriba a la izquierda en dp.</summary>
        public SpeechBubble(RectTransform parent, ProceduralArt art, string name)
        {
            _root = UiBuilder.Rect(name, parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _stem = UiBuilder.Image("Stem", _root, null, RimColor); // detrás del pico, que tapa su arranque
            _stem.gameObject.SetActive(false);
            _tailShadow = UiBuilder.Image("TailShadow", _root, null, TailShadowColor);
            _tailRim = UiBuilder.Image("TailRim", _root, null, RimColor);
            Image rim = UiBuilder.Sliced("Rim", _root, art.RoundedRect, RimColor);
            rim.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(rim.rectTransform);
            rim.rectTransform.offsetMin = new Vector2(-RimDp, -RimDp);
            rim.rectTransform.offsetMax = new Vector2(RimDp, RimDp);
            _tailPlate = UiBuilder.Image("Tail", _root, null, GlassColor);
            Image plate = UiBuilder.Sliced("Plate", _root, art.RoundedRect, GlassColor);
            plate.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(plate.rectTransform);
            _label = UiBuilder.Label("Text", _root, TextStyle.PlainBold, string.Empty, TextSp, InkColor, TextAlignmentOptions.Center);
            _label.textWrappingMode = TextWrappingModes.Normal;
            _label.enableAutoSizing = false;
            UiBuilder.Stretch(_label.rectTransform);
            _label.rectTransform.offsetMin = new Vector2(PadXDp, PadYDp);
            _label.rectTransform.offsetMax = new Vector2(-PadXDp, -PadYDp);
            _root.gameObject.SetActive(false);
        }

        /// <summary>El bocadillo (el rectángulo del vidrio, sin el pico).</summary>
        public RectTransform Rect => _root;

        /// <summary>El texto.</summary>
        public TextMeshProUGUI Label => _label;

        /// <summary>El pico (el vidrio, sin borde ni sombra).</summary>
        public RectTransform Tail => _tailPlate.rectTransform;

        /// <summary>El tallo del pico (activo solo si lo que habla queda más abajo que la punta del pico).</summary>
        public RectTransform Stem => _stem.rectTransform;

        /// <summary>El pico lleva tallo.</summary>
        public bool StemShown => _stem.gameObject.activeSelf;

        /// <summary>Está a la vista (aunque sea con opacidad 0 al empezar su fundido).</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Lo que dice ("" oculto).</summary>
        public string Text => Visible ? _label.text : string.Empty;

        /// <summary>Opacidad (0 oculto).</summary>
        public float Alpha => Visible ? _group.alpha : 0f;

        /// <summary>
        /// Punta del pico (o de su tallo, si lo lleva) en dp del padre (arriba a la izquierda, y hacia abajo), tras
        /// <see cref="PlaceAbove(float, float, float, float, float, float, float, float)"/>.
        /// </summary>
        public Vector2 TailTip { get; private set; }

        /// <summary>
        /// Enseña <paramref name="text"/> con opacidad <paramref name="alpha"/>; colócalo después con
        /// <see cref="PlaceAbove(float, float, float, float, float, float)"/>.
        /// </summary>
        public void Show(string text, float alpha)
        {
            _label.text = text;
            _label.fontSize = TextSp * UiBuilder.TextScale;
            SetAlpha(alpha);
            _root.gameObject.SetActive(true);
        }

        /// <summary>Oculta el bocadillo.</summary>
        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        /// <summary>Opacidad del bocadillo entero. Sin asignaciones.</summary>
        public void SetAlpha(float alpha)
        {
            _group.alpha = Mathf.Clamp01(alpha);
        }

        /// <summary>
        /// Coloca el bocadillo encima de (<paramref name="x"/>, <paramref name="y"/>) (dp del padre, arriba a la izquierda), con el
        /// pico hacia abajo y su punta en ese punto: ajustado al texto, como mucho <paramref name="maxWidth"/> dp de ancho, y entre
        /// <paramref name="minX"/> y <paramref name="maxX"/> en horizontal (el pico se corre por dentro, lejos de las esquinas). Si
        /// arriba no cabe (el borde de arriba quedaría por encima de <paramref name="minY"/>), se baja lo justo. Solo al enseñarlo o
        /// al recolocarlo (mide el texto).
        /// </summary>
        public void PlaceAbove(float x, float y, float minX, float maxX, float minY, float maxWidth)
        {
            PlaceAbove(x, y, minX, maxX, minY, maxWidth, y, 0f);
        }

        /// <summary>
        /// Como <see cref="PlaceAbove(float, float, float, float, float, float)"/>, con la punta del pico en <paramref name="y"/> y,
        /// si <paramref name="pointY"/> queda más abajo, un tallo de <paramref name="stemWidth"/> dp del color del borde que baja
        /// desde la punta, en vertical, hasta <paramref name="pointY"/> (encima de lo que haya detrás del bocadillo). Solo al
        /// enseñarlo o al recolocarlo.
        /// </summary>
        public void PlaceAbove(float x, float y, float minX, float maxX, float minY, float maxWidth, float pointY, float stemWidth)
        {
            float room = Mathf.Max(0f, maxX - minX - (2f * RimDp));
            float limit = Mathf.Max((2f * PadXDp) + 24f, Mathf.Min(maxWidth, room));
            FitText(_label, limit - (2f * PadXDp), out float textWidth, out float textHeight);
            float width = Mathf.Ceil(textWidth + (2f * PadXDp));
            float height = Mathf.Ceil(textHeight + (2f * PadYDp));
            float left = Mathf.Clamp(x - (width * 0.5f), minX + RimDp, Mathf.Max(minX + RimDp, maxX - RimDp - width));
            float bottom = y - TailOutDp;
            float top = Mathf.Max(minY + RimDp, bottom - height);
            UiBuilder.Place(_root, left, top, width, height);
            float tailX = Mathf.Clamp(x - left, Mathf.Min(TailCornerDp, width * 0.5f), Mathf.Max(width - TailCornerDp, width * 0.5f));
            TailTip = new Vector2(left + tailX, top + height + TailOutDp);
            float stem = pointY - TailTip.y;
            bool withStem = stem > 0.5f && stemWidth > 0f;
            if (_stem.gameObject.activeSelf != withStem)
            {
                _stem.gameObject.SetActive(withStem);
            }

            if (withStem)
            {
                // Arranca dentro del pico (que lo tapa) para que no quede hueco con la punta, y acaba en pointY.
                float from = height + (TailOutDp * 0.5f);
                RectTransform rt = _stem.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(stemWidth, height + TailOutDp + stem - from);
                rt.anchoredPosition = new Vector2(tailX, -from);
                rt.localRotation = Quaternion.identity;
                TailTip = new Vector2(TailTip.x, pointY);
            }

            PlaceTail(_tailShadow.rectTransform, tailX, height + TailShadowDp, TailDp + (2f * RimDp) + (2f * TailShadowDp));
            PlaceTail(_tailRim.rectTransform, tailX, height, TailDp + (2f * RimDp));
            PlaceTail(_tailPlate.rectTransform, tailX, height, TailDp);
        }

        // El pico: un cuadrado de size dp girado 45°, centrado en el canto de abajo del bocadillo (y = yDown desde arriba).
        private static void PlaceTail(RectTransform rt, float x, float yDown, float size)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(x, -yDown);
            rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        // El ancho más estrecho (hasta inner) con el que el texto sigue en las mismas líneas que a inner, y su alto: el bocadillo
        // no deja aire de más a la derecha de una frase corta ni parte una que cabe. Mide el texto: solo al colocar.
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
