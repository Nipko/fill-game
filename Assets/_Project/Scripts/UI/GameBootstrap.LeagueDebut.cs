#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.LiveOps;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// El estreno de la Liga del río (añadido de D-046 del 2026-11-06; D-059, añadido del 2026-10-31, punto 3): la pantalla
    /// <see cref="LeagueDebutScreen"/>, una sola vez por jugador, en el primer momento tranquilo tras desbloquearse la Liga.
    /// <list type="bullet">
    /// <item><b>Cuándo está pendiente</b> (<see cref="LeagueDebutPending"/>): la Liga activa (<c>live_league_enabled</c>, ganado el
    /// <c>live_league_unlock_level</c>, la edad contestada y no &lt; 13, perfil escribible) y ni <c>progress.league_debut_seen</c> ni
    /// <c>progress.league_opened</c> en el perfil. Así sale también al jugador que ya pasó el 41 cuando llega la 1.1 o cuando se
    /// enciende la Liga en Remote Config, y no vuelve tras reinstalar si el perfil viene de la nube (las dos marcas van en
    /// <c>progress</c> y se fusionan por «o»).</item>
    /// <item><b>Dónde sale</b>: (1) al dejar la victoria de campaña (Siguiente, Inicio o el avance solo tras la Cosecha doble), como
    /// pantalla aparte que tapa la victoria entera, después del pre-prompt de push si esa victoria lo lleva (entonces espera al
    /// siguiente momento: nunca dos pantallas seguidas); (2) si no, al entrar al inicio, en lugar del takeover de esa entrada (la
    /// libreta sale en la siguiente entrada, sin gastar el cupo), nunca con un aviso por abrir ni en la secuencia del 10. Nunca
    /// encima de la victoria a la vista, del banner de mundo (que es del nivel: en el 41 sale al entrar, antes de jugar) ni de la
    /// Liga; tampoco en una toma de capturas.</item>
    /// <item><b>Respuestas</b>: «Ver la Liga» va al inicio (desde la victoria, como Inicio) y abre la Liga en cuanto el inicio
    /// está despejado; «Luego» (y el Atrás de Android) sigue el camino que se pulsó y deja «Nueva» en la tarjeta «Liga» del inicio
    /// hasta que el jugador la abra.</item>
    /// <item>Se apunta como vista al salir (antes de responder), así un cierre de la app no la repite.</item>
    /// </list>
    /// Analítica: el catálogo de 08 §8.2 no tiene evento del estreno y no se inventa (pregunta abierta del añadido); «Ver la Liga»
    /// escribe el <c>af_league_open</c> de siempre. Sonda: <c>AF_STATE league_debut</c> y <c>AF_LAYOUT league_debut</c>.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        private LeagueDebutScreen? _leagueDebut;
        private Action? _leagueDebutLeave;
        private bool _leagueDebutLeaveToLobby;
        private bool _leagueDebutOpenPending;
        private bool _leagueDebutLobbyWas;

        /// <summary>
        /// El estreno en los tests: false lo apaga (<c>PlayModeHelpers.NoLeagueDebutUnlessSet</c>: los tests que no tratan de él
        /// entran al inicio y dejan la victoria como antes de que existiera); null o true, como en el juego.
        /// </summary>
        internal static bool? LeagueDebutOverride { get; set; }

        /// <summary>La pantalla del estreno (null antes del arranque o sin Liga).</summary>
        internal LeagueDebutScreen? LeagueDebutView => _leagueDebut;

        /// <summary>El estreno está a la vista.</summary>
        public bool LeagueDebutVisible => _leagueDebut != null && _leagueDebut.Visible;

        /// <summary>«Ver la Liga» ya se pulsó y la Liga se abrirá en cuanto el inicio esté despejado.</summary>
        internal bool LeagueDebutOpenPending => _leagueDebutOpenPending;

        /// <summary>
        /// El estreno saldría en el próximo momento tranquilo: la Liga activa, el perfil escribible y sin
        /// <c>progress.league_debut_seen</c> ni <c>progress.league_opened</c>.
        /// </summary>
        public bool LeagueDebutPending
        {
            get
            {
                Profile? profile = _profile;
                return LeagueDebutOverride != false && _leagueDebut != null && !_capture.Active && profile != null && !ProfileLocked
                    && !AccountDeleted && _league != null && _league.Active && !profile.Progress.LeagueDebutSeen
                    && !profile.Progress.LeagueOpened;
            }
        }

        /// <summary>
        /// La victoria de campaña de <paramref name="wonLevel"/> deja el estreno pendiente al salir (la Liga encendida y permitida,
        /// desbloqueada con esta victoria o antes, y sin el estreno visto): entonces esa victoria no apunta ninguna oferta, ni encima
        /// de la victoria ni al volver al inicio (<c>GameBootstrap.Market.cs</c>), como con el pre-prompt de push: nunca dos pantallas
        /// seguidas. Se llama en la victoria, antes de que el progreso guardado sepa del nivel ganado.
        /// </summary>
        internal bool LeagueDebutDueAtCampaignVictory(int wonLevel)
        {
            Profile? profile = _profile;
            LeagueService? league = _league;
            if (LeagueDebutOverride == false || _leagueDebut == null || _capture.Active || profile == null || league == null || ProfileLocked
                || AccountDeleted || profile.Progress.LeagueDebutSeen || profile.Progress.LeagueOpened)
            {
                return false;
            }

            int maxLevel = Math.Max(_progress?.MaxLevel ?? 0, wonLevel);
            return league.Config.LiveLeagueEnabled && LeagueAllowed() && LeagueRules.IsUnlocked(maxLevel, league.Config);
        }

        /// <summary>«Nueva» en la tarjeta «Liga» del inicio: la Liga activa, el estreno visto y la Liga aún sin abrir.</summary>
        public bool LeagueNewBadge
        {
            get
            {
                Profile? profile = _profile;
                return profile != null && _league != null && _league.Active && profile.Progress.LeagueDebutSeen && !profile.Progress.LeagueOpened;
            }
        }

        // En SetUpLeague: la pantalla (el arte se pide al abrirla).
        private void SetUpLeagueDebut()
        {
            if (_art == null || _leagueDebut != null)
            {
                return;
            }

            _leagueDebut = new LeagueDebutScreen(transform, _art, () => OnLeagueDebutAnswer(true), () => OnLeagueDebutAnswer(false));
        }

        /// <summary>
        /// Siguiente o Inicio de la victoria de campaña: si el estreno está pendiente, lo enseña y devuelve true; <paramref name="leave"/>
        /// sigue el camino con «Luego». Si no, false y no hace nada.
        /// </summary>
        internal bool TryLeagueDebutOnLeave(bool toLobby, Action leave)
        {
            if (!LeagueDebutPending || _leagueDebutLeave != null)
            {
                return false;
            }

            _leagueDebutLeave = leave ?? throw new ArgumentNullException(nameof(leave));
            _leagueDebutLeaveToLobby = toLobby;
            ShowLeagueDebut("victory");
            return true;
        }

        // Al entrar al inicio (TakeoverAtLobby, o TickLeague sin la Bitácora): el estreno en lugar del takeover de esa entrada.
        private bool TryLeagueDebutAtLobby()
        {
            if (!LeagueDebutPending || PushRoutePending || (_app != null && _app.L10MapDue) || !LobbyClear())
            {
                return false;
            }

            _leagueDebutLeave = null;
            ShowLeagueDebut("lobby");
            SkipNextLobbyTakeover(); // al cerrarlo, esa vuelta al inicio no saca la libreta: sale en la siguiente entrada
            return true;
        }

        private void ShowLeagueDebut(string source)
        {
            LeagueDebutScreen? view = _leagueDebut;
            Profile? profile = _profile;
            if (view == null || profile == null)
            {
                return;
            }

            profile.Progress.LeagueDebutSeen = true;
            SaveLeagueMarks(profile);
            view.Open(ReduceMotionNow);
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.StreakUp); // la fanfarria con los efectos del proyecto (sin audio nuevo)
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.Confetti);
            AF.Audio.AudioCues.Stinger(AF.Audio.StingerId.Arrival); // su remate (con Música)
            _a11y?.Announce(LeagueText.DebutTitle);
            _a11y?.Invalidate();
            if (DevProbe.Enabled)
            {
                MarketProbe("league_debut", "\"source\":\"" + source + "\",\"reduce_motion\":" + (view.ReduceMotion ? "true" : "false"));
                if (_probe != null)
                {
                    var ids = new List<string>(2);
                    var rects = new List<RectTransform>(2);
                    view.VisibleButtons(ids, rects);
                    _probe.Layout("league_debut", 0, null, ids.ToArray(), rects.ToArray());
                }
            }
        }

        // «Ver la Liga» (open) o «Luego»: el camino de la victoria, si espera, y la Liga.
        private void OnLeagueDebutAnswer(bool open)
        {
            Action? leave = _leagueDebutLeave;
            bool toLobby = _leagueDebutLeaveToLobby;
            _leagueDebutLeave = null;
            if (DevProbe.Enabled)
            {
                MarketProbe("league_debut_answer", "\"answer\":\"" + (open ? "open" : "later") + "\"");
            }

            if (open)
            {
                _leagueDebutOpenPending = true;
                if (leave != null)
                {
                    // Desde la victoria (Siguiente o Inicio): al inicio, sin el takeover de esa entrada, y allí la Liga.
                    SkipNextLobbyTakeover();
                    _app?.PressHome();
                }

                TryOpenLeagueFromDebut();
            }
            else if (leave != null)
            {
                if (toLobby)
                {
                    SkipNextLobbyTakeover(); // la entrada al inicio que sigue no apila la libreta sobre el estreno
                }

                leave();
            }

            _a11y?.Invalidate();
        }

        // «Ver la Liga» pendiente: la abre en cuanto el inicio está despejado; si el jugador vuelve a un nivel antes, se olvida.
        private void TryOpenLeagueFromDebut()
        {
            if (!_leagueDebutOpenPending)
            {
                return;
            }

            if (_app != null && _app.Screen == AppScreen.Level && !VictoryOnScreen())
            {
                _leagueDebutOpenPending = false;
                return;
            }

            if (LobbyClear())
            {
                // Con el inicio despejado, se abre ahora o nunca: si la Liga ya no puede abrirse (Remote Config, la edad), se olvida
                // en lugar de abrirse sola mucho después.
                OpenLeague();
                _leagueDebutOpenPending = false;
            }
        }

        // La Liga se abrió (OpenLeague): progress.league_opened y fuera «Nueva».
        private void MarkLeagueOpened()
        {
            Profile? profile = _profile;
            if (profile == null || profile.Progress.LeagueOpened)
            {
                return;
            }

            profile.Progress.LeagueOpened = true;
            SaveLeagueMarks(profile);
            _leagueButton?.SetNew(false);
        }

        // Guarda las marcas del estreno en un lote propio, como el tooltip de Marea (GameBootstrap.UndoTip.cs).
        private void SaveLeagueMarks(Profile profile)
        {
            ProfileSaveBatch? batch = _saveBatch;
            if (batch != null && !ProfileLocked && !AccountDeleted)
            {
                using (Saves())
                {
                    batch.TrySave(profile);
                }
            }
        }

        // Cada fotograma (TickLeague): la animación, la Liga que espera a «Ver la Liga», la entrada al inicio sin la Bitácora y «Nueva».
        private void TickLeagueDebut(float dtMs)
        {
            _leagueDebut?.Tick(dtMs);
            TryOpenLeagueFromDebut();

            // Sin la Bitácora (sin su TakeoverAtLobby), la entrada al inicio se detecta aquí.
            bool lobby = LobbyClear();
            if ((_logbook == null || _logbookScreen == null) && lobby && !_leagueDebutLobbyWas)
            {
                bool skip = _skipLobbyTakeover;
                _skipLobbyTakeover = false;
                if (!skip)
                {
                    TryLeagueDebutAtLobby();
                }
            }

            _leagueDebutLobbyWas = lobby;
            if (_leagueButton != null && _leagueButton.SetNew(LeagueNewBadge))
            {
                _dayCardsDirty = true; // la píldora cambia el ancho de la esquina: la fila se vuelve a colocar
                PlaceDayCards();
            }
        }

        /// <summary>
        /// Atrás de Android con el estreno delante: «Luego», también antes de que los botones reciban toques (Atrás es un gesto
        /// deliberado, no un toque impaciente) [P]. False si no se ve.
        /// </summary>
        public bool PressLeagueDebutBack()
        {
            if (!LeagueDebutVisible)
            {
                return false;
            }

            _leagueDebut!.Answer(false);
            return true;
        }

        // Borrado de cuenta: fuera la pantalla y lo que esperaba.
        private void WipeLeagueDebut()
        {
            _leagueDebutLeave = null;
            _leagueDebutOpenPending = false;
            _leagueDebut?.Close();
            _leagueButton?.SetNew(false);
        }
    }
}
