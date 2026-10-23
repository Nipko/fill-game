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
    /// La decisión es <see cref="Choose"/>, pura y probada en EditMode.
    /// </summary>
    public static class MaxAdProvider
    {
        /// <summary>Por qué <see cref="MaxConfig.Read"/> no dio configuración en el editor y los tests.</summary>
        public const string ReasonEditor = "editor";

        /// <summary>Por qué se eligió el proveedor (<c>max</c>, <c>editor</c>, <c>not_configured</c>, <c>config_unreadable</c>).</summary>
        public static string LastReason { get; private set; } = "none";

        /// <summary>
        /// La decisión, sin motor: con configuración, lo que dé <paramref name="createMax"/> (MAX); sin ella, en un release fuera del
        /// editor (<paramref name="why"/> distinto de <see cref="ReasonEditor"/>), <see cref="UnavailableAdService"/>; si no, null
        /// (anuncios simulados).
        /// </summary>
        public static IAdService? Choose(bool hasConfig, string why, bool releaseBuild, Func<IAdService>? createMax = null)
        {
            if (hasConfig)
            {
                return createMax?.Invoke();
            }

            if (releaseBuild && !string.Equals(why, ReasonEditor, StringComparison.Ordinal))
            {
                return new UnavailableAdService();
            }

            return null;
        }

        /// <summary>
        /// El proveedor real, o null para usar el simulado. <paramref name="releaseBuild"/>: sin configuración devuelve
        /// <see cref="UnavailableAdService"/> en lugar de null.
        /// </summary>
        public static IAdService? Create(
            IClock clock, Func<string?> uid, Func<int> nextLevel, Func<AdsConfig>? config, bool releaseBuild)
        {
            MaxConfig? max = MaxConfig.Read(out string why);
            LastReason = max != null ? "max" : why;
            Func<IAdService>? createMax = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (max != null)
            {
                createMax = () => new MaxAdService(new NativeMaxSdkPort(max), clock, uid, nextLevel, config);
            }
#endif
            return Choose(createMax != null, why, releaseBuild, createMax);
        }
    }
}
