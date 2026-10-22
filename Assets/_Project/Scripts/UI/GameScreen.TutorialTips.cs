#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Boosters;
using AF.Core.Session;
using AF.GameFlow;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Las ayudas del tutorial de la tarea B11 (reglas en <see cref="TutorialTips"/>; 02 §13 y 06 §2.2), que la pantalla de nivel
    /// enseña mirando su propio estado en cada fotograma (<see cref="TutorialTipsTick"/>), sin ganchos nuevos en el flujo:
    /// <list type="bullet">
    /// <item><description><b>«Reiniciar es gratis…»</b> (6 y 8): un tooltip sin personaje (<see cref="TipBubble"/>) encima del
    /// panel de Aguas estancadas, con el pico hacia abajo en el eje de «Volver a la orilla (gratis)» [P] (encima del botón taparía
    /// Marea, la primera opción del modal). Una vez por nivel mientras la pantalla de nivel exista (la app abierta): la primera vez
    /// que el modal se ve quieto en ese nivel, no en los reintentos ni al volver a entrar [P]; se va con el modal. Con
    /// <c>biz_feature_lives</c> apagado, solo la primera frase (D-038; <see cref="TutorialTips.RestartTipKey"/>).</description></item>
    /// <item><description><b>Cosecha doble</b> (11): en la victoria, encima del botón «Cosecha doble: x2 por video», con el pico
    /// hacia él, si el botón sale y el panel ya subió; se va al pulsarlo, al desactivarse o al dejar la victoria. Una vez por nivel
    /// mientras la app siga abierta; si en esa victoria no hay anuncio, no sale (Q-B11-12).</description></item>
    /// <item><description><b>Dedo guía de la Cuia extra</b> (8): la mano de <see cref="TrayFinger"/> sobre «+1 cuia» mientras se
    /// juega, la bandeja ofrece la del stock, no se aplicó ninguna en el nivel y no hay otra cosa a la vista (el aviso de Capi y
    /// los demás avisos, una tarjeta, la pausa, Morpho, la Luciérnaga o el dedo del tablero). Se va al usarla.</description></item>
    /// </list>
    /// Sondas <c>AF_STATE tutorial_tip</c> (<c>tip</c> = <c>restart</c>, <c>double_harvest</c> o <c>extra_cuia_finger</c>,
    /// <c>visible</c> y la clave) en cada cambio, y los rectángulos <c>tip_restart</c>, <c>tip_double_harvest</c> y
    /// <c>extra_cuia_finger</c> en <c>AF_LAYOUT</c>.
    /// </summary>
    public sealed partial class GameScreen
    {
        /// <summary>Ancho máximo de los tooltips (dp) [P]: el de los avisos de texto a 360 dp, menos los márgenes.</summary>
        public const float TipMaxWidthDp = 296f;

        /// <summary>Margen de los tooltips con los bordes del lienzo (dp) [P].</summary>
        public const float TipEdgeDp = 12f;

        private TipBubble? _restartTip;
        private TipBubble? _doubleTip;
        private TrayFinger? _trayFinger;
        // Las ayudas que ya salieron (una vez por nivel mientras exista la pantalla, es decir, con la app abierta): un bit por nivel
        // de TutorialTips.RestartTipLevels y el nivel de la Cosecha doble que ya la presentó.
        private int _restartTipShownMask;
        private int _doubleTipShownLevel = -1;
        private Vector2 _restartTipAnchor = new Vector2(float.NaN, float.NaN);
        private Vector2 _doubleTipAnchor = new Vector2(float.NaN, float.NaN);
        private string? _lastTipProbe;

        /// <summary>Solo tests: enciende o apaga las ayudas de B11 (null = encendidas, como en el juego).</summary>
        internal static bool? TutorialTipsOverride { get; set; }

        /// <summary>El tooltip «Reiniciar es gratis…» (null hasta el primero; tests).</summary>
        internal TipBubble? RestartTip => _restartTip;

        /// <summary>El tooltip de la Cosecha doble (null hasta el primero; tests).</summary>
        internal TipBubble? DoubleHarvestTip => _doubleTip;

        /// <summary>El dedo guía de la Cuia extra (null hasta el primero; tests).</summary>
        internal TrayFinger? ExtraCuiaFinger => _trayFinger;

        /// <summary>El tooltip «Reiniciar es gratis…» se ve (tests).</summary>
        internal bool RestartTipVisible => _restartTip != null && _restartTip.Visible && !_restartTip.Leaving;

        /// <summary>El tooltip de la Cosecha doble se ve (tests).</summary>
        internal bool DoubleHarvestTipVisible => _doubleTip != null && _doubleTip.Visible && !_doubleTip.Leaving;

        /// <summary>El dedo de la Cuia extra se ve (tests).</summary>
        internal bool ExtraCuiaFingerVisible => _trayFinger != null && _trayFinger.Shown;

        /// <summary>Campos de la última sonda <c>AF_STATE tutorial_tip</c> (null si no salió ninguna o sin sonda; tests).</summary>
        internal string? LastTutorialTipProbe => _lastTipProbe;

        // Cada fotograma (desde CardsTick): enseña, coloca o retira las tres ayudas. Sin asignaciones salvo al enseñar una (el
        // texto y sus medidas) y en las sondas (solo en desarrollo).
        private void TutorialTipsTick(float dtMs)
        {
            LevelFlow? flow = _flow;
            bool on = (TutorialTipsOverride ?? true) && flow != null && flow.HasLevel;
            TickRestartTip(on ? flow : null, dtMs);
            TickDoubleHarvestTip(on ? flow : null, dtMs);
            TickExtraCuiaFinger(on ? flow : null, dtMs);
        }

        // Al cargar un nivel (desde CardsLevelLoaded): nada de lo del nivel anterior, con su sonda si se veía; las marcas de «ya
        // salió» se quedan (una vez por nivel con la app abierta).
        private void TutorialTipsLevelLoaded(LevelFlow flow)
        {
            if (_restartTip != null && _restartTip.Visible)
            {
                bool leaving = _restartTip.Leaving; // si ya se iba, su sonda visible:false ya salió
                _restartTip.Hide();
                if (!leaving)
                {
                    EmitTipProbe("restart", false, null);
                }
            }

            if (_doubleTip != null && _doubleTip.Visible)
            {
                bool leaving = _doubleTip.Leaving;
                _doubleTip.Hide();
                if (!leaving)
                {
                    EmitTipProbe("double_harvest", false, null);
                }
            }

            if (_trayFinger != null && _trayFinger.Shown)
            {
                _trayFinger.Hide();
                EmitTipProbe("extra_cuia_finger", false, null);
            }
        }

        // El bit del nivel en TutorialTips.RestartTipLevels (0 si no es uno de ellos). Sin asignaciones.
        private static int RestartTipBit(int level)
        {
            int[] levels = TutorialTips.RestartTipLevels;
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == level)
                {
                    return 1 << i;
                }
            }

            return 0;
        }

        // AF_LAYOUT: los tooltips y la mano, si se ven.
        private void TutorialTipsLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            if (RestartTipVisible)
            {
                ids.Add("tip_restart");
                rects.Add(_restartTip!.Rect);
            }

            if (DoubleHarvestTipVisible)
            {
                ids.Add("tip_double_harvest");
                rects.Add(_doubleTip!.Rect);
            }

            if (ExtraCuiaFingerVisible)
            {
                ids.Add("extra_cuia_finger");
                rects.Add(_trayFinger!.Hand.rectTransform);
            }
        }

        private void TickRestartTip(LevelFlow? flow, float dtMs)
        {
            bool modalReady = flow != null && !flow.IsDaily && TutorialTips.ShowsRestartTip(flow.LevelNumber)
                && flow.Phase == FlowPhase.StuckModal && StuckVisible && _stuckAnim < 0f && !_pause.Visible
                && _stuck.RestartButton.gameObject.activeInHierarchy;
            if (_restartTip != null && _restartTip.Visible)
            {
                if (!modalReady)
                {
                    if (!_restartTip.Leaving)
                    {
                        _restartTip.Leave(flow == null || flow.ReduceMotion);
                        EmitTipProbe("restart", false, null);
                    }
                }
                else if (!_restartTip.Leaving)
                {
                    PlaceRestartTip(false);
                }

                _restartTip.Tick(dtMs);
                return;
            }

            int bit = modalReady ? RestartTipBit(flow!.LevelNumber) : 0;
            if (bit == 0 || (_restartTipShownMask & bit) != 0)
            {
                return;
            }

            _restartTipShownMask |= bit;
            bool lives = _services == null || _services.Wallet.LivesEnabled;
            string key = TutorialTips.RestartTipKey(lives);
            _restartTip ??= new TipBubble(_stuckOverlay, _art, "RestartTip");
            _restartTip.Show(TutorialTipsText.Tip(key), flow.ReduceMotion);
            PlaceRestartTip(true);
            EmitTipProbe("restart", true, key);
        }

        // Encima del panel del modal, con la punta del pico en su canto de arriba y en el eje de «Volver a la orilla».
        private void PlaceRestartTip(bool force)
        {
            TipBubble tip = _restartTip!;
            RectTransform space = _stuckOverlay;
            Vector2 button = TopLeftOf(space, WorldCenter((RectTransform)_stuck.RestartButton.transform));
            Vector2 panelTop = TopLeftOf(space, WorldTop(_stuck.Panel));
            var anchor = new Vector2(button.x, panelTop.y);
            if (!force && anchor == _restartTipAnchor)
            {
                return;
            }

            _restartTipAnchor = anchor;
            tip.PlaceAbove(anchor.x, anchor.y, TipEdgeDp, space.rect.width - TipEdgeDp, _safeTop + TipEdgeDp, TipMaxWidthDp);
        }

        private void TickDoubleHarvestTip(LevelFlow? flow, float dtMs)
        {
            bool buttonReady = flow != null && TutorialTips.ShowsDoubleHarvestTip(flow.LevelNumber, flow.DoubleHarvestFromLevel, flow.IsDaily)
                && flow.Phase == FlowPhase.VictoryScreen && _victoryAnim >= PanelSlideMs && DoubleHarvestShown && _double.Button.interactable;
            if (_doubleTip != null && _doubleTip.Visible)
            {
                if (!buttonReady)
                {
                    if (!_doubleTip.Leaving)
                    {
                        _doubleTip.Leave(flow == null || flow.ReduceMotion);
                        EmitTipProbe("double_harvest", false, null);
                    }
                }
                else if (!_doubleTip.Leaving)
                {
                    PlaceDoubleHarvestTip(false);
                }

                _doubleTip.Tick(dtMs);
                return;
            }

            if (!buttonReady || _doubleTipShownLevel == flow!.LevelNumber)
            {
                return;
            }

            _doubleTipShownLevel = flow.LevelNumber;
            _doubleTip ??= new TipBubble(_victoryOverlay, _art, "DoubleHarvestTip");
            _doubleTip.Show(TutorialTipsText.Tip(TutorialTips.DoubleHarvestKey), flow.ReduceMotion);
            PlaceDoubleHarvestTip(true);
            EmitTipProbe("double_harvest", true, TutorialTips.DoubleHarvestKey);
        }

        // Encima del botón de la Cosecha doble, con la punta del pico en su canto de arriba y en su eje.
        private void PlaceDoubleHarvestTip(bool force)
        {
            TipBubble tip = _doubleTip!;
            RectTransform space = _victoryOverlay;
            var button = (RectTransform)_double.Button.transform;
            Vector2 center = TopLeftOf(space, WorldCenter(button));
            Vector2 top = TopLeftOf(space, WorldTop(button));
            var anchor = new Vector2(center.x, top.y);
            if (!force && anchor == _doubleTipAnchor)
            {
                return;
            }

            _doubleTipAnchor = anchor;
            tip.PlaceAbove(anchor.x, anchor.y, TipEdgeDp, space.rect.width - TipEdgeDp, _safeTop + TipEdgeDp, TipMaxWidthDp);
        }

        private void TickExtraCuiaFinger(LevelFlow? flow, float dtMs)
        {
            bool show = flow != null && ExtraCuiaFingerDue(flow);
            if (_trayFinger != null && _trayFinger.Shown)
            {
                if (!show)
                {
                    _trayFinger.Hide();
                    EmitTipProbe("extra_cuia_finger", false, null);
                    return;
                }

                _trayFinger.Tick(dtMs);
                return;
            }

            if (!show)
            {
                return;
            }

            _trayFinger ??= new TrayFinger(_root, _art);
            _trayFinger.Show((RectTransform)_tray.ExtraCuiaButton.transform, flow!.ReduceMotion);
            EmitTipProbe("extra_cuia_finger", true, null);
        }

        // El dedo de la Cuia extra tiene que verse ahora (TutorialTips.ShowsExtraCuiaFinger, con la vista libre). Sin asignaciones.
        private bool ExtraCuiaFingerDue(LevelFlow flow)
        {
            if (flow.Phase != FlowPhase.Playing || !flow.AcceptsBoardInput || flow.IsResolving || flow.Session.Status != LevelStatus.Playing)
            {
                return false;
            }

            ExtraCuiaOffer offer = flow.ExtraCuia;
            if (offer.State != ExtraCuiaState.Ready || !_tray.ExtraCuiaShown
                || !TutorialTips.ShowsExtraCuiaFinger(flow.LevelNumber, flow.Boosters.ExtraCuiaUnlockLevel, flow.IsDaily, flow.Attempt.ExtraCuias, offer.Stock))
            {
                return false;
            }

            // Ninguna otra cosa a la vista: el aviso de Capi y los demás (o en cola), la tarjeta de TiTino, la pausa, Morpho, la
            // Luciérnaga, el dedo del tablero, la tropa de la intro o el rótulo del intento; y con la vista quieta.
            return _noticeCount == 0 && !NoticeShowing && !_pause.Visible && !_confirm.Visible && !flow.RevealSelecting && !flow.HintThinking
                && !(_morphoFx != null && _morphoFx.Active) && !(_hintCard != null && _hintCard.Visible) && !TutorialFingerVisible
                && !_intro.TroopVisible && !_banner.Visible && IsViewIdle;
        }

        // AF_STATE tutorial_tip: la ayuda, si se ve y su clave. Solo con la sonda y en los cambios, nunca por fotograma.
        private void EmitTipProbe(string tip, bool visible, string? key)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            var sb = new StringBuilder(96);
            sb.Append("\"level\":").Append((_flow?.LevelNumber ?? 0).ToString(CultureInfo.InvariantCulture));
            AppendJsonString(sb, "tip", tip);
            sb.Append(",\"visible\":").Append(visible ? "true" : "false");
            AppendJsonString(sb, "key", key);
            _lastTipProbe = sb.ToString();
            _probe.Event("tutorial_tip", _lastTipProbe);
        }

        // Centro de rt en coordenadas de mundo.
        private static Vector3 WorldCenter(RectTransform rt)
        {
            return rt.TransformPoint(rt.rect.center);
        }

        // Centro del canto de arriba de rt en coordenadas de mundo.
        private static Vector3 WorldTop(RectTransform rt)
        {
            Rect r = rt.rect;
            return rt.TransformPoint(new Vector3(r.center.x, r.yMax, 0f));
        }

        // Un punto de mundo en dp de space, arriba a la izquierda y hacia abajo (las coordenadas de UiBuilder.Place).
        private static Vector2 TopLeftOf(RectTransform space, Vector3 world)
        {
            Vector3 local = space.InverseTransformPoint(world);
            Rect r = space.rect;
            return new Vector2(local.x - r.xMin, r.yMax - local.y);
        }
    }
}
