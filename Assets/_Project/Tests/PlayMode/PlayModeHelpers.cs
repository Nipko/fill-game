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
        /// usa uno en memoria: los tests nunca tocan el <c>profile.json</c> del Editor. Sin tarjeta previa (LEVEL_CARD) salvo
        /// que el test la encienda con <see cref="LaunchOptions.LevelCardsOverride"/> = true (<see cref="NoLevelCardsUnlessSet"/>).
        /// </summary>
        public static IEnumerator LoadGame(int level, IProgressStore progress)
        {
            LaunchOptions.StartLevelOverride = level;
            LaunchOptions.ProgressStoreOverride = progress;
            if (LaunchOptions.ProfileStoreOverride == null)
            {
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore();
            }

            NoLevelCardsUnlessSet();
            yield return LoadScene();
            yield return WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current.Flow != null && GameBootstrap.Current.Flow.LevelNumber == level,
                "arranque de la escena Game con el nivel " + level);
        }

        /// <summary>
        /// Carga la escena Game sin nivel forzado (como el juego: la primera vez el nivel 1, si no la pantalla de inicio) con
        /// el perfil <paramref name="store"/> y espera a que <see cref="GameBootstrap.App"/> haya decidido la primera pantalla.
        /// Sin tarjeta previa salvo que el test la encienda (<see cref="NoLevelCardsUnlessSet"/>).
        /// </summary>
        public static IEnumerator LoadApp(IProfileStore store)
        {
            LaunchOptions.StartLevelOverride = 0;
            LaunchOptions.ProgressStoreOverride = null;
            LaunchOptions.ProfileStoreOverride = store;
            NoLevelCardsUnlessSet();
            yield return LoadScene();
            yield return WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "arranque de la escena Game");
        }

        /// <summary>
        /// Como <see cref="LoadApp"/>, pero con el <c>LocalProfileStore</c> que crea el propio arranque en la carpeta
        /// <paramref name="directory"/> (<see cref="LaunchOptions.ProfileDirectoryOverride"/>) en vez de un almacén del test.
        /// </summary>
        public static IEnumerator LoadAppFromDisk(string directory)
        {
            LaunchOptions.StartLevelOverride = 0;
            LaunchOptions.ProgressStoreOverride = null;
            LaunchOptions.ProfileStoreOverride = null;
            LaunchOptions.ProfileDirectoryOverride = directory;
            NoLevelCardsUnlessSet();
            yield return LoadScene();
            yield return WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "arranque de la escena Game con el perfil en disco");
        }

        /// <summary>
        /// Apaga la tarjeta previa (LEVEL_CARD, 02 §10) si el test no la fijó (<see cref="LaunchOptions.LevelCardsOverride"/> =
        /// null): los tests que no tratan de ella entran al nivel por la intro, como antes de que existiera, aunque
        /// <c>GameConfig.LevelCardsEnabledDefault</c> pase a true (T13). <see cref="LaunchOptions.Reset"/> la vuelve a null.
        /// </summary>
        public static void NoLevelCardsUnlessSet()
        {
            if (LaunchOptions.LevelCardsOverride == null)
            {
                LaunchOptions.LevelCardsOverride = false;
            }

            NoStopLabelsUnlessSet();
            NoBannerTravelUnlessSet();
            NoFruitNewUnlessSet();
            NoSlothRestartNoticeUnlessSet();
            NoTutorialSlothUnlessSet();
            NoTutorialFingerUnlessSet();
            NoL10SequenceUnlessSet();
            NoContentEndMapUnlessSet();
            NoIdleHintUnlessSet();
            NoShuffleUnlessSet();
        }

        /// <summary>
        /// Apaga la Pororoca (bloque B2) si el test no la fijó (<see cref="GameScreen.ShuffleOverride"/> = null): los tests que no
        /// tratan de ella juegan desde el 30 con la bandeja de antes (sin la ranura Barajar, sus avisos ni la tarjeta del 30), y
        /// sus capturas como antes. Los que la prueban (<c>ShufflePlayModeTests</c>) la encienden y la vuelven a null en su
        /// TearDown.
        /// </summary>
        public static void NoShuffleUnlessSet()
        {
            if (GameScreen.ShuffleOverride == null)
            {
                GameScreen.ShuffleOverride = false;
            }
        }

        /// <summary>
        /// Apaga la secuencia del Mapa del 10 (M4.2) si el test no la fijó (<see cref="GameBootstrap.L10SequenceOverride"/> = null):
        /// los tests que no tratan de ella entran al inicio con <c>max_level</c> ≥ 10 y <c>map_seen_level</c> 0, y pulsan «Siguiente»
        /// en la victoria del 10, como antes de que existiera (sin el Mapa encima del inicio ni el inicio en lugar del 11). Los que
        /// la prueban (<c>Level10MapTests</c>) la encienden y la vuelven a null en su TearDown.
        /// </summary>
        public static void NoL10SequenceUnlessSet()
        {
            if (GameBootstrap.L10SequenceOverride == null)
            {
                GameBootstrap.L10SequenceOverride = false;
            }
        }

        /// <summary>
        /// Apaga el Mapa del fin de contenido (M4.3) si el test no lo fijó (<see cref="GameBootstrap.ContentEndMapOverride"/> = null):
        /// los tests que no tratan de él ganan el último nivel publicado y ven «El río sigue creciendo» sola, como antes de que
        /// existiera. Los que lo prueban (<c>ContentEndMapTests</c>) lo encienden y lo vuelven a null en su TearDown.
        /// </summary>
        public static void NoContentEndMapUnlessSet()
        {
            if (GameBootstrap.ContentEndMapOverride == null)
            {
                GameBootstrap.ContentEndMapOverride = false;
            }
        }

        /// <summary>
        /// Apaga la pista por inactividad (MO3) si el test no la fijó (<see cref="GameScreen.IdleHintOverride"/> = null): con el
        /// reloj manual que salta minutos (canoas, anuncios) o con más de 12 s reales en juego, una cuia pulsaría y sonaría
        /// <c>sfx_morpho_idle</c> en mitad de pruebas que no tratan de ella. Los que la prueban (<c>IdleHintTests</c>) la encienden
        /// y la vuelven a null en su TearDown.
        /// </summary>
        public static void NoIdleHintUnlessSet()
        {
            if (GameScreen.IdleHintOverride == null)
            {
                GameScreen.IdleHintOverride = false;
            }
        }

        /// <summary>
        /// Apaga la instrucción del tutorial con TiTino (TU1a) si el test no la fijó (<see cref="GameScreen.TutorialSlothOverride"/>
        /// = null): los tests que no tratan de ella juegan el 1, 2, 3 y 6 con sus avisos, sus etiquetas del HUD y sus capturas como
        /// antes de que existiera. Los que la prueban (<c>TutorialSlothTests</c>) la encienden y la vuelven a null en su TearDown.
        /// </summary>
        public static void NoTutorialSlothUnlessSet()
        {
            if (GameScreen.TutorialSlothOverride == null)
            {
                GameScreen.TutorialSlothOverride = false;
            }
        }

        /// <summary>
        /// Apaga el dedo guía del 1 y el 2 (DC6) si el test no lo fijó (<see cref="GameScreen.TutorialFingerOverride"/> = null):
        /// los tests que no tratan de él juegan el 1 y el 2 y hacen sus capturas como antes de que existiera. Los que lo prueban
        /// (<c>TutorialFingerTests</c>) lo encienden y lo vuelven a null en su TearDown.
        /// </summary>
        public static void NoTutorialFingerUnlessSet()
        {
            if (GameScreen.TutorialFingerOverride == null)
            {
                GameScreen.TutorialFingerOverride = false;
            }
        }

        /// <summary>
        /// Apaga el aviso de TiTino tras Volver a la orilla (TI6b) si el test no lo fijó (<see cref="GameScreen.SlothRestartNoticeOverride"/>
        /// = null): los tests que no tratan de él reinician en 1-30 con sus avisos, sus etiquetas del HUD y sus capturas como antes
        /// de que existiera. Los que lo prueban (<c>SlothFailTests</c>) lo encienden y lo vuelven a null en su TearDown.
        /// </summary>
        public static void NoSlothRestartNoticeUnlessSet()
        {
            if (GameScreen.SlothRestartNoticeOverride == null)
            {
                GameScreen.SlothRestartNoticeOverride = false;
            }
        }

        /// <summary>
        /// Apaga la tarjeta «Fruta nueva» (F5) si el test no la fijó (<see cref="GameScreen.FruitNewOverride"/> = null): los tests
        /// que no tratan de ella juegan el 2, 4, 7, 11, 21, 41… con sus avisos, sus etiquetas del HUD y sus capturas como antes de
        /// que existiera. Los que la prueban (<c>FruitNewCardTests</c>) la encienden y la vuelven a null en su TearDown.
        /// </summary>
        public static void NoFruitNewUnlessSet()
        {
            if (GameScreen.FruitNewOverride == null)
            {
                GameScreen.FruitNewOverride = false;
            }
        }

        /// <summary>
        /// Apaga el rótulo del lugar (M2.3) si el test no lo fijó (<see cref="GameScreen.StopLabelsOverride"/> = null): los tests
        /// que no tratan de él juegan el 31, el 51 o el 131 con sus avisos como antes de que existiera (sin el rótulo delante en la
        /// cola ni las etiquetas del HUD apartadas). Los que lo prueban lo encienden y lo vuelven a null en su TearDown.
        /// </summary>
        public static void NoStopLabelsUnlessSet()
        {
            if (GameScreen.StopLabelsOverride == null)
            {
                GameScreen.StopLabelsOverride = false;
            }
        }

        /// <summary>
        /// Apaga el viaje de mundo del banner (M3.6) si el test no lo fijó (<see cref="GameScreen.BannerTravelOverride"/> = null):
        /// los tests que no tratan de él abren el banner del 41, el 81 o el 141 como antes de que existiera (el de D-012, que un
        /// toque cierra). Los que lo prueban (<c>WorldBannerTravelTests</c>) lo encienden y lo vuelven a null en su TearDown.
        /// </summary>
        public static void NoBannerTravelUnlessSet()
        {
            if (GameScreen.BannerTravelOverride == null)
            {
                GameScreen.BannerTravelOverride = false;
            }
        }

        // Sin idioma del teléfono fijado por el test, uno vacío (ManualDeviceLocale): los textos y las cifras no dependen de la
        // cultura del equipo (tarea L3; el idioma automático da es-419 y las cifras, es-CO). Sin pantalla de arranque (TI6d)
        // salvo que el test la encienda (BootSplash.EnabledOverride = true; BootSplashTests la vuelve a null en su TearDown):
        // los tests que no tratan de ella arrancan como antes de que existiera, todo en el Start, sin fotogramas de más ni el
        // segundo de sloth_wake de la primera apertura.
        private static IEnumerator LoadScene()
        {
            if (LaunchOptions.DeviceLocaleOverride == null)
            {
                LaunchOptions.DeviceLocaleOverride = new AF.Platform.ManualDeviceLocale(string.Empty);
            }

            if (BootSplash.EnabledOverride == null)
            {
                BootSplash.EnabledOverride = false;
            }

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

        // Mundo (1-5) → su id y su agua base, para las paradas 0-19 de 1-200.
        private static readonly string[] StopWorldIds = { string.Empty, "w01_estuario", "w02_tapajos", "w03_manaos", "w04_trapecio", "w05_loreto" };
        private static readonly string[] StopWaters = { string.Empty, "white", "clear", "black", "white", "white" };

        /// <summary>
        /// Carga la escena Game con el tablero de 2 filas del nivel 7 (<see cref="Level7BoardPack"/>) puesto en el nivel
        /// <c>10 × <paramref name="stop"/> + 7</c> (la parada <paramref name="stop"/> de 0-19, con el agua base de su mundo), sin
        /// pasar la intro: las capturas del fondo por parada (StopSheetSnapshotTests, tareas M1.4 y M2.4). Quien llama limpia los
        /// ganchos con <see cref="LaunchOptions.Reset"/>.
        /// </summary>
        public static IEnumerator LoadStopBoard(int stop)
        {
            int level = (10 * stop) + 7;
            int world = AF.Levels.WorldTable.WorldOf(level);
            Assert.That(world, Is.InRange(1, StopWorldIds.Length - 1), "parada " + stop + " fuera de 1-200");
            yield return LoadGameWithPack(Level7BoardPack(world, StopWorldIds[world], StopWaters[world], level), level,
                new MemoryProgressStore(level, level - 1));
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

        /// <summary>
        /// Avanza la vista y el flujo <paramref name="ms"/> milisegundos en pasos de <see cref="FrameMs"/>, como
        /// <c>GameBootstrap.Update</c> (también el avance tras la Cosecha doble, después del Tick del flujo: <see cref="AutoNext"/>).
        /// </summary>
        public static void Step(GameBootstrap boot, int ms)
        {
            for (int elapsed = 0; elapsed < ms; elapsed += FrameMs)
            {
                boot.View!.Tick(FrameMs);
                boot.Flow!.Tick(FrameMs, boot.View.IsViewIdle);
                AutoNext(boot);
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
                AutoNext(boot);
                left -= dt;
            }
        }

        /// <summary>
        /// Lo que hace <c>GameBootstrap.Update</c> tras el Tick del flujo: si la espera tras la Cosecha doble venció, Siguiente
        /// (D-011, punto 4; <see cref="GameScreen.TakeAutoNext"/>).
        /// </summary>
        public static void AutoNext(GameBootstrap boot)
        {
            if (boot.View!.TakeAutoNext())
            {
                boot.PressNext();
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
