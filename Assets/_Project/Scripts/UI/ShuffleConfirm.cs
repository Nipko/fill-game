#nullable enable
using System;
using System.Collections.Generic;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Hoja de confirmación de una línea de la Pororoca (bloque B2; 04 §2.4: «La ola remueve el río sin tocar las cuias llenas.
    /// 300 semillas. ¿Seguir?»; regla general de confirmación para gastos ≥ 200 semillas [P]): cabecera «Pororoca» con la X, el
    /// glifo de la ola, la línea con el precio y un único botón «Seguir» [P]; la X cancela sin coste (el velo solo bloquea el
    /// tablero). Con la misma
    /// composición que <see cref="QuitConfirm"/> (<see cref="ModalKit"/>). La abre y la cierra <c>GameScreen.Shuffle.cs</c>; no
    /// cobra nada por sí misma.
    /// </summary>
    internal sealed class ShuffleConfirm
    {
        /// <summary>Lado del glifo de la ola en la hoja (dp).</summary>
        public const float GlyphDp = 56f;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly ReliefParts _close;
        private readonly Image _glyph;
        private readonly TextMeshProUGUI _body;
        private readonly ReliefParts _confirm;

        public ShuffleConfirm(Transform parent, ProceduralArt art, Action onConfirm, Action onCancel)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            _root = UiBuilder.Rect("ShuffleConfirm", parent);
            ModalKit.Dim("Dim", _root, 0.55f); // captura los toques: el tablero no se toca con la hoja abierta
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            ModalKit.Header(_panel, art, ShuffleText.Name, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, onCancel);
            _glyph = UiBuilder.Image("Wave", _panel, art.IconWave, Color.white);
            _glyph.preserveAspect = true;
            _body = ModalKit.Line("Body", _panel, string.Empty, 17f, Palette.FamilyWood.Base);
            _body.textWrappingMode = TextWrappingModes.Normal;
            _confirm = ModalKit.Cta("ConfirmButton", _panel, art.CtaWater, TextStyle.CtaWater, ShuffleText.ConfirmYes, 21f, onConfirm, out _);
            _root.gameObject.SetActive(false);
        }

        /// <summary>La hoja está abierta.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Raíz de la hoja (tests de orden de capas).</summary>
        public RectTransform Root => _root;

        /// <summary>Precio que enseña la línea (0 si está cerrada).</summary>
        public int Price { get; private set; }

        /// <summary>Texto de la línea.</summary>
        public string BodyText => _body.text;

        public Button ConfirmButton => _confirm.Button;

        public Button CloseButton => _close.Button;

        /// <summary>Abre la hoja con el precio <paramref name="price"/>, centrada en un lienzo de <paramref name="canvasW"/> × <paramref name="canvasH"/> dp.</summary>
        public void Show(int price, float canvasW, float canvasH)
        {
            Price = price;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            UiBuilder.Stretch(_root);
            _body.text = ShuffleText.Confirm(price);
            float width = Mathf.Min(328f, canvasW - 32f);
            float inner = width - 32f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner - 52f, 40f);
            UiBuilder.Place((RectTransform)_close.Button.transform, width - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
            y += 52f;
            UiBuilder.Place(_glyph.rectTransform, (width - GlyphDp) * 0.5f, y, GlyphDp, GlyphDp);
            y += GlyphDp + 8f;
            y += ModalKit.PlaceLine(_body, 16f, y, inner, 48f) + 12f;
            ModalKit.Place(_confirm, 16f, y, inner);
            y += ModalKit.HeightOf(_confirm) + 14f;
            ModalKit.Size(_panel, _surface, _art, (canvasW - width) * 0.5f, Mathf.Round((canvasH - y) * 0.45f), width, y);
        }

        /// <summary>Cierra la hoja sin hacer nada.</summary>
        public void Hide()
        {
            Price = 0;
            _root.gameObject.SetActive(false);
        }

        /// <summary>Botones visibles para <c>AF_LAYOUT</c> (sonda de desarrollo).</summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            if (!Visible)
            {
                return;
            }

            ids.Add("shuffle_cancel");
            rects.Add((RectTransform)_close.Button.transform);
            ids.Add("shuffle_confirm");
            rects.Add((RectTransform)_confirm.Button.transform);
        }
    }
}
