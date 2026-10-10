#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Sprites de las partículas de desbloqueo de <see cref="FxLayer"/> (arquitectura del paso 1 §9 y §12; ficha de Magic Sort
    /// §3): el destello de estrella de cuatro puntas del revelado (§3.1: «3 o 4 destellos blancos en forma de estrella»), la
    /// esquirla de cristal o astilla que sale despedida (§3.4-§3.5) y la burbuja que sube y estalla (propuesta de D-007 b.2:
    /// Remolino y Aguas turbias). Todos blancos o en grises, para que los tiña <c>Image.color</c>; formas y tamaños [P].
    /// </summary>
    /// <remarks>
    /// Perezosos, como los de <c>ProceduralArt.Mechanics.cs</c>: se generan la primera vez que se piden (<see cref="FxLayer"/>
    /// los pide al construirse, para no crear texturas al emitir) y tras <see cref="Dispose"/> se vuelven a generar. Uno por
    /// familia y pequeños (≤ 64 px de lado): cada uno crea una textura.
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        // Lados de los lienzos (px). Las partículas miden de 4 a 22 dp: 64 px bastan a 3 px/dp.
        private const int StarSparklePx = 64;
        private const int ShardWidthPx = 32;
        private const int ShardHeightPx = 40;
        private const int BubblePx = 48;

        // Esquirla: polígono convexo en sentido antihorario (px del lienzo de 32 × 40) y el tono de cada faceta, la que va de
        // un vértice al siguiente pasando por el centro (los tonos dan el tallado al teñirla).
        private static readonly Vector2[] ShardOutline =
        {
            new Vector2(7f, 3f), new Vector2(24f, 9f), new Vector2(29f, 27f), new Vector2(15f, 37f), new Vector2(3f, 19f),
        };

        private static readonly float[] ShardFacetShade = { 0.86f, 0.70f, 0.80f, 1f, 0.93f };

        private Sprite? _starSparkle;
        private Sprite? _shard;
        private Sprite? _bubble;

        /// <summary>
        /// Destello de estrella de cuatro puntas con las puntas finas sobre los ejes, el núcleo brillante y un halo suave
        /// (blanco; aditivo en <see cref="FxLayer"/>). Distinto de <see cref="Star"/>, el destello más ancho del sellado.
        /// </summary>
        public Sprite StarSparkle => _starSparkle != null ? _starSparkle : (_starSparkle = BuildStarSparkle());

        /// <summary>Esquirla de cristal de cinco lados y caras de distinto tono, con un canto de luz (4:5; se tiñe).</summary>
        public Sprite Shard => _shard != null ? _shard : (_shard = BuildShard());

        /// <summary>Burbuja: aro claro, interior casi transparente y dos brillos (se tiñe; en <see cref="FxLayer"/>, blanca).</summary>
        public Sprite Bubble => _bubble != null ? _bubble : (_bubble = BuildBubble());

        private Sprite BuildStarSparkle()
        {
            const int s = StarSparklePx;
            const float c = s * 0.5f;
            var pc = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = Mathf.Abs(((x + 0.5f) - c) / c);
                    float dy = Mathf.Abs(((y + 0.5f) - c) / c);

                    // Astroide |x|^0,42 + |y|^0,42 ≤ 1: lados cóncavos y puntas finas en los ejes (cintura de 0,18 en la
                    // diagonal), con un canto suave de 0,1 en ese espacio.
                    float f = Mathf.Pow(dx, 0.42f) + Mathf.Pow(dy, 0.42f);
                    float star = Mathf.Clamp01((1f - f) / 0.1f);
                    float r = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float core = Mathf.Clamp01(1f - (r / 0.26f));
                    float halo = Mathf.Clamp01(1f - (r / 0.7f));
                    float a = Mathf.Max(star, Mathf.Max(core * core, halo * halo * halo * 0.4f));
                    pc.Set(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            return MakeSprite(pc, "fx_star_sparkle", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildShard()
        {
            var pc = new PixelCanvas(ShardWidthPx, ShardHeightPx);
            Vector2[] poly = ShardOutline;
            int n = poly.Length;
            Vector2 center = Vector2.zero;
            for (int k = 0; k < n; k++)
            {
                center += poly[k];
            }

            center /= n;
            for (int y = 0; y < ShardHeightPx; y++)
            {
                for (int x = 0; x < ShardWidthPx; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);

                    // Distancia con signo al polígono convexo (la mayor de las de sus lados): una sola cobertura para toda la
                    // silueta, sin costuras entre facetas.
                    float d = float.MinValue;
                    int facet = 0;
                    for (int k = 0; k < n; k++)
                    {
                        Vector2 a = poly[k];
                        Vector2 b = poly[(k + 1) % n];
                        d = Mathf.Max(d, OutsideDistance(a, b, p));
                        if (Cross(a - center, p - center) >= 0f && Cross(p - center, b - center) >= 0f)
                        {
                            facet = k;
                        }
                    }

                    float cover = PixelCanvas.Coverage(d);
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    float shade = ShardFacetShade[facet];
                    pc.Blend(x, y, new Color(shade, shade, shade, 1f), cover);
                }
            }

            // Canto de luz en el lado de arriba a la izquierda (del 4.º al 5.º vértice) y un brillo en la faceta más clara.
            pc.FillCapsule(poly[3].x, poly[3].y - 1.2f, poly[4].x + 1.2f, poly[4].y, 0.8f, new Color(1f, 1f, 1f, 0.85f));
            pc.FillCircle(13f, 26f, 1.6f, new Color(1f, 1f, 1f, 0.9f));
            return MakeSprite(pc, "fx_shard", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildBubble()
        {
            const int s = BubblePx;
            const float c = s * 0.5f;
            const float radius = 21f;
            var pc = new PixelCanvas(s, s);
            pc.FillCircle(c, c, radius - 2f, new Color(1f, 1f, 1f, 0.14f)); // interior casi transparente
            pc.StrokeArc(c, c, radius - 1.5f, 3f, 0f, 360f, new Color(1f, 1f, 1f, 0.95f)); // aro
            pc.FillEllipseSoft(c - 7f, c + 8f, 5f, 3.2f, 1.5f, new Color(1f, 1f, 1f, 0.95f)); // brillo arriba a la izquierda
            pc.FillCircle(c + 8f, c - 9f, 2f, new Color(1f, 1f, 1f, 0.5f)); // reflejo abajo a la derecha
            return MakeSprite(pc, "fx_bubble", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Distancia con signo de p a la recta a → b (positiva fuera de un polígono en sentido antihorario).
        private static float OutsideDistance(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 e = b - a;
            float len = Mathf.Max(1e-6f, e.magnitude);
            return -Cross(e, p - a) / len;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return (a.x * b.y) - (a.y * b.x);
        }
    }
}
