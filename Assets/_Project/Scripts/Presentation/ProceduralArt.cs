#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using AF.Core.Board;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AF.Presentation
{
    /// <summary>
    /// Gráficos generados por código al arrancar para la dirección visual v2 «Selva brillante premium» (D-003;
    /// docs/art/direccion-visual-v2.md). Sin assets binarios: el arte final es del artista (CLAUDE.md, «Humanos») y los PNG
    /// pintados provisionales llegan por las ranuras de <see cref="ArtSlots"/> (guía 10.3), que tienen prioridad.
    /// </summary>
    /// <remarks>
    /// <para>Este archivo: vidrio de la cuia (4.1, 4.3) a la densidad de la pantalla, tapa de calabaza (07 §4.4), atlas de
    /// patrones por tinte (07 §3), partículas y brillos (6), iconos (5.7) y sombra de contacto. La escena y la UI están en
    /// <c>ProceduralArt.Scene.cs</c> y <c>ProceduralArt.Ui.cs</c>.</para>
    /// <para>Mezclas: lo horneado dentro de una textura mezcla en sRGB (<see cref="PixelCanvas.Blend"/>) salvo lo que deba
    /// igualar a la GPU (<see cref="PixelCanvas.BlendLinear"/>, guía 2.5). Los brillos que tocan el líquido no se hornean
    /// aquí: los pinta el shader del líquido como color opaco precalculado (4.2).</para>
    /// </remarks>
    public sealed partial class ProceduralArt : IDisposable
    {
        /// <summary>Píxeles por u por defecto de las texturas de la cuia (u = ancho de la cuia / 2, 07 §4.1).</summary>
        public const int CuiaPixelsPerU = 64;

        /// <summary>Densidad mínima del vidrio horneado (guía 4.1: <c>ceil(u_dp × escala)</c> px/u, entre este mínimo y el máximo).</summary>
        public const int MinCuiaPixelsPerU = 48;

        /// <summary>
        /// Densidad máxima del vidrio horneado (guía 4.1): 96 px/u con u = 32 dp en el SM-S938B y 128 en pantallas de
        /// 4 px/dp (D-004) [P].
        /// </summary>
        public const int MaxCuiaPixelsPerU = 128;

        /// <summary>Radio en píxeles del sprite 9-slice <see cref="RoundedRect"/>.</summary>
        public const float RoundedRectRadiusPx = 20f;

        /// <summary>Lado de una tesela del patrón por tinte, en píxeles; cubre <see cref="PatternTileU"/> u.</summary>
        public const int PatternTilePx = 64;

        /// <summary>Lado de la tesela del patrón en u (dos teselas por dosis de 1 u).</summary>
        public const float PatternTileU = 0.5f;

        /// <summary>Alto de una fila del atlas de patrones: una dosis (1 u, dos teselas).</summary>
        public const int PatternRowPx = 2 * PatternTilePx;

        /// <summary>Densidad de los sprites de UI (px por dp): la del SM-S938B (1080 px / 360 dp).</summary>
        public const float UiPxPerDp = 3f;

        private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        private readonly Dictionary<int, Sprite> _cuiaBack = new Dictionary<int, Sprite>();
        private readonly Dictionary<int, Sprite> _cuiaFront = new Dictionary<int, Sprite>();
        private readonly Dictionary<int, Sprite> _cuiaMarks = new Dictionary<int, Sprite>();
        private readonly Color _water;
        private readonly PanelColors _panel;

        /// <summary>Genera los gráficos comunes para el agua base <paramref name="waterBase"/>.</summary>
        public ProceduralArt(string waterBase)
        {
            Stopwatch watch = Stopwatch.StartNew();
            _water = Palette.WaterBase(waterBase);
            _panel = Palette.Panel;
            Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SealCap = BuildSealCap();
            Ring = BuildRing();
            Disc = BuildDisc();
            Glow = BuildGlow();
            Star = BuildStar();
            Drop = BuildDrop();
            GlintStreak = BuildGlintStreak();
            RoundedRect = BuildRoundedRect();
            Feather = BuildFeather();
            LeafBit = BuildLeafBit();
            Petal = BuildPetal();
            ContactShadow = BuildContactShadow();
            PatternAtlas = BuildPatternAtlas();
            BuildScene();
            BuildUi();
            watch.Stop();
            GenerationMs = watch.Elapsed.TotalMilliseconds;
            if (Debug.isDebugBuild)
            {
                Debug.Log("AF_ART procedural " + GenerationMs.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) + " ms");
            }
        }

        /// <summary>Tiempo de generación al arrancar (guía 8: ≤ 300 ms de CPU en clase A).</summary>
        public double GenerationMs { get; }

        /// <summary>Fuente heredada de uGUI (solo depuración; la UI usa TextMeshPro, guía 5.6).</summary>
        public Font Font { get; }

        /// <summary>Escala del lienzo (px de pantalla por dp), para hornear el vidrio a la densidad real (guía 4.1).</summary>
        public float ScreenScale { get; set; } = UiPxPerDp;

        /// <summary>Tapa de calabaza (07 §4.4): 1,10 u × 0,35 u.</summary>
        public Sprite SealCap { get; }

        /// <summary>Aro de luz (sellado y celebración, 07 §5.3-§5.4; sin resaltado de destinos, D-002). Aditivo.</summary>
        public Sprite Ring { get; }

        /// <summary>Disco blanco.</summary>
        public Sprite Disc { get; }

        /// <summary>Halo radial suave (polen, motas de luz). Aditivo.</summary>
        public Sprite Glow { get; }

        /// <summary>Destello en estrella de cuatro puntas (sellado, guía 6.8). Aditivo.</summary>
        public Sprite Star { get; }

        /// <summary>Gota con borde claro (salpicaduras, guía 6.6); se tiñe con <c>Image.color</c>.</summary>
        public Sprite Drop { get; }

        /// <summary>Destello alargado del río (guía 6.12). Aditivo.</summary>
        public Sprite GlintStreak { get; }

        /// <summary>Rectángulo redondeado blanco 9-slice (radio <see cref="RoundedRectRadiusPx"/> px).</summary>
        public Sprite RoundedRect { get; }

        /// <summary>Pluma blanca para el estallido de guacamayo (guía 6.9); se tiñe.</summary>
        public Sprite Feather { get; }

        /// <summary>Hoja pequeña del estallido (guía 6.9), ya coloreada.</summary>
        public Sprite LeafBit { get; }

        /// <summary>Pétalo de heliconia del estallido (guía 6.9), ya coloreado.</summary>
        public Sprite Petal { get; }

        /// <summary>Sombra de contacto de la cuia sobre la repisa (guía 4.1): elipse negra suave.</summary>
        public Sprite ContactShadow { get; }

        /// <summary>
        /// Skin con la que se construyen las cuias nuevas (tarea B14; 07 §4.6): la que el jugador lleva puesta, que fija la UI desde
        /// el perfil (Q-M8 (a)). <see cref="CuiaView"/> la lee al crearse; cambiarla no repinta las cuias que ya hay. Base por
        /// defecto.
        /// </summary>
        public CuiaSkin CuiaSkin { get; set; } = CuiaSkin.Base;

        /// <summary>Agua base del mundo (07 §2.1).</summary>
        public Color Water => _water;

        /// <summary>Colores del panel con los que se horneó el arte (guía 4.6; D-004).</summary>
        public PanelColors PanelColors => _panel;

        /// <summary>
        /// Atlas de patrones por tinte (07 §3, columna "Patrón"): una fila de <see cref="PatternRowPx"/> px (una dosis)
        /// por índice de motor 0-14, marcas blancas sobre transparente, repetible en horizontal, y detrás la fila turbia
        /// (<see cref="MurkyPatternRow"/>: trama diagonal en A y el «?» de las Aguas turbias en R y G; 07 §4.3). Una sola
        /// textura, lineal y sin mipmaps, para que los patrones de todas las cuias vayan en el mismo lote de dibujo.
        /// </summary>
        public Texture2D PatternAtlas { get; }

        /// <summary>El tinte tiene patrón (Guanábana es "liso", 07 §3).</summary>
        public static bool HasPattern(int tint)
        {
            return tint >= 2 && tint <= Cuia.MaxTintColor;
        }

        /// <summary>
        /// UV de una dosis del tinte <paramref name="tint"/> en <see cref="PatternAtlas"/>: <paramref name="widthU"/> u de
        /// ancho (se repite en horizontal) y <paramref name="fill"/> de dosis de alto, anclado al fondo de la dosis.
        /// </summary>
        public Rect PatternUv(int tint, float widthU, float fill)
        {
            float rowH = PatternRowPx / (float)PatternAtlas.height;
            return new Rect(0f, tint * rowH, widthU / PatternTileU, Mathf.Clamp01(fill) * rowH);
        }

        /// <summary>Densidad del vidrio para una cuia de <paramref name="unitDp"/> dp por u en esta pantalla (guía 4.1).</summary>
        public int CuiaPixelsPerUFor(float unitDp)
        {
            int ppu = Mathf.CeilToInt(unitDp * Mathf.Max(0.5f, ScreenScale));
            return Mathf.Clamp(ppu, MinCuiaPixelsPerU, MaxCuiaPixelsPerU);
        }

        /// <summary>Vidrio trasero y cavidad opaca de una cuia de capacidad <paramref name="capacity"/>.</summary>
        public Sprite CuiaBack(int capacity, int pixelsPerU = CuiaPixelsPerU)
        {
            int key = (capacity * 1000) + pixelsPerU;
            if (!_cuiaBack.TryGetValue(key, out Sprite? sprite))
            {
                sprite = BuildCuiaBack(capacity, pixelsPerU);
                _cuiaBack[key] = sprite;
            }

            return sprite;
        }

        /// <summary>Vidrio frontal: contorno, fresnel, franjas, cáustica, base gruesa, pie y borde de fibra.</summary>
        public Sprite CuiaFront(int capacity, int pixelsPerU = CuiaPixelsPerU)
        {
            int key = (capacity * 1000) + pixelsPerU;
            if (!_cuiaFront.TryGetValue(key, out Sprite? sprite))
            {
                sprite = BuildCuiaFront(capacity, pixelsPerU);
                _cuiaFront[key] = sprite;
            }

            return sprite;
        }

        /// <summary>
        /// Marcas de dosis (07 §4.1) en un sprite aparte, encima del vidrio frontal: la vista las oculta mientras la cuia
        /// se inclina para verter, para que no crucen en diagonal las franjas horizontales del líquido (revisión visual
        /// del 2026-10-07).
        /// </summary>
        public Sprite CuiaMarks(int capacity, int pixelsPerU = CuiaPixelsPerU)
        {
            int key = (capacity * 1000) + pixelsPerU;
            if (!_cuiaMarks.TryGetValue(key, out Sprite? sprite))
            {
                sprite = BuildCuiaMarks(capacity, pixelsPerU);
                _cuiaMarks[key] = sprite;
            }

            return sprite;
        }

        /// <summary>Destruye texturas y sprites generados.</summary>
        public void Dispose()
        {
            AbandonBannerArt(); // el arte del banner que aún se pinta en otro hilo para y se descarta (ProceduralArt.Mascots.cs)
            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(_owned[i]);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(_owned[i]); // tests EditMode y herramientas del Editor
                    }
                }
            }

            _owned.Clear();
            ForgetIntroArt();
            _fruitIconAtlas = null; // el atlas de iconos de fruta (ProceduralArt.Icons.cs) se vuelve a generar si hace falta
            _cuiaBack.Clear();
            _cuiaFront.Clear();
            _cuiaMarks.Clear();
            _trays.Clear();
        }

        private Sprite MakeSprite(PixelCanvas canvas, string name, Vector2 pivot, Vector4 border, bool repeatX = false)
        {
            return MakeSprite(canvas, name, pivot, border, 1f, repeatX);
        }

        // pxPerDp: el sprite se crea con PPU = 100 · pxPerDp, así un borde 9-slice de N px mide N / pxPerDp dp con el
        // CanvasScaler (referencePixelsPerUnit = 100), igual que los PNG de las ranuras (PPU 300/400; prompts §1).
        private Sprite MakeSprite(PixelCanvas canvas, string name, Vector2 pivot, Vector4 border, float pxPerDp, bool repeatX = false)
        {
            Texture2D tex = canvas.ToTexture(name, repeatX);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, canvas.Width, canvas.Height), pivot, 100f * pxPerDp, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            _owned.Add(tex);
            _owned.Add(sprite);
            return sprite;
        }

        // Atlas de patrones: las filas 0-14 (una por tinte; la 0 y la 1 vacías) y la fila turbia (DrawMurkyRow, en
        // ProceduralArt.Murky.cs). Lineal: la fila turbia guarda máscaras en R y G (el «?» y su halo) que no deben pasar por la
        // curva sRGB; las demás solo usan A, que nunca la pasa.
        private Texture2D BuildPatternAtlas()
        {
            const int t = PatternTilePx;
            int rows = MurkyPatternRow + 1;
            var atlas = new PixelCanvas(t, PatternRowPx * rows);
            for (int tint = 2; tint <= Cuia.MaxTintColor; tint++)
            {
                var row = new PixelCanvas(t, PatternRowPx);
                for (int ox = -t; ox <= t; ox += t)
                {
                    for (int oy = -t; oy <= PatternRowPx; oy += t)
                    {
                        DrawPatternTile(row, tint, ox, oy); // copias desplazadas: la tesela se repite sin costuras
                    }
                }

                for (int y = 0; y < PatternRowPx; y++)
                {
                    for (int x = 0; x < t; x++)
                    {
                        atlas.Set(x, (tint * PatternRowPx) + y, row.Get(x, y));
                    }
                }
            }

            PixelCanvas murky = DrawMurkyRow();
            for (int y = 0; y < PatternRowPx; y++)
            {
                for (int x = 0; x < t; x++)
                {
                    atlas.Set(x, (MurkyPatternRow * PatternRowPx) + y, murky.Get(x, y));
                }
            }

            var tex = new Texture2D(atlas.Width, atlas.Height, TextureFormat.RGBA32, false, true)
            {
                name = "pattern_atlas",
                filterMode = FilterMode.Bilinear,
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            tex.SetPixels32(atlas.TakeColor32());
            tex.Apply(false, true);
            _owned.Add(tex);
            return tex;
        }

        // Motivos de 07 §3 en una tesela de 64 × 64 px con origen (ox, oy); blanco opaco (el color lo pone la dosis).
        private static void DrawPatternTile(PixelCanvas pc, int tint, float ox, float oy)
        {
            Color w = Color.white;
            switch (tint)
            {
                case 2: // Copoazú: puntos finos
                    for (int j = 0; j < 4; j++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            pc.FillCircle(ox + (i * 16f) + 8f + ((j % 2) * 8f), oy + (j * 16f) + 8f, 2.6f, w);
                        }
                    }

                    break;
                case 3: // Taperebá: puntos grandes
                    pc.FillCircle(ox + 16f, oy + 16f, 9f, w);
                    pc.FillCircle(ox + 48f, oy + 48f, 9f, w);
                    break;
                case 4: // Aguaje: escamas
                    for (int r = 0; r < 4; r++)
                    {
                        for (int i = -1; i <= 2; i++)
                        {
                            pc.StrokeArc(ox + (i * 32f) + ((r % 2) * 16f) + 16f, oy + (r * 16f) + 16f, 15f, 3f, 180f, 360f, w);
                        }
                    }

                    break;
                case 5: // Achiote: triángulos
                    pc.FillTriangle(new Vector2(ox + 16f, oy + 26f), new Vector2(ox + 6f, oy + 8f), new Vector2(ox + 26f, oy + 8f), w);
                    pc.FillTriangle(new Vector2(ox + 48f, oy + 58f), new Vector2(ox + 38f, oy + 40f), new Vector2(ox + 58f, oy + 40f), w);
                    break;
                case 6: // Camu camu: rayas verticales
                    pc.FillRoundedRect(ox + 9f, oy - 1f, ox + 19f, oy + 65f, 0f, w);
                    pc.FillRoundedRect(ox + 41f, oy - 1f, ox + 51f, oy + 65f, 0f, w);
                    break;
                case 7: // Bacaba: ondas
                    DrawWave(pc, ox - 2f, ox + 66f, oy + 16f, 6f, 64f, 2.2f, w);
                    DrawWave(pc, ox - 2f, ox + 66f, oy + 48f, 6f, 64f, 2.2f, w);
                    break;
                case 8: // Açaí: rombos
                    FillDiamond(pc, ox + 16f, oy + 16f, 11f, w);
                    FillDiamond(pc, ox + 48f, oy + 48f, 11f, w);
                    break;
                case 9: // Huito: cuadrícula
                    pc.FillRoundedRect(ox - 1.5f, oy - 1f, ox + 1.5f, oy + 65f, 0f, w);
                    pc.FillRoundedRect(ox + 30.5f, oy - 1f, ox + 33.5f, oy + 65f, 0f, w);
                    pc.FillRoundedRect(ox - 1f, oy - 1.5f, ox + 65f, oy + 1.5f, 0f, w);
                    pc.FillRoundedRect(ox - 1f, oy + 30.5f, ox + 65f, oy + 33.5f, 0f, w);
                    break;
                case 10: // Agua del Tapajós: líneas onduladas finas
                    for (int k = 0; k < 4; k++)
                    {
                        DrawWave(pc, ox - 2f, ox + 66f, oy + 8f + (k * 16f), 3f, 32f, 1.3f, w);
                    }

                    break;
                case 11: // Lulo: chevrons
                    for (int k = 0; k < 2; k++)
                    {
                        float y0 = oy + 10f + (k * 32f);
                        for (int i = -1; i < 2; i++)
                        {
                            float x0 = ox + (i * 32f);
                            pc.FillCapsule(x0, y0 + 12f, x0 + 16f, y0, 2.2f, w);
                            pc.FillCapsule(x0 + 16f, y0, x0 + 32f, y0 + 12f, 2.2f, w);
                        }
                    }

                    break;
                case 12: // Arazá: medias lunas
                    FillCrescent(pc, ox + 16f, oy + 16f, 9f, w);
                    FillCrescent(pc, ox + 48f, oy + 48f, 9f, w);
                    break;
                case 13: // Cacao: cruzado
                    for (int k = -2; k <= 2; k++)
                    {
                        float c = k * 32f;
                        pc.FillCapsule(ox - 8f, oy - 8f + c, ox + 72f, oy + 72f + c, 1.6f, w);
                        pc.FillCapsule(ox - 8f, oy + 72f + c, ox + 72f, oy - 8f + c, 1.6f, w);
                    }

                    break;
                case 14: // Agua del Río Negro: moteado
                    pc.FillCircle(ox + 10f, oy + 12f, 5f, w);
                    pc.FillCircle(ox + 30f, oy + 6f, 3f, w);
                    pc.FillCircle(ox + 50f, oy + 18f, 6f, w);
                    pc.FillCircle(ox + 20f, oy + 38f, 4f, w);
                    pc.FillCircle(ox + 42f, oy + 44f, 5f, w);
                    pc.FillCircle(ox + 58f, oy + 58f, 3f, w);
                    pc.FillCircle(ox + 6f, oy + 54f, 3.5f, w);
                    pc.FillCircle(ox + 34f, oy + 26f, 2.5f, w);
                    break;
            }
        }

        private static void FillDiamond(PixelCanvas pc, float cx, float cy, float r, Color c)
        {
            pc.FillTriangle(new Vector2(cx - r, cy), new Vector2(cx, cy - r), new Vector2(cx + r, cy), c);
            pc.FillTriangle(new Vector2(cx - r, cy), new Vector2(cx + r, cy), new Vector2(cx, cy + r), c);
        }

        private static void FillCrescent(PixelCanvas pc, float cx, float cy, float r, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - r - 1f);
            int x1 = Mathf.CeilToInt(cx + r + 1f);
            int y0 = Mathf.FloorToInt(cy - r - 1f);
            int y1 = Mathf.CeilToInt(cy + r + 1f);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d1 = Vector2.Distance(p, new Vector2(cx, cy)) - r;
                    float d2 = Vector2.Distance(p, new Vector2(cx + (0.45f * r), cy + (0.35f * r))) - (0.85f * r);
                    pc.Blend(x, y, c, PixelCanvas.Coverage(d1) * (1f - PixelCanvas.Coverage(d2)));
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        // Vidrio (guía 4.1 y 4.3). Capas horneadas en dos sprites: detrás del líquido (tinte del cuerpo y cavidad opaca
        // con sus franjas como color opaco) y delante (contorno, fresnel, franjas sobre el vidrio, punto del hombro,
        // cáustica, base gruesa, pared de la cavidad, marcas de dosis y fibra). Nada blanco con alfa sobre el líquido salvo
        // líneas de ≤ 2 dp (marcas, pared): los brillos del líquido los pinta su shader (4.2).
        // ------------------------------------------------------------------------------------------------------------

        private Sprite BuildCuiaBack(int capacity, int ppu)
        {
            int w = Mathf.RoundToInt(CuiaGeometry.WidthU * ppu);
            int h = Mathf.RoundToInt(CuiaGeometry.HeightU(capacity) * ppu);
            var pc = new PixelCanvas(w, h);
            float lip = CuiaGeometry.LipU(capacity);
            float height = CuiaGeometry.HeightU(capacity);
            Color tint = Palette.GlassTint;
            tint.a = 0.10f;
            Color cavity = _panel.Cavity;
            Color cavityHl = ColorMath.AddLabLightness(cavity, 8f); // franjas en la cavidad vacía: cavidad + 8 L* (4.3)
            var a = new Vector2(0.25f, height - 1.0f);
            var b = new Vector2(0.55f, 1.0f);
            var a2 = a + new Vector2(0.16f, 0f);
            var b2 = b + new Vector2(0.16f, 0f);
            for (int y = 0; y < h; y++)
            {
                float yu = (y + 0.5f) / ppu;
                float outer = CuiaGeometry.OuterHalfWidth(yu, capacity);
                float inner = CuiaGeometry.InnerHalfWidth(yu, capacity);
                for (int x = 0; x < w; x++)
                {
                    float xu = (x + 0.5f) / ppu;
                    float dx = Mathf.Abs(xu - 1f);
                    if (yu >= CuiaGeometry.FootTopU && yu < lip)
                    {
                        pc.Blend(x, y, tint, PixelCanvas.Coverage((dx - outer) * ppu));
                    }

                    // Bajo el fondo junto a las paredes, la cavidad sigue con su ancho hasta el fondo en arco (D-006, como el
                    // fondo redondeado de Magic Sort; CuiaGeometry.FaceSagU).
                    float cavityW = yu < CuiaGeometry.CavityBottomU ? CuiaGeometry.CavityHalfWidthU : inner;
                    if (cavityW > 0f && yu < lip && yu > CuiaGeometry.CavityFloorU(0f) - (1f / ppu))
                    {
                        float floorY = CuiaGeometry.CavityFloorU(xu - 1f);
                        float cov = PixelCanvas.Coverage((dx - cavityW) * ppu) * Mathf.Clamp01(((yu - floorY) * ppu) + 0.5f);
                        if (cov <= 0f)
                        {
                            continue;
                        }

                        // Cavidad opaca (D-003) con la viñeta lateral (07 §4.2) y las franjas como color opaco.
                        Color c = cavity;
                        float edge = cavityW - dx;
                        if (edge < 0.15f)
                        {
                            c = ColorMath.Darken(c, 0.30f * (1f - Mathf.Clamp01(edge / 0.15f)));
                        }

                        var p = new Vector2(xu, yu);
                        float d1 = DistanceToSegment(p, a, b);
                        float d2 = DistanceToSegment(p, a2, b2);
                        float main = d1 < 0.09f ? Mathf.Exp(-(d1 * d1) / 0.00405f) : 0f;
                        float sec = d2 < 0.025f ? 0.5f * Mathf.Exp(-(d2 * d2) / 0.0003125f) : 0f;
                        float hl = Mathf.Max(main, sec);
                        if (hl > 0f)
                        {
                            c = Color.Lerp(c, cavityHl, hl);
                        }

                        c.a = 1f;
                        pc.Blend(x, y, c, cov);
                    }
                }
            }

            return MakeSprite(pc, "cuia_back_" + capacity + "_" + ppu, new Vector2(0.5f, 0f), Vector4.zero);
        }

        private Sprite BuildCuiaFront(int capacity, int ppu)
        {
            int w = Mathf.RoundToInt(CuiaGeometry.WidthU * ppu);
            int h = Mathf.RoundToInt(CuiaGeometry.HeightU(capacity) * ppu);
            var pc = new PixelCanvas(w, h);
            float height = CuiaGeometry.HeightU(capacity);
            float lip = CuiaGeometry.LipU(capacity);
            float cavityTop = CuiaGeometry.CavityTopU(capacity);
            float shoulderTop = cavityTop + CuiaGeometry.ShoulderU;
            // ppu = ceil(u en dp × escala): 1 dp de pantalla mide ScreenScale / ppu u en esta textura.
            float lineU = Mathf.Max(1f / ppu, Mathf.Max(0.5f, ScreenScale) / ppu); // 1 dp en u
            float rimW = Mathf.Max(0.07f, 1.2f * lineU); // contorno: 0,07 u, mínimo 1,2 dp
            Color rim = Palette.GlassRim;
            Color white = Color.white;
            Color fiber = Palette.Fiber;
            Color fiberLight = Color.Lerp(fiber, Palette.FiberLight, 0.55f);
            Color fiberDark = ColorMath.Darken(fiber, 0.32f);

            var hlA = new Vector2(0.25f, height - 1.0f);
            var hlB = new Vector2(0.55f, 1.0f);
            var hlA2 = hlA + new Vector2(0.16f, 0f);
            var hlB2 = hlB + new Vector2(0.16f, 0f);
            // Punto especular del hombro (D-006): a 0,22 u sobre el techo de la cavidad, dentro del hombro de 0,40 u, en el
            // centro de la pared de vidrio a esa altura (lado derecho).
            float spotY = cavityTop + 0.22f;
            var spot = new Vector2(1f + (0.5f * (CuiaGeometry.OuterHalfWidth(spotY, capacity) + CuiaGeometry.InnerHalfWidth(spotY, capacity))), spotY);

            for (int y = 0; y < h; y++)
            {
                float yu = (y + 0.5f) / ppu;
                float outer = CuiaGeometry.OuterHalfWidth(yu, capacity);
                float inner = CuiaGeometry.InnerHalfWidth(yu, capacity);
                for (int x = 0; x < w; x++)
                {
                    float xu = (x + 0.5f) / ppu;
                    float dx = Mathf.Abs(xu - 1f);
                    float covOuter = PixelCanvas.Coverage((dx - outer) * ppu);
                    if (covOuter <= 0f)
                    {
                        continue;
                    }

                    bool fiberBand = yu < CuiaGeometry.FootTopU || yu >= lip;
                    if (fiberBand)
                    {
                        // Pie y borde de fibra de arumã trenzada (07 §4.6; guía 4.7): chevrones a 45° de 0,12 u, tres tonos.
                        float chev = Mathf.Repeat((xu + (Mathf.Abs(Mathf.Repeat(yu, 0.24f) - 0.12f) * 1f)) / 0.12f, 1f);
                        Color c = chev < 0.40f ? fiberLight : (chev < 0.80f ? fiber : fiberDark);
                        float y0 = yu < CuiaGeometry.FootTopU ? 0f : lip;
                        float y1 = yu < CuiaGeometry.FootTopU ? CuiaGeometry.FootTopU : height;
                        float band = (yu - y0) / Mathf.Max(0.01f, y1 - y0); // 0 abajo, 1 arriba
                        c = Color.Lerp(ColorMath.Darken(c, 0.25f), ColorMath.Lighten(c, 0.12f), band); // luz desde arriba
                        float vy = Mathf.Clamp01(((yu - y0) * ppu) + 0.5f) * Mathf.Clamp01(((y1 - yu) * ppu) + 0.5f);
                        pc.Blend(x, y, c, covOuter * vy);
                        float edgeY = Mathf.Min(yu - y0, y1 - yu);
                        if (edgeY < lineU)
                        {
                            pc.Blend(x, y, fiberDark, covOuter * vy * 0.7f);
                        }

                        float side = outer - dx;
                        if (side < 0.06f)
                        {
                            pc.Blend(x, y, ColorMath.WithAlpha(fiberDark, 0.5f), covOuter * vy * (1f - (side / 0.06f)));
                        }

                        continue;
                    }

                    // Fondo de la cavidad en arco (D-006; CuiaGeometry.FaceSagU): bajo el fondo junto a las paredes, lo que
                    // queda sobre el arco también es cavidad (y líquido).
                    float floorY = dx < CuiaGeometry.CavityHalfWidthU ? CuiaGeometry.CavityFloorU(xu - 1f) : CuiaGeometry.CavityBottomU;
                    bool inDip = yu < CuiaGeometry.CavityBottomU && yu >= floorY && dx < CuiaGeometry.CavityHalfWidthU;
                    bool inCavity = (inner > 0f && dx < inner && yu >= CuiaGeometry.CavityBottomU) || inDip;
                    bool overLiquid = inCavity && yu < cavityTop; // ahí solo se permiten líneas de ≤ 2 dp

                    // Base gruesa (del pie al fondo de la cavidad, en arco): #0E1A16 al 40 % y una línea de luz de 1 dp 0,02 u
                    // sobre el pie; con antialias en el arco.
                    float baseCov = Mathf.Clamp01(((floorY - yu) * ppu) + 0.5f);
                    if (yu < CuiaGeometry.CavityBottomU && baseCov > 0f)
                    {
                        pc.Blend(x, y, new Color(0x0E / 255f, 0x1A / 255f, 0x16 / 255f, 0.40f), covOuter * baseCov);
                        float l = Mathf.Abs(yu - (CuiaGeometry.FootTopU + 0.02f));
                        if (l < lineU)
                        {
                            pc.Blend(x, y, ColorMath.WithAlpha(white, 0.35f), covOuter * baseCov * (1f - (l / lineU)));
                        }

                        // Cáustica de la base: media luna de pie + 0,04 a pie + 0,22 u, x 0,45-1,55, blanco al 45 % (revisión
                        // visual del 2026-10-07; la guía 4.1 decía 0,30).
                        float cx = (xu - 1f) / 0.55f;
                        float cy = (yu - (CuiaGeometry.FootTopU + 0.13f)) / 0.09f;
                        float crescent = 1f - Mathf.Clamp01((cx * cx) + (cy * cy));
                        float cyCut = (yu - (CuiaGeometry.FootTopU + 0.17f)) / 0.07f;
                        float cut = 1f - Mathf.Clamp01((cx * cx) + (cyCut * cyCut));
                        float caustic = Mathf.Clamp01(crescent - (cut * 0.85f));
                        if (caustic > 0f)
                        {
                            pc.Blend(x, y, ColorMath.WithAlpha(white, 0.45f), covOuter * baseCov * Mathf.SmoothStep(0f, 1f, caustic * 2f));
                        }
                    }

                    // Fresnel interior: 0,14 u hacia dentro desde el contorno, alfa 0,45 → 0, solo sobre la pared de vidrio.
                    float fromEdge = outer - dx;
                    if (!overLiquid && fromEdge < 0.14f + rimW)
                    {
                        float f = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((fromEdge - rimW) / 0.14f));
                        pc.Blend(x, y, ColorMath.WithAlpha(rim, 0.45f), covOuter * f * (inCavity ? 0.6f : 1f));
                    }

                    // Franjas especulares sobre el vidrio (fuera del líquido): principal con pico 0,55 y secundaria 0,30.
                    if (!overLiquid)
                    {
                        var p = new Vector2(xu, yu);
                        float d1 = DistanceToSegment(p, hlA, hlB);
                        float d2 = DistanceToSegment(p, hlA2, hlB2);
                        float main = d1 < 0.09f ? Mathf.Exp(-(d1 * d1) / 0.00405f) * 0.55f : 0f;
                        float sec = d2 < 0.025f ? Mathf.Exp(-(d2 * d2) / 0.0003125f) * 0.30f : 0f;
                        float s = Mathf.Max(main, sec);
                        if (s > 0.002f)
                        {
                            pc.Blend(x, y, ColorMath.WithAlpha(white, s), covOuter);
                        }

                        // Punto especular del hombro: (centro de la pared; techo + 0,22 u), radio 0,16 u, 0,70 → 0.
                        float sd = Vector2.Distance(p, spot) / 0.16f;
                        if (sd < 1f)
                        {
                            pc.Blend(x, y, ColorMath.WithAlpha(white, 0.70f * (1f - Mathf.SmoothStep(0f, 1f, sd))), covOuter);
                        }
                    }

                    // Brillo de «caramelo» (revisión visual del 2026-10-07), solo sobre las paredes, el hombro y el cuello y
                    // nunca sobre el líquido ni la cavidad (guía 2.5 y 4.2): en la pared izquierda, una franja blanca de
                    // 0,08 u al 85 % con borde nítido que sigue el contorno de y = 1,0 u al final del hombro (allí es el
                    // arco de brillo, al 80 %); en la pared derecha, una luz de borde de 0,05 u al 50 % junto al contorno.
                    if (yu >= 1.0f && yu <= shoulderTop && !inCavity)
                    {
                        float wall = outer - inner; // ancho horizontal de la pared a esta altura (u)
                        float fromOut = outer - dx;
                        float ends = Mathf.Clamp01((yu - 1.0f) / 0.25f) * Mathf.Clamp01((shoulderTop - yu) / 0.12f);
                        float b0 = xu < 1f ? rimW + 0.02f : rimW;
                        float b1 = Mathf.Min(b0 + (xu < 1f ? 0.08f : 0.05f), wall - 0.05f);
                        if (b1 > b0 + 0.01f)
                        {
                            float band = Mathf.Clamp01(((fromOut - b0) * ppu) + 0.5f) * Mathf.Clamp01(((b1 - fromOut) * ppu) + 0.5f);
                            float alpha = xu < 1f ? (yu > cavityTop ? 0.80f : 0.85f) : 0.50f;
                            if (band > 0f)
                            {
                                pc.Blend(x, y, ColorMath.WithAlpha(white, alpha), covOuter * band * ends);
                            }
                        }
                    }

                    // Pared de la cavidad: línea de 1 dp blanca al 22 %.
                    if (inner > 0f && yu >= CuiaGeometry.CavityBottomU)
                    {
                        float wall = Mathf.Abs(dx - inner);
                        if (wall < lineU)
                        {
                            pc.Blend(x, y, ColorMath.WithAlpha(white, 0.22f), 1f - (wall / lineU));
                        }
                    }

                    // Contorno exterior: 0,07 u (mínimo 1,2 dp), vidrio_borde al 90 % (10:1 frente al panel).
                    float ring = covOuter - PixelCanvas.Coverage((dx - (outer - rimW)) * ppu);
                    if (ring > 0f)
                    {
                        pc.Blend(x, y, ColorMath.WithAlpha(rim, 0.90f), ring);
                    }

                }
            }

            return MakeSprite(pc, "cuia_front_" + capacity + "_" + ppu, new Vector2(0.5f, 0f), Vector4.zero);
        }

        private Sprite BuildCuiaMarks(int capacity, int ppu)
        {
            // Marcas de dosis (07 §4.1), encima de todos los brillos (V4): 1 dp, marfil al 25 %, solo dentro de la cavidad;
            // en arco, como las fronteras del líquido y el fondo (D-006; CuiaGeometry.FaceSagU).
            int w = Mathf.RoundToInt(CuiaGeometry.WidthU * ppu);
            int h = Mathf.RoundToInt(CuiaGeometry.HeightU(capacity) * ppu);
            var pc = new PixelCanvas(w, h);
            float lineU = Mathf.Max(1f / ppu, Mathf.Max(0.5f, ScreenScale) / ppu); // 1 dp en u
            var ivory = new Color(0xFF / 255f, 0xF8 / 255f, 0xE6 / 255f, 1f);
            for (int k = 0; k <= capacity; k++)
            {
                float my = CuiaGeometry.CavityBottomU + k;
                int y0 = Mathf.Max(0, Mathf.FloorToInt((my - CuiaGeometry.FaceSagU(0f) - lineU) * ppu));
                int y1 = Mathf.Min(h - 1, Mathf.CeilToInt((my + lineU) * ppu));
                for (int y = y0; y <= y1; y++)
                {
                    float yu = (y + 0.5f) / ppu;
                    float inner = CuiaGeometry.InnerHalfWidth(Mathf.Max(yu, CuiaGeometry.CavityBottomU), capacity);
                    for (int x = 0; x < w; x++)
                    {
                        float xu = (x + 0.5f) / ppu;
                        if (Mathf.Abs(xu - 1f) >= inner)
                        {
                            continue;
                        }

                        float d = Mathf.Abs(yu - (my - CuiaGeometry.FaceSagU(xu - 1f)));
                        if (d >= lineU * 0.6f)
                        {
                            continue;
                        }

                        float alpha = k == 0 ? 0.22f : 0.25f;
                        pc.Blend(x, y, ColorMath.WithAlpha(ivory, alpha), 1f - (d / (lineU * 0.6f)));
                    }
                }
            }

            return MakeSprite(pc, "cuia_marks_" + capacity + "_" + ppu, new Vector2(0.5f, 0f), Vector4.zero);
        }

        private Sprite BuildSealCap()
        {
            // Tapa de calabaza (07 §4.4): #C9A36B con veta y brillo, luz desde arriba a la izquierda. Mide 1,10 × 0,35 u:
            // 144 × 48 px (≈ 131 px/u ≥ MaxCuiaPixelsPerU) para que no se amplíe con u = 32 dp a 4 px/dp (D-004). Las cotas
            // son las del dibujo original de 108 × 36 px escaladas por k.
            const float k = 4f / 3f;
            const int w = 144;
            const int h = 48;
            var pc = new PixelCanvas(w, h);
            Color wood = Palette.CapWood;
            Color dark = ColorMath.Darken(wood, 0.30f);
            Color light = ColorMath.Lighten(wood, 0.45f);
            const float cx = 54f * k;
            const float cy = 4.5f * k;
            const float rx = 51f * k;
            const float ry = 28f * k;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = (x + 0.5f - cx) / rx;
                    float py = (y + 0.5f - cy) / ry;
                    float e = Mathf.Sqrt((px * px) + (py * py));
                    float d = (e - 1f) * ry;
                    if (y + 0.5f < cy - (1.5f * k))
                    {
                        d = Mathf.Max(d, (cy - (1.5f * k)) - (y + 0.5f));
                    }

                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Color c = Color.Lerp(ColorMath.Darken(wood, 0.18f), ColorMath.Lighten(wood, 0.10f), Mathf.Clamp01((y - (4f * k)) / (26f * k)));
                    c = Color.Lerp(c, ColorMath.Darken(c, 0.15f), Mathf.Clamp01(px * 0.8f)); // sombra a la derecha
                    pc.Blend(x, y, c, cov);
                    if (y < 7f * k)
                    {
                        pc.Blend(x, y, dark, cov * 0.75f); // asiento sobre el borde
                    }

                    float veta = Mathf.Abs(e - 0.62f) * ry;
                    if (veta < 1.4f * k && y > 7f * k)
                    {
                        pc.Blend(x, y, dark, cov * (1f - (veta / (1.4f * k))) * 0.45f);
                    }

                    if (e > 0.9f && py > 0.2f && px < 0.2f)
                    {
                        pc.Blend(x, y, ColorMath.WithAlpha(light, 0.6f), cov * Mathf.Clamp01((e - 0.9f) * 10f)); // luz de borde
                    }
                }
            }

            pc.FillEllipseSoft(36f * k, 22f * k, 9f * k, 4.5f * k, 3f * k, ColorMath.WithAlpha(light, 0.75f)); // brillo
            pc.FillCircle(54f * k, 31f * k, 2.4f * k, ColorMath.WithAlpha(dark, 0.9f)); // pedúnculo
            return MakeSprite(pc, "seal_cap", new Vector2(0.5f, 0f), Vector4.zero);
        }

        private Sprite BuildRing()
        {
            // Aro del sellado (2,4 u) y de las salpicaduras: 2,4 × MaxCuiaPixelsPerU px de lado (308) para que no se amplíe
            // con u = 32 dp (D-004). Las cotas son las del dibujo original de 96 px escaladas por s / 96.
            int s = Mathf.CeilToInt(2.4f * MaxCuiaPixelsPerU);
            float k = s / 96f;
            float center = s / 2f;
            float radius = 39f * k;
            float coreHalf = 3f * k;
            float halo = 9f * k;
            var pc = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
            {
                float dy = (y + 0.5f) - center;
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) - center;
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float core = PixelCanvas.Coverage(Mathf.Abs(d - radius) - coreHalf);
                    float glow = Mathf.Clamp01(1f - (Mathf.Abs(d - radius) / halo));
                    pc.Blend(x, y, Color.white, Mathf.Max(core, glow * glow * 0.55f));
                }
            }

            return MakeSprite(pc, "ring", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildDisc()
        {
            var pc = new PixelCanvas(32, 32);
            pc.FillCircle(16f, 16f, 15f, Color.white);
            return MakeSprite(pc, "disc", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildGlow()
        {
            const int s = 32;
            var pc = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f)) / 16f;
                    float a = Mathf.Clamp01(1f - d);
                    pc.Set(x, y, new Color(1f, 1f, 1f, a * a * (0.6f + (0.4f * a))));
                }
            }

            return MakeSprite(pc, "glow", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildStar()
        {
            const int s = 96;
            var pc = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f - 48f) / 48f);
                    float dy = Mathf.Abs((y + 0.5f - 48f) / 48f);
                    float ray = Mathf.Max(Mathf.Clamp01(1f - (dx * 1f) - (dy * 9f)), Mathf.Clamp01(1f - (dy * 1f) - (dx * 9f)));
                    float r = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float core = Mathf.Clamp01(1f - (r * 3.2f));
                    float a = Mathf.Max(ray * ray, core * core);
                    pc.Set(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            return MakeSprite(pc, "star", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildDrop()
        {
            const int s = 24;
            var pc = new PixelCanvas(s, s);
            pc.FillCircle(12f, 12f, 10.5f, new Color(0.82f, 0.82f, 0.82f, 1f));
            pc.FillEllipseSoft(9f, 15f, 4f, 3f, 2f, new Color(1f, 1f, 1f, 0.95f)); // brillo arriba a la izquierda
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(12f, 12f));
                    if (d > 8.5f && d < 11f)
                    {
                        pc.Blend(x, y, new Color(1f, 1f, 1f, 0.5f), 1f - Mathf.Abs(d - 9.75f) / 1.25f); // borde claro
                    }
                }
            }

            return MakeSprite(pc, "drop", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildGlintStreak()
        {
            var pc = new PixelCanvas(32, 8);
            pc.FillRoundedRectSoft(3f, 2.5f, 29f, 5.5f, 1.5f, 3f, Color.white);
            return MakeSprite(pc, "glint", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildRoundedRect()
        {
            var pc = new PixelCanvas(64, 64);
            pc.FillRoundedRect(0f, 0f, 64f, 64f, RoundedRectRadiusPx, Color.white);
            return MakeSprite(pc, "rounded_rect", new Vector2(0.5f, 0.5f), new Vector4(24f, 24f, 24f, 24f));
        }

        private Sprite BuildFeather()
        {
            // Pluma de guacamayo: vexilo asimétrico con barbas y raquis (se tiñe con Image.color).
            const int w = 24;
            const int h = 72;
            var pc = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = (y + 0.5f) / h; // 0 abajo (cálamo), 1 punta
                    float half = 10.5f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Clamp01((t - 0.12f) / 0.88f))), 0.7f);
                    float off = (x + 0.5f) - 12f;
                    float edge = (Mathf.Abs(off) - half);
                    float cov = t > 0.12f ? PixelCanvas.Coverage(edge) : 0f;
                    float barb = 0.85f + (0.15f * Mathf.Sin(((y * 0.9f) + (Mathf.Abs(off) * 0.8f)) * 1.3f));
                    float shade = off < 0f ? 1f : 0.82f;
                    pc.Blend(x, y, new Color(barb * shade, barb * shade, barb * shade, 1f), cov);
                }
            }

            pc.FillCapsule(12f, 1f, 12.5f, 69f, 0.9f, new Color(1f, 1f, 1f, 0.85f)); // raquis claro
            return MakeSprite(pc, "feather", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildLeafBit()
        {
            var pc = new PixelCanvas(28, 48);
            DrawLeaf(pc, new Vector2(14f, 2f), new Vector2(0f, 1f), 44f, 22f, 0.06f, LeafShape.Ovate, Palette.LeafMid, Palette.LeafLight, 1f, 1);
            return MakeSprite(pc, "leaf_bit", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildPetal()
        {
            return MakeSprite(BuildPetalCanvas(), "petal", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Pétalo de heliconia (bráctea): lienzo aparte para exportarlo a revisión (ExportSceneFallbacks).
        internal static PixelCanvas BuildPetalCanvas()
        {
            var pc = new PixelCanvas(24, 40);
            DrawLeaf(pc, new Vector2(12f, 2f), new Vector2(0f, 1f), 36f, 18f, 0.10f, LeafShape.Bract, Palette.Heliconia, Hex(0xFF8A3D), 0.5f, 2);
            return pc;
        }

        private Sprite BuildContactShadow()
        {
            // Elipse negra con borde suave; el alfa (0,55 en reposo, 0,25 elevada) lo pone Image.color (guía 4.1).
            const int w = 128;
            const int h = 32;
            var pc = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = (x + 0.5f - 64f) / 64f;
                    float py = (y + 0.5f - 16f) / 16f;
                    float r = Mathf.Sqrt((px * px) + (py * py));
                    float a = 1f - Smooth(0.5f, 1f, r); // núcleo opaco: la Image le da el alfa de 0,55 (guía 4.1)
                    pc.Set(x, y, new Color(0f, 0f, 0f, a));
                }
            }

            return MakeSprite(pc, "contact_shadow", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private static void DrawWave(PixelCanvas pc, float x0, float x1, float yMid, float amplitude, float wavelength, float radius, Color c)
        {
            float prevX = x0;
            float prevY = yMid + (amplitude * Mathf.Sin(0f));
            for (float x = x0 + 2f; x <= x1; x += 2f)
            {
                float y = yMid + (amplitude * Mathf.Sin((x - x0) * 2f * Mathf.PI / wavelength));
                pc.FillCapsule(prevX, prevY, x, y, radius, c);
                prevX = x;
                prevY = y;
            }
        }

        private static void DrawArrowArc(PixelCanvas pc, Vector2 center, float radius, float thickness, float fromDeg, float toDeg, Color c, float headScale = 1f)
        {
            pc.StrokeArc(center.x, center.y, radius, thickness, fromDeg, toDeg, c);
            float a = toDeg * Mathf.Deg2Rad;
            var p = new Vector2(center.x + (Mathf.Cos(a) * radius), center.y + (Mathf.Sin(a) * radius));
            var dir = new Vector2(-Mathf.Sin(a), Mathf.Cos(a)); // tangente en sentido antihorario
            var perp = new Vector2(dir.y, -dir.x);
            Vector2 tip = p + (dir * (thickness * 1.7f * headScale));
            Vector2 b0 = p - (dir * 1.5f) + (perp * (thickness * 1.45f * headScale));
            Vector2 b1 = p - (dir * 1.5f) - (perp * (thickness * 1.45f * headScale));
            pc.FillTriangle(tip, b0, b1, c);
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + (ab * t));
        }

        private static Color Hex(int rgb)
        {
            return Palette.Hex(rgb);
        }

        // smoothstep de GLSL: 0 por debajo de e0, 1 por encima de e1 y curva suave entre medias. (Mathf.SmoothStep no es
        // esto: interpola entre sus dos primeros argumentos.)
        private static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - (2f * t));
        }
    }
}
