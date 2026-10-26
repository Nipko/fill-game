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

        /// <summary>
        /// Pantalla «Made with Unity» quitada (D-051, punto 1: opcional en todos los planes desde Unity 6). Sin ella tampoco sale
        /// el logo de Unity. La intro de la marca la dibuja el juego (<c>AF.UI.BrandIntro</c>).
        /// </summary>
        public const bool ShowUnitySplash = false;

        /// <summary>
        /// Fondo petróleo de la marca (D-045 y D-051, punto 2): <c>#14444A</c>, el tono medio del cielo del icono v2
        /// (<c>docs/art/marca-oficial/v2/generar.py</c>, degradado «cielo»). Es el mismo de la pantalla del sistema de Android 12+
        /// (<c>af_brand_petroleum</c> en <c>AfIcon.androidlib</c>, que genera <c>exportar.py --android-xml</c>) y de la intro.
        /// Unity lo usa además como fondo de su pantalla y como <c>staticSplashScreenBackgroundColor</c> de su tema.
        /// </summary>
        public static readonly Color32 BrandPetroleum = new Color32(0x14, 0x44, 0x4A, 0xFF);

        /// <summary>
        /// HDR del pipeline (URP) apagado (memoria, 2026-10-26; observación 8 de la prueba completa del 2026-10-25): el juego no usa
        /// posproceso ni Bloom (el del 2D Renderer está prohibido, UUM-150232), así que el HDR solo obligaba a la cámara a pintar en
        /// un búfer intermedio de pantalla completa antes de copiarlo a la pantalla. Se aplica a todos los assets de URP del proyecto
        /// (el de Graphics y el de cada nivel de calidad) con su propiedad serializada <c>m_SupportsHDR</c>.
        /// </summary>
        public const bool PipelineHdr = false;

        /// <summary>Propiedad serializada del HDR en el asset de URP (<c>UniversalRenderPipelineAsset.supportsHDR</c>).</summary>
        internal const string HdrProperty = "m_SupportsHDR";

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

            // D-051, punto 1: sin la pantalla de Unity ni su logo; el fondo, el petróleo de la marca.
            PlayerSettings.SplashScreen.show = ShowUnitySplash;
            PlayerSettings.SplashScreen.showUnityLogo = ShowUnitySplash;
            PlayerSettings.SplashScreen.backgroundColor = BrandPetroleum;

            ApplyPipelineHdr(PipelineHdr);
            FontSetup.SetAtlasReadability(false); // memoria (2026-10-26): los atlas de las fuentes, sin copia en CPU

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
            Check(problems, "splashScreen.show", PlayerSettings.SplashScreen.show, ShowUnitySplash);
            Check(problems, "splashScreen.showUnityLogo", PlayerSettings.SplashScreen.showUnityLogo, ShowUnitySplash);
            Check(problems, "splashScreen.backgroundColor", (Color32)PlayerSettings.SplashScreen.backgroundColor, BrandPetroleum);
            foreach (RenderPipelineAsset asset in PipelineAssets())
            {
                bool? hdr = PipelineHdrOf(asset);
                if (hdr != PipelineHdr)
                {
                    problems.Add("pipeline.supportsHDR(" + asset.name + ")=" + (hdr.HasValue ? hdr.Value.ToString() : "?") + " (esperado " + PipelineHdr + ")");
                }
            }

            List<string> readableAtlases = FontSetup.ReadableAtlases();
            if (readableAtlases.Count > 0)
            {
                problems.Add("fonts.atlasReadable=" + string.Join(",", readableAtlases) + " (esperado ninguno)");
            }

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
                + ", keystorePropio=" + PlayerSettings.Android.useCustomKeystore
                + ", pantallaUnity=" + PlayerSettings.SplashScreen.show
                + ", logoUnity=" + PlayerSettings.SplashScreen.showUnityLogo
                + ", hdr=" + DescribeHdr();
        }

        /// <summary>
        /// Los assets de pipeline que usa el proyecto, sin repetir: el de Graphics y el de cada nivel de calidad (todos son el mismo
        /// <c>Assets/Settings/UniversalRP.asset</c> hoy).
        /// </summary>
        internal static List<RenderPipelineAsset> PipelineAssets()
        {
            var assets = new List<RenderPipelineAsset>();
            AddPipeline(assets, GraphicsSettings.defaultRenderPipeline);
            for (int i = 0; i < QualitySettings.count; i++)
            {
                AddPipeline(assets, QualitySettings.GetRenderPipelineAssetAt(i));
            }

            return assets;
        }

        /// <summary>El HDR del asset (<see cref="HdrProperty"/>), o null si no es un asset de URP.</summary>
        internal static bool? PipelineHdrOf(RenderPipelineAsset asset)
        {
            SerializedProperty? hdr = new SerializedObject(asset).FindProperty(HdrProperty);
            return hdr != null && hdr.propertyType == SerializedPropertyType.Boolean ? hdr.boolValue : (bool?)null;
        }

        // Pone el HDR de todos los assets de pipeline del proyecto (método versionado: nunca se edita el YAML a mano).
        static void ApplyPipelineHdr(bool on)
        {
            List<RenderPipelineAsset> assets = PipelineAssets();
            if (assets.Count == 0)
            {
                throw new InvalidOperationException(LogPrefix + "el proyecto no tiene asset de pipeline (URP) en Graphics ni en Quality.");
            }

            foreach (RenderPipelineAsset asset in assets)
            {
                var so = new SerializedObject(asset);
                SerializedProperty? hdr = so.FindProperty(HdrProperty);
                if (hdr == null || hdr.propertyType != SerializedPropertyType.Boolean)
                {
                    throw new InvalidOperationException(LogPrefix + asset.name + " no tiene " + HdrProperty + " (¿no es URP?).");
                }

                if (hdr.boolValue != on)
                {
                    Debug.Log(LogPrefix + asset.name + ": HDR " + hdr.boolValue + " -> " + on);
                    hdr.boolValue = on;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(asset);
                }
            }
        }

        static string DescribeHdr()
        {
            var parts = new List<string>();
            foreach (RenderPipelineAsset asset in PipelineAssets())
            {
                bool? hdr = PipelineHdrOf(asset);
                parts.Add(asset.name + ":" + (hdr.HasValue ? hdr.Value.ToString() : "?"));
            }

            return string.Join("/", parts);
        }

        static void AddPipeline(List<RenderPipelineAsset> assets, RenderPipelineAsset? asset)
        {
            if (asset != null && !assets.Contains(asset))
            {
                assets.Add(asset);
            }
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
