#nullable enable
using System;
using AF.Platform;

namespace AF.Ads.Max
{
    /// <summary>
    /// Qué proveedor de anuncios usa este build (D-042):
    /// <list type="bullet">
    /// <item>En un teléfono Android con la configuración de MAX en el build (<see cref="MaxConfig"/>): <see cref="MaxAdService"/> sobre
    /// <see cref="NativeMaxSdkPort"/>. No toca el SDK hasta que el consentimiento lo permite (<see cref="ConsentAdService"/>).</item>
    /// <item>Sin configuración, en un build de desarrollo, el editor o los tests: null, y quien arranca usa los anuncios simulados
    /// (<see cref="FakeAdService"/>), como en la fase 2.</item>
    /// <item>Sin configuración en un build de release: <see cref="UnavailableAdService"/> (nunca el anuncio simulado).</item>
    /// </list>
    /// </summary>
    public static class MaxAdProvider
    {
        /// <summary>Por qué se eligió el proveedor (<c>max</c>, <c>editor</c>, <c>not_configured</c>, <c>config_unreadable</c>).</summary>
        public static string LastReason { get; private set; } = "none";

        /// <summary>
        /// El proveedor real, o null para usar el simulado. <paramref name="releaseBuild"/>: sin configuración devuelve
        /// <see cref="UnavailableAdService"/> en lugar de null.
        /// </summary>
        public static IAdService? Create(
            IClock clock, Func<string?> uid, Func<int> nextLevel, Func<AdsConfig>? config, bool releaseBuild)
        {
            MaxConfig? max = MaxConfig.Read(out string why);
            LastReason = max != null ? "max" : why;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (max != null)
            {
                return new MaxAdService(new NativeMaxSdkPort(max), clock, uid, nextLevel, config);
            }
#endif
            if (releaseBuild && why != "editor")
            {
                return new UnavailableAdService();
            }

            return null;
        }
    }
}
