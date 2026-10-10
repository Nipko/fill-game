#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M3 Liana atada (02 §9.1 M3; propuesta de D-007 b.2 M3; ficha de Magic Sort §3.3 y §2.7) [P]: 2-3 vueltas de liana
    /// verde hoja oscuro con hojitas (el tallo en <see cref="Palette.LeafMid"/>, a ΔE00 16,3 del Lulo; nunca
    /// <c>LeafLight</c> ni <c>JungleLight</c>, a 7,8 y 6,4) que dejan ver el contenido (Q17 A): 1-2 sobre el cuerpo, como una
    /// hélice (vueltas paralelas que bajan de izquierda a derecha y siguen la curva del cilindro, con su tramo trasero detrás
    /// del líquido, su sombra de contacto y su luz de borde; direccion-visual-v2 §4.8), y la del cuello, del mismo grosor, que
    /// acaba en el nudo, arriba a la derecha. Con fruta (<c>key</c> &gt; 0), la fruta llave grande cuelga del nudo, delante
    /// de él, sobre el hombro derecho: puede tapar el cuello y el aro de fibra (la Liana no es destino), nunca el líquido; su
    /// borde de abajo queda al menos 2 dp por encima de la cara superior de la cuia llena. De cuenta (<c>n</c> &gt; 0, variante
    /// del 243), <c>n</c> cuentas perla ensartadas en la vuelta del cuello, junto al nudo (sobre el nudo se leerían como los
    /// ojos de una serpiente; sueltas sobre el hombro, flotarían). Se oculta en cuanto la Liana se suelta.
    /// </summary>
    /// <remarks>
    /// <para><b>Desbloqueo</b> (paso Release, etapa (d) o (d′), después del Seal de la cuia de la llave; <c>ctx.OtherIndex</c>
    /// = la cuia recién sellada): dura <see cref="ReleaseMs"/> × factor de velocidad. Como la tela de Magic Sort (t = 56,
    /// 362 y 398): una ✓ verde sobre la fruta (sprite: el atlas TMP no tiene U+2713); un destello en la tapa de la cuia
    /// sellada y un cometa de luz que sale de ella hasta la fruta (<see cref="BezierStripGraphic"/> aditivos en la capa
    /// delantera del tablero: solo los últimos <see cref="MaxTailU"/> u de lo recorrido, transparentes en la cola, con un halo
    /// de caída suave de <see cref="BeamHalos"/> tiras que se apilan hacia el núcleo y un destello redondo en la punta), con un
    /// brillo y destellos al llegar; y la liana se seca (de verde a pajizo de fibra) y cae. Fases [P] en fracción del paso: ✓
    /// 0-0,14 (y se apaga 0,64-0,74); destello de la tapa 0-0,20; cometa 0,08-0,40 (se apaga y entra en la fruta 0,42-0,56);
    /// brillo 0,38-0,62; se seca 0,48-0,70; cae 0,66-1. La Liana de cuenta hace lo mismo sin la ✓ ni el brillo de la fruta, y
    /// el cometa llega a sus cuentas.</para>
    /// <para><b>Ruta del cometa</b> (riesgo de T12: «el hilo de luz entre filas cruza otras cuias»; direccion-visual-v2 §7: nada
    /// de brillos blancos apilados sobre el líquido). En la misma fila, un arco bajo que pasa por encima de las bocas. Entre
    /// filas, dos Bézier: un arco por encima de las bocas de la fila de la cuia sellada hasta un pasillo entre columnas y la
    /// bajada por ese pasillo hasta la fruta (o la subida, si la Liana está más arriba, por el pasillo de su fila). Se elige,
    /// entre el arco directo y los pasillos más cercanos a la fruta, la ruta que menos pasa por delante del cuerpo de las
    /// cuias; el tramo que aun así pase por delante de una lleva el núcleo a ×<see cref="CrossCoreAlpha"/> y el halo a
    /// ×<see cref="CrossHaloAlpha"/>. El arco se queda en el panel: bajo la repisa de la fila de arriba o, en la fila superior,
    /// bajo el área táctil de las ranuras, con un margen.</para>
    /// <para>El cometa vive fuera de la cuia (en la capa delantera del tablero): un componente en el overlay lo destruye cuando
    /// se destruye la cuia. Todo se crea en <see cref="OnAttach"/>; <see cref="Tick"/>, <see cref="Show"/> y los pasos no
    /// asignan memoria y solo escriben lo que cambia (08 §5.4). Sin paso, nada se mueve (D-002).</para>
    /// </remarks>
    public sealed class LianaOverlay : CuiaOverlay
    {
        /// <summary>Vueltas de liana como mucho, con la del cuello (b.2 M3: 2-3 vueltas).</summary>
        public const int MaxWraps = 3;

        /// <summary>Vueltas sobre el cuerpo como mucho (la otra es la del cuello).</summary>
        public const int MaxBodyWraps = MaxWraps - 1;

        /// <summary>Fracción de la ruta que se ve del cometa como mucho (la cola) [P].</summary>
        public const float CometTail = 0.4f;

        /// <summary>Largo de la cola del cometa como mucho (u) [P]: corto, porque puede pasar cerca de otras cuias.</summary>
        public const float MaxTailU = 1.5f;

        /// <summary>Margen del cometa bajo la repisa de la fila de arriba o el borde de las ranuras (dp).</summary>
        public const float BeamCeilingMarginDp = 4f;

        /// <summary>Desbloqueo de la Liana (ms, sin factor de velocidad) [P b.2 M3: 600-900 ms].</summary>
        public const float ReleaseMs = 750f;

        /// <summary>
        /// Lado de la celda de la fruta llave (u; <see cref="ProceduralArt.FruitFlight"/>, que deja margen y el rabito): la
        /// fruta se ve de unos 0,7 u, grande para la cuia.
        /// </summary>
        public const float FruitCellU = 1.2f;

        /// <summary>Centro de la fruta a la vista: cuelga del hombro derecho, junto al cuello (u desde el eje).</summary>
        public const float FruitCenterXU = 0.60f;

        /// <summary>
        /// Centro de la fruta a la vista sobre el techo de la cavidad (u): su borde de abajo queda al menos 2 dp por encima de
        /// la cara superior del líquido con la cuia llena (Q17 A: el contenido se ve entero).
        /// </summary>
        public const float FruitCenterAboveCavityU = 0.70f;

        /// <summary>Centro del sprite de la vuelta del cuello sobre el techo de la cavidad (u), en el eje.</summary>
        public const float CollarAboveCavityU = 1.00f;

        /// <summary>
        /// Centro del nudo: arriba a la derecha del borde, a la izquierda del rabito de la fruta, que cuelga delante de él (u
        /// desde el eje).
        /// </summary>
        public const float KnotXU = ProceduralArt.LianaCollarKnotXU;

        /// <summary>Centro del nudo sobre el techo de la cavidad (u): sobre el aro de fibra.</summary>
        public const float KnotAboveCavityU = CollarAboveCavityU + ProceduralArt.LianaCollarKnotYU;

        /// <summary>Lado de las cuentas de la Liana de cuenta (u): 4 dp en una cuia de 48 dp, más gruesas que la liana.</summary>
        public const float DotSizeU = 0.17f;

        /// <summary>Posición de la primera cuenta en la vuelta del cuello (0 en el contorno izquierdo del borde, 1 junto al nudo).</summary>
        public const float BeadFirstT = 0.80f;

        /// <summary>Paso entre cuentas en la vuelta del cuello (fracción): ≈ 0,22 u, con 1 dp de hueco entre dos.</summary>
        public const float BeadStepT = 0.27f;

        /// <summary>Tramo de la vuelta del cuello que pueden ocupar las cuentas (fracción): con muchas, más juntas y más pequeñas.</summary>
        public const float BeadSpanT = 0.74f;

        /// <summary>Lo que cae la liana al soltarse (u) [P].</summary>
        public const float FallU = 1.4f;

        /// <summary>Destellos al llegar el hilo de luz.</summary>
        public const int ReleaseSparkles = 5;

        /// <summary>Segmentos de cada tira del cometa.</summary>
        public const int BeamSegments = 24;

        /// <summary>Tramos (Bézier cúbicas) de la ruta del cometa como mucho.</summary>
        public const int MaxRouteSegments = 2;

        /// <summary>
        /// Tiras del halo del cometa [P]: de +<see cref="BeamHaloStepU"/> a +4 × <see cref="BeamHaloStepU"/> u de ancho sobre
        /// el núcleo, cada una con alfa 0,35 × el del halo; aditivas, se apilan de 1 a 4 capas hacia el núcleo (caída suave,
        /// sin bandas).
        /// </summary>
        public const int BeamHalos = 4;

        /// <summary>Lo que cada tira del halo ensancha la anterior (u) [P].</summary>
        public const float BeamHaloStepU = 0.06f;

        /// <summary>Lado del destello de la punta del cometa (u): tapa los extremos del núcleo y del halo.</summary>
        public const float BeamSparkU = 1.0f;

        /// <summary>
        /// Lado de la cabeza del cometa (u): un brillo pequeño e intenso sobre el destello que, sumado a él, satura alrededor
        /// del extremo del núcleo; la punta se lee redonda, sin el corte recto de la tira.
        /// </summary>
        public const float BeamHeadGlowU = 0.6f;

        /// <summary>Lado del destello de la tapa de origen al empezar (u); crece hasta × 1,6 mientras se apaga [P].</summary>
        public const float OriginFlashU = 0.6f;

        /// <summary>Opacidad inicial del destello de la tapa de origen [P].</summary>
        public const float OriginFlashAlpha = 0.5f;

        /// <summary>Fracción del paso en la que se apaga el destello de la tapa de origen [P].</summary>
        public const float OriginFlashEnd = 0.2f;

        /// <summary>Opacidad del núcleo del cometa en el tramo que pasa por delante del cuerpo de una cuia [P].</summary>
        public const float CrossCoreAlpha = 0.45f;

        /// <summary>Opacidad del halo del cometa en el tramo que pasa por delante del cuerpo de una cuia [P].</summary>
        public const float CrossHaloAlpha = 0.15f;

        /// <summary>
        /// Tinte de la liana atada [P] #289454: con el gris del tallo (<see cref="ProceduralArt.LianaStemShade"/>) da
        /// <see cref="Palette.LeafMid"/>.
        /// </summary>
        public static readonly Color FreshTint = Palette.Hex(0x289454);

        /// <summary>Tinte de la liana seca: pajizo de fibra (<see cref="Palette.FiberBase"/>; excepción <c>fibra_*</c>).</summary>
        public static readonly Color DryTint = Palette.FiberBase;

        /// <summary>Luz de borde de la liana atada: <see cref="Palette.LeafShine"/> (línea de 1 dp, 4.8).</summary>
        public static readonly Color FreshShine = OverlayParts.Fade(Palette.LeafShine, 0.9f);

        /// <summary>Luz de borde de la liana seca: fibra clara (<see cref="Palette.FiberLight"/>).</summary>
        public static readonly Color DryShine = OverlayParts.Fade(Palette.FiberLight, 0.9f);

        // Fases del desbloqueo (fracción del paso) [P]. El cometa se ve unos 360 ms (0,08-0,56 de 750 ms).
        private const float CheckInEnd = 0.14f;
        private const float CheckFadeStart = 0.64f;
        private const float CheckFadeEnd = 0.74f;
        private const float BeamStart = 0.08f;
        private const float BeamArrive = 0.40f;
        private const float BeamFadeStart = 0.42f;
        private const float BeamFadeEnd = 0.56f;
        private const float GlowStart = 0.38f;
        private const float GlowEnd = 0.62f;
        private const float DryStart = 0.48f;
        private const float DryEnd = 0.70f;
        private const float FallStart = 0.66f;

        // Medidas (u). La celda de la fruta va 0,04 u a la derecha y 0,07 u por encima del centro de la fruta: el rabito
        // arriba, junto al nudo.
        private const float FruitCellDxU = 0.04f;
        private const float FruitCellDyU = 0.07f;
        private const float GlowU = 1.7f;
        private const float CapTopAboveMouthU = 0.31f;
        private const float BeamBaseU = 0.05f;
        private const float BeamHeadU = 0.14f;
        private const float BeamHaloAlpha = 0.15f;
        private const float MaxBeamLiftU = 2.6f;
        private const float SameRowLiftU = 0.5f;
        private const float SameRowLiftPerU = 0.08f;
        private const float MaxSameRowLiftU = 0.75f;
        private const int CeilingSamples = 16;
        private const int ArcSamples = 24;
        private const int CoverSamples = 5;
        private const float AisleDetourPenalty = 0.02f;

        // Luz del cometa (aditiva): de transparente en la cola a casi blanco con alfa 0,9 en la cabeza, y un halo cálido
        // (cielo_horizonte, Palette.SkyHorizon #FFE4A8, como el brillo y los destellos) de caída suave: BeamHalos tiras de
        // +0,06 a +0,24 u con alfa 0,35 × 0,15 cada una, que se apilan hasta 0,21 junto al núcleo y se desvanecen hacia la
        // cola y hacia fuera.
        private static readonly Color BeamTail = new Color(1f, 0.86f, 0.52f, 0f);
        private static readonly Color BeamHeadColor = new Color(1f, 0.97f, 0.86f, 0.9f);
        private static readonly Color HaloTail = OverlayParts.Fade(Palette.SkyHorizon, 0f);
        private static readonly Color HaloHead = OverlayParts.Fade(Palette.SkyHorizon, 0.35f * BeamHaloAlpha);
        private static readonly Color HeadGlow = new Color(1f, 0.98f, 0.9f, 1f);

        private readonly Image[] _wraps = new Image[MaxBodyWraps];
        private readonly Image[] _wrapShines = new Image[MaxBodyWraps];
        private readonly Image[] _wrapBacks = new Image[MaxBodyWraps];
        private RectTransform _vine = null!;
        private RectTransform _vineBack = null!;
        private Image _collar = null!;
        private Image _collarShine = null!;
        private Image _glow = null!;
        private Image _fruit = null!;
        private Image _check = null!;
        private PipRow _dots = null!;
        private RectTransform _beamRoot = null!;
        private readonly BezierStripGraphic[] _cores = new BezierStripGraphic[MaxRouteSegments];
        private readonly BezierStripGraphic[] _halos = new BezierStripGraphic[MaxRouteSegments * BeamHalos];
        private Image _spark = null!;
        private Image _head = null!;
        private Image _flash = null!;
        private int _key;
        private int _n;
        private int _dotsLaidOut = -1;
        private float _u = 24f;
        private bool _active;
        private float _elapsed;
        private float _duration = 1f;
        private float _progress;
        private bool _withCheck;
        private bool _hasBeam;
        private bool _sparkled;
        private int _otherIndex = -1;
        private Vector2 _selfPos;
        private IBoardFx? _fx;
        private Color _tint;
        private float _dry;
        private float _fall;

        // Ruta del cometa: 1 o 2 Bézier cúbicas (_route[4k..4k+3]), con su tabla de longitudes para recorrerla a velocidad
        // constante, y los cuerpos de las cuias (para elegir la ruta y atenuar el tramo que pase por delante).
        private readonly Vector2[] _route = new Vector2[4 * MaxRouteSegments];
        private readonly Vector2[] _candidate = new Vector2[4 * MaxRouteSegments];
        private readonly float[] _arc = new float[MaxRouteSegments * (ArcSamples + 1)];
        private readonly float[] _segmentLength = new float[MaxRouteSegments];
        private readonly Rect[] _bodies = new Rect[BoardState.MaxCuias];
        private int _bodyCount;
        private int _segments = 1;
        private int _headSegment;
        private float _routeLength = 1f;
        private float _cover;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Liana;

        /// <inheritdoc />
        public override bool IsAnimating => _active;

        /// <summary>
        /// Vueltas de liana que se dibujan, con la del cuello (b.2 M3: 2-3): 2 en las cuias de 1-2 dosis y 3 en las demás.
        /// </summary>
        public int WrapCount => BodyWrapCount + 1;

        /// <summary>Vueltas sobre el cuerpo: 1 en las cuias de 1-2 dosis y 2 en las demás (la otra va en el cuello).</summary>
        public int BodyWrapCount => Host.Capacity <= 2 ? 1 : MaxBodyWraps;

        /// <summary>El brillo cálido de la fruta llave está a la vista (solo en la Liana con fruta, al llegar el cometa).</summary>
        public bool FruitGlowVisible => _glow.gameObject.activeSelf;

        /// <summary>El destello de la punta del cometa está a la vista.</summary>
        public bool BeamSparkVisible => BeamVisible && _spark.gameObject.activeSelf && _spark.color.a > 0.001f;

        /// <summary>El destello de la tapa de origen está a la vista (primer 20 % del paso).</summary>
        public bool OriginFlashVisible => _beamRoot.gameObject.activeSelf && _flash.gameObject.activeSelf && _flash.color.a > 0.001f;

        /// <summary>Centro del destello de la tapa de origen, en el espacio de las capas de tablero.</summary>
        public Vector2 OriginFlashPos => _flash.rectTransform.anchoredPosition;

        /// <summary>Fruta llave que cuelga del nudo (0 en la Liana de cuenta).</summary>
        public int ShownKey => _key;

        /// <summary>Puntos del nudo a la vista (Liana de cuenta: <c>n</c>).</summary>
        public int CountDots => _dots.Visible ? _dots.Count : 0;

        /// <summary>Fracción del desbloqueo en curso (0..1; 0 sin paso).</summary>
        public float ReleaseProgress => _active ? _progress : 0f;

        /// <summary>La ✓ verde está a la vista sobre la fruta.</summary>
        public bool CheckVisible => _check.gameObject.activeSelf && _check.color.a > 0.001f;

        /// <summary>El hilo de luz está a la vista (el tramo con la punta).</summary>
        public bool BeamVisible => _beamRoot != null && _beamRoot.gameObject.activeSelf && _cores[_headSegment].gameObject.activeSelf && _cores[_headSegment].color.a > 0.001f;

        /// <summary>El núcleo del tramo del cometa que lleva la punta (en la capa delantera del tablero).</summary>
        public BezierStripGraphic Beam => _cores[_headSegment];

        /// <summary>Raíz del cometa: hija directa de la capa delantera del tablero (<c>LianaBeam&lt;índice&gt;</c>).</summary>
        public RectTransform BeamRoot => _beamRoot;

        /// <summary>Tramos de la ruta del cometa (1: arco directo; 2: por un pasillo entre columnas).</summary>
        public int BeamRouteSegments => _segments;

        /// <summary>Lo que la ruta entera del cometa pasa por delante del cuerpo de alguna cuia (dp, muestreado).</summary>
        public float BeamRouteCoverDp => _cover;

        /// <summary>Origen del hilo (la tapa de la cuia sellada) en el espacio de las capas de tablero.</summary>
        public Vector2 BeamFrom => _route[0];

        /// <summary>Destino del hilo (la fruta, o la cuenta del medio en la Liana de cuenta) en el espacio de las capas de tablero.</summary>
        public Vector2 BeamTo => _route[(4 * (_segments - 1)) + 3];

        /// <summary>
        /// Punto más alto de la ruta entera del cometa (desde la tapa hasta la fruta o el nudo), en el espacio de las capas de
        /// tablero: queda bajo la repisa de la fila de arriba o bajo el área táctil de la fila superior.
        /// </summary>
        public float BeamPeakY
        {
            get
            {
                float peak = float.MinValue;
                for (int s = 0; s < _segments; s++)
                {
                    for (int k = 0; k <= CeilingSamples * 2; k++)
                    {
                        peak = Mathf.Max(peak, Bezier(_route, 4 * s, k / (float)(CeilingSamples * 2)).y);
                    }
                }

                return peak;
            }
        }

        /// <summary>Tinte actual de la liana (<see cref="FreshTint"/> atada; hacia <see cref="DryTint"/> al secarse).</summary>
        public Color VineTint => _tint;

        /// <summary>Lo seca que está la liana (0 verde, 1 pajiza).</summary>
        public float DryAmount => _dry;

        /// <summary>Lo que ha caído la liana (u).</summary>
        public float FallAmount => _fall;

        /// <summary>Rectángulo de la celda de la fruta en u (x desde el eje, y desde el techo de la cavidad).</summary>
        public static Rect FruitCellRectU => new Rect(
            FruitCenterXU + FruitCellDxU - (0.5f * FruitCellU),
            FruitCenterAboveCavityU + FruitCellDyU - (0.5f * FruitCellU),
            FruitCellU,
            FruitCellU);

        /// <summary>Punto <paramref name="t"/> (0..1) del tramo <paramref name="segment"/> de la ruta del cometa (tests).</summary>
        public Vector2 RoutePoint(int segment, float t)
        {
            return Bezier(_route, 4 * Mathf.Clamp(segment, 0, _segments - 1), t);
        }

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Liana;
            Visible = on;
            if (on)
            {
                int key = cuia.MechanicKey;
                if (key != _key)
                {
                    _key = key;
                    _fruit.sprite = key > 0 ? Host.Art.FruitFlight(key) : null;
                }

                _n = cuia.MechanicN;
            }

            int wraps = BodyWrapCount;
            for (int i = 0; i < MaxBodyWraps; i++)
            {
                bool shown = on && i < wraps;
                OverlayParts.SetVisible(_wraps[i], shown);
                OverlayParts.SetVisible(_wrapShines[i], shown);
                OverlayParts.SetVisible(_wrapBacks[i], shown);
            }

            OverlayParts.SetVisible(_collar, on);
            OverlayParts.SetVisible(_collarShine, on);
            OverlayParts.SetVisible(_fruit, on && _key > 0);
            int dots = on && _key == 0 ? _n : 0;
            if (dots != _dotsLaidOut)
            {
                LayoutDots(dots);
            }

            _dots.Set(dots, dots);
            PlaceBeads();
            HideStepParts();
            ApplyRest();
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            int cap = Host.Capacity;
            float cavityTop = CuiaGeometry.CavityTopU(cap);
            int wraps = BodyWrapCount;
            float band = cap / (float)wraps;
            for (int i = 0; i < MaxBodyWraps; i++)
            {
                // El arco cruza el eje sobre una junta de dosis (con 4, las 1|2 y 3|4): no tapa el centro de ninguna.
                float y = CuiaGeometry.CavityBottomU + (band * (i + 0.5f)) - ProceduralArt.LianaCoilAxisYU;
                OverlayParts.Place(_wraps[i], 0f, y, ProceduralArt.LianaCoilWidthU, ProceduralArt.LianaCoilHeightU, u);
                OverlayParts.Place(_wrapShines[i], 0f, y, ProceduralArt.LianaCoilWidthU, ProceduralArt.LianaCoilHeightU, u);
                OverlayParts.Place(_wrapBacks[i], 0f, y, ProceduralArt.LianaCoilWidthU, ProceduralArt.LianaCoilHeightU, u);
            }

            float collarY = cavityTop + CollarAboveCavityU;
            float fruitY = cavityTop + FruitCenterAboveCavityU;
            OverlayParts.Place(_collar, 0f, collarY, ProceduralArt.LianaCollarWidthU, ProceduralArt.LianaCollarHeightU, u);
            OverlayParts.Place(_collarShine, 0f, collarY, ProceduralArt.LianaCollarWidthU, ProceduralArt.LianaCollarHeightU, u);
            OverlayParts.Place(_fruit, FruitCenterXU + FruitCellDxU, fruitY + FruitCellDyU, FruitCellU, FruitCellU, u);
            OverlayParts.Place(_glow, FruitCenterXU, fruitY, GlowU, GlowU, u);
            OverlayParts.Place(_check, FruitCenterXU, fruitY, ProceduralArt.LianaCheckU, ProceduralArt.LianaCheckU, u);
            LayoutDots(_dotsLaidOut < 0 ? 0 : _dotsLaidOut);
            if (_active)
            {
                RefreshBeamEnds();
                Render(_progress);
            }
        }

        /// <inheritdoc />
        public override void OnBoardLayout()
        {
            if (_active)
            {
                RefreshBeamEnds();
                Render(_progress);
            }
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (_active)
            {
                _elapsed = Mathf.Min(_duration, _elapsed + dtMs);
                Render(_elapsed / _duration);
            }
        }

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            return kind == MechanicStepKind.Release ? ReleaseMs : 0f;
        }

        /// <inheritdoc />
        public override float PlayStep(in MechanicStepContext ctx)
        {
            if (ctx.Kind != MechanicStepKind.Release || ctx.Before.Mechanic != MechanicType.Liana)
            {
                return 0f;
            }

            float ms = ReleaseMs * ctx.SpeedFactor;
            if (ms <= 0f)
            {
                return 0f;
            }

            Show(ctx.Before, false); // la liana atada de antes de la etapa, en reposo
            _active = true;
            _duration = ms;
            _elapsed = 0f;
            _progress = 0f;
            _sparkled = false;
            _fx = ctx.Fx ?? Host.Fx;
            _selfPos = ctx.SelfPos;
            _otherIndex = ctx.OtherIndex;
            _withCheck = _key > 0;
            RefreshBeamEnds();
            OverlayParts.SetVisible(_beamRoot, _hasBeam);
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
            HideStepParts();
            ApplyRest();
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(Shade(FreshTint, ProceduralArt.LianaStemShade)); // tallo de la liana y punta de las hojitas (= LeafMid)
            into.Add(Shade(FreshTint, ProceduralArt.LianaLeafBaseShade)); // hojitas junto al tallo (≈ LeafDark)
            into.Add(Shade(FreshTint, ProceduralArt.LianaKnotShade)); // lóbulos del nudo
            into.Add(ProceduralArt.Pearl); // puntos de la Liana de cuenta
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            ProceduralArt art = Host.Art;
            _vineBack = OverlayParts.NewAnchor("LianaVineBack", Host.BodyBack); // detrás del líquido: solo sobre el vidrio vacío
            _vine = OverlayParts.NewAnchor("LianaVine", Host.BodyFront);
            for (int i = 0; i < MaxBodyWraps; i++)
            {
                _wrapBacks[i] = OverlayParts.NewImage("LianaWrapBack" + i, _vineBack, art.LianaCoilBack, FreshTint);
                _wraps[i] = OverlayParts.NewImage("LianaWrap" + i, _vine, art.LianaCoil, FreshTint);
                _wrapShines[i] = OverlayParts.NewImage("LianaShine" + i, _vine, art.LianaCoilShine, FreshShine);
            }

            // El nudo va detrás de la fruta, que cuelga delante de él: no se lee como su cáliz.
            _collar = OverlayParts.NewImage("LianaCollar", _vine, art.LianaCollar, FreshTint);
            _collarShine = OverlayParts.NewImage("LianaCollarShine", _vine, art.LianaCollarShine, FreshShine);
            _glow = OverlayParts.NewImage("LianaGlow", _vine, art.Glow, OverlayParts.Fade(Palette.SkyHorizon, 0f));
            Material? additive = UiMaterials.Additive;
            if (additive != null)
            {
                _glow.material = additive;
            }

            _fruit = OverlayParts.NewImage("LianaFruit", _vine, null, Color.white);
            _dots = new PipRow("LianaDot", _vine, art.CounterPip, art.CounterPipEmpty, ProceduralArt.Pearl);
            _check = OverlayParts.NewImage("LianaCheck", _vine, art.LianaCheck, Color.white);
            _tint = FreshTint;

            // Cometa de luz: en la capa delantera del tablero (delante de las cuias), bajo una raíz con el espacio de la capa.
            // Un núcleo por tramo de la ruta, con su halo de hijo (se ocultan juntos); el destello de la punta, el brillo de la
            // cabeza y el destello de la tapa de origen, en la raíz.
            RectTransform layer = Host.BoardFrontLayer!;
            var rootGo = new GameObject("LianaBeam" + Host.Index, typeof(RectTransform));
            rootGo.transform.SetParent(layer, false);
            _beamRoot = (RectTransform)rootGo.transform;
            _beamRoot.anchorMin = layer.pivot;
            _beamRoot.anchorMax = layer.pivot;
            _beamRoot.pivot = new Vector2(0.5f, 0.5f);
            _beamRoot.anchoredPosition = Vector2.zero;
            _beamRoot.sizeDelta = Vector2.zero;
            var center = new Vector2(0.5f, 0.5f);
            _flash = NewGlow("LianaBeamFlash", _beamRoot, Palette.SkyHorizon, additive);
            for (int s = 0; s < MaxRouteSegments; s++)
            {
                _cores[s] = NewStrip("LianaBeamCore" + s, _beamRoot, center, additive);
                RectTransform core = _cores[s].rectTransform;
                for (int k = BeamHalos - 1; k >= 0; k--)
                {
                    _halos[(s * BeamHalos) + k] = NewStrip("LianaBeamHalo" + k, core, center, additive); // de la más ancha a la más estrecha
                }

                _cores[s].gameObject.SetActive(false);
            }

            _spark = NewGlow("LianaBeamSpark", _beamRoot, Palette.SkyHorizon, additive);
            _head = NewGlow("LianaBeamHead", _beamRoot, HeadGlow, additive);
            rootGo.SetActive(false);
            _vine.gameObject.AddComponent<BeamOwner>().Beam = rootGo;
        }

        // Fotograma p (0..1) del desbloqueo.
        private void Render(float p)
        {
            _progress = p;

            // ✓ sobre la fruta.
            if (_withCheck)
            {
                float s = Ease.OutBack(Mathf.Clamp01(p / CheckInEnd));
                float a = 1f - Mathf.Clamp01((p - CheckFadeStart) / (CheckFadeEnd - CheckFadeStart));
                bool on = s > 0.001f && a > 0.001f;
                OverlayParts.SetVisible(_check, on);
                if (on)
                {
                    SetScale(_check.rectTransform, s, s);
                    OverlayParts.SetColor(_check, new Color(1f, 1f, 1f, a));
                }
            }

            if (_hasBeam)
            {
                RenderBeam(p);
            }

            // Brillo y destellos al llegar.
            bool glowOn = p >= GlowStart && p < GlowEnd;
            OverlayParts.SetVisible(_glow, glowOn && _key > 0);
            if (glowOn && _key > 0)
            {
                float g = (p - GlowStart) / (GlowEnd - GlowStart);
                OverlayParts.SetColor(_glow, OverlayParts.Fade(Palette.SkyHorizon, 0.8f * Mathf.Sin(Mathf.PI * g)));
                float scale = Mathf.Lerp(0.8f, 1.2f, Ease.Out(g));
                SetScale(_glow.rectTransform, scale, scale);
            }

            if (!_sparkled && p >= BeamArrive)
            {
                _sparkled = true;
                _fx?.Sparkles(ToFx(_selfPos + (EndLocalU() * _u)), Palette.SkyHorizon, ReleaseSparkles, _u);
            }

            // Se seca y cae.
            _dry = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((p - DryStart) / (DryEnd - DryStart)));
            float q = Mathf.Clamp01((p - FallStart) / (1f - FallStart));
            _fall = FallU * q * q;
            float alpha = 1f - Ease.In(Mathf.Clamp01((q - 0.25f) / 0.75f));
            ApplyVine(Color.Lerp(FreshTint, DryTint, _dry), alpha, _fall, 10f * q);
        }

        // El cometa en p: el destello de la tapa de origen, los últimos MaxTailU u de lo recorrido (un núcleo por tramo de la
        // ruta) y, al apagarse, la cola entra en la fruta (o en el nudo).
        private void RenderBeam(float p)
        {
            float fl = p / OriginFlashEnd;
            bool flashOn = fl < 1f;
            OverlayParts.SetVisible(_flash, flashOn);
            if (flashOn)
            {
                float k = Mathf.SmoothStep(0f, 1f, fl);
                OverlayParts.SetColor(_flash, OverlayParts.Fade(Palette.SkyHorizon, OriginFlashAlpha * (1f - k)));
                SetScale(_flash.rectTransform, 1f + (0.6f * k), 1f + (0.6f * k));
            }

            float head = Ease.InOut(Mathf.Clamp01((p - BeamStart) / (BeamArrive - BeamStart)));
            float fade = Mathf.Clamp01((p - BeamFadeStart) / (BeamFadeEnd - BeamFadeStart));
            float alpha = 1f - Ease.In(fade);
            bool on = head > 0.001f && alpha > 0.001f;
            float span = Mathf.Min(CometTail, MaxTailU * _u / Mathf.Max(1f, _routeLength));
            float tail = Mathf.Lerp(Mathf.Max(0f, head - span), head, fade);
            float split = _segments > 1 ? _segmentLength[0] / _routeLength : 1f;
            _headSegment = _segments > 1 && head > split ? 1 : 0;
            float headCross = 0f;
            for (int s = 0; s < MaxRouteSegments; s++)
            {
                float g0 = s == 0 ? 0f : split;
                float g1 = s == 0 ? split : 1f;
                float lo = Mathf.Max(tail, g0);
                float hi = Mathf.Min(head, g1);
                bool segmentOn = on && s < _segments && hi - lo > 1e-4f;
                OverlayParts.SetVisible(_cores[s], segmentOn);
                if (!segmentOn)
                {
                    continue;
                }

                float range = Mathf.Max(1e-4f, g1 - g0);
                float t0 = LocalT(s, (lo - g0) / range);
                float t1 = LocalT(s, (hi - g0) / range);
                float width = Mathf.Max(1e-4f, head - tail);
                SetSegmentSpan(s, t0, t1, (lo - tail) / width, (hi - tail) / width);
                float cross = CoverFraction(s, t0, t1);
                if (s == _headSegment)
                {
                    headCross = CoverFraction(s, Mathf.Lerp(t0, t1, 0.7f), t1);
                }

                OverlayParts.SetColor(_cores[s], new Color(1f, 1f, 1f, alpha * Mathf.Lerp(1f, CrossCoreAlpha, cross)));
                var halo = new Color(1f, 1f, 1f, alpha * Mathf.Lerp(1f, CrossHaloAlpha, cross));
                for (int k = 0; k < BeamHalos; k++)
                {
                    OverlayParts.SetColor(_halos[(s * BeamHalos) + k], halo);
                }
            }

            OverlayParts.SetVisible(_spark, on);
            OverlayParts.SetVisible(_head, on);
            if (on)
            {
                float local = _segments > 1 ? (_headSegment == 0 ? head / split : (head - split) / Mathf.Max(1e-4f, 1f - split)) : head;
                Vector2 end = Bezier(_route, 4 * _headSegment, LocalT(_headSegment, local));
                SetPos(_spark.rectTransform, end);
                SetPos(_head.rectTransform, end);
                float dim = Mathf.Lerp(1f, CrossCoreAlpha, headCross);
                OverlayParts.SetColor(_spark, OverlayParts.Fade(Palette.SkyHorizon, 0.9f * alpha * dim));
                OverlayParts.SetColor(_head, OverlayParts.Fade(HeadGlow, alpha * dim));
            }
        }

        // Liana en reposo: atada, verde, sin caer.
        private void ApplyRest()
        {
            _dry = 0f;
            _fall = 0f;
            ApplyVine(FreshTint, 1f, 0f, 0f);
        }

        // Tinte, opacidad, caída (u) y giro (grados, en el mismo sentido en todas las vueltas: la hélice se afloja) de la
        // liana, su luz de borde, su tramo trasero, el nudo, la fruta y los puntos.
        private void ApplyVine(Color tint, float alpha, float fallU, float tiltDeg)
        {
            _tint = tint;
            Color c = OverlayParts.Fade(tint, alpha);
            Color shine = OverlayParts.Fade(Color.Lerp(FreshShine, DryShine, _dry), alpha);
            for (int i = 0; i < MaxBodyWraps; i++)
            {
                OverlayParts.SetColor(_wraps[i], c);
                OverlayParts.SetColor(_wrapBacks[i], c);
                OverlayParts.SetColor(_wrapShines[i], shine);
                Quaternion r = tiltDeg == 0f ? Quaternion.identity : Quaternion.Euler(0f, 0f, -tiltDeg * (1f + (0.4f * i)));
                SetRotation(_wraps[i].rectTransform, r);
                SetRotation(_wrapShines[i].rectTransform, r);
                SetRotation(_wrapBacks[i].rectTransform, r);
            }

            OverlayParts.SetColor(_collar, c);
            OverlayParts.SetColor(_collarShine, shine);
            OverlayParts.SetColor(_fruit, new Color(1f, 1f, 1f, alpha));
            _dots.SetAlpha(alpha);
            var pos = new Vector2(0f, -fallU * _u);
            if (_vine.anchoredPosition != pos)
            {
                _vine.anchoredPosition = pos;
            }

            if (_vineBack.anchoredPosition != pos)
            {
                _vineBack.anchoredPosition = pos;
            }
        }

        private void HideStepParts()
        {
            OverlayParts.SetVisible(_check, false);
            OverlayParts.SetVisible(_glow, false);
            if (_beamRoot != null)
            {
                OverlayParts.SetVisible(_beamRoot, false);
            }
        }

        // Cuentas de la Liana de cuenta: el lado (con muchas, más pequeñas) y su sitio en la vuelta del cuello.
        private void LayoutDots(int count)
        {
            _dotsLaidOut = count;
            float size = Mathf.Min(DotSizeU, 0.8f * BeadStep(count) * 0.8f);
            float cavityTop = CuiaGeometry.CavityTopU(Host.Capacity);
            _dots.Layout(KnotXU, cavityTop + KnotAboveCavityU, size, size, _u);
            PlaceBeads();
        }

        // Ensarta las cuentas en la vuelta del cuello, desde el nudo hacia la izquierda (la fila de PipRow, recolocada).
        private void PlaceBeads()
        {
            for (int i = 0; i < PipRow.MaxPips; i++)
            {
                Vector2 at = BeadU(i, _dots.Count) * _u;
                RectTransform rt = _dots.Pip(i).rectTransform;
                if (rt.anchoredPosition != at)
                {
                    rt.anchoredPosition = at;
                }
            }
        }

        // Paso entre cuentas (fracción de la vuelta del cuello) con count cuentas.
        private static float BeadStep(int count)
        {
            return count <= 1 ? BeadStepT : Mathf.Min(BeadStepT, BeadSpanT / (count - 1));
        }

        // Centro de la cuenta i de count (u: x desde el eje, y desde el pie), sobre la vuelta del cuello.
        private Vector2 BeadU(int i, int count)
        {
            Vector2 local = ProceduralArt.LianaCollarPointU(BeadFirstT - (i * BeadStep(count)));
            return new Vector2(local.x, CuiaGeometry.CavityTopU(Host.Capacity) + CollarAboveCavityU + local.y);
        }

        // Ruta del cometa: de la tapa de la cuia sellada (pie + dos veces el centro del cuerpo) a la fruta o al nudo. En la
        // misma fila, un arco bajo por encima de las bocas; entre filas, el arco directo o el de un pasillo (el que menos pase
        // por delante de las cuias), siempre bajo el techo de las ranuras o de la repisa de la fila de arriba.
        private void RefreshBeamEnds()
        {
            ICuiaBoard? board = Host.Board;
            _hasBeam = false;
            if (board == null || (uint)_otherIndex >= (uint)board.Count || (uint)Host.Index >= (uint)board.Count)
            {
                return;
            }

            _selfPos = board.Foot(Host.Index);
            Vector2 foot = board.Foot(_otherIndex);
            Vector2 center = board.BodyCenter(_otherIndex);
            Vector2 top = foot + (2f * (center - foot));
            Vector2 p0 = top + new Vector2(0f, CapTopAboveMouthU * _u);
            Vector2 p3 = _selfPos + (EndLocalU() * _u);
            CacheBodies(board);
            float ceiling = Ceiling(board, Mathf.Max(foot.y, _selfPos.y));
            bool sameRow = Mathf.Abs(foot.y - _selfPos.y) < 0.5f * _u;
            BuildDirect(p0, p3, sameRow, ceiling);
            _segments = 1;
            float best = Score(_route, 1, ceiling);
            if (!sameRow)
            {
                bool down = p3.y < p0.y;
                NearestAisles(board, down ? foot.y : _selfPos.y, p3.x, out float left, out float right);
                for (int side = 0; side < 2; side++)
                {
                    float ax = side == 0 ? left : right;
                    if (float.IsNaN(ax))
                    {
                        continue;
                    }

                    BuildAisle(down, ax, p0, p3, ceiling);
                    float score = Score(_candidate, 2, ceiling);
                    if (score < best)
                    {
                        best = score;
                        System.Array.Copy(_candidate, _route, _route.Length);
                        _segments = 2;
                    }
                }
            }

            MeasureRoute();
            _cover = Cover(_route, _segments);
            _headSegment = 0;
            SetSize(_spark.rectTransform, BeamSparkU * _u);
            SetSize(_head.rectTransform, BeamHeadGlowU * _u);
            SetSize(_flash.rectTransform, OriginFlashU * _u);
            SetPos(_flash.rectTransform, p0);
            _hasBeam = true;
        }

        // Arco directo de un solo tramo: sube poco en la misma fila y más entre filas, bajo el techo.
        private void BuildDirect(Vector2 p0, Vector2 p3, bool sameRow, float ceiling)
        {
            Vector2 d = p3 - p0;
            float lift = sameRow
                ? Mathf.Min((SameRowLiftU * _u) + (SameRowLiftPerU * d.magnitude), MaxSameRowLiftU * _u)
                : Mathf.Min((1.0f * _u) + (0.3f * d.magnitude), MaxBeamLiftU * _u);
            lift = ClampLift(p0, d, lift, ceiling);
            _route[0] = p0;
            _route[1] = p0 + (d * 0.2f) + new Vector2(0f, lift);
            _route[2] = p0 + (d * 0.8f) + new Vector2(0f, lift);
            _route[3] = p3;
        }

        // Ruta por el pasillo ax en dos tramos (en _candidate). Si la Liana está más abajo: un arco por encima de las bocas de
        // la fila sellada hasta lo alto del pasillo (bajo el techo) y la bajada por el pasillo hasta la fruta, a la que entra de
        // lado. Si está más arriba: un tramo bajo, por delante de la repisa de su fila, hasta el pie del pasillo, y la subida
        // por él hasta la fruta.
        private void BuildAisle(bool down, float ax, Vector2 p0, Vector2 p3, float ceiling)
        {
            float u = _u;
            float dirx = ax >= p0.x ? 1f : -1f;
            float hx = Mathf.Abs(ax - p0.x);
            float side = ax >= p3.x ? 1f : -1f;
            float hook = Mathf.Min(0.9f * u, (0.9f * Mathf.Abs(ax - p3.x)) + (0.3f * u));
            Vector2 j;
            if (down)
            {
                // El arco llega a 0,52 u por encima de la tapa con v = 1: se baja para quedar bajo el techo.
                float v = Mathf.Clamp01((ceiling - p0.y) / (0.52f * u));
                j = new Vector2(ax, p0.y + (0.25f * u * v));
                _candidate[1] = new Vector2(p0.x + (dirx * 0.35f * hx), p0.y + (0.55f * u * v));
                _candidate[2] = new Vector2(ax, j.y + (0.45f * u * v));
                _candidate[5] = new Vector2(ax, j.y - (0.70f * (j.y - p3.y)));
                _candidate[6] = new Vector2(p3.x + (side * hook), p3.y);
            }
            else
            {
                j = new Vector2(ax, _selfPos.y + (0.1f * u));
                _candidate[1] = new Vector2(p0.x + (dirx * 0.45f * hx), p0.y + (0.25f * u));
                _candidate[2] = new Vector2(ax, j.y - (0.6f * u));
                _candidate[5] = new Vector2(ax, j.y + (0.70f * (p3.y - j.y)));
                _candidate[6] = new Vector2(p3.x + (side * hook), p3.y + (0.25f * u));
            }

            _candidate[0] = p0;
            _candidate[3] = j;
            _candidate[4] = j;
            _candidate[7] = p3;
        }

        // Las juntas entre ranuras vecinas de la fila con pie en rowFootY más cercanas a x por la izquierda y por la derecha
        // (NaN si no hay): el centro de los pasillos entre columnas, sin los márgenes del panel.
        private void NearestAisles(ICuiaBoard board, float rowFootY, float x, out float left, out float right)
        {
            left = float.NaN;
            right = float.NaN;
            float tolerance = 0.5f * _u;
            for (int i = 0; i < board.Count; i++)
            {
                if (Mathf.Abs(board.Foot(i).y - rowFootY) > tolerance)
                {
                    continue;
                }

                float edge = board.SlotRect(i).xMax;
                bool inner = false;
                for (int k = 0; k < board.Count && !inner; k++)
                {
                    inner = k != i && Mathf.Abs(board.Foot(k).y - rowFootY) <= tolerance && Mathf.Abs(board.SlotRect(k).xMin - edge) < 1f;
                }

                if (!inner)
                {
                    continue;
                }

                if (edge <= x)
                {
                    left = float.IsNaN(left) ? edge : Mathf.Max(left, edge);
                }
                else
                {
                    right = float.IsNaN(right) ? edge : Mathf.Min(right, edge);
                }
            }
        }

        // Cuerpos de las cuias (del pie de fibra al final del hombro, con el ancho máximo del vidrio), en el espacio de las capas.
        private void CacheBodies(ICuiaBoard board)
        {
            _bodyCount = Mathf.Min(board.Count, _bodies.Length);
            for (int i = 0; i < _bodyCount; i++)
            {
                Vector2 foot = board.Foot(i);
                float heightU = 2f * (board.BodyCenter(i).y - foot.y) / Mathf.Max(1e-3f, _u);
                float shoulderTop = heightU - CuiaGeometry.NeckU - CuiaGeometry.RimU;
                float half = CuiaGeometry.BodyOuterHalfWidthU * _u;
                _bodies[i] = Rect.MinMaxRect(foot.x - half, foot.y + (CuiaGeometry.FootTopU * _u), foot.x + half, foot.y + (shoulderTop * _u));
            }
        }

        private bool InsideBody(Vector2 p, float pad)
        {
            for (int i = 0; i < _bodyCount; i++)
            {
                Rect r = _bodies[i];
                if (p.x >= r.xMin - pad && p.x <= r.xMax + pad && p.y >= r.yMin - pad && p.y <= r.yMax + pad)
                {
                    return true;
                }
            }

            return false;
        }

        // Puntuación de una ruta: lo que pasa por delante del cuerpo de las cuias (con 0,12 u de holgura, dp), un poco del largo
        // (a igualdad, la más corta) y mucho por cada dp sobre el techo.
        private float Score(Vector2[] route, int segments, float ceiling)
        {
            float cover = 0f;
            float length = 0f;
            float over = 0f;
            float pad = 0.12f * _u;
            for (int s = 0; s < segments; s++)
            {
                Vector2 prev = Bezier(route, 4 * s, 0f);
                for (int i = 1; i <= ArcSamples; i++)
                {
                    Vector2 q = Bezier(route, 4 * s, i / (float)ArcSamples);
                    float piece = Vector2.Distance(prev, q);
                    Vector2 mid = 0.5f * (prev + q);
                    length += piece;
                    if (InsideBody(mid, pad))
                    {
                        cover += piece;
                    }

                    over = Mathf.Max(over, mid.y - ceiling);
                    prev = q;
                }
            }

            return cover + (AisleDetourPenalty * length) + (10f * over);
        }

        // Lo que la ruta pasa por delante del cuerpo de las cuias, sin holgura (dp).
        private float Cover(Vector2[] route, int segments)
        {
            float cover = 0f;
            for (int s = 0; s < segments; s++)
            {
                Vector2 prev = Bezier(route, 4 * s, 0f);
                for (int i = 1; i <= ArcSamples; i++)
                {
                    Vector2 q = Bezier(route, 4 * s, i / (float)ArcSamples);
                    if (InsideBody(0.5f * (prev + q), 0f))
                    {
                        cover += Vector2.Distance(prev, q);
                    }

                    prev = q;
                }
            }

            return cover;
        }

        // Fracción del tramo [t0, t1] del tramo s de la ruta que pasa por delante de un cuerpo (muestreada).
        private float CoverFraction(int s, float t0, float t1)
        {
            int inside = 0;
            for (int i = 0; i < CoverSamples; i++)
            {
                if (InsideBody(Bezier(_route, 4 * s, Mathf.Lerp(t0, t1, i / (float)(CoverSamples - 1))), 0f))
                {
                    inside++;
                }
            }

            return inside / (float)CoverSamples;
        }

        // Tabla de longitudes de cada tramo, para recorrer la ruta a velocidad constante.
        private void MeasureRoute()
        {
            _routeLength = 0f;
            for (int s = 0; s < MaxRouteSegments; s++)
            {
                int a = s * (ArcSamples + 1);
                _arc[a] = 0f;
                _segmentLength[s] = 0f;
                if (s >= _segments)
                {
                    continue;
                }

                Vector2 prev = Bezier(_route, 4 * s, 0f);
                for (int i = 1; i <= ArcSamples; i++)
                {
                    Vector2 q = Bezier(_route, 4 * s, i / (float)ArcSamples);
                    _arc[a + i] = _arc[a + i - 1] + Vector2.Distance(prev, q);
                    prev = q;
                }

                _segmentLength[s] = _arc[a + ArcSamples];
                _routeLength += _segmentLength[s];
            }

            _routeLength = Mathf.Max(1e-3f, _routeLength);
        }

        // Parámetro t del tramo s a la fracción frac de su longitud.
        private float LocalT(int s, float frac)
        {
            int a = s * (ArcSamples + 1);
            float target = Mathf.Clamp01(frac) * _segmentLength[s];
            for (int i = 1; i <= ArcSamples; i++)
            {
                if (_arc[a + i] >= target)
                {
                    float s0 = _arc[a + i - 1];
                    float s1 = _arc[a + i];
                    float f = s1 > s0 ? (target - s0) / (s1 - s0) : 0f;
                    return (i - 1 + f) / ArcSamples;
                }
            }

            return 1f;
        }

        /// <summary>
        /// Techo del arco: el canto inferior de la ranura (la repisa) de la fila de arriba de la cuia más alta de las dos o, si
        /// esa es la fila superior, el área táctil de las ranuras; menos <see cref="BeamCeilingMarginDp"/> y el medio grosor de
        /// la cabeza.
        /// </summary>
        private float Ceiling(ICuiaBoard board, float highestFootY)
        {
            float above = float.MaxValue;
            float slotsTop = float.MinValue;
            for (int i = 0; i < board.Count; i++)
            {
                Rect slot = board.SlotRect(i);
                slotsTop = Mathf.Max(slotsTop, slot.yMax);
                if (board.Foot(i).y > highestFootY + (0.5f * _u))
                {
                    above = Mathf.Min(above, slot.yMin);
                }
            }

            return (above < float.MaxValue ? above : slotsTop) - BeamCeilingMarginDp - (0.5f * BeamHeadU * _u);
        }

        /// <summary>La mayor elevación del arco directo que lo deja entero bajo el techo: B(t).y = base(t) + 3t(1 − t) · lift.</summary>
        private static float ClampLift(Vector2 p0, Vector2 d, float lift, float ceiling)
        {
            for (int k = 1; k < CeilingSamples; k++)
            {
                float t = k / (float)CeilingSamples;
                float s = 1f - t;
                float baseY = p0.y + (d.y * ((3f * s * s * t * 0.2f) + (3f * s * t * t * 0.8f) + (t * t * t)));
                lift = Mathf.Min(lift, (ceiling - baseY) / (3f * s * t));
            }

            return Mathf.Max(0f, lift);
        }

        // El tramo [t0, t1] del tramo s de la ruta (de Casteljau: primero [0, t1] y, de ese, la parte desde t0 / t1), con el
        // grosor y el color del cometa entre las fracciones q0 y q1 de lo que se ve (de la cola a la punta).
        private void SetSegmentSpan(int s, float t0, float t1, float q0, float q1)
        {
            int o = 4 * s;
            Vector2 a = Vector2.LerpUnclamped(_route[o], _route[o + 1], t1);
            Vector2 b = Vector2.LerpUnclamped(_route[o + 1], _route[o + 2], t1);
            Vector2 c = Vector2.LerpUnclamped(_route[o + 2], _route[o + 3], t1);
            Vector2 ab = Vector2.LerpUnclamped(a, b, t1);
            Vector2 bc = Vector2.LerpUnclamped(b, c, t1);
            Vector2 end = Vector2.LerpUnclamped(ab, bc, t1);
            float f = t1 > 1e-4f ? Mathf.Clamp01(t0 / t1) : 0f;
            Vector2 e = Vector2.LerpUnclamped(_route[o], a, f);
            Vector2 g = Vector2.LerpUnclamped(a, ab, f);
            Vector2 h = Vector2.LerpUnclamped(ab, end, f);
            Vector2 eg = Vector2.LerpUnclamped(e, g, f);
            Vector2 gh = Vector2.LerpUnclamped(g, h, f);
            Vector2 start = Vector2.LerpUnclamped(eg, gh, f);
            float w0 = Mathf.Lerp(BeamBaseU, BeamHeadU, q0) * _u;
            float w1 = Mathf.Lerp(BeamBaseU, BeamHeadU, q1) * _u;
            BezierStripGraphic core = _cores[s];
            core.SetCurve(start, gh, h, end, w0, w1);
            core.SetColors(Color.Lerp(BeamTail, BeamHeadColor, q0), Color.Lerp(BeamTail, BeamHeadColor, q1), 0f, Color.clear, 0f);
            Color haloFrom = Color.Lerp(HaloTail, HaloHead, q0);
            Color haloTo = Color.Lerp(HaloTail, HaloHead, q1);
            for (int k = 0; k < BeamHalos; k++)
            {
                // Cada tira, más ancha que la anterior y afilada hacia la cabeza (la mitad del ensanche), donde la tapa el
                // destello.
                float halo = (k + 1) * BeamHaloStepU * _u;
                BezierStripGraphic strip = _halos[(s * BeamHalos) + k];
                strip.SetCurve(start, gh, h, end, w0 + (halo * Mathf.Lerp(1f, 0.5f, q0)), w1 + (halo * Mathf.Lerp(1f, 0.5f, q1)));
                strip.SetColors(haloFrom, haloTo, 0f, Color.clear, 0f);
            }
        }

        // Punto t de la Bézier cúbica route[o..o+3].
        private static Vector2 Bezier(Vector2[] route, int o, float t)
        {
            float s = 1f - t;
            return (s * s * s * route[o]) + (3f * s * s * t * route[o + 1]) + (3f * s * t * t * route[o + 2]) + (t * t * t * route[o + 3]);
        }

        // Adónde llega el hilo, en u desde el pie: la fruta, o la cuenta del medio en la Liana de cuenta.
        private Vector2 EndLocalU()
        {
            if (_key > 0)
            {
                return new Vector2(FruitCenterXU, CuiaGeometry.CavityTopU(Host.Capacity) + FruitCenterAboveCavityU);
            }

            int count = Mathf.Max(1, _n);
            Vector2 first = BeadU(0, count);
            Vector2 last = BeadU(count - 1, count);
            return 0.5f * (first + last);
        }

        /// <summary>Centro de la cuenta <paramref name="i"/> de <paramref name="count"/> de la Liana de cuenta (u desde el pie; tests).</summary>
        public Vector2 BeadCenterU(int i, int count)
        {
            return BeadU(i, count);
        }

        // Tira de luz aditiva sin toques, con el origen en el punto anchor de parent y sin tamaño: sus puntos son los de parent.
        private static BezierStripGraphic NewStrip(string name, RectTransform parent, Vector2 anchor, Material? additive)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(BezierStripGraphic));
            go.transform.SetParent(parent, false);
            BezierStripGraphic strip = go.GetComponent<BezierStripGraphic>();
            strip.Setup(BeamSegments);
            RectTransform rt = strip.rectTransform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            if (additive != null)
            {
                strip.material = additive;
            }

            return strip;
        }

        // Brillo aditivo (sprite Glow) con el origen en el centro de parent (la raíz del cometa: el espacio de la capa).
        private Image NewGlow(string name, RectTransform parent, Color color, Material? additive)
        {
            Image img = OverlayParts.NewImage(name, parent, Host.Art.Glow, OverlayParts.Fade(color, 0f));
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            if (additive != null)
            {
                img.material = additive;
            }

            return img;
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

        private static Color Shade(Color c, float k)
        {
            return new Color(c.r * k, c.g * k, c.b * k, 1f);
        }

        private static void SetScale(RectTransform rt, float x, float y)
        {
            var s = new Vector3(x, y, 1f);
            if (rt.localScale != s)
            {
                rt.localScale = s;
            }
        }

        private static void SetRotation(RectTransform rt, Quaternion r)
        {
            if (rt.localRotation != r)
            {
                rt.localRotation = r;
            }
        }

        private static void SetPos(RectTransform rt, Vector2 pos)
        {
            if (rt.anchoredPosition != pos)
            {
                rt.anchoredPosition = pos;
            }
        }

        private static void SetSize(RectTransform rt, float side)
        {
            var size = new Vector2(side, side);
            if (rt.sizeDelta != size)
            {
                rt.sizeDelta = size;
            }
        }

        /// <summary>
        /// Destruye el cometa, que vive en la capa delantera del tablero, cuando se destruye la cuia (cambio de nivel): así no
        /// queda huérfano en esa capa.
        /// </summary>
        private sealed class BeamOwner : MonoBehaviour
        {
            public GameObject? Beam;

            private void OnDestroy()
            {
                if (Beam == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    Destroy(Beam);
                }
                else
                {
                    DestroyImmediate(Beam);
                }
            }
        }
    }
}
