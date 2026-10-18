#nullable enable
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using TMPro;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Parcial de la llegada a la parada del Mapa del río en la victoria del <c>10k</c> (02 §6, fila Llegada; D-013 punto 8; T6-A;
    /// propuesta del Mapa §4.2 y §8.2; tarea M1.6): crea la franja (<see cref="ArrivalStrip"/>), la registra como proveedor de
    /// la ranura del Dato del río (GameScreen.Victory.cs) y escribe sus sondas de desarrollo (DevProbe.Map.cs):
    /// <list type="bullet">
    /// <item><c>AF_STATE arrival</c> cuando empieza la franja (<c>phase</c> = <c>start</c>, tras la subida del panel o con el toque
    /// que la adelanta) y al terminar (<c>complete</c>, con <c>by</c> = <c>time</c> o <c>tap</c>, o <c>close</c> si la victoria se
    /// cierra antes; docs/dev/prototipo-v0.md);</item>
    /// <item><c>AF_STATE stop_arrive</c>, el <c>af_stop_arrive</c> de 08 §8.2 (<c>stop_id</c> = <c>s{nn}</c>, <c>world</c>,
    /// <c>level</c>, <c>view</c> = <c>victory</c> y <c>skipped</c>), una vez por llegada;</item>
    /// <item><c>AF_LAYOUT arrival</c> cuando empieza la franja, con el área táctil <c>arrival</c> (un toque la completa).</item>
    /// </list>
    /// Al acabar la franja de una llegada (por tiempo, con un toque o porque la victoria se cierra antes), el jugador ya vio el
    /// Mapa hasta ese nivel: <c>progress.map_seen_level</c> pasa al <c>10k</c> (<see cref="IGameActions.MarkMapSeen"/>; propuesta
    /// del Mapa §4.2; M3.5), así que al abrir el Mapa no se repite esa llegada al ponerse al día y la pestaña del inicio no lleva
    /// el punto. El dato desplazado no es una llegada y no anota nada.
    /// <c>river_fact_shown</c> sale del punto común de la ranura (GameScreen.Victory.cs), compartido con la línea de fruta (F3).
    /// <para><b>«Ver mapa»</b> (<c>map_open</c>, P-M2; 02 §6; D-013, punto 7; propuesta del Mapa §8.1 y §8.2; tarea M4.3): con una
    /// llegada a la vista (la franja con su parada: <see cref="ArrivalKind.Arrival"/> o <see cref="ArrivalKind.Place"/>, nunca el
    /// dato desplazado) y el Mapa en la escena (<see cref="RiverMapOpenable"/>), la fila de «Inicio» se parte en dos botones de
    /// madera del mismo ancho, «Inicio» y «Ver mapa», justo encima de la franja, como en el esquema de la victoria del
    /// <c>10k</c> de la propuesta (§8.2) [P]: la ranura no cambia de alto y «Siguiente» sigue en su sitio y a su tiempo. Entra con
    /// «Inicio» (la misma opacidad, a los <see cref="VictoryButtonsInMs"/> ms) y, como él, deja de responder en AD_GATE y mientras
    /// la victoria espera a avanzar sola tras la Cosecha doble. Un toque completa la franja (su <c>stop_arrive</c> y lo visto del
    /// Mapa hasta el <c>10k</c>, así que el Mapa no repite la llegada) y abre el Mapa encima de la victoria
    /// (<see cref="MapOpenSource.Victory"/>); al cerrarlo, la victoria sigue tal cual, y «Siguiente» y AD_GATE, igual. En
    /// <c>AF_LAYOUT victory</c>, el id <c>victory_map</c>.</para>
    /// </summary>
    public sealed partial class GameScreen
    {
        private ArrivalStrip? _arrival;
        private bool _stopArriveSent;
        private readonly List<string> _arrivalIds = new List<string>(1);
        private readonly List<RectTransform> _arrivalRects = new List<RectTransform>(1);
        private ReliefParts _victoryMap;
        private CanvasGroup? _victoryMapGroup;
        private bool _victoryMapShown;
        private bool _victoryMapBuilt;

        /// <summary>Hueco entre «Inicio» y «Ver mapa» cuando comparten la fila (dp) [P].</summary>
        internal const float VictoryMapGapDp = 8f;

        /// <summary>Cuerpo de «Ver mapa» en la victoria (sp), el de «Inicio».</summary>
        internal const float VictoryMapSp = 18f;

        /// <summary>
        /// Sin la llegada en las pantallas que se construyan mientras valga true (tests de la ranura sola, con proveedores de
        /// prueba; como <see cref="SimulatedScreen"/>). No se usa en el juego.
        /// </summary>
        internal static bool ArrivalDisabledForTests { get; set; }

        /// <summary>La franja de llegada (null solo con <see cref="ArrivalDisabledForTests"/>).</summary>
        internal ArrivalStrip? Arrival => _arrival;

        /// <summary>Veces que se escribió <c>stop_arrive</c> (tests).</summary>
        internal int StopArriveCount { get; private set; }

        /// <summary>El <c>skipped</c> del último <c>stop_arrive</c>.</summary>
        internal bool LastStopArriveSkipped { get; private set; }

        /// <summary>Veces que se escribió <c>AF_LAYOUT arrival</c> (tests).</summary>
        internal int ArrivalLayoutCount { get; private set; }

        /// <summary>«Ver mapa» de la victoria del <c>10k</c> (tarea M4.3).</summary>
        internal UnityEngine.UI.Button VictoryMapButton => _victoryMap.Button;

        /// <summary>«Ver mapa» está en la victoria de ahora (con una llegada y el Mapa en la escena).</summary>
        internal bool VictoryMapShown => _victoryMapShown;

        /// <summary>Opacidad de «Ver mapa» (la de «Inicio»).</summary>
        internal float VictoryMapAlpha => _victoryMapGroup != null ? _victoryMapGroup.alpha : 0f;

        /// <summary>Veces que «Ver mapa» abrió el Mapa (tests).</summary>
        internal int VictoryMapOpened { get; private set; }

        partial void ArrivalBuild()
        {
            if (ArrivalDisabledForTests)
            {
                return;
            }

            _arrival = new ArrivalStrip(_art, _victoryPanel, () => RiverStops, OnArrivalStarted, OnArrivalCompleted);
            RegisterVictoryLineProvider(_arrival);

            // «Ver mapa» (M4.3): el secundario de madera de «Inicio», con su misma entrada; oculto salvo con una llegada.
            _victoryMap = UiBuilder.Relief("VictoryMapButton", _victoryPanel, _art.CtaWood, OnVictoryMapPressed);
            _victoryMapGroup = _victoryMap.Face.parent.gameObject.AddComponent<CanvasGroup>();
            _victoryMapGroup.blocksRaycasts = false;
            _victoryMapGroup.alpha = 0f;
            TextMeshProUGUI label = UiBuilder.Label("Label", _victoryMap.Face, TextStyle.PillWood, UiText.MapOpen, VictoryMapSp, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(label.rectTransform, _art.CtaWood);
            UiBuilder.AutoSize(label, UiBuilder.MinTextSp, VictoryMapSp);
            _victoryMap.Relief.Bind(_victoryMap.Face, _victoryMap.Base, _victoryMap.FaceImage, _art.CtaWood, null, null, label);
            _victoryMap.Button.gameObject.SetActive(false);
            _victoryMapBuilt = true;
        }

        partial void ArrivalLevelLoaded(LevelFlow flow)
        {
            _stopArriveSent = false;
            ShowVictoryMap(false);
            // De F3 (la línea de fruta), no de la llegada: calienta las medidas de la ranura del Dato del río al cargar el nivel, para
            // que la primera victoria no las pague (~20 ms; FactLineTests.Measures_AreMadeWhenTheLevelLoads_NotAtTheVictory). No la
            // quites al rehacer este parcial: llévala al gancho propio de la ranura (SlotLevelLoaded) cuando GameScreen.cs lo declare.
            WarmVictorySlot(flow.LevelNumber);
        }

        partial void ArrivalVictoryShown(LevelFlow flow)
        {
            _stopArriveSent = false;
            ArrivalStrip? strip = _arrival;
            bool arrival = strip != null && strip.Stop != null && (strip.Kind == ArrivalKind.Arrival || strip.Kind == ArrivalKind.Place);
            ShowVictoryMap(arrival && RiverMapOpenable);
            LayoutVictoryMap();
        }

        partial void ArrivalTick(float dtMs)
        {
            // «Ver mapa» entra y sale con «Inicio» (ApplyVictoryAnim): la misma opacidad, sin asignaciones.
            CanvasGroup? group = _victoryMapGroup;
            if (group != null && _victoryMapShown && group.alpha != _homeGroup.alpha)
            {
                group.alpha = _homeGroup.alpha;
            }
        }

        partial void ArrivalLayoutVictory()
        {
            LayoutVictoryMap();
        }

        partial void ArrivalLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            if (_victoryMapShown)
            {
                ids.Add("victory_map");
                rects.Add((RectTransform)_victoryMap.Button.transform);
            }
        }

        partial void ArrivalAdGate(LevelFlow flow)
        {
            CloseArrival();
            if (_victoryMapShown)
            {
                UiBuilder.SetEnabled(_victoryMap.Button, false); // como «Inicio»: la victoria ya se cierra
            }
        }

        partial void ArrivalHomePressed(LevelFlow flow)
        {
            CloseArrival();
        }

        // «Ver mapa» se ve o no (con la llegada de esta victoria); oculto, «Inicio» vuelve a su sitio en el siguiente LayoutVictory.
        private void ShowVictoryMap(bool show)
        {
            if (!_victoryMapBuilt)
            {
                return; // sin la llegada (ArrivalDisabledForTests), sin «Ver mapa»
            }

            _victoryMapShown = show;
            if (_victoryMap.Button.gameObject.activeSelf != show)
            {
                _victoryMap.Button.gameObject.SetActive(show);
            }

            if (_victoryMapGroup != null)
            {
                _victoryMapGroup.alpha = show ? _homeGroup.alpha : 0f;
            }
        }

        // Con «Ver mapa», la fila de «Inicio» se parte en dos botones del mismo ancho que ocupan juntos el ancho de «Siguiente»
        // (Inicio a la izquierda); sin él, LayoutVictory ya dejó «Inicio» centrado. Responde como «Inicio» (inactivo mientras la
        // victoria espera a avanzar sola tras la Cosecha doble, D-011, punto 4).
        private void LayoutVictoryMap()
        {
            if (!_victoryMapShown)
            {
                return;
            }

            RectTransform next = (RectTransform)_next.Button.transform;
            float x = next.anchoredPosition.x;
            float rowW = next.sizeDelta.x;
            float half = Mathf.Floor((rowW - VictoryMapGapDp) * 0.5f);
            UiBuilder.Place((RectTransform)_home.Button.transform, x, _victoryHomeTopDp, half, 48f);
            UiBuilder.Place((RectTransform)_victoryMap.Button.transform, x + rowW - half, _victoryHomeTopDp, half, 48f);
            LevelFlow? flow = _flow;
            UiBuilder.SetEnabled(_victoryMap.Button, _autoNextMs < 0f && flow != null && flow.Phase == FlowPhase.VictoryScreen);
        }

        // «Ver mapa» (M4.3): completa la franja (su stop_arrive y lo visto del Mapa hasta el 10k: el Mapa no repite la llegada al
        // ponerse al día) y abre el Mapa encima de la victoria, que sigue igual debajo. Nada en AD_GATE ni mientras la victoria
        // espera a avanzar sola tras la Cosecha doble.
        private void OnVictoryMapPressed()
        {
            LevelFlow? flow = _flow;
            if (!_victoryMapShown || _autoNextMs >= 0f || flow == null || flow.Phase != FlowPhase.VictoryScreen)
            {
                return;
            }

            _arrival?.Complete();
            if (_actions.OpenRiverMap(MapOpenSource.Victory))
            {
                VictoryMapOpened++;
            }
        }

        // La victoria se cierra (Siguiente, el avance tras la Cosecha doble o Inicio) con la franja a medias: llegada saltada.
        private void CloseArrival()
        {
            ArrivalStrip? strip = _arrival;
            if (strip == null || strip.Kind == ArrivalKind.None || strip.Completed)
            {
                return;
            }

            strip.Close();
            _probe.Arrival("complete", strip, _slot.ContentDp);
            SendStopArrive(strip);
        }

        private void OnArrivalStarted(ArrivalStrip strip)
        {
            if (strip.Stop != null)
            {
                AF.Audio.AudioCues.Play(AF.Audio.SfxId.Fact); // AU1: llegada a la parada (la línea de fruta va sin sonido, D-017)
                AF.Audio.AudioCues.Stinger(AF.Audio.StingerId.Arrival); // AU3: su remate, después de la toada
            }

            _probe.Arrival("start", strip, _slot.ContentDp);
            ArrivalLayoutCount++;
            _arrivalIds.Clear();
            _arrivalRects.Clear();
            strip.LayoutIds(_arrivalIds, _arrivalRects);
            if (_arrivalIds.Count > 0)
            {
                _probe.Layout("arrival", strip.Level, null, _arrivalIds.ToArray(), _arrivalRects.ToArray());
            }
        }

        private void OnArrivalCompleted(ArrivalStrip strip)
        {
            _probe.Arrival("complete", strip, _slot.ContentDp);
            SendStopArrive(strip);
        }

        /// <summary>Veces que la franja anotó lo visto del Mapa (tests).</summary>
        internal int ArrivalMapSeenCount { get; private set; }

        // af_stop_arrive (propuesta del Mapa §11.5), una vez por llegada: solo con parada (el dato desplazado no es una llegada).
        // Con ella, map_seen_level hasta el 10k de la franja (M3.5: «MarkMapSeen al acabar la franja de la victoria»).
        private void SendStopArrive(ArrivalStrip strip)
        {
            RiverStop? stop = strip.Stop;
            if (_stopArriveSent || stop == null)
            {
                return;
            }

            _stopArriveSent = true;
            StopArriveCount++;
            LastStopArriveSkipped = strip.Skipped;
            _probe.StopArrive(strip.Level, stop, strip.Skipped);
            ArrivalMapSeenCount++;
            _actions.MarkMapSeen(strip.Level);
        }
    }
}
