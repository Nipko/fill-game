#nullable enable
using System.Collections.Generic;
using AF.Boosters;
using AF.Levels;
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
    /// está disponible) y «Rendirse»; la X y «Rendirse» confirman la rendición. Sin anuncio (05 §10). La cabecera lleva la
    /// etiqueta del nivel (propuesta-mecanicas-dificultad.md §c.1; D-009): ámbar con la rana en Difícil, índigo con el jaguar en
    /// Muy difícil y la madera de siempre en normal (<see cref="SetLabel"/>, aplicada en el siguiente <see cref="Show"/>).
    /// </summary>
    internal sealed class GiveUpConfirm
    {
        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly Image _badge;
        private readonly ReliefParts _close;
        private readonly Image _canoe;
        private readonly TextMeshProUGUI _line;
        private readonly ReliefParts _rescue;
        private readonly TextMeshProUGUI _rescueLabel;
        private readonly PriceTag _price;
        private readonly TextMeshProUGUI _note;
        private readonly NotePulse _pulse;
        private readonly Button _giveUp;
        private StuckRescueKind _rescueKind;
        private float _height = 300f;
        private float _width = 328f;
        private string _label = LevelDefinition.LabelNormal;
        private Color? _band;

        public GiveUpConfirm(Transform parent, ProceduralArt art, IGameActions actions)
        {
            _art = art;
            _root = UiBuilder.Rect("GiveUpConfirm", parent);
            ModalKit.Dim("Dim", _root, 0.55f);
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            _title = ModalKit.Header(_panel, art, UiText.GiveUpConfirmTitle, out _header);
            _badge = ModalKit.HeaderBadge(_header);
            _close = ModalKit.Close("CloseButton", _panel, art, () => actions.ConfirmGiveUp());
            _canoe = UiBuilder.Image("Canoe", _panel, art.IconCanoeEmpty, Color.white);
            _canoe.preserveAspect = true;
            _line = ModalKit.Line("Line", _panel, UiText.GiveUpConfirmCanoe, 18f, Palette.UiRedWarning);
            _rescue = ModalKit.Cta("RescueButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, UiText.StuckRescueCta, 21f, () => OnRescue(actions), out _rescueLabel);
            _price = new PriceTag("Price", _rescue.Face, art);
            _note = ModalKit.Line("Note", _panel, string.Empty, 13f, Palette.FamilyWood.Base);
            _pulse = new NotePulse(_note);
            _giveUp = ModalKit.Link("GiveUpButton", _panel, UiText.StuckGiveUp, () => actions.ConfirmGiveUp(), out _);
            _root.gameObject.SetActive(false);
        }

        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>
        /// Toque en «Continuar: +1 cuia» en gris (sin saldo): no compra nada, pero el toque llega (observación 6 de la jornada 5). Lo
        /// pone <c>GameScreen</c>, como en Aguas estancadas (<see cref="StuckModal.RescueMissing"/>).
        /// </summary>
        public System.Action? RescueMissing { get; set; }

        /// <summary>«Te faltan {n} semillas» responde a un toque (tests).</summary>
        internal NotePulse RescuePulse => _pulse;

        /// <summary>Late «Te faltan {n} semillas» bajo el rescate en gris (<see cref="NotePulse"/>). False si no se ve.</summary>
        public bool PulseRescueMissing(bool reduceMotion)
        {
            return Visible && _rescueKind == StuckRescueKind.Disabled && _pulse.Start(reduceMotion);
        }

        /// <summary>Avanza el latido de la nota.</summary>
        public void Tick(float dtMs)
        {
            _pulse.Tick(dtMs);
        }

        // Con saldo, el rescate; en gris, nada se cobra y responde RescueMissing (antes, un toque mudo; guion 8.2).
        private void OnRescue(IGameActions actions)
        {
            if (_rescueKind == StuckRescueKind.Disabled)
            {
                RescueMissing?.Invoke();
                return;
            }

            actions.PressRescue();
        }

        /// <summary>Raíz del modal, hija del lienzo de la pantalla de nivel (tests de orden de capas).</summary>
        public RectTransform Root => _root;

        public Button CloseButton => _close.Button;

        public Button RescueButton => _rescue.Button;

        /// <summary>Etiqueta de «Continuar: +1 cuia» (tests de desbordes; tarea L4).</summary>
        internal TextMeshProUGUI RescueLabel => _rescueLabel;

        /// <summary>Etiqueta de precio de «Continuar: +1 cuia» (tests de desbordes; tarea L4).</summary>
        internal RectTransform RescuePriceRect => _price.Root;

        /// <summary>Alto de la cara visible de «Continuar: +1 cuia» (dp; tests de desbordes; tarea L4).</summary>
        internal float RescueFaceDp => ModalKit.FaceHeightOf(_rescue);

        public Button GiveUpButton => _giveUp;

        /// <summary>Línea de canoa visible (texto), o null sin canoa en juego.</summary>
        public string? LineText => _line.gameObject.activeSelf ? _line.text : null;

        /// <summary><c>difficulty.label</c> que viste la cabecera en el siguiente <see cref="Show"/> (o en el actual).</summary>
        public string Label => _label;

        /// <summary>Banda de la cabecera aplicada en el último <see cref="Show"/> (<c>ui_ambar</c> o <c>ui_indigo</c>; null = madera).</summary>
        public Color? HeaderBand => _band;

        /// <summary>Color del título de la cabecera.</summary>
        public Color TitleColor => _title.color;

        /// <summary>
        /// Material del título de la cabecera (tests): el de <c>TextStyle.ModalTitle</c>, con contorno y sombra, sobre la madera
        /// y el índigo; el de la fuente, sin contorno, con la tinta sobre el ámbar (<c>ModalKit.SetTitleColor</c>).
        /// </summary>
        internal Material? TitleMaterial => _title.fontSharedMaterial;

        /// <summary>Sello de la etiqueta en la cabecera, o null si no se ve.</summary>
        public Sprite? HeaderBadge => _badge.gameObject.activeSelf ? _badge.sprite : null;

        /// <summary>
        /// Guarda la etiqueta del nivel (<c>difficulty.label</c>: <c>normal</c>, <c>hard</c> o <c>very_hard</c>) para la cabecera
        /// del siguiente <see cref="Show"/>. La llama GameScreen (GameScreen.Funnel.cs) justo antes de abrir el modal.
        /// </summary>
        public void SetLabel(string? difficultyLabel)
        {
            _label = string.IsNullOrEmpty(difficultyLabel) ? LevelDefinition.LabelNormal : difficultyLabel!;
        }

        public void Hide()
        {
            _pulse.Stop();
            _root.gameObject.SetActive(false);
        }

        public void Show(GiveUpConfirmView view, float canvasW, float canvasH)
        {
            _pulse.Stop(); // la nota vuelve a su sitio antes de recolocarla
            _rescueKind = view.Rescue;
            _root.gameObject.SetActive(true);
            _width = Mathf.Min(328f, canvasW - 32f);
            float inner = _width - 32f;
            float y = 16f;
            _band = ModalKit.ApplyHeaderLabel(_header, _title, _badge, _art, _label);
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
                y += ModalKit.PlaceLine(_line, 16f, y, inner, 24f) + 10f;
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
                if (!available)
                {
                    _rescue.Button.interactable = true; // gris, pero el toque responde (OnRescue; observación 6 de la jornada 5)
                }

                _price.Set(view.RescuePrice, view.RescueDiscounted ? view.RescueBasePrice : 0, !available);

                // La etiqueta a la izquierda del precio; si no cabe en una línea a 12 sp, en dos y el botón crece (tarea L4).
                ModalKit.FitCtaLabel(_rescue, _rescueLabel, inner, _price.Width + 36f, -(_price.Width + 12f) * 0.5f);
                ModalKit.Place(_rescue, 16f, y, inner);
                _price.PlaceAt(inner - _price.Width - 12f, (ModalKit.FaceHeightOf(_rescue) - _price.Height) * 0.5f);
                y += ModalKit.HeightOf(_rescue);
                if (!available)
                {
                    _note.gameObject.SetActive(true);
                    _note.text = UiText.MissingSeeds(view.RescueMissing);
                    _note.color = Palette.UiRedWarning;
                    y += ModalKit.PlaceLine(_note, 16f, y, inner, 18f) + 2f;
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
