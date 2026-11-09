#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Analytics;
using AF.Audio;
using AF.LiveOps;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Los marcos de país del avatar en el juego (Q-M6 de D-033: «se guarda como derecho y se enseña en la 1.1», con su añadido del
    /// 2026-11-08). <see cref="SetUpAvatarFrames"/> (desde <c>SetUpAvatars</c>) crea el servicio (<see cref="AvatarFrameService"/>) sobre
    /// <c>avatar</c> y <c>entitlements.frames</c> del perfil y lo engancha a:
    /// <list type="bullet">
    /// <item>el retrato del jugador (<see cref="AvatarPortraitNow"/>), que lleva el marco dentro: el inicio, Ajustes, «Tu avatar», la
    /// fila del jugador en la Liga y la línea de la Liga en la victoria;</item>
    /// <item>«Tu avatar», con la fila de los marcos para ponerlos o quitarlos (<see cref="ChooseAvatarFrame"/>);</item>
    /// <item>la libreta de la Bitácora: al sellar la casilla 30, el marco se pone solo y la libreta lo enseña en el retrato con el
    /// enlace «Ver tu avatar» (<see cref="OnLogbookFrameEarned"/> y <see cref="ShowLogbookFrameReveal"/>);</item>
    /// <item>el punto «nuevo» del retrato del inicio, con un marco sin ver.</item>
    /// </list>
    /// Analítica (08 §8.2; pregunta 2 del añadido de Q-M6, «Medir ya»): <c>af_avatar_frame_select</c> con <c>auto</c> true al ganarlo en la
    /// Bitácora y false al elegirlo en «Tu avatar» (<see cref="LogAvatarFrameSelect"/>), y <c>af_avatar_open</c> con <c>source</c> =
    /// <c>logbook</c> desde «Ver tu avatar». Como todos los eventos del catálogo, pasan por <c>AnalyticsService</c>: sin consentimiento
    /// (ni, en la banda &lt;13, sin el SDK) no salen a Firebase.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>
        /// Extra de intent (texto) de los builds de desarrollo (Q-M6, añadido del 2026-11-08): <c>--es af_frame_grant 3</c> da el marco de
        /// país del mundo 3 (<c>frame_w03</c>); <c>1,4,7</c> varios; <c>all</c> los 9 de la 1.1. Los apunta en <c>entitlements.frames</c>
        /// y pone el último dado, como el día 30 de la Bitácora, sin marcarlos vistos (sale el punto del inicio). Logcat lo dice con
        /// <c>AF_STATE avatar</c> (<c>cause</c> = <c>frame_grant</c>). Nunca se lee en release.
        /// </summary>
        public const string IntentExtraFrameGrant = "af_frame_grant";

        /// <summary>
        /// Extra de intent (entero) de los builds de desarrollo (Q-M6, añadido del 2026-11-08): <c>--ei af_logbook_day 30</c> deja la
        /// Bitácora en esa casilla (1-30), sin sello hoy y con hoy ya jugado, para probar en el teléfono el cobro de esa casilla (la 30:
        /// la Canasta grande y el marco) sin esperar 30 días. Necesita la Bitácora desbloqueada (<c>af_max_level</c>). Nunca en release.
        /// </summary>
        public const string IntentExtraLogbookDay = "af_logbook_day";

        /// <summary>Mundos que da <c>af_frame_grant all</c>: los de la 1.1 (1-9).</summary>
        public const int FrameGrantAllWorlds = 9;

        private AvatarFrameService? _avatarFrames;

        /// <summary><c>af_frame_grant</c> forzado por un test (el valor del extra); null = el extra. Solo en desarrollo.</summary>
        internal static string? FrameGrantOverride { get; set; }

        /// <summary><c>af_logbook_day</c> forzado por un test; null = el extra. Solo en desarrollo.</summary>
        internal static int? LogbookDayOverride { get; set; }

        /// <summary>Los marcos del avatar (null antes del arranque).</summary>
        public AvatarFrameService? FrameService => _avatarFrames;

        /// <summary>El marco que lleva el avatar ahora, o null.</summary>
        internal AvatarFrameInfo? AvatarFrameNow => _avatarFrames?.Current;

        private void SetUpAvatarFrames(Profile profile, IClock clock)
        {
            _avatarFrames = new AvatarFrameService(profile.Avatar, profile.Entitlements.Frames, () => SaveAvatarProfile(), clock);
            if (_avatarScreen != null)
            {
                _avatarScreen.FrameTapped = id => ChooseAvatarFrame(id);
            }

            if (_logbookScreen != null)
            {
                WireLogbookFrames(_logbookScreen);
            }

            if (!ProfileLocked && Debug.isDebugBuild)
            {
                ApplyDevFrameGrant(FrameGrantOverride ?? LaunchOptions.ReadIntentString(IntentExtraFrameGrant));
            }
        }

        // La libreta con los marcos: el icono de verdad en las filas de premio y el enlace «Ver tu avatar».
        private void WireLogbookFrames(LogbookScreen screen)
        {
            screen.FrameIconFor = LogbookFrameIcon;
            screen.FrameViewPressed = () => OpenAvatarFromLogbook();
        }

        /// <summary>
        /// El extra <c>af_frame_grant</c> (desarrollo; <see cref="IntentExtraFrameGrant"/>): da esos marcos y pone el último. Devuelve
        /// cuántos dio.
        /// </summary>
        internal int ApplyDevFrameGrant(string? value)
        {
            AvatarFrameService? frames = _avatarFrames;
            if (frames == null || string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            int granted = 0;
            string? last = null;
            using (Saves())
            {
                string[] parts = string.Equals(value!.Trim(), "all", StringComparison.OrdinalIgnoreCase) ? AllWorlds() : value.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int world) && AvatarFrames.OfWorld(world) != null)
                    {
                        if (frames.Grant(world))
                        {
                            granted++;
                        }

                        last = AvatarFrames.OfWorld(world)!.Id;
                    }
                }

                if (last != null)
                {
                    frames.PutOnEarned(last);
                }
            }

            Debug.LogWarning("[AF.Bootstrap] perfil de desarrollo: " + IntentExtraFrameGrant + " = " + value + " (" + granted + " marcos nuevos).");
            AvatarProbe("frame_grant");
            return granted;
        }

        private static string[] AllWorlds()
        {
            var all = new string[FrameGrantAllWorlds];
            for (int w = 1; w <= FrameGrantAllWorlds; w++)
            {
                all[w - 1] = w.ToString(CultureInfo.InvariantCulture);
            }

            return all;
        }

        /// <summary>
        /// El extra <c>af_logbook_day</c> (desarrollo; <see cref="IntentExtraLogbookDay"/>) sobre <paramref name="profile"/>: la casilla
        /// que toca, sin sello hoy y con hoy jugado. Devuelve si cambió algo (quien llama guarda).
        /// </summary>
        internal static bool ApplyDevLogbookDay(Profile profile, IClock clock, int day)
        {
            if (profile == null || clock == null || day < 1 || day > LoginCalendarTable.Days)
            {
                return false;
            }

            CalendarData calendar = profile.Calendar ?? new CalendarData();
            calendar.NextDay = day;
            calendar.LastClaimDate = null;
            calendar.PlayedDate = clock.LocalDateKey();
            profile.Calendar = calendar;
            Debug.LogWarning("[AF.Bootstrap] perfil de desarrollo: " + IntentExtraLogbookDay + " = " + day + ".");
            return true;
        }

        /// <summary>El valor de <c>af_logbook_day</c> (0 sin el extra o fuera de desarrollo).</summary>
        internal static int DevLogbookDay()
        {
            return LogbookDayOverride ?? (Debug.isDebugBuild ? LaunchOptions.ReadIntentInt(IntentExtraLogbookDay, 0) : 0);
        }

        /// <summary>El punto del retrato del inicio por un marco sin ver (desde el 10, como el avatar).</summary>
        private bool AvatarFrameDot(int maxLevel)
        {
            return maxLevel >= AvatarRules.HomeUnlockLevel && _avatarFrames != null && _avatarFrames.AnyUnseen;
        }

        private void PrepareAvatarScreenFrames(AvatarScreen screen)
        {
            float density = Mathf.Clamp(_lobby != null ? _lobby.ScaleFactor : ProceduralArt.UiPxPerDp, 2f, 4f);
            screen.Frames = _avatarFrames;
            screen.FramePx = Mathf.CeilToInt(AvatarScreen.CellPortraitDp * density);
        }

        /// <summary>
        /// Toca la celda de marco <paramref name="frameId"/> en «Tu avatar» (null: «Sin marco»): lo pone en el acto, sin confirmar (como un
        /// avatar), con el rebote del retrato y el sonido de UI.
        /// </summary>
        public AvatarFrameSelectResult ChooseAvatarFrame(string? frameId)
        {
            AvatarFrameService? frames = _avatarFrames;
            if (frames == null || ProfileLocked || AccountDeleted)
            {
                return AvatarFrameSelectResult.NotOwned;
            }

            AvatarFrameSelectResult result;
            using (Saves())
            {
                result = frames.Select(frameId);
            }

            if (result == AvatarFrameSelectResult.Selected)
            {
                LogAvatarFrameSelect(frameId, auto: false);
                AudioCues.Play(SfxId.UiTap);
                _avatarScreen?.FrameChosen();
                AvatarProbe("frame");
            }

            RefreshAvatarUi();
            return result;
        }

        /// <summary>
        /// Recién sellada una casilla con marco (la 30), dentro de la misma escritura del cobro: el marco se pone solo (Q-M8 (a), como la
        /// skin ganada) [P].
        /// </summary>
        private void OnLogbookFrameEarned(LoginCalendarClaim? claim)
        {
            if (claim?.FrameId == null || _avatarFrames == null)
            {
                return;
            }

            if (_avatarFrames.PutOnEarned(claim.FrameId))
            {
                LogAvatarFrameSelect(claim.FrameId, auto: true);
            }
        }

        /// <summary>
        /// <c>af_avatar_frame_select</c> (08 §8.2; Q-M6, añadido del 2026-11-08, «Medir ya»): <c>frame</c> = el id del marco o
        /// <see cref="AnalyticsCatalog.AvatarFrameNone"/> con «Sin marco» [P]; <c>auto</c> = se puso solo al ganarlo en la Bitácora.
        /// </summary>
        private void LogAvatarFrameSelect(string? frameId, bool auto)
        {
            _analytics?.Log(AnalyticsEvent.Create("af_avatar_frame_select").Add("frame", frameId ?? AnalyticsCatalog.AvatarFrameNone).Add("auto", auto));
        }

        /// <summary>
        /// El momento del marco en la libreta, tras <see cref="LogbookScreen.ShowClaimed"/>: con el avatar a la vista (desde el 10 y con
        /// <c>ui_avatars_enabled</c>), el retrato con el marco, su frase y «Ver tu avatar»; el marco pasa a visto.
        /// </summary>
        private void ShowLogbookFrameReveal(LogbookScreen screen, LoginCalendarClaim claim)
        {
            AvatarFrameService? frames = _avatarFrames;
            AvatarFrameInfo? frame = AvatarFrames.Find(claim.FrameId);
            if (frames == null || frame == null || !AvatarsShownInHome)
            {
                return;
            }

            Sprite? portrait = AvatarPortraitNow();
            if (portrait == null)
            {
                return;
            }

            // «Ver tu avatar» solo con la libreta abierta sobre el inicio: desde la victoria, cerrar la libreta sigue con la victoria.
            bool canView = screen.Source != LogbookOpenSource.Victory && screen.Source != LogbookOpenSource.L10;
            screen.ShowFrameReveal(portrait, LogbookText.FrameReveal(frame.World), canView);
            using (Saves())
            {
                frames.MarkSeen(frame.Id);
            }

            RefreshAvatarUi();
            AvatarProbe("frame_earned");
        }

        // «Ver tu avatar» de la libreta: la cierra (con lo que tuviera pendiente detrás) y abre «Tu avatar», con af_avatar_open y
        // source = logbook (08 §8.2; pregunta 2 del añadido de Q-M6, «Medir ya»).
        private void OpenAvatarFromLogbook()
        {
            OnLogbookClose();
            OpenAvatarScreen(AvatarOpenSourceLogbook);
        }

        // El icono del marco en las filas de premio de la libreta (22 dp).
        private Sprite? LogbookFrameIcon(string frameId)
        {
            AvatarFrameInfo? frame = AvatarFrames.Find(frameId);
            if (frame == null || _art == null)
            {
                return null;
            }

            float density = Mathf.Clamp(_lobby != null ? _lobby.ScaleFactor : ProceduralArt.UiPxPerDp, 2f, 4f);
            return _art.AvatarFrameOverlay(frame.World, frame.Country, Mathf.CeilToInt(22f * density));
        }

        // AF_STATE avatar (Q-M6): el marco puesto, los ganados y si hay alguno sin ver.
        private void AppendFrameProbe(StringBuilder sb)
        {
            AvatarFrameService? frames = _avatarFrames;
            if (frames == null)
            {
                return;
            }

            AvatarFrameInfo? on = frames.Current;
            sb.Append(",\"frame\":").Append(on == null ? "null" : "\"" + on.Id + "\"").Append(",\"frames\":[");
            IReadOnlyList<string> owned = frames.OwnedIds;
            for (int i = 0; i < owned.Count; i++)
            {
                sb.Append(i > 0 ? ",\"" : "\"").Append(owned[i]).Append('"');
            }

            sb.Append("],\"frames_new\":").Append(frames.AnyUnseen ? "true" : "false");
        }
    }
}
