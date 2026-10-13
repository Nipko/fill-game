#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Sprites de un botón con relieve (guía visual v2, 5.1): <see cref="Base"/> (sombra proyectada, canto de 6 dp y
    /// contorno) queda fija y <see cref="Face"/> (cara con degradado, brillo interior, lustre y contorno) baja 4 dp al
    /// pulsar. <see cref="FaceDisabled"/> es la cara plana del estado deshabilitado (sin canto ni sombra). Si la ranura de
    /// arte tiene un PNG pintado, <see cref="Painted"/> es true: el PNG lleva cara, canto y sombra en un solo sprite.
    /// </summary>
    public sealed class ReliefSprites
    {
        internal ReliefSprites(Sprite face, Sprite? baseSprite, Sprite? faceDisabled, float widthDp, float heightDp, float faceHeightDp, bool sliced, bool painted)
        {
            Face = face;
            Base = baseSprite;
            FaceDisabled = faceDisabled;
            WidthDp = widthDp;
            HeightDp = heightDp;
            FaceHeightDp = faceHeightDp;
            Sliced = sliced;
            Painted = painted;
        }

        /// <summary>Cara (o el PNG completo si <see cref="Painted"/>).</summary>
        public Sprite Face { get; }

        /// <summary>Sombra, canto y contorno (null con un PNG pintado).</summary>
        public Sprite? Base { get; }

        /// <summary>Cara plana del estado deshabilitado (null si el botón nunca se deshabilita).</summary>
        public Sprite? FaceDisabled { get; }

        /// <summary>Ancho nativo del sprite (dp).</summary>
        public float WidthDp { get; }

        /// <summary>Alto del sprite (dp): cara + canto + sombra.</summary>
        public float HeightDp { get; }

        /// <summary>Alto de la cara (dp).</summary>
        public float FaceHeightDp { get; }

        /// <summary>9-slice horizontal (se estira a lo ancho a su alto nativo).</summary>
        public bool Sliced { get; }

        /// <summary>Viene de una ranura de arte (PNG pintado).</summary>
        public bool Painted { get; }
    }

    public sealed partial class ProceduralArt
    {
        /// <summary>Canto visible de un botón en reposo (guía 5.1).</summary>
        public const float ButtonEdgeDp = 6f;

        /// <summary>Lo que baja la cara al pulsar (guía 5.1): el canto visible pasa de 6 a 2 dp.</summary>
        public const float ButtonPressDp = 4f;

        private readonly Dictionary<int, Sprite> _trays = new Dictionary<int, Sprite>();
        private readonly Dictionary<int, Sprite> _surfaces = new Dictionary<int, Sprite>();

        // Resolución de la superficie de los modales (px por dp).
        private const float ModalSurfacePxPerDp = 1.5f;

        /// <summary>CTA de la familia Hoja (Siguiente): cara de 50 dp + canto de 6 (56, 02 §3.3), 9-slice horizontal.</summary>
        public ReliefSprites CtaLeaf { get; private set; } = null!;

        /// <summary>CTA de la familia Agua (Deshacer · Marea en el modal).</summary>
        public ReliefSprites CtaWater { get; private set; } = null!;

        /// <summary>Ranura de Deshacer de la bandeja: cuadrado redondeado de 64 dp, familia Agua (guía 3.6).</summary>
        public ReliefSprites SlotWater { get; private set; } = null!;

        /// <summary>Disco de Reiniciar de 40 dp, familia Madera (guía 3.5).</summary>
        public ReliefSprites DiscWood { get; private set; } = null!;

        /// <summary>Píldora de 60 × 16 dp de <c>madera_oscura</c> bajo el disco de Reiniciar (guía 3.5).</summary>
        public Sprite Pill { get; private set; } = null!;

        /// <summary>Placa de la insignia de nivel con sus dos hojas, 136 × 52 dp (ranura <c>ui_placa_nivel</c>; guía 3.5).</summary>
        public Sprite BadgePlate { get; private set; } = null!;

        /// <summary>
        /// Marco de los modales, 9-slice con el centro transparente (ranura <c>ui_panel_modal</c>, solo marco; guía 5.3). La
        /// superficie la pone <see cref="ModalSurfaceFor"/>, siempre por código.
        /// </summary>
        public Sprite ModalPanel { get; private set; } = null!;

        /// <summary>
        /// Superficie de madera clara pulida de un modal de <paramref name="widthDp"/> × <paramref name="heightDp"/> dp (guía
        /// 5.3; revisión visual del 2026-10-07): <c>panel_claro</c> → <c>panel_claro_fondo</c> con veta horizontal (multiplica
        /// 0,94-0,96, periodo de 7-11 dp, ondulación suave) y lustre arriba, para que no se lea como papel ni cartulina. Se
        /// genera al tamaño exacto (a 1,5 px/dp) para que la veta no se estire.
        /// </summary>
        public Sprite ModalSurfaceFor(float widthDp, float heightDp)
        {
            int key = ModalSurfaceKey(widthDp, heightDp, out int w, out int h);
            if (!_surfaces.TryGetValue(key, out Sprite? sprite))
            {
                sprite = BuildModalSurface(w, h);
                _surfaces[key] = sprite;
            }

            return sprite;
        }

        /// <summary>
        /// La superficie de <see cref="ModalSurfaceFor"/> de esas medidas ya está generada (no costará nada pedirla; tests del
        /// banner de mundo, que la pinta en otro hilo con su arte: <see cref="RequestBannerArt"/>).
        /// </summary>
        public bool HasModalSurface(float widthDp, float heightDp)
        {
            return _surfaces.ContainsKey(ModalSurfaceKey(widthDp, heightDp, out _, out _));
        }

        // Clave de la caché de superficies y sus medidas en dp enteros (las de ModalSurfaceFor).
        private static int ModalSurfaceKey(float widthDp, float heightDp, out int w, out int h)
        {
            w = Mathf.Max(8, Mathf.RoundToInt(widthDp));
            h = Mathf.Max(8, Mathf.RoundToInt(heightDp));
            return (w * 4096) + h;
        }

        // La superficie que pintó otro hilo (el arte del banner de mundo, RequestBannerArt), a la caché si aún no está: el hilo
        // principal solo crea la textura.
        private void AdoptModalSurface(int key, int w, int h, BannerPixels px)
        {
            if (!_surfaces.ContainsKey(key))
            {
                _surfaces[key] = MakeSprite32(px, ModalSurfaceName(w, h), new Vector2(0.5f, 0.5f), ModalSurfacePxPerDp);
            }
        }

        /// <summary>Cinta-hoja de victoria, 344 × 72 dp (ranura <c>ui_cinta_hoja</c>; guía 5.5 con la revisión visual del 2026-10-07).</summary>
        public Sprite Ribbon { get; private set; } = null!;

        /// <summary>Placa de cabecera del modal de bloqueo, 296 × 40 dp (guía 5.3).</summary>
        public Sprite HeaderPlate { get; private set; } = null!;

        /// <summary>Franja de agua quieta del modal de bloqueo, 296 × 20 dp, sin animación (guía 5.3).</summary>
        public Sprite StillWater { get; private set; } = null!;

        /// <summary>Velo vertical de la barra de estado: <c>scrim</c> 0,35 → 0 (guía 3.5).</summary>
        public Sprite StatusScrim { get; private set; } = null!;

        /// <summary>Glifo de Marea · Deshacer en <c>crema</c> (ola que retrocede con flecha curva; 07 §7.6, guía 5.7).</summary>
        public Sprite IconUndo { get; private set; } = null!;

        /// <summary>Glifo de Marea deshabilitado (<c>deshabilitado_glifo</c>, plano).</summary>
        public Sprite IconUndoDisabled { get; private set; } = null!;

        /// <summary>Glifo de Volver a la orilla · Reiniciar en <c>crema</c> (flecha circular con ola).</summary>
        public Sprite IconRestart { get; private set; } = null!;

        /// <summary>Bandeja «cumbuca» ajustada al ancho <paramref name="widthDp"/> (80 dp de alto; ranura <c>ui_bandeja</c>).</summary>
        public Sprite Tray(float widthDp)
        {
            int key = Mathf.RoundToInt(widthDp);
            if (!_trays.TryGetValue(key, out Sprite? sprite))
            {
                sprite = ArtSlots.Find(ArtSlots.Tray) ?? BuildTray(key);
                _trays[key] = sprite;
            }

            return sprite;
        }

        /// <summary>El sprite de la bandeja lleva un margen de sombra alrededor (dp a cada lado y abajo).</summary>
        public bool TrayIsPainted => ArtSlots.Find(ArtSlots.Tray) != null;

        private void BuildUi()
        {
            CtaLeaf = Relief(ArtSlots.ButtonLeaf, "cta_leaf", 72f, 50f, 20f, false, Palette.FamilyLeaf, true, 312f);
            CtaWater = Relief(ArtSlots.ButtonWater, "cta_water", 72f, 50f, 20f, false, Palette.FamilyWater, true, 312f);
            SlotWater = Relief(ArtSlots.SlotWater, "slot_water", 64f, 64f, 18f, false, Palette.FamilyWater, false, 64f);
            DiscWood = Relief(ArtSlots.DiscWood, "disc_wood", 40f, 40f, 20f, true, Palette.FamilyWood, false, 40f);
            Pill = BuildPlate("pill", 60f, 16f, 8f, false, 0f);
            BadgePlate = ArtSlots.Find(ArtSlots.BadgePlate) ?? BuildPlate("badge_plate", 136f, 52f, 12f, true, 4f);
            ModalPanel = ArtSlots.Find(ArtSlots.ModalPanel) ?? BuildModalPanel();

            Ribbon = ArtSlots.Find(ArtSlots.Ribbon) ?? BuildRibbon();
            HeaderPlate = BuildPlate("header_plate", 296f, 40f, 12f, false, 0f);
            StillWater = BuildStillWater();
            StatusScrim = BuildScrim();
            IconUndo = BuildIconUndo(Palette.Cream, Palette.FamilyWater.Edge, "icon_undo");
            IconUndoDisabled = BuildIconUndo(Palette.DisabledGlyph, Color.clear, "icon_undo_disabled");
            IconRestart = BuildIconRestart(Palette.Cream, Palette.FamilyWood.Edge);
            BuildLobbyUi();
            BuildBoosterUi();
        }

        private ReliefSprites Relief(string slot, string name, float wDp, float faceHDp, float radiusDp, bool circle, ButtonFamily family, bool sliced, float paintedWidthDp)
        {
            Sprite? painted = ArtSlots.Find(slot);
            float heightDp = faceHDp + 14f;
            if (painted != null)
            {
                float h = painted.rect.height / (painted.pixelsPerUnit / 100f);
                return new ReliefSprites(painted, null, BuildRelief(name + "_disabled", wDp, faceHDp, radiusDp, circle, family, 2, sliced), paintedWidthDp, h, faceHDp, sliced, true);
            }

            Sprite face = BuildRelief(name + "_face", wDp, faceHDp, radiusDp, circle, family, 0, sliced);
            Sprite baseSprite = BuildRelief(name + "_base", wDp, faceHDp, radiusDp, circle, family, 1, sliced);
            Sprite disabled = BuildRelief(name + "_disabled", wDp, faceHDp, radiusDp, circle, family, 2, sliced);
            return new ReliefSprites(face, baseSprite, disabled, wDp, heightDp, faceHDp, sliced, false);
        }

        // part: 0 cara, 1 base (sombra + canto + contorno), 2 cara deshabilitada (plana). Coordenadas en dp con y hacia
        // abajo: cara en [0, faceH], canto en [6, faceH + 6], sombra en [10, faceH + 10] con 6 dp de desenfoque.
        private Sprite BuildRelief(string name, float wDp, float faceHDp, float radiusDp, bool circle, ButtonFamily family, int part, bool sliced)
        {
            const float k = UiPxPerDp;
            float hDp = faceHDp + 14f;
            int w = Mathf.RoundToInt(wDp * k);
            int h = Mathf.RoundToInt(hDp * k);
            var pc = new PixelCanvas(w, h);
            float r = circle ? (wDp * 0.5f) - 1f : radiusDp;
            for (int y = 0; y < h; y++)
            {
                float yd = hDp - ((y + 0.5f) / k);
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    if (part == 1)
                    {
                        float shadow = Shape(xd, yd - 10f, wDp, faceHDp, r);
                        pc.Blend(x, y, new Color(0f, 0f, 0f, 0.30f), PixelCanvas.SoftCoverage(shadow * k, 6f * k));
                        float edge = Shape(xd, yd - ButtonEdgeDp, wDp, faceHDp, r);
                        float covEdge = PixelCanvas.Coverage(edge * k);
                        if (covEdge > 0f)
                        {
                            pc.Blend(x, y, family.Outline, covEdge);
                            float inner = PixelCanvas.Coverage((edge + 2f) * k);
                            Color e = Color.Lerp(family.Edge, ColorMath.Darken(family.Edge, 0.25f), Mathf.Clamp01((yd - faceHDp) / ButtonEdgeDp));
                            pc.Blend(x, y, e, inner);
                        }

                        continue;
                    }

                    float d = Shape(xd, yd, wDp, faceHDp, r);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    if (part == 2)
                    {
                        // Deshabilitado: plano, contorno de 1,5 dp (guía 5.1).
                        pc.Blend(x, y, Palette.DisabledOutline, cov);
                        pc.Blend(x, y, Palette.Disabled, PixelCanvas.Coverage((d + 1.5f) * k));
                        continue;
                    }

                    pc.Blend(x, y, family.Outline, cov);
                    float inside = PixelCanvas.Coverage((d + 2f) * k);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    float t = Mathf.Clamp01(yd / faceHDp);
                    Color c = family.FaceAt(t);

                    // Brillo interior de 1,5 dp en el borde superior interno (blanco 0,35).
                    float fromEdge = -(d + 2f);
                    if (fromEdge < 1.5f && yd < faceHDp * 0.5f)
                    {
                        c = Color.Lerp(c, Color.white, 0.35f * (1f - (fromEdge / 1.5f)));
                    }

                    // Lustre: elipse en el 6-28 % superior con 8 dp de margen lateral, blanco 0,25 → 0.
                    float lx0 = 8f;
                    float lx1 = wDp - 8f;
                    float ly0 = faceHDp * 0.06f;
                    float ly1 = faceHDp * 0.28f;
                    float lr = Mathf.Min((ly1 - ly0) * 0.5f, (lx1 - lx0) * 0.5f);
                    float ld = circle
                        ? Vector2.Distance(new Vector2(xd, yd), new Vector2(wDp * 0.5f, (ly0 + ly1) * 0.5f)) - (wDp * 0.30f)
                        : PixelCanvas.RoundedRectDistance(xd, yd, lx0, ly0, lx1, ly1, lr);
                    if (ld < 0f)
                    {
                        float fade = 1f - Mathf.Clamp01((yd - ly0) / Mathf.Max(0.01f, ly1 - ly0));
                        c = Color.Lerp(c, Color.white, 0.25f * fade * Mathf.Clamp01(-ld * 0.8f));
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, inside);
                }
            }

            float border = sliced ? (radiusDp + 4f) * k : 0f;
            return MakeSprite(pc, name, new Vector2(0.5f, 1f), new Vector4(border, 0f, border, 0f), k);
        }

        private static float Shape(float xd, float yd, float wDp, float faceHDp, float r)
        {
            return PixelCanvas.RoundedRectDistance(xd, yd, 1f, 1f, wDp - 1f, faceHDp - 1f, r);
        }

        // Placa de madera oscura con veta, bisel superior de 2 dp, ranura tallada a 4 dp del borde y sombra; con hojas de
        // 8 dp que asoman por los extremos si leaves (insignia de nivel, guía 3.5). sliceDp > 0: 9-slice con ese borde.
        private Sprite BuildPlate(string name, float wDp, float hDp, float radiusDp, bool leaves, float marginDp, float sliceDp = 0f)
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(wDp * k);
            int h = Mathf.RoundToInt(hDp * k);
            var pc = new PixelCanvas(w, h);
            float x0 = leaves ? 8f : 0.5f;
            float x1 = wDp - x0;
            float y0 = marginDp > 0f ? 2f : 0.5f;
            float y1 = hDp - (marginDp > 0f ? 6f : 0.5f);
            Color wood = Palette.WoodDark;
            if (leaves)
            {
                for (int side = 0; side < 2; side++)
                {
                    var basePx = new Vector2((side == 0 ? 14f : wDp - 14f) * k, (hDp - ((y0 + y1) * 0.5f)) * k);
                    var dir = new Vector2(side == 0 ? -1f : 1f, 0.35f).normalized;
                    DrawLeaf(pc, basePx, dir, 16f * k, 9f * k, 0.12f, LeafShape.Ovate, Palette.LeafDark, Palette.LeafLight, k, 300 + side);
                }
            }

            for (int y = 0; y < h; y++)
            {
                float yd = hDp - ((y + 0.5f) / k);
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    if (marginDp > 0f)
                    {
                        float sh = PixelCanvas.RoundedRectDistance(xd, yd - 3f, x0, y0, x1, y1, radiusDp);
                        pc.Blend(x, y, new Color(0f, 0f, 0f, 0.35f), PixelCanvas.SoftCoverage(sh * k, 4f * k));
                    }

                    float d = PixelCanvas.RoundedRectDistance(xd, yd, x0, y0, x1, y1, radiusDp);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float grain = ArtNoise.Value2(xd / 26f, yd * 0.55f, 211);
                    float line = Mathf.Abs(Mathf.Sin((yd * 0.75f) + (grain * 4f)));
                    Color c = ColorMath.Darken(wood, line < 0.14f ? 0.13f : 0f);
                    c = Color.Lerp(c, ColorMath.Lighten(wood, 0.10f), Mathf.Clamp01(1f - ((yd - y0) / ((y1 - y0) * 0.3f))) * 0.6f); // barniz
                    float fromEdge = -d;
                    if (fromEdge < 2f && yd < (y0 + y1) * 0.5f)
                    {
                        c = Color.Lerp(c, Palette.FamilyWood.Light, 0.7f * (1f - (fromEdge / 2f))); // bisel superior
                    }
                    else if (fromEdge < 1.2f)
                    {
                        c = Color.Lerp(c, Palette.Groove, 0.6f);
                    }

                    // Ranura tallada a 4 dp del borde: 1,5 dp oscura y 1 dp clara debajo.
                    if (fromEdge > 3.6f && fromEdge < 5.1f)
                    {
                        c = Color.Lerp(c, Palette.Groove, 0.55f);
                    }
                    else if (fromEdge >= 5.1f && fromEdge < 6.1f)
                    {
                        c = Color.Lerp(c, Palette.FamilyWood.Light, 0.30f);
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }

            Vector4 border = sliceDp > 0f ? new Vector4(sliceDp * k, sliceDp * k, sliceDp * k, sliceDp * k) : Vector4.zero;
            return MakeSprite(pc, name, new Vector2(0.5f, 0.5f), border, k);
        }

        private Sprite BuildTray(int widthDp)
        {
            // Bandeja «cumbuca» (guía 3.6): madera_oscura con veta, radio 24, rebaje interior, ribete de cuerda de 4 dp en
            // el canto superior y sombra proyectada (y + 4, desenfoque 8, 0,35). El sprite añade 8 dp a cada lado y 12 abajo.
            const float k = UiPxPerDp;
            float bodyW = widthDp;
            float wDp = bodyW + 16f;
            float hDp = 80f + 14f;
            int w = Mathf.RoundToInt(wDp * k);
            int h = Mathf.RoundToInt(hDp * k);
            var pc = new PixelCanvas(w, h);
            float x0 = 8f;
            float x1 = 8f + bodyW;
            const float y0 = 2f;
            const float y1 = 82f;
            for (int y = 0; y < h; y++)
            {
                float yd = hDp - ((y + 0.5f) / k);
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float sh = PixelCanvas.RoundedRectDistance(xd, yd - 4f, x0, y0, x1, y1, 24f);
                    pc.Blend(x, y, new Color(0f, 0f, 0f, 0.35f), PixelCanvas.SoftCoverage(sh * k, 8f * k));
                    float d = PixelCanvas.RoundedRectDistance(xd, yd, x0, y0, x1, y1, 24f);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float grain = ArtNoise.Value2(xd / 30f, yd * 0.5f, 223);
                    float line = Mathf.Abs(Mathf.Sin((yd * 0.7f) + (grain * 5f)));
                    Color c = ColorMath.Darken(Palette.WoodDark, line < 0.14f ? 0.14f : 0f);
                    c = Color.Lerp(ColorMath.Lighten(c, 0.08f), ColorMath.Darken(c, 0.15f), Mathf.Clamp01((yd - y0) / (y1 - y0)));

                    // Rebaje interior: sombra interior de 4 dp (negro 0,40) dentro de un hueco a 8 dp del borde.
                    float recess = PixelCanvas.RoundedRectDistance(xd, yd, x0 + 8f, y0 + 10f, x1 - 8f, y1 - 6f, 16f);
                    if (recess < 0f)
                    {
                        c = ColorMath.Darken(c, 0.18f);
                        float top = yd - (y0 + 10f);
                        float inset = Mathf.Min(-recess, top);
                        c = ColorMath.Darken(c, 0.40f * Mathf.Clamp01(1f - (inset / 4f)));
                    }
                    else if (recess < 1.2f)
                    {
                        c = Color.Lerp(c, Palette.FamilyWood.Light, 0.25f); // filo del rebaje
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, cov);

                    // Ribete de cuerda de 4 dp en el canto superior (hebras a 30°, tres tonos; guía 4.7).
                    float ropeD = Mathf.Abs(d + 3f) - 2f;
                    if (ropeD < 0.5f && yd < y0 + 26f)
                    {
                        float along = xd + (yd * 0.58f);
                        float strand = Mathf.Repeat(along / 4.8f, 1f);
                        Color rc = strand < 0.33f ? Palette.FiberLight : (strand < 0.7f ? Palette.FiberBase : Palette.FiberShadow);
                        pc.Blend(x, y, rc, PixelCanvas.Coverage(ropeD * k));
                    }
                }
            }

            return MakeSprite(pc, "tray_" + widthDp, new Vector2(0.5f, 1f), Vector4.zero, k);
        }

        private Sprite BuildModalPanel()
        {
            // 9-slice de 96 × 96 dp, borde de 32: marco de 8 dp en madera_oscura tallada y radio 24 (guía 5.3), con el
            // centro transparente: la superficie va aparte (ModalSurfaceFor), al tamaño exacto del panel.
            const float k = UiPxPerDp;
            const float sizeDp = 96f;
            int s = Mathf.RoundToInt(sizeDp * k);
            var pc = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
            {
                float yd = sizeDp - ((y + 0.5f) / k);
                for (int x = 0; x < s; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float d = PixelCanvas.RoundedRectDistance(xd, yd, 0.5f, 0.5f, sizeDp - 0.5f, sizeDp - 0.5f, 24f);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float inner = PixelCanvas.RoundedRectDistance(xd, yd, 8.5f, 8.5f, sizeDp - 8.5f, sizeDp - 8.5f, 16f);
                    bool horizontalBar = Mathf.Min(yd, sizeDp - yd) < Mathf.Min(xd, sizeDp - xd);
                    float across = horizontalBar ? yd : xd;
                    float grain = Mathf.Sin((across * 1.9f) + (Mathf.Sin(across * 0.6f) * 1.5f));
                    Color frame = ColorMath.Darken(Palette.WoodDark, grain > 0.85f ? 0.15f : 0f);
                    float fromOut = -d;
                    if (fromOut < 2f && yd < sizeDp * 0.5f)
                    {
                        frame = Color.Lerp(frame, Palette.FamilyWood.Light, 0.6f * (1f - (fromOut / 2f)));
                    }

                    if (fromOut > 3.5f && fromOut < 5f)
                    {
                        frame = Color.Lerp(frame, Palette.Groove, 0.6f);
                    }

                    frame.a = 1f;
                    float covSurface = PixelCanvas.Coverage(inner * k);
                    pc.Blend(x, y, frame, cov * (1f - covSurface));

                    // Sombra interior de 1,5 dp del canto superior sobre la superficie (se ve a través del centro vacío).
                    if (covSurface > 0f && -inner < 1.5f && yd < sizeDp * 0.5f)
                    {
                        pc.Blend(x, y, ColorMath.WithAlpha(ColorMath.Darken(Palette.PanelLightLow, 0.25f), 0.6f * (1f - (-inner / 1.5f))), covSurface);
                    }
                }
            }

            return MakeSprite(pc, "modal_panel", new Vector2(0.5f, 0.5f), new Vector4(32f * k, 32f * k, 32f * k, 32f * k), k);
        }

        private Sprite BuildModalSurface(int widthDp, int heightDp)
        {
            return MakeSprite(PaintModalSurface(widthDp, heightDp), ModalSurfaceName(widthDp, heightDp), new Vector2(0.5f, 0.5f), Vector4.zero, ModalSurfacePxPerDp);
        }

        private static string ModalSurfaceName(int widthDp, int heightDp)
        {
            return "modal_surface_" + widthDp + "x" + heightDp;
        }

        /// <summary>
        /// Píxeles de la superficie de <see cref="ModalSurfaceFor"/> de <paramref name="widthDp"/> × <paramref name="heightDp"/> dp
        /// (enteros), de abajo arriba, sin crear ninguna textura (tests: la veta que se pinta en otro hilo es la de siempre).
        /// </summary>
        public static Color32[] ModalSurfacePixels(int widthDp, int heightDp)
        {
            return PaintModalSurface(widthDp, heightDp).ToColor32();
        }

        // Los píxeles de la superficie (ver ModalSurfaceFor): solo cuentas con Color, Mathf y ArtNoise sobre un PixelCanvas
        // propio, así que también se pinta en otro hilo (el arte del banner de mundo, ProceduralArt.Mascots.cs).
        private static PixelCanvas PaintModalSurface(int widthDp, int heightDp)
        {
            const float k = ModalSurfacePxPerDp;
            int w = Mathf.RoundToInt(widthDp * k);
            int h = Mathf.RoundToInt(heightDp * k);
            var pc = new PixelCanvas(w, h);

            // Líneas de veta con periodo pseudoaleatorio de 7-11 dp (semilla fija), cada una con su ondulación.
            const int maxLines = 64;
            float[] lineY = new float[maxLines];
            int lines = 0;
            float yLine = 4f;
            while (yLine < heightDp && lines < maxLines)
            {
                lineY[lines++] = yLine;
                yLine += 7f + (ArtNoise.Hash(lines, 3, 557) * 4f);
            }

            // Periodo de la ondulación y tono de cada línea, una vez por línea (T52: antes, una vez por píxel y línea).
            float[] period = new float[maxLines];
            float[] tone = new float[maxLines];
            for (int i = 0; i < lines; i++)
            {
                period[i] = 40f + (ArtNoise.Hash(i, 5, 557) * 40f);
                tone[i] = Mathf.Lerp(0.94f, 0.96f, ArtNoise.Hash(i, 7, 557));
            }

            // En cada fila solo cuentan las líneas a menos de 1,2 dp + la ondulación (±1,5 dp): las demás nunca llegan a la
            // fila, así que se saltan sin cambiar ningún píxel (T52: la superficie de 328 × 281 dp costaba ≈ 220 ms en el
            // Editor recorriendo todas las líneas en cada píxel).
            int[] near = new int[maxLines];
            for (int y = 0; y < h; y++)
            {
                float yd = heightDp - ((y + 0.5f) / k); // dp desde arriba
                float t = Mathf.Clamp01(yd / heightDp);
                Color baseC = Color.Lerp(Palette.PanelLight, Palette.PanelLightLow, t);
                int nearCount = 0;
                for (int i = 0; i < lines; i++)
                {
                    if (Mathf.Abs(yd - lineY[i]) < 2.75f)
                    {
                        near[nearCount++] = i;
                    }
                }

                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float mult = 1f;
                    for (int n = 0; n < nearCount; n++)
                    {
                        int i = near[n];
                        float wave = 1.5f * Mathf.Sin((xd / period[i]) * Mathf.PI * 2f + i);
                        float d = Mathf.Abs(yd - (lineY[i] + wave));
                        if (d < 1.2f)
                        {
                            mult = Mathf.Min(mult, Mathf.Lerp(tone[i], 1f, d / 1.2f));
                        }
                    }

                    Color c = new Color(baseC.r * mult, baseC.g * mult, baseC.b * mult, 1f);

                    // Lustre del 8 % en la parte de arriba (barniz).
                    float lustre = Mathf.Clamp01(1f - (yd / Mathf.Max(12f, heightDp * 0.3f)));
                    c = Color.Lerp(c, Color.white, 0.08f * lustre);
                    pc.Set(x, y, c);
                }
            }

            return pc;
        }

        private Sprite BuildRibbon()
        {
            // Cinta-hoja (guía 5.5; revisión visual del 2026-10-07: 344 × 72 dp para Baloo 2 de 30): hoja verde horizontal
            // con extremos en punta; franja de texto de 256 × 42 dp en hoja_base → hoja_fondo (crema da 4,83-6,26:1); nervio
            // en el tercio superior, fuera del texto; luz de borde solo en el canto superior.
            const float k = UiPxPerDp;
            const float wDp = 344f;
            const float hDp = 72f;
            int w = Mathf.RoundToInt(wDp * k);
            int h = Mathf.RoundToInt(hDp * k);
            var pc = new PixelCanvas(w, h);
            const float bandX0 = 44f;
            const float bandX1 = 300f;
            const float bandY0 = 15f;
            const float bandY1 = 57f;
            ButtonFamily leaf = Palette.FamilyLeaf;
            for (int y = 0; y < h; y++)
            {
                float yd = hDp - ((y + 0.5f) / k);
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float xn = (xd / wDp * 2f) - 1f;
                    float half = 35f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Abs(xn), 4f)), 0.8f);
                    float midY = 37f + (2f * xn * xn); // ligera curva
                    float d = Mathf.Abs(yd - midY) - half;
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float v = (yd - (midY - half)) / Mathf.Max(0.01f, 2f * half); // 0 arriba, 1 abajo

                    // Todo el cuerpo de la hoja en hoja_base → hoja_fondo (sin un rectángulo de texto con cantos duros);
                    // hacia las puntas se funde con 24 dp de degradado al verde de hoja.
                    Color body = Color.Lerp(leaf.Base, leaf.Deep, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((yd - bandY0) / (bandY1 - bandY0))));
                    Color tip = Color.Lerp(Palette.LeafMid, Palette.LeafDark, Mathf.SmoothStep(0f, 1f, v));
                    float fx = Mathf.Min(xd - bandX0, bandX1 - xd); // > 0 dentro de la franja de texto
                    Color c = Color.Lerp(tip, body, Smooth(-24f, 0f, fx));
                    bool inBand = xd > bandX0 && xd < bandX1 && yd > bandY0 && yd < bandY1;
                    if (inBand)
                    {
                        c = body;
                    }
                    else
                    {
                        // Nervio en el tercio superior, fuera de la franja de texto, y nervaduras laterales.
                        float rib = Mathf.Abs(yd - (midY - (half * 0.62f)));
                        if (rib < 0.9f && Mathf.Abs(xn) < 0.92f)
                        {
                            c = Color.Lerp(c, Palette.Vein, 0.6f);
                        }

                        float lateral = Mathf.Repeat((xd + ((yd - midY) * 0.9f * Mathf.Sign(xn))) / 11f, 1f);
                        if ((lateral < 0.07f) && (xd < bandX0 || xd > bandX1 || yd > bandY1))
                        {
                            c = Color.Lerp(c, Palette.Vein, 0.22f);
                        }
                    }

                    // Luz de borde solo en el canto superior.
                    if (yd < midY && -d < 1.5f)
                    {
                        c = Color.Lerp(c, Palette.LeafShine, 0.85f);
                    }
                    else if (yd > midY && -d < 1.2f)
                    {
                        c = ColorMath.Darken(c, 0.35f);
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }

            return MakeSprite(pc, "ribbon_leaf", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        private Sprite BuildStillWater()
        {
            // Agua quieta (guía 5.3): degradado rio_base → rio_profundo, 3 reflejos horizontales y 1 hoja flotando.
            const float k = UiPxPerDp;
            const float wDp = 296f;
            const float hDp = 20f;
            int w = Mathf.RoundToInt(wDp * k);
            int h = Mathf.RoundToInt(hDp * k);
            var pc = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                float yd = hDp - ((y + 0.5f) / k);
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float d = PixelCanvas.RoundedRectDistance(xd, yd, 0.5f, 0.5f, wDp - 0.5f, hDp - 0.5f, 6f);
                    Color c = Color.Lerp(Palette.RiverBase, Palette.RiverDeep, yd / hDp);
                    c.a = 1f;
                    pc.Blend(x, y, c, PixelCanvas.Coverage(d * k));
                }
            }

            pc.FillRoundedRectSoft(40f * k, 13f * k, 120f * k, 14.2f * k, 0.6f * k, 2f, new Color(1f, 0.95f, 0.85f, 0.35f));
            pc.FillRoundedRectSoft(150f * k, 8f * k, 210f * k, 9f * k, 0.5f * k, 2f, new Color(1f, 0.95f, 0.85f, 0.30f));
            pc.FillRoundedRectSoft(228f * k, 14f * k, 270f * k, 15f * k, 0.5f * k, 2f, new Color(1f, 0.95f, 0.85f, 0.28f));
            DrawLeaf(pc, new Vector2(190f * k, 6f * k), new Vector2(1f, 0.15f).normalized, 14f * k, 7f * k, 0.1f, LeafShape.Ovate, Palette.LeafMid, Palette.LeafLight, k, 401);
            return MakeSprite(pc, "still_water", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        private Sprite BuildScrim()
        {
            var pc = new PixelCanvas(4, 64);
            Color s = Palette.Scrim;
            for (int y = 0; y < 64; y++)
            {
                float t = (y + 0.5f) / 64f; // 0 abajo, 1 arriba
                for (int x = 0; x < 4; x++)
                {
                    pc.Set(x, y, new Color(s.r, s.g, s.b, 0.35f * t * t));
                }
            }

            return MakeSprite(pc, "status_scrim", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildIconUndo(Color glyph, Color shadow, string name)
        {
            // Ola que retrocede con flecha curva a la izquierda (≥ 40 % del glifo; 07 §7.6). Silueta sólida con sombra
            // interior de 1 dp del canto de la familia (guía 5.7). 32 dp a 3 px/dp.
            var pc = new PixelCanvas(96, 96);
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 0 && shadow.a <= 0f)
                {
                    continue;
                }

                Color c = pass == 0 ? shadow : glyph;
                float oy = pass == 0 ? -3f : 0f;
                DrawWave(pc, 14f, 82f, 24f + oy, 5f, 34f, 5f, c);
                DrawArrowArc(pc, new Vector2(48f, 44f + oy), 24f, 9f, 15f, 168f, c);
            }

            return MakeSprite(pc, name, new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private Sprite BuildIconRestart(Color glyph, Color shadow)
        {
            // Flecha circular con ola (07 §7.6), 26 dp a 3 px/dp, silueta sólida en crema. Arco de 300° con la punta bien
            // marcada arriba a la derecha y la ola como línea de base 1,3 veces más ancha que el círculo, separada 2 dp:
            // con una ola corta bajo el círculo el glifo se leía como ♀ (revisión visual del 2026-10-07, VA-11).
            var pc = new PixelCanvas(78, 78);
            for (int pass = 0; pass < 2; pass++)
            {
                Color c = pass == 0 ? shadow : glyph;
                float oy = pass == 0 ? -3f : 0f;
                DrawArrowArc(pc, new Vector2(39f, 47f + oy), 17f, 7f, 100f, 400f, c, 1.45f);
                DrawWave(pc, 11f, 67f, 12f + oy, 3f, 19f, 3.3f, c);
            }

            return MakeSprite(pc, "icon_restart", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }
    }
}
