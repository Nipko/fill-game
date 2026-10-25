#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Piezas provisionales pequeñas del Mapa del río (D-013; propuesta del Mapa §6.1, §7 y §12, piezas 7 y 9; tarea M1.5), en
    /// la familia de D-003 (vidrio, madera y fibra):
    /// <list type="bullet">
    /// <item><see cref="MapMiniCuia"/>: la Muestra del río, una <b>cuia de vidrio en miniatura</b> de
    /// <see cref="MiniCuiaWidthDp"/> × <see cref="MiniCuiaHeightDp"/> dp con la silueta de D-006 (<see cref="CuiaGeometry"/>,
    /// capacidad 2: pie y borde de fibra, hombro y cuello corto; nunca un tubo). El agua la pinta el código: la máscara blanca
    /// <see cref="MapMiniCuiaWater"/> va detrás del vidrio y se tiñe con <c>Image.color</c> del agua del sitio.</item>
    /// <item><see cref="MapCanoe"/>: la canoa del Mapa con Capi remando, cargada de cuias atadas con cuerda (sin cestas, que
    /// serían la Canasta; sin personas), de <see cref="MapCanoeWidthDp"/> × <see cref="MapCanoeHeightDp"/> dp. No es
    /// <see cref="IconCanoe"/> (la vida de la economía, 44 × 26 dp, sin nadie dentro): otra pieza, otro tamaño y con Capi.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>Las ranuras mandan: <c>map_minicuia</c> (96 × 160, el vidrio) y <c>map_canoe</c> (el bucle de Capi remando). Con su PNG
    /// no se crea el provisional. Ninguna de las dos depende de <see cref="MapArtRule"/>: Capi ya sale por
    /// código en el banner de mundo, y la mini cuia es la cuia del juego en pequeño.</para>
    /// <para>Perezosas: nada se crea al construir <see cref="ProceduralArt"/>, solo la primera vez que se piden
    /// (<see cref="MapIconsBuilt"/>). Tras <see cref="Dispose"/> se vuelven a generar (el == de Unity).</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ranura del vidrio de la mini cuia (glosario §11).</summary>
        public const string MiniCuiaSlot = "map_minicuia";

        /// <summary>Ranura de la canoa con Capi remando (glosario §11).</summary>
        public const string MapCanoeSlot = "map_canoe";

        /// <summary>Ancho de la caja de la mini cuia (dp).</summary>
        public const float MiniCuiaWidthDp = 20f;

        /// <summary>Alto de la caja de la mini cuia (dp).</summary>
        public const float MiniCuiaHeightDp = 32f;

        /// <summary>Capacidad de la silueta de la mini cuia (D-006): la de la cuia chica, para que se lea como una muestra.</summary>
        public const int MiniCuiaCapacity = 2;

        /// <summary>Ancho de la canoa de Capi (dp): el tope de los 32-40 dp de la propuesta (§12, pieza 7).</summary>
        public const float MapCanoeWidthDp = 40f;

        /// <summary>Alto de la canoa de Capi (dp), con Capi sentado dentro.</summary>
        public const float MapCanoeHeightDp = 32f;

        // Un u de la mini cuia (dp): la silueta de capacidad 2 (3,60 u de alto) ocupa el alto de la caja menos 1 dp de contorno.
        private const float MiniCuiaUnitDp = (MiniCuiaHeightDp - 1f) / (CuiaGeometry.CavityBottomU + MiniCuiaCapacity + CuiaGeometry.AboveCavityU);

        private Sprite? _miniCuia;
        private Sprite? _miniCuiaWater;
        private Sprite? _mapCanoe;

        /// <summary>Si ya se creó alguna de las piezas de este archivo.</summary>
        public bool MapIconsBuilt => _miniCuia != null || _miniCuiaWater != null || _mapCanoe != null;

        /// <summary>El vidrio de la mini cuia: el PNG de <see cref="MiniCuiaSlot"/> si existe; si no, el provisional.</summary>
        public Sprite MapMiniCuia => MapMiniCuiaFor(ArtSlots.Find(MiniCuiaSlot));

        /// <summary>
        /// Máscara blanca del agua de la mini cuia (la cavidad de D-006 con su fondo en arco y la cara superior), en la misma
        /// caja que el vidrio: va detrás de él y se tiñe con el agua del sitio. Siempre por código («el líquido lo pinta el
        /// código», propuesta §12, pieza 9).
        /// </summary>
        public Sprite MapMiniCuiaWater => _miniCuiaWater != null ? _miniCuiaWater : (_miniCuiaWater = BuildMiniCuia(true));

        /// <summary>La canoa con Capi remando: el PNG de <see cref="MapCanoeSlot"/> si existe; si no, la provisional.</summary>
        public Sprite MapCanoe => MapCanoeFor(ArtSlots.Find(MapCanoeSlot));

        /// <summary>El vidrio de la mini cuia con el PNG de su ranura explícito (<paramref name="slotPng"/>): si existe, manda.</summary>
        public Sprite MapMiniCuiaFor(Sprite? slotPng)
        {
            if (slotPng != null)
            {
                return slotPng;
            }

            return _miniCuia != null ? _miniCuia : (_miniCuia = BuildMiniCuia(false));
        }

        /// <summary>La canoa de Capi con el PNG de su ranura explícito (<paramref name="slotPng"/>): si existe, manda.</summary>
        public Sprite MapCanoeFor(Sprite? slotPng)
        {
            if (slotPng != null)
            {
                return slotPng;
            }

            return _mapCanoe != null ? _mapCanoe : (_mapCanoe = BuildMapCanoe());
        }

        // La mini cuia en dp (origen abajo a la izquierda, eje en x = 10): el contorno, el vidrio translúcido con su brillo a la
        // izquierda (luz de juego), la cavidad casi transparente para que se vea el agua de detrás, y el pie y el borde de fibra.
        // Con water, solo la máscara blanca de la cavidad hasta el nivel lleno.
        private Sprite BuildMiniCuia(bool water)
        {
            const float k = UiPxPerDp;
            const float u = MiniCuiaUnitDp;
            const float axis = MiniCuiaWidthDp * 0.5f;
            const float baseY = 0.5f;
            const int cap = MiniCuiaCapacity;
            int w = Mathf.RoundToInt(MiniCuiaWidthDp * k);
            int h = Mathf.RoundToInt(MiniCuiaHeightDp * k);
            var pc = new PixelCanvas(w, h);
            float height = CuiaGeometry.HeightU(cap);
            float cavityTop = CuiaGeometry.CavityTopU(cap);
            float outline = 0.9f; // dp
            Color ink = new Color(Palette.Groove.r, Palette.Groove.g, Palette.Groove.b, 0.75f);
            Color glass = new Color(Palette.GlassRim.r, Palette.GlassRim.g, Palette.GlassRim.b, 0.42f);
            Color cavityGlass = new Color(1f, 1f, 1f, 0.10f);
            for (int y = 0; y < h; y++)
            {
                float yd = (y + 0.5f) / k;
                float yu = (yd - baseY) / u;
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float xu = (xd - axis) / u;
                    if (water)
                    {
                        // Cavidad recta de 1,60 u con el fondo en arco, llena hasta su techo (la cara superior la pinta el
                        // propio borde del vidrio: la máscara llega al techo de la zona de dosis).
                        float floor = CuiaGeometry.CavityFloorU(xu);
                        float dSide = (Mathf.Abs(xu) - CuiaGeometry.CavityHalfWidthU) * u;
                        float dw = Mathf.Max(dSide, Mathf.Max((floor - yu) * u, (yu - cavityTop) * u));
                        pc.Blend(x, y, Color.white, PixelCanvas.Coverage(dw * k));
                        continue;
                    }

                    float half = CuiaGeometry.OuterHalfWidth(Mathf.Clamp(yu, 0f, height), cap);
                    float d = Mathf.Max((Mathf.Abs(xu) - half) * u, Mathf.Max(-yu * u, (yu - height) * u)); // dp, negativa dentro
                    if (d > outline + 1f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, ink, PixelCanvas.Coverage((d - outline) * k));
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Color c;
                    if (yu < CuiaGeometry.FootTopU || yu > CuiaGeometry.LipU(cap))
                    {
                        // Pie y borde de fibra.
                        c = Color.Lerp(Palette.FiberShadow, Palette.FiberLight, Mathf.Clamp01(0.55f - (0.45f * xu)));
                    }
                    else
                    {
                        float inner = CuiaGeometry.InnerHalfWidth(yu, cap);
                        bool cavity = inner > 0f && Mathf.Abs(xu) < inner - 0.08f && yu > CuiaGeometry.CavityFloorU(xu);
                        c = cavity ? cavityGlass : glass;

                        // Brillo vertical a la izquierda y un filo claro a la derecha (vidrio, guía 4.1).
                        float streak = Mathf.Abs(xu + 0.55f) - 0.13f;
                        if (streak < 0f && yu > CuiaGeometry.CavityBottomU && yu < cavityTop + 0.2f)
                        {
                            c = Color.Lerp(c, new Color(1f, 1f, 1f, 0.85f), 0.8f);
                        }
                        else if (Mathf.Abs(xu) > half - 0.12f)
                        {
                            c = new Color(1f, 1f, 1f, 0.7f);
                        }
                    }

                    pc.Blend(x, y, c, cov);
                }
            }

            return MakeSprite(pc, water ? "map_minicuia_water" : "map_minicuia_glass", new Vector2(0.5f, 0f), Vector4.zero, k);
        }

        // La canoa de Capi en dp (origen abajo a la izquierda, proa a la derecha): detrás, el remo y las dos cuias atadas a
        // popa; Capi sentado de perfil mirando a la proa; delante, el casco de madera con su borda y la línea de agua. Nada de
        // cestas ni personas.
        private Sprite BuildMapCanoe()
        {
            const float k = MascotPxPerDp;
            int w = Mathf.RoundToInt(MapCanoeWidthDp * k);
            int h = Mathf.RoundToInt(MapCanoeHeightDp * k);
            var pc = new PixelCanvas(w, h);
            Color ink = new Color(Palette.Groove.r, Palette.Groove.g, Palette.Groove.b, 0.85f);

            // Remo: la pala dentro del agua por delante de Capi, el mango hacia su pecho.
            pc.FillCapsule(25.5f * k, 19.5f * k, 33.5f * k, 6.5f * k, 1.15f * k, ink);
            pc.FillCapsule(25.5f * k, 19.5f * k, 33.5f * k, 6.5f * k, 0.6f * k, Palette.FamilyWood.Light);
            pc.FillEllipseSoft(33.8f * k, 6.2f * k, 1.7f * k, 3.2f * k, 1f, Palette.FamilyWood.Edge);

            // Dos cuias de vidrio atadas a popa, con la cuerda de fibra que las cruza.
            DrawTiedCuia(pc, 5f, 11f, 17.5f, k, ink);
            DrawTiedCuia(pc, 9.2f, 11f, 16f, k, ink);
            pc.FillCapsule(2.6f * k, 14f * k, 11.6f * k, 13.4f * k, 0.55f * k, Palette.FiberShadow);
            pc.FillCapsule(2.6f * k, 14f * k, 11.6f * k, 13.4f * k, 0.3f * k, Palette.FiberLight);

            // Capi sentado (el cuadrado de 20 dp, las patas dentro del casco).
            DrawCapi(pc, Mathf.RoundToInt(12f * k), Mathf.RoundToInt(8f * k), Mathf.RoundToInt(20f * k));

            // Casco: 38 dp de eslora, los extremos levantados y la proa (derecha) más alta; borda clara y una línea de tablón.
            Color hullTop = Palette.DockLight;
            Color hullLow = Palette.DockShadow;
            for (int y = 0; y < h; y++)
            {
                float yd = (y + 0.5f) / k;
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float xn = (xd - 20f) / 19f;
                    float ax = Mathf.Min(1f, Mathf.Abs(xn));
                    float rise = (4f * Mathf.Pow(ax, 5f)) + (xn > 0f ? 2.2f * ax * ax * ax : 0f);
                    float top = 12.5f + rise;
                    float bottom = 12.5f - (7.5f * Mathf.Pow(Mathf.Max(0f, 1f - (xn * xn)), 0.7f));
                    float d = Mathf.Max(Mathf.Max(bottom - yd, yd - top), (Mathf.Abs(xn) - 1f) * 19f);
                    float cov = PixelCanvas.Coverage(d * k);
                    if (cov > 0f)
                    {
                        pc.Blend(x, y, ink, cov);
                        float inside = PixelCanvas.Coverage((d + 1.1f) * k);
                        if (inside > 0f)
                        {
                            float t = Mathf.Clamp01((top - yd) / Mathf.Max(0.01f, top - bottom));
                            Color c = Color.Lerp(hullTop, hullLow, t);
                            if (top - yd < 1.7f)
                            {
                                c = Palette.ShelfEdge;
                            }
                            else if (Mathf.Abs(yd - (bottom + ((top - bottom) * 0.5f))) < 0.35f)
                            {
                                c = ColorMath.Darken(c, 0.25f);
                            }

                            c.a = 1f;
                            pc.Blend(x, y, c, inside);
                        }
                    }

                    // Línea de agua de vidrio, ondulada, por delante del casco.
                    if (xd > 0.8f && xd < MapCanoeWidthDp - 0.8f)
                    {
                        float yw = 5.6f + (0.7f * Mathf.Sin((xd * 0.9f) + 0.6f));
                        float dw = Mathf.Abs(yd - yw) - 0.9f;
                        pc.Blend(x, y, new Color(Palette.GlassRim.r, Palette.GlassRim.g, Palette.GlassRim.b, 0.9f), PixelCanvas.Coverage(dw * k));
                    }
                }
            }

            return MakeSprite(pc, "map_canoe_capi", new Vector2(0.5f, 0f), Vector4.zero, k);
        }

        // Una cuia de vidrio pequeña atada a popa (dp): cuerpo redondeado y cuello corto con su borde de fibra, de bottom a top.
        private static void DrawTiedCuia(PixelCanvas pc, float cx, float bottom, float top, float k, Color ink)
        {
            float neck = top - 1.6f;
            pc.FillRoundedRect((cx - 2.1f) * k, bottom * k, (cx + 2.1f) * k, neck * k, 1.6f * k, ink);
            pc.FillRoundedRect((cx - 1.6f) * k, (bottom + 0.4f) * k, (cx + 1.6f) * k, (neck - 0.45f) * k, 1.2f * k,
                new Color(Palette.GlassRim.r, Palette.GlassRim.g, Palette.GlassRim.b, 0.92f));
            pc.FillRoundedRect((cx - 1.15f) * k, (neck - 0.6f) * k, (cx + 1.15f) * k, top * k, 0.5f * k, ink);
            pc.FillRoundedRect((cx - 0.75f) * k, (neck - 0.3f) * k, (cx + 0.75f) * k, (top - 0.35f) * k, 0.35f * k, Palette.FiberBase);
            pc.FillCapsule((cx - 0.9f) * k, (bottom + 1.2f) * k, (cx - 0.9f) * k, (neck - 1.2f) * k, 0.35f * k, new Color(1f, 1f, 1f, 0.8f));
        }
    }
}
