#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte provisional del Muestrario y de los miradores del Mapa del río (D-013, punto 10: Q9-A; 06 §4.5; propuesta del Mapa §5.2
    /// y §7; tarea M4.1), en la familia de D-003 (vidrio, madera y luz de juego):
    /// <list type="bullet">
    /// <item><see cref="MapViewpoint"/>: el <b>mirador</b>, el «?» fuera de la ruta que revela el progreso (propuesta §5.2), una
    /// burbuja de vidrio azul hielo de <see cref="MapViewpointDp"/> dp con su canto oscuro, un brillo arriba a la izquierda y el
    /// «?» dibujado en madera oscura (ninguna de las dos familias de letra tiene que pintarlo: es un icono, como el ✓ de P-M2,
    /// nunca texto). Lo usan el marcador del Mapa y, en pequeño, el distintivo de la muestra especial del Muestrario.</item>
    /// </list>
    /// La repisa del Muestrario es la de las filas del tablero (<see cref="Shelf"/>, guía 4.5): el Muestrario «parece un tablero en
    /// pequeño» (propuesta §7).
    /// <para>Perezosa: nada se crea al construir <see cref="ProceduralArt"/>, solo la primera vez que se pide
    /// (<see cref="SamplesArtBuilt"/>); tras <see cref="Dispose"/> se vuelve a generar (el == de Unity). Sin ranura propia todavía:
    /// el encargo del Mapa (propuesta §12) no lista el mirador, así que va siempre por código.</para>
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Lado del mirador en el Mapa (dp) [P]: dentro de su área táctil de 48 dp.</summary>
        public const float MapViewpointDp = 28f;

        /// <summary>Vidrio del mirador: el azul hielo del vidrio vacío del Mapa (D-013, punto 4: nunca parece sequía) [P].</summary>
        public static readonly Color ViewpointGlass = Palette.Hex(0xCFEFF2);

        /// <summary>Tinta del «?» del mirador: la madera oscura de los textos sobre crema (contraste alto con el vidrio).</summary>
        public static readonly Color ViewpointInk = Palette.WoodDark;

        private Sprite? _viewpoint;

        /// <summary>Si ya se creó alguna de las piezas de este archivo.</summary>
        public bool SamplesArtBuilt => _viewpoint != null;

        /// <summary>
        /// El mirador: burbuja de vidrio azul hielo (<see cref="ViewpointGlass"/>) de <see cref="MapViewpointDp"/> dp con canto
        /// oscuro y aro crema, un brillo y el «?» en <see cref="ViewpointInk"/>. Pivote en el centro.
        /// </summary>
        public Sprite MapViewpoint => _viewpoint != null ? _viewpoint : (_viewpoint = BuildMapViewpoint());

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
    }
}
