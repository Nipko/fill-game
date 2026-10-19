#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Levels
{
    /// <summary>Tipo de parada del Mapa del río (08 §4.5, campo <c>kind</c>) [P].</summary>
    public enum StopKind
    {
        /// <summary><c>gate</c>: gran parada, la primera de cada mundo.</summary>
        Gate = 0,

        /// <summary><c>stop</c>: parada.</summary>
        Stop = 1,

        /// <summary><c>close</c>: cierre de mundo (el «castillo»), la última de un mundo de más de un tramo.</summary>
        Close = 2,
    }

    /// <summary>
    /// Fondo de una parada (08 §4.5, campo <c>bg</c>; glosario §11): el kit del mundo y uno y solo uno de héroe pintado o pieza
    /// por código, más la variante de luz. Solo nombres de ranura o de pieza, nunca arte. Inmutable.
    /// </summary>
    public sealed class RiverStopBackdrop
    {
        /// <summary>Crea el fondo (el lector ya lo validó).</summary>
        public RiverStopBackdrop(string kit, string? hero, string? code, string? lut)
        {
            Kit = kit;
            Hero = hero;
            Code = code;
            Lut = lut;
        }

        /// <summary>Kit del mundo, <c>bg_kit_w{nn}</c>.</summary>
        public string Kit { get; }

        /// <summary>Héroe pintado del sitio, <c>bg_hero_&lt;sitio&gt;</c>, o null.</summary>
        public string? Hero { get; }

        /// <summary>Pieza por código (<c>codigo:&lt;pieza&gt;</c>) o parada solo con el kit (<c>kit:&lt;pieza&gt;</c>), o null.</summary>
        public string? Code { get; }

        /// <summary>Variante de luz: <c>dusk</c> (atardecer), <c>dawn</c> (alba) o null.</summary>
        public string? Lut { get; }
    }

    /// <summary>
    /// Una parada del Mapa del río (<c>af-river-stops/1</c>; 08 §4.5; contrato 30): la parada k = <see cref="N"/> cubre los niveles
    /// <c>10k+1</c> … <c>10k+10</c> y se llega a ella al ganar el <c>10k</c>. Solo claves de texto (sus textos van en
    /// <c>facts.csv</c>, contrato 32) y nombres por convención sin campo (<see cref="NameKey"/>, <see cref="RiverKey"/> y
    /// <see cref="CountryNameKeyOrDefault"/>). Los campos <c>at</c> y <c>leg</c> (ruta de la canoa) los leerá M3.2 cuando existan.
    /// Inmutable.
    /// </summary>
    public sealed class RiverStop
    {
        private readonly string[] _facts;

        /// <summary>Crea la parada (el lector ya la validó).</summary>
        public RiverStop(int n, int world, string id, StopKind kind, string country, string? countryName, int? kmFromSea, int kmHome,
            string sample, string? flagRaise, string? arrivalFact, string[] facts, string? worldQuestion, bool answersWorldQuestion,
            RiverStopBackdrop backdrop, string? atLine = null)
        {
            AtLine = atLine;
            N = n;
            World = world;
            Id = id;
            Kind = kind;
            Country = country;
            CountryName = countryName;
            KmFromSea = kmFromSea;
            KmHome = kmHome;
            Sample = sample;
            FlagRaise = flagRaise;
            ArrivalFact = arrivalFact;
            _facts = (string[])facts.Clone();
            WorldQuestion = worldQuestion;
            AnswersWorldQuestion = answersWorldQuestion;
            Backdrop = backdrop;
            NameKey = "map.stop." + id + ".name";
            RiverKey = "map.stop." + id + ".river";
            CountryNameKeyOrDefault = countryName ?? "map.country." + country.ToLowerInvariant() + ".name";
        }

        /// <summary>k de la parada (0-99); su id visible es <c>s{nn}</c> (glosario §11).</summary>
        public int N { get; }

        /// <summary>Primer nivel que cubre: <c>10k+1</c>.</summary>
        public int FirstLevel => (10 * N) + 1;

        /// <summary>Último nivel que cubre: <c>10k+10</c>.</summary>
        public int LastLevel => (10 * N) + 10;

        /// <summary>Mundo (1-13), igual que el de <see cref="WorldTable"/> para sus diez niveles.</summary>
        public int World { get; }

        /// <summary>Id del sitio en minúsculas con guiones bajos (glosario §11); nunca «belen».</summary>
        public string Id { get; }

        /// <summary>Gran parada, parada o cierre de mundo [P].</summary>
        public StopKind Kind { get; }

        /// <summary>País de la parada (ISO 3166-1 alfa-2, en mayúsculas).</summary>
        public string Country { get; }

        /// <summary>
        /// Clave del nombre de país de la placa cuando no basta con el de <see cref="Country"/> (<c>map.stop.&lt;id&gt;.country</c>;
        /// en n = 13, «Colombia y Perú»), o null.
        /// </summary>
        public string? CountryName { get; }

        /// <summary>Km desde el mar por el cauce; null en n = 0 (Belém no está en el cauce).</summary>
        public int? KmFromSea { get; }

        /// <summary>Km por agua hasta casa, en el estuario (<c>map_home_distance</c>); 0 en n = 0 [P].</summary>
        public int KmHome { get; }

        /// <summary>Agua del sitio para la mini cuia del Muestrario: <c>white</c>, <c>black</c>, <c>clear</c>, <c>clear_white</c> o <c>black_white</c> [P].</summary>
        public string Sample { get; }

        /// <summary>Bandera (ISO2) que se iza al llegar (al empezar y al cambiar de país), o null.</summary>
        public string? FlagRaise { get; }

        /// <summary>Clave del dato de llegada (<c>map.*</c>; también el <c>fact_id</c> de <c>af_river_fact_shown</c>); null en n = 0.</summary>
        public string? ArrivalFact { get; }

        /// <summary>Claves de los «Otro dato» de la tarjeta de la parada (vacío hasta M2.6).</summary>
        public IReadOnlyList<string> Facts => _facts;

        /// <summary>Clave de la pregunta del mundo (<c>map.world.w{nn}.question</c>) en la parada que la plantea, o null.</summary>
        public string? WorldQuestion { get; }

        /// <summary>La parada cuyo dato de llegada responde la pregunta de su mundo (el ✓ de la cabecera del Mapa) [P].</summary>
        public bool AnswersWorldQuestion { get; }

        /// <summary>Fondo de la parada.</summary>
        public RiverStopBackdrop Backdrop { get; }

        /// <summary>Clave del nombre del sitio, por convención: <c>map.stop.&lt;id&gt;.name</c> (08 §4.5).</summary>
        public string NameKey { get; }

        /// <summary>Clave del río del sitio, por convención: <c>map.stop.&lt;id&gt;.river</c> (08 §4.5).</summary>
        public string RiverKey { get; }

        /// <summary>Clave del nombre de país de la placa: <see cref="CountryName"/> o, si es null, <c>map.country.&lt;iso2&gt;.name</c>.</summary>
        public string CountryNameKeyOrDefault { get; }

        /// <summary>
        /// Id de la línea del Mapa en la que está la parada (<c>at.line</c>), o null si el archivo no la trae. Solo dice si la
        /// última parada está en el cauce (<see cref="RiverStopCatalog.HeadsToSource"/>; tarea FX13).
        /// </summary>
        public string? AtLine { get; }
    }

    /// <summary>
    /// Catálogo de las paradas del Mapa del río (<c>af-river-stops/1</c>; 08 §2.2 y §4.5; contrato 30), sin motor: lo lee y lo
    /// contrasta con <see cref="WorldTable"/> <see cref="RiverStopCatalogReader"/>. Las búsquedas (<see cref="At"/>,
    /// <see cref="StopOf"/>, <see cref="ArrivalOnWin"/> y <see cref="FirstStopOfWorld"/>) no asignan memoria. Inmutable.
    /// </summary>
    public sealed class RiverStopCatalog
    {
        /// <summary>Formato que lee este cliente.</summary>
        public const string Format = "af-river-stops/1";

        /// <summary>Tope del dato del Mapa en español, en caracteres (08 §4.5) [P].</summary>
        public const int FactMaxChars = 90;

        /// <summary>Tope de la pregunta del mundo en español, en caracteres (08 §4.5) [P].</summary>
        public const int QuestionMaxChars = 50;

        /// <summary>Tope del nombre de un sitio en español, en caracteres (08 §4.5) [P].</summary>
        public const int NameMaxChars = 32;

        /// <summary>Niveles que cubre cada parada (la cadencia de 08 §4.5: la parada k cubre <c>10k+1</c> … <c>10k+10</c>).</summary>
        public const int LevelsPerStop = 10;

        private readonly RiverStop[] _stops;

        // Índice de la primera parada de cada mundo (índice 0 = mundo 1), −1 si el mundo no tiene paradas.
        private readonly int[] _firstOfWorld;

        /// <summary>Crea el catálogo con las paradas en orden de n (el lector ya las validó).</summary>
        public RiverStopCatalog(string source, RiverStop[] stops)
        {
            Source = source;
            _stops = (RiverStop[])stops.Clone();
            _firstOfWorld = new int[WorldTable.WorldCount];
            for (int w = 0; w < _firstOfWorld.Length; w++)
            {
                _firstOfWorld[w] = -1;
            }

            for (int i = _stops.Length - 1; i >= 0; i--)
            {
                int world = _stops[i].World;
                if (world >= 1 && world <= WorldTable.WorldCount)
                {
                    _firstOfWorld[world - 1] = i;
                }
            }
        }

        /// <summary>Ruta de la fuente desde la que se generó el archivo (<c>source</c>).</summary>
        public string Source { get; }

        /// <summary>Paradas del catálogo (20 en 1-200).</summary>
        public int Count => _stops.Length;

        /// <summary>
        /// Tras la última parada, el tramo va a la naciente del Mismi (tarea FX13; decisión del dueño del 2026-10-19, en D-013): la
        /// última parada está en el cauce (<see cref="RiverMap.CauceLineId"/>), que sube hasta la naciente. Con las paradas 0-69,
        /// Tambo: los niveles 691-700 van hacia la naciente. Es la misma condición con la que <see cref="RiverMapReader.SourceGoal"/>
        /// da la ruta de la meta.
        /// </summary>
        public bool HeadsToSource => _stops.Length > 0
            && string.Equals(_stops[_stops.Length - 1].AtLine, RiverMap.CauceLineId, StringComparison.Ordinal);

        /// <summary>Último nivel que cubren las paradas (1-200: el 200).</summary>
        public int LastLevel => _stops.Length * LevelsPerStop;

        /// <summary>Parada k = <paramref name="n"/>, o null si no existe.</summary>
        public RiverStop? At(int n)
        {
            return n >= 0 && n < _stops.Length ? _stops[n] : null;
        }

        /// <summary>Parada del nivel <paramref name="level"/>: <c>At((L − 1) / 10)</c>; null si el nivel no tiene parada.</summary>
        public RiverStop? StopOf(int level)
        {
            return level >= 1 ? At((level - 1) / LevelsPerStop) : null;
        }

        /// <summary>
        /// Parada a la que se llega al ganar el nivel <paramref name="wonLevel"/>: <c>At(L / 10)</c> en los múltiplos de 10 desde el
        /// 10; null en los demás niveles o si la parada no existe (el 200 con las paradas de 1-200).
        /// </summary>
        public RiverStop? ArrivalOnWin(int wonLevel)
        {
            return wonLevel >= LevelsPerStop && wonLevel % LevelsPerStop == 0 ? At(wonLevel / LevelsPerStop) : null;
        }

        /// <summary>Primera parada (el gate) del mundo <paramref name="world"/>, o null si el catálogo no llega a ese mundo.</summary>
        public RiverStop? FirstStopOfWorld(int world)
        {
            if (world < 1 || world > WorldTable.WorldCount)
            {
                return null;
            }

            int index = _firstOfWorld[world - 1];
            return index >= 0 ? _stops[index] : null;
        }
    }
}
