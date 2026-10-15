#nullable enable
using System;
using System.Collections.Generic;

namespace AF.GameFlow
{
    /// <summary>
    /// Lo que lee el Mapa del río del progreso (08 §2.2: <see cref="RiverJourney"/> sobre <c>max_level</c>,
    /// <c>skipped_levels</c> y <c>map_seen_level</c>; 08 §4.3; tarea M3.1b), solo lectura y sin asignar memoria: lo consultan
    /// la pestaña del inicio (el punto de «hay una llegada sin ver») y el Mapa al abrirse («ponerse al día», propuesta del
    /// Mapa §4.2). Lo implementa todo <see cref="IProgressStore"/>.
    /// </summary>
    public interface IMapProgress
    {
        /// <summary>Nivel más alto superado (<c>progress.max_level</c>), o 0 si ninguno.</summary>
        int MaxLevel { get; }

        /// <summary>Siguiente nivel que se juega (<c>progress.next_level</c>), o 0 si no hay progreso.</summary>
        int NextLevel { get; }

        /// <summary>
        /// Último nivel cuyo estado del Mapa ya vio el jugador (<c>progress.map_seen_level</c>, D-013 punto 14), recortado a
        /// [0, <see cref="MaxLevel"/>]: 0 si nunca lo vio.
        /// </summary>
        int MapSeenLevel { get; }

        /// <summary>
        /// El nivel <paramref name="level"/> se saltó con el Salto del Boto (<c>progress.skipped_levels</c>; 04 §2.6): su boya
        /// lleva a Rosa. Sin asignaciones; false para cualquier nivel que no esté en la lista, también fuera de rango.
        /// </summary>
        bool IsSkipped(int level);
    }

    /// <summary>
    /// Progreso de campaña: <c>progress.next_level</c>, <c>progress.max_level</c> y <c>progress.map_seen_level</c> del perfil
    /// (GDD 08 §4.3; 04 §10.1; D-013). En el juego lo guarda <see cref="ProfileProgressStore"/> en el perfil de <c>AF.Save</c>
    /// (tarea B1-5; antes, en v0, en PlayerPrefs con <c>af_v0_level</c>, que se migra una vez: <see cref="ProgressMigration"/>);
    /// en los tests, <see cref="MemoryProgressStore"/>. Es también la vista del Mapa (<see cref="IMapProgress"/>, M3.1b).
    /// </summary>
    public interface IProgressStore : IMapProgress
    {
        /// <summary>Siguiente nivel que se juega (<c>next_level</c>), o 0 si no hay progreso.</summary>
        int LoadNextLevel();

        /// <summary>Nivel más alto superado (<c>max_level</c>), o 0 si ninguno.</summary>
        int LoadMaxLevel();

        /// <summary>Guarda el siguiente nivel y lo deja persistido (sobrevive a un <c>force-stop</c>).</summary>
        void SaveNextLevel(int level);

        /// <summary>
        /// Victoria del nivel <paramref name="level"/>: <c>max_level</c> = máx(<c>max_level</c>, <paramref name="level"/>) y
        /// <c>next_level</c> = <paramref name="nextLevel"/>, en un solo guardado persistido.
        /// </summary>
        void SaveWin(int level, int nextLevel);

        /// <summary>
        /// <c>map_seen_level</c> (<see cref="IMapProgress.MapSeenLevel"/>): el último nivel cuyo estado del Mapa ya vio el
        /// jugador, recortado a [0, <c>max_level</c>].
        /// </summary>
        int LoadMapSeenLevel();

        /// <summary>
        /// El jugador vio el estado del Mapa hasta el nivel <paramref name="level"/> (al cerrar el Mapa y tras la franja de
        /// llegada de la victoria; 08 §4.3): <c>map_seen_level</c> = máx(actual, mín(<paramref name="level"/>,
        /// <c>max_level</c>)). Es monotónico: nunca baja, y si el valor no cambia no se guarda nada.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> es negativo.</exception>
        void SaveMapSeenLevel(int level);
    }

    /// <summary>Progreso en memoria (tests y Editor).</summary>
    public sealed class MemoryProgressStore : IProgressStore
    {
        private readonly List<int> _skipped = new List<int>();
        private int _mapSeenLevel;

        /// <summary>
        /// Crea el almacén con un valor inicial (0 = sin progreso); <paramref name="mapSeenLevel"/> se recorta a
        /// [0, <paramref name="maxLevel"/>] al leerlo, como en el perfil.
        /// </summary>
        public MemoryProgressStore(int nextLevel = 0, int maxLevel = 0, int mapSeenLevel = 0)
        {
            NextLevel = nextLevel;
            MaxLevel = maxLevel;
            _mapSeenLevel = mapSeenLevel;
        }

        /// <summary>Siguiente nivel guardado.</summary>
        public int NextLevel { get; private set; }

        /// <summary>Nivel más alto superado.</summary>
        public int MaxLevel { get; private set; }

        /// <inheritdoc />
        public int MapSeenLevel => ClampSeen(_mapSeenLevel, MaxLevel);

        /// <summary>Veces que se ha guardado.</summary>
        public int SaveCount { get; private set; }

        /// <summary>Anota <paramref name="level"/> como saltado (lo que hace el Salto del Boto en el perfil), para los tests.</summary>
        public void MarkSkipped(int level)
        {
            if (!IsSkipped(level))
            {
                _skipped.Add(level);
            }
        }

        /// <inheritdoc />
        public bool IsSkipped(int level)
        {
            return ProgressRules.Contains(_skipped, level);
        }

        /// <inheritdoc />
        public int LoadNextLevel()
        {
            return NextLevel;
        }

        /// <inheritdoc />
        public int LoadMaxLevel()
        {
            return MaxLevel;
        }

        /// <inheritdoc />
        public void SaveNextLevel(int level)
        {
            NextLevel = level;
            SaveCount++;
        }

        /// <inheritdoc />
        public void SaveWin(int level, int nextLevel)
        {
            MaxLevel = Math.Max(MaxLevel, level);
            NextLevel = nextLevel;
            SaveCount++;
        }

        /// <inheritdoc />
        public int LoadMapSeenLevel()
        {
            return MapSeenLevel;
        }

        /// <inheritdoc />
        public void SaveMapSeenLevel(int level)
        {
            int seen = ProgressRules.NextMapSeen(MapSeenLevel, level, MaxLevel);
            if (seen == MapSeenLevel)
            {
                return;
            }

            _mapSeenLevel = seen;
            SaveCount++;
        }

        private static int ClampSeen(int value, int maxLevel)
        {
            return value < 0 ? 0 : Math.Min(value, Math.Max(maxLevel, 0));
        }
    }

    /// <summary>
    /// Reglas compartidas por los dos almacenes de progreso, sin asignaciones: la pertenencia a una lista de niveles
    /// (<c>skipped_levels</c>) y la monotonía de <c>map_seen_level</c>.
    /// </summary>
    internal static class ProgressRules
    {
        /// <summary><paramref name="list"/> contiene <paramref name="level"/>; recorre por índice, sin enumerador.</summary>
        public static bool Contains(List<int> list, int level)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == level)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// El <c>map_seen_level</c> que queda al ver el Mapa hasta <paramref name="level"/>: máx(<paramref name="current"/>,
        /// mín(<paramref name="level"/>, <paramref name="maxLevel"/>)). Nunca baja.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> es negativo.</exception>
        public static int NextMapSeen(int current, int level, int maxLevel)
        {
            if (level < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "map_seen_level no es negativo (08 §4.3).");
            }

            return Math.Max(current, Math.Min(level, maxLevel));
        }
    }
}
