#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Parcial de la interfaz de la Crecida, la Hoja escudo y la Canasta en el nivel (tarea B1b de la tanda 5; nota
    /// <c>docs/dev/crecida-canasta.md</c>; lógica de B1a en <see cref="StreakService"/> y <c>LevelFlow.Streak.cs</c>):
    /// <list type="bullet">
    /// <item><description><b>La barra vertical del HUD</b> (<see cref="StreakBar"/>; 02 §5; 07 §7.4): desde
    /// <c>streak_unlock_level</c>, sale en T, al habilitarse el toque (<c>OnIntroFinished</c>, o al reanudar sin intro), y se va al
    /// cargar otro nivel, en cada intro (D-030, pregunta 4 (c): la tropa lateral puede pasar por su franja) y en la pantalla de
    /// victoria, que la replica en grande. Lee el estado con <see cref="LevelFlow.PeekStreak"/> solo en esos momentos.</description></item>
    /// <item><description><b>Las barras del panel único de la victoria</b> (<see cref="VictoryBars"/>; D-033, puntos 8-10): entre la
    /// Cosecha y la Cosecha doble; entran en la victoria más alta posible con la que se elige el escalón de la victoria compacta
    /// (<see cref="WorstBarsDp"/>; D-032, punto 2) siempre que la pantalla tenga Crecida (la campaña con su servicio), así que
    /// «Siguiente» no cambia de sitio de una victoria a otra (Q7-A). Un toque en Siguiente con animaciones pendientes las completa,
    /// enseña el estado final <see cref="NextHoldMs"/> ms y sigue (D-033, punto 9; <see cref="TakeNextHold"/>). Las semillas que
    /// pagan no se enseñan en el saldo hasta que llegan (D-011; <see cref="PendingBarSeeds"/>). En los hitos de TiTino (D-016,
    /// T6-A), si con las barras su frase no cabe y sin ellas sí, la victoria va sin barras y TiTino habla (B1b-Q10 (c), decisión
    /// del dueño; <see cref="StreakYieldToSloth"/>).</description></item>
    /// <item><description><b>El aviso de la Hoja escudo</b> (04 §6.5 y §8: «toast del primer fallo cubierto»): con el fallo duro que
    /// cubre la hoja suena <c>sfx_streak_shield</c> y, en cuanto se vuelve a jugar tras la intro, sale <c>shield_toast</c> por la
    /// cola de avisos [P] (en 1-30 el reinicio vacía la cola y desde el 31 va Nivel fallido delante).</description></item>
    /// </list>
    /// </summary>
    public sealed partial class GameScreen
    {
        /// <summary>Lo que se ve el estado final de las barras tras un toque en Siguiente antes de seguir (ms; D-033, punto 9) [P].</summary>
        internal const float NextHoldMs = 250f;

        /// <summary>Margen de las barras a cada lado del panel de victoria (dp), como la fila de la Cosecha.</summary>
        internal const float VictoryBarsSideDp = 16f;

        private StreakBar _streakBar = null!;
        private VictoryBars _victoryBars = null!;
        private Func<RectTransform, Vector2> _canvasCenter = null!;
        private float _nextHoldMs = -1f;
        private bool _victoryBarsDropped;
        private bool _victoryBarsYield;
        private bool _victoryBarsYieldTried;
        private bool _shieldToastPending;
        private int _shieldToastLevel;
        private readonly float[] _worstBars = new float[2 * (VictoryCompactMax + 1)]; // a lo ancho y, detrás, en una fila
        private string? _worstBarsLocale;
        private float _worstBarsScale = -1f;

        /// <summary>La barra de la Crecida del HUD (tests y capturas).</summary>
        internal StreakBar StreakHudBar => _streakBar;

        /// <summary>Las barras del panel de victoria (tests y capturas).</summary>
        internal VictoryBars VictoryBarsView => _victoryBars;

        /// <summary>
        /// Las barras no caben en esta pantalla, idioma y escala ni con el último escalón de la victoria compacta, así que no salen
        /// [P] (tarea B1b; medido: solo en en-XA al 130 % en 16:9). Lo decide <c>ChooseVictoryCompact</c>.
        /// </summary>
        internal bool VictoryBarsDropped => _victoryBarsDropped;

        /// <summary>
        /// Esta victoria, un hito de TiTino, va sin barras para que él diga su frase (B1b-Q10 (c), decisión del dueño;
        /// <see cref="StreakYieldToSloth"/>): lo que pagan llega igual al saldo y a la bandeja (<see cref="VictoryBars.Barless"/>).
        /// </summary>
        internal bool VictoryBarsYielded => _victoryBarsYield;

        /// <summary>
        /// En esta victoria se probó quitar las barras para que TiTino hablara (un hito suyo que con ellas no cabía); si
        /// <see cref="VictoryBarsYielded"/> es false, ni sin ellas cabía y se quedaron (tests).
        /// </summary>
        internal bool VictoryBarsYieldTried => _victoryBarsYieldTried;

        /// <summary>Siguiente se pulsó con las barras pendientes y espera sus 250 ms (tests).</summary>
        internal bool NextHoldPending => _nextHoldMs >= 0f;

        /// <summary>
        /// La pantalla puede llevar las barras de la Crecida y la Canasta: hay un flujo de campaña con su servicio (no el Reto del
        /// río, que no las lleva: Q-C2, E25). Con ellas, la victoria más alta posible las cuenta (D-032, punto 2).
        /// </summary>
        internal bool VictoryBarsPossible => _flow != null && _flow.Streak != null && !_flow.IsDaily;

        private void StreakBuild()
        {
            _streakBar = new StreakBar(_hud, _art);
            RectTransform fly = UiBuilder.Rect("BarsFly", _victoryOverlay); // detrás del saldo (capa superior), delante del panel
            _victoryBars = new VictoryBars(_victoryPanel, fly, _art);
            _canvasCenter = CanvasCenterDp;
        }

        private void StreakLevelLoaded(int level)
        {
            if (level != _shieldToastLevel)
            {
                _shieldToastPending = false; // el aviso de la hoja es del nivel en que cubrió el fallo
            }

            _streakBar.SetVisible(false);
            _victoryBars.Hide();
            _victoryBarsYield = false;
            _victoryBarsYieldTried = false;
            _nextHoldMs = -1f;
        }

        // Toque habilitado tras la intro: la barra sale en T (02 §5; 07 §7.4) y, si la hoja cubrió el último fallo, su aviso.
        private void StreakTouchEnabled(LevelFlow flow)
        {
            ShowHudStreak(flow);
            if (_shieldToastPending)
            {
                _shieldToastPending = false;
                ShowToast(UiText.ShieldToast);
            }
        }

        // Reabierto sin intro (02 §14): en juego o con un modal del embudo, la barra ya estaba a la vista.
        private void StreakResumed(LevelFlow flow, FlowPhase phase)
        {
            if (phase == FlowPhase.Playing || phase == FlowPhase.StuckModal || phase == FlowPhase.GiveUpConfirm || phase == FlowPhase.LevelFailed)
            {
                ShowHudStreak(flow);
            }
        }

        private void ShowHudStreak(LevelFlow flow)
        {
            StreakSnapshot? s = flow.PeekStreak();
            bool active = s.HasValue && s.Value.Active;
            if (active)
            {
                _streakBar.Set(s!.Value.Wins, s.Value.ShieldAvailable, s.Value.RiverFull);
            }

            _streakBar.SetVisible(active);
        }

        private void StreakFailResolved(LevelFlow flow)
        {
            StreakServiceResult? r = flow.LastStreakResult;
            if (r != null && r.Streak.ShieldUsed)
            {
                AF.Audio.AudioCues.Play(AF.Audio.SfxId.StreakShield); // la hoja que se despliega (07 §9.2)
                _shieldToastPending = true;
                _shieldToastLevel = flow.LevelNumber;
            }
        }

        // Antes de colocar la victoria: qué barras lleva (la Crecida tras la victoria y lo que pagó) y sus textos.
        private void StreakVictoryPlan(LevelFlow flow)
        {
            _streakBar.SetVisible(false); // la victoria la replica en grande (02 §6)
            _nextHoldMs = -1f;
            _victoryBarsYield = false; // con barras, hasta que StreakYieldToSloth decida lo contrario
            _victoryBarsYieldTried = false;
            StreakService? service = flow.Streak;
            string? place = null;
            RiverStopCatalog? stops = RiverStops;
            if (stops != null && flow.LevelNumber >= 1)
            {
                place = UiText.NextPlaceName(stops, stops.StopOf(flow.LevelNumber));
            }

            _victoryBars.Plan.Build(flow.LevelNumber, !flow.IsDaily, flow.PeekStreak(), flow.LastStreakResult, service?.Config,
                service?.Basket, service?.BasketUnlockLevel ?? 0, place);
            _victoryBars.Prepare(flow.ReduceMotion, _canvasCenter);
        }

        /// <summary>
        /// B1b-Q10 (c), decisión del dueño (D-033, añadido del 2026-10-20): en los hitos de TiTino (<see cref="VictoryLineArbiter.IsSlothHito"/>:
        /// 40, 60, 80, 140, 200…; D-016, T6-A; el dueño citó el 60, el 80 y el 140, donde a 360 × 780 no cabía), si con las barras de la Crecida y la Canasta su frase no cabe (<c>SlothVictoryShown</c> no le
        /// dio voz), la victoria se coloca otra vez sin ellas y, si así cabe, va sin barras y TiTino habla como antes de B1b; si ni
        /// así cabe (16:9 con la ranura, 130 % con el héroe), las barras se quedan, porque quitarlas no le daría la voz. Sin barras,
        /// su secuencia sigue (<see cref="VictoryBars.Barless"/>): la Canasta del 60 se abre y su contenido vuela al saldo y a la
        /// bandeja (D-011), y la Crecida avanza igual (ya lo hizo <see cref="StreakService"/>) aunque su barra no se dibuje. Se llama
        /// con la victoria ya colocada, tras <c>SlothVictoryShown</c> y antes de que la ranura pida su línea al árbitro; deja en
        /// <paramref name="bandTop"/> y <paramref name="bandBottom"/> la franja con la que se decidió.
        /// </summary>
        private void StreakYieldToSloth(LevelFlow flow, ref float bandTop, ref float bandBottom)
        {
            int won = flow.LevelNumber;
            if (_slothCanSpeak || SlothBoxCanSpeakOverride.HasValue || !SlothEnabled || !SlothVoice || won < 1 || won > WorldTable.LastLevel
                || !VictoryLineArbiter.IsSlothHito(won) || _victoryBarsDropped || !_victoryBars.Root.gameObject.activeSelf
                || _victoryBars.Height <= 0f)
            {
                return;
            }

            _victoryBarsYieldTried = true;
            _victoryBarsYield = true;
            RelayoutForSloth(flow, out bandTop, out bandBottom);
            if (_slothCanSpeak)
            {
                return;
            }

            _victoryBarsYield = false; // ni sin barras cabe su frase: la victoria se queda como estaba, con ellas
            RelayoutForSloth(flow, out bandTop, out bandBottom);
        }

        // Vuelve a colocar la victoria recién abierta (como OnVictoryScreen) y a decidir la caja de TiTino con ella.
        private void RelayoutForSloth(LevelFlow flow, out float bandTop, out float bandBottom)
        {
            LayoutVictory();
            ShiftBoard(ShiftToClear(_victoryPanelY - RibbonRiseDp, 8f), true);
            ApplyVictoryAnim(0f);
            VictoryFreeBand(out bandTop, out bandBottom);
            SlothVictoryShown(flow, bandTop, bandBottom);
        }

        // Con la victoria colocada y el saldo arriba (SyncChrome): adónde vuelan las semillas (el saldo) y los comodines (la bandeja).
        private void StreakVictoryStarted()
        {
            Vector2 seeds = CanvasCenterDp(_walletBar.SeedsRect);
            var stock = new Vector2(_canvasW * 0.5f, TrayTop + 40f);
            _victoryBars.SetTargets(seeds, stock);
        }

        private void StreakTick(float dtMs)
        {
            if (_victoryOverlay.gameObject.activeSelf)
            {
                _victoryBars.Tick(dtMs);
            }

            if (_nextHoldMs > 0f)
            {
                _nextHoldMs = Mathf.Max(0f, _nextHoldMs - dtMs);
            }
        }

        // Siguiente (D-033, punto 9): con las barras a medias, las completa, enseña el estado final 250 ms y sigue (TakeNextHold);
        // si no, como siempre. Un segundo toque durante la espera no hace nada.
        private void OnNextPressed()
        {
            if (_nextHoldMs >= 0f)
            {
                return;
            }

            if (_victoryBars.Pending && !_victoryBars.PendingReward)
            {
                _victoryBars.Complete(); // solo subía el agua: al estado final y adelante (B1b-Q11)
            }
            else if (_victoryBars.PendingReward)
            {
                _victoryBars.Complete();
                _nextHoldMs = NextHoldMs;
                // Mientras espera, Inicio y la Cosecha doble no responden, como tras la Cosecha doble (D-011, punto 4): nada puede
                // abrir un vídeo que acabaría fuera de la victoria.
                UiBuilder.SetEnabled(_home.Button, false);
                UiBuilder.SetEnabled(_double.Button, false);
                _probe.Victory("next_hold", RequireFlow(), _doubleShown);
                return;
            }

            _actions.PressNext();
        }

        /// <summary>
        /// La espera de Siguiente venció (D-033, punto 9): true una vez, si la pantalla sigue en VICTORY_SCREEN; quien llama pulsa
        /// Siguiente (<c>GameBootstrap.Update</c>, por <see cref="TakeAutoNext"/>, como tras la Cosecha doble).
        /// </summary>
        private bool TakeNextHold()
        {
            if (_nextHoldMs != 0f)
            {
                return false;
            }

            _nextHoldMs = -1f;
            LevelFlow? flow = _flow;
            return flow != null && flow.HasLevel && flow.Phase == FlowPhase.VictoryScreen;
        }

        // Semillas de la Crecida y la Canasta pagadas en esta victoria que el saldo aún no enseña (D-011, punto 1): todas mientras
        // termina el último vertido y en la celebración; en la victoria, las que aún no llegaron volando.
        private int PendingBarSeeds(LevelFlow flow)
        {
            if (flow.IsResolvingWin || flow.Phase == FlowPhase.Celebration)
            {
                StreakServiceResult? r = flow.LastStreakResult;
                return r != null && r.Streak.Event == StreakEvent.Victory ? Mathf.Max(0, r.StreakSeeds + r.BasketSeeds) : 0;
            }

            bool victory = flow.Phase == FlowPhase.VictoryScreen || flow.Phase == FlowPhase.AdGate;
            return victory ? _victoryBars.PendingSeeds : 0;
        }

        /// <summary>
        /// Alto de las barras más altas posibles con el escalón <paramref name="level"/> (dp): 0 si la pantalla no las lleva; si no,
        /// dos barras (la Crecida y la Canasta) con su línea a 14 sp × la escala (<see cref="VictoryBars.WorstHeight"/>), en caché por
        /// idioma y escala (se mide al cargar el nivel, con <see cref="WarmVictorySlot"/>).
        /// </summary>
        private float WorstBarsDp(int level)
        {
            if (!VictoryBarsPossible || _victoryBarsDropped || _victoryBarsYield)
            {
                return 0f; // sin barras (o cedidas a TiTino en su hito, B1b-Q10 (c)), el escalón se elige como antes de B1b
            }

            string locale = Loc.IsConfigured ? Loc.Locale : LocLocale.SourceLocale;
            float scale = UiBuilder.TextScale;
            if (!string.Equals(locale, _worstBarsLocale, StringComparison.Ordinal) || !Mathf.Approximately(scale, _worstBarsScale))
            {
                _worstBarsLocale = locale;
                _worstBarsScale = scale;
                bool keep = _victoryBars.ForceHalf;
                int n = VictoryCompactMax + 1;
                for (int i = 0; i < n; i++)
                {
                    _victoryBars.ForceHalf = false;
                    _worstBars[i] = _victoryBars.WorstHeight(i);
                    _victoryBars.ForceHalf = true;
                    _worstBars[n + i] = _victoryBars.WorstHeight(i);
                }

                _victoryBars.ForceHalf = keep;
            }

            int step = Mathf.Clamp(level, 0, VictoryCompactMax);
            return _worstBars[(_victoryBars.ForceHalf ? VictoryCompactMax + 1 : 0) + step];
        }

        private void StreakLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            if (!_victoryBars.Root.gameObject.activeSelf)
            {
                return;
            }

            if (_victoryBars.StreakRect.gameObject.activeSelf)
            {
                ids.Add("streak_bar");
                rects.Add(_victoryBars.StreakRect);
            }

            if (_victoryBars.BasketRect.gameObject.activeSelf)
            {
                ids.Add("basket_bar");
                rects.Add(_victoryBars.BasketRect);
            }
        }

        // Centro de rt en dp del lienzo (x desde la izquierda, y desde arriba).
        private Vector2 CanvasCenterDp(RectTransform rt)
        {
            CanvasRectDp(rt, out float left, out float top, out float right, out float bottom);
            return new Vector2((left + right) * 0.5f, (top + bottom) * 0.5f);
        }
    }
}
