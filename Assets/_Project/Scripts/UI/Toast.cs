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
    /// vertido (D-006: el origen que vierte, delante de todo).
    /// </summary>
    internal sealed class Toast
    {
        private const float FadeMs = 150f;

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _plate;
        private readonly Image _edge;
        private readonly TextMeshProUGUI _label;
        private float _leftMs = -1f;
        private float _totalMs;

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

        /// <summary>Añade los gráficos del aviso (tests de raycast).</summary>
        public void CollectGraphics(List<Graphic> graphics)
        {
            graphics.Add(_plate);
            graphics.Add(_edge);
            graphics.Add(_label);
        }
    }
}
