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

        /// <summary>Ancho del botón de precio de cada fila (dp) [P].</summary>
        public const float BuyWidthDp = 108f;

        /// <summary>Alto mínimo de una pestaña (dp): el área táctil de 07 §7.8.</summary>
        public const float TabDp = 48f;

        /// <summary>Margen interior de cada lado de la etiqueta de una pestaña (dp) [P].</summary>
        public const float TabPadDp = 3f;

        private readonly ProceduralArt _art;
        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly ReliefParts _close;
        private readonly Image _seedIcon;
        private readonly TextMeshProUGUI _seeds;
        private readonly TextMeshProUGUI _status;
        private readonly Button[] _tabs = new Button[4];
        private readonly TextMeshProUGUI[] _tabLabels = new TextMeshProUGUI[4];
        private readonly RowView[] _rows = new RowView[MaxRows];
        private readonly RectTransform _viewport;
        private readonly RectTransform _content;
        private readonly ScrollRect _scroll;
        private readonly Image _fadeTop;
        private readonly Image _fadeBottom;
        private readonly List<MarketRow> _model = new List<MarketRow>(MaxRows);
        private readonly float[] _tabWidths = new float[4];
        private readonly float[] _tabWords = new float[4];
        private readonly float[] _rowHeights = new float[MaxRows];
        private readonly MarketDeps _deps;
        private Action? _onClosed;
        private bool _logShown;
        private bool _keepScroll; // el próximo Layout conserva la posición de la lista (un Refresh sin cambio de pestaña)
        private MarketTab _laidTab;
        private bool _laidOut;
        private int _visibleRows = -1; // filas con botón a la vista en el último AF_LAYOUT (máscara de bits; sonda de desarrollo)

        public MarketScreen(Transform host, ProceduralArt art, MarketDeps deps)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            _deps = deps ?? throw new ArgumentNullException(nameof(deps));
            _canvas = new OverlayCanvas("MarketCanvas", host, SortingOrder);
            ModalKit.Dim("Dim", _canvas.Root, 0.6f);
            _panel = ModalKit.Card("Panel", _canvas.Root, art, out _surface);
            _title = ModalKit.Header(_panel, art, MarketText.Title, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, () => Close());
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
                _tabLabels[i] = UiBuilder.Label("Label", _tabs[i].transform, TextStyle.PlainBold, string.Empty, 15f, Palette.FamilyWood.Base, TextAlignmentOptions.Center);
                UiBuilder.Stretch(_tabLabels[i].rectTransform);
                // D-033, punto 11: al 130 % «Sin anuncios» no cabía en una línea de su cuarto de fila: va en dos, sin partir palabras.
                _tabLabels[i].textWrappingMode = TextWrappingModes.Normal;
                UiBuilder.AutoSize(_tabLabels[i], UiBuilder.MinTextSp, 15f);
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
                _rows[i] = new RowView(_content, art, i, () => PressRow(index));
            }

            _fadeTop = UiBuilder.Image("FadeTop", _viewport, art.ScrollFade(true), Color.white);
            _fadeBottom = UiBuilder.Image("FadeBottom", _viewport, art.ScrollFade(false), Color.white);
            _scroll.onValueChanged.AddListener(_ =>
            {
                UpdateFades();
                ProbeScrolled();
            });

            _canvas.Canvas.gameObject.SetActive(false);
        }

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

        /// <summary>Se compró o se eligió algo: el arranque refresca el inicio (saldo, Cántaro) y las cuias (skin).</summary>
        public event Action<PurchaseResult>? Purchased;

        /// <summary>Se puso otra skin.</summary>
        public event Action<string>? SkinSelected;

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

            _canvas.Canvas.gameObject.SetActive(false);
            Action? done = _onClosed;
            _onClosed = null;
            done?.Invoke();
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
                _tabLabels[i].color = selected ? Palette.Ink : (Color)Palette.FamilyWood.Base;
                _tabLabels[i].fontStyle = selected ? FontStyles.Underline : FontStyles.Normal;
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
            Sprite? icon = IconOf(row.Id, skin);
            view.Icon.sprite = icon;
            view.Icon.color = skin ? CuiaSkin.For(row.Id).Rim : Color.white;
            if (skin && string.Equals(row.Id, SkinRewards.BaseSkinId, StringComparison.Ordinal))
            {
                view.Icon.color = Palette.CapWood; // la base no tiene borde propio: madera clara [P]
            }

            bool hasButton = row.Action != MarketAction.None;
            view.Cta.Button.gameObject.SetActive(hasButton);
            if (!hasButton)
            {
                return;
            }

            switch (row.Action)
            {
                case MarketAction.Equip:
                    view.CtaLabel.text = MarketText.SkinEquip;
                    UiBuilder.SetEnabled(view.Cta.Button, true);
                    break;
                case MarketAction.Equipped:
                    view.CtaLabel.text = MarketText.SkinEquipped;
                    UiBuilder.SetEnabled(view.Cta.Button, false);
                    break;
                default:
                    view.CtaLabel.text = _deps.Store.PriceOf(row.Id).ToString();
                    // Mientras una compra se confirma («Confirmando…»), ningún precio responde: no se abre otra (IsPurchasing).
                    UiBuilder.SetEnabled(view.Cta.Button, storeOn && row.Action == MarketAction.Buy && !_deps.Store.IsPurchasing);
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
                return _art.Disc;
            }

            switch (id)
            {
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

            float tabH = TabWidths(inner, shownTabs);
            float x = 16f;
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (!_tabs[i].gameObject.activeSelf)
                {
                    continue;
                }

                UiBuilder.Place((RectTransform)_tabs[i].transform, x, y, _tabWidths[i], tabH);
                UiBuilder.Place(_tabLabels[i].rectTransform, TabPadDp, 0f, _tabWidths[i] - (2f * TabPadDp), tabH);
                x += _tabWidths[i];
            }

            y += tabH + 8f;
            int count = Math.Min(_model.Count, MaxRows);
            float rowH = RowDp;
            if (count > 0)
            {
                // Cada fila, al alto que pidan sus textos y su botón (D-033, punto 11: al 130 % el contenido va en las líneas que
                // pida); las que caben se reparten el sitio como siempre, entre MinRowDp y RowDp.
                float room = available - y - 16f;
                for (int i = 0; i < count; i++)
                {
                    _rowHeights[i] = _rows[i].Needed(inner);
                }

                rowH = Mathf.Clamp(Mathf.Floor(room / count), MinRowDp, RowDp);

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
        /// ni así caben, la salvaguarda de <see cref="ShareTabWidths"/> las mete en el panel. Mide al colocar, nunca por fotograma.
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
                _tabWords[i] = _tabs[i].gameObject.activeSelf ? Mathf.Ceil(UiBuilder.LongestWordWidth(_tabLabels[i])) + (2f * TabPadDp) + 1f : -1f;
            }

            ShareTabWidths(inner, _tabWords, _tabWidths);
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

        /// <summary>Una fila: icono, nombre, contenido y botón.</summary>
        private sealed class RowView
        {
            public RowView(RectTransform panel, ProceduralArt art, int index, Action onPress)
            {
                Root = UiBuilder.Rect("Row" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), panel);
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
                Root.gameObject.SetActive(false);
            }

            public RectTransform Root { get; }

            public Image Icon { get; }

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
                UiBuilder.Place(Icon.rectTransform, 0f, (h - 36f) * 0.5f, 36f, 36f);
                float textX = 44f;
                float textW = TextWidth(w);
                float nameH = NameHeight(textW);
                float detailH = DetailHeight(textW);
                float noteH = NoteHeight(textW);
                float top = 2f + nameH;
                UiBuilder.Place(Name.rectTransform, textX, 2f, textW, nameH);
                UiBuilder.Place(Detail.rectTransform, textX, top, textW, noteH > 0f ? detailH : Mathf.Max(detailH, h - top - 4f));
                UiBuilder.Place(Note.rectTransform, textX, top + detailH, textW, Mathf.Max(Mathf.Max(14f, noteH), h - top - detailH - 2f));
                float ctaH = ModalKit.HeightOf(Cta);
                ModalKit.Place(Cta, w - BuyWidthDp, Mathf.Max(0f, (h - ctaH) * 0.5f), BuyWidthDp);
            }

            private static float TextWidth(float w) => w - 44f - BuyWidthDp - 8f;

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
