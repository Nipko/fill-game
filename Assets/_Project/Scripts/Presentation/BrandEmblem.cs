#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// El emblema v2 (D-045, «El río que se tiñe»: la cuia de vidrio con Taperebá, Aguaje y Açaí, inclinada según D-006, vierte un
    /// chorro en parábola y el açaí tiñe el río desde el impacto, río abajo) para la intro y la pantalla de arranque (añadido de
    /// D-051). Las capas las genera <c>docs/art/marca-oficial/v2/exportar.py --unity-emblem</c> desde el mismo SVG del icono
    /// (<c>BrandEmblem.Generated.cs</c>) y aquí se rasterizan por grupos, al tamaño real en píxeles de la pantalla: nítido a
    /// cualquier densidad, sin PNG. Coordenadas en unidades del icono de 1024 (y hacia abajo). Solo en la construcción, nunca por
    /// fotograma.
    /// </summary>
    public static partial class BrandEmblem
    {
        /// <summary>Grupo: palmeras, selva, franja de la orilla, río blanco y sus ondas.</summary>
        public const int Base = 0;

        /// <summary>Grupo: el açaí que se abre desde el impacto, con sus ondas.</summary>
        public const int Acai = 1;

        /// <summary>Grupo: la orilla de acá, con su línea clara.</summary>
        public const int Bank = 2;

        /// <summary>Grupo: los aros del impacto y las gotitas.</summary>
        public const int Impact = 3;

        /// <summary>Grupo: el chorro, de la boca de la cuia al impacto.</summary>
        public const int Stream = 4;

        /// <summary>Grupo: la cuia de vidrio con sus tres dosis, su sombra, su pie y su boca de fibra.</summary>
        public const int Cuia = 5;

        /// <summary>Número de grupos.</summary>
        public const int GroupCount = 6;

        private static Rect[]? s_layerBounds;
        private static Rect[]? s_groupBounds;

        /// <summary>La caja de un grupo (unidades del icono).</summary>
        public static Rect GroupBounds(int group)
        {
            EnsureBounds();
            return s_groupBounds![group];
        }

        /// <summary>
        /// Rasteriza los grupos de <paramref name="groupMask"/> (bit 1 &lt;&lt; grupo) en la ventana <paramref name="window"/>
        /// (unidades del icono) a <paramref name="pxPerUnit"/> px por unidad, en el orden de pintado del SVG. La textura mide
        /// ⌈ancho · px⌉ × ⌈alto · px⌉ y su píxel (0, 0) es la esquina de arriba a la izquierda de la ventana. Con
        /// <paramref name="fadeRightUnits"/> &gt; 0, el borde derecho se funde en ese ancho (unidades), para que la pieza no
        /// enseñe un corte recto cuando se anima encogida.
        /// </summary>
        public static Texture2D Render(int groupMask, Rect window, float pxPerUnit, string name, float fadeRightUnits = 0f)
        {
            EnsureBounds();
            int w = Mathf.Max(1, Mathf.CeilToInt(window.width * pxPerUnit));
            int h = Mathf.Max(1, Mathf.CeilToInt(window.height * pxPerUnit));
            var r = new float[w * h];
            var g = new float[w * h];
            var b = new float[w * h];
            var a = new float[w * h];
            for (int layer = 0; layer < LayerCount; layer++)
            {
                if ((groupMask & (1 << LayerGroup[layer])) == 0)
                {
                    continue;
                }

                Rect lb = s_layerBounds![layer];
                int x0 = Mathf.Max(0, Mathf.FloorToInt((lb.xMin - window.xMin) * pxPerUnit) - 1);
                int y0 = Mathf.Max(0, Mathf.FloorToInt((lb.yMin - window.yMin) * pxPerUnit) - 1);
                int x1 = Mathf.Min(w, Mathf.CeilToInt((lb.xMax - window.xMin) * pxPerUnit) + 1);
                int y1 = Mathf.Min(h, Mathf.CeilToInt((lb.yMax - window.yMin) * pxPerUnit) + 1);
                if (x1 <= x0 || y1 <= y0)
                {
                    continue;
                }

                int rw = x1 - x0;
                int rh = y1 - y0;
                float ox = (-window.xMin * pxPerUnit) - x0;
                float oy = (-window.yMin * pxPerUnit) - y0;
                float[] cov = VectorFill.Coverage(Points, ContourEnds, LayerFirstContour[layer], LayerContourCount[layer], pxPerUnit, ox, oy, rw, rh);
                int clip = LayerClip[layer];
                float[]? clipCov = clip >= 0
                    ? VectorFill.Coverage(Points, ContourEnds, LayerFirstContour[clip], LayerContourCount[clip], pxPerUnit, ox, oy, rw, rh)
                    : null;
                float opacity = LayerOpacity[layer];
                float gy0 = LayerGradientY0[layer];
                float gy1 = LayerGradientY1[layer];
                float span = Mathf.Max(1e-3f, gy1 - gy0);
                for (int row = 0; row < rh; row++)
                {
                    float iy = window.yMin + ((y0 + row + 0.5f) / pxPerUnit);
                    Color c = Gradient(layer, (iy - gy0) / span);
                    float ca = c.a * opacity;
                    int src = row * rw;
                    int dst = ((y0 + row) * w) + x0;
                    for (int x = 0; x < rw; x++)
                    {
                        float k = cov[src + x];
                        if (clipCov != null)
                        {
                            k *= clipCov[src + x];
                        }

                        k *= ca;
                        if (k <= 0f)
                        {
                            continue;
                        }

                        int i = dst + x;
                        float keep = 1f - k;
                        r[i] = (c.r * k) + (r[i] * keep);
                        g[i] = (c.g * k) + (g[i] * keep);
                        b[i] = (c.b * k) + (b[i] * keep);
                        a[i] = k + (a[i] * keep);
                    }
                }
            }

            var px = new Color32[w * h];
            float fadePx = fadeRightUnits * pxPerUnit;
            for (int row = 0; row < h; row++)
            {
                int src = row * w;
                int dst = (h - 1 - row) * w; // la textura va de abajo arriba
                for (int x = 0; x < w; x++)
                {
                    float al = a[src + x];
                    if (fadePx > 0f && al > 0f)
                    {
                        float f = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((w - x - 0.5f) / fadePx));
                        r[src + x] *= f;
                        g[src + x] *= f;
                        b[src + x] *= f;
                        al *= f;
                    }

                    px[dst + x] = al <= 0f
                        ? new Color32(0, 0, 0, 0)
                        : (Color32)new Color(r[src + x] / al, g[src + x] / al, b[src + x] / al, al);
                }
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // El color del degradado de la capa en t (0 arriba, 1 abajo de la caja de su forma).
        private static Color Gradient(int layer, float t)
        {
            t = Mathf.Clamp01(t);
            int k = layer * 3;
            float o0 = LayerStops[k];
            float o1 = LayerStops[k + 1];
            float o2 = LayerStops[k + 2];
            Color c0 = Unpack(LayerColors[k]);
            Color c1 = Unpack(LayerColors[k + 1]);
            Color c2 = Unpack(LayerColors[k + 2]);
            if (t <= o0)
            {
                return c0;
            }

            if (t <= o1)
            {
                return Color.Lerp(c0, c1, (t - o0) / Mathf.Max(1e-4f, o1 - o0));
            }

            if (t <= o2)
            {
                return Color.Lerp(c1, c2, (t - o1) / Mathf.Max(1e-4f, o2 - o1));
            }

            return c2;
        }

        private static Color Unpack(uint rgba)
        {
            return new Color(((rgba >> 24) & 0xFF) / 255f, ((rgba >> 16) & 0xFF) / 255f, ((rgba >> 8) & 0xFF) / 255f, (rgba & 0xFF) / 255f);
        }

        private static void EnsureBounds()
        {
            if (s_layerBounds != null)
            {
                return;
            }

            var layers = new Rect[LayerCount];
            var groups = new Rect[GroupCount];
            var seen = new bool[GroupCount];
            for (int layer = 0; layer < LayerCount; layer++)
            {
                int first = LayerFirstContour[layer];
                int start = first == 0 ? 0 : ContourEnds[first - 1];
                int end = ContourEnds[first + LayerContourCount[layer] - 1];
                float xMin = float.MaxValue;
                float yMin = float.MaxValue;
                float xMax = float.MinValue;
                float yMax = float.MinValue;
                for (int i = start; i < end; i++)
                {
                    float x = Points[2 * i];
                    float y = Points[(2 * i) + 1];
                    xMin = Mathf.Min(xMin, x);
                    yMin = Mathf.Min(yMin, y);
                    xMax = Mathf.Max(xMax, x);
                    yMax = Mathf.Max(yMax, y);
                }

                layers[layer] = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
                int group = LayerGroup[layer];
                if (LayerClip[layer] >= 0)
                {
                    continue; // una trama no pasa de la forma que la recorta
                }

                groups[group] = seen[group]
                    ? Rect.MinMaxRect(Mathf.Min(groups[group].xMin, xMin), Mathf.Min(groups[group].yMin, yMin), Mathf.Max(groups[group].xMax, xMax), Mathf.Max(groups[group].yMax, yMax))
                    : layers[layer];
                seen[group] = true;
            }

            s_groupBounds = groups;
            s_layerBounds = layers;
        }
    }
}
