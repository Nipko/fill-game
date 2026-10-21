#nullable enable
using System.Collections.Generic;
using AF.Economy;

namespace AF.GameFlow
{
    /// <summary>
    /// Meta de B14 en el flujo del nivel (tarea B14 de la tanda 5), con el mismo patrón que la Crecida (LevelFlow.Streak.cs): los
    /// servicios se inyectan desde el arranque y aquí solo hay dos llamadas.
    /// <list type="bullet">
    /// <item><description><b>Skins por hito</b> (06 §4.5; D-033, Q-M8 (a)): <c>ResolveWin</c> llama a <see cref="OnWinSkins"/>
    /// dentro del mismo lote del perfil, así el derecho, la skin puesta y la victoria salen en la misma escritura;
    /// <see cref="LastSkinUnlock"/> es lo ganado (lo lee la vista para el aviso «{skin} · puesta» y la sonda
    /// <c>af_skin_unlock</c>). El Salto del Boto (B3) la da igual al saltar el nivel que cierra el mundo
    /// (<see cref="OnSkipSkins"/>, en el mismo lote que el salto); si faltara, la recupera <see cref="SkinService.Backfill"/> en
    /// el siguiente arranque.</description></item>
    /// <item><description><b>Álbum de frutas</b> (08 §4.3 y §8.2): <c>Load</c> llama a <see cref="OnLoadAlbum"/> con el tablero
    /// inicial; <see cref="LastFruitsDiscovered"/> son los tintes nuevos de esta carga (la sonda <c>af_fruit_discovered</c>). El
    /// Reto del río no cuenta: es la campaña la que llena el Cuaderno [P].</description></item>
    /// </list>
    /// Sin servicio (los tests del flujo, el editor sin arranque), no hacen nada.
    /// </summary>
    public sealed partial class LevelFlow
    {
        private readonly List<int> _fruitsDiscovered = new List<int>();
        private SkinService? _skins;
        private FruitAlbum? _album;
        private SkinUnlock? _lastSkinUnlock;

        /// <summary>Servicio de skins (<see cref="UseSkins"/>), o null.</summary>
        public SkinService? Skins => _skins;

        /// <summary>Álbum de frutas (<see cref="UseAlbum"/>), o null.</summary>
        public FruitAlbum? Album => _album;

        /// <summary>La skin ganada en la última victoria de este flujo, o null (se limpia al cargar un nivel).</summary>
        public SkinUnlock? LastSkinUnlock => _lastSkinUnlock;

        /// <summary>
        /// La última skin ganada en este flujo (no se limpia al cargar) y cuántas van: con el contador, quien la lee no pierde una
        /// victoria aunque la carga siguiente caiga en el mismo fotograma (revisión de B14).
        /// </summary>
        public SkinUnlock? LatestSkinUnlock { get; private set; }

        /// <summary>Skins ganadas en este flujo (sube con cada <see cref="LatestSkinUnlock"/>).</summary>
        public int SkinUnlockSerial { get; private set; }

        /// <summary>
        /// <see cref="LoadSerial"/> cuando se ganó <see cref="LatestSkinUnlock"/>: el aviso sale en la carga siguiente. En una
        /// victoria es la del nivel ganado; en un Salto del Boto, la de antes de cargar el nivel siguiente, que ya es la siguiente.
        /// </summary>
        public int SkinUnlockLoad { get; private set; }

        /// <summary>Cargas de nivel de este flujo (sube con cada <c>Load</c>): quien espera «la pantalla siguiente» la compara.</summary>
        public int LoadSerial { get; private set; }

        /// <summary>Tintes descubiertos en la última carga de nivel (número de motor, en orden; vacía si ninguno).</summary>
        public IReadOnlyList<int> LastFruitsDiscovered => _fruitsDiscovered;

        /// <summary>Veces que la carga de un nivel descubrió algún tinte (la sonda lo usa para saber si hay líneas nuevas).</summary>
        public int FruitDiscoveryCount { get; private set; }

        /// <summary>Inyecta el servicio de skins (null lo quita). Vale desde la siguiente victoria.</summary>
        public void UseSkins(SkinService? skins)
        {
            _skins = skins;
            _lastSkinUnlock = null;
        }

        /// <summary>Inyecta el álbum de frutas (null lo quita). Vale desde la siguiente carga.</summary>
        public void UseAlbum(FruitAlbum? album)
        {
            _album = album;
            _fruitsDiscovered.Clear();
        }

        // En ResolveWin, dentro del lote y tras la Crecida.
        private void OnWinSkins()
        {
            _lastSkinUnlock = _skins?.ApplyLevelPassed(LevelNumber, !IsDaily);
            if (_lastSkinUnlock != null)
            {
                LatestSkinUnlock = _lastSkinUnlock;
                SkinUnlockSerial++;
                SkinUnlockLoad = LoadSerial;
            }
        }

        // En Skip (LevelFlow.Skip.cs), dentro del lote y antes de cargar el nivel siguiente: un nivel saltado también se supera
        // (cuenta para max_level), así que también cierra el mundo y da su skin (nota de B14 en D-033). La carga siguiente limpia
        // LastSkinUnlock; el aviso lo lleva LatestSkinUnlock.
        private void OnSkipSkins(int level)
        {
            SkinUnlock? unlock = _skins?.ApplyLevelPassed(level, !IsDaily);
            if (unlock != null)
            {
                _lastSkinUnlock = unlock;
                LatestSkinUnlock = unlock;
                SkinUnlockSerial++;
                SkinUnlockLoad = LoadSerial;
            }
        }

        // En Load, dentro del lote, con el tablero inicial ya creado.
        private void OnLoadAlbum()
        {
            LoadSerial++;
            _lastSkinUnlock = null;
            _fruitsDiscovered.Clear();
            if (_album == null || IsDaily || _session == null)
            {
                return;
            }

            if (_album.Discover(_session.Initial, _fruitsDiscovered) > 0)
            {
                FruitDiscoveryCount++;
            }
        }
    }
}
