#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Hoja (M1; 02 §9.1 M1; propuesta de D-007 b.2 M1; D-003 «Selva brillante premium»): la hoja
    /// de palma cerrada, plegada y tendida sobre la boca (<see cref="HojaPalm"/>), la misma hoja ya abierta y plana que cae
    /// girando al agua (<see cref="HojaFlat"/>) y el medallón pulido sobre el que va la fruta llave (<see cref="HojaBadge"/>).
    /// La silueta es alargada, con puntas y pliegues de palma, para no confundirse con la hoja redonda de Victoria amazonica de
    /// la Hoja escudo (glosario §6.3; 02 §9.1).
    /// </summary>
    /// <remarks>
    /// <para>Perezosos, como el resto del arte de mecánicas: se generan la primera vez que se piden (el overlay los pide al
    /// unirse a la cuia, nunca en un paso) y tras <see cref="Dispose"/> se vuelven a generar. A <see cref="MechanicPxPerU"/>
    /// px por u.</para>
    /// <para>Color (direccion-visual-v2 §2.4, regla 1): el haz va de <see cref="HojaFaceDark"/> a <see cref="HojaFaceLight"/>,
    /// el envés es <see cref="HojaFold"/> y el medallón <see cref="HojaBadgeColor"/>, todos a ΔE00 ≥ 10 de los 14 tintes (el
    /// más cercano, Lulo, a 13,1 del haz claro). El brillo ancho se limita a una mezcla del 8 % con <c>hoja_brillo</c> (ΔE00
    /// ≥ 10 de Lulo); solo las líneas de 2 dp o menos (aristas de los pliegues, filo del nervio) llevan <c>hoja_brillo</c> a
    /// más alfa, como permite la excepción de la regla.</para>
    /// <para>Se aparta de «reutiliza <c>DrawFrond</c>» (T11) por la silueta, como ya hizo T03 (ver
    /// <c>ProceduralArt.Mechanics</c>): la fronda pinnada de açaí es un raquis con foliolos sueltos que cuelgan, deja la boca a
    /// la vista entre los foliolos y se confunde con el follaje que enmarca la pantalla, mientras que b.2 M1 pide una hoja de
    /// palma alargada y plegada que tape la boca como una tapa.</para>
    /// <para>Tampoco reutiliza <c>DrawPalmLeaf</c> de T03, que sigue dibujando el icono de la Hoja (<c>MechanicIcon</c>) con la
    /// misma silueta (larga, en punta y con el envés oscuro debajo). Esa rutina encadena discos de dos colores planos (haz y
    /// envés) con trazos de pliegue encima: no da degradado de luz, ni caras de pliegue en luz y en sombra, ni brillo, y es la
    /// pieza más grande de la cuia (2,4 u), donde D-003 pide volumen y luz de juego. La hoja de palma la dibuja
    /// <c>DrawHojaPalm</c> píxel a píxel, cerrada (<see cref="HojaPalm"/>) y abierta (<see cref="HojaFlat"/>), y de esta última
    /// sale la silueta clara de <see cref="HojaGlow"/>. <c>DrawLeafShape</c> y <c>DrawLeaf</c> los reutiliza el Brote.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho del sprite de la hoja cerrada (u).</summary>
        public const float HojaPalmWidthU = 2.4f;

        /// <summary>Alto del sprite de la hoja cerrada (u).</summary>
        public const float HojaPalmHeightU = 1.0f;

        /// <summary>Altura del nervio central en el centro de la hoja cerrada (u desde el pie del sprite).</summary>
        public const float HojaPalmMidribU = 0.62f;

        /// <summary>Borde de abajo del envés en el centro de la hoja cerrada (u desde el pie del sprite).</summary>
        public const float HojaPalmLowU = 0.22f;

        /// <summary>Ancho del sprite de la hoja abierta y plana (u).</summary>
        public const float HojaFlatWidthU = 2.2f;

        /// <summary>Alto del sprite de la hoja abierta y plana (u).</summary>
        public const float HojaFlatHeightU = 0.62f;

        /// <summary>Lado del medallón de la fruta llave (u).</summary>
        public const float HojaBadgeU = 0.92f;

        /// <summary>Haz de la hoja en luz #3A8A55 [P]: ΔE00 13,1 de Lulo, el tinte más cercano.</summary>
        public static readonly Color HojaFaceLight = Palette.Hex(0x3A8A55);

        /// <summary>Haz de la hoja en sombra #1E6340 [P] (<c>selva_media</c> oscurecida): ΔE00 ≥ 25 de los tintes.</summary>
        public static readonly Color HojaFaceDark = Palette.Hex(0x1E6340);

        /// <summary>Envés plegado: <c>hoja_oscura</c> #145A35 (ΔE00 ≥ 32 de los tintes).</summary>
        public static readonly Color HojaFold = Palette.LeafDark;

        /// <summary>Medallón pulido de la fruta llave: verde perla #C7DDCB [P] (ΔE00 13,5 de Guanábana, el tinte más cercano).</summary>
        public static readonly Color HojaBadgeColor = Palette.Hex(0xC7DDCB);

        private Sprite? _hojaPalm;
        private Sprite? _hojaFlat;
        private Sprite? _hojaGlow;
        private Sprite? _hojaBadge;

        /// <summary>
        /// Hoja de palma cerrada (<see cref="HojaPalmWidthU"/> × <see cref="HojaPalmHeightU"/> u): tendida sobre la boca como
        /// una tapa, recta en el centro y con las puntas caídas hacia los hombros de la cuia, el envés oscuro asomando por debajo,
        /// pliegues de palma que convergen en las puntas, nervio central y un brillo arriba a la izquierda. Ya coloreada.
        /// </summary>
        internal Sprite HojaPalm => _hojaPalm != null ? _hojaPalm : (_hojaPalm = BuildHojaPalm());

        /// <summary>La hoja ya abierta y plana (<see cref="HojaFlatWidthU"/> × <see cref="HojaFlatHeightU"/> u), la que cae girando. Ya coloreada.</summary>
        internal Sprite HojaFlat => _hojaFlat != null ? _hojaFlat : (_hojaFlat = BuildHojaFlat());

        /// <summary>
        /// La hoja abierta en luz: la silueta de <see cref="HojaFlat"/> casi blanca, con los pliegues y el nervio apenas
        /// marcados (lo tiñe <see cref="Palette.Glint"/>). Va encima de la hoja que cae y la aclara mientras se desvanece en el
        /// agua, porque el tinte de una <c>Image</c> multiplica y no puede aclarar.
        /// </summary>
        internal Sprite HojaGlow => _hojaGlow != null ? _hojaGlow : (_hojaGlow = BuildHojaGlow());

        /// <summary>Medallón pulido redondo (blanco y grises con bisel y brillo; lo tiñe <see cref="HojaBadgeColor"/>).</summary>
        internal Sprite HojaBadge => _hojaBadge != null ? _hojaBadge : (_hojaBadge = BuildHojaBadge());

        private Sprite BuildHojaPalm()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(HojaPalmWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(HojaPalmHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);

            // Tendida sobre la boca como una tapa: recta en el centro y con las puntas caídas hacia los hombros.
            float half = 0.31f * MechanicPxPerU;
            float fold = (HojaPalmMidribU - HojaPalmLowU) * MechanicPxPerU - half;
            DrawHojaPalm(pc, w * 0.5f, HojaPalmMidribU * MechanicPxPerU, w - (8f * k), 0.34f * MechanicPxPerU, 2.2f, half, fold, 4, true, k);
            return MakeSprite(pc, "mech_hoja_palm", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildHojaFlat()
        {
            return MakeSprite(DrawHojaFlat(), "mech_hoja_flat", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildHojaGlow()
        {
            // La misma hoja abierta, casi blanca: el haz más claro (#3A8A55, luminancia ~0,42) queda en 1 y la nervadura baja
            // hasta 0,84, para que la luz siga leyéndose como hoja. Conserva el alfa (la silueta y su borde suavizado).
            PixelCanvas pc = DrawHojaFlat();
            for (int y = 0; y < pc.Height; y++)
            {
                for (int x = 0; x < pc.Width; x++)
                {
                    Color c = pc.Get(x, y);
                    if (c.a <= 0f)
                    {
                        continue;
                    }

                    float lum = (0.30f * c.r) + (0.59f * c.g) + (0.11f * c.b);
                    float v = Mathf.Lerp(0.84f, 1f, Mathf.Clamp01(lum / 0.42f));
                    pc.Set(x, y, new Color(v, v, v, c.a));
                }
            }

            return MakeSprite(pc, "mech_hoja_glow", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // La hoja ya abierta: casi recta, con las puntas apenas caídas.
        private static PixelCanvas DrawHojaFlat()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(HojaFlatWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(HojaFlatHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            DrawHojaPalm(pc, w * 0.5f, h * 0.55f, w - (8f * k), 6f * k, 2f, 0.21f * MechanicPxPerU, 4f * k, 4, false, k);
            return pc;
        }

        private Sprite BuildHojaBadge()
        {
            int s = Mathf.RoundToInt(HojaBadgeU * MechanicPxPerU);
            var pc = new PixelCanvas(s, s);
            float c = s * 0.5f;
            float r = (s * 0.5f) - 3f;

            // Sombra suave abajo, canto oscuro, cara con bisel (más clara arriba a la izquierda) y un brillo pequeño.
            pc.FillEllipseSoft(c + 1f, c - 2.5f, r + 1f, r + 0.5f, 2.5f, new Color(0f, 0f, 0f, 0.35f));
            pc.FillCircle(c, c, r, new Color(0.66f, 0.66f, 0.66f, 1f));
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) - c;
                    float dy = (y + 0.5f) - c;
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float cover = PixelCanvas.Coverage(d - (r - 2.2f));
                    if (cover <= 0f)
                    {
                        continue;
                    }

                    // Luz a 35° desde arriba a la izquierda: 1 en el canto alto, 0,84 en el bajo.
                    float lit = Mathf.Clamp01(0.5f + (((-dx * 0.57f) + (dy * 0.82f)) / (2f * r)));
                    float v = Mathf.Lerp(0.84f, 1f, lit);
                    pc.Blend(x, y, new Color(v, v, v, 1f), cover);
                }
            }

            pc.FillEllipseSoft(c - (r * 0.38f), c + (r * 0.42f), r * 0.26f, r * 0.13f, 1.5f, new Color(1f, 1f, 1f, 0.9f));
            return MakeSprite(pc, "mech_hoja_badge", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Hoja de palma de largo length (px) con el nervio central en (cx, cy) y las puntas caídas droop px (curva
        // y = cy − droop · |2t − 1|^droopPow, recta en el centro): por cada píxel, el punto más cercano del nervio da la distancia
        // d a lo largo de la normal (positiva arriba). Dentro del haz (|d| ≤ medio ancho, lleno en el centro y en punta en los
        // extremos) el color sale del degradado de luz (claro arriba), de los pliegues de palma (cada uno con su cara en luz y en
        // sombra, la arista del valle en nervadura y la cresta clara) y del nervio central; debajo asoma el envés plegado (fold px
        // en el centro) y todo lleva un contorno de nervadura. Con gloss, un brillo ancho arriba a la izquierda (luz a 35°, guía
        // 1.2), mezclado al 8 % como mucho.
        private static void DrawHojaPalm(PixelCanvas pc, float cx, float cy, float length, float droop, float droopPow, float halfWidth, float fold, int pleats, bool gloss, float k)
        {
            const int samples = 200;
            const float tipFraction = 0.17f; // tramo de cada extremo que se afila hasta la punta
            var pts = new Vector2[samples + 1];
            var nrm = new Vector2[samples + 1];
            var tan = new Vector2[samples + 1];
            var wid = new float[samples + 1];
            var fol = new float[samples + 1];
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                float m = (2f * t) - 1f; // −1 … 1
                float am = Mathf.Abs(m);
                pts[i] = new Vector2(cx + (m * length * 0.5f), cy - (droop * Mathf.Pow(am, droopPow)));
                float slope = -droop * droopPow * Mathf.Pow(Mathf.Max(am, 1e-4f), droopPow - 1f) * Mathf.Sign(m) * 2f; // dy/dt
                tan[i] = new Vector2(length, slope).normalized;
                nrm[i] = new Vector2(-tan[i].y, tan[i].x); // a la izquierda del avance: arriba
                float s = Mathf.Sin(Mathf.PI * t);
                float taper = Mathf.Pow(Mathf.Clamp01(Mathf.Min(t, 1f - t) / tipFraction), 0.85f);
                wid[i] = halfWidth * Mathf.Pow(s, 0.3f) * taper;
                fol[i] = fold * Mathf.Pow(s, 1.2f) * taper;
            }

            float outline = 1.4f * k;
            float reach = halfWidth + fold + outline + 3f;
            int lo = 0;
            for (int x = 0; x < pc.Width; x++)
            {
                float px = x + 0.5f;
                while (lo < samples && pts[lo].x < px - reach)
                {
                    lo++;
                }

                for (int y = 0; y < pc.Height; y++)
                {
                    var p = new Vector2(px, y + 0.5f);
                    int best = -1;
                    float bestD2 = float.MaxValue;
                    for (int i = lo; i <= samples && pts[i].x <= px + reach; i++)
                    {
                        float d2 = (p - pts[i]).sqrMagnitude;
                        if (d2 < bestD2)
                        {
                            bestD2 = d2;
                            best = i;
                        }
                    }

                    if (best < 0)
                    {
                        continue;
                    }

                    Vector2 rel = p - pts[best];
                    float dn = Vector2.Dot(rel, nrm[best]);
                    float along = Vector2.Dot(rel, tan[best]);
                    float hw = wid[best];
                    float hf = fol[best];
                    float outside = Mathf.Max(dn - hw, -(dn + hw + hf));
                    if ((best == 0 && along < 0f) || (best == samples && along > 0f))
                    {
                        outside = Mathf.Max(outside, Mathf.Abs(along));
                    }

                    // En las puntas el contorno se afila con la hoja: nada de agujas sueltas más allá de la punta.
                    float line = Mathf.Min(outline, 0.35f * (hw + hf));
                    float covAll = PixelCanvas.Coverage(outside - line);
                    if (covAll <= 0f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, Palette.Vein, covAll);
                    float covIn = PixelCanvas.Coverage(outside);
                    if (covIn <= 0f)
                    {
                        continue;
                    }

                    float t = best / (float)samples;

                    // Envés: más oscuro hacia el canto de abajo.
                    float under = hf > 0.01f ? Mathf.Clamp01((-(dn + hw)) / hf) : 1f;
                    pc.Blend(x, y, Color.Lerp(HojaFold, ColorMath.Darken(HojaFold, 0.25f), under), covIn);

                    // Haz, con el canto del pliegue nítido sobre el envés.
                    float covFace = covIn * PixelCanvas.Coverage(-(dn + hw));
                    if (covFace <= 0f)
                    {
                        continue;
                    }

                    float sN = hw > 0.01f ? Mathf.Clamp(dn / hw, -1f, 1f) : 0f; // −1 abajo … 1 arriba
                    Color col = Color.Lerp(HojaFaceDark, HojaFaceLight, Mathf.SmoothStep(0f, 1f, (sN + 1f) * 0.5f));

                    // Pliegues de palma: la cara de cada pliegue que mira a la luz, más clara; la otra, más oscura; en la
                    // arista del valle, nervadura, y en la cresta, un filo de hoja_brillo (≤ 1 dp).
                    float q = (sN + 1f) * 0.5f * pleats;
                    float wave = Mathf.Sin(q * 2f * Mathf.PI);
                    col = wave > 0f ? Color.Lerp(col, HojaFaceLight, 0.18f * wave) : Color.Lerp(col, HojaFaceDark, -0.28f * wave);
                    float pleatPx = (2f * hw) / pleats;
                    float valley = Mathf.Abs(q - Mathf.Round(q)) * pleatPx;
                    float crest = Mathf.Abs(q - (Mathf.Floor(q) + 0.5f)) * pleatPx;
                    bool central = Mathf.Abs(sN) < 0.25f;
                    float veinHalf = (central ? 1.1f : 0.6f) * k;
                    if (valley < veinHalf + 0.7f)
                    {
                        col = Color.Lerp(col, Palette.Vein, (central ? 0.85f : 0.45f) * PixelCanvas.Coverage(valley - veinHalf));
                    }
                    else if (crest < 0.9f * k && t > 0.06f && t < 0.94f)
                    {
                        col = Color.Lerp(col, Palette.LeafShine, 0.30f * PixelCanvas.Coverage(crest - (0.5f * k)));
                    }

                    // Filo claro del nervio central, justo encima (≤ 1 dp).
                    float ridge = Mathf.Abs(dn - (2.1f * k));
                    if (ridge < 0.8f * k && t > 0.08f && t < 0.92f)
                    {
                        col = Color.Lerp(col, Palette.LeafShine, 0.40f * PixelCanvas.Coverage(ridge - (0.4f * k)));
                    }

                    // Brillo ancho arriba a la izquierda (mezcla de ≤ 8 %: ΔE00 ≥ 10 de Lulo) y luz de borde fina arriba.
                    if (gloss)
                    {
                        float gs = Mathf.Clamp01(1f - (Mathf.Abs(sN - 0.55f) / 0.35f));
                        float gt = Mathf.Clamp01(1f - (Mathf.Abs(t - 0.32f) / 0.2f));
                        col = Color.Lerp(col, Palette.LeafShine, 0.08f * gs * gs * gt);
                    }

                    float rim = hw - dn;
                    if (rim < 0.9f * k && t > 0.1f && t < 0.6f)
                    {
                        col = Color.Lerp(col, Palette.LeafShine, 0.45f * PixelCanvas.Coverage(rim - (0.5f * k)));
                    }

                    col.a = 1f;
                    pc.Blend(x, y, col, covFace);
                }
            }
        }
    }
}
