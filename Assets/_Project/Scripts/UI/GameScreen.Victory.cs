#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
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
    /// <item>«Siguiente» no cambia de tiempo: se habilita en el mismo milisegundo con y sin ranura, y a los 1,40 s está entero
    /// (VA-13); el contenido de la ranura aparece entonces (<see cref="VictoryLineSlot.RevealMs"/>).</item>
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

        private readonly VictoryLineSlot _slot = new VictoryLineSlot();
        private VictoryLine _victoryLine;
        private float _slotTopDp;
        private float _slothBandTop;
        private float _slothBandBottom;
        private bool _slothCanSpeak;
        private int _slothSpokeAt = -1;

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
            y += _slot.ReserveFor(locale, UiBuilder.TextScale, VictorySlotWidthDp);
        }

        partial void SlotLayout()
        {
            if (!_slot.Reserved)
            {
                _slot.Collapse();
                return;
            }

            string locale = Loc.IsConfigured ? Loc.Locale : LocLocale.SourceLocale;
            float content = _slot.ContentHeightFor(locale, UiBuilder.TextScale, VictorySlotWidthDp);
            _slot.Layout(VictoryLineSlot.SideDp, _slotTopDp, VictorySlotWidthDp, content);
        }

        partial void SlotVictoryShown(LevelFlow flow)
        {
            int won = flow.LevelNumber;

            // SlothCanSpeak: en un hito, el de la caja de ahora; en el 10k+1, si TiTino habló en el hito (el árbitro es puro y no
            // lo recuerda: así el dato desplazado sale una vez y solo una).
            bool canSpeak = VictoryLineArbiter.IsSlothHito(won) ? _slothCanSpeak : _slothSpokeAt > 0 && won == _slothSpokeAt + 1;
            SlothCouldSpeak = canSpeak;
            _slothCanSpeak = false;
            if (won >= 1 && won <= WorldTable.LastLevel)
            {
                Func<int, string?>? fruit = FruitKeyOf;
                VictoryContext context = VictoryLineArbiter.ContextFor(won, MapAvailable, RiverStops, fruit?.Invoke(won), canSpeak);
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

            _slot.Show(flow, in _victoryLine, flow.ReduceMotion);
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
        }

        partial void SlotHomePressed(LevelFlow flow)
        {
            _slot.Dismiss();
        }

        partial void SlothVictoryShown(LevelFlow flow, float top, float bottom)
        {
            _slothBandTop = top;
            _slothBandBottom = bottom;
            _slothCanSpeak = SlothVoice && bottom - top >= SlothSpeakBoxDp;
        }
    }
}
