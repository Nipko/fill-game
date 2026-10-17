#nullable enable
using System;
using AF.GameFlow;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Lo que pasó en el arranque (tarea TI6d; sonda <c>AF_STATE boot</c>, <see cref="DevProbe.Boot"/>): para medir en el
    /// teléfono el primer toque en el nivel 1 (&lt; 20 s, 09 §6) y si cabe el segundo de <c>sloth_wake</c> (06 §2.2).
    /// </summary>
    public sealed class BootReport
    {
        /// <summary>Primera apertura: no había perfil (<c>first_open</c>).</summary>
        public bool FirstOpen { get; internal set; }

        /// <summary>Pose de TiTino en la pantalla de arranque (<c>sloth_wake</c> o <c>sloth_idle</c>); null si no salió.</summary>
        public string? SlothPose { get; internal set; }

        /// <summary>Desde que se creó la pantalla hasta que terminó la carga (ms; <c>load_ms</c>).</summary>
        public long LoadMs { get; internal set; }

        /// <summary>Tiempo a la vista de la pantalla de arranque, de su creación a su corte (ms; <c>splash_ms</c>).</summary>
        public long SplashMs { get; internal set; }

        /// <summary>Lo que se vio del gesto de TiTino (reloj de su pose al cortar, ms; <c>sloth_ms</c>; 0 sin TiTino).</summary>
        public long SlothMs { get; internal set; }

        /// <summary>Espera añadida tras la carga (ms; <c>hold_ms</c>; 0 salvo en la primera apertura con TiTino).</summary>
        public long HoldMs { get; internal set; }

        /// <summary>
        /// El segundo de <c>sloth_wake</c> cupo en el objetivo de 20 s (<c>wake_fits</c>; <see cref="BootSplashTiming.Fits"/>):
        /// false si hacía falta esperar y no cabía (se anota para el dueño). True si no hacía falta esperar.
        /// </summary>
        public bool WakeFits { get; internal set; } = true;

        /// <summary>
        /// Desde el arranque hasta el corte de la pantalla (ms; <c>splash_end_ms</c>): en el teléfono, desde que arranca Unity
        /// (<see cref="Time.realtimeSinceStartupAsDouble"/>), sin el lanzamiento del proceso ni el de la Activity, que van antes
        /// (para el arranque en frío de 09 §6.1 se suma el <c>TotalTime</c> de <c>adb shell am start -W</c>); en el Editor, desde el
        /// Awake de la escena.
        /// </summary>
        public long SplashEndMs { get; internal set; }

        /// <summary>
        /// Desde el arranque hasta el primer toque del jugador en el juego (ms; <c>first_touch_ms</c>; el mismo origen que
        /// <see cref="SplashEndMs"/>: desde que arranca Unity), o −1 si aún no ha tocado. Cuenta el primer toque en una cuia, en el
        /// fondo del nivel o durante la intro, o «Jugar» en el inicio.
        /// </summary>
        public long FirstTouchMs { get; internal set; } = -1;

        /// <summary>Pantalla del primer toque (<c>level</c> o <c>lobby</c>; null si aún no ha tocado).</summary>
        public string? FirstTouchScreen { get; internal set; }

        /// <summary>Nivel del primer toque (el de la pantalla de nivel, o el del botón Jugar; 0 si aún no ha tocado).</summary>
        public int FirstTouchLevel { get; internal set; }
    }

    /// <summary>
    /// Pantalla de arranque (06 §2.2: «Splash de 1 pantalla (río + logotipo) con TiTino colgado de su embaúba»; D-016, T2-A;
    /// propuesta de TiTino d.3 y paso 6d; tarea TI6d), construida por código en su propio lienzo, por encima de todo, y
    /// <b>sin texto</b> (06 §2.1, regla 3: ni un componente de texto).
    /// <list type="bullet">
    /// <item>El río al alba (cielo, dos líneas de selva y el agua blanca del estuario, con el reflejo de la selva, la orilla, el
    /// reflejo del sol y sus brillos) y, arriba, el logotipo provisional: una gota de tinte de açaí que cae en una cuia (07 §10:
    /// «una gota o pincelada de tinte»), sin letras. Con el PNG de la ranura <see cref="LogoSlot"/> (el logotipo del artista, que
    /// trae su nombre como dibujo), ese PNG sustituye a la gota y a la cuia.</item>
    /// <item>TiTino (<see cref="SlothRig"/>; propuesta c.4: 140-180 dp en el arranque) colgado de su rama, que entra desde el
    /// borde derecho bajo el logotipo: <c>sloth_wake</c> en la primera apertura y <c>sloth_idle</c> en las demás
    /// (<see cref="ShowSloth"/>). Su tamaño sale del aire que queda entre la rama y la copa de la selva
    /// (<see cref="SlothFitDp"/>: hasta <see cref="SlothSizeDp"/>), para que nunca se monte sobre la selva ni toque el río. Sin el
    /// arte final, solo en el Editor y en desarrollo (<see cref="MapArtRule"/>), y nunca con <c>ui_sloth_enabled</c> apagado
    /// (<see cref="SlothEnabled"/>): entonces la pantalla sale sin TiTino y el logotipo baja al centro óptico del cielo, con un
    /// halo mayor.</item>
    /// <item>En las pantallas bajas (alto &lt; <see cref="TallAspect"/> × ancho, como 16:9) la composición se aprieta: el
    /// logotipo sube y el horizonte baja.</item>
    /// <item>Cuánto dura lo decide <see cref="BootSplashTiming"/> (GameBootstrap): se corta al terminar la carga, salvo el
    /// segundo de <c>sloth_wake</c> de la primera apertura si cabe en el objetivo de 20 s.</item>
    /// </list>
    /// El fondo traga los toques mientras se ve (aún no hay nada que tocar). Las texturas son suyas y se destruyen con
    /// <see cref="Dispose"/>; las piezas de TiTino son de <see cref="ProceduralArt"/> y no se tocan. Nada asigna memoria por
    /// fotograma.
    /// </summary>
    public sealed class BootSplash : IDisposable
    {
        /// <summary>Orden del lienzo: por encima de todos los demás (Ajustes 40, tienda 50, anuncio 60, aviso del perfil 70).</summary>
        public const int SortingOrder = 90;

        /// <summary>Tamaño S máximo de TiTino en el arranque (propuesta de TiTino c.4: 140-180 dp) [P].</summary>
        public const float SlothSizeDp = 160f;

        /// <summary>Tamaño S mínimo de TiTino, aunque no quepa entero sobre la selva (pantallas muy bajas) [P].</summary>
        public const float SlothMinDp = 120f;

        /// <summary>
        /// Caída del cuerpo bajo la rama (S): el borde inferior de la caja del torso colgado (0,60 + 0,56 × 1,02 / 2 ≈ 0,886 S en
        /// lo más hondo de la respiración, <c>SlothRig.Hang</c>), con un poco de margen [P].
        /// </summary>
        public const float SlothDropS = 0.90f;

        /// <summary>Aire mínimo entre el fondo de TiTino y la copa de la selva cercana (dp) [P].</summary>
        public const float SlothClearDp = 16f;

        /// <summary>Proporción alto/ancho por debajo de la cual la pantalla es baja (16:9 = 1,78; 20:9 = 2,22) [P].</summary>
        public const float TallAspect = 2.0f;

        /// <summary>Copa de la selva lejana por encima del horizonte (dp).</summary>
        public const float FarCanopyDp = 58f;

        /// <summary>Copa de la selva cercana por encima del horizonte (dp).</summary>
        public const float NearCanopyDp = 36f;

        /// <summary>Ranura del logotipo final (sin el nombre de la marca en el identificador; CLAUDE.md, glosario §11) [P].</summary>
        public const string LogoSlot = "ui_logo";

        // Proporciones de la composición (del alto del lienzo) [P]: el centro del logotipo con TiTino debajo y sin él (el centro
        // óptico del cielo), y la línea del horizonte; en las pantallas bajas, *Short.
        private const float LogoCenterY = 0.25f;
        private const float LogoCenterYShort = 0.22f;
        private const float LogoAloneY = 0.36f;
        private const float LogoAloneYShort = 0.34f;
        private const float HorizonY = 0.64f;
        private const float HorizonYShort = 0.68f;

        // Medidas en dp [P]: la gota, la cuia, el halo (con TiTino y sin él) y el logotipo final.
        private const float DropDp = 112f;
        private const float BowlWidthDp = 150f;
        private const float BowlHeightDp = 75f;
        private const float GlowDp = 236f;
        private const float GlowAloneDp = 300f;
        private const float LogoPngWidthDp = 280f;

        // La rama de TiTino, por debajo del centro del logotipo.
        private const float BranchBelowLogoDp = 116f;

        // El agua [P]: la orilla (alto en dp), el reflejo de la selva (alto en dp) y el del sol (ancho y alto en dp).
        private const float ShoreDp = 3f;
        private const float TreeReflectionDp = 70f;
        private const float SunPathWidthDp = 40f;
        private const float SunPathHeightDp = 160f;

        // Los tres brillos del agua: bajo el horizonte (dp), centro (fracción del ancho), ancho y alto (dp) [P].
        private const float GlintHeightDp = 5f;
        private static readonly float[] GlintY = { 34f, 82f, 146f };
        private static readonly float[] GlintX = { 0.18f, 0.52f, 0.30f };
        private static readonly float[] GlintW = { 96f, 150f, 70f };

        private static readonly Color DropLight = Palette.Hex(0x9B5CC4);
        private static readonly Color DropDeep = Palette.Hex(0x4A1C68);
        private static readonly Color DropInk = Palette.Hex(0x2E0F44);
        private static readonly Color BowlLight = Palette.Hex(0xD8B07A);
        private static readonly Color BowlDeep = Palette.Hex(0x8A5A32);
        private static readonly Color BowlRim = Palette.Hex(0xEFD2A0);

        private readonly OverlayCanvas _canvas;
        private readonly Texture2D?[] _textures = new Texture2D?[8];
        private readonly Sprite?[] _sprites = new Sprite?[8];
        private readonly Image _sky;
        private readonly Image _farTrees;
        private readonly Image _nearTrees;
        private readonly Image _river;
        private readonly Image _treeReflection;
        private readonly Image _sunPath;
        private readonly Image _shore;
        private readonly Image[] _glints = new Image[3];
        private readonly Image _glow;
        private readonly Image _drop;
        private readonly Image _bowl;
        private readonly Image? _logoPng;
        private readonly double _createdAt;
        private SlothRig? _sloth;
        private bool _withSloth;
        private bool _disposed;

        /// <summary>
        /// Interruptor de los tests (null = lo de siempre: la pantalla sale en cada arranque en frío). Los tests PlayMode que no
        /// tratan de ella la apagan (<c>PlayModeHelpers</c>, al cargar la escena) y los que la prueban la encienden y la vuelven
        /// a null en su TearDown.
        /// </summary>
        public static bool? EnabledOverride { get; set; }

        /// <summary>La pantalla de arranque sale en los arranques en frío (<see cref="EnabledOverride"/> o, sin él, siempre).</summary>
        public static bool Enabled => EnabledOverride ?? true;

        /// <summary>Interruptor de los tests para TiTino en el arranque (null = <see cref="GameConfig.UiSlothEnabled"/>).</summary>
        public static bool? SlothEnabledOverride { get; set; }

        /// <summary>
        /// TiTino sale en la pantalla de arranque: <c>ui_sloth_enabled</c> (08 §9; D-016: el interruptor de cada gancho, como el de
        /// la victoria), salvo que un test lo fuerce (<see cref="SlothEnabledOverride"/>).
        /// </summary>
        public static bool SlothEnabled => SlothEnabledOverride ?? GameConfig.UiSlothEnabled;

        /// <summary>
        /// Se espera a TiTino en la pantalla (<see cref="SlothEnabled"/> y su arte: el provisional, en el Editor y en desarrollo, o
        /// los dos PNG finales, las piezas y la rama; la regla de <see cref="ProceduralArt.SlothRigArt()"/>). Con esto la pantalla
        /// nace ya compuesta con o sin él y el logotipo no salta cuando TiTino entra.
        /// </summary>
        public static bool SlothExpected =>
            SlothEnabled
            && (MapArtRule.ShowProvisional
                || (ArtSlots.Find(ProceduralArt.SlothPartsSlot) != null && ArtSlots.Find(ProceduralArt.SlothBranchSlot) != null));

        /// <summary>
        /// Solo para los tests: ms que se suman al reloj del arranque (<c>GameBootstrap.SinceStartupMs</c>), para recorrer el caso
        /// «el segundo de <c>sloth_wake</c> no cabe en los 20 s». Siempre 0 fuera de ellos (y lo vuelven a 0 en su TearDown).
        /// </summary>
        public static long TestStartupOffsetMs { get; set; }

        /// <summary>
        /// Crea la pantalla, ya a la vista, en un lienzo propio dentro de <paramref name="host"/>, compuesta para TiTino
        /// (<paramref name="withSloth"/>: el logotipo arriba y su rama debajo) o sin él (el logotipo en el centro óptico del cielo).
        /// Si <see cref="ShowSloth"/> no puede ponerlo, se recompone sin él.
        /// </summary>
        public BootSplash(Transform host, bool withSloth = true)
        {
            _createdAt = Time.realtimeSinceStartupAsDouble;
            _withSloth = withSloth;
            _canvas = new OverlayCanvas("BootSplashCanvas", host, SortingOrder);
            RectTransform root = _canvas.Root;

            // El río arranca más oscuro que su reflejo puro para no confundirse con el cielo del alba justo encima.
            Color riverTop = Color.Lerp(Palette.RiverReflect, Palette.RiverBase, 0.35f);
            Sprite sky = Own(0, VerticalGradient(128, Palette.SkyHorizon, Palette.SkyMid, Palette.SkyTop));
            Sprite trees = Own(1, Treeline(256, 128));
            Sprite river = Own(2, VerticalGradient(64, Palette.RiverDeep, Palette.RiverBase, riverTop));
            Sprite glint = Own(3, SoftCapsule(64, 8));
            Sprite glow = Own(4, SoftDisc(128));
            Sprite reflection = Own(7, TreelineReflection(256, 128, FarCanopyDp + 6f, TreeReflectionDp));

            UiBuilder.Stretch(UiBuilder.Image("TouchBlocker", root, null, Color.clear, raycast: true).rectTransform); // traga los toques: aún no hay nada que tocar
            _sky = UiBuilder.Image("Sky", root, sky, Color.white);
            _farTrees = UiBuilder.Image("FarTrees", root, trees, Palette.FarShore);
            _nearTrees = UiBuilder.Image("NearTrees", root, trees, Palette.JungleMid);
            _nearTrees.rectTransform.localScale = new Vector3(-1f, 1f, 1f); // la misma silueta al revés: otra línea de copas
            _river = UiBuilder.Image("River", root, river, Color.white);

            // La selva reflejada en el agua, oscura y apagada hacia abajo; el reflejo del sol bajo el halo del logotipo; los
            // brillos; y la línea clara de la orilla. Juntos dicen «agua» (D-003: luz de juego), con la paleta del río del juego.
            Color mirrored = Color.Lerp(Palette.JungleMid, Palette.RiverDeep, 0.45f);
            _treeReflection = UiBuilder.Image("TreeReflection", root, reflection, new Color(mirrored.r, mirrored.g, mirrored.b, 0.45f));
            _sunPath = UiBuilder.Image("SunPath", root, glow, new Color(Palette.Sun.r, Palette.Sun.g, Palette.Sun.b, 0.25f));
            for (int i = 0; i < _glints.Length; i++)
            {
                _glints[i] = UiBuilder.Image("Glint" + i, root, glint, new Color(Palette.WaterLine.r, Palette.WaterLine.g, Palette.WaterLine.b, 0.75f));
            }

            _shore = UiBuilder.Image("Shore", root, null, new Color(Palette.WaterLine.r, Palette.WaterLine.g, Palette.WaterLine.b, 0.8f));

            _glow = UiBuilder.Image("Glow", root, glow, new Color(Palette.Sun.r, Palette.Sun.g, Palette.Sun.b, 0.6f));
            Sprite? logoPng = ArtSlots.Find(LogoSlot);
            if (logoPng != null)
            {
                _logoPng = UiBuilder.Image("Logo", root, logoPng, Color.white);
                _logoPng.preserveAspect = true;
            }

            _bowl = UiBuilder.Image("LogoBowl", root, Own(5, Bowl(256, 128)), Color.white);
            _drop = UiBuilder.Image("LogoDrop", root, Own(6, Drop(256)), Color.white);
            _bowl.enabled = logoPng == null;
            _drop.enabled = logoPng == null;
            Layout();
        }

        /// <summary>Lienzo propio (capturas de prueba).</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>La pantalla está a la vista.</summary>
        public bool Visible => !_disposed && _canvas.Canvas.gameObject.activeSelf;

        /// <summary>TiTino en la pantalla (null hasta <see cref="ShowSloth"/>, o sin su arte).</summary>
        public SlothRig? Sloth => _sloth;

        /// <summary>TiTino está a la vista.</summary>
        public bool SlothVisible => _sloth != null && _sloth.Visible;

        /// <summary>Lo que lleva su pose a la vista (reloj de la pose, ms; 0 sin TiTino).</summary>
        public float SlothClockMs => SlothVisible ? _sloth!.ClockMs : 0f;

        /// <summary>Desde que se creó la pantalla (ms de tiempo real).</summary>
        public long AgeMs => (long)((Time.realtimeSinceStartupAsDouble - _createdAt) * 1000.0);

        /// <summary>La composición deja sitio a TiTino (el logotipo arriba); false: el logotipo solo, en el centro del cielo.</summary>
        public bool ComposedForSloth => _withSloth;

        /// <summary>La pantalla es baja (alto &lt; <see cref="TallAspect"/> × ancho, como 16:9).</summary>
        public bool ShortScreen => _canvas.Height < TallAspect * _canvas.Width;

        /// <summary>Línea del horizonte, entre la selva y el río (dp desde arriba).</summary>
        public float HorizonDp => Mathf.Round((ShortScreen ? HorizonYShort : HorizonY) * _canvas.Height);

        /// <summary>Centro del logotipo (dp desde arriba).</summary>
        public float LogoCenterDp =>
            (_withSloth ? (ShortScreen ? LogoCenterYShort : LogoCenterY) : (ShortScreen ? LogoAloneYShort : LogoAloneY)) * _canvas.Height;

        /// <summary>Lado del halo del logotipo (dp): mayor sin TiTino, para llenar el cielo.</summary>
        public float GlowSizeDp => _withSloth ? GlowDp : GlowAloneDp;

        /// <summary>
        /// Tamaño S de TiTino en esta pantalla (dp): el que deja <see cref="SlothClearDp"/> de aire entre su fondo
        /// (<see cref="SlothDropS"/> bajo la rama) y la copa de la selva cercana, entre <see cref="SlothMinDp"/> y
        /// <see cref="SlothSizeDp"/>.
        /// </summary>
        public float SlothFitDp
        {
            get
            {
                float room = HorizonDp - NearCanopyDp - SlothClearDp - BranchLineDp;
                return Mathf.Clamp(room / SlothDropS, SlothMinDp, SlothSizeDp);
            }
        }

        /// <summary>Línea de la rama de TiTino en el lienzo (dp, x desde la izquierda e y desde arriba).</summary>
        public Vector2 BranchAnchor
        {
            get
            {
                float s = _sloth?.Size ?? SlothFitDp;
                return new Vector2(_canvas.Width - (0.75f * s), BranchLineDp);
            }
        }

        /// <summary>Todo lo que se pinta (tests: ningún componente de texto, ningún raycast salvo el fondo).</summary>
        public RectTransform Root => _canvas.Root;

        private float BranchLineDp => LogoCenterDp + BranchBelowLogoDp;

        /// <summary>
        /// Pone a TiTino colgado de su rama con <c>sloth_wake</c> (<paramref name="firstOpen"/>) o <c>sloth_idle</c>, al tamaño que
        /// cabe (<see cref="SlothFitDp"/>). Sin piezas (<paramref name="art"/> null: release sin el arte final,
        /// <see cref="ProceduralArt.SlothRigArt()"/>) no crea nada, recompone la pantalla sin él y devuelve false.
        /// </summary>
        public bool ShowSloth(SlothArt? art, bool firstOpen, bool reduceMotion)
        {
            if (_disposed || _sloth != null)
            {
                return _sloth != null;
            }

            _withSloth = true;
            _sloth = SlothRig.Create(_canvas.Root, art, SlothFitDp);
            if (_sloth == null)
            {
                _withSloth = false;
                Layout();
                return false;
            }

            Layout();
            _sloth.Play(firstOpen ? SlothPoseId.Wake : SlothPoseId.Idle, reduceMotion);
            return true;
        }

        /// <summary>Avanza la pose de TiTino <paramref name="dtMs"/> ms (y rehace la composición si cambió la pantalla).</summary>
        public void Tick(float dtMs)
        {
            if (_disposed)
            {
                return;
            }

            if (_canvas.Measure())
            {
                Layout();
            }

            _sloth?.Tick(dtMs);
        }

        /// <summary>Lleva la pose de TiTino al instante <paramref name="ms"/> (capturas deterministas).</summary>
        public void Seek(float ms)
        {
            _sloth?.Seek(ms);
        }

        /// <summary>Corta la pantalla y libera lo suyo (el lienzo y sus texturas).</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _sloth = null;
            if (_canvas.Canvas != null)
            {
                UnityEngine.Object.Destroy(_canvas.Canvas.gameObject);
            }

            for (int i = 0; i < _textures.Length; i++)
            {
                if (_sprites[i] != null)
                {
                    UnityEngine.Object.Destroy(_sprites[i]);
                    _sprites[i] = null;
                }

                if (_textures[i] != null)
                {
                    UnityEngine.Object.Destroy(_textures[i]);
                    _textures[i] = null;
                }
            }
        }

        // Composición en dp (origen arriba a la izquierda): cielo y río a sangre, la selva sobre el horizonte y su reflejo bajo
        // la orilla, el reflejo del sol bajo el logotipo, el logotipo con su halo y TiTino colgado bajo él, con la rama desde el
        // borde derecho y a su tamaño (SlothFitDp).
        private void Layout()
        {
            float w = _canvas.Width;
            float h = _canvas.Height;
            float horizon = HorizonDp;
            float shoreY = horizon + 6f;
            UiBuilder.Place(_sky.rectTransform, 0f, 0f, w, horizon + 2f);
            UiBuilder.Place(_farTrees.rectTransform, 0f, horizon - FarCanopyDp, w, FarCanopyDp + 6f);
            UiBuilder.Place(_nearTrees.rectTransform, 0f, horizon - NearCanopyDp, w, NearCanopyDp + 8f);
            _nearTrees.rectTransform.pivot = new Vector2(0.5f, 1f);
            _nearTrees.rectTransform.anchoredPosition = new Vector2(w * 0.5f, -(horizon - NearCanopyDp));
            UiBuilder.Place(_river.rectTransform, 0f, shoreY, w, h - shoreY);
            UiBuilder.Place(_treeReflection.rectTransform, 0f, shoreY, w, TreeReflectionDp);
            for (int i = 0; i < _glints.Length; i++)
            {
                UiBuilder.Place(_glints[i].rectTransform, (GlintX[i] * w) - (GlintW[i] * 0.5f) + 40f, horizon + GlintY[i], GlintW[i], GlintHeightDp);
            }

            UiBuilder.Place(_shore.rectTransform, 0f, shoreY, w, ShoreDp);

            float cx = w * 0.5f;
            float cy = LogoCenterDp;
            float glow = GlowSizeDp;
            UiBuilder.Place(_sunPath.rectTransform, cx - (SunPathWidthDp * 0.5f), shoreY, SunPathWidthDp, SunPathHeightDp);
            UiBuilder.Place(_glow.rectTransform, cx - (glow * 0.5f), cy - (glow * 0.5f), glow, glow);
            UiBuilder.Place(_drop.rectTransform, cx - (DropDp * 0.5f), cy - 80f, DropDp, DropDp);
            UiBuilder.Place(_bowl.rectTransform, cx - (BowlWidthDp * 0.5f), cy + 14f, BowlWidthDp, BowlHeightDp);
            if (_logoPng != null)
            {
                UiBuilder.Place(_logoPng.rectTransform, cx - (LogoPngWidthDp * 0.5f), cy - (LogoPngWidthDp * 0.32f), LogoPngWidthDp, LogoPngWidthDp * 0.64f);
            }

            if (_sloth != null)
            {
                float s = SlothFitDp;
                if (Mathf.Abs(_sloth.Size - s) > 0.01f)
                {
                    _sloth.SetArt(_sloth.Art, s); // otra pantalla, otro aire: el mismo gesto a otro tamaño
                    _sloth.Seek(_sloth.ClockMs);
                }

                Vector2 anchor = BranchAnchor;
                _sloth.Place(anchor.x, anchor.y);
            }
        }

        private Sprite Own(int slot, Texture2D tex)
        {
            _textures[slot] = tex;
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = tex.name;
            _sprites[slot] = sprite;
            return sprite;
        }

        private static Texture2D NewTexture(int w, int h, string name)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            return tex;
        }

        // Degradado vertical de abajo (bottom) a arriba (top), pasando por mid en la mitad.
        private static Texture2D VerticalGradient(int h, Color bottom, Color mid, Color top)
        {
            Texture2D tex = NewTexture(1, h, "AF_BootGradient");
            var px = new Color32[h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                px[y] = t < 0.5f ? Color.Lerp(bottom, mid, t * 2f) : Color.Lerp(mid, top, (t - 0.5f) * 2f);
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // Línea de copas (blanca, se tiñe con el color de la Image; TreeTop), con 1 px de suavizado en el borde.
        private static Texture2D Treeline(int w, int h)
        {
            Texture2D tex = NewTexture(w, h, "AF_BootTreeline");
            var px = new Color32[w * h];
            for (int x = 0; x < w; x++)
            {
                float edge = TreeTop(x) * (h - 1);
                for (int y = 0; y < h; y++)
                {
                    float a = Mathf.Clamp01(edge - y + 0.5f);
                    px[(y * w) + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // Alto de la línea de copas en la columna x (fracción del alto de la imagen, desde abajo): copas redondeadas (|sen|) de
        // dos tamaños.
        private static float TreeTop(int x)
        {
            float u = x;
            return Mathf.Clamp01(0.50f + (0.22f * Mathf.Abs(Mathf.Sin(u * 0.045f))) + (0.12f * Mathf.Abs(Mathf.Sin((u * 0.11f) + 0.7f))));
        }

        // La selva lejana reflejada en el agua (blanca, se tiñe con el color de la Image): la línea de copas de Treeline boca abajo
        // desde el borde de arriba (la orilla), a la escala de la selva (silhouetteDp de alto en su imagen, heightDp en la del
        // reflejo), que se apaga hacia abajo como (1 − d)².
        private static Texture2D TreelineReflection(int w, int h, float silhouetteDp, float heightDp)
        {
            Texture2D tex = NewTexture(w, h, "AF_BootTreeReflection");
            var px = new Color32[w * h];
            float scale = silhouetteDp / heightDp;
            for (int x = 0; x < w; x++)
            {
                float edge = TreeTop(x) * scale * (h - 1);
                for (int y = 0; y < h; y++)
                {
                    float d = h - 1 - y; // desde la orilla, hacia abajo
                    float a = Mathf.Clamp01(edge - d + 0.5f);
                    float fade = 1f - (d / (h - 1));
                    px[(y * w) + x] = new Color32(255, 255, 255, (byte)(a * fade * fade * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // Cápsula horizontal blanca de borde suave (los brillos del agua).
        private static Texture2D SoftCapsule(int w, int h)
        {
            Texture2D tex = NewTexture(w, h, "AF_BootGlint");
            var px = new Color32[w * h];
            float r = h * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, w - r);
                    float d = new Vector2(x + 0.5f - cx, y + 0.5f - r).magnitude / r;
                    float a = Mathf.Clamp01(1f - d);
                    px[(y * w) + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // Disco blanco que se apaga hacia el borde (el halo del logotipo).
        private static Texture2D SoftDisc(int size)
        {
            Texture2D tex = NewTexture(size, size, "AF_BootGlow");
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x + 0.5f - r, y + 0.5f - r).magnitude / r;
                    float a = Mathf.Clamp01(1f - d);
                    px[(y * size) + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // La gota de tinte de açaí (u, v en 0..1, v hacia arriba): un círculo abajo y su punta arriba, con un brillo arriba a la
        // izquierda y un contorno oscuro. Suavizado con 2 × 2 muestras por píxel.
        private static Texture2D Drop(int size)
        {
            Texture2D tex = NewTexture(size, size, "AF_BootDrop");
            var px = new Color32[size * size];
            var c = new Vector2(0.5f, 0.38f);
            const float r = 0.30f;
            var tip = new Vector2(0.5f, 0.97f);
            Vector2 axis = (tip - c).normalized;
            float beta = Mathf.Acos(r / (tip - c).magnitude);
            Vector2 left = c + (r * Rotate(axis, beta));
            Vector2 right = c + (r * Rotate(axis, -beta));
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color sum = Color.clear;
                    for (int s = 0; s < 4; s++)
                    {
                        var p = new Vector2((x + 0.25f + (0.5f * (s & 1))) / size, (y + 0.25f + (0.5f * (s >> 1))) / size);

                        // Distancia al borde de la gota (> 0 dentro): la mayor de la del círculo y la del triángulo de la punta.
                        float inCircle = r - (p - c).magnitude;
                        float inTip = InTriangle(p, tip, left, right) ? EdgeDistance(p, tip, left, right) : -1f;
                        float inside = Mathf.Max(inCircle, inTip);
                        if (inside < 0f)
                        {
                            continue;
                        }

                        float shade = Mathf.Clamp01(((p.x - 0.30f) * 0.9f) + ((0.75f - p.y) * 0.6f));
                        Color col = Color.Lerp(DropLight, DropDeep, shade);
                        float shine = Mathf.Clamp01(1f - new Vector2((p.x - 0.38f) / 0.06f, (p.y - 0.47f) / 0.10f).magnitude);
                        col = Color.Lerp(col, Color.white, 0.75f * Mathf.SmoothStep(0f, 1f, shine));
                        if (inside < 0.018f)
                        {
                            col = Color.Lerp(col, DropInk, 0.85f);
                        }

                        sum += new Color(col.r, col.g, col.b, 1f);
                    }

                    px[(y * size) + x] = Premultiplied(sum * 0.25f);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // La cuia: media elipse de madera con su borde claro y la superficie de açaí arriba. Suavizado con 2 × 2 muestras.
        private static Texture2D Bowl(int w, int h)
        {
            Texture2D tex = NewTexture(w, h, "AF_BootBowl");
            var px = new Color32[w * h];
            const float lip = 0.80f;
            Color acai = Palette.Tint(8);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color sum = Color.clear;
                    for (int s = 0; s < 4; s++)
                    {
                        float u = (x + 0.25f + (0.5f * (s & 1))) / w;
                        float v = (y + 0.25f + (0.5f * (s >> 1))) / h;
                        float du = (u - 0.5f) / 0.49f;
                        float body = (du * du) + Sq((v - lip) / 0.78f);
                        float rimOuter = (du * du) + Sq((v - lip) / 0.19f);
                        float surface = Sq((u - 0.5f) / 0.43f) + Sq((v - lip) / 0.13f);
                        Color col;
                        if (surface <= 1f)
                        {
                            col = Color.Lerp(acai, DropLight, Mathf.Clamp01((v - lip + 0.13f) / 0.26f) * 0.6f);
                        }
                        else if (rimOuter <= 1f)
                        {
                            col = BowlRim;
                        }
                        else if (v <= lip && body <= 1f)
                        {
                            float shade = Mathf.Clamp01((Mathf.Abs(u - 0.42f) * 1.4f) + ((lip - v) * 0.5f));
                            col = Color.Lerp(BowlLight, BowlDeep, shade);
                        }
                        else
                        {
                            continue;
                        }

                        sum += new Color(col.r, col.g, col.b, 1f);
                    }

                    px[(y * w) + x] = Premultiplied(sum * 0.25f);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // La media de las muestras viene premultiplicada: se divide el color por la cobertura.
        private static Color32 Premultiplied(Color avg)
        {
            if (avg.a <= 0f)
            {
                return new Color32(0, 0, 0, 0);
            }

            return new Color(avg.r / avg.a, avg.g / avg.a, avg.b / avg.a, avg.a);
        }

        private static float Sq(float v)
        {
            return v * v;
        }

        private static Vector2 Rotate(Vector2 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2((cos * v.x) - (sin * v.y), (sin * v.x) + (cos * v.y));
        }

        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(p, a, b);
            float d2 = Cross(p, b, c);
            float d3 = Cross(p, c, a);
            bool neg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool pos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(neg && pos);
        }

        // Distancia a los dos lados largos del triángulo de la punta (tip-left y tip-right).
        private static float EdgeDistance(Vector2 p, Vector2 tip, Vector2 left, Vector2 right)
        {
            return Mathf.Min(SegmentDistance(p, tip, left), SegmentDistance(p, tip, right));
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + (t * ab))).magnitude;
        }

        private static float Cross(Vector2 p, Vector2 a, Vector2 b)
        {
            return ((p.x - b.x) * (a.y - b.y)) - ((a.x - b.x) * (p.y - b.y));
        }
    }
}
