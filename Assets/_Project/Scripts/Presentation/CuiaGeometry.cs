#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Silueta de la cuia-garrafa en unidades <c>u</c> (GDD 07 §4.1 y §4.5): ancho exterior 2 u, alto 2 + capacidad u
    /// (6 u la estándar de 4), cavidad interior recta de 1,30 u desde y = 0,60 u con dosis de 1 u exactas, hombro de
    /// 0,60 u, cuello de 0,55 u (labio en 5,75 u) y borde de 0,25 u. <c>u = ancho de la cuia / 2</c>.
    /// </summary>
    public static class CuiaGeometry
    {
        /// <summary>Ancho exterior (u).</summary>
        public const float WidthU = 2f;

        /// <summary>Alto del pie de fibra (u).</summary>
        public const float FootTopU = 0.30f;

        /// <summary>Fondo plano de la cavidad (u).</summary>
        public const float CavityBottomU = 0.60f;

        /// <summary>Medio ancho de la cavidad recta: 1,30 u / 2.</summary>
        public const float CavityHalfWidthU = 0.65f;

        /// <summary>Alto del hombro (u).</summary>
        public const float ShoulderU = 0.60f;

        /// <summary>Alto del cuello (u).</summary>
        public const float NeckU = 0.55f;

        /// <summary>Alto del borde de fibra (u).</summary>
        public const float RimU = 0.25f;

        /// <summary>Alto total (u): 6 para la estándar de 4 (tabla de 07 §4.5).</summary>
        public static float HeightU(int capacity)
        {
            return CavityBottomU + capacity + ShoulderU + NeckU + RimU;
        }

        /// <summary>Techo de la zona de dosis (u): 4,60 en la estándar.</summary>
        public static float CavityTopU(int capacity)
        {
            return CavityBottomU + capacity;
        }

        /// <summary>Labio de vertido (u): 5,75 en la estándar.</summary>
        public static float LipU(int capacity)
        {
            return HeightU(capacity) - RimU;
        }

        /// <summary>Medio ancho exterior a la altura <paramref name="y"/> (u); 0 fuera de la cuia.</summary>
        public static float OuterHalfWidth(float y, int capacity)
        {
            float top = CavityTopU(capacity);
            if (y < 0f)
            {
                return 0f;
            }

            if (y < FootTopU)
            {
                return 0.80f; // pie: anillo de 1,60 u
            }

            if (y < CavityBottomU)
            {
                return 0.80f + (0.15f * Ease.Out((y - FootTopU) / (CavityBottomU - FootTopU))); // base 1,60 → 1,90
            }

            if (y < top)
            {
                float peak = Mathf.Max(CavityBottomU + 0.01f, top - 0.40f); // máximo de 2,00 u en y ≈ 4,2
                if (y <= peak)
                {
                    return 0.95f + (0.05f * Mathf.SmoothStep(0f, 1f, (y - CavityBottomU) / (peak - CavityBottomU)));
                }

                return 1.00f - (0.01f * ((y - peak) / Mathf.Max(0.01f, top - peak)));
            }

            if (y < top + ShoulderU)
            {
                return Mathf.Lerp(0.99f, 0.50f, Mathf.SmoothStep(0f, 1f, (y - top) / ShoulderU)); // hombro 2,00 → 1,00
            }

            if (y < top + ShoulderU + NeckU)
            {
                return 0.50f; // cuello 1,00
            }

            if (y <= HeightU(capacity))
            {
                return 0.55f; // borde: anillo de 1,10
            }

            return 0f;
        }

        /// <summary>Medio ancho interior (cavidad) a la altura <paramref name="y"/> (u); 0 donde no hay cavidad.</summary>
        public static float InnerHalfWidth(float y, int capacity)
        {
            float top = CavityTopU(capacity);
            if (y < CavityBottomU)
            {
                return 0f;
            }

            if (y < top)
            {
                return CavityHalfWidthU;
            }

            if (y < top + ShoulderU)
            {
                return Mathf.Lerp(CavityHalfWidthU, 0.40f, Mathf.SmoothStep(0f, 1f, (y - top) / ShoulderU)); // 1,30 → 0,80
            }

            if (y <= HeightU(capacity))
            {
                return 0.40f;
            }

            return 0f;
        }
    }
}
