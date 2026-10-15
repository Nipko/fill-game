#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Defecto 1 de la prueba de la ola 1 en el teléfono (tarea FXT; <c>docs/dev/traspaso/pruebas-telefono-2026-10-14-tanda4.md</c>):
    /// tras cada arranque en frío, la primera pulsación de Atrás no cerraba Ajustes. En Android el teclado del Input System
    /// aparece con el primer evento de tecla y la bajada y la subida de Atrás llegan juntas; la primera consulta de
    /// <c>Keyboard.escapeKey.wasPressedThisFrame</c> solo compara con el fotograma anterior y la perdía. Aquí se arranca la escena
    /// sin ningún teclado, se añade el teclado y se encolan la bajada y la subida de Escape antes del mismo fotograma, como
    /// <c>adb shell input keyevent 4</c>: Ajustes se cierra a la primera y sale <c>AF_STATE settings_closed</c>. Atrás es un solo
    /// camino para las hojas del inicio (<see cref="GameBootstrap.PressAndroidBack"/>): también vuelve de «El río sigue
    /// creciendo», y en el inicio sin hoja no hace nada.
    /// </summary>
    public sealed class SettingsBackColdStartTests : InputTestFixture
    {
        private const string SettingsClosed = "AF_STATE {\"event\":\"settings_closed\"";

        private readonly List<string> _lines = new List<string>();

        /// <inheritdoc />
        public override void Setup()
        {
            base.Setup();
            _lines.Clear();
            Application.logMessageReceived += OnLog;
        }

        /// <inheritdoc />
        public override void TearDown()
        {
            Application.logMessageReceived -= OnLog;

            // El módulo de UI y la acción de Atrás usan el Input System de prueba: se apagan antes de restaurarlo.
            InputSystemUIInputModule[] modules = UnityEngine.Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsSortMode.None);
            for (int i = 0; i < modules.Length; i++)
            {
                modules[i].enabled = false;
            }

            if (GameBootstrap.Current != null)
            {
                GameBootstrap.Current.enabled = false;
            }

            LaunchOptions.Reset();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator ColdStart_TheFirstBack_ClosesSettings_AndWritesSettingsClosed()
        {
            Assert.That(InputSystem.GetDevice<Keyboard>(), Is.Null, "arranque en frío: sin teclado al cargar la escena");
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            yield return null;
            yield return null;
            Assert.That(boot.SettingsView!.Visible, Is.True);
            Assert.That(Keyboard.current, Is.Null, "el bucle del juego ya corre y aún no hay teclado");

            // La primera tecla crea el teclado, y su bajada y su subida llegan en el mismo fotograma.
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.escapeKey);
            Release(keyboard.escapeKey);
            yield return null;
            yield return null;

            Assert.That(boot.SettingsView.Visible, Is.False, "la primera pulsación de Atrás cierra Ajustes");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "y se queda en el inicio");
            Assert.That(boot.Lobby!.Visible, Is.True);
            Assert.That(Count(SettingsClosed), Is.EqualTo(1), "sale AF_STATE settings_closed una vez");

            // En la misma sesión, otra vez a la primera, aunque la pulsación dure un solo fotograma.
            Assert.That(boot.PressSettings(), Is.True);
            yield return null;
            Press(keyboard.escapeKey);
            Release(keyboard.escapeKey);
            yield return null;
            yield return null;
            Assert.That(boot.SettingsView.Visible, Is.False, "segunda apertura: también a la primera");
            Assert.That(Count(SettingsClosed), Is.EqualTo(2));

            // Sin hoja abierta, Atrás no hace nada en el inicio.
            Press(keyboard.escapeKey);
            Release(keyboard.escapeKey);
            yield return null;
            yield return null;
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.SettingsView.Visible, Is.False);
            Assert.That(Count(SettingsClosed), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Back_IsOnePathForTheLobbySheets_AndReturnsFromTheRiverKeepsGrowing()
        {
            // Más allá del último nivel publicado: «Jugar» abre «El río sigue creciendo» (06 §9), una hoja del inicio.
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(9999)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.ContentEnd));
            yield return null;

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.escapeKey);
            Release(keyboard.escapeKey);
            yield return null;
            yield return null;

            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby), "Atrás vuelve al inicio, como su botón Volver");
            Assert.That(boot.Lobby!.OverlayVisible, Is.False);
            Assert.That(boot.PressAndroidBack(), Is.False, "en el inicio sin hoja, nada");
            Assert.That(Count(SettingsClosed), Is.EqualTo(0), "Ajustes no estaba abierta");
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (condition.StartsWith("AF_STATE ", StringComparison.Ordinal))
            {
                _lines.Add(condition);
            }
        }

        private int Count(string prefix)
        {
            int n = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].StartsWith(prefix, StringComparison.Ordinal))
                {
                    n++;
                }
            }

            return n;
        }
    }
}
