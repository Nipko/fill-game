#nullable enable
using System;
using System.Collections.Generic;
using AF.LiveOps;
using AF.Presentation;
using AF.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>Lo que la Liga sabe del jugador para la cabecera de «Tu avatar» (avatares.md §3.2, punto 1).</summary>
    internal readonly struct AvatarLeagueInfo
    {
        public AvatarLeagueInfo(string? nameKey, int nameNum, string? country)
        {
            NameKey = nameKey;
            NameNum = nameNum;
            Country = country;
        }

        /// <summary>El animal del seudónimo, o null si la Liga aún no dio seudónimo.</summary>
        public string? NameKey { get; }

        public int NameNum { get; }

        public string? Country { get; }
    }

    /// <summary>
    /// «Tu avatar» (D-056; <c>docs/dev/avatares.md</c> §3.2 y §3.6, Q-AV-6, Q-AV-8 y Q-AV-10 con la recomendada): un modal de la familia
    /// de Ajustes (tarjeta de madera clara, X arriba a la derecha; el Atrás de Android lo cierra <c>GameBootstrap</c>), construido por
    /// código. De arriba abajo:
    /// <list type="number">
    /// <item>la cabecera: el retrato elegido a 112 dp, el nombre del animal y, si la Liga ya dio seudónimo, «En la Liga: Capibara 4821»
    /// con su bandera y el país (sin estadísticas en la 1.0);</item>
    /// <item>la rejilla de los avatares (4 columnas a 360 dp, 3 a 320 dp o con el texto al 130 %), primero los libres y después los de
    /// hito, con el nombre debajo en una o dos líneas; se desplaza si no cabe. El de la Liga no sale con la Liga apagada;</item>
    /// <item>los estados: elegido (aro de hoja de 3 dp y la chapa ✓), libre, nuevo (chapa «Nuevo» en heliconia) y bloqueado (silueta,
    /// chapa del hito y su texto corto).</item>
    /// </list>
    /// Tocar uno libre lo elige en el acto (lo hace <c>GameBootstrap</c> con <see cref="Tapped"/>; aquí, el rebote de 150 ms del retrato
    /// o un fundido con Reducir movimiento); tocar uno bloqueado lo sacude ±8 dp (sin sacudida con Reducir movimiento) y saca un globo
    /// con la frase entera del hito. TalkBack lee cada celda entera («{animal}, elegido», «{animal}, bloqueado. {hito}»…) y la cabecera.
    /// Las celdas pintan sus retratos del atlas perezoso (<see cref="AvatarAtlas"/>), que pone y libera <c>GameBootstrap</c>.
    /// </summary>
    internal sealed class AvatarScreen
    {
        /// <summary>Orden del lienzo: encima de Ajustes (40), desde donde también se abre.</summary>
        public const int SortingOrder = 42;

        /// <summary>Ancho máximo de la tarjeta (dp), como Ajustes.</summary>
        public const float PanelMaxWidth = 344f;

        /// <summary>Retrato de la cabecera (dp).</summary>
        public const float HeaderPortraitDp = 112f;

        /// <summary>Retrato de una celda (dp).</summary>
        public const float CellPortraitDp = 64f;

        /// <summary>Duración del rebote o el fundido al elegir (ms).</summary>
        public const float ChooseMs = 150f;

        /// <summary>Duración de la sacudida de un bloqueado (ms) y su amplitud (dp).</summary>
        public const float ShakeMs = 300f;

        public const float ShakeDp = 8f;

        /// <summary>Id de la pantalla en la sonda (<c>AF_LAYOUT avatar</c>).</summary>
        public const string ProbeScreen = "avatar";

        private const float NameSp = 13f;
        private const float GoalSp = 12f;
        private const float HeaderNameSp = 22f;
        private const float AliasSp = 13f;
        private const float BalloonSp = 14f;

        private readonly ProceduralArt _art;
        private readonly DevProbe? _probe;
        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly TextMeshProUGUI _title;
        private readonly Image _header;
        private readonly ReliefParts _close;
        private readonly Image _portrait;
        private readonly TextMeshProUGUI _name;
        private readonly TextMeshProUGUI _alias;
        private readonly LeagueFlag _flag;
        private readonly TextMeshProUGUI _country;
        private readonly RectTransform _viewport;
        private readonly RectTransform _content;
        private readonly ScrollRect _scroll;
        private readonly RectTransform _balloon;
        private readonly TextMeshProUGUI _balloonText;
        private readonly List<Cell> _cells = new List<Cell>(AvatarCatalog.Count);
        private AvatarService? _service;
        private AvatarAtlas? _atlas;
        private AvatarLeagueInfo _league;
        private bool _leagueEnabled;
        private int _maxLevel;
        private string _headerA11y = string.Empty;
        private int _balloonCell = -1;
        private int _shakeCell = -1;
        private float _shakeMs;
        private float _chooseMs = -1f;
        private int _columns = 4;
        private bool _scrollToBalloon;

        public AvatarScreen(Transform host, ProceduralArt art, DevProbe? probe, Action onClose)
        {
            _art = art;
            _probe = probe;
            Action close = onClose ?? throw new ArgumentNullException(nameof(onClose));
            _canvas = new OverlayCanvas("AvatarCanvas", host, SortingOrder);
            ModalKit.Dim("Dim", _canvas.Root, 0.62f);
            _panel = ModalKit.Card("Panel", _canvas.Root, art, out _surface);
            _title = ModalKit.Header(_panel, art, AvatarText.Title, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, () => close());

            _portrait = UiBuilder.Image("Portrait", _panel, null, Color.white);
            _portrait.preserveAspect = true;
            _name = UiBuilder.Label("Name", _panel, TextStyle.PlainBold, string.Empty, HeaderNameSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
            _name.textWrappingMode = TextWrappingModes.Normal;
            _alias = UiBuilder.Label("Alias", _panel, TextStyle.Body, string.Empty, AliasSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
            _alias.textWrappingMode = TextWrappingModes.Normal;
            _flag = new LeagueFlag("Flag", _panel, art);
            _country = UiBuilder.Label("Country", _panel, TextStyle.Body, string.Empty, AliasSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
            _country.textWrappingMode = TextWrappingModes.Normal;
            A11yLabels.SetText(_name, () => _headerA11y); // la cabecera se lee entera en un elemento
            A11yLabels.Set(_alias, () => string.Empty);
            A11yLabels.Set(_country, () => string.Empty);

            _viewport = UiBuilder.Rect("Viewport", _panel);
            _viewport.gameObject.AddComponent<RectMask2D>();
            _viewport.gameObject.AddComponent<HitArea>().raycastTarget = true;
            _content = UiBuilder.Rect("Content", _viewport);
            _scroll = _viewport.gameObject.AddComponent<FlingStopScrollRect>();
            _scroll.viewport = _viewport;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;

            for (int i = 0; i < AvatarCatalog.Count; i++)
            {
                _cells.Add(new Cell(this, i, _content, art));
            }

            _balloon = UiBuilder.Rect("Balloon", _content);
            Image balloonPlate = UiBuilder.Sliced("Plate", _balloon, art.WoodPlate, Color.white);
            UiBuilder.Stretch(balloonPlate.rectTransform);
            _balloonText = UiBuilder.Label("Text", _balloon, TextStyle.PillWood, string.Empty, BalloonSp, Palette.Cream, TextAlignmentOptions.Center);
            _balloonText.textWrappingMode = TextWrappingModes.Normal;
            A11yLabels.Set(_balloonText, () => string.Empty); // lo dice la celda
            _balloon.gameObject.SetActive(false);
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>Al tocar una celda (su id): lo fija <c>GameBootstrap</c>, que elige (o no) y llama a <see cref="Chosen"/> o <see cref="Rejected"/>.</summary>
        public Action<string>? Tapped { get; set; }

        /// <summary>Reducir movimiento: sin rebote ni sacudida (un fundido de 150 ms).</summary>
        public bool ReduceMotion { get; set; }

        /// <summary>La pantalla está abierta.</summary>
        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        public Canvas Canvas => _canvas.Canvas;

        public Button CloseButton => _close.Button;

        internal RectTransform Panel => _panel;

        internal RectTransform Viewport => _viewport;

        /// <summary>Columnas de la rejilla ahora.</summary>
        public int Columns => _columns;

        /// <summary>El nombre de la cabecera.</summary>
        public string NameText => _name.text;

        /// <summary>«En la Liga: …», o «» si no se ve.</summary>
        public string AliasText => _alias.gameObject.activeSelf ? _alias.text : string.Empty;

        /// <summary>El país de la cabecera, o «».</summary>
        public string CountryText => _country.gameObject.activeSelf ? _country.text : string.Empty;

        /// <summary>Lo que lee TalkBack de la cabecera.</summary>
        public string HeaderA11y => _headerA11y;

        /// <summary>El retrato de la cabecera.</summary>
        public Sprite? PortraitSprite => _portrait.sprite;

        /// <summary>El globo del hito está a la vista, con su texto.</summary>
        public string BalloonText => _balloon.gameObject.activeSelf ? _balloonText.text : string.Empty;

        /// <summary>El recuadro del globo (tests de 130 %).</summary>
        internal RectTransform BalloonRect => _balloon;

        /// <summary>Hay un rebote o una sacudida en marcha.</summary>
        public bool Animating => _shakeCell >= 0 || _chooseMs >= 0f;

        /// <summary>Los ids de las celdas a la vista, en orden.</summary>
        public IReadOnlyList<string> ShownIds
        {
            get
            {
                var ids = new List<string>(_cells.Count);
                for (int i = 0; i < _cells.Count; i++)
                {
                    if (_cells[i].Root.gameObject.activeSelf)
                    {
                        ids.Add(_cells[i].Avatar.Id);
                    }
                }

                return ids;
            }
        }

        /// <summary>El botón de la celda de <paramref name="id"/>.</summary>
        public Button CellButton(string id) => CellOf(id).Button;

        /// <summary>El recuadro de la celda de <paramref name="id"/>.</summary>
        internal RectTransform CellRect(string id) => CellOf(id).Root;

        /// <summary>Lo que lee TalkBack de la celda de <paramref name="id"/>.</summary>
        public string CellA11y(string id) => CellOf(id).Reading;

        /// <summary>La celda de <paramref name="id"/> está elegida (aro y ✓).</summary>
        public bool CellChosen(string id) => CellOf(id).ChosenShown;

        /// <summary>La celda de <paramref name="id"/> lleva la chapa «Nuevo».</summary>
        public bool CellNew(string id) => CellOf(id).NewShown;

        /// <summary>La celda de <paramref name="id"/> está bloqueada.</summary>
        public bool CellLocked(string id) => CellOf(id).Locked;

        /// <summary>El texto corto del hito bajo la celda (vacío si no está bloqueada).</summary>
        public string CellGoal(string id) => CellOf(id).GoalText;

        /// <summary>Todos los textos a la vista (tests: ≥ 12 sp y sin recortes).</summary>
        internal IEnumerable<TextMeshProUGUI> Texts()
        {
            yield return _title;
            yield return _name;
            if (_alias.gameObject.activeSelf)
            {
                yield return _alias;
            }

            if (_country.gameObject.activeSelf)
            {
                yield return _country;
            }

            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i].Root.gameObject.activeSelf)
                {
                    foreach (TextMeshProUGUI t in _cells[i].Texts())
                    {
                        yield return t;
                    }
                }
            }

            if (_balloon.gameObject.activeSelf)
            {
                yield return _balloonText;
            }
        }

        /// <summary>El fondo del retrato de <paramref name="avatar"/>.</summary>
        public static AvatarArtBackdrop BackdropOf(AvatarInfo avatar)
        {
            switch (avatar.Backdrop)
            {
                case AvatarBackdrop.Water: return AvatarArtBackdrop.Water;
                case AvatarBackdrop.Wood: return AvatarArtBackdrop.Wood;
                case AvatarBackdrop.Petroleum: return AvatarArtBackdrop.Petroleum;
                default: return AvatarArtBackdrop.Leaf;
            }
        }

        /// <summary>El icono de la chapa de <paramref name="avatar"/> bloqueado.</summary>
        public static AvatarArtBadge BadgeOf(AvatarInfo avatar)
        {
            switch (avatar.Milestone)
            {
                case AvatarMilestone.HardWin: return AvatarArtBadge.Hard;
                case AvatarMilestone.VeryHardWin: return AvatarArtBadge.VeryHard;
                case AvatarMilestone.World3:
                case AvatarMilestone.World4:
                case AvatarMilestone.World5:
                case AvatarMilestone.World6:
                    return AvatarArtBadge.World;
                case AvatarMilestone.Streak10: return AvatarArtBadge.Streak;
                case AvatarMilestone.DailyStreak7:
                case AvatarMilestone.DailyStamps20:
                    return AvatarArtBadge.Daily;
                case AvatarMilestone.League50: return AvatarArtBadge.League;
                default: return AvatarArtBadge.None;
            }
        }

        /// <summary>
        /// Abre la pantalla con lo de ahora: el servicio (el elegido y lo desbloqueado), el retrato de la cabecera, el atlas de las
        /// celdas, si la Liga está encendida (el ocelote), <c>max_level</c> (los niveles que faltan) y el seudónimo de la Liga.
        /// </summary>
        public void Open(AvatarService service, Sprite portrait, AvatarAtlas atlas, bool leagueEnabled, int maxLevel, AvatarLeagueInfo league)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
            _canvas.Canvas.gameObject.SetActive(true);
            _content.anchoredPosition = Vector2.zero; // arriba del todo al abrir
            _balloonCell = -1;
            _shakeCell = -1;
            _chooseMs = -1f;
            Refresh(portrait, leagueEnabled, maxLevel, league);
        }

        /// <summary>Repinta con lo de ahora (tras elegir o al llegar un desbloqueo con la pantalla abierta).</summary>
        public void Refresh(Sprite portrait, bool leagueEnabled, int maxLevel, AvatarLeagueInfo league)
        {
            AvatarService? service = _service;
            AvatarAtlas? atlas = _atlas;
            if (service == null || atlas == null)
            {
                return;
            }

            _leagueEnabled = leagueEnabled;
            _maxLevel = maxLevel;
            _league = league;
            AvatarData data = service.Data;
            AvatarInfo current = service.Current;
            _portrait.sprite = portrait;
            _name.text = AvatarText.Name(current);
            bool alias = league.NameKey != null;
            _alias.gameObject.SetActive(alias);
            _alias.text = alias ? AvatarText.LeagueAlias(league.NameKey!, league.NameNum) : string.Empty;
            bool country = alias && league.Country != null;
            _country.gameObject.SetActive(country);
            _country.text = country ? LeagueText.Country(league.Country!) : string.Empty;
            _flag.Show(country ? league.Country : null, 21f, 14f);
            _headerA11y = AvatarText.HeaderA11y(current, league.NameKey, league.NameNum, league.Country);

            for (int i = 0; i < _cells.Count; i++)
            {
                Cell cell = _cells[i];
                AvatarInfo a = cell.Avatar;
                bool visible = AvatarRules.VisibleInPicker(a, data, leagueEnabled);
                cell.Root.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                bool locked = !AvatarRules.IsAvailable(a, data);
                bool chosen = string.Equals(a.Id, current.Id, StringComparison.Ordinal);
                bool isNew = AvatarRules.IsNew(a, data);
                atlas.Request(a.Index, a.Id, a.Index, BackdropOf(a), BadgeOf(a), locked);
                cell.Fill(atlas.Cell(a.Index), chosen, isNew, locked, maxLevel);
            }

            if (_balloonCell >= 0 && !_cells[_balloonCell].Locked)
            {
                _balloonCell = -1;
            }

            Layout();
            EmitLayout();
        }

        /// <summary>Se eligió <paramref name="id"/>: el rebote (o el fundido) del retrato de la cabecera.</summary>
        public void Chosen(string id)
        {
            _chooseMs = 0f;
            _balloonCell = -1;
            _balloon.gameObject.SetActive(false);
        }

        /// <summary>Se tocó uno bloqueado: la sacudida y el globo con la frase entera del hito.</summary>
        public void Rejected(string id)
        {
            Cell cell = CellOf(id);
            _shakeCell = cell.Slot;
            _shakeMs = 0f;
            _balloonCell = cell.Slot;
            _scrollToBalloon = true;
            Layout();
            EmitLayout();
        }

        public void Close()
        {
            _canvas.Canvas.gameObject.SetActive(false);
            _balloon.gameObject.SetActive(false);
            _balloonCell = -1;
            _service = null;
            _atlas = null;
        }

        /// <summary>Cada fotograma con la pantalla abierta: las animaciones (el atlas lo avanza <c>GameBootstrap</c>).</summary>
        public void Tick(float dtMs)
        {
            if (!Visible)
            {
                return;
            }

            if (_shakeCell >= 0)
            {
                _shakeMs += dtMs;
                Cell cell = _cells[_shakeCell];
                float t = _shakeMs / ShakeMs;
                float dx = ReduceMotion || t >= 1f ? 0f : Mathf.Sin(t * Mathf.PI * 6f) * ShakeDp * (1f - t);
                cell.Shake(dx);
                if (t >= 1f)
                {
                    _shakeCell = -1;
                }
            }

            if (_chooseMs >= 0f)
            {
                _chooseMs += dtMs;
                float t = Mathf.Clamp01(_chooseMs / ChooseMs);
                if (ReduceMotion)
                {
                    _portrait.color = new Color(1f, 1f, 1f, 0.35f + (0.65f * t));
                    _portrait.rectTransform.localScale = Vector3.one;
                }
                else
                {
                    float k = 1f + (0.12f * Mathf.Sin(t * Mathf.PI));
                    _portrait.rectTransform.localScale = new Vector3(k, k, 1f);
                    _portrait.color = Color.white;
                }

                if (t >= 1f)
                {
                    _chooseMs = -1f;
                    _portrait.rectTransform.localScale = Vector3.one;
                    _portrait.color = Color.white;
                }
            }
        }

        private Cell CellOf(string id)
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                if (string.Equals(_cells[i].Avatar.Id, id, StringComparison.Ordinal))
                {
                    return _cells[i];
                }
            }

            throw new ArgumentException("Avatar desconocido: " + id, nameof(id));
        }

        private void OnCell(Cell cell)
        {
            Tapped?.Invoke(cell.Avatar.Id);
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
            y += Mathf.Max(48f, headerH) + 10f;

            // Cabecera: el retrato a la izquierda y, a su lado, el nombre, el seudónimo y el país, centrados en su alto (crecen si no caben).
            float textX = 16f + HeaderPortraitDp + 12f;
            float textW = inner - HeaderPortraitDp - 12f;
            Fit(_name, HeaderNameSp * s, s);
            Fixed(_alias, AliasSp * s);
            Fixed(_country, AliasSp * s);
            float nameH = Mathf.Ceil(Mathf.Max(HeaderNameSp * s * 1.25f, UiBuilder.MinTextHeight(_name, textW)));
            float aliasH = _alias.gameObject.activeSelf ? Mathf.Ceil(Mathf.Max(AliasSp * s * 1.3f, UiBuilder.MinTextHeight(_alias, textW - 2f) + 4f)) : 0f;
            float flagW = _country.gameObject.activeSelf ? 21f + 6f : 0f;
            float countryH = _country.gameObject.activeSelf ? Mathf.Ceil(Mathf.Max(AliasSp * s * 1.3f, UiBuilder.MinTextHeight(_country, textW - flagW))) : 0f;
            float textH = nameH + aliasH + countryH;
            float blockH = Mathf.Max(HeaderPortraitDp, textH);
            UiBuilder.Place(_portrait.rectTransform, 16f, y + ((blockH - HeaderPortraitDp) * 0.5f), HeaderPortraitDp, HeaderPortraitDp);
            float ty = y + ((blockH - textH) * 0.5f);
            UiBuilder.Place(_name.rectTransform, textX, ty, textW, nameH);
            ty += nameH;
            if (aliasH > 0f)
            {
                UiBuilder.Place(_alias.rectTransform, textX, ty, textW, aliasH);
                ty += aliasH;
            }

            if (countryH > 0f)
            {
                UiBuilder.Place(_flag.Root, textX, ty + ((countryH - 14f) * 0.5f), 21f, 14f);
                _flag.Show(_flag.Code, 21f, 14f);
                UiBuilder.Place(_country.rectTransform, textX + flagW, ty, textW - flagW, countryH);
            }

            y += blockH + 12f;

            // La rejilla: 4 columnas a 360 dp; 3 a 320 dp o con el texto al 130 % (avatares.md §3.2 y §3.6).
            float gridW = w - 20f;
            _columns = (cw >= 340f && s < 1.15f) ? 4 : 3;
            float colW = gridW / _columns;
            float textAreaW = colW - 2f;

            // Ningún nombre se parte a media palabra: si la palabra más larga no cabe (en inglés, «Hummingbird» a 320 dp y al 130 %),
            // una columna menos.
            while (_columns > 2 && LongestCellWord(s) > textAreaW)
            {
                _columns--;
                colW = gridW / _columns;
                textAreaW = colW - 2f;
            }

            // Cada fila de la rejilla, con el alto de su celda más alta (un hito en dos líneas no estira las demás filas).
            var shown = new List<Cell>(_cells.Count);
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i].Root.gameObject.activeSelf)
                {
                    shown.Add(_cells[i]);
                }
            }

            int balloonRow = -1;
            float balloonH = 0f;
            if (_balloonCell >= 0 && _cells[_balloonCell].Root.gameObject.activeSelf && _cells[_balloonCell].Locked)
            {
                balloonRow = shown.IndexOf(_cells[_balloonCell]) / _columns;
                _balloonText.text = AvatarText.GoalLong(_cells[_balloonCell].Avatar, _maxLevel);
                Fit(_balloonText, BalloonSp * s, s);
                balloonH = Mathf.Ceil(UiBuilder.MinTextHeight(_balloonText, gridW - 28f)) + 16f;
            }

            float cy = 0f;
            float minRowH = float.MaxValue;
            float balloonTop = 0f;
            for (int r = 0; r * _columns < shown.Count; r++)
            {
                float rowH = 0f;
                for (int c = 0; c < _columns && (r * _columns) + c < shown.Count; c++)
                {
                    rowH = Mathf.Max(rowH, shown[(r * _columns) + c].Measure(textAreaW, s));
                }

                for (int c = 0; c < _columns && (r * _columns) + c < shown.Count; c++)
                {
                    shown[(r * _columns) + c].Layout(c * colW, cy, colW, rowH, s);
                }

                minRowH = Mathf.Min(minRowH, rowH);
                cy += rowH;
                if (r == balloonRow)
                {
                    balloonTop = cy;
                    UiBuilder.Place(_balloon, 6f, cy, gridW - 12f, balloonH);
                    UiBuilder.Place(_balloonText.rectTransform, 8f, 4f, gridW - 28f, balloonH - 8f);
                    cy += balloonH + 6f;
                }
            }

            _balloon.gameObject.SetActive(balloonRow >= 0);
            float viewH = Mathf.Max(minRowH < float.MaxValue ? minRowH : 96f, h - 16f - y);
            UiBuilder.Place(_viewport, 10f, y, gridW, viewH);
            // Se conserva lo desplazado (elegir otro no devuelve la rejilla arriba) y, tras tocar un bloqueado, el globo queda a la vista.
            float keep = _content.anchoredPosition.y;
            UiBuilder.Place(_content, 0f, 0f, gridW, cy);
            _scroll.vertical = cy > viewH + 0.5f;
            float range = Mathf.Max(0f, cy - viewH);
            float offset = Mathf.Clamp(keep, 0f, range);
            if (_scrollToBalloon && balloonRow >= 0)
            {
                offset = Mathf.Clamp(Mathf.Max(offset, balloonTop + balloonH + 6f - viewH), 0f, range);
            }

            _content.anchoredPosition = new Vector2(0f, offset);
            _scroll.velocity = Vector2.zero;
            _scrollToBalloon = false;
        }

        // La palabra más larga de los nombres a la vista, al tamaño de las celdas (dp).
        private float LongestCellWord(float s)
        {
            float widest = 0f;
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i].Root.gameObject.activeSelf)
                {
                    widest = Mathf.Max(widest, _cells[i].LongestWord(s));
                }
            }

            return widest;
        }

        // Tamaño fijo con la escala de texto (sin autoajuste: la caja se mide a ese tamaño y crece a lo alto).
        private static void Fixed(TMPro.TextMeshProUGUI text, float size)
        {
            text.enableAutoSizing = false;
            text.fontSize = Mathf.Max(UiBuilder.MinTextSp, size);
        }

        // Autoajuste hasta 12 sp por la escala de texto (02 §12).
        private static void Fit(TextMeshProUGUI text, float max, float s)
        {
            text.enableAutoSizing = true;
            text.fontSizeMax = max;
            text.fontSizeMin = Mathf.Min(max, UiBuilder.MinTextSp * Mathf.Max(1f, s));
        }

        // AF_LAYOUT avatar (avatares.md §3.6): la X y cada celda a la vista (avatar_<id>).
        private void EmitLayout()
        {
            if (!DevProbe.Enabled || _probe == null || !Visible)
            {
                return;
            }

            var ids = new List<string>(_cells.Count + 1) { "close" };
            var rects = new List<RectTransform>(_cells.Count + 1) { (RectTransform)_close.Button.transform };
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i].Root.gameObject.activeSelf && !A11yScroll.ClippedOut(_cells[i].Root))
                {
                    ids.Add("avatar_" + _cells[i].Avatar.Id);
                    rects.Add(_cells[i].Root);
                }
            }

            _probe.Layout(ProbeScreen, 0, null, ids.ToArray(), rects.ToArray());
        }

        /// <summary>Una celda: el retrato de 64 dp con su aro, la chapa ✓ o «Nuevo», el nombre y, si está bloqueada, el hito.</summary>
        private sealed class Cell
        {
            private readonly AvatarScreen _owner;
            private readonly Image _ring;
            private readonly Image _portrait;
            private readonly RectTransform _check;
            private readonly RectTransform _newChip;
            private readonly TextMeshProUGUI _newText;
            private readonly TextMeshProUGUI _name;
            private readonly TextMeshProUGUI _goal;
            private float _x;

            public Cell(AvatarScreen owner, int slot, Transform parent, ProceduralArt art)
            {
                _owner = owner;
                Slot = slot;
                Avatar = AvatarCatalog.All[slot];
                Button = UiBuilder.BareButton("Avatar_" + Avatar.Id, parent, () => _owner.OnCell(this));
                Root = (RectTransform)Button.transform;
                _ring = UiBuilder.Image("Ring", Root, art.Disc, Palette.FamilyLeaf.Light);
                _portrait = UiBuilder.Image("Portrait", Root, null, Color.white);
                _portrait.preserveAspect = true;
                _check = UiBuilder.Rect("Check", Root);
                Image checkDisc = UiBuilder.Image("Disc", _check, art.Disc, Palette.FamilyLeaf.Deep);
                UiBuilder.Stretch(checkDisc.rectTransform);
                Image checkMark = UiBuilder.Image("Mark", _check, art.AvatarCheck, Palette.Cream);
                UiBuilder.Stretch(checkMark.rectTransform);
                checkMark.rectTransform.offsetMin = new Vector2(2f, 2f);
                checkMark.rectTransform.offsetMax = new Vector2(-2f, -2f);
                _newChip = UiBuilder.Rect("New", Root);
                Image chip = UiBuilder.Sliced("Plate", _newChip, art.RoundedRect, Palette.Heliconia);
                UiBuilder.Stretch(chip.rectTransform);
                _newText = UiBuilder.Label("Text", _newChip, TextStyle.PlainBold, AvatarText.New, 12f, Palette.Cream, TextAlignmentOptions.Center);
                _name = UiBuilder.Label("Name", Root, TextStyle.Body, string.Empty, NameSp, Palette.FamilyWood.Deep, TextAlignmentOptions.Top);
                _name.textWrappingMode = TextWrappingModes.Normal;
                _goal = UiBuilder.Label("Goal", Root, TextStyle.Body, string.Empty, GoalSp, Palette.FamilyWood.Base, TextAlignmentOptions.Top);
                _goal.textWrappingMode = TextWrappingModes.Normal;
                A11yLabels.Set(Button, () => Reading, () => ChosenShown);
                A11yLabels.Set(_name, () => string.Empty);
                A11yLabels.Set(_goal, () => string.Empty);
                A11yLabels.Set(_newText, () => string.Empty);
            }

            public int Slot { get; }

            public AvatarInfo Avatar { get; }

            public Button Button { get; }

            public RectTransform Root { get; }

            public string Reading { get; private set; } = string.Empty;

            public bool ChosenShown { get; private set; }

            public bool NewShown { get; private set; }

            public bool Locked { get; private set; }

            public string GoalText => _goal.gameObject.activeSelf ? _goal.text : string.Empty;

            public IEnumerable<TextMeshProUGUI> Texts()
            {
                yield return _name;
                if (_goal.gameObject.activeSelf)
                {
                    yield return _goal;
                }

                if (_newChip.gameObject.activeSelf)
                {
                    yield return _newText;
                }
            }

            public void Fill(Sprite portrait, bool chosen, bool isNew, bool locked, int maxLevel)
            {
                _portrait.sprite = portrait;
                ChosenShown = chosen;
                NewShown = isNew && !locked;
                Locked = locked;
                _ring.gameObject.SetActive(chosen);
                _check.gameObject.SetActive(chosen);
                _newChip.gameObject.SetActive(NewShown && !chosen);
                _name.text = AvatarText.Name(Avatar);
                _goal.gameObject.SetActive(locked);
                _goal.text = locked ? AvatarText.GoalShort(Avatar) : string.Empty;
                Reading = AvatarText.CellA11y(Avatar, chosen, NewShown, locked, maxLevel);
            }

            /// <summary>La palabra más larga del nombre y del hito, a su tamaño más pequeño (dp).</summary>
            public float LongestWord(float s)
            {
                Fit(_name, NameSp * s, s);
                Fit(_goal, GoalSp * s, s);
                float w = LongestPiece(_name);
                return _goal.gameObject.activeSelf ? Mathf.Max(w, LongestPiece(_goal)) : w;
            }

            // El trozo más ancho que no se puede partir: TextMeshPro parte en los espacios y tras un guion («Martim-pescador»).
            private static float LongestPiece(TextMeshProUGUI text)
            {
                string value = text.text;
                float widest = 0f;
                int start = 0;
                for (int i = 0; i <= value.Length; i++)
                {
                    bool end = i == value.Length || value[i] == ' ' || value[i] == '\n';
                    bool hyphen = i < value.Length && value[i] == '-';
                    if (end || hyphen)
                    {
                        int stop = hyphen ? i + 1 : i;
                        if (stop > start)
                        {
                            widest = Mathf.Max(widest, UiBuilder.MinTextWidth(text, value.Substring(start, stop - start)));
                        }

                        start = i + 1;
                    }
                }

                return widest;
            }

            /// <summary>Alto que necesita la celda con el ancho de texto <paramref name="textW"/>: el retrato, el nombre (≤ 2 líneas) y el hito.</summary>
            public float Measure(float textW, float s)
            {
                Fit(_name, NameSp * s, s);
                Fit(_goal, GoalSp * s, s);
                float nameH = Mathf.Ceil(Mathf.Max(NameSp * s * 1.25f, UiBuilder.MinTextHeight(_name, textW)));
                float goalH = _goal.gameObject.activeSelf ? Mathf.Ceil(Mathf.Max(GoalSp * s * 1.25f, UiBuilder.MinTextHeight(_goal, textW))) : 0f;
                return Mathf.Max(AvatarSlotMinDp, 6f + CellPortraitDp + 4f + nameH + goalH + 8f);
            }

            public void Layout(float x, float y, float w, float h, float s)
            {
                _x = x;
                UiBuilder.Place(Root, x, y, w, h);
                float px = (w - CellPortraitDp) * 0.5f;
                const float ring = 3f;
                UiBuilder.Place(_ring.rectTransform, px - ring, 6f - ring, CellPortraitDp + (2f * ring), CellPortraitDp + (2f * ring));
                UiBuilder.Place(_portrait.rectTransform, px, 6f, CellPortraitDp, CellPortraitDp);
                const float chip = 22f;
                UiBuilder.Place(_check, px + CellPortraitDp - chip + 4f, 6f + CellPortraitDp - chip + 2f, chip, chip);
                _newText.fontSize = Mathf.Max(UiBuilder.MinTextSp, 12f) * s; // 12 sp con la escala (02 §12)
                float newW = Mathf.Min(w - 4f, Mathf.Ceil(UiBuilder.MinTextWidth(_newText)) + 12f);
                float newH = Mathf.Ceil((_newText.fontSize * 1.3f) + 2f);
                UiBuilder.Place(_newChip, (w - newW) * 0.5f, 2f, newW, newH);
                UiBuilder.Place(_newText.rectTransform, 0f, 0f, newW, newH);
                float textW = w - 2f;
                float nameH = Mathf.Ceil(Mathf.Max(NameSp * s * 1.25f, UiBuilder.MinTextHeight(_name, textW)));
                float ty = 6f + CellPortraitDp + 4f;
                UiBuilder.Place(_name.rectTransform, 1f, ty, textW, nameH);
                if (_goal.gameObject.activeSelf)
                {
                    float goalH = Mathf.Ceil(Mathf.Max(GoalSp * s * 1.25f, UiBuilder.MinTextHeight(_goal, textW)));
                    UiBuilder.Place(_goal.rectTransform, 1f, ty + nameH, textW, goalH);
                }
            }

            /// <summary>Desplaza la celda <paramref name="dx"/> dp en horizontal (la sacudida del rechazo).</summary>
            public void Shake(float dx)
            {
                Vector2 p = Root.anchoredPosition;
                Root.anchoredPosition = new Vector2(_x + dx, p.y);
            }
        }

        /// <summary>Alto mínimo de una celda (el área táctil de 48 dp queda dentro del retrato).</summary>
        private const float AvatarSlotMinDp = 96f;
    }
}
