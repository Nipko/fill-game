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
    /// vertido (D-006: el origen que vierte, delante de todo). Mide <see cref="DefaultHeightDp"/> de alto; el aviso puede
    /// encogerse hasta su texto (<see cref="MinHeightDp"/>) para no tapar las burbujas «Te faltan {n}» ni la repisa de abajo
    /// (GameScreen.Notices.cs, T52) [P].
    /// </summary>
    internal sealed class Toast
    {
        /// <summary>Alto de la placa (dp) cuando cabe: el de siempre.</summary>
        public const float DefaultHeightDp = 56f;

        /// <summary>El canto de madera asoma este alto bajo la placa (dp): el aviso se ve hasta su borde de abajo + esto.</summary>
        public const float EdgeDp = 3f;

        private const float FadeMs = 150f;

        // Aire del texto a lo alto dentro de la placa (dp): el de siempre y el mínimo con la placa encogida a su texto [P].
        private const float PadDp = 6f;
        private const float MinPadDp = 4f;
        private const float SidePadDp = 14f;

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _plate;
        private readonly Image _edge;
        private readonly TextMeshProUGUI _label;
        private float _leftMs = -1f;
        private float _totalMs;
        private float _top = float.NaN;
        private float _height = DefaultHeightDp;
        private float _width;
        private string? _measuredText;
        private float _measuredWidth = -1f;
        private float _textHeight;

        public Toast(string name, Transform parent, ProceduralArt art, float size)
        {
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
            UiBuilder.AutoSize(_label, Mathf.Max(11f, size - 4f), size);
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

        /// <summary>Muestra <paramref name="text"/> durante <paramref name="ms"/> milisegundos.</summary>
        public void Show(string text, float ms)
        {
            _label.text = text;
            _leftMs = ms;
            _totalMs = Mathf.Max(1f, ms);
            _group.alpha = 0f;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
        }

        /// <summary>Borde de arriba de la placa (dp desde arriba del lienzo; NaN antes de colocarla).</summary>
        public float Top => _top;

        /// <summary>Alto de la placa (dp), sin el canto que asoma debajo (<see cref="EdgeDp"/>).</summary>
        public float Height => _height;

        /// <summary>Rectángulo del aviso tal como se ve, con el canto de abajo (dp desde arriba del lienzo; tests de solapes).</summary>
        public Rect SeenRect => new Rect(_root.anchoredPosition.x, _top, _width, _height + EdgeDp);

        /// <summary>Texto del aviso (tests de desbordes).</summary>
        public TextMeshProUGUI Label => _label;

        /// <summary>Raíz del aviso: la placa (tests de solapes).</summary>
        public RectTransform Root => _root;

        /// <summary>Coloca el aviso centrado con su borde superior en <paramref name="top"/> y su alto de siempre.</summary>
        public void Place(float canvasW, float top)
        {
            Place(canvasW, top, DefaultHeightDp);
        }

        /// <summary>
        /// Coloca el aviso centrado con su borde superior en <paramref name="top"/> y <paramref name="height"/> dp de alto (como
        /// mucho <see cref="DefaultHeightDp"/>; con menos, el texto conserva su tamaño y el aire de arriba y de abajo se reduce).
        /// </summary>
        public void Place(float canvasW, float top, float height)
        {
            float w = Mathf.Min(320f, canvasW - 32f);
            float h = Mathf.Min(DefaultHeightDp, height);
            _top = top;
            _height = h;
            _width = w;
            UiBuilder.Place(_root, (canvasW - w) * 0.5f, top, w, h);
            float pad = Mathf.Clamp((h - TextHeight(w)) * 0.5f, 1f, PadDp);
            _label.rectTransform.offsetMin = new Vector2(SidePadDp, pad);
            _label.rectTransform.offsetMax = new Vector2(-SidePadDp, -pad);
        }

        /// <summary>
        /// Alto mínimo de la placa para el texto de ahora a su tamaño de siempre, en un lienzo de <paramref name="canvasW"/> dp:
        /// sus líneas y <see cref="MinPadDp"/> arriba y abajo. Mide solo si cambia el texto o el ancho.
        /// </summary>
        public float MinHeightDp(float canvasW)
        {
            return Mathf.Min(DefaultHeightDp, Mathf.Ceil(TextHeight(Mathf.Min(320f, canvasW - 32f))) + (2f * MinPadDp));
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
