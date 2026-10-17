#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Muestra del tinte para la UI (tarjeta «Fruta nueva», 06 §4.5: «muestra de color, patrón de accesibilidad»; tarea F5) [P]:
    /// la misma que la llave del medallón de la Hoja atada (<see cref="HojaAtadaSwatch"/>), un disco del tinte con su patrón y su
    /// icono de accesibilidad, como una dosis vista de frente. Y el disco bajo el icono de la fruta de esa tarjeta
    /// (<see cref="FruitNewDisc"/>), a la resolución de la UI, con la luminancia de cada icono (la media,
    /// <see cref="FruitIconLuminance"/>, y la de su silueta, <see cref="FruitIconEdgeLuminance"/>) para darle el fondo con
    /// contraste (D-003, principio 3). Sin arte nuevo: lo sustituye el del artista.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Lado del disco bajo el icono de la tarjeta «Fruta nueva» (dp): el icono de 40 dp con 2 dp de aire alrededor.</summary>
        public const float FruitNewDiscDp = 44f;

        // Canto interior del disco: 1 dp de tinta #2B2118 a alfa 0,12, que le da volumen sin cambiar su color [P].
        private const float FruitNewDiscRimDp = 1f;
        private static readonly Color FruitNewDiscRim = new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 0.12f);

        private Sprite? _fruitNewDisc;
        private readonly float[] _fruitIconLuminance = NewLuminanceCache();
        private readonly float[] _fruitIconEdgeLuminance = NewLuminanceCache();

        /// <summary>
        /// Muestra del tinte <paramref name="tint"/> (índice de motor 1-14): disco del color con su patrón y su icono de
        /// accesibilidad (<see cref="HojaAtadaSwatch"/>). Perezosa y en caché por tinte: se construye al enseñar la tarjeta, nunca
        /// por fotograma.
        /// </summary>
        public Sprite TintSwatch(int tint)
        {
            return HojaAtadaSwatch(tint);
        }

        /// <summary>
        /// Disco blanco (lo tiñe <c>Image.color</c>) bajo el icono de la fruta de la tarjeta «Fruta nueva», de
        /// <see cref="FruitNewDiscDp"/> dp a <see cref="UiPxPerDp"/> px/dp (132 px; el <see cref="Disc"/> de 32 px se veía
        /// escalonado y borroso a ese tamaño): círculo con antialias de radio lado/2 − 1 px y un canto interior de 1 dp de tinta a
        /// alfa 0,12. Se genera la primera vez que se pide.
        /// </summary>
        public Sprite FruitNewDisc => _fruitNewDisc != null ? _fruitNewDisc : (_fruitNewDisc = BuildFruitNewDisc());

        /// <summary>
        /// Luminancia relativa WCAG media del icono de la fruta del tinte <paramref name="tint"/> (1-14; <see cref="FruitFlight"/>),
        /// ponderada por su alfa (la tarjeta «Fruta nueva» elige su disco con la de la silueta,
        /// <see cref="FruitIconEdgeLuminance"/>). Lee los píxeles del atlas si es legible; si no (el procedural y, sin
        /// <c>isReadable</c>, el de los PNG), los de la fruta procedural [P]. Una vez por tinte y en caché: al enseñar la tarjeta,
        /// nunca por fotograma.
        /// </summary>
        public float FruitIconLuminance(int tint)
        {
            int t = Mathf.Clamp(tint, 1, 14);
            if (float.IsNaN(_fruitIconLuminance[t]))
            {
                MeasureFruitIcon(t);
            }

            return _fruitIconLuminance[t];
        }

        /// <summary>
        /// Luminancia relativa WCAG media de la silueta del icono de la fruta del tinte <paramref name="tint"/> (1-14;
        /// <see cref="FruitFlight"/>): la de sus píxeles opacos (alfa &gt; 0,5) que tocan uno transparente (alfa &lt; 0,5) o el
        /// borde. Es lo que tiene que separarse del disco para que la fruta se reconozca: con la media, el centro claro del
        /// achiote elegía la madera oscura y sus púas rojas se perdían en ella (revisión de F5). Sin silueta (icono vacío), la
        /// media. Los mismos píxeles que <see cref="FruitIconLuminance"/>; una vez por tinte y en caché, nunca por fotograma.
        /// </summary>
        public float FruitIconEdgeLuminance(int tint)
        {
            int t = Mathf.Clamp(tint, 1, 14);
            if (float.IsNaN(_fruitIconEdgeLuminance[t]))
            {
                MeasureFruitIcon(t);
            }

            return _fruitIconEdgeLuminance[t];
        }

        // Mide las dos luminancias del icono del tinte t y las deja en caché: los píxeles del atlas si es legible; si no (el
        // procedural y, sin isReadable, el de los PNG), los de la fruta procedural [P].
        private void MeasureFruitIcon(int t)
        {
            Color[] px;
            int w;
            int h;
            Sprite sprite = FruitFlight(t);
            Texture2D tex = sprite.texture;
            if (tex != null && tex.isReadable)
            {
                Rect r = sprite.textureRect;
                w = (int)r.width;
                h = (int)r.height;
                px = tex.GetPixels((int)r.x, (int)r.y, w, h);
            }
            else
            {
                var pc = new PixelCanvas(FruitFlightPx, FruitFlightPx);
                DrawFruit(pc, t, 0f, 0f);
                w = pc.Width;
                h = pc.Height;
                px = new Color[w * h];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        px[(y * w) + x] = pc.Get(x, y);
                    }
                }
            }

            float sumY = 0f;
            float sumA = 0f;
            float edgeY = 0f;
            float edgeA = 0f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = px[(y * w) + x];
                    AddLuminance(c, ref sumY, ref sumA);
                    if (c.a > 0.5f && (Clear(px, w, h, x - 1, y) || Clear(px, w, h, x + 1, y) || Clear(px, w, h, x, y - 1)
                        || Clear(px, w, h, x, y + 1)))
                    {
                        AddLuminance(c, ref edgeY, ref edgeA);
                    }
                }
            }

            float mean = sumA > 0f ? sumY / sumA : 0f;
            _fruitIconLuminance[t] = mean;
            _fruitIconEdgeLuminance[t] = edgeA > 0f ? edgeY / edgeA : mean;
        }

        // El píxel (x, y) es transparente (alfa < 0,5) o está fuera del icono.
        private static bool Clear(Color[] px, int w, int h, int x, int y)
        {
            return x < 0 || y < 0 || x >= w || y >= h || !(px[(y * w) + x].a >= 0.5f);
        }

        private Sprite BuildFruitNewDisc()
        {
            int s = Mathf.RoundToInt(FruitNewDiscDp * UiPxPerDp);
            float c = s * 0.5f;
            float r = c - 1f;
            float inner = r - (FruitNewDiscRimDp * UiPxPerDp);
            var pc = new PixelCanvas(s, s);
            pc.FillCircle(c, c, r, Color.white);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) - c;
                    float dy = (y + 0.5f) - c;
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy));
                    pc.Blend(x, y, FruitNewDiscRim, PixelCanvas.Coverage(Mathf.Max(d - r, inner - d))); // el canto, dentro del borde
                }
            }

            return MakeSprite(pc, "fruit_new_disc", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        // Suma la luminancia del píxel ponderada por su alfa. Salta los transparentes y los que no son números (las hojas de
        // DrawLeaf dejan alguno fuera de su silueta: sin ellos, una sola NaN anulaba la media del taperebá y del Río Negro).
        private static void AddLuminance(Color c, ref float sumY, ref float sumA)
        {
            float y = ColorMath.RelativeLuminance(c);
            if (!(c.a > 0f) || float.IsNaN(y) || float.IsInfinity(y))
            {
                return;
            }

            float a = Mathf.Min(1f, c.a);
            sumY += a * y;
            sumA += a;
        }

        private static float[] NewLuminanceCache()
        {
            var cache = new float[15];
            for (int i = 0; i < cache.Length; i++)
            {
                cache[i] = float.NaN;
            }

            return cache;
        }
    }
}
