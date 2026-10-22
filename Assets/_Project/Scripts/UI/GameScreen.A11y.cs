#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.GameFlow;
using AF.Platform;
using AF.Presentation;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Accesibilidad de la pantalla del nivel (bloque B9; 02 §3.1 C9 y §12; 07 §3, regla 5, y §7.8). Parcial nuevo de B9: lo
    /// conecta el parche <c>B9-ui.diff</c> (las llamadas desde <c>GameScreen.cs</c> y la creación en <c>GameBootstrap</c>).
    /// <list type="bullet">
    /// <item><b>C9, mantener pulsado</b>: un <see cref="CuiaHoldRelay"/> en cada cuia (<see cref="AttachHold"/>), el seguimiento
    /// (<see cref="CuiaHoldTracker"/>, con el reloj de la pantalla) y el bocadillo con los nombres
    /// (<see cref="CuiaNamesBubble"/>). Al soltar tras los nombres no hay toque ni arrastre (<see cref="HoldSwallows"/>).</item>
    /// <item><b>Alto contraste</b> (<see cref="HighContrast"/>): el patrón del líquido al 35 % (<see cref="ApplyLevelPatterns"/>).</item>
    /// <item><b>Reducir movimiento</b> (02 §4 y §12; 07 §7.8): el factor de las duraciones del vertido
    /// (<see cref="PourMotionFactor"/>, 0,5 de <c>anim_reduce_motion_factor</c>); el parche quita además la sacudida del rechazo,
    /// la onda del agua y el confeti; el resto ya lo lleva <c>LevelFlow.ReduceMotion</c>.</item>
    /// <item><b>Lector de pantalla</b> (<see cref="A11y"/>): esta pantalla es una fuente propia (<see cref="IA11ySource"/>):
    /// con el tablero jugable, las cuias por orden de ranura, después la barra de comodines y después lo demás del lienzo (la
    /// pausa, los saldos…); la selección se anuncia (<see cref="A11yTapped"/>).</item>
    /// </list>
    /// </summary>
    public sealed partial class GameScreen : IA11ySource
    {
        private readonly CuiaHoldTracker _hold = new CuiaHoldTracker();
        private readonly List<Transform> _a11ySkip = new List<Transform>();
        private CuiaNamesBubble? _namesBubble;
        private A11yService? _a11y;
        private bool _highContrast;

        /// <summary>
        /// El lector de pantalla del juego (null: sin él, en los tests que no lo tratan). Al asignarlo, esta pantalla se registra
        /// como fuente de su escáner.
        /// </summary>
        internal A11yService? A11y
        {
            get => _a11y;
            set
            {
                if (ReferenceEquals(_a11y, value))
                {
                    return;
                }

                _a11y?.Scanner.RemoveSource(this);
                _a11y = value;
                _a11y?.Scanner.AddSource(this);
            }
        }

        /// <summary>
        /// «Alto contraste» (07 §3, regla 5; 02 §12; D-033, Q-N4 (a)): el patrón del líquido al 35 %. Se aplica al momento al nivel
        /// cargado y a los siguientes.
        /// </summary>
        internal bool HighContrast
        {
            get => _highContrast;
            set
            {
                if (_highContrast == value)
                {
                    return;
                }

                _highContrast = value;
                LevelFlow? flow = _flow;
                if (flow != null && flow.HasLevel && _views.Length > 0)
                {
                    ApplyLevelPatterns(flow.LevelNumber);
                }
            }
        }

        /// <summary>
        /// <c>anim_reduce_motion_factor</c> (08 §9: 0,5): con «Reducir movimiento» (<c>LevelFlow.ReduceMotion</c>), las duraciones
        /// del vertido se multiplican por él (02 §4). Lo fija <c>GameBootstrap</c> desde Remote Config; se toma al cargar cada nivel.
        /// </summary>
        internal double ReduceMotionFactor { get; set; } = A11yConfig.Defaults.AnimReduceMotionFactor;

        /// <summary>Factor de las duraciones del vertido del nivel de <paramref name="flow"/>: <see cref="ReduceMotionFactor"/> o 1.</summary>
        internal float PourMotionFactor(LevelFlow flow)
        {
            return A11yRules.MotionFactor(flow.ReduceMotion, ReduceMotionFactor);
        }

        /// <summary>El seguimiento de C9 (tests).</summary>
        internal CuiaHoldTracker Hold => _hold;

        /// <summary>Los nombres de C9 a la vista («» sin bocadillo; tests).</summary>
        internal string HoldNamesText => _namesBubble != null ? _namesBubble.Text : string.Empty;

        /// <summary>El bocadillo de C9 está a la vista (tests).</summary>
        internal bool HoldNamesVisible => _namesBubble != null && _namesBubble.Visible;

        /// <summary>
        /// Patrones de fruta del nivel <paramref name="level"/> con el ajuste del jugador y, en «Alto contraste», el patrón al 35 %
        /// (07 §3, reglas 1 y 5). Sustituye a las llamadas a <c>_animator.ApplyPatterns</c> (parche).
        /// </summary>
        internal void ApplyLevelPatterns(int level)
        {
            _animator.ApplyPatterns(PatternsOn(level));
            if (_highContrast)
            {
                LiquidGraphic.ApplyPatternOpacity(A11yRules.PatternOpacity(true, LiquidGraphic.ShaderPatternOpacity));
            }
        }

        bool IA11ySource.Collect(Canvas canvas, A11yScanner scanner, List<A11yItem> into)
        {
            LevelFlow? flow = _flow;
            if (!ReferenceEquals(canvas, _canvas) || flow == null || !flow.HasLevel || !flow.AcceptsBoardInput || _views.Length == 0
                || flow.RevealSelecting)
            {
                return false; // fuera del tablero jugable (intro, celebración, modales): el recorrido genérico
            }

            // 02 §12: el foco recorre las cuias por orden de ranura y después la barra de comodines.
            BoardState state = flow.Session.Current;
            for (int i = 0; i < _views.Length && i < state.Count; i++)
            {
                int index = i;
                CuiaView view = _views[i];
                if (view == null || !view.gameObject.activeInHierarchy)
                {
                    continue;
                }

                into.Add(new A11yItem("cuia_" + i, CuiaSpeech.Label(state, i), AccessibilityRole.Button, view.Body, () => TapFromReader(index))
                {
                    Selected = flow.Selected == i,
                });
            }

            _a11ySkip.Clear();
            _a11ySkip.Add(_board);
            AddTray(into, _tray.UndoButton, TraySpeech.Undo(_tray.UndoLabel.text, flow.UndoStatus));
            AddTray(into, _tray.ExtraCuiaButton, TraySpeech.Extra(_tray.ExtraLabel.text, flow.ExtraCuia));
            AddTray(into, _tray.HintButton, TraySpeech.Hint(_tray.HintLabel.text, flow.Hint));
            AddTray(into, _tray.ShuffleButton, TraySpeech.Shuffle(_tray.ShuffleLabel.text, flow.Shuffle));
            AddTray(into, _tray.RevealButton, _tray.RevealLabel.text);

            // Lo demás del lienzo (la pausa, los saldos, los avisos…), en el orden de lectura, sin repetir lo de arriba.
            scanner.CollectGeneric(canvas, _a11ySkip, into);
            return true;
        }

        // Un botón de la barra de comodines, si se ve, con su descripción; se salta en el recorrido genérico.
        private void AddTray(List<A11yItem> into, Button button, string label)
        {
            _a11ySkip.Add(button.transform);
            if (button.gameObject.activeInHierarchy)
            {
                into.Add(A11yScanner.ButtonItem(button, label.Trim()));
            }
        }

        // Activar una cuia con el lector (doble toque): el mismo toque que con el dedo (C1-C6).
        private bool TapFromReader(int index)
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.AcceptsBoardInput || (uint)index >= (uint)_views.Length)
            {
                return false;
            }

            _actions.TapCuia(index);
            return true;
        }

        /// <summary>
        /// Tras un toque en el tablero (parche: al final de <c>OnTap</c>): con el lector, la selección se anuncia («açaí
        /// seleccionado: elige destino», 02 §12) y el tablero se vuelve a describir.
        /// </summary>
        internal void A11yTapped(LevelFlow flow, TapResult result)
        {
            A11yService? a11y = _a11y;
            if (a11y == null || !a11y.On)
            {
                return;
            }

            if (result.Kind == TapKind.Selected)
            {
                string fruit = CuiaSpeech.TopName(flow.Session.Current, result.Selection);
                if (fruit.Length > 0)
                {
                    a11y.Announce(A11yText.Selected(fruit));
                }
            }

            a11y.Invalidate();
        }

        /// <summary>Pone el escuchador de C9 en la cuia <paramref name="view"/> (parche: al crear cada cuia).</summary>
        internal void AttachHold(CuiaView view)
        {
            CuiaHoldRelay.Attach(view.gameObject, view.Index, OnHoldDown, OnHoldMove, OnHoldUp);
        }

        /// <summary>
        /// El touch-up de la cuia <paramref name="index"/> es el final de un C9 ya mostrado: ni toque ni arrastre (parche: al
        /// principio de <c>OnCuiaUp</c>).
        /// </summary>
        internal bool HoldSwallows(int index)
        {
            return _hold.Swallows(index);
        }

        /// <summary>Avanza C9 (parche: en <c>Tick</c>): a los 400 ms, los nombres.</summary>
        internal void HoldTick(float dtMs)
        {
            if (_hold.Tick(dtMs))
            {
                ShowHoldNames(_hold.Index);
            }
        }

        /// <summary>Olvida C9 y oculta los nombres (parche: al construir el tablero y al volver de segundo plano).</summary>
        internal void HoldReset()
        {
            _hold.Reset();
            _namesBubble?.Hide();
        }

        private void OnHoldDown(int index, int pointer, Vector2 screenPx)
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel || !flow.AcceptsBoardInput || flow.RevealSelecting)
            {
                return; // C9 es del tablero jugable; en la selección de la Luciérnaga el toque elige la cuia
            }

            _hold.Press(index, pointer, ToDp(screenPx));
        }

        private void OnHoldMove(int index, int pointer, Vector2 screenPx)
        {
            _hold.Move(pointer, ToDp(screenPx));
        }

        private void OnHoldUp(int index, int pointer)
        {
            _hold.Release(index, pointer);
            _namesBubble?.Hide();
        }

        private void ShowHoldNames(int index)
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel || (uint)index >= (uint)_views.Length || index >= flow.Session.Current.Count)
            {
                return;
            }

            _namesBubble ??= new CuiaNamesBubble(_topLayer, _art);
            _namesBubble.Show(CuiaSpeech.Names(flow.Session.Current, index), _views[index].Body);
            _probe.State("hold", flow, index, null);
        }

        // px de pantalla → dp del lienzo (CanvasScaler: 1 unidad = 1 dp).
        private Vector2 ToDp(Vector2 screenPx)
        {
            float scale = _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            return screenPx / scale;
        }
    }
}
