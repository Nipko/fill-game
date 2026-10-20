#nullable enable
using System;
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>
    /// En qué nivel sale la tarjeta «¡Miquito a la vista!» (guion de la intro §4.5 y §14): en el primer nivel ≥
    /// <c>nivel_tarjeta</c> que esté libre, sin banner de mundo, sin Fruta nueva, sin debut de mecánica o de comodín y sin
    /// tutorial pendiente al empezar, con una sola tarjeta por nivel (prioridad tutorial → Fruta nueva → miquito; la instrucción
    /// del tutorial con TiTino de 1, 2, 3 y 6 ocupa su nivel, <see cref="TutorialInstruction"/>, tarea TU1a). Los niveles
    /// con banner salen de la única regla que los decide, <see cref="WorldBanners"/> (11, 41, 81, 141…; el 1 no lo lleva por
    /// decisión del dueño, pero ya está ocupado por la Fruta nueva y el tutorial). Regla pura y determinista: no guarda nada
    /// (el avistamiento, <c>fauna.sighted</c> y <c>af_fauna_sighted</c>, Q11, D-029, DC15, se apunta cuando la tarjeta sale a la
    /// vista, en GameScreen.PumpNotices → SightingShown). Solo en el primer intento del nivel: los reintentos ya la vieron. Decide también en qué niveles sale la tarjeta
    /// «Fruta nueva» y de qué tinte (<see cref="FruitNewAt"/>; 06 §4.5; tarea F5). Los niveles ocupados por estrenos salen de la
    /// especificación de la campaña (<see cref="OccupiedDebutLevels"/>, con las variantes de capacidad 18, 22 y 160 y los estrenos
    /// aún sin tarjeta, hoy solo la Tinaja en el 323) más los hitos de 02 §13 (propuesta de Frutas §3.3).
    /// </summary>
    public static class SightingRule
    {
        /// <summary>
        /// Nivel del primer contacto (07 §6.4: «Sin tarjeta en el nivel 1: el primer contacto es el tutorial»; 02 §13, dedo guía
        /// del nivel 1): estrena el açaí y el aguaje, pero no lleva la tarjeta «Fruta nueva» (tutorial &gt; Fruta nueva).
        /// </summary>
        public const int FirstContactLevel = 1;

        /// <summary>
        /// Los 14 tintes por orden de estreno (glosario §9, columna «Debut»; 07 §3; 06 §4.5; 02 §13 para 1-12), con el id de código
        /// del glosario §9 (el sujeto de su serie de Frutas del río y de sus claves, 08 §4.7). Paralela a
        /// <see cref="FruitDebutTintLevels"/>. Coinciden con el primer nivel de los packs en que sale cada tinte (lo comprueba
        /// OccupiedLevelsTests).
        /// </summary>
        public static readonly string[] FruitDebutTints =
        {
            "acai", "aguaje", "tapereba", "guanabana", "bacaba", "agua_tapajos", "camu_camu", "urucum", "agua_negro", "cupuacu",
            "lulo", "jenipapo", "cacao", "araza",
        };

        /// <summary>Nivel de estreno de cada tinte de <see cref="FruitDebutTints"/> (glosario §9).</summary>
        public static readonly int[] FruitDebutTintLevels = { 1, 1, 2, 4, 7, 11, 21, 41, 61, 81, 111, 141, 201, 301 };

        /// <summary>
        /// Debut de cada tinte: lleva la tarjeta «Fruta nueva» (07 §3, columna «Debut»; 06 §4.5), salvo en los niveles con
        /// instrucción del tutorial (el <see cref="FirstContactLevel"/> y el 2; <see cref="FruitNewAt"/>), que ocupan su nivel igual.
        /// Sale de <see cref="FruitDebutTintLevels"/>, en orden y sin repetidos: 1, 2, 4, 7, 11, 21, 41, 61, 81, 111, 141, 201 y 301.
        /// </summary>
        public static readonly int[] FruitDebutLevels = SortedUnion(FruitDebutTintLevels, Array.Empty<int>());

        // Claves de la frase de «Fruta nueva» (fact.<tinte>.tag) y del nombre visible (tint.<tinte>.name) de cada tinte de
        // FruitDebutTints (08 §4.7; pregunta 49 (a), D-022), preparadas una vez para que consultarlas no asigne memoria. Son las
        // mismas que FactSeries.TagKey y NameKey de la serie del tinte: OccupiedLevelsTests las compara.
        private static readonly string[] s_fruitTagKeys = FruitKeys(FactCatalog.KeyPrefix, ".tag");
        private static readonly string[] s_fruitNameKeys = FruitKeys(FactCatalog.NamePrefix, ".name");

        /// <summary>
        /// Estrenos con su tutorial de una pantalla (D-007; 02 §9.1 y §11; 04 §2.5), en orden: Cuia chica 14, Cuia alta 27, Hoja
        /// 43, Matapi 63, Liana con fruta 83, Aguas turbias y la Luciérnaga regalada 100, Remolino 103, Barro 123, Lluvia 143,
        /// Brote 163 y Piraña 183. Única copia en el código (la usan también los tests del catálogo y las capturas de los
        /// estrenos); no se modifica.
        /// </summary>
        public static readonly int[] MechanicDebutLevels = { 14, 27, 43, 63, 83, 100, 103, 123, 143, 163, 183 };

        /// <summary>
        /// Estrenos de las variantes de la 1.0 con su tarjeta de estreno (02 §9.1, «Variantes» y lista única de estrenos; D-007,
        /// propuesta de D-007 b.3; firma de <c>pack_schema</c> 3), en orden: Liana de cuenta 243 (<c>intro_mechanic</c>
        /// <c>liana_n</c> [P]), Hoja atada 283 (<c>hoja_atada</c>; VA1a), Brote de dos hojas 343 (<c>brote_n2</c> [P]) y Flor de
        /// vitória-régia 403 (<c>flor</c>; las dos, VA1b). Van aparte de <see cref="MechanicDebutLevels"/>, que sigue siendo la copia de los 11 estrenos de
        /// 1-200 que usan los tests de los paquetes y de las capturas, pero cuentan igual como hitos (<see cref="MilestoneLevels"/>):
        /// el nivel lleva su tarjeta de estreno y no comparte la de «¡Miquito a la vista!». La compara con
        /// <c>tools/level-generator/specs/campaign_v2.json</c> un test (SightingRuleTests).
        /// </summary>
        public static readonly int[] VariantDebutLevels = { 243, 283, 343, 403 };

        /// <summary>
        /// Hitos con su propia presentación (02 §13: Marea 3, Reto del río 7, Cuia extra 8, Mercado 10, contador de Marea y
        /// Cosecha doble 11, Morpho 12, Cántaro 20, animación corta 21, Pororoca 30, Canoas y Cuia de rescate 31), todos los
        /// estrenos de <see cref="MechanicDebutLevels"/> (D-007 y 04 §2.5: 14, 27, 43, 63, 83, 100, 103, 123, 143, 163 y 183) y
        /// los de las variantes (<see cref="VariantDebutLevels"/>: 243, 283, 343 y 403), en orden y sin repetidos. Se arma con las dos
        /// listas: un estreno nuevo no puede quedarse fuera.
        /// </summary>
        public static readonly int[] MilestoneLevels = SortedUnion(SortedUnion(new[] { 3, 7, 8, 10, 11, 12, 20, 21, 30, 31 }, MechanicDebutLevels), VariantDebutLevels);

        /// <summary>
        /// Estrenos de las variantes de capacidad sin tarjeta propia (02 §8 y 03 §9; <c>capacities[].debut</c> de
        /// <c>campaign_v1.json</c> y <c>campaign_v2.json</c>): Cuia chica de 1 en el 18 y de 3 en el 22, y Cuia alta de 6 en el 160
        /// (la primera, en un hito desde el 141). No llevan <c>intro_mechanic</c> en los packs, pero son novedad del tablero: el
        /// nivel queda ocupado y la tarjeta del miquito pasa al siguiente libre (propuesta de Frutas §3.3, paso F5). Las primeras
        /// de cada capacidad (14 y 27) ya están en <see cref="MechanicDebutLevels"/>.
        /// </summary>
        public static readonly int[] CapacityVariantLevels = { 18, 22, 160 };

        /// <summary>
        /// Estrenos de <c>campaign_v2.json</c> (<c>mechanics[].debut</c>) cuya tarjeta de estreno aún no tiene el cliente: la
        /// Tinaja en el 323 (D-019; 06 y 07, lista única de estrenos). El Brote de dos hojas (343) y la Flor (403) estuvieron aquí
        /// hasta que VA1b les dio su tarjeta (02 §9.1,
        /// «Variantes»). Ocupan ya su nivel (<see cref="IsBusy"/> y <see cref="OccupiedDebutLevels"/>): la tarjeta del miquito
        /// nunca cae en un estreno (07 §6.4; 02 §13), aunque sus packs lleguen antes que su tarjeta. No cuentan en
        /// <see cref="IsDebut"/> ni en <see cref="MilestoneLevels"/>, que siguen siendo los estrenos con tarjeta. Cuando un estreno
        /// tenga la suya (como el 343 y el 403 con VA1b, que pasaron a <see cref="VariantDebutLevels"/>; la Tinaja, con la tarea que le dé
        /// tarjeta, en <see cref="MechanicDebutLevels"/>), sale de aquí; mientras siga, la unión no lo repite.
        /// </summary>
        public static readonly int[] DebutsWithoutCardLevels = { 323 };

        /// <summary>
        /// Todos los estrenos de la campaña que ocupan su nivel (<c>mechanics[].debut</c> ∪ <c>capacities[].debut</c> de las
        /// especificaciones): <see cref="MechanicDebutLevels"/>, <see cref="VariantDebutLevels"/> y
        /// <see cref="CapacityVariantLevels"/> y <see cref="DebutsWithoutCardLevels"/>, en orden y sin repetidos. Lo compara con
        /// <c>campaign_v1.json</c> y <c>campaign_v2.json</c>, sin excepciones, OccupiedLevelsTests.
        /// </summary>
        public static readonly int[] OccupiedDebutLevels = SortedUnion(
            SortedUnion(SortedUnion(MechanicDebutLevels, VariantDebutLevels), CapacityVariantLevels), DebutsWithoutCardLevels);

        /// <summary>
        /// <paramref name="level"/> estrena una mecánica o una variante con su tarjeta de estreno (<see cref="MechanicDebutLevels"/>
        /// o <see cref="VariantDebutLevels"/>).
        /// </summary>
        public static bool IsDebut(int level)
        {
            return Array.IndexOf(MechanicDebutLevels, level) >= 0 || Array.IndexOf(VariantDebutLevels, level) >= 0;
        }

        /// <summary>
        /// El nivel ya tiene banner de mundo (<see cref="WorldBanners.ShowsOn"/>), Fruta nueva, un hito propio, un estreno de la
        /// campaña sin tarjeta propia (<see cref="CapacityVariantLevels"/> y <see cref="DebutsWithoutCardLevels"/>) o la
        /// instrucción del tutorial con TiTino (<see cref="TutorialInstruction.Has"/>: 1, 2, 3, 6 y 12; tareas TU1a y TU1b), que va la
        /// primera (tutorial &gt; Fruta nueva &gt; miquito &gt; rótulo).
        /// </summary>
        public static bool IsBusy(int level)
        {
            return TutorialInstruction.Has(level) || WorldBanners.ShowsOn(level) || Array.IndexOf(FruitDebutLevels, level) >= 0
                || Array.IndexOf(MilestoneLevels, level) >= 0 || Array.IndexOf(OccupiedDebutLevels, level) >= 0;
        }

        /// <summary>
        /// La tarjeta «Fruta nueva» de <paramref name="level"/> (06 §4.5; tarea F5): el índice en <see cref="FruitDebutTints"/> del
        /// tinte que se estrena en él, o −1 si no lleva. Solo en el primer intento (<paramref name="firstAttempt"/>; como el banner
        /// de mundo, ni al reintentar ni al reanudar), nunca en el <see cref="FirstContactLevel"/> (lo ocupa el tutorial) y como
        /// mucho una por nivel: en 1-300 sale en el 4, 7, 11, 21, 41, 61, 81, 111, 141 y 201 (y en el 301 cuando haya niveles).
        /// Gana a la tarjeta del miquito y al rótulo del lugar, que no salen en esos niveles (<see cref="IsBusy"/> y
        /// <see cref="StopLabelRule"/>). <b>Con el tutorial</b> (tarea TU1a): nunca en un nivel con instrucción del tutorial con
        /// TiTino (<see cref="TutorialInstruction.Has"/>: 1, 2, 3, 6 y 12). Tras la intro sale como mucho una tarjeta por nivel, con
        /// prioridad tutorial &gt; Fruta nueva &gt; miquito &gt; rótulo (D-023, punto 39; 02 §4.2; 07 §6.4; guion de la intro
        /// §14), así que en el 2 sale solo la instrucción y el taperebá se queda sin tarjeta, como el açaí y el aguaje en el 1. La
        /// fila del 2 de 06 §2.2 («Tarjeta "Fruta nueva: Taperebá"») y la lista de 06 §4.5 (que empieza en el 1) chocan con esa
        /// regla: la contradicción, registrada en la entrega de TU1a, la decidió el dueño con (a), sin «Fruta nueva» en el 1 ni en el 2
        /// (pregunta 55, D-029), y el carril de documentación alinea 06. Sin asignaciones.
        /// </summary>
        public static int FruitNewAt(int level, bool firstAttempt)
        {
            if (!firstAttempt || level <= FirstContactLevel || TutorialInstruction.Has(level))
            {
                return -1;
            }

            for (int i = 0; i < FruitDebutTintLevels.Length; i++)
            {
                if (FruitDebutTintLevels[i] == level)
                {
                    return i; // desde el 2, un solo tinte se estrena en cada nivel
                }
            }

            return -1;
        }

        /// <summary>Clave de la frase de «Fruta nueva» del tinte <paramref name="fruit"/> (<c>fact.&lt;tinte&gt;.tag</c>; 06 §4.5).</summary>
        public static string FruitTagKey(int fruit)
        {
            return s_fruitTagKeys[fruit];
        }

        /// <summary>Clave del nombre visible del tinte <paramref name="fruit"/> (<c>tint.&lt;tinte&gt;.name</c>; glosario §9).</summary>
        public static string FruitNameKey(int fruit)
        {
            return s_fruitNameKeys[fruit];
        }

        /// <summary>
        /// Primer nivel libre desde <paramref name="cardLevel"/> (incluido). <paramref name="tutorialPending"/> marca los niveles
        /// con un tutorial pendiente al empezar (p. ej. el de Marea en el 5 si no salió en el 3); null = ninguno.
        /// </summary>
        public static int FirstFreeLevel(int cardLevel, Func<int, bool>? tutorialPending = null)
        {
            int level = Math.Max(1, cardLevel);
            for (int guard = 0; guard < 1000; guard++, level++)
            {
                if (!IsBusy(level) && (tutorialPending == null || !tutorialPending(level)))
                {
                    return level;
                }
            }

            return level;
        }

        /// <summary>
        /// La tarjeta de una especie con <paramref name="cardLevel"/> sale en <paramref name="level"/>: es su primer nivel
        /// libre, el primer intento y la especie no se ha nombrado ya en este nivel.
        /// </summary>
        public static bool ShowsCard(int level, int cardLevel, bool firstAttempt, Func<int, bool>? tutorialPending = null)
        {
            return firstAttempt && level >= cardLevel && level == FirstFreeLevel(cardLevel, tutorialPending);
        }

        // Las claves de texto de los tintes de FruitDebutTints (una vez, al iniciar el tipo).
        private static string[] FruitKeys(string prefix, string suffix)
        {
            var keys = new string[FruitDebutTints.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = prefix + FruitDebutTints[i] + suffix;
            }

            return keys;
        }

        // Unión ordenada y sin repetidos de dos listas (una vez, al iniciar el tipo).
        private static int[] SortedUnion(int[] a, int[] b)
        {
            var merged = new int[a.Length + b.Length];
            Array.Copy(a, merged, a.Length);
            Array.Copy(b, 0, merged, a.Length, b.Length);
            Array.Sort(merged);
            int n = 0;
            for (int i = 0; i < merged.Length; i++)
            {
                if (n == 0 || merged[n - 1] != merged[i])
                {
                    merged[n++] = merged[i];
                }
            }

            Array.Resize(ref merged, n);
            return merged;
        }
    }
}
