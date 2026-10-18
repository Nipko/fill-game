#nullable enable
using System;
using System.Collections.Generic;
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
    /// La línea de fruta de Frutas del río en la ranura del Dato del río de la victoria (D-017, Q1-A a Q7-A; 02 §6, fila «Dato
    /// del río»; propuesta de Frutas §2.2, reglas 1-9, §2.5, §2.6, §2.8 y §2.10; arquitectura de la tanda 4, §2; tarea F3). Es un
    /// proveedor de <see cref="VictoryLineSlot"/> y enseña la línea que le da el árbitro (<see cref="VictoryLine.FruitKey"/>;
    /// <see cref="VictoryLineArbiter"/>: el 10 con el Mapa, los <c>10k</c> con dato de llegada y los hitos en que habla TiTino
    /// no llevan línea de fruta; la cadencia es <c>GameConfig.FruitLineEveryN</c>). Siempre obedece al árbitro: si da la línea de
    /// fruta, sale.
    /// <list type="bullet">
    /// <item><b>Cabecera</b> de <see cref="VictoryLineSlot.HeaderDp"/> dp: el icono de la fruta a <see cref="IconDp"/> dp (la
    /// fruta en vuelo del tinte, <see cref="ProceduralArt.FruitFlight"/>, con su PNG <c>fruta_&lt;tinte&gt;</c> si existe), «Dato del
    /// río» (<c>river_fact</c>) y, a continuación en la misma fila, un punto medio dibujado (<see cref="SeparatorDotDp"/> dp) y
    /// «Açaí · 3/20» (<c>fact_progress</c>), en Nunito Bold de <see cref="HeaderSp"/> sp: «Dato del río · Açaí · 3/20». [P] 02
    /// §6, D-017 Q1-A y el glosario §6.4 dicen «Dato del río · Açaí 3/20», y el glosario §10 da <c>fact_progress</c> =
    /// «{name} · {k}/{n}»: la contradicción es del dueño (open issue de F3). Son dos etiquetas contiguas, nunca una cadena pegada
    /// a otra; si no caben en una fila (al 130 % o con la pseudolocalización), el contador baja a una segunda fila, sin el punto,
    /// y la cabecera crece. En la última línea de la serie el contador pasa a un sello pequeño con el icono de la fruta, junto al
    /// nombre, sin animación (Q5-A; propuesta §2.6).</item>
    /// <item><b>Texto</b> (<c>Loc.Fact</c>) a todo el ancho, en Nunito Regular de <see cref="VictoryLineSlot.TextSp"/> sp, que crece
    /// hacia abajo hasta el alto fijo H de la línea de fruta (el de la ranura, <see cref="VictoryLineSlot.ContentHeightFor"/>); si
    /// una línea real no cabe, baja de medio en medio punto hasta <see cref="UiBuilder.MinTextSp"/> sp, nunca menos, y nunca se
    /// recorta.</item>
    /// <item><b>Tiempos:</b> entra con «Siguiente», a los <see cref="VictoryLineSlot.FruitRevealMs"/> ms de la pantalla de
    /// victoria (la fila 1,40 s de 07 §5.4), con un fundido de <see cref="VictoryLineSlot.FruitFadeMs"/> ms; con Reducir
    /// movimiento, sin fundido (<see cref="VictoryLineSlot.FruitLineAlpha"/>). Sin sonido ni confeti; no bloquea, no pide toques y
    /// no es un takeover.</item>
    /// <item><b>Toques</b> (tarea F4; D-017 Q4-A; propuesta de Frutas §2.2, regla 4): toda la línea es un botón de
    /// <see cref="VictoryLineSlot.MinContentDp"/> dp de alto como mínimo (<see cref="TapArea"/>) que abre la ficha de su fruta
    /// (<see cref="FruitSheet"/>; lo hace quien da <see cref="Tapped"/>, <c>GameBootstrap</c>). Recibe toques en cuanto se ve (con
    /// «Siguiente», a los <see cref="VictoryLineSlot.FruitRevealMs"/> ms), nunca antes, y entra en <c>AF_LAYOUT victory</c> como
    /// <see cref="TapId"/>, solo si la ficha se puede abrir (<see cref="Opens"/>: en release, con su título y su contador
    /// aprobados). Sus gráficos no reciben toques: solo el área táctil.</item>
    /// <item><b>Textos:</b> solo si están todos en el idioma de la UI (<see cref="UiText.TryFruitLine"/>): nunca mezcla idiomas y,
    /// en release, nunca enseña nada sin aprobar.</item>
    /// <item><b>Sitio:</b> arriba de la ranura, a <see cref="TopAirDp"/> dp como mucho bajo su borde, con el sobrante de la ranura
    /// debajo (la ranura puede ser más alta que H por la llegada, M1.6). Si la victoria lleva además la placa del lugar sin dato
    /// (con <see cref="RiverFactPolicy.SingleKey"/> en los <c>10k</c>, o una llegada sin dato; arquitectura de la tanda 4, §2), la
    /// placa va arriba (<see cref="ArrivalStrip.Stacked"/>) y la línea, <see cref="StackGapDp"/> dp debajo; la ranura reserva
    /// la placa más alta de esos casos más H (GameScreen.Victory.cs).</item>
    /// </list>
    /// Nada de lo que hace por fotograma asigna memoria: los textos y las medidas se hacen al enseñarla y al recolocar la victoria.
    /// </summary>
    internal sealed class FactLine : IVictoryLineProvider
    {
        /// <summary>Icono de la fruta de la cabecera (02 §6; propuesta §2.2, regla 5).</summary>
        public const float IconDp = VictoryLineSlot.HeaderDp;

        /// <summary>
        /// Disco bajo el icono de la cabecera, para que las frutas claras (copoazú, y las blancas) se distingan sobre la crema del
        /// panel (revisión de F3) [P].
        /// </summary>
        public const float IconBackDp = 24f;

        /// <summary>Del icono a la cabecera [P].</summary>
        public const float IconGapDp = 8f;

        /// <summary>Cabecera: Nunito Bold de 12 sp (02 §6).</summary>
        public const float HeaderSp = UiBuilder.MinTextSp;

        /// <summary>Hueco a cada lado del punto medio que separa «Dato del río» del contador en la misma fila [P].</summary>
        public const float HeaderGapDp = 6f;

        /// <summary>Punto medio dibujado entre «Dato del río» y el contador (el « · » de 02 §6) [P].</summary>
        public const float SeparatorDotDp = 3f;

        /// <summary>Aire máximo entre el borde de arriba de la ranura y la línea (revisión de F3) [P].</summary>
        public const float TopAirDp = 12f;

        /// <summary>De la placa del lugar a la línea de fruta cuando van juntas (<see cref="ArrivalStrip.Stacked"/>) [P].</summary>
        public const float StackGapDp = 8f;

        /// <summary>Sello de la última línea de la serie (Q5-A): un disco de 20 dp [P].</summary>
        public const float SealDp = 20f;

        /// <summary>Aro del sello [P].</summary>
        public const float SealRingDp = 2f;

        /// <summary>Del nombre al sello [P].</summary>
        public const float SealGapDp = 4f;

        /// <summary>Id del área táctil de la línea en <c>AF_LAYOUT victory</c> (F4).</summary>
        public const string TapId = "fruit_line";

        /// <summary>Paso con el que baja el texto si una línea real no cabe en H [P].</summary>
        public const float ShrinkStepSp = 0.5f;

        /// <summary>Tinta de la cabecera: madera oscura sobre el panel (≥ 4,5:1; propuesta §2.8).</summary>
        public static readonly Color HeaderInk = Palette.FamilyWood.Deep;

        /// <summary>Tinta del texto (07 §7.2).</summary>
        public static readonly Color TextInk = Palette.UiInk;

        /// <summary>Disco bajo el icono de la cabecera: la crema del panel oscurecida un 12 % (revisión de F3) [P].</summary>
        public static readonly Color IconBack = new Color(Palette.PanelLight.r * 0.88f, Palette.PanelLight.g * 0.88f, Palette.PanelLight.b * 0.88f, 1f);

        /// <summary>Aro del sello: el verde hoja de la cinta «¡Nivel completado!» [P].</summary>
        public static readonly Color SealRing = Palette.FamilyLeaf.Deep;

        private readonly ProceduralArt _art;
        private readonly Func<FactSchedule?> _schedule;
        private readonly Func<float> _blockDp;
        private readonly Func<float> _aboveDp;
        private readonly TextMeshProUGUI _measureBold;
        private readonly TextMeshProUGUI _measureBody;
        private readonly List<NeedEntry> _needs = new List<NeedEntry>(4);

        private RectTransform? _root;
        private CanvasGroup? _group;
        private Image? _iconBack;
        private Image? _icon;
        private TextMeshProUGUI? _headerLabel;
        private Image? _headerDot;
        private TextMeshProUGUI? _progressLabel;
        private Image? _sealRing;
        private Image? _sealFace;
        private Image? _sealIcon;
        private TextMeshProUGUI? _textLabel;
        private Button? _tap;
        private float _width;
        private float _height;
        private bool _reduceMotion;
        private bool _opens;

        /// <summary>
        /// Crea la línea. <paramref name="measureParent"/> aloja las etiquetas de medir (ocultas); <paramref name="schedule"/> da el
        /// calendario de Frutas del río de ahora; <paramref name="blockDp"/>, el alto fijo H de la línea de fruta en el idioma, la
        /// escala y el ancho de ahora (de la caché de la ranura); <paramref name="aboveDp"/>, lo que ocupa encima la placa del lugar
        /// cuando va con la línea (<see cref="ArrivalStrip.Stacked"/>; 0 si no).
        /// </summary>
        public FactLine(ProceduralArt art, RectTransform measureParent, Func<FactSchedule?> schedule, Func<float> blockDp, Func<float> aboveDp)
        {
            _art = art;
            _schedule = schedule;
            _blockDp = blockDp;
            _aboveDp = aboveDp;
            RectTransform measure = UiBuilder.Rect("FactLineMeasure", measureParent);
            _measureBold = UiBuilder.Label("MeasureBold", measure, TextStyle.PlainBold, string.Empty, HeaderSp, Color.clear, TextAlignmentOptions.TopLeft);
            _measureBody = UiBuilder.Label("MeasureBody", measure, TextStyle.Body, string.Empty, VictoryLineSlot.TextSp, Color.clear, TextAlignmentOptions.TopLeft);
            _measureBody.textWrappingMode = TextWrappingModes.Normal;
            measure.gameObject.SetActive(false);
        }

        /// <summary>
        /// Lo que hace un toque en la línea a la vista (F4: abrir la ficha de su fruta; lo da <c>GameBootstrap</c>). Sin él, la línea
        /// no recibe toques ni entra en <c>AF_LAYOUT victory</c>.
        /// </summary>
        public Action? Tapped { get; set; }

        /// <summary>
        /// La línea recibe toques ahora: se ve (con «Siguiente», tras <see cref="VictoryLineSlot.FruitRevealMs"/> ms) y abre la ficha
        /// (<see cref="Opens"/>).
        /// </summary>
        public bool Touchable => _group != null && _group.blocksRaycasts;

        /// <summary>
        /// Un toque en la línea de ahora abriría la ficha (revisión de F4): alguien escucha (<see cref="Tapped"/>) y la ficha se puede
        /// enseñar en el idioma de la UI (<see cref="UiText.FruitSheetVisible"/>; su título y su contador tienen texto y, en release,
        /// están aprobados, con estados independientes de los de la línea). Si no, la línea se ve pero no recibe toques, no suena ni
        /// entra en <c>AF_LAYOUT victory</c>. Se decide al enseñarla (el idioma no cambia durante la victoria).
        /// </summary>
        public bool Opens => _opens && Tapped != null;

        /// <summary>Área táctil de la línea (≥ <see cref="VictoryLineSlot.MinContentDp"/> dp de alto; null hasta la primera colocación).</summary>
        public RectTransform? TapArea => _tap != null ? (RectTransform)_tap.transform : null;

        /// <summary>La línea existe en esta partida (false: tests de la ranura sola). Cuenta desde la siguiente maquetación.</summary>
        public bool Enabled { get; set; } = true;

        /// <inheritdoc />
        public bool SlotActive => Enabled && _schedule() != null;

        /// <summary>Se enseña en la victoria de ahora.</summary>
        public bool Shown { get; private set; }

        /// <summary>Nivel de la victoria que se enseña (0 sin línea).</summary>
        public int Level { get; private set; }

        /// <summary>Serie y línea k que se enseñan (sin serie si no hay línea).</summary>
        public FactPosition Position { get; private set; }

        /// <summary>Clave de texto de la línea (<c>fact.&lt;tinte&gt;.&lt;kk&gt;</c>), o null sin línea.</summary>
        public string? Key { get; private set; }

        /// <summary>
        /// <c>fact_id</c> de la línea que se ve (<c>&lt;tinte&gt;.&lt;kk&gt;</c>, 08 §4.6 y §8.2), o null sin línea: el de
        /// <c>river_fact_shown</c> en el punto común de la ranura.
        /// </summary>
        public string? ShownFactId { get; private set; }

        /// <summary>«Dato del río» (vacío sin línea).</summary>
        public string HeaderText { get; private set; } = string.Empty;

        /// <summary>«Açaí · 3/20», o solo el nombre del tinte con el sello (vacío sin línea).</summary>
        public string ProgressText { get; private set; } = string.Empty;

        /// <summary>El texto de la línea (vacío sin línea).</summary>
        public string BodyText { get; private set; } = string.Empty;

        /// <summary>Última línea de la serie: el contador pasa a un sello (Q5-A).</summary>
        public bool Sealed { get; private set; }

        /// <summary>El contador va en una segunda fila de la cabecera (no cabía junto a «Dato del río»).</summary>
        public bool HeaderWrapped { get; private set; }

        /// <summary>Alto de la cabecera que se enseña (dp; <see cref="IconDp"/> o más si el contador baja de fila).</summary>
        public float HeaderHeightDp { get; private set; }

        /// <summary>Alto que ocupa lo que se enseña ahora: cabecera, texto y margen de abajo (dp; 0 sin línea).</summary>
        public float ShownHeightDp { get; private set; }

        /// <summary>Alto fijo H de la línea de fruta en la última colocación (dp): hasta ahí crece el texto.</summary>
        public float BlockHeightDp { get; private set; }

        /// <summary>
        /// Desplazamiento vertical de la línea dentro de la ranura (dp): como mucho <see cref="TopAirDp"/>, o bajo la placa del lugar
        /// si van juntas.
        /// </summary>
        public float OffsetDp { get; private set; }

        /// <summary>Tamaño del texto que se enseña (dp, con la escala de texto): <see cref="VictoryLineSlot.TextSp"/> o menos si no cabía.</summary>
        public float TextSizeDp { get; private set; }

        /// <summary>Con Reducir movimiento en la victoria de ahora.</summary>
        public bool ReduceMotion => _reduceMotion;

        /// <summary>Opacidad de toda la línea (fundido de entrada).</summary>
        public float Alpha => _group != null ? _group.alpha : 0f;

        /// <summary>Toda la línea (null hasta la primera colocación).</summary>
        public RectTransform? Root => _root;

        /// <summary>El icono de la fruta.</summary>
        public Image? Icon => _icon;

        /// <summary>El disco bajo el icono de la fruta (<see cref="IconBackDp"/> dp, centrado en el icono).</summary>
        public Image? IconBackDisc => _iconBack;

        /// <summary>«Dato del río».</summary>
        public TextMeshProUGUI? HeaderLabel => _headerLabel;

        /// <summary>El punto medio entre «Dato del río» y el contador (solo con la cabecera en una fila).</summary>
        public Image? HeaderDot => _headerDot;

        /// <summary>El contador o el nombre con el sello.</summary>
        public TextMeshProUGUI? ProgressLabel => _progressLabel;

        /// <summary>El sello de la última línea (su aro).</summary>
        public Image? Seal => _sealRing;

        /// <summary>El texto de la línea.</summary>
        public TextMeshProUGUI? TextLabel => _textLabel;

        /// <summary>
        /// Milisegundos que costó la última medida de <see cref="NeedDp"/> que no salió de la caché (tests, AF_FACT y
        /// <c>AF_PERF slot_measure</c>).
        /// </summary>
        public double NeedMs { get; private set; }

        /// <summary>Medidas de <see cref="NeedDp"/> que no salieron de la caché.</summary>
        public int NeedMeasureCount { get; private set; }

        /// <summary>
        /// Índice de motor del tinte <paramref name="subject"/> (glosario §9; <see cref="ProceduralArt.TintIds"/>), o 0 si no es un
        /// tinte. Sin asignaciones.
        /// </summary>
        public static int TintOf(string subject)
        {
            string[] ids = ProceduralArt.TintIds;
            for (int i = 1; i < ids.Length; i++)
            {
                if (string.Equals(ids[i], subject, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// Alto de la línea de fruta más alta del calendario (Q7-A: «el alto de la línea más larga de ese idioma a esa escala»)
        /// en el idioma de <see cref="Loc"/>, a la escala <paramref name="textScale"/> y en <paramref name="widthDp"/> dp: su cabecera
        /// (en una fila o en dos) y su texto a <see cref="VictoryLineSlot.TextSp"/> sp, más el margen de abajo; 0 sin calendario o
        /// sin ninguna línea con texto en el idioma. La ranura reserva lo mayor de esto, del peor caso sintético de
        /// <see cref="VictoryLineSlot"/> y de la llegada más alta, así que ninguna línea real necesita bajar de 14 sp. Mide con
        /// TextMeshPro una vez por combinación (caché): GameScreen la calienta al cargar el nivel, fuera de la victoria (revisión de
        /// F3: unos 16-23 ms en el escritorio), y nunca por fotograma.
        /// </summary>
        public float NeedDp(float textScale, float widthDp)
        {
            FactSchedule? schedule = _schedule();
            if (schedule == null)
            {
                return 0f;
            }

            string locale = Loc.IsConfigured ? Loc.Locale : LocLocale.SourceLocale;
            LocCatalog catalog = Loc.Catalog;
            bool unapproved = Loc.ShowUnapproved;
            for (int i = 0; i < _needs.Count; i++)
            {
                NeedEntry e = _needs[i];
                if (ReferenceEquals(e.Schedule, schedule) && ReferenceEquals(e.Catalog, catalog) && e.Unapproved == unapproved && e.Locale == locale
                    && Mathf.Approximately(e.Scale, textScale) && Mathf.Approximately(e.Width, widthDp))
                {
                    return e.Height;
                }
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            float headerSize = HeaderSp * textScale;
            float avail = widthDp - IconDp - IconGapDp;
            float textSize = VictoryLineSlot.TextSp * textScale;
            float textLine = LineHeight(headerSize);
            float need = 0f;
            IReadOnlyList<FactSeries> series = schedule.Catalog.Series;
            for (int i = 0; i < series.Count; i++)
            {
                FactSeries s = series[i];
                for (int k = 1; k <= s.Lines; k++)
                {
                    var p = new FactPosition(s, k);
                    if (!UiText.TryFruitLine(in p, out string header, out string progress, out string text, out bool @sealed))
                    {
                        continue;
                    }

                    float headerH = HeaderHeightFor(header, progress, @sealed, headerSize, textLine, avail, out _, out _, out _, out _);
                    need = Mathf.Max(need, Mathf.Ceil(headerH + Height(text, textSize, widthDp) + VictoryLineSlot.BottomDp));
                }
            }

            NeedMs = watch.Elapsed.TotalMilliseconds;
            NeedMeasureCount++;
            _needs.Add(new NeedEntry(schedule, catalog, unapproved, locale, textScale, widthDp, need));
            return need;
        }

        /// <inheritdoc />
        public void Layout(RectTransform content, float widthDp, float heightDp)
        {
            _width = widthDp;
            _height = heightDp;
            Build(content);
            UiBuilder.Place(_root!, 0f, 0f, widthDp, heightDp);
            if (Shown)
            {
                Arrange();
            }
        }

        /// <inheritdoc />
        public bool Show(LevelFlow flow, in VictoryLine line)
        {
            return ShowFor(flow.LevelNumber, flow.ReduceMotion, in line);
        }

        /// <summary>
        /// Enseña la línea de la victoria del nivel <paramref name="level"/> con la línea <paramref name="line"/> del árbitro, o la
        /// oculta y devuelve false si no hay línea de fruta, si la clave no es del calendario o si le falta algún texto en el
        /// idioma de la UI. Empieza invisible: <see cref="Tick"/> la hace entrar con «Siguiente».
        /// </summary>
        public bool ShowFor(int level, bool reduceMotion, in VictoryLine line)
        {
            ResetState();
            Level = level;
            _reduceMotion = reduceMotion;

            // Lo decide el árbitro: con la placa del lugar sin dato (SingleKey, o una llegada sin dato), la línea va bajo la placa.
            if (_root == null || !TryResolve(in line, out FactPosition position, out string header, out string progress, out string text, out bool @sealed))
            {
                Hide();
                return false;
            }

            Position = position;
            Key = line.FruitKey;
            ShownFactId = position.FactId;
            HeaderText = header;
            ProgressText = progress;
            BodyText = text;
            Sealed = @sealed;
            Shown = true;
            _opens = UiText.FruitSheetVisible;
            _root.gameObject.SetActive(true);
            _group!.alpha = 0f;
            SetTouchable(false); // hasta que entre, con «Siguiente» (Tick)
            Arrange();
            return true;
        }

        /// <summary>
        /// La línea <paramref name="line"/> del árbitro se puede enseñar en esta victoria (lo mismo que decide <see cref="ShowFor"/>,
        /// sin enseñar nada): hay línea de fruta, la clave es del calendario y tiene todos sus textos en el idioma de la UI (en release,
        /// aprobados). GameScreen.Victory.cs lo pregunta antes de la victoria para que la placa del lugar solo vaya arriba
        /// (<see cref="ArrivalStrip.Stacked"/>) si la línea va de verdad debajo; si no, la placa va centrada.
        /// </summary>
        public bool CanShow(in VictoryLine line)
        {
            return SlotActive && _root != null && TryResolve(in line, out _, out _, out _, out _, out _);
        }

        // La posición y los textos de la línea que da el árbitro, o false si no hay línea de fruta, si la clave no es del calendario
        // o si le falta algún texto en el idioma de la UI (UiText.TryFruitLine).
        private bool TryResolve(in VictoryLine line, out FactPosition position, out string header, out string progress, out string text, out bool @sealed)
        {
            position = default;
            header = string.Empty;
            progress = string.Empty;
            text = string.Empty;
            @sealed = false;
            FactSchedule? schedule = _schedule();
            string? key = line.FruitKey;
            if (key == null || schedule == null)
            {
                return false;
            }

            int lineLevel = schedule.LevelOf(key);
            position = lineLevel > 0 ? schedule.Progress(lineLevel) : default;
            return position.IsValid && string.Equals(position.Key, key, StringComparison.Ordinal)
                && UiText.TryFruitLine(in position, out header, out progress, out text, out @sealed);
        }

        /// <inheritdoc />
        public void Tick(float dtMs, float sinceShownMs)
        {
            if (!Shown || _group == null)
            {
                return;
            }

            float a = VictoryLineSlot.FruitLineAlpha(sinceShownMs, _reduceMotion);
            if (!Mathf.Approximately(_group.alpha, a))
            {
                _group.alpha = a;
            }

            SetTouchable(a > 0f && Opens);
        }

        /// <inheritdoc />
        public void Hide()
        {
            Shown = false;
            SetTouchable(false);
            if (_root != null && _root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }
        }

        /// <inheritdoc />
        public void LayoutIds(List<string> ids, List<RectTransform> rects)
        {
            // La victoria publica su AF_LAYOUT al terminar de subir el panel, antes de que entre la línea: el área táctil se anuncia
            // en cuanto la línea se enseña y abre la ficha (Opens) (recibe toques desde que entra, con «Siguiente»).
            if (Shown && Opens && _tap != null)
            {
                ids.Add(TapId);
                rects.Add((RectTransform)_tap.transform);
            }
        }

        // Un toque en el área de la línea: solo si se ve (el CanvasGroup no deja pasar toques antes) y alguien escucha.
        private void OnTap()
        {
            if (Shown && Touchable)
            {
                Tapped?.Invoke();
            }
        }

        private void SetTouchable(bool on)
        {
            if (_group != null && _group.blocksRaycasts != on)
            {
                _group.blocksRaycasts = on;
                _group.interactable = on;
            }
        }

        private void ResetState()
        {
            Shown = false;
            Level = 0;
            Position = default;
            Key = null;
            ShownFactId = null;
            _opens = false;
            HeaderText = string.Empty;
            ProgressText = string.Empty;
            BodyText = string.Empty;
            Sealed = false;
            HeaderWrapped = false;
            HeaderHeightDp = 0f;
            ShownHeightDp = 0f;
            OffsetDp = 0f;
            TextSizeDp = 0f;
        }

        // Crea la línea bajo el contenido de la ranura (una vez): la raíz con su CanvasGroup, el área táctil (F4), el icono, la
        // cabecera, el sello y el texto. Solo el área táctil recibe toques; el CanvasGroup no los deja pasar mientras no se ve.
        private void Build(RectTransform content)
        {
            if (_root != null)
            {
                if (_root.parent != content)
                {
                    _root.SetParent(content, false);
                }

                return;
            }

            RectTransform root = UiBuilder.Rect("FactLine", content);
            _root = root;
            _group = root.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _tap = UiBuilder.BareButton("Tap", root, OnTap); // detrás de todo: los gráficos no reciben toques
            _iconBack = UiBuilder.Image("FruitIconBack", root, _art.Disc, IconBack);
            _iconBack.raycastTarget = false;
            _icon = UiBuilder.Image("FruitIcon", root, null, Color.white);
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            _headerLabel = Label("Header", root, TextStyle.PlainBold, HeaderSp, HeaderInk, TextAlignmentOptions.MidlineLeft);
            _headerDot = UiBuilder.Image("HeaderDot", root, _art.Disc, HeaderInk);
            _headerDot.raycastTarget = false;
            _progressLabel = Label("Progress", root, TextStyle.PlainBold, HeaderSp, HeaderInk, TextAlignmentOptions.MidlineLeft);
            _sealRing = UiBuilder.Image("Seal", root, _art.Disc, SealRing);
            _sealFace = UiBuilder.Image("SealFace", root, _art.Disc, Palette.PanelLight);
            _sealIcon = UiBuilder.Image("SealIcon", root, null, Color.white);
            _sealIcon.preserveAspect = true;
            _textLabel = Label("Text", root, TextStyle.Body, VictoryLineSlot.TextSp, TextInk, TextAlignmentOptions.TopLeft);
            _textLabel.textWrappingMode = TextWrappingModes.Normal;
            root.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI Label(string name, RectTransform parent, TextStyle style, float sp, Color color, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI label = UiBuilder.Label(name, parent, style, string.Empty, sp, color, alignment);
            label.raycastTarget = false;
            return label;
        }

        // Coloca la línea para lo que se enseña ahora (al enseñarla y al recolocar la victoria; nunca por fotograma).
        private void Arrange()
        {
            float w = _width;
            float scale = UiBuilder.TextScale;
            float headerSize = HeaderSp * scale;
            float block = Mathf.Max(VictoryLineSlot.MinContentDp, _blockDp());
            BlockHeightDp = block;

            // H es el alto de la línea de fruta; la ranura puede ser más alta (la llegada más alta del catálogo, M1.6): la línea va
            // arriba, a TopAirDp como mucho bajo el borde (cerca del filete que la anuncia), y el sobrante queda debajo. Con la placa
            // del lugar encima (Stacked), justo debajo de ella.
            float above = _aboveDp();
            float off = above > 0f
                ? Mathf.Ceil(above + StackGapDp)
                : Mathf.Min(TopAirDp, Mathf.Max(0f, Mathf.Floor((_height - block) * 0.5f)));
            OffsetDp = off;

            int tint = TintOf(Position.Series!.Subject);
            Sprite? fruit = tint > 0 ? _art.FruitFlight(tint) : null;
            _icon!.sprite = fruit;
            SetActive(_icon, fruit != null);
            SetActive(_iconBack, fruit != null);
            UiBuilder.Place(_iconBack!.rectTransform, (IconDp - IconBackDp) * 0.5f, off + ((IconDp - IconBackDp) * 0.5f), IconBackDp, IconBackDp);
            UiBuilder.Place(_icon.rectTransform, 0f, off, IconDp, IconDp);

            // La cabecera: «Dato del río», el punto medio y el contador seguidos en una fila si caben; si no, el contador en una
            // segunda fila, sin el punto.
            float x = IconDp + IconGapDp;
            float avail = w - x;
            HeaderHeightDp = HeaderHeightFor(HeaderText, ProgressText, Sealed, headerSize, LineHeight(headerSize), avail, out bool oneRow,
                out float headerW, out float progressW, out float lineH);
            HeaderWrapped = !oneRow;
            float progressX;
            float progressTop;
            float sealW = Sealed ? SealGapDp + SealDp : 0f;
            SetActive(_headerDot, oneRow);
            if (oneRow)
            {
                float headerBox = headerW + 1f;
                SetLabel(_headerLabel!, HeaderText, headerSize, x, off, headerBox, lineH);
                float dotX = x + headerBox + HeaderGapDp;
                UiBuilder.Place(_headerDot!.rectTransform, dotX, off + ((lineH - SeparatorDotDp) * 0.5f), SeparatorDotDp, SeparatorDotDp);
                progressX = dotX + SeparatorDotDp + HeaderGapDp;
                progressTop = off;
            }
            else
            {
                SetLabel(_headerLabel!, HeaderText, headerSize, x, off, avail, lineH);
                progressX = x;
                progressTop = off + lineH;
            }

            float progressBox = Mathf.Min(w - progressX - sealW, progressW + 1f);
            SetLabel(_progressLabel!, ProgressText, headerSize, progressX, progressTop, progressBox, lineH);
            PlaceSeal(fruit, progressX + progressBox + SealGapDp, progressTop + ((lineH - SealDp) * 0.5f));

            // El texto, a todo el ancho bajo la cabecera: 14 sp, o menos si una línea real no cabe en H (nunca menos de 12 sp).
            float top = off + HeaderHeightDp;
            float room = block - HeaderHeightDp - VictoryLineSlot.BottomDp;
            float size = VictoryLineSlot.TextSp * scale;
            float min = UiBuilder.MinTextSp * scale;
            float h = Height(BodyText, size, w);
            while (h > room + 0.5f && size > min)
            {
                size = Mathf.Max(min, size - (ShrinkStepSp * scale));
                h = Height(BodyText, size, w);
            }

            TextSizeDp = size;
            SetLabel(_textLabel!, BodyText, size, 0f, top, w, Mathf.Max(h, 1f));
            ShownHeightDp = HeaderHeightDp + h + VictoryLineSlot.BottomDp;

            // Toda la línea es un botón de 48 dp como mínimo (02 §6; propuesta de Frutas §2.2, regla 4), dentro de la ranura.
            float tapH = Mathf.Max(VictoryLineSlot.MinContentDp, ShownHeightDp);
            UiBuilder.Place((RectTransform)_tap!.transform, 0f, Mathf.Max(0f, Mathf.Min(off, _height - tapH)), w, tapH);
        }

        // El sello de la última línea (Q5-A): un aro verde, una cara clara y el icono de la fruta, sin animación.
        private void PlaceSeal(Sprite? fruit, float x, float y)
        {
            bool on = Sealed;
            SetActive(_sealRing, on);
            SetActive(_sealFace, on);
            SetActive(_sealIcon, on && fruit != null);
            if (!on)
            {
                return;
            }

            UiBuilder.Place(_sealRing!.rectTransform, x, y, SealDp, SealDp);
            float face = SealDp - (2f * SealRingDp);
            UiBuilder.Place(_sealFace!.rectTransform, x + SealRingDp, y + SealRingDp, face, face);
            _sealIcon!.sprite = fruit;
            float icon = face - 2f;
            UiBuilder.Place(_sealIcon.rectTransform, x + SealRingDp + 1f, y + SealRingDp + 1f, icon, icon);
        }

        // Alto de la cabecera con estos textos: una fila de IconDp si «Dato del río», el punto medio, el contador (y el sello)
        // caben seguidos en avail dp; si no, dos filas de texto (el contador debajo), con IconDp como mínimo.
        private float HeaderHeightFor(string header, string progress, bool sealedLine, float headerSize, float textLine, float avail, out bool oneRow,
            out float headerW, out float progressW, out float lineH)
        {
            headerW = Width(header, headerSize);
            progressW = Width(progress, headerSize);
            float sealW = sealedLine ? SealGapDp + SealDp : 0f;
            oneRow = headerW + 1f + (2f * HeaderGapDp) + SeparatorDotDp + progressW + 1f + sealW <= avail;
            if (oneRow)
            {
                lineH = Mathf.Max(IconDp, textLine);
                return lineH;
            }

            lineH = Mathf.Max(IconDp * 0.5f, textLine);
            return Mathf.Max(IconDp, 2f * lineH);
        }

        private float Width(string text, float sizeDp)
        {
            if (text.Length == 0)
            {
                return 0f;
            }

            _measureBold.fontSize = sizeDp;
            float wpx = _measureBold.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x;
            _measureBold.text = string.Empty;
            return Mathf.Ceil(wpx);
        }

        private float LineHeight(float sizeDp)
        {
            _measureBold.fontSize = sizeDp;
            float h = _measureBold.GetPreferredValues("Ág", float.PositiveInfinity, float.PositiveInfinity).y;
            _measureBold.text = string.Empty;
            return Mathf.Ceil(h);
        }

        private float Height(string text, float sizeDp, float widthDp)
        {
            if (text.Length == 0)
            {
                return 0f;
            }

            _measureBody.fontSize = sizeDp;
            float h = _measureBody.GetPreferredValues(text, widthDp, float.PositiveInfinity).y;
            _measureBody.text = string.Empty;
            return h;
        }

        private static void SetLabel(TextMeshProUGUI label, string text, float sizeDp, float x, float y, float w, float h)
        {
            label.fontSize = sizeDp;
            label.text = text;
            UiBuilder.Place(label.rectTransform, x, y, Mathf.Max(w, 1f), Mathf.Max(h, 1f));
        }

        private static void SetActive(Graphic? g, bool on)
        {
            if (g != null && g.gameObject.activeSelf != on)
            {
                g.gameObject.SetActive(on);
            }
        }

        private readonly struct NeedEntry
        {
            public NeedEntry(FactSchedule schedule, LocCatalog catalog, bool unapproved, string locale, float scale, float width, float height)
            {
                Schedule = schedule;
                Catalog = catalog;
                Unapproved = unapproved;
                Locale = locale;
                Scale = scale;
                Width = width;
                Height = height;
            }

            public FactSchedule Schedule { get; }

            public LocCatalog Catalog { get; }

            public bool Unapproved { get; }

            public string Locale { get; }

            public float Scale { get; }

            public float Width { get; }

            public float Height { get; }
        }
    }
}
