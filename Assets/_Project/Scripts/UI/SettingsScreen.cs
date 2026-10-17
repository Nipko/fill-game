#nullable enable
using System;
using System.Collections.Generic;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Pantalla de Ajustes (tarea L3; D-018 Q7-A y §3.5 de la propuesta de idiomas; 02 §12 y GDD.md §2 #5: Patrones de fruta
    /// siempre en Ajustes; 07 §7.6: engranaje de madera del inicio), construida por código como los modales del embudo
    /// (<see cref="ModalKit"/>, guía visual v2 5.3): una tarjeta de madera clara sobre un velo, en su propio lienzo por encima
    /// del inicio, con la cabecera «Ajustes», el cierre (y el Atrás de Android, que maneja
    /// <c>GameBootstrap.PressAndroidBack</c>) y dos secciones de opciones con su marca de radio (el estado no depende solo
    /// del color, 07 §7.8):
    /// <list type="bullet">
    /// <item><b>Idioma</b>: «Automático» (el del teléfono, <c>settings.language</c> = auto) y cada idioma ofrecido con su
    /// nombre propio (<see cref="LocLocale.Offered"/>: solo los que tienen la UI completa; hoy es-419 y, en desarrollo,
    /// en-XA).</item>
    /// <item><b>Patrones de fruta</b>: Automático (desde el 41), Siempre o Nunca (<c>settings.patterns</c>).</item>
    /// <item><b>Mensajes de TiTino</b> (tarea TI3; D-016, punto 4; 02 §12): una fila que se marca o se desmarca
    /// (<c>settings.sloth_messages</c>, activada por defecto; apagada, TiTino solo hace gestos), en
    /// <see cref="SlothMessagesSlot"/>.</item>
    /// <item><b>Sonido, Música y Vibración</b> (tarea AU1; 07 §9.4; D-026), arriba del todo: tres filas que se marcan o se
    /// desmarcan (<c>settings.sound</c>, <c>music</c> y <c>haptics</c>, activadas por defecto) y se aplican al momento, en
    /// <see cref="AudioSlot"/> (<c>SettingsScreen.Audio.cs</c>). Con ellas la tarjeta no cabe en un teléfono 16:9 al 130 %: lo de
    /// debajo de la cabecera va en un visor que se desplaza cuando hace falta.</item>
    /// <item><b>Sobre la Amazonía</b> (tarea F4; D-017 Q4-A; 07 §8.2), al final: «Frutas del río» con una ficha por serie del
    /// calendario, que abre su ficha (<see cref="FruitSheet"/>), en <see cref="AboutSlot"/> (<c>SettingsScreen.About.cs</c>).</item>
    /// </list>
    /// Sin Eliminar cuenta: no existe (D-008: lo que no existe se oculta). El hueco de la pista por inactividad (MO3) está
    /// creado y oculto (<see cref="IdleHintSlot"/>): esa tarea lo llena y <see cref="Layout"/> lo coloca cuando se active. Elegir llama a quien
    /// la abre (<c>GameBootstrap</c>), que guarda el perfil y aplica: el idioma reconstruye la UI volviendo al inicio, con esta
    /// pantalla otra vez abierta (FXT); los patrones, al momento; los mensajes de TiTino, desde su siguiente aparición. Escala de la fuente
    /// <see cref="FontScale"/> hasta el 130 % sin recortes (02 §12), con todo texto ≥ 12 sp.
    /// </summary>
    internal sealed partial class SettingsScreen
    {
        /// <summary>Ancho máximo de la tarjeta (dp).</summary>
        public const float PanelMaxWidth = 344f;

        /// <summary>Alto mínimo de una opción (área táctil, 07 §7.8).</summary>
        public const float MinRowDp = 48f;

        /// <summary>Tamaño de la fuente de las opciones al 100 % (sp).</summary>
        public const float OptionSp = 18f;

        /// <summary>Tamaño de la fuente de las cabeceras de sección al 100 % (sp).</summary>
        public const float SectionSp = 17f;

        /// <summary>Tamaño mínimo de cualquier texto de la pantalla (02 §12: ≥ 12 sp).</summary>
        public const float MinTextSp = 13f;

        /// <summary>Id de la opción «Automático» de Idioma (sonda y tests).</summary>
        public const string LanguageAutoId = SettingsData.LanguageAuto;

        /// <summary>
        /// Id de la fila «Mensajes de TiTino» en la sonda (<c>AF_LAYOUT</c>) y valor de <c>af_settings_change.key</c> (08 §8.2):
        /// <c>sloth_messages</c>.
        /// </summary>
        public const string SlothMessagesId = "sloth_messages";

        /// <summary>Margen interior de la tarjeta (dp): el de la cabecera arriba y el del visor abajo, dentro del marco.</summary>
        public const float PanelInsetDp = 16f;

        /// <summary>
        /// Alto del fundido de abajo del visor (dp; revisión de AU1): sale mientras quede contenido por debajo, para que se
        /// entienda que la lista sigue.
        /// </summary>
        public const float FadeBottomDp = 24f;

        /// <summary>Alto del fundido de arriba del visor (dp): sale en cuanto el contenido se ha desplazado.</summary>
        public const float FadeTopDp = 18f;

        /// <summary>
        /// Lo que se ve de la última fila cortada por el visor, en fracción de su alto, cuando el contenido se desplaza (revisión
        /// de AU1): entre <see cref="CutRowMin"/> y <see cref="CutRowMax"/>; si no, el visor se acorta hasta media fila.
        /// </summary>
        public const float CutRowMin = 0.35f;

        /// <summary>Fracción máxima de la última fila cortada que se ve (ver <see cref="CutRowMin"/>).</summary>
        public const float CutRowMax = 0.6f;

        private const float RowGapDp = 6f;
        private const float SectionGapDp = 12f;
        private const float RadioDp = 20f;

        private static readonly PatternsSetting[] PatternOrder = { PatternsSetting.Auto, PatternsSetting.On, PatternsSetting.Off };

        private readonly ProceduralArt _art;
        private readonly DevProbe _probe;
        private readonly Action<string> _onLanguage;
        private readonly Action<PatternsSetting> _onPatterns;
        private readonly Action<bool> _onSlothMessages;
        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly ReliefParts _close;
        private readonly RectTransform _viewport;
        private readonly RectTransform _content;
        private readonly ScrollRect _scroll;
        private readonly Image _fadeTop;
        private readonly Image _fadeBottom;
        private readonly List<float> _rowTops = new List<float>();
        private readonly List<float> _rowHeights = new List<float>(); // alto de cada fila de _rowTops (las frutas de F4 crecen)
        private readonly TextMeshProUGUI _languageTitle;
        private readonly TextMeshProUGUI _patternsTitle;
        private readonly RectTransform _languageGroup;
        private readonly List<OptionRow> _languageRows = new List<OptionRow>();
        private readonly OptionRow[] _patternRows = new OptionRow[3];
        private readonly OptionRow _slothRow;
        private readonly List<string> _offered = new List<string>();

        private string _language = SettingsData.LanguageAuto;
        private PatternsSetting _patterns = PatternsSetting.Auto;
        private bool _slothMessages = true;
        private float _fontScale = 1f;
        private int _screenW;
        private int _screenH;
        private Rect _safeArea;

        /// <summary>Construye la pantalla bajo <paramref name="host"/>, oculta hasta <see cref="Show"/>.</summary>
        public SettingsScreen(Transform host, ProceduralArt art, DevProbe probe, Action<string> onLanguage, Action<PatternsSetting> onPatterns,
            Action<bool> onSlothMessages, Action onClose)
        {
            _art = art;
            _probe = probe;
            _onLanguage = onLanguage;
            _onPatterns = onPatterns;
            _onSlothMessages = onSlothMessages;
            _canvas = new OverlayCanvas("SettingsCanvas", host, 40);
            ModalKit.Dim("Dim", _canvas.Root, 0.62f);
            _panel = ModalKit.Card("Panel", _canvas.Root, art, out _surface);
            _title = ModalKit.Header(_panel, art, UiText.Settings, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, onClose);

            // Contenido bajo la cabecera, en un visor que se desplaza si no cabe (AU1: con Sonido, Música y Vibración, la tarjeta
            // pasa del alto de un teléfono 16:9 al 130 %); la cabecera y el cierre quedan fijos.
            _viewport = UiBuilder.Rect("Viewport", _panel);
            _viewport.gameObject.AddComponent<RectMask2D>();
            _viewport.gameObject.AddComponent<HitArea>().raycastTarget = true; // el arrastre en los huecos también desplaza
            _content = UiBuilder.Rect("Content", _viewport);
            _scroll = _viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = _viewport;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            BuildAudioRows(); // Sonido, Música y Vibración (AU1; SettingsScreen.Audio.cs)

            _languageTitle = SectionTitle("LanguageTitle", UiText.Language);
            _languageGroup = UiBuilder.Rect("LanguageOptions", _content);
            _patternsTitle = SectionTitle("PatternsTitle", UiText.PatternsToggle);
            for (int i = 0; i < PatternOrder.Length; i++)
            {
                PatternsSetting value = PatternOrder[i];
                _patternRows[i] = new OptionRow(_content, art, "Patterns_" + DevProbe.PatternsKey(value), "patterns_" + DevProbe.PatternsKey(value),
                    PatternLabel(value), () => _onPatterns(value));
            }

            // Mensajes de TiTino (TI3): una fila que se marca o se desmarca, en su hueco.
            SlothMessagesSlot = UiBuilder.Rect("SlothMessagesSlot", _content);
            _slothRow = new OptionRow(SlothMessagesSlot, art, "SlothMessages", SlothMessagesId, UiText.SettingsSlothMessages,
                () => _onSlothMessages(!_slothMessages));

            // Hueco de la pista por inactividad (MO3), oculto hasta que exista (D-008); «Sobre la Amazonía» (F4), oculto hasta que
            // haya algo que enseñar (SettingsScreen.About.cs).
            IdleHintSlot = HiddenSlot("IdleHintSlot");
            AboutSlot = HiddenSlot("AboutSlot");
            BuildAbout();

            // Fundidos de los bordes del visor (revisión de AU1), encima del contenido y sin recibir toques: abajo mientras quede
            // lista por debajo y arriba en cuanto se ha desplazado, para que el corte no parezca un fallo (D-003).
            _fadeTop = UiBuilder.Image("FadeTop", _viewport, art.ScrollFade(true), Color.white);
            _fadeBottom = UiBuilder.Image("FadeBottom", _viewport, art.ScrollFade(false), Color.white);
            _scroll.onValueChanged.AddListener(_ => UpdateFades());

            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>La pantalla está abierta.</summary>
        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Lienzo propio (capturas de prueba).</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>Tarjeta (tests: dentro de la pantalla).</summary>
        public RectTransform Panel => _panel;

        /// <summary>Ancho del lienzo (dp).</summary>
        public float CanvasWidth => _canvas.Width;

        /// <summary>Alto del lienzo (dp).</summary>
        public float CanvasHeight => _canvas.Height;

        /// <summary>Cierre (X).</summary>
        public Button CloseButton => _close.Button;

        /// <summary>Título de la cabecera.</summary>
        public string Title => _title.text;

        /// <summary>Cabecera de la sección Idioma.</summary>
        public string LanguageTitle => _languageTitle.text;

        /// <summary>Cabecera de la sección Patrones de fruta.</summary>
        public string PatternsTitle => _patternsTitle.text;

        /// <summary>Opciones de Idioma a la vista («auto» y los idiomas ofrecidos, en orden).</summary>
        public IReadOnlyList<string> LanguageOptions => _offered;

        /// <summary>El idioma guardado que se marca (<c>settings.language</c>).</summary>
        public string SelectedLanguage => _language;

        /// <summary>Los Patrones de fruta que se marcan (<c>settings.patterns</c>).</summary>
        public PatternsSetting SelectedPatterns => _patterns;

        /// <summary>Hueco de «Mensajes de TiTino» (TI3), con su fila.</summary>
        public RectTransform SlothMessagesSlot { get; }

        /// <summary>Lo que se marca en «Mensajes de TiTino» (<c>settings.sloth_messages</c>).</summary>
        public bool SelectedSlothMessages => _slothMessages;

        /// <summary>Botón de la fila «Mensajes de TiTino» (la marca o la desmarca).</summary>
        public Button SlothMessagesButton => _slothRow.Button;

        /// <summary>Texto de la fila «Mensajes de TiTino».</summary>
        public string SlothMessagesLabel => _slothRow.Label.text;

        /// <summary>Si la fila «Mensajes de TiTino» lleva la marca (mensajes activados).</summary>
        public bool SlothMessagesMarked => _slothRow.Selected;

        /// <summary>Hueco de la pista por inactividad (MO3), oculto.</summary>
        public RectTransform IdleHintSlot { get; }

        /// <summary>Hueco de «Sobre la Amazonía» (F4): visible con alguna fruta que enseñar.</summary>
        public RectTransform AboutSlot { get; }

        /// <summary>
        /// Escala de la fuente del sistema, entre 1 y <see cref="LevelCardView.MaxFontScale"/> (02 §12: por encima del 130 % se
        /// fija ese tope). Las opciones crecen en alto con el texto. La escala de Android aún no llega al juego [P]: tests y
        /// capturas. Con la pantalla abierta, se vuelve a colocar.
        /// </summary>
        public float FontScale
        {
            get => _fontScale;
            set
            {
                _fontScale = Mathf.Clamp(value, 1f, LevelCardView.MaxFontScale);
                if (Visible)
                {
                    Layout();
                }
            }
        }

        /// <summary>
        /// Pares texto / fondo de la pantalla, para la comprobación de contraste ≥ 4,5:1 (07 §1.2 y §7.8): cabeceras de
        /// sección y opciones sobre la madera clara, la opción elegida (crema sobre hoja) y las demás (madera sobre su placa).
        /// </summary>
        public static (string Name, Color Text, Color Background)[] ContrastPairs()
        {
            return new[]
            {
                ("cabecera de sección", (Color)Palette.FamilyWood.Deep, (Color)Palette.PanelLightLow),
                ("opción elegida", (Color)Palette.Cream, Palette.FamilyLeaf.Deep),
                ("opción", Palette.FamilyWood.Deep, OptionPlate),
            };
        }

        /// <summary>
        /// Pares límite / fondo de los componentes que no son texto, para la comprobación ≥ 3:1 (WCAG 1.4.11; revisión de L3): el
        /// borde (opaco) de una opción no elegida sobre los dos extremos del degradado de la tarjeta.
        /// </summary>
        public static (string Name, Color Boundary, Color Background)[] BoundaryContrastPairs()
        {
            return new[]
            {
                ("borde de opción sobre la madera clara baja", OptionBorder, (Color)Palette.PanelLightLow),
                ("borde de opción sobre la madera clara", OptionBorder, (Color)Palette.PanelLight),
            };
        }

        /// <summary>Botón de la opción de Idioma <paramref name="id"/> («auto» o un idioma), o null si no está.</summary>
        public Button? LanguageButton(string id)
        {
            OptionRow? row = FindLanguage(id);
            return row?.Button;
        }

        /// <summary>Texto de la opción de Idioma <paramref name="id"/>, o null si no está.</summary>
        public string? LanguageLabel(string id)
        {
            OptionRow? row = FindLanguage(id);
            return row?.Label.text;
        }

        /// <summary>Si la opción de Idioma <paramref name="id"/> lleva la marca.</summary>
        public bool LanguageMarked(string id)
        {
            OptionRow? row = FindLanguage(id);
            return row != null && row.Selected;
        }

        /// <summary>Botón de la opción de Patrones de fruta <paramref name="value"/>.</summary>
        public Button PatternsButton(PatternsSetting value)
        {
            return _patternRows[Index(value)].Button;
        }

        /// <summary>Texto de la opción de Patrones de fruta <paramref name="value"/>.</summary>
        public string PatternsLabel(PatternsSetting value)
        {
            return _patternRows[Index(value)].Label.text;
        }

        /// <summary>Si la opción de Patrones de fruta <paramref name="value"/> lleva la marca.</summary>
        public bool PatternsMarked(PatternsSetting value)
        {
            return _patternRows[Index(value)].Selected;
        }

        /// <summary>Todos los textos de la pantalla (tests: ≥ 12 sp y sin recortes).</summary>
        public IEnumerable<TextMeshProUGUI> Texts()
        {
            yield return _title;
            yield return _languageTitle;
            yield return _patternsTitle;
            for (int i = 0; i < _offered.Count; i++)
            {
                yield return _languageRows[i].Label;
            }

            for (int i = 0; i < _patternRows.Length; i++)
            {
                yield return _patternRows[i].Label;
            }

            yield return _slothRow.Label;
            foreach (TextMeshProUGUI label in AudioTexts())
            {
                yield return label;
            }

            foreach (TextMeshProUGUI label in AboutTexts())
            {
                yield return label;
            }
        }

        /// <summary>
        /// Abre la pantalla con las opciones de Idioma «Automático» + <paramref name="offered"/> (idiomas con la UI completa) y
        /// marca lo guardado: <paramref name="language"/> (<c>settings.language</c>), <paramref name="patterns"/> y
        /// <paramref name="slothMessages"/> (<c>settings.sloth_messages</c>).
        /// </summary>
        public void Show(IReadOnlyList<string> offered, string language, PatternsSetting patterns, bool slothMessages)
        {
            if (offered == null)
            {
                throw new ArgumentNullException(nameof(offered));
            }

            _offered.Clear();
            _offered.Add(LanguageAutoId);
            for (int i = 0; i < offered.Count; i++)
            {
                _offered.Add(offered[i]);
            }

            while (_languageRows.Count < _offered.Count)
            {
                _languageRows.Add(new OptionRow(_languageGroup, _art, "Language_" + _languageRows.Count, string.Empty, string.Empty, null));
            }

            for (int i = 0; i < _languageRows.Count; i++)
            {
                OptionRow row = _languageRows[i];
                bool used = i < _offered.Count;
                row.Root.gameObject.SetActive(used);
                if (used)
                {
                    string id = _offered[i];
                    row.Rebind(id == LanguageAutoId ? "lang_auto" : "lang_" + id, id == LanguageAutoId ? UiText.SettingsLanguageAuto : UiText.LanguageName(id), () => _onLanguage(id));
                }
            }

            _canvas.Canvas.gameObject.SetActive(true);
            Mark(language, patterns);
            MarkSlothMessages(slothMessages);
            RefreshAbout(); // Sobre la Amazonía (F4): las frutas con el progreso de ahora
            ReadScreen();
            Layout();
            _probe.SettingsOpened(_language, _patterns, offered);
            EmitLayout();
        }

        /// <summary>Marca lo guardado sin cerrar (tras elegir unos Patrones de fruta).</summary>
        public void Mark(string language, PatternsSetting patterns)
        {
            _language = language ?? SettingsData.LanguageAuto;
            _patterns = patterns;
            for (int i = 0; i < _offered.Count; i++)
            {
                _languageRows[i].SetSelected(string.Equals(_offered[i], _language, StringComparison.Ordinal));
            }

            for (int i = 0; i < _patternRows.Length; i++)
            {
                _patternRows[i].SetSelected(PatternOrder[i] == _patterns);
            }
        }

        /// <summary>Marca o desmarca «Mensajes de TiTino» sin cerrar (tras tocarla).</summary>
        public void MarkSlothMessages(bool on)
        {
            _slothMessages = on;
            _slothRow.SetSelected(on);
        }

        /// <summary>Cierra la pantalla (sin avisar a nadie: lo hace quien la cierra).</summary>
        public void Hide()
        {
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>Con la pantalla abierta, la vuelve a colocar si cambió la pantalla o la zona segura.</summary>
        public void Tick()
        {
            if (!Visible)
            {
                return;
            }

            GameScreen.ReadScreen(out int sw, out int sh, out Rect safe);
            if (sw == _screenW && sh == _screenH && safe == _safeArea)
            {
                return;
            }

            ReadScreen();
            Layout();
            EmitLayout();
        }

        // Pantalla, zona segura y escala del lienzo de ahora.
        private void ReadScreen()
        {
            GameScreen.ReadScreen(out _screenW, out _screenH, out _safeArea);
            _canvas.Measure();
        }

        private void Layout()
        {
            float s = _fontScale;
            float cw = _canvas.Width;
            float ch = _canvas.Height;
            float scale = _screenW > 0 ? _screenW / Mathf.Max(1f, cw) : 1f;
            float safeTop = Mathf.Max(GameScreen.MinSafeTop, _screenH > 0 ? (_screenH - _safeArea.yMax) / scale : 0f);
            float safeBottom = Mathf.Max(GameScreen.MinSafeBottom, _screenH > 0 ? _safeArea.yMin / scale : 0f);

            float w = Mathf.Min(PanelMaxWidth, cw - 16f);
            float inner = w - 32f;
            float rowH = Mathf.Max(MinRowDp, Mathf.Round((OptionSp * s) + 26f));
            float titleH = Mathf.Round((SectionSp * s) + 10f);
            float headerH = Mathf.Max(40f, Mathf.Round((22f * s) + 12f));

            // Cabecera: la placa con el título y el cierre a su derecha.
            float y = 16f;
            _title.fontSizeMax = 22f * s;
            UiBuilder.Place(_header.rectTransform, 16f, y + ((48f - headerH) * 0.5f), inner - ModalKit.CloseDp - 8f, headerH);
            UiBuilder.Place((RectTransform)_close.Button.transform, w - 16f - ModalKit.CloseDp, y, ModalKit.CloseDp, ModalKit.CloseDp);
            y += Mathf.Max(48f, headerH) + SectionGapDp;

            // Lo de debajo de la cabecera va en el contenido desplazable (AU1), desde su borde de arriba; primero Sonido, Música y
            // Vibración (SettingsScreen.Audio.cs).
            float headerBottom = y;
            _rowTops.Clear();
            _rowHeights.Clear();
            for (int i = 0; i < AudioRowCount; i++)
            {
                _rowTops.Add(i * (rowH + RowGapDp));
                _rowHeights.Add(rowH);
            }

            y = LayoutAudio(0f, inner, rowH, s);

            // Idioma.
            PlaceTitle(_languageTitle, 16f, y, inner, titleH, s);
            y += titleH + RowGapDp;
            UiBuilder.Place(_languageGroup, 0f, 0f, w, 0f);
            for (int i = 0; i < _offered.Count; i++)
            {
                _languageRows[i].Place(16f, y, inner, rowH, s);
                _rowTops.Add(y);
                _rowHeights.Add(rowH);
                y += rowH + RowGapDp;
            }

            y += SectionGapDp - RowGapDp;

            // Patrones de fruta.
            PlaceTitle(_patternsTitle, 16f, y, inner, titleH, s);
            y += titleH + RowGapDp;
            for (int i = 0; i < _patternRows.Length; i++)
            {
                _patternRows[i].Place(16f, y, inner, rowH, s);
                _rowTops.Add(y);
                _rowHeights.Add(rowH);
                y += rowH + RowGapDp;
            }

            // Mensajes de TiTino (TI3): una fila en su hueco, tras los patrones.
            y += SectionGapDp - RowGapDp;
            UiBuilder.Place(SlothMessagesSlot, 16f, y, inner, rowH);
            _slothRow.Place(0f, 0f, inner, rowH, s);
            _rowTops.Add(y);
            _rowHeights.Add(rowH);
            y += rowH + RowGapDp;

            // Sobre la Amazonía (F4), si hay frutas que enseñar; el hueco de MO3, aún oculto (no ocupa sitio). El contenido acaba en
            // la última fila.
            y -= RowGapDp;
            if (AboutShown)
            {
                y = LayoutAbout(y + SectionGapDp, w, inner, rowH, titleH, s);
            }

            // La tarjeta crece con el contenido hasta el alto libre entre las zonas seguras; si no cabe, el contenido se desplaza
            // (empieza arriba cada vez que se coloca). El visor deja abajo el mismo margen interior de 16 dp que la cabecera arriba:
            // lo que se corta, se corta dentro de la crema y nunca pisa el marco de madera (revisión de AU1; D-003).
            float contentH = y;
            float maxH = Mathf.Max(headerBottom + MinRowDp + PanelInsetDp, ch - safeTop - safeBottom - 16f);
            float h = Mathf.Min(headerBottom + contentH + PanelInsetDp, maxH);
            float viewH = h - headerBottom - PanelInsetDp;
            _scroll.vertical = contentH > viewH + 0.5f;
            if (_scroll.vertical)
            {
                // Si se desplaza, el visor acaba a media fila (revisión de AU1): una franja de unos dp en el borde no se lee como
                // una fila más, y el jugador podría no encontrar nunca las de abajo.
                viewH = CutAtRow(viewH);
                h = headerBottom + viewH + PanelInsetDp;
            }

            UiBuilder.Place(_viewport, 0f, headerBottom, w, viewH);
            UiBuilder.Place(_content, 0f, 0f, w, contentH);
            _scroll.velocity = Vector2.zero;
            PlaceFades(headerBottom, w, h, viewH);
            float top = Mathf.Round((ch - h) * 0.5f);
            top = Mathf.Min(top, ch - safeBottom - 8f - h);
            top = Mathf.Max(top, safeTop + 8f);
            ModalKit.Size(_panel, _surface, _art, Mathf.Round((cw - w) * 0.5f), top, w, h, true);
        }

        /// <summary>
        /// Alto del visor (dp) para que, con el contenido arriba, la última fila cortada se vea entre el
        /// <see cref="CutRowMin"/> y el <see cref="CutRowMax"/> de su alto: si ya es así, <paramref name="viewH"/>; si no, el
        /// visor acaba en la mitad de la última fila que se ve al menos a medias (nunca de la primera). Cada fila con su alto: las
        /// frutas de «Sobre la Amazonía» (F4) crecen con su nombre.
        /// </summary>
        private float CutAtRow(float viewH)
        {
            float last = -1f;
            float lastH = 0f;
            for (int i = 0; i < _rowTops.Count; i++)
            {
                float top = _rowTops[i];
                float rowH = _rowHeights[i];
                float shown = (viewH - top) / rowH;
                if (shown >= CutRowMin && shown <= CutRowMax)
                {
                    return viewH;
                }

                if (shown >= 0.5f && top > last)
                {
                    last = top;
                    lastH = rowH;
                }
            }

            return last > 0f ? last + Mathf.Round(lastH * 0.5f) : viewH;
        }

        // Los fundidos de arriba y abajo del visor, del color de la superficie en cada borde (sin la veta), sobre el ancho de las
        // filas con 2 dp de aire (dentro de la crema: nunca pisan el marco).
        private void PlaceFades(float headerBottom, float w, float h, float viewH)
        {
            float surfaceH = h - 16f; // la superficie va 8 dp dentro de la tarjeta por cada lado (UiBuilder.ModalBody)
            _fadeTop.color = ProceduralArt.ModalSurfaceColorAt(headerBottom - 8f, surfaceH);
            _fadeBottom.color = ProceduralArt.ModalSurfaceColorAt(headerBottom + viewH - 8f, surfaceH);
            UiBuilder.Place(_fadeTop.rectTransform, 14f, 0f, w - 28f, FadeTopDp);
            UiBuilder.Place(_fadeBottom.rectTransform, 14f, viewH - FadeBottomDp, w - 28f, FadeBottomDp);
            UpdateFades();
        }

        // Cada fundido sale solo si queda contenido por ese lado (verticalNormalizedPosition: 1 arriba, 0 abajo).
        private void UpdateFades()
        {
            bool scrolls = _scroll.vertical;
            float pos = scrolls ? _scroll.verticalNormalizedPosition : 1f;
            SetActive(_fadeTop.gameObject, scrolls && pos < 0.99f);
            SetActive(_fadeBottom.gameObject, scrolls && pos > 0.01f);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }

        private static void PlaceTitle(TextMeshProUGUI text, float x, float y, float w, float h, float s)
        {
            text.fontSizeMax = SectionSp * s;
            text.fontSizeMin = MinTextSp;
            UiBuilder.Place(text.rectTransform, x, y, w, h);
        }

        private void EmitLayout()
        {
            if (!DevProbe.Enabled || !Visible)
            {
                return;
            }

            int n = 2 + _offered.Count + _patternRows.Length + AudioRowCount + AboutLayoutCount;
            var ids = new string[n];
            var rects = new RectTransform[n];
            ids[0] = "close";
            rects[0] = (RectTransform)_close.Button.transform;
            int k = 1;
            for (int i = 0; i < _offered.Count; i++, k++)
            {
                ids[k] = _languageRows[i].Id;
                rects[k] = _languageRows[i].Root;
            }

            for (int i = 0; i < _patternRows.Length; i++, k++)
            {
                ids[k] = _patternRows[i].Id;
                rects[k] = _patternRows[i].Root;
            }

            ids[k] = _slothRow.Id;
            rects[k] = _slothRow.Root;
            AudioLayoutIds(ids, rects, k + 1); // sound, music y haptics (AU1)
            AboutLayoutIds(ids, rects, k + 1 + AudioRowCount); // fruit_<sujeto> de Sobre la Amazonía (F4)

            _probe.Layout("settings", 0, null, ids, rects);
        }

        private TextMeshProUGUI SectionTitle(string name, string text)
        {
            TextMeshProUGUI title = UiBuilder.Label(name, _content, TextStyle.PlainBold, text, SectionSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
            UiBuilder.AutoSize(title, MinTextSp, SectionSp);
            return title;
        }

        private RectTransform HiddenSlot(string name)
        {
            RectTransform slot = UiBuilder.Rect(name, _content);
            slot.gameObject.SetActive(false);
            return slot;
        }

        private OptionRow? FindLanguage(string id)
        {
            for (int i = 0; i < _offered.Count; i++)
            {
                if (string.Equals(_offered[i], id, StringComparison.Ordinal))
                {
                    return _languageRows[i];
                }
            }

            return null;
        }

        private static int Index(PatternsSetting value)
        {
            for (int i = 0; i < PatternOrder.Length; i++)
            {
                if (PatternOrder[i] == value)
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(value), "Ajuste de patrones desconocido.");
        }

        private static string PatternLabel(PatternsSetting value)
        {
            switch (value)
            {
                case PatternsSetting.On: return UiText.SettingsPatternsOn;
                case PatternsSetting.Off: return UiText.SettingsPatternsOff;
                default: return UiText.SettingsPatternsAuto;
            }
        }

        // Placa de una opción no elegida: crema (más clara que la tarjeta, la levanta; la madera oscura encima, 11,5:1) con un
        // borde opaco de 2 dp de la madera base #7E5638 (5,1:1 sobre la madera clara de la tarjeta, ≥ 3:1 de WCAG 1.4.11 para
        // el límite de un componente), para que se lea como botón y no como texto suelto (revisión de L3: el papel solo se
        // separaba de la tarjeta un 3 % de luminancia y el primer borde, la madera oscura al 25 %, daba 1,56:1). Opaco y no con
        // alfa: el proyecto está en Linear y la GPU mezcla en lineal (ColorMath.CompositeLinear), así que la madera oscura al
        // 60 % quedaría en 2,2:1 en pantalla aunque en sRGB dé 3,3:1.
        private static readonly Color OptionPlate = Palette.Cream;
        private static readonly Color OptionBorder = Palette.FamilyWood.Base;
        private const float OptionBorderDp = 2f;
        private const float OptionRadiusDp = 12f;

        // Una opción: placa crema con borde de madera, o de hoja si está elegida, con su marca de radio a la izquierda (anillo,
        // hueco y punto: el estado no depende solo del color) y el texto. Toda la fila es el área táctil (≥ 48 dp).
        private sealed class OptionRow
        {
            private readonly Image _border;
            private readonly Image _plate;
            private readonly Image _ring;
            private readonly Image _hole;
            private readonly Image _dot;
            private Action? _onClick;

            public OptionRow(Transform parent, ProceduralArt art, string name, string id, string text, Action? onClick)
            {
                Id = id;
                _onClick = onClick;
                Button = UiBuilder.BareButton(name, parent, () => _onClick?.Invoke());
                Root = (RectTransform)Button.transform;
                _border = UiBuilder.Sliced("Border", Root, art.RoundedRect, OptionBorder);
                UiBuilder.Stretch(_border.rectTransform);
                _plate = UiBuilder.Sliced("Plate", Root, art.RoundedRect, OptionPlate);
                UiBuilder.Stretch(_plate.rectTransform);
                _border.pixelsPerUnitMultiplier = 24f / OptionRadiusDp;
                _ring = Circle("Ring", art, RadioDp);
                _hole = Circle("Hole", art, RadioDp - 6f);
                _dot = Circle("Dot", art, 10f);
                Label = UiBuilder.Label("Label", Root, TextStyle.PlainBold, text, OptionSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
                UiBuilder.AutoSize(Label, MinTextSp + 1f, OptionSp);
                SetSelected(false);
            }

            public string Id { get; private set; }

            public Button Button { get; }

            public RectTransform Root { get; }

            public TextMeshProUGUI Label { get; }

            public bool Selected { get; private set; }

            public void Rebind(string id, string text, Action onClick)
            {
                Id = id;
                Label.text = text;
                _onClick = onClick;
            }

            public void SetSelected(bool selected)
            {
                Selected = selected;
                Color plate = selected ? Palette.FamilyLeaf.Deep : OptionPlate;
                Color ink = selected ? (Color)Palette.Cream : Palette.FamilyWood.Deep;
                _plate.color = plate;
                Label.color = ink;
                _ring.color = ink;
                _hole.color = plate;
                _dot.color = ink;
                _dot.gameObject.SetActive(selected);

                // La elegida, verde llena hasta el borde y sin él; las demás, la placa dentro del borde.
                float inset = selected ? 0f : OptionBorderDp;
                _border.gameObject.SetActive(!selected);
                _plate.rectTransform.offsetMin = new Vector2(inset, inset);
                _plate.rectTransform.offsetMax = new Vector2(-inset, -inset);
                _plate.pixelsPerUnitMultiplier = 24f / (OptionRadiusDp - inset);
            }

            public void Place(float x, float y, float w, float h, float s)
            {
                UiBuilder.Place(Root, x, y, w, h);
                float r = h * 0.5f;
                Center(_ring.rectTransform, 14f + (RadioDp * 0.5f), r, RadioDp);
                Center(_hole.rectTransform, 14f + (RadioDp * 0.5f), r, RadioDp - 6f);
                Center(_dot.rectTransform, 14f + (RadioDp * 0.5f), r, 10f);
                Label.fontSizeMax = OptionSp * s;
                Label.fontSizeMin = MinTextSp + 1f;
                UiBuilder.Place(Label.rectTransform, 14f + RadioDp + 12f, 0f, w - (14f + RadioDp + 12f) - 12f, h);
            }

            private Image Circle(string name, ProceduralArt art, float d)
            {
                Image img = UiBuilder.Sliced(name, Root, art.RoundedRect, Color.white);
                img.pixelsPerUnitMultiplier = 24f / (d * 0.5f);
                return img;
            }

            private static void Center(RectTransform rt, float cx, float cy, float d)
            {
                UiBuilder.Place(rt, cx - (d * 0.5f), cy - (d * 0.5f), d, d);
            }
        }
    }
}
