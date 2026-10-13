#nullable enable
using System;
using System.Globalization;
using AF.Levels;

namespace AF.UI
{
    /// <summary>
    /// Textos ES-419 del banner de entrada a cada mundo (07 §2.2, regla transversal 1; 02 §6; clave <c>world_banner</c> del
    /// glosario §10: «Mundo {n}: {nombre} · {país} · {río}»), con los mundos, países y ríos del glosario §8 tal cual: el país
    /// con su región entre paréntesis y el río sin el tipo de agua de la columna «Río (tipo de agua)», como el ejemplo literal
    /// de 06 §2.3 («Mundo 2: Tapajós · Brasil (Pará) · Tapajós»). En el mundo 12, los ríos llevan su etiqueta «vecinos del
    /// Orinoco» (glosario §8: «no amazónicos, etiquetados "vecinos del Orinoco"»; §7). Todos los nombres de lugares quedan
    /// pendientes de revisión del asesor cultural (07 §8.2). Sin asignaciones tras la primera vez: cada texto se arma una vez y
    /// se guarda.
    /// </summary>
    public static partial class UiText
    {
        /// <summary>Título corto del banner, «Mundo {n}», cuando el nombre no cabe en la cabecera.</summary>
        public const string WorldTitleN = "Mundo {0}";

        /// <summary>Separador de <c>world_banner</c> entre el nombre, el país y el río.</summary>
        public const string WorldSeparator = " · ";

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

        /// <summary>«Mundo {n}» (la cabecera cuando el nombre no cabe a 24 sp).</summary>
        public static string WorldTitle(int world)
        {
            int w = Check(world);
            return WorldTitles[w] ??= string.Format(CultureInfo.InvariantCulture, WorldTitleN, w);
        }

        /// <summary>
        /// «Mundo {n}: {nombre}»: la cabecera del banner, la parte de <c>world_banner</c> antes del primer « · ». Pendiente del
        /// asesor cultural.
        /// </summary>
        public static string WorldHeading(int world)
        {
            int w = Check(world);
            return WorldHeadings[w] ??= WorldTitle(w) + ": " + WorldNames[w - 1];
        }

        /// <summary>
        /// «{país} · {río}»: la línea bajo la viñeta cuando la cabecera lleva el nombre (<see cref="WorldHeading"/>). Pendiente
        /// del asesor cultural.
        /// </summary>
        public static string WorldPlace(int world)
        {
            int w = Check(world);
            return WorldPlaces[w] ??= WorldCountries[w - 1] + WorldSeparator + WorldRivers[w - 1];
        }

        /// <summary>
        /// «{nombre} · {país} · {río}»: la línea bajo la viñeta cuando la cabecera solo lleva «Mundo {n}» (<see cref="WorldTitle"/>).
        /// Pendiente del asesor cultural.
        /// </summary>
        public static string WorldPlaceNamed(int world)
        {
            int w = Check(world);
            return WorldPlacesNamed[w] ??= WorldNames[w - 1] + WorldSeparator + WorldPlace(w);
        }

        /// <summary>
        /// <c>world_banner</c> (glosario §10): «Mundo {n}: {nombre} · {país} · {río}». Pendiente del asesor cultural.
        /// </summary>
        public static string WorldBanner(int world)
        {
            int w = Check(world);
            return WorldBannerTexts[w] ??= WorldHeading(w) + WorldSeparator + WorldPlace(w);
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
