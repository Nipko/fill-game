#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Save.Json;

namespace AF.Save
{
    /// <summary>
    /// Conversión entre <see cref="Profile"/> y su JSON (GDD 08 §4.3), con los nombres de clave del GDD, orden de claves
    /// estable, InvariantCulture y horas UTC en ISO 8601 (<c>2026-10-05T14:03:11Z</c>, con fracción solo si la hay:
    /// la ida y vuelta conserva los ticks). Escribe siempre <c>profile_schema</c> = <see cref="Profile.CurrentSchema"/>.
    /// Al leer, una clave que falta toma el default de <see cref="Profile"/> y una desconocida se ignora; un tipo
    /// equivocado es un archivo corrupto (<see cref="FormatException"/>), salvo en lo que no es progreso ni economía:
    /// <c>settings</c>, <c>sloth</c> y <c>progress.map_seen_level</c> toman su default ante un valor ausente o inválido
    /// (un ajuste nunca hace corrupto un perfil), y <c>map_seen_level</c> queda recortado a [0, <c>max_level</c>]; igual
    /// <c>progress.undo_tip_seen</c> (DC23), que sin la clave o con otro tipo queda en «no visto». <c>fauna</c>
    /// (esquema 2; DC15) también es tolerante: una entrada de <c>sighted</c> que no es válida se descarta. <c>streak</c> y
    /// <c>daily_challenge</c> se omiten mientras son null, y <c>daily.streak_hits</c> y <c>daily.streak_seeds</c> en su default
    /// (vacía y 0): son economía y un tipo equivocado en ellos es corrupción. Los de B14 son cosméticos y tolerantes:
    /// <c>entitlements.skin_equipped</c> y <c>entitlements.frames</c> se omiten en su default (null y vacía), y <c>album</c>
    /// (solo <c>fruits</c>), sin tintes; un valor que no vale toma el default o se descarta.
    /// </summary>
    public static class ProfileJson
    {
        /// <summary>Tope del perfil completo: menos de 100 KB (08 §4.3, frente a 3 MB de Saved Games).</summary>
        public const int MaxBytes = 100 * 1024;

        /// <summary>Clave de la versión del esquema.</summary>
        public const string SchemaKey = "profile_schema";

        /// <summary>Texto JSON del perfil (UTF-16; al disco va como UTF-8 sin BOM, todo ASCII).</summary>
        public static string Serialize(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return JsonText.Write(ToNode(profile));
        }

        /// <summary>Lee un perfil del esquema actual (sin migraciones; las aplica <see cref="LocalProfileStore"/>).</summary>
        /// <exception cref="FormatException">JSON inválido, tipos equivocados o <c>profile_schema</c> distinto del actual.</exception>
        public static Profile Deserialize(string json)
        {
            JsonNode root = JsonText.Parse(json ?? throw new ArgumentNullException(nameof(json)));
            int schema = ReadSchema(root);
            if (schema != Profile.CurrentSchema)
            {
                throw new FormatException("profile_schema " + schema.ToString(CultureInfo.InvariantCulture)
                    + " no es el de esta build (" + Profile.CurrentSchema.ToString(CultureInfo.InvariantCulture) + ").");
            }

            return FromNode(root);
        }

        /// <summary>Lee <c>profile_schema</c> de la raíz.</summary>
        /// <exception cref="FormatException">La raíz no es un objeto o falta la versión (o no es un entero ≥ 0).</exception>
        internal static int ReadSchema(JsonNode root)
        {
            if (root.Kind != JsonKind.Object)
            {
                throw new FormatException("La raíz del perfil no es un objeto JSON.");
            }

            JsonNode? node = root.Get(SchemaKey);
            if (node == null)
            {
                throw new FormatException("Falta profile_schema.");
            }

            int schema = node.AsInt();
            if (schema < 0)
            {
                throw new FormatException("profile_schema negativo.");
            }

            return schema;
        }

        internal static JsonNode ToNode(Profile p)
        {
            JsonNode root = JsonNode.NewObject();
            root.Set(SchemaKey, JsonNode.FromInt(Profile.CurrentSchema));
            root.Set("created_at", Timestamp(p.CreatedAt));
            root.Set("economy_ts", Timestamp(p.EconomyTs));

            JsonNode canoes = JsonNode.NewObject();
            canoes.Set("count", JsonNode.FromInt(p.Wallet.Canoes.Count));
            canoes.Set("next_regen_at", Timestamp(p.Wallet.Canoes.NextRegenAt));
            canoes.Set("infinite_until", Timestamp(p.Wallet.Canoes.InfiniteUntil));
            JsonNode wallet = JsonNode.NewObject();
            wallet.Set("seeds", JsonNode.FromInt(p.Wallet.Seeds));
            wallet.Set("canoes", canoes);
            root.Set("wallet", wallet);

            JsonNode stock = JsonNode.NewObject();
            stock.Set("undo", JsonNode.FromInt(p.Stock.Undo));
            stock.Set("hint", JsonNode.FromInt(p.Stock.Hint));
            stock.Set("reveal", JsonNode.FromInt(p.Stock.Reveal));
            stock.Set("extra_cuia", JsonNode.FromInt(p.Stock.ExtraCuia));
            stock.Set("shuffle", JsonNode.FromInt(p.Stock.Shuffle));
            root.Set("stock", stock);

            DailyData d = p.Daily;
            JsonNode daily = JsonNode.NewObject();
            daily.Set("date_local", JsonNode.FromString(d.DateLocal));
            daily.Set("rv_undo_refill", JsonNode.FromInt(d.RvUndoRefill));
            daily.Set("rv_hint", JsonNode.FromInt(d.RvHint));
            daily.Set("rv_reveal", JsonNode.FromInt(d.RvReveal));
            daily.Set("rv_extra_canoe", JsonNode.FromInt(d.RvExtraCanoe));
            daily.Set("rv_double_harvest", JsonNode.FromInt(d.RvDoubleHarvest));
            daily.Set("rv_total", JsonNode.FromInt(d.RvTotal));
            daily.Set("interstitials_today", JsonNode.FromInt(d.InterstitialsToday));
            daily.Set("rescues_today", JsonNode.FromInt(d.RescuesToday));
            daily.Set("af_rescue_shown", JsonNode.FromInt(d.AfRescueShown));
            daily.Set("af_rescue_declined_days", JsonNode.FromInt(d.AfRescueDeclinedDays));
            daily.Set("af_rescue_hidden_until", Timestamp(d.AfRescueHiddenUntil));
            daily.Set("shuffles_today", JsonNode.FromInt(d.ShufflesToday));
            daily.Set("shield_available", JsonNode.FromBool(d.ShieldAvailable));
            if (d.StreakHits.Count > 0)
            {
                daily.Set("streak_hits", IntList(d.StreakHits)); // Crecida (Q-M9 (a)): la clave falta en su default
            }

            if (d.StreakSeeds != 0)
            {
                daily.Set("streak_seeds", JsonNode.FromInt(d.StreakSeeds));
            }

            root.Set("daily", daily);

            JsonNode progress = JsonNode.NewObject();
            progress.Set("max_level", JsonNode.FromInt(p.Progress.MaxLevel));
            progress.Set("next_level", JsonNode.FromInt(p.Progress.NextLevel));
            progress.Set("since_last_skip", JsonNode.FromInt(p.Progress.SinceLastSkip));
            progress.Set("skipped_levels", IntList(p.Progress.SkippedLevels));
            progress.Set("content_version", JsonNode.FromString(p.Progress.ContentVersion));
            progress.Set("map_seen_level", JsonNode.FromInt(p.Progress.ClampedMapSeenLevel));
            progress.Set("undo_tip_seen", JsonNode.FromBool(p.Progress.UndoTipSeen));
            root.Set("progress", progress);

            if (p.Streak != null)
            {
                // Crecida (Q-M9 (a)): tras progress, como en 08 §4.3; la clave falta hasta el primer guardado.
                JsonNode streak = JsonNode.NewObject();
                streak.Set("wins", JsonNode.FromInt(p.Streak.Wins));
                streak.Set("river_full", JsonNode.FromBool(p.Streak.RiverFull));
                root.Set("streak", streak);
            }

            root.Set("level_state", p.LevelState == null ? JsonNode.Null : LevelStateNode(p.LevelState));

            JsonNode entitlements = JsonNode.NewObject();
            entitlements.Set("no_ads", JsonNode.FromBool(p.Entitlements.NoAds));
            entitlements.Set("starter_bought", JsonNode.FromBool(p.Entitlements.StarterBought));
            entitlements.Set("skins", StringList(p.Entitlements.Skins));
            if (p.Entitlements.SkinEquipped != null)
            {
                entitlements.Set("skin_equipped", JsonNode.FromString(p.Entitlements.SkinEquipped)); // B14 [P]: falta con la base
            }

            if (p.Entitlements.Frames.Count > 0)
            {
                entitlements.Set("frames", StringList(p.Entitlements.Frames)); // Q-M6 [P]: falta sin marcos
            }

            root.Set("entitlements", entitlements);

            JsonNode settings = JsonNode.NewObject();
            settings.Set("sound", JsonNode.FromBool(p.Settings.Sound));
            settings.Set("music", JsonNode.FromBool(p.Settings.Music));
            settings.Set("haptics", JsonNode.FromBool(p.Settings.Haptics));
            settings.Set("patterns", JsonNode.FromString(SettingsData.PatternsText(p.Settings.Patterns)));
            settings.Set("language", JsonNode.FromString(p.Settings.Language));
            settings.Set("sloth_messages", JsonNode.FromBool(p.Settings.SlothMessages));
            settings.Set("idle_hint", JsonNode.FromBool(p.Settings.IdleHint));
            root.Set("settings", settings);

            JsonNode lastLine = JsonNode.NewArray();
            for (int i = 0; i < SlothData.MomentCount; i++)
            {
                lastLine.Add(JsonNode.FromInt(p.Sloth.LastLine[i]));
            }

            JsonNode sloth = JsonNode.NewObject();
            sloth.Set("last_line", lastLine);
            sloth.Set("last_greeting_date", JsonNode.FromString(p.Sloth.LastGreetingDate));
            sloth.Set("last_seen_date", JsonNode.FromString(p.Sloth.LastSeenDate));
            root.Set("sloth", sloth);

            JsonNode ads = JsonNode.NewObject();
            ads.Set("last_iap", Timestamp(p.Ads.LastIap));
            ads.Set("levels_since_interstitial", JsonNode.FromInt(p.Ads.LevelsSinceInterstitial));
            root.Set("ads", ads);

            JsonNode stats = JsonNode.NewObject();
            stats.Set("levels_won", JsonNode.FromInt(p.Stats.LevelsWon));
            stats.Set("fails_hard", JsonNode.FromInt(p.Stats.FailsHard));
            stats.Set("rescues", JsonNode.FromInt(p.Stats.Rescues));
            stats.Set("rv_watched", JsonNode.FromInt(p.Stats.RvWatched));
            stats.Set("sessions", JsonNode.FromInt(p.Stats.Sessions));
            root.Set("stats", stats);

            JsonNode sighted = JsonNode.NewArray();
            IReadOnlyList<FaunaSighting> seen = p.Fauna.Sighted;
            for (int i = 0; i < seen.Count; i++)
            {
                JsonNode entry = JsonNode.NewObject();
                entry.Set("species_id", JsonNode.FromString(seen[i].SpeciesId));
                entry.Set("level", JsonNode.FromInt(seen[i].Level));
                sighted.Add(entry);
            }

            JsonNode fauna = JsonNode.NewObject();
            fauna.Set("sighted", sighted);
            root.Set("fauna", fauna);

            if (p.Album.Fruits.Count > 0)
            {
                // album.fruits (08 §4.3; B14): la clave falta hasta el primer tinte descubierto.
                JsonNode fruits = JsonNode.NewArray();
                for (int i = 0; i < p.Album.Fruits.Count; i++)
                {
                    fruits.Add(JsonNode.FromInt(p.Album.Fruits[i]));
                }

                JsonNode album = JsonNode.NewObject();
                album.Set("fruits", fruits);
                root.Set("album", album);
            }

            if (p.DailyChallenge != null)
            {
                root.Set("daily_challenge", DailyChallengeNode(p.DailyChallenge)); // E4: la clave falta hasta el primer guardado del Reto
            }

            return root;
        }

        /// <summary>Perfil desde un documento ya en el esquema actual.</summary>
        /// <exception cref="FormatException">Un tipo no es el esperado.</exception>
        internal static Profile FromNode(JsonNode root)
        {
            var p = new Profile();
            p.CreatedAt = ReadTimestamp(root, "created_at") ?? default(DateTime);
            p.EconomyTs = ReadTimestamp(root, "economy_ts");

            JsonNode? wallet = Section(root, "wallet");
            if (wallet != null)
            {
                p.Wallet.Seeds = ReadInt(wallet, "seeds", p.Wallet.Seeds);
                JsonNode? canoes = Section(wallet, "canoes");
                if (canoes != null)
                {
                    p.Wallet.Canoes.Count = ReadInt(canoes, "count", p.Wallet.Canoes.Count);
                    p.Wallet.Canoes.NextRegenAt = ReadTimestamp(canoes, "next_regen_at");
                    p.Wallet.Canoes.InfiniteUntil = ReadTimestamp(canoes, "infinite_until");
                }
            }

            JsonNode? stock = Section(root, "stock");
            if (stock != null)
            {
                StockData s = p.Stock;
                s.Undo = ReadInt(stock, "undo", s.Undo);
                s.Hint = ReadInt(stock, "hint", s.Hint);
                s.Reveal = ReadInt(stock, "reveal", s.Reveal);
                s.ExtraCuia = ReadInt(stock, "extra_cuia", s.ExtraCuia);
                s.Shuffle = ReadInt(stock, "shuffle", s.Shuffle);
            }

            JsonNode? daily = Section(root, "daily");
            if (daily != null)
            {
                DailyData d = p.Daily;
                d.DateLocal = ReadString(daily, "date_local", d.DateLocal);
                d.RvUndoRefill = ReadInt(daily, "rv_undo_refill", d.RvUndoRefill);
                d.RvHint = ReadInt(daily, "rv_hint", d.RvHint);
                d.RvReveal = ReadInt(daily, "rv_reveal", d.RvReveal);
                d.RvExtraCanoe = ReadInt(daily, "rv_extra_canoe", d.RvExtraCanoe);
                d.RvDoubleHarvest = ReadInt(daily, "rv_double_harvest", d.RvDoubleHarvest);
                d.RvTotal = ReadInt(daily, "rv_total", d.RvTotal);
                d.InterstitialsToday = ReadInt(daily, "interstitials_today", d.InterstitialsToday);
                d.RescuesToday = ReadInt(daily, "rescues_today", d.RescuesToday);
                d.AfRescueShown = ReadInt(daily, "af_rescue_shown", d.AfRescueShown);
                d.AfRescueDeclinedDays = ReadInt(daily, "af_rescue_declined_days", d.AfRescueDeclinedDays);
                d.AfRescueHiddenUntil = ReadTimestamp(daily, "af_rescue_hidden_until");
                d.ShufflesToday = ReadInt(daily, "shuffles_today", d.ShufflesToday);
                d.ShieldAvailable = ReadBool(daily, "shield_available", d.ShieldAvailable);
                // Como el resto de listas y enteros del perfil: "streak_hits": null se lee como vacía y "streak_seeds": null es
                // corrupción (ReadIntList y ReadInt). Los rangos (hitos 3/6/10, semillas >= 0) los recorta AF.Economy.
                ReadIntList(daily, "streak_hits", d.StreakHits);
                d.StreakSeeds = ReadInt(daily, "streak_seeds", d.StreakSeeds);
            }

            JsonNode? progress = Section(root, "progress");
            if (progress != null)
            {
                ProgressData g = p.Progress;
                g.MaxLevel = ReadInt(progress, "max_level", g.MaxLevel);
                g.NextLevel = ReadInt(progress, "next_level", g.NextLevel);
                g.SinceLastSkip = ReadInt(progress, "since_last_skip", g.SinceLastSkip);
                ReadIntList(progress, "skipped_levels", g.SkippedLevels);
                g.ContentVersion = ReadString(progress, "content_version", g.ContentVersion);
                g.MapSeenLevel = TolerantInt(progress, "map_seen_level", g.MapSeenLevel);
                g.MapSeenLevel = g.ClampedMapSeenLevel;
                g.UndoTipSeen = TolerantBool(progress, "undo_tip_seen", g.UndoTipSeen);
            }

            // streak (Crecida; Q-M9 (a)): es economía (sus hitos pagan), así que un tipo equivocado es corrupción, como en wallet.
            // Los rangos (wins 0-10) no se validan aquí: los interpreta AF.Economy, igual que daily_challenge.
            JsonNode? streakNode = Section(root, "streak");
            if (streakNode != null)
            {
                var streak = new StreakData();
                streak.Wins = ReadInt(streakNode, "wins", streak.Wins);
                streak.RiverFull = ReadBool(streakNode, "river_full", streak.RiverFull);
                p.Streak = streak;
            }

            JsonNode? levelState = Section(root, "level_state");
            p.LevelState = levelState == null ? null : ReadLevelState(levelState);

            JsonNode? entitlements = Section(root, "entitlements");
            if (entitlements != null)
            {
                p.Entitlements.NoAds = ReadBool(entitlements, "no_ads", p.Entitlements.NoAds);
                p.Entitlements.StarterBought = ReadBool(entitlements, "starter_bought", p.Entitlements.StarterBought);
                ReadStringList(entitlements, "skins", p.Entitlements.Skins);
                // skin_equipped y frames (B14 [P]) son cosméticos: un tipo equivocado vale su default, sin corrupción.
                p.Entitlements.SkinEquipped = TolerantString(entitlements, "skin_equipped");
                TolerantStringList(entitlements, "frames", p.Entitlements.Frames);
            }

            JsonNode? settings = TolerantSection(root, "settings");
            if (settings != null)
            {
                ReadSettings(settings, p.Settings);
            }

            JsonNode? sloth = TolerantSection(root, "sloth");
            if (sloth != null)
            {
                ReadSloth(sloth, p.Sloth);
            }

            JsonNode? ads = Section(root, "ads");
            if (ads != null)
            {
                p.Ads.LastIap = ReadTimestamp(ads, "last_iap");
                p.Ads.LevelsSinceInterstitial = ReadInt(ads, "levels_since_interstitial", p.Ads.LevelsSinceInterstitial);
            }

            JsonNode? stats = Section(root, "stats");
            if (stats != null)
            {
                StatsData t = p.Stats;
                t.LevelsWon = ReadInt(stats, "levels_won", t.LevelsWon);
                t.FailsHard = ReadInt(stats, "fails_hard", t.FailsHard);
                t.Rescues = ReadInt(stats, "rescues", t.Rescues);
                t.RvWatched = ReadInt(stats, "rv_watched", t.RvWatched);
                t.Sessions = ReadInt(stats, "sessions", t.Sessions);
            }

            JsonNode? fauna = TolerantSection(root, "fauna");
            if (fauna != null)
            {
                ReadFauna(fauna, p.Fauna);
            }

            JsonNode? album = TolerantSection(root, "album");
            JsonNode? albumFruits = album?.Get("fruits");
            if (albumFruits != null && albumFruits.Kind == JsonKind.Array)
            {
                // album.fruits (B14): un valor que no es un tinte de 1-14 se descarta; los repetidos, una vez y en orden.
                IReadOnlyList<JsonNode> items = albumFruits.Items;
                for (int i = 0; i < items.Count; i++)
                {
                    if (TryInt(items[i], out int tint) && AlbumData.IsTint(tint))
                    {
                        p.Album.AddFruit(tint);
                    }
                }
            }

            JsonNode? dailyChallenge = Section(root, "daily_challenge");
            p.DailyChallenge = dailyChallenge == null ? null : ReadDailyChallenge(dailyChallenge);
            return p;
        }

        // fauna.sighted (esquema 2; DC15): una entrada que no es un objeto {species_id, level} válido se descarta; si una especie
        // se repite, gana su nivel más bajo; el orden ordinal lo pone FaunaData.
        private static void ReadFauna(JsonNode node, FaunaData fauna)
        {
            JsonNode? sighted = node.Get("sighted");
            if (sighted == null || sighted.Kind != JsonKind.Array)
            {
                return;
            }

            IReadOnlyList<JsonNode> items = sighted.Items;
            for (int i = 0; i < items.Count; i++)
            {
                JsonNode item = items[i];
                if (item.Kind != JsonKind.Object)
                {
                    continue;
                }

                string? id = TolerantString(item, "species_id");
                JsonNode? levelNode = item.Get("level");
                if (FaunaData.IsSpeciesId(id) && levelNode != null && TryInt(levelNode, out int level) && level >= 1)
                {
                    fauna.Merge(new FaunaSighting(id!, level));
                }
            }
        }

        private static JsonNode LevelStateNode(LevelStateData s)
        {
            JsonNode node = JsonNode.NewObject();
            node.Set("level", JsonNode.FromInt(s.Level));
            node.Set("seed", JsonNode.FromString(s.Seed));
            node.Set("variant", JsonNode.FromString(s.Variant));
            node.Set("fails_level", JsonNode.FromInt(s.FailsLevel));
            node.Set("pours", JsonNode.FromInt(s.Pours));
            node.Set("extra_cuias", JsonNode.FromInt(s.ExtraCuias));
            node.Set("rescues_level", JsonNode.FromInt(s.RescuesLevel));
            node.Set("undo_free", JsonNode.FromInt(s.UndoFree));
            node.Set("revealed_cuias", IntList(s.RevealedCuias));
            node.Set("history", JsonNode.FromString(s.History));
            node.Set("stuck", JsonNode.FromBool(s.Stuck));
            return node;
        }

        private static LevelStateData ReadLevelState(JsonNode node)
        {
            var s = new LevelStateData();
            s.Level = ReadInt(node, "level", s.Level);
            s.Seed = ReadString(node, "seed", s.Seed) ?? throw new FormatException("level_state.seed no puede ser null.");
            s.Variant = ReadString(node, "variant", s.Variant) ?? throw new FormatException("level_state.variant no puede ser null.");
            s.FailsLevel = ReadInt(node, "fails_level", s.FailsLevel);
            s.Pours = ReadInt(node, "pours", s.Pours);
            s.ExtraCuias = ReadInt(node, "extra_cuias", s.ExtraCuias);
            s.RescuesLevel = ReadInt(node, "rescues_level", s.RescuesLevel);
            s.UndoFree = ReadInt(node, "undo_free", s.UndoFree);
            ReadIntList(node, "revealed_cuias", s.RevealedCuias);
            s.History = ReadString(node, "history", s.History) ?? throw new FormatException("level_state.history no puede ser null.");
            s.Stuck = ReadBool(node, "stuck", s.Stuck);
            return s;
        }

        // daily_challenge (E4; nota del Reto §10). Es economía (last_claimed_date impide cobrar dos veces la misma fecha): un tipo
        // equivocado es corrupción, como en wallet, y también una entrada de pending u offline_claims sin date o una in_progress
        // sin date o sin id. Los rangos no se validan aquí: streak_days ≥ 0, stamps.days en 1-31, el formato yyyy-MM de
        // stamps.month y yyyy-MM-dd de las fechas los interpreta AF.LiveOps (DailyChallengeRecord.IsStamped ignora un mes o un
        // día que no casan; una fecha mal formada nunca coincide con la de hoy), igual que level_state no valida su semilla.
        private static JsonNode DailyChallengeNode(DailyChallengeData d)
        {
            JsonNode node = JsonNode.NewObject();
            node.Set("last_date", JsonNode.FromString(d.LastDate));
            node.Set("result", JsonNode.FromString(d.Result));
            node.Set("streak_days", JsonNode.FromInt(d.StreakDays));
            node.Set("last_claimed_date", JsonNode.FromString(d.LastClaimedDate));
            node.Set("pending", ClaimList(d.Pending));
            node.Set("offline_claims", ClaimList(d.OfflineClaims));
            JsonNode stamps = JsonNode.NewObject();
            stamps.Set("month", JsonNode.FromString(d.StampsMonth));
            stamps.Set("days", IntList(d.StampDays));
            node.Set("stamps", stamps);
            if (d.InProgress == null)
            {
                node.Set("in_progress", JsonNode.Null);
            }
            else
            {
                JsonNode play = JsonNode.NewObject();
                play.Set("date", JsonNode.FromString(d.InProgress.Date));
                play.Set("band", JsonNode.FromInt(d.InProgress.Band));
                play.Set("id", JsonNode.FromString(d.InProgress.Id));
                play.Set("level_state", LevelStateNode(d.InProgress.LevelState));
                node.Set("in_progress", play);
            }

            return node;
        }

        private static DailyChallengeData ReadDailyChallenge(JsonNode node)
        {
            var d = new DailyChallengeData();
            d.LastDate = ReadString(node, "last_date", d.LastDate);
            d.Result = ReadString(node, "result", d.Result);
            d.StreakDays = ReadInt(node, "streak_days", d.StreakDays);
            d.LastClaimedDate = ReadString(node, "last_claimed_date", d.LastClaimedDate);
            ReadClaimList(node, "pending", d.Pending);
            ReadClaimList(node, "offline_claims", d.OfflineClaims);
            JsonNode? stamps = Section(node, "stamps");
            if (stamps != null)
            {
                d.StampsMonth = ReadString(stamps, "month", d.StampsMonth);
                ReadIntList(stamps, "days", d.StampDays);
            }

            JsonNode? play = Section(node, "in_progress");
            if (play != null)
            {
                // Sin date o sin id la partida no se puede reanudar ni descartar con su fecha: corrupción, como date en pending.
                var inProgress = new DailyInProgressData();
                inProgress.Date = ReadString(play, "date", null)
                    ?? throw new FormatException("daily_challenge.in_progress.date falta o es null.");
                inProgress.Band = ReadInt(play, "band", inProgress.Band);
                inProgress.Id = ReadString(play, "id", null)
                    ?? throw new FormatException("daily_challenge.in_progress.id falta o es null.");
                JsonNode? state = Section(play, "level_state");
                inProgress.LevelState = state == null ? new LevelStateData() : ReadLevelState(state);
                d.InProgress = inProgress;
            }

            return d;
        }

        private static JsonNode ClaimList(List<DailyClaimData> claims)
        {
            JsonNode array = JsonNode.NewArray();
            for (int i = 0; i < claims.Count; i++)
            {
                JsonNode claim = JsonNode.NewObject();
                claim.Set("date", JsonNode.FromString(claims[i].Date));
                claim.Set("streak", JsonNode.FromInt(claims[i].Streak));
                array.Add(claim);
            }

            return array;
        }

        private static void ReadClaimList(JsonNode parent, string key, List<DailyClaimData> target)
        {
            target.Clear();
            JsonNode? node = parent.Get(key);
            if (node == null || node.IsNull)
            {
                return;
            }

            if (node.Kind != JsonKind.Array)
            {
                throw new FormatException("'" + key + "' debe ser una lista JSON.");
            }

            IReadOnlyList<JsonNode> items = node.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Kind != JsonKind.Object)
                {
                    throw new FormatException("'" + key + "' debe ser una lista de objetos {date, streak}.");
                }

                var claim = new DailyClaimData();
                claim.Date = ReadString(items[i], "date", null) ?? throw new FormatException("'" + key + "': falta date.");
                claim.Streak = ReadInt(items[i], "streak", claim.Streak);
                target.Add(claim);
            }
        }

        // settings y sloth: lo ausente o inválido toma el default, campo a campo.
        private static void ReadSettings(JsonNode node, SettingsData s)
        {
            string? patterns = TolerantString(node, "patterns");
            if (patterns != null && SettingsData.TryParsePatterns(patterns, out PatternsSetting value))
            {
                s.Patterns = value;
            }

            string? language = SettingsData.NormalizeLanguage(TolerantString(node, "language"));
            if (language != null)
            {
                s.Language = language;
            }

            s.Sound = TolerantBool(node, "sound", s.Sound);
            s.Music = TolerantBool(node, "music", s.Music);
            s.Haptics = TolerantBool(node, "haptics", s.Haptics);
            s.SlothMessages = TolerantBool(node, "sloth_messages", s.SlothMessages);
            s.IdleHint = TolerantBool(node, "idle_hint", s.IdleHint);
        }

        private static void ReadSloth(JsonNode node, SlothData s)
        {
            // Lista de otra longitud (otra versión del banco de momentos): se toman los que hay, en orden; un elemento que
            // no es un entero ≥ NoLine vale NoLine.
            JsonNode? lastLine = node.Get("last_line");
            if (lastLine != null && lastLine.Kind == JsonKind.Array)
            {
                IReadOnlyList<JsonNode> items = lastLine.Items;
                for (int i = 0; i < SlothData.MomentCount && i < items.Count; i++)
                {
                    s.LastLine[i] = TryInt(items[i], out int line) && line >= SlothData.NoLine ? line : SlothData.NoLine;
                }
            }

            string? greeting = TolerantString(node, "last_greeting_date");
            if (SlothData.IsDateKey(greeting))
            {
                s.LastGreetingDate = greeting;
            }

            string? seen = TolerantString(node, "last_seen_date");
            if (SlothData.IsDateKey(seen))
            {
                s.LastSeenDate = seen;
            }
        }

        // ---------------------------------------------------------------- horas

        /// <summary>
        /// Hora UTC en ISO 8601 con calendario gregoriano: <c>yyyy-MM-ddTHH:mm:ssZ</c>, con <c>.f…</c> (hasta 7 cifras,
        /// sin ceros finales) solo si hay fracción de segundo. Una hora local se pasa antes a UTC.
        /// </summary>
        public static string FormatTimestamp(DateTime value)
        {
            DateTime utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
            string text = utc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss", CultureInfo.InvariantCulture);
            long fraction = utc.Ticks % TimeSpan.TicksPerSecond;
            if (fraction != 0)
            {
                text += "." + fraction.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0');
            }

            return text + "Z";
        }

        /// <summary>Lee una hora escrita por <see cref="FormatTimestamp"/> (<see cref="DateTimeKind.Utc"/>).</summary>
        /// <exception cref="FormatException">No tiene ese formato exacto o la fecha no existe.</exception>
        public static DateTime ParseTimestamp(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            // yyyy-MM-ddTHH:mm:ss[.f{1,7}]Z
            if (text.Length < 20 || text[4] != '-' || text[7] != '-' || text[10] != 'T' || text[13] != ':' || text[16] != ':'
                || text[text.Length - 1] != 'Z')
            {
                throw BadTimestamp(text);
            }

            long fractionTicks = 0;
            int end = text.Length - 1;
            if (end > 19)
            {
                int digits = end - 20;
                if (text[19] != '.' || digits < 1 || digits > 7)
                {
                    throw BadTimestamp(text);
                }

                fractionTicks = Digits(text, 20, digits);
                for (int i = digits; i < 7; i++)
                {
                    fractionTicks *= 10;
                }
            }

            try
            {
                var value = new DateTime(
                    (int)Digits(text, 0, 4), (int)Digits(text, 5, 2), (int)Digits(text, 8, 2),
                    (int)Digits(text, 11, 2), (int)Digits(text, 14, 2), (int)Digits(text, 17, 2), DateTimeKind.Utc);
                return value.AddTicks(fractionTicks);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw BadTimestamp(text);
            }
        }

        private static long Digits(string text, int start, int count)
        {
            long value = 0;
            for (int i = start; i < start + count; i++)
            {
                char c = text[i];
                if (c < '0' || c > '9')
                {
                    throw BadTimestamp(text);
                }

                value = (value * 10) + (c - '0');
            }

            return value;
        }

        private static FormatException BadTimestamp(string text)
        {
            return new FormatException("Hora mal formada: '" + text + "' (se espera yyyy-MM-ddTHH:mm:ss[.fffffff]Z).");
        }

        private static JsonNode Timestamp(DateTime? value)
        {
            return value.HasValue ? JsonNode.FromString(FormatTimestamp(value.Value)) : JsonNode.Null;
        }

        // ---------------------------------------------------------------- lectura tolerante

        private static JsonNode? Section(JsonNode parent, string key)
        {
            JsonNode? node = parent.Get(key);
            if (node == null || node.IsNull)
            {
                return null;
            }

            if (node.Kind != JsonKind.Object)
            {
                throw new FormatException("'" + key + "' debe ser un objeto JSON.");
            }

            return node;
        }

        private static int ReadInt(JsonNode parent, string key, int fallback)
        {
            JsonNode? node = parent.Get(key);
            return node == null ? fallback : Wrap(key, node.AsInt);
        }

        private static bool ReadBool(JsonNode parent, string key, bool fallback)
        {
            JsonNode? node = parent.Get(key);
            return node == null ? fallback : Wrap(key, node.AsBool);
        }

        private static string? ReadString(JsonNode parent, string key, string? fallback)
        {
            JsonNode? node = parent.Get(key);
            if (node == null)
            {
                return fallback;
            }

            return node.IsNull ? null : Wrap(key, node.AsString);
        }

        private static DateTime? ReadTimestamp(JsonNode parent, string key)
        {
            string? text = ReadString(parent, key, null);
            if (text == null)
            {
                return null;
            }

            string value = text;
            return Wrap(key, () => ParseTimestamp(value));
        }

        private static void ReadIntList(JsonNode parent, string key, List<int> target)
        {
            target.Clear();
            JsonNode? node = parent.Get(key);
            if (node == null || node.IsNull)
            {
                return;
            }

            if (node.Kind != JsonKind.Array)
            {
                throw new FormatException("'" + key + "' debe ser una lista JSON.");
            }

            IReadOnlyList<JsonNode> items = node.Items;
            for (int i = 0; i < items.Count; i++)
            {
                target.Add(Wrap(key, items[i].AsInt));
            }
        }

        private static void ReadStringList(JsonNode parent, string key, List<string> target)
        {
            target.Clear();
            JsonNode? node = parent.Get(key);
            if (node == null || node.IsNull)
            {
                return;
            }

            if (node.Kind != JsonKind.Array)
            {
                throw new FormatException("'" + key + "' debe ser una lista JSON.");
            }

            IReadOnlyList<JsonNode> items = node.Items;
            for (int i = 0; i < items.Count; i++)
            {
                target.Add(Wrap(key, items[i].AsString));
            }
        }

        // Lista de textos tolerante: sin la clave, o si no es una lista, vacía; los elementos que no son texto (o repetidos), fuera.
        private static void TolerantStringList(JsonNode parent, string key, List<string> target)
        {
            target.Clear();
            JsonNode? node = parent.Get(key);
            if (node == null || node.Kind != JsonKind.Array)
            {
                return;
            }

            IReadOnlyList<JsonNode> items = node.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Kind == JsonKind.String)
                {
                    string value = items[i].AsString();
                    if (!target.Contains(value))
                    {
                        target.Add(value);
                    }
                }
            }
        }

        /// <summary>Objeto <paramref name="key"/>, o null si falta o no es un objeto (sin error).</summary>
        private static JsonNode? TolerantSection(JsonNode parent, string key)
        {
            JsonNode? node = parent.Get(key);
            return node != null && node.Kind == JsonKind.Object ? node : null;
        }

        private static int TolerantInt(JsonNode parent, string key, int fallback)
        {
            JsonNode? node = parent.Get(key);
            return node != null && TryInt(node, out int value) ? value : fallback;
        }

        private static bool TolerantBool(JsonNode parent, string key, bool fallback)
        {
            JsonNode? node = parent.Get(key);
            return node != null && node.Kind == JsonKind.Bool ? node.AsBool() : fallback;
        }

        private static string? TolerantString(JsonNode parent, string key)
        {
            JsonNode? node = parent.Get(key);
            return node != null && node.Kind == JsonKind.String ? node.AsString() : null;
        }

        private static bool TryInt(JsonNode node, out int value)
        {
            if (node.Kind == JsonKind.Number
                && int.TryParse(node.NumberText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            value = 0;
            return false;
        }

        private static JsonNode StringList(List<string> values)
        {
            JsonNode array = JsonNode.NewArray();
            for (int i = 0; i < values.Count; i++)
            {
                array.Add(JsonNode.FromString(values[i] ?? throw new InvalidOperationException("Una lista de textos del perfil no admite null.")));
            }

            return array;
        }

        private static JsonNode IntList(List<int> values)
        {
            JsonNode array = JsonNode.NewArray();
            for (int i = 0; i < values.Count; i++)
            {
                array.Add(JsonNode.FromInt(values[i]));
            }

            return array;
        }

        private static T Wrap<T>(string key, Func<T> read)
        {
            try
            {
                return read();
            }
            catch (FormatException e)
            {
                throw new FormatException("'" + key + "': " + e.Message, e);
            }
        }
    }
}
