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
    /// no llevan línea de fruta; la cadencia es <c>GameConfig.FruitLineEveryN</c>).
    /// <list type="bullet">
    /// <item><b>Cabecera</b> de <see cref="VictoryLineSlot.HeaderDp"/> dp: el icono de la fruta a <see cref="IconDp"/> dp (la
    /// fruta en vuelo del tinte, <see cref="ProceduralArt.FruitFlight"/>, con su PNG <c>fruta_&lt;tinte&gt;</c> si existe), «Dato del
    /// río» (<c>river_fact</c>) a la izquierda y «Açaí · 3/20» (<c>fact_progress</c>) a la derecha, en Nunito Bold de
    /// <see cref="HeaderSp"/> sp. Son dos etiquetas: si no caben en una fila (al 130 % o con la pseudolocalización), el contador
    /// baja a una segunda fila y la cabecera crece. En la última línea de la serie el contador pasa a un sello pequeño con el icono
    /// de la fruta, junto al nombre, sin animación (Q5-A; propuesta §2.6).</item>
    /// <item><b>Texto</b> (<c>Loc.Fact</c>) a todo el ancho, en Nunito Regular de <see cref="VictoryLineSlot.TextSp"/> sp, que crece
    /// hacia abajo hasta el alto fijo H de la línea de fruta (el de la ranura, <see cref="VictoryLineSlot.ContentHeightFor"/>); si
    /// una línea real no cabe, baja de medio en medio punto hasta <see cref="UiBuilder.MinTextSp"/> sp, nunca menos, y nunca se
    /// recorta.</item>
    /// <item><b>Tiempos:</b> entra con «Siguiente», a los <see cref="VictoryLineSlot.FruitRevealMs"/> ms de la pantalla de
    /// victoria (la fila 1,40 s de 07 §5.4), con un fundido de <see cref="VictoryLineSlot.FruitFadeMs"/> ms; con Reducir
    /// movimiento, sin fundido (<see cref="VictoryLineSlot.FruitLineAlpha"/>). Sin sonido ni confeti; no bloquea, no pide toques y
    /// no es un takeover.</item>
    /// <item><b>Toques:</b> ninguno hasta F4 (la ficha): todos sus gráficos van con <c>raycastTarget</c> false y su CanvasGroup no
    /// bloquea, así que no entra en <c>AF_LAYOUT victory</c>.</item>
    /// <item><b>Textos:</b> solo si están todos en el idioma de la UI (<see cref="UiText.TryFruitLine"/>): nunca mezcla idiomas y,
    /// en release, nunca enseña nada sin aprobar.</item>
    /// <item>Con la política <see cref="RiverFactPolicy.SingleKey"/> (no es la de hoy), en el <c>10k</c> la placa del lugar y la
    /// línea de fruta no caben juntas en la ranura: la línea cede el sitio a la placa y queda en la ficha [P].</item>
    /// </list>
    /// Nada de lo que hace por fotograma asigna memoria: los textos y las medidas se hacen al enseñarla y al recolocar la victoria.
    /// </summary>
    internal sealed class FactLine : IVictoryLineProvider
    {
        /// <summary>Icono de la fruta de la cabecera (02 §6; propuesta §2.2, regla 5).</summary>
        public const float IconDp = VictoryLineSlot.HeaderDp;

        /// <summary>Del icono a la cabecera [P].</summary>
        public const float IconGapDp = 8f;

        /// <summary>Cabecera: Nunito Bold de 12 sp (02 §6).</summary>
        public const float HeaderSp = UiBuilder.MinTextSp;

        /// <summary>Hueco mínimo entre «Dato del río» y el contador en la misma fila [P].</summary>
        public const float HeaderGapDp = 12f;

        /// <summary>Sello de la última línea de la serie (Q5-A): un disco de 20 dp [P].</summary>
        public const float SealDp = 20f;

        /// <summary>Aro del sello [P].</summary>
        public const float SealRingDp = 2f;

        /// <summary>Del nombre al sello [P].</summary>
        public const float SealGapDp = 4f;

        /// <summary>Paso con el que baja el texto si una línea real no cabe en H [P].</summary>
        public const float ShrinkStepSp = 0.5f;

        /// <summary>Tinta de la cabecera: madera oscura sobre el panel (≥ 4,5:1; propuesta §2.8).</summary>
        public static readonly Color HeaderInk = Palette.FamilyWood.Deep;

        /// <summary>Tinta del texto (07 §7.2).</summary>
        public static readonly Color TextInk = Palette.UiInk;

        /// <summary>Aro del sello: el verde hoja de la cinta «¡Nivel completado!» [P].</summary>
        public static readonly Color SealRing = Palette.FamilyLeaf.Deep;

        private readonly ProceduralArt _art;
        private readonly Func<FactSchedule?> _schedule;
        private readonly Func<float> _blockDp;
        private readonly TextMeshProUGUI _measureBold;
        private readonly TextMeshProUGUI _measureBody;
        private readonly List<NeedEntry> _needs = new List<NeedEntry>(4);

        private RectTransform? _root;
        private CanvasGroup? _group;
        private Image? _icon;
        private TextMeshProUGUI? _headerLabel;
        private TextMeshProUGUI? _progressLabel;
        private Image? _sealRing;
        private Image? _sealFace;
        private Image? _sealIcon;
        private TextMeshProUGUI? _textLabel;
        private float _width;
        private float _height;
        private bool _reduceMotion;

        /// <summary>
        /// Crea la línea. <paramref name="measureParent"/> aloja las etiquetas de medir (ocultas); <paramref name="schedule"/> da el
        /// calendario de Frutas del río de ahora; <paramref name="blockDp"/>, el alto fijo H de la línea de fruta en el idioma, la
        /// escala y el ancho de ahora (de la caché de la ranura).
        /// </summary>
        public FactLine(ProceduralArt art, RectTransform measureParent, Func<FactSchedule?> schedule, Func<float> blockDp)
        {
            _art = art;
            _schedule = schedule;
            _blockDp = blockDp;
            RectTransform measure = UiBuilder.Rect("FactLineMeasure", measureParent);
            _measureBold = UiBuilder.Label("MeasureBold", measure, TextStyle.PlainBold, string.Empty, HeaderSp, Color.clear, TextAlignmentOptions.TopLeft);
            _measureBody = UiBuilder.Label("MeasureBody", measure, TextStyle.Body, string.Empty, VictoryLineSlot.TextSp, Color.clear, TextAlignmentOptions.TopLeft);
            _measureBody.textWrappingMode = TextWrappingModes.Normal;
            measure.gameObject.SetActive(false);
        }

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

        /// <summary>Desplazamiento vertical de la línea dentro de la ranura (dp): el aire que deja H, repartido arriba y abajo.</summary>
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

        /// <summary>«Dato del río».</summary>
        public TextMeshProUGUI? HeaderLabel => _headerLabel;

        /// <summary>El contador o el nombre con el sello.</summary>
        public TextMeshProUGUI? ProgressLabel => _progressLabel;

        /// <summary>El sello de la última línea (su aro).</summary>
        public Image? Seal => _sealRing;

        /// <summary>El texto de la línea.</summary>
        public TextMeshProUGUI? TextLabel => _textLabel;

        /// <summary>Milisegundos que costó la última medida de <see cref="NeedDp"/> que no salió de la caché (tests y AF_FACT).</summary>
        public double NeedMs { get; private set; }

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
        /// TextMeshPro una vez por combinación (caché), al maquetar la primera victoria: nunca por fotograma.
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
            FactSchedule? schedule = _schedule();
            string? key = line.FruitKey;

            // Con SingleKey, el 10k lleva la placa del lugar en la misma ranura: la línea le cede el sitio y queda en la ficha [P].
            if (key == null || line.Arrival || schedule == null || _root == null)
            {
                Hide();
                return false;
            }

            int lineLevel = schedule.LevelOf(key);
            FactPosition position = lineLevel > 0 ? schedule.Progress(lineLevel) : default;
            if (!position.IsValid || !string.Equals(position.Key, key, StringComparison.Ordinal)
                || !UiText.TryFruitLine(in position, out string header, out string progress, out string text, out bool @sealed))
            {
                Hide();
                return false;
            }

            Position = position;
            Key = key;
            ShownFactId = position.FactId;
            HeaderText = header;
            ProgressText = progress;
            BodyText = text;
            Sealed = @sealed;
            Shown = true;
            _root.gameObject.SetActive(true);
            _group!.alpha = 0f;
            Arrange();
            return true;
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
        }

        /// <inheritdoc />
        public void Hide()
        {
            Shown = false;
            if (_root != null && _root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }
        }

        /// <inheritdoc />
        public void LayoutIds(List<string> ids, List<RectTransform> rects)
        {
            // Sin toques hasta F4 (la ficha): no hay área táctil que anunciar.
        }

        private void ResetState()
        {
            Shown = false;
            Level = 0;
            Position = default;
            Key = null;
            ShownFactId = null;
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

        // Crea la línea bajo el contenido de la ranura (una vez): la raíz con su CanvasGroup, el icono, la cabecera, el sello y el
        // texto. Ningún gráfico recibe toques (raycastTarget false hasta F4).
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
            _icon = UiBuilder.Image("FruitIcon", root, null, Color.white);
            _icon.preserveAspect = true;
            _headerLabel = Label("Header", root, TextStyle.PlainBold, HeaderSp, HeaderInk, TextAlignmentOptions.MidlineLeft);
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
            // centrada en ella, siempre en el mismo sitio, con el aire repartido arriba y abajo.
            float off = Mathf.Max(0f, Mathf.Floor((_height - block) * 0.5f));
            OffsetDp = off;

            int tint = TintOf(Position.Series!.Subject);
            Sprite? fruit = tint > 0 ? _art.FruitFlight(tint) : null;
            _icon!.sprite = fruit;
            SetActive(_icon, fruit != null);
            UiBuilder.Place(_icon.rectTransform, 0f, off, IconDp, IconDp);

            // La cabecera: «Dato del río» y el contador en una fila si caben; si no, el contador en una segunda fila.
            float x = IconDp + IconGapDp;
            float avail = w - x;
            float sealW = Sealed ? SealGapDp + SealDp : 0f;
            HeaderHeightDp = HeaderHeightFor(HeaderText, ProgressText, Sealed, headerSize, LineHeight(headerSize), avail, out bool oneRow,
                out float headerW, out float progressW, out float lineH);
            HeaderWrapped = !oneRow;
            float progressX;
            float progressTop;
            if (oneRow)
            {
                SetLabel(_headerLabel!, HeaderText, headerSize, x, off, Mathf.Min(avail, headerW + 1f), lineH);
                progressX = w - sealW - progressW - 1f;
                progressTop = off;
            }
            else
            {
                SetLabel(_headerLabel!, HeaderText, headerSize, x, off, avail, lineH);
                progressX = x;
                progressTop = off + lineH;
            }

            SetLabel(_progressLabel!, ProgressText, headerSize, progressX, progressTop, Mathf.Min(avail, progressW + 1f), lineH);
            PlaceSeal(fruit, progressX + Mathf.Min(avail - sealW, progressW + 1f) + SealGapDp, progressTop + ((lineH - SealDp) * 0.5f));

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

        // Alto de la cabecera con estos textos: una fila de IconDp si «Dato del río», el contador (y el sello) caben juntos en
        // avail dp; si no, dos filas de texto (el contador debajo), con IconDp como mínimo.
        private float HeaderHeightFor(string header, string progress, bool sealedLine, float headerSize, float textLine, float avail, out bool oneRow,
            out float headerW, out float progressW, out float lineH)
        {
            headerW = Width(header, headerSize);
            progressW = Width(progress, headerSize);
            float sealW = sealedLine ? SealGapDp + SealDp : 0f;
            oneRow = headerW + HeaderGapDp + progressW + sealW <= avail;
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
