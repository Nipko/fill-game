#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Ventanita de casa provisional de TiTino (D-013, punto 6, Q17-A; 06 §4.7; propuesta del Mapa §6.1 y §9; propuesta del
    /// personaje c.3 y g.2; tarea M2.1): un marco de madera de <see cref="HomeWindowDp"/> × <see cref="HomeWindowDp"/> dp [P] con
    /// su embaúba del estuario dentro (el tronco claro con los anillos de sus cicatrices y dos hojas palmeadas de envés plateado,
    /// sobre el cielo de la mañana). El retrato de M1.5 (<see cref="SlothPortrait(bool)"/>) se pinta encima, colgado bajo la rama,
    /// en el hueco de <see cref="HomeWindowBorderDp"/> dp por lado; el marco no lleva nombre ni texto. Nada de hojas quemadas ni
    /// ceniza (g.2, punto 1); nunca la sumaúma ni la lupuna.
    /// </summary>
    /// <remarks>
    /// <para><b>La embaúba está pendiente del asesor cultural</b> (propuesta de TiTino g.2, punto 1, y c.4; encargo del Mapa,
    /// fila 11, que encarga <c>map_home_window</c> cuando el asesor la confirme). Si la desaconseja, la alternativa prevista es una
    /// rama genérica sin especie reconocible (o la que proponga el asesor): solo cambia el dibujo del árbol, no el marco, el
    /// retrato ni la ranura.</para>
    /// <para>El PNG de la ranura <see cref="HomeWindowSlot"/> (<c>map_home_window</c>, glosario §11) manda y se pinta en 9 cortes:
    /// es la ventanita entera, marco y embaúba [P]. Sin él, la de código sale con <see cref="MapArtRule.ShowProvisional"/>, en
    /// todos los builds, release incluido (D-052); solo si un test la apaga, nada (<see cref="HomeWindowFor"/> da null).</para>
    /// <para>Perezosa: se crea la primera vez que se pide (<see cref="HomeWindowBuilt"/>), nunca al construir
    /// <see cref="ProceduralArt"/>. Tras <see cref="Dispose"/> se vuelve a generar (el == de Unity).</para>
    /// <para>Paleta [P] de esta tarea: corteza clara de <i>Cecropia</i> #D9D3C3 con anillos #9C927F, haz #4E7D3A y envés
    /// plateado #C5CFBC; el cielo, del celeste de la mañana al verde pálido de la orilla.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ranura de la ventanita de casa (glosario §11): marco y embaúba, en 9 cortes.</summary>
        public const string HomeWindowSlot = "map_home_window";

        /// <summary>Lado de la ventanita (dp) [P]: el de la propuesta del Mapa (48-64 dp), el del retrato horneado.</summary>
        public const float HomeWindowDp = SlothPortraitDp;

        /// <summary>Grosor del marco de madera (dp) [P]: el retrato va dentro de este margen; también el borde de los 9 cortes.</summary>
        public const float HomeWindowBorderDp = 5f;

        /// <summary>Corteza clara de la embaúba #D9D3C3 [P].</summary>
        public static readonly Color EmbaubaBark = Palette.Hex(0xD9D3C3);

        /// <summary>Anillos (cicatrices de las hojas) del tronco #9C927F [P].</summary>
        public static readonly Color EmbaubaRing = Palette.Hex(0x9C927F);

        /// <summary>Haz de la hoja palmeada #4E7D3A [P].</summary>
        public static readonly Color EmbaubaLeaf = Palette.Hex(0x4E7D3A);

        /// <summary>Envés plateado de la hoja #C5CFBC [P]: lo que hace reconocible la embaúba de lejos.</summary>
        public static readonly Color EmbaubaLeafUnder = Palette.Hex(0xC5CFBC);

        private static readonly Color HomeSkyTop = Palette.Hex(0xA9DDF0);
        private static readonly Color HomeSkyLow = Palette.Hex(0xD5EBCF);

        private Sprite? _homeWindow;

        /// <summary>Si ya se creó la ventanita provisional.</summary>
        public bool HomeWindowBuilt => _homeWindow != null;

        /// <summary>
        /// La ventanita que se pinta: el PNG de <see cref="HomeWindowSlot"/> si existe; si no, la de código con
        /// <see cref="MapArtRule.ShowProvisional"/> (en todos los builds, D-052); si no, null.
        /// </summary>
        public Sprite? HomeWindow => HomeWindowFor(ArtSlots.Find(HomeWindowSlot), MapArtRule.ShowProvisional);

        /// <summary>
        /// La regla de <see cref="HomeWindow"/> con el PNG de la ranura (<paramref name="slotPng"/>) y la compuerta
        /// (<paramref name="showProvisional"/>) explícitos: el PNG manda y, con él, la provisional no se crea.
        /// </summary>
        public Sprite? HomeWindowFor(Sprite? slotPng, bool showProvisional)
        {
            if (slotPng != null)
            {
                return slotPng;
            }

            return showProvisional ? HomeWindowProcedural : null;
        }

        /// <summary>
        /// La ventanita provisional por código (<see cref="HomeWindowDp"/> × <see cref="HomeWindowDp"/> dp, pivote al centro,
        /// borde de 9 cortes de <see cref="HomeWindowBorderDp"/> dp), creada la primera vez que se pide. No mira la ranura ni la
        /// compuerta: para eso, <see cref="HomeWindow"/>.
        /// </summary>
        public Sprite HomeWindowProcedural => _homeWindow != null ? _homeWindow : (_homeWindow = BuildHomeWindow());

        // Todo en px con el origen abajo a la izquierda (k px por dp): primero el cielo y la embaúba, que pueden pasar por debajo
        // del marco; luego el marco opaco encima y, al final, el recorte del borde redondeado de fuera.
        private Sprite BuildHomeWindow()
        {
            const float k = UiPxPerDp;
            const float size = HomeWindowDp;
            const float border = HomeWindowBorderDp;
            const float outerRadius = 9f;
            const float innerRadius = 4.5f;
            int s = Mathf.RoundToInt(size * k);
            var pc = new PixelCanvas(s, s);

            // Cielo de la mañana dentro del marco, más claro y verdoso hacia la orilla.
            for (int y = 0; y < s; y++)
            {
                float t = Mathf.Clamp01((y + 0.5f) / s);
                Color sky = Color.Lerp(HomeSkyLow, HomeSkyTop, t);
                for (int x = 0; x < s; x++)
                {
                    pc.Set(x, y, sky);
                }
            }

            // Hojas palmeadas que cuelgan de la rama a los dos lados de la cabeza (donde el retrato no las tapa), con los lóbulos
            // de fuera del envés plateado.
            DrawEmbaubaLeaf(pc, 10f * k, 41f * k, -0.45f, 16f * k, k, 701);
            DrawEmbaubaLeaf(pc, 46f * k, 43f * k, 0.45f, 15f * k, k, 702);

            // La rama: tronco claro que cruza en diagonal suave por arriba, con su sombra y los anillos de las cicatrices.
            var a = new Vector2(-2f * k, 42f * k);
            var b = new Vector2(58f * k, 47f * k);
            pc.FillCapsule(a.x, a.y - (1.2f * k), b.x, b.y - (1.2f * k), 3.1f * k, ColorMath.Darken(EmbaubaRing, 0.25f));
            pc.FillCapsule(a.x, a.y, b.x, b.y, 2.8f * k, EmbaubaBark);
            pc.FillCapsule(a.x, a.y + (1.1f * k), b.x, b.y + (1.1f * k), 0.9f * k, ColorMath.Lighten(EmbaubaBark, 0.35f));
            for (int i = 1; i <= 5; i++)
            {
                float u = i / 6f;
                Vector2 p = Vector2.Lerp(a, b, u);
                pc.FillCapsule(p.x - (0.3f * k), p.y - (2.4f * k), p.x + (0.3f * k), p.y + (2.4f * k), 0.35f * k, EmbaubaRing);
            }

            // Marco de madera (encima de todo lo de dentro): veta, bisel claro arriba y ranura oscura junto al hueco. Fuera del
            // borde redondeado también se pinta madera y luego se recorta el alfa: así el filtrado del borde no saca la rama o
            // el cielo de debajo como un filo claro.
            Color wood = Palette.WoodDark;
            for (int y = 0; y < s; y++)
            {
                float yd = (y + 0.5f) / k;
                for (int x = 0; x < s; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float outer = PixelCanvas.RoundedRectDistance(xd, yd, 0.5f, 0.5f, size - 0.5f, size - 0.5f, outerRadius);
                    float inner = PixelCanvas.RoundedRectDistance(xd, yd, border, border, size - border, size - border, innerRadius);
                    float ring = PixelCanvas.Coverage(-inner * k);
                    if (ring > 0f)
                    {
                        float grain = ArtNoise.Value2(xd / 9f, yd * 0.6f, 233);
                        float line = Mathf.Abs(Mathf.Sin((xd * 0.9f) + (yd * 0.35f) + (grain * 4f)));
                        Color c = ColorMath.Darken(wood, line < 0.15f ? 0.12f : 0f);
                        float fromOuter = -outer;
                        if (fromOuter < 1.4f && yd > size * 0.5f)
                        {
                            c = Color.Lerp(c, Palette.FamilyWood.Light, 0.65f * (1f - (fromOuter / 1.4f))); // bisel superior
                        }
                        else if (inner > -1.2f && inner < 0.6f)
                        {
                            c = Color.Lerp(c, Palette.Groove, 0.7f); // ranura junto al hueco
                        }

                        c.a = 1f;
                        pc.Blend(x, y, c, ring);
                    }

                    pc.MaskAlpha(x, y, PixelCanvas.Coverage(outer * k));
                }
            }

            float slice = border * k;
            return MakeSprite(pc, "home_window", new Vector2(0.5f, 0.5f), new Vector4(slice, slice, slice, slice), k);
        }

        // Hoja palmeada de embaúba: 7 lóbulos alargados en abanico desde el pecíolo (px), girados angle radianes desde la
        // vertical hacia abajo, con el envés plateado en los lóbulos de los lados (las hojas se dan la vuelta con el viento).
        private static void DrawEmbaubaLeaf(PixelCanvas pc, float cx, float cy, float angle, float length, float k, int seed)
        {
            const int lobes = 7;
            const float spread = 1.9f; // radianes del abanico
            for (int i = 0; i < lobes; i++)
            {
                float t = (i / (float)(lobes - 1)) - 0.5f;
                float a = angle + (t * spread);
                var dir = new Vector2(Mathf.Sin(a), -Mathf.Cos(a));
                float len = length * (1f - (0.38f * Mathf.Abs(t) * 2f));
                bool under = i == 0 || i == lobes - 1;
                Color dark = under ? ColorMath.Darken(EmbaubaLeafUnder, 0.18f) : ColorMath.Darken(EmbaubaLeaf, 0.2f);
                Color light = under ? EmbaubaLeafUnder : ColorMath.Lighten(EmbaubaLeaf, 0.18f);
                // Ovate y estrecha, no Lanceolate: la forma Lanceolate de DrawLeaf da NaN en la punta (u = 1: sin(π) < 0 en float).
                DrawLeaf(pc, new Vector2(cx, cy), dir, len, len * 0.32f, 0.05f * t, LeafShape.Ovate, dark, light, k, seed + i);
            }

            pc.FillCircle(cx, cy, 1.1f * k, ColorMath.Darken(EmbaubaLeaf, 0.3f));
        }
    }
}
