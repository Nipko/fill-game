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
    /// <item>TiTino (tarea TI6a; <see cref="SlothVictory"/>): sale en su caja si cabe y hace su gesto; en sus hitos puede hablar
    /// (<c>SlothCanSpeak</c>, que se calcula antes de que el árbitro decida) si <see cref="SlothVoice"/> (<c>ui_sloth_enabled</c>
    /// ∧ <c>settings.sloth_messages</c>), su caja mide ≥ <see cref="SlothSpeakBoxDp"/> dp, está en pantalla, los topes de la
    /// sesión lo dejan (<see cref="SlothLines.HitoVoice"/>) y su frase tiene texto que se pueda enseñar y cabe en el bocadillo.
    /// Con <see cref="HitoLinePolicy.SlothHito"/> (el default desde TI6a, T6-A) la línea es entonces suya.</item>
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

        /// <summary>Pie de la cabecera en la última victoria: canto de arriba de la caja de TiTino (dp desde arriba).</summary>
        internal float SlothBandTopDp => _slothBandTop;

        /// <summary>Cima de las cuias (con su elevación) tras ShiftToClear en la última victoria: canto de abajo de la caja.</summary>
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
        internal float VictorySlotWidthDp => Mathf.Min(328f, _canvasW - 32f) - (2f * VictoryLineSlot.SideDp);

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
            y += VictoryLineSlot.ReserveOf(SlotContentDp(locale, UiBuilder.TextScale));
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
            _slot.Layout(VictoryLineSlot.SideDp, _slotTopDp, VictorySlotWidthDp, content);
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

            SlothContext context = SlothVictoryContext(won, bottom - top);
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

        // Filas de cuias del nivel (con 3, TiTino no sale: 07 §5.1).
        private int SlothRows => _layout?.Rows ?? 1;

        // Contexto de SlothLines para la victoria de won con una caja de boxDp: la sesión (con el ajuste de ahora) y los datos de
        // la victoria. La cadencia de 1 de cada N necesita stats.levels_won, que la pantalla no tiene: con 0, fuera de sus hitos
        // TiTino solo hace el gesto, que es lo que pasa de todos modos con la línea de fruta en cada victoria (02 §6, fila
        // TiTino; D-017) [P].
        private SlothContext SlothVictoryContext(int won, float boxDp)
        {
            SlothSession session = SlothSession;
            session.Messages = _slothVoiceOverride ?? SlothMessagesSetting;
            var facts = new SlothVictoryFacts(0, false, SlothDebut.None, 0, false, false, false, boxDp >= SlothSpeakBoxDp,
                UiText.SlothColorFactApproved);
            return session.Context.WithVictory(in facts);
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
            SlothContext context = SlothVictoryContext(won, box);
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

            bool shown = sloth.Show(_slothBandTop, _slothBandBottom, SlothRows, line.Gesture, text, _canvasW, flow.ReduceMotion);
            if (line.Speaks && (!shown || !sloth.HasBubble))
            {
                line = new SlothLine(line.Screen, line.Gesture, line.Moment, SlothData.NoLine, null, 0, false); // sin frase
            }

            LastSlothLine = line;
            SlothSession.Record(in line, in context);
        }

        partial void SlothTick(float dtMs)
        {
            SlothVictory? sloth = _sloth;
            if (sloth == null || sloth.ClockMs < 0f)
            {
                return;
            }

            // Red de seguridad de SlothLeave: fuera de la victoria (sin su overlay o con el flujo ya en otra fase), TiTino se va.
            LevelFlow? flow = _flow;
            if (!_victoryOverlay.gameObject.activeSelf || flow == null
                || (flow.Phase != FlowPhase.VictoryScreen && flow.Phase != FlowPhase.AdGate))
            {
                sloth.Hide();
                return;
            }

            // Si la caja cambió (la Cosecha doble recoloca el panel y el tablero, o la pantalla cambió), TiTino se recoloca, se
            // encoge o se va: nunca se cruza con las cuias.
            VictoryFreeBand(out float top, out float bottom);
            _slothBandTop = top;
            _slothBandBottom = bottom;
            sloth.Relayout(top, bottom, SlothRows, _canvasW);
            sloth.Tick(dtMs);
        }

        // La victoria se cierra por cualquier camino (HideOverlays o la pantalla de nivel que se oculta): TiTino se va.
        partial void SlothLeave()
        {
            _sloth?.Hide();
        }

        // Caja de TiTino en la victoria (propuesta de TiTino d.4; gancho SlothVictoryShown, que OnVictoryScreen llama con esta
        // franja): la libre entre el pie de la cabecera (la insignia, lo más bajo del HUD) y lo más alto de las cuias con su
        // elevación, con el tablero ya en el sitio al que lo lleva ShiftToClear (dp desde arriba). Sin tablero, vacía.
        private void VictoryFreeBand(out float top, out float bottom)
        {
            top = HudBadgeBottom;
            bottom = top;
            if (_layout == null || _views.Length == 0)
            {
                return;
            }

            bottom = Mathf.Max(top, CuiasTopDp() - _boardShiftTarget);
        }

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
