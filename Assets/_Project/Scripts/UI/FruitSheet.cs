#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Ficha de una fruta de Frutas del río (D-017, Q4-A y Q6-A; 02 §6, fila «Dato del río»; propuesta de Frutas §2.3 y §6.4;
    /// tarea F4): una hoja inferior de solo lectura por serie del calendario (<c>frutas_del_rio_v1.json</c>), construida por código
    /// en su propio lienzo, por encima de la victoria, de Ajustes y del Mapa (<see cref="SortingOrder"/>), sin escena ni prefab.
    /// Se abre desde la línea de fruta de la victoria (<see cref="FactLine"/>: toda la línea es un botón de ≥ 48 dp) y desde
    /// Ajustes › «Sobre la Amazonía» › «Frutas del río»; la abre y la cierra <c>GameBootstrap</c> (<see cref="IGameActions.OpenFruitSheet"/>),
    /// que escribe la sonda <c>af_fact_page_open</c> {tint_id, unlocked} (08 §8.2). Al cerrarla (su X, el velo o el Atrás de Android)
    /// se vuelve a lo de debajo tal como estaba: mientras se ve, la victoria no avanza.
    /// <list type="bullet">
    /// <item><b>Cabecera:</b> «Frutas del río» (<c>fact_sheet_title</c>) en la placa, con el cierre.</item>
    /// <item><b>La fruta:</b> su icono (la fruta en vuelo del tinte; en las series de río [P], la gota del agua), «Açaí · 3/20»
    /// (<c>fact_progress</c> con el nombre visible y las líneas reveladas) y el nombre científico, en cursiva, si lo tiene
    /// (<see cref="ScientificName"/>). Con las 20 reveladas, el contador pasa al sello sobre el icono (Q5-A), como en la línea.</item>
    /// <item><b>Las líneas:</b> las reveladas (<see cref="IsRevealed"/>: nivel ≤ <c>progress.max_level</c> o en
    /// <c>progress.skipped_levels</c>, el Salto del Boto; <see cref="FactSchedule.IsLevelRevealed"/>, sin estado nuevo) van numeradas
    /// con su texto; las demás, en silueta con «Nivel {n}» (<c>fact_locked</c>), el nivel en que salen. Una revelada sin texto en
    /// el idioma de la UI (o, en release, sin aprobar) también va en silueta (propuesta §6.4, punto 5). Si la serie no empezó
    /// (ninguna revelada), en su lugar su frase de «Fruta nueva» (<c>fact.&lt;tinte&gt;.tag</c>, si tiene texto) y «Su serie empieza
    /// en el nivel {n}» (<c>fact_starts_at</c>).</item>
    /// <item><b>Fuentes</b> (<c>fact_sources</c>), al pie: el título y la dirección de cada fuente que citan las líneas que se leen
    /// (<see cref="SourcesOf"/>), como texto, sin abrir el navegador (propuesta §2.3; 07 §8.2) [P: solo las de las líneas a la
    /// vista].</item>
    /// </list>
    /// Todo texto sale de las tablas (<see cref="UiText"/> y <see cref="Loc.Fact(string)"/>) salvo los datos bibliográficos de las
    /// fuentes, que son del calendario (contrato 31). Textos ≥ 12 sp a cualquier escala hasta el 130 %, sin recortes (lo que no
    /// cabe se desplaza), contraste ≥ 4,5:1 (<see cref="ContrastPairs"/>) y cierre de 48 dp. Se compone al abrirse y al cambiar la
    /// pantalla: nunca por fotograma.
    /// </summary>
    internal sealed class FruitSheet
    {
        /// <summary>Orden del lienzo: encima de Ajustes (40) y del Mapa del río (45), debajo de la compra simulada (50).</summary>
        public const int SortingOrder = 46;

        /// <summary>Ancho máximo de la hoja (dp).</summary>
        public const float MaxWidthDp = 360f;

        /// <summary>Margen interior de la hoja (dp).</summary>
        public const float InsetDp = 16f;

        /// <summary>Icono de la fruta en la cabecera de la ficha (dp).</summary>
        public const float IconDp = 48f;

        /// <summary>Disco bajo el icono, para las frutas claras sobre la madera clara (dp).</summary>
        public const float IconBackDp = 42f;

        /// <summary>Sello de la serie completa sobre la esquina del icono (Q5-A) (dp).</summary>
        public const float SealDp = 22f;

        /// <summary>Aro del sello (dp).</summary>
        public const float SealRingDp = 2f;

        /// <summary>Nombre y contador («Açaí · 3/20») (sp).</summary>
        public const float NameSp = 18f;

        /// <summary>Nombre científico, en cursiva (sp).</summary>
        public const float ScientificSp = 13f;

        /// <summary>Número de una línea (sp).</summary>
        public const float NumberSp = 12f;

        /// <summary>Texto de una línea revelada (sp): el de la línea de la victoria.</summary>
        public const float LineSp = VictoryLineSlot.TextSp;

        /// <summary>«Nivel {n}» de una línea en silueta (sp).</summary>
        public const float LockedSp = 13f;

        /// <summary>Frase de «Fruta nueva» y «Su serie empieza en el nivel {n}» (sp).</summary>
        public const float StartSp = 14f;

        /// <summary>«Fuentes» (sp).</summary>
        public const float SourcesTitleSp = 14f;

        /// <summary>Título y dirección de una fuente (sp): el mínimo de 02 §12.</summary>
        public const float SourceSp = UiBuilder.MinTextSp;

        /// <summary>Alto de la barra que dibuja el texto en silueta (dp).</summary>
        public const float BarDp = 10f;

        /// <summary>Hueco entre dos líneas (dp).</summary>
        public const float RowGapDp = 10f;

        /// <summary>
        /// Espacio libre que queda sobre la hoja más alta (dp), con el velo: se ve que es una hoja sobre lo de antes, no una
        /// pantalla nueva.
        /// </summary>
        public const float TopGapDp = 40f;

        /// <summary>Tinta de los textos: la madera oscura sobre la madera clara (como las secciones de Ajustes).</summary>
        public static readonly Color Ink = Palette.FamilyWood.Deep;

        /// <summary>Tinta secundaria (nombre científico, «Nivel {n}», direcciones): la madera base (≥ 4,5:1 sobre la crema).</summary>
        public static readonly Color SoftInk = Palette.FamilyWood.Base;

        /// <summary>Disco del número de una línea revelada: la hoja de la opción elegida de Ajustes, con el número en crema.</summary>
        public static readonly Color NumberBack = Palette.FamilyLeaf.Deep;

        /// <summary>Disco del número y barra de una línea en silueta: la crema de la tarjeta oscurecida un 18 % [P].</summary>
        public static readonly Color LockedBack = new Color(Palette.PanelLightLow.r * 0.82f, Palette.PanelLightLow.g * 0.82f, Palette.PanelLightLow.b * 0.82f, 1f);

        /// <summary>Disco bajo el icono: la crema de la tarjeta oscurecida un 12 %, como el de la línea (<see cref="FactLine.IconBack"/>).</summary>
        public static readonly Color IconBack = FactLine.IconBack;

        /// <summary>Icono de una serie que aún no empezó: en silueta, como una parada futura del Mapa.</summary>
        public static readonly Color SilhouetteColor = StopCard.SilhouetteColor;

        /// <summary>Gota de las series de río [P] (sin fruta): el azul del agua lejana del río.</summary>
        public static readonly Color RiverDrop = Palette.RiverFar;

        // Id de una fuente citada en el context de una línea de facts.csv: «Fuente A1, …» o «Fuentes T1, …; T2, …» (el mismo
        // patrón que comprueba FactCatalogTests en cada línea del calendario).
        private static readonly Regex CitedSource = new Regex("(?:Fuentes? |; )([A-Z][A-Za-z]*[0-9]+), ", RegexOptions.CultureInvariant);

        private readonly ProceduralArt _art;
        private readonly DevProbe _probe;
        private readonly Action _onClose;
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
        private readonly Image _iconBack;
        private readonly Image _icon;
        private readonly Image _sealRing;
        private readonly Image _sealFace;
        private readonly Image _sealIcon;
        private readonly TextMeshProUGUI _name;
        private readonly TextMeshProUGUI _scientific;
        private readonly TextMeshProUGUI _tag;
        private readonly TextMeshProUGUI _startsAt;
        private readonly TextMeshProUGUI _sourcesTitle;
        private readonly List<LineRow> _rows = new List<LineRow>(20);
        private readonly List<SourceRow> _sourceRows = new List<SourceRow>(8);
        private readonly List<FactSource> _sources = new List<FactSource>(8);
        private readonly List<string> _shownKeys = new List<string>(20);

        private FactSeries? _series;
        private int _screenW;
        private int _screenH;
        private Rect _safeArea;

        /// <summary>Construye la hoja bajo <paramref name="host"/>, oculta. <paramref name="onClose"/> la cierra (su X y el velo).</summary>
        public FruitSheet(Transform host, ProceduralArt art, DevProbe probe, Action onClose)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            _probe = probe ?? throw new ArgumentNullException(nameof(probe));
            _onClose = onClose ?? throw new ArgumentNullException(nameof(onClose));
            _canvas = new OverlayCanvas("FruitSheetCanvas", host, SortingOrder);
            ModalKit.Dim("Dim", _canvas.Root, 0.62f);
            Button scrim = UiBuilder.BareButton("ScrimButton", _canvas.Root, () =>
            {
                AF.Audio.AudioCues.Play(AF.Audio.SfxId.UiBack); // como la X: cerrar suena a madera grave (AU1)
                _onClose();
            });
            UiBuilder.Stretch((RectTransform)scrim.transform);
            _panel = ModalKit.Card("Panel", _canvas.Root, art, out _surface);
            _surface.raycastTarget = true; // la hoja se queda con sus toques: no cierran la ficha
            _title = ModalKit.Header(_panel, art, UiText.FactSheetTitle, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, () => _onClose());

            // Lo de debajo de la cabecera va en un visor que se desplaza si no cabe (20 líneas al 130 %, como Ajustes).
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

            _iconBack = UiBuilder.Image("FruitIconBack", _content, art.Disc, IconBack);
            _icon = UiBuilder.Image("FruitIcon", _content, null, Color.white);
            _icon.preserveAspect = true;
            _sealRing = UiBuilder.Image("Seal", _content, art.Disc, FactLine.SealRing);
            _sealFace = UiBuilder.Image("SealFace", _content, art.Disc, Palette.PanelLight);
            _sealIcon = UiBuilder.Image("SealIcon", _content, null, Color.white);
            _sealIcon.preserveAspect = true;
            _name = WrappedLabel("Name", TextStyle.PlainBold, NameSp, Ink);
            _scientific = WrappedLabel("Scientific", TextStyle.Body, ScientificSp, SoftInk);
            _scientific.fontStyle = FontStyles.Italic;
            _tag = WrappedLabel("Tag", TextStyle.Body, StartSp, Ink);
            _startsAt = WrappedLabel("StartsAt", TextStyle.PlainBold, StartSp, Ink);
            _sourcesTitle = WrappedLabel("SourcesTitle", TextStyle.PlainBold, SourcesTitleSp, Ink);
            _sourcesTitle.text = UiText.FactSources;

            // Fundidos de los bordes del visor, como en Ajustes (revisión de AU1): abajo mientras quede ficha por debajo y arriba en
            // cuanto se ha desplazado, para que el corte no parezca un fallo (D-003). Encima del contenido y sin recibir toques.
            _fadeTop = UiBuilder.Image("FadeTop", _viewport, art.ScrollFade(true), Color.white);
            _fadeBottom = UiBuilder.Image("FadeBottom", _viewport, art.ScrollFade(false), Color.white);
            _scroll.onValueChanged.AddListener(_ => UpdateFades());
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>La hoja está abierta.</summary>
        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Lienzo propio (capturas de prueba).</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>La hoja (tests: dentro de la pantalla).</summary>
        public RectTransform Panel => _panel;

        /// <summary>Ancho del lienzo (dp).</summary>
        public float CanvasWidth => _canvas.Width;

        /// <summary>Alto del lienzo (dp).</summary>
        public float CanvasHeight => _canvas.Height;

        /// <summary>Visor del contenido desplazable.</summary>
        public RectTransform Viewport => _viewport;

        /// <summary>El contenido no cabe y se desplaza.</summary>
        public bool Scrolls => _scroll.vertical;

        /// <summary>Cierre (X).</summary>
        public Button CloseButton => _close.Button;

        /// <summary>Se ve el fundido de abajo del visor: queda ficha por debajo.</summary>
        public bool FadeBottomVisible => _fadeBottom.gameObject.activeSelf;

        /// <summary>Se ve el fundido de arriba del visor: el contenido se ha desplazado.</summary>
        public bool FadeTopVisible => _fadeTop.gameObject.activeSelf;

        /// <summary>Desde dónde se abrió la última vez.</summary>
        public FruitSheetSource Source { get; private set; }

        /// <summary>Serie que enseña (null antes de abrirla).</summary>
        public FactSeries? Series => _series;

        /// <summary>Líneas reveladas de la serie (el <c>unlocked</c> de <c>af_fact_page_open</c>).</summary>
        public int Unlocked { get; private set; }

        /// <summary>La serie empezó: alguna línea revelada.</summary>
        public bool Started => Unlocked > 0;

        /// <summary>Las 20 líneas reveladas: el contador pasa al sello (Q5-A).</summary>
        public bool Sealed { get; private set; }

        /// <summary>Título de la cabecera («Frutas del río»).</summary>
        public string TitleText => _title.text;

        /// <summary>«Açaí · 3/20», o solo el nombre con la serie completa.</summary>
        public string NameText => _name.text;

        /// <summary>Nombre científico (vacío si no tiene).</summary>
        public string ScientificText => _scientific.gameObject.activeSelf ? _scientific.text : string.Empty;

        /// <summary>Frase de «Fruta nueva» de una serie sin empezar (vacía si no se ve).</summary>
        public string TagText => _tag.gameObject.activeSelf ? _tag.text : string.Empty;

        /// <summary>«Su serie empieza en el nivel {n}» de una serie sin empezar (vacío si no se ve).</summary>
        public string StartsAtText => _startsAt.gameObject.activeSelf ? _startsAt.text : string.Empty;

        /// <summary>Líneas de la ficha que se ven (0 en una serie sin empezar).</summary>
        public int RowCount { get; private set; }

        /// <summary>Claves de las líneas que se leen (reveladas con texto), en orden.</summary>
        public IReadOnlyList<string> ShownKeys => _shownKeys;

        /// <summary>Fuentes del pie, en el orden en que las citan las líneas que se leen.</summary>
        public IReadOnlyList<FactSource> Sources => _sources;

        /// <summary>Se ve el pie de «Fuentes».</summary>
        public bool SourcesShown => _sourcesTitle.gameObject.activeSelf;

        /// <summary>El icono de la fruta (o la gota de una serie de río).</summary>
        public Image Icon => _icon;

        /// <summary>El sello de la serie completa (su aro).</summary>
        public Image Seal => _sealRing;

        /// <summary>La línea <paramref name="i"/> (0..<see cref="RowCount"/> − 1) va revelada con su texto.</summary>
        public bool RowRevealed(int i)
        {
            return Row(i).Revealed;
        }

        /// <summary>Número de la línea <paramref name="i"/> («1» … «20»).</summary>
        public string RowNumber(int i)
        {
            return Row(i).Number.text;
        }

        /// <summary>Texto de la línea <paramref name="i"/>: el de la línea revelada o «Nivel {n}» en silueta.</summary>
        public string RowText(int i)
        {
            LineRow row = Row(i);
            return row.Revealed ? row.Text.text : row.Locked.text;
        }

        /// <summary>Todos los textos que se ven (tests: ≥ 12 sp y sin recortes).</summary>
        public IEnumerable<TextMeshProUGUI> Texts()
        {
            yield return _title;
            foreach (TextMeshProUGUI t in new[] { _name, _scientific, _tag, _startsAt, _sourcesTitle })
            {
                if (t.gameObject.activeSelf && t.text.Length > 0)
                {
                    yield return t;
                }
            }

            for (int i = 0; i < RowCount; i++)
            {
                LineRow row = _rows[i];
                yield return row.Number;
                yield return row.Revealed ? row.Text : row.Locked;
            }

            for (int i = 0; i < _sources.Count; i++)
            {
                yield return _sourceRows[i].Title;
                yield return _sourceRows[i].Url;
            }
        }

        /// <summary>
        /// Pares texto / fondo de la ficha, para la comprobación de contraste ≥ 4,5:1 (07 §1.2 y §7.8): los textos sobre los dos
        /// extremos de la madera clara y el número sobre su disco.
        /// </summary>
        public static (string Name, Color Text, Color Background)[] ContrastPairs()
        {
            return new[]
            {
                ("texto sobre la madera clara", Ink, (Color)Palette.PanelLight),
                ("texto sobre la madera clara baja", Ink, (Color)Palette.PanelLightLow),
                ("científico, nivel y fuente sobre la madera clara", SoftInk, (Color)Palette.PanelLight),
                ("científico, nivel y fuente sobre la madera clara baja", SoftInk, (Color)Palette.PanelLightLow),
                ("número de una línea revelada", (Color)Palette.Cream, NumberBack),
                ("número de una línea en silueta", Ink, LockedBack),
            };
        }

        // ============================================================================================ reglas puras

        /// <summary>
        /// La línea del nivel <paramref name="level"/> está revelada con el progreso <paramref name="progress"/> (D-017 Q4-A;
        /// propuesta de Frutas §6.4, paso 4; sin estado nuevo): <see cref="FactSchedule.IsLevelRevealed"/> con
        /// <c>progress.max_level</c>, o el nivel está en <c>progress.skipped_levels</c> (Salto del Boto, 04 §2.6). Sin asignaciones.
        /// </summary>
        public static bool IsRevealed(int level, IMapProgress progress)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            return FactSchedule.IsLevelRevealed(level, progress.MaxLevel, null) || (level > 0 && progress.IsSkipped(level));
        }

        /// <summary>Líneas reveladas de <paramref name="series"/> con <paramref name="progress"/> (el <c>unlocked</c> de la sonda).</summary>
        public static int UnlockedCount(FactSeries series, IMapProgress progress)
        {
            if (series == null)
            {
                throw new ArgumentNullException(nameof(series));
            }

            int n = 0;
            for (int k = 1; k <= series.Lines; k++)
            {
                if (IsRevealed(series.LevelAt(k), progress))
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>
        /// Nombre científico de la serie de un tinte de fruta [P]: el que da el <c>context</c> de sus líneas en <c>facts.csv</c>
        /// («línea k/n de la serie &lt;tinte&gt; (&lt;Nombre&gt;; &lt;nombre científico&gt;), …», el formato que comprueba
        /// FactCatalogTests), solo si es un binomio latino (<c>Euterpe oleracea</c>), que no se traduce (propuesta de Frutas §6.5).
        /// Las aguas y los ríos no tienen: su paréntesis dice el río y su tipo de agua en español. Null si no hay. Ni el calendario
        /// ni las tablas tienen aún un campo propio para él (open issue de F4).
        /// </summary>
        public static string? ScientificName(FactSeries series, LocTable facts)
        {
            if (series == null || facts == null)
            {
                throw new ArgumentNullException(series == null ? nameof(series) : nameof(facts));
            }

            if (series.Kind != FactSeriesKind.Tint)
            {
                return null;
            }

            var pattern = new Regex("de la serie " + Regex.Escape(series.Subject) + @" \([^;()]+; ([A-Z][a-z]+ [a-z]+)\)", RegexOptions.CultureInvariant);
            for (int k = 1; k <= series.Lines; k++)
            {
                int row = facts.IndexOf(series.KeyAt(k));
                if (row < 0)
                {
                    continue;
                }

                Match m = pattern.Match(facts.ContextAt(row));
                if (m.Success)
                {
                    return m.Groups[1].Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Fuentes del calendario <paramref name="catalog"/> que citan en <c>facts.csv</c> las líneas <paramref name="keys"/>, en el
        /// orden en que se citan y sin repetir, en <paramref name="into"/> (que vacía antes). Un id que no esté en el catálogo se
        /// salta.
        /// </summary>
        public static int SourcesOf(IReadOnlyList<string> keys, FactCatalog catalog, LocTable facts, List<FactSource> into)
        {
            if (keys == null || catalog == null || facts == null || into == null)
            {
                throw new ArgumentNullException(keys == null ? nameof(keys) : catalog == null ? nameof(catalog) : facts == null ? nameof(facts) : nameof(into));
            }

            into.Clear();
            for (int i = 0; i < keys.Count; i++)
            {
                int row = facts.IndexOf(keys[i]);
                if (row < 0)
                {
                    continue;
                }

                foreach (Match m in CitedSource.Matches(facts.ContextAt(row)))
                {
                    FactSource? source = catalog.Source(m.Groups[1].Value);
                    if (source != null && !into.Contains(source))
                    {
                        into.Add(source);
                    }
                }
            }

            return into.Count;
        }

        // ============================================================================================ abrir y cerrar

        /// <summary>
        /// Abre la ficha de <paramref name="series"/> (del calendario <paramref name="catalog"/>) con el progreso
        /// <paramref name="progress"/>, abierta desde <paramref name="source"/>. False (y no abre) si la ficha no se puede enseñar
        /// en el idioma de la UI (<see cref="UiText.FruitSheetVisible"/>) o el nombre de la serie no tiene texto (en release, sin
        /// aprobar): nunca una ficha a medias ni en dos idiomas.
        /// </summary>
        public bool Show(FactSeries series, FactCatalog catalog, IMapProgress progress, FruitSheetSource source)
        {
            if (series == null || catalog == null || progress == null)
            {
                throw new ArgumentNullException(series == null ? nameof(series) : catalog == null ? nameof(catalog) : nameof(progress));
            }

            string name = Loc.Fact(series.NameKey);
            if (name.Length == 0 || !UiText.FruitSheetVisible)
            {
                return false;
            }

            _series = series;
            Source = source;
            Unlocked = UnlockedCount(series, progress);
            Sealed = Unlocked == series.Lines;
            _canvas.Canvas.gameObject.SetActive(true);
            Compose(series, catalog, progress, name);
            ReadScreen();
            Layout();
            EmitLayout();
            return true;
        }

        /// <summary>Cierra la hoja (sin avisar a nadie: lo hace quien la cierra).</summary>
        public void Hide()
        {
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>Con la hoja abierta, la vuelve a colocar si cambió la pantalla o la zona segura.</summary>
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

        /// <summary>Desplaza el contenido hasta el final (tests y capturas).</summary>
        public void ScrollToEnd()
        {
            _scroll.verticalNormalizedPosition = 0f;
            UpdateFades();
        }

        // Los textos y las imágenes de la serie (al abrirla; nunca por fotograma).
        private void Compose(FactSeries series, FactCatalog catalog, IMapProgress progress, string name)
        {
            LocTable facts = Loc.Catalog.Facts;
            int tint = FactLine.TintOf(series.Subject);
            Sprite? fruit = tint > 0 ? _art.FruitFlight(tint) : null;
            bool river = series.Kind == FactSeriesKind.River;
            _icon.sprite = fruit ?? (river ? _art.Drop : null);
            _icon.color = !Started ? SilhouetteColor : fruit != null ? Color.white : RiverDrop;
            SetActive(_icon, _icon.sprite != null);
            SetActive(_iconBack, _icon.sprite != null);

            _name.text = Sealed ? name : UiText.FactProgress(name, Unlocked, series.Lines);
            SetActive(_sealRing, Sealed);
            SetActive(_sealFace, Sealed);
            _sealIcon.sprite = fruit;
            SetActive(_sealIcon, Sealed && fruit != null);

            string? scientific = ScientificName(series, facts);
            _scientific.text = scientific ?? string.Empty;
            SetActive(_scientific, scientific != null);

            // Las líneas, o la frase de «Fruta nueva» y el nivel en que empieza la serie.
            _shownKeys.Clear();
            RowCount = Started ? series.Lines : 0;
            while (_rows.Count < RowCount)
            {
                _rows.Add(new LineRow(_content, _art, _rows.Count));
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                LineRow row = _rows[i];
                bool used = i < RowCount;
                row.SetActive(used);
                if (!used)
                {
                    continue;
                }

                int k = i + 1;
                int level = series.LevelAt(k);
                string key = series.KeyAt(k);
                string text = IsRevealed(level, progress) ? Loc.Fact(key) : string.Empty;
                row.Bind(UiText.Grouped(k), text, UiText.FactLocked(level));
                if (text.Length > 0)
                {
                    _shownKeys.Add(key);
                }
            }

            string tag = !Started && series.TagKey != null ? Loc.Fact(series.TagKey) : string.Empty;
            _tag.text = tag;
            SetActive(_tag, tag.Length > 0);
            _startsAt.text = Started ? string.Empty : UiText.FactStartsAt(series.FromLevel);
            SetActive(_startsAt, !Started);

            // Las fuentes de las líneas que se leen.
            SourcesOf(_shownKeys, catalog, facts, _sources);
            while (_sourceRows.Count < _sources.Count)
            {
                _sourceRows.Add(new SourceRow(_content, _sourceRows.Count));
            }

            for (int i = 0; i < _sourceRows.Count; i++)
            {
                bool used = i < _sources.Count;
                _sourceRows[i].SetActive(used);
                if (used)
                {
                    _sourceRows[i].Bind(_sources[i]);
                }
            }

            SetActive(_sourcesTitle, _sources.Count > 0);
        }

        // Pantalla, zona segura y escala del lienzo de ahora.
        private void ReadScreen()
        {
            GameScreen.ReadScreen(out _screenW, out _screenH, out _safeArea);
            _canvas.Measure();
        }

        private void Layout()
        {
            float s = Mathf.Clamp(UiBuilder.TextScale, 1f, UiBuilder.MaxTextScale);
            float cw = _canvas.Width;
            float ch = _canvas.Height;
            float px = _screenW > 0 ? _screenW / Mathf.Max(1f, cw) : 1f;
            float safeTop = Mathf.Max(GameScreen.MinSafeTop, _screenH > 0 ? (_screenH - _safeArea.yMax) / px : 0f);
            float safeBottom = Mathf.Max(GameScreen.MinSafeBottom, _screenH > 0 ? _safeArea.yMin / px : 0f);
            float w = Mathf.Min(MaxWidthDp, cw);
            float inner = w - (2f * InsetDp);

            // Cabecera: la placa con el título y el cierre a su derecha.
            float headerH = Mathf.Max(40f, Mathf.Round((22f * s) + 12f));
            _title.fontSizeMax = 22f * s;
            _title.fontSizeMin = Mathf.Max(16f, UiBuilder.MinTextSp * s);
            float y = InsetDp;
            UiBuilder.Place(_header.rectTransform, InsetDp, y + ((ModalKit.CloseDp - headerH) * 0.5f), inner - ModalKit.CloseDp - 8f, headerH);
            UiBuilder.Place((RectTransform)_close.Button.transform, w - InsetDp - ModalKit.CloseDp, y, ModalKit.CloseDp, ModalKit.CloseDp);
            float headerBottom = y + Mathf.Max(ModalKit.CloseDp, headerH) + 12f;

            float contentH = LayoutContent(w, s);

            // La hoja crece con el contenido hasta dejar TopGapDp libres bajo la zona segura; si no cabe, el contenido se
            // desplaza. Va pegada abajo (8 dp de marco quedan fuera de la pantalla) y su relleno de abajo salva la zona de gestos.
            float bottomPad = InsetDp + safeBottom;
            float maxH = Mathf.Max(headerBottom + 48f + bottomPad, ch - safeTop - TopGapDp + 8f);
            float h = Mathf.Min(headerBottom + contentH + bottomPad + 8f, maxH);
            float viewH = h - headerBottom - bottomPad - 8f;
            _scroll.vertical = contentH > viewH + 0.5f;
            UiBuilder.Place(_viewport, 0f, headerBottom, w, viewH);
            UiBuilder.Place(_content, 0f, 0f, w, contentH);
            _scroll.velocity = Vector2.zero;
            ModalKit.Size(_panel, _surface, _art, Mathf.Round((cw - w) * 0.5f), ch - h + 8f, w, h, true);

            // Los fundidos, del color de la superficie en cada borde del visor, sobre el ancho del texto con 2 dp de aire.
            float surfaceH = h - 16f; // la superficie va 8 dp dentro de la hoja por cada lado (UiBuilder.ModalBody)
            _fadeTop.color = ProceduralArt.ModalSurfaceColorAt(headerBottom - 8f, surfaceH);
            _fadeBottom.color = ProceduralArt.ModalSurfaceColorAt(headerBottom + viewH - 8f, surfaceH);
            UiBuilder.Place(_fadeTop.rectTransform, 14f, 0f, w - 28f, SettingsScreen.FadeTopDp);
            UiBuilder.Place(_fadeBottom.rectTransform, 14f, viewH - SettingsScreen.FadeBottomDp, w - 28f, SettingsScreen.FadeBottomDp);
            UpdateFades();
        }

        // Cada fundido sale solo si queda contenido por ese lado (verticalNormalizedPosition: 1 arriba, 0 abajo).
        private void UpdateFades()
        {
            bool scrolls = _scroll.vertical;
            float pos = scrolls ? _scroll.verticalNormalizedPosition : 1f;
            SetActive(_fadeTop, scrolls && pos < 0.99f);
            SetActive(_fadeBottom, scrolls && pos > 0.01f);
        }

        // Coloca el contenido desplazable de arriba abajo y devuelve su alto (dp).
        private float LayoutContent(float w, float s)
        {
            float x = InsetDp;
            float right = w - InsetDp;
            float y = 0f;

            // La fruta: icono, «Açaí · 3/20» y el nombre científico, centrados a lo alto con el icono.
            float textX = x + IconDp + 12f;
            float textW = right - textX;
            float nameH = Measure(_name, NameSp * s, textW);
            float sciH = _scientific.gameObject.activeSelf ? Measure(_scientific, ScientificSp * s, textW) : 0f;
            float textH = nameH + (sciH > 0f ? 2f + sciH : 0f);
            float block = Mathf.Max(IconDp, textH);
            float ty = y + Mathf.Round((block - textH) * 0.5f);
            UiBuilder.Place(_name.rectTransform, textX, ty, textW, nameH);
            UiBuilder.Place(_scientific.rectTransform, textX, ty + nameH + 2f, textW, Mathf.Max(1f, sciH));
            float iy = y + Mathf.Round((block - IconDp) * 0.5f);
            UiBuilder.Place(_iconBack.rectTransform, x + ((IconDp - IconBackDp) * 0.5f), iy + ((IconDp - IconBackDp) * 0.5f), IconBackDp, IconBackDp);
            UiBuilder.Place(_icon.rectTransform, x, iy, IconDp, IconDp);
            float sx = x + IconDp - SealDp + 4f;
            float sy = iy + IconDp - SealDp + 4f;
            UiBuilder.Place(_sealRing.rectTransform, sx, sy, SealDp, SealDp);
            float face = SealDp - (2f * SealRingDp);
            UiBuilder.Place(_sealFace.rectTransform, sx + SealRingDp, sy + SealRingDp, face, face);
            UiBuilder.Place(_sealIcon.rectTransform, sx + SealRingDp + 1f, sy + SealRingDp + 1f, face - 2f, face - 2f);
            y += block + 14f;

            // Las líneas: el número en su disco y el texto, o «Nivel {n}» y la barra en silueta.
            float disc = Mathf.Max(24f, Mathf.Round((NumberSp * s) + 10f));
            float tx = x + disc + 10f;
            float tw = right - tx;
            for (int i = 0; i < RowCount; i++)
            {
                y += _rows[i].Place(x, y, disc, tx, tw, s) + RowGapDp;
            }

            // Una serie sin empezar: su frase de «Fruta nueva» y el nivel en que empieza.
            if (_tag.gameObject.activeSelf)
            {
                float h = Measure(_tag, StartSp * s, right - x);
                UiBuilder.Place(_tag.rectTransform, x, y, right - x, h);
                y += h + 8f;
            }

            if (_startsAt.gameObject.activeSelf)
            {
                float h = Measure(_startsAt, StartSp * s, right - x);
                UiBuilder.Place(_startsAt.rectTransform, x, y, right - x, h);
                y += h + RowGapDp;
            }

            // Fuentes, al pie: el título y la dirección de cada una, como texto.
            if (_sourcesTitle.gameObject.activeSelf)
            {
                y += 8f;
                float h = Measure(_sourcesTitle, SourcesTitleSp * s, right - x);
                UiBuilder.Place(_sourcesTitle.rectTransform, x, y, right - x, h);
                y += h + 6f;
                for (int i = 0; i < _sources.Count; i++)
                {
                    y += _sourceRows[i].Place(x, y, right - x, s) + 8f;
                }
            }

            return Mathf.Ceil(Mathf.Max(0f, y - RowGapDp + 4f));
        }

        private TextMeshProUGUI WrappedLabel(string name, TextStyle style, float sp, Color color)
        {
            return Wrapped(name, _content, style, sp, color);
        }

        private static TextMeshProUGUI Wrapped(string name, Transform parent, TextStyle style, float sp, Color color)
        {
            TextMeshProUGUI label = UiBuilder.Label(name, parent, style, string.Empty, sp, color, TextAlignmentOptions.TopLeft);
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        // Alto del texto de label a sizeDp en widthDp (y fija ese tamaño); nunca se encoge: lo que no cabe, crece.
        private static float Measure(TextMeshProUGUI label, float sizeDp, float widthDp)
        {
            label.enableAutoSizing = false;
            label.fontSize = sizeDp;
            return label.text.Length == 0 ? 0f : Mathf.Ceil(label.GetPreferredValues(label.text, widthDp, float.PositiveInfinity).y);
        }

        private static float MeasureWidth(TextMeshProUGUI label, float sizeDp)
        {
            label.enableAutoSizing = false;
            label.fontSize = sizeDp;
            return label.text.Length == 0 ? 0f : Mathf.Ceil(label.GetPreferredValues(label.text, float.PositiveInfinity, float.PositiveInfinity).x);
        }

        private static void SetActive(Component c, bool on)
        {
            if (c.gameObject.activeSelf != on)
            {
                c.gameObject.SetActive(on);
            }
        }

        private LineRow Row(int i)
        {
            if (i < 0 || i >= RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(i), i, "La ficha enseña " + RowCount + " líneas.");
            }

            return _rows[i];
        }

        private void EmitLayout()
        {
            if (!DevProbe.Enabled || !Visible)
            {
                return;
            }

            _probe.Layout("fruit_sheet", 0, null, new[] { "close" }, new[] { (RectTransform)_close.Button.transform });
        }

        // Una línea de la ficha: el número en su disco y el texto (revelada) o «Nivel {n}» con la barra (en silueta).
        private sealed class LineRow
        {
            private readonly Image _disc;
            private readonly Image _bar;

            public LineRow(Transform parent, ProceduralArt art, int index)
            {
                string name = "Line" + (index + 1);
                _disc = UiBuilder.Image(name + "Disc", parent, art.Disc, NumberBack);
                Number = UiBuilder.Label(name + "Number", parent, TextStyle.PlainBold, string.Empty, NumberSp, Palette.Cream, TextAlignmentOptions.Center);
                Text = Wrapped(name + "Text", parent, TextStyle.Body, LineSp, Ink);
                Locked = UiBuilder.Label(name + "Locked", parent, TextStyle.PlainBold, string.Empty, LockedSp, SoftInk, TextAlignmentOptions.MidlineLeft);
                _bar = UiBuilder.Sliced(name + "Bar", parent, art.RoundedRect, LockedBack);
                _bar.pixelsPerUnitMultiplier = 24f / (BarDp * 0.5f);
            }

            public TextMeshProUGUI Number { get; }

            public TextMeshProUGUI Text { get; }

            public TextMeshProUGUI Locked { get; }

            public bool Revealed { get; private set; }

            public void Bind(string number, string text, string locked)
            {
                Revealed = text.Length > 0;
                Number.text = number;
                Number.color = Revealed ? (Color)Palette.Cream : Ink;
                _disc.color = Revealed ? NumberBack : LockedBack;
                Text.text = text;
                Locked.text = Revealed ? string.Empty : locked;
                FruitSheet.SetActive(Text, Revealed);
                FruitSheet.SetActive(Locked, !Revealed);
                FruitSheet.SetActive(_bar, !Revealed);
            }

            public void SetActive(bool on)
            {
                FruitSheet.SetActive(_disc, on);
                FruitSheet.SetActive(Number, on);
                FruitSheet.SetActive(Text, on && Revealed);
                FruitSheet.SetActive(Locked, on && !Revealed);
                FruitSheet.SetActive(_bar, on && !Revealed);
            }

            // Coloca la línea en y y devuelve su alto: el disco centrado en la primera línea del texto.
            public float Place(float x, float y, float disc, float tx, float tw, float s)
            {
                float numberH = Measure(Number, NumberSp * s, disc);
                float rowH;
                if (Revealed)
                {
                    float h = Measure(Text, LineSp * s, tw);
                    float first = Mathf.Ceil(Text.GetPreferredValues("Ág", tw, float.PositiveInfinity).y);
                    float discY = Mathf.Round((first - disc) * 0.5f);
                    float textY = discY < 0f ? -discY : 0f;
                    discY = Mathf.Max(0f, discY);
                    UiBuilder.Place(Text.rectTransform, tx, y + textY, tw, h);
                    UiBuilder.Place(_disc.rectTransform, x, y + discY, disc, disc);
                    UiBuilder.Place(Number.rectTransform, x, y + discY + Mathf.Round((disc - numberH) * 0.5f), disc, numberH);
                    rowH = Mathf.Max(disc + discY, h + textY);
                }
                else
                {
                    float lw = MeasureWidth(Locked, LockedSp * s);
                    float lh = Measure(Locked, LockedSp * s, float.PositiveInfinity);
                    rowH = Mathf.Max(disc, lh);
                    UiBuilder.Place(_disc.rectTransform, x, y + Mathf.Round((rowH - disc) * 0.5f), disc, disc);
                    UiBuilder.Place(Number.rectTransform, x, y + Mathf.Round((rowH - numberH) * 0.5f), disc, numberH);
                    UiBuilder.Place(Locked.rectTransform, tx, y + Mathf.Round((rowH - lh) * 0.5f), lw + 1f, lh);
                    float bx = tx + lw + 12f;
                    UiBuilder.Place(_bar.rectTransform, bx, y + Mathf.Round((rowH - BarDp) * 0.5f), Mathf.Max(BarDp, tx + tw - bx), BarDp);
                }

                return rowH;
            }
        }

        // Una fuente del pie: su título y su dirección, cada uno en su etiqueta (nunca pegados en una cadena).
        private sealed class SourceRow
        {
            public SourceRow(Transform parent, int index)
            {
                Title = Wrapped("Source" + (index + 1) + "Title", parent, TextStyle.Body, SourceSp, Ink);
                Url = Wrapped("Source" + (index + 1) + "Url", parent, TextStyle.Body, SourceSp, SoftInk);
            }

            public TextMeshProUGUI Title { get; }

            public TextMeshProUGUI Url { get; }

            public void Bind(FactSource source)
            {
                Title.text = source.Title;
                Url.text = source.Url;
            }

            public void SetActive(bool on)
            {
                FruitSheet.SetActive(Title, on);
                FruitSheet.SetActive(Url, on);
            }

            public float Place(float x, float y, float w, float s)
            {
                float th = Measure(Title, SourceSp * s, w);
                UiBuilder.Place(Title.rectTransform, x, y, w, th);
                float uh = Measure(Url, SourceSp * s, w);
                UiBuilder.Place(Url.rectTransform, x, y + th + 2f, w, uh);
                return th + 2f + uh;
            }
        }
    }
}
