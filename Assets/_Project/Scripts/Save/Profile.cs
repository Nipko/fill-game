#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Platform;

namespace AF.Save
{
    /// <summary>
    /// Perfil del jugador guardado en <c>profile.json</c> (GDD 08 §4.3, que extiende el inventario de 04 §10.1), con
    /// <c>profile_schema</c> = <see cref="CurrentSchema"/>. Contiene el subconjunto que usa la pista B (D-008; tareas B1-1,
    /// B1-3 y B1-4): <c>created_at</c>, <c>economy_ts</c>, <c>wallet</c> (semillas y canoas), <c>stock</c>, <c>daily</c>,
    /// <c>progress</c> (con <c>map_seen_level</c>, D-013), <c>level_state</c>, <c>entitlements</c> (<c>no_ads</c>,
    /// <c>starter_bought</c> y <c>skins</c>), <c>settings</c> (<c>patterns</c>, <c>language</c> y <c>sloth_messages</c>,
    /// D-016 y D-018; <c>idle_hint</c> [P]), <c>sloth</c> [P] (D-016), <c>ads</c> y <c>stats</c>, con los nombres exactos
    /// del GDD. El bloque <c>ads</c> y <c>daily.af_rescue_hidden_until</c> no están en 08 §4.3: los nombres salen del
    /// pseudocódigo de 05 §6.3 y de la regla de 48 h de 05 §4.1 [P]. El resto de 08 §4.3 (consentimiento, uid, calendario,
    /// álbum, sonido…) llega con su sistema. <c>settings</c>, <c>sloth</c> y <c>map_seen_level</c> entran con el esquema 1
    /// sin migrar, porque aún no se ha publicado (08 §4.3; D-013 punto 14 y D-016 punto 4). El esquema 2 añade <c>fauna</c>
    /// [P] (los avistamientos de la tarjeta «¡Miquito a la vista!», Q11 del guion de la intro; D-029; tarea DC15), con su paso
    /// de migración 1 → 2 (<see cref="ProfileMigrations"/>), y <c>progress.undo_tip_seen</c> [P] (el tooltip de Marea, una vez
    /// en la vida; D-030, pregunta 15 (b); tarea DC23), que entra en el mismo esquema 2 sin paso propio porque aún no se ha
    /// publicado. La Crecida (B1a; Q-M9 (a) [P]) añade <c>streak</c> (omitido mientras es null) y <c>daily.streak_hits</c> y
    /// <c>daily.streak_seeds</c> (omitidos en su default), también sin migrar.
    /// <para>
    /// Es un contenedor de datos: las reglas (topes, precios, regeneración de canoas, reinicio diario) viven en
    /// <c>AF.Economy</c>, <c>AF.Ads</c> y <c>AF.Boosters</c>, que lo mutan y lo guardan con <see cref="IProfileStore"/>.
    /// Las horas son UTC. Una clave que falta en el archivo toma el valor por defecto de esta clase; una clave
    /// desconocida se ignora. Añadir un campo nuevo con un default razonable no exige migración mientras no se haya
    /// publicado el esquema; cambiar o quitar uno publicado sí (<see cref="ProfileMigrations"/>).
    /// </para>
    /// </summary>
    public sealed class Profile
    {
        /// <summary>
        /// Versión del esquema que escribe esta build (08 §4.4: solo avanza). 2 desde DC15: <c>fauna.sighted</c>, con el paso
        /// 1 → 2 de <see cref="ProfileMigrations"/>.
        /// </summary>
        public const int CurrentSchema = 2;

        /// <summary><c>profile_schema</c>: siempre <see cref="CurrentSchema"/> en memoria (las migraciones van antes).</summary>
        public int ProfileSchema => CurrentSchema;

        /// <summary><c>created_at</c>: creación del perfil (primera ejecución), UTC.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// <c>economy_ts</c>: última mutación de <c>wallet</c>, <c>stock</c> o <c>daily</c> (08 §4.3), UTC; null si
        /// nunca ha cambiado. Desempata los conflictos de guardado a igual nivel máximo (08 §11).
        /// </summary>
        public DateTime? EconomyTs { get; set; }

        /// <summary><c>wallet</c>: semillas y canoas.</summary>
        public WalletData Wallet { get; } = new WalletData();

        /// <summary><c>stock</c>: comodines comprados o regalados.</summary>
        public StockData Stock { get; } = new StockData();

        /// <summary><c>daily</c>: contadores del día local (04 §10.1).</summary>
        public DailyData Daily { get; } = new DailyData();

        /// <summary><c>progress</c>: avance en la campaña.</summary>
        public ProgressData Progress { get; } = new ProgressData();

        /// <summary><c>level_state</c>: nivel en curso (02 R8 y §14; 04 §10.1), o null si no hay ninguno.</summary>
        public LevelStateData? LevelState { get; set; }

        /// <summary><c>entitlements</c>: derechos comprados.</summary>
        public EntitlementsData Entitlements { get; } = new EntitlementsData();

        /// <summary><c>settings</c>: los ajustes del jugador que ya existen en el juego (08 §4.3).</summary>
        public SettingsData Settings { get; } = new SettingsData();

        /// <summary><c>sloth</c> [P]: lo que el perezoso recuerda entre sesiones (D-016 punto 4; propuesta de TiTino d.10).</summary>
        public SlothData Sloth { get; } = new SlothData();

        /// <summary><c>ads</c>: estado de la cadencia de intersticiales que dura más que una sesión (05 §6.3) [P].</summary>
        public AdsData Ads { get; } = new AdsData();

        /// <summary><c>stats</c>: contadores agregados (nunca por nivel: eso va a analítica, 08 §4.3).</summary>
        public StatsData Stats { get; } = new StatsData();

        /// <summary>
        /// <c>fauna</c> [P] (esquema 2; Q11 del guion de la intro, D-029; tarea DC15): las especies de miquito cuya tarjeta
        /// «¡Miquito a la vista!» ya vio el jugador. Lo necesitan el Cuaderno de campo (1.1) y el puente en el Mapa del río.
        /// </summary>
        public FaunaData Fauna { get; } = new FaunaData();

        /// <summary>
        /// <c>daily_challenge</c> (08 §4.3 ampliado por R-Q4, R-Q5 y R-Q16; nota del Reto §10; enganche E4): el registro del Reto
        /// del río, o null si nunca se guardó (entonces la clave no se escribe). Vive en el mismo perfil que <see cref="Wallet"/>
        /// para que el registro del cobro y el pago salgan en la misma escritura (nunca dos pagos por la misma fecha). Las reglas
        /// las aplica <c>AF.LiveOps</c> (<c>ProfileDailyChallengeStore</c>); aquí solo se guardan. Entra sin subir el esquema (va
        /// en el 2, el de <c>fauna</c>, y la migración 1 → 2 la conserva) porque aún no se ha publicado (08 §4.3).
        /// </summary>
        public DailyChallengeData? DailyChallenge { get; set; }

        /// <summary>
        /// <c>streak</c> (08 §4.3 con Q-M9 (a) [P] del plan de la tanda 5; 06 §4.3; 05 §2.4): la Crecida, <c>{wins, river_full}</c>,
        /// o null si nunca se guardó (entonces la clave no se escribe). El <c>shield_available</c> de 08 §4.3 no está aquí: hay uno
        /// solo, el de <c>daily</c> (04 §10.1), junto a los hitos y las semillas de hoy (<see cref="DailyData.StreakHits"/> y
        /// <see cref="DailyData.StreakSeeds"/>), que se reinician con <c>date_local</c>. Vive en el mismo perfil que
        /// <see cref="Wallet"/> para que el apunte de un hito y su pago salgan en la misma escritura (un hito nunca se paga dos veces
        /// el mismo día). Las reglas las aplica <c>AF.Economy</c> (<c>ProfileStreakStore</c>); aquí solo se guardan. Entra sin subir
        /// el esquema (va en el 2, y la migración 1 → 2 la conserva) porque aún no se ha publicado (08 §4.3).
        /// </summary>
        public StreakData? Streak { get; set; }

        /// <summary>
        /// Perfil de primera ejecución: <c>created_at</c> = ahora, 0 semillas (el GDD no fija otro saldo inicial),
        /// <paramref name="initialCanoes"/> canoas, stock vacío, sin nivel en curso y el nivel 1 como siguiente
        /// (08 §2.3: la primera vez se entra directamente al nivel 1).
        /// </summary>
        /// <param name="clock">Reloj del que sale <c>created_at</c>.</param>
        /// <param name="initialCanoes">Canoas iniciales; quien llama pasa <c>canoes_max</c> (08 §9; 04 §7.1: debut con 5/5).</param>
        public static Profile CreateNew(IClock clock, int initialCanoes)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            if (initialCanoes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCanoes), "Las canoas iniciales no pueden ser negativas.");
            }

            var profile = new Profile { CreatedAt = clock.UtcNow };
            profile.Wallet.Canoes.Count = initialCanoes;
            return profile;
        }

        /// <summary>Copia profunda (ida y vuelta por JSON, la misma que el guardado).</summary>
        public Profile Clone()
        {
            return ProfileJson.Deserialize(ProfileJson.Serialize(this));
        }
    }

    /// <summary><c>wallet</c> (08 §4.3).</summary>
    public sealed class WalletData
    {
        /// <summary><c>seeds</c>: saldo de semillas.</summary>
        public int Seeds { get; set; }

        /// <summary><c>canoes</c>.</summary>
        public CanoesData Canoes { get; } = new CanoesData();
    }

    /// <summary><c>wallet.canoes</c> (04 §7.1; 04 §10.1).</summary>
    public sealed class CanoesData
    {
        /// <summary><c>count</c>: canoas disponibles.</summary>
        public int Count { get; set; }

        /// <summary><c>next_regen_at</c>: cuándo vuelve la próxima canoa (UTC), o null si están todas.</summary>
        public DateTime? NextRegenAt { get; set; }

        /// <summary><c>infinite_until</c>: fin de las Canoas infinitas (UTC), o null si no hay.</summary>
        public DateTime? InfiniteUntil { get; set; }
    }

    /// <summary><c>stock</c> (04 §10.1): unidades de cada comodín.</summary>
    public sealed class StockData
    {
        /// <summary><c>undo</c> (Marea).</summary>
        public int Undo { get; set; }

        /// <summary><c>hint</c> (Morpho).</summary>
        public int Hint { get; set; }

        /// <summary><c>reveal</c> (Luciérnaga).</summary>
        public int Reveal { get; set; }

        /// <summary><c>extra_cuia</c> (Cuia extra).</summary>
        public int ExtraCuia { get; set; }

        /// <summary><c>shuffle</c> (Pororoca).</summary>
        public int Shuffle { get; set; }
    }

    /// <summary>
    /// <c>daily</c>: contadores del día local, con las claves de 04 §10.1 más <c>interstitials_today</c> (tope diario de
    /// intersticiales, 05 §6.3) y los de la Crecida <c>streak_hits</c> y <c>streak_seeds</c> [P] (06 §4.3; Q-M9 (a)).
    /// Al cambiar <c>date_local</c> los pone a cero quien aplica la regla de cada uno.
    /// </summary>
    public sealed class DailyData
    {
        /// <summary><c>date_local</c>: día local de los contadores, <c>yyyy-MM-dd</c> (<see cref="ClockExtensions.LocalDateKey"/>), o null.</summary>
        public string? DateLocal { get; set; }

        /// <summary><c>rv_undo_refill</c>: recompensados vistos hoy en esa ubicación.</summary>
        public int RvUndoRefill { get; set; }

        /// <summary><c>rv_hint</c>.</summary>
        public int RvHint { get; set; }

        /// <summary><c>rv_reveal</c>.</summary>
        public int RvReveal { get; set; }

        /// <summary><c>rv_extra_canoe</c>.</summary>
        public int RvExtraCanoe { get; set; }

        /// <summary><c>rv_double_harvest</c>.</summary>
        public int RvDoubleHarvest { get; set; }

        /// <summary><c>rv_total</c>: recompensados de hoy en todas las ubicaciones.</summary>
        public int RvTotal { get; set; }

        /// <summary><c>interstitials_today</c>: intersticiales mostrados hoy.</summary>
        public int InterstitialsToday { get; set; }

        /// <summary><c>rescues_today</c>: Cuias de rescate compradas hoy (la primera va a mitad de precio).</summary>
        public int RescuesToday { get; set; }

        /// <summary><c>af_rescue_shown</c>: veces que se ofreció hoy la Balsa de rescate.</summary>
        public int AfRescueShown { get; set; }

        /// <summary><c>af_rescue_declined_days</c>: rechazos de la Balsa de rescate.</summary>
        public int AfRescueDeclinedDays { get; set; }

        /// <summary>
        /// <c>af_rescue_hidden_until</c> [P]: la Balsa de rescate está oculta hasta esta hora (UTC) tras
        /// <c>af_rescue_hide_after_declines</c> rechazos (05 §4.1: «tras 2 rechazos → oculta 48 h»), o null. No es un
        /// contador del día: el cambio de fecha no lo toca.
        /// </summary>
        public DateTime? AfRescueHiddenUntil { get; set; }

        /// <summary><c>shuffles_today</c>: Pororocas usadas hoy.</summary>
        public int ShufflesToday { get; set; }

        /// <summary>
        /// <c>shield_available</c>: Hoja escudo disponible hoy (1 por día local, <c>streak_shield_per_day</c>; 06 §4.3). Es el único
        /// <c>shield_available</c> del perfil: el bloque <c>streak</c> no lleva otro (Q-M9 (a) [P]).
        /// </summary>
        public bool ShieldAvailable { get; set; }

        /// <summary>
        /// <c>streak_hits</c> [P] (Q-M9 (a)): hitos de la Crecida ya pagados hoy (p. ej. <c>[3, 6]</c>; cada hito se paga una vez por
        /// día local, <c>streak_hits_once_per_day</c>, 06 §4.3). Vacía por defecto; vacía, la clave no se escribe.
        /// </summary>
        public List<int> StreakHits { get; } = new List<int>();

        /// <summary>
        /// <c>streak_seeds</c> [P] (Q-M9 (a)): semillas de los hitos pagadas hoy que cuentan para el tope diario
        /// (<c>streak_daily_cap</c>, 600; 06 §4.3). 0 por defecto; con 0, la clave no se escribe.
        /// </summary>
        public int StreakSeeds { get; set; }
    }

    /// <summary>
    /// <c>streak</c> (08 §4.3 con Q-M9 (a) [P]): la Crecida que no es del día. Solo datos: el significado lo da
    /// <c>AF.Economy.StreakRecord</c>.
    /// </summary>
    public sealed class StreakData
    {
        /// <summary><c>wins</c>: la racha (vuelve a 0 al pagar el hito 10 y tras un fallo duro no escudado; 05 §2.4).</summary>
        public int Wins { get; set; }

        /// <summary><c>river_full</c>: río crecido, hasta el siguiente fallo duro no escudado (08 §4.3; 05 §2.4).</summary>
        public bool RiverFull { get; set; }
    }

    /// <summary><c>progress</c> (08 §4.3; 04 §10.1; <c>map_seen_level</c>, D-013, y <c>undo_tip_seen</c> [P], DC23).</summary>
    public sealed class ProgressData
    {
        /// <summary><c>max_level</c>: nivel más alto superado (0 = ninguno).</summary>
        public int MaxLevel { get; set; }

        /// <summary><c>next_level</c>: siguiente nivel que se juega (1 en la primera ejecución).</summary>
        public int NextLevel { get; set; } = 1;

        /// <summary><c>since_last_skip</c>: niveles desde el último Salto del Boto.</summary>
        public int SinceLastSkip { get; set; }

        /// <summary><c>skipped_levels</c>: niveles saltados con el Salto del Boto.</summary>
        public List<int> SkippedLevels { get; } = new List<int>();

        /// <summary><c>content_version</c>: versión del catálogo de niveles con el que se jugó (08 §4.4), o null.</summary>
        public string? ContentVersion { get; set; }

        /// <summary>
        /// <c>map_seen_level</c> (D-013 punto 14; 08 §4.3): el último nivel cuyo estado del Mapa del río ya vio el jugador; 0
        /// por defecto. Lo escribe <c>AF.GameFlow</c> al cerrar el Mapa y tras la franja de llegada de la victoria (08 §4.3);
        /// con él, el Mapa anima exactamente lo pendiente al abrirse. Al guardar y al leer queda recortado a
        /// [0, <see cref="MaxLevel"/>] (<see cref="ClampedMapSeenLevel"/>); en la fusión de la nube gana el máximo
        /// (<see cref="MergeMapSeenLevel"/>).
        /// </summary>
        public int MapSeenLevel { get; set; }

        /// <summary><see cref="MapSeenLevel"/> recortado a [0, <see cref="MaxLevel"/>]: lo que se guarda.</summary>
        public int ClampedMapSeenLevel => ClampMapSeen(MapSeenLevel, MaxLevel);

        /// <summary>
        /// Fusión de 08 §11 (<c>ResolveConflict</c>): sobre el progreso del perfil ganador, <c>map_seen_level</c> pasa a ser
        /// el máximo de los dos, como <c>max_level</c>, recortado a [0, <see cref="MaxLevel"/>] del ganador.
        /// </summary>
        /// <param name="other">El progreso del otro perfil (el perdedor).</param>
        public void MergeMapSeenLevel(ProgressData other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            MapSeenLevel = ClampMapSeen(Math.Max(MapSeenLevel, other.MapSeenLevel), MaxLevel);
        }

        /// <summary>
        /// <c>undo_tip_seen</c> [P] (esquema 2; D-030, pregunta 15 (b); tarea DC23): el tooltip de Marea del tutorial ya salió
        /// («Marea · Deshacer», 04 §2.1; 02 §13 y 06 §2.2, fila del 3). Sale una sola vez en la vida: se apunta al mostrarse, en
        /// el 3 tras el primer vertido que no acerca o, si no salió allí, al inicio del 5. False por defecto (también en un perfil
        /// sin la clave: el esquema 2 aún no se ha publicado y entra sin migración); en la fusión de la nube basta con que uno
        /// de los dos lo haya visto (<see cref="MergeUndoTipSeen"/>).
        /// </summary>
        public bool UndoTipSeen { get; set; }

        /// <summary>
        /// Fusión de 08 §11 (<c>ResolveConflict</c>): sobre el progreso del perfil ganador, <c>undo_tip_seen</c> queda visto si
        /// lo vio cualquiera de los dos (un tooltip del tutorial no vuelve a salir por perder un conflicto) [P].
        /// </summary>
        /// <param name="other">El progreso del otro perfil (el perdedor).</param>
        public void MergeUndoTipSeen(ProgressData other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            UndoTipSeen = UndoTipSeen || other.UndoTipSeen;
        }

        private static int ClampMapSeen(int value, int maxLevel)
        {
            int top = Math.Max(0, maxLevel);
            return value < 0 ? 0 : value > top ? top : value;
        }
    }

    /// <summary>
    /// <c>level_state</c> (04 §10.1): el nivel en curso, referenciado por su semilla y su variante (no guarda el
    /// tablero), más el historial comprimido. El formato de <see cref="History"/> lo fija quien reanuda el nivel (B2-5).
    /// </summary>
    public sealed class LevelStateData
    {
        /// <summary><c>level</c>: número de nivel.</summary>
        public int Level { get; set; }

        /// <summary><c>seed</c>: semilla canónica <c>amazonfill:v1:m{mm}:v{v}:n{nnnn}</c> (08 §4.1).</summary>
        public string Seed { get; set; } = "";

        /// <summary><c>variant</c>: <c>base</c>, <c>plus_empty</c> o <c>minus_color</c> (la entrada de <c>relief[]</c> servida).</summary>
        public string Variant { get; set; } = "base";

        /// <summary><c>fails_level</c>: fallos en este nivel (04 §5.2).</summary>
        public int FailsLevel { get; set; }

        /// <summary><c>pours</c>: vertidos hechos.</summary>
        public int Pours { get; set; }

        /// <summary><c>extra_cuias</c>: cuias añadidas (Cuia extra y Cuia de rescate).</summary>
        public int ExtraCuias { get; set; }

        /// <summary><c>rescues_level</c>: Cuias de rescate compradas en este nivel.</summary>
        public int RescuesLevel { get; set; }

        /// <summary><c>undo_free</c>: Mareas gratis que quedan en este nivel.</summary>
        public int UndoFree { get; set; }

        /// <summary><c>revealed_cuias</c>: índices de las cuias reveladas.</summary>
        public List<int> RevealedCuias { get; } = new List<int>();

        /// <summary><c>history</c>: movimientos comprimidos.</summary>
        public string History { get; set; } = "";

        /// <summary><c>stuck</c>: el nivel quedó en Aguas estancadas.</summary>
        public bool Stuck { get; set; }
    }

    /// <summary><c>entitlements</c> (08 §4.3): Sin anuncios, Mochila de inicio y skins compradas.</summary>
    public sealed class EntitlementsData
    {
        /// <summary><c>no_ads</c>: Sin anuncios comprado (<c>af_noads</c>, 05 §4.1).</summary>
        public bool NoAds { get; set; }

        /// <summary><c>starter_bought</c>: Mochila de inicio comprada (<c>af_starter</c>: 1 por cuenta, 05 §4.1).</summary>
        public bool StarterBought { get; set; }

        /// <summary>
        /// <c>skins</c>: skins de cuia del jugador. Hoy solo las compradas, con el SKU del pack (<c>af_skin_m02</c> …
        /// <c>af_skin_m04</c>, 05 §4.1), porque el GDD aún no fija los IDs de skin de los mundos 2-4 (08 §4.3 solo nombra
        /// la base, <c>cuia_vidrio</c>, que es gratis).
        /// </summary>
        public List<string> Skins { get; } = new List<string>();
    }

    /// <summary>
    /// Valor de <c>settings.patterns</c>: el ajuste «Patrones de fruta» (02 §12; glosario §10 <c>patterns_toggle</c>). Vive
    /// aquí porque se persiste en el perfil. Lo resuelve <c>AF.UI</c> (<c>Auto</c> = <c>Palette.PatternsOnFor(level)</c>) y a
    /// <c>AF.Presentation</c> solo le llega un bool: Presentation no referencia AF.Save (08 §2.2).
    /// </summary>
    public enum PatternsSetting
    {
        /// <summary><c>auto</c> (default): los patrones salen según el nivel, como hasta ahora.</summary>
        Auto = 0,

        /// <summary><c>on</c>: siempre.</summary>
        On = 1,

        /// <summary><c>off</c>: nunca.</summary>
        Off = 2,
    }

    /// <summary>
    /// <c>settings</c> (08 §4.3): solo los ajustes que ya existen en el juego (<c>sound</c>, <c>music</c> y <c>haptics</c> desde
    /// AU1); <c>push</c> llega con su sistema. Un valor que falta o no es válido en el archivo toma el default (los ajustes nunca
    /// hacen corrupto un perfil).
    /// </summary>
    public sealed class SettingsData
    {
        /// <summary>Valor de <see cref="Language"/> que sigue al teléfono (D-018 Q7-A; 08 §4.3): el default.</summary>
        public const string LanguageAuto = "auto";

        // Tanda 1 de D-018 (Q1-A) y el idioma falso en-XA, solo de desarrollo (glosario §11, fila Localización). El perfil
        // solo admite estos códigos; si un idioma guardado se puede usar (UI completa, build de desarrollo) lo decide quien
        // resuelve el idioma, no el perfil. La tanda 2 añade aquí sus códigos.
        private static readonly string[] LanguageCodes = { "es-419", "pt-BR", "en", "en-XA" };

        private string _language = LanguageAuto;
        private PatternsSetting _patterns = PatternsSetting.Auto;

        /// <summary>Códigos de idioma que admite <see cref="Language"/> además de <see cref="LanguageAuto"/>, con su forma canónica.</summary>
        public static IReadOnlyList<string> Languages { get; } = Array.AsReadOnly(LanguageCodes);

        /// <summary><c>patterns</c>: «Patrones de fruta» (<c>auto</c>, <c>on</c> u <c>off</c>; default <c>auto</c>).</summary>
        /// <exception cref="ArgumentOutOfRangeException">Al asignar un valor que no es del enum.</exception>
        public PatternsSetting Patterns
        {
            get => _patterns;
            set
            {
                if (value != PatternsSetting.Auto && value != PatternsSetting.On && value != PatternsSetting.Off)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Ajuste de patrones desconocido.");
                }

                _patterns = value;
            }
        }

        /// <summary>
        /// <c>language</c> (D-018 Q7-A; 08 §4.3): <see cref="LanguageAuto"/> (seguir al teléfono; default) o uno de
        /// <see cref="Languages"/>, en su forma canónica (se acepta sin distinguir mayúsculas: <c>ES-419</c> → <c>es-419</c>).
        /// </summary>
        /// <exception cref="ArgumentException">Al asignar null o un código que no está en <see cref="Languages"/>.</exception>
        public string Language
        {
            get => _language;
            set => _language = NormalizeLanguage(value)
                ?? throw new ArgumentException("Idioma no admitido en settings.language: '" + value + "'.", nameof(value));
        }

        /// <summary>
        /// <c>sound</c> (08 §4.3; 07 §9.4; tarea AU1): el ajuste Sonido (los efectos). Default true, el de
        /// <c>audio_sfx_default_on</c> (08 §9).
        /// </summary>
        public bool Sound { get; set; } = true;

        /// <summary><c>music</c> (08 §4.3; 07 §9.4): el ajuste Música. Default true, el de <c>audio_music_default_on</c> (08 §9).</summary>
        public bool Music { get; set; } = true;

        /// <summary>
        /// <c>haptics</c> (08 §4.3; 02 §4): el ajuste Vibración. Default true, el de <c>ui_haptics_default_on</c> (08 §9).
        /// </summary>
        public bool Haptics { get; set; } = true;

        /// <summary><c>sloth_messages</c> (D-016 punto 4): el ajuste «Mensajes de TiTino»; apagado, solo gestos. Default true.</summary>
        public bool SlothMessages { get; set; } = true;

        /// <summary><c>idle_hint</c> [P]: la pista por inactividad (04 §2.3), que se puede apagar en Ajustes. Default true.</summary>
        public bool IdleHint { get; set; } = true;

        /// <summary>
        /// Forma canónica de <paramref name="value"/> si vale para <see cref="Language"/> (<see cref="LanguageAuto"/> o uno de
        /// <see cref="Languages"/>, sin distinguir mayúsculas), o null si no vale.
        /// </summary>
        public static string? NormalizeLanguage(string? value)
        {
            if (value == null)
            {
                return null;
            }

            if (string.Equals(value, LanguageAuto, StringComparison.OrdinalIgnoreCase))
            {
                return LanguageAuto;
            }

            for (int i = 0; i < LanguageCodes.Length; i++)
            {
                if (string.Equals(value, LanguageCodes[i], StringComparison.OrdinalIgnoreCase))
                {
                    return LanguageCodes[i];
                }
            }

            return null;
        }

        /// <summary>Texto de <paramref name="value"/> en el JSON: <c>auto</c>, <c>on</c> u <c>off</c>.</summary>
        internal static string PatternsText(PatternsSetting value)
        {
            return value == PatternsSetting.On ? "on" : value == PatternsSetting.Off ? "off" : "auto";
        }

        /// <summary>Lee <c>auto</c>, <c>on</c> u <c>off</c>; false con cualquier otro texto.</summary>
        internal static bool TryParsePatterns(string text, out PatternsSetting value)
        {
            switch (text)
            {
                case "auto":
                    value = PatternsSetting.Auto;
                    return true;
                case "on":
                    value = PatternsSetting.On;
                    return true;
                case "off":
                    value = PatternsSetting.Off;
                    return true;
                default:
                    value = PatternsSetting.Auto;
                    return false;
            }
        }
    }

    /// <summary>
    /// <c>sloth</c> [P] (D-016 punto 4; propuesta de TiTino d.7 y d.10): la última frase vista de cada momento, para no
    /// repetirla nunca seguida, y las fechas locales del último saludo y de la última apertura, para los topes de 1 saludo
    /// al día y el regreso tras ≥ 3 días. Las reglas las aplica <c>AF.GameFlow</c>; aquí solo se guardan. Un valor que
    /// falta o no es válido en el archivo toma el default.
    /// </summary>
    public sealed class SlothData
    {
        /// <summary>Número de momentos con banco de frases (glosario §10: <c>sloth_line_&lt;momento&gt;_&lt;nn&gt;</c>).</summary>
        public const int MomentCount = 8;

        /// <summary>Valor de <see cref="LastLine"/> cuando aún no se vio ninguna frase de ese momento.</summary>
        public const int NoLine = -1;

        private static readonly string[] MomentKeys = { "win", "hard", "fail", "restart", "streak", "return", "debut", "milestone" };

        private string? _lastGreetingDate;
        private string? _lastSeenDate;

        /// <summary>
        /// Momentos en el orden de los índices de <see cref="LastLine"/> (el de D-016 punto 4 y el glosario §10). El orden
        /// se guarda en el perfil: cambiarlo exige migrar.
        /// </summary>
        public static IReadOnlyList<string> Moments { get; } = Array.AsReadOnly(MomentKeys);

        /// <summary>
        /// <c>last_line</c>: por cada momento de <see cref="Moments"/>, el índice de la última frase vista de su banco, o
        /// <see cref="NoLine"/>. Siempre tiene <see cref="MomentCount"/> elementos.
        /// </summary>
        public int[] LastLine { get; } = new int[] { NoLine, NoLine, NoLine, NoLine, NoLine, NoLine, NoLine, NoLine };

        /// <summary><c>last_greeting_date</c>: fecha local (<c>yyyy-MM-dd</c>) del último saludo del inicio, o null.</summary>
        /// <exception cref="ArgumentException">Al asignar un texto que no es una fecha <c>yyyy-MM-dd</c>.</exception>
        public string? LastGreetingDate
        {
            get => _lastGreetingDate;
            set => _lastGreetingDate = RequireDateKey(value);
        }

        /// <summary><c>last_seen_date</c>: fecha local (<c>yyyy-MM-dd</c>) de la última apertura del juego, o null.</summary>
        /// <exception cref="ArgumentException">Al asignar un texto que no es una fecha <c>yyyy-MM-dd</c>.</exception>
        public string? LastSeenDate
        {
            get => _lastSeenDate;
            set => _lastSeenDate = RequireDateKey(value);
        }

        /// <summary>
        /// true si <paramref name="text"/> es una fecha que existe con el formato de <c>ClockExtensions.LocalDateKey</c>:
        /// <c>yyyy-MM-dd</c>, calendario gregoriano.
        /// </summary>
        public static bool IsDateKey(string? text)
        {
            if (text == null || text.Length != 10 || text[4] != '-' || text[7] != '-')
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (i != 4 && i != 7 && (text[i] < '0' || text[i] > '9'))
                {
                    return false;
                }
            }

            return DateTime.TryParseExact(text, "yyyy'-'MM'-'dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        private static string? RequireDateKey(string? value)
        {
            if (value != null && !IsDateKey(value))
            {
                throw new ArgumentException("Se espera una fecha yyyy-MM-dd o null: '" + value + "'.", nameof(value));
            }

            return value;
        }
    }

    /// <summary>
    /// <c>ads</c> [P]: lo que la cadencia de intersticiales recuerda entre sesiones (05 §6.3, <c>player.last_iap</c> y
    /// <c>player.levels_since_interstitial</c>). Los contadores del día van en <c>daily</c> (04 §10.1) y lo propio de la
    /// sesión, en memoria (<c>AF.Ads.AdSession</c>).
    /// </summary>
    public sealed class AdsData
    {
        /// <summary>
        /// <c>last_iap</c>: última compra con dinero (UTC), o null. Corta los intersticiales
        /// <c>ads_interstitial_suppress_after_iap_h</c> horas (24), también tras <c>af_infinite_canoes_1h</c> (D-008).
        /// </summary>
        public DateTime? LastIap { get; set; }

        /// <summary><c>levels_since_interstitial</c>: niveles completados desde el último intersticial.</summary>
        public int LevelsSinceInterstitial { get; set; }
    }

    /// <summary>
    /// <c>daily_challenge</c> (08 §4.3; nota del Reto §10): <c>last_date</c>, <c>result</c> y <c>streak_days</c> de 08 §4.3;
    /// <c>last_claimed_date</c>, <c>pending</c> y <c>stamps</c> (R-Q5); <c>offline_claims</c> [P] e <c>in_progress</c> [P]
    /// (R-Q4 y R-Q16). Solo datos: el significado lo da <c>AF.LiveOps.DailyChallengeRecord</c>.
    /// </summary>
    public sealed class DailyChallengeData
    {
        /// <summary><c>last_date</c>: fecha local (<c>yyyy-MM-dd</c>) del último resultado, o null.</summary>
        public string? LastDate { get; set; }

        /// <summary><c>result</c>: <c>win</c>, <c>fail</c> o <c>abandon</c> (08 §8.2), o null si aún no hay resultado.</summary>
        public string? Result { get; set; }

        /// <summary><c>streak_days</c>: días seguidos ganados y cobrados.</summary>
        public int StreakDays { get; set; }

        /// <summary><c>last_claimed_date</c>: última fecha cobrada (<c>yyyy-MM-dd</c>), o null.</summary>
        public string? LastClaimedDate { get; set; }

        /// <summary><c>pending</c>: cobros pendientes de validar, <c>[{date, streak}]</c>.</summary>
        public List<DailyClaimData> Pending { get; } = new List<DailyClaimData>();

        /// <summary><c>offline_claims</c> [P]: cobros pagados sin red, <c>[{date, streak}]</c> (los acota AF.LiveOps).</summary>
        public List<DailyClaimData> OfflineClaims { get; } = new List<DailyClaimData>();

        /// <summary><c>stamps.month</c>: mes de los sellos (<c>yyyy-MM</c>), o null.</summary>
        public string? StampsMonth { get; set; }

        /// <summary><c>stamps.days</c>: días sellados de <see cref="StampsMonth"/> (1-31).</summary>
        public List<int> StampDays { get; } = new List<int>();

        /// <summary><c>in_progress</c> [P]: partida del Reto en curso, o null.</summary>
        public DailyInProgressData? InProgress { get; set; }
    }

    /// <summary>Elemento de <c>daily_challenge.pending</c> y <c>offline_claims</c>: <c>{date, streak}</c>.</summary>
    public sealed class DailyClaimData
    {
        /// <summary><c>date</c>: fecha cobrada (<c>yyyy-MM-dd</c>).</summary>
        public string Date { get; set; } = "";

        /// <summary><c>streak</c>: <c>streak_days</c> tras ese cobro.</summary>
        public int Streak { get; set; }
    }

    /// <summary><c>daily_challenge.in_progress</c> [P]: <c>{date, band, id, level_state}</c> (<c>level_state</c> como en 04 §10.1).</summary>
    public sealed class DailyInProgressData
    {
        /// <summary><c>date</c>: fecha local de inicio (<c>yyyy-MM-dd</c>).</summary>
        public string Date { get; set; } = "";

        /// <summary><c>band</c>: banda 1-3, congelada al empezar.</summary>
        public int Band { get; set; }

        /// <summary><c>id</c>: tablero <c>d{b}_n{nnnn}</c> (03 §16).</summary>
        public string Id { get; set; } = "";

        /// <summary><c>level_state</c>: estado de la partida, con el formato de 04 §10.1.</summary>
        public LevelStateData LevelState { get; set; } = new LevelStateData();
    }

    /// <summary><c>stats</c> (08 §4.3).</summary>
    public sealed class StatsData
    {
        /// <summary><c>levels_won</c>.</summary>
        public int LevelsWon { get; set; }

        /// <summary><c>fails_hard</c>.</summary>
        public int FailsHard { get; set; }

        /// <summary><c>rescues</c>.</summary>
        public int Rescues { get; set; }

        /// <summary><c>rv_watched</c>.</summary>
        public int RvWatched { get; set; }

        /// <summary><c>sessions</c>.</summary>
        public int Sessions { get; set; }
    }

    /// <summary>
    /// Un avistamiento de <see cref="FaunaData.Sighted"/>: la especie (<c>species_id</c>, el id del catálogo de fauna, p. ej.
    /// <c>saimiri_collinsi</c>; glosario §11) y el nivel en que su tarjeta salió a la vista por primera vez (<c>level</c>, ≥ 1).
    /// </summary>
    public sealed class FaunaSighting
    {
        /// <summary>Crea el avistamiento.</summary>
        /// <exception cref="ArgumentException"><paramref name="speciesId"/> no es un id de especie (<see cref="FaunaData.IsSpeciesId"/>).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> &lt; 1.</exception>
        public FaunaSighting(string speciesId, int level)
        {
            if (!FaunaData.IsSpeciesId(speciesId))
            {
                throw new ArgumentException("Id de especie no válido: '" + speciesId + "'.", nameof(speciesId));
            }

            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "El nivel de un avistamiento es >= 1.");
            }

            SpeciesId = speciesId;
            Level = level;
        }

        /// <summary><c>species_id</c>: id de la especie en el catálogo de fauna.</summary>
        public string SpeciesId { get; }

        /// <summary><c>level</c>: nivel en que su tarjeta salió a la vista por primera vez.</summary>
        public int Level { get; }
    }

    /// <summary>
    /// <c>fauna</c> [P] (esquema 2; Q11 del guion de la intro, D-029; tarea DC15). <c>sighted</c>: una entrada por especie cuya
    /// tarjeta «¡Miquito a la vista!» salió a la vista, con su primer nivel, en orden ordinal por <c>species_id</c>
    /// (<see cref="StringComparer.Ordinal"/>: determinista, no depende del orden en que se vieron). Apuntar una especie ya vista
    /// no cambia nada (<see cref="RecordSighting"/> devuelve false). Se apunta al mostrar la tarjeta, no al llegar la tropa: sin
    /// tarjeta no hay avistamiento. Una entrada que no es válida en el archivo se descarta (un avistamiento nunca hace corrupto
    /// un perfil).
    /// </summary>
    public sealed class FaunaData
    {
        /// <summary>Largo máximo de un id de especie (los del catálogo no pasan de 30 caracteres).</summary>
        public const int MaxSpeciesIdLength = 64;

        private readonly List<FaunaSighting> _sighted = new List<FaunaSighting>();

        /// <summary><c>sighted</c>: las especies vistas, en orden ordinal por id.</summary>
        public IReadOnlyList<FaunaSighting> Sighted => _sighted;

        /// <summary>
        /// true si <paramref name="text"/> vale como id de especie: de 1 a <see cref="MaxSpeciesIdLength"/> caracteres
        /// <c>a-z</c>, <c>0-9</c> o <c>_</c> (el nombre científico en minúsculas con guiones bajos; glosario §11).
        /// </summary>
        public static bool IsSpeciesId(string? text)
        {
            if (text == null || text.Length == 0 || text.Length > MaxSpeciesIdLength)
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>true si la tarjeta de <paramref name="speciesId"/> ya salió a la vista.</summary>
        public bool HasSighted(string speciesId)
        {
            return IndexOf(speciesId ?? throw new ArgumentNullException(nameof(speciesId))) >= 0;
        }

        /// <summary>El avistamiento de <paramref name="speciesId"/>, o null si aún no se vio.</summary>
        public FaunaSighting? Find(string speciesId)
        {
            int i = IndexOf(speciesId ?? throw new ArgumentNullException(nameof(speciesId)));
            return i >= 0 ? _sighted[i] : null;
        }

        /// <summary>
        /// Apunta que la tarjeta de <paramref name="speciesId"/> salió a la vista en el nivel <paramref name="level"/>. Devuelve
        /// true la primera vez (la lista crece, en su sitio ordinal) y false si la especie ya estaba (nada cambia: se conserva su
        /// primer nivel).
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="speciesId"/> no es un id de especie.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> &lt; 1.</exception>
        public bool RecordSighting(string speciesId, int level)
        {
            var sighting = new FaunaSighting(speciesId, level);
            int i = IndexOf(speciesId);
            if (i >= 0)
            {
                return false;
            }

            _sighted.Insert(~i, sighting);
            return true;
        }

        /// <summary>
        /// Fusión de 08 §11 (<c>ResolveConflict</c>, como <c>merged.album = union(...)</c>): sobre la fauna del perfil ganador,
        /// la unión de las especies de los dos, con el nivel más bajo de cada una.
        /// </summary>
        /// <param name="other">La fauna del otro perfil (el perdedor).</param>
        public void MergeSightings(FaunaData other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            for (int k = 0; k < other._sighted.Count; k++)
            {
                Merge(other._sighted[k]);
            }
        }

        /// <summary>Lectura del archivo: una entrada en su sitio; si la especie se repite, gana el nivel más bajo.</summary>
        internal void Merge(FaunaSighting sighting)
        {
            int i = IndexOf(sighting.SpeciesId);
            if (i < 0)
            {
                _sighted.Insert(~i, sighting);
            }
            else if (sighting.Level < _sighted[i].Level)
            {
                _sighted[i] = sighting;
            }
        }

        // Búsqueda binaria ordinal: el índice, o el complemento del sitio donde iría (como List<T>.BinarySearch).
        private int IndexOf(string speciesId)
        {
            int lo = 0;
            int hi = _sighted.Count - 1;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                int cmp = string.CompareOrdinal(_sighted[mid].SpeciesId, speciesId);
                if (cmp == 0)
                {
                    return mid;
                }

                if (cmp < 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return ~lo;
        }
    }
}
