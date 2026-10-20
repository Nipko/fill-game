#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AF.EditorTools
{
    /// <summary>
    /// Ajustes Android fijados del proyecto (docs/setup/plan-instalacion.md, paso s25). Idempotente:
    /// se puede ejecutar las veces que haga falta. Es la forma versionada de cambiar estos Player
    /// Settings (CLAUDE.md, regla dura 3: nunca editar ProjectSettings/ a mano).
    ///
    /// Uso en batchmode (sin ningún Editor abierto en la carpeta), con la Unity CLI y el editor ARM64 nativo:
    ///   unity run &lt;repo&gt; --architecture arm64 --log-file Logs/project-setup.log --non-interactive
    ///             -- -nographics -executeMethod AF.EditorTools.ProjectSetup.ApplyAndroidDefaults
    ///   Unity.exe -batchmode -nographics -quit -projectPath . -logFile Logs/project-setup.log
    ///             -executeMethod AF.EditorTools.ProjectSetup.ApplyAndroidDefaults
    /// Código de salida: 0 si todo queda aplicado; 1 si algo falla (la excepción aborta el batchmode).
    /// También está en el menú "Amazon Fill/CI" del Editor.
    /// </summary>
    public static class ProjectSetup
    {
        /// <summary>
        /// Identificador PROVISIONAL de los builds de prueba. Usa el nombre interno del proyecto (Amazon Fill),
        /// no la marca, para que un cambio de marca no obligue a tocar código. El nombre comercial ya está
        /// decidido (Tintalú), pero el ID definitivo, com.&lt;estudio&gt;.tintalu, solo se fija tras el
        /// dictamen de marca (docs/legal/marca.md §7). Solo para builds de humo; nunca se sube a Google Play.
        /// </summary>
        public const string ApplicationId = "com.amazonfill.dev";

        /// <summary>
        /// Nombre visible de los builds de desarrollo: "Tintalú (dev)" (docs/dev/prototipo-v0.md d; glosario §11: el nombre
        /// visible lleva la marca, los identificadores internos no). Si la "ú" diera problemas en rutas de Gradle o Burst,
        /// el plan prevé "Tintalu (dev)".
        /// </summary>
        public const string ProductName = "Tintalú (dev)";

        /// <summary>Orientación vertical bloqueada, sin autorrotación (GDD 02 §5: "Orientación: vertical bloqueada").</summary>
        public const UIOrientation Orientation = UIOrientation.Portrait;

        /// <summary>minSdk 26 (CLAUDE.md, stack fijado).</summary>
        public const AndroidSdkVersions MinSdk = AndroidSdkVersions.AndroidApiLevel26;

        /// <summary>targetSdk FIJO en 36 (plan de instalación, cambio 5 del GDD). Nunca "Auto".</summary>
        public const AndroidSdkVersions TargetSdk = AndroidSdkVersions.AndroidApiLevel36;

        /// <summary>Solo ARM64 (IL2CPP).</summary>
        public const AndroidArchitecture Architectures = AndroidArchitecture.ARM64;

        /// <summary>
        /// android:appCategory del manifiesto. En 6000.3 la API pública es un string con el valor del
        /// manifiesto (el enum UnityEditor.AndroidAppCategory es interno); en ProjectSettings.asset queda
        /// como androidAppCategory: 3 (Game) con useAndroidAppCategory: 1.
        /// </summary>
        public const string AppCategory = "game";

        /// <summary>
        /// Managed Stripping High (08 §5.4; D-033, Q-T6: aplicarlo ya, con link.xml y un build de prueba). Las excepciones van en
        /// <c>Assets/Plugins/link.xml</c> (08 §3); si un SDK falla por reflexión se vuelve a Medium (08 §16.3, punto 3).
        /// </summary>
        public const ManagedStrippingLevel StrippingLevel = ManagedStrippingLevel.High;

        /// <summary>
        /// Retroceso predictivo de Android (08 §13.1 #4): todavía desactivado. Con <c>false</c>, Unity escribe
        /// <c>android:enableOnBackInvokedCallback="false"</c> en el manifiesto y Atrás sigue llegando como Escape al Input System,
        /// que es lo que prueban BackButtonTests y la prueba en el teléfono (FXT). Activarlo exige probar Atrás en frío en el
        /// teléfono (Android 16) y pasar <c>-RequirePredictiveBack</c> a <c>ci/check-manifest.ps1</c> (bloque B10 de la tanda 5).
        /// </summary>
        public const bool PredictiveBack = false;

        const string LogPrefix = "[AF.ProjectSetup] ";

        [MenuItem("Amazon Fill/CI/Aplicar ajustes Android (dev)")]
        public static void ApplyAndroidDefaults()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new InvalidOperationException(LogPrefix + "este Editor no tiene el módulo Android Build Support.");
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log(LogPrefix + "cambiando la plataforma activa a Android...");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android))
                {
                    throw new InvalidOperationException(LogPrefix + "no se pudo cambiar la plataforma activa a Android.");
                }
            }

            NamedBuildTarget android = NamedBuildTarget.Android;
            PlayerSettings.productName = ProductName;
            PlayerSettings.defaultInterfaceOrientation = Orientation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.useAnimatedAutorotation = false;
            PlayerSettings.SetApplicationIdentifier(android, ApplicationId);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.minSdkVersion = MinSdk;
            PlayerSettings.Android.targetSdkVersion = TargetSdk;
            PlayerSettings.Android.targetArchitectures = Architectures;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
            Debug.Log(LogPrefix + "appCategory antes: \"" + PlayerSettings.Android.appCategory + "\"");
            PlayerSettings.Android.appCategory = AppCategory;
            PlayerSettings.SetManagedStrippingLevel(android, StrippingLevel);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.Android.predictiveBackSupport = PredictiveBack;

            // Sin firma de release: Unity firma con la clave de depuración. El keystore lo gestiona el dueño.
            PlayerSettings.Android.useCustomKeystore = false;

            // AAB por defecto. Ojo: esto vive en Library/ (EditorUserBuildSettings), no en ProjectSettings/,
            // así que BuildSmoke lo fija explícitamente en cada build.
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

            AssetDatabase.SaveAssets();

            List<string> problems = Validate();
            if (problems.Count > 0)
            {
                throw new InvalidOperationException(LogPrefix + "ajustes sin aplicar: " + string.Join("; ", problems));
            }

            Debug.Log(LogPrefix + "OK. " + Describe());
        }

        /// <summary>
        /// Compara los Player Settings actuales con los fijados. Devuelve una lista vacía si coinciden.
        /// </summary>
        public static List<string> Validate()
        {
            NamedBuildTarget android = NamedBuildTarget.Android;
            var problems = new List<string>();
            Check(problems, "productName", PlayerSettings.productName, ProductName);
            Check(problems, "defaultInterfaceOrientation", PlayerSettings.defaultInterfaceOrientation, Orientation);
            Check(problems, "applicationId", PlayerSettings.GetApplicationIdentifier(android), ApplicationId);
            Check(problems, "scriptingBackend", PlayerSettings.GetScriptingBackend(android), ScriptingImplementation.IL2CPP);
            Check(problems, "minSdk", PlayerSettings.Android.minSdkVersion, MinSdk);
            Check(problems, "targetSdk", PlayerSettings.Android.targetSdkVersion, TargetSdk);
            Check(problems, "targetArchitectures", PlayerSettings.Android.targetArchitectures, Architectures);
            if (!string.Equals(PlayerSettings.Android.appCategory, AppCategory, StringComparison.OrdinalIgnoreCase))
            {
                problems.Add("appCategory=" + PlayerSettings.Android.appCategory + " (esperado " + AppCategory + ")");
            }

            Check(problems, "managedStrippingLevel", PlayerSettings.GetManagedStrippingLevel(android), StrippingLevel);
            Check(problems, "stripEngineCode", PlayerSettings.stripEngineCode, true);
            Check(problems, "predictiveBackSupport", PlayerSettings.Android.predictiveBackSupport, PredictiveBack);
            Check(problems, "useCustomKeystore", PlayerSettings.Android.useCustomKeystore, false);
            return problems;
        }

        /// <summary>Resumen de una línea de los ajustes Android actuales (para el log).</summary>
        public static string Describe()
        {
            NamedBuildTarget android = NamedBuildTarget.Android;
            return "plataforma=" + EditorUserBuildSettings.activeBuildTarget
                + ", applicationId=" + PlayerSettings.GetApplicationIdentifier(android)
                + ", productName=\"" + PlayerSettings.productName + "\""
                + ", orientacion=" + PlayerSettings.defaultInterfaceOrientation
                + ", backend=" + PlayerSettings.GetScriptingBackend(android)
                + ", minSdk=" + (int)PlayerSettings.Android.minSdkVersion
                + ", targetSdk=" + (int)PlayerSettings.Android.targetSdkVersion
                + ", arquitecturas=" + PlayerSettings.Android.targetArchitectures
                + ", appCategory=" + PlayerSettings.Android.appCategory
                + ", stripping=" + PlayerSettings.GetManagedStrippingLevel(android)
                + ", stripEngineCode=" + PlayerSettings.stripEngineCode
                + ", retrocesoPredictivo=" + PlayerSettings.Android.predictiveBackSupport
                + ", aab=" + EditorUserBuildSettings.buildAppBundle
                + ", keystorePropio=" + PlayerSettings.Android.useCustomKeystore;
        }

        static void Check<T>(List<string> problems, string name, T actual, T expected)
        {
            if (!EqualityComparer<T>.Default.Equals(actual, expected))
            {
                problems.Add(name + "=" + actual + " (esperado " + expected + ")");
            }
        }
    }
}
