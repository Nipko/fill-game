#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Luciérnaga · Revelar (tarea T14 del paso 1; 07 §7.6: «luciérnaga encendida sobre una cuia
    /// oscura», amarillo verdoso #D8E24A; 04 §2.5; D-003): el glifo de 32 dp de la ranura de la bandeja (y su versión
    /// deshabilitada), la luciérnaga que vuela del botón a la cuia (cuerpo y alas, en u) y el aro del pulso del modo de
    /// selección (9-slice blanco que se tiñe). Todo perezoso: un nivel sin Aguas turbias no crea más textura que el glifo.
    /// </summary>
    /// <remarks>
    /// <para><b>Color</b>: #D8E24A (<see cref="FireflyLight"/>, el de 07 §7.6) queda a ΔE00 5,7 de Arazá (#BBCC33), por debajo de
    /// los 10 que pide direccion-visual-v2 §2.4 (regla 1) a lo que toca o rodea una cuia, y la luciérnaga se posa en la boca
    /// de una. Por eso #D8E24A va solo en filos de ≤ 2 dp (el aro del farol), la excepción de la regla, y la luz encendida es
    /// la misma familia más clara: <see cref="FireflyLit"/> (#F2F95E, a ΔE00 ≥ 10,8 de los 14 tintes) con el centro en
    /// <see cref="FireflyCore"/> [P]. Cuerpo, alas, cuia oscura, la luz del farol en el fondo del glifo (hasta
    /// <see cref="IconGlowPeak"/>) y aro del pulso, también a ΔE00 ≥ 10 (lo comprueba <c>LuciernagaTests</c>).</para>
    /// <para><b>Glifo</b> (revisión de las capturas 65): la luciérnaga va delante del cuerpo de una cuia oscura de boca ancha
    /// («luciérnaga en el frasco»), no sobre la boca: así se lee a 24 dp y las insignias de las esquinas de arriba de la ranura
    /// no la tapan.</para>
    /// <para>Pendiente de revisión del asesor cultural (nombra fauna) y del artista 2D, que lo sustituye.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Lado del glifo de la ranura (dp; 07 §7.6: 32 dp en la ranura de 64).</summary>
        public const float FireflyIconDp = 32f;

        /// <summary>Ancho del cuerpo de la luciérnaga en vuelo (u) [P].</summary>
        public const float FireflyBodyWidthU = 0.44f;

        /// <summary>Alto del cuerpo de la luciérnaga en vuelo, de la punta del farol a las antenas (u) [P].</summary>
        public const float FireflyBodyHeightU = 0.80f;

        /// <summary>Envergadura de las alas abiertas (u) [P].</summary>
        public const float FireflyWingsWidthU = 0.96f;

        /// <summary>Alto de las alas abiertas (u) [P].</summary>
        public const float FireflyWingsHeightU = 0.52f;

        /// <summary>Centro del farol bajo el centro del cuerpo (u): la luz va en la punta del abdomen.</summary>
        public const float FireflyLanternBelowU = 0.21f;

        /// <summary>Raíz de las alas sobre el centro del cuerpo (u): salen del tórax.</summary>
        public const float FireflyWingsAboveU = 0.12f;

        /// <summary>
        /// Alfa de la luz del farol en el fondo de la cuia del glifo [P]: con él, la mezcla más clara del brillo sobre la cuia
        /// oscura (<see cref="IconGlowPeak"/>) queda a ΔE00 ≥ 10 de los tintes (a 0,75 caería a 4 de Arazá).
        /// </summary>
        public const float IconGlowAlpha = 0.55f;

        /// <summary>Lado del sprite del aro del pulso (px; 9-slice).</summary>
        public const int RevealHaloPx = 144;

        /// <summary>Distancia del canto del sprite del aro a la línea del aro (px): lo que ocupa su brillo de fuera.</summary>
        public const int RevealHaloGlowPx = 20;

        /// <summary>Radio de la esquina de la línea del aro (px).</summary>
        public const int RevealHaloRadiusPx = 44;

        /// <summary>Borde del 9-slice del aro del pulso (px): el brillo de fuera y la esquina.</summary>
        public const int RevealHaloBorderPx = RevealHaloGlowPx + RevealHaloRadiusPx;

        /// <summary>Amarillo verdoso de 07 §7.6 (#D8E24A): solo en filos de ≤ 2 dp (ver la clase).</summary>
        public static readonly Color FireflyLight = Palette.Hex(0xD8E24A);

        /// <summary>Luz encendida del farol (#F2F95E) [P]: la familia de #D8E24A, más clara, a ΔE00 ≥ 10 de los tintes.</summary>
        public static readonly Color FireflyLit = Palette.Hex(0xF2F95E);

        /// <summary>Centro del farol, el punto más caliente (#F6FB9E) [P].</summary>
        public static readonly Color FireflyCore = Palette.Hex(0xF6FB9E);

        /// <summary>Abdomen y cabeza de la luciérnaga (#1E1610) [P].</summary>
        public static readonly Color FireflyBody = Palette.Hex(0x1E1610);

        /// <summary>Tórax (pronoto) de la luciérnaga (#2E2018) [P].</summary>
        public static readonly Color FireflyThorax = Palette.Hex(0x2E2018);

        /// <summary>Alas translúcidas (#BFE3EA) [P].</summary>
        public static readonly Color FireflyWing = Palette.Hex(0xBFE3EA);

        /// <summary>La «cuia oscura» del glifo: <c>panel_bajo</c> (#142C36), con contorno crema.</summary>
        public static readonly Color FireflyIconCuia = Palette.Hex(0x142C36);

        /// <summary>Luz del aro del pulso y del halo de la luciérnaga (#EEF8A0) [P]: aditiva, a ΔE00 ≥ 10 de los tintes.</summary>
        public static readonly Color RevealHaloColor = Palette.Hex(0xEEF8A0);

        /// <summary>
        /// El punto más claro de la luz del farol en el fondo de la cuia del glifo: <see cref="FireflyLit"/> sobre
        /// <see cref="FireflyIconCuia"/> al <see cref="IconGlowAlpha"/>, mezclado como lo hace el lienzo (tests de color).
        /// </summary>
        public static Color IconGlowPeak => Color.Lerp(FireflyIconCuia, FireflyLit, IconGlowAlpha);

        // Glifo: la cuia de boca ancha (cuerpo, cuello y boca, px de 96, y hacia arriba) y la luciérnaga delante de ella, con el
        // tórax en (IconFireflyX, IconFireflyY), girada IconFireflyDeg (la cabeza hacia arriba a la derecha) y a escala
        // IconFireflyScale de su dibujo en unidades (farol en IconLanternY bajo el tórax).
        private const float IconJarOutlinePx = 5f;
        private const float IconFireflyX = 48f;
        private const float IconFireflyY = 34f;
        private const float IconFireflyDeg = -18f;
        private const float IconFireflyScale = 1.3f;
        private const float IconLanternY = -15.5f;

        private Sprite? _iconFirefly;
        private Sprite? _iconFireflyDisabled;
        private Sprite? _fireflyBody;
        private Sprite? _fireflyWings;
        private Sprite? _revealHalo;

        /// <summary>Glifo de Revelar · Luciérnaga (32 dp a 3 px/dp): la luciérnaga encendida sobre una cuia oscura (07 §7.6).</summary>
        public Sprite IconFirefly => _iconFirefly != null ? _iconFirefly : (_iconFirefly = BuildIconFirefly(false));

        /// <summary>Glifo deshabilitado (<c>deshabilitado_glifo</c>, plano y sin luz).</summary>
        public Sprite IconFireflyDisabled => _iconFireflyDisabled != null ? _iconFireflyDisabled : (_iconFireflyDisabled = BuildIconFirefly(true));

        /// <summary>
        /// Cuerpo de la luciérnaga en vuelo (<see cref="FireflyBodyWidthU"/> × <see cref="FireflyBodyHeightU"/> u, cabeza arriba):
        /// abdomen oscuro, tórax, cabeza y antenas, con el farol encendido en la punta del abdomen
        /// (<see cref="FireflyLanternBelowU"/> bajo el centro). Pivote en el centro.
        /// </summary>
        public Sprite FireflyBodySprite => _fireflyBody != null ? _fireflyBody : (_fireflyBody = BuildFireflyBody());

        /// <summary>Las dos alas abiertas (<see cref="FireflyWingsWidthU"/> × <see cref="FireflyWingsHeightU"/> u), translúcidas; pivote en su raíz.</summary>
        public Sprite FireflyWingsSprite => _fireflyWings != null ? _fireflyWings : (_fireflyWings = BuildFireflyWings());

        /// <summary>
        /// Aro del pulso del modo de selección (9-slice blanco de <see cref="RevealHaloPx"/> px, borde de
        /// <see cref="RevealHaloBorderPx"/>; se tiñe con <see cref="RevealHaloColor"/> y va aditivo): una línea clara que rodea
        /// la boca y los lados de la cuia, con un brillo suave por fuera y otro, más débil, por dentro, abierta por abajo: los
        /// lados se apagan en el borde de abajo del 9-slice.
        /// </summary>
        public Sprite RevealHalo => _revealHalo != null ? _revealHalo : (_revealHalo = BuildRevealHalo());

        // Glifo de 96 × 96 px (32 dp a 3 px/dp; y hacia arriba): la luciérnaga encendida delante de una cuia oscura de boca ancha
        // (07 §7.6, «sobre una cuia oscura»: el vidrio oscuro hace de fondo a su luz), con el farol en la punta del abdomen y su
        // luz que llena el fondo del vidrio. Lo que la identifica (el farol, el cuerpo y las alas) queda dentro del cuerpo de la
        // cuia, lejos de las esquinas de arriba de la ranura, donde van las insignias («+N» ámbar a la izquierda; «+1» con el
        // video a la derecha: revisión de la captura 65c, que tapaba la luciérnaga). Deshabilitado: la cuia hueca (solo el
        // contorno) y la luciérnaga en deshabilitado_glifo, con las alas a medio tono y el farol apagado (un aro).
        private Sprite BuildIconFirefly(bool disabled)
        {
            var pc = new PixelCanvas(96, 96);
            if (disabled)
            {
                StrokeIconJar(pc, Palette.DisabledGlyph);
                DrawIconFirefly(pc, true);
                return MakeSprite(pc, "icon_firefly_disabled", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
            }

            // Sombra interior de 1 dp del canto de la familia (guía 5.7), como los demás glifos de la bandeja.
            FillIconJar(pc, -3f, Palette.FamilyWater.Edge, Palette.FamilyWater.Edge);
            FillIconJar(pc, 0f, Palette.Cream, FireflyIconCuia);
            pc.FillCapsule(21f, 16f, 21f, 44f, 2f, ColorMath.WithAlpha(Palette.Cream, 0.5f)); // reflejo del vidrio

            // La luz del farol llena el fondo del vidrio, solo por dentro de la cuia.
            Vector2 lantern = IconFireflyPoint(0f, IconLanternY);
            float gx = lantern.x + 2f;
            float gy = lantern.y + 6f;
            const float rx = 28f;
            const float ry = 24f;
            const float soft = 22f;
            Color glow = ColorMath.WithAlpha(FireflyLit, IconGlowAlpha);
            for (int y = 0; y < pc.Height; y++)
            {
                for (int x = 0; x < pc.Width; x++)
                {
                    float px = ((x + 0.5f) - gx) / rx;
                    float py = ((y + 0.5f) - gy) / ry;
                    float d = (Mathf.Sqrt((px * px) + (py * py)) - 1f) * ry;
                    pc.Blend(x, y, glow, PixelCanvas.SoftCoverage(d, soft) * PixelCanvas.Coverage(IconJarDistance(x + 0.5f, y + 0.5f)));
                }
            }

            DrawIconFirefly(pc, false);
            return MakeSprite(pc, "icon_firefly", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        // La cuia del glifo (cuerpo de 24 × 19 dp, cuello y boca) desplazada oy px: el contorno de IconJarOutlinePx en edge y el
        // relleno en fill.
        private static void FillIconJar(PixelCanvas pc, float oy, Color edge, Color fill)
        {
            const float o = IconJarOutlinePx;
            pc.FillRoundedRect(12f - o, 4f + oy - o, 84f + o, 60f + oy + o, 22f + o, edge);
            pc.FillRoundedRect(36f - o, 56f + oy - o, 60f + o, 74f + oy + o, 5f + o, edge);
            pc.FillRoundedRect(30f - o, 72f + oy - o, 66f + o, 82f + oy + o, 4f + o, edge);
            pc.FillRoundedRect(12f, 4f + oy, 84f, 60f + oy, 22f, fill);
            pc.FillRoundedRect(36f, 56f + oy, 60f, 74f + oy, 5f, fill);
            pc.FillRoundedRect(30f, 72f + oy, 66f, 82f + oy, 4f, fill);
        }

        // Solo el contorno de la cuia del glifo (deshabilitado): lo que cubre la cuia con su contorno menos lo que cubre sin él.
        private static void StrokeIconJar(PixelCanvas pc, Color c)
        {
            for (int y = 0; y < pc.Height; y++)
            {
                for (int x = 0; x < pc.Width; x++)
                {
                    float d = IconJarDistance(x + 0.5f, y + 0.5f);
                    float a = PixelCanvas.Coverage(d - IconJarOutlinePx) * (1f - PixelCanvas.Coverage(d));
                    if (a > 0f)
                    {
                        pc.Blend(x, y, c, a);
                    }
                }
            }
        }

        // Distancia con signo (px, negativa dentro) a la cuia del glifo, sin contorno: la unión de cuerpo, cuello y boca.
        private static float IconJarDistance(float px, float py)
        {
            float body = PixelCanvas.RoundedRectDistance(px, py, 12f, 4f, 84f, 60f, 22f);
            float neck = PixelCanvas.RoundedRectDistance(px, py, 36f, 56f, 60f, 74f, 5f);
            float mouth = PixelCanvas.RoundedRectDistance(px, py, 30f, 72f, 66f, 82f, 4f);
            return Mathf.Min(body, Mathf.Min(neck, mouth));
        }

        // La luciérnaga del glifo: alas abiertas en V (detrás), abdomen oscuro con el farol encendido en la punta (filo de
        // #D8E24A de menos de 1 dp, luz de #F2F95E y centro caliente), tórax, cabeza y antenas. Deshabilitada, el farol es un aro.
        private static void DrawIconFirefly(PixelCanvas pc, bool disabled)
        {
            const float k = IconFireflyScale;
            Color body = disabled ? Palette.DisabledGlyph : FireflyBody;
            Color wing = disabled ? ColorMath.WithAlpha(Palette.DisabledGlyph, 0.5f) : ColorMath.WithAlpha(FireflyWing, 0.95f);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector2 w = IconFireflyPoint(s * 10.5f, 5.5f);
                FillEllipseRotated(pc, w.x, w.y, 12.5f * k, 5.6f * k, IconFireflyDeg + (s * 32f), wing);
            }

            Vector2 a = IconFireflyPoint(0f, -6f);
            Vector2 b = IconFireflyPoint(0f, -11f);
            pc.FillCapsule(a.x, a.y, b.x, b.y, 6.6f * k, body);
            Vector2 lantern = IconFireflyPoint(0f, IconLanternY);
            if (disabled)
            {
                pc.StrokeArc(lantern.x, lantern.y, 6.2f * k, 3.4f * k, 0f, 360f, body);
            }
            else
            {
                pc.FillCircle(lantern.x, lantern.y, 8.6f * k, FireflyLight);
                pc.FillCircle(lantern.x, lantern.y, 7f * k, FireflyLit);
                Vector2 core = IconFireflyPoint(-1.2f, IconLanternY + 0.9f);
                pc.FillCircle(core.x, core.y, 3.8f * k, FireflyCore);
            }

            Vector2 thorax = IconFireflyPoint(0f, 0f);
            FillEllipseRotated(pc, thorax.x, thorax.y, 7.2f * k, 5.4f * k, IconFireflyDeg, disabled ? body : FireflyThorax);
            Vector2 head = IconFireflyPoint(0f, 6.5f);
            pc.FillCircle(head.x, head.y, 3.6f * k, body);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector2 a0 = IconFireflyPoint(s * 1.4f, 8.5f);
                Vector2 a1 = IconFireflyPoint(s * 6f, 15.5f);
                pc.FillCapsule(a0.x, a0.y, a1.x, a1.y, 1.05f * k, body);
            }
        }

        // Punto (x, y) del dibujo de la luciérnaga del glifo (eje y hacia la cabeza, en unidades) en px del glifo.
        private static Vector2 IconFireflyPoint(float x, float y)
        {
            float rad = IconFireflyDeg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            float lx = x * IconFireflyScale;
            float ly = y * IconFireflyScale;
            return new Vector2(IconFireflyX + (lx * c) - (ly * s), IconFireflyY + (lx * s) + (ly * c));
        }

        // Cuerpo en vuelo a MechanicPxPerU (cabeza arriba, y hacia arriba): abdomen, farol en la punta, tórax, cabeza y antenas.
        private Sprite BuildFireflyBody()
        {
            int w = Mathf.RoundToInt(FireflyBodyWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(FireflyBodyHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            float lanternY = (h * 0.5f) - (FireflyLanternBelowU * MechanicPxPerU);

            // Abdomen oscuro con dos juntas apenas más claras (líneas de 1 px).
            pc.FillCapsule(cx, lanternY + 6f, cx, lanternY + 34f, 10.5f, FireflyBody);
            for (int j = 0; j < 2; j++)
            {
                float y = lanternY + 18f + (j * 9f);
                pc.FillCapsule(cx - 8f, y, cx + 8f, y, 0.6f, ColorMath.Lighten(FireflyBody, 0.18f));
            }

            // Farol: filo de #D8E24A, luz y centro.
            pc.FillCircle(cx, lanternY, 13f, FireflyLight);
            pc.FillCircle(cx, lanternY, 11f, FireflyLit);
            pc.FillCircle(cx - 1.5f, lanternY + 1.5f, 6.5f, FireflyCore);

            // Tórax ancho, cabeza y antenas.
            float thorax = lanternY + 42f;
            pc.FillRoundedRect(cx - 10f, thorax - 6f, cx + 10f, thorax + 6f, 6f, FireflyThorax);
            pc.FillCircle(cx, thorax + 10f, 5.5f, FireflyBody);
            pc.FillCapsule(cx - 2f, thorax + 14f, cx - 9f, h - 3f, 1.3f, FireflyBody);
            pc.FillCapsule(cx + 2f, thorax + 14f, cx + 9f, h - 3f, 1.3f, FireflyBody);
            return MakeSprite(pc, "firefly_body", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Alas abiertas a MechanicPxPerU: dos elipses inclinadas, translúcidas, con un filo claro.
        private Sprite BuildFireflyWings()
        {
            int w = Mathf.RoundToInt(FireflyWingsWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(FireflyWingsHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                float x = cx + (sign * 22f);
                float deg = side == 0 ? 160f : 20f;
                FillEllipseRotated(pc, x, cy + 4f, 24f, 11f, deg, ColorMath.WithAlpha(Palette.Cream, 0.55f));
                FillEllipseRotated(pc, x, cy + 4f, 22.5f, 9.5f, deg, ColorMath.WithAlpha(FireflyWing, 0.78f));
            }

            return MakeSprite(pc, "firefly_wings", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Aro del pulso, abierto por abajo: la línea de 5 px a RevealHaloGlowPx del canto, por arriba (esquinas redondeadas) y
        // por los lados, con un brillo de fuera (0,55) y otro de dentro (0,30) que se apagan antes del canto; sin línea abajo, y
        // los lados se apagan en el borde de abajo del 9-slice (RevealHaloBorderPx), así que el aro baja hasta el pie de la cuia
        // sin cruzar la repisa ni la insignia «?×n» (revisión de la captura 66). Blanco: el color lo pone la imagen.
        private Sprite BuildRevealHalo()
        {
            const int s = RevealHaloPx;
            const float g = RevealHaloGlowPx;
            const float b = RevealHaloBorderPx;
            var pc = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
            {
                float fy = Mathf.Clamp01((y + 0.5f) / b);
                float fade = fy * fy * (3f - (2f * fy));
                for (int x = 0; x < s; x++)
                {
                    // El rectángulo sigue hacia abajo sin cerrarse: abajo solo cuentan los lados.
                    float d = PixelCanvas.RoundedRectDistance(x + 0.5f, y + 0.5f, g, -4f * s, s - g, s - g, RevealHaloRadiusPx);
                    float line = PixelCanvas.Coverage(Mathf.Abs(d) - 2.5f);
                    float fall = 1f - Mathf.Clamp01(Mathf.Abs(d) / (g - 1f));
                    float glow = (d > 0f ? 0.55f : 0.30f) * fall * fall;
                    float a = Mathf.Max(line, glow) * fade;
                    if (a > 0f)
                    {
                        pc.Set(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
            }

            return MakeSprite(pc, "reveal_halo", new Vector2(0.5f, 0.5f), new Vector4(b, b, b, b));
        }

        // Elipse de semiejes rx × ry girada deg grados (antihorario) con antialias aproximado por distancia.
        private static void FillEllipseRotated(PixelCanvas pc, float cx, float cy, float rx, float ry, float deg, Color c)
        {
            float rad = deg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float r = Mathf.Max(rx, ry);
            float minR = Mathf.Min(rx, ry);
            int x0 = Mathf.FloorToInt(cx - r - 1f);
            int x1 = Mathf.CeilToInt(cx + r + 1f);
            int y0 = Mathf.FloorToInt(cy - r - 1f);
            int y1 = Mathf.CeilToInt(cy + r + 1f);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f) - cx;
                    float dy = (y + 0.5f) - cy;
                    float qx = (dx * cos) + (dy * sin);
                    float qy = (-dx * sin) + (dy * cos);
                    float k = Mathf.Sqrt(((qx / rx) * (qx / rx)) + ((qy / ry) * (qy / ry)));
                    pc.Blend(x, y, c, PixelCanvas.Coverage((k - 1f) * minR));
                }
            }
        }
    }
}
