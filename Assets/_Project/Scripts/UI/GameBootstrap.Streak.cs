#nullable enable
using System;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Platform;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Arranque de la Crecida, la Hoja escudo y la Canasta (B1a, A7; nota <c>docs/dev/crecida-canasta.md</c>, E32 y E33):
    /// <see cref="SetUpStreak"/> crea el <see cref="StreakService"/> con el perfil, el lote de guardado, la cartera de campaña y el
    /// reloj que ya creó <c>Start</c>, y lo inyecta en el flujo de campaña con <see cref="LevelFlow.UseStreak"/>. Parcial nuevo:
    /// <c>GameBootstrap.cs</c> no se toca.
    /// <list type="bullet">
    /// <item><b>S3-bootstrap</b> (lo aplica el integrador, después de S1-profile, S4-wallet, S2-flow y S5-flow-profile): llama a
    /// <see cref="SetUpStreak"/> en <c>Start</c>, justo después de <c>SetUpDaily();</c> (de su configuración sale
    /// <c>live_unlock_level_daily</c>), y añade <c>GameBootstrap.StreakProfile.cs</c>, que implementa
    /// <see cref="CreateProfileStreakStore"/> con <c>ProfileStreakStore</c> (S1) sobre el mismo perfil y la vía síncrona del
    /// lote (<see cref="ProfileSaveBatch.Durable"/>), la misma que la cartera: el apunte de un hito y su pago salen en la misma
    /// escritura del perfil.</item>
    /// <item>Sin S1 (esta rama), no hay almacén en el perfil: en desarrollo se usa uno en memoria (se pierde al cerrar la app; la
    /// garantía de «un hito una vez al día» de <see cref="IStreakStore"/> solo vale dentro de la sesión) y en release no hay
    /// Crecida (el flujo sin servicio no hace nada).</item>
    /// </list>
    /// Las cifras son las de 08 §9 (<see cref="StreakConfig.Defaults"/> y <see cref="BasketConfig.Defaults"/>): aún no hay
    /// <c>AF.RemoteConfig</c> que las rellene por <c>[RemoteConfigKey]</c>. <c>live_unlock_level_daily</c> tiene una sola fuente,
    /// <see cref="DailyChallengeConfig"/>: la del Reto si ya existe (<see cref="DailyService"/>), o su default.
    /// <para>
    /// Sondas (solo en desarrollo, <see cref="DevProbe.Enabled"/>): <c>streak_state</c> con <c>cause = start</c> al crear el
    /// servicio y, con <see cref="StreakProbeRunner"/>, las líneas de cada <see cref="LevelFlow.LastStreakResult"/> nuevo
    /// (victoria, fallo duro o Salto del Boto) y <c>streak_state</c> con <c>cause = day</c> al cambiar el día local
    /// (<see cref="TickStreakProbe"/>). La sonda no toca <c>GameScreen</c>: lee el resultado en el primer fotograma después de
    /// producirse. En la victoria, las líneas salen después de <c>AF_STATE win</c> (de <c>ResolveWin</c>) y <b>antes</b> de
    /// <c>victory</c>, que la vista escribe tras el drenado y la celebración (hasta unos 4 s después). En el fallo duro salen
    /// después de <c>fail_hard</c> y de <c>level_failed</c> (o del modal que toque).
    /// </para>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        private StreakService? _streakService;
        private StreakProbeRunner? _streakRunner;
        private StreakServiceResult? _streakProbeSeen;
        private IClock? _streakClock;
        private DateTime _streakProbeDate;

        /// <summary>Almacén de la Crecida para los tests (gana al del perfil); <see cref="ResetStreakOverrides"/> lo limpia.</summary>
        internal static IStreakStore? StreakStoreOverride { get; set; }

        /// <summary>Cifras de la Crecida para los tests (por defecto, <see cref="StreakConfig.Defaults"/>).</summary>
        internal static StreakConfig? StreakConfigOverride { get; set; }

        /// <summary>Cifras de la Canasta para los tests (por defecto, <see cref="BasketConfig.Defaults"/>).</summary>
        internal static BasketConfig? BasketConfigOverride { get; set; }

        /// <summary>
        /// Sonda de la Crecida para los tests: null (por defecto) sigue a <see cref="DevProbe.Enabled"/>; false la apaga (ni
        /// <c>streak_state</c> de partida, ni <see cref="StreakProbeRunner"/>, ni líneas en <see cref="TickStreakProbe"/>), para
        /// probar en el Editor el camino de release. <see cref="ResetStreakOverrides"/> lo limpia.
        /// </summary>
        internal static bool? StreakProbeOverride { get; set; }

        private static bool StreakProbeEnabled => (StreakProbeOverride ?? true) && DevProbe.Enabled;

        /// <summary>Servicio de la Crecida (null antes de <see cref="SetUpStreak"/>, o en release sin almacén en el perfil).</summary>
        public StreakService? StreakService => _streakService;

        /// <summary>Componente que avanza las sondas de la Crecida (null fuera de desarrollo).</summary>
        public StreakProbeRunner? StreakRunner => _streakRunner;

        /// <summary>Limpia los ganchos de test de la Crecida.</summary>
        internal static void ResetStreakOverrides()
        {
            StreakStoreOverride = null;
            StreakConfigOverride = null;
            BasketConfigOverride = null;
            StreakProbeOverride = null;
        }

        /// <summary>
        /// Crea el <see cref="StreakService"/> (E32) y lo inyecta en el flujo de campaña. Devuelve false, sin crear nada, si aún no
        /// hay arranque, o en release sin almacén en el perfil (sin S1). Una segunda llamada no hace nada y devuelve true. Con la
        /// sonda, escribe el <c>streak_state</c> de partida y añade <see cref="StreakProbeRunner"/>.
        /// </summary>
        public bool SetUpStreak()
        {
            if (_streakService != null)
            {
                return true;
            }

            Profile? profile = _profile;
            ProfileSaveBatch? batch = _saveBatch;
            Wallet? wallet = _wallet;
            LevelFlow? flow = _flow;
            if (profile == null || batch == null || wallet == null || flow == null || _progress == null)
            {
                Debug.LogError(LogPrefix + "SetUpStreak antes del arranque: la Crecida no se crea.");
                return false;
            }

            IClock clock = LaunchOptions.ClockOverride ?? SystemClock.Instance;
            int basketUnlock = _dailyService?.Config.LiveUnlockLevelDaily ?? DailyChallengeConfig.Defaults.LiveUnlockLevelDaily;
            StreakService? service = CreateStreakService(profile, batch, wallet, clock, basketUnlock, Debug.isDebugBuild, out bool inMemory);
            if (service == null)
            {
                return false; // solo en release sin S1: sin Crecida ni Canasta, sin aviso (como el Reto sin registro)
            }

            if (inMemory)
            {
                Debug.LogWarning(LogPrefix + "Crecida con el estado en memoria (sin ProfileStreakStore): se pierde al cerrar la app.");
            }

            _streakService = service;
            _streakClock = clock;
            flow.UseStreak(service);
            if (_lobby != null)
            {
                _lobby.StreakPeek = level => level >= 1 ? service.Peek(level, true) : (StreakSnapshot?)null; // widget del inicio (B1b)
            }
            if (StreakProbeEnabled)
            {
                _streakProbeSeen = null;
                _streakProbeDate = clock.LocalDate();
                StreakSnapshot start = service.Peek(StreakProbeLevel(), true);
                DevProbe.StreakState(StreakProbeFormat.CauseStart, StreakProbeLevel(), in start);
                StreakProbeRunner runner = gameObject.GetComponent<StreakProbeRunner>();
                if (runner == null)
                {
                    runner = gameObject.AddComponent<StreakProbeRunner>();
                }

                _streakRunner = runner;
                _streakRunner.Bind(this);
            }

            return true;
        }

        /// <summary>
        /// Sondas de la Crecida (lo llama <see cref="StreakProbeRunner"/> cada fotograma; solo en desarrollo): las líneas del
        /// <see cref="LevelFlow.LastStreakResult"/> del flujo de campaña si es nuevo (con <c>streak_state</c> si el estado cambió) y
        /// <c>streak_state</c> con <c>cause = day</c> si cambió el día local. El estado se consulta para el nivel siguiente de la
        /// campaña (<c>max_level</c> + 1). Sin asignaciones mientras nada cambia. Devuelve cuántas líneas escribió.
        /// </summary>
        public int TickStreakProbe()
        {
            StreakService? service = _streakService;
            LevelFlow? flow = _flow;
            if (!StreakProbeEnabled || service == null || flow == null || _streakClock == null)
            {
                return 0;
            }

            int written = 0;
            StreakServiceResult? last = flow.LastStreakResult;
            if (last != null && !ReferenceEquals(last, _streakProbeSeen))
            {
                _streakProbeSeen = last;
                int level = StreakProbeLevel();
                StreakSnapshot? after = last.Has(StreakServiceEvents.StateChanged) ? service.Peek(level, true) : (StreakSnapshot?)null;
                written += DevProbe.StreakResult(last, after, level);
            }

            DateTime date = _streakClock.LocalDate();
            if (date != _streakProbeDate)
            {
                _streakProbeDate = date;
                int level = StreakProbeLevel();
                StreakSnapshot state = service.Peek(level, true);
                if (DevProbe.StreakState(StreakProbeFormat.CauseDay, level, in state) != null)
                {
                    written++;
                }
            }

            return written;
        }

        /// <summary>
        /// El <see cref="StreakService"/> de E32 sobre <paramref name="profile"/>: el almacén de los tests
        /// (<see cref="StreakStoreOverride"/>), si no el del perfil (<see cref="CreateProfileStreakStore"/>, S3 con S1) y, sin
        /// ninguno, uno en memoria solo si <paramref name="debugBuild"/> (<paramref name="inMemory"/> = true); en release sin almacén,
        /// null. La cartera es la de campaña, con el mismo perfil y lote.
        /// </summary>
        internal static StreakService? CreateStreakService(
            Profile profile, ProfileSaveBatch batch, Wallet wallet, IClock clock, int basketUnlockLevel, bool debugBuild, out bool inMemory)
        {
            inMemory = false;
            IStreakStore? store = StreakStoreOverride;
            if (store == null)
            {
                CreateProfileStreakStore(profile, batch, clock, ref store);
            }

            if (store == null)
            {
                if (!debugBuild)
                {
                    return null;
                }

                store = new InMemoryStreakStore(clock);
                inMemory = true;
            }

            return new StreakService(
                store, wallet, clock, basketUnlockLevel, StreakConfigOverride ?? ActiveRcConfigs?.Streak, BasketConfigOverride ?? ActiveRcConfigs?.Basket);
        }

        /// <summary>
        /// Gancho del parche S3-bootstrap: <c>GameBootstrap.StreakProfile.cs</c> lo implementa con <c>ProfileStreakStore</c> (S1)
        /// sobre <paramref name="profile"/> y <see cref="ProfileSaveBatch.Durable"/>. Sin el parche no hay implementación y
        /// <paramref name="store"/> queda como estaba (null).
        /// </summary>
        static partial void CreateProfileStreakStore(Profile profile, ProfileSaveBatch batch, IClock clock, ref IStreakStore? store);

        private int StreakProbeLevel()
        {
            return (_progress?.MaxLevel ?? 0) + 1;
        }
    }
}
