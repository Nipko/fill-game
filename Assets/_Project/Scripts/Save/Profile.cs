#nullable enable
using System;
using System.Collections.Generic;
using AF.Platform;

namespace AF.Save
{
    /// <summary>
    /// Perfil del jugador guardado en <c>profile.json</c> (GDD 08 §4.3, que extiende el inventario de 04 §10.1), con
    /// <c>profile_schema</c> = <see cref="CurrentSchema"/>. Contiene el subconjunto que usa la pista B (D-008; tarea B1-1):
    /// <c>created_at</c>, <c>economy_ts</c>, <c>wallet</c> (semillas y canoas), <c>stock</c>, <c>daily</c>,
    /// <c>progress</c>, <c>level_state</c>, <c>entitlements.no_ads</c> y <c>stats</c>, con los nombres exactos del GDD.
    /// El resto de 08 §4.3 (consentimiento, uid, calendario, álbum…) llega con su sistema.
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
        /// <summary>Versión del esquema que escribe esta build (08 §4.4: solo avanza).</summary>
        public const int CurrentSchema = 1;

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

        /// <summary><c>stats</c>: contadores agregados (nunca por nivel: eso va a analítica, 08 §4.3).</summary>
        public StatsData Stats { get; } = new StatsData();

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
    /// intersticiales, 05 §6.3). Al cambiar <c>date_local</c> los pone a cero quien aplica la regla de cada uno.
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

        /// <summary><c>shuffles_today</c>: Pororocas usadas hoy.</summary>
        public int ShufflesToday { get; set; }

        /// <summary><c>shield_available</c>: Hoja escudo disponible hoy.</summary>
        public bool ShieldAvailable { get; set; }
    }

    /// <summary><c>progress</c> (08 §4.3; 04 §10.1).</summary>
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

    /// <summary><c>entitlements</c> (08 §4.3): por ahora solo Sin anuncios.</summary>
    public sealed class EntitlementsData
    {
        /// <summary><c>no_ads</c>: Sin anuncios comprado (<c>af_noads</c>, 05 §4.1).</summary>
        public bool NoAds { get; set; }
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
}
