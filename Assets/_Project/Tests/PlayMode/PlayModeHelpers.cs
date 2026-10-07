#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace AF.Tests.PlayMode
{
    /// <summary>Carga de la escena Game y esperas con tiempo límite para los tests PlayMode.</summary>
    internal static class PlayModeHelpers
    {
        public const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        public const float TimeoutSeconds = 20f;

        public static IEnumerator LoadGame(int level, IProgressStore progress)
        {
            LaunchOptions.StartLevelOverride = level;
            LaunchOptions.ProgressStoreOverride = progress;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
#endif
            yield return WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current.Flow != null && GameBootstrap.Current.Flow.LevelNumber == level,
                "arranque de la escena Game con el nivel " + level);
        }

        public static IEnumerator WaitFor(System.Func<bool> condition, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > TimeoutSeconds)
                {
                    Assert.Fail("Tiempo agotado esperando: " + what);
                }

                yield return null;
            }
        }
    }
}
