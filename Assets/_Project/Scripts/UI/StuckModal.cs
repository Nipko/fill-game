#nullable enable
using System.Collections.Generic;
using System.Globalization;
using AF.Boosters;
using AF.IAP;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Panel de Aguas estancadas (paso 1 del embudo de D-008; GDD 04 §6.2, §6.5 y §6.6 wireframes A, B y C; 07 §7.5; tarea
    /// B2-2), anclado abajo con el tablero atenuado detrás, como «No Available Moves» de Magic Sort (ficha §4.4: saldo arriba,
    /// tablero a la vista, opciones abajo) pero con la estructura y las cifras del GDD y el material de D-003:
    /// <list type="bullet">
    /// <item><description>titular «No hay más vertidos», subtítulo «Aguas estancadas» y, desde el 31, la línea de canoa;</description></item>
    /// <item><description>regla 0: Marea primero («Deshacer · Marea» con su cupo) o su recarga, video y semillas en una fila;</description></item>
    /// <item><description>desde el 31, «Continuar: +1 cuia» con su precio (300 con el 600 tachado el primero del día), o en
    /// gris con «Te faltan {n} semillas» y, debajo, Puñado y Balsa en el orden de la geo;</description></item>
    /// <item><description>pie: «Volver a la orilla (gratis)» como botón en 1-30 y el enlace «Rendirse» [P] desde el 31.</description></item>
    /// </list>
    /// Se rearma con <see cref="Show"/> cada vez que cambia el contexto (p. ej. tras comprar un Puñado: mismo precio, saldo
    /// nuevo). El alto depende de las opciones (<see cref="Height"/>).
    /// </summary>
    internal sealed class StuckModal
    {
        private readonly ProceduralArt _art;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _subtitle;
        private readonly TextMeshProUGUI _canoeLine;
        private readonly TextMeshProUGUI _unsolvable;
        private readonly ReliefParts _undo;
        private readonly TextMeshProUGUI _undoLabel;
        private readonly RectTransform _undoBadge;
        private readonly TextMeshProUGUI _undoBadgeText;
        private readonly ReliefParts _refillVideo;
        private readonly TextMeshProUGUI _refillVideoLabel;
        private readonly Image _refillVideoIcon;
        private readonly ReliefParts _refillSeeds;
        private readonly TextMeshProUGUI _refillSeedsLabel;
        private readonly ReliefParts _rescue;
        private readonly TextMeshProUGUI _rescueLabel;
        private readonly PriceTag _rescuePrice;
        private readonly TextMeshProUGUI _rescueNote;
        private readonly ReliefParts _handful;
        private readonly TextMeshProUGUI _handfulLabel;
        private readonly ReliefParts _balsa;
        private readonly TextMeshProUGUI _balsaLabel;
        private readonly TextMeshProUGUI _balsaNote;
        private readonly ReliefParts _restart;
        private readonly Button _giveUp;

        private StuckModalView? _view;
        private float _width = 328f;

        public StuckModal(Transform overlay, ProceduralArt art, IGameActions actions)
        {
            _art = art;
            _panel = ModalKit.Card("Panel", overlay, art, out _surface);
            ModalKit.Header(_panel, art, UiText.StuckTitle, out _header);
            _subtitle = ModalKit.Line("Subtitle", _panel, UiText.StuckSubtitle, 14f, Palette.FamilyWood.Base);
            _canoeLine = ModalKit.Line("CanoeLine", _panel, UiText.StuckCanoeLost, 15f, Palette.UiRedWarning);
            _unsolvable = ModalKit.Line("Unsolvable", _panel, UiText.StuckUnsolvable, 14f, Palette.FamilyWood.Base);

            _undo = ModalKit.Cta("UndoModalButton", _panel, art.CtaWater, TextStyle.CtaWater, UiText.StuckUndo, 22f, () => OnUndo(actions), out _undoLabel);
            _undoBadge = UiBuilder.Rect("Badge", _undo.Face);
            Image badgeDisc = UiBuilder.Image("Disc", _undoBadge, art.Disc, Palette.Cream);
            UiBuilder.Stretch(badgeDisc.rectTransform);
            _undoBadgeText = UiBuilder.Label("Value", _undoBadge, TextStyle.PlainBold, string.Empty, 15f, Palette.Ink, TextAlignmentOptions.Center);
            UiBuilder.Stretch(_undoBadgeText.rectTransform);

            _refillVideo = ModalKit.Cta("RefillVideoButton", _panel, art.CtaWater, TextStyle.CtaWater, string.Empty, 16f, () => actions.PressUndoRefillVideo(), out _refillVideoLabel);
            _refillVideoLabel.textWrappingMode = TextWrappingModes.Normal;
            _refillVideoIcon = UiBuilder.Image("Video", _refillVideo.Face, art.IconVideo, Color.white);
            _refillSeeds = ModalKit.Cta("RefillSeedsButton", _panel, art.CtaWood, TextStyle.PillWood, string.Empty, 15f, () => actions.PressUndoRefillSeeds(), out _refillSeedsLabel);
            _refillSeedsLabel.textWrappingMode = TextWrappingModes.Normal;

            _rescue = ModalKit.Cta("RescueButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, UiText.StuckRescueCta, 21f, () => actions.PressRescue(), out _rescueLabel);
            _rescuePrice = new PriceTag("Price", _rescue.Face, art);
            _rescueNote = ModalKit.Line("RescueNote", _panel, string.Empty, 13f, Palette.FamilyWood.Base);

            _handful = ModalKit.Cta("HandfulButton", _panel, art.CtaWood, TextStyle.PillWood, string.Empty, 14f, () => actions.PressHandful(), out _handfulLabel);
            _handfulLabel.textWrappingMode = TextWrappingModes.Normal;
            _balsa = ModalKit.Cta("BalsaButton", _panel, art.CtaWood, TextStyle.PillWood, string.Empty, 14f, () => actions.PressBalsa(), out _balsaLabel);
            _balsaLabel.textWrappingMode = TextWrappingModes.Normal;
            _balsaNote = ModalKit.Line("BalsaNote", _panel, UiText.StuckBalsaContent, 12f, Palette.FamilyWood.Base);

            _restart = ModalKit.Cta("RestartModalButton", _panel, art.CtaWood, TextStyle.PillWood, UiText.StuckRestart, 18f, () => actions.PressRestart(), out _);
            _giveUp = ModalKit.Link("GiveUpButton", _panel, UiText.StuckGiveUp, () => actions.PressGiveUp(), out _);
        }

        /// <summary>Panel.</summary>
        public RectTransform Panel => _panel;

        /// <summary>Alto del panel con las opciones del último <see cref="Show"/> (dp).</summary>
        public float Height { get; private set; } = 232f;

        /// <summary>Modelo mostrado.</summary>
        public StuckModalView? View => _view;

        /// <summary>Marea del modal (también la recarga gratis de la rama F).</summary>
        public Button UndoButton => _undo.Button;

        /// <summary>«+3 Deshacer · Ver video».</summary>
        public Button RefillVideoButton => _refillVideo.Button;

        /// <summary>«+3 Deshacer · 30 semillas».</summary>
        public Button RefillSeedsButton => _refillSeeds.Button;

        /// <summary>«Continuar: +1 cuia».</summary>
        public Button RescueButton => _rescue.Button;

        /// <summary>Etiqueta de «Continuar: +1 cuia» (tests de legibilidad del estado deshabilitado).</summary>
        public TextMeshProUGUI RescueLabel => _rescueLabel;

        /// <summary>Puñado de semillas.</summary>
        public Button HandfulButton => _handful.Button;

        /// <summary>Balsa de rescate.</summary>
        public Button BalsaButton => _balsa.Button;

        /// <summary>Pie en 1-30: «Volver a la orilla (gratis)».</summary>
        public Button RestartButton => _restart.Button;

        /// <summary>Pie desde el 31: «Rendirse».</summary>
        public Button GiveUpButton => _giveUp;

        /// <summary>Línea de canoa visible (texto), o null.</summary>
        public string? CanoeLineText => _canoeLine.gameObject.activeSelf ? _canoeLine.text : null;

        /// <summary>Texto de la nota del rescate (descuento o «Te faltan»), o null.</summary>
        public string? RescueNoteText => _rescueNote.gameObject.activeSelf ? _rescueNote.text : null;

        /// <summary>Precio que muestra el rescate (0 si no hay rescate).</summary>
        public int RescuePriceShown => _rescue.Button.gameObject.activeSelf ? _rescuePrice.Value : 0;

        /// <summary>Botones visibles con su id para <c>AF_LAYOUT</c>.</summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            Add(ids, rects, "undo_modal", _undo.Button);
            Add(ids, rects, "refill_video", _refillVideo.Button);
            Add(ids, rects, "refill_seeds", _refillSeeds.Button);
            Add(ids, rects, "rescue", _rescue.Button);
            Add(ids, rects, "handful", _handful.Button);
            Add(ids, rects, "balsa", _balsa.Button);
            Add(ids, rects, "restart_modal", _restart.Button);
            Add(ids, rects, "give_up", _giveUp);
        }

        /// <summary>Rearma el panel con <paramref name="view"/> (textos, visibles y alto) para un lienzo de <paramref name="canvasW"/> dp.</summary>
        public void Show(StuckModalView view, GameServices? services, float canvasW)
        {
            _view = view;
            _width = Mathf.Min(328f, canvasW - 32f);
            float inner = _width - 32f;
            float half = (inner - 8f) * 0.5f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner, 40f);
            y += 42f;
            UiBuilder.Place(_subtitle.rectTransform, 16f, y, inner, 20f);
            y += 22f;

            bool canoeLine = view.CanoeLine != StuckCanoeLine.None;
            _canoeLine.gameObject.SetActive(canoeLine);
            if (canoeLine)
            {
                _canoeLine.text = view.CanoeLine == StuckCanoeLine.LostLast ? UiText.StuckCanoeLostLast : UiText.StuckCanoeLost;
                UiBuilder.Place(_canoeLine.rectTransform, 16f, y, inner, 20f);
                y += 22f;
            }

            bool unsolvable = view.Unsolvable;
            _unsolvable.gameObject.SetActive(unsolvable);
            if (unsolvable)
            {
                UiBuilder.Place(_unsolvable.rectTransform, 16f, y, inner, 36f);
                y += 38f;
            }

            y += 6f;

            // Regla 0: Marea (o la recarga gratis de la rama F) a lo ancho; si no, la recarga por video y por semillas en una fila.
            bool refill = view.Undo == StuckUndoKind.Refill;
            _undo.Button.gameObject.SetActive(!refill);
            _refillVideo.Button.gameObject.SetActive(refill && view.UndoVideo);
            _refillSeeds.Button.gameObject.SetActive(refill);
            int amount = services?.Wallet.Config.UndoRefillAmount ?? 3;
            if (!refill)
            {
                _undoLabel.text = view.Undo == StuckUndoKind.Gift ? UiText.StuckUndoGift : UiText.StuckUndo;
                bool badge = view.Undo == StuckUndoKind.Undo && !view.UndoUnlimited && view.UndoBadge > 0;
                _undoBadge.gameObject.SetActive(badge);
                if (badge)
                {
                    _undoBadgeText.text = view.UndoBadge.ToString(CultureInfo.InvariantCulture);
                }

                ModalKit.Place(_undo, 16f, y, inner);
                float face = _art.CtaWater.FaceHeightDp;
                UiBuilder.Place(_undoBadge, inner - 40f, (face - 28f) * 0.5f, 28f, 28f);
                _undoLabel.rectTransform.sizeDelta = new Vector2(badge ? -88f : -24f, _undoLabel.rectTransform.sizeDelta.y);
                y += ModalKit.HeightOf(_undo) + 8f;
            }
            else
            {
                // En la fila de dos botones cada etiqueta va en dos líneas: el salto es el {sep} de su plantilla (UiText), nunca un
                // Replace sobre el texto; sin el video, la de semillas va en una.
                _refillVideoLabel.text = UiText.StuckRefillVideo(amount);
                _refillSeedsLabel.text = UiText.StuckRefillSeeds(amount, view.UndoRefillPrice, view.UndoVideo);
                UiBuilder.SetEnabled(_refillSeeds.Button, view.UndoRefillMissing == 0);
                if (view.UndoVideo)
                {
                    float videoX = view.UndoVideoFirst ? 16f : 16f + half + 8f;
                    float seedsX = view.UndoVideoFirst ? 16f + half + 8f : 16f;
                    ModalKit.Place(_refillVideo, videoX, y, half);
                    ModalKit.Place(_refillSeeds, seedsX, y, half);
                    UiBuilder.Place(_refillVideoIcon.rectTransform, 8f, (_art.CtaWater.FaceHeightDp - 20f) * 0.5f, 22f, 20f);
                    _refillVideoLabel.rectTransform.sizeDelta = new Vector2(-40f, _art.CtaWater.FaceHeightDp * 0.8f);
                    _refillVideoLabel.rectTransform.anchoredPosition = new Vector2(10f, -(_art.CtaWater.FaceHeightDp * 0.5f));
                    _refillSeedsLabel.rectTransform.sizeDelta = new Vector2(-16f, _art.CtaWood.FaceHeightDp * 0.9f);
                    _refillSeedsLabel.rectTransform.anchoredPosition = new Vector2(0f, -(_art.CtaWood.FaceHeightDp * 0.5f));
                    y += Mathf.Max(ModalKit.HeightOf(_refillVideo), ModalKit.HeightOf(_refillSeeds)) + 8f;
                }
                else
                {
                    _refillSeedsLabel.rectTransform.sizeDelta = new Vector2(-24f, _art.CtaWood.FaceHeightDp * 0.7f);
                    _refillSeedsLabel.rectTransform.anchoredPosition = new Vector2(0f, -(_art.CtaWood.FaceHeightDp * 0.5f));
                    ModalKit.Place(_refillSeeds, 16f, y, inner);
                    y += ModalKit.HeightOf(_refillSeeds) + 8f;
                }
            }

            // Cuia de rescate (ramas A y B): precio en la etiqueta ámbar; el primero del día con el 600 tachado y «−50 %».
            bool rescue = view.Rescue != StuckRescueKind.None;
            _rescue.Button.gameObject.SetActive(rescue);
            _rescueNote.gameObject.SetActive(false);
            if (rescue)
            {
                bool available = view.Rescue == StuckRescueKind.Available;
                UiBuilder.SetEnabled(_rescue.Button, available);
                _rescuePrice.Set(view.RescuePrice, view.RescueDiscounted ? view.RescueBasePrice : 0, !available);
                ModalKit.Place(_rescue, 16f, y, inner);
                float face = _art.CtaLeaf.FaceHeightDp;
                _rescuePrice.PlaceAt(inner - _rescuePrice.Width - 12f, (face - 26f) * 0.5f);
                _rescueLabel.rectTransform.sizeDelta = new Vector2(-(_rescuePrice.Width + 36f), _rescueLabel.rectTransform.sizeDelta.y);
                _rescueLabel.rectTransform.anchoredPosition = new Vector2(-(_rescuePrice.Width + 12f) * 0.5f, _rescueLabel.rectTransform.anchoredPosition.y);
                y += ModalKit.HeightOf(_rescue);
                string? note = !available ? UiText.MissingSeeds(view.RescueMissing) : view.RescueDiscounted ? UiText.StuckRescueDiscount : null;
                if (note != null)
                {
                    _rescueNote.gameObject.SetActive(true);
                    _rescueNote.text = note;
                    _rescueNote.color = available ? (Color)Palette.FamilyWood.Base : (Color)Palette.UiRedWarning;
                    UiBuilder.Place(_rescueNote.rectTransform, 16f, y, inner, 18f);
                    y += 20f;
                }

                y += 8f;
            }

            // Rama B: Puñado y Balsa, en el orden de la geo (04 §6.2), con su precio local.
            bool handful = false;
            bool balsa = false;
            for (int i = 0; i < view.Offers.Count; i++)
            {
                handful |= view.Offers[i] == StuckOffer.Handful;
                balsa |= view.Offers[i] == StuckOffer.Balsa;
            }

            _handful.Button.gameObject.SetActive(handful);
            _balsa.Button.gameObject.SetActive(balsa);
            _balsaNote.gameObject.SetActive(balsa);
            if (handful || balsa)
            {
                string handfulPrice = services?.PriceText(StoreCatalog.Handful) ?? string.Empty;
                string balsaPrice = services?.PriceText(StoreCatalog.Rescue) ?? string.Empty;
                _handfulLabel.text = UiText.StuckPackLine(services?.HandfulSeeds ?? 1000, handfulPrice);
                _balsaLabel.text = UiText.StuckBalsaLine(balsaPrice);
                float x = 16f;
                float w = handful && balsa ? half : inner;
                for (int i = 0; i < view.Offers.Count; i++)
                {
                    ReliefParts card = view.Offers[i] == StuckOffer.Handful ? _handful : _balsa;
                    TextMeshProUGUI label = view.Offers[i] == StuckOffer.Handful ? _handfulLabel : _balsaLabel;
                    ModalKit.Place(card, x, y, w);
                    label.rectTransform.sizeDelta = new Vector2(-14f, _art.CtaWood.FaceHeightDp * 0.95f);
                    label.rectTransform.anchoredPosition = new Vector2(0f, -(_art.CtaWood.FaceHeightDp * 0.5f));
                    x += w + 8f;
                }

                y += ModalKit.HeightOf(_handful);
                if (balsa)
                {
                    UiBuilder.Place(_balsaNote.rectTransform, 16f, y, inner, 18f);
                    y += 20f;
                }

                y += 6f;
            }

            // Pie: botón en 1-30 (rama C: «es la salida normal»), enlace «Rendirse» desde el 31.
            bool restart = view.Footer == StuckFooter.RestartFree;
            _restart.Button.gameObject.SetActive(restart);
            _giveUp.gameObject.SetActive(!restart);
            if (restart)
            {
                ModalKit.Place(_restart, 16f, y, inner);
                y += ModalKit.HeightOf(_restart) + 12f;
            }
            else
            {
                UiBuilder.Place((RectTransform)_giveUp.transform, (_width - 200f) * 0.5f, y, 200f, 48f);
                y += 48f + 8f;
            }

            Height = y;
        }

        /// <summary>Coloca el panel centrado en horizontal con su borde superior en <paramref name="top"/>.</summary>
        public void Place(float canvasW, float top)
        {
            ModalKit.Size(_panel, _surface, _art, (canvasW - _width) * 0.5f, top, _width, Height);
        }

        private void OnUndo(IGameActions actions)
        {
            if (_view != null && _view.Undo == StuckUndoKind.Gift)
            {
                actions.PressUndoGift();
                return;
            }

            actions.PressUndo();
        }

        private static void Add(List<string> ids, List<RectTransform> rects, string id, Button button)
        {
            if (button.gameObject.activeInHierarchy)
            {
                ids.Add(id);
                rects.Add((RectTransform)button.transform);
            }
        }
    }
}
