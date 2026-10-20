#nullable enable
using System;
using System.Collections.Generic;
using AF.Save;

namespace AF.GameFlow
{
    /// <summary>
    /// Dónde guarda el Salto del Boto su resultado (04 §2.6; 02 §2 SKIP_RESOLVE; 08 §4.3 <c>progress</c>): el nivel queda en
    /// <c>skipped_levels</c> (el Mapa del río pone a Rosa en su boya), cuenta como superado (<c>max_level</c>), el siguiente es
    /// el que se juega (<c>next_level</c>) y <c>since_last_skip</c> vuelve a 0 (04 §6.2 <c>SKIP</c>). No es una victoria: no suma
    /// <c>stats.levels_won</c>. Va aparte de <see cref="IProgressStore"/> para no tocar esa interfaz (bloque B3; nota
    /// <c>docs/dev/salto-del-boto.md</c>). En el juego, <see cref="ProfileSkipStore"/> sobre el mismo perfil que el progreso y la
    /// cartera; en los tests, <see cref="MemorySkipStore"/>.
    /// </summary>
    public interface ISkipStore
    {
        /// <summary><c>progress.skipped_levels</c>: de aquí sale <c>last_skip_level</c> (<c>SkipBooster.LastSkipLevel</c>).</summary>
        IReadOnlyList<int> SkippedLevels { get; }

        /// <summary>
        /// Salto del nivel <paramref name="level"/>: lo añade a <c>skipped_levels</c> (una vez), <c>max_level</c> =
        /// máx(<c>max_level</c>, <paramref name="level"/>), <c>next_level</c> = <paramref name="nextLevel"/> y
        /// <c>since_last_skip</c> = 0, en un solo guardado (dentro del lote del flujo, la misma escritura que el cobro y la Canasta).
        /// </summary>
        void SaveSkip(int level, int nextLevel);
    }

    /// <summary>
    /// <see cref="ISkipStore"/> sobre el perfil (<see cref="Profile.Progress"/>), guardado con el mismo <see cref="IProfileStore"/>
    /// que <see cref="ProfileProgressStore"/> y la cartera (en el juego, <see cref="ProfileSaveBatch"/>). Si la escritura falla, el
    /// cambio sigue en memoria (<see cref="PendingSave"/>) y sale con el siguiente guardado del perfil.
    /// </summary>
    public sealed class ProfileSkipStore : ISkipStore
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;

        /// <summary>Crea el almacén sobre <paramref name="profile"/>, que se guarda en <paramref name="store"/>.</summary>
        public ProfileSkipStore(Profile profile, IProfileStore store)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>El último cambio no llegó al disco (escritura fallida o almacén protegido).</summary>
        public bool PendingSave { get; private set; }

        /// <inheritdoc />
        public IReadOnlyList<int> SkippedLevels => _profile.Progress.SkippedLevels;

        /// <inheritdoc />
        public void SaveSkip(int level, int nextLevel)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "Los niveles empiezan en 1.");
            }

            if (nextLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(nextLevel), nextLevel, "Los niveles empiezan en 1.");
            }

            ProgressData progress = _profile.Progress;
            if (!ProgressRules.Contains(progress.SkippedLevels, level))
            {
                progress.SkippedLevels.Add(level);
            }

            progress.MaxLevel = Math.Max(progress.MaxLevel, level);
            progress.NextLevel = nextLevel;
            progress.SinceLastSkip = 0;
            PendingSave = !_store.TrySave(_profile);
        }
    }

    /// <summary>
    /// <see cref="ISkipStore"/> en memoria (tests y Editor). Si se le pasa un <see cref="MemoryProgressStore"/>, le apunta el
    /// salto (<see cref="MemoryProgressStore.MarkSkipped"/> y <c>next_level</c>) para que el Mapa lo vea.
    /// </summary>
    public sealed class MemorySkipStore : ISkipStore
    {
        private readonly List<int> _skipped = new List<int>();
        private readonly MemoryProgressStore? _progress;

        /// <summary>Crea el almacén, opcionalmente enganchado a <paramref name="progress"/>.</summary>
        public MemorySkipStore(MemoryProgressStore? progress = null)
        {
            _progress = progress;
        }

        /// <summary>Saltos guardados.</summary>
        public int SaveCount { get; private set; }

        /// <summary><c>since_last_skip</c> tras el último salto (0), o −1 si no hubo ninguno.</summary>
        public int SinceLastSkip { get; private set; } = -1;

        /// <summary><c>max_level</c> que dejaría el salto.</summary>
        public int MaxLevel { get; private set; }

        /// <inheritdoc />
        public IReadOnlyList<int> SkippedLevels => _skipped;

        /// <inheritdoc />
        public void SaveSkip(int level, int nextLevel)
        {
            if (!ProgressRules.Contains(_skipped, level))
            {
                _skipped.Add(level);
            }

            MaxLevel = Math.Max(MaxLevel, level);
            SinceLastSkip = 0;
            SaveCount++;
            if (_progress != null)
            {
                _progress.MarkSkipped(level);
                _progress.SaveNextLevel(nextLevel);
            }
        }
    }
}
