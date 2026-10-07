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

        /// <summary>Paso fijo del reloj manual (≈60 fps, 08 §5.4).</summary>
        public const int FrameMs = 16;

        /// <summary>
        /// Quita el reloj a <see cref="GameBootstrap"/> (deja de llamar a <c>Update</c>) para que el test avance la vista y
        /// el flujo con <see cref="Step"/>: los tiempos de animación no dependen de los fps del Editor en batchmode.
        /// </summary>
        public static void TakeClock(GameBootstrap boot)
        {
            boot.enabled = false;
        }

        /// <summary>Avanza la vista y el flujo <paramref name="ms"/> milisegundos en pasos de <see cref="FrameMs"/>, como <c>GameBootstrap.Update</c>.</summary>
        public static void Step(GameBootstrap boot, int ms)
        {
            for (int elapsed = 0; elapsed < ms; elapsed += FrameMs)
            {
                boot.View!.Tick(FrameMs);
                boot.Flow!.Tick(FrameMs, boot.View.IsViewIdle);
            }
        }

        /// <summary>Avanza hasta que la cola visual se vacía y devuelve los milisegundos que tardó; falla pasado <paramref name="maxMs"/>.</summary>
        public static int StepUntilIdle(GameBootstrap boot, int maxMs)
        {
            int elapsed = 0;
            while (!boot.View!.IsViewIdle)
            {
                if (elapsed > maxMs)
                {
                    Assert.Fail("La cola visual no se vació en " + maxMs + " ms.");
                }

                Step(boot, FrameMs);
                elapsed += FrameMs;
            }

            return elapsed;
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
