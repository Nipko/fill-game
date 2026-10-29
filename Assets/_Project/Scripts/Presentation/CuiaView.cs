#nullable enable
using System;
using AF.Audio;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Presentation.Mechanics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Vista de una cuia en uGUI (GDD 07 §4; guía visual v2, 4.1-4.3 y 6): ranura fija con su área táctil (02 §3.3: ancho
    /// de la ranura × alto de la cuia + 32 dp) y sombra de contacto sobre la repisa, y un cuerpo móvil con vidrio trasero y
    /// cavidad opaca, líquido (<see cref="LiquidGraphic"/>, shader <c>AF/UI/Liquid</c>), vidrio frontal, tapa de calabaza,
    /// aro de luz y destello. La vista nunca decide reglas: muestra lo que le dice el modelo o la cola de animación (02 §3.2).
    /// Las dosis ocultas (Aguas turbias) y el Barro los pintan <see cref="MurkyMarks"/> y <see cref="MudMarks"/>, que cuelgan
    /// del cuerpo encima del líquido; cada mecánica, su <see cref="CuiaOverlay"/> (07 §4.3, «Bloqueada por mecánica»). Las
    /// ocultas de la Tinaja no son agua turbia: van en el barro de su vasija (contrato 27), que las tapa.
    /// </summary>
    /// <remarks>
    /// <para>Input (02 §3.1): el touch-down solo avisa (la elevación empieza ya, 0 ms de latencia percibida) y la
    /// decisión se toma en el touch-up: soltar dentro del área táctil es un toque; soltar fuera lo cancela.</para>
    /// <para>La pose del cuerpo y la sombra solo se escriben cuando cambian, para no ensuciar el lienzo cada fotograma con
    /// la pantalla quieta (08 §5.4). Nada cambia en una cuia por la selección de otra (D-002).</para>
    /// <para>Mientras vierte, el cuerpo visual (no la ranura con su área táctil) pasa a la capa de vertido, delante de las
    /// vecinas, del marco del panel y del HUD (D-006, como Magic Sort; <see cref="MoveBodyTo"/>), y vuelve a su ranura al
    /// terminar (<see cref="ReturnBody"/>), en la misma posición de pantalla.</para>
    /// <para>Anclas de los overlays (<see cref="CuiaOverlayHost"/>), de atrás adelante: <c>SlotBack</c> en la ranura, antes
    /// del cuerpo; <c>BodyBack</c> entre el vidrio trasero y el líquido; <c>BodyFront</c> tras las marcas de dosis y antes de
    /// la tapa; <c>SlotFront</c> en la ranura, tras el cuerpo. El overlay de una mecánica se crea la primera vez que
    /// <see cref="ShowContent"/> la ve (también a mitad de nivel: Marea devuelve una Hoja) y recibe <c>Show</c>,
    /// <c>Layout</c>, <c>OnBoardLayout</c>, <c>OnRejected</c>, <c>Tick</c> y <c>SetInFlight</c> de esta vista.</para>
    /// <para>Tablero (contrato congelado desde la ola 3, T07): <see cref="BindBoard"/> rellena en el anfitrión las capas de
    /// tablero y el tablero (<c>BoardAnimator.Bind</c> lo llama en cada vista). Ningún overlay se une antes: si
    /// <see cref="ShowContent"/> o <see cref="UseOverlay"/> lo piden sin tablero, se crea o se une en <see cref="BindBoard"/>.
    /// Los pasos de mecánica de la cola llegan al overlay de la mecánica (<see cref="OverlayOf"/>); el revelado y el lavado
    /// del barro, a <see cref="PlayReveal"/> y <see cref="PlayMudWash"/>, que delegan en las marcas.</para>
    /// </remarks>
    public sealed class CuiaView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>Elevación de la selección: 16 dp (02 §4.1; 07 §4.3).</summary>
        public const float LiftDp = 16f;

        /// <summary>
        /// Elevación de la Tinaja seleccionada: 8 dp, porque pesa (07 §4.3, fila Tinaja; propuesta de la Tinaja, tabla
        /// «Animación y sonido»; M13, D-019; tarea TJ2).
        /// </summary>
        public const float TinajaLiftDp = 8f;

        /// <summary>Balanceo de la Tinaja al seleccionarla: ±2° (07 §4.3; propuesta de la Tinaja, «Animación y sonido»).</summary>
        public const float TinajaSwayDeg = 2f;

        /// <summary>
        /// Duración del asiento con balanceo de la Tinaja al seleccionarla: 150 ms, después de la subida (propuesta de la Tinaja,
        /// b.2, tabla «Animación y sonido», fila «Seleccionar la tinaja»: «Sube 8 dp … y se asienta con un balanceo de 2°» en
        /// «100 + 150 ms»). La subida dura <c>anim_lift_ms</c> (100 ms por defecto, 08 §9; <see cref="SetLiftDuration"/>) y el
        /// balanceo empieza cuando termina: unos 250 ms en total.
        /// </summary>
        public const float TinajaSwayMs = 150f;

        /// <summary>Altura extra del área táctil sobre la cuia, para alcanzarla elevada (02 §3.3).</summary>
        public const float TouchHeadroomDp = 32f;

        /// <summary>Deselección: 100 ms ease-in (02 §4.1).</summary>
        public const float LowerMs = 100f;

        /// <summary>
        /// Rechazo: sacudida horizontal de ±8 dp, 3 ciclos (<see cref="ShakeCycles"/>), 240 ms, sin amortiguar (decisión del
        /// dueño del 2026-10-09, T-G: la de ±6 dp, 2 ciclos y 120 ms de 02 §4.1 casi no se veía en el teléfono; 02 §4.1 y 07
        /// §4.3 se alinean en el paso 2). Un toque válido durante la sacudida se atiende como siempre: la sacudida no bloquea
        /// la cola de toques.
        /// </summary>
        public const float ShakeMs = 240f;

        /// <summary>Amplitud de la sacudida (dp; T-G).</summary>
        public const float ShakeDp = 8f;

        /// <summary>Ciclos completos de la sacudida: x = <see cref="ShakeDp"/> · sen(2π · ciclos · t / <see cref="ShakeMs"/>) (T-G).</summary>
        public const int ShakeCycles = 3;

        /// <summary>Sellado completo: 450 ms (02 §4.1; storyboard de 07 §5.3).</summary>
        public const float SealMs = 450f;

        // Aro de sellado y celebración teñido de cielo_horizonte #FFE4A8 (revisión visual del 2026-10-07: el blanco aditivo
        // con poco alfa sobre el panel oscuro se leía gris).
        private static readonly Color RingColor = new Color(1f, 0xE4 / 255f, 0xA8 / 255f, 1f);

        // Brillo cálido detrás de cada cuia sellada durante la victoria (revisión visual del 2026-10-07): #FFE4A8 al 25 %.
        private static readonly Color AuraColor = new Color(1f, 0xE4 / 255f, 0xA8 / 255f, 1f);

        /// <summary>Alfa máximo del brillo de victoria detrás de una cuia sellada.</summary>
        public const float VictoryAuraAlpha = 0.25f;

        private RectTransform _root = null!;
        private RectTransform _body = null!;
        private Image _shadow = null!;
        private Image _aura = null!;
        private Image _back = null!;
        private LiquidGraphic _liquid = null!;
        private Image _front = null!;
        private Image _marks = null!;
        private Image _cap = null!;
        private Image _glow = null!;
        private Image _star = null!;
        private ProceduralArt _art = null!;
        private IBoardFx? _fx;
        private Color[] _doseColor = Array.Empty<Color>();
        private float[] _doseFill = Array.Empty<float>();
        private Action<int>? _onDown;
        private Action<int, bool>? _onUp;
        private Vector2 _shownOffset = new Vector2(float.NaN, float.NaN);
        private float _shownAngle;
        private float _shownShadow = -1f;
        private float _shownMarks = 1f;
        private float _shownAura = -1f;
        private int _glassPpu;
        private bool _glassBoost;
        private bool _bodyInFront;
        private int _bodySibling;
        private MurkyMarks _murky = null!;
        private MudMarks _mud = null!;
        private Cuia _shownCuia;
        private bool _inFlight;
        private RectTransform _slotBack = null!;
        private RectTransform _bodyBack = null!;
        private RectTransform _bodyFront = null!;
        private RectTransform _slotFront = null!;
        private CuiaOverlayHost _host = null!;
        private readonly CuiaOverlay?[] _overlays = new CuiaOverlay?[(int)MechanicTypes.Last + 1];
        private CuiaOverlay? _overlay;
        private bool _boardBound;
        private CuiaSkin _skin = CuiaSkin.Base;
        private RectTransform _skinLayer = null!;
        private Image _skinRim = null!;
        private Image _skinFoot = null!;
        private Image _skinBand = null!;
        private Image _skinBandB = null!;
        private Image _skinHandle = null!;

        private float _u = 24f;
        private float _liftMs = 120f;
        private bool _locked;
        private bool _liftTarget;
        private bool _fruitPatterns;
        private bool _pourIcons;
        private float _liftValue;
        private float _swayElapsed = -1f;
        private bool _swayPending;
        private int _tinajaPartner = -1;
        private bool _glassHidden;
        private float _shakeElapsed = -1f;
        private float _sealElapsed = -1f;
        private bool _sealDropsDone;
        private float _fxDelay = -1f;
        private float _fxElapsed = -1f;
        private bool _sealed;

        /// <summary>Índice de la cuia en el tablero.</summary>
        public int Index { get; private set; }

        /// <summary>Capacidad (dosis).</summary>
        public int Capacity { get; private set; }

        /// <summary>Unidad u en dp (ancho de la cuia / 2, 07 §4.1).</summary>
        public float Unit => _u;

        /// <summary>Alto de la cuia en u.</summary>
        public float HeightU => CuiaGeometry.HeightU(Capacity);

        /// <summary>Ranura (raíz fija; posición del pie en el contenedor del tablero, origen arriba a la izquierda).</summary>
        public RectTransform Root => _root;

        /// <summary>Cuerpo móvil (elevación, sacudida, traslado e inclinación).</summary>
        public RectTransform Body => _body;

        /// <summary>Líquido de la cuia (shader <c>AF/UI/Liquid</c>).</summary>
        public LiquidGraphic Liquid => _liquid;

        /// <summary>Marcas de Aguas turbias: el pardo opaco y el «?» de cada dosis oculta (07 §4.3).</summary>
        public MurkyMarks Murky => _murky;

        /// <summary>Marcas del Barro: el pardo de várzea y sus grumos (02 §9.1 M5).</summary>
        public MudMarks Mud => _mud;

        /// <summary>Último estado pintado con <see cref="ShowContent"/> (<c>default</c> antes del primero).</summary>
        public Cuia ShownCuia => _shownCuia;

        /// <summary>El cuerpo está en vuelo (en la capa de vertido): las marcas de las bandas se ocultan.</summary>
        public bool InFlight => _inFlight;

        /// <summary>Overlay de la última mecánica mostrada (null si la cuia nunca tuvo mecánica). Se oculta solo al abrirse.</summary>
        public CuiaOverlay? Overlay => _overlay;

        /// <summary>Lo que esta cuia ofrece a sus overlays: anclas, geometría, arte, efectos y tablero.</summary>
        public CuiaOverlayHost OverlayHost => _host;

        /// <summary>La vista ya tiene tablero (<see cref="BindBoard"/>): sus overlays se crean y se unen al pedirlos.</summary>
        public bool IsBoardBound => _boardBound;

        /// <summary>Pie de la cuia en reposo, en coordenadas del contenedor del tablero.</summary>
        public Vector2 FootPosition => _root.anchoredPosition;

        /// <summary>Desplazamiento actual del cuerpo respecto a su ranura (esté en la ranura o en la capa de vertido).</summary>
        public Vector2 BodyOffset => float.IsNaN(_shownOffset.x) ? _body.anchoredPosition : _shownOffset;

        /// <summary>El cuerpo está en la capa de vertido (<see cref="MoveBodyTo"/>), delante del resto de la pantalla.</summary>
        public bool BodyInFront => _bodyInFront;

        /// <summary>Ángulo actual del cuerpo (grados; negativo = inclinada a la derecha).</summary>
        public float BodyAngle => _body.localEulerAngles.z > 180f ? _body.localEulerAngles.z - 360f : _body.localEulerAngles.z;

        /// <summary>Un trabajo de animación controla la pose del cuerpo.</summary>
        public bool Locked => _locked;

        /// <summary>Está sellada en la vista (tapa puesta).</summary>
        public bool Sealed => _sealed;

        /// <summary>El vidrio lleva el +10 % de brillo de la selección (solo el origen, D-002).</summary>
        public bool GlassBoosted => _glassBoost;

        /// <summary>
        /// El vidrio (trasero, frontal y marcas de dosis) está oculto porque lo tapa el cuerpo de barro de la Tinaja
        /// (<see cref="TinajaOverlay.BodyVisible"/>): la vasija no se lee dentro de una botella, tampoco boca abajo (TJ2).
        /// </summary>
        public bool GlassHidden => _glassHidden;

        /// <summary>
        /// Elevación de la selección de esta cuia (dp): <see cref="TinajaLiftDp"/> si lo último mostrado es una Tinaja y
        /// <see cref="LiftDp"/> en las demás.
        /// </summary>
        public float LiftAmountDp => _shownCuia.Mechanic == MechanicType.Tinaja ? TinajaLiftDp : LiftDp;

        /// <summary>
        /// Su cuia, si esta es la Tinaja: el índice de la pareja en el tablero inicial del nivel (−1 sin pareja). La fija la
        /// pantalla al construir el tablero y la recibe el <see cref="TinajaOverlay"/> (<see cref="TinajaOverlay.AssignPartner"/>):
        /// así el jirau sigue bajo las dos ranuras de la pareja aunque la partida se reanude tras la suelta (07 §4.3; 02 §5).
        /// </summary>
        public void SetTinajaPartner(int index)
        {
            _tinajaPartner = index;
            if (_overlays[(int)MechanicType.Tinaja] is TinajaOverlay tinaja && tinaja.AttachedHost != null)
            {
                tinaja.AssignPartner(index);
            }
        }

        /// <summary>La pareja de la Tinaja que fijó la pantalla (<see cref="SetTinajaPartner"/>), o −1.</summary>
        public int TinajaPartner => _tinajaPartner;

        /// <summary>Altura actual del líquido en dosis (suma de rellenos), para el chorro.</summary>
        public float LiquidLevel
        {
            get
            {
                float level = 0f;
                for (int k = 0; k < _doseFill.Length; k++)
                {
                    level += _doseFill[k];
                }

                return level;
            }
        }

        /// <summary>
        /// Crea la vista dentro de <paramref name="parent"/> (contenedor del tablero). <paramref name="onDown"/> recibe el
        /// touch-down y <paramref name="onUp"/> el touch-up con "soltado dentro del área táctil" (02 §3.1).
        /// </summary>
        public static CuiaView Create(RectTransform parent, int index, int capacity, ProceduralArt art, Action<int> onDown, Action<int, bool> onUp, IBoardFx? fx = null)
        {
            var go = new GameObject("Cuia" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(HitArea));
            go.transform.SetParent(parent, false);
            CuiaView view = go.AddComponent<CuiaView>();
            view.Build(index, capacity, art, onDown, onUp, fx);
            return view;
        }

        /// <summary>
        /// Coloca la ranura: <paramref name="foot"/> es el centro del pie en el contenedor (origen arriba a la izquierda,
        /// y hacia arriba), <paramref name="cuiaWidth"/> el ancho de la cuia y <paramref name="slotWidth"/> el de la ranura.
        /// </summary>
        public void SetGeometry(Vector2 foot, float cuiaWidth, float slotWidth)
        {
            _u = cuiaWidth / CuiaGeometry.WidthU;
            float h = HeightU * _u;
            _root.anchoredPosition = foot;
            _root.sizeDelta = new Vector2(slotWidth, h + TouchHeadroomDp);
            _body.sizeDelta = new Vector2(cuiaWidth, h);
            _liquid.SetUnit(_u);
            _murky.Layout(_u);
            _mud.Layout(_u);
            _host.U = _u;
            for (int t = 0; t < _overlays.Length; t++)
            {
                CuiaOverlay? o = _overlays[t];
                if (o != null && o.AttachedHost != null)
                {
                    o.Layout(_u, HeightU);
                }
            }

            // Vidrio horneado a la densidad real de la pantalla (guía 4.1: ceil(u_dp × escala) px/u).
            int ppu = _art.CuiaPixelsPerUFor(_u);
            if (ppu != _glassPpu)
            {
                _glassPpu = ppu;
                _back.sprite = _art.CuiaBack(Capacity, ppu);
                _front.sprite = _art.CuiaFront(Capacity, ppu);
                _marks.sprite = _art.CuiaMarks(Capacity, ppu);
            }

            _aura.rectTransform.anchoredPosition = new Vector2(0f, HeightU * 0.5f * _u);
            _aura.rectTransform.sizeDelta = new Vector2(4.2f * _u, (HeightU + 2.2f) * _u);
            _cap.rectTransform.sizeDelta = new Vector2(1.10f * _u, 0.35f * _u);
            _glow.rectTransform.anchoredPosition = new Vector2(0f, (HeightU - 0.3f) * _u);
            _glow.rectTransform.sizeDelta = new Vector2(2.4f * _u, 2.4f * _u);
            _star.rectTransform.anchoredPosition = new Vector2(0f, (HeightU + 0.1f) * _u);
            _star.rectTransform.sizeDelta = new Vector2(40f, 40f);
            PlaceCapAtRest();
            LayoutSkin();
            _shownShadow = -1f;
            UpdateShadow();
        }

        /// <summary>Duración de la elevación (<c>anim_lift_ms</c> × factor de velocidad).</summary>
        public void SetLiftDuration(float ms)
        {
            _liftMs = Mathf.Max(1f, ms);
        }

        /// <summary>
        /// Muestra el contenido final de <paramref name="cuia"/> (sin animación). Una dosis oculta nunca enseña su tinte: la
        /// pinta <see cref="MurkyMarks.PaintHidden"/> (07 §4.3) o, en la Tinaja, el barro opaco de su vasija
        /// (<see cref="Palette.TinajaClay"/>, mate, sin «?» ni pardo: contrato 27); el Barro, <see cref="MudMarks.PaintMud"/>.
        /// Mientras una marca anima, se pinta el líquido pero no se llama a su <c>Show</c> (el final del paso cierra).
        /// </summary>
        public void ShowContent(Cuia cuia, bool sealedNow)
        {
            _shownCuia = cuia;
            int count = cuia.Count;
            bool murkyWater = MurkyMarks.IsMurkyWater(cuia);
            for (int k = 0; k < Capacity; k++)
            {
                if (k >= count)
                {
                    SetDose(k, Color.clear, 0f, 0);
                }
                else if (cuia.IsHiddenAt(k))
                {
                    if (murkyWater)
                    {
                        PaintHiddenDose(k);
                    }
                    else
                    {
                        PaintTinajaHiddenDose(k);
                    }
                }
                else
                {
                    int tint = cuia.ColorAt(k);
                    if (tint == Cuia.MudColor)
                    {
                        PaintMudDose(k);
                    }
                    else
                    {
                        SetDose(k, Palette.Tint(tint), 1f, tint);
                    }
                }
            }

            SetSealed(sealedNow);
            if (!_murky.IsAnimating)
            {
                _murky.Show(cuia);
            }

            if (!_mud.IsAnimating)
            {
                _mud.Show(cuia);
            }

            ShowOverlays(cuia, sealedNow);
        }

        /// <summary>
        /// Une <paramref name="overlay"/> a esta cuia en lugar del que crearía la fábrica para su mecánica (pruebas y
        /// variantes). Después, <see cref="ShowContent"/> lo usa como cualquier otro. Sin tablero todavía, se une en
        /// <see cref="BindBoard"/> (el anfitrión nunca llega a un overlay sin capas ni tablero).
        /// </summary>
        /// <exception cref="InvalidOperationException">La cuia ya tiene un overlay de esa mecánica.</exception>
        public void UseOverlay(CuiaOverlay overlay)
        {
            if (overlay == null)
            {
                throw new ArgumentNullException(nameof(overlay));
            }

            int slot = (int)overlay.Type;
            if (slot <= 0 || slot >= _overlays.Length)
            {
                throw new ArgumentException("El overlay no dibuja ninguna mecánica.", nameof(overlay));
            }

            if (_overlays[slot] != null)
            {
                throw new InvalidOperationException("La cuia ya tiene un overlay de esa mecánica.");
            }

            _overlays[slot] = overlay;
            _overlay = overlay;
            if (_boardBound)
            {
                AttachOverlay(overlay);
            }
        }

        /// <summary>
        /// Une la vista al tablero (lo llama <c>BoardAnimator.Bind</c>): rellena en el anfitrión de los overlays
        /// <paramref name="board"/> y las capas de tablero y, si algún overlay se pidió antes, lo crea o lo une ahora y lo
        /// pinta desde el último estado mostrado. Idempotente: volver a llamarla (cuia añadida) solo actualiza las referencias.
        /// </summary>
        /// <exception cref="ArgumentNullException">Falta alguno de los tres.</exception>
        public void BindBoard(ICuiaBoard board, RectTransform backLayer, RectTransform frontLayer)
        {
            _host.Board = board ?? throw new ArgumentNullException(nameof(board));
            _host.BoardBackLayer = backLayer ?? throw new ArgumentNullException(nameof(backLayer));
            _host.BoardFrontLayer = frontLayer ?? throw new ArgumentNullException(nameof(frontLayer));
            if (_boardBound)
            {
                return;
            }

            _boardBound = true;
            for (int t = 0; t < _overlays.Length; t++)
            {
                CuiaOverlay? o = _overlays[t];
                if (o != null && o.AttachedHost == null)
                {
                    AttachOverlay(o);
                }
            }

            if (_shownCuia.Capacity > 0)
            {
                ShowOverlays(_shownCuia, _sealed);
            }
        }

        // El overlay de la Tinaja de esta cuia, si lo tiene (sin asignaciones).
        private TinajaOverlay? TinajaOf()
        {
            return OverlayOf(MechanicType.Tinaja) as TinajaOverlay;
        }

        /// <summary>Overlay de la mecánica <paramref name="type"/> de esta cuia (null si nunca se creó o aún no tiene tablero).</summary>
        public CuiaOverlay? OverlayOf(MechanicType type)
        {
            int slot = (int)type;
            if (slot <= 0 || slot >= _overlays.Length)
            {
                return null;
            }

            CuiaOverlay? o = _overlays[slot];
            return o != null && o.AttachedHost != null ? o : null;
        }

        /// <summary>El tablero se recolocó (<c>BoardAnimator.BoardLaidOut</c>): avisa a los overlays (<c>OnBoardLayout</c>).</summary>
        public void NotifyBoardLayout()
        {
            for (int t = 0; t < _overlays.Length; t++)
            {
                CuiaOverlay? o = _overlays[t];
                if (o != null && o.AttachedHost != null)
                {
                    o.OnBoardLayout();
                }
            }
        }

        /// <summary>
        /// Revelado animado de las capas <paramref name="layerMask"/> (paso Reveal de la cola con <see cref="RevealStyle.Rule"/>;
        /// la Luciérnaga con <see cref="RevealStyle.Firefly"/>): delega en <see cref="MurkyMarks.PlayReveal"/> con el tinte real
        /// de cada capa del estado mostrado. Las capas se ven turbias desde este fotograma. Devuelve la duración (ms) ya
        /// multiplicada por <paramref name="speedFactor"/>, o 0. En la Tinaja (TJ4), la Luciérnaga no aclara agua turbia (no la
        /// hay, contrato 27): la barriga se enciende por dentro y sus capas salen como bandas (<see cref="TinajaOverlay.PlayFirefly"/>,
        /// con <paramref name="reduceMotion"/>); la regla de la cola ya la anima su overlay (TJ3).
        /// </summary>
        public float PlayReveal(int layerMask, RevealStyle style, float speedFactor = 1f, bool reduceMotion = false)
        {
            if (style == RevealStyle.Firefly && !MurkyMarks.IsMurkyWater(_shownCuia))
            {
                TinajaOverlay? tinaja = TinajaOf();
                return tinaja != null ? tinaja.PlayFirefly(layerMask, _shownCuia, speedFactor, reduceMotion) : 0f;
            }

            float ms = _murky.PlayReveal(_liquid, layerMask, style, _shownCuia, speedFactor);
            if (ms > 0f)
            {
                for (int k = 0; k < Capacity; k++)
                {
                    if (((layerMask >> k) & 1) != 0)
                    {
                        TrackDose(k, Palette.Murky, 1f);
                    }
                }
            }

            return ms;
        }

        /// <summary>
        /// Lavado del barro de las capas <paramref name="layerMask"/> (paso MudWash de la cola): delega en
        /// <see cref="MudMarks.PlayWash"/>. Devuelve la duración (ms) ya multiplicada por <paramref name="speedFactor"/>; 0 =
        /// salto.
        /// </summary>
        public float PlayMudWash(int layerMask = -1, float speedFactor = 1f)
        {
            return _mud.PlayWash(_liquid, layerMask, speedFactor);
        }

        /// <summary>Duración nominal (ms, sin factor) del revelado <paramref name="style"/> (<see cref="MurkyMarks.StepDurationMs"/>).</summary>
        public float RevealStepMs(RevealStyle style)
        {
            return _murky.StepDurationMs(style);
        }

        /// <summary>Duración nominal (ms, sin factor) del lavado del barro (<see cref="MudMarks.WashDurationMs"/>).</summary>
        public float MudWashStepMs => _mud.WashDurationMs;

        /// <summary>
        /// Las marcas de turbias o de barro están animando un paso, o la Luciérnaga está revelando la tinaja
        /// (<see cref="TinajaOverlay.FireflyAnimating"/>; TJ4).
        /// </summary>
        public bool MarksAnimating => _murky.IsAnimating || _mud.IsAnimating || (TinajaOf()?.FireflyAnimating ?? false);

        /// <summary>
        /// Termina en el acto el revelado o el lavado en curso, también el de la Luciérnaga en la tinaja (el <c>ShowContent</c> del
        /// final del paso cierra).
        /// </summary>
        public void SnapMarks()
        {
            TinajaOf()?.SnapFirefly();
            _murky.Snap();
            _mud.Snap();
        }

        /// <summary>
        /// Pinta la dosis <paramref name="k"/> como oculta (pardo opaco y «?»; la intro en el primer impacto). En la Tinaja (la
        /// del último <see cref="ShowContent"/>), su barro opaco: sus ocultas nunca se pintan como agua turbia (contrato 27).
        /// </summary>
        internal void PaintHiddenDose(int k)
        {
            if ((uint)k >= (uint)Capacity)
            {
                return;
            }

            if (!MurkyMarks.IsMurkyWater(_shownCuia))
            {
                PaintTinajaHiddenDose(k);
                return;
            }

            TrackDose(k, _murky.PaintHidden(_liquid, k), 1f);
        }

        // Dosis oculta de la Tinaja: el barro de su vasija, mate, sin tinte, sin patrón y sin icono (la tapa el cuerpo de la
        // vasija, TinajaOverlay; sin su arte, si un test lo apaga, se ve barro y nunca la fruta).
        private void PaintTinajaHiddenDose(int k)
        {
            Color clay = Palette.HiddenDose(true);
            TrackDose(k, clay, 1f);
            _liquid.SetMatteDose(k, clay, 1f);
        }

        /// <summary>Pinta la dosis <paramref name="k"/> como Barro (pardo de várzea y grumos; la intro en el primer impacto).</summary>
        internal void PaintMudDose(int k)
        {
            if ((uint)k < (uint)Capacity)
            {
                TrackDose(k, _mud.PaintMud(_liquid, k), 1f);
            }
        }

        /// <summary>Fija el color, el relleno (0..1 de la dosis) y el tinte (0 = agua base) de la dosis <paramref name="k"/>.</summary>
        public void SetDose(int k, Color color, float fill, int tint)
        {
            if ((uint)k >= (uint)Capacity)
            {
                return;
            }

            TrackDose(k, color, fill);
            _liquid.SetDose(k, color, _doseFill[k], tint);
        }

        // Lleva la cuenta de lo que se ve en la dosis k (DoseColor y LiquidLevel), lo pinte la vista o una marca.
        private void TrackDose(int k, Color color, float fill)
        {
            _doseColor[k] = color;
            _doseFill[k] = Mathf.Clamp01(fill);
        }

        /// <summary>Relleno actual (0..1) de la dosis <paramref name="k"/>.</summary>
        public float DoseFill(int k)
        {
            return _doseFill[k];
        }

        /// <summary>Color actual de la dosis <paramref name="k"/>.</summary>
        public Color DoseColor(int k)
        {
            return _doseColor[k];
        }

        /// <summary>Arranca la onda de la superficie (al caer una dosis; 08 §5.2).</summary>
        public void Ripple()
        {
            _liquid.StartRipple();
        }

        /// <summary>
        /// Onda de la superficie en función de un reloj de fuera (la intro, A14): edad en ms (&lt; 0 sin onda) y fase fija; no
        /// acumula estado y no avanza con <see cref="Tick"/> hasta <see cref="ReleaseRipple"/>.
        /// </summary>
        public void SetRippleAt(float ageMs, float phase)
        {
            _liquid.SetRippleAt(ageMs, phase);
        }

        /// <summary>Devuelve la onda al reloj del juego.</summary>
        public void ReleaseRipple()
        {
            _liquid.ReleaseRipple();
        }

        /// <summary>La onda la lleva un reloj de fuera (la intro).</summary>
        public bool RippleDriven => _liquid.RippleDriven;

        /// <summary>Onda en curso (tests).</summary>
        public bool Rippling => _liquid.Rippling;

        /// <summary>Edad (ms; −1 sin onda) y fase (radianes) de la onda (tests de determinismo, A14).</summary>
        public void RippleState(out float ageMs, out float phase)
        {
            ageMs = _liquid.RippleAgeMs;
            phase = _liquid.RipplePhase;
        }

        /// <summary>
        /// Lengua de líquido por la pared inferior del cuello mientras sale el chorro (0..1; revisión visual del
        /// 2026-10-07): une la superficie inclinada con el labio del que sale el chorro.
        /// </summary>
        public void SetTongue(float amount, Color color, int tint)
        {
            _liquid.SetTongue(amount, color, tint);
        }

        /// <summary>Las marcas de dosis se ven (se ocultan con la cuia inclinada).</summary>
        public bool MarksVisible => _shownMarks > 0.001f;

        /// <summary>Pone o quita la tapa sin animación.</summary>
        public void SetSealed(bool sealedNow)
        {
            _sealed = sealedNow;
            _sealElapsed = -1f;
            _cap.gameObject.SetActive(sealedNow);
            _glow.gameObject.SetActive(false);
            _star.gameObject.SetActive(false);
            _liquid.SetFlash(0f);
            PlaceCapAtRest();
        }

        /// <summary>
        /// Fotograma <paramref name="ms"/> del sellado (07 §5.3; guía 6.8): líquido +15 % 0-100 ms, caída de la tapa
        /// 100-250 ms con mini-rebote, y a los 250 ms aro de luz aditivo, destello en estrella y gotas.
        /// </summary>
        public void SealAnimationAt(float ms)
        {
            if (_sealElapsed < 0f || ms < _sealElapsed)
            {
                _sealDropsDone = false;
                AudioCues.Seal(); // AU1: maracá y tapa (la tapa cae a los 100-250 ms, con el «plop» del sonido) y su háptico ligero
            }

            _sealed = true;
            _sealElapsed = ms;
            _liquid.SetFlash(ms < 100f ? Mathf.Sin(Mathf.PI * (ms / 100f)) * 0.15f : 0f);
            bool capVisible = ms >= 100f;
            _cap.gameObject.SetActive(capVisible);
            if (capVisible)
            {
                float t = Mathf.Clamp01((ms - 100f) / 150f);
                float drop = 0.6f * (1f - Ease.Out(t));
                float bounce = ms > 250f && ms < 350f ? 0.03f * Mathf.Sin(Mathf.PI * ((ms - 250f) / 100f)) : 0f;
                _cap.rectTransform.anchoredPosition = new Vector2(0f, (HeightU - 0.04f + drop + bounce) * _u);
            }

            bool ring = ms >= 250f && ms < SealMs;
            _glow.gameObject.SetActive(ring);
            _star.gameObject.SetActive(ring);
            if (ring)
            {
                float t = (ms - 250f) / (SealMs - 250f);
                _glow.color = new Color(RingColor.r, RingColor.g, RingColor.b, 0.9f * (1f - (t * t)));
                _glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.25f, Ease.Out(t));
                float star = t < 0.4f ? Mathf.Lerp(0f, 1.2f, t / 0.4f) : Mathf.Lerp(1.2f, 0f, (t - 0.4f) / 0.6f);
                _star.rectTransform.localScale = Vector3.one * star;
                _star.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f * t);
                _star.color = new Color(1f, 0.98f, 0.9f, Mathf.Clamp01(star));
                if (!_sealDropsDone && _fx != null)
                {
                    _sealDropsDone = true;
                    Color tint = _doseColor.Length > 0 ? _doseColor[Capacity - 1] : Color.white;
                    _fx.SealDrops(FootPosition + BodyOffset + new Vector2(0f, HeightU * _u), tint, _u);
                }
            }

            if (ms >= SealMs)
            {
                SetSealed(true);
            }
        }

        /// <summary>
        /// Elevación de la selección (02 §4.1); se aplica cuando ningún trabajo controla el cuerpo. Mientras está elevada, sus
        /// dosis visibles muestran el icono de su fruta en cualquier nivel (07 §3, regla 2: «al seleccionar una cuia, cada dosis
        /// muestra su icono de fruta durante la elevación aunque el toggle esté apagado»; T-E.5), y al bajarla se apaga si el
        /// nivel no tiene Patrones de fruta (<see cref="SetFruitPatterns"/>) ni la toma un vertido como origen, que los mantiene
        /// hasta su retorno (<see cref="SetPourIcons"/>; 07 §5.2). Solo la cuia elevada: nada se resalta en los destinos (D-002).
        /// </summary>
        public void SetLifted(bool lifted)
        {
            if (lifted && !_liftTarget && _shownCuia.Mechanic == MechanicType.Tinaja)
            {
                _swayPending = true; // la tinaja sube y, arriba, se asienta con su balanceo (07 §4.3; b.2: 100 + 150 ms)
                _swayElapsed = -1f;
            }
            else if (!lifted)
            {
                _swayPending = false;
            }

            _liftTarget = lifted;
            SetGlassBoost(lifted && !_locked);
            ApplyFruitIcons();
        }

        /// <summary>La cuia está elevada o subiendo (selección o touch-down en curso).</summary>
        public bool IsLifted => _liftTarget;

        /// <summary>
        /// Patrones de fruta del nivel (07 §3, regla 1; 02 §12; T-E): con true, cada dosis visible lleva el icono de su fruta
        /// (<see cref="LiquidGraphic.SetFruitIcons"/>), esté elevada o no. Lo fija <c>BoardAnimator.ApplyLevelPatterns</c> al
        /// cargar el nivel y al unir una cuia añadida; sin él, false (solo el icono de la elevación).
        /// </summary>
        public void SetFruitPatterns(bool on)
        {
            _fruitPatterns = on;
            ApplyFruitIcons();
        }

        /// <summary>El nivel tiene Patrones de fruta (<see cref="SetFruitPatterns"/>).</summary>
        public bool FruitPatterns => _fruitPatterns;

        /// <summary>
        /// Iconos del origen mientras un vertido lo tiene tomado (07 §5.2: «iconos de fruta visibles» desde el toque en el
        /// origen, también en el viaje y el caudal, y «iconos se ocultan» en el retorno; 07 §4.3, fila Seleccionada; T-E.5): el
        /// toque en el destino baja la selección (<see cref="SetLifted"/> false), pero los iconos siguen hasta que la cuia
        /// empieza a volver a su ranura. Lo encienden y lo apagan los trabajos de vertido de <c>BoardAnimator</c>; solo en el
        /// origen, nunca en el destino (D-002). Sin asignaciones.
        /// </summary>
        public void SetPourIcons(bool on)
        {
            if (_pourIcons == on)
            {
                return;
            }

            _pourIcons = on;
            ApplyFruitIcons();
        }

        /// <summary>
        /// El líquido pinta el icono de fruta en sus dosis visibles: con Patrones de fruta en el nivel, con la cuia elevada o con
        /// la cuia como origen de un vertido hasta su retorno (07 §3, reglas 1 y 2; 07 §5.2; <see cref="SetPourIcons"/>). Las
        /// ocultas, el Barro y el agua base nunca lo llevan (<see cref="LiquidGraphic.FruitIconDrawn"/>).
        /// </summary>
        public bool FruitIconsOn => _liquid.FruitIcons;

        /// <summary>Rechazo suave: sacudida (02 §4.1, C2, C6; T-G). Sin rojo, sin texto y sin cambiar de color (guía 6.3).</summary>
        public void Shake()
        {
            _shakeElapsed = 0f;
            OverlayOf(_shownCuia.Mechanic)?.OnRejected();
        }

        /// <summary>Sacudida en curso (desde <see cref="Shake"/> hasta <see cref="ShakeMs"/>, o hasta que un trabajo la bloquea).</summary>
        public bool Shaking => _shakeElapsed >= 0f;

        /// <summary>
        /// Desplazamiento horizontal (dp) de la sacudida a los <paramref name="ms"/> ms (T-G): <see cref="ShakeDp"/> ·
        /// sen(2π · <see cref="ShakeCycles"/> · ms / <see cref="ShakeMs"/>), sin amortiguar; 0 fuera de [0, <see cref="ShakeMs"/>).
        /// </summary>
        public static float ShakeOffsetAt(float ms)
        {
            if (ms < 0f || ms >= ShakeMs)
            {
                return 0f;
            }

            return ShakeDp * Mathf.Sin(2f * Mathf.PI * ShakeCycles * (ms / ShakeMs));
        }

        // Icono de fruta en el líquido: con Patrones de fruta, mientras la cuia está elevada o mientras un vertido la tiene como
        // origen hasta su retorno (07 §3, reglas 1 y 2; 07 §5.2).
        private void ApplyFruitIcons()
        {
            if (_liquid != null)
            {
                _liquid.SetFruitIcons(_fruitPatterns || _liftTarget || _pourIcons);
            }
        }

        /// <summary>Celebración (07 §5.4): tras <paramref name="delayMs"/>, aro de luz si está sellada y salto de 0,15 u.</summary>
        public void PlayCelebration(float delayMs)
        {
            _fxDelay = Mathf.Max(0f, delayMs);
            _fxElapsed = -1f;
        }

        /// <summary>Toma el control de la pose del cuerpo (trabajo de vertido o intro).</summary>
        public void Lock()
        {
            _locked = true;
            _shakeElapsed = -1f;
            SetGlassBoost(false);
        }

        /// <summary>Fija la pose del cuerpo mientras está bloqueada.</summary>
        public void SetPose(Vector2 offset, float angleDeg)
        {
            WritePose(offset, angleDeg);
        }

        /// <summary>
        /// Lleva el cuerpo visual a <paramref name="layer"/> (la capa de vertido, alineada con el contenedor del tablero y
        /// delante de todo, D-006) conservando su posición en pantalla. La ranura, con su área táctil y su sombra, no se
        /// mueve. Idempotente; sin asignaciones.
        /// </summary>
        public void MoveBodyTo(RectTransform layer)
        {
            if (_bodyInFront)
            {
                return;
            }

            _bodyInFront = true;
            _body.SetParent(layer, false);
            _body.anchorMin = new Vector2(0f, 1f);
            _body.anchorMax = new Vector2(0f, 1f);
            _body.SetAsLastSibling();
            _body.anchoredPosition = _root.anchoredPosition + BodyOffsetOrZero();
            SetInFlight(true);
        }

        /// <summary>Devuelve el cuerpo visual a su ranura (mismo índice de hermano), en la misma posición de pantalla.</summary>
        public void ReturnBody()
        {
            if (!_bodyInFront)
            {
                return;
            }

            _bodyInFront = false;
            _body.SetParent(_root, false);
            _body.SetSiblingIndex(_bodySibling);
            _body.anchorMin = new Vector2(0.5f, 0f);
            _body.anchorMax = new Vector2(0.5f, 0f);
            _body.anchoredPosition = BodyOffsetOrZero();
            SetInFlight(false);
        }

        // En vuelo, las marcas de las bandas se ocultan: las bandas del líquido no giran con el vaso (guía 6.4).
        private void SetInFlight(bool inFlight)
        {
            _inFlight = inFlight;
            _murky.SetInFlight(inFlight);
            _mud.SetInFlight(inFlight);
            for (int t = 0; t < _overlays.Length; t++)
            {
                CuiaOverlay? o = _overlays[t];
                if (o != null && o.AttachedHost != null)
                {
                    o.SetInFlight(inFlight);
                }
            }
        }

        // Overlay de la mecánica, perezoso (se crea la primera vez que aparece; Marea puede devolver una Hoja a mitad de
        // nivel), y Show en todos los creados: cada uno se oculta si su mecánica ya no está. Ninguno se repinta mientras anima.
        // Sin tablero, la creación espera a BindBoard (que vuelve a llamar aquí con el último estado mostrado).
        private void ShowOverlays(Cuia cuia, bool sealedNow)
        {
            if (!_boardBound)
            {
                return;
            }

            MechanicType type = cuia.Mechanic;
            if (type != MechanicType.None)
            {
                CuiaOverlay? overlay = EnsureOverlay(type);
                if (overlay != null)
                {
                    _overlay = overlay;
                }
            }

            for (int t = 0; t < _overlays.Length; t++)
            {
                CuiaOverlay? o = _overlays[t];
                if (o != null && o.AttachedHost != null && !o.IsAnimating)
                {
                    o.Show(cuia, sealedNow);
                }
            }

            SyncTinajaGlass();
        }

        // Mientras se ve el cuerpo de barro de la Tinaja (arte provisional), el vidrio de la cuia (trasero, frontal y marcas de
        // dosis, con su boca y su pie de cuerda) se oculta en todos los estados: la vasija no se lee metida en una botella ni,
        // boca abajo, con el vidrio asomando por los lados de su boca (TJ2). Sin el cuerpo (release hasta el arte), el vidrio.
        private void SyncTinajaGlass()
        {
            bool hide = _overlays[(int)MechanicType.Tinaja] is TinajaOverlay tinaja && tinaja.AttachedHost != null
                && tinaja.Visible && tinaja.BodyVisible;
            if (hide == _glassHidden)
            {
                return;
            }

            _glassHidden = hide;
            _back.enabled = !hide;
            _front.enabled = !hide;
            _marks.enabled = !hide;
            _skinLayer.gameObject.SetActive(!hide); // la skin es de la cuia de vidrio, no de la vasija de barro
        }

        private CuiaOverlay? EnsureOverlay(MechanicType type)
        {
            int slot = (int)type;
            if ((uint)slot >= (uint)_overlays.Length)
            {
                return null;
            }

            CuiaOverlay? overlay = _overlays[slot];
            if (overlay == null)
            {
                overlay = CuiaOverlays.Create(type);
                if (overlay != null)
                {
                    _overlays[slot] = overlay;
                    AttachOverlay(overlay);
                }
            }

            return overlay;
        }

        // Une el overlay con el anfitrión ya completo (capas y tablero; solo tras BindBoard).
        private void AttachOverlay(CuiaOverlay overlay)
        {
            overlay.Attach(_host);
            if (overlay is TinajaOverlay tinaja && _tinajaPartner >= 0)
            {
                tinaja.AssignPartner(_tinajaPartner); // la pareja del tablero inicial, que fijó la pantalla
            }

            if (_inFlight)
            {
                overlay.SetInFlight(true);
            }
        }

        /// <summary>Devuelve el cuerpo a su ranura y libera el control (la elevación arranca desde 0).</summary>
        public void Unlock()
        {
            _locked = false;
            _liftValue = 0f;
            WritePose(Vector2.zero, 0f);
            SetGlassBoost(_liftTarget);
        }

        /// <summary>
        /// Canto interior de la boca por el que sale el chorro, medido desde el eje (u): 0,34 u, dentro del cuello interior
        /// de <see cref="CuiaGeometry.NeckInnerHalfWidthU"/> (0,40 u).
        /// </summary>
        public const float PourEdgeU = 0.34f;

        /// <summary>
        /// Centro de la boca (centro de lo alto del borde de fibra, en el eje a <see cref="HeightU"/>) en coordenadas del
        /// contenedor, con la pose actual. Durante el caudal queda sobre el eje del destino (D-006).
        /// </summary>
        public Vector2 MouthCenterInContainer()
        {
            return FootPosition + BodyOffset + Rotate(new Vector2(0f, HeightU * _u), BodyAngle * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Punto más bajo de la boca con la pose actual: el canto de lo alto del borde de fibra
        /// (<see cref="CuiaGeometry.RimHalfWidthU"/>) del lado bajo, en coordenadas del contenedor. Sin inclinación, el
        /// centro de la boca. D-006: queda a 0,20-0,60 u sobre el borde del destino mientras vierte.
        /// </summary>
        public Vector2 MouthLowestInContainer()
        {
            float deg = BodyAngle;
            if (Mathf.Abs(deg) < 0.5f)
            {
                return MouthCenterInContainer();
            }

            float side = deg > 0f ? -1f : 1f; // el lado bajo: a la izquierda si la cuia gira en sentido antihorario
            return FootPosition + BodyOffset + Rotate(new Vector2(side * CuiaGeometry.RimHalfWidthU * _u, HeightU * _u), deg * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Punto de la boca por el que sale el chorro: el canto interior del lado bajo (<see cref="PourEdgeU"/>, donde acaba
        /// la lengua de líquido), a 0,21 u del punto más bajo de la boca (<see cref="MouthLowestInContainer"/>), en
        /// coordenadas del contenedor, con la pose actual. Sin inclinación, el centro de la boca.
        /// </summary>
        public Vector2 PourPointInContainer()
        {
            float deg = BodyAngle;
            if (Mathf.Abs(deg) < 0.5f)
            {
                return MouthCenterInContainer();
            }

            float angle = deg * Mathf.Deg2Rad;
            float side = deg > 0f ? -1f : 1f;
            return FootPosition + BodyOffset + Rotate(new Vector2(side * PourEdgeU * _u, HeightU * _u), angle);
        }

        /// <summary>
        /// Dirección (unitaria) hacia fuera del labio por el lado bajo, con la pose actual: la del chorro al salir
        /// (revisión visual del 2026-10-07). Sin inclinación, hacia abajo.
        /// </summary>
        public Vector2 PourOutward()
        {
            float deg = BodyAngle;
            if (Mathf.Abs(deg) < 0.5f)
            {
                return Vector2.down;
            }

            float side = deg > 0f ? -1f : 1f;
            return Rotate(new Vector2(side, 0f), deg * Mathf.Deg2Rad);
        }

        /// <summary>Eje de la cuia (unitario, del pie al labio) con la pose actual.</summary>
        public Vector2 AxisUp()
        {
            return Rotate(Vector2.up, BodyAngle * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Brillo cálido aditivo detrás de la cuia (0..1 de <see cref="VictoryAuraAlpha"/>), solo en la victoria y solo si
        /// está sellada: es posterior al juego y no depende de la selección ni de la legalidad (D-002).
        /// </summary>
        public void SetVictoryAura(float amount)
        {
            float a = _sealed ? Mathf.Clamp01(amount) : 0f;
            if (Mathf.Abs(a - _shownAura) < 0.002f)
            {
                return;
            }

            _shownAura = a;
            bool on = a > 0.002f;
            if (_aura.gameObject.activeSelf != on)
            {
                _aura.gameObject.SetActive(on);
            }

            _aura.color = new Color(AuraColor.r, AuraColor.g, AuraColor.b, VictoryAuraAlpha * a);
        }

        /// <summary>El brillo de victoria se ve detrás de la cuia (tests).</summary>
        public bool VictoryAuraVisible => _aura.gameObject.activeSelf;

        /// <summary>Gira <paramref name="v"/> <paramref name="radians"/> en sentido antihorario.</summary>
        public static Vector2 Rotate(Vector2 v, float radians)
        {
            float c = Mathf.Cos(radians);
            float s = Mathf.Sin(radians);
            return new Vector2((v.x * c) - (v.y * s), (v.x * s) + (v.y * c));
        }

        /// <summary>Superficie del líquido en reposo, en coordenadas del contenedor.</summary>
        public Vector2 SurfaceInContainer()
        {
            return FootPosition + new Vector2(0f, (CuiaGeometry.CavityBottomU + LiquidLevel) * _u);
        }

        /// <summary>Avanza las animaciones propias (elevación, sacudida, celebración, onda).</summary>
        public void Tick(float dtMs)
        {
            if (!_locked)
            {
                float target = _liftTarget ? 1f : 0f;
                if (!Mathf.Approximately(_liftValue, target))
                {
                    float duration = _liftTarget ? _liftMs : LowerMs;
                    _liftValue = Mathf.MoveTowards(_liftValue, target, dtMs / duration);
                }

                if (_swayPending && _liftTarget && _liftValue >= 1f)
                {
                    _swayPending = false; // la subida terminó: empieza el asiento con balanceo
                    _swayElapsed = 0f;
                }

                float x = 0f;
                if (_shakeElapsed >= 0f)
                {
                    // T-G: 3 ciclos de ±8 dp en 240 ms; al llegar a 240 ms vuelve a x = 0 exacto (sen(6π) se redondearía).
                    _shakeElapsed += dtMs;
                    if (_shakeElapsed >= ShakeMs)
                    {
                        _shakeElapsed = -1f;
                    }
                    else
                    {
                        x = ShakeOffsetAt(_shakeElapsed);
                    }
                }

                // Subida con rebote (easeOutBack: pico de ≈ 17,6 dp hacia t ≈ 0,58 y asiento en 16; guía 6.1) y bajada
                // ease-in (6.2). La Tinaja sube 8 dp y, arriba, se asienta con un balanceo de ±2° que se apaga (07 §4.3).
                float y = LiftAmountDp * (_liftTarget ? Ease.OutBack(_liftValue) : Ease.In(_liftValue));
                y += HopOffset(dtMs);
                y += _host.BodyNudgeDp; // un paso del overlay (TJ3: su cuia de la Tinaja se asienta al soltarse)
                WritePose(new Vector2(x, y), SwayAngle(dtMs));
            }
            else
            {
                HopOffset(dtMs);
            }

            _liquid.TickRipple(dtMs);
            _murky.Tick(dtMs);
            _mud.Tick(dtMs);
            for (int t = 0; t < _overlays.Length; t++)
            {
                CuiaOverlay? o = _overlays[t];
                if (o != null && o.AttachedHost != null)
                {
                    o.Tick(dtMs);
                }
            }
        }

        // Balanceo de la Tinaja al seleccionarla, desde que termina la subida: 2° · sen(2π · t / T) · (1 − t / T) durante
        // T = TinajaSwayMs; 0 fuera de él, al bajarla o si ya no es una Tinaja. Sin asignaciones.
        private float SwayAngle(float dtMs)
        {
            if (_swayElapsed < 0f)
            {
                return 0f;
            }

            _swayElapsed += dtMs;
            if (_swayElapsed >= TinajaSwayMs || !_liftTarget || _shownCuia.Mechanic != MechanicType.Tinaja)
            {
                _swayElapsed = -1f;
                return 0f;
            }

            float t = _swayElapsed / TinajaSwayMs;
            return TinajaSwayDeg * Mathf.Sin(2f * Mathf.PI * t) * (1f - t);
        }

        /// <summary>Posición de pantalla (px) del último touch-down sobre esta cuia (C10, el arrastre; tarea B11).</summary>
        public Vector2 PressScreenPosition { get; private set; }

        /// <summary>Posición de pantalla (px) del último touch-up del dedo que bajó en esta cuia (C10; B11).</summary>
        public Vector2 ReleaseScreenPosition { get; private set; }

        /// <summary>Cámara del último evento de puntero (null en un lienzo Screen Space Overlay; C10; B11).</summary>
        public Camera? PointerCamera { get; private set; }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            PressScreenPosition = eventData.position;
            ReleaseScreenPosition = eventData.position;
            PointerCamera = eventData.pressEventCamera;
            _onDown?.Invoke(Index);
        }

        /// <inheritdoc />
        public void OnPointerUp(PointerEventData eventData)
        {
            ReleaseScreenPosition = eventData.position;
            PointerCamera = eventData.pressEventCamera;
            bool inside = RectTransformUtility.RectangleContainsScreenPoint(_root, eventData.position, eventData.pressEventCamera);
            _onUp?.Invoke(Index, inside);
        }

        // Solo toca el RectTransform si la pose cambió: con la pantalla quieta el lienzo no se reconstruye (08 §5.4).
        private void WritePose(Vector2 offset, float angleDeg)
        {
            if (offset == _shownOffset && angleDeg == _shownAngle)
            {
                return;
            }

            _shownOffset = offset;
            _shownAngle = angleDeg;
            _body.anchoredPosition = _bodyInFront ? _root.anchoredPosition + offset : offset;
            _body.localRotation = angleDeg == 0f ? Quaternion.identity : Quaternion.Euler(0f, 0f, angleDeg);
            _liquid.SetTilt(angleDeg); // el líquido no rota (guía 6.4)
            UpdateMarks(angleDeg);
            UpdateShadow();
        }

        // Las marcas de dosis giran con el vidrio: con la cuia inclinada cruzarían en diagonal las capas horizontales del
        // líquido, así que se apagan en los primeros 6° (revisión visual del 2026-10-07).
        private void UpdateMarks(float angleDeg)
        {
            float a = 1f - Mathf.Clamp01(Mathf.Abs(angleDeg) / 6f);
            if (Mathf.Abs(a - _shownMarks) < 0.001f)
            {
                return;
            }

            _shownMarks = a;
            _marks.color = new Color(1f, 1f, 1f, a);
        }

        // Sombra de contacto (guía 4.1): en reposo 1,90 × 0,28 u con alfa 0,55 (ΔL* ≥ 15 sobre la repisa); con la cuia
        // elevada se queda en la repisa y se encoge a 1,50 × 0,22 u con alfa 0,25.
        private void UpdateShadow()
        {
            float away = float.IsNaN(_shownOffset.x) ? 0f : Mathf.Clamp01(_shownOffset.magnitude / LiftDp);
            if (Mathf.Abs(away - _shownShadow) < 0.001f)
            {
                return;
            }

            _shownShadow = away;
            float w = Mathf.Lerp(1.90f, 1.50f, away) * _u;
            float h = Mathf.Lerp(0.28f, 0.22f, away) * _u * 2.6f; // el sprite deja borde suave alrededor del núcleo
            _shadow.rectTransform.sizeDelta = new Vector2(w * 1.35f, h);
            _shadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.55f, 0.25f, away));
        }

        private void SetGlassBoost(bool on)
        {
            if (_glassBoost == on)
            {
                return;
            }

            _glassBoost = on;
            Material? mat = on ? UiMaterials.GlassSelected : UiMaterials.Glass;
            if (mat != null)
            {
                _front.material = mat;
            }
        }

        private float HopOffset(float dtMs)
        {
            if (_fxDelay >= 0f)
            {
                _fxDelay -= dtMs;
                if (_fxDelay < 0f)
                {
                    _fxDelay = -1f;
                    _fxElapsed = 0f;
                }

                return 0f;
            }

            if (_fxElapsed < 0f)
            {
                return 0f;
            }

            _fxElapsed += dtMs;
            const float hopMs = 240f;
            const float glowMs = 300f;
            if (_sealed)
            {
                // Aro cálido (#FFE4A8) y destello en estrella de 40 dp como en el sellado (guía 6.8; revisión visual del
                // 2026-10-07: el aro blanco con poco alfa se leía gris). El aro se apaga con 1 − t² para que se vea entero.
                bool on = _fxElapsed < glowMs;
                _glow.gameObject.SetActive(on);
                _star.gameObject.SetActive(on);
                if (on)
                {
                    float t = _fxElapsed / glowMs;
                    _glow.color = new Color(RingColor.r, RingColor.g, RingColor.b, 0.9f * (1f - (t * t)));
                    _glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.25f, Ease.Out(t));
                    float star = t < 0.4f ? Mathf.Lerp(0f, 1.2f, t / 0.4f) : Mathf.Lerp(1.2f, 0f, (t - 0.4f) / 0.6f);
                    _star.rectTransform.localScale = Vector3.one * star;
                    _star.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f * t);
                    _star.color = new Color(1f, 0.98f, 0.9f, Mathf.Clamp01(star));
                }
            }

            if (_fxElapsed >= Mathf.Max(hopMs, glowMs))
            {
                _fxElapsed = -1f;
                _glow.gameObject.SetActive(false);
                _star.gameObject.SetActive(false);
                return 0f;
            }

            return _fxElapsed < hopMs ? 0.15f * _u * Mathf.Sin(Mathf.PI * (_fxElapsed / hopMs)) : 0f;
        }

        private Vector2 BodyOffsetOrZero()
        {
            return float.IsNaN(_shownOffset.x) ? Vector2.zero : _shownOffset;
        }

        // Si la vista se destruye con el cuerpo en la capa de vertido (no debería: la cola lo devuelve antes), el cuerpo
        // no queda huérfano en esa capa.
        private void OnDestroy()
        {
            if (_bodyInFront && _body != null)
            {
                Destroy(_body.gameObject);
            }
        }

        private void PlaceCapAtRest()
        {
            _cap.rectTransform.anchoredPosition = new Vector2(0f, (HeightU - 0.04f) * _u);
        }

        private void Build(int index, int capacity, ProceduralArt art, Action<int> onDown, Action<int, bool> onUp, IBoardFx? fx)
        {
            Index = index;
            Capacity = capacity;
            _art = art;
            _fx = fx;
            _onDown = onDown;
            _onUp = onUp;
            _root = (RectTransform)transform;
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0.5f, 0f);
            GetComponent<HitArea>().raycastTarget = true;

            // Sombra de contacto: en la ranura (no en el cuerpo), así se queda en la repisa al elevarse.
            _shadow = NewImage("ContactShadow", _root, art.ContactShadow, new Color(0f, 0f, 0f, 0.55f));
            SetAnchors(_shadow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f));
            _shadow.rectTransform.anchoredPosition = new Vector2(0f, -0.5f);

            // Brillo de victoria detrás del cuerpo (apagado salvo en la victoria con la cuia sellada).
            _aura = NewImage("VictoryAura", _root, art.Glow, AuraColor);
            SetAnchors(_aura.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f));
            _aura.gameObject.SetActive(false);

            _slotBack = OverlayParts.NewAnchor("SlotBack", _root); // overlays en la ranura, detrás del cuerpo
            _body = NewRect("Body", _root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            _bodySibling = _body.GetSiblingIndex();

            _glassPpu = ProceduralArt.CuiaPixelsPerU;
            _back = NewImage("GlassBack", _body, art.CuiaBack(capacity, _glassPpu), Color.white);
            Stretch(_back.rectTransform);
            _bodyBack = OverlayParts.NewAnchor("BodyBack", _body); // entre el vidrio trasero y el líquido

            var liquidGo = new GameObject("Liquid", typeof(RectTransform), typeof(CanvasRenderer), typeof(LiquidGraphic));
            liquidGo.transform.SetParent(_body, false);
            _liquid = liquidGo.GetComponent<LiquidGraphic>();
            RectTransform lrt = _liquid.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            _liquid.Setup(capacity, art.PatternAtlas, art.FruitIconAtlas);
            _doseColor = new Color[capacity];
            _doseFill = new float[capacity];

            // Marcas de las bandas, encima del líquido y debajo del vidrio frontal: el Barro y, sobre él, los «?».
            _mud = new MudMarks(_body, capacity, art);
            _murky = new MurkyMarks(_body, capacity, art);

            _front = NewImage("GlassFront", _body, art.CuiaFront(capacity, _glassPpu), Color.white);
            Stretch(_front.rectTransform);
            Material? glass = UiMaterials.Glass;
            if (glass != null)
            {
                _front.material = glass;
            }

            _marks = NewImage("DoseMarks", _body, art.CuiaMarks(capacity, _glassPpu), Color.white);
            Stretch(_marks.rectTransform);
            BuildSkinParts(art); // borde, pie, banda y asa de la skin (B14), sobre el vidrio y bajo los overlays y la tapa
            _bodyFront = OverlayParts.NewAnchor("BodyFront", _body); // sobre las marcas de dosis y bajo la tapa

            _cap = NewImage("SealCap", _body, art.SealCap, Color.white);
            SetAnchors(_cap.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            _cap.gameObject.SetActive(false);

            _glow = NewImage("SealRing", _body, art.Ring, RingColor);
            SetAnchors(_glow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f));
            _glow.gameObject.SetActive(false);
            _star = NewImage("SealStar", _body, art.Star, Color.white);
            SetAnchors(_star.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f));
            _star.gameObject.SetActive(false);
            Material? additive = UiMaterials.Additive;
            if (additive != null)
            {
                _glow.material = additive;
                _star.material = additive;
                _aura.material = additive;
            }

            _slotFront = OverlayParts.NewAnchor("SlotFront", _root); // overlays en la ranura, delante del cuerpo
            _host = new CuiaOverlayHost(index, capacity, _slotBack, _bodyBack, _bodyFront, _slotFront, art, fx);
            ApplySkin(art.CuiaSkin);
        }

        // ============================================================================================ skin (B14)

        /// <summary>Skin que lleva la cuia (07 §4.6; <see cref="ApplySkin"/>).</summary>
        public CuiaSkin Skin => _skin;

        /// <summary>Capa de la skin (borde, pie, banda y asa), o sus piezas apagadas con la base (tests y capturas).</summary>
        public RectTransform SkinLayer => _skinLayer;

        /// <summary>Borde de la skin (<c>rim_{skin}</c>; tests).</summary>
        public Image SkinRim => _skinRim;

        /// <summary>Pie de la skin (<c>foot_{skin}</c>; tests).</summary>
        public Image SkinFoot => _skinFoot;

        /// <summary>Banda del hombro (o su mitad izquierda; tests).</summary>
        public Image SkinBand => _skinBand;

        /// <summary>Mitad derecha de la banda bicolor (tests).</summary>
        public Image SkinBandB => _skinBandB;

        /// <summary>Asa decorativa (tests).</summary>
        public Image SkinHandle => _skinHandle;

        /// <summary>Vidrio trasero (tests: su tinte).</summary>
        public Image GlassBackImage => _back;

        /// <summary>Tapa de sellado (tests: su tinte).</summary>
        public Image CapImage => _cap;

        /// <summary>
        /// Pinta la cuia con <paramref name="skin"/> (arte provisional por código; 07 §4.6): el borde, el pie, la banda del hombro (y
        /// el asa) en su color, el vidrio con su tinte (≤ 8 %) y la tapa con el suyo. La cavidad, el líquido, las marcas de dosis y
        /// el brillo no cambian. Con la base, el vidrio horneado tal cual. Sin asignaciones salvo la primera vez.
        /// </summary>
        public void ApplySkin(CuiaSkin skin)
        {
            _skin = skin ?? CuiaSkin.Base;
            bool on = !_skin.IsBase;
            _skinRim.gameObject.SetActive(on);
            _skinFoot.gameObject.SetActive(on);
            _skinBand.gameObject.SetActive(on && _skin.HasBand);
            _skinBandB.gameObject.SetActive(on && _skin.HasBand && _skin.BandBicolor);
            _skinHandle.gameObject.SetActive(on && _skin.HasHandle);
            _skinRim.color = _skin.Rim;
            _skinFoot.color = _skin.Foot;
            _skinBand.color = _skin.Band;
            _skinBandB.color = _skin.BandB;
            _skinHandle.color = _skin.Handle;
            Color glass = _skin.GlassMultiplier;
            _back.color = glass;
            _front.color = glass;
            _cap.color = _skin.Cap;
            LayoutSkin();
        }

        private void BuildSkinParts(ProceduralArt art)
        {
            _skinLayer = NewRect("Skin", _body, Vector2.zero, Vector2.one, new Vector2(0.5f, 0f));
            _skinLayer.offsetMin = Vector2.zero;
            _skinLayer.offsetMax = Vector2.zero;
            _skinFoot = SkinPiece("SkinFoot", art);
            _skinRim = SkinPiece("SkinRim", art);
            _skinBand = SkinPiece("SkinBand", art);
            _skinBandB = SkinPiece("SkinBandB", art);
            _skinHandle = SkinPiece("SkinHandle", art);
        }

        private Image SkinPiece(string name, ProceduralArt art)
        {
            Image img = NewImage(name, _skinLayer, art.RoundedRect, Color.white);
            img.type = Image.Type.Sliced;
            SetAnchors(img.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            img.gameObject.SetActive(false);
            return img;
        }

        // Las piezas en u, de CuiaSkinParts (la misma fuente que la miniatura del Mercado, ProceduralArt.SkinThumb): el pie, el
        // borde en el labio, la banda justo por encima de la cavidad (en el hombro, D-006; nunca cubre la cavidad) y el asa.
        private void LayoutSkin()
        {
            if (_skinLayer == null || _skin.IsBase)
            {
                return;
            }

            float u = _u;
            float corner = ProceduralArt.RoundedRectRadiusPx / Mathf.Max(0.5f, CuiaSkinParts.CornerU * u);
            PlaceSkin(_skinFoot, CuiaSkinParts.Foot(Capacity), u, corner);
            PlaceSkin(_skinRim, CuiaSkinParts.Rim(Capacity), u, corner);
            if (_skin.BandBicolor)
            {
                PlaceSkin(_skinBand, CuiaSkinParts.Band(Capacity, -1), u, corner);
                PlaceSkin(_skinBandB, CuiaSkinParts.Band(Capacity, 1), u, corner);
            }
            else
            {
                PlaceSkin(_skinBand, CuiaSkinParts.Band(Capacity, 0), u, corner);
            }

            PlaceSkin(_skinHandle, CuiaSkinParts.Handle(Capacity), u, corner);
        }

        private static void PlaceSkin(Image img, Rect pieceU, float u, float ppuMultiplier)
        {
            PlaceSkin(img, pieceU.x * u, pieceU.y * u, pieceU.width * u, pieceU.height * u, ppuMultiplier);
        }

        private static void PlaceSkin(Image img, float x, float y, float w, float h, float ppuMultiplier)
        {
            img.rectTransform.anchoredPosition = new Vector2(x, y);
            img.rectTransform.sizeDelta = new Vector2(w, h);
            img.pixelsPerUnitMultiplier = ppuMultiplier;
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            SetAnchors(rt, anchorMin, anchorMax, pivot);
            return rt;
        }

        private static Image NewImage(string name, Transform parent, Sprite? sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void SetAnchors(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
