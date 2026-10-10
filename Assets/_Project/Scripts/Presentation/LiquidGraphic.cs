#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Líquido de una cuia en uGUI con el shader <c>AF/UI/Liquid</c> (GDD 08 §5.2; guía visual v2, 4.2): un polígono por
    /// dosis visible, con los datos por dosis en los canales de vértice (uGUI no admite <c>MaterialPropertyBlock</c>; así
    /// todas las cuias comparten material y lote). El rectángulo del gráfico es el del cuerpo de la cuia con el pivote en el
    /// pie: las coordenadas locales son <c>(x − 1) · u</c> e <c>y · u</c> en dp. La vista lo rellena con
    /// <see cref="SetDose"/>; solo se reconstruye cuando algo cambia (08 §5.4).
    /// </summary>
    /// <remarks>
    /// <para><b>Cavidad</b> (D-006, punto 3, como Magic Sort): 1,60 u de ancho (0,80 del exterior) y 1 u por dosis; la cara
    /// superior del líquido es una elipse de <see cref="CuiaGeometry.TopFaceU"/> (0,40 u) de alto y el ancho de la cavidad,
    /// con el color aclarado del menisco (<see cref="Palette.MeniscusLighten"/>), más clara que el tinte. Las fronteras entre
    /// dosis y el fondo son arcos con la flecha de su borde delantero (<see cref="CuiaGeometry.FaceSagU"/>, 0,20 u en el
    /// eje): en el eje todas las dosis miden 1 u, también la de arriba, como en Magic Sort. Por eso la malla de cada dosis
    /// baja <see cref="BottomExtraU"/> de capa bajo su frontera y el shader la muestra solo sobre su arco. La geometría
    /// llega al shader como globales que fija <see cref="Setup"/> desde <see cref="CuiaGeometry"/>: una sola fuente.</para>
    /// <para><b>Inclinada</b> (al verter; guía 6.4: "el líquido no rota"), de −92° a 92° (D-006: 92° − 9° × dosis que
    /// quedan): cada frontera entre dosis es una línea horizontal en pantalla, escrita en forma normal en las coordenadas de
    /// la cuia, <c>X · sen θ + y · cos θ = d</c> (X = x − 1; vale también cerca de 90°, donde la pendiente no existe), y su
    /// <c>d</c> sale del volumen (<see cref="LevelForVolume"/>): el líquido llena la silueta interior girada desde su punto
    /// más bajo. Si el volumen no cabe bajo el labio con ese ángulo, la superficie se recorta en el labio (lo que no cabe ya
    /// está cayendo por el chorro). El shader recorta a la silueta interior (cavidad, hombro y cuello). Mientras sale el
    /// chorro, una «lengua» de 0,12 u del tinte corre por la pared inferior desde la superficie hasta el labio
    /// (<see cref="SetTongue"/>).</para>
    /// <para>Canales: <c>TEXCOORD0</c> = (x, y local, y de capa, alto de la cuia); <c>TEXCOORD2.w</c> = patrón + 4 · dosis.
    /// La y de capa es la de la dosis sin inclinar (degradado, patrón, cara superior y superficie); la y local, la del
    /// vidrio (brillos y recorte).</para>
    /// <para><b>Aguas turbias</b> (07 §4.3; 08 §5.2; T15): una dosis oculta se pinta en el shader, no con sprites encima, para
    /// que la trama y el «?» sigan a su banda cuando la cuia se inclina. Con <see cref="SetDose(int, Color, float, int, MurkyLook, float)"/>
    /// en un modo turbio la banda lleva el patrón <see cref="MurkyPatternMode"/> (± el avance del revelado, ver
    /// <see cref="MurkyLook"/>) y la fila turbia del atlas (<see cref="ProceduralArt.MurkyPatternRow"/>) en
    /// <c>TEXCOORD3.a</c>; el shader pinta encima del pardo la trama diagonal al 25 % y el «?» marfil de 0,5 u, sin la
    /// mezcla fija del 15 % de los patrones de fruta. Al disolverse por la regla (<see cref="MurkyLook.Dissolve"/>) hay un
    /// fundido cruzado sin saltos: <c>TEXCOORD3.a</c> pasa a ser la fila de la fruta que se revela
    /// (<see cref="FruitRowCode"/>; la turbia la da <c>_MurkyRow</c>), la trama se va antes de que entre el patrón de la
    /// fruta (<see cref="MurkyHatchAmount"/> y <see cref="FruitPatternWeight"/>, espejos en CPU del shader) y la franja y el
    /// menisco de la fruta entran con su patrón; al final del Dissolve la banda es su dosis normal. Equivale a la máscara
    /// <c>_HiddenMask</c> (una banda por dosis, también no contiguas) y al color <c>_MurkyColor</c> = #8A7B6A
    /// (<see cref="Palette.Murky"/>, el color de la banda) de 08 §5.2, que no pueden ir como propiedades: uGUI no admite
    /// <c>MaterialPropertyBlock</c> por cuia y todas comparten material.
    /// La «?» va derecha en pantalla y centrada en la cuerda de su banda: su x a lo ancho de la pantalla, relativa al centro
    /// de la cuerda y en u de capa, va por vértice en <c>TEXCOORD1.w</c>, que en las bandas normales es el destello del
    /// sellado (una cuia con dosis ocultas nunca está sellada, R5). Con la banda más fina que 1 u (cuia inclinada), la «?»
    /// y la trama se encogen con ella y no salen de la banda.</para>
    /// </remarks>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LiquidGraphic : MaskableGraphic
    {
        /// <summary>
        /// Patrón de una banda turbia en <c>TEXCOORD2.w</c> (patrón + 4 · dosis): 2 sin animar; 2 + 0,45 · avance al
        /// disolverse por la regla y 2 − 0,45 · avance al girar con la Luciérnaga (el shader decodifica el modo con
        /// <c>floor((w + 1,5) / 4)</c>, así que el patrón vive en [−1, 2,5)).
        /// </summary>
        public const float MurkyPatternMode = 2f;

        /// <summary>Lo que se aparta del <see cref="MurkyPatternMode"/> el avance 1 de un revelado (ver la constante).</summary>
        public const float MurkyProgressSpan = 0.45f;

        /// <summary>Avance de <see cref="MurkyLook.Dissolve"/> en el que el «?» empieza a apagarse (shader: smoothstep(0,35; 0,85)).</summary>
        public const float MarkFadeFrom = 0.35f;

        /// <summary>Avance de <see cref="MurkyLook.Dissolve"/> en el que el «?» ya no se ve.</summary>
        public const float MarkFadeTo = 0.85f;

        /// <summary>Trama diagonal de una dosis turbia: 25 % hacia la tinta (07 §4.3).</summary>
        public const float MurkyHatchOpacity = 0.25f;

        /// <summary>
        /// Avance de <see cref="MurkyLook.Dissolve"/> en el que la trama empieza a irse (shader: smoothstep(0,18; 0,55)) [P]:
        /// la trama se va mientras la dosis toma su tinte y antes de que entre el patrón de su fruta.
        /// </summary>
        public const float HatchFadeFrom = 0.18f;

        /// <summary>Avance de <see cref="MurkyLook.Dissolve"/> en el que la trama ya no se ve.</summary>
        public const float HatchFadeTo = 0.55f;

        /// <summary>
        /// Avance de <see cref="MurkyLook.Dissolve"/> en el que el patrón de la fruta que se revela empieza a entrar (shader:
        /// smoothstep(0,64; 1)) [P]: después de <see cref="HatchFadeTo"/> (la misma muestra lee la trama o el patrón, nunca los
        /// dos) y entero al final del Dissolve, así que el paso a la dosis normal no salta.
        /// </summary>
        public const float FruitPatternFrom = 0.64f;

        /// <summary>
        /// Cómo pinta el shader una dosis (07 §4.3; 08 §5.2): normal o turbia, y en este caso si su «?» está quieto, se
        /// disuelve (revelado por la regla, 1.200 ms) o gira de canto (Luciérnaga, 04 §2.5).
        /// </summary>
        public enum MurkyLook : byte
        {
            /// <summary>Dosis normal: su color, su tinte y el patrón de su fruta.</summary>
            None = 0,

            /// <summary>Oculta: pardo opaco, trama diagonal al 25 % y «?» marfil de 0,5 u, sin tinte ni patrón de fruta.</summary>
            Hidden = 1,

            /// <summary>
            /// Revelado por la regla (R7), avance 0..1: la trama se va (<see cref="HatchFadeFrom"/>-<see cref="HatchFadeTo"/>),
            /// el «?» crece, ondula y se disuelve (<see cref="MarkFadeFrom"/>-<see cref="MarkFadeTo"/>) y entra el patrón de la
            /// fruta que se revela (<see cref="FruitPatternFrom"/>-1), con su franja y su menisco: al avance 1 es su dosis normal.
            /// </summary>
            Dissolve = 2,

            /// <summary>Revelado con la Luciérnaga: el «?» gira hasta quedar de canto y la trama se va (avance 0..1).</summary>
            Turn = 3,
        }
        /// <summary>Amplitud inicial de la onda tras un vertido (08 §5.2): 0,03 u.</summary>
        public const float RippleAmplitudeU = 0.03f;

        /// <summary>Constante de caída de la onda (08 §5.2): exp(−t / 0,25 s).</summary>
        public const float RippleDecayMs = 250f;

        /// <summary>Por debajo de esta amplitud (u) la onda ya no se ve y la superficie queda quieta.</summary>
        public const float RippleSettledU = 0.0008f;

        /// <summary>Grosor de la lengua de líquido por la pared inferior del cuello mientras sale el chorro (u).</summary>
        public const float TongueU = 0.12f;

        /// <summary>Inclinación máxima que se dibuja (grados): 92° con el origen vacío (D-006) y algo de margen.</summary>
        public const float MaxTiltDeg = 95f;

        /// <summary>
        /// Lo que baja la malla de cada dosis bajo su frontera (u de capa): la flecha de los arcos en el eje, la mitad de la
        /// cara superior (<see cref="CuiaGeometry.FaceSagU"/>).
        /// </summary>
        public const float BottomExtraU = 0.5f * CuiaGeometry.TopFaceU;

        // La dosis superior sube la mitad de la cara superior más la onda y un margen, para que quepa la elipse.
        private const float TopExtraU = (0.5f * CuiaGeometry.TopFaceU) + RippleAmplitudeU + 0.05f;
        private const int ShoulderSamples = 16;
        private const int BisectSteps = 24;
        private const int TongueSamples = 48;
        private const int MaxCapacity = 8;

        private static readonly int DpUId = Shader.PropertyToID("_DpU");
        private static readonly int PatternRowsId = Shader.PropertyToID("_PatternRows");
        private static readonly int MurkyRowId = Shader.PropertyToID("_MurkyRow");
        private static readonly int InkId = Shader.PropertyToID("_InkLin");
        private static readonly int PaperId = Shader.PropertyToID("_PaperLin");
        private static readonly int CavityBottomId = Shader.PropertyToID("_AFCavityBottomU");
        private static readonly int CavityHalfId = Shader.PropertyToID("_AFCavityHalfU");
        private static readonly int NeckHalfId = Shader.PropertyToID("_AFNeckHalfU");
        private static readonly int ShoulderId = Shader.PropertyToID("_AFShoulderU");
        private static readonly int AboveCavityId = Shader.PropertyToID("_AFAboveCavityU");
        private static readonly int RimId = Shader.PropertyToID("_AFRimU");
        private static readonly int TopFaceId = Shader.PropertyToID("_AFTopFaceU");

        // Silueta interior (cavidad, hombro y cuello hasta el labio) por capacidad, en X = x − 1 e y (u): se crea una vez.
        private static readonly Vector2[]?[] s_silhouettes = new Vector2[MaxCapacity + 1][];

        // Polígono de una banda: el rectángulo de la cavidad recortado por dos semiplanos (≤ 6 vértices) y un búfer.
        private readonly Vector2[] _band = new Vector2[8];
        private readonly Vector2[] _bandTmp = new Vector2[8];
        private Color[] _color = Array.Empty<Color>();
        private float[] _fill = Array.Empty<float>();
        private int[] _tint = Array.Empty<int>();
        private MurkyLook[] _look = Array.Empty<MurkyLook>();
        private float[] _lookProgress = Array.Empty<float>();
        private float[] _levels = Array.Empty<float>();
        private Texture? _atlas;
        private int _capacity;
        private float _unit = 24f;
        private float _rippleMs = -1f;
        private float _ripplePhase;
        private bool _rippleDriven;
        private float _flash;
        private float _angle;
        private float _tongue;
        private Color _tongueColor;
        private int _tongueTint;

        /// <inheritdoc />
        public override Texture mainTexture => _atlas != null ? _atlas : s_WhiteTexture;

        /// <summary>Onda en curso.</summary>
        public bool Rippling => _rippleMs >= 0f;

        /// <summary>Edad de la onda en curso (ms; −1 sin onda).</summary>
        public float RippleAgeMs => _rippleMs;

        /// <summary>Fase de la onda (radianes).</summary>
        public float RipplePhase => _ripplePhase;

        /// <summary>La onda la lleva un reloj de fuera (<see cref="SetRippleAt"/>: la intro) y no avanza con <see cref="TickRipple"/>.</summary>
        public bool RippleDriven => _rippleDriven;

        /// <summary>Inclinación del cuerpo con la que se dibujan las capas (grados; 0 en reposo).</summary>
        public float TiltDeg => _angle;

        /// <summary>Lengua de líquido visible (0..1).</summary>
        public float Tongue => _tongue;

        /// <summary>
        /// Fija en el shader <c>AF/UI/Liquid</c> la geometría de la cuia como globales, desde <see cref="CuiaGeometry"/>:
        /// fondo y medio ancho de la cavidad, medio ancho del cuello, hombro, alto sobre la cavidad, borde y cara superior.
        /// </summary>
        public static void ApplyShaderGeometry()
        {
            Shader.SetGlobalFloat(CavityBottomId, CuiaGeometry.CavityBottomU);
            Shader.SetGlobalFloat(CavityHalfId, CuiaGeometry.CavityHalfWidthU);
            Shader.SetGlobalFloat(NeckHalfId, CuiaGeometry.NeckInnerHalfWidthU);
            Shader.SetGlobalFloat(ShoulderId, CuiaGeometry.ShoulderU);
            Shader.SetGlobalFloat(AboveCavityId, CuiaGeometry.AboveCavityU);
            Shader.SetGlobalFloat(RimId, CuiaGeometry.RimU);
            Shader.SetGlobalFloat(TopFaceId, CuiaGeometry.TopFaceU);
        }

        /// <summary>Prepara el gráfico para <paramref name="capacity"/> dosis con el atlas de patrones.</summary>
        public void Setup(int capacity, Texture atlas)
        {
            _capacity = capacity;
            _color = new Color[capacity];
            _fill = new float[capacity];
            _tint = new int[capacity];
            _look = new MurkyLook[capacity];
            _lookProgress = new float[capacity];
            _levels = new float[capacity + 1];
            _atlas = atlas;
            raycastTarget = false;
            ApplyShaderGeometry();
            Material? mat = UiMaterials.Liquid;
            if (mat != null)
            {
                material = mat;
                mat.SetFloat(PatternRowsId, atlas.height / (float)ProceduralArt.PatternRowPx);
                mat.SetFloat(MurkyRowId, ProceduralArt.MurkyPatternRow); // la trama y el «?» (al disolverse, TEXCOORD3.a es la fruta)
                // Ya en el espacio del shader: van con SetVector, que no convierte. SetColor los volvía a pasar a lineal en
                // el proyecto Linear (doble conversión): el marfil llegaba como #E7D4B3 en vez de #F4EBDA (el «?» de las
                // turbias y las marcas claras de los patrones) y la tinta casi negra, lejos de Palette.PatternMark.
                Vector3 ink = ColorMath.ToShaderSpace(Palette.UiInk); // marcas sobre los tintes claros y halo del «?»
                Vector3 paper = ColorMath.ToShaderSpace(Palette.UiPaper); // marcas sobre los tintes oscuros y «?» turbio
                mat.SetVector(InkId, new Vector4(ink.x, ink.y, ink.z, 1f));
                mat.SetVector(PaperId, new Vector4(paper.x, paper.y, paper.z, 1f));
            }

            SetVerticesDirty();
            SetMaterialDirty();
        }

        /// <summary>
        /// Unidad u en dp (ancho de la cuia / 2). Todas las cuias de un nivel miden lo mismo (02 §5), así que 1 dp en u va
        /// en el material compartido.
        /// </summary>
        public void SetUnit(float unitDp)
        {
            if (!Mathf.Approximately(unitDp, _unit))
            {
                _unit = unitDp;
                SetVerticesDirty();
            }

            Material? mat = UiMaterials.Liquid;
            if (mat != null && unitDp > 0f)
            {
                mat.SetFloat(DpUId, 1f / unitDp);
            }
        }

        /// <summary>Fija la dosis <paramref name="k"/> (color, relleno 0..1, tinte; 0 = agua base), sin modo turbio.</summary>
        public void SetDose(int k, Color color, float fill, int tint)
        {
            SetDose(k, color, fill, tint, MurkyLook.None, 0f);
        }

        /// <summary>
        /// Fija la dosis <paramref name="k"/> con el modo turbio <paramref name="look"/> (07 §4.3; ver <see cref="MurkyLook"/>):
        /// oculta o girando, el tinte no se usa (va 0: ni su franja, ni su menisco, ni su patrón delatan la fruta oculta); al
        /// disolverse por la regla, <paramref name="tint"/> es el de la fruta que se revela, cuyo patrón, franja y menisco
        /// entran con <see cref="FruitPatternWeight"/>. <paramref name="progress"/> (0..1) es el avance de
        /// <see cref="MurkyLook.Dissolve"/> o <see cref="MurkyLook.Turn"/>. Solo reconstruye la malla si algo cambia; sin
        /// asignaciones.
        /// </summary>
        public void SetDose(int k, Color color, float fill, int tint, MurkyLook look, float progress = 0f)
        {
            if ((uint)k >= (uint)_capacity)
            {
                return;
            }

            fill = Mathf.Clamp01(fill);
            if (look == MurkyLook.None || look == MurkyLook.Hidden)
            {
                progress = 0f;
            }
            else
            {
                progress = Mathf.Clamp01(progress);
            }

            if (look == MurkyLook.Hidden || look == MurkyLook.Turn)
            {
                tint = 0;
            }

            if (_color[k] == color && Mathf.Approximately(_fill[k], fill) && _tint[k] == tint && _look[k] == look && Mathf.Approximately(_lookProgress[k], progress))
            {
                return;
            }

            _color[k] = color;
            _fill[k] = fill;
            _tint[k] = tint;
            _look[k] = look;
            _lookProgress[k] = progress;
            SetVerticesDirty();
        }

        /// <summary>Modo turbio de la dosis <paramref name="k"/> (<see cref="MurkyLook.None"/> fuera de rango).</summary>
        public MurkyLook MurkyLookAt(int k)
        {
            return (uint)k < (uint)_capacity ? _look[k] : MurkyLook.None;
        }

        /// <summary>Avance (0..1) del revelado de la dosis <paramref name="k"/> en <see cref="MurkyLook.Dissolve"/> o <see cref="MurkyLook.Turn"/>.</summary>
        public float MurkyProgressAt(int k)
        {
            return (uint)k < (uint)_capacity ? _lookProgress[k] : 0f;
        }

        /// <summary>Color con el que se pinta la dosis <paramref name="k"/> (el del último <c>SetDose</c>; transparente fuera de rango).</summary>
        public Color DoseColorAt(int k)
        {
            return (uint)k < (uint)_capacity ? _color[k] : Color.clear;
        }

        /// <summary>
        /// Opacidad (0..1) del «?» que pinta el shader en una banda turbia con <paramref name="look"/> y
        /// <paramref name="progress"/>: el mismo cálculo que <c>AF_UI_Liquid.shader</c> («fade»). Quieta, 1; al disolverse
        /// por la regla, 1 − smoothstep(<see cref="MarkFadeFrom"/>, <see cref="MarkFadeTo"/>, avance); al girar con la
        /// Luciérnaga, 1 hasta quedar de canto (lo que se ve lo estrecha el giro); 0 en una dosis normal.
        /// </summary>
        public static float MurkyMarkOpacity(MurkyLook look, float progress)
        {
            switch (look)
            {
                case MurkyLook.Hidden:
                    return 1f;
                case MurkyLook.Dissolve:
                    return 1f - SmoothStep(MarkFadeFrom, MarkFadeTo, progress);
                case MurkyLook.Turn:
                    return progress < 0.999f ? 1f : 0f;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Opacidad de la trama diagonal (hacia la tinta) en una banda con <paramref name="look"/> y <paramref name="progress"/>:
        /// el mismo cálculo que <c>AF_UI_Liquid.shader</c> («hatch»). Quieta, <see cref="MurkyHatchOpacity"/>; al disolverse,
        /// baja a 0 entre <see cref="HatchFadeFrom"/> y <see cref="HatchFadeTo"/>; al girar, entre 0,3 y 1; 0 en una dosis normal.
        /// </summary>
        public static float MurkyHatchAmount(MurkyLook look, float progress)
        {
            switch (look)
            {
                case MurkyLook.Hidden:
                    return MurkyHatchOpacity;
                case MurkyLook.Dissolve:
                    return MurkyHatchOpacity * (1f - SmoothStep(HatchFadeFrom, HatchFadeTo, progress));
                case MurkyLook.Turn:
                    return MurkyHatchOpacity * (1f - SmoothStep(0.3f, 1f, progress));
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Peso (0..1) del patrón, la franja y el menisco propios de la fruta en una banda con <paramref name="look"/> y
        /// <paramref name="progress"/> (el mismo cálculo que el shader): 1 en una dosis normal; al disolverse por la
        /// regla, sube entre <see cref="FruitPatternFrom"/> y 1 (con 1, la banda es la dosis normal de esa fruta); 0 oculta o
        /// girando.
        /// </summary>
        public static float FruitPatternWeight(MurkyLook look, float progress)
        {
            switch (look)
            {
                case MurkyLook.None:
                    return 1f;
                case MurkyLook.Dissolve:
                    return SmoothStep(FruitPatternFrom, 1f, progress);
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Valor de <c>TEXCOORD3.a</c> de una banda que se disuelve hacia la fruta <paramref name="tint"/>: su fila del atlas
        /// (0, vacía, si no tiene patrón: Guanábana, agua base o Barro) + 0,5 si sus marcas van en marfil
        /// (<see cref="Palette.PatternBase"/>). El shader lo decodifica con <c>floor(a + 0,25)</c>.
        /// </summary>
        public static float FruitRowCode(int tint)
        {
            if (!ProceduralArt.HasPattern(tint))
            {
                return 0f;
            }

            return tint + (Palette.PatternBase(tint) == Palette.UiPaper ? 0.5f : 0f);
        }

        /// <summary>
        /// El shader dibuja el «?» de la dosis <paramref name="k"/>: turbia y sin haber terminado de disolverse (opacidad
        /// <see cref="MurkyMarkOpacity"/> por encima de 0,001) o de girar.
        /// </summary>
        public bool MurkyMarkDrawn(int k)
        {
            if ((uint)k >= (uint)_capacity || _look[k] == MurkyLook.None || _fill[k] <= 0.001f || _color[k].a <= 0.001f)
            {
                return false;
            }

            return MurkyMarkOpacity(_look[k], _lookProgress[k]) > 0.001f;
        }

        /// <summary>Valor de <c>TEXCOORD2.w</c> − 4 · dosis de una banda turbia con <paramref name="look"/> y <paramref name="progress"/>.</summary>
        public static float MurkyPattern(MurkyLook look, float progress)
        {
            switch (look)
            {
                case MurkyLook.Dissolve:
                    return MurkyPatternMode + (MurkyProgressSpan * Mathf.Clamp01(progress));
                case MurkyLook.Turn:
                    return MurkyPatternMode - (MurkyProgressSpan * Mathf.Clamp01(progress));
                default:
                    return MurkyPatternMode;
            }
        }

        /// <summary>
        /// Inclinación del cuerpo (grados, de −92° a 92° y algo más; guía 6.4: "el líquido no rota"): las fronteras entre
        /// dosis siguen horizontales en pantalla y su posición sale del volumen de cada dosis dentro de la silueta girada.
        /// Solo cambia al verter.
        /// </summary>
        public void SetTilt(float angleDeg)
        {
            float angle = Mathf.Clamp(angleDeg, -MaxTiltDeg, MaxTiltDeg);
            if (Mathf.Abs(angle - _angle) > 0.02f || (angle == 0f && _angle != 0f))
            {
                _angle = angle;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Lengua del chorro (0..1): una franja de <see cref="TongueU"/> del tinte <paramref name="tint"/> por la pared
        /// inferior desde la superficie hasta el labio, solo con la cuia inclinada.
        /// </summary>
        public void SetTongue(float amount, Color color, int tint)
        {
            amount = Mathf.Clamp01(amount);
            if (Mathf.Abs(amount - _tongue) < 0.001f && color == _tongueColor && tint == _tongueTint)
            {
                return;
            }

            _tongue = amount;
            _tongueColor = color;
            _tongueTint = tint;
            SetVerticesDirty();
        }

        /// <summary>Destello del sellado: 0 a 0,15 (07 §5.3).</summary>
        public void SetFlash(float flash)
        {
            if (!Mathf.Approximately(flash, _flash))
            {
                _flash = flash;
                SetVerticesDirty();
            }
        }

        /// <summary>Arranca la onda de la superficie (al caer una dosis); manda sobre una onda llevada desde fuera.</summary>
        public void StartRipple()
        {
            _rippleDriven = false;
            _rippleMs = 0f;
            _ripplePhase = Mathf.Repeat(_ripplePhase + 2.3f, Mathf.PI * 2f);
            SetVerticesDirty();
        }

        /// <summary>Quita la onda (reposo: la superficie está quieta).</summary>
        public void StopRipple()
        {
            _rippleDriven = false;
            if (_rippleMs >= 0f)
            {
                _rippleMs = -1f;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Fija la onda desde un reloj de fuera (la intro, A14): edad <paramref name="ageMs"/> (&lt; 0, o ya imperceptible: sin
        /// onda) y fase <paramref name="phase"/>. No acumula nada: el mismo instante da la misma superficie. Mientras la lleva el
        /// reloj de fuera, <see cref="TickRipple"/> no la avanza; <see cref="ReleaseRipple"/> o una onda nueva la devuelven.
        /// </summary>
        public void SetRippleAt(float ageMs, float phase)
        {
            _rippleDriven = true;
            float ms = ageMs >= 0f && RippleAmplitudeU * Mathf.Exp(-ageMs / RippleDecayMs) >= RippleSettledU ? ageMs : -1f;
            bool changed = ms != _rippleMs || (ms >= 0f && phase != _ripplePhase);
            _rippleMs = ms;
            _ripplePhase = phase;
            if (changed)
            {
                SetVerticesDirty();
            }
        }

        /// <summary>Devuelve la onda al reloj del juego: sigue decayendo desde su edad con <see cref="TickRipple"/>.</summary>
        public void ReleaseRipple()
        {
            _rippleDriven = false;
        }

        /// <summary>Avanza la onda; solo reconstruye la malla mientras dura (no si la lleva un reloj de fuera).</summary>
        public void TickRipple(float dtMs)
        {
            if (_rippleMs < 0f || _rippleDriven)
            {
                return;
            }

            _rippleMs += dtMs;
            if (RippleAmplitude() < RippleSettledU)
            {
                _rippleMs = -1f;
            }

            SetVerticesDirty();
        }

        /// <summary>
        /// Posición <c>d</c> de una línea horizontal en pantalla dentro de la cuia girada <paramref name="angleDeg"/> grados
        /// (sentido antihorario): <c>d = X · sen θ + y · cos θ</c> en las coordenadas de la cuia (X = x − 1, en u). Crece
        /// hacia arriba de la pantalla; sin inclinar, <c>d = y</c>.
        /// </summary>
        public static float LineOffset(float xRel, float y, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return (xRel * Mathf.Sin(rad)) + (y * Mathf.Cos(rad));
        }

        /// <summary>
        /// Área (u²) de la silueta interior de una cuia de <paramref name="capacity"/> dosis (cavidad, hombro y cuello hasta
        /// el labio) que queda por debajo, en pantalla, de la línea horizontal <c>d</c> con la cuia girada
        /// <paramref name="angleDeg"/> grados (ver <see cref="LineOffset"/>). Exacta sobre el polígono de la silueta (el
        /// hombro, con 16 tramos). Sin asignaciones tras la primera llamada por capacidad.
        /// </summary>
        public static float AreaBelow(float d, float angleDeg, int capacity)
        {
            Vector2[] poly = Silhouette(capacity);
            float rad = angleDeg * Mathf.Deg2Rad;
            float sn = Mathf.Sin(rad);
            float cs = Mathf.Cos(rad);
            int n = poly.Length;
            bool has = false;
            Vector2 first = default;
            Vector2 prev = default;
            float sum = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = poly[i];
                Vector2 q = poly[(i + 1) % n];
                float fp = (p.x * sn) + (p.y * cs) - d;
                float fq = (q.x * sn) + (q.y * cs) - d;
                if (fp <= 0f)
                {
                    Accumulate(p, ref has, ref first, ref prev, ref sum);
                }

                if ((fp < 0f && fq > 0f) || (fp > 0f && fq < 0f))
                {
                    float t = fp / (fp - fq);
                    Accumulate(p + ((q - p) * t), ref has, ref first, ref prev, ref sum);
                }
            }

            if (!has)
            {
                return 0f;
            }

            sum += (prev.x * first.y) - (first.x * prev.y);
            return 0.5f * Mathf.Abs(sum);
        }

        /// <summary>
        /// Posición <c>d</c> del labio en pantalla con la cuia girada <paramref name="angleDeg"/> grados: la del canto más
        /// bajo de la boca interior (<see cref="CuiaGeometry.NeckInnerHalfWidthU"/> en <see cref="CuiaGeometry.LipU"/>). El
        /// líquido no sube de ahí: lo que no cabe ya está cayendo.
        /// </summary>
        public static float LipLevel(float angleDeg, int capacity)
        {
            float lip = CuiaGeometry.LipU(capacity);
            float w = CuiaGeometry.NeckInnerHalfWidthU;
            return Mathf.Min(LineOffset(-w, lip, angleDeg), LineOffset(w, lip, angleDeg));
        }

        /// <summary>
        /// Posición <c>d</c> (ver <see cref="LineOffset"/>) de la superficie horizontal que deja <paramref name="volume"/> u²
        /// de líquido por debajo, llenando desde el punto más bajo de la silueta girada (bisección sobre
        /// <see cref="AreaBelow"/>), recortada en el labio (<see cref="LipLevel"/>) si no cabe. Sin inclinar es la altura de
        /// la superficie. Monótona en el volumen; sin NaN de −92° a 92°.
        /// </summary>
        public static float LevelForVolume(float volume, float angleDeg, int capacity)
        {
            float bottom = CuiaGeometry.CavityBottomU;
            float lipLevel = LipLevel(angleDeg, capacity);
            if (Mathf.Abs(angleDeg) < 1e-3f)
            {
                return Mathf.Min(bottom + (Mathf.Max(0f, volume) / (2f * CuiaGeometry.CavityHalfWidthU)), lipLevel);
            }

            Vector2[] poly = Silhouette(capacity);
            float lo = float.MaxValue;
            float hi = float.MinValue;
            for (int i = 0; i < poly.Length; i++)
            {
                float d = LineOffset(poly[i].x, poly[i].y, angleDeg);
                lo = Mathf.Min(lo, d);
                hi = Mathf.Max(hi, d);
            }

            if (volume <= 0f)
            {
                return Mathf.Min(lo, lipLevel);
            }

            for (int i = 0; i < BisectSteps; i++)
            {
                float mid = 0.5f * (lo + hi);
                if (AreaBelow(mid, angleDeg, capacity) < volume)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            return Mathf.Min(0.5f * (lo + hi), lipLevel);
        }

        /// <inheritdoc />
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int top = -1;
            for (int k = 0; k < _capacity; k++)
            {
                if (_fill[k] > 0.001f && _color[k].a > 0.001f)
                {
                    top = k;
                }
            }

            if (_unit <= 0f || _capacity <= 0 || top < 0)
            {
                return;
            }

            float heightU = CuiaGeometry.HeightU(_capacity);
            float bottomU = CuiaGeometry.CavityBottomU;
            float amp = RippleAmplitude();
            float width = 2f * CuiaGeometry.CavityHalfWidthU;
            bool tilted = Mathf.Abs(_angle) > 0.01f;
            float surface = bottomU + top + _fill[top];
            int topTint = _tint[top];
            float lighten = topTint > 0 ? Palette.MeniscusLighten(topTint) : 0.20f;
            if (_look[top] == MurkyLook.Dissolve)
            {
                lighten = Mathf.Lerp(0.20f, lighten, FruitPatternWeight(MurkyLook.Dissolve, _lookProgress[top])); // el de su fruta entra con su patrón
            }

            Color men = ColorMath.Lighten(_color[top], lighten);
            Vector3 menS = ColorMath.ToShaderSpace(men);

            // Fronteras por volumen (forma normal, ver LineOffset): _levels[k] es la línea bajo la dosis k (k = 0, el fondo)
            // y _levels[k + 1] la de encima. Sin inclinar son alturas: bottom + suma de rellenos.
            float volume = 0f;
            _levels[0] = tilted ? LevelForVolume(0f, _angle, _capacity) : bottomU;
            for (int k = 0; k <= top; k++)
            {
                volume += _fill[k] * width;
                _levels[k + 1] = tilted ? LevelForVolume(volume, _angle, _capacity) : _levels[k] + _fill[k];
            }

            for (int k = 0; k <= top; k++)
            {
                float fill = _fill[k];
                Color c = _color[k];
                if (fill <= 0.001f || c.a <= 0.001f)
                {
                    continue;
                }

                float lo = _levels[k];
                float hi = _levels[k + 1];
                if (hi - lo < 1e-5f)
                {
                    continue; // dosis que ya no cabe bajo el labio: está cayendo por el chorro
                }

                int tint = _tint[k];
                Color lit = tint > 0 ? Palette.LiquidHighlight(tint, c) : ColorMath.AddLabLightness(c, 10f);
                bool murky = _look[k] != MurkyLook.None;
                bool dissolving = _look[k] == MurkyLook.Dissolve;
                if (dissolving && tint > 0)
                {
                    // La franja de la fruta que se revela entra con su patrón: al final del Dissolve es la de su dosis normal.
                    lit = Color.Lerp(ColorMath.AddLabLightness(c, 10f), lit, FruitPatternWeight(MurkyLook.Dissolve, _lookProgress[k]));
                }

                Vector3 litS = ColorMath.ToShaderSpace(lit);
                float pattern = -1f; // sin patrón (Guanábana, agua base)
                float row = tint;
                if (murky)
                {
                    // Oculta o girando, la fila turbia (nada de su fruta); al disolverse, la de la fruta que se revela (su
                    // patrón entra en el shader cuando ya se fue la trama, que el shader lee de _MurkyRow).
                    pattern = MurkyPattern(_look[k], _lookProgress[k]);
                    row = dissolving ? FruitRowCode(tint) : ProceduralArt.MurkyPatternRow;
                }
                else if (ProceduralArt.HasPattern(tint))
                {
                    pattern = Palette.PatternBase(tint) == Palette.UiPaper ? 1f : 0f;
                }

                var uv1 = new Vector4(litS.x, litS.y, litS.z, _flash);
                var uv2 = new Vector4(surface, amp, _ripplePhase, pattern + (4f * k));
                var uv3 = new Vector4(menS.x, menS.y, menS.z, row);

                // La dosis superior sube TopExtraU de capa para la cara superior y la onda, y todas bajan BottomExtraU para
                // su frontera en arco (en d, con la escala de la banda).
                float hiMesh = k == top ? hi + (TopExtraU * (hi - lo) / fill) : hi;
                float loMesh = lo - (BottomExtraU * (hi - lo) / fill);
                AddBand(vh, lo, hi, loMesh, hiMesh, bottomU + k, fill, heightU, c, uv1, uv2, uv3, murky);
            }

            if (tilted && _tongue > 0.01f)
            {
                AddTongue(vh, heightU, _levels[top + 1]);
            }
        }

        // Banda entre las líneas loMesh y hiMesh (forma normal): el rectángulo de la cavidad [−w, w] × [fondo en el eje,
        // labio] recortado por los dos semiplanos (≤ 6 vértices, convexo) y dibujado en abanico. El shader recorta luego a
        // la silueta interior (con el fondo en arco) y a la frontera en arco. La y de capa es afín en d: fondo de la dosis +
        // relleno · (d − lo) / (hi − lo). En una banda turbia, uv1.w de cada vértice es la x de la «?» (ver la clase): la
        // coordenada horizontal de pantalla e = X · cos θ − y · sen θ, relativa al centro de la cuerda de la banda a media
        // altura y con la escala de la banda (u de capa por u de pantalla), así que es afín y se interpola exacta. Sin
        // asignaciones.
        private void AddBand(VertexHelper vh, float lo, float hi, float loMesh, float hiMesh, float doseBottom, float fill, float heightU, Color c, Vector4 uv1, Vector4 uv2, Vector4 uv3, bool murky)
        {
            float w = CuiaGeometry.CavityHalfWidthU;
            float yMin = CuiaGeometry.CavityFloorU(0f);
            float yMax = CuiaGeometry.LipU(_capacity);
            _band[0] = new Vector2(-w, yMin);
            _band[1] = new Vector2(w, yMin);
            _band[2] = new Vector2(w, yMax);
            _band[3] = new Vector2(-w, yMax);
            int n = ClipHalfPlane(_band, 4, _bandTmp, loMesh, -1f); // d ≥ loMesh
            n = ClipHalfPlane(_bandTmp, n, _band, hiMesh, 1f); // d ≤ hiMesh
            if (n < 3)
            {
                return;
            }

            float scale = fill / Mathf.Max(1e-5f, hi - lo);
            float rad = _angle * Mathf.Deg2Rad;
            float sn = Mathf.Sin(rad);
            float cs = Mathf.Cos(rad);
            float chord = murky ? ChordCenter(0.5f * (lo + hi), sn, cs) : 0f;
            Color32 vc = c;
            int i0 = vh.currentVertCount;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = _band[i];
                float d = LineOffset(p.x, p.y, _angle);
                if (murky)
                {
                    uv1.w = scale * (((p.x * cs) - (p.y * sn)) - chord);
                }

                AddVert(vh, p.x, p.y, doseBottom + ((d - lo) * scale), heightU, vc, uv1, uv2, uv3);
            }

            for (int i = 1; i + 1 < n; i++)
            {
                vh.AddTriangle(i0, i0 + i, i0 + i + 1);
            }
        }

        /// <summary>
        /// Centro, en la coordenada horizontal de pantalla <c>e = X · cos θ − y · sen θ</c> (u; sin inclinar, <c>e = X</c>), de
        /// la cuerda que la línea horizontal <c>d</c> (ver <see cref="LineOffset"/>) corta en la silueta interior de una cuia de
        /// <paramref name="capacity"/> dosis girada <paramref name="angleDeg"/> grados. Es donde se centra la «?» de una banda
        /// turbia (a media altura de la banda). 0 si la línea no corta la silueta. Sin asignaciones tras la primera llamada por
        /// capacidad.
        /// </summary>
        public static float ChordCenter(float d, float angleDeg, int capacity)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return ChordCenter(Silhouette(capacity), d, Mathf.Sin(rad), Mathf.Cos(rad));
        }

        private float ChordCenter(float d, float sn, float cs)
        {
            return ChordCenter(Silhouette(_capacity), d, sn, cs);
        }

        private static float ChordCenter(Vector2[] poly, float d, float sn, float cs)
        {
            int n = poly.Length;
            float eMin = float.MaxValue;
            float eMax = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = poly[i];
                Vector2 q = poly[(i + 1) % n];
                float fp = (p.x * sn) + (p.y * cs) - d;
                float fq = (q.x * sn) + (q.y * cs) - d;
                if ((fp <= 0f && fq > 0f) || (fp > 0f && fq <= 0f))
                {
                    Vector2 r = p + ((q - p) * (fp / (fp - fq)));
                    float e = (r.x * cs) - (r.y * sn);
                    eMin = Mathf.Min(eMin, e);
                    eMax = Mathf.Max(eMax, e);
                }
            }

            return eMin <= eMax ? 0.5f * (eMin + eMax) : 0f;
        }

        // Sutherland-Hodgman con un semiplano: conserva los puntos con sign · (d(p) − limit) ≤ 0. Devuelve los vértices
        // escritos en dst (≤ n + 1).
        private int ClipHalfPlane(Vector2[] src, int n, Vector2[] dst, float limit, float sign)
        {
            int m = 0;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = src[i];
                Vector2 q = src[(i + 1) % n];
                float fp = sign * (LineOffset(p.x, p.y, _angle) - limit);
                float fq = sign * (LineOffset(q.x, q.y, _angle) - limit);
                if (fp <= 0f)
                {
                    dst[m++] = p;
                }

                if ((fp < 0f && fq > 0f) || (fp > 0f && fq < 0f))
                {
                    dst[m++] = p + ((q - p) * (fp / (fp - fq)));
                }
            }

            return m;
        }

        // Lengua del chorro: franja de 0,12 u por la pared interior del lado bajo (el que mira al destino), desde donde la
        // superficie toca esa pared hasta el labio. Color de la dosis que se vacía, sin patrón ni cara superior. El punto
        // de contacto se busca bajando por la pared desde el labio (vale a cualquier ángulo).
        private void AddTongue(VertexHelper vh, float heightU, float surfaceD)
        {
            float side = _angle > 0f ? -1f : 1f; // lado bajo: X < 0 si el cuerpo gira en sentido antihorario
            float lip = CuiaGeometry.LipU(_capacity);
            float bottom = CuiaGeometry.CavityBottomU;
            if (LineOffset(side * CuiaGeometry.NeckInnerHalfWidthU, lip, _angle) <= surfaceD)
            {
                return; // el líquido ya llega al labio
            }

            float contact = -1f;
            float prevY = lip;
            float prevF = LineOffset(side * CuiaGeometry.InnerHalfWidth(lip, _capacity), lip, _angle) - surfaceD;
            for (int i = 1; i <= TongueSamples; i++)
            {
                float y = Mathf.Lerp(lip, bottom, i / (float)TongueSamples);
                float f = LineOffset(side * CuiaGeometry.InnerHalfWidth(Mathf.Max(y, bottom), _capacity), y, _angle) - surfaceD;
                if (f <= 0f)
                {
                    contact = prevY + ((y - prevY) * (prevF / Mathf.Max(1e-6f, prevF - f)));
                    break;
                }

                prevY = y;
                prevF = f;
            }

            if (contact < 0f || lip - contact < 0.05f)
            {
                return;
            }

            Color c = _tongueColor;
            Vector3 litS = ColorMath.ToShaderSpace(c);
            float layer = CuiaGeometry.CavityBottomU + 0.5f; // dentro de una dosis llena: alfa 1, sin cara superior
            var uv1 = new Vector4(litS.x, litS.y, litS.z, 0f);
            var uv2 = new Vector4(CuiaGeometry.CavityBottomU + 2f, 0f, 0f, -1f);
            var uv3 = new Vector4(litS.x, litS.y, litS.z, 0f);
            Color32 vc = c;
            float thick = TongueU * _tongue;
            const int segments = 8;
            int first = vh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float y = Mathf.Lerp(Mathf.Max(bottom, contact - 0.15f), lip, i / (float)segments);
                float wall = CuiaGeometry.InnerHalfWidth(Mathf.Max(y, bottom), _capacity);
                float outer = side * (wall + 0.04f); // un poco más allá de la pared: la recorta el shader
                float inner = side * (wall - thick);
                AddVert(vh, outer, y, layer, heightU, vc, uv1, uv2, uv3);
                AddVert(vh, inner, y, layer, heightU, vc, uv1, uv2, uv3);
                if (i > 0)
                {
                    int b = first + ((i - 1) * 2);
                    vh.AddTriangle(b, b + 1, b + 3);
                    vh.AddTriangle(b + 3, b + 2, b);
                }
            }
        }

        // xRel = x − 1 (u); yU = y local (u); layer = y de capa (u).
        private void AddVert(VertexHelper vh, float xRel, float yU, float layer, float heightU, Color32 color, Vector4 uv1, Vector4 uv2, Vector4 uv3)
        {
            var pos = new Vector3(xRel * _unit, yU * _unit, 0f);
            vh.AddVert(pos, color, new Vector4(xRel + 1f, yU, layer, heightU), uv1, uv2, uv3, Vector3.back, new Vector4(1f, 0f, 0f, -1f));
        }

        // smoothstep de HLSL: 0 hasta a, 1 desde b y la cúbica de Hermite entre medias.
        private static float SmoothStep(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - (2f * t));
        }

        private float RippleAmplitude()
        {
            return _rippleMs < 0f ? 0f : RippleAmplitudeU * Mathf.Exp(-_rippleMs / RippleDecayMs);
        }

        private static void Accumulate(Vector2 v, ref bool has, ref Vector2 first, ref Vector2 prev, ref float sum)
        {
            if (has)
            {
                sum += (prev.x * v.y) - (v.x * prev.y);
            }
            else
            {
                first = v;
                has = true;
            }

            prev = v;
        }

        // Polígono de la silueta interior (X = x − 1): pared derecha de la cavidad, hombro en 16 tramos, cuello hasta el
        // labio y la mitad izquierda en espejo. Se crea una vez por capacidad.
        private static Vector2[] Silhouette(int capacity)
        {
            int cap = Mathf.Clamp(capacity, 1, MaxCapacity);
            Vector2[]? poly = s_silhouettes[cap];
            if (poly != null)
            {
                return poly;
            }

            float top = CuiaGeometry.CavityTopU(cap);
            float lip = CuiaGeometry.LipU(cap);
            int half = 2 + ShoulderSamples + 1; // fondo, techo de la cavidad, hombro y labio
            poly = new Vector2[2 * half];
            int n = 0;
            poly[n++] = new Vector2(CuiaGeometry.CavityHalfWidthU, CuiaGeometry.CavityBottomU);
            poly[n++] = new Vector2(CuiaGeometry.CavityHalfWidthU, top);
            for (int i = 1; i <= ShoulderSamples; i++)
            {
                float y = top + (CuiaGeometry.ShoulderU * i / ShoulderSamples);
                float w = i == ShoulderSamples ? CuiaGeometry.NeckInnerHalfWidthU : CuiaGeometry.InnerHalfWidth(y, cap);
                poly[n++] = new Vector2(w, y);
            }

            poly[n++] = new Vector2(CuiaGeometry.NeckInnerHalfWidthU, lip);
            for (int i = half - 1; i >= 0; i--)
            {
                poly[n++] = new Vector2(-poly[i].x, poly[i].y);
            }

            s_silhouettes[cap] = poly;
            return poly;
        }
    }
}
