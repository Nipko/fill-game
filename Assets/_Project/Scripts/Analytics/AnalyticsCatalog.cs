#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Analytics
{
    /// <summary>Un evento de la taxonomía con sus parámetros específicos.</summary>
    public sealed class CatalogEntry
    {
        internal CatalogEntry(string name, string[] parameters)
        {
            Name = name;
            Params = parameters;
        }

        /// <summary>Nombre exacto (08 §8.2).</summary>
        public string Name { get; }

        /// <summary>Parámetros específicos admitidos, en el orden de la tabla.</summary>
        public IReadOnlyList<string> Params { get; }

        /// <summary>El evento admite el parámetro específico <paramref name="name"/>.</summary>
        public bool Allows(string name)
        {
            for (int i = 0; i < Params.Count; i++)
            {
                if (string.Equals(Params[i], name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// La taxonomía única de analítica (08 §8.2; «un evento nuevo se da de alta en esta tabla antes de usarse»): cada evento
    /// con sus parámetros específicos, copiados de 08 §8.2 y, donde 08 dice «Según capítulo 4 §11», de 04 §11. Los comunes
    /// (08 §8.1) están en <see cref="CommonParamNames"/> y las propiedades de usuario en <see cref="UserProperties"/>.
    /// <c>AnalyticsCatalogTests</c> lee las dos tablas del GDD y comprueba que esto es exactamente lo que dicen.
    /// </summary>
    public static class AnalyticsCatalog
    {
        /// <summary>Límite de parámetros propios por evento (08 §8.1: 25 de Firebase − 15 comunes).</summary>
        public const int MaxSpecificParams = 10;

        /// <summary>Parámetros comunes de 08 §8.1, en su orden.</summary>
        public static readonly IReadOnlyList<string> CommonParamNames = new[]
        {
            "level", "world", "difficulty", "murky", "attempt", "seeds_balance", "canoes", "streak", "geo_tier", "no_ads",
            "app_version", "content_version", "rc_variant", "session_id", "net",
        };

        /// <summary>
        /// Valores de <c>key</c> de <c>af_settings_change</c> (08 §8.2, en su orden): los de siempre, <c>push</c> (la fila «Avisos»;
        /// añadido de la UI de los avisos de D-033) y <c>reduce_motion</c> y <c>high_contrast</c> (Q-B9-16).
        /// </summary>
        public static readonly IReadOnlyList<string> SettingsChangeKeys = new[]
        {
            "patterns", "sound", "music", "haptics", "language", "consent", "sloth_messages", "push", "reduce_motion", "high_contrast",
        };

        /// <summary>
        /// Valores de <c>source</c> de <c>af_settings_change</c> (08 §8.2, en su orden; D-046, punto 2): de dónde llega el cambio de
        /// <c>sound</c> o <c>music</c>, la fila de Ajustes o los botones de un toque de la pausa y del inicio.
        /// </summary>
        public static readonly IReadOnlyList<string> SettingsChangeSources = new[] { "settings", "pause", "home" };

        /// <summary>Valores de <c>origin</c> de <c>af_push_prompt</c> (08 §8.2; B6-Q17): el primer pre-prompt, el segundo o Ajustes.</summary>
        public static readonly IReadOnlyList<string> PushPromptOrigins = new[] { "first", "second", "settings" };

        /// <summary>Propiedades de usuario de 08 §8.1, en su orden.</summary>
        public static readonly IReadOnlyList<string> UserProperties = new[]
        {
            "geo_tier", "payer_class", "consent_profile", "device_class", "install_phase", "first_world_reached",
            "device_language", "app_language",
        };

        private static readonly CatalogEntry[] Entries =
        {
            E("af_boot", "cold_start_ms", "first_interaction_ms", "rc_source", "migrated_from_schema"),
            E("af_consent", "profile", "text_version", "analytics", "ads_personalized", "age_band", "step"),
            E("af_push_prompt", "step", "result", "origin"), // origin: añadido de la UI de los avisos (D-033)
            E("af_push_sent", "type"),
            E("af_push_open", "type"),
            E("af_asset_pack", "pack", "state", "bytes", "ms"),
            E("level_start", "level_name", "variant", "band"),
            E("af_level_win", "pours", "par", "pours_over_par", "undo_used", "boosters_json", "t_ms", "reward_seeds", "double_harvest", "rescued"),
            E("af_level_fail", "kind", "canoe_lost", "streak_broken", "shield_used", "relief_served", "pours", "t_ms"),
            E("af_level_abandon", "pours", "t_ms", "reason"),
            E("level_end", "success", "level_name"),
            E("af_anim_skipped", "anim", "at_ms", "duration_cfg_ms"),
            E("af_river_lowered", "variant", "fails_level"),
            E("af_dda_applied", "reason", "variant"),
            E("af_mechanic_intro", "mech", "completed", "attempts"),
            E("af_label_shown", "label", "source"),

            // 08 §8.2: «Según capítulo 4 §11».
            E("af_booster_used", "booster", "source", "price", "state_hash"),
            E("af_booster_denied", "booster", "reason"),
            E("af_hint_result", "solver", "realities", "nodes", "ms", "executed_hint"),
            E("af_shuffle_result", "method", "par_before", "par_after", "tries", "revealed_murky"),
            E("af_stuck_shown", "rescue_price", "discount", "rescue_solvable", "undo_available", "options_order", "skip_eligible", "shield_status"),
            E("af_stuck_choice", "choice", "position", "ms_to_choice"),
            E("af_stuck_outcome", "result"),
            E("af_rescue_unsolvable", "state_hash", "seed"),
            E("af_skip_used", "fails_level", "since_last_skip", "level_tag"),
            E("af_canoes_depleted", "wait_remaining_s"),
            E("af_no_canoes_choice", "choice"),
            E("af_canoes_wait_return", "waited_s", "returned"),
            E("af_infinite_canoes_start", "source", "minutes", "fails_during"),
            E("af_infinite_canoes_end", "source", "minutes", "fails_during"),

            E("af_streak_milestone", "milestone", "reward_type", "reward_value"),
            E("af_streak_shield_used", "streak_at_use"),
            E("earn_virtual_currency", "virtual_currency_name", "source", "value"),
            E("spend_virtual_currency", "virtual_currency_name", "item_name", "value"),
            E("af_wallet_snapshot", "seeds", "stock_json", "max_level", "days_since_install"),
            E("af_basket_open", "level", "contents_json"),
            E("af_calendar_claim", "day", "reward_type", "reward_value", "days_missed_before", "choice", "cycle"), // choice y cycle: D-033 (B4)
            E("af_piggy", "state", "seeds_inside"),
            E("af_daily_challenge", "date", "result", "t_ms", "murky_bonus", "streak_days"),
            E("af_river_fact_shown", "fact_id", "dismissed_ms"),
            E("af_fact_page_open", "tint_id", "unlocked"),
            E("af_map_open", "source", "stop", "world"),
            E("af_map_close", "dwell_ms", "stops_tapped", "view_changes", "north_up"),
            E("af_stop_arrive", "stop_id", "world", "level", "view", "skipped"),
            E("af_fruit_discovered", "tint_id"),
            E("af_fauna_sighted", "species_id", "level", "first"),
            E("af_skin_unlock", "skin_id", "source"),
            E("af_comeback_gift", "days_away"),
            E("af_event_join", "event_id", "n"),
            E("af_event_milestone", "event_id", "n"),
            E("af_event_claim", "event_id", "n"),
            E("af_league_rank", "week", "bucket_rank", "tier"),
            E("af_world_enter", "world", "days_since_install", "pack_ready"),
            E("af_ad_request", "placement", "format", "result", "ms", "network"),
            E("af_ad_shown", "placement", "format", "gap_since_last_s", "levels_since_last", "session_count", "day_count"),
            E("ad_impression", "ad_platform", "ad_source", "ad_format", "ad_unit_name", "value", "currency"),
            E("af_ad_reward", "placement", "granted", "latency_s"),
            E("af_iap_offer_shown", "sku", "context", "price_local", "currency"),
            E("af_iap_result", "sku", "result", "context", "ms"),
            E("purchase", "transaction_id", "value", "currency", "items", "context"),
            E("af_entitlement_restored", "type", "count"),
            E("af_shop_view", "tab", "from"),
            E("af_settings_change", "key", "value", "source"),
            E("af_account_delete", "step", "source"),
            E("af_save_sync", "result", "bytes", "ms"),
            E("af_level_load", "ms", "from_cache", "pack_id"),
            E("af_perf_sample", "fps_avg", "fps_p5", "frame_ms_p95", "mem_mb", "device_class", "gpu", "thermal"),
            E("af_error", "module", "code", "fatal"),
        };

        private static readonly Dictionary<string, CatalogEntry> ByName = Index();

        /// <summary>Todos los eventos, en el orden de 08 §8.2.</summary>
        public static IReadOnlyList<CatalogEntry> All => Entries;

        /// <summary>El evento <paramref name="name"/>, o null si no está dado de alta.</summary>
        public static CatalogEntry? Find(string name)
        {
            return name != null && ByName.TryGetValue(name, out CatalogEntry? e) ? e : null;
        }

        /// <summary><paramref name="name"/> es un parámetro común (08 §8.1).</summary>
        public static bool IsCommon(string name)
        {
            for (int i = 0; i < CommonParamNames.Count; i++)
            {
                if (string.Equals(CommonParamNames[i], name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static CatalogEntry E(string name, params string[] parameters) => new CatalogEntry(name, parameters);

        private static Dictionary<string, CatalogEntry> Index()
        {
            var d = new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
            for (int i = 0; i < Entries.Length; i++)
            {
                d.Add(Entries[i].Name, Entries[i]);
            }

            return d;
        }
    }
}
