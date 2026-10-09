#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Levels
{
    /// <summary>Esqueleto del rig de un miquito (guion de la intro §4.2-§4.3; propuesta de fauna b.3).</summary>
    public enum FaunaSkeleton
    {
        /// <summary>A · calitrícido pequeño, uñas en garra (sagui, sauim, pichico).</summary>
        A = 0,

        /// <summary>B · cébido mediano, cola larga no prensil (mono ardilla).</summary>
        B = 1,

        /// <summary>C · pitécido o tití, cuerpo mullido (huapo, cotoncillo, mono nocturno, saquí).</summary>
        C = 2,

        /// <summary>D · atélido grande, cola prensil (churuco, maquisapa).</summary>
        D = 3,
    }

    /// <summary>Modelo de suelta (guion §4.4).</summary>
    public enum FaunaModel
    {
        /// <summary><c>fila</c>.</summary>
        Row = 0,

        /// <summary><c>cuelga</c>.</summary>
        Hang = 1,

        /// <summary><c>sacude</c>.</summary>
        Shake = 2,
    }

    /// <summary>Locomoción (<c>corre</c>, <c>salta</c>, <c>camina</c>, <c>trepa</c>).</summary>
    public enum FaunaLocomotion
    {
        /// <summary><c>corre</c>.</summary>
        Run = 0,

        /// <summary><c>salta</c>: arcos cortos.</summary>
        Jump = 1,

        /// <summary><c>camina</c>.</summary>
        Walk = 2,

        /// <summary><c>trepa</c>.</summary>
        Climb = 3,
    }

    /// <summary>Iluminación de la especie (<c>dia</c> o <c>noche</c>, con la LUT de noche).</summary>
    public enum FaunaLighting
    {
        /// <summary><c>dia</c>.</summary>
        Day = 0,

        /// <summary><c>noche</c>.</summary>
        Night = 1,
    }

    /// <summary>Paleta orientativa de un miquito (la fija el artista, siempre menos saturada que los tintes), en 0xRRGGBB.</summary>
    public sealed class FaunaPalette
    {
        /// <summary>Crea la paleta.</summary>
        public FaunaPalette(int body, int limbs, int face, int muzzle, int cap, int tail, int tailTip)
        {
            Body = body;
            Limbs = limbs;
            Face = face;
            Muzzle = muzzle;
            Cap = cap;
            Tail = tail;
            TailTip = tailTip;
        }

        /// <summary><c>cuerpo</c>.</summary>
        public int Body { get; }

        /// <summary><c>extremidades</c>.</summary>
        public int Limbs { get; }

        /// <summary><c>cara</c>.</summary>
        public int Face { get; }

        /// <summary><c>hocico</c>.</summary>
        public int Muzzle { get; }

        /// <summary><c>gorra</c>.</summary>
        public int Cap { get; }

        /// <summary><c>cola</c> (si falta, la del cuerpo).</summary>
        public int Tail { get; }

        /// <summary><c>punta_cola</c>.</summary>
        public int TailTip { get; }
    }

    /// <summary>
    /// Una especie del elenco de miquitos de la intro (guion §4.2-§4.3, formato <c>af-fauna/0</c>). Todo lo que se ve o se lee
    /// de la especie sale de aquí: el código no conoce ninguna especie. Nombres, datos y paleta, pendientes del asesor
    /// cultural (07 §8.2) y del artista.
    /// </summary>
    public sealed class FaunaSpecies
    {
        internal FaunaSpecies(
            string id,
            string scientific,
            bool showScientific,
            IReadOnlyDictionary<string, string> names,
            FaunaSkeleton skeleton,
            FaunaModel model,
            FaunaLocomotion locomotion,
            int sizeDp,
            int troop,
            string[] restTraits,
            FaunaPalette palette,
            FaunaLighting lighting,
            int[] worlds,
            int fromLevel,
            int toLevel,
            int cardLevel,
            string? iucn,
            bool threatened,
            string factKey,
            string partsSlot,
            string portraitSlot,
            string review,
            string[] avoid)
        {
            Id = id;
            Scientific = scientific;
            ShowScientific = showScientific;
            Names = names;
            Skeleton = skeleton;
            Model = model;
            Locomotion = locomotion;
            SizeDp = sizeDp;
            Troop = troop;
            RestTraits = restTraits;
            Palette = palette;
            Lighting = lighting;
            Worlds = worlds;
            FromLevel = fromLevel;
            ToLevel = toLevel;
            CardLevel = cardLevel;
            Iucn = iucn;
            Threatened = threatened;
            FactKey = factKey;
            PartsSlot = partsSlot;
            PortraitSlot = portraitSlot;
            Review = review;
            Avoid = avoid;
        }

        /// <summary><c>id</c> (nombre científico en minúsculas con guiones bajos).</summary>
        public string Id { get; }

        /// <summary><c>cientifico</c>.</summary>
        public string Scientific { get; }

        /// <summary><c>mostrar_cientifico</c>.</summary>
        public bool ShowScientific { get; }

        /// <summary><c>nombres</c> por idioma (<c>es</c>, <c>pt</c>, <c>en</c>), pendientes del asesor.</summary>
        public IReadOnlyDictionary<string, string> Names { get; }

        /// <summary><c>esqueleto</c>.</summary>
        public FaunaSkeleton Skeleton { get; }

        /// <summary><c>modelo</c>.</summary>
        public FaunaModel Model { get; }

        /// <summary><c>locomocion</c>.</summary>
        public FaunaLocomotion Locomotion { get; }

        /// <summary><c>tam_dp</c>: tamaño en pantalla (se recorta a la franja − 10).</summary>
        public int SizeDp { get; }

        /// <summary><c>tropa_intro</c>.</summary>
        public int Troop { get; }

        /// <summary><c>rasgos_reposo</c>: rasgos que se leen en reposo (máscara, collar, bigote, cara roja…).</summary>
        public string[] RestTraits { get; }

        /// <summary><c>paleta</c>.</summary>
        public FaunaPalette Palette { get; }

        /// <summary>
        /// <c>paleta_pareja</c> (opcional): la del segundo individuo de la tropa cuando la especie tiene dimorfismo (por
        /// ejemplo, la hembra gris parda del saquí junto al macho negro de cara blanca). null: todos con <see cref="Palette"/>.
        /// </summary>
        public FaunaPalette? PairPalette { get; internal set; }

        /// <summary><c>iluminacion</c>.</summary>
        public FaunaLighting Lighting { get; }

        /// <summary><c>mundos</c> donde vive (barrera de los ríos: nunca aparece en otro).</summary>
        public int[] Worlds { get; }

        /// <summary><c>desde_nivel</c>.</summary>
        public int FromLevel { get; }

        /// <summary><c>hasta_nivel</c>.</summary>
        public int ToLevel { get; }

        /// <summary><c>nivel_tarjeta</c>: desde aquí, el primer nivel libre lleva la tarjeta «¡Miquito a la vista!».</summary>
        public int CardLevel { get; }

        /// <summary><c>uicn</c> (null si no está evaluada en el catálogo).</summary>
        public string? Iucn { get; }

        /// <summary><c>amenazada</c>: la línea de la tarjeta es una buena noticia de conservación, nunca la etiqueta UICN.</summary>
        public bool Threatened { get; }

        /// <summary><c>dato_tarjeta</c>: clave de localización de la línea de la tarjeta.</summary>
        public string FactKey { get; }

        /// <summary>Ranura <c>fauna_&lt;id&gt;_partes</c>.</summary>
        public string PartsSlot { get; }

        /// <summary>Ranura <c>fauna_&lt;id&gt;_retrato</c>.</summary>
        public string PortraitSlot { get; }

        /// <summary><c>revision_asesor</c> (hoy siempre <c>pendiente</c>).</summary>
        public string Review { get; }

        /// <summary><c>evitar</c>.</summary>
        public string[] Avoid { get; }

        /// <summary>Tiene el rasgo de reposo <paramref name="trait"/>.</summary>
        public bool HasTrait(string trait)
        {
            for (int i = 0; i < RestTraits.Length; i++)
            {
                if (string.Equals(RestTraits[i], trait, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Vive en el mundo <paramref name="world"/>.</summary>
        public bool LivesIn(int world)
        {
            return Array.IndexOf(Worlds, world) >= 0;
        }

        /// <summary>Nombre en <paramref name="language"/> (o en español; o el científico).</summary>
        public string NameIn(string language)
        {
            if (Names.TryGetValue(language, out string? name) && !string.IsNullOrEmpty(name))
            {
                return name;
            }

            return Names.TryGetValue("es", out string? es) && !string.IsNullOrEmpty(es) ? es : Scientific;
        }
    }

    /// <summary>Tramo sin miquitos (<c>sin_fauna</c>): el mundo 13 de los Andes (07 §2.2: «sin fauna en primer plano»).</summary>
    public sealed class FaunaGap
    {
        internal FaunaGap(int[] worlds, int fromLevel, int toLevel)
        {
            Worlds = worlds;
            FromLevel = fromLevel;
            ToLevel = toLevel;
        }

        /// <summary>Mundos del tramo.</summary>
        public int[] Worlds { get; }

        /// <summary>Primer nivel.</summary>
        public int FromLevel { get; }

        /// <summary>Último nivel.</summary>
        public int ToLevel { get; }
    }

    /// <summary>
    /// Catálogo de especies de miquito de la intro (<c>Assets/_Project/Data/Fauna/miquitos_v0.json</c>, formato
    /// <c>af-fauna/0</c>; guion de la intro §4.3). No es Remote Config. Cada tramo de niveles tiene su especie (la barrera de
    /// los ríos de Wallace) y los tramos cubren 1-1.000 sin solapes ni huecos, con los de <c>sin_fauna</c>.
    /// </summary>
    public sealed class FaunaCatalog
    {
        /// <summary>Formato que lee el cliente.</summary>
        public const string Format = "af-fauna/0";

        /// <summary>Último nivel que cubre el catálogo (13 mundos).</summary>
        public const int LastLevel = 1000;

        internal FaunaCatalog(FaunaSpecies[] species, FaunaGap[] noFauna)
        {
            Species = species;
            NoFauna = noFauna;
        }

        /// <summary>Especies en el orden del archivo.</summary>
        public IReadOnlyList<FaunaSpecies> Species { get; }

        /// <summary>Tramos sin miquitos.</summary>
        public IReadOnlyList<FaunaGap> NoFauna { get; }

        /// <summary>Especie con el id <paramref name="id"/>, o null.</summary>
        public FaunaSpecies? Find(string id)
        {
            for (int i = 0; i < Species.Count; i++)
            {
                if (string.Equals(Species[i].Id, id, StringComparison.Ordinal))
                {
                    return Species[i];
                }
            }

            return null;
        }

        /// <summary>Especie del tramo que contiene <paramref name="level"/> (sin mirar el mundo), o null.</summary>
        public FaunaSpecies? ForLevel(int level)
        {
            for (int i = 0; i < Species.Count; i++)
            {
                if (level >= Species[i].FromLevel && level <= Species[i].ToLevel)
                {
                    return Species[i];
                }
            }

            return null;
        }

        /// <summary>El nivel está en un tramo sin miquitos.</summary>
        public bool IsNoFauna(int level)
        {
            for (int i = 0; i < NoFauna.Count; i++)
            {
                if (level >= NoFauna[i].FromLevel && level <= NoFauna[i].ToLevel)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Compuerta del arte de una especie (guion de la intro §4.3 y §11): la única regla de «hay arte», pura y comprobable. En un
    /// build de desarrollo basta el miquito provisional por código; fuera de él, la tropa exige el PNG <c>fauna_&lt;id&gt;_partes</c>
    /// y la tarjeta «¡Miquito a la vista!» exige además el PNG <c>fauna_&lt;id&gt;_retrato</c> (ni el soft launch ni el vídeo con
    /// miquitos provisionales, §11). Sin arte: dosel en la intro y sin tarjeta, nunca la especie de otro mundo.
    /// </summary>
    public static class FaunaArtRule
    {
        /// <summary>La tropa de la especie puede salir en la intro.</summary>
        public static bool TroopArt(bool developmentBuild, bool partsPng)
        {
            return developmentBuild || partsPng;
        }

        /// <summary>La tarjeta de la especie puede salir: la regla de la tropa y, fuera de desarrollo, también el retrato.</summary>
        public static bool CardArt(bool developmentBuild, bool partsPng, bool portraitPng)
        {
            return TroopArt(developmentBuild, partsPng) && (developmentBuild || portraitPng);
        }
    }

    /// <summary>Qué tropa hace la intro de un nivel (guion de la intro §4.3).</summary>
    public static class IntroCast
    {
        /// <summary>
        /// Especie de la intro del nivel <paramref name="level"/> del mundo <paramref name="world"/>, o null (modo dosel) si: no
        /// hay catálogo; el nivel está en <c>sin_fauna</c> (mundo 13); es un reintento (<paramref name="attempt"/> &gt; 1); no hay
        /// especie para el tramo; la especie del tramo no vive en ese mundo (nunca la de otro mundo: barrera de los ríos); o
        /// falta su arte (<paramref name="artAvailable"/> = false, con <see cref="FaunaArtRule"/>: en un build de release sin el
        /// PNG final; en desarrollo, el miquito provisional por código cuenta como disponible).
        /// </summary>
        public static FaunaSpecies? Resolve(FaunaCatalog? catalog, int world, int level, int attempt, bool artAvailable)
        {
            if (catalog == null || attempt > 1 || catalog.IsNoFauna(level))
            {
                return null;
            }

            FaunaSpecies? species = catalog.ForLevel(level);
            if (species == null || !species.LivesIn(world) || !artAvailable)
            {
                return null;
            }

            return species;
        }
    }
}
