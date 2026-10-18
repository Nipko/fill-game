#nullable enable
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Tinaja (M13; D-019; 07 §4.3 y §4.5; propuesta de la Tinaja, b.2 «Aspecto» y c.6; encargo de
    /// arte, 3.1; tarea TJ2) hasta el arte del artista: el cuerpo de barro cocido claro (<see cref="TinajaBody"/>; vasija lisa,
    /// más alta que ancha, con hombro, el labio por cuello corto y una sola línea incisa, con gotitas quietas de «sudor»), la
    /// boca (<see cref="TinajaMouthDisc"/> y su sombra, <see cref="TinajaMouthGloss"/>), las bandas de luz de las capas
    /// reveladas (<see cref="TinajaBand"/>), el icono de la fruta (<see cref="TinajaGlyph"/>), el chip «×n»
    /// (<see cref="TinajaChipPill"/>), el jirau (<see cref="TinajaJirau"/> y su atadura del medio, <see cref="TinajaJirauTie"/>), y
    /// el poste del jirau del que cuelga su cuia (<see cref="TinajaPost"/>) con el asa de fibra (<see cref="TinajaAsa"/>).
    /// </summary>
    /// <remarks>
    /// <para><b>Compuerta</b> (<see cref="MapArtRule"/>, como el arte provisional del Mapa y de TiTino): las piezas que son arte
    /// (cuerpo, jirau, poste y asa) solo salen con <see cref="MapArtRule.ShowProvisional"/> (Editor y desarrollo). Sus ranuras de
    /// PNG aún no tienen nombre en el glosario §11 (encargo de arte, sección 5, pendiente 1: «ningún PNG lleva un nombre
    /// inventado»), así que en release no se pinta ninguna hasta que lleguen. Lo que pinta el código según el estado (la boca con
    /// su tinte y su icono, las bandas y el chip) sale siempre.</para>
    /// <para><b>Color</b> (direccion-visual-v2 §2.4, regla 1; 07 §3, regla 6): el barro (<see cref="Palette.TinajaClay"/>), la
    /// madera del jirau (<see cref="JirauWood"/>, <see cref="JirauShade"/>) y la del chip (<see cref="TinajaChipWood"/>) quedan a
    /// ΔE00 ≥ 10 de los 14 tintes; nunca el pardo #8A7B6A ni el «?» de Aguas turbias, ni el terracota #B5653A (07 §4.3). Las
    /// ataduras y el asa, en tonos <c>fibra_*</c>. La boca y las bandas llevan el tinte a propósito (es lo que se ve).</para>
    /// <para><b>Vetos</b> (07 §4.3 y §8.1): sin decoración (kené, marajoara, tapajônica), sin cara, ni dos bocas, ni hoja en la
    /// boca; nunca redonda y baja, morada, con fuego, vapor ni burbujas; nada de cordón, liana ni nudo entre la tinaja y su cuia
    /// (el poste del jirau va del lado contrario a la tinaja).</para>
    /// <para>Todo perezoso: se genera la primera vez que se pide (al mostrar la tinaja o su cuia al cargar el nivel), nunca en un
    /// paso; tras <see cref="Dispose"/> se vuelve a generar.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho del sprite del cuerpo de la tinaja (u): la panza (2 × 1,28 u, dentro de la ranura de ≥ 2,86 u) con margen.</summary>
        public const float TinajaBodyWidthU = 2.70f;

        /// <summary>Alto del sprite del cuerpo de la tinaja (u): del pie (y = 0) a lo alto del labio con margen.</summary>
        public const float TinajaBodyHeightU = 6.80f;

        /// <summary>Centro del labio (la elipse de arriba de la vasija, vista un poco desde arriba; u desde el pie) [P].</summary>
        public const float TinajaLipCenterU = 6.27f;

        /// <summary>Medio ancho del labio (u) [P]: la boca entra dentro con su 0,70 del ancho de la cuia.</summary>
        public const float TinajaLipRxU = 0.86f;

        /// <summary>Medio alto del labio (u) [P]: lo alto de la vasija queda en 6,74 u.</summary>
        public const float TinajaLipRyU = 0.47f;

        /// <summary>Medio ancho del hueco oscuro de la boca, dentro del labio (u) [P].</summary>
        public const float TinajaOpeningRxU = 0.76f;

        /// <summary>Medio alto del hueco oscuro de la boca (u) [P].</summary>
        public const float TinajaOpeningRyU = 0.39f;

        /// <summary>
        /// Ancho del agua de la boca (u): 1,42 u, 0,71 del ancho de la cuia (07 §4.3: «boca de 0,70 del ancho o más»; ≈ 34 dp con
        /// la cuia de 48 dp).
        /// </summary>
        public const float TinajaMouthWidthU = 1.42f;

        /// <summary>Alto del agua de la boca (u) [P]: el icono de la fruta cabe entero dentro.</summary>
        public const float TinajaMouthHeightU = 0.72f;

        /// <summary>Centro del agua de la boca (u desde el pie): un poco más abajo que el labio, que asoma por detrás.</summary>
        public const float TinajaMouthCenterU = 6.25f;

        /// <summary>
        /// Lado de la celda del icono de la fruta en la boca (u) [P]: la silueta (0,9 de la celda) mide ≈ 14,3 dp con la cuia de
        /// 48 dp (07 §4.3: icono de 14-16 dp).
        /// </summary>
        public const float TinajaMouthGlyphU = 0.66f;

        /// <summary>Alto de una banda de luz de una capa revelada (u) [P]: algo menos que la dosis, centrada a su altura.</summary>
        public const float TinajaBandHeightU = 0.62f;

        /// <summary>Lo que la banda se queda dentro del contorno de la panza por cada lado (u) [P].</summary>
        public const float TinajaBandInsetU = 0.14f;

        /// <summary>Lado de la celda del icono de la fruta en una banda (u) [P].</summary>
        public const float TinajaBandGlyphU = 0.56f;

        /// <summary>Altura de la única línea incisa (u desde el pie) [P]: sobre la banda más alta posible (la capa 3, hasta 4,41 u).</summary>
        public const float TinajaIncisedLineU = 4.78f;

        /// <summary>Alto del chip «×n» (dp) [P]: los glifos de la insignia a escala <see cref="TinajaChipGlyphScale"/>.</summary>
        public const float TinajaChipHeightDp = 18f;

        /// <summary>
        /// Escala de los glifos marfil del chip sobre los de la insignia de Aguas turbias (<see cref="MurkyBadgeCellDp"/> ×
        /// <see cref="MurkyBadgeDp"/>) [P]: la cifra mide ≈ 10,2 dp de alto, la de un texto de 14 sp (07 §4.3).
        /// </summary>
        public const float TinajaChipGlyphScale = 1.1f;

        /// <summary>Ancho del chip «×n» (dp) [P]: dos celdas de glifo y su margen.</summary>
        public const float TinajaChipWidthDp = (2f * MurkyBadgeCellDp * TinajaChipGlyphScale) + 6f;

        /// <summary>Alto de diseño del jirau (dp, a <see cref="UiPxPerDp"/>); el overlay lo escala al alto en u con el multiplicador del 9-slice.</summary>
        public const float TinajaJirauDesignDp = 14f;

        /// <summary>Borde de 9 cortes del jirau a cada lado (dp de diseño): las ataduras de las puntas.</summary>
        public const float TinajaJirauBorderDp = 5f;

        /// <summary>Poste del jirau: distancia de su eje al eje de la cuia que cuelga (u) [P], dentro de la ranura (≥ 1,43 u).</summary>
        public const float TinajaPostXU = 1.19f;

        /// <summary>Altura del travesaño del poste, del que cuelga el asa (u desde el pie) [P].</summary>
        public const float TinajaPostArmU = 4.48f;

        /// <summary>Punto del gancho del que cuelga el asa (u desde el pie): bajo el travesaño, en el eje de la cuia.</summary>
        public const float TinajaHookU = 4.30f;

        /// <summary>Caja del sprite del poste en u, desde el eje de la cuia (a la derecha; a la izquierda se refleja).</summary>
        public static readonly Rect TinajaPostRectU = new Rect(-0.20f, -0.20f, 1.55f, 4.92f);

        /// <summary>Caja del sprite del asa de fibra en u, desde el eje y el pie de una Cuia chica de 2 (alto 3,60 u).</summary>
        public static readonly Rect TinajaAsaRectU = new Rect(-0.52f, 3.30f, 1.04f, 1.12f);

        /// <summary>
        /// Madera del chip «×n», <see cref="Palette.Frame"/> #7C4E3A (07 §4.3: «marfil sobre madera»): con el marfil
        /// <see cref="Palette.UiPaper"/> de los glifos da ≈ 6,6:1 (≥ 5:1) y queda a ΔE00 ≥ 10 de los 14 tintes.
        /// </summary>
        public static readonly Color TinajaChipWood = Palette.Frame;

        /// <summary>Varas del jirau, madera clara #B09468 [P] (ΔE00 ≥ 16 frente a los 14 tintes).</summary>
        public static readonly Color JirauWood = Palette.Hex(0xB09468);

        /// <summary>Sombra de las varas y madera del poste en la sombra, #7A5A3C [P] (ΔE00 ≥ 12 frente a los 14 tintes).</summary>
        public static readonly Color JirauShade = Palette.Hex(0x7A5A3C);

        // Contorno de 1-2 dp del barro y fondo del hueco de la boca [P] (líneas finas y lo que queda bajo el agua de la boca).
        private static readonly Color TinajaClayLine = Palette.Hex(0x8E5E43);
        private static readonly Color TinajaOpeningDark = Palette.Hex(0x3E2A1E);
        private static readonly Color TinajaOpeningFront = Palette.Hex(0x6B4A35);

        // Perfil de la panza: alturas (u desde el pie) y medio ancho (u) en cada una; entre dos puntos, smoothstep (monótono:
        // sin rebotes). Cubre el vidrio de la Cuia alta de 5 entero (CuiaGeometry.OuterHalfWidth): la tinaja es opaca.
        private static readonly float[] TinajaProfileY = { 0.00f, 0.15f, 0.60f, 1.60f, 2.60f, 3.60f, 4.50f, 5.20f, 5.50f, 5.65f, 5.85f, 6.40f };
        private static readonly float[] TinajaProfileHalf = { 0.86f, 0.95f, 1.10f, 1.24f, 1.28f, 1.24f, 1.14f, 1.06f, 1.035f, 1.02f, 0.88f, 0.86f };

        // Gotitas de «sudor» quietas (u desde el eje y el pie) y su radio (u) [P].
        private static readonly Vector3[] TinajaSweat =
        {
            new Vector3(-0.58f, 1.52f, 0.055f),
            new Vector3(0.46f, 2.44f, 0.045f),
            new Vector3(-0.22f, 3.52f, 0.040f),
            new Vector3(0.74f, 3.86f, 0.050f),
            new Vector3(-0.80f, 2.86f, 0.042f),
            new Vector3(0.22f, 0.78f, 0.048f),
        };

        private Sprite? _tinajaBody;
        private Sprite? _tinajaMouthDisc;
        private Sprite? _tinajaMouthGloss;
        private Sprite? _tinajaBand;
        private Sprite? _tinajaChipPill;
        private Sprite? _tinajaJirau;
        private Sprite? _tinajaJirauTie;
        private Sprite? _tinajaPost;
        private Sprite? _tinajaAsa;
        private readonly Sprite?[] _tinajaGlyphs = new Sprite?[Cuia.MaxTintColor + 1];

        /// <summary>Cuerpo de la tinaja (<see cref="TinajaBodyWidthU"/> × <see cref="TinajaBodyHeightU"/> u, pivote al centro).</summary>
        internal Sprite TinajaBody => _tinajaBody != null ? _tinajaBody : (_tinajaBody = BuildTinajaBody());

        /// <summary>Elipse blanca del agua de la boca: la tiñe el overlay con la fruta de arriba.</summary>
        internal Sprite TinajaMouthDisc => _tinajaMouthDisc != null ? _tinajaMouthDisc : (_tinajaMouthDisc = BuildTinajaMouthDisc());

        /// <summary>Sombra del labio sobre el agua de la boca (arriba) y un brillo (abajo a la izquierda), encima del agua.</summary>
        internal Sprite TinajaMouthGloss => _tinajaMouthGloss != null ? _tinajaMouthGloss : (_tinajaMouthGloss = BuildTinajaMouthGloss());

        /// <summary>Banda de luz blanca de bordes suaves: la tiñe el overlay con la fruta de la capa revelada.</summary>
        internal Sprite TinajaBand => _tinajaBand != null ? _tinajaBand : (_tinajaBand = BuildTinajaBand());

        /// <summary>Píldora de madera del chip «×n» (9 cortes en horizontal, <see cref="TinajaChipHeightDp"/> de alto).</summary>
        internal Sprite TinajaChipPill => _tinajaChipPill != null ? _tinajaChipPill : (_tinajaChipPill = BuildTinajaChipPill());

        /// <summary>Jirau: tres varas atadas en las puntas (9 cortes en horizontal; se estira bajo las dos ranuras).</summary>
        internal Sprite TinajaJirau => _tinajaJirau != null ? _tinajaJirau : (_tinajaJirau = BuildTinajaJirau());

        /// <summary>Atadura de fibra del medio del jirau (entre las dos ranuras).</summary>
        internal Sprite TinajaJirauTie => _tinajaJirauTie != null ? _tinajaJirauTie : (_tinajaJirauTie = BuildTinajaJirauTie());

        /// <summary>
        /// Poste del jirau con su travesaño y el gancho de fibra, a la derecha del eje de la cuia (<see cref="TinajaPostRectU"/>;
        /// pivote en el eje y el pie).
        /// </summary>
        internal Sprite TinajaPost => _tinajaPost != null ? _tinajaPost : (_tinajaPost = BuildTinajaPost());

        /// <summary>Asa de fibra de su cuia (<see cref="TinajaAsaRectU"/>, pivote al centro): de los dos agujeritos del labio al gancho.</summary>
        internal Sprite TinajaAsa => _tinajaAsa != null ? _tinajaAsa : (_tinajaAsa = BuildTinajaAsa());

        /// <summary>
        /// Medio ancho del contorno de la tinaja a la altura <paramref name="yU"/> (u desde el pie), sin el labio; 0 fuera. Cubre
        /// el vidrio de la Cuia alta de 5 a cualquier altura (<c>TinajaVisualTests</c>).
        /// </summary>
        public static float TinajaHalfWidthU(float yU)
        {
            float[] ys = TinajaProfileY;
            float[] hs = TinajaProfileHalf;
            if (yU < 0f || yU > ys[ys.Length - 1])
            {
                return 0f;
            }

            for (int i = 1; i < ys.Length; i++)
            {
                if (yU <= ys[i])
                {
                    float t = (yU - ys[i - 1]) / (ys[i] - ys[i - 1]);
                    return Mathf.Lerp(hs[i - 1], hs[i], Mathf.SmoothStep(0f, 1f, t));
                }
            }

            return hs[hs.Length - 1];
        }

        /// <summary>
        /// Medio ancho de la silueta entera a la altura <paramref name="yU"/> (u desde el pie): la panza y, encima, la elipse
        /// del labio (lo alto de la vasija, a <see cref="TinajaLipCenterU"/> + <see cref="TinajaLipRyU"/>).
        /// </summary>
        public static float TinajaSilhouetteHalfWidthU(float yU)
        {
            float body = yU <= TinajaLipCenterU ? TinajaHalfWidthU(yU) : 0f;
            float ey = (yU - TinajaLipCenterU) / TinajaLipRyU;
            float lip = ey * ey < 1f ? TinajaLipRxU * Mathf.Sqrt(1f - (ey * ey)) : 0f;
            return Mathf.Max(body, lip);
        }

        /// <summary>
        /// Icono de la fruta del tinte <paramref name="tint"/> (1-14) para la boca y las bandas de la tinaja: la silueta de los
        /// Patrones de fruta (<see cref="FruitIconMask"/>, celda de <see cref="FruitIconPx"/> px) en el color con que se pinta
        /// sobre su dosis (<see cref="Palette.PatternBase"/> al <see cref="LiquidGraphic.FruitIconOpacity"/>) y su contorno si lo
        /// lleva, sobre transparente. Perezoso y en caché por tinte: el overlay lo pide al cargar el nivel para las frutas de la
        /// tinaja, nunca en un paso.
        /// </summary>
        public Sprite TinajaGlyph(int tint)
        {
            int t = Mathf.Clamp(tint, Cuia.MinColor, Cuia.MaxTintColor);
            Sprite? cached = _tinajaGlyphs[t];
            if (cached != null)
            {
                return cached;
            }

            Sprite sprite = BuildTinajaGlyph(t);
            _tinajaGlyphs[t] = sprite;
            return sprite;
        }

        /// <summary>Ya está generado el icono de la tinaja del tinte <paramref name="tint"/> (tests: la carga lo prepara).</summary>
        public bool HasTinajaGlyph(int tint)
        {
            return tint >= Cuia.MinColor && tint <= Cuia.MaxTintColor && _tinajaGlyphs[tint] != null;
        }

        // Cuerpo: la panza (perfil de TinajaHalfWidthU) con sombreado cilíndrico (luz arriba a la izquierda, D-003), su contorno,
        // la línea incisa y el sudor; encima, el labio (elipse clara) con el hueco oscuro de la boca. El agua de la boca la
        // pinta el overlay con su tinte.
        private Sprite BuildTinajaBody()
        {
            const float px = MechanicPxPerU;
            int w = Mathf.RoundToInt(TinajaBodyWidthU * px);
            int h = Mathf.RoundToInt(TinajaBodyHeightU * px);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            const float line = 1.6f; // contorno, en px (≈ 0,4 dp)
            for (int y = 0; y < h; y++)
            {
                float yu = (y + 0.5f) / px;
                float half = yu <= TinajaLipCenterU ? TinajaHalfWidthU(yu) : 0f;
                if (half <= 0f)
                {
                    continue;
                }

                for (int x = 0; x < w; x++)
                {
                    float xu = ((x + 0.5f) - cx) / px;
                    float dist = (Mathf.Abs(xu) - half) * px; // negativa dentro
                    float cover = PixelCanvas.Coverage(dist);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    float nx = Mathf.Clamp(xu / half, -1f, 1f);
                    Color col = Color.Lerp(Palette.TinajaClayLight, Palette.TinajaClayShade, Mathf.SmoothStep(0f, 1f, (nx + 0.55f) / 1.5f));
                    col = Color.Lerp(col, Palette.TinajaClay, 0.35f);
                    float edge = nx * nx * nx * nx;
                    col = ColorMath.Darken(col, (0.16f * edge) + (yu < 0.45f ? 0.10f * (1f - (yu / 0.45f)) : 0f));
                    float gloss = Mathf.Exp(-((nx + 0.52f) * (nx + 0.52f)) / 0.012f);
                    col = Color.Lerp(col, Color.white, 0.16f * gloss * Mathf.Clamp01((yu - 0.6f) / 1.2f));

                    // La única línea incisa: un surco oscuro con su filo claro debajo.
                    float dy = (yu - TinajaIncisedLineU) * px;
                    if (Mathf.Abs(dy) < 1.2f)
                    {
                        col = Color.Lerp(col, TinajaClayLine, 0.55f * (1f - (Mathf.Abs(dy) / 1.2f)));
                    }
                    else if (dy < -1.2f && dy > -2.8f)
                    {
                        col = ColorMath.Lighten(col, 0.10f);
                    }

                    col = Color.Lerp(col, TinajaClayLine, Mathf.Clamp01(1f - ((-dist) / line)));
                    col.a = 1f;
                    pc.Blend(x, y, col, cover);
                }
            }

            // Sudor: gotitas quietas que pillan la luz (sombra abajo, cuerpo claro y un punto de brillo).
            for (int i = 0; i < TinajaSweat.Length; i++)
            {
                Vector3 s = TinajaSweat[i];
                float sx = cx + (s.x * px);
                float sy = s.y * px;
                float r = s.z * px;
                pc.FillCircle(sx + (0.25f * r), sy - (0.45f * r), r, ColorMath.WithAlpha(TinajaClayLine, 0.45f));
                pc.FillCircle(sx, sy, r, ColorMath.WithAlpha(Color.Lerp(Palette.TinajaClayLight, Color.white, 0.55f), 0.85f));
                pc.FillCircle(sx - (0.3f * r), sy + (0.3f * r), 0.38f * r, ColorMath.WithAlpha(Color.white, 0.95f));
            }

            // Labio: canto oscuro, cara de arriba clara y, por dentro, el hueco oscuro (más claro hacia delante).
            float lx = cx;
            float ly = TinajaLipCenterU * px;
            pc.FillEllipseSoft(lx, ly - 1f, (TinajaLipRxU * px) + 0.6f, (TinajaLipRyU * px) + 0.6f, 1f, TinajaClayLine);
            pc.FillEllipseSoft(lx, ly, TinajaLipRxU * px, TinajaLipRyU * px, 1f, Color.Lerp(Palette.TinajaClayLight, Color.white, 0.10f));
            pc.FillEllipseSoft(lx, ly + (0.02f * px), TinajaOpeningRxU * px, TinajaOpeningRyU * px, 1f, TinajaOpeningFront);
            pc.FillEllipseSoft(lx, ly + (0.10f * px), TinajaOpeningRxU * px * 0.92f, TinajaOpeningRyU * px * 0.70f, 6f, TinajaOpeningDark);
            return MakeSprite(pc, "mech_tinaja_body", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Elipse blanca del agua de la boca, de borde suave (el tinte lo pone Image.color).
        private Sprite BuildTinajaMouthDisc()
        {
            const float px = MechanicPxPerU;
            int w = Mathf.RoundToInt(TinajaMouthWidthU * px) + 4;
            int h = Mathf.RoundToInt(TinajaMouthHeightU * px) + 4;
            var pc = new PixelCanvas(w, h);
            pc.FillEllipseSoft(w * 0.5f, h * 0.5f, (w * 0.5f) - 2f, (h * 0.5f) - 2f, 1f, Color.white);
            return MakeSprite(pc, "mech_tinaja_mouth", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Sobre el agua de la boca: la sombra del labio en el canto de atrás (arriba) y un brillo en el de delante (abajo).
        private Sprite BuildTinajaMouthGloss()
        {
            const float px = MechanicPxPerU;
            int w = Mathf.RoundToInt(TinajaMouthWidthU * px) + 4;
            int h = Mathf.RoundToInt(TinajaMouthHeightU * px) + 4;
            var pc = new PixelCanvas(w, h);
            float rx = (w * 0.5f) - 2f;
            float ry = (h * 0.5f) - 2f;
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float ex = ((x + 0.5f) - cx) / rx;
                    float ey = ((y + 0.5f) - cy) / ry;
                    float rr = (ex * ex) + (ey * ey);
                    if (rr >= 1f)
                    {
                        continue;
                    }

                    // Sombra del labio: una media luna arriba, que se va hacia el centro.
                    float shade = Mathf.Clamp01((ey - 0.15f) / 0.85f) * Mathf.Clamp01((rr - 0.35f) / 0.65f);
                    if (shade > 0f)
                    {
                        pc.Blend(x, y, new Color(0.16f, 0.10f, 0.06f, 1f), 0.42f * shade);
                    }
                }
            }

            pc.FillEllipseSoft(cx - (0.30f * rx), cy - (0.52f * ry), 0.28f * rx, 0.12f * ry, 2f, ColorMath.WithAlpha(Color.white, 0.40f));
            return MakeSprite(pc, "mech_tinaja_mouth_gloss", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Banda de luz: blanca, más intensa en el centro y de bordes suaves (la luz que pasa por el barro).
        private Sprite BuildTinajaBand()
        {
            const int w = 192;
            const int h = 60;
            var pc = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                float vy = Mathf.Abs(((y + 0.5f) / h) - 0.5f) * 2f;
                for (int x = 0; x < w; x++)
                {
                    float vx = Mathf.Abs(((x + 0.5f) / w) - 0.5f) * 2f;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - vy) / 0.30f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - vx) / 0.16f));
                    if (a > 0f)
                    {
                        pc.Set(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
            }

            return MakeSprite(pc, "mech_tinaja_band", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Icono de la fruta sobre transparente: el contorno (si lo lleva) y la silueta en el color de los Patrones de fruta.
        private Sprite BuildTinajaGlyph(int tint)
        {
            const int cell = FruitIconPx;
            float[] mask = FruitIconMask(tint);
            float[] rim = FruitIconRimMask(tint);
            Color dose = Palette.Tint(tint);
            Color glyph = Color.Lerp(dose, Palette.PatternBase(tint), LiquidGraphic.FruitIconOpacity);
            Color rimColor = ColorMath.Darken(dose, LiquidGraphic.FruitIconRimDarken);
            glyph.a = 1f;
            rimColor.a = 1f;
            var pc = new PixelCanvas(cell, cell);
            for (int y = 0; y < cell; y++)
            {
                for (int x = 0; x < cell; x++)
                {
                    int i = (y * cell) + x;
                    pc.Blend(x, y, rimColor, rim[i]);
                    pc.Blend(x, y, glyph, mask[i]);
                }
            }

            return MakeSprite(pc, "mech_tinaja_glyph_" + TintIds[tint], new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Píldora de madera del chip: canto oscuro de 1 dp, madera y un brillo arriba (la luz de juego, D-003).
        private Sprite BuildTinajaChipPill()
        {
            const float k = UiPxPerDp;
            int h = Mathf.RoundToInt(TinajaChipHeightDp * k);
            int w = h + 24;
            float r = h * 0.5f;
            var pc = new PixelCanvas(w, h);
            pc.FillRoundedRect(0f, 0f, w, h, r, Palette.WoodDark);
            pc.FillRoundedRect(k, k, w - k, h - k, r - k, TinajaChipWood);
            pc.FillRoundedRectSoft(2.5f * k, h * 0.58f, w - (2.5f * k), h - (2f * k), (h * 0.2f) - 1f, 2.5f, ColorMath.WithAlpha(Color.white, 0.14f));
            return MakeSprite(pc, "mech_tinaja_chip", new Vector2(0.5f, 0.5f), new Vector4(r + 1f, 0f, r + 1f, 0f), UiPxPerDp);
        }

        // Jirau: tres varas horizontales (sombra abajo, luz arriba y algo de veta) con una atadura de fibra en cada punta, en el
        // borde de 9 cortes; el centro se estira sin deformar las ataduras.
        private Sprite BuildTinajaJirau()
        {
            const float k = UiPxPerDp;
            int h = Mathf.RoundToInt(TinajaJirauDesignDp * k);
            int border = Mathf.RoundToInt(TinajaJirauBorderDp * k);
            int w = (2 * border) + 48;
            var pc = new PixelCanvas(w, h);
            float rp = h / 6f;
            for (int i = 0; i < 3; i++)
            {
                float cy = rp + (i * 2f * rp);
                pc.FillCapsule(rp + 0.5f, cy - 0.8f, w - rp - 0.5f, cy - 0.8f, rp, JirauShade);
                pc.FillCapsule(rp + 0.5f, cy, w - rp - 0.5f, cy, rp - 1.1f, JirauWood);
                pc.FillCapsule(rp + 2f, cy + (0.38f * rp), w - rp - 2f, cy + (0.38f * rp), 0.22f * rp, ColorMath.Lighten(JirauWood, 0.30f));
                for (int g = 0; g < 4; g++)
                {
                    float gx = border + 6f + (g * 9f) + (i * 3f);
                    pc.FillCapsule(gx, cy - (0.25f * rp), gx + 5f, cy - (0.25f * rp), 0.35f, ColorMath.WithAlpha(JirauShade, 0.7f));
                }
            }

            DrawJirauLashing(pc, border * 0.55f, h);
            DrawJirauLashing(pc, w - (border * 0.55f), h);
            return MakeSprite(pc, "mech_tinaja_jirau", new Vector2(0.5f, 0.5f), new Vector4(border, 0f, border, 0f), UiPxPerDp);
        }

        private Sprite BuildTinajaJirauTie()
        {
            const float k = UiPxPerDp;
            int h = Mathf.RoundToInt(TinajaJirauDesignDp * k);
            int w = Mathf.RoundToInt(6f * k);
            var pc = new PixelCanvas(w, h);
            DrawJirauLashing(pc, w * 0.5f, h);
            return MakeSprite(pc, "mech_tinaja_jirau_tie", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        // Atadura de fibra: vueltas diagonales que abrazan las tres varas en x = cx.
        private static void DrawJirauLashing(PixelCanvas pc, float cx, int h)
        {
            float half = 1.7f * UiPxPerDp;
            for (float y = 1.5f; y < h - 1.5f; y += 3.2f)
            {
                pc.FillCapsule(cx - half, y - 0.8f, cx + half, y + 0.8f, 1.0f, Palette.FiberShadow);
                pc.FillCapsule(cx - half, y - 0.4f, cx + half, y + 1.2f, 0.65f, Palette.FiberBase);
                pc.FillCapsule(cx - (0.4f * half), y + 0.5f, cx + (0.3f * half), y + 0.9f, 0.3f, Palette.FiberLight);
            }
        }

        // Poste del jirau a la derecha del eje (se refleja para la izquierda): la vara vertical desde el jirau, el travesaño
        // hasta el eje y, bajo él, el gancho de fibra del que cuelga el asa. Nada une la tinaja con su cuia.
        private Sprite BuildTinajaPost()
        {
            const float px = MechanicPxPerU;
            Rect box = TinajaPostRectU;
            int w = Mathf.RoundToInt(box.width * px);
            int h = Mathf.RoundToInt(box.height * px);
            var pc = new PixelCanvas(w, h);
            float ox = -box.xMin * px;
            float oy = -box.yMin * px;
            float postX = ox + (TinajaPostXU * px);
            float postR = 0.075f * px;
            float armY = oy + (TinajaPostArmU * px);
            float armR = 0.065f * px;

            // Vara vertical: sombra, madera y luz a la izquierda.
            pc.FillCapsule(postX + 1.2f, oy + 2f, postX + 1.2f, armY + (0.06f * px), postR, JirauShade);
            pc.FillCapsule(postX, oy + 2f, postX, armY + (0.06f * px), postR - 1.2f, JirauWood);
            pc.FillCapsule(postX - (0.35f * postR), oy + 6f, postX - (0.35f * postR), armY, 0.22f * postR, ColorMath.Lighten(JirauWood, 0.30f));

            // Travesaño hasta el eje de la cuia, atado al poste.
            float armX0 = ox - (0.08f * px);
            pc.FillCapsule(armX0, armY - 1f, postX, armY - 1f, armR, JirauShade);
            pc.FillCapsule(armX0, armY, postX, armY, armR - 1.1f, JirauWood);
            pc.FillCapsule(armX0 + 3f, armY + (0.35f * armR), postX - 3f, armY + (0.35f * armR), 0.22f * armR, ColorMath.Lighten(JirauWood, 0.30f));
            for (int i = 0; i < 3; i++)
            {
                float t = postX - (0.10f * px) + (i * 0.05f * px);
                pc.FillCapsule(t - 2f, armY - armR - 1f, t + 2f, armY + armR + 1f, 1.1f, Palette.FiberBase);
            }

            // Gancho de fibra bajo el travesaño, en el eje: una lazada corta.
            float hookX = ox;
            float hookY = oy + (TinajaHookU * px);
            pc.FillCapsule(hookX, armY - armR, hookX, hookY, 0.035f * px, Palette.FiberShadow);
            pc.FillCapsule(hookX - 0.6f, armY - armR, hookX - 0.6f, hookY, 0.022f * px, Palette.FiberBase);
            pc.StrokeArc(hookX, hookY - (0.05f * px), 0.055f * px, 0.035f * px, 200f, 520f, Palette.FiberBase);
            return MakeSprite(pc, "mech_tinaja_post", new Vector2(ox / w, oy / h), Vector4.zero);
        }

        // Asa de fibra de su cuia: dos cabos que salen de los agujeritos del labio (±0,40 u, 0,12 u bajo lo alto de la Cuia
        // chica de 2) y se juntan arriba, en la lazada que cuelga del gancho.
        private Sprite BuildTinajaAsa()
        {
            const float px = MechanicPxPerU;
            Rect box = TinajaAsaRectU;
            int w = Mathf.RoundToInt(box.width * px);
            int h = Mathf.RoundToInt(box.height * px);
            var pc = new PixelCanvas(w, h);
            float ox = -box.xMin * px;
            float oy = -box.yMin * px;
            float holeY = oy + ((CuiaGeometry.HeightU(2) - 0.12f) * px);
            float topY = oy + ((TinajaHookU - 0.06f) * px);
            float r = 0.034f * px;
            for (int side = -1; side <= 1; side += 2)
            {
                float hx = ox + (side * 0.40f * px);
                float mx = ox + (side * 0.16f * px);
                float my = topY - (0.30f * px);
                pc.FillCapsule(hx, holeY, mx, my, r + 0.8f, Palette.FiberShadow);
                pc.FillCapsule(mx, my, ox, topY, r + 0.8f, Palette.FiberShadow);
                pc.FillCapsule(hx, holeY, mx, my, r, Palette.FiberBase);
                pc.FillCapsule(mx, my, ox, topY, r, Palette.FiberBase);
                pc.FillCircle(hx, holeY, 1.6f * r, Palette.FiberShadow); // el nudito que asoma por el agujerito
            }

            pc.StrokeArc(ox, topY + (0.03f * px), 0.05f * px, 0.03f * px, -20f, 200f, Palette.FiberLight);
            return MakeSprite(pc, "mech_tinaja_asa", new Vector2(0.5f, 0.5f), Vector4.zero);
        }
    }
}
