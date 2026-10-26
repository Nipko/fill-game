#nullable enable
using System;
using System.Collections.Generic;
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
    /// TiTino en el inicio (D-050, punto 1, «el muelle del río»; antes, tarea TI6c en la ventanita de casa; propuesta de TiTino
    /// d.7; D-016, punto 5; 07 §5.4 y §6.5, poses <c>sloth_*</c>): grande, colgado de su rama arriba a la izquierda (la caja de
    /// <see cref="HomeWindow"/> en su modo del inicio), con un bocadillo pegado a él que rota mensajes.
    /// <list type="bullet">
    /// <item><b>Pose por la hora local</b> (<see cref="BasePoseFor"/>, del <see cref="IClock"/> del juego): de día,
    /// <c>sloth_idle</c> o, en las horas de sol, <c>sloth_sunbathe</c>; de noche (20-6), <c>sloth_sleep</c>. Se mira cada
    /// <see cref="PoseCheckMs"/> ms: si la hora cambia con el inicio abierto, cambia la pose.</item>
    /// <item><b>Saludo</b> (<see cref="SlothLines.ForHome"/>): al volver tras ≥ 3 días, el de regreso; en la primera apertura de la
    /// franja de 06:00 a 12:00 (D-030, pregunta 17 b), el de la mañana; como mucho uno al día entre los dos
    /// (<c>sloth.last_greeting_date</c>), con <c>sloth_wave</c> y su frase en el bocadillo. Se decide una vez por visita al inicio,
    /// con el inicio delante (<see cref="AppScreen.Lobby"/>); en cada visita se apunta <c>sloth.last_seen_date</c>. Justo después
    /// del bocadillo del Mapa del 10 (D-028, pregunta 60 b), el saludo no sale ni se gasta.</item>
    /// <item><b>El bocadillo que rota</b> (D-050, punto 1): tras el saludo (o sin él), cada <see cref="RotationGapMs"/> ms, uno de
    /// estos, con su tiempo de lectura (D-011): «hoy hay Reto nuevo» si el Reto de hoy está sin jugar, lo que falta para la
    /// próxima Canasta, «Casa · a unos {km} km» (la información de la ventanita de casa de antes), el Dato del perezoso de hoy
    /// (<see cref="UiText.SlothFact"/>: en release, solo los aprobados por el asesor) y, antes del nivel 10 o si no hay nada más
    /// que contar, que anima a jugar. Nunca promociona compras ni señala el Cántaro (D-013, Q10-A). El bocadillo crece hacia abajo
    /// hasta el muelle; si llega a la travesía, el inicio aparta lo tapado mientras dure. Un mensaje que no cabe se salta.</item>
    /// <item><b>Toque</b> (<see cref="Tap"/>): <c>sloth_look</c> y el mensaje siguiente al momento (el Dato del día, si aún no salió).
    /// Tocarlo no lo cuida ni da premios (d.7).</item>
    /// <item><b>De noche duerme callado</b> (añadido de D-050 del 2026-10-26: «De noche TiTino duerme callado (solo un Zzz); si lo
    /// toco, se despierta un momento y dice su mensaje»): con <c>sloth_sleep</c> no rota el bocadillo ni saluda (el saludo de la
    /// visita no se dice ni se gasta, aunque la visita cuenta para <c>sloth.last_seen_date</c>), y junto a su cabeza sube un «Zzz»
    /// dibujado (<see cref="HomeSlothZzz"/>; quieto con Reducir movimiento). Al tocarlo se despierta (<c>sloth_wake</c>, después
    /// <c>sloth_idle</c>), dice el mensaje siguiente de la rotación con las reglas del día y, cuando el mensaje se va (y como
    /// mínimo <see cref="NightAwakeMs"/> ms), vuelve a dormirse. Con <c>settings.sloth_messages</c> apagado, se despierta y se
    /// vuelve a dormir sin mensaje. TalkBack lee que duerme (<c>home_sloth_asleep_a11y</c>).</item>
    /// <item><b>Nunca</b> con Sin canoas, la hoja de compra ni una oferta (lo oculta <see cref="HomeWindow.Apply"/>).</item>
    /// </list>
    /// <para>Con <c>settings.sloth_messages</c> apagado, solo gestos (sin bocadillo); con <c>ui_sloth_enabled</c> apagado, nada: TiTino
    /// queda quieto. Sin el arte del rig (release sin el PNG final), el retrato hace de TiTino.</para>
    /// <para>Avanza solo con el inicio a la vista (un componente en el lienzo del inicio); nada asigna memoria por fotograma: los textos
    /// de la rotación se arman una vez por visita y cada mensaje se coloca al salir.</para>
    /// </summary>
    internal sealed class HomeSloth
    {
        /// <summary>Hora local desde la que es de noche y TiTino duerme (20:00) [P].</summary>
        public const int NightFromHour = 20;

        /// <summary>Hora local en la que deja de ser de noche (06:00, la del saludo de la mañana) [P].</summary>
        public const int NightToHour = SlothLines.MorningFromHour;

        /// <summary>Primera hora local de sol, en la que toma el sol en la rama (10:00) [P].</summary>
        public const int SunFromHour = 10;

        /// <summary>Hora local en la que deja de tomar el sol, sin incluirla (16:00) [P].</summary>
        public const int SunToHour = 16;

        /// <summary>Cada cuánto se mira la hora para la pose (ms).</summary>
        public const float PoseCheckMs = 1000f;

        /// <summary>
        /// Lo que baja el rig, como mucho, durante <c>sloth_wave</c> (dp) [P]: el saludo se nota en todo el cuerpo. Entra y sale con
        /// el gesto (<see cref="WaveDrop"/>); con Reducir movimiento, nada.
        /// </summary>
        public const float WaveDropDp = 4f;

        /// <summary>Hueco entre la caja de TiTino y la punta del pico del bocadillo (dp) [P].</summary>
        public const float BubbleGapDp = 2f;

        /// <summary>Sombra del pico del bocadillo (dp) [P]: 1 dp de #000 al 15 %, que lo separa de lo claro del decorado.</summary>
        public const float TailShadowDp = 1f;

        /// <summary>Ancho máximo del bocadillo (dp) [P]; como mucho, hasta el margen derecho que da el inicio.</summary>
        public const float BubbleMaxWidthDp = 230f;

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

        /// <summary>Pausa entre un mensaje y el siguiente de la rotación (ms) [P].</summary>
        public const float RotationGapMs = 5000f;

        /// <summary>Espera hasta el primer mensaje de la rotación cuando no hay saludo (ms) [P].</summary>
        public const float FirstNoteDelayMs = 1200f;

        /// <summary>Paso más largo de un fotograma (ms): tras una pausa, el bocadillo no desaparece de golpe.</summary>
        public const float MaxStepMs = 100f;

        /// <summary>Nivel desde el que hay Reto y Bitácora (y la Canasta de cada parada); antes, TiTino anima a jugar.</summary>
        public const int NewsFromLevel = 10;

        /// <summary>
        /// Lo que se queda despierto como mínimo al tocarlo de noche (ms) [P]: con su mensaje, hasta que el mensaje se va; sin él
        /// (mensajes apagados), esto. Cabe entero <c>sloth_wake</c> (<see cref="SlothRig.WakeMs"/>).
        /// </summary>
        public const float NightAwakeMs = 4000f;

        // Arranque del «Zzz» desde el centro de la cabeza dormida (dp; y hacia abajo) [P]: arriba a su derecha, hacia el cielo
        // libre entre los saldos y la travesía, donde de día va el bocadillo.
        private const float ZzzFromHeadXDp = 20f;
        private const float ZzzFromHeadYDp = -10f;

        // Pico del bocadillo hacia TiTino: un cuadrado girado 45° (dp), medio fuera del bocadillo.
        private const float TailDp = 12f;
        private const float TailOutDp = (TailDp + (2f * BubbleRimDp)) * 0.7072f;

        // Altura del pico desde arriba del bocadillo (dp): a la altura de la cabeza de TiTino colgado.
        private const float TailYDp = 22f;

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
        private readonly Driver _driver;
        private readonly HomeSlothZzz _zzz;
        private readonly List<HomeNote> _ring = new List<HomeNote>(6);
        private readonly string[] _ringText = new string[6];

        private SlothRig? _rig;
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
        private Func<bool>? _dailyPending;
        private Func<int>? _levelsToBasket;
        private Func<bool>? _notesClear;
        private bool _ringMessages;
        private Action? _save;

        private SlothPoseId _basePose = SlothPoseId.Idle;
        private SlothPoseId _gesture;
        private float _gestureMs = -1f;
        private float _gestureEndMs;
        private float _poseCheckMs;
        private float _awakeMs = -1f;
        private bool _asleepReported;

        private HomeNote _noteKind;
        private float _noteMs = -1f;
        private float _noteOutMs;
        private bool _noteReduceMotion;
        private float _gapMs = -1f;
        private int _ringNext;
        private bool _ringBuilt;
        private float _x;
        private float _y;
        private float _boxW = HomeWindow.SceneWidthDp;
        private float _boxH = 130f;
        private float _branchInBox = HomeWindow.SceneAboveDp;
        private float _rigDp = HomeWindow.SceneRigMaxDp;
        private float _maxRight = LobbyScreen.ReferenceWidth;
        private float _maxBottom = 1000f;

        /// <summary>
        /// Crea a TiTino (sin rig hasta que se vea) en la caja de <paramref name="home"/>, con su área táctil y su bocadillo (ocultos)
        /// bajo <paramref name="parent"/>, el lienzo del inicio.
        /// </summary>
        public HomeSloth(HomeWindow home, Transform parent, ProceduralArt art)
        {
            _home = home;
            _art = art;
            _hole = UiBuilder.Rect("SlothHole", home.Root);
            UiBuilder.Stretch(_hole);

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

            // El «Zzz» de la noche, encima de la escena como el bocadillo; TalkBack lee que duerme (de día, como siempre).
            _zzz = new HomeSlothZzz(parent);
            A11yLabels.Set(_tap, () => Asleep ? UiText.HomeSlothAsleepA11y : string.Empty);

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

        /// <summary>Lo que dice TiTino en el bocadillo (D-050, punto 1).</summary>
        public enum HomeNote : byte
        {
            /// <summary>Nada.</summary>
            None = 0,

            /// <summary>El saludo del día (de la mañana o de regreso).</summary>
            Greeting = 1,

            /// <summary>«Hoy hay Reto del río nuevo» (<c>home_sloth_daily</c>).</summary>
            Daily = 2,

            /// <summary>Lo que falta para la próxima Canasta (<c>home_sloth_basket</c>).</summary>
            Basket = 3,

            /// <summary>El Dato del perezoso de hoy.</summary>
            Fact = 4,

            /// <summary>Que anima a jugar (<c>home_sloth_play</c>).</summary>
            Play = 5,

            /// <summary>Lo lejos que queda su casa: «Casa · a unos {km} km» (<c>map_home_distance</c>, la píldora de la ventanita de antes).</summary>
            Home = 6,
        }

        /// <summary>
        /// Los tests avanzan el reloj a mano (<see cref="Tick"/>): ningún componente lo avanza en cada fotograma. Quien lo enciende
        /// lo apaga en su TearDown.
        /// </summary>
        internal static bool ManualTickForTests { get; set; }

        /// <summary><c>ui_sloth_enabled</c> (08 §9; <see cref="GameConfig.UiSlothEnabled"/>): apagado, TiTino no hace nada.</summary>
        public bool Enabled { get; set; } = GameConfig.UiSlothEnabled;

        /// <summary>TiTino se ve (con el rig o, sin él, con el retrato).</summary>
        public bool Shown => _shown;

        /// <summary>El rig (null hasta que TiTino se ve, o sin arte).</summary>
        public SlothRig? Rig => _rig;

        /// <summary>
        /// La pose que hace ahora (la de la hora, o <c>sloth_idle</c> mientras está despierto de noche, o el gesto en curso).
        /// </summary>
        public SlothPoseId Pose => _gestureMs >= 0f ? _gesture : RestPose;

        /// <summary>
        /// Duerme (añadido de D-050 del 2026-10-26): se ve, es de noche (<c>sloth_sleep</c>) y no lo han despertado. Callado, con
        /// el «Zzz».
        /// </summary>
        public bool Asleep => _shown && Enabled && _basePose == SlothPoseId.Sleep && _awakeMs < 0f;

        /// <summary>De noche, lo despertó un toque y aún no ha vuelto a dormirse.</summary>
        public bool AwakeAtNight => _awakeMs >= 0f;

        /// <summary>El «Zzz» está a la vista.</summary>
        public bool ZzzShown => _zzz.Shown;

        /// <summary>Raíz del «Zzz» (tests).</summary>
        public RectTransform ZzzRect => _zzz.Root;

        /// <summary>Caja que ocupa el «Zzz» en todo su bucle (dp del lienzo, y hacia abajo; tests).</summary>
        public Rect ZzzBoundsDp => _zzz.BoundsDp;

        /// <summary>
        /// Avisa cuando TiTino se duerme o se despierta (<see cref="Asleep"/> cambia), con la causa: <c>shown</c> (aparece),
        /// <c>hidden</c> (se oculta), <c>hour</c> (cambia la hora), <c>tap</c> (lo despierta un toque) o <c>sleep</c> (vuelve a
        /// dormirse). Para las sondas del inicio; nunca por fotograma.
        /// </summary>
        public Action<bool, string>? AsleepChanged { get; set; }

        // La pose de reposo: la de la hora o, despierto de noche tras un toque, sloth_idle.
        private SlothPoseId RestPose => _awakeMs >= 0f && _basePose == SlothPoseId.Sleep ? SlothPoseId.Idle : _basePose;

        /// <summary>La pose de la hora (<see cref="BasePoseFor"/>), sin el gesto.</summary>
        public SlothPoseId BasePose => _basePose;

        /// <summary>Hay un gesto en curso (<c>sloth_wave</c>, <c>sloth_look</c> o, de noche, <c>sloth_wake</c>).</summary>
        public bool Gesturing => _gestureMs >= 0f;

        /// <summary>Área táctil de TiTino (activa solo con TiTino a la vista).</summary>
        public Button TapButton => _tap;

        /// <summary>Caja del área táctil.</summary>
        public RectTransform TapRect => (RectTransform)_tap.transform;

        /// <summary>El bocadillo está a la vista (con alfa &gt; 0 o a punto de entrar), con cualquier mensaje.</summary>
        public bool BubbleShown => _noteMs >= 0f && _noteKind != HomeNote.None;

        /// <summary>El bocadillo lleva el saludo del día.</summary>
        public bool BubbleIsGreeting => BubbleShown && _noteKind == HomeNote.Greeting;

        /// <summary>Qué lleva el bocadillo (<see cref="HomeNote.None"/> sin bocadillo).</summary>
        public HomeNote Note => BubbleShown ? _noteKind : HomeNote.None;

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

        /// <summary>Lo que rota esta visita, en orden (tests).</summary>
        public IReadOnlyList<HomeNote> Rotation => _ring;

        /// <summary>Tope de abajo del bocadillo que dio el inicio (dp; tests).</summary>
        public float NoteMaxBottom => _maxBottom;

        /// <summary>Lo que se decidió en el último saludo del inicio (default antes del primero).</summary>
        public SlothLine LastGreeting { get; private set; }

        /// <summary>Visitas al inicio en las que se decidió el saludo.</summary>
        public int GreetingDecisions { get; private set; }

        /// <summary>Ya salió el Dato del perezoso en esta sesión.</summary>
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

        /// <summary>Id de sonda de lo que dice el bocadillo (<c>none</c>, <c>greeting</c>, <c>daily</c>, <c>basket</c>, <c>fact</c>, <c>play</c> o <c>home</c>).</summary>
        public static string NoteId(HomeNote note)
        {
            switch (note)
            {
                case HomeNote.Greeting: return "greeting";
                case HomeNote.Daily: return "daily";
                case HomeNote.Basket: return "basket";
                case HomeNote.Fact: return "fact";
                case HomeNote.Play: return "play";
                case HomeNote.Home: return "home";
                default: return "none";
            }
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
        /// Lo que entra en la rotación del bocadillo (D-050, punto 1), en orden: el Reto de hoy sin jugar, la Canasta (con
        /// <paramref name="levelsToBasket"/> ≥ 1), la distancia a casa (con <paramref name="hasHome"/>: lejos de Belém), el Dato
        /// del día (con <paramref name="hasFact"/>) y, antes del nivel <see cref="NewsFromLevel"/> (delante, sin Reto ni Canasta) o
        /// si no hay nada más, que anima a jugar. Sin mensajes, nada.
        /// </summary>
        public static void RotationFor(bool messages, int nextLevel, bool dailyPending, int levelsToBasket, bool hasFact, bool hasHome, List<HomeNote> ring)
        {
            ring.Clear();
            if (!messages)
            {
                return;
            }

            bool early = nextLevel < NewsFromLevel;
            if (early)
            {
                ring.Add(HomeNote.Play);
            }

            if (dailyPending && !early)
            {
                ring.Add(HomeNote.Daily);
            }

            if (levelsToBasket >= 1 && !early)
            {
                ring.Add(HomeNote.Basket);
            }

            if (hasHome)
            {
                ring.Add(HomeNote.Home);
            }

            if (hasFact)
            {
                ring.Add(HomeNote.Fact);
            }

            if (ring.Count == 0)
            {
                ring.Add(HomeNote.Play);
            }
        }

        /// <summary>
        /// Fija a mano lo que TiTino necesita del juego (si no, lo toma del <see cref="GameBootstrap"/> del que cuelga):
        /// <paramref name="clock"/>, la sesión de TiTino (<paramref name="session"/>), el <c>sloth</c> del perfil
        /// (<paramref name="data"/>) y cómo guardarlo (<paramref name="save"/>), <c>settings.sloth_messages</c>
        /// (<paramref name="messages"/>), el nivel de «Jugar» (<paramref name="nextLevel"/>), Reducir movimiento
        /// (<paramref name="reduceMotion"/>), si el inicio es la pantalla de ahora (<paramref name="inLobby"/>) y, para la rotación,
        /// si el Reto de hoy está sin jugar (<paramref name="dailyPending"/>) y los niveles que faltan para la próxima Canasta
        /// (<paramref name="levelsToBasket"/>, −1 sin Canasta).
        /// </summary>
        public void Bind(IClock clock, SlothSession session, SlothData data, Action? save, Func<bool> messages, Func<int> nextLevel,
            Func<bool> reduceMotion, Func<bool> inLobby, Func<bool>? dailyPending = null, Func<int>? levelsToBasket = null,
            Func<bool>? notesClear = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _save = save;
            _messages = messages ?? throw new ArgumentNullException(nameof(messages));
            _nextLevel = nextLevel ?? throw new ArgumentNullException(nameof(nextLevel));
            _reduceMotion = reduceMotion ?? throw new ArgumentNullException(nameof(reduceMotion));
            _inLobby = inLobby ?? throw new ArgumentNullException(nameof(inLobby));
            _dailyPending = dailyPending;
            _notesClear = notesClear;
            _levelsToBasket = levelsToBasket;
            _bound = true;
        }

        /// <summary>
        /// Coloca a TiTino en su caja (esquina en <paramref name="x"/>, <paramref name="yTop"/>; <paramref name="boxW"/> ×
        /// <paramref name="boxH"/> dp, con la línea de la rama a <paramref name="branchInBox"/> dp de arriba y el tamaño S
        /// <paramref name="rigDp"/>), su área táctil encima y el bocadillo a su derecha, sin pasar de <paramref name="maxRight"/> ni
        /// de <paramref name="maxBottom"/>.
        /// </summary>
        public void LayoutScene(float x, float yTop, float boxW, float boxH, float branchInBox, float rigDp, float maxRight, float maxBottom)
        {
            _x = x;
            _y = yTop;
            _boxW = boxW;
            _boxH = boxH;
            _branchInBox = branchInBox;
            _maxRight = maxRight;
            _maxBottom = maxBottom;
            if (!Mathf.Approximately(rigDp, _rigDp))
            {
                _rigDp = rigDp;
                if (_rig != null)
                {
                    _rig.SetArt(_rig.Art, _rigDp);
                    if (_shown)
                    {
                        ApplyBasePose();
                    }
                }
            }

            UiBuilder.Place(TapRect, x, yTop, Mathf.Max(HomeWindow.MinTapDp, boxW), Mathf.Max(HomeWindow.MinTapDp, boxH));
            if (BubbleShown && !PlaceBubble())
            {
                HideNote(); // ya no cabe (otra pantalla, otra escala): fuera, y la rotación sigue
                _gapMs = 0f;
            }

            if (_rig != null && _shown)
            {
                PlaceRig(Pose);
            }

            PlaceZzz();
        }

        /// <summary>
        /// TiTino se ve (<paramref name="on"/>) o se oculta (Sin canoas, una pantalla de pago, sin parada o sin arte): con él, el
        /// rig, el área táctil y el bocadillo, que se pierde si estaba a la vista.
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

            _awakeMs = -1f;
            if (!on)
            {
                HideNote();
                _rig?.SetVisible(false);
                UpdateZzz(0f);
                ReportAsleep("hidden");
                return;
            }

            if (!Enabled)
            {
                // ui_sloth_enabled apagado: TiTino quieto (el retrato), sin rig ni toque.
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
            UpdateZzz(0f);
            ReportAsleep("shown");
        }

        /// <summary>Empieza una visita al inicio: el saludo del día y la rotación se deciden en cuanto TiTino se vea.</summary>
        public void BeginVisit()
        {
            _visitPending = true;
            _ringBuilt = false;
            _gapMs = -1f;
        }

        /// <summary>Acaba la visita al inicio (se va a un nivel): el bocadillo se pierde.</summary>
        public void EndVisit()
        {
            _visitPending = false;
            _ringBuilt = false;
            _gapMs = -1f;
            HideNote();
            if (_awakeMs >= 0f)
            {
                FallAsleep(); // la próxima visita lo encuentra dormido
            }
        }

        /// <summary>
        /// Toque en TiTino (d.7): <c>sloth_look</c> y, con sus mensajes encendidos, el mensaje siguiente al momento (el Dato del día
        /// si aún no salió en esta sesión). Dormido (de noche), se despierta con <c>sloth_wake</c> y dice ese mensaje; vuelve a
        /// dormirse cuando el mensaje se va (añadido de D-050 del 2026-10-26). false si TiTino no se ve o está apagado.
        /// </summary>
        public bool Tap()
        {
            if (!_shown || !Enabled)
            {
                return false;
            }

            Taps++;
            if (Asleep)
            {
                _awakeMs = 0f;
                ApplyBasePose(); // sin rig, el retrato abre los ojos
                PlayGesture(SlothPoseId.Wake, SlothRig.WakeMs);
            }
            else
            {
                PlayGesture(SlothPoseId.Look, SlothRig.LookMs);
            }

            TapNote();
            UpdateZzz(0f);
            ReportAsleep("tap");
            return true;
        }

        // El mensaje del toque, con los mensajes de TiTino encendidos: el Dato del día si aún no salió en esta sesión y, si no, el
        // siguiente de la rotación.
        private void TapNote()
        {
            if (!Resolve() || !_messages!())
            {
                return;
            }

            EnsureRing();
            if (!FactShownThisSession)
            {
                int fact = _ring.IndexOf(HomeNote.Fact);
                if (fact >= 0 && TryShowRing(fact))
                {
                    return;
                }
            }

            ShowNextInRing();
        }

        // Vuelve a dormirse tras despertarlo de noche: sloth_sleep y el «Zzz».
        private void FallAsleep()
        {
            _awakeMs = -1f;
            if (_shown && Enabled && _gestureMs < 0f)
            {
                ApplyBasePose();
            }

            UpdateZzz(0f);
            ReportAsleep("sleep");
        }

        // Avisa del cambio de Asleep (nunca por fotograma: solo cuando cambia).
        private void ReportAsleep(string cause)
        {
            bool asleep = Asleep;
            if (asleep == _asleepReported)
            {
                return;
            }

            _asleepReported = asleep;
            AsleepChanged?.Invoke(asleep, cause);
        }

        // El «Zzz»: solo dormido, con el rig (sin él no se sabe dónde está la cabeza) y sin bocadillo.
        private void UpdateZzz(float dtMs)
        {
            bool reduce = ReduceMotionNow;
            bool on = Asleep && _rig != null && !BubbleShown;
            if (on != _zzz.Shown)
            {
                if (on)
                {
                    PlaceZzz();
                }

                _zzz.SetShown(on, reduce);
            }
            else if (on)
            {
                _zzz.Tick(dtMs, reduce);
            }
        }

        // El arranque del «Zzz», junto a la cabeza de sloth_sleep (SlothRig.SleepHeadS, sobre la línea de la rama del decorado).
        private void PlaceZzz()
        {
            float headX = _x + (_boxW * 0.5f) + (SlothRig.SleepHeadS.x * _rigDp);
            float headY = _y + _branchInBox - (SlothRig.SleepHeadS.y * _rigDp);
            _zzz.Place(headX + ZzzFromHeadXDp, headY + ZzzFromHeadYDp);
        }

        /// <summary>
        /// Avanza <paramref name="dtMs"/> ms: decide el saludo de la visita en cuanto TiTino se ve, mira la hora para la pose, acaba
        /// los gestos, lleva el bocadillo y la rotación. Sin asignaciones (salvo el saludo y los textos de la rotación, una vez por
        /// visita, y al colocar cada mensaje).
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
                EnsureRing();
                if (!BubbleShown)
                {
                    _gapMs = RotationGapMs - FirstNoteDelayMs;
                }
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
                            if (pose != SlothPoseId.Sleep)
                            {
                                _awakeMs = -1f; // amaneció mientras estaba despierto: ya es de día
                            }

                            ApplyBasePose();
                            ReportAsleep("hour");
                        }
                    }
                }
            }

            TickNote(dtMs);
            TickRotation(dtMs);

            // Despierto de noche tras un toque: se vuelve a dormir cuando su mensaje se va (y el gesto acaba), como mínimo a los
            // NightAwakeMs.
            if (_awakeMs >= 0f)
            {
                _awakeMs += dtMs;
                if (_awakeMs >= NightAwakeMs && !BubbleShown && _gestureMs < 0f)
                {
                    FallAsleep();
                }
            }

            UpdateZzz(dtMs);
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

            // De noche duerme callado (añadido de D-050 del 2026-10-26): ni gesto ni frase, y el saludo no se gasta (la visita sí
            // cuenta para last_seen_date) [P].
            bool night = BasePoseFor(hour) == SlothPoseId.Sleep;
            bool speaks = !night && line.Speaks && UiText.TrySlothLine(in line, out text);
            SlothLine shown = speaks
                ? line
                : night
                    ? new SlothLine(SlothScreen.Home, SlothGesture.None, line.Moment, SlothData.NoLine, null, 0, false, line.Deferred)
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
                ShowNote(text, HomeNote.Greeting);
            }
        }

        // Los mensajes que rotan esta visita, con sus textos (una vez por visita: asigna cadenas).
        private void EnsureRing()
        {
            if (_ringBuilt || !Resolve())
            {
                return;
            }

            _ringBuilt = true;
            _ringMessages = _messages!();
            string today = _clock!.LocalDateKey();
            SlothLines.TryDayNumber(today, out int day);
            string fact = FactFor(day, out string? factKey);
            int level = _nextLevel!();
            bool daily = _dailyPending != null && _dailyPending();
            int basket = _levelsToBasket != null ? _levelsToBasket() : -1;
            string home = _home.DistanceText;
            RotationFor(_messages!(), level, daily, basket, fact.Length > 0, home.Length > 0, _ring);
            for (int i = 0; i < _ring.Count; i++)
            {
                switch (_ring[i])
                {
                    case HomeNote.Daily:
                        _ringText[i] = UiText.HomeSlothDaily;
                        break;
                    case HomeNote.Basket:
                        _ringText[i] = UiText.HomeSlothBasket(basket);
                        break;
                    case HomeNote.Fact:
                        _ringText[i] = fact;
                        break;
                    case HomeNote.Home:
                        _ringText[i] = home;
                        break;
                    default:
                        _ringText[i] = UiText.HomeSlothPlay;
                        break;
                }
            }

            _pendingFactKey = factKey;
            _ringNext = 0;
        }

        private string? _pendingFactKey;

        // La rotación: tras cada mensaje (o sin él), RotationGapMs de pausa y el siguiente que quepa; solo con el inicio delante.
        /// <summary>Solo tests: estado de la rotación (pausa en ms, −1 parada; anillo; visita pendiente).</summary>
        internal string RotationStateForTests => "gap=" + _gapMs + " ring=" + _ring.Count + " built=" + _ringBuilt + " pending=" + _visitPending + " inLobby=" + (_inLobby == null ? "-" : _inLobby().ToString()) + " clear=" + (_notesClear == null ? "-" : _notesClear().ToString());

        private void TickRotation(float dtMs)
        {
            if (!_ringBuilt || _messages == null)
            {
                return;
            }

            // «Mensajes de TiTino» se cambia en Ajustes, encima del inicio y sin cerrar la visita: apagado, el bocadillo se va y la
            // rotación para; encendido otra vez, la rotación se rehace (una vez, al cambiar) y sigue tras una pausa.
            bool messages = _messages();
            if (messages != _ringMessages)
            {
                _ringBuilt = false;
                EnsureRing();
                if (!messages)
                {
                    HideNote();
                }

                _gapMs = 0f;
                return;
            }

            // De noche duerme callado (añadido de D-050 del 2026-10-26): la rotación espera a que amanezca; solo habla al tocarlo.
            if (_gapMs < 0f || BubbleShown || _ring.Count == 0 || _basePose == SlothPoseId.Sleep)
            {
                return;
            }

            // La pausa solo corre con el inicio delante y despejado (sin Ajustes, el Mercado, el Reto ni una oferta encima): los
            // mensajes no se gastan sin verse.
            if ((_inLobby != null && !_inLobby()) || (_notesClear != null && !_notesClear()))
            {
                return;
            }

            _gapMs += dtMs;
            if (_gapMs >= RotationGapMs)
            {
                ShowNextInRing();
            }
        }

        private void ShowNextInRing()
        {
            int count = _ring.Count;
            for (int tries = 0; tries < count; tries++)
            {
                int i = _ringNext % count;
                _ringNext = (_ringNext + 1) % count;
                if (TryShowRing(i))
                {
                    return;
                }
            }

            _gapMs = 0f; // nada cabe ahora: se vuelve a intentar tras otra pausa
        }

        private bool TryShowRing(int index)
        {
            if (index < 0 || index >= _ring.Count || string.IsNullOrEmpty(_ringText[index]))
            {
                return false;
            }

            HomeNote kind = _ring[index];
            if (kind == HomeNote.Fact && FactShownThisSession)
            {
                return false; // D-016, punto 5, y 07 §6.5: como mucho un Dato del perezoso por sesión
            }

            if (!ShowNote(_ringText[index], kind))
            {
                return false;
            }

            if (kind == HomeNote.Fact)
            {
                FactShownThisSession = true;
                FactKey = _pendingFactKey;
            }

            _ringNext = (index + 1) % _ring.Count;
            return true;
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
            // Solo con el inicio delante: con «El río sigue creciendo» (ContentEnd), el velo y el modal tapan a TiTino y el saludo
            // del día (como mucho 1; D-016, punto 5) se gastaría sin verse. Se decide al cerrarlo, en la misma visita.
            Bind(LaunchOptions.ClockOverride ?? SystemClock.Instance, boot.View!.SlothSession, profile.Sloth, () => SaveProfile(boot),
                () => profile.Settings.SlothMessages, () => app.PlayLevel, () => boot.Flow != null && boot.Flow.ReduceMotion,
                () => app.Screen == AppScreen.Lobby, () => boot.HomeDailyPending, () => boot.HomeLevelsToBasket, () => boot.HomeNotesClear);
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

        // El rig, la primera vez que TiTino se ve; sin arte (release sin el PNG final), el retrato hace de TiTino.
        private void EnsureRig()
        {
            if (_rig != null || _rigMissing)
            {
                return;
            }

            SlothArt? art = _art.SlothRigArt();
            _rig = art != null ? SlothRig.Create(_hole, art, _rigDp) : null;
            _rigMissing = _rig == null;
            if (_rig != null)
            {
                // La rama de TiTino la pinta el inicio (ProceduralArt.HomeBranch): la del rig no se pinta nunca.
                _rig.BranchImage.enabled = false;
            }
        }

        /// <summary>La rama del rig se pinta (nunca en el inicio: la rama es la del decorado, D-050).</summary>
        public bool RigBranchShown => _rig != null && _rig.BranchImage.enabled;

        private bool ReduceMotionNow => _reduceMotion != null && _reduceMotion();

        // La pose de la hora (o sloth_idle, despierto de noche): el rig (con el retrato oculto) o, sin rig, el retrato con los ojos
        // cerrados mientras duerme.
        private void ApplyBasePose()
        {
            SlothPoseId rest = RestPose;
            if (_rig == null)
            {
                _home.PortraitImage.enabled = true;
                _home.SetAsleep(rest == SlothPoseId.Sleep);
                return;
            }

            _home.PortraitImage.enabled = false;
            PlaceRig(rest);
            _rig.Play(rest, ReduceMotionNow);
            _rig.BranchImage.enabled = false;
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
            _rig.BranchImage.enabled = false;
        }

        // En la línea de la rama del decorado, en el centro de la caja: colgado, debajo de ella; tumbado o en ovillo, encima. Durante
        // sloth_wave, el rig baja un poco con la garra (WaveDrop).
        private void PlaceRig(SlothPoseId pose)
        {
            float y = _branchInBox;
            if (pose == SlothPoseId.Wave && _gestureMs >= 0f && !_rig!.ReduceMotion)
            {
                y += WaveDrop(_gestureMs);
            }

            _rig!.Place(_boxW * 0.5f, y);
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

        // Un mensaje en el bocadillo, con su tiempo de lectura (D-011). false si no cabe (y no se enseña).
        private bool ShowNote(string text, HomeNote kind)
        {
            HideNote();
            _label.text = text;
            if (!PlaceBubble())
            {
                return false;
            }

            _noteKind = kind;
            _noteMs = 0f;
            _gapMs = -1f;
            _noteOutMs = NoticeTiming.ReadMs(text);
            _noteReduceMotion = ReduceMotionNow;
            _bubbleGroup.alpha = _noteReduceMotion ? 1f : 0f;
            _bubble.gameObject.SetActive(true);
            return true;
        }

        private void HideNote()
        {
            if (_noteMs < 0f && !_bubble.gameObject.activeSelf)
            {
                return;
            }

            _noteMs = -1f;
            _noteKind = HomeNote.None;
            _bubble.gameObject.SetActive(false);
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
                _gapMs = 0f; // empieza la pausa hasta el siguiente de la rotación
                return;
            }

            float alpha = 1f;
            if (!_noteReduceMotion)
            {
                float fadeIn = Mathf.Clamp01(_noteMs / BubbleFadeMs);
                float fadeOut = 1f - Mathf.Clamp01((_noteMs - _noteOutMs) / BubbleFadeMs);
                alpha = Mathf.Min(fadeIn, fadeOut);
            }

            if (!Mathf.Approximately(_bubbleGroup.alpha, alpha))
            {
                _bubbleGroup.alpha = alpha;
            }
        }

        // Ancho y alto del texto de label en como mucho inner dp: ajustado al texto si cabe en una línea y, si no, el ancho más
        // estrecho con las mismas líneas (quedan parejas, como el bocadillo de la victoria). El salto lo decide el ancho, nunca un
        // salto añadido. Solo al colocar, nunca por fotograma.
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

        // A la derecha de TiTino, con el pico a la izquierda hacia su cabeza (y su sombra de 1 dp): el texto a 14 sp con la escala de
        // texto, ajustado (FitText), que crece hacia abajo (02 §12) hasta el tope que dio el inicio. false si no cabe.
        private bool PlaceBubble()
        {
            float left = _x + _boxW + BubbleGapDp + TailOutDp;
            float top = _y + Mathf.Max(4f, _branchInBox - 10f);
            float maxWidth = Mathf.Min(BubbleMaxWidthDp, _maxRight - BubbleRimDp - left);
            if (maxWidth < (2f * BubblePadXDp) + 40f)
            {
                return false;
            }

            FitText(_label, maxWidth - (2f * BubblePadXDp), out float textWidth, out float textHeight);
            float width = Mathf.Ceil(textWidth + (2f * BubblePadXDp));
            float height = Mathf.Ceil(textHeight + (2f * BubblePadYDp));
            width = Mathf.Max(width, 56f);
            if (top + height + BubbleRimDp > _maxBottom)
            {
                return false;
            }

            UiBuilder.Place(_bubble, left, top, width, height);
            float tailY = Mathf.Min(TailYDp, height * 0.5f);
            PlaceTail(_tailShadow.rectTransform, tailY, TailDp + (2f * BubbleRimDp) + (2f * TailShadowDp));
            PlaceTail(_tailRim.rectTransform, tailY, TailDp + (2f * BubbleRimDp));
            PlaceTail(_tailPlate.rectTransform, tailY, TailDp);
            return true;
        }

        // El pico en el canto izquierdo del bocadillo, a y dp de arriba, girado 45°.
        private static void PlaceTail(RectTransform rt, float y, float size)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(0f, -y);
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
