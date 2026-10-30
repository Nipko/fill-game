#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El botón Atrás de Android no cierra Aguas estancadas ni ¿Seguro? (04 §6.6: «el botón Atrás de Android no cierra»; el
    /// modal solo se deja eligiendo), y en Nivel fallido hace lo mismo que su X (02 §3.5; observación 21 de la jornada 6,
    /// decisión del dueño del 2026-10-30). En el Input System, el Atrás de Android llega como <c>Keyboard.escapeKey</c> (y es la
    /// acción «Cancel» del módulo de UI): aquí se inyecta Escape con <see cref="InputTestFixture"/>, con el bucle del juego en
    /// marcha (<c>GameBootstrap.Update</c> y el <c>EventSystem</c> de la escena). Los demás casos de Atrás en el nivel, la pausa
    /// y el inicio, en <see cref="AndroidBackTests"/>.
    /// </summary>
    public sealed class BackButtonTests : UiInputTestFixture
    {
        private const int FramesPressed = 3;

        /// <inheritdoc />
        public override void Setup()
        {
            base.Setup();
            FunnelSetup.Hooks();
        }

        /// <inheritdoc />
        public override void TearDown()
        {
            LaunchOptions.Reset();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Level31_AndroidBack_DoesNotCloseStuckGiveUpOrLevelFailed()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1000));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            GameScreen view = boot.View!;

            // Paso 1: Aguas estancadas.
            FunnelSetup.GetStuck(boot);
            yield return PressBack(boot, keyboard);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal), "Atrás no cierra Aguas estancadas (04 §6.6)");
            Assert.That(view.StuckVisible, Is.True);
            Assert.That(boot.LastBackScreen, Is.EqualTo("stuck"));
            Assert.That(boot.LastBackAction, Is.EqualTo("blocked"), "la vibración corta");
            Assert.That(view.Pause.Visible, Is.False, "ni abre la pausa encima del modal");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(1000));
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(5));

            // Paso 2: ¿Seguro? (la canoa aún no se cobra).
            view.Stuck.GiveUpButton.onClick.Invoke();
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            yield return PressBack(boot, keyboard);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm), "Atrás no confirma ni cierra ¿Seguro?");
            Assert.That(view.GiveUp.Visible, Is.True);
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(5), "Atrás no es la X de ¿Seguro?: no cobra la canoa");
            Assert.That(boot.LastBackAction, Is.EqualTo("blocked"));

            // Paso 3: Nivel fallido: Atrás es su X, al inicio (02 §3.5).
            view.GiveUp.GiveUpButton.onClick.Invoke();
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(4));
            yield return PressBack(boot, keyboard);
            Assert.That(boot.LastBackScreen, Is.EqualTo("level_failed"));
            Assert.That(boot.LastBackAction, Is.EqualTo("close"));
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "Atrás cierra Nivel fallido como su X: al inicio (06 §4.7)");
            Assert.That(boot.Lobby!.Visible, Is.True);
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(4), "sin cobrar otra canoa");
        }

        // Pulsa y suelta Escape (el Atrás de Android) con el bucle del juego en marcha durante unos fotogramas. En un
        // [UnityTest], InputTestFixture solo encola el evento: el Input System lo procesa en el siguiente fotograma del bucle.
        private IEnumerator PressBack(GameBootstrap boot, Keyboard keyboard)
        {
            boot.enabled = true;
            Press(keyboard.escapeKey);
            yield return null;
            Assert.That(keyboard.escapeKey.isPressed, Is.True, "Escape llega al Input System");
            for (int i = 1; i < FramesPressed; i++)
            {
                yield return null;
            }

            Release(keyboard.escapeKey);
            for (int i = 0; i < FramesPressed; i++)
            {
                yield return null;
            }

            Assert.That(keyboard.escapeKey.isPressed, Is.False);
            PlayModeHelpers.TakeClock(boot);
        }
    }
}
