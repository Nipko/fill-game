#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Piezas del Muestrario y de los miradores del Mapa del río (D-013, punto 10: Q9-A; 06 §4.5; propuesta del Mapa §5.2, §7 y
    /// §12; tarea M4.1), en la familia de D-003 (vidrio, madera y luz de juego):
    /// <list type="bullet">
    /// <item><see cref="MapViewpoint"/>: el <b>mirador</b>, el «?» fuera de la ruta que revela el progreso (propuesta §5.2). Su
    /// ranura es <see cref="MapViewpointSlot"/> (<c>map_mirador</c>, del atlas de UI: propuesta §12, fila 13; encargo del Mapa); sin
    /// su PNG, el provisional: una burbuja de vidrio azul hielo de <see cref="MapViewpointDp"/> dp con su canto oscuro, un brillo
    /// arriba a la izquierda y el «?» dibujado en madera oscura (ninguna de las dos familias de letra tiene que pintarlo: es un
    /// icono, como el ✓ de P-M2, nunca texto). Lo usan el marcador del Mapa, la tarjeta del mirador y, en pequeño, el distintivo
    /// de la muestra especial del Muestrario.</item>
    /// <item><see cref="MapRack"/>: la <b>repisa del Muestrario</b>. Su ranura es <see cref="MapRackSlot"/> (<c>map_rack</c>, 9
    /// cortes: propuesta §12, fila 10; encargo del Mapa); sin su PNG, la repisa de las filas del tablero
    /// (<see cref="Shelf"/>, guía 4.5): el Muestrario «parece un tablero en pequeño» (propuesta §7).</item>
    /// <item><see cref="MapMiniCuiaEmpty"/>: la mini cuia de una muestra que falta en el Muestrario, en <b>vidrio oscuro</b>: la
    /// misma silueta y la misma caja que el vidrio de la mini cuia (<see cref="MapMiniCuia"/>, 20 × 32 dp) rellena de
    /// <see cref="EmptyGlassFill"/> con el contorno en <see cref="EmptyGlassEdge"/> y sin brillo, para que lo ganado (lleno del agua
    /// del sitio) mande sobre lo que falta. Siempre por código, como la máscara del agua; con el PNG de <c>map_minicuia</c>, el
    /// Muestrario pinta ese vidrio translúcido en su lugar (no se puede sacar la silueta de un PNG).</item>
    /// </list>
    /// <para>Las ranuras mandan, con el mismo patrón que <see cref="MapMiniCuiaFor"/> y <see cref="MapCanoeFor"/>: con su PNG no se
    /// crea el provisional. Ninguna es arte provisional de P-M5 (<see cref="MapArtRule"/>): son piezas de UI del Mapa.</para>
    /// <para>Perezosas: nada se crea al construir <see cref="ProceduralArt"/>, solo la primera vez que se piden
    /// (<see cref="SamplesArtBuilt"/>); tras <see cref="Dispose"/> se vuelven a generar (el == de Unity).</para>
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ranura del «?» del mirador (glosario §11).</summary>
        public const string MapViewpointSlot = "map_mirador";

        /// <summary>Ranura de la repisa del Muestrario, 9 cortes (glosario §11).</summary>
        public const string MapRackSlot = "map_rack";

        /// <summary>Lado del mirador en el Mapa (dp) [P]: dentro de su área táctil de 48 dp.</summary>
        public const float MapViewpointDp = 28f;

        /// <summary>Vidrio del mirador: el azul hielo del vidrio vacío del Mapa (D-013, punto 4: nunca parece sequía) [P].</summary>
        public static readonly Color ViewpointGlass = Palette.Hex(0xCFEFF2);

        /// <summary>Tinta del «?» del mirador: la madera oscura de los textos sobre crema (contraste alto con el vidrio).</summary>
        public static readonly Color ViewpointInk = Palette.WoodDark;

        /// <summary>Relleno del vidrio oscuro de una muestra que falta (revisión de M4.1): madera oscura al 35 %.</summary>
        public static readonly Color EmptyGlassFill = new Color(Palette.WoodDark.r, Palette.WoodDark.g, Palette.WoodDark.b, 0.35f);

        /// <summary>Contorno del vidrio oscuro de una muestra que falta (revisión de M4.1): crema al 30 %.</summary>
        public static readonly Color EmptyGlassEdge = new Color(Palette.Cream.r, Palette.Cream.g, Palette.Cream.b, 0.30f);

        private Sprite? _viewpoint;
        private Sprite? _miniCuiaEmpty;

        /// <summary>Si ya se creó alguna de las piezas provisionales de este archivo.</summary>
        public bool SamplesArtBuilt => _viewpoint != null || _miniCuiaEmpty != null;

        /// <summary>El mirador: el PNG de <see cref="MapViewpointSlot"/> si existe; si no, el provisional. Pivote en el centro.</summary>
        public Sprite MapViewpoint => MapViewpointFor(ArtSlots.Find(MapViewpointSlot));

        /// <summary>La repisa del Muestrario: el PNG de <see cref="MapRackSlot"/> si existe; si no, la del tablero (<see cref="Shelf"/>).</summary>
        public Sprite MapRack => MapRackFor(ArtSlots.Find(MapRackSlot));

        /// <summary>
        /// Vidrio oscuro de la mini cuia de una muestra que falta, en la caja de <see cref="MapMiniCuia"/> (pivote abajo en el
        /// centro). Siempre por código.
        /// </summary>
        public Sprite MapMiniCuiaEmpty => _miniCuiaEmpty != null ? _miniCuiaEmpty : (_miniCuiaEmpty = BuildMiniCuiaEmpty());

        /// <summary>
        /// El mirador con el PNG de su ranura explícito (<paramref name="slotPng"/>): si existe, manda. Si no, la burbuja de vidrio
        /// azul hielo (<see cref="ViewpointGlass"/>) de <see cref="MapViewpointDp"/> dp con canto oscuro y aro crema, un brillo y el
        /// «?» en <see cref="ViewpointInk"/>.
        /// </summary>
        public Sprite MapViewpointFor(Sprite? slotPng)
        {
            if (slotPng != null)
            {
                return slotPng;
            }

            return _viewpoint != null ? _viewpoint : (_viewpoint = BuildMapViewpoint());
        }

        /// <summary>La repisa del Muestrario con el PNG de su ranura explícito (<paramref name="slotPng"/>): si existe, manda.</summary>
        public Sprite MapRackFor(Sprite? slotPng)
        {
            return slotPng != null ? slotPng : Shelf;
        }

        // La burbuja, dibujada a UiPxPerDp en un lienzo de MapViewpointDp (origen abajo a la izquierda): canto oscuro, aro crema,
        // vidrio, brillo y el «?» (arco de 220° en sentido antihorario de −60° a 160°, el trazo que baja y el punto).
        private Sprite BuildMapViewpoint()
        {
            const float k = UiPxPerDp;
            const float size = MapViewpointDp;
            int pw = Mathf.RoundToInt(size * k);
            var pc = new PixelCanvas(pw, pw);
            const float c = size * 0.5f;
            pc.FillCircle(c * k, c * k, 13.6f * k, Palette.Groove);
            pc.FillCircle(c * k, c * k, 12.6f * k, Palette.Cream);
            pc.FillCircle(c * k, c * k, 11.4f * k, ViewpointGlass);
            pc.FillEllipseSoft((c - 4.2f) * k, (c + 5f) * k, 3.2f * k, 1.8f * k, 0.8f * k, new Color(1f, 1f, 1f, 0.75f));

            const float r = 3.9f;
            const float thick = 2.6f;
            float cx = c;
            float cy = c + 2.6f;
            pc.StrokeArc(cx * k, cy * k, r * k, thick * k, -60f, 160f, ViewpointInk);
            float ex = cx + (r * Mathf.Cos(-60f * Mathf.Deg2Rad));
            float ey = cy + (r * Mathf.Sin(-60f * Mathf.Deg2Rad));
            pc.FillCapsule(ex * k, ey * k, cx * k, (c - 2.8f) * k, thick * 0.5f * k, ViewpointInk);
            pc.FillCircle(cx * k, (c - 6.6f) * k, 1.55f * k, ViewpointInk);
            return MakeSprite(pc, "map_viewpoint", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        // El vidrio oscuro: la silueta exterior de BuildMiniCuia (la misma caja, el mismo eje y el mismo pie), rellena de
        // EmptyGlassFill, con su contorno de 0,9 dp en EmptyGlassEdge; sin brillo, sin cavidad y sin fibra.
        private Sprite BuildMiniCuiaEmpty()
        {
            const float k = UiPxPerDp;
            const float u = MiniCuiaUnitDp;
            const float axis = MiniCuiaWidthDp * 0.5f;
            const float baseY = 0.5f;
            const int cap = MiniCuiaCapacity;
            const float outline = 0.9f; // dp, el del vidrio
            int w = Mathf.RoundToInt(MiniCuiaWidthDp * k);
            int h = Mathf.RoundToInt(MiniCuiaHeightDp * k);
            var pc = new PixelCanvas(w, h);
            float height = CuiaGeometry.HeightU(cap);
            for (int y = 0; y < h; y++)
            {
                float yu = (((y + 0.5f) / k) - baseY) / u;
                for (int x = 0; x < w; x++)
                {
                    float xu = (((x + 0.5f) / k) - axis) / u;
                    float half = CuiaGeometry.OuterHalfWidth(Mathf.Clamp(yu, 0f, height), cap);
                    float d = Mathf.Max((Mathf.Abs(xu) - half) * u, Mathf.Max(-yu * u, (yu - height) * u)); // dp, negativa dentro
                    float outer = PixelCanvas.Coverage((d - outline) * k);
                    if (outer <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, EmptyGlassEdge, outer);
                    float inner = PixelCanvas.Coverage(d * k);
                    if (inner > 0f)
                    {
                        // Dentro del contorno, el relleno sustituye al canto (no se suman: el vidrio no se aclara).
                        pc.Set(x, y, Color.Lerp(pc.Get(x, y), EmptyGlassFill, inner));
                    }
                }
            }

            return MakeSprite(pc, "map_minicuia_empty", new Vector2(0.5f, 0f), Vector4.zero, k);
        }
    }
}
