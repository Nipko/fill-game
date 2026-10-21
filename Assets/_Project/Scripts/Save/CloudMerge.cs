#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Save
{
    /// <summary>Quién gana un conflicto con la nube (08 §11).</summary>
    public enum CloudMergeWinner
    {
        /// <summary>El perfil del dispositivo.</summary>
        Local = 0,

        /// <summary>El snapshot de la nube.</summary>
        Cloud = 1,
    }

    /// <summary>Por qué ganó quien ganó (los casos de <see cref="CloudMerge"/>).</summary>
    public enum CloudMergeReason
    {
        /// <summary>Caso 1: no hay perfil en la nube (o no se pudo leer): se queda el local y se sube.</summary>
        NoCloud = 0,

        /// <summary>Caso 2: los dos son el mismo perfil: no hay conflicto.</summary>
        Identical = 1,

        /// <summary>Caso 3: distinto <c>progress.max_level</c>: gana el mayor.</summary>
        MaxLevel = 2,

        /// <summary>Caso 4: mismo <c>max_level</c>: gana el <c>economy_ts</c> más reciente (sin fecha cuenta como la más antigua).</summary>
        EconomyTs = 3,

        /// <summary>Caso 5: mismo <c>max_level</c> y mismo <c>economy_ts</c>: gana el local [P].</summary>
        Tie = 4,

        /// <summary>
        /// Caso 8: uno de los dos está manipulado (su <c>integrity.hmac</c> no cuadra) y el otro no: gana el coherente (08 §10.1: «en
        /// conflicto con la nube gana el blob coherente»), sin nada del otro. Quien llama decide qué es coherente
        /// (<c>ProfileIntegrity.IsCoherent</c>: todo salvo una firma que no cuadra en un perfil sin migrar) [P].
        /// </summary>
        Integrity = 5,
    }

    /// <summary>Valor de <c>af_save_sync.result</c> (08 §8.2) de una fusión.</summary>
    public enum CloudSyncResult
    {
        /// <summary><c>ok</c>: sin conflicto (sin nube o idénticos).</summary>
        Ok = 0,

        /// <summary><c>conflict_local_won</c>.</summary>
        ConflictLocalWon = 1,

        /// <summary><c>conflict_cloud_won</c>.</summary>
        ConflictCloudWon = 2,

        /// <summary><c>merged</c>: no lo usa la fusión de 08 §11 (siempre hay un ganador); queda para la nube de la 1.2 [P].</summary>
        Merged = 3,

        /// <summary><c>failed</c>: la nube no respondió o su blob no se pudo leer; se sigue con el local.</summary>
        Failed = 4,
    }

    /// <summary>Lo que devuelve <see cref="CloudMerge.Resolve"/>.</summary>
    public sealed class CloudMergeResult
    {
        internal CloudMergeResult(Profile merged, CloudMergeWinner winner, CloudMergeReason reason, bool localChanged, bool cloudChanged)
        {
            Merged = merged;
            Winner = winner;
            Reason = reason;
            LocalChanged = localChanged;
            CloudChanged = cloudChanged;
        }

        /// <summary>El perfil fusionado: una copia nueva (ni el local ni el de la nube se tocan).</summary>
        public Profile Merged { get; }

        /// <summary>Quién ganó.</summary>
        public CloudMergeWinner Winner { get; }

        /// <summary>Por qué.</summary>
        public CloudMergeReason Reason { get; }

        /// <summary>El fusionado no es el local: hay que sustituir el perfil del dispositivo y guardarlo.</summary>
        public bool LocalChanged { get; }

        /// <summary>El fusionado no es el de la nube: hay que subirlo.</summary>
        public bool CloudChanged { get; }

        /// <summary><c>af_save_sync.result</c> (08 §11: <c>winner == local ? conflict_local_won : conflict_cloud_won</c>).</summary>
        public CloudSyncResult SyncResult => Reason == CloudMergeReason.NoCloud || Reason == CloudMergeReason.Identical
            ? CloudSyncResult.Ok
            : Winner == CloudMergeWinner.Local ? CloudSyncResult.ConflictLocalWon : CloudSyncResult.ConflictCloudWon;

        /// <summary>Texto de <see cref="SyncResult"/> para la analítica (<c>ok</c>, <c>conflict_local_won</c>…).</summary>
        public string SyncResultId => CloudMerge.ToId(SyncResult);
    }

    /// <summary>
    /// Fusión del perfil local con el de la nube (GDD 08 §11, <c>ResolveConflict</c>, y fila «Conflicto»; 04 §10.2; 08 §10.1;
    /// bloque B8). Pura y determinista: no lee el reloj ni el disco, no itera diccionarios y no toca sus entradas (trabaja sobre
    /// una copia del ganador). Los casos (08 §14.2: «conflictos local/nube (8 casos)»; la lista es [P], nota
    /// <c>docs/dev/nube-integridad.md</c> §2):
    /// <list type="number">
    /// <item><b>Sin nube</b>: el local tal cual (<see cref="CloudMergeReason.NoCloud"/>).</item>
    /// <item><b>Idénticos</b>: nada que hacer (<see cref="CloudMergeReason.Identical"/>).</item>
    /// <item><b>Distinto <c>max_level</c></b>: gana el mayor, con su cartera y su nivel en curso.</item>
    /// <item><b>Mismo <c>max_level</c></b>: gana el <c>economy_ts</c> más reciente.</item>
    /// <item><b>Empate</b> (mismo nivel y misma fecha): gana el local [P].</item>
    /// <item><b>Consumibles</b> (semillas, <c>stock</c>, canoas): siempre los del ganador; <b>nunca se suman</b> (04 §10.2).</item>
    /// <item><b>Campos que se fusionan</b> sobre el ganador: <c>daily</c> con <c>max</c> por clave si es el mismo día (con B1a-Q1:
    /// <c>shield_available</c> solo si los dos lo tienen, <c>streak_hits</c> por unión y <c>streak_seeds</c> por máximo) o, si
    /// no, el del día más reciente [P]; <c>entitlements</c> por «o» (<c>no_ads</c>, <c>starter_bought</c>) y unión
    /// (<c>skins</c>); la fauna por unión (como <c>album</c>); <c>map_seen_level</c> por máximo (D-013);
    /// <c>undo_tip_seen</c> por «o»; <c>skipped_levels</c> por unión (B3, Q-S1); <c>stats</c> por máximo [P]; <c>ads.last_iap</c>
    /// por máximo [P]. Con el parche del perfil de B8, <c>applied_purchases</c> es la lista del ganador (D-033, Q-B8-1; 08 §11) y lo demás de
    /// <c>integrity</c> (<see cref="MergeAddedFields"/>). <c>purchase_queue</c> (F3-3) es solo local: no se compara, no sube y
    /// el fusionado lleva siempre la del dispositivo (<see cref="SharedJson"/>).</item>
    /// <item><b>Integridad</b>: si uno está manipulado (su firma no cuadra) y el otro no, gana el otro, sin unir nada del otro (un blob manipulado no aporta
    /// derechos ni compras) [P].</item>
    /// </list>
    /// Con B6 (Q-B8-11 (a) [P]): <c>stats.last_seen_at</c> y <c>last_win_at</c> por máximo; en <c>settings.push</c>, «o» en los
    /// pre-prompts, máximo en <c>pre_prompt_at</c> y <c>granted</c> del dispositivo. Con B14, <c>entitlements.frames</c> y
    /// <c>album.fruits</c> por unión; con B4 [P], <c>calendar</c> el de más avance y <c>played_date</c> por máximo; con B5
    /// (Q-B5-9 (a) [P]), <c>piggy</c> del ganador y <c>offers</c> con listas unidas, contadores por máximo, la primera vista de la
    /// Mochila más antigua y la última tarjeta Sin anuncios más reciente.
    /// Lo demás (<c>settings</c>, <c>sloth</c>, <c>streak</c>, <c>daily_challenge</c>, <c>level_state</c>, <c>progress</c>
    /// salvo lo dicho) es del ganador: va con su cartera en el mismo blob y así nunca se paga dos veces lo mismo. Tras fusionar, el
    /// cliente llama a <c>restoreLedger</c> (08 §11; aún sin conectar: F3-5) y vuelve a acreditar los tokens que falten.
    /// </summary>
    public static partial class CloudMerge
    {
        /// <summary>
        /// Fusiona <paramref name="local"/> con <paramref name="cloud"/> (null = sin nube). <paramref name="localCoherent"/> y
        /// <paramref name="cloudCoherent"/> dicen si cada uno cuadra con su firma (<c>ProfileIntegrity.Verify</c>); quien llama lo
        /// comprueba antes, porque la firma no es asunto de la fusión.
        /// </summary>
        public static CloudMergeResult Resolve(Profile local, Profile? cloud, bool localCoherent = true, bool cloudCoherent = true)
        {
            if (local == null)
            {
                throw new ArgumentNullException(nameof(local));
            }

            string localJson = SharedJson(local);
            if (cloud == null)
            {
                return new CloudMergeResult(WithLocalQueue(ProfileJson.Deserialize(localJson), local), CloudMergeWinner.Local, CloudMergeReason.NoCloud, false, true);
            }

            string cloudJson = SharedJson(cloud);
            if (string.Equals(localJson, cloudJson, StringComparison.Ordinal))
            {
                return new CloudMergeResult(WithLocalQueue(ProfileJson.Deserialize(localJson), local), CloudMergeWinner.Local, CloudMergeReason.Identical, false, false);
            }

            CloudMergeWinner winner;
            CloudMergeReason reason;
            if (localCoherent != cloudCoherent)
            {
                winner = localCoherent ? CloudMergeWinner.Local : CloudMergeWinner.Cloud;
                reason = CloudMergeReason.Integrity;
            }
            else if (local.Progress.MaxLevel != cloud.Progress.MaxLevel)
            {
                winner = local.Progress.MaxLevel > cloud.Progress.MaxLevel ? CloudMergeWinner.Local : CloudMergeWinner.Cloud;
                reason = CloudMergeReason.MaxLevel;
            }
            else
            {
                int byTs = CompareTs(local.EconomyTs, cloud.EconomyTs);
                if (byTs != 0)
                {
                    winner = byTs > 0 ? CloudMergeWinner.Local : CloudMergeWinner.Cloud;
                    reason = CloudMergeReason.EconomyTs;
                }
                else
                {
                    winner = CloudMergeWinner.Local;
                    reason = CloudMergeReason.Tie;
                }
            }

            Profile winnerProfile = winner == CloudMergeWinner.Local ? local : cloud;
            Profile loser = winner == CloudMergeWinner.Local ? cloud : local;
            Profile merged = ProfileJson.Deserialize(winner == CloudMergeWinner.Local ? localJson : cloudJson);
            merged.AccountCode = local.AccountCode; // F3-5 [P]: el código de borrado es del dispositivo (el otro puede estar invalidado)
            if (reason != CloudMergeReason.Integrity)
            {
                MergeFields(merged, loser);
                MergeAddedFields(merged, winnerProfile, loser);
                MergeDeviceFields(merged, loser, local);
            }

            string mergedJson = ProfileJson.Serialize(merged); // sin purchase_queue: la copia sale de SharedJson
            return new CloudMergeResult(
                WithLocalQueue(merged, local),
                winner,
                reason,
                !string.Equals(mergedJson, localJson, StringComparison.Ordinal),
                !string.Equals(mergedJson, cloudJson, StringComparison.Ordinal));
        }

        /// <summary>
        /// El perfil como se comparte con la nube: sin <c>purchase_queue</c> (F3-3 [P]; D-035, añadido de F3-3: la cola de
        /// verificación es solo del dispositivo, porque sus compras sin terminar las vuelve a dar la tienda de ese teléfono). No
        /// cambia <paramref name="profile"/> (la quita y la devuelve).
        /// </summary>
        public static string SharedJson(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            string? queue = profile.PurchaseQueue;
            profile.PurchaseQueue = null;
            try
            {
                return ProfileJson.Serialize(profile);
            }
            finally
            {
                profile.PurchaseQueue = queue;
            }
        }

        // purchase_queue: siempre la del dispositivo, gane quien gane (D-035, añadido de F3-3).
        private static Profile WithLocalQueue(Profile merged, Profile local)
        {
            merged.PurchaseQueue = local.PurchaseQueue;
            return merged;
        }

        /// <summary>Texto de <c>af_save_sync.result</c> (08 §8.2).</summary>
        public static string ToId(CloudSyncResult result)
        {
            switch (result)
            {
                case CloudSyncResult.Ok: return "ok";
                case CloudSyncResult.ConflictLocalWon: return "conflict_local_won";
                case CloudSyncResult.ConflictCloudWon: return "conflict_cloud_won";
                case CloudSyncResult.Merged: return "merged";
                case CloudSyncResult.Failed: return "failed";
                default: throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }
        }

        /// <summary>
        /// <c>daily</c> de 04 §10.1 y 08 §11 («<c>daily</c> se fusiona con <c>max</c> por clave»: los topes de vídeo y de rescate no se
        /// duplican por dispositivo), sobre <paramref name="merged"/> (copia del ganador) con <paramref name="other"/>:
        /// <list type="bullet">
        /// <item>mismo <c>date_local</c>: cada contador por máximo; <c>shield_available</c> solo si los dos lo tienen (B1a-Q1: un
        /// escudo gastado en un dispositivo no vuelve); <c>streak_hits</c> por unión, en orden; <c>streak_seeds</c> por máximo;</item>
        /// <item>fechas distintas: el bloque del día más reciente entero (los contadores de ayer no cuentan hoy, 04 §10.1) [P];</item>
        /// <item>siempre: <c>af_rescue_declined_days</c> y <c>af_rescue_hidden_until</c> por máximo (no son del día).</item>
        /// </list>
        /// </summary>
        public static void MergeDaily(DailyData merged, DailyData other)
        {
            if (merged == null)
            {
                throw new ArgumentNullException(nameof(merged));
            }

            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            int declined = Math.Max(merged.AfRescueDeclinedDays, other.AfRescueDeclinedDays);
            DateTime? hiddenUntil = MaxTs(merged.AfRescueHiddenUntil, other.AfRescueHiddenUntil);
            int byDate = string.CompareOrdinal(merged.DateLocal ?? string.Empty, other.DateLocal ?? string.Empty);
            if (byDate < 0)
            {
                CopyDay(other, merged);
            }
            else if (byDate == 0)
            {
                merged.RvUndoRefill = Math.Max(merged.RvUndoRefill, other.RvUndoRefill);
                merged.RvHint = Math.Max(merged.RvHint, other.RvHint);
                merged.RvReveal = Math.Max(merged.RvReveal, other.RvReveal);
                merged.RvExtraCanoe = Math.Max(merged.RvExtraCanoe, other.RvExtraCanoe);
                merged.RvDoubleHarvest = Math.Max(merged.RvDoubleHarvest, other.RvDoubleHarvest);
                merged.RvTotal = Math.Max(merged.RvTotal, other.RvTotal);
                merged.InterstitialsToday = Math.Max(merged.InterstitialsToday, other.InterstitialsToday);
                merged.RescuesToday = Math.Max(merged.RescuesToday, other.RescuesToday);
                merged.AfRescueShown = Math.Max(merged.AfRescueShown, other.AfRescueShown);
                merged.ShufflesToday = Math.Max(merged.ShufflesToday, other.ShufflesToday);
                merged.ShieldAvailable = merged.ShieldAvailable && other.ShieldAvailable;
                UnionSorted(merged.StreakHits, other.StreakHits);
                merged.StreakSeeds = Math.Max(merged.StreakSeeds, other.StreakSeeds);
            }

            merged.AfRescueDeclinedDays = declined;
            merged.AfRescueHiddenUntil = hiddenUntil;
        }

        /// <summary>Añade a <paramref name="target"/> los enteros de <paramref name="other"/> que no tiene y lo deja en orden ascendente, sin repetidos.</summary>
        public static void UnionSorted(List<int> target, IReadOnlyList<int> other)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            for (int i = 0; i < other.Count; i++)
            {
                target.Add(other[i]);
            }

            target.Sort();
            int w = 0;
            for (int r = 0; r < target.Count; r++)
            {
                if (w == 0 || target[r] != target[w - 1])
                {
                    target[w++] = target[r];
                }
            }

            target.RemoveRange(w, target.Count - w);
        }

        /// <summary>Añade a <paramref name="target"/> los textos de <paramref name="other"/> que no tiene y lo deja en orden ordinal, sin repetidos.</summary>
        public static void UnionSorted(List<string> target, IReadOnlyList<string> other)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            for (int i = 0; i < other.Count; i++)
            {
                target.Add(other[i]);
            }

            target.Sort(StringComparer.Ordinal);
            int w = 0;
            for (int r = 0; r < target.Count; r++)
            {
                if (w == 0 || !string.Equals(target[r], target[w - 1], StringComparison.Ordinal))
                {
                    target[w++] = target[r];
                }
            }

            target.RemoveRange(w, target.Count - w);
        }

        // Campos que ya tiene el perfil (sin el parche de B8), sobre la copia del ganador.
        private static void MergeFields(Profile merged, Profile other)
        {
            MergeDaily(merged.Daily, other.Daily);

            merged.Progress.MergeMapSeenLevel(other.Progress); // D-013: máximo, recortado al max_level del ganador
            merged.Progress.MergeUndoTipSeen(other.Progress); // DC23: visto si lo vio cualquiera
            UnionSorted(merged.Progress.SkippedLevels, other.Progress.SkippedLevels); // B3, Q-S1: unión

            merged.Entitlements.NoAds = merged.Entitlements.NoAds || other.Entitlements.NoAds;
            merged.Entitlements.StarterBought = merged.Entitlements.StarterBought || other.Entitlements.StarterBought;
            UnionSorted(merged.Entitlements.Skins, other.Entitlements.Skins);

            merged.Fauna.MergeSightings(other.Fauna);

            merged.Stats.LevelsWon = Math.Max(merged.Stats.LevelsWon, other.Stats.LevelsWon);
            merged.Stats.FailsHard = Math.Max(merged.Stats.FailsHard, other.Stats.FailsHard);
            merged.Stats.Rescues = Math.Max(merged.Stats.Rescues, other.Stats.Rescues);
            merged.Stats.RvWatched = Math.Max(merged.Stats.RvWatched, other.Stats.RvWatched);
            merged.Stats.Sessions = Math.Max(merged.Stats.Sessions, other.Stats.Sessions);

            merged.Ads.LastIap = MaxTs(merged.Ads.LastIap, other.Ads.LastIap);
        }

        // Q-B8-11 (a) [P] (con B6): stats.last_seen_at y last_win_at por máximo; settings.push con «o» en los pre-prompts, la
        // fecha del pre-prompt por máximo y granted del dispositivo (el permiso es del teléfono, no de la cuenta).
        private static void MergeDeviceFields(Profile merged, Profile other, Profile local)
        {
            merged.Stats.LastSeenAt = MaxTs(merged.Stats.LastSeenAt, other.Stats.LastSeenAt);
            merged.Stats.LastWinAt = MaxTs(merged.Stats.LastWinAt, other.Stats.LastWinAt);
            PushSettingsData push = merged.Settings.Push;
            push.PrePromptShown = push.PrePromptShown || other.Settings.Push.PrePromptShown;
            push.SecondPromptShown = push.SecondPromptShown || other.Settings.Push.SecondPromptShown;
            push.PrePromptAt = MaxTs(push.PrePromptAt, other.Settings.Push.PrePromptAt);
            push.Granted = local.Settings.Push.Granted;

            // B14: los marcos de país por unión, como las skins (EntitlementsData.MergeOwned), y el álbum de frutas por unión.
            UnionSorted(merged.Entitlements.Frames, other.Entitlements.Frames);
            merged.Album.MergeFruits(other.Album);

            // B4 [P] (nota de B4 §7): calendar, el registro con más avance (cycle y luego next_day; en empate, el del ganador), y
            // played_date por máximo. Así una casilla sellada en un teléfono no se vuelve a sellar en el otro.
            CalendarData? mine = merged.Calendar;
            CalendarData? theirs = other.Calendar;
            if (theirs != null)
            {
                string? played = mine == null ? theirs.PlayedDate : MaxDate(mine.PlayedDate, theirs.PlayedDate);
                if (mine == null || theirs.Cycle > mine.Cycle || (theirs.Cycle == mine.Cycle && theirs.NextDay > mine.NextDay))
                {
                    mine = new CalendarData
                    {
                        Cycle = theirs.Cycle,
                        NextDay = theirs.NextDay,
                        LastClaimDate = theirs.LastClaimDate,
                    };
                    merged.Calendar = mine;
                }

                mine.PlayedDate = played;
            }

            // B5, Q-B5-9 (a) [P]: piggy es del ganador (va con su cartera, como wallet); offers une las listas, toma el máximo de
            // los contadores y, de las fechas, la primera vista de la Mochila más antigua (empieza sus 48 h) y la última tarjeta
            // Sin anuncios más reciente.
            OffersData? offersOther = other.Offers;
            if (offersOther != null)
            {
                OffersData o = merged.Offers ??= new OffersData();
                UnionSorted(o.StarterReminders, offersOther.StarterReminders);
                UnionSorted(o.SkinCards, offersOther.SkinCards);
                o.NoAdsDeclines = Math.Max(o.NoAdsDeclines, offersOther.NoAdsDeclines);
                o.InterstitialsSeen = Math.Max(o.InterstitialsSeen, offersOther.InterstitialsSeen);
                o.NoAdsCardDate = MaxDate(o.NoAdsCardDate, offersOther.NoAdsCardDate);
                o.StarterFirstSeenAt = MinTs(o.StarterFirstSeenAt, offersOther.StarterFirstSeenAt);
            }
        }

        // La más antigua de dos fechas; null no cuenta (si solo hay una, esa).
        private static DateTime? MinTs(DateTime? a, DateTime? b)
        {
            if (!a.HasValue)
            {
                return b;
            }

            if (!b.HasValue)
            {
                return a;
            }

            return a.Value.Ticks <= b.Value.Ticks ? a : b;
        }

        // La fecha local (yyyy-MM-dd) más reciente; null cuenta como la más antigua.
        private static string? MaxDate(string? a, string? b)
        {
            return string.CompareOrdinal(a ?? string.Empty, b ?? string.Empty) >= 0 ? a : b;
        }

        /// <summary>
        /// Fusión de los campos que añade el parche del perfil de B8 (<c>applied_purchases</c>, <c>integrity</c>, <c>locale</c>…).
        /// Sin el parche no hace nada; lo implementa <c>CloudMerge.Fields.cs</c>.
        /// </summary>
        static partial void MergeAddedFields(Profile merged, Profile winner, Profile loser);

        private static void CopyDay(DailyData from, DailyData to)
        {
            to.DateLocal = from.DateLocal;
            to.RvUndoRefill = from.RvUndoRefill;
            to.RvHint = from.RvHint;
            to.RvReveal = from.RvReveal;
            to.RvExtraCanoe = from.RvExtraCanoe;
            to.RvDoubleHarvest = from.RvDoubleHarvest;
            to.RvTotal = from.RvTotal;
            to.InterstitialsToday = from.InterstitialsToday;
            to.RescuesToday = from.RescuesToday;
            to.AfRescueShown = from.AfRescueShown;
            to.ShufflesToday = from.ShufflesToday;
            to.ShieldAvailable = from.ShieldAvailable;
            to.StreakHits.Clear();
            to.StreakHits.AddRange(from.StreakHits);
            to.StreakSeeds = from.StreakSeeds;
        }

        // > 0 si a es más reciente; sin fecha cuenta como la más antigua.
        private static int CompareTs(DateTime? a, DateTime? b)
        {
            if (!a.HasValue)
            {
                return b.HasValue ? -1 : 0;
            }

            if (!b.HasValue)
            {
                return 1;
            }

            return a.Value.Ticks.CompareTo(b.Value.Ticks);
        }

        private static DateTime? MaxTs(DateTime? a, DateTime? b)
        {
            return CompareTs(a, b) >= 0 ? a : b;
        }
    }
}
