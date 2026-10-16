#nullable enable
using System;
using AF.Core.Mechanics;
using AF.Economy;
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>Qué tarjeta previa toca (02 §10; propuesta de D-009 §c.1).</summary>
    public enum LevelCardKind
    {
        /// <summary>Nivel Difícil o Muy difícil sin estreno: rana o jaguar, etiqueta, premio y cinta ×3/×4.</summary>
        Difficulty = 0,

        /// <summary>Estreno de una mecánica (<c>intro_mechanic</c>) en un nivel normal: el tutorial hace de tarjeta.</summary>
        Debut = 1,

        /// <summary>Las dos cosas a la vez (nivel 100: Muy difícil, estreno de Aguas turbias y Luciérnaga regalada).</summary>
        Combined = 2,
    }

    /// <summary>
    /// Contenido de la tarjeta previa de 1 toque (02 §10; propuesta de D-009 §c.1; 04 §2.5), inmutable. La arma
    /// <see cref="LevelFlow"/> al cargar el nivel (<see cref="For"/>) y la recibe la vista con
    /// <see cref="ILevelCardListener.OnLevelCard"/>; la pinta T13. El premio y la cinta salen de la misma fuente que la
    /// Cosecha de la victoria (<see cref="EconomyConfig.WinReward"/>): 60 y ×3 en Difícil, 80 y ×4 en Muy difícil.
    /// </summary>
    public sealed class LevelCardInfo
    {
        /// <summary><c>intro_mechanic</c> del estreno de la Liana de cuenta (243; firma de <c>pack_schema</c> 3) [P].</summary>
        public const string LianaCountIntro = "liana_n";

        /// <summary><c>intro_mechanic</c> del estreno de la Hoja atada (283; firma de <c>pack_schema</c> 3).</summary>
        public const string HojaAtadaIntro = "hoja_atada";

        private readonly int _mechanics;

        /// <summary>Crea la tarjeta (en el juego, <see cref="For"/>).</summary>
        /// <param name="level">Número de campaña del nivel.</param>
        /// <param name="label"><c>difficulty.label</c>: <c>normal</c>, <c>hard</c> o <c>very_hard</c>.</param>
        /// <param name="rewardSeeds">Cosecha del nivel (<c>eco_reward_*</c>).</param>
        /// <param name="ribbonMultiplier">Cinta ×N (1 = sin cinta).</param>
        /// <param name="introMechanic"><c>intro_mechanic</c> (D-007), o null.</param>
        /// <param name="mechanicMask">Bit <c>(int)type</c> = el nivel trae esa mecánica al empezar.</param>
        /// <param name="hasMud">Alguna cuia lleva Barro (02 §9.1 M5).</param>
        /// <param name="hasSmallCuia">Alguna cuia es una Cuia chica (02 §8).</param>
        /// <param name="hasTallCuia">Alguna cuia es una Cuia alta (02 §8).</param>
        /// <param name="isMurky"><c>murky</c> del nivel: Aguas turbias (02 §11; 07 §7.7).</param>
        /// <param name="revealGifted">Al cargar este nivel se regaló una Luciérnaga (04 §2.5).</param>
        /// <exception cref="ArgumentException">Sin etiqueta y sin estreno no hay tarjeta, o la etiqueta no es de 08 §4.1.</exception>
        public LevelCardInfo(
            int level,
            string label,
            int rewardSeeds,
            int ribbonMultiplier,
            string? introMechanic,
            int mechanicMask,
            bool hasMud,
            bool hasSmallCuia,
            bool hasTallCuia,
            bool isMurky,
            bool revealGifted)
        {
            if (!RewardTiers.TryParse(label, out RewardTier tier))
            {
                throw new ArgumentException("Etiqueta de dificultad desconocida: " + label + ".", nameof(label));
            }

            bool labeled = tier != RewardTier.Normal;
            bool debut = !string.IsNullOrEmpty(introMechanic);
            if (!labeled && !debut)
            {
                throw new ArgumentException("Un nivel normal sin estreno no tiene tarjeta previa (02 §10).", nameof(label));
            }

            Level = level;
            Label = string.IsNullOrEmpty(label) ? LevelDefinition.LabelNormal : label;
            Tier = tier;
            RewardSeeds = rewardSeeds;
            RibbonMultiplier = ribbonMultiplier < 1 ? 1 : ribbonMultiplier;
            IntroMechanic = debut ? introMechanic : null;
            _mechanics = mechanicMask;
            HasMud = hasMud;
            HasSmallCuia = hasSmallCuia;
            HasTallCuia = hasTallCuia;
            IsMurky = isMurky;
            RevealGifted = revealGifted;
            Kind = labeled && debut ? LevelCardKind.Combined : labeled ? LevelCardKind.Difficulty : LevelCardKind.Debut;
        }

        /// <summary>Número de campaña del nivel (con <c>af_level_offset</c>, el desplazado).</summary>
        public int Level { get; }

        /// <summary><c>difficulty.label</c> del nivel: <c>normal</c>, <c>hard</c> o <c>very_hard</c> (08 §4.1).</summary>
        public string Label { get; }

        /// <summary>Etiqueta como <see cref="RewardTier"/>.</summary>
        public RewardTier Tier { get; }

        /// <summary>«Premio: N»: la Cosecha del nivel (<c>eco_reward_*</c>: 20, 60 u 80).</summary>
        public int RewardSeeds { get; }

        /// <summary>
        /// Cinta «×N» sobre la Cosecha normal (propuesta de D-009 §c.2): 3 en Difícil y 4 en Muy difícil; 1 (sin cinta) en un
        /// nivel normal o si el premio no es un múltiplo exacto del normal (no se anuncia un multiplicador que no se paga).
        /// </summary>
        public int RibbonMultiplier { get; }

        /// <summary><c>intro_mechanic</c> que estrena este nivel (D-007: <c>chica</c>, <c>alta</c>, <c>hoja</c>…), o null.</summary>
        public string? IntroMechanic { get; }

        /// <summary>
        /// Clave del icono grande del estreno (<see cref="IntroIconKeyOf"/>): la de <see cref="IntroMechanic"/>, salvo en la Liana de
        /// cuenta, que no es un tipo propio y lleva el de la Liana. Null sin estreno.
        /// </summary>
        public string? IntroIconKey => IntroIconKeyOf(IntroMechanic);

        /// <summary>Bit <c>(int)type</c> por cada mecánica que el nivel trae al empezar (para los iconos [P] de §c.1).</summary>
        public int MechanicMask => _mechanics;

        /// <summary>Alguna cuia lleva Barro (02 §9.1 M5).</summary>
        public bool HasMud { get; }

        /// <summary>Alguna cuia es una Cuia chica (capacidad menor que 4, 02 §8).</summary>
        public bool HasSmallCuia { get; }

        /// <summary>Alguna cuia es una Cuia alta (capacidad mayor que 4, 02 §8).</summary>
        public bool HasTallCuia { get; }

        /// <summary>
        /// Nivel de Aguas turbias (<see cref="LevelDefinition.IsMurky"/>): chip gris-pardo «?» (02 §11; 07 §7.7). Es la misma
        /// regla que el chip del HUD (<c>flow.Level.IsMurky</c>), así que los dos coinciden también en la variante aliviada,
        /// cuyo tablero no tiene ocultas: el modificador es del nivel, no del tablero servido.
        /// </summary>
        public bool IsMurky { get; }

        /// <summary>Al cargar este nivel se regaló una Luciérnaga (04 §2.5): la tarjeta enseña «+1 Luciérnaga».</summary>
        public bool RevealGifted { get; }

        /// <summary>Tipo de tarjeta: dificultad, estreno o las dos cosas.</summary>
        public LevelCardKind Kind { get; }

        /// <summary>El nivel trae la mecánica <paramref name="type"/> al empezar (<see cref="MechanicType.None"/> nunca).</summary>
        public bool HasMechanic(MechanicType type)
        {
            return type != MechanicType.None && MechanicTypes.IsDefined(type) && ((_mechanics >> (int)type) & 1) != 0;
        }

        /// <summary>
        /// Clave del icono grande de la tarjeta del estreno <paramref name="introMechanic"/> (02 §9.1 y §10): la misma clave para
        /// las mecánicas y las capacidades (<c>hoja</c>, <c>chica</c>…) y para la Hoja atada (<c>hoja_atada</c>, que es su propio
        /// tipo, <see cref="MechanicType.HojaAtada"/>, con su propio icono); la Liana de cuenta (<c>liana_n</c> [P], 243) es la
        /// Liana con <c>n</c>, así que lleva el icono de la Liana [P] y la distingue su texto. Null sin estreno.
        /// </summary>
        public static string? IntroIconKeyOf(string? introMechanic)
        {
            if (string.IsNullOrEmpty(introMechanic))
            {
                return null;
            }

            return string.Equals(introMechanic, LianaCountIntro, StringComparison.Ordinal) ? MechanicTypes.ToKey(MechanicType.Liana) : introMechanic;
        }

        /// <summary>El nivel tiene tarjeta previa: Difícil o Muy difícil, o estreno de una mecánica (02 §10; §c.1).</summary>
        public static bool Shows(LevelDefinition level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            return !string.IsNullOrEmpty(level.IntroMechanic) || (RewardTiers.TryParse(level.DifficultyLabel, out RewardTier tier) && tier != RewardTier.Normal);
        }

        /// <summary>
        /// Tarjeta del nivel <paramref name="level"/> con el número de campaña <paramref name="levelNumber"/>, o null si no
        /// tiene (<see cref="Shows"/>). El premio sale de <paramref name="economy"/>, como la Cosecha; el chip «?» sale de
        /// <see cref="LevelDefinition.IsMurky"/>, como el del HUD.
        /// </summary>
        public static LevelCardInfo? For(LevelDefinition level, int levelNumber, EconomyConfig economy, bool revealGifted)
        {
            if (economy == null)
            {
                throw new ArgumentNullException(nameof(economy));
            }

            if (!Shows(level))
            {
                return null;
            }

            RewardTiers.TryParse(level.DifficultyLabel, out RewardTier tier);
            int mechanics = 0;
            for (int t = 1; t <= (int)MechanicTypes.Last; t++)
            {
                if (level.HasMechanic((MechanicType)t))
                {
                    mechanics |= 1 << t;
                }
            }

            return new LevelCardInfo(
                levelNumber,
                level.DifficultyLabel,
                economy.WinReward(tier),
                RibbonFor(economy, tier),
                level.IntroMechanic,
                mechanics,
                level.HasMud,
                level.HasSmallCuia,
                level.HasTallCuia,
                level.IsMurky,
                revealGifted);
        }

        /// <summary>
        /// Cinta «×N» de <paramref name="tier"/>: premio de la etiqueta entre el normal, solo si es un múltiplo exacto (con los
        /// defaults de 08 §9, 60/20 = 3 y 80/20 = 4); si no, 1.
        /// </summary>
        public static int RibbonFor(EconomyConfig economy, RewardTier tier)
        {
            if (economy == null)
            {
                throw new ArgumentNullException(nameof(economy));
            }

            int normal = economy.WinReward(RewardTier.Normal);
            int reward = economy.WinReward(tier);
            if (tier == RewardTier.Normal || normal <= 0 || reward <= normal || reward % normal != 0)
            {
                return 1;
            }

            return reward / normal;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "LevelCard(" + Level.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + Kind + ", " + Label
                + (IntroMechanic == null ? string.Empty : ", " + IntroMechanic) + ")";
        }
    }
}
