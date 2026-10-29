#nullable enable
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// La forma del agujero del primer fotograma de la intro (jornada 6, observación 12, 2026-10-29): One UI recorta el icono de su
    /// pantalla de arranque con su cuadrado redondeado (la máscara de iconos del sistema), no con un círculo. Java mide esa máscara
    /// (<c>AfSplashHold.systemMask()</c>: sus radios a 0, 45 y 22,5 grados) y aquí se ajusta una <b>superelipse</b>
    /// |x/a|ⁿ + |y/a|ⁿ = 1 con el radio de los ejes (<see cref="Axis"/>) y el de las diagonales (<see cref="Diag"/>) exactos.
    /// <para>
    /// La apertura la lleva al círculo sin que ningún píxel cambie de golpe (<see cref="At"/>): las esquinas se quedan donde estaban,
    /// los ejes crecen con el círculo y el exponente baja hasta 2; en cuanto el círculo que se abre pasa por las esquinas, el agujero
    /// ya es ese círculo. Cada agujero contiene al anterior (un test lo comprueba cada 0,5 grados).
    /// </para>
    /// Sin máscara (el Editor, Android 8-11, el velo propio, una máscara ilegible o fuera de rango) es el círculo de siempre
    /// (<see cref="Circle"/>). Sin asignaciones.
    /// </summary>
    internal readonly struct IconMaskShape
    {
        /// <summary>El círculo: sin máscara del sistema.</summary>
        public static readonly IconMaskShape Circle = new IconMaskShape(1f, 1f, 0f, false);

        /// <summary>Exponente máximo de la superelipse (casi un cuadrado) [P].</summary>
        public const float MaxExponent = 20f;

        /// <summary>
        /// Lo que crece el círculo, como mínimo, mientras la forma pasa al círculo (fracción del radio del fotograma 0) [P]: con el
        /// cuadrado redondeado de One UI manda su diagonal (~13-19 %), que el círculo alcanza a los ~130-160 ms de la apertura.
        /// </summary>
        public const float MinMorphGrowth = 0.10f;

        /// <summary>El radio de los ejes más pequeño que se cree (fracción de medio lado): por debajo, la máscara no es la del icono.</summary>
        public const float MinAxis = 0.80f;

        private IconMaskShape(float axis, float diag, float mid, bool fromMask)
        {
            Axis = axis;
            Diag = diag;
            Mid = mid;
            FromMask = fromMask;
        }

        /// <summary>Radio de los ejes en el fotograma 0 (fracción de medio lado de la caja: 1 = el círculo inscrito).</summary>
        public float Axis { get; }

        /// <summary>Radio de las diagonales, a 45 grados, en el fotograma 0 (fracción de medio lado).</summary>
        public float Diag { get; }

        /// <summary>Radio de la máscara a 22,5 grados (fracción de medio lado; 0 si no se midió): solo para ver el ajuste.</summary>
        public float Mid { get; }

        /// <summary>La forma sale de la máscara medida del sistema (no del círculo por defecto).</summary>
        public bool FromMask { get; }

        /// <summary>Exponente de la superelipse del fotograma 0 (2 = el círculo).</summary>
        public float Exponent => ExponentFor(Axis, Diag);

        /// <summary>No es un círculo (sonda <c>morph_shape</c>: <c>squircle</c>; si no, <c>circle</c>).</summary>
        public bool IsSquircle => FromMask && Exponent > 2.02f;

        /// <summary>El nombre para la sonda <c>morph_shape</c>.</summary>
        public string Name => IsSquircle ? "squircle" : "circle";

        /// <summary>
        /// La forma de la máscara de <paramref name="box"/> (diezmilésimas de medio lado); el círculo si no hay, si no es de una
        /// caja medida o si sale de rango (ejes entre <see cref="MinAxis"/> y 1, diagonal entre los ejes y los ejes × √2).
        /// </summary>
        public static IconMaskShape From(SplashIconBox box)
        {
            if (!box.Measured || box.MaskAxis <= 0 || box.MaskDiag <= 0)
            {
                return Circle;
            }

            float axis = box.MaskAxis / 10000f;
            float diag = box.MaskDiag / 10000f;
            float mid = box.MaskMid > 0 ? box.MaskMid / 10000f : 0f;
            if (axis < MinAxis || axis > 1.005f || diag < axis * 0.995f || diag >= axis * 1.41f)
            {
                return Circle;
            }

            return new IconMaskShape(Mathf.Min(axis, 1f), Mathf.Max(diag, Mathf.Min(axis, 1f)), mid, true);
        }

        /// <summary>
        /// El exponente de la superelipse con radio <paramref name="axis"/> en los ejes y <paramref name="diag"/> a 45 grados:
        /// diag / axis = √2 · 2^(−1/n). 2 si la diagonal no sale de los ejes (el círculo); como mucho <see cref="MaxExponent"/>.
        /// </summary>
        public static float ExponentFor(float axis, float diag)
        {
            if (axis <= 0f || diag <= axis * 1.00001f)
            {
                return 2f;
            }

            float den = 0.5f - Mathf.Log(diag / axis, 2f);
            if (den <= 1f / MaxExponent)
            {
                return MaxExponent;
            }

            return Mathf.Clamp(1f / den, 2f, MaxExponent);
        }

        /// <summary>El radio de la superelipse (<paramref name="axis"/>, <paramref name="n"/>) en el ángulo <paramref name="radians"/>.</summary>
        public static float Radius(float axis, float n, float radians)
        {
            float c = Mathf.Abs(Mathf.Cos(radians));
            float s = Mathf.Abs(Mathf.Sin(radians));
            float sum = Mathf.Pow(c, n) + Mathf.Pow(s, n);
            return sum <= 0f ? axis : axis * Mathf.Pow(sum, -1f / n);
        }

        /// <summary>
        /// El agujero cuando el círculo de la apertura mide <paramref name="rho"/> de radio y el del fotograma 0
        /// <paramref name="rho0"/> (las mismas unidades): radio de los ejes <paramref name="axis"/>, exponente <paramref name="n"/>
        /// y cuánto falta para el círculo (<paramref name="progress"/>: 0 la forma del sistema, 1 ya el círculo de radio
        /// <paramref name="rho"/>). Las esquinas se quedan a <see cref="Diag"/> · rho0 hasta que el círculo las alcanza; los ejes
        /// van de <see cref="Axis"/> · rho0 a rho.
        /// </summary>
        public void At(float rho0, float rho, out float axis, out float n, out float progress)
        {
            if (!FromMask || rho0 <= 0f)
            {
                axis = rho;
                n = 2f;
                progress = 1f;
                return;
            }

            float done = rho0 * Mathf.Max(Diag, 1f + MinMorphGrowth);
            progress = Mathf.Clamp01((rho - rho0) / (done - rho0));
            axis = rho * Mathf.Lerp(Axis, 1f, progress);
            float diag = Mathf.Max(Diag * rho0, axis);
            n = progress >= 1f ? 2f : ExponentFor(axis, diag);
        }
    }
}
