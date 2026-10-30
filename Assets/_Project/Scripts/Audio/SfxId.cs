#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// Los 61 efectos de sonido de <c>docs/audio/lista-de-sonidos.md</c> (GDD 07 §9.2), en el orden del catálogo del laboratorio
    /// (<c>docs/audio/laboratorio-sonidos.html</c>, <c>SFX_CATALOG</c>): §1 tablero y vertido (13), §2 comodines, economía e
    /// interfaz (17), §3 mecánicas y personajes (21) y §4 Mapa, llegadas y pantallas especiales (10). El ID de texto de cada uno
    /// (<see cref="SfxCatalog.Name"/>) es el de la lista; los de §3 y §4 son [P] hasta encargarlos (07 §9.5). Todos son
    /// **provisionales por código** (tarea AU1; D-026): las grabaciones con licencia los sustituyen sin tocar los ganchos.
    /// </summary>
    public enum SfxId
    {
        /// <summary><c>sfx_select</c>: tocar una cuia para elegirla, afinado a la fruta de arriba.</summary>
        Select = 0,

        /// <summary><c>sfx_pour_start</c>: empieza el vertido.</summary>
        PourStart,

        /// <summary><c>sfx_pour_loop</c>: el chorro mientras cae (bucle continuo; su tono sigue al nivel del destino).</summary>
        PourLoop,

        /// <summary><c>sfx_pour_end</c>: termina el vertido.</summary>
        PourEnd,

        /// <summary><c>sfx_illegal</c>: vertido no permitido (madera neutra; nunca con háptico).</summary>
        Illegal,

        /// <summary><c>sfx_seal</c>: una cuia se completa y se tapa.</summary>
        Seal,

        /// <summary><c>sfx_river_rise</c>: intro, el río crece.</summary>
        RiverRise,

        /// <summary><c>sfx_fill</c>: intro, se llena una fila de cuias.</summary>
        Fill,

        /// <summary><c>sfx_fruit_plip</c>: intro, cae una fruta en el agua.</summary>
        FruitPlip,

        /// <summary><c>sfx_tint</c>: intro, una racha se tiñe (afinado a su fruta; como mucho 6 a la vez).</summary>
        Tint,

        /// <summary><c>sfx_win_toada</c>: victoria.</summary>
        WinToada,

        /// <summary><c>sfx_confetti</c>: victoria, confeti de plumas.</summary>
        Confetti,

        /// <summary><c>sfx_stuck</c>: Aguas estancadas.</summary>
        Stuck,

        /// <summary><c>sfx_undo</c>: Marea.</summary>
        Undo,

        /// <summary><c>sfx_capi_paddle</c>: Cuia extra o de rescate, llega Capi.</summary>
        CapiPaddle,

        /// <summary><c>sfx_hint</c>: Morpho (pista).</summary>
        Hint,

        /// <summary><c>sfx_shuffle</c>: Pororoca.</summary>
        Shuffle,

        /// <summary><c>sfx_reveal</c>: Luciérnaga.</summary>
        Reveal,

        /// <summary><c>sfx_skip</c>: Salto del Boto.</summary>
        Skip,

        /// <summary><c>sfx_restart</c>: Volver a la orilla.</summary>
        Restart,

        /// <summary><c>sfx_river_drop</c>: El río bajó.</summary>
        RiverDrop,

        /// <summary><c>sfx_seeds_tick</c>: el contador de semillas sube (1 por 10 semillas, máx. 20).</summary>
        SeedsTick,

        /// <summary><c>sfx_basket_open</c>: Canasta.</summary>
        BasketOpen,

        /// <summary><c>sfx_streak_up</c>: Crecida, sube la racha.</summary>
        StreakUp,

        /// <summary><c>sfx_streak_shield</c>: Hoja escudo.</summary>
        StreakShield,

        /// <summary><c>sfx_canoe_lost</c>: se pierde una canoa.</summary>
        CanoeLost,

        /// <summary><c>sfx_canoe_back</c>: se recupera una canoa.</summary>
        CanoeBack,

        /// <summary><c>sfx_ui_tap</c>: cualquier botón.</summary>
        UiTap,

        /// <summary><c>sfx_ui_back</c>: Atrás o cerrar.</summary>
        UiBack,

        /// <summary><c>sfx_ui_buy</c>: compra.</summary>
        UiBuy,

        /// <summary><c>sfx_mech_hoja</c>: se desbloquea la Hoja.</summary>
        MechHoja,

        /// <summary><c>sfx_mech_matapi</c>: se abre o se completa el Matapi.</summary>
        MechMatapi,

        /// <summary><c>sfx_mech_liana</c>: se suelta la Liana.</summary>
        MechLiana,

        /// <summary><c>sfx_mech_liana_cuenta</c>: baja la cuenta de la Liana de cuenta.</summary>
        MechLianaCuenta,

        /// <summary><c>sfx_mech_remolino</c>: el chorro pasa por el Remolino.</summary>
        MechRemolino,

        /// <summary><c>sfx_mech_barro</c>: se lava el Barro.</summary>
        MechBarro,

        /// <summary><c>sfx_mech_lluvia</c>: llueve sobre el tablero.</summary>
        MechLluvia,

        /// <summary><c>sfx_mech_pirana</c>: la Piraña muerde.</summary>
        MechPirana,

        /// <summary><c>sfx_mech_brote</c>: se abre el Brote.</summary>
        MechBrote,

        /// <summary><c>sfx_mech_flor</c>: se abre la Flor.</summary>
        MechFlor,

        /// <summary><c>sfx_mech_hoja_atada</c>: se suelta la Hoja atada.</summary>
        MechHojaAtada,

        /// <summary><c>sfx_tinaja_select</c>: elegir la Tinaja.</summary>
        TinajaSelect,

        /// <summary><c>sfx_tinaja_scoop</c>: sacar agua de la Tinaja.</summary>
        TinajaScoop,

        /// <summary><c>sfx_tinaja_reveal</c>: se revela la boca de la Tinaja.</summary>
        TinajaReveal,

        /// <summary><c>sfx_tinaja_release</c>: se suelta su cuia chica.</summary>
        TinajaRelease,

        /// <summary><c>sfx_sloth_nod</c>: TiTino asiente.</summary>
        SlothNod,

        /// <summary><c>sfx_sloth_happy</c>: TiTino celebra.</summary>
        SlothHappy,

        /// <summary><c>sfx_sloth_branch</c>: entra la rama de TiTino.</summary>
        SlothBranch,

        /// <summary><c>sfx_sloth_yawn</c>: TiTino se duerme.</summary>
        SlothYawn,

        /// <summary><c>sfx_rosa_surface</c>: Rosa asoma.</summary>
        RosaSurface,

        /// <summary><c>sfx_monkey_chirp</c>: tropa de miquitos en la intro (como mucho uno).</summary>
        MonkeyChirp,

        /// <summary><c>sfx_fact</c>: llegada a una parada (victoria del 10k).</summary>
        Fact,

        /// <summary><c>sfx_world_banner</c>: banner de mundo nuevo (la piha, solo aquí).</summary>
        WorldBanner,

        /// <summary><c>sfx_map_paddle</c>: Capi rema en el Mapa.</summary>
        MapPaddle,

        /// <summary><c>sfx_map_sample</c>: se llena una muestra en el Mapa.</summary>
        MapSample,

        /// <summary><c>sfx_map_rosa</c>: Rosa en el Mapa.</summary>
        MapRosa,

        /// <summary><c>sfx_map_pequepeque</c>: barcas en el Mapa (opcional).</summary>
        MapPequepeque,

        /// <summary><c>sfx_fruit_new</c>: tarjeta «Fruta nueva».</summary>
        FruitNew,

        /// <summary><c>sfx_mech_card</c>: tarjeta previa de una mecánica o reto.</summary>
        MechCard,

        /// <summary><c>sfx_morpho_idle</c>: pista del Morpho por inactividad.</summary>
        MorphoIdle,

        /// <summary><c>sfx_store_open</c>: se abre la tienda.</summary>
        StoreOpen,
    }

    /// <summary>Una fila del catálogo: ID de la lista, nivel objetivo y cómo se masteriza.</summary>
    public readonly struct SfxEntry
    {
        /// <summary>Crea la fila.</summary>
        public SfxEntry(SfxId id, string name, float targetPeakDb, bool canopy, bool gesture, bool fruitTuned)
        {
            Id = id;
            Name = name;
            TargetPeakDb = targetPeakDb;
            Canopy = canopy;
            Gesture = gesture;
            FruitTuned = fruitTuned;
        }

        /// <summary>El efecto.</summary>
        public SfxId Id { get; }

        /// <summary>ID de texto de la lista (<c>sfx_select</c>…).</summary>
        public string Name { get; }

        /// <summary>Pico objetivo (dBFS): la columna «dBFS» del catálogo del laboratorio.</summary>
        public float TargetPeakDb { get; }

        /// <summary>Lleva la reverberación de dosel del laboratorio (<c>useCanopyReverb</c>); los demás, sin cola añadida.</summary>
        public bool Canopy { get; }

        /// <summary>Gesto repetido (<c>select</c>, <c>tint</c>, <c>plip</c>, <c>ui_tap</c>, <c>ui_back</c>, <c>seeds_tick</c>, <c>pour_start</c> y <c>pour_end</c>): termina en ≤ 0,2 s.</summary>
        public bool Gesture { get; }

        /// <summary>Se afina a la fruta (14 notas pentatónicas): <c>sfx_select</c> y <c>sfx_tint</c>.</summary>
        public bool FruitTuned { get; }
    }

    /// <summary>
    /// Catálogo de los 61 efectos con su nivel objetivo (la columna dBFS del catálogo del laboratorio, que copia la jerarquía de
    /// Magic Sort de la lista: gestos a −24/−22 desde la observación 23 de la jornada 6 (antes −36/−27, tapados por la música),
    /// mecánicas y avisos a −24/−18, premios a −17/−12; 07 §9.4: picos ≤ −6 dBTP) y la escala de las 14 frutas (07 §3; tabla de
    /// la lista). Sin asignaciones tras el arranque.
    /// </summary>
    public static class SfxCatalog
    {
        /// <summary>Número de efectos (61).</summary>
        public const int Count = 61;

        /// <summary>Duración máxima de un gesto repetido (s), con su cola.</summary>
        public const float GestureMaxSeconds = 0.2f;

        // Frecuencia (Hz) de la nota de cada fruta por su índice de motor (1-14; 0 = sin fruta). Tabla de la lista y FRUITS del
        // laboratorio: C4 (Huito) a G6 (Guanábana), de la más oscura a la más clara.
        private static readonly double[] FruitHz =
        {
            0.0,
            1567.98, // 1 guanabana   · Guanábana  · G6
            1318.51, // 2 cupuacu     · Copoazú    · E6
            1046.50, // 3 tapereba    · Taperebá   · C6
            587.33,  // 4 aguaje      · Aguaje     · D5
            440.00,  // 5 urucum      · Achiote    · A4
            392.00,  // 6 camu_camu   · Camu camu  · G4
            783.99,  // 7 bacaba      · Bacaba     · G5
            329.63,  // 8 acai        · Açaí       · E4
            261.63,  // 9 jenipapo    · Huito      · C4
            880.00,  // 10 agua_tapajos · Tapajós  · A5
            659.25,  // 11 lulo       · Lulo       · E5
            1174.66, // 12 araza      · Arazá      · D6
            293.66,  // 13 cacao      · Cacao      · D4
            523.25,  // 14 agua_negro · Río Negro  · C5
        };

        private static readonly SfxEntry[] Entries =
        {
            // §1. Tablero y vertido (13)
            new SfxEntry(SfxId.Select, "sfx_select", -24f, false, true, true),
            new SfxEntry(SfxId.PourStart, "sfx_pour_start", -22f, false, true, false),
            new SfxEntry(SfxId.PourLoop, "sfx_pour_loop", -23f, false, false, false),
            new SfxEntry(SfxId.PourEnd, "sfx_pour_end", -22f, false, true, false),
            new SfxEntry(SfxId.Illegal, "sfx_illegal", -28f, false, false, false),
            new SfxEntry(SfxId.Seal, "sfx_seal", -17f, false, false, false),
            new SfxEntry(SfxId.RiverRise, "sfx_river_rise", -26f, false, false, false),
            new SfxEntry(SfxId.Fill, "sfx_fill", -21f, false, false, false),
            new SfxEntry(SfxId.FruitPlip, "sfx_fruit_plip", -23f, false, true, false),
            new SfxEntry(SfxId.Tint, "sfx_tint", -23f, false, true, true),
            new SfxEntry(SfxId.WinToada, "sfx_win_toada", -14f, true, false, false),
            new SfxEntry(SfxId.Confetti, "sfx_confetti", -20f, false, false, false),
            new SfxEntry(SfxId.Stuck, "sfx_stuck", -20f, false, false, false),

            // §2. Comodines, economía e interfaz (17)
            new SfxEntry(SfxId.Undo, "sfx_undo", -22f, false, false, false),
            new SfxEntry(SfxId.CapiPaddle, "sfx_capi_paddle", -18f, false, false, false),
            new SfxEntry(SfxId.Hint, "sfx_hint", -20f, false, false, false),
            new SfxEntry(SfxId.Shuffle, "sfx_shuffle", -18f, false, false, false),
            new SfxEntry(SfxId.Reveal, "sfx_reveal", -19f, false, false, false),
            new SfxEntry(SfxId.Skip, "sfx_skip", -17f, true, false, false),
            new SfxEntry(SfxId.Restart, "sfx_restart", -22f, false, false, false),
            new SfxEntry(SfxId.RiverDrop, "sfx_river_drop", -18f, false, false, false),
            new SfxEntry(SfxId.SeedsTick, "sfx_seeds_tick", -26f, false, true, false),
            new SfxEntry(SfxId.BasketOpen, "sfx_basket_open", -16f, false, false, false),
            new SfxEntry(SfxId.StreakUp, "sfx_streak_up", -17f, false, false, false),
            new SfxEntry(SfxId.StreakShield, "sfx_streak_shield", -19f, false, false, false),
            new SfxEntry(SfxId.CanoeLost, "sfx_canoe_lost", -21f, false, false, false),
            new SfxEntry(SfxId.CanoeBack, "sfx_canoe_back", -19f, false, false, false),
            new SfxEntry(SfxId.UiTap, "sfx_ui_tap", -24f, false, true, false),
            new SfxEntry(SfxId.UiBack, "sfx_ui_back", -24f, false, true, false),
            new SfxEntry(SfxId.UiBuy, "sfx_ui_buy", -18f, false, false, false),

            // §3. Mecánicas y personajes (21)
            new SfxEntry(SfxId.MechHoja, "sfx_mech_hoja", -20f, false, false, false),
            new SfxEntry(SfxId.MechMatapi, "sfx_mech_matapi", -19f, false, false, false),
            new SfxEntry(SfxId.MechLiana, "sfx_mech_liana", -19f, false, false, false),
            new SfxEntry(SfxId.MechLianaCuenta, "sfx_mech_liana_cuenta", -24f, false, false, false),
            new SfxEntry(SfxId.MechRemolino, "sfx_mech_remolino", -20f, false, false, false),
            new SfxEntry(SfxId.MechBarro, "sfx_mech_barro", -20f, false, false, false),
            new SfxEntry(SfxId.MechLluvia, "sfx_mech_lluvia", -21f, false, false, false),
            new SfxEntry(SfxId.MechPirana, "sfx_mech_pirana", -19f, false, false, false),
            new SfxEntry(SfxId.MechBrote, "sfx_mech_brote", -20f, false, false, false),
            new SfxEntry(SfxId.MechFlor, "sfx_mech_flor", -18f, false, false, false),
            new SfxEntry(SfxId.MechHojaAtada, "sfx_mech_hoja_atada", -20f, false, false, false),
            new SfxEntry(SfxId.TinajaSelect, "sfx_tinaja_select", -26f, false, false, false),
            new SfxEntry(SfxId.TinajaScoop, "sfx_tinaja_scoop", -22f, false, false, false),
            new SfxEntry(SfxId.TinajaReveal, "sfx_tinaja_reveal", -25f, true, false, false),
            new SfxEntry(SfxId.TinajaRelease, "sfx_tinaja_release", -19f, false, false, false),
            new SfxEntry(SfxId.SlothNod, "sfx_sloth_nod", -22f, false, false, false),
            new SfxEntry(SfxId.SlothHappy, "sfx_sloth_happy", -20f, false, false, false),
            new SfxEntry(SfxId.SlothBranch, "sfx_sloth_branch", -24f, false, false, false),
            new SfxEntry(SfxId.SlothYawn, "sfx_sloth_yawn", -24f, false, false, false),
            new SfxEntry(SfxId.RosaSurface, "sfx_rosa_surface", -19f, false, false, false),
            new SfxEntry(SfxId.MonkeyChirp, "sfx_monkey_chirp", -34f, false, false, false),

            // §4. Mapa del río, llegadas y pantallas especiales (10)
            new SfxEntry(SfxId.Fact, "sfx_fact", -21f, false, false, false),
            new SfxEntry(SfxId.WorldBanner, "sfx_world_banner", -14f, true, false, false),
            new SfxEntry(SfxId.MapPaddle, "sfx_map_paddle", -20f, false, false, false),
            new SfxEntry(SfxId.MapSample, "sfx_map_sample", -18f, false, false, false),
            new SfxEntry(SfxId.MapRosa, "sfx_map_rosa", -21f, false, false, false),
            new SfxEntry(SfxId.MapPequepeque, "sfx_map_pequepeque", -28f, false, false, false),
            new SfxEntry(SfxId.FruitNew, "sfx_fruit_new", -17f, false, false, false),
            new SfxEntry(SfxId.MechCard, "sfx_mech_card", -20f, false, false, false),
            new SfxEntry(SfxId.MorphoIdle, "sfx_morpho_idle", -28f, false, false, false),
            new SfxEntry(SfxId.StoreOpen, "sfx_store_open", -19f, false, false, false),
        };

        /// <summary>Número de frutas del motor (14).</summary>
        public const int FruitCount = 14;

        /// <summary>La fila de <paramref name="id"/>.</summary>
        public static SfxEntry Get(SfxId id)
        {
            int i = (int)id;
            if ((uint)i >= (uint)Entries.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Efecto desconocido.");
            }

            return Entries[i];
        }

        /// <summary>ID de texto de la lista (<c>sfx_select</c>…).</summary>
        public static string Name(SfxId id)
        {
            return Get(id).Name;
        }

        /// <summary>El efecto de ID de texto <paramref name="name"/>; false si no está en la lista.</summary>
        public static bool TryParse(string? name, out SfxId id)
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                if (string.Equals(Entries[i].Name, name, StringComparison.Ordinal))
                {
                    id = Entries[i].Id;
                    return true;
                }
            }

            id = SfxId.Select;
            return false;
        }

        /// <summary>
        /// Frecuencia de la nota de la fruta <paramref name="tint"/> (índice de motor 1-14), o 0 si no es una fruta (agua, Barro,
        /// oculta): el efecto afinado usa entonces la nota por defecto del laboratorio.
        /// </summary>
        public static double FruitFrequency(int tint)
        {
            return tint >= 1 && tint <= FruitCount ? FruitHz[tint] : 0.0;
        }
    }
}
