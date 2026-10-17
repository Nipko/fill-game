#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Muestra del tinte para la UI (tarjeta «Fruta nueva», 06 §4.5: «muestra de color, patrón de accesibilidad»; tarea F5) [P]:
    /// la misma que la llave del medallón de la Hoja atada (<see cref="HojaAtadaSwatch"/>), un disco del tinte con su patrón y su
    /// icono de accesibilidad, como una dosis vista de frente. Y el disco bajo el icono de la fruta de esa tarjeta
    /// (<see cref="FruitNewDisc"/>), a la resolución de la UI, con la luminancia de cada icono (<see cref="FruitIconLuminance"/>)
    /// para darle el fondo con contraste (D-003, principio 3). Sin arte nuevo: lo sustituye el del artista.
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
        /// ponderada por su alfa: el fondo que necesita para leerse (la tarjeta «Fruta nueva» elige su disco con ella). Lee los
        /// píxeles del atlas si es legible; si no (el procedural y, sin <c>isReadable</c>, el de los PNG), los de la fruta
        /// procedural [P]. Una vez por tinte y en caché: al enseñar la tarjeta, nunca por fotograma.
        /// </summary>
        public float FruitIconLuminance(int tint)
        {
            int t = Mathf.Clamp(tint, 1, 14);
            float cached = _fruitIconLuminance[t];
            if (!float.IsNaN(cached))
            {
                return cached;
            }

            float sumY = 0f;
            float sumA = 0f;
            Sprite sprite = FruitFlight(t);
            Texture2D tex = sprite.texture;
            if (tex != null && tex.isReadable)
            {
                Rect r = sprite.textureRect;
                Color[] px = tex.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
                for (int i = 0; i < px.Length; i++)
                {
                    AddLuminance(px[i], ref sumY, ref sumA);
                }
            }
            else
            {
                var pc = new PixelCanvas(FruitFlightPx, FruitFlightPx);
                DrawFruit(pc, t, 0f, 0f);
                for (int y = 0; y < pc.Height; y++)
                {
                    for (int x = 0; x < pc.Width; x++)
                    {
                        AddLuminance(pc.Get(x, y), ref sumY, ref sumA);
                    }
                }
            }

            float lum = sumA > 0f ? sumY / sumA : 0f;
            _fruitIconLuminance[t] = lum;
            return lum;
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
