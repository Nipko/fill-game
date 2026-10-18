#nullable enable
using System.Globalization;
using AF.Boosters;
using AF.GameFlow;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Parcial de Morpho · Pista (tarea MO2; 04 §2.3; 07 §7.4 y §7.6; 02 T5; D-019; D-002: ayuda explícita), con los ganchos
    /// <c>Hint*</c> de GameScreen.cs y los avisos de <see cref="IHintListener"/> (MO1):
    /// <list type="bullet">
    /// <item><description><b>Bandeja</b>: la ranura Pista (<see cref="BoosterTray"/>) existe desde <c>hint_unlock_level</c>,
    /// tras Cuia extra y antes de Revelar; su acción es <see cref="IGameActions.PressHint"/>. Sin <c>hint_supported</c>, el toque
    /// enseña el globo «La morpho no conoce este tramo todavía» (<see cref="ShowHintUnsupported"/>); sin stock, saldo ni video,
    /// late su burbuja «Te faltan {n}» (<see cref="ShowHintMissing"/>).</description></item>
    /// <item><description><b>Vertido</b> (<see cref="OnHint"/> con <see cref="HintAnswer.Pour"/>, ya cobrado): la mariposa vuela
    /// del botón al origen en 0,4 s, salta al destino y las dos cuias laten con su anillo 3 s o hasta que el flujo apaga la pista
    /// (<see cref="OnHintCleared"/>: un vertido, sea cual sea, u otra acción). Con Reducir movimiento, sin vuelo
    /// (<see cref="MorphoFx"/>).</description></item>
    /// <item><description><b>Respuestas sin vertido</b> (no cobran): la tarjeta <see cref="HintAnswerCard"/> con la frase y
    /// Deshacer y Volver a la orilla, que hacen lo mismo que la ranura de Marea y el botón de la cabecera (con su confirmación).
    /// Se cierra con la X, tocando fuera o si el nivel deja de estar en juego.</description></item>
    /// </list>
    /// Sin asignaciones por fotograma: las imágenes se crean una vez y los sprites de la mariposa, al cargar un nivel con la ranura
    /// (<see cref="MorphoFx.Prewarm"/>); la sonda (<c>DevProbe.Event("hint", …)</c>) solo escribe en desarrollo, en las respuestas.
    /// Se llama GameScreen.Morpho.cs (revisión de MO2): <c>GameScreen.Hint.cs</c> queda libre para la pista por inactividad de
    /// MO3, que lo crea como archivo nuevo.
    /// </summary>
    public sealed partial class GameScreen : IHintListener
    {
        private MorphoFx _morphoFx = null!;
        private HintAnswerCard _hintCard = null!;
        private object? _hintCardBoard;

        /// <summary>La mariposa y los anillos de la pista (tests y capturas).</summary>
        internal MorphoFx MorphoFx => _morphoFx;

        /// <summary>La tarjeta de las respuestas sin vertido (tests y capturas).</summary>
        internal HintAnswerCard HintAnswerCard => _hintCard;

        /// <inheritdoc />
        public void OnHintThinking(bool thinking)
        {
            if (thinking)
            {
                _hintCard.Hide();
                _tray.HideHintTooltip();
            }
        }

        /// <inheritdoc />
        public void OnHint(in HintResult result)
        {
            LevelFlow flow = RequireFlow();
            _hintCard.Hide();
            _tray.HideHintTooltip();
            if (result.HasMove)
            {
                AF.Audio.AudioCues.Play(AF.Audio.SfxId.Hint); // aleteo y campanilla (04 §2.3)
                _morphoFx.Play(HintButtonPoint(), result.Move.Source, result.Move.Target, _views, flow.ReduceMotion, HintCeilingY());
                HintProbe("pour", flow, result);
                return;
            }

            _morphoFx.Snap();
            UndoStatus undo = flow.UndoStatus;
            bool undoEnabled = undo.HasQuota ? flow.CanUndo : flow.AcceptsBoardInput;
            _hintCard.Show(result.Answer, result.Tinaja, undoEnabled, _canvasW, _canvasH);
            _hintCardBoard = flow.Session.Current;
            EmitHintLayout(flow);
            HintProbe("answer", flow, result);
        }

        /// <inheritdoc />
        public void OnHintCleared()
        {
            _morphoFx.Clear(); // otro vertido o cualquier cambio: los anillos se apagan (04 §2.3 d)
        }

        /// <summary>
        /// Pista en un nivel sin <c>hint_supported</c> (04 §2.3 b): el globo «La morpho no conoce este tramo todavía» sobre la
        /// ranura gris.
        /// </summary>
        internal void ShowHintUnsupported()
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel)
            {
                return;
            }

            _tray.ShowHintTooltip();
            if (DevProbe.Enabled)
            {
                _probe.Event("hint", "\"phase\":\"unsupported\",\"level\":" + flow.LevelNumber.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Pista sin stock, sin saldo y sin video (como Revelar: pregunta 48 (b), D-022) [P]: ningún aviso; late la burbuja «Te
        /// faltan {n}» de la ranura (con Reducir movimiento, solo el color).
        /// </summary>
        internal void ShowHintMissing(int missing)
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel)
            {
                return;
            }

            _tray.Refresh();
            _tray.PulseHintMissing(missing, flow.ReduceMotion);
        }

        partial void HintBuild()
        {
            _tray.SetHintAction(() => _actions.PressHint());
            _morphoFx = new MorphoFx(_mechanicFrontLayer, _mechanicBackLayer, _art); // la estela, detrás de las cuias
            _hintCard = new HintAnswerCard(_root, _art, OnHintCardUndo, OnHintCardRestart, OnHintCardClose);
        }

        partial void HintLayout()
        {
            _hintCard.Layout(_canvasW, _canvasH);
        }

        partial void HintTick(float dtMs)
        {
            _morphoFx.Tick(dtMs, _views);
            if (!_hintCard.Visible)
            {
                return;
            }

            // La tarjeta solo vale en juego y con el tablero en que respondió Morpho.
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel || flow.Phase != FlowPhase.Playing || !ReferenceEquals(flow.Session.Current, _hintCardBoard))
            {
                _hintCard.Hide();
            }
        }

        partial void HintLevelLoaded(LevelFlow flow)
        {
            // Vistas nuevas (BuildBoard): nada de la pista anterior se pinta en ellas.
            _morphoFx.Snap();
            _hintCard.Hide();
            _hintCardBoard = null;
            if (flow.Hint.State != HintState.Hidden)
            {
                _morphoFx.Prewarm(); // la mariposa genera ya sus sprites: ni el toque ni el vuelo crean texturas
            }
        }

        private void OnHintCardUndo()
        {
            _hintCard.Hide();
            OnTrayUndo(); // como la ranura de Marea: deshace con cupo o stock; sin nada, recarga
        }

        private void OnHintCardRestart()
        {
            _hintCard.Hide();
            OnHudRestart(); // como la cabecera: con ≥ 8 vertidos, su confirmación (04 §2.7)
        }

        private void OnHintCardClose()
        {
            _hintCard.Hide();
        }

        private void EmitHintLayout(LevelFlow flow)
        {
            _layoutIds.Clear();
            _layoutRects.Clear();
            _hintCard.VisibleButtons(_layoutIds, _layoutRects);
            _probe.Layout("hint_answer", flow.LevelNumber, null, _layoutIds.ToArray(), _layoutRects.ToArray());
        }

        // Punto de salida de la mariposa: lo alto del botón Pista, en las coordenadas del contenedor del tablero.
        private Vector2 HintButtonPoint()
        {
            var rt = (RectTransform)_tray.HintButton.transform;
            Rect r = rt.rect;
            return BoardPoint(rt.TransformPoint(new Vector3(r.center.x, r.yMax - 10f, 0f)));
        }

        // El canto de arriba del marco del tablero (y hacia arriba, coordenadas del contenedor): el salto de la mariposa no lo pasa.
        private float HintCeilingY()
        {
            return _layout != null ? -_layout.PanelY : float.PositiveInfinity;
        }

        private void HintProbe(string phase, LevelFlow flow, in HintResult result)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            CultureInfo inv = CultureInfo.InvariantCulture;
            string fields = "\"phase\":\"" + phase + "\",\"level\":" + flow.LevelNumber.ToString(inv)
                + ",\"answer\":" + ((int)result.Answer).ToString(inv)
                + ",\"from\":" + (result.HasMove ? result.Move.Source : -1).ToString(inv)
                + ",\"to\":" + (result.HasMove ? result.Move.Target : -1).ToString(inv)
                + ",\"tinaja\":" + (result.Tinaja ? "true" : "false");
            _probe.Event("hint", fields);
        }
    }
}
