#nullable enable
using System;
using AF.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>Aspecto de una cinta del Mapa del río (propuesta del Mapa §3.4 y §11.1).</summary>
    public enum RibbonLook
    {
        /// <summary>Vidrio: «por descubrir», azul hielo translúcido con borde claro, sin tipos de agua.</summary>
        Glass = 0,

        /// <summary>
        /// Agua: el color de cada tramo (<c>water</c>) y las dos aguas lado a lado (<c>splits</c>), con el mismo borde de vidrio
        /// (<c>vidrio_borde</c>; propuesta del Mapa §3.5).
        /// </summary>
        Water = 1,
    }

    /// <summary>
    /// Parte de la cinta que se dibuja (propuesta del Mapa §3.5; D-003, vidrio y luz). El Mapa dibuja el agua llena en dos pasadas,
    /// como un mapa con orla: primero las orlas de todas las líneas y luego el agua de todas, para que en una confluencia el agua
    /// del afluente entre en la del cauce sin una costura de orla por medio.
    /// </summary>
    public enum RibbonPart
    {
        /// <summary>La cinta entera (borde, cuerpo y borde), como el vidrio.</summary>
        Whole = 0,

        /// <summary>
        /// Solo la orla, por fuera del ancho del agua: un canto de vidrio de <see cref="RiverRibbonGraphic.CasingRimDp"/>
        /// (<c>vidrio_borde</c>, opaco) y, por fuera, un filete de <see cref="RiverRibbonGraphic.CasingFilletDp"/> de
        /// <see cref="Palette.UiInk"/> al <see cref="RiverRibbonGraphic.CasingFilletAlpha"/>, que lo separa de la tierra.
        /// </summary>
        Casing = 1,

        /// <summary>
        /// Solo el agua, sin borde (lo pone la orla), con un reflejo interior de <see cref="RiverRibbonGraphic.ReflectionDp"/> en
        /// <see cref="Palette.Glint"/> al <see cref="RiverRibbonGraphic.ReflectionAlpha"/>, desplazado
        /// <see cref="RiverRibbonGraphic.ReflectionOffset"/> del ancho hacia el borde de arriba.
        /// </summary>
        Core = 2,
    }

    /// <summary>
    /// Las bocas de los afluentes de una línea del Mapa, para el trazo curvo (FX12; <see cref="RiverRibbonGraphic.SetAnchors"/>):
    /// dónde cae cada una en la línea (s, de menor a mayor) y la punta del afluente que llega ahí (unidades del marco), que es por
    /// donde tiene que pasar la curva aunque no caiga justo sobre la polilínea del anfitrión.
    /// </summary>
    public sealed class RibbonAnchors
    {
        public RibbonAnchors(float[] s, Vector2[] points, bool[]? mouth = null)
        {
            S = s;
            Points = points;
            Mouth = mouth ?? new bool[s.Length];
        }

        /// <summary>s de cada boca en la línea, de menor a mayor.</summary>
        public float[] S { get; }

        /// <summary>Punta del afluente de cada boca (unidades del marco).</summary>
        public Vector2[] Points { get; }

        /// <summary>Si cada ancla es la boca de un afluente (si no, un sitio de la polilínea donde se coloca algo).</summary>
        public bool[] Mouth { get; }

        /// <summary>Anclas.</summary>
        public int Count => S.Length;
    }

    /// <summary>
    /// Cinta de un río del Mapa (08 §2.2; propuesta del Mapa §3.3-§3.4, §11.1 y §11.4): <c>MaskableGraphic</c> con tiras de
    /// triángulos y color por vértice sobre el material de UI por defecto, como <see cref="WaterStripGraphic"/> y
    /// <see cref="BezierStripGraphic"/>, sin shader propio. Dibuja el intervalo [s0, s1] de una línea (<see cref="SetFill"/>), de
    /// modo que sirve para las tres instancias de la pantalla: el vidrio (toda la cuenca, una vez), el lleno (al abrir y tras una
    /// llegada) y el frente (solo el tramo que se anima; sin saltar puntos desde la revisión de FX11, para seguir el vidrio).
    /// <list type="bullet">
    /// <item><description>Ancho por la clase de la línea: <see cref="WidthClass1Dp"/>, <see cref="WidthClass2Dp"/> y
    /// <see cref="WidthClass3Dp"/> [P]; uniones en inglete limitadas a <see cref="MiterLimit"/> veces el medio ancho, para que un
    /// codo cerrado no lance vértices lejos.</description></item>
    /// <item><description>Agua: color por vértice según <c>water</c> (blanca, negra, clara y andina) y, en las franjas de
    /// <c>splits</c>, la cinta partida a lo largo con el agua de cada orilla mirando río abajo; los cambios de agua son
    /// netos (cada tramo con sus propios vértices). Un tramo sin agua comprobada ([NV]) se dibuja en vidrio.</description></item>
    /// <item><description>Borde: el vidrio y el agua llevan el mismo borde claro, <c>vidrio_borde</c> (#EAF7F3,
    /// <see cref="Palette.GlassRim"/>), como fija la propuesta del Mapa §3.5 para el agua llena y como el canto de la franja de
    /// llegada. Con Patrones de fruta (02 §12; propuesta del Mapa §11.4; <see cref="SetPatterns"/>), el lleno se distingue del
    /// vidrio también por el trazo: el agua lleva una trama por tipo (puntos en la blanca, rayas transversales en la negra,
    /// ondas en la clara y, en la andina, guiones a lo largo del río [P], para que el paso de la andina a la blanca no dependa
    /// solo del color: 07 §Accesibilidad), con la opacidad canónica del patrón (07 §3, regla 1: <c>ui_pattern_opacity</c>,
    /// 0,20; 0,35 solo en Alto contraste, que pasa quien la usa).</description></item>
    /// <item><description>Presupuesto de vértices (<see cref="SetVertexBudget"/>): si el intervalo no cabe, se saltan puntos
    /// intermedios de la línea de forma uniforme (si no cabe ninguno, todos) y la cinta une los que quedan con cuerdas: el
    /// inglete de cada sección y la trama siguen esas cuerdas, no la polilínea original. Las dos puntas se dibujan siempre
    /// (si no caben todos los cambios de agua, se omiten los últimos antes que la sección final, y la trama sigue el aspecto
    /// que dibuja la cinta), salvo con un tope que no admite ni dos secciones, que deja la malla vacía; la trama sale solo
    /// mientras quede presupuesto. Nunca se pasa del tope.</description></item>
    /// <item><description>Trazo suave (<see cref="SetSmooth"/>; M5.1c, la geometría de producción de M5.1): la cinta se queda solo
    /// con los puntos de la línea a <see cref="SpacingWidths"/> anchos de cinta en pantalla unos de otros (12 dp en la clase 1 y
    /// 7,5 dp en la clase 2 de la vista Mundo), elegidos a lo largo de la línea entera, así que el vidrio y el agua de una línea
    /// saltan los mismos puntos y quedan una sobre otra; los une con cuerdas, como con el presupuesto. Las uniones cuyo inglete
    /// pasaría de <see cref="BevelMiter"/> medios anchos van en bisel: el lado de dentro con su inglete y el de fuera cortado por
    /// una cuña, sin picos ni solapes.</description></item>
    /// <item><description>Trazo curvo (<see cref="SetCurve"/>; FX12, la vista Mundo y el viaje del banner): sobre el trazo suave, la
    /// cinta sigue una curva de Catmull-Rom centrípeta por los puntos que se quedan (filtrados, y pasando cerca de las bocas de los
    /// afluentes y de lo que el Mapa coloca sobre la línea: <see cref="SetAnchors"/>), partida donde hace falta; con un tope de
    /// vértices (el frente largo), solo los pasos que hacen falta para no apartarse de la curva más de una tolerancia
    /// (<see cref="CurveBudgetToleranceDp"/>). Es de la línea entera, así que el vidrio, el lleno, el frente
    /// y su orla van sobre la misma curva. Solo cambia el dibujo: los datos, el cursor y lo que se coloca sobre la línea, no.</description></item>
    /// </list>
    /// Las coordenadas son las locales de su RectTransform según <see cref="MapSpace"/>. Solo se reconstruye cuando cambia algo
    /// (<see cref="SetFill"/> sin cambios no hace nada ni asigna memoria); al reconstruirse no asigna memoria tras la primera vez.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RiverRibbonGraphic : MaskableGraphic
    {
        /// <summary>Ancho del cauce y del ramal del Pará (clase 1), en dp [P].</summary>
        public const float WidthClass1Dp = 6f;

        /// <summary>Ancho de los afluentes (clase 2), en dp [P].</summary>
        public const float WidthClass2Dp = 3.5f;

        /// <summary>Ancho de los ríos locales (clase 3), en dp [P].</summary>
        public const float WidthClass3Dp = 2f;

        /// <summary>
        /// Tope de vértices del frente, el tramo que se anima, según la propuesta del Mapa §11.4 (≤ 400). Desde la revisión de FX11
        /// el Mapa ya no lo aplica a sus cintas del frente: llevan <see cref="MaxVertices"/>, como el vidrio y el lleno, para no
        /// saltar puntos y seguir el vidrio. Es una desviación provisional [P] de §11.4, anotada allí y pendiente del dueño: el frente del 300
        /// (5 piezas) pide 2.534 vértices entre agua y orla en Mundo (<c>Fx11Tests</c> lo acota en 3.000; con el trazo curvo de FX12, el
        /// Mapa reparte ese tope entre las piezas: <c>RiverMapScreen.FrontCurveMaxVertices</c>). Queda como cifra de
        /// referencia: la usan las pruebas de la cinta y las de un frente de una sola pieza (<c>MapProgressTests</c>).
        /// </summary>
        public const int FrontMaxVertices = 400;

        /// <summary>Tope de vértices de una malla de uGUI.</summary>
        public const int MaxVertices = 65000;

        /// <summary>Largo máximo del inglete, en medios anchos [P].</summary>
        public const float MiterLimit = 2f;

        /// <summary>Grosor del borde (vidrio y agua), en dp [P] (como mucho el 35 % del medio ancho).</summary>
        public const float RimDp = 1f;

        /// <summary>Paso de la trama de Patrones de fruta a lo largo de la cinta, en dp [P].</summary>
        public const float PatternStepDp = 4f;

        /// <summary>
        /// Opacidad del patrón en Alto contraste (07 §3, reglas 1 y 5: 35 %, solo en ese modo). La cinta no lee el modo: quien la
        /// configura la pasa a <see cref="SetPatterns"/>.
        /// </summary>
        public const float PatternOpacityHighContrast = 0.35f;

        /// <summary>
        /// Largo del guion de la trama del agua andina, en fracciones de <see cref="PatternStepDp"/> [P] (2,8 dp de cada 4: una
        /// línea discontinua a lo largo del río; la propuesta del Mapa §11.4 solo fija la trama de la blanca, la negra y la clara).
        /// </summary>
        public const float AndeanDashFraction = 0.7f;

        private const float RimMaxFraction = 0.35f;

        /// <summary>
        /// Con el trazo suave (<see cref="SetSmooth"/>), separación mínima entre los puntos de la línea que dibuja la cinta, en anchos
        /// de la cinta en pantalla (M5.1c, ratificado por el dueño: D-029): 12 dp en la clase 1 (8 dp) y 7,5 dp en la clase 2 (5 dp) de la vista Mundo.
        /// </summary>
        public const float SpacingWidths = 1.5f;

        /// <summary>
        /// Con el trazo suave, una unión cuyo inglete mediría más de estos medios anchos (un giro de más de 74°) va en bisel
        /// (M5.1c, ratificado por el dueño: D-029).
        /// </summary>
        public const float BevelMiter = 1.25f;

        /// <summary>
        /// Con el trazo curvo (<see cref="SetCurve"/>; FX12), separación mínima entre los puntos de la línea por los que pasa la
        /// curva, en anchos de la cinta en pantalla [P]: más que <see cref="SpacingWidths"/> (16 dp en la clase 1 y 10 dp en la clase 2
        /// de la vista Mundo), para que la curva, partida donde hace falta, cueste menos vértices (el frente del 300 en Mundo pediría
        /// 5.764 vértices con todos sus pasos, frente a 2.534 con cuerdas cada 1,5 anchos; el Mapa lo deja en el tope de FX11 con
        /// <see cref="SetVertexBudget"/>).
        /// </summary>
        public const float CurveSpacingWidths = 2f;

        /// <summary>
        /// Con el trazo curvo, lo más que el borde de fuera de la cinta se aparta de la curva en un codo entre cuerdas (su flecha),
        /// en dp [P]: un tramo se parte por la mitad mientras la flecha de su cuerda (la del eje, más lo que añade el medio ancho de
        /// fuera con el giro) o la del codo con el tramo de al lado (por la tangente de la curva en sus puntas: largo × desvío / 4)
        /// pase de esto. Con 0,75 dp y 25° (la primera versión de FX12) quedaban cuerdas de unos 14 dp con 25° entre ellas y flechas
        /// de 0,8 dp, que en el teléfono se seguían leyendo como esquinas (revisión de FX12); 0,3 dp es un píxel a 3 px/dp.
        /// </summary>
        public const float CurveToleranceDp = 0.3f;

        /// <summary>
        /// Con el trazo curvo, giro máximo entre dos cuerdas seguidas de un tramo, en grados [P], si las dos miden al menos
        /// <see cref="CurveShortChordDp"/>: a 8 dp de ancho, 10° desplazan el borde de fuera de un arco menos de un píxel (revisión
        /// de FX12). Entre cuerdas más cortas manda la flecha (<see cref="CurveToleranceDp"/>), con <see cref="CurveMaxShortTurnDegrees"/>.
        /// </summary>
        public const float CurveMaxTurnDegrees = 10f;

        /// <summary>
        /// Con el trazo curvo, cuerda corta, en dp [P]: en un codo con una de menos de esto (un meandro cerrado, de pocos dp de radio, como los
        /// del Madre de Dios o el Beni en el Mundo), el giro puede llegar a <see cref="CurveMaxShortTurnDegrees"/> si la flecha no
        /// pasa de <see cref="CurveToleranceDp"/>: partirlos cada 10° no se ve y doblaba los vértices del frente (tope de FX11).
        /// </summary>
        public const float CurveShortChordDp = 4f;

        /// <summary>Con el trazo curvo, giro máximo entre dos cuerdas cortas (<see cref="CurveShortChordDp"/>), en grados [P].</summary>
        public const float CurveMaxShortTurnDegrees = 25f;

        /// <summary>
        /// Con el trazo curvo, largo mínimo de una cuerda en pantalla, en dp [P] (no se parte más): con 0,75 dp, en los meandros más
        /// cerrados del Madre de Dios a 15× quedaban codos de 50° entre cuerdas de 1 dp (revisión de FX12).
        /// </summary>
        public const float CurveMinStepDp = 0.25f;

        /// <summary>Con el trazo curvo, veces como mucho que se parte por la mitad un tramo (hasta 64 cuerdas) [P].</summary>
        public const int CurveMaxDepth = 6;

        /// <summary>
        /// Con el trazo curvo, pasadas del filtro (1, 2, 1) / 4 sobre los puntos por los que pasa la curva [P]: quita los picos de un
        /// punto de la geometría de producción (las puntas de la línea y las anclas no se mueven más de lo que dejan
        /// <see cref="CurveMaxShiftWidths"/> y <see cref="CurveAnchorWidths"/>).
        /// </summary>
        public const int CurveSmoothPasses = 1;

        /// <summary>
        /// Con el trazo curvo, lo más que el filtro aparta un punto del suyo, en anchos de la cinta [P]. Lo que se coloca sobre la
        /// línea (las paradas, las boyas y la canoa) no depende de esto: es un ancla de la curva (<see cref="SetAnchors"/>).
        /// </summary>
        public const float CurveMaxShiftWidths = 1.5f;

        /// <summary>
        /// Con el trazo curvo, alcance de la vuelta de un ancla (<see cref="SetAnchors"/>) a su sitio, en separaciones de los puntos
        /// [P]: los puntos de alrededor vuelven con ella, cada vez menos, para que la curva no haga esquina en la boca.
        /// </summary>
        public const float CurveAnchorReach = 3f;

        /// <summary>
        /// Con el trazo curvo, lo más que la curva pasa de un ancla (la punta del afluente), en anchos de la cinta [P]: menos de medio
        /// ancho, así que la boca del afluente cae dentro de la cinta de su anfitrión. Revisión de FX12: con 0,3 el ancla tiraba de la
        /// curva y dejaba un quiebro en S en la boca; con 0,45 (y el filtro, que ya casi no aparta los puntos) apenas tira.
        /// </summary>
        public const float CurveAnchorWidths = 0.45f;

        /// <summary>
        /// Con el trazo curvo, dos anclas a menos de estos anchos de la cinta (a lo largo de la línea) se quedan en una, la boca si la
        /// hay [P] (revisión de FX12: una parada en una boca hacía que la curva volviera atrás).
        /// </summary>
        public const float CurveAnchorMergeWidths = 0.3f;

        // Con el trazo curvo, un paso de la curva no va a menos de esto (dp) de una sección del plan: una cuerda de nada.
        private const float CurveMinGapDp = 0.5f;

        /// <summary>Canto de vidrio de la orla del agua llena (dp), por fuera del ancho del agua [P].</summary>
        public const float CasingRimDp = 1.5f;

        /// <summary>Filete de la orla del agua llena (dp), por fuera del canto [P].</summary>
        public const float CasingFilletDp = 1f;

        /// <summary>Opacidad del filete de <see cref="Palette.UiInk"/> de la orla [P].</summary>
        public const float CasingFilletAlpha = 0.45f;

        /// <summary>Lo que la orla añade al agua por cada lado (dp): canto y filete.</summary>
        public const float CasingDp = CasingRimDp + CasingFilletDp;

        /// <summary>Ancho del reflejo interior del agua (dp) [P]; como mucho <see cref="ReflectionMaxFraction"/> del ancho del agua.</summary>
        public const float ReflectionDp = 1f;

        /// <summary>Tope del reflejo, en fracción del ancho del agua, para que un río fino no se quede sin agua [P].</summary>
        public const float ReflectionMaxFraction = 0.3f;

        /// <summary>Opacidad del reflejo de <see cref="Palette.Glint"/> sobre el agua [P].</summary>
        public const float ReflectionAlpha = 0.35f;

        /// <summary>Desplazamiento del reflejo desde el eje hacia el borde de arriba, en fracción del ancho del agua [P].</summary>
        public const float ReflectionOffset = 0.25f;

        /// <summary>Vidrio del Mapa, azul hielo translúcido (#CFEFF3 con alfa 0,85) [P] (paleta de la muestra del Mapa).</summary>
        public static readonly Color GlassBody = new Color(0xCF / 255f, 0xEF / 255f, 0xF3 / 255f, 0.85f);

        /// <summary>Agua andina #9FC9D4 [P] (paleta de la muestra del Mapa, <c>generar_mapa.py</c>).</summary>
        public static readonly Color AndeanWater = Palette.Hex(0x9FC9D4);

        private RiverLine? _line;
        private PolylineCursor? _cursor;
        private MapSpace _space = new MapSpace(MapSpace.BasinWidthDp, Vector2.zero);
        private RibbonLook _look = RibbonLook.Glass;
        private RibbonPart _part = RibbonPart.Whole;
        private float _glassAlpha = GlassBody.a;
        private bool _patterns;
        private float _patternOpacity = Palette.PatternOpacityOn;
        private float _s0;
        private float _s1 = 1f;
        private float _widthDp = -1f;
        private int _budget = MaxVertices;
        private int _vertexCount;
        private int _markCount;

        // Plan de la malla (las secciones, antes de dibujarlas): se reutiliza y solo crece, así que tras la primera vez con el
        // intervalo más largo no asigna memoria.
        private float[] _planArc = Array.Empty<float>();
        private int[] _planVertex = Array.Empty<int>();
        private Vector2[] _planPoint = Array.Empty<Vector2>();
        private RunLook[] _planLook = Array.Empty<RunLook>();
        private bool[] _planConnect = Array.Empty<bool>();
        private int _planCount;
        private bool _decimated;
        private bool _truncated; // el presupuesto cortó el plan antes de algún cambio de agua

        // Trazo suave: los puntos de la línea que se quedan (elegidos a lo largo de la línea entera, desde su primer punto) para la
        // separación en unidades del marco _keepUnits; −1, por calcular.
        private bool _smooth;
        private bool[] _keep = Array.Empty<bool>();
        private float _keepUnits = -1f;
        private int _bevelCount;

        // Trazo curvo (FX12): los puntos por los que pasa la curva (_kept: arco, punto filtrado, punto de la línea y si es un ancla) y
        // sus pasos (_grid: arco y punto); se calculan para la separación _curveUnits y la escala _curveDp (−1, por calcular). _chords: la
        // última malla sigue cuerdas entre sus secciones (puntos saltados o curva), no la polilínea.
        private bool _curve;
        private bool[] _keptFixed = Array.Empty<bool>();
        private bool[] _keptMouth = Array.Empty<bool>();
        private Vector2[] _keptOrigin = Array.Empty<Vector2>();
        private RibbonAnchors? _anchors;
        private Vector2[] _keptPoint = Array.Empty<Vector2>();
        private float[] _keptArc = Array.Empty<float>();
        private float[] _gridArc = Array.Empty<float>();
        private Vector2[] _gridPoint = Array.Empty<Vector2>();
        private int _gridCount;
        private int _keptCount;
        private float _curveUnits = -1f;
        private float _curveDp = -1f;
        private bool _chords;
        private bool _curved; // la última malla va sobre la curva (las puntas, perpendiculares a su tangente)

        // Segundo plan, para subdividir el primero (se intercambian; solo crecen).
        private float[] _subArc = Array.Empty<float>();
        private int[] _subVertex = Array.Empty<int>();
        private Vector2[] _subPoint = Array.Empty<Vector2>();
        private RunLook[] _subLook = Array.Empty<RunLook>();
        private bool[] _subConnect = Array.Empty<bool>();

        /// <summary>Configura el gráfico (sin raycast).</summary>
        public void Setup()
        {
            raycastTarget = false;
        }

        /// <summary>Línea que dibuja, o null.</summary>
        public RiverLine? Line => _line;

        /// <summary>Cursor de la línea (el mismo que pueden usar la canoa y Rosa), o null.</summary>
        public PolylineCursor? Cursor => _cursor;

        /// <summary>Espacio del Mapa con el que se dibuja (<see cref="SetSpace"/>).</summary>
        public MapSpace Space => _space;

        /// <summary>Inicio del intervalo dibujado (s).</summary>
        public float FillStart => _s0;

        /// <summary>Fin del intervalo dibujado (s).</summary>
        public float FillEnd => _s1;

        /// <summary>Aspecto.</summary>
        public RibbonLook Look => _look;

        /// <summary>Parte que dibuja (<see cref="SetPart"/>).</summary>
        public RibbonPart Part => _part;

        /// <summary>Opacidad del cuerpo de vidrio (<see cref="SetGlassBodyAlpha"/>).</summary>
        public float GlassBodyAlpha => _glassAlpha;

        /// <summary>Lleva la trama de Patrones de fruta.</summary>
        public bool Patterns => _patterns;

        /// <summary>Opacidad de la trama (<see cref="SetPatterns"/>).</summary>
        public float PatternOpacity => _patternOpacity;

        /// <summary>
        /// La última malla saltó puntos de la línea por el presupuesto o por el trazo suave (<see cref="SetSmooth"/>): la cinta sigue
        /// cuerdas entre los puntos que quedan.
        /// </summary>
        public bool LastWasDecimated => _decimated;

        /// <summary>Tope de vértices.</summary>
        public int VertexBudget => _budget;

        /// <summary>Ancho de la cinta en dp: el fijado con <see cref="SetWidth"/> o el de la clase de la línea.</summary>
        public float WidthDp => _widthDp > 0f ? _widthDp : (_line != null ? WidthForClass(_line.Class) : WidthClass1Dp);

        /// <summary>Vértices de la última malla construida.</summary>
        public int LastVertexCount => _vertexCount;

        /// <summary>Marcas de la trama de la última malla construida.</summary>
        public int LastMarkCount => _markCount;

        /// <summary>Ancho por clase de línea [P]: 6 dp (1), 3,5 dp (2) y 2 dp (3).</summary>
        public static float WidthForClass(int lineClass)
        {
            switch (lineClass)
            {
                case 1:
                    return WidthClass1Dp;
                case 2:
                    return WidthClass2Dp;
                default:
                    return WidthClass3Dp;
            }
        }

        /// <summary>
        /// Color del agua <paramref name="water"/>: la base de cada agua (07 §2.1; <see cref="Palette.WaterBase"/>) y la andina
        /// [P]; sin agua comprobada, el vidrio.
        /// </summary>
        public static Color WaterColor(RiverWater water)
        {
            switch (water)
            {
                case RiverWater.White:
                    return Palette.WaterWhite;
                case RiverWater.Black:
                    return Palette.WaterBlack;
                case RiverWater.Clear:
                    return Palette.WaterClear;
                case RiverWater.Andean:
                    return AndeanWater;
                default:
                    return GlassBody;
            }
        }

        /// <summary>
        /// Color de la trama sobre el agua <paramref name="water"/> (opaco): hacia el papel sobre las aguas oscuras (L* &lt; 50) y
        /// hacia la tinta sobre las claras, con la opacidad <paramref name="opacity"/> (por defecto la canónica,
        /// <see cref="Palette.PatternOpacityOn"/>), mezclada en sRGB como <see cref="Palette.PatternMark(Color, int, float)"/>.
        /// </summary>
        public static Color MarkColor(RiverWater water, float opacity = Palette.PatternOpacityOn)
        {
            Color body = WaterColor(water);
            Color toward = ColorMath.LabLightness(body) < 50f ? Palette.UiPaper : Palette.UiInk;
            Color mark = Color.Lerp(body, toward, Mathf.Clamp01(opacity));
            mark.a = 1f;
            return mark;
        }

        /// <summary>
        /// Línea que dibuja. Crea su <see cref="PolylineCursor"/> (una vez por línea: es la única asignación de la cinta) y vuelve
        /// a dibujar el intervalo entero [0, 1].
        /// </summary>
        public void SetLine(RiverLine? line)
        {
            if (ReferenceEquals(line, _line))
            {
                return;
            }

            _line = line;
            _cursor = line != null ? new PolylineCursor(line) : null;
            _keepUnits = -1f;
            _curveUnits = -1f;
            if (line != null && _keep.Length < line.PointCount)
            {
                _keep = new bool[line.PointCount]; // solo crece: con la línea más larga, ya no asigna
            }

            _anchors = null;
            EnsureKeptCapacity();
            _s0 = 0f;
            _s1 = 1f;
            SetVerticesDirty();
        }

        /// <summary>
        /// Con el trazo curvo (FX12), los sitios de la línea por los que la curva tiene que pasar: las bocas de los afluentes
        /// (<see cref="MouthAnchors"/>, en la punta de cada afluente) y, en el Mapa, lo que se coloca sobre la polilínea (las paradas,
        /// las boyas y la canoa; <see cref="MergeAnchors"/>). La curva pasa a ≤ <see cref="CurveAnchorWidths"/> anchos de cada uno.
        /// El objeto es de quien llama (no se copia); <see cref="SetLine"/> lo quita.
        /// </summary>
        public void SetAnchors(RibbonAnchors? anchors)
        {
            if (ReferenceEquals(anchors, _anchors))
            {
                return;
            }

            _anchors = anchors;
            _curveUnits = -1f;
            EnsureKeptCapacity();
            SetVerticesDirty();
        }

        /// <summary>
        /// Las bocas de los afluentes de cada línea de <paramref name="map"/> (por el índice de la línea; s en ella, de menor a mayor,
        /// y la punta del afluente que llega ahí), para <see cref="SetAnchors"/>. La punta es la de la polilínea del afluente más
        /// cerca de su sitio en el anfitrión: la boca de los datos, aunque no caiga sobre él (revisión de FX12). Asigna: una vez por Mapa.
        /// </summary>
        public static RibbonAnchors[] MouthAnchors(RiverMap map)
        {
            var lists = new System.Collections.Generic.List<(float S, Vector2 Point)>[map.Lines.Count];
            for (int i = 0; i < map.Lines.Count; i++)
            {
                RiverLine tributary = map.Lines[i];
                RiverMapPlace? mouth = tributary.Mouth;
                if (mouth == null || mouth.Value.Line < 0 || mouth.Value.Line >= lists.Length || tributary.PointCount < 1)
                {
                    continue;
                }

                map.Lines[mouth.Value.Line].PointAt(mouth.Value.S, out float hx, out float hy);
                var place = new Vector2(hx, hy);
                var first = new Vector2(tributary.X(0), tributary.Y(0));
                int lastIndex = tributary.PointCount - 1;
                var last = new Vector2(tributary.X(lastIndex), tributary.Y(lastIndex));
                Vector2 tip = (first - place).sqrMagnitude < (last - place).sqrMagnitude ? first : last;
                (lists[mouth.Value.Line] ??= new System.Collections.Generic.List<(float S, Vector2 Point)>()).Add((mouth.Value.S, tip));
            }

            var anchors = new RibbonAnchors[lists.Length];
            for (int i = 0; i < lists.Length; i++)
            {
                var list = lists[i];
                if (list == null)
                {
                    anchors[i] = new RibbonAnchors(Array.Empty<float>(), Array.Empty<Vector2>());
                    continue;
                }

                list.Sort((x, y) => x.S.CompareTo(y.S));
                var s = new float[list.Count];
                var points = new Vector2[list.Count];
                var isMouth = new bool[list.Count];
                for (int k = 0; k < list.Count; k++)
                {
                    s[k] = list[k].S;
                    points[k] = list[k].Point;
                    isMouth[k] = true;
                }

                anchors[i] = new RibbonAnchors(s, points, isMouth);
            }

            return anchors;
        }

        // La caché de MouthAnchorsOf: las anclas del último Mapa (una sola, por referencia; el Mapa y el viaje del banner comparten
        // el mismo RiverMap).
        private static RiverMap? s_anchorsFor;
        private static RibbonAnchors[]? s_anchors;

        /// <summary>
        /// Las bocas de los afluentes de <paramref name="line"/> (<see cref="MouthAnchors"/>), de una caché del último Mapa: asigna
        /// solo la primera vez con cada Mapa. Null sin línea. Único sitio de la caché, para el Mapa y el viaje del banner.
        /// </summary>
        public static RibbonAnchors? MouthAnchorsOf(RiverMap map, RiverLine? line)
        {
            if (line == null)
            {
                return null;
            }

            if (!ReferenceEquals(map, s_anchorsFor) || s_anchors == null)
            {
                s_anchors = MouthAnchors(map);
                s_anchorsFor = map;
            }

            return line.Index < s_anchors.Length ? s_anchors[line.Index] : null;
        }

        /// <summary>
        /// Las anclas de <paramref name="mouths"/> y los sitios <paramref name="places"/> (s en la línea y su punto, sobre la polilínea:
        /// lo que se coloca sobre ella), juntos y de menor a mayor s; en la misma s, primero la boca. Asigna (al abrir el Mapa).
        /// </summary>
        public static RibbonAnchors MergeAnchors(RibbonAnchors? mouths, System.Collections.Generic.List<(float S, Vector2 Point)>? places)
        {
            int nm = mouths != null ? mouths.Count : 0;
            int np = places != null ? places.Count : 0;
            var all = new System.Collections.Generic.List<(float S, int Order, Vector2 Point)>(nm + np);
            for (int i = 0; i < nm; i++)
            {
                all.Add((mouths!.S[i], i, mouths.Points[i]));
            }

            for (int i = 0; i < np; i++)
            {
                all.Add((places![i].S, nm + i, places[i].Point));
            }

            all.Sort((x, y) => x.S != y.S ? x.S.CompareTo(y.S) : x.Order.CompareTo(y.Order));
            var s = new float[all.Count];
            var points = new Vector2[all.Count];
            var isMouth = new bool[all.Count];
            for (int i = 0; i < all.Count; i++)
            {
                s[i] = all[i].S;
                points[i] = all[i].Point;
                isMouth[i] = all[i].Order < nm && mouths!.Mouth[all[i].Order];
            }

            return new RibbonAnchors(s, points, isMouth);
        }

        // Los arreglos de los puntos de la curva, para los de la línea y sus anclas (solo crecen).
        private void EnsureKeptCapacity()
        {
            int size = (_line != null ? _line.PointCount : 0) + (_anchors != null ? _anchors.Count : 0);
            if (_keptArc.Length < size)
            {
                _keptArc = new float[size];
                _keptPoint = new Vector2[size];
                _keptFixed = new bool[size];
                _keptMouth = new bool[size];
                _keptOrigin = new Vector2[size];
            }
        }

        /// <summary>Espacio del Mapa (dp por unidad y origen local).</summary>
        public void SetSpace(MapSpace space)
        {
            if (!space.Equals(_space))
            {
                _space = space;
                SetVerticesDirty();
            }
        }

        /// <summary>Aspecto: vidrio o agua.</summary>
        public void SetLook(RibbonLook look)
        {
            if (look != _look)
            {
                _look = look;
                SetVerticesDirty();
            }
        }

        /// <summary>Parte de la cinta que se dibuja: entera, solo la orla o solo el agua (<see cref="RibbonPart"/>).</summary>
        public void SetPart(RibbonPart part)
        {
            if (part != _part)
            {
                _part = part;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Opacidad del cuerpo de vidrio (por defecto la de <see cref="GlassBody"/>, 0,85); el borde no cambia. El Mapa la baja para
        /// que el cauce vacío se lea como vidrio fantasma y el progreso mande.
        /// </summary>
        public void SetGlassBodyAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            if (alpha != _glassAlpha)
            {
                _glassAlpha = alpha;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Trama de Patrones de fruta (02 §12): solo en el aspecto de agua. <paramref name="opacity"/> es la del patrón (07 §3,
        /// regla 1): <c>ui_pattern_opacity</c> (por defecto <see cref="Palette.PatternOpacityOn"/>, 0,20) o, en Alto contraste,
        /// <see cref="PatternOpacityHighContrast"/>.
        /// </summary>
        public void SetPatterns(bool on, float opacity = Palette.PatternOpacityOn)
        {
            opacity = Mathf.Clamp01(opacity);
            if (on != _patterns || opacity != _patternOpacity)
            {
                _patterns = on;
                _patternOpacity = opacity;
                SetVerticesDirty();
            }
        }

        /// <summary>Ancho en dp; ≤ 0 vuelve al de la clase de la línea.</summary>
        public void SetWidth(float widthDp)
        {
            widthDp = widthDp > 0f ? widthDp : -1f;
            if (widthDp != _widthDp)
            {
                _widthDp = widthDp;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Trazo suave (M5.1c): solo los puntos de la línea a <see cref="SpacingWidths"/> anchos de cinta en pantalla unos de otros,
        /// unidos con cuerdas, y uniones en bisel donde el inglete pasaría de <see cref="BevelMiter"/>. Lo usan el Mapa y el viaje
        /// del banner; apagado, la cinta sigue cada punto de la polilínea.
        /// </summary>
        public void SetSmooth(bool on)
        {
            if (on != _smooth)
            {
                _smooth = on;
                SetVerticesDirty();
            }
        }

        /// <summary>Trazo suave (<see cref="SetSmooth"/>).</summary>
        public bool Smooth => _smooth;

        /// <summary>
        /// Trazo curvo (FX12; solo con el trazo suave): los puntos que se quedan van a <see cref="CurveSpacingWidths"/> anchos (no a
        /// <see cref="SpacingWidths"/>) y, en vez de cuerdas entre ellos, la cinta sigue la curva de Catmull-Rom centrípeta que pasa
        /// por ellos, partida por la mitad donde la cuerda se aparta más de <see cref="CurveToleranceDp"/> o gira más de
        /// <see cref="CurveMaxTurnDegrees"/> (cuerdas de al menos <see cref="CurveMinStepDp"/>, como mucho
        /// <see cref="CurveMaxDepth"/> veces por tramo); los codos que siguen pasando de <see cref="BevelMiter"/> van en bisel, como
        /// con cuerdas. La curva es de la línea entera
        /// (cada arco de la polilínea cae en un punto fijo de ella, y los pasos también), así que el vidrio, el lleno, el frente y
        /// su orla de una línea van sobre la misma curva. Lo enciende la vista Mundo (y el viaje del banner), donde el zoom deja
        /// ver las esquinas; la Cuenca sigue con cuerdas. Los datos no cambian: solo el dibujo.
        /// </summary>
        public void SetCurve(bool on)
        {
            if (on != _curve)
            {
                _curve = on;
                SetVerticesDirty();
            }
        }

        /// <summary>Trazo curvo (<see cref="SetCurve"/>).</summary>
        public bool Curve => _curve;

        /// <summary>
        /// Solo para las capturas de «antes» de FX12 (<c>Fx12SnapshotTests</c>): con true, las cintas que se construyan desde ese
        /// momento ignoran <see cref="SetCurve"/> y van con las cuerdas de antes de FX12. Nunca en el juego.
        /// </summary>
        public static bool CurveOffForSnapshots;

        /// <summary>Uniones en bisel de la última malla (trazo suave).</summary>
        public int LastBevelCount => _bevelCount;

        /// <summary>
        /// Secciones del plan de la última malla (las puntas, los puntos de la línea y los cambios de agua y, con el trazo curvo, los
        /// pasos de la curva; tests).
        /// </summary>
        public int LastSectionPointCount => _planCount;

        /// <summary>Punto (unidades del marco) de la sección <paramref name="i"/> del plan de la última malla (tests).</summary>
        public Vector2 LastSectionPoint(int i)
        {
            return _planPoint[i];
        }

        /// <summary>Tope de vértices (al menos 8; por defecto, <see cref="MaxVertices"/>).</summary>
        public void SetVertexBudget(int maxVertices)
        {
            maxVertices = Mathf.Clamp(maxVertices, 8, MaxVertices);
            if (maxVertices != _budget)
            {
                _budget = maxVertices;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Intervalo dibujado, en s (en cualquier orden: un tramo que remonta va de s mayor a menor; se recorta a [0, 1]). Solo
        /// reconstruye si cambia: devuelve true si cambió. Sin asignaciones.
        /// </summary>
        public bool SetFill(float s0, float s1)
        {
            float a = Mathf.Clamp01(Mathf.Min(s0, s1));
            float b = Mathf.Clamp01(Mathf.Max(s0, s1));
            if (a == _s0 && b == _s1)
            {
                return false;
            }

            _s0 = a;
            _s1 = b;
            SetVerticesDirty();
            return true;
        }

        /// <inheritdoc />
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            _vertexCount = 0;
            _markCount = 0;
            _planCount = 0;
            _decimated = false;
            _chords = false;
            _curved = false;
            _truncated = false;
            RiverLine? line = _line;
            PolylineCursor? cursor = _cursor;
            if (line == null || cursor == null || cursor.Length <= 0f || _s1 <= _s0 || _space.DpPerUnit <= 0f)
            {
                return;
            }

            float half = WidthDp * 0.5f;
            float rim = Mathf.Min(RimDp, half * RimMaxFraction) / half;
            float length = cursor.Length;
            float aStart = _s0 * length;
            float aEnd = _s1 * length;
            bool water = _look == RibbonLook.Water && _part != RibbonPart.Casing;

            // Puntos de la línea estrictamente dentro del intervalo.
            int count = cursor.PointCount;
            int first = cursor.Locate(aStart, out _) + 1;
            while (first < count && cursor.ArcOfPoint(first) <= aStart)
            {
                first++;
            }

            int last = cursor.Locate(aEnd, out _) + 1;
            while (last >= first && (last >= count || cursor.ArcOfPoint(last) >= aEnd))
            {
                last--;
            }

            int inner = Mathf.Max(0, last - first + 1);

            // Trazo suave: solo los puntos que se quedan (UpdateKeep) y, cerca de las dos puntas del intervalo, ninguno a menos de
            // media separación de ellas (una cuerda de nada haría un quiebro).
            bool curve = _curve && !CurveOffForSnapshots;
            float units = _smooth ? (curve ? CurveSpacingWidths : SpacingWidths) * WidthDp / _space.DpPerUnit : 0f;
            float endSq = 0f;
            Vector2 startPoint = Vector2.zero;
            Vector2 endPoint = Vector2.zero;
            if (units > 0f)
            {
                UpdateKeep(cursor, units);
                endSq = 0.25f * units * units;
                startPoint = cursor.PointAtArc(aStart);
                endPoint = cursor.PointAtArc(aEnd);
            }

            int usable = 0;
            for (int v = NextUsable(cursor, first, last, units, startPoint, endPoint, endSq); v <= last;
                v = NextUsable(cursor, v + 1, last, units, startPoint, endPoint, endSq))
            {
                usable++;
            }

            // Presupuesto: secciones fijas (los dos extremos y cada cambio de agua, que va dos veces) y, si no caben todas, se
            // salta uno de cada «stride» puntos intermedios; si no cabe ninguno, se saltan todos.
            int boundaries = 0;
            bool anySplit = false;
            if (water)
            {
                for (float b = line.NextBoundaryAfter(_s0); b < _s1; b = line.NextBoundaryAfter(b))
                {
                    boundaries++;
                }

                for (int i = 0; i < line.Splits.Count; i++)
                {
                    anySplit |= line.Splits[i].S0 < _s1 && line.Splits[i].S1 > _s0;
                }
            }

            int perSection = 2 * BandCount(anySplit);
            if (2 * perSection > _budget)
            {
                return; // ni las dos puntas caben
            }

            int fixedSections = 2 + (2 * boundaries);
            int stride = 1;
            int nextVertex = NextUsable(cursor, first, last, units, startPoint, endPoint, endSq);
            if ((usable + fixedSections) * perSection > _budget)
            {
                int room = (_budget / perSection) - fixedSections;
                if (room >= 1)
                {
                    stride = (usable + room - 1) / room;
                }
                else
                {
                    nextVertex = last + 1; // solo las secciones fijas
                }
            }

            EnsurePlanCapacity(usable + fixedSections + 1);
            int keptVertices = 0;
            RunLook look = LookAt(line, _s0, water);
            int used = AddPlan(cursor, aStart, -1, look, false);
            float nextBoundary = water ? line.NextBoundaryAfter(_s0) : 2f;
            while (true)
            {
                float vertexArc = nextVertex <= last ? cursor.ArcOfPoint(nextVertex) : float.MaxValue;
                float boundaryArc = nextBoundary < _s1 ? nextBoundary * length : float.MaxValue;
                float arc = Mathf.Min(aEnd, Mathf.Min(vertexArc, boundaryArc));
                bool atVertex = vertexArc <= arc && arc < aEnd;
                bool atBoundary = boundaryArc <= arc && arc < aEnd;
                RunLook after = atBoundary ? LookAt(line, nextBoundary, water) : look;
                bool change = atBoundary && !after.Equals(look);

                // La sección final tiene su sitio reservado: si esta (y la que abre el agua nueva) no lo dejan, se salta a ella.
                int cost = Cost(look) + (change ? Cost(after) : 0);
                if (arc >= aEnd || used + cost + perSection > _budget)
                {
                    _truncated = arc < aEnd; // se omiten los cambios de agua que quedan: la punta lleva el aspecto anterior
                    AddPlan(cursor, aEnd, -1, look, true);
                    break;
                }

                int vertex = atVertex ? nextVertex : -1;
                used += AddPlan(cursor, arc, vertex, look, true);
                if (atVertex)
                {
                    keptVertices++;
                    for (int k = 0; k < stride && nextVertex <= last; k++)
                    {
                        nextVertex = NextUsable(cursor, nextVertex + 1, last, units, startPoint, endPoint, endSq);
                    }
                }

                if (atBoundary)
                {
                    nextBoundary = line.NextBoundaryAfter(nextBoundary);
                    if (change)
                    {
                        look = after;
                        used += AddPlan(cursor, arc, vertex, look, false); // cambio neto: tira nueva
                    }
                }
            }

            _decimated = keptVertices < inner;
            _chords = _decimated;
            _curveToleranceUsed = 0f;
            if (curve && units > 0f && _planCount >= 2)
            {
                // Trazo curvo (FX12): cada sección, en la curva de la línea, y entre ellas los pasos de la curva que caben.
                UpdateCurve(cursor, units);
                RefinePlan(cursor, perSection);
                _chords = true;
                _curved = _keptCount >= 2;
            }

            _bevelCount = 0;
            int remaining = 0;
            for (int i = 0; i < _planCount; i++)
            {
                remaining += Cost(_planLook[i]);
            }

            int previous = -1;
            for (int i = 0; i < _planCount; i++)
            {
                RunLook sectionLook = _planLook[i];
                int connect = _planConnect[i] ? previous : -1;
                remaining -= Cost(sectionLook);

                // Trazo suave: un codo cerrado va en bisel (dos secciones en el punto) si cabe en el presupuesto con lo que queda.
                if (_smooth && connect >= 0 && vh.currentVertCount + (2 * Cost(sectionLook)) + remaining <= _budget
                    && TryBevel(cursor, i, half, out Vector2 inLeft, out Vector2 inRight, out Vector2 outLeft, out Vector2 outRight,
                        out float upIn, out float upOut))
                {
                    previous = EmitSection(vh, (inLeft + inRight) * 0.5f, (inLeft - inRight) * 0.5f, upIn, sectionLook, half, rim, connect);
                    previous = EmitSection(vh, (outLeft + outRight) * 0.5f, (outLeft - outRight) * 0.5f, upOut, sectionLook, half, rim, previous);
                    _bevelCount++;
                }
                else
                {
                    Vector2 offset = SectionOffset(cursor, i);
                    Vector2 center = _space.ToLocal(_planPoint[i]);
                    float up = offset.sqrMagnitude > 1e-12f ? offset.normalized.y : 0f; // > 0: la orilla izquierda queda arriba
                    previous = EmitSection(vh, center, offset * half, up, sectionLook, half, rim, connect);
                }

                if (previous < 0)
                {
                    break; // no pasa: el plan ya cuenta el presupuesto
                }
            }

            if (water && _patterns)
            {
                AddPattern(vh, line, cursor, aStart, aEnd, half);
            }

            _vertexCount = vh.currentVertCount;
        }

        // Vértices de una sección con el aspecto «look».
        private int Cost(RunLook look)
        {
            return 2 * BandCount(look.Split);
        }

        // Bandas de una sección: la cinta entera, borde, agua (una o dos) y borde; la orla, filete, canto y filete; el agua, una o
        // dos y el reflejo.
        private int BandCount(bool split)
        {
            switch (_part)
            {
                case RibbonPart.Casing:
                    return 3;
                case RibbonPart.Core:
                    return split ? 3 : 2;
                default:
                    return split ? 4 : 3;
            }
        }

        private void EnsurePlanCapacity(int sections)
        {
            if (_planArc.Length >= sections)
            {
                return;
            }

            _planArc = new float[sections];
            _planVertex = new int[sections];
            _planPoint = new Vector2[sections];
            _planLook = new RunLook[sections];
            _planConnect = new bool[sections];
        }

        // Añade una sección al plan (en el punto «vertex» de la línea, o dentro de un tramo si es −1) y devuelve sus vértices.
        private int AddPlan(PolylineCursor cursor, float arc, int vertex, RunLook look, bool connect)
        {
            if (_planCount >= _planArc.Length)
            {
                return 0; // no pasa: la capacidad cuenta todas las secciones posibles
            }

            int i = _planCount++;
            _planArc[i] = arc;
            _planVertex[i] = vertex;
            _planPoint[i] = vertex >= 0 ? cursor.Point(vertex) : cursor.PointAtArc(arc);
            _planLook[i] = look;
            _planConnect[i] = connect;
            return Cost(look);
        }

        // Desplazamiento de la sección i del plan hacia la orilla izquierda, en medios anchos. Sin puntos saltados, el de la
        // polilínea: inglete en un punto de la línea y la normal del tramo dentro de él. Con puntos saltados, la cinta sigue
        // cuerdas entre las secciones, así que el inglete se hace entre la cuerda que llega y la que sale (en las puntas, la
        // única cuerda).
        private Vector2 SectionOffset(PolylineCursor cursor, int i)
        {
            if (!_chords)
            {
                int vertex = _planVertex[i];
                if (vertex >= 0)
                {
                    cursor.DirectionsAt(vertex, out Vector2 incoming, out Vector2 outgoing);
                    return Miter(Normal(incoming), Normal(outgoing));
                }

                int segment = cursor.Locate(_planArc[i], out float t);
                if (i > 0 && i == _planCount - 1 && t <= 0f && segment > 0)
                {
                    // La sección final cae justo en un punto de la línea: la cinta llega por el tramo anterior (Locate da el
                    // siguiente, el que empieza en ese punto). La inicial no lo necesita: sale por el tramo que da Locate.
                    cursor.DirectionsAt(segment, out Vector2 arriving, out _);
                    return Normal(arriving);
                }

                return Normal(cursor.SegmentDirection(segment));
            }

            Vector2 p = _planPoint[i];
            bool hasIn = false;
            bool hasOut = false;
            Vector2 dIn = Vector2.zero;
            Vector2 dOut = Vector2.zero;
            for (int k = i - 1; k >= 0 && !hasIn; k--)
            {
                hasIn = TryChord(_planPoint[k], p, out dIn);
            }

            for (int k = i + 1; k < _planCount && !hasOut; k++)
            {
                hasOut = TryChord(p, _planPoint[k], out dOut);
            }

            if (hasIn && hasOut)
            {
                return Miter(Normal(dIn), Normal(dOut));
            }

            if (_curved && TryCurveTangent(cursor, _planArc[i], out Vector2 tangent))
            {
                // Trazo curvo, en una punta (del lleno, del frente o de la línea): el corte, perpendicular a la tangente de la curva,
                // no a la única cuerda (que gira hasta CurveMaxTurnDegrees respecto del vidrio que sigue).
                return Normal(tangent);
            }

            if (hasIn || hasOut)
            {
                return Normal(hasIn ? dIn : dOut);
            }

            return Normal(cursor.SegmentDirection(cursor.Locate(_planArc[i], out _)));
        }

        // Tangente unitaria de la curva del trazo curvo en el arco «arc» (por una diferencia de ±CurveMinGapDp / 4, o de un lado en
        // las puntas de la línea).
        private bool TryCurveTangent(PolylineCursor cursor, float arc, out Vector2 direction)
        {
            float h = 0.25f * CurveMinGapDp / _space.DpPerUnit;
            float a0 = Mathf.Max(0f, arc - h);
            float a1 = Mathf.Min(cursor.Length, arc + h);
            return TryChord(CurvePointAt(cursor, a0), CurvePointAt(cursor, a1), out direction);
        }

        // Dirección unitaria de «a» a «b» en el marco, si no son el mismo punto.
        private static bool TryChord(Vector2 a, Vector2 b, out Vector2 direction)
        {
            Vector2 d = b - a;
            float lengthSq = d.sqrMagnitude;
            if (lengthSq <= 1e-14f)
            {
                direction = Vector2.zero;
                return false;
            }

            direction = d / Mathf.Sqrt(lengthSq);
            return true;
        }

        // Una sección transversal de centro «mid» (local) con el medio eje «axis» (local, hacia la orilla izquierda: el medio ancho por
        // el inglete): dos vértices por banda, en mid + axis · u. «up» es la componente vertical (local) de la normal izquierda. Si
        // «connect» ≥ 0, la une con la sección anterior de la misma tira. Devuelve el primer vértice de la sección, o −1 si no cabe
        // en el presupuesto.
        private int EmitSection(VertexHelper vh, Vector2 mid, Vector2 axis, float up, RunLook look, float half, float rim, int connect)
        {
            int bands = BandCount(look.Split);
            if (vh.currentVertCount + (2 * bands) > _budget)
            {
                return -1;
            }

            float alpha = color.a;
            int start = vh.currentVertCount;
            for (int k = 0; k < bands; k++)
            {
                Band(look, k, rim, half, up, out float u0, out float u1, out Color c);
                c.a *= alpha;
                Color32 c32 = c;
                vh.AddVert(mid + (axis * u0), c32, Vector4.zero);
                vh.AddVert(mid + (axis * u1), c32, Vector4.zero);
                if (connect >= 0)
                {
                    int p = connect + (2 * k);
                    int q = start + (2 * k);
                    vh.AddTriangle(p, p + 1, q + 1);
                    vh.AddTriangle(q + 1, q, p);
                }
            }

            return start;
        }

        // Bisel (trazo suave) en la sección i del plan, si es un codo cuyo inglete pasaría de BevelMiter medios anchos: dos
        // secciones en el mismo punto, la primera perpendicular a lo que llega y la segunda a lo que sale, con el lado de dentro
        // en el inglete (limitado a MiterLimit) y el de fuera a medio ancho del punto; entre las dos, una cuña por fuera y nada por
        // dentro. Las orillas de cada una (izquierda y derecha, locales) y la componente vertical de su normal izquierda.
        private bool TryBevel(PolylineCursor cursor, int i, float half, out Vector2 inLeft, out Vector2 inRight, out Vector2 outLeft,
            out Vector2 outRight, out float upIn, out float upOut)
        {
            inLeft = inRight = outLeft = outRight = Vector2.zero;
            upIn = upOut = 0f;
            // Ni en las puntas ni en un cambio de agua (la tira nueva empieza en el mismo punto con su inglete).
            if (i == 0 || i == _planCount - 1 || !_planConnect[i + 1] || !SectionDirections(cursor, i, out Vector2 dIn, out Vector2 dOut))
            {
                return false;
            }

            Vector2 nIn = Normal(dIn);
            Vector2 nOut = Normal(dOut);
            Vector2 m = nIn + nOut;
            if (m.sqrMagnitude < 1e-6f)
            {
                return false; // media vuelta: la deja el inglete
            }

            m.Normalize();
            float cos = Vector2.Dot(m, nOut);
            if (cos >= 1f / BevelMiter)
            {
                return false; // el inglete basta
            }

            Vector2 center = _space.ToLocal(_planPoint[i]);
            Vector2 inner = m * (half / Mathf.Max(cos, 1f / MiterLimit));
            Vector2 lIn = MapSpace.DirectionToLocal(dIn);
            Vector2 lOut = MapSpace.DirectionToLocal(dOut);
            bool left = (lIn.x * lOut.y) - (lIn.y * lOut.x) > 0f; // gira a la izquierda: la orilla de dentro es la izquierda
            if (left)
            {
                inLeft = center + inner;
                outLeft = inLeft;
                inRight = center - (nIn * half);
                outRight = center - (nOut * half);
            }
            else
            {
                inRight = center - inner;
                outRight = inRight;
                inLeft = center + (nIn * half);
                outLeft = center + (nOut * half);
            }

            upIn = nIn.y;
            upOut = nOut.y;
            return true;
        }

        // Direcciones (unidades del marco) por las que llega y sale la cinta en la sección i del plan: las de la polilínea en un punto
        // de la línea o, con puntos saltados, las de las cuerdas. False si falta alguna.
        private bool SectionDirections(PolylineCursor cursor, int i, out Vector2 dIn, out Vector2 dOut)
        {
            dIn = Vector2.zero;
            dOut = Vector2.zero;
            if (!_chords)
            {
                int vertex = _planVertex[i];
                if (vertex <= 0 || vertex >= cursor.PointCount - 1)
                {
                    return false;
                }

                cursor.DirectionsAt(vertex, out dIn, out dOut);
                return dIn.sqrMagnitude > 0f && dOut.sqrMagnitude > 0f;
            }

            Vector2 p = _planPoint[i];
            bool hasIn = false;
            bool hasOut = false;
            for (int k = i - 1; k >= 0 && !hasIn; k--)
            {
                hasIn = TryChord(_planPoint[k], p, out dIn);
            }

            for (int k = i + 1; k < _planCount && !hasOut; k++)
            {
                hasOut = TryChord(p, _planPoint[k], out dOut);
            }

            return hasIn && hasOut;
        }

        // El primer punto de la línea desde «from» (hasta «last») que dibuja la cinta: con el trazo suave (units > 0), uno de los que
        // se quedan (UpdateKeep) que no esté a menos de media separación (endSq, al cuadrado) de las puntas del intervalo; si no,
        // «from». Mayor que «last» si no queda ninguno.
        private int NextUsable(PolylineCursor cursor, int from, int last, float units, Vector2 startPoint, Vector2 endPoint, float endSq)
        {
            if (units <= 0f)
            {
                return from;
            }

            for (int v = from; v <= last; v++)
            {
                if (!_keep[v])
                {
                    continue;
                }

                Vector2 p = cursor.Point(v);
                if ((p - startPoint).sqrMagnitude >= endSq && (p - endPoint).sqrMagnitude >= endSq)
                {
                    return v;
                }
            }

            return last + 1;
        }

        // Los puntos de la línea que se quedan con el trazo suave, para la separación «units» (unidades del marco): el primero, el
        // último y, a lo largo de la línea, cada uno a «units» o más del anterior que se quedó (sin el último de ellos si queda a
        // menos de media separación del final). Elegidos sobre la línea entera, así que no dependen del intervalo dibujado: el
        // vidrio, la orla y el agua de una línea saltan los mismos. Solo si cambia la separación; sin asignaciones.
        private void UpdateKeep(PolylineCursor cursor, float units)
        {
            if (units == _keepUnits)
            {
                return;
            }

            _keepUnits = units;
            int n = cursor.PointCount;
            float sq = units * units;
            Vector2 kept = cursor.Point(0);
            int lastKept = 0;
            _keep[0] = true;
            for (int i = 1; i < n - 1; i++)
            {
                Vector2 p = cursor.Point(i);
                bool keep = (p - kept).sqrMagnitude >= sq;
                _keep[i] = keep;
                if (keep)
                {
                    kept = p;
                    lastKept = i;
                }
            }

            if (n > 1)
            {
                _keep[n - 1] = true;
                if (lastKept > 0 && (cursor.Point(n - 1) - kept).sqrMagnitude < 0.25f * sq)
                {
                    _keep[lastKept] = false;
                }
            }
        }

        // Trazo curvo: los puntos que se quedan, en orden, y los pasos de la curva de la línea entera (_grid: arco y punto, en orden,
        // empezando por cada punto que se queda), para la separación «units» y la escala de la vista: cada tramo entre dos puntos se
        // parte por la mitad mientras su cuerda se aparte de la curva más de CurveToleranceDp o gire más de CurveMaxTurnDegrees
        // (hasta CurveMaxDepth veces y sin cuerdas de menos de CurveMinStepDp). Elegidos sobre la línea entera, como los puntos
        // (UpdateKeep, ya hecho). Solo si cambia la separación o la escala; los arreglos solo crecen.
        private void UpdateCurve(PolylineCursor cursor, float units)
        {
            float dp = _space.DpPerUnit;
            if (units == _curveUnits && dp == _curveDp)
            {
                return;
            }

            _curveUnits = units;
            _curveDp = dp;
            // Los puntos que se quedan y las anclas (SetAnchors), por arco; un punto a menos de media separación de un ancla se
            // salta (salvo las puntas de la línea), y un ancla a menos de media separación de una punta se queda en la punta.
            int n = cursor.PointCount;
            float[]? anchors = _anchors?.S;
            Vector2[]? targets = _anchors?.Points;
            bool[]? isMouth = _anchors?.Mouth;
            float merge = CurveAnchorMergeWidths * WidthDp / dp;
            int m = anchors != null ? anchors.Length : 0;
            float length = cursor.Length;
            float gap = 0.5f * units;
            int a = 0;
            _keptCount = 0;
            for (int i = 0; i < n; i++)
            {
                if (!_keep[i])
                {
                    continue;
                }

                float arc = cursor.ArcOfPoint(i);
                while (a < m && anchors![a] * length < arc)
                {
                    float anchorArc = anchors[a] * length;
                    if (anchorArc >= gap && length - anchorArc >= gap)
                    {
                        int c0 = _keptCount;
                        if (c0 > 0 && _keptFixed[c0 - 1] && anchorArc - _keptArc[c0 - 1] < merge)
                        {
                            // Dos anclas casi en el mismo sitio (una parada en una boca, una boya en una parada): una sola, la boca si
                            // la hay; dos puntos tan juntos con sitios distintos (la punta del afluente no cae justo en la polilínea)
                            // harían que la curva volviera atrás.
                            if (isMouth![a] && !_keptMouth[c0 - 1])
                            {
                                _keptArc[c0 - 1] = anchorArc;
                                _keptPoint[c0 - 1] = targets![a];
                                _keptMouth[c0 - 1] = true;
                            }
                        }
                        else
                        {
                            AddKept(anchorArc, targets![a], true); // la boca de los datos o el sitio de lo que se coloca ahí
                            _keptMouth[_keptCount - 1] = isMouth![a];
                        }
                    }

                    a++;
                }

                bool end = i == 0 || i == n - 1;
                int c = _keptCount;
                bool nearPrevious = c > 0 && _keptFixed[c - 1] && arc - _keptArc[c - 1] < gap;
                bool nearNext = a < m && (anchors![a] * length) - arc < gap && anchors[a] * length >= gap
                    && length - (anchors[a] * length) >= gap;
                if (!end && (nearPrevious || nearNext))
                {
                    continue;
                }

                AddKept(arc, cursor.Point(i), false);
            }

            int count = _keptCount;
            for (int k = 0; k < count; k++)
            {
                _keptOrigin[k] = _keptPoint[k];
            }

            // Filtro (1, 2, 1) / 4 de los puntos de dentro, CurveSmoothPasses veces: quita los dientes de un punto (un pico de la
            // geometría de producción) sin mover las puntas; ningún punto se aparta más de CurveMaxShiftWidths anchos del suyo. Corta
            // los meandros (hasta 16 dp en el Mundo): por eso lo que se coloca sobre la polilínea es un ancla (SetAnchors).
            for (int pass = 0; pass < CurveSmoothPasses; pass++)
            {
                SmoothKept(count, 0.5f);
            }

            float maxShift = CurveMaxShiftWidths * WidthDp / dp;
            for (int k = 1; k < count - 1; k++)
            {
                Vector2 shift = _keptPoint[k] - _keptOrigin[k];
                if (shift.sqrMagnitude > maxShift * maxShift)
                {
                    _keptPoint[k] = _keptOrigin[k] + (shift.normalized * maxShift);
                }
            }

            // Cada ancla vuelve hasta CurveAnchorWidths anchos de su sitio (la boca cae dentro de la cinta sin forzar un codo en ella),
            // y los puntos de alrededor con ella, cada vez menos hasta CurveAnchorReach separaciones; las puntas no se mueven.
            float reach = CurveAnchorReach * units;
            float slack = CurveAnchorWidths * WidthDp / dp;
            for (int k = 1; k < count - 1; k++)
            {
                if (!_keptFixed[k])
                {
                    continue;
                }

                Vector2 back = _keptOrigin[k] - _keptPoint[k];
                float away = back.magnitude;
                if (away <= slack)
                {
                    continue;
                }

                back *= 1f - (slack / away);
                for (int j = k; j > 0 && _keptArc[k] - _keptArc[j] < reach; j--)
                {
                    _keptPoint[j] += back * Falloff((_keptArc[k] - _keptArc[j]) / reach);
                }

                for (int j = k + 1; j < count - 1 && _keptArc[j] - _keptArc[k] < reach; j++)
                {
                    _keptPoint[j] += back * Falloff((_keptArc[j] - _keptArc[k]) / reach);
                }
            }

            _gridCount = 0;
            // Las medidas de la partición, en unidades del marco; el medio ancho de fuera es el del agua más su orla (el mismo en todas
            // las cintas de la línea en la vista, así que todas siguen los mismos pasos).
            _subTolerance = CurveToleranceDp / dp;
            _subMinStep = CurveMinStepDp / dp;
            _subShort = CurveShortChordDp / dp;
            _subHalfOuter = ((0.5f * WidthDp) + CasingRimDp) / dp;
            _subMaxTurn = CurveMaxTurnDegrees * Mathf.Deg2Rad;
            _subMaxShortTurn = CurveMaxShortTurnDegrees * Mathf.Deg2Rad;
            for (int k = 0; k < count; k++)
            {
                Vector2 p0 = _keptPoint[k];
                AddGrid(_keptArc[k], p0);
                if (k + 1 < count)
                {
                    Subdivide(cursor, k, 0f, p0, 1f, _keptPoint[k + 1], 0);
                }
            }
        }

        // Una pasada del filtro sobre los puntos de dentro de la curva: cada uno, w de su camino hacia la media de sus vecinos.
        private void SmoothKept(int count, float w)
        {
            Vector2 before = count > 0 ? _keptPoint[0] : Vector2.zero;
            for (int k = 1; k < count - 1; k++)
            {
                Vector2 here = _keptPoint[k];
                _keptPoint[k] = here + (w * ((0.5f * (before + _keptPoint[k + 1])) - here));
                before = here;
            }
        }

        // Un punto de la curva en el arco «arc»: el de la línea o, si es un ancla, la punta del afluente («point»).
        private void AddKept(float arc, Vector2 point, bool anchor)
        {
            int c = _keptCount;
            if (c > 0 && arc - _keptArc[c - 1] <= 1e-7f)
            {
                if (anchor && !_keptFixed[c - 1])
                {
                    _keptPoint[c - 1] = point; // en el arco del punto anterior: lo fija, en la punta del afluente
                }

                _keptFixed[c - 1] |= anchor;
                return;
            }

            _keptArc[c] = arc;
            _keptPoint[c] = point;
            _keptMouth[c] = false;
            _keptFixed[c] = anchor;
            _keptCount = c + 1;
        }

        // Peso de la vuelta de un ancla a una distancia x ∈ [0, 1] de su alcance: 1 en el ancla y 0 al final, sin esquinas.
        private static float Falloff(float x)
        {
            x = Mathf.Clamp01(x);
            float r = 1f - x;
            return r * r * (1f + (2f * x));
        }

        // Medidas de la partición de la curva (UpdateCurve), en unidades del marco y radianes.
        private float _subTolerance;
        private float _subMinStep;
        private float _subShort;
        private float _subHalfOuter;
        private float _subMaxTurn;
        private float _subMaxShortTurn;

        // Flecha (unidades) del borde de fuera en un codo de giro «turn» (rad) entre cuerdas que miden «chord» de media: un polígono de
        // cuerdas c con giro θ por un arco de radio c / θ se aparta de él c θ / 8, y por fuera sus cuerdas miden c + θ · medio ancho.
        private float OuterSagitta(float turn, float chord)
        {
            return (chord + (turn * _subHalfOuter)) * turn * 0.125f;
        }

        // Giro máximo (rad) entre dos cuerdas de «la» y «lb» unidades: CurveMaxTurnDegrees, o CurveMaxShortTurnDegrees si alguna es
        // corta (entonces manda la flecha).
        private float MaxTurnFor(float la, float lb)
        {
            return la < _subShort || lb < _subShort ? _subMaxShortTurn : _subMaxTurn;
        }

        // Ángulo (rad) entre dos vectores no nulos.
        private static float Turn(Vector2 a, Vector2 b, float la, float lb)
        {
            return Mathf.Acos(Mathf.Clamp(Vector2.Dot(a, b) / (la * lb), -1f, 1f));
        }

        // Parte el trozo [t0, t1] del tramo k de la curva mientras se vea el codo: si la flecha de su cuerda (CurveToleranceDp, por
        // fuera), su giro (CurveMaxTurnDegrees o, entre cuerdas cortas, CurveMaxShortTurnDegrees) o el codo con el tramo de al lado
        // en sus puntas pasan de la cuenta; añade los pasos de dentro en orden.
        private void Subdivide(PolylineCursor cursor, int k, float t0, Vector2 p0, float t1, Vector2 p1, int depth)
        {
            if (depth >= CurveMaxDepth)
            {
                return;
            }

            float tm = 0.5f * (t0 + t1);
            Vector2 pm = CurveSpan(cursor, k, tm);
            Vector2 a = pm - p0;
            Vector2 b = p1 - pm;
            float la = a.magnitude;
            float lb = b.magnitude;
            if (la < _subMinStep || lb < _subMinStep)
            {
                return;
            }

            Vector2 chord = p1 - p0;
            float lc = chord.magnitude;
            float apart = lc > 1e-9f ? Mathf.Abs((chord.x * a.y) - (chord.y * a.x)) / lc : la;
            float turn = Turn(a, b, la, lb);

            // El codo de las dos mitades: la flecha de la cuerda (la del eje, más lo que añade el medio ancho de fuera) y su giro.
            bool split = apart + (turn * turn * _subHalfOuter * 0.125f) > _subTolerance || turn > MaxTurnFor(la, lb);

            // La curva junto a las dos puntas del trozo (la tangente, por una diferencia corta): el codo con el trozo de al lado gira
            // la suma de los dos desvíos, así que cada uno, como mucho la mitad del giro y de la flecha (largo × desvío / 4), aunque
            // el punto medio caiga sobre la cuerda (una S, o un codo pegado a una punta).
            if (!split && lc > 1e-9f)
            {
                float dt = (t1 - t0) * (1f / 16f);
                Vector2 near0 = CurveSpan(cursor, k, t0 + dt) - p0;
                Vector2 near1 = p1 - CurveSpan(cursor, k, t1 - dt);
                float l0 = near0.magnitude;
                float l1 = near1.magnitude;
                float half = 0.5f * MaxTurnFor(lc, lc);
                float off0 = l0 > 1e-9f ? Turn(near0, chord, l0, lc) : 0f;
                float off1 = l1 > 1e-9f ? Turn(near1, chord, l1, lc) : 0f;
                float off = Mathf.Max(off0, off1);
                split = off > half || OuterSagitta(2f * off, lc) > _subTolerance;
            }

            if (!split)
            {
                return;
            }

            Subdivide(cursor, k, t0, p0, tm, pm, depth + 1);
            float a0 = _keptArc[k];
            AddGrid(a0 + ((_keptArc[k + 1] - a0) * tm), pm);
            Subdivide(cursor, k, tm, pm, t1, p1, depth + 1);
        }

        private void AddGrid(float arc, Vector2 point)
        {
            if (_gridCount >= _gridArc.Length)
            {
                int size = Mathf.Max(64, _gridArc.Length * 2); // solo crece: con la escala de más pasos, ya no asigna
                Array.Resize(ref _gridArc, size);
                Array.Resize(ref _gridPoint, size);
            }

            _gridArc[_gridCount] = arc;
            _gridPoint[_gridCount] = point;
            _gridCount++;
        }

        // El primer paso de la curva con arco mayor que «arc».
        private int FirstGridAfter(float arc)
        {
            int lo = 0;
            int hi = _gridCount;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (_gridArc[mid] <= arc)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return lo;
        }

        // El tramo de la curva (el k-ésimo punto que se queda y el siguiente) que contiene el arco «arc» de la polilínea.
        private int FindSpan(float arc)
        {
            int lo = 0;
            int hi = _keptCount - 2;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) >> 1;
                if (_keptArc[mid] <= arc)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return lo;
        }

        /// <summary>
        /// Punto (unidades del marco) de la curva del trazo curvo en el arco <paramref name="arc"/> de la polilínea: en el tramo entre
        /// dos puntos que se quedan, la fracción del arco entre ellos. Sin trazo curvo calculado, el de la polilínea.
        /// </summary>
        private Vector2 CurvePointAt(PolylineCursor cursor, float arc)
        {
            if (_keptCount < 2)
            {
                return cursor.PointAtArc(arc);
            }

            int k = FindSpan(arc);
            float a0 = _keptArc[k];
            float a1 = _keptArc[k + 1];
            return CurveSpan(cursor, k, a1 > a0 ? Mathf.Clamp01((arc - a0) / (a1 - a0)) : 0f);
        }

        // Catmull-Rom centrípeta (Barry-Goldman, alfa 0,5) entre los puntos que se quedan k y k + 1, en t ∈ [0, 1]; en las puntas de
        // la línea, el punto de fuera se refleja. Pasa por los dos puntos y no hace bucles ni picos en los codos cerrados.
        private Vector2 CurveSpan(PolylineCursor cursor, int k, float t)
        {
            Vector2 p1 = _keptPoint[k];
            Vector2 p2 = _keptPoint[k + 1];
            if (t <= 0f)
            {
                return p1;
            }

            if (t >= 1f)
            {
                return p2;
            }

            // Relativo a p1, para no perder precisión.
            Vector2 a2 = p2 - p1;
            Vector2 a0 = k > 0 ? _keptPoint[k - 1] - p1 : -a2;
            Vector2 a3 = k + 2 < _keptCount ? _keptPoint[k + 2] - p1 : a2 + a2;
            float t1 = Mathf.Max(Mathf.Sqrt(a0.magnitude), 1e-6f);
            float t2 = t1 + Mathf.Max(Mathf.Sqrt(a2.magnitude), 1e-6f);
            float t3 = t2 + Mathf.Max(Mathf.Sqrt((a3 - a2).magnitude), 1e-6f);
            float u = t1 + (t * (t2 - t1));
            Vector2 q1 = a0 * ((t1 - u) / t1); // y p1 (el origen) por (u − 0) / t1
            Vector2 q2 = a2 * ((u - t1) / (t2 - t1));
            Vector2 q3 = (a2 * ((t3 - u) / (t3 - t2))) + (a3 * ((u - t2) / (t3 - t2)));
            Vector2 r1 = (q1 * ((t2 - u) / t2)) + (q2 * (u / t2));
            Vector2 r2 = (q2 * ((t3 - u) / (t3 - t1))) + (q3 * ((u - t1) / (t3 - t1)));
            Vector2 c = (r1 * ((t2 - u) / (t2 - t1))) + (r2 * ((u - t1) / (t2 - t1)));
            return p1 + c;
        }

        // Trazo curvo: lleva cada sección del plan a la curva (las de los puntos que se quedan ya están en ella) y mete entre cada
        // dos secciones unidas los pasos de la curva de la línea que caen entre ellas (a más de CurveMinGapDp), con el aspecto de la
        // tira; si no caben todos en el presupuesto, uno de cada «stride». Los pasos son los de la línea entera, así que el vidrio
        // y un trozo suyo pasan por los mismos puntos. Sin asignaciones tras la primera vez con el plan más largo.
        private void RefinePlan(PolylineCursor cursor, int perSection)
        {
            int used = 0;
            for (int i = 0; i < _planCount; i++)
            {
                _planPoint[i] = CurvePointAt(cursor, _planArc[i]);

                used += Cost(_planLook[i]);
            }

            float gap = CurveMinGapDp / _space.DpPerUnit;
            _curveToleranceUsed = 0f;
            int candidates = RefineSweep(cursor, gap, 1, false);
            int room = (_budget - used) / perSection;
            if (candidates == 0 || room <= 0)
            {
                _curveToleranceUsed = candidates == 0 ? 0f : -1f;
                return;
            }

            // Si no caben todos los pasos (el frente largo, con su tope: SetVertexBudget), solo los que hacen falta para que la cinta
            // no se aparte de la curva más de la tolerancia más pequeña de la escala que quepa (CurveBudgetToleranceDp): las secciones
            // siguen en la curva del vidrio, así que el agua queda a esa distancia de él como mucho. Si ni la mayor cabe, uno de cada
            // «stride», como antes.
            float tolerance = 0f;
            if (candidates > room)
            {
                for (int t = 0; t < CurveBudgetToleranceSteps && tolerance <= 0f; t++)
                {
                    float tryDp = CurveBudgetToleranceDp(t);
                    if (RefineSweepTolerance(tryDp / _space.DpPerUnit, gap, false) <= room)
                    {
                        tolerance = tryDp;
                    }
                }
            }

            int stride = candidates <= room || tolerance > 0f ? 1 : (candidates + room - 1) / room;
            _curveToleranceUsed = candidates <= room ? 0f : (tolerance > 0f ? tolerance : -1f);
            int sections = _planCount + candidates + 1;
            if (_subArc.Length < sections)
            {
                _subArc = new float[sections];
                _subVertex = new int[sections];
                _subPoint = new Vector2[sections];
                _subLook = new RunLook[sections];
                _subConnect = new bool[sections];
            }

            if (tolerance > 0f)
            {
                RefineSweepTolerance(tolerance / _space.DpPerUnit, gap, true);
            }
            else
            {
                RefineSweep(cursor, gap, stride, true);
            }
        }

        /// <summary>Pasos de la escala de tolerancias de una cinta con tope (<see cref="CurveBudgetToleranceDp"/>).</summary>
        public const int CurveBudgetToleranceSteps = 8;

        /// <summary>
        /// Tolerancia t (0 … <see cref="CurveBudgetToleranceSteps"/> − 1) de la escala con la que una cinta con tope de vértices (el
        /// frente largo; revisión de FX12) elige los pasos de la curva que dibuja, en dp [P]: de <see cref="CurveToleranceDp"/> a
        /// 1,75 dp, por debajo de los 2 dp a los que el agua puede quedar del vidrio.
        /// </summary>
        public static float CurveBudgetToleranceDp(int t)
        {
            return CurveToleranceDp + ((1.75f - CurveToleranceDp) * t / (CurveBudgetToleranceSteps - 1));
        }

        // Tolerancia (dp) con la que la última malla eligió los pasos de la curva: 0, todos; −1, uno de cada «stride».
        private float _curveToleranceUsed;

        /// <summary>
        /// Tolerancia (dp) con la que la última malla (trazo curvo) eligió los pasos de la curva por su tope de vértices: 0 si caben
        /// todos; −1 si ni con la mayor de la escala cabían y va uno de cada tantos (tests).
        /// </summary>
        public float LastCurveToleranceDp => _curveToleranceUsed;

        // Como RefineSweep, pero entre cada dos secciones unidas del plan se queda solo con los pasos de la curva que hacen falta para
        // que la cuerda no se aparte más de «tolerance» (unidades) de los pasos que se salta (todos están en la curva). Devuelve los
        // pasos que se quedan; con «emit», escribe el plan nuevo y los intercambia. Sin asignaciones.
        private int RefineSweepTolerance(float tolerance, float gap, bool emit)
        {
            int kept = 0;
            int written = 0;
            for (int i = 0; i < _planCount; i++)
            {
                if (emit)
                {
                    _subArc[written] = _planArc[i];
                    _subVertex[written] = _planVertex[i];
                    _subPoint[written] = _planPoint[i];
                    _subLook[written] = _planLook[i];
                    _subConnect[written] = _planConnect[i];
                    written++;
                }

                if (i + 1 >= _planCount || !_planConnect[i + 1])
                {
                    continue;
                }

                float a0 = _planArc[i] + gap;
                float a1 = _planArc[i + 1] - gap;
                if (a1 <= a0)
                {
                    continue;
                }

                RunLook look = _planLook[i + 1];
                int gFirst = FirstGridAfter(a0);
                int gEnd = gFirst;
                while (gEnd < _gridCount && _gridArc[gEnd] < a1)
                {
                    gEnd++;
                }

                Vector2 last = _planPoint[i];
                int run = gFirst; // el primer paso saltado desde «last»
                for (int g = gFirst; g < gEnd; g++)
                {
                    Vector2 next = g + 1 < gEnd ? _gridPoint[g + 1] : _planPoint[i + 1];
                    bool need = false;
                    for (int j = run; j <= g && !need; j++)
                    {
                        need = SegmentDistance(_gridPoint[j], last, next) > tolerance;
                    }

                    if (!need)
                    {
                        continue;
                    }

                    if (emit)
                    {
                        _subArc[written] = _gridArc[g];
                        _subVertex[written] = -1;
                        _subPoint[written] = _gridPoint[g];
                        _subLook[written] = look;
                        _subConnect[written] = true;
                        written++;
                    }

                    kept++;
                    last = _gridPoint[g];
                    run = g + 1;
                }
            }

            if (emit)
            {
                (_planArc, _subArc) = (_subArc, _planArc);
                (_planVertex, _subVertex) = (_subVertex, _planVertex);
                (_planPoint, _subPoint) = (_subPoint, _planPoint);
                (_planLook, _subLook) = (_subLook, _planLook);
                (_planConnect, _subConnect) = (_subConnect, _planConnect);
                _planCount = written;
            }

            return kept;
        }

        // Distancia de «p» al segmento a-b.
        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            float t = lengthSq > 1e-14f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq) : 0f;
            return (a + (ab * t) - p).magnitude;
        }

        /// <summary>
        /// Con el trazo curvo en la vista de la cinta (su espacio y su ancho), las secciones que pide el trozo [s0, s1] de su línea
        /// con todos los pasos de la curva (sus pasos y sus dos puntas); 0 sin trazo curvo. Calcula la curva de la línea si hace
        /// falta (la misma que usará la malla). El Mapa lo usa para repartir el tope del frente entre sus piezas (revisión de FX12).
        /// Sin asignaciones tras la primera vez.
        /// </summary>
        public int CurveDemand(float s0, float s1)
        {
            if (_line == null || _cursor == null || !_smooth || !_curve || CurveOffForSnapshots)
            {
                return 0;
            }

            float units = CurveSpacingWidths * WidthDp / _space.DpPerUnit;
            UpdateKeep(_cursor, units);
            UpdateCurve(_cursor, units);
            float length = _cursor.Length;
            return FirstGridAfter(Mathf.Max(s0, s1) * length) - FirstGridAfter(Mathf.Min(s0, s1) * length) + 2;
        }

        /// <summary>
        /// Vértices de una sección del trozo [s0, s1] de <paramref name="line"/> con la parte <paramref name="part"/> (en el agua, con
        /// dos aguas lado a lado en el trozo, un par más), para repartir un tope.
        /// </summary>
        public static int SectionVertices(RiverLine line, float s0, float s1, RibbonPart part)
        {
            bool split = false;
            for (int i = 0; i < line.Splits.Count; i++)
            {
                split |= line.Splits[i].S0 < Mathf.Max(s0, s1) && line.Splits[i].S1 > Mathf.Min(s0, s1);
            }

            switch (part)
            {
                case RibbonPart.Casing:
                    return 6;
                case RibbonPart.Core:
                    return split ? 6 : 4;
                default:
                    return split ? 8 : 6;
            }
        }

        // Recorre los pasos de la curva entre las secciones unidas del plan; con «emit», escribe el plan nuevo (las secciones y uno
        // de cada «stride» pasos) en el segundo plan y los intercambia. Devuelve los pasos que caben entre secciones.
        private int RefineSweep(PolylineCursor cursor, float gap, int stride, bool emit)
        {
            int candidates = 0;
            int written = 0;
            for (int i = 0; i < _planCount; i++)
            {
                if (emit)
                {
                    _subArc[written] = _planArc[i];
                    _subVertex[written] = _planVertex[i];
                    _subPoint[written] = _planPoint[i];
                    _subLook[written] = _planLook[i];
                    _subConnect[written] = _planConnect[i];
                    written++;
                }

                if (i + 1 >= _planCount || !_planConnect[i + 1])
                {
                    continue;
                }

                float a0 = _planArc[i] + gap;
                float a1 = _planArc[i + 1] - gap;
                if (a1 <= a0)
                {
                    continue;
                }

                RunLook look = _planLook[i + 1];
                for (int g = FirstGridAfter(a0); g < _gridCount && _gridArc[g] < a1; g++)
                {
                    if (emit && candidates % stride == 0)
                    {
                        _subArc[written] = _gridArc[g];
                        _subVertex[written] = -1;
                        _subPoint[written] = _gridPoint[g];
                        _subLook[written] = look;
                        _subConnect[written] = true;
                        written++;
                    }

                    candidates++;
                }
            }

            if (emit)
            {
                (_planArc, _subArc) = (_subArc, _planArc);
                (_planVertex, _subVertex) = (_subVertex, _planVertex);
                (_planPoint, _subPoint) = (_subPoint, _planPoint);
                (_planLook, _subLook) = (_subLook, _planLook);
                (_planConnect, _subConnect) = (_subConnect, _planConnect);
                _planCount = written;
            }

            return candidates;
        }

        // Banda k de la sección: de u0 a u1 en medios anchos (+1 = orilla izquierda mirando río abajo) y su color. «half» es el
        // medio ancho del agua en dp y «up», la componente vertical (local, hacia arriba) de la normal izquierda.
        private void Band(RunLook look, int k, float rim, float half, float up, out float u0, out float u1, out Color c)
        {
            if (_part == RibbonPart.Casing)
            {
                CasingBand(k, half, out u0, out u1, out c);
                return;
            }

            if (_part == RibbonPart.Core)
            {
                CoreBand(look, k, half, up, out u0, out u1, out c);
                return;
            }

            bool glass = _look == RibbonLook.Glass;
            if (!look.Split)
            {
                RiverWater w = glass ? RiverWater.None : look.A;
                switch (k)
                {
                    case 0:
                        u0 = -1f;
                        u1 = -1f + rim;
                        c = Palette.GlassRim;
                        return;
                    case 1:
                        u0 = -1f + rim;
                        u1 = 1f - rim;
                        c = BodyColor(w);
                        return;
                    default:
                        u0 = 1f - rim;
                        u1 = 1f;
                        c = Palette.GlassRim;
                        return;
                }
            }

            // Dos aguas: la derecha (B) de −1 a 0 y la izquierda (A) de 0 a 1, con el borde de vidrio por fuera.
            switch (k)
            {
                case 0:
                    u0 = -1f;
                    u1 = -1f + rim;
                    c = Palette.GlassRim;
                    return;
                case 1:
                    u0 = -1f + rim;
                    u1 = 0f;
                    c = BodyColor(look.B);
                    return;
                case 2:
                    u0 = 0f;
                    u1 = 1f - rim;
                    c = BodyColor(look.A);
                    return;
                default:
                    u0 = 1f - rim;
                    u1 = 1f;
                    c = Palette.GlassRim;
                    return;
            }
        }

        // El color del cuerpo: el agua o, sin agua comprobada, el vidrio con su opacidad (SetGlassBodyAlpha).
        private Color BodyColor(RiverWater water)
        {
            Color c = WaterColor(water);
            if (water == RiverWater.None)
            {
                c.a = _glassAlpha;
            }

            return c;
        }

        // La orla, por fuera del agua: filete, canto de vidrio de orilla a orilla (el agua va encima: sin rendijas) y filete.
        private static void CasingBand(int k, float half, out float u0, out float u1, out Color c)
        {
            float rimU = 1f + (CasingRimDp / half);
            float outU = 1f + (CasingDp / half);
            switch (k)
            {
                case 0:
                    u0 = -outU;
                    u1 = -rimU;
                    c = FilletColor;
                    return;
                case 1:
                    u0 = -rimU;
                    u1 = rimU;
                    c = Palette.GlassRim;
                    return;
                default:
                    u0 = rimU;
                    u1 = outU;
                    c = FilletColor;
                    return;
            }
        }

        // El agua sin borde y su reflejo: una banda de ReflectionDp (como mucho ReflectionMaxFraction del ancho del agua) a
        // ReflectionOffset del ancho hacia el borde de arriba; donde el río va en vertical, por el eje, para que la tira no salte de
        // orilla. Opaco: Glint al ReflectionAlpha sobre el agua de ese lado. Sin agua comprobada, sin reflejo (banda de ancho 0).
        private void CoreBand(RunLook look, int k, float half, float up, out float u0, out float u1, out Color c)
        {
            int last = look.Split ? 2 : 1;
            if (k < last)
            {
                if (!look.Split)
                {
                    u0 = -1f;
                    u1 = 1f;
                    c = BodyColor(look.A);
                    return;
                }

                u0 = k == 0 ? -1f : 0f;
                u1 = k == 0 ? 0f : 1f;
                c = BodyColor(k == 0 ? look.B : look.A);
                return;
            }

            float center = 2f * ReflectionOffset * Mathf.Clamp(up / 0.5f, -1f, 1f);
            RiverWater side = look.Split && center < 0f ? look.B : look.A;
            float w = side == RiverWater.None ? 0f : Mathf.Min(ReflectionDp, ReflectionMaxFraction * 2f * half) / half;
            u0 = center - (w * 0.5f);
            u1 = center + (w * 0.5f);
            c = ReflectionColor(side);
        }

        /// <summary>Filete de la orla: <see cref="Palette.UiInk"/> al <see cref="CasingFilletAlpha"/>.</summary>
        public static readonly Color FilletColor = new Color(Palette.UiInk.r, Palette.UiInk.g, Palette.UiInk.b, CasingFilletAlpha);

        /// <summary>Color del reflejo sobre el agua <paramref name="water"/>: <see cref="Palette.Glint"/> al <see cref="ReflectionAlpha"/> sobre ella, opaco.</summary>
        public static Color ReflectionColor(RiverWater water)
        {
            Color body = WaterColor(water);
            Color c = Color.Lerp(body, Palette.Glint, ReflectionAlpha);
            c.a = body.a;
            return c;
        }

        // Trama de Patrones de fruta (02 §12; propuesta del Mapa §11.4): una marca cada PatternStepDp, de 4 vértices, mientras
        // quede presupuesto. En las dos aguas, una marca por mitad. Con puntos saltados o con cambios de agua omitidos por el
        // presupuesto, las marcas van sobre las cuerdas que dibuja la cinta (la posición, por la longitud de arco entre las dos
        // secciones que las rodean) y con su aspecto, no con el agua del archivo, que la cinta ya no dibuja.
        private void AddPattern(VertexHelper vh, RiverLine line, PolylineCursor cursor, float aStart, float aEnd, float half)
        {
            float step = PatternStepDp / _space.DpPerUnit;
            int marks = Mathf.Min(MaxVertices / 4, Mathf.FloorToInt((aEnd - aStart) / step));
            int chord = 0;
            for (int index = 0; index < marks; index++)
            {
                float arc = aStart + (step * (index + 0.5f));
                RunLook look;
                Vector2 center;
                Vector2 dir;
                if ((_chords || _truncated) && _planCount >= 2)
                {
                    // La cuerda [chord, chord + 1] con arco[chord] ≤ arc < arco[chord + 1] (las secciones repetidas de un cambio de
                    // agua tienen el mismo arco y se pasan de largo).
                    while (chord + 2 < _planCount && _planArc[chord + 1] <= arc)
                    {
                        chord++;
                    }

                    float a0 = _planArc[chord];
                    float a1 = _planArc[chord + 1];
                    float t = a1 > a0 ? Mathf.Clamp01((arc - a0) / (a1 - a0)) : 0f;
                    Vector2 p0 = _planPoint[chord];
                    Vector2 p1 = _planPoint[chord + 1];
                    look = _planLook[chord + 1]; // la tira entre las dos secciones lleva el aspecto de la de llegada
                    center = _space.ToLocal(Vector2.LerpUnclamped(p0, p1, t));
                    dir = MapSpace.DirectionToLocal(TryChord(p0, p1, out Vector2 d) ? d : cursor.SegmentDirection(cursor.Locate(arc, out _)));
                }
                else
                {
                    look = LookAt(line, arc / cursor.Length, true);
                    int segment = cursor.Locate(arc, out _);
                    center = _space.ToLocal(cursor.PointAtArc(arc));
                    dir = MapSpace.DirectionToLocal(cursor.SegmentDirection(segment));
                }

                Vector2 normal = new Vector2(-dir.y, dir.x);
                if (look.Split)
                {
                    if (!AddMark(vh, look.A, center + (normal * (half * 0.5f)), dir, normal, half * 0.5f, index)
                        || !AddMark(vh, look.B, center - (normal * (half * 0.5f)), dir, normal, half * 0.5f, index))
                    {
                        return;
                    }
                }
                else if (!AddMark(vh, look.A, center, dir, normal, half, index))
                {
                    return;
                }
            }
        }

        // Una marca: punto (blanca), raya transversal (negra), onda (clara: trazos diagonales alternos que forman un zigzag) o
        // guion a lo largo del río (andina [P]: una línea discontinua por el centro, para que el paso de la andina a la blanca
        // no se distinga solo por el color). Devuelve false si ya no cabe en el presupuesto.
        private bool AddMark(VertexHelper vh, RiverWater water, Vector2 c, Vector2 dir, Vector2 normal, float room, int index)
        {
            if (water == RiverWater.None)
            {
                return true;
            }

            if (vh.currentVertCount + 4 > _budget)
            {
                return false;
            }

            Color32 mark = MarkColorWithAlpha(water);
            int b = vh.currentVertCount;
            switch (water)
            {
                case RiverWater.Black:
                {
                    Vector2 across = normal * (room * 0.65f);
                    Vector2 thick = dir * 0.4f;
                    vh.AddVert(c - across - thick, mark, Vector4.zero);
                    vh.AddVert(c + across - thick, mark, Vector4.zero);
                    vh.AddVert(c + across + thick, mark, Vector4.zero);
                    vh.AddVert(c - across + thick, mark, Vector4.zero);
                    break;
                }

                case RiverWater.Andean:
                {
                    Vector2 along = dir * (PatternStepDp * AndeanDashFraction * 0.5f);
                    Vector2 thick = normal * 0.4f;
                    vh.AddVert(c - along - thick, mark, Vector4.zero);
                    vh.AddVert(c + along - thick, mark, Vector4.zero);
                    vh.AddVert(c + along + thick, mark, Vector4.zero);
                    vh.AddVert(c - along + thick, mark, Vector4.zero);
                    break;
                }

                case RiverWater.Clear:
                {
                    float sign = (index & 1) == 0 ? 1f : -1f;
                    Vector2 a = c - (dir * (PatternStepDp * 0.5f)) + (normal * (room * 0.45f * sign));
                    Vector2 z = c + (dir * (PatternStepDp * 0.5f)) - (normal * (room * 0.45f * sign));
                    Vector2 stroke = z - a;
                    Vector2 side = stroke.sqrMagnitude > 1e-6f ? new Vector2(-stroke.y, stroke.x).normalized * 0.35f : normal * 0.35f;
                    vh.AddVert(a - side, mark, Vector4.zero);
                    vh.AddVert(a + side, mark, Vector4.zero);
                    vh.AddVert(z + side, mark, Vector4.zero);
                    vh.AddVert(z - side, mark, Vector4.zero);
                    break;
                }

                default:
                {
                    float r = Mathf.Min(0.9f, room * 0.45f);
                    vh.AddVert(c - (dir * r), mark, Vector4.zero);
                    vh.AddVert(c + (normal * r), mark, Vector4.zero);
                    vh.AddVert(c + (dir * r), mark, Vector4.zero);
                    vh.AddVert(c - (normal * r), mark, Vector4.zero);
                    break;
                }
            }

            vh.AddTriangle(b, b + 1, b + 2);
            vh.AddTriangle(b + 2, b + 3, b);
            _markCount++;
            return true;
        }

        private Color32 MarkColorWithAlpha(RiverWater water)
        {
            Color c = MarkColor(water, _patternOpacity);
            c.a *= color.a;
            return c;
        }

        private RunLook LookAt(RiverLine line, float s, bool water)
        {
            if (!water || _look == RibbonLook.Glass)
            {
                return default;
            }

            if (line.TryGetSplitAt(s, out RiverSplit split))
            {
                return new RunLook(true, split.Left, split.Right);
            }

            return new RunLook(false, line.WaterAt(s), RiverWater.None);
        }

        // Normal izquierda (mirando hacia s creciente) en coordenadas locales de una dirección del marco.
        private static Vector2 Normal(Vector2 mapDirection)
        {
            Vector2 d = MapSpace.DirectionToLocal(mapDirection);
            return new Vector2(-d.y, d.x);
        }

        // Inglete entre las normales de llegada y de salida, en medios anchos, limitado a MiterLimit.
        private static Vector2 Miter(Vector2 nIn, Vector2 nOut)
        {
            Vector2 m = nIn + nOut;
            if (m.sqrMagnitude < 1e-6f)
            {
                return nOut; // media vuelta: sin inglete
            }

            m.Normalize();
            float cos = Vector2.Dot(m, nOut);
            return m * (1f / Mathf.Max(cos, 1f / MiterLimit));
        }

        // Aspecto de un tramo de la cinta: un agua o dos lado a lado (A a la izquierda y B a la derecha, mirando río abajo).
        private readonly struct RunLook
        {
            public RunLook(bool split, RiverWater a, RiverWater b)
            {
                Split = split;
                A = a;
                B = b;
            }

            public bool Split { get; }

            public RiverWater A { get; }

            public RiverWater B { get; }

            public bool Equals(RunLook other)
            {
                return Split == other.Split && A == other.A && B == other.B;
            }
        }
    }
}
