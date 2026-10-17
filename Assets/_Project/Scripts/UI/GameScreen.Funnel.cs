#nullable enable
using AF.GameFlow;

namespace AF.UI
{
    /// <summary>
    /// Parcial del embudo de D-008 (arquitectura del paso 1, §7; tarea T08): viste las cabeceras de ¿Seguro? y Nivel fallido con
    /// la etiqueta del nivel (propuesta-mecanicas-dificultad.md §c.1, D-009: ámbar en Difícil, índigo en Muy difícil y la de
    /// siempre en normal) sin editar GameScreen.cs. La etiqueta sale de <c>difficulty.label</c> del nivel, nunca del premio:
    /// Remote Config puede igualar dos tramos de <c>eco_reward_*</c>.
    /// <para><b>TiTino en Nivel fallido</b> (tarea TI6b; propuesta de TiTino d.5; D-016, puntos 4 y 5; arquitectura de la tanda 4,
    /// §3: el patrón SetLabel de <see cref="LevelFailedScreen"/>): solo en esa pantalla del embudo, nunca en Aguas estancadas,
    /// ¿Seguro? ni Sin canoas. Al abrirla, <see cref="SlothLines.ForFail"/> decide con la sesión de TiTino
    /// (<see cref="SlothSession"/>, la misma de la victoria): <c>sloth_encourage</c> con <c>ui_sloth_enabled</c> y
    /// <c>ui_sloth_fail_enabled</c>, y la frase solo en el primer fallo del nivel (<c>fails_level</c> = 1), con los topes de la
    /// sesión y si cabe en la tarjeta (<see cref="LevelFailedScreen.PhraseFits"/>: con la escala de texto puede no caber entre
    /// el pie del HUD y <c>safeBottom</c>, y entonces solo el gesto). Sin la Crecida en el cliente, el fallo nunca la rompe
    /// [P]: la frase n.º 21 llegará con ella. <c>stats.fails_hard</c> aún no lo cuenta nadie: el banco rota con el nivel [P].
    /// Lo enseñado se apunta en la sesión (también el gesto sin frase, que corta la racha de «dos pantallas seguidas»). Si
    /// Nivel fallido se vuelve a abrir en el mismo fallo (Sin canoas → «Llenar canoas» → Nivel fallido), TiTino repite lo que
    /// hizo, sin elegir otra frase ni apuntarla otra vez.</para>
    /// </summary>
    public sealed partial class GameScreen
    {
        private int _slothFailLevel;
        private int _slothFailCount = -1;
        private SlothGesture _slothFailGesture;
        private string? _slothFailText;

        /// <summary>Lo que decidió TiTino en el último Nivel fallido (default antes del primero; tests).</summary>
        internal SlothLine LastSlothFailLine { get; private set; }

        // Gancho de T04: GameScreen.cs lo llama justo antes de cada _giveUp.Show y _failed.Show (al abrir el paso del embudo y al
        // reanudar el nivel guardado con el modal abierto).
        partial void FunnelModalShowing(FlowPhase phase, LevelFlow flow)
        {
            string label = flow.Level.DifficultyLabel;
            if (phase == FlowPhase.GiveUpConfirm)
            {
                _giveUp.SetLabel(label);
            }
            else if (phase == FlowPhase.LevelFailed)
            {
                _failed.SetLabel(label);
                SlothFailShowing(flow);
            }
        }

        // TiTino en el Nivel fallido que se va a abrir (ver la clase): decide con SlothLines.ForFail y se lo da a la tarjeta.
        private void SlothFailShowing(LevelFlow flow)
        {
            int level = flow.LevelNumber;
            int fails = flow.Attempt.FailsLevel;
            _failed.SetBounds(BoardTop, _canvasH - _safeBottom); // del pie de la fila del HUD (el saldo del embudo) a safeBottom
            if (level == _slothFailLevel && fails == _slothFailCount)
            {
                _failed.SetSloth(_slothFailGesture, _slothFailText, flow.ReduceMotion); // el mismo fallo, otra vez a la vista
                return;
            }

            _slothFailLevel = level;
            _slothFailCount = fails;
            _slothFailGesture = SlothGesture.None;
            _slothFailText = null;
            LastSlothFailLine = default;
            if (!SlothEnabled || !_failed.EnsureSlothArt())
            {
                _failed.SetSloth(SlothGesture.None, null, flow.ReduceMotion); // apagado o sin su arte (release): no sale
                return;
            }

            SlothSession session = SlothSession;
            session.Messages = _slothVoiceOverride ?? SlothMessagesSetting;
            bool first = fails == 1;
            SlothContext context = session.Context.WithFail(new SlothFailFacts(first, false, 0, true));
            SlothLine line = SlothLines.ForFail(level, in context);
            string? text = null;
            if (line.Speaks)
            {
                if (UiText.TrySlothLine(in line, out string candidate) && _failed.PhraseFits(candidate, _canvasW, _canvasH))
                {
                    text = candidate;
                }
                else
                {
                    // Sin texto que se pueda enseñar o sin sitio en la tarjeta: solo el gesto (d.5), por la misma regla.
                    context = session.Context.WithFail(new SlothFailFacts(first, false, 0, false));
                    line = SlothLines.ForFail(level, in context);
                }
            }

            _slothFailGesture = line.Gesture;
            _slothFailText = text;
            _failed.SetSloth(line.Gesture, text, flow.ReduceMotion);
            if (line.Gesture != SlothGesture.None)
            {
                session.Record(in line, in context);
            }

            LastSlothFailLine = line;
            EmitSlothFailProbe(level, line, text != null);
        }

        // AF_STATE sloth_fail: nivel, gesto, clave de la frase (o null) y si va la frase. Solo con la sonda; al abrir el modal.
        private void EmitSlothFailProbe(int level, in SlothLine line, bool phrase)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            var sb = new System.Text.StringBuilder(128);
            sb.Append("\"level\":").Append(level.ToString(System.Globalization.CultureInfo.InvariantCulture));
            AppendJsonString(sb, "gesture", line.Gesture == SlothGesture.None ? "none" : AF.Presentation.SlothRig.IdOf(AF.Presentation.SlothPoseId.Encourage));
            if (line.Key != null)
            {
                AppendJsonString(sb, "line", line.Key);
            }

            sb.Append(",\"phrase\":").Append(phrase ? "true" : "false");
            _probe.Event("sloth_fail", sb.ToString());
        }
    }
}
