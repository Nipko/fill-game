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
    /// equivocado es un archivo corrupto (<see cref="FormatException"/>).
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
            root.Set("daily", daily);

            JsonNode progress = JsonNode.NewObject();
            progress.Set("max_level", JsonNode.FromInt(p.Progress.MaxLevel));
            progress.Set("next_level", JsonNode.FromInt(p.Progress.NextLevel));
            progress.Set("since_last_skip", JsonNode.FromInt(p.Progress.SinceLastSkip));
            progress.Set("skipped_levels", IntList(p.Progress.SkippedLevels));
            progress.Set("content_version", JsonNode.FromString(p.Progress.ContentVersion));
            root.Set("progress", progress);

            root.Set("level_state", p.LevelState == null ? JsonNode.Null : LevelStateNode(p.LevelState));

            JsonNode entitlements = JsonNode.NewObject();
            entitlements.Set("no_ads", JsonNode.FromBool(p.Entitlements.NoAds));
            entitlements.Set("starter_bought", JsonNode.FromBool(p.Entitlements.StarterBought));
            entitlements.Set("skins", StringList(p.Entitlements.Skins));
            root.Set("entitlements", entitlements);

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
            }

            JsonNode? levelState = Section(root, "level_state");
            p.LevelState = levelState == null ? null : ReadLevelState(levelState);

            JsonNode? entitlements = Section(root, "entitlements");
            if (entitlements != null)
            {
                p.Entitlements.NoAds = ReadBool(entitlements, "no_ads", p.Entitlements.NoAds);
                p.Entitlements.StarterBought = ReadBool(entitlements, "starter_bought", p.Entitlements.StarterBought);
                ReadStringList(entitlements, "skins", p.Entitlements.Skins);
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

            return p;
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
