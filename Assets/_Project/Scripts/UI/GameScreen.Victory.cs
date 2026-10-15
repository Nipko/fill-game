#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
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
    /// <item>TiTino puede hablar si <see cref="SlothVoice"/> (<c>ui_sloth_enabled</c> ∧ <c>settings.sloth_messages</c>; lo
    /// enciende TI6a cuando TiTino exista en la victoria) y su caja mide ≥ <see cref="SlothSpeakBoxDp"/> dp. Solo cuenta con
    /// <see cref="HitoLinePolicy.SlothHito"/>, que también llega con TI6a.</item>
    /// </list>
    /// </summary>
    public sealed partial class GameScreen
    {
        /// <summary>
        /// Caja de TiTino con la que habla: gesto a 72 dp y bocadillo de hasta 3 líneas al 130 % (propuesta de TiTino d.4)
        /// [P, a confirmar con las capturas de TI6a].
        /// </summary>
        internal const float SlothSpeakBoxDp = 104f;

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
        private bool _riverFactSent;

        /// <summary>La ranura del Dato del río (tests y proveedores).</summary>
        internal VictoryLineSlot VictorySlot => _slot;

        /// <summary>La línea que dio el árbitro en la última victoria (default antes de la primera).</summary>
        internal VictoryLine CurrentVictoryLine => _victoryLine;

        /// <summary>
        /// TiTino tiene voz: <c>ui_sloth_enabled</c> ∧ <c>settings.sloth_messages</c> (D-016 punto 4; propuesta de TiTino d.4).
        /// false hasta que TI6a lo pinte en la victoria y lo encienda; sin voz, nunca habla.
        /// </summary>
        internal bool SlothVoice { get; set; }

        /// <summary>
        /// El Mapa del río existe en el cliente (en el 10 cuenta él la primera llegada, 06 §2.3): false hasta M3.3, que lo
        /// enciende.
        /// </summary>
        internal bool MapAvailable { get; set; }

        /// <summary>
        /// Clave de la línea de fruta del nivel (D-017; 08 §4.6), o null: la da el calendario de Frutas del río (F1F2 y F3).
        /// Sin él, ningún nivel tiene línea de fruta.
        /// </summary>
        internal Func<int, string?>? FruitKeyOf { get; set; }

        /// <summary>
        /// Política de los hitos de TiTino en lugar de <see cref="MapConfig.HitoPolicy"/> (tests: SlothHito antes de TI6a); null =
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
        // victoria a otra (Q7-A).
        private float SlotContentDp(string locale, float textScale)
        {
            float width = VictorySlotWidthDp;
            float fruit = _slot.ContentHeightFor(locale, textScale, width);
            ArrivalStrip? arrival = _arrival;
            return arrival != null && arrival.SlotActive ? Mathf.Max(fruit, arrival.NeedDp(textScale, width)) : fruit;
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
                Func<int, string?>? fruit = FruitKeyOf;
                VictoryContext context = VictoryLineArbiter.ContextFor(won, MapAvailable, RiverStops, fruit?.Invoke(won), canSpeak);
                if (HitoPolicyOverride.HasValue || RiverPolicyOverride.HasValue)
                {
                    context = new VictoryContext(context.Won, context.MapAvailable, context.Stops, context.FruitKey,
                        RiverPolicyOverride ?? context.River, HitoPolicyOverride ?? context.Hito, context.SlothCanSpeak,
                        context.RiverFactEveryN, context.FruitEveryN);
                }

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
            _slot.Show(flow, in _victoryLine);
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
        // {level, fact_id, dismissed_ms} de M1.6 y F3, con VictorySlot.DismissedMs.
        partial void SlotAdGate(LevelFlow flow)
        {
            _slot.Dismiss();
            SendRiverFactShown(flow);
        }

        partial void SlotHomePressed(LevelFlow flow)
        {
            _slot.Dismiss();
            SendRiverFactShown(flow);
        }

        // El punto común de river_fact_shown (08 §8.2, af_river_fact_shown; propuesta del Mapa §11.5): una vez por victoria, si la
        // ranura enseñó un dato, con su clave (fact_id) y el tiempo desde la victoria hasta cerrarla (dismissed_ms). Hoy, el dato
        // de la llegada o el desplazado (ArrivalStrip.ShownFactKey); F3 añade aquí la clave de la línea de fruta.
        private void SendRiverFactShown(LevelFlow flow)
        {
            if (_riverFactSent || !_slot.Shown)
            {
                return;
            }

            string? factId = _arrival?.ShownFactKey;
            if (factId == null)
            {
                return;
            }

            _riverFactSent = true;
            RiverFactShownCount++;
            LastRiverFactId = factId;
            LastRiverFactDismissedMs = _slot.DismissedMs;
            _probe.RiverFactShown(flow.LevelNumber, factId, _slot.DismissedMs);
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

        partial void SlothVictoryShown(LevelFlow flow, float top, float bottom)
        {
            _slothBandTop = top;
            _slothBandBottom = bottom;
            _slothCanSpeak = SlothBoxCanSpeakOverride ?? (SlothVoice && bottom - top >= SlothSpeakBoxDp);
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
    }
}
