#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Gráficos provisionales de la pantalla de inicio (06 §4.7; D-008 punto 6; tarea B1-6) y del botón «Inicio» de la
    /// victoria, en la familia de materiales de D-003 (madera, hoja, piedra; sin oro biselado ni gemas, 07 §1.3): CTA grande
    /// de hoja, botón secundario de madera (07 §7.3), placa de madera 9-slice para los contadores y las pestañas, e iconos
    /// de Semillas, Canoas, Ajustes y Canoas infinitas (07 §7.6; glosario §2.1 y §2.2). Los sustituye el arte del artista.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Semilla ámbar de 07 §7.6 [P] (<c>#D9A441</c>).</summary>
        public static readonly Color SeedAmber = Palette.Hex(0xD9A441);

        /// <summary>CTA grande de la familia Hoja para «Jugar · Nivel {n}»: cara de 64 dp + canto (60-72 dp, como Magic Sort, ficha §4.8).</summary>
        public ReliefSprites CtaLeafLarge { get; private set; } = null!;

        /// <summary>Botón secundario de la familia Madera (07 §7.3: 40 dp de alto): «Inicio» en la victoria [P].</summary>
        public ReliefSprites CtaWood { get; private set; } = null!;

        /// <summary>Placa de madera oscura 9-slice (borde de 18 dp): contadores del inicio y barra de pestañas.</summary>
        public Sprite WoodPlate { get; private set; } = null!;

        /// <summary>Semilla ovalada ámbar con veta central, brillo arriba y borde marrón (glosario §2.1), 32 dp.</summary>
        public Sprite IconSeed { get; private set; } = null!;

        /// <summary>Canoa de madera de lado, proa a la derecha, con línea de agua (glosario §2.2), 44 × 26 dp.</summary>
        public Sprite IconCanoe { get; private set; } = null!;

        /// <summary>Canoa vacía: silueta gris volcada con la línea de agua por encima (glosario §2.2), 44 × 26 dp.</summary>
        public Sprite IconCanoeEmpty { get; private set; } = null!;

        /// <summary>Engranaje de Ajustes en <c>crema</c> (07 §7.6: «engranaje de madera» sobre el disco de madera), 26 dp.</summary>
        public Sprite IconGear { get; private set; } = null!;

        /// <summary>Engranaje deshabilitado (<c>deshabilitado_glifo</c>).</summary>
        public Sprite IconGearDisabled { get; private set; } = null!;

        /// <summary>Signo ∞ de Canoas infinitas (la fuente del atlas no tiene U+221E), 26 × 14 dp.</summary>
        public Sprite IconInfinite { get; private set; } = null!;

        private void BuildLobbyUi()
        {
            CtaLeafLarge = Relief(ArtSlots.ButtonLeaf, "cta_leaf_large", 72f, 64f, 24f, false, Palette.FamilyLeaf, true, 312f);
            CtaWood = Relief(ArtSlots.ButtonWood, "cta_wood", 72f, 40f, 16f, false, Palette.FamilyWood, true, 168f);
            WoodPlate = BuildPlate("wood_plate", 48f, 48f, 14f, false, 0f, 18f);
            IconSeed = BuildIconSeed();
            IconCanoe = BuildIconCanoe(false);
            IconCanoeEmpty = BuildIconCanoe(true);
            IconGear = BuildIconGear(Palette.Cream, Palette.FamilyWood.Edge, "icon_gear");
            IconGearDisabled = BuildIconGear(Palette.DisabledGlyph, Color.clear, "icon_gear_disabled");
            IconInfinite = BuildIconInfinite();
        }

        private Sprite BuildIconSeed()
        {
            // Óvalo de 20 × 27 dp inclinado 28°, más estrecho hacia la punta (silueta de semilla de cacao estilizada), luz
            // arriba a la izquierda (07 §1.1), veta central y brillo superior; borde marrón de 1,8 dp.
            const float k = UiPxPerDp;
            const float size = 32f;
            int s = Mathf.RoundToInt(size * k);
            var pc = new PixelCanvas(s, s);
            Color light = ColorMath.Lighten(SeedAmber, 0.30f);
            Color dark = ColorMath.Darken(SeedAmber, 0.12f);
            Color vein = ColorMath.Darken(SeedAmber, 0.38f);
            float angle = -28f * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            const float rx = 10f;
            const float ry = 13.5f;
            const float cx = 16f;
            const float cy = 16.5f;
            for (int y = 0; y < s; y++)
            {
                float yd = size - ((y + 0.5f) / k);
                for (int x = 0; x < s; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float dx = xd - cx;
                    float dy = yd - cy;
                    float u = (dx * cos) + (dy * sin);
                    float v = (-dx * sin) + (dy * cos);
                    float a = rx * (v < 0f ? 1f - (0.12f * Mathf.Clamp01(-v / ry)) : 1f);
                    float q = Mathf.Sqrt(((u * u) / (a * a)) + ((v * v) / (ry * ry)));
                    float d = (q - 1f) * Mathf.Min(a, ry);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, Palette.DockShadow, cov);
                    float inside = PixelCanvas.Coverage((d + 1.8f) * k);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    float lightT = Mathf.Clamp01(0.5f - (0.35f * u / a) - (0.35f * v / ry));
                    Color c = Color.Lerp(dark, light, lightT);
                    if (Mathf.Abs(u) < 0.6f && Mathf.Abs(v) < ry * 0.62f)
                    {
                        c = Color.Lerp(c, vein, 0.8f);
                    }

                    float hu = (u + 4.2f) / 2.2f;
                    float hv = (v + 5.5f) / 4.2f;
                    float h = (hu * hu) + (hv * hv);
                    if (h < 1f)
                    {
                        c = Color.Lerp(c, Color.white, 0.55f * (1f - h));
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, inside);
                }
            }

            return MakeSprite(pc, "icon_seed", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        private Sprite BuildIconCanoe(bool empty)
        {
            // Casco de 40 dp con los extremos levantados y la proa (derecha) más alta; borda clara, una línea de tablón y
            // contorno oscuro. Vacía: el mismo casco volcado (quilla arriba) en grises. Delante, una línea de agua ondulada.
            const float k = UiPxPerDp;
            const float wDp = 44f;
            const float hDp = 26f;
            int w = Mathf.RoundToInt(wDp * k);
            int h = Mathf.RoundToInt(hDp * k);
            var pc = new PixelCanvas(w, h);
            Color hullTop = empty ? Palette.Disabled : Palette.DockLight;
            Color hullLow = empty ? Palette.DisabledOutline : Palette.DockShadow;
            Color rim = empty ? ColorMath.Lighten(Palette.Disabled, 0.15f) : Palette.FamilyWood.Light;
            Color outline = empty ? Palette.DisabledGlyph : Palette.Groove;
            Color water = empty ? new Color(0.86f, 0.87f, 0.88f, 0.95f) : new Color(0x3B / 255f, 0xB8 / 255f, 0xD9 / 255f, 0.95f);
            float waterY = empty ? 13f : 17.5f;
            for (int y = 0; y < h; y++)
            {
                float yd = hDp - ((y + 0.5f) / k);
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float xn = (xd - 22f) / 20f;
                    float ax = Mathf.Min(1f, Mathf.Abs(xn));
                    float rise = (5.5f * Mathf.Pow(ax, 5f)) + (xn > 0f ? 2.5f * ax * ax * ax : 0f);
                    float top = 10f - rise;
                    float bottom = 10f + (9f * Mathf.Pow(Mathf.Max(0f, 1f - (xn * xn)), 0.7f));
                    if (empty)
                    {
                        float flippedTop = 28f - bottom;
                        bottom = 28f - top;
                        top = flippedTop;
                    }

                    float d = Mathf.Max(Mathf.Max(top - yd, yd - bottom), (Mathf.Abs(xn) - 1f) * 20f);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov > 0f)
                    {
                        pc.Blend(x, y, outline, cov);
                        float inside = PixelCanvas.Coverage((d + 1.4f) * k);
                        if (inside > 0f)
                        {
                            float t = Mathf.Clamp01((yd - top) / Mathf.Max(0.01f, bottom - top));
                            Color c = Color.Lerp(hullTop, hullLow, t);
                            float rimDistance = empty ? bottom - yd : yd - top;
                            if (rimDistance < 2.4f)
                            {
                                c = rim;
                            }
                            else if (Mathf.Abs(yd - (top + ((bottom - top) * (empty ? 0.55f : 0.45f)))) < 0.5f)
                            {
                                c = ColorMath.Darken(c, 0.25f);
                            }

                            c.a = 1f;
                            pc.Blend(x, y, c, inside);
                        }
                    }

                    if (xd > 1.5f && xd < wDp - 1.5f)
                    {
                        float yw = waterY + (0.9f * Mathf.Sin(xd * 0.75f));
                        float dw = Mathf.Abs(yd - yw) - 1.1f;
                        pc.Blend(x, y, water, PixelCanvas.Coverage(dw * k));
                    }
                }
            }

            return MakeSprite(pc, empty ? "icon_canoe_empty" : "icon_canoe", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        private Sprite BuildIconGear(Color glyph, Color shadow, string name)
        {
            // Engranaje de 8 dientes con agujero central, 26 dp a 3 px/dp, silueta sólida con sombra interior de 1 dp del
            // canto de la familia (guía 5.7), como los demás glifos.
            const int s = 78;
            var pc = new PixelCanvas(s, s);
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 0 && shadow.a <= 0f)
                {
                    continue;
                }

                Color c = pass == 0 ? shadow : glyph;
                float oy = pass == 0 ? -3f : 0f;
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float dx = (x + 0.5f) - 39f;
                        float dy = (y + 0.5f) - (40f + oy);
                        float r = Mathf.Sqrt((dx * dx) + (dy * dy));
                        float theta = Mathf.Atan2(dy, dx);
                        float outer = 28f + (6f * Mathf.Clamp(Mathf.Cos(8f * theta) * 2.2f, -1f, 1f));
                        float d = Mathf.Max(r - outer, 10.5f - r);
                        pc.Blend(x, y, c, PixelCanvas.Coverage(d));
                    }
                }
            }

            return MakeSprite(pc, name, new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private Sprite BuildIconInfinite()
        {
            // Lemniscata de Bernoulli (a = 9,5 dp) con trazo crema de 3 dp y contorno oscuro.
            const int w = 78;
            const int h = 42;
            var pc = new PixelCanvas(w, h);
            const float a = 28.5f;
            const int segments = 64;
            for (int pass = 0; pass < 2; pass++)
            {
                Color c = pass == 0 ? Palette.Groove : Palette.Cream;
                float radius = pass == 0 ? 7.2f : 4.5f;
                for (int i = 0; i < segments; i++)
                {
                    Vector2 p0 = Lemniscate(a, (Mathf.PI * 2f * i) / segments);
                    Vector2 p1 = Lemniscate(a, (Mathf.PI * 2f * (i + 1)) / segments);
                    pc.FillCapsule(39f + p0.x, 21f + p0.y, 39f + p1.x, 21f + p1.y, radius, c);
                }
            }

            return MakeSprite(pc, "icon_infinite", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private static Vector2 Lemniscate(float a, float t)
        {
            float sin = Mathf.Sin(t);
            float cos = Mathf.Cos(t);
            float den = 1f + (sin * sin);
            return new Vector2((a * cos) / den, (a * sin * cos) / den);
        }
    }
}
