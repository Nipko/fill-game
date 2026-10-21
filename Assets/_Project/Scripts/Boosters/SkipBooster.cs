#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.Platform;

namespace AF.Boosters
{
    /// <summary>
    /// Cifras del Salto del Boto con los <b>defaults compilados</b> de la tabla única de 08 §9 (fila
    /// <c>skip_seed_price / skip_min_fails / skip_min_gap_levels / skip_min_level</c>; 04 §2.6 y §12; dirección 9). El precio
    /// (<c>skip_seed_price</c>) es de <see cref="EconomyConfig"/>, como el resto de precios en semillas; aquí van las tres
    /// condiciones. <c>SkipBoosterTests.EveryDefault_IsTheOneOfThe08_9Table</c> las contrasta con 08 §9. Va aparte de <see cref="BoosterConfig"/> para no tocar ese
    /// archivo (bloque B3 de la tanda 5; nota <c>docs/dev/salto-del-boto.md</c>).
    /// </summary>
    public sealed class SkipConfig
    {
        /// <summary>Una instancia nueva con los defaults de 08 §9.</summary>
        public static SkipConfig Defaults => new SkipConfig();

        /// <summary><c>skip_min_fails</c>: fallos (duros y blandos, 04 §5.2) en el mismo nivel para ofrecer el salto (04 §2.6).</summary>
        [RemoteConfigKey("skip_min_fails")]
        public int SkipMinFails { get; set; } = 5;

        /// <summary><c>skip_min_gap_levels</c>: niveles desde el último salto (04 §2.6: «≥10 niveles desde el último salto»).</summary>
        [RemoteConfigKey("skip_min_gap_levels")]
        public int SkipMinGapLevels { get; set; } = 10;

        /// <summary><c>skip_min_level</c>: primer nivel con Salto del Boto (04 §2.6 y §12, punto 2).</summary>
        [RemoteConfigKey("skip_min_level")]
        public int SkipMinLevel { get; set; } = 31;

        /// <summary>Comprueba que las cifras tienen sentido (todas ≥ 1). No aplica los rangos de 08 §9: eso es de Remote Config.</summary>
        /// <exception cref="InvalidOperationException">Alguna cifra no es válida.</exception>
        public void Validate()
        {
            if (SkipMinFails < 1 || SkipMinGapLevels < 1 || SkipMinLevel < 1)
            {
                throw new InvalidOperationException("skip_min_fails, skip_min_gap_levels y skip_min_level deben ser ≥ 1.");
            }
        }
    }

    /// <summary>Estado de la opción Salto del Boto (modal, pausa y banner; 04 §2.6).</summary>
    public enum SkipState
    {
        /// <summary>No se ofrece: alguna de las condiciones de 04 §2.6 no se cumple (o fuera de la campaña).</summary>
        Hidden = 0,

        /// <summary>Elegible y con saldo: se puede pagar.</summary>
        Ready = 1,

        /// <summary>Elegible sin saldo: el precio en gris y «Te faltan {n} semillas» (04 §1, «Sin stock (solo semillas)»).</summary>
        Disabled = 2,
    }

    /// <summary>Lo que se sabe para decidir el Salto del Boto (04 §6.1 <c>skip_eligible</c>).</summary>
    public struct SkipInput
    {
        /// <summary>Nivel de campaña.</summary>
        public int Level;

        /// <summary>Es un nivel de campaña (el Reto del río y los eventos no admiten salto: 04 §2.6 e).</summary>
        public bool IsCampaign;

        /// <summary><c>skippable</c> del nivel (false en los estrenos; 04 §2.6).</summary>
        public bool Skippable;

        /// <summary><c>fails_level</c> (04 §5.2): fallos duros y blandos en este nivel.</summary>
        public int FailsLevel;

        /// <summary>Último nivel saltado (<c>last_skip_level</c>, 02 §2), o 0 si nunca se saltó ninguno.</summary>
        public int LastSkipLevel;

        /// <summary>El catálogo tiene el nivel siguiente (el salto «carga el siguiente»; 04 §2.6).</summary>
        public bool HasNextLevel;

        /// <summary>Saldo de semillas.</summary>
        public int Seeds;

        /// <summary>Precio (<c>skip_seed_price</c>).</summary>
        public int Price;
    }

    /// <summary>La opción Salto del Boto ya decidida.</summary>
    public readonly struct SkipOffer
    {
        /// <summary>Crea la oferta.</summary>
        public SkipOffer(SkipState state, int price, int missing, int levelsSinceLastSkip)
        {
            State = state;
            Price = price;
            Missing = missing;
            LevelsSinceLastSkip = levelsSinceLastSkip;
        }

        /// <summary>Estado.</summary>
        public SkipState State { get; }

        /// <summary>Precio en semillas (0 si no se ofrece).</summary>
        public int Price { get; }

        /// <summary>Semillas que faltan (0 si alcanza o no se ofrece).</summary>
        public int Missing { get; }

        /// <summary>
        /// Niveles desde el último salto (<c>since_last_skip</c> de <c>af_skip_used</c>, 04 §11): el nivel menos
        /// <c>last_skip_level</c>, o el propio nivel si nunca se saltó ninguno [P] (nota §3, Q-S1).
        /// </summary>
        public int LevelsSinceLastSkip { get; }

        /// <summary>Se ofrece (lista o en gris).</summary>
        public bool Offered => State != SkipState.Hidden;

        /// <summary>Se puede pagar ahora.</summary>
        public bool CanUse => State == SkipState.Ready;
    }

    /// <summary>
    /// Salto del Boto · Saltar nivel (<c>skip</c>; GDD 04 §2.6, §5.2, §6.1-§6.3 rama D; 02 §2 SKIP_RESOLVE; glosario §5.1):
    /// marca el nivel como superado sin recompensa y carga el siguiente, por <c>skip_seed_price</c> (300) semillas. Se ofrece
    /// solo con las tres condiciones de la dirección 9 a la vez, más <c>skippable</c> (04 §6.1):
    /// <c>fails_level ≥ skip_min_fails</c>, ≥ <c>skip_min_gap_levels</c> niveles desde el último salto y nivel ≥
    /// <c>skip_min_level</c>. No es un fallo: no cuesta canoa ni toca la Crecida (C11 de <c>docs/dev/crecida-canasta.md</c>), pero
    /// cuenta para la Canasta de cada 10 niveles (K9). Sin motor y sin asignaciones.
    /// </summary>
    public static class SkipBooster
    {
        /// <summary>
        /// <c>last_skip_level</c> (02 §2): el nivel más alto de <paramref name="skippedLevels"/> por debajo de
        /// <paramref name="level"/>, o 0 si no hay ninguno. Se deduce de <c>progress.skipped_levels</c> (08 §4.3), sin campo nuevo
        /// [P] (nota §3, Q-S1). Recorre por índice, sin enumerador.
        /// </summary>
        public static int LastSkipLevel(IReadOnlyList<int>? skippedLevels, int level)
        {
            int last = 0;
            if (skippedLevels == null)
            {
                return 0;
            }

            for (int i = 0; i < skippedLevels.Count; i++)
            {
                int s = skippedLevels[i];
                if (s < level && s > last)
                {
                    last = s;
                }
            }

            return last;
        }

        /// <summary>Niveles desde el último salto: <paramref name="level"/> − <paramref name="lastSkipLevel"/> (el nivel si es 0).</summary>
        public static int LevelsSince(int level, int lastSkipLevel)
        {
            return lastSkipLevel <= 0 ? level : level - lastSkipLevel;
        }

        /// <summary>
        /// <c>skip_eligible</c> de 04 §6.1, más dos condiciones del cliente: en la campaña (04 §2.6 e) y con nivel siguiente en el
        /// catálogo (sin él no hay adónde llevar al jugador) [P].
        /// </summary>
        public static bool IsEligible(in SkipInput input, SkipConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return input.IsCampaign
                && input.HasNextLevel
                && input.Skippable
                && input.Level >= config.SkipMinLevel
                && input.FailsLevel >= config.SkipMinFails
                && LevelsSince(input.Level, input.LastSkipLevel) >= config.SkipMinGapLevels;
        }

        /// <summary>La opción para <paramref name="input"/>: oculta, lista o en gris con lo que falta.</summary>
        public static SkipOffer Evaluate(in SkipInput input, SkipConfig config)
        {
            int since = LevelsSince(input.Level, input.LastSkipLevel);
            if (!IsEligible(input, config))
            {
                return new SkipOffer(SkipState.Hidden, 0, 0, since);
            }

            if (input.Seeds >= input.Price)
            {
                return new SkipOffer(SkipState.Ready, input.Price, 0, since);
            }

            return new SkipOffer(SkipState.Disabled, input.Price, input.Price - input.Seeds, since);
        }

        /// <summary>Cobra el salto (<c>skip_seed_price</c>, <see cref="SeedSink.Skip"/>; 04 §6.2 <c>SKIP: saldo -= 300</c>).</summary>
        public static TransactionResult Pay(Wallet wallet)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            return wallet.Apply(Transaction.Buy(SeedSink.Skip));
        }
    }
}
