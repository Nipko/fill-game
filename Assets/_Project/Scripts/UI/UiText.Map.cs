#nullable enable
using System;
using AF.Levels;
using AF.Localization;
using TMPro;

namespace AF.UI
{
    /// <summary>
    /// Textos del sistema del Mapa del río (D-013; glosario §10; propuesta del Mapa §6.4; P-M2 de D-023; tarea M1.5), de
    /// <c>strings.csv</c>: las 11 claves del glosario (<c>map_stop_remaining</c>, <c>map_arrived</c>, <c>map_home_distance</c>,
    /// <c>map_country_welcome</c>, <c>map_world_question</c>, <c>map_next_world</c>, <c>map_samples</c>, <c>map_north_up</c>,
    /// <c>map_back_to_canoe</c>, <c>map_hito_preview</c> y <c>stop_label</c>), las 10 de P-M2 y el término <c>river_map</c> del
    /// glosario §7.1 (el botón de la pausa, M4.3), más <c>map_source_name</c>, la naciente del último tramo (FX13). Parcial de
    /// <see cref="UiText"/>.
    /// <list type="bullet">
    /// <item>Los nombres de las paradas, de los países y de los ríos son textos largos de <c>facts.csv</c> (claves por convención
    /// de <see cref="RiverStop"/>, 08 §4.5; su bloque lo genera <c>tools/river-map/stops.py</c>) y salen por
    /// <see cref="Loc.Fact(string)"/>: en release, un nombre sin aprobar del asesor es «» (D-018, Q10-A).</item>
    /// <item>Plurales con la sintaxis de la tabla (<c>map_stop_remaining</c>) y cifras con los separadores de la región de
    /// <see cref="Loc"/> (<see cref="LocArgs"/>): los números de nivel y de mundo, sin separador de miles.</item>
    /// <item><c>map_label_andes</c> y <c>map_label_atlantic</c> llevan ↑ y ↓ y se pintan en Baloo 2 (B1:
    /// <c>FontCoverage.TitleOnlyKeys</c>); el ✓ de la pregunta del mundo es un icono, nunca texto.</item>
    /// <item>Todos nacen <c>pendiente_asesor</c>, solo en ES, a la espera del traductor y del asesor cultural.</item>
    /// </list>
    /// Los textos con argumentos asignan su cadena: solo al construir la pantalla, nunca por fotograma.
    /// </summary>
    public static partial class UiText
    {
        /// <summary>Ancho (dp) en el que el nombre de una parada cabe en <see cref="StopNameMaxLines"/> líneas (tarjeta de la parada).</summary>
        public const float StopNameWidthDp = 296f;

        /// <summary>Líneas que puede ocupar el nombre de una parada a <see cref="StopNameWidthDp"/>.</summary>
        public const int StopNameMaxLines = 2;

        /// <summary>Cuerpo (sp) del nombre de la parada en la cabecera de su tarjeta, en Baloo 2 ExtraBold [P].</summary>
        public const float StopNameSizeSp = 20f;

        /// <summary><c>map_north_up</c> [P]: botón «Norte arriba» del Mapa.</summary>
        public static string MapNorthUp => Loc.T(Keys.MapNorthUp);

        /// <summary><c>map_back_to_canoe</c> [P]: botón «Volver a la canoa» del Mapa.</summary>
        public static string MapBackToCanoe => Loc.T(Keys.MapBackToCanoe);

        /// <summary><c>map_view_world</c> [P] (P-M2): pestaña de la vista Mundo (D-013, punto 5).</summary>
        public static string MapViewWorld => Loc.T(Keys.MapViewWorld);

        /// <summary><c>map_view_basin</c> [P] (P-M2): pestaña de la vista Cuenca (D-013, punto 5).</summary>
        public static string MapViewBasin => Loc.T(Keys.MapViewBasin);

        /// <summary><c>map_label_andes</c> [P] (P-M2): «Andes ↑», en Baloo 2.</summary>
        public static string MapLabelAndes => Loc.T(Keys.MapLabelAndes);

        /// <summary><c>map_label_atlantic</c> [P] (P-M2): «Océano Atlántico ↓», en Baloo 2.</summary>
        public static string MapLabelAtlantic => Loc.T(Keys.MapLabelAtlantic);

        /// <summary><c>map_back_to_level</c> [P] (P-M2): el «Jugar» del Mapa abierto desde la pausa (D-013, punto 7; 04 §5.2).</summary>
        public static string MapBackToLevel => Loc.T(Keys.MapBackToLevel);

        /// <summary><c>map_open</c> [P] (P-M2): «Ver mapa» en la victoria del <c>10k</c> y en el banner.</summary>
        public static string MapOpen => Loc.T(Keys.MapOpen);

        /// <summary>
        /// <c>river_map</c> (glosario §7.1): «Mapa del río», el botón del menú de pausa que abre el Mapa encima del nivel (02 §5;
        /// D-013, punto 7; 04 §5.2; tarea M4.3).
        /// </summary>
        public static string RiverMap => Loc.T(Keys.RiverMap);

        /// <summary><c>map_card_other_fact</c> [P] (P-M2): «Otro dato» en la tarjeta de la parada.</summary>
        public static string MapCardOtherFact => Loc.T(Keys.MapCardOtherFact);

        /// <summary>
        /// <c>map_l10_intro</c> [P] (P-M2): el bocadillo con el que TiTino se presenta en el Mapa del 10 (06 §2.3; ≤ 50
        /// caracteres, fuera de <c>sloth_line_*</c>). Pendiente del asesor.
        /// </summary>
        public static string MapL10Intro => Loc.T(Keys.MapL10Intro);

        /// <summary>Nombre de la parada <paramref name="stop"/> (<c>map.stop.&lt;id&gt;.name</c> de <c>facts.csv</c>). Pendiente del asesor.</summary>
        public static string StopName(RiverStop stop)
        {
            return Loc.Fact(NotNull(stop).NameKey);
        }

        /// <summary>País de la placa de <paramref name="stop"/> (<see cref="RiverStop.CountryNameKeyOrDefault"/>). Pendiente del asesor.</summary>
        public static string StopCountry(RiverStop stop)
        {
            return Loc.Fact(NotNull(stop).CountryNameKeyOrDefault);
        }

        /// <summary>Río de <paramref name="stop"/>, sin la palabra «río» (<c>map.stop.&lt;id&gt;.river</c>). Pendiente del asesor.</summary>
        public static string StopRiver(RiverStop stop)
        {
            return Loc.Fact(NotNull(stop).RiverKey);
        }

        /// <summary>
        /// <c>map_stop_remaining</c> [P]: «Faltan {n} niveles para {lugar}», con el plural del idioma («Falta 1 nivel para…»).
        /// <paramref name="levels"/> va sin separador de miles; un negativo se escribe 0.
        /// </summary>
        public static string MapStopRemaining(int levels, string place)
        {
            return Format(Keys.MapStopRemaining, LocArgs.OfDigits("n", NonNegative(levels)).And("place", place));
        }

        /// <summary>
        /// <c>map_source_name</c> [P] (tarea FX13; D-013, viñeta del 2026-10-19): «El nevado Mismi», la naciente a la que apunta el
        /// último tramo tras la última parada. No es una parada: solo el <c>{place}</c> de <see cref="MapStopRemaining(int, string)"/>.
        /// Pendiente del asesor.
        /// </summary>
        public static string MapSourceName => Loc.T(Keys.MapSourceName);

        /// <summary>
        /// Lugar de «Faltan N» jugando en la parada <paramref name="current"/>: el nombre de la parada siguiente del catálogo
        /// <paramref name="stops"/> o, tras la última si está en el cauce (<see cref="RiverStopCatalog.HeadsToSource"/>: niveles
        /// 691-700 con las paradas 0-69), la naciente (<see cref="MapSourceName"/>; tarea FX13). «» sin parada en juego, fuera del
        /// catálogo o tras una última parada fuera del cauce (en release, también con un nombre sin el visto bueno del asesor).
        /// </summary>
        public static string NextPlaceName(RiverStopCatalog stops, RiverStop? current)
        {
            if (stops == null)
            {
                throw new ArgumentNullException(nameof(stops));
            }

            if (current == null)
            {
                return string.Empty;
            }

            RiverStop? next = stops.At(current.N + 1);
            if (next != null)
            {
                return StopName(next);
            }

            return current.N == stops.Count - 1 && stops.HeadsToSource ? MapSourceName : string.Empty;
        }

        /// <summary><see cref="MapStopRemaining(int, string)"/> hacia la parada <paramref name="next"/>.</summary>
        public static string MapStopRemaining(int levels, RiverStop next)
        {
            return MapStopRemaining(levels, StopName(next));
        }

        /// <summary><c>map_arrived</c> [P]: «{lugar} · {país} · río {río}» (placa de llegada y tarjeta de la parada), en una línea.</summary>
        public static string MapArrived(string place, string country, string river)
        {
            return MapArrived(place, country, river, 0);
        }

        /// <summary>
        /// <c>map_arrived</c> [P] con cortes: su plantilla lleva <c>{sep}</c> donde la línea se puede partir (revisión de M1.6), y
        /// el <c>{sep}</c> número i es un salto de línea si el bit i de <paramref name="breaks"/> está puesto y « · » si no
        /// (<see cref="MapArrivedSeparators"/>). Así la placa nunca deja un « · » colgando al final de una línea.
        /// </summary>
        public static string MapArrived(string place, string country, string river, int breaks)
        {
            return Format(Keys.MapArrived, LocArgs.Of("place", place).And("country", country).And("river", river), breaks);
        }

        /// <summary>Cuántos cortes (<c>{sep}</c>) tiene <c>map_arrived</c> en el idioma actual. Sin asignaciones.</summary>
        public static int MapArrivedSeparators()
        {
            string? template = Loc.Template(Keys.MapArrived);
            return template == null ? 0 : LocFormat.SeparatorCount(template);
        }

        /// <summary><see cref="MapArrived(string, string, string)"/> de la parada <paramref name="stop"/>.</summary>
        public static string MapArrived(RiverStop stop)
        {
            return MapArrived(StopName(stop), StopCountry(stop), StopRiver(stop));
        }

        /// <summary>
        /// <c>map_home_distance</c> [P]: «Casa · a unos {km} km» (ventanita de TiTino), con el separador de miles de la región
        /// y un espacio duro antes de «km» (08 §4.7). Un negativo se escribe 0.
        /// </summary>
        public static string MapHomeDistance(int km)
        {
            return Format(Keys.MapHomeDistance, LocArgs.Of("km", NonNegative(km)));
        }

        /// <summary><c>map_country_welcome</c> [P]: «{país} te da la bienvenida» (llegada del 80 y del 140; P-M7).</summary>
        public static string MapCountryWelcome(string country)
        {
            return Format(Keys.MapCountryWelcome, LocArgs.Of("country", country));
        }

        /// <summary><see cref="MapCountryWelcome(string)"/> con el país de la parada <paramref name="stop"/>.</summary>
        public static string MapCountryWelcome(RiverStop stop)
        {
            return MapCountryWelcome(StopCountry(stop));
        }

        /// <summary>
        /// <c>map_world_question</c> [P]: «{mundo} · {pregunta}» en la cabecera del Mapa, con el nombre del mundo
        /// <paramref name="world"/> (1-13) y el texto de la pregunta (<c>map.world.wNN.question</c>). El ✓ es un icono aparte.
        /// </summary>
        public static string MapWorldQuestion(int world, string question)
        {
            return Format(Keys.MapWorldQuestion, LocArgs.Of("world", WorldName(world)).And("question", question));
        }

        /// <summary>
        /// <c>map_next_world</c> [P]: «Mundo {n} · {nombre} · Nivel {nivel}» en la silueta de un mundo futuro; el número de
        /// mundo y el de nivel, sin separador de miles.
        /// </summary>
        public static string MapNextWorld(int world, int firstLevel)
        {
            return Format(Keys.MapNextWorld, LocArgs.OfDigits("n", Check(world)).And("name", WorldName(world)).AndDigits("level", NonNegative(firstLevel)));
        }

        /// <summary><c>map_samples</c> [P]: «Muestrario · {k}/{n}», con el separador de miles de la región.</summary>
        public static string MapSamples(int seen, int total)
        {
            return Format(Keys.MapSamples, LocArgs.Of("k", NonNegative(seen)).And("n", NonNegative(total)));
        }

        /// <summary><c>map_hito_preview</c> [P]: «Si lo superas, llegas a {lugar}» (tarjeta previa del hito p20; 02 §10).</summary>
        public static string MapHitoPreview(string place)
        {
            return Format(Keys.MapHitoPreview, LocArgs.Of("place", place));
        }

        /// <summary><see cref="MapHitoPreview(string)"/> hacia la parada <paramref name="stop"/>.</summary>
        public static string MapHitoPreview(RiverStop stop)
        {
            return MapHitoPreview(StopName(stop));
        }

        /// <summary><c>stop_label</c> [P]: «{lugar} · {país}», el rótulo del lugar (07 §2.1).</summary>
        public static string StopLabel(string place, string country)
        {
            return Format(Keys.StopLabel, LocArgs.Of("place", place).And("country", country));
        }

        /// <summary><see cref="StopLabel(string, string)"/> de la parada <paramref name="stop"/>.</summary>
        public static string StopLabel(RiverStop stop)
        {
            return StopLabel(StopName(stop), StopCountry(stop));
        }

        /// <summary><c>map_card_source</c> [P] (P-M2): «Fuente: {fuente}» al pie de la tarjeta de la parada.</summary>
        public static string MapCardSource(string source)
        {
            return Format(Keys.MapCardSource, LocArgs.Of("source", source));
        }

        /// <summary><c>map_facts_count</c> [P] (P-M2): «Datos del río · {k}/{n}» en el Muestrario.</summary>
        public static string MapFactsCount(int seen, int total)
        {
            return Format(Keys.MapFactsCount, LocArgs.Of("k", NonNegative(seen)).And("n", NonNegative(total)));
        }

        /// <summary>
        /// Líneas que ocupa <paramref name="text"/> con la fuente de <paramref name="style"/> a <paramref name="sizeSp"/> sp en
        /// <paramref name="widthDp"/> dp, con el corte por palabras de TextMeshPro (en los espacios y en los saltos de línea; una
        /// palabra más ancha que la línea ocupa la suya y sigue en la siguiente). Mide con los avances de los glifos del atlas,
        /// sin el kerning; un carácter que falta cuenta un em. -1 si no hay fuente. Asigna: solo para medir, nunca por fotograma.
        /// </summary>
        internal static int LineCount(string text, TextStyle style, float sizeSp, float widthDp)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            TMP_FontAsset? font = UiFonts.Font(style);
            if (font == null || font.faceInfo.pointSize <= 0f)
            {
                return -1;
            }

            float scale = sizeSp / font.faceInfo.pointSize * (font.faceInfo.scale > 0f ? font.faceInfo.scale : 1f);
            float space = Advance(font, ' ') * scale;
            int lines = 1;
            float x = 0f;
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == '\n')
                {
                    lines++;
                    x = 0f;
                    i++;
                    continue;
                }

                if (c == ' ')
                {
                    x += space;
                    i++;
                    continue;
                }

                float word = 0f;
                while (i < text.Length && text[i] != ' ' && text[i] != '\n')
                {
                    word += Advance(font, text[i]) * scale;
                    i++;
                }

                if (x > 0f && x + word > widthDp)
                {
                    lines++;
                    x = 0f;
                }

                x += word;
                while (x > widthDp)
                {
                    // La palabra no cabe ni sola: TextMeshPro la parte por caracteres.
                    lines++;
                    x -= widthDp;
                }
            }

            return lines;
        }

        // Avance horizontal del carácter c en el atlas (en unidades de la fuente), o un em si falta.
        private static float Advance(TMP_FontAsset font, char c)
        {
            if (font.characterLookupTable.TryGetValue(c, out TMP_Character? character) && character?.glyph != null)
            {
                return character.glyph.metrics.horizontalAdvance;
            }

            return font.faceInfo.pointSize;
        }

        private static RiverStop NotNull(RiverStop stop)
        {
            return stop ?? throw new ArgumentNullException(nameof(stop));
        }

        /// <summary>Claves de los textos del Mapa del río (parcial de <see cref="Keys"/>; glosario §10 y P-M2).</summary>
        public static partial class Keys
        {
            /// <summary><c>map_arrived</c> [P] (glosario §10).</summary>
            public const string MapArrived = "map_arrived";

            /// <summary><c>map_back_to_canoe</c> [P] (glosario §10).</summary>
            public const string MapBackToCanoe = "map_back_to_canoe";

            /// <summary><c>map_back_to_level</c> [P] (P-M2).</summary>
            public const string MapBackToLevel = "map_back_to_level";

            /// <summary><c>map_card_other_fact</c> [P] (P-M2).</summary>
            public const string MapCardOtherFact = "map_card_other_fact";

            /// <summary><c>map_card_source</c> [P] (P-M2).</summary>
            public const string MapCardSource = "map_card_source";

            /// <summary><c>map_country_welcome</c> [P] (glosario §10; P-M7).</summary>
            public const string MapCountryWelcome = "map_country_welcome";

            /// <summary><c>map_facts_count</c> [P] (P-M2).</summary>
            public const string MapFactsCount = "map_facts_count";

            /// <summary><c>map_hito_preview</c> [P] (glosario §10).</summary>
            public const string MapHitoPreview = "map_hito_preview";

            /// <summary><c>map_home_distance</c> [P] (glosario §10).</summary>
            public const string MapHomeDistance = "map_home_distance";

            /// <summary><c>map_l10_intro</c> [P] (P-M2).</summary>
            public const string MapL10Intro = "map_l10_intro";

            /// <summary><c>map_label_andes</c> [P] (P-M2; Baloo 2).</summary>
            public const string MapLabelAndes = "map_label_andes";

            /// <summary><c>map_label_atlantic</c> [P] (P-M2; Baloo 2).</summary>
            public const string MapLabelAtlantic = "map_label_atlantic";

            /// <summary><c>map_next_world</c> [P] (glosario §10).</summary>
            public const string MapNextWorld = "map_next_world";

            /// <summary><c>map_north_up</c> [P] (glosario §10).</summary>
            public const string MapNorthUp = "map_north_up";

            /// <summary><c>map_open</c> [P] (P-M2).</summary>
            public const string MapOpen = "map_open";

            /// <summary><c>map_samples</c> [P] (glosario §10).</summary>
            public const string MapSamples = "map_samples";

            /// <summary><c>map_source_name</c> [P] (FX13; D-013, viñeta del 2026-10-19).</summary>
            public const string MapSourceName = "map_source_name";

            /// <summary><c>map_stop_remaining</c> [P] (glosario §10).</summary>
            public const string MapStopRemaining = "map_stop_remaining";

            /// <summary><c>map_view_basin</c> [P] (P-M2).</summary>
            public const string MapViewBasin = "map_view_basin";

            /// <summary><c>map_view_world</c> [P] (P-M2).</summary>
            public const string MapViewWorld = "map_view_world";

            /// <summary><c>map_world_question</c> [P] (glosario §10).</summary>
            public const string MapWorldQuestion = "map_world_question";

            /// <summary><c>river_map</c> (glosario §7.1): el botón «Mapa del río» de la pausa (M4.3).</summary>
            public const string RiverMap = "river_map";

            /// <summary><c>stop_label</c> [P] (glosario §10).</summary>
            public const string StopLabel = "stop_label";
        }
    }
}
