#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Crecida y la Canasta (tarea B1b de la tanda 5; 02 §5 y §6; 06 §4.3-§4.4; 07 §7.4), en la línea de
    /// D-003 (madera, vidrio y luz de juego; sin papel ni acuarela) y sin tintes de fruta (07 §3, regla 6):
    /// <list type="bullet">
    /// <item><description>el icono grande de la tarjeta de la Crecida del 25 (04 §4; <see cref="CardHeroIcon"/> con la clave
    /// <see cref="StreakHeroIconKey"/>): el medidor vertical del río con el agua a media altura, sus muescas de los hitos 3 y 6 y la
    /// Hoja escudo (la hoja redonda de la vitória-régia, glosario §6.3) al pie;</description></item>
    /// <item><description>el icono de la Canasta (<see cref="IconBasket"/>): la cesta tejida de fibra de la Canasta (glosario §7.1:
    /// «contenido visible»), con dos hojas y una semilla asomando, la que llevan el chip del hito 6 y la barra «Canasta · {sitio}
    /// k/10» de la victoria.</description></item>
    /// </list>
    /// Se generan la primera vez que se piden. La barra del HUD y las barras de la victoria no tienen sprite propio: usan
    /// <see cref="RoundedRect"/> teñido y <see cref="LilyPad"/> para la Hoja escudo. Lo sustituye el arte del artista.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>
        /// Clave del icono grande de la tarjeta de la Crecida (<c>win_streak</c>, el ID del sistema del glosario §6.3): la misma que
        /// <c>AF.GameFlow.LevelCardInfo.StreakIntro</c>.
        /// </summary>
        public const string StreakHeroIconKey = "win_streak";

        /// <summary>Lado del icono de la Canasta (dp).</summary>
        public const float BasketIconDp = 32f;

        private Sprite? _iconBasket;

        /// <summary>Icono de la Canasta, de <see cref="BasketIconDp"/> dp (la cesta con hojas y una semilla asomando).</summary>
        public Sprite IconBasket => _iconBasket != null ? _iconBasket : (_iconBasket = BuildIconBasket());

        private Sprite BuildIconBasket()
        {
            const float k = UiPxPerDp;
            int s = Mathf.RoundToInt(BasketIconDp * k);
            var pc = new PixelCanvas(s, s);
            float u = s / 32f; // 1 dp del icono en px

            // Dos hojas y una semilla asomando por la boca (el contenido visible: 06 §4.4, sin azar).
            pc.FillEllipseSoft(11f * u, 22f * u, 4.2f * u, 7f * u, 0.8f * u, Palette.LeafDark);
            pc.FillEllipseSoft(11f * u, 22.5f * u, 3f * u, 5.6f * u, 0.8f * u, Palette.LeafMid);
            pc.FillEllipseSoft(21f * u, 23f * u, 4f * u, 6.5f * u, 0.8f * u, Palette.LeafDark);
            pc.FillEllipseSoft(21f * u, 23.5f * u, 2.8f * u, 5.1f * u, 0.8f * u, Palette.LeafLight);
            pc.FillCircle(16f * u, 20f * u, 3.6f * u, Palette.DockShadow);
            pc.FillCircle(16f * u, 20f * u, 2.8f * u, SeedAmber);

            // Cuerpo: trapecio tejido de 18 dp abajo a 26 arriba, entre y = 3 y y = 17.
            for (int y = 0; y < s; y++)
            {
                float yd = (y + 0.5f) / u;
                for (int x = 0; x < s; x++)
                {
                    float xd = (x + 0.5f) / u;
                    float t = Mathf.Clamp01((yd - 3f) / 14f);
                    float half = Mathf.Lerp(9f, 13f, t);
                    float d = Mathf.Max(Mathf.Abs(xd - 16f) - half, Mathf.Max(3f - yd, yd - 17f));
                    float cov = PixelCanvas.Coverage(d * u);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, Palette.FiberShadow, cov);
                    float inside = PixelCanvas.Coverage((d + 1f) * u);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    int band = Mathf.FloorToInt((yd - 3f) / 3f);
                    float weave = Mathf.Sin(((xd - 16f) / half) * 6f + (band * 1.6f));
                    Color c = Color.Lerp(Palette.FiberBase, Palette.FiberLight, 0.5f + (0.5f * weave));
                    if (((yd - 3f) % 3f) < 0.6f)
                    {
                        c = Palette.FiberShadow;
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, inside);
                }
            }

            // Borde trenzado.
            pc.FillRoundedRect(2.5f * u, 15.5f * u, 29.5f * u, 19f * u, 1.6f * u, Palette.FiberShadow);
            pc.FillRoundedRect(3.4f * u, 16.4f * u, 28.6f * u, 18.1f * u, 0.9f * u, Palette.FiberLight);
            return MakeSprite(pc, "icon_basket", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        // Medidor de la Crecida (tarjeta del 25) en un lienzo cuadrado de s px (k = s / 96; y hacia arriba): el tubo de vidrio vertical
        // del borde derecho del HUD (07 §7.4), con el agua hasta el hito 6 y la muesca del 3, y la Hoja escudo al pie (glosario §6.3).
        private static void DrawStreakGlyph(PixelCanvas pc, int s)
        {
            float k = s / 96f;
            float x0 = 38f * k;
            float x1 = 58f * k;
            float y0 = 20f * k;
            float y1 = 90f * k;

            // Tubo: canto oscuro, vidrio claro y el agua de abajo arriba (60 %: el hito 6).
            pc.FillRoundedRect(x0 - (2.5f * k), y0 - (2.5f * k), x1 + (2.5f * k), y1 + (2.5f * k), 12f * k, CardGlassEdge);
            pc.FillRoundedRect(x0, y0, x1, y1, 10f * k, CardGlass);
            float water = y0 + ((y1 - y0) * 0.6f);
            pc.FillRoundedRect(x0, y0, x1, water, 10f * k, Palette.FamilyWater.Deep);
            pc.FillRoundedRect(x0 + (2f * k), y0 + (2f * k), x1 - (6f * k), water - (2f * k), 6f * k, Palette.FamilyWater.Base);
            pc.FillRoundedRect(x0 + (3f * k), water - (3f * k), x1 - (3f * k), water, 1.5f * k, Palette.FamilyWater.Light);

            // Muescas de los hitos 3 y 6 (05 §2.4), a la derecha del tubo.
            for (int i = 1; i <= 2; i++)
            {
                float y = y0 + ((y1 - y0) * (0.3f * i));
                pc.FillRoundedRect(x1 + (3f * k), y - (1.6f * k), x1 + (11f * k), y + (1.6f * k), 1.6f * k, CardGlassEdge);
            }

            // La Hoja escudo al pie: la hoja redonda de borde levantado.
            float cx = 48f * k;
            float cy = 14f * k;
            pc.FillEllipseSoft(cx, cy, 30f * k, 11f * k, 1.2f * k, Palette.LeafDark);
            pc.FillEllipseSoft(cx, cy + (1f * k), 27f * k, 9f * k, 1.2f * k, Palette.LeafMid);
            pc.FillEllipseSoft(cx - (6f * k), cy + (3f * k), 12f * k, 3f * k, 1.2f * k, Palette.LeafLight);
        }
    }
}
