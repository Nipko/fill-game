#nullable enable
using System;
using System.Globalization;
using AF.Levels;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos del banner de entrada a cada mundo (07 §2.2, regla transversal 1; 02 §6; clave <c>world_banner</c> del glosario
    /// §10: «Mundo {n}: {nombre} · {país} · {río}»), de <c>strings.csv</c>. Los mundos, países y ríos son los del glosario §8 tal
    /// cual, con las claves <c>world_wNN_name</c>, <c>world_wNN_country</c> y <c>world_wNN_river</c> [P] (NN = 01-13, como los
    /// ID <c>w01_estuario</c> … <c>w13_origen</c> del glosario §11): el país con su región entre paréntesis y el río sin el
    /// tipo de agua de la columna «Río (tipo de agua)», como el ejemplo literal de 06 §2.3 («Mundo 2: Tapajós · Brasil (Pará) ·
    /// Tapajós»). En el mundo 12, los ríos llevan su etiqueta «vecinos del Orinoco» (glosario §8). Todos los nombres de lugares
    /// quedan pendientes de revisión del asesor cultural (07 §8.2). Sin asignaciones tras la primera vez: cada texto se arma una
    /// vez y se guarda, y las cachés se vacían con <see cref="Loc.Changed"/> (cambio de idioma o de región de las cifras).
    /// <para>Idiomas, nivel 1 (D-018; tarea L1): cada texto es una plantilla con marcadores con nombre, sin pegar trozos. El
    /// marcador <c>{sep}</c> va donde la línea se puede partir; vale « · » (<see cref="WorldSeparator"/>) o un salto de línea,
    /// y el banner (<see cref="WorldBannerView"/>) elige cuál con <see cref="WorldPlaceLine"/>. La negrita del nombre, cuando
    /// abre la línea del lugar, va en su plantilla (<c>world_place_named_rich</c>).</para>
    /// </summary>
    public static partial class UiText
    {
        /// <summary>
        /// Separador de <c>world_banner</c> entre el nombre, el país y el río: lo que vale cada <c>{sep}</c> que no parte la línea.
        /// </summary>
        public const string WorldSeparator = Separator;

        private static readonly string[] WorldNameKeys = WorldKeys("name");
        private static readonly string[] WorldCountryKeys = WorldKeys("country");
        private static readonly string[] WorldRiverKeys = WorldKeys("river");

        private static readonly string?[] WorldTitles = new string?[WorldTable.WorldCount + 1];
        private static readonly string?[] WorldHeadings = new string?[WorldTable.WorldCount + 1];
        private static readonly string?[] WorldPlaces = new string?[WorldTable.WorldCount + 1];
        private static readonly string?[] WorldPlacesNamed = new string?[WorldTable.WorldCount + 1];
        private static readonly string?[] WorldBannerTexts = new string?[WorldTable.WorldCount + 1];

        /// <summary>Clave del nombre del mundo <paramref name="world"/> (1-13): <c>world_wNN_name</c>.</summary>
        public static string WorldNameKey(int world)
        {
            return WorldNameKeys[Check(world) - 1];
        }

        /// <summary>Clave del país del mundo <paramref name="world"/> (1-13): <c>world_wNN_country</c>.</summary>
        public static string WorldCountryKey(int world)
        {
            return WorldCountryKeys[Check(world) - 1];
        }

        /// <summary>Clave del río del mundo <paramref name="world"/> (1-13): <c>world_wNN_river</c>.</summary>
        public static string WorldRiverKey(int world)
        {
            return WorldRiverKeys[Check(world) - 1];
        }

        /// <summary>Nombre del mundo <paramref name="world"/> (1-13; glosario §8). Pendiente del asesor cultural.</summary>
        public static string WorldName(int world)
        {
            return Loc.T(WorldNameKey(world));
        }

        /// <summary>País del mundo <paramref name="world"/> (glosario §8, tal cual). Pendiente del asesor cultural.</summary>
        public static string WorldCountry(int world)
        {
            return Loc.T(WorldCountryKey(world));
        }

        /// <summary>Río del mundo <paramref name="world"/> (glosario §8, sin el tipo de agua). Pendiente del asesor cultural.</summary>
        public static string WorldRiver(int world)
        {
            return Loc.T(WorldRiverKey(world));
        }

        /// <summary>«Mundo {n}» (la cabecera cuando el nombre no cabe a 24 sp; <c>world_title</c>).</summary>
        public static string WorldTitle(int world)
        {
            int w = Check(world);
            return WorldTitles[w] ??= Format(Keys.WorldTitle, LocArgs.OfDigits("n", w));
        }

        /// <summary>
        /// «Mundo {n}: {nombre}»: la cabecera del banner, la parte de <c>world_banner</c> antes del primer corte
        /// (<c>world_heading</c>). Pendiente del asesor cultural.
        /// </summary>
        public static string WorldHeading(int world)
        {
            int w = Check(world);
            return WorldHeadings[w] ??= Format(Keys.WorldHeading, LocArgs.OfDigits("n", w).And("name", WorldName(w)));
        }

        /// <summary>
        /// «{país} · {río}»: la línea bajo la viñeta cuando la cabecera lleva el nombre (<see cref="WorldHeading"/>), en una
        /// línea (<c>world_place</c>). Pendiente del asesor cultural.
        /// </summary>
        public static string WorldPlace(int world)
        {
            int w = Check(world);
            return WorldPlaces[w] ??= Format(Keys.WorldPlace, WorldArgs(w));
        }

        /// <summary>
        /// «{nombre} · {país} · {río}»: la línea bajo la viñeta cuando la cabecera solo lleva «Mundo {n}» (<see cref="WorldTitle"/>),
        /// en una línea y sin la negrita (<c>world_place_named</c>). Pendiente del asesor cultural.
        /// </summary>
        public static string WorldPlaceNamed(int world)
        {
            int w = Check(world);
            return WorldPlacesNamed[w] ??= Format(Keys.WorldPlaceNamed, WorldArgs(w));
        }

        /// <summary>
        /// <c>world_banner</c> (glosario §10): «Mundo {n}: {nombre} · {país} · {río}», la cabecera y la línea del lugar ya
        /// formateadas, con su corte en medio [P]. Pendiente del asesor cultural.
        /// </summary>
        public static string WorldBanner(int world)
        {
            int w = Check(world);
            return WorldBannerTexts[w] ??= Format(Keys.WorldBanner, LocArgs.Of("heading", WorldHeading(w)).And("place", WorldPlace(w)));
        }

        /// <summary>
        /// La línea del lugar tal como la pinta el banner: «{país} · {río}» o, con <paramref name="named"/> (la cabecera solo
        /// dice «Mundo {n}»), «<b>{nombre}</b> · {país} · {río}» con el nombre en negrita. El corte número i
        /// (<see cref="WorldPlaceSeparators"/>) es un salto de línea si el bit i de <paramref name="breaks"/> está puesto; si no,
        /// « · ». Asigna: solo al colocar el banner.
        /// </summary>
        public static string WorldPlaceLine(int world, bool named, int breaks)
        {
            return Format(named ? Keys.WorldPlaceNamedRich : Keys.WorldPlace, WorldArgs(Check(world)), breaks);
        }

        /// <summary>
        /// Cuántos cortes tiene la línea del lugar (<see cref="WorldPlaceLine"/>) en el idioma actual: los <c>{sep}</c> de su
        /// plantilla. Sin asignaciones.
        /// </summary>
        public static int WorldPlaceSeparators(bool named)
        {
            string? template = Loc.Template(named ? Keys.WorldPlaceNamedRich : Keys.WorldPlace);
            return template == null ? 0 : LocFormat.SeparatorCount(template);
        }

        // El nombre, el país y el río del mundo w: los argumentos de las líneas del lugar.
        private static LocArgs WorldArgs(int w)
        {
            return LocArgs.Of("name", WorldName(w)).And("country", WorldCountry(w)).And("river", WorldRiver(w));
        }

        private static string[] WorldKeys(string part)
        {
            var keys = new string[WorldTable.WorldCount];
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = "world_w" + (i + 1).ToString("00", CultureInfo.InvariantCulture) + "_" + part;
            }

            return keys;
        }

        private static void ClearWorldCaches()
        {
            Array.Clear(WorldTitles, 0, WorldTitles.Length);
            Array.Clear(WorldHeadings, 0, WorldHeadings.Length);
            Array.Clear(WorldPlaces, 0, WorldPlaces.Length);
            Array.Clear(WorldPlacesNamed, 0, WorldPlacesNamed.Length);
            Array.Clear(WorldBannerTexts, 0, WorldBannerTexts.Length);
        }

        private static int Check(int world)
        {
            if (world < 1 || world > WorldTable.WorldCount)
            {
                throw new ArgumentOutOfRangeException(nameof(world), world, "Los mundos van de 1 a 13 (glosario §8).");
            }

            return world;
        }
    }
}
