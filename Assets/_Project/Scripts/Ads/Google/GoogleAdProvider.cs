#nullable enable
using System;
using AF.Platform;
using UnityEngine;

namespace AF.Ads.Google
{
    /// <summary>
    /// Qué proveedor de anuncios usa este build (D-049: AdMob directo con Google Mobile Ads; MAX salió del proyecto, añadido del 2026-10-27):
    /// <list type="bullet">
    /// <item>En un teléfono Android: <see cref="GoogleAdService"/> sobre <see cref="NativeGmaPort"/>, con los bloques de
    /// <see cref="GoogleAdUnits.Choose"/> (los de prueba de Google en desarrollo y con <c>com.amazonfill.dev</c>). No toca el SDK hasta
    /// que el consentimiento lo permite (<see cref="ConsentAdService"/>).</item>
    /// <item>En el editor y los tests: null, y quien arranca usa los anuncios simulados (<see cref="FakeAdService"/>; 08 §2.1,
    /// principio 6).</item>
    /// </list>
    /// </summary>
    public static class GoogleAdProvider
    {
        /// <summary>Proveedor elegido: Google Mobile Ads.</summary>
        public const string ReasonGoogle = "google";

        /// <summary>Proveedor elegido: ninguno real (editor o tests).</summary>
        public const string ReasonEditor = "editor";

        /// <summary>Por qué se eligió el proveedor (<c>google</c> o <c>editor</c>), sin datos.</summary>
        public static string LastReason { get; private set; } = "none";

        /// <summary>Bloques de producción en este build (para el log del arranque, sin escribir los ID).</summary>
        public static bool LastProductionUnits { get; private set; }

        /// <summary>El proveedor real, o null para usar el simulado.</summary>
        public static IAdService? Create(IClock clock, Func<string?> uid, Func<int> nextLevel, Func<AdsConfig>? config)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            GoogleAdUnits units = GoogleAdUnits.Choose(Debug.isDebugBuild, Application.identifier);
            LastReason = ReasonGoogle;
            LastProductionUnits = units.Production;
            return new GoogleAdService(new NativeGmaPort(units), clock, uid, nextLevel, config);
#else
            LastReason = ReasonEditor;
            LastProductionUnits = false;
            return null;
#endif
        }
    }
}
