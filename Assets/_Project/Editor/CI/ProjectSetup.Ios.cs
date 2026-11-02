#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace AF.EditorTools
{
    /// <summary>
    /// Ajustes iOS fijados del proyecto (fase M1 de <c>docs/dev/traspaso/mac-ios.md</c>, D-067). Idempotente, como
    /// <see cref="ApplyAndroidDefaults"/>, y la forma versionada de cambiar estos Player Settings (regla dura 3). No toca nada de
    /// Android: los ajustes comunes (nombre, orientación, pantalla de Unity, HDR, posproceso) son los mismos que fija
    /// <see cref="ApplyAndroidDefaults"/>, y aquí solo se comprueban.
    ///
    /// Uso en la Mac, en batchmode (sin ningún Editor abierto en la carpeta):
    ///   Unity -batchmode -nographics -quit -buildTarget iOS -projectPath . -logFile Logs/project-setup-ios.log
    ///         -executeMethod AF.EditorTools.ProjectSetup.ApplyIosDefaults
    /// Código de salida: 0 si todo queda aplicado; 1 si algo falla (la excepción aborta el batchmode).
    /// </summary>
    public static partial class ProjectSetup
    {
        /// <summary>
        /// iOS 15 como mínimo (<c>mac-ios.md</c> §4, M1 [P]): el iPhone 6s y posteriores; Metal y ProMotion sin restricciones.
        /// </summary>
        public const string IosMinimumVersion = "15.0";

        /// <summary>Solo iPhone en la primera versión [P]: el juego es vertical y en iPad pediría todas las orientaciones.</summary>
        public const iOSTargetDevice IosTargetDevice = iOSTargetDevice.iPhoneOnly;

        /// <summary>ARM64 (<c>PlayerSettings.SetArchitecture</c>: 1 = ARM64 en iOS).</summary>
        public const int IosArchitectureArm64 = 1;

        /// <summary>Solo Metal (<c>mac-ios.md</c> §4, M1).</summary>
        public static readonly GraphicsDeviceType[] IosGraphicsApis = { GraphicsDeviceType.Metal };

        /// <summary>
        /// Número de build de iOS (<c>CFBundleVersion</c>). En M1 se instala por cable con el Apple ID gratuito y no hace falta
        /// subirlo; TestFlight (M5) lo pide creciente.
        /// </summary>
        public const string IosBuildNumber = "1";

        [MenuItem("Amazon Fill/CI/Aplicar ajustes iOS (dev)")]
        public static void ApplyIosDefaults()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                throw new InvalidOperationException(LogPrefix + "este Editor no tiene el módulo iOS Build Support.");
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            {
                Debug.Log(LogPrefix + "cambiando la plataforma activa a iOS...");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.iOS, BuildTarget.iOS))
                {
                    throw new InvalidOperationException(LogPrefix + "no se pudo cambiar la plataforma activa a iOS.");
                }
            }

            NamedBuildTarget ios = NamedBuildTarget.iOS;
            PlayerSettings.SetApplicationIdentifier(ios, ApplicationId);
            PlayerSettings.SetScriptingBackend(ios, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(ios, IosArchitectureArm64);
            PlayerSettings.SetManagedStrippingLevel(ios, StrippingLevel);
            // Metal es la única API de Unity 6 en iOS: la lista automática ya es solo Metal, y Unity deja el ajuste en automático
            // aunque se le pida lo contrario. Se comprueba la lista resultante (ValidateIos).
            PlayerSettings.iOS.targetOSVersionString = IosMinimumVersion;
            PlayerSettings.iOS.targetDevice = IosTargetDevice;
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.buildNumber = IosBuildNumber;
            PlayerSettings.iOS.requiresFullScreen = true;

            // Firma automática, sin equipo en el repositorio: el equipo (el Apple ID gratuito del dueño hasta que exista la cuenta
            // de Parallext, I-8) se pasa a xcodebuild con DEVELOPMENT_TEAM al compilar (docs/dev/mac-setup.md §6).
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;

            // Avisos locales (M2): el permiso solo tras «Avísame» o desde Ajustes (06 §8.1), nunca al abrir; sin notificaciones
            // remotas ni sus capacidades (solo locales, como en Android: 06 §8.2).
            Unity.Notifications.NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch = false;
            Unity.Notifications.NotificationSettings.iOSSettings.AddRemoteNotificationCapability = false;
            Unity.Notifications.NotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch = false;
            Unity.Notifications.NotificationSettings.iOSSettings.AddTimeSensitiveEntitlement = false;

            AssetDatabase.SaveAssets();

            List<string> problems = ValidateIos();
            if (problems.Count > 0)
            {
                throw new InvalidOperationException(LogPrefix + "ajustes iOS sin aplicar: " + string.Join("; ", problems));
            }

            Debug.Log(LogPrefix + "OK (iOS). " + DescribeIos());
        }

        /// <summary>Compara los Player Settings de iOS (y los comunes) con los fijados. Devuelve una lista vacía si coinciden.</summary>
        public static List<string> ValidateIos()
        {
            NamedBuildTarget ios = NamedBuildTarget.iOS;
            var problems = new List<string>();
            Check(problems, "productName", PlayerSettings.productName, ProductName);
            Check(problems, "defaultInterfaceOrientation", PlayerSettings.defaultInterfaceOrientation, Orientation);
            Check(problems, "ios.applicationId", PlayerSettings.GetApplicationIdentifier(ios), ApplicationId);
            Check(problems, "ios.scriptingBackend", PlayerSettings.GetScriptingBackend(ios), ScriptingImplementation.IL2CPP);
            Check(problems, "ios.architecture", PlayerSettings.GetArchitecture(ios), IosArchitectureArm64);
            Check(problems, "ios.managedStrippingLevel", PlayerSettings.GetManagedStrippingLevel(ios), StrippingLevel);
            string apis = string.Join(">", PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS));
            Check(problems, "ios.graphicsAPIs", apis, string.Join(">", IosGraphicsApis));
            Check(problems, "ios.targetOSVersion", PlayerSettings.iOS.targetOSVersionString, IosMinimumVersion);
            Check(problems, "ios.targetDevice", PlayerSettings.iOS.targetDevice, IosTargetDevice);
            Check(problems, "ios.sdkVersion", PlayerSettings.iOS.sdkVersion, iOSSdkVersion.DeviceSDK);
            Check(problems, "ios.buildNumber", PlayerSettings.iOS.buildNumber, IosBuildNumber);
            Check(problems, "ios.requiresFullScreen", PlayerSettings.iOS.requiresFullScreen, true);
            Check(problems, "ios.automaticSigning", PlayerSettings.iOS.appleEnableAutomaticSigning, true);
            Check(problems, "splashScreen.show", PlayerSettings.SplashScreen.show, ShowUnitySplash);
            Check(problems, "splashScreen.backgroundColor", (Color32)PlayerSettings.SplashScreen.backgroundColor, BrandPetroleum);
            Check(problems, "notifications.ios.requestAuthorizationOnAppLaunch",
                Unity.Notifications.NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch, false);
            Check(problems, "notifications.ios.addRemoteNotificationCapability",
                Unity.Notifications.NotificationSettings.iOSSettings.AddRemoteNotificationCapability, false);
            Check(problems, "notifications.ios.requestRemoteAuthorizationOnAppLaunch",
                Unity.Notifications.NotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch, false);
            Check(problems, "notifications.ios.addTimeSensitiveEntitlement",
                Unity.Notifications.NotificationSettings.iOSSettings.AddTimeSensitiveEntitlement, false);
            return problems;
        }

        /// <summary>Resumen de una línea de los ajustes iOS actuales (para el log).</summary>
        public static string DescribeIos()
        {
            NamedBuildTarget ios = NamedBuildTarget.iOS;
            return "plataforma=" + EditorUserBuildSettings.activeBuildTarget
                + ", bundleId=" + PlayerSettings.GetApplicationIdentifier(ios)
                + ", productName=\"" + PlayerSettings.productName + "\""
                + ", version=" + PlayerSettings.bundleVersion + " (" + PlayerSettings.iOS.buildNumber + ")"
                + ", iosMinimo=" + PlayerSettings.iOS.targetOSVersionString
                + ", dispositivos=" + PlayerSettings.iOS.targetDevice
                + ", backend=" + PlayerSettings.GetScriptingBackend(ios)
                + ", arquitectura=" + PlayerSettings.GetArchitecture(ios)
                + ", graficos=" + string.Join(">", PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS))
                + ", stripping=" + PlayerSettings.GetManagedStrippingLevel(ios)
                + ", firmaAutomatica=" + PlayerSettings.iOS.appleEnableAutomaticSigning;
        }
    }
}
