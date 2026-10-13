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
    /// SM-S938B). Revisión de B3: la Guanábana, el Achiote y el Lulo, de cuerpo liso y grande y con los pinchos solo como
    /// textura interior (con espinas o cerdas por fuera se leían como bichos dentro del jugo); y en su segunda revisión, la hoja
    /// hundida, que cruza en diagonal una banda de agua baja (maciza por encima del agua y en negativo dentro de ella) en lugar
    /// del bloque de agua lleno, que se leía como un escudo o una pegatina.</para>
    /// <para>Contorno (canal G; <see cref="FruitIconHasRim"/>): los iconos marfil sobre un tinte de L* 45-55 llevan un
    /// contorno de ≈ 1 dp del propio tinte oscurecido, que los separa de las marcas marfil de su patrón.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Lado de cada celda del atlas de iconos de fruta (px).</summary>
        public const int FruitIconPx = 64;

        /// <summary>Celdas del atlas de iconos: una por índice de motor 0-15 (0 agua base y 15 Barro, vacías).</summary>
        public const int FruitIconCells = Cuia.MudColor + 1;

        /// <summary>Fracción del alto de la celda que ocupa la silueta: 0,45 u en la caja de 0,5 u (07 §3, regla 1).</summary>
        public const float FruitIconFill = 0.9f;

        /// <summary>
        /// Grosor del contorno oscuro de los iconos que lo llevan (<see cref="FruitIconHasRim"/>), en px de la celda [P]: ≈ 1 dp
        /// con la caja de 0,5 u en reposo y la cuia de ≈ 47 dp de ancho (23,4 dp por u: 64 px / 11,7 dp = 5,5 px por dp). Crece y
        /// mengua con la caja, como el icono.
        /// </summary>
        public const float FruitIconRimPx = 5.5f;

        /// <summary>L* (CIELAB) mínimo del tinte con icono en marfil que lleva contorno (<see cref="FruitIconHasRim"/>) [P].</summary>
        public const float FruitIconRimLightnessMin = 45f;

        /// <summary>L* (CIELAB) máximo del tinte con icono en marfil que lleva contorno (<see cref="FruitIconHasRim"/>) [P].</summary>
        public const float FruitIconRimLightnessMax = 55f;

        // «v» huecas de la Guanábana (x, y locales desde su centro, en px): 6, sueltas, sin filas.
        private static readonly float[] GuanabanaVs = { -7f, 11.5f, 6f, 13.5f, -1f, 3f, 10f, 1.5f, -8f, -6.5f, 4.5f, -11f };

        // Semillas del Achiote en la abertura (x, y, radio en px).
        private static readonly float[] AchioteSeeds = { 31f, 29f, 3.9f, 36f, 36.5f, 3.9f, 27.5f, 38.5f, 3.9f, 33f, 46f, 3.7f };

        // Hoja hundida del Agua del Río Negro (DrawSunkenLeafIcon), en px de la celda: la banda de agua (su canto, una sola ola
        // de la longitud de la banda: la cresta a la izquierda y el valle a la derecha) y la hoja que la cruza.
        private const float SunkenWaterFromX = 5f;
        private const float SunkenWaterToX = 59f;
        private const float SunkenWaterBottomY = 3f;
        private const float SunkenWaterTopY = 27f;
        private const float SunkenWaterAmp = 4f;
        private const float SunkenWaterPhase = -0.25f;
        private const float SunkenWaterCorner = 7f;
        private const float SunkenWaterWave = 56f;
        private const float SunkenLeafX = 31f;
        private const float SunkenLeafY = 32f;
        private const float SunkenLeafHalfLen = 24f;
        private const float SunkenLeafHalfWidth = 13.5f;
        private const float SunkenLeafDeg = -38f;
        private const float SunkenLeafVein = 1.6f;
        private const float SunkenLeafStem = 7f;
        private const float SunkenLeafGap = 2.2f;

        private Texture2D? _fruitIconAtlas;

        /// <summary>
        /// Tiempo de generación del atlas de iconos (ms; 0 si aún no se generó). Se paga una vez por sesión, en la primera carga
        /// de nivel: presupuesto de 30 ms en el Editor tras el JIT (<c>FruitPatternsTests.IconAtlas_GeneratedWithinBudget</c>);
        /// en el teléfono lo da la línea <c>AF_ART fruit_icons</c> de los builds de desarrollo.
        /// </summary>
        public double FruitIconAtlasMs { get; private set; }

        /// <summary>
        /// Atlas de iconos de accesibilidad por tinte (ver la clase): <see cref="FruitIconPx"/> × (<see cref="FruitIconPx"/> ·
        /// <see cref="FruitIconCells"/>) px, la silueta en R (y en B y A) y su contorno (<see cref="FruitIconRimMask"/>) en G. Lo
        /// genera la primera llamada.
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
                    var rim = new float[FruitIconPx * FruitIconPx];
                    for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
                    {
                        System.Array.Clear(mask, 0, mask.Length);
                        System.Array.Clear(rim, 0, rim.Length);
                        DrawFruitIcon(mask, tint);
                        if (FruitIconHasRim(tint))
                        {
                            DilateIcon(mask, rim, FruitIconRimPx);
                        }

                        int offset = tint * FruitIconPx * FruitIconPx;
                        for (int i = 0; i < mask.Length; i++)
                        {
                            byte v = IconByte(mask[i]);
                            byte g = IconByte(rim[i]);
                            px[offset + i] = new Color32(v, g, v, v);
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
                    if (UnityEngine.Debug.isDebugBuild)
                    {
                        UnityEngine.Debug.Log("AF_ART fruit_icons " + FruitIconAtlasMs.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " ms");
                    }
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

        /// <summary>
        /// El icono del tinte lleva un contorno de ≈ 1 dp (<see cref="FruitIconRimPx"/>) del propio tinte oscurecido
        /// (<see cref="LiquidGraphic.FruitIconRimDarken"/>) [P]: los iconos en marfil (<see cref="Palette.PatternBase"/>) sobre
        /// un tinte de L* entre <see cref="FruitIconRimLightnessMin"/> y <see cref="FruitIconRimLightnessMax"/> (Achiote y Agua
        /// del Río Negro), donde el marfil al 90 % contrasta poco (≈ 3,5:1) y se funde con las marcas marfil de su patrón. No
        /// cambia ningún HEX.
        /// </summary>
        public static bool FruitIconHasRim(int tint)
        {
            if (!HasFruitIcon(tint) || Palette.PatternBase(tint) != Palette.UiPaper)
            {
                return false;
            }

            float l = ColorMath.LabLightness(Palette.Tint(tint));
            return l >= FruitIconRimLightnessMin && l <= FruitIconRimLightnessMax;
        }

        /// <summary>
        /// Máscara (0..1) del contorno del icono del tinte <paramref name="tint"/> (canal G del atlas): la silueta engordada
        /// <see cref="FruitIconRimPx"/> px, que también rellena los huecos estrechos; vacía si el tinte no lleva contorno
        /// (<see cref="FruitIconHasRim"/>). Para tests y herramientas (asigna).
        /// </summary>
        public static float[] FruitIconRimMask(int tint)
        {
            var rim = new float[FruitIconPx * FruitIconPx];
            if (FruitIconHasRim(tint))
            {
                DilateIcon(FruitIconMask(tint), rim, FruitIconRimPx);
            }

            return rim;
        }

        // Cobertura 0..1 en un byte (al más cercano).
        private static byte IconByte(float v)
        {
            return (byte)(int)((Mathf.Clamp01(v) * 255f) + 0.5f);
        }

        // Dilatación con antialias: cada píxel de la silueta (con su cobertura) tiñe los que quedan a radius px o menos. Los del
        // interior (lleno, con sus 8 vecinos llenos) solo se tiñen a sí mismos: la silueta engordada es la silueta más los discos
        // de su borde, y a cualquier píxel al que llegue el disco de uno interior le llega antes, más cerca y con cobertura 1, el
        // de un píxel lleno del borde. La raíz solo se calcula en el canto del disco (de radius − 0,5 a radius + 0,5 px).
        private static void DilateIcon(float[] mask, float[] dst, float radius)
        {
            int reach = Mathf.CeilToInt(radius + 1f);
            float outer = (radius + 0.5f) * (radius + 0.5f);
            float inner = radius > 0.5f ? (radius - 0.5f) * (radius - 0.5f) : 0f;
            for (int qy = 0; qy < FruitIconPx; qy++)
            {
                for (int qx = 0; qx < FruitIconPx; qx++)
                {
                    int q = (qy * FruitIconPx) + qx;
                    float mq = mask[q];
                    if (mq <= 0f)
                    {
                        continue;
                    }

                    if (IsInteriorIconPixel(mask, qx, qy))
                    {
                        if (mq > dst[q])
                        {
                            dst[q] = mq;
                        }

                        continue;
                    }

                    int y0 = Mathf.Max(0, qy - reach);
                    int y1 = Mathf.Min(FruitIconPx - 1, qy + reach);
                    int x0 = Mathf.Max(0, qx - reach);
                    int x1 = Mathf.Min(FruitIconPx - 1, qx + reach);
                    for (int y = y0; y <= y1; y++)
                    {
                        for (int x = x0; x <= x1; x++)
                        {
                            float dx = x - qx;
                            float dy = y - qy;
                            float d2 = (dx * dx) + (dy * dy);
                            if (d2 >= outer)
                            {
                                continue;
                            }

                            float v = d2 <= inner ? mq : mq * PixelCanvas.Coverage(Mathf.Sqrt(d2) - radius);
                            int i = (y * FruitIconPx) + x;
                            if (v > dst[i])
                            {
                                dst[i] = v;
                            }
                        }
                    }
                }
            }
        }

        // El píxel y sus 8 vecinos están llenos (cobertura 1); en el borde de la celda, nunca.
        private static bool IsInteriorIconPixel(float[] mask, int x, int y)
        {
            if (x <= 0 || y <= 0 || x >= FruitIconPx - 1 || y >= FruitIconPx - 1)
            {
                return false;
            }

            for (int dy = -1; dy <= 1; dy++)
            {
                int row = (y + dy) * FruitIconPx;
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (mask[row + x + dx] < 1f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Silueta del tinte en la celda de 64 × 64 px (origen abajo a la izquierda): de y ≈ 3 a y ≈ 61 (FruitIconFill).
        private static void DrawFruitIcon(float[] m, int tint)
        {
            switch (tint)
            {
                case 1: // Guanábana (ver DrawGuanabanaIcon)
                    DrawGuanabanaIcon(m);
                    break;

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

                case 5: // Achiote (ver DrawAchioteIcon)
                    DrawAchioteIcon(m);
                    break;

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

                case 11: // Lulo (ver DrawLuloIcon)
                    DrawLuloIcon(m);
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

                default: // 14 · Agua del Río Negro (ver DrawSunkenLeafIcon)
                    DrawSunkenLeafIcon(m);
                    break;
            }
        }

        // Guanábana: fruto espinoso como corazón asimétrico de cuerpo liso (más ancho arriba y con la punta de abajo torcida), el
        // borde en bultos suaves de menos de 1 dp, las espinas solo como «v» huecas por dentro y un pezón corto y recto (sin el
        // rabito rizado, que parecía una antena, ni las espinas por fuera, que la hacían una mosca o una mancha erizada).
        private static void DrawGuanabanaIcon(float[] m)
        {
            const float cx = 32f;
            const float cy = 32f;
            const float tilt = -18f;
            IconBumpyHeart(m, cx, cy, 18f, 23f, tilt, 0.24f, 0.7f, 11, 1.4f, false);
            float rad = tilt * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            var center = new Vector2(cx, cy);
            for (int i = 0; i < GuanabanaVs.Length; i += 2)
            {
                Vector2 q = center + Rotate(new Vector2(GuanabanaVs[i], GuanabanaVs[i + 1]), c, s);
                Vector2 tip = q + Rotate(new Vector2(0f, -2.2f), c, s);
                Vector2 l = q + Rotate(new Vector2(-3.2f, 2.2f), c, s);
                Vector2 r = q + Rotate(new Vector2(3.2f, 2.2f), c, s);
                IconCapsule(m, l.x, l.y, tip.x, tip.y, 1.25f, true);
                IconCapsule(m, r.x, r.y, tip.x, tip.y, 1.25f, true);
            }

            Vector2 top = center + Rotate(new Vector2(-1.5f, 22.5f), c, s);
            Vector2 nub = center + Rotate(new Vector2(-2f, 27.5f), c, s);
            IconCapsule(m, top.x, top.y, nub.x, nub.y, 2.8f, false);
        }

        // Achiote: cápsula en corazón abierta en dos valvas, con semillas redondas en la abertura y pelillos cortos solo en el
        // contorno de arriba (sin la raya central de arriba abajo ni las cerdas en abanico, que la hacían una garrapata).
        private static void DrawAchioteIcon(float[] m)
        {
            // El corazón: dos lóbulos arriba y la punta abajo.
            IconCircle(m, 22f, 41f, 15f, false);
            IconCircle(m, 42f, 41f, 15f, false);
            IconTriangle(m, new Vector2(7.6f, 36f), new Vector2(56.4f, 36f), new Vector2(32f, 3f), false);
            // Abierto en dos valvas desde arriba: la abertura en V, con las semillas dentro, separadas de las valvas por su hueco.
            IconTriangle(m, new Vector2(32f, 20f), new Vector2(18f, 66f), new Vector2(46f, 66f), true);
            for (int i = 0; i < AchioteSeeds.Length; i += 3)
            {
                IconCircle(m, AchioteSeeds[i], AchioteSeeds[i + 1], AchioteSeeds[i + 2] + 2f, true);
            }

            for (int i = 0; i < AchioteSeeds.Length; i += 3)
            {
                IconCircle(m, AchioteSeeds[i], AchioteSeeds[i + 1], AchioteSeeds[i + 2], false);
            }

            // Pelillos cortos solo en el contorno de arriba de los lóbulos.
            for (int side = -1; side <= 1; side += 2)
            {
                float lobeX = 32f + (side * 10f);
                for (int k = 0; k < 6; k++)
                {
                    float deg = side < 0 ? 92f + (k * 14f) : 88f - (k * 14f);
                    float a = deg * Mathf.Deg2Rad;
                    var n = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    IconCapsule(m, lobeX + (n.x * 14f), 41f + (n.y * 14f), lobeX + (n.x * 16.8f), 41f + (n.y * 16.8f), 0.95f, false);
                }
            }
        }

        // Lulo: fruto peludo, de cuerpo redondo con el borde algo irregular (el vello) y el cáliz de 5 sépalos en estrella arriba,
        // separado del cuerpo por su hueco (sin los rayos largos, que lo hacían un virus o un erizo).
        private static void DrawLuloIcon(float[] m)
        {
            IconBumpyHeart(m, 32f, 25f, 20.5f, 20.5f, 0f, 0f, 0f, 24, 1.1f, false);
            IconStar(m, 32f, 44f, 5, 14f, 3.8f, 36f, 2.2f, true);
            IconStar(m, 32f, 44f, 5, 14f, 3.8f, 36f, 0f, false);
            IconCapsule(m, 32f, 46f, 33.5f, 59.5f, 2.3f, false);
        }

        // Agua del Río Negro: hoja hundida (revisión 2 de B3). El agua es una banda horizontal en la mitad de abajo de la caja
        // (de y = 3 al canto de una sola ola suave, con el fondo recto y las esquinas redondeadas: sin copa ni pie, para que no
        // recuerde al bol del Açaí), y la hoja la cruza en diagonal: por encima de la línea del agua, silueta maciza con el nervio
        // en negativo y el rabito; por debajo, en negativo dentro del agua, con un hueco de ≈ 2 px que la separa del agua donde
        // se cruzan. Sin el bloque lleno de antes (se leía como un escudo o una pegatina).
        private static void DrawSunkenLeafIcon(float[] m)
        {
            float k = 2f * Mathf.PI / SunkenWaterWave;
            LensCircles(SunkenLeafHalfLen, SunkenLeafHalfWidth, out float radius, out float offset);
            float rad = SunkenLeafDeg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            var axis = new Vector2(-s, c);
            var center = new Vector2(SunkenLeafX, SunkenLeafY);
            Vector2 veinFrom = center - (axis * (SunkenLeafHalfLen - 6f));
            Vector2 veinTo = center + (axis * (SunkenLeafHalfLen - 4.5f));
            Vector2 stemFrom = center + (axis * (SunkenLeafHalfLen - 2f));
            Vector2 stemTo = center + (axis * (SunkenLeafHalfLen + SunkenLeafStem));
            for (int y = 0; y < FruitIconPx; y++)
            {
                for (int x = 0; x < FruitIconPx; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float water = WaterBandDistance(px, py, SunkenWaterFromX, SunkenWaterToX, SunkenWaterTopY, SunkenWaterAmp, k, SunkenWaterPhase, SunkenWaterBottomY, SunkenWaterCorner, out float line);
                    float leaf = LensDistance(px - center.x, py - center.y, c, s, radius, offset);

                    // Arriba: la hoja maciza sobre la línea del agua, con el nervio en negativo, y el rabito.
                    float above = Mathf.Max(Mathf.Max(leaf, -line), -CapsuleDistance(px, py, veinFrom, veinTo, SunkenLeafVein));
                    float stem = CapsuleDistance(px, py, stemFrom, stemTo, 2.2f);

                    // Abajo: el agua sin la hoja (en negativo) ni su hueco.
                    float below = Mathf.Max(water, -(leaf - SunkenLeafGap));
                    IconWrite(m, x, y, Mathf.Min(Mathf.Min(above, stem), below), false);
                }
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

        // Corazón asimétrico de cuerpo liso (la Guanábana): una elipse de semiejes rx y ry girada deg grados, más ancha arriba
        // (widenTop: fracción de rx que gana en lo alto y pierde abajo), con la punta de abajo desplazada a un lado (bend, en
        // fracciones de rx) y el borde en bumps bultos suaves de bumpAmp px hacia fuera.
        private static void IconBumpyHeart(float[] m, float cx, float cy, float rx, float ry, float deg, float widenTop, float bend, int bumps, float bumpAmp, bool cut)
        {
            float rad = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            float big = Mathf.Max(rx, ry) + bumpAmp + 2f;
            IconBox(cx - big, cy - big, cx + big, cy + big, out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f) - cx;
                    float py = (y + 0.5f) - cy;
                    float lx = (px * c) + (py * s);
                    float ly = ((-px * s) + (py * c)) / ry;
                    float low = Mathf.Min(ly, 0f);
                    float wx = rx * (1f + (widenTop * Mathf.Clamp(ly, -1f, 1f)));
                    float nx = (lx - (bend * rx * low * low)) / wx;
                    float r = Mathf.Sqrt((nx * nx) + (ly * ly));
                    float bump = 0.5f - (0.5f * Mathf.Cos(bumps * Mathf.Atan2(ly, nx)));
                    IconWrite(m, x, y, ((r - 1f) * rx) - (bumpAmp * bump), cut);
                }
            }
        }

        // Estrella de points puntas (pétalos triangulares del centro a tipR, con medio ancho baseHalfW en la base) girada rotDeg
        // grados desde la vertical; grow engorda la forma (para recortar su hueco alrededor).
        private static void IconStar(float[] m, float cx, float cy, int points, float tipR, float baseHalfW, float rotDeg, float grow, bool cut)
        {
            // Los 3 bordes de cada pétalo (n · p + c), calculados una vez por pétalo y no por píxel.
            var edges = new float[points * 9];
            var center = new Vector2(cx, cy);
            for (int k = 0; k < points; k++)
            {
                float a = (90f + rotDeg + (k * 360f / points)) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var side = new Vector2(-dir.y, dir.x);
                Vector2 q0 = center + (side * baseHalfW);
                Vector2 q1 = center - (side * baseHalfW);
                Vector2 tip = center + (dir * tipR);
                float cross = ((q1.x - q0.x) * (tip.y - q0.y)) - ((q1.y - q0.y) * (tip.x - q0.x));
                if (cross < 0f)
                {
                    Vector2 t = q0;
                    q0 = q1;
                    q1 = t;
                }

                int e = k * 9;
                IconEdgeLine(q0, q1, out edges[e], out edges[e + 1], out edges[e + 2]);
                IconEdgeLine(q1, tip, out edges[e + 3], out edges[e + 4], out edges[e + 5]);
                IconEdgeLine(tip, q0, out edges[e + 6], out edges[e + 7], out edges[e + 8]);
            }

            float reach = tipR + grow + 1f;
            float hub = baseHalfW * 1.15f;
            IconBox(cx - reach, cy - reach, cx + reach, cy + reach, out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y <= y1; y++)
            {
                float py = y + 0.5f;
                for (int x = x0; x <= x1; x++)
                {
                    float px = x + 0.5f;
                    float ox = px - cx;
                    float oy = py - cy;
                    float d = Mathf.Sqrt((ox * ox) + (oy * oy)) - hub;
                    for (int k = 0; k < points; k++)
                    {
                        int e = k * 9;
                        float d0 = (edges[e] * px) + (edges[e + 1] * py) + edges[e + 2];
                        float d1 = (edges[e + 3] * px) + (edges[e + 4] * py) + edges[e + 5];
                        float d2 = (edges[e + 6] * px) + (edges[e + 7] * py) + edges[e + 8];
                        d = Mathf.Min(d, Mathf.Max(d0, Mathf.Max(d1, d2)));
                    }

                    IconWrite(m, x, y, d - grow, cut);
                }
            }
        }

        // Distancia con signo (px, negativa dentro) a una banda de agua de fromX a toX: el canto de arriba en ola (media topY,
        // amplitud amp px, número de onda k y fase phase), el de abajo recto en bottomY y las esquinas redondeadas (radio corner
        // px). En line, la distancia con signo a la línea del agua (positiva por encima).
        private static float WaterBandDistance(float px, float py, float fromX, float toX, float topY, float amp, float k, float phase, float bottomY, float corner, out float line)
        {
            float mid = 0.5f * (fromX + toX);
            float half = 0.5f * (toX - fromX);
            float slope = amp * k * Mathf.Cos((px * k) + phase);
            line = (py - (topY + (amp * Mathf.Sin((px * k) + phase)))) / Mathf.Sqrt(1f + (slope * slope));
            float dBottom = bottomY - py;
            float dSide = Mathf.Abs(px - mid) - half;
            float qa = Mathf.Max(dSide + corner, 0f);
            float qb = Mathf.Max(Mathf.Max(line, dBottom) + corner, 0f);
            return Mathf.Sqrt((qa * qa) + (qb * qb)) + Mathf.Min(Mathf.Max(dSide, Mathf.Max(line, dBottom)) + corner, 0f) - corner;
        }

        // Distancia con signo (px, negativa dentro) a la cápsula de a a b con radio r.
        private static float CapsuleDistance(float px, float py, Vector2 a, Vector2 b, float r)
        {
            float vx = b.x - a.x;
            float vy = b.y - a.y;
            float qx = px - a.x;
            float qy = py - a.y;
            float t = Mathf.Clamp01(((qx * vx) + (qy * vy)) / Mathf.Max(1e-6f, (vx * vx) + (vy * vy)));
            float dx = qx - (vx * t);
            float dy = qy - (vy * t);
            return Mathf.Sqrt((dx * dx) + (dy * dy)) - r;
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

            IconEdgeLine(p0, p1, out float ax, out float ay, out float ac);
            IconEdgeLine(p1, p2, out float bx, out float by, out float bc);
            IconEdgeLine(p2, p0, out float cx, out float cy, out float cc);
            IconBox(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x)), Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y)), Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x)), Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y)), out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y <= y1; y++)
            {
                float py = y + 0.5f;
                for (int x = x0; x <= x1; x++)
                {
                    float px = x + 0.5f;
                    float d = Mathf.Max((ax * px) + (ay * py) + ac, Mathf.Max((bx * px) + (by * py) + bc, (cx * px) + (cy * py) + cc));
                    IconWrite(m, x, y, d, cut);
                }
            }
        }

        // Recta del borde a → b como n · p + c = distancia con signo (positiva fuera, con el triángulo en sentido antihorario):
        // con la normal y la longitud calculadas una vez por borde y no por píxel.
        private static void IconEdgeLine(Vector2 a, Vector2 b, out float nx, out float ny, out float c)
        {
            float ex = b.x - a.x;
            float ey = b.y - a.y;
            float len = Mathf.Max(1e-6f, Mathf.Sqrt((ex * ex) + (ey * ey)));
            nx = ey / len;
            ny = -ex / len;
            c = -((nx * a.x) + (ny * a.y));
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
