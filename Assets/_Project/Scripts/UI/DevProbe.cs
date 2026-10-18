#nullable enable
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Presentation;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Sonda de desarrollo (docs/dev/prototipo-v0.md c): en builds de desarrollo (<see cref="Debug.isDebugBuild"/>)
    /// escribe en el log, en una línea JSON con InvariantCulture, <c>AF_LAYOUT</c> (rectángulos táctiles en píxeles con origen
    /// arriba a la izquierda; se toca en el centro; partida en varias líneas con <c>"part"</c>/<c>"parts"</c> si pasa de
    /// <see cref="MaxLayoutLineBytes"/>: ver <see cref="Layout"/>) y <c>AF_STATE</c> (eventos del nivel), para que un
    /// agente pueda jugar y verificar el prototipo con <c>adb logcat -s Unity</c> y <c>adb shell input tap</c>. Ninguna línea
    /// pasa de <see cref="MaxLineBytes"/> bytes (tarea FX10: logcat corta en ~1.024 caracteres): las que no caben salen en
    /// partes (DevProbe.Lines.cs).
    /// </summary>
    /// <remarks>
    /// <para>Eventos de <c>AF_STATE</c>: <c>load</c>, <c>ready</c>, <c>pour</c>, <c>reject</c>, <c>undo</c>, <c>restart</c>,
    /// <c>win</c> y <c>stuck</c> (contrato) más <c>select</c> y <c>deselect</c> (ayuda para seguir los toques). <c>load</c>
    /// lleva además los Patrones de fruta del nivel (T-E.7): <c>"patterns":{"on":true,"opacity":0.20}</c>, con <c>on</c> =
    /// el interruptor del nivel con el ajuste de Ajustes (<see cref="PatternsResolver.IsOn"/>; en automático,
    /// <see cref="Palette.PatternsOnFor"/>: desde el 41; el icono en cada dosis; tarea L3) y <c>opacity</c> =
    /// la opacidad del patrón que lleva el shader (<see cref="LiquidGraphic.ShaderPatternOpacity"/>: 0,20 con los Patrones
    /// activos y 0,15 sin ellos; con Siempre o Nunca no depende del nivel), con punto decimal. Del embudo de
    /// D-008 y los comodines, los de <see cref="Funnel"/> (y <c>resume</c> al reabrir el nivel guardado, B2-5). De la
    /// victoria y los anuncios simulados (B2-4): <c>victory</c>, <c>double_harvest</c>, <c>auto_next</c> (avance tras la
    /// Cosecha doble, D-011 punto 4) y <c>ad_gate</c> (<see cref="Victory"/>), <c>ad_check</c> (<see cref="AdCheck"/>),
    /// <c>ad</c> y <c>ad_closed</c> (<see cref="Ad"/>) y <c>purchase_sim</c> (<see cref="Purchase"/>). Fuera del nivel
    /// (tarea B1-6, <see cref="App"/>): <c>lobby</c>, <c>content_end</c> y <c>no_canoes</c>. Todas las del embudo, la victoria, los anuncios, las compras y el inicio llevan
    /// semillas y canoas (B2-6). <c>AF_LAYOUT</c> de esas pantallas con los botones <c>play</c>, <c>canoes</c>,
    /// <c>settings</c>, <c>back</c>, <c>double_harvest</c>, <c>next</c> y <c>home</c>.</para>
    /// <para>Cada línea lleva <c>"pour_mode"</c> (run por defecto, D-005; single en pruebas), el modo de la sesión del
    /// nivel, y los eventos con vertido (<c>pour</c>, <c>undo</c>) llevan, además de
    /// <c>"move":[origen,destino,cantidad]</c>, la cantidad suelta en <c>"amount"</c> (en <c>run</c>, la racha que cabe;
    /// en <c>single</c>, siempre 1). <c>pours</c> cuenta vertidos aplicados (en <c>single</c>, dosis movidas).</para>
    /// <para>Mecánicas de la 1.0 (D-007; arquitectura del paso 1, §9): tras <c>cuias</c>, cada línea de nivel lleva el mundo
    /// (<c>world</c>), el agua base (<c>water</c>), la etiqueta (<c>label</c>) y los campos del tablero de
    /// <see cref="AppendBoardFields"/> (<c>murky</c>, <c>mech</c>, <c>hidden</c>, <c>sealed</c> y, con Lluvia o Piraña,
    /// <c>counter</c>); los vertidos añaden <c>recv</c> y <c>ev</c> (<see cref="AppendPourEvents"/>). Los colores de las dosis
    /// ocultas salen en <c>cuias</c>: la sonda solo escribe en desarrollo (<see cref="Enabled"/>), donde hacen falta para
    /// jugar con adb. Los eventos sueltos de la presentación van con <see cref="Event"/>.</para>
    /// <para>Con <see cref="Event"/>, la tarjeta previa y el banner de mundo (T13 y A4; docs/dev/prototipo-v0.md):
    /// <c>level_card</c> (con <c>bite_demo</c> en el estreno de la Piraña); <c>world_banner</c> al abrirse, con <c>level</c>,
    /// <c>world</c>, <c>text</c> (la clave entera), <c>then</c> (<c>level_card</c> o <c>intro</c>) y <c>reduce_motion</c>;
    /// <c>world_banner_art</c> cuando su tarjeta empieza a entrar, con <c>level</c>, <c>world</c>, <c>async</c>,
    /// <c>wait_ms</c> (reloj real desde que pidió el arte), <c>mascots_ms</c>, <c>main_ms</c> y <c>fallback</c> (<c>none</c>,
    /// o <c>error</c>, <c>queue</c> o <c>timeout</c> si entró sin arte); y <c>world_banner_closed</c> al tocarlo, con
    /// <c>level</c>, <c>world</c> y <c>next</c> (del mismo vocabulario que <c>then</c>). Y <c>AF_LAYOUT</c> con
    /// <c>"screen":"level_card"</c> o <c>"screen":"world_banner"</c> (id <c>world_banner</c>, el rectángulo de la tarjeta) al
    /// terminar su entrada; en la línea <c>level</c>, el id <c>world_banner</c> solo con la tarjeta a la vista.</para>
    /// <para>Clase parcial (arquitectura de la tanda 4, §3): cada función añade su <c>DevProbe.&lt;Función&gt;.cs</c>; el idioma de
    /// la UI (<c>loc</c>), en DevProbe.Loc.cs.</para>
    /// </remarks>
    public sealed partial class DevProbe
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
            Write(sb.ToString());
        }

        /// <summary>
        /// Línea <c>AF_STATE</c>: <c>t</c> en ms desde el arranque, nivel, modo de vertido, estado de 02 §2, estado del
        /// modelo, vertidos, selección, toque o vertido del evento (con su cantidad), cuias de abajo arriba (con los colores de
        /// las ocultas: la sonda solo escribe en desarrollo) y, después, el mundo, el agua, la etiqueta y los campos del
        /// tablero de <see cref="AppendBoardFields"/>.
        /// </summary>
        public void State(string evt, LevelFlow flow, int tap, Pour? move)
        {
            string? line = FormatState(evt, flow, tap, move);
            if (line != null)
            {
                Write(line);
            }
        }

        /// <summary>
        /// Como <see cref="State(string, LevelFlow, int, Pour?)"/> para un vertido: con <c>move</c> y <c>amount</c> de
        /// <paramref name="result"/> y, al final, el receptor y las etapas de R10 (<see cref="AppendPourEvents"/>).
        /// </summary>
        public void State(string evt, LevelFlow flow, int tap, in PourResult result)
        {
            string? line = FormatState(evt, flow, tap, in result);
            if (line != null)
            {
                Write(line);
            }
        }

        /// <summary>Texto de la línea <c>AF_STATE</c> de <see cref="State(string, LevelFlow, int, Pour?)"/>; null si la sonda no escribe o no hay nivel.</summary>
        public string? FormatState(string evt, LevelFlow flow, int tap, Pour? move)
        {
            PourEvents none = default;
            return FormatState(evt, flow, tap, move, false, in none);
        }

        /// <summary>Texto de la línea <c>AF_STATE</c> de un vertido (la sobrecarga de <c>State</c> con <see cref="PourResult"/>); null si la sonda no escribe o no hay nivel.</summary>
        public string? FormatState(string evt, LevelFlow flow, int tap, in PourResult result)
        {
            PourEvents events = result.Events;
            return FormatState(evt, flow, tap, result.Pour, true, in events);
        }

        /// <summary>
        /// Línea <c>AF_STATE</c> de un evento suelto de la presentación (p. ej. <c>step_budget</c>, el presupuesto de los pasos
        /// de mecánica): <c>event</c>, <c>t</c> y los campos de <paramref name="fieldsJson"/> (miembros JSON
        /// <c>"clave":valor</c> separados por comas, con o sin llaves; vacío o null, ninguno). Solo en desarrollo.
        /// </summary>
        public void Event(string evt, string fieldsJson)
        {
            string? line = FormatEvent(evt, fieldsJson);
            if (line != null)
            {
                Write(line);
            }
        }

        /// <summary>Texto de la línea de <see cref="Event"/>; null si la sonda no escribe.</summary>
        public string? FormatEvent(string evt, string? fieldsJson)
        {
            if (!Enabled)
            {
                return null;
            }

            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"").Append(evt).Append('"');
            AppendTime(sb);
            string fields = fieldsJson == null ? string.Empty : fieldsJson.Trim();
            if (fields.Length >= 2 && fields[0] == '{' && fields[fields.Length - 1] == '}')
            {
                fields = fields.Substring(1, fields.Length - 2).Trim();
            }

            if (fields.Length > 0)
            {
                sb.Append(',').Append(fields);
            }

            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Campos del tablero de la línea <c>AF_STATE</c> (cada uno empieza con coma):
        /// <list type="bullet">
        /// <item><description><c>murky</c>: el nivel tiene Aguas turbias (<paramref name="initial"/> con dosis ocultas; 02
        /// §11);</description></item>
        /// <item><description><c>mech</c>: una cadena por cuia de <paramref name="current"/>, <c>"-"</c> sin mecánica o su tipo
        /// (<see cref="MechanicTypes.ToKey"/>) con <c>;key=K</c>, <c>;pair=P</c> (siempre en el Remolino), <c>;n=N</c>,
        /// <c>;k=K</c> y <c>;c=C</c> (el contador) si valen más de 0 y <c>;closed</c> con la Hoja puesta o el Brote cerrado
        /// (p. ej. <c>hoja;key=4;closed</c>);</description></item>
        /// <item><description><c>hidden</c>: la máscara de ocultas de cada cuia (bit k = dosis k desde el fondo);</description></item>
        /// <item><description><c>sealed</c>: los índices de las cuias selladas;</description></item>
        /// <item><description><c>counter</c>, solo si hay contador (Lluvia o Piraña; <see cref="LevelCounter"/>):
        /// <c>type</c>, <c>cuia</c>, <c>remaining</c>, <c>pending</c> y <c>blocked</c>.</description></item>
        /// </list>
        /// Puro: no depende de <see cref="LevelFlow"/> ni de <see cref="Enabled"/>.
        /// </summary>
        internal static void AppendBoardFields(StringBuilder sb, BoardState initial, BoardState current)
        {
            sb.Append(",\"murky\":").Append(initial.HasHidden ? "true" : "false");
            sb.Append(",\"mech\":[");
            for (int i = 0; i < current.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append('"');
                AppendMechanic(sb, current[i]);
                sb.Append('"');
            }

            sb.Append("],\"hidden\":[");
            for (int i = 0; i < current.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(current[i].HiddenMask.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append("],\"sealed\":[");
            bool first = true;
            for (int i = 0; i < current.Count; i++)
            {
                if (!current.IsSealed(i))
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append(']');
            LevelCounter counter = LevelCounter.Of(current);
            if (counter.IsActive)
            {
                sb.Append(",\"counter\":{\"type\":\"").Append(MechanicTypes.ToKey(counter.Type)).Append('"');
                sb.Append(",\"cuia\":").Append(counter.CuiaIndex.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"remaining\":").Append(counter.Remaining.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"pending\":").Append(counter.PendingDrops.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"blocked\":").Append(counter.Blocked ? "true" : "false").Append('}');
            }
        }

        /// <summary>
        /// Campos de un vertido (cada uno empieza con coma): <c>recv</c>, la cuia que recibió (la pareja en el Remolino), y
        /// <c>ev</c> con las máscaras de las etapas de R10 (bit i = cuia i): <c>rev</c> (Aguas turbias reveladas), <c>mud</c>
        /// (Barro lavado), <c>open</c> (Hoja o Brote abiertos), <c>seal</c>, <c>rel</c> (soltadas), <c>drop</c>, <c>wait</c> y
        /// <c>end</c> (Lluvia), <c>bite</c>, <c>cave</c> y <c>lost</c> (Piraña) y <c>futile</c>. Puro.
        /// </summary>
        internal static void AppendPourEvents(StringBuilder sb, in PourEvents ev)
        {
            sb.Append(",\"recv\":").Append(ev.Receiver.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"ev\":{");
            AppendMask(sb, "rev", ev.RevealedMask, true);
            AppendMask(sb, "mud", ev.BarroWashedMask, false);
            AppendMask(sb, "open", ev.OpenedMask, false);
            AppendMask(sb, "seal", ev.SealedMask, false);
            AppendMask(sb, "rel", ev.ReleasedMask, false);
            AppendMask(sb, "drop", ev.LluviaDropMask, false);
            AppendMask(sb, "wait", ev.LluviaWaitingMask, false);
            AppendMask(sb, "end", ev.LluviaEndedMask, false);
            AppendMask(sb, "bite", ev.PiranaBittenMask, false);
            AppendMask(sb, "cave", ev.PiranaCaveMask, false);
            AppendMask(sb, "lost", ev.PiranaBiteLostMask, false);
            sb.Append(",\"futile\":").Append(ev.Futile ? "true" : "false").Append('}');
        }

        private static void AppendMask(StringBuilder sb, string name, int mask, bool first)
        {
            if (!first)
            {
                sb.Append(',');
            }

            sb.Append('"').Append(name).Append("\":").Append(mask.ToString(CultureInfo.InvariantCulture));
        }

        // "-" sin mecánica; si no, el tipo y sus atributos no nulos (pair siempre en el Remolino) y ";closed".
        private static void AppendMechanic(StringBuilder sb, Cuia cuia)
        {
            MechanicType type = cuia.Mechanic;
            if (type == MechanicType.None || !MechanicTypes.IsDefined(type))
            {
                sb.Append('-');
                return;
            }

            sb.Append(MechanicTypes.ToKey(type));
            AppendAttr(sb, "key", cuia.MechanicKey, false);
            AppendAttr(sb, "pair", cuia.MechanicPairId, type == MechanicType.Remolino);
            AppendAttr(sb, "n", cuia.MechanicN, false);
            AppendAttr(sb, "k", cuia.MechanicK, false);
            AppendAttr(sb, "c", cuia.MechanicCounter, false);
            if ((cuia.MechanicFlags & Cuia.FlagClosed) != 0)
            {
                sb.Append(";closed");
            }
        }

        /// <summary>
        /// Ajustes del jugador (<c>settings</c> del perfil; tarea L3) para el campo <c>patterns</c> de <c>load</c>; null = los de
        /// por defecto (Patrones de fruta en automático).
        /// </summary>
        public SettingsData? Settings { get; set; }

        /// <summary>
        /// Campo <c>"patterns":{"on":bool,"opacity":x}</c> del nivel <paramref name="level"/> (T-E.7): el interruptor Patrones de
        /// fruta con el ajuste <paramref name="setting"/> (<see cref="PatternsResolver.IsOn"/>: en automático,
        /// <see cref="Palette.PatternsOnFor"/>; tarea L3) y la opacidad del patrón que lleva el material del líquido
        /// (<see cref="LiquidGraphic.ShaderPatternOpacity"/>), con dos decimales y punto, como InvariantCulture, sin asignar.
        /// </summary>
        internal static void AppendPatterns(StringBuilder sb, int level, PatternsSetting setting)
        {
            sb.Append(",\"patterns\":{\"on\":").Append(PatternsResolver.IsOn(setting, level) ? "true" : "false");
            sb.Append(",\"opacity\":");
            AppendUnit2(sb, LiquidGraphic.ShaderPatternOpacity);
            sb.Append('}');
        }

        // Un valor de 0 a 1 con dos decimales y punto («0.20»), como lo escribiría InvariantCulture, sin asignar.
        private static void AppendUnit2(StringBuilder sb, float value)
        {
            int h = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
            sb.Append((char)('0' + (h / 100))).Append('.').Append((char)('0' + ((h / 10) % 10))).Append((char)('0' + (h % 10)));
        }

        private static void AppendAttr(StringBuilder sb, string name, int value, bool always)
        {
            if (always || value > 0)
            {
                sb.Append(';').Append(name).Append('=').Append(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        private string? FormatState(string evt, LevelFlow flow, int tap, Pour? move, bool hasEvents, in PourEvents events)
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

            sb.Append(']');
            AF.Levels.LevelDefinition level = flow.Level;
            sb.Append(",\"world\":").Append(level.World.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"water\":\"").Append(level.WaterBase).Append('"');
            sb.Append(",\"label\":\"").Append(level.DifficultyLabel).Append('"');
            AppendBoardFields(sb, flow.Session.Initial, board);
            if (hasEvents)
            {
                AppendPourEvents(sb, in events);
            }

            if (string.Equals(evt, "load", System.StringComparison.Ordinal))
            {
                AppendPatterns(sb, flow.LevelNumber, Settings != null ? Settings.Patterns : PatternsSetting.Auto);
            }

            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Línea <c>AF_PERF</c> con lo que costó escribir el perfil tras una acción del jugador (revisión de B2-5; T-D, con el
        /// objetivo del dueño del 2026-10-09: <c>save_ms</c> p95 &lt; 5 ms y ninguno &gt; 8 ms, en todos los vertidos): <c>event</c>
        /// (<c>pour</c>), nivel, escrituras de esa acción (1 con el lote), <c>save_ms</c> = lo que pagó el hilo principal
        /// (serializar y entregar al escritor de segundo plano, o la escritura síncrona), <c>bg_ms</c> = lo que tardó la última
        /// escritura terminada del escritor (<c>.tmp</c>, fsync y Replace; casi siempre la del vertido anterior, porque la de
        /// este sigue en marcha) y <c>sync</c> = la escritura de esta acción fue síncrona (el vertido que gana es fin de nivel:
        /// espera al escritor y escribe en el toque). <c>sync</c> solo distingue la línea: sigue contando para el objetivo
        /// hasta que el dueño decida otra cosa. Sirve para medirlo en el teléfono con <c>adb logcat -s Unity</c>.
        /// </summary>
        public void SaveCost(string evt, int level, int writes, double ms, double bgMs, bool sync)
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
            sb.Append(",\"save_ms\":").Append(ms.ToString("0.00", CultureInfo.InvariantCulture));
            sb.Append(",\"bg_ms\":").Append(bgMs.ToString("0.00", CultureInfo.InvariantCulture));
            sb.Append(",\"sync\":").Append(sync ? "true" : "false").Append('}');
            Write(sb.ToString());
        }

        /// <summary>
        /// Línea <c>AF_STATE</c> del embudo de bloqueo y de los comodines (D-008; tareas B2-1 a B2-3): <c>stuck_modal</c>,
        /// <c>giveup_confirm</c>, <c>level_failed</c>, <c>no_canoes</c>, <c>rescue</c>, <c>rescue_check</c>, <c>extra_cuia</c>,
        /// <c>extra_cuia_gift</c>, <c>fail_hard</c>, <c>fail_hard_canoe</c>, <c>fail_soft</c> y <c>river_lowered</c>, siempre con
        /// semillas y canoas, <c>fails_level</c>, cuias añadidas, el resultado de «la compra cumple», el precio congelado y el
        /// cupo y el stock de Marea. Con <paramref name="ledger"/> (en <c>stuck_modal</c>), también si el modal ofrece la Balsa
        /// de rescate (<c>balsa_ok</c>: <paramref name="balsaOk"/>, <c>balsa_ok</c> de 04 §6.2, congelado al abrir), sus
        /// impresiones de hoy ya contada la de este modal (<c>balsa_shown_today</c>, contra <c>af_rescue_daily_cap</c>) y si está
        /// oculta por rechazos (<c>balsa_hidden</c>): por qué falta la Balsa en el modal (prueba de la ola 3 en el teléfono; tarea
        /// FX5).
        /// </summary>
        public void Funnel(string evt, LevelFlow flow, AF.IAP.StoreLedger? ledger = null, bool balsaOk = false)
        {
            string? line = FormatFunnel(evt, flow, ledger, balsaOk);
            if (line != null)
            {
                Write(line);
            }
        }

        /// <summary>Texto de la línea de <see cref="Funnel"/>; null si la sonda no escribe o no hay nivel.</summary>
        public string? FormatFunnel(string evt, LevelFlow flow, AF.IAP.StoreLedger? ledger = null, bool balsaOk = false)
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
            if (ledger != null)
            {
                sb.Append(",\"balsa_ok\":").Append(balsaOk ? "true" : "false");
                sb.Append(",\"balsa_shown_today\":").Append(ledger.BalsaShownToday.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"balsa_hidden\":").Append(ledger.BalsaHiddenUntil.HasValue ? "true" : "false");
            }

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
                Write(line);
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
        /// <c>double_harvest</c> (Cosecha doble cobrada), <c>auto_next</c> (la espera tras la Cosecha doble venció y la victoria
        /// pasa sola al nivel siguiente, D-011 punto 4; le sigue <c>ad_gate</c>) y <c>ad_gate</c> (Siguiente, tocado o por el
        /// avance tras la Cosecha doble), con la Cosecha, si se dobló, si se ofrece la Cosecha doble y el saldo (semillas y
        /// canoas).
        /// </summary>
        public void Victory(string evt, LevelFlow flow, bool doubleOffered)
        {
            string? line = FormatVictory(evt, flow, doubleOffered);
            if (line != null)
            {
                Write(line);
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
            Write(sb.ToString());
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
                Write(line);
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
                Write(line);
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
        /// Línea <c>AF_LAYOUT</c> de la pantalla <paramref name="screen"/>: tamaño de pantalla, áreas táctiles de las cuias y
        /// botones con su id. Para tocar, se usa la última línea de la pantalla que está a la vista (para el tablero, la última
        /// <c>level</c>): las pantallas se repiten, y cerrar un modal no vuelve a publicar lo que queda debajo (al cerrar la
        /// Pausa, al cancelar ¿Salir? o ¿Reiniciar?, al Reintentar con canoas sin «El río bajó» o al cerrar Sin canoas para
        /// seguir jugando, el tablero vuelve a verse sin una línea <c>level</c> nueva).
        /// <list type="bullet">
        /// <item><description><c>level</c> (undo, extra_cuia desde el 8, restart, pause, level_card y reveal si se ven): al final
        /// de <c>GameScreen.OnLevelLoaded</c>, después de los ganchos <c>*LevelLoaded</c> y con la bandeja ya refrescada; otra vez
        /// cuando aparece o se va una ranura de la bandeja (Cuia extra o Revelar, que la recentran), al cambiar la pantalla
        /// y al acabar el re-layout de una cuia añadida (Cuia extra, rescate o Balsa: una sola línea, con las N + 1 cuias en su
        /// sitio final; si en esos 600 ms se abrió un modal del nivel, detrás se repite la línea del modal).</description></item>
        /// <item><description>Modales del nivel: <c>pause</c>, <c>quit_confirm</c> y <c>restart_confirm</c> al abrirse;
        /// <c>level_card</c> al terminar su entrada; <c>stuck</c> y <c>victory</c> al terminar de subir el panel.</description></item>
        /// <item><description>Embudo del nivel (D-008), al abrirse: <c>giveup_confirm</c> (close, rescue si se ofrece y give_up)
        /// y <c>level_failed</c> (retry y close), también al reabrir la app en ellos, y <c>no_canoes</c> (close,
        /// extra_canoe_video, refill_canoes e infinite_canoes si se ofrecen, y home), que se repite cada vez que rehace su layout
        /// (no se guarda: la app se reabre en Nivel fallido o en el nivel).</description></item>
        /// <item><description>Inicio: <c>lobby</c>, <c>content_end</c> y <c>no_canoes</c>.</description></item>
        /// </list>
        /// Ninguna línea pasa de <see cref="MaxLayoutLineBytes"/> (tareas FX7 y FX10: en el teléfono logcat corta cada mensaje en
        /// unos 1.024 caracteres, y con <c>af_max_level 560</c> la del Mapa llegaba cortada): si no cabe, la sonda sale en varias líneas seguidas, todas de la
        /// misma pantalla y con su <c>level</c>, <c>sw</c> y <c>sh</c>, con <c>"part":k,"parts":n</c> detrás de <c>sh</c>; las cuias van
        /// enteras en la parte 1 (en las demás, <c>"cuias":[]</c>) y los botones, en orden, repartidos entre las partes (la parte 1
        /// puede ir sin botones; las excepciones al tope, en <see cref="LayoutLines"/>). Quien toca
        /// junta los botones de las <c>n</c> últimas líneas de la pantalla (de la parte 1 a la <c>n</c>). Una línea que cabe sale como
        /// siempre, sin <c>part</c> ni <c>parts</c>.
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
            string head = sb.ToString();
            sb.Clear();
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

            string cuiaList = sb.ToString();
            var entries = new string[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                sb.Clear();
                sb.Append("{\"id\":\"").Append(buttonIds[i]).Append('"');
                AppendRect(sb, buttons[i]);
                sb.Append('}');
                entries[i] = sb.ToString();
            }

            string[] lines = LayoutLines(head, cuiaList, entries, MaxLayoutLineBytes);
            for (int i = 0; i < lines.Length; i++)
            {
                Debug.Log(lines[i]);
            }
        }

        /// <summary>
        /// Tope de bytes (UTF-8) de una línea <c>AF_LAYOUT</c>, contada entera (tareas FX7 y FX10): el de todas las líneas de la
        /// sonda, <see cref="MaxLineBytes"/>, por debajo de los ~1.024 caracteres en que logcat corta un mensaje en el teléfono
        /// (FX7 lo puso en 3.900 por los 4.068 bytes de una entrada de logcat, pero el mensaje se corta antes).
        /// </summary>
        public const int MaxLayoutLineBytes = MaxLineBytes;

        /// <summary>
        /// Las líneas <c>AF_LAYOUT</c> de una pantalla (tarea FX7): <paramref name="head"/> (<c>AF_LAYOUT {"screen":…,"sh":N</c>),
        /// las cuias ya escritas (<paramref name="cuiaList"/>, sin corchetes) y cada botón (<paramref name="buttons"/>, un objeto
        /// JSON cada uno), en una sola línea si cabe en <paramref name="maxBytes"/> bytes de UTF-8; si no, en las partes que
        /// hagan falta, cada una con <c>"part":k,"parts":n</c>, las cuias solo en la primera y los botones en orden, tantos como
        /// quepan en cada una. Si con las cuias no cabe ni el primer botón, la parte 1 va solo con las cuias y ese botón abre la
        /// parte 2. Solo dos casos pasan del tope, y no se parten: un botón suelto que por sí mismo (con la cabecera de su parte) ya
        /// no cabe, que va solo en su parte, y unas cuias que ya no caben solas en la parte 1 (con los datos reales, unos 35 bytes
        /// por cuia y unos 55 por botón, ninguno de los dos llega a pasar).
        /// </summary>
        public static string[] LayoutLines(string head, string cuiaList, string[] buttons, int maxBytes)
        {
            Encoding utf8 = Encoding.UTF8;
            string whole = head + ",\"cuias\":[" + cuiaList + "],\"buttons\":[" + string.Join(",", buttons) + "]}";
            if (utf8.GetByteCount(whole) <= maxBytes)
            {
                return new[] { whole };
            }

            // Lo que ocupa una parte sin sus botones: la cabecera, "part" y "parts" (con hasta 4 cifras cada uno), las cuias en la
            // primera y los corchetes; cada botón suma su objeto y su coma.
            int headBytes = utf8.GetByteCount(head) + utf8.GetByteCount(",\"part\":0000,\"parts\":0000") + utf8.GetByteCount(",\"cuias\":[],\"buttons\":[]}");
            int cuiaBytes = utf8.GetByteCount(cuiaList);
            var groups = new System.Collections.Generic.List<int>(); // primer botón de cada parte
            int used = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                int b = utf8.GetByteCount(buttons[i]);
                if (groups.Count > 0)
                {
                    int budget = maxBytes - headBytes - (groups.Count == 1 ? cuiaBytes : 0);
                    if (used + 1 + b <= budget)
                    {
                        used += 1 + b; // con su coma
                        continue;
                    }
                }
                else if (cuiaBytes > 0 && b > maxBytes - headBytes - cuiaBytes)
                {
                    // La parte 1 lleva las cuias: si con ellas el primer botón ya no cabe, la parte 1 va solo con las cuias y el
                    // botón abre la parte 2 (revisión de FX7). Sin cuias, un botón que no cabe solo va solo en la parte 1.
                    groups.Add(i);
                }

                groups.Add(i);
                used = b;
            }

            if (groups.Count == 0)
            {
                groups.Add(0); // sin botones: una sola parte con las cuias
            }

            int parts = groups.Count;
            var lines = new string[parts];
            var sb = new StringBuilder(maxBytes);
            for (int p = 0; p < parts; p++)
            {
                int from = groups[p];
                int to = p + 1 < parts ? groups[p + 1] : buttons.Length;
                sb.Clear();
                sb.Append(head);
                sb.Append(",\"part\":").Append((p + 1).ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"parts\":").Append(parts.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"cuias\":[").Append(p == 0 ? cuiaList : string.Empty).Append("],\"buttons\":[");
                for (int i = from; i < to; i++)
                {
                    if (i > from)
                    {
                        sb.Append(',');
                    }

                    sb.Append(buttons[i]);
                }

                sb.Append("]}");
                lines[p] = sb.ToString();
            }

            return lines;
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
