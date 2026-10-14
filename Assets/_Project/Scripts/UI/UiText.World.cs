#nullable enable
using System;
using AF.Levels;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos ES-419 del banner de entrada a cada mundo (07 §2.2, regla transversal 1; 02 §6; clave <c>world_banner</c> del
    /// glosario §10: «Mundo {n}: {nombre} · {país} · {río}»), con los mundos, países y ríos del glosario §8 tal cual: el país
    /// con su región entre paréntesis y el río sin el tipo de agua de la columna «Río (tipo de agua)», como el ejemplo literal
    /// de 06 §2.3 («Mundo 2: Tapajós · Brasil (Pará) · Tapajós»). En el mundo 12, los ríos llevan su etiqueta «vecinos del
    /// Orinoco» (glosario §8: «no amazónicos, etiquetados "vecinos del Orinoco"»; §7). Todos los nombres de lugares quedan
    /// pendientes de revisión del asesor cultural (07 §8.2). Sin asignaciones tras la primera vez: cada texto se arma una vez y
    /// se guarda (se rehace si cambia el idioma, <see cref="Loc.Changed"/>).
    /// <para>Idiomas, nivel 1 (D-018; tarea L1): cada texto es una plantilla con marcadores con nombre, sin pegar trozos. El
    /// marcador <c>{sep}</c> va donde la línea se puede partir; vale « · » (<see cref="WorldSeparator"/>) o un salto de línea,
    /// y el banner (<see cref="WorldBannerView"/>) elige cuál con <see cref="WorldPlaceLine"/>. La negrita del nombre, cuando
    /// abre la línea del lugar, va en su plantilla (<see cref="WorldPlaceNamedRichFormat"/>).</para>
    /// </summary>
    public static partial class UiText
    {
        /// <summary><c>world_title</c> [P]: título corto del banner, «Mundo {n}», cuando el nombre no cabe en la cabecera.</summary>
        public const string WorldTitleN = "Mundo {n}";

        /// <summary>
        /// <c>world_heading</c> [P]: la cabecera del banner, «Mundo {n}: {name}», la parte de <c>world_banner</c> antes del primer
        /// corte, en una sola plantilla (sin pegar «Mundo {n}» y el nombre).
        /// </summary>
        public const string WorldHeadingFormat = "Mundo {n}: {name}";

        /// <summary><c>world_place</c> [P]: la línea del lugar bajo la viñeta, «{country} · {river}», con su corte.</summary>
        public const string WorldPlaceFormat = "{country}{sep}{river}";

        /// <summary>
        /// <c>world_place_named</c> [P]: la línea del lugar cuando la cabecera solo dice «Mundo {n}», «{name} · {country} ·
        /// {river}», con sus dos cortes (el texto llano: <see cref="WorldPlaceNamed"/>).
        /// </summary>
        public const string WorldPlaceNamedFormat = "{name}{sep}{country}{sep}{river}";

        /// <summary>
        /// La misma línea tal como la pinta el banner (<see cref="WorldPlaceLine"/>): el nombre en negrita, con la etiqueta de
        /// TextMeshPro en la plantilla para que cada idioma la ponga donde vaya su nombre.
        /// </summary>
        public const string WorldPlaceNamedRichFormat = "<b>{name}</b>{sep}{country}{sep}{river}";

        /// <summary>
        /// <c>world_banner</c> (glosario §10: «Mundo {n}: {nombre} · {país} · {río}»): la cabecera (<see cref="WorldHeadingFormat"/>)
        /// y la línea del lugar (<see cref="WorldPlaceFormat"/>) ya formateadas, con su corte en medio [P]. Así la cadena entera es
        /// siempre la cabecera y la línea que enseña el banner, y no hacen falta cuatro argumentos (<see cref="LocArgs.MaxCount"/>).
        /// </summary>
        public const string WorldBannerFormat = "{heading}{sep}{place}";

        /// <summary>
        /// Separador de <c>world_banner</c> entre el nombre, el país y el río: lo que vale cada <c>{sep}</c> que no parte la línea.
        /// </summary>
        public const string WorldSeparator = Separator;

        // Glosario §8, mundos 1-13 (columna «Mundo»). Pendientes de revisión del asesor cultural.
        private static readonly string[] WorldNames =
        {
            "Estuario", "Tapajós", "Manaos", "Trapecio", "Loreto", "Oriente", "Madre de Dios", "Madidi", "Ucayali", "Alto Negro",
            "Escudo de Guayana", "Invitados del Orinoco", "El origen",
        };

        // Glosario §8, columna «País», tal cual. Pendientes de revisión del asesor cultural.
        private static readonly string[] WorldCountries =
        {
            "Brasil (Pará)", "Brasil (Pará)", "Brasil (Amazonas)", "Colombia", "Perú", "Ecuador", "Perú", "Bolivia", "Perú",
            "Venezuela / Brasil", "Guyana / Surinam / Guayana Francesa", "Venezuela / Colombia", "Perú (Andes)",
        };

        // Glosario §8, columna «Río (tipo de agua)», sin el tipo de agua (06 §2.3: «… · Tapajós»). Pendientes de revisión del
        // asesor cultural.
        private static readonly string[] WorldRivers =
        {
            "Amazonas / Tocantins", "Tapajós", "Negro + Solimões", "Amazonas / Putumayo", "Marañón-Ucayali",
            "Napo + lagunas negras de Cuyabeno", "Tambopata / Madre de Dios", "Beni / Mamoré, Iténez", "Ucayali, Shanay-Timpishka",
            "Negro, Casiquiare", "Takutu / Branco, Maroni", "Caroní, Caño Cristales (vecinos del Orinoco)", "Marañón alto",
        };

        private static readonly string?[] WorldTitles = new string?[WorldTable.WorldCount + 1];
        private static readonly string?[] WorldHeadings = new string?[WorldTable.WorldCount + 1];
        private static readonly string?[] WorldPlaces = new string?[WorldTable.WorldCount + 1];
        private static readonly string?[] WorldPlacesNamed = new string?[WorldTable.WorldCount + 1];
        private static readonly string?[] WorldBannerTexts = new string?[WorldTable.WorldCount + 1];

        /// <summary>Nombre del mundo <paramref name="world"/> (1-13; glosario §8). Pendiente del asesor cultural.</summary>
        public static string WorldName(int world)
        {
            return WorldNames[Check(world) - 1];
        }

        /// <summary>País del mundo <paramref name="world"/> (glosario §8, tal cual). Pendiente del asesor cultural.</summary>
        public static string WorldCountry(int world)
        {
            return WorldCountries[Check(world) - 1];
        }

        /// <summary>Río del mundo <paramref name="world"/> (glosario §8, sin el tipo de agua). Pendiente del asesor cultural.</summary>
        public static string WorldRiver(int world)
        {
            return WorldRivers[Check(world) - 1];
        }

        /// <summary>«Mundo {n}» (la cabecera cuando el nombre no cabe a 24 sp; <see cref="WorldTitleN"/>).</summary>
        public static string WorldTitle(int world)
        {
            int w = Check(world);
            return WorldTitles[w] ??= Format(WorldTitleN, LocArgs.OfDigits("n", w));
        }

        /// <summary>
        /// «Mundo {n}: {nombre}»: la cabecera del banner, la parte de <c>world_banner</c> antes del primer corte
        /// (<see cref="WorldHeadingFormat"/>). Pendiente del asesor cultural.
        /// </summary>
        public static string WorldHeading(int world)
        {
            int w = Check(world);
            return WorldHeadings[w] ??= Format(WorldHeadingFormat, LocArgs.OfDigits("n", w).And("name", WorldNames[w - 1]));
        }

        /// <summary>
        /// «{país} · {río}»: la línea bajo la viñeta cuando la cabecera lleva el nombre (<see cref="WorldHeading"/>), en una
        /// línea. Pendiente del asesor cultural.
        /// </summary>
        public static string WorldPlace(int world)
        {
            int w = Check(world);
            return WorldPlaces[w] ??= Format(WorldPlaceFormat, WorldArgs(w));
        }

        /// <summary>
        /// «{nombre} · {país} · {río}»: la línea bajo la viñeta cuando la cabecera solo lleva «Mundo {n}» (<see cref="WorldTitle"/>),
        /// en una línea y sin la negrita. Pendiente del asesor cultural.
        /// </summary>
        public static string WorldPlaceNamed(int world)
        {
            int w = Check(world);
            return WorldPlacesNamed[w] ??= Format(WorldPlaceNamedFormat, WorldArgs(w));
        }

        /// <summary>
        /// <c>world_banner</c> (glosario §10): «Mundo {n}: {nombre} · {país} · {río}» (<see cref="WorldBannerFormat"/>). Pendiente
        /// del asesor cultural.
        /// </summary>
        public static string WorldBanner(int world)
        {
            int w = Check(world);
            return WorldBannerTexts[w] ??= Format(WorldBannerFormat, LocArgs.Of("heading", WorldHeading(w)).And("place", WorldPlace(w)));
        }

        /// <summary>
        /// La línea del lugar tal como la pinta el banner: «{país} · {río}» o, con <paramref name="named"/> (la cabecera solo
        /// dice «Mundo {n}»), «<b>{nombre}</b> · {país} · {río}» con el nombre en negrita. El corte número i
        /// (<see cref="WorldPlaceSeparators"/>) es un salto de línea si el bit i de <paramref name="breaks"/> está puesto; si no,
        /// « · ». Asigna: solo al colocar el banner.
        /// </summary>
        public static string WorldPlaceLine(int world, bool named, int breaks)
        {
            return Format(named ? WorldPlaceNamedRichFormat : WorldPlaceFormat, WorldArgs(Check(world)), breaks);
        }

        /// <summary>
        /// Cuántos cortes tiene la línea del lugar (<see cref="WorldPlaceLine"/>): los <c>{sep}</c> de su plantilla. Sin
        /// asignaciones.
        /// </summary>
        public static int WorldPlaceSeparators(bool named)
        {
            return LocFormat.SeparatorCount(named ? WorldPlaceNamedRichFormat : WorldPlaceFormat);
        }

        // El nombre, el país y el río del mundo w: los argumentos de las líneas del lugar.
        private static LocArgs WorldArgs(int w)
        {
            return LocArgs.Of("name", WorldNames[w - 1]).And("country", WorldCountries[w - 1]).And("river", WorldRivers[w - 1]);
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
