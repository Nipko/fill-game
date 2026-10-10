#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de las etiquetas de dificultad de D-009 (propuesta-mecanicas-dificultad.md §c.1-c.2; 07 §7.7; 02 §10;
    /// glosario §6.1), en la línea de D-003 (luz de juego, madera y vidrio; nunca papel ni acuarela): el sello de la rana dardo
    /// naranja y negra (Difícil) y el del jaguar de perfil con rosetas sobre la noche (Muy difícil), sin calavera; la cinta
    /// «×3»/«×4» de agua profunda clavada a la píldora con un remache; la píldora ámbar o índigo del chip; la banda de color de las
    /// cabeceras de ¿Seguro? y Nivel fallido; y la variante nocturna del botón «Jugar · Nivel {n}». Todo se genera de forma
    /// perezosa la primera vez que se pide (un inicio sin etiqueta no crea ninguna textura) y, tras <see cref="Dispose"/>, los
    /// sprites destruidos se vuelven a generar (el == de Unity). Lo sustituye el arte del artista. Los sellos de fauna (rana
    /// dardo y jaguar) quedan pendientes de revisión del asesor cultural (07 §8.2).
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Lado de los sellos de etiqueta <see cref="RanaDardo"/> y <see cref="JaguarHead"/> (dp).</summary>
        public const float LabelIconDp = 32f;

        /// <summary>Alto de la píldora del chip de dificultad (dp; 07 §7.7: «pill»).</summary>
        public const float LabelPillDp = 26f;

        /// <summary>Sombra bajo la píldora, incluida en el sprite de <see cref="DifficultyPill"/> (dp).</summary>
        public const float LabelPillShadowDp = 2f;

        /// <summary>Ancho del sprite de la cinta <see cref="RibbonTag"/> (dp), con su sombra.</summary>
        public const float RibbonTagWidthDp = 32f;

        /// <summary>Alto del sprite de la cinta <see cref="RibbonTag"/> (dp), con su sombra.</summary>
        public const float RibbonTagHeightDp = 24f;

        /// <summary>Borde izquierdo del cuerpo de la cinta dentro de su sprite (dp): lo que queda a la izquierda es margen de antialias.</summary>
        public const float RibbonTagBodyLeftDp = 2f;

        /// <summary>
        /// Zona de texto de la cinta dentro de su sprite (dp desde la esquina superior izquierda: x, y, ancho y alto): el cuerpo
        /// de agua a la derecha del remache y antes de la cola en V.
        /// </summary>
        public static readonly Rect RibbonTagTextDp = new Rect(6f, 2.5f, 21f, 17.5f);

        /// <summary>
        /// Naranja de advertencia de la rana dardo #D05802 [P] (07 §7.7: «naranja/negro»). No es el HEX de ningún tinte (07 §3,
        /// regla 6; direccion-visual-v2 §2.4.1 y §7): su tinte más cercano es el Agua del Río Negro, a ΔE00 9,8 (9,77), y le
        /// siguen el Achiote (9,8; 9,82) y el Aguaje (11,7). Cumple el ≥ 5 de §2.4, regla 1, que vale para los tokens de UI que
        /// no tocan una cuia (el chip del inicio, las cabeceras de los modales y la tarjeta previa). El ≥ 10 de esa misma regla
        /// (contrato del paso 1, ARQUITECTURA §5, COLOR) solo se le aplica si el sello va sobre una superficie que toque o rodee
        /// una cuia: si T09 lo pone así en el HUD, este naranja (por debajo de 10) tiene que cambiar o el sello, apartarse. Es un
        /// glifo sobre el disco marfil, así que va a ≥ 3:1 de todo el disco (§2.4, regla 2: 3,8:1 sobre el centro y 3,1:1 sobre
        /// el borde; el #E85D12 anterior se quedaba en 2,5-3,2:1). Lo comprueba <c>LobbyTextTests</c>.
        /// </summary>
        public static readonly Color FrogOrange = Palette.Hex(0xD05802);

        /// <summary>Brillo del lomo de la rana #FF9A5A [P] (ΔE00 8,5 del Aguaje).</summary>
        public static readonly Color FrogOrangeLight = Palette.Hex(0xFF9A5A);

        /// <summary>Negro de la rana dardo #17120E [P].</summary>
        public static readonly Color FrogBlack = Palette.Hex(0x17120E);

        /// <summary>Oro del pelaje del jaguar #E39B33 [P].</summary>
        public static readonly Color JaguarGold = Palette.Hex(0xE39B33);

        /// <summary>
        /// Color de la cinta «×3»/«×4» [P]: el agua profunda de la familia Agua (<c>agua_fondo</c> #0F6275), que se distingue del
        /// ámbar, del índigo, del verde de Jugar y de la madera del muelle (con madera clara se perdía en el muelle, revisión de
        /// la captura 40); crema encima, 6,5:1.
        /// </summary>
        public static readonly Color RibbonTagColor = Palette.FamilyWater.Deep;

        /// <summary>Lo más claro de la cinta, arriba (<see cref="RibbonTagColor"/> aclarado un 12 %).</summary>
        public static readonly Color RibbonTagLight = ColorMath.Lighten(Palette.FamilyWater.Deep, 0.12f);

        /// <summary>
        /// Familia de botón nocturna [P] de «Jugar · Nivel {n}» en Muy difícil (07 §7.7; propuesta §c.1: «el botón en variante
        /// nocturna»): índigo oscuro con la base en <c>ui_indigo</c> #24305E (marfil encima, 10,7:1).
        /// </summary>
        public static readonly ButtonFamily FamilyNight = new ButtonFamily(
            Palette.Hex(0x4C5C9E), Palette.UiIndigo, Palette.Hex(0x1A2448), Palette.Hex(0x121A38), Palette.Hex(0x0A0F24));

        // Colores de los sellos [P]. Todos quedan a ΔE00 ≥ 5 de los 14 tintes (07 §3, regla 6; direccion-visual-v2 §2.4,
        // regla 1), también el degradado de cada disco, salvo las dos excepciones de §2.4 que documenta LabelSealColors:
        // JaguarEye (≤ 2 dp) y JaguarShade (≤ 2 dp o alfa 0,55). Lo comprueba LobbyTextTests.
        //
        // Disco de la rana: marfil rosado. El borde #EFD8A6 anterior estaba a ΔE00 1,8 del Copoazú y el brillo, a 4,3; ahora el
        // borde queda a 8,6 y el brillo a 8,0.
        private static readonly Color RanaDiscCenter = Palette.Hex(0xFFF6DE);
        private static readonly Color RanaDiscEdge = Palette.Hex(0xF8DCC0);
        private static readonly Color RanaDiscGlow = ColorMath.Lighten(Palette.UiAmber, 0.75f);
        private static readonly Color RanaRing = ColorMath.Darken(Palette.UiAmber, 0.28f);
        private static readonly Color RanaRingLight = ColorMath.Lighten(Palette.UiAmber, 0.45f);
        private static readonly Color RanaRingInner = Palette.Hex(0x5A3A10);

        // Disco del jaguar: noche índigo que se apaga hacia un borde pizarra. Con el borde #1C2752 anterior el degradado pasaba
        // a ΔE00 2,6 del Huito hacia la mitad del radio; el borde casi neutro lo mantiene a ≥ 5,7.
        private static readonly Color JaguarDiscCenter = Palette.Hex(0x404E8E);
        private static readonly Color JaguarDiscEdge = Palette.Hex(0x1E2228);
        private static readonly Color JaguarDiscGlow = Palette.Hex(0x5468B0);
        private static readonly Color JaguarRing = Palette.Hex(0x161D3A);
        private static readonly Color JaguarRingLight = Palette.Hex(0x5A6CB4);

        private static readonly Color JaguarGoldLight = ColorMath.Lighten(JaguarGold, 0.35f);
        private static readonly Color JaguarShade = Palette.Hex(0xB46E1E);

        // Hocico, garganta y oreja: crema rosada a ΔE00 7,5 del Copoazú (el #F6E3BC anterior estaba a 4,5).
        private static readonly Color JaguarCream = Palette.Hex(0xF8E4C8);
        private static readonly Color JaguarInk = Palette.Hex(0x1E140C);
        private static readonly Color JaguarEye = Palette.Hex(0xF2C14E);
        private static readonly Color JaguarNose = Palette.Hex(0x8A4B3A);
        private static readonly Color MedallionShadow = new Color(0f, 0f, 0f, 0.34f);

        // Muestras del degradado de cada disco que miden LabelSealColors y LabelSealContrastPairs (cada 5 % del degradado).
        private const int MedallionDiscSamples = 20;

        private readonly Dictionary<int, Sprite> _headerBands = new Dictionary<int, Sprite>();
        private Sprite? _ranaDardo;
        private Sprite? _jaguarHead;
        private Sprite? _ribbonTag;
        private Sprite? _pillHard;
        private Sprite? _pillVeryHard;
        private ReliefSprites? _ctaNightLarge;

        /// <summary>
        /// Sello de Difícil (32 dp): rana dardo naranja con manchas redondas negras y contorno negro, vista desde arriba y con
        /// las patas recogidas junto al cuerpo (la silueta compacta y naranja se lee como rana a 30 dp; con patas negras abiertas
        /// en X se leía como un escarabajo o una calavera con tibias), sobre un disco marfil rosado con aro ámbar tostado
        /// (07 §7.7; glosario §6.1). Lo reutilizan el HUD (T09) y la tarjeta previa (T13). Pendiente de revisión del asesor
        /// cultural (07 §8.2).
        /// </summary>
        public Sprite RanaDardo => _ranaDardo != null ? _ranaDardo : (_ranaDardo = BuildRanaDardo());

        /// <summary>
        /// Sello de Muy difícil (32 dp): cabeza de jaguar de perfil con rosetas, solo el animal (07 §7.7), sobre un disco de
        /// noche índigo que se apaga hacia un borde pizarra, con dos estrellas y aro índigo oscuro con un filete marfil. Rasgos
        /// del jaguar y no del guepardo ni del leopardo (fauna real, 07 §8.1): cráneo redondo, hocico corto y ancho, mandíbula pesada,
        /// rosetas con un punto de tinta dentro y sin la lágrima del ojo al hocico; boca neutra, sin sonrisa.
        /// Pendiente de revisión del asesor cultural (07 §8.2).
        /// </summary>
        public Sprite JaguarHead => _jaguarHead != null ? _jaguarHead : (_jaguarHead = BuildJaguarHead());

        /// <summary>
        /// Todos los colores con los que se pintan los sellos <see cref="RanaDardo"/> y <see cref="JaguarHead"/>, con el nombre de
        /// su campo, para el test de ΔE00 frente a los 14 tintes (07 §3, regla 6; direccion-visual-v2 §2.4, regla 1): el
        /// degradado de cada disco de <c>DrawMedallionBase</c> muestreado cada 5 % (<c>RanaDisc@0.35</c>…), el brillo del disco,
        /// el aro con su luz, su filete y su sombra, la rana, el jaguar y los brillos crema y marfil de los ojos y las estrellas.
        /// Las dos excepciones de §2.4 que el test documenta son <c>JaguarEye</c> (el iris visible, alrededor de la pupila, mide
        /// ≤ 1,4 dp de grosor) y <c>JaguarShade</c> (solo se pinta en líneas de ≤ 2 dp, el borde de la oreja y el centro de las
        /// rosetas, alrededor de su punto de tinta, y en la sombra de la nuca con alfa 0,55). Solo para tests: crea el array en
        /// cada llamada.
        /// </summary>
        public static (string Name, Color Color)[] LabelSealColors()
        {
            var colors = new List<(string Name, Color Color)>
            {
                ("RanaDiscGlow", RanaDiscGlow),
                ("RanaRing", RanaRing),
                ("RanaRingLight", RanaRingLight),
                ("RanaRingInner", RanaRingInner),
                ("FrogOrange", FrogOrange),
                ("FrogOrangeLight", FrogOrangeLight),
                ("FrogBlack", FrogBlack),
                ("Cream", Palette.Cream),
                ("JaguarDiscGlow", JaguarDiscGlow),
                ("JaguarRing", JaguarRing),
                ("JaguarRingLight", JaguarRingLight),
                ("UiPaper", Palette.UiPaper),
                ("JaguarGold", JaguarGold),
                ("JaguarGoldLight", JaguarGoldLight),
                ("JaguarShade", JaguarShade),
                ("JaguarCream", JaguarCream),
                ("JaguarInk", JaguarInk),
                ("JaguarEye", JaguarEye),
                ("JaguarNose", JaguarNose),
                ("MedallionShadow", ColorMath.WithAlpha(MedallionShadow, 1f)),
            };
            for (int i = 0; i <= MedallionDiscSamples; i++)
            {
                float t = i / (float)MedallionDiscSamples;
                string at = t.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                colors.Add(("RanaDisc@" + at, MedallionDisc(RanaDiscCenter, RanaDiscEdge, t)));
                colors.Add(("JaguarDisc@" + at, MedallionDisc(JaguarDiscCenter, JaguarDiscEdge, t)));
            }

            return colors.ToArray();
        }

        /// <summary>
        /// Glifos de los sellos sobre su disco, para el test de contraste ≥ 3:1 de iconos y glifos (direccion-visual-v2 §2.4,
        /// regla 2): el naranja y el negro de la rana sobre todo el degradado del disco marfil y el oro del jaguar sobre todo el
        /// de la noche, muestreados cada 5 %. Solo para tests: crea el array en cada llamada.
        /// </summary>
        public static (string Name, Color Glyph, Color Background)[] LabelSealContrastPairs()
        {
            var pairs = new List<(string Name, Color Glyph, Color Background)>();
            for (int i = 0; i <= MedallionDiscSamples; i++)
            {
                float t = i / (float)MedallionDiscSamples;
                string at = t.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                Color rana = MedallionDisc(RanaDiscCenter, RanaDiscEdge, t);
                pairs.Add(("FrogOrange sobre RanaDisc@" + at, FrogOrange, rana));
                pairs.Add(("FrogBlack sobre RanaDisc@" + at, FrogBlack, rana));
                pairs.Add(("JaguarGold sobre JaguarDisc@" + at, JaguarGold, MedallionDisc(JaguarDiscCenter, JaguarDiscEdge, t)));
            }

            return pairs.ToArray();
        }

        /// <summary>
        /// Cinta «×3»/«×4» (propuesta §c.2: como la «x5» de Magic Sort, ficha §3.6): agua profunda con luz arriba y bisel, cola
        /// en V a la derecha y, en la esquina superior izquierda, un remache macizo de <c>madera_oscura</c> que la clava sobre el
        /// extremo de la píldora. El remache queda arriba, por encima de la altura de la «x» del texto: la anilla hueca que
        /// había, a media altura entre la palabra y la «×», se leía como una «o» («Difícil o×3»). El texto lo pone TextMeshPro
        /// en <see cref="RibbonTagTextDp"/>. <see cref="RibbonTagWidthDp"/> × <see cref="RibbonTagHeightDp"/> dp.
        /// </summary>
        public Sprite RibbonTag => _ribbonTag != null ? _ribbonTag : (_ribbonTag = BuildRibbonTag());

        /// <summary>
        /// Variante nocturna [P] del CTA grande de «Jugar · Nivel {n}» (<see cref="CtaLeafLarge"/>, mismas medidas y 9-slice):
        /// cara índigo con luz arriba, canto y contorno de noche.
        /// </summary>
        public ReliefSprites CtaNightLarge
        {
            get
            {
                if (_ctaNightLarge == null || _ctaNightLarge.Face == null)
                {
                    const float w = 72f;
                    const float face = 64f;
                    const float radius = 24f;
                    Sprite faceSprite = BuildRelief("cta_night_large_face", w, face, radius, false, FamilyNight, 0, true);
                    Sprite baseSprite = BuildRelief("cta_night_large_base", w, face, radius, false, FamilyNight, 1, true);
                    Sprite disabled = BuildRelief("cta_night_large_disabled", w, face, radius, false, FamilyNight, 2, true);
                    _ctaNightLarge = new ReliefSprites(faceSprite, baseSprite, disabled, w, face + 14f, face, true, false);
                }

                return _ctaNightLarge;
            }
        }

        /// <summary>Sello de la etiqueta: <see cref="JaguarHead"/> en Muy difícil y <see cref="RanaDardo"/> en Difícil.</summary>
        public Sprite DifficultyIcon(bool veryHard)
        {
            return veryHard ? JaguarHead : RanaDardo;
        }

        /// <summary>
        /// Píldora del chip de dificultad (07 §7.7): <c>ui_ambar</c> o <c>ui_indigo</c> con luz arriba, canto inferior y
        /// contorno más oscuro; el centro, donde va el texto, conserva el color del token. 9-slice horizontal (borde de 14 dp),
        /// de <see cref="LabelPillDp"/> de alto más <see cref="LabelPillShadowDp"/> de sombra debajo.
        /// </summary>
        public Sprite DifficultyPill(bool veryHard)
        {
            if (veryHard)
            {
                return _pillVeryHard != null ? _pillVeryHard : (_pillVeryHard = BuildPill("label_pill_very_hard", Palette.UiIndigo));
            }

            return _pillHard != null ? _pillHard : (_pillHard = BuildPill("label_pill_hard", Palette.UiAmber));
        }

        /// <summary>
        /// Banda de color de la cabecera de un modal (296 × 40 dp, como <see cref="HeaderPlate"/>; propuesta §c.1: «cabecera
        /// ámbar» o «índigo»): el color <paramref name="band"/> con luz arriba, bisel, ranura tallada y contorno oscuro.
        /// </summary>
        public Sprite HeaderBand(Color band)
        {
            Color32 c = band;
            int key = (c.r << 16) | (c.g << 8) | c.b;
            if (!_headerBands.TryGetValue(key, out Sprite? sprite) || sprite == null)
            {
                sprite = BuildHeaderBand("header_band_" + key.ToString("X6", System.Globalization.CultureInfo.InvariantCulture), band);
                _headerBands[key] = sprite;
            }

            return sprite;
        }

        private Sprite BuildRanaDardo()
        {
            int s = Mathf.RoundToInt(LabelIconDp * UiPxPerDp);
            var pc = new PixelCanvas(s, s);
            float k = s / 96f;
            DrawMedallionBase(pc, k, RanaDiscCenter, RanaDiscEdge, RanaDiscGlow);

            // Rana vista desde arriba, cabeza hacia arriba, escalada 0,78 alrededor del centro del disco. Las patas, naranjas con
            // contorno negro y recogidas: los dedos quedan a menos de 30 unidades del centro (48, 47) del lienzo, las traseras
            // dobladas junto al cuerpo y las delanteras hacia delante y abajo, sin levantar los brazos.
            const float f = 0.78f;
            for (int pass = 0; pass < 2; pass++)
            {
                // Pasada 0: silueta engordada en negro (contorno); pasada 1: relleno naranja.
                float grow = pass == 0 ? 1.8f : 0f;
                float limbGrow = pass == 0 ? 1.5f : 0f;
                Color limb = pass == 0 ? FrogBlack : FrogOrange;
                for (int side = 0; side < 2; side++)
                {
                    float m = side == 0 ? 1f : -1f;
                    FrogCapsule(pc, k, f, m, 40f, 36f, 25f, 42f, 3.4f + limbGrow, limb); // muslo
                    FrogCapsule(pc, k, f, m, 25f, 42f, 24f, 26f, 2.8f + limbGrow, limb); // pierna
                    FrogCapsule(pc, k, f, m, 24f, 26f, 22f, 25f, 2.0f + limbGrow, limb); // pie
                    FrogCircle(pc, k, f, m, 19.5f, 27.5f, 2.1f + limbGrow, limb);
                    FrogCircle(pc, k, f, m, 19.8f, 23.2f, 2.1f + limbGrow, limb);
                    FrogCircle(pc, k, f, m, 23f, 21.5f, 2.0f + limbGrow, limb);
                    FrogCapsule(pc, k, f, m, 40f, 56f, 28f, 60f, 2.8f + limbGrow, limb); // brazo
                    FrogCapsule(pc, k, f, m, 28f, 60f, 30f, 66f, 2.4f + limbGrow, limb); // antebrazo, hacia delante
                    FrogCircle(pc, k, f, m, 26f, 68f, 2.0f + limbGrow, limb);
                    FrogCircle(pc, k, f, m, 28f, 70.8f, 2.0f + limbGrow, limb);
                    FrogCircle(pc, k, f, m, 31.2f, 70.2f, 1.9f + limbGrow, limb);
                }

                Color body = pass == 0 ? FrogBlack : FrogOrange;
                pc.FillEllipseSoft(FrogX(48f, 1f, f) * k, FrogY(45f, f) * k, (14.5f + grow) * f * k, (17.5f + grow) * f * k, 0.8f * k, body);
                pc.FillEllipseSoft(FrogX(48f, 1f, f) * k, FrogY(59.5f, f) * k, (13.5f + grow) * f * k, (10.5f + grow) * f * k, 0.8f * k, body);
                FrogCircle(pc, k, f, 1f, 38.5f, 64f, 5.4f + grow, FrogBlack); // ojos saltones, encima de la cabeza
                FrogCircle(pc, k, f, -1f, 38.5f, 64f, 5.4f + grow, FrogBlack);
            }

            // Brillo del lomo, manchas negras redondas de advertencia (ningún trazo: abajo se leía como una boca) y ojos con brillo.
            pc.FillEllipseSoft(FrogX(44f, 1f, f) * k, FrogY(55f, f) * k, 6f * f * k, 3.2f * f * k, 2.2f * k, ColorMath.WithAlpha(FrogOrangeLight, 0.85f));
            FrogCircle(pc, k, f, 1f, 43f, 46f, 3.7f, FrogBlack);
            FrogCircle(pc, k, f, -1f, 44.5f, 40f, 3.2f, FrogBlack);
            FrogCircle(pc, k, f, 1f, 49.5f, 52.5f, 2.2f, FrogBlack);
            FrogCircle(pc, k, f, 1f, 47f, 33f, 2.6f, FrogBlack);
            FrogCircle(pc, k, f, 1f, 37.2f, 65.8f, 1.6f, Palette.Cream);
            FrogCircle(pc, k, f, 1f, 56.2f, 65.8f, 1.6f, Palette.Cream);

            DrawMedallionRing(pc, k, RanaRing, RanaRingLight, ColorMath.WithAlpha(RanaRingInner, 0.55f));
            return MakeSprite(pc, "label_rana_dardo", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private Sprite BuildJaguarHead()
        {
            int s = Mathf.RoundToInt(LabelIconDp * UiPxPerDp);
            var pc = new PixelCanvas(s, s);
            float k = s / 96f;
            DrawMedallionBase(pc, k, JaguarDiscCenter, JaguarDiscEdge, JaguarDiscGlow);

            // Dos estrellas de noche arriba a la derecha, lejos de la cabeza.
            DrawTinyStar(pc, 70f * k, 72f * k, 3.4f * k, ColorMath.WithAlpha(Palette.UiPaper, 0.9f));
            DrawTinyStar(pc, 80f * k, 62f * k, 2.4f * k, ColorMath.WithAlpha(Palette.UiPaper, 0.7f));

            // Silueta engordada en tinta (contorno) y después el pelaje: cuello, cráneo, hocico, mandíbula y oreja. El jaguar es
            // macizo: cráneo redondo y grande, hocico corto y ancho y mandíbula pesada (con el hocico largo y estrecho se leía
            // como un guepardo o un leopardo, revisión de las capturas 41 y 43).
            for (int pass = 0; pass < 2; pass++)
            {
                float grow = pass == 0 ? 2f : 0f;
                Color fur = pass == 0 ? JaguarInk : JaguarGold;
                pc.FillCapsule(38f * k, 40f * k, 28f * k, 4f * k, (15f + grow) * k, fur);
                pc.FillCircle(44f * k, 54f * k, (22.5f + grow) * k, fur);
                pc.FillEllipseSoft(63f * k, 45f * k, (13f + grow) * k, (12.5f + grow) * k, 0.8f * k, fur);
                pc.FillEllipseSoft(58f * k, 35.5f * k, (13f + grow) * k, (9.5f + grow) * k, 0.8f * k, fur);
                pc.FillCircle(32f * k, 72f * k, (8f + grow) * k, pass == 0 ? JaguarInk : JaguarShade);
            }

            // Oreja: borde oscuro y dentro claro; sombra de la nuca; luz en la frente.
            pc.FillCircle(32f * k, 72f * k, 6.2f * k, JaguarGold);
            pc.FillEllipseSoft(33.2f * k, 70.8f * k, 3.2f * k, 4f * k, 1f * k, JaguarCream);
            pc.FillEllipseSoft(31f * k, 46f * k, 11f * k, 18f * k, 4f * k, ColorMath.WithAlpha(JaguarShade, 0.55f));
            pc.FillEllipseSoft(48f * k, 66f * k, 10f * k, 5f * k, 3f * k, ColorMath.WithAlpha(JaguarGoldLight, 0.6f));

            // Hocico y garganta claros.
            pc.FillEllipseSoft(67f * k, 42.5f * k, 8f * k, 7f * k, 1.6f * k, JaguarCream);
            pc.FillEllipseSoft(58.5f * k, 31.5f * k, 10f * k, 5f * k, 1.6f * k, JaguarCream);

            // Rosetas: anillos rotos de tinta con el centro más oscuro que el pelaje y un punto de tinta dentro (la marca del
            // jaguar; las del leopardo no lo tienen), y motas sueltas en la frente y la mejilla.
            DrawRosette(pc, 40f * k, 52f * k, 4.4f * k, k);
            DrawRosette(pc, 28.5f * k, 40f * k, 4.2f * k, k);
            DrawRosette(pc, 44f * k, 66.5f * k, 3.8f * k, k);
            DrawRosette(pc, 30f * k, 22f * k, 4.6f * k, k);
            DrawRosette(pc, 42f * k, 29f * k, 3.8f * k, k);
            DrawRosette(pc, 24f * k, 58f * k, 3.4f * k, k);
            pc.FillCircle(53f * k, 66f * k, 1.5f * k, JaguarInk);
            pc.FillCircle(57.5f * k, 69f * k, 1.2f * k, JaguarInk);
            pc.FillCircle(50f * k, 46f * k, 1.4f * k, JaguarInk);
            pc.FillCircle(54.5f * k, 49f * k, 1.1f * k, JaguarInk);
            pc.FillCircle(51f * k, 40.5f * k, 1.2f * k, JaguarInk);

            // Ojo almendrado ámbar con pupila y la ceja. Sin la lágrima oscura del ojo al hocico: es el rasgo del guepardo, que
            // es africano; el jaguar no la tiene (fauna real, 07 §8.1).
            pc.FillEllipseSoft(60f * k, 56f * k, 4.6f * k, 3.2f * k, 0.8f * k, JaguarInk);
            pc.FillEllipseSoft(60.4f * k, 56.1f * k, 3.3f * k, 2.1f * k, 0.6f * k, JaguarEye);
            pc.FillCircle(61.3f * k, 56f * k, 1.4f * k, JaguarInk);
            pc.FillCircle(60.3f * k, 56.9f * k, 0.7f * k, Palette.Cream);
            pc.FillCapsule(54.5f * k, 60f * k, 63.5f * k, 59.5f * k, 0.9f * k, ColorMath.WithAlpha(JaguarInk, 0.8f));

            // Nariz en la punta del hocico corto, boca neutra (un único trazo recto de 3 u bajo la nariz, sin la vuelta por la
            // mandíbula, que dibujaba una sonrisa ladeada y junto a «¿Seguro?» podía leerse como burla) y bigotes.
            pc.FillEllipseSoft(74.6f * k, 48.5f * k, 3.6f * k, 2.7f * k, 0.6f * k, JaguarInk);
            pc.FillEllipseSoft(74.4f * k, 48.9f * k, 2.6f * k, 1.8f * k, 0.6f * k, JaguarNose);
            pc.FillCapsule(74f * k, 45.8f * k, 74f * k, 42.8f * k, 0.9f * k, JaguarInk);
            pc.FillCircle(67f * k, 44f * k, 0.8f * k, JaguarInk);
            pc.FillCircle(70f * k, 45.2f * k, 0.8f * k, JaguarInk);
            pc.FillCircle(68f * k, 47.4f * k, 0.8f * k, JaguarInk);

            DrawMedallionRing(pc, k, JaguarRing, JaguarRingLight, ColorMath.WithAlpha(Palette.UiPaper, 0.75f));
            return MakeSprite(pc, "label_jaguar_head", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private Sprite BuildRibbonTag()
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(RibbonTagWidthDp * k);
            int h = Mathf.RoundToInt(RibbonTagHeightDp * k);
            var pc = new PixelCanvas(w, h);
            const float x0 = RibbonTagBodyLeftDp;
            const float x1 = RibbonTagWidthDp - 0.5f;
            const float y0 = 2.5f;
            const float y1 = 20f;
            const float notch = 4f;
            float midY = (y0 + y1) * 0.5f;
            Color body = RibbonTagColor;
            Color outline = Palette.FamilyWater.Outline;
            for (int y = 0; y < h; y++)
            {
                float yd = RibbonTagHeightDp - ((y + 0.5f) / k);
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;

                    // Sombra suave 1,5 dp por debajo.
                    float sh = TagDistance(xd, yd - 1.5f, x0, y0, x1, y1, notch, midY);
                    pc.Blend(x, y, new Color(0f, 0f, 0f, 0.32f), PixelCanvas.SoftCoverage(sh * k, 2.4f * k));

                    float d = TagDistance(xd, yd, x0, y0, x1, y1, notch, midY);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, outline, cov);
                    float inside = PixelCanvas.Coverage((d + 1.1f) * k);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    float v = Mathf.Clamp01((yd - y0) / (y1 - y0)); // 0 arriba, 1 abajo
                    Color c = v < 0.45f
                        ? Color.Lerp(RibbonTagLight, body, Smooth(0f, 0.45f, v))
                        : Color.Lerp(body, ColorMath.Darken(body, 0.18f), Smooth(0.6f, 1f, v));
                    if (-d < 2.1f && v < 0.4f)
                    {
                        c = Color.Lerp(c, Palette.FamilyWater.Light, 0.55f * (1f - ((-d - 1.1f) / 1f))); // bisel superior
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, inside);
                }
            }

            // Remache macizo de madera_oscura (3,5 dp) con un brillo crema de 1 dp, en la esquina superior izquierda: clava la
            // cinta sobre el extremo de la píldora y queda por encima de la altura de la «x», así que nada circular cae en la
            // línea del texto entre la palabra y la «×».
            const float rivetX = x0 + 2.6f;
            const float rivetY = y0 + 2.6f;
            pc.FillCircle(rivetX * k, (RibbonTagHeightDp - rivetY) * k, 1.75f * k, Palette.WoodDark);
            pc.FillCircle((rivetX - 0.45f) * k, (RibbonTagHeightDp - rivetY + 0.45f) * k, 0.5f * k, ColorMath.WithAlpha(Palette.Cream, 0.9f));
            return MakeSprite(pc, "label_ribbon_tag", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        // Distancia con signo (dp, negativa dentro) a la cinta: rectángulo redondeado [x0, x1] × [y0, y1] menos la cola en V de
        // profundidad notch en el extremo derecho.
        private static float TagDistance(float xd, float yd, float x0, float y0, float x1, float y1, float notch, float midY)
        {
            float d = PixelCanvas.RoundedRectDistance(xd, yd, x0, y0, x1, y1, 3f);
            float half = (y1 - y0) * 0.5f;
            float vx = x1 - (notch * (1f - (Mathf.Abs(yd - midY) / half))); // borde de la V a esa altura
            float notchD = vx - xd; // > 0 a la izquierda de la V (dentro)
            return Mathf.Max(d, -notchD * 0.9f);
        }

        private Sprite BuildPill(string name, Color color)
        {
            const float k = UiPxPerDp;
            const float wDp = 40f;
            const float pillH = LabelPillDp;
            const float hDp = LabelPillDp + LabelPillShadowDp;
            int w = Mathf.RoundToInt(wDp * k);
            int h = Mathf.RoundToInt(hDp * k);
            var pc = new PixelCanvas(w, h);
            float r = (pillH * 0.5f) - 0.5f;
            Color outline = ColorMath.Darken(color, 0.42f);
            for (int y = 0; y < h; y++)
            {
                float yd = hDp - ((y + 0.5f) / k);
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float sh = PixelCanvas.RoundedRectDistance(xd, yd - 1.6f, 0.8f, 0.5f, wDp - 0.8f, pillH - 0.5f, r);
                    pc.Blend(x, y, new Color(0f, 0f, 0f, 0.35f), PixelCanvas.SoftCoverage(sh * k, 2f * k));
                    float d = PixelCanvas.RoundedRectDistance(xd, yd, 0.5f, 0.5f, wDp - 0.5f, pillH - 0.5f, r);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, outline, cov);
                    float inside = PixelCanvas.Coverage((d + 1.4f) * k);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    // Luz en el 40 % de arriba, el color del token en el centro (texto) y un canto algo más oscuro abajo.
                    float v = yd / pillH;
                    Color c = color;
                    if (v < 0.42f)
                    {
                        c = Color.Lerp(ColorMath.Lighten(color, 0.24f), color, Smooth(0.04f, 0.42f, v));
                    }
                    else if (v > 0.8f)
                    {
                        c = Color.Lerp(color, ColorMath.Darken(color, 0.16f), Smooth(0.8f, 0.98f, v));
                    }

                    float fromEdge = -(d + 1.4f);
                    if (fromEdge < 1.2f && v < 0.5f)
                    {
                        c = Color.Lerp(c, Color.white, 0.35f * (1f - (fromEdge / 1.2f)));
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, inside);
                }
            }

            float border = 14f * k;
            return MakeSprite(pc, name, new Vector2(0.5f, 0.5f), new Vector4(border, 0f, border, 0f), k);
        }

        private Sprite BuildHeaderBand(string name, Color band)
        {
            const float k = UiPxPerDp;
            const float wDp = 296f;
            const float hDp = 40f;
            const float radius = 12f;
            int w = Mathf.RoundToInt(wDp * k);
            int h = Mathf.RoundToInt(hDp * k);
            var pc = new PixelCanvas(w, h);
            Color outline = ColorMath.Darken(band, 0.5f);
            Color groove = ColorMath.Darken(band, 0.3f);
            for (int y = 0; y < h; y++)
            {
                float yd = hDp - ((y + 0.5f) / k);
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float d = PixelCanvas.RoundedRectDistance(xd, yd, 0.5f, 0.5f, wDp - 0.5f, hDp - 0.5f, radius);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, outline, cov);
                    float inside = PixelCanvas.Coverage((d + 1.2f) * k);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    // Barniz: luz arriba, el color del token en la franja del título y algo más oscuro abajo; una veta muy
                    // suave para que se lea como lacado y no como papel.
                    float v = yd / hDp;
                    Color c = band;
                    if (v < 0.36f)
                    {
                        c = Color.Lerp(ColorMath.Lighten(band, 0.2f), band, Smooth(0.03f, 0.36f, v));
                    }
                    else if (v > 0.8f)
                    {
                        c = Color.Lerp(band, ColorMath.Darken(band, 0.14f), Smooth(0.8f, 1f, v));
                    }

                    float grain = ArtNoise.Value2(xd / 30f, yd * 0.5f, 617);
                    if (Mathf.Abs(Mathf.Sin((yd * 0.7f) + (grain * 4f))) < 0.1f)
                    {
                        c = ColorMath.Darken(c, 0.05f);
                    }

                    float fromEdge = -(d + 1.2f);
                    if (fromEdge < 2f && v < 0.5f)
                    {
                        c = Color.Lerp(c, Color.white, 0.45f * (1f - (fromEdge / 2f))); // bisel superior
                    }
                    else if (fromEdge < 1f)
                    {
                        c = Color.Lerp(c, groove, 0.5f);
                    }

                    // Ranura tallada a 4 dp del borde: 1,2 dp oscura y 1 dp clara debajo.
                    if (fromEdge > 3.2f && fromEdge < 4.4f)
                    {
                        c = Color.Lerp(c, groove, 0.55f);
                    }
                    else if (fromEdge >= 4.4f && fromEdge < 5.4f)
                    {
                        c = Color.Lerp(c, Color.white, 0.18f);
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, inside);
                }
            }

            return MakeSprite(pc, name, new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        // Disco del sello (k = px por unidad de un lienzo de 96): relleno con degradado radial de center a edge, más un brillo
        // glow arriba a la izquierda. La sombra la pone DrawMedallionRing por debajo de todo.
        private static void DrawMedallionBase(PixelCanvas pc, float k, Color center, Color edge, Color glow)
        {
            float c = 48f * k;
            float r = 42f * k;
            int x0 = Mathf.FloorToInt(c - r - 1f);
            int x1 = Mathf.CeilToInt(c + r + 1f);
            for (int y = x0; y <= x1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f) - c;
                    float dy = (y + 0.5f) - c;
                    float dist = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float cov = PixelCanvas.Coverage(dist - r);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float t = Mathf.Pow(Mathf.Clamp01(dist / r), 1.6f);
                    pc.Blend(x, y, MedallionDisc(center, edge, t), cov);
                }
            }

            pc.FillEllipseSoft(38f * k, 62f * k, 22f * k, 16f * k, 12f * k, ColorMath.WithAlpha(glow, 0.35f));
        }

        // Color opaco del degradado del disco en t (0 en el centro, 1 en el borde). Lo comparten DrawMedallionBase y las
        // muestras que miden los tests (LabelSealColors y LabelSealContrastPairs).
        private static Color MedallionDisc(Color center, Color edge, float t)
        {
            Color col = Color.Lerp(center, edge, t);
            col.a = 1f;
            return col;
        }

        // Aro del sello: recorta lo que asome fuera del disco (el cuello del jaguar), pinta el aro de 5 px con luz arriba y un
        // filete interior y pone por debajo de todo una sombra suave desplazada hacia abajo.
        private static void DrawMedallionRing(PixelCanvas pc, float k, Color ring, Color light, Color inner)
        {
            float c = 48f * k;
            float clip = 42.5f * k;
            for (int y = 0; y < pc.Height; y++)
            {
                for (int x = 0; x < pc.Width; x++)
                {
                    float dx = (x + 0.5f) - c;
                    float dy = (y + 0.5f) - c;
                    float dist = Mathf.Sqrt((dx * dx) + (dy * dy));
                    if (dist > clip - 1f)
                    {
                        pc.MaskAlpha(x, y, PixelCanvas.Coverage(dist - clip));
                    }
                }
            }

            pc.StrokeArc(c, c, 42.5f * k, 5.2f * k, 0f, 360f, ring);
            pc.StrokeArc(c, c, 44.2f * k, 1.4f * k, 35f, 145f, ColorMath.WithAlpha(light, 0.9f));
            pc.StrokeArc(c, c, 39.6f * k, 1.1f * k, 0f, 360f, inner);
            Color shadow = MedallionShadow;
            float sy = c - (2.4f * k);
            float sr = 44.6f * k;
            for (int y = 0; y < pc.Height; y++)
            {
                for (int x = 0; x < pc.Width; x++)
                {
                    float dx = (x + 0.5f) - c;
                    float dy = (y + 0.5f) - sy;
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy)) - sr;
                    BlendUnder(pc, x, y, shadow, PixelCanvas.SoftCoverage(d, 3f * k));
                }
            }
        }

        // Compone c por DEBAJO del píxel (el píxel encima, «over»), con cobertura coverage.
        private static void BlendUnder(PixelCanvas pc, int x, int y, Color c, float coverage)
        {
            float a = c.a * Mathf.Clamp01(coverage);
            if (a <= 0f)
            {
                return;
            }

            Color d = pc.Get(x, y);
            float outA = d.a + (a * (1f - d.a));
            if (outA <= 0f)
            {
                return;
            }

            float kk = a * (1f - d.a);
            pc.Set(x, y, new Color(
                ((d.r * d.a) + (c.r * kk)) / outA,
                ((d.g * d.a) + (c.g * kk)) / outA,
                ((d.b * d.a) + (c.b * kk)) / outA,
                outA));
        }

        private static void DrawTinyStar(PixelCanvas pc, float cx, float cy, float r, Color c)
        {
            pc.FillCapsule(cx - r, cy, cx + r, cy, r * 0.16f, c);
            pc.FillCapsule(cx, cy - r, cx, cy + r, r * 0.16f, c);
            pc.FillCircle(cx, cy, r * 0.32f, c);
        }

        // Roseta del jaguar: anillo de tinta roto en dos arcos con el centro más oscuro que el pelaje y, dentro, un punto macizo
        // de tinta de 1,2 u de radio: las rosetas con punto central son la marca del jaguar (las del leopardo no lo tienen).
        private static void DrawRosette(PixelCanvas pc, float cx, float cy, float r, float k)
        {
            pc.FillCircle(cx, cy, r * 0.62f, ColorMath.WithAlpha(JaguarShade, 0.9f));
            pc.FillCircle(cx, cy, 1.2f * k, JaguarInk);
            pc.StrokeArc(cx, cy, r, 1.7f * k, 15f, 150f, JaguarInk);
            pc.StrokeArc(cx, cy, r, 1.7f * k, 190f, 335f, JaguarInk);
        }

        // Coordenadas de la rana: x reflejada en el eje del cuerpo (m = −1, el lado derecho) y escalada f alrededor de (48, 47).
        private static float FrogX(float x, float m, float f)
        {
            float mx = m > 0f ? x : 96f - x;
            return 48f + ((mx - 48f) * f);
        }

        private static float FrogY(float y, float f)
        {
            return 47f + ((y - 46f) * f);
        }

        private static void FrogCapsule(PixelCanvas pc, float k, float f, float m, float ax, float ay, float bx, float by, float r, Color c)
        {
            pc.FillCapsule(FrogX(ax, m, f) * k, FrogY(ay, f) * k, FrogX(bx, m, f) * k, FrogY(by, f) * k, r * f * k, c);
        }

        private static void FrogCircle(PixelCanvas pc, float k, float f, float m, float x, float y, float r, Color c)
        {
            pc.FillCircle(FrogX(x, m, f) * k, FrogY(y, f) * k, r * f * k, c);
        }
    }
}
