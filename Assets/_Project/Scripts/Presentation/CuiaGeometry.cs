#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Silueta de la cuia-garrafa en unidades <c>u</c> (GDD 07 §4.1 y §4.5; D-006, punto 3, como Magic Sort: ficha §2.2 y
    /// propuesta §3.4, opción (a)): 1 dosis = 1 u; ancho exterior 2 u; cavidad interior recta de 1,60 × capacidad u (0,80
    /// del ancho) desde y = 0,60 u (con el fondo en arco, ver <see cref="FaceSagU"/>); pie de fibra de 0,30 u, base de 0,30 u, hombro de 0,40 u, cuello corto de 0,35 u y borde
    /// de fibra de 0,25 u (labio en 5,35 u); alto exterior 1,60 + capacidad u (5,60 u la estándar de 4: las 4 dosis ocupan
    /// 0,714 del alto, ≈ 1:2,8). <c>u = ancho de la cuia / 2</c>. Se mantienen el hombro, el cuello corto, el pie y el borde
    /// de fibra; nada de labio de botella (trade dress de 07 §1.3). Única fuente de estas cifras: el vidrio horneado
    /// (<see cref="ProceduralArt"/>), el líquido (<see cref="LiquidGraphic"/> y el shader <c>AF/UI/Liquid</c>, que las recibe
    /// como globales) y la pose del vertido las leen de aquí.
    /// </summary>
    public static class CuiaGeometry
    {
        /// <summary>Ancho exterior (u).</summary>
        public const float WidthU = 2f;

        /// <summary>Alto del pie de fibra (u).</summary>
        public const float FootTopU = 0.30f;

        /// <summary>
        /// Fondo de la cavidad junto a las paredes (u): base de vidrio grueso de 0,30 u sobre el pie. En el eje baja el arco de
        /// <see cref="FaceSagU"/> (0,20 u; <see cref="CavityFloorU"/>).
        /// </summary>
        public const float CavityBottomU = 0.60f;

        /// <summary>Medio ancho de la cavidad recta: 1,60 u / 2 (0,80 del ancho exterior, D-006).</summary>
        public const float CavityHalfWidthU = 0.80f;

        /// <summary>Alto del hombro (u).</summary>
        public const float ShoulderU = 0.40f;

        /// <summary>Alto del cuello corto (u).</summary>
        public const float NeckU = 0.35f;

        /// <summary>Alto del borde de fibra (u).</summary>
        public const float RimU = 0.25f;

        /// <summary>Medio ancho exterior del borde de fibra (u): anillo de 1,10 u. Sus cantos son la boca de vertido.</summary>
        public const float RimHalfWidthU = 0.55f;

        /// <summary>Medio ancho exterior del cuello (u): 1,00 u.</summary>
        public const float NeckOuterHalfWidthU = 0.50f;

        /// <summary>Medio ancho interior del cuello y de la boca (u): 0,80 u.</summary>
        public const float NeckInnerHalfWidthU = 0.40f;

        /// <summary>Medio ancho exterior al empezar el hombro (u): 1,98 u.</summary>
        public const float ShoulderOuterHalfWidthU = 0.99f;

        /// <summary>Medio ancho exterior máximo del cuerpo (u): 2,00 u.</summary>
        public const float BodyOuterHalfWidthU = 1.00f;

        /// <summary>Medio ancho exterior de la base sobre la cavidad (u): 1,90 u.</summary>
        public const float BaseOuterHalfWidthU = 0.95f;

        /// <summary>Medio ancho exterior del pie de fibra (u): anillo de 1,60 u.</summary>
        public const float FootHalfWidthU = 0.80f;

        /// <summary>
        /// Alto de la cara superior del líquido (u): elipse de 0,40 u (≈ 0,4 de dosis, como Magic Sort: 24 px con dosis de
        /// 62,75 px; ficha §2.2), del ancho de la cavidad y más clara que el tinte (D-006, punto 3).
        /// </summary>
        public const float TopFaceU = 0.40f;

        /// <summary>Del techo de la cavidad a lo alto del borde (u): hombro + cuello + borde = 1,00.</summary>
        public const float AboveCavityU = ShoulderU + NeckU + RimU;

        /// <summary>
        /// Flecha (u) del borde delantero de la cara superior a <paramref name="xRel"/> u del eje (X = x − 1): media elipse
        /// de <see cref="TopFaceU"/> de alto y el ancho de la cavidad, 0,20 u en el eje y 0 en las paredes. Como en Magic
        /// Sort (d_002340: cada frontera es el borde delantero de una elipse igual), cada frontera entre dosis y el fondo del
        /// líquido son ese arco bajo su nivel (el nivel es el de las paredes), así que en el eje todas las dosis miden 1 u,
        /// también la de arriba; el fondo de la cavidad baja ese arco (fondo redondeado). Lo usan el shader
        /// <c>AF/UI/Liquid</c>, <see cref="LiquidGraphic"/> y el vidrio horneado (<see cref="ProceduralArt"/>).
        /// </summary>
        public static float FaceSagU(float xRel)
        {
            float e = Mathf.Clamp01(Mathf.Abs(xRel) / CavityHalfWidthU);
            return 0.5f * TopFaceU * Mathf.Sqrt(1f - (e * e));
        }

        /// <summary>Fondo de la cavidad a <paramref name="xRel"/> u del eje: <see cref="CavityBottomU"/> menos el arco de <see cref="FaceSagU"/>.</summary>
        public static float CavityFloorU(float xRel)
        {
            return CavityBottomU - FaceSagU(xRel);
        }

        /// <summary>Alto total (u): 1,60 + capacidad (5,60 para la estándar de 4).</summary>
        public static float HeightU(int capacity)
        {
            return CavityBottomU + capacity + AboveCavityU;
        }

        /// <summary>Techo de la zona de dosis (u): 4,60 en la estándar.</summary>
        public static float CavityTopU(int capacity)
        {
            return CavityBottomU + capacity;
        }

        /// <summary>Labio de vertido (u): 5,35 en la estándar.</summary>
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
                return FootHalfWidthU; // pie: anillo de 1,60 u
            }

            if (y < CavityBottomU)
            {
                // Base 1,60 → 1,90 u.
                return FootHalfWidthU + ((BaseOuterHalfWidthU - FootHalfWidthU) * Ease.Out((y - FootTopU) / (CavityBottomU - FootTopU)));
            }

            if (y < top)
            {
                float peak = Mathf.Max(CavityBottomU + 0.01f, top - 0.40f); // máximo de 2,00 u en y ≈ 4,2
                if (y <= peak)
                {
                    return BaseOuterHalfWidthU + ((BodyOuterHalfWidthU - BaseOuterHalfWidthU) * Mathf.SmoothStep(0f, 1f, (y - CavityBottomU) / (peak - CavityBottomU)));
                }

                return BodyOuterHalfWidthU - ((BodyOuterHalfWidthU - ShoulderOuterHalfWidthU) * ((y - peak) / Mathf.Max(0.01f, top - peak)));
            }

            if (y < top + ShoulderU)
            {
                return Mathf.Lerp(ShoulderOuterHalfWidthU, NeckOuterHalfWidthU, Mathf.SmoothStep(0f, 1f, (y - top) / ShoulderU)); // hombro 1,98 → 1,00
            }

            if (y < top + ShoulderU + NeckU)
            {
                return NeckOuterHalfWidthU; // cuello 1,00
            }

            if (y <= HeightU(capacity))
            {
                return RimHalfWidthU; // borde: anillo de 1,10
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
                return Mathf.Lerp(CavityHalfWidthU, NeckInnerHalfWidthU, Mathf.SmoothStep(0f, 1f, (y - top) / ShoulderU)); // 1,60 → 0,80
            }

            if (y <= HeightU(capacity))
            {
                return NeckInnerHalfWidthU;
            }

            return 0f;
        }
    }
}
