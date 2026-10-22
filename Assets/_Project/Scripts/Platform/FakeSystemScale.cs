#nullable enable

namespace AF.Platform
{
    /// <summary>
    /// Escalas del sistema fijadas a mano (<see cref="ISystemScale"/>; bloque B9): la implementación del Editor (fuente al 100 % y
    /// animaciones normales, como un teléfono sin tocar) y de los tests, que asignan lo que quieren probar (p. ej. la fuente al
    /// 150 % o «Quitar animaciones»).
    /// </summary>
    public sealed class FakeSystemScale : ISystemScale
    {
        /// <summary>Crea las escalas con <paramref name="fontScale"/> y <paramref name="animatorDurationScale"/> (1 y 1 por defecto).</summary>
        public FakeSystemScale(float fontScale = 1f, float animatorDurationScale = 1f)
        {
            FontScale = fontScale;
            AnimatorDurationScale = animatorDurationScale;
        }

        /// <inheritdoc />
        public float FontScale { get; set; }

        /// <inheritdoc />
        public float AnimatorDurationScale { get; set; }
    }
}
