#nullable enable

namespace AF.Analytics
{
    /// <summary>
    /// Los parámetros comunes de 08 §8.1 que <see cref="AnalyticsService"/> adjunta a cada evento: <c>level</c>,
    /// <c>world</c>, <c>difficulty</c>, <c>murky</c>, <c>attempt</c>, <c>seeds_balance</c>, <c>canoes</c>, <c>streak</c>,
    /// <c>geo_tier</c>, <c>no_ads</c>, <c>app_version</c>, <c>content_version</c>, <c>rc_variant</c>, <c>session_id</c> y
    /// <c>net</c>. Un valor null no se envía (fuera de un nivel no hay <c>level</c> ni <c>attempt</c>) [P].
    /// </summary>
    public sealed class CommonParams
    {
        /// <summary>Nivel en curso (o el último jugado).</summary>
        public int? Level { get; set; }

        /// <summary>Mundo del nivel.</summary>
        public int? World { get; set; }

        /// <summary>Etiqueta del nivel (<c>normal</c>, <c>hard</c>, <c>very_hard</c>).</summary>
        public string? Difficulty { get; set; }

        /// <summary>Nivel con Aguas turbias.</summary>
        public bool? Murky { get; set; }

        /// <summary>Intento del nivel, desde 1.</summary>
        public int? Attempt { get; set; }

        /// <summary>Saldo de semillas.</summary>
        public int? SeedsBalance { get; set; }

        /// <summary>Canoas (−1 = infinitas [P]).</summary>
        public int? Canoes { get; set; }

        /// <summary>Victorias de la Crecida.</summary>
        public int? Streak { get; set; }

        /// <summary>Tramo geográfico (<c>T1</c>, <c>T2</c>, <c>T3</c>).</summary>
        public string? GeoTier { get; set; }

        /// <summary>Tiene Sin anuncios.</summary>
        public bool? NoAds { get; set; }

        /// <summary>Versión de la app.</summary>
        public string? AppVersion { get; set; }

        /// <summary><c>content_version</c> (08 §9).</summary>
        public string? ContentVersion { get; set; }

        /// <summary><c>biz_experiment_tag</c> (08 §9: «viaja en analítica como <c>rc_variant</c>»).</summary>
        public string? RcVariant { get; set; }

        /// <summary>Id de la sesión (D-033, Q-M4: arranque en frío o vuelta tras ≥ 30 min en segundo plano).</summary>
        public string? SessionId { get; set; }

        /// <summary><c>online</c> u <c>offline</c>.</summary>
        public string? Net { get; set; }

        /// <summary>Pone todo a null.</summary>
        public void Clear()
        {
            Level = null;
            World = null;
            Difficulty = null;
            Murky = null;
            Attempt = null;
            SeedsBalance = null;
            Canoes = null;
            Streak = null;
            GeoTier = null;
            NoAds = null;
            AppVersion = null;
            ContentVersion = null;
            RcVariant = null;
            SessionId = null;
            Net = null;
        }

        internal void AppendTo(AnalyticsEvent e)
        {
            Int(e, "level", Level);
            Int(e, "world", World);
            Str(e, "difficulty", Difficulty);
            Bool(e, "murky", Murky);
            Int(e, "attempt", Attempt);
            Int(e, "seeds_balance", SeedsBalance);
            Int(e, "canoes", Canoes);
            Int(e, "streak", Streak);
            Str(e, "geo_tier", GeoTier);
            Bool(e, "no_ads", NoAds);
            Str(e, "app_version", AppVersion);
            Str(e, "content_version", ContentVersion);
            Str(e, "rc_variant", RcVariant);
            Str(e, "session_id", SessionId);
            Str(e, "net", Net);
        }

        private static void Int(AnalyticsEvent e, string name, int? v)
        {
            if (v.HasValue && e.Find(name) == null)
            {
                e.AddCommon(AnalyticsParam.Of(name, (long)v.Value));
            }
        }

        private static void Bool(AnalyticsEvent e, string name, bool? v)
        {
            if (v.HasValue && e.Find(name) == null)
            {
                e.AddCommon(AnalyticsParam.Of(name, v.Value));
            }
        }

        private static void Str(AnalyticsEvent e, string name, string? v)
        {
            if (v != null && e.Find(name) == null)
            {
                e.AddCommon(AnalyticsParam.Of(name, v));
            }
        }
    }

    /// <summary>De dónde salen los parámetros comunes en cada envío (el arranque lo implementa con el flujo y la cartera).</summary>
    public interface ICommonParamsSource
    {
        /// <summary>Rellena <paramref name="p"/> (llega con todo a null).</summary>
        void Fill(CommonParams p);
    }
}
