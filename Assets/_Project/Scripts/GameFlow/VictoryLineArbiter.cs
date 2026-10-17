#nullable enable
using System;
using AF.Levels;

namespace AF.GameFlow
{
    /// <summary>
    /// Qué hace el dato de llegada del Mapa del río en la victoria (arquitectura de la tanda 4, §2; pregunta 45 de GDD.md §6.3).
    /// </summary>
    public enum RiverFactPolicy
    {
        /// <summary>
        /// Lo que dice hoy el canon (08 §9, <c>ui_river_fact_every_n_levels</c> = 10; pregunta 45, (a), D-022): en la victoria
        /// del <c>10k</c> con parada, la llegada lleva su dato y ocupa la línea; la línea de fruta de ese nivel queda en la
        /// ficha (Q4-A de D-017). Es el default (<see cref="MapConfig.RiverPolicy"/>).
        /// </summary>
        Canon = 0,

        /// <summary>
        /// La (b) de la pregunta 45: la llegada solo lleva la placa del lugar y la línea es la de fruta. Queda como interruptor
        /// por si el dueño cambia la respuesta.
        /// </summary>
        SingleKey = 1,
    }

    /// <summary>Qué pasa con la línea en los hitos de TiTino (T6-A de D-013 y D-016; P-M3 del plan del Mapa).</summary>
    public enum HitoLinePolicy
    {
        /// <summary>Los hitos son victorias normales: TiTino no habla en la ranura. Era el default antes de TI6a.</summary>
        ArrivalEverywhere = 0,

        /// <summary>
        /// En los hitos habla TiTino si puede (<see cref="VictoryContext.SlothCanSpeak"/>); la llegada va sin dato y el dato pasa
        /// a la victoria siguiente, el <c>10k+1</c> (T6-A).
        /// </summary>
        SlothHito = 1,
    }

    /// <summary>
    /// Entrada del árbitro de la línea de la victoria (<see cref="VictoryLineArbiter.For"/>). Valor puro, sin asignaciones.
    /// </summary>
    public readonly struct VictoryContext
    {
        /// <summary>Crea el contexto de la victoria del nivel <paramref name="won"/>.</summary>
        public VictoryContext(int won, bool mapAvailable, RiverStopCatalog? stops, string? fruitKey, RiverFactPolicy river,
            HitoLinePolicy hito, bool slothCanSpeak, int riverFactEveryN, int fruitEveryN)
        {
            Won = won;
            MapAvailable = mapAvailable;
            Stops = stops;
            FruitKey = fruitKey;
            River = river;
            Hito = hito;
            SlothCanSpeak = slothCanSpeak;
            RiverFactEveryN = riverFactEveryN;
            FruitEveryN = fruitEveryN;
        }

        /// <summary>Nivel ganado (1-1.000).</summary>
        public int Won { get; }

        /// <summary>El Mapa del río existe en el cliente: en el 10 cuenta él la primera llegada (02 §6; 06 §2.3).</summary>
        public bool MapAvailable { get; }

        /// <summary>Catálogo de paradas, o null si no se cargó (entonces no hay llegadas).</summary>
        public RiverStopCatalog? Stops { get; }

        /// <summary>Clave de la línea de fruta de este nivel (D-017; 08 §4.6), o null si el nivel no tiene línea.</summary>
        public string? FruitKey { get; }

        /// <summary>Política del dato de llegada (<see cref="MapConfig.RiverPolicy"/>).</summary>
        public RiverFactPolicy River { get; }

        /// <summary>Política de los hitos de TiTino (<see cref="MapConfig.HitoPolicy"/>).</summary>
        public HitoLinePolicy Hito { get; }

        /// <summary>
        /// TiTino puede hablar: <c>ui_sloth_enabled</c> ∧ <c>settings.sloth_messages</c> ∧ caja de TiTino ≥ 104 dp (propuesta de
        /// TiTino d.4), medido antes de decidir la línea. Solo cuenta en un hito y en el <c>10k+1</c> que lo sigue: en ese
        /// <c>10k+1</c> quien llama pasa el valor que se usó en la victoria del hito, porque el árbitro es puro y no recuerda
        /// si TiTino habló (así el dato desplazado sale una vez y solo una).
        /// </summary>
        public bool SlothCanSpeak { get; }

        /// <summary>Cadencia del dato de llegada: <c>ui_river_fact_every_n_levels</c> (08 §9; 10). 0 o menos: nunca.</summary>
        public int RiverFactEveryN { get; }

        /// <summary>
        /// Cadencia de la línea de fruta: <c>ui_fruit_line_every_n_levels</c> (08 §4.6 y §9; D-022): 1, en cada victoria; N &gt; 1,
        /// solo si el nivel es múltiplo de N; 0 o menos, nunca.
        /// </summary>
        public int FruitEveryN { get; }
    }

    /// <summary>
    /// La única línea de texto de una victoria (T6-A) y lo que la acompaña, según <see cref="VictoryLineArbiter.For"/>. Como
    /// mucho uno de <see cref="SlothSpeaks"/>, <see cref="ArrivalFactKey"/> y <see cref="FruitKey"/> es de texto. Valor puro.
    /// </summary>
    public readonly struct VictoryLine
    {
        /// <summary>Crea la línea (solo la construye el árbitro y los tests).</summary>
        public VictoryLine(bool arrival, bool arrivalFact, string? arrivalFactKey, bool deferredFact, string? fruitKey,
            bool slothSpeaks, bool slothBig)
        {
            Arrival = arrival;
            ArrivalFact = arrivalFact;
            ArrivalFactKey = arrivalFactKey;
            DeferredFact = deferredFact;
            FruitKey = fruitKey;
            SlothSpeaks = slothSpeaks;
            SlothBig = slothBig;
        }

        /// <summary>Hay franja de llegada a la parada (la placa del lugar; 02 §6, fila Llegada).</summary>
        public bool Arrival { get; }

        /// <summary>La franja de llegada lleva debajo la placa de su dato (<see cref="ArrivalFactKey"/>).</summary>
        public bool ArrivalFact { get; }

        /// <summary>
        /// Clave del dato del Mapa que sale en esta victoria, sea el de la llegada (<see cref="ArrivalFact"/>) o el desplazado
        /// (<see cref="DeferredFact"/>); null si no sale ninguno. Es el <c>fact_id</c> de <c>af_river_fact_shown</c>.
        /// </summary>
        public string? ArrivalFactKey { get; }

        /// <summary>
        /// Es el <c>10k+1</c> que sigue a un hito en el que habló TiTino: sale solo la placa del dato desplazado, con el retrato,
        /// en lugar de la línea de fruta (T6-A; D-022).
        /// </summary>
        public bool DeferredFact { get; }

        /// <summary>Clave de la línea de fruta que sale, o null (desplazada a la ficha, cadencia o sin línea).</summary>
        public string? FruitKey { get; }

        /// <summary>TiTino dice su frase de hito en esta victoria (la línea es suya).</summary>
        public bool SlothSpeaks { get; }

        /// <summary>El gesto de TiTino es <c>sloth_cheer_big</c>: el nivel es uno de sus hitos (02 §6, fila TiTino).</summary>
        public bool SlothBig { get; }
    }

    /// <summary>
    /// El árbitro de la línea de la victoria (arquitectura de la tanda 4, §2; 02 §6, filas «Dato del río», Llegada y TiTino;
    /// pregunta 45, (a), D-022; T6-A): una sola línea de texto por victoria, con prioridad frase de TiTino en sus hitos &gt; dato
    /// de llegada &gt; línea de fruta. Sin motor, determinista y sin asignaciones.
    /// <list type="bullet">
    /// <item>Nivel 10: con el Mapa, la ranura queda vacía (el Mapa del 10 cuenta la llegada y el dato 1, 06 §2.3); sin él, la
    /// línea de fruta.</item>
    /// <item>Niveles <c>10k</c> con parada (k ≥ 2): con <see cref="RiverFactPolicy.Canon"/>, llegada con dato y sin línea de
    /// fruta (queda en la ficha); con <see cref="RiverFactPolicy.SingleKey"/>, la llegada solo con la placa, más la línea de
    /// fruta.</item>
    /// <item>Hitos (<see cref="IsSlothHito"/>): con <see cref="HitoLinePolicy.SlothHito"/> y <see cref="VictoryContext.SlothCanSpeak"/>,
    /// habla TiTino, la llegada va sin dato y el dato pasa al <c>10k+1</c> (<see cref="VictoryLine.DeferredFact"/>), sin línea
    /// de fruta. Si TiTino no puede hablar, el hito es una victoria normal.</item>
    /// <item>El resto: la línea de fruta según su cadencia.</item>
    /// </list>
    /// </summary>
    public static class VictoryLineArbiter
    {
        // Hitos de T6-A además de los fines de mundo: el primer Difícil (40) y el primer Muy difícil (60).
        private const int FirstHardLevel = 40;
        private const int FirstVeryHardLevel = 60;

        /// <summary>
        /// Contexto con los defaults del cliente (<see cref="MapConfig.RiverPolicy"/>, <see cref="MapConfig.HitoPolicy"/>,
        /// <see cref="MapConfig.UiRiverFactEveryNLevels"/> y la cadencia de fruta, <see cref="GameConfig.FruitLineEveryN"/> [P],
        /// <c>ui_fruit_line_every_n_levels</c> de 08 §9) para la victoria del nivel
        /// <paramref name="won"/>.
        /// </summary>
        public static VictoryContext ContextFor(int won, bool mapAvailable, RiverStopCatalog? stops, string? fruitKey, bool slothCanSpeak)
        {
            return new VictoryContext(won, mapAvailable, stops, fruitKey, MapConfig.RiverPolicy, MapConfig.HitoPolicy, slothCanSpeak,
                MapConfig.UiRiverFactEveryNLevels, GameConfig.FruitLineEveryN);
        }

        /// <summary>
        /// El nivel <paramref name="won"/> es un hito de TiTino (T6-A; 02 §6, fila TiTino; contrato 6): el último de cada mundo
        /// según <see cref="WorldTable.LastLevelOf"/> salvo el 10, más el 40 (primer Difícil) y el 60 (primer Muy difícil).
        /// En 1-1.000: 40, 60, 80, 140, 200, 300, 420, 560, 700, 820, 920, 960 y 1.000.
        /// </summary>
        public static bool IsSlothHito(int won)
        {
            if (won == FirstHardLevel || won == FirstVeryHardLevel)
            {
                return true;
            }

            for (int world = 2; world <= WorldTable.WorldCount; world++)
            {
                if (WorldTable.LastLevelOf(world) == won)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>La línea de la victoria de <see cref="VictoryContext.Won"/> (véase el resumen de la clase).</summary>
        /// <exception cref="ArgumentOutOfRangeException">El nivel no está en 1..1.000.</exception>
        public static VictoryLine For(in VictoryContext c)
        {
            int won = c.Won;
            if (won < 1 || won > WorldTable.LastLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(c), won, "La primera vuelta va del nivel 1 al 1.000 (glosario §8).");
            }

            bool hito = IsSlothHito(won);
            bool slothSpeaks = hito && SlothSpeaksAt(in c);

            // En el 10 la llegada la cuenta el Mapa (02 §6): nunca hay franja en la victoria, con o sin Mapa.
            RiverStop? stop = won == RiverStopCatalog.LevelsPerStop ? null : c.Stops?.ArrivalOnWin(won);
            bool arrival = stop != null;

            string? factKey = null;
            bool arrivalFact = false;
            bool deferred = false;
            if (stop != null)
            {
                if (!slothSpeaks && ArrivalFactDue(in c, won) && stop.ArrivalFact != null)
                {
                    factKey = stop.ArrivalFact;
                    arrivalFact = true;
                }
            }
            else
            {
                factKey = DeferredFactKey(in c, won);
                deferred = factKey != null;
            }

            bool slotTaken = slothSpeaks || factKey != null || (won == RiverStopCatalog.LevelsPerStop && c.MapAvailable);
            string? fruitKey = !slotTaken && FruitDue(c.FruitEveryN, won) ? c.FruitKey : null;
            return new VictoryLine(arrival, arrivalFact, factKey, deferred, fruitKey, slothSpeaks, hito);
        }

        // TiTino habla en un hito: política SlothHito y puede hablar.
        private static bool SlothSpeaksAt(in VictoryContext c)
        {
            return c.Hito == HitoLinePolicy.SlothHito && c.SlothCanSpeak;
        }

        // El dato de llegada toca en el nivel (política Canon y cadencia de ui_river_fact_every_n_levels).
        private static bool ArrivalFactDue(in VictoryContext c, int won)
        {
            return c.River == RiverFactPolicy.Canon && c.RiverFactEveryN > 0 && won % c.RiverFactEveryN == 0;
        }

        // Dato que TiTino desplazó en el hito 10k y sale en el 10k+1, o null.
        private static string? DeferredFactKey(in VictoryContext c, int won)
        {
            int hito = won - 1;
            if (hito < RiverStopCatalog.LevelsPerStop || hito % RiverStopCatalog.LevelsPerStop != 0 || !IsSlothHito(hito)
                || !SlothSpeaksAt(in c) || !ArrivalFactDue(in c, hito))
            {
                return null;
            }

            RiverStop? stop = c.Stops?.ArrivalOnWin(hito);
            return stop?.ArrivalFact;
        }

        // Cadencia de la línea de fruta (08 §4.6): 1, siempre; N > 1, en los múltiplos de N; 0 o menos, nunca.
        private static bool FruitDue(int everyN, int won)
        {
            return everyN == 1 || (everyN > 1 && won % everyN == 0);
        }
    }
}
