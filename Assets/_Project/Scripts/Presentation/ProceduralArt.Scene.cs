#nullable enable
using System;
using System.IO;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>Forma de hoja para <c>DrawLeaf</c> (guía 4.8).</summary>
    internal enum LeafShape
    {
        /// <summary>Ovada ancha (filodendro).</summary>
        Ovate,

        /// <summary>Lanceolada (hoja en flecha de orilla, foliolos de açaí).</summary>
        Lanceolate,

        /// <summary>Bráctea en barca (heliconia).</summary>
        Bract,
    }

    public sealed partial class ProceduralArt
    {
        /// <summary>Versión del arte procedural: forma parte de la clave de la caché en disco (guía 4.9).</summary>
        public const string ArtVersion = "v2.2.1";

        // Respaldo de escena a 2,25 px/dp (el panorama, 810 × 1215 px) y follaje a 3 px/dp (revisión visual del 2026-10-07:
        // a 1,5 y 2 px/dp las orillas y las frondas salían en escalera al ampliarse en el teléfono, a 3 px/dp).
        private const float ScenePxPerDp = 2.25f;
        private const float FoliagePxPerDp = 3f;

        // Lado del follaje de las esquinas superiores (dp).
        private const float TopFoliageDp = 160f;

        /// <summary>Panorama del Estuario, 360 × 540 dp anclado arriba (ranura <c>bg_estuario_panorama</c>; guía 3.9).</summary>
        public Sprite Panorama { get; private set; } = null!;

        /// <summary>Muelle, 360 × 360 dp anclado abajo, con el 20 % superior vacío (ranura <c>bg_estuario_muelle</c>).</summary>
        public Sprite Dock { get; private set; } = null!;

        /// <summary>Follaje de la esquina superior izquierda (ranura <c>foliage_tl</c>; guía 3.8).</summary>
        public Sprite FoliageTopLeft { get; private set; } = null!;

        /// <summary>Follaje de la esquina superior derecha, con el racimo de açaí unido a la fronda (ranura <c>foliage_tr</c>).</summary>
        public Sprite FoliageTopRight { get; private set; } = null!;

        /// <summary>Follaje de la esquina inferior izquierda (ranura <c>foliage_bl</c>).</summary>
        public Sprite FoliageBottomLeft { get; private set; } = null!;

        /// <summary>Follaje de la esquina inferior derecha (ranura <c>foliage_br</c>).</summary>
        public Sprite FoliageBottomRight { get; private set; } = null!;

        /// <summary>Relleno del panel del tablero (siempre por código, guía 4.6): degradado y charco de luz.</summary>
        public Sprite PanelFill { get; private set; } = null!;

        /// <summary>Marco del panel, 9-slice (ranura <c>ui_panel_tablero</c>, solo marco; guía 4.6).</summary>
        public Sprite PanelFrame { get; private set; } = null!;

        /// <summary>Madera del marco procedural del panel (dp; revisión visual del 2026-10-07: 12 en lugar de 6).</summary>
        public const float PanelFrameWoodDp = 12f;

        /// <summary>
        /// Borde del marco 9-slice en dp a escala 1 (para colocar el relleno dentro): contorno de 1,5 dp + madera de 12 dp.
        /// GameScreen lo reduce con <c>Image.pixelsPerUnitMultiplier</c> si las cuias quedan a menos de 8 dp del marco.
        /// </summary>
        public float PanelFrameDp { get; private set; } = 13.5f;

        /// <summary>Sombra interior del panel: 16 dp, negro 0,35 → 0 (9-slice).</summary>
        public Sprite PanelInnerShadow { get; private set; } = null!;

        /// <summary>Sombra proyectada suave de paneles y bandeja (9-slice, desenfoque ≈ 10 dp).</summary>
        public Sprite SoftShadow { get; private set; } = null!;

        /// <summary>Repisa de madera tallada de una fila (9-slice horizontal, 26 dp de alto; guía 4.5).</summary>
        public Sprite Shelf { get; private set; } = null!;

        /// <summary>
        /// Capi en el muelle, solo si existe <c>capi_idle.png</c> (ranura <c>capi_idle</c>; guía 3.7 y 10.2 F7). Sin respaldo
        /// procedural: una mascota pobre resta más que ninguna (revisión visual del 2026-10-07); null si no hay PNG.
        /// </summary>
        public Sprite? Capi { get; private set; }

        /// <summary>Garza blanca posada, 24 × 34 dp, de perfil (07 §2: "garzas en la orilla"; pendiente de revisión cultural).</summary>
        public Sprite Heron { get; private set; } = null!;

        /// <summary>Hoja de vitória-régia en perspectiva, 48 × 16 dp (pendiente de revisión cultural).</summary>
        public Sprite LilyPad { get; private set; } = null!;

        /// <summary>Rayos de luz radiales blancos, 12 (se tiñen y van con el material aditivo; victoria, guía 6.9).</summary>
        public Sprite LightRays { get; private set; } = null!;

        /// <summary>
        /// Velo negro con un hueco circular en el centro (victoria; revisión visual del 2026-10-07): mostrado a
        /// 2 × <see cref="VeilHoleHalfDp"/> dp de lado, es transparente hasta 150 dp del centro y opaco desde 320 dp, así el
        /// atenuado de la escena no cae bajo los rayos. El alfa lo pone la Image.
        /// </summary>
        public Sprite VeilHole { get; private set; } = null!;

        /// <summary>Medio lado del velo con hueco en pantalla (dp): cubre el lienzo desde cualquier centro.</summary>
        public const float VeilHoleHalfDp = 1100f;

        /// <summary>Poste de amarre con cuerda enrollada, 40 × 48 dp (guía 3.8).</summary>
        public Sprite RopePost { get; private set; } = null!;

        private void BuildScene()
        {
            Panorama = ArtSlots.Find(ArtSlots.Panorama) ?? Cached("bg_estuario_panorama", Mathf.RoundToInt(360 * ScenePxPerDp), Mathf.RoundToInt(PanoramaHeightDp * ScenePxPerDp), BuildPanoramaCanvas, new Vector2(0.5f, 1f), Vector4.zero, ScenePxPerDp);
            Dock = ArtSlots.Find(ArtSlots.Dock) ?? Cached("bg_estuario_muelle", Mathf.RoundToInt(360 * ScenePxPerDp), Mathf.RoundToInt(DockHeightDp * ScenePxPerDp), BuildDockCanvas, new Vector2(0.5f, 1f), Vector4.zero, ScenePxPerDp);
            int topPx = Mathf.RoundToInt(TopFoliageDp * FoliagePxPerDp);
            FoliageTopLeft = ArtSlots.Find(ArtSlots.FoliageTopLeft) ?? Cached("foliage_tl", topPx, topPx, () => BuildFoliageCanvas(0), new Vector2(0f, 1f), Vector4.zero, FoliagePxPerDp);
            FoliageTopRight = ArtSlots.Find(ArtSlots.FoliageTopRight) ?? Cached("foliage_tr", topPx, topPx, () => BuildFoliageCanvas(1), new Vector2(1f, 1f), Vector4.zero, FoliagePxPerDp);
            int bottomPx = Mathf.RoundToInt(BottomFoliageDp * FoliagePxPerDp);
            FoliageBottomLeft = ArtSlots.Find(ArtSlots.FoliageBottomLeft) ?? Cached("foliage_bl", bottomPx, bottomPx, () => BuildFoliageCanvas(2), new Vector2(0f, 0f), Vector4.zero, FoliagePxPerDp);
            FoliageBottomRight = ArtSlots.Find(ArtSlots.FoliageBottomRight) ?? Cached("foliage_br", bottomPx, bottomPx, () => BuildFoliageCanvas(3), new Vector2(1f, 0f), Vector4.zero, FoliagePxPerDp);
            PanelFill = BuildPanelFill();
            Sprite? frame = ArtSlots.Find(ArtSlots.PanelFrame);
            if (frame != null)
            {
                PanelFrame = frame;
                PanelFrameDp = 24f * 0.5f; // el PNG trae el marco dentro de su borde 9-slice de 24 dp; el relleno entra 12 dp
            }
            else
            {
                PanelFrame = BuildPanelFrame();
                PanelFrameDp = 1.5f + PanelFrameWoodDp; // contorno de 1,5 dp + marco de 12 dp
            }

            PanelInnerShadow = BuildInnerShadow();
            SoftShadow = BuildSoftShadow();
            Shelf = BuildShelf();
            Capi = ArtSlots.Find(ArtSlots.Capi);
            Heron = BuildHeron();
            LilyPad = BuildLilyPad();
            LightRays = BuildLightRays();
            VeilHole = BuildVeilHole();
            RopePost = BuildRopePost();
        }

        /// <summary>
        /// Herramienta de revisión: escribe en <paramref name="dir"/> los PNG del respaldo procedural de la escena
        /// (panorama, muelle y follaje) para mirarlos sin abrir el juego. No se usa en el juego.
        /// </summary>
        public static void ExportSceneFallbacks(string dir)
        {
            Directory.CreateDirectory(dir);
            Save(dir, "bg_estuario_panorama", BuildPanoramaCanvas());
            Save(dir, "bg_estuario_muelle", BuildDockCanvas());
            for (int corner = 0; corner < 4; corner++)
            {
                Save(dir, corner == 0 ? "foliage_tl" : corner == 1 ? "foliage_tr" : corner == 2 ? "foliage_bl" : "foliage_br", BuildFoliageCanvas(corner));
            }
        }

        private static void Save(string dir, string name, PixelCanvas pc)
        {
            var tex = new Texture2D(pc.Width, pc.Height, TextureFormat.RGBA32, false);
            tex.SetPixels32(pc.ToColor32());
            tex.Apply(false, false);
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        // Caché en disco (guía 4.9 [P]): las texturas grandes de la escena se guardan en persistentDataPath con la versión
        // del arte y del juego en la clave, y se leen desde el segundo arranque. En el Editor no se usa (se itera el arte).
        private Sprite Cached(string name, int w, int h, Func<PixelCanvas> build, Vector2 pivot, Vector4 border, float pxPerDp)
        {
            string? path = null;
            if (!Application.isEditor)
            {
                try
                {
                    string dir = Path.Combine(Application.persistentDataPath, "afart");
                    path = Path.Combine(dir, name + "_" + w + "x" + h + "_" + ArtVersion + "_" + Application.version + ".raw");
                    if (File.Exists(path))
                    {
                        byte[] raw = File.ReadAllBytes(path);
                        if (raw.Length == w * h * 4)
                        {
                            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                            {
                                name = name,
                                filterMode = FilterMode.Bilinear,
                                wrapMode = TextureWrapMode.Clamp,
                                hideFlags = HideFlags.DontSave,
                            };
                            tex.LoadRawTextureData(raw);
                            tex.Apply(false, true);
                            return SpriteFrom(tex, name, pivot, border, pxPerDp);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[AF.ProceduralArt] caché de " + name + " no disponible: " + e.Message);
                    path = null;
                }
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            PixelCanvas pc = build();
            watch.Stop();
            if (Debug.isDebugBuild)
            {
                Debug.Log("AF_ART " + name + " " + watch.ElapsedMilliseconds + " ms");
            }

            if (path != null)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    Color32[] px = pc.ToColor32();
                    var bytes = new byte[px.Length * 4];
                    for (int i = 0; i < px.Length; i++)
                    {
                        bytes[(i * 4) + 0] = px[i].r;
                        bytes[(i * 4) + 1] = px[i].g;
                        bytes[(i * 4) + 2] = px[i].b;
                        bytes[(i * 4) + 3] = px[i].a;
                    }

                    File.WriteAllBytes(path, bytes);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[AF.ProceduralArt] no se pudo guardar la caché de " + name + ": " + e.Message);
                }
            }

            return MakeSprite(pc, name, pivot, border, pxPerDp);
        }

        private Sprite SpriteFrom(Texture2D tex, string name, Vector2 pivot, Vector4 border, float pxPerDp)
        {
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 100f * pxPerDp, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            _owned.Add(tex);
            _owned.Add(sprite);
            return sprite;
        }

        // ------------------------------------------------------------------------------------------------------------
        // Panorama (guía 3.9 y 4.9, con las revisiones visuales del 2026-10-07): cielo de tres paradas que arriba es
        // turquesa, sol en el hueco entre los dos grupos de palmeras de açaí con un brillo aditivo de 120 dp y 3 rayos de
        // luz, nubes redondas con la sombra solo en su tercio inferior y orilla lejana con bruma baja. El río pardo se lee
        // como agua y no como arena: reflejo de Fresnel del cielo (L* 80-90 junto al horizonte, pardo cerca del panel; nunca
        // agua azul), reflejos verticales de las palmeras, reflejos de las orillas cortados por las ondas con borde suave,
        // ondas en perspectiva (fila i en horizonte + 1,2·i^1,7 dp, cada una con una línea clara y otra oscura 1 dp más
        // abajo) y una columna de destellos aditivos del sol de 24-32 dp. Coordenadas de diseño en dp (y hacia abajo) sobre
        // 360 × 540; se hornea a 2,25 px/dp (810 × 1215 px) para que los bordes no salgan en escalera al ampliarse.
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>Horizonte del panorama procedural, en dp desde arriba sobre 360 × 540 (guía 3.9).</summary>
        public const float PanoramaHorizonDp = 118f;

        /// <summary>Alto del panorama sobre un ancho de 360 dp (guía 3.4: 360 × 540).</summary>
        public const float PanoramaHeightDp = 540f;

        // Sol: en el hueco entre los dos grupos de palmeras (x 185-220) y bajo la insignia (revisión del 2026-10-07).
        private const float SunXDp = 202f;
        private const float SunYDp = 104f;
        private const float SunRadiusDp = 24f;

        /// <summary>X del sol del panorama procedural, en dp sobre 360 de ancho (la columna de su reflejo en StopBackdrop).</summary>
        public const float PanoramaSunXDp = SunXDp;

        // Rampa con la que los rayos del sol se apagan bajo el borde superior del panorama (revisión de D-004).
        private const float RayFadeTopDp = 48f;

        // Ondas en perspectiva (revisión del 2026-10-07): la fila i está en horizonte + RippleRowK · i^RippleRowExp dp.
        private const float RippleRowK = 1.2f;
        private const float RippleRowExp = 1.7f;
        private const float RippleMaxDp = 420f;

        // Palmeras de açaí en dos grupos centrales (x 125-185 y 220-280), coronas entre 70 y 118 dp: x y alto de la corona.
        private static readonly float[] AcaiPalms = { 131f, 76f, 149f, 68f, 166f, 82f, 182f, 92f, 226f, 86f, 242f, 70f, 259f, 79f, 275f, 94f };

        private static PixelCanvas BuildPanoramaCanvas()
        {
            const float k = ScenePxPerDp;
            int w = Mathf.RoundToInt(360 * k);
            int h = Mathf.RoundToInt(PanoramaHeightDp * k);
            var pc = new PixelCanvas(w, h);
            const float horizon = PanoramaHorizonDp;
            var sun = new Vector2(SunXDp, SunYDp);
            for (int y = 0; y < h; y++)
            {
                float yd = (h - y - 0.5f) / k; // dp desde arriba
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    Color c;
                    if (yd < horizon)
                    {
                        float t = yd / horizon;
                        c = t < 0.62f ? Color.Lerp(Palette.SkyTop, Palette.SkyMid, Mathf.SmoothStep(0f, 1f, t / 0.62f))
                            : Color.Lerp(Palette.SkyMid, Palette.SkyHorizon, Mathf.SmoothStep(0f, 1f, (t - 0.62f) / 0.38f));

                        // Halo del sol (cielo_horizonte) y disco de 24 dp: el punto más luminoso de la escena.
                        float ds = Vector2.Distance(new Vector2(xd, yd), sun);
                        float halo = Mathf.Clamp01(1f - (ds / 110f));
                        c = Color.Lerp(c, Palette.SkyHorizon, 0.55f * halo * halo);
                        float inner = Mathf.Clamp01(1f - (ds / 46f));
                        c = Color.Lerp(c, Palette.Sun, 0.6f * inner * inner);
                        float disc = PixelCanvas.Coverage((ds - SunRadiusDp) * k);
                        c = Color.Lerp(c, Palette.Sun, disc);
                    }
                    else
                    {
                        // Río pardo: rio_base al pie del horizonte, #8A6C48 a +60 dp y #6B5640 a +140 dp.
                        float t = yd - horizon;
                        c = t < 60f ? Color.Lerp(Palette.RiverBase, Palette.RiverLow, Mathf.SmoothStep(0f, 1f, t / 60f))
                            : Color.Lerp(Palette.RiverLow, Palette.RiverFar, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 60f) / 80f)));

                        // Reflejo de Fresnel del cielo: 0,95 junto al horizonte que cae con exp(−t / 22 dp) hasta un suelo
                        // de 0,10; el tono va de cielo_horizonte a cielo_medio en 50 dp. La revisión proponía 0,70 + 0,10,
                        // pero con las ondas y la bruma encima el agua junto al horizonte se quedaba en L* 75-78 en la
                        // captura; con 0,85 + 0,10 llega a L* 80-90, que era su criterio.
                        Color sky = Color.Lerp(Palette.SkyHorizon, Palette.SkyMid, Mathf.Clamp01(t / 50f));
                        c = Color.Lerp(c, sky, (0.85f * Mathf.Exp(-t / 22f)) + 0.10f);

                        // Columna cálida bajo el sol (gaussiana de σ 18 dp), debajo de los destellos.
                        float col = Mathf.Exp(-((xd - sun.x) * (xd - sun.x)) / (2f * 18f * 18f));
                        c = Color.Lerp(c, Palette.RiverReflect, 0.55f * col * Mathf.Exp(-t / 150f));
                    }

                    pc.Set(x, y, c);
                }
            }

            DrawClouds(pc, k);
            DrawSunGlow(pc, k, sun, horizon);
            DrawFarShore(pc, k, horizon);
            DrawFarShoreReflection(pc, k, horizon, sun.x);
            DrawSideBanks(pc, k, horizon);
            DrawWindStreaks(pc, k, horizon);
            DrawPalmReflections(pc, k, horizon);
            DrawRipples(pc, k, horizon);
            DrawSunGlints(pc, k, sun.x, horizon);
            return pc;
        }

        // Distancia (dp bajo el horizonte) de la fila de ondas i.
        private static float RippleRowT(int i)
        {
            return RippleRowK * Mathf.Pow(i, RippleRowExp);
        }

        // Grosor de las ondas a t dp bajo el horizonte: de 0,75 dp junto al horizonte a 2 dp a 260 dp.
        private static float RippleThickness(float t)
        {
            return Mathf.Lerp(0.75f, 2f, Mathf.Clamp01(t / 260f));
        }

        // Corte de los reflejos por las ondas a t dp bajo el horizonte: 1 sobre la línea de una fila y 0 lejos de ella,
        // con borde suave de 1,5 px (en lugar de los huecos de 1 dp sin suavizado de la versión anterior).
        private static float RippleCut(float t, float k)
        {
            if (t < 0.6f || t > RippleMaxDp)
            {
                return 0f;
            }

            int i0 = Mathf.Max(1, Mathf.FloorToInt(Mathf.Pow(t / RippleRowK, 1f / RippleRowExp)));
            float best = Mathf.Min(Mathf.Abs(t - RippleRowT(i0)), Mathf.Abs(t - RippleRowT(i0 + 1)));
            return PixelCanvas.SoftCoverage((best - (0.5f * RippleThickness(t))) * k, 1.5f);
        }

        private static void DrawClouds(PixelCanvas pc, float k)
        {
            // Todas por debajo de y = 6 dp: con el horizonte bajo, el panorama se desplaza y su borde superior se ve.
            DrawCloud(pc, k, 46f, 30f, 1.0f, 3);
            DrawCloud(pc, k, 248f, 31f, 1.2f, 7);
            DrawCloud(pc, k, 330f, 66f, 0.8f, 13);
            DrawCloud(pc, k, 112f, 62f, 0.55f, 19);
        }

        private static void DrawCloud(PixelCanvas pc, float k, float cx, float cy, float scale, int seed)
        {
            // Cúmulo redondeado: unión de lóbulos, sin base plana; nube_luz y la sombra cálida solo en el tercio inferior.
            const int lobes = 6;
            float x0 = (cx - (46f * scale)) * k;
            float x1 = (cx + (46f * scale)) * k;
            float yTop = (cy - (20f * scale)) * k;
            float yBase = (cy + (14f * scale)) * k;
            float top = cy - (18f * scale);
            float bottom = cy + (10f * scale);
            int h = pc.Height;
            for (int py = Mathf.FloorToInt(yTop) - 2; py <= Mathf.CeilToInt(yBase) + 2; py++)
            {
                for (int px = Mathf.FloorToInt(x0) - 2; px <= Mathf.CeilToInt(x1) + 2; px++)
                {
                    float xd = (px + 0.5f) / k;
                    float yd = (py + 0.5f) / k;
                    float best = 1e9f;
                    for (int i = 0; i < lobes; i++)
                    {
                        float off = i - ((lobes - 1) * 0.5f);
                        float lx = cx + (off * 13f * scale);
                        float r = (9f + (ArtNoise.Hash(i, seed, 5) * 9f)) * scale * (1f - (Mathf.Abs(off) * 0.12f));
                        float ly = cy + (2f * scale) - (r * 0.55f);
                        best = Mathf.Min(best, Vector2.Distance(new Vector2(xd, yd), new Vector2(lx, ly)) - r);

                        // Lóbulos bajos más pequeños: redondean la base sin aplanarla.
                        if (i > 0 && i < lobes - 1)
                        {
                            float rb = r * 0.55f;
                            best = Mathf.Min(best, Vector2.Distance(new Vector2(xd, yd), new Vector2(lx + (3f * scale), cy + (4f * scale))) - rb);
                        }
                    }

                    float cov = PixelCanvas.SoftCoverage(best * k, 2.5f);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float t = Mathf.Clamp01((yd - top) / (bottom - top));
                    Color c = Color.Lerp(Palette.CloudLight, Palette.CloudShadow, Smooth(0.62f, 1f, t) * 0.85f);
                    c.a = 0.95f;
                    pc.Blend(px, h - 1 - py, c, cov);
                }
            }
        }

        private static void DrawSunGlow(PixelCanvas pc, float k, Vector2 sun, float horizon)
        {
            // Brillo aditivo del sol (#FFF6D6, radio 120 dp, 0,5 en el centro) y 3 rayos de luz suaves (alfa 0,12) que
            // suben desde el disco hacia el cielo. Solo por encima del horizonte: nunca tocan el panel (revisión visual del
            // 2026-10-07: «no hay luz cálida que brille»). Los rayos se apagan en los 48 dp de arriba del panorama: con el
            // horizonte bajo, su borde superior queda a la vista y por encima solo hay cielo_alto liso (revisión de D-004:
            // sin la rampa se veía una costura horizontal en el cielo del nivel 1).
            int h = pc.Height;
            float[] rayDeg = { 58f, 97f, 138f }; // 0° = derecha, 90° = arriba
            for (int py = 0; py < h; py++)
            {
                float yd = (h - py - 0.5f) / k;
                if (yd > horizon + 2f)
                {
                    continue;
                }

                float fadeY = Mathf.Clamp01((horizon + 2f - yd) / 4f);
                for (int px = 0; px < pc.Width; px++)
                {
                    float xd = (px + 0.5f) / k;
                    float dx = xd - sun.x;
                    float dy = sun.y - yd; // hacia arriba
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float glow = Mathf.Clamp01(1f - (d / 120f));
                    float amount = 0.5f * glow * glow;
                    if (d > SunRadiusDp && dy > -8f)
                    {
                        float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        float rays = 0f;
                        for (int r = 0; r < rayDeg.Length; r++)
                        {
                            float off = Mathf.Abs(ang - rayDeg[r]);
                            rays = Mathf.Max(rays, 1f - Smooth(1.5f, 5f, off));
                        }

                        amount += 0.12f * rays * Mathf.Clamp01(1f - (d / 320f)) * Smooth(SunRadiusDp, SunRadiusDp + 24f, d) * Smooth(0f, RayFadeTopDp, yd);
                    }

                    if (amount > 0.002f)
                    {
                        pc.AddLight(px, py, Palette.Sun, amount * fadeY);
                    }
                }
            }
        }

        private static void DrawFarShore(PixelCanvas pc, float k, float horizon)
        {
            int h = pc.Height;
            // Línea de selva lejana con bruma (orilla_lejana), de 4 a 10 dp sobre el horizonte.
            for (int x = 0; x < pc.Width; x++)
            {
                float xd = (x + 0.5f) / k;
                float top = FarShoreTop(xd, horizon);
                for (float yd = top - 1f; yd < horizon + 1.5f; yd += 1f / k)
                {
                    int py = h - 1 - Mathf.FloorToInt(yd * k);
                    float cov = Mathf.Clamp01((yd - top) * k);
                    Color c = Color.Lerp(Palette.FarShore, Palette.FarPalm, 0.25f + (0.35f * ArtNoise.Value2(xd / 4f, yd / 3f, 29)));
                    pc.Blend(x, py, c, cov);
                }
            }

            for (int i = 0; i < AcaiPalms.Length; i += 2)
            {
                DrawAcaiPalm(pc, k, AcaiPalms[i], horizon - 1f, AcaiPalms[i + 1], 0.9f + (ArtNoise.Hash(i, 3, 41) * 0.3f), i);
            }

            // Bruma baja sobre el horizonte (07 §2.1: "alba dorada, bruma baja").
            for (int y = 0; y < h; y++)
            {
                float yd = (h - y - 0.5f) / k;
                float m = Mathf.Exp(-((yd - (horizon - 2f)) * (yd - (horizon - 2f))) / (2f * 5.5f * 5.5f)) * 0.38f;
                if (m < 0.01f)
                {
                    continue;
                }

                for (int x = 0; x < pc.Width; x++)
                {
                    pc.Blend(x, y, Palette.SkyHorizon, m * (0.75f + (0.25f * ArtNoise.Value1(x / (18f * k), 47))));
                }
            }
        }

        // Borde superior de la selva lejana en x (dp desde arriba).
        private static float FarShoreTop(float xd, float horizon)
        {
            return horizon - 3.5f - (6f * ArtNoise.Fbm2(xd / 9f, 0.5f, 23));
        }

        private static void DrawAcaiPalm(PixelCanvas pc, float k, float x, float baseY, float crownY, float scale, int seed)
        {
            int h = pc.Height;
            Color trunk = ColorMath.Darken(Palette.FarPalm, 0.05f);
            float lean = (ArtNoise.Hash(seed, 1, 9) - 0.5f) * 6f;
            var b = new Vector2(x * k, h - (baseY * k));
            var c = new Vector2((x + lean) * k, h - (crownY * k));
            var mid = Vector2.Lerp(b, c, 0.5f) + new Vector2(lean * 0.4f * k, 0f);
            pc.FillCapsule(b.x, b.y, mid.x, mid.y, 0.75f * k, trunk);
            pc.FillCapsule(mid.x, mid.y, c.x, c.y, 0.65f * k, trunk);

            // Fronda pinnada colgante: raquis curvo con foliolos que caen.
            int fronds = 8;
            for (int f = 0; f < fronds; f++)
            {
                float ang = Mathf.Lerp(-170f, -10f, f / (float)(fronds - 1)) + ((ArtNoise.Hash(f, seed, 3) - 0.5f) * 18f);
                float len = (16f + (ArtNoise.Hash(f, seed, 4) * 6f)) * scale * k;
                float rad = ang * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad) * 0.55f + 0.25f); // en px con y hacia arriba
                dir.Normalize();
                Vector2 prev = c;
                for (int s = 1; s <= 6; s++)
                {
                    float t = s / 6f;
                    Vector2 p = c + (dir * len * t) + new Vector2(0f, -len * 0.55f * t * t);
                    pc.FillCapsule(prev.x, prev.y, p.x, p.y, 0.45f * k, Palette.FarPalm);
                    // Foliolos colgantes.
                    Vector2 tangent = (p - prev).normalized;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 leaflet = p + (new Vector2(-tangent.y * side, tangent.x * side) * 2.2f * k * (1f - (t * 0.4f))) + new Vector2(0f, -3.5f * k * (1f - (t * 0.3f)));
                        pc.FillCapsule(p.x, p.y, leaflet.x, leaflet.y, 0.35f * k, Palette.FarPalm);
                    }

                    prev = p;
                }
            }

            pc.FillCircle(c.x, c.y, 1.3f * k, trunk);
        }

        private static void DrawSideBanks(PixelCanvas pc, float k, float horizon)
        {
            // Orillas de selva saturada a los lados del horizonte (selva_media, selva_luz, selva_sombra), fuera de los dos
            // grupos de palmeras, cada una con su reflejo especular en el agua (selva_sombra → selva_media al 66 %, de
            // 0,8 × el alto de la orilla) y una línea de agua clara de 1,5 dp al pie. Los reflejos los cortan las filas de
            // ondas con borde suave (RippleCut), no huecos sin suavizado.
            int h = pc.Height;
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                float reachTop = left ? 104f : 100f;
                for (int x = 0; x < pc.Width; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float fromEdge = left ? xd : 360f - xd;
                    if (fromEdge > reachTop + 12f)
                    {
                        continue;
                    }

                    float reach = Mathf.Clamp01((reachTop + 12f - fromEdge) / 12f);

                    // Perfil de copas: más alto junto al borde y bajando hacia el centro.
                    float profile = 1f - Mathf.Clamp01(fromEdge / reachTop);
                    float top = horizon - 4f - (profile * profile * 34f) - (5f * ArtNoise.Fbm2(xd / 7f, side * 3f, 71));
                    float bottom = horizon + 2f + (profile * 5f);
                    for (float yd = top - 1f; yd <= bottom; yd += 1f / k)
                    {
                        int py = h - 1 - Mathf.FloorToInt(yd * k);
                        float cov = Mathf.Clamp01((yd - top) * k) * reach;
                        float n = ArtNoise.Value2(xd / 5f, yd / 5f, 79 + side);
                        Color c = Color.Lerp(Palette.JungleShadow, Palette.JungleMid, Mathf.SmoothStep(0.15f, 0.7f, n));
                        float lit = Mathf.Clamp01(1f - ((yd - top) / 10f)) * (left ? 1f : 0.7f);
                        c = Color.Lerp(c, Palette.JungleLight, 0.6f * lit * Mathf.SmoothStep(0.35f, 0.8f, n));
                        c = Color.Lerp(c, Palette.FarShore, 0.25f * (1f - profile)); // bruma hacia el centro
                        pc.Blend(x, py, c, cov);
                    }

                    // Reflejo especular de la orilla, cortado por las ondas.
                    float bankH = bottom - top;
                    float reflectH = 0.8f * bankH * (0.88f + (0.12f * Mathf.Sin((xd * 0.31f) + side)));
                    for (float yd = bottom; yd < bottom + reflectH; yd += 1f / k)
                    {
                        float t = (yd - bottom) / reflectH;
                        float cut = t > 0.12f ? RippleCut(yd - horizon + (0.6f * Mathf.Sin((xd * 0.21f) + (side * 2f))), k) : 0f;
                        int py = h - 1 - Mathf.FloorToInt(yd * k);
                        Color c = Color.Lerp(Palette.JungleShadow, Palette.JungleMid, t);
                        float tail = 1f - Smooth(0.7f, 1f, t);
                        pc.Blend(x, py, c, 0.66f * tail * reach * (1f - (0.85f * cut)));
                    }

                    // Orilla cercana: la selva de los lados se refleja en el agua junto a los bordes, en todo el alto del
                    // río (un velo verde que se apaga hacia el centro y también lo cortan las ondas).
                    float near = Mathf.Clamp01(1f - (fromEdge / 64f));
                    if (near > 0f)
                    {
                        float veil = 0.5f * near * Mathf.Sqrt(near);
                        for (float yd = bottom + 1f; yd < PanoramaHeightDp; yd += 1f / k)
                        {
                            float cut = RippleCut(yd - horizon + (0.6f * Mathf.Sin(xd * 0.17f)), k);
                            int py = h - 1 - Mathf.FloorToInt(yd * k);
                            pc.Blend(x, py, Palette.JungleMid, veil * (1f - (0.85f * cut)));
                        }
                    }

                    // Línea de agua de 1,5 dp al pie de la orilla.
                    if (profile > 0.04f)
                    {
                        for (float yd = bottom - 0.75f; yd < bottom + 0.75f; yd += 1f / k)
                        {
                            int py = h - 1 - Mathf.FloorToInt(yd * k);
                            pc.Blend(x, py, Palette.WaterLine, 0.6f * reach * Mathf.Clamp01(profile * 6f));
                        }
                    }
                }
            }
        }

        private static void DrawFarShoreReflection(PixelCanvas pc, float k, float horizon, float sunX)
        {
            // Reflejo de la selva lejana justo bajo el horizonte: tan fino como la orilla (0,8 × su alto, 3-8 dp), con
            // bruma (palmera_lejana → orilla_lejana al 32 %), salvo bajo el sol. Así no apaga el reflejo de Fresnel del
            // cielo, que es lo más claro del agua.
            int h = pc.Height;
            for (int x = 0; x < pc.Width; x++)
            {
                float xd = (x + 0.5f) / k;
                float reflectH = 0.8f * (horizon - FarShoreTop(xd, horizon));
                float sunCol = Mathf.Exp(-((xd - sunX) * (xd - sunX)) / (2f * 14f * 14f));
                for (float yd = horizon; yd < horizon + reflectH + 1f; yd += 1f / k)
                {
                    float t = (yd - horizon) / Mathf.Max(0.5f, reflectH);
                    float cut = RippleCut(yd - horizon, k);
                    int py = h - 1 - Mathf.FloorToInt(yd * k);
                    Color c = Color.Lerp(Palette.FarPalm, Palette.FarShore, 0.4f + (t * 0.4f));
                    pc.Blend(x, py, c, 0.32f * (1f - Smooth(0.6f, 1f, t)) * (1f - (0.85f * sunCol)) * (1f - (0.8f * cut)));
                }
            }
        }

        private static void DrawPalmReflections(PixelCanvas pc, float k, float horizon)
        {
            // Bajo cada palmera de açaí, un trazo vertical de palmera_lejana al 40 %, de 1,5 dp de ancho y 0,7 × el alto
            // de la palmera, con una leve ondulación, que termina en el reflejo de la corona y que cortan las ondas
            // (revisión visual del 2026-10-07).
            int h = pc.Height;
            const float half = 0.75f;
            for (int i = 0; i < AcaiPalms.Length; i += 2)
            {
                float px = AcaiPalms[i];
                float len = 0.7f * (horizon - 1f - AcaiPalms[i + 1]);
                float cy = horizon + len;
                int py0 = Mathf.FloorToInt(horizon * k);
                int py1 = Mathf.CeilToInt((cy + 4f) * k);
                for (int pyd = py0; pyd <= py1; pyd++)
                {
                    float yd = (pyd + 0.5f) / k;
                    float t = yd - horizon;
                    float tt = Mathf.Clamp01(t / len);
                    float cut = RippleCut(t, k);
                    float wob = Mathf.Sin((t * 0.9f) + i) * (0.25f + (0.03f * t));
                    int py = h - 1 - pyd;
                    float trunk = t <= len ? 0.40f * (1f - (0.5f * tt)) : 0f;
                    for (int x = Mathf.FloorToInt((px - 9f) * k); x <= Mathf.CeilToInt((px + 9f) * k); x++)
                    {
                        float xd = (x + 0.5f) / k;
                        float a = trunk * PixelCanvas.Coverage((Mathf.Abs(xd - px - wob) - half) * k);

                        // Corona reflejada: elipse suave de 7 × 3 dp al pie del trazo.
                        float ex = (xd - px - wob) / 7f;
                        float ey = (yd - cy) / 3f;
                        float crown = 0.20f * PixelCanvas.SoftCoverage((Mathf.Sqrt((ex * ex) + (ey * ey)) - 1f) * 3f * k, 2f * k);
                        a = Mathf.Max(a, crown);
                        if (a > 0.002f)
                        {
                            pc.Blend(x, py, Palette.FarPalm, a * (1f - (0.8f * cut)));
                        }
                    }
                }
            }
        }

        private static void DrawWindStreaks(PixelCanvas pc, float k, float horizon)
        {
            // Franjas de viento: bandas horizontales largas y suaves, unas más claras (cielo reflejado en agua lisa,
            // cielo_medio al 22 %) y otras más oscuras (agua rizada, rio_profundo al 16 %), más finas y juntas junto al
            // horizonte. Es la textura que separa una lámina de agua de un camino de tierra.
            int h = pc.Height;
            const int bands = 14;
            for (int b = 0; b < bands; b++)
            {
                float tt = (b + 0.5f + ((ArtNoise.Hash(b, 1, 301) - 0.5f) * 0.6f)) / bands;
                float t = 4f + (tt * tt * 230f);
                float thick = Mathf.Lerp(1.5f, 7f, Mathf.Clamp01(t / 230f));
                float cx = ArtNoise.Hash(b, 2, 301) * 360f;
                float halfLen = Mathf.Lerp(70f, 170f, ArtNoise.Hash(b, 3, 301));
                bool light = (b % 2) == 0;
                Color c = ColorMath.WithAlpha(light ? Palette.SkyMid : Palette.RiverDeep, light ? 0.22f : 0.16f);
                float yc = horizon + t;
                for (int py = Mathf.FloorToInt((yc - (thick * 1.2f)) * k); py <= Mathf.CeilToInt((yc + (thick * 1.2f)) * k); py++)
                {
                    float yd = (py + 0.5f) / k;
                    float vy = 1f - Smooth(0.35f, 1.15f, Mathf.Abs(yd - yc) / thick);
                    if (vy <= 0f)
                    {
                        continue;
                    }

                    for (int px = 0; px < pc.Width; px++)
                    {
                        float xd = (px + 0.5f) / k;
                        float dx = Mathf.Abs(Mathf.Repeat(xd - cx + 180f, 360f) - 180f);
                        float hx = 1f - Smooth(0.5f, 1f, dx / halfLen);
                        if (hx > 0f)
                        {
                            pc.Blend(px, h - 1 - py, c, vy * hx);
                        }
                    }
                }
            }
        }

        private static void DrawRipples(PixelCanvas pc, float k, float horizon)
        {
            // Ondas en perspectiva (revisión visual del 2026-10-07): filas en horizonte + 1,2·i^1,7 dp; los guiones crecen
            // de 8 a 40 dp de largo y de 0,75 a 2 dp de grosor al alejarse del horizonte. Cada onda lleva una línea clara
            // (cielo_horizonte al 55 %) con una oscura 1 dp más abajo (rio_profundo al 30 %). Reparto estratificado: un
            // guion por celda del ancho, para que no queden racimos ni huecos.
            int h = pc.Height;
            for (int i = 1; ; i++)
            {
                float t = RippleRowT(i);
                if (t > RippleMaxDp)
                {
                    break;
                }

                float f = Mathf.Clamp01(t / 260f);
                float th = RippleThickness(t);
                float baseLen = Mathf.Lerp(8f, 40f, f);
                int strokes = Mathf.Max(3, Mathf.CeilToInt(360f * 0.30f / baseLen));
                float cell = 360f / strokes;
                float shift = ArtNoise.Hash(i, 0, 127) * cell;
                float fade = 1f - (0.30f * f);
                for (int j = 0; j < strokes; j++)
                {
                    float len = baseLen * (0.6f + (0.8f * ArtNoise.Hash(i, j, 131)));
                    float xd = Mathf.Repeat(shift + ((j + 0.2f + (0.6f * ArtNoise.Hash(i, j, 137))) * cell), 360f);
                    float yy = horizon + t + ((ArtNoise.Hash(i, j, 139) - 0.5f) * 0.5f * th);

                    // Racimos: el rizado se agrupa en zonas (ruido de baja frecuencia) en lugar de repartirse por igual,
                    // que se leía como las marcas de una playa.
                    float cluster = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((ArtNoise.Value2(xd / 70f, i * 0.37f, 149) - 0.25f) / 0.5f));
                    float a = fade * (0.15f + (0.85f * cluster));
                    if (a < 0.2f)
                    {
                        continue;
                    }

                    // La línea oscura se aclara junto al horizonte, donde manda el reflejo del cielo (L* 80-90).
                    float dark = 0.30f * Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(t / 40f));
                    DrawRippleLine(pc, k, h, xd - (len * 0.5f), xd + (len * 0.5f), yy + 1f, th, Palette.RiverDeep, dark * a, false);
                    DrawRippleLine(pc, k, h, xd - (len * 0.5f), xd + (len * 0.5f), yy, th, Palette.SkyHorizon, 0.55f * a, false);
                }
            }
        }

        // Línea horizontal de grosor th (dp) con los extremos afilados, de x0 a x1 (dp) a la altura yd (dp desde arriba);
        // aditiva (luz en lineal, como la GPU) o mezclada.
        private static void DrawRippleLine(PixelCanvas pc, float k, int h, float x0, float x1, float yd, float th, Color c, float alpha, bool additive)
        {
            float half = th * 0.5f;
            float mid = (x0 + x1) * 0.5f;
            float halfLen = (x1 - x0) * 0.5f;
            int py0 = Mathf.FloorToInt((yd - half - 1f) * k);
            int py1 = Mathf.CeilToInt((yd + half + 1f) * k);
            Color blend = ColorMath.WithAlpha(c, alpha);
            for (int py = py0; py <= py1; py++)
            {
                float ydp = (py + 0.5f) / k;
                for (int px = Mathf.FloorToInt(x0 * k); px <= Mathf.CeilToInt(x1 * k); px++)
                {
                    float xd = (px + 0.5f) / k;
                    float u = Mathf.Abs(xd - mid) / Mathf.Max(0.5f, halfLen); // 0 en el centro, 1 en la punta
                    float local = half * (1f - (u * u));
                    float cov = PixelCanvas.Coverage((Mathf.Abs(ydp - yd) - local) * k) * Mathf.Clamp01((1f - u) * 4f);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    if (additive)
                    {
                        pc.AddLight(px, h - 1 - py, c, alpha * cov);
                    }
                    else
                    {
                        pc.Blend(px, h - 1 - py, blend, cov);
                    }
                }
            }
        }

        private static void DrawSunGlints(PixelCanvas pc, float k, float sunX, float horizon)
        {
            // Columna de destellos del sol (revisión visual del 2026-10-07): de 24 dp de ancho junto al horizonte a 32 dp a
            // 130 dp, aditiva, destello con alfa 0,9 → 0,6, desde el horizonte hasta más allá del panel en todos los
            // niveles. Los destellos siguen las filas de ondas, 2 o 3 por fila.
            int h = pc.Height;
            for (int i = 1; ; i++)
            {
                float t = RippleRowT(i);
                if (t > 130f)
                {
                    break;
                }

                float f = t / 130f;
                float halfCol = Mathf.Lerp(12f, 16f, f);
                float th = RippleThickness(t) * 1.1f;
                int n = 2 + (i % 2);
                for (int j = 0; j < n; j++)
                {
                    float xd = sunX + ((((j + 0.5f) / n) - 0.5f) * 2f * halfCol) + ((ArtNoise.Hash(i, j, 61) - 0.5f) * (2f * halfCol / n));
                    float len = Mathf.Lerp(3f, 11f, f) * (0.7f + (0.6f * ArtNoise.Hash(i, j, 67)));
                    DrawRippleLine(pc, k, h, xd - (len * 0.5f), xd + (len * 0.5f), horizon + t, th, Palette.Glint, Mathf.Lerp(0.9f, 0.6f, f), true);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        // Muelle (guía 4.9, con las revisiones visuales del 2026-10-07): tablas de 18 a 30 dp con juntas de 2 dp de ranura,
        // viga frontal de 14 dp con su borde superior a 72 dp del borde del sprite (transparente por encima). La cara de
        // las tablas sube a L* 64-68 en el tercio superior (muelle_soleado #C99A6A) y baja hacia el tono base; encima, un
        // degradado cálido desde arriba a la izquierda y 7 manchas de luz moteada de hojas (aditivas, cielo_horizonte
        // #FFE4A8 al 18-25 %, de 40 a 90 dp) en lugar de las bandas diagonales, que se leían como la sombra de una persiana.
        // Sin postes: el panel del tablero se apoya en la viga. El sprite mide 360 × 480 dp para cubrir hasta el pie de la
        // pantalla aunque la viga suba en los niveles de una fila.
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>Alto del muelle procedural (dp, sobre 360 de ancho).</summary>
        public const float DockHeightDp = 480f;

        /// <summary>Borde superior de la viga frontal dentro del sprite del muelle (dp desde arriba; guía 3.4).</summary>
        public const float DockBeamTopDp = 72f;

        // Manchas de luz moteada sobre las tablas: centro (x, y en dp del sprite), diámetro (dp) y alfa aditivo.
        private static readonly float[] DockDapples =
        {
            62f, 118f, 84f, 0.25f,
            176f, 104f, 52f, 0.20f,
            292f, 132f, 70f, 0.23f,
            118f, 196f, 60f, 0.21f,
            246f, 222f, 90f, 0.19f,
            34f, 292f, 46f, 0.18f,
            330f, 318f, 56f, 0.20f,
        };

        private static PixelCanvas BuildDockCanvas()
        {
            const float k = ScenePxPerDp;
            int w = Mathf.RoundToInt(360 * k);
            int h = Mathf.RoundToInt(DockHeightDp * k);
            var pc = new PixelCanvas(w, h);
            const float beamTop = DockBeamTopDp;
            const float beamH = 14f;

            // Tablas.
            float y0 = beamTop + beamH;
            int row = 0;
            while (y0 < DockHeightDp)
            {
                float plank = Mathf.Lerp(18f, 30f, Mathf.Clamp01((y0 - 86f) / 230f));
                float y1 = Mathf.Min(DockHeightDp, y0 + plank);
                float tone = ArtNoise.Hash(row, 0, 83);
                Color baseC = Color.Lerp(Palette.DockBase, Palette.DockLight, 0.15f + (0.45f * tone));
                float joint = 40f + (ArtNoise.Hash(row, 1, 83) * 200f);
                float joint2 = joint + 110f + (ArtNoise.Hash(row, 2, 83) * 60f);
                for (int py = Mathf.FloorToInt(y0 * k); py < Mathf.CeilToInt(y1 * k) && py < h; py++)
                {
                    float yd = (py + 0.5f) / k;
                    float local = (yd - y0) / Mathf.Max(1f, y1 - y0);

                    // Cara soleada en el tercio superior del muelle (L* 64-68), que baja al tono base hacia la mitad.
                    float sun = 1f - Smooth(beamTop + 90f, beamTop + 190f, yd);
                    Color face = Color.Lerp(baseC, ColorMath.Lighten(Palette.DockSunlit, 0.05f * (tone - 0.5f)), 0.85f * sun);
                    for (int px = 0; px < w; px++)
                    {
                        float xd = (px + 0.5f) / k;
                        Color c = face;
                        float grain = ArtNoise.Value2(xd / 38f, (yd * 1.7f) + (row * 13f), 89 + row);
                        float line = Mathf.Abs(Mathf.Sin(((yd + (grain * 6f)) * 1.25f) + row));
                        c = ColorMath.Darken(c, line < 0.12f ? 0.10f : 0f);
                        c = Color.Lerp(c, Palette.DockSunlit, local < 0.12f ? 0.40f * (1f - (local / 0.12f)) : 0f); // canto iluminado
                        c = ColorMath.Darken(c, local > 0.8f ? 0.18f * ((local - 0.8f) / 0.2f) : 0f); // sombra abajo
                        float jd = Mathf.Min(Mathf.Abs(xd - joint), Mathf.Abs(xd - joint2));
                        if (jd < 1.2f)
                        {
                            c = Color.Lerp(c, Palette.Groove, 0.92f * Mathf.Clamp01(1.2f - jd)); // junta a tope más oscura
                        }

                        c = ColorMath.Darken(c, 0.18f * Mathf.Clamp01((yd - 260f) / 160f)); // viñeta abajo (≤ 25 %)
                        pc.Set(px, h - 1 - py, c);
                    }
                }

                // Clavos junto a cada junta.
                float plankMid = (y0 + y1) * 0.5f;
                for (int nail = 0; nail < 4; nail++)
                {
                    float nx = (nail < 2 ? joint : joint2) + ((nail % 2) == 0 ? -4f : 4f);
                    float ny = h - (plankMid * k);
                    pc.FillCircle(nx * k, ny, 1.1f * k, ColorMath.Darken(baseC, 0.55f));
                    pc.FillCircle((nx - 0.3f) * k, ny + (0.3f * k), 0.45f * k, ColorMath.Lighten(baseC, 0.35f));
                }

                // Junta de 2 dp entre tablas, en ranura casi opaca (más oscura que antes, revisión del 2026-10-07).
                for (int px = 0; px < w; px++)
                {
                    for (int j = 0; j < Mathf.CeilToInt(2f * k); j++)
                    {
                        int py = Mathf.FloorToInt(y0 * k) + j;
                        if (py < h)
                        {
                            pc.Blend(px, h - 1 - py, Palette.Groove, 0.95f);
                        }
                    }
                }

                y0 = y1;
                row++;
            }

            // Viga frontal.
            for (int py = Mathf.FloorToInt(beamTop * k); py < Mathf.CeilToInt((beamTop + beamH) * k); py++)
            {
                float yd = (py + 0.5f) / k;
                float t = (yd - beamTop) / beamH;
                for (int px = 0; px < w; px++)
                {
                    float xd = (px + 0.5f) / k;
                    Color c = Color.Lerp(Palette.DockLight, Palette.DockShadow, Mathf.SmoothStep(0f, 1f, t));
                    float g = ArtNoise.Value2(xd / 30f, yd * 1.5f, 97);
                    c = ColorMath.Darken(c, Mathf.Abs(Mathf.Sin((yd + (g * 4f)) * 2.1f)) < 0.1f ? 0.1f : 0f);
                    if (t < 0.12f)
                    {
                        c = Color.Lerp(c, Palette.ShelfEdge, 0.55f);
                    }

                    float bolt = Mathf.Repeat(xd + 20f, 60f) - 30f;
                    float bd = Mathf.Sqrt((bolt * bolt) + ((yd - (beamTop + 7f)) * (yd - (beamTop + 7f))));
                    if (bd < 2.2f)
                    {
                        c = Color.Lerp(Palette.Groove, Palette.DockLight, Mathf.Clamp01((beamTop + 6f - yd) * 0.6f));
                    }

                    pc.Set(px, h - 1 - py, c);
                }
            }

            // Sombra de la viga sobre las tablas.
            for (int py = Mathf.CeilToInt((beamTop + beamH) * k); py < Mathf.CeilToInt((beamTop + beamH + 8f) * k); py++)
            {
                float t = ((py / k) - (beamTop + beamH)) / 8f;
                for (int px = 0; px < w; px++)
                {
                    pc.Multiply(px, h - 1 - py, 0.7f, 1f - t);
                }
            }

            DrawDockLight(pc, k, beamTop + beamH);
            return pc;
        }

        private static void DrawDockLight(PixelCanvas pc, float k, float planksTop)
        {
            // Luz sobre las tablas (07 §1.2: desde arriba a la izquierda): degradado cálido aditivo (cielo_horizonte, 0,16
            // en la esquina y 0 a 420 dp) y manchas de luz moteada de hojas con el borde irregular (unión de tres elipses
            // por mancha) y suave (8 dp). Todo aditivo, como la luz de la GPU (guía 2.5).
            int h = pc.Height;
            for (int py = 0; py < h; py++)
            {
                float yd = DockHeightDp - ((py + 0.5f) / k);
                if (yd < planksTop)
                {
                    continue;
                }

                for (int px = 0; px < pc.Width; px++)
                {
                    float xd = (px + 0.5f) / k;
                    float dCorner = Mathf.Sqrt((xd * xd) + ((yd - planksTop) * (yd - planksTop)));
                    float warm = 1f - Mathf.Clamp01(dCorner / 420f);
                    float amount = 0.16f * warm * warm;
                    for (int i = 0; i < DockDapples.Length; i += 4)
                    {
                        float cx = DockDapples[i];
                        float cy = DockDapples[i + 1];
                        float r = DockDapples[i + 2] * 0.5f;
                        if (Mathf.Abs(xd - cx) > r + 12f || Mathf.Abs(yd - cy) > r + 12f)
                        {
                            continue;
                        }

                        // Racimo de 8 motas de sol aplanadas (la luz cae en ángulo sobre las tablas), de 0,18 a 0,34 × el
                        // radio de la mancha, más densas en el centro: luz que pasa entre las hojas, no una mancha lisa.
                        float best = 1e9f;
                        for (int e = 0; e < 8; e++)
                        {
                            float ang = ArtNoise.Hash(i, e, 211) * Mathf.PI * 2f;
                            float dist = Mathf.Sqrt(ArtNoise.Hash(i, e, 223)) * r * 0.78f;
                            float rx = r * (0.18f + (0.16f * ArtNoise.Hash(i, e, 227)));
                            float ry = rx * 0.62f;
                            float nx = (xd - cx - (Mathf.Cos(ang) * dist)) / rx;
                            float ny = (yd - cy - (Mathf.Sin(ang) * dist * 0.62f)) / ry;
                            best = Mathf.Min(best, (Mathf.Sqrt((nx * nx) + (ny * ny)) - 1f) * ry);
                        }

                        float falloff = 1f - (0.45f * Mathf.Clamp01(Mathf.Sqrt(((xd - cx) * (xd - cx)) + ((yd - cy) * (yd - cy))) / r));
                        amount += DockDapples[i + 3] * falloff * PixelCanvas.SoftCoverage(best, 3.5f);
                    }

                    if (amount > 0.002f)
                    {
                        pc.AddLight(px, py, Palette.SkyHorizon, amount);
                    }
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        // Follaje de esquina (guía 3.8 y 4.8): fronda de açaí, filodendro, hoja en flecha y, como mucho, un acento de
        // heliconia por racimo; sombra horneada (+3, +4) dp; luz de borde arriba a la izquierda. Sin monstera ni coco.
        // corner: 0 arriba-izq., 1 arriba-der. (con el racimo de açaí), 2 abajo-izq., 3 abajo-der. Las esquinas inferiores
        // miden 170 dp y van más claras (hoja_media → hoja_clara, con hoja_brillo en el borde): dan verde al muelle
        // (revisión visual del 2026-10-07).
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>Lado del follaje de las esquinas inferiores (dp).</summary>
        public const float BottomFoliageDp = 170f;

        private static PixelCanvas BuildFoliageCanvas(int corner)
        {
            int size = Mathf.RoundToInt((corner < 2 ? TopFoliageDp : BottomFoliageDp) * FoliagePxPerDp);
            var pc = new PixelCanvas(size, size);
            const float k = FoliagePxPerDp;
            float s = size / k; // dp
            bool right = corner == 1 || corner == 3;
            bool top = corner < 2;

            // Diseño en dp con el origen en la esquina de la pantalla, x hacia dentro e y hacia dentro.
            LeafSpec[] leaves = top ? TopLeaves(right) : BottomLeaves(right);
            for (int pass = 0; pass < 2; pass++)
            {
                bool shadow = pass == 0;
                for (int i = 0; i < leaves.Length; i++)
                {
                    LeafSpec l = leaves[i];
                    Vector2 basePx = ToCanvas(l.Base, s, k, right, top);
                    Vector2 dir = ToCanvasDir(l.AngleDeg, right, top);
                    if (shadow)
                    {
                        basePx += new Vector2(3f * k, -4f * k);
                    }

                    if (l.Kind == 1)
                    {
                        DrawFrond(pc, basePx, dir, l.Length * k, l.Width * k, l.Bend, k, shadow, i + (corner * 31), !top);
                    }
                    else
                    {
                        Color dark = l.Kind == 2 ? Palette.Heliconia : (top ? Palette.LeafDark : Palette.LeafMid);
                        Color light = l.Kind == 2 ? Hex(0xFFB347) : Palette.LeafLight;
                        LeafShape shape = l.Kind == 2 ? LeafShape.Bract : (l.Kind == 3 ? LeafShape.Lanceolate : LeafShape.Ovate);
                        if (shadow)
                        {
                            DrawLeafShadow(pc, basePx, dir, l.Length * k, l.Width * k, l.Bend * (right == top ? 1f : -1f), shape, k);
                        }
                        else
                        {
                            DrawLeaf(pc, basePx, dir, l.Length * k, l.Width * k, l.Bend * (right == top ? 1f : -1f), shape, dark, light, k, i + (corner * 17));
                        }
                    }
                }
            }

            if (corner == 1)
            {
                DrawAcaiBunch(pc, ToCanvas(new Vector2(58f, 34f), s, k, right, top), k);
            }

            return pc;
        }

        private readonly struct LeafSpec
        {
            public LeafSpec(int kind, float bx, float by, float angleDeg, float length, float width, float bend)
            {
                Kind = kind;
                Base = new Vector2(bx, by);
                AngleDeg = angleDeg;
                Length = length;
                Width = width;
                Bend = bend;
            }

            public int Kind { get; } // 0 filodendro, 1 fronda de açaí, 2 heliconia, 3 hoja en flecha

            public Vector2 Base { get; }

            public float AngleDeg { get; } // 0 = hacia dentro en horizontal, 90 = hacia dentro en vertical

            public float Length { get; }

            public float Width { get; }

            public float Bend { get; }
        }

        /// <summary>Ancho del manojo de hojas de un extremo de la rama de la tropa (dp; se ven los 44 de dentro).</summary>
        public const float BranchEndFoliageWidthDp = 80f;

        /// <summary>Alto del manojo de hojas de un extremo de la rama de la tropa (dp, de la rama hacia arriba).</summary>
        public const float BranchEndFoliageHeightDp = 128f;

        /// <summary>Pivote horizontal del manojo: el borde de la pantalla, a 36 dp de su lado de fuera.</summary>
        public const float BranchEndFoliagePivotX = 36f / BranchEndFoliageWidthDp;

        private Sprite? _branchEnd;

        /// <summary>
        /// Manojo de hojas del extremo de la rama baja de la tropa (provisional por código hasta el arte; revisión del
        /// 2026-10-11: sin él, el miquito que trepaba junto al borde parecía levitar sobre el agua y el recorte dejaba una cabeza
        /// o una cola sueltas): <see cref="BranchEndFoliageWidthDp"/> × <see cref="BranchEndFoliageHeightDp"/> dp con el
        /// pivote abajo, en el borde de la pantalla (<see cref="BranchEndFoliagePivotX"/>); dibujado para el extremo izquierdo
        /// (el derecho se refleja). Las hojas quedan dentro de la pieza, sin sombra que se corte en su borde. La rama sale de él
        /// y la tropa entra y sale trepando por dentro.
        /// </summary>
        public Sprite BranchEndFoliage => _branchEnd != null ? _branchEnd : (_branchEnd = BuildBranchEndFoliage());

        // Hojas del manojo en dp con el origen abajo en el borde de la pantalla, x hacia dentro e y hacia arriba: forma (0
        // filodendro, 3 hoja en flecha), base, ángulo (0 = hacia dentro, 90 = hacia arriba), largo, ancho y curva. Dentro de la
        // pieza (x de −32 a 36 dp, y de 2 a 117); la parte densa cubre de 0 a ≈ 30 dp hacia dentro y hasta ≈ 90 dp de alto (lo
        // que sube un miquito al trepar), y dos hojas en flecha llegan a ≈ 115.
        private static readonly LeafSpec[] BranchEndLeaves =
        {
            new LeafSpec(3, 2f, 2f, 86f, 112f, 16f, -0.06f),
            new LeafSpec(3, -8f, 56f, 72f, 62f, 16f, 0.10f),
            new LeafSpec(0, -24f, 10f, 60f, 72f, 38f, 0.06f),
            new LeafSpec(0, -20f, 24f, 30f, 54f, 32f, 0.08f),
            new LeafSpec(0, -14f, 50f, 14f, 50f, 30f, -0.10f),
            new LeafSpec(0, -12f, 74f, -2f, 42f, 26f, 0.10f),
            new LeafSpec(0, -10f, 92f, 34f, 36f, 22f, -0.08f),
            new LeafSpec(0, -18f, 13f, 10f, 40f, 22f, -0.08f),
        };

        private Sprite BuildBranchEndFoliage()
        {
            const float k = FoliagePxPerDp;
            int w = Mathf.RoundToInt(BranchEndFoliageWidthDp * k);
            int h = Mathf.RoundToInt(BranchEndFoliageHeightDp * k);
            var pc = new PixelCanvas(w, h);
            float edge = BranchEndFoliagePivotX * w;
            for (int i = 0; i < BranchEndLeaves.Length; i++)
            {
                LeafSpec l = BranchEndLeaves[i];
                var basePx = new Vector2(edge + (l.Base.x * k), l.Base.y * k);
                float a = l.AngleDeg * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                LeafShape shape = l.Kind == 3 ? LeafShape.Lanceolate : LeafShape.Ovate;
                Color dark = i % 2 == 0 ? Palette.LeafDark : Palette.LeafMid;
                DrawLeaf(pc, basePx, dir, l.Length * k, l.Width * k, l.Bend, shape, dark, Palette.LeafLight, k, 211 + (i * 13));
            }

            return MakeSprite(pc, "branch_end_foliage", new Vector2(BranchEndFoliagePivotX, 0f), Vector4.zero, k);
        }

        private static LeafSpec[] TopLeaves(bool right)
        {
            if (!right)
            {
                // Arriba a la izquierda: la parte densa no pasa de x 80 ni de y 96 (deja ver el sol, guía 3.8).
                return new[]
                {
                    new LeafSpec(1, -12f, 6f, 18f, 132f, 22f, 0.22f),
                    new LeafSpec(0, -8f, -6f, 58f, 92f, 56f, 0.10f),
                    new LeafSpec(3, -6f, 4f, 30f, 112f, 26f, 0.12f),
                    new LeafSpec(0, 6f, -10f, 82f, 70f, 42f, -0.08f),
                    new LeafSpec(0, -10f, 20f, 40f, 66f, 40f, 0.14f),
                };
            }

            // Arriba a la derecha: parte densa a la derecha de x 284 y por encima de y 65; el racimo de açaí cuelga
            // de la fronda por encima de y 110.
            return new[]
            {
                new LeafSpec(1, -10f, 4f, 22f, 126f, 22f, 0.26f),
                new LeafSpec(0, -8f, -8f, 50f, 84f, 52f, 0.12f),
                new LeafSpec(3, -4f, 10f, 34f, 98f, 24f, 0.10f),
                new LeafSpec(0, 8f, -8f, 78f, 62f, 38f, -0.06f),
            };
        }

        private static LeafSpec[] BottomLeaves(bool right)
        {
            // Diseñadas para 140 dp y llevadas a 170 (× 1,2).
            if (!right)
            {
                return new[]
                {
                    new LeafSpec(3, -7f, -2f, 38f, 144f, 29f, 0.16f),
                    new LeafSpec(0, -12f, -7f, 62f, 103f, 62f, 0.10f),
                    new LeafSpec(1, -10f, 12f, 14f, 144f, 24f, 0.18f),
                    new LeafSpec(0, 12f, -12f, 82f, 79f, 48f, -0.10f),
                };
            }

            return new[]
            {
                new LeafSpec(0, -10f, -5f, 56f, 108f, 65f, 0.12f),
                new LeafSpec(3, -2f, 7f, 26f, 139f, 29f, 0.14f),
                new LeafSpec(2, 24f, -5f, 70f, 89f, 19f, 0.05f), // único acento rojo (heliconia)
                new LeafSpec(0, 17f, -14f, 84f, 77f, 48f, -0.08f),
            };
        }

        private static Vector2 ToCanvas(Vector2 dp, float sizeDp, float k, bool right, bool top)
        {
            float x = right ? sizeDp - dp.x : dp.x;
            float yFromTop = top ? dp.y : sizeDp - dp.y;
            return new Vector2(x * k, (sizeDp - yFromTop) * k);
        }

        private static Vector2 ToCanvasDir(float angleDeg, bool right, bool top)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            float dx = Mathf.Cos(a) * (right ? -1f : 1f);
            float dy = Mathf.Sin(a) * (top ? -1f : 1f); // en px, y hacia arriba
            return new Vector2(dx, dy).normalized;
        }

        private static float LeafWidthAt(LeafShape shape, float u)
        {
            u = Mathf.Clamp01(u);
            switch (shape)
            {
                case LeafShape.Lanceolate:
                    return Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.85f)), 1.15f);
                case LeafShape.Bract:
                    return Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.6f)), 0.7f);
                default:
                    float s = Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.72f));
                    return Mathf.Pow(Mathf.Max(0f, s), 0.75f) * (u < 0.08f ? 0.6f + (5f * u) : 1f);
            }
        }

        /// <summary>
        /// Hoja con nervio central, nervios laterales a 30-40°, degradado del tallo a la punta, luz de borde arriba a la
        /// izquierda (guía 4.8). <paramref name="k"/> = px por dp; <paramref name="bend"/> curva el nervio (fracción del largo).
        /// </summary>
        internal static void DrawLeaf(PixelCanvas pc, Vector2 basePx, Vector2 dir, float length, float width, float bend, LeafShape shape, Color dark, Color light, float k, int seed)
        {
            Vector2 n = new Vector2(-dir.y, dir.x);
            var lightDir = new Vector2(-0.6f, 0.8f);
            float lightSide = Mathf.Sign(Vector2.Dot(n, lightDir));
            Color mid = Color.Lerp(dark, light, 0.5f);
            float half = width * 0.5f;
            float bendPx = bend * length;
            Vector2 tip = basePx + (dir * length) + (n * bendPx);
            int x0 = Mathf.FloorToInt(Mathf.Min(basePx.x, tip.x) - half - Mathf.Abs(bendPx) - 2f);
            int x1 = Mathf.CeilToInt(Mathf.Max(basePx.x, tip.x) + half + Mathf.Abs(bendPx) + 2f);
            int y0 = Mathf.FloorToInt(Mathf.Min(basePx.y, tip.y) - half - Mathf.Abs(bendPx) - 2f);
            int y1 = Mathf.CeilToInt(Mathf.Max(basePx.y, tip.y) + half + Mathf.Abs(bendPx) + 2f);
            x0 = Mathf.Max(0, x0);
            y0 = Mathf.Max(0, y0);
            x1 = Mathf.Min(pc.Width - 1, x1);
            y1 = Mathf.Min(pc.Height - 1, y1);
            float veinSpacing = (9f + (ArtNoise.Hash(seed, 0, 7) * 3f)) * k;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    Vector2 rel = new Vector2(x + 0.5f, y + 0.5f) - basePx;
                    float along = Vector2.Dot(rel, dir);
                    float u = along / length;
                    if (u < -0.02f || u > 1.02f)
                    {
                        continue;
                    }

                    float across = Vector2.Dot(rel, n) - (bendPx * u * u);
                    float hw = half * LeafWidthAt(shape, u);
                    // Borde suavizado de 1,5 px (revisión visual del 2026-10-07: sin escalones en hojas y foliolos).
                    float d = Mathf.Abs(across) - hw;
                    float cov = PixelCanvas.SoftCoverage(d, 1.5f) * Mathf.Clamp01((u + 0.02f) * length) * Mathf.Clamp01((1.02f - u) * length);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float side = hw > 0.01f ? across / hw : 0f; // -1..1
                    Color c = Color.Lerp(dark, mid, Mathf.SmoothStep(0f, 0.8f, u));
                    float facing = side * lightSide;
                    c = facing > 0f ? Color.Lerp(c, light, 0.30f * facing) : ColorMath.Darken(c, 0.18f * -facing);

                    // Nervio central (1,5 dp) con una línea clara al lado; en hojas estrechas (foliolos) solo el degradado.
                    float ad = Mathf.Abs(across);
                    bool wide = half > 4f * k;
                    if (!wide)
                    {
                        c.a = 1f;
                        pc.Blend(x, y, c, cov);
                        continue;
                    }

                    if (ad < 0.75f * k && u > 0.02f && u < 0.96f)
                    {
                        c = Color.Lerp(c, Palette.Vein, 0.75f * (1f - (ad / (0.75f * k))));
                    }
                    else if (ad < 1.8f * k && facing > 0f && u < 0.9f)
                    {
                        c = Color.Lerp(c, light, 0.25f);
                    }

                    // Nervios laterales a ~35° cada 9-12 dp, 1 dp, alfa 0,25.
                    float phase = Mathf.Repeat((along - (ad * 1.4f)) / veinSpacing, 1f);
                    if (shape != LeafShape.Bract && ad > 1.5f * k && ad < hw - (1.5f * k) && (phase < 0.08f || phase > 0.97f))
                    {
                        c = Color.Lerp(c, Palette.Vein, 0.25f);
                    }

                    // Luz de borde (1,5 dp de hoja_brillo) en el borde que mira arriba a la izquierda.
                    if (facing > 0.2f && hw - ad < 1.5f * k)
                    {
                        c = Color.Lerp(c, Palette.LeafShine, 0.75f * facing);
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }
        }

        private static void DrawLeafShadow(PixelCanvas pc, Vector2 basePx, Vector2 dir, float length, float width, float bend, LeafShape shape, float k)
        {
            Vector2 n = new Vector2(-dir.y, dir.x);
            float half = width * 0.5f;
            float bendPx = bend * length;
            Vector2 tip = basePx + (dir * length) + (n * bendPx);
            float soft = 4f * k;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(basePx.x, tip.x) - half - Mathf.Abs(bendPx) - soft));
            int x1 = Mathf.Min(pc.Width - 1, Mathf.CeilToInt(Mathf.Max(basePx.x, tip.x) + half + Mathf.Abs(bendPx) + soft));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(basePx.y, tip.y) - half - Mathf.Abs(bendPx) - soft));
            int y1 = Mathf.Min(pc.Height - 1, Mathf.CeilToInt(Mathf.Max(basePx.y, tip.y) + half + Mathf.Abs(bendPx) + soft));
            var shadow = new Color(0.02f, 0.06f, 0.03f, 0.25f);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    Vector2 rel = new Vector2(x + 0.5f, y + 0.5f) - basePx;
                    float along = Vector2.Dot(rel, dir);
                    float u = along / length;
                    if (u < -0.05f || u > 1.05f)
                    {
                        continue;
                    }

                    float across = Vector2.Dot(rel, n) - (bendPx * u * u);
                    float d = Mathf.Abs(across) - (half * LeafWidthAt(shape, u));
                    pc.Blend(x, y, shadow, PixelCanvas.SoftCoverage(d, soft));
                }
            }
        }

        private static void DrawFrond(PixelCanvas pc, Vector2 basePx, Vector2 dir, float length, float leafletLen, float droop, float k, bool shadow, int seed, bool upward)
        {
            // Fronda pinnada de açaí: raquis curvo y foliolos estrechos colgantes a ambos lados.
            var down = new Vector2(0f, -1f); // la gravedad tira hacia abajo en todas las esquinas
            Vector2 prev = basePx;
            Color rachis = shadow ? new Color(0.02f, 0.06f, 0.03f, 0.25f) : (upward ? Palette.LeafMid : Palette.LeafDark);
            Color leafDark = upward ? Palette.LeafMid : Palette.LeafDark; // esquinas inferiores, más claras
            Color leafLight = upward ? Palette.LeafLight : Palette.LeafMid;
            int steps = 18;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 p = basePx + (dir * length * t) + (down * droop * length * t * t);
                pc.FillCapsule(prev.x, prev.y, p.x, p.y, Mathf.Lerp(1.4f, 0.6f, t) * k, rachis);
                Vector2 tangent = (p - prev).normalized;
                if (t > 0.06f)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 normal = new Vector2(-tangent.y, tangent.x) * side;
                        Vector2 ld = ((tangent * 0.55f) + (normal * 0.65f) + (down * 0.55f)).normalized;
                        float len = leafletLen * k * 1.6f * (1f - (0.55f * t)) * (0.85f + (0.3f * ArtNoise.Hash(i, side + seed, 13)));
                        float wid = 3.4f * k * (1f - (0.3f * t));
                        if (shadow)
                        {
                            continue; // los foliolos finos no proyectan sombra horneada: se leería como un peine fantasma
                        }
                        else
                        {
                            DrawLeaf(pc, p, ld, len, wid, 0.10f * side, LeafShape.Lanceolate, leafDark, leafLight, k, seed + i);
                        }
                    }
                }

                prev = p;
            }
        }

        private static void DrawAcaiBunch(PixelCanvas pc, Vector2 attachPx, float k)
        {
            // Racimo de açaí colgado de la fronda (guía 3.8): raquillas anaranjadas con frutos morado-negros.
            Color stalk = Hex(0xC8762F);
            Color berry = Hex(0x3A1D46);
            Color berryHi = Hex(0x7C4C8E);
            Vector2 end = attachPx + new Vector2(-4f * k, -18f * k);
            pc.FillCapsule(attachPx.x, attachPx.y, end.x, end.y, 1.3f * k, stalk);
            for (int b = 0; b < 7; b++)
            {
                float t = b / 6f;
                Vector2 from = Vector2.Lerp(attachPx, end, 0.3f + (0.7f * t));
                Vector2 to = from + new Vector2(((b % 2) == 0 ? -1f : 1f) * (6f + (t * 3f)) * k, -(8f + (t * 6f)) * k);
                pc.FillCapsule(from.x, from.y, to.x, to.y, 0.7f * k, stalk);
                for (int f = 0; f < 5; f++)
                {
                    Vector2 fp = Vector2.Lerp(from, to, 0.3f + (f * 0.17f)) + new Vector2((ArtNoise.Hash(b, f, 3) - 0.5f) * 3f * k, -(1.2f * k));
                    pc.FillCircle(fp.x, fp.y, 2.0f * k, berry);
                    pc.FillCircle(fp.x - (0.6f * k), fp.y + (0.6f * k), 0.6f * k, berryHi);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        // Panel del tablero (guía 4.6): relleno por código sin textura ni ruido (degradado + charco de luz de +3-4 L*),
        // sombra interior de 16 dp, marco de 6 dp en madera media rojiza con bisel, contorno y tallas de hoja en las
        // esquinas, y sombra proyectada suave.
        // ------------------------------------------------------------------------------------------------------------
        private Sprite BuildPanelFill()
        {
            const int w = 96;
            const int h = 160;
            var pc = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                float t = 1f - ((y + 0.5f) / h); // 0 arriba
                for (int x = 0; x < w; x++)
                {
                    Color c = Color.Lerp(_panel.Top, _panel.Bottom, t);
                    float px = ((x + 0.5f) / w) - 0.15f;
                    float py = (t - 0.10f) * (h / (float)w) * 0.6f;
                    float r = Mathf.Sqrt((px * px) + (py * py)) / 0.45f;
                    float pool = Mathf.Clamp01(1f - r);
                    pool = pool * pool * (3f - (2f * pool));
                    pc.Set(x, y, c);
                    pc.BlendLinear(x, y, Palette.SkyHorizon, 0.016f * pool); // +3-4 L* en el centro (2.5)
                }
            }

            return MakeSprite(pc, "panel_fill", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildPanelFrame()
        {
            // Marco tallado de 12 dp (revisión visual del 2026-10-07; la guía 4.6 decía 6): 9-slice de 80 × 80 dp a 3 px/dp
            // con borde de 32 dp. Radio exterior 24 e interior 12; bisel claro de 2 dp (marco_bisel) en los cantos que miran
            // arriba, canto oscuro de 3 dp (ranura) abajo, una ranura tallada a 5 dp del borde, hojas talladas de 22 dp en
            // las esquinas y un filo de luz interior de 1,5 dp (cielo_horizonte al 25 %) bajo el canto superior.
            const float k = UiPxPerDp;
            const float sizeDp = 80f;
            int s = Mathf.RoundToInt(sizeDp * k);
            var pc = new PixelCanvas(s, s);
            const float outerR = 24f;
            const float frame = PanelFrameWoodDp;
            const float innerR = 12f;
            const float edge = 1.5f; // contorno exterior en ranura
            Color wood = Palette.Frame;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float yd = sizeDp - ((y + 0.5f) / k); // dp desde arriba
                    float dOut = PixelCanvas.RoundedRectDistance(xd, yd, edge, edge, sizeDp - edge, sizeDp - edge, outerR);
                    float dIn = PixelCanvas.RoundedRectDistance(xd, yd, edge + frame, edge + frame, sizeDp - edge - frame, sizeDp - edge - frame, innerR);
                    float covOutline = PixelCanvas.Coverage((dOut - edge) * k);
                    if (covOutline <= 0f)
                    {
                        continue;
                    }

                    // Filo de luz interior bajo el canto superior (sobre el relleno, por dentro del marco).
                    bool topBar = yd < sizeDp * 0.5f && Mathf.Min(yd, sizeDp - yd) < Mathf.Min(xd, sizeDp - xd);
                    if (dIn > -1.5f && dIn <= 0.5f && topBar)
                    {
                        pc.Blend(x, y, Palette.SkyHorizon, 0.25f * Mathf.Clamp01((dIn + 1.5f) * k) * Mathf.Clamp01((0.5f - dIn) * k));
                    }

                    pc.Blend(x, y, Palette.Groove, covOutline * (1f - PixelCanvas.Coverage(dOut * k)) * 0.95f);
                    float covFrame = PixelCanvas.Coverage(dOut * k) * (1f - PixelCanvas.Coverage(dIn * k));
                    if (covFrame <= 0f)
                    {
                        continue;
                    }

                    // Veta en la dirección de cada barra (constante a lo largo del estiramiento del 9-slice).
                    bool horizontalBar = Mathf.Min(yd, sizeDp - yd) < Mathf.Min(xd, sizeDp - xd);
                    float across = horizontalBar ? yd : xd;
                    float grain = 0.5f + (0.5f * Mathf.Sin((across * 1.7f) + (Mathf.Sin(across * 0.55f) * 1.3f)));
                    Color c = ColorMath.Darken(wood, grain > 0.86f ? 0.12f : 0.02f);
                    float t = -dOut; // dp hacia dentro desde el borde exterior
                    float inT = dIn; // dp hacia fuera desde el borde interior

                    // Volumen de la barra: más clara hacia el canto exterior que mira arriba, más oscura hacia abajo.
                    bool upperOuter = (yd < sizeDp * 0.5f && horizontalBar) || (xd < sizeDp * 0.5f && !horizontalBar);
                    float across01 = Mathf.Clamp01(t / frame);
                    c = upperOuter ? Color.Lerp(ColorMath.Lighten(c, 0.10f), c, across01) : Color.Lerp(ColorMath.Darken(c, 0.12f), c, across01);

                    // Ranura tallada a 5 dp del borde exterior: 1,5 dp oscura y 1 dp clara debajo (guía 4.4).
                    if (t > 5f && t < 6.5f)
                    {
                        c = Color.Lerp(c, Palette.Groove, 0.55f);
                    }
                    else if (t >= 6.5f && t < 7.5f)
                    {
                        c = Color.Lerp(c, Palette.FamilyWood.Light, 0.35f);
                    }

                    // Bisel claro de 2 dp (marco_bisel al 90 %, nítido: revisión visual del 2026-10-07) en los cantos que
                    // miran arriba y a la izquierda, y canto oscuro de 3 dp abajo.
                    if (upperOuter && t < 2.5f)
                    {
                        c = Color.Lerp(c, Palette.FrameBevel, 0.9f * Mathf.Clamp01((2f - t) * k + 0.5f));
                    }
                    else if (!upperOuter && t < 3f)
                    {
                        c = Color.Lerp(c, Palette.Groove, 0.55f * (1f - (t / 3f) * 0.5f));
                    }

                    // Canto interior: la luz cae en los cantos interiores que miran arriba (abajo y a la derecha).
                    if (inT < 1.5f && !upperOuter)
                    {
                        c = Color.Lerp(c, Palette.FrameBevel, 0.6f);
                    }
                    else if (inT < 1.5f)
                    {
                        c = ColorMath.Darken(c, 0.35f);
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, covFrame);
                }
            }

            // Hojas talladas de 22 dp en las cuatro esquinas, tangentes al arco, en mitad del marco.
            for (int corner = 0; corner < 4; corner++)
            {
                float sx = corner % 2 == 0 ? -1f : 1f;
                float sy = corner < 2 ? 1f : -1f; // y hacia arriba
                float c0 = edge + outerR;
                var center = new Vector2(sx < 0f ? c0 : sizeDp - c0, sy > 0f ? sizeDp - c0 : c0);
                Vector2 mid = center + (new Vector2(sx, sy).normalized * (outerR - (frame * 0.5f)));
                var tangent = new Vector2(-sy, sx).normalized;
                DrawCarvedLeaf(pc, (mid - (tangent * 11f)) * k, tangent, 22f * k, 7.5f * k, Palette.Frame, k);
            }

            return MakeSprite(pc, "panel_frame", new Vector2(0.5f, 0.5f), new Vector4(32f * k, 32f * k, 32f * k, 32f * k), k);
        }

        private static void DrawCarvedLeaf(PixelCanvas pc, Vector2 basePx, Vector2 dir, float length, float width, Color wood, float k)
        {
            // Talla en bajorrelieve: hoja más oscura con un filo claro abajo a la derecha (luz arriba a la izquierda).
            DrawLeafShape(pc, basePx + new Vector2(0.6f * k, -0.6f * k), dir, length, width, ColorMath.Lighten(wood, 0.35f), 0.7f);
            DrawLeafShape(pc, basePx, dir, length, width, ColorMath.Darken(wood, 0.30f), 0.9f);
            pc.FillCapsule(basePx.x, basePx.y, basePx.x + (dir.x * length * 0.85f), basePx.y + (dir.y * length * 0.85f), 0.35f * k, ColorMath.Darken(wood, 0.45f));
        }

        private static void DrawLeafShape(PixelCanvas pc, Vector2 basePx, Vector2 dir, float length, float width, Color c, float alpha)
        {
            Vector2 n = new Vector2(-dir.y, dir.x);
            int r = Mathf.CeilToInt(length + width);
            for (int y = Mathf.FloorToInt(basePx.y) - r; y <= Mathf.CeilToInt(basePx.y) + r; y++)
            {
                for (int x = Mathf.FloorToInt(basePx.x) - r; x <= Mathf.CeilToInt(basePx.x) + r; x++)
                {
                    Vector2 rel = new Vector2(x + 0.5f, y + 0.5f) - basePx;
                    float u = Vector2.Dot(rel, dir) / length;
                    if (u < 0f || u > 1f)
                    {
                        continue;
                    }

                    float d = Mathf.Abs(Vector2.Dot(rel, n)) - (width * 0.5f * LeafWidthAt(LeafShape.Ovate, u));
                    pc.Blend(x, y, ColorMath.WithAlpha(c, alpha), PixelCanvas.Coverage(d));
                }
            }
        }

        private Sprite BuildInnerShadow()
        {
            // 9-slice de 48 × 48 dp a 2 px/dp: centro transparente; 16 dp de negro 0,35 → 0 desde el borde (radio 14).
            const float k = 2f;
            const float sizeDp = 48f;
            int s = Mathf.RoundToInt(sizeDp * k);
            var pc = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float yd = (y + 0.5f) / k;
                    float d = -PixelCanvas.RoundedRectDistance(xd, yd, 0f, 0f, sizeDp, sizeDp, 14f);
                    if (d < 0f)
                    {
                        continue;
                    }

                    float t = Mathf.Clamp01(1f - (d / 16f));
                    pc.Set(x, y, new Color(0f, 0f, 0f, 0.35f * t * t));
                }
            }

            return MakeSprite(pc, "panel_inner_shadow", new Vector2(0.5f, 0.5f), new Vector4(20f * k, 20f * k, 20f * k, 20f * k), k);
        }

        private Sprite BuildSoftShadow()
        {
            // 9-slice de 80 × 80 dp a 1 px/dp: rectángulo de radio 20 con borde suave de 20 dp (≈ desenfoque de 10 dp).
            const float sizeDp = 80f;
            int s = Mathf.RoundToInt(sizeDp);
            var pc = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = PixelCanvas.RoundedRectDistance(x + 0.5f, y + 0.5f, 20f, 20f, sizeDp - 20f, sizeDp - 20f, 20f);
                    pc.Set(x, y, new Color(0f, 0f, 0f, PixelCanvas.SoftCoverage(d, 20f)));
                }
            }

            return MakeSprite(pc, "soft_shadow", new Vector2(0.5f, 0.5f), new Vector4(36f, 36f, 36f, 36f), 1f);
        }

        // ------------------------------------------------------------------------------------------------------------
        // Repisa (guía 4.5): cara superior de 6 dp (repisa_sup, L* ≤ 65) con veta suave, bisel de 1,5 dp, frente de
        // 12 dp con degradado y veta, canto inferior de 2 dp, extremos con radio de 4 dp y una hoja tallada de 10 dp, y
        // sombra de 6 dp sobre el panel. 26 dp de alto; 9-slice horizontal.
        // ------------------------------------------------------------------------------------------------------------
        private Sprite BuildShelf()
        {
            const float k = UiPxPerDp;
            const float wDp = 64f;
            const float hDp = 26f;
            int w = Mathf.RoundToInt(wDp * k);
            int h = Mathf.RoundToInt(hDp * k);
            var pc = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                float yd = hDp - ((y + 0.5f) / k); // dp desde arriba
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float d = PixelCanvas.RoundedRectDistance(xd, yd, 0.5f, 0f, wDp - 0.5f, 20f, 4f);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (yd >= 20f)
                    {
                        // Sombra sobre el panel: 6 dp, negro 0,35 → 0.
                        float t = Mathf.Clamp01((yd - 20f) / 6f);
                        float sideFade = Mathf.Clamp01(Mathf.Min(xd, wDp - xd) / 4f);
                        pc.Set(x, y, new Color(0f, 0f, 0f, 0.35f * (1f - t) * (1f - t) * sideFade));
                        continue;
                    }

                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Color c;
                    if (yd < 6f)
                    {
                        float g = Mathf.Sin((yd * 2.6f) + 0.7f);
                        c = ColorMath.Darken(Palette.ShelfTop, g > 0.8f ? 0.07f : 0f);
                        c = ColorMath.Darken(c, 0.06f * (yd / 6f)); // barniz: más claro arriba, sin pasar de L* 65
                    }
                    else if (yd < 7.5f)
                    {
                        c = Color.Lerp(Palette.ShelfFront, Palette.ShelfEdge, 0.80f); // bisel de 1,5 dp
                    }
                    else if (yd < 18f)
                    {
                        float t = (yd - 7.5f) / 10.5f;
                        c = Color.Lerp(Palette.ShelfFront, Palette.ShelfFrontLow, t);
                        float g = Mathf.Sin((yd * 0.9f) + (Mathf.Sin(yd * 0.31f) * 2f));
                        c = ColorMath.Darken(c, g > 0.82f ? 0.12f : 0f);
                    }
                    else
                    {
                        c = Color.Lerp(Palette.ShelfFrontLow, Palette.Groove, 0.60f); // canto inferior de 2 dp
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }

            // Hoja tallada de 10 dp en el frente de cada extremo.
            DrawCarvedLeaf(pc, new Vector2(4f * k, (hDp - 12.5f) * k), new Vector2(1f, 0.12f).normalized, 10f * k, 5f * k, Palette.ShelfFront, k);
            DrawCarvedLeaf(pc, new Vector2((wDp - 4f) * k, (hDp - 12.5f) * k), new Vector2(-1f, 0.12f).normalized, 10f * k, 5f * k, Palette.ShelfFront, k);
            return MakeSprite(pc, "shelf", new Vector2(0.5f, 0.5f), new Vector4(16f * k, 0f, 16f * k, 0f), k);
        }

        // ------------------------------------------------------------------------------------------------------------
        // Escena del río con el horizonte bajo (revisión visual del 2026-10-07): una garza blanca posada en la orilla
        // izquierda y hojas de vitória-régia junto a las orillas, fuera del panel y sin animación ligada al juego (D-002).
        // Nombres y especies pendientes de revisión del asesor cultural. Nada de canoas ni frutas (guía 7).
        // ------------------------------------------------------------------------------------------------------------
        private Sprite BuildHeron()
        {
            const float k = UiPxPerDp;
            const float wDp = 24f;
            const float hDp = 34f;
            var pc = new PixelCanvas(Mathf.RoundToInt(wDp * k), Mathf.RoundToInt(hDp * k));
            Color white = Hex(0xF7F4EC);
            Color shade = Hex(0xCFCBC2);
            Color leg = Hex(0x2B2118);
            Color beak = Hex(0xE8B930);

            // Patas finas hasta la línea de agua (pie del sprite).
            pc.FillCapsule(10.5f * k, 1.0f * k, 11.2f * k, 13.5f * k, 0.45f * k, leg);
            pc.FillCapsule(13.6f * k, 1.0f * k, 13.0f * k, 13.5f * k, 0.45f * k, leg);

            // Cuerpo: elipse inclinada con la cola abajo a la izquierda; luz arriba a la izquierda.
            for (int y = 0; y < pc.Height; y++)
            {
                for (int x = 0; x < pc.Width; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float yd = (y + 0.5f) / k; // dp desde abajo
                    var p = new Vector2(xd - 11.5f, yd - 16f);
                    const float a = -0.42f; // inclinación del cuerpo
                    float rx = (p.x * Mathf.Cos(a)) + (p.y * Mathf.Sin(a));
                    float ry = (-p.x * Mathf.Sin(a)) + (p.y * Mathf.Cos(a));
                    float d = (Mathf.Sqrt(((rx / 7f) * (rx / 7f)) + ((ry / 3.8f) * (ry / 3.8f))) - 1f) * 3.8f;
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Color c = Color.Lerp(white, shade, Smooth(0.2f, 1f, Mathf.Clamp01((-ry + 1f) / 4.5f)));
                    pc.Blend(x, y, c, cov);
                }
            }

            // Cuello en S y cabeza mirando al centro del río.
            Vector2 prev = new Vector2(16.2f, 17.6f);
            for (int i = 1; i <= 10; i++)
            {
                float t = i / 10f;
                var p = new Vector2(16.2f + (Mathf.Sin(t * Mathf.PI * 1.2f) * 2.2f) + (t * 0.8f), 17.6f + (t * 11f));
                pc.FillCapsule(prev.x * k, prev.y * k, p.x * k, p.y * k, Mathf.Lerp(1.5f, 1.05f, t) * k, white);
                prev = p;
            }

            pc.FillEllipseSoft((prev.x + 0.3f) * k, (prev.y + 0.6f) * k, 2.1f * k, 1.6f * k, 1f, white);
            pc.FillTriangle(new Vector2((prev.x + 1.6f) * k, (prev.y + 1.2f) * k), new Vector2((prev.x + 1.6f) * k, (prev.y + 0.0f) * k), new Vector2((prev.x + 6.4f) * k, (prev.y + 0.2f) * k), beak);
            pc.FillCircle((prev.x + 0.9f) * k, (prev.y + 0.9f) * k, 0.38f * k, leg);

            // Contorno suave de 1 dp para separarla del cielo claro.
            return MakeSprite(Outline(pc, Hex(0x6E6A62), 0.55f), "heron", new Vector2(0.5f, 0f), Vector4.zero, k);
        }

        private Sprite BuildLilyPad()
        {
            // Vitória-régia en perspectiva: disco aplanado verde con nervios radiales y el borde levantado rojizo.
            const float k = UiPxPerDp;
            const float wDp = 48f;
            const float hDp = 16f;
            var pc = new PixelCanvas(Mathf.RoundToInt(wDp * k), Mathf.RoundToInt(hDp * k));
            Color pad = Hex(0x3E8E3A);
            Color padDark = Hex(0x2A6E32);
            Color rim = Hex(0xB4524A);
            Color rimLight = Hex(0xE08A6E);
            float cx = wDp * 0.5f;
            float cy = hDp * 0.5f;
            float rx = (wDp * 0.5f) - 1f;
            float ry = (hDp * 0.5f) - 1.5f;
            for (int y = 0; y < pc.Height; y++)
            {
                for (int x = 0; x < pc.Width; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float yd = (y + 0.5f) / k; // desde abajo
                    float nx = (xd - cx) / rx;
                    float ny = (yd - cy) / ry;
                    float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                    float cov = PixelCanvas.Coverage((r - 1f) * ry * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Color c = Color.Lerp(pad, padDark, Mathf.Clamp01((0.6f - ny) * 0.6f));
                    float ang = Mathf.Atan2(ny, nx);
                    if (Mathf.Repeat(ang * 7f / Mathf.PI, 1f) < 0.08f && r > 0.12f && r < 0.86f)
                    {
                        c = ColorMath.Darken(c, 0.18f); // nervios radiales
                    }

                    // Borde levantado: rojizo por fuera, más claro en la mitad de arriba (la que mira a la luz).
                    if (r > 0.86f)
                    {
                        c = Color.Lerp(rim, rimLight, Mathf.Clamp01(ny));
                    }
                    else if (r > 0.80f)
                    {
                        c = Color.Lerp(c, Hex(0x77B85A), 0.6f);
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }

            return MakeSprite(pc, "lily_pad", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        private Sprite BuildLightRays()
        {
            // 12 rayos con borde suave y caída radial, en blanco: el color (destello al 35 %) lo pone la Image.
            const int size = 256;
            var pc = new PixelCanvas(size, size);
            const int rays = 12;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = ((x + 0.5f) / size * 2f) - 1f;
                    float py = ((y + 0.5f) / size * 2f) - 1f;
                    float r = Mathf.Sqrt((px * px) + (py * py));
                    if (r >= 1f)
                    {
                        continue;
                    }

                    float a = Mathf.Atan2(py, px) / (Mathf.PI * 2f) * rays;
                    float f = Mathf.Abs(Mathf.Repeat(a, 1f) - 0.5f) * 2f; // 0 en el centro del rayo, 1 entre rayos
                    float ray = 1f - Smooth(0.25f, 0.65f, f);
                    float fall = (1f - r) * (1f - r) * Smooth(0.05f, 0.25f, r);
                    pc.Set(x, y, new Color(1f, 1f, 1f, ray * fall));
                }
            }

            return MakeSprite(pc, "light_rays", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildVeilHole()
        {
            const int size = 256;
            const float clear = 150f / VeilHoleHalfDp;
            const float full = 320f / VeilHoleHalfDp;
            var pc = new PixelCanvas(size, size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = ((x + 0.5f) / size * 2f) - 1f;
                    float py = ((y + 0.5f) / size * 2f) - 1f;
                    float r = Mathf.Sqrt((px * px) + (py * py));
                    pc.Set(x, y, new Color(0f, 0f, 0f, Smooth(clear, full, r)));
                }
            }

            return MakeSprite(pc, "veil_hole", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Contorno de 1 dp (3 px) alrededor de lo opaco, por debajo de la figura.
        private static PixelCanvas Outline(PixelCanvas src, Color color, float alpha)
        {
            var dst = new PixelCanvas(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    float m = 0f;
                    for (int dy = -3; dy <= 3; dy++)
                    {
                        for (int dx = -3; dx <= 3; dx++)
                        {
                            int sx = x + dx;
                            int sy = y + dy;
                            if (sx < 0 || sy < 0 || sx >= src.Width || sy >= src.Height || (dx * dx) + (dy * dy) > 9)
                            {
                                continue;
                            }

                            m = Mathf.Max(m, src.Get(sx, sy).a);
                        }
                    }

                    if (m > 0f)
                    {
                        dst.Set(x, y, ColorMath.WithAlpha(color, alpha * m));
                    }

                    Color c = src.Get(x, y);
                    if (c.a > 0f)
                    {
                        dst.Blend(x, y, new Color(c.r, c.g, c.b, 1f), c.a);
                    }
                }
            }

            return dst;
        }

        private Sprite BuildRopePost()
        {
            // Poste de amarre de 40 × 48 dp con cuerda enrollada (guía 3.8 y 4.7).
            const float k = UiPxPerDp;
            const float wDp = 40f;
            const float hDp = 48f;
            int w = Mathf.RoundToInt(wDp * k);
            int h = Mathf.RoundToInt(hDp * k);
            var pc = new PixelCanvas(w, h);
            pc.FillEllipseSoft(20f * k, 3f * k, 17f * k, 3f * k, 3f * k, new Color(0f, 0f, 0f, 0.30f));

            // Rollo de cuerda en el suelo.
            for (int ring = 0; ring < 3; ring++)
            {
                float ry = (6f + (ring * 3.2f)) * k;
                float rx = (16f - (ring * 2.5f)) * k;
                DrawRopeEllipse(pc, 20f * k, ry, rx, 3.6f * k, 2.4f * k, k);
            }

            // Poste.
            for (int y = 0; y < h; y++)
            {
                float yd = hDp - ((y + 0.5f) / k);
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float d = PixelCanvas.RoundedRectDistance(xd, yd, 13f, 2f, 27f, 38f, 5f);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float u = (xd - 13f) / 14f;
                    Color c = Color.Lerp(Palette.DockLight, Palette.DockShadow, Mathf.SmoothStep(0f, 1f, u));
                    if (yd < 5f)
                    {
                        c = Color.Lerp(c, Palette.ShelfEdge, 0.4f * (1f - u));
                    }

                    float g = Mathf.Abs(Mathf.Sin((xd * 2.2f) + (ArtNoise.Value2(xd / 3f, yd / 12f, 171) * 3f)));
                    c = ColorMath.Darken(c, g < 0.15f ? 0.12f : 0f);
                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }

            // Vueltas de cuerda alrededor del poste.
            for (int i = 0; i < 4; i++)
            {
                float yd = 16f + (i * 3.4f);
                DrawRopeSegment(pc, 11.5f * k, (hDp - yd) * k, 28.5f * k, (hDp - yd - 1.5f) * k, 1.8f * k, k);
            }

            return MakeSprite(pc, "rope_post", new Vector2(0.5f, 0f), Vector4.zero, k);
        }

        private static void DrawRopeEllipse(PixelCanvas pc, float cx, float cy, float rx, float ry, float thick, float k)
        {
            int steps = 48;
            Vector2 prev = new Vector2(cx + rx, cy);
            for (int i = 1; i <= steps; i++)
            {
                float a = (i / (float)steps) * Mathf.PI * 2f;
                var p = new Vector2(cx + (Mathf.Cos(a) * rx), cy + (Mathf.Sin(a) * ry));
                Color c = (i % 3) == 0 ? Palette.FiberLight : ((i % 3) == 1 ? Palette.FiberBase : Palette.FiberShadow);
                if (Mathf.Sin(a) > 0.2f)
                {
                    c = ColorMath.Darken(c, 0.25f); // parte de atrás del rollo
                }

                pc.FillCapsule(prev.x, prev.y, p.x, p.y, thick * 0.5f, c);
                prev = p;
            }
        }

        private static void DrawRopeSegment(PixelCanvas pc, float x0, float y0, float x1, float y1, float thick, float k)
        {
            int steps = 10;
            for (int i = 0; i < steps; i++)
            {
                float t0 = i / (float)steps;
                float t1 = (i + 1) / (float)steps;
                Color c = (i % 3) == 0 ? Palette.FiberLight : ((i % 3) == 1 ? Palette.FiberBase : Palette.FiberShadow);
                pc.FillCapsule(Mathf.Lerp(x0, x1, t0), Mathf.Lerp(y0, y1, t0), Mathf.Lerp(x0, x1, t1), Mathf.Lerp(y0, y1, t1), thick * 0.5f, c);
            }
        }
    }
}
