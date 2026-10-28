#nullable enable
using System;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>El fondo de un retrato de avatar (avatares.md §2.1): una de las cuatro familias de la UI, nunca un tinte.</summary>
    public enum AvatarArtBackdrop
    {
        Leaf = 0,
        Water = 1,
        Wood = 2,
        Petroleum = 3,
    }

    /// <summary>El icono de la chapa de un avatar bloqueado (avatares.md §2.1): el del hito que lo desbloquea.</summary>
    public enum AvatarArtBadge
    {
        None = 0,

        /// <summary>Mundo: el banderín de la parada.</summary>
        World = 1,

        /// <summary>Difícil: el glifo del chip (la rana).</summary>
        Hard = 2,

        /// <summary>Muy difícil: el glifo del chip (la cabeza de jaguar).</summary>
        VeryHard = 3,

        /// <summary>Crecida: la regla con agua.</summary>
        Streak = 4,

        /// <summary>Reto del río: el disco con olas.</summary>
        Daily = 5,

        /// <summary>Liga: el podio.</summary>
        League = 6,
    }

    /// <summary>
    /// Retratos de los avatares del jugador por código (D-056 y D-052; <c>docs/dev/avatares.md</c> §2): 16 animales de busto en un disco
    /// de fondo (una familia de la UI) con hojas ovadas al pie, de juguete brillante (D-058: el volumen en tres tonos, el barniz, el
    /// borde fino del color de cada parte en lugar del contorno de tinta de Capi y los ojos con dos reflejos), proporciones de adulto y un aro de madera
    /// con filete crema (el sitio del marco de la 1.2). Bloqueado: el fondo desaturado al 40 %, el animal en silueta sólida del
    /// <c>_contorno</c> de su familia (alfa 0,85, sin ojos ni brillos), el aro gris y la chapa de madera con el icono del hito. El
    /// disco neutro (sin avatar o un índice desconocido): Hoja con una hoja ovada crema. El PNG del artista manda por la ranura
    /// <c>avatar_&lt;id&gt;</c> (<see cref="ArtSlots.AvatarPrefix"/>) para el retrato normal; el bloqueado sale siempre por código.
    /// <para>Cada parte es una forma con distancia con signo (elipse, cápsula o polígono) en unidades del disco (radio interior = 1, y
    /// hacia arriba), pintada con su tinta y su relleno con un sombreado vertical suave; todo se recorta al disco salvo la chapa.</para>
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Retratos del catálogo (índices 0-15, avatares.md §1.2).</summary>
        public const int AvatarArtCount = 16;

        /// <summary>Índice del disco neutro.</summary>
        public const int AvatarNeutral = -1;

        /// <summary>Radio del disco de fondo en fracción del radio del retrato: el aro ocupa el 7 % del diámetro.</summary>
        public const float AvatarDiscFraction = 0.86f;

        /// <summary>Diámetro de la chapa del hito en fracción del retrato (avatares.md §2.1: 30 %).</summary>
        public const float AvatarBadgeFraction = 0.30f;

        /// <summary>Alfa de la silueta de un bloqueado.</summary>
        public const float AvatarSilhouetteAlpha = 0.85f;

        private static readonly Color AvInk = Palette.Hex(0x2B2118);
        private static readonly Color AvRingEdge = Palette.Hex(0x2E2018);
        private static readonly Color AvRingLight = Palette.Hex(0xBF8F62);
        private static readonly Color AvRingBase = Palette.Hex(0x7E5638);
        private static readonly Color AvPetroleumLight = Palette.Hex(0x23626A);
        private static readonly Color AvPetroleumDeep = Palette.Hex(0x14444A);
        private static readonly Color AvPetroleumOutline = Palette.Hex(0x0A2A2E);

        private static readonly AvPart[][] AvAnimals = BuildAvAnimals();
        private static readonly AvPart[] AvLeaves = BuildAvLeaves();
        private static readonly AvPart[] AvNeutral = BuildAvNeutral();
        private static readonly AvPart[][] AvBadges = BuildAvBadges();

        private Sprite? _avatarPortrait;
        private string? _avatarPortraitKey;

        /// <summary>
        /// El retrato elegido (avatares.md §2.2): <paramref name="px"/> × <paramref name="px"/> con mipmaps, para el inicio (40 dp), la
        /// línea de la victoria (24 dp) y la cabecera de «Tu avatar» (112 dp). El PNG <c>avatar_&lt;id&gt;</c> si existe; si no, por
        /// código. Se rehace solo al cambiar de avatar o de tamaño (el anterior se destruye).
        /// </summary>
        public Sprite AvatarPortrait(string id, int index, AvatarArtBackdrop backdrop, int px)
        {
            Sprite? png = ArtSlots.Find(AvatarSlot(id));
            if (png != null)
            {
                return png;
            }

            px = Mathf.Clamp(px, 16, 1024);
            string key = id + "@" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "@" + px.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (_avatarPortrait != null && string.Equals(_avatarPortraitKey, key, StringComparison.Ordinal))
            {
                return _avatarPortrait;
            }

            ForgetAvatarPortrait();
            var pc = new PixelCanvas(px, px);
            var scratch = new PixelCanvas(px, px);
            PaintAvatar(pc, scratch, index, backdrop, AvatarArtBadge.None, false);
            scratch.Release();
            Texture2D tex = pc.ToTexture("avatar_" + id, false, false, true);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = "avatar_" + id;
            sprite.hideFlags = HideFlags.DontSave;
            _avatarPortrait = sprite;
            _avatarPortraitKey = key;
            return sprite;
        }

        /// <summary>Destruye el retrato elegido (al rehacerlo y al cerrar la escena).</summary>
        public void ForgetAvatarPortrait()
        {
            if (_avatarPortrait == null)
            {
                return;
            }

            Texture2D tex = _avatarPortrait.texture;
            DestroyNow(_avatarPortrait);
            DestroyNow(tex);
            _avatarPortrait = null;
            _avatarPortraitKey = null;
        }

        private Sprite? _avatarChevron;
        private Sprite? _avatarCheck;

        /// <summary>Flecha «›» blanca (se tiñe) de la fila «Avatar» de Ajustes, 24 × 24 dp.</summary>
        public Sprite AvatarChevron => _avatarChevron ??= BuildAvatarGlyph(false);

        /// <summary>Marca «✓» blanca (se tiñe) de la chapa del avatar elegido, 24 × 24 dp.</summary>
        public Sprite AvatarCheck => _avatarCheck ??= BuildAvatarGlyph(true);

        private Sprite BuildAvatarGlyph(bool check)
        {
            const float k = UiPxPerDp;
            int n = Mathf.RoundToInt(24f * k);
            var pc = new PixelCanvas(n, n);
            float r = 1.6f * k;
            if (check)
            {
                pc.FillCapsule(6f * k, 12.5f * k, 10f * k, 8f * k, r, Color.white);
                pc.FillCapsule(10f * k, 8f * k, 18f * k, 17f * k, r, Color.white);
            }
            else
            {
                pc.FillCapsule(9f * k, 18f * k, 15f * k, 12f * k, r, Color.white);
                pc.FillCapsule(15f * k, 12f * k, 9f * k, 6f * k, r, Color.white);
            }

            return MakeSprite(pc, check ? "avatar_check" : "avatar_chevron", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        /// <summary>Nombre de la ranura del PNG del avatar <paramref name="id"/> (identificador, no texto visible).</summary>
        public static string AvatarSlot(string id) => ArtSlots.AvatarPrefix + id;

        /// <summary>Un atlas perezoso de celdas de <paramref name="cellPx"/> px (la rejilla de «Tu avatar» y las filas de la Liga).</summary>
        public static AvatarAtlas CreateAvatarAtlas(int cellPx) => new AvatarAtlas(cellPx);

        /// <summary>
        /// Los píxeles de un retrato de <paramref name="px"/> × <paramref name="px"/> (tests y exportación a <c>Logs/art/</c>; abajo a la
        /// izquierda primero, como <see cref="Texture2D"/>).
        /// </summary>
        public static Color32[] AvatarPixels(int index, AvatarArtBackdrop backdrop, AvatarArtBadge badge, bool locked, int px)
        {
            var pc = new PixelCanvas(px, px);
            var scratch = new PixelCanvas(px, px);
            PaintAvatar(pc, scratch, index, backdrop, badge, locked);
            scratch.Release();
            return pc.TakeColor32();
        }

        /// <summary>
        /// Pinta el retrato <paramref name="index"/> (0-15, o <see cref="AvatarNeutral"/>) en <paramref name="pc"/> entero (cuadrado),
        /// que debe venir transparente. <paramref name="scratch"/>, del mismo tamaño, guarda la silueta de un bloqueado (se limpia aquí).
        /// No asigna memoria.
        /// </summary>
        internal static void PaintAvatar(PixelCanvas pc, PixelCanvas scratch, int index, AvatarArtBackdrop backdrop, AvatarArtBadge badge, bool locked)
        {
            int n = pc.Width;
            float half = n * 0.5f;
            float k = half * AvatarDiscFraction; // px por unidad del disco
            var frame = new AvFrame(half, half, k, n);
            bool neutral = index < 0 || index >= AvatarArtCount;
            AvatarArtBackdrop bg = neutral ? AvatarArtBackdrop.Leaf : backdrop;
            BackdropColors(bg, out Color light, out Color deep, out Color outline);
            if (locked)
            {
                light = Desaturate(light, 0.4f);
                deep = Desaturate(deep, 0.4f);
            }

            PaintDisc(pc, frame, light, deep);
            Color leaf = Color.Lerp(deep, Color.black, 0.28f);
            leaf.a = 1f;
            for (int i = 0; i < AvLeaves.Length; i++)
            {
                DrawPart(pc, frame, AvLeaves[i], leaf, false, true, 0.12f);
            }

            if (neutral)
            {
                for (int i = 0; i < AvNeutral.Length; i++)
                {
                    DrawPart(pc, frame, AvNeutral[i], AvNeutral[i].Fill, AvNeutral[i].Ink, true, AvNeutral[i].Shade);
                }
            }
            else
            {
                AvPart[] parts = AvAnimals[index];
                if (!locked)
                {
                    for (int i = 0; i < parts.Length; i++)
                    {
                        DrawToyPart(pc, frame, parts[i]);
                    }
                }
                else
                {
                    scratch.Fill(Color.clear);
                    Color sil = outline;
                    sil.a = 1f;
                    for (int i = 0; i < parts.Length; i++)
                    {
                        AvPart p = parts[i];
                        if (!p.Detail)
                        {
                            DrawPart(scratch, frame, p, sil, false, true, 0f);
                        }
                    }

                    for (int y = 0; y < n; y++)
                    {
                        for (int x = 0; x < n; x++)
                        {
                            Color c = scratch.Get(x, y);
                            if (c.a > 0f)
                            {
                                pc.Blend(x, y, new Color(c.r, c.g, c.b, 1f), c.a * AvatarSilhouetteAlpha);
                            }
                        }
                    }
                }
            }

            PaintRing(pc, frame, locked);
            if (locked && badge != AvatarArtBadge.None)
            {
                PaintBadge(pc, frame, badge);
            }
        }

        /// <summary>
        /// La silueta del animal <paramref name="index"/> en un retrato de <paramref name="px"/> px (las partes sin detalle, recortadas al
        /// disco, como la del bloqueado): <paramref name="mask"/>[y · px + x] es la cobertura (0..1). Para las pruebas de legibilidad a
        /// 24 y 32 dp. Sin asignar más que su lienzo.
        /// </summary>
        internal static void AvatarSilhouetteMask(int index, int px, float[] mask)
        {
            var scratch = new PixelCanvas(px, px);
            float half = px * 0.5f;
            var frame = new AvFrame(half, half, half * AvatarDiscFraction, px);
            AvPart[] parts = AvAnimals[index];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!parts[i].Detail)
                {
                    DrawPart(scratch, frame, parts[i], Color.white, false, true, 0f);
                }
            }

            for (int y = 0; y < px; y++)
            {
                for (int x = 0; x < px; x++)
                {
                    mask[(y * px) + x] = scratch.Get(x, y).a;
                }
            }

            scratch.Release();
        }

        private static void BackdropColors(AvatarArtBackdrop bg, out Color light, out Color deep, out Color outline)
        {
            switch (bg)
            {
                case AvatarArtBackdrop.Water:
                    light = Palette.FamilyWater.Base;
                    deep = Palette.FamilyWater.Deep;
                    outline = Palette.FamilyWater.Outline;
                    break;
                case AvatarArtBackdrop.Wood:
                    light = Palette.FamilyWood.Base;
                    deep = Palette.FamilyWood.Deep;
                    outline = Palette.FamilyWood.Outline;
                    break;
                case AvatarArtBackdrop.Petroleum:
                    light = AvPetroleumLight;
                    deep = AvPetroleumDeep;
                    outline = AvPetroleumOutline;
                    break;
                default:
                    light = Palette.FamilyLeaf.Base;
                    deep = Palette.FamilyLeaf.Deep;
                    outline = Palette.FamilyLeaf.Outline;
                    break;
            }
        }

        private static Color Desaturate(Color c, float amount)
        {
            float l = (c.r * 0.299f) + (c.g * 0.587f) + (c.b * 0.114f);
            return new Color(Mathf.Lerp(c.r, l, amount), Mathf.Lerp(c.g, l, amount), Mathf.Lerp(c.b, l, amount), c.a);
        }

        // Disco de fondo: degradado radial suave, más claro arriba a la izquierda (la luz de juego). Solo recorre, fila a fila, el tramo
        // que puede tocar el disco (con un margen: la cobertura se sigue mirando píxel a píxel) y escribe los opacos en el búfer (el
        // retrato de 192 px debe entrar en 8 ms en el Editor: 2026-10-27); la mezcla es la de Color.Lerp, escrita a mano.
        private static void PaintDisc(PixelCanvas pc, in AvFrame f, Color light, Color deep)
        {
            float rIn = f.K;
            int n = f.N;
            float cx = f.Cx;
            float cy = f.Cy;
            float lim = rIn + 1.5f;
            float lim2 = lim * lim;
            float dr = deep.r - light.r;
            float dg = deep.g - light.g;
            float db = deep.b - light.b;
            float da = deep.a - light.a;
            Color[] buf = pc.Pixels;
            int width = pc.Width;
            for (int y = 0; y < n; y++)
            {
                float dy = (y + 0.5f) - cy;
                float h2 = lim2 - (dy * dy);
                if (h2 <= 0f)
                {
                    continue;
                }

                float h = Mathf.Sqrt(h2);
                int xa = Mathf.Max(0, Mathf.FloorToInt(cx - h - 1f));
                int xb = Mathf.Min(n - 1, Mathf.CeilToInt(cx + h));
                float dy2 = dy * dy;
                float ly = (dy / rIn) - 0.35f;
                float ly2 = ly * ly;
                int row = y * width;
                for (int x = xa; x <= xb; x++)
                {
                    float dx = (x + 0.5f) - cx;
                    float cov = 0.5f - (Mathf.Sqrt((dx * dx) + dy2) - rIn);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float lx = (dx / rIn) + 0.25f;
                    float t = Mathf.Sqrt((lx * lx) + ly2) / 1.45f;
                    t = t < 0f ? 0f : (t > 1f ? 1f : t);
                    float s = t * t * (3f - (2f * t));
                    var c = new Color(light.r + (dr * s), light.g + (dg * s), light.b + (db * s), light.a + (da * s));
                    if (cov >= 1f)
                    {
                        buf[row + x] = c;
                    }
                    else
                    {
                        pc.Blend(x, y, c, cov);
                    }
                }
            }
        }

        // Aro de madera (canto oscuro, luz en el centro) con el filete crema por dentro; bloqueado, gris deshabilitado.
        private static void PaintRing(PixelCanvas pc, in AvFrame f, bool locked)
        {
            float rIn = f.K;
            float rOut = f.N * 0.5f;
            float filet = Mathf.Max(1.2f, f.N * 0.014f);
            float skip = Mathf.Max(0f, rIn - filet - 1.5f);
            float skip2 = skip * skip;
            Color edge = locked ? Color.Lerp(Palette.DisabledOutline, Color.black, 0.35f) : AvRingEdge;
            Color mid = locked ? Palette.DisabledOutline : AvRingBase;
            Color shine = locked ? Color.Lerp(Palette.DisabledOutline, Color.white, 0.25f) : AvRingLight;
            Color filetColor = locked ? Color.Lerp(Palette.Cream, Palette.DisabledOutline, 0.45f) : (Color)Palette.Cream;
            float shineR = shine.r - mid.r;
            float shineG = shine.g - mid.g;
            float shineB = shine.b - mid.b;
            float shineA = shine.a - mid.a;
            // Fila a fila, solo el tramo dentro del canto de fuera y sin el hueco del disco (con un margen de un píxel: los
            // dos límites se siguen mirando píxel a píxel).
            float lim = rOut + 1f;
            float lim2 = lim * lim;
            for (int y = 0; y < f.N; y++)
            {
                float dy = (y + 0.5f) - f.Cy;
                float dy2 = dy * dy;
                float h2 = lim2 - dy2;
                if (h2 <= 0f)
                {
                    continue;
                }

                float h = Mathf.Sqrt(h2);
                int xa = Mathf.Max(0, Mathf.FloorToInt(f.Cx - h - 1f));
                int xb = Mathf.Min(f.N - 1, Mathf.CeilToInt(f.Cx + h));
                int xs0 = int.MaxValue;
                int xs1 = int.MinValue;
                float hs2 = skip2 - dy2;
                if (hs2 > 0f)
                {
                    float hs = Mathf.Sqrt(hs2);
                    xs0 = Mathf.CeilToInt(f.Cx - hs + 0.5f);
                    xs1 = Mathf.FloorToInt(f.Cx + hs - 1.5f);
                }

                for (int x = xa; x <= xb; x++)
                {
                    if (x == xs0 && xs0 <= xs1)
                    {
                        x = xs1;
                        continue; // el disco: el aro empieza en el filete
                    }

                    float dx = (x + 0.5f) - f.Cx;
                    float r2 = (dx * dx) + dy2;
                    if (r2 < skip2)
                    {
                        continue; // el disco: el aro empieza en el filete
                    }

                    float r = Mathf.Sqrt(r2);
                    float outer = PixelCanvas.Coverage(r - rOut + 0.5f);
                    float inner = PixelCanvas.Coverage(r - (rIn - filet)); // 1 dentro del hueco del filete
                    float cov = Mathf.Min(outer, 1f - inner);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    if (r < rIn)
                    {
                        pc.Blend(x, y, filetColor, cov);
                        continue;
                    }

                    float u = Mathf.Clamp01((r - rIn) / Mathf.Max(1f, rOut - rIn));
                    float band = 1f - Mathf.Abs((u * 2f) - 1f); // 0 en los cantos, 1 en el centro
                    float lightK = Mathf.Clamp01(0.5f + (0.5f * ((-dx + dy) / Mathf.Max(1f, r) * 0.7071f))); // arriba a la izquierda
                    // Las dos mezclas de Color.Lerp, escritas a mano (sus t ya están en 0..1).
                    float t1 = lightK * 0.75f;
                    float br = mid.r + (shineR * t1);
                    float bg = mid.g + (shineG * t1);
                    float bb = mid.b + (shineB * t1);
                    float ba = mid.a + (shineA * t1);
                    float t2 = band * 2.2f;
                    t2 = t2 < 0f ? 0f : (t2 > 1f ? 1f : t2);
                    var c = new Color(edge.r + ((br - edge.r) * t2), edge.g + ((bg - edge.g) * t2), edge.b + ((bb - edge.b) * t2), edge.a + ((ba - edge.a) * t2));
                    pc.Blend(x, y, c, cov);
                }
            }
        }

        // La chapa de madera del hito abajo a la derecha, con su icono en crema (sin recortar al disco).
        private static void PaintBadge(PixelCanvas pc, in AvFrame f, AvatarArtBadge badge)
        {
            float radiusPx = f.N * AvatarBadgeFraction * 0.5f;
            float bcx = f.Cx + (f.K * 0.62f);
            float bcy = f.Cy - (f.K * 0.62f);
            float ink = Mathf.Max(1.25f, f.N / 96f);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(bcx - radiusPx - ink - 1f));
            int x1 = Mathf.Min(f.N - 1, Mathf.CeilToInt(bcx + radiusPx + ink + 1f));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(bcy - radiusPx - ink - 1f));
            int y1 = Mathf.Min(f.N - 1, Mathf.CeilToInt(bcy + radiusPx + ink + 1f));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f) - bcx;
                    float dy = (y + 0.5f) - bcy;
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy)) - radiusPx;
                    pc.Blend(x, y, AvRingEdge, PixelCanvas.Coverage(d - ink));
                    float t = Mathf.Clamp01(0.5f + ((dy - dx) / (radiusPx * 2.6f)));
                    pc.Blend(x, y, Color.Lerp(AvRingBase, AvRingLight, t), PixelCanvas.Coverage(d));
                }
            }

            int b = (int)badge;
            if (b <= 0 || b >= AvBadges.Length)
            {
                return;
            }

            var icon = new AvFrame(bcx, bcy, radiusPx * 0.62f, f.N);
            AvPart[] parts = AvBadges[b];
            for (int i = 0; i < parts.Length; i++)
            {
                DrawPart(pc, icon, parts[i], parts[i].Fill, false, false, 0f);
            }
        }

        // Una parte en el marco f: la tinta (si lleva) y el relleno con su sombreado vertical; recortada al disco con clip.
        private static void DrawPart(PixelCanvas pc, in AvFrame f, AvPart p, Color fill, bool ink, bool clip, float shade)
        {
            float inkPx = ink ? Mathf.Max(1.25f, f.N / 96f) : 0f;
            float pad = inkPx + 1.5f;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(f.Cx + (p.MinX * f.K) - pad));
            int x1 = Mathf.Min(f.N - 1, Mathf.CeilToInt(f.Cx + (p.MaxX * f.K) + pad));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(f.Cy + (p.MinY * f.K) - pad));
            int y1 = Mathf.Min(f.N - 1, Mathf.CeilToInt(f.Cy + (p.MaxY * f.K) + pad));
            float invK = 1f / f.K;
            float insideR = Mathf.Max(0f, 1f - (1f / f.K));
            float inside2 = insideR * insideR; // dentro de este radio el recorte del disco no tapa nada
            float spanY = Mathf.Max(1e-4f, p.MaxY - p.MinY);
            for (int y = y0; y <= y1; y++)
            {
                float uy = ((y + 0.5f) - f.Cy) * invK;
                Color shaded = fill;
                if (shade > 0f)
                {
                    float t = Mathf.Clamp01((uy - p.MinY) / spanY);
                    float m = 1f - (shade * 0.6f) + (shade * t);
                    shaded = new Color(Mathf.Clamp01(fill.r * m), Mathf.Clamp01(fill.g * m), Mathf.Clamp01(fill.b * m), fill.a);
                }

                for (int x = x0; x <= x1; x++)
                {
                    float ux = ((x + 0.5f) - f.Cx) * invK;
                    float clipCov = 1f;
                    if (clip)
                    {
                        float r2 = (ux * ux) + (uy * uy);
                        if (r2 > inside2)
                        {
                            clipCov = PixelCanvas.Coverage((Mathf.Sqrt(r2) - 1f) * f.K);
                            if (clipCov <= 0f)
                            {
                                continue;
                            }
                        }
                    }

                    float d = Sdf(p, ux, uy) * f.K;
                    if (d > inkPx + 1f)
                    {
                        continue; // fuera de la tinta y del relleno
                    }

                    if (d <= -0.5f && clipCov >= 1f && shaded.a >= 1f)
                    {
                        pc.Set(x, y, shaded); // dentro del todo: el relleno opaco tapa la tinta
                        continue;
                    }

                    if (ink)
                    {
                        float ci = PixelCanvas.Coverage(d - inkPx) * clipCov;
                        if (ci > 0f)
                        {
                            pc.Blend(x, y, AvInk, ci);
                        }
                    }

                    float cf = PixelCanvas.Coverage(d) * clipCov;
                    if (cf > 0f)
                    {
                        pc.Blend(x, y, shaded, cf);
                    }
                }
            }
        }

        // Juguete de los avatares (D-058): el borde fino coloreado (px del retrato: ≈ 1 px a 32 dp) y el barniz de cada clase de
        // parte (las formas del animal, lacadas; los parches sin borde y las de color plano, algo menos).
        private const float AvToyGloss = 0.6f;
        private const float AvToyPatchGloss = 0.45f;
        private const float AvToyFlatGloss = 0.3f;

        // Celdas por eje de la tabla de sombreado de cada parte (ToyLut, 25² normales en bytes: unos 1,9 KB por parte; los 16
        // avatares, ≈ 360 KB, solo de los que se pintan). Se rellena la primera vez que la parte se pinta (≈ 625 cuentas).
        private const int AvLutSize = 24;

        /// <summary>
        /// Crea (vacías) las tablas de sombreado de las partes del avatar <paramref name="index"/>: al pedir la celda, no al pintar,
        /// así que pintar no asigna memoria. Se rellenan al pintar la primera vez.
        /// </summary>
        internal static void EnsureAvatarLuts(int index)
        {
            if (index < 0 || index >= AvatarArtCount)
            {
                return;
            }

            AvPart[] parts = AvAnimals[index];
            for (int i = 0; i < parts.Length; i++)
            {
                AvPart p = parts[i];
                if (!p.Detail && p.Lut == null) // los ojos y los detalles no la usan
                {
                    p.Lut = ToyLut.Create(AvLutSize);
                }
            }
        }

        // Una parte de un retrato normal de juguete (D-058), recortada al disco: el borde fino del color oscuro de la parte (en
        // lugar de la tinta), el relleno con el volumen de su forma (una cúpula para la elipse, un cilindro con las puntas en
        // cúpula para la cápsula y un cojín desde el borde para el polígono) y el barniz; los ojos, con el iris en degradado; los
        // detalles (pupilas, brillos, marcas), planos como antes. Sin asignar memoria: los tonos se calculan al crear las tablas.
        private static void DrawToyPart(PixelCanvas pc, in AvFrame f, AvPart p)
        {
            if (p.Detail && !p.Ink)
            {
                DrawPart(pc, f, p, p.Fill, false, true, 0f);
                return;
            }

            bool eye = p.Detail && p.Ink;
            float edgePx = p.Ink ? Mathf.Max(0.9f, f.N / 150f) : 0f;
            float gloss = p.Ink ? AvToyGloss : (p.Shade > 0f ? AvToyPatchGloss : AvToyFlatGloss);
            float bulge = p.Ink ? 1f : 0.6f; // los parches sin borde, más planos: siguen a la pieza de debajo
            float pad = edgePx + 1.5f;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(f.Cx + (p.MinX * f.K) - pad));
            int x1 = Mathf.Min(f.N - 1, Mathf.CeilToInt(f.Cx + (p.MaxX * f.K) + pad));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(f.Cy + (p.MinY * f.K) - pad));
            int y1 = Mathf.Min(f.N - 1, Mathf.CeilToInt(f.Cy + (p.MaxY * f.K) + pad));
            float invK = 1f / f.K;
            float insideR = Mathf.Max(0f, 1f - (1f / f.K));
            float inside2 = insideR * insideR;
            float pcx = (p.MinX + p.MaxX) * 0.5f;
            float pcy = (p.MinY + p.MaxY) * 0.5f;
            float phx = Mathf.Max(1e-4f, (p.MaxX - p.MinX) * 0.5f);
            float phy = Mathf.Max(1e-4f, (p.MaxY - p.MinY) * 0.5f);
            float pillow = 0.45f * Mathf.Min(phx, phy);
            // La tabla de la parte: creada al pedir la celda (EnsureAvatarLuts), así que pintar no asigna, y rellena la primera
            // vez que se pinta. Los avatares se pintan en el hilo principal (el atlas y el retrato); aun así, Fill publica la
            // tabla con una barrera (Volatile) y dos hilos que la rellenen a la vez escriben lo mismo.
            ToyLut? lut = null;
            if (!eye)
            {
                lut = p.Lut ??= ToyLut.Create(AvLutSize);
                if (!lut.Ready)
                {
                    lut.Fill(p.Toy, gloss);
                }
            }

            // Fila a fila, solo el tramo que puede tocar el disco (con margen: el recorte se sigue mirando píxel a píxel) y con lo que
            // no depende de x calculado una vez por fila. La distancia y la normal de la elipse y de la cápsula van aquí escritas con
            // las mismas cuentas que Sdf y ToyNormal (el mismo dibujo: a lo sumo 0,002/255 de redondeo por canal) y los píxeles opacos, directos al búfer: el
            // retrato de 192 px tenía que bajar de 8 ms en el Editor x64 (2026-10-27).
            int mode = eye || p.Kind == AvKind.Poly ? 0 : (p.Kind == AvKind.Ellipse ? 1 : 2);
            float k = f.K;
            float cx = f.Cx;
            float cy = f.Cy;
            float clipLim = 1f + (2f / k); // más allá de 1 + 0,5 px el recorte del disco no deja nada
            float clipLim2 = clipLim * clipLim;
            float maxD = edgePx + 1f;
            float pa = p.A;
            float pb = p.B;
            float pcos = p.Cos;
            float psin = p.Sin;
            bool rotated = pcos != 1f || psin != 0f;
            float ec = p.C;
            float ed = p.D;
            float ecc = ec * ec;
            float edd = ed * ed;
            float ec106 = ec * 1.06f;
            float ed106 = ed * 1.06f;
            float eMin = Mathf.Min(ec, ed);
            float vx = p.C - p.A;
            float vy = p.D - p.B;
            float vv = Mathf.Max(1e-6f, (vx * vx) + (vy * vy));
            float capR = p.E;
            float capInv = bulge / Mathf.Max(1e-5f, p.E * 1.06f);
            Color outline = p.Toy.Outline;
            Color[] buf = pc.Pixels;
            int width = pc.Width;
            for (int y = y0; y <= y1; y++)
            {
                float uy = ((y + 0.5f) - cy) * invK;
                float uy2 = uy * uy;
                float h2 = clipLim2 - uy2;
                if (h2 <= 0f)
                {
                    continue;
                }

                float h = Mathf.Sqrt(h2) * k;
                int xa = Mathf.Max(x0, Mathf.FloorToInt(cx - h - 1f));
                int xb = Mathf.Min(x1, Mathf.CeilToInt(cx + h + 1f));
                float py = uy - pb;
                float pySin = py * psin;
                float pyCos = py * pcos;
                float qy2 = 0f;
                float sy2 = 0f;
                if (mode == 1 && !rotated)
                {
                    float qy = py / ed;
                    float sy = py / edd;
                    qy2 = qy * qy;
                    sy2 = sy * sy;
                }

                float pyvy = py * vy;
                int row = y * width;
                for (int x = xa; x <= xb; x++)
                {
                    float ux = ((x + 0.5f) - cx) * invK;
                    float clipCov = 1f;
                    float r2 = (ux * ux) + uy2;
                    if (r2 > inside2)
                    {
                        clipCov = PixelCanvas.Coverage((Mathf.Sqrt(r2) - 1f) * k);
                        if (clipCov <= 0f)
                        {
                            continue;
                        }
                    }

                    float px = ux - pa;
                    float sd;
                    float capDx = 0f;
                    float capDy = 0f;
                    if (mode == 1)
                    {
                        float qx;
                        float qyy;
                        float sx;
                        float syy;
                        if (rotated)
                        {
                            float ex = (px * pcos) + pySin;
                            float ey = (-px * psin) + pyCos;
                            qx = ex / ec;
                            float qy = ey / ed;
                            qyy = qy * qy;
                            sx = ex / ecc;
                            float sy = ey / edd;
                            syy = sy * sy;
                        }
                        else
                        {
                            qx = px / ec;
                            qyy = qy2;
                            sx = px / ecc;
                            syy = sy2;
                        }

                        float k0 = Mathf.Sqrt((qx * qx) + qyy);
                        if (k0 < 1e-5f)
                        {
                            sd = -eMin;
                        }
                        else
                        {
                            float k1 = Mathf.Sqrt((sx * sx) + syy);
                            sd = k0 * (k0 - 1f) / k1;
                        }
                    }
                    else if (mode == 2)
                    {
                        float t = ((px * vx) + pyvy) / vv;
                        t = t < 0f ? 0f : (t > 1f ? 1f : t);
                        capDx = px - (vx * t);
                        capDy = py - (vy * t);
                        sd = Mathf.Sqrt((capDx * capDx) + (capDy * capDy)) - capR;
                    }
                    else
                    {
                        sd = Sdf(p, ux, uy);
                    }

                    float d = sd * k;
                    if (d > maxD)
                    {
                        continue;
                    }

                    if (edgePx > 0f && d > -0.5f)
                    {
                        float ci = PixelCanvas.Coverage(d - edgePx) * clipCov;
                        if (ci > 0f)
                        {
                            pc.Blend(x, y, outline, ci);
                        }
                    }

                    float cf = PixelCanvas.Coverage(d) * clipCov;
                    if (cf <= 0f)
                    {
                        continue;
                    }

                    Color c;
                    if (eye)
                    {
                        // El iris en degradado (la pupila y los reflejos son las partes de detalle que van encima).
                        float er = Mathf.Max(1e-4f, p.C);
                        c = ToyShading.Iris((ux - p.A) / er, (uy - p.B) / er, p.Fill, 0f);
                    }
                    else
                    {
                        float nx;
                        float ny;
                        if (mode == 1)
                        {
                            float lx = ((px * pcos) + pySin) / ec106;
                            float ly = ((-px * psin) + pyCos) / ed106;
                            nx = ((lx * pcos) - (ly * psin)) * bulge;
                            ny = ((lx * psin) + (ly * pcos)) * bulge;
                        }
                        else if (mode == 2)
                        {
                            nx = capDx * capInv;
                            ny = capDy * capInv;
                        }
                        else
                        {
                            Vector2 n = ToyNormal(p, ux, uy, sd, bulge, pcx, pcy, phx, phy, pillow);
                            nx = n.x;
                            ny = n.y;
                        }

                        lut!.Sample(nx, ny, out float r, out float g, out float b);
                        c = new Color(r, g, b, 1f);
                    }

                    if (cf >= 1f)
                    {
                        buf[row + x] = c;
                    }
                    else
                    {
                        pc.Blend(x, y, c, cf);
                    }
                }
            }
        }

        // La normal aparente de la parte en (x, y) (unidades del disco), a sd de su borde, en el plano (la z la pone la cúpula,
        // ToyShading.Normal): la cúpula de la elipse (girada con ella), el cilindro de la cápsula o el cojín del polígono desde su
        // borde, aplanada por bulge.
        private static Vector2 ToyNormal(AvPart p, float x, float y, float sd, float bulge, float pcx, float pcy, float phx, float phy, float pillow)
        {
            switch (p.Kind)
            {
                case AvKind.Ellipse:
                {
                    float px = x - p.A;
                    float py = y - p.B;
                    float lx = ((px * p.Cos) + (py * p.Sin)) / (p.C * 1.06f);
                    float ly = ((-px * p.Sin) + (py * p.Cos)) / (p.D * 1.06f);
                    return new Vector2(((lx * p.Cos) - (ly * p.Sin)) * bulge, ((lx * p.Sin) + (ly * p.Cos)) * bulge);
                }

                case AvKind.Capsule:
                {
                    float px = x - p.A;
                    float py = y - p.B;
                    float vx = p.C - p.A;
                    float vy = p.D - p.B;
                    float t = Mathf.Clamp01(((px * vx) + (py * vy)) / Mathf.Max(1e-6f, (vx * vx) + (vy * vy)));
                    float inv = bulge / Mathf.Max(1e-5f, p.E * 1.06f);
                    return new Vector2((px - (vx * t)) * inv, (py - (vy * t)) * inv);
                }

                default:
                {
                    // El cojín desde el borde (como ToyShading.EdgeDome): mira de lado en el canto y de frente a pillow dentro.
                    float dx = (x - pcx) / phx;
                    float dy = (y - pcy) / phy;
                    float len = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float m = (1f - Mathf.Clamp01(-sd / Mathf.Max(1e-5f, pillow))) * bulge;
                    return len > 1e-5f ? new Vector2(dx / len * m, dy / len * m) : Vector2.zero;
                }
            }
        }

        // Distancia con signo (unidades del disco, negativa dentro) de la parte en (x, y).
        private static float Sdf(AvPart p, float x, float y)
        {
            switch (p.Kind)
            {
                case AvKind.Ellipse:
                {
                    float px = x - p.A;
                    float py = y - p.B;
                    if (p.Cos != 1f || p.Sin != 0f)
                    {
                        float rx = (px * p.Cos) + (py * p.Sin);
                        float ry = (-px * p.Sin) + (py * p.Cos);
                        px = rx;
                        py = ry;
                    }

                    float qx = px / p.C;
                    float qy = py / p.D;
                    float k0 = Mathf.Sqrt((qx * qx) + (qy * qy));
                    if (k0 < 1e-5f)
                    {
                        return -Mathf.Min(p.C, p.D);
                    }

                    float sx = px / (p.C * p.C);
                    float sy = py / (p.D * p.D);
                    float k1 = Mathf.Sqrt((sx * sx) + (sy * sy));
                    return k0 * (k0 - 1f) / k1;
                }

                case AvKind.Capsule:
                {
                    float px = x - p.A;
                    float py = y - p.B;
                    float vx = p.C - p.A;
                    float vy = p.D - p.B;
                    float t = Mathf.Clamp01(((px * vx) + (py * vy)) / Mathf.Max(1e-6f, (vx * vx) + (vy * vy)));
                    float dx = px - (vx * t);
                    float dy = py - (vy * t);
                    return Mathf.Sqrt((dx * dx) + (dy * dy)) - p.E;
                }

                default:
                {
                    float[] v = p.Points!;
                    int n = v.Length / 2;
                    float d = ((x - v[0]) * (x - v[0])) + ((y - v[1]) * (y - v[1]));
                    float s = 1f;
                    for (int i = 0, j = n - 1; i < n; j = i, i++)
                    {
                        float vix = v[2 * i];
                        float viy = v[(2 * i) + 1];
                        float vjx = v[2 * j];
                        float vjy = v[(2 * j) + 1];
                        float ex = vjx - vix;
                        float ey = vjy - viy;
                        float wx = x - vix;
                        float wy = y - viy;
                        float t = Mathf.Clamp01(((wx * ex) + (wy * ey)) / Mathf.Max(1e-6f, (ex * ex) + (ey * ey)));
                        float bx = wx - (ex * t);
                        float by = wy - (ey * t);
                        d = Mathf.Min(d, (bx * bx) + (by * by));
                        bool c1 = y >= viy;
                        bool c2 = y < vjy;
                        bool c3 = (ex * wy) > (ey * wx);
                        if ((c1 && c2 && c3) || (!c1 && !c2 && !c3))
                        {
                            s = -s;
                        }
                    }

                    return s * Mathf.Sqrt(d);
                }
            }
        }

        private static void DestroyNow(UnityEngine.Object o)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(o);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(o);
            }
        }

        // ------------------------------------------------------------------ las formas

        private enum AvKind : byte
        {
            Ellipse = 0,
            Capsule = 1,
            Poly = 2,
        }

        /// <summary>El marco de pintado: centro (px), px por unidad y lado del lienzo.</summary>
        private readonly struct AvFrame
        {
            public AvFrame(float cx, float cy, float k, int n)
            {
                Cx = cx;
                Cy = cy;
                K = k;
                N = n;
            }

            public float Cx { get; }

            public float Cy { get; }

            public float K { get; }

            public int N { get; }
        }

        /// <summary>Una forma del dibujo, inmutable tras construirse (tablas estáticas, sin asignar al pintar).</summary>
        private sealed class AvPart
        {
            public AvKind Kind;
            public float A;
            public float B;
            public float C;
            public float D;
            public float E;
            public float Cos = 1f;
            public float Sin;
            public float[]? Points;
            public Color Fill;
            public bool Ink = true;
            public bool Detail;
            public float Shade = 0.16f;

            /// <summary>Los tonos de juguete del relleno (D-058), calculados una vez al crear las tablas (<see cref="WithToyTones"/>).</summary>
            public ToyTones Toy;

            /// <summary>La tabla de sombreado de la parte (D-058; <see cref="EnsureAvatarLuts"/>), o null hasta que se pide.</summary>
            public ToyLut? Lut;
            public float MinX;
            public float MinY;
            public float MaxX;
            public float MaxY;

            /// <summary>Sin contorno de tinta (manchas, marcas y brillos dentro de otra parte).</summary>
            public AvPart NoInk()
            {
                Ink = false;
                return this;
            }

            /// <summary>Detalle (ojos, brillos y marcas): no sale en la silueta de un bloqueado.</summary>
            public AvPart Det()
            {
                Detail = true;
                Ink = false;
                Shade = 0f;
                return this;
            }

            /// <summary>Ojo o rasgo con tinta pero sin silueta.</summary>
            public AvPart Eye()
            {
                Detail = true;
                Shade = 0f;
                return this;
            }

            /// <summary>Color plano, sin sombreado.</summary>
            public AvPart Flat()
            {
                Shade = 0f;
                return this;
            }
        }

        private static AvPart E(float cx, float cy, float rx, float ry, int hex, float rotDeg = 0f)
        {
            float rad = rotDeg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            float ex = Mathf.Sqrt((rx * rx * c * c) + (ry * ry * s * s));
            float ey = Mathf.Sqrt((rx * rx * s * s) + (ry * ry * c * c));
            return new AvPart
            {
                Kind = AvKind.Ellipse, A = cx, B = cy, C = rx, D = ry, Cos = c, Sin = s, Fill = Palette.Hex(hex),
                MinX = cx - ex, MaxX = cx + ex, MinY = cy - ey, MaxY = cy + ey,
            };
        }

        private static AvPart Cap(float ax, float ay, float bx, float by, float r, int hex)
        {
            return new AvPart
            {
                Kind = AvKind.Capsule, A = ax, B = ay, C = bx, D = by, E = r, Fill = Palette.Hex(hex),
                MinX = Mathf.Min(ax, bx) - r, MaxX = Mathf.Max(ax, bx) + r, MinY = Mathf.Min(ay, by) - r, MaxY = Mathf.Max(ay, by) + r,
            };
        }

        private static AvPart Poly(int hex, params float[] xy)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int i = 0; i < xy.Length; i += 2)
            {
                minX = Mathf.Min(minX, xy[i]);
                maxX = Mathf.Max(maxX, xy[i]);
                minY = Mathf.Min(minY, xy[i + 1]);
                maxY = Mathf.Max(maxY, xy[i + 1]);
            }

            return new AvPart { Kind = AvKind.Poly, Points = xy, Fill = Palette.Hex(hex), MinX = minX, MaxX = maxX, MinY = minY, MaxY = maxY };
        }

        // Ojo de adulto: borde fino, iris (en degradado en el retrato de juguete) y pupila, y los dos reflejos de juguete (D-058):
        // el grande arriba a la izquierda, hacia la luz, y el pequeño abajo a la derecha.
        private static AvPart[] Eye(float x, float y, float r, int iris)
        {
            return new[]
            {
                E(x, y, r, r, iris).Eye(),
                E(x, y, r * 0.5f, r * 0.55f, 0x1A1410).Det(),
                E(x - (r * 0.3f), y + (r * 0.35f), r * 0.22f, r * 0.22f, 0xFFF8E6).Det(),
                E(x + (r * 0.36f), y - (r * 0.32f), r * 0.11f, r * 0.11f, 0xFFF8E6).Det(), // el reflejo pequeño (D-058)
            };
        }

        private static AvPart[] Join(params object[] items)
        {
            int count = 0;
            for (int i = 0; i < items.Length; i++)
            {
                count += items[i] is AvPart[] arr ? arr.Length : 1;
            }

            var result = new AvPart[count];
            int k = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is AvPart[] arr)
                {
                    for (int j = 0; j < arr.Length; j++)
                    {
                        result[k++] = arr[j];
                    }
                }
                else
                {
                    result[k++] = (AvPart)items[i];
                }
            }

            return result;
        }

        private static AvPart[] BuildAvLeaves()
        {
            return new[]
            {
                E(-0.80f, -0.70f, 0.20f, 0.42f, 0x000000, 38f).NoInk(),
                E(-0.48f, -0.98f, 0.19f, 0.40f, 0x000000, 14f).NoInk(),
                E(0.50f, -0.96f, 0.19f, 0.40f, 0x000000, -16f).NoInk(),
                E(0.82f, -0.66f, 0.20f, 0.42f, 0x000000, -40f).NoInk(),
            };
        }

        private static AvPart[] BuildAvNeutral()
        {
            return new[]
            {
                E(0f, 0.02f, 0.30f, 0.56f, 0xFFF8E6, -28f).Flat(),
                Cap(0.22f, -0.42f, -0.20f, 0.44f, 0.025f, 0xD8C9A8).NoInk().Flat(),
            };
        }

        // Los iconos de la chapa (unidades: el radio útil de la chapa), en crema; los huecos, en madera.
        private static AvPart[][] BuildAvBadges()
        {
            const int cream = 0xFFF8E6;
            const int wood = 0x7E5638;
            var badges = new AvPart[7][];
            badges[0] = Array.Empty<AvPart>();
            badges[(int)AvatarArtBadge.World] = new[]
            {
                Cap(-0.45f, -0.85f, -0.45f, 0.85f, 0.11f, cream),
                Poly(cream, -0.40f, 0.85f, 0.75f, 0.45f, -0.40f, 0.05f),
            };
            badges[(int)AvatarArtBadge.Hard] = new[]
            {
                E(-0.42f, 0.32f, 0.28f, 0.28f, cream),
                E(0.42f, 0.32f, 0.28f, 0.28f, cream),
                E(0f, -0.18f, 0.80f, 0.55f, cream),
                E(-0.42f, 0.34f, 0.11f, 0.11f, wood),
                E(0.42f, 0.34f, 0.11f, 0.11f, wood),
            };
            badges[(int)AvatarArtBadge.VeryHard] = new[]
            {
                E(-0.52f, 0.48f, 0.26f, 0.26f, cream),
                E(0.52f, 0.48f, 0.26f, 0.26f, cream),
                E(0f, -0.05f, 0.72f, 0.66f, cream),
                E(-0.26f, 0.08f, 0.10f, 0.10f, wood),
                E(0.26f, 0.08f, 0.10f, 0.10f, wood),
                Poly(wood, -0.14f, -0.22f, 0.14f, -0.22f, 0f, -0.40f),
            };
            badges[(int)AvatarArtBadge.Streak] = new[]
            {
                Cap(-0.30f, -0.80f, -0.30f, 0.85f, 0.14f, cream),
                Cap(-0.30f, 0.55f, 0.10f, 0.55f, 0.07f, cream),
                Cap(-0.30f, 0.20f, 0.00f, 0.20f, 0.07f, cream),
                Poly(cream, -0.85f, -0.30f, -0.45f, -0.18f, 0.00f, -0.34f, 0.45f, -0.18f, 0.85f, -0.30f, 0.80f, -0.80f, -0.80f, -0.80f),
            };
            badges[(int)AvatarArtBadge.Daily] = new[]
            {
                E(0f, 0f, 0.82f, 0.82f, cream),
                Poly(wood, -0.62f, 0.05f, -0.30f, 0.20f, 0.00f, 0.05f, 0.30f, 0.20f, 0.62f, 0.05f, 0.62f, -0.07f, 0.30f, 0.08f, 0.00f, -0.07f, -0.30f, 0.08f, -0.62f, -0.07f),
                Poly(wood, -0.52f, -0.35f, -0.25f, -0.22f, 0.00f, -0.35f, 0.25f, -0.22f, 0.52f, -0.35f, 0.52f, -0.47f, 0.25f, -0.34f, 0.00f, -0.47f, -0.25f, -0.34f, -0.52f, -0.47f),
            };
            badges[(int)AvatarArtBadge.League] = new[]
            {
                Poly(cream, -0.24f, -0.70f, 0.24f, -0.70f, 0.24f, 0.50f, -0.24f, 0.50f),
                Poly(cream, -0.80f, -0.70f, -0.30f, -0.70f, -0.30f, 0.10f, -0.80f, 0.10f),
                Poly(cream, 0.30f, -0.70f, 0.80f, -0.70f, 0.80f, -0.20f, 0.30f, -0.20f),
            };
            return badges;
        }

        // Los 16 animales del catálogo (avatares.md §1.2), en su orden: de busto, de frente o a tres cuartos (mirando a la derecha), con
        // proporciones de adulto (ojos ≤ 12 % del ancho de la cara), sin ropa ni accesorios y con su comportamiento real.
        private static AvPart[][] BuildAvAnimals()
        {
            var a = new AvPart[AvatarArtCount][];

            // 0 Guacamayo (guacamayo rojo): rojo con la cara desnuda clara, pico curvo claro y ala azul con la franja amarilla.
            a[0] = Join(
                E(-0.10f, -0.78f, 0.62f, 0.55f, 0xC8322B),
                E(-0.52f, -0.70f, 0.30f, 0.55f, 0x2A5DB0, 18f),
                E(-0.46f, -0.42f, 0.22f, 0.13f, 0xF2C230, 18f).NoInk(),
                E(0.00f, 0.12f, 0.42f, 0.46f, 0xC8322B),
                E(0.20f, 0.14f, 0.20f, 0.24f, 0xF1ECE2).Flat(),
                Cap(0.12f, 0.05f, 0.30f, 0.08f, 0.012f, 0xB8A592).Det(),
                Cap(0.12f, 0.12f, 0.30f, 0.15f, 0.012f, 0xB8A592).Det(),
                Poly(0x2B2118, 0.30f, 0.02f, 0.50f, -0.08f, 0.44f, -0.24f, 0.28f, -0.14f),
                Poly(0xEDE3D0, 0.28f, 0.28f, 0.52f, 0.24f, 0.66f, 0.06f, 0.62f, -0.12f, 0.52f, -0.06f, 0.30f, 0.04f),
                Eye(0.15f, 0.24f, 0.045f, 0xE9CF5A),
                E(-0.12f, 0.36f, 0.12f, 0.06f, 0xFFFFFF, -20f).Det());
            a[0][a[0].Length - 1].Fill = new Color(1f, 1f, 1f, 0.22f);

            // 1 Tucán: cabeza y lomo negros, babero crema-amarillo, pico grande naranja con la punta oscura y ojo con anillo azul.
            a[1] = Join(
                E(-0.12f, -0.78f, 0.62f, 0.56f, 0x1E1A17),
                E(-0.05f, 0.12f, 0.42f, 0.46f, 0x1E1A17),
                E(0.02f, -0.28f, 0.30f, 0.30f, 0xF3DB7A).NoInk(),
                E(0.05f, -0.52f, 0.20f, 0.10f, 0xC8452E).NoInk(),
                Poly(0xD9691F, 0.22f, 0.10f, 0.60f, 0.04f, 0.90f, -0.04f, 0.58f, -0.10f, 0.22f, -0.04f),
                Poly(0xF0902F, 0.20f, 0.38f, 0.58f, 0.36f, 0.88f, 0.14f, 0.96f, -0.02f, 0.60f, 0.06f, 0.22f, 0.10f),
                Poly(0x2B2118, 0.86f, 0.17f, 0.96f, -0.02f, 0.84f, -0.02f).NoInk(),
                E(0.08f, 0.24f, 0.09f, 0.09f, 0x4FA3D9).Eye(),
                Eye(0.08f, 0.24f, 0.042f, 0x2A2420));

            // 2 Tapir (anta): cabeza gris parda de frente, orejas con el borde claro, la crin corta y la trompa corta.
            a[2] = Join(
                E(0f, -0.95f, 0.66f, 0.46f, 0x5A5050),
                E(-0.40f, 0.52f, 0.15f, 0.21f, 0xD8D0C4, 15f),
                E(0.40f, 0.52f, 0.15f, 0.21f, 0xD8D0C4, -15f),
                E(-0.40f, 0.50f, 0.10f, 0.16f, 0x4A4242, 15f).NoInk(),
                E(0.40f, 0.50f, 0.10f, 0.16f, 0x4A4242, -15f).NoInk(),
                E(0f, 0.55f, 0.10f, 0.18f, 0x2E2626),
                E(0f, 0.02f, 0.50f, 0.54f, 0x5A5050),
                Cap(0f, -0.15f, 0.04f, -0.52f, 0.17f, 0x4C4444),
                E(0.03f, -0.58f, 0.19f, 0.11f, 0x8A8078),
                E(0.03f, -0.66f, 0.10f, 0.03f, 0x2B2118).Det(),
                Eye(-0.21f, 0.14f, 0.036f, 0x3A2A20),
                Eye(0.21f, 0.14f, 0.036f, 0x3A2A20));

            // 3 Tortuga (taricaya): cabeza oliva con manchas amarillas, saliendo del caparazón.
            a[3] = Join(
                Cap(0f, -0.30f, 0f, -0.90f, 0.22f, 0x4E5A3A),
                E(0f, -1.02f, 0.95f, 0.50f, 0x6E6A3C),
                Cap(-0.55f, -0.70f, -0.30f, -0.95f, 0.012f, 0x3E3220).Det(),
                Cap(0.55f, -0.70f, 0.30f, -0.95f, 0.012f, 0x3E3220).Det(),
                Cap(-0.20f, -0.60f, 0.20f, -0.60f, 0.012f, 0x3E3220).Det(),
                E(0.04f, 0.02f, 0.44f, 0.38f, 0x55633F),
                E(-0.17f, 0.14f, 0.07f, 0.05f, 0xE8C93A).Det(),
                E(0.24f, 0.14f, 0.07f, 0.05f, 0xE8C93A).Det(),
                E(0.05f, 0.22f, 0.06f, 0.04f, 0xE8C93A).Det(),
                E(-0.10f, -0.16f, 0.08f, 0.04f, 0xE8C93A).Det(),
                Cap(-0.15f, -0.13f, 0.25f, -0.13f, 0.012f, 0x2B2118).Det(),
                Eye(-0.12f, 0.04f, 0.034f, 0x3A3018),
                Eye(0.20f, 0.04f, 0.034f, 0x3A3018));

            // 4 Coatí: hocico largo con la punta oscura, máscara clara junto a los ojos y orejas redondas con el borde claro.
            a[4] = Join(
                E(0f, -0.95f, 0.62f, 0.46f, 0x8A5A34),
                E(0f, -0.62f, 0.26f, 0.20f, 0xC9A27A).NoInk(),
                E(-0.30f, 0.42f, 0.11f, 0.11f, 0xE9DCC6),
                E(0.30f, 0.42f, 0.11f, 0.11f, 0xE9DCC6),
                E(-0.30f, 0.41f, 0.07f, 0.07f, 0x5E3A20).NoInk(),
                E(0.30f, 0.41f, 0.07f, 0.07f, 0x5E3A20).NoInk(),
                E(0f, 0.10f, 0.40f, 0.38f, 0x8A5A34),
                Poly(0x7A4E2C, -0.17f, -0.04f, 0.17f, -0.04f, 0.08f, -0.60f, -0.06f, -0.60f),
                E(0.01f, -0.63f, 0.09f, 0.06f, 0x2B2118),
                E(-0.17f, 0.12f, 0.11f, 0.07f, 0xE9DCC6, 10f).Det(),
                E(0.17f, 0.12f, 0.11f, 0.07f, 0xE9DCC6, -10f).Det(),
                Eye(-0.16f, 0.13f, 0.036f, 0x2E2018),
                Eye(0.16f, 0.13f, 0.036f, 0x2E2018));

            // 5 Colibrí: verde con la gola rubí, vientre claro y el pico de aguja.
            a[5] = Join(
                E(-0.58f, -0.40f, 0.52f, 0.15f, 0x1F6B44, 32f),
                E(-0.15f, -0.60f, 0.44f, 0.56f, 0x2E8B57),
                E(-0.05f, -0.78f, 0.25f, 0.30f, 0xE8E4D8).NoInk(),
                E(-0.05f, 0.20f, 0.33f, 0.32f, 0x2E8B57),
                E(0.06f, -0.16f, 0.22f, 0.15f, 0xC2304A).NoInk(),
                Cap(0.24f, 0.18f, 0.96f, 0.04f, 0.03f, 0x2B2118),
                E(0.20f, 0.30f, 0.04f, 0.025f, 0xF2EFE6).Det(),
                Eye(0.08f, 0.25f, 0.042f, 0x1A1410),
                E(-0.14f, 0.36f, 0.10f, 0.05f, 0xFFFFFF, -15f).Det());
            a[5][a[5].Length - 1].Fill = new Color(1f, 1f, 1f, 0.22f);

            // 6 Rana dardo: azul con manchas negras, de frente y con las patas delanteras; nada que diga «veneno».
            a[6] = Join(
                E(0f, -0.62f, 0.72f, 0.50f, 0x2F6FCE),
                Cap(-0.52f, -0.55f, -0.66f, -0.95f, 0.10f, 0x2F6FCE),
                Cap(0.52f, -0.55f, 0.66f, -0.95f, 0.10f, 0x2F6FCE),
                E(-0.35f, 0.22f, 0.17f, 0.16f, 0x2F6FCE),
                E(0.35f, 0.22f, 0.17f, 0.16f, 0x2F6FCE),
                E(0f, -0.08f, 0.62f, 0.38f, 0x3A7BD5),
                E(-0.20f, -0.02f, 0.07f, 0.05f, 0x1A1410).Det(),
                E(0.22f, -0.20f, 0.06f, 0.05f, 0x1A1410).Det(),
                E(0.00f, 0.12f, 0.05f, 0.04f, 0x1A1410).Det(),
                E(-0.30f, -0.62f, 0.09f, 0.06f, 0x1A1410).Det(),
                E(0.25f, -0.70f, 0.08f, 0.06f, 0x1A1410).Det(),
                Cap(-0.30f, -0.26f, 0.30f, -0.26f, 0.012f, 0x1A1410).Det(),
                Eye(-0.35f, 0.24f, 0.07f, 0x1A1410),
                Eye(0.35f, 0.24f, 0.07f, 0x1A1410));

            // 7 Nutria gigante (ariranha): cabeza ancha y plana, mancha crema en la garganta, asomando del agua con una onda.
            a[7] = Join(
                E(0f, -0.62f, 0.44f, 0.38f, 0x5A3E2A),
                E(-0.10f, -0.46f, 0.20f, 0.13f, 0xEADBC0).NoInk(),
                E(0.13f, -0.52f, 0.14f, 0.10f, 0xEADBC0).NoInk(),
                E(-0.40f, 0.28f, 0.07f, 0.07f, 0x4A3220),
                E(0.40f, 0.28f, 0.07f, 0.07f, 0x4A3220),
                E(0f, 0.04f, 0.46f, 0.38f, 0x5A3E2A),
                E(0f, -0.17f, 0.25f, 0.16f, 0x6B4A33),
                E(0f, -0.08f, 0.09f, 0.05f, 0x2B2118),
                Cap(0.12f, -0.18f, 0.40f, -0.12f, 0.008f, 0xE8DCC8).Det(),
                Cap(0.12f, -0.22f, 0.40f, -0.24f, 0.008f, 0xE8DCC8).Det(),
                Cap(-0.12f, -0.18f, -0.40f, -0.12f, 0.008f, 0xE8DCC8).Det(),
                Cap(-0.12f, -0.22f, -0.40f, -0.24f, 0.008f, 0xE8DCC8).Det(),
                Eye(-0.20f, 0.10f, 0.036f, 0x1E1410),
                Eye(0.20f, 0.10f, 0.036f, 0x1E1410),
                Poly(0x2E8FA8, -1.0f, -0.72f, -0.6f, -0.66f, -0.2f, -0.76f, 0.2f, -0.66f, 0.6f, -0.76f, 1.0f, -0.68f, 1.0f, -1.1f, -1.0f, -1.1f),
                Poly(0x8ADBE6, -0.62f, -0.70f, -0.20f, -0.79f, 0.20f, -0.70f, 0.20f, -0.74f, -0.20f, -0.83f, -0.62f, -0.74f).NoInk());

            // 8 Jaguar: de frente, orejas redondas, rosetas abiertas en la frente y las mejillas, hocico claro y boca cerrada.
            a[8] = Join(
                E(0f, -0.88f, 0.60f, 0.40f, 0xD39A3E),
                E(-0.42f, 0.44f, 0.17f, 0.17f, 0xD39A3E),
                E(0.42f, 0.44f, 0.17f, 0.17f, 0xD39A3E),
                E(-0.42f, 0.42f, 0.07f, 0.07f, 0xF1E4C8).NoInk(),
                E(0.42f, 0.42f, 0.07f, 0.07f, 0xF1E4C8).NoInk(),
                E(0f, 0f, 0.55f, 0.50f, 0xD9A040),
                E(0f, -0.24f, 0.26f, 0.19f, 0xF1E4C8),
                Poly(0xB5655A, -0.08f, -0.10f, 0.08f, -0.10f, 0f, -0.18f),
                Cap(0f, -0.18f, 0f, -0.27f, 0.012f, 0x2B2118).Det(),
                Cap(0f, -0.27f, -0.10f, -0.32f, 0.012f, 0x2B2118).Det(),
                Cap(0f, -0.27f, 0.10f, -0.32f, 0.012f, 0x2B2118).Det(),
                E(-0.10f, 0.32f, 0.055f, 0.05f, 0x3A2A1A).Det(),
                E(0.10f, 0.32f, 0.055f, 0.05f, 0x3A2A1A).Det(),
                E(-0.10f, 0.32f, 0.025f, 0.022f, 0xB9802E).Det(),
                E(0.10f, 0.32f, 0.025f, 0.022f, 0xB9802E).Det(),
                E(0f, 0.20f, 0.04f, 0.035f, 0x3A2A1A).Det(),
                E(-0.38f, -0.10f, 0.06f, 0.05f, 0x3A2A1A).Det(),
                E(0.38f, -0.10f, 0.06f, 0.05f, 0x3A2A1A).Det(),
                E(-0.38f, -0.10f, 0.028f, 0.024f, 0xB9802E).Det(),
                E(0.38f, -0.10f, 0.028f, 0.024f, 0xB9802E).Det(),
                E(-0.30f, -0.80f, 0.07f, 0.06f, 0x3A2A1A).Det(),
                E(0.30f, -0.80f, 0.07f, 0.06f, 0x3A2A1A).Det(),
                E(0.0f, -0.95f, 0.07f, 0.06f, 0x3A2A1A).Det(),
                Eye(-0.20f, 0.08f, 0.055f, 0xD9A23A),
                Eye(0.20f, 0.08f, 0.055f, 0xD9A23A));

            // 9 Martín pescador (martín pescador grande): cabeza azul grisácea con cresta, collar blanco, pecho rufo y pico negro largo.
            a[9] = Join(
                E(-0.52f, -0.62f, 0.25f, 0.50f, 0x2F6E8E, 10f),
                E(-0.05f, -0.78f, 0.50f, 0.52f, 0xB5552B),
                E(-0.05f, -0.18f, 0.38f, 0.12f, 0xF2EFE6),
                Poly(0x24576F, -0.32f, 0.42f, -0.04f, 0.54f, -0.50f, 0.70f, -0.40f, 0.52f, -0.62f, 0.56f),
                E(-0.05f, 0.20f, 0.36f, 0.34f, 0x2F6E8E),
                Poly(0x2B2118, 0.22f, 0.25f, 0.88f, 0.12f, 0.22f, 0.07f),
                E(0.18f, 0.31f, 0.035f, 0.03f, 0xF2EFE6).Det(),
                Eye(0.06f, 0.24f, 0.042f, 0x1A1410));

            // 10 Caimán (de anteojos): cabeza de lado, mirando a la derecha, con la boca cerrada; escamas del lomo.
            a[10] = Join(
                E(-0.35f, -0.78f, 0.62f, 0.45f, 0x56613F),
                E(-0.55f, -0.62f, 0.10f, 0.06f, 0x404A2E).Det(),
                E(-0.30f, -0.56f, 0.10f, 0.06f, 0x404A2E).Det(),
                E(-0.05f, -0.62f, 0.10f, 0.06f, 0x404A2E).Det(),
                Poly(0x5F6B47, -0.50f, 0.18f, 0.20f, 0.10f, 0.86f, 0.02f, 0.92f, -0.10f, 0.86f, -0.20f, 0.20f, -0.28f, -0.50f, -0.36f),
                E(-0.32f, -0.02f, 0.34f, 0.30f, 0x5F6B47),
                E(-0.14f, 0.20f, 0.13f, 0.10f, 0x6E7A52),
                Cap(-0.04f, 0.24f, 0.10f, 0.22f, 0.018f, 0x48523A).Det(),
                Cap(-0.36f, -0.14f, 0.86f, -0.10f, 0.012f, 0x2B2118).Det(),
                E(0.80f, 0.02f, 0.025f, 0.02f, 0x2B2118).Det(),
                E(-0.13f, 0.22f, 0.045f, 0.04f, 0xC8B44A).Eye(),
                Cap(-0.13f, 0.20f, -0.13f, 0.24f, 0.012f, 0x1A1410).Det());

            // 11 Oso hormiguero (tamanduá-bandeira): gris con el hocico tubular largo y la franja negra y blanca del hombro.
            a[11] = Join(
                E(-0.30f, -0.65f, 0.62f, 0.52f, 0x8C8478),
                Poly(0xEEE8DC, -0.90f, -0.18f, -0.76f, -0.10f, 0.12f, -0.60f, 0.04f, -0.70f).NoInk(),
                Poly(0x1E1A17, -0.92f, -0.30f, -0.78f, -0.20f, 0.08f, -0.70f, -0.02f, -0.84f).NoInk(),
                E(-0.30f, 0.38f, 0.07f, 0.07f, 0x7A7266),
                E(-0.15f, 0.15f, 0.30f, 0.28f, 0x8C8478),
                Poly(0x8C8478, 0.00f, 0.24f, 0.88f, -0.01f, 0.88f, -0.12f, 0.00f, -0.04f),
                E(0.86f, -0.065f, 0.03f, 0.05f, 0x2B2118).Det(),
                Eye(-0.02f, 0.15f, 0.03f, 0x1A1410));

            // 12 Garza (garza cocoi): cuello blanco en S, gorra negra con el penacho, pico amarillo y cuerpo gris.
            a[12] = Join(
                E(-0.22f, -0.95f, 0.60f, 0.45f, 0x9AA3A8),
                Cap(0.02f, 0.22f, 0.12f, -0.12f, 0.14f, 0xF4F2EC),
                Cap(0.12f, -0.12f, -0.06f, -0.62f, 0.15f, 0xF4F2EC),
                E(0.05f, -0.30f, 0.025f, 0.02f, 0x2B2118).Det(),
                E(0.02f, -0.45f, 0.025f, 0.02f, 0x2B2118).Det(),
                Cap(-0.20f, 0.46f, -0.52f, 0.30f, 0.03f, 0x1E1A17),
                E(0f, 0.35f, 0.24f, 0.20f, 0xF4F2EC),
                E(-0.04f, 0.46f, 0.21f, 0.09f, 0x1E1A17).NoInk(),
                Poly(0xE3B52E, 0.18f, 0.40f, 0.86f, 0.30f, 0.18f, 0.29f),
                E(0.10f, 0.37f, 0.045f, 0.045f, 0xE3C94A).Eye(),
                Eye(0.10f, 0.37f, 0.03f, 0x1A1410));

            // 13 Mono aullador (aullador rojo): pelaje rojizo, cara oscura desnuda y la barba.
            a[13] = Join(
                E(0f, -0.95f, 0.66f, 0.45f, 0x9C3E22),
                E(0f, 0.04f, 0.50f, 0.52f, 0x9C3E22),
                E(0f, -0.36f, 0.36f, 0.30f, 0x8A361E),
                E(0f, 0.0f, 0.28f, 0.32f, 0x3A2A24),
                Cap(-0.18f, 0.18f, 0.18f, 0.18f, 0.05f, 0x2A1E1A).Det(),
                E(0f, -0.16f, 0.16f, 0.11f, 0x4A3830),
                E(-0.04f, -0.11f, 0.02f, 0.015f, 0x1A1410).Det(),
                E(0.04f, -0.11f, 0.02f, 0.015f, 0x1A1410).Det(),
                Eye(-0.10f, 0.07f, 0.034f, 0x7A4A2A),
                Eye(0.10f, 0.07f, 0.034f, 0x7A4A2A));

            // 14 Ocelote: más esbelto que el jaguar, orejas puntiagudas, rayas en las mejillas y la frente, ojos ámbar con el borde claro.
            a[14] = Join(
                E(0f, -0.88f, 0.56f, 0.40f, 0xD7B27A),
                Poly(0xD7B27A, -0.44f, 0.18f, -0.34f, 0.70f, -0.08f, 0.40f),
                Poly(0xD7B27A, 0.44f, 0.18f, 0.34f, 0.70f, 0.08f, 0.40f),
                Poly(0x3A2A1A, -0.36f, 0.32f, -0.33f, 0.58f, -0.18f, 0.40f).NoInk(),
                Poly(0x3A2A1A, 0.36f, 0.32f, 0.33f, 0.58f, 0.18f, 0.40f).NoInk(),
                E(0f, 0f, 0.48f, 0.48f, 0xD7B27A),
                E(0f, -0.22f, 0.22f, 0.16f, 0xF2E7D0),
                Cap(-0.44f, -0.04f, -0.22f, 0.02f, 0.022f, 0x3A2A1A).Det(),
                Cap(-0.44f, -0.18f, -0.24f, -0.12f, 0.022f, 0x3A2A1A).Det(),
                Cap(0.44f, -0.04f, 0.22f, 0.02f, 0.022f, 0x3A2A1A).Det(),
                Cap(0.44f, -0.18f, 0.24f, -0.12f, 0.022f, 0x3A2A1A).Det(),
                Cap(-0.07f, 0.42f, -0.05f, 0.20f, 0.02f, 0x3A2A1A).Det(),
                Cap(0.07f, 0.42f, 0.05f, 0.20f, 0.02f, 0x3A2A1A).Det(),
                Cap(-0.36f, -0.80f, -0.10f, -0.86f, 0.03f, 0x3A2A1A).Det(),
                Cap(0.36f, -0.80f, 0.10f, -0.86f, 0.03f, 0x3A2A1A).Det(),
                Poly(0xB5655A, -0.07f, -0.10f, 0.07f, -0.10f, 0f, -0.17f),
                Cap(0f, -0.17f, 0f, -0.25f, 0.012f, 0x2B2118).Det(),
                E(-0.20f, 0.08f, 0.075f, 0.065f, 0xF2E7D0).Det(),
                E(0.20f, 0.08f, 0.075f, 0.065f, 0xF2E7D0).Det(),
                Eye(-0.20f, 0.08f, 0.058f, 0xC8962E),
                Eye(0.20f, 0.08f, 0.058f, 0xC8962E));

            // 15 Armadillo (tatú): cabeza con el escudo y el hocico puntiagudo, orejas largas y el caparazón con sus bandas.
            a[15] = Join(
                E(-0.30f, -0.70f, 0.78f, 0.55f, 0x9C8470),
                Cap(-0.70f, -0.40f, -0.75f, -0.95f, 0.012f, 0x6E5A48).Det(),
                Cap(-0.45f, -0.25f, -0.48f, -1.0f, 0.012f, 0x6E5A48).Det(),
                Cap(-0.20f, -0.20f, -0.20f, -1.0f, 0.012f, 0x6E5A48).Det(),
                Cap(0.05f, -0.25f, 0.08f, -1.0f, 0.012f, 0x6E5A48).Det(),
                E(-0.10f, 0.50f, 0.07f, 0.18f, 0xB49A86, 15f),
                E(0.06f, 0.52f, 0.07f, 0.18f, 0xB49A86, -10f),
                Poly(0xB49A86, -0.18f, 0.34f, 0.18f, 0.38f, 0.78f, 0.00f, 0.74f, -0.08f, -0.06f, -0.08f),
                E(0.05f, 0.24f, 0.18f, 0.10f, 0x9C8470, -12f).NoInk(),
                E(0.75f, -0.04f, 0.03f, 0.03f, 0x2B2118).Det(),
                Eye(0.16f, 0.16f, 0.03f, 0x1A1410));

            return WithToyTones(a);
        }

        // Los tonos de juguete de cada parte, una vez (no al pintar: el atlas pinta sin asignar ni repetir la cuenta en OKLab).
        private static AvPart[][] WithToyTones(AvPart[][] animals)
        {
            for (int i = 0; i < animals.Length; i++)
            {
                AvPart[] parts = animals[i];
                for (int j = 0; j < parts.Length; j++)
                {
                    parts[j].Toy = ToyShading.Tones(parts[j].Fill, 0.85f);
                }
            }

            return animals;
        }
    }

    /// <summary>
    /// Atlas perezoso de los retratos de avatar (avatares.md §2.2): 5 × 4 celdas de <see cref="CellPx"/> px (los 16 y el disco neutro),
    /// en RGBA32 con mipmaps. Se pide qué va en cada celda (<see cref="Request"/>) y se pintan como mucho dos por fotograma
    /// (<see cref="Tick"/>), con el hueco transparente mientras tanto; al desbloquear, se repinta la celda. Sin asignar memoria por
    /// fotograma (un lienzo y su borrador reutilizados). Se libera con <see cref="Dispose"/> al cerrar «Tu avatar» y la Liga.
    /// </summary>
    public sealed class AvatarAtlas : IDisposable
    {
        /// <summary>Columnas del atlas.</summary>
        public const int Columns = 5;

        /// <summary>Filas del atlas.</summary>
        public const int Rows = 4;

        /// <summary>Celda del disco neutro.</summary>
        public const int NeutralCell = ProceduralArt.AvatarArtCount;

        /// <summary>Retratos por fotograma (avatares.md §2.2: «dos retratos por fotograma»).</summary>
        public const int PerTick = 2;

        private readonly Texture2D _texture;
        private readonly Sprite?[] _sprites = new Sprite?[Columns * Rows];
        private readonly Sprite?[] _png = new Sprite?[Columns * Rows];
        private readonly int[] _index = new int[Columns * Rows];
        private readonly AvatarArtBackdrop[] _backdrop = new AvatarArtBackdrop[Columns * Rows];
        private readonly AvatarArtBadge[] _badge = new AvatarArtBadge[Columns * Rows];
        private readonly bool[] _locked = new bool[Columns * Rows];
        private readonly bool[] _dirty = new bool[Columns * Rows];
        private readonly bool[] _painted = new bool[Columns * Rows];
        private PixelCanvas? _cell;
        private PixelCanvas? _scratch;
        private bool _disposed;

        internal AvatarAtlas(int cellPx)
        {
            CellPx = Mathf.Clamp(cellPx, 16, 512);
            _texture = new Texture2D(Columns * CellPx, Rows * CellPx, TextureFormat.RGBA32, true)
            {
                name = "avatar_atlas",
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
                mipMapBias = -0.5f,
            };
            var clear = new Color32[_texture.width * _texture.height];
            _texture.SetPixels32(clear);
            _texture.Apply(true, false);
            for (int i = 0; i < _index.Length; i++)
            {
                _index[i] = int.MinValue;
            }
        }

        /// <summary>Lado de una celda (px).</summary>
        public int CellPx { get; }

        /// <summary>La textura (tests).</summary>
        public Texture2D Texture => _texture;

        /// <summary>Quedan celdas por pintar.</summary>
        public bool Pending
        {
            get
            {
                for (int i = 0; i < _dirty.Length; i++)
                {
                    if (_dirty[i])
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Pide la celda <paramref name="cell"/> (0-15 o <see cref="NeutralCell"/>) con ese retrato; si ya está así, nada. Un retrato
        /// normal con PNG del artista (<c>avatar_&lt;id&gt;</c>) usa el PNG y no se pinta.
        /// </summary>
        public void Request(int cell, string? id, int index, AvatarArtBackdrop backdrop, AvatarArtBadge badge, bool locked)
        {
            if (_disposed || cell < 0 || cell >= _dirty.Length)
            {
                return;
            }

            if (_index[cell] == index && _backdrop[cell] == backdrop && _badge[cell] == badge && _locked[cell] == locked && (_painted[cell] || _dirty[cell] || _png[cell] != null))
            {
                return;
            }

            if (!locked)
            {
                ProceduralArt.EnsureAvatarLuts(index);
            }

            _index[cell] = index;
            _backdrop[cell] = backdrop;
            _badge[cell] = badge;
            _locked[cell] = locked;
            _png[cell] = !locked && id != null ? ArtSlots.Find(ProceduralArt.AvatarSlot(id)) : null;
            _dirty[cell] = _png[cell] == null;
            _painted[cell] = false;
        }

        /// <summary>Pinta hasta <paramref name="budget"/> celdas pendientes y sube la textura; cuántas pintó.</summary>
        public int Tick(int budget = PerTick)
        {
            if (_disposed)
            {
                return 0;
            }

            int done = 0;
            for (int i = 0; i < _dirty.Length && done < budget; i++)
            {
                if (!_dirty[i])
                {
                    continue;
                }

                _cell ??= new PixelCanvas(CellPx, CellPx);
                _scratch ??= new PixelCanvas(CellPx, CellPx);
                _cell.Fill(Color.clear);
                ProceduralArt.PaintAvatar(_cell, _scratch, _index[i], _backdrop[i], _badge[i], _locked[i]);
                _cell.CopyTo(_texture, (i % Columns) * CellPx, (i / Columns) * CellPx);
                _dirty[i] = false;
                _painted[i] = true;
                done++;
            }

            if (done > 0)
            {
                _texture.Apply(true, false);
            }

            return done;
        }

        /// <summary>Pinta todo lo pendiente de una vez (tests y capturas).</summary>
        public void PaintAll()
        {
            while (Tick(Columns * Rows) > 0)
            {
            }
        }

        /// <summary>La celda ya tiene su retrato (pintado o del PNG).</summary>
        public bool Ready(int cell) => cell >= 0 && cell < _dirty.Length && (_painted[cell] || _png[cell] != null);

        /// <summary>El sprite de la celda (el PNG del artista si lo hay); transparente hasta que se pinte.</summary>
        public Sprite Cell(int cell)
        {
            if (cell < 0 || cell >= _sprites.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(cell));
            }

            if (_png[cell] != null)
            {
                return _png[cell]!;
            }

            Sprite? s = _sprites[cell];
            if (s == null)
            {
                s = Sprite.Create(_texture, new Rect((cell % Columns) * CellPx, (cell / Columns) * CellPx, CellPx, CellPx), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                s.name = "avatar_cell_" + cell.ToString(System.Globalization.CultureInfo.InvariantCulture);
                s.hideFlags = HideFlags.DontSave;
                _sprites[cell] = s;
            }

            return s;
        }

        /// <summary>Suelta la textura, los sprites y los lienzos.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            for (int i = 0; i < _sprites.Length; i++)
            {
                if (_sprites[i] != null)
                {
                    Destroy(_sprites[i]!);
                    _sprites[i] = null;
                }
            }

            Destroy(_texture);
            _cell?.Release();
            _scratch?.Release();
            _cell = null;
            _scratch = null;
        }

        private static void Destroy(UnityEngine.Object o)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(o);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(o);
            }
        }
    }
}
