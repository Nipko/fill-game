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

        /// <summary>
        /// Android Blit Type en <c>Auto</c> (memoria, 2026-10-29; observación 6 de la jornada 6 de la prueba completa, bloque 19;
        /// <c>docs/dev/memoria.md</c>). Con <c>Always</c> Unity pinta cada fotograma en un búfer de color y profundidad fuera de la
        /// pantalla, del tamaño de la pantalla, y lo copia a la superficie de Android: unos 20 MiB de <c>GL mtrack</c> a 1080 × 2340.
        /// Con <c>Auto</c> solo lo hace cuando hace falta (escala de resolución, que el juego no usa); el fotograma es el mismo. Lo
        /// comprueba el portátil en el teléfono (guion, bloque 19).
        /// </summary>
        public const AndroidBlitType BlitType = AndroidBlitType.Auto;

        /// <summary>
        /// El 2D Renderer sin <c>PostProcessData</c> (memoria, 2026-10-29; observación 14 de la jornada 6; <c>docs/dev/memoria.md</c> §7).
        /// La cámara del juego no pinta posproceso (sin <c>UniversalAdditionalCameraData</c> serializado: <c>renderPostProcessing</c>
        /// a false) y el Bloom del 2D Renderer está prohibido (UUM-150232), pero el asset de datos de posproceso seguía referenciado
        /// por el renderer, así que el reproductor cargaba al arrancar sus 10 texturas de grano (FilmGrain), las de SMAA y los shaders
        /// <c>UberPost</c>, <c>Bloom</c>, <c>FinalPost</c> y compañía (≈ 3-4 MB entre texturas y shaders, también en el APK). URP 2D
        /// comprueba el null (<c>Renderer2DRendergraph</c>: sin datos no crea los pases de posproceso ni la LUT), así que la imagen no
        /// cambia. Se quita de cada <c>Renderer2DData</c> de los assets de pipeline del proyecto con su propiedad serializada.
        /// </summary>
        public const bool RendererPostProcessData = false;

        /// <summary>
        /// APIs gráficas de Android, en orden de preferencia (añadido de D-060 del 2026-10-29; observaciones 14 y 20 de la jornada 6;
        /// <c>docs/dev/memoria.md</c> §7.8): <b>OpenGL ES 3 primero y Vulkan de respaldo</b>, en lugar de la lista automática de Unity
        /// (que en un Adreno 830 elegía Vulkan). En el S938B, <c>-force-gles</c> bajó el PSS 51 MB en el <c>dev.apk</c> y 36 MB en el
        /// ensayo de release (sobre todo <c>GL mtrack</c> y Private Other), con la misma imagen. Vulkan se queda en la lista: el teléfono
        /// que no tenga OpenGL ES 3 (raro con minSdk 26) arranca con Vulkan, y el portátil puede comparar con el mismo APK lanzándolo con
        /// <c>-e unity "-force-vulkan"</c>.
        /// <para>Condición de D-060: si en el teléfono empeoran el Morpho (<c>AF_PERF morpho</c>), <c>motion_pauses</c> o
        /// <c>frame_max_ms</c>, se vuelve a Vulkan. <b>Cómo revertir:</b> <see cref="UseDefaultGraphicsApis"/> a true (la lista
        /// automática de Unity, con Vulkan primero) o esta lista con <c>GraphicsDeviceType.Vulkan</c> delante; después
        /// <c>pwsh -NoProfile -File scripts/unity-build.ps1 -Method Setup</c> y commitear <c>ProjectSettings/ProjectSettings.asset</c> con
        /// el test de <c>MemorySettingsTests</c>.</para>
        /// </summary>
        public static readonly GraphicsDeviceType[] AndroidGraphicsApis = { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.Vulkan };

        /// <summary>false: la lista de <see cref="AndroidGraphicsApis"/>, no la automática de Unity (añadido de D-060 del 2026-10-29).</summary>
        public const bool UseDefaultGraphicsApis = false;

        /// <summary>Propiedad serializada de los datos de posproceso del 2D Renderer (<c>Renderer2DData.postProcessData</c>).</summary>
        internal const string PostProcessDataProperty = "m_PostProcessData";

        /// <summary>Lista de renderers del asset de URP (<c>UniversalRenderPipelineAsset.m_RendererDataList</c>).</summary>
        internal const string RendererListProperty = "m_RendererDataList";

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
            PlayerSettings.Android.blitType = BlitType; // memoria (2026-10-29): sin búfer intermedio de pantalla completa
            ApplyRendererPostProcessData(RendererPostProcessData); // memoria (2026-10-29, obs. 14 de la j6): sin los recursos de posproceso
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, UseDefaultGraphicsApis); // añadido de D-060: OpenGL ES 3 primero
            if (!UseDefaultGraphicsApis)
            {
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, AndroidGraphicsApis);
            }
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
            Check(problems, "android.blitType", PlayerSettings.Android.blitType, BlitType);
            foreach (RenderPipelineAsset asset in PipelineAssets())
            {
                bool? hdr = PipelineHdrOf(asset);
                if (hdr != PipelineHdr)
                {
                    problems.Add("pipeline.supportsHDR(" + asset.name + ")=" + (hdr.HasValue ? hdr.Value.ToString() : "?") + " (esperado " + PipelineHdr + ")");
                }
            }

            Check(problems, "android.useDefaultGraphicsAPIs", PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android), UseDefaultGraphicsApis);
            if (!UseDefaultGraphicsApis)
            {
                string apis = DescribeGraphicsApis();
                string expected = string.Join(">", AndroidGraphicsApis);
                if (!string.Equals(apis, expected, StringComparison.Ordinal))
                {
                    problems.Add("android.graphicsAPIs=" + apis + " (esperado " + expected + ")");
                }
            }

            foreach (ScriptableObject renderer in RendererDatas())
            {
                bool? hasPostProcess = RendererHasPostProcessData(renderer);
                if (hasPostProcess != RendererPostProcessData)
                {
                    problems.Add("renderer.postProcessData(" + renderer.name + ")=" + (hasPostProcess.HasValue ? hasPostProcess.Value.ToString() : "?")
                        + " (esperado " + RendererPostProcessData + ")");
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
                + ", hdr=" + DescribeHdr()
                + ", blit=" + PlayerSettings.Android.blitType
                + ", posproceso=" + DescribeRendererPostProcess()
                + ", graficos=" + (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) ? "auto" : DescribeGraphicsApis());
        }

        /// <summary>Las APIs gráficas de Android que tiene el proyecto, en orden, como «OpenGLES3&gt;Vulkan».</summary>
        internal static string DescribeGraphicsApis()
        {
            return string.Join(">", PlayerSettings.GetGraphicsAPIs(BuildTarget.Android));
        }

        /// <summary>
        /// Los assets de datos de los renderers (<see cref="RendererListProperty"/>) de todos los assets de pipeline del proyecto, sin
        /// repetir (hoy, solo <c>Assets/Settings/Renderer2D.asset</c>).
        /// </summary>
        internal static List<ScriptableObject> RendererDatas()
        {
            var renderers = new List<ScriptableObject>();
            foreach (RenderPipelineAsset asset in PipelineAssets())
            {
                SerializedProperty? list = new SerializedObject(asset).FindProperty(RendererListProperty);
                if (list == null || !list.isArray)
                {
                    continue;
                }

                for (int i = 0; i < list.arraySize; i++)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue is ScriptableObject renderer && !renderers.Contains(renderer))
                    {
                        renderers.Add(renderer);
                    }
                }
            }

            return renderers;
        }

        /// <summary>
        /// Si el renderer tiene datos de posproceso (<see cref="PostProcessDataProperty"/>), o null si no es un 2D Renderer (o un
        /// renderer sin esa propiedad).
        /// </summary>
        internal static bool? RendererHasPostProcessData(ScriptableObject renderer)
        {
            SerializedProperty? data = new SerializedObject(renderer).FindProperty(PostProcessDataProperty);
            return data != null && data.propertyType == SerializedPropertyType.ObjectReference ? data.objectReferenceValue != null : (bool?)null;
        }

        // Quita (o deja) los datos de posproceso de los renderers del proyecto (método versionado: nunca se edita el YAML a mano).
        // Solo quitar: si alguna vez hiciera falta volver a ponerlos, se asignan en el Editor (Renderer2D › Post-processing › Data).
        static void ApplyRendererPostProcessData(bool hasPostProcessData)
        {
            List<ScriptableObject> renderers = RendererDatas();
            if (renderers.Count == 0)
            {
                throw new InvalidOperationException(LogPrefix + "los assets de pipeline no tienen renderers (" + RendererListProperty + ").");
            }

            foreach (ScriptableObject renderer in renderers)
            {
                var so = new SerializedObject(renderer);
                SerializedProperty? data = so.FindProperty(PostProcessDataProperty);
                if (data == null || data.propertyType != SerializedPropertyType.ObjectReference)
                {
                    throw new InvalidOperationException(LogPrefix + renderer.name + " no tiene " + PostProcessDataProperty + " (¿no es el 2D Renderer?).");
                }

                if (!hasPostProcessData && data.objectReferenceValue != null)
                {
                    Debug.Log(LogPrefix + renderer.name + ": postProcessData " + data.objectReferenceValue.name + " -> null");
                    data.objectReferenceValue = null;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(renderer);
                }
                else if (hasPostProcessData && data.objectReferenceValue == null)
                {
                    throw new InvalidOperationException(LogPrefix + renderer.name + ": sin datos de posproceso; asígnalos en el Editor.");
                }
            }
        }

        static string DescribeRendererPostProcess()
        {
            var parts = new List<string>();
            foreach (ScriptableObject renderer in RendererDatas())
            {
                bool? has = RendererHasPostProcessData(renderer);
                parts.Add(renderer.name + ":" + (has.HasValue ? has.Value.ToString() : "?"));
            }

            return string.Join("/", parts);
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
