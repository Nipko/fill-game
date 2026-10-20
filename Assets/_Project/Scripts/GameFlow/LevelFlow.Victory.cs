#nullable enable
using System;
using AF.Economy;

namespace AF.GameFlow
{
    /// <summary>
    /// Victoria con la cadena corta de D-008 (punto 3; 02 §2 y §6; tarea B2-4): la Cosecha (20, 60 u 80 semillas según la
    /// etiqueta, <c>eco_reward_*</c>) se acredita con <see cref="Transaction.Win"/> en el instante lógico de la victoria, con el
    /// progreso, para que cerrar la app durante la celebración no la pierda (02 §14: «sin pérdida de recompensa»); desde el 11
    /// (<c>ads_rv_double_harvest_from_level</c>), la Cosecha doble por video (<c>rv_double_harvest</c>; 04 §2.8); y
    /// «Siguiente» → AD_GATE (<see cref="IAdGate"/>) → N + 1. Sin auto-avance por tiempo (<c>victory_autonext_s</c> = 0); tras
    /// cobrar la Cosecha doble, la vista avanza sola por el mismo camino que «Siguiente» (D-011, punto 4), y AD_GATE no enseña
    /// el intersticial porque <c>AdPolicy</c> lo bloquea tras un recompensado. El calificativo «¡Perfecto!» (<see cref="IsPerfect"/>;
    /// Magic Sort §10.2 #25, A, D-029; tarea DC14) sale solo si el intento ganado no llevó la Cuia de rescate.
    /// </summary>
    public sealed partial class LevelFlow
    {
        private int _harvestBase;
        private int _harvest;
        private RewardTier _harvestTier;
        private bool _harvestDoubled;
        private bool _perfect;

        /// <summary>Anuncios de la cadencia de intersticiales (AD_GATE); null = sin anuncios (tests del flujo).</summary>
        public IAdGate? AdGate { get; set; }

        /// <summary>Semillas de la Cosecha acreditadas en la última victoria (con la Cosecha doble si se vio el video); 0 sin cartera.</summary>
        public int HarvestSeeds => _harvest;

        /// <summary>Cosecha de la etiqueta del nivel ganado (antes de doblarla).</summary>
        public int HarvestBase => _harvestBase;

        /// <summary>Etiqueta del nivel ganado.</summary>
        public RewardTier HarvestTier => _harvestTier;

        /// <summary>La Cosecha ya se dobló en esta victoria.</summary>
        public bool HarvestDoubled => _harvestDoubled;

        /// <summary>
        /// La última victoria es «¡Perfecto!» (Magic Sort §10.2 #25, recomendada A; D-029; tarea DC14): en el intento ganado el
        /// tablero no llevaba la Cuia de rescate, ni la de «Continuar: +1 cuia» ni la de la Balsa de rescate. Se evalúa por
        /// intento con las cuias añadidas del nivel en curso (<see cref="LevelAttempt.Rescues"/>), que persisten al reiniciar y
        /// se olvidan al salir del nivel: tras un rescate, Volver a la orilla sigue con esa cuia en el tablero y no es perfecto;
        /// salir al inicio y volver a entrar empieza sin ella. La Cuia extra, Marea, Morpho y la Luciérnaga no lo quitan, porque
        /// la pregunta solo nombra la Cuia de rescate [P]. False fuera de una victoria.
        /// </summary>
        public bool IsPerfect => _perfect;

        /// <summary>
        /// Victorias de campaña (<c>stats.levels_won</c>; <see cref="IProgressStore.LevelsWon"/>), contando la de ahora desde su
        /// instante lógico: la cadencia de las frases de TiTino en la victoria (D-016, punto 5).
        /// </summary>
        public int LevelsWon => _progress.LevelsWon;

        /// <summary>Nivel desde el que hay Cosecha doble (<c>ads_rv_double_harvest_from_level</c>, 11).</summary>
        public int DoubleHarvestFromLevel => _wallet?.Config.AdsRvDoubleHarvestFromLevel ?? EconomyConfig.Defaults.AdsRvDoubleHarvestFromLevel;

        /// <summary>
        /// La pantalla de victoria ofrece «Cosecha doble: x2 por video» (desde el 11, una vez por victoria). Si hay video lo
        /// decide la vista con <c>AF.Ads</c>: sin anuncio el botón no aparece (02 §6).
        /// </summary>
        public bool DoubleHarvestOffered => _wallet != null && Phase == FlowPhase.VictoryScreen && !_harvestDoubled && _harvestBase > 0
            && LevelNumber >= DoubleHarvestFromLevel;

        /// <summary>
        /// Recompensa de «Cosecha doble» (la vista ya vio el anuncio entero de <c>rv_double_harvest</c>): otra Cosecha igual
        /// (<see cref="Transaction.DoubleHarvest"/>). False si no se ofrecía o la cartera la rechazó (no cambia nada).
        /// </summary>
        public bool ApplyDoubleHarvest()
        {
            if (!DoubleHarvestOffered)
            {
                return false;
            }

            TransactionResult result = _wallet!.Apply(Transaction.DoubleHarvest(LevelNumber, _harvestTier));
            if (!result.IsApplied)
            {
                return false;
            }

            _harvestDoubled = true;
            _harvest += result.SeedsDelta;
            _listener.OnHarvestDoubled(_harvest);
            return true;
        }

        /// <summary>
        /// «Siguiente» en VICTORY_SCREEN (02 §2): AD_GATE con <see cref="AdGate"/> (el intersticial, si las reglas de 05 §6.3 lo
        /// permiten) y después <paramref name="onDone"/>, una sola vez (enseguida si no hay anuncio). False fuera de la victoria.
        /// </summary>
        public bool BeginAdGate(Action onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            if (Phase != FlowPhase.VictoryScreen)
            {
                return false;
            }

            Phase = FlowPhase.AdGate;
            _listener.OnAdGate();
            IAdGate? gate = AdGate;
            if (gate == null)
            {
                onDone();
                return true;
            }

            bool done = false;
            gate.Run(LevelNumber, () =>
            {
                if (done)
                {
                    return;
                }

                done = true;
                onDone();
            });
            return true;
        }

        // Instante lógico de la victoria (R12): el nivel en curso deja de estarlo, el progreso avanza, se cobra la Cosecha y
        // la victoria cuenta para la cadencia de intersticiales, todo en una sola escritura del perfil (SaveBatch; antes eran
        // cuatro con su fsync cada una). Sin lote, primero se borra level_state: así un cierre a mitad nunca reabre el tablero
        // de un nivel ya cobrado. Es fin de nivel (T-D): esa escritura espera al escritor de segundo plano y va en síncrono.
        private void ResolveWin()
        {
            using (Saves())
            {
                SaveBatch?.RequireDurable();
                StateStore?.Clear();
                _perfect = _attempt.Rescues == 0; // antes de nada: el intento ganado, con sus cuias añadidas (DC14)
                _attempt.Won();
                _progress.SaveWin(LevelNumber, LevelNumber + 1);
                ResetHarvest(_perfect);
                RewardTiers.TryParse(Level.DifficultyLabel, out _harvestTier);
                if (_wallet != null)
                {
                    TransactionResult paid = _wallet.Apply(Transaction.Win(LevelNumber, _harvestTier));
                    if (paid.IsApplied)
                    {
                        _harvestBase = paid.SeedsDelta;
                        _harvest = paid.SeedsDelta;
                    }
                }

                AdGate?.RecordWin(LevelNumber);
            }

            _listener.OnWin();
        }

        private void ResetHarvest(bool perfect = false)
        {
            _perfect = perfect;
            _harvestBase = 0;
            _harvest = 0;
            _harvestTier = RewardTier.Normal;
            _harvestDoubled = false;
        }
    }
}
