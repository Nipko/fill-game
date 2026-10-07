#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Utilidades de color: interpolación en OKLab para el teñido de Sumergir y teñir (02 §4.2, 07 §5.1: "para no
    /// pasar por grises"), aclarados, CIELAB D65 (L*, ΔL* y ΔE00 de la guía visual v2, docs/art/direccion-visual-v2.md
    /// 2.4-2.5), contraste WCAG 2.1 y composición en espacio lineal (la que hace la GPU con el proyecto en Linear).
    /// </summary>
    public static class ColorMath
    {
        // Blanco de referencia D65 (CIE 1931, 2°).
        private const float Xn = 0.95047f;
        private const float Yn = 1.00000f;
        private const float Zn = 1.08883f;

        /// <summary>Interpola dos colores sRGB en OKLab (alfa lineal).</summary>
        public static Color OkLabLerp(Color from, Color to, float t)
        {
            t = Mathf.Clamp01(t);
            Vector3 a = ToOkLab(from);
            Vector3 b = ToOkLab(to);
            Color c = FromOkLab(Vector3.LerpUnclamped(a, b, t));
            c.a = Mathf.LerpUnclamped(from.a, to.a, t);
            return c;
        }

        /// <summary>Cambia la luminosidad OKLab de <paramref name="c"/> en <paramref name="factor"/> (1,04 = +4 %).</summary>
        public static Color ScaleOkLightness(Color c, float factor)
        {
            Vector3 lab = ToOkLab(c);
            lab.x *= factor;
            Color r = FromOkLab(lab);
            r.a = c.a;
            return r;
        }

        /// <summary>Aclara un color hacia el blanco en la proporción <paramref name="amount"/> (conserva el alfa).</summary>
        public static Color Lighten(Color c, float amount)
        {
            return new Color(
                c.r + ((1f - c.r) * amount),
                c.g + ((1f - c.g) * amount),
                c.b + ((1f - c.b) * amount),
                c.a);
        }

        /// <summary>Oscurece un color hacia el negro en la proporción <paramref name="amount"/> (conserva el alfa).</summary>
        public static Color Darken(Color c, float amount)
        {
            float k = 1f - amount;
            return new Color(c.r * k, c.g * k, c.b * k, c.a);
        }

        /// <summary>Mismo color con otro alfa.</summary>
        public static Color WithAlpha(Color c, float alpha)
        {
            c.a = alpha;
            return c;
        }

        /// <summary>CIELAB D65 (L*, a*, b*) de un color sRGB.</summary>
        public static Vector3 ToLab(Color c)
        {
            float r = ToLinear(c.r);
            float g = ToLinear(c.g);
            float b = ToLinear(c.b);
            float x = ((0.4124564f * r) + (0.3575761f * g) + (0.1804375f * b)) / Xn;
            float y = ((0.2126729f * r) + (0.7151522f * g) + (0.0721750f * b)) / Yn;
            float z = ((0.0193339f * r) + (0.1191920f * g) + (0.9503041f * b)) / Zn;
            float fx = LabF(x);
            float fy = LabF(y);
            float fz = LabF(z);
            return new Vector3((116f * fy) - 16f, 500f * (fx - fy), 200f * (fy - fz));
        }

        /// <summary>Color sRGB (opaco, recortado a la gama) desde CIELAB D65.</summary>
        public static Color FromLab(Vector3 lab)
        {
            float fy = (lab.x + 16f) / 116f;
            float fx = fy + (lab.y / 500f);
            float fz = fy - (lab.z / 200f);
            float x = LabFInv(fx) * Xn;
            float y = LabFInv(fy) * Yn;
            float z = LabFInv(fz) * Zn;
            float r = (3.2404542f * x) - (1.5371385f * y) - (0.4985314f * z);
            float g = (-0.9692660f * x) + (1.8760108f * y) + (0.0415560f * z);
            float b = (0.0556434f * x) - (0.2040259f * y) + (1.0572252f * z);
            return new Color(ToSrgb(r), ToSrgb(g), ToSrgb(b), 1f);
        }

        /// <summary>L* (CIELAB) de un color sRGB.</summary>
        public static float LabLightness(Color c)
        {
            return ToLab(c).x;
        }

        /// <summary>El mismo color con <paramref name="deltaL"/> puntos más de L* (a* y b* iguales; conserva el alfa).</summary>
        public static Color AddLabLightness(Color c, float deltaL)
        {
            Vector3 lab = ToLab(c);
            lab.x = Mathf.Clamp(lab.x + deltaL, 0f, 100f);
            Color r = FromLab(lab);
            r.a = c.a;
            return r;
        }

        /// <summary>Diferencia de color CIEDE2000 (ΔE00) entre dos colores sRGB.</summary>
        public static float DeltaE00(Color c1, Color c2)
        {
            return DeltaE00Lab(ToLab(c1), ToLab(c2));
        }

        /// <summary>Diferencia de color CIEDE2000 (ΔE00) entre dos colores CIELAB (Sharma, Wu y Dalal, 2005).</summary>
        public static float DeltaE00Lab(Vector3 lab1, Vector3 lab2)
        {
            double l1 = lab1.x, a1 = lab1.y, b1 = lab1.z;
            double l2 = lab2.x, a2 = lab2.y, b2 = lab2.z;
            double c1s = System.Math.Sqrt((a1 * a1) + (b1 * b1));
            double c2s = System.Math.Sqrt((a2 * a2) + (b2 * b2));
            double cBar = (c1s + c2s) / 2.0;
            double cBar7 = System.Math.Pow(cBar, 7.0);
            double g = 0.5 * (1.0 - System.Math.Sqrt(cBar7 / (cBar7 + 6103515625.0))); // 25^7
            double a1p = (1.0 + g) * a1;
            double a2p = (1.0 + g) * a2;
            double c1p = System.Math.Sqrt((a1p * a1p) + (b1 * b1));
            double c2p = System.Math.Sqrt((a2p * a2p) + (b2 * b2));
            double h1p = HueDeg(b1, a1p);
            double h2p = HueDeg(b2, a2p);
            double dLp = l2 - l1;
            double dCp = c2p - c1p;
            double dhp;
            if (c1p * c2p == 0.0)
            {
                dhp = 0.0;
            }
            else if (System.Math.Abs(h2p - h1p) <= 180.0)
            {
                dhp = h2p - h1p;
            }
            else if (h2p - h1p > 180.0)
            {
                dhp = h2p - h1p - 360.0;
            }
            else
            {
                dhp = h2p - h1p + 360.0;
            }

            double dHp = 2.0 * System.Math.Sqrt(c1p * c2p) * System.Math.Sin(Deg(dhp / 2.0));
            double lBarP = (l1 + l2) / 2.0;
            double cBarP = (c1p + c2p) / 2.0;
            double hBarP;
            if (c1p * c2p == 0.0)
            {
                hBarP = h1p + h2p;
            }
            else if (System.Math.Abs(h1p - h2p) <= 180.0)
            {
                hBarP = (h1p + h2p) / 2.0;
            }
            else if (h1p + h2p < 360.0)
            {
                hBarP = (h1p + h2p + 360.0) / 2.0;
            }
            else
            {
                hBarP = (h1p + h2p - 360.0) / 2.0;
            }

            double t = 1.0
                - (0.17 * System.Math.Cos(Deg(hBarP - 30.0)))
                + (0.24 * System.Math.Cos(Deg(2.0 * hBarP)))
                + (0.32 * System.Math.Cos(Deg((3.0 * hBarP) + 6.0)))
                - (0.20 * System.Math.Cos(Deg((4.0 * hBarP) - 63.0)));
            double dTheta = 30.0 * System.Math.Exp(-System.Math.Pow((hBarP - 275.0) / 25.0, 2.0));
            double cBarP7 = System.Math.Pow(cBarP, 7.0);
            double rc = 2.0 * System.Math.Sqrt(cBarP7 / (cBarP7 + 6103515625.0));
            double l50 = (lBarP - 50.0) * (lBarP - 50.0);
            double sl = 1.0 + ((0.015 * l50) / System.Math.Sqrt(20.0 + l50));
            double sc = 1.0 + (0.045 * cBarP);
            double sh = 1.0 + (0.015 * cBarP * t);
            double rt = -System.Math.Sin(Deg(2.0 * dTheta)) * rc;
            double tl = dLp / sl;
            double tc = dCp / sc;
            double th = dHp / sh;
            return (float)System.Math.Sqrt((tl * tl) + (tc * tc) + (th * th) + (rt * tc * th));
        }

        /// <summary>Luminancia relativa WCAG 2.1 de un color sRGB.</summary>
        public static float RelativeLuminance(Color c)
        {
            return (0.2126f * ToLinear(c.r)) + (0.7152f * ToLinear(c.g)) + (0.0722f * ToLinear(c.b));
        }

        /// <summary>Contraste WCAG 2.1 entre dos colores sRGB opacos (1 a 21).</summary>
        public static float ContrastRatio(Color a, Color b)
        {
            float la = RelativeLuminance(a);
            float lb = RelativeLuminance(b);
            float hi = Mathf.Max(la, lb);
            float lo = Mathf.Min(la, lb);
            return (hi + 0.05f) / (lo + 0.05f);
        }

        /// <summary>
        /// Composición "over" de <paramref name="top"/> con alfa <paramref name="alpha"/> sobre <paramref name="bottom"/>
        /// en espacio lineal, como la hace la GPU con el proyecto en Linear (guía 2.5, [GPU]). Devuelve sRGB opaco.
        /// </summary>
        public static Color CompositeLinear(Color top, float alpha, Color bottom)
        {
            alpha = Mathf.Clamp01(alpha);
            return new Color(
                ToSrgb((ToLinear(top.r) * alpha) + (ToLinear(bottom.r) * (1f - alpha))),
                ToSrgb((ToLinear(top.g) * alpha) + (ToLinear(bottom.g) * (1f - alpha))),
                ToSrgb((ToLinear(top.b) * alpha) + (ToLinear(bottom.b) * (1f - alpha))),
                1f);
        }

        /// <summary>Componente sRGB a lineal.</summary>
        public static float ToLinear(float c)
        {
            return c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

        /// <summary>Componente lineal a sRGB (recortado a 0..1).</summary>
        public static float ToSrgb(float c)
        {
            c = Mathf.Clamp01(c);
            return c <= 0.0031308f ? 12.92f * c : (1.055f * Mathf.Pow(c, 1f / 2.4f)) - 0.055f;
        }

        /// <summary>
        /// Color listo para un canal UV de un shader de UI: lineal si el proyecto está en Linear (los colores de vértice
        /// los convierte el Canvas; los UV no), sRGB si está en Gamma.
        /// </summary>
        public static Vector3 ToShaderSpace(Color c)
        {
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                return new Vector3(ToLinear(c.r), ToLinear(c.g), ToLinear(c.b));
            }

            return new Vector3(c.r, c.g, c.b);
        }

        private static Vector3 ToOkLab(Color c)
        {
            float r = ToLinear(c.r);
            float g = ToLinear(c.g);
            float b = ToLinear(c.b);
            float l = Cbrt((0.4122214708f * r) + (0.5363325363f * g) + (0.0514459929f * b));
            float m = Cbrt((0.2119034982f * r) + (0.6806995451f * g) + (0.1073969566f * b));
            float s = Cbrt((0.0883024619f * r) + (0.2817188376f * g) + (0.6299787005f * b));
            return new Vector3(
                (0.2104542553f * l) + (0.7936177850f * m) - (0.0040720468f * s),
                (1.9779984951f * l) - (2.4285922050f * m) + (0.4505937099f * s),
                (0.0259040371f * l) + (0.7827717662f * m) - (0.8086757660f * s));
        }

        private static Color FromOkLab(Vector3 lab)
        {
            float l = lab.x + (0.3963377774f * lab.y) + (0.2158037573f * lab.z);
            float m = lab.x - (0.1055613458f * lab.y) - (0.0638541728f * lab.z);
            float s = lab.x - (0.0894841775f * lab.y) - (1.2914855480f * lab.z);
            l = l * l * l;
            m = m * m * m;
            s = s * s * s;
            float r = (4.0767416621f * l) - (3.3077115913f * m) + (0.2309699292f * s);
            float g = (-1.2684380046f * l) + (2.6097574011f * m) - (0.3413193965f * s);
            float b = (-0.0041960863f * l) - (0.7034186147f * m) + (1.7076147010f * s);
            return new Color(ToSrgb(r), ToSrgb(g), ToSrgb(b), 1f);
        }

        private static float LabF(float t)
        {
            const float d = 6f / 29f;
            return t > d * d * d ? Cbrt(t) : (t / (3f * d * d)) + (4f / 29f);
        }

        private static float LabFInv(float t)
        {
            const float d = 6f / 29f;
            return t > d ? t * t * t : 3f * d * d * (t - (4f / 29f));
        }

        private static double HueDeg(double b, double a)
        {
            if (a == 0.0 && b == 0.0)
            {
                return 0.0;
            }

            double h = System.Math.Atan2(b, a) * 180.0 / System.Math.PI;
            return h < 0.0 ? h + 360.0 : h;
        }

        private static double Deg(double degrees)
        {
            return degrees * System.Math.PI / 180.0;
        }

        private static float Cbrt(float x)
        {
            return x < 0f ? -Mathf.Pow(-x, 1f / 3f) : Mathf.Pow(x, 1f / 3f);
        }
    }

    /// <summary>Curvas de animación (02 §4.1: ease-out, ease-in, ease-in-out; guía visual v2 §6: easeOutBack, easeOutCubic).</summary>
    public static class Ease
    {
        /// <summary>Cuadrática de entrada.</summary>
        public static float In(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        /// <summary>Cuadrática de salida.</summary>
        public static float Out(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - ((1f - t) * (1f - t));
        }

        /// <summary>Cúbica de entrada y salida.</summary>
        public static float InOut(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - (Mathf.Pow((-2f * t) + 2f, 3f) / 2f);
        }

        /// <summary>Cúbica de salida.</summary>
        public static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return 1f - (u * u * u);
        }

        /// <summary>
        /// Salida con rebote hacia atrás (<paramref name="overshoot"/> = 1,70158 da un pico de +10 % hacia t ≈ 0,58;
        /// guía 6.1). Puede pasar de 1.
        /// </summary>
        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            t = Mathf.Clamp01(t);
            float c3 = overshoot + 1f;
            float u = t - 1f;
            return 1f + (c3 * u * u * u) + (overshoot * u * u);
        }
    }
}
