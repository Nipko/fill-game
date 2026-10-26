#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Las piezas del icono v2 (D-045, «El río que se tiñe») para la intro y la pantalla de arranque (añadido de D-051): la cuia de
    /// vidrio con Taperebá, Aguaje y Açaí (silueta de <c>CuiaGeometry</c> en capacidad 3, inclinada según D-006), su chorro en
    /// parábola, los aros del impacto y las dos palmeras de açaí. Las capas las genera
    /// <c>docs/art/marca-oficial/v2/exportar.py --unity-emblem</c> desde el mismo SVG del icono (<c>BrandEmblem.Generated.cs</c>, en
    /// unidades del icono de 1024 con y hacia abajo) y aquí se componen en un <see cref="VectorCanvas"/> con una escala y un
    /// desplazamiento: nítidas a cualquier densidad, sin PNG. El río con meandros que las rodea lo compone
    /// <see cref="BrandRiverScene"/>.
    /// </summary>
    public static partial class BrandEmblem
    {
        /// <summary>Grupo: los aros del impacto y las gotitas.</summary>
        public const int Impact = 3;

        /// <summary>Grupo: el chorro, de la boca de la cuia al impacto.</summary>
        public const int Stream = 4;

        /// <summary>Grupo: la cuia de vidrio con sus tres dosis, su sombra, su pie y su boca de fibra.</summary>
        public const int Cuia = 5;

        /// <summary>Grupo: la palmera de açaí de la izquierda del icono.</summary>
        public const int PalmA = 6;

        /// <summary>Grupo: la palmera de açaí de la derecha del icono.</summary>
        public const int PalmB = 7;

        /// <summary>Número de grupos (0-2 quedan sin uso: el río y la selva los compone el juego).</summary>
        public const int GroupCount = 8;

        private static Rect[]? s_groupBounds;

        /// <summary>La caja de un grupo (unidades del icono); vacía si el grupo no tiene capas.</summary>
        public static Rect GroupBounds(int group)
        {
            EnsureBounds();
            return s_groupBounds![group];
        }

        /// <summary>Compone las capas del grupo <paramref name="group"/> en <paramref name="canvas"/>, llevadas a dp con x · s + tx, y · s + ty.</summary>
        internal static void Draw(VectorCanvas canvas, int group, float s, float tx, float ty, Color? tint = null)
        {
            for (int layer = 0; layer < LayerCount; layer++)
            {
                if (LayerGroup[layer] != group)
                {
                    continue;
                }

                int k = layer * 3;
                Color c0 = Unpack(LayerColors[k]);
                Color c1 = Unpack(LayerColors[k + 1]);
                Color c2 = Unpack(LayerColors[k + 2]);
                if (tint.HasValue)
                {
                    c0 = c1 = c2 = new Color(tint.Value.r, tint.Value.g, tint.Value.b, c0.a * tint.Value.a);
                }

                var paint = new VectorCanvas.Paint(c0, c1, c2, LayerStops[k], LayerStops[k + 1], LayerStops[k + 2],
                    (LayerGradientY0[layer] * s) + ty, (LayerGradientY1[layer] * s) + ty, LayerOpacity[layer]);
                int clip = LayerClip[layer];
                if (clip >= 0)
                {
                    canvas.Fill(Points, ContourEnds, LayerFirstContour[layer], LayerContourCount[layer], s, tx, ty, paint,
                        Points, ContourEnds, LayerFirstContour[clip], LayerContourCount[clip]);
                }
                else
                {
                    canvas.Fill(Points, ContourEnds, LayerFirstContour[layer], LayerContourCount[layer], s, tx, ty, paint);
                }
            }
        }

        /// <summary>
        /// Rasteriza el grupo <paramref name="group"/> solo, en la ventana <paramref name="windowDp"/> a <paramref name="pxPerDp"/>,
        /// llevado a dp con x · s + tx, y · s + ty.
        /// </summary>
        public static Texture2D Render(int group, Rect windowDp, float pxPerDp, float s, float tx, float ty, string name)
        {
            var canvas = new VectorCanvas(windowDp, pxPerDp);
            Draw(canvas, group, s, tx, ty);
            return canvas.ToTexture(name);
        }

        private static Color Unpack(uint rgba)
        {
            return new Color(((rgba >> 24) & 0xFF) / 255f, ((rgba >> 16) & 0xFF) / 255f, ((rgba >> 8) & 0xFF) / 255f, (rgba & 0xFF) / 255f);
        }

        private static void EnsureBounds()
        {
            if (s_groupBounds != null)
            {
                return;
            }

            var groups = new Rect[GroupCount];
            var seen = new bool[GroupCount];
            for (int layer = 0; layer < LayerCount; layer++)
            {
                if (LayerClip[layer] >= 0)
                {
                    continue; // una trama no pasa de la forma que la recorta
                }

                int first = LayerFirstContour[layer];
                int start = first == 0 ? 0 : ContourEnds[first - 1];
                int end = ContourEnds[first + LayerContourCount[layer] - 1];
                int group = LayerGroup[layer];
                for (int i = start; i < end; i++)
                {
                    float x = Points[2 * i];
                    float y = Points[(2 * i) + 1];
                    if (!seen[group])
                    {
                        groups[group] = new Rect(x, y, 0f, 0f);
                        seen[group] = true;
                        continue;
                    }

                    Rect r = groups[group];
                    groups[group] = Rect.MinMaxRect(Mathf.Min(r.xMin, x), Mathf.Min(r.yMin, y), Mathf.Max(r.xMax, x), Mathf.Max(r.yMax, y));
                }
            }

            s_groupBounds = groups;
        }
    }
}
