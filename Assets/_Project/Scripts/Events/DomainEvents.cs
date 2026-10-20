#nullable enable
using System;

namespace AF.Events
{
    // Eventos de dominio del bus (08 §2.1, principio 3; D-033, Q-T1 (a)). Solo datos simples: los publica el módulo que sabe
    // lo que pasó (AF.GameFlow, AF.Economy) y AF.Analytics los traduce a la taxonomía única de 08 §8.2. Los nombres de los
    // campos son los del juego, no los de Firebase: la traducción (y sus nombres exactos) vive en AF.Analytics.
    // Los textos son ids (08 §8.1; .claude/rules/localizacion.md: «la analítica lleva claves o ID, nunca texto»).

    /// <summary>
    /// Empieza un intento de un nivel de campaña o del Reto: al terminar Llenar y teñir (08 §8.2, <c>level_start</c>). Lo
    /// publica <c>LevelFlow</c>.
    /// </summary>
    public readonly struct LevelStarted
    {
        /// <summary>Crea el evento.</summary>
        public LevelStarted(int level, string levelName, string variant, int band, int attempt, int world, string difficulty, bool murky)
        {
            Level = level;
            LevelName = levelName ?? throw new ArgumentNullException(nameof(levelName));
            Variant = variant ?? throw new ArgumentNullException(nameof(variant));
            Band = band;
            Attempt = attempt;
            World = world;
            Difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
            Murky = murky;
        }

        /// <summary>Número del nivel (en el Reto, el nivel efectivo de la partida).</summary>
        public int Level { get; }

        /// <summary><c>id</c> del nivel de 08 §4.1 (<c>m{mm}_v{v}_n{nnnn}</c> o, en el Reto, <c>d{b}_n{nnnn}</c>).</summary>
        public string LevelName { get; }

        /// <summary><c>base</c>, <c>plus_empty</c> o <c>minus_color</c> (la variante <c>relief[]</c> servida).</summary>
        public string Variant { get; }

        /// <summary>Banda del Reto (1-3); 0 en la campaña.</summary>
        public int Band { get; }

        /// <summary>Intento del nivel, desde 1.</summary>
        public int Attempt { get; }

        /// <summary>Mundo del nivel.</summary>
        public int World { get; }

        /// <summary>Etiqueta: <c>normal</c>, <c>hard</c> o <c>very_hard</c>.</summary>
        public string Difficulty { get; }

        /// <summary>Nivel con Aguas turbias.</summary>
        public bool Murky { get; }
    }

    /// <summary>Victoria de un nivel (08 §8.2, <c>af_level_win</c> y <c>level_end</c> con <c>success</c> = true).</summary>
    public readonly struct LevelWon
    {
        /// <summary>Crea el evento.</summary>
        public LevelWon(
            int level, string levelName, int pours, int par, int undoUsed, string boostersJson, long timeMs, int rewardSeeds,
            bool doubleHarvest, bool rescued)
        {
            Level = level;
            LevelName = levelName ?? throw new ArgumentNullException(nameof(levelName));
            Pours = pours;
            Par = par;
            UndoUsed = undoUsed;
            BoostersJson = boostersJson ?? throw new ArgumentNullException(nameof(boostersJson));
            TimeMs = timeMs;
            RewardSeeds = rewardSeeds;
            DoubleHarvest = doubleHarvest;
            Rescued = rescued;
        }

        /// <summary>Número del nivel.</summary>
        public int Level { get; }

        /// <summary><c>id</c> del nivel (08 §4.1).</summary>
        public string LevelName { get; }

        /// <summary>Vertidos del intento ganado.</summary>
        public int Pours { get; }

        /// <summary>Par del nivel (0 si el paquete no lo trae).</summary>
        public int Par { get; }

        /// <summary>Mareas usadas en el intento.</summary>
        public int UndoUsed { get; }

        /// <summary>Comodines del intento, como objeto JSON <c>{"id":n,…}</c> (vacío: <c>{}</c>).</summary>
        public string BoostersJson { get; }

        /// <summary>Milisegundos desde el fin de Llenar y teñir hasta la victoria (el intento ganado).</summary>
        public long TimeMs { get; }

        /// <summary>Semillas de la Cosecha acreditadas en el instante de la victoria.</summary>
        public int RewardSeeds { get; }

        /// <summary>La Cosecha salió doblada en el instante de la victoria (x2 del río crecido).</summary>
        public bool DoubleHarvest { get; }

        /// <summary>El intento ganado llevaba la Cuia de rescate.</summary>
        public bool Rescued { get; }
    }

    /// <summary>Fallo de un nivel (04 §5.2; 08 §8.2, <c>af_level_fail</c> y, si es duro, <c>level_end</c> con success = false).</summary>
    public readonly struct LevelFailed
    {
        /// <summary>Crea el evento.</summary>
        public LevelFailed(
            int level, string levelName, bool hard, bool canoeLost, bool streakBroken, bool shieldUsed, bool reliefServed,
            int pours, long timeMs)
        {
            Level = level;
            LevelName = levelName ?? throw new ArgumentNullException(nameof(levelName));
            Hard = hard;
            CanoeLost = canoeLost;
            StreakBroken = streakBroken;
            ShieldUsed = shieldUsed;
            ReliefServed = reliefServed;
            Pours = pours;
            TimeMs = timeMs;
        }

        /// <summary>Número del nivel.</summary>
        public int Level { get; }

        /// <summary><c>id</c> del nivel (08 §4.1).</summary>
        public string LevelName { get; }

        /// <summary>Fallo duro (<c>kind</c> = <c>hard</c>); si no, blando.</summary>
        public bool Hard { get; }

        /// <summary>Se perdió una canoa.</summary>
        public bool CanoeLost { get; }

        /// <summary>Se rompió la Crecida.</summary>
        public bool StreakBroken { get; }

        /// <summary>La Hoja escudo salvó la Crecida.</summary>
        public bool ShieldUsed { get; }

        /// <summary>El siguiente intento llevará la variante aliviada («El río bajó»).</summary>
        public bool ReliefServed { get; }

        /// <summary>Vertidos del intento.</summary>
        public int Pours { get; }

        /// <summary>Milisegundos del intento.</summary>
        public long TimeMs { get; }
    }

    /// <summary>Salida sin resolver que no es fallo (08 §8.2, <c>af_level_abandon</c>).</summary>
    public readonly struct LevelAbandoned
    {
        /// <summary>Crea el evento.</summary>
        public LevelAbandoned(int level, string levelName, int pours, long timeMs, string reason)
        {
            Level = level;
            LevelName = levelName ?? throw new ArgumentNullException(nameof(levelName));
            Pours = pours;
            TimeMs = timeMs;
            Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        }

        /// <summary>Número del nivel.</summary>
        public int Level { get; }

        /// <summary><c>id</c> del nivel.</summary>
        public string LevelName { get; }

        /// <summary>Vertidos del intento.</summary>
        public int Pours { get; }

        /// <summary>Milisegundos del intento.</summary>
        public long TimeMs { get; }

        /// <summary><c>map</c>, <c>app_background</c> o <c>crash_resume</c>.</summary>
        public string Reason { get; }
    }

    /// <summary>Entran semillas en la cartera (08 §8.2, <c>earn_virtual_currency</c>). Lo publica <c>Wallet.Apply</c>.</summary>
    public readonly struct SeedsEarned
    {
        /// <summary>Crea el evento.</summary>
        public SeedsEarned(string source, int value, int balance)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Value = value;
            Balance = balance;
        }

        /// <summary>Fuente (<c>win</c>, <c>hard</c>, <c>streak</c>, <c>iap</c>…; <c>EconomyIds</c>).</summary>
        public string Source { get; }

        /// <summary>Semillas que entraron (&gt; 0).</summary>
        public int Value { get; }

        /// <summary>Saldo después.</summary>
        public int Balance { get; }
    }

    /// <summary>Salen semillas de la cartera (08 §8.2, <c>spend_virtual_currency</c>). Lo publica <c>Wallet.Apply</c>.</summary>
    public readonly struct SeedsSpent
    {
        /// <summary>Crea el evento.</summary>
        public SeedsSpent(string itemName, int value, int balance)
        {
            ItemName = itemName ?? throw new ArgumentNullException(nameof(itemName));
            Value = value;
            Balance = balance;
        }

        /// <summary>Sumidero (<c>undo_refill</c>, <c>continue_cuia</c>…; <c>EconomyIds</c>).</summary>
        public string ItemName { get; }

        /// <summary>Semillas que salieron (&gt; 0).</summary>
        public int Value { get; }

        /// <summary>Saldo después.</summary>
        public int Balance { get; }
    }
}
