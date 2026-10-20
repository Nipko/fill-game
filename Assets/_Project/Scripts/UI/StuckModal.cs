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
    /// crecen, y Puñado y Balsa van una debajo de otra si a media anchura no caben).
    /// <para>
    /// Disposición compacta (tarea FX16a; decisión del dueño del 2026-10-20: «compactar Aguas estancadas … en 16:9»; 02 §7: «el
    /// tablero queda entero a la vista»): en los lienzos más bajos que <see cref="CompactCanvasBelowDp"/> (el 16:9, 360 × 640 dp
    /// desde DC3, D-028), si la disposición de siempre taparía alguna cuia con el tablero subido hasta su tope, el panel se
    /// rearma con menos aire, las líneas en su alto justo, Marea a 48 dp y el rescate a 56 dp (04 §6.6, wireframe A), los CTA
    /// sin el estirado de la escala de texto (crecen solo lo que pide su etiqueta a 12 sp) y Puñado y Balsa en una fila si
    /// caben sin partir su precio, a media anchura o con el ancho repartido según su etiqueta (en tres líneas como mucho, cuatro
    /// con la escala de texto; 64 dp de alto mínimo y 10 dp de aire a los lados). Ningún texto baja de 12 sp ni ningún botón de
    /// 48 dp, y el panel cabe siempre entre las zonas seguras (la rama B medía 597 dp al 130 %, más que los 576 dp entre ellas).
    /// Si ni así quedan las cuias enteras sobre el panel con el tablero subido hasta su tope, <c>GameScreen.FitBoardToStuck</c>
    /// reduce el tablero mientras el modal esté abierto (cifras en <c>StuckCompactTests</c>: en el 31, la rama B mide 362 dp al
    /// 100 % y 423 al 130 % en es-419, con el tablero a 1 y a 0,67; en el 640, de 3 filas, a 0,73 y 0,68 en la rama A y a 0,46
    /// y 0,28 en la rama B). A 360 × 780 no cambia nada: al 130 %, la rama B (597 dp) sigue tapando la parte de abajo de las
    /// cuias de un tablero de una fila (pendiente desde la revisión de A1: 02 §7 frente a 02 §12).
    /// </para>
    /// </summary>
    internal sealed class StuckModal
    {
        // Etiqueta de las ofertas (Puñado y Balsa) para decidir si van en una fila: en dos líneas, en el 95 % del alto de la cara
        // de siempre (la de sus sprites por la escala de texto).
        private const float OfferLabelFace = 0.95f;

        /// <summary>
        /// Alto mínimo de los botones de Puñado y Balsa (dp; <see cref="ModalKit.HeightOf"/>) [P] (revisión de DC3; 02 §12: las
        /// cajas crecen): a 360 × 640 dp sus dos líneas llegaban al canto de una cara de 40 dp, con unos 2 dp de aire.
        /// </summary>
        internal const float OfferMinHeightDp = 56f;

        /// <summary>Aire mínimo arriba y abajo de la etiqueta de Puñado y Balsa al 100 % (dp) [P] (revisión de DC3).</summary>
        internal const float OfferLabelAirDp = 6f;

        /// <summary>Aire a cada lado de la etiqueta de Puñado y Balsa (dp) [P] (revisión de DC3; antes 7).</summary>
        internal const float OfferLabelSideDp = 8f;

        // Lo que el alto colocado de un botón con relieve (ModalKit.HeightOf) queda por debajo del de su relieve estirado.
        private const float ReliefHeightTrimDp = 6f;

        // Área táctil mínima de un botón (dp; 07 §7.8).
        private const float MinTouchDp = 48f;

        /// <summary>
        /// Aire mínimo arriba y abajo de la etiqueta de Puñado y Balsa con la escala de texto (dp) [P] (revisión de L4; guía 5.x:
        /// botones con aire interior): por encima del 100 %, la cara crece a lo alto lo que necesitan las líneas de la etiqueta a
        /// su tamaño más 2 × este aire, y la etiqueta va en la cara menos ese aire. Al 100 %, con <see cref="OfferLabelAirDp"/>.
        /// </summary>
        internal const float OfferLabelPadDp = 8f;

        /// <summary>
        /// Alto de lienzo (dp) por debajo del cual el panel pasa a su disposición compacta cuando la de siempre taparía alguna
        /// cuia con el tablero subido hasta su tope (tarea FX16a; 02 §7: «el tablero queda entero a la vista») [P]: el 16:9
        /// (360 × 640 dp desde DC3, D-028) y los demás más bajos que 360 × 780, donde nada cambia.
        /// </summary>
        internal const float CompactCanvasBelowDp = 780f;

        /// <summary>Aire entre el panel y la zona segura de abajo en la disposición de siempre (dp).</summary>
        internal const float RegularBottomGapDp = 8f;

        /// <summary>Aire entre el panel y la zona segura de abajo en la disposición compacta (dp) [P] (FX16a).</summary>
        internal const float CompactBottomGapDp = 4f;

        /// <summary>
        /// Aire a cada lado de la etiqueta de Puñado y Balsa en la disposición compacta (dp) [P] (FX16a; revisión: de 10 a 12 dp
        /// a los lados, para que con un texto más largo el botón no quede apretado).
        /// </summary>
        internal const float OfferCompactSideDp = 10f;

        /// <summary>
        /// Alto mínimo de Puñado y Balsa cuando van en una fila en la disposición compacta (dp) [P] (FX16a; revisión: los dos
        /// igual de altos, a 64 dp, con la etiqueta centrada en vertical y al menos <see cref="OfferLabelAirDp"/> de aire).
        /// </summary>
        internal const float OfferCompactRowMinHeightDp = 64f;

        // Disposición compacta (FX16a) [P]: aire sobre la cabecera y entre botones (dp).
        private const float CompactTopDp = 8f;
        private const float CompactGapDp = 4f;

        /// <summary>
        /// Alto de Marea (y de su recarga) en la disposición compacta (dp): los 48 de 04 §6.6 (wireframe A: «secundaria
        /// (marfil), 48 px»), si su etiqueta cabe a 12 sp (FX16a).
        /// </summary>
        internal const float CompactUndoHeightDp = 48f;

        /// <summary>
        /// Alto de «Continuar: +1 cuia» en la disposición compacta (dp): los 56 de 04 §6.6 (wireframe A: «CTA primaria (ámbar),
        /// 56 px»), si su etiqueta cabe a 12 sp (FX16a).
        /// </summary>
        internal const float CompactRescueHeightDp = 56f;

        // Parte de la cara de un CTA que ocupa su etiqueta en una línea (UiBuilder.PlaceCtaLabel) y en dos
        // (ModalKit.CtaTwoLinesShare), e interlineado de la etiqueta en dos líneas (el de ModalKit.FitCtaLabel).
        private const float CtaOneLineShare = 0.60f;
        private const float CtaTwoLinesSpacing = -20f;

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
        private readonly TextMeshProUGUI _restartLabel;
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

            _restart = ModalKit.Cta("RestartModalButton", _panel, art.CtaWood, TextStyle.PillWood, UiText.StuckRestart, 18f, () => actions.PressRestart(), out _restartLabel);
            _giveUp = ModalKit.Link("GiveUpButton", _panel, UiText.StuckGiveUp, () => actions.PressGiveUp(), out _);
        }

        /// <summary>
        /// Solo para las capturas «antes» de FX16a (<c>StuckCompactSnapshotTests</c>): el panel siempre en la disposición de
        /// siempre y el tablero sin reducir (<c>GameScreen.FitBoardToStuck</c>), como antes de la tarea.
        /// </summary>
        internal static bool RegularOnlyForTests { get; set; }

        /// <summary>Panel.</summary>
        public RectTransform Panel => _panel;

        /// <summary>Alto del panel con las opciones del último <see cref="Show"/> (dp).</summary>
        public float Height { get; private set; } = 232f;

        /// <summary>El último <see cref="Show"/> dejó el panel en su disposición compacta (tarea FX16a).</summary>
        public bool Compact { get; private set; }

        /// <summary>Aire entre el panel y la zona segura de abajo con la disposición del último <see cref="Show"/> (dp).</summary>
        public float BottomGapDp => Compact ? CompactBottomGapDp : RegularBottomGapDp;

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
        internal float RescueFaceDp => FaceOf(_rescue);

        /// <summary>Alto de la cara visible de Puñado y de Balsa (dp; las dos miden lo mismo; tests de desbordes; tarea L4).</summary>
        internal float OfferFaceDp => FaceOf(_handful);

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

        /// <summary>
        /// Rearma el panel con <paramref name="view"/> (textos, visibles y alto) para un lienzo de <paramref name="canvasW"/> dp.
        /// <paramref name="roomDp"/> es el alto que le queda al panel, con su aire de abajo, para no tapar ninguna cuia con el
        /// tablero subido hasta su tope (<c>GameScreen</c> lo calcula en los lienzos más bajos que
        /// <see cref="CompactCanvasBelowDp"/>; sin límite, en los demás): si la disposición de siempre no cabe ahí, el panel se
        /// rearma en la compacta (<see cref="Compact"/>; tarea FX16a).
        /// </summary>
        public void Show(StuckModalView view, GameServices? services, float canvasW, float roomDp = float.PositiveInfinity)
        {
            Lay(view, services, canvasW, false);
            if (!RegularOnlyForTests && Height + RegularBottomGapDp > roomDp + 0.01f)
            {
                Lay(view, services, canvasW, true);
            }
        }

        // Disposición del panel: la de siempre o, con compact, la compacta (tarea FX16a; 02 §7: «el tablero queda entero a la
        // vista» en 16:9): menos aire, las líneas en su alto justo, los CTA sin el estirado de la escala de texto (crecen solo lo
        // que pide su etiqueta a 12 sp; Marea a 48 dp y el rescate a 56) y Puñado y Balsa en una fila si sus etiquetas caben
        // sin partir su precio en tres líneas como mucho (cuatro con la escala de texto), a media anchura o repartida (OfferRowSplit).
        // Ningún texto baja de 12 sp ni ningún botón de 48 dp.
        private void Lay(StuckModalView view, GameServices? services, float canvasW, bool compact)
        {
            _view = view;
            Compact = compact;
            _width = Mathf.Min(328f, canvasW - 32f);
            float inner = _width - 32f;
            float half = (inner - 8f) * 0.5f;
            float lineH = compact ? 0f : 20f;
            float lineAir = compact ? 0f : 2f;
            float gap = compact ? CompactGapDp : 8f;
            float y = compact ? CompactTopDp : 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner, 40f);
            y += compact ? 40f : 42f;
            y += ModalKit.PlaceLine(_subtitle, 16f, y, inner, lineH) + lineAir;

            bool canoeLine = view.CanoeLine != StuckCanoeLine.None;
            _canoeLine.gameObject.SetActive(canoeLine);
            if (canoeLine)
            {
                _canoeLine.text = view.CanoeLine == StuckCanoeLine.LostLast ? UiText.StuckCanoeLostLast : UiText.StuckCanoeLost;
                y += ModalKit.PlaceLine(_canoeLine, 16f, y, inner, lineH) + lineAir;
            }

            bool unsolvable = view.Unsolvable;
            _unsolvable.gameObject.SetActive(unsolvable);
            if (unsolvable)
            {
                y += ModalKit.PlaceLine(_unsolvable, 16f, y, inner, compact ? 0f : 36f) + lineAir;
            }

            y += compact ? 4f : 6f;

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

                float inset = badge ? 88f : 24f;
                FitStretch(_undo, compact, CompactK(_undo, _undoLabel, inner - inset, CtaOneLineShare, CompactUndoHeightDp));
                PlaceButton(_undo, 16f, y, inner);
                float face = FaceOf(_undo);
                UiBuilder.PlaceCtaLabel(_undoLabel.rectTransform, face);
                UiBuilder.Place(_undoBadge, inner - 40f, (face - 28f) * 0.5f, 28f, 28f);
                _undoLabel.rectTransform.sizeDelta = new Vector2(-inset, _undoLabel.rectTransform.sizeDelta.y);
                y += HeightOf(_undo) + gap;
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
                    // Las dos igual de altas: en la compacta, lo que pida la etiqueta más alta.
                    float k = Mathf.Max(
                        CompactK(_refillVideo, _refillVideoLabel, half - 40f, 0.8f, CompactUndoHeightDp),
                        CompactK(_refillSeeds, _refillSeedsLabel, half - 16f, 0.9f, CompactUndoHeightDp));
                    FitStretch(_refillVideo, compact, k);
                    FitStretch(_refillSeeds, compact, k);
                    float videoX = view.UndoVideoFirst ? 16f : 16f + half + 8f;
                    float seedsX = view.UndoVideoFirst ? 16f + half + 8f : 16f;
                    PlaceButton(_refillVideo, videoX, y, half);
                    PlaceButton(_refillSeeds, seedsX, y, half);
                    float videoFace = FaceOf(_refillVideo);
                    float seedsFace = FaceOf(_refillSeeds);
                    UiBuilder.Place(_refillVideoIcon.rectTransform, 8f, (videoFace - 20f) * 0.5f, 22f, 20f);
                    _refillVideoLabel.rectTransform.sizeDelta = new Vector2(-40f, videoFace * 0.8f);
                    _refillVideoLabel.rectTransform.anchoredPosition = new Vector2(10f, -(videoFace * 0.5f));
                    _refillSeedsLabel.rectTransform.sizeDelta = new Vector2(-16f, seedsFace * 0.9f);
                    _refillSeedsLabel.rectTransform.anchoredPosition = new Vector2(0f, -(seedsFace * 0.5f));
                    y += Mathf.Max(HeightOf(_refillVideo), HeightOf(_refillSeeds)) + gap;
                }
                else
                {
                    FitStretch(_refillSeeds, compact, CompactK(_refillSeeds, _refillSeedsLabel, inner - 24f, 0.7f, CompactUndoHeightDp));
                    float seedsFace = FaceOf(_refillSeeds);
                    _refillSeedsLabel.rectTransform.sizeDelta = new Vector2(-24f, seedsFace * 0.7f);
                    _refillSeedsLabel.rectTransform.anchoredPosition = new Vector2(0f, -(seedsFace * 0.5f));
                    PlaceButton(_refillSeeds, 16f, y, inner);
                    y += HeightOf(_refillSeeds) + gap;
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
                float inset = _rescuePrice.Width + 36f;
                float shift = -(_rescuePrice.Width + 12f) * 0.5f;
                if (compact)
                {
                    FitCtaLabelCompact(_rescue, _rescueLabel, inner, inset, shift);
                }
                else
                {
                    ModalKit.FitCtaLabel(_rescue, _rescueLabel, inner, inset, shift);
                }

                PlaceButton(_rescue, 16f, y, inner);
                float face = FaceOf(_rescue);
                _rescuePrice.PlaceAt(inner - _rescuePrice.Width - 12f, (face - _rescuePrice.Height) * 0.5f);
                y += HeightOf(_rescue);
                string? note = !available ? UiText.MissingSeeds(view.RescueMissing) : view.RescueDiscounted ? UiText.StuckRescueDiscount : null;
                if (note != null)
                {
                    _rescueNote.gameObject.SetActive(true);
                    _rescueNote.text = note;
                    _rescueNote.color = available ? (Color)Palette.FamilyWood.Base : (Color)Palette.UiRedWarning;
                    y += ModalKit.PlaceLine(_rescueNote, 16f, y, inner, compact ? 0f : 18f) + lineAir;
                }

                y += gap;
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
                // debajo de la otra, a lo ancho (la caja crece; pregunta 36 (a); A1). En la compacta, en una fila si cada etiqueta
                // cabe a 12 sp en su ancho sin partir su precio y en tres líneas como mucho (cuatro con la escala de texto):
                // primero a media anchura y, si no, con el ancho repartido según lo que pide cada etiqueta (OfferRowSplit; FX16a).
                bool both = handful && balsa;
                float side = compact ? OfferCompactSideDp : OfferLabelSideDp;
                float handfulW = inner;
                float balsaW = inner;
                bool stacked = false;
                if (both)
                {
                    handfulW = half;
                    balsaW = half;
                    if (compact)
                    {
                        if (!OfferRowFits(_handfulLabel, half - (2f * side), handfulPrice) || !OfferRowFits(_balsaLabel, half - (2f * side), balsaPrice))
                        {
                            stacked = !OfferRowSplit(_handfulLabel, handfulPrice, _balsaLabel, balsaPrice, inner - 8f, side, out handfulW);
                            balsaW = inner - 8f - handfulW;
                        }
                    }
                    else
                    {
                        stacked = !OfferFits(_handful, _handfulLabel, half) || !OfferFits(_balsa, _balsaLabel, half);
                    }

                    if (stacked)
                    {
                        handfulW = inner;
                        balsaW = inner;
                    }
                }

                float x = 16f;

                // Las dos caras crecen a lo alto hasta que el botón mide OfferMinHeightDp y sus etiquetas, a su tamaño, dejan
                // OfferLabelAirDp de aire arriba y abajo (OfferLabelPadDp con la escala de texto; revisión de L4) y
                // OfferLabelSideDp a los lados; las dos igual de altas (revisión de DC3). En la compacta, medidas a 12 sp (sin el
                // estirado de la escala de texto), con OfferLabelAirDp arriba y abajo, OfferCompactSideDp a los lados y, en una
                // fila, OfferCompactRowMinHeightDp de alto mínimo (revisión de FX16a).
                bool roomy = !compact && UiBuilder.TextScale > 1f;
                float pad = !compact && UiBuilder.TextScale > 1f ? OfferLabelPadDp : OfferLabelAirDp;
                float minHeight = both && !stacked ? OfferCompactRowMinHeightDp : OfferMinHeightDp;
                float minFace = compact ? _handful.Sprites.FaceHeightDp * MinK(_handful, minHeight) : OfferMinFace(_handful);
                if (handful)
                {
                    minFace = Mathf.Max(minFace, OfferFaceFor(_handfulLabel, handfulW - (2f * side), pad, roomy));
                }

                if (balsa)
                {
                    minFace = Mathf.Max(minFace, OfferFaceFor(_balsaLabel, balsaW - (2f * side), pad, roomy));
                }

                if (compact)
                {
                    Stretch(_handful, minFace / Mathf.Max(1f, _handful.Sprites.FaceHeightDp), minHeight);
                    Stretch(_balsa, minFace / Mathf.Max(1f, _balsa.Sprites.FaceHeightDp), minHeight);
                }
                else
                {
                    ModalKit.StretchToFace(_handful, minFace);
                    ModalKit.StretchToFace(_balsa, minFace);
                }

                for (int i = 0; i < view.Offers.Count; i++)
                {
                    bool isHandful = view.Offers[i] == StuckOffer.Handful;
                    ReliefParts card = isHandful ? _handful : _balsa;
                    TextMeshProUGUI label = isHandful ? _handfulLabel : _balsaLabel;
                    float w = isHandful ? handfulW : balsaW;
                    PlaceButton(card, x, y, w);
                    float face = FaceOf(card);
                    label.rectTransform.sizeDelta = new Vector2(-2f * side, face - (2f * pad));
                    label.rectTransform.anchoredPosition = new Vector2(0f, -(face * 0.5f));
                    if (stacked && i < view.Offers.Count - 1)
                    {
                        y += HeightOf(card) + gap;
                    }
                    else
                    {
                        x += w + 8f;
                    }
                }

                y += HeightOf(_handful);
                if (balsa)
                {
                    y += ModalKit.PlaceLine(_balsaNote, 16f, y, inner, compact ? 0f : 18f) + lineAir;
                }

                y += compact ? 0f : 6f;
            }

            // Pie: botón en 1-30 (rama C: «es la salida normal»), enlace «Rendirse» desde el 31.
            bool restart = view.Footer == StuckFooter.RestartFree;
            _restart.Button.gameObject.SetActive(restart);
            _giveUp.gameObject.SetActive(!restart);
            if (restart)
            {
                FitStretch(_restart, compact, CompactK(_restart, _restartLabel, inner - 24f, CtaOneLineShare, MinTouchDp));
                PlaceButton(_restart, 16f, y, inner);
                UiBuilder.PlaceCtaLabel(_restartLabel.rectTransform, FaceOf(_restart));
                y += HeightOf(_restart) + (compact ? 8f : 12f);
            }
            else
            {
                UiBuilder.Place((RectTransform)_giveUp.transform, (_width - 200f) * 0.5f, y, 200f, 48f);
                y += 48f + (compact ? 2f : 8f);
            }

            Height = y;
        }

        /// <summary>Coloca el panel centrado en horizontal con su borde superior en <paramref name="top"/>.</summary>
        public void Place(float canvasW, float top)
        {
            ModalKit.Size(_panel, _surface, _art, (canvasW - _width) * 0.5f, top, _width, Height);
        }

        // La etiqueta de una oferta (Puñado o Balsa) cabe en dos líneas en un botón de width dp a su tamaño mínimo (12 sp): en el
        // 95 % de la cara de siempre (la de sus sprites por la escala de texto, sin el estirado de un Show anterior), con
        // OfferLabelSideDp a cada lado. Si no, las ofertas van una debajo de otra.
        private static bool OfferFits(ReliefParts card, TextMeshProUGUI label, float width)
        {
            float face = card.Sprites.FaceHeightDp * Mathf.Max(1f, UiBuilder.TextScale);
            return UiBuilder.MinTextHeight(label, width - (2f * OfferLabelSideDp)) <= (face * OfferLabelFace) + 0.01f;
        }

        // Cara de una oferta con la que el botón mide OfferMinHeightDp (ModalKit.HeightOf: el relieve estirado menos
        // ReliefHeightTrimDp), y nunca menos que la de la escala de texto.
        private static float OfferMinFace(ReliefParts card)
        {
            float k = Mathf.Max(1f, UiBuilder.TextScale);
            if (card.Sprites.HeightDp > 0f)
            {
                k = Mathf.Max(k, (OfferMinHeightDp + ReliefHeightTrimDp) / card.Sprites.HeightDp);
            }

            return card.Sprites.FaceHeightDp * k;
        }

        // Alto de cara que necesita la etiqueta de una oferta con textW dp de ancho para el texto (el botón menos el aire de
        // los lados) y pad dp de aire arriba y abajo: con la escala de texto (roomy), a su tamaño (TextMeshPro mide con el
        // autoajuste a su máximo; revisión de L4); al 100 % y en la disposición compacta, a su tamaño mínimo (12 sp), y el
        // autoajuste la deja entre 12 y 14 sp (por la escala) en su caja.
        private static float OfferFaceFor(TextMeshProUGUI label, float textW, float pad, bool roomy)
        {
            if (string.IsNullOrEmpty(label.text))
            {
                return 0f;
            }

            float need = roomy ? label.GetPreferredValues(label.text, textW, float.PositiveInfinity).y : UiBuilder.MinTextHeight(label, textW);
            return Mathf.Ceil(need) + (2f * pad);
        }

        // Disposición compacta: la etiqueta de una oferta va a media anchura si a 12 sp, en textW dp, ocupa como mucho una línea
        // más que las dos de su plantilla (dos más con la escala de texto: al 130 %, en dos columnas ahorran unos 70 dp, lo que
        // pide la rama B del 31; FX16a) y el corte entre palabras (el de TextMeshPro: en cada línea, tantas como quepan) no
        // parte su precio (price, tal cual lo da la tienda; puede llevar espacios, como «COP 3.900»). Solo mide.
        private static bool OfferRowFits(TextMeshProUGUI label, float textW, string price)
        {
            float plain = UiBuilder.MinTextHeight(label, float.PositiveInfinity);
            float lines = UiBuilder.TextScale > 1f ? 2f : 1.5f;
            if (UiBuilder.MinTextHeight(label, textW) > (plain * lines) + 0.5f)
            {
                return false;
            }

            string text = label.text;
            int lineStart = text.LastIndexOf('\n') + 1;
            int at = string.IsNullOrEmpty(price) ? -1 : text.IndexOf(price, lineStart, System.StringComparison.Ordinal);
            if (at < 0)
            {
                return true;
            }

            // Corte voraz de la línea del precio: ningún corte cae dentro del precio.
            int end = text.Length;
            int start = lineStart;
            while (start < end)
            {
                int cut = -1;
                int i = start;
                while (i < end)
                {
                    int j = text.IndexOf(' ', i);
                    if (j < 0 || j > end)
                    {
                        j = end;
                    }

                    if (cut >= 0 && UiBuilder.MinTextWidth(label, text.Substring(start, j - start)) > textW + 0.01f)
                    {
                        break;
                    }

                    cut = j;
                    i = j + 1;
                }

                if (cut >= end)
                {
                    return true;
                }

                if (cut > at && cut < at + price.Length)
                {
                    return false;
                }

                start = cut + 1;
            }

            return true;
        }

        // Disposición compacta: Puñado y Balsa en una fila de rowW dp (sin el hueco entre los dos) con el ancho repartido según lo
        // que pide cada etiqueta (FX16a): de 4 en 4 dp, del 35 % al 65 % para el Puñado, el reparto en que las dos caben
        // (OfferRowFits) con la etiqueta más baja y, a igualdad, el más cercano a la mitad. Solo mide; false si no hay ninguno.
        private static bool OfferRowSplit(TextMeshProUGUI handful, string handfulPrice, TextMeshProUGUI balsa, string balsaPrice, float rowW, float side,
            out float handfulW)
        {
            handfulW = rowW * 0.5f;
            float best = float.PositiveInfinity;
            float bestOff = float.PositiveInfinity;
            bool found = false;
            for (float w = Mathf.Ceil(rowW * 0.35f); w <= rowW * 0.65f; w += 4f)
            {
                float hw = w - (2f * side);
                float bw = rowW - w - (2f * side);
                if (!OfferRowFits(handful, hw, handfulPrice) || !OfferRowFits(balsa, bw, balsaPrice))
                {
                    continue;
                }

                float tall = Mathf.Max(UiBuilder.MinTextHeight(handful, hw), UiBuilder.MinTextHeight(balsa, bw));
                float off = Mathf.Abs(w - (rowW * 0.5f));
                if (tall < best - 0.5f || (tall <= best + 0.5f && off < bestOff))
                {
                    best = Mathf.Min(best, tall);
                    bestOff = off;
                    handfulW = w;
                    found = true;
                }
            }

            return found;
        }

        // Disposición compacta: cuánto se estira a lo alto un CTA (por el alto de sus sprites) para que su etiqueta, a su tamaño
        // mínimo (12 sp por la escala de texto) en textW dp de ancho, quepa con 2 dp de holgura en share de su cara, y nunca
        // con menos de minHeightDp dp de botón (MinK).
        private static float CompactK(ReliefParts cta, TextMeshProUGUI label, float textW, float share, float minHeightDp)
        {
            float k = MinK(cta, minHeightDp);
            float box = share * cta.Sprites.FaceHeightDp;
            if (box <= 0f || string.IsNullOrEmpty(label.text))
            {
                return k;
            }

            return Mathf.Max(k, (Mathf.Ceil(UiBuilder.MinTextHeight(label, textW)) + 2f) / box);
        }

        // Estirado con el que un botón con relieve mide minHeightDp dp (HeightOf: el relieve estirado menos
        // ReliefHeightTrimDp); puede ser menor que 1 (más bajo que sus sprites).
        private static float MinK(ReliefParts cta, float minHeightDp)
        {
            return cta.Sprites.HeightDp > 0f ? (minHeightDp + ReliefHeightTrimDp) / cta.Sprites.HeightDp : 1f;
        }

        // Estira un CTA: en la disposición compacta, k veces el alto de sus sprites (también menos de 1, hasta 48 dp de
        // botón); en la de siempre, como al crearlo (ModalKit.Cta: por la escala de texto).
        private static void FitStretch(ReliefParts cta, bool compact, float k)
        {
            if (compact)
            {
                Stretch(cta, k, MinTouchDp);
                return;
            }

            Stretch(cta, 1f, 0f);
            ModalKit.StretchForTextScale(cta);
        }

        // Estira el relieve del botón a k veces el alto de sus sprites (1: el de siempre), como ModalKit.StretchTo, pero también
        // por debajo de 1 (la disposición compacta), sin que el botón baje de minHeightDp dp.
        private static void Stretch(ReliefParts cta, float k, float minHeightDp)
        {
            RectTransform visual = (RectTransform)cta.Face.parent;
            visual.sizeDelta = new Vector2(visual.sizeDelta.x, cta.Sprites.HeightDp * Mathf.Max(k, MinK(cta, minHeightDp)));
        }

        // Lo que el relieve del botón se estiró a lo alto (como ModalKit, pero también por debajo de 1).
        private static float ScaleOf(ReliefParts parts)
        {
            float h = parts.Sprites.HeightDp;
            float visual = ((RectTransform)parts.Face.parent).sizeDelta.y;
            return h > 0f && visual > 0f ? visual / h : 1f;
        }

        // Alto de la cara visible de un botón con relieve (dp): ModalKit.FaceHeightOf, también por debajo de sus sprites.
        private static float FaceOf(ReliefParts parts)
        {
            return parts.Sprites.FaceHeightDp * ScaleOf(parts);
        }

        // Alto que ocupa un botón con relieve colocado con PlaceButton (dp): ModalKit.HeightOf, también por debajo de sus
        // sprites; nunca menos de 48 dp de área táctil.
        private static float HeightOf(ReliefParts parts)
        {
            return Mathf.Max(MinTouchDp, (parts.Sprites.HeightDp * ScaleOf(parts)) - ReliefHeightTrimDp);
        }

        // Coloca un botón con relieve (su área táctil) en el panel: ModalKit.Place con HeightOf.
        private static void PlaceButton(ReliefParts parts, float x, float y, float w)
        {
            UiBuilder.Place((RectTransform)parts.Button.transform, x, y, w, HeightOf(parts));
        }

        // ModalKit.FitCtaLabel en la disposición compacta: la etiqueta en una línea si cabe a su tamaño mínimo y, si no, en
        // dos, y el botón crece lo que pida la etiqueta, pero sin el estirado de la escala de texto (FX16a).
        private static void FitCtaLabelCompact(ReliefParts cta, TextMeshProUGUI label, float buttonW, float insetDp, float shiftX)
        {
            float width = buttonW - insetDp;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.lineSpacing = 0f;
            bool two = UiBuilder.MinTextWidth(label) > width + 0.5f;
            if (two)
            {
                label.textWrappingMode = TextWrappingModes.Normal;
                label.lineSpacing = CtaTwoLinesSpacing;
            }

            float share = two ? ModalKit.CtaTwoLinesShare : CtaOneLineShare;
            Stretch(cta, CompactK(cta, label, width, share, CompactRescueHeightDp), MinTouchDp);
            float face = FaceOf(cta);
            UiBuilder.PlaceCtaLabel(label.rectTransform, face);
            label.rectTransform.sizeDelta = new Vector2(-insetDp, face * share);
            label.rectTransform.anchoredPosition = new Vector2(shiftX, -(face * (two ? 0.5f : 0.56f)));
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
