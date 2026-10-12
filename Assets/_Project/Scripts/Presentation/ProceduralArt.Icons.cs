#nullable enable
using System.Diagnostics;
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Iconos de accesibilidad por tinte (07 §3, columna «Icono de accesibilidad»; T-E.3) [P, provisionales hasta el arte del
    /// artista]: fruto espinoso, vaina ovalada, ciruela, fruto escamoso, cápsula erizada, cereza, racimo, bol, fruto con sombra,
    /// gota, fruto peludo, fruto aterciopelado, mazorca y hoja hundida. Son siluetas de un solo canal (blanco con la misma
    /// máscara en RGBA) que el shader del líquido tiñe con <see cref="Palette.PatternBase"/> del tinte, centradas en cada dosis
    /// visible (<see cref="LiquidGraphic.SetFruitIcons"/>). No son las frutas en vuelo de la intro
    /// (<see cref="FruitFlight"/>, 07 §6.4): esas van a todo color, con contorno de tinta, y el GDD las distingue a propósito
    /// (07 §3, fila Açaí: «el icono de accesibilidad es un bol: en vuelo, la ramita»).
    /// </summary>
    /// <remarks>
    /// <para>Atlas propio (<see cref="FruitIconAtlas"/>): una columna de <see cref="FruitIconCells"/> celdas de
    /// <see cref="FruitIconPx"/> px, una por índice de motor (la 0, agua base, y la 15, Barro, vacías), lineal, sin mipmaps,
    /// de abajo arriba. Va aparte del atlas de patrones porque los canales RGB de las filas de fruta de ese atlas no están libres:
    /// el bocado de la Piraña (<c>PiranaOverlay</c>) los muestra tal cual en una <c>RawImage</c>. Se genera de forma perezosa la
    /// primera vez que se pide, al crear la primera cuia (<see cref="LiquidGraphic.Setup"/>, en la carga del nivel), nunca
    /// dentro de un paso.</para>
    /// <para>Cada celda es la caja del icono (0,5 u con la dosis llena, el alto del «?» de las turbias; 07 §4.3) y la silueta
    /// ocupa <see cref="FruitIconFill"/> de su alto: 0,45 u, el de 07 §3, regla 1 (02 §12 dice «60 % del ancho de la dosis»;
    /// se alinea en el paso 2). Siluetas gruesas, con huecos de 2-3 px como mínimo, para leerse a 48 dp (≈ 30 px en el
    /// SM-S938B).</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Lado de cada celda del atlas de iconos de fruta (px).</summary>
        public const int FruitIconPx = 64;

        /// <summary>Celdas del atlas de iconos: una por índice de motor 0-15 (0 agua base y 15 Barro, vacías).</summary>
        public const int FruitIconCells = Cuia.MudColor + 1;

        /// <summary>Fracción del alto de la celda que ocupa la silueta: 0,45 u en la caja de 0,5 u (07 §3, regla 1).</summary>
        public const float FruitIconFill = 0.9f;

        private Texture2D? _fruitIconAtlas;

        /// <summary>Tiempo de generación del atlas de iconos (ms; 0 si aún no se generó).</summary>
        public double FruitIconAtlasMs { get; private set; }

        /// <summary>
        /// Atlas de iconos de accesibilidad por tinte (ver la clase): <see cref="FruitIconPx"/> × (<see cref="FruitIconPx"/> ·
        /// <see cref="FruitIconCells"/>) px, la máscara en RGBA. Lo genera la primera llamada.
        /// </summary>
        public Texture2D FruitIconAtlas
        {
            get
            {
                if (_fruitIconAtlas == null)
                {
                    Stopwatch watch = Stopwatch.StartNew();
                    var px = new Color32[FruitIconPx * FruitIconPx * FruitIconCells];
                    var mask = new float[FruitIconPx * FruitIconPx];
                    for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
                    {
                        System.Array.Clear(mask, 0, mask.Length);
                        DrawFruitIcon(mask, tint);
                        int offset = tint * FruitIconPx * FruitIconPx;
                        for (int i = 0; i < mask.Length; i++)
                        {
                            byte v = (byte)Mathf.RoundToInt(Mathf.Clamp01(mask[i]) * 255f);
                            px[offset + i] = new Color32(v, v, v, v);
                        }
                    }

                    var tex = new Texture2D(FruitIconPx, FruitIconPx * FruitIconCells, TextureFormat.RGBA32, false, true)
                    {
                        name = "fruit_icon_atlas",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.DontSave,
                    };
                    tex.SetPixels32(px);
                    tex.Apply(false, true);
                    _owned.Add(tex);
                    _fruitIconAtlas = tex;
                    watch.Stop();
                    FruitIconAtlasMs = watch.Elapsed.TotalMilliseconds;
                }

                return _fruitIconAtlas;
            }
        }

        /// <summary>El tinte tiene icono de accesibilidad: los 14 de 07 §3 (ni el agua base ni el Barro).</summary>
        public static bool HasFruitIcon(int tint)
        {
            return tint >= 1 && tint <= Cuia.MaxTintColor;
        }

        /// <summary>
        /// Máscara (0..1) del icono del tinte <paramref name="tint"/> en su celda de <see cref="FruitIconPx"/> ×
        /// <see cref="FruitIconPx"/> px, de abajo arriba (índice y · <see cref="FruitIconPx"/> + x): la misma que lleva el
        /// atlas. Para tests y herramientas (asigna); vacía sin icono.
        /// </summary>
        public static float[] FruitIconMask(int tint)
        {
            var mask = new float[FruitIconPx * FruitIconPx];
            if (HasFruitIcon(tint))
            {
                DrawFruitIcon(mask, tint);
            }

            return mask;
        }

        // Silueta del tinte en la celda de 64 × 64 px (origen abajo a la izquierda): de y ≈ 3 a y ≈ 61 (FruitIconFill).
        private static void DrawFruitIcon(float[] m, int tint)
        {
            switch (tint)
            {
                case 1: // Guanábana: fruto espinoso (óvalo alargado e inclinado, con espinas cortas y gruesas y su rabito curvo).
                    {
                        const float cx = 31f;
                        const float cy = 29f;
                        const float rx = 15f;
                        const float ry = 22f;
                        const float tilt = -22f;
                        IconEllipse(m, cx, cy, rx, ry, tilt, false);
                        float rad = tilt * Mathf.Deg2Rad;
                        float c = Mathf.Cos(rad);
                        float s = Mathf.Sin(rad);
                        for (int i = 0; i < 11; i++)
                        {
                            float a = (i + 0.5f) / 11f * Mathf.PI * 2f;
                            if (a > 1.2f && a < 1.95f)
                            {
                                continue; // el rabito
                            }

                            var local = new Vector2(rx * Mathf.Cos(a), ry * Mathf.Sin(a));
                            var normal = new Vector2(Mathf.Cos(a) / rx, Mathf.Sin(a) / ry).normalized;
                            Vector2 p = new Vector2(cx, cy) + Rotate(local, c, s);
                            Vector2 n = Rotate(normal, c, s);
                            var t = new Vector2(-n.y, n.x);
                            IconTriangle(m, p + (t * 4.2f) - (n * 1.5f), p - (t * 4.2f) - (n * 1.5f), p + (n * 6.0f), false);
                        }

                        // Espinas también dentro de la silueta (huecos en V), para que no se lea como una bola peluda.
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 q = new Vector2(cx, cy) + Rotate(new Vector2(i % 2 == 0 ? -5f : 5f, -11f + (i * 7.5f)), c, s);
                            IconTriangle(m, q + new Vector2(-2.6f, -1.8f), q + new Vector2(2.6f, -1.8f), q + new Vector2(0f, 2.8f), true);
                        }

                        IconCapsule(m, 38f, 48.5f, 39.5f, 54f, 2.5f, false);
                        IconCapsule(m, 39.5f, 54f, 36f, 59.5f, 2.5f, false);
                        break;
                    }

                case 2: // Copoazú: vaina ovalada inclinada, con su costura y el rabito.
                    IconEllipse(m, 30f, 30f, 15f, 28f, -35f, false);
                    IconCapsule(m, 22f, 17.5f, 39f, 42f, 1.6f, true);
                    IconCapsule(m, 45f, 52f, 50f, 60.5f, 2.4f, false);
                    break;

                case 3: // Taperebá: ciruela con su surco, el rabito y una hoja.
                    IconCircle(m, 30f, 26f, 21f, false);
                    IconCapsule(m, 30f, 43f, 26.5f, 32f, 1.4f, true);
                    IconCapsule(m, 26.5f, 32f, 27f, 18f, 1.4f, true);
                    IconCapsule(m, 31f, 45f, 33f, 56f, 2.4f, false);
                    IconEllipse(m, 44f, 54f, 11f, 5f, 20f, false);
                    IconCapsule(m, 35.5f, 51.5f, 51f, 56.5f, 1.0f, true);
                    break;

                case 4: // Aguaje: fruto escamoso (huevo con hileras de escamas y una coronita).
                    IconEllipse(m, 32f, 29f, 19f, 25f, 0f, false);
                    IconTriangle(m, new Vector2(25f, 51f), new Vector2(29.5f, 51f), new Vector2(26f, 59f), false);
                    IconTriangle(m, new Vector2(29.5f, 52f), new Vector2(34.5f, 52f), new Vector2(32f, 61f), false);
                    IconTriangle(m, new Vector2(34.5f, 51f), new Vector2(39f, 51f), new Vector2(38f, 59f), false);
                    for (int row = 0; row < 5; row++)
                    {
                        float y = 12f + (row * 8f);
                        float shift = (row % 2) * 5f;
                        for (int col = -2; col <= 2; col++)
                        {
                            float x = 32f + (col * 10f) + shift - 2.5f;
                            float dx = (x - 32f) / 15f;
                            float dy = (y - 29f) / 21f;
                            if ((dx * dx) + (dy * dy) < 1f)
                            {
                                IconArc(m, x, y + 2f, 5f, 2.2f, 200f, 340f, true);
                            }
                        }
                    }

                    break;

                case 5: // Achiote: cápsula erizada (gota con la punta arriba, cerdas largas y la juntura de sus valvas).
                    {
                        const float cx = 32f;
                        const float cy = 26f;
                        const float r = 15f;
                        var tip = new Vector2(32f, 54f);
                        float d = tip.y - cy;
                        float th = Mathf.Acos(r / d);
                        var left = new Vector2(cx - (r * Mathf.Sin(th)), cy + (r * Mathf.Cos(th)));
                        var right = new Vector2(cx + (r * Mathf.Sin(th)), cy + (r * Mathf.Cos(th)));
                        IconCircle(m, cx, cy, r, false);
                        IconTriangle(m, tip, left, right, false);
                        for (int i = 0; i < 11; i++)
                        {
                            float a = (165f + (i * 21f)) * Mathf.Deg2Rad;
                            var n = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                            IconCapsule(m, cx + (n.x * (r - 1f)), cy + (n.y * (r - 1f)), cx + (n.x * (r + 6f)), cy + (n.y * (r + 6f)), 1.2f, false);
                        }

                        for (int side = -1; side <= 1; side += 2)
                        {
                            Vector2 a0 = side < 0 ? left : right;
                            Vector2 edge = tip - a0;
                            var n = new Vector2(side * edge.y, -side * edge.x).normalized;
                            if (n.x * side < 0f)
                            {
                                n = -n;
                            }

                            for (int k = 1; k <= 2; k++)
                            {
                                Vector2 p = a0 + (edge * (k / 3f));
                                IconCapsule(m, p.x - n.x, p.y - n.y, p.x + (n.x * 6f), p.y + (n.y * 6f), 1.2f, false);
                            }
                        }

                        IconCapsule(m, tip.x, tip.y - 1f, tip.x, 60f, 1.2f, false);
                        IconCapsule(m, 32f, 46f, 32f, 14f, 1.3f, true);
                        break;
                    }

                case 6: // Camu camu: cereza (dos bayas con sus rabitos, una hoja y un brillo).
                    IconCircle(m, 21f, 18f, 12.5f, false);
                    IconCircle(m, 43f, 21f, 12.5f, false);
                    IconCapsule(m, 22f, 29f, 35f, 55f, 2.0f, false);
                    IconCapsule(m, 42f, 32f, 35f, 55f, 2.0f, false);
                    IconEllipse(m, 44f, 56f, 4f, 9f, -70f, false);
                    IconCircle(m, 16.5f, 22f, 3.4f, true);
                    IconCircle(m, 38.5f, 25f, 3.4f, true);
                    break;

                case 7: // Bacaba: racimo de bayas (4-3-2-1) colgando de su raquis.
                    IconCapsule(m, 32f, 46f, 33f, 59f, 2.2f, false);
                    IconBerryRow(m, 44f, 4);
                    IconBerryRow(m, 33f, 3);
                    IconBerryRow(m, 22f, 2);
                    IconBerryRow(m, 11f, 1);
                    break;

                case 8: // Açaí: bol (tigela) con su borde, su pie y el montón de bayas.
                    IconHalfEllipseBelow(m, 32f, 28f, 26f, 19f, false);
                    IconRoundBox(m, 5f, 26.5f, 59f, 32f, 2.7f, false);
                    IconRoundBox(m, 23f, 4f, 41f, 10f, 1.5f, false);
                    IconCapsule(m, 9f, 25.6f, 55f, 25.6f, 1.1f, true);
                    IconCircle(m, 13f, 36f, 6.2f, false);
                    IconCircle(m, 25f, 39f, 6.6f, false);
                    IconCircle(m, 38f, 39.5f, 6.6f, false);
                    IconCircle(m, 50f, 36f, 6.2f, false);
                    IconCircle(m, 19.5f, 47f, 6.4f, false);
                    IconCircle(m, 32f, 50f, 6.6f, false);
                    IconCircle(m, 44.5f, 47f, 6.4f, false);
                    IconCircle(m, 32f, 57f, 4.6f, false);
                    break;

                case 9: // Huito: fruto con sombra (redondo, con rabito y hojita, sobre su sombra aparte).
                    IconCircle(m, 32f, 34f, 17.5f, false);
                    IconCapsule(m, 32f, 50f, 35f, 59f, 2.2f, false);
                    IconEllipse(m, 42f, 58f, 3f, 6.5f, -65f, false);
                    IconEllipse(m, 25.5f, 41f, 3.2f, 5.6f, 35f, true);
                    IconEllipse(m, 34f, 8f, 21f, 4.6f, 0f, false);
                    break;

                case 10: // Agua del Tapajós: gota (con su brillo).
                    {
                        const float cx = 32f;
                        const float cy = 22f;
                        const float r = 17f;
                        var tip = new Vector2(32f, 61f);
                        float th = Mathf.Acos(r / (tip.y - cy));
                        IconCircle(m, cx, cy, r, false);
                        IconTriangle(m, tip, new Vector2(cx - (r * Mathf.Sin(th)), cy + (r * Mathf.Cos(th))), new Vector2(cx + (r * Mathf.Sin(th)), cy + (r * Mathf.Cos(th))), false);
                        IconArc(m, cx, cy, 10.5f, 3.2f, 140f, 205f, true);
                        break;
                    }

                case 11: // Lulo: fruto peludo (bola con pelillos y el cáliz en estrella).
                    IconCircle(m, 32f, 27f, 18f, false);
                    for (int i = 0; i < 24; i++)
                    {
                        float deg = i * 15f;
                        if (deg > 55f && deg < 125f)
                        {
                            continue; // el cáliz
                        }

                        float a = deg * Mathf.Deg2Rad;
                        IconCapsule(m, 32f + (Mathf.Cos(a) * 16.5f), 27f + (Mathf.Sin(a) * 16.5f), 32f + (Mathf.Cos(a) * 23f), 27f + (Mathf.Sin(a) * 23f), 1.0f, false);
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        float a = (90f + (i * 72f)) * Mathf.Deg2Rad;
                        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        var side = new Vector2(-d.y, d.x);
                        var c = new Vector2(32f, 46f);
                        IconTriangle(m, c + (d * 10f), c + (side * 3.4f), c - (side * 3.4f), false);
                    }

                    IconCapsule(m, 32f, 50f, 30.5f, 59f, 2.0f, false);
                    break;

                case 12: // Arazá: fruto aterciopelado (ancho y achatado, punteado, con su coronita y el rabito).
                    IconEllipse(m, 32f, 28f, 24f, 21f, 0f, false);
                    IconCircle(m, 26.5f, 49f, 2.8f, false);
                    IconCircle(m, 37.5f, 49f, 2.8f, false);
                    IconCapsule(m, 32f, 48f, 33f, 59f, 2.0f, false);
                    for (int row = 0; row < 4; row++)
                    {
                        float y = 15f + (row * 9f);
                        for (int col = -2; col <= 2; col++)
                        {
                            float x = 32f + (col * 9f) + ((row % 2) * 4.5f);
                            float dx = (x - 32f) / 18f;
                            float dy = (y - 28f) / 15f;
                            if ((dx * dx) + (dy * dy) < 1f)
                            {
                                IconCircle(m, x, y, 1.9f, true);
                            }
                        }
                    }

                    break;

                case 13: // Cacao: mazorca (huso con dos costillas y el pedúnculo).
                    IconLens(m, 32f, 31f, 27f, 14f, -18f, false);
                    IconLensOutline(m, 32f, 31f, 23.5f, 6f, -18f, 2.4f, true);
                    IconCapsule(m, 40f, 55f, 42.5f, 59.5f, 2.2f, false);
                    break;

                default: // 14 · Agua del Río Negro: hoja hundida (bajo la línea del agua, con burbujas que suben).
                    for (int i = 0; i < 12; i++)
                    {
                        float x0 = 7f + (i * 4.2f);
                        float x1 = x0 + 4.2f;
                        IconCapsule(m, x0, 56f + (2.8f * Mathf.Sin(x0 * 0.37f)), x1, 56f + (2.8f * Mathf.Sin(x1 * 0.37f)), 2.0f, false);
                    }

                    IconLens(m, 30f, 24f, 21f, 9.5f, 52f, false);
                    {
                        float rad = 52f * Mathf.Deg2Rad;
                        var axis = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
                        var c = new Vector2(30f, 24f);
                        Vector2 a = c - (axis * 17f);
                        Vector2 b = c + (axis * 17f);
                        IconCapsule(m, a.x, a.y, b.x, b.y, 1.1f, true);
                        Vector2 stemFrom = c - (axis * 20f);
                        Vector2 stemTo = c - (axis * 28f);
                        IconCapsule(m, stemFrom.x, stemFrom.y, stemTo.x, stemTo.y, 1.8f, false);
                    }

                    IconCircle(m, 45f, 41f, 2.6f, false);
                    IconCircle(m, 49f, 47.5f, 2.0f, false);
                    break;
            }
        }

        // Fila del racimo de la Bacaba: n bayas de 5,6 px de radio a 13 px, centradas en x = 32 a la altura y.
        private static void IconBerryRow(float[] m, float y, int n)
        {
            float x0 = 32f - ((n - 1) * 6.5f);
            for (int i = 0; i < n; i++)
            {
                IconCircle(m, x0 + (i * 13f), y, 5.6f, false);
            }
        }

        private static Vector2 Rotate(Vector2 v, float c, float s)
        {
            return new Vector2((v.x * c) - (v.y * s), (v.x * s) + (v.y * c));
        }

        // Escribe la cobertura de la distancia con signo d (px, negativa dentro) en el píxel (x, y): une (máximo) o recorta
        // (mínimo con 1 − cobertura).
        private static void IconWrite(float[] m, int x, int y, float d, bool cut)
        {
            if (x < 0 || y < 0 || x >= FruitIconPx || y >= FruitIconPx)
            {
                return;
            }

            float cov = PixelCanvas.Coverage(d);
            int i = (y * FruitIconPx) + x;
            m[i] = cut ? Mathf.Min(m[i], 1f - cov) : Mathf.Max(m[i], cov);
        }

        private static void IconBox(float minX, float minY, float maxX, float maxY, out int x0, out int y0, out int x1, out int y1)
        {
            x0 = Mathf.Max(0, Mathf.FloorToInt(minX) - 2);
            y0 = Mathf.Max(0, Mathf.FloorToInt(minY) - 2);
            x1 = Mathf.Min(FruitIconPx - 1, Mathf.CeilToInt(maxX) + 2);
            y1 = Mathf.Min(FruitIconPx - 1, Mathf.CeilToInt(maxY) + 2);
        }

        private static void IconCircle(float[] m, float cx, float cy, float r, bool cut)
        {
            IconBox(cx - r, cy - r, cx + r, cy + r, out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f) - cx;
                    float dy = (y + 0.5f) - cy;
                    IconWrite(m, x, y, Mathf.Sqrt((dx * dx) + (dy * dy)) - r, cut);
                }
            }
        }

        // Elipse de semiejes rx (x local) y ry (y local) girada deg grados en sentido antihorario.
        private static void IconEllipse(float[] m, float cx, float cy, float rx, float ry, float deg, bool cut)
        {
            float rad = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            float big = Mathf.Max(rx, ry);
            float small = Mathf.Min(rx, ry);
            IconBox(cx - big, cy - big, cx + big, cy + big, out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f) - cx;
                    float py = (y + 0.5f) - cy;
                    float lx = ((px * c) + (py * s)) / rx;
                    float ly = ((-px * s) + (py * c)) / ry;
                    IconWrite(m, x, y, (Mathf.Sqrt((lx * lx) + (ly * ly)) - 1f) * small, cut);
                }
            }
        }

        // Media elipse por debajo de cy (el cuenco del bol).
        private static void IconHalfEllipseBelow(float[] m, float cx, float cy, float rx, float ry, bool cut)
        {
            float small = Mathf.Min(rx, ry);
            IconBox(cx - rx, cy - ry, cx + rx, cy, out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = ((x + 0.5f) - cx) / rx;
                    float py = ((y + 0.5f) - cy) / ry;
                    float d = (Mathf.Sqrt((px * px) + (py * py)) - 1f) * small;
                    IconWrite(m, x, y, Mathf.Max(d, (y + 0.5f) - cy), cut);
                }
            }
        }

        private static void IconCapsule(float[] m, float ax, float ay, float bx, float by, float r, bool cut)
        {
            IconBox(Mathf.Min(ax, bx) - r, Mathf.Min(ay, by) - r, Mathf.Max(ax, bx) + r, Mathf.Max(ay, by) + r, out int x0, out int y0, out int x1, out int y1);
            float vx = bx - ax;
            float vy = by - ay;
            float len2 = Mathf.Max(1e-6f, (vx * vx) + (vy * vy));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f) - ax;
                    float py = (y + 0.5f) - ay;
                    float t = Mathf.Clamp01(((px * vx) + (py * vy)) / len2);
                    float dx = px - (vx * t);
                    float dy = py - (vy * t);
                    IconWrite(m, x, y, Mathf.Sqrt((dx * dx) + (dy * dy)) - r, cut);
                }
            }
        }

        private static void IconTriangle(float[] m, Vector2 p0, Vector2 p1, Vector2 p2, bool cut)
        {
            float cross = ((p1.x - p0.x) * (p2.y - p0.y)) - ((p1.y - p0.y) * (p2.x - p0.x));
            if (cross < 0f)
            {
                Vector2 t = p1;
                p1 = p2;
                p2 = t;
            }

            IconBox(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x)), Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y)), Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x)), Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y)), out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Mathf.Max(IconEdge(p0, p1, p), Mathf.Max(IconEdge(p1, p2, p), IconEdge(p2, p0, p)));
                    IconWrite(m, x, y, d, cut);
                }
            }
        }

        // Distancia con signo al borde a → b (positiva fuera, con el triángulo en sentido antihorario).
        private static float IconEdge(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 e = b - a;
            float len = Mathf.Max(1e-6f, e.magnitude);
            return -(((e.x * (p.y - a.y)) - (e.y * (p.x - a.x))) / len);
        }

        private static void IconRoundBox(float[] m, float minX, float minY, float maxX, float maxY, float r, bool cut)
        {
            IconBox(minX, minY, maxX, maxY, out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    IconWrite(m, x, y, PixelCanvas.RoundedRectDistance(x + 0.5f, y + 0.5f, minX, minY, maxX, maxY, r), cut);
                }
            }
        }

        // Arco de anillo (radio radius, grosor thickness) de fromDeg a toDeg en sentido antihorario, con los extremos redondos.
        private static void IconArc(float[] m, float cx, float cy, float radius, float thickness, float fromDeg, float toDeg, bool cut)
        {
            const int segments = 10;
            float half = 0.5f * thickness;
            for (int i = 0; i < segments; i++)
            {
                float a0 = Mathf.Lerp(fromDeg, toDeg, i / (float)segments) * Mathf.Deg2Rad;
                float a1 = Mathf.Lerp(fromDeg, toDeg, (i + 1) / (float)segments) * Mathf.Deg2Rad;
                IconCapsule(m, cx + (Mathf.Cos(a0) * radius), cy + (Mathf.Sin(a0) * radius), cx + (Mathf.Cos(a1) * radius), cy + (Mathf.Sin(a1) * radius), half, cut);
            }
        }

        // Huso (vesica) de semilargo halfLen a lo largo de su eje y semiancho halfWidth, con el eje girado deg grados en sentido
        // antihorario desde la vertical.
        private static void IconLens(float[] m, float cx, float cy, float halfLen, float halfWidth, float deg, bool cut)
        {
            LensCircles(halfLen, halfWidth, out float radius, out float offset);
            float rad = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            IconBox(cx - halfLen, cy - halfLen, cx + halfLen, cy + halfLen, out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    IconWrite(m, x, y, LensDistance((x + 0.5f) - cx, (y + 0.5f) - cy, c, s, radius, offset), cut);
                }
            }
        }

        // El contorno (grosor thickness) de un huso como el de IconLens: dos líneas curvas de punta a punta (las costillas).
        private static void IconLensOutline(float[] m, float cx, float cy, float halfLen, float halfWidth, float deg, float thickness, bool cut)
        {
            LensCircles(halfLen, halfWidth, out float radius, out float offset);
            float rad = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            float half = 0.5f * thickness;
            IconBox(cx - halfLen, cy - halfLen, cx + halfLen, cy + halfLen, out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float d = LensDistance((x + 0.5f) - cx, (y + 0.5f) - cy, c, s, radius, offset);
                    IconWrite(m, x, y, Mathf.Abs(d) - half, cut);
                }
            }
        }

        // Círculos del huso: radio R y separación a del eje, con R − a = semiancho y R² − a² = semilargo².
        private static void LensCircles(float halfLen, float halfWidth, out float radius, out float offset)
        {
            float sum = (halfLen * halfLen) / Mathf.Max(1e-3f, halfWidth);
            radius = 0.5f * (sum + halfWidth);
            offset = 0.5f * (sum - halfWidth);
        }

        private static float LensDistance(float px, float py, float c, float s, float radius, float offset)
        {
            float lx = (px * c) + (py * s);
            float ly = (-px * s) + (py * c);
            float d0 = Mathf.Sqrt(((lx + offset) * (lx + offset)) + (ly * ly)) - radius;
            float d1 = Mathf.Sqrt(((lx - offset) * (lx - offset)) + (ly * ly)) - radius;
            return Mathf.Max(d0, d1);
        }
    }
}
