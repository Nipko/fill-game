#nullable enable
using System;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El momento del marco de país en la libreta (Q-M6 de D-033, añadido del 2026-11-08) [P]: tras sellar la casilla 30, bajo lo
    /// recibido, el retrato del jugador con el marco recién ganado (que se pone solo), «Marco de {mundo}: ya lo lleva tu avatar» y el
    /// enlace «Ver tu avatar», que cierra la libreta y abre «Tu avatar». No es una pantalla nueva ni un takeover: va dentro de la
    /// libreta, que ya estaba abierta, y «Cerrar» sigue siendo el botón principal.
    /// </summary>
    public sealed partial class LogbookScreen
    {
        /// <summary>Lado del retrato del momento (dp).</summary>
        public const float FrameRevealPortraitDp = 64f;

        private RectTransform _reveal = null!;
        private Image _revealPortrait = null!;
        private TextMeshProUGUI _revealText = null!;
        private Button _revealView = null!;
        private TextMeshProUGUI _revealViewLabel = null!;

        /// <summary>El icono del marco <c>frame_wNN</c> en las filas de premio (lo pone <c>GameBootstrap</c>); null: el aro de siempre.</summary>
        public Func<string, Sprite?>? FrameIconFor { get; set; }

        /// <summary>Toque en «Ver tu avatar» (lo pone <c>GameBootstrap</c>).</summary>
        public Action? FrameViewPressed { get; set; }

        /// <summary>Se ve el momento del marco.</summary>
        public bool FrameRevealShown => _reveal.gameObject.activeSelf;

        /// <summary>El texto del momento («Marco de Estuario: ya lo lleva tu avatar»).</summary>
        public string FrameRevealText => FrameRevealShown ? _revealText.text : string.Empty;

        /// <summary>El retrato del momento.</summary>
        public Sprite? FrameRevealPortrait => FrameRevealShown ? _revealPortrait.sprite : null;

        /// <summary>El enlace «Ver tu avatar» (null si no se ve).</summary>
        public Button? FrameViewButton => FrameRevealShown && _revealView.gameObject.activeSelf ? _revealView : null;

        /// <summary>El recuadro del momento (tests de composición).</summary>
        internal RectTransform FrameRevealRect => _reveal;

        /// <summary>
        /// Enseña el momento con <paramref name="portrait"/> (el avatar con el marco) y <paramref name="text"/>; con
        /// <paramref name="canView"/>, el enlace «Ver tu avatar». Se llama tras <see cref="ShowClaimed"/>.
        /// </summary>
        public void ShowFrameReveal(Sprite portrait, string text, bool canView)
        {
            if (portrait == null)
            {
                throw new ArgumentNullException(nameof(portrait));
            }

            _revealPortrait.sprite = portrait;
            _revealText.text = text ?? string.Empty;
            _revealViewLabel.text = LogbookText.Plain(LogbookText.Keys.FrameView);
            _revealView.gameObject.SetActive(canView);
            _reveal.gameObject.SetActive(true);
            Layout();
        }

        private void BuildFrameReveal()
        {
            _reveal = UiBuilder.Rect("FrameReveal", _panel);
            _revealPortrait = UiBuilder.Image("Portrait", _reveal, null, Color.white);
            _revealPortrait.preserveAspect = true;
            _revealText = UiBuilder.Label("Text", _reveal, TextStyle.PlainBold, string.Empty, 15f, Palette.UiInk, TextAlignmentOptions.MidlineLeft);
            _revealText.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_revealText, UiBuilder.MinTextSp, 15f);
            _revealView = ModalKit.Link("FrameView", _reveal, string.Empty, () => FrameViewPressed?.Invoke(), out _revealViewLabel);
            _reveal.gameObject.SetActive(false);
        }

        private void HideFrameReveal()
        {
            if (_reveal != null)
            {
                _reveal.gameObject.SetActive(false);
                _revealPortrait.sprite = null;
            }
        }

        // Bajo lo recibido: el retrato a la izquierda y, a su lado, el texto y el enlace. Devuelve el nuevo y.
        private float LayoutFrameReveal(float y, float inner)
        {
            if (!_reveal.gameObject.activeSelf)
            {
                return y;
            }

            float s = Mathf.Max(1f, UiBuilder.TextScale);
            float textX = FrameRevealPortraitDp + 12f;
            float textW = inner - textX;
            float textH = Mathf.Ceil(Mathf.Max(22f * s, UiBuilder.MinTextHeight(_revealText, textW)));
            float linkH = _revealView.gameObject.activeSelf ? 48f : 0f;
            float blockH = Mathf.Max(FrameRevealPortraitDp, textH + linkH);
            UiBuilder.Place(_reveal, 16f, y, inner, blockH);
            UiBuilder.Place(_revealPortrait.rectTransform, 0f, (blockH - FrameRevealPortraitDp) * 0.5f, FrameRevealPortraitDp, FrameRevealPortraitDp);
            float ty = (blockH - textH - linkH) * 0.5f;
            UiBuilder.Place(_revealText.rectTransform, textX, ty, textW, textH);
            if (linkH > 0f)
            {
                UiBuilder.Place((RectTransform)_revealView.transform, textX, ty + textH, textW, linkH);
                _revealViewLabel.alignment = TextAlignmentOptions.MidlineLeft;
            }

            return y + blockH + 8f;
        }
    }
}
