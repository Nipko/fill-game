#nullable enable
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Tinaja (M13; D-019; 07 §4.3 y §4.5; propuesta de la Tinaja, b.2 «Aspecto» y c.6; encargo de
    /// arte, 3.1; tarea TJ2) hasta el arte del artista: el cuerpo de barro cocido claro (<see cref="TinajaBody"/>; vasija lisa,
    /// más alta que ancha, con hombro, el labio por cuello corto y una sola línea incisa, con gotitas quietas de «sudor»), la
    /// boca (<see cref="TinajaMouthDisc"/> y su sombra, <see cref="TinajaMouthGloss"/>), las bandas de las capas reveladas
    /// (<see cref="TinajaBand"/> con el tinte, su filo de luz <see cref="TinajaBandLight"/> y, si hace falta, su contorno
    /// <see cref="TinajaBandRim"/>), el icono de la fruta (<see cref="TinajaGlyph"/>), el chip «×n»
    /// (<see cref="TinajaChipPill"/>), el jirau (<see cref="TinajaJirau"/>, dos varas atadas solo en las puntas), y el poste bajo
    /// con su gancho (<see cref="TinajaPost"/>) del que cuelga su cuia por el asa de fibra (<see cref="TinajaAsa"/>). La vasija,
    /// el jirau, su cuia colgada y el asa son contenido cultural: pendiente del asesor cultural (D-019; 07 §4.3; encargo de arte
    /// 3.4).
    /// </summary>
    /// <remarks>
    /// <para><b>Compuerta</b> (<see cref="MapArtRule"/>, como el arte provisional del Mapa y de TiTino): las piezas que son arte
    /// (cuerpo, jirau, poste y asa) solo salen con <see cref="MapArtRule.ShowProvisional"/> (Editor y desarrollo). Sus ranuras de
    /// PNG aún no tienen nombre en el glosario §11 (encargo de arte, sección 5, pendiente 1: «ningún PNG lleva un nombre
    /// inventado»), así que en release no se pinta ninguna hasta que lleguen. La boca (con su brillo y su icono) y las bandas
    /// están hechas para la silueta de la vasija y salen solo con el cuerpo provisional. En release, sin el cuerpo, del overlay
    /// solo sale el chip «×n», en el hombro del vidrio (<c>TinajaOverlay.GlassChipCenterU</c>); la cima y las capas reveladas
    /// se ven en el líquido del vidrio (<c>TinajaOverlay</c>, <c>CuiaView</c>).</para>
    /// <para><b>Color</b> (direccion-visual-v2 §2.4, regla 1; 07 §3, regla 6): el barro (<see cref="Palette.TinajaClay"/>), la
    /// madera del jirau (<see cref="JirauWood"/>, <see cref="JirauShade"/>) y la del chip (<see cref="TinajaChipWood"/>) quedan a
    /// ΔE00 ≥ 10 de los 14 tintes; nunca el pardo #8A7B6A ni el «?» de Aguas turbias, ni el terracota #B5653A (07 §4.3). Las
    /// ataduras y el asa, en tonos <c>fibra_*</c>. La boca y las bandas llevan el HEX del tinte a propósito, sin aclarar (es lo
    /// que se ve y lo que se empareja con su fruta).</para>
    /// <para><b>Vetos</b> (07 §4.3 y §8.1): sin decoración (kené, marajoara, tapajônica), sin cara, ni dos bocas, ni hoja en la
    /// boca; nunca redonda y baja, morada, con fuego, vapor ni burbujas; nada de cordón, liana ni nudo entre la tinaja y su cuia
    /// (propuesta, b.2 «Aspecto»; encargo de arte, sección 3): el poste del jirau va del lado contrario a la tinaja y el jirau son
    /// varas continuas bajo las dos ranuras, atadas solo en las puntas (nada en el punto medio entre las dos piezas).</para>
    /// <para>Todo perezoso: se genera la primera vez que se pide (al mostrar la tinaja o su cuia al cargar el nivel), nunca en un
    /// paso; tras <see cref="Dispose"/> se vuelve a generar.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho del sprite del cuerpo de la tinaja (u): la panza (2 × 1,28 u, dentro de la ranura de ≥ 2,86 u) con margen.</summary>
        public const float TinajaBodyWidthU = 2.70f;

        /// <summary>
        /// Alto del sprite del cuerpo de la tinaja (u): del pie (y = 0) a lo alto del labio (<see cref="TinajaTopU"/>, 6,60 u) con
        /// un margen de 0,04 u (≈ 4 px a <c>MechanicPxPerU</c>) para el canto suavizado.
        /// </summary>
        public const float TinajaBodyHeightU = 6.64f;

        /// <summary>
        /// Lo alto de la vasija (u desde el pie): 6,60 u, el alto de la Cuia alta de 5 (02 §5: «la tinaja ocupa una ranura con el
        /// alto de la Cuia alta de 5 (6,60 u)»; 07 §4.5; encargo de arte 3.1), el mismo con que el layout mide su fila.
        /// </summary>
        public const float TinajaTopU = TinajaLipCenterU + TinajaLipRyU;

        /// <summary>Centro del labio (la elipse de arriba de la vasija, vista un poco desde arriba; u desde el pie) [P].</summary>
        public const float TinajaLipCenterU = 6.15f;

        /// <summary>Medio ancho del labio (u) [P]: la boca entra dentro con su 0,70 del ancho de la cuia.</summary>
        public const float TinajaLipRxU = 0.86f;

        /// <summary>Medio alto del labio (u) [P]: lo alto de la vasija queda en 6,60 u (<see cref="TinajaTopU"/>).</summary>
        public const float TinajaLipRyU = 0.45f;

        /// <summary>Medio ancho del hueco oscuro de la boca, dentro del labio (u) [P].</summary>
        public const float TinajaOpeningRxU = 0.76f;

        /// <summary>Medio alto del hueco oscuro de la boca (u) [P].</summary>
        public const float TinajaOpeningRyU = 0.39f;

        /// <summary>
        /// Boca abajo (TJ3): grosor del arco del canto delantero del labio (u): 0,125 u, ≈ 3 dp con la cuia de 48 dp (u ≈ 24 dp)
        /// [P] (revisión de TJ3).
        /// </summary>
        public const float TinajaUpendLipArcU = 0.125f;

        /// <summary>Boca abajo: el arco del canto del labio, un barro más claro que el cuerpo, #C9A07E [P] (revisión de TJ3).</summary>
        public static readonly Color TinajaUpendLipArc = Palette.Hex(0xC9A07E);

        /// <summary>Boca abajo: alto de la tapa de la base sobre su ancho, 0,28, como la boca vista desde arriba [P] (revisión de TJ3).</summary>
        public const float TinajaBaseCapAspect = 0.28f;

        /// <summary>Boca abajo: borde de la tapa de la base (u): ≈ 1 dp con la cuia de 48 dp [P].</summary>
        public const float TinajaBaseCapRimU = 0.042f;

        /// <summary>
        /// Ancho del agua de la boca (u): 1,42 u, 0,71 del ancho de la cuia (07 §4.3: «boca de 0,70 del ancho o más»; ≈ 34 dp con
        /// la cuia de 48 dp).
        /// </summary>
        public const float TinajaMouthWidthU = 1.42f;

        /// <summary>Alto del agua de la boca (u) [P]: el icono de la fruta cabe entero dentro.</summary>
        public const float TinajaMouthHeightU = 0.72f;

        /// <summary>Centro del agua de la boca (u desde el pie): un poco más abajo que el labio, que asoma por detrás.</summary>
        public const float TinajaMouthCenterU = 6.13f;

        /// <summary>
        /// Lado de la celda del icono de la fruta en la boca (u) [P]: la silueta (0,9 de la celda) mide ≈ 14,3 dp con la cuia de
        /// 48 dp (07 §4.3: icono de 14-16 dp).
        /// </summary>
        public const float TinajaMouthGlyphU = 0.66f;

        /// <summary>
        /// Centro del brillo del agua de la boca, desde el centro de la boca (u) [P]: en el cuarto de arriba a la izquierda de la
        /// elipse, por encima de la sombra del icono de la fruta.
        /// </summary>
        public static readonly Vector2 TinajaMouthGlossOffsetU = new Vector2(-0.35f, 0.18f);

        /// <summary>Opacidad del brillo de la boca (≤ 0,25) [P].</summary>
        public const float TinajaMouthGlossAlpha = 0.25f;

        /// <summary>Alto de una banda de una capa revelada (u) [P]: algo menos que la dosis, centrada a su altura.</summary>
        public const float TinajaBandHeightU = 0.62f;

        /// <summary>
        /// Lienzo de las piezas de una banda (px): 192 × 60, que el overlay estira a la banda (≈ 2,2 × 0,62 u); con la cuia de
        /// 48 dp, ≈ 4 px por dp en alto.
        /// </summary>
        private const int BandPxWidth = 192;

        private const int BandPxHeight = 60;

        // Filo de luz (≈ 1,5 dp con la cuia de 48 dp) y contorno (≈ 1 dp) de una banda, en px de su lienzo.
        private const float BandLightPx = 6f;
        private const float BandRimPx = 4f;

        /// <summary>Lo que la banda se queda dentro del contorno de la panza por cada lado (u) [P].</summary>
        public const float TinajaBandInsetU = 0.14f;

        /// <summary>Lado de la celda del icono de la fruta en una banda (u) [P].</summary>
        public const float TinajaBandGlyphU = 0.56f;

        /// <summary>Altura de la única línea incisa (u desde el pie) [P]: sobre la banda más alta posible (la capa 3, hasta 4,41 u).</summary>
        public const float TinajaIncisedLineU = 4.78f;

        /// <summary>Alto del chip «×n» (dp) [P]: los glifos de la insignia a escala <see cref="TinajaChipGlyphScale"/>.</summary>
        public const float TinajaChipHeightDp = 18f;

        /// <summary>
        /// Escala de los glifos marfil del chip sobre los de la insignia de Aguas turbias (<see cref="MurkyBadgeCellDp"/> ×
        /// <see cref="MurkyBadgeDp"/>) [P]: la cifra mide ≈ 10,2 dp de alto, la de un texto de 14 sp (07 §4.3).
        /// </summary>
        public const float TinajaChipGlyphScale = 1.1f;

        /// <summary>Ancho del chip «×n» (dp) [P]: dos celdas de glifo y su margen.</summary>
        public const float TinajaChipWidthDp = (2f * MurkyBadgeCellDp * TinajaChipGlyphScale) + 6f;

        /// <summary>
        /// Alto de diseño del jirau (dp, a <see cref="UiPxPerDp"/>): dos varas. El overlay lo escala a su alto
        /// (<c>TinajaOverlay.JirauHeightDp</c>) con el multiplicador del 9-slice.
        /// </summary>
        public const float TinajaJirauDesignDp = 6f;

        /// <summary>Borde de 9 cortes del jirau a cada lado (dp de diseño): las ataduras de las puntas.</summary>
        public const float TinajaJirauBorderDp = 5f;

        /// <summary>Poste del jirau: distancia de su eje al eje de la cuia que cuelga (u) [P], dentro de la ranura (≥ 1,43 u).</summary>
        public const float TinajaPostXU = 1.19f;

        /// <summary>
        /// Punta del poste (u desde el pie) [P]: un poste trasero bajo, a la altura de la cima del asa, sin travesaño (nada pasa
        /// por encima del cuello de su cuia: la silueta poste + travesaño + cuerda se leía como una horca, 07 §8).
        /// </summary>
        public const float TinajaPostTopU = 4.00f;

        /// <summary>Medio ancho del asa de fibra (u) [P]: sale de los dos hombros de la Cuia chica de 2, donde el vidrio mide ±0,80 u.</summary>
        public const float TinajaAsaHalfU = 0.80f;

        /// <summary>Cima del asa de fibra (u desde el pie) [P]: 0,32 u sobre lo alto de la Cuia chica de 2 (3,60 u), como mucho 3,95 u.</summary>
        public const float TinajaAsaTopU = 3.92f;

        /// <summary>
        /// Gancho del poste (u, desde el eje y el pie de su cuia; a la derecha, se refleja a su lado) [P]: sale corto hacia delante
        /// de la punta del poste, baja hacia su cuia y se curva hacia arriba bajo el flanco del asa, que se apoya en él. Queda fuera
        /// del cuello y del borde de fibra de su cuia (≥ 0,55 u del eje) y más bajo que la cima del asa.
        /// </summary>
        public static readonly Vector2 TinajaHookU = new Vector2(0.74f, 3.38f);

        /// <summary>Caja del sprite del poste en u, desde el eje de la cuia (a la derecha; a la izquierda se refleja).</summary>
        public static readonly Rect TinajaPostRectU = new Rect(-0.20f, -0.12f, 1.55f, 4.24f);

        /// <summary>
        /// Arranque del asa en los hombros de la Cuia chica de 2 (u desde el pie) [P]: la altura del hombro donde el vidrio mide
        /// ±<see cref="TinajaAsaHalfU"/> (<c>CuiaGeometry.OuterHalfWidth</c>), sobre el líquido (2,60 u) y bajo el cuello.
        /// </summary>
        public const float TinajaAsaFootU = 2.77f;

        /// <summary>Exponente de la curva del asa [P]: con 3, el arco sube recto desde los hombros y pasa holgado sobre el borde de fibra.</summary>
        private const float TinajaAsaExponent = 3f;

        /// <summary>Caja del sprite del asa de fibra en u, desde el eje y el pie de una Cuia chica de 2 (alto 3,60 u).</summary>
        public static readonly Rect TinajaAsaRectU = new Rect(-0.92f, 2.62f, 1.84f, 1.38f);

        /// <summary>
        /// Altura del eje del asa (u desde el pie) a <paramref name="xU"/> u del eje de su cuia: un arco
        /// |x / <see cref="TinajaAsaHalfU"/>|³ + |(y − <see cref="TinajaAsaFootU"/>) / (cima − arranque)|³ = 1 desde los hombros
        /// hasta <see cref="TinajaAsaTopU"/>; −1 fuera del asa.
        /// </summary>
        public static float TinajaAsaYAt(float xU)
        {
            float e = Mathf.Abs(xU) / TinajaAsaHalfU;
            if (e > 1f)
            {
                return -1f;
            }

            float k = Mathf.Pow(1f - Mathf.Pow(e, TinajaAsaExponent), 1f / TinajaAsaExponent);
            return TinajaAsaFootU + ((TinajaAsaTopU - TinajaAsaFootU) * k);
        }

        /// <summary>
        /// Madera del chip «×n», <see cref="Palette.Frame"/> #7C4E3A (07 §4.3: «marfil sobre madera»), a ΔE00 ≥ 10 de los 14
        /// tintes. Contraste de la cifra: 5,9:1 (marfil <see cref="Palette.UiPaper"/> #F4EBDA sobre #7C4E3A), medido en la
        /// captura tj2_01 del 2026-10-18: la cifra sale #F4EBDA sobre #7C4E3A en toda su altura, porque el brillo (#8E6756, que
        /// con la cifra daría 3,9:1) es una franja de <see cref="TinajaChipGlossDp"/> dp por encima de los glifos, con 1 dp de
        /// madera entre los dos.
        /// </summary>
        public static readonly Color TinajaChipWood = Palette.Frame;

        /// <summary>Alto de la franja de brillo del chip (dp) [P], por encima de los glifos.</summary>
        public const float TinajaChipGlossDp = 2f;

        /// <summary>Distancia de la franja de brillo al borde de arriba del chip (dp): el canto oscuro de 1 dp.</summary>
        public const float TinajaChipGlossTopDp = 1f;

        /// <summary>Varas del jirau, madera clara #B09468 [P] (ΔE00 ≥ 16 frente a los 14 tintes).</summary>
        public static readonly Color JirauWood = Palette.Hex(0xB09468);

        /// <summary>Sombra de las varas y madera del poste en la sombra, #7A5A3C [P] (ΔE00 ≥ 12 frente a los 14 tintes).</summary>
        public static readonly Color JirauShade = Palette.Hex(0x7A5A3C);

        // Contorno de 1-2 dp del barro y fondo del hueco de la boca [P] (líneas finas y lo que queda bajo el agua de la boca).
        private static readonly Color TinajaClayLine = Palette.Hex(0x8E5E43);
        private static readonly Color TinajaOpeningDark = Palette.Hex(0x3E2A1E);
        private static readonly Color TinajaOpeningFront = Palette.Hex(0x6B4A35);

        // Perfil de la panza: alturas (u desde el pie) y medio ancho (u) en cada una; entre dos puntos, smoothstep (monótono:
        // sin rebotes). Con el labio, cubre el vidrio de la Cuia alta de 5 (CuiaGeometry.OuterHalfWidth) hasta lo alto de su
        // cuello: la tinaja es opaca. Su borde de fibra (de 6,35 a 6,60 u) no hace falta cubrirlo: con el cuerpo a la vista, el
        // vidrio de la cuia entero se oculta (CuiaView).
        private static readonly float[] TinajaProfileY = { 0.00f, 0.15f, 0.60f, 1.60f, 2.60f, 3.60f, 4.50f, 5.20f, 5.50f, 5.65f, 5.85f, 6.40f };
        private static readonly float[] TinajaProfileHalf = { 0.86f, 0.95f, 1.10f, 1.24f, 1.28f, 1.24f, 1.14f, 1.06f, 1.035f, 1.02f, 0.88f, 0.86f };

        // Gotitas de «sudor» quietas (u desde el eje y el pie) y su radio (u) [P].
        private static readonly Vector3[] TinajaSweat =
        {
            new Vector3(-0.58f, 1.52f, 0.055f),
            new Vector3(0.46f, 2.44f, 0.045f),
            new Vector3(-0.22f, 3.52f, 0.040f),
            new Vector3(0.74f, 3.86f, 0.050f),
            new Vector3(-0.80f, 2.86f, 0.042f),
            new Vector3(0.22f, 0.78f, 0.048f),
        };

        private Sprite? _tinajaBody;
        private Sprite? _tinajaMouthDisc;
        private Sprite? _tinajaMouthGloss;
        private Sprite? _tinajaBand;
        private Sprite? _tinajaBandLight;
        private Sprite? _tinajaBandRim;
        private Sprite? _tinajaChipPill;
        private Sprite? _tinajaJirau;
        private Sprite? _tinajaPost;
        private Sprite? _tinajaAsa;
        private Sprite? _tinajaDryMouthCenter;
        private Sprite? _tinajaBodyClosed;
        private Sprite? _tinajaBaseCap;
        private Sprite? _tinajaDripDot;
        private readonly Sprite?[] _tinajaGlyphs = new Sprite?[Cuia.MaxTintColor + 1];

        /// <summary>Cuerpo de la tinaja (<see cref="TinajaBodyWidthU"/> × <see cref="TinajaBodyHeightU"/> u, pivote al centro).</summary>
        internal Sprite TinajaBody => _tinajaBody != null ? _tinajaBody : (_tinajaBody = BuildTinajaBody(false));

        /// <summary>
        /// Cuerpo de la tinaja boca abajo (TJ3, revisión de la suelta; mismas medidas que <see cref="TinajaBody"/>): sin el hueco
        /// oscuro de la boca. El labio es barro, como la panza, con el arco de su canto delantero (la mitad de la elipse que, ya
        /// volteada, queda abajo) en un barro más claro (<see cref="TinajaUpendLipArc"/>, ≈ 3 dp): una vasija maciza vista
        /// desde arriba, nunca un tubo abierto. El overlay lo pone al voltearse (desde la mitad del giro) y boca abajo.
        /// </summary>
        internal Sprite TinajaBodyClosed => _tinajaBodyClosed != null ? _tinajaBodyClosed : (_tinajaBodyClosed = BuildTinajaBody(true));

        /// <summary>
        /// Tapa de la base de la tinaja boca abajo (TJ3): elipse blanca con un borde de ≈ 1 dp al 85 % (el overlay la tiñe de
        /// <c>TinajaOverlay.BaseCapColor</c>, así el borde queda un 15 % más oscuro).
        /// </summary>
        internal Sprite TinajaBaseCap => _tinajaBaseCap != null ? _tinajaBaseCap : (_tinajaBaseCap = BuildTinajaBaseCap());

        /// <summary>Gota de la vuelta de su cuia al sacar (TJ3): círculo blanco opaco de canto suave (el overlay lo tiñe).</summary>
        internal Sprite TinajaDripDot => _tinajaDripDot != null ? _tinajaDripDot : (_tinajaDripDot = BuildTinajaDripDot());

        /// <summary>Elipse blanca del agua de la boca: la tiñe el overlay con la fruta de arriba.</summary>
        internal Sprite TinajaMouthDisc => _tinajaMouthDisc != null ? _tinajaMouthDisc : (_tinajaMouthDisc = BuildTinajaMouthDisc());

        /// <summary>Sombra del labio sobre el agua de la boca (arriba) y un brillo (arriba a la izquierda), encima del agua.</summary>
        internal Sprite TinajaMouthGloss => _tinajaMouthGloss != null ? _tinajaMouthGloss : (_tinajaMouthGloss = BuildTinajaMouthGloss());

        /// <summary>
        /// Centro más claro del fondo seco de la boca (TJ3): elipse blanca del tamaño de la boca con el alfa que cae del centro al
        /// borde. El overlay la tiñe de <c>TinajaOverlay.DryMouthCenter</c> sobre el disco de <c>DryMouthRim</c>: un fondo de barro
        /// seco, mate y sin brillo, que no se confunde con el agua ni con la boca oscura del revelado.
        /// </summary>
        internal Sprite TinajaDryMouthCenter => _tinajaDryMouthCenter != null ? _tinajaDryMouthCenter : (_tinajaDryMouthCenter = BuildTinajaDryMouthCenter());

        /// <summary>Relleno de una banda, blanco opaco de canto nítido: lo tiñe el overlay con el HEX del tinte de la capa revelada.</summary>
        internal Sprite TinajaBand => _tinajaBand != null ? _tinajaBand : (_tinajaBand = BuildTinajaBand());

        /// <summary>Filo de luz de una banda: sus franjas de arriba y abajo (el overlay las pone en marfil al 35 %).</summary>
        internal Sprite TinajaBandLight => _tinajaBandLight != null ? _tinajaBandLight : (_tinajaBandLight = BuildTinajaBandLight());

        /// <summary>Contorno de ≈ 1 dp de una banda (el overlay lo tiñe con el tinte oscurecido, solo si el tinte queda cerca del barro).</summary>
        internal Sprite TinajaBandRim => _tinajaBandRim != null ? _tinajaBandRim : (_tinajaBandRim = BuildTinajaBandRim());

        /// <summary>Píldora de madera del chip «×n» (9 cortes en horizontal, <see cref="TinajaChipHeightDp"/> de alto).</summary>
        internal Sprite TinajaChipPill => _tinajaChipPill != null ? _tinajaChipPill : (_tinajaChipPill = BuildTinajaChipPill());

        /// <summary>Jirau: dos varas atadas en las puntas (9 cortes en horizontal; se estira bajo las dos ranuras).</summary>
        internal Sprite TinajaJirau => _tinajaJirau != null ? _tinajaJirau : (_tinajaJirau = BuildTinajaJirau());

        /// <summary>
        /// Poste bajo del jirau con su gancho corto, a la derecha del eje de la cuia (<see cref="TinajaPostRectU"/>; pivote en el
        /// eje y el pie). Sin travesaño ni cuerda: nada pasa sobre el cuello de su cuia.
        /// </summary>
        internal Sprite TinajaPost => _tinajaPost != null ? _tinajaPost : (_tinajaPost = BuildTinajaPost());

        /// <summary>Asa de fibra de su cuia (<see cref="TinajaAsaRectU"/>, pivote al centro): un arco de hombro a hombro, sobre el cuello.</summary>
        internal Sprite TinajaAsa => _tinajaAsa != null ? _tinajaAsa : (_tinajaAsa = BuildTinajaAsa());

        /// <summary>
        /// Medio ancho del contorno de la tinaja a la altura <paramref name="yU"/> (u desde el pie), sin el labio; 0 fuera. Cubre
        /// el vidrio de la Cuia alta de 5 a cualquier altura (<c>TinajaVisualTests</c>).
        /// </summary>
        public static float TinajaHalfWidthU(float yU)
        {
            float[] ys = TinajaProfileY;
            float[] hs = TinajaProfileHalf;
            if (yU < 0f || yU > ys[ys.Length - 1])
            {
                return 0f;
            }

            for (int i = 1; i < ys.Length; i++)
            {
                if (yU <= ys[i])
                {
                    float t = (yU - ys[i - 1]) / (ys[i] - ys[i - 1]);
                    return Mathf.Lerp(hs[i - 1], hs[i], Mathf.SmoothStep(0f, 1f, t));
                }
            }

            return hs[hs.Length - 1];
        }

        /// <summary>
        /// Medio ancho de la silueta entera a la altura <paramref name="yU"/> (u desde el pie): la panza y, encima, la elipse
        /// del labio (lo alto de la vasija, a <see cref="TinajaLipCenterU"/> + <see cref="TinajaLipRyU"/>).
        /// </summary>
        public static float TinajaSilhouetteHalfWidthU(float yU)
        {
            float body = yU <= TinajaLipCenterU ? TinajaHalfWidthU(yU) : 0f;
            float ey = (yU - TinajaLipCenterU) / TinajaLipRyU;
            float lip = ey * ey < 1f ? TinajaLipRxU * Mathf.Sqrt(1f - (ey * ey)) : 0f;
            return Mathf.Max(body, lip);
        }

        /// <summary>
        /// Icono de la fruta del tinte <paramref name="tint"/> (1-14) para la boca y las bandas de la tinaja: la silueta de los
        /// Patrones de fruta (<see cref="FruitIconMask"/>, celda de <see cref="FruitIconPx"/> px) en el color con que se pinta
        /// sobre su dosis (<see cref="Palette.PatternBase"/> al <see cref="LiquidGraphic.FruitIconOpacity"/>) y su contorno si lo
        /// lleva, sobre transparente. Perezoso y en caché por tinte: el overlay lo pide al cargar el nivel para las frutas de la
        /// tinaja, nunca en un paso.
        /// </summary>
        public Sprite TinajaGlyph(int tint)
        {
            int t = Mathf.Clamp(tint, Cuia.MinColor, Cuia.MaxTintColor);
            Sprite? cached = _tinajaGlyphs[t];
            if (cached != null)
            {
                return cached;
            }

            Sprite sprite = BuildTinajaGlyph(t);
            _tinajaGlyphs[t] = sprite;
            return sprite;
        }

        /// <summary>Ya está generado el icono de la tinaja del tinte <paramref name="tint"/> (tests: la carga lo prepara).</summary>
        public bool HasTinajaGlyph(int tint)
        {
            return tint >= Cuia.MinColor && tint <= Cuia.MaxTintColor && _tinajaGlyphs[tint] != null;
        }

        // Cuerpo: la panza (perfil de TinajaHalfWidthU) con sombreado cilíndrico (luz arriba a la izquierda, D-003), su contorno,
        // la línea incisa y el sudor; encima, el labio (elipse clara) con el hueco oscuro de la boca. El agua de la boca la
        // pinta el overlay con su tinte. Cerrado (closed, para boca abajo): el labio es barro sombreado como la panza, sin hueco,
        // y su mitad de arriba (abajo al voltearlo: el canto delantero) lleva un arco de barro claro de TinajaUpendLipArcU.
        private Sprite BuildTinajaBody(bool closed)
        {
            const float px = MechanicPxPerU;
            int w = Mathf.RoundToInt(TinajaBodyWidthU * px);
            int h = Mathf.RoundToInt(TinajaBodyHeightU * px);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            const float line = 1.6f; // contorno, en px (≈ 0,4 dp)
            for (int y = 0; y < h; y++)
            {
                float yu = (y + 0.5f) / px;
                bool lipRow = closed && yu > TinajaLipCenterU;
                float half = yu <= TinajaLipCenterU ? TinajaHalfWidthU(yu) : (lipRow ? TinajaSilhouetteHalfWidthU(yu) : 0f);
                if (half <= 0f)
                {
                    continue;
                }

                float shadeHalf = lipRow ? TinajaLipRxU : half; // el labio cerrado sigue el sombreado del cuello
                for (int x = 0; x < w; x++)
                {
                    float xu = ((x + 0.5f) - cx) / px;
                    float dist = (Mathf.Abs(xu) - half) * px; // negativa dentro
                    if (lipRow)
                    {
                        // Sobre el centro del labio, el canto es la elipse: distancia aproximada (en px) por su semieje menor.
                        float ex = xu / TinajaLipRxU;
                        float ey = (yu - TinajaLipCenterU) / TinajaLipRyU;
                        dist = (Mathf.Sqrt((ex * ex) + (ey * ey)) - 1f) * TinajaLipRyU * px;
                    }

                    float cover = PixelCanvas.Coverage(dist);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    float nx = Mathf.Clamp(xu / shadeHalf, -1f, 1f);
                    Color col = Color.Lerp(Palette.TinajaClayLight, Palette.TinajaClayShade, Mathf.SmoothStep(0f, 1f, (nx + 0.55f) / 1.5f));
                    col = Color.Lerp(col, Palette.TinajaClay, 0.35f);
                    float edge = nx * nx * nx * nx;
                    col = ColorMath.Darken(col, (0.16f * edge) + (yu < 0.45f ? 0.10f * (1f - (yu / 0.45f)) : 0f));
                    float gloss = Mathf.Exp(-((nx + 0.52f) * (nx + 0.52f)) / 0.012f);
                    col = Color.Lerp(col, Color.white, 0.16f * gloss * Mathf.Clamp01((yu - 0.6f) / 1.2f));

                    // La única línea incisa: un surco oscuro con su filo claro debajo.
                    float dy = (yu - TinajaIncisedLineU) * px;
                    if (Mathf.Abs(dy) < 1.2f)
                    {
                        col = Color.Lerp(col, TinajaClayLine, 0.55f * (1f - (Mathf.Abs(dy) / 1.2f)));
                    }
                    else if (dy < -1.2f && dy > -2.8f)
                    {
                        col = ColorMath.Lighten(col, 0.10f);
                    }

                    if (lipRow && -dist < TinajaUpendLipArcU * px)
                    {
                        col = TinajaUpendLipArc; // el canto delantero del labio, ya volteado (TJ3)
                    }

                    col = Color.Lerp(col, TinajaClayLine, Mathf.Clamp01(1f - ((-dist) / line)));
                    col.a = 1f;
                    pc.Blend(x, y, col, cover);
                }
            }

            // Sudor: gotitas quietas que pillan la luz (sombra abajo, cuerpo claro y un punto de brillo).
            for (int i = 0; i < TinajaSweat.Length; i++)
            {
                Vector3 s = TinajaSweat[i];
                float sx = cx + (s.x * px);
                float sy = s.y * px;
                float r = s.z * px;
                pc.FillCircle(sx + (0.25f * r), sy - (0.45f * r), r, ColorMath.WithAlpha(TinajaClayLine, 0.45f));
                pc.FillCircle(sx, sy, r, ColorMath.WithAlpha(Color.Lerp(Palette.TinajaClayLight, Color.white, 0.55f), 0.85f));
                pc.FillCircle(sx - (0.3f * r), sy + (0.3f * r), 0.38f * r, ColorMath.WithAlpha(Color.white, 0.95f));
            }

            if (closed)
            {
                return MakeSprite(pc, "mech_tinaja_body_closed", new Vector2(0.5f, 0.5f), Vector4.zero);
            }

            // Labio: canto oscuro, cara de arriba clara y, por dentro, el hueco oscuro (más claro hacia delante).
            float lx = cx;
            float ly = TinajaLipCenterU * px;
            pc.FillEllipseSoft(lx, ly - 1f, (TinajaLipRxU * px) + 0.6f, (TinajaLipRyU * px) + 0.6f, 1f, TinajaClayLine);
            pc.FillEllipseSoft(lx, ly, TinajaLipRxU * px, TinajaLipRyU * px, 1f, Color.Lerp(Palette.TinajaClayLight, Color.white, 0.10f));
            pc.FillEllipseSoft(lx, ly + (0.02f * px), TinajaOpeningRxU * px, TinajaOpeningRyU * px, 1f, TinajaOpeningFront);
            pc.FillEllipseSoft(lx, ly + (0.10f * px), TinajaOpeningRxU * px * 0.92f, TinajaOpeningRyU * px * 0.70f, 6f, TinajaOpeningDark);
            return MakeSprite(pc, "mech_tinaja_body", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Tapa de la base (TJ3): elipse blanca con un borde de TinajaBaseCapRimU al 85 % (gris; el tinte del overlay la oscurece).
        private Sprite BuildTinajaBaseCap()
        {
            const float px = MechanicPxPerU;
            float wu = 2f * TinajaHalfWidthU(0f);
            int w = Mathf.RoundToInt(wu * px) + 4;
            int h = Mathf.RoundToInt(wu * TinajaBaseCapAspect * px) + 4;
            var pc = new PixelCanvas(w, h);
            float rx = (w * 0.5f) - 2f;
            float ry = (h * 0.5f) - 2f;
            float rim = TinajaBaseCapRimU * px;
            pc.FillEllipseSoft(w * 0.5f, h * 0.5f, rx, ry, 1f, new Color(0.85f, 0.85f, 0.85f, 1f));
            pc.FillEllipseSoft(w * 0.5f, h * 0.5f, rx - rim, ry - rim, 1f, Color.white);
            return MakeSprite(pc, "mech_tinaja_base_cap", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Gota de la vuelta (TJ3): círculo blanco opaco de canto suave.
        private Sprite BuildTinajaDripDot()
        {
            const int s = 32;
            var pc = new PixelCanvas(s, s);
            pc.FillEllipseSoft(s * 0.5f, s * 0.5f, (s * 0.5f) - 1f, (s * 0.5f) - 1f, 1f, Color.white);
            return MakeSprite(pc, "mech_tinaja_drip", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Elipse blanca del agua de la boca, de borde suave (el tinte lo pone Image.color).
        private Sprite BuildTinajaMouthDisc()
        {
            const float px = MechanicPxPerU;
            int w = Mathf.RoundToInt(TinajaMouthWidthU * px) + 4;
            int h = Mathf.RoundToInt(TinajaMouthHeightU * px) + 4;
            var pc = new PixelCanvas(w, h);
            pc.FillEllipseSoft(w * 0.5f, h * 0.5f, (w * 0.5f) - 2f, (h * 0.5f) - 2f, 1f, Color.white);
            return MakeSprite(pc, "mech_tinaja_mouth", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Centro del fondo seco: el alfa cae de 1 en el centro a 0 a 0,85 del radio (suave, sin canto).
        private Sprite BuildTinajaDryMouthCenter()
        {
            const float px = MechanicPxPerU;
            int w = Mathf.RoundToInt(TinajaMouthWidthU * px) + 4;
            int h = Mathf.RoundToInt(TinajaMouthHeightU * px) + 4;
            var pc = new PixelCanvas(w, h);
            float rx = (w * 0.5f) - 2f;
            float ry = (h * 0.5f) - 2f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float ex = ((x + 0.5f) - (w * 0.5f)) / rx;
                    float ey = ((y + 0.5f) - (h * 0.5f)) / ry;
                    float r = Mathf.Sqrt((ex * ex) + (ey * ey)) / 0.85f;
                    if (r < 1f)
                    {
                        float a = 1f - Mathf.SmoothStep(0f, 1f, r);
                        pc.Set(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
            }

            return MakeSprite(pc, "mech_tinaja_dry_mouth", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Sobre el agua de la boca: la sombra del labio en el canto de atrás (arriba) y un brillo arriba a la izquierda.
        private Sprite BuildTinajaMouthGloss()
        {
            const float px = MechanicPxPerU;
            int w = Mathf.RoundToInt(TinajaMouthWidthU * px) + 4;
            int h = Mathf.RoundToInt(TinajaMouthHeightU * px) + 4;
            var pc = new PixelCanvas(w, h);
            float rx = (w * 0.5f) - 2f;
            float ry = (h * 0.5f) - 2f;
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float ex = ((x + 0.5f) - cx) / rx;
                    float ey = ((y + 0.5f) - cy) / ry;
                    float rr = (ex * ex) + (ey * ey);
                    if (rr >= 1f)
                    {
                        continue;
                    }

                    // Sombra del labio: una media luna arriba, que se va hacia el centro.
                    float shade = Mathf.Clamp01((ey - 0.15f) / 0.85f) * Mathf.Clamp01((rr - 0.35f) / 0.65f);
                    if (shade > 0f)
                    {
                        pc.Blend(x, y, new Color(0.16f, 0.10f, 0.06f, 1f), 0.42f * shade);
                    }
                }
            }

            // El brillo, en el cuarto de arriba a la izquierda (la luz de D-003), nunca a la altura de la sombra del icono de la
            // fruta (abajo): así no hace una sombra doble junto a él.
            pc.FillEllipseSoft(cx + (TinajaMouthGlossOffsetU.x * px), cy + (TinajaMouthGlossOffsetU.y * px), 0.28f * rx, 0.12f * ry, 2f, ColorMath.WithAlpha(Color.white, TinajaMouthGlossAlpha));
            return MakeSprite(pc, "mech_tinaja_mouth_gloss", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Las tres piezas de una banda comparten lienzo (BandPx) y forma: un rectángulo redondeado de canto nítido (solo el
        // suavizado de 1 px), para que el relleno sea el tinte entero, sin mezclarse con el barro en un degradado.
        // Relleno: blanco opaco (lo tiñe el overlay con el HEX del tinte, alfa 1).
        private Sprite BuildTinajaBand()
        {
            return BuildTinajaBandPiece("mech_tinaja_band", 0);
        }

        // Filo de luz: las franjas de arriba y abajo (≈ 1,5 dp con la cuia de 48 dp), blancas; el overlay las pone en marfil al
        // 35 % (TinajaOverlay.BandEdgeLight).
        private Sprite BuildTinajaBandLight()
        {
            return BuildTinajaBandPiece("mech_tinaja_band_light", 1);
        }

        // Contorno: un anillo de ≈ 1 dp por dentro del canto; el overlay lo tiñe con el tinte oscurecido (solo los tintes que
        // quedan cerca del barro, TinajaOverlay.BandNeedsRim).
        private Sprite BuildTinajaBandRim()
        {
            return BuildTinajaBandPiece("mech_tinaja_band_rim", 2);
        }

        private Sprite BuildTinajaBandPiece(string name, int piece)
        {
            const int w = BandPxWidth;
            const int h = BandPxHeight;
            const float radius = 12f;
            var pc = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = PixelCanvas.RoundedRectDistance(x + 0.5f, y + 0.5f, 0.5f, 0.5f, w - 0.5f, h - 0.5f, radius);
                    float cover = PixelCanvas.Coverage(d);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    float a;
                    if (piece == 0)
                    {
                        a = cover;
                    }
                    else if (piece == 1)
                    {
                        float fromEdge = Mathf.Min(y + 0.5f, h - (y + 0.5f)); // px hasta el canto de arriba o de abajo
                        a = cover * Mathf.Clamp01(BandLightPx + 0.5f - fromEdge);
                    }
                    else
                    {
                        a = Mathf.Clamp01(cover - PixelCanvas.Coverage(d + BandRimPx));
                    }

                    if (a > 0f)
                    {
                        pc.Set(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
            }

            return MakeSprite(pc, name, new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Icono de la fruta sobre transparente: el contorno (si lo lleva) y la silueta en el color de los Patrones de fruta.
        private Sprite BuildTinajaGlyph(int tint)
        {
            const int cell = FruitIconPx;
            float[] mask = FruitIconMask(tint);
            float[] rim = FruitIconRimMask(tint);
            Color dose = Palette.Tint(tint);
            Color glyph = Color.Lerp(dose, Palette.PatternBase(tint), LiquidGraphic.FruitIconOpacity);
            Color rimColor = ColorMath.Darken(dose, LiquidGraphic.FruitIconRimDarken);
            glyph.a = 1f;
            rimColor.a = 1f;
            var pc = new PixelCanvas(cell, cell);
            for (int y = 0; y < cell; y++)
            {
                for (int x = 0; x < cell; x++)
                {
                    int i = (y * cell) + x;
                    pc.Blend(x, y, rimColor, rim[i]);
                    pc.Blend(x, y, glyph, mask[i]);
                }
            }

            return MakeSprite(pc, "mech_tinaja_glyph_" + TintIds[tint], new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Píldora de madera del chip: canto oscuro de 1 dp, madera y un brillo arriba (la luz de juego, D-003). El brillo es una
        // franja de 2 dp pegada al canto de arriba (de 15 a 17 dp), por encima de los glifos: la cifra (≈ 10,3 dp, centrada en el
        // chip de 18 dp) acaba a ≈ 14,1 dp, así que nunca cae sobre él y queda entera sobre la madera.
        private Sprite BuildTinajaChipPill()
        {
            const float k = UiPxPerDp;
            int h = Mathf.RoundToInt(TinajaChipHeightDp * k);
            int w = h + 24;
            float r = h * 0.5f;
            var pc = new PixelCanvas(w, h);
            pc.FillRoundedRect(0f, 0f, w, h, r, Palette.WoodDark);
            pc.FillRoundedRect(k, k, w - k, h - k, r - k, TinajaChipWood);
            pc.FillRoundedRectSoft(3.5f * k, h - (TinajaChipGlossTopDp + TinajaChipGlossDp) * k, w - (3.5f * k), h - (TinajaChipGlossTopDp * k), TinajaChipGlossDp * k * 0.5f, 1f, ColorMath.WithAlpha(Color.white, 0.14f));
            return MakeSprite(pc, "mech_tinaja_chip", new Vector2(0.5f, 0.5f), new Vector4(r + 1f, 0f, r + 1f, 0f), UiPxPerDp);
        }

        // Jirau: dos varas horizontales (sombra abajo, luz arriba y algo de veta) con una atadura de fibra en cada punta, en el
        // borde de 9 cortes; el centro se estira sin deformar las ataduras y no lleva ninguna (veto de b.2: nada entre las dos
        // piezas). Dos varas, y no tres, para que el jirau quepa entero en la cara superior de la repisa (TinajaOverlay).
        private Sprite BuildTinajaJirau()
        {
            const float k = UiPxPerDp;
            int h = Mathf.RoundToInt(TinajaJirauDesignDp * k);
            int border = Mathf.RoundToInt(TinajaJirauBorderDp * k);
            int w = (2 * border) + 48;
            var pc = new PixelCanvas(w, h);
            float rp = h / 4f;
            for (int i = 0; i < 2; i++)
            {
                float cy = rp + (i * 2f * rp);
                pc.FillCapsule(rp + 0.5f, cy - 0.6f, w - rp - 0.5f, cy - 0.6f, rp, JirauShade);
                pc.FillCapsule(rp + 0.5f, cy, w - rp - 0.5f, cy, rp - 1.0f, JirauWood);
                pc.FillCapsule(rp + 2f, cy + (0.38f * rp), w - rp - 2f, cy + (0.38f * rp), 0.22f * rp, ColorMath.Lighten(JirauWood, 0.30f));
                for (int g = 0; g < 4; g++)
                {
                    float gx = border + 6f + (g * 9f) + (i * 3f);
                    pc.FillCapsule(gx, cy - (0.25f * rp), gx + 5f, cy - (0.25f * rp), 0.35f, ColorMath.WithAlpha(JirauShade, 0.7f));
                }
            }

            DrawJirauLashing(pc, border * 0.55f, h);
            DrawJirauLashing(pc, w - (border * 0.55f), h);
            return MakeSprite(pc, "mech_tinaja_jirau", new Vector2(0.5f, 0.5f), new Vector4(border, 0f, border, 0f), UiPxPerDp);
        }

        // Atadura de fibra de una punta del jirau: vueltas diagonales que abrazan las dos varas en x = cx (dentro del borde de
        // 9 cortes, así el centro que se estira son varas lisas).
        private static void DrawJirauLashing(PixelCanvas pc, float cx, int h)
        {
            float half = 1.7f * UiPxPerDp;
            for (float y = 1.5f; y < h - 1.5f; y += 3.2f)
            {
                pc.FillCapsule(cx - half, y - 0.8f, cx + half, y + 0.8f, 1.0f, Palette.FiberShadow);
                pc.FillCapsule(cx - half, y - 0.4f, cx + half, y + 1.2f, 0.65f, Palette.FiberBase);
                pc.FillCapsule(cx - (0.4f * half), y + 0.5f, cx + (0.3f * half), y + 0.9f, 0.3f, Palette.FiberLight);
            }
        }

        // Poste del jirau a la derecha del eje (se refleja para la izquierda): una vara baja que se apoya en el jirau, con la
        // punta a TinajaPostTopU, y de la punta un gancho corto de madera que baja hacia su cuia y se curva hacia arriba bajo el
        // flanco del asa (TinajaHookU). Sin travesaño ni cuerda: nada pasa sobre el cuello de su cuia ni lo toca (la silueta
        // poste + travesaño + cuerda al cuello se leía como una horca, 07 §8). Nada une la tinaja con su cuia.
        private Sprite BuildTinajaPost()
        {
            const float px = MechanicPxPerU;
            Rect box = TinajaPostRectU;
            int w = Mathf.RoundToInt(box.width * px);
            int h = Mathf.RoundToInt(box.height * px);
            var pc = new PixelCanvas(w, h);
            float ox = -box.xMin * px;
            float oy = -box.yMin * px;
            float postX = ox + (TinajaPostXU * px);
            float postR = 0.075f * px;
            float baseY = oy - (0.05f * px); // sobre las varas del jirau
            float topY = oy + (TinajaPostTopU * px);

            // Vara vertical: sombra, madera y luz a la izquierda; la punta, redondeada.
            pc.FillCapsule(postX + 1.2f, baseY, postX + 1.2f, topY - postR, postR, JirauShade);
            pc.FillCapsule(postX, baseY, postX, topY - postR, postR - 1.2f, JirauWood);
            pc.FillCapsule(postX - (0.35f * postR), baseY + 4f, postX - (0.35f * postR), topY - (1.5f * postR), 0.22f * postR, ColorMath.Lighten(JirauWood, 0.30f));

            // Gancho: de la punta del poste baja en curva hasta la copa (TinajaHookU), bajo el flanco del asa, y su punta sube un
            // poco hacia su cuia sin llegar al borde de fibra (≥ 0,55 u del eje). Curva de Bézier cuadrática por tramos.
            var p0 = new Vector2(postX - (0.4f * postR), topY - (1.2f * postR));
            var p1 = new Vector2(ox + (0.96f * px), oy + (3.66f * px));
            var p2 = new Vector2(ox + (TinajaHookU.x * px), oy + (TinajaHookU.y * px));
            var tip = new Vector2(ox + ((TinajaHookU.x - 0.06f) * px), oy + ((TinajaHookU.y + 0.08f) * px));
            float hookR = 0.036f * px;
            const int steps = 14;
            for (int pass = 0; pass < 2; pass++)
            {
                Color col = pass == 0 ? JirauShade : JirauWood;
                float rr = pass == 0 ? hookR + 1.1f : hookR;
                float dy = pass == 0 ? -1f : 0f;
                Vector2 prev = p0;
                for (int s = 1; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    Vector2 q = ((1f - t) * (1f - t) * p0) + (2f * (1f - t) * t * p1) + (t * t * p2);
                    pc.FillCapsule(prev.x, prev.y + dy, q.x, q.y + dy, rr, col);
                    prev = q;
                }

                pc.FillCapsule(p2.x, p2.y + dy, tip.x, tip.y + dy, rr * 0.9f, col);
            }

            // Una atadura de fibra donde el gancho sale de la punta (en el poste, lejos de su cuia).
            for (int i = 0; i < 3; i++)
            {
                float y = topY - (0.10f * px) - (i * 0.045f * px);
                pc.FillCapsule(postX - postR - 1f, y - 1.2f, postX + postR + 1f, y + 1.2f, 1.1f, Palette.FiberBase);
            }

            return MakeSprite(pc, "mech_tinaja_post", new Vector2(ox / w, oy / h), Vector4.zero);
        }

        // Asa de fibra de su cuia: un arco (TinajaAsaYAt) que sale de los dos hombros de la Cuia chica de 2 (±TinajaAsaHalfU, a
        // TinajaAsaFootU) y sube holgado por encima del cuello y del borde de fibra hasta TinajaAsaTopU; en cada hombro, el nudito
        // que la sujeta. No toca el cuello ni el borde.
        private Sprite BuildTinajaAsa()
        {
            const float px = MechanicPxPerU;
            Rect box = TinajaAsaRectU;
            int w = Mathf.RoundToInt(box.width * px);
            int h = Mathf.RoundToInt(box.height * px);
            var pc = new PixelCanvas(w, h);
            float ox = -box.xMin * px;
            float oy = -box.yMin * px;
            float r = 0.034f * px;
            const int steps = 48;
            for (int pass = 0; pass < 2; pass++)
            {
                Color col = pass == 0 ? Palette.FiberShadow : Palette.FiberBase;
                float rr = pass == 0 ? r + 0.8f : r;
                Vector2 prev = AsaPoint(-1f, ox, oy, px);
                for (int s = 1; s <= steps; s++)
                {
                    Vector2 q = AsaPoint(-1f + (2f * s / steps), ox, oy, px);
                    pc.FillCapsule(prev.x, prev.y, q.x, q.y, rr, col);
                    prev = q;
                }
            }

            // Brillo de la fibra en la cima y los nuditos de los hombros.
            for (int s = -6; s < 6; s++)
            {
                Vector2 a = AsaPoint(s / 24f, ox, oy, px);
                Vector2 b = AsaPoint((s + 1) / 24f, ox, oy, px);
                pc.FillCapsule(a.x, a.y + (0.35f * r), b.x, b.y + (0.35f * r), 0.3f * r, Palette.FiberLight);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float kx = ox + (side * TinajaAsaHalfU * px);
                float ky = oy + (TinajaAsaFootU * px);
                pc.FillCircle(kx, ky, 1.6f * r, Palette.FiberShadow);
                pc.FillCircle(kx - (0.3f * r), ky + (0.3f * r), 0.9f * r, Palette.FiberBase);
            }

            return MakeSprite(pc, "mech_tinaja_asa", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Punto del asa en px del sprite para s ∈ [−1, 1]: x = s · medio ancho (de un hombro al otro). Cerca de los hombros, donde
        // el arco sube casi recto, se reparte por la altura para que los tramos no salten.
        private static Vector2 AsaPoint(float s, float ox, float oy, float px)
        {
            // Reparto por ángulo: x = ±a·|cos θ|^(2/p), y = arranque + b·|sen θ|^(2/p) (la misma curva que TinajaAsaYAt).
            float theta = (1f - ((s + 1f) * 0.5f)) * Mathf.PI;
            float c = Mathf.Cos(theta);
            float sn = Mathf.Sin(theta);
            float e = 2f / TinajaAsaExponent;
            float x = Mathf.Sign(c) * TinajaAsaHalfU * Mathf.Pow(Mathf.Abs(c), e);
            float y = TinajaAsaFootU + ((TinajaAsaTopU - TinajaAsaFootU) * Mathf.Pow(Mathf.Abs(sn), e));
            return new Vector2(ox + (x * px), oy + (y * px));
        }
    }
}
