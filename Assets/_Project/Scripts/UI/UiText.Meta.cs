#nullable enable
using System;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de la meta de B14 (tanda 5): skins por hito, la Canasta y el Premio del mundo en el Mapa y «Datos del río» en Ajustes.
    /// Parcial de <see cref="UiText"/>; todas las claves son nuevas en <c>strings.csv</c> [P] (sin fila en el glosario §10: pendientes
    /// de registrar). Los que asignan su cadena lo hacen al componer una pantalla, nunca por fotograma.
    /// </summary>
    public static partial class UiText
    {
        // La clave del nombre de una skin, skin_<id>_name [P] (una por skin en strings.csv; no es una clave de Keys).
        private const string SkinNamePrefix = "skin_";
        private const string SkinNameSuffix = "_name";

        /// <summary>
        /// Nombre visible de la skin <paramref name="skinId"/> (<c>skin_&lt;id&gt;_name</c> [P]: «Cuia de Santarém»…; glosario §4),
        /// pendiente del asesor; «» si la clave no tiene texto en el idioma de la UI.
        /// </summary>
        public static string SkinName(string skinId)
        {
            string key = SkinNameKey(skinId);
            return Loc.Has(key) ? Loc.T(key) : string.Empty;
        }

        /// <summary>Clave del nombre de la skin <paramref name="skinId"/>: <c>skin_&lt;id&gt;_name</c> [P].</summary>
        public static string SkinNameKey(string skinId)
        {
            return SkinNamePrefix + (skinId ?? throw new ArgumentNullException(nameof(skinId))) + SkinNameSuffix;
        }

        /// <summary>Las claves de los nombres de las skins de la 1.0: la base y las de <see cref="AF.Economy.SkinRewards.Milestones"/>.</summary>
        public static System.Collections.Generic.IEnumerable<string> SkinNameKeys
        {
            get
            {
                yield return SkinNameKey(AF.Economy.SkinRewards.BaseSkinId);
                for (int i = 0; i < AF.Economy.SkinRewards.Milestones.Count; i++)
                {
                    yield return SkinNameKey(AF.Economy.SkinRewards.Milestones[i].SkinId);
                }
            }
        }

        /// <summary>
        /// <c>skin_equipped_toast</c> [P] (D-033, Q-M8 (a): «la skin ganada se pone sola, con aviso»): «{skin} · puesta», con el nombre
        /// <paramref name="skin"/> de la skin.
        /// </summary>
        public static string SkinEquippedToast(string skin)
        {
            return Format(Keys.SkinEquippedToast, LocArgs.Of("skin", skin ?? throw new ArgumentNullException(nameof(skin))));
        }

        /// <summary><c>map_card_basket</c> [P] (06 §4.4-§4.5): «Canasta», la fila con el contenido de la Canasta de una parada futura.</summary>
        public static string MapCardBasket => Loc.T(Keys.MapCardBasket);

        /// <summary>
        /// <c>map_world_prize</c> [P] (06 §4.5; D-013, punto 10: «la skin sale como Premio del mundo · faltan N, sin precio»): con
        /// los <paramref name="levels"/> niveles que faltan para cerrar el mundo, sin separador de miles.
        /// </summary>
        public static string MapWorldPrize(int levels)
        {
            return Format(Keys.MapWorldPrize, LocArgs.OfDigits("n", NonNegative(levels)));
        }

        /// <summary>
        /// <c>settings_river_facts</c> [P] (07 §8.2: «una sección Sobre la Amazonía en Ajustes con la fuente de cada Dato del río»):
        /// «Datos del río», el subtítulo de la lista de los datos de llegada vistos, cada uno con su fuente.
        /// </summary>
        public static string SettingsRiverFacts => Loc.T(Keys.SettingsRiverFacts);

        /// <summary>Claves de <c>strings.csv</c> (parcial de <see cref="UiText.Keys"/>).</summary>
        public static partial class Keys
        {
            /// <summary><c>skin_equipped_toast</c> [P] (B14).</summary>
            public const string SkinEquippedToast = "skin_equipped_toast";

            /// <summary><c>map_card_basket</c> [P] (B14).</summary>
            public const string MapCardBasket = "map_card_basket";

            /// <summary><c>map_world_prize</c> [P] (B14).</summary>
            public const string MapWorldPrize = "map_world_prize";

            /// <summary><c>settings_river_facts</c> [P] (B14).</summary>
            public const string SettingsRiverFacts = "settings_river_facts";
        }
    }
}
