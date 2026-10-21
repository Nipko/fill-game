#nullable enable
using System.Collections.Generic;
using System.Globalization;
using AF.Boosters;
using AF.Core.Board;
using AF.GameFlow;
using AF.Presentation;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Parcial de la Pororoca · Barajar (bloque B2; 04 §2.4 y §4; 07 §7.4), con los ganchos <c>Shuffle*</c> de GameScreen.cs:
    /// <list type="bullet">
    /// <item><description><b>Bandeja</b>: la ranura Barajar desde el nivel 30, entre Pista y Revelar (la pinta
    /// <see cref="BoosterTray"/> con <c>LevelFlow.Shuffle</c>). El toque: con poca agua, Lluvia o Piraña, el globo que lo dice
    /// (04 §2.4 b y d); sin saldo, el latido de «Te faltan {n}»; con semillas y ≥ 200 [P], la hoja de confirmación de una línea
    /// (<see cref="ShuffleConfirm"/>); con el stock, la ola sin preguntar [P].</description></item>
    /// <item><description><b>La ola</b> (<see cref="OnShuffled"/>, con el tablero ya aplicado, guardado y cobrado): 1,2 s, no
    /// saltable (<see cref="ShuffleWaveFx"/>, que captura los toques). Las cuias que cambian se elevan y, al pasar el frente por
    /// su centro, bajan con su contenido nuevo; las selladas y las fijas no se mueven y quedan delante; una fija que solo se
    /// revela (la tinaja) cambia sin elevarse. Sonido <c>sfx_shuffle</c>. Simplificación [P] frente a 04 §2.4: el líquido no
    /// gira dentro de cada cuia (P-8 de <c>docs/dev/pororoca.md</c>).</description></item>
    /// <item><description><b>Falla</b> (<see cref="OnShuffleFailed"/>): «La ola no pudo remover el río» por la cola de avisos;
    /// no se cobró nada.</description></item>
    /// <item><description><b>Tarjeta del 30</b> (<see cref="ShuffleIntroCard"/>): al habilitarse el toque del primer nivel 30,
    /// con la vista quieta, una vez (<c>LevelFlow.ShuffleCardDue</c>); un toque la cierra.</description></item>
    /// </list>
    /// Sonda de desarrollo <c>AF_STATE {"event":"shuffle",…}</c> con la fase, el método, el par antes y después, los intentos,
    /// las capas reveladas y la vía de pago (04 §11, <c>af_shuffle_result</c> y <c>af_booster_used</c>). Sin asignaciones por
    /// fotograma.
    /// </summary>
    public sealed partial class GameScreen : IShuffleListener
    {
        private readonly Vector3[] _shuffleCorners = new Vector3[4];
        private readonly float[] _shuffleCenters = new float[BoardState.MaxCuias];
        private ShuffleConfirm _shuffleConfirm = null!;
        private ShuffleIntroCard _shuffleCard = null!;
        private ShuffleWaveFx _shuffleWave = null!;
        private BoardState? _shuffleTarget;
        private int _shufflePending;

        /// <summary>
        /// Gancho de los tests PlayMode: null (el juego) = con la Pororoca; false = sin ella en la vista (la ranura no existe, la
        /// bandeja no la pinta y la tarjeta del 30 no sale; el flujo y sus cifras no cambian), para que las pruebas que miden la bandeja de 1-4 ranuras, sus avisos o las capturas sigan como antes de B2
        /// (<c>PlayModeHelpers.NoShuffleUnlessSet</c>). Los tests de la Pororoca la encienden y la vuelven a null en su TearDown.
        /// </summary>
        internal static bool? ShuffleOverride { get; set; }

        /// <summary>Hoja de confirmación (tests y capturas).</summary>
        internal ShuffleConfirm ShuffleConfirm => _shuffleConfirm;

        /// <summary>Tarjeta del nivel 30 (tests y capturas).</summary>
        internal ShuffleIntroCard ShuffleCard => _shuffleCard;

        /// <summary>La ola (tests y capturas).</summary>
        internal ShuffleWaveFx ShuffleWave => _shuffleWave;

        /// <summary>Cuias que la ola aún no ha cambiado (máscara de bits; 0 sin ola).</summary>
        internal int ShufflePendingMask => _shufflePending;

        /// <inheritdoc />
        public void OnShuffled(ShuffleOutcome outcome, ShufflePayment payment)
        {
            LevelFlow flow = RequireFlow();
            _shuffleConfirm.Hide();
            _animator.SnapAll();
            ClearLifts();
            BoardState target = flow.Session.Current;
            BoardState shown = _shownBoard ?? target;
            _shuffleTarget = target;
            _shufflePending = 0;
            float top = float.PositiveInfinity;
            float bottom = float.NegativeInfinity;
            int n = Mathf.Min(_views.Length, target.Count);
            for (int i = 0; i < n; i++)
            {
                CuiaView view = _views[i];
                view.Body.GetWorldCorners(_shuffleCorners);
                Vector3 bl = _root.InverseTransformPoint(_shuffleCorners[0]);
                Vector3 tr = _root.InverseTransformPoint(_shuffleCorners[2]);
                Rect r = _root.rect;
                _shuffleCenters[i] = (0.5f * (bl.x + tr.x)) - r.xMin;
                top = Mathf.Min(top, r.yMax - tr.y);
                bottom = Mathf.Max(bottom, r.yMax - bl.y);
                Cuia before = i < shown.Count ? shown[i] : target[i];
                if (before.Packed == target[i].Packed)
                {
                    continue; // fija o igual: no se mueve (04 §2.4: «las selladas no se mueven»)
                }

                _shufflePending |= 1 << i;
                if (before.WithRevealed(before.HiddenMask).Packed != target[i].Packed)
                {
                    SetLift(i, true); // las cuias no selladas se elevan (04 §2.4 VFX)
                }
            }

            if (float.IsPositiveInfinity(top))
            {
                top = 0.3f * _canvasH;
                bottom = 0.7f * _canvasH;
            }

            _shuffleWave.Play(0f, _canvasW, top - 24f, bottom + 8f, flow.ReduceMotion);
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.Shuffle);
            ShuffleProbe("shuffled", flow, outcome, ShuffleBooster.SourceId(payment));
            LabelsBoardChanged(flow);
            RevealBoardChanged(flow);
        }

        /// <inheritdoc />
        public void OnShuffleFailed(ShuffleOutcome outcome)
        {
            LevelFlow flow = RequireFlow();
            _shuffleConfirm.Hide();
            ShowToast(ShuffleText.Failed);
            ShuffleProbe("failed", flow, outcome, null);
        }

        /// <summary>Termina en el acto la ola en marcha: todas las cuias quedan con su contenido nuevo (otro nivel, el modal…).</summary>
        internal void SettleShuffle()
        {
            FinishShuffleWave();
        }

        partial void ShuffleBuild()
        {
            _tray.SetShuffleAction(OnTrayShuffle);
            _shuffleConfirm = new ShuffleConfirm(_root, _art, OnShuffleConfirm, OnShuffleCancel);
            _shuffleCard = new ShuffleIntroCard(_root, _art, OnShuffleCardDismissed);
            _shuffleWave = new ShuffleWaveFx(_root, _art);
        }

        partial void ShuffleLayout()
        {
            if (_shuffleConfirm.Visible)
            {
                _shuffleConfirm.Show(_shuffleConfirm.Price, _canvasW, _canvasH);
            }

            if (_shuffleCard.Visible)
            {
                _shuffleCard.Show(_canvasW, _canvasH, _flow?.ReduceMotion ?? false);
            }
        }

        partial void ShuffleTick(float dtMs)
        {
            LevelFlow? flow = _flow;
            if (_shuffleWave.Playing)
            {
                bool done = _shuffleWave.Tick(dtMs);
                float front = done ? float.PositiveInfinity : _shuffleWave.FrontX;
                PaintShufflePassed(front);
                if (done)
                {
                    FinishShuffleWave();
                }
            }

            _shuffleCard.Tick(dtMs);
            if (flow == null || !flow.HasLevel)
            {
                return;
            }

            if (_shuffleConfirm.Visible && !flow.AcceptsBoardInput)
            {
                _shuffleConfirm.Hide(); // el tablero dejó de estar en juego: la hoja no cobra nada
            }

            if (ShuffleOverride != false && flow.ShuffleCardDue && flow.AcceptsBoardInput && flow.Phase == FlowPhase.Playing && _animator.IsIdle
                && !_shuffleCard.Visible && !_shuffleWave.Playing)
            {
                _shuffleCard.Show(_canvasW, _canvasH, flow.ReduceMotion);
                flow.MarkShuffleCardShown(); // una vez: también si la app se cierra con la tarjeta abierta
                ShuffleProbe("card", flow, null, null);
                EmitLevelLayout();
            }
        }

        partial void ShuffleLevelLoaded(LevelFlow flow)
        {
            _shuffleWave.Stop();
            _shuffleTarget = null;
            _shufflePending = 0;
            _shuffleConfirm.Hide();
            _shuffleCard.Hide();
        }

        partial void ShuffleBoardChanged(LevelFlow flow)
        {
            if (_shuffleWave.Playing && !ReferenceEquals(flow.Session.Current, _shuffleTarget))
            {
                FinishShuffleWave(); // otra cosa cambió el tablero: la ola termina ya
            }
        }

        partial void ShuffleLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            if (_tray.ShuffleShown)
            {
                ids.Add("shuffle");
                rects.Add((RectTransform)_tray.ShuffleButton.transform);
            }

            _shuffleConfirm.VisibleButtons(ids, rects);
            _shuffleCard.VisibleButtons(ids, rects);
        }

        // Toque en Barajar (04 §2.4).
        private void OnTrayShuffle()
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel || _shuffleWave.Playing || flow.IsResolving)
            {
                return;
            }

            ShuffleOffer offer = flow.Shuffle;
            switch (offer.State)
            {
                case ShuffleState.LowWater:
                    _tray.ShowShuffleTooltip(ShuffleText.LowWater);
                    ShuffleProbe("denied", flow, null, offer.DeniedReason);
                    return;
                case ShuffleState.Unsupported:
                    _tray.ShowShuffleTooltip(ShuffleText.Unsupported);
                    ShuffleProbe("denied", flow, null, offer.DeniedReason);
                    return;
                case ShuffleState.Ready:
                    break;
                default:
                    ShuffleProbe("denied", flow, null, offer.DeniedReason);
                    return;
            }

            if (!offer.CanUse)
            {
                _tray.PulseShuffleMissing(offer.Missing, flow.ReduceMotion);
                ShuffleProbe("denied", flow, null, offer.DeniedReason);
                return;
            }

            if (offer.NeedsConfirmation)
            {
                flow.ClearSelection();
                _shuffleConfirm.Show(offer.Price, _canvasW, _canvasH);
                ShuffleProbe("confirm", flow, null, null);
                EmitLevelLayout();
                return;
            }

            (_actions as IShuffleActions)?.PressShuffle();
        }

        private void OnShuffleConfirm()
        {
            _shuffleConfirm.Hide();
            (_actions as IShuffleActions)?.PressShuffle();
            EmitLevelLayout();
        }

        private void OnShuffleCancel()
        {
            _shuffleConfirm.Hide();
            LevelFlow? flow = _flow;
            if (flow != null && flow.HasLevel)
            {
                ShuffleProbe("cancel", flow, null, null);
            }

            EmitLevelLayout();
        }

        private void OnShuffleCardDismissed()
        {
            _shuffleCard.Hide();
            EmitLevelLayout();
        }

        // Las cuias cuyo centro ya cruzó el frente bajan con su contenido nuevo.
        private void PaintShufflePassed(float front)
        {
            BoardState? target = _shuffleTarget;
            if (target == null || _shufflePending == 0)
            {
                return;
            }

            int n = Mathf.Min(_views.Length, target.Count);
            for (int i = 0; i < n; i++)
            {
                int bit = 1 << i;
                if ((_shufflePending & bit) == 0 || _shuffleCenters[i] > front)
                {
                    continue;
                }

                _shufflePending &= ~bit;
                _animator.ShowContent(i, target[i], target.IsSealed(i));
                SetLift(i, false);
            }
        }

        private void FinishShuffleWave()
        {
            if (_shuffleWave.Playing)
            {
                _shuffleWave.Stop();
            }

            PaintShufflePassed(float.PositiveInfinity);
            if (_shuffleTarget != null)
            {
                _shownBoard = _shuffleTarget;
            }

            _shuffleTarget = null;
            _shufflePending = 0;
        }

        // DevProbe.Event("shuffle", …): fase, nivel, método, par, intentos, revelado y vía o motivo (solo en desarrollo).
        private void ShuffleProbe(string phase, LevelFlow flow, ShuffleOutcome? outcome, string? detail)
        {
            if (!DevProbe.Enabled || !flow.HasLevel)
            {
                return;
            }

            var json = new System.Text.StringBuilder(160);
            json.Append("\"phase\":\"").Append(phase).Append("\",\"level\":").Append(flow.LevelNumber.ToString(CultureInfo.InvariantCulture))
                .Append(",\"used_level\":").Append(flow.ShufflesUsed.ToString(CultureInfo.InvariantCulture))
                .Append(",\"used_today\":").Append(flow.ShufflesToday.ToString(CultureInfo.InvariantCulture));
            if (outcome.HasValue)
            {
                ShuffleOutcome o = outcome.Value;
                json.Append(",\"method\":\"").Append(o.MethodId).Append("\",\"par_before\":").Append(o.ParBefore.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"par_after\":").Append(o.ParAfter.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"tries\":").Append(o.Tries.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"revealed_murky\":").Append(o.Revealed.ToString(CultureInfo.InvariantCulture));
            }

            if (detail != null)
            {
                json.Append(",\"detail\":\"").Append(detail).Append('"');
            }

            _probe.Event("shuffle", json.ToString());
        }
    }
}
