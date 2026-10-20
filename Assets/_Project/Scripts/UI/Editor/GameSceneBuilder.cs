#nullable enable
using System;
using AF.Audio.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace AF.UI.Editor
{
    /// <summary>
    /// Genera la escena <c>Assets/_Project/Scenes/Game.unity</c> del prototipo v0 (docs/dev/prototipo-v0.md c) y la
    /// deja como la única de Build Settings. La escena es casi vacía (.claude/rules/unity-assets.md): cámara, EventSystem
    /// con <see cref="InputSystemUIInputModule"/> y <see cref="GameBootstrap"/> enlazado a los paquetes de niveles
    /// (<see cref="LevelPackPaths"/>), al catálogo de miquitos, a las paradas y a la geometría del Mapa del río, a la tabla de
    /// temporada (<see cref="SeasonTablePath"/>; tarea SE2), a las tablas de textos
    /// (<see cref="LocTables"/>; tarea L2b) y al registro de clips grabados (<see cref="AudioClipRegistry"/>; tarea AU3: lo genera
    /// antes <c>AF.EditorTools.AudioAssetSetup.Apply</c>); la UI se construye por código en tiempo de ejecución. Es la forma versionada de
    /// crear la escena (CLAUDE.md, regla dura 3).
    ///
    /// Uso en batchmode (sin ningún Editor abierto en la carpeta; <c>x86_64</c> en un equipo x64):
    ///   unity run &lt;repo&gt; --architecture arm64 --log-file Logs/game-scene.log --non-interactive
    ///             -- -nographics -executeMethod AF.UI.Editor.GameSceneBuilder.CreateGameScene
    /// Códigos de salida (solo en batchmode): 0 escena creada · 2 falta el paquete de niveles · 4 excepción.
    /// </summary>
    public static class GameSceneBuilder
    {
        /// <summary>Ruta de la escena.</summary>
        public const string ScenePath = "Assets/_Project/Scenes/Game.unity";

        /// <summary>Paquete de niveles del tutorial (niveles 1-10; prototipo v0).</summary>
        public const string LevelPackPath = "Assets/_Project/Data/Levels/tutorial_v0.json";

        /// <summary>
        /// Paquetes de la escena, en orden (08 §2.2: índice nivel → paquete; tarea B1-5; traspaso §6 pasos 1.1-1.2): el tutorial
        /// (1-10), la campaña 11-200 de <c>pack_schema</c> 2 (<c>mundo_02</c> 11-40, <c>mundo_03</c> 41-80, <c>mundo_04</c>
        /// 81-140 y <c>mundo_05</c> 141-200), el mundo 6, Oriente, de <c>pack_schema</c> 3 (<c>mundo_06</c> 201-300, generado
        /// por P7a; tarea C1a; 03 §11) y los mundos 7-9 (<c>mundo_07</c> 301-420, <c>mundo_08</c> 421-560 y <c>mundo_09</c>
        /// 561-700, generados por GEN79 con D-027; tarea C1b) en la variante publicada, la que escribe el generador en
        /// <c>Data/Levels/</c> con el <c>published_variant</c> de <c>campaign_v2.json</c> (hoy la «b», sin la Tinaja; D-019 Q-Ti3;
        /// <c>LevelCatalog.PublishedVariant</c>, que un test EditMode compara con la especificación). El primero va en
        /// <c>levelPack</c> y el resto en <c>extraLevelPacks</c>. El último nivel
        /// del último paquete es el fin de contenido: después, «El río sigue creciendo» (06 §9).
        /// </summary>
        public static readonly string[] LevelPackPaths =
        {
            LevelPackPath,
            "Assets/_Project/Data/Levels/mundo_02_v1.json",
            "Assets/_Project/Data/Levels/mundo_03_v1.json",
            "Assets/_Project/Data/Levels/mundo_04_v1.json",
            "Assets/_Project/Data/Levels/mundo_05_v1.json",
            "Assets/_Project/Data/Levels/mundo_06_v1.json",
            "Assets/_Project/Data/Levels/mundo_07_v1.json",
            "Assets/_Project/Data/Levels/mundo_08_v1.json",
            "Assets/_Project/Data/Levels/mundo_09_v1.json",
        };

        /// <summary>Catálogo de miquitos de la intro «Llenar y teñir» (<c>af-fauna/0</c>; guion de la intro §4.3).</summary>
        public const string FaunaCatalogPath = "Assets/_Project/Data/Fauna/miquitos_v0.json";

        /// <summary>Paradas del Mapa del río (<c>af-river-stops/1</c>; D-013; 08 §4.5; las genera <c>tools/river-map/stops.py</c>).</summary>
        public const string RiverStopsPath = "Assets/_Project/Data/Map/river_stops_v1.json";

        /// <summary>Pool del Reto del río (<c>kind: daily</c>, af-level/1; 06 §4.1; 08 §6.2; lo genera <c>tools/level-generator --daily</c>; enganche E3).</summary>
        public const string DailyPoolPath = "Assets/_Project/Data/Daily/daily_pool_v1.json";

        /// <summary>Geometría del Mapa del río (<c>af-river-map/1</c>; D-013; 08 §4.5; la genera <c>tools/river-map/geometry.py</c>; tarea M3.3).</summary>
        public const string RiverMapPath = "Assets/_Project/Data/Map/river_map_v1.json";

        /// <summary>Calendario de Frutas del río (<c>af-facts/1</c>; D-017; 08 §4.6; contrato 31).</summary>
        public const string FruitCalendarPath = "Assets/_Project/Data/Content/frutas_del_rio_v1.json";

        /// <summary>
        /// Tabla de temporada del cliente (<c>af-seasons/1</c>; pregunta 32 b, D-020; la genera <c>tools/level-generator --seasons</c>,
        /// tarea SE1; la LUT de aguas altas o bajas del fondo, tarea SE2).
        /// </summary>
        public const string SeasonTablePath = "Assets/_Project/Data/Levels/seasons_v1.json";

        private const string LogPrefix = "[AF.GameSceneBuilder] ";

        /// <summary>Crea (o recrea) la escena y la deja como única escena de Build Settings.</summary>
        [MenuItem("Amazon Fill/Escenas/Crear Game.unity")]
        public static void CreateGameScene()
        {
            int code;
            try
            {
                code = Run();
            }
            catch (Exception e)
            {
                Debug.LogError(LogPrefix + "excepción: " + e);
                code = 4;
            }

            if (code == 0)
            {
                Debug.Log(LogPrefix + "OK: " + ScenePath + " creada y única escena de Build Settings.");
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

        private static int Run()
        {
            AssetDatabase.Refresh();
            var packs = new TextAsset[LevelPackPaths.Length];
            for (int i = 0; i < LevelPackPaths.Length; i++)
            {
                packs[i] = AssetDatabase.LoadAssetAtPath<TextAsset>(LevelPackPaths[i]);
                if (packs[i] == null)
                {
                    Debug.LogError(LogPrefix + "no existe el paquete de niveles " + LevelPackPaths[i] + ".");
                    return 2;
                }
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning(LogPrefix + "cancelado: hay escenas sin guardar.");
                return 4;
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.15f, 0.12f, 1f);

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
            eventSystemGo.AddComponent<InputSystemUIInputModule>(); // sin acciones: toma las de por defecto al activarse

            var bootstrapGo = new GameObject("GameBootstrap");
            GameBootstrap bootstrap = bootstrapGo.AddComponent<GameBootstrap>();
            bootstrap.Configure(packs);
            TextAsset? fauna = AssetDatabase.LoadAssetAtPath<TextAsset>(FaunaCatalogPath);
            if (fauna == null)
            {
                Debug.LogWarning(LogPrefix + "no existe el catálogo de miquitos " + FaunaCatalogPath + ": la intro irá en modo dosel.");
            }

            bootstrap.ConfigureFauna(fauna);
            TextAsset? stops = AssetDatabase.LoadAssetAtPath<TextAsset>(RiverStopsPath);
            if (stops == null)
            {
                Debug.LogWarning(LogPrefix + "no existen las paradas del Mapa del río " + RiverStopsPath + ": el juego seguirá sin el Mapa.");
            }

            bootstrap.ConfigureRiverStops(stops);
            TextAsset? dailyPool = AssetDatabase.LoadAssetAtPath<TextAsset>(DailyPoolPath);
            if (dailyPool == null)
            {
                Debug.LogWarning(LogPrefix + "no existe el pool del Reto del río " + DailyPoolPath + ": el juego seguirá sin el Reto (tarjeta oculta).");
            }

            bootstrap.ConfigureDailyPool(dailyPool);
            TextAsset? riverMap = AssetDatabase.LoadAssetAtPath<TextAsset>(RiverMapPath);
            if (riverMap == null)
            {
                Debug.LogWarning(LogPrefix + "no existe la geometría del Mapa del río " + RiverMapPath + ": el juego seguirá sin la pantalla del Mapa.");
            }

            bootstrap.ConfigureRiverMap(riverMap);
            TextAsset? fruits = AssetDatabase.LoadAssetAtPath<TextAsset>(FruitCalendarPath);
            if (fruits == null)
            {
                Debug.LogWarning(LogPrefix + "no existe el calendario de Frutas del río " + FruitCalendarPath + ": el juego seguirá sin líneas de fruta.");
            }

            bootstrap.ConfigureFruitCalendar(fruits);
            TextAsset? seasons = AssetDatabase.LoadAssetAtPath<TextAsset>(SeasonTablePath);
            if (seasons == null)
            {
                Debug.LogWarning(LogPrefix + "no existe la tabla de temporada " + SeasonTablePath + ": el juego seguirá sin LUT de temporada.");
            }

            bootstrap.ConfigureSeasons(seasons);

            // Tablas de textos (D-018; 08 §4.7): sin ellas el juego arranca, avisa y va sin textos (GameBootstrap).
            TextAsset? strings = AssetDatabase.LoadAssetAtPath<TextAsset>(LocTables.StringsAssetPath);
            TextAsset? facts = AssetDatabase.LoadAssetAtPath<TextAsset>(LocTables.FactsAssetPath);
            if (strings == null || facts == null)
            {
                Debug.LogWarning(LogPrefix + "no existen las tablas de textos " + LocTables.StringsAssetPath + " y " + LocTables.FactsAssetPath + ": la UI irá sin textos.");
            }

            bootstrap.ConfigureLocalization(strings, facts);

            // Clips grabados de la entrega 1 (tarea AU3): sin el registro el juego suena con el audio por código de respaldo.
            AudioClipRegistry? audio = AssetDatabase.LoadAssetAtPath<AudioClipRegistry>(AudioClipRegistry.AssetPath);
            if (audio == null)
            {
                Debug.LogWarning(LogPrefix + "no existe el registro de clips " + AudioClipRegistry.AssetPath + " (AF.EditorTools.AudioAssetSetup.Apply): el juego sonará con el audio por código.");
            }

            bootstrap.ConfigureAudio(audio);
            EditorUtility.SetDirty(bootstrap);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError(LogPrefix + "no se pudo guardar " + ScenePath + ".");
                return 4;
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            return 0;
        }
    }
}
