#nullable enable
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;

namespace AF.UI
{
    /// <summary>
    /// La memoria del Mapa por vueltas (observación 5 de la prueba de la 1.1 del 2026-11-07: <c>GL mtrack</c> no vuelve al cerrar el
    /// Mapa v2), solo con la sonda. Cuenta las aperturas de la sesión (<c>map_open_n</c>) y escribe dos líneas <c>AF_PERF mem</c>
    /// más, con los miembros <c>map_*</c> (<see cref="RiverMapScreen.SampleGfx"/>):
    /// <list type="bullet">
    /// <item><c>map_settled</c>, <see cref="MapSettledMs"/> después de abrir, con el Mapa aún abierto (la de <c>map</c> sale antes del
    /// primer fotograma, cuando los lienzos aún no tienen sus lotes ni la geometría sus vértices);</item>
    /// <item><c>map_after</c>, <see cref="MapAfterMs"/> después de cerrar, si no se volvió a abrir: la de <c>map_close</c> sale en el
    /// mismo fotograma del cierre y sus <c>obj_*</c> aún cuentan las texturas que <c>Object.Destroy</c> suelta al final del
    /// fotograma; esta, no.</item>
    /// </list>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Tiempo con el Mapa abierto tras el que sale <c>map_settled</c> (el viaje de Capi ya acabó).</summary>
        public const float MapSettledMs = 5000f;

        /// <summary>Tiempo tras cerrar el Mapa tras el que sale <c>map_after</c>.</summary>
        public const float MapAfterMs = 2000f;

        private int _mapOpens;
        private float _mapMemMs = -1f;
        private bool _mapMemOpen;
        private int _mapMemLevel;

        /// <summary>
        /// Abre el Mapa con los búferes de los lienzos del arte por código en préstamo (<see cref="PixelBuffers"/>; añadido de D-069 del
        /// 2026-11-08): el río vivo, los pueblos, el rig de Capi y la tripulación pintan en lienzos de 16 bytes por píxel que se tiran
        /// al subir a la GPU (unos 17 MiB por apertura). Con IL2CPP el heap gestionado crece hasta el pico y no se devuelve: mientras se
        /// abre, cada lienzo reutiliza el búfer del anterior si le sirve, y al acabar se sueltan todos (ninguno queda vivo con el Mapa
        /// abierto ni cerrado). Si ya se estaban guardando (el arranque), no lo toca.
        /// </summary>
        private void OpenMapPooled(RiverMapScreen map, MapOpenSource source, RiverStopCatalog stops, IProgressStore progress, int play, bool playAvailable)
        {
            bool pool = !PixelBuffers.Pooling;
            if (pool)
            {
                PixelBuffers.BeginPooling();
            }

            try
            {
                map.Open(source, _riverMapData!, _riverRoutes!, stops, progress, play, playAvailable, DailyPlayUnlocked);
            }
            finally
            {
                if (pool)
                {
                    PixelBuffers.EndPooling();
                }
            }
        }

        /// <summary>Aperturas del Mapa en esta sesión (la vuelta de la medida).</summary>
        internal int MapOpens => _mapOpens;

        private void MapMemOpened(int playLevel)
        {
            _mapOpens++;
            _mapMemOpen = true;
            _mapMemLevel = playLevel;
            _mapMemMs = DevProbe.Enabled ? 0f : -1f;
        }

        private void MapMemClosed()
        {
            _mapMemOpen = false;
            _mapMemMs = DevProbe.Enabled ? 0f : -1f;
        }

        // Cada fotograma; sin asignaciones salvo el de la línea.
        private void MapMemTick(float dtMs)
        {
            if (_mapMemMs < 0f || _mapScreen == null)
            {
                return;
            }

            _mapMemMs += dtMs;
            if (_mapMemMs < (_mapMemOpen ? MapSettledMs : MapAfterMs))
            {
                return;
            }

            _mapMemMs = -1f;
            if (_mapMemOpen != _mapScreen.Visible)
            {
                return; // se cerró (o se abrió) por otro camino: la vuelta siguiente lo dirá
            }

            DevProbe.Mem(_mapMemOpen ? "map_settled" : "map_after", _mapMemOpen ? _mapMemLevel : 0, _mapScreen.GfxMemJson(_mapOpens).Substring(1));
        }
    }
}
