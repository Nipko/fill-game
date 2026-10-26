#nullable enable
using System;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de accesibilidad (bloque B9; 02 §3.1 C9 y §12; 07 §7.8): las etiquetas del lector de pantalla (TalkBack), el anuncio
    /// de la selección, los nombres de C9 y las filas de Ajustes «Reducir movimiento» y «Alto contraste». Solo claves, con
    /// <see cref="Loc"/>. Las claves nuevas entran en <c>strings.csv</c> con el parche <c>B9-loc.diff</c> (que las registra también
    /// en <c>LocTablesTests</c>); hasta entonces, en desarrollo se ve «[clave]» (los tests de la rama configuran su propia tabla).
    /// No es un parcial de <c>UiText</c> porque los <c>UiText*.cs</c> están reservados (encargo §3.2), como <c>AccountText</c>.
    /// <para>Los textos literales de 02 §12 son los ejemplos de su fila «Lector de pantalla» («cuia 3: açaí, açaí, aguaje, 1
    /// hueco», «turbia», «tinaja: camu camu en la boca, 3 dosis dentro», «su cuia chica: açaí, 1 hueco», «Deshacer, 2 restantes»
    /// y «açaí seleccionado: elige destino»); los demás son [P] (nota <c>docs/dev/accesibilidad.md</c> §5) y todos nacen en
    /// <c>borrador</c>. Los nombres de las frutas no son claves nuevas: son los de <c>facts.csv</c> (<c>tint.&lt;tinte&gt;.name</c>,
    /// <see cref="FruitAlbum.TintIds"/>), que esperan al asesor (07 §8.2).</para>
    /// <para>Las listas de dosis no se pegan en el código (<c>.claude/rules/localizacion.md</c>): se arman con el patrón de lista
    /// <see cref="Keys.List"/> («{list}, {item}», como los patrones de lista de CLDR), que decide el traductor.</para>
    /// <para>Asignan su cadena: solo cuando cambia el tablero o la pantalla y solo con el lector de pantalla activo (o al enseñar
    /// los nombres de C9), nunca por fotograma.</para>
    /// </summary>
    public static class A11yText
    {
        // tint.<tinte>.name de cada tinte por su número de motor (FruitAlbum.TintIds; glosario §9), preparadas una vez.
        private static readonly string[] NameKeys = BuildNameKeys();

        /// <summary>Todas las claves de <see cref="Keys"/> (registro de <c>LocTablesTests</c> y tests de la rama).</summary>
        public static readonly string[] AllKeys =
        {
            Keys.Cuia, Keys.CuiaFull, Keys.CuiaEmpty, Keys.List, Keys.Murky, Keys.Mud, Keys.Tinaja, Keys.TinajaEmpty,
            Keys.Cucharon, Keys.CucharonFull, Keys.CucharonEmpty, Keys.Selected, Keys.ButtonRemaining, Keys.NamesJoin,
            Keys.SettingsReduceMotion, Keys.SettingsHighContrast, Keys.FruitUnnamed, Keys.Close, Keys.TapToContinue,
            Keys.CuiaMechanic, Keys.MapJourney,
        };

        /// <summary>
        /// Prefijo de las claves de los nombres de las mecánicas en <c>facts.csv</c> (<c>mechanic.&lt;id&gt;</c>, las del glosario
        /// §6.5 y de <c>glossary.csv</c>; Q-B9-14).
        /// </summary>
        public const string MechanicNamePrefix = "mechanic.";

        /// <summary>
        /// Etiqueta de la cuia en la ranura <paramref name="slot"/> (1..n) con <paramref name="count"/> dosis, ya en lista en
        /// <paramref name="doses"/>, y <paramref name="holes"/> huecos. La forma (vacía, con huecos o llena) sale de las cifras, nunca
        /// del texto.
        /// </summary>
        public static string Cuia(int slot, string doses, int count, int holes)
        {
            if (count <= 0)
            {
                return Loc.T(Keys.CuiaEmpty, LocArgs.OfDigits("n", slot).AndDigits("holes", holes));
            }

            return holes > 0
                ? Loc.T(Keys.Cuia, LocArgs.OfDigits("n", slot).And("doses", doses).AndDigits("holes", holes))
                : Loc.T(Keys.CuiaFull, LocArgs.OfDigits("n", slot).And("doses", doses));
        }

        /// <summary>Etiqueta de la cuia chica de la Tinaja (M13; D-019): «su cuia chica: açaí, 1 hueco».</summary>
        public static string Cucharon(string doses, int count, int holes)
        {
            if (count <= 0)
            {
                return Loc.T(Keys.CucharonEmpty, LocArgs.OfDigits("holes", holes));
            }

            return holes > 0
                ? Loc.T(Keys.Cucharon, LocArgs.Of("doses", doses).AndDigits("holes", holes))
                : Loc.T(Keys.CucharonFull, LocArgs.Of("doses", doses));
        }

        /// <summary>Etiqueta de la Tinaja: «tinaja: camu camu en la boca, 3 dosis dentro»; sin dosis, «tinaja: vacía» [P].</summary>
        public static string Tinaja(string mouth, int doses)
        {
            return doses <= 0
                ? Loc.T(Keys.TinajaEmpty)
                : Loc.T(Keys.Tinaja, LocArgs.Of("fruit", mouth).AndDigits("n", doses));
        }

        /// <summary>Patrón de lista: <paramref name="list"/> seguida de <paramref name="item"/> («{list}, {item}»).</summary>
        public static string Append(string list, string item)
        {
            if (list.Length == 0)
            {
                return item;
            }

            return item.Length == 0 ? list : Loc.T(Keys.List, LocArgs.Of("list", list).And("item", item));
        }

        /// <summary>Patrón de los nombres de C9, uno por línea: <paramref name="list"/> y debajo <paramref name="item"/>.</summary>
        public static string AppendLine(string list, string item)
        {
            if (list.Length == 0)
            {
                return item;
            }

            return item.Length == 0 ? list : Loc.T(Keys.NamesJoin, LocArgs.Of("list", list).And("item", item));
        }

        /// <summary>Una dosis oculta (Aguas turbias, 02 §11): «turbia».</summary>
        public static string Murky => Loc.T(Keys.Murky);

        /// <summary>Una dosis de Barro (M5, 02 §9.1) [P]: «barro».</summary>
        public static string Mud => Loc.T(Keys.Mud);

        /// <summary>
        /// Nombre visible de la fruta <paramref name="tint"/> (1..14; <c>tint.&lt;tinte&gt;.name</c>) o, si no lo hay en este idioma
        /// o build (en release, mientras no lo apruebe el asesor), «fruta» [P] (<see cref="Keys.FruitUnnamed"/>; Q-B9-7): el lector
        /// nunca se salta una dosis ni deja una cuia llena como si estuviera vacía.
        /// </summary>
        public static string FruitName(int tint)
        {
            string name = tint >= 1 && tint < NameKeys.Length ? Loc.Fact(NameKeys[tint]) : string.Empty;
            return name.Length > 0 ? name : Loc.T(Keys.FruitUnnamed);
        }

        /// <summary>
        /// Etiqueta de una cuia con mecánica (08 §14.2; Q-B9-14 (a)): <paramref name="cuia"/> (la de <see cref="Cuia"/>) y detrás
        /// el nombre de la mecánica <paramref name="mechanic"/> («cuia 3: açaí, 1 hueco. Hoja»). Sin nombre (o si aún no se puede
        /// enseñar en este idioma o build), la etiqueta tal cual.
        /// </summary>
        public static string WithMechanic(string cuia, string mechanic)
        {
            if (cuia == null)
            {
                throw new ArgumentNullException(nameof(cuia));
            }

            return string.IsNullOrEmpty(mechanic) ? cuia : Loc.T(Keys.CuiaMechanic, LocArgs.Of("cuia", cuia).And("mechanic", mechanic));
        }

        /// <summary>
        /// Nombre visible de la mecánica <paramref name="id"/> (<c>mechanic.&lt;id&gt;</c> de <c>facts.csv</c>, glosario §6.5), o
        /// «» si no lo hay en este idioma o build (en release, mientras no lo apruebe el asesor; Q-B9-14).
        /// </summary>
        public static string MechanicName(string id)
        {
            return string.IsNullOrEmpty(id) ? string.Empty : Loc.Fact(MechanicNamePrefix + id);
        }

        /// <summary>Anuncio de la selección (02 §12): «açaí seleccionado: elige destino».</summary>
        public static string Selected(string fruit)
        {
            return Loc.T(Keys.Selected, LocArgs.Of("fruit", fruit ?? throw new ArgumentNullException(nameof(fruit))));
        }

        /// <summary>Descripción de un botón con su estado (02 §12): «Deshacer, 2 restantes».</summary>
        public static string ButtonRemaining(string label, int remaining)
        {
            return Loc.T(Keys.ButtonRemaining, LocArgs.Of("label", label ?? throw new ArgumentNullException(nameof(label))).AndDigits("n", remaining));
        }

        /// <summary>Etiqueta del lector de la X de cerrar de los modales y hojas [P]: «Cerrar».</summary>
        public static string Close => Loc.T(Keys.Close);

        /// <summary>Etiqueta del lector de un velo que se toca para seguir (tarjeta previa, banner del mundo) [P]: «Continuar».</summary>
        public static string TapToContinue => Loc.T(Keys.TapToContinue);

        /// <summary>Etiqueta del lector del área que cubre el Mapa mientras la canoa sube el tramo (D-053) [P].</summary>
        public static string MapJourney => Loc.T(Keys.MapJourney);

        /// <summary>Fila de Ajustes «Reducir movimiento» (07 §7.8).</summary>
        public static string SettingsReduceMotion => Loc.T(Keys.SettingsReduceMotion);

        /// <summary>Fila de Ajustes «Alto contraste» (07 §3, regla 5; 07 §7.8).</summary>
        public static string SettingsHighContrast => Loc.T(Keys.SettingsHighContrast);

        private static string[] BuildNameKeys()
        {
            var keys = new string[FruitAlbum.TintIds.Count];
            for (int i = 1; i < keys.Length; i++)
            {
                keys[i] = FactCatalog.NamePrefix + FruitAlbum.TintIds[i] + ".name";
            }

            keys[0] = string.Empty;
            return keys;
        }

        /// <summary>Claves.</summary>
        public static class Keys
        {
            /// <summary>«cuia {n}: {doses}, {holes:plural:…}» (02 §12).</summary>
            public const string Cuia = "a11y_cuia";

            /// <summary>«cuia {n}: {doses}», sin huecos [P].</summary>
            public const string CuiaFull = "a11y_cuia_full";

            /// <summary>«cuia {n}: {holes:plural:…}», vacía [P].</summary>
            public const string CuiaEmpty = "a11y_cuia_empty";

            /// <summary>«{cuia}. {mechanic}»: la etiqueta de la cuia con el nombre de su mecánica [P] (08 §14.2; Q-B9-14).</summary>
            public const string CuiaMechanic = "a11y_cuia_mechanic";

            /// <summary>Patrón de lista «{list}, {item}» [P].</summary>
            public const string List = "a11y_list";

            /// <summary>«Cerrar», la X de los modales [P].</summary>
            public const string Close = "a11y_close";

            /// <summary>«Continuar», un velo que se toca para seguir [P].</summary>
            public const string TapToContinue = "a11y_tap_to_continue";

            /// <summary>«Capi va río arriba. Toca para ir más rápido.», el área del Mapa durante el viaje [P] (D-053).</summary>
            public const string MapJourney = "a11y_map_journey";

            /// <summary>«fruta», una fruta cuyo nombre aún no se puede enseñar [P] (Q-B9-7).</summary>
            public const string FruitUnnamed = "a11y_fruit_unnamed";

            /// <summary>«turbia» (02 §12).</summary>
            public const string Murky = "a11y_murky";

            /// <summary>«barro» [P].</summary>
            public const string Mud = "a11y_mud";

            /// <summary>«tinaja: {fruit} en la boca, {n:plural:…}» (02 §12; D-019).</summary>
            public const string Tinaja = "a11y_tinaja";

            /// <summary>«tinaja: vacía» [P].</summary>
            public const string TinajaEmpty = "a11y_tinaja_empty";

            /// <summary>«su cuia chica: {doses}, {holes:plural:…}» (02 §12; D-019).</summary>
            public const string Cucharon = "a11y_cucharon";

            /// <summary>«su cuia chica: {doses}» [P].</summary>
            public const string CucharonFull = "a11y_cucharon_full";

            /// <summary>«su cuia chica: {holes:plural:…}» [P].</summary>
            public const string CucharonEmpty = "a11y_cucharon_empty";

            /// <summary>«{fruit} seleccionado: elige destino» (02 §12).</summary>
            public const string Selected = "a11y_selected";

            /// <summary>«{label}, {n:plural:…}» (02 §12: «Deshacer, 2 restantes»).</summary>
            public const string ButtonRemaining = "a11y_button_remaining";

            /// <summary>Nombres de C9, uno por línea: «{list}\n{item}» [P].</summary>
            public const string NamesJoin = "a11y_names_join";

            /// <summary>«Reducir movimiento» (07 §7.8; 02 §12).</summary>
            public const string SettingsReduceMotion = "settings_reduce_motion";

            /// <summary>«Alto contraste» (07 §3, regla 5; 02 §12; D-033, Q-N4).</summary>
            public const string SettingsHighContrast = "settings_high_contrast";
        }
    }
}
