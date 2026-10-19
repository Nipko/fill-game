#nullable enable
using System;
using AF.GameFlow;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// TiTino en la ventanita de casa del inicio (tarea TI6c; propuesta de TiTino d.7 y paso 6c de (i); D-013, Q17-A: en el
    /// inicio, solo en la ventanita; D-016, punto 5; 07 §5.4 y §6.5, poses <c>sloth_*</c>). Vive dentro del marco de
    /// <see cref="HomeWindow"/> (el de la pantalla de inicio; la del Mapa del río no lo lleva):
    /// <list type="bullet">
    /// <item><b>Pose por la hora local</b> (<see cref="BasePoseFor"/>, del <see cref="IClock"/> del juego): de día,
    /// <c>sloth_idle</c> o, en las horas de sol, <c>sloth_sunbathe</c>; de noche, <c>sloth_sleep</c>. Se mira cada
    /// <see cref="PoseCheckMs"/> ms: si la hora cambia con el inicio abierto, cambia la pose.</item>
    /// <item><b>Saludos</b> (<see cref="SlothLines.ForHome"/>): al volver tras ≥ 3 días, el de regreso; en la primera apertura del
    /// día entre las 06:00 y las 12:00, el de la mañana; como mucho uno al día entre los dos (<c>sloth.last_greeting_date</c>),
    /// con <c>sloth_wave</c> y, si hay voz, su frase en el bocadillo. Se decide una vez por visita al inicio, cuando la ventanita
    /// se ve con el inicio delante (<see cref="AppScreen.Lobby"/>; con «El río sigue creciendo» abierto, espera a que se cierre
    /// para no gastar el saludo del día detrás del modal); en cada visita se apunta <c>sloth.last_seen_date</c> y se guarda el
    /// perfil si algo cambió. Justo después del bocadillo del Mapa del 10 (D-028, pregunta 60 b), el saludo que tocaba no sale
    /// ni se gasta y esa visita no apunta nada: queda para la visita siguiente al inicio. Durante <c>sloth_wave</c>, el rig baja hasta <see cref="WaveDropDp"/> dp para que el marco no
    /// corte la garra que saluda. Colgado, se agarra a la rama pintada de la embaúba de la ventanita y la rama del rig no se
    /// pinta (si no, se verían dos ramas y una bajaría con él); tumbado o en ovillo, sobre la rama del rig, más abajo.</item>
    /// <item><b>Toque</b> (<see cref="Tap"/>): <c>sloth_look</c> y, como mucho una vez por sesión, un «Dato del perezoso»
    /// (<see cref="UiText.SlothFact"/>: en release, solo los aprobados por el asesor). Tocarlo no lo cuida ni da premios (d.7).
    /// El dato sale en su <b>tarjeta</b>, nunca en el bocadillo (D-013, punto 6: «su bocadillo (≤ 50 caracteres) queda para sus
    /// frases propias»; propuesta de TiTino g.3: la tarjeta que sale al tocarlo en el inicio, con datos de ≤ 90 caracteres):
    /// una placa de la familia de la del Dato del río (<see cref="ArrivalStrip"/>: esquinas de 10 dp, borde de 1 dp #E3D3B0 y
    /// fondo claro) con el retrato de TiTino a la izquierda y el dato a 14 sp.</item>
    /// <item><b>Nunca</b> con Sin canoas, la hoja de compra ni una oferta (lo oculta <see cref="HomeWindow.Apply"/>): el rig, el
    /// área táctil, el bocadillo y la tarjeta se van con la ventanita. Los dos salen debajo del marco (el bocadillo con el pico
    /// hacia TiTino y una sombra de 1 dp que lo separa del decorado): por debajo de los saldos, de Ajustes y de la píldora de
    /// la distancia, y por encima de «Jugar» y de su chip.</item>
    /// </list>
    /// <para>Con <c>settings.sloth_messages</c> apagado, solo gestos (sin frase ni dato); con <c>ui_sloth_enabled</c> apagado, nada:
    /// la ventanita queda con su retrato quieto. Sin el arte del rig (<see cref="ProceduralArt.SlothRigArt()"/>; release sin el PNG
    /// final), el retrato de M1.5 hace de TiTino: cierra los ojos de noche y no hace gestos, pero el saludo y el dato salen igual.</para>
    /// <para>Lo que necesita del juego (el reloj, la sesión de TiTino, el <c>sloth</c> del perfil y su guardado, los ajustes, el
    /// nivel de «Jugar» y Reducir movimiento) lo toma del <see cref="GameBootstrap"/> del que cuelga el inicio la primera vez que
    /// se ve (<see cref="Bind"/> lo fija a mano). El reloj es el del juego (<see cref="LaunchOptions.ClockOverride"/> en los tests).
    /// Avanza solo con el inicio a la vista (un componente en el lienzo del inicio); nada asigna memoria por fotograma, tampoco
    /// mientras el arranque del juego aún no ha terminado (solo al enlazarse, una vez).</para>
    /// </summary>
    internal sealed class HomeSloth
    {
        /// <summary>Tamaño S de TiTino en la ventanita (dp) [P]: cabe colgado o tumbado en el hueco de 46 dp del marco de 56.</summary>
        public const float RigSizeDp = 30f;

        /// <summary>Hora local desde la que es de noche y TiTino duerme (20:00) [P].</summary>
        public const int NightFromHour = 20;

        /// <summary>Hora local en la que deja de ser de noche (06:00, la del saludo de la mañana) [P].</summary>
        public const int NightToHour = SlothLines.MorningFromHour;

        /// <summary>Primera hora local de sol, en la que toma el sol en la copa (10:00) [P].</summary>
        public const int SunFromHour = 10;

        /// <summary>Hora local en la que deja de tomar el sol, sin incluirla (16:00) [P].</summary>
        public const int SunToHour = 16;

        /// <summary>Cada cuánto se mira la hora para la pose (ms).</summary>
        public const float PoseCheckMs = 1000f;

        /// <summary>
        /// Línea de la rama de TiTino colgado, desde arriba del marco (dp) [P]: sobre la rama pintada de la embaúba de la ventanita,
        /// de la que se agarra (la rama del rig no se pinta mientras cuelga).
        /// </summary>
        public const float HangBranchDp = 12f;

        /// <summary>Línea de la rama de TiTino tumbado o en ovillo, desde arriba del marco (dp) [P]: una rama más baja.</summary>
        public const float LieBranchDp = 40f;

        /// <summary>
        /// Lo que baja el rig, como mucho, durante <c>sloth_wave</c> (dp) [P]: la rama efectiva a 16 dp, para que la garra levantada
        /// quede ≥ 3 dp por debajo del canto de arriba del hueco (con 12 dp, el marco la cortaba). Entra y sale con el gesto
        /// (<see cref="WaveDrop"/>); con Reducir movimiento (pose quieta, sin la garra levantada), nada.
        /// </summary>
        public const float WaveDropDp = 4f;

        /// <summary>
        /// Hueco entre el canto de abajo del marco y el pico del bocadillo (dp) [P]: corto, para que el pico salga del marco y no
        /// de lo que el decorado ponga debajo (la garza de la orilla, a 360 × 640 dp).
        /// </summary>
        public const float BubbleGapDp = 6f;

        /// <summary>
        /// Centro del pico, desde el canto izquierdo del marco (dp) [P]: bajo la garra de delante de TiTino (la que saluda), a la
        /// derecha del centro, lejos de la garza del decorado, que a 360 × 640 dp queda bajo la mitad izquierda del marco.
        /// </summary>
        public const float TailXDp = 40f;

        /// <summary>Sombra del pico del bocadillo (dp) [P]: 1 dp de #000 al 15 %, que lo separa de lo blanco del decorado.</summary>
        public const float TailShadowDp = 1f;

        /// <summary>
        /// Hueco entre el canto de abajo del marco y la tarjeta del Dato del perezoso (dp) [P]: corto, para que la tarjeta tape lo
        /// que el decorado pone justo debajo del marco (la garza de la orilla, a 360 × 640 dp) y no asome suelto entre los dos.
        /// </summary>
        public const float FactCardGapDp = 2f;

        /// <summary>Texto de la tarjeta del dato (sp): el de la placa del Dato del río (<see cref="VictoryLineSlot.TextSp"/>).</summary>
        public const float FactCardTextSp = VictoryLineSlot.TextSp;

        /// <summary>Ancho máximo del bocadillo (dp) [P]; como mucho, hasta el margen derecho que da <see cref="HomeWindow.Place"/>.</summary>
        public const float BubbleMaxWidthDp = 260f;

        /// <summary>Texto del bocadillo (sp; 02 §6: 14 sp, el de la victoria), con la escala de texto de 02 §12.</summary>
        public const float BubbleTextSp = 14f;

        /// <summary>Aire del texto a cada lado dentro del bocadillo (dp) [P].</summary>
        public const float BubblePadXDp = 12f;

        /// <summary>Aire del texto arriba y abajo dentro del bocadillo (dp) [P].</summary>
        public const float BubblePadYDp = 7f;

        /// <summary>Grosor del borde de madera clara del bocadillo (dp) [P].</summary>
        public const float BubbleRimDp = 2f;

        /// <summary>Fundido de entrada y de salida del bocadillo (sin él con Reducir movimiento) [P].</summary>
        public const float BubbleFadeMs = 150f;

        /// <summary>Paso más largo de un fotograma (ms): tras una pausa, el bocadillo no desaparece de golpe.</summary>
        public const float MaxStepMs = 100f;

        // Pico del bocadillo hacia TiTino: un cuadrado girado 45° (dp), medio fuera del bocadillo.
        private const float TailDp = 12f;
        private const float TailOutDp = (TailDp + (2f * BubbleRimDp)) * 0.7072f;

        // Holgura del ancho ajustado al texto (dp): la de redondeo de TextMeshPro, para que no salte de línea.
        private const float FitSlackDp = 2f;

        // Vidrio esmerilado casi opaco y madera clara (D-003), como el bocadillo de la victoria (SlothVictory).
        private static readonly Color GlassColor = new Color(0xEA / 255f, 0xF7 / 255f, 0xF3 / 255f, 0.96f);
        private static readonly Color RimColor = Palette.FrameBevel;
        private static readonly Color TailShadowColor = new Color(0f, 0f, 0f, 0.15f);

        private readonly HomeWindow _home;
        private readonly ProceduralArt _art;
        private readonly RectTransform _hole;
        private readonly Button _tap;
        private readonly RectTransform _bubble;
        private readonly CanvasGroup _bubbleGroup;
        private readonly Image _tailShadow;
        private readonly Image _tailRim;
        private readonly Image _tailPlate;
        private readonly TextMeshProUGUI _label;
        private readonly RectTransform _card;
        private readonly CanvasGroup _cardGroup;
        private readonly Image _cardPortrait;
        private readonly TextMeshProUGUI _cardLabel;
        private readonly Driver _driver;

        private SlothRig? _rig;
        private Image? _rigBranch;
        private bool _rigBranchArt;
        private bool _rigMissing;
        private GameBootstrap? _boot;
        private bool _bootLooked;
        private bool _shown;
        private bool _visitPending;
        private bool _bound;
        private IClock? _clock;
        private SlothSession? _session;
        private SlothData? _data;
        private Func<bool>? _messages;
        private Func<int>? _nextLevel;
        private Func<bool>? _reduceMotion;
        private Func<bool>? _inLobby;
        private Action? _save;

        private SlothPoseId _basePose = SlothPoseId.Idle;
        private SlothPoseId _gesture;
        private float _gestureMs = -1f;
        private float _gestureEndMs;
        private float _poseCheckMs;

        private NoteKind _noteKind;
        private float _noteMs = -1f;
        private float _noteOutMs;
        private bool _noteReduceMotion;
        private float _x;
        private float _y;
        private float _maxRight = LobbyScreen.ReferenceWidth;

        /// <summary>
        /// Crea a TiTino (sin rig hasta que la ventanita se vea) dentro del marco de <paramref name="home"/>, con su área táctil y su
        /// bocadillo (ocultos) bajo <paramref name="parent"/>, el lienzo del inicio.
        /// </summary>
        public HomeSloth(HomeWindow home, Transform parent, ProceduralArt art)
        {
            _home = home;
            _art = art;

            // El hueco del marco, que recorta al rig: lo que se mueve nunca sale del marco.
            _hole = UiBuilder.Rect("SlothHole", home.Root);
            _hole.gameObject.AddComponent<RectMask2D>();
            float border = ProceduralArt.HomeWindowBorderDp;
            UiBuilder.Place(_hole, border, border, HomeWindow.SizeDp - (2f * border), HomeWindow.SizeDp - (2f * border));

            _tap = UiBuilder.BareButton("HomeSlothTap", parent, () => Tap());
            _tap.gameObject.SetActive(false);

            _bubble = UiBuilder.Rect("HomeSlothBubble", parent);
            _bubbleGroup = _bubble.gameObject.AddComponent<CanvasGroup>();
            _bubbleGroup.blocksRaycasts = false;
            _bubbleGroup.interactable = false;
            _tailShadow = UiBuilder.Image("TailShadow", _bubble, null, TailShadowColor);
            _tailRim = UiBuilder.Image("TailRim", _bubble, null, RimColor);
            Image rim = UiBuilder.Sliced("Rim", _bubble, art.RoundedRect, RimColor);
            rim.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(rim.rectTransform);
            rim.rectTransform.offsetMin = new Vector2(-BubbleRimDp, -BubbleRimDp);
            rim.rectTransform.offsetMax = new Vector2(BubbleRimDp, BubbleRimDp);
            _tailPlate = UiBuilder.Image("Tail", _bubble, null, GlassColor);
            Image plate = UiBuilder.Sliced("Plate", _bubble, art.RoundedRect, GlassColor);
            plate.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(plate.rectTransform);
            _label = UiBuilder.Label("Text", _bubble, TextStyle.PlainBold, string.Empty, BubbleTextSp, Palette.UiInk, TextAlignmentOptions.Center);
            _label.textWrappingMode = TextWrappingModes.Normal;
            _label.enableAutoSizing = false;
            UiBuilder.Stretch(_label.rectTransform);
            _label.rectTransform.offsetMin = new Vector2(BubblePadXDp, BubblePadYDp);
            _label.rectTransform.offsetMax = new Vector2(-BubblePadXDp, -BubblePadYDp);
            _bubble.gameObject.SetActive(false);

            // La tarjeta del Dato del perezoso: la placa del Dato del río (ArrivalStrip), con el retrato y el dato.
            _card = UiBuilder.Rect("HomeSlothFactCard", parent);
            _cardGroup = _card.gameObject.AddComponent<CanvasGroup>();
            _cardGroup.blocksRaycasts = false;
            _cardGroup.interactable = false;
            Image edge = UiBuilder.Sliced("Edge", _card, art.RoundedRect, ArrivalStrip.PlateEdge);
            edge.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / ArrivalStrip.PlateRadiusDp;
            UiBuilder.Stretch(edge.rectTransform);
            Image back = UiBuilder.Sliced("Back", _card, art.RoundedRect, Palette.PanelLight);
            back.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / (ArrivalStrip.PlateRadiusDp - 1f);
            UiBuilder.Stretch(back.rectTransform);
            back.rectTransform.offsetMin = new Vector2(1f, 1f);
            back.rectTransform.offsetMax = new Vector2(-1f, -1f);
            _cardPortrait = UiBuilder.Image("Portrait", _card, null, Color.white);
            _cardPortrait.preserveAspect = true;
            _cardLabel = UiBuilder.Label("Fact", _card, TextStyle.Body, string.Empty, FactCardTextSp, Palette.UiInk, TextAlignmentOptions.TopLeft);
            _cardLabel.textWrappingMode = TextWrappingModes.Normal;
            _cardLabel.enableAutoSizing = false;
            _card.gameObject.SetActive(false);

            // El reloj del inicio: avanza mientras el lienzo del inicio está activo; activarlo abre una visita.
            var driverGo = new GameObject("HomeSloth", typeof(RectTransform));
            driverGo.transform.SetParent(parent, false);
            _driver = driverGo.AddComponent<Driver>();
            _driver.Owner = this;
            if (_driver.isActiveAndEnabled)
            {
                BeginVisit();
            }
        }

        // Lo que dice TiTino ahora: el saludo, en el bocadillo; el Dato del perezoso, en su tarjeta. Nunca los dos a la vez.
        private enum NoteKind : byte
        {
            None = 0,
            Greeting = 1,
            Fact = 2,
        }

        /// <summary>
        /// Los tests avanzan el reloj a mano (<see cref="Tick"/>): ningún componente lo avanza en cada fotograma. Quien lo enciende
        /// lo apaga en su TearDown.
        /// </summary>
        internal static bool ManualTickForTests { get; set; }

        /// <summary><c>ui_sloth_enabled</c> (08 §9; <see cref="GameConfig.UiSlothEnabled"/>): apagado, TiTino no hace nada.</summary>
        public bool Enabled { get; set; } = GameConfig.UiSlothEnabled;

        /// <summary>TiTino se ve en la ventanita (con el rig o, sin él, con el retrato).</summary>
        public bool Shown => _shown;

        /// <summary>El rig (null hasta que la ventanita se ve, o sin arte).</summary>
        public SlothRig? Rig => _rig;

        /// <summary>La pose que hace ahora (la de la hora o el gesto en curso).</summary>
        public SlothPoseId Pose => _gestureMs >= 0f ? _gesture : _basePose;

        /// <summary>La pose de la hora (<see cref="BasePoseFor"/>), sin el gesto.</summary>
        public SlothPoseId BasePose => _basePose;

        /// <summary>Hay un gesto en curso (<c>sloth_wave</c> o <c>sloth_look</c>).</summary>
        public bool Gesturing => _gestureMs >= 0f;

        /// <summary>Área táctil de TiTino (activa solo con TiTino a la vista).</summary>
        public Button TapButton => _tap;

        /// <summary>Caja del área táctil.</summary>
        public RectTransform TapRect => (RectTransform)_tap.transform;

        /// <summary>El bocadillo (solo el saludo del día) está a la vista (con alfa &gt; 0 o a punto de entrar).</summary>
        public bool BubbleShown => _noteMs >= 0f && _noteKind == NoteKind.Greeting;

        /// <summary>El bocadillo lleva el saludo del día: el bocadillo solo lleva sus frases (D-013, punto 6).</summary>
        public bool BubbleIsGreeting => BubbleShown;

        /// <summary>Texto del bocadillo («» sin bocadillo).</summary>
        public string BubbleText => BubbleShown ? _label.text : string.Empty;

        /// <summary>Opacidad del bocadillo.</summary>
        public float BubbleAlpha => BubbleShown ? _bubbleGroup.alpha : 0f;

        /// <summary>Caja del bocadillo (sin el borde ni el pico).</summary>
        public RectTransform BubbleRect => _bubble;

        /// <summary>Texto del bocadillo (tests de desbordes).</summary>
        public TextMeshProUGUI BubbleLabel => _label;

        /// <summary>Milisegundos que dura el bocadillo de ahora (<see cref="NoticeTiming.ReadMs(string)"/>).</summary>
        public float BubbleDurationMs => BubbleShown ? _noteOutMs : 0f;

        /// <summary>La tarjeta del Dato del perezoso está a la vista (con alfa &gt; 0 o a punto de entrar).</summary>
        public bool FactCardShown => _noteMs >= 0f && _noteKind == NoteKind.Fact;

        /// <summary>Texto de la tarjeta («» sin tarjeta).</summary>
        public string FactCardText => FactCardShown ? _cardLabel.text : string.Empty;

        /// <summary>Opacidad de la tarjeta.</summary>
        public float FactCardAlpha => FactCardShown ? _cardGroup.alpha : 0f;

        /// <summary>Caja de la tarjeta, con su borde.</summary>
        public RectTransform FactCardRect => _card;

        /// <summary>Texto de la tarjeta (tests de desbordes).</summary>
        public TextMeshProUGUI FactCardLabel => _cardLabel;

        /// <summary>Retrato de TiTino de la tarjeta.</summary>
        public Image FactCardPortrait => _cardPortrait;

        /// <summary>Milisegundos que dura la tarjeta de ahora (<see cref="NoticeTiming.ReadMs(string)"/>).</summary>
        public float FactCardDurationMs => FactCardShown ? _noteOutMs : 0f;

        /// <summary>Lo que se decidió en el último saludo del inicio (default antes del primero).</summary>
        public SlothLine LastGreeting { get; private set; }

        /// <summary>Visitas al inicio en las que se decidió el saludo.</summary>
        public int GreetingDecisions { get; private set; }

        /// <summary>Ya salió el Dato del perezoso en esta sesión (como mucho uno; d.7).</summary>
        public bool FactShownThisSession { get; private set; }

        /// <summary>Clave del último Dato del perezoso enseñado (null si aún ninguno).</summary>
        public string? FactKey { get; private set; }

        /// <summary>Toques en TiTino en esta sesión.</summary>
        public int Taps { get; private set; }

        /// <summary>
        /// Pose de TiTino a la hora local <paramref name="hour"/> (d.7) [P]: de <see cref="NightFromHour"/> a
        /// <see cref="NightToHour"/>, <c>sloth_sleep</c>; de <see cref="SunFromHour"/> a <see cref="SunToHour"/>,
        /// <c>sloth_sunbathe</c>; el resto del día, <c>sloth_idle</c>.
        /// </summary>
        public static SlothPoseId BasePoseFor(int hour)
        {
            if (hour >= NightFromHour || hour < NightToHour)
            {
                return SlothPoseId.Sleep;
            }

            return hour >= SunFromHour && hour < SunToHour ? SlothPoseId.Sunbathe : SlothPoseId.Idle;
        }

        /// <summary>
        /// El Dato del perezoso que toca hoy (día <paramref name="day"/>, <see cref="SlothLines.TryDayNumber"/>) [P]: rota cada día
        /// por los 11 de g.3 desde <c>day mod 11</c> y salta los que no tienen texto que se pueda enseñar (en release, los que el
        /// asesor no aprobó). «» si no hay ninguno.
        /// </summary>
        public static string FactFor(int day, out string? key)
        {
            var keys = SlothLines.SlothFactKeys;
            int count = keys.Count;
            int start = ((day % count) + count) % count;
            for (int i = 0; i < count; i++)
            {
                string k = keys[(start + i) % count];
                string text = UiText.SlothFact(k);
                if (text.Length > 0)
                {
                    key = k;
                    return text;
                }
            }

            key = null;
            return string.Empty;
        }

        /// <summary>
        /// Fija a mano lo que TiTino necesita del juego (si no, lo toma del <see cref="GameBootstrap"/> del que cuelga):
        /// <paramref name="clock"/>, la sesión de TiTino (<paramref name="session"/>), el <c>sloth</c> del perfil
        /// (<paramref name="data"/>) y cómo guardarlo (<paramref name="save"/>), <c>settings.sloth_messages</c>
        /// (<paramref name="messages"/>), el nivel de «Jugar» (<paramref name="nextLevel"/>), Reducir movimiento
        /// (<paramref name="reduceMotion"/>) y si el inicio es la pantalla de ahora (<paramref name="inLobby"/>).
        /// </summary>
        public void Bind(IClock clock, SlothSession session, SlothData data, Action? save, Func<bool> messages, Func<int> nextLevel,
            Func<bool> reduceMotion, Func<bool> inLobby)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _save = save;
            _messages = messages ?? throw new ArgumentNullException(nameof(messages));
            _nextLevel = nextLevel ?? throw new ArgumentNullException(nameof(nextLevel));
            _reduceMotion = reduceMotion ?? throw new ArgumentNullException(nameof(reduceMotion));
            _inLobby = inLobby ?? throw new ArgumentNullException(nameof(inLobby));
            _bound = true;
        }

        /// <summary>
        /// Coloca el área táctil sobre el marco (con su esquina en <paramref name="x"/>, <paramref name="yTop"/> dp) y el bocadillo
        /// o la tarjeta debajo, sin pasar de <paramref name="maxRight"/>.
        /// </summary>
        public void Layout(float x, float yTop, float maxRight)
        {
            _x = x;
            _y = yTop;
            _maxRight = maxRight;
            float side = Mathf.Max(HomeWindow.SizeDp, HomeWindow.MinTapDp);
            float inset = (HomeWindow.SizeDp - side) * 0.5f;
            UiBuilder.Place(TapRect, x + inset, yTop + inset, side, side);
            if (BubbleShown)
            {
                PlaceBubble();
            }
            else if (FactCardShown)
            {
                PlaceCard();
            }
        }

        /// <summary>
        /// La ventanita se ve (<paramref name="on"/>) o se oculta (Sin canoas, una pantalla de pago, sin parada o sin arte): con
        /// ella, el rig, el área táctil y el bocadillo o la tarjeta, que se pierden si estaban a la vista.
        /// </summary>
        public void SetShown(bool on)
        {
            if (on == _shown)
            {
                return;
            }

            _shown = on;
            bool active = on && Enabled;
            if (_tap.gameObject.activeSelf != active)
            {
                _tap.gameObject.SetActive(active);
            }

            if (!on)
            {
                HideNote();
                _rig?.SetVisible(false);
                return;
            }

            if (!Enabled)
            {
                // ui_sloth_enabled apagado: la ventanita con su retrato quieto, sin rig ni toque.
                _rig?.SetVisible(false);
                _home.PortraitImage.enabled = true;
                return;
            }

            EnsureRig();
            _gestureMs = -1f;
            bool resolved = Resolve();
            _poseCheckMs = resolved ? 0f : PoseCheckMs; // sin el juego aún, la hora se mira en el siguiente paso
            _basePose = resolved ? BasePoseFor(_clock!.LocalNow().Hour) : SlothPoseId.Idle;
            ApplyBasePose();
        }

        /// <summary>Empieza una visita al inicio: el saludo del día se decide en cuanto TiTino se vea.</summary>
        public void BeginVisit()
        {
            _visitPending = true;
        }

        /// <summary>Acaba la visita al inicio (se va a un nivel): el bocadillo o la tarjeta se pierden.</summary>
        public void EndVisit()
        {
            _visitPending = false;
            HideNote();
        }

        /// <summary>
        /// Toque en TiTino (d.7): <c>sloth_look</c> y, si aún no salió en esta sesión y hay voz, el Dato del perezoso de hoy en su
        /// tarjeta con el retrato (nunca en el bocadillo: D-013, punto 6). false si TiTino no se ve o está apagado.
        /// </summary>
        public bool Tap()
        {
            if (!_shown || !Enabled)
            {
                return false;
            }

            Taps++;
            PlayGesture(SlothPoseId.Look, SlothRig.LookMs);
            if (FactShownThisSession || !Resolve() || !_messages!())
            {
                return true;
            }

            string today = _clock!.LocalDateKey();
            SlothLines.TryDayNumber(today, out int day);
            string text = FactFor(day, out string? key);
            Sprite? portrait = text.Length > 0 ? _art.SlothPortrait(false) : null;
            if (portrait == null)
            {
                return true; // en release, sin datos aprobados (o sin el retrato final): solo el gesto
            }

            FactShownThisSession = true;
            FactKey = key;
            _cardPortrait.sprite = portrait;
            ShowNote(text, NoteKind.Fact);
            return true;
        }

        /// <summary>
        /// Avanza <paramref name="dtMs"/> ms: decide el saludo de la visita en cuanto TiTino se ve, mira la hora para la pose,
        /// acaba los gestos y lleva el bocadillo. Sin asignaciones (salvo el saludo, una vez por visita).
        /// </summary>
        public void Tick(float dtMs)
        {
            dtMs = Mathf.Clamp(dtMs, 0f, MaxStepMs);
            if (!_shown || !Enabled)
            {
                return;
            }

            if (_visitPending && Resolve() && _inLobby!())
            {
                _visitPending = false;
                Greet();
            }

            if (_gestureMs >= 0f)
            {
                _gestureMs += dtMs;
                _rig?.Tick(dtMs);
                if (_gestureMs >= _gestureEndMs)
                {
                    _gestureMs = -1f;
                    ApplyBasePose();
                }
                else if (_gesture == SlothPoseId.Wave && _rig != null)
                {
                    PlaceRig(SlothPoseId.Wave); // la bajada del saludo sigue a la garra
                }
            }
            else
            {
                _rig?.Tick(dtMs);
                _poseCheckMs += dtMs;
                if (_poseCheckMs >= PoseCheckMs)
                {
                    _poseCheckMs = 0f; // también si el juego aún arranca: se vuelve a mirar al cabo de PoseCheckMs
                    if (Resolve())
                    {
                        SlothPoseId pose = BasePoseFor(_clock!.LocalNow().Hour);
                        if (pose != _basePose)
                        {
                            _basePose = pose;
                            ApplyBasePose();
                        }
                    }
                }
            }

            TickNote(dtMs);
        }

        // El saludo de la visita (d.7; SlothLines.ForHome): una vez por visita, con lo guardado en sloth. La frase solo se dice si
        // tiene texto que se pueda enseñar (en release, aprobada); si no, el saludo es solo el gesto y cuenta igual para el tope
        // de 1 al día.
        private void Greet()
        {
            IClock clock = _clock!;
            SlothSession session = _session!;
            SlothData data = _data!;
            string today = clock.LocalDateKey();
            int hour = clock.LocalNow().Hour;
            int[] last = data.LastLine;
            int returnIndex = (int)SlothMoment.Return;
            string? seenBefore = data.LastSeenDate;
            string? greetedBefore = data.LastGreetingDate;
            int lineBefore = returnIndex < last.Length ? last[returnIndex] : SlothData.NoLine;

            var context = new SlothContext(session.Enabled && Enabled, _messages!(), session.FailEnabled, session.WinLineEveryN,
                session.LinesPerSessionMax, session.LinesThisSession, session.PreviousScreenSpoke, last, session.PreviousScreenMapIntro)
                .WithHome(new SlothHomeFacts(today, hour, seenBefore, greetedBefore, _nextLevel!()));
            SlothLine line = SlothLines.ForHome(in context);
            string text = string.Empty;
            bool speaks = line.Speaks && UiText.TrySlothLine(in line, out text);
            SlothLine shown = speaks
                ? line
                : new SlothLine(SlothScreen.Home, line.Gesture, line.Moment, SlothData.NoLine, null, 0, line.Greeting, line.Deferred);
            session.Record(in shown, in context);

            // El sloth del perfil, aunque la sesión lleve el suyo: la última apertura, el saludo del día y la frase vista. Con el
            // saludo aplazado tras el bocadillo del Mapa del 10 (D-028, pregunta 60 b), nada: el saludo queda para la visita
            // siguiente (si aquí se apuntara la apertura de hoy, el de la mañana ya no tocaría).
            if (!shown.Deferred)
            {
                data.LastSeenDate = today;
            }

            if (shown.Greeting)
            {
                data.LastGreetingDate = today;
            }

            if (shown.Speaks && returnIndex < last.Length)
            {
                last[returnIndex] = shown.Index;
            }

            LastGreeting = shown;
            GreetingDecisions++;
            bool changed = !string.Equals(seenBefore, data.LastSeenDate, StringComparison.Ordinal)
                || !string.Equals(greetedBefore, data.LastGreetingDate, StringComparison.Ordinal)
                || (returnIndex < last.Length && last[returnIndex] != lineBefore);
            if (changed)
            {
                _save?.Invoke();
            }

            if (shown.Gesture == SlothGesture.Wave)
            {
                PlayGesture(SlothPoseId.Wave, SlothRig.WaveMs);
            }

            if (speaks)
            {
                ShowNote(text, NoteKind.Greeting);
            }
        }

        // Lo que necesita del juego: lo fijado con Bind o, la primera vez que hace falta, lo del GameBootstrap del que cuelga el
        // inicio (sin él, un sloth propio sin guardar, la sesión por defecto y el reloj del juego).
        private bool Resolve()
        {
            return _bound || BindFromGame();
        }

        // Mientras el arranque no termina se llama en cada fotograma: sin lambdas ni búsquedas repetidas (el GameBootstrap se
        // busca una vez), nada asigna memoria. Los cierres se crean solo al enlazar, una vez (BindToGame, BindOwn).
        private bool BindFromGame()
        {
            if (!_bootLooked)
            {
                _boot = _driver != null ? _driver.GetComponentInParent<GameBootstrap>(true) : null;
                _bootLooked = true;
            }

            GameBootstrap? boot = _boot;
            if (ReferenceEquals(boot, null))
            {
                BindOwn();
                return true;
            }

            if (boot == null || boot.Profile == null || boot.App == null || boot.View == null)
            {
                return false; // el arranque aún no terminó (o el juego se está cerrando)
            }

            BindToGame(boot);
            return true;
        }

        // Sin juego del que colgar: un sloth propio sin guardar, la sesión por defecto y el reloj del juego.
        private void BindOwn()
        {
            var own = new SlothData();
            Bind(LaunchOptions.ClockOverride ?? SystemClock.Instance, new SlothSession(own, true), own, null, () => true, () => 0,
                () => false, () => true);
        }

        // Aparte de BindFromGame: las lambdas capturan boot y el compilador crea su clase de cierre al entrar en el método; allí,
        // eso asignaría memoria en cada fotograma mientras el arranque no termina.
        private void BindToGame(GameBootstrap boot)
        {
            Profile profile = boot.Profile!;
            AppFlow app = boot.App!;
            // Solo con el inicio delante: con «El río sigue creciendo» (ContentEnd), el velo y el modal tapan la ventanita y el
            // saludo del día (como mucho 1; D-016, punto 5) se gastaría sin verse. Se decide al cerrarlo, en la misma visita.
            Bind(LaunchOptions.ClockOverride ?? SystemClock.Instance, boot.View!.SlothSession, profile.Sloth, () => SaveProfile(boot),
                () => profile.Settings.SlothMessages, () => app.PlayLevel, () => boot.Flow != null && boot.Flow.ReduceMotion,
                () => app.Screen == AppScreen.Lobby);
        }

        // Guarda el perfil por el lote del juego (en segundo plano con el perfil en disco). Con el perfil en solo lectura (08 §4.4),
        // nada: queda en memoria para esta sesión.
        private static void SaveProfile(GameBootstrap boot)
        {
            Profile? profile = boot.Profile;
            ProfileSaveBatch? batch = boot.SaveBatch;
            if (profile != null && batch != null && !boot.ProfileLocked)
            {
                batch.TrySave(profile);
            }
        }

        // El rig, la primera vez que TiTino se ve; sin arte (release sin el PNG final), el retrato del marco hace de TiTino.
        private void EnsureRig()
        {
            if (_rig != null || _rigMissing)
            {
                return;
            }

            SlothArt? art = _art.SlothRigArt();
            _rig = art != null ? SlothRig.Create(_hole, art, RigSizeDp) : null;
            _rigMissing = _rig == null;
            if (_rig != null)
            {
                // La rama del rig (su pieza «Branch»): solo se pinta con TiTino tumbado o en ovillo (PlaceRig).
                Transform branch = _rig.Root.Find("Branch");
                _rigBranch = branch != null ? branch.GetComponent<Image>() : null;
                _rigBranchArt = _rigBranch != null && _rigBranch.enabled;
            }
        }

        /// <summary>
        /// La rama del rig se pinta: solo con TiTino tumbado o en ovillo (<c>sloth_sunbathe</c>, <c>sloth_sleep</c>), sobre la
        /// rama de abajo; colgado, se agarra a la rama pintada de la ventanita. false sin rig.
        /// </summary>
        public bool RigBranchShown => _rigBranch != null && _rigBranch.enabled;

        private bool ReduceMotionNow => _reduceMotion != null && _reduceMotion();

        // La pose de la hora: el rig (con el retrato oculto) o, sin rig, el retrato con los ojos cerrados de noche.
        private void ApplyBasePose()
        {
            if (_rig == null)
            {
                _home.PortraitImage.enabled = true;
                _home.SetAsleep(_basePose == SlothPoseId.Sleep);
                return;
            }

            _home.PortraitImage.enabled = false;
            PlaceRig(_basePose);
            _rig.Play(_basePose, ReduceMotionNow);
        }

        private void PlayGesture(SlothPoseId pose, float ms)
        {
            _gesture = pose;
            _gestureMs = 0f;
            _gestureEndMs = ms;
            if (_rig == null)
            {
                return; // el retrato no hace gestos
            }

            PlaceRig(pose);
            _rig.Play(pose, ReduceMotionNow);
        }

        // Colgado, de la rama pintada de la embaúba, sin pintar la del rig (si no, se verían dos ramas, y durante sloth_wave una
        // bajaría con él); tumbado o en ovillo, sobre la rama del rig, más baja: entero dentro del hueco. Durante sloth_wave, el
        // rig baja con la garra (WaveDrop) para que el marco no la corte; las otras garras siguen agarradas a la rama pintada
        // (gruesa y más baja hacia la izquierda), como mucho por su canto de abajo.
        private void PlaceRig(SlothPoseId pose)
        {
            float border = ProceduralArt.HomeWindowBorderDp;
            bool lying = pose == SlothPoseId.Sunbathe || pose == SlothPoseId.Sleep;
            bool branchOn = lying && _rigBranchArt;
            if (_rigBranch != null && _rigBranch.enabled != branchOn)
            {
                _rigBranch.enabled = branchOn;
            }

            float y = (lying ? LieBranchDp : HangBranchDp) - border;
            if (pose == SlothPoseId.Wave && _gestureMs >= 0f && !_rig!.ReduceMotion)
            {
                y += WaveDrop(_gestureMs);
            }

            _rig!.Place((HomeWindow.SizeDp * 0.5f) - border, y);
        }

        /// <summary>
        /// Lo que baja el rig a los <paramref name="ms"/> ms de <c>sloth_wave</c> (dp): baja con la garra, se queda en
        /// <see cref="WaveDropDp"/> mientras está levantada y vuelve con ella (<c>min(1; 1,6 · sen(π · t / 0,8 s))</c>); 0 fuera
        /// del gesto.
        /// </summary>
        public static float WaveDrop(float ms)
        {
            if (ms <= 0f || ms >= SlothRig.WaveMs)
            {
                return 0f;
            }

            return WaveDropDp * Mathf.Clamp01(1.6f * Mathf.Sin(Mathf.PI * ms / SlothRig.WaveMs));
        }

        // El saludo, en el bocadillo; el dato, en su tarjeta: uno solo a la vista, con su tiempo de lectura (D-011).
        private void ShowNote(string text, NoteKind kind)
        {
            HideNote();
            _noteKind = kind;
            _noteMs = 0f;
            _noteOutMs = NoticeTiming.ReadMs(text);
            _noteReduceMotion = ReduceMotionNow;
            float alpha = _noteReduceMotion ? 1f : 0f;
            if (kind == NoteKind.Fact)
            {
                _cardLabel.text = text;
                _cardGroup.alpha = alpha;
                _card.gameObject.SetActive(true);
                PlaceCard();
            }
            else
            {
                _label.text = text;
                _bubbleGroup.alpha = alpha;
                _bubble.gameObject.SetActive(true);
                PlaceBubble();
            }
        }

        private void HideNote()
        {
            if (_noteMs < 0f && !_bubble.gameObject.activeSelf && !_card.gameObject.activeSelf)
            {
                return;
            }

            _noteMs = -1f;
            _noteKind = NoteKind.None;
            _bubble.gameObject.SetActive(false);
            _card.gameObject.SetActive(false);
        }

        private void TickNote(float dtMs)
        {
            if (_noteMs < 0f)
            {
                return;
            }

            _noteMs += dtMs;
            if (_noteMs >= _noteOutMs + (_noteReduceMotion ? 0f : BubbleFadeMs))
            {
                HideNote();
                return;
            }

            float alpha = 1f;
            if (!_noteReduceMotion)
            {
                float fadeIn = Mathf.Clamp01(_noteMs / BubbleFadeMs);
                float fadeOut = 1f - Mathf.Clamp01((_noteMs - _noteOutMs) / BubbleFadeMs);
                alpha = Mathf.Min(fadeIn, fadeOut);
            }

            CanvasGroup group = _noteKind == NoteKind.Fact ? _cardGroup : _bubbleGroup;
            if (!Mathf.Approximately(group.alpha, alpha))
            {
                group.alpha = alpha;
            }
        }

        // Ancho y alto del texto de label en como mucho inner dp: ajustado al texto si cabe en una línea y, si no, el ancho más
        // estrecho con las mismas líneas (quedan parejas y sin una palabra sola en la última, como el bocadillo de la victoria). El
        // salto lo decide el ancho, nunca un salto añadido. Solo al colocar, nunca por fotograma.
        private static void FitText(TextMeshProUGUI label, float inner, out float textWidth, out float textHeight)
        {
            string text = label.text;
            float single = label.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x;
            if (single + FitSlackDp <= inner)
            {
                textWidth = single + FitSlackDp;
                textHeight = label.GetPreferredValues(text, textWidth, float.PositiveInfinity).y;
                return;
            }

            textHeight = label.GetPreferredValues(text, inner, float.PositiveInfinity).y;
            float lo = Mathf.Max(1f, Mathf.Min(inner, single * 0.5f));
            float hi = inner;
            for (int i = 0; i < 12 && hi - lo > 0.5f; i++)
            {
                float mid = 0.5f * (lo + hi);
                if (label.GetPreferredValues(text, mid, float.PositiveInfinity).y <= textHeight + 0.5f)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid;
                }
            }

            textWidth = Mathf.Min(inner, hi + FitSlackDp);
        }

        // Debajo del marco, alineado a su izquierda, con el pico arriba hacia TiTino (y su sombra de 1 dp): el texto a 14 sp con la
        // escala de texto, ajustado (FitText); crece hacia abajo (02 §12). Solo al enseñarlo o al recolocar el inicio.
        private void PlaceBubble()
        {
            float left = _x;
            float top = _y + HomeWindow.SizeDp + BubbleGapDp + TailOutDp;
            float maxWidth = Mathf.Max((2f * BubblePadXDp) + 24f, Mathf.Min(BubbleMaxWidthDp, _maxRight - BubbleRimDp - left));
            FitText(_label, maxWidth - (2f * BubblePadXDp), out float textWidth, out float textHeight);
            float width = Mathf.Ceil(textWidth + (2f * BubblePadXDp));
            float height = Mathf.Ceil(textHeight + (2f * BubblePadYDp));
            width = Mathf.Max(width, HomeWindow.SizeDp);
            UiBuilder.Place(_bubble, left, top, width, height);
            float tailX = TailXDp; // bajo la garra de delante, a la derecha del centro del marco
            PlaceTail(_tailShadow.rectTransform, tailX, TailDp + (2f * BubbleRimDp) + (2f * TailShadowDp));
            PlaceTail(_tailRim.rectTransform, tailX, TailDp + (2f * BubbleRimDp));
            PlaceTail(_tailPlate.rectTransform, tailX, TailDp);
        }

        // La tarjeta del dato, debajo del marco, alineada a su izquierda y como mucho hasta el margen derecho: el retrato arriba a la
        // izquierda con el relleno de la placa del Dato del río, y el texto a su derecha (FitText), centrado en el alto del
        // retrato si es más bajo. Crece hacia abajo con el texto (02 §12). Solo al enseñarla o al recolocar el inicio.
        private void PlaceCard()
        {
            const float pad = ArrivalStrip.PlatePadDp;
            const float portrait = ArrivalStrip.PortraitDp;
            const float chrome = pad + portrait + ArrivalStrip.PortraitGapDp + ArrivalStrip.PlateRightPadDp;
            float left = _x;
            float top = _y + HomeWindow.SizeDp + FactCardGapDp;
            float inner = Mathf.Max(24f, _maxRight - left - chrome);
            FitText(_cardLabel, inner, out float textWidth, out float textHeight);
            textWidth = Mathf.Min(inner, Mathf.Ceil(textWidth));
            textHeight = Mathf.Ceil(textHeight);
            float contentH = Mathf.Max(portrait, textHeight);
            UiBuilder.Place(_card, left, top, chrome + textWidth, contentH + (2f * pad));
            UiBuilder.Place(_cardPortrait.rectTransform, pad, pad, portrait, portrait);
            float textTop = pad + Mathf.Floor((contentH - textHeight) * 0.5f);
            UiBuilder.Place(_cardLabel.rectTransform, pad + portrait + ArrivalStrip.PortraitGapDp, textTop, textWidth, textHeight);
        }

        private static void PlaceTail(RectTransform rt, float x, float size)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        /// <summary>
        /// El reloj de TiTino en el inicio: un componente sin gráficos en el lienzo del inicio. Se activa y se desactiva con él
        /// (abre y cierra las visitas) y lo avanza en cada fotograma con el tiempo sin escala, salvo en los tests que lo avanzan a
        /// mano (<see cref="ManualTickForTests"/>).
        /// </summary>
        private sealed class Driver : MonoBehaviour
        {
            public HomeSloth? Owner;

            private void OnEnable()
            {
                Owner?.BeginVisit();
            }

            private void OnDisable()
            {
                Owner?.EndVisit();
            }

            private void Update()
            {
                if (Owner != null && !ManualTickForTests)
                {
                    Owner.Tick(Time.unscaledDeltaTime * 1000f);
                }
            }
        }
    }
}
