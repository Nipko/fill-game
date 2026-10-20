#nullable enable
using AF.Boosters;
using AF.Economy;

namespace AF.GameFlow
{
    /// <summary>Lo que dejó un Salto del Boto (<c>af_skip_used</c>, 04 §11: <c>fails_level</c>, <c>since_last_skip</c>, <c>level_tag</c>).</summary>
    public readonly struct SkipResult
    {
        /// <summary>Crea el resultado.</summary>
        public SkipResult(int level, int failsLevel, int sinceLastSkip, string levelTag, TransactionResult paid, StreakServiceResult? streak)
        {
            Level = level;
            FailsLevel = failsLevel;
            SinceLastSkip = sinceLastSkip;
            LevelTag = levelTag;
            Paid = paid;
            Streak = streak;
        }

        /// <summary>Nivel saltado.</summary>
        public int Level { get; }

        /// <summary><c>fails_level</c> al saltar.</summary>
        public int FailsLevel { get; }

        /// <summary>Niveles desde el salto anterior (<see cref="SkipOffer.LevelsSinceLastSkip"/>).</summary>
        public int SinceLastSkip { get; }

        /// <summary><c>difficulty_label</c> del nivel saltado (<c>level_tag</c>).</summary>
        public string LevelTag { get; }

        /// <summary>El cobro de <c>skip_seed_price</c>; si no se aplicó (sin saldo), el salto no se hizo.</summary>
        public TransactionResult Paid { get; }

        /// <summary>La Canasta del <c>10k</c> pagada por el salto (K9, E34), o null (sin servicio, o no era un <c>10k</c>).</summary>
        public StreakServiceResult? Streak { get; }

        /// <summary>El salto se hizo (cobrado, guardado y cargado el siguiente).</summary>
        public bool Applied => Paid.IsApplied;
    }

    /// <summary>
    /// Salto del Boto en el flujo del nivel (bloque B3 de la tanda 5; GDD 04 §2.6, §5.2 y §6.2 <c>SKIP</c>; 02 §2 SKIP_RESOLVE;
    /// nota <c>docs/dev/salto-del-boto.md</c>). Se ofrece con <see cref="SkipBooster"/> en Aguas estancadas (rama D), en la pausa
    /// y en el banner de la entrada del nivel; al confirmarlo (<see cref="Skip"/>), en una sola escritura del perfil: cobra 300
    /// semillas, paga la Canasta si el nivel es un <c>10k</c> (<see cref="OnSkipStreak"/>, E34, dentro del lote y antes de
    /// guardar el salto), marca el nivel como saltado y superado (<see cref="ISkipStore"/>) y carga el siguiente con
    /// <c>fails_level</c> = 0. No cuesta canoa ni toca la Crecida, y no es una victoria. Sin almacén (<see cref="UseSkip"/> sin
    /// llamar: tests del flujo, Reto del río) no se ofrece nunca.
    /// </summary>
    public sealed partial class LevelFlow
    {
        private ISkipStore? _skipStore;
        private SkipConfig _skipConfig = SkipConfig.Defaults;
        private SkipResult? _lastSkip;

        /// <summary>Almacén del Salto del Boto (<see cref="UseSkip"/>), o null.</summary>
        public ISkipStore? SkipStore => _skipStore;

        /// <summary>Cifras del Salto del Boto (08 §9).</summary>
        public SkipConfig SkipConfig => _skipConfig;

        /// <summary>Resultado del último <see cref="Skip"/> de este flujo (también rechazado), o null.</summary>
        public SkipResult? LastSkip => _lastSkip;

        /// <summary>
        /// La opción Salto del Boto ahora (04 §6.1 <c>skip_eligible</c> con el saldo): oculta fuera de INTRO, LEVEL_CARD, PLAYING
        /// (sin victoria pendiente) y STUCK_MODAL, sin almacén, sin cartera, en el Reto, en un nivel ya superado, sin nivel
        /// siguiente en el catálogo o sin canoas para empezarlo [P] (nota §3, Q-S4).
        /// </summary>
        public SkipOffer SkipOffer
        {
            get
            {
                if (_skipStore == null || _wallet == null || _session == null || _level == null || IsDaily || !SkipPhase
                    || LevelNumber <= _progress.LoadMaxLevel())
                {
                    return default;
                }

                int next = LevelNumber + 1;
                var input = new SkipInput
                {
                    Level = LevelNumber,
                    IsCampaign = true,
                    Skippable = _level.Skippable,
                    FailsLevel = _attempt.FailsLevel,
                    LastSkipLevel = SkipBooster.LastSkipLevel(_skipStore.SkippedLevels, LevelNumber),
                    HasNextLevel = _catalog.Contains(next) && _wallet.CanStartLevel(next),
                    Seeds = _wallet.Seeds,
                    Price = _wallet.Price(SeedSink.Skip),
                };
                return SkipBooster.Evaluate(input, _skipConfig);
            }
        }

        /// <summary>
        /// La confirmación avisa de que se pierde la Cuia extra aplicada en este nivel, sin reembolso (04 §2.6 c).
        /// </summary>
        public bool SkipLosesExtraCuia => _attempt.ExtraCuias > 0;

        private bool SkipPhase => Phase == FlowPhase.StuckModal || Phase == FlowPhase.Intro || Phase == FlowPhase.LevelCard
            || (Phase == FlowPhase.Playing && _pending != Outcome.Win);

        /// <summary>
        /// Punto de inyección: el arranque (<c>GameBootstrap.Skip.cs</c>) pasa el almacén sobre el perfil y, si llegan de Remote
        /// Config, las cifras. Null quita el Salto (no se ofrece). Vale desde el siguiente cálculo de <see cref="SkipOffer"/>.
        /// </summary>
        public void UseSkip(ISkipStore? store, SkipConfig? config = null)
        {
            SkipConfig c = config ?? SkipConfig.Defaults;
            c.Validate();
            _skipStore = store;
            _skipConfig = c;
            _lastSkip = null;
        }

        /// <summary>
        /// Salto del Boto confirmado (04 §6.2 <c>SKIP: saldo -= 300; markSkipped(level); since_last_skip = 0; load next level</c>):
        /// con la opción lista, en un solo lote del perfil (fin de nivel: síncrono, T-D) cobra <c>skip_seed_price</c>, llama a
        /// <see cref="OnSkipStreak"/> (la Canasta del <c>10k</c>, K9; antes de guardar el salto, que sube <c>max_level</c>),
        /// guarda el salto, borra el <c>level_state</c> y carga el nivel siguiente (sin AD_GATE: no es una victoria). Null si la
        /// opción no está lista ahora (ni cobra ni cambia nada); un resultado rechazado si la cartera no lo cobró.
        /// </summary>
        public SkipResult? Skip()
        {
            LastFunnelError = null;
            SkipOffer offer = SkipOffer;
            if (!offer.CanUse)
            {
                return null;
            }

            int level = LevelNumber;
            int fails = _attempt.FailsLevel;
            string tag = Level.DifficultyLabel;
            using (Saves())
            {
                SaveBatch?.RequireDurable();
                TransactionResult paid = SkipBooster.Pay(_wallet!);
                if (!paid.IsApplied)
                {
                    _lastSkip = new SkipResult(level, fails, offer.LevelsSinceLastSkip, tag, paid, null);
                    return _lastSkip;
                }

                StreakServiceResult? streak = OnSkipStreak(); // E34: dentro del lote y antes de guardar el salto
                _skipStore!.SaveSkip(level, level + 1);
                StateStore?.Clear();
                _lastSkip = new SkipResult(level, fails, offer.LevelsSinceLastSkip, tag, paid, streak);
                Load(level + 1); // LOADING(N+1), con fails_level = 0 (nivel nuevo), en la misma escritura
            }

            return _lastSkip;
        }
    }
}
