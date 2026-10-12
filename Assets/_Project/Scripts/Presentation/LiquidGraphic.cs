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
    /// vidrio (brillos y recorte). <c>TEXCOORD2.y</c> lleva la amplitud de la onda y, en su parte entera, el destello del
    /// sellado en pasos de 1/<see cref="FlashPackSteps"/> (<see cref="PackSurface"/>): así <c>TEXCOORD1.w</c> queda libre
    /// para la x del «?» o del icono de fruta en cualquier banda.</para>
    /// <para><b>Patrón</b> (07 §3, regla 1; T-E): la opacidad es la propiedad <c>_PatternOpacity</c> del material compartido,
    /// que se fija por código al cargar el nivel (<see cref="ApplyPatternOpacity"/>, desde <c>BoardAnimator.ApplyLevelPatterns</c>:
    /// 0,15 hasta el 40 y 0,20 desde el 41, <see cref="Palette.PatternOpacityFor"/>); nunca se edita un <c>.mat</c>.</para>
    /// <para><b>Icono de fruta</b> (07 §3, reglas 1 y 2; T-E.4 y T-E.5): con <see cref="SetFruitIcons"/> (Patrones de fruta
    /// desde el 41, o la cuia elevada en cualquier nivel), el shader pinta en cada dosis visible el icono de accesibilidad de
    /// su tinte (<see cref="ProceduralArt.FruitIconAtlas"/>, en <c>_IconAtlas</c>) del color <see cref="Palette.PatternBase"/>
    /// al <see cref="FruitIconOpacity"/> [P], con la silueta de 0,45 u en reposo. Va como el «?»: derecho en pantalla, en la
    /// columna de su banda con más hueco (<see cref="PlaceGlyph"/>; su sitio, empaquetado sobre el alto en <c>TEXCOORD0.w</c>,
    /// y su x por vértice en <c>TEXCOORD1.w</c>), encoge con el hueco y se apaga si no cabe, así que sigue a la banda al
    /// inclinarse y al llenarse o vaciarse en el vertido. Nunca en una dosis oculta ni mientras se revela (el icono de fruta
    /// de la Luciérnaga de <c>MurkyMarks</c> manda hasta que la banda vuelve a ser normal), ni en el Barro (mate), ni en el
    /// agua base (tinte 0) (<see cref="FruitIconDrawn"/>). Un vertido o un cambio de dosis reconstruyen la malla como siempre;
    /// el icono no asigna memoria.</para>
    /// <para><b>Aguas turbias</b> (07 §4.3; 08 §5.2; T15): una dosis oculta se pinta en el shader, no con sprites encima, para
    /// que la trama y el «?» sigan a su banda cuando la cuia se inclina. Con <see cref="SetDose(int, Color, float, int, MurkyLook, float)"/>
    /// en un modo turbio la banda lleva el patrón <see cref="MurkyPatternMode"/> (± el avance del revelado, ver
    /// <see cref="MurkyLook"/>) y la fila turbia del atlas (<see cref="ProceduralArt.MurkyPatternRow"/>) en
    /// <c>TEXCOORD3.a</c>; el shader pinta encima del pardo la trama diagonal al 25 % y el «?» marfil de 0,5 u, sin la
    /// mezcla de los patrones de fruta (<c>_PatternOpacity</c>). En los dos revelados hay un fundido cruzado sin saltos: <c>TEXCOORD3.a</c>
    /// pasa a ser la fila de la fruta que se revela (<see cref="FruitRowCode"/>; la turbia la da <c>_MurkyRow</c>), la trama se
    /// va antes de que entre el patrón de la fruta (<see cref="MurkyHatchAmount"/> y <see cref="FruitPatternWeight"/>, espejos
    /// en CPU del shader) y la franja y el menisco de la fruta entran con su patrón; al final la banda es su dosis normal. Con
    /// la Luciérnaga (<see cref="MurkyLook.Turn"/>) además baja por la banda un frente de luz [P] (<see cref="FireflyLightAt"/>):
    /// encima, el color de la dosis, que la CPU florece en OKLab; debajo, el pardo y su brillo (<c>_MurkyLin</c>,
    /// <c>_MurkyLitLin</c>); el shader solo mezcla en lineal en el borde, de <see cref="FireflyEdgeU"/>.</para>
    /// <para>[P] Esto sustituye a la máscara <c>_HiddenMask</c> (una banda por dosis, también no contiguas) y al color
    /// <c>_MurkyColor</c> = #8A7B6A (<see cref="Palette.Murky"/>, el color de la banda) de 08 §5.2, que no pueden ir como
    /// propiedades porque uGUI no admite <c>MaterialPropertyBlock</c> por cuia y todas comparten material; y el «?» lo pinta
    /// el shader con la fila turbia del atlas en lugar del «sprite "?"» de 08 §5.2. Se alinea con el GDD en el paso 2.</para>
    /// <para>El «?» va derecho en pantalla e isótropo (<see cref="MurkyGlyphPlacement"/>): su x en pantalla, relativa a su
    /// centro y en u, va por vértice en <c>TEXCOORD1.w</c> (en las bandas sin marca, 0: el destello del sellado va en
    /// <c>TEXCOORD2.y</c>); su centro en capa y el hueco libre en su columna van empaquetados sobre el alto de la cuia en
    /// <c>TEXCOORD0.w</c> (<see cref="GlyphPackScale"/>: en las bandas sin «?» ni icono de fruta no cambia nada). Mide
    /// <see cref="GlyphSizeU"/> (de <see cref="GlyphMinU"/> a <see cref="GlyphMaxU"/>) y se apaga si la banda es más fina
    /// (<see cref="GlyphFade"/>): con la cuia inclinada busca la parte más gruesa de la cuña sin tocar el vidrio.</para>
    /// <para><b>Acabado mate</b> (el Barro: 02 §9.1 M5; T16, «opaco, con grumos y sin brillo»; lo pide
    /// <c>MudMarks.PaintMud</c> con <see cref="SetMatteDose"/>): la banda lleva el patrón <see cref="MattePatternMode"/>, sin
    /// patrón como −1, y el shader no le pinta ninguno de los brillos de la guía 4.2: ni la franja principal, ni la secundaria,
    /// ni el brillo vertical, ni, si es la de arriba, la línea clara del borde de su cara superior, que además va de su color,
    /// sin el aclarado del menisco. Su franja (<c>TEXCOORD1.rgb</c>) es su color. Conserva el degradado de ±4 % de L y el
    /// oscurecido lateral, que dan volumen y no brillo. Cualquier otro <c>SetDose</c> de esa dosis le devuelve el brillo.</para>
    /// </remarks>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LiquidGraphic : MaskableGraphic
    {
        /// <summary>
        /// Patrón de una banda turbia en <c>TEXCOORD2.w</c> (patrón + 4 · dosis): 2 sin animar; 2 + 0,45 · avance al
        /// disolverse por la regla y 2 − 0,45 · avance con la Luciérnaga (el shader decodifica el modo con
        /// <c>floor((w + 1,5) / 4)</c>, así que el patrón vive en [−1,5; 2,5)). Shader: <c>AF_MURKY_MODE</c>.
        /// </summary>
        public const float MurkyPatternMode = 2f;

        /// <summary>
        /// Patrón de una banda mate en <c>TEXCOORD2.w</c> (patrón + 4 · dosis; <see cref="SetMatteDose"/>, el Barro): −1,25,
        /// sin patrón como −1 y además sin brillos (ver la clase). El shader lo separa de −1 por su punto medio
        /// (<see cref="IsMattePattern"/>). Shader: <c>AF_MATTE_MODE</c>.
        /// </summary>
        public const float MattePatternMode = -1.25f;

        /// <summary>Lo que se aparta del <see cref="MurkyPatternMode"/> el avance 1 de un revelado (shader: <c>AF_MURKY_SPAN</c>).</summary>
        public const float MurkyProgressSpan = 0.45f;

        /// <summary>Avance de <see cref="MurkyLook.Dissolve"/> en el que el «?» empieza a apagarse (shader: <c>AF_MARK_FADE_FROM</c>).</summary>
        public const float MarkFadeFrom = 0.35f;

        /// <summary>Avance de <see cref="MurkyLook.Dissolve"/> en el que el «?» ya no se ve (shader: <c>AF_MARK_FADE_TO</c>).</summary>
        public const float MarkFadeTo = 0.85f;

        /// <summary>Halo de tinta del «?»: 55 % (glosario §6.2; shader: <c>AF_MARK_HALO</c>).</summary>
        public const float MarkHaloOpacity = 0.55f;

        /// <summary>
        /// Avance de <see cref="MurkyLook.Dissolve"/> en el que el halo de tinta del «?» empieza a irse [P] (shader:
        /// <c>AF_MARK_HALO_FADE_FROM</c>): la tinta se va primero, así el «?» que se disuelve no queda gris sobre la fruta.
        /// </summary>
        public const float MarkHaloFadeFrom = 0.15f;

        /// <summary>Avance de <see cref="MurkyLook.Dissolve"/> en el que el halo ya no se ve [P] (shader: <c>AF_MARK_HALO_FADE_TO</c>).</summary>
        public const float MarkHaloFadeTo = 0.35f;

        /// <summary>
        /// Avance de <see cref="MurkyLook.Dissolve"/> en el que el marfil del «?» empieza a ir hacia el blanco [P] (shader:
        /// <c>AF_MARK_WHITE_FROM</c>; ficha de Magic Sort §3.1: se disuelve en destellos blancos, no hacia la banda).
        /// </summary>
        public const float MarkWhiteFrom = 0.2f;

        /// <summary>Avance de <see cref="MurkyLook.Dissolve"/> en el que el «?» ya es blanco [P] (shader: <c>AF_MARK_WHITE_TO</c>).</summary>
        public const float MarkWhiteTo = 0.6f;

        /// <summary>Amplitud de la ondulación del «?» al disolverse (u, con el «?» de 0,5 u) [P] (shader: <c>AF_MARK_WOBBLE_U</c>).</summary>
        public const float MarkWobbleU = 0.025f;

        /// <summary>Trama diagonal de una dosis turbia: 25 % hacia la tinta (07 §4.3; shader: <c>AF_HATCH_OPACITY</c>).</summary>
        public const float MurkyHatchOpacity = 0.25f;

        /// <summary>
        /// Avance de <see cref="MurkyLook.Dissolve"/> en el que la trama empieza a irse (shader: <c>AF_HATCH_FADE_FROM</c>) [P]:
        /// la trama se va mientras la dosis toma su tinte y antes de que entre el patrón de su fruta.
        /// </summary>
        public const float HatchFadeFrom = 0.18f;

        /// <summary>Avance de <see cref="MurkyLook.Dissolve"/> en el que la trama ya no se ve (shader: <c>AF_HATCH_FADE_TO</c>).</summary>
        public const float HatchFadeTo = 0.55f;

        /// <summary>
        /// Avance de <see cref="MurkyLook.Dissolve"/> en el que el patrón de la fruta que se revela empieza a entrar (shader:
        /// <c>AF_FRUIT_PATTERN_FROM</c>, hasta <see cref="FruitPatternTo"/>) [P]: después de <see cref="HatchFadeTo"/> (la misma
        /// muestra lee la trama o el patrón, nunca los dos).
        /// </summary>
        public const float FruitPatternFrom = 0.64f;

        /// <summary>
        /// Avance de <see cref="MurkyLook.Dissolve"/> en el que el patrón de la fruta ya entró entero (shader:
        /// <c>AF_FRUIT_PATTERN_TO</c>): el final del Dissolve, así que el paso a la dosis normal no salta.
        /// </summary>
        public const float FruitPatternTo = 1f;

        /// <summary>
        /// Luciérnaga: parte del avance de una banda en la que su frente de luz la recorre de arriba abajo [P] (shader:
        /// <c>AF_FIREFLY_FRONT_SPAN</c>); en el resto entra el patrón de su fruta.
        /// </summary>
        public const float FireflyFrontSpan = 0.5f;

        /// <summary>Luciérnaga: ancho del borde suave del frente de luz (u de capa; ≤ 0,15 u) [P] (shader: <c>AF_FIREFLY_EDGE_U</c>).</summary>
        public const float FireflyEdgeU = 0.12f;

        /// <summary>
        /// Luciérnaga: profundidad del frente (desde arriba de la banda) en la que su «?» empieza a girar, la de lo alto del
        /// «?» [P] (shader: <c>AF_FIREFLY_TURN_FROM</c>).
        /// </summary>
        public const float FireflyTurnFrom = 0.25f;

        /// <summary>
        /// Luciérnaga: profundidad del frente en la que el «?» queda de canto (su centro; 04 §2.5: «cada "?" gira») [P]
        /// (shader: <c>AF_FIREFLY_TURN_TO</c>). Desde ahí gira de vuelta el icono de su fruta (<c>MurkyMarks</c>).
        /// </summary>
        public const float FireflyTurnTo = 0.5f;

        /// <summary>
        /// Luciérnaga: giro (<see cref="FireflyTurnAt"/>, 0 de frente y 1 de canto) por encima del cual el «?» ya está de canto
        /// y no se pinta (shader: <c>AF_FIREFLY_TURN_OFF</c>, en <c>step(turn, …)</c>): ahí la insignia baja en uno.
        /// </summary>
        public const float FireflyTurnOff = 0.999f;

        /// <summary>
        /// Luciérnaga: avance de la banda en el que empieza a entrar el patrón de su fruta [P] (shader:
        /// <c>AF_FIREFLY_PATTERN_FROM</c>): después de <see cref="FireflyFrontSpan"/>, cuando el frente ya se llevó la trama.
        /// </summary>
        public const float FireflyPatternFrom = 0.55f;

        /// <summary>Luciérnaga: avance en el que el patrón de la fruta ya entró entero [P] (shader: <c>AF_FIREFLY_PATTERN_TO</c>).</summary>
        public const float FireflyPatternTo = 0.85f;

        /// <summary>«?» de una banda turbia: alto mínimo en pantalla (u) [P] (shader: <c>AF_GLYPH_MIN_U</c>).</summary>
        public const float GlyphMinU = 0.30f;

        /// <summary>«?» de una banda turbia: alto máximo, el de 07 §4.3 en reposo (u; shader: <c>AF_GLYPH_MAX_U</c>).</summary>
        public const float GlyphMaxU = 0.50f;

        /// <summary>«?»: alto como fracción del hueco libre de su columna (margen arriba y abajo) [P] (shader: <c>AF_GLYPH_ROOM_RATIO</c>).</summary>
        public const float GlyphRoomRatio = 0.8f;

        /// <summary>
        /// «?»: hueco (u de pantalla) por debajo del cual se apaga del todo; un «?» de <see cref="GlyphMinU"/> ya no cabe [P]
        /// (shader: <c>AF_GLYPH_FADE_FROM</c>).
        /// </summary>
        public const float GlyphFadeFrom = 0.30f;

        /// <summary>«?»: hueco desde el que se ve entero [P] (shader: <c>AF_GLYPH_FADE_TO</c>).</summary>
        public const float GlyphFadeTo = 0.34f;

        /// <summary>
        /// Empaquetado del sitio del «?» sobre el alto de la cuia en <c>TEXCOORD0.w</c> (shader: <c>AF_GLYPH_PACK_SCALE</c>):
        /// alto + 16 · (centro + <see cref="GlyphCenterSteps"/> · hueco), con el alto siempre por debajo de 16 u.
        /// </summary>
        public const float GlyphPackScale = 16f;

        /// <summary>Pasos del centro del «?» en el empaquetado (shader: <c>AF_GLYPH_CENTER_STEPS</c>).</summary>
        public const float GlyphCenterSteps = 80f;

        /// <summary>Pasos por u de capa del centro del «?» (shader: <c>AF_GLYPH_CENTER_PER_U</c>; 0,3 en reposo es exacto).</summary>
        public const float GlyphCenterPerU = 40f;

        /// <summary>Desplazamiento del centro empaquetado (u de capa): el centro va de −0,5 a 1,475 (shader: <c>AF_GLYPH_CENTER_OFFSET</c>).</summary>
        public const float GlyphCenterOffset = 0.5f;

        /// <summary>Pasos por u de pantalla del hueco del «?» (shader: <c>AF_GLYPH_ROOM_PER_U</c>; 1 en reposo es exacto).</summary>
        public const float GlyphRoomPerU = 50f;

        /// <summary>
        /// Cómo pinta el shader una dosis (07 §4.3; 08 §5.2): normal o turbia, y en este caso si su «?» está quieto, se
        /// disuelve (revelado por la regla, 1.200 ms) o gira de canto con el frente de luz de la Luciérnaga (04 §2.5).
        /// </summary>
        public enum MurkyLook : byte
        {
            /// <summary>Dosis normal: su color, su tinte y el patrón de su fruta.</summary>
            None = 0,

            /// <summary>Oculta: pardo opaco, trama diagonal al 25 % y «?» marfil de 0,5 u, sin tinte ni patrón de fruta.</summary>
            Hidden = 1,

            /// <summary>
            /// Revelado por la regla (R7), avance 0..1: la trama se va (<see cref="HatchFadeFrom"/>-<see cref="HatchFadeTo"/>),
            /// el «?» crece, ondula, se blanquea y se disuelve (<see cref="MarkFadeFrom"/>-<see cref="MarkFadeTo"/>) y entra el
            /// patrón de la fruta que se revela (<see cref="FruitPatternFrom"/>-1), con su franja y su menisco: al avance 1 es
            /// su dosis normal.
            /// </summary>
            Dissolve = 2,

            /// <summary>
            /// Revelado con la Luciérnaga (04 §2.5), avance 0..1: un frente de luz baja por la banda (hasta
            /// <see cref="FireflyFrontSpan"/>) y se lleva el pardo y la trama, el «?» gira de canto cuando el frente pasa por su
            /// centro y después entra el patrón de la fruta (<see cref="FireflyPatternFrom"/>-<see cref="FireflyPatternTo"/>),
            /// con su franja y su menisco: al avance 1 es su dosis normal.
            /// </summary>
            Turn = 3,
        }

        /// <summary>
        /// Opacidad del icono de fruta sobre su dosis (T-E.4) [P]: el color <see cref="Palette.PatternBase"/> del tinte casi
        /// opaco, mezclado como el patrón (shader: <c>AF_ICON_OPACITY</c>).
        /// </summary>
        public const float FruitIconOpacity = 0.9f;

        /// <summary>
        /// Pasos del destello del sellado en la parte entera de <c>TEXCOORD2.y</c> (<see cref="PackSurface"/>; shader:
        /// <c>AF_FLASH_STEPS</c>): el destello de 0 a 0,15 en pasos de 1/256.
        /// </summary>
        public const float FlashPackSteps = 256f;

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

        // Sitio del «?» con la cuia inclinada: columnas de pantalla cada medio ancho del «?», el medio ancho del «?» con su halo
        // a 0,5 u (≈ 0,18 u) y lo que se acepta perder de hueco para quedarse más cerca del centro de la cuerda [P].
        private const int GlyphColumns = 112;
        private const float GlyphHalfWidthU = 0.18f;
        private const float GlyphRoomTolerance = 0.06f;

        private static readonly int DpUId = Shader.PropertyToID("_DpU");
        private static readonly int PatternOpacityId = Shader.PropertyToID("_PatternOpacity");
        private static readonly int IconAtlasId = Shader.PropertyToID("_IconAtlas");
        private static readonly int PatternRowsId = Shader.PropertyToID("_PatternRows");
        private static readonly int MurkyRowId = Shader.PropertyToID("_MurkyRow");
        private static readonly int InkId = Shader.PropertyToID("_InkLin");
        private static readonly int PaperId = Shader.PropertyToID("_PaperLin");
        private static readonly int MurkyId = Shader.PropertyToID("_MurkyLin");
        private static readonly int MurkyLitId = Shader.PropertyToID("_MurkyLitLin");
        private static readonly int CavityBottomId = Shader.PropertyToID("_AFCavityBottomU");
        private static readonly int CavityHalfId = Shader.PropertyToID("_AFCavityHalfU");
        private static readonly int NeckHalfId = Shader.PropertyToID("_AFNeckHalfU");
        private static readonly int ShoulderId = Shader.PropertyToID("_AFShoulderU");
        private static readonly int AboveCavityId = Shader.PropertyToID("_AFAboveCavityU");
        private static readonly int RimId = Shader.PropertyToID("_AFRimU");
        private static readonly int TopFaceId = Shader.PropertyToID("_AFTopFaceU");

        // Silueta interior (cavidad, hombro y cuello hasta el labio) por capacidad, en X = x − 1 e y (u): se crea una vez.
        private static readonly Vector2[]?[] s_silhouettes = new Vector2[MaxCapacity + 1][];

        // Columnas para el sitio del «?» (solo el hilo principal; sin asignaciones).
        private static readonly float[] s_columnBottom = new float[GlyphColumns + 1];
        private static readonly float[] s_columnTop = new float[GlyphColumns + 1];

        // Polígono de una banda: el rectángulo de la cavidad recortado por dos semiplanos (≤ 6 vértices) y un búfer.
        private readonly Vector2[] _band = new Vector2[8];
        private readonly Vector2[] _bandTmp = new Vector2[8];
        private Color[] _color = Array.Empty<Color>();
        private float[] _fill = Array.Empty<float>();
        private int[] _tint = Array.Empty<int>();
        private MurkyLook[] _look = Array.Empty<MurkyLook>();
        private float[] _lookProgress = Array.Empty<float>();
        private bool[] _matte = Array.Empty<bool>();
        private float[] _levels = Array.Empty<float>();
        private Texture? _atlas;
        private int _capacity;
        private float _unit = 24f;
        private float _rippleMs = -1f;
        private float _ripplePhase;
        private bool _rippleDriven;
        private float _flash;
        private bool _fruitIcons;
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

        /// <summary>
        /// Brillo (franja principal) de una dosis turbia: el pardo con +10 L*, como cualquier banda sin tinte. Es el que lleva
        /// una dosis oculta y el que queda bajo el frente de la Luciérnaga (<c>_MurkyLitLin</c>).
        /// </summary>
        public static Color MurkyHighlight => ColorMath.AddLabLightness(Palette.Murky, 10f);

        /// <summary>
        /// Prepara el gráfico para <paramref name="capacity"/> dosis con el atlas de patrones y el de iconos de fruta
        /// (<see cref="ProceduralArt.FruitIconAtlas"/>; sin él, los iconos no se ven). Deja en el material compartido la
        /// opacidad del patrón del nivel cargado (<see cref="Palette.PatternOpacity"/>).
        /// </summary>
        public void Setup(int capacity, Texture atlas, Texture? iconAtlas = null)
        {
            _capacity = capacity;
            _color = new Color[capacity];
            _fill = new float[capacity];
            _tint = new int[capacity];
            _look = new MurkyLook[capacity];
            _lookProgress = new float[capacity];
            _matte = new bool[capacity];
            _levels = new float[capacity + 1];
            _atlas = atlas;
            raycastTarget = false;
            ApplyShaderGeometry();
            Material? mat = UiMaterials.Liquid;
            if (mat != null)
            {
                material = mat;
                mat.SetFloat(PatternRowsId, atlas.height / (float)ProceduralArt.PatternRowPx);
                mat.SetFloat(MurkyRowId, ProceduralArt.MurkyPatternRow); // la trama y el «?» (al revelarse, TEXCOORD3.a es la fruta)
                mat.SetFloat(PatternOpacityId, Palette.PatternOpacity); // 07 §3, regla 1: la del nivel (ApplyPatternOpacity)
                if (iconAtlas != null)
                {
                    mat.SetTexture(IconAtlasId, iconAtlas); // iconos de accesibilidad por tinte (07 §3; T-E.3)
                }

                // Ya en el espacio del shader: van con SetVector, que no convierte. SetColor los volvía a pasar a lineal en
                // el proyecto Linear (doble conversión): el marfil llegaba como #E7D4B3 en vez de #F4EBDA (el «?» de las
                // turbias y las marcas claras de los patrones) y la tinta casi negra, lejos de Palette.PatternMark.
                SetShaderColor(mat, InkId, Palette.UiInk); // marcas sobre los tintes claros y halo del «?»
                SetShaderColor(mat, PaperId, Palette.UiPaper); // marcas sobre los tintes oscuros y «?» turbio
                SetShaderColor(mat, MurkyId, Palette.Murky); // el pardo bajo el frente de la Luciérnaga (_MurkyColor de 08 §5.2 [P])
                SetShaderColor(mat, MurkyLitId, MurkyHighlight); // y su brillo
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

        /// <summary>
        /// Opacidad del patrón del nivel (07 §3, regla 1; 08 §9 <c>ui_pattern_opacity</c>; T-E.1 y T-E.2): la fija en la
        /// propiedad <c>_PatternOpacity</c> del material compartido del líquido (todas las cuias del nivel) y en
        /// <see cref="Palette.PatternOpacity"/> (<see cref="Palette.PatternMark"/>, el bocado de la Piraña). Por código al cargar
        /// el nivel, como <c>_DpU</c>; nunca en un <c>.mat</c>. Sin asignaciones.
        /// </summary>
        public static void ApplyPatternOpacity(float opacity)
        {
            Palette.SetPatternOpacity(opacity);
            Material? mat = UiMaterials.Liquid;
            if (mat != null)
            {
                mat.SetFloat(PatternOpacityId, Palette.PatternOpacity);
            }
        }

        /// <summary>
        /// Opacidad del patrón que pinta el shader: la propiedad <c>_PatternOpacity</c> del material compartido (sin material,
        /// <see cref="Palette.PatternOpacity"/>). Para la sonda de desarrollo y los tests.
        /// </summary>
        public static float ShaderPatternOpacity
        {
            get
            {
                Material? mat = UiMaterials.Liquid;
                return mat != null ? mat.GetFloat(PatternOpacityId) : Palette.PatternOpacity;
            }
        }

        /// <summary>
        /// Icono de fruta en las dosis visibles (07 §3, reglas 1 y 2; ver la clase): lo pide la vista con Patrones de fruta en
        /// el nivel o con la cuia elevada. Solo reconstruye la malla si cambia.
        /// </summary>
        public void SetFruitIcons(bool on)
        {
            if (on != _fruitIcons)
            {
                _fruitIcons = on;
                SetVerticesDirty();
            }
        }

        /// <summary>El gráfico pinta el icono de fruta en sus dosis visibles (<see cref="SetFruitIcons"/>).</summary>
        public bool FruitIcons => _fruitIcons;

        /// <summary>
        /// La dosis <paramref name="k"/> es de las que llevan icono con <see cref="SetFruitIcons"/>: normal (ni oculta ni
        /// revelándose), no mate (el Barro) y con uno de los 14 tintes (el agua base, tinte 0, no). No mira el hueco.
        /// </summary>
        public bool IsFruitIconDose(int k)
        {
            return (uint)k < (uint)_capacity && _look[k] == MurkyLook.None && !_matte[k] && ProceduralArt.HasFruitIcon(_tint[k]);
        }

        /// <summary>
        /// El shader dibuja el icono de fruta de la dosis <paramref name="k"/>: iconos encendidos, dosis con icono
        /// (<see cref="IsFruitIconDose"/>), con banda y con hueco para él (<see cref="GlyphFade"/> del hueco cuantizado de la
        /// malla, y el sitio empaquetado distinto de 0, como lo decodifica el shader). Sin asignaciones.
        /// </summary>
        public bool FruitIconDrawn(int k)
        {
            if (!_fruitIcons || !IsFruitIconDose(k) || _fill[k] <= 0.001f || _color[k].a <= 0.001f)
            {
                return false;
            }

            return FruitIconPlacement(k, out _, out _, out _, out float room, out int code) && code > 0 && GlyphFade(room) > 0.001f;
        }

        /// <summary>
        /// Sitio del icono de fruta de la dosis <paramref name="k"/> tal como lo pinta el shader (con la inclinación de ahora;
        /// el mismo que el del «?», <see cref="MurkyGlyphPlacement"/>): centro en coordenadas de la cuia (X = x − 1 e y, u),
        /// alto de la silueta en pantalla (<see cref="FruitIconSizeU"/>: 0,45 u en reposo) y hueco libre en su columna, ya
        /// cuantizado como lo lleva la malla. False si la dosis no lleva icono (<see cref="IsFruitIconDose"/>) o no tiene banda;
        /// no mira si los iconos están encendidos. Sin asignaciones.
        /// </summary>
        public bool FruitIconPlacement(int k, out float centerX, out float centerY, out float sizeU, out float roomU)
        {
            return FruitIconPlacement(k, out centerX, out centerY, out sizeU, out roomU, out _);
        }

        /// <summary>
        /// Alto (u de pantalla) de la silueta del icono de fruta de una banda con <paramref name="roomU"/> de hueco: su caja es la
        /// del «?» (<see cref="GlyphSizeU"/>, de 0,30 a 0,50 u) y la silueta ocupa <see cref="ProceduralArt.FruitIconFill"/> de
        /// ella: 0,45 u con la dosis llena (07 §3, regla 1).
        /// </summary>
        public static float FruitIconSizeU(float roomU)
        {
            return GlyphSizeU(roomU) * ProceduralArt.FruitIconFill;
        }

        /// <summary>
        /// <c>TEXCOORD2.y</c> de una banda (ver la clase): el destello del sellado (0..1, en la práctica ≤ 0,15) en pasos de
        /// 1/<see cref="FlashPackSteps"/> en la parte entera y la amplitud de la onda (u, &lt; 0,75) en la fraccionaria.
        /// </summary>
        public static float PackSurface(float amplitudeU, float flash)
        {
            return Mathf.Round(Mathf.Clamp01(flash) * FlashPackSteps) + Mathf.Clamp(amplitudeU, 0f, 0.5f);
        }

        /// <summary>Decodifica <see cref="PackSurface"/> como el shader: floor(y + 0,25) pasos de destello y el resto, la onda.</summary>
        public static void UnpackSurface(float packed, out float amplitudeU, out float flash)
        {
            float steps = Mathf.Floor(packed + 0.25f);
            amplitudeU = Mathf.Max(packed - steps, 0f);
            flash = steps / FlashPackSteps;
        }

        /// <summary>Fija la dosis <paramref name="k"/> (color, relleno 0..1, tinte; 0 = agua base), sin modo turbio.</summary>
        public void SetDose(int k, Color color, float fill, int tint)
        {
            SetDose(k, color, fill, tint, MurkyLook.None, 0f);
        }

        /// <summary>
        /// Fija la dosis <paramref name="k"/> con el modo turbio <paramref name="look"/> (07 §4.3; ver <see cref="MurkyLook"/>):
        /// oculta, el tinte no se usa (va 0: ni su franja, ni su menisco, ni su patrón delatan la fruta oculta); al revelarse
        /// (por la regla o con la Luciérnaga), <paramref name="tint"/> es el de la fruta que se revela, cuyo patrón, franja y
        /// menisco entran con <see cref="FruitPatternWeight"/>. <paramref name="progress"/> (0..1) es el avance de
        /// <see cref="MurkyLook.Dissolve"/> o <see cref="MurkyLook.Turn"/>. Solo reconstruye la malla si algo cambia; sin
        /// asignaciones.
        /// </summary>
        public void SetDose(int k, Color color, float fill, int tint, MurkyLook look, float progress = 0f)
        {
            if (look == MurkyLook.None || look == MurkyLook.Hidden)
            {
                progress = 0f;
            }
            else
            {
                progress = Mathf.Clamp01(progress);
            }

            if (look == MurkyLook.Hidden)
            {
                tint = 0;
            }

            Store(k, color, fill, tint, look, progress, false);
        }

        /// <summary>
        /// Fija la dosis <paramref name="k"/> (color y relleno 0..1) sin tinte ni patrón y con acabado mate (el Barro: 02 §9.1
        /// M5; T16, «opaco, con grumos y sin brillo»; lo usa <c>MudMarks.PaintMud</c>): el shader no le pinta brillos y, si
        /// es la de arriba, su cara superior va de su color (ver la clase). Cualquier otro <c>SetDose</c> de la dosis le
        /// devuelve el brillo. Solo reconstruye la malla si algo cambia; sin asignaciones.
        /// </summary>
        public void SetMatteDose(int k, Color color, float fill)
        {
            Store(k, color, fill, 0, MurkyLook.None, 0f, true);
        }

        /// <summary>La dosis <paramref name="k"/> se pinta mate (<see cref="SetMatteDose"/>; false fuera de rango).</summary>
        public bool IsMatteAt(int k)
        {
            return (uint)k < (uint)_capacity && _matte[k];
        }

        /// <summary>
        /// El shader pinta mate, sin brillos, una banda con el patrón <paramref name="pattern"/> (<c>TEXCOORD2.w</c> − 4 ·
        /// dosis): el de <see cref="MattePatternMode"/>, por debajo del punto medio entre él y −1 (sin patrón). Espejo del
        /// shader («gloss»).
        /// </summary>
        public static bool IsMattePattern(float pattern)
        {
            return pattern < 0.5f * (MattePatternMode - 1f);
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

        /// <summary>Tinte de la dosis <paramref name="k"/> tal como llega al shader (0 en una oculta; fuera de rango, 0).</summary>
        public int DoseTintAt(int k)
        {
            return (uint)k < (uint)_capacity ? _tint[k] : 0;
        }

        /// <summary>
        /// Espejo en CPU del color de base que pinta el shader a la profundidad <paramref name="depth"/> de la banda
        /// <paramref name="k"/> (0 arriba, 1 abajo, entre sus arcos), sin degradado, brillos ni marcas: con la Luciérnaga
        /// (<see cref="MurkyLook.Turn"/>), la mezcla del frente entre el pardo y el color de la dosis
        /// (<see cref="FireflyLightAt"/>), en el espacio del shader (lineal en el proyecto Linear); si no, el color de la dosis.
        /// </summary>
        public Color PaintedColorAt(int k, float depth)
        {
            if ((uint)k >= (uint)_capacity)
            {
                return Color.clear;
            }

            Color c = _color[k];
            if (_look[k] != MurkyLook.Turn)
            {
                return c;
            }

            return MixAsShader(Palette.Murky, c, FireflyLightAt(_lookProgress[k], depth));
        }

        /// <summary>
        /// Espejo en CPU del color que pinta el shader en el centro de la franja especular principal de la banda
        /// <paramref name="k"/>, a la profundidad <paramref name="depth"/> (0 arriba, 1 abajo) y, como
        /// <see cref="PaintedColorAt"/>, sin degradado, oscurecido lateral ni marcas: el de su franja (<c>TEXCOORD1.rgb</c>, guía
        /// 4.2: <see cref="Palette.LiquidHighlight"/>, o +10 L* sin tinte; con la Luciérnaga, mezclado por el frente con el
        /// brillo del pardo, <see cref="MurkyHighlight"/>); en una dosis mate (<see cref="SetMatteDose"/>, el Barro), el de base
        /// (<see cref="PaintedColorAt"/>), porque el shader no le pinta brillos. Mezcla como el shader (en lineal en el proyecto
        /// Linear) y devuelve sRGB.
        /// </summary>
        public Color PaintedHighlightAt(int k, float depth)
        {
            if ((uint)k >= (uint)_capacity)
            {
                return Color.clear;
            }

            if (_matte[k])
            {
                return PaintedColorAt(k, depth);
            }

            Color lit = StripeColor(k);
            return _look[k] == MurkyLook.Turn ? MixAsShader(MurkyHighlight, lit, FireflyLightAt(_lookProgress[k], depth)) : lit;
        }

        /// <summary>
        /// Opacidad (0..1) del «?» que pinta el shader en una banda turbia con <paramref name="look"/> y
        /// <paramref name="progress"/>, sin contar el hueco de su banda (<see cref="GlyphFade"/>): el mismo cálculo que
        /// <c>AF_UI_Liquid.shader</c> («fade»). Quieta, 1; al disolverse por la regla, 1 − smoothstep(<see cref="MarkFadeFrom"/>,
        /// <see cref="MarkFadeTo"/>, avance); con la Luciérnaga, 1 hasta que queda de canto (<see cref="FireflyTurnAt"/> por
        /// encima de <see cref="FireflyTurnOff"/>, como <c>step</c>; lo que se ve lo estrecha el giro); 0 en una dosis normal.
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
                    return FireflyTurnAt(progress) <= FireflyTurnOff ? 1f : 0f;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Opacidad de la trama diagonal (hacia la tinta) en una banda con <paramref name="look"/> y <paramref name="progress"/>:
        /// el mismo cálculo que <c>AF_UI_Liquid.shader</c> («hatch»). Quieta, <see cref="MurkyHatchOpacity"/>; al disolverse,
        /// baja a 0 entre <see cref="HatchFadeFrom"/> y <see cref="HatchFadeTo"/>; con la Luciérnaga, la que queda al fondo de la
        /// banda, lo último que se lleva el frente (<see cref="MurkyHatchAt"/> a profundidad 1); 0 en una dosis normal.
        /// </summary>
        public static float MurkyHatchAmount(MurkyLook look, float progress)
        {
            return MurkyHatchAt(look, progress, 1f);
        }

        /// <summary>
        /// Opacidad de la trama a la profundidad <paramref name="depth"/> de la banda (0 arriba, 1 abajo): con la Luciérnaga,
        /// solo bajo el frente de luz (<see cref="FireflyLightAt"/>); en los demás modos no depende de la profundidad.
        /// </summary>
        public static float MurkyHatchAt(MurkyLook look, float progress, float depth)
        {
            switch (look)
            {
                case MurkyLook.Hidden:
                    return MurkyHatchOpacity;
                case MurkyLook.Dissolve:
                    return MurkyHatchOpacity * (1f - SmoothStep(HatchFadeFrom, HatchFadeTo, progress));
                case MurkyLook.Turn:
                    return MurkyHatchOpacity * (1f - FireflyLightAt(progress, depth));
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Peso (0..1) del patrón, la franja y el menisco propios de la fruta en una banda con <paramref name="look"/> y
        /// <paramref name="progress"/> (el mismo cálculo que el shader): 1 en una dosis normal; al disolverse por la regla,
        /// sube entre <see cref="FruitPatternFrom"/> y <see cref="FruitPatternTo"/>; con la Luciérnaga, entre
        /// <see cref="FireflyPatternFrom"/> y <see cref="FireflyPatternTo"/> (con 1, la banda es la dosis normal de esa fruta);
        /// 0 oculta.
        /// </summary>
        public static float FruitPatternWeight(MurkyLook look, float progress)
        {
            switch (look)
            {
                case MurkyLook.None:
                    return 1f;
                case MurkyLook.Dissolve:
                    return SmoothStep(FruitPatternFrom, FruitPatternTo, progress);
                case MurkyLook.Turn:
                    return SmoothStep(FireflyPatternFrom, FireflyPatternTo, progress);
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Luciérnaga: profundidad del centro del frente de luz en una banda con avance <paramref name="progress"/> (0 arriba
        /// de la banda, 1 abajo; espejo del shader): baja de −<see cref="FireflyEdgeU"/>/2 a 1 + <see cref="FireflyEdgeU"/>/2
        /// mientras el avance va de 0 a <see cref="FireflyFrontSpan"/>, así que el borde entra y sale entero.
        /// </summary>
        public static float FireflyFrontDepth(float progress)
        {
            float half = 0.5f * FireflyEdgeU;
            return Mathf.Lerp(-half, 1f + half, Mathf.Clamp01(progress / FireflyFrontSpan));
        }

        /// <summary>
        /// Luciérnaga: cuánto ha aclarado el frente la profundidad <paramref name="depth"/> de la banda (1 encima del frente:
        /// el color de la dosis; 0 debajo: el pardo; espejo del shader, «lightOn»).
        /// </summary>
        public static float FireflyLightAt(float progress, float depth)
        {
            float half = 0.5f * FireflyEdgeU;
            float front = FireflyFrontDepth(progress);
            return 1f - SmoothStep(front - half, front + half, depth);
        }

        /// <summary>
        /// Luciérnaga: giro del «?» (0 de frente, 1 de canto) con el frente en <see cref="FireflyFrontDepth"/>: empieza en
        /// <see cref="FireflyTurnFrom"/> (lo alto del «?») y queda de canto en <see cref="FireflyTurnTo"/> (su centro).
        /// </summary>
        public static float FireflyTurnAt(float progress)
        {
            return Mathf.Clamp01((FireflyFrontDepth(progress) - FireflyTurnFrom) / (FireflyTurnTo - FireflyTurnFrom));
        }

        /// <summary>
        /// Alto (u de pantalla) del «?» de una banda con <paramref name="roomU"/> de hueco en su columna:
        /// <see cref="GlyphRoomRatio"/> del hueco, entre <see cref="GlyphMinU"/> y <see cref="GlyphMaxU"/> (espejo del shader).
        /// </summary>
        public static float GlyphSizeU(float roomU)
        {
            return Mathf.Clamp(GlyphRoomRatio * roomU, GlyphMinU, GlyphMaxU);
        }

        /// <summary>
        /// Visibilidad (0..1) del «?» por el hueco de su banda (espejo del shader): 0 si no cabe uno de
        /// <see cref="GlyphMinU"/> (hueco ≤ <see cref="GlyphFadeFrom"/>) y 1 desde <see cref="GlyphFadeTo"/>; entre medias se
        /// funde, así que al inclinarse la cuia el «?» se apaga sin saltos y solo queda la trama.
        /// </summary>
        public static float GlyphFade(float roomU)
        {
            return SmoothStep(GlyphFadeFrom, GlyphFadeTo, roomU);
        }

        /// <summary>
        /// Código del sitio del «?» que va sobre el alto de la cuia en <c>TEXCOORD0.w</c> (ver <see cref="GlyphPackScale"/>):
        /// centro en u de capa desde el fondo de la dosis y hueco en u de pantalla, cuantizados.
        /// </summary>
        public static int PackGlyph(float centerLayerU, float roomU)
        {
            int c = Mathf.Clamp(Mathf.RoundToInt((centerLayerU + GlyphCenterOffset) * GlyphCenterPerU), 0, (int)GlyphCenterSteps - 1);
            int r = Mathf.Clamp(Mathf.RoundToInt(roomU * GlyphRoomPerU), 0, 63);
            return c + ((int)GlyphCenterSteps * r);
        }

        /// <summary>Decodifica <see cref="PackGlyph"/> como el shader: centro (u de capa desde el fondo de la dosis) y hueco (u).</summary>
        public static void UnpackGlyph(int code, out float centerLayerU, out float roomU)
        {
            int r = code / (int)GlyphCenterSteps;
            int c = code - ((int)GlyphCenterSteps * r);
            centerLayerU = (c / GlyphCenterPerU) - GlyphCenterOffset;
            roomU = r / GlyphRoomPerU;
        }

        /// <summary>
        /// Valor de <c>TEXCOORD3.a</c> de una banda que se revela hacia la fruta <paramref name="tint"/>: su fila del atlas
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
        /// El shader dibuja el «?» de la dosis <paramref name="k"/>: turbia, sin haber terminado de disolverse (opacidad
        /// <see cref="MurkyMarkOpacity"/> por encima de 0,001) o de girar, y con hueco para él en su banda (<see cref="GlyphFade"/>
        /// del hueco cuantizado que lleva la malla). Sin asignaciones.
        /// </summary>
        public bool MurkyMarkDrawn(int k)
        {
            if ((uint)k >= (uint)_capacity || _look[k] == MurkyLook.None || _fill[k] <= 0.001f || _color[k].a <= 0.001f)
            {
                return false;
            }

            if (MurkyMarkOpacity(_look[k], _lookProgress[k]) <= 0.001f)
            {
                return false;
            }

            return MurkyGlyphPlacement(k, out _, out _, out _, out float room) && GlyphFade(room) > 0.001f;
        }

        /// <summary>
        /// Sitio del «?» de la banda turbia <paramref name="k"/> tal como lo pinta el shader (con la inclinación de ahora):
        /// centro en coordenadas de la cuia (X = x − 1 e y, u), alto en pantalla (<see cref="GlyphSizeU"/>) y hueco libre en su
        /// columna, ya cuantizado como lo lleva la malla. En reposo, centrado en la cuia a media banda entre sus arcos (07
        /// §4.3: 0,5 u); inclinada, en la columna de la cuña con más hueco que quede más cerca del centro de la cuerda y a media
        /// altura entre el vidrio (o la banda de debajo) y la de encima o la superficie, así que el punto del «?» no toca el
        /// vidrio. False si la dosis no es turbia o no tiene banda. Sin asignaciones.
        /// </summary>
        public bool MurkyGlyphPlacement(int k, out float centerX, out float centerY, out float sizeU, out float roomU)
        {
            centerX = 0f;
            centerY = 0f;
            sizeU = 0f;
            roomU = 0f;
            if ((uint)k >= (uint)_capacity || _look[k] == MurkyLook.None)
            {
                return false;
            }

            if (!GlyphPlacement(k, out centerX, out centerY, out roomU, out _))
            {
                return false;
            }

            sizeU = GlyphSizeU(roomU);
            return true;
        }

        // Como FruitIconPlacement, con el sitio empaquetado que lleva la malla (code: 0 = el shader no pinta el icono).
        private bool FruitIconPlacement(int k, out float centerX, out float centerY, out float sizeU, out float roomU, out int code)
        {
            centerX = 0f;
            centerY = 0f;
            sizeU = 0f;
            roomU = 0f;
            code = 0;
            if (!IsFruitIconDose(k) || !GlyphPlacement(k, out centerX, out centerY, out roomU, out code))
            {
                return false;
            }

            sizeU = FruitIconSizeU(roomU);
            return true;
        }

        // Sitio de la marca de la banda k («?» o icono de fruta), como lo pinta el shader con la inclinación de ahora: centro
        // en coordenadas de la cuia (cuantizado), hueco cuantizado y el código empaquetado. False sin banda. Sin asignaciones.
        private bool GlyphPlacement(int k, out float centerX, out float centerY, out float roomU, out int code)
        {
            centerX = 0f;
            centerY = 0f;
            roomU = 0f;
            code = 0;
            int top = ComputeLevels();
            if (top < k || _fill[k] <= 0.001f || _levels[k + 1] - _levels[k] < 1e-5f)
            {
                return false;
            }

            float lo = _levels[k];
            float hi = _levels[k + 1];
            float rad = _angle * Mathf.Deg2Rad;
            float sn = Mathf.Sin(rad);
            float cs = Mathf.Cos(rad);
            PlaceGlyph(k, lo, hi, _fill[k], sn, cs, out float e, out float d, out float room);
            float scale = _fill[k] / Mathf.Max(1e-5f, hi - lo);
            code = PackGlyph((d - lo) * scale, room);
            UnpackGlyph(code, out float centerLayer, out roomU);
            float dq = lo + (centerLayer / Mathf.Max(1e-5f, scale)); // el centro cuantizado, como lo decodifica el shader
            centerX = (e * cs) + (dq * sn);
            centerY = (dq * cs) - (e * sn);
            return true;
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
            int top = ComputeLevels();
            if (_unit <= 0f || _capacity <= 0 || top < 0)
            {
                return;
            }

            float heightU = CuiaGeometry.HeightU(_capacity);
            float bottomU = CuiaGeometry.CavityBottomU;
            float surf = PackSurface(RippleAmplitude(), _flash); // la onda y, en la parte entera, el destello del sellado
            bool tilted = Mathf.Abs(_angle) > 0.01f;
            float surface = bottomU + top + _fill[top];
            int topTint = _tint[top];
            float lighten = topTint > 0 ? Palette.MeniscusLighten(topTint) : 0.20f;
            if (_look[top] == MurkyLook.Dissolve || _look[top] == MurkyLook.Turn)
            {
                lighten = Mathf.Lerp(0.20f, lighten, FruitPatternWeight(_look[top], _lookProgress[top])); // el de su fruta entra con su patrón
            }

            if (_matte[top])
            {
                lighten = 0f; // mate (el Barro): la cara superior, de su color; el aclarado del menisco es un brillo más (guía 4.2)
            }

            Color men = ColorMath.Lighten(_color[top], lighten);
            Vector3 menS = ColorMath.ToShaderSpace(men);
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
                bool murky = _look[k] != MurkyLook.None;
                bool revealing = _look[k] == MurkyLook.Dissolve || _look[k] == MurkyLook.Turn;
                Vector3 litS = ColorMath.ToShaderSpace(StripeColor(k));
                float pattern = -1f; // sin patrón (Guanábana, agua base)
                float row = tint;
                if (murky)
                {
                    // Oculta, la fila turbia (nada de su fruta); al revelarse, la de la fruta que se revela (su patrón entra
                    // en el shader cuando ya se fue la trama, que el shader lee de _MurkyRow).
                    pattern = MurkyPattern(_look[k], _lookProgress[k]);
                    row = revealing ? FruitRowCode(tint) : ProceduralArt.MurkyPatternRow;
                }
                else if (_matte[k])
                {
                    pattern = MattePatternMode; // sin patrón y sin brillos (el Barro)
                }
                else if (ProceduralArt.HasPattern(tint))
                {
                    pattern = Palette.PatternBase(tint) == Palette.UiPaper ? 1f : 0f;
                }

                // El icono de fruta (T-E.4 y T-E.5) va como el «?» de una turbia: su sitio sobre el alto y su x por vértice.
                bool glyph = murky || (_fruitIcons && IsFruitIconDose(k));
                var uv1 = new Vector4(litS.x, litS.y, litS.z, 0f);
                var uv2 = new Vector4(surface, surf, _ripplePhase, pattern + (4f * k));
                var uv3 = new Vector4(menS.x, menS.y, menS.z, row);

                // La dosis superior sube TopExtraU de capa para la cara superior y la onda, y todas bajan BottomExtraU para
                // su frontera en arco (en d, con la escala de la banda).
                float hiMesh = k == top ? hi + (TopExtraU * (hi - lo) / fill) : hi;
                float loMesh = lo - (BottomExtraU * (hi - lo) / fill);
                AddBand(vh, k, lo, hi, loMesh, hiMesh, bottomU + k, fill, heightU, c, uv1, uv2, uv3, glyph);
            }

            if (tilted && _tongue > 0.01f)
            {
                AddTongue(vh, heightU, _levels[top + 1]);
            }
        }

        // Fronteras por volumen (forma normal, ver LineOffset): _levels[k] es la línea bajo la dosis k (k = 0, el fondo) y
        // _levels[k + 1] la de encima. Sin inclinar son alturas: bottom + suma de rellenos. Devuelve la dosis de arriba con
        // líquido (−1 si no hay). Sin asignaciones.
        private int ComputeLevels()
        {
            int top = -1;
            for (int k = 0; k < _capacity; k++)
            {
                if (_fill[k] > 0.001f && _color[k].a > 0.001f)
                {
                    top = k;
                }
            }

            if (top < 0)
            {
                return -1;
            }

            float bottomU = CuiaGeometry.CavityBottomU;
            float width = 2f * CuiaGeometry.CavityHalfWidthU;
            bool tilted = Mathf.Abs(_angle) > 0.01f;
            float volume = 0f;
            _levels[0] = tilted ? LevelForVolume(0f, _angle, _capacity) : bottomU;
            for (int k = 0; k <= top; k++)
            {
                volume += _fill[k] * width;
                _levels[k + 1] = tilted ? LevelForVolume(volume, _angle, _capacity) : _levels[k] + _fill[k];
            }

            return top;
        }

        // Banda entre las líneas loMesh y hiMesh (forma normal): el rectángulo de la cavidad [−w, w] × [fondo en el eje,
        // labio] recortado por los dos semiplanos (≤ 6 vértices, convexo) y dibujado en abanico. El shader recorta luego a
        // la silueta interior (con el fondo en arco) y a la frontera en arco. La y de capa es afín en d: fondo de la dosis +
        // relleno · (d − lo) / (hi − lo). En una banda con marca (glyph: el «?» de una turbia o el icono de fruta), uv1.w de
        // cada vértice es la x de la marca en pantalla (u, desde su centro: e = X · cos θ − y · sen θ menos la del centro,
        // PlaceGlyph), que es afín y se interpola exacta, y el alto de la cuia lleva encima el centro y el hueco de la marca
        // (PackGlyph). Sin asignaciones.
        private void AddBand(VertexHelper vh, int k, float lo, float hi, float loMesh, float hiMesh, float doseBottom, float fill, float heightU, Color c, Vector4 uv1, Vector4 uv2, Vector4 uv3, bool glyph)
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
            float glyphE = 0f;
            float height = heightU;
            if (glyph)
            {
                PlaceGlyph(k, lo, hi, fill, sn, cs, out glyphE, out float glyphD, out float room);
                height = heightU + (GlyphPackScale * PackGlyph((glyphD - lo) * scale, room));
            }

            Color32 vc = c;
            int i0 = vh.currentVertCount;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = _band[i];
                float d = LineOffset(p.x, p.y, _angle);
                if (glyph)
                {
                    uv1.w = ((p.x * cs) - (p.y * sn)) - glyphE;
                }

                AddVert(vh, p.x, p.y, doseBottom + ((d - lo) * scale), height, vc, uv1, uv2, uv3);
            }

            for (int i = 1; i + 1 < n; i++)
            {
                vh.AddTriangle(i0, i0 + i, i0 + i + 1);
            }
        }

        // Sitio del «?» de la banda turbia k entre las líneas lo y hi (ver MurkyGlyphPlacement): columna e y centro d en pantalla
        // (u, en la base girada de la cuia) y hueco libre en esa columna (u). En reposo, el eje y media banda entre los arcos
        // (relleno − la flecha de 0,2 u) con todo el relleno de hueco. Inclinada, se mira cada columna de pantalla: el hueco va
        // del vidrio (fondo en arco) o del arco de su frontera, en las dosis de encima, hasta el arco de la de encima o la
        // cara superior; con el ancho del «?» (las columnas vecinas). Se queda la columna más cercana al centro de la cuerda
        // (a media banda) entre las que pierden como mucho GlyphRoomTolerance del mejor hueco, interpolada en el borde, así que
        // el «?» se mueve sin saltos al inclinarse. Sin asignaciones.
        private void PlaceGlyph(int k, float lo, float hi, float fill, float sn, float cs, out float e, out float d, out float room)
        {
            if (Mathf.Abs(_angle) <= 0.01f)
            {
                e = 0f;
                d = lo + (0.5f * fill) - CuiaGeometry.FaceSagU(0f);
                room = fill;
                return;
            }

            float scale = fill / Mathf.Max(1e-5f, hi - lo);
            Vector2[] poly = Silhouette(_capacity);
            float eMin = float.MaxValue;
            float eMax = float.MinValue;
            for (int i = 0; i < poly.Length; i++)
            {
                float ev = (poly[i].x * cs) - (poly[i].y * sn);
                eMin = Mathf.Min(eMin, ev);
                eMax = Mathf.Max(eMax, ev);
            }

            int count = Mathf.Clamp(Mathf.CeilToInt((eMax - eMin) / (0.5f * GlyphHalfWidthU)), 2, GlyphColumns);
            float step = (eMax - eMin) / count;
            int reach = Mathf.Max(1, Mathf.CeilToInt(GlyphHalfWidthU / step));
            for (int j = 0; j <= count; j++)
            {
                ColumnRange(poly, k, lo, hi, scale, eMin + (j * step), sn, cs, out s_columnBottom[j], out s_columnTop[j]);
            }

            float best = float.MinValue;
            for (int j = 0; j <= count; j++)
            {
                best = Mathf.Max(best, GridRoom(j, count, reach, out _));
            }

            float wanted = best - GlyphRoomTolerance;
            float chord = ChordCenter(poly, 0.5f * (lo + hi), sn, cs);
            e = chord;
            if (RoomAt(poly, k, lo, hi, scale, chord, sn, cs, out _) < wanted)
            {
                int pick = -1;
                float pickDistance = float.MaxValue;
                for (int j = 0; j <= count; j++)
                {
                    float distance = Mathf.Abs(eMin + (j * step) - chord);
                    if (GridRoom(j, count, reach, out _) >= wanted && distance < pickDistance)
                    {
                        pick = j;
                        pickDistance = distance;
                    }
                }

                if (pick >= 0)
                {
                    float pickE = eMin + (pick * step);
                    e = pickE;
                    int toward = pickE < chord ? pick + 1 : pick - 1;
                    if (toward >= 0 && toward <= count)
                    {
                        // Hacia el centro de la cuerda hasta donde el hueco deja de bastar (interpolado: sin saltos de columna).
                        float r0 = GridRoom(pick, count, reach, out _);
                        float r1 = GridRoom(toward, count, reach, out _);
                        float f = r0 - r1 > 1e-6f ? Mathf.Clamp01((r0 - wanted) / (r0 - r1)) : 0f;
                        e = Mathf.Lerp(pickE, eMin + (toward * step), f);
                        e = pickE < chord ? Mathf.Min(e, chord) : Mathf.Max(e, chord);
                    }
                }
            }

            room = Mathf.Max(0f, RoomAt(poly, k, lo, hi, scale, e, sn, cs, out d));
        }

        // Hueco de la columna j de la rejilla con sus vecinas a ±reach (el ancho del «?»): lo más bajo de los techos menos lo
        // más alto de los suelos; center, su mitad.
        private static float GridRoom(int j, int count, int reach, out float center)
        {
            float bottom = float.MinValue;
            float top = float.MaxValue;
            for (int i = Mathf.Max(0, j - reach); i <= Mathf.Min(count, j + reach); i++)
            {
                bottom = Mathf.Max(bottom, s_columnBottom[i]);
                top = Mathf.Min(top, s_columnTop[i]);
            }

            center = 0.5f * (bottom + top);
            return top - bottom;
        }

        // Hueco (u) en la columna e con el ancho del «?» (e ± GlyphHalfWidthU) y su centro d.
        private static float RoomAt(Vector2[] poly, int k, float lo, float hi, float scale, float e, float sn, float cs, out float center)
        {
            float bottom = float.MinValue;
            float top = float.MaxValue;
            for (int s = -1; s <= 1; s++)
            {
                ColumnRange(poly, k, lo, hi, scale, e + (s * GlyphHalfWidthU), sn, cs, out float b, out float t);
                bottom = Mathf.Max(bottom, b);
                top = Mathf.Min(top, t);
            }

            center = 0.5f * (bottom + top);
            return top - bottom;
        }

        // Parte visible de la banda k en la columna de pantalla e (d, u): desde el vidrio (con el fondo en arco, que la silueta
        // tiene plano) o, en las dosis de encima, desde el arco de su frontera, hasta el arco de la de encima o la cara
        // superior. Fuera de la silueta, vacía.
        private static void ColumnRange(Vector2[] poly, int k, float lo, float hi, float scale, float e, float sn, float cs, out float bottom, out float top)
        {
            int n = poly.Length;
            float dMin = float.MaxValue;
            float dMax = float.MinValue;
            float xAtMin = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = poly[i];
                Vector2 q = poly[(i + 1) % n];
                float fp = (p.x * cs) - (p.y * sn) - e;
                float fq = (q.x * cs) - (q.y * sn) - e;
                if ((fp <= 0f && fq > 0f) || (fp > 0f && fq <= 0f))
                {
                    Vector2 r = p + ((q - p) * (fp / (fp - fq)));
                    float dr = (r.x * sn) + (r.y * cs);
                    if (dr < dMin)
                    {
                        dMin = dr;
                        xAtMin = r.x;
                    }

                    dMax = Mathf.Max(dMax, dr);
                }
            }

            if (dMin > dMax)
            {
                bottom = float.MaxValue;
                top = float.MinValue;
                return;
            }

            float glass = dMin - (CuiaGeometry.FaceSagU(xAtMin) * Mathf.Max(0f, cs));
            float invScale = 1f / Mathf.Max(1e-5f, scale);
            bottom = k == 0 ? glass : Mathf.Max(lo - (CuiaGeometry.FaceSagU((e * cs) + (lo * sn)) * invScale), glass);
            top = Mathf.Min(hi - (CuiaGeometry.FaceSagU((e * cs) + (hi * sn)) * invScale), dMax);
        }

        /// <summary>
        /// Centro, en la coordenada horizontal de pantalla <c>e = X · cos θ − y · sen θ</c> (u; sin inclinar, <c>e = X</c>), de
        /// la cuerda que la línea horizontal <c>d</c> (ver <see cref="LineOffset"/>) corta en la silueta interior de una cuia de
        /// <paramref name="capacity"/> dosis girada <paramref name="angleDeg"/> grados. 0 si la línea no corta la silueta. Sin
        /// asignaciones tras la primera llamada por capacidad.
        /// </summary>
        public static float ChordCenter(float d, float angleDeg, int capacity)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return ChordCenter(Silhouette(capacity), d, Mathf.Sin(rad), Mathf.Cos(rad));
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

        // xRel = x − 1 (u); yU = y local (u); layer = y de capa (u); heightU = alto de la cuia (y, en una banda turbia, el
        // sitio de su «?» empaquetado encima).
        private void AddVert(VertexHelper vh, float xRel, float yU, float layer, float heightU, Color32 color, Vector4 uv1, Vector4 uv2, Vector4 uv3)
        {
            var pos = new Vector3(xRel * _unit, yU * _unit, 0f);
            vh.AddVert(pos, color, new Vector4(xRel + 1f, yU, layer, heightU), uv1, uv2, uv3, Vector3.back, new Vector4(1f, 0f, 0f, -1f));
        }

        // Guarda la dosis k (ya normalizados el avance y el tinte) y reconstruye la malla solo si algo cambia. Sin asignaciones.
        private void Store(int k, Color color, float fill, int tint, MurkyLook look, float progress, bool matte)
        {
            if ((uint)k >= (uint)_capacity)
            {
                return;
            }

            fill = Mathf.Clamp01(fill);
            if (_color[k] == color && Mathf.Approximately(_fill[k], fill) && _tint[k] == tint && _look[k] == look && Mathf.Approximately(_lookProgress[k], progress) && _matte[k] == matte)
            {
                return;
            }

            _color[k] = color;
            _fill[k] = fill;
            _tint[k] = tint;
            _look[k] = look;
            _lookProgress[k] = progress;
            _matte[k] = matte;
            SetVerticesDirty();
        }

        // Franja principal de la dosis k como color opaco (guía 4.2; TEXCOORD1.rgb): la de su tinte (Palette.LiquidHighlight)
        // o, sin tinte, +10 L*; al revelarse, la de su fruta entra con su patrón. Mate (el Barro), su propio color: el shader
        // no pinta brillos en ella. Sin asignaciones.
        private Color StripeColor(int k)
        {
            Color c = _color[k];
            if (_matte[k])
            {
                return c;
            }

            int tint = _tint[k];
            Color lit = tint > 0 ? Palette.LiquidHighlight(tint, c) : ColorMath.AddLabLightness(c, 10f);
            MurkyLook look = _look[k];
            if ((look == MurkyLook.Dissolve || look == MurkyLook.Turn) && tint > 0)
            {
                // La franja de la fruta que se revela entra con su patrón: al final del revelado es la de su dosis normal.
                lit = Color.Lerp(ColorMath.AddLabLightness(c, 10f), lit, FruitPatternWeight(look, _lookProgress[k]));
            }

            return lit;
        }

        // Un color del Palette (sRGB) en el espacio del shader, como vector: SetVector no lo convierte.
        private static void SetShaderColor(Material mat, int id, Color c)
        {
            Vector3 s = ColorMath.ToShaderSpace(c);
            mat.SetVector(id, new Vector4(s.x, s.y, s.z, 1f));
        }

        // Mezcla de dos colores sRGB como la del shader (en lineal en el proyecto Linear), de vuelta a sRGB, con el alfa de to.
        private static Color MixAsShader(Color from, Color to, float t)
        {
            if (QualitySettings.activeColorSpace != ColorSpace.Linear)
            {
                return new Color(Mathf.Lerp(from.r, to.r, t), Mathf.Lerp(from.g, to.g, t), Mathf.Lerp(from.b, to.b, t), to.a);
            }

            return new Color(MixLinear(from.r, to.r, t), MixLinear(from.g, to.g, t), MixLinear(from.b, to.b, t), to.a);
        }

        // Mezcla lineal de dos componentes sRGB (como la GPU en el proyecto Linear), de vuelta a sRGB.
        private static float MixLinear(float a, float b, float t)
        {
            return ColorMath.ToSrgb(Mathf.Lerp(ColorMath.ToLinear(a), ColorMath.ToLinear(b), t));
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
