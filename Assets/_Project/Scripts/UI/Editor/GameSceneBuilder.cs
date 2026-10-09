#nullable enable
using System;
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
    /// (<see cref="LevelPackPaths"/>); la UI se
    /// construye por código en tiempo de ejecución. Es la forma versionada de crear la escena (CLAUDE.md, regla dura 3).
    ///
    /// Uso en batchmode (sin ningún Editor abierto en la carpeta):
    ///   unity run &lt;repo&gt; --architecture arm64 --log-file Logs/game-scene.log --non-interactive
    ///             -- -nographics -executeMethod AF.UI.Editor.GameSceneBuilder.CreateGameScene
    /// Códigos de salida (solo en batchmode): 0 escena creada · 2 falta el paquete de niveles · 4 excepción.
    /// </summary>
    public static class GameSceneBuilder
    {
        /// <summary>Ruta de la escena.</summary>
        public const string ScenePath = "Assets/_Project/Scenes/Game.unity";

        /// <summary>Paquete de niveles del prototipo v0.</summary>
        public const string LevelPackPath = "Assets/_Project/Data/Levels/tutorial_v0.json";

        /// <summary>
        /// Paquetes de la escena, en orden (08 §2.2: índice nivel → paquete; tarea B1-5). En la integración se añaden aquí
        /// <c>mundo_02</c> … <c>mundo_05</c> y se regenera la escena.
        /// </summary>
        public static readonly string[] LevelPackPaths = { LevelPackPath };

        /// <summary>Catálogo de miquitos de la intro «Llenar y teñir» (<c>af-fauna/0</c>; guion de la intro §4.3).</summary>
        public const string FaunaCatalogPath = "Assets/_Project/Data/Fauna/miquitos_v0.json";

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
