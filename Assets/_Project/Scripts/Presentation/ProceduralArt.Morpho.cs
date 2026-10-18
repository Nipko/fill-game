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
    /// regla (como el filo de la luciérnaga). Lo comprueba <c>MorphoUiTests</c>.</para>
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

        private Sprite? _iconMorpho;
        private Sprite? _iconMorphoDisabled;
        private Sprite? _morphoWings;
        private Sprite? _morphoBody;

        /// <summary>Glifo de Pista · Morpho (32 dp a 3 px/dp): la mariposa azul con su destello sobre dos cuias (07 §7.6).</summary>
        public Sprite IconMorpho => _iconMorpho != null ? _iconMorpho : (_iconMorpho = BuildIconMorpho(false));

        /// <summary>Glifo deshabilitado (<c>deshabilitado_glifo</c>, plano y sin brillo).</summary>
        public Sprite IconMorphoDisabled => _iconMorphoDisabled != null ? _iconMorphoDisabled : (_iconMorphoDisabled = BuildIconMorpho(true));

        /// <summary>
        /// Las alas abiertas de la mariposa en vuelo (<see cref="MorphoWingsWidthU"/> × <see cref="MorphoWingsHeightU"/> u), con
        /// la raíz en el centro: el aleteo es la escala horizontal de la imagen.
        /// </summary>
        public Sprite MorphoWingsSprite => _morphoWings != null ? _morphoWings : (_morphoWings = BuildMorphoWings());

        /// <summary>Cuerpo, cabeza y antenas (<see cref="MorphoBodyWidthU"/> × <see cref="MorphoBodyHeightU"/> u), cabeza arriba; pivote en el centro.</summary>
        public Sprite MorphoBodySprite => _morphoBody != null ? _morphoBody : (_morphoBody = BuildMorphoBody());

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

            // Destello de marfil (cruz de cuatro puntas) sobre el ala derecha.
            pc.FillCapsule(80f, 78f, 80f, 92f, 1.6f, Palette.UiPaper);
            pc.FillCapsule(73f, 85f, 87f, 85f, 1.6f, Palette.UiPaper);
            pc.FillCircle(80f, 85f, 2.6f, Palette.UiPaper);
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

            float g = grow * 0.6f;
            pc.FillCapsule(48f, 44f, 48f, 72f, 3.4f + g, body);
            pc.FillCircle(48f, 75f, 4.2f + g, body);
            for (int s = -1; s <= 1; s += 2)
            {
                pc.FillCapsule(48f + (s * 1.5f), 78f, 48f + (s * 9f), 90f, 1.3f + g, body);
                pc.FillCircle(48f + (s * 9f), 90f, 2.4f + g, body);
            }
        }

        // Alas abiertas a MechanicPxPerU (y hacia arriba): las de arriba, grandes, y las de abajo, con el filo oscuro, el azul,
        // el reflejo claro hacia la raíz y dos puntos de marfil en las puntas (≤ 2 dp).
        private Sprite BuildMorphoWings()
        {
            int w = Mathf.RoundToInt(MorphoWingsWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(MorphoWingsHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            for (int s = -1; s <= 1; s += 2)
            {
                float upDeg = 90f - (s * 58f);
                float lowDeg = 90f + (s * 60f);
                FillEllipseRotated(pc, cx + (s * 22f), cy + 9f, 25f, 15f, upDeg, MorphoDeep);
                FillEllipseRotated(pc, cx + (s * 15f), cy - 14f, 16f, 11f, lowDeg, MorphoDeep);
                FillEllipseRotated(pc, cx + (s * 22f), cy + 9.5f, 22f, 12.5f, upDeg, MorphoBlue);
                FillEllipseRotated(pc, cx + (s * 15f), cy - 13.5f, 13.5f, 8.8f, lowDeg, MorphoBlue);
                FillEllipseRotated(pc, cx + (s * 17f), cy + 6f, 11f, 6f, upDeg, ColorMath.WithAlpha(MorphoLight, 0.9f));
                pc.FillCircle(cx + (s * 38f), cy + 26f, 2.2f, Palette.UiPaper);
            }

            return MakeSprite(pc, "morpho_wings", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Cuerpo a MechanicPxPerU (cabeza arriba): abdomen, tórax, cabeza y dos antenas con su maza.
        private Sprite BuildMorphoBody()
        {
            int w = Mathf.RoundToInt(MorphoBodyWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(MorphoBodyHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            pc.FillCapsule(cx, 6f, cx, h * 0.62f, 3.6f, MorphoBody);
            pc.FillCircle(cx, (h * 0.62f) + 4f, 4.6f, MorphoBody);
            for (int s = -1; s <= 1; s += 2)
            {
                pc.FillCapsule(cx + (s * 1.5f), (h * 0.62f) + 7f, cx + (s * 8f), h - 4f, 1.2f, MorphoBody);
                pc.FillCircle(cx + (s * 8f), h - 4f, 2.2f, MorphoBody);
            }

            return MakeSprite(pc, "morpho_body", new Vector2(0.5f, 0.5f), Vector4.zero);
        }
    }
}
