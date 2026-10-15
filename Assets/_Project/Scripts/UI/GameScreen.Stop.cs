#nullable enable
using System.Globalization;
using System.Text;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Parcial del fondo de la parada en el tablero (D-013, punto 9, Q5-A; 07 §2.1; propuesta del Mapa §10.5; tarea M1.4): los
    /// ganchos <c>Stop*</c> que declara GameScreen.cs pintan con <see cref="StopBackdrop"/>, delante del panorama y detrás de la
    /// garza, el fondo de la parada del nivel (<see cref="RiverStopCatalog.StopOf"/>; con <c>ui_bg_per_stop</c> = false, la gran
    /// parada de su mundo) con el agua base del mundo, y tiñen el panorama con su luz (<see cref="StopBackdrop.DisplayedLutColor"/>).
    /// <list type="bullet">
    /// <item>Sin catálogo de paradas, o del 201 en adelante mientras no haya paradas, nada cambia: el aspecto de hoy.</item>
    /// <item>Al cambiar de composición (del 20 al 21), fundido de <see cref="MapConfig.StopCrossfadeMs"/> ms en paralelo con la
    /// intro, sin añadir tiempo a la carga; corte directo en la primera carga y con Reducir movimiento.</item>
    /// <item>El panel, la intro y la tropa no cambian; nada del fondo recibe toques.</item>
    /// </list>
    /// <para>En desarrollo, al cargar cada nivel, <c>AF_STATE {"event":"stop",…}</c> con los campos de docs/dev/prototipo-v0.md:
    /// <c>level</c>, <c>stop</c> (k o null), <c>id</c>, <c>world</c>, <c>hero</c> (la ranura o la pieza del kit que se pinta, o
    /// null; <see cref="StopBackdrop.HeroId"/>), <c>lut</c>, <c>water</c>, <c>fade</c> (true si hubo fundido) y <c>build_ms</c> (lo
    /// que costó componerlo con sus mallas; ≤ 8 ms, 08 §5.4); y además <c>hero_src</c> (<c>png</c>, <c>shape</c> o <c>none</c>),
    /// <c>shape</c>, <c>code</c> y <c>change</c> (<c>cut</c>, <c>fade</c> o <c>same</c>).</para>
    /// <para>La banda de agua no se pinta bajo el relleno opaco del panel (<see cref="StopBackdrop.PanelHoleInsetDp"/>), y el
    /// hueco sigue al tablero cuando sube con la victoria o un modal.</para>
    /// <para><b>Victoria con vista</b> (D-013, punto 8, Q6-A; 02 §6; 07 §5.4; <c>ui_victory_view</c>, 08 §9; tarea M2.2): al abrirse
    /// la pantalla de victoria, tras la ola de saltos, el tablero entero (el panel, sus estantes y las cuias, con lo que va en
    /// su capa) pasa del alfa 1 al 0 en <see cref="VictoryViewFadeMs"/> ms y se ve el sitio del fondo, que deja de tener el
    /// hueco del panel; si es una llegada (<see cref="RiverStopCatalog.ArrivalOnWin"/> salvo en el 10, como
    /// <see cref="MapMoments.ForVictory"/>), el fondo funde a la vez, en el mismo fundido, a la parada que traerá el nivel
    /// siguiente, así que ese nivel ya carga con su fondo (<c>change</c> = <c>same</c>). En el 10 no hay llegada (la cuenta el
    /// Mapa, 02 §6 y 06 §2.3): el tablero se va sobre el estuario y el 11 cambia de fondo al cargar.
    /// Con Reducir movimiento, corte; con la clave apagada, como antes (el tablero a la vista y el fondo cambia al cargar).
    /// No mueve ningún tiempo de la victoria (Siguiente, Inicio, la Cosecha doble, AD_GATE y la ranura no cambian); la carga
    /// del nivel siguiente devuelve el alfa a 1.</para>
    /// </summary>
    public sealed partial class GameScreen
    {
        /// <summary>Cambio de fondo de la sonda: corte directo.</summary>
        internal const string StopChangeCut = "cut";

        /// <summary>Cambio de fondo de la sonda: fundido.</summary>
        internal const string StopChangeFade = "fade";

        /// <summary>Cambio de fondo de la sonda: la misma composición (sin cambio).</summary>
        internal const string StopChangeSame = "same";

        private StopBackdrop _stopBackdrop = null!;
        private bool _stopLoadedOnce;
        private string? _lastStopProbe;
        private string _lastStopChange = StopChangeSame;
        private float _stopHorizon;
        private float _stopPanelTop;
        private bool _stopHasBoard;
        private float _stopHoleShift;
        private readonly StringBuilder _stopProbeSb = new StringBuilder(256);

        /// <summary>Fundido de la victoria con vista: 250 ms (02 §6; 07 §5.4, fila 0,50-0,75; D-013, punto 8).</summary>
        internal const float VictoryViewFadeMs = 250f;

        // Victoria con vista: grupo del tablero (se crea la primera vez), ms del fundido en curso (−1 = ninguno) y si está
        // activa (de StopVictoryShown a la carga siguiente).
        private CanvasGroup? _boardGroup;
        private float _victoryViewMs = -1f;
        private bool _victoryViewOn;

        /// <summary>
        /// Solo tests: enciende o apaga la victoria con vista (null = <c>ui_victory_view</c>, <see cref="MapConfig.UiVictoryView"/>).
        /// </summary>
        internal static bool? VictoryViewOverride { get; set; }

        /// <summary>Alfa del tablero (el panel y las cuias): 1 salvo en la victoria con vista (tests).</summary>
        internal float BoardAlpha => _boardGroup == null ? 1f : _boardGroup.alpha;

        /// <summary>La victoria con vista está activa: de la pantalla de victoria a la carga siguiente (tests).</summary>
        internal bool VictoryViewOn => _victoryViewOn;

        /// <summary>El fundido del tablero de la victoria con vista está en curso (tests).</summary>
        internal bool VictoryViewFading => _victoryViewMs >= 0f;

        /// <summary>Fondo de la parada (tests y capturas).</summary>
        internal StopBackdrop StopBackdrop => _stopBackdrop;

        /// <summary>
        /// Rectángulo del héroe del fondo en dp del lienzo (x desde la izquierda, y desde arriba), o vacío sin héroe: lo que
        /// TiTino no debe tapar en la victoria (TI6a).
        /// </summary>
        internal Rect HeroRect => _stopBackdrop.HeroRect;

        /// <summary>Campos de la última línea <c>AF_STATE stop</c> (tests; null sin sonda o antes del primer nivel).</summary>
        internal string? LastStopProbe => _lastStopProbe;

        /// <summary>Cambio de fondo de la última carga: <c>cut</c>, <c>fade</c> o <c>same</c> (tests).</summary>
        internal string LastStopChange => _lastStopChange;

        /// <summary>Tinte del panorama: la luz de la parada que se ve (blanco = como hoy; tests).</summary>
        internal Color PanoramaTint => _panorama.color;

        /// <summary>Panorama de la escena (tests: el fondo de la parada va justo delante).</summary>
        internal RectTransform PanoramaRect => _panorama.rectTransform;

        /// <summary>Garza de la escena (tests: el fondo de la parada va justo detrás).</summary>
        internal RectTransform HeronRect => _heron.rectTransform;

        /// <summary>Panel del tablero donde se ve ahora, con el desplazamiento del tablero (dp desde arriba; tests).</summary>
        internal Rect StopPanelRect => PanelRect();

        /// <summary>
        /// Parada cuyo fondo lleva el nivel <paramref name="level"/>: la suya (<see cref="RiverStopCatalog.StopOf"/>) o, con
        /// <paramref name="perStop"/> = false (<c>ui_bg_per_stop</c>, 08 §9), la gran parada de su mundo
        /// (<see cref="RiverStopCatalog.FirstStopOfWorld"/>). Null sin catálogo o si el nivel no tiene parada (201 en adelante).
        /// </summary>
        internal static RiverStop? StopForLevel(RiverStopCatalog? stops, int level, bool perStop)
        {
            RiverStop? stop = stops?.StopOf(level);
            if (stop == null || perStop)
            {
                return stop;
            }

            return stops!.FirstStopOfWorld(stop.World) ?? stop;
        }

        partial void StopBuild(RectTransform scene)
        {
            _stopBackdrop = new StopBackdrop(scene, _art);
        }

        partial void StopLayoutBackdrop(float horizon, float panelTop, bool hasBoard)
        {
            _stopHorizon = horizon;
            _stopPanelTop = panelTop;
            _stopHasBoard = hasBoard;
            LayoutStopBackdrop();
        }

        // La banda deja sin pintar lo que tapa el relleno opaco del panel (guía §8), allí donde se ve ahora (con el desplazamiento
        // del tablero de la victoria o de un modal).
        private void LayoutStopBackdrop()
        {
            Rect hole = new Rect(0f, 0f, 0f, 0f);
            _stopHoleShift = _boardShift;
            if (_stopHasBoard && !_victoryViewOn) // con la victoria con vista, el panel se va: la banda entera
            {
                Rect panel = PanelRect();
                float inset = StopBackdrop.PanelHoleInsetDp;
                if (panel.width > 2f * inset && panel.height > 2f * inset)
                {
                    hole = new Rect(panel.x + inset, panel.y + inset, panel.width - (2f * inset), panel.height - (2f * inset));
                }
            }

            _stopBackdrop.Layout(_canvasW, _stopHorizon, _stopPanelTop, _stopHasBoard, StopBandBottom(), hole);
        }

        partial void StopLevelLoaded(LevelFlow flow)
        {
            EndVictoryView();
            RiverStop? stop = StopForLevel(RiverStops, flow.LevelNumber, MapConfig.UiBgPerStop);
            string water = flow.Level.WaterBase;
            RiverStop? before = _stopBackdrop.Stop;
            // Sin parada antes ni después no cambia nada visible, aunque cambie el agua del mundo (201 en adelante, sin catálogo).
            bool same = (before == null && stop == null)
                || (ReferenceEquals(before, stop) && string.Equals(_stopBackdrop.Water, water, System.StringComparison.Ordinal));
            bool fade = _stopLoadedOnce && !flow.ReduceMotion && !same;
            _stopLoadedOnce = true;
            _stopBackdrop.Apply(stop, water, fade, MapConfig.StopCrossfadeMs);
            _lastStopChange = same ? StopChangeSame : (_stopBackdrop.Fading ? StopChangeFade : StopChangeCut);
            ApplyStopLut();
            EmitStopProbe(flow);
        }

        partial void StopVictoryShown(LevelFlow flow)
        {
            if (!(VictoryViewOverride ?? MapConfig.UiVictoryView))
            {
                return; // como antes: el tablero a la vista y el fondo cambia al cargar el nivel siguiente
            }

            bool cut = flow.ReduceMotion;
            _victoryViewOn = true;
            LayoutStopBackdrop(); // sin hueco: bajo el panel que se va, la banda del agua

            // En una llegada, el fondo pasa ya, en el mismo fundido, a la parada que trae el nivel siguiente (con
            // ui_bg_per_stop = false, la gran parada de su mundo) con el agua de su mundo. El 10 no es una llegada (la misma
            // definición que MapMoments.ForVictory y VictoryLineArbiter): la primera llegada la cuenta el Mapa del 10 (02 §6,
            // fila Llegada; 06 §2.3, paso 3; D-013, punto 7), así que en el 10 el tablero se va sobre el estuario y el fondo de
            // Santarém llega con la carga del 11 (D-013, punto 9).
            int won = flow.LevelNumber;
            RiverStopCatalog? stops = RiverStops;
            if (stops != null && won != RiverStopCatalog.LevelsPerStop && stops.ArrivalOnWin(won) != null)
            {
                int next = won + 1;
                RiverStop? stop = StopForLevel(stops, next, MapConfig.UiBgPerStop);
                string water = flow.Catalog.Find(next)?.WaterBase ?? flow.Level.WaterBase;
                _stopBackdrop.Apply(stop, water, !cut, VictoryViewFadeMs);
                ApplyStopLut();
            }

            if (_boardGroup == null)
            {
                _boardGroup = _board.gameObject.AddComponent<CanvasGroup>();
            }

            _victoryViewMs = cut ? -1f : 0f;
            _boardGroup.alpha = cut ? 0f : 1f;
        }

        // La carga de un nivel devuelve el tablero a la vista y el hueco del panel a la banda.
        private void EndVictoryView()
        {
            _victoryViewMs = -1f;
            if (_boardGroup != null && _boardGroup.alpha < 1f)
            {
                _boardGroup.alpha = 1f;
            }

            if (_victoryViewOn)
            {
                _victoryViewOn = false;
                LayoutStopBackdrop();
            }
        }

        partial void StopTick(float dtMs)
        {
            if (_victoryViewMs >= 0f && _boardGroup != null)
            {
                // Lineal, como el fundido del fondo (StopBackdrop.Tick): el tablero se va mientras el sitio nuevo entra.
                _victoryViewMs += Mathf.Max(0f, dtMs);
                float t = Mathf.Clamp01(_victoryViewMs / VictoryViewFadeMs);
                _boardGroup.alpha = 1f - t;
                if (t >= 1f)
                {
                    _victoryViewMs = -1f;
                }
            }

            if (_stopHasBoard && !Mathf.Approximately(_boardShift, _stopHoleShift))
            {
                LayoutStopBackdrop(); // el tablero se movió: el hueco de la banda lo sigue (sin asignar memoria)
            }

            if (_stopBackdrop.Tick(dtMs))
            {
                ApplyStopLut();
            }
        }

        // La luz de la parada sobre el panorama y el cielo que lo completa (blanco = como hoy). La banda de agua no la lleva
        // (VA-23).
        private void ApplyStopLut()
        {
            Color lut = _stopBackdrop.DisplayedLutColor;
            if (_panorama.color != lut)
            {
                _panorama.color = lut;
            }

            Color sky = Palette.SkyTop * lut;
            sky.a = 1f;
            if (_skyFill.color != sky)
            {
                _skyFill.color = sky;
            }
        }

        // Pie de la banda de agua: la viga del muelle (LayoutBackdrop la pone DockBeamTopDp bajo el borde del PNG del muelle).
        private float StopBandBottom()
        {
            float dockTop = -_dock.rectTransform.anchoredPosition.y;
            return dockTop + (ProceduralArt.DockBeamTopDp * (_canvasW / ReferenceWidth)) + 8f;
        }

        private void EmitStopProbe(LevelFlow flow)
        {
            if (!DevProbe.Enabled)
            {
                _lastStopProbe = null;
                return;
            }

            StopBackdrop b = _stopBackdrop;
            RiverStop? stop = b.Stop;
            StringBuilder sb = _stopProbeSb;
            sb.Clear();
            sb.Append("\"level\":").Append(flow.LevelNumber.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"stop\":").Append(stop == null ? "null" : stop.N.ToString(CultureInfo.InvariantCulture));
            AppendJsonString(sb, "id", stop?.Id);
            sb.Append(",\"world\":").Append(stop == null ? "null" : stop.World.ToString(CultureInfo.InvariantCulture));
            AppendJsonString(sb, "hero", b.HeroId);
            AppendJsonString(sb, "lut", stop?.Backdrop.Lut);
            AppendJsonString(sb, "water", b.Water);
            sb.Append(",\"fade\":").Append(_lastStopChange == StopChangeFade ? "true" : "false");
            sb.Append(",\"build_ms\":").Append(b.BuildMs.ToString("0.###", CultureInfo.InvariantCulture));
            AppendJsonString(sb, "hero_src", b.HeroSource);
            AppendJsonString(sb, "shape", b.Shape);
            AppendJsonString(sb, "code", b.Code);
            AppendJsonString(sb, "change", _lastStopChange);
            _lastStopProbe = sb.ToString();
            _probe.Event("stop", _lastStopProbe);
        }

        // ,"key":"valor" con el valor escapado como cadena JSON (comillas, barras y controles), o ,"key":null. Sin asignar.
        internal static void AppendJsonString(StringBuilder sb, string key, string? value)
        {
            sb.Append(",\"").Append(key).Append("\":");
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '"' || c == '\\')
                {
                    sb.Append('\\').Append(c);
                }
                else if (c < ' ')
                {
                    sb.Append("\\u00").Append(HexDigit(c >> 4)).Append(HexDigit(c & 0xF));
                }
                else
                {
                    sb.Append(c);
                }
            }

            sb.Append('"');
        }

        private static char HexDigit(int v)
        {
            return (char)(v < 10 ? '0' + v : 'a' + (v - 10));
        }
    }
}
