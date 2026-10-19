#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Pantalla de inicio y progreso guardado en la escena <c>Game</c> real (tareas B1-5 y B1-6; 08 §2.3 y §4.3; 06 §4.7;
    /// D-008 punto 6): la primera vez se entra al nivel 1; después, el inicio con «Jugar · Nivel {n}» del perfil; Inicio
    /// vuelve desde la victoria; tras ganar el 1 y recrear el arranque el progreso dice 2; «El río sigue creciendo» tras el
    /// último nivel; Sin canoas desde el 31; y sin solapes a 360 × 640 ni a 360 × 800, con controles ≥ 48 dp.
    /// </summary>
    public sealed class LobbyTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator FirstRun_GoesStraightToLevel1()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "08 §2.3: la primera vez, directamente el nivel 1");
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(1));
            Assert.That(boot.View!.Visible, Is.True);
            Assert.That(boot.Lobby!.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Lobby_ShowsTheSavedLevel_AndPlayLoadsIt()
        {
            var store = new MemoryProfileStore(ProfileAt(4));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(lobby.Visible, Is.True);
            Assert.That(boot.View!.Visible, Is.False);
            yield return null;
            Assert.That(lobby.PlayLabel, Is.EqualTo("Jugar · Nivel 4"));
            Assert.That(lobby.SeedsShown, Is.EqualTo(0));
            Assert.That(lobby.CanoesShown, Is.False, "las canoas se ven desde el 31");
            Assert.That(lobby.ChipShown, Is.False, "el tutorial no lleva etiqueta de dificultad");
            Assert.That(lobby.SettingsButton.interactable, Is.True, "Ajustes abre su pantalla (tarea L3)");

            lobby.PlayButton.onClick.Invoke();
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(4));
            Assert.That(lobby.Visible, Is.False);
            Assert.That(boot.View.Visible, Is.True);
            yield return PlayModeHelpers.WaitFor(() => boot.Flow.Phase == FlowPhase.Playing, "fin de la intro del nivel 4");
        }

        [UnityTest]
        public IEnumerator WinLevel1_HomeReturnsToTheLobby_AndAfterRecreatingTheBootstrapTheProgressSays2()
        {
            // B1-5: el progreso vive en el perfil; tras ganar el 1 y recrear el arranque, next_level = 2.
            var store = new MemoryProfileStore();
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(1));
            yield return WinCurrentLevel(boot);
            Assert.That(ProfileJson.Deserialize(store.Json!).Progress.NextLevel, Is.EqualTo(2));
            Assert.That(ProfileJson.Deserialize(store.Json!).Progress.MaxLevel, Is.EqualTo(1));

            // «Inicio» [P] en la victoria vuelve al inicio, que ya dice «Jugar · Nivel 2».
            Assert.That(boot.PressHome(), Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            yield return null;
            Assert.That(boot.Lobby!.PlayLabel, Is.EqualTo("Jugar · Nivel 2"));

            // Se recrea el arranque con el mismo perfil (como cerrar y abrir la app): ya no es la primera vez.
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap again = GameBootstrap.Current!;
            Assert.That(again, Is.Not.SameAs(boot));
            Assert.That(again.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(again.App.PlayLevel, Is.EqualTo(2));
            yield return null;
            Assert.That(again.Lobby!.PlayLabel, Is.EqualTo("Jugar · Nivel 2"));
            Assert.That(again.PressPlay(), Is.True);
            Assert.That(again.Flow!.LevelNumber, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator VictoryHome_IsASecondaryButtonUnderNext()
        {
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            yield return WinCurrentLevel(boot);
            yield return PlayModeHelpers.WaitFor(() => boot.View!.HomeButton.gameObject.activeInHierarchy, "botón Inicio");
            RectTransform next = (RectTransform)boot.View!.NextButton.transform;
            RectTransform home = (RectTransform)boot.View.HomeButton.transform;
            Rect nextRect = ScreenRect(next);
            Rect homeRect = ScreenRect(home);
            Assert.That(homeRect.yMax, Is.LessThanOrEqualTo(nextRect.yMin + 0.5f), "Inicio va debajo de Siguiente");
            Assert.That(nextRect.Overlaps(homeRect), Is.False);
            float scale = boot.View.Canvas.scaleFactor;
            Assert.That(homeRect.height / scale, Is.GreaterThanOrEqualTo(LobbyScreen.MinTouchDp - 0.5f));
            boot.View.HomeButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
        }

        [UnityTest]
        public IEnumerator AfterTheLastLevel_PlayOpensTheRiverKeepsGrowing()
        {
            // El catálogo de la escena acaba en el 700 (tutorial_v0 y mundo_02..09_v1, desde C1b): el 701 aún no está publicado.
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(701)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.ContentEnd));
            Assert.That(boot.Lobby!.OverlayVisible, Is.True);
            Assert.That(boot.Lobby.OverlayTitle, Is.EqualTo("El río sigue creciendo"));
            Assert.That(boot.Flow!.HasLevel, Is.False);
            boot.Lobby.BackButton.onClick.Invoke();
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.Lobby.OverlayVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator From31_CanoesShowWithTheirTimer_AndWithoutCanoesPlayOpensNoCanoes()
        {
            // af_level_offset [P] = 30: el nivel 31 se juega con el tablero del 1 (B1-5).
            var clock = new ManualClock(T0, TimeSpan.FromHours(-5));
            Profile profile = ProfileAt(31, clock);
            profile.Wallet.Canoes.Count = 0;
            profile.Wallet.Canoes.NextRegenAt = T0 + TimeSpan.FromMinutes(18) + TimeSpan.FromSeconds(42);
            LaunchOptions.LevelOffsetOverride = 30;
            LaunchOptions.ClockOverride = clock;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            yield return null;
            Assert.That(lobby.PlayLabel, Is.EqualTo("Jugar · Nivel 31"));
            Assert.That(lobby.CanoesShown, Is.True);
            Assert.That(lobby.CanoesCountShown, Is.EqualTo(0));
            Assert.That(lobby.CanoesTimerShown, Is.EqualTo((18 * 60) + 42), "«18:42» hasta la próxima canoa");

            lobby.PlayButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes));
            Assert.That(lobby.OverlayTitle, Is.EqualTo("Sin canoas"));
            Assert.That(boot.Flow!.HasLevel, Is.False, "sin canoas no se entra");
            lobby.BackButton.onClick.Invoke();
            lobby.CanoesButton.onClick.Invoke();
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.NoCanoes), "el contador en 0/5 también abre Sin canoas");
            lobby.BackButton.onClick.Invoke();

            // Pasa el tiempo: vuelve una canoa, el contador dice 1/5 y Jugar entra al 31.
            clock.Advance(TimeSpan.FromMinutes(19));
            yield return null;
            yield return null;
            Assert.That(lobby.CanoesCountShown, Is.EqualTo(1));
            Assert.That(lobby.CanoesTimerShown, Is.EqualTo((24 * 60) + 42));
            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(31));
        }

        [UnityTest]
        public IEnumerator AHardLevel_ShowsTheDifficultyChipOnThePlayButton()
        {
            LaunchOptions.LevelPackJsonOverride = PackWithLabel("hard");
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(1)));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            Assert.That(boot.Lobby!.ChipShown, Is.True);
            Assert.That(boot.Lobby.ChipText, Is.EqualTo("Difícil"));
            Assert.That(boot.Lobby.ChipRibbonText, Is.EqualTo("×3"), "propuesta §c.2: 60 / 20");
            Rect chip = ScreenRect(boot.Lobby.ChipRect);
            Rect play = ScreenRect((RectTransform)boot.Lobby.PlayButton.transform);
            Assert.That(chip.Overlaps(play), Is.False, "el chip va encima del botón, sin taparlo");

            // T08: el chip entero (sello, píldora y cinta) tampoco lo tapa, ni en Muy difícil con el botón nocturno, a 360 × 640
            // y a 360 × 800 dp.
            foreach (Vector2Int size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2400) })
            {
                foreach (string label in new[] { "hard", "very_hard" })
                {
                    GameScreen.SimulatedScreen = new Vector4(size.x, size.y, 96f, 48f);
                    LaunchOptions.LevelPackJsonOverride = PackWithLabel(label);
                    yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(1)));
                    LobbyScreen lobby = GameBootstrap.Current!.Lobby!;
                    yield return null;
                    string where = size.y + " px, " + label;
                    Assert.That(lobby.ChipShown, Is.True, where);
                    Assert.That(lobby.PlayNight, Is.EqualTo(label == "very_hard"), where);
                    Assert.That(lobby.ChipRibbonText, Is.EqualTo(label == "very_hard" ? "×4" : "×3"), where);
                    LabelFunnel.AssertChipClearOfPlay(lobby);
                }
            }
        }

        [UnityTest]
        public IEnumerator Layout_HasNoOverlaps_At360x640And360x800_WithControlsOfAtLeast48dp()
        {
            var clock = new ManualClock(T0);
            Profile profile = ProfileAt(31, clock);
            profile.Wallet.Canoes.Count = 3;
            profile.Wallet.Canoes.NextRegenAt = T0 + TimeSpan.FromMinutes(10);
            profile.Wallet.Seeds = 12345;
            LaunchOptions.LevelOffsetOverride = 30;
            LaunchOptions.ClockOverride = clock;
            LaunchOptions.LevelPackJsonOverride = null;
            var store = new MemoryProfileStore(profile);
            foreach (Vector2Int size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2400) })
            {
                // 360 × 640 dp (16:9) y 360 × 800 dp (20:9) a 3 px/dp, con 32 dp de recorte arriba y 16 de gestos abajo.
                GameScreen.SimulatedScreen = new Vector4(size.x, size.y, 96f, 48f);
                yield return PlayModeHelpers.LoadApp(store);
                GameBootstrap boot = GameBootstrap.Current!;
                LobbyScreen lobby = boot.Lobby!;
                yield return null;
                Canvas.ForceUpdateCanvases();
                string where = size.x + " × " + size.y;
                Assert.That(lobby.CanvasWidth, Is.EqualTo(360f).Within(0.5f), where);
                Assert.That(lobby.CanvasHeight, Is.EqualTo(size.y / 3f).Within(0.5f), where);
                Assert.That(lobby.CanoesShown, Is.True, where);

                // Rectángulos en dp del lienzo simulado (origen arriba a la izquierda, y hacia abajo): en batchmode la pantalla
                // real es otra, así que se mide respecto al borde superior del lienzo, al que se anclan todos los elementos.
                var root = (RectTransform)lobby.Canvas.transform;
                var named = new List<KeyValuePair<string, Rect>>
                {
                    new KeyValuePair<string, Rect>("semillas", CanvasRect(lobby.SeedsRect, root)),
                    new KeyValuePair<string, Rect>("canoas", CanvasRect((RectTransform)lobby.CanoesButton.transform, root)),
                    new KeyValuePair<string, Rect>("ajustes", CanvasRect((RectTransform)lobby.SettingsButton.transform, root)),
                    new KeyValuePair<string, Rect>("jugar", CanvasRect((RectTransform)lobby.PlayButton.transform, root)),
                    new KeyValuePair<string, Rect>("inicio", CanvasRect(lobby.HomeTabRect, root)),
                };

                float w = lobby.CanvasWidth;
                float h = lobby.CanvasHeight;
                for (int i = 0; i < named.Count; i++)
                {
                    Rect r = named[i].Value;
                    Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-0.5f), where + ": " + named[i].Key + " dentro de la pantalla");
                    Assert.That(r.xMax, Is.LessThanOrEqualTo(w + 0.5f), where + ": " + named[i].Key + " dentro de la pantalla");
                    Assert.That(r.yMin, Is.GreaterThanOrEqualTo(lobby.SafeTop - 0.5f), where + ": " + named[i].Key + " bajo el recorte");
                    Assert.That(r.yMax, Is.LessThanOrEqualTo(h - lobby.SafeBottom + 0.5f), where + ": " + named[i].Key + " sobre la zona de gestos");
                    for (int j = i + 1; j < named.Count; j++)
                    {
                        Assert.That(named[i].Value.Overlaps(named[j].Value), Is.False, where + ": " + named[i].Key + " y " + named[j].Key + " se solapan");
                    }
                }

                // 07 §1.2 y §7.8: controles ≥ 48 dp.
                foreach (RectTransform control in new[] { (RectTransform)lobby.CanoesButton.transform, (RectTransform)lobby.SettingsButton.transform, (RectTransform)lobby.PlayButton.transform })
                {
                    Rect r = CanvasRect(control, root);
                    Assert.That(r.width, Is.GreaterThanOrEqualTo(LobbyScreen.MinTouchDp - 0.5f), where + ": ancho de " + control.name);
                    Assert.That(r.height, Is.GreaterThanOrEqualTo(LobbyScreen.MinTouchDp - 0.5f), where + ": alto de " + control.name);
                }

                // 06 §4.7: el botón Jugar centrado en el 60 % del alto.
                Rect playRect = named[3].Value;
                Assert.That(playRect.center.y / h, Is.EqualTo(LobbyScreen.PlayCenterFraction).Within(0.01f), where);
            }
        }

        private static Profile ProfileAt(int nextLevel, IClock? clock = null)
        {
            Profile profile = Wallet.NewProfile(clock ?? new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        private static IEnumerator WinCurrentLevel(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            boot.TapAnywhere();
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                boot.TapCuia(move.Source);
                boot.TapCuia(move.Target);
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.VictoryScreen, "pantalla de victoria");
        }

        // Rectángulo de rt en dp del lienzo root, con origen en su esquina superior izquierda y la y hacia abajo.
        private static Rect CanvasRect(RectTransform rt, RectTransform root)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Rect bounds = root.rect;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = root.InverseTransformPoint(corners[i]);
                float x = local.x - bounds.xMin;
                float y = bounds.yMax - local.y;
                xMin = Mathf.Min(xMin, x);
                xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y);
                yMax = Mathf.Max(yMax, y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        // Rectángulo en píxeles de pantalla (origen abajo a la izquierda) de un elemento de un lienzo en Screen Space Overlay.
        private static Rect ScreenRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float xMin = Mathf.Min(corners[0].x, corners[2].x);
            float xMax = Mathf.Max(corners[0].x, corners[2].x);
            float yMin = Mathf.Min(corners[0].y, corners[2].y);
            float yMax = Mathf.Max(corners[0].y, corners[2].y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        // Dos niveles del tutorial (el 1 de 02 §13), el segundo con difficulty.label: el chip del botón Jugar [P].
        private static string PackWithLabel(string label)
        {
            const string level = "{ \"id\": \"m01_v1_n0001\", \"level\": 1, \"world\": 1, \"lap\": 1, \"water_base\": \"white\", "
                + "\"palette\": [\"acai\", \"aguaje\"], \"units_per_color\": 4, \"caps\": [4, 4, 4], "
                + "\"cuias\": [[8, 8, 8, 4], [4, 4, 4, 8], []], \"attrs\": [null, null, null], \"murky\": false, \"hidden\": [], "
                + "\"difficulty\": { \"label\": \"LABEL\", \"par\": 3 }, \"relief\": [] }";
            return "{ \"subset_of\": \"af-level/1\", \"pack_id\": \"test_label\", \"kind\": \"campaign\", \"world\": 1, "
                + "\"world_id\": \"w01_estuario\", \"lap\": 1, \"water_base\": \"white\", \"levels\": [ "
                + level.Replace("LABEL", label) + " ] }";
        }
    }
}
