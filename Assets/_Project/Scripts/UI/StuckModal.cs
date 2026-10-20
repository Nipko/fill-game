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
    /// nuevo). El alto depende de las opciones (<see cref="Height"/>) y de la escala de texto (02 §12: las líneas y los CTA
    /// crecen, y Puñado y Balsa van una debajo de otra si a media anchura no caben). Pendiente del dueño (revisión de A1): al
    /// 130 %, en la rama B (unos 559 dp), el panel tapa la parte de abajo de las cuias de un tablero de una fila aunque el
    /// tablero suba hasta su tope (en el 31, el 36 % a 360 × 780 y el 50 % en 16:9), contra 02 §7 («el tablero queda entero a
    /// la vista»); al 100 % queda entero. Cifras de antes de DC3 (16:9 a 438,75 × 780 dp): desde DC3 (D-028) el 16:9 mide
    /// 360 × 640 dp; el panel de la rama B mide 405 dp al 100 % (tapa el 25 % de las cuias del 31) y 597 dp al 130 %, más que
    /// los 576 entre las zonas seguras (MinTextSizeTests lo registra; pendiente del dueño). Compactarlo sin bajar de 12 sp × 1,3, sin partir un precio y sin renunciar a que los
    /// CTA crezcan con la letra (07 §7.8) no llega a los 47 dp (360 × 780) y 79 dp (16:9) que faltan para no tapar las cuias:
    /// a media anchura, las etiquetas de Puñado y Balsa no caben en sus dos líneas a 15,6 dp (<c>OfferFits</c>) y el aire entre
    /// bloques da unos 15 dp. MinTextSizeTests registra cuánto tapa.
    /// </summary>
    internal sealed class StuckModal
    {
        // Etiqueta de las ofertas (Puñado y Balsa): 7 dp de aire a cada lado y el 95 % del alto de la cara, en dos líneas.
        private const float OfferLabelInsetDp = 14f;
        private const float OfferLabelFace = 0.95f;

        /// <summary>
        /// Aire mínimo arriba y abajo de la etiqueta de Puñado y Balsa con la escala de texto (dp) [P] (revisión de L4; guía 5.x:
        /// botones con aire interior): por encima del 100 %, la cara crece a lo alto lo que necesitan las líneas de la etiqueta a
        /// su tamaño más 2 × este aire, y la etiqueta va en la cara menos ese aire. Al 100 %, como siempre (OfferLabelFace).
        /// </summary>
        internal const float OfferLabelPadDp = 8f;

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

        /// <summary>Etiqueta de precio de «Continuar: +1 cuia» (tests de desbordes; tarea L4).</summary>
        internal RectTransform RescuePriceRect => _rescuePrice.Root;

        /// <summary>Alto de la cara visible de «Continuar: +1 cuia» (dp; tests de desbordes; tarea L4).</summary>
        internal float RescueFaceDp => ModalKit.FaceHeightOf(_rescue);

        /// <summary>Alto de la cara visible de Puñado y de Balsa (dp; las dos miden lo mismo; tests de desbordes; tarea L4).</summary>
        internal float OfferFaceDp => ModalKit.FaceHeightOf(_handful);

        /// <summary>Etiqueta de Puñado (tests de desbordes; tarea L4).</summary>
        internal TextMeshProUGUI HandfulLabel => _handfulLabel;

        /// <summary>Etiqueta de Balsa (tests de desbordes; tarea L4).</summary>
        internal TextMeshProUGUI BalsaLabel => _balsaLabel;

        /// <summary>Línea «Todo para seguir: semillas, una cuia y canoas infinitas» bajo la Balsa (tests; tarea FX5).</summary>
        internal TextMeshProUGUI BalsaNote => _balsaNote;

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

        /// <summary>
        /// Botones visibles con su id para <c>AF_LAYOUT</c> y, tras <c>balsa</c>, la línea «Todo para seguir: …» de la Balsa
        /// (<c>balsa_note</c>: un texto, no se toca), para comprobar en el teléfono que la Balsa sale con su contenido (prueba de la
        /// ola 3: al 130 % faltaban las dos; tarea FX5).
        /// </summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            Add(ids, rects, "undo_modal", _undo.Button);
            Add(ids, rects, "refill_video", _refillVideo.Button);
            Add(ids, rects, "refill_seeds", _refillSeeds.Button);
            Add(ids, rects, "rescue", _rescue.Button);
            Add(ids, rects, "handful", _handful.Button);
            Add(ids, rects, "balsa", _balsa.Button);
            if (_balsaNote.gameObject.activeInHierarchy)
            {
                ids.Add("balsa_note");
                rects.Add(_balsaNote.rectTransform);
            }

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
            y += ModalKit.PlaceLine(_subtitle, 16f, y, inner, 20f) + 2f;

            bool canoeLine = view.CanoeLine != StuckCanoeLine.None;
            _canoeLine.gameObject.SetActive(canoeLine);
            if (canoeLine)
            {
                _canoeLine.text = view.CanoeLine == StuckCanoeLine.LostLast ? UiText.StuckCanoeLostLast : UiText.StuckCanoeLost;
                y += ModalKit.PlaceLine(_canoeLine, 16f, y, inner, 20f) + 2f;
            }

            bool unsolvable = view.Unsolvable;
            _unsolvable.gameObject.SetActive(unsolvable);
            if (unsolvable)
            {
                y += ModalKit.PlaceLine(_unsolvable, 16f, y, inner, 36f) + 2f;
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
                float face = ModalKit.FaceHeightOf(_undo);
                UiBuilder.Place(_undoBadge, inner - 40f, (face - 28f) * 0.5f, 28f, 28f);
                _undoLabel.rectTransform.sizeDelta = new Vector2(badge ? -88f : -24f, _undoLabel.rectTransform.sizeDelta.y);
                y += ModalKit.HeightOf(_undo) + 8f;
            }
            else
            {
                // En la fila de dos botones cada etiqueta va en dos líneas, con el salto en su plantilla (UiText), nunca un Replace
                // sobre el texto: fijo («\n») en la del video y el {sep} en la de semillas, que sin el video va en una.
                _refillVideoLabel.text = UiText.StuckRefillVideo(amount);
                _refillSeedsLabel.text = UiText.StuckRefillSeeds(amount, view.UndoRefillPrice, view.UndoVideo);
                UiBuilder.SetEnabled(_refillSeeds.Button, view.UndoRefillMissing == 0);
                if (view.UndoVideo)
                {
                    float videoX = view.UndoVideoFirst ? 16f : 16f + half + 8f;
                    float seedsX = view.UndoVideoFirst ? 16f + half + 8f : 16f;
                    ModalKit.Place(_refillVideo, videoX, y, half);
                    ModalKit.Place(_refillSeeds, seedsX, y, half);
                    float videoFace = ModalKit.FaceHeightOf(_refillVideo);
                    float seedsFace = ModalKit.FaceHeightOf(_refillSeeds);
                    UiBuilder.Place(_refillVideoIcon.rectTransform, 8f, (videoFace - 20f) * 0.5f, 22f, 20f);
                    _refillVideoLabel.rectTransform.sizeDelta = new Vector2(-40f, videoFace * 0.8f);
                    _refillVideoLabel.rectTransform.anchoredPosition = new Vector2(10f, -(videoFace * 0.5f));
                    _refillSeedsLabel.rectTransform.sizeDelta = new Vector2(-16f, seedsFace * 0.9f);
                    _refillSeedsLabel.rectTransform.anchoredPosition = new Vector2(0f, -(seedsFace * 0.5f));
                    y += Mathf.Max(ModalKit.HeightOf(_refillVideo), ModalKit.HeightOf(_refillSeeds)) + 8f;
                }
                else
                {
                    float seedsFace = ModalKit.FaceHeightOf(_refillSeeds);
                    _refillSeedsLabel.rectTransform.sizeDelta = new Vector2(-24f, seedsFace * 0.7f);
                    _refillSeedsLabel.rectTransform.anchoredPosition = new Vector2(0f, -(seedsFace * 0.5f));
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

                // La etiqueta a la izquierda del precio; si no cabe en una línea a 12 sp, en dos y el botón crece (tarea L4).
                ModalKit.FitCtaLabel(_rescue, _rescueLabel, inner, _rescuePrice.Width + 36f, -(_rescuePrice.Width + 12f) * 0.5f);
                ModalKit.Place(_rescue, 16f, y, inner);
                float face = ModalKit.FaceHeightOf(_rescue);
                _rescuePrice.PlaceAt(inner - _rescuePrice.Width - 12f, (face - _rescuePrice.Height) * 0.5f);
                y += ModalKit.HeightOf(_rescue);
                string? note = !available ? UiText.MissingSeeds(view.RescueMissing) : view.RescueDiscounted ? UiText.StuckRescueDiscount : null;
                if (note != null)
                {
                    _rescueNote.gameObject.SetActive(true);
                    _rescueNote.text = note;
                    _rescueNote.color = available ? (Color)Palette.FamilyWood.Base : (Color)Palette.UiRedWarning;
                    y += ModalKit.PlaceLine(_rescueNote, 16f, y, inner, 18f) + 2f;
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
                // Las dos en una fila; si a media anchura alguna etiqueta no cabe ni a 12 sp (con la escala de texto: 02 §12), una
                // debajo de la otra, a lo ancho (la caja crece; pregunta 36 (a); A1).
                bool both = handful && balsa;
                bool stacked = both && !(OfferFits(_handful, _handfulLabel, half) && OfferFits(_balsa, _balsaLabel, half));
                float x = 16f;
                float w = both && !stacked ? half : inner;

                // Con la escala de texto, las dos caras crecen a lo alto hasta que sus etiquetas, a su tamaño, dejan
                // OfferLabelPadDp de aire arriba y abajo (las dos igual de altas; revisión de L4).
                bool roomy = UiBuilder.TextScale > 1f;
                if (roomy)
                {
                    float face = 0f;
                    if (handful)
                    {
                        face = Mathf.Max(face, OfferFaceFor(_handfulLabel, w));
                    }

                    if (balsa)
                    {
                        face = Mathf.Max(face, OfferFaceFor(_balsaLabel, w));
                    }

                    ModalKit.StretchToFace(_handful, face);
                    ModalKit.StretchToFace(_balsa, face);
                }
                for (int i = 0; i < view.Offers.Count; i++)
                {
                    ReliefParts card = view.Offers[i] == StuckOffer.Handful ? _handful : _balsa;
                    TextMeshProUGUI label = view.Offers[i] == StuckOffer.Handful ? _handfulLabel : _balsaLabel;
                    ModalKit.Place(card, x, y, w);
                    float face = ModalKit.FaceHeightOf(card);
                    label.rectTransform.sizeDelta = new Vector2(-OfferLabelInsetDp, roomy ? face - (2f * OfferLabelPadDp) : face * OfferLabelFace);
                    label.rectTransform.anchoredPosition = new Vector2(0f, -(face * 0.5f));
                    if (stacked && i < view.Offers.Count - 1)
                    {
                        y += ModalKit.HeightOf(card) + 8f;
                    }
                    else
                    {
                        x += w + 8f;
                    }
                }

                y += ModalKit.HeightOf(_handful);
                if (balsa)
                {
                    y += ModalKit.PlaceLine(_balsaNote, 16f, y, inner, 18f) + 2f;
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

        // La etiqueta de una oferta (Puñado o Balsa) cabe en un botón de width dp a su tamaño mínimo (12 sp), en su caja.
        private static bool OfferFits(ReliefParts card, TextMeshProUGUI label, float width)
        {
            return UiBuilder.MinTextHeight(label, width - OfferLabelInsetDp) <= (ModalKit.FaceHeightOf(card) * OfferLabelFace) + 0.01f;
        }

        // Alto de cara que necesita la etiqueta de una oferta en un botón de width dp, a su tamaño (TextMeshPro mide con el
        // autoajuste a su máximo), con OfferLabelPadDp de aire arriba y abajo.
        private static float OfferFaceFor(TextMeshProUGUI label, float width)
        {
            if (string.IsNullOrEmpty(label.text))
            {
                return 0f;
            }

            return Mathf.Ceil(label.GetPreferredValues(label.text, width - OfferLabelInsetDp, float.PositiveInfinity).y) + (2f * OfferLabelPadDp);
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
