#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Session;
using AF.GameFlow;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Etiquetas de D-009 antes de jugar, con el catálogo real de la escena (tarea T08; propuesta-mecanicas-dificultad.md §c.1-c.2;
    /// 07 §7.7; 02 §10): en el inicio, el 40 (Difícil) lleva el chip ámbar con la rana y «×3», el 60 (Muy difícil) el chip
    /// índigo con el jaguar, «×4» y el botón nocturno, y el 41 (normal) nada; en el embudo de D-008, ¿Seguro? y Nivel fallido
    /// llevan la cabecera ámbar o índigo por el gancho <c>FunnelModalShowing</c> (GameScreen.Funnel.cs), y en normal la de
    /// siempre; <c>SetLabel</c> llamado a mano da lo mismo.
    /// </summary>
    public sealed class LabelChipTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Lobby_Level40_HardChipRibbonX3()
        {
            yield return LabelFunnel.LoadLobby(40);
            LobbyScreen lobby = GameBootstrap.Current!.Lobby!;
            Assert.That(lobby.PlayLabel, Is.EqualTo("Jugar · Nivel 40"));
            Assert.That(lobby.ChipShown, Is.True, "el 40 es el primer Difícil (02 §10)");
            Assert.That(lobby.ChipText, Is.EqualTo("Difícil"));
            Assert.That(lobby.ChipRibbonText, Is.EqualTo("×3"), "60 / 20 (eco_reward_*)");
            DifficultyChip chip = lobby.Chip;
            Assert.That(chip.VeryHard, Is.False);
            Assert.That(chip.Icon!.name, Is.EqualTo("label_rana_dardo"));
            Assert.That(chip.PillColor, Is.EqualTo((Color)Palette.UiAmber));
            Assert.That(chip.PillSprite!.name, Is.EqualTo("label_pill_hard"), "la píldora pintada es la ámbar");
            Assert.That(chip.TextColor, Is.EqualTo((Color)Palette.Ink), "texto tinta");
            Assert.That(lobby.PlayNight, Is.False, "en Difícil el botón sigue verde");
            Assert.That(lobby.PlayFaceSprite!.name, Does.Contain("cta_leaf_large"));
            LabelFunnel.AssertChipClearOfPlay(lobby);
        }

        [UnityTest]
        public IEnumerator Lobby_Level60_VeryHardChipNightButton()
        {
            yield return LabelFunnel.LoadLobby(60);
            LobbyScreen lobby = GameBootstrap.Current!.Lobby!;
            Assert.That(lobby.ChipShown, Is.True, "el 60 es el primer Muy difícil (02 §10)");
            Assert.That(lobby.ChipText, Is.EqualTo("Muy difícil"));
            Assert.That(lobby.ChipRibbonText, Is.EqualTo("×4"), "80 / 20 (eco_reward_*)");
            DifficultyChip chip = lobby.Chip;
            Assert.That(chip.VeryHard, Is.True);
            Assert.That(chip.Icon!.name, Is.EqualTo("label_jaguar_head"));
            Assert.That(chip.PillColor, Is.EqualTo((Color)Palette.UiIndigo));
            Assert.That(chip.PillSprite!.name, Is.EqualTo("label_pill_very_hard"), "la píldora pintada es la índigo");
            Assert.That(chip.TextColor, Is.EqualTo((Color)Palette.UiPaper), "texto marfil");
            Assert.That(lobby.PlayNight, Is.True, "propuesta §c.1: el botón en variante nocturna");
            Assert.That(lobby.PlayFaceSprite!.name, Does.Contain("cta_night_large"));
            Assert.That(lobby.PlayLabelColor, Is.EqualTo((Color)Palette.UiPaper));
            Assert.That(lobby.PlayLabel, Is.EqualTo("Jugar · Nivel 60"));
            LabelFunnel.AssertChipClearOfPlay(lobby);

            // Jugar sigue funcionando con la variante nocturna.
            lobby.PlayButton.onClick.Invoke();
            Assert.That(GameBootstrap.Current!.App!.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(GameBootstrap.Current.Flow!.LevelNumber, Is.EqualTo(60));
        }

        [UnityTest]
        public IEnumerator Lobby_Level41_NoChip()
        {
            yield return LabelFunnel.LoadLobby(41);
            LobbyScreen lobby = GameBootstrap.Current!.Lobby!;
            Assert.That(lobby.PlayLabel, Is.EqualTo("Jugar · Nivel 41"));
            Assert.That(lobby.ChipShown, Is.False, "normal: botón verde, sin chip (propuesta §c.1)");
            Assert.That(lobby.PlayNight, Is.False);
            Assert.That(lobby.PlayFaceSprite!.name, Does.Contain("cta_leaf_large"));
            Assert.That(lobby.PlayLabelColor, Is.EqualTo((Color)Palette.Cream));
        }

        [UnityTest]
        public IEnumerator FailedScreen_Level40_HeaderAmber_ViaFunnelHook()
        {
            yield return LabelFunnel.LoadLevel(40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LabelFunnel.QuitToLevelFailed(boot);
            Assert.That(view.Failed.Visible, Is.True);
            Assert.That(view.Failed.Label, Is.EqualTo("hard"), "GameScreen.Funnel.cs: la etiqueta del nivel, no la del premio");
            Assert.That(view.Failed.HeaderBand, Is.EqualTo((Color?)Palette.UiAmber), "cabecera ámbar (propuesta §c.1)");
            Assert.That(view.Failed.TitleColor, Is.EqualTo((Color)Palette.Ink), "tinta sobre ámbar, 5,4:1");
            Assert.That(view.Failed.HeaderBadge!.name, Is.EqualTo("label_rana_dardo"), "no solo color (07 §7.8)");
            Assert.That(view.Failed.TitleText, Is.EqualTo("Nivel 40"));
            Assert.That(view.Failed.RewardText, Is.EqualTo("Premio: 60 semillas"), "eco_reward_hard; no se cobra");
        }

        [UnityTest]
        public IEnumerator GiveUp_Level60_HeaderIndigo_ViaFunnelHook()
        {
            yield return LabelFunnel.LoadLevel(60);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LabelFunnel.GetStuck(boot);
            Assert.That(boot.PressGiveUp(), Is.True);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            Assert.That(view.GiveUp.Visible, Is.True);
            Assert.That(view.GiveUp.Label, Is.EqualTo("very_hard"));
            Assert.That(view.GiveUp.HeaderBand, Is.EqualTo((Color?)Palette.UiIndigo), "cabecera índigo (propuesta §c.1)");
            Assert.That(view.GiveUp.TitleColor, Is.EqualTo((Color)Palette.UiPaper), "marfil sobre índigo, 10,7:1");
            Assert.That(view.GiveUp.HeaderBadge!.name, Is.EqualTo("label_jaguar_head"));

            // SetLabel a mano en ¿Seguro? abierto da lo mismo que el gancho en el siguiente Show: ámbar con la rana en Difícil y
            // de vuelta al índigo con el jaguar en Muy difícil.
            LevelFlow flow = boot.Flow!;
            float w = view.CanvasWidth;
            float h = 780f;
            view.GiveUp.SetLabel("hard");
            view.GiveUp.Show(StuckModalModel.BuildGiveUpConfirm(flow.BuildStuckContext(false, true, GeoTier.T3)), w, h);
            Assert.That(view.GiveUp.HeaderBand, Is.EqualTo((Color?)Palette.UiAmber), "cabecera ámbar en Difícil");
            Assert.That(view.GiveUp.TitleColor, Is.EqualTo((Color)Palette.Ink), "tinta sobre ámbar, 5,4:1");
            Assert.That(view.GiveUp.HeaderBadge!.name, Is.EqualTo("label_rana_dardo"));
            view.GiveUp.SetLabel("very_hard");
            view.GiveUp.Show(StuckModalModel.BuildGiveUpConfirm(flow.BuildStuckContext(false, true, GeoTier.T3)), w, h);
            Assert.That(view.GiveUp.HeaderBand, Is.EqualTo((Color?)Palette.UiIndigo));
            Assert.That(view.GiveUp.TitleColor, Is.EqualTo((Color)Palette.UiPaper));
            Assert.That(view.GiveUp.HeaderBadge!.name, Is.EqualTo("label_jaguar_head"));

            // Y Nivel fallido, el paso siguiente, también.
            Assert.That(boot.ConfirmGiveUp(), Is.True);
            Assert.That(view.Failed.Visible, Is.True);
            Assert.That(view.Failed.HeaderBand, Is.EqualTo((Color?)Palette.UiIndigo));
            Assert.That(view.Failed.RewardText, Is.EqualTo("Premio: 80 semillas"));
        }

        [UnityTest]
        public IEnumerator SetLabel_Normal_KeepsDefaultHeader()
        {
            // El 41 es normal: el gancho deja la cabecera de madera de siempre, con el título crema y sin sello.
            yield return LabelFunnel.LoadLevel(41);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LabelFunnel.QuitToLevelFailed(boot);
            Assert.That(view.Failed.Visible, Is.True);
            Assert.That(view.Failed.Label, Is.EqualTo("normal"));
            Assert.That(view.Failed.HeaderBand, Is.Null, "en normal, igual que antes");
            Assert.That(view.Failed.TitleColor, Is.EqualTo((Color)Palette.Cream));
            Assert.That(view.Failed.HeaderBadge, Is.Null);

            // SetLabel a mano da el mismo resultado que el gancho, y volver a normal deja la cabecera de siempre.
            float w = 360f;
            float h = 780f;
            view.Failed.SetLabel("hard");
            view.Failed.Show(41, 20, w, h);
            Assert.That(view.Failed.HeaderBand, Is.EqualTo((Color?)Palette.UiAmber));
            Assert.That(view.Failed.TitleColor, Is.EqualTo((Color)Palette.Ink));
            view.Failed.SetLabel("very_hard");
            view.Failed.Show(41, 20, w, h);
            Assert.That(view.Failed.HeaderBand, Is.EqualTo((Color?)Palette.UiIndigo));
            Assert.That(view.Failed.HeaderBadge!.name, Is.EqualTo("label_jaguar_head"));
            view.Failed.SetLabel("normal");
            view.Failed.Show(41, 20, w, h);
            Assert.That(view.Failed.HeaderBand, Is.Null);
            Assert.That(view.Failed.TitleColor, Is.EqualTo((Color)Palette.Cream));
            Assert.That(view.Failed.HeaderBadge, Is.Null);
            view.Failed.SetLabel(null);
            view.Failed.Show(41, 20, w, h);
            Assert.That(view.Failed.HeaderBand, Is.Null, "sin etiqueta es normal");

            view.GiveUp.SetLabel("very_hard");
            Assert.That(view.GiveUp.Label, Is.EqualTo("very_hard"), "se guarda y se aplica en el siguiente Show");
            view.GiveUp.SetLabel("normal");
            Assert.That(view.GiveUp.Label, Is.EqualTo("normal"));
        }
    }

    /// <summary>Preparación común de las pruebas y capturas de etiquetas (tarea T08) sobre el catálogo real de la escena.</summary>
    internal static class LabelFunnel
    {
        /// <summary>Carga la app con el perfil en <paramref name="level"/> (pantalla de inicio) y espera un fotograma.</summary>
        public static IEnumerator LoadLobby(int level)
        {
            Profile profile = FunnelSetup.Profile(1240);
            profile.Progress.NextLevel = level;
            profile.Progress.MaxLevel = level - 1;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            Assert.That(GameBootstrap.Current!.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            yield return null;
        }

        /// <summary>Carga el nivel <paramref name="level"/> del catálogo real con 5 canoas, quita el reloj y salta la intro.</summary>
        public static IEnumerator LoadLevel(int level)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        /// <summary>Pausa › «Salir» › confirmar (desde el 31 es fallo duro: D-008) → Nivel fallido.</summary>
        public static void QuitToLevelFailed(GameBootstrap boot)
        {
            GameScreen view = boot.View!;
            view.PauseButton.onClick.Invoke();
            view.Pause.QuitButton.onClick.Invoke();
            Assert.That(view.Confirm.Visible, Is.True);
            view.Confirm.ConfirmButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelFailed));
        }

        /// <summary>
        /// Juega una línea de vertidos que acaba en Aguas estancadas (búsqueda en profundidad con las reglas del núcleo, hasta
        /// 20.000 vertidos probados) y espera a que se abra el modal (R15).
        /// </summary>
        public static void GetStuck(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            var session = new LevelSession(flow.Session.Current, flow.Session.FutileRuleEnabled, flow.Session.PourMode);
            var line = new List<int>();
            int tried = 0;
            Assert.That(FindStuck(session, line, new HashSet<BoardState>(), ref tried, 0), Is.True, "no se encontró una línea que bloquee el nivel " + flow.LevelNumber);
            for (int i = 0; i < line.Count; i += 2)
            {
                Assert.That(boot.TapCuia(line[i]).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(boot.TapCuia(line[i + 1]).Kind, Is.EqualTo(TapKind.Poured));
                PlayModeHelpers.StepUntilIdle(boot, 6000);
            }

            for (int elapsed = 0; elapsed < 6000 && flow.Phase != FlowPhase.StuckModal; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            PlayModeHelpers.Step(boot, 300); // el panel termina de subir (250 ms)
        }

        /// <summary>El chip entero (sello, píldora y cinta) queda encima de Jugar sin tocarlo y dentro del lienzo.</summary>
        public static void AssertChipClearOfPlay(LobbyScreen lobby)
        {
            Canvas.ForceUpdateCanvases();
            Rect play = ScreenRect((RectTransform)lobby.PlayButton.transform);
            Rect chip = ScreenRect(lobby.ChipRect);
            Assert.That(chip.Overlaps(play), Is.False, "el chip no tapa el botón");
            Assert.That(chip.yMin, Is.GreaterThanOrEqualTo(play.yMax), "el chip va encima del botón");
            foreach (RectTransform part in new[] { lobby.Chip.PillRect, lobby.Chip.MedallionRect, lobby.Chip.RibbonRect })
            {
                if (!part.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Rect r = ScreenRect(part);
                Assert.That(r.Overlaps(play), Is.False, part.name + " no tapa el botón");
                Assert.That(chip.Contains(r.min + new Vector2(0.5f, 0.5f)) && chip.Contains(r.max - new Vector2(0.5f, 0.5f)), Is.True, part.name + " dentro del chip");
            }

            // Dentro del lienzo (en dp desde su borde izquierdo: con una pantalla simulada el lienzo real puede ser otro).
            float scale = lobby.ScaleFactor;
            float left = ScreenRect((RectTransform)lobby.Canvas.transform).xMin;
            Assert.That((chip.xMin - left) / scale, Is.GreaterThanOrEqualTo(-0.5f), "dentro de la pantalla");
            Assert.That((chip.xMax - left) / scale, Is.LessThanOrEqualTo(lobby.CanvasWidth + 0.5f), "dentro de la pantalla");
            Assert.That(chip.height / scale, Is.EqualTo(DifficultyChip.HeightDp).Within(0.5f));
            Assert.That(play.yMax + (6f * scale), Is.EqualTo(chip.yMax - (DifficultyChip.PillBottomDp * scale)).Within(0.5f), "píldora 6 dp por encima de Jugar");
        }

        // Rectángulo en píxeles de pantalla (origen abajo a la izquierda) de un elemento de un lienzo en Screen Space Overlay.
        public static Rect ScreenRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float xMin = Mathf.Min(corners[0].x, corners[2].x);
            float xMax = Mathf.Max(corners[0].x, corners[2].x);
            float yMin = Mathf.Min(corners[0].y, corners[2].y);
            float yMax = Mathf.Max(corners[0].y, corners[2].y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        // Búsqueda en profundidad de una línea (pares origen, destino) que deja el tablero en Aguas estancadas, con las reglas
        // del núcleo (LevelSession.TryPour, R10 y mecánicas incluidas) y sin repetir tableros.
        private static bool FindStuck(LevelSession session, List<int> line, HashSet<BoardState> seen, ref int tried, int depth)
        {
            if (session.Status == LevelStatus.Stuck)
            {
                return true;
            }

            if (session.Status == LevelStatus.Won || depth > 60 || tried > 20000 || !seen.Add(session.Current))
            {
                return false;
            }

            int n = session.Current.Count;
            for (int a = 0; a < n; a++)
            {
                for (int b = 0; b < n; b++)
                {
                    if (a == b || !session.TryPour(a, b, out PourResult _))
                    {
                        continue;
                    }

                    tried++;
                    line.Add(a);
                    line.Add(b);
                    if (FindStuck(session, line, seen, ref tried, depth + 1))
                    {
                        return true;
                    }

                    line.RemoveRange(line.Count - 2, 2);
                    session.Undo();
                }
            }

            return false;
        }
    }
}
