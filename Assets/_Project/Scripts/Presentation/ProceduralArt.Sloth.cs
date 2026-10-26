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
    /// ese. El rig con las poses es de TI4. Desde el pulido del 2026-10-25 (D-052) la cabeza es la del rig
    /// (<c>DrawSlothHeadAt</c>), con los hombros y la garganta debajo: el mismo TiTino en la placa, la ventanita y el rig.
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

        // El retrato con el dibujo del rig: los hombros con su pelo, la garganta parda y la cabeza (DrawSlothHeadAt, la misma
        // de la pieza del rig) a 19 dp de radio, centrada en (28, 30) dp. Todo en unidades de la cabeza (la celda de 128).
        private Sprite BuildSlothPortrait(bool eyesClosed)
        {
            const float k = UiPxPerDp;
            const float size = SlothPortraitDp;
            int s = Mathf.RoundToInt(size * k);
            var pc = new PixelCanvas(s, s);
            float hk = 19f * k / SlothHeadRadiusPx; // px por unidad de la cabeza
            float cx = 28f * k;
            float cy = 30f * k;
            SlothTones fur = Tones(SlothFur, 0.36f, 0.2f);
            SlothTones algae = Tones(Color.Lerp(SlothFur, SlothAlgae, 0.75f), 0.34f, 0.16f);
            SlothTones throat = Tones(SlothThroat, 0.3f, 0.14f);
            float aa = 1f / hk;

            // Hombros: medio óvalo bajo la cabeza, con mechones que caen y el matiz de algas a parches en los costados.
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float ux = (x + 0.5f - cx) / hk;
                    float uy = (y + 0.5f - cy) / hk;
                    float a = Mathf.Atan2(uy + 70f, ux);
                    float body = EllipseDp(ux, uy, 0f, -76f, 64f, 38f) - SlothTuft(a, 16, 2.4f, 0.2f, 43, out float valley);
                    if (body > SlothInkPx + aa)
                    {
                        continue;
                    }

                    pc.Blend(x, y, SlothOutline, PixelCanvas.Coverage((body - SlothInkPx) * hk));
                    float cov = PixelCanvas.Coverage(body * hk);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float nx = ux / 66f;
                    float ny = (uy + 66f) / 44f;
                    float strand = SlothStrand(-uy, ux, 23, 8f, 16f);
                    float algaeAmount = 0.28f * SlothSmooth(18f, 52f, Mathf.Abs(ux)) * SlothAlgaePatch(ux, uy, 11);
                    Color col = SlothFurColor(fur, algae, nx, ny, strand, algaeAmount);
                    float throatD = EllipseDp(ux, uy, 0f, -46f, 22f, 13f) + (3f * (ArtNoise.Value2(ux * 0.3f, uy * 0.12f, 9) - 0.5f));
                    float t = PixelCanvas.SoftCoverage(throatD * hk, 9f * hk) * 0.85f;
                    if (t > 0f)
                    {
                        Color th = Shade(throat, SlothDiffuse(nx, ny, out _));
                        float m = 1f + (0.14f * strand);
                        th.r *= m;
                        th.g *= m;
                        th.b *= m;
                        col = Color.Lerp(col, th, t * 0.92f);
                    }

                    float groove = valley * Mathf.Clamp01(1f + (body / 8f)) * 0.3f;
                    col.r *= 1f - groove;
                    col.g *= 1f - groove;
                    col.b *= 1f - groove;
                    col.a = 1f;
                    pc.Blend(x, y, col, cov);
                }
            }

            DrawSlothHeadAt(pc, cx, cy, hk, eyesClosed, 0, 0, s, s);
            return MakeSprite(pc, eyesClosed ? "sloth_portrait_closed" : "sloth_portrait_open", new Vector2(0.5f, 0.5f), Vector4.zero, k);
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
