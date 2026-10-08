#nullable enable
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Sonda de desarrollo (docs/dev/prototipo-v0.md c): en builds de desarrollo (<see cref="Debug.isDebugBuild"/>)
    /// escribe en el log, en una sola línea JSON con InvariantCulture, <c>AF_LAYOUT</c> (rectángulos táctiles en píxeles
    /// con origen arriba a la izquierda; se toca en el centro) y <c>AF_STATE</c> (eventos del nivel), para que un
    /// agente pueda jugar y verificar el prototipo con <c>adb logcat -s Unity</c> y <c>adb shell input tap</c>.
    /// </summary>
    /// <remarks>
    /// <para>Eventos de <c>AF_STATE</c>: <c>load</c>, <c>ready</c>, <c>pour</c>, <c>reject</c>, <c>undo</c>, <c>restart</c>,
    /// <c>win</c> y <c>stuck</c> (contrato) más <c>select</c> y <c>deselect</c> (ayuda para seguir los toques). Fuera del
    /// nivel (tarea B1-6, <see cref="App"/>): <c>lobby</c>, <c>content_end</c> y <c>no_canoes</c>, con semillas y canoas; y
    /// <c>AF_LAYOUT</c> de esas pantallas con los botones <c>play</c>, <c>canoes</c>, <c>settings</c> y <c>back</c>.</para>
    /// <para>Cada línea lleva <c>"pour_mode"</c> (run por defecto, D-005; single en pruebas), el modo de la sesión del
    /// nivel, y los eventos con vertido (<c>pour</c>, <c>undo</c>) llevan, además de
    /// <c>"move":[origen,destino,cantidad]</c>, la cantidad suelta en <c>"amount"</c> (en <c>run</c>, la racha que cabe;
    /// en <c>single</c>, siempre 1). <c>pours</c> cuenta vertidos aplicados (en <c>single</c>, dosis movidas).</para>
    /// </remarks>
    public sealed class DevProbe
    {
        private readonly StringBuilder _sb = new StringBuilder(1024);
        private readonly Vector3[] _corners = new Vector3[4];

        /// <summary>La sonda escribe (solo en builds de desarrollo y en el Editor).</summary>
        public static bool Enabled => Debug.isDebugBuild;

        /// <summary>En el dispositivo, quita la traza de pila de los <c>Debug.Log</c> para que cada línea sea una sola.</summary>
        public static void Initialize()
        {
            if (Enabled && !Application.isEditor)
            {
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            }
        }

        /// <summary>
        /// Línea <c>AF_STATE</c>: <c>t</c> en ms desde el arranque, nivel, modo de vertido, estado de 02 §2, estado del
        /// modelo, vertidos, selección, toque o vertido del evento (con su cantidad) y cuias de abajo arriba.
        /// </summary>
        public void State(string evt, LevelFlow flow, int tap, Pour? move)
        {
            string? line = FormatState(evt, flow, tap, move);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE</c> de <see cref="State"/>; null si la sonda no escribe o no hay nivel.</summary>
        public string? FormatState(string evt, LevelFlow flow, int tap, Pour? move)
        {
            if (!Enabled || !flow.HasLevel)
            {
                return null;
            }

            BoardState board = flow.Session.Current;
            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"").Append(evt).Append('"');
            sb.Append(",\"t\":").Append(((long)(Time.realtimeSinceStartupAsDouble * 1000.0)).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"level\":").Append(flow.LevelNumber.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"pour_mode\":\"").Append(PourModes.ToKey(flow.Session.PourMode)).Append('"');
            sb.Append(",\"phase\":\"").Append(FlowPhaseNames.Of(flow.Phase)).Append('"');
            sb.Append(",\"status\":\"").Append(StatusName(flow)).Append('"');
            sb.Append(",\"pours\":").Append(flow.Session.AppliedPours.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"selected\":").Append(flow.Selected.ToString(CultureInfo.InvariantCulture));
            if (tap != int.MinValue)
            {
                sb.Append(",\"tap\":").Append(tap.ToString(CultureInfo.InvariantCulture));
            }

            if (move.HasValue)
            {
                Pour p = move.Value;
                sb.Append(",\"move\":[").Append(p.Source.ToString(CultureInfo.InvariantCulture))
                    .Append(',').Append(p.Target.ToString(CultureInfo.InvariantCulture))
                    .Append(',').Append(p.Amount.ToString(CultureInfo.InvariantCulture)).Append(']');
                sb.Append(",\"amount\":").Append(p.Amount.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append(",\"cuias\":[");
            for (int i = 0; i < board.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append('[');
                Cuia cuia = board[i];
                for (int k = 0; k < cuia.Count; k++)
                {
                    if (k > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(cuia.ColorAt(k).ToString(CultureInfo.InvariantCulture));
                }

                sb.Append(']');
            }

            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>
        /// Línea <c>AF_STATE</c> de las pantallas fuera del nivel (tarea B1-6): <c>lobby</c>, <c>content_end</c> y
        /// <c>no_canoes</c>, con el nivel del botón Jugar, si está publicado, su etiqueta, las semillas y las canoas
        /// (<c>canoes</c>, <c>canoes_max</c>, si se ven desde el 31, Canoas infinitas y segundos hasta la próxima).
        /// </summary>
        public void App(string evt, AppFlow app)
        {
            string? line = FormatApp(evt, app);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea de <see cref="App"/>; null si la sonda no escribe.</summary>
        public string? FormatApp(string evt, AppFlow app)
        {
            if (!Enabled)
            {
                return null;
            }

            AF.Economy.CanoesStatus canoes = app.Wallet.Canoes;
            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"").Append(evt).Append('"');
            sb.Append(",\"t\":").Append(((long)(Time.realtimeSinceStartupAsDouble * 1000.0)).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"screen\":\"").Append(AppScreenNames.Of(app.Screen)).Append('"');
            sb.Append(",\"level\":").Append(app.PlayLevel.ToString(CultureInfo.InvariantCulture));
            if (app.OverlayLevel > 0)
            {
                sb.Append(",\"overlay_level\":").Append(app.OverlayLevel.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append(",\"available\":").Append(app.PlayLevelAvailable ? "true" : "false");
            sb.Append(",\"label\":\"").Append(app.PlayLevelLabel).Append('"');
            sb.Append(",\"seeds\":").Append(app.Wallet.Seeds.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"canoes\":").Append(canoes.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"canoes_max\":").Append(canoes.Max.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"canoes_visible\":").Append(app.CanoesVisible ? "true" : "false");
            sb.Append(",\"infinite\":").Append(canoes.IsInfinite ? "true" : "false");
            sb.Append(",\"next_canoe_s\":").Append(((long)canoes.TimeToNext.TotalSeconds).ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Línea <c>AF_LAYOUT</c> de la pantalla <paramref name="screen"/> (<c>level</c>, <c>stuck</c>, <c>victory</c>,
        /// <c>lobby</c>, <c>content_end</c>, <c>no_canoes</c>): tamaño de pantalla, áreas táctiles de las cuias y botones con
        /// su id.
        /// </summary>
        public void Layout(string screen, int level, CuiaView[]? cuias, string[] buttonIds, RectTransform[] buttons)
        {
            if (!Enabled)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_LAYOUT {\"screen\":\"").Append(screen).Append('"');
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"sw\":").Append(Screen.width.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"sh\":").Append(Screen.height.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"cuias\":[");
            if (cuias != null)
            {
                for (int i = 0; i < cuias.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append("{\"i\":").Append(i.ToString(CultureInfo.InvariantCulture));
                    AppendRect(sb, cuias[i].Root);
                    sb.Append('}');
                }
            }

            sb.Append("],\"buttons\":[");
            for (int i = 0; i < buttons.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append("{\"id\":\"").Append(buttonIds[i]).Append('"');
                AppendRect(sb, buttons[i]);
                sb.Append('}');
            }

            sb.Append("]}");
            Debug.Log(sb.ToString());
        }

        private void AppendRect(StringBuilder sb, RectTransform rt)
        {
            rt.GetWorldCorners(_corners);
            float minX = Mathf.Min(_corners[0].x, _corners[2].x);
            float maxX = Mathf.Max(_corners[0].x, _corners[2].x);
            float minY = Mathf.Min(_corners[0].y, _corners[2].y);
            float maxY = Mathf.Max(_corners[0].y, _corners[2].y);
            int x = Mathf.RoundToInt(minX);
            int y = Mathf.RoundToInt(Screen.height - maxY);
            int w = Mathf.RoundToInt(maxX - minX);
            int h = Mathf.RoundToInt(maxY - minY);
            sb.Append(",\"x\":").Append(x.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"y\":").Append(y.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"w\":").Append(w.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"h\":").Append(h.ToString(CultureInfo.InvariantCulture));
        }

        private static string StatusName(LevelFlow flow)
        {
            switch (flow.Session.Status)
            {
                case AF.Core.Session.LevelStatus.Won:
                    return "won";
                case AF.Core.Session.LevelStatus.Stuck:
                    return "stuck";
                default:
                    return "playing";
            }
        }
    }
}
