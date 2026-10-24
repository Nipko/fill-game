#nullable enable
using System;
using System.Collections.Generic;
using AF.LiveOps;
using AF.Platform;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Pantalla de la Liga del río de la 1.0 (D-046; <c>docs/dev/liga.md</c> §1), construida por código como Ajustes (una tarjeta
    /// de madera clara sobre un velo, en su propio lienzo):
    /// <list type="bullet">
    /// <item>cabecera «Liga del río» con su cierre (y el Atrás de Android, que maneja <c>GameBootstrap</c>);</item>
    /// <item>pestañas <b>Global</b> y <b>{país}</b> (bandera y nombre); con el país desconocido, solo Global;</item>
    /// <item>«Semana del {inicio} al {fin}» y «Termina en {tiempo}»; con una clasificación vieja, «Actualizado hace {tiempo}»;</item>
    /// <item>el top 100 en una lista que se desplaza: posición, seudónimo, y en Global la bandera con el nombre del país debajo del
    /// seudónimo (la bandera nunca va sola), y los puntos;</item>
    /// <item>la fila del jugador fija abajo: su posición exacta en el top o «Estás en el {p} % mejor», y sus puntos;</item>
    /// <item>sin nada guardado de esta semana, «Conéctate para ver la Liga» (o «Cargando…» mientras se pide).</item>
    /// </list>
    /// TalkBack lee cada fila entera (posición, seudónimo, país y puntos) en un solo elemento. Al 130 % los textos encogen hasta
    /// 12 sp y las filas crecen con la escala. Las filas se crean una vez (100) y se reutilizan.
    /// </summary>
    internal sealed class LeagueScreen
    {
        /// <summary>Orden del lienzo: encima del inicio y de sus iconos, por debajo de Ajustes (40).</summary>
        public const int SortingOrder = 38;

        /// <summary>Ancho máximo de la tarjeta (dp), como Ajustes.</summary>
        public const float PanelMaxWidth = 344f;

        /// <summary>Alto mínimo de una fila y de una pestaña (área táctil, 07 §7.8).</summary>
        public const float MinRowDp = 48f;

        /// <summary>Id de la pantalla en la sonda (<c>AF_LAYOUT league</c>).</summary>
        public const string ProbeScreen = "league";

        private const float RankColDp = 32f;
        private const float PointsColDp = 64f;
        private const float LineK = 1.35f;
        private const float GapDp = 6f;
        private const float FlagHDp = 14f;
        private const float NameSp = 15f;
        private const float CountrySp = 12f;
        private const float InfoSp = 14f;

        private readonly ProceduralArt _art;
        private readonly DevProbe? _probe;
        private readonly Action _onClose;
        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly TextMeshProUGUI _title;
        private readonly Image _header;
        private readonly ReliefParts _close;
        private readonly Tab _tabGlobal;
        private readonly Tab _tabCountry;
        private readonly TextMeshProUGUI _week;
        private readonly TextMeshProUGUI _ends;
        private readonly TextMeshProUGUI _status;
        private readonly RectTransform _viewport;
        private readonly RectTransform _content;
        private readonly ScrollRect _scroll;
        private readonly TextMeshProUGUI _message;
        private readonly Row _me;
        private readonly TextMeshProUGUI _youPill;
        private readonly List<Row> _rows = new List<Row>(LeagueRules.MaxTop);
        private LeagueService? _service;
        private IClock? _clock;
        private LeagueScope _scope;
        private float _tickMs;
        private long _endsShownMin = -1;

        public LeagueScreen(Transform host, ProceduralArt art, DevProbe? probe, Action onClose)
        {
            _art = art;
            _probe = probe;
            _onClose = onClose ?? throw new ArgumentNullException(nameof(onClose));
            _canvas = new OverlayCanvas("LeagueCanvas", host, SortingOrder);
            ModalKit.Dim("Dim", _canvas.Root, 0.62f);
            _panel = ModalKit.Card("Panel", _canvas.Root, art, out _surface);
            _title = ModalKit.Header(_panel, art, LeagueText.Title, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, () => _onClose());
            _tabGlobal = new Tab("TabGlobal", _panel, art, () => SelectTab(LeagueScope.Global), false);
            _tabCountry = new Tab("TabCountry", _panel, art, () => SelectTab(LeagueScope.Country), true);
            _week = UiBuilder.Label("Week", _panel, TextStyle.PlainBold, string.Empty, InfoSp, Palette.FamilyWood.Deep, TextAlignmentOptions.Center);
            _week.textWrappingMode = TextWrappingModes.Normal;
            _ends = UiBuilder.Label("EndsIn", _panel, TextStyle.Body, string.Empty, InfoSp, Palette.FamilyWood.Deep, TextAlignmentOptions.Center);
            _status = UiBuilder.Label("Status", _panel, TextStyle.Body, string.Empty, CountrySp, Palette.FamilyWood.Base, TextAlignmentOptions.Center);

            _viewport = UiBuilder.Rect("Viewport", _panel);
            _viewport.gameObject.AddComponent<RectMask2D>();
            _viewport.gameObject.AddComponent<HitArea>().raycastTarget = true;
            _content = UiBuilder.Rect("Content", _viewport);
            _scroll = _viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = _viewport;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _message = UiBuilder.Label("Message", _panel, TextStyle.PlainBold, string.Empty, 16f, Palette.FamilyWood.Deep, TextAlignmentOptions.Center);
            _message.textWrappingMode = TextWrappingModes.Normal;

            _me = new Row("MeRow", _panel, art, true);
            _youPill = UiBuilder.Label("You", _me.Root, TextStyle.PillWood, LeagueText.You, 12f, Palette.Cream, TextAlignmentOptions.Center);
            A11yLabels.Set(_youPill, () => string.Empty); // lo dice la lectura de la fila
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>La pantalla está abierta.</summary>
        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        public Canvas Canvas => _canvas.Canvas;

        /// <summary>El ámbito a la vista.</summary>
        public LeagueScope Scope => _scope;

        /// <summary>La pestaña del país está a la vista (se conoce el país).</summary>
        public bool CountryTabShown => _tabCountry.Button.gameObject.activeSelf;

        /// <summary>El país de la pestaña (null sin ella).</summary>
        public string? CountryTabCode { get; private set; }

        public Button CloseButton => _close.Button;

        public Button GlobalTabButton => _tabGlobal.Button;

        public Button CountryTabButton => _tabCountry.Button;

        /// <summary>Lo que dice la cabecera de la semana.</summary>
        public string WeekText => _week.text;

        /// <summary>«Termina en {tiempo}».</summary>
        public string EndsText => _ends.text;

        /// <summary>«Actualizado hace…» (vacío si no se ve).</summary>
        public string StatusText => _status.gameObject.activeSelf ? _status.text : string.Empty;

        /// <summary>El aviso del centro («Conéctate…», «Cargando…»), vacío si no se ve.</summary>
        public string MessageText => _message.gameObject.activeSelf ? _message.text : string.Empty;

        /// <summary>Filas del top a la vista.</summary>
        public int RowsShown { get; private set; }

        /// <summary>La fila del jugador está a la vista.</summary>
        public bool MeShown => _me.Root.gameObject.activeSelf;

        /// <summary>Lo que dice la fila del jugador (la línea de la posición o del percentil).</summary>
        public string MeDetailText => _me.Sub.text;

        /// <summary>Puntos de la fila del jugador.</summary>
        public string MePointsText => _me.Points.text;

        /// <summary>El rango que enseña la fila del jugador («» fuera del top).</summary>
        public string MeRankText => _me.Rank.text;

        /// <summary>La lectura de TalkBack de la fila <paramref name="i"/> del top (o de la del jugador con −1).</summary>
        public string RowA11y(int i)
        {
            Row row = i < 0 ? _me : _rows[i];
            return A11yLabels.TryGetTextReading(row.Reader.gameObject, out string label) ? label : string.Empty;
        }

        /// <summary>Nombre del seudónimo de la fila <paramref name="i"/>.</summary>
        public string RowName(int i) => _rows[i].Name.text;

        /// <summary>El país de la fila <paramref name="i"/> (texto bajo el nombre; vacío en la pestaña del país).</summary>
        public string RowCountry(int i) => _rows[i].Sub.gameObject.activeSelf ? _rows[i].Sub.text : string.Empty;

        /// <summary>La bandera de la fila <paramref name="i"/> (null sin bandera).</summary>
        public string? RowFlag(int i) => _rows[i].Flag.Root.gameObject.activeSelf ? _rows[i].Flag.Code : null;

        /// <summary>El recuadro de la fila <paramref name="i"/> (para medir si cabe).</summary>
        internal RectTransform RowRect(int i) => i < 0 ? _me.Root : _rows[i].Root;

        internal TextMeshProUGUI[] TextsOf(int i)
        {
            Row r = i < 0 ? _me : _rows[i];
            return new[] { r.Rank, r.Name, r.Sub, r.Points };
        }

        internal RectTransform Panel => _panel;

        /// <summary>Abre la pantalla en <paramref name="scope"/> con lo que sabe <paramref name="service"/>.</summary>
        public void Open(LeagueService service, IClock clock, LeagueScope scope)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _scope = scope;
            _canvas.Canvas.gameObject.SetActive(true);
            _scroll.verticalNormalizedPosition = 1f;
            Refresh();
        }

        public void Close()
        {
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>Se pulsó una pestaña (lo fija <c>GameBootstrap</c>, que pide la clasificación y escribe la analítica).</summary>
        public Action<LeagueScope>? TabSelected { get; set; }

        /// <summary>Cada fotograma con la pantalla abierta: el «Termina en…» y «Actualizado hace…» cada pocos segundos.</summary>
        public void Tick(float dtMs)
        {
            if (!Visible || _clock == null)
            {
                return;
            }

            _tickMs += dtMs;
            if (_tickMs < 1000f)
            {
                return;
            }

            _tickMs = 0f;
            long minute = (long)(_clock.UtcNow.Ticks / TimeSpan.TicksPerMinute);
            if (minute != _endsShownMin)
            {
                Refresh();
            }
        }

        /// <summary>Repinta con lo que sabe el servicio ahora (al abrir, al llegar una clasificación y cada minuto).</summary>
        public void Refresh()
        {
            LeagueService? service = _service;
            IClock? clock = _clock;
            if (service == null || clock == null || !Visible)
            {
                return;
            }

            DateTime now = clock.UtcNow;
            _endsShownMin = now.Ticks / TimeSpan.TicksPerMinute;
            string? country = service.Country;
            CountryTabCode = country;
            if (country == null && _scope == LeagueScope.Country)
            {
                _scope = LeagueScope.Global;
            }

            _tabGlobal.Set(LeagueText.TabGlobal, null, _scope == LeagueScope.Global);
            _tabCountry.Button.gameObject.SetActive(country != null);
            if (country != null)
            {
                _tabCountry.Set(LeagueText.Country(country), country, _scope == LeagueScope.Country);
            }

            LeagueCacheEntry? entry = service.Cached(_scope);
            LeagueBoard? board = entry?.Board;
            DateTime start = board?.StartsAt ?? LeagueRules.WeekStart(now);
            DateTime end = board?.EndsAt ?? LeagueRules.WeekEnd(start);
            _week.text = LeagueText.Week(start, end);
            _ends.text = LeagueText.EndsIn(end - now);

            LeagueBoardState state = service.StateOf(_scope);
            bool stale = state == LeagueBoardState.Stale && service.LastFailure(_scope).HasValue;
            _status.gameObject.SetActive(stale && board != null);
            if (stale && board != null)
            {
                _status.text = LeagueText.UpdatedAgo(now - board.UpdatedAt);
            }

            bool global = _scope == LeagueScope.Global;
            int shown = 0;
            if (board != null)
            {
                for (int i = 0; i < board.Top.Count && i < LeagueRules.MaxTop; i++)
                {
                    Row row = RowAt(i);
                    row.Fill(board.Top[i], global);
                    shown++;
                }
            }

            for (int i = shown; i < _rows.Count; i++)
            {
                _rows[i].Root.gameObject.SetActive(false);
            }

            RowsShown = shown;
            string message = board == null
                ? (state == LeagueBoardState.Loading ? LeagueText.Loading : LeagueText.Offline)
                : (shown == 0 ? LeagueText.EmptyWeek : string.Empty);
            _message.text = message;
            _message.gameObject.SetActive(message.Length > 0);

            _me.Root.gameObject.SetActive(board != null);
            if (board != null)
            {
                _me.FillMe(board.Me, global);
            }

            Layout();
            EmitLayout();
        }

        private void SelectTab(LeagueScope scope)
        {
            if (_scope == scope)
            {
                return;
            }

            _scope = scope;
            _scroll.verticalNormalizedPosition = 1f;
            TabSelected?.Invoke(scope);
            Refresh();
        }

        private Row RowAt(int i)
        {
            while (_rows.Count <= i)
            {
                _rows.Add(new Row("Row" + _rows.Count, _content, _art, false));
            }

            return _rows[i];
        }

        private void Layout()
        {
            _canvas.Measure();
            float s = Mathf.Max(1f, UiBuilder.TextScale);
            float cw = _canvas.Width;
            float ch = _canvas.Height;
            float w = Mathf.Min(PanelMaxWidth, cw - 16f);
            float inner = w - 32f;
            float top = Mathf.Max(GameScreen.MinSafeTop, _canvas.SafeTop) + 8f;
            float bottom = Mathf.Max(GameScreen.MinSafeBottom, _canvas.SafeBottom) + 8f;
            float h = Mathf.Max(320f, ch - top - bottom);
            ModalKit.Size(_panel, _surface, _art, Mathf.Round((cw - w) * 0.5f), top, w, h, true);

            float headerH = Mathf.Max(40f, Mathf.Round((22f * s) + 12f));
            _title.fontSizeMax = 22f * s;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y + ((48f - headerH) * 0.5f), inner - ModalKit.CloseDp - 8f, headerH);
            UiBuilder.Place((RectTransform)_close.Button.transform, w - 16f - ModalKit.CloseDp, y, ModalKit.CloseDp, ModalKit.CloseDp);
            y += Mathf.Max(48f, headerH) + 8f;

            float tabH = Mathf.Max(MinRowDp, Mathf.Round((16f * s) + 22f));
            bool two = _tabCountry.Button.gameObject.activeSelf;
            float tabW = two ? (inner - 8f) * 0.5f : inner;
            _tabGlobal.Layout(16f, y, tabW, tabH, s);
            if (two)
            {
                _tabCountry.Layout(16f + tabW + 8f, y, tabW, tabH, s);
            }

            y += tabH + 6f;
            _week.fontSizeMax = InfoSp * s;
            _week.fontSizeMin = UiBuilder.MinTextSp * s;
            _week.enableAutoSizing = true;
            float weekH = Mathf.Ceil(Mathf.Max((InfoSp * s) + 4f, UiBuilder.MinTextHeight(_week, inner)));
            UiBuilder.Place(_week.rectTransform, 16f, y, inner, weekH);
            y += weekH;
            Row.Fit(_ends, InfoSp * s, s);
            float endsH = Mathf.Ceil((InfoSp * s) + 6f);
            UiBuilder.Place(_ends.rectTransform, 16f, y, inner, endsH);
            y += endsH;
            if (_status.gameObject.activeSelf)
            {
                _status.fontSize = Mathf.Max(UiBuilder.MinTextSp, CountrySp * s);
                float statusH = Mathf.Ceil(_status.fontSize + 6f);
                UiBuilder.Place(_status.rectTransform, 16f, y, inner, statusH);
                y += statusH;
            }

            y += 4f;
            bool global = _scope == LeagueScope.Global;
            float rowH = RowHeight(s, global);
            _youPill.fontSize = Mathf.Max(UiBuilder.MinTextSp, 12f * s);
            float pillW = Mathf.Ceil(UiBuilder.MinTextWidth(_youPill)) + 12f;
            float meH = _me.MeasureMe(w - 24f, s, pillW);
            float listBottom = h - 16f - (_me.Root.gameObject.activeSelf ? meH + 6f : 0f);
            float viewH = Mathf.Max(rowH, listBottom - y);
            UiBuilder.Place(_viewport, 12f, y, w - 24f, viewH);
            float rowsW = w - 24f;
            for (int i = 0; i < RowsShown; i++)
            {
                _rows[i].Layout(0f, i * rowH, rowsW, rowH, s, i % 2 == 1);
            }

            UiBuilder.Place(_content, 0f, 0f, rowsW, RowsShown * rowH);
            _scroll.vertical = RowsShown * rowH > viewH + 0.5f;
            UiBuilder.Place(_message.rectTransform, 24f, y + 24f, w - 48f, Mathf.Max(64f, 40f * s));
            _message.fontSize = Mathf.Max(UiBuilder.MinTextSp, 16f * s);
            if (_me.Root.gameObject.activeSelf)
            {
                _me.LayoutMe(12f, h - 16f - meH, w - 24f, meH, s, _youPill, pillW);
            }
        }

        // Alto de una fila del top: el seudónimo y, en Global, la línea de la bandera con el país, cada una con su interlineado.
        private static float RowHeight(float s, bool twoLines)
        {
            float lines = (NameSp * s * LineK) + (twoLines ? Mathf.Max(UiBuilder.MinTextSp, CountrySp * s) * LineK : 0f);
            return Mathf.Max(MinRowDp, Mathf.Ceil(lines + 8f));
        }

        private void EmitLayout()
        {
            if (!DevProbe.Enabled || _probe == null)
            {
                return;
            }

            var ids = new List<string>(3) { "close", "tab_global" };
            var rects = new List<RectTransform>(3) { (RectTransform)_close.Button.transform, (RectTransform)_tabGlobal.Button.transform };
            if (CountryTabShown)
            {
                ids.Add("tab_country");
                rects.Add((RectTransform)_tabCountry.Button.transform);
            }

            _probe.Layout(ProbeScreen, 0, null, ids.ToArray(), rects.ToArray());
        }

        /// <summary>Una pestaña: placa de madera con su texto (y, la del país, su bandera); la elegida, más oscura y con texto crema.</summary>
        private sealed class Tab
        {
            private readonly Image _plate;
            private readonly TextMeshProUGUI _label;
            private readonly LeagueFlag? _flag;
            private bool _selected;

            public Tab(string name, Transform parent, ProceduralArt art, Action onClick, bool withFlag)
            {
                Button = UiBuilder.BareButton(name, parent, onClick);
                _plate = UiBuilder.Sliced("Plate", Button.transform, art.WoodPlate, Color.white);
                UiBuilder.Stretch(_plate.rectTransform);
                _flag = withFlag ? new LeagueFlag("Flag", Button.transform, art) : null;
                _label = UiBuilder.Label("Label", Button.transform, TextStyle.PillWood, string.Empty, 16f, Palette.Cream, TextAlignmentOptions.Center);
                A11yLabels.Set(Button, () => _label.text, () => _selected);
            }

            public Button Button { get; }

            public void Set(string text, string? flag, bool selected)
            {
                _label.text = text;
                _selected = selected;
                _plate.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.55f);
                if (_flag != null)
                {
                    _flag.Show(flag, FlagHDp * LeagueFlag.Aspect * 1.4f, FlagHDp * 1.4f);
                }
            }

            public void Layout(float x, float y, float w, float h, float s)
            {
                UiBuilder.Place((RectTransform)Button.transform, x, y, w, h);
                _label.fontSizeMax = 16f * s;
                _label.fontSizeMin = Mathf.Min(16f * s, UiBuilder.MinTextSp * s);
                _label.enableAutoSizing = true;
                float pad = 10f;
                if (_flag != null && _flag.Root.gameObject.activeSelf)
                {
                    float fh = FlagHDp * 1.4f;
                    float fw = fh * LeagueFlag.Aspect;
                    UiBuilder.Place(_flag.Root, pad, (h - fh) * 0.5f, fw, fh);
                    _flag.Show(_flag.Code, fw, fh);
                    UiBuilder.Place(_label.rectTransform, pad + fw + 6f, 0f, w - (2f * pad) - fw - 6f, h);
                }
                else
                {
                    UiBuilder.Place(_label.rectTransform, pad, 0f, w - (2f * pad), h);
                }
            }
        }

        /// <summary>Una fila: posición, seudónimo (con la bandera y el país debajo en Global) y puntos.</summary>
        private sealed class Row
        {
            private readonly Image _bg;
            private readonly bool _isMe;
            private string _reading = string.Empty;

            public Row(string name, Transform parent, ProceduralArt art, bool isMe)
            {
                _isMe = isMe;
                Root = UiBuilder.Rect(name, parent);
                _bg = isMe
                    ? UiBuilder.Sliced("Plate", Root, art.WoodPlate, Color.white)
                    : UiBuilder.Image("Band", Root, null, new Color(Palette.FamilyWood.Light.r, Palette.FamilyWood.Light.g, Palette.FamilyWood.Light.b, 0.18f));
                UiBuilder.Stretch(_bg.rectTransform);
                Color ink = isMe ? Palette.Cream : Palette.UiInk;
                TextStyle style = isMe ? TextStyle.PillWood : TextStyle.PlainBold;
                Rank = UiBuilder.Label("Rank", Root, style, string.Empty, NameSp, ink, TextAlignmentOptions.MidlineRight);
                Name = UiBuilder.Label("Name", Root, style, string.Empty, NameSp, ink, TextAlignmentOptions.MidlineLeft);
                Sub = UiBuilder.Label("Sub", Root, isMe ? TextStyle.PillWood : TextStyle.Body, string.Empty, CountrySp, isMe ? Palette.Cream : Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
                Points = UiBuilder.Label("Points", Root, style, string.Empty, NameSp, ink, TextAlignmentOptions.MidlineRight);
                Flag = new LeagueFlag("Flag", Root, art);
                Name.textWrappingMode = TextWrappingModes.NoWrap;
                Sub.textWrappingMode = TextWrappingModes.NoWrap;
                // La fila se lee entera en un elemento: el del rango (siempre tiene texto) o, en la del jugador, el del nombre.
                Reader = isMe ? Name : Rank;
                A11yLabels.SetText(Reader, () => _reading);
                A11yLabels.Set(isMe ? Rank : Name, () => string.Empty);
                A11yLabels.Set(Sub, () => string.Empty);
                A11yLabels.Set(Points, () => string.Empty);
            }

            public RectTransform Root { get; }

            public TextMeshProUGUI Rank { get; }

            public TextMeshProUGUI Name { get; }

            public TextMeshProUGUI Sub { get; }

            public TextMeshProUGUI Points { get; }

            public LeagueFlag Flag { get; }

            /// <summary>El texto que lleva la lectura de TalkBack de la fila.</summary>
            public TextMeshProUGUI Reader { get; }

            public void Fill(LeagueRow row, bool global)
            {
                Root.gameObject.SetActive(true);
                Rank.text = Loc(row.Rank);
                Name.text = LeagueText.Player(row.NameKey, row.NameNum);
                Points.text = LeagueText.PointsNumber(row.Points);
                bool country = global && row.Country != null;
                Sub.gameObject.SetActive(country);
                Sub.text = country ? LeagueText.Country(row.Country!) : string.Empty;
                Flag.Show(country ? row.Country : null, FlagHDp * LeagueFlag.Aspect, FlagHDp);
                _reading = LeagueText.RowA11y(row, global);
            }

            public void FillMe(LeagueMe me, bool global)
            {
                Rank.text = me.Rank.HasValue ? Loc(me.Rank.Value) : string.Empty;
                Name.text = me.NameKey != null ? LeagueText.Player(me.NameKey, me.NameNum) : LeagueText.You;
                Points.text = LeagueText.PointsNumber(me.Points);
                Sub.gameObject.SetActive(true);
                Sub.text = me.Rank.HasValue
                    ? (global && me.Country != null ? LeagueText.Country(me.Country) : string.Empty)
                    : (me.Percentile.HasValue ? LeagueText.Percentile(me.Percentile.Value) : LeagueText.NoPoints);
                Flag.Show(null, 0f, 0f);
                _reading = LeagueText.MeA11y(me, global);
            }

            public void Layout(float x, float y, float w, float h, float s, bool odd)
            {
                UiBuilder.Place(Root, x, y, w, h);
                _bg.enabled = odd;
                float nameSp = NameSp * s;
                float subSp = Mathf.Max(UiBuilder.MinTextSp, CountrySp * s);
                Fit(Rank, nameSp, s);
                Fit(Name, nameSp, s);
                Fit(Points, nameSp, s);
                Fit(Sub, subSp, s);
                const float pad = 4f;
                UiBuilder.Place(Rank.rectTransform, pad, 0f, RankColDp, h);
                UiBuilder.Place(Points.rectTransform, w - pad - PointsColDp, 0f, PointsColDp, h);
                float nameX = pad + RankColDp + GapDp;
                float nameW = w - nameX - PointsColDp - pad - GapDp;
                bool two = Sub.gameObject.activeSelf && Sub.text.Length > 0;
                if (!two)
                {
                    UiBuilder.Place(Name.rectTransform, nameX, 0f, nameW, h);
                    return;
                }

                float lineH = Mathf.Ceil(nameSp * LineK);
                float subH = Mathf.Ceil(subSp * LineK);
                float top = Mathf.Max(0f, (h - lineH - subH) * 0.5f);
                UiBuilder.Place(Name.rectTransform, nameX, top, nameW, lineH);
                float subX = nameX;
                if (Flag.Root.gameObject.activeSelf)
                {
                    float fh = Mathf.Min(FlagHDp * s, subH);
                    float fw = fh * LeagueFlag.Aspect;
                    UiBuilder.Place(Flag.Root, nameX, top + lineH + ((subH - fh) * 0.5f), fw, fh);
                    Flag.Show(Flag.Code, fw, fh);
                    subX += fw + 5f;
                }

                UiBuilder.Place(Sub.rectTransform, subX, top + lineH, nameX + nameW - subX, subH);
            }

            /// <summary>
            /// Alto de la fila del jugador con el ancho <paramref name="w"/>: arriba la posición, el seudónimo y los puntos; debajo,
            /// la píldora «Tú» y la línea de la posición o del percentil, que puede ir en dos líneas (al 130 % en pt-BR no cabe en una).
            /// </summary>
            public float MeasureMe(float w, float s, float pillW)
            {
                float nameSp = NameSp * s;
                float subSp = Mathf.Max(UiBuilder.MinTextSp, CountrySp * s);
                Sub.enableAutoSizing = false;
                Sub.fontSize = subSp;
                Sub.textWrappingMode = TextWrappingModes.Normal;
                float subW = SubWidth(w, pillW);
                float subH = Sub.text.Length > 0 ? Mathf.Ceil(Mathf.Max(subSp * LineK, UiBuilder.MinTextHeight(Sub, subW))) : 0f;
                float pillH = Mathf.Ceil((Mathf.Max(UiBuilder.MinTextSp, 12f * s) * LineK) + 2f);
                return Mathf.Max(MinRowDp + 8f, Mathf.Ceil(8f + (nameSp * LineK) + 2f + Mathf.Max(subH, pillH) + 8f));
            }

            /// <summary>Coloca la fila del jugador (ya medida con <see cref="MeasureMe"/>) con su píldora «Tú».</summary>
            public void LayoutMe(float x, float y, float w, float h, float s, TextMeshProUGUI pill, float pillW)
            {
                UiBuilder.Place(Root, x, y, w, h);
                float nameSp = NameSp * s;
                Fit(Rank, nameSp, s);
                Fit(Name, nameSp, s);
                Fit(Points, nameSp, s);
                const float pad = 10f;
                float lineH = Mathf.Ceil(nameSp * LineK);
                const float top = 8f;
                UiBuilder.Place(Rank.rectTransform, pad, top, RankColDp, lineH);
                UiBuilder.Place(Points.rectTransform, w - pad - PointsColDp, top, PointsColDp, lineH);
                float nameX = pad + RankColDp + GapDp;
                UiBuilder.Place(Name.rectTransform, nameX, top, w - nameX - PointsColDp - pad - GapDp, lineH);
                float line2 = top + lineH + 2f;
                float pillH = Mathf.Ceil((pill.fontSize * LineK) + 2f);
                UiBuilder.Place(pill.rectTransform, nameX, line2, pillW, pillH);
                float subX = nameX + pillW + GapDp;
                UiBuilder.Place(Sub.rectTransform, subX, line2, SubWidth(w, pillW), h - line2 - 6f);
                Flag.Root.gameObject.SetActive(false);
            }

            private static float SubWidth(float w, float pillW)
            {
                const float pad = 10f;
                float nameX = pad + RankColDp + GapDp;
                return w - (nameX + pillW + GapDp) - pad;
            }

            // Autoajuste hasta 12 sp por la escala de texto (02 §12: al 130 %, nada por debajo de 15,6 sp).
            public static void Fit(TextMeshProUGUI text, float max, float s)
            {
                text.enableAutoSizing = true;
                text.fontSizeMax = max;
                text.fontSizeMin = Mathf.Min(max, UiBuilder.MinTextSp * Mathf.Max(1f, s));
            }

            private static string Loc(int n) => AF.Localization.Loc.Number(n);
        }
    }
}
