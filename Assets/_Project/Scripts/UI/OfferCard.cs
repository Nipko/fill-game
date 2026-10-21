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
    /// Tarjeta de una oferta (bloque B5; 05 §5: Mochila de inicio, Sin anuncios, Cántaro y skin del mundo), con la composición de
    /// <see cref="ShuffleConfirm"/> (<see cref="ModalKit"/>) en su propio lienzo encima de la victoria o del inicio: cabecera con el
    /// nombre de tienda y la X, el icono, la línea de contenido, una nota (temporizador, «¡Lleno!»…), el botón con el precio local de
    /// la tienda y «Ahora no». Sin TiTino, Rosa ni Capi (D-013, T1-A: ninguna mascota presenta una oferta). Llama una sola vez a la
    /// respuesta: true si el jugador toca comprar; false con la X, «Ahora no» o Atrás. No compra nada por sí misma.
    /// </summary>
    internal sealed class OfferCard
    {
        /// <summary>Orden del lienzo: encima de la victoria y del inicio, por debajo del Mercado (30), de Ajustes (40) y de la hoja de compra (50).</summary>
        public const int SortingOrder = 28;

        /// <summary>Lado del icono (dp).</summary>
        public const float IconDp = 56f;

        private readonly ProceduralArt _art;
        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly ReliefParts _close;
        private readonly Image _icon;
        private readonly TextMeshProUGUI _body;
        private readonly TextMeshProUGUI _note;
        private readonly ReliefParts _buy;
        private readonly TextMeshProUGUI _buyLabel;
        private readonly Button _notNow;
        private Action<bool>? _onClosed;
        private Func<bool>? _canBuy;

        public OfferCard(Transform host, ProceduralArt art)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            _canvas = new OverlayCanvas("OfferCardCanvas", host, SortingOrder);
            ModalKit.Dim("Dim", _canvas.Root, 0.55f);
            _panel = ModalKit.Card("Panel", _canvas.Root, art, out _surface);
            _title = ModalKit.Header(_panel, art, string.Empty, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, () => Close(false));
            _icon = UiBuilder.Image("Icon", _panel, art.IconSeed, Color.white);
            _icon.preserveAspect = true;
            _body = ModalKit.Line("Body", _panel, string.Empty, 17f, Palette.FamilyWood.Base);
            _note = ModalKit.Line("Note", _panel, string.Empty, 15f, Palette.FamilyWood.Deep);
            _buy = ModalKit.Cta("BuyButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, string.Empty, 21f, () => Close(true), out _buyLabel);
            _notNow = ModalKit.Link("NotNowButton", _panel, MarketText.NotNow, () => Close(false), out _);
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>Está abierta.</summary>
        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Lienzo (capturas).</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>SKU que enseña (null cerrada).</summary>
        public string? Sku { get; private set; }

        public string TitleText => _title.text;

        public string BodyText => _body.text;

        public string NoteText => _note.text;

        public string BuyText => _buyLabel.text;

        public Button BuyButton => _buy.Button;

        public Button CloseButton => _close.Button;

        public Button NotNowButton => _notNow;

        /// <summary>
        /// Abre la tarjeta del SKU <paramref name="sku"/> con su título, icono, contenido, nota (vacía = sin nota) y precio; el botón
        /// de compra, activo solo con <paramref name="buyEnabled"/>. Si ya había una abierta, la anterior se cierra como «Ahora no».
        /// </summary>
        /// <param name="canBuy">Se consulta al tocar comprar: si devuelve false (otra compra en curso), la tarjeta sigue abierta.</param>
        public void Present(
            string sku, string title, Sprite icon, Color iconColor, string body, string note, string price, bool buyEnabled, Action<bool> onClosed,
            Func<bool>? canBuy = null)
        {
            if (Visible)
            {
                Close(false);
            }

            Sku = sku;
            _onClosed = onClosed ?? throw new ArgumentNullException(nameof(onClosed));
            _canBuy = canBuy;
            _canvas.Canvas.gameObject.SetActive(true);
            _canvas.Measure();
            _title.text = title;
            _icon.sprite = icon;
            _icon.color = iconColor;
            _body.text = body;
            _note.text = note;
            _note.gameObject.SetActive(note.Length > 0);
            _buyLabel.text = price;
            UiBuilder.SetEnabled(_buy.Button, buyEnabled);
            Layout();
        }

        /// <summary>Cierra la tarjeta: comprar (true) o no (false).</summary>
        public void Close(bool buy)
        {
            if (!Visible)
            {
                return;
            }

            if (buy && (!_buy.Relief.Interactable || (_canBuy != null && !_canBuy())))
            {
                return; // sin compra posible ahora: la tarjeta sigue a la vista, nada se pierde
            }

            _canvas.Canvas.gameObject.SetActive(false);
            Sku = null;
            _canBuy = null;
            Action<bool>? done = _onClosed;
            _onClosed = null;
            done?.Invoke(buy);
        }

        /// <summary>Botones visibles para <c>AF_LAYOUT</c> (sonda de desarrollo).</summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            if (!Visible)
            {
                return;
            }

            ids.Add("offer_close");
            rects.Add((RectTransform)_close.Button.transform);
            ids.Add("offer_buy");
            rects.Add((RectTransform)_buy.Button.transform);
            ids.Add("offer_not_now");
            rects.Add((RectTransform)_notNow.transform);
        }

        private void Layout()
        {
            float canvasW = _canvas.Width;
            float canvasH = _canvas.Height;
            float width = Mathf.Min(328f, canvasW - 32f);
            float inner = width - 32f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner - 52f, 40f);
            UiBuilder.Place((RectTransform)_close.Button.transform, width - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
            y += 52f;
            UiBuilder.Place(_icon.rectTransform, (width - IconDp) * 0.5f, y, IconDp, IconDp);
            y += IconDp + 8f;
            y += ModalKit.PlaceLine(_body, 16f, y, inner, 44f) + 4f;
            if (_note.gameObject.activeSelf)
            {
                y += ModalKit.PlaceLine(_note, 16f, y, inner, 24f) + 8f;
            }

            y += 4f;
            ModalKit.Place(_buy, 16f, y, inner);
            y += ModalKit.HeightOf(_buy) + 4f;
            UiBuilder.Place((RectTransform)_notNow.transform, (width - 200f) * 0.5f, y, 200f, 48f);
            y += 48f + 12f;
            ModalKit.Size(_panel, _surface, _art, (canvasW - width) * 0.5f, Mathf.Round((canvasH - y) * 0.45f), width, y, sharedSurface: true);
        }
    }
}
