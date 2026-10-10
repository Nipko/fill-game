#nullable enable
using AF.Presentation;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Pintura procedural [P] de las piezas propias de los chips del HUD y de Siguiente (tarea T09): la cinta de madera del
    /// chip de dificultad (<see cref="DifficultyChip"/>) y la placa, la nube y la gotita del contador (<see cref="HudCounterChip"/>).
    /// <c>PixelCanvas</c> es interno de AF.Presentation, así que estas piezas, que solo usa la UI, se pintan aquí con las mismas
    /// distancias con signo y la misma mezcla «encima» con cobertura. A 3 px por dp (<see cref="ProceduralArt.UiPxPerDp"/>).
    /// Solo asigna memoria al crear cada sprite (una vez por app).
    /// </summary>
    internal static class ChipPaint
    {
        /// <summary>Píxeles por dp de los sprites de UI.</summary>
        public const float PxPerDp = ProceduralArt.UiPxPerDp;

        /// <summary>Cobertura de antialias para una distancia con signo en píxeles (negativa dentro).</summary>
        public static float Coverage(float signedDistancePx)
        {
            return Mathf.Clamp01(0.5f - signedDistancePx);
        }

        /// <summary>Cobertura suave (sombras): 1 dentro y 0 a <paramref name="softPx"/> píxeles fuera.</summary>
        public static float SoftCoverage(float signedDistancePx, float softPx)
        {
            return Mathf.Clamp01(0.5f - (signedDistancePx / Mathf.Max(0.001f, softPx)));
        }

        /// <summary>Distancia con signo a un rectángulo [x0, x1] × [y0, y1] de esquinas de radio <paramref name="r"/>.</summary>
        public static float RoundedRectDistance(float px, float py, float x0, float y0, float x1, float y1, float r)
        {
            float qx = Mathf.Abs(px - ((x0 + x1) * 0.5f)) - (((x1 - x0) * 0.5f) - r);
            float qy = Mathf.Abs(py - ((y0 + y1) * 0.5f)) - (((y1 - y0) * 0.5f) - r);
            float ox = Mathf.Max(qx, 0f);
            float oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt((ox * ox) + (oy * oy)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        /// <summary>Distancia con signo a un círculo.</summary>
        public static float CircleDistance(float px, float py, float cx, float cy, float r)
        {
            float dx = px - cx;
            float dy = py - cy;
            return Mathf.Sqrt((dx * dx) + (dy * dy)) - r;
        }

        /// <summary>Distancia con signo a una gota con la punta hacia arriba: círculo de radio r en (cx, cy) y punta en cy − tip.</summary>
        public static float DropDistance(float px, float py, float cx, float cy, float r, float tip)
        {
            float d = CircleDistance(px, py, cx, cy, r);

            // Cono tangente al círculo hasta la punta (y hacia abajo en la imagen: la punta queda arriba).
            float apexY = cy - tip;
            if (py < cy && py >= apexY)
            {
                float t = (py - apexY) / (cy - apexY); // 0 en la punta, 1 en el centro
                float half = r * 0.9f * Mathf.Sqrt(Mathf.Clamp01(t));
                float side = Mathf.Abs(px - cx) - half;
                d = Mathf.Min(d, Mathf.Max(side, apexY - py));
            }

            return d;
        }

        /// <summary>«Encima»: <paramref name="c"/> sobre <paramref name="dst"/> con alfa <c>c.a × coverage</c>.</summary>
        public static Color Over(Color dst, Color c, float coverage)
        {
            float a = c.a * coverage;
            if (a <= 0f)
            {
                return dst;
            }

            float outA = a + (dst.a * (1f - a));
            if (outA <= 0f)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            float k = dst.a * (1f - a);
            return new Color(((c.r * a) + (dst.r * k)) / outA, ((c.g * a) + (dst.g * k)) / outA, ((c.b * a) + (dst.b * k)) / outA, outA);
        }

        /// <summary>
        /// Sprite legible (los tests miran sus píxeles) de <paramref name="w"/> × <paramref name="h"/> px a
        /// <see cref="PxPerDp"/>, con el 9-slice <paramref name="border"/> (px) y fuera del guardado de la escena.
        /// </summary>
        public static Sprite ToSprite(Color[] pixels, int w, int h, string name, Vector4 border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f * PxPerDp, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
