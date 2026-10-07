#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Utilidades de color: interpolación en OKLab para el teñido de Sumergir y teñir (02 §4.2, 07 §5.1: "para no
    /// pasar por grises") y aclarados.
    /// </summary>
    public static class ColorMath
    {
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

        /// <summary>Aclara un color hacia el blanco en la proporción <paramref name="amount"/> (conserva el alfa).</summary>
        public static Color Lighten(Color c, float amount)
        {
            return new Color(
                c.r + ((1f - c.r) * amount),
                c.g + ((1f - c.g) * amount),
                c.b + ((1f - c.b) * amount),
                c.a);
        }

        /// <summary>Mismo color con otro alfa.</summary>
        public static Color WithAlpha(Color c, float alpha)
        {
            c.a = alpha;
            return c;
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

        private static float ToLinear(float c)
        {
            return c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

        private static float ToSrgb(float c)
        {
            c = Mathf.Clamp01(c);
            return c <= 0.0031308f ? 12.92f * c : (1.055f * Mathf.Pow(c, 1f / 2.4f)) - 0.055f;
        }

        private static float Cbrt(float x)
        {
            return x < 0f ? -Mathf.Pow(-x, 1f / 3f) : Mathf.Pow(x, 1f / 3f);
        }
    }

    /// <summary>Curvas de animación (02 §4.1: ease-out, ease-in, ease-in-out).</summary>
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
    }
}
