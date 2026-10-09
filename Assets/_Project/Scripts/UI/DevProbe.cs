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
    /// <c>win</c> y <c>stuck</c> (contrato) más <c>select</c> y <c>deselect</c> (ayuda para seguir los toques). Del embudo de
    /// D-008 y los comodines, los de <see cref="Funnel"/> (y <c>resume</c> al reabrir el nivel guardado, B2-5). De la
    /// victoria y los anuncios simulados (B2-4): <c>victory</c>, <c>double_harvest</c> y <c>ad_gate</c>
    /// (<see cref="Victory"/>), <c>ad_check</c> (<see cref="AdCheck"/>), <c>ad</c> y <c>ad_closed</c> (<see cref="Ad"/>) y
    /// <c>purchase_sim</c> (<see cref="Purchase"/>). Fuera del nivel (tarea B1-6, <see cref="App"/>): <c>lobby</c>,
    /// <c>content_end</c> y <c>no_canoes</c>. Todas las del embudo, la victoria, los anuncios, las compras y el inicio llevan
    /// semillas y canoas (B2-6). <c>AF_LAYOUT</c> de esas pantallas con los botones <c>play</c>, <c>canoes</c>,
    /// <c>settings</c>, <c>back</c>, <c>double_harvest</c>, <c>next</c> y <c>home</c>.</para>
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
        /// Línea <c>AF_STATE</c> con el evento de analítica <c>af_anim_skipped</c> (08 §8.2: <c>anim</c>, <c>at_ms</c> y
        /// <c>duration_cfg_ms</c>): hasta que exista <c>AF.Analytics</c>, la sonda lo deja en el log de los builds de desarrollo.
        /// </summary>
        public void AnimSkipped(string anim, LevelFlow flow, int atMs, int durationCfgMs)
        {
            if (!Enabled || !flow.HasLevel)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"af_anim_skipped\",\"anim\":\"").Append(anim).Append('"');
            sb.Append(",\"level\":").Append(flow.LevelNumber.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"at_ms\":").Append(atMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"duration_cfg_ms\":").Append(durationCfgMs.ToString(CultureInfo.InvariantCulture)).Append('}');
            Debug.Log(sb.ToString());
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
        /// Línea <c>AF_PERF</c> con lo que costó escribir el perfil tras una acción del jugador (revisión de B2-5; umbral [P] de
        /// 5 ms por vertido pendiente del dueño): <c>event</c> (p. ej. <c>pour</c>), nivel, escrituras de esa acción (1 con el
        /// lote) y ms de la última. Sirve para medirlo en el teléfono con <c>adb logcat -s Unity</c>.
        /// </summary>
        public void SaveCost(string evt, int level, int writes, double ms)
        {
            if (!Enabled)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_PERF {\"event\":\"").Append(evt).Append('"');
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"writes\":").Append(writes.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"save_ms\":").Append(ms.ToString("0.00", CultureInfo.InvariantCulture)).Append('}');
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Línea <c>AF_STATE</c> del embudo de bloqueo y de los comodines (D-008; tareas B2-1 a B2-3): <c>stuck_modal</c>,
        /// <c>giveup_confirm</c>, <c>level_failed</c>, <c>no_canoes</c>, <c>rescue</c>, <c>rescue_check</c>, <c>extra_cuia</c>,
        /// <c>extra_cuia_gift</c>, <c>fail_hard</c>, <c>fail_hard_canoe</c>, <c>fail_soft</c> y <c>river_lowered</c>, siempre con
        /// semillas y canoas, <c>fails_level</c>, cuias añadidas, el resultado de «la compra cumple», el precio congelado y el
        /// cupo y el stock de Marea.
        /// </summary>
        public void Funnel(string evt, LevelFlow flow)
        {
            string? line = FormatFunnel(evt, flow);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea de <see cref="Funnel"/>; null si la sonda no escribe o no hay nivel.</summary>
        public string? FormatFunnel(string evt, LevelFlow flow)
        {
            if (!Enabled || !flow.HasLevel)
            {
                return null;
            }

            AF.Economy.Wallet? wallet = flow.Wallet;
            AF.Economy.CanoesStatus canoes = wallet?.Canoes ?? default;
            AF.Boosters.UndoStatus undo = flow.UndoStatus;
            AF.Economy.RescueQuote? quote = flow.FrozenRescueQuote;
            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"").Append(evt).Append('"');
            sb.Append(",\"t\":").Append(((long)(Time.realtimeSinceStartupAsDouble * 1000.0)).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"level\":").Append(flow.LevelNumber.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"phase\":\"").Append(FlowPhaseNames.Of(flow.Phase)).Append('"');
            sb.Append(",\"seeds\":").Append((wallet?.Seeds ?? 0).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"canoes\":").Append(canoes.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"infinite\":").Append(canoes.IsInfinite ? "true" : "false");
            sb.Append(",\"fails_level\":").Append(flow.Attempt.FailsLevel.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"relief\":").Append(flow.Attempt.ReliefIndex.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"extra_cuias\":").Append(flow.Attempt.ExtraCuias.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"rescues_level\":").Append(flow.Attempt.Rescues.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"cuias_count\":").Append(flow.Session.Current.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"rescue_check\":\"").Append(flow.RescueCheck.ToString()).Append('"');
            sb.Append(",\"rescue_price\":").Append((quote.HasValue ? quote.Value.Price : 0).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"undo_unlimited\":").Append(undo.Unlimited ? "true" : "false");
            sb.Append(",\"undo_free\":").Append(undo.FreeLeft.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"undo_stock\":").Append(undo.Stock.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"extra_cuia_stock\":").Append((wallet?.Stock.Get(AF.Economy.BoosterKind.ExtraCuia) ?? 0).ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Línea <c>AF_STATE</c> <c>purchase_sim</c> de una compra simulada (D-008 punto 5): SKU, contexto, resultado, nivel y
        /// el saldo después (semillas y canoas).
        /// </summary>
        public void Purchase(AF.IAP.PurchaseResult result, AF.Economy.Wallet? wallet = null)
        {
            string? line = FormatPurchase(result, wallet);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea de <see cref="Purchase"/>; null si la sonda no escribe.</summary>
        public string? FormatPurchase(AF.IAP.PurchaseResult result, AF.Economy.Wallet? wallet)
        {
            if (!Enabled || result == null)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"purchase_sim\"");
            AppendTime(sb);
            sb.Append(",\"sku\":\"").Append(result.Request.Sku).Append('"');
            sb.Append(",\"context\":\"").Append(result.Request.Context.ToString()).Append('"');
            sb.Append(",\"status\":\"").Append(result.Status.ToString()).Append('"');
            sb.Append(",\"level\":").Append(result.Request.Level.ToString(CultureInfo.InvariantCulture));
            if (wallet != null)
            {
                AppendWallet(sb, wallet);
            }
            else if (result.Transaction != null)
            {
                sb.Append(",\"seeds\":").Append(result.Transaction.SeedsBalance.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Línea <c>AF_STATE</c> de la victoria (D-008 punto 3; 02 §6; tarea B2-4): <c>victory</c> (pantalla de victoria),
        /// <c>double_harvest</c> (Cosecha doble cobrada) y <c>ad_gate</c> (se tocó «Siguiente»), con la Cosecha, si se dobló,
        /// si se ofrece la Cosecha doble y el saldo (semillas y canoas).
        /// </summary>
        public void Victory(string evt, LevelFlow flow, bool doubleOffered)
        {
            string? line = FormatVictory(evt, flow, doubleOffered);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea de <see cref="Victory"/>; null si la sonda no escribe o no hay nivel.</summary>
        public string? FormatVictory(string evt, LevelFlow flow, bool doubleOffered)
        {
            if (!Enabled || !flow.HasLevel)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"").Append(evt).Append('"');
            AppendTime(sb);
            sb.Append(",\"level\":").Append(flow.LevelNumber.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"phase\":\"").Append(FlowPhaseNames.Of(flow.Phase)).Append('"');
            sb.Append(",\"harvest\":").Append(flow.HarvestSeeds.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"harvest_base\":").Append(flow.HarvestBase.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"doubled\":").Append(flow.HarvestDoubled ? "true" : "false");
            sb.Append(",\"double_offered\":").Append(doubleOffered ? "true" : "false");
            AppendWallet(sb, flow.Wallet);
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Línea <c>AF_STATE</c> <c>ad_check</c> de AD_GATE (05 §6.3; tarea B2-4): el nivel ganado y la decisión de las reglas
        /// (<c>allowed</c> o <c>blocked:Motivo</c>), con el saldo.
        /// </summary>
        public void AdCheck(int level, AF.Ads.AdDecision decision, AF.Economy.Wallet? wallet)
        {
            if (!Enabled)
            {
                return;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"ad_check\"");
            AppendTime(sb);
            sb.Append(",\"placement\":\"interstitial_post_level\"");
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"decision\":\"").Append(decision.ToString()).Append('"');
            AppendWallet(sb, wallet);
            sb.Append('}');
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Línea <c>AF_STATE</c> de un anuncio simulado (D-008 punto 4; B2-4): <c>ad</c> al mostrarse y <c>ad_closed</c> al
        /// cerrarse, con su tipo (<c>interstitial</c> o <c>rewarded</c>), su ubicación, cómo terminó, el nivel y el saldo.
        /// </summary>
        public void Ad(string evt, AF.Ads.SimulatedAd ad, string outcome, int level, AF.Economy.Wallet? wallet)
        {
            string? line = FormatAd(evt, ad, outcome, level, wallet);
            if (line != null)
            {
                Debug.Log(line);
            }
        }

        /// <summary>Texto de la línea de <see cref="Ad"/>; null si la sonda no escribe.</summary>
        public string? FormatAd(string evt, AF.Ads.SimulatedAd ad, string outcome, int level, AF.Economy.Wallet? wallet)
        {
            if (!Enabled || ad == null)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"").Append(evt).Append('"');
            AppendTime(sb);
            sb.Append(",\"format\":\"").Append(ad.Format == AF.Ads.AdFormat.Interstitial ? "interstitial" : "rewarded").Append('"');
            sb.Append(",\"placement\":\"").Append(AF.Ads.AdPlacements.ToId(ad.Placement)).Append('"');
            sb.Append(",\"outcome\":\"").Append(outcome).Append('"');
            sb.Append(",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture));
            AppendWallet(sb, wallet);
            sb.Append('}');
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

        private static void AppendTime(StringBuilder sb)
        {
            sb.Append(",\"t\":").Append(((long)(Time.realtimeSinceStartupAsDouble * 1000.0)).ToString(CultureInfo.InvariantCulture));
        }

        // Saldo de la línea: semillas, canoas y Canoas infinitas (B2-6: «siempre con semillas y canoas»).
        private static void AppendWallet(StringBuilder sb, AF.Economy.Wallet? wallet)
        {
            AF.Economy.CanoesStatus canoes = wallet?.Canoes ?? default;
            sb.Append(",\"seeds\":").Append((wallet?.Seeds ?? 0).ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"canoes\":").Append(canoes.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"infinite\":").Append(canoes.IsInfinite ? "true" : "false");
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
