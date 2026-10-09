#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Save;
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

        /// <summary>
        /// Carga la escena Game directamente en el nivel <paramref name="level"/> (gancho <see cref="LaunchOptions.StartLevelOverride"/>)
        /// con el progreso <paramref name="progress"/>. Si el test no puso un perfil (<see cref="LaunchOptions.ProfileStoreOverride"/>),
        /// usa uno en memoria: los tests nunca tocan el <c>profile.json</c> del Editor.
        /// </summary>
        public static IEnumerator LoadGame(int level, IProgressStore progress)
        {
            LaunchOptions.StartLevelOverride = level;
            LaunchOptions.ProgressStoreOverride = progress;
            if (LaunchOptions.ProfileStoreOverride == null)
            {
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore();
            }

            yield return LoadScene();
            yield return WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current.Flow != null && GameBootstrap.Current.Flow.LevelNumber == level,
                "arranque de la escena Game con el nivel " + level);
        }

        /// <summary>
        /// Carga la escena Game sin nivel forzado (como el juego: la primera vez el nivel 1, si no la pantalla de inicio) con
        /// el perfil <paramref name="store"/> y espera a que <see cref="GameBootstrap.App"/> haya decidido la primera pantalla.
        /// </summary>
        public static IEnumerator LoadApp(IProfileStore store)
        {
            LaunchOptions.StartLevelOverride = 0;
            LaunchOptions.ProgressStoreOverride = null;
            LaunchOptions.ProfileStoreOverride = store;
            yield return LoadScene();
            yield return WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "arranque de la escena Game");
        }

        private static IEnumerator LoadScene()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
#endif
        }

        /// <summary>
        /// Carga la escena Game con el paquete <paramref name="json"/> (gancho <see cref="LaunchOptions.LevelPackJsonOverride"/>)
        /// en el nivel <paramref name="level"/>. Quien llama limpia los ganchos con <see cref="LaunchOptions.Reset"/>.
        /// </summary>
        public static IEnumerator LoadGameWithPack(string json, int level, IProgressStore store)
        {
            LaunchOptions.LevelPackJsonOverride = json;
            yield return LoadGame(level, store);
        }

        /// <summary>
        /// Paquete de prueba con un tablero de 13 cuias (D-006; tablero de Magic Sort con 13 botellas, ficha §2.1): subconjunto
        /// de <c>af-level/1</c>, nivel 1, 11 tintes del glosario §9 con su ID y su índice de motor (1-11), 4 dosis cada uno
        /// repartidas en escalera (la cuia i lleva los tintes i … i + 3), 2 cuias vacías y capacidad 4. No empieza resuelto
        /// ni en Aguas estancadas (hay vacías) y su par es ≥ 1.
        /// </summary>
        public static string ThirteenCuiaPack()
        {
            string[] ids = { "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo", "agua_tapajos", "lulo" };
            var cuias = new System.Text.StringBuilder();
            var caps = new System.Text.StringBuilder();
            var attrs = new System.Text.StringBuilder();
            for (int i = 0; i < 13; i++)
            {
                string sep = i == 0 ? string.Empty : ", ";
                caps.Append(sep).Append('4');
                attrs.Append(sep).Append("null");
                cuias.Append(sep).Append('[');
                if (i < ids.Length)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        cuias.Append(k == 0 ? string.Empty : ", ").Append(((i + k) % ids.Length) + 1);
                    }
                }

                cuias.Append(']');
            }

            return "{ \"subset_of\": \"af-level/1\", \"pack_id\": \"test_13\", \"kind\": \"campaign\", \"world\": 1, "
                + "\"world_id\": \"w01_estuario\", \"lap\": 1, \"water_base\": \"white\", \"levels\": [ { \"id\": \"m01_v1_n0001\", "
                + "\"level\": 1, \"world\": 1, \"lap\": 1, \"water_base\": \"white\", \"palette\": [\"" + string.Join("\", \"", ids)
                + "\"], \"units_per_color\": 4, \"caps\": [" + caps + "], \"cuias\": [" + cuias + "], \"attrs\": [" + attrs
                + "], \"murky\": false, \"hidden\": [], \"difficulty\": { \"par\": 1 }, \"relief\": [] } ] }";
        }

        /// <summary>
        /// Paquete de prueba de 16 cuias (6 + 5 + 5, D-006): 14 tintes del glosario §9 con 4 dosis cada uno en escalera (la cuia
        /// i lleva los tintes i … i + 3: 4 rachas por cuia, 56 en total) y 2 vacías. Con más de 24 rachas, «Llenar y teñir» cae
        /// en el modo de una fruta por cuia y, con 3 filas, en el modo dosel (guion de la intro §7.4).
        /// </summary>
        public static string SixteenCuiaPack()
        {
            string[] ids = { "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo", "agua_tapajos", "lulo", "araza", "cacao", "agua_negro" };
            var cuias = new int[16][];
            for (int i = 0; i < 16; i++)
            {
                cuias[i] = i < ids.Length ? new[] { (i % 14) + 1, ((i + 3) % 14) + 1, ((i + 6) % 14) + 1, ((i + 9) % 14) + 1 } : new int[0];
            }

            return BoardPack(1, "w01_estuario", "white", 1, ids, cuias);
        }

        /// <summary>
        /// Paquete de un nivel con el tablero del nivel 7 del tutorial (7 cuias, 4 + 3, 15 rachas) en el mundo
        /// <paramref name="world"/> y el nivel <paramref name="level"/>: sirve para ver la tropa de otra especie (guion de la
        /// intro §4.2: sagui plateado en el 11-40 del mundo 2, huapo colorado en el 141-200 del mundo 5…).
        /// </summary>
        public static string Level7BoardPack(int world, string worldId, string water, int level)
        {
            string[] ids = { "acai", "aguaje", "tapereba", "guanabana", "bacaba" };
            int[][] cuias = { new[] { 8, 8, 7, 7 }, new[] { 3, 4, 4, 7 }, new[] { 1, 1, 8, 3 }, new[] { 3, 7, 8, 4 }, new[] { 4, 1, 1, 3 }, new int[0], new int[0] };
            return BoardPack(world, worldId, water, level, ids, cuias);
        }

        /// <summary>Paquete <c>af-level/1</c> (subconjunto de v0) de un solo nivel con capacidad 4.</summary>
        public static string BoardPack(int world, string worldId, string water, int level, string[] palette, int[][] board)
        {
            var cuias = new System.Text.StringBuilder();
            var caps = new System.Text.StringBuilder();
            var attrs = new System.Text.StringBuilder();
            for (int i = 0; i < board.Length; i++)
            {
                string sep = i == 0 ? string.Empty : ", ";
                caps.Append(sep).Append('4');
                attrs.Append(sep).Append("null");
                cuias.Append(sep).Append('[');
                for (int k = 0; k < board[i].Length; k++)
                {
                    cuias.Append(k == 0 ? string.Empty : ", ").Append(board[i][k]);
                }

                cuias.Append(']');
            }

            string ww = world.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
            string id = "m" + ww + "_v1_n" + level.ToString("0000", System.Globalization.CultureInfo.InvariantCulture);
            return "{ \"subset_of\": \"af-level/1\", \"pack_id\": \"test_" + id + "\", \"kind\": \"campaign\", \"world\": " + world
                + ", \"world_id\": \"" + worldId + "\", \"lap\": 1, \"water_base\": \"" + water + "\", \"levels\": [ { \"id\": \"" + id + "\", "
                + "\"level\": " + level + ", \"world\": " + world + ", \"lap\": 1, \"water_base\": \"" + water + "\", \"palette\": [\""
                + string.Join("\", \"", palette) + "\"], \"units_per_color\": 4, \"caps\": [" + caps + "], \"cuias\": [" + cuias + "], \"attrs\": ["
                + attrs + "], \"murky\": false, \"hidden\": [], \"difficulty\": { \"par\": 1 }, \"relief\": [] } ] }";
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

        /// <summary>
        /// Avanza la vista y el flujo exactamente <paramref name="ms"/> milisegundos, en pasos de como mucho
        /// <see cref="FrameMs"/> (el último, el resto), para llegar a un instante concreto de la animación.
        /// </summary>
        public static void TickExact(GameBootstrap boot, int ms)
        {
            int left = ms;
            while (left > 0)
            {
                int dt = left < FrameMs ? left : FrameMs;
                boot.View!.Tick(dt);
                boot.Flow!.Tick(dt, boot.View.IsViewIdle);
                left -= dt;
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

        /// <summary>
        /// Vertidos más cortos desde <paramref name="state"/> tras los que <paramref name="source"/> → <paramref name="target"/>
        /// es legal (BFS sobre el modelo con las reglas del núcleo y el modo de vertido de la partida); vacío si ya lo es y
        /// null si no aparece en los primeros estados. Los estados ganados o bloqueados no sirven de paso intermedio: en
        /// ellos el flujo no acepta toques.
        /// </summary>
        public static Pour[]? FindSetup(BoardState state, int source, int target, PourMode mode, bool futileRule)
        {
            const int maxStates = 20000;
            var parents = new Dictionary<BoardState, (BoardState? Parent, Pour Move)>();
            var queue = new Queue<BoardState>();
            parents.Add(state, (null, default));
            queue.Enqueue(state);
            int n = state.Count;
            while (queue.Count > 0 && parents.Count < maxStates)
            {
                BoardState current = queue.Dequeue();
                if (PourRules.IsLegal(current, source, target))
                {
                    var path = new List<Pour>();
                    for (BoardState? at = current; at != null && parents[at].Parent != null; at = parents[at].Parent)
                    {
                        path.Add(parents[at].Move);
                    }

                    path.Reverse();
                    return path.ToArray();
                }

                for (int a = 0; a < n; a++)
                {
                    for (int b = 0; b < n; b++)
                    {
                        if (a == b || !PourRules.IsLegal(current, a, b))
                        {
                            continue;
                        }

                        BoardState next = PourRules.Apply(current, a, b, mode);
                        if (parents.ContainsKey(next) || next.IsWon() || PourRules.IsStuck(next, futileRule))
                        {
                            continue;
                        }

                        parents.Add(next, (current, new Pour(a, b, PourRules.Amount(current, a, b, mode))));
                        queue.Enqueue(next);
                    }
                }
            }

            return null;
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
