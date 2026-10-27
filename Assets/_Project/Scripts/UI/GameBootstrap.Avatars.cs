#nullable enable
using System;
using System.Globalization;
using AF.Analytics;
using AF.Audio;
using AF.Economy;
using AF.Events;
using AF.LiveOps;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Los avatares del jugador en el juego (D-056 y D-057; <c>docs/dev/avatares.md</c>). <see cref="SetUpAvatars"/> (en <c>Start</c>,
    /// tras la Liga) crea:
    /// <list type="bullet">
    /// <item>el servicio (<see cref="AvatarService"/>) sobre <c>avatar</c> del perfil, guardado con el lote: si falta el avatar, uno de
    /// los 6 libres al azar sin preguntar (Q-AV-2), y los hitos de un perfil anterior (<c>retro</c>);</item>
    /// <item>el retrato del inicio arriba a la izquierda y la fila «Avatar» de Ajustes desde el 10 (Q-AV-3), con el punto «nuevo»;</item>
    /// <item>«Tu avatar» (<see cref="AvatarScreen"/>) desde el inicio, Ajustes y la fila del jugador en la Liga, con el atlas perezoso de
    /// los retratos (<see cref="AvatarAtlas"/>) que comparte con la Liga y se suelta al cerrar las dos;</item>
    /// <item>los desbloqueos: al ganar (con la etiqueta del nivel), y cada vez que cambia lo que miran los hitos (la Crecida, el Reto,
    /// los puntos de la Liga), con <c>af_avatar_unlock</c>; la elección, con <c>af_avatar_select</c> y el envío a la Liga
    /// (<see cref="LeagueService.SyncAvatar"/>).</item>
    /// </list>
    /// Con <c>ui_avatars_enabled</c> apagada: ni retrato, ni fila, ni avatar en la Liga ni en la victoria; el perfil conserva el campo y
    /// la Liga deja de enviarlo.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Cada cuánto se vuelven a mirar los hitos y el envío pendiente a la Liga (ms) [P].</summary>
        public const float AvatarCheckIntervalMs = 1000f;

        private AvatarService? _avatars;
        private AvatarConfig _avatarConfig = AvatarConfig.Defaults;
        private AvatarScreen? _avatarScreen;
        private AvatarAtlas? _avatarAtlas;
        private AvatarAtlas? _leagueAvatarAtlas;
        private float _avatarCheckMs;
        private float _avatarSyncMs;
        private long _avatarSignature = long.MinValue;

        /// <summary>Elección del libre por defecto para los tests (recibe 6, devuelve 0-5); por defecto, al azar.</summary>
        internal static Func<int, int>? AvatarPickOverride { get; set; }

        /// <summary>Cifras de los avatares para los tests (ganan a Remote Config).</summary>
        internal static AvatarConfig? AvatarConfigOverride { get; set; }

        /// <summary>Limpia los ganchos de test de los avatares.</summary>
        internal static void ResetAvatarOverrides()
        {
            AvatarPickOverride = null;
            AvatarConfigOverride = null;
        }

        /// <summary>El servicio de los avatares (null antes del arranque).</summary>
        public AvatarService? Avatars => _avatars;

        internal AvatarScreen? AvatarView => _avatarScreen;

        internal AvatarAtlas? AvatarAtlasNow => _avatarAtlas;

        internal AvatarAtlas? LeagueAvatarAtlasNow => _leagueAvatarAtlas;

        /// <summary>«Tu avatar» está abierta.</summary>
        public bool AvatarScreenOpen => _avatarScreen != null && _avatarScreen.Visible;

        /// <summary>Los avatares existen (<c>ui_avatars_enabled</c>).</summary>
        public bool AvatarsEnabled => _avatars != null && _avatarConfig.UiAvatarsEnabled;

        /// <summary>El avatar sale en el inicio y en Ajustes: encendidos y desde el 10.</summary>
        public bool AvatarsShownInHome => AvatarsEnabled && (_profile?.Progress.MaxLevel ?? 0) >= AvatarRules.HomeUnlockLevel;

        private void SetUpAvatars()
        {
            Profile? profile = _profile;
            if (profile == null || _art == null)
            {
                return;
            }

            _avatarConfig = AvatarConfigOverride ?? _rcConfigs?.Avatars ?? AvatarConfig.Defaults;
            IClock clock = _telemetryClock ?? new SystemClock();
            _avatars = new AvatarService(profile.Avatar, () => SaveAvatarProfile(), clock);
            _avatars.Unlocked += OnAvatarUnlocked;
            _avatars.Selected += OnAvatarSelected;
            if (!ProfileLocked)
            {
                using (Saves())
                {
                    Func<int, int> pick = AvatarPickOverride ?? (n => UnityEngine.Random.Range(0, n));
                    if (_avatars.EnsureValid(pick))
                    {
                        AvatarProbe("assign");
                    }

                    _avatars.Evaluate(AvatarProgressNow(false, false), retro: true);
                }
            }

            _avatarScreen = new AvatarScreen(transform, _art, _probe, () => CloseAvatarScreen()) { Tapped = id => ChooseAvatar(id) };
            if (_lobby != null)
            {
                _lobby.AvatarTapped = () => OpenAvatarScreen("home");
            }

            if (_settingsScreen != null)
            {
                _settingsScreen.AvatarRowPressed = () => OpenAvatarScreen("settings");
            }

            _events?.Subscribe<LevelWon>(OnAvatarLevelWon);
            if (_league != null)
            {
                _league.AvatarRejected += OnLeagueAvatarRejected;
            }

            if (_leagueScreen != null)
            {
                _leagueScreen.MeTapped = () => OpenAvatarScreen("league");
            }

            AvatarRunner runner = gameObject.GetComponent<AvatarRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<AvatarRunner>();
            }

            runner.Bind(this);
            _avatarSignature = AvatarSignature();
            SyncLeagueAvatar();
            RefreshAvatarUi();
        }

        private void SaveAvatarProfile()
        {
            Profile? profile = _profile;
            if (profile == null || ProfileLocked || AccountDeleted)
            {
                return;
            }

            _saveBatch?.TrySave(profile);
        }

        /// <summary>Lo que miran los hitos ahora (avatares.md §1.3), con la etiqueta de la victoria si la hay.</summary>
        private AvatarProgress AvatarProgressNow(bool hardWon, bool veryHardWon)
        {
            Profile profile = _profile!;
            DailyChallengeData? daily = profile.DailyChallenge;
            return new AvatarProgress(
                profile.Progress.MaxLevel,
                hardWon,
                veryHardWon,
                profile.Streak?.RiverFull ?? false,
                daily?.StreakDays ?? 0,
                daily?.StampDays.Count ?? 0,
                LeaguePointsForAvatars());
        }

        // Los puntos de la Liga de la última respuesta del servidor (la cola o la clasificación guardada); 0 sin Liga.
        private int LeaguePointsForAvatars()
        {
            LeagueService? league = _league;
            if (league == null)
            {
                return 0;
            }

            LeagueRecord record = league.Record;
            int points = record.MePoints;
            LeagueCacheEntry? global = record.CacheGlobal;
            if (global != null)
            {
                points = Math.Max(points, global.Board.Me.Points);
            }

            return points;
        }

        // Un número que cambia cuando cambia algo que miran los hitos (para no reevaluar en cada fotograma).
        private long AvatarSignature()
        {
            Profile? profile = _profile;
            if (profile == null)
            {
                return 0L;
            }

            DailyChallengeData? daily = profile.DailyChallenge;
            long s = profile.Progress.MaxLevel;
            s = (s * 31) + ((profile.Streak?.RiverFull ?? false) ? 1 : 0);
            s = (s * 31) + (daily?.StreakDays ?? 0);
            s = (s * 31) + (daily?.StampDays.Count ?? 0);
            s = (s * 31) + LeaguePointsForAvatars();
            return s;
        }

        /// <summary>Cada fotograma (lo llama <see cref="AvatarRunner"/>): el atlas, la pantalla, los hitos y el envío pendiente.</summary>
        internal void TickAvatars(float dtMs)
        {
            if (_avatars == null || AccountDeleted)
            {
                return;
            }

            _avatarScreen?.Tick(dtMs);
            AvatarAtlas? atlas = _avatarAtlas;
            if (atlas != null)
            {
                if (!AvatarScreenOpen)
                {
                    atlas.Dispose(); // memoria (2026-10-26): el atlas de la rejilla solo vive con «Tu avatar» abierta
                    _avatarAtlas = null;
                }
                else
                {
                    atlas.Tick();
                }
            }

            AvatarAtlas? leagueAtlas = _leagueAvatarAtlas;
            if (leagueAtlas != null)
            {
                if (!LeagueOpen)
                {
                    leagueAtlas.Dispose(); // y el de las filas de la Liga, solo con la Liga abierta
                    _leagueAvatarAtlas = null;
                    if (_leagueScreen != null)
                    {
                        _leagueScreen.Atlas = null;
                    }
                }
                else if (leagueAtlas.Tick() > 0)
                {
                    _leagueScreen?.RefreshAvatars();
                }
            }

            _avatarCheckMs += dtMs;
            if (_avatarCheckMs >= AvatarCheckIntervalMs)
            {
                _avatarCheckMs = 0f;
                long signature = AvatarSignature();
                if (signature != _avatarSignature)
                {
                    _avatarSignature = signature;
                    EvaluateAvatars(false, false);
                    RefreshAvatarUi(); // max_level llegó al 10: sale el retrato
                }
            }

            _avatarSyncMs += dtMs;
            if (_avatarSyncMs >= RetryIntervalMs)
            {
                _avatarSyncMs = 0f;
                SyncLeagueAvatar();
            }
        }

        private void EvaluateAvatars(bool hardWon, bool veryHardWon)
        {
            AvatarService? avatars = _avatars;
            if (avatars == null || ProfileLocked || AccountDeleted || _profile == null)
            {
                return;
            }

            using (Saves())
            {
                avatars.Evaluate(AvatarProgressNow(hardWon, veryHardWon), retro: false);
            }
        }

        // La victoria de campaña: los hitos con la etiqueta del nivel (Difícil, Muy difícil) y el resto con el perfil de ahora.
        private void OnAvatarLevelWon(LevelWon won)
        {
            RewardTier tier = _flow?.HarvestTier ?? RewardTier.Normal;
            EvaluateAvatars(tier == RewardTier.Hard, tier == RewardTier.VeryHard);
            _avatarSignature = AvatarSignature();
            RefreshAvatarUi();
        }

        private void OnAvatarUnlocked(AvatarUnlock unlock)
        {
            _analytics?.Log(AnalyticsEvent.Create("af_avatar_unlock").Add("avatar", unlock.Avatar.Id)
                .Add("milestone", AvatarCatalog.MilestoneId(unlock.Avatar.Milestone) ?? string.Empty).Add("retro", unlock.Retro)); // 08 §8.2 (D-056)
            AvatarProbe("unlock");
            _avatarAtlas?.Request(unlock.Avatar.Index, unlock.Avatar.Id, unlock.Avatar.Index, AvatarScreen.BackdropOf(unlock.Avatar), AvatarArtBadge.None, false);
            RefreshAvatarUi();
        }

        private void OnAvatarSelected(AvatarInfo avatar, bool free, bool first)
        {
            _analytics?.Log(AnalyticsEvent.Create("af_avatar_select").Add("avatar", avatar.Id).Add("free", free).Add("first", first)); // 08 §8.2 (D-056)
            AvatarProbe("select");
            SyncLeagueAvatar();
        }

        private void OnLeagueAvatarRejected(int index)
        {
            _analytics?.Log(AnalyticsEvent.Create("af_error").Add("module", "league").Add("code", "avatar_rejected").Add("fatal", false)); // avatares.md §4.4
        }

        /// <summary>
        /// El avatar a la Liga (avatares.md §4.4): con los avatares encendidos, el índice elegido queda pendiente si no es el que confirmó
        /// el servidor y sale con la Liga activa. Tras elegir, al arrancar (también tras una fusión con la nube) y cada
        /// <see cref="RetryIntervalMs"/>.
        /// </summary>
        private void SyncLeagueAvatar()
        {
            LeagueService? league = _league;
            AvatarService? avatars = _avatars;
            if (league == null || avatars == null || !AvatarsEnabled || AccountDeleted || ProfileLocked)
            {
                return;
            }

            using (Saves())
            {
                league.SyncAvatar(avatars.Current.Index);
            }
        }

        /// <summary>El retrato elegido a la densidad de la pantalla (112 dp, entre 2 y 4 px/dp; avatares.md §2.2).</summary>
        internal Sprite? AvatarPortraitNow()
        {
            AvatarService? avatars = _avatars;
            if (avatars == null || _art == null)
            {
                return null;
            }

            AvatarInfo current = avatars.Current;
            float density = Mathf.Clamp(_lobby != null ? _lobby.ScaleFactor : ProceduralArt.UiPxPerDp, 2f, 4f);
            int px = Mathf.CeilToInt(AvatarScreen.HeaderPortraitDp * density);
            return _art.AvatarPortrait(current.Id, current.Index, AvatarScreen.BackdropOf(current), px);
        }

        /// <summary>El retrato del inicio, la fila de Ajustes, la línea de la victoria y, si está abierta, «Tu avatar».</summary>
        internal void RefreshAvatarUi()
        {
            AvatarService? avatars = _avatars;
            if (avatars == null)
            {
                return;
            }

            bool shown = AvatarsShownInHome;
            Sprite? portrait = shown || AvatarScreenOpen || (AvatarsEnabled && LeagueActive) ? AvatarPortraitNow() : null;
            int maxLevel = _profile?.Progress.MaxLevel ?? 0;
            bool dot = AvatarRules.DotShown(avatars.Data, maxLevel);
            AvatarInfo current = avatars.Current;
            _lobby?.SetAvatar(shown ? portrait : null, shown, dot, AvatarText.HomeA11y(current, dot));
            _settingsScreen?.SetAvatarRow(shown ? portrait : null, shown, AvatarText.HomeA11y(current, false));
            if (_screen != null)
            {
                _screen.LeagueAvatar = AvatarsEnabled ? portrait : null;
            }

            if (_leagueScreen != null)
            {
                _leagueScreen.MyAvatar = AvatarsEnabled && _leagueScreen.Atlas != null ? portrait : null;
                _leagueScreen.RefreshAvatars(); // la fila del jugador ve el cambio en el acto (avatares.md §3.3)
            }

            if (AvatarScreenOpen && portrait != null && _avatarAtlas != null)
            {
                _avatarScreen!.Refresh(portrait, LeagueEnabledForAvatars, maxLevel, AvatarLeagueNow());
            }

            if (DevProbe.Enabled && _probe != null && _lobby != null && _lobby.Visible && shown)
            {
                _probe.Layout("lobby_avatar", 0, null, new[] { "avatar" }, new[] { _lobby.AvatarRect });
            }
        }

        // La Liga encendida para el selector (el ocelote; Q-AV-8): live_league_enabled (o el extra de desarrollo) y con permiso.
        private bool LeagueEnabledForAvatars => _league != null && _league.Config.LiveLeagueEnabled && LeagueAllowed();

        private AvatarLeagueInfo AvatarLeagueNow()
        {
            LeagueService? league = _league;
            if (league == null || !league.Active)
            {
                return new AvatarLeagueInfo(null, 0, null);
            }

            LeagueRecord record = league.Record;
            string? nameKey = record.MeNameKey ?? league.Cached(LeagueScope.Global)?.Board.Me.NameKey;
            int nameNum = record.MeNameKey != null ? record.MeNameNum : (league.Cached(LeagueScope.Global)?.Board.Me.NameNum ?? 0);
            return new AvatarLeagueInfo(nameKey, nameNum, nameKey != null ? league.Country : null);
        }

        /// <summary>El atlas de retratos de 64 dp a la densidad de la pantalla, creado al pedirlo (lo usan «Tu avatar» y la Liga).</summary>
        internal AvatarAtlas AvatarAtlasForUi()
        {
            if (_avatarAtlas == null)
            {
                float density = Mathf.Clamp(_lobby != null ? _lobby.ScaleFactor : ProceduralArt.UiPxPerDp, 2f, 4f);
                _avatarAtlas = ProceduralArt.CreateAvatarAtlas(Mathf.CeilToInt(AvatarScreen.CellPortraitDp * density));
            }

            return _avatarAtlas;
        }

        /// <summary>
        /// Prepara las filas de la Liga antes de abrirla (avatares.md §3.3): con los avatares encendidos, el atlas de 32 dp (el de los
        /// demás jugadores, todos sin bloquear, y el disco neutro) y el retrato del jugador; si no, sin avatar.
        /// </summary>
        private void PrepareLeagueAvatars(LeagueScreen screen)
        {
            if (!AvatarsEnabled)
            {
                screen.Atlas = null;
                screen.MyAvatar = null;
                return;
            }

            if (_leagueAvatarAtlas == null)
            {
                float density = Mathf.Clamp(_lobby != null ? _lobby.ScaleFactor : ProceduralArt.UiPxPerDp, 2f, 4f);
                _leagueAvatarAtlas = ProceduralArt.CreateAvatarAtlas(Mathf.CeilToInt(LeagueScreen.AvatarDp * density));
            }

            screen.Atlas = _leagueAvatarAtlas;
            screen.MyAvatar = AvatarPortraitNow();
        }

        /// <summary>
        /// Abre «Tu avatar» desde <paramref name="source"/> (<c>home</c>, <c>settings</c> o <c>league</c>), con <c>af_avatar_open</c>.
        /// False si los avatares están apagados, si ya está abierta o (desde el inicio y Ajustes) antes del 10.
        /// </summary>
        public bool OpenAvatarScreen(string source)
        {
            AvatarService? avatars = _avatars;
            AvatarScreen? screen = _avatarScreen;
            if (avatars == null || screen == null || screen.Visible || !AvatarsEnabled || AccountDeleted)
            {
                return false;
            }

            if (source != "league" && !AvatarsShownInHome)
            {
                return false;
            }

            Sprite? portrait = AvatarPortraitNow();
            if (portrait == null)
            {
                return false;
            }

            screen.ReduceMotion = ReduceMotionNow;
            screen.Open(avatars, portrait, AvatarAtlasForUi(), LeagueEnabledForAvatars, _profile?.Progress.MaxLevel ?? 0, AvatarLeagueNow());
            _analytics?.Log(AnalyticsEvent.Create("af_avatar_open").Add("source", source)); // 08 §8.2 (D-056)
            AvatarProbe("open");
            _skipNextDelta = true;
            return true;
        }

        /// <summary>Cierra «Tu avatar» (su X y el Atrás de Android): lo desbloqueado pasa a visto y se apaga el punto.</summary>
        public bool CloseAvatarScreen()
        {
            AvatarScreen? screen = _avatarScreen;
            if (screen == null || !screen.Visible)
            {
                return false;
            }

            screen.Close();
            if (_avatars != null && !ProfileLocked && !AccountDeleted)
            {
                using (Saves())
                {
                    _avatars.MarkSeen();
                }
            }

            RefreshAvatarUi();
            AvatarProbe("close");
            return true;
        }

        /// <summary>Toca la celda de <paramref name="id"/> en «Tu avatar»: la elige si se puede; si está bloqueada, la sacude.</summary>
        public AvatarSelectResult ChooseAvatar(string id)
        {
            AvatarService? avatars = _avatars;
            AvatarScreen? screen = _avatarScreen;
            if (avatars == null || screen == null || ProfileLocked || AccountDeleted)
            {
                return AvatarSelectResult.Unknown;
            }

            AvatarSelectResult result;
            using (Saves())
            {
                result = avatars.Select(id, LeagueEnabledForAvatars);
            }

            if (result == AvatarSelectResult.Locked)
            {
                screen.Rejected(id);
                return result;
            }

            if (result == AvatarSelectResult.Selected)
            {
                AudioCues.Play(SfxId.UiTap);
                screen.Chosen(id);
            }

            RefreshAvatarUi();
            return result;
        }

        // AF_STATE avatar (avatares.md §3.6): id, chosen, unlocked, new y la causa.
        private void AvatarProbe(string cause)
        {
            AvatarService? avatars = _avatars;
            if (!DevProbe.Enabled || avatars == null)
            {
                return;
            }

            AvatarData data = avatars.Data;
            var sb = new System.Text.StringBuilder(160);
            sb.Append("\"id\":\"").Append(avatars.Current.Id).Append("\",\"chosen\":").Append(data.Chosen ? "true" : "false").Append(",\"unlocked\":[");
            for (int i = 0; i < data.Unlocked.Count; i++)
            {
                sb.Append(i > 0 ? ",\"" : "\"").Append(data.Unlocked[i]).Append('"');
            }

            sb.Append("],\"new\":").Append(AvatarRules.DotShown(data, _profile?.Progress.MaxLevel ?? 0) ? "true" : "false");
            sb.Append(",\"cause\":\"").Append(cause).Append('"');
            MarketProbe("avatar", sb.ToString());
        }
    }

    /// <summary>
    /// Avanza los avatares cada fotograma (<see cref="GameBootstrap.TickAvatars"/>: el atlas, «Tu avatar», los hitos y el envío a la
    /// Liga), como <see cref="LeagueRunner"/>. Los tests pueden apagarlo y llamar a <see cref="GameBootstrap.TickAvatars"/> a mano.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AvatarRunner : MonoBehaviour
    {
        private GameBootstrap? _boot;

        /// <summary>Asocia el arranque que lo creó.</summary>
        public void Bind(GameBootstrap boot)
        {
            _boot = boot;
        }

        private void Update()
        {
            if (_boot != null)
            {
                _boot.TickAvatars(Mathf.Min(Time.unscaledDeltaTime * 1000f, 100f));
            }
        }
    }
}
