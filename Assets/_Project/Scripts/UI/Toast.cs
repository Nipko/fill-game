#nullable enable
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Aviso temporal sobre el tablero, sin toques (V7): el cartel «El río bajó» de 2,0 s (04 §2.7; 07 §5.5: «banner de
    /// papel»), el regalo de Capi en el nivel 8 (04 §2.2) y «¡Una cuia más! Sigue desde aquí» (04 §6.5 <c>rescue_applied</c>).
    /// Placa de madera clara con el texto en madera_base y una entrada de 150 ms.
    /// </summary>
    internal sealed class Toast
    {
        private const float FadeMs = 150f;

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly TextMeshProUGUI _label;
        private float _leftMs = -1f;
        private float _totalMs;

        public Toast(string name, Transform parent, ProceduralArt art, float size)
        {
            _root = UiBuilder.Rect(name, parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            Image plate = UiBuilder.Sliced("Plate", _root, art.RoundedRect, Palette.PanelLight);
            plate.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(plate.rectTransform);
            Image edge = UiBuilder.Sliced("Edge", _root, art.RoundedRect, Palette.FamilyWood.Base);
            edge.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(edge.rectTransform);
            edge.rectTransform.offsetMin = new Vector2(0f, -3f);
            edge.rectTransform.offsetMax = new Vector2(0f, -3f);
            edge.transform.SetAsFirstSibling();
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

        /// <summary>Coloca el aviso centrado con su borde superior en <paramref name="top"/>.</summary>
        public void Place(float canvasW, float top)
        {
            float w = Mathf.Min(320f, canvasW - 32f);
            UiBuilder.Place(_root, (canvasW - w) * 0.5f, top, w, 56f);
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
    }
}
