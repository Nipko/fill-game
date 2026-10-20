#nullable enable
using System;
using AF.GameFlow;
using AF.LiveOps;
using AF.Platform;
using AF.Save;

namespace AF.UI
{
    /// <summary>
    /// <see cref="ILevelStateStore"/> de la partida del Reto (nota §3 [P]; R-Q4: se reanuda al reabrir la app el mismo día y se
    /// descarta otro día) sobre <see cref="IDailyChallengeStore"/>: guarda el <c>level_state</c> (04 §10.1) en
    /// <see cref="DailyPlayInProgress.State"/> del bloque <c>daily_challenge</c>, nunca en el <c>level_state</c> de la campaña. Vive
    /// en AF.UI porque es el único ensamblado que ve AF.GameFlow y AF.LiveOps (08 §2.1, principio 2).
    /// <list type="bullet">
    /// <item><see cref="Load"/>: el estado guardado de la partida de <b>hoy</b> (fecha local), o null si no hay partida, si es de
    /// otro día (se descarta al empezar: <see cref="DailyChallengeService.Start"/> / <see cref="DailyChallengeService.DiscardStale"/>)
    /// o si aún no se guardó ningún estado (<c>level</c> = 0: recién empezada).</item>
    /// <item><see cref="Save"/>: copia el estado en la partida en curso y guarda (tras cada vertido y, con R-Q16 (a), también al
    /// salir sin ganar: <c>LevelFlow.ExitDaily</c> guarda en síncrono y la partida queda reanudable). Sin partida en curso no
    /// hace nada (la partida se cerró con la victoria o se descartó por cambio de día).</item>
    /// <item><see cref="Clear"/> (victoria): deja la partida sin estado guardado pero <b>no la cierra</b>; la cierra
    /// <see cref="DailyChallengeService.ClaimWin"/>, que se llama justo después (<c>LevelFlow</c> borra el estado antes de avisar
    /// de la victoria).</item>
    /// </list>
    /// La fecha la decide <see cref="IClock"/>; nunca <c>DateTime.Now</c>.
    /// </summary>
    public sealed class DailyLevelStateStore : ILevelStateStore
    {
        private readonly IDailyChallengeStore _store;
        private readonly IClock _clock;

        /// <summary>Crea el almacén.</summary>
        public DailyLevelStateStore(IDailyChallengeStore store, IClock clock)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Guardados hechos (diagnóstico y tests).</summary>
        public int SaveCount { get; private set; }

        /// <summary>
        /// Partida guardada de otro día (R-Q4: se descarta). Con la app abierta la partida de ayer sigue (R-Q3); esto solo lo mira
        /// quien reabre.
        /// </summary>
        public bool HasStale
        {
            get
            {
                DailyPlayInProgress? play = _store.Load().InProgress;
                return play != null && !string.Equals(play.DateKey, _clock.LocalDateKey(), StringComparison.Ordinal);
            }
        }

        /// <inheritdoc />
        public LevelStateData? Load()
        {
            DailyPlayInProgress? play = _store.Load().InProgress;
            if (play == null || !string.Equals(play.DateKey, _clock.LocalDateKey(), StringComparison.Ordinal) || play.State.Level < 1)
            {
                return null;
            }

            return LevelStateSnapshot.Copy(play.State);
        }

        /// <inheritdoc />
        public void Save(LevelStateData state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            DailyChallengeRecord record = _store.Load();
            DailyPlayInProgress? play = record.InProgress;
            if (play == null)
            {
                return;
            }

            play.State = LevelStateSnapshot.Copy(state);
            _store.Save(record);
            SaveCount++;
        }

        /// <inheritdoc />
        public void Clear()
        {
            DailyChallengeRecord record = _store.Load();
            DailyPlayInProgress? play = record.InProgress;
            if (play == null || play.State.Level < 1)
            {
                return;
            }

            play.State = new LevelStateData { Seed = play.State.Seed };
            _store.Save(record);
            SaveCount++;
        }
    }
}
