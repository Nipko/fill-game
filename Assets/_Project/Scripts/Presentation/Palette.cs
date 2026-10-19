#nullable enable
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Colores del juego con los HEX canónicos: 14 tintes (GDD 07 §3, glosario §9), agua base de los mundos
    /// (07 §2.1, 08 §5.1), tapa de calabaza (07 §4.4) y tokens de UI (07 §7.2). Los tintes solo se usan dentro de las
    /// cuias (07 §3, regla 6); la UI vive en los tokens.
    /// </summary>
    public static class Palette
    {
        private static readonly Color32[] Tints =
        {
            new Color32(0x00, 0x00, 0x00, 0x00), // 0: sin color
            Hex(0xFAFAF7), // 1 guanabana  · Guanábana
            Hex(0xE6D3A3), // 2 cupuacu    · Copoazú
            Hex(0xF2B705), // 3 tapereba   · Taperebá
            Hex(0xEF7D1A), // 4 aguaje     · Aguaje
            Hex(0xCF3E1C), // 5 urucum     · Achiote
            Hex(0x9E1B22), // 6 camu_camu  · Camu camu
            Hex(0xE58FC6), // 7 bacaba     · Bacaba
            Hex(0x6A2C91), // 8 acai       · Açaí
            Hex(0x1F2F6B), // 9 jenipapo   · Huito
            Hex(0x3BB8D9), // 10 agua_tapajos · Agua del Tapajós
            Hex(0x4EB265), // 11 lulo      · Lulo
            Hex(0xBBCC33), // 12 araza     · Arazá
            Hex(0x5C3317), // 13 cacao     · Cacao
            Hex(0xA9672E), // 14 agua_negro · Agua del Río Negro
        };

        /// <summary>
        /// Agua base negra (mundos 3 y 10; 07 §2.1) en ámbar-té translúcido, #522500 [P, hasta que DC1b cambie el HEX canónico
        /// de 07 §2.1 y de la guía visual] (D-029; guion de la intro §18, Q14-A; tarea DC13): sustituye a #4A2C14, que al 70 %
        /// sobre la cavidad se leía casi negro. L* casi igual (20,5 frente a 21,2; ΔL* 29,4 frente al tinte 14, Agua del Río
        /// Negro #A9672E) con C* 35,2 en lugar de 23,8 y h 58° (ámbar, hacia el tinte 14, no hacia el rojo); compuesta en lineal
        /// al <c>IntroAnimation.WaterAlpha</c> (70 %, sin cambiar) sobre la cavidad, #462413: ΔL* 31,5 frente al tinte 14,
        /// C* 23,1 (antes 15,4) y h 52°. ΔE00 20,1 frente al Barro (18,7 en la cavidad) y ≥ 27,7 frente a la blanca, la blanca
        /// de la franja (la más cercana) y la clara. Lo comprueba <c>WaterBlackContrastTests</c>.
        /// </summary>
        public static readonly Color WaterBlack = Hex(0x522500);

        /// <summary>Agua base blanca #B08A5A (07 §2.1; Estuario).</summary>
        public static readonly Color WaterWhite = Hex(0xB08A5A);

        /// <summary>Agua base clara #2E8B8B (07 §2.1).</summary>
        public static readonly Color WaterClear = Hex(0x2E8B8B);

        /// <summary>
        /// Agua parda opaca de las dosis ocultas de Aguas turbias, #8A7B6A (07 §4.3; 08 §5.2 <c>_MurkyColor</c>). Una dosis
        /// oculta nunca enseña su tinte real.
        /// </summary>
        public static readonly Color Murky = Hex(0x8A7B6A);

        /// <summary>
        /// Barro (color 15 del motor; 02 §9.1 M5): pardo oliva de várzea #5F563A [P, hasta el arte final]. ΔE00 ≥ 17 frente a
        /// los 14 tintes (el más cercano, Cacao) y ≥ 16 frente a <see cref="Murky"/> y a las tres aguas base (02 §4.2;
        /// direccion-visual-v2 §2.4, regla 1). Lo comprueba <c>MurkyMudContentTests</c>.
        /// </summary>
        public static readonly Color Mud = Hex(0x5F563A);

        /// <summary>
        /// Barro cocido claro de la Tinaja (M13; D-019; 07 §4.3; propuesta de la Tinaja, b.2 «Aspecto»), #C69A78, dentro del
        /// rango #C4906E-#C7A182 [P, hasta el arte del artista]: ΔE00 ≥ 15 frente a los 14 tintes (el más cercano, Aguaje) y
        /// ≥ 15,9 frente a <see cref="Murky"/>. Es el color principal de la vasija opaca y el de sus dosis ocultas en el líquido
        /// (<see cref="HiddenDose"/>): la tinaja nunca se pinta como agua turbia (contrato 27; 08 §5.2).
        /// </summary>
        public static readonly Color TinajaClay = Hex(0xC69A78);

        /// <summary>Barro de la Tinaja en la luz (el extremo claro del rango de 07 §4.3, #C7A182).</summary>
        public static readonly Color TinajaClayLight = Hex(0xC7A182);

        /// <summary>Barro de la Tinaja en la sombra (el extremo oscuro del rango de 07 §4.3, #C4906E).</summary>
        public static readonly Color TinajaClayShade = Hex(0xC4906E);

        /// <summary>
        /// Color con el que la vista pinta en el líquido una dosis oculta de una cuia con la mecánica <paramref name="tinaja"/>:
        /// con la Tinaja, su barro opaco (<see cref="TinajaClay"/>; sus ocultas no son agua turbia, contrato 27); si no, el
        /// pardo de Aguas turbias (<see cref="Murky"/>).
        /// </summary>
        public static Color HiddenDose(bool tinaja)
        {
            return tinaja ? TinajaClay : Murky;
        }

        /// <summary>Tapa de calabaza, madera clara #C9A36B (07 §4.4).</summary>
        public static readonly Color CapWood = Hex(0xC9A36B);

        /// <summary>Fibra de arumã trenzada, ocre: borde y pie de la Cuia de vidrio (07 §4.6). Tono provisional.</summary>
        public static readonly Color Fiber = Hex(0xA8834F);

        /// <summary><c>ui_papel</c> #F4EBDA: paneles y modales (07 §7.2).</summary>
        public static readonly Color UiPaper = Hex(0xF4EBDA);

        /// <summary><c>ui_tinta</c> #2B2118: texto principal (07 §7.2).</summary>
        public static readonly Color UiInk = Hex(0x2B2118);

        /// <summary><c>ui_madera</c> #6B4A2B: marcos, bandeja cumbuca, botón secundario (07 §7.2).</summary>
        public static readonly Color UiWood = Hex(0x6B4A2B);

        /// <summary><c>ui_oliva</c> #3E5C3A: fondo de HUD translúcido (07 §7.2).</summary>
        public static readonly Color UiOlive = Hex(0x3E5C3A);

        /// <summary><c>ui_verde_boton</c> #3F6B33: botón primario (07 §7.2).</summary>
        public static readonly Color UiGreenButton = Hex(0x3F6B33);

        /// <summary><c>ui_ambar</c> #C98A2B (07 §7.2).</summary>
        public static readonly Color UiAmber = Hex(0xC98A2B);

        /// <summary><c>ui_indigo</c> #24305E (07 §7.2).</summary>
        public static readonly Color UiIndigo = Hex(0x24305E);

        /// <summary><c>ui_rojo_aviso</c> #9E3B2E (07 §7.2).</summary>
        public static readonly Color UiRedWarning = Hex(0x9E3B2E);

        /// <summary>
        /// Borde inferior de la "piedra de río" de papel (07 §7.3: 2 dp más oscuro): papel × 0,72. Tono derivado, fuera de
        /// la tabla de tokens de 07 §7.2.
        /// </summary>
        public static readonly Color StoneEdge = new Color(0xF4 / 255f * 0.72f, 0xEB / 255f * 0.72f, 0xDA / 255f * 0.72f, 1f);

        /// <summary>Turquesa del icono de Marea, #3BB8D9 al 80 % (07 §7.6).</summary>
        public static readonly Color IconTurquoise = new Color(0x3B / 255f, 0xB8 / 255f, 0xD9 / 255f, 0.8f);

        /// <summary>Plumas del confeti de guacamayo, rojo y azul (02 §4.1; 07 §5.4). Tonos provisionales, fuera de la paleta de tintes.</summary>
        public static readonly Color FeatherRed = Hex(0xC23A2B);

        /// <summary>Pluma azul de guacamayo (provisional).</summary>
        public static readonly Color FeatherBlue = Hex(0x2F6FD6);

        // ------------------------------------------------------------------------------------------------------------
        // Dirección visual v2 «Selva brillante premium» (D-003; docs/art/direccion-visual-v2.md 2.2-2.3). Tokens de
        // entorno y de UI; los HEX de los tintes no cambian (2.1) y siguen prohibidos en la UI (07 §3, regla 6).
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// <c>cielo_alto</c>: #5BC0EB (revisión visual del 2026-10-07: el #7CCBE8 de la guía 2.2 dejaba la escena casi
        /// monocroma). Es también el relleno de cielo sobre el panorama cuando el horizonte baja (GameScreen).
        /// </summary>
        public static readonly Color SkyTop = Hex(0x5BC0EB);

        /// <summary><c>cielo_medio</c> #FFC994.</summary>
        public static readonly Color SkyMid = Hex(0xFFC994);

        /// <summary><c>cielo_horizonte</c> #FFE4A8: horizonte y halo del sol.</summary>
        public static readonly Color SkyHorizon = Hex(0xFFE4A8);

        /// <summary><c>sol</c> #FFF6D6.</summary>
        public static readonly Color Sun = Hex(0xFFF6D6);

        /// <summary><c>nube_luz</c>: #FFF6EA (revisión visual del 2026-10-07; la guía 2.2 decía #FFF0DC).</summary>
        public static readonly Color CloudLight = Hex(0xFFF6EA);

        /// <summary><c>nube_sombra</c> #F3AE8A.</summary>
        public static readonly Color CloudShadow = Hex(0xF3AE8A);

        /// <summary><c>orilla_lejana</c> #7FB0A2.</summary>
        public static readonly Color FarShore = Hex(0x7FB0A2);

        /// <summary><c>palmera_lejana</c> #4E8C78: siluetas de palmeras de açaí.</summary>
        public static readonly Color FarPalm = Hex(0x4E8C78);

        /// <summary><c>selva_media</c> #2C7A4B.</summary>
        public static readonly Color JungleMid = Hex(0x2C7A4B);

        /// <summary><c>selva_luz</c> #63B548.</summary>
        public static readonly Color JungleLight = Hex(0x63B548);

        /// <summary><c>selva_sombra</c> #1C5236.</summary>
        public static readonly Color JungleShadow = Hex(0x1C5236);

        /// <summary><c>rio_reflejo</c> #F6CF92.</summary>
        public static readonly Color RiverReflect = Hex(0xF6CF92);

        /// <summary><c>rio_medio</c> #CDA06A.</summary>
        public static readonly Color RiverMid = Hex(0xCDA06A);

        /// <summary><c>rio_base</c> #B08A5A (= agua base blanca del Estuario, 07 §2.1).</summary>
        public static readonly Color RiverBase = Hex(0xB08A5A);

        /// <summary><c>rio_profundo</c> #80603E.</summary>
        public static readonly Color RiverDeep = Hex(0x80603E);

        /// <summary>Río a +60 dp bajo el horizonte, #8A6C48 (revisión visual del 2026-10-07: el río no debe leerse como arena).</summary>
        public static readonly Color RiverLow = Hex(0x8A6C48);

        /// <summary>Río a +140 dp bajo el horizonte, #6B5640 (revisión visual del 2026-10-07).</summary>
        public static readonly Color RiverFar = Hex(0x6B5640);

        /// <summary>Línea de agua al pie de las orillas, #F7E6C4.</summary>
        public static readonly Color WaterLine = Hex(0xF7E6C4);

        /// <summary><c>destello</c> #FFF4D2: brillos aditivos del agua, polen.</summary>
        public static readonly Color Glint = Hex(0xFFF4D2);

        /// <summary><c>hoja_oscura</c> #145A35.</summary>
        public static readonly Color LeafDark = Hex(0x145A35);

        /// <summary><c>hoja_media</c> #23824A.</summary>
        public static readonly Color LeafMid = Hex(0x23824A);

        /// <summary><c>hoja_clara</c> #5DBB3F.</summary>
        public static readonly Color LeafLight = Hex(0x5DBB3F);

        /// <summary><c>hoja_brillo</c> #C6EF6B.</summary>
        public static readonly Color LeafShine = Hex(0xC6EF6B);

        /// <summary><c>nervadura</c> #0E3F25.</summary>
        public static readonly Color Vein = Hex(0x0E3F25);

        /// <summary><c>heliconia</c> #E2483A: un único acento floral por racimo (escena, no UI).</summary>
        public static readonly Color Heliconia = Hex(0xE2483A);

        /// <summary><c>muelle_luz</c>: #C08A58 (revisión visual del 2026-10-07; la guía 2.2 decía #A87A50).</summary>
        public static readonly Color DockLight = Hex(0xC08A58);

        /// <summary><c>muelle_base</c>: #A8774C, L* 54 (revisión visual del 2026-10-07: con #80583A, L* 41, la escena era marrón sobre marrón).</summary>
        public static readonly Color DockBase = Hex(0xA8774C);

        /// <summary>
        /// Cara soleada de las tablas en el tercio superior del muelle, #C99A6A, L* 67 (revisión visual del 2026-10-07: cara
        /// de L* 64-68 arriba para que el muelle no sea una masa parda de valor medio).
        /// </summary>
        public static readonly Color DockSunlit = Hex(0xC99A6A);

        /// <summary><c>muelle_sombra</c> #5A3D28.</summary>
        public static readonly Color DockShadow = Hex(0x5A3D28);

        /// <summary><c>ranura</c> #2A1A10: juntas y ranuras talladas.</summary>
        public static readonly Color Groove = Hex(0x2A1A10);

        /// <summary><c>fibra_luz</c> #EED29A.</summary>
        public static readonly Color FiberLight = Hex(0xEED29A);

        /// <summary><c>fibra_base</c> #C49A5C.</summary>
        public static readonly Color FiberBase = Hex(0xC49A5C);

        /// <summary><c>fibra_sombra</c> #8A6436.</summary>
        public static readonly Color FiberShadow = Hex(0x8A6436);

        /// <summary><c>marco</c> #7C4E3A: marco del panel, madera media rojiza.</summary>
        public static readonly Color Frame = Hex(0x7C4E3A);

        /// <summary><c>marco_bisel</c> #D29B6E.</summary>
        public static readonly Color FrameBevel = Hex(0xD29B6E);

        /// <summary><c>madera_oscura</c> #36261C: bandeja, placa del nivel y marco de los modales.</summary>
        public static readonly Color WoodDark = Hex(0x36261C);

        /// <summary><c>repisa_sup</c> #C4935E: cara superior de la repisa (L* ≤ 65, VA-04b).</summary>
        public static readonly Color ShelfTop = Hex(0xC4935E);

        /// <summary><c>repisa_frente</c> #866650.</summary>
        public static readonly Color ShelfFront = Hex(0x866650);

        /// <summary><c>repisa_frente_inf</c> #6E523E.</summary>
        public static readonly Color ShelfFrontLow = Hex(0x6E523E);

        /// <summary><c>repisa_canto</c> #F2C38A: bisel iluminado.</summary>
        public static readonly Color ShelfEdge = Hex(0xF2C38A);

        /// <summary><c>vidrio_borde</c> #EAF7F3: contorno y fresnel del vidrio.</summary>
        public static readonly Color GlassRim = Hex(0xEAF7F3);

        /// <summary>Tinte del cuerpo del vidrio #CFEFE6 (guía 4.1; <c>glass_tint</c> de 07 §4.6).</summary>
        public static readonly Color GlassTint = Hex(0xCFEFE6);

        /// <summary><c>scrim</c> #0B2A33: velo bajo la barra de estado.</summary>
        public static readonly Color Scrim = Hex(0x0B2A33);

        /// <summary><c>crema</c> #FFF8E6: texto y glifos sobre botones, placas y madera.</summary>
        public static readonly Color Cream = Hex(0xFFF8E6);

        /// <summary><c>tinta</c> #2B2118 (= <c>ui_tinta</c>).</summary>
        public static readonly Color Ink = Hex(0x2B2118);

        /// <summary><c>panel_claro</c> #FBF0D9: superficie de modales (madera clara pulida, nunca papel).</summary>
        public static readonly Color PanelLight = Hex(0xFBF0D9);

        /// <summary><c>panel_claro_fondo</c> #F2E4C6.</summary>
        public static readonly Color PanelLightLow = Hex(0xF2E4C6);

        /// <summary><c>deshabilitado</c> #A9A39A.</summary>
        public static readonly Color Disabled = Hex(0xA9A39A);

        /// <summary><c>deshabilitado_texto</c> #3A3530 (4,85:1).</summary>
        public static readonly Color DisabledText = Hex(0x3A3530);

        /// <summary><c>deshabilitado_glifo</c> #4A4540 (3,79:1).</summary>
        public static readonly Color DisabledGlyph = Hex(0x4A4540);

        /// <summary><c>deshabilitado_contorno</c> #7C766D.</summary>
        public static readonly Color DisabledOutline = Hex(0x7C766D);

        /// <summary>Familia <b>Hoja</b> (primario: Siguiente).</summary>
        public static readonly ButtonFamily FamilyLeaf = new ButtonFamily(Hex(0x86D25A), Hex(0x2F7D32), Hex(0x236A29), Hex(0x17501E), Hex(0x0F3A15));

        /// <summary>Familia <b>Agua</b> (Marea: Deshacer en la bandeja y en el modal).</summary>
        public static readonly ButtonFamily FamilyWater = new ButtonFamily(Hex(0x8ADBE6), Hex(0x13788F), Hex(0x0F6275), Hex(0x0A4A59), Hex(0x073640));

        /// <summary>Familia <b>Madera</b> (secundario: Reiniciar, placa del nivel).</summary>
        public static readonly ButtonFamily FamilyWood = new ButtonFamily(Hex(0xBF8F62), Hex(0x7E5638), Hex(0x46302A), Hex(0x2E2018), Hex(0x1E140E));

        /// <summary>
        /// Panel del tablero «azul petróleo de río», elegido por el dueño el 2026-10-07 (D-004; guía 4.6): #234452 (L* 27)
        /// → #142C36 (L* 17), cavidad #102228. C* 14,2 / 11,0 (≤ 16, VA-04), ΔE00 ≥ 14 frente a Huito.
        /// </summary>
        public static readonly PanelColors Panel = new PanelColors(Hex(0x234452), Hex(0x142C36), Hex(0x102228));

        /// <summary>
        /// Color opaco precalculado de la franja especular principal sobre el líquido de un tinte (guía 4.2): el tinte con
        /// +12 L* (CIELAB) si su L* &lt; 40 y +10 en el resto; Guanábana, sin franja. Opaco, como <see cref="PatternMark"/>:
        /// así el resultado no depende del espacio de mezcla (2.5).
        /// </summary>
        public static Color LiquidHighlight(int tint, Color dose)
        {
            float dl = HighlightDeltaL(tint, dose);
            return dl <= 0f ? dose : ColorMath.AddLabLightness(dose, dl);
        }

        /// <summary>ΔL* de la franja principal para el tinte <paramref name="tint"/> (0 = sin franja; guía 4.2).</summary>
        public static float HighlightDeltaL(int tint, Color dose)
        {
            if (tint == 1)
            {
                return 0f; // Guanábana: sin franja
            }

            return ColorMath.LabLightness(dose) < 40f ? 12f : 10f;
        }

        /// <summary>
        /// Nivel desde el que el ajuste Patrones de fruta arranca activado: default de <c>ui_patterns_default_from_level</c> = 41
        /// (08 §9; 07 §3, regla 1; 02 §12; glosario §9). Sin Remote Config real todavía: es la constante.
        /// </summary>
        public const int PatternsDefaultFromLevel = 41;

        /// <summary>
        /// Opacidad del patrón con Patrones de fruta activado: default de <c>ui_pattern_opacity</c> = 0,20 (08 §9, rango 0,15-0,35;
        /// 07 §3, regla 1, valor canónico). Sin Remote Config real todavía: es la constante.
        /// </summary>
        public const float PatternOpacityOn = 0.20f;

        /// <summary>
        /// Opacidad del patrón que se dibuja «siempre», sutil, sin Patrones de fruta (02 §12; 07 §3, regla 1: 15 % hasta el
        /// nivel 40).
        /// </summary>
        public const float PatternOpacitySubtle = 0.15f;

        /// <summary>
        /// Interruptor Patrones de fruta del nivel <paramref name="level"/> (T-E.6): activo desde
        /// <see cref="PatternsDefaultFromLevel"/>. Es el valor del toggle de Ajustes en automático (07 §3, reglas 1 y 5; 02 §12;
        /// <c>settings.patterns</c> = auto): con on u off, el ajuste manda (lo resuelve <c>AF.UI</c> y lo aplica
        /// <see cref="BoardAnimator.ApplyPatterns"/>; tarea L3). Gobierna la opacidad del 20 % y el icono de cada dosis; el icono de
        /// la cuia elevada (07 §3, regla 2) no depende de él.
        /// </summary>
        public static bool PatternsOnFor(int level)
        {
            return level >= PatternsDefaultFromLevel;
        }

        /// <summary>
        /// Opacidad del patrón en el nivel <paramref name="level"/>: <see cref="PatternOpacitySubtle"/> (0,15) hasta el 40 y
        /// <see cref="PatternOpacityOn"/> (0,20) desde el 41 (<see cref="PatternsOnFor"/>). Sin asignaciones.
        /// </summary>
        public static float PatternOpacityFor(int level)
        {
            return PatternsOnFor(level) ? PatternOpacityOn : PatternOpacitySubtle;
        }

        /// <summary>
        /// Opacidad del patrón del nivel cargado: la que lleva la propiedad <c>_PatternOpacity</c> del material compartido del
        /// líquido (la fija <see cref="LiquidGraphic.ApplyPatternOpacity"/> al cargar el nivel) y la de <see cref="PatternMark"/>
        /// (el bocado de la Piraña). Sin nivel cargado, la sutil.
        /// </summary>
        public static float PatternOpacity { get; private set; } = PatternOpacitySubtle;

        /// <summary>Fija <see cref="PatternOpacity"/> (solo <see cref="LiquidGraphic.ApplyPatternOpacity"/>, con el material).</summary>
        internal static void SetPatternOpacity(float opacity)
        {
            PatternOpacity = Mathf.Clamp01(opacity);
        }

        /// <summary>
        /// Color de las marcas del patrón: tinta <c>ui_tinta</c> sobre los tintes claros y marfil <c>ui_papel</c> sobre
        /// los de L* &lt; 52 de la tabla de 07 §3 (5, 6, 8, 9, 13 y 14; 07 §3 fija "patrón en marfil" para Huito).
        /// </summary>
        public static Color PatternBase(int tint)
        {
            switch (tint)
            {
                case 5:
                case 6:
                case 8:
                case 9:
                case 13:
                case 14:
                    return UiPaper;
                default:
                    return UiInk;
            }
        }

        /// <summary>
        /// Color opaco de las marcas del patrón sobre una dosis de color <paramref name="dose"/> con la opacidad del nivel
        /// cargado (<see cref="PatternOpacity"/>: 15 % hasta el 40 y 20 % desde el 41, la misma que el shader del líquido): la
        /// mezcla se hace aquí, en sRGB, y la marca se pinta opaca. Así el porcentaje es el que ve el jugador: con el espacio de
        /// color Linear del proyecto, un alfa del 15 % en la UI se mezclaría en lineal y no daría el mismo contraste (la misma
        /// causa que aclaraba los textos con alfa).
        /// </summary>
        public static Color PatternMark(Color dose, int tint)
        {
            return PatternMark(dose, tint, PatternOpacity);
        }

        /// <summary>Como <see cref="PatternMark(Color, int)"/> con la opacidad <paramref name="opacity"/> (0..1).</summary>
        public static Color PatternMark(Color dose, int tint, float opacity)
        {
            Color mark = Color.Lerp(dose, PatternBase(tint), opacity);
            mark.a = dose.a;
            return mark;
        }

        /// <summary>
        /// Color del tinte con índice de motor <paramref name="tint"/> (1-14), o <see cref="Mud"/> para el Barro (15). Fuera de
        /// rango, magenta (error visible).
        /// </summary>
        public static Color Tint(int tint)
        {
            if (tint == Cuia.MudColor)
            {
                return Mud;
            }

            if (tint < 1 || tint >= Tints.Length)
            {
                return Color.magenta;
            }

            return Tints[tint];
        }

        /// <summary>
        /// Aclarado del menisco (07 §3, regla 4): +20 % de luminosidad; +25-30 % en los tintes de L* &lt; 35
        /// (6 y 8 al 25 %, 9 y 13 al 30 %, según la tabla de 07 §3).
        /// </summary>
        public static float MeniscusLighten(int tint)
        {
            switch (tint)
            {
                case 6:
                case 8:
                    return 0.25f;
                case 9:
                case 13:
                    return 0.30f;
                default:
                    return 0.20f;
            }
        }

        /// <summary>Agua base del preset <c>water_base</c> (<c>black</c>, <c>white</c>, <c>clear</c>; glosario §11).</summary>
        public static Color WaterBase(string waterBase)
        {
            switch (waterBase)
            {
                case "black":
                    return WaterBlack;
                case "clear":
                    return WaterClear;
                default:
                    return WaterWhite;
            }
        }

        /// <summary>Color opaco desde un entero 0xRRGGBB.</summary>
        public static Color32 Hex(int rgb)
        {
            return new Color32((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), 0xFF);
        }
    }

    /// <summary>Familia de botón (guía 2.3): cara con degradado <c>_luz</c> → <c>_base</c> (25 %) → <c>_fondo</c>, canto y contorno.</summary>
    public readonly struct ButtonFamily
    {
        /// <summary>Crea la familia.</summary>
        public ButtonFamily(Color light, Color baseColor, Color deep, Color edge, Color outline)
        {
            Light = light;
            Base = baseColor;
            Deep = deep;
            Edge = edge;
            Outline = outline;
        }

        /// <summary><c>_luz</c> (0 % de la cara).</summary>
        public Color Light { get; }

        /// <summary><c>_base</c> (25 %).</summary>
        public Color Base { get; }

        /// <summary><c>_fondo</c> (100 %).</summary>
        public Color Deep { get; }

        /// <summary><c>_canto</c>.</summary>
        public Color Edge { get; }

        /// <summary><c>_contorno</c>.</summary>
        public Color Outline { get; }

        /// <summary>Color de la cara a la altura <paramref name="t"/> (0 arriba, 1 abajo).</summary>
        public Color FaceAt(float t)
        {
            return t < 0.25f ? Color.Lerp(Light, Base, t / 0.25f) : Color.Lerp(Base, Deep, (t - 0.25f) / 0.75f);
        }
    }

    /// <summary>Colores del panel del tablero (guía 4.6): relleno alto y bajo y cavidad vacía de la cuia.</summary>
    public readonly struct PanelColors
    {
        /// <summary>Crea los colores.</summary>
        public PanelColors(Color top, Color bottom, Color cavity)
        {
            Top = top;
            Bottom = bottom;
            Cavity = cavity;
        }

        /// <summary><c>panel_alto</c>.</summary>
        public Color Top { get; }

        /// <summary><c>panel_bajo</c>.</summary>
        public Color Bottom { get; }

        /// <summary><c>cavidad</c>: el tono medio del panel × 0,6, opaca (D-003).</summary>
        public Color Cavity { get; }
    }
}
