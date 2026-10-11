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
    /// «Siguiente» → AD_GATE (<see cref="IAdGate"/>, sin auto-avance) → N + 1. Sin el calificativo «¡Perfecto!» (pregunta
    /// 10.2 #25 abierta).
    /// </summary>
    public sealed partial class LevelFlow
    {
        private int _harvestBase;
        private int _harvest;
        private RewardTier _harvestTier;
        private bool _harvestDoubled;

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
                _attempt.Won();
                _progress.SaveWin(LevelNumber, LevelNumber + 1);
                ResetHarvest();
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

        private void ResetHarvest()
        {
            _harvestBase = 0;
            _harvest = 0;
            _harvestTier = RewardTier.Normal;
            _harvestDoubled = false;
        }
    }
}
