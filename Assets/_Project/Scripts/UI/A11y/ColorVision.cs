#nullable enable
using System;
using UnityEngine;

namespace AF.UI
{
    /// <summary>Tipo de daltonismo que simula <see cref="ColorVision"/> (07 §3, regla 3, y §7.8: los tres tipos).</summary>
    public enum ColorVisionDeficiency
    {
        /// <summary>Sin simular (la visión típica).</summary>
        None = 0,

        /// <summary>Protanopía (sin conos L).</summary>
        Protan = 1,

        /// <summary>Deuteranopía (sin conos M).</summary>
        Deutan = 2,

        /// <summary>Tritanopía (sin conos S).</summary>
        Tritan = 3,
    }

    /// <summary>
    /// Simulador de daltonismo para el QA de la paleta (bloque B9; 07 §3, regla 3, y §7.8: «QA con simulador de daltonismo en los
    /// tres tipos sobre el shader real»; 08 §14.3). El GDD no fija el método, así que es [P] (plan de la tanda 5, §4, fila B9;
    /// Q-B9-10): las matrices de Machado, Oliveira y Fernandes (2009, «A Physiologically-based Model for Simulation of Color Vision
    /// Deficiency», IEEE TVCG 15(6)) con severidad 1,0, aplicadas en RGB lineal; la distancia, CIEDE2000 en CIELAB (D65), la misma
    /// escala L* de la tabla de 07 §3. Solo para tests y herramientas: el juego no lo usa. Sin asignaciones.
    /// </summary>
    public static class ColorVision
    {
        // Machado et al. (2009), tabla de severidad 1,0 (filas: R', G', B' a partir de R, G, B lineales).
        private static readonly float[] Protan =
        {
            0.152286f, 1.052583f, -0.204868f,
            0.114503f, 0.786281f, 0.099216f,
            -0.003882f, -0.048116f, 1.051998f,
        };

        private static readonly float[] Deutan =
        {
            0.367322f, 0.860646f, -0.227968f,
            0.280085f, 0.672501f, 0.047413f,
            -0.011820f, 0.042940f, 0.968881f,
        };

        private static readonly float[] Tritan =
        {
            1.255528f, -0.076749f, -0.178779f,
            -0.078411f, 0.930809f, 0.147602f,
            0.004733f, 0.691367f, 0.303900f,
        };

        /// <summary>
        /// El color sRGB <paramref name="srgb"/> (sin alfa) como lo vería alguien con <paramref name="deficiency"/>, en sRGB y
        /// recortado a [0, 1].
        /// </summary>
        public static Color Simulate(Color srgb, ColorVisionDeficiency deficiency)
        {
            if (deficiency == ColorVisionDeficiency.None)
            {
                return new Color(srgb.r, srgb.g, srgb.b, 1f);
            }

            float[] m = deficiency == ColorVisionDeficiency.Protan ? Protan : deficiency == ColorVisionDeficiency.Deutan ? Deutan : Tritan;
            float r = ToLinear(srgb.r);
            float g = ToLinear(srgb.g);
            float b = ToLinear(srgb.b);
            float r2 = Mathf.Clamp01((m[0] * r) + (m[1] * g) + (m[2] * b));
            float g2 = Mathf.Clamp01((m[3] * r) + (m[4] * g) + (m[5] * b));
            float b2 = Mathf.Clamp01((m[6] * r) + (m[7] * g) + (m[8] * b));
            return new Color(ToSrgb(r2), ToSrgb(g2), ToSrgb(b2), 1f);
        }

        /// <summary>CIELAB (D65, observador de 2°) del color sRGB <paramref name="srgb"/>: (L*, a*, b*).</summary>
        public static Vector3 Lab(Color srgb)
        {
            double r = ToLinear(srgb.r);
            double g = ToLinear(srgb.g);
            double b = ToLinear(srgb.b);
            double x = (0.4124564 * r) + (0.3575761 * g) + (0.1804375 * b);
            double y = (0.2126729 * r) + (0.7151522 * g) + (0.0721750 * b);
            double z = (0.0193339 * r) + (0.1191920 * g) + (0.9503041 * b);
            double fx = F(x / 0.95047);
            double fy = F(y / 1.0);
            double fz = F(z / 1.08883);
            return new Vector3((float)((116.0 * fy) - 16.0), (float)(500.0 * (fx - fy)), (float)(200.0 * (fy - fz)));
        }

        /// <summary>Diferencia de color CIEDE2000 entre dos colores CIELAB (kL = kC = kH = 1).</summary>
        public static double DeltaE2000(Vector3 lab1, Vector3 lab2)
        {
            double l1 = lab1.x, a1 = lab1.y, b1 = lab1.z;
            double l2 = lab2.x, a2 = lab2.y, b2 = lab2.z;
            double c1 = Math.Sqrt((a1 * a1) + (b1 * b1));
            double c2 = Math.Sqrt((a2 * a2) + (b2 * b2));
            double cBar = (c1 + c2) / 2.0;
            double cBar7 = Math.Pow(cBar, 7.0);
            double gFactor = 0.5 * (1.0 - Math.Sqrt(cBar7 / (cBar7 + 6103515625.0))); // 25^7
            double a1p = (1.0 + gFactor) * a1;
            double a2p = (1.0 + gFactor) * a2;
            double c1p = Math.Sqrt((a1p * a1p) + (b1 * b1));
            double c2p = Math.Sqrt((a2p * a2p) + (b2 * b2));
            double h1p = Hue(b1, a1p);
            double h2p = Hue(b2, a2p);
            double dLp = l2 - l1;
            double dCp = c2p - c1p;
            double dhp;
            if (c1p * c2p == 0.0)
            {
                dhp = 0.0;
            }
            else
            {
                dhp = h2p - h1p;
                if (dhp > 180.0)
                {
                    dhp -= 360.0;
                }
                else if (dhp < -180.0)
                {
                    dhp += 360.0;
                }
            }

            double dHp = 2.0 * Math.Sqrt(c1p * c2p) * Math.Sin(Rad(dhp / 2.0));
            double lBarP = (l1 + l2) / 2.0;
            double cBarP = (c1p + c2p) / 2.0;
            double hBarP;
            if (c1p * c2p == 0.0)
            {
                hBarP = h1p + h2p;
            }
            else if (Math.Abs(h1p - h2p) <= 180.0)
            {
                hBarP = (h1p + h2p) / 2.0;
            }
            else
            {
                hBarP = h1p + h2p < 360.0 ? (h1p + h2p + 360.0) / 2.0 : (h1p + h2p - 360.0) / 2.0;
            }

            double t = 1.0 - (0.17 * Math.Cos(Rad(hBarP - 30.0))) + (0.24 * Math.Cos(Rad(2.0 * hBarP)))
                + (0.32 * Math.Cos(Rad((3.0 * hBarP) + 6.0))) - (0.20 * Math.Cos(Rad((4.0 * hBarP) - 63.0)));
            double dTheta = 30.0 * Math.Exp(-Math.Pow((hBarP - 275.0) / 25.0, 2.0));
            double cBarP7 = Math.Pow(cBarP, 7.0);
            double rc = 2.0 * Math.Sqrt(cBarP7 / (cBarP7 + 6103515625.0));
            double l50 = (lBarP - 50.0) * (lBarP - 50.0);
            double sl = 1.0 + (0.015 * l50 / Math.Sqrt(20.0 + l50));
            double sc = 1.0 + (0.045 * cBarP);
            double sh = 1.0 + (0.015 * cBarP * t);
            double rt = -Math.Sin(Rad(2.0 * dTheta)) * rc;
            double dl = dLp / sl;
            double dc = dCp / sc;
            double dh = dHp / sh;
            return Math.Sqrt((dl * dl) + (dc * dc) + (dh * dh) + (rt * dc * dh));
        }

        /// <summary>CIEDE2000 entre dos colores sRGB vistos con <paramref name="deficiency"/>.</summary>
        public static double DeltaE2000(Color a, Color b, ColorVisionDeficiency deficiency)
        {
            return DeltaE2000(Lab(Simulate(a, deficiency)), Lab(Simulate(b, deficiency)));
        }

        /// <summary>Componente sRGB → lineal (IEC 61966-2-1).</summary>
        public static float ToLinear(float c)
        {
            return c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

        /// <summary>Componente lineal → sRGB (IEC 61966-2-1).</summary>
        public static float ToSrgb(float c)
        {
            return c <= 0.0031308f ? c * 12.92f : (1.055f * Mathf.Pow(c, 1f / 2.4f)) - 0.055f;
        }

        private static double F(double t)
        {
            return t > 216.0 / 24389.0 ? Math.Pow(t, 1.0 / 3.0) : ((24389.0 / 27.0 * t) + 16.0) / 116.0;
        }

        private static double Hue(double b, double ap)
        {
            if (b == 0.0 && ap == 0.0)
            {
                return 0.0;
            }

            double h = Math.Atan2(b, ap) * 180.0 / Math.PI;
            return h < 0.0 ? h + 360.0 : h;
        }

        private static double Rad(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
