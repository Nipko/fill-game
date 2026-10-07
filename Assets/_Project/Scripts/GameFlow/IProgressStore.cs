#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Progreso mínimo del prototipo v0: el siguiente nivel que se juega. Provisional y fuera de <c>AF.Save</c>
    /// (docs/dev/prototipo-v0.md, pregunta 4 al dueño); el perfil real es el de GDD 08 §4.3.
    /// </summary>
    public interface IProgressStore
    {
        /// <summary>Siguiente nivel guardado, o 0 si no hay progreso.</summary>
        int LoadNextLevel();

        /// <summary>Guarda el siguiente nivel y lo deja persistido (sobrevive a un <c>force-stop</c>).</summary>
        void SaveNextLevel(int level);
    }

    /// <summary>Progreso en memoria (tests y Editor).</summary>
    public sealed class MemoryProgressStore : IProgressStore
    {
        /// <summary>Crea el almacén con un valor inicial (0 = sin progreso).</summary>
        public MemoryProgressStore(int nextLevel = 0)
        {
            NextLevel = nextLevel;
        }

        /// <summary>Valor guardado.</summary>
        public int NextLevel { get; private set; }

        /// <summary>Veces que se ha guardado.</summary>
        public int SaveCount { get; private set; }

        /// <inheritdoc />
        public int LoadNextLevel()
        {
            return NextLevel;
        }

        /// <inheritdoc />
        public void SaveNextLevel(int level)
        {
            NextLevel = level;
            SaveCount++;
        }
    }
}
