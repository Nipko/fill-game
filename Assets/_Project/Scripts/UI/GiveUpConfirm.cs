#nullable enable
using System.Collections.Generic;
using AF.Boosters;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// ¿Seguro? [P] (paso 2 del embudo de D-008; GDD 04 §6.2 <c>buildGiveUpConfirm</c> y wireframe «Paso 2» de §6.6; tarea
    /// B2-2), con la composición de «Continue?» de Magic Sort (ficha §4.4: título con X, imagen de lo que se pierde, la
    /// frase y un único botón de seguir) y el material de D-003: canoa volcada en lugar del corazón roto. «Perderás 1 canoa»
    /// (o «tu última canoa»), otra vez «Continuar: +1 cuia» con el mismo precio y condiciones (no aparece si el rescate no
    /// está disponible) y «Rendirse»; la X y «Rendirse» confirman la rendición. Sin anuncio (05 §10).
    /// </summary>
    internal sealed class GiveUpConfirm
    {
        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly ReliefParts _close;
        private readonly Image _canoe;
        private readonly TextMeshProUGUI _line;
        private readonly ReliefParts _rescue;
        private readonly TextMeshProUGUI _rescueLabel;
        private readonly PriceTag _price;
        private readonly TextMeshProUGUI _note;
        private readonly Button _giveUp;
        private float _height = 300f;
        private float _width = 328f;

        public GiveUpConfirm(Transform parent, ProceduralArt art, IGameActions actions)
        {
            _art = art;
            _root = UiBuilder.Rect("GiveUpConfirm", parent);
            ModalKit.Dim("Dim", _root, 0.55f);
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            ModalKit.Header(_panel, art, UiText.GiveUpConfirmTitle, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, () => actions.ConfirmGiveUp());
            _canoe = UiBuilder.Image("Canoe", _panel, art.IconCanoeEmpty, Color.white);
            _canoe.preserveAspect = true;
            _line = ModalKit.Line("Line", _panel, UiText.GiveUpConfirmCanoe, 18f, Palette.UiRedWarning);
            _rescue = ModalKit.Cta("RescueButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, UiText.StuckRescueCta, 21f, () => actions.PressRescue(), out _rescueLabel);
            _price = new PriceTag("Price", _rescue.Face, art);
            _note = ModalKit.Line("Note", _panel, string.Empty, 13f, Palette.FamilyWood.Base);
            _giveUp = ModalKit.Link("GiveUpButton", _panel, UiText.StuckGiveUp, () => actions.ConfirmGiveUp(), out _);
            _root.gameObject.SetActive(false);
        }

        public bool Visible => _root.gameObject.activeSelf;

        public Button CloseButton => _close.Button;

        public Button RescueButton => _rescue.Button;

        public Button GiveUpButton => _giveUp;

        /// <summary>Línea de canoa visible (texto), o null sin canoa en juego.</summary>
        public string? LineText => _line.gameObject.activeSelf ? _line.text : null;

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        public void Show(GiveUpConfirmView view, float canvasW, float canvasH)
        {
            _root.gameObject.SetActive(true);
            _width = Mathf.Min(328f, canvasW - 32f);
            float inner = _width - 32f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner - 52f, 40f);
            UiBuilder.Place((RectTransform)_close.Button.transform, _width - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
            y += 52f;

            // Sin canoa en juego (Canoas infinitas o 0 canoas) no se anuncia ninguna pérdida: ni la canoa volcada ni la línea.
            bool canoe = view.ShowsCanoe;
            _canoe.gameObject.SetActive(canoe);
            _line.gameObject.SetActive(canoe);
            if (canoe)
            {
                _line.text = view.LastCanoe ? UiText.GiveUpConfirmLastCanoe : UiText.GiveUpConfirmCanoe;
                UiBuilder.Place(_canoe.rectTransform, (_width - 132f) * 0.5f, y, 132f, 78f);
                y += 84f;
                UiBuilder.Place(_line.rectTransform, 16f, y, inner, 24f);
                y += 34f;
            }
            else
            {
                y += 8f;
            }

            bool rescue = view.Rescue != StuckRescueKind.None;
            _rescue.Button.gameObject.SetActive(rescue);
            _note.gameObject.SetActive(false);
            if (rescue)
            {
                bool available = view.Rescue == StuckRescueKind.Available;
                UiBuilder.SetEnabled(_rescue.Button, available);
                _price.Set(view.RescuePrice, view.RescueDiscounted ? view.RescueBasePrice : 0, !available);
                ModalKit.Place(_rescue, 16f, y, inner);
                _price.PlaceAt(inner - _price.Width - 12f, (_art.CtaLeaf.FaceHeightDp - 26f) * 0.5f);
                _rescueLabel.rectTransform.sizeDelta = new Vector2(-(_price.Width + 36f), _rescueLabel.rectTransform.sizeDelta.y);
                _rescueLabel.rectTransform.anchoredPosition = new Vector2(-(_price.Width + 12f) * 0.5f, _rescueLabel.rectTransform.anchoredPosition.y);
                y += ModalKit.HeightOf(_rescue);
                if (!available)
                {
                    _note.gameObject.SetActive(true);
                    _note.text = UiText.MissingSeeds(view.RescueMissing);
                    _note.color = Palette.UiRedWarning;
                    UiBuilder.Place(_note.rectTransform, 16f, y, inner, 18f);
                    y += 20f;
                }

                y += 8f;
            }

            UiBuilder.Place((RectTransform)_giveUp.transform, (_width - 200f) * 0.5f, y, 200f, 48f);
            y += 48f + 10f;
            _height = y;
            ModalKit.Size(_panel, _surface, _art, (canvasW - _width) * 0.5f, Mathf.Round((canvasH - _height) * 0.45f), _width, _height);
        }

        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            ids.Add("close");
            rects.Add((RectTransform)_close.Button.transform);
            if (_rescue.Button.gameObject.activeSelf)
            {
                ids.Add("rescue");
                rects.Add((RectTransform)_rescue.Button.transform);
            }

            ids.Add("give_up");
            rects.Add((RectTransform)_giveUp.transform);
        }
    }
}
