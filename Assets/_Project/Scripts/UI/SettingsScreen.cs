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
    /// del inicio, con la cabecera «Ajustes», el cierre (y el Atrás de Android, que maneja <c>GameBootstrap</c>) y dos secciones
    /// de opciones con su marca de radio (el estado no depende solo del color, 07 §7.8):
    /// <list type="bullet">
    /// <item><b>Idioma</b>: «Automático» (el del teléfono, <c>settings.language</c> = auto) y cada idioma ofrecido con su
    /// nombre propio (<see cref="LocLocale.Offered"/>: solo los que tienen la UI completa; hoy es-419 y, en desarrollo,
    /// en-XA).</item>
    /// <item><b>Patrones de fruta</b>: Automático (desde el 41), Siempre o Nunca (<c>settings.patterns</c>).</item>
    /// </list>
    /// Sin sonido, música, vibración ni Eliminar cuenta: no existen (D-008: lo que no existe se oculta). Los huecos de
    /// «Mensajes de TiTino» (TI3), la pista por inactividad (MO3) y «Sobre la Amazonía» (F4) están creados y ocultos
    /// (<see cref="SlothMessagesSlot"/>, <see cref="IdleHintSlot"/> y <see cref="AboutSlot"/>): esas tareas los llenan y
    /// <see cref="Layout"/> los coloca cuando se activan. Elegir llama a quien la abre (<c>GameBootstrap</c>), que guarda el
    /// perfil y aplica: el idioma reconstruye la UI volviendo al inicio; los patrones, al momento. Escala de la fuente
    /// <see cref="FontScale"/> hasta el 130 % sin recortes (02 §12), con todo texto ≥ 12 sp.
    /// </summary>
    internal sealed class SettingsScreen
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

        private const float RowGapDp = 6f;
        private const float SectionGapDp = 12f;
        private const float RadioDp = 20f;

        private static readonly PatternsSetting[] PatternOrder = { PatternsSetting.Auto, PatternsSetting.On, PatternsSetting.Off };

        private readonly ProceduralArt _art;
        private readonly DevProbe _probe;
        private readonly Action<string> _onLanguage;
        private readonly Action<PatternsSetting> _onPatterns;
        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly ReliefParts _close;
        private readonly TextMeshProUGUI _languageTitle;
        private readonly TextMeshProUGUI _patternsTitle;
        private readonly RectTransform _languageGroup;
        private readonly List<OptionRow> _languageRows = new List<OptionRow>();
        private readonly OptionRow[] _patternRows = new OptionRow[3];
        private readonly List<string> _offered = new List<string>();

        private string _language = SettingsData.LanguageAuto;
        private PatternsSetting _patterns = PatternsSetting.Auto;
        private float _fontScale = 1f;
        private int _screenW;
        private int _screenH;
        private Rect _safeArea;

        /// <summary>Construye la pantalla bajo <paramref name="host"/>, oculta hasta <see cref="Show"/>.</summary>
        public SettingsScreen(Transform host, ProceduralArt art, DevProbe probe, Action<string> onLanguage, Action<PatternsSetting> onPatterns, Action onClose)
        {
            _art = art;
            _probe = probe;
            _onLanguage = onLanguage;
            _onPatterns = onPatterns;
            _canvas = new OverlayCanvas("SettingsCanvas", host, 40);
            ModalKit.Dim("Dim", _canvas.Root, 0.62f);
            _panel = ModalKit.Card("Panel", _canvas.Root, art, out _surface);
            _title = ModalKit.Header(_panel, art, UiText.Settings, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, onClose);

            _languageTitle = SectionTitle("LanguageTitle", UiText.Language);
            _languageGroup = UiBuilder.Rect("LanguageOptions", _panel);
            _patternsTitle = SectionTitle("PatternsTitle", UiText.PatternsToggle);
            for (int i = 0; i < PatternOrder.Length; i++)
            {
                PatternsSetting value = PatternOrder[i];
                _patternRows[i] = new OptionRow(_panel, art, "Patterns_" + DevProbe.PatternsKey(value), "patterns_" + DevProbe.PatternsKey(value),
                    PatternLabel(value), () => _onPatterns(value));
            }

            // Huecos de las tareas siguientes, ocultos hasta que existan (D-008).
            SlothMessagesSlot = HiddenSlot("SlothMessagesSlot");
            IdleHintSlot = HiddenSlot("IdleHintSlot");
            AboutSlot = HiddenSlot("AboutSlot");

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

        /// <summary>Hueco de «Mensajes de TiTino» (TI3), oculto.</summary>
        public RectTransform SlothMessagesSlot { get; }

        /// <summary>Hueco de la pista por inactividad (MO3), oculto.</summary>
        public RectTransform IdleHintSlot { get; }

        /// <summary>Hueco de «Sobre la Amazonía» (F4), oculto.</summary>
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
        /// sección y opciones sobre la madera clara, la opción elegida (crema sobre hoja) y las demás (madera sobre papel).
        /// </summary>
        public static (string Name, Color Text, Color Background)[] ContrastPairs()
        {
            return new[]
            {
                ("cabecera de sección", (Color)Palette.FamilyWood.Deep, (Color)Palette.PanelLightLow),
                ("opción elegida", (Color)Palette.Cream, Palette.FamilyLeaf.Deep),
                ("opción", Palette.FamilyWood.Deep, (Color)Palette.UiPaper),
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
        }

        /// <summary>
        /// Abre la pantalla con las opciones de Idioma «Automático» + <paramref name="offered"/> (idiomas con la UI completa) y
        /// marca lo guardado: <paramref name="language"/> (<c>settings.language</c>) y <paramref name="patterns"/>.
        /// </summary>
        public void Show(IReadOnlyList<string> offered, string language, PatternsSetting patterns)
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

            // Idioma.
            PlaceTitle(_languageTitle, 16f, y, inner, titleH, s);
            y += titleH + RowGapDp;
            UiBuilder.Place(_languageGroup, 0f, 0f, w, 0f);
            for (int i = 0; i < _offered.Count; i++)
            {
                _languageRows[i].Place(16f, y, inner, rowH, s);
                y += rowH + RowGapDp;
            }

            y += SectionGapDp - RowGapDp;

            // Patrones de fruta.
            PlaceTitle(_patternsTitle, 16f, y, inner, titleH, s);
            y += titleH + RowGapDp;
            for (int i = 0; i < _patternRows.Length; i++)
            {
                _patternRows[i].Place(16f, y, inner, rowH, s);
                y += rowH + RowGapDp;
            }

            // Huecos de TI3, MO3 y F4: aún ocultos (no ocupan sitio).
            y += 16f - RowGapDp;

            float h = y;
            float top = Mathf.Round((ch - h) * 0.5f);
            top = Mathf.Min(top, ch - safeBottom - 8f - h);
            top = Mathf.Max(top, safeTop + 8f);
            ModalKit.Size(_panel, _surface, _art, Mathf.Round((cw - w) * 0.5f), top, w, h, true);
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

            int n = 1 + _offered.Count + _patternRows.Length;
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

            _probe.Layout("settings", 0, null, ids, rects);
        }

        private TextMeshProUGUI SectionTitle(string name, string text)
        {
            TextMeshProUGUI title = UiBuilder.Label(name, _panel, TextStyle.PlainBold, text, SectionSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
            UiBuilder.AutoSize(title, MinTextSp, SectionSp);
            return title;
        }

        private RectTransform HiddenSlot(string name)
        {
            RectTransform slot = UiBuilder.Rect(name, _panel);
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

        // Una opción: placa de papel (madera clara) o de hoja si está elegida, con su marca de radio a la izquierda (anillo,
        // hueco y punto: el estado no depende solo del color) y el texto. Toda la fila es el área táctil (≥ 48 dp).
        private sealed class OptionRow
        {
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
                _plate = UiBuilder.Sliced("Plate", Root, art.RoundedRect, Palette.UiPaper);
                UiBuilder.Stretch(_plate.rectTransform);
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
                Color plate = selected ? Palette.FamilyLeaf.Deep : (Color)Palette.UiPaper;
                Color ink = selected ? (Color)Palette.Cream : Palette.FamilyWood.Deep;
                _plate.color = plate;
                Label.color = ink;
                _ring.color = ink;
                _hole.color = plate;
                _dot.color = ink;
                _dot.gameObject.SetActive(selected);
            }

            public void Place(float x, float y, float w, float h, float s)
            {
                UiBuilder.Place(Root, x, y, w, h);
                float r = h * 0.5f;
                _plate.pixelsPerUnitMultiplier = 24f / 12f;
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
