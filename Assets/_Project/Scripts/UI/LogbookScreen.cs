#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.Localization;
using AF.LiveOps;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>Desde dónde se abrió la libreta (sonda y botón).</summary>
    public enum LogbookOpenSource
    {
        /// <summary>La tarjeta del inicio.</summary>
        Card = 0,

        /// <summary>Sola, al entrar al inicio (el único takeover de la sesión; D-033, punto 5).</summary>
        Takeover = 1,

        /// <summary>La pantalla 2 de la secuencia del nivel 10 (06 §2.3; D-033, punto 11).</summary>
        L10 = 2,

        /// <summary>La fila «Bitácora · elige tu premio» de la victoria (D-033, punto 5; la dibuja el panel de B1b: Q-B11).</summary>
        Victory = 3,
    }

    /// <summary>
    /// Libreta de la Bitácora del río (B4; 06 §4.2 «Pantalla»; 06 §2.3, pantalla 2 del nivel 10; D-033, añadido del 2026-10-20,
    /// puntos 4-6 y 11), construida por código en su lienzo propio (orden <see cref="SortingOrder"/>, encima del inicio y de la
    /// victoria, por debajo de Ajustes, el Mapa, la tienda y los anuncios):
    /// <list type="bullet">
    /// <item><description>30 casillas en <b>5 filas × 6</b>, las de hito (7, 14, 21, 30) más grandes; las pasadas del ciclo con
    /// sello (solo el sello: la elección no se guarda, punto 6); la de hoy <b>pulsa</b> si se puede sellar; pie «Página {k} de
    /// 30 · Ciclo {c}».</description></item>
    /// <item><description>Al tocar la de hoy salen <b>3 tarjetas</b> (semillas / comodín / mezcla, en ese orden fijo) y lo que va
    /// fijo (Canasta, Canasta grande, marco) en una línea aparte; <b>un toque sella</b>, sin confirmación [P] (punto 4). Cerrar
    /// sin elegir la deja pendiente; nunca se elige sola.</description></item>
    /// <item><description>Tras sellar, el sello cae sobre la casilla (como en el calendario del Reto) y debajo sale lo recibido.</description></item>
    /// <item><description>En la secuencia del 10, además, «Cada día que juegues, sella una página» (06 §2.3) y un solo botón
    /// «Seguir»; en el resto, «Cerrar».</description></item>
    /// </list>
    /// Gráficos provisionales por código (sin arte de libreta). Ningún texto fuera de la tabla (<see cref="LogbookText"/>).
    /// </summary>
    public sealed class LogbookScreen
    {
        /// <summary>Orden del lienzo (encima del calendario del Reto, 30; por debajo de Ajustes, 40, y el Mapa, 45).</summary>
        public const int SortingOrder = 32;

        /// <summary>Columnas de la libreta (06 §4.2: «5 filas × 6»).</summary>
        public const int Columns = 6;

        /// <summary>Filas.</summary>
        public const int Rows = 5;

        /// <summary>Periodo del pulso de la casilla de hoy (ms) [P].</summary>
        public const float PulsePeriodMs = 900f;

        /// <summary>Amplitud del pulso (fracción de la escala) [P].</summary>
        public const float PulseAmplitude = 0.08f;

        /// <summary>Cuánto crecen las casillas de hito sobre las demás (dp por lado) [P].</summary>
        public const float MilestoneGrowDp = 3f;

        /// <summary>Duración de la caída del sello (ms), la del calendario del Reto.</summary>
        public const float StampDropMs = DailyStampCalendar.StampDropMs;

        private const float MaxPanelW = 328f;
        private const float Gap = 6f;
        private const float CardH = 72f;
        private const float CardGap = 8f;
        private const int MaxItems = 4;

        private readonly OverlayCanvas _canvas;
        private readonly ProceduralArt _art;
        private readonly Action<LoginCalendarChoice> _onChoose;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly TextMeshProUGUI _title;
        private readonly Image _titlePlate;
        private readonly TextMeshProUGUI _intro;
        private readonly RectTransform _grid;
        private readonly Button[] _cells = new Button[LoginCalendarTable.Days];
        private readonly Image[] _cellPlates = new Image[LoginCalendarTable.Days];
        private readonly TextMeshProUGUI[] _cellDays = new TextMeshProUGUI[LoginCalendarTable.Days];
        private readonly Image[] _stamps = new Image[LoginCalendarTable.Days];
        private readonly TextMeshProUGUI _footer;
        private readonly TextMeshProUGUI _status;
        private readonly RectTransform _choices;
        private readonly Button[] _cards = new Button[LoginCalendarTable.Choices];
        private readonly Image[] _cardPlates = new Image[LoginCalendarTable.Choices];
        private readonly ItemRow[] _cardItems = new ItemRow[LoginCalendarTable.Choices];
        private readonly ItemRow _extras;
        private readonly ItemRow _received;
        private readonly ReliefParts _button;
        private readonly TextMeshProUGUI _buttonLabel;
        private readonly List<int> _stampedShown = new List<int>(LoginCalendarTable.Days);

        private LoginCalendarSnapshot? _snapshot;
        private bool _choosing;
        private float _pulseMs;
        private int _fallingDay;
        private float _dropMs = -1f;

        /// <summary>Construye la libreta (oculta) bajo <paramref name="host"/>.</summary>
        /// <param name="host">Padre del lienzo.</param>
        /// <param name="art">Arte por código.</param>
        /// <param name="onChoose">Toque en una de las tres tarjetas: quien llama sella con esa opción y llama a <see cref="ShowClaimed"/>.</param>
        /// <param name="onClose">«Seguir» o «Cerrar».</param>
        public LogbookScreen(Transform host, ProceduralArt art, Action<LoginCalendarChoice> onChoose, Action onClose)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            _onChoose = onChoose ?? throw new ArgumentNullException(nameof(onChoose));
            if (onClose == null)
            {
                throw new ArgumentNullException(nameof(onClose));
            }

            _canvas = new OverlayCanvas("LogbookCanvas", host, SortingOrder);
            ModalKit.Dim("Dim", _canvas.Root, 0.55f);
            _panel = ModalKit.Card("LogbookPanel", _canvas.Root, art, out _surface);
            _title = ModalKit.Header(_panel, art, string.Empty, out _titlePlate);
            _intro = ModalKit.Line("Intro", _panel, string.Empty, 15f, Palette.UiInk);

            _grid = UiBuilder.Rect("Pages", _panel);
            for (int i = 0; i < _cells.Length; i++)
            {
                int day = i + 1;
                _cells[i] = UiBuilder.BareButton("Page" + day, _grid, () => PressCell(day));
                _cellPlates[i] = UiBuilder.Image("Plate", _cells[i].transform, art.RoundedRect, CellColor(false));
                UiBuilder.Stretch(_cellPlates[i].rectTransform);
                _cellDays[i] = UiBuilder.Label("N", _cells[i].transform, TextStyle.PlainBold, string.Empty, 14f, Palette.UiWood, TextAlignmentOptions.Center);
                UiBuilder.Stretch(_cellDays[i].rectTransform);
                _stamps[i] = UiBuilder.Image("Stamp", _cells[i].transform, art.Disc, DailyStampCalendar.StampColor);
                Image ring = UiBuilder.Image("Ring", _stamps[i].transform, art.Ring, Palette.Cream);
                UiBuilder.Stretch(ring.rectTransform);
                RectTransform stampRt = _stamps[i].rectTransform;
                stampRt.anchorMin = new Vector2(0.5f, 0.5f);
                stampRt.anchorMax = new Vector2(0.5f, 0.5f);
                stampRt.pivot = new Vector2(0.5f, 0.5f);
                stampRt.anchoredPosition = Vector2.zero;
                _stamps[i].gameObject.SetActive(false);
                RectTransform cellRt = (RectTransform)_cells[i].transform;
                cellRt.pivot = new Vector2(0.5f, 0.5f);
            }

            _footer = ModalKit.Line("Footer", _panel, string.Empty, 14f, Palette.UiWood);
            _status = ModalKit.Line("Status", _panel, string.Empty, 15f, Palette.UiInk);

            _choices = UiBuilder.Rect("Choices", _panel);
            for (int c = 0; c < _cards.Length; c++)
            {
                var choice = (LoginCalendarChoice)c;
                _cards[c] = UiBuilder.BareButton("Choice_" + LoginCalendarTable.ChoiceId(choice), _choices, () => PressChoice(choice));
                _cardPlates[c] = UiBuilder.Image("Plate", _cards[c].transform, art.RoundedRect, new Color(Palette.UiPaper.r, Palette.UiPaper.g, Palette.UiPaper.b, 0.95f));
                UiBuilder.Stretch(_cardPlates[c].rectTransform);
                _cardItems[c] = new ItemRow(_cards[c].transform, art, MaxItems, 15f);
            }

            _extras = new ItemRow(_panel, art, 2, 14f);
            _received = new ItemRow(_panel, art, MaxItems + 2, 15f);
            _button = ModalKit.Cta("Close", _panel, art.CtaLeafLarge, TextStyle.CtaLeaf, string.Empty, 20f, onClose, out _buttonLabel);
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>Se ve.</summary>
        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Desde dónde se abrió.</summary>
        public LogbookOpenSource Source { get; private set; }

        /// <summary>Se ven las tres tarjetas (en lugar de la libreta).</summary>
        public bool Choosing => Visible && _choosing;

        /// <summary>La casilla de hoy pulsa (se puede sellar y no se están viendo las tarjetas).</summary>
        public bool TodayPulsing => Visible && !_choosing && _snapshot != null && _snapshot.Ready;

        /// <summary>La casilla que toca (la de hoy).</summary>
        public int TodayDay => _snapshot?.NextDay ?? 0;

        /// <summary>Casillas con sello a la vista (incluida la que cae), en orden.</summary>
        public IReadOnlyList<int> StampedDays => _stampedShown;

        /// <summary>Casilla cuyo sello cae (0 si ninguna).</summary>
        public int FallingDay => _fallingDay;

        /// <summary>El sello que cae ya se posó.</summary>
        public bool StampLanded => _dropMs < 0f;

        /// <summary>Texto de la cabecera.</summary>
        public string TitleText => _title.text;

        /// <summary>Se ve «Cada día que juegues, sella una página».</summary>
        public bool IntroShown => _intro.gameObject.activeSelf;

        /// <summary>Texto del pie.</summary>
        public string FooterText => _footer.text;

        /// <summary>Texto de estado.</summary>
        public string StatusText => _status.text;

        /// <summary>Texto del botón.</summary>
        public string ButtonText => _buttonLabel.text;

        /// <summary>«Seguir» o «Cerrar».</summary>
        public Button CloseButton => _button.Button;

        /// <summary>Botón de la casilla <paramref name="day"/> (1-30).</summary>
        public Button CellButton(int day)
        {
            return _cells[day - 1];
        }

        /// <summary>Rectángulo de la casilla <paramref name="day"/> (sonda y tests).</summary>
        public RectTransform CellRect(int day)
        {
            return (RectTransform)_cells[day - 1].transform;
        }

        /// <summary>Lado de la casilla <paramref name="day"/> (dp; las de hito, más grandes).</summary>
        public float CellSize(int day)
        {
            return CellRect(day).sizeDelta.x;
        }

        /// <summary>Tarjeta de <paramref name="choice"/>.</summary>
        public Button CardButton(LoginCalendarChoice choice)
        {
            return _cards[(int)choice];
        }

        /// <summary>Textos de la tarjeta de <paramref name="choice"/>, en orden.</summary>
        public IReadOnlyList<string> CardTexts(LoginCalendarChoice choice)
        {
            return _cardItems[(int)choice].Texts;
        }

        /// <summary>Textos de lo fijo (Canasta, marco) bajo las tarjetas.</summary>
        public IReadOnlyList<string> ExtrasTexts => _extras.Texts;

        /// <summary>Textos de lo recibido tras sellar.</summary>
        public IReadOnlyList<string> ReceivedTexts => _received.Texts;

        /// <summary>Lienzo.</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>Raíz del panel (sonda <c>AF_LAYOUT</c>).</summary>
        public RectTransform Panel => _panel;

        /// <summary>Abre la libreta con <paramref name="snapshot"/> desde <paramref name="source"/>.</summary>
        public void Show(LoginCalendarSnapshot snapshot, LogbookOpenSource source)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            Source = source;
            _choosing = false;
            _fallingDay = 0;
            _dropMs = -1f;
            _pulseMs = 0f;
            _received.Set(Array.Empty<Item>(), 0);
            _canvas.Measure();
            Render();
            _canvas.Canvas.gameObject.SetActive(true);
        }

        /// <summary>
        /// Tras sellar (<paramref name="claim"/>): vuelve a la libreta con <paramref name="after"/>, el sello cae sobre la casilla
        /// sellada y debajo sale lo recibido.
        /// </summary>
        public void ShowClaimed(LoginCalendarClaim claim, LoginCalendarSnapshot after)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            _snapshot = after ?? throw new ArgumentNullException(nameof(after));
            _choosing = false;
            var items = new Item[MaxItems + 2];
            int n = Fill(items, 0, claim.Reward);
            n = FillExtras(items, n, claim.Basket, claim.FrameId);
            _received.Set(items, n);
            Render(claim.Day);
            if (claim.Day >= 1 && claim.Day <= LoginCalendarTable.Days)
            {
                _fallingDay = claim.Day;
                _dropMs = 0f;
                Image stamp = _stamps[_fallingDay - 1];
                _cells[_fallingDay - 1].transform.SetAsLastSibling();
                stamp.gameObject.SetActive(true);
                stamp.rectTransform.localScale = Vector3.one * 2.4f;
                stamp.color = WithAlpha(DailyStampCalendar.StampColor, 0f);
                if (!_stampedShown.Contains(_fallingDay))
                {
                    _stampedShown.Add(_fallingDay);
                }
            }
        }

        /// <summary>Toque en la casilla de hoy (lo que hace su botón): con la casilla lista, salen las tres tarjetas.</summary>
        public bool PressToday()
        {
            return _snapshot != null && PressCell(_snapshot.NextDay);
        }

        /// <summary>Toque en la tarjeta de <paramref name="choice"/> (lo que hace su botón).</summary>
        public bool PressChoice(LoginCalendarChoice choice)
        {
            if (!Choosing)
            {
                return false;
            }

            _onChoose(choice);
            return true;
        }

        /// <summary>Oculta la libreta (la casilla, si no se eligió, queda pendiente).</summary>
        public void Hide()
        {
            _choosing = false;
            _dropMs = -1f;
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>Avanza el pulso de la casilla de hoy y la caída del sello. Sin asignaciones.</summary>
        public void Tick(float dtMs)
        {
            if (!Visible)
            {
                return;
            }

            if (_canvas.Measure())
            {
                Layout();
            }

            if (TodayPulsing)
            {
                _pulseMs = (_pulseMs + dtMs) % PulsePeriodMs;
                float s = 1f + (PulseAmplitude * Mathf.Sin(_pulseMs / PulsePeriodMs * Mathf.PI * 2f));
                _cells[_snapshot!.NextDay - 1].transform.localScale = new Vector3(s, s, 1f);
            }

            if (_dropMs >= 0f && _fallingDay > 0)
            {
                _dropMs += dtMs;
                float t = Mathf.Clamp01(_dropMs / StampDropMs);
                float scale = t < 0.75f ? Mathf.Lerp(2.4f, 0.92f, (t / 0.75f) * (t / 0.75f)) : Mathf.Lerp(0.92f, 1f, (t - 0.75f) / 0.25f);
                Image stamp = _stamps[_fallingDay - 1];
                stamp.rectTransform.localScale = new Vector3(scale, scale, 1f);
                stamp.color = WithAlpha(DailyStampCalendar.StampColor, Mathf.Clamp01(t * 2f));
                if (t >= 1f)
                {
                    Settle();
                }
            }
        }

        /// <summary>Termina las animaciones (el sello posado).</summary>
        public void Settle()
        {
            if (_fallingDay > 0)
            {
                Image stamp = _stamps[_fallingDay - 1];
                stamp.rectTransform.localScale = Vector3.one;
                stamp.color = DailyStampCalendar.StampColor;
            }

            _dropMs = -1f;
        }

        private bool PressCell(int day)
        {
            LoginCalendarSnapshot? snap = _snapshot;
            if (!Visible || _choosing || snap == null || !snap.Ready || day != snap.NextDay)
            {
                return false;
            }

            _choosing = true;
            _cells[day - 1].transform.localScale = Vector3.one;
            var items = new Item[MaxItems];
            for (int c = 0; c < _cards.Length; c++)
            {
                int n = Fill(items, 0, snap.Options[c]);
                _cardItems[c].Set(items, n);
            }

            var extras = new Item[2];
            _extras.Set(extras, FillExtras(extras, 0, snap.Basket, snap.FrameId));
            Render();
            return true;
        }

        // La libreta o las tarjetas, según el estado. sealedDay: la casilla que se acaba de sellar (su página en el pie).
        private void Render(int sealedDay = 0)
        {
            LoginCalendarSnapshot snap = _snapshot!;
            if (sealedDay == 0 && snap.State == LoginCalendarState.Sealed && snap.NextDay == 1 && snap.Cycle > 1)
            {
                sealedDay = LoginCalendarTable.Days; // hoy se selló la 30: hasta mañana se ve el ciclo que se cerró, entero
            }

            _title.text = LogbookText.Title;
            bool l10 = Source == LogbookOpenSource.L10;
            _intro.text = l10 ? LogbookText.Intro : string.Empty;
            _intro.gameObject.SetActive(l10 && !_choosing);
            _buttonLabel.text = LogbookText.Plain(l10 ? LogbookText.Keys.Continue : LogbookText.Keys.Close);
            int page = sealedDay > 0 ? sealedDay : snap.NextDay;
            int cycle = sealedDay > 0 && snap.NextDay == 1 && sealedDay == LoginCalendarTable.Days ? snap.Cycle - 1 : snap.Cycle;
            _footer.text = LogbookText.Footer(page, cycle);
            _status.text = LogbookText.Plain(StatusKey(snap.State, _choosing));

            // Casillas: con la 30 recién sellada (otro ciclo), la libreta enseña el ciclo que se cerró, entero.
            int sealedUpTo = sealedDay == LoginCalendarTable.Days && snap.NextDay == 1 ? LoginCalendarTable.Days : snap.NextDay - 1;
            _stampedShown.Clear();
            for (int i = 0; i < _cells.Length; i++)
            {
                int day = i + 1;
                bool stamped = day <= sealedUpTo;
                _stamps[i].gameObject.SetActive(stamped);
                _stamps[i].rectTransform.localScale = Vector3.one;
                _stamps[i].color = DailyStampCalendar.StampColor;
                _cellDays[i].text = Loc.Number(day);
                _cellPlates[i].color = CellColor(LoginCalendarTable.IsMilestone(day));
                _cells[i].transform.localScale = Vector3.one;
                if (stamped)
                {
                    _stampedShown.Add(day);
                }
            }

            _grid.gameObject.SetActive(!_choosing);
            _choices.gameObject.SetActive(_choosing);
            _extras.Root.gameObject.SetActive(_choosing && _extras.Count > 0);
            _received.Root.gameObject.SetActive(!_choosing && _received.Count > 0);
            Layout();
        }

        private static string StatusKey(LoginCalendarState state, bool choosing)
        {
            if (choosing)
            {
                return LogbookText.Keys.StatusChoose;
            }

            switch (state)
            {
                case LoginCalendarState.Ready: return LogbookText.Keys.StatusReady;
                case LoginCalendarState.Sealed: return LogbookText.Keys.StatusSealed;
                default: return LogbookText.Keys.StatusPlay;
            }
        }

        private void Layout()
        {
            float w = Mathf.Min(MaxPanelW, _canvas.Width - 32f);
            float inner = w - 32f;
            UiBuilder.Place(_titlePlate.rectTransform, 20f, 12f, w - 40f, 44f);
            float y = 64f;
            if (_intro.gameObject.activeSelf)
            {
                y += ModalKit.PlaceLine(_intro, 16f, y, inner, 22f) + 6f;
            }

            if (!_choosing)
            {
                float cell = Mathf.Floor((inner - ((Columns - 1) * Gap)) / Columns);
                float gridW = (Columns * cell) + ((Columns - 1) * Gap);
                float gridH = (Rows * cell) + ((Rows - 1) * Gap);
                UiBuilder.Place(_grid, (w - gridW) * 0.5f, y, gridW, gridH);
                for (int i = 0; i < _cells.Length; i++)
                {
                    int row = i / Columns;
                    int col = i % Columns;
                    float grow = LoginCalendarTable.IsMilestone(i + 1) ? MilestoneGrowDp : 0f;
                    RectTransform rt = (RectTransform)_cells[i].transform;
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(cell + (2f * grow), cell + (2f * grow));
                    rt.anchoredPosition = new Vector2((col * (cell + Gap)) + (cell * 0.5f), -((row * (cell + Gap)) + (cell * 0.5f)));
                    _stamps[i].rectTransform.sizeDelta = new Vector2(cell - 10f, cell - 10f);
                }

                y += gridH + 10f;
            }

            y += ModalKit.PlaceLine(_footer, 16f, y, inner, 20f) + 4f;
            y += ModalKit.PlaceLine(_status, 16f, y, inner, 22f) + 8f;
            if (_choosing)
            {
                float h = CardH * UiBuilder.TextScale;
                UiBuilder.Place(_choices, 16f, y, inner, (h * _cards.Length) + (CardGap * (_cards.Length - 1)));
                for (int c = 0; c < _cards.Length; c++)
                {
                    UiBuilder.Place((RectTransform)_cards[c].transform, 0f, c * (h + CardGap), inner, h);
                    _cardItems[c].Layout(8f, 8f, inner - 16f, h - 16f);
                }

                y += (h * _cards.Length) + (CardGap * (_cards.Length - 1)) + 8f;
                if (_extras.Root.gameObject.activeSelf)
                {
                    UiBuilder.Place(_extras.Root, 16f, y, inner, 26f * UiBuilder.TextScale);
                    _extras.Layout(0f, 0f, inner, 26f * UiBuilder.TextScale);
                    y += (26f * UiBuilder.TextScale) + 8f;
                }
            }
            else if (_received.Root.gameObject.activeSelf)
            {
                float h = 52f * UiBuilder.TextScale;
                UiBuilder.Place(_received.Root, 16f, y, inner, h);
                _received.Layout(0f, 0f, inner, h);
                y += h + 8f;
            }

            ModalKit.Place(_button, 36f, y, w - 72f);
            y += ModalKit.HeightOf(_button) + 22f;
            float x = (_canvas.Width - w) * 0.5f;
            float top = Mathf.Max(48f, (_canvas.Height - y) * 0.5f);
            ModalKit.Size(_panel, _surface, _art, x, top, w, y);
        }

        // Las filas de un premio (semillas primero y después los comodines, en el orden de BoosterKind), desde from.
        private int Fill(Item[] into, int from, LoginCalendarReward reward)
        {
            int n = from;
            if (reward.Seeds > 0 && n < into.Length)
            {
                into[n++] = new Item(_art.IconSeed, LogbookText.Seeds(reward.Seeds));
            }

            for (int k = 0; k < BoosterKinds.Count && n < into.Length; k++)
            {
                var kind = (BoosterKind)k;
                int units = reward.Stock.Get(kind);
                if (units > 0)
                {
                    into[n++] = new Item(IconOf(kind), LogbookText.Units(KeyOf(kind), units));
                }
            }

            return n;
        }

        private int FillExtras(Item[] into, int from, BasketContents basket, string? frame)
        {
            int n = from;
            if (!basket.IsNone && n < into.Length)
            {
                string key = basket.Kind == BasketKind.Grand ? LogbookText.Keys.ItemGrandBasket : LogbookText.Keys.ItemBasket;
                into[n++] = new Item(_art.EmptyBasket, LogbookText.Plain(key));
            }

            if (frame != null && n < into.Length)
            {
                into[n++] = new Item(_art.Ring, LogbookText.Plain(LogbookText.Keys.ItemFrame));
            }

            return n;
        }

        private Sprite IconOf(BoosterKind kind)
        {
            switch (kind)
            {
                case BoosterKind.Undo: return _art.IconUndo;
                case BoosterKind.Hint: return _art.IconMorpho;
                case BoosterKind.Reveal: return _art.IconFirefly;
                case BoosterKind.ExtraCuia: return _art.IconCuiaPlus;
                default: return _art.Disc;
            }
        }

        private static string KeyOf(BoosterKind kind)
        {
            switch (kind)
            {
                case BoosterKind.Undo: return LogbookText.Keys.ItemUndo;
                case BoosterKind.Hint: return LogbookText.Keys.ItemHint;
                case BoosterKind.Reveal: return LogbookText.Keys.ItemReveal;
                default: return LogbookText.Keys.ItemExtraCuia;
            }
        }

        private static Color CellColor(bool milestone)
        {
            Color c = milestone ? Palette.UiAmber : Palette.UiPaper;
            return new Color(c.r, c.g, c.b, milestone ? 0.55f : 0.9f);
        }

        private static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        /// <summary>Una fila de premio: icono y una cadena de la tabla.</summary>
        internal readonly struct Item
        {
            public Item(Sprite icon, string text)
            {
                Icon = icon;
                Text = text;
            }

            public Sprite Icon { get; }

            public string Text { get; }
        }

        // Filas de premio (icono + texto) en una caja, de dos en dos por línea. Los huecos se crean una vez.
        private sealed class ItemRow
        {
            private const float IconDp = 22f;
            private readonly Image[] _icons;
            private readonly TextMeshProUGUI[] _labels;
            private readonly List<string> _texts;

            public ItemRow(Transform parent, ProceduralArt art, int slots, float size)
            {
                Root = UiBuilder.Rect("Items", parent);
                _icons = new Image[slots];
                _labels = new TextMeshProUGUI[slots];
                _texts = new List<string>(slots);
                for (int i = 0; i < slots; i++)
                {
                    _icons[i] = UiBuilder.Image("Icon" + i, Root, art.Disc, Color.white);
                    _labels[i] = UiBuilder.Label("Text" + i, Root, TextStyle.PlainBold, string.Empty, size, Palette.UiInk, TextAlignmentOptions.MidlineLeft);
                    UiBuilder.AutoSize(_labels[i], UiBuilder.MinTextSp, size);
                    _icons[i].gameObject.SetActive(false);
                    _labels[i].gameObject.SetActive(false);
                }
            }

            public RectTransform Root { get; }

            public int Count => _texts.Count;

            public IReadOnlyList<string> Texts => _texts;

            public void Set(Item[] items, int count)
            {
                _texts.Clear();
                for (int i = 0; i < _icons.Length; i++)
                {
                    bool on = i < count;
                    _icons[i].gameObject.SetActive(on);
                    _labels[i].gameObject.SetActive(on);
                    if (on)
                    {
                        _icons[i].sprite = items[i].Icon;
                        _labels[i].text = items[i].Text;
                        _texts.Add(items[i].Text);
                    }
                }
            }

            // Dos filas por línea, centradas en vertical en h.
            public void Layout(float x, float y, float w, float h)
            {
                int n = _texts.Count;
                if (n == 0)
                {
                    return;
                }

                int lines = (n + 1) / 2;
                float lineH = Mathf.Min(h / lines, 28f * UiBuilder.TextScale);
                float top = y + ((h - (lineH * lines)) * 0.5f);
                float colW = w * 0.5f;
                for (int i = 0; i < n; i++)
                {
                    int line = i / 2;
                    int col = i % 2;
                    bool alone = n % 2 == 1 && i == n - 1;
                    float cx = alone ? x + (w * 0.25f) : x + (col * colW);
                    float cy = top + (line * lineH);
                    UiBuilder.Place(_icons[i].rectTransform, cx + 4f, cy + ((lineH - IconDp) * 0.5f), IconDp, IconDp);
                    UiBuilder.Place(_labels[i].rectTransform, cx + IconDp + 10f, cy, colW - IconDp - 14f, lineH);
                }
            }
        }
    }
}
