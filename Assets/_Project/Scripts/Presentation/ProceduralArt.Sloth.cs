#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Retrato provisional del perezoso protagonista (D-013 y D-016; propuesta del personaje c.4, c.5 y c.7; tarea M1.5): la
    /// cabeza redonda de pelo gris pardo con el velo de algas arriba, la cara clara con el antifaz oscuro, los ojos pequeños con
    /// su brillo (abiertos) o en arco (cerrados, para el parpadeo y <c>sloth_sleep</c>), la nariz y la línea natural de la boca,
    /// y la garganta parda de <i>Bradypus variegatus</i>, con el contorno de tinta de la fauna (07 §6.1). Sin orejas redondas, sin
    /// labios ni dientes, sin ropa ni accesorios. Es el retrato de la placa del Dato del río (40 dp) y de la ventanita de casa
    /// (56 dp): se hornea a <see cref="SlothPortraitDp"/> y se pinta a cualquier lado entre <see cref="SlothPortraitMinDp"/> y
    /// ese. El rig con las poses es de TI4.
    /// </summary>
    /// <remarks>
    /// <para>El PNG de la ranura <see cref="SlothPortraitSlot"/> (512 × 512, del artista) manda: con él no se crea nada y vale
    /// para los ojos abiertos y cerrados. Sin él, el de código sale con <see cref="MapArtRule.ShowProvisional"/>, en todos los
    /// builds, release incluido (D-052).</para>
    /// <para>Perezoso: las dos texturas se crean la primera vez que se piden (<see cref="SlothPortraitBuilt"/>), nunca al
    /// construir <see cref="ProceduralArt"/>. Tras <see cref="Dispose"/> se vuelven a generar (el == de Unity).</para>
    /// <para>Paleta orientativa [P] de la propuesta del personaje (c.4); la garganta parda (#6E5440) es [P] de esta tarea.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ranura del retrato final (glosario §11: <c>fauna_&lt;id&gt;_retrato</c> de <i>Bradypus variegatus</i>).</summary>
        public const string SlothPortraitSlot = ArtSlots.FaunaPrefix + "bradypus_variegatus" + ArtSlots.FaunaPortraitSuffix;

        /// <summary>Lado del retrato horneado (dp): el de la ventanita de casa, el mayor de los dos usos.</summary>
        public const float SlothPortraitDp = 56f;

        /// <summary>Lado mínimo al que se pinta (dp): el de la placa del Dato del río.</summary>
        public const float SlothPortraitMinDp = 40f;

        /// <summary>Pelaje base gris pardo #9A8F7A (c.4) [P].</summary>
        public static readonly Color SlothFur = Palette.Hex(0x9A8F7A);

        /// <summary>Velo de algas del lomo y la coronilla #8C9C6A (c.4) [P].</summary>
        public static readonly Color SlothAlgae = Palette.Hex(0x8C9C6A);

        /// <summary>Cara #E3D8C2 (c.4) [P].</summary>
        public static readonly Color SlothFace = Palette.Hex(0xE3D8C2);

        /// <summary>Antifaz #4B3A2C (c.4) [P].</summary>
        public static readonly Color SlothMask = Palette.Hex(0x4B3A2C);

        /// <summary>Garganta parda de la especie #6E5440 [P].</summary>
        public static readonly Color SlothThroat = Palette.Hex(0x6E5440);

        // Nariz y contorno #2B2118 (c.4), el contorno al 80 % como la fauna (07 §6.1).
        private static readonly Color SlothInk = new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 1f);
        private static readonly Color SlothOutline = new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 0.8f);

        // Hueso claro de las garras #CDBFA3 (c.4): el párpado cerrado sobre el antifaz.
        private static readonly Color SlothLid = Palette.Hex(0xCDBFA3);

        private Sprite? _slothOpen;
        private Sprite? _slothClosed;

        /// <summary>Si ya se creó alguno de los dos retratos provisionales.</summary>
        public bool SlothPortraitBuilt => _slothOpen != null || _slothClosed != null;

        /// <summary>
        /// El retrato que se pinta: el PNG de <see cref="SlothPortraitSlot"/> si existe; si no, el de código con
        /// <see cref="MapArtRule.ShowProvisional"/> (en todos los builds, D-052); si no, null.
        /// </summary>
        public Sprite? SlothPortrait(bool eyesClosed)
        {
            return SlothPortrait(eyesClosed, ArtSlots.Find(SlothPortraitSlot), MapArtRule.ShowProvisional);
        }

        /// <summary>
        /// La regla de <see cref="SlothPortrait(bool)"/> con el PNG de la ranura (<paramref name="slotPng"/>) y la compuerta
        /// (<paramref name="showProvisional"/>) explícitos: el PNG manda y, con él, el provisional no se crea.
        /// </summary>
        public Sprite? SlothPortrait(bool eyesClosed, Sprite? slotPng, bool showProvisional)
        {
            if (slotPng != null)
            {
                return slotPng;
            }

            return showProvisional ? SlothPortraitProcedural(eyesClosed) : null;
        }

        /// <summary>
        /// El retrato provisional por código (<see cref="SlothPortraitDp"/> × <see cref="SlothPortraitDp"/> dp, pivote al centro),
        /// creado la primera vez que se pide. No mira la ranura ni la compuerta: para eso, <see cref="SlothPortrait(bool)"/>.
        /// </summary>
        public Sprite SlothPortraitProcedural(bool eyesClosed)
        {
            if (eyesClosed)
            {
                return _slothClosed != null ? _slothClosed : (_slothClosed = BuildSlothPortrait(true));
            }

            return _slothOpen != null ? _slothOpen : (_slothOpen = BuildSlothPortrait(false));
        }

        // Todo en dp con el origen abajo a la izquierda; el lienzo, a UiPxPerDp. Las distancias de las elipses, en dp
        // (aproximadas por debajo: (q − 1) · min(rx, ry)), se pasan a px para la cobertura.
        private Sprite BuildSlothPortrait(bool eyesClosed)
        {
            const float k = UiPxPerDp;
            const float size = SlothPortraitDp;
            int s = Mathf.RoundToInt(size * k);
            var pc = new PixelCanvas(s, s);
            float outline = 2f; // px: el contorno de 2 px de la fauna (c.4)
            Color furLight = ColorMath.Lighten(SlothFur, 0.16f);
            Color furDark = ColorMath.Darken(SlothFur, 0.22f);

            for (int y = 0; y < s; y++)
            {
                float yd = (y + 0.5f) / k;
                for (int x = 0; x < s; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float d = SlothSilhouette(xd, yd) * k; // px, negativa dentro
                    if (d > outline + 1f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, SlothOutline, PixelCanvas.Coverage(d - outline));
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    // Pelo: más oscuro abajo y a la derecha, más claro arriba a la izquierda (luz de juego a 35°); el velo de
                    // algas en la coronilla y los hombros; mechones con un ruido suave de baja frecuencia.
                    float light = Mathf.Clamp01(0.55f + (0.018f * (yd - 28f)) - (0.012f * (xd - 28f)));
                    Color c = Color.Lerp(furDark, furLight, light);
                    float algae = Mathf.Clamp01((yd - 38f) / 10f) * 0.3f;
                    algae += Mathf.Clamp01((14f - yd) / 8f) * Mathf.Clamp01((Mathf.Abs(xd - 28f) - 10f) / 8f) * 0.25f;
                    c = Color.Lerp(c, SlothAlgae, algae);
                    float strand = ArtNoise.Value2(xd * 0.9f, yd * 0.35f, 17) - 0.5f;
                    c = strand > 0f ? Color.Lerp(c, furLight, strand * 0.35f) : Color.Lerp(c, furDark, -strand * 0.35f);

                    // Garganta parda bajo la cara.
                    float throat = EllipseDp(xd, yd, 28f, 13.5f, 8.5f, 4.6f);
                    c = Color.Lerp(c, SlothThroat, PixelCanvas.SoftCoverage(throat * k, 2.5f));
                    c.a = 1f;
                    pc.Blend(x, y, c, cov);

                    // Cara clara, un poco más ancha abajo (el hocico corto).
                    float face = Mathf.Min(EllipseDp(xd, yd, 28f, 29f, 12.5f, 10.5f), EllipseDp(xd, yd, 28f, 24.5f, 7.5f, 5.5f));
                    float faceCov = PixelCanvas.SoftCoverage(face * k, 1.5f);
                    if (faceCov > 0f)
                    {
                        Color f = Color.Lerp(ColorMath.Darken(SlothFace, 0.08f), SlothFace, Mathf.Clamp01(0.5f + (0.06f * (yd - 26f))));
                        pc.Blend(x, y, f, faceCov);
                    }

                    // Antifaz: dos manchas alargadas que bajan hacia atrás desde los ojos.
                    float mask = Mathf.Min(EllipseDp(xd, yd, 21.2f, 29.6f, 5.4f, 2.5f, 0.38f), EllipseDp(xd, yd, 34.8f, 29.6f, 5.4f, 2.5f, -0.38f));
                    float maskCov = PixelCanvas.Coverage(mask * k);
                    if (maskCov > 0f)
                    {
                        pc.Blend(x, y, SlothMask, maskCov);
                    }
                }
            }

            // Ojos: abiertos, pequeños y negros con un brillo; cerrados, un arco claro sobre el antifaz.
            float eyeY = 30.2f * k;
            float leftX = 22.4f * k;
            float rightX = 33.6f * k;
            if (eyesClosed)
            {
                pc.StrokeArc(leftX, eyeY + (0.9f * k), 1.8f * k, 0.75f * k, 200f, 340f, SlothLid);
                pc.StrokeArc(rightX, eyeY + (0.9f * k), 1.8f * k, 0.75f * k, 200f, 340f, SlothLid);
            }
            else
            {
                pc.FillCircle(leftX, eyeY, 1.7f * k, SlothInk);
                pc.FillCircle(rightX, eyeY, 1.7f * k, SlothInk);
                pc.FillCircle(leftX - (0.5f * k), eyeY + (0.6f * k), 0.6f * k, new Color(1f, 1f, 1f, 0.95f));
                pc.FillCircle(rightX - (0.5f * k), eyeY + (0.6f * k), 0.6f * k, new Color(1f, 1f, 1f, 0.95f));
            }

            // Nariz ancha y oscura con su brillo, y la línea natural de la boca (sin labios ni dientes).
            pc.FillEllipseSoft(28f * k, 25.6f * k, 2.7f * k, 1.8f * k, 1.2f, SlothInk);
            pc.FillEllipseSoft(27.2f * k, 26.2f * k, 0.8f * k, 0.45f * k, 1f, new Color(1f, 1f, 1f, 0.45f));
            pc.StrokeArc(28f * k, 24.6f * k, 3.2f * k, 0.7f * k, 215f, 325f, SlothOutline);

            return MakeSprite(pc, eyesClosed ? "sloth_portrait_closed" : "sloth_portrait_open", new Vector2(0.5f, 0.5f), Vector4.zero, k);
        }

        // Silueta del retrato (dp, negativa dentro): la cabeza redonda con el borde de pelo en mechones y los hombros abajo,
        // unidos con suavidad.
        private static float SlothSilhouette(float x, float y)
        {
            float dx = x - 28f;
            float dy = y - 30f;
            float r = Mathf.Sqrt((dx * dx) + (dy * dy));
            float angle = Mathf.Atan2(dy, dx);
            float shaggy = 19f + (0.9f * Mathf.Sin(angle * 11f)) + (0.5f * Mathf.Sin((angle * 23f) + 1.3f));
            float head = r - shaggy;
            float shoulders = EllipseDp(x, y, 28f, 1f, 25f, 14f);
            float k = 4f;
            float h = Mathf.Clamp01(0.5f + (0.5f * (shoulders - head) / k));
            return Mathf.Lerp(shoulders, head, h) - (k * h * (1f - h));
        }

        // Distancia aproximada (dp, negativa dentro) a la elipse de centro (cx, cy) y semiejes (rx, ry), girada rot radianes.
        private static float EllipseDp(float x, float y, float cx, float cy, float rx, float ry, float rot = 0f)
        {
            float dx = x - cx;
            float dy = y - cy;
            if (rot != 0f)
            {
                float cos = Mathf.Cos(rot);
                float sin = Mathf.Sin(rot);
                float u = (dx * cos) + (dy * sin);
                dy = (-dx * sin) + (dy * cos);
                dx = u;
            }

            float q = Mathf.Sqrt(((dx * dx) / (rx * rx)) + ((dy * dy) / (ry * ry)));
            return (q - 1f) * Mathf.Min(rx, ry);
        }
    }
}
