#nullable enable
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
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
    /// <c>river_fact_shown</c> sale del punto común de la ranura (GameScreen.Victory.cs), compartido con la línea de fruta (F3).
    /// </summary>
    public sealed partial class GameScreen
    {
        private ArrivalStrip? _arrival;
        private bool _stopArriveSent;
        private readonly List<string> _arrivalIds = new List<string>(1);
        private readonly List<RectTransform> _arrivalRects = new List<RectTransform>(1);

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

        partial void ArrivalBuild()
        {
            if (ArrivalDisabledForTests)
            {
                return;
            }

            _arrival = new ArrivalStrip(_art, _victoryPanel, () => RiverStops, OnArrivalStarted, OnArrivalCompleted);
            RegisterVictoryLineProvider(_arrival);
        }

        partial void ArrivalLevelLoaded(LevelFlow flow)
        {
            _stopArriveSent = false;
            // De F3 (la línea de fruta), no de la llegada: calienta las medidas de la ranura del Dato del río al cargar el nivel, para
            // que la primera victoria no las pague (~20 ms; FactLineTests.Measures_AreMadeWhenTheLevelLoads_NotAtTheVictory). No la
            // quites al rehacer este parcial: llévala al gancho propio de la ranura (SlotLevelLoaded) cuando GameScreen.cs lo declare.
            WarmVictorySlot(flow.LevelNumber);
        }

        partial void ArrivalVictoryShown(LevelFlow flow)
        {
            _stopArriveSent = false;
        }

        partial void ArrivalAdGate(LevelFlow flow)
        {
            CloseArrival();
        }

        partial void ArrivalHomePressed(LevelFlow flow)
        {
            CloseArrival();
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

        // af_stop_arrive (propuesta del Mapa §11.5), una vez por llegada: solo con parada (el dato desplazado no es una llegada).
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
        }
    }
}
