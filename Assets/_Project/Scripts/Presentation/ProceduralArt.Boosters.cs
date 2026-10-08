#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Gráficos provisionales de la bandeja de comodines (B2-1; 07 §7.4 y §7.6) y del embudo de bloqueo de D-008 (B2-2 y
    /// B2-3; 04 §6.6 y §7.3), en la familia de materiales de D-003 (madera, hoja y agua; sin oro biselado, gemas, corazones
    /// rotos ni calaveras de Magic Sort, 07 §1.3): glifo «+1 cuia», icono de video, pausa, cierre y la cesta vacía de Nivel
    /// fallido. Los sustituye el arte del artista.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Glifo de Cuia extra · «+1 cuia» en <c>crema</c>: silueta de cuia con un «+» (07 §7.6), 32 dp.</summary>
        public Sprite IconCuiaPlus { get; private set; } = null!;

        /// <summary>Glifo «+1 cuia» deshabilitado (<c>deshabilitado_glifo</c>).</summary>
        public Sprite IconCuiaPlusDisabled { get; private set; } = null!;

        /// <summary>Icono «Ver video» (07 §7.3: 20 dp): pantalla redondeada con triángulo de reproducción, en crema.</summary>
        public Sprite IconVideo { get; private set; } = null!;

        /// <summary>Glifo de pausa (dos barras) en <c>crema</c>, 26 dp, para el disco de madera de la cabecera (02 §5).</summary>
        public Sprite IconPause { get; private set; } = null!;

        /// <summary>Glifo de cierre (X) en <c>crema</c>, 22 dp, para el disco de madera de los modales.</summary>
        public Sprite IconClose { get; private set; } = null!;

        /// <summary>Cesta de semillas vacía de Nivel fallido (04 §6.6, paso 3), 96 × 72 dp.</summary>
        public Sprite EmptyBasket { get; private set; } = null!;

        private void BuildBoosterUi()
        {
            IconCuiaPlus = BuildIconCuiaPlus(Palette.Cream, Palette.FamilyWater.Edge, "icon_cuia_plus");
            IconCuiaPlusDisabled = BuildIconCuiaPlus(Palette.DisabledGlyph, Color.clear, "icon_cuia_plus_disabled");
            IconVideo = BuildIconVideo();
            IconPause = BuildIconPause();
            IconClose = BuildIconClose();
            EmptyBasket = BuildEmptyBasket();
        }

        private Sprite BuildIconCuiaPlus(Color glyph, Color shadow, string name)
        {
            // Cuia de perfil (cuello corto, hombros redondeados, cuerpo de 13 dp) a la izquierda y un «+» de trazo grueso a la
            // derecha, 32 dp a 3 px/dp; silueta sólida con la sombra interior de 1 dp del canto de la familia (guía 5.7).
            var pc = new PixelCanvas(96, 96);
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 0 && shadow.a <= 0f)
                {
                    continue;
                }

                Color c = pass == 0 ? shadow : glyph;
                float oy = pass == 0 ? -3f : 0f;
                pc.FillRoundedRect(14f, 8f + oy, 53f, 64f + oy, 12f, c);
                pc.FillRoundedRect(24f, 60f + oy, 43f, 80f + oy, 4f, c);
                pc.FillRoundedRect(21f, 76f + oy, 46f, 86f + oy, 4f, c);
                pc.FillCapsule(74f, 26f + oy, 74f, 58f + oy, 5.5f, c);
                pc.FillCapsule(58f, 42f + oy, 90f, 42f + oy, 5.5f, c);
            }

            // Vidrio: el interior de la cuia se vacía a la mitad para que se lea como recipiente y no como bloque.
            if (shadow.a > 0f)
            {
                Color inner = new Color(Palette.FamilyWater.Base.r, Palette.FamilyWater.Base.g, Palette.FamilyWater.Base.b, 0.55f);
                pc.FillRoundedRect(21f, 15f, 46f, 40f, 7f, inner);
            }

            return MakeSprite(pc, name, new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private Sprite BuildIconVideo()
        {
            // 20 dp: pantalla redondeada de crema con contorno de madera y triángulo de reproducción en agua (07 §7.3).
            var pc = new PixelCanvas(60, 60);
            pc.FillRoundedRect(3f, 9f, 57f, 51f, 9f, Palette.FamilyWood.Outline);
            pc.FillRoundedRect(6.5f, 12.5f, 53.5f, 47.5f, 6f, Palette.Cream);
            pc.FillTriangle(new Vector2(24f, 20f), new Vector2(24f, 40f), new Vector2(41f, 30f), Palette.FamilyWater.Base);
            return MakeSprite(pc, "icon_video", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private Sprite BuildIconPause()
        {
            var pc = new PixelCanvas(78, 78);
            for (int pass = 0; pass < 2; pass++)
            {
                Color c = pass == 0 ? Palette.FamilyWood.Edge : Palette.Cream;
                float oy = pass == 0 ? -3f : 0f;
                pc.FillRoundedRect(20f, 16f + oy, 33f, 62f + oy, 4f, c);
                pc.FillRoundedRect(45f, 16f + oy, 58f, 62f + oy, 4f, c);
            }

            return MakeSprite(pc, "icon_pause", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private Sprite BuildIconClose()
        {
            var pc = new PixelCanvas(66, 66);
            for (int pass = 0; pass < 2; pass++)
            {
                Color c = pass == 0 ? Palette.FamilyWood.Edge : Palette.Cream;
                float oy = pass == 0 ? -3f : 0f;
                pc.FillCapsule(15f, 15f + oy, 51f, 51f + oy, 5.5f, c);
                pc.FillCapsule(15f, 51f + oy, 51f, 15f + oy, 5.5f, c);
            }

            return MakeSprite(pc, "icon_close", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private Sprite BuildEmptyBasket()
        {
            // Cesta tejida de fibra (paleta de la cuerda de las cuias), vacía, con dos semillas caídas a la izquierda: «el
            // premio que no se cobra» (04 §6.6) sin el pedestal de monedas de Magic Sort. 96 × 72 dp a 3 px/dp; y hacia arriba.
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(96f * k);
            int h = Mathf.RoundToInt(72f * k);
            var pc = new PixelCanvas(w, h);
            pc.FillEllipseSoft(48f * k, 7f * k, 40f * k, 5f * k, 3f * k, new Color(0f, 0f, 0f, 0.28f));
            for (int y = 0; y < h; y++)
            {
                float yd = (y + 0.5f) / k;
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    // Cuerpo: trapecio de 52 dp abajo a 76 dp arriba entre y = 8 y y = 46, con esquinas suaves.
                    float t = Mathf.Clamp01((yd - 8f) / 38f);
                    float half = Mathf.Lerp(26f, 38f, t);
                    float d = Mathf.Max(Mathf.Abs(xd - 48f) - half, Mathf.Max(8f - yd, yd - 46f));
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, Palette.FiberShadow, cov);
                    float inside = PixelCanvas.Coverage((d + 1.5f) * k);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    // Tejido: bandas horizontales alternas y trenzado vertical.
                    int band = Mathf.FloorToInt((yd - 8f) / 5f);
                    float weave = Mathf.Sin(((xd - 48f) / half) * 9f + (band * 1.6f));
                    Color c = Color.Lerp(Palette.FiberBase, Palette.FiberLight, 0.5f + (0.5f * weave));
                    if (((yd - 8f) % 5f) < 0.9f)
                    {
                        c = Palette.FiberShadow;
                    }

                    c = Color.Lerp(c, Palette.FiberShadow, 0.35f * (1f - t));
                    c.a = 1f;
                    pc.Blend(x, y, c, inside);
                }
            }

            // Borde superior trenzado y el hueco oscuro de dentro (vacía).
            pc.FillRoundedRect(8f * k, 43f * k, 88f * k, 51f * k, 4f * k, Palette.FiberShadow);
            pc.FillRoundedRect(9.5f * k, 44.5f * k, 86.5f * k, 49.5f * k, 2.5f * k, Palette.FiberLight);
            pc.FillEllipseSoft(48f * k, 47f * k, 34f * k, 2.2f * k, 1f * k, new Color(0x3A / 255f, 0x24 / 255f, 0x14 / 255f, 0.9f));

            // Asas.
            pc.StrokeArc(48f * k, 47f * k, 22f * k, 3f * k, 20f, 160f, Palette.FiberShadow);

            // Dos semillas ámbar caídas fuera de la cesta.
            pc.FillCircle(12f * k, 10f * k, 4.2f * k, Palette.DockShadow);
            pc.FillCircle(12f * k, 10f * k, 3.2f * k, SeedAmber);
            pc.FillCircle(84f * k, 9f * k, 3.6f * k, Palette.DockShadow);
            pc.FillCircle(84f * k, 9f * k, 2.7f * k, SeedAmber);
            return MakeSprite(pc, "empty_basket", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }
    }
}
