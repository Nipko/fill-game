#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M10 Matapi (D-007; propuesta b.2 M10) [P]: un cilindro de 6 varillas de fibra (tonos <c>fibra_*</c>) visto de frente,
    /// repartidas como en un cilindro (±15°, ±45° y ±78°: más juntas hacia los bordes), atadas con tres aros de cuerda sobre
    /// las juntas de dosis; en la boca, un embudo de fibra ancho hacia dentro, con el canto interior claro y el hueco oscuro:
    /// la forma ya dice «entra». <b>Ninguna varilla pasa por delante del líquido</b>: el patrón de cada fruta es el canal
    /// para daltónicos (07 §3: 15, 20 y 35 %), y unas rayas verticales sobre la dosis la harían pasar por Camu camu (con los
    /// aros, por la rejilla del Huito). Las dos de los bordes van sobre la pared de vidrio (fuera de la cavidad), de arriba
    /// abajo; las cuatro de dentro se ven por el vidrio vacío, desde la cara superior del líquido hacia arriba, como la parte
    /// de atrás del cilindro que el líquido tapa, y siguen al nivel mientras se llena la cuia. Nunca se abre: al sellarse, el
    /// embudo se cierra con el tirón y la tapa lo cubre (se apaga bajo ella en <see cref="FunnelFadeMs"/>), así que el Matapi
    /// sellado lleva la tapa limpia y el cuello de vidrio despejado, como las demás cuias selladas, y se reconoce por los aros
    /// y las varillas de los bordes.
    /// </summary>
    /// <remarks>
    /// <para><b>Rechazo</b> (R11, sin texto ni color de error; D-002): si se toca como origen, la cuia se sacude y el embudo
    /// late con la sacudida (escala 1 → <see cref="FunnelPulseScale"/> → 1 en <see cref="PulseMs"/>, los 240 ms de la sacudida
    /// de T-G, dentro de los ≤ 300 ms [P] del criterio de la ola 3).</para>
    /// <para><b>Sellado</b> (b.2 M10: «el embudo se cierra con un tirón de cuerda y la trampa sale del agua entre gotas y un
    /// brillo»; solo como receptor, así que la cola lo despacha como paso Seal de esta cuia): dura <see cref="SealMs"/> ×
    /// factor de velocidad. Fases [P] en fracción del paso: 0-0,08 asoma la cuerda, floja (inclinada y con catenaria);
    /// 0,10-0,30 el tirón la tensa y el aro ciñe el embudo, que se cierra (con rebote) y sube sobre el borde; la trampa sube
    /// <see cref="TrapLiftU"/> entre 0,18 y 0,40 (easeOutCubic) y vuelve entre 0,40 y 0,62, mientras <see cref="DripCount"/>
    /// gotas se desprenden del aro de abajo por fuera de la silueta y caen a la repisa, donde salpican (<c>Fx.Splash</c>,
    /// con el anillo sobre la repisa y no dentro del vidrio); destellos dorados (<c>Fx.Sparkles</c>) en la boca a 0,45;
    /// 0,55-0,86 la tapa de calabaza cae con un mini-rebote, como la del sellado normal (07 §5.3), y en cuanto toca el borde
    /// (0,74) el embudo cerrado y el aro de cuerda se apagan bajo ella, para que no se vean dos tramas sobre el borde; 0,70-1
    /// aparece un brillo cálido. La cuerda del tirón es corta (<see cref="ProceduralArt.MatapiCordLengthU"/>) y se desvanece
    /// por la punta: asoma floja, en catenaria, y el tirón la endereza de golpe (easeOutBack en <see cref="CordSnapMs"/>). Al
    /// terminar, la cola pinta <c>ShowContent(After, true)</c> y la tapa de la cuia queda donde cayó la de este paso. No se
    /// muestra ninguna captura.</para>
    /// <para>Todo se crea en <see cref="OnAttach"/>; <see cref="Tick"/>, <see cref="Show"/> y los pasos no asignan memoria y
    /// solo escriben lo que cambia (08 §5.4). Sin pasos ni sacudida, nada se mueve (D-002).</para>
    /// </remarks>
    public sealed class MatapiOverlay : CuiaOverlay
    {
        /// <summary>Varillas del cilindro: tres a cada lado del eje.</summary>
        public const int RodCount = 6;

        /// <summary>Opacidad de las dos varillas de los bordes, sobre la pared de vidrio (la silueta del cilindro).</summary>
        public const float RodAlpha = 0.75f;

        /// <summary>Opacidad de las cuatro varillas de dentro, que solo se ven por el vidrio vacío.</summary>
        public const float InnerRodAlpha = 0.45f;

        /// <summary>Distancia del eje a una varilla del todo de lado (u): x = esto × sen(ángulo).</summary>
        public const float RodRadiusU = 0.86f;

        /// <summary>Holgura entre la cara superior del líquido y el pie de una varilla de dentro (u).</summary>
        public const float RodFaceGapU = 0.03f;

        /// <summary>Tramo más corto de varilla que se dibuja (u): por debajo, la varilla se oculta.</summary>
        public const float MinRodU = 0.12f;

        /// <summary>Lo alto de las varillas sobre el techo de la cavidad (u), bajo el aro de arriba.</summary>
        public const float RodTopAboveCavityU = 0.1f;

        /// <summary>Sellado del Matapi (ms, sin factor de velocidad) [P b.2 M10: ~800 ms].</summary>
        public const float SealMs = 800f;

        /// <summary>Escala del embudo en el pico del latido del rechazo [P].</summary>
        public const float FunnelPulseScale = 1.15f;

        /// <summary>
        /// El latido dura lo que la sacudida: 240 ms (<see cref="CuiaView.ShakeMs"/>; decisión del dueño del 2026-10-09, T-G, que
        /// sustituye a los 120 ms de 02 §4.1), dentro de los ≤ 300 ms [P] del criterio de la ola 3.
        /// </summary>
        public const float PulseMs = CuiaView.ShakeMs;

        /// <summary>Ancho del embudo cerrado, en fracción del abierto [P]: 1,02 u, el del borde de la cuia (1,10 u) bajo la tapa.</summary>
        public const float ClosedFunnelScale = 0.6f;

        /// <summary>Lo que tarda el embudo cerrado en apagarse bajo la tapa cuando esta toca el borde (ms, sin factor) [P].</summary>
        public const float FunnelFadeMs = 80f;

        /// <summary>Lo que tarda la cuerda en enderezarse con el tirón (ms, sin factor; easeOutBack) [P].</summary>
        public const float CordSnapMs = 80f;

        /// <summary>Alto del embudo cerrado, en fracción del abierto [P]: cabe entero sobre el borde y bajo la tapa.</summary>
        public const float ClosedFunnelScaleY = 0.45f;

        /// <summary>Ancho del aro de cuerda con el embudo cerrado, en fracción del suyo [P].</summary>
        public const float ClosedTieScale = 0.55f;

        /// <summary>Ancho del aro de cuerda al empezar el tirón (embudo abierto), en fracción del suyo [P].</summary>
        public const float OpenTieScale = 1.0f;

        /// <summary>Aros de cuerda del cilindro: abajo, en medio y arriba.</summary>
        public const int BandCount = 3;

        /// <summary>Lo que sube la trampa en el sellado (u) [P]: unos 7 dp, que se lean a 48 dp.</summary>
        public const float TrapLiftU = 0.32f;

        /// <summary>Gotas que se desprenden del aro de abajo al subir la trampa (b.2 M10: «entre gotas») [P].</summary>
        public const int DripCount = 4;

        /// <summary>Distancia del eje a la que caen las gotas (u): por fuera de la silueta (cuerpo de 1,00 u de medio ancho).</summary>
        public const float DripXU = 1.06f;

        /// <summary>Altura a la que llegan las gotas (u desde el pie): sobre la repisa, donde salpican.</summary>
        public const float DripLandU = -0.05f;

        /// <summary>Altura desde la que cae la tapa (u sobre su sitio), como en el sellado normal (07 §5.3).</summary>
        public const float CapDropU = 0.6f;

        /// <summary>Destellos de la boca al salir del agua.</summary>
        public const int SealSparkles = 6;

        /// <summary>Inclinación de la cuerda floja, antes del tirón (grados) [P].</summary>
        public const float CordSlackTiltDeg = 22f;

        /// <summary>Inclinación de la cuerda tensa, tras el tirón (grados) [P].</summary>
        public const float CordTautTiltDeg = 18f;

        // Medidas (u) de la boca, del aro y de la tapa. La tapa es la de CuiaView (1,10 × 0,35 u, base a HeightU − 0,04) y
        // el borde de la cuia mide 0,25 u. Abierta, la boca se centra 0,12 u bajo lo alto: el aro queda sobre el borde y el
        // cono entra por el cuello. Cerrada (0,6 × 0,45), se centra 0,03 u bajo lo alto: de HeightU − 0,24 a HeightU + 0,18,
        // entera sobre el borde y bajo la tapa, sin colgar en el cuello. El aro de cuerda sube con ella.
        private const float FunnelBelowTopU = 0.12f;
        private const float ClosedFunnelBelowTopU = 0.03f;
        private const float TieBelowTopU = 0.36f;
        private const float ClosedTieBelowTopU = 0.17f;
        private const float ClosedTieScaleY = 0.7f;
        private const float CordStretch = 0.06f;
        private const float CapWidthU = 1.10f;
        private const float CapHeightU = 0.35f;
        private const float CapSinkU = 0.04f;
        private const float BottomHoopU = 0.42f;

        // Fases del sellado (fracción del paso) [P].
        private const float CordInEnd = 0.08f;
        private const float TugStart = 0.10f;
        private const float TugEnd = 0.30f;
        private const float CordFadeStart = 0.40f;
        private const float CordFadeEnd = 0.55f;
        private const float LiftStart = 0.18f;
        private const float LiftPeak = 0.40f;
        private const float LiftEnd = 0.62f;
        private const float DripFirst = 0.22f;
        private const float DripGap = 0.05f;
        private const float DripFall = 0.18f;
        private const float DripBelowHoopEndU = 0.12f;
        private const float DripWidthU = 0.16f;
        private const float DripHeightU = 0.21f;
        private const float DripSplashScale = 0.5f;
        private const float SparkleAt = 0.45f;
        private const float CapStart = 0.55f;
        private const float CapLand = 0.74f;
        private const float CapBounceEnd = 0.86f;
        private const float GlowStart = 0.70f;
        private const float TieFadeStart = CapLand;
        private const float TieFadeEnd = CapBounceEnd;
        private const float FunnelFadeStart = CapLand;
        private const float FunnelFadeEnd = CapLand + (FunnelFadeMs / SealMs);
        private const float CordSnapEnd = TugStart + (CordSnapMs / SealMs);

        // Gotas de agua clara al salir del agua (vidrio teñido #CFEFE6). El brillo cálido del sellado es cielo_horizonte
        // (Palette.SkyHorizon, #FFE4A8), como el aro de CuiaView.
        private static readonly Color WaterDrop = Palette.GlassTint;

        private readonly Image[] _rods = new Image[RodCount];
        private readonly Image[] _hoops = new Image[BandCount];
        private readonly Image[] _drips = new Image[DripCount];
        private CuiaView? _view;
        private float _rodLevel;
        private int _splashMask;
        private bool _reduceMotion;
        private RectTransform _trap = null!;
        private Image _funnel = null!;
        private Image _tie = null!;
        private Image _cord = null!;
        private Sprite _cordSlack = null!;
        private Sprite _cordTaut = null!;
        private Image _cap = null!;
        private Image _glow = null!;
        private Image _star = null!;
        private float _u = 24f;
        private float _heightU = CuiaGeometry.HeightU(4);
        private bool _closedLook;
        private float _close;
        private float _pulse = 1f;
        private float _pulseElapsed = -1f;
        private bool _active;
        private float _elapsed;
        private float _duration = 1f;
        private float _progress;
        private bool _sparkled;
        private bool _sealTarget;
        private Vector2 _selfPos;
        private IBoardFx? _fx;
        private float _lift;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Matapi;

        /// <inheritdoc />
        public override bool IsAnimating => _active;

        /// <summary>Escala actual del latido del embudo (1 en reposo).</summary>
        public float FunnelPulse => _pulse;

        /// <summary>El embudo se ve cerrado (Matapi sellado).</summary>
        public bool FunnelClosed => _closedLook;

        /// <summary>El embudo está a la vista (abierto, o cerrado hasta que lo cubre la tapa del sellado).</summary>
        public bool FunnelVisible => _funnel.gameObject.activeSelf && _funnel.color.a > 0.001f;

        /// <summary>Cierre actual del embudo (0 abierto, 1 cerrado; el tirón rebota algo por encima de 1).</summary>
        public float FunnelClose => _close;

        /// <summary>Lo que la trampa está subida ahora mismo (u).</summary>
        public float TrapLift => _lift;

        /// <summary>Fracción del paso de sellado en curso (0..1; 0 sin paso).</summary>
        public float SealProgress => _active ? _progress : 0f;

        /// <summary>La tapa de este paso está a la vista (cae antes de que la cola ponga la de la cuia).</summary>
        public bool StepCapVisible => _cap.gameObject.activeSelf;

        /// <summary>El brillo del sellado está a la vista.</summary>
        public bool GlowVisible => _glow.gameObject.activeSelf;

        /// <summary>La cuerda del tirón está a la vista.</summary>
        public bool CordVisible => _cord.gameObject.activeSelf;

        /// <summary>La cuerda del tirón está floja (con catenaria, antes del tirón).</summary>
        public bool CordSlack => _cord.sprite == _cordSlack;

        /// <summary>Inclinación actual de la cuerda del tirón (grados desde la vertical, hacia la derecha).</summary>
        public float CordTiltDeg => -Mathf.DeltaAngle(0f, _cord.rectTransform.localEulerAngles.z);

        /// <summary>El aro de cuerda del embudo está a la vista (solo durante el sellado).</summary>
        public bool TieVisible => _tie.gameObject.activeSelf && _tie.color.a > 0.001f;

        /// <summary>Centro del embudo (u desde el pie, sin la subida de la trampa).</summary>
        public float FunnelCenterU => _funnel.rectTransform.anchoredPosition.y / _u;

        /// <summary>Alto actual del embudo (u, con su escala).</summary>
        public float FunnelHeightU => ProceduralArt.MatapiMouthHeightU * _funnel.rectTransform.localScale.y;

        /// <summary>La gota <paramref name="i"/> del sellado está a la vista.</summary>
        public bool DripVisible(int i)
        {
            return _drips[i].gameObject.activeSelf;
        }

        /// <summary>Centro de la gota <paramref name="i"/> del sellado (u desde el pie, x desde el eje).</summary>
        public Vector2 DripPosU(int i)
        {
            return _drips[i].rectTransform.anchoredPosition / _u;
        }

        /// <summary>
        /// Tramo a la vista de la varilla <paramref name="i"/> tal como está ahora (u desde el pie, sin la subida de la
        /// trampa); false si está oculta.
        /// </summary>
        public bool ShownRodSpanU(int i, out float bottomU, out float topU)
        {
            RectTransform rt = _rods[i].rectTransform;
            float center = rt.anchoredPosition.y / _u;
            float half = 0.5f * rt.sizeDelta.y / _u;
            bottomU = center - half;
            topU = center + half;
            return _rods[i].gameObject.activeSelf;
        }

        /// <summary>Posición en x (u desde el eje) de la varilla <paramref name="i"/>: tres a cada lado, como en un cilindro.</summary>
        public static float RodXU(int i)
        {
            return RodRadiusU * RodSide(i);
        }

        /// <summary>Ancho de la varilla <paramref name="i"/> (u): las de los bordes, de canto, más finas.</summary>
        public static float RodWidthU(int i)
        {
            return Mathf.Lerp(0.10f, 0.07f, Mathf.Abs(RodSide(i)));
        }

        /// <summary>La varilla <paramref name="i"/> va entera sobre la pared de vidrio, fuera de la cavidad (las de los bordes).</summary>
        public static bool RodOnWall(int i)
        {
            return Mathf.Abs(RodXU(i)) - (0.5f * RodWidthU(i)) >= CuiaGeometry.CavityHalfWidthU;
        }

        /// <summary>
        /// Tramo de la varilla <paramref name="i"/> (u desde el pie) con el líquido a <paramref name="level"/> dosis en una
        /// cuia de <paramref name="capacity"/>: las de la pared, de la base a lo alto; las de dentro, desde la cara superior
        /// del líquido (su canto de atrás, a la altura de la varilla, más <see cref="RodFaceGapU"/>) hacia arriba. Devuelve
        /// false si el tramo mide menos de <see cref="MinRodU"/> (la varilla no se dibuja).
        /// </summary>
        public static bool RodSpanU(int i, int capacity, float level, out float bottomU, out float topU)
        {
            topU = CuiaGeometry.CavityTopU(capacity) + RodTopAboveCavityU;
            bottomU = CuiaGeometry.FootTopU;
            if (!RodOnWall(i) && level > 0.001f)
            {
                float inner = Mathf.Max(0f, Mathf.Abs(RodXU(i)) - (0.5f * RodWidthU(i)));
                bottomU = CuiaGeometry.CavityBottomU + level + CuiaGeometry.FaceSagU(inner) + RodFaceGapU;
            }

            return topU - bottomU >= MinRodU;
        }

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Matapi;
            Visible = on;
            PlaceRods(cuia.Count);
            for (int i = 0; i < BandCount; i++)
            {
                OverlayParts.SetVisible(_hoops[i], on);
            }

            _closedLook = on && sealedNow;
            _close = _closedLook ? 1f : 0f;
            ShowFunnel(on && !_closedLook); // sellado, el embudo cerrado queda bajo la tapa
            HideStepParts();
            SetLift(0f);
            ApplyFunnel();
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            _heightU = heightU;
            float top = CuiaGeometry.CavityTopU(Host.Capacity) + RodTopAboveCavityU;
            PlaceRods(_rodLevel);
            for (int i = 0; i < DripCount; i++)
            {
                RectTransform drip = _drips[i].rectTransform;
                var size = new Vector2(DripWidthU * u, DripHeightU * u);
                if (drip.sizeDelta != size)
                {
                    drip.sizeDelta = size;
                }
            }

            float bandH = ProceduralArt.MatapiBandHeightU;
            OverlayParts.Place(_hoops[0], 0f, BottomHoopU, ProceduralArt.MatapiBandWidthU, bandH, u);
            OverlayParts.Place(_hoops[1], 0f, (BottomHoopU + top) * 0.5f, ProceduralArt.MatapiBandWidthU, bandH, u);
            OverlayParts.Place(_hoops[2], 0f, top - 0.14f, ProceduralArt.MatapiBandWidthU, bandH, u);
            OverlayParts.Place(_funnel, 0f, heightU - FunnelBelowTopU, ProceduralArt.MatapiMouthWidthU, ProceduralArt.MatapiMouthHeightU, u);
            OverlayParts.Place(_tie, 0f, heightU - TieBelowTopU, ProceduralArt.MatapiTieWidthU, ProceduralArt.MatapiTieHeightU, u);
            RectTransform cord = _cord.rectTransform; // pivote en el pie: crece hacia arriba desde el aro (ApplyFunnel lo sigue)
            var cordSize = new Vector2(ProceduralArt.MatapiCordSpriteWidthU * u, ProceduralArt.MatapiCordLengthU * u);
            if (cord.sizeDelta != cordSize)
            {
                cord.sizeDelta = cordSize;
            }

            OverlayParts.Place(_glow, 0f, heightU, 2.6f, 2.6f, u);
            OverlayParts.Place(_star, 0f, heightU + 0.1f, 1.6f, 1.6f, u);
            PlaceCap(0f);
            SetLift(_lift, true);
            ApplyFunnel();
            if (_active)
            {
                Render(_progress);
            }
        }

        /// <inheritdoc />
        public override void OnBoardLayout()
        {
            if (_active && Host.Board != null && (uint)Host.Index < (uint)Host.Board.Count)
            {
                _selfPos = Host.Board.Foot(Host.Index);
            }
        }

        /// <inheritdoc />
        public override void OnRejected()
        {
            if (Visible)
            {
                _pulseElapsed = 0f;
            }
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            // Las varillas de dentro siguen al líquido mientras la cuia se llena (el relleno lo pinta la cola antes que este
            // Tick, en el mismo fotograma).
            if (Visible && _view != null)
            {
                float level = _view.LiquidLevel;
                if (Mathf.Abs(level - _rodLevel) > 0.002f)
                {
                    PlaceRods(level);
                }
            }

            if (_pulseElapsed >= 0f)
            {
                _pulseElapsed += dtMs;
                if (_pulseElapsed >= PulseMs)
                {
                    _pulseElapsed = -1f;
                    _pulse = 1f;
                }
                else
                {
                    _pulse = 1f + ((FunnelPulseScale - 1f) * Mathf.Sin(Mathf.PI * (_pulseElapsed / PulseMs)));
                }

                ApplyFunnel();
            }

            if (_active)
            {
                _elapsed = Mathf.Min(_duration, _elapsed + dtMs);
                Render(_elapsed / _duration);
            }
        }

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            return kind == MechanicStepKind.Seal ? SealMs : 0f;
        }

        /// <inheritdoc />
        public override float PlayStep(in MechanicStepContext ctx)
        {
            if (ctx.Kind != MechanicStepKind.Seal || ctx.Before.Mechanic != MechanicType.Matapi)
            {
                return 0f;
            }

            float ms = SealMs * ctx.SpeedFactor;
            if (ms <= 0f)
            {
                return 0f;
            }

            _active = true;
            _duration = ms;
            _elapsed = 0f;
            _progress = 0f;
            _splashMask = 0;
            _sparkled = false;
            _sealTarget = ctx.AfterSealed;
            _selfPos = ctx.SelfPos;
            _fx = ctx.Fx ?? Host.Fx;
            _reduceMotion = ctx.ReduceMotion;
            _closedLook = false;
            OverlayParts.SetColor(_tie, Color.white);
            ShowFunnel(true);
            Render(0f);
            return ms;
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _progress = 0f;
            _closedLook = Visible && _sealTarget;
            _close = _closedLook ? 1f : 0f;
            ShowFunnel(Visible && !_closedLook);
            HideStepParts();
            SetLift(0f);
            ApplyFunnel();
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            // Varillas, aros, embudo, aro de cuerda y cuerda son de fibra (fibra_*): excepción de la regla 1 de
            // direccion-visual-v2 §2.4. El hueco oscuro de la boca (unos 1,5 × 0,3 u sobre la boca de la cuia) no es de fibra
            // y cuenta. Las gotas, la tapa y el brillo solo se ven durante el paso de sellado.
            into.Add(ProceduralArt.MatapiHole);
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            ProceduralArt art = Host.Art;
            _view = Host.BodyFront.GetComponentInParent<CuiaView>(); // solo para leer el nivel del líquido (puede no haberla)
            _trap = OverlayParts.NewAnchor("MatapiTrap", Host.BodyFront);
            for (int i = 0; i < RodCount; i++)
            {
                // Las de los bordes, sobre la pared de vidrio, en fibra; las de dentro, más claras y transparentes (se ven
                // por el vidrio vacío: la parte de atrás del cilindro). La luz viene de delante.
                float side = Mathf.Abs(RodSide(i));
                bool wall = RodOnWall(i);
                Color fiber = wall ? Palette.FiberBase : (side < 0.5f ? Palette.FiberLight : Palette.FiberBase);
                _rods[i] = OverlayParts.NewImage("MatapiRod" + i, _trap, art.MatapiRod, OverlayParts.Fade(fiber, wall ? RodAlpha : InnerRodAlpha));
            }

            for (int i = 0; i < BandCount; i++)
            {
                _hoops[i] = OverlayParts.NewImage("MatapiBand" + i, _trap, art.MatapiBand, Color.white);
            }

            _funnel = OverlayParts.NewImage("MatapiMouth", _trap, art.MatapiMouth, Color.white);
            _tie = OverlayParts.NewImage("MatapiTie", _trap, art.MatapiTieRing, Color.white);
            _cordSlack = art.MatapiPullCordSlack;
            _cordTaut = art.MatapiPullCord;
            _cord = OverlayParts.NewImage("MatapiCord", _trap, _cordSlack, Color.white);
            _cord.rectTransform.pivot = new Vector2(0.5f, 0f);

            // Lo que no sube con la trampa: las gotas que caen, la tapa del paso y el brillo (aditivo, sin Bloom).
            for (int i = 0; i < DripCount; i++)
            {
                _drips[i] = OverlayParts.NewImage("MatapiDrip" + i, Host.BodyFront, art.Drop, WaterDrop);
            }

            _cap = OverlayParts.NewImage("MatapiStepCap", Host.BodyFront, art.SealCap, Color.white);
            _glow = OverlayParts.NewImage("MatapiGlow", Host.BodyFront, art.Glow, OverlayParts.Fade(Palette.SkyHorizon, 0f));
            _star = OverlayParts.NewImage("MatapiStar", Host.BodyFront, art.Star, new Color(1f, 0.98f, 0.9f, 0f));
            Material? additive = UiMaterials.Additive;
            if (additive != null)
            {
                _glow.material = additive;
                _star.material = additive;
            }
        }

        // Fotograma p (0..1) del sellado.
        private void Render(float p)
        {
            _progress = p;

            // Tirón: la cuerda asoma floja, el tirón la tensa y el aro ciñe el embudo, que se cierra con rebote y sube sobre
            // el borde; el aro se apaga bajo la tapa.
            float pull = Mathf.Clamp01((p - TugStart) / (TugEnd - TugStart));
            _close = Ease.OutBack(pull, 2.2f);
            bool tieOn = p < TieFadeEnd;
            OverlayParts.SetVisible(_tie, tieOn);
            if (tieOn)
            {
                OverlayParts.SetColor(_tie, new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((p - TieFadeStart) / (TieFadeEnd - TieFadeStart))));
            }

            ApplyFunnel();

            // En cuanto la tapa toca el borde, el embudo cerrado se apaga bajo ella (sin dos tramas sobre el borde).
            float funnelAlpha = 1f - Mathf.Clamp01((p - FunnelFadeStart) / (FunnelFadeEnd - FunnelFadeStart));
            OverlayParts.SetVisible(_funnel, funnelAlpha > 0.001f);
            OverlayParts.SetColor(_funnel, new Color(1f, 1f, 1f, funnelAlpha));

            bool cordOn = p < CordFadeEnd;
            OverlayParts.SetVisible(_cord, cordOn);
            if (cordOn)
            {
                Sprite rope = p < TugStart ? _cordSlack : _cordTaut;
                if (_cord.sprite != rope)
                {
                    _cord.sprite = rope;
                }

                float grow = Ease.Out(Mathf.Clamp01(p / CordInEnd));
                float stretch = 1f + (CordStretch * Mathf.Sin(Mathf.PI * pull)); // el tirón la estira un momento
                SetScale(_cord.rectTransform, 1f, Mathf.Max(0.001f, grow * stretch));
                float snap = Ease.OutBack(Mathf.Clamp01((p - TugStart) / (CordSnapEnd - TugStart))); // se endereza de golpe
                float tilt = Mathf.LerpUnclamped(CordSlackTiltDeg, CordTautTiltDeg, snap);
                Quaternion lean = Quaternion.Euler(0f, 0f, -tilt);
                if (_cord.rectTransform.localRotation != lean)
                {
                    _cord.rectTransform.localRotation = lean;
                }

                OverlayParts.SetColor(_cord, new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((p - CordFadeStart) / (CordFadeEnd - CordFadeStart))));
            }

            // La trampa sale del agua y vuelve a su sitio, y gotean gotas del aro de abajo por fuera de la silueta.
            SetLift(LiftAt(p));
            IBoardFx? fx = _fx;
            RenderDrips(p, fx);
            if (!_sparkled && p >= SparkleAt)
            {
                _sparkled = true;
                fx?.Sparkles(ToFx(_selfPos + new Vector2(0f, (_heightU + _lift) * _u)), Palette.SkyHorizon, SealSparkles, _u);
            }

            // La tapa cae con un mini-rebote (07 §5.3).
            bool capOn = p >= CapStart;
            OverlayParts.SetVisible(_cap, capOn);
            if (capOn)
            {
                float drop = CapDropU * (1f - Ease.Out(Mathf.Clamp01((p - CapStart) / (CapLand - CapStart))));
                float bounce = p > CapLand && p < CapBounceEnd ? 0.03f * Mathf.Sin(Mathf.PI * ((p - CapLand) / (CapBounceEnd - CapLand))) : 0f;
                PlaceCap(drop + bounce);
            }

            // Brillo cálido y destello al cerrarse.
            bool glowOn = p >= GlowStart && p < 1f;
            OverlayParts.SetVisible(_glow, glowOn);
            OverlayParts.SetVisible(_star, glowOn);
            if (glowOn)
            {
                float g = (p - GlowStart) / (1f - GlowStart);
                float alpha = 0.85f * (1f - (g * g)) * Mathf.Clamp01(g / 0.15f);
                OverlayParts.SetColor(_glow, OverlayParts.Fade(Palette.SkyHorizon, alpha));
                float scale = Mathf.Lerp(0.8f, 1.3f, Ease.Out(g));
                SetScale(_glow.rectTransform, scale, scale);
                float star = g < 0.4f ? Mathf.Lerp(0f, 1.1f, g / 0.4f) : Mathf.Lerp(1.1f, 0f, (g - 0.4f) / 0.6f);
                SetScale(_star.rectTransform, star, star);
                Quaternion spin = Quaternion.Euler(0f, 0f, 45f * g);
                if (_star.rectTransform.localRotation != spin)
                {
                    _star.rectTransform.localRotation = spin;
                }

                OverlayParts.SetColor(_star, new Color(1f, 0.98f, 0.9f, Mathf.Clamp01(star)));
            }
        }

        // Escala y sitio del embudo, del aro y del pie de la cuerda: el cierre (0 abierto, 1 cerrado) por el latido del
        // rechazo. Al cerrarse, el embudo sube sobre el borde de la cuia y el aro con él.
        private void ApplyFunnel()
        {
            float funnelX = Mathf.LerpUnclamped(1f, ClosedFunnelScale, _close) * _pulse;
            float funnelY = Mathf.LerpUnclamped(1f, ClosedFunnelScaleY, _close) * _pulse;
            SetScale(_funnel.rectTransform, funnelX, funnelY);
            SetY(_funnel.rectTransform, _heightU - Mathf.LerpUnclamped(FunnelBelowTopU, ClosedFunnelBelowTopU, _close));
            float tieX = Mathf.LerpUnclamped(OpenTieScale, ClosedTieScale, _close) * _pulse;
            float tieY = Mathf.LerpUnclamped(1f, ClosedTieScaleY, _close) * _pulse;
            SetScale(_tie.rectTransform, tieX, tieY);
            float tieAt = _heightU - Mathf.LerpUnclamped(TieBelowTopU, ClosedTieBelowTopU, _close);
            SetY(_tie.rectTransform, tieAt);
            SetY(_cord.rectTransform, tieAt);
        }

        // Subida de la trampa en p (u): sube con easeOutCubic hasta el pico y vuelve con una bajada suave.
        private static float LiftAt(float p)
        {
            if (p <= LiftStart || p >= LiftEnd)
            {
                return 0f;
            }

            return p < LiftPeak
                ? TrapLiftU * Ease.OutCubic((p - LiftStart) / (LiftPeak - LiftStart))
                : TrapLiftU * (1f - Ease.InOutSine((p - LiftPeak) / (LiftEnd - LiftPeak)));
        }

        // Gotas del sellado en p: se desprenden por turnos (izquierda, derecha…) del canto del aro de abajo, por fuera de la
        // silueta, caen acelerando hasta la repisa y, la primera de cada lado, salpica allí (Fx.Splash: gotitas y el anillo
        // sobre la repisa). Con Reducir movimiento no se ven (como las partículas del pool).
        private void RenderDrips(float p, IBoardFx? fx)
        {
            for (int i = 0; i < DripCount; i++)
            {
                float start = DripFirst + (i * DripGap);
                float t = (p - start) / DripFall;
                float x = ((i % 2) == 0 ? -1f : 1f) * DripXU;
                bool on = !_reduceMotion && t >= 0f && t < 1f;
                OverlayParts.SetVisible(_drips[i], on);
                if (on)
                {
                    float from = BottomHoopU + DripBelowHoopEndU + LiftAt(start);
                    float y = Mathf.LerpUnclamped(from, DripLandU, t * t) + (0.5f * DripHeightU);
                    var pos = new Vector2(x * _u, y * _u);
                    RectTransform rt = _drips[i].rectTransform;
                    if (rt.anchoredPosition != pos)
                    {
                        rt.anchoredPosition = pos;
                    }

                    SetScale(rt, 1f, 1f + (0.35f * t)); // se estira al caer
                }

                int bit = 1 << i;
                if (i < 2 && t >= 1f && (_splashMask & bit) == 0)
                {
                    _splashMask |= bit;
                    fx?.Splash(ToFx(_selfPos + new Vector2(x * _u, DripLandU * _u)), WaterDrop, DripSplashScale * _u);
                }
            }
        }

        // Coloca las varillas con el líquido a level dosis (RodSpanU) y oculta las que no se ven o el Matapi entero.
        private void PlaceRods(float level)
        {
            _rodLevel = level;
            int capacity = Host.Capacity;
            for (int i = 0; i < RodCount; i++)
            {
                bool on = RodSpanU(i, capacity, level, out float bottom, out float top) && Visible;
                OverlayParts.SetVisible(_rods[i], on);
                if (on)
                {
                    OverlayParts.Place(_rods[i], RodXU(i), (top + bottom) * 0.5f, RodWidthU(i), top - bottom, _u);
                }
            }
        }

        // Fracción del radio de la varilla i (seno de su ángulo en el cilindro): ±15°, ±45° y ±78°, repartidas como en un
        // cilindro visto de frente, más juntas hacia los bordes (|x| = 0,22, 0,61 y 0,84 u). Las de ±78° van sobre la pared.
        private static float RodSide(int i)
        {
            float deg;
            switch (i)
            {
                case 0:
                    deg = -78f;
                    break;
                case 1:
                    deg = -45f;
                    break;
                case 2:
                    deg = -15f;
                    break;
                case 3:
                    deg = 15f;
                    break;
                case 4:
                    deg = 45f;
                    break;
                default:
                    deg = 78f;
                    break;
            }

            return Mathf.Sin(deg * Mathf.Deg2Rad);
        }

        private void SetY(RectTransform rt, float yU)
        {
            var pos = new Vector2(0f, yU * _u);
            if (rt.anchoredPosition != pos)
            {
                rt.anchoredPosition = pos;
            }
        }

        // Muestra u oculta el embudo, opaco.
        private void ShowFunnel(bool on)
        {
            OverlayParts.SetVisible(_funnel, on);
            OverlayParts.SetColor(_funnel, Color.white);
        }

        private void HideStepParts()
        {
            for (int i = 0; i < DripCount; i++)
            {
                OverlayParts.SetVisible(_drips[i], false);
            }

            OverlayParts.SetVisible(_tie, false);
            OverlayParts.SetVisible(_cord, false);
            OverlayParts.SetVisible(_cap, false);
            OverlayParts.SetVisible(_glow, false);
            OverlayParts.SetVisible(_star, false);
        }

        private void SetLift(float liftU, bool force = false)
        {
            if (!force && Mathf.Approximately(liftU, _lift))
            {
                return;
            }

            _lift = liftU;
            var pos = new Vector2(0f, liftU * _u);
            if (_trap.anchoredPosition != pos)
            {
                _trap.anchoredPosition = pos;
            }
        }

        // La tapa del paso, dropU por encima de su sitio (el de la tapa de CuiaView).
        private void PlaceCap(float dropU)
        {
            OverlayParts.Place(_cap, 0f, _heightU - CapSinkU + (CapHeightU * 0.5f) + dropU, CapWidthU, CapHeightU, _u);
        }

        // Punto de las capas de tablero → coordenadas de los efectos (FxLayer: anclas arriba a la izquierda del tablero, que
        // comparte rectángulo con las capas).
        private Vector2 ToFx(Vector2 layerPos)
        {
            RectTransform? layer = Host.BoardFrontLayer;
            if (layer == null)
            {
                return layerPos;
            }

            Rect r = layer.rect;
            return new Vector2(layerPos.x - r.xMin, layerPos.y - r.yMax);
        }

        private static void SetScale(RectTransform rt, float x, float y)
        {
            var s = new Vector3(x, y, 1f);
            if (rt.localScale != s)
            {
                rt.localScale = s;
            }
        }
    }
}
