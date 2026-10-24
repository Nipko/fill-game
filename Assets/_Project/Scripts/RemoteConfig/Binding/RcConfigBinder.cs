#nullable enable
using System;
using System.Collections.Generic;
using AF.Ads;
using AF.Backend.Verification;
using AF.Boosters;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.LiveOps;
using AF.Platform;

namespace AF.RemoteConfig.Binding
{
    /// <summary>
    /// Las clases de cifras del cliente leídas de Remote Config (bloque B7; nota <c>docs/dev/remote-config-analytics.md</c> §2):
    /// cada propiedad con <c>[RemoteConfigKey]</c> de <see cref="EconomyConfig"/>, <see cref="BoosterConfig"/>,
    /// <see cref="AdsConfig"/>, <see cref="StoreConfig"/>, <see cref="OffersConfig"/>, <see cref="DailyChallengeConfig"/>, <see cref="StreakConfig"/>,
    /// <see cref="BasketConfig"/>, <see cref="ClockSecurityConfig"/>, <see cref="VerificationQueueConfig"/> y <see cref="RevocationConfig"/>
    /// (F3-5), <see cref="TakeoverConfig"/> y <see cref="LoginCalendarConfig"/> (B4) toma el valor de su clave en unos valores congelados
    /// (<see cref="RcSnapshot"/>). Con los defaults compilados sale exactamente <c>X.Defaults</c> (lo prueba
    /// <c>RcConfigBinderTests</c>).
    /// <para>
    /// Sin reflexión (Managed Stripping High, B10): cada clave tiene su asignación escrita aquí, en una tabla por clase. Un
    /// valor remoto que no se entiende (JSON roto, tipo de otra forma) deja el default de esa propiedad y se anota en
    /// <see cref="RcConfigs.Errors"/>; si la clase entera no pasa su <c>Validate()</c>, se usa su <c>Defaults</c> completo y
    /// también se anota [P] (nota, Q-B7-7). Cada <c>Validate()</c> comprueba solo lo que su clase valida, no todos los rangos
    /// de 08 §9 (por ejemplo, <c>EconomyConfig</c> no limita <c>eco_reward_*</c> a 15-30 / 45-90 / 60-120): un valor remoto
    /// fuera del rango de 08 §9 que su <c>Validate()</c> acepta se usa tal cual hasta que se completen esas comprobaciones.
    /// </para>
    /// Los kill switches de negocio entran ya combinados con <see cref="BizGates"/>: <c>biz_maintenance_mode</c> apaga
    /// anuncios e IAP, y <c>biz_min_app_version</c> apaga las compras.
    /// </summary>
    public static class RcConfigBinder
    {
        private static readonly Entry<EconomyConfig>[] EconomyEntries =
        {
            I<EconomyConfig>("eco_reward_normal", (c, v) => c.EcoRewardNormal = v),
            I<EconomyConfig>("eco_reward_hard", (c, v) => c.EcoRewardHard = v),
            I<EconomyConfig>("eco_reward_very_hard", (c, v) => c.EcoRewardVeryHard = v),
            I<EconomyConfig>("ads_rv_double_harvest_from_level", (c, v) => c.AdsRvDoubleHarvestFromLevel = v),
            I<EconomyConfig>("undo_refill_amount", (c, v) => c.UndoRefillAmount = v),
            I<EconomyConfig>("undo_refill_seed_price", (c, v) => c.UndoRefillSeedPrice = v),
            I<EconomyConfig>("extra_cuia_seed_price", (c, v) => c.ExtraCuiaSeedPrice = v),
            I<EconomyConfig>("hint_seed_price", (c, v) => c.HintSeedPrice = v),
            I<EconomyConfig>("shuffle_seed_price", (c, v) => c.ShuffleSeedPrice = v),
            I<EconomyConfig>("reveal_seed_price", (c, v) => c.RevealSeedPrice = v),
            I<EconomyConfig>("skip_seed_price", (c, v) => c.SkipSeedPrice = v),
            I<EconomyConfig>("rescue_min_level", (c, v) => c.RescueMinLevel = v),
            J<EconomyConfig>("rescue_price_bands", (c, j) => c.RescuePriceBands = Pairs(j, (a, b) => new PriceBand(a, b))),
            D<EconomyConfig>("rescue_first_day_mult", (c, v) => c.RescueFirstDayMult = v),
            I<EconomyConfig>("canoes_max", (c, v) => c.CanoesMax = v),
            I<EconomyConfig>("canoes_regen_min", (c, v) => c.CanoesRegenMin = v),
            B<EconomyConfig>("canoes_refill_seeds_enabled", (c, v) => c.CanoesRefillSeedsEnabled = v),
            I<EconomyConfig>("canoes_refill_seed_price", (c, v) => c.CanoesRefillSeedPrice = v),
        };

        private static readonly Entry<BoosterConfig>[] BoosterEntries =
        {
            I<BoosterConfig>("undo_free_per_level", (c, v) => c.UndoFreePerLevel = v),
            I<BoosterConfig>("undo_unlimited_until_level", (c, v) => c.UndoUnlimitedUntilLevel = v),
            I<BoosterConfig>("extra_cuia_unlock_level", (c, v) => c.ExtraCuiaUnlockLevel = v),
            I<BoosterConfig>("extra_cuia_max_per_level", (c, v) => c.ExtraCuiaMaxPerLevel = v),
            I<BoosterConfig>("shuffle_unlock_level", (c, v) => c.ShuffleUnlockLevel = v),
            I<BoosterConfig>("rescue_max_per_level", (c, v) => c.RescueMaxPerLevel = v),
            B<BoosterConfig>("rescue_check_solvable", (c, v) => c.RescueCheckSolvable = v),
            I<BoosterConfig>("hint_budget_nodes", (c, v) => c.HintBudgetNodes = v),
            D<BoosterConfig>("hint_weight", (c, v) => c.HintWeight = v),
            I<BoosterConfig>("hint_timeout_ms", (c, v) => c.HintTimeoutMs = v),
            I<BoosterConfig>("hint_unlock_level", (c, v) => c.HintUnlockLevel = v),
            I<BoosterConfig>("hint_tutorial_units", (c, v) => c.HintTutorialUnits = v),
            I<BoosterConfig>("hint_idle_from_level", (c, v) => c.HintIdleFromLevel = v),
            I<BoosterConfig>("hint_idle_delay_s", (c, v) => c.HintIdleDelayS = v),
            I<BoosterConfig>("restart_confirm_min_pours", (c, v) => c.RestartConfirmMinPours = v),
            I<BoosterConfig>("relief_after_fails", (c, v) => c.ReliefAfterFails = v),
            I<BoosterConfig>("anim_river_lowered_banner_ms", (c, v) => c.AnimRiverLoweredBannerMs = v),
            S<BoosterConfig>("stuck_order_t1", (c, v) => c.StuckOrderT1 = v),
            S<BoosterConfig>("stuck_order_t3", (c, v) => c.StuckOrderT3 = v),
            B<BoosterConfig>("stuck_show_shield_status", (c, v) => c.StuckShowShieldStatus = v),
        };

        private static readonly Entry<AdsConfig>[] AdsEntries =
        {
            B<AdsConfig>("biz_ads_enabled", (c, v) => c.BizAdsEnabled = v),
            B<AdsConfig>("biz_interstitials_enabled", (c, v) => c.BizInterstitialsEnabled = v),
            B<AdsConfig>("biz_rewarded_enabled", (c, v) => c.BizRewardedEnabled = v),
            I<AdsConfig>("ads_first_interstitial_level", (c, v) => c.AdsFirstInterstitialLevel = v),
            J<AdsConfig>("ads_interstitial_every_n_levels", (c, j) => c.AdsInterstitialEveryNLevels = Pairs(j, (a, b) => new CadenceBand(a, b))),
            I<AdsConfig>("ads_interstitial_min_gap_s", (c, v) => c.AdsInterstitialMinGapS = v),
            I<AdsConfig>("ads_interstitial_session_warmup_s", (c, v) => c.AdsInterstitialSessionWarmupS = v),
            I<AdsConfig>("ads_interstitial_session_cap", (c, v) => c.AdsInterstitialSessionCap = v),
            I<AdsConfig>("ads_interstitial_daily_cap", (c, v) => c.AdsInterstitialDailyCap = v),
            I<AdsConfig>("ads_interstitial_suppress_after_iap_h", (c, v) => c.AdsInterstitialSuppressAfterIapH = v),
            I<AdsConfig>("ads_rv_cap_double_harvest", (c, v) => c.AdsRvCapDoubleHarvest = v),
            I<AdsConfig>("ads_rv_cap_undo_refill", (c, v) => c.AdsRvCapUndoRefill = v),
            I<AdsConfig>("ads_rv_cap_hint", (c, v) => c.AdsRvCapHint = v),
            I<AdsConfig>("ads_rv_cap_reveal", (c, v) => c.AdsRvCapReveal = v),
            I<AdsConfig>("ads_rv_cap_extra_canoe", (c, v) => c.AdsRvCapExtraCanoe = v),
            I<AdsConfig>("ads_rv_cap_daily_chest", (c, v) => c.AdsRvCapDailyChest = v),
            I<AdsConfig>("ads_rv_cooldown_s_double_harvest", (c, v) => c.AdsRvCooldownSDoubleHarvest = v),
            I<AdsConfig>("ads_rv_cooldown_s_undo_refill", (c, v) => c.AdsRvCooldownSUndoRefill = v),
            I<AdsConfig>("ads_rv_cooldown_s_hint", (c, v) => c.AdsRvCooldownSHint = v),
            I<AdsConfig>("ads_rv_cooldown_s_reveal", (c, v) => c.AdsRvCooldownSReveal = v),
            I<AdsConfig>("ads_rv_cooldown_s_extra_canoe", (c, v) => c.AdsRvCooldownSExtraCanoe = v),
            I<AdsConfig>("ads_rv_cooldown_s_daily_chest", (c, v) => c.AdsRvCooldownSDailyChest = v),
            I<AdsConfig>("ads_rv_global_cap_day", (c, v) => c.AdsRvGlobalCapDay = v),
            B<AdsConfig>("eco_calendar_double_ad_enabled", (c, v) => c.EcoCalendarDoubleAdEnabled = v),
        };

        private static readonly Entry<StoreConfig>[] StoreEntries =
        {
            B<StoreConfig>("biz_iap_enabled", (c, v) => c.BizIapEnabled = v),
            J<StoreConfig>("eco_seed_pack_amounts", (c, j) => c.EcoSeedPackAmounts = SeedPacks(j)),
            I<StoreConfig>("eco_noads_bonus_seeds", (c, v) => c.EcoNoadsBonusSeeds = v),
            I<StoreConfig>("af_rescue_daily_cap", (c, v) => c.AfRescueDailyCap = v),
            I<StoreConfig>("af_rescue_hide_after_declines", (c, v) => c.AfRescueHideAfterDeclines = v),
            I<StoreConfig>("eco_piggy_min_buy", (c, v) => c.EcoPiggyMinBuy = v),
            I<StoreConfig>("eco_piggy_cap", (c, v) => c.EcoPiggyCap = v),
        };

        // B5 (ofertas y Cántaro): eco_piggy_cap y eco_piggy_min_buy siguen en StoreConfig.
        private static readonly Entry<OffersConfig>[] OffersEntries =
        {
            J<OffersConfig>("eco_starter_window_levels", (c, j) => c.EcoStarterWindowLevels = Ints(j)),
            I<OffersConfig>("eco_starter_timer_h", (c, v) => c.EcoStarterTimerH = v),
            S<OffersConfig>("eco_starter_first_show", (c, v) => c.EcoStarterFirstShow = v),
            S<OffersConfig>("eco_noads_card_trigger", (c, v) => c.EcoNoadsCardTrigger = v),
            I<OffersConfig>("eco_piggy_from_level", (c, v) => c.EcoPiggyFromLevel = v),
            J<OffersConfig>("eco_piggy_fill", (c, j) => c.EcoPiggyFill = Ints(j)),
        };

        private static readonly Entry<DailyChallengeConfig>[] DailyEntries =
        {
            B<DailyChallengeConfig>("biz_feature_daily_challenge", (c, v) => c.BizFeatureDailyChallenge = v),
            I<DailyChallengeConfig>("live_unlock_level_daily", (c, v) => c.LiveUnlockLevelDaily = v),
            J<DailyChallengeConfig>("lvl_daily_bracket_levels", (c, j) => c.LvlDailyBracketLevels = Ints(j)),
            I<DailyChallengeConfig>("live_daily_offset", (c, v) => c.LiveDailyOffset = v),
            Txt<DailyChallengeConfig>("daily_overrides", (c, text) => c.DailyOverrides = Overrides(text)),
            J<DailyChallengeConfig>("live_daily_challenge_reward", (c, j) => c.LiveDailyChallengeReward = DailyReward(j)),
            S<DailyChallengeConfig>("live_daily_streak7_reward", (c, v) => c.LiveDailyStreak7Reward = v),
            B<DailyChallengeConfig>("ui_murky_bonus_enabled", (c, v) => c.UiMurkyBonusEnabled = v),
        };

        private static readonly Entry<StreakConfig>[] StreakEntries =
        {
            B<StreakConfig>("biz_feature_win_streak", (c, v) => c.BizFeatureWinStreak = v),
            I<StreakConfig>("streak_unlock_level", (c, v) => c.StreakUnlockLevel = v),
            Txt<StreakConfig>("streak_milestones", (c, text) => c.StreakMilestones = Milestones(text)),
            B<StreakConfig>("streak_hits_once_per_day", (c, v) => c.StreakHitsOncePerDay = v),
            I<StreakConfig>("streak_daily_cap", (c, v) => c.StreakDailyCap = v),
            I<StreakConfig>("streak_shield_per_day", (c, v) => c.StreakShieldPerDay = v),
            B<StreakConfig>("streak_restart_counts_as_fail", (c, v) => c.StreakRestartCountsAsFail = v),
            I<StreakConfig>("streak_reminder_hours", (c, v) => c.StreakReminderHours = v),
        };

        private static readonly Entry<BasketConfig>[] BasketEntries =
        {
            Txt<BasketConfig>("eco_basket_cycle", (c, text) => c.EcoBasketCycle = Cycle(text)),
            I<BasketConfig>("eco_basket_seeds", (c, v) => c.EcoBasketSeeds = v),
        };

        private static readonly Entry<ClockSecurityConfig>[] ClockEntries =
        {
            I<ClockSecurityConfig>("sec_clock_max_drift_h", (c, v) => c.SecClockMaxDriftH = v),
        };

        // F3-5: la cola de verificación de compras (08 §10.3) y la revocación del SSV (08 §10.2).
        private static readonly Entry<VerificationQueueConfig>[] VerificationEntries =
        {
            I<VerificationQueueConfig>("sec_purchase_deferred_ack_h", (c, v) => c.SecPurchaseDeferredAckH = v),
        };

        private static readonly Entry<RevocationConfig>[] RevocationEntries =
        {
            I<RevocationConfig>("sec_ssv_grace_h", (c, v) => c.SecSsvGraceH = v),
        };

        // B4: la cola de takeovers (rango de 08 §9: solo 1) y la Bitácora del río. La tasa va antes que la tabla, que se valida con
        // ella; eco_calendar_table_v2 aún no tiene default en 08 §9 (pending): sin valor remoto queda la tabla compilada (Q-B4).
        private static readonly Entry<TakeoverConfig>[] TakeoverEntries =
        {
            I<TakeoverConfig>("ui_max_takeovers_per_session", (c, v) => c.UiMaxTakeoversPerSession = v),
        };

        private static readonly Entry<LoginCalendarConfig>[] LoginCalendarEntries =
        {
            B<LoginCalendarConfig>("biz_feature_login_calendar", (c, v) => c.BizFeatureLoginCalendar = v),
            Txt<LoginCalendarConfig>("eco_calendar_locked_booster_seeds", (c, text) => c.EcoCalendarLockedBoosterSeeds = ValueRate(text)),
            OptionalTxt<LoginCalendarConfig>("eco_calendar_table_v2", (c, text) => c.EcoCalendarTableV2 = CalendarTable(text, c.EcoCalendarLockedBoosterSeeds)),
        };

        // D-046: la Liga del río de la 1.0 (docs/dev/liga.md §5).
        private static readonly Entry<LeagueConfig>[] LeagueEntries =
        {
            B<LeagueConfig>("live_league_enabled", (c, v) => c.LiveLeagueEnabled = v),
            I<LeagueConfig>("live_league_unlock_level", (c, v) => c.LiveLeagueUnlockLevel = v),
            I<LeagueConfig>("live_league_cache_minutes", (c, v) => c.LiveLeagueCacheMinutes = v),
        };

        /// <summary>Las claves que lee cada clase, en orden (para los tests de cobertura).</summary>
        public static IReadOnlyDictionary<Type, IReadOnlyList<string>> BoundKeys { get; } = new Dictionary<Type, IReadOnlyList<string>>
        {
            { typeof(EconomyConfig), KeysOf(EconomyEntries) },
            { typeof(BoosterConfig), KeysOf(BoosterEntries) },
            { typeof(AdsConfig), KeysOf(AdsEntries) },
            { typeof(StoreConfig), KeysOf(StoreEntries) },
            { typeof(OffersConfig), KeysOf(OffersEntries) },
            { typeof(DailyChallengeConfig), KeysOf(DailyEntries) },
            { typeof(StreakConfig), KeysOf(StreakEntries) },
            { typeof(BasketConfig), KeysOf(BasketEntries) },
            { typeof(ClockSecurityConfig), KeysOf(ClockEntries) },
            { typeof(VerificationQueueConfig), KeysOf(VerificationEntries) },
            { typeof(RevocationConfig), KeysOf(RevocationEntries) },
            { typeof(TakeoverConfig), KeysOf(TakeoverEntries) },
            { typeof(LoginCalendarConfig), KeysOf(LoginCalendarEntries) },
            { typeof(LeagueConfig), KeysOf(LeagueEntries) },
            { typeof(ConsentConfig), ConsentConfigBinder.Keys }, // B12: su lector propio (ConsentConfigBinder)
            { typeof(A11yConfig), A11yConfigBinder.Keys }, // B9: su lector propio (A11yConfigBinder)
        };

        /// <summary>
        /// Todas las clases de cifras con los valores <paramref name="rc"/>, ya validadas y con los kill switches combinados
        /// (<paramref name="appVersion"/> es la de la app, para <c>biz_min_app_version</c>).
        /// </summary>
        public static RcConfigs Bind(RcSnapshot rc, string appVersion)
        {
            if (rc == null)
            {
                throw new ArgumentNullException(nameof(rc));
            }

            var errors = new List<string>();
            var gates = new BizGates(rc, appVersion);
            EconomyConfig economy = Fill(new EconomyConfig(), EconomyEntries, rc, errors, c => c.Validate(), () => EconomyConfig.Defaults);
            BoosterConfig boosters = Fill(new BoosterConfig(), BoosterEntries, rc, errors, c => c.Validate(), () => BoosterConfig.Defaults);
            AdsConfig ads = Fill(new AdsConfig(), AdsEntries, rc, errors, c => c.Validate(), () => AdsConfig.Defaults);
            StoreConfig store = Fill(new StoreConfig(), StoreEntries, rc, errors, c => c.Validate(), () => StoreConfig.Defaults);
            OffersConfig offers = Fill(new OffersConfig(), OffersEntries, rc, errors, c => c.Validate(), () => OffersConfig.Defaults);
            DailyChallengeConfig daily = Fill(new DailyChallengeConfig(), DailyEntries, rc, errors, c => c.Validate(), () => DailyChallengeConfig.Defaults);
            StreakConfig streak = Fill(new StreakConfig(), StreakEntries, rc, errors, c => c.Validate(), () => StreakConfig.Defaults);
            BasketConfig basket = Fill(new BasketConfig(), BasketEntries, rc, errors, c => c.Validate(), () => BasketConfig.Defaults);
            ClockSecurityConfig clock = Fill(new ClockSecurityConfig(), ClockEntries, rc, errors, c => c.Validate(), () => ClockSecurityConfig.Defaults);
            VerificationQueueConfig verification = Fill(new VerificationQueueConfig(), VerificationEntries, rc, errors, c => c.Validate(), () => VerificationQueueConfig.Defaults);
            RevocationConfig revocation = Fill(new RevocationConfig(), RevocationEntries, rc, errors, c => c.Validate(), () => RevocationConfig.Defaults);
            TakeoverConfig takeovers = Fill(new TakeoverConfig(), TakeoverEntries, rc, errors, c => c.Validate(), () => TakeoverConfig.Defaults);
            LoginCalendarConfig calendar = Fill(new LoginCalendarConfig(), LoginCalendarEntries, rc, errors, c => c.Validate(), () => LoginCalendarConfig.Defaults);
            LeagueConfig league = Fill(new LeagueConfig(), LeagueEntries, rc, errors, c => c.Validate(), () => LeagueConfig.Defaults); // D-046
            ConsentConfig consent = ConsentConfigBinder.Bind(rc, errors);
            A11yConfig a11y = A11yConfigBinder.Bind(rc, errors); // B9

            // Compuertas de negocio (08 §9): el mantenimiento apaga anuncios e IAP; la versión mínima, las compras.
            ads.BizAdsEnabled = gates.AdsEnabled;
            ads.BizInterstitialsEnabled = gates.InterstitialsEnabled;
            ads.BizRewardedEnabled = gates.RewardedEnabled;
            store.BizIapEnabled = gates.IapEnabled;

            // El Reto lleva su propia copia del anti-reloj (DailyChallengeConfig.ClockSecurity): la misma clave única.
            daily.ClockSecurity.SecClockMaxDriftH = clock.SecClockMaxDriftH;

            // biz_feature_rescue (08 §9) [P]: sin Cuia de rescate; el resto de kill switches se aplican donde vive su sistema.
            if (!gates.Feature(BizFeature.Rescue))
            {
                boosters.RescueMaxPerLevel = 0;
            }
            return new RcConfigs(rc, gates, economy, boosters, ads, store, offers, daily, streak, basket, clock, errors)
            {
                Verification = verification,
                Revocation = revocation,
                Takeovers = takeovers,
                LoginCalendar = calendar,
                League = league,
                BackendSwitch = new RcBackendSwitch(rc),
                Consent = consent,
                DragPourEnabled = RcGameplayBinder.DragPourEnabled(rc, errors), // B11: C10 (RcGameplayBinder)
                LevelLabels = RcGameplayBinder.LevelLabels(rc, errors), // B11: lvl_label_override
                A11y = a11y,
            };
        }

        /// <summary>Atajo: <see cref="Bind"/> con los valores vigentes de <paramref name="service"/>.</summary>
        public static RcConfigs Bind(RemoteConfigService service, string appVersion)
        {
            return Bind((service ?? throw new ArgumentNullException(nameof(service))).Freeze(), appVersion);
        }

        private static T Fill<T>(T config, Entry<T>[] entries, RcSnapshot rc, List<string> errors, Action<T> validate, Func<T> defaults)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                Entry<T> e = entries[i];
                try
                {
                    e.Apply(config, rc);
                }
                catch (Exception ex) when (ex is FormatException || ex is InvalidOperationException || ex is ArgumentException)
                {
                    errors.Add(e.Key + ": " + ex.Message); // se queda el default de esa propiedad
                }
            }

            try
            {
                validate(config);
                return config;
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(typeof(T).Name + ": " + ex.Message + " Se usan los defaults de 08 §9.");
                return defaults();
            }
        }

        private static IReadOnlyList<string> KeysOf<T>(Entry<T>[] entries)
        {
            var keys = new string[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                keys[i] = entries[i].Key;
            }

            return keys;
        }

        private static Entry<T> I<T>(string key, Action<T, int> set) => new Entry<T>(key, (c, rc) => set(c, rc.Get<int>(key)));

        private static Entry<T> D<T>(string key, Action<T, double> set) => new Entry<T>(key, (c, rc) => set(c, rc.Get<double>(key)));

        private static Entry<T> B<T>(string key, Action<T, bool> set) => new Entry<T>(key, (c, rc) => set(c, rc.Get<bool>(key)));

        private static Entry<T> S<T>(string key, Action<T, string> set) => new Entry<T>(key, (c, rc) => set(c, rc.Get<string>(key)));

        // JSON leído como árbol.
        private static Entry<T> J<T>(string key, Action<T, RcJson> set) => new Entry<T>(key, (c, rc) => set(c, rc.Get<RcJson>(key)));

        // JSON como texto, para los lectores propios de cada módulo (StreakMilestones, BasketConfig, DailyOverrides).
        private static Entry<T> Txt<T>(string key, Action<T, string> set) => new Entry<T>(key, (c, rc) => set(c, rc.Get<string>(key)));

        // Como Txt, para una clave sin default compilado en 08 §9 (pending): sin valor remoto se queda el default del código.
        private static Entry<T> OptionalTxt<T>(string key, Action<T, string> set) => new Entry<T>(key, (c, rc) =>
        {
            if (rc.TryGet(key, out string text))
            {
                set(c, text);
            }
        });

        private static LoginCalendarValueRate ValueRate(string json)
        {
            if (!LoginCalendarValueRate.TryParse(json, out LoginCalendarValueRate rate))
            {
                throw new FormatException("eco_calendar_locked_booster_seeds no válido");
            }

            return rate;
        }

        // FormatException si no vale: se queda la tabla compilada (Q-B4).
        private static LoginCalendarTable CalendarTable(string json, LoginCalendarValueRate rate) => LoginCalendarTable.Parse(json, rate);

        private static IReadOnlyList<TItem> Pairs<TItem>(RcJson json, Func<int, int, TItem> make)
        {
            if (json.Kind != RcJsonKind.Array)
            {
                throw new FormatException("se esperaba un arreglo de pares [[a,b],…]");
            }

            var items = new TItem[json.Items.Count];
            for (int i = 0; i < items.Length; i++)
            {
                RcJson pair = json.Items[i];
                if (pair.Kind != RcJsonKind.Array || pair.Items.Count != 2)
                {
                    throw new FormatException("se esperaba un par [a,b]");
                }

                items[i] = make(Int(pair.Items[0]), Int(pair.Items[1]));
            }

            return items;
        }

        private static IReadOnlyList<int> Ints(RcJson json)
        {
            if (json.Kind != RcJsonKind.Array)
            {
                throw new FormatException("se esperaba un arreglo de enteros");
            }

            var items = new int[json.Items.Count];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = Int(json.Items[i]);
            }

            return items;
        }

        private static IReadOnlyList<SeedPack> SeedPacks(RcJson json)
        {
            if (json.Kind != RcJsonKind.Object)
            {
                throw new FormatException("se esperaba un objeto {sku: semillas}");
            }

            var packs = new SeedPack[json.Members.Count];
            for (int i = 0; i < packs.Length; i++)
            {
                packs[i] = new SeedPack(json.Members[i].Key, Int(json.Members[i].Value));
            }

            return packs;
        }

        private static DailyChallengeReward DailyReward(RcJson json)
        {
            RcJson? seeds = json.Kind == RcJsonKind.Object ? json["seeds"] : null;
            RcJson? mult = json.Kind == RcJsonKind.Object ? json["murky_bonus_mult"] : null;
            if (seeds == null || mult == null || json.Members.Count != 2)
            {
                throw new FormatException("se esperaba {\"seeds\":n,\"murky_bonus_mult\":m}");
            }

            return new DailyChallengeReward(Int(seeds), Int(mult));
        }

        private static DailyOverrides Overrides(string json)
        {
            if (!DailyOverrides.TryParse(json, out DailyOverrides result, out string error))
            {
                throw new FormatException(error);
            }

            return result;
        }

        private static StreakMilestones Milestones(string json)
        {
            if (!StreakMilestones.TryParse(json, out StreakMilestones result))
            {
                throw new FormatException("streak_milestones no válido");
            }

            return result;
        }

        private static IReadOnlyList<string> Cycle(string json)
        {
            if (!BasketConfig.TryParseCycle(json, out IReadOnlyList<string> cycle))
            {
                throw new FormatException("eco_basket_cycle no válido");
            }

            return cycle;
        }

        private static int Int(RcJson node)
        {
            if (!node.IsInteger || node.Number < int.MinValue || node.Number > int.MaxValue)
            {
                throw new FormatException("se esperaba un entero");
            }

            return (int)node.Number;
        }

        private sealed class Entry<T>
        {
            private readonly Action<T, RcSnapshot> _apply;

            public Entry(string key, Action<T, RcSnapshot> apply)
            {
                Key = key;
                _apply = apply;
            }

            public string Key { get; }

            public void Apply(T config, RcSnapshot rc) => _apply(config, rc);
        }
    }

    /// <summary>El resultado de <see cref="RcConfigBinder.Bind(RcSnapshot, string)"/>.</summary>
    public sealed class RcConfigs
    {
        internal RcConfigs(
            RcSnapshot values, BizGates gates, EconomyConfig economy, BoosterConfig boosters, AdsConfig ads, StoreConfig store,
            OffersConfig offers, DailyChallengeConfig daily, StreakConfig streak, BasketConfig basket, ClockSecurityConfig clock,
            IReadOnlyList<string> errors)
        {
            Values = values;
            Gates = gates;
            Economy = economy;
            Boosters = boosters;
            Ads = ads;
            Store = store;
            Offers = offers;
            Daily = daily;
            Streak = streak;
            Basket = basket;
            ClockSecurity = clock;
            Errors = errors;
        }

        /// <summary>Los valores congelados de los que salen.</summary>
        public RcSnapshot Values { get; }

        /// <summary>Las compuertas de negocio.</summary>
        public BizGates Gates { get; }

        /// <summary>Cifras de la cartera.</summary>
        public EconomyConfig Economy { get; }

        /// <summary>Cifras de los comodines.</summary>
        public BoosterConfig Boosters { get; }

        /// <summary>Cifras de los anuncios (con los kill switches combinados).</summary>
        public AdsConfig Ads { get; }

        /// <summary>Cifras de la tienda (con <c>biz_iap_enabled</c> combinado).</summary>
        public StoreConfig Store { get; }

        /// <summary>Cifras de las ofertas contextuales y del Cántaro (B5).</summary>
        public OffersConfig Offers { get; }

        /// <summary>Cifras del Reto del río.</summary>
        public DailyChallengeConfig Daily { get; }

        /// <summary>Cifras de la Crecida.</summary>
        public StreakConfig Streak { get; }

        /// <summary>Cifras de la Canasta.</summary>
        public BasketConfig Basket { get; }

        /// <summary>Anti-reloj.</summary>
        public ClockSecurityConfig ClockSecurity { get; }

        /// <summary>Consentimiento y edad (B12).</summary>
        public ConsentConfig Consent { get; internal set; } = ConsentConfig.Defaults;

        /// <summary>Lo que no se pudo leer (clave: motivo), en orden; vacío si todo vino bien.</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>Cola de verificación de compras (<c>sec_purchase_deferred_ack_h</c>; F3-5).</summary>
        public VerificationQueueConfig Verification { get; internal set; } = VerificationQueueConfig.Defaults;

        /// <summary>Revocación local del SSV (<c>sec_ssv_grace_h</c>; F3-5).</summary>
        public RevocationConfig Revocation { get; internal set; } = RevocationConfig.Defaults;

        /// <summary>Cola de takeovers (<c>ui_max_takeovers_per_session</c>; B4).</summary>
        public TakeoverConfig Takeovers { get; internal set; } = TakeoverConfig.Defaults;

        /// <summary>Bitácora del río (B4).</summary>
        public LoginCalendarConfig LoginCalendar { get; internal set; } = LoginCalendarConfig.Defaults;

        /// <summary>Liga del río de la 1.0 (D-046): interruptor, desbloqueo y vigencia de la caché.</summary>
        public LeagueConfig League { get; internal set; } = LeagueConfig.Defaults;

        /// <summary><c>drag_pour_enabled</c> (C10, el arrastre; 08 §9; B11; <see cref="RcGameplayBinder"/>).</summary>
        public bool DragPourEnabled { get; internal set; } = AF.GameFlow.DragPour.EnabledDefault;

        /// <summary><c>lvl_label_override</c> ya leída (08 §9; B11; <see cref="RcGameplayBinder"/>): la aplica el catálogo de niveles.</summary>
        public AF.Levels.LevelLabelOverrides LevelLabels { get; internal set; } = AF.Levels.LevelLabelOverrides.Empty;

        /// <summary><c>biz_backend_enabled</c> de estos valores (<see cref="RcBackendSwitch"/>; B7 y F3-5).</summary>
        public IBackendSwitch BackendSwitch { get; internal set; } = new AF.IAP.BackendSwitch();

        /// <summary>Accesibilidad: <c>ui_reduce_motion_default</c> y <c>anim_reduce_motion_factor</c> (B9).</summary>
        public A11yConfig A11y { get; internal set; } = A11yConfig.Defaults;
    }

    /// <summary>
    /// <see cref="IBackendSwitch"/> desde Remote Config (B7; D-033, añadido de B7: «basta un adaptador en <c>AF.RemoteConfig.Binding</c>
    /// que lea <c>biz_backend_enabled</c> del <c>RcSnapshot</c>»): el interruptor de corte del backend (08 §9, <c>bool</c>, <c>true</c>
    /// [P]) con los valores congelados de la sesión. Si la clave no se puede leer, el default compilado
    /// (<see cref="AF.IAP.BackendSwitch.Default"/>).
    /// </summary>
    public sealed class RcBackendSwitch : IBackendSwitch
    {
        private readonly bool _enabled;

        /// <summary>Lee <c>biz_backend_enabled</c> de <paramref name="rc"/>.</summary>
        public RcBackendSwitch(RcSnapshot rc)
        {
            if (rc == null)
            {
                throw new ArgumentNullException(nameof(rc));
            }

            _enabled = rc.TryGet(AF.IAP.BackendSwitch.Key, out bool on) ? on : AF.IAP.BackendSwitch.Default;
        }

        /// <inheritdoc />
        public bool BizBackendEnabled => _enabled;
    }
}
