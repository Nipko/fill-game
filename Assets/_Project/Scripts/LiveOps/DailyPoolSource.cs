#nullable enable
using System;
using AF.Levels;

namespace AF.LiveOps
{
    /// <summary>
    /// De dónde sale el pool del Reto del río (memoria, 2026-10-26; <c>docs/dev/memoria-2026-10-26.md</c>): leer los 2.187 tableros
    /// de <c>daily_pool_v1.json</c> (1,4 MB de JSON) en el arranque subía el pico del heap gestionado, que con IL2CPP no se devuelve.
    /// Con un lector, el pool se lee la primera vez que hace falta un tablero (al abrir el Reto, o al resolver una sustitución
    /// <c>daily_overrides</c> del día) y <see cref="Release"/> lo suelta al cerrarlo; la tarjeta del inicio no lo necesita. El
    /// sorteo no cambia (D-031; nota <c>docs/dev/reto-del-rio.md</c> §5-§6): el mismo JSON da el mismo pool cada vez.
    /// </summary>
    public sealed class DailyPoolSource
    {
        private readonly Func<DailyPool?>? _load;
        private DailyPool? _pool;
        private bool _failed;

        /// <summary>Un pool ya leído (o null: sin Reto); <see cref="Release"/> no lo suelta.</summary>
        public DailyPoolSource(DailyPool? pool)
        {
            _pool = pool;
        }

        /// <summary>
        /// Un pool que se lee al pedirlo con <paramref name="load"/> (null = pool no válido: desde entonces <see cref="InBuild"/> es
        /// false y no se vuelve a intentar).
        /// </summary>
        public DailyPoolSource(Func<DailyPool?> load)
        {
            _load = load ?? throw new ArgumentNullException(nameof(load));
        }

        /// <summary>
        /// El Reto tiene pool en este build, sin leerlo: uno ya leído, o un lector que todavía no ha fallado. Si al leerlo resulta
        /// no válido, pasa a false (la tarjeta se oculta como si no existiera, D-008).
        /// </summary>
        public bool InBuild => _pool != null || (_load != null && !_failed);

        /// <summary>El pool está leído ahora mismo.</summary>
        public bool IsLoaded => _pool != null;

        /// <summary>Veces que se ha leído con el lector (tests y sonda).</summary>
        public int Loads { get; private set; }

        /// <summary>El pool (lo lee la primera vez, o tras <see cref="Release"/>), o null si no hay o no es válido.</summary>
        public DailyPool? Get()
        {
            if (_pool != null || _load == null || _failed)
            {
                return _pool;
            }

            _pool = _load();
            Loads++;
            _failed = _pool == null;
            return _pool;
        }

        /// <summary>Suelta el pool leído con el lector (al cerrar el Reto); la siguiente <see cref="Get"/> lo vuelve a leer.</summary>
        public void Release()
        {
            if (_load != null)
            {
                _pool = null;
            }
        }
    }
}
