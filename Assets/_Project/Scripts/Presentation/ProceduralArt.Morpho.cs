#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de Morpho · Pista (tarea MO2; 07 §7.6: «mariposa azul con destello sobre dos cuias», azul morpho
    /// #2F6FD6 y destello en marfil; 04 §2.3, VFX; D-003): el glifo de 32 dp de la ranura de la bandeja (y su versión
    /// deshabilitada), la mariposa que vuela del botón a las cuias (alas y cuerpo, en u) y el color del anillo con que laten
    /// las dos cuias de la pista (sobre el aro de <see cref="RevealHalo"/>). Todo perezoso: el glifo lo pide la bandeja al
    /// crearse; las alas y el cuerpo, la primera pista con vertido (o <c>MorphoFx.Prewarm</c> al cargar un nivel con la ranura).
    /// </summary>
    /// <remarks>
    /// <para><b>Color</b> (direccion-visual-v2 §2.4, regla 1: lo que toca o rodea una cuia, a ΔE00 ≥ 10 de los 14 tintes):
    /// <see cref="MorphoBlue"/> (#2F6FD6, el de 07 §7.6) queda a ≥ 24; el filo oscuro de las alas (<see cref="MorphoDeep"/>), a
    /// ≥ 12,6 (del Huito); su reflejo (<see cref="MorphoLight"/>), a ≥ 15 (del Agua del Tapajós); el cuerpo
    /// (<see cref="MorphoBody"/>), a ≥ 17; y el anillo (<see cref="MorphoRingColor"/>), a ≥ 14 (de la Guanábana). El marfil del
    /// destello (<see cref="Palette.UiPaper"/>) queda a 7 de la Guanábana: solo va en puntos de ≤ 2 dp, la excepción de la
    /// regla (como el filo de la luciérnaga). La mariposa que vuela (revisión de MO2) lleva alas más claras,
    /// <see cref="MorphoFlightBlue"/> (a ≥ 23,6) con el reflejo <see cref="MorphoFlightLight"/> (a ≥ 10,6, del Agua del
    /// Tapajós), y un halo de 2 dp (<see cref="MorphoHalo"/>, a 8,9 del Copoazú: línea de ≤ 2 dp, la misma excepción). Lo
    /// comprueba <c>MorphoUiTests</c>.</para>
    /// <para>Pendiente de revisión del asesor cultural (nombra fauna) y del artista 2D, que lo sustituye.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Envergadura de las alas abiertas de la mariposa en vuelo (u) [P].</summary>
        public const float MorphoWingsWidthU = 0.92f;

        /// <summary>Alto de las alas abiertas (u) [P].</summary>
        public const float MorphoWingsHeightU = 0.74f;

        /// <summary>Ancho del cuerpo con las antenas (u) [P].</summary>
        public const float MorphoBodyWidthU = 0.22f;

        /// <summary>Alto del cuerpo, del abdomen a la punta de las antenas (u) [P].</summary>
        public const float MorphoBodyHeightU = 0.64f;

        /// <summary>Azul morpho de 07 §7.6 (#2F6FD6): el color de las alas.</summary>
        public static readonly Color MorphoBlue = Palette.Hex(0x2F6FD6);

        /// <summary>Filo oscuro de las alas (#1E4FA8) [P].</summary>
        public static readonly Color MorphoDeep = Palette.Hex(0x1E4FA8);

        /// <summary>Reflejo estructural de las alas (#6FA8F0) [P]: también la estela de destellos del salto.</summary>
        public static readonly Color MorphoLight = Palette.Hex(0x6FA8F0);

        /// <summary>Cuerpo y antenas (#26262E) [P].</summary>
        public static readonly Color MorphoBody = Palette.Hex(0x26262E);

        /// <summary>Anillo claro con que laten las dos cuias de la pista (#CFE4FF) [P]: aditivo, sobre <see cref="RevealHalo"/>.</summary>
        public static readonly Color MorphoRingColor = Palette.Hex(0xCFE4FF);

        /// <summary>
        /// Azul iridiscente de las alas de la mariposa que vuela (#2F80ED) [P] (revisión de MO2: más claro que el azul morpho del
        /// glifo, el de 07 §7.6, para que se siga sobre el panel azul petróleo; D-003, luz de juego).
        /// </summary>
        public static readonly Color MorphoFlightBlue = Palette.Hex(0x2F80ED);

        /// <summary>
        /// Reflejo de las alas en vuelo y color de la estela (#88D8FF) [P] (la revisión de MO2 pedía #7FD3FF, que queda a ΔE00 9,9
        /// del Agua del Tapajós; este, a 10,6).
        /// </summary>
        public static readonly Color MorphoFlightLight = Palette.Hex(0x88D8FF);

        /// <summary>
        /// Halo claro de 2 dp alrededor de la mariposa en vuelo (#FFF4D6, alfa <see cref="MorphoHaloAlpha"/>) [P] (revisión de
        /// MO2): queda a ΔE00 8,9 del Copoazú, pero es una línea de ≤ 2 dp, la excepción de la regla 1 (como el destello).
        /// </summary>
        public static readonly Color MorphoHalo = Palette.Hex(0xFFF4D6);

        /// <summary>Alfa del halo de la mariposa [P].</summary>
        public const float MorphoHaloAlpha = 0.85f;

        /// <summary>
        /// Margen de los sprites de la mariposa en vuelo a cada lado (px a <see cref="MechanicPxPerU"/>): cabe el halo
        /// (<see cref="MorphoHaloPx"/>) y lo que las alas sobresalen de su caja.
        /// </summary>
        public const int MorphoSpritePadPx = 12;

        /// <summary>
        /// Lo que el halo engorda la silueta (px): ≈ 2 dp con la mariposa de 24-28 dp de envergadura (88 px de alas: 7 px son
        /// 1,9-2,2 dp).
        /// </summary>
        public const float MorphoHaloPx = 7f;

        // Altura de la maza de las antenas del glifo (px, y hacia arriba) [P].
        private const float AntennaTipY = 87f;

        private Sprite? _iconMorpho;
        private Sprite? _iconMorphoDisabled;
        private Sprite? _morphoWings;
        private Sprite? _morphoBody;
        private Sprite? _morphoWingsHalo;
        private Sprite? _morphoBodyHalo;

        /// <summary>Glifo de Pista · Morpho (32 dp a 3 px/dp): la mariposa azul con su destello sobre dos cuias (07 §7.6).</summary>
        public Sprite IconMorpho => _iconMorpho != null ? _iconMorpho : (_iconMorpho = BuildIconMorpho(false));

        /// <summary>Glifo deshabilitado (<c>deshabilitado_glifo</c>, plano y sin brillo).</summary>
        public Sprite IconMorphoDisabled => _iconMorphoDisabled != null ? _iconMorphoDisabled : (_iconMorphoDisabled = BuildIconMorpho(true));

        /// <summary>
        /// Las alas abiertas de la mariposa en vuelo (<see cref="MorphoWingsWidthU"/> × <see cref="MorphoWingsHeightU"/> u), con
        /// la raíz en el centro: el aleteo es la escala horizontal de la imagen.
        /// </summary>
        public Sprite MorphoWingsSprite => _morphoWings != null ? _morphoWings : (_morphoWings = BuildMorphoWings(false));

        /// <summary>Cuerpo, cabeza y antenas (<see cref="MorphoBodyWidthU"/> × <see cref="MorphoBodyHeightU"/> u), cabeza arriba; pivote en el centro.</summary>
        public Sprite MorphoBodySprite => _morphoBody != null ? _morphoBody : (_morphoBody = BuildMorphoBody(false));

        /// <summary>Silueta del halo de las alas (blanca; la tiñe <see cref="MorphoHalo"/>), del tamaño de <see cref="MorphoWingsSprite"/>.</summary>
        public Sprite MorphoWingsHaloSprite => _morphoWingsHalo != null ? _morphoWingsHalo : (_morphoWingsHalo = BuildMorphoWings(true));

        /// <summary>Silueta del halo del cuerpo (blanca), del tamaño de <see cref="MorphoBodySprite"/>.</summary>
        public Sprite MorphoBodyHaloSprite => _morphoBodyHalo != null ? _morphoBodyHalo : (_morphoBodyHalo = BuildMorphoBody(true));

        // Glifo de 96 × 96 px (y hacia arriba): dos cuias pequeñas abajo, en crema con la sombra de 1 dp del canto de la familia
        // (como «+1 cuia»), y la mariposa encima, con un contorno de tinta de 2,5 dp (07 §7.6: trazo 2,5 dp; la cara de la ranura
        // es azul agua y la mariposa también), las alas en azul morpho con su reflejo y el destello de marfil arriba a la
        // derecha, lejos de las esquinas donde van las insignias. Deshabilitado: todo en deshabilitado_glifo, plano.
        private Sprite BuildIconMorpho(bool disabled)
        {
            var pc = new PixelCanvas(96, 96);
            Color glyph = disabled ? ColorMath.WithAlpha(Palette.DisabledGlyph, 0.6f) : Palette.Cream;
            if (!disabled)
            {
                DrawIconCuias(pc, -3f, Palette.FamilyWater.Edge);
            }

            DrawIconCuias(pc, 0f, glyph);
            if (disabled)
            {
                DrawIconButterfly(pc, 0f, Palette.DisabledGlyph, Palette.DisabledGlyph, Palette.DisabledGlyph);
                return MakeSprite(pc, "icon_morpho_disabled", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
            }

            DrawIconButterfly(pc, 7.5f, Palette.Ink, Palette.Ink, Palette.Ink);
            DrawIconButterfly(pc, 0f, MorphoDeep, MorphoBlue, MorphoBody);
            for (int s = -1; s <= 1; s += 2)
            {
                // Reflejo estructural en el centro de cada ala de arriba.
                FillEllipseRotated(pc, 48f + (s * 18f), 70f, 8f, 4.6f, 90f - (s * 60f), MorphoLight);
            }

            // Destello de marfil (cruz de cuatro puntas) sobre el ala derecha, por debajo de las antenas (revisión de MO2: en lo
            // alto del lienzo cruzaba el canto de la ranura en la bandeja apretada).
            pc.FillCapsule(80f, 75f, 80f, 87f, 1.6f, Palette.UiPaper);
            pc.FillCapsule(74f, 81f, 86f, 81f, 1.6f, Palette.UiPaper);
            pc.FillCircle(80f, 81f, 2.6f, Palette.UiPaper);
            return MakeSprite(pc, "icon_morpho", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        // Las dos cuias del glifo (cuerpo, cuello y boca), desplazadas oy px.
        private static void DrawIconCuias(PixelCanvas pc, float oy, Color c)
        {
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? 6f : 58f;
                pc.FillRoundedRect(x, 4f + oy, x + 32f, 30f + oy, 10f, c);
                pc.FillRoundedRect(x + 10f, 28f + oy, x + 22f, 37f + oy, 3f, c);
                pc.FillRoundedRect(x + 7f, 35f + oy, x + 25f, 41f + oy, 3f, c);
            }
        }

        // La mariposa del glifo, con las alas engordadas grow px (el contorno se dibuja con grow > 0 debajo): alas de arriba y de
        // abajo con su filo (edge) y su color (wing), y el cuerpo y las antenas (body).
        private static void DrawIconButterfly(PixelCanvas pc, float grow, Color edge, Color wing, Color body)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                float upDeg = 90f - (s * 58f);
                float lowDeg = 90f + (s * 60f);
                FillEllipseRotated(pc, 48f + (s * 18f), 68f, 21f + grow, 12.5f + grow, upDeg, edge);
                FillEllipseRotated(pc, 48f + (s * 12f), 50f, 13f + grow, 9f + grow, lowDeg, edge);
                if (grow <= 0f)
                {
                    FillEllipseRotated(pc, 48f + (s * 18f), 68.5f, 18.5f, 10f, upDeg, wing);
                    FillEllipseRotated(pc, 48f + (s * 12f), 50.5f, 10.5f, 6.8f, lowDeg, wing);
                }
            }

            // Las antenas acaban 3 px más abajo y su contorno engorda menos que el del cuerpo (revisión de MO2): con el contorno
            // de tinta, la maza llega a y = 87 + 2,4 + 3 = 92,4 px, dentro del lienzo de 96 (antes, 96,9: salía recortada).
            float g = grow * 0.6f;
            float ga = grow * 0.4f;
            pc.FillCapsule(48f, 44f, 48f, 72f, 3.4f + g, body);
            pc.FillCircle(48f, 75f, 4.2f + g, body);
            for (int s = -1; s <= 1; s += 2)
            {
                pc.FillCapsule(48f + (s * 1.5f), 78f, 48f + (s * 9f), AntennaTipY, 1.3f + ga, body);
                pc.FillCircle(48f + (s * 9f), AntennaTipY, 2.4f + ga, body);
            }
        }

        // Alas abiertas a MechanicPxPerU (y hacia arriba), en un lienzo con MorphoSpritePadPx de margen por lado (para el halo):
        // las de arriba, grandes, y las de abajo, encima, con el acabado de juguete (D-058): esmalte azul iridiscente con volumen
        // de placa baja, el margen oscuro de la morpho, el reflejo claro hacia la raíz, el doble brillo del barniz, la oclusión
        // que proyecta el ala de abajo sobre la de arriba y la del cuerpo, y dos puntos de marfil en las puntas (≤ 2 dp). Con
        // halo, solo la silueta engordada MorphoHaloPx, en blanco (la imagen la tiñe con MorphoHalo): la misma de siempre.
        private Sprite BuildMorphoWings(bool halo)
        {
            int w = Mathf.RoundToInt(MorphoWingsWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(MorphoWingsHeightU * MechanicPxPerU);
            int pad = MorphoSpritePadPx;
            var pc = new PixelCanvas(w + (2 * pad), h + (2 * pad));
            float cx = (w * 0.5f) + pad;
            float cy = (h * 0.5f) + pad;
            if (halo)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    float upDeg = 90f - (s * 58f);
                    float lowDeg = 90f + (s * 60f);
                    FillEllipseRotated(pc, cx + (s * 22f), cy + 9f, 25f + MorphoHaloPx, 15f + MorphoHaloPx, upDeg, Color.white);
                    FillEllipseRotated(pc, cx + (s * 15f), cy - 14f, 16f + MorphoHaloPx, 11f + MorphoHaloPx, lowDeg, Color.white);
                }

                return MakeSprite(pc, "morpho_wings_halo", new Vector2(0.5f, 0.5f), Vector4.zero);
            }

            // Primero las dos alas de arriba y después las de abajo, que las tapan en la raíz.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    if (pass == 0)
                    {
                        DrawToyWing(pc, cx, cx + (s * 22f), cy + 9f, 25f, 15f, 90f - (s * 58f), cx + (s * 15f), cy - 14f, 16f, 11f, 90f + (s * 60f));
                    }
                    else
                    {
                        DrawToyWing(pc, cx, cx + (s * 15f), cy - 14f, 16f, 11f, 90f + (s * 60f), 0f, 0f, 0f, 0f, 0f);
                    }
                }
            }

            for (int s = -1; s <= 1; s += 2)
            {
                pc.FillCircle(cx + (s * 38f), cy + 26f, 2.2f, Palette.UiPaper);
            }

            return MakeSprite(pc, "morpho_wings", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Morpho de juguete: los tonos del esmalte, calculados una vez (en su propia clase: se inicializan al pintarla, con los
        // colores ya listos).
        private static class MorphoToy
        {
            public static readonly ToyTones Deep = ToyShading.Tones(MorphoDeep, 1f);
            public static readonly ToyTones Blue = ToyShading.Tones(MorphoFlightBlue, 0.8f);
            public static readonly ToyTones Light = ToyShading.Tones(MorphoFlightLight, 0.6f);
            public static readonly ToyTones Body = ToyShading.Tones(MorphoBody);
        }

        // Un ala de esmalte en la elipse (wx, wy, rx, ry) girada deg grados: placa baja abombada con el margen oscuro, el azul que
        // se aclara hacia la raíz (bodyX, la x del cuerpo) y se vuelve más claro donde le da el brillo (iridiscencia), y su
        // contorno fino. Si rx2 > 0, el ala de abajo (lx, ly, rx2, ry2, deg2) le proyecta una sombra suave.
        private static void DrawToyWing(PixelCanvas pc, float bodyX, float wx, float wy, float rx, float ry, float deg,
            float lx, float ly, float rx2, float ry2, float deg2)
        {
            float rad = deg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float cos2 = Mathf.Cos(deg2 * Mathf.Deg2Rad);
            float sin2 = Mathf.Sin(deg2 * Mathf.Deg2Rad);
            float r = Mathf.Max(rx, ry);
            float minR = Mathf.Min(rx, ry);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(wx - r - 1f));
            int x1 = Mathf.Min(pc.Width - 1, Mathf.CeilToInt(wx + r + 1f));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(wy - r - 1f));
            int y1 = Mathf.Min(pc.Height - 1, Mathf.CeilToInt(wy + r + 1f));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f) - wx;
                    float dy = (y + 0.5f) - wy;
                    float qx = ((dx * cos) + (dy * sin)) / rx;
                    float qy = ((-dx * sin) + (dy * cos)) / ry;
                    float q = Mathf.Sqrt((qx * qx) + (qy * qy));
                    float cov = PixelCanvas.Coverage((q - 1f) * minR);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    // Normal de placa baja, de vuelta al espacio de la pantalla.
                    float nx = 0.6f * ((qx * cos) - (qy * sin));
                    float ny = 0.6f * ((qx * sin) + (qy * cos));
                    Vector3 n = ToyShading.Normal(nx, ny);

                    // El color: claro en la raíz, azul de vuelo y el margen oscuro (≈ 4 px), con el contorno fino fuera.
                    // P3D-3 (el dueño, 2026-10-27): más contraste entre el margen azul profundo, más ancho y menos barnizado, y
                    // el centro claro, que llega más lejos desde la raíz.
                    float root = Mathf.Clamp01(1f - (Mathf.Abs((x + 0.5f) - bodyX) / (2.2f * rx)));
                    ToyTones t = ToyTones.Lerp(MorphoToy.Blue, MorphoToy.Light, 0.8f * root);
                    float edgePx = (1f - q) * minR;
                    float deep = 1f - Mathf.Clamp01((edgePx - 4.6f) / 2.4f);
                    t = ToyTones.Lerp(t, MorphoToy.Deep, deep);
                    float ao = 0f;
                    if (rx2 > 0f)
                    {
                        float ex = (x + 0.5f) - lx;
                        float ey = (y + 0.5f) - ly;
                        float px2 = ((ex * cos2) + (ey * sin2)) / rx2;
                        float py2 = ((-ex * sin2) + (ey * cos2)) / ry2;
                        float lowD = (Mathf.Sqrt((px2 * px2) + (py2 * py2)) - 1f) * Mathf.Min(rx2, ry2);
                        ao = 0.55f * (1f - Mathf.Clamp01(lowD / 5f));
                    }

                    ao = Mathf.Max(ao, 0.45f * (1f - Mathf.Clamp01((Mathf.Abs((x + 0.5f) - bodyX) - 3f) / 5f)));
                    Color c = ToyShading.Shade(t, n, ao, 1f - (0.6f * deep));
                    c = Color.Lerp(c, ColorMath.WithAlpha(MorphoToy.Light.Light, 1f), 0.35f * ToyShading.Specular(n, 4f) * (1f - ao) * (1f - deep));
                    c = Color.Lerp(c, MorphoToy.Deep.Outline, Mathf.Clamp01(1f - (edgePx / 1.1f)) * 0.9f);
                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }
        }

        // Cuerpo a MechanicPxPerU (cabeza arriba), con el mismo margen, de juguete lacado: abdomen y tórax como un cilindro negro
        // brillante, la cabeza abombada con dos puntos de luz por ojos y las dos antenas con su maza. Con halo, la silueta
        // engordada MorphoHaloPx, en blanco: la misma de siempre.
        private Sprite BuildMorphoBody(bool halo)
        {
            int w = Mathf.RoundToInt(MorphoBodyWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(MorphoBodyHeightU * MechanicPxPerU);
            int pad = MorphoSpritePadPx;
            var pc = new PixelCanvas(w + (2 * pad), h + (2 * pad));
            float cx = (w * 0.5f) + pad;
            float g = halo ? MorphoHaloPx : 0f;
            Color c = halo ? Color.white : MorphoBody;
            float headY = pad + (h * 0.62f) + 4f;
            for (int s = -1; s <= 1; s += 2)
            {
                pc.FillCapsule(cx + (s * 1.5f), pad + (h * 0.62f) + 7f, cx + (s * 8f), pad + h - 4f, 1.2f + g, c);
                pc.FillCircle(cx + (s * 8f), pad + h - 4f, 2.2f + g, c);
            }

            if (halo)
            {
                pc.FillCapsule(cx, pad + 6f, cx, pad + (h * 0.62f), 3.6f + g, c);
                pc.FillCircle(cx, headY, 4.6f + g, c);
                return MakeSprite(pc, "morpho_body_halo", new Vector2(0.5f, 0.5f), Vector4.zero);
            }

            float top = pad + (h * 0.62f);
            for (int y = 0; y < pc.Height; y++)
            {
                for (int x = 0; x < pc.Width; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float ty = Mathf.Clamp(py, pad + 6f, top);
                    float abdomen = Mathf.Sqrt(((px - cx) * (px - cx)) + ((py - ty) * (py - ty))) - 3.6f;
                    float headD = Mathf.Sqrt(((px - cx) * (px - cx)) + ((py - headY) * (py - headY))) - 4.6f;
                    float d = Mathf.Min(abdomen, headD);
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Vector3 n = headD < abdomen
                        ? ToyShading.Normal((px - cx) / 4.9f, (py - headY) / 4.9f)
                        : ToyShading.Normal((px - cx) / 3.9f, 0f);
                    float ao = 0.5f * ToyShading.CreaseAO(abdomen, headD, 2.5f);
                    pc.Blend(x, y, ToyShading.Shade(MorphoToy.Body, n, ao, 1f), cov);
                }
            }

            for (int s = -1; s <= 1; s += 2)
            {
                pc.FillCircle(cx + (s * 2.3f), headY + 0.8f, 1.1f, new Color(1f, 1f, 1f, 0.85f));
            }

            return MakeSprite(pc, "morpho_body", new Vector2(0.5f, 0.5f), Vector4.zero);
        }
    }
}
