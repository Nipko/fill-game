#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// «Ponerse al día» al abrir el Mapa del río (tarea M3.5; D-013, punto 14; propuesta del Mapa §4.2 y §11.5; 08 §4.3 y §8.2),
    /// parcial de <see cref="RiverMapScreen"/> sobre sus ganchos <c>CatchUp*</c>:
    /// <list type="bullet">
    /// <item>al abrir, lo ganado desde la última vez que se vio el Mapa (<c>progress.map_seen_level</c>) se enseña de boya en
    /// boya: el río arranca en <see cref="RiverJourney.CatchUpFrom"/> (como mucho las 10 boyas de un tramo) y cada
    /// <see cref="MapConfig.CatchUpPerBuoyMs"/> ms se enciende la siguiente, hasta <c>max_level</c>, en
    /// <see cref="RiverJourney.CatchUpMs"/> ≤ <see cref="MapConfig.CatchUpMaxMs"/> ms (con seen 52 y max 58, 6 boyas en
    /// 900 ms);</item>
    /// <item>la canoa recorre el agua con ellas y, si hubo una llegada sin ver (un <c>10k</c> ganado después de
    /// <c>map_seen_level</c>), la recorre: llega a la parada, se llena su mini cuia (<see cref="MapConfig.ArrivalSampleFillMs"/>
    /// ms), se iza su bandera y se enciende la baliza si cierra el mundo; cada llegada escribe <c>AF_STATE stop_arrive</c> con
    /// <c>view</c> = <c>map</c> (<c>af_stop_arrive</c>, 08 §8.2);</item>
    /// <item>un toque en cualquier sitio del Mapa lo completa en el acto (las llegadas que faltaban, con <c>skipped</c>); cerrarlo
    /// a medias, también. Con Reducir movimiento (07 §7.8), sin animación: el estado final directamente;</item>
    /// <item>lo visto se anota al cerrar el Mapa (<c>MarkMapSeen</c> en <see cref="GameBootstrap.CloseRiverMap"/>, con la sonda
    /// <c>map_close</c>) y al acabar la franja de llegada de la victoria (GameScreen.Arrival.cs).</item>
    /// </list>
    /// Mientras dura, el Mapa pinta a 60 fps; las mallas del río se rehacen una vez por boya (≤ 10), nunca por fotograma.
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        private bool _catchUp;
        private int _catchFrom;
        private int _catchTo;
        private int _catchSeen;
        private int _catchStep;
        private float _catchMs;
        private int _catchArrivals;
        private bool _lastCatchArrivalSkipped;
        private int _lastCatchArrivalLevel;
        private bool _catchCompletedByTouch;
        private bool[] _hadFill = Array.Empty<bool>();

        /// <summary>«Ponerse al día» está en marcha.</summary>
        public bool CatchingUp => _catchUp;

        /// <summary>Nivel desde el que se puso al día el último <see cref="Open"/> (= <see cref="CatchUpToLevel"/> si no hubo nada).</summary>
        public int CatchUpFromLevel => _catchFrom;

        /// <summary>Nivel hasta el que se pone al día (<c>max_level</c> al abrir).</summary>
        public int CatchUpToLevel => _catchTo;

        /// <summary>Boyas que enciende «ponerse al día» en el último <see cref="Open"/>.</summary>
        public int CatchUpBuoys => _catchTo - _catchFrom;

        /// <summary>Lo que dura (ms): <see cref="RiverJourney.CatchUpMs"/>.</summary>
        public int CatchUpDurationMs => Mathf.Min(CatchUpBuoys * MapConfig.CatchUpPerBuoyMs, MapConfig.CatchUpMaxMs);

        /// <summary>Milisegundos de «ponerse al día» transcurridos.</summary>
        public float CatchUpElapsedMs => _catchMs;

        /// <summary>Llegadas que recorrió la canoa al ponerse al día (cada una, un <c>stop_arrive</c> con <c>view</c> = <c>map</c>).</summary>
        public int CatchUpArrivals => _catchArrivals;

        /// <summary>El <c>skipped</c> de la última llegada al ponerse al día.</summary>
        public bool LastCatchUpArrivalSkipped => _lastCatchArrivalSkipped;

        /// <summary>El nivel (<c>10k</c>) de la última llegada al ponerse al día (0 si ninguna).</summary>
        public int LastCatchUpArrivalLevel => _lastCatchArrivalLevel;

        /// <summary>El último «ponerse al día» lo completó un toque (o el cierre) antes de acabar.</summary>
        public bool CatchUpCompletedByTouch => _catchCompletedByTouch;

        partial void CatchUpOpen(IMapProgress progress)
        {
            _catchUp = false;
            _catchMs = 0f;
            _catchArrivals = 0;
            _lastCatchArrivalSkipped = false;
            _lastCatchArrivalLevel = 0;
            _catchCompletedByTouch = false;
            int max = Mathf.Clamp(progress.MaxLevel, 0, WorldTable.LastLevel);
            int seen = Mathf.Clamp(progress.MapSeenLevel, 0, max);
            _catchFrom = max;
            _catchTo = max;
            _catchSeen = seen;
            _catchStep = max;
            if (_map == null || _routes == null || FillLayer == null)
            {
                return;
            }

            int from = RiverJourney.CatchUpFrom(seen, max);
            if (from >= max)
            {
                return;
            }

            _catchFrom = from;
            if (ProgressReduceMotion)
            {
                // Sin viaje ni llenado animado: el estado final ya está pintado (el fundido de los afluentes es de M3.4), pero las
                // llegadas sin ver se cuentan como vistas.
                SendCatchUpArrivals(from, max, false);
                return;
            }

            _catchUp = true;
            _catchStep = from;
            ShowProgressAt(from);
            StartFades(seen); // lo que ya estaba lleno en «from» pero nunca se vio (el Xingu con seen 0) aparece con su fundido
            ShowMarkersAt(from);
            MoveCanoe(from);
        }

        partial void CatchUpTick(float dtMs, ref bool animating)
        {
            if (!_catchUp)
            {
                return;
            }

            animating = true;
            _catchMs += dtMs;
            float x = _catchFrom + (_catchMs / MapConfig.CatchUpPerBuoyMs);
            int level = Mathf.Min(_catchTo, Mathf.FloorToInt(x));
            while (_catchStep < level)
            {
                Step(_catchStep + 1);
            }

            if (x >= _catchTo)
            {
                FinishCatchUp(false);
                return;
            }

            MoveCanoe(x);
        }

        partial void CatchUpClose(IMapProgress progress)
        {
            if (_catchUp)
            {
                FinishCatchUp(true); // cerrado a medias: lo que faltaba, como saltado
            }
        }

        /// <summary>
        /// Un toque durante «ponerse al día»: lo completa en el acto (las llegadas que faltaban, con <c>skipped</c>). True si había
        /// algo que completar (el toque no hace nada más). Lo llaman el toque del mapa, el de una mini cuia y el de la ventanita.
        /// </summary>
        internal bool CatchUpTouch()
        {
            if (!_catchUp)
            {
                return false;
            }

            FinishCatchUp(true);
            return true;
        }

        // Enciende la boya del nivel «level»: el río, los afluentes que se llenan al pasar su boca (con su fundido), las banderas,
        // las mini cuias y las balizas; si es una llegada sin ver, se llena su mini cuia y escribe stop_arrive.
        private void Step(int level)
        {
            _catchStep = level;
            RecordFill();
            ShowProgressAt(level);
            if (NewPassbyFill())
            {
                StartFades(level - 1); // solo si en esta boya pasa una boca: así no corta los fundidos que siguen en marcha
            }

            ShowMarkersAt(level);
            if (IsUnseenArrival(level))
            {
                FillStopMarker(level / RiverStopCatalog.LevelsPerStop);
                SendCatchUpArrival(level, false);
            }
        }

        // Acaba: el estado final (max_level) y, con un toque o el cierre, las llegadas que faltaban como saltadas.
        private void FinishCatchUp(bool byTouch)
        {
            if (!_catchUp)
            {
                return;
            }

            _catchUp = false;
            _catchCompletedByTouch = byTouch;
            int from = _catchStep;
            if (from < _catchTo)
            {
                _catchStep = _catchTo;
                ShowProgressAt(_catchTo);
                StartFades(from);
                ShowMarkersAt(_catchTo);
                SendCatchUpArrivals(from, _catchTo, byTouch);
            }

            if (byTouch)
            {
                FinishStopMarkerFill();
            }

            MoveCanoe(_catchTo);
        }

        // Qué líneas tienen ya agua (antes de encender una boya). Asigna solo la primera vez o con otro Mapa.
        private void RecordFill()
        {
            IReadOnlyList<RiverLine> lines = _map!.Lines;
            if (_hadFill.Length != lines.Count)
            {
                _hadFill = new bool[lines.Count];
            }

            for (int i = 0; i < lines.Count; i++)
            {
                _hadFill[i] = FillFrom(i) < 1f;
            }
        }

        // Al encender la boya, se llenó por su boca algún afluente de paso o local que antes no tenía agua (el que se funde).
        private bool NewPassbyFill()
        {
            IReadOnlyList<RiverLine> lines = _map!.Lines;
            for (int i = 0; i < lines.Count && i < _hadFill.Length; i++)
            {
                RiverLineKind kind = lines[i].Kind;
                if ((kind == RiverLineKind.Passby || kind == RiverLineKind.Local) && !_hadFill[i] && FillFrom(i) < 1f && !RouteClimbs(_routes!, i))
                {
                    return true;
                }
            }

            return false;
        }

        // Las llegadas sin ver de (from, to], en orden.
        private void SendCatchUpArrivals(int from, int to, bool skipped)
        {
            for (int level = from + 1; level <= to; level++)
            {
                if (IsUnseenArrival(level))
                {
                    SendCatchUpArrival(level, skipped);
                }
            }
        }

        private bool IsUnseenArrival(int level)
        {
            return level > _catchSeen && level >= RiverStopCatalog.LevelsPerStop && level % RiverStopCatalog.LevelsPerStop == 0;
        }

        // af_stop_arrive con view = map (08 §8.2): solo con parada en el catálogo.
        private void SendCatchUpArrival(int level, bool skipped)
        {
            RiverStop? stop = _stops?.At(level / RiverStopCatalog.LevelsPerStop);
            if (stop == null)
            {
                return;
            }

            _catchArrivals++;
            _lastCatchArrivalSkipped = skipped;
            _lastCatchArrivalLevel = level;
            _probe.StopArrive(level, stop, skipped, true);
        }
    }
}
