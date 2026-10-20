#nullable enable
using System;
using System.Collections.Generic;
using AF.Boosters;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Acciones del Salto del Boto (bloque B3). Va aparte de <see cref="IGameActions"/> para no tocar esa interfaz (y sus dos
    /// implementaciones): la implementa <c>GameBootstrap.Skip.cs</c> y la piden con <c>as</c> el modal, la pausa y el banner. El
    /// Reto del río no la implementa (04 §2.6 e).
    /// </summary>
    public interface ISkipActions
    {
        /// <summary>Toque en «Salto del Boto» (modal, pausa o banner): abre la confirmación de una línea si la opción está lista.</summary>
        bool PressSkip();

        /// <summary>«Saltar nivel» en la confirmación: hace el salto (<c>LevelFlow.Skip</c>).</summary>
        bool ConfirmSkip();

        /// <summary>X de la confirmación: sigue donde estaba.</summary>
        void CancelSkip();
    }

    /// <summary>
    /// Confirmación de una línea del Salto del Boto (04 §2.6: «Confirmación de una línea»; c: el aviso de la Cuia extra que se
    /// pierde), con la composición de <see cref="QuitConfirm"/> y el material de D-003: título «¿Saltar el nivel?» [P] con su X,
    /// Rosa, «Rosa te lleva río arriba, al siguiente nivel», el aviso si hace falta y un único CTA «Saltar nivel» con el precio
    /// (300). Sin cuenta atrás ni anuncio (05 §10). La X sigue donde estaba (modal, pausa cerrada o banner).
    /// </summary>
    internal sealed class SkipConfirm
    {
        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly ReliefParts _close;
        private readonly Image _rosa;
        private readonly TextMeshProUGUI _body;
        private readonly TextMeshProUGUI _warning;
        private readonly ReliefParts _confirm;
        private readonly TextMeshProUGUI _confirmLabel;
        private readonly PriceTag _price;

        public SkipConfirm(Transform parent, ProceduralArt art, Action onConfirm, Action onCancel)
        {
            _art = art;
            _root = UiBuilder.Rect("SkipConfirm", parent);
            UiBuilder.Stretch(_root);
            ModalKit.Dim("Dim", _root, 0.55f);
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            ModalKit.Header(_panel, art, SkipText.ConfirmTitle, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, onCancel);
            _rosa = UiBuilder.Image("Rosa", _panel, art.Rosa, Color.white);
            _rosa.preserveAspect = true;
            _body = ModalKit.Line("Body", _panel, SkipText.Tagline, 17f, Palette.FamilyWood.Base);
            _warning = ModalKit.Line("ExtraCuia", _panel, SkipText.ConfirmExtraCuia, 15f, Palette.UiRedWarning);
            _confirm = ModalKit.Cta("SkipConfirmButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, SkipText.ConfirmCta, 20f, onConfirm, out _confirmLabel);
            _price = new PriceTag("Price", _confirm.Face, art);
            _root.gameObject.SetActive(false);
        }

        public bool Visible => _root.gameObject.activeSelf;

        public RectTransform Root => _root;

        public Button ConfirmButton => _confirm.Button;

        public Button CloseButton => _close.Button;

        /// <summary>Se ve el aviso de la Cuia extra (04 §2.6 c).</summary>
        public bool WarnsExtraCuia => _warning.gameObject.activeSelf;

        /// <summary>Precio que muestra el CTA.</summary>
        public int PriceShown => _price.Value;

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        /// <summary>Abre la confirmación con el precio de <paramref name="offer"/> y, si <paramref name="losesExtraCuia"/>, el aviso.</summary>
        public void Show(SkipOffer offer, bool losesExtraCuia, float canvasW, float canvasH)
        {
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling(); // encima del modal y de la pausa
            float width = Mathf.Min(328f, canvasW - 32f);
            float inner = width - 32f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner - 52f, 40f);
            UiBuilder.Place((RectTransform)_close.Button.transform, width - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
            y += 52f;
            UiBuilder.Place(_rosa.rectTransform, (width - 72f) * 0.5f, y, 72f, 48f);
            y += 54f;
            y += ModalKit.PlaceLine(_body, 16f, y, inner, 44f) + 6f;
            _warning.gameObject.SetActive(losesExtraCuia);
            if (losesExtraCuia)
            {
                y += ModalKit.PlaceLine(_warning, 16f, y, inner, 22f) + 6f;
            }

            _price.Set(offer.Price, 0, false);
            ModalKit.FitCtaLabel(_confirm, _confirmLabel, inner, _price.Width + 36f, -(_price.Width + 12f) * 0.5f);
            ModalKit.Place(_confirm, 16f, y, inner);
            float face = ModalKit.FaceHeightOf(_confirm);
            _price.PlaceAt(inner - _price.Width - 12f, (face - _price.Height) * 0.5f);
            y += ModalKit.HeightOf(_confirm) + 14f;
            ModalKit.Size(_panel, _surface, _art, (canvasW - width) * 0.5f, Mathf.Round((canvasH - y) * 0.45f), width, y);
        }

        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            ids.Add("close");
            rects.Add((RectTransform)_close.Button.transform);
            ids.Add("confirm_skip");
            rects.Add((RectTransform)_confirm.Button.transform);
        }
    }

    /// <summary>
    /// Banner del Salto del Boto en la entrada del nivel (04 §2.6, «Inventario y HUD»: «como banner en la pantalla previa del
    /// nivel cuando es elegible ("Rosa puede llevarte río arriba · 300")»). [P] (nota §3, Q-S3): una franja de madera con Rosa,
    /// arriba del tablero, mientras la opción se ofrece y el intento aún no empezó (tarjeta previa, intro o tablero sin vertidos);
    /// un toque abre la confirmación. No tapa el tablero ni captura toques fuera de su franja.
    /// </summary>
    internal sealed class SkipBanner
    {
        /// <summary>Alto de la franja (dp; área táctil ≥ 48, 07 §7.8).</summary>
        public const float HeightDp = 48f;

        /// <summary>Distancia desde el borde de arriba del lienzo (dp), bajo la cabecera del nivel [P].</summary>
        public const float TopDp = 132f;

        private readonly RectTransform _root;
        private readonly Button _button;
        private readonly TextMeshProUGUI _label;
        private int _price = -1;

        public SkipBanner(Transform parent, ProceduralArt art, Action onTap)
        {
            _button = UiBuilder.BareButton("SkipBanner", parent, onTap);
            _root = (RectTransform)_button.transform;
            Image plate = UiBuilder.Sliced("Plate", _root, art.RoundedRect, Palette.FamilyWood.Base);
            UiBuilder.Stretch(plate.rectTransform);
            Image rosa = UiBuilder.Image("Rosa", _root, art.Rosa, Color.white);
            rosa.preserveAspect = true;
            UiBuilder.Place(rosa.rectTransform, 8f, 6f, 48f, 36f);
            _label = UiBuilder.Label("Label", _root, TextStyle.PlainBold, string.Empty, 15f, Palette.Cream, TextAlignmentOptions.MidlineLeft);
            _label.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_label, UiBuilder.MinTextSp, 15f);
            _root.gameObject.SetActive(false);
        }

        public bool Visible => _root.gameObject.activeSelf;

        public Button Button => _button;

        /// <summary>Texto mostrado (tests).</summary>
        public string Text => _label.text;

        public void Hide()
        {
            if (_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }
        }

        /// <summary>Muestra la franja con <paramref name="price"/>; solo compone el texto si el precio cambió.</summary>
        public void Show(int price, float canvasW)
        {
            if (price != _price)
            {
                _price = price;
                _label.text = SkipText.Banner(price);
            }

            float width = Mathf.Min(328f, canvasW - 32f);
            UiBuilder.Place(_root, (canvasW - width) * 0.5f, TopDp, width, HeightDp);
            UiBuilder.Place(_label.rectTransform, 64f, 0f, width - 76f, HeightDp);
            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
                _root.SetAsLastSibling();
            }
        }
    }
}
