#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.LiveOps;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Calendario de sellos del Reto del río (tarea R9; 06 §4.1 «Pantalla»: «al ganar, el sello cae sobre el calendario del mes
    /// (cuadrícula de 28-31), +60 y el botón Cosecha doble»; nota §8): cuadrícula del mes con un casillero por día (28-31), los
    /// sellos (hoja de los CTA, <see cref="StampColor"/>) de <see cref="DailyChallengeRecord.StampDays"/>, y al ganar el sello del día que <b>cae</b> (animación por código de
    /// <see cref="StampDropMs"/> ms: escala 2,4 → 1 con un rebote corto y opacidad 0 → 1), el premio «+60» con el icono de semilla
    /// cuando se pagó (<see cref="DailyClaimReport.SeedsPaid"/>), «+1 Pororoca» si cerró una racha de 7, y las líneas de
    /// pendiente de validar o sin premio. Botón «Inicio» y, oculto, «Cosecha doble».
    /// <para><b>Cosecha doble:</b> necesita el enganche E9 en AF.Economy (nota §1.4: <c>Transaction.DoubleHarvest</c> es de un nivel
    /// de campaña y <c>Transaction.Earn</c> rechaza <c>SeedSource.DoubleHarvest</c>). Hasta que exista, el botón está creado pero
    /// <b>siempre oculto</b> (<see cref="DoubleHarvestShown"/> = false) aunque quien llama lo pida; ver
    /// <see cref="DoubleHarvestHookReady"/>.</para>
    /// <para>[P] La cuadrícula empieza en la primera columna (sin cabecera de días de la semana: el primer día de la semana
    /// depende de la región y el GDD no lo fija). Lienzo propio por encima del tablero (orden <see cref="SortingOrder"/>, por
    /// debajo de Ajustes, la tienda y los anuncios).</para>
    /// </summary>
    public sealed class DailyStampCalendar
    {
        /// <summary>
        /// Color del sello: la hoja de los CTA (<c>Palette.UiGreenButton</c>) con el aro crema. Nunca rojo: la guía v2 §7 reserva
        /// el rojo de la UI a <c>rojo_aviso</c>, y la heliconia es escena y FX, no UI (D-003).
        /// </summary>
        public static readonly Color StampColor = Palette.UiGreenButton;

        /// <summary>Orden del lienzo (encima del tablero; por debajo de Ajustes 40, tienda 50 y anuncio 60).</summary>
        public const int SortingOrder = 30;

        /// <summary>Duración de la caída del sello (ms) [P].</summary>
        public const float StampDropMs = 450f;

        /// <summary>Retraso del «+60» tras posarse el sello (ms) [P].</summary>
        public const float RewardDelayMs = 150f;

        /// <summary>Columnas de la cuadrícula.</summary>
        public const int Columns = 7;

        /// <summary>
        /// El enganche E9 (Cosecha doble del Reto en <c>Transaction</c>/<c>Wallet</c>) ya está: hasta entonces false y el botón no
        /// se enseña. Cuando el integrador aplique E9, se pone a true junto con la acción del botón.
        /// </summary>
        public const bool DoubleHarvestHookReady = false;

        private const float PanelW = 312f;
        private const float Cell = 36f;
        private const float Gap = 4f;
        private const string MonthFormat = "yyyy'-'MM";
        private const float PendingAlpha = 0.45f;

        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly ProceduralArt _art;
        private readonly TextMeshProUGUI _title;
        private readonly RectTransform _grid;
        private readonly Image[] _cells = new Image[31];
        private readonly TextMeshProUGUI[] _cellDays = new TextMeshProUGUI[31];
        private readonly Image[] _stamps = new Image[31];
        private readonly RectTransform _rewardRow;
        private readonly TextMeshProUGUI _reward;
        private readonly TextMeshProUGUI _status;
        private readonly ReliefParts _double;
        private readonly ReliefParts _home;
        private readonly List<int> _stampedShown = new List<int>(31);
        private int _days;
        private int _fallingDay;
        private float _dropMs = -1f;
        private float _rewardMs = -1f;
        private bool _rewardPending;
        private float _fallAlpha = 1f;

        /// <summary>Construye el calendario (oculto) en su lienzo propio bajo <paramref name="host"/>.</summary>
        public DailyStampCalendar(Transform host, ProceduralArt art, Action onHome, Action onDoubleHarvest)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            if (onHome == null)
            {
                throw new ArgumentNullException(nameof(onHome));
            }

            if (onDoubleHarvest == null)
            {
                throw new ArgumentNullException(nameof(onDoubleHarvest));
            }

            _canvas = new OverlayCanvas("DailyCalendarCanvas", host, SortingOrder);
            ModalKit.Dim("Dim", _canvas.Root, 0.55f);
            _panel = ModalKit.Card("CalendarPanel", _canvas.Root, art, out _surface);
            _title = ModalKit.Header(_panel, art, string.Empty, out Image plate);
            UiBuilder.Place(plate.rectTransform, 20f, 12f, PanelW - 40f, 44f);

            _grid = UiBuilder.Rect("Grid", _panel);
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = UiBuilder.Image("Day" + (i + 1), _grid, art.RoundedRect, new Color(Palette.UiPaper.r, Palette.UiPaper.g, Palette.UiPaper.b, 0.9f));
                _cellDays[i] = UiBuilder.Label("N", _cells[i].transform, TextStyle.PlainBold, string.Empty, 13f, Palette.UiWood, TextAlignmentOptions.Center);
                UiBuilder.Stretch(_cellDays[i].rectTransform);
                _stamps[i] = UiBuilder.Image("Stamp", _cells[i].transform, art.Disc, StampColor);
                Image ring = UiBuilder.Image("Ring", _stamps[i].transform, art.Ring, Palette.Cream);
                UiBuilder.Stretch(ring.rectTransform);
                UiBuilder.Place(_stamps[i].rectTransform, 4f, 4f, Cell - 8f, Cell - 8f);
                _stamps[i].gameObject.SetActive(false);
            }

            _rewardRow = UiBuilder.Rect("Reward", _panel);
            Image seed = UiBuilder.Image("Seed", _rewardRow, art.IconSeed, Color.white);
            UiBuilder.Place(seed.rectTransform, 0f, 4f, 28f, 28f);
            _reward = UiBuilder.Label("Amount", _rewardRow, TextStyle.Badge, string.Empty, 26f, Palette.Cream, TextAlignmentOptions.MidlineLeft);
            UiBuilder.Place(_reward.rectTransform, 34f, 0f, 120f, 36f);
            _status = ModalKit.Line("Status", _panel, string.Empty, 15f, Palette.UiInk);

            _double = ModalKit.Cta("DoubleHarvest", _panel, art.CtaWood, TextStyle.CtaWater, DailyText.DoubleHarvestLabel, 18f, onDoubleHarvest, out _);
            _home = ModalKit.Cta("Home", _panel, art.CtaLeafLarge, TextStyle.CtaLeaf, DailyText.HomeLabel, 20f, onHome, out _);
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>Se ve.</summary>
        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Casilleros del mes que se ven (28-31).</summary>
        public int DayCount => _days;

        /// <summary>Días con sello a la vista (incluido el que cae), en orden.</summary>
        public IReadOnlyList<int> StampedDays => _stampedShown;

        /// <summary>Día cuyo sello cae en esta apertura (0 si ninguno).</summary>
        public int FallingDay => _fallingDay;

        /// <summary>El sello que cae ya se posó (o no había ninguno).</summary>
        public bool StampLanded => _dropMs < 0f;

        /// <summary>Se ve el premio «+60».</summary>
        public bool RewardShown => _rewardRow.gameObject.activeSelf;

        /// <summary>Texto del premio.</summary>
        public string RewardText => _reward.text;

        /// <summary>Texto de estado (Pororoca, pendiente o sin premio), «» si no hay.</summary>
        public string StatusText => _status.gameObject.activeSelf ? _status.text : string.Empty;

        /// <summary>Título del calendario.</summary>
        public string TitleText => _title.text;

        /// <summary>Se ve el botón Cosecha doble (nunca hasta E9).</summary>
        public bool DoubleHarvestShown => _double.Button.gameObject.activeSelf;

        /// <summary>Botón «Inicio».</summary>
        public Button HomeButton => _home.Button;

        /// <summary>Botón Cosecha doble.</summary>
        public Button DoubleHarvestButton => _double.Button;

        /// <summary>Lienzo.</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>Escala del sello que cae (1 al posarse; para los tests).</summary>
        public float FallingScale => _fallingDay > 0 ? _stamps[_fallingDay - 1].rectTransform.localScale.x : 1f;

        /// <summary>
        /// Abre el calendario del mes de <paramref name="date"/> con los sellos de <paramref name="record"/>. Con
        /// <paramref name="claim"/> (la victoria de ahora), el sello del día reclamado cae si el cobro fue confirmado o quedó
        /// pendiente, y el premio sale según lo pagado; sin él (desde la tarjeta) solo se ven los sellos.
        /// <paramref name="offerDoubleHarvest"/> se ignora hasta E9 (<see cref="DoubleHarvestHookReady"/>).
        /// </summary>
        /// <param name="shuffleUnlockLevel">
        /// Nivel en que se desbloquea la Pororoca si aún no se puede usar (R-Q14 a: entra en el stock y espera; el calendario lo
        /// dice), o 0 si ya se puede.
        /// </param>
        public void Show(DateTime date, DailyChallengeRecord record, DailyClaimReport? claim, bool offerDoubleHarvest, int shuffleUnlockLevel = 0)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            DateTime month = date;
            bool fresh = claim.HasValue && claim.Value.Outcome != ClaimOutcome.NoReward
                && DailyChallengeRules.TryParseDateKey(claim.Value.DateKey, out month);
            if (!fresh)
            {
                month = date;
            }

            _canvas.Measure();
            _days = DateTime.DaysInMonth(month.Year, month.Month);
            _title.text = DailyText.CardTitle(fresh ? month : date);
            bool sameMonth = string.Equals(record.StampsMonth, month.ToString(MonthFormat, CultureInfo.InvariantCulture), StringComparison.Ordinal);
            _stampedShown.Clear();
            for (int i = 0; i < _cells.Length; i++)
            {
                bool inMonth = i < _days;
                _cells[i].gameObject.SetActive(inMonth);
                if (!inMonth)
                {
                    continue;
                }

                _cellDays[i].text = DayNumber(i + 1);
                bool stamped = sameMonth && record.StampDays.Contains(i + 1);
                _stamps[i].gameObject.SetActive(stamped);
                _stamps[i].rectTransform.localScale = Vector3.one;
                _stamps[i].color = StampColor;
                if (stamped)
                {
                    _stampedShown.Add(i + 1);
                }
            }

            _fallingDay = 0;
            _dropMs = -1f;
            _rewardMs = -1f;
            _rewardPending = false;
            _rewardRow.gameObject.SetActive(false);
            string status = string.Empty;
            if (claim.HasValue)
            {
                DailyClaimReport c = claim.Value;
                if (fresh && sameMonth && month.Day >= 1 && month.Day <= _days && record.StampDays.Contains(month.Day))
                {
                    _fallingDay = month.Day;
                    _dropMs = 0f;
                    Image stamp = _stamps[_fallingDay - 1];
                    stamp.gameObject.SetActive(true);
                    stamp.rectTransform.localScale = Vector3.one * 2.4f;
                    stamp.color = WithAlpha(StampColor, 0f);
                    _fallAlpha = c.Outcome == ClaimOutcome.Pending ? PendingAlpha : 1f;
                }

                if (c.SeedsPaid > 0)
                {
                    _reward.text = DailyText.RewardSeeds(c.SeedsPaid);
                    _rewardPending = true;
                    if (_fallingDay == 0)
                    {
                        _rewardRow.gameObject.SetActive(true);
                        _rewardPending = false;
                    }
                }

                if (c.Outcome == ClaimOutcome.Pending)
                {
                    status = DailyText.WinPendingLabel;
                }
                else if (c.Outcome == ClaimOutcome.NoReward)
                {
                    status = DailyText.NoRewardLabel;
                }
                else if (c.ShufflePaid > 0)
                {
                    status = shuffleUnlockLevel > 0 ? DailyText.StreakRewardLocked(shuffleUnlockLevel) : DailyText.StreakRewardLabel;
                }
            }

            _status.text = status;
            _status.gameObject.SetActive(status.Length > 0);
            bool offerDouble = DoubleHarvestHookReady && offerDoubleHarvest && claim.HasValue && claim.Value.SeedsPaid > 0;
            _double.Button.gameObject.SetActive(offerDouble);
            Layout();
            _canvas.Canvas.gameObject.SetActive(true);
        }

        /// <summary>Oculta el calendario.</summary>
        public void Hide()
        {
            _dropMs = -1f;
            _rewardMs = -1f;
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>Avanza la caída del sello y la entrada del premio. Sin asignaciones.</summary>
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

            if (_dropMs >= 0f && _fallingDay > 0)
            {
                _dropMs += dtMs;
                float t = Mathf.Clamp01(_dropMs / StampDropMs);
                // Cae desde 2,4× con aceleración y rebota un 8 % al posarse (últimos 25 %).
                float scale = t < 0.75f ? Mathf.Lerp(2.4f, 0.92f, EaseIn(t / 0.75f)) : Mathf.Lerp(0.92f, 1f, (t - 0.75f) / 0.25f);
                Image stamp = _stamps[_fallingDay - 1];
                stamp.rectTransform.localScale = new Vector3(scale, scale, 1f);
                stamp.color = WithAlpha(StampColor, Mathf.Clamp01(t * 2f) * _fallAlpha);
                if (t >= 1f)
                {
                    stamp.rectTransform.localScale = Vector3.one;
                    stamp.color = WithAlpha(StampColor, _fallAlpha);
                    _dropMs = -1f;
                    if (_rewardPending)
                    {
                        _rewardMs = 0f;
                    }
                }
            }

            if (_rewardMs >= 0f)
            {
                _rewardMs += dtMs;
                if (_rewardMs >= RewardDelayMs)
                {
                    _rewardMs = -1f;
                    _rewardPending = false;
                    _rewardRow.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>Termina las animaciones (el sello posado y el premio a la vista).</summary>
        public void Settle()
        {
            if (_dropMs >= 0f)
            {
                Tick(StampDropMs + 1f);
            }

            if (_rewardMs >= 0f || _rewardPending)
            {
                _rewardMs = -1f;
                _rewardPending = false;
                _rewardRow.gameObject.SetActive(true);
            }
        }

        private static string DayNumber(int day)
        {
            return AF.Localization.Loc.Number(day);
        }

        private static float EaseIn(float t)
        {
            return t * t;
        }

        private static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        private void Layout()
        {
            int rows = (_days + Columns - 1) / Columns;
            float gridW = (Columns * Cell) + ((Columns - 1) * Gap);
            float gridH = (rows * Cell) + ((rows - 1) * Gap);
            float y = 66f;
            UiBuilder.Place(_grid, (PanelW - gridW) * 0.5f, y, gridW, gridH);
            for (int i = 0; i < _days; i++)
            {
                int row = i / Columns;
                int col = i % Columns;
                UiBuilder.Place(_cells[i].rectTransform, col * (Cell + Gap), row * (Cell + Gap), Cell, Cell);
            }

            y += gridH + 14f;
            UiBuilder.Place(_rewardRow, (PanelW - 150f) * 0.5f, y, 150f, 36f);
            y += 40f;
            if (_status.gameObject.activeSelf)
            {
                y += ModalKit.PlaceLine(_status, 24f, y, PanelW - 48f, 22f) + 8f;
            }

            if (_double.Button.gameObject.activeSelf)
            {
                ModalKit.Place(_double, 36f, y, PanelW - 72f);
                y += ModalKit.HeightOf(_double) + 8f;
            }

            ModalKit.Place(_home, 36f, y, PanelW - 72f);
            y += ModalKit.HeightOf(_home) + 22f;
            float x = (_canvas.Width - PanelW) * 0.5f;
            float top = Mathf.Max(48f, (_canvas.Height - y) * 0.5f);
            ModalKit.Size(_panel, _surface, _art, x, top, PanelW, y);
        }
    }
}
