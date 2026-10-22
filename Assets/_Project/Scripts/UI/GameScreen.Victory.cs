#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Parcial de la victoria del Mapa del río y de Frutas del río (arquitectura de la tanda 4, §2; 02 §6, filas «Dato del río»,
    /// Llegada y TiTino; Q7-A de D-017; D-013 punto 8; T6-A; tarea M1.3b): la ranura del Dato del río bajo «Inicio»
    /// (<see cref="VictoryLineSlot"/>), que pide la única línea de la victoria al árbitro (<see cref="VictoryLineArbiter"/>) y
    /// la reparte a sus proveedores, y la caja de TiTino, de la que sale <c>SlothCanSpeak</c>.
    /// <list type="bullet">
    /// <item>La ranura solo existe con algún proveedor activo (<see cref="RegisterVictoryLineProvider"/>: la llegada en M1.6
    /// y la línea de fruta en F3); entonces reserva bajo «Inicio» el alto fijo H(idioma, escala, ancho), guardado en caché, y
    /// «Siguiente» queda en el mismo sitio en todas las victorias. Sin proveedores, la victoria es la de siempre.</item>
    /// <item>«Siguiente» no cambia de tiempo: se habilita en el mismo milisegundo con y sin ranura y entra a los
    /// <see cref="VictoryButtonsInMs"/> ms de la pantalla de victoria (la fila 1,40 de 07 §5.4; VA-13). La ranura no decide
    /// cuándo aparece lo de sus proveedores: la línea de fruta (F3) entra con «Siguiente»
    /// (<see cref="VictoryLineSlot.FruitLineAlpha"/>) y la llegada (M1.6) lleva su franja desde la subida del panel.</item>
    /// <item>La línea de fruta (<see cref="FactLine"/>; tarea F3) es un proveedor más: existe en cuanto llega el calendario de
    /// Frutas del río (<see cref="FruitFacts"/>), da la clave de cada nivel al árbitro (<see cref="FruitKeyOf"/>) y enseña la que
    /// este decida. Su <c>fact_id</c> sale por el punto común de <c>river_fact_shown</c>, como el dato de llegada. Si el árbitro
    /// da a la vez la placa del lugar sin dato y la línea de fruta (SingleKey, o una llegada sin dato; arquitectura de la tanda 4,
    /// §2), van las dos, la placa arriba y la línea debajo, y la ranura reserva ese alto (<see cref="StackNeedDp"/>).</item>
    /// <item>Las medidas de la ranura (la línea de fruta más alta del calendario, la llegada más alta y la placa con la línea) se
    /// calientan al cargar el nivel (<see cref="WarmVictorySlot"/>), fuera de la victoria; en desarrollo, con
    /// <c>AF_PERF slot_measure</c>.</item>
    /// <item>TiTino (tareas TI6a y DC2; <see cref="SlothVictory"/>): con la victoria con vista, su caja es la franja del pie de
    /// la cabecera a la cima de la cinta sin el héroe del sitio (pregunta 53 a de D-028; <see cref="SlothViewBand"/>) y entra
    /// con la vista; sin ella, la libre entre la cabecera y la cima de las cuias. Sale en su caja si cabe y hace su gesto; en
    /// sus hitos puede hablar
    /// (<c>SlothCanSpeak</c>, que se calcula antes de que el árbitro decida) si <see cref="SlothVoice"/> (<c>ui_sloth_enabled</c>
    /// ∧ <c>settings.sloth_messages</c>), su caja mide ≥ <see cref="SlothSpeakBoxDp"/> dp, está en pantalla, los topes de la
    /// sesión lo dejan (<see cref="SlothLines.HitoVoice"/>) y su frase tiene texto que se pueda enseñar y cabe en el bocadillo.
    /// Con <see cref="HitoLinePolicy.SlothHito"/> (el default desde TI6a, T6-A) la línea es entonces suya. Fuera de sus hitos
    /// habla en 1 de cada <c>ui_sloth_win_line_every_n</c> victorias si la ranura va vacía (D-016, punto 5;
    /// <see cref="SlothFactsFor"/>); mientras TiTino está a la vista, la garza del fondo se funde (<see cref="HeronFadeMs"/>).</item>
    /// <item>El título de la cinta (tarea DC14; D-029): «¡Perfecto!» si el intento ganado no llevó la Cuia de rescate y, si no,
    /// el de siempre (<see cref="VictoryTitleShown"/>).</item>
    /// </list>
    /// </summary>
    public sealed partial class GameScreen
    {
        /// <summary>
        /// Caja de TiTino con la que habla: gesto a 72 dp como mucho y bocadillo de hasta 3 líneas al 130 % (propuesta de TiTino
        /// d.4) [P; capturas de TI6a, <c>ti6a_*</c>].
        /// </summary>
        internal const float SlothSpeakBoxDp = SlothVictory.SpeakBoxDp;

        /// <summary>
        /// «Siguiente», «Inicio» y la Cosecha doble empiezan a entrar (de 0,9 a 1,0 en 150 ms; ApplyVictoryAnim) a los 600 ms de la
        /// pantalla de victoria, que se abre con la subida del panel (fila 0,80 de 07 §5.4): es la fila 1,40, la de los botones y la
        /// línea de fruta (02 §6; guía 6.9). Lo comparten ApplyVictoryAnim y la línea de fruta
        /// (<see cref="VictoryLineSlot.FruitRevealMs"/>).
        /// </summary>
        internal const float VictoryButtonsInMs = 600f;

        /// <summary>
        /// El panel de victoria termina de subir a los 250 ms de la pantalla (guía 6.9; ApplyVictoryAnim): ahí empieza la franja
        /// de llegada (02 §6, fila Llegada; tarea M1.6).
        /// </summary>
        internal const float VictoryPanelUpMs = PanelSlideMs;

        private readonly VictoryLineSlot _slot = new VictoryLineSlot();
        private VictoryLine _victoryLine;
        private float _slotTopDp;
        private float _slothBandTop;
        private float _slothBandBottom;
        private Rect _slothViewHero;
        private Rect _slothDecidedHero;
        private float _slothDecidedTop;
        private float _slothDecidedBottom;
        private bool _slothPending;
        private SlothGesture _slothPendingGesture;
        private string? _slothPendingText;
        private bool _slothPendingReduce;
        private bool _slothCanSpeak;
        private int _slothSpokeAt = -1;
        private SlothVictory? _sloth;
        private SlothSession? _slothSession;
        private bool? _slothVoiceOverride;
        private string? _slothHitoText;
        private bool _riverFactSent;
        private FactSchedule? _fruitFacts;
        private FactLine? _fruitLine;
        private readonly List<StackEntry> _stackNeeds = new List<StackEntry>(4);

        /// <summary>
        /// Victoria compacta de 16:9, escalón 1 (D-032, punto 2) [P]: márgenes justos (el marco de abajo
        /// <see cref="VictoryFrameCompactDp"/> dp, el aire bajo la Cosecha <see cref="VictoryHarvestGapCompactDp"/> y los huecos de la
        /// ranura de <see cref="VictoryLineSlot.Tight"/>) y el título de la cinta en una línea hasta
        /// <see cref="RibbonTitleCompactMinSp"/> sp antes de pasar a dos.
        /// </summary>
        internal const int VictoryCompactMargins = 1;

        /// <summary>
        /// Escalón 2 (D-032, punto 2) [P]: además, el texto de la ranura del Dato del río (el dato de la llegada y la línea de fruta)
        /// empieza en <see cref="UiBuilder.MinTextSp"/> sp, no en 14, y la ranura usa casi todo el ancho del panel
        /// (<see cref="VictorySlotCompactSideDp"/> dp a cada lado).
        /// </summary>
        internal const int VictoryCompactText = 2;

        /// <summary>
        /// Escalón 3 (D-032, punto 2) [P]: además, «Inicio» va a la izquierda de «Siguiente», en su misma fila, con «Ver mapa» entre
        /// los dos en la victoria con llegada si «Siguiente» conserva sitio para su etiqueta (<see cref="VictoryRowFitsMap"/>; si no,
        /// no sale: pregunta abierta al dueño en D-032); «Siguiente» crece en alto solo lo que pide su etiqueta
        /// (no la escala de texto entera) y el chip del nivel siguiente deja <see cref="NextChipRowCompactDp"/> dp sobre él.
        /// </summary>
        internal const int VictoryCompactRow = 3;

        /// <summary>El escalón más alto de la victoria compacta.</summary>
        internal const int VictoryCompactMax = VictoryCompactRow;

        /// <summary>Marco bajo la ranura (o «Inicio») en la victoria de siempre (dp).</summary>
        internal const float VictoryFrameDp0 = 18f;

        /// <summary>Marco de abajo desde el escalón 1 (dp) [P]: el cuerpo del panel ya tiene 8 dp de borde.</summary>
        internal const float VictoryFrameCompactDp = 8f;

        /// <summary>Aire bajo la fila de la Cosecha en la victoria de siempre (dp).</summary>
        internal const float VictoryHarvestGapDp0 = 4f;

        /// <summary>Aire bajo la fila de la Cosecha desde el escalón 1 (dp) [P]: la fila ya lleva el suyo.</summary>
        internal const float VictoryHarvestGapCompactDp = 0f;

        /// <summary>
        /// Lo que baja «Siguiente» por el chip del nivel siguiente en el escalón 3 (dp) [P]: los <see cref="NextChipHangDp"/> que
        /// cuelga y 2 de aire (en la de siempre, <see cref="NextChipRowDp"/>).
        /// </summary>
        internal const float NextChipRowCompactDp = NextChipHangDp + 2f;

        // La etiqueta de un CTA va en el 60 % de su cara (UiBuilder.PlaceCtaLabel).
        private const float CtaLabelFaceShare = 0.60f;

        /// <summary>Lo menos que baja el título de la cinta en una línea desde el escalón 1 (sp, × la escala de texto) [P].</summary>
        internal const float RibbonTitleCompactMinSp = 16f;

        /// <summary>Margen de la ranura a cada lado desde el escalón 2 (dp) [P]; en la de siempre, <see cref="VictoryLineSlot.SideDp"/>.</summary>
        internal const float VictorySlotCompactSideDp = 8f;

        /// <summary>
        /// Margen interior de la línea de fruta a cada lado desde el escalón 2 (dp): lo que la ranura se acerca al canto, para que el
        /// texto, sin placa propia, quede a <see cref="VictoryLineSlot.SideDp"/> dp del canto, como siempre (8 dp de aire dentro del marco).
        /// </summary>
        internal const float FruitLineCompactInsetDp = VictoryLineSlot.SideDp - VictorySlotCompactSideDp;

        /// <summary>Hueco entre «Inicio» y «Siguiente» en la fila del escalón 3 (dp) [P].</summary>
        internal const float VictoryRowGapDp = 8f;

        /// <summary>Ancho mínimo de «Inicio» en la fila del escalón 3 (dp) [P].</summary>
        internal const float VictoryRowHomeMinDp = 96f;

        /// <summary>Parte del ancho de la fila que puede llevarse «Inicio» como mucho en el escalón 3 [P].</summary>
        internal const float VictoryRowHomeMaxShare = 0.42f;

        /// <summary>Cuerpo de la etiqueta de «Siguiente» en la fila del escalón 3: de 22 sp hasta este mínimo (× la escala; el de «Inicio») [P].</summary>
        internal const float NextRowMinSp = 14f;

        private int _victoryCompact;
        private string? _nextNeedText;
        private float _nextNeedScale = -1f;
        private float _nextNeedFaceDp;
        private float _victoryNextCenterX = 164f;
        private string? _ribbonWorstLocale;
        private float _ribbonWorstScale = -1f;
        private float _ribbonWorst0;
        private float _ribbonWorst1;

        /// <summary>
        /// Escalón de la victoria compacta de la última maquetación (D-032, punto 2; 0 = la de siempre, hasta
        /// <see cref="VictoryCompactMax"/>): el más bajo con el que la victoria más alta posible de esta pantalla, idioma y escala
        /// (cinta con el título más alto, Cosecha doble, el chip del nivel siguiente y la ranura) deja la cinta bajo la cabecera
        /// del HUD. No depende de lo que lleve cada victoria, así que «Siguiente» queda a la misma altura en todas (Q7-A); en el
        /// escalón 3, en la victoria con «Ver mapa», va más estrecho y empieza más a la derecha, con el mismo canto derecho [P]
        /// (PlaceVictoryRow).
        /// </summary>
        internal int VictoryCompact => _victoryCompact;

        /// <summary>Margen de la ranura a cada lado en el escalón de ahora (dp).</summary>
        internal float VictorySlotSideDp => _victoryCompact >= VictoryCompactText ? VictorySlotCompactSideDp : VictoryLineSlot.SideDp;

        /// <summary>
        /// Lo más alto que puede ser el panel de victoria para que la cinta (que asoma <c>RibbonRiseDp</c> sobre él) quede bajo la
        /// cabecera del HUD (dp): el panel va anclado abajo, a 8 dp de la zona segura.
        /// </summary>
        internal float VictoryPanelBudgetDp => _canvasH - _safeBottom - 8f - HudBadgeBottom - RibbonRiseDp;

        /// <summary>Alto de la victoria más alta posible con el escalón <paramref name="level"/> en esta pantalla (dp; tests).</summary>
        internal float VictoryWorstPanelDp(int level)
        {
            int keep = _victoryCompact;
            ApplyVictoryCompact(level);
            float panel = WorstPanelDp(level);
            ApplyVictoryCompact(keep);
            return panel;
        }

        // Elige el escalón (lo más bajo que quepa; si ni el último cabe, el último) y deja la ranura y sus proveedores con él. Mide
        // con TextMeshPro solo lo que no esté en la caché: WarmVictorySlot lo llama al cargar el nivel.
        private int ChooseVictoryCompact()
        {
            float budget = VictoryPanelBudgetDp;
            _victoryBarsDropped = false;
            _victoryBars.LogbookSuppressed = false;
            int level = CompactStepWithBars(budget);
            if (level >= VictoryCompactRow && _victoryBars.Plan.Logbook)
            {
                // La fila de la Bitácora (GameScreen.LogbookRow.cs) en el escalón 3 iría tras el chevron (D-033, punto 10): no sale y
                // el escalón se elige sin ella [P]; la libreta sale sola al volver al inicio (D-033, punto 5).
                _victoryBars.LogbookSuppressed = true;
                level = CompactStepWithBars(budget);
            }

            if (WorstPanelDp(level) > budget + 0.01f && VictoryBarsPossible)
            {
                // Ni el último escalón cabe con las barras de la Crecida y la Canasta: no salen («si no cabe, no sale», como «Ver
                // mapa» en D-032 y la regla de 02 §5) [P] y el escalón se elige sin ellas (tarea B1b; pregunta abierta al dueño).
                _victoryBarsDropped = true;
                level = CompactStepFor(budget);
            }

            ApplyVictoryCompact(level);
            return level;
        }

        // El escalón con las barras de la Crecida y la Canasta: antes de compactar lo demás, en una fila, a media anchura [P] (tarea
        // B1b): así 360 × 780 no compacta (D-032, punto 2) y en 16:9 se compacta lo menos posible; desde el escalón 2 ya van así
        // (D-033, punto 10).
        private int CompactStepWithBars(float budget)
        {
            _victoryBars.ForceHalf = false;
            int level = CompactStepFor(budget);
            if (level > 0 && VictoryBarsPossible)
            {
                _victoryBars.ForceHalf = true;
                level = CompactStepFor(budget);
            }

            return level;
        }

        // El escalón más bajo con el que la victoria más alta posible cabe en budget (o el último).
        private int CompactStepFor(float budget)
        {
            int level = 0;
            while (level < VictoryCompactMax)
            {
                ApplyVictoryCompact(level);
                if (WorstPanelDp(level) <= budget + 0.01f)
                {
                    break;
                }

                level++;
            }

            ApplyVictoryCompact(level);
            return level;
        }

        // El escalón en la ranura y en sus proveedores: los huecos (Tight) y el cuerpo del texto (BodySp).
        private void ApplyVictoryCompact(int level)
        {
            _victoryCompact = level;
            float body = level >= VictoryCompactText ? UiBuilder.MinTextSp : VictoryLineSlot.TextSp;
            _slot.Tight = level >= VictoryCompactMargins;
            _slot.BodySp = body;
            if (_arrival != null)
            {
                _arrival.BodySp = body;
            }

            if (_fruitLine != null)
            {
                _fruitLine.BodySp = body;
                _fruitLine.InsetDp = level >= VictoryCompactText ? FruitLineCompactInsetDp : 0f;
            }
        }

        // La victoria más alta posible con el escalón de ahora (ApplyVictoryCompact ya hecho): como LayoutVictory, con la cinta del
        // título más alto, la Cosecha doble y el chip del nivel siguiente.
        private float WorstPanelDp(int level)
        {
            float ribbon = WorstRibbonDp(level >= VictoryCompactMargins);
            float above = VictoryHarvestTopDp + (ribbon - RibbonHeightDp) + VictoryHarvestDp + VictoryHarvestGapDp(level)
                + WorstBarsDp(level) // las barras de la Crecida y la Canasta más altas posibles (D-033, punto 10; GameScreen.Streak.cs)
                + ModalKit.HeightOf(_double) + 2f + (_labelsBuilt ? NextChipRowFor(level) : 0f);
            float below = level >= VictoryCompactRow
                ? Mathf.Max(NextHeightFor(CompactNextStretch()), 48f)
                : VictoryHomeBelowCtaDp + VictoryNextGrowDp + 48f;
            if (_slot.Reserved)
            {
                _slot.Build(_victoryPanel);
                string locale = Loc.IsConfigured ? Loc.Locale : LocLocale.SourceLocale;
                below += _slot.ReserveFor(SlotContentDp(locale, UiBuilder.TextScale));
            }

            return above + below + VictoryFrameDp(level);
        }

        // Lo que Siguiente crece en alto con la escala de texto (ModalKit.StretchForTextScale; tarea L4): Inicio baja lo mismo.
        private float VictoryNextGrowDp => Mathf.Max(0f, NextHeightFor(Mathf.Max(1f, UiBuilder.TextScale)) - Mathf.Max(48f, _art.CtaLeaf.HeightDp - 6f));

        // Alto de «Siguiente» con su relieve estirado k veces (como ModalKit.HeightOf).
        private float NextHeightFor(float k)
        {
            return Mathf.Max(48f, (_next.Sprites.HeightDp * Mathf.Max(1f, k)) - 6f);
        }

        // Estirado de «Siguiente» en la fila del escalón 3: lo justo para que su etiqueta quepa a NextRowMinSp en el 60 % de la cara;
        // nunca más que la escala de texto (lo de siempre).
        private float CompactNextStretch()
        {
            float face = _next.Sprites.FaceHeightDp;
            float scale = Mathf.Max(1f, UiBuilder.TextScale);
            if (face <= 0f)
            {
                return scale;
            }

            return Mathf.Clamp(NextNeedFaceDp() / face, 1f, scale);
        }

        // La cara que necesita la etiqueta de «Siguiente» a NextRowMinSp (× la escala) en el 60 % de su alto (dp): su alto medido con
        // TextMeshPro (Baloo 2 lleva una línea bastante más alta que su cuerpo), en caché por texto y escala; deja la etiqueta como estaba.
        private float NextNeedFaceDp()
        {
            TMPro.TextMeshProUGUI label = _nextLabel;
            float scale = UiBuilder.TextScale;
            string text = label.text;
            if (string.Equals(_nextNeedText, text, StringComparison.Ordinal) && Mathf.Approximately(_nextNeedScale, scale))
            {
                return _nextNeedFaceDp;
            }

            bool auto = label.enableAutoSizing;
            float size = label.fontSize;
            label.enableAutoSizing = false;
            label.fontSize = NextRowMinSp * scale;
            float h = label.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).y;
            label.fontSize = size;
            label.enableAutoSizing = auto;
            _nextNeedText = text;
            _nextNeedScale = scale;
            _nextNeedFaceDp = Mathf.Ceil((h + 1f) / CtaLabelFaceShare);
            return _nextNeedFaceDp;
        }

        // Estira «Siguiente» como siempre (la escala de texto, ModalKit.StretchForTextScale) o, en la fila del escalón 3, lo justo
        // (CompactNextStretch), y recoloca su etiqueta en la cara; solo si cambia.
        private void ApplyNextStretch(bool compact)
        {
            float k = compact ? CompactNextStretch() : Mathf.Max(1f, UiBuilder.TextScale);
            RectTransform visual = (RectTransform)_next.Face.parent;
            float h = _next.Sprites.HeightDp * k;
            if (Mathf.Approximately(visual.sizeDelta.y, h))
            {
                return;
            }

            visual.sizeDelta = new Vector2(visual.sizeDelta.x, h);
            UiBuilder.PlaceCtaLabel(_nextLabel.rectTransform, ModalKit.FaceHeightOf(_next));
        }

        // Lo que baja «Siguiente» por el chip del nivel siguiente con el escalón level.
        private static float NextChipRowFor(int level)
        {
            return level >= VictoryCompactRow ? NextChipRowCompactDp : NextChipRowDp;
        }

        private static float VictoryHarvestGapDp(int level)
        {
            return level >= VictoryCompactMargins ? VictoryHarvestGapCompactDp : VictoryHarvestGapDp0;
        }

        private static float VictoryFrameDp(int level)
        {
            return level >= VictoryCompactMargins ? VictoryFrameCompactDp : VictoryFrameDp0;
        }

        // Mínimo del título de la cinta (dp, con la escala): 22 sp o, desde el escalón 1, 16 sp.
        private float RibbonTitleMinDp(bool compact)
        {
            return compact ? Mathf.Min(_ribbonTitleMinDp, RibbonTitleCompactMinSp * UiBuilder.TextScale) : _ribbonTitleMinDp;
        }

        // Alto de la cinta con el título más alto de los dos que puede llevar («¡Perfecto!» y el de siempre, DC14) en el idioma y la
        // escala de ahora (dp), sin tocar el título que se ve; en caché por idioma y escala.
        private float WorstRibbonDp(bool compact)
        {
            string locale = Loc.IsConfigured ? Loc.Locale : LocLocale.SourceLocale;
            float scale = UiBuilder.TextScale;
            if (!string.Equals(_ribbonWorstLocale, locale, StringComparison.Ordinal) || !Mathf.Approximately(_ribbonWorstScale, scale))
            {
                _ribbonWorstLocale = locale;
                _ribbonWorstScale = scale;
                string perfect = UiText.VictoryTitle(true);
                string plain = UiText.VictoryTitle(false);
                _ribbonWorst0 = Mathf.Max(RibbonHeightFor(perfect, false), RibbonHeightFor(plain, false));
                _ribbonWorst1 = Mathf.Max(RibbonHeightFor(perfect, true), RibbonHeightFor(plain, true));
            }

            return compact ? _ribbonWorst1 : _ribbonWorst0;
        }

        // Alto de la cinta con el título value (como LayoutRibbonTitle, sin cambiar el texto del título; deja su estado como estaba).
        private float RibbonHeightFor(string value, bool compact)
        {
            const float boxW = RibbonWidthDp - 88f;
            TMPro.TextMeshProUGUI title = _ribbonTitle;
            bool auto = title.enableAutoSizing;
            float size = title.fontSize;
            float min = title.fontSizeMin;
            float max = title.fontSizeMax;
            TMPro.TextWrappingModes wrapping = title.textWrappingMode;
            float spacing = title.lineSpacing;
            float tmin = RibbonTitleMinDp(compact);
            title.enableAutoSizing = false;
            title.fontSize = tmin;
            title.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            title.lineSpacing = 0f;
            float h = RibbonHeightDp;
            if (!string.IsNullOrEmpty(value) && title.GetPreferredValues(value, float.PositiveInfinity, float.PositiveInfinity).x > boxW)
            {
                title.textWrappingMode = TMPro.TextWrappingModes.Normal;
                title.lineSpacing = RibbonTitleLineSpacing;
                float need = Mathf.Ceil(title.GetPreferredValues(value, boxW, float.PositiveInfinity).y) + 1f;
                h = Mathf.Clamp(need + RibbonTitleMarginDp, RibbonHeightDp, RibbonTallDp);
            }

            title.enableAutoSizing = auto;
            title.fontSizeMin = min;
            title.fontSizeMax = max;
            title.fontSize = size;
            title.textWrappingMode = wrapping;
            title.lineSpacing = spacing;
            return h;
        }

        // Ancho de «Inicio» en la fila del escalón 3: lo que pide su etiqueta a su mínimo (más sus márgenes), entre
        // VictoryRowHomeMinDp y VictoryRowHomeMaxShare de la fila.
        private float VictoryRowHomeWidthDp(float rowW)
        {
            float need = Mathf.Ceil(UiBuilder.MinTextWidth(_homeLabel)) + CtaLabelPadDp;
            return Mathf.Clamp(need, VictoryRowHomeMinDp, Mathf.Floor(rowW * VictoryRowHomeMaxShare));
        }

        // Lo que pide de ancho un botón para su etiqueta: los 24 dp de sus márgenes (UiBuilder.PlaceCtaLabel) y 2 de holgura.
        private const float CtaLabelPadDp = 24f + 2f;

        // Ancho de una sola línea del texto de ahora de label a sizeDp (dp), sin el autoajuste; deja la etiqueta como estaba. Mide
        // con TextMeshPro al colocar, nunca por fotograma.
        private static float LabelWidthAt(TMPro.TextMeshProUGUI label, float sizeDp)
        {
            bool auto = label.enableAutoSizing;
            float size = label.fontSize;
            label.enableAutoSizing = false;
            label.fontSize = sizeDp;
            float w = label.GetPreferredValues(label.text, float.PositiveInfinity, float.PositiveInfinity).x;
            label.fontSize = size;
            label.enableAutoSizing = auto;
            return w;
        }

        /// <summary>
        /// Lo menos que mide de ancho cada botón secundario («Inicio» y «Ver mapa») en la fila del escalón 3 con la llegada, si la
        /// fila deja ese sitio (dp) [P]; si no, cada uno lo que pide su etiqueta, y nunca menos de 48.
        /// </summary>
        internal const float VictoryRowPairMinDp = 72f;

        // Anchos de la fila del escalón 3 con «Ver mapa» (D-032, punto 2): «Inicio» y «Ver mapa» a la izquierda, del mismo ancho si
        // cabe (lo que pida la etiqueta más larga de las dos a 12 sp × la escala, al menos VictoryRowPairMinDp) o cada uno el suyo, y
        // «Siguiente» con el resto. false si «Siguiente» no tiene sitio para su etiqueta a NextRowMinSp × la escala.
        private bool VictoryRowMapWidths(float rowW, out float homeW, out float mapW)
        {
            float scale = UiBuilder.TextScale;
            float homeNeed = Mathf.Max(48f, Mathf.Ceil(UiBuilder.MinTextWidth(_homeLabel)) + CtaLabelPadDp);
            float mapNeed = _victoryMapLabel != null
                ? Mathf.Max(48f, Mathf.Ceil(LabelWidthAt(_victoryMapLabel, UiBuilder.MinTextSp * scale)) + CtaLabelPadDp)
                : 48f;
            float nextNeed = Mathf.Max(48f, Mathf.Ceil(LabelWidthAt(_nextLabel, Mathf.Max(UiBuilder.MinTextSp, NextRowMinSp) * scale)) + CtaLabelPadDp);
            float pair = Mathf.Max(VictoryRowPairMinDp, Mathf.Max(homeNeed, mapNeed));
            if (rowW - (2f * pair) - (2f * VictoryRowGapDp) >= nextNeed)
            {
                homeW = pair;
                mapW = pair;
                return true;
            }

            homeW = homeNeed;
            mapW = mapNeed;
            return rowW - homeW - mapW - (2f * VictoryRowGapDp) >= nextNeed;
        }

        /// <summary>
        /// Con la llegada, en el escalón 3, «Ver mapa» cabe en la fila de «Inicio» y «Siguiente» (D-032, punto 2): «Siguiente» tiene
        /// sitio para su etiqueta a <see cref="NextRowMinSp"/> sp × la escala. Mide con TextMeshPro; al colocar, nunca por fotograma.
        /// </summary>
        internal bool VictoryRowFitsMap()
        {
            float nextW = Mathf.Min(_canvasW - 48f, Mathf.Min(312f, VictoryPanelWidthDp - 16f));
            return VictoryRowMapWidths(nextW, out _, out _);
        }

        // La fila del escalón 3 (D-032, punto 2) [P]: «Inicio» a la izquierda de «Siguiente», en su misma fila y centrado a lo alto con
        // él, y, con la llegada (ArrivalVictoryShown), «Ver mapa» entre los dos; «Siguiente» se queda con el resto del ancho y su
        // etiqueta se ajusta (14-22 sp × la escala, nunca menos de 12). La fila no cambia de alto con «Ver mapa», así que «Siguiente»
        // queda a la misma altura en todas las victorias (Q7-A) y con el mismo canto derecho; con «Ver mapa», más estrecho y desplazado
        // a la derecha (su centro y el chip del nivel siguiente también) [P] (D-032, punto 2). Devuelve el pie de la fila.
        private float PlaceVictoryRow(float rowX, float y, float nextW, float nextH)
        {
            float homeW = 0f;
            float mapW = 0f;
            bool map = _victoryMapShown && VictoryRowMapWidths(nextW, out homeW, out mapW);
            if (!map)
            {
                homeW = VictoryRowHomeWidthDp(nextW);
            }

            float left = homeW + VictoryRowGapDp + (map ? mapW + VictoryRowGapDp : 0f);
            float nextX = rowX + left;
            float nextRowW = nextW - left;
            SetNextLabelFit(true);
            UiBuilder.Place((RectTransform)_next.Button.transform, nextX, y, nextRowW, nextH);
            _victoryNextCenterX = nextX + (nextRowW * 0.5f);
            _victoryHomeTopDp = y + Mathf.Max(0f, (nextH - 48f) * 0.5f);
            UiBuilder.Place((RectTransform)_home.Button.transform, rowX, _victoryHomeTopDp, homeW, 48f);
            if (map)
            {
                UiBuilder.Place((RectTransform)_victoryMap.Button.transform, rowX + homeW + VictoryRowGapDp, _victoryHomeTopDp, mapW, 48f);
            }

            return y + Mathf.Max(nextH, 48f);
        }

        // Vuelve a colocar la fila del escalón 3 cuando «Ver mapa» entra o sale tras LayoutVictory (ArrivalVictoryShown), y el chip
        // del nivel siguiente sobre el nuevo centro de «Siguiente».
        private void RelayoutVictoryRow()
        {
            float panelW = VictoryPanelWidthDp;
            float nextW = Mathf.Min(_canvasW - 48f, Mathf.Min(312f, panelW - 16f));
            PlaceVictoryRow((panelW - nextW) / 2f, _victoryCtaTopDp, nextW, ModalKit.HeightOf(_next));
            LabelsLayoutVictory();
        }

        // La etiqueta de «Siguiente»: de tamaño fijo (la de siempre) o, en la fila del escalón 3, ajustada de 22 sp a
        // NextRowMinSp (× la escala de texto). Solo cambia lo que hace falta.
        private void SetNextLabelFit(bool fit)
        {
            TMPro.TextMeshProUGUI label = _nextLabel;
            if (fit)
            {
                if (!label.enableAutoSizing)
                {
                    label.enableAutoSizing = true;
                    label.fontSizeMin = Mathf.Max(UiBuilder.MinTextSp * UiBuilder.TextScale, NextRowMinSp * UiBuilder.TextScale);
                    label.fontSizeMax = _nextLabelDp;
                }
            }
            else if (label.enableAutoSizing)
            {
                label.enableAutoSizing = false;
                label.fontSize = _nextLabelDp;
            }
        }

        /// <summary>Ancho del panel de victoria (dp): 328, o el del lienzo menos 32 si es más estrecho.</summary>
        internal float VictoryPanelWidthDp => Mathf.Min(328f, _canvasW - 32f);

        /// <summary>Centro de «Siguiente» en el panel de victoria (dp desde su canto izquierdo): ahí cuelga el chip del nivel siguiente.</summary>
        internal float VictoryNextCenterXDp => _victoryNextCenterX;

        /// <summary>La ranura del Dato del río (tests y proveedores).</summary>
        internal VictoryLineSlot VictorySlot => _slot;

        /// <summary>La línea que dio el árbitro en la última victoria (default antes de la primera).</summary>
        internal VictoryLine CurrentVictoryLine => _victoryLine;

        /// <summary>
        /// TiTino tiene voz: <c>ui_sloth_enabled</c> ∧ <c>settings.sloth_messages</c> (D-016 punto 4; propuesta de TiTino d.4),
        /// con el ajuste de los servicios del juego (sin ellos, encendido, su default). Sin voz, solo hace el gesto. Los tests
        /// pueden fijarlo.
        /// </summary>
        internal bool SlothVoice
        {
            get => _slothVoiceOverride ?? (SlothEnabled && SlothMessagesSetting);
            set => _slothVoiceOverride = value;
        }

        /// <summary>
        /// TiTino sale en la victoria: <c>ui_sloth_enabled</c> (08 §9; <see cref="GameConfig.UiSlothEnabled"/>), salvo que un test
        /// lo fije. Apagado, no sale ni hace gestos.
        /// </summary>
        internal bool SlothEnabled { get; set; } = GameConfig.UiSlothEnabled;

        // settings.sloth_messages del perfil (Ajustes › Mensajes de TiTino); sin servicios (tests), su default.
        private bool SlothMessagesSetting => _services == null || _services.Settings.SlothMessages;

        /// <summary>
        /// Estado de TiTino en la sesión (D-016, punto 5: frases dichas, pantalla anterior con frase y <c>sloth.last_line</c>),
        /// que comparten sus apariciones. Quien arranca el juego puede dar el del perfil; si nadie lo da, la pantalla usa uno
        /// propio que vive lo que ella (sin guardar <c>sloth.last_line</c>).
        /// </summary>
        internal SlothSession SlothSession
        {
            get => _slothSession ??= new SlothSession(new SlothData(), SlothMessagesSetting);
            set => _slothSession = value;
        }

        /// <summary>TiTino en la victoria.</summary>
        internal SlothVictory SlothView => _sloth!;

        /// <summary>Lo que decidió TiTino en la última victoria (con frase solo si se enseñó; default antes de la primera).</summary>
        internal SlothLine LastSlothLine { get; private set; }

        /// <summary>
        /// Lo que TiTino supo de la última victoria en la que se decidió su línea (<see cref="SlothFactsFor"/>; tests de FX8:
        /// <c>usedHint</c> con una pista de Morpho cobrada).
        /// </summary>
        internal SlothVictoryFacts LastSlothFacts { get; private set; }

        /// <summary>
        /// El Mapa del río existe en el cliente (en el 10 cuenta él la primera llegada, 06 §2.3): false hasta M3.3, que lo
        /// enciende.
        /// </summary>
        internal bool MapAvailable { get; set; }

        /// <summary>
        /// Clave de la línea de fruta del nivel (D-017; 08 §4.6), o null: la da el calendario de Frutas del río
        /// (<see cref="FruitFacts"/>, <see cref="FactSchedule.KeyFor"/>; los tests pueden darla a mano). Sin él, ningún nivel
        /// tiene línea de fruta.
        /// </summary>
        internal Func<int, string?>? FruitKeyOf { get; set; }

        /// <summary>
        /// Calendario de Frutas del río (<c>af-facts/1</c>, D-017; lo pasa <see cref="GameBootstrap"/>; null = sin el archivo o
        /// inválido: el juego sigue sin líneas de fruta). Al darlo, da también <see cref="FruitKeyOf"/> y crea la línea de fruta
        /// (<see cref="FactLine"/>), que se registra en la ranura (salvo con <see cref="FactLineDisabledForTests"/>).
        /// </summary>
        public FactSchedule? FruitFacts
        {
            get => _fruitFacts;
            set
            {
                _fruitFacts = value;
                FruitKeyOf = value != null ? value.KeyFor : (Func<int, string?>?)null;
                if (value != null && _fruitLine == null && !FactLineDisabledForTests)
                {
                    _fruitLine = new FactLine(_art, _victoryPanel, () => _fruitFacts, FruitBlockDp, PlaceAboveFruitDp);
                    RegisterVictoryLineProvider(_fruitLine);
                }
            }
        }

        /// <summary>
        /// Sin la línea de fruta en las pantallas que reciban el calendario mientras valga true (tests de la ranura sola o de la
        /// llegada sola; como <see cref="ArrivalDisabledForTests"/>). No se usa en el juego.
        /// </summary>
        internal static bool FactLineDisabledForTests { get; set; }

        /// <summary>La línea de fruta (null sin calendario o con <see cref="FactLineDisabledForTests"/>).</summary>
        internal FactLine? FruitLine => _fruitLine;

        /// <summary>
        /// Cadencia de la línea de fruta en lugar de <c>GameConfig.FruitLineEveryN</c> (tests: 0, 1 y 2); null = la del
        /// cliente.
        /// </summary>
        internal int? FruitEveryNOverride { get; set; }

        /// <summary>
        /// Cadencia del dato de llegada en lugar de <see cref="MapConfig.UiRiverFactEveryNLevels"/> (tests: una llegada sin dato con
        /// Canon); null = la del cliente.
        /// </summary>
        internal int? RiverFactEveryNOverride { get; set; }

        /// <summary>
        /// Política de los hitos de TiTino en lugar de <see cref="MapConfig.HitoPolicy"/> (tests: ArrivalEverywhere, la de antes de TI6a); null =
        /// la del cliente.
        /// </summary>
        internal HitoLinePolicy? HitoPolicyOverride { get; set; }

        /// <summary>
        /// Política del dato de llegada en lugar de <see cref="MapConfig.RiverPolicy"/> (tests: SingleKey); null = la del cliente.
        /// </summary>
        internal RiverFactPolicy? RiverPolicyOverride { get; set; }

        /// <summary>
        /// <c>SlothCanSpeak</c> de la caja de TiTino en los hitos en lugar de medirla (tests); null = voz ∧ caja ≥
        /// <see cref="SlothSpeakBoxDp"/> dp.
        /// </summary>
        internal bool? SlothBoxCanSpeakOverride { get; set; }

        /// <summary>Veces que se escribió <c>river_fact_shown</c> (tests).</summary>
        internal int RiverFactShownCount { get; private set; }

        /// <summary>El <c>fact_id</c> del último <c>river_fact_shown</c> (null antes del primero).</summary>
        internal string? LastRiverFactId { get; private set; }

        /// <summary>El <c>dismissed_ms</c> del último <c>river_fact_shown</c>.</summary>
        internal float LastRiverFactDismissedMs { get; private set; } = -1f;

        /// <summary>
        /// Canto de arriba de la caja de TiTino en la última victoria (dp desde arriba): el pie de la cabecera o, en la victoria con
        /// vista, el pie del héroe del sitio si TiTino va debajo de él (DC2).
        /// </summary>
        internal float SlothBandTopDp => _slothBandTop;

        /// <summary>
        /// Canto de abajo de la caja de TiTino en la última victoria (dp desde arriba): en la victoria con vista, la cima de la cinta
        /// menos <see cref="SlothRibbonGapDp"/> o la cima del héroe del sitio si TiTino va encima de él (DC2); sin ella, la cima de
        /// las cuias (con su elevación) tras ShiftToClear.
        /// </summary>
        internal float SlothBandBottomDp => _slothBandBottom;

        /// <summary>Alto de la caja de TiTino en la última victoria (dp).</summary>
        internal float SlothBoxDp => Mathf.Max(0f, _slothBandBottom - _slothBandTop);

        /// <summary>
        /// Alto que tendría la caja de TiTino ahora si ShiftToClear subiera el tablero todo lo que pide el panel de victoria, sin
        /// el tope de la cabecera (dp; negativo = lo que le falta al tablero para no tocar la cabecera): la medida para la
        /// decisión del dueño sobre el sitio de TiTino (TI6a). Con la victoria a la vista; sin tablero, 0.
        /// </summary>
        internal float SlothBoxUnclampedDp
        {
            get
            {
                if (_layout == null || _views.Length == 0)
                {
                    return 0f;
                }

                float needed = Mathf.Max(0f, _layout.PanelBottom - (_victoryPanelY - RibbonRiseDp - 8f));
                return CuiasTopDp() - needed - HudBadgeBottom;
            }
        }

        /// <summary>El valor de <c>SlothCanSpeak</c> que se pasó al árbitro en la última victoria.</summary>
        internal bool SlothCouldSpeak { get; private set; }

        /// <summary>Canto de arriba de «Siguiente» (dp desde arriba del lienzo; el panel está anclado abajo).</summary>
        internal float NextTopDp => _victoryPanelY + _victoryCtaTopDp;

        /// <summary>Pie del área táctil de «Inicio» (dp desde arriba del lienzo).</summary>
        internal float HomeBottomDp => _victoryPanelY + _victoryHomeTopDp + 48f;

        /// <summary>Canto de arriba del panel de victoria (dp desde arriba del lienzo; la cinta asoma 38 dp por encima).</summary>
        internal float VictoryPanelTopDp => _victoryPanelY;

        /// <summary>Pie del panel de victoria (dp desde arriba del lienzo).</summary>
        internal float VictoryPanelBottomDp => _victoryPanelY + _victoryPanelDp;

        /// <summary>Canto de arriba de la ranura (dp desde arriba del lienzo; con reserva).</summary>
        internal float SlotTopDp => _victoryPanelY + _slotTopDp;

        /// <summary>Opacidad de «Siguiente» (entra de 0,60 a 0,75 s; tests).</summary>
        internal float NextAlpha => _nextGroup.alpha;

        /// <summary>Ancho de la ranura (dp): el del panel de victoria menos sus márgenes.</summary>
        internal float VictorySlotWidthDp => Mathf.Min(328f, _canvasW - 32f) - (2f * VictorySlotSideDp);

        /// <summary>
        /// Título de la cinta en esta victoria (tarea DC14; Magic Sort §10.2 #25, recomendada A; D-029): «¡Perfecto!»
        /// (<c>victory_perfect</c>) si el intento ganado no llevó la Cuia de rescate (<see cref="LevelFlow.IsPerfect"/>); si no,
        /// «¡Nivel completado!». Sustituye al título, sin línea nueva: la ranura del Dato del río y la franja de TiTino no cambian
        /// y la cinta lo mide como siempre (LayoutRibbonTitle, en una o dos líneas). Se fija en cada victoria, antes de colocarla.
        /// </summary>
        private void VictoryTitleShown(LevelFlow flow)
        {
            string title = UiText.VictoryTitle(flow.IsPerfect);
            if (!string.Equals(_ribbonTitle.text, title, StringComparison.Ordinal))
            {
                _ribbonTitle.text = title;
            }
        }

        /// <summary>
        /// Registra un proveedor de la ranura (M1.6 y F3, desde sus ganchos de construcción, y los tests). La reserva entra en
        /// la siguiente maquetación de la victoria (OnVictoryScreen la hace): una victoria que ya se ve no cambia.
        /// </summary>
        internal void RegisterVictoryLineProvider(IVictoryLineProvider provider)
        {
            _slot.Register(provider);
        }

        /// <summary>Quita un proveedor de la ranura (tests); como al registrar, cuenta desde la siguiente victoria.</summary>
        internal void UnregisterVictoryLineProvider(IVictoryLineProvider provider)
        {
            _slot.Unregister(provider);
        }

        /// <summary>
        /// Alto del contenido de la ranura (bajo el filete) para <paramref name="locale"/> y la escala de texto
        /// <paramref name="textScale"/> con el ancho de ahora (dp; medido y en caché).
        /// </summary>
        internal float VictorySlotContentHeight(string locale, float textScale)
        {
            _slot.Build(_victoryPanel);
            return _slot.ContentHeightFor(locale, textScale, VictorySlotWidthDp);
        }

        /// <summary>
        /// Alto del contenido que reserva la ranura en el idioma de <see cref="Loc"/> a la escala <paramref name="textScale"/> con el
        /// ancho de ahora: máx(línea de fruta, llegada más alta del catálogo) (pregunta 11, (a) [P]; <see cref="ArrivalStrip.NeedDp"/>).
        /// </summary>
        internal float VictorySlotHeight(float textScale)
        {
            _slot.Build(_victoryPanel);
            string locale = Loc.IsConfigured ? Loc.Locale : LocLocale.SourceLocale;
            return SlotContentDp(locale, textScale);
        }

        // Pregunta 11, (a) [P]: la llegada compactada no siempre cabe en el alto de la línea de fruta (medidas de M1.3b), así que la
        // ranura reserva lo mayor de los dos; los dos son fijos por idioma, escala y ancho, y «Siguiente» no se mueve de una
        // victoria a otra (Q7-A). Si alguna victoria lleva la placa del lugar y la línea de fruta juntas (StackNeedDp), también eso.
        private float SlotContentDp(string locale, float textScale)
        {
            float width = VictorySlotWidthDp;
            float fruit = FruitBlockFor(locale, textScale, width);
            ArrivalStrip? arrival = _arrival;
            if (arrival == null || !arrival.SlotActive)
            {
                return fruit;
            }

            return Mathf.Max(Mathf.Max(fruit, arrival.NeedDp(textScale, width)), StackNeedDp(locale, textScale, width, fruit));
        }

        /// <summary>
        /// Lo que ocupan juntas la placa del lugar y la línea de fruta (dp; 0 si no van juntas en ninguna victoria): la placa más
        /// alta de las llegadas en las que el árbitro da la placa sin dato y además la línea de fruta (<see cref="VictoryLine.Arrival"/>
        /// sin <see cref="VictoryLine.ArrivalFact"/> y con <see cref="VictoryLine.FruitKey"/>: con SingleKey en los <c>10k</c>, con
        /// otra cadencia del dato de llegada o en una parada sin dato; arquitectura de la tanda 4, §2), más
        /// <see cref="FactLine.StackGapDp"/> y el alto fijo H de la línea de fruta (<paramref name="fruitDp"/>). Pregunta al árbitro
        /// con la política de ahora (también las de los tests), así que con los defaults y los datos de hoy es 0. En caché.
        /// </summary>
        internal float StackNeedDp(string locale, float textScale, float widthDp, float fruitDp)
        {
            ArrivalStrip? arrival = _arrival;
            FactLine? fruitLine = _fruitLine;
            RiverStopCatalog? stops = RiverStops;
            if (arrival == null || !arrival.SlotActive || fruitLine == null || !fruitLine.SlotActive || stops == null)
            {
                return 0f;
            }

            // La política de ahora (los defaults del cliente o los de los tests), para la caché.
            VictoryContext policy = ArbiterContext(RiverStopCatalog.LevelsPerStop, false);
            RiverFactPolicy river = policy.River;
            int riverEvery = policy.RiverFactEveryN;
            int fruitEvery = policy.FruitEveryN;
            Func<int, string?>? keyOf = FruitKeyOf;
            for (int i = 0; i < _stackNeeds.Count; i++)
            {
                StackEntry e = _stackNeeds[i];
                if (ReferenceEquals(e.Stops, stops) && ReferenceEquals(e.KeyOf, keyOf) && e.Locale == locale && e.River == river
                    && e.RiverEvery == riverEvery && e.FruitEvery == fruitEvery && Mathf.Approximately(e.Scale, textScale)
                    && Mathf.Approximately(e.Width, widthDp) && Mathf.Approximately(e.Fruit, fruitDp))
                {
                    return e.Height;
                }
            }

            float place = 0f;
            for (int n = 2; n < stops.Count; n++)
            {
                int won = n * RiverStopCatalog.LevelsPerStop;
                RiverStop? stop = stops.At(n);
                if (won > WorldTable.LastLevel || stop == null)
                {
                    continue;
                }

                // TiTino callado: el peor caso (si habla en un hito, no hay línea de fruta).
                VictoryContext context = ArbiterContext(won, false);
                VictoryLine line = VictoryLineArbiter.For(in context);
                if (line.Arrival && !line.ArrivalFact && line.FruitKey != null)
                {
                    place = Mathf.Max(place, arrival.PlaceHeightOf(stop, textScale, widthDp));
                }
            }

            float need = place > 0f ? Mathf.Ceil(place + FactLine.StackGapDp + fruitDp) : 0f;
            _stackNeeds.Add(new StackEntry(stops, keyOf, locale, river, riverEvery, fruitEvery, textScale, widthDp, fruitDp, need));
            return need;
        }

        /// <summary>
        /// Calienta las medidas de la ranura con el idioma, la escala y el ancho de ahora (<see cref="FactLine.NeedDp"/>, la llegada
        /// más alta y <see cref="StackNeedDp"/>), para que la primera victoria no las pague al subir el panel (07 §5.4; revisión
        /// de F3: unos 16-23 ms en el escritorio). Lo llama la carga del nivel (GameScreen.Arrival.cs); en desarrollo escribe
        /// <c>AF_PERF slot_measure</c> si algo no salió de la caché. Sin ranura, nada.
        /// </summary>
        internal void WarmVictorySlot(int level)
        {
            if (!_slot.Reserved)
            {
                return;
            }

            FactLine? fruitLine = _fruitLine;
            int before = fruitLine != null ? fruitLine.NeedMeasureCount : 0;
            int measures = _slot.MeasureCount;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            _slot.Build(_victoryPanel);
            ChooseVictoryCompact(); // el escalón de la victoria compacta (D-032) mide aquí lo suyo, no en la victoria
            string locale = Loc.IsConfigured ? Loc.Locale : LocLocale.SourceLocale;
            float content = SlotContentDp(locale, UiBuilder.TextScale);
            double ms = watch.Elapsed.TotalMilliseconds;
            bool fresh = (fruitLine != null && fruitLine.NeedMeasureCount != before) || _slot.MeasureCount != measures;
            if (fresh)
            {
                SlotWarmMs = ms;
                _probe.SlotMeasure(level, locale, UiBuilder.TextScale, ms, fruitLine != null ? fruitLine.NeedMs : 0d, content);
            }
        }

        /// <summary>Milisegundos de la última vez que <see cref="WarmVictorySlot"/> midió algo (−1 si nunca; tests).</summary>
        internal double SlotWarmMs { get; private set; } = -1d;

        // El contexto del árbitro para la victoria de won con la política de ahora (los defaults del cliente o los de los tests).
        private VictoryContext ArbiterContext(int won, bool canSpeak)
        {
            Func<int, string?>? fruit = FruitKeyOf;
            VictoryContext context = VictoryLineArbiter.ContextFor(won, MapAvailable, RiverStops, fruit?.Invoke(won), canSpeak);
            if (HitoPolicyOverride.HasValue || RiverPolicyOverride.HasValue || FruitEveryNOverride.HasValue || RiverFactEveryNOverride.HasValue)
            {
                context = new VictoryContext(context.Won, context.MapAvailable, context.Stops, context.FruitKey,
                    RiverPolicyOverride ?? context.River, HitoPolicyOverride ?? context.Hito, context.SlothCanSpeak,
                    RiverFactEveryNOverride ?? context.RiverFactEveryN, FruitEveryNOverride ?? context.FruitEveryN);
            }

            return context;
        }

        // Lo que ocupa encima de la línea de fruta la placa del lugar cuando van juntas (ArrivalStrip.Stacked), o 0.
        private float PlaceAboveFruitDp()
        {
            ArrivalStrip? arrival = _arrival;
            return arrival != null && arrival.Stacked && arrival.Kind != ArrivalKind.None ? arrival.OffsetDp + arrival.ShownHeightDp : 0f;
        }

        // Alto fijo H de la línea de fruta (Q7-A; tarea F3): el del peor caso sintético de la ranura (M1.3b) o, si es mayor, el de
        // la línea más alta del calendario con su cabecera (FactLine.NeedDp: con el contador en dos filas, al 130 %, una línea real
        // puede pasar del sintético). Fijo por idioma, escala y ancho; de la caché.
        private float FruitBlockFor(string locale, float textScale, float width)
        {
            float fruit = _slot.ContentHeightFor(locale, textScale, width);
            FactLine? line = _fruitLine;
            return line != null && line.SlotActive ? Mathf.Max(fruit, line.NeedDp(textScale, width)) : fruit;
        }

        partial void SlotReserveBelowHome(ref float y)
        {
            _slotTopDp = y;
            if (!_slot.Reserved)
            {
                _slot.Collapse();
                return;
            }

            _slot.Build(_victoryPanel);
            string locale = Loc.IsConfigured ? Loc.Locale : LocLocale.SourceLocale;
            y += _slot.ReserveFor(SlotContentDp(locale, UiBuilder.TextScale));
        }

        partial void SlotLayout()
        {
            if (!_slot.Reserved)
            {
                _slot.Collapse();
                return;
            }

            string locale = Loc.IsConfigured ? Loc.Locale : LocLocale.SourceLocale;
            float content = SlotContentDp(locale, UiBuilder.TextScale);
            _slot.Layout(VictorySlotSideDp, _slotTopDp, VictorySlotWidthDp, content);
        }

        partial void SlotVictoryShown(LevelFlow flow)
        {
            int won = flow.LevelNumber;

            bool canSpeak = SlothCanSpeakFor(won, _slothCanSpeak, _slothSpokeAt);
            SlothCouldSpeak = canSpeak;
            _slothCanSpeak = false;
            if (won >= 1 && won <= WorldTable.LastLevel)
            {
                VictoryContext context = ArbiterContext(won, canSpeak);
                _victoryLine = VictoryLineArbiter.For(in context);
            }
            else
            {
                _victoryLine = default;
            }

            if (_victoryLine.SlothSpeaks)
            {
                _slothSpokeAt = won;
            }

            _riverFactSent = false;
            if (_arrival != null)
            {
                // La placa del lugar sin dato va arriba solo si la línea de fruta va de verdad debajo (ArrivalStrip.Stacked): el
                // árbitro la da y tiene sus textos en el idioma de la UI (en release, aprobados). Si no, la placa va centrada.
                _arrival.StackFruit = _fruitLine != null && _fruitLine.CanShow(in _victoryLine);
            }

            _slot.Show(flow, in _victoryLine);
            FactLine? fruitLine = _fruitLine;
            if (fruitLine != null && fruitLine.Shown)
            {
                FruitLineShownCount++;
                _probe.FruitLine(fruitLine, _slot.ContentDp);
            }

            SlothShowVictory(flow);
        }

        /// <summary>Veces que la línea de fruta salió en una victoria (la sonda <c>AF_STATE fruit_line</c>; tests).</summary>
        internal int FruitLineShownCount { get; private set; }

        // Alto fijo H de la línea de fruta en el idioma, la escala y el ancho de ahora (de la caché de la ranura): hasta ahí crece
        // el texto de la línea, que va arriba de la ranura si esta es más alta (la reserva es máx(línea de fruta, llegada), M1.6).
        private float FruitBlockDp()
        {
            _slot.Build(_victoryPanel);
            string locale = Loc.IsConfigured ? Loc.Locale : LocLocale.SourceLocale;
            return FruitBlockFor(locale, UiBuilder.TextScale, VictorySlotWidthDp);
        }

        partial void SlotTick(float dtMs)
        {
            if (!_victoryOverlay.gameObject.activeSelf)
            {
                _slot.Stop(); // fuera de la victoria, ni cuenta ni avisa a los proveedores
                return;
            }

            _slot.Tick(dtMs);
        }

        partial void SlotLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            _slot.LayoutIds(ids, rects);
        }

        // La victoria se cierra con Siguiente (AD_GATE) o con Inicio: aquí va el punto común de river_fact_shown
        // {level, fact_id, dismissed_ms} de M1.6 y F3, con VictorySlot.DismissedMs (la línea de fruta, desde que entra).
        partial void SlotAdGate(LevelFlow flow)
        {
            _sloth?.DismissBubble(); // el bocadillo se va con Siguiente (02 §6, fila TiTino)
            _slot.Dismiss();
            SendRiverFactShown(flow);
        }

        partial void SlotHomePressed(LevelFlow flow)
        {
            _sloth?.DismissBubble();
            _slot.Dismiss();
            SendRiverFactShown(flow);
        }

        // El punto común de river_fact_shown (08 §8.2, af_river_fact_shown; propuesta del Mapa §11.5; propuesta de Frutas §2.10):
        // una vez por victoria, si la ranura enseñó un dato, con su clave (fact_id) y dismissed_ms: el dato de la llegada o el
        // desplazado (ArrivalStrip.ShownFactKey), con el tiempo desde la victoria hasta cerrarla, o, si no, la línea de fruta
        // (FactLine.ShownFactId, <tinte>.<kk>), con el tiempo desde que entra (VictoryLineSlot.FruitRevealMs) hasta cerrarla, 0 si
        // se cerró antes (Frutas §2.10: «tiempo entre que aparece la línea y el toque en Siguiente»). El árbitro nunca da las dos
        // en la misma victoria (una sola línea, T6-A).
        private void SendRiverFactShown(LevelFlow flow)
        {
            if (_riverFactSent || !_slot.Shown)
            {
                return;
            }

            string? factId = _arrival?.ShownFactKey;
            float dismissedMs = _slot.DismissedMs;
            if (factId == null && _fruitLine != null && _fruitLine.Shown)
            {
                factId = _fruitLine.ShownFactId;
                dismissedMs = FruitDismissedMs(dismissedMs);
            }

            if (factId == null)
            {
                return;
            }

            _riverFactSent = true;
            RiverFactShownCount++;
            LastRiverFactId = factId;
            LastRiverFactDismissedMs = dismissedMs;
            _probe.RiverFactShown(flow.LevelNumber, factId, dismissedMs);
            UiAnalytics.RiverFactShown(Analytics, factId, dismissedMs); // af_river_fact_shown (B7)
        }

        /// <summary>
        /// <c>dismissed_ms</c> de la línea de fruta: de <paramref name="slotDismissedMs"/> (desde que se vio la victoria,
        /// <see cref="VictoryLineSlot.DismissedMs"/>) a desde que entra la línea (<see cref="VictoryLineSlot.FruitRevealMs"/>), y 0 si
        /// la victoria se cerró antes (propuesta de Frutas §2.10).
        /// </summary>
        internal static float FruitDismissedMs(float slotDismissedMs)
        {
            return Mathf.Max(0f, slotDismissedMs - VictoryLineSlot.FruitRevealMs);
        }

        /// <summary>
        /// <c>SlothCanSpeak</c> que se pasa al árbitro en la victoria de <paramref name="won"/> (arquitectura de la tanda 4, §2;
        /// T6-A): en un hito (<see cref="VictoryLineArbiter.IsSlothHito"/>), <paramref name="hitoBoxCanSpeak"/> (voz ∧ caja ≥
        /// <see cref="SlothSpeakBoxDp"/> dp, del gancho <c>SlothVictoryShown</c>); en el 10k+1, si TiTino habló en el hito
        /// anterior (<paramref name="spokeAt"/> = el nivel en que habló por última vez, o ≤ 0 si nunca): el árbitro es puro y
        /// no lo recuerda, y así el dato desplazado sale una vez y solo una; en el resto, false.
        /// </summary>
        internal static bool SlothCanSpeakFor(int won, bool hitoBoxCanSpeak, int spokeAt)
        {
            if (VictoryLineArbiter.IsSlothHito(won))
            {
                return hitoBoxCanSpeak;
            }

            return spokeAt > 0 && won == spokeAt + 1;
        }

        partial void SlothBoxBuild()
        {
            _sloth = new SlothVictory(_root, _art);
        }

        // SlothCanSpeak de la victoria de flow, antes de que decida el árbitro (arquitectura de la tanda 4, §2; T6-A): en un hito
        // de TiTino, con voz, con su caja de ≥ 104 dp y sin 3 filas, con el arte del rig (si no, no está en pantalla), con los
        // topes de la sesión (SlothLines.HitoVoice) y con su frase de hito enseñable y dentro del bocadillo. Si no, false: el
        // hito es una victoria normal y la llegada lleva su dato.
        partial void SlothVictoryShown(LevelFlow flow, float top, float bottom)
        {
            _slothPending = false;
            _slothViewHero = new Rect(0f, 0f, 0f, 0f);
            if (SlothViewOn)
            {
                // Victoria con vista (DC2): la caja va de la cabecera a la cinta sin el héroe del sitio que se verá, que aún no
                // está (StopVictoryShown cambia el fondo después).
                _slothViewHero = ViewHeroRectFor(flow.LevelNumber);
                VictoryFreeBand(out top, out bottom);
            }

            // La caja y el héroe con los que se decide la victoria (SlothCanSpeak y el árbitro); SlothViewBegun no los toca (tests).
            _slothDecidedHero = _slothViewHero;
            _slothDecidedTop = top;
            _slothDecidedBottom = bottom;
            _slothBandTop = top;
            _slothBandBottom = bottom;
            _slothHitoText = null;
            if (SlothBoxCanSpeakOverride.HasValue)
            {
                _slothCanSpeak = SlothBoxCanSpeakOverride.Value;
                return;
            }

            _slothCanSpeak = false;
            int won = flow.LevelNumber;
            SlothVictory? sloth = _sloth;
            if (sloth == null || !SlothEnabled || !SlothVoice || won < 1 || won > WorldTable.LastLevel || !VictoryLineArbiter.IsSlothHito(won)
                || SlothVictory.ModeFor(bottom - top, SlothRows) != SlothVictoryMode.Speak || !sloth.EnsureArt())
            {
                return;
            }

            SlothContext context = SlothVictoryContext(flow, bottom - top);
            if (!SlothLines.HitoVoice(won, in context))
            {
                return;
            }

            // La frase que diría en su hito con la línea suya (el árbitro aún no decidió): ¿tiene texto y cabe en el bocadillo?
            var hito = new VictoryLine(false, false, null, false, null, true, true);
            SlothLine line = SlothLines.ForVictory(won, in hito, in context);
            if (!UiText.TrySlothLine(in line, out string text) || !sloth.BubbleFits(text, top, bottom, _canvasW))
            {
                return;
            }

            _slothHitoText = text;
            _slothCanSpeak = true;
        }

        // Filas de cuias del nivel para la caja de TiTino (con 3, no sale: 07 §5.4, fila 0,60, y GDD §6.3, fila 53). En la victoria
        // con vista el tablero se va y la caja no depende de las filas: cuenta como una, así que con 3 filas TiTino sí sale si la
        // franja da [P; DC2: cambia la regla escrita de 07 §5.4, fila 0,60; DC1 la lleva a 07 §5.4 y a la fila 53].
        private int SlothRows => SlothViewOn ? 1 : _layout?.Rows ?? 1;

        // Contexto de SlothLines para la victoria de flow con una caja de boxDp: la sesión (con el ajuste de ahora) y los datos de
        // la victoria (SlothFactsFor).
        private SlothContext SlothVictoryContext(LevelFlow flow, float boxDp)
        {
            SlothSession session = SlothSession;
            session.Messages = _slothVoiceOverride ?? SlothMessagesSetting;
            LevelDefinition level = flow.Level;
            SlothVictoryFacts facts = SlothFactsFor(flow.LevelNumber, flow.LevelsWon, level.DifficultyLabel, level.IntroMechanic,
                flow.Attempt.Rescues > 0, boxDp >= SlothSpeakBoxDp, UiText.SlothColorFactApproved, flow.HintsUsed > 0);
            LastSlothFacts = facts;
            return session.Context.WithVictory(in facts);
        }

        /// <summary>
        /// Lo que TiTino sabe de la victoria del nivel <paramref name="won"/> (D-016, punto 5; propuesta de TiTino d.6, punto 2): fuera
        /// de sus hitos, frase en 1 de cada <c>ui_sloth_win_line_every_n</c> victorias si la ranura va vacía (sin línea de fruta, sin
        /// llegada ni dato desplazado; hoy, en 261-300 y 321-700, que no tienen serie de fruta), del banco más específico
        /// (<see cref="SlothLines.ForVictory"/>):
        /// <list type="bullet">
        /// <item>la cadencia cuenta con <paramref name="winCount"/>, <c>stats.levels_won</c> con esta victoria (desde TI6b lo suma
        /// cada victoria en su instante lógico: <see cref="LevelFlow.LevelsWon"/>), «la victoria número N, 2N, 3N…» (d.6, punto
        /// 2); <paramref name="won"/> es el nivel ganado (el estreno);</item>
        /// <item>Difícil o Muy difícil, por <c>difficulty.label</c> (<paramref name="difficultyLabel"/>);</item>
        /// <item>el estreno (<see cref="SlothDebutOf"/>);</item>
        /// <item>la Crecida (<c>streak.wins</c>) y su hito: 0 y no, porque el cliente aún no tiene la Crecida;</item>
        /// <item>la Cuia de rescate en este nivel (<paramref name="usedRescue"/>) y Morpho (<paramref name="usedHint"/>: alguna pista
        /// cobrada desde que se cargó el nivel, <see cref="LevelFlow.HintsUsed"/> &gt; 0; tarea FX8): con cualquiera de las dos,
        /// TiTino no dice las frases n.º 4 y 8 del banco de victoria, que presumen de haberlo resuelto solo (propuesta de TiTino
        /// e.4; <see cref="SlothLines.ForVictory"/>).</item>
        /// </list>
        /// </summary>
        internal static SlothVictoryFacts SlothFactsFor(int won, int winCount, string? difficultyLabel, string? introMechanic, bool usedRescue,
            bool speakBox, bool colorFactApproved, bool usedHint = false)
        {
            bool hard = string.Equals(difficultyLabel, LevelDefinition.LabelHard, StringComparison.Ordinal)
                || string.Equals(difficultyLabel, LevelDefinition.LabelVeryHard, StringComparison.Ordinal);
            return new SlothVictoryFacts(Math.Max(0, winCount), hard, SlothDebutOf(won, introMechanic), 0, false, usedRescue, usedHint, speakBox,
                colorFactApproved);
        }

        /// <summary>
        /// Estreno del nivel <paramref name="won"/> para el banco <c>debut</c> (propuesta de TiTino e.4, frases 26-28), en el orden
        /// de sus frases [P]: una mecánica (<c>intro_mechanic</c>, 02 §9.1), una fruta (la tarjeta «Fruta nueva»,
        /// <see cref="SightingRule.FruitDebutLevels"/>) o el primer nivel de un mundo (salvo el 1).
        /// </summary>
        internal static SlothDebut SlothDebutOf(int won, string? introMechanic)
        {
            if (!string.IsNullOrEmpty(introMechanic))
            {
                return SlothDebut.Mechanic;
            }

            if (Array.IndexOf(SightingRule.FruitDebutLevels, won) >= 0)
            {
                return SlothDebut.Fruit;
            }

            if (won > 1 && won <= WorldTable.LastLevel && WorldTable.FirstLevel(WorldTable.WorldOf(won)) == won)
            {
                return SlothDebut.World;
            }

            return SlothDebut.None;
        }

        // TiTino en la victoria de flow con la línea que dio el árbitro (al final de SlotVictoryShown): el gesto y, si la línea
        // es suya, el bocadillo; apunta lo enseñado en la sesión (también la victoria sin frase, que corta la racha).
        private void SlothShowVictory(LevelFlow flow)
        {
            SlothVictory? sloth = _sloth;
            LastSlothLine = default;
            int won = flow.LevelNumber;
            string? hitoText = _slothHitoText;
            _slothHitoText = null;
            if (sloth == null)
            {
                return;
            }

            if (!SlothEnabled || won < 1 || won > WorldTable.LastLevel)
            {
                sloth.Hide();
                return;
            }

            float box = _slothBandBottom - _slothBandTop;
            SlothContext context = SlothVictoryContext(flow, box);
            SlothLine line = SlothLines.ForVictory(won, in _victoryLine, in context);
            string? text = null;
            if (line.Speaks)
            {
                if (_victoryLine.SlothSpeaks && hitoText != null)
                {
                    text = hitoText;
                }
                else if (UiText.TrySlothLine(in line, out string other) && sloth.BubbleFits(other, _slothBandTop, _slothBandBottom, _canvasW))
                {
                    text = other;
                }
            }

            // Con la victoria con vista, TiTino entra cuando empieza la vista (cuando el tablero empieza a irse), un solo criterio
            // para todos los tableros [P; DC2]:
            //  - con 10 cuias o menos la ola de saltos ya acabó y la vista empieza al abrirse la victoria: TiTino entra en la fila
            //    0,80 de 07 §5.4 (ratificada en 54 a). Su rama se cruza con las cuias y el panel del tablero mientras se
            //    desvanecen (≤ 250 ms, 02 §6): solapamiento con el tablero que se va [P; DC2], para que DC1 lo lleve a 07 §5.4 y a
            //    la fila 53;
            //  - con 11-16 cuias, la ola sigue y StopVictoryShown espera a que acabe para llevarse el tablero: TiTino espera con
            //    él, nunca sobre las cuias a la vista (SlothTick), y su rama y su bocadillo llegan después de las filas 0,80 y 1,10.
            bool shown;
            bool bubble;
            if (SlothViewOn && CelebrationWaveEndMs(_views.Length) - _stopCelebrationMs > 0f)
            {
                // La ola de saltos aún en marcha (11-16 cuias): lo que enseñará ya está decidido; su caja no cambia hasta entonces.
                sloth.Hide();
                SlothVictoryMode mode = SlothVictory.ModeFor(box, SlothRows);
                shown = mode != SlothVictoryMode.None && line.Gesture != SlothGesture.None && sloth.EnsureArt();
                bubble = shown && text != null && mode == SlothVictoryMode.Speak;
                _slothPending = shown;
                _slothPendingGesture = line.Gesture;
                _slothPendingText = text;
                _slothPendingReduce = flow.ReduceMotion;
            }
            else
            {
                shown = sloth.Show(_slothBandTop, _slothBandBottom, SlothRows, line.Gesture, text, _canvasW, flow.ReduceMotion);
                bubble = sloth.HasBubble;
            }

            if (line.Speaks && (!shown || !bubble))
            {
                line = new SlothLine(line.Screen, line.Gesture, line.Moment, SlothData.NoLine, null, 0, false); // sin frase
            }

            LastSlothLine = line;
            SlothSession.Record(in line, in context);
        }

        partial void SlothTick(float dtMs)
        {
            SlothVictory? sloth = _sloth;
            if (sloth == null)
            {
                return;
            }

            if (_slothPending)
            {
                // TiTino espera a que empiece la victoria con vista (la ola de saltos de 11-16 cuias; SlothShowVictory).
                LevelFlow? waiting = _flow;
                if (!_victoryOverlay.gameObject.activeSelf || waiting == null
                    || (waiting.Phase != FlowPhase.VictoryScreen && waiting.Phase != FlowPhase.AdGate))
                {
                    _slothPending = false;
                }
                else if (_victoryViewWaitMs < 0f)
                {
                    _slothPending = false;
                    SlothViewBegun();
                    VictoryFreeBand(out float from, out float to);
                    _slothBandTop = from;
                    _slothBandBottom = to;
                    sloth.Show(from, to, SlothRows, _slothPendingGesture, _slothPendingText, _canvasW, _slothPendingReduce);
                    _slothPendingText = null;
                }
            }

            if (sloth.ClockMs < 0f)
            {
                FadeHeron(false, dtMs); // TiTino se fue a mitad de la victoria (la caja ya no le da): la garza vuelve
                return;
            }

            // Red de seguridad de SlothLeave: fuera de la victoria (sin su overlay o con el flujo ya en otra fase), TiTino se va.
            LevelFlow? flow = _flow;
            if (!_victoryOverlay.gameObject.activeSelf || flow == null
                || (flow.Phase != FlowPhase.VictoryScreen && flow.Phase != FlowPhase.AdGate))
            {
                sloth.Hide();
                SetHeronAlpha(1f);
                return;
            }

            // Si la caja cambió (la Cosecha doble recoloca el panel y el tablero, o la pantalla cambió), TiTino se recoloca, se
            // encoge o se va: nunca se cruza con las cuias, la cinta ni el héroe del sitio.
            SlothViewBegun();
            VictoryFreeBand(out float top, out float bottom);
            _slothBandTop = top;
            _slothBandBottom = bottom;
            sloth.Relayout(top, bottom, SlothRows, _canvasW);
            sloth.Tick(dtMs);
            FadeHeron(sloth.Visible, dtMs);
        }

        // La victoria se cierra por cualquier camino (HideOverlays o la pantalla de nivel que se oculta): TiTino se va y la garza
        // vuelve a su sitio.
        partial void SlothLeave()
        {
            _slothPending = false;
            _slothPendingText = null;
            _sloth?.Hide();
            SetHeronAlpha(1f);
        }

        /// <summary>
        /// Fundido de la garza blanca del fondo mientras TiTino está a la vista en la victoria (ms) [P; revisión de TI6a]: la garza
        /// de la orilla izquierda cae detrás de TiTino o bajo sus pies y, con el mismo valor claro, las dos siluetas se funden.
        /// Se va a alfa 0 en este tiempo desde que TiTino sale y vuelve cuando se va (de golpe al cerrarse la victoria).
        /// </summary>
        internal const float HeronFadeMs = 200f;

        /// <summary>Opacidad de la garza del fondo (tests).</summary>
        internal float HeronAlpha => _heron.color.a;

        // La garza hacia alfa 0 (away) o 1 en HeronFadeMs. Sin asignaciones.
        private void FadeHeron(bool away, float dtMs)
        {
            float target = away ? 0f : 1f;
            Color c = _heron.color;
            if (c.a == target)
            {
                return;
            }

            c.a = Mathf.MoveTowards(c.a, target, Mathf.Max(0f, dtMs) / HeronFadeMs);
            _heron.color = c;
        }

        private void SetHeronAlpha(float alpha)
        {
            Color c = _heron.color;
            if (c.a != alpha)
            {
                c.a = alpha;
                _heron.color = c;
            }
        }

        // Caja de TiTino en la victoria (propuesta de TiTino d.4; gancho SlothVictoryShown, que OnVictoryScreen llama con esta
        // franja), en dp desde arriba. Sin tablero, vacía.
        //  - Con la victoria con vista (ui_victory_view; DC2, pregunta 53 a de D-028): la franja del pie de la cabecera (la
        //    insignia, lo más bajo del HUD) a la cima de la cinta, menos SlothRibbonGapDp, sin el héroe del sitio del fondo
        //    (SlothViewBand).
        //  - Sin ella (la clave apagada), como en TI6a: la libre entre el pie de la cabecera y lo más alto de las cuias con su
        //    elevación, con el tablero ya en el sitio al que lo lleva ShiftToClear.
        private void VictoryFreeBand(out float top, out float bottom)
        {
            top = HudBadgeBottom;
            bottom = top;
            if (_layout == null || _views.Length == 0)
            {
                return;
            }

            if (SlothViewOn)
            {
                SlothViewBand(top, _victoryPanelY - RibbonRiseDp - SlothRibbonGapDp, _slothViewHero, out top, out bottom);
                return;
            }

            bottom = Mathf.Max(top, CuiasTopDp() - _boardShiftTarget);
        }

        /// <summary>
        /// Aire entre la caja de TiTino y la cima de la cinta en la victoria con vista (dp) [P; DC2]: la cinta entra de 0,6 a 1,08 y
        /// a 1,0 (ApplyVictoryAnim), y a 1,08 su canto de arriba sube un 4 % de su alto (≤ 3,6 dp con la hoja más alta).
        /// </summary>
        internal const float SlothRibbonGapDp = 8f;

        // La victoria con vista está encendida (ui_victory_view, 08 §9; la misma condición que StopVictoryShown): TiTino usa la
        // franja de la cabecera a la cinta (DC2).
        private static bool SlothViewOn => VictoryViewOverride ?? MapConfig.UiVictoryView;

        /// <summary>
        /// Caja de TiTino en la victoria con vista (DC2; pregunta 53 a de D-028): la franja de <paramref name="bandTop"/> (pie de la
        /// cabecera) a <paramref name="bandBottom"/> (cima de la cinta con su aire) sin el héroe del sitio del fondo
        /// (<paramref name="hero"/>, en dp del lienzo; vacío sin héroe: la franja entera). El héroe ocupa casi todo el ancho por el
        /// centro (240 × 80 dp a 360 de ancho) y TiTino, con su bocadillo, todo el ancho desde la izquierda, así que la caja es el
        /// trozo de la franja que queda por encima del héroe o el que queda por debajo, el más alto (a igual alto, el de abajo).
        /// </summary>
        internal static void SlothViewBand(float bandTop, float bandBottom, Rect hero, out float top, out float bottom)
        {
            top = bandTop;
            bottom = Mathf.Max(bandTop, bandBottom);
            if (hero.width <= 0f || hero.height <= 0f || hero.yMax <= top || hero.yMin >= bottom)
            {
                return;
            }

            float above = hero.yMin - top;
            float below = bottom - hero.yMax;
            if (below >= above)
            {
                top = Mathf.Min(bottom, hero.yMax);
            }
            else
            {
                bottom = Mathf.Max(top, hero.yMin);
            }
        }

        // Héroe del sitio que se verá en la victoria con vista de won (dp del lienzo; vacío sin héroe), para la caja de TiTino, que
        // se decide antes de que StopVictoryShown cambie el fondo: sin llegada, el del fondo de ahora; en una llegada, el de la
        // parada que trae el nivel siguiente (la misma regla que StopVictoryShown y StopBackdrop.ResolveHero), en el sitio fijo del
        // héroe (StopBackdrop.BuildHero). Ya empezada la vista, el del fondo (SlothTick). Solo al decidir la victoria.
        private Rect ViewHeroRectFor(int won)
        {
            RiverStopCatalog? stops = RiverStops;
            bool arrival = stops != null && won != RiverStopCatalog.LevelsPerStop && stops.ArrivalOnWin(won) != null;
            if (!arrival)
            {
                return _stopBackdrop.HeroRect;
            }

            if (!StopHasHero(StopForLevel(stops, won + 1, MapConfig.UiBgPerStop)))
            {
                return new Rect(0f, 0f, 0f, 0f);
            }

            float k = _canvasW / 360f;
            float hw = StopBackdrop.HeroWidthDp * k;
            return new Rect((_canvasW * 0.5f) - (hw * 0.5f), _stopHorizon - (StopBackdrop.HeroAboveDp * k), hw, StopBackdrop.HeroHeightDp * k);
        }

        // Empezada la victoria con vista, el héroe es el del fondo que se ve (el que predijo ViewHeroRectFor). Sin asignaciones.
        private void SlothViewBegun()
        {
            if (_victoryViewOn && _victoryViewWaitMs < 0f)
            {
                _slothViewHero = _stopBackdrop.HeroRect;
            }
        }

        // El fondo de la parada pinta héroe (StopBackdrop.ResolveHero): su PNG si existe o su silueta si la regla del arte la deja.
        private static bool StopHasHero(RiverStop? stop)
        {
            if (stop == null)
            {
                return false;
            }

            string? hero = stop.Backdrop.Hero;
            if (hero != null)
            {
                Func<string, Sprite?>? find = StopBackdrop.HeroSpriteOverride;
                if ((find != null ? find(hero) : ArtSlots.Find(hero)) != null)
                {
                    return true;
                }
            }

            return StopShapes.ShapeOf(stop.Backdrop) != null && MapArtRule.CanShow(MapArtRule.ShowProvisional, false);
        }

        /// <summary>Héroe del sitio que TiTino no tapa en la victoria con vista (dp del lienzo; vacío sin héroe; tests).</summary>
        internal Rect SlothViewHeroRect => _slothViewHero;

        /// <summary>
        /// Héroe del sitio con el que se decidió la última victoria (<c>SlothVictoryShown</c>, antes de que el fondo cambie a la parada
        /// de la llegada; <see cref="ViewHeroRectFor"/>), que SlothViewBegun no toca (tests: el que se ve tiene que ser este).
        /// </summary>
        internal Rect SlothDecidedHeroRect => _slothDecidedHero;

        /// <summary>Canto de arriba de la caja con la que se decidió la última victoria (dp desde arriba; tests).</summary>
        internal float SlothDecidedBandTopDp => _slothDecidedTop;

        /// <summary>Canto de abajo de la caja con la que se decidió la última victoria (dp desde arriba; tests).</summary>
        internal float SlothDecidedBandBottomDp => _slothDecidedBottom;

        /// <summary>Pie de la cabecera (la insignia, lo más bajo del HUD; dp desde arriba; tests).</summary>
        internal float SlothHeaderBottomDp => HudBadgeBottom;

        // Lo más alto de las cuias con su elevación, con el tablero sin desplazar (dp desde arriba): la misma cima que usa
        // ShiftToClear (GameScreen.cs), que hoy la calcula por su cuenta; que la use también es cosa de la dueña de GameScreen.cs.
        private float CuiasTopDp()
        {
            float highestTop = float.MaxValue;
            for (int i = 0; i < _views.Length; i++)
            {
                highestTop = Mathf.Min(highestTop, _layout!.FootY[i] - (_views[i].HeightU * _views[i].Unit) - CuiaView.LiftDp);
            }

            return highestTop;
        }

        private readonly struct StackEntry
        {
            public StackEntry(RiverStopCatalog stops, Func<int, string?>? keyOf, string locale, RiverFactPolicy river, int riverEvery, int fruitEvery,
                float scale, float width, float fruit, float height)
            {
                Stops = stops;
                KeyOf = keyOf;
                Locale = locale;
                River = river;
                RiverEvery = riverEvery;
                FruitEvery = fruitEvery;
                Scale = scale;
                Width = width;
                Fruit = fruit;
                Height = height;
            }

            public RiverStopCatalog Stops { get; }

            public Func<int, string?>? KeyOf { get; }

            public string Locale { get; }

            public RiverFactPolicy River { get; }

            public int RiverEvery { get; }

            public int FruitEvery { get; }

            public float Scale { get; }

            public float Width { get; }

            public float Fruit { get; }

            public float Height { get; }
        }
    }
}
