#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Pantalla de nivel del prototipo v0 construida por código sobre un Canvas de 360 dp de ancho de referencia
    /// (GDD 02 §5; 07 §7.4): fondo, banda de río, tablero, HUD (nivel, Volver a la orilla, bandeja con Marea), modal
    /// Aguas estancadas (02 §7; 07 §7.5) y pantalla de victoria (02 §6). Escucha a <see cref="LevelFlow"/> y hace que
    /// la vista persiga al modelo (02 §3.2).
    /// </summary>
    public sealed class GameScreen : ILevelFlowListener
    {
        /// <summary>Ancho de referencia del lienzo (dp).</summary>
        public const float ReferenceWidth = 360f;

        /// <summary>Alto de referencia del lienzo (dp; 19,5:9).</summary>
        public const float ReferenceHeight = 780f;

        /// <summary>Zona segura superior mínima (02 §5).</summary>
        public const float MinSafeTop = 32f;

        /// <summary>Zona segura inferior mínima (02 §5).</summary>
        public const float MinSafeBottom = 24f;

        private const float PanelSlideMs = 250f;
        private const float OverlayFadeMs = 200f;

        /// <summary>Alto de la banda de río en reposo (07 §7.4): ocupa el pie del tablero (y = 560-620 a 360 × 780).</summary>
        private const float RiverBandDp = 60f;

        /// <summary>Panel Aguas estancadas: 07 §7.5 fija 300 dp con Cuia de rescate; antes del 31 solo lleva Deshacer y el enlace.</summary>
        private const float StuckPanelDp = 220f;

        /// <summary>Panel de victoria de v0: título y Siguiente (sin Cosecha, Crecida ni Canasta, fuera de v0).</summary>
        private const float VictoryPanelDp = 160f;

        private readonly ProceduralArt _art;
        private readonly DevProbe _probe;
        private readonly Action<int> _onCuia;
        private readonly Action _onBackground;
        private readonly Action _onAnywhere;
        private readonly Action _onUndo;
        private readonly Action _onRestart;
        private readonly Action _onNext;

        private readonly Canvas _canvas;
        private readonly CanvasScaler _scaler;
        private readonly RectTransform _root;
        private readonly Image _background;
        private readonly Image _river;
        private readonly RectTransform _board;
        private readonly RectTransform _streamLayer;
        private readonly RectTransform _hud;
        private readonly Text _levelText;
        private readonly Button _restartButton;
        private readonly Image _tray;
        private readonly Button _undoButton;
        private readonly RectTransform _confettiLayer;
        private readonly RectTransform _skipCatcher;

        private readonly RectTransform _stuckOverlay;
        private readonly Image _stuckDim;
        private readonly RectTransform _stuckPanel;
        private readonly Button _stuckUndo;
        private readonly Button _stuckRestart;

        private readonly RectTransform _victoryOverlay;
        private readonly Image _victoryDim;
        private readonly RectTransform _victoryPanel;
        private readonly Button _nextButton;

        private readonly BoardAnimator _animator;
        private readonly IntroAnimation _intro = new IntroAnimation();
        private readonly ConfettiLayer _confetti;
        private readonly Action<int> _cuiaDown;
        private readonly Action<int, bool> _cuiaUp;

        private LevelFlow? _flow;
        private int _pressPreview = -1;
        private CuiaView[] _views = Array.Empty<CuiaView>();
        private BoardState? _shownBoard;
        private BoardLayoutResult? _layout;
        private float _canvasW = ReferenceWidth;
        private float _canvasH = ReferenceHeight;
        private float _safeTop = MinSafeTop;
        private float _safeBottom = MinSafeBottom;
        private float _stuckPanelY;
        private float _victoryPanelY;
        private int _screenW;
        private int _screenH;
        private Rect _safeArea;
        private float _stuckAnim = -1f;
        private float _victoryAnim = -1f;
        private float _riverTop;
        private float _boardShift;
        private float _boardShiftFrom;
        private float _boardShiftTarget;
        private float _boardShiftAnim = -1f;
        private float _confettiDelay = -1f;
        private bool _restartProbePending;
        private bool _undoEnabledShown = true;

        /// <summary>
        /// Construye la pantalla bajo <paramref name="host"/>. Las acciones son los puntos de entrada del input
        /// (los mismos que usan los tests PlayMode).
        /// </summary>
        public GameScreen(
            Transform host,
            ProceduralArt art,
            DevProbe probe,
            Action<int> onCuia,
            Action onBackground,
            Action onAnywhere,
            Action onUndo,
            Action onRestart,
            Action onNext)
        {
            _art = art;
            _probe = probe;
            _onCuia = onCuia;
            _onBackground = onBackground;
            _onAnywhere = onAnywhere;
            _onUndo = onUndo;
            _onRestart = onRestart;
            _onNext = onNext;
            _cuiaDown = OnCuiaDown;
            _cuiaUp = OnCuiaUp;

            var canvasGo = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(host, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.pixelPerfect = false;
            _scaler = canvasGo.GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _scaler.referencePixelsPerUnit = 100f;
            _root = (RectTransform)canvasGo.transform;

            _background = UiBuilder.Image("Background", _root, art.Background, Color.white);
            UiBuilder.Stretch(_background.rectTransform);

            _river = UiBuilder.Image("RiverBand", _root, art.RiverBand, Color.white);

            // C7 (02 §3.1): el toque en el fondo deselecciona al soltar, como el resto de decisiones.
            RectTransform bgCatcher = HitLayer("BackgroundCatcher", _root);
            bgCatcher.gameObject.AddComponent<PointerTapRelay>().Bind(() => _onBackground());

            _board = UiBuilder.Rect("Board", _root);
            _streamLayer = UiBuilder.Rect("Streams", _board);

            _hud = UiBuilder.Rect("Hud", _root);
            _levelText = UiBuilder.Text("LevelTitle", _hud, art.Font, UiText.Level(1), 28, Palette.UiPaper, TextAnchor.MiddleCenter, FontStyle.Bold);
            _restartButton = UiBuilder.BareButton("RestartButton", _hud, () => _onRestart());
            // Volver a la orilla: icono de 40 dp en la fila superior (07 §7.4, §7.6) dentro de su área táctil de 48 dp.
            Image restartIcon = UiBuilder.Image("Icon", _restartButton.transform, art.IconRestart, Color.white);
            UiBuilder.Place(restartIcon.rectTransform, 4f, 0f, 40f, 40f);
            Text restartLabel = UiBuilder.Text("Label", _restartButton.transform, art.Font, UiText.Restart, 12, Palette.UiPaper, TextAnchor.UpperCenter, FontStyle.Normal);
            UiBuilder.Place(restartLabel.rectTransform, -16f, 40f, 80f, 16f);

            // Bandeja cumbuca de 328 × 80 (07 §7.4) con la ranura de Marea dibujada como "piedra de río" de 64 × 72
            // (07 §7.3): lo que parece botón es exactamente el área táctil, y la bandeja queda lista para Cuia extra (8).
            _tray = UiBuilder.Rounded("Tray", _hud, art, Palette.UiWood, 18f);
            _undoButton = UiBuilder.BareButton("UndoButton", _tray.transform, () => _onUndo());
            Image undoStoneEdge = UiBuilder.Rounded("StoneEdge", _undoButton.transform, art, Palette.StoneEdge, 14f);
            UiBuilder.Place(undoStoneEdge.rectTransform, 0f, 6f, 64f, 72f);
            Image undoStone = UiBuilder.Rounded("Stone", _undoButton.transform, art, Palette.UiPaper, 14f);
            UiBuilder.Place(undoStone.rectTransform, 0f, 4f, 64f, 72f);
            Image undoIcon = UiBuilder.Image("Icon", _undoButton.transform, art.IconUndo, Color.white);
            UiBuilder.Place(undoIcon.rectTransform, 12f, 8f, 40f, 40f);
            Text undoLabel = UiBuilder.Text("Label", _undoButton.transform, art.Font, UiText.Undo, 12, Palette.UiInk, TextAnchor.UpperCenter, FontStyle.Bold);
            UiBuilder.Place(undoLabel.rectTransform, -8f, 52f, 80f, 16f);

            _confettiLayer = UiBuilder.Rect("Confetti", _root);
            _confetti = new ConfettiLayer(_confettiLayer, art.Feather, GameConfig.AnimConfettiMaxParticles);

            // Aguas estancadas (02 §7; 07 §7.5): tablero atenuado detrás; panel de papel anclado abajo.
            _stuckOverlay = UiBuilder.Rect("StuckOverlay", _root);
            _stuckDim = UiBuilder.Image("Dim", _stuckOverlay, null, new Color(0f, 0f, 0f, 0f), true);
            UiBuilder.Stretch(_stuckDim.rectTransform);
            _stuckPanel = UiBuilder.Rect("Panel", _stuckOverlay);
            Image stuckFrame = UiBuilder.Rounded("Frame", _stuckPanel, art, Palette.UiWood, 16f, true);
            UiBuilder.Stretch(stuckFrame.rectTransform);
            Image stuckPaper = UiBuilder.Rounded("Paper", _stuckPanel, art, Palette.UiPaper, 14f);
            UiBuilder.Stretch(stuckPaper.rectTransform);
            stuckPaper.rectTransform.offsetMin = new Vector2(2f, 2f);
            stuckPaper.rectTransform.offsetMax = new Vector2(-2f, -2f);
            Text stuckTitle = UiBuilder.Text("Title", _stuckPanel, art.Font, UiText.StuckTitle, 22, Palette.UiInk, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiBuilder.Place(stuckTitle.rectTransform, 0f, 28f, 328f, 30f);
            // Subtítulo en un token opaco (07 §7.2): ui_madera sobre papel ≈ 7:1 (≥ 4,5:1, 02 §12). Nunca alfa en textos.
            Text stuckSubtitle = UiBuilder.Text("Subtitle", _stuckPanel, art.Font, UiText.StuckSubtitle, 14, Palette.UiWood, TextAnchor.MiddleCenter, FontStyle.Normal);
            UiBuilder.Place(stuckSubtitle.rectTransform, 0f, 62f, 328f, 20f);
            _stuckUndo = UiBuilder.StoneButton("UndoModalButton", _stuckPanel, art, Palette.UiWood, () => _onUndo());
            Text stuckUndoLabel = UiBuilder.Text("Label", _stuckUndo.transform, art.Font, UiText.StuckUndo, 16, Palette.UiPaper, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiBuilder.Stretch(stuckUndoLabel.rectTransform);
            _stuckRestart = UiBuilder.BareButton("RestartModalButton", _stuckPanel, () => _onRestart());
            Text stuckRestartLabel = UiBuilder.Text("Label", _stuckRestart.transform, art.Font, UiText.StuckRestart, 16, Palette.UiWood, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiBuilder.Stretch(stuckRestartLabel.rectTransform);
            _stuckOverlay.gameObject.SetActive(false);

            // Victoria (02 §6; 07 §5.4): el panel sube desde abajo y se queda anclado abajo, con las cuias selladas visibles
            // encima y Siguiente en la zona del pulgar. Sin Semillas ni Crecida en v0; sin auto-avance (victory_autonext_s = 0).
            _victoryOverlay = UiBuilder.Rect("VictoryOverlay", _root);
            _victoryDim = UiBuilder.Image("Dim", _victoryOverlay, null, new Color(0f, 0f, 0f, 0f), true);
            UiBuilder.Stretch(_victoryDim.rectTransform);
            _victoryPanel = UiBuilder.Rect("Panel", _victoryOverlay);
            Image victoryFrame = UiBuilder.Rounded("Frame", _victoryPanel, art, Palette.UiWood, 16f, true);
            UiBuilder.Stretch(victoryFrame.rectTransform);
            Image victoryPaper = UiBuilder.Rounded("Paper", _victoryPanel, art, Palette.UiPaper, 14f);
            UiBuilder.Stretch(victoryPaper.rectTransform);
            victoryPaper.rectTransform.offsetMin = new Vector2(2f, 2f);
            victoryPaper.rectTransform.offsetMax = new Vector2(-2f, -2f);
            Text victoryTitle = UiBuilder.Text("Title", _victoryPanel, art.Font, UiText.LevelComplete, 28, Palette.UiInk, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiBuilder.Place(victoryTitle.rectTransform, 0f, 22f, 328f, 40f);
            _nextButton = UiBuilder.StoneButton("NextButton", _victoryPanel, art, Palette.UiGreenButton, () => _onNext());
            Text nextLabel = UiBuilder.Text("Label", _nextButton.transform, art.Font, UiText.Next, 16, Palette.UiPaper, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiBuilder.Stretch(nextLabel.rectTransform);
            _victoryOverlay.gameObject.SetActive(false);

            // Captura de toques para saltar Sumergir y teñir o la celebración (02 §4.2, §2): por encima de todo.
            _skipCatcher = HitLayer("SkipCatcher", _root);
            _skipCatcher.gameObject.AddComponent<PointerDownRelay>().Bind(() => _onAnywhere());
            _skipCatcher.gameObject.SetActive(false);

            _animator = new BoardAnimator(_streamLayer);
            ApplyScreenMetrics(true);
        }

        /// <summary>
        /// Pantalla simulada en píxeles (ancho, alto, inserto superior e inferior de la zona segura) para las capturas
        /// visuales de prueba en el Editor; null = <see cref="Screen"/> real. No se usa en el juego.
        /// </summary>
        public static Vector4? SimulatedScreen { get; set; }

        /// <summary>La cola visual está vacía (02 §3.2).</summary>
        public bool IsViewIdle => _animator.IsIdle;

        /// <summary>Lienzo de la pantalla (capturas de prueba).</summary>
        public Canvas Canvas => _canvas;

        /// <summary>Vistas de las cuias del nivel actual.</summary>
        public CuiaView[] Views => _views;

        /// <summary>Asocia el flujo (antes de cargar el primer nivel).</summary>
        public void Bind(LevelFlow flow)
        {
            _flow = flow;
        }

        /// <summary>Avanza la vista <paramref name="dtMs"/> milisegundos.</summary>
        public void Tick(float dtMs)
        {
            ReadScreen(out int sw, out int sh, out Rect safe);
            if (sw != _screenW || sh != _screenH || safe != _safeArea)
            {
                ApplyScreenMetrics(false);
            }

            if (_intro.Running)
            {
                _intro.Tick(dtMs);
            }

            _animator.Tick(dtMs);
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].Tick(dtMs);
            }

            if (_confettiDelay >= 0f)
            {
                _confettiDelay -= dtMs;
                if (_confettiDelay < 0f)
                {
                    _confettiDelay = -1f;
                    _confetti.Play(GameConfig.AnimConfettiMaxParticles);
                }
            }

            _confetti.Tick(dtMs);
            TickBoardShift(dtMs);
            TickPanels(dtMs);
            RefreshHud();
        }

        /// <summary>Vuelta de segundo plano (02 §14): la vista salta al modelo sin animar.</summary>
        public void SnapToModel()
        {
            _animator.SnapAll();
            _intro.Complete();
        }

        /// <inheritdoc />
        public void OnLevelLoaded(int level)
        {
            LevelFlow flow = RequireFlow();
            _animator.SnapAll();
            _intro.Complete();
            _confetti.Stop();
            _confettiDelay = -1f;
            HideOverlays();
            BuildBoard(flow);
            _levelText.text = UiText.Level(level);
            _pressPreview = -1;
            _shownBoard = flow.Session.Current;
            _probe.State("load", flow, int.MinValue, null);
            EmitLevelLayout();
        }

        /// <inheritdoc />
        public void OnIntroStarted(int durationMs, bool isShort)
        {
            LevelFlow flow = RequireFlow();
            _animator.SnapAll();
            ClearLifts();
            HideOverlays();
            _shownBoard = flow.Session.Current;
            _intro.Start(_views, flow.Session.Current, _art.Water, _river.rectTransform, durationMs);
            _skipCatcher.gameObject.SetActive(true);
            if (_restartProbePending)
            {
                _restartProbePending = false;
                _probe.State("restart", flow, int.MinValue, null);
            }
        }

        /// <inheritdoc />
        public void OnIntroFinished(bool skipped)
        {
            _intro.Complete();
            _skipCatcher.gameObject.SetActive(false);
            _probe.State("ready", RequireFlow(), int.MinValue, null);
        }

        /// <inheritdoc />
        public void OnTap(TapResult result)
        {
            LevelFlow flow = RequireFlow();
            switch (result.Kind)
            {
                case TapKind.Selected:
                    SetLift(result.PreviousSelection, false);
                    SetLift(result.Selection, true);
                    HighlightTargets(flow, result.Selection);
                    _probe.State("select", flow, result.Tapped, null);
                    break;

                case TapKind.Deselected:
                    SetLift(result.PreviousSelection, false);
                    HighlightTargets(flow, -1);
                    _probe.State("deselect", flow, result.Tapped, null);
                    break;

                case TapKind.Rejected:
                    SetLift(result.PreviousSelection, false);
                    if ((uint)result.Tapped < (uint)_views.Length)
                    {
                        _views[result.Tapped].Shake();
                    }

                    HighlightTargets(flow, -1);
                    _probe.State("reject", flow, result.Tapped, null);
                    break;

                case TapKind.Poured:
                    HighlightTargets(flow, -1);
                    SetLift(result.PreviousSelection, false);
                    EnqueuePour(flow, result.Pour.Pour);
                    _probe.State("pour", flow, result.Tapped, result.Pour.Pour);
                    break;
            }
        }

        /// <inheritdoc />
        public void OnUndo(Pour undone, bool fromStuckModal)
        {
            LevelFlow flow = RequireFlow();
            _animator.SnapAll();
            ClearLifts();
            HideOverlays(true);
            BoardState before = _shownBoard ?? flow.Session.Current;
            BoardState after = flow.Session.Current;
            _animator.EnqueueUndo(
                undone.Target,
                undone.Source,
                undone.Amount,
                before[undone.Target].TopColor,
                before[undone.Target],
                after[undone.Target],
                after.IsSealed(undone.Target),
                before[undone.Source],
                after[undone.Source],
                after.IsSealed(undone.Source));
            _shownBoard = after;
            _probe.State("undo", flow, int.MinValue, undone);
        }

        /// <inheritdoc />
        public void OnRestart(bool fromStuckModal)
        {
            _animator.SnapAll();
            ClearLifts();
            HideOverlays();
            _shownBoard = RequireFlow().Session.Current;
            _restartProbePending = true;
        }

        /// <inheritdoc />
        public void OnWin()
        {
            LevelFlow flow = RequireFlow();
            ClearLifts();
            HighlightTargets(flow, -1);
            _probe.State("win", flow, int.MinValue, null);
        }

        /// <inheritdoc />
        public void OnStuck()
        {
            LevelFlow flow = RequireFlow();
            ClearLifts();
            HighlightTargets(flow, -1);
            _probe.State("stuck", flow, int.MinValue, null);
        }

        /// <inheritdoc />
        public void OnCelebrationStarted()
        {
            // 07 §5.4: los sellos brillan en secuencia (60 ms entre cuias) con un salto en ola; confeti a los 0,5 s.
            _animator.SnapAll();
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].PlayCelebration(i * 60f);
            }

            _confettiDelay = 500f;
            _skipCatcher.gameObject.SetActive(true);
        }

        /// <inheritdoc />
        public void OnVictoryScreen()
        {
            _skipCatcher.gameObject.SetActive(false);
            _victoryOverlay.gameObject.SetActive(true);
            ShiftBoard(ShiftToClear(_victoryPanelY, 8f), true);
            _victoryAnim = 0f;
            ApplyVictoryAnim(0f);
        }

        /// <inheritdoc />
        public void OnStuckModalOpened()
        {
            _animator.SnapAll();
            _stuckOverlay.gameObject.SetActive(true);
            ShiftBoard(ShiftToClear(_stuckPanelY, 12f), true); // 07 §5.5: las cuias siguen visibles sobre el modal
            _stuckAnim = 0f;
            ApplyStuckAnim(0f);
        }

        // 02 §3.1: la elevación empieza en el touch-down (0 ms percibidos) si, sin selección, la cuia puede ser origen
        // (lo decide el núcleo); el toque se decide en el touch-up.
        private void OnCuiaDown(int index)
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.AcceptsBoardInput || flow.Selected != TapController.NoSelection || _pressPreview >= 0)
            {
                return;
            }

            if ((uint)index < (uint)_views.Length && PourRules.CanBeSource(flow.Session.Current, index))
            {
                _pressPreview = index;
                _views[index].SetLifted(true);
            }
        }

        // Soltar dentro del área táctil es un toque (C1-C6); soltar fuera cancela (C10, el arrastre, queda fuera de v0).
        private void OnCuiaUp(int index, bool inside)
        {
            if (inside)
            {
                _onCuia(index);
            }

            if (_pressPreview == index)
            {
                _pressPreview = -1;
                if ((uint)index < (uint)_views.Length && (_flow == null || _flow.Selected != index))
                {
                    _views[index].SetLifted(false);
                }
            }
        }

        private LevelFlow RequireFlow()
        {
            return _flow ?? throw new InvalidOperationException("GameScreen sin LevelFlow: llama a Bind antes de cargar.");
        }

        private void BuildBoard(LevelFlow flow)
        {
            for (int i = 0; i < _views.Length; i++)
            {
                if (_views[i] != null)
                {
                    UnityEngine.Object.Destroy(_views[i].gameObject);
                }
            }

            BoardState board = flow.Session.Current;
            var views = new CuiaView[board.Count];
            float liftMs = GameConfig.AnimLiftMs * GameConfig.SpeedFactor(flow.LevelNumber);
            for (int i = 0; i < board.Count; i++)
            {
                views[i] = CuiaView.Create(_board, i, board[i].Capacity, _art, _cuiaDown, _cuiaUp);
                views[i].SetLiftDuration(liftMs);
                views[i].ShowContent(board[i], board.IsSealed(i));
            }

            _views = views;
            _streamLayer.SetAsLastSibling();
            float f = GameConfig.SpeedFactor(flow.LevelNumber);
            var timings = new PourTimings(
                GameConfig.AnimLiftMs * f,
                GameConfig.AnimTravelMs * f,
                GameConfig.AnimPourDoseMs * f,
                GameConfig.AnimPourTailMs * f,
                GameConfig.AnimReturnMs * f);
            _animator.Bind(_views, timings, GameConfig.QueueMaxLagPours);
            LayoutBoard();
        }

        private void EnqueuePour(LevelFlow flow, Pour pour)
        {
            BoardState after = flow.Session.Current;
            BoardState before = _shownBoard ?? after;
            _animator.EnqueuePour(
                pour.Source,
                pour.Target,
                pour.Amount,
                before[pour.Source].TopColor,
                before[pour.Source],
                after[pour.Source],
                after.IsSealed(pour.Source),
                before[pour.Target],
                after[pour.Target],
                after.IsSealed(pour.Target));
            _shownBoard = after;
        }

        private void SetLift(int index, bool lifted)
        {
            if ((uint)index < (uint)_views.Length)
            {
                _views[index].SetLifted(lifted);
            }
        }

        private void ClearLifts()
        {
            _pressPreview = -1;
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].SetLifted(false);
                _views[i].SetTargetHighlight(false);
            }
        }

        // Destinos legales del origen seleccionado (07 §4.3); la legalidad la decide el núcleo.
        private void HighlightTargets(LevelFlow flow, int source)
        {
            BoardState state = flow.Session.Current;
            for (int i = 0; i < _views.Length; i++)
            {
                bool on = source >= 0 && i != source && PourRules.IsLegal(state, source, i);
                _views[i].SetTargetHighlight(on);
            }
        }

        // animateBoard: el tablero vuelve a su sitio con la misma curva que usó para subir (Marea desde el modal); si no,
        // vuelve al instante (carga, intro y reinicio, que recolocan la vista de todos modos).
        private void HideOverlays(bool animateBoard = false)
        {
            ShiftBoard(0f, animateBoard);
            _stuckOverlay.gameObject.SetActive(false);
            _victoryOverlay.gameObject.SetActive(false);
            _stuckAnim = -1f;
            _victoryAnim = -1f;
        }

        private void RefreshHud()
        {
            bool undo = _flow != null && _flow.CanUndo;
            if (undo != _undoEnabledShown)
            {
                _undoEnabledShown = undo;
                UiBuilder.SetEnabled(_undoButton, undo);
                UiBuilder.SetEnabled(_stuckUndo, undo);
            }
        }

        private void TickPanels(float dtMs)
        {
            if (_stuckAnim >= 0f)
            {
                _stuckAnim += dtMs;
                ApplyStuckAnim(_stuckAnim);
                if (_stuckAnim >= PanelSlideMs)
                {
                    _stuckAnim = -1f;
                    _probe.Layout("stuck", RequireFlow().LevelNumber, null, new[] { "undo_modal", "restart_modal" }, new[] { (RectTransform)_stuckUndo.transform, (RectTransform)_stuckRestart.transform });
                }
            }

            if (_victoryAnim >= 0f)
            {
                _victoryAnim += dtMs;
                ApplyVictoryAnim(_victoryAnim);
                if (_victoryAnim >= PanelSlideMs)
                {
                    _victoryAnim = -1f;
                    _probe.Layout("victory", RequireFlow().LevelNumber, null, new[] { "next" }, new[] { (RectTransform)_nextButton.transform });
                }
            }
        }

        private void ApplyStuckAnim(float ms)
        {
            // 07 §5.5: el tablero queda visible y atenuado al 70 %; el modal sube desde abajo en 0,25 s.
            float fade = Mathf.Clamp01(ms / OverlayFadeMs);
            _stuckDim.color = new Color(0.04f, 0.06f, 0.05f, 0.30f * fade);
            float slide = 1f - Ease.Out(ms / PanelSlideMs);
            _stuckPanel.anchoredPosition = new Vector2(_stuckPanel.anchoredPosition.x, -(_stuckPanelY + (slide * (StuckPanelDp + 40f))));
        }

        private void ApplyVictoryAnim(float ms)
        {
            // Atenuado suave: las cuias selladas son la recompensa del nivel y siguen a la vista (02 §6; 07 §5.4).
            float fade = Mathf.Clamp01(ms / OverlayFadeMs);
            _victoryDim.color = new Color(0.02f, 0.04f, 0.03f, 0.15f * fade);
            float slide = 1f - Ease.Out(ms / PanelSlideMs);
            _victoryPanel.anchoredPosition = new Vector2(_victoryPanel.anchoredPosition.x, -(_victoryPanelY + (slide * (VictoryPanelDp + 40f))));
        }

        private void EmitLevelLayout()
        {
            _probe.Layout("level", RequireFlow().LevelNumber, _views, new[] { "undo", "restart" }, new[] { (RectTransform)_undoButton.transform, (RectTransform)_restartButton.transform });
        }

        private void ApplyScreenMetrics(bool initial)
        {
            ReadScreen(out _screenW, out _screenH, out _safeArea);

            // Lienzo de al menos 360 × 780 dp (02 §5): 360 de ancho en 19,5:9 y 20:9 (360 × 800).
            float scale = Mathf.Min(_screenW / ReferenceWidth, _screenH / ReferenceHeight);
            scale = Mathf.Max(0.01f, scale);
            _scaler.scaleFactor = scale;
            _canvasW = _screenW / scale;
            _canvasH = _screenH / scale;
            float insetTop = (_screenH - _safeArea.yMax) / scale;
            float insetBottom = _safeArea.yMin / scale;
            _safeTop = Mathf.Max(MinSafeTop, insetTop);
            _safeBottom = Mathf.Max(MinSafeBottom, insetBottom);

            if (!initial)
            {
                _animator.SnapAll(); // las poses de vertido en curso se calcularon con el layout anterior
            }

            LayoutHud();
            LayoutBoard();
            if (_intro.Running)
            {
                _intro.RefreshRiverRest();
            }

            if (!initial && _flow != null && _flow.HasLevel)
            {
                EmitLevelLayout();
            }
        }

        // Posiciones de 02 §5 y 07 §7.4 (dp desde arriba): fila superior en safe + 8 (alto 48); barra de evento colapsada;
        // bandeja de 80 dp que termina 16 dp por encima de la zona segura inferior; 40 dp entre tablero y bandeja.
        private float TopRowY => _safeTop + 8f;

        private float BoardTop => TopRowY + 48f;

        private float TrayBottom => _canvasH - _safeBottom - 16f;

        private float TrayTop => TrayBottom - 80f;

        private float BoardBottom => TrayTop - 40f;

        private void LayoutHud()
        {
            float w = _canvasW;
            UiBuilder.Place(_levelText.rectTransform, 60f, TopRowY, w - 120f, 48f);
            UiBuilder.Place((RectTransform)_restartButton.transform, w - 8f - 48f, TopRowY, 48f, 48f);

            float trayW = Mathf.Min(328f, w - 32f);
            UiBuilder.Place(_tray.rectTransform, (w - trayW) / 2f, TrayTop, trayW, 80f);
            UiBuilder.Place((RectTransform)_undoButton.transform, (trayW - 64f) / 2f, 0f, 64f, 80f);

            // Paneles anclados abajo (07 §5.4, §5.5, §7.5), con los CTA en la zona del pulgar (02 §3.3).
            float panelW = Mathf.Min(328f, w - 32f);
            _stuckPanelY = _canvasH - _safeBottom - 8f - StuckPanelDp;
            UiBuilder.Place(_stuckPanel, (w - panelW) / 2f, _stuckPanelY, panelW, StuckPanelDp);
            UiBuilder.Place((RectTransform)_stuckUndo.transform.parent, (panelW - 280f) / 2f, 96f, 280f, 48f);
            UiBuilder.Place((RectTransform)_stuckRestart.transform, (panelW - 280f) / 2f, StuckPanelDp - 12f - 48f, 280f, 48f); // enlace al pie (07 §7.5)

            _victoryPanelY = _canvasH - _safeBottom - 8f - VictoryPanelDp;
            UiBuilder.Place(_victoryPanel, (w - panelW) / 2f, _victoryPanelY, panelW, VictoryPanelDp);
            UiBuilder.Place((RectTransform)_nextButton.transform.parent, (panelW - 280f) / 2f, VictoryPanelDp - 24f - 56f, 280f, 56f);
        }

        // 07 §7.4: el tablero ocupa y = 120-620 con la banda de río de 60 dp al fondo; las filas se alinean por el pie y
        // la última apoya el pie en el centro de la banda, así el tablero queda junto a la bandeja (zona del pulgar) y el
        // hueco libre queda arriba, bajo la cabecera, en lugar de entre el río y la bandeja.
        private float FootLine => BoardBottom - (RiverBandDp / 2f);

        private void LayoutBoard()
        {
            if (_views.Length == 0)
            {
                PlaceRiver(FootLine);
                return;
            }

            float heightU = 0f;
            for (int i = 0; i < _views.Length; i++)
            {
                heightU = Mathf.Max(heightU, _views[i].HeightU);
            }

            _layout = BoardLayout.Compute(_views.Length, heightU, _canvasW, BoardTop, FootLine);
            float lastFoot = 0f;
            for (int i = 0; i < _views.Length; i++)
            {
                float foot = _layout.FootY[i];
                _views[i].SetGeometry(new Vector2(_layout.CenterX[i], -foot), _layout.CuiaWidth, _layout.SlotWidth);
                lastFoot = Mathf.Max(lastFoot, foot);
            }

            PlaceRiver(lastFoot);
        }

        // Banda de río de 60 dp (07 §7.4) bajo la última fila: las cuias caen hasta ella en la intro (07 §5.1).
        private void PlaceRiver(float footY)
        {
            _riverTop = footY - (RiverBandDp / 2f);
            UiBuilder.Place(_river.rectTransform, 0f, _riverTop - _boardShift, _canvasW, RiverBandDp);
        }

        // Con el tablero anclado abajo, un panel anclado abajo (modal Aguas estancadas, victoria) taparía la última fila:
        // el tablero y su banda de río suben lo justo para quedar por encima del panel (07 §5.5: "las cuias siguen
        // visibles"; 02 §6), sin pasar de la cabecera. Desplazar el contenedor no cambia las coordenadas internas que usa
        // la cola de animación.
        private float ShiftToClear(float panelTop, float gap)
        {
            if (_layout == null || _views.Length == 0)
            {
                return 0f;
            }

            float lowestFoot = 0f;
            float highestTop = float.MaxValue;
            for (int i = 0; i < _views.Length; i++)
            {
                lowestFoot = Mathf.Max(lowestFoot, _layout.FootY[i]);
                highestTop = Mathf.Min(highestTop, _layout.FootY[i] - (_views[i].HeightU * _views[i].Unit) - CuiaView.LiftDp);
            }

            float needed = lowestFoot - (panelTop - gap);
            float room = highestTop - BoardTop;
            return Mathf.Clamp(needed, 0f, Mathf.Max(0f, room));
        }

        private void ShiftBoard(float target, bool animate)
        {
            _boardShiftTarget = target;
            if (!animate || Mathf.Approximately(_boardShift, target))
            {
                _boardShiftAnim = -1f;
                SetBoardShift(target);
                return;
            }

            _boardShiftFrom = _boardShift;
            _boardShiftAnim = 0f;
        }

        private void TickBoardShift(float dtMs)
        {
            if (_boardShiftAnim < 0f)
            {
                return;
            }

            _boardShiftAnim += dtMs;
            float t = Mathf.Clamp01(_boardShiftAnim / PanelSlideMs);
            SetBoardShift(Mathf.Lerp(_boardShiftFrom, _boardShiftTarget, Ease.Out(t)));
            if (t >= 1f)
            {
                _boardShiftAnim = -1f;
            }
        }

        private void SetBoardShift(float shift)
        {
            _boardShift = shift;
            _board.anchoredPosition = new Vector2(0f, shift);
            UiBuilder.Place(_river.rectTransform, 0f, _riverTop - shift, _canvasW, RiverBandDp);
        }

        private static void ReadScreen(out int width, out int height, out Rect safeArea)
        {
            if (SimulatedScreen.HasValue)
            {
                Vector4 sim = SimulatedScreen.Value;
                width = Mathf.Max(1, (int)sim.x);
                height = Mathf.Max(1, (int)sim.y);
                safeArea = new Rect(0f, sim.w, width, height - sim.z - sim.w);
                return;
            }

            width = Mathf.Max(1, Screen.width);
            height = Mathf.Max(1, Screen.height);
            safeArea = Screen.safeArea;
        }

        private static RectTransform HitLayer(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(HitArea));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            UiBuilder.Stretch(rt);
            go.GetComponent<HitArea>().raycastTarget = true;
            return rt;
        }
    }
}
