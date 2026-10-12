#nullable enable
using System;
using System.Globalization;
using AF.Levels;

namespace AF.UI
{
    /// <summary>
    /// Textos ES-419 del banner de entrada a cada mundo (07 §2.2, regla transversal 1; 02 §6; clave <c>world_banner</c> del
    /// glosario §10: «Mundo {n}: {nombre} · {país} · {río}»), con los mundos, países y ríos del glosario §8. [P] de presentación:
    /// el país sin la región entre paréntesis del glosario («Brasil (Pará)» → «Brasil»), el río sin el tipo de agua y con
    /// «río»/«ríos» delante (en el mundo 2, «Tapajós · Brasil · Tapajós» se leía como una errata), varios ríos unidos con «y», y en
    /// el mundo 12 el rótulo «vecinos del Orinoco» de la regla 3. Todos los nombres de lugares quedan pendientes de revisión del
    /// asesor cultural (07 §8.2). Sin asignaciones tras la primera vez: cada texto se arma una vez y se guarda.
    /// </summary>
    public static partial class UiText
    {
        /// <summary>Título del banner, «Mundo {n}» (la parte de <c>world_banner</c> antes de los dos puntos).</summary>
        public const string WorldTitleN = "Mundo {0}";

        // Glosario §8, mundos 1-13. Pendientes de revisión del asesor cultural.
        private static readonly string[] WorldNames =
        {
            "Estuario", "Tapajós", "Manaos", "Trapecio", "Loreto", "Oriente", "Madre de Dios", "Madidi", "Ucayali", "Alto Negro",
            "Escudo de Guayana", "Invitados del Orinoco", "El origen",
        };

        private static readonly string[] WorldCountries =
        {
            "Brasil", "Brasil", "Brasil", "Colombia", "Perú", "Ecuador", "Perú", "Bolivia", "Perú", "Venezuela y Brasil",
            "Guyana, Surinam y Guayana Francesa", "Venezuela y Colombia", "Perú",
        };

        private static readonly string[] WorldRivers =
        {
            "ríos Amazonas y Tocantins", "río Tapajós", "ríos Negro y Solimões", "ríos Amazonas y Putumayo", "ríos Marañón y Ucayali",
            "río Napo", "ríos Tambopata y Madre de Dios", "ríos Beni, Mamoré e Iténez", "río Ucayali", "ríos Negro y Casiquiare",
            "ríos Takutu, Branco y Maroni", "vecinos del Orinoco: Caroní y Caño Cristales", "alto Marañón",
        };

        private static readonly string?[] WorldTitles = new string?[WorldTable.WorldCount + 1];
        private static readonly string?[] WorldPlaces = new string?[WorldTable.WorldCount + 1];
        private static readonly string?[] WorldBannerTexts = new string?[WorldTable.WorldCount + 1];

        /// <summary>Nombre del mundo <paramref name="world"/> (1-13; glosario §8). Pendiente del asesor cultural.</summary>
        public static string WorldName(int world)
        {
            return WorldNames[Check(world) - 1];
        }

        /// <summary>«Mundo {n}».</summary>
        public static string WorldTitle(int world)
        {
            int w = Check(world);
            return WorldTitles[w] ??= string.Format(CultureInfo.InvariantCulture, WorldTitleN, w);
        }

        /// <summary>«{nombre} · {país} · {río}» del mundo <paramref name="world"/>. Pendiente del asesor cultural.</summary>
        public static string WorldPlace(int world)
        {
            int w = Check(world);
            return WorldPlaces[w] ??= WorldNames[w - 1] + " · " + WorldCountries[w - 1] + " · " + WorldRivers[w - 1];
        }

        /// <summary>
        /// <c>world_banner</c> (glosario §10): «Mundo {n}: {nombre} · {país} · {río}». Pendiente del asesor cultural.
        /// </summary>
        public static string WorldBanner(int world)
        {
            int w = Check(world);
            return WorldBannerTexts[w] ??= WorldTitle(w) + ": " + WorldPlace(w);
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
