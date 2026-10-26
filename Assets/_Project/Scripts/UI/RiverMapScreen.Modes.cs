#nullable enable
using System;
using AF.GameFlow;
using AF.Levels;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Las entradas del Mapa del río desde la pausa, la victoria y el fin de contenido, y el modo <c>ui_map_mode</c> (tarea M4.3;
    /// D-013, punto 7; 02 §5 y §6; 04 §5.2; 06 §4.5 y §9; 08 §9), parcial de <see cref="RiverMapScreen"/> sobre su gancho
    /// <c>ModesOpen</c> y dos ayudas que leen «ponerse al día» (RiverMapScreen.CatchUp.cs) y las mini cuias
    /// (RiverMapScreen.Markers.cs):
    /// <list type="bullet">
    /// <item><description><b>Pausa</b> (<see cref="MapOpenSource.Pause"/>): el Mapa se abre encima del nivel en pausa y «Jugar»
    /// pasa a «Volver al nivel» (<c>map_back_to_level</c>, P-M2): cierra el Mapa y deja el nivel tal cual. No es salir del nivel:
    /// ni canoa, ni Crecida, ni <c>fails_level</c>, ni cambio de fase (04 §5.2, fila del Mapa desde la pausa; D-023).</description></item>
    /// <item><description><b>Victoria</b> (<see cref="MapOpenSource.Victory"/>, «Ver mapa» de la victoria del <c>10k</c>): encima
    /// de la victoria, que sigue debajo; «Jugar» pasa a «Volver» (<c>back</c>, glosario §10) [P] y vuelve a ella. «Siguiente» y
    /// AD_GATE no cambian. Desde D-053, con el viaje del tramo recién ganado, como el fin de contenido (<see cref="LastLegStart"/>:
    /// del 10 al 20 en la victoria del 20), aunque la franja ya anotara la llegada (sin <c>stop_arrive</c> nuevo).</description></item>
    /// <item><description><b>Fin de contenido</b> (<see cref="MapOpenSource.ContentEnd"/>, 06 §9: «la canoa llega al último tramo
    /// publicado»): antes de «El río sigue creciendo», el Mapa con <b>el viaje del último tramo publicado</b>: «ponerse al día»
    /// (M3.5) arranca, como pronto, al principio de ese tramo (<see cref="LastLegStart"/>: con <c>max_level</c> 300, desde el
    /// 290), así que la canoa lo recorre de boya en boya hasta la última parada aunque ya se hubiera visto (la franja de la
    /// victoria anota lo visto). Dura <see cref="RiverJourney.CatchUpMs"/> ≤ <see cref="MapConfig.CatchUpMaxMs"/> ms, por debajo
    /// del tope <see cref="MapConfig.AnimMapWorldMs"/> (<c>anim_map_world_ms</c>, 08 §9); un toque lo completa y, con Reducir
    /// movimiento, el estado final entra con el fundido de «ponerse al día». Repetir el tramo no es una llegada nueva: solo las
    /// llegadas por encima de <c>map_seen_level</c> escriben <c>stop_arrive</c>; la del final de lo ya visto solo llena otra vez su
    /// mini cuia. Lo que viene después sigue en vidrio. Sin frase de TiTino (D-013, punto 15: la de fin de contenido no está en
    /// esta tarea). «Jugar» pasa a «Volver» [P]: con él, la ← o el Atrás de Android, el Mapa se cierra y queda la pantalla de
    /// siempre, con su texto y su ETA.</description></item>
    /// <item><description><b><c>ui_map_mode</c> = <c>worlds</c></b> (08 §9; 06 §4.5; Q1-C de la propuesta del Mapa): un filtro de
    /// la misma pantalla y del mismo catálogo, no una segunda implementación: solo las grandes paradas (<see cref="StopKind.Gate"/>,
    /// la primera de cada mundo: una por mundo, las 13 del canon anterior con el catálogo entero; 9 con el de 1-700), sin boyas
    /// intermedias ni Rosa en ellas (eso ya lo hacía RiverMapScreen.Progress.cs). El modo es el de
    /// <see cref="ProgressMapMode"/> (el default de <see cref="MapConfig.UiMapMode"/>, mientras no exista Remote Config, o el
    /// de un test).</description></item>
    /// </list>
    /// La sonda <c>map_open</c> (<c>af_map_open</c>, 08 §8.2) con <c>source</c> <c>pause</c>, <c>victory</c> o <c>content_end</c> la
    /// escribe <see cref="GameBootstrap.OpenRiverMap"/>. Sin asignaciones: solo se fijan un texto y un entero al abrir.
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        private int _modesReplayFrom = -1;

        /// <summary><c>ui_map_mode</c> = <c>worlds</c>: solo las grandes paradas, sin boyas (<see cref="ProgressMapMode"/>).</summary>
        public bool WorldsMode => IsWorlds(_progressMapMode);

        /// <summary>
        /// El Mapa se abrió al acabarse el contenido y hace el viaje del último tramo publicado: «ponerse al día» arranca, como
        /// pronto, en <see cref="ContentEndJourneyFromLevel"/>.
        /// </summary>
        public bool ContentEndJourney => _modesReplayFrom >= 0 && _source == MapOpenSource.ContentEnd;

        /// <summary>Nivel desde el que arranca el viaje del fin de contenido (<see cref="LastLegStart"/>), o −1 sin él.</summary>
        public int ContentEndJourneyFromLevel => ContentEndJourney ? _modesReplayFrom : -1;

        /// <summary>Lo que dura el viaje del fin de contenido (ms): el de «ponerse al día»; 0 sin él.</summary>
        public int ContentEndJourneyMs => ContentEndJourney ? CatchUpDurationMs : 0;

        /// <summary>
        /// El Mapa repite el viaje del tramo recién ganado aunque ya se hubiera visto (D-053): el del fin de contenido y el de «Ver
        /// mapa» en la victoria del <c>10k</c>, cuya franja ya anotó la llegada como vista. Arranca, como pronto, en
        /// <see cref="ReplayJourneyFromLevel"/>.
        /// </summary>
        public bool ReplayJourney => _modesReplayFrom >= 0;

        /// <summary>Nivel desde el que arranca el viaje repetido (<see cref="LastLegStart"/>), o −1 sin él.</summary>
        public int ReplayJourneyFromLevel => _modesReplayFrom;

        /// <summary>
        /// Principio del tramo que acaba en <paramref name="maxLevel"/> (el último publicado al acabarse el contenido): el
        /// <c>10k</c> anterior (300 → 290; 295 → 290; 10 → 0). 0 con <paramref name="maxLevel"/> ≤ 0.
        /// </summary>
        public static int LastLegStart(int maxLevel)
        {
            if (maxLevel <= 0)
            {
                return 0;
            }

            return ((maxLevel - 1) / RiverStopCatalog.LevelsPerStop) * RiverStopCatalog.LevelsPerStop;
        }

        /// <summary>
        /// La parada <paramref name="stop"/> tiene mini cuia con el modo <paramref name="mapMode"/> (<c>ui_map_mode</c>): todas
        /// con <c>stops</c> (o un valor desconocido); con <c>worlds</c>, solo las grandes paradas (08 §9: la primera de cada mundo).
        /// </summary>
        public static bool ShowsStop(RiverStop stop, string? mapMode)
        {
            if (stop == null)
            {
                throw new ArgumentNullException(nameof(stop));
            }

            return !IsWorlds(mapMode) || stop.Kind == StopKind.Gate;
        }

        /// <summary>Paradas del catálogo <paramref name="stops"/> que se ven con el modo <paramref name="mapMode"/> (<see cref="ShowsStop"/>).</summary>
        public static int VisibleStopCount(RiverStopCatalog stops, string? mapMode)
        {
            if (stops == null)
            {
                throw new ArgumentNullException(nameof(stops));
            }

            int n = 0;
            for (int k = 0; k < stops.Count; k++)
            {
                RiverStop? stop = stops.At(k);
                if (stop != null && ShowsStop(stop, mapMode))
                {
                    n++;
                }
            }

            return n;
        }

        partial void ModesOpen(MapOpenSource source)
        {
            _modesReplayFrom = -1;
            switch (source)
            {
                case MapOpenSource.Pause:
                    _playLabel.text = UiText.MapBackToLevel; // P-M2: el «Jugar» del Mapa abierto desde la pausa
                    break;
                case MapOpenSource.Victory:
                case MapOpenSource.ContentEnd:
                    _playLabel.text = UiText.Back; // vuelve a la victoria o a «El río sigue creciendo» [P]
                    break;
            }

            // D-053 (petición del dueño del 2026-10-26): «Ver mapa» en la victoria del 10k también hace el viaje del tramo que se
            // acaba de ganar (la franja ya anotó la llegada como vista, así que sin esto la canoa no se movía: «no pasa nada»).
            IMapProgress? progress = _progress;
            if ((source == MapOpenSource.ContentEnd || source == MapOpenSource.Victory) && progress != null)
            {
                int max = Mathf.Clamp(progress.MaxLevel, 0, WorldTable.LastLevel);
                if (max >= RiverStopCatalog.LevelsPerStop)
                {
                    _modesReplayFrom = LastLegStart(max);
                }
            }
        }

        // «Ponerse al día» (RiverMapScreen.CatchUp.cs) arranca en lo visto; en el viaje del fin de contenido, como pronto al
        // principio del último tramo publicado. Lo visto de verdad (el que decide qué llegadas son nuevas) no cambia.
        private int ModesCatchUpSeen(int seen)
        {
            return _modesReplayFrom >= 0 ? Math.Min(seen, _modesReplayFrom) : seen;
        }

        // El primer fotograma del viaje del fin de contenido (revisión de FX13): arranca en la llegada del principio del último
        // tramo (690 con el 700) y enseña ya ese tramo, vacío, con sus 10 boyas en su sitio (FrontAheadStop), y no el de la
        // llegada entero (el de Tambo, sin largo en Mundo, salía en la fila corta bajo la parada y saltaba al tramo de la naciente
        // en el fotograma siguiente). Solo mientras dura el viaje y en su nivel de partida.
        private bool FrontAheadNow(int level)
        {
            return _modesReplayFrom >= 0 && _catchUp && level == _catchFrom && level == _modesReplayFrom;
        }

        // En el viaje del fin de contenido, la llegada ya vista al final del tramo vuelve a llenar su mini cuia (sin stop_arrive).
        private bool ModesReplaysArrival(int level)
        {
            return _modesReplayFrom >= 0 && level > _modesReplayFrom && level >= RiverStopCatalog.LevelsPerStop
                && level % RiverStopCatalog.LevelsPerStop == 0;
        }

        // Las mini cuias (RiverMapScreen.Markers.cs): con ui_map_mode = worlds, solo las grandes paradas.
        private bool ModesShowsStop(RiverStop stop)
        {
            return ShowsStop(stop, _progressMapMode);
        }

        private static bool IsWorlds(string? mapMode)
        {
            return string.Equals(mapMode, MapConfig.MapModeWorlds, StringComparison.Ordinal);
        }
    }
}
