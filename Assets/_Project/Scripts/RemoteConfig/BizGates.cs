#nullable enable
using System;
using System.Globalization;

namespace AF.RemoteConfig
{
    /// <summary>Sistemas con kill switch propio, <c>biz_feature_&lt;sistema&gt;</c> (08 §9; 09 §4, riesgos R2 y R13).</summary>
    public enum BizFeature
    {
        /// <summary><c>biz_feature_daily_challenge</c>: Reto del río.</summary>
        DailyChallenge = 0,

        /// <summary><c>biz_feature_login_calendar</c>: Bitácora del río.</summary>
        LoginCalendar = 1,

        /// <summary><c>biz_feature_win_streak</c>: Crecida.</summary>
        WinStreak = 2,

        /// <summary><c>biz_feature_lives</c>: Canoas.</summary>
        Lives = 3,

        /// <summary><c>biz_feature_rescue</c>: Cuia de rescate.</summary>
        Rescue = 4,

        /// <summary><c>biz_feature_murky</c>: Aguas turbias.</summary>
        Murky = 5,

        /// <summary><c>biz_feature_piggy</c>: Cántaro.</summary>
        Piggy = 6,

        /// <summary><c>biz_feature_shop</c>: Mercado.</summary>
        Shop = 7,
    }

    /// <summary>
    /// Compuertas de negocio de 08 §9 leídas de unos valores congelados: <c>biz_maintenance_mode</c> / <c>_message</c>
    /// («Bloquea anuncios e IAP durante incidentes; el juego offline sigue»), <c>biz_min_app_version</c> («Por debajo,
    /// pantalla "Actualiza Tintalú" (compras y liga bloqueadas)»), los kill switches <c>biz_iap_enabled</c>,
    /// <c>biz_ads_enabled</c>, <c>biz_interstitials_enabled</c>, <c>biz_rewarded_enabled</c> y los
    /// <c>biz_feature_&lt;sistema&gt;</c>. Sin estado: cada propiedad lee el <see cref="RcSnapshot"/> de la construcción.
    /// </summary>
    public sealed class BizGates
    {
        private static readonly string[] FeatureKeys =
        {
            "biz_feature_daily_challenge", "biz_feature_login_calendar", "biz_feature_win_streak", "biz_feature_lives",
            "biz_feature_rescue", "biz_feature_murky", "biz_feature_piggy", "biz_feature_shop",
        };

        private readonly RcSnapshot _rc;

        /// <summary>Crea las compuertas con los valores <paramref name="rc"/> y la versión de la app <paramref name="appVersion"/>.</summary>
        public BizGates(RcSnapshot rc, string appVersion)
        {
            _rc = rc ?? throw new ArgumentNullException(nameof(rc));
            AppVersion = appVersion ?? throw new ArgumentNullException(nameof(appVersion));
        }

        /// <summary>Número de sistemas con kill switch.</summary>
        public static int FeatureCount => FeatureKeys.Length;

        /// <summary>Versión de la app (<c>Application.version</c>).</summary>
        public string AppVersion { get; }

        /// <summary><c>biz_maintenance_mode</c>.</summary>
        public bool MaintenanceMode => _rc.Get<bool>("biz_maintenance_mode");

        /// <summary>
        /// <c>biz_maintenance_message</c>, tal cual. Remote Config lleva claves, nunca texto (<c>.claude/rules/localizacion.md</c>):
        /// la vista lo trata como clave de <c>strings.csv</c> [P] (nota, Q-B7-8); vacío = el aviso genérico.
        /// </summary>
        public string MaintenanceMessage => _rc.Get<string>("biz_maintenance_message");

        /// <summary><c>biz_min_app_version</c>.</summary>
        public string MinAppVersion => _rc.Get<string>("biz_min_app_version");

        /// <summary>La versión de la app es menor que <c>biz_min_app_version</c>: pantalla «Actualiza Tintalú».</summary>
        public bool UpdateRequired => CompareVersions(AppVersion, MinAppVersion) < 0;

        /// <summary>
        /// Hay compras: <c>biz_iap_enabled</c>, sin mantenimiento y con la versión mínima. La pantalla del Mercado tiene además su
        /// propio kill switch, <c>biz_feature_shop</c> (<see cref="Feature"/>).
        /// </summary>
        public bool IapEnabled => _rc.Get<bool>("biz_iap_enabled") && !MaintenanceMode && !UpdateRequired;

        /// <summary>Hay anuncios: <c>biz_ads_enabled</c> y sin mantenimiento.</summary>
        public bool AdsEnabled => _rc.Get<bool>("biz_ads_enabled") && !MaintenanceMode;

        /// <summary>Hay intersticiales: <see cref="AdsEnabled"/> y <c>biz_interstitials_enabled</c>.</summary>
        public bool InterstitialsEnabled => AdsEnabled && _rc.Get<bool>("biz_interstitials_enabled");

        /// <summary>Hay recompensados: <see cref="AdsEnabled"/> y <c>biz_rewarded_enabled</c>.</summary>
        public bool RewardedEnabled => AdsEnabled && _rc.Get<bool>("biz_rewarded_enabled");

        /// <summary>El kill switch de un sistema (true = encendido, el default).</summary>
        public bool Feature(BizFeature feature) => _rc.Get<bool>(KeyOf(feature));

        /// <summary>La clave de <paramref name="feature"/> en 08 §9.</summary>
        public static string KeyOf(BizFeature feature)
        {
            int i = (int)feature;
            if (i < 0 || i >= FeatureKeys.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(feature), feature, null);
            }

            return FeatureKeys[i];
        }

        /// <summary>
        /// Compara dos versiones <c>a.b.c</c> por números (lo que falte vale 0: <c>1.0</c> = <c>1.0.0</c>). Un tramo que no es
        /// número termina la comparación en ese punto (<c>1.2-dev</c> = <c>1.2</c>). Una versión mínima vacía o ilegible no
        /// bloquea [P]: devuelve 0 (iguales).
        /// </summary>
        public static int CompareVersions(string version, string minimum)
        {
            if (string.IsNullOrWhiteSpace(minimum) || !char.IsDigit(minimum.Trim()[0]))
            {
                return 0;
            }

            string[] a = (version ?? string.Empty).Trim().Split('.');
            string[] b = minimum.Trim().Split('.');
            int n = Math.Max(a.Length, b.Length);
            for (int i = 0; i < n; i++)
            {
                int x = Part(a, i);
                int y = Part(b, i);
                if (x != y)
                {
                    return x < y ? -1 : 1;
                }
            }

            return 0;
        }

        private static int Part(string[] parts, int i)
        {
            if (i >= parts.Length)
            {
                return 0;
            }

            string p = parts[i];
            int end = 0;
            while (end < p.Length && char.IsDigit(p[end]))
            {
                end++;
            }

            return end == 0 ? 0 : int.Parse(p.Substring(0, Math.Min(end, 9)), NumberStyles.None, CultureInfo.InvariantCulture);
        }
    }
}
