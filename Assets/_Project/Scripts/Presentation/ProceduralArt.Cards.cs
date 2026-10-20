#nullable enable
using System.Collections.Generic;
using AF.Core.Mechanics;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la tarjeta previa de 1 toque y del tutorial de una pantalla de cada estreno (tarea T13 del paso 1;
    /// 02 §8, §9.1 y §10; propuesta-mecanicas-dificultad.md §c.1; 07 §7.6 y §7.7), en la línea de D-003 (madera, vidrio y luz
    /// de juego; nunca papel ni acuarela):
    /// <list type="bullet">
    /// <item><description>los iconos de 32 dp que faltaban para la fila de iconos de la tarjeta: <see cref="CuiaChicaIcon"/> y
    /// <see cref="CuiaAltaIcon"/> (02 §8), dos cuias de vidrio de pie sobre la misma base, la estándar de 4 en sombra como
    /// referencia y la chica (2 dosis) o la alta (5 dosis) delante, con su cavidad oscura y una marca por dosis;</description></item>
    /// <item><description>el icono grande del estreno (<see cref="CardHeroIcon"/>, <see cref="CardHeroIconDp"/>), dibujado a su
    /// tamaño para que no se vea borroso: el mismo dibujo que los iconos de 32 dp de <c>ProceduralArt.Mechanics</c>
    /// (<see cref="MechanicIcon"/> y <see cref="MudIcon"/>, con los dibujos comunes <c>DrawMechanicGlyph</c> y
    /// <c>DrawMudGlyph</c>) y las cuias de arriba;</description></item>
    /// <item><description>el medallón de madera tallada con el centro claro donde va ese icono (<see cref="CardMedallion"/>; con
    /// el centro de agua oscura, <see cref="CardMedallionWater"/>, para el Matapi, cuya fibra clara se pierde sobre la madera
    /// clara) y la luciérnaga de la píldora «+1 Luciérnaga» (<see cref="CardFirefly"/>; 07 §7.6: amarillo verdoso
    /// #D8E24A).</description></item>
    /// </list>
    /// Todo se genera de forma perezosa la primera vez que se pide (un nivel sin tarjeta no crea ninguna textura) y, tras
    /// <see cref="Dispose"/>, los sprites destruidos se vuelven a generar (el == de Unity). Sin tintes de fruta (07 §3, regla 6).
    /// La luciérnaga, el matapi, la piraña y el resto de la fauna y los objetos quedan pendientes de revisión del asesor cultural
    /// (07 §8.2). Lo sustituye el arte del artista.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Lado del icono grande del estreno dentro del medallón (dp).</summary>
        public const float CardHeroIconDp = 48f;

        /// <summary>
        /// Clave del icono grande de la Liana de cuenta (<c>intro_mechanic</c> <c>liana_n</c> del 243; VA1a) [P]: la liana con dos
        /// cuentas del gris perla del tablero y sin baya (<see cref="CardHeroIcon"/>).
        /// </summary>
        public const string LianaCountIconKey = "liana_n";

        /// <summary>Lado del medallón de la tarjeta de estreno (dp), con su sombra.</summary>
        public const float CardMedallionDp = 76f;

        /// <summary>Lado del glifo de la luciérnaga de «+1 Luciérnaga» (dp).</summary>
        public const float CardFireflyDp = 20f;

        /// <summary>Amarillo verdoso de la Luciérnaga #D8E24A (07 §7.6, «Luciérnaga (Revelar)»).</summary>
        public static readonly Color FireflyGlow = Palette.Hex(0xD8E24A);

        /// <summary>Vidrio de los iconos de cuia: el tinte del vidrio aclarado, para que se lea sobre la madera clara [P].</summary>
        private static readonly Color CardGlass = Color.Lerp(Palette.GlassTint, Color.white, 0.35f);

        /// <summary>Canto del vidrio de los iconos de cuia: verde azulado muy oscuro, de la familia del panel [P].</summary>
        private static readonly Color CardGlassEdge = Palette.Hex(0x1F3A40);

        private readonly Dictionary<string, Sprite> _cardHeroes = new Dictionary<string, Sprite>();
        private Sprite? _cuiaChicaIcon;
        private Sprite? _cuiaAltaIcon;
        private Sprite? _cardMedallion;
        private Sprite? _cardMedallionWater;
        private Sprite? _cardFirefly;

        /// <summary>
        /// Agua oscura del remanso (la del pozo del Remolino, <see cref="WhirlPoolColor"/>) [P]: va detrás de los iconos que en el
        /// tablero se ven sobre el panel oscuro y sobre la madera clara de la tarjeta se perderían (<see cref="CardIconOnWater"/>).
        /// Es una propiedad, no un campo: el orden de los inicializadores estáticos entre partes de una clase parcial no está
        /// definido.
        /// </summary>
        public static Color CardWater => WhirlPoolColor;

        /// <summary>
        /// El icono de <paramref name="type"/> va sobre agua oscura (<see cref="CardWater"/>) y no sobre la madera clara de la
        /// tarjeta [P]: hoy solo el Matapi, cuya fibra clara (#EED29A y #C49A5C) apenas contrasta con la madera clara (revisión de
        /// las capturas 62d y 61c). Así se ve como en el tablero, sobre el panel oscuro.
        /// </summary>
        public static bool CardIconOnWater(MechanicType type)
        {
            return type == MechanicType.Matapi;
        }

        /// <summary>Icono de la Cuia chica (32 dp; 02 §8): la cuia de 2 dosis junto a la estándar en sombra, más baja que ella.</summary>
        public Sprite CuiaChicaIcon => _cuiaChicaIcon != null ? _cuiaChicaIcon : (_cuiaChicaIcon = BuildCuiaSizeIcon(2, MechanicIconDp, "card_icon_cuia_chica"));

        /// <summary>Icono de la Cuia alta (32 dp; 02 §8): la cuia de 5 dosis junto a la estándar en sombra, más alta que ella.</summary>
        public Sprite CuiaAltaIcon => _cuiaAltaIcon != null ? _cuiaAltaIcon : (_cuiaAltaIcon = BuildCuiaSizeIcon(5, MechanicIconDp, "card_icon_cuia_alta"));

        /// <summary>
        /// Medallón de la tarjeta de estreno (<see cref="CardMedallionDp"/>): aro de <c>madera_oscura</c> tallada con bisel de luz
        /// arriba, centro claro de madera pulida con un brillo y sombra suave debajo.
        /// </summary>
        public Sprite CardMedallion => _cardMedallion != null ? _cardMedallion : (_cardMedallion = BuildCardMedallion(false));

        /// <summary>
        /// El mismo medallón con el centro de agua oscura (<see cref="CardWater"/>), para el icono grande de un estreno que se lee
        /// sobre agua (<see cref="CardIconOnWater"/>): el Matapi.
        /// </summary>
        public Sprite CardMedallionWater => _cardMedallionWater != null ? _cardMedallionWater : (_cardMedallionWater = BuildCardMedallion(true));

        /// <summary>Medallón del estreno <paramref name="introMechanic"/>: con el centro de agua oscura si su icono la pide.</summary>
        public Sprite CardMedallionFor(string? introMechanic)
        {
            return introMechanic != null && MechanicTypes.TryParse(introMechanic, out MechanicType type) && CardIconOnWater(type)
                ? CardMedallionWater
                : CardMedallion;
        }

        /// <summary>Luciérnaga de «+1 Luciérnaga» (<see cref="CardFireflyDp"/>): abdomen encendido con su halo y alas claras.</summary>
        public Sprite CardFirefly => _cardFirefly != null ? _cardFirefly : (_cardFirefly = BuildCardFirefly());

        /// <summary>
        /// Icono grande (<see cref="CardHeroIconDp"/>) del estreno <paramref name="introMechanic"/> (<c>intro_mechanic</c> de
        /// D-007: <c>chica</c>, <c>alta</c>, <c>hoja</c>, <c>matapi</c>, <c>liana</c>, <c>remolino</c>, <c>barro</c>,
        /// <c>lluvia</c>, <c>brote</c> o <c>pirana</c>; y las variantes de la 1.0, <c>hoja_atada</c> y <c>liana_n</c>,
        /// <see cref="LianaCountIconKey"/> (VA1a), y <c>brote_n2</c>, <see cref="BroteTwoLeavesIconKey"/>, y <c>flor</c> (VA1b)),
        /// o null si no es ninguno. <c>murky</c> no tiene: su estreno, el 100, es la
        /// tarjeta combinada, que lleva el chip «?» y no el medallón.
        /// </summary>
        public Sprite? CardHeroIcon(string? introMechanic)
        {
            if (string.IsNullOrEmpty(introMechanic))
            {
                return null;
            }

            string key = introMechanic!;
            if (_cardHeroes.TryGetValue(key, out Sprite? cached) && cached != null)
            {
                return cached;
            }

            Sprite? sprite = BuildCardHero(key);
            if (sprite != null)
            {
                _cardHeroes[key] = sprite;
            }

            return sprite;
        }

        private Sprite? BuildCardHero(string key)
        {
            string name = "card_hero_" + key;
            switch (key)
            {
                case "chica":
                    return BuildCuiaSizeIcon(2, CardHeroIconDp, name);
                case "alta":
                    return BuildCuiaSizeIcon(5, CardHeroIconDp, name);
                case "barro":
                    return BuildHeroPixels(name, DrawMudGlyph);
                case LianaCountIconKey:
                    return BuildHeroPixels(name, DrawLianaCountGlyph);
                case BroteTwoLeavesIconKey:
                    return BuildHeroPixels(name, DrawBroteTwoLeavesGlyph); // ProceduralArt.BroteDosHojas.cs (VA1b)
                case StreakHeroIconKey:
                    return BuildHeroPixels(name, DrawStreakGlyph); // la tarjeta de la Crecida del 25 (ProceduralArt.Streak.cs; B1b)
                default:
                    if (!MechanicTypes.TryParse(key, out MechanicType type) || type == MechanicType.None)
                    {
                        return null;
                    }

                    return BuildHeroPixels(name, (pc, s) => DrawMechanicGlyph(pc, type, s));
            }
        }

        // Liana de cuenta (estreno del 243, VA1a) en un lienzo cuadrado de s px (k = s / 96; y hacia arriba): la liana de la Liana,
        // con sus hojas y sin la baya (que se leía como la fruta de la Liana con fruta), y delante de ella dos cuentas: las
        // cuentas que dicen cuántas cuias hay que sellar (revisión de VA1a). Son las del tablero (LianaOverlay las ensarta con
        // CounterPip, un disco liso teñido de Pearl), para que la tarjeta y la cuia enseñen la misma pieza: el mismo disco liso
        // gris perla, sin brillo, más grande (≈ 14 % del círculo de 76 dp de la tarjeta) y con un canto pizarra (Slate) de ≈ 1 dp
        // que lo separa del fondo crema del medallón (en el tablero lo separa el vidrio oscuro).
        private static void DrawLianaCountGlyph(PixelCanvas pc, int s)
        {
            float k = s / 96f;
            var a = new Vector2(8f * k, 14f * k);
            var c = new Vector2(60f * k, 30f * k);
            var b = new Vector2(88f * k, 84f * k);
            DrawVine(pc, a, c, b, 6f * k, 0.8f * k, true);
            for (int i = 0; i < 2; i++)
            {
                Vector2 p = Quad(a, c, b, i == 0 ? 0.48f : 0.74f);
                pc.FillCircle(p.x, p.y, 11f * k, Slate);
                pc.FillCircle(p.x, p.y, 9f * k, Pearl);
            }
        }

        // El icono grande del estreno, con el mismo dibujo que el de 32 dp (DrawMechanicGlyph y DrawMudGlyph, en
        // ProceduralArt.Mechanics.cs) en un lienzo de CardHeroIconDp, para que no sea el pequeño estirado.
        private Sprite BuildHeroPixels(string name, System.Action<PixelCanvas, int> draw)
        {
            int s = Mathf.RoundToInt(CardHeroIconDp * UiPxPerDp);
            var pc = new PixelCanvas(s, s);
            draw(pc, s);
            return MakeSprite(pc, name, new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        // Dos cuias de pie sobre la misma base y con la misma u: a la izquierda la estándar de 4 en sombra (la referencia) y a la
        // derecha la de capacidad cap, así la chica se ve más baja y la alta más alta (02 §8: «misma silueta recortada por
        // arriba» y «silueta alargada»). La u sale de la más alta, que llena el 92 % del lado.
        private Sprite BuildCuiaSizeIcon(int capacity, float sizeDp, string name)
        {
            int s = Mathf.RoundToInt(sizeDp * UiPxPerDp);
            var pc = new PixelCanvas(s, s);
            float tallest = Mathf.Max(CuiaGeometry.HeightU(4), CuiaGeometry.HeightU(capacity));
            float u = (s * 0.92f) / tallest;
            float baseY = s * 0.04f;
            float half = s * 0.5f;
            DrawMiniCuia(pc, half - (1.25f * u), baseY, u, 4, true);
            DrawMiniCuia(pc, half + (1.25f * u), baseY, u, capacity, false);
            return MakeSprite(pc, name, new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        // Cuia de vidrio de capacidad cap con el pie en (cx, baseY) y u px por u (CuiaGeometry): canto oscuro, vidrio claro,
        // cavidad oscura (la del panel, D-003) con una marca por dosis, pie y borde de fibra y un reflejo en la pared izquierda. En
        // sombra (ghost), todo con poca opacidad y sin reflejo.
        private static void DrawMiniCuia(PixelCanvas pc, float cx, float baseY, float u, int cap, bool ghost)
        {
            float line = Mathf.Max(1.4f, 0.09f * u);
            float mark = Mathf.Max(1.1f, 0.06f * u);
            float height = CuiaGeometry.HeightU(cap);
            float lip = CuiaGeometry.LipU(cap);
            float top = CuiaGeometry.CavityTopU(cap);
            // En sombra: la silueta con su canto y un velo muy leve, para que se lea como la medida de referencia y no como una
            // cuia blanca (revisión de las capturas 62b y 62c).
            Color outline = ghost ? ColorMath.WithAlpha(CardGlassEdge, 0.50f) : CardGlassEdge;
            Color glass = ghost ? ColorMath.WithAlpha(CardGlassEdge, 0.07f) : CardGlass;
            Color cavity = ghost ? ColorMath.WithAlpha(CardGlassEdge, 0.12f) : Palette.Panel.Cavity;
            Color fiber = ghost ? ColorMath.WithAlpha(Palette.Fiber, 0.30f) : Palette.Fiber;
            Color fiberDark = ghost ? ColorMath.WithAlpha(ColorMath.Darken(Palette.Fiber, 0.32f), 0.30f) : ColorMath.Darken(Palette.Fiber, 0.32f);
            Color marks = ghost ? ColorMath.WithAlpha(CardGlassEdge, 0.22f) : ColorMath.WithAlpha(Palette.GlassRim, 0.75f);
            int x0 = Mathf.FloorToInt(cx - u - 2f);
            int x1 = Mathf.CeilToInt(cx + u + 2f);
            int y0 = Mathf.FloorToInt(baseY - 1f);
            int y1 = Mathf.CeilToInt(baseY + (height * u) + 1f);
            for (int y = y0; y <= y1; y++)
            {
                float yu = ((y + 0.5f) - baseY) / u;
                float outer = CuiaGeometry.OuterHalfWidth(yu, cap);
                if (outer <= 0f)
                {
                    continue;
                }

                float inner = CuiaGeometry.InnerHalfWidth(yu, cap);
                bool fiberBand = yu < CuiaGeometry.FootTopU || yu >= lip;
                for (int x = x0; x <= x1; x++)
                {
                    float xu = ((x + 0.5f) - cx) / u;
                    float dx = Mathf.Abs(xu);
                    float d = (dx - outer) * u; // px, negativa dentro
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    // El canto: bajo un relleno opaco basta pintarlo entero; en sombra, solo en el borde (el relleno es translúcido).
                    float inside = PixelCanvas.Coverage(d + line);
                    pc.Blend(x, y, outline, ghost ? Mathf.Clamp01(cov - inside) : cov);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    Color fill;
                    if (fiberBand)
                    {
                        // Fibra trenzada: franjas oblicuas claras y oscuras.
                        float chevron = Mathf.Repeat((xu * 2.2f) + (yu * 3.4f), 1f);
                        fill = chevron < 0.5f ? fiber : fiberDark;
                    }
                    else if (inner > 0f && dx < inner && yu >= CuiaGeometry.CavityFloorU(xu))
                    {
                        fill = cavity;
                    }
                    else
                    {
                        fill = glass;
                    }

                    pc.Blend(x, y, fill, inside);
                    if (!fiberBand && inner > 0f && dx < inner && yu > CuiaGeometry.CavityBottomU && yu < top)
                    {
                        // Una marca por frontera de dosis (cap − 1 líneas), de pared a pared de la cavidad.
                        float k = (yu - CuiaGeometry.CavityBottomU) + 0.5f;
                        float nearest = Mathf.Floor(k);
                        if (nearest >= 1f && nearest <= cap - 1)
                        {
                            float distPx = Mathf.Abs(yu - (CuiaGeometry.CavityBottomU + nearest)) * u;
                            pc.Blend(x, y, marks, PixelCanvas.Coverage(distPx - (mark * 0.5f)));
                        }
                    }

                    if (!ghost && !fiberBand && xu < -0.84f && xu > -0.95f && yu > CuiaGeometry.CavityBottomU + 0.3f && yu < top - 0.1f)
                    {
                        pc.Blend(x, y, new Color(1f, 1f, 1f, 0.55f), inside); // reflejo de la pared izquierda
                    }
                }
            }
        }

        // water: el centro es agua oscura (CardWater) en lugar de madera pulida, con un brillo más tenue.
        private Sprite BuildCardMedallion(bool water)
        {
            const float k = UiPxPerDp;
            int s = Mathf.RoundToInt(CardMedallionDp * k);
            var pc = new PixelCanvas(s, s);
            float c = s * 0.5f;
            float rOuter = ((CardMedallionDp * 0.5f) - 2.5f) * k; // 35,5 dp: deja sitio a la sombra
            float rInner = rOuter - (5f * k); // aro de 5 dp
            pc.FillEllipseSoft(c, c - (1.8f * k), rOuter, rOuter, 3f * k, new Color(0f, 0f, 0f, 0.30f));
            Color ringLight = Palette.FamilyWood.Deep;
            Color ringDark = Palette.FamilyWood.Edge;
            Color outline = Palette.FamilyWood.Outline;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) - c;
                    float dy = (y + 0.5f) - c; // hacia arriba
                    float dist = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float cov = PixelCanvas.Coverage(dist - rOuter);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    // Aro de madera oscura: canto exterior más oscuro, cuerpo con algo de luz en el centro del aro y bisel arriba.
                    float ringT = Mathf.Clamp01((dist - rInner) / (rOuter - rInner));
                    float bulge = Mathf.Sin(ringT * Mathf.PI);
                    Color ring = Color.Lerp(ringDark, ringLight, bulge);
                    float up = Mathf.Clamp01(dy / rOuter);
                    ring = Color.Lerp(ring, Palette.FrameBevel, 0.38f * up * bulge);
                    ring.a = 1f;
                    pc.Blend(x, y, outline, cov);
                    pc.Blend(x, y, ring, PixelCanvas.Coverage(dist - (rOuter - (0.8f * k))));

                    // Centro de madera pulida: crema en el centro hacia panel_claro_fondo y una sombra interior en el borde. Con agua:
                    // el agua oscura algo aclarada en el centro hacia la del remanso.
                    float faceCov = PixelCanvas.Coverage(dist - rInner);
                    if (faceCov <= 0f)
                    {
                        continue;
                    }

                    float t = Mathf.Clamp01(dist / rInner);
                    Color face = water
                        ? Color.Lerp(ColorMath.Lighten(CardWater, 0.12f), CardWater, Mathf.Pow(t, 1.4f))
                        : Color.Lerp(Palette.Cream, Palette.PanelLightLow, Mathf.Pow(t, 1.4f));
                    if (t > 0.84f)
                    {
                        face = ColorMath.Darken(face, 0.16f * Mathf.SmoothStep(0f, 1f, (t - 0.84f) / 0.16f));
                    }

                    face.a = 1f;
                    pc.Blend(x, y, face, faceCov);
                }
            }

            pc.FillEllipseSoft(c - (7f * k), c + (9f * k), 15f * k, 10f * k, 9f * k, new Color(1f, 1f, 1f, water ? 0.10f : 0.45f));
            return MakeSprite(pc, water ? "card_medallion_water" : "card_medallion", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private Sprite BuildCardFirefly()
        {
            int s = Mathf.RoundToInt(CardFireflyDp * UiPxPerDp);
            var pc = new PixelCanvas(s, s);
            float c = s * 0.5f;
            Color glow = FireflyGlow;
            pc.FillEllipseSoft(c, c * 0.86f, s * 0.40f, s * 0.40f, s * 0.24f, ColorMath.WithAlpha(glow, 0.50f)); // halo
            Color wing = ColorMath.WithAlpha(Palette.UiPaper, 0.88f);
            pc.FillEllipseSoft(c - (s * 0.17f), c + (s * 0.08f), s * 0.15f, s * 0.21f, 1.2f, wing);
            pc.FillEllipseSoft(c + (s * 0.17f), c + (s * 0.08f), s * 0.15f, s * 0.21f, 1.2f, wing);
            pc.FillEllipseSoft(c, c - (s * 0.12f), s * 0.135f, s * 0.195f, 1.2f, ColorMath.Darken(glow, 0.35f)); // abdomen: canto
            pc.FillEllipseSoft(c, c - (s * 0.12f), s * 0.105f, s * 0.165f, 1.2f, glow);
            pc.FillEllipseSoft(c, c - (s * 0.15f), s * 0.05f, s * 0.09f, 1.5f, ColorMath.Lighten(glow, 0.6f)); // luz
            pc.FillCircle(c, c + (s * 0.12f), s * 0.085f, Slate); // tórax
            pc.FillCircle(c, c + (s * 0.24f), s * 0.065f, Slate); // cabeza
            return MakeSprite(pc, "card_firefly", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }
    }
}
