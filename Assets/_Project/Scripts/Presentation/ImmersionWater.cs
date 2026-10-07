#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Agua de inmersión de una fila en Sumergir y teñir (guía visual v2, 6.11; revisiones visuales del 2026-10-07): agua
    /// del mundo con el borde superior ondulado (amplitud 0,06 u, longitud de onda 1,3 u, fase a 2,5 u/s), menisco claro de
    /// 1,5 dp (<c>destello</c> al 70 %), una banda superior de 0,35 u con el agua +12 L* (la luz del cielo en la
    /// superficie), degradado vertical del agua base al 55 % arriba a su tono oscuro al 62 % abajo, para que se vea la cuia
    /// a través (al 70 % y casi opaca, las cuias parecían medio enterradas en arena), y 3 líneas de brillo que se deslizan.
    /// El rectángulo tiene el pivote abajo en el centro (la cara de la repisa); la intro solo anima su alto y su reloj.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ImmersionWater : MaskableGraphic
    {
        /// <summary>Amplitud de la onda del borde superior (u).</summary>
        public const float WaveAmplitudeU = 0.06f;

        /// <summary>Longitud de onda del borde superior (u).</summary>
        public const float WaveLengthU = 1.3f;

        /// <summary>Velocidad de fase de la onda (u/s).</summary>
        public const float WaveSpeedU = 2.5f;

        /// <summary>Opacidad del agua de inmersión arriba (revisión visual del 2026-10-07: 0,55 en lugar de 0,75).</summary>
        public const float TopAlpha = 0.55f;

        /// <summary>Opacidad del agua de inmersión abajo.</summary>
        public const float BottomAlpha = 0.62f;

        /// <summary>Alto de la banda clara bajo el menisco (u).</summary>
        public const float BandU = 0.35f;

        private const float BandDeltaL = 12f;
        private const int Columns = 32;
        private const int Glints = 3;
        private const float MeniscusDp = 1.5f;

        private Color _top = new Color(0xB0 / 255f, 0x8A / 255f, 0x5A / 255f, TopAlpha);
        private Color _band = new Color(0xCE / 255f, 0xA7 / 255f, 0x76 / 255f, TopAlpha);
        private Color _bottom = new Color(0x6B / 255f, 0x56 / 255f, 0x40 / 255f, BottomAlpha);
        private float _unit = 24f;
        private float _timeMs;

        /// <summary>Configura el gráfico (sin raycast).</summary>
        public void Setup()
        {
            raycastTarget = false;
        }

        /// <summary>
        /// Agua base del mundo (07 §2.1): arriba al 55 %, con la banda superior +12 L*, y abajo su tono oscuro al 62 % (en el
        /// Estuario, #B08A5A → #6B5640, la paleta de río corregida).
        /// </summary>
        public void SetWater(Color water)
        {
            var top = new Color(water.r, water.g, water.b, TopAlpha);
            Color lit = ColorMath.AddLabLightness(water, BandDeltaL);
            var band = new Color(lit.r, lit.g, lit.b, TopAlpha);
            Color dark = water == Palette.WaterWhite ? Palette.RiverFar : ColorMath.Darken(water, 0.38f);
            var bottom = new Color(dark.r, dark.g, dark.b, BottomAlpha);
            if (top != _top || bottom != _bottom || band != _band)
            {
                _top = top;
                _band = band;
                _bottom = bottom;
                SetVerticesDirty();
            }
        }

        /// <summary>Unidad u en dp (para la onda).</summary>
        public void SetUnit(float unitDp)
        {
            if (!Mathf.Approximately(unitDp, _unit))
            {
                _unit = Mathf.Max(1f, unitDp);
                SetVerticesDirty();
            }
        }

        /// <summary>Reloj de la onda y de los destellos (ms desde el inicio de la intro).</summary>
        public void SetTime(float ms)
        {
            if (!Mathf.Approximately(ms, _timeMs))
            {
                _timeMs = ms;
                SetVerticesDirty();
            }
        }

        /// <inheritdoc />
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            float h = r.height;
            if (h < 0.5f || r.width < 1f)
            {
                return;
            }

            float amp = Mathf.Min(WaveAmplitudeU * _unit, h * 0.5f);
            float t = _timeMs / 1000f;

            // Cuerpo: columnas con el borde de arriba ondulado y el degradado vertical en los colores de vértice; bajo el
            // menisco, la banda clara (+12 L*) de BandU que se funde con el agua hacia abajo.
            float bandH = Mathf.Min(BandU * _unit, h * 0.45f);
            for (int i = 0; i < Columns; i++)
            {
                float x0 = Mathf.Lerp(r.xMin, r.xMax, i / (float)Columns);
                float x1 = Mathf.Lerp(r.xMin, r.xMax, (i + 1) / (float)Columns);
                float y0 = r.yMin + Top(x0, h, amp, t);
                float y1 = r.yMin + Top(x1, h, amp, t);
                float e0 = EndFade(x0, r);
                float e1 = EndFade(x1, r);
                float m0 = Mathf.Max(r.yMin, y0 - MeniscusDp - bandH);
                float m1 = Mathf.Max(r.yMin, y1 - MeniscusDp - bandH);
                int b = vh.currentVertCount;
                vh.AddVert(new Vector3(x0, r.yMin), Fade(_bottom, e0), Vector4.zero);
                vh.AddVert(new Vector3(x0, m0), Fade(_top, e0), Vector4.zero);
                vh.AddVert(new Vector3(x1, m1), Fade(_top, e1), Vector4.zero);
                vh.AddVert(new Vector3(x1, r.yMin), Fade(_bottom, e1), Vector4.zero);
                vh.AddTriangle(b, b + 1, b + 2);
                vh.AddTriangle(b + 2, b + 3, b);

                b = vh.currentVertCount;
                vh.AddVert(new Vector3(x0, m0), Fade(_top, e0), Vector4.zero);
                vh.AddVert(new Vector3(x0, y0 - MeniscusDp), Fade(_band, e0), Vector4.zero);
                vh.AddVert(new Vector3(x1, y1 - MeniscusDp), Fade(_band, e1), Vector4.zero);
                vh.AddVert(new Vector3(x1, m1), Fade(_top, e1), Vector4.zero);
                vh.AddTriangle(b, b + 1, b + 2);
                vh.AddTriangle(b + 2, b + 3, b);

                // Menisco claro de 1,5 dp en el borde.
                Color men = ColorMath.WithAlpha(Palette.Glint, 0.7f);
                b = vh.currentVertCount;
                vh.AddVert(new Vector3(x0, y0 - MeniscusDp), Fade(men, e0), Vector4.zero);
                vh.AddVert(new Vector3(x0, y0), Fade(men, e0), Vector4.zero);
                vh.AddVert(new Vector3(x1, y1), Fade(men, e1), Vector4.zero);
                vh.AddVert(new Vector3(x1, y1 - MeniscusDp), Fade(men, e1), Vector4.zero);
                vh.AddTriangle(b, b + 1, b + 2);
                vh.AddTriangle(b + 2, b + 3, b);
            }

            // 3 líneas de brillo de 1 dp y 18-30 dp de largo, con las puntas transparentes, que se deslizan a distintas
            // profundidades bajo el menisco (guía 6.11: «menisco claro y 3 brillos»).
            if (h > 6f)
            {
                for (int g = 0; g < Glints; g++)
                {
                    float speed = 14f + (g * 6f); // dp/s
                    float len = 18f + (g * 6f);
                    float phase = Mathf.Repeat((g * 0.37f) + (t * speed / r.width), 1f);
                    float cx = Mathf.Lerp(r.xMin + (len * 0.5f) + 4f, r.xMax - (len * 0.5f) - 4f, phase);
                    float depth = 3f + (g * 0.22f * Mathf.Max(0f, h - 8f));
                    float cy = r.yMin + Mathf.Max(2f, Top(cx, h, amp, t) - MeniscusDp - depth);
                    Color line = ColorMath.WithAlpha(Palette.Glint, 0.60f - (g * 0.10f));
                    Color tip = ColorMath.WithAlpha(Palette.Glint, 0f);
                    int b = vh.currentVertCount;
                    vh.AddVert(new Vector3(cx - (len * 0.5f), cy), tip, Vector4.zero);
                    vh.AddVert(new Vector3(cx, cy + 0.5f), line, Vector4.zero);
                    vh.AddVert(new Vector3(cx + (len * 0.5f), cy), tip, Vector4.zero);
                    vh.AddVert(new Vector3(cx, cy - 0.5f), line, Vector4.zero);
                    vh.AddTriangle(b, b + 1, b + 2);
                    vh.AddTriangle(b + 2, b + 3, b);
                }
            }
        }

        // Altura del borde superior en x (dp desde abajo): onda que viaja a 2,5 u/s.
        private float Top(float x, float h, float amp, float t)
        {
            float phase = ((x / _unit) - (WaveSpeedU * t)) * (2f * Mathf.PI / WaveLengthU);
            return Mathf.Max(MeniscusDp, h - amp + (amp * Mathf.Sin(phase)));
        }

        private static float EndFade(float x, Rect r)
        {
            return Mathf.Clamp01(Mathf.Min(x - r.xMin, r.xMax - x) / 4f);
        }

        private static Color32 Fade(Color c, float f)
        {
            return new Color(c.r, c.g, c.b, c.a * f);
        }
    }
}
