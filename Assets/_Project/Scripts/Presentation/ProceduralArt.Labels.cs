#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de las etiquetas de dificultad de D-009 (propuesta-mecanicas-dificultad.md §c.1-c.2; 07 §7.7; 02 §10;
    /// glosario §6.1), en la línea de D-003 (luz de juego, madera y vidrio; nunca papel ni acuarela): el sello de la rana dardo
    /// naranja y negra (Difícil) y el del jaguar de perfil con rosetas sobre la noche (Muy difícil), sin calavera; la cinta
    /// «×3»/«×4» de agua profunda colgada de una anilla de fibra; la píldora ámbar o índigo del chip; la banda de color de las
    /// cabeceras de ¿Seguro? y Nivel fallido; y la variante nocturna del botón «Jugar · Nivel {n}». Todo se genera de forma
    /// perezosa la primera vez que se pide (un inicio sin etiqueta no crea ninguna textura) y, tras <see cref="Dispose"/>, los
    /// sprites destruidos se vuelven a generar (el == de Unity). Lo sustituye el arte del artista.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Lado de los sellos de etiqueta <see cref="RanaDardo"/> y <see cref="JaguarHead"/> (dp).</summary>
        public const float LabelIconDp = 32f;

        /// <summary>Alto de la píldora del chip de dificultad (dp; 07 §7.7: «pill»).</summary>
        public const float LabelPillDp = 26f;

        /// <summary>Sombra bajo la píldora, incluida en el sprite de <see cref="DifficultyPill"/> (dp).</summary>
        public const float LabelPillShadowDp = 2f;

        /// <summary>Ancho del sprite de la cinta <see cref="RibbonTag"/> (dp), con la anilla a la izquierda.</summary>
        public const float RibbonTagWidthDp = 38f;

        /// <summary>Alto del sprite de la cinta <see cref="RibbonTag"/> (dp), con su sombra.</summary>
        public const float RibbonTagHeightDp = 24f;

        /// <summary>
        /// Zona de texto de la cinta dentro de su sprite (dp desde la esquina superior izquierda: x, y, ancho y alto): el cuerpo
        /// de madera a la derecha del agujero de la anilla y antes de la cola en V.
        /// </summary>
        public static readonly Rect RibbonTagTextDp = new Rect(12f, 2.5f, 21f, 17.5f);

        /// <summary>Naranja de advertencia de la rana dardo #F07A12 [P] (07 §7.7: «naranja/negro»).</summary>
        public static readonly Color FrogOrange = Palette.Hex(0xF07A12);

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

        private static readonly Color FrogOrangeLight = Palette.Hex(0xFFA947);
        private static readonly Color JaguarShade = Palette.Hex(0xB46E1E);
        private static readonly Color JaguarCream = Palette.Hex(0xF6E3BC);
        private static readonly Color JaguarInk = Palette.Hex(0x1E140C);
        private static readonly Color JaguarEye = Palette.Hex(0xF2C14E);
        private static readonly Color JaguarNose = Palette.Hex(0x8A4B3A);

        private readonly Dictionary<int, Sprite> _headerBands = new Dictionary<int, Sprite>();
        private Sprite? _ranaDardo;
        private Sprite? _jaguarHead;
        private Sprite? _ribbonTag;
        private Sprite? _pillHard;
        private Sprite? _pillVeryHard;
        private ReliefSprites? _ctaNightLarge;

        /// <summary>
        /// Sello de Difícil (32 dp): rana dardo naranja con manchas y patas negras, vista desde arriba, sobre un disco marfil
        /// cálido con aro ámbar tostado (07 §7.7; glosario §6.1). Lo reutilizan el HUD (T09) y la tarjeta previa (T13).
        /// </summary>
        public Sprite RanaDardo => _ranaDardo != null ? _ranaDardo : (_ranaDardo = BuildRanaDardo());

        /// <summary>
        /// Sello de Muy difícil (32 dp): cabeza de jaguar de perfil con rosetas, solo el animal (07 §7.7), sobre un disco de
        /// noche índigo con dos estrellas y aro índigo oscuro con un filete marfil.
        /// </summary>
        public Sprite JaguarHead => _jaguarHead != null ? _jaguarHead : (_jaguarHead = BuildJaguarHead());

        /// <summary>
        /// Cinta «×3»/«×4» (propuesta §c.2: como la «x5» de Magic Sort, ficha §3.6, «colgada de una anilla»): agua profunda con
        /// luz arriba y bisel, cola en V a la derecha y una anilla de fibra a la izquierda; el texto lo pone TextMeshPro en
        /// <see cref="RibbonTagTextDp"/>. <see cref="RibbonTagWidthDp"/> × <see cref="RibbonTagHeightDp"/> dp.
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
            DrawMedallionBase(pc, k, Palette.Hex(0xFFF6DE), Palette.Hex(0xEFD8A6), ColorMath.Lighten(Palette.UiAmber, 0.55f));

            // Rana vista desde arriba, cabeza hacia arriba, escalada 0,78 alrededor del centro del disco.
            const float f = 0.78f;
            for (int pass = 0; pass < 2; pass++)
            {
                // Pasada 0: silueta engordada en negro (contorno); pasada 1: relleno.
                float grow = pass == 0 ? 1.8f : 0f;
                Color limb = FrogBlack;
                for (int side = 0; side < 2; side++)
                {
                    float m = side == 0 ? 1f : -1f;
                    FrogCapsule(pc, k, f, m, 40f, 36f, 25f, 42f, 3.4f + grow, limb); // muslo
                    FrogCapsule(pc, k, f, m, 25f, 42f, 24f, 26f, 2.8f + grow, limb); // pierna
                    FrogCapsule(pc, k, f, m, 24f, 26f, 15f, 21f, 2.0f + grow, limb); // pie
                    FrogCircle(pc, k, f, m, 12.5f, 23.5f, 2.1f + grow, limb);
                    FrogCircle(pc, k, f, m, 13f, 17.5f, 2.1f + grow, limb);
                    FrogCircle(pc, k, f, m, 18f, 16f, 2.0f + grow, limb);
                    FrogCapsule(pc, k, f, m, 40f, 56f, 28f, 60f, 2.8f + grow, limb); // brazo
                    FrogCapsule(pc, k, f, m, 28f, 60f, 24f, 71f, 2.4f + grow, limb); // antebrazo
                    FrogCircle(pc, k, f, m, 20.5f, 73f, 2.0f + grow, limb);
                    FrogCircle(pc, k, f, m, 24f, 76.5f, 2.0f + grow, limb);
                    FrogCircle(pc, k, f, m, 28.5f, 74.5f, 1.9f + grow, limb);
                }

                Color body = pass == 0 ? FrogBlack : FrogOrange;
                pc.FillEllipseSoft(FrogX(48f, 1f, f) * k, FrogY(45f, f) * k, (14.5f + grow) * f * k, (17.5f + grow) * f * k, 0.8f * k, body);
                pc.FillEllipseSoft(FrogX(48f, 1f, f) * k, FrogY(59.5f, f) * k, (13.5f + grow) * f * k, (10.5f + grow) * f * k, 0.8f * k, body);
                FrogCircle(pc, k, f, 1f, 38.5f, 64f, 5.4f + grow, FrogBlack); // ojos saltones, encima de la cabeza
                FrogCircle(pc, k, f, -1f, 38.5f, 64f, 5.4f + grow, FrogBlack);
            }

            // Brillo del lomo, manchas negras de advertencia, motas naranjas en los muslos y ojos con brillo.
            pc.FillEllipseSoft(FrogX(44f, 1f, f) * k, FrogY(55f, f) * k, 6f * f * k, 3.2f * f * k, 2.2f * k, ColorMath.WithAlpha(FrogOrangeLight, 0.85f));
            FrogCircle(pc, k, f, 1f, 43f, 46f, 3.7f, FrogBlack);
            FrogCircle(pc, k, f, -1f, 44.5f, 40f, 3.2f, FrogBlack);
            FrogCircle(pc, k, f, 1f, 49.5f, 52.5f, 2.2f, FrogBlack);
            FrogCapsule(pc, k, f, 1f, 43f, 33.5f, 51f, 31.5f, 2.2f, FrogBlack);
            FrogCircle(pc, k, f, 1f, 31f, 40f, 1.5f, FrogOrange);
            FrogCircle(pc, k, f, -1f, 31f, 40f, 1.5f, FrogOrange);
            FrogCircle(pc, k, f, 1f, 30f, 60.5f, 1.2f, FrogOrange);
            FrogCircle(pc, k, f, -1f, 30f, 60.5f, 1.2f, FrogOrange);
            FrogCircle(pc, k, f, 1f, 37.2f, 65.8f, 1.6f, Palette.Cream);
            FrogCircle(pc, k, f, 1f, 56.2f, 65.8f, 1.6f, Palette.Cream);

            DrawMedallionRing(pc, k, ColorMath.Darken(Palette.UiAmber, 0.28f), ColorMath.Lighten(Palette.UiAmber, 0.45f), ColorMath.WithAlpha(Palette.Hex(0x5A3A10), 0.55f));
            return MakeSprite(pc, "label_rana_dardo", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private Sprite BuildJaguarHead()
        {
            int s = Mathf.RoundToInt(LabelIconDp * UiPxPerDp);
            var pc = new PixelCanvas(s, s);
            float k = s / 96f;
            DrawMedallionBase(pc, k, Palette.Hex(0x3C4E92), Palette.Hex(0x1C2752), Palette.Hex(0x5468B0));

            // Dos estrellas de noche arriba a la derecha, lejos de la cabeza.
            DrawTinyStar(pc, 70f * k, 72f * k, 3.4f * k, ColorMath.WithAlpha(Palette.UiPaper, 0.9f));
            DrawTinyStar(pc, 80f * k, 62f * k, 2.4f * k, ColorMath.WithAlpha(Palette.UiPaper, 0.7f));

            // Silueta engordada en tinta (contorno) y después el pelaje: cuello, cráneo, hocico, mandíbula y oreja.
            for (int pass = 0; pass < 2; pass++)
            {
                float grow = pass == 0 ? 2f : 0f;
                Color fur = pass == 0 ? JaguarInk : JaguarGold;
                pc.FillCapsule(38f * k, 40f * k, 28f * k, 4f * k, (15f + grow) * k, fur);
                pc.FillCircle(44f * k, 54f * k, (21f + grow) * k, fur);
                pc.FillEllipseSoft(65f * k, 45f * k, (15f + grow) * k, (11.5f + grow) * k, 0.8f * k, fur);
                pc.FillEllipseSoft(58f * k, 35.5f * k, (13f + grow) * k, (8f + grow) * k, 0.8f * k, fur);
                pc.FillCircle(32f * k, 72f * k, (8f + grow) * k, pass == 0 ? JaguarInk : JaguarShade);
            }

            // Oreja: borde oscuro y dentro claro; sombra de la nuca; luz en la frente.
            pc.FillCircle(32f * k, 72f * k, 6.2f * k, JaguarGold);
            pc.FillEllipseSoft(33.2f * k, 70.8f * k, 3.2f * k, 4f * k, 1f * k, JaguarCream);
            pc.FillEllipseSoft(31f * k, 46f * k, 11f * k, 18f * k, 4f * k, ColorMath.WithAlpha(JaguarShade, 0.55f));
            pc.FillEllipseSoft(48f * k, 66f * k, 10f * k, 5f * k, 3f * k, ColorMath.WithAlpha(ColorMath.Lighten(JaguarGold, 0.35f), 0.6f));

            // Hocico y garganta claros.
            pc.FillEllipseSoft(69f * k, 42.5f * k, 9.5f * k, 7f * k, 1.6f * k, JaguarCream);
            pc.FillEllipseSoft(59f * k, 32f * k, 10f * k, 4.6f * k, 1.6f * k, JaguarCream);

            // Rosetas: anillos rotos de tinta con el centro más oscuro que el pelaje, y motas sueltas en la frente y la mejilla.
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

            // Ojo almendrado ámbar con pupila, la lágrima oscura hacia el hocico y la ceja.
            pc.FillEllipseSoft(60f * k, 56f * k, 4.6f * k, 3.2f * k, 0.8f * k, JaguarInk);
            pc.FillEllipseSoft(60.4f * k, 56.1f * k, 3.3f * k, 2.1f * k, 0.6f * k, JaguarEye);
            pc.FillCircle(61.3f * k, 56f * k, 1.4f * k, JaguarInk);
            pc.FillCircle(60.3f * k, 56.9f * k, 0.7f * k, Palette.Cream);
            pc.FillCapsule(63.5f * k, 53.5f * k, 68f * k, 49.5f * k, 0.9f * k, JaguarInk);
            pc.FillCapsule(54.5f * k, 60f * k, 63.5f * k, 59.5f * k, 0.9f * k, ColorMath.WithAlpha(JaguarInk, 0.8f));

            // Nariz, boca y bigotes.
            pc.FillEllipseSoft(78.2f * k, 48f * k, 3.6f * k, 2.7f * k, 0.6f * k, JaguarInk);
            pc.FillEllipseSoft(78f * k, 48.4f * k, 2.6f * k, 1.8f * k, 0.6f * k, JaguarNose);
            pc.FillCapsule(77.5f * k, 45.2f * k, 72.5f * k, 40.2f * k, 0.9f * k, JaguarInk);
            pc.FillCapsule(72.5f * k, 40.2f * k, 64f * k, 38.4f * k, 0.9f * k, JaguarInk);
            pc.FillCircle(70f * k, 44f * k, 0.8f * k, JaguarInk);
            pc.FillCircle(73f * k, 45f * k, 0.8f * k, JaguarInk);
            pc.FillCircle(71f * k, 47f * k, 0.8f * k, JaguarInk);

            DrawMedallionRing(pc, k, Palette.Hex(0x161D3A), Palette.Hex(0x5A6CB4), ColorMath.WithAlpha(Palette.UiPaper, 0.75f));
            return MakeSprite(pc, "label_jaguar_head", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private Sprite BuildRibbonTag()
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(RibbonTagWidthDp * k);
            int h = Mathf.RoundToInt(RibbonTagHeightDp * k);
            var pc = new PixelCanvas(w, h);
            const float x0 = 8f;
            const float x1 = 37.5f;
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

            // Agujero de la anilla y la anilla de fibra que pasa por él (cuelga del chip).
            pc.FillCircle(10.6f * k, (RibbonTagHeightDp - midY) * k, 1.5f * k, Palette.Groove);
            float ringY = (RibbonTagHeightDp - midY) * k;
            pc.StrokeArc(7.2f * k, ringY, 3.5f * k, 1.6f * k, 0f, 360f, Palette.FiberShadow);
            pc.StrokeArc(7.2f * k, ringY, 3.9f * k, 0.6f * k, 60f, 160f, ColorMath.WithAlpha(Palette.FiberLight, 0.9f));
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
                    Color col = Color.Lerp(center, edge, t);
                    col.a = 1f;
                    pc.Blend(x, y, col, cov);
                }
            }

            pc.FillEllipseSoft(38f * k, 62f * k, 22f * k, 16f * k, 12f * k, ColorMath.WithAlpha(glow, 0.35f));
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
            var shadow = new Color(0f, 0f, 0f, 0.34f);
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

        // Roseta del jaguar: anillo de tinta roto en dos arcos con el centro más oscuro que el pelaje.
        private static void DrawRosette(PixelCanvas pc, float cx, float cy, float r, float k)
        {
            pc.FillCircle(cx, cy, r * 0.62f, ColorMath.WithAlpha(JaguarShade, 0.9f));
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
