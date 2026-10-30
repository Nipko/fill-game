#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.IAP;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El Mercado (bloque B5; 05 §4.1 y §5; 06 §4.7; glosario §7.1 y §10: «Mercado», ambientado en Ver-o-Peso), con arte provisional
    /// por código: una tarjeta de madera a pantalla casi completa, en su propio lienzo encima del inicio (orden
    /// <see cref="SortingOrder"/>, por debajo de la hoja de compra), con el título y la X, el saldo de semillas, las pestañas de
    /// 06 §4.7 (Semillas · Comodines · Cuias · Sin anuncios; las vacías no salen) y una fila por producto: icono, nombre de tienda,
    /// contenido y el botón con el precio local de la tienda (<see cref="IStoreService.PriceOf"/>; la UI nunca calcula un precio).
    /// <list type="bullet">
    /// <item><description>Comprar pasa por <see cref="IStoreService.Purchase"/> con <c>context = shop</c> (la hoja «Compra simulada»
    /// mientras no haya IAP real; D-008). Al terminar, el Cántaro se vacía si era él y las filas se rehacen.</description></item>
    /// <item><description>Cuias: elige la skin puesta (<see cref="SkinService.Select"/>; D-033, Q-M8 (a)); las que faltan salen como
    /// «Premio del mundo · faltan N» y, con la venta de skins encendida (Q-B5-1), con su precio.</description></item>
    /// <item><description>«Restaurar compras» no sale hasta el adaptador real de IAP (Q-E5 (a) de D-033).</description></item>
    /// </list>
    /// Sin asignaciones por fotograma: solo se rehace al abrir, al cambiar de pestaña y tras una compra o una elección.
    /// <para>Más visible desde la observación 24 de la jornada 6 (decisión del dueño del 2026-10-30; «Selva brillante premium»,
    /// D-003, sin texturas nuevas, D-052): las pestañas son botones (una pastilla con borde; la elegida, rellena de hoja con la letra
    /// crema; las demás, en tono suave), la cabecera lleva la banda ámbar de luz cálida y el saldo su placa, cada fila es una tarjeta
    /// con sombra y canto iluminado, y en la pestaña Cuias tocar la tarjeta de una skin abre la vista previa con la cuia en grande, su
    /// nombre y el mismo botón de la fila (se cierra con la X, tocando fuera y con Atrás: <see cref="PressBack"/>).</para>
    /// </summary>
    internal sealed class MarketScreen
    {
        /// <summary>Orden del lienzo: encima del inicio, de la tarjeta del Reto (5) y de las tarjetas de oferta; por debajo de Ajustes (40) y de la hoja de compra (50).</summary>
        public const int SortingOrder = 30;

        /// <summary>Filas que caben como mucho (la pestaña Semillas: el Cántaro y 5 packs).</summary>
        public const int MaxRows = 6;

        /// <summary>Alto de una fila (dp) [P]; se encoge hasta <see cref="MinRowDp"/> en pantallas bajas.</summary>
        public const float RowDp = 64f;

        /// <summary>Alto mínimo de una fila (dp): el del botón (48 dp de área táctil, 07 §7.8) más un margen.</summary>
        public const float MinRowDp = 56f;

        /// <summary>
        /// Alto de una fila de la pestaña Cuias (dp) [P]: la ficha de la skin mide el alto de la fila menos 4, y a 64 dp la cuia casi
        /// no se distinguía (revisión de la observación 4 de la jornada 6). Se encoge hasta <see cref="MinRowDp"/> como las demás.
        /// </summary>
        public const float SkinRowDp = 84f;

        /// <summary>Ancho de la caja de la ficha de una skin en su fila (dp); el alto es el de la fila menos 4 [P].</summary>
        public const float SkinTileDp = 44f;

        /// <summary>Ancho del botón de precio de cada fila (dp) [P].</summary>
        public const float BuyWidthDp = 108f;

        /// <summary>Alto mínimo de una pestaña (dp): el área táctil de 07 §7.8.</summary>
        public const float TabDp = 48f;

        /// <summary>
        /// Hueco mínimo entre las etiquetas de dos pestañas vecinas (dp) [P] (observación 5 de la jornada 6 en el teléfono, 2026-10-28:
        /// al 130 % en es-419, «Semillas» y «Comodines» casi se tocaban con el margen de 3 dp de antes). Lo garantiza
        /// <see cref="TabPadDp"/> a cada lado de cada etiqueta: el texto nunca sale de su caja (se encoge con el autoajuste o va en
        /// más líneas), así que entre dos pestañas quedan al menos dos márgenes.
        /// </summary>
        public const float TabGapDp = 12f;

        /// <summary>Margen interior de cada lado de la etiqueta de una pestaña (dp): la mitad de <see cref="TabGapDp"/>.</summary>
        public const float TabPadDp = TabGapDp * 0.5f;

        /// <summary>Tamaño de la etiqueta de una pestaña (sp, × escala): el tope de su autoajuste; el suelo es 12 sp × escala (02 §12).</summary>
        public const float TabSp = 15f;

        /// <summary>Margen de la pastilla de una pestaña dentro de su área táctil, a los lados (dp): entre dos pastillas quedan 4 dp.</summary>
        public const float TabChipInsetXDp = 2f;

        /// <summary>Margen de la pastilla de una pestaña dentro de su área táctil, arriba y abajo (dp).</summary>
        public const float TabChipInsetYDp = 3f;

        /// <summary>Radio de las esquinas de las pastillas y de las tarjetas (dp).</summary>
        public const float ChipRadiusDp = 12f;

        /// <summary>Margen interior de la tarjeta de cada fila (dp), a los lados.</summary>
        public const float CardPadDp = 6f;

        /// <summary>Alto máximo de la cuia en la vista previa (dp): casi 1:1 con <see cref="ProceduralArt.SkinPreviewPixelsPerU"/>.</summary>
        public const float PreviewCuiaDp = 200f;

        /// <summary>Alto mínimo de la cuia en la vista previa (dp), en pantallas bajas.</summary>
        public const float PreviewCuiaMinDp = 96f;

        /// <summary>Color de la pastilla de la pestaña elegida (el acento: la hoja de 07 §7.2, con la letra crema, 4,8:1).</summary>
        public static readonly Color TabSelectedFill = Palette.FamilyLeaf.Base;

        /// <summary>Color de la pastilla de las demás pestañas (un tono suave de la madera clara).</summary>
        public static readonly Color TabIdleFill = Color.Lerp(Palette.PanelLight, Palette.FamilyWood.Light, 0.22f);

        /// <summary>Color de la letra de la pestaña elegida.</summary>
        public static readonly Color TabSelectedText = Palette.Cream;

        /// <summary>Color de la letra de las demás pestañas.</summary>
        public static readonly Color TabIdleText = Palette.FamilyWood.Deep;

        private readonly ProceduralArt _art;
        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly ReliefParts _close;
        private readonly Image _seedIcon;
        private readonly Image _seedsPillEdge;
        private readonly Image _seedsPill;
        private readonly TextMeshProUGUI _seeds;
        private readonly TextMeshProUGUI _status;
        private readonly Button[] _tabs = new Button[4];
        private readonly TextMeshProUGUI[] _tabLabels = new TextMeshProUGUI[4];
        private readonly Image[] _tabEdges = new Image[4];
        private readonly Image[] _tabChips = new Image[4];
        private readonly RowView[] _rows = new RowView[MaxRows];
        private readonly RectTransform _viewport;
        private readonly RectTransform _content;
        private readonly ScrollRect _scroll;
        private readonly Image _fadeTop;
        private readonly Image _fadeBottom;
        private readonly List<MarketRow> _model = new List<MarketRow>(MaxRows);
        private readonly float[] _tabWidths = new float[4];
        private readonly float[] _tabWords = new float[4];
        private int _tabCols = 4;
        private readonly float[] _rowHeights = new float[MaxRows];
        private readonly MarketDeps _deps;
        private Action? _onClosed;
        private bool _logShown;
        private bool _keepScroll; // el próximo Layout conserva la posición de la lista (un Refresh sin cambio de pestaña)
        private MarketTab _laidTab;
        private bool _laidOut;
        private int _visibleRows = -1; // filas con botón a la vista en el último AF_LAYOUT (máscara de bits; sonda de desarrollo)

        // La vista previa de una skin (observación 24 de la jornada 6), encima de la tarjeta, en el mismo lienzo.
        private readonly RectTransform _preview;
        private readonly RectTransform _previewPanel;
        private readonly Image _previewSurface;
        private readonly Image _previewHeader;
        private readonly TextMeshProUGUI _previewTitle;
        private readonly ReliefParts _previewClose;
        private readonly Button _previewOutside;
        private readonly Image _previewCuia;
        private readonly TextMeshProUGUI _previewDetail;
        private readonly ReliefParts _previewCta;
        private readonly TextMeshProUGUI _previewCtaLabel;
        private string? _previewId;

        public MarketScreen(Transform host, ProceduralArt art, MarketDeps deps)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            _deps = deps ?? throw new ArgumentNullException(nameof(deps));
            _canvas = new OverlayCanvas("MarketCanvas", host, SortingOrder);
            ModalKit.Dim("Dim", _canvas.Root, 0.6f);
            _panel = ModalKit.Card("Panel", _canvas.Root, art, out _surface);
            // Observación 24 de la jornada 6: la cabecera con la banda ámbar (luz cálida) y el título en tinta, en vez de la madera oscura.
            _title = ModalKit.Header(_panel, art, MarketText.Title, out _header, Palette.UiAmber);
            _close = ModalKit.Close("CloseButton", _panel, art, () => Close());
            // El saldo en su placa (madera clara con canto), como un contador del juego.
            _seedsPillEdge = UiBuilder.Sliced("SeedsPillEdge", _panel, art.RoundedRect, Palette.FamilyWood.Light);
            _seedsPill = UiBuilder.Sliced("SeedsPill", _panel, art.RoundedRect, Palette.PanelLight);
            _seedsPillEdge.pixelsPerUnitMultiplier = ChipPpuMultiplier;
            _seedsPill.pixelsPerUnitMultiplier = ChipPpuMultiplier;
            _seedIcon = UiBuilder.Image("SeedIcon", _panel, art.IconSeed, Color.white);
            _seedIcon.preserveAspect = true;
            _seeds = UiBuilder.Label("Seeds", _panel, TextStyle.PlainBold, string.Empty, 18f, Palette.FamilyWood.Deep, TextAlignmentOptions.Left);
            // F3-5: «Las compras no están disponibles ahora» cuando la tienda no vende (backend apagado o ausente).
            _status = UiBuilder.Label("Status", _panel, TextStyle.PlainBold, string.Empty, 13f, Palette.UiRedWarning, TextAlignmentOptions.Left);
            _status.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_status, UiBuilder.MinTextSp, 13f);
            _status.gameObject.SetActive(false);
            for (int i = 0; i < _tabs.Length; i++)
            {
                MarketTab tab = MarketModel.AllTabs[i];
                _tabs[i] = UiBuilder.BareButton("Tab" + tab, _panel, () => SelectTab(tab));
                // Observación 24 de la jornada 6: cada pestaña, una pastilla con borde que se lee como botón (no solo texto subrayado).
                _tabEdges[i] = UiBuilder.Sliced("ChipEdge", _tabs[i].transform, art.RoundedRect, Palette.FamilyWood.Light);
                _tabChips[i] = UiBuilder.Sliced("Chip", _tabs[i].transform, art.RoundedRect, TabIdleFill);
                _tabEdges[i].pixelsPerUnitMultiplier = ChipPpuMultiplier;
                _tabChips[i].pixelsPerUnitMultiplier = ChipPpuMultiplier;
                _tabLabels[i] = UiBuilder.Label("Label", _tabs[i].transform, TextStyle.PlainBold, string.Empty, 15f, TabIdleText, TextAlignmentOptions.Center);
                int tabIndex = i;
                A11yLabels.Set(_tabs[i], () => _tabLabels[tabIndex].text, () => Tab == MarketModel.AllTabs[tabIndex]); // TalkBack: «Cuias, seleccionada»
                UiBuilder.Stretch(_tabLabels[i].rectTransform);
                // D-033, punto 11: al 130 % «Sin anuncios» no cabía en una línea de su cuarto de fila: va en dos, sin partir palabras.
                _tabLabels[i].textWrappingMode = TextWrappingModes.Normal;
                UiBuilder.AutoSize(_tabLabels[i], UiBuilder.MinTextSp, TabSp);
            }

            // Las filas van en un visor que se desplaza si no caben (D-033, punto 11: al 130 % las filas crecen con su texto), con
            // los fundidos de Ajustes en los bordes mientras quede lista por ese lado.
            _viewport = UiBuilder.Rect("Rows", _panel);
            _viewport.gameObject.AddComponent<RectMask2D>();
            _viewport.gameObject.AddComponent<HitArea>().raycastTarget = true; // el arrastre entre filas también desplaza
            _content = UiBuilder.Rect("Content", _viewport);
            _scroll = _viewport.gameObject.AddComponent<FlingStopScrollRect>();
            _scroll.viewport = _viewport;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.vertical = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            for (int i = 0; i < _rows.Length; i++)
            {
                int index = i;
                _rows[i] = new RowView(_content, art, i, () => PressRow(index), () => OpenPreview(index));
            }

            _fadeTop = UiBuilder.Image("FadeTop", _viewport, art.ScrollFade(true), Color.white);
            _fadeBottom = UiBuilder.Image("FadeBottom", _viewport, art.ScrollFade(false), Color.white);
            _scroll.onValueChanged.AddListener(_ =>
            {
                UpdateFades();
                ProbeScrolled();
            });

            // La vista previa de una skin: velo que cierra al tocarlo, tarjeta con el nombre, la X, la cuia en grande, su línea y el
            // botón de la fila (observación 24 de la jornada 6). Hermana de la tarjeta, encima de ella.
            _preview = UiBuilder.Rect("Preview", _canvas.Root);
            UiBuilder.Stretch(_preview);
            Image previewDim = ModalKit.Dim("Dim", _preview, 0.55f);
            previewDim.raycastTarget = false;
            _previewOutside = UiBuilder.BareButton("Outside", _preview, () => ClosePreview());
            UiBuilder.Stretch((RectTransform)_previewOutside.transform);
            A11yLabels.Set(_previewOutside, () => A11yText.Close);
            _previewPanel = ModalKit.Card("PreviewPanel", _preview, art, out _previewSurface);
            _previewPanel.gameObject.AddComponent<HitArea>().raycastTarget = true; // tocar la tarjeta no la cierra
            _previewTitle = ModalKit.Header(_previewPanel, art, string.Empty, out _previewHeader, Palette.UiAmber);
            _previewClose = ModalKit.Close("CloseButton", _previewPanel, art, () => ClosePreview());
            _previewCuia = UiBuilder.Image("Cuia", _previewPanel, null, Color.white);
            _previewCuia.preserveAspect = true;
            _previewDetail = UiBuilder.Label("Detail", _previewPanel, TextStyle.Body, string.Empty, 14f, Palette.FamilyWood.Base, TextAlignmentOptions.Center);
            _previewDetail.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_previewDetail, UiBuilder.MinTextSp, 14f);
            _previewCta = ModalKit.Cta("BuyButton", _previewPanel, art.CtaLeaf, TextStyle.CtaLeaf, string.Empty, 17f, () => PressPreview(), out _previewCtaLabel);
            A11yLabels.Set(_previewCta.Button, () => A11yText.Append(_previewTitle.text, _previewCtaLabel.text));
            _preview.gameObject.SetActive(false);

            _canvas.Canvas.gameObject.SetActive(false);
        }

        // Las pastillas usan RoundedRect (radio de 20 px a 100 px por unidad): este multiplicador deja sus esquinas en ChipRadiusDp.
        private static float ChipPpuMultiplier => ProceduralArt.RoundedRectRadiusPx / ChipRadiusDp;

        /// <summary>Está abierto.</summary>
        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Lienzo (capturas y tests).</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>Pestaña a la vista.</summary>
        public MarketTab Tab { get; private set; }

        /// <summary>Filas de la pestaña a la vista (tests).</summary>
        public IReadOnlyList<MarketRow> Rows => _model;

        /// <summary>
        /// La Mochila de inicio está entre las filas a la vista pero ya no está a la venta (su temporizador llegó a cero, se pasó el 20
        /// o se compró): quien lleva el Mercado la quita con <see cref="Refresh"/> (T-L9; 05 §4.1). False con el Mercado cerrado.
        /// </summary>
        public bool StarterStale
        {
            get
            {
                if (!Visible)
                {
                    return false;
                }

                for (int i = 0; i < _model.Count; i++)
                {
                    if (string.Equals(_model[i].Id, StoreCatalog.Starter, StringComparison.Ordinal))
                    {
                        return !_deps.Offers.StarterAvailable(_deps.MaxLevel());
                    }
                }

                return false;
            }
        }

        /// <summary>Texto del saldo de semillas.</summary>
        public string SeedsText => _seeds.text;

        /// <summary>La línea «Las compras no están disponibles ahora» (F3-5), o null si no se ve.</summary>
        public string? StatusText => _status.gameObject.activeSelf ? _status.text : null;

        /// <summary>Botón de cierre.</summary>
        public Button CloseButton => _close.Button;

        /// <summary>Se vio la pestaña <paramref name="tab"/> (las vacías no salen).</summary>
        public bool TabShown(MarketTab tab) => _tabs[(int)tab].gameObject.activeSelf;

        /// <summary>Etiqueta de la pestaña <paramref name="tab"/> (tests del hueco entre pestañas).</summary>
        internal TextMeshProUGUI TabLabel(MarketTab tab) => _tabLabels[(int)tab];

        /// <summary>Botón (área táctil) de la pestaña <paramref name="tab"/> (tests).</summary>
        internal Button TabButton(MarketTab tab) => _tabs[(int)tab];

        /// <summary>La pastilla de la pestaña <paramref name="tab"/> (su relleno; tests).</summary>
        internal Image TabChip(MarketTab tab) => _tabChips[(int)tab];

        /// <summary>El borde de la pastilla de la pestaña <paramref name="tab"/> (tests).</summary>
        internal Image TabChipEdge(MarketTab tab) => _tabEdges[(int)tab];

        /// <summary>La vista previa de una skin está abierta.</summary>
        public bool PreviewOpen => _preview.gameObject.activeSelf;

        /// <summary>La skin de la vista previa, o null.</summary>
        public string? PreviewSkinId => _previewId;

        /// <summary>El nombre de la vista previa (tests).</summary>
        public string PreviewTitle => _previewTitle.text;

        /// <summary>La cuia en grande de la vista previa (tests).</summary>
        public Image PreviewCuia => _previewCuia;

        /// <summary>El botón de la vista previa (comprar, poner o «Puesta»; tests).</summary>
        public Button PreviewButton => _previewCta.Button;

        /// <summary>Lo que dice el botón de la vista previa (tests).</summary>
        public string PreviewButtonText => _previewCtaLabel.text;

        /// <summary>La X de la vista previa (tests).</summary>
        public Button PreviewCloseButton => _previewClose.Button;

        /// <summary>El velo de la vista previa: tocarlo la cierra (tests).</summary>
        public Button PreviewOutsideButton => _previewOutside;

        /// <summary>La tarjeta de la vista previa (tests).</summary>
        public RectTransform PreviewPanel => _previewPanel;

        /// <summary>El área que abre la vista previa de la fila <paramref name="index"/> (pestaña Cuias; tests).</summary>
        public Button RowCardButton(int index) => _rows[index].CardTap;

        /// <summary>La versión de las notas de compra con la que se rehízo por última vez (el arranque lo rehace si cambian; F3-5).</summary>
        public int NotesShown { get; private set; } = -1;

        /// <summary>Alto del lienzo (dp; tests).</summary>
        public float AreaHeight => _canvas.Height;

        /// <summary>Ancho del lienzo (dp; tests).</summary>
        public float AreaWidth => _canvas.Width;

        /// <summary>Margen de la zona segura de arriba (dp; tests).</summary>
        public float SafeTopDp => _canvas.SafeTop;

        /// <summary>Margen de la zona segura de abajo (dp; tests).</summary>
        public float SafeBottomDp => _canvas.SafeBottom;

        /// <summary>Las filas no caben y se desplazan (D-033, punto 11).</summary>
        public bool RowsScroll => _scroll.vertical;

        /// <summary>Botón de la fila <paramref name="index"/> (tests).</summary>
        public Button RowButton(int index) => _rows[index].Cta.Button;

        /// <summary>Texto del botón de la fila <paramref name="index"/>.</summary>
        public string RowButtonText(int index) => _rows[index].CtaLabel.text;

        /// <summary>Nombre de la fila <paramref name="index"/>.</summary>
        public string RowNameText(int index) => _rows[index].Name.text;

        /// <summary>Línea de contenido de la fila <paramref name="index"/>.</summary>
        public string RowDetailText(int index) => _rows[index].Detail.text;

        /// <summary>Nota de la fila <paramref name="index"/> (el temporizador de la Mochila).</summary>
        public string RowNoteText(int index) => _rows[index].Note.text;

        /// <summary>El botón de la fila <paramref name="index"/> se ve.</summary>
        public bool RowButtonShown(int index) => _rows[index].Cta.Button.gameObject.activeSelf;

        /// <summary>El icono de la fila <paramref name="index"/> (tests: la cuia de la skin, no un círculo liso).</summary>
        public Image RowIcon(int index) => _rows[index].Icon;

        /// <summary>Se compró o se eligió algo: el arranque refresca el inicio (saldo, Cántaro) y las cuias (skin).</summary>
        public event Action<PurchaseResult>? Purchased;

        /// <summary>Se puso otra skin.</summary>
        public event Action<string>? SkinSelected;

        /// <summary>Se abrió la vista previa de una skin, con su nombre (TalkBack lo anuncia).</summary>
        public event Action<string>? PreviewOpened;

        /// <summary>
        /// Se abrió o cambió de pestaña (para <c>AF_LAYOUT</c>); en desarrollo, también cuando al desplazar la lista cambian las filas
        /// que se ven.
        /// </summary>
        public event Action? Shown;

        /// <summary>Abre el Mercado en <paramref name="tab"/> (si no tiene nada, en Semillas) y llama a <paramref name="onClosed"/> al cerrarlo.</summary>
        public void Open(MarketTab tab, Action? onClosed = null)
        {
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.StoreOpen);
            _onClosed = onClosed;
            _canvas.Canvas.gameObject.SetActive(true);
            _canvas.Measure();
            Tab = MarketModel.TabVisible(tab, _deps.Offers, _deps.MaxLevel()) ? tab : MarketTab.Seeds;
            _logShown = true;
            _laidOut = false; // al abrir, la lista empieza arriba
            Refresh();
            Shown?.Invoke();
        }

        /// <summary>Cierra el Mercado (la X y Atrás).</summary>
        public void Close()
        {
            if (!Visible)
            {
                return;
            }

            HidePreview();
            _canvas.Canvas.gameObject.SetActive(false);
            Action? done = _onClosed;
            _onClosed = null;
            done?.Invoke();
        }

        /// <summary>
        /// Atrás de Android con el Mercado delante (desde <c>PressMarketBack</c>): con la vista previa abierta, la cierra; si no, cierra
        /// el Mercado como su X. Devuelve si cerró algo.
        /// </summary>
        public bool PressBack()
        {
            if (!Visible)
            {
                return false;
            }

            if (PreviewOpen)
            {
                AF.Audio.AudioCues.Play(AF.Audio.SfxId.UiBack);
                ClosePreview();
                return true;
            }

            Close();
            return true;
        }

        /// <summary>
        /// Abre la vista previa de la skin de la fila <paramref name="index"/> (pestaña Cuias; el toque en su tarjeta): la cuia en
        /// grande, su nombre y el botón de la fila (observación 24 de la jornada 6). False si no es una fila de skin.
        /// </summary>
        public bool OpenPreview(int index)
        {
            if (!Visible || Tab != MarketTab.Cuias || index < 0 || index >= _model.Count)
            {
                return false;
            }

            _previewId = _model[index].Id;
            _preview.gameObject.SetActive(true);
            _preview.SetAsLastSibling();
            FillPreview(_model[index], _deps.Store.IsAvailable && !_deps.PurchasesBlocked);
            PreviewOpened?.Invoke(_previewTitle.text);
            Shown?.Invoke();
            return true;
        }

        /// <summary>Cierra la vista previa (la X, el toque fuera y Atrás) y suelta su cuia en grande.</summary>
        public void ClosePreview()
        {
            if (!PreviewOpen)
            {
                return;
            }

            HidePreview();
            Shown?.Invoke();
        }

        private void HidePreview()
        {
            _previewId = null;
            if (_preview.gameObject.activeSelf)
            {
                _preview.gameObject.SetActive(false);
            }

            _previewCuia.sprite = null;
            _art.ReleaseSkinPreview(); // por la memoria: solo vive mientras se ve (observación 6 de la jornada 6)
        }

        // El botón de la vista previa hace lo mismo que el de su fila.
        private bool PressPreview()
        {
            int index = PreviewRowIndex();
            return index >= 0 && PressRow(index);
        }

        private int PreviewRowIndex()
        {
            if (_previewId == null)
            {
                return -1;
            }

            for (int i = 0; i < _model.Count; i++)
            {
                if (string.Equals(_model[i].Id, _previewId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private void FillPreview(MarketRow row, bool storeOn)
        {
            _previewTitle.text = MarketText.SkinName(row.Id);
            _previewCuia.sprite = _art.SkinPreview(CuiaSkin.For(row.Id));
            _previewCuia.color = Color.white;
            _previewDetail.text = Detail(row, true);
            bool hasButton = row.Action != MarketAction.None;
            _previewCta.Button.gameObject.SetActive(hasButton);
            if (hasButton)
            {
                FillCta(_previewCta, _previewCtaLabel, row, storeOn);
            }

            LayoutPreview();
        }

        // La tarjeta de la vista previa, centrada en la zona segura: la cabecera con el nombre y la X, la cuia lo más grande que quepa
        // (entre PreviewCuiaMinDp y PreviewCuiaDp), su línea y el botón.
        private void LayoutPreview()
        {
            float w = Mathf.Min(320f, _canvas.Width - 32f);
            float inner = w - 32f;
            float top = _canvas.SafeTop + 8f;
            float available = _canvas.Height - top - _canvas.SafeBottom - 8f;
            UiBuilder.Place(_previewHeader.rectTransform, 16f, 16f, inner - 52f, 40f);
            UiBuilder.Place((RectTransform)_previewClose.Button.transform, w - 16f - ModalKit.CloseDp, 12f, ModalKit.CloseDp, ModalKit.CloseDp);
            float detailH = _previewDetail.text.Length > 0 ? Mathf.Ceil(UiBuilder.MinTextHeight(_previewDetail, inner)) : 0f;
            bool cta = _previewCta.Button.gameObject.activeSelf;
            float ctaH = cta ? ModalKit.HeightOf(_previewCta) : 0f;
            float fixedH = 68f + 12f + (detailH > 0f ? detailH + 8f : 0f) + (cta ? ctaH + 4f : 0f) + 16f;
            float cuiaH = Mathf.Clamp(Mathf.Floor(available - fixedH), PreviewCuiaMinDp, PreviewCuiaDp);
            Sprite? sprite = _previewCuia.sprite;
            float aspect = sprite != null && sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 0.43f;
            float cuiaW = Mathf.Floor(cuiaH * aspect);
            float y = 68f;
            UiBuilder.Place(_previewCuia.rectTransform, (w - cuiaW) * 0.5f, y, cuiaW, cuiaH);
            y += cuiaH + 12f;
            if (detailH > 0f)
            {
                UiBuilder.Place(_previewDetail.rectTransform, 16f, y, inner, detailH);
                y += detailH + 8f;
            }

            _previewDetail.gameObject.SetActive(detailH > 0f);
            if (cta)
            {
                float ctaW = Mathf.Min(inner, 200f);
                ModalKit.Place(_previewCta, (w - ctaW) * 0.5f, y, ctaW);
                y += ctaH + 4f;
            }

            float h = y + 16f;
            ModalKit.Size(_previewPanel, _previewSurface, _art, (_canvas.Width - w) * 0.5f, top + Mathf.Max(0f, (available - h) * 0.5f), w, h, sharedSurface: true);
        }

        /// <summary>Cambia a la pestaña <paramref name="tab"/> (si se ve).</summary>
        public void SelectTab(MarketTab tab)
        {
            if (!Visible || !MarketModel.TabVisible(tab, _deps.Offers, _deps.MaxLevel()))
            {
                return;
            }

            Tab = tab;
            _logShown = true;
            Refresh();
            Shown?.Invoke();
        }

        /// <summary>
        /// Botones visibles para <c>AF_LAYOUT</c> (sonda de desarrollo): la X, las pestañas y el botón de cada fila que el visor no
        /// recorta del todo (las de debajo del corte salen cuando se desplaza la lista hasta ellas).
        /// </summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            if (!Visible)
            {
                return;
            }

            if (PreviewOpen)
            {
                // Con la vista previa delante, solo lo suyo: lo de detrás no se puede tocar.
                ids.Add("market_preview_close");
                rects.Add((RectTransform)_previewClose.Button.transform);
                if (_previewCta.Button.gameObject.activeSelf)
                {
                    ids.Add("market_preview_cta");
                    rects.Add((RectTransform)_previewCta.Button.transform);
                }

                return;
            }

            ids.Add("market_close");
            rects.Add((RectTransform)_close.Button.transform);
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i].gameObject.activeSelf)
                {
                    ids.Add("market_tab_" + i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    rects.Add((RectTransform)_tabs[i].transform);
                }
            }

            _visibleRows = VisibleRowMask();
            for (int i = 0; i < _model.Count && i < _rows.Length; i++)
            {
                if ((_visibleRows & (1 << i)) != 0)
                {
                    ids.Add("market_row_" + _model[i].Id);
                    rects.Add((RectTransform)_rows[i].Cta.Button.transform);
                    if (_rows[i].CardTap.gameObject.activeSelf)
                    {
                        ids.Add("market_card_" + _model[i].Id); // la tarjeta de la skin: abre la vista previa
                        rects.Add(_rows[i].IconRect);
                    }
                }
            }
        }

        // Las filas cuyo botón se ve (bit i: la fila i): con botón y sin que el visor lo recorte del todo (como el lector; las de
        // debajo del corte no se pueden tocar).
        private int VisibleRowMask()
        {
            int mask = 0;
            for (int i = 0; i < _model.Count && i < _rows.Length; i++)
            {
                Button button = _rows[i].Cta.Button;
                if (button.gameObject.activeInHierarchy && !A11yScroll.ClippedOut((RectTransform)button.transform))
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        // Desarrollo: al desplazar la lista, si cambian las filas que se ven, otra línea AF_LAYOUT con las de ahora.
        private void ProbeScrolled()
        {
            if (DevProbe.Enabled && Visible && _visibleRows >= 0 && VisibleRowMask() != _visibleRows)
            {
                Shown?.Invoke();
            }
        }

        /// <summary>Lo que hace el botón de la fila <paramref name="index"/> (el toque; los tests lo llaman igual).</summary>
        public bool PressRow(int index)
        {
            if (!Visible || index < 0 || index >= _model.Count)
            {
                return false;
            }

            MarketRow row = _model[index];
            switch (row.Action)
            {
                case MarketAction.Buy:
                    return Buy(row.Id);
                case MarketAction.Equip:
                    if (_deps.Skins != null && _deps.Skins.Select(row.Id))
                    {
                        SkinSelected?.Invoke(row.Id);
                        Refresh();
                        return true;
                    }

                    return false;
                default:
                    return false;
            }
        }

        /// <summary>Rehace las pestañas y las filas con el estado de ahora.</summary>
        public void Refresh()
        {
            if (!Visible)
            {
                return;
            }

            int maxLevel = _deps.MaxLevel();
            if (!MarketModel.TabVisible(Tab, _deps.Offers, maxLevel))
            {
                Tab = MarketTab.Seeds;
            }

            // Un Refresh que no cambia de pestaña (comprar, equipar, una nota o el estado de la tienda) deja la lista donde estaba;
            // abrir el Mercado o cambiar de pestaña la lleva arriba.
            _keepScroll = _laidOut && _laidTab == Tab;
            MarketModel.Build(Tab, _deps.Offers, _deps.Skins, maxLevel, _deps.CurrentWorld(), _model);
            _seeds.text = Loc.Number(_deps.Offers.Ledger.Wallet.Seeds);
            bool storeOn = _deps.Store.IsAvailable && !_deps.PurchasesBlocked;
            _status.gameObject.SetActive(!_deps.Store.IsAvailable);
            _status.text = _deps.Store.IsAvailable ? string.Empty : MarketText.StoreUnavailable;
            NotesShown = _deps.Notes?.Version ?? 0;
            for (int i = 0; i < _tabs.Length; i++)
            {
                MarketTab tab = MarketModel.AllTabs[i];
                bool shown = MarketModel.TabVisible(tab, _deps.Offers, maxLevel);
                _tabs[i].gameObject.SetActive(shown);
                _tabLabels[i].text = MarketText.Tab(tab);
                bool selected = tab == Tab;
                // Observación 24 de la jornada 6: la elegida, rellena con el acento y la letra crema; las demás, en tono suave.
                _tabLabels[i].color = selected ? TabSelectedText : TabIdleText;
                _tabLabels[i].fontStyle = FontStyles.Normal;
                _tabChips[i].color = selected ? TabSelectedFill : TabIdleFill;
                _tabEdges[i].color = selected ? (Color)Palette.FamilyLeaf.Deep : (Color)Palette.FamilyWood.Light;
            }

            for (int i = 0; i < _rows.Length; i++)
            {
                RowView view = _rows[i];
                if (i >= _model.Count)
                {
                    view.Root.gameObject.SetActive(false);
                    continue;
                }

                view.Root.gameObject.SetActive(true);
                Fill(view, _model[i], storeOn);
            }

            if (_logShown)
            {
                // af_iap_offer_shown (context = shop) de lo que se puede comprar en la pestaña, al abrirla (no tras cada compra).
                _logShown = false;
                for (int i = 0; i < _model.Count; i++)
                {
                    if (_model[i].Action == MarketAction.Buy && storeOn)
                    {
                        _deps.OfferShown?.Invoke(_model[i].Id); // solo lo que se puede comprar ahora
                    }
                }
            }

            Layout();
            if (PreviewOpen)
            {
                // Tras poner la skin o comprarla, la vista previa dice lo de ahora; si su fila ya no está, se cierra.
                int index = Tab == MarketTab.Cuias ? PreviewRowIndex() : -1;
                if (index < 0)
                {
                    HidePreview();
                }
                else
                {
                    FillPreview(_model[index], storeOn);
                }
            }
        }

        private bool Buy(string sku)
        {
            IStoreService store = _deps.Store;
            if (!store.IsAvailable || _deps.PurchasesBlocked || store.IsPurchasing)
            {
                return false;
            }

            if (string.Equals(sku, StoreCatalog.Starter, StringComparison.Ordinal) && !_deps.Offers.StarterAvailable(_deps.MaxLevel()))
            {
                // T-L9 (05 §4.1): con el temporizador a cero la Mochila ya no se vende, aunque su fila siguiera a la vista.
                Refresh();
                return false;
            }

            PurchaseRequest request = string.Equals(sku, StoreCatalog.Piggy, StringComparison.Ordinal)
                ? _deps.Offers.Piggy.Request(PurchaseContext.Shop, _deps.MaxLevel() + 1)
                : new PurchaseRequest(sku, PurchaseContext.Shop);
            store.Purchase(request, result =>
            {
                _deps.Offers.Piggy.OnPurchaseResult(result);
                Purchased?.Invoke(result);
                Refresh();
            });
            return true;
        }

        private void Fill(RowView view, MarketRow row, bool storeOn)
        {
            bool skin = Tab == MarketTab.Cuias;
            view.Name.text = skin ? MarketText.SkinName(row.Id) : MarketText.ProductName(row.Id);
            view.Detail.text = Detail(row, skin);
            // F3-5: la nota de compra («Pendiente de confirmar» o «No se pudo confirmar la compra») gana al temporizador de la Mochila.
            PurchaseNoteKind noteKind = _deps.Notes?.Of(row.Id) ?? PurchaseNoteKind.None;
            string? purchaseNote = MarketText.PurchaseNote(noteKind);
            view.Note.text = purchaseNote ?? (!skin && row.Id == StoreCatalog.Starter && row.Number > 0 ? MarketText.StarterLeft(row.Number) : string.Empty);
            view.Note.color = noteKind == PurchaseNoteKind.Failed ? (Color)Palette.UiRedWarning : (Color)Palette.FamilyWood.Base;
            // Observación 4 de la jornada 6: cada skin con su cuia (la del tablero en pequeño, ProceduralArt.SkinThumb), no un círculo
            // de su color; el Cántaro con su tinaja (la del inicio). La miniatura lleva sus colores: sin tinte.
            view.Icon.sprite = IconOf(row.Id, skin);
            view.Icon.color = Color.white;
            view.TallIcon = skin;
            view.SkinName = skin ? view.Name.text : null; // TalkBack: «Cuia de Santarém, Poner» y no solo «Poner»
            view.CardTap.gameObject.SetActive(skin); // la tarjeta de la skin abre la vista previa (observación 24 de la jornada 6)

            bool hasButton = row.Action != MarketAction.None;
            view.Cta.Button.gameObject.SetActive(hasButton);
            if (!hasButton)
            {
                return;
            }

            FillCta(view.Cta, view.CtaLabel, row, storeOn);
        }

        // El botón de una fila o de la vista previa: «Poner», «Puesta» (en gris) o el precio que da la tienda.
        private void FillCta(ReliefParts cta, TextMeshProUGUI label, MarketRow row, bool storeOn)
        {
            switch (row.Action)
            {
                case MarketAction.Equip:
                    label.text = MarketText.SkinEquip;
                    UiBuilder.SetEnabled(cta.Button, true);
                    break;
                case MarketAction.Equipped:
                    label.text = MarketText.SkinEquipped;
                    UiBuilder.SetEnabled(cta.Button, false);
                    break;
                default:
                    label.text = _deps.Store.PriceOf(row.Id).ToString();
                    // Mientras una compra se confirma («Confirmando…»), ningún precio responde: no se abre otra (IsPurchasing).
                    UiBuilder.SetEnabled(cta.Button, storeOn && row.Action == MarketAction.Buy && !_deps.Store.IsPurchasing);
                    break;
            }
        }

        private string Detail(MarketRow row, bool skin)
        {
            if (skin)
            {
                if (row.Action == MarketAction.Equip || row.Action == MarketAction.Equipped)
                {
                    return string.Empty;
                }

                return MarketText.WorldPrize(row.Number);
            }

            StoreProduct product = _deps.Offers.Ledger.Catalog.Get(row.Id);
            switch (row.Id)
            {
                case StoreCatalog.Piggy:
                    return row.Action == MarketAction.BuyLocked
                        ? MarketText.PiggyLocked(_deps.Offers.Piggy.MinBuy)
                        : (row.Number >= row.Number2 ? MarketText.PiggyFull : MarketText.PiggyContent(row.Number, row.Number2));
                case StoreCatalog.Starter:
                    return MarketText.StarterContent(product.Content.Seeds, product.Content.Stock.Get(BoosterKind.ExtraCuia), product.Content.Stock.Get(BoosterKind.Hint));
                case StoreCatalog.InfiniteCanoes1h:
                    return MarketText.CanoesContent(row.Number);
                case StoreCatalog.NoAds:
                    return row.Action == MarketAction.None ? MarketText.NoAdsOwned : MarketText.NoAdsContent(product.Content.Seeds);
                default:
                    return MarketText.SeedsAmount(row.Number);
            }
        }

        private Sprite? IconOf(string id, bool skin)
        {
            if (skin)
            {
                return _art.SkinThumb(CuiaSkin.For(id));
            }

            switch (id)
            {
                case StoreCatalog.Piggy: return _art.DockJar;
                case StoreCatalog.Starter: return _art.IconCuiaPlus;
                case StoreCatalog.InfiniteCanoes1h: return _art.IconInfinite;
                case StoreCatalog.NoAds: return _art.IconVideo;
                default: return _art.IconSeed;
            }
        }

        private void Layout()
        {
            float w = Mathf.Min(360f, _canvas.Width) - 16f;
            float top = _canvas.SafeTop + 8f;
            float available = _canvas.Height - top - _canvas.SafeBottom - 8f;
            float inner = w - 32f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner - 52f, 40f);
            UiBuilder.Place((RectTransform)_close.Button.transform, w - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
            y += 52f;
            UiBuilder.Place(_seedIcon.rectTransform, 16f, y + 2f, 24f, 24f);
            UiBuilder.Place(_seeds.rectTransform, 46f, y, inner - 30f, 28f);
            float seedsW = Mathf.Min(inner, 30f + Mathf.Ceil(_seeds.GetPreferredValues(_seeds.text).x) + 14f);
            UiBuilder.Place(_seedsPillEdge.rectTransform, 10f, y - 2f, seedsW + 2f, 32f);
            UiBuilder.Place(_seedsPill.rectTransform, 11.5f, y - 0.5f, seedsW - 1f, 29f);
            y += 36f;
            if (_status.gameObject.activeSelf)
            {
                UiBuilder.Place(_status.rectTransform, 16f, y, inner, 34f);
                y += 38f;
            }

            int shownTabs = 0;
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i].gameObject.activeSelf)
                {
                    shownTabs++;
                }
            }

            // En una fila o, si sus palabras no caben juntas (320 dp con la letra al 130 %; observación 8 de la jornada 6), en
            // una rejilla de dos columnas, fila a fila.
            float tabH = TabWidths(inner, shownTabs);
            float x = 16f;
            int placed = 0;
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (!_tabs[i].gameObject.activeSelf)
                {
                    continue;
                }

                if (placed > 0 && placed % _tabCols == 0)
                {
                    x = 16f;
                    y += tabH;
                }

                UiBuilder.Place((RectTransform)_tabs[i].transform, x, y, _tabWidths[i], tabH);
                UiBuilder.Place(_tabEdges[i].rectTransform, TabChipInsetXDp, TabChipInsetYDp, _tabWidths[i] - (2f * TabChipInsetXDp), tabH - (2f * TabChipInsetYDp));
                UiBuilder.Place(_tabChips[i].rectTransform, TabChipInsetXDp + 1.5f, TabChipInsetYDp + 1.5f, _tabWidths[i] - (2f * TabChipInsetXDp) - 3f, tabH - (2f * TabChipInsetYDp) - 3f);
                UiBuilder.Place(_tabLabels[i].rectTransform, TabPadDp, 0f, _tabWidths[i] - (2f * TabPadDp), tabH);
                x += _tabWidths[i];
                placed++;
            }

            ShareTabFontSize();
            y += tabH + 8f;
            int count = Math.Min(_model.Count, MaxRows);
            float maxRowH = Tab == MarketTab.Cuias ? SkinRowDp : RowDp; // las skins, más altas: su ficha es lo que se elige
            float rowH = maxRowH;
            if (count > 0)
            {
                // Cada fila, al alto que pidan sus textos y su botón (D-033, punto 11: al 130 % el contenido va en las líneas que
                // pida); las que caben se reparten el sitio como siempre, entre MinRowDp y RowDp.
                float room = available - y - 16f;
                for (int i = 0; i < count; i++)
                {
                    _rowHeights[i] = _rows[i].Needed(inner);
                }

                rowH = Mathf.Clamp(Mathf.Floor(room / count), MinRowDp, maxRowH);

                // Si no caben a su alto de siempre, las que no lo necesitan bajan hasta su mínimo (nunca por debajo de lo que piden).
                while (rowH > MinRowDp && RowsHeight(count, rowH) > room)
                {
                    rowH = Mathf.Max(MinRowDp, rowH - 1f);
                }
            }

            float contentH = 0f;
            for (int i = 0; i < count; i++)
            {
                float rowHi = Mathf.Max(rowH, _rowHeights[i]);
                _rowHeights[i] = rowHi;
                _rows[i].Place(0f, contentH, inner, rowHi);
                contentH += rowHi;
            }

            // Si ni así caben, el visor se desplaza y acaba a media fila (como Ajustes: un corte limpio no parece el final).
            float rowsRoom = Mathf.Max(MinRowDp, available - y - 16f);
            float viewH = Mathf.Min(contentH, rowsRoom);
            _scroll.vertical = contentH > viewH + 0.5f;
            if (_scroll.vertical)
            {
                viewH = CutAtRow(viewH, count);
            }

            float keep = _keepScroll ? _content.anchoredPosition.y : 0f; // lo que la lista ya bajó (dp)
            UiBuilder.Place(_viewport, 16f, y, inner, viewH);
            UiBuilder.Place(_content, 0f, 0f, inner, contentH);
            _scroll.velocity = Vector2.zero;
            if (_scroll.vertical && keep > 0f)
            {
                _content.anchoredPosition = new Vector2(0f, Mathf.Clamp(keep, 0f, contentH - viewH));
            }

            _laidTab = Tab;
            _laidOut = true;
            _keepScroll = false;
            float panelH = y + viewH + 16f;
            float surfaceH = panelH - 16f;
            _fadeTop.color = ProceduralArt.ModalSurfaceColorAt(y - 8f, surfaceH);
            _fadeBottom.color = ProceduralArt.ModalSurfaceColorAt(y + viewH - 8f, surfaceH);
            UiBuilder.Place(_fadeTop.rectTransform, 0f, 0f, inner, SettingsScreen.FadeTopDp);
            UiBuilder.Place(_fadeBottom.rectTransform, 0f, viewH - SettingsScreen.FadeBottomDp, inner, SettingsScreen.FadeBottomDp);
            UpdateFades();
            y += viewH + 16f;
            float h = y;
            ModalKit.Size(_panel, _surface, _art, (_canvas.Width - w) * 0.5f, top + Mathf.Max(0f, (available - h) * 0.5f), w, h, sharedSurface: true);
        }

        /// <summary>
        /// Alto del visor para que la última fila cortada se vea entre <see cref="SettingsScreen.CutRowMin"/> y
        /// <see cref="SettingsScreen.CutRowMax"/> de su alto; si no, acaba en la mitad de la última que se ve al menos a medias
        /// (nunca de la primera). Usa los altos ya colocados de <see cref="_rowHeights"/>.
        /// </summary>
        private float CutAtRow(float viewH, int count)
        {
            float top = 0f;
            for (int i = 0; i < count; i++)
            {
                float h = _rowHeights[i];
                if (top + h > viewH + 0.5f)
                {
                    float seen = (viewH - top) / h;
                    if (seen >= SettingsScreen.CutRowMin && seen <= SettingsScreen.CutRowMax)
                    {
                        return viewH;
                    }

                    if (seen >= 0.5f)
                    {
                        return Mathf.Floor(top + (0.5f * h));
                    }

                    return i == 0 ? viewH : Mathf.Floor(top - (0.5f * _rowHeights[i - 1]));
                }

                top += h;
            }

            return viewH;
        }

        // Cada fundido sale solo si queda lista por ese lado (verticalNormalizedPosition: 1 arriba, 0 abajo).
        private void UpdateFades()
        {
            bool scrolls = _scroll.vertical;
            float pos = scrolls ? _scroll.verticalNormalizedPosition : 1f;
            bool top = scrolls && pos < 0.99f;
            bool bottom = scrolls && pos > 0.01f;
            if (_fadeTop.gameObject.activeSelf != top)
            {
                _fadeTop.gameObject.SetActive(top);
            }

            if (_fadeBottom.gameObject.activeSelf != bottom)
            {
                _fadeBottom.gameObject.SetActive(bottom);
            }
        }

        private float RowsHeight(int count, float rowH)
        {
            float total = 0f;
            for (int i = 0; i < count; i++)
            {
                total += Mathf.Max(rowH, _rowHeights[i]);
            }

            return total;
        }

        /// <summary>
        /// Reparte <paramref name="inner"/> entre las <paramref name="shown"/> pestañas visibles (en <see cref="_tabWidths"/>) y
        /// devuelve su alto (D-033, punto 11; 02 §12). A partes iguales, salvo que la palabra más ancha de una etiqueta no quepa
        /// en su parte a 12 sp (× escala): esa se queda el ancho de su palabra y las demás se reparten el resto. Así ninguna
        /// palabra se parte; la etiqueta que no cabe en una línea va en dos y la fila de pestañas crece lo que pida (≥ 48 dp). Si
        /// ni así caben (las palabras juntas pasan del panel: 320 dp con la letra al 130 % en es-419 o pt-BR; observación 8 de la
        /// jornada 6), van en dos columnas de la mitad del panel (<see cref="TabColumns"/>), fila a fila, y la palabra cabe entera; solo
        /// si ni en media fila cabe, la salvaguarda de <see cref="ShareTabWidths"/> las mete en el panel. Devuelve el alto de cada fila
        /// de pestañas. Mide al colocar, nunca por fotograma.
        /// </summary>
        private float TabWidths(float inner, int shown)
        {
            if (shown <= 0)
            {
                for (int i = 0; i < _tabs.Length; i++)
                {
                    _tabWidths[i] = 0f;
                }

                return TabDp;
            }

            for (int i = 0; i < _tabs.Length; i++)
            {
                // Cada colocación mide con el autoajuste de siempre (ShareTabFontSize bajó el tope en la anterior).
                _tabLabels[i].fontSizeMax = TabSp * UiBuilder.TextScale;
                _tabWords[i] = _tabs[i].gameObject.activeSelf ? Mathf.Ceil(UiBuilder.LongestWordWidth(_tabLabels[i])) + (2f * TabPadDp) + 1f : -1f;
            }

            _tabCols = TabColumns(inner, _tabWords);
            if (_tabCols < shown)
            {
                float half = Mathf.Floor(inner / _tabCols * 100f) / 100f;
                for (int i = 0; i < _tabs.Length; i++)
                {
                    _tabWidths[i] = _tabWords[i] >= 0f ? half : 0f;
                }
            }
            else
            {
                ShareTabWidths(inner, _tabWords, _tabWidths);
            }

            float h = TabDp;
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i].gameObject.activeSelf)
                {
                    h = Mathf.Max(h, Mathf.Ceil(UiBuilder.MinTextHeight(_tabLabels[i], _tabWidths[i] - (2f * TabPadDp))) + 4f);
                }
            }

            return h;
        }

        /// <summary>
        /// Un solo tamaño de letra para todas las pestañas visibles (observación 15 de la jornada 6 en el teléfono: al 130 %, y en
        /// es-419 también al 100 %, cada pestaña encogía su texto por su cuenta y la fila mezclaba tamaños). Cada etiqueta, ya en su
        /// caja, se ajusta sola con su autoajuste (entre 12 sp y <see cref="TabSp"/> × escala); el tamaño común es el menor de esos,
        /// el mayor que hace caber a todas, y nunca baja del mínimo del autoajuste (12 sp × escala, 02 §12; D-061 no rebaja las
        /// pestañas). Se aplica como tope del autoajuste de cada una, así que cada etiqueta queda a ese tamaño. En la rejilla de 2×2
        /// (320 dp con la letra grande) también es uno solo: las dos filas son la misma barra. Mide al colocar, nunca por fotograma.
        /// </summary>
        private void ShareTabFontSize()
        {
            float shared = TabSp * UiBuilder.TextScale;
            for (int i = 0; i < _tabLabels.Length; i++)
            {
                if (!_tabs[i].gameObject.activeSelf)
                {
                    continue;
                }

                TextMeshProUGUI label = _tabLabels[i];
                label.ForceMeshUpdate(); // el autoajuste deja en fontSize el tamaño con el que cabe en su caja
                shared = Mathf.Min(shared, label.fontSize);
            }

            float floor = UiBuilder.MinTextSp * UiBuilder.TextScale;
            shared = Mathf.Max(floor, Mathf.Floor(shared * 100f) / 100f);
            for (int i = 0; i < _tabLabels.Length; i++)
            {
                TextMeshProUGUI label = _tabLabels[i];
                label.fontSizeMax = Mathf.Max(label.fontSizeMin, shared);
                if (_tabs[i].gameObject.activeSelf)
                {
                    label.ForceMeshUpdate();
                }
            }

            TabFontSize = shared;
        }

        /// <summary>El tamaño común de las etiquetas de las pestañas en la última colocación (dp; tests).</summary>
        internal float TabFontSize { get; private set; }

        /// <summary>
        /// Columnas de las pestañas visibles (las de <paramref name="words"/> no negativas, en dp con su aire): todas en una fila si la
        /// suma de sus palabras cabe en <paramref name="inner"/>; si no, con tres o más, dos columnas si cada palabra cabe en la mitad
        /// (observación 8 de la jornada 6: así ninguna se parte); si ni así, una fila con la salvaguarda de <see cref="ShareTabWidths"/>.
        /// </summary>
        internal static int TabColumns(float inner, float[] words)
        {
            int shown = 0;
            float total = 0f;
            float widest = 0f;
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i] >= 0f)
                {
                    shown++;
                    total += words[i];
                    widest = Mathf.Max(widest, words[i]);
                }
            }

            if (shown <= 2 || total <= inner || widest > inner * 0.5f)
            {
                return Math.Max(1, shown);
            }

            return 2;
        }

        /// <summary>
        /// Reparte <paramref name="inner"/> dp entre las pestañas (en <paramref name="widths"/>) según lo que pide la palabra más
        /// ancha de cada una (<paramref name="words"/>, en dp; negativa: la pestaña no se ve y se queda en 0). A partes iguales,
        /// salvo las que no caben en su parte, que se quedan el ancho de su palabra mientras las demás se reparten el resto.
        /// Salvaguarda: si ni así caben (la suma de las palabras pasa de <paramref name="inner"/>), cada una se queda una parte
        /// proporcional a su palabra, y la etiqueta se encoge o parte la palabra; las pestañas nunca se salen del panel: la suma de
        /// <paramref name="widths"/> nunca pasa de <paramref name="inner"/>.
        /// </summary>
        internal static void ShareTabWidths(float inner, float[] words, float[] widths)
        {
            int shown = 0;
            float total = 0f;
            for (int i = 0; i < words.Length; i++)
            {
                widths[i] = words[i] >= 0f ? -1f : 0f;
                if (words[i] >= 0f)
                {
                    shown++;
                    total += words[i];
                }
            }

            if (shown == 0)
            {
                return;
            }

            if (total > inner)
            {
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i] >= 0f)
                    {
                        widths[i] = total > 0f ? Mathf.Floor(inner * (words[i] / total) * 100f) / 100f : inner / shown;
                    }
                }

                return;
            }

            // Reparto con mínimos: fija las que no caben en la parte igual y vuelve a repartir lo que queda entre las demás.
            float left = inner;
            int free = shown;
            bool changed = true;
            while (changed && free > 0)
            {
                changed = false;
                float share = left / free;
                for (int i = 0; i < words.Length; i++)
                {
                    if (widths[i] < 0f && words[i] > share)
                    {
                        widths[i] = words[i];
                        left -= words[i];
                        free--;
                        changed = true;
                    }
                }
            }

            float rest = free > 0 ? Mathf.Max(0f, left) / free : 0f;
            for (int i = 0; i < words.Length; i++)
            {
                if (widths[i] < 0f)
                {
                    widths[i] = rest;
                }
            }
        }

        /// <summary>
        /// Una fila: una tarjeta con sombra y canto iluminado (observación 24 de la jornada 6), y en ella el icono, el nombre, el
        /// contenido y el botón. En la pestaña Cuias, tocar la tarjeta abre la vista previa (<see cref="CardTap"/>).
        /// </summary>
        private sealed class RowView
        {
            public RowView(RectTransform panel, ProceduralArt art, int index, Action onPress, Action onCard)
            {
                Root = UiBuilder.Rect("Row" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), panel);
                float m = ChipPpuMultiplier;
                Shadow = UiBuilder.Sliced("CardShadow", Root, art.RoundedRect, new Color(0.18f, 0.11f, 0.06f, 0.22f));
                Edge = UiBuilder.Sliced("CardEdge", Root, art.RoundedRect, ColorMath.Lighten(Palette.FamilyWood.Light, 0.15f));
                Card = UiBuilder.Sliced("Card", Root, art.RoundedRect, Palette.PanelLight);
                Shine = UiBuilder.Sliced("CardShine", Root, art.RoundedRect, new Color(1f, 1f, 1f, 0.7f));
                Shadow.pixelsPerUnitMultiplier = m;
                Edge.pixelsPerUnitMultiplier = m;
                Card.pixelsPerUnitMultiplier = m;
                Shine.pixelsPerUnitMultiplier = m * 3f;
                CardTap = UiBuilder.BareButton("CardTap", Root, onCard); // debajo del botón de la fila: el botón gana el toque
                A11yLabels.Set(CardTap, () => SkinName ?? string.Empty);
                CardTap.gameObject.SetActive(false);
                Icon = UiBuilder.Image("Icon", Root, art.IconSeed, Color.white);
                Icon.preserveAspect = true;
                Name = UiBuilder.Label("Name", Root, TextStyle.PlainBold, string.Empty, 16f, Palette.FamilyWood.Deep, TextAlignmentOptions.Left);
                Name.textWrappingMode = TextWrappingModes.Normal; // un nombre largo va en dos líneas (D-033, punto 11)
                UiBuilder.AutoSize(Name, UiBuilder.MinTextSp, 16f);
                Detail = UiBuilder.Label("Detail", Root, TextStyle.Body, string.Empty, 13f, Palette.FamilyWood.Base, TextAlignmentOptions.Left);
                Detail.textWrappingMode = TextWrappingModes.Normal;
                UiBuilder.AutoSize(Detail, UiBuilder.MinTextSp, 13f);
                Note = UiBuilder.Label("Note", Root, TextStyle.PlainBold, string.Empty, 12f, Palette.UiRedWarning, TextAlignmentOptions.Left);
                Note.textWrappingMode = TextWrappingModes.Normal;
                UiBuilder.AutoSize(Note, UiBuilder.MinTextSp, 12f);
                Cta = ModalKit.Cta("BuyButton", Root, art.CtaLeaf, TextStyle.CtaLeaf, string.Empty, 17f, onPress, out TextMeshProUGUI label);
                CtaLabel = label;
                A11yLabels.Set(Cta.Button, () => SkinName == null ? CtaLabel.text : A11yText.Append(SkinName, CtaLabel.text));
                Root.gameObject.SetActive(false);
            }

            public RectTransform Root { get; }

            public Image Shadow { get; }

            public Image Edge { get; }

            public Image Card { get; }

            public Image Shine { get; }

            /// <summary>El área de la tarjeta que abre la vista previa (solo en la pestaña Cuias).</summary>
            public Button CardTap { get; }

            public Image Icon { get; }

            /// <summary>El rectángulo de la ficha (sonda: <c>market_card_*</c>).</summary>
            public RectTransform IconRect => Icon.rectTransform;

            /// <summary>El icono es una cuia (pestaña Cuias): ocupa el alto de la fila en vez de 36 × 36 dp.</summary>
            public bool TallIcon { get; set; }

            /// <summary>Nombre de la skin de la fila (pestaña Cuias), que el lector dice con el botón; null en las demás.</summary>
            public string? SkinName { get; set; }

            public TextMeshProUGUI Name { get; }

            public TextMeshProUGUI Detail { get; }

            /// <summary>Nota de la fila (el temporizador de la Mochila); vacía en las demás.</summary>
            public TextMeshProUGUI Note { get; }

            public ReliefParts Cta { get; }

            public TextMeshProUGUI CtaLabel { get; }

            /// <summary>
            /// Alto que pide la fila con <paramref name="w"/> dp de ancho (D-033, punto 11; 02 §12): el nombre, el contenido y la
            /// nota en las líneas que pidan a su tamaño mínimo (12 sp × escala o más), y el botón con su relieve, el que sea mayor.
            /// Con los textos que ya cabían no pasa de <see cref="MinRowDp"/>: nada cambia donde ya cabía.
            /// </summary>
            public float Needed(float w)
            {
                float textW = TextWidth(w);
                float text = 2f + NameHeight(textW) + DetailHeight(textW) + NoteHeight(textW) + 4f;
                float cta = Cta.Button.gameObject.activeSelf ? ModalKit.HeightOf(Cta) + 4f : 0f;
                return Mathf.Ceil(Mathf.Max(MinRowDp, Mathf.Max(text, cta)));
            }

            public void Place(float x, float y, float w, float h)
            {
                UiBuilder.Place(Root, x, y, w, h);
                // La tarjeta deja 2 dp arriba y abajo (4 entre dos filas), con su sombra 2 dp más abajo y el canto de luz arriba.
                UiBuilder.Place(Shadow.rectTransform, 1f, 4f, w - 2f, h - 4f);
                UiBuilder.Place(Edge.rectTransform, 0f, 2f, w, h - 4f);
                UiBuilder.Place(Card.rectTransform, 1f, 3f, w - 2f, h - 6f);
                UiBuilder.Place(Shine.rectTransform, ChipRadiusDp * 0.5f, 3.5f, w - ChipRadiusDp, 2f);
                UiBuilder.Place((RectTransform)CardTap.transform, 0f, 0f, w, h);
                float iconW = IconWidth;
                float iconH = TallIcon ? Mathf.Max(36f, h - 4f) : 36f;
                UiBuilder.Place(Icon.rectTransform, CardPadDp, (h - iconH) * 0.5f, iconW, iconH);
                float textX = CardPadDp + iconW + 8f;
                float textW = TextWidth(w);
                float nameH = NameHeight(textW);
                float detailH = DetailHeight(textW);
                float noteH = NoteHeight(textW);
                float nameY = 2f;
                if (TallIcon)
                {
                    // La skin: el nombre (y su «Premio del mundo · faltan N») centrado con su ficha, no pegado arriba (revisión
                    // de la observación 4 de la jornada 6).
                    float block = nameH + (Detail.text.Length > 0 ? detailH : 0f) + noteH;
                    nameY = Mathf.Max(2f, Mathf.Floor((h - block) * 0.5f));
                }

                float top = nameY + nameH;
                UiBuilder.Place(Name.rectTransform, textX, nameY, textW, nameH);
                UiBuilder.Place(Detail.rectTransform, textX, top, textW, noteH > 0f || TallIcon ? detailH : Mathf.Max(detailH, h - top - 4f));
                UiBuilder.Place(Note.rectTransform, textX, top + detailH, textW, Mathf.Max(Mathf.Max(14f, noteH), h - top - detailH - 2f));
                float ctaH = ModalKit.HeightOf(Cta);
                ModalKit.Place(Cta, w - CardPadDp - BuyWidthDp, Mathf.Max(0f, (h - ctaH) * 0.5f), BuyWidthDp);
            }

            // La ficha de la skin es más ancha que un icono (SkinTileDp); los demás, 36 dp.
            private float IconWidth => TallIcon ? SkinTileDp : 36f;

            private float TextWidth(float w) => w - (2f * CardPadDp) - IconWidth - 8f - BuyWidthDp - 8f;

            // Los altos de siempre (20, 18 y 14 dp) o, si el texto no cabe ni a su mínimo, lo que pida.
            private float NameHeight(float textW) => Mathf.Max(20f, Mathf.Ceil(UiBuilder.MinTextHeight(Name, textW)));

            private float DetailHeight(float textW) => Mathf.Max(18f, Mathf.Ceil(UiBuilder.MinTextHeight(Detail, textW)));

            private float NoteHeight(float textW) => Note.text.Length > 0 ? Mathf.Max(14f, Mathf.Ceil(UiBuilder.MinTextHeight(Note, textW))) : 0f;
        }
    }

    /// <summary>Lo que el Mercado necesita del arranque.</summary>
    internal sealed class MarketDeps
    {
        public MarketDeps(IStoreService store, OfferScheduler offers, SkinService? skins, Func<int> maxLevel, Func<int> currentWorld)
        {
            Store = store ?? throw new ArgumentNullException(nameof(store));
            Offers = offers ?? throw new ArgumentNullException(nameof(offers));
            Skins = skins;
            MaxLevel = maxLevel ?? throw new ArgumentNullException(nameof(maxLevel));
            CurrentWorld = currentWorld ?? throw new ArgumentNullException(nameof(currentWorld));
        }

        public IStoreService Store { get; }

        public OfferScheduler Offers { get; }

        public SkinService? Skins { get; set; }

        public Func<int> MaxLevel { get; }

        public Func<int> CurrentWorld { get; }

        /// <summary>Compras bloqueadas (perfil bloqueado; 08 §4.4).</summary>
        public bool PurchasesBlocked { get; set; }

        /// <summary>Las notas de compra de la tienda verificada (F3-5), o null (sin notas).</summary>
        public PurchaseNotes? Notes { get; set; }

        /// <summary><c>af_iap_offer_shown</c> con <c>context = shop</c> de un SKU que la pestaña enseña (08 §8.2), o null.</summary>
        public Action<string>? OfferShown { get; set; }
    }
}
