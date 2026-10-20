#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using AF.Platform;

namespace AF.Economy
{
    /// <summary>
    /// Cifras de la Canasta (06 §4.4; 05 §2.1) con los defaults compilados de 08 §9. El nivel de desbloqueo
    /// (<c>live_unlock_level_daily</c>) no está aquí: su única fuente es <c>DailyChallengeConfig</c> (<c>AF.LiveOps</c>), y
    /// <c>BasketRules.For</c> lo recibe como argumento.
    /// </summary>
    public sealed class BasketConfig
    {
        private static readonly string[] DefaultCycle = { "undo3", "hint", "extra_cuia", "undo3", "hint" };

        /// <summary>Una instancia nueva con los defaults de 08 §9 (cada llamada devuelve otra).</summary>
        public static BasketConfig Defaults => new BasketConfig();

        /// <summary>
        /// <c>eco_basket_cycle</c>: comodín de cada Canasta desde el nivel 20, en ciclo fijo (06 §4.4). Cada ficha es el id del
        /// comodín (<c>undo</c>, <c>hint</c>, <c>reveal</c>, <c>extra_cuia</c>, <c>shuffle</c>) con unidades opcionales:
        /// <c>undo3</c> = 3 Mareas.
        /// </summary>
        [RemoteConfigKey("eco_basket_cycle")]
        public IReadOnlyList<string> EcoBasketCycle { get; set; } = (string[])DefaultCycle.Clone();

        /// <summary><c>eco_basket_seeds</c>: semillas de cada Canasta (también la primera y la del hito 6 de la Crecida).</summary>
        [RemoteConfigKey("eco_basket_seeds")]
        public int EcoBasketSeeds { get; set; } = 50;

        /// <summary>Comprueba que los valores están en los rangos de 08 §9 y que cada ficha del ciclo se entiende.</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            Require(EcoBasketSeeds >= 30 && EcoBasketSeeds <= 50, "eco_basket_seeds debe estar en 30-50");
            IReadOnlyList<string>? cycle = EcoBasketCycle;
            Require(cycle != null && cycle.Count > 0, "eco_basket_cycle no puede estar vacío");
            for (int i = 0; i < cycle!.Count; i++)
            {
                Require(TryParseToken(cycle[i], out _, out _), "ficha de eco_basket_cycle desconocida: «" + cycle[i] + "»");
            }
        }

        /// <summary>
        /// Lee el JSON de Remote Config (<c>["undo3","hint",…]</c>). Si no se puede leer, está vacío o tiene una ficha
        /// desconocida, devuelve false y deja <paramref name="cycle"/> con el default de 08 §9.
        /// </summary>
        public static bool TryParseCycle(string? json, out IReadOnlyList<string> cycle)
        {
            cycle = (string[])DefaultCycle.Clone();
            if (!EconomyJsonLite.TryReadStringArray(json, out List<string> items) || items.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (!TryParseToken(items[i], out _, out _))
                {
                    return false;
                }
            }

            cycle = items.ToArray();
            return true;
        }

        /// <summary>Escribe el ciclo con la sintaxis de 08 §9: <c>["undo3","hint","extra_cuia","undo3","hint"]</c>.</summary>
        public static string FormatCycle(IReadOnlyList<string> cycle)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < cycle.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                EconomyJsonLite.AppendString(sb, cycle[i]);
            }

            return sb.Append(']').ToString();
        }

        /// <summary>
        /// Lee una ficha de <c>eco_basket_cycle</c>: el id de un comodín con stock (<see cref="BoosterKinds.ToId"/>) seguido, si
        /// acaso, de sus unidades (1-99): <c>hint</c> = 1 Morpho, <c>undo3</c> = 3 Mareas. No asigna memoria.
        /// </summary>
        public static bool TryParseToken(string? token, out BoosterKind kind, out int units)
        {
            kind = BoosterKind.Undo;
            units = 0;
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            for (int k = 0; k < BoosterKinds.Count; k++)
            {
                string id = ((BoosterKind)k).ToId();
                if (!token!.StartsWith(id, StringComparison.Ordinal))
                {
                    continue;
                }

                int rest = token.Length - id.Length;
                if (rest == 0)
                {
                    kind = (BoosterKind)k;
                    units = 1;
                    return true;
                }

                if (rest > 2)
                {
                    continue;
                }

                int n = 0;
                bool digits = true;
                for (int i = id.Length; i < token.Length; i++)
                {
                    char c = token[i];
                    if (c < '0' || c > '9')
                    {
                        digits = false;
                        break;
                    }

                    n = n * 10 + (c - '0');
                }

                if (digits && n >= 1)
                {
                    kind = (BoosterKind)k;
                    units = n;
                    return true;
                }
            }

            return false;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("BasketConfig: " + message + ".");
            }
        }
    }
}
