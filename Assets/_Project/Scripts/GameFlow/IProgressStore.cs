#nullable enable
using System;

namespace AF.GameFlow
{
    /// <summary>
    /// Progreso de campaña: <c>progress.next_level</c> y <c>progress.max_level</c> del perfil (GDD 08 §4.3; 04 §10.1). En el
    /// juego lo guarda <see cref="ProfileProgressStore"/> en el perfil de <c>AF.Save</c> (tarea B1-5; antes, en v0, en
    /// PlayerPrefs con <c>af_v0_level</c>, que se migra una vez: <see cref="ProgressMigration"/>); en los tests,
    /// <see cref="MemoryProgressStore"/>.
    /// </summary>
    public interface IProgressStore
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
    }

    /// <summary>Progreso en memoria (tests y Editor).</summary>
    public sealed class MemoryProgressStore : IProgressStore
    {
        /// <summary>Crea el almacén con un valor inicial (0 = sin progreso).</summary>
        public MemoryProgressStore(int nextLevel = 0, int maxLevel = 0)
        {
            NextLevel = nextLevel;
            MaxLevel = maxLevel;
        }

        /// <summary>Siguiente nivel guardado.</summary>
        public int NextLevel { get; private set; }

        /// <summary>Nivel más alto superado.</summary>
        public int MaxLevel { get; private set; }

        /// <summary>Veces que se ha guardado.</summary>
        public int SaveCount { get; private set; }

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
    }
}
