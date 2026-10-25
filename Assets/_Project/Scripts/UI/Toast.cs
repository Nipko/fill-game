#nullable enable
using System.Collections.Generic;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Aviso temporal sobre el tablero, sin toques (V7: <see cref="CanvasGroup.blocksRaycasts"/> = false y ningún gráfico es
    /// <c>raycastTarget</c>). La pantalla de nivel tiene dos: el cartel (<c>_banner</c>), con «El río bajó» de 2,0 s (04 §2.7;
    /// 07 §5.5: «banner de papel») y el rótulo «Nivel {n} · intento {k}», cada uno con su propia duración; y el aviso
    /// (<c>_toast</c>): el regalo de Capi en el nivel 8 (04 §2.2), «¡Una cuia más! Sigue desde aquí» (04 §6.5
    /// <c>rescue_applied</c>), «+1 Luciérnaga» y «Te faltan {n} semillas», con la duración por lectura de
    /// <see cref="AF.GameFlow.NoticeTiming"/> y de uno en uno con la tarjeta del miquito (la cola de avisos de GameScreen; D-011,
    /// punto 2). Placa de madera clara con el texto en madera_base y una entrada de 150 ms. Al salir se pone el último de su
    /// padre: el cartel, encima de todo; el aviso, dentro de la capa de avisos de GameScreen, que va detrás de la capa de
    /// vertido (D-006: el origen que vierte, delante de todo). Mide <see cref="DefaultHeightDp"/> de alto y
    /// <see cref="FullWidthDp"/> de ancho; el aviso puede encogerse hasta su texto, a lo alto (<see cref="MinHeightDp"/>) y, si
    /// hace falta, a lo ancho (<see cref="FitWidthDp"/>), para no tapar las burbujas «Te faltan {n}» de la bandeja, la repisa
    /// de abajo ni sus fichas (GameScreen.Notices.cs, T52) [P]. Con la escala de texto (<see cref="UiBuilder.TextScale"/>;
    /// 02 §12) el texto crece y la placa con él (<see cref="PreferredHeightDp"/>); el texto pasa a otra línea si no cabe y se
    /// encoge lo justo, nunca por debajo de 12 sp (pregunta 36 (a), D-020; antes, hasta 4 sp menos con un suelo de 11): si ni
    /// así cabe, la placa crece.
    /// </summary>
    internal sealed class Toast
    {
        /// <summary>Alto de la placa (dp) cuando cabe: el de siempre.</summary>
        public const float DefaultHeightDp = 56f;

        /// <summary>El canto de madera asoma este alto bajo la placa (dp): el aviso se ve hasta su borde de abajo + esto.</summary>
        public const float EdgeDp = 3f;

        /// <summary>Aire del texto a cada lado dentro de la placa (dp).</summary>
        public const float SidePadDp = 14f;

        private const float FadeMs = 150f;

        // Aire del texto a lo alto dentro de la placa (dp): el de siempre y el mínimo con la placa encogida a su texto [P].
        private const float PadDp = 6f;
        private const float MinPadDp = 4f;

        // Holgura del ancho ajustado al texto (dp): la de redondeo de las medidas de TextMeshPro, para que no salte a dos líneas.
        private const float FitSlackDp = 2f;

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _plate;
        private readonly Image _edge;
        private readonly TextMeshProUGUI _label;
        private readonly float _scale;
        private float _leftMs = -1f;
        private float _totalMs;
        private float _top = float.NaN;
        private float _left;
        private float _height = DefaultHeightDp;
        private float _width;
        private string? _measuredText;
        private float _measuredWidth = -1f;
        private float _textHeight;
        private string? _lineText;
        private float _lineWidth;
        private string? _preferredText;
        private float _preferredWidth = -1f;
        private float _preferredHeight;
        private bool _fitPad; // colocada con PlaceAt: el aire de arriba y de abajo se ajusta al texto que lleva

        public Toast(string name, Transform parent, ProceduralArt art, float size)
        {
            _scale = UiBuilder.TextScale;
            _root = UiBuilder.Rect(name, parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _plate = UiBuilder.Sliced("Plate", _root, art.RoundedRect, Palette.PanelLight);
            _plate.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(_plate.rectTransform);
            _edge = UiBuilder.Sliced("Edge", _root, art.RoundedRect, Palette.FamilyWood.Base);
            _edge.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(_edge.rectTransform);
            _edge.rectTransform.offsetMin = new Vector2(0f, -3f);
            _edge.rectTransform.offsetMax = new Vector2(0f, -3f);
            _edge.transform.SetAsFirstSibling();
            _label = UiBuilder.Label("Text", _root, TextStyle.PlainBold, string.Empty, size, Palette.FamilyWood.Base, TextAlignmentOptions.Center);
            _label.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_label, UiBuilder.MinTextSp, size);
            UiBuilder.Stretch(_label.rectTransform);
            _label.rectTransform.offsetMin = new Vector2(14f, 6f);
            _label.rectTransform.offsetMax = new Vector2(-14f, -6f);
            _root.gameObject.SetActive(false);
        }

        public bool Visible => _root.gameObject.activeSelf;

        public string Text => _label.text;

        /// <summary>Duración del aviso a la vista, con sus fundidos (ms; tests).</summary>
        public float DurationMs => _totalMs;

        /// <summary>Ms que le quedan a la vista (−1 si no se ve).</summary>
        public float LeftMs => _leftMs;

        /// <summary>El <see cref="CanvasGroup"/> del aviso bloquea los toques (siempre false: los deja pasar; tests).</summary>
        public bool BlocksRaycasts => _group.blocksRaycasts;

        /// <summary>
        /// Muestra <paramref name="text"/> durante <paramref name="ms"/> milisegundos. Si la placa se colocó con
        /// <see cref="PlaceAt"/>, su aire de arriba y de abajo se vuelve a ajustar a este texto (revisión de T52: dependía del
        /// texto que se mostró antes).
        /// </summary>
        public void Show(string text, float ms)
        {
            _label.text = text;
            _leftMs = ms;
            _totalMs = Mathf.Max(1f, ms);
            _group.alpha = 0f;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            if (_fitPad)
            {
                FitPad();
            }
        }

        /// <summary>Borde de arriba de la placa (dp desde arriba del lienzo; NaN antes de colocarla).</summary>
        public float Top => _top;

        /// <summary>Alto de la placa (dp), sin el canto que asoma debajo (<see cref="EdgeDp"/>).</summary>
        public float Height => _height;

        /// <summary>Borde izquierdo de la placa (dp desde la izquierda del lienzo).</summary>
        public float Left => _left;

        /// <summary>Ancho de la placa (dp).</summary>
        public float Width => _width;

        /// <summary>Rectángulo del aviso tal como se ve, con el canto de abajo (dp desde arriba del lienzo; tests de solapes).</summary>
        public Rect SeenRect => new Rect(_left, _top, _width, _height + EdgeDp);

        /// <summary>Ancho de siempre de la placa en un lienzo de <paramref name="canvasW"/> dp, centrada.</summary>
        public static float FullWidthDp(float canvasW)
        {
            return Mathf.Min(320f, canvasW - 32f);
        }

        /// <summary>Texto del aviso (tests de desbordes).</summary>
        public TextMeshProUGUI Label => _label;

        /// <summary>Raíz del aviso: la placa (tests de solapes).</summary>
        public RectTransform Root => _root;

        /// <summary>
        /// Coloca el aviso centrado con su borde superior en <paramref name="top"/>, su alto de siempre (por la escala de texto) y
        /// el aire de siempre (6 dp arriba y abajo, sea cual sea el texto: el cartel «El río bajó» y el rótulo del intento, que
        /// se colocan antes de mostrar su texto; revisión de T52).
        /// </summary>
        public void Place(float canvasW, float top)
        {
            float w = FullWidthDp(canvasW);
            SetRect((canvasW - w) * 0.5f, top, w, DefaultHeightDp * _scale);
            _fitPad = false;
            SetPad(PadDp);
        }

        /// <summary>
        /// Coloca la placa con su esquina de arriba a la izquierda en (<paramref name="left"/>, <paramref name="top"/>) y
        /// <paramref name="width"/> × <paramref name="height"/> dp (GameScreen.Notices.cs: la placa ajustada a su texto junto a
        /// las fichas de la repisa, o la que las tapa enteras). El texto conserva su tamaño; el aire de arriba y de abajo se
        /// reparte, hasta 6 dp, con el texto de ahora y con el de cada <see cref="Show"/> siguiente.
        /// </summary>
        public void PlaceAt(float left, float top, float width, float height)
        {
            SetRect(left, top, width, height);
            _fitPad = true;
            FitPad();
        }

        private void SetRect(float left, float top, float width, float height)
        {
            _top = top;
            _left = left;
            _height = height;
            _width = width;
            UiBuilder.Place(_root, left, top, width, height);
        }

        // El aire de arriba y de abajo con el texto de ahora: lo que sobra de la placa, hasta 6 dp; si no sobra nada (la placa
        // en el hueco que le deja la última fila, con la escala de texto: GameScreen.Notices.cs), sin aire, y el texto se encoge,
        // nunca por debajo de 12 sp (A1; antes, al menos 1 dp, y el texto se encogía antes).
        private void FitPad()
        {
            SetPad(Mathf.Clamp((_height - TextHeight(_width)) * 0.5f, 0f, PadDp));
        }

        private void SetPad(float pad)
        {
            _label.rectTransform.offsetMin = new Vector2(SidePadDp, pad);
            _label.rectTransform.offsetMax = new Vector2(-SidePadDp, -pad);
        }

        /// <summary>Aire del texto arriba y abajo dentro de la placa (dp; tests).</summary>
        public float TextPadDp => _label.rectTransform.offsetMin.y;

        /// <summary>
        /// Alto de la placa con el texto de ahora, de su ancho de siempre en un lienzo de <paramref name="canvasW"/> dp: el de
        /// siempre (<see cref="DefaultHeightDp"/>) por la escala de texto o, si el texto no cabe en él ni a su tamaño mínimo
        /// (12 sp), el que necesita a ese mínimo con <see cref="PadDp"/> arriba y abajo (02 §12: la caja crece; pregunta 36
        /// (a)). A la escala 1, con los textos del juego, el de siempre. Mide solo si cambia el texto o el ancho (sin
        /// asignaciones por fotograma).
        /// </summary>
        public float PreferredHeightDp(float canvasW)
        {
            float w = FullWidthDp(canvasW);
            string text = _label.text;
            if (!ReferenceEquals(text, _preferredText) || !Mathf.Approximately(w, _preferredWidth))
            {
                _preferredText = text;
                _preferredWidth = w;
                _preferredHeight = Mathf.Ceil(UiBuilder.MinTextHeight(_label, w - (2f * SidePadDp))) + (2f * PadDp);
            }

            return Mathf.Max(DefaultHeightDp * _scale, _preferredHeight);
        }

        /// <summary>
        /// Lo menos que puede medir la placa, de su ancho de siempre, con el texto de ahora encogido hasta su mínimo (12 sp, 02 §12)
        /// y <see cref="MinPadDp"/> arriba y abajo: por debajo, el texto ya no cabe legible (D-046; GameScreen.PlaceToastInTroopBand).
        /// Mide solo si cambia el texto o el ancho (con <see cref="PreferredHeightDp"/>): sin asignaciones por fotograma.
        /// </summary>
        public float LegibleHeightDp(float canvasW)
        {
            PreferredHeightDp(canvasW); // mide (y guarda) el texto a su mínimo solo si cambia el texto o el ancho
            return _preferredHeight - (2f * PadDp) + (2f * MinPadDp);
        }

        /// <summary>
        /// Alto mínimo de la placa para el texto de ahora a su tamaño de siempre, en un lienzo de <paramref name="canvasW"/> dp:
        /// sus líneas y <see cref="MinPadDp"/> arriba y abajo, y como mucho <see cref="PreferredHeightDp"/>. Mide solo si cambia
        /// el texto o el ancho.
        /// </summary>
        public float MinHeightDp(float canvasW)
        {
            return Mathf.Min(PreferredHeightDp(canvasW), Mathf.Ceil(TextHeight(FullWidthDp(canvasW))) + (2f * MinPadDp));
        }

        /// <summary>
        /// Ancho de la placa ajustada al texto de ahora en una sola línea, a su tamaño de siempre: el texto, una holgura de
        /// redondeo y <see cref="SidePadDp"/> a cada lado (T52 [P]; a 15 sp, 186 dp para «Te faltan 120 semillas» y 253 para
        /// «¡Una cuia más! Sigue desde aquí»); como mucho el de siempre (<see cref="FullWidthDp"/>), que es también lo que da si
        /// el texto no cabe en una línea. Mide solo si cambia el texto.
        /// </summary>
        public float FitWidthDp(float canvasW)
        {
            string text = _label.text;
            if (!ReferenceEquals(text, _lineText))
            {
                _lineText = text;
                _lineWidth = string.IsNullOrEmpty(text) ? 0f : _label.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x;
            }

            return Mathf.Min(FullWidthDp(canvasW), Mathf.Ceil(_lineWidth + FitSlackDp) + (2f * SidePadDp));
        }

        // Alto del texto de ahora en una placa de w dp de ancho, al tamaño máximo del autoajuste (lo que mide TMP con él).
        private float TextHeight(float w)
        {
            string text = _label.text;
            if (!ReferenceEquals(text, _measuredText) || !Mathf.Approximately(w, _measuredWidth))
            {
                _measuredText = text;
                _measuredWidth = w;
                _textHeight = string.IsNullOrEmpty(text) ? 0f : _label.GetPreferredValues(text, w - (2f * SidePadDp), float.PositiveInfinity).y;
            }

            return _textHeight;
        }

        public void Hide()
        {
            _leftMs = -1f;
            _root.gameObject.SetActive(false);
        }

        public void Tick(float dtMs)
        {
            if (_leftMs < 0f)
            {
                return;
            }

            _leftMs -= dtMs;
            float shown = _totalMs - _leftMs;
            _group.alpha = Mathf.Clamp01(Mathf.Min(shown, _leftMs) / FadeMs);
            if (_leftMs <= 0f)
            {
                Hide();
            }
        }

        /// <summary>Añade los gráficos del aviso (tests de raycast).</summary>
        public void CollectGraphics(List<Graphic> graphics)
        {
            graphics.Add(_plate);
            graphics.Add(_edge);
            graphics.Add(_label);
        }
    }
}
