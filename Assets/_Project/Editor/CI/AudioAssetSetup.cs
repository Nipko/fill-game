#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using AF.Audio;
using AF.Audio.Unity;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AF.EditorTools
{
    /// <summary>
    /// Audio definitivo de la entrega 1 en el proyecto (tarea AU3; encargo de sonidos §11; 08 §6): la forma versionada de fijar los
    /// ajustes de importación de los 173 WAV de <c>Assets/_Project/Audio/</c> y de generar el registro de clips
    /// (<see cref="AudioClipRegistry"/>, que la escena cuelga de <c>GameBootstrap</c>). Nunca se edita YAML a mano: los
    /// <c>.meta</c> y el <c>.asset</c> los escribe Unity. Idempotente: si todo ya está como debe, no toca nada.
    ///
    /// Ajustes por clase (<see cref="AudioClipIds.ClassOf"/>), todos con «Load In Background», sin «Normalize» (con «Force To
    /// Mono», Unity llevaría cada clip a 0 dBFS y se perdería la mezcla de la entrega: <c>sfx_select</c> va a −36 dBFS) y sin
    /// sustituciones por plataforma:
    /// <list type="bullet">
    /// <item><description>Efectos, notas de fruta, bucles del vertido y fauna: mono, «Decompress On Load», Vorbis de calidad
    /// <see cref="EffectQuality"/>, precargados. Vorbis y no ADPCM: ADPCM comprime fijo a ~3,5:1 con 4 bits por muestra y su
    /// ruido de cuantificación se oye como un siseo sobre los tonos puros que se apagan (las 28 notas de fruta a −36 y −30 dBFS,
    /// la marimba de <c>sfx_select</c>); Vorbis a calidad alta no deja artefactos audibles y pesa menos (~6:1 en mono a 48 kHz).
    /// Al descomprimirse al cargar, sonar no cuesta CPU.</description></item>
    /// <item><description>Agua, lluvia y tema de menú: «Streaming», Vorbis q<see cref="MusicQuality"/> (08 §6), estéreo si el WAV
    /// lo es; sin precarga.</description></item>
    /// <item><description>Lecho, capas y variante nocturna de cada mundo: «Compressed In Memory», Vorbis q<see cref="MusicQuality"/>,
    /// sin precarga (el servicio los carga al pedir el mundo). No en streaming: las 4 piezas arrancan con <c>PlayScheduled</c> en el
    /// mismo instante y tienen que repetirse juntas a la muestra, y el streaming no lo garantiza (cada flujo empieza cuando su
    /// búfer se llena y la vuelta del bucle relee del disco).</description></item>
    /// <item><description>Remates: «Compressed In Memory», Vorbis q<see cref="MusicQuality"/>, precargados (son cortos y suenan
    /// sin espera).</description></item>
    /// </list>
    ///
    /// Uso en batchmode (sin ningún Editor abierto en la carpeta; <c>x86_64</c> en un equipo x64):
    ///   unity run &lt;repo&gt; --architecture x86_64 --log-file Logs/audio-setup.log --non-interactive
    ///             -- -nographics -executeMethod AF.EditorTools.AudioAssetSetup.Apply
    /// y después <c>AF.UI.Editor.GameSceneBuilder.CreateGameScene</c>, que cuelga el registro de la escena.
    /// Códigos de salida de <see cref="Apply"/> (solo en batchmode): 0 hecho · 2 falta algún WAV de la especificación · 4 excepción.
    ///
    /// <see cref="BuildGameAab"/> mide el peso del audio en un AAB de la escena del juego (el de humo, <see cref="BuildSmoke"/>,
    /// usa una escena mínima y no lleva el audio): <c>Builds/Android/game.aab</c> y el informe por carpeta en
    /// <see cref="SizeReportPath"/>. Códigos: 0 hecho · 2 Player Settings distintos · 3 sin módulo Android · 4 build fallido ·
    /// 5 sin la escena del juego en Build Settings · 6 compuerta de textos.
    /// </summary>
    public static class AudioAssetSetup
    {
        /// <summary>Calidad Vorbis de los efectos, notas, bucles del vertido y fauna (0-1; «calidad alta»).</summary>
        public const float EffectQuality = 0.8f;

        /// <summary>Calidad Vorbis del agua, la lluvia, la música y los remates (0-1; 08 §6: «Música Vorbis q0,4»).</summary>
        public const float MusicQuality = 0.4f;

        /// <summary>AAB de la escena del juego para medir el peso del audio.</summary>
        public const string GameAabPath = BuildSmoke.OutputDir + "/game.aab";

        /// <summary>Informe del peso del audio en el build (Logs/ está ignorada por git).</summary>
        public const string SizeReportPath = "Logs/audio-size.md";

        private const string LogPrefix = "[AF.AudioAssetSetup] ";
        private const int ExitOk = 0;
        private const int ExitMissing = 2;
        private const int ExitSettings = 2;
        private const int ExitPlatform = 3;
        private const int ExitFailed = 4;
        private const int ExitNoGameScene = 5;
        private const int ExitLocGate = 6;

        private static readonly string[] OverridePlatforms = { "Android", "Standalone", "iOS", "WebGL" };

        /// <summary>Ajustes de importación que debe tener un clip de la clase <paramref name="cls"/>.</summary>
        public static AudioImportExpectation Expected(AudioClipClass cls)
        {
            switch (cls)
            {
                case AudioClipClass.Effect:
                    return new AudioImportExpectation(true, AudioClipLoadType.DecompressOnLoad, EffectQuality, true);
                case AudioClipClass.Stream:
                    return new AudioImportExpectation(false, AudioClipLoadType.Streaming, MusicQuality, false);
                case AudioClipClass.MusicLayer:
                    return new AudioImportExpectation(false, AudioClipLoadType.CompressedInMemory, MusicQuality, false);
                default:
                    return new AudioImportExpectation(false, AudioClipLoadType.CompressedInMemory, MusicQuality, true);
            }
        }

        /// <summary>Fija los ajustes de importación de los 173 WAV y genera (o pone al día) el registro de clips.</summary>
        [MenuItem("Amazon Fill/Audio/Importar el audio definitivo y generar el registro")]
        public static void Apply()
        {
            int code;
            try
            {
                code = Run();
            }
            catch (Exception e)
            {
                Debug.LogError(LogPrefix + "excepción: " + e);
                code = ExitFailed;
            }

            if (code == ExitOk)
            {
                Debug.Log(LogPrefix + "OK: ajustes de importación y " + AudioClipRegistry.AssetPath + " al día.");
            }
            else
            {
                Debug.LogError(LogPrefix + "FALLO (código " + code + ").");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }

        /// <summary>
        /// Lo que no está como debe (tests): WAV que faltan, ajustes de importación distintos de <see cref="Expected"/> y ranuras
        /// del registro sin su clip. Vacío si todo está bien.
        /// </summary>
        public static List<string> Problems()
        {
            var problems = new List<string>();
            for (int slot = 0; slot < AudioClipIds.Count; slot++)
            {
                string path = AudioClipIds.AssetPath(slot);
                if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
                {
                    problems.Add(path + ": no existe o no es audio");
                    continue;
                }

                string? diff = Difference(importer, Expected(AudioClipIds.ClassOf(slot)));
                if (diff != null)
                {
                    problems.Add(path + ": " + diff);
                }
            }

            AudioClipRegistry? registry = AssetDatabase.LoadAssetAtPath<AudioClipRegistry>(AudioClipRegistry.AssetPath);
            if (registry == null)
            {
                problems.Add(AudioClipRegistry.AssetPath + ": no existe");
                return problems;
            }

            for (int slot = 0; slot < AudioClipIds.Count; slot++)
            {
                AudioClip? clip = registry.Get(slot);
                string expected = AudioClipIds.AssetPath(slot);
                if (clip == null)
                {
                    problems.Add(AudioClipIds.Id(slot) + ": sin clip en el registro");
                }
                else if (!string.Equals(AssetDatabase.GetAssetPath(clip), expected, StringComparison.Ordinal))
                {
                    problems.Add(AudioClipIds.Id(slot) + ": el registro apunta a " + AssetDatabase.GetAssetPath(clip) + " y no a " + expected);
                }
            }

            return problems;
        }

        private static int Run()
        {
            AssetDatabase.Refresh();
            var missing = new List<string>();
            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int slot = 0; slot < AudioClipIds.Count; slot++)
                {
                    string path = AudioClipIds.AssetPath(slot);
                    if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
                    {
                        missing.Add(path);
                        continue;
                    }

                    if (Configure(importer, Expected(AudioClipIds.ClassOf(slot))))
                    {
                        importer.SaveAndReimport();
                        changed++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (missing.Count > 0)
            {
                Debug.LogError(LogPrefix + "faltan " + missing.Count + " WAV de la especificación: " + string.Join(", ", missing));
                return ExitMissing;
            }

            Debug.Log(LogPrefix + "ajustes de importación cambiados en " + changed + " de " + AudioClipIds.Count + " WAV.");
            var ids = new string[AudioClipIds.Count];
            var clips = new AudioClip[AudioClipIds.Count];
            for (int slot = 0; slot < AudioClipIds.Count; slot++)
            {
                ids[slot] = AudioClipIds.Id(slot);
                AudioClip? clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioClipIds.AssetPath(slot));
                if (clip == null)
                {
                    Debug.LogError(LogPrefix + "no se pudo cargar el clip " + AudioClipIds.AssetPath(slot) + ".");
                    return ExitMissing;
                }

                clips[slot] = clip;
            }

            AudioClipRegistry? registry = AssetDatabase.LoadAssetAtPath<AudioClipRegistry>(AudioClipRegistry.AssetPath);
            if (registry == null)
            {
                registry = AudioClipRegistry.Create(ids, clips);
                AssetDatabase.CreateAsset(registry, AudioClipRegistry.AssetPath);
                Debug.Log(LogPrefix + "registro creado: " + AudioClipRegistry.AssetPath + " (" + ids.Length + " clips).");
            }
            else if (!SameContent(registry, ids, clips))
            {
                registry.Assign(ids, clips);
                EditorUtility.SetDirty(registry);
                Debug.Log(LogPrefix + "registro puesto al día: " + AudioClipRegistry.AssetPath + " (" + ids.Length + " clips).");
            }
            else
            {
                Debug.Log(LogPrefix + "el registro ya estaba al día.");
            }

            AssetDatabase.SaveAssets();
            List<string> problems = Problems();
            if (problems.Count > 0)
            {
                Debug.LogError(LogPrefix + "tras aplicar siguen " + problems.Count + " problemas: " + string.Join("; ", problems));
                return ExitFailed;
            }

            return ExitOk;
        }

        private static bool SameContent(AudioClipRegistry registry, string[] ids, AudioClip[] clips)
        {
            if (registry.Ids.Length != ids.Length || registry.Clips.Length != clips.Length)
            {
                return false;
            }

            for (int i = 0; i < ids.Length; i++)
            {
                if (!string.Equals(registry.Ids[i], ids[i], StringComparison.Ordinal) || registry.Clips[i] != clips[i])
                {
                    return false;
                }
            }

            return true;
        }

        // Pone los ajustes esperados; true si cambió algo (y hay que reimportar).
        private static bool Configure(AudioImporter importer, AudioImportExpectation want)
        {
            if (Difference(importer, want) == null)
            {
                return false;
            }

            importer.forceToMono = want.ForceToMono;
            importer.loadInBackground = true;
            importer.ambisonic = false;
            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            s.loadType = want.LoadType;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = want.Quality;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            s.preloadAudioData = want.Preload;
            importer.defaultSampleSettings = s;
            for (int i = 0; i < OverridePlatforms.Length; i++)
            {
                if (importer.ContainsSampleSettingsOverride(OverridePlatforms[i]))
                {
                    importer.ClearSampleSettingOverride(OverridePlatforms[i]);
                }
            }

            // «Normalize» no tiene propiedad pública en AudioImporter: se apaga con su SerializedObject (lo guarda Unity en el .meta).
            var so = new SerializedObject(importer);
            SerializedProperty? normalize = so.FindProperty(NormalizeProperty);
            if (normalize != null && normalize.boolValue)
            {
                normalize.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return true;
        }

        /// <summary>El importador normaliza el clip al pasarlo a mono («Normalize»; propiedad serializada <c>normalize</c>).</summary>
        public static bool Normalizes(AudioImporter importer)
        {
            var so = new SerializedObject(importer);
            SerializedProperty? normalize = so.FindProperty(NormalizeProperty);
            return normalize != null && normalize.boolValue;
        }

        private const string NormalizeProperty = "m_Normalize";

        // Qué difiere de lo esperado (null si nada).
        private static string? Difference(AudioImporter importer, AudioImportExpectation want)
        {
            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            var sb = new StringBuilder();
            if (importer.forceToMono != want.ForceToMono)
            {
                sb.Append(" forceToMono=").Append(importer.forceToMono);
            }

            if (!importer.loadInBackground)
            {
                sb.Append(" loadInBackground=false");
            }

            if (importer.ambisonic)
            {
                sb.Append(" ambisonic=true");
            }

            if (Normalizes(importer))
            {
                sb.Append(" normalize=true");
            }

            if (s.loadType != want.LoadType)
            {
                sb.Append(" loadType=").Append(s.loadType);
            }

            if (s.compressionFormat != AudioCompressionFormat.Vorbis)
            {
                sb.Append(" compressionFormat=").Append(s.compressionFormat);
            }

            if (Math.Abs(s.quality - want.Quality) > 0.001f)
            {
                sb.Append(" quality=").Append(s.quality.ToString("0.00", CultureInfo.InvariantCulture));
            }

            if (s.sampleRateSetting != AudioSampleRateSetting.PreserveSampleRate)
            {
                sb.Append(" sampleRateSetting=").Append(s.sampleRateSetting);
            }

            if (s.preloadAudioData != want.Preload)
            {
                sb.Append(" preloadAudioData=").Append(s.preloadAudioData);
            }

            for (int i = 0; i < OverridePlatforms.Length; i++)
            {
                if (importer.ContainsSampleSettingsOverride(OverridePlatforms[i]))
                {
                    sb.Append(" override=").Append(OverridePlatforms[i]);
                }
            }

            return sb.Length == 0 ? null : sb.ToString().Trim();
        }

        /// <summary>
        /// Construye <see cref="GameAabPath"/> (AAB de release con la escena del juego, firmado con la clave de depuración, nunca
        /// para Play) y escribe en <see cref="SizeReportPath"/> el peso del audio en el build por carpeta (Build Report).
        /// </summary>
        [MenuItem("Amazon Fill/Audio/Medir el peso del audio en un AAB del juego")]
        public static void BuildGameAab()
        {
            int code;
            try
            {
                code = RunBuild();
            }
            catch (Exception e)
            {
                Debug.LogError(LogPrefix + "excepción: " + e);
                code = ExitFailed;
            }

            if (code != ExitOk)
            {
                Debug.LogError(LogPrefix + "FALLO del AAB del juego (código " + code + ").");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }

        private static int RunBuild()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError(LogPrefix + "este Editor no tiene el módulo Android Build Support.");
                return ExitPlatform;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android
                && !EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android))
            {
                Debug.LogError(LogPrefix + "no se pudo cambiar la plataforma activa a Android.");
                return ExitPlatform;
            }

            if (!LocReleaseGate.Check(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android), LogPrefix))
            {
                return ExitLocGate;
            }

            List<string> problems = ProjectSetup.Validate();
            if (problems.Count > 0)
            {
                Debug.LogError(LogPrefix + "Player Settings distintos de los fijados: " + string.Join("; ", problems) + ".");
                return ExitSettings;
            }

            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                {
                    scenes.Add(scene.path);
                }
            }

            if (!scenes.Contains(BuildDev.GameScenePath))
            {
                Debug.LogError(LogPrefix + "Build Settings no incluye " + BuildDev.GameScenePath + ".");
                return ExitNoGameScene;
            }

            bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
            Directory.CreateDirectory(BuildSmoke.OutputDir);
            try
            {
                EditorUserBuildSettings.buildAppBundle = true;
                if (File.Exists(GameAabPath))
                {
                    File.Delete(GameAabPath);
                }

                var options = new BuildPlayerOptions
                {
                    scenes = scenes.ToArray(),
                    locationPathName = GameAabPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None,
                };

                Debug.Log(LogPrefix + "construyendo " + GameAabPath + " ...");
                Stopwatch watch = Stopwatch.StartNew();
                BuildReport report = BuildPipeline.BuildPlayer(options);
                watch.Stop();
                BuildSummary summary = report.summary;
                bool ok = summary.result == BuildResult.Succeeded && File.Exists(GameAabPath);
                long bytes = ok ? new FileInfo(GameAabPath).Length : 0L;
                Debug.Log(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}AAB {1}: resultado={2}, errores={3}, avisos={4}, tiempo={5:F1} s, tamano={6:F2} MB",
                    LogPrefix, GameAabPath, summary.result, summary.totalErrors, summary.totalWarnings, watch.Elapsed.TotalSeconds, bytes / 1000000.0));
                if (!ok)
                {
                    return ExitFailed;
                }

                WriteSizeReport(report, bytes);
                return ExitOk;
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = previousAppBundle;
            }
        }

        // Peso empaquetado del audio por carpeta de la entrega (Build Report, packedAssets), frente al total empaquetado y al AAB.
        private static void WriteSizeReport(BuildReport report, long aabBytes)
        {
            var byFolder = new SortedDictionary<string, long>(StringComparer.Ordinal);
            var countByFolder = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var counted = new HashSet<string>(StringComparer.Ordinal); // cada clip sale dos veces: el objeto y sus datos
            long audio = 0;
            long all = 0;
            foreach (PackedAssets packed in report.packedAssets)
            {
                foreach (PackedAssetInfo info in packed.contents)
                {
                    all += (long)info.packedSize;
                    string path = info.sourceAssetPath ?? string.Empty;
                    if (!path.StartsWith(AudioClipIds.Folder, StringComparison.Ordinal) || info.type != typeof(AudioClip))
                    {
                        continue;
                    }

                    string rel = path.Substring(AudioClipIds.Folder.Length);
                    int slash = rel.LastIndexOf('/');
                    string folder = slash < 0 ? "." : rel.Substring(0, slash);
                    byFolder.TryGetValue(folder, out long sum);
                    byFolder[folder] = sum + (long)info.packedSize;
                    countByFolder.TryGetValue(folder, out int n);
                    countByFolder[folder] = counted.Add(path) ? n + 1 : n;
                    audio += (long)info.packedSize;
                }
            }

            var sb = new StringBuilder();
            sb.Append("# Peso del audio en el AAB del juego (AF.EditorTools.AudioAssetSetup.BuildGameAab)\n\n");
            sb.Append("| Carpeta | Clips | MB |\n| --- | ---: | ---: |\n");
            foreach (KeyValuePair<string, long> pair in byFolder)
            {
                sb.Append("| `").Append(pair.Key).Append("` | ").Append(countByFolder[pair.Key].ToString(CultureInfo.InvariantCulture))
                    .Append(" | ").Append((pair.Value / 1000000.0).ToString("0.00", CultureInfo.InvariantCulture)).Append(" |\n");
            }

            sb.Append("| **Audio** | | **").Append((audio / 1000000.0).ToString("0.00", CultureInfo.InvariantCulture)).Append("** |\n\n");
            sb.Append("Datos empaquetados en total: ").Append((all / 1000000.0).ToString("0.00", CultureInfo.InvariantCulture))
                .Append(" MB. AAB: ").Append((aabBytes / 1000000.0).ToString("0.00", CultureInfo.InvariantCulture)).Append(" MB (1 MB = 1.000.000 bytes).\n");
            Directory.CreateDirectory(Path.GetDirectoryName(SizeReportPath)!);
            File.WriteAllText(SizeReportPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log(LogPrefix + "peso del audio:\n" + sb);
        }
    }

    /// <summary>Ajustes de importación esperados de una clase de clips (<see cref="AudioAssetSetup.Expected"/>).</summary>
    public readonly struct AudioImportExpectation
    {
        /// <summary>Crea la expectativa.</summary>
        public AudioImportExpectation(bool forceToMono, AudioClipLoadType loadType, float quality, bool preload)
        {
            ForceToMono = forceToMono;
            LoadType = loadType;
            Quality = quality;
            Preload = preload;
        }

        /// <summary>Se fuerza a mono (efectos); el resto conserva sus canales.</summary>
        public bool ForceToMono { get; }

        /// <summary>Cómo se carga.</summary>
        public AudioClipLoadType LoadType { get; }

        /// <summary>Calidad Vorbis (0-1).</summary>
        public float Quality { get; }

        /// <summary>Se precarga con la escena.</summary>
        public bool Preload { get; }
    }
}
