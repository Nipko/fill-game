#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
    /// creciendo» y de Sin canoas abierta desde el inicio, y en el inicio sin hoja no hace nada. Con la hoja «Compra simulada»,
    /// un anuncio simulado o el aviso del perfil bloqueado delante, Atrás no hace nada (revisión de FXT): no cierra lo que hay
    /// debajo, para que la compra o el anuncio no terminen sobre una pantalla ya cerrada.
    /// </summary>
    public sealed class SettingsBackColdStartTests : InputTestFixture
    {
        private const string SettingsClosed = "AF_STATE {\"event\":\"settings_closed\"";
        private const string PurchaseSim = "AF_STATE {\"event\":\"purchase_sim\"";

        private static readonly DateTime T0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);

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
            GameBootstrap.ResetDailyOverrides();
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
            GameBootstrap.DailyPoolDisabledOverride = true; // sin el Reto: con él, «Jugar · Reto del río» abre el Reto (D-032, punto 6)
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

        [UnityTest]
        public IEnumerator Back_ClosesNoCanoes_OpenedFromTheLobby()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesAt31()));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.PressCanoes(), Is.True, "con 0/5 el contador abre Sin canoas");
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.NoCanoes));
            Assert.That(boot.Lobby!.OverlayVisible, Is.True);
            yield return null;

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return PressEscape(keyboard);

            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby), "Atrás vuelve al inicio, como «Volver al inicio»");
            Assert.That(boot.Lobby.OverlayVisible, Is.False);
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(0), "sin coste");
            Assert.That(Count(SettingsClosed), Is.EqualTo(0), "Ajustes no estaba abierta");
        }

        [UnityTest]
        public IEnumerator Back_DoesNothing_WithTheSimulatedPurchaseOverNoCanoes()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesAt31()));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressCanoes(), Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes));
            yield return null;

            // «Canoas infinitas 1 h» de Sin canoas abre la hoja «Compra simulada» encima.
            StoreSheet sheet = boot.StoreSheetView!;
            Button infinite = boot.Lobby!.NoCanoes.InfiniteButton;
            Assert.That(infinite.gameObject.activeInHierarchy, Is.True, "Sin canoas ofrece Canoas infinitas");
            infinite.onClick.Invoke();
            Assert.That(sheet.Visible, Is.True, "la hoja «Compra simulada» está abierta");
            Assert.That(boot.Store!.IsPurchasing, Is.True);
            yield return null;

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return PressEscape(keyboard);

            Assert.That(sheet.Visible, Is.True, "Atrás no cierra la hoja");
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.NoCanoes), "ni Sin canoas por debajo");
            Assert.That(boot.Lobby.OverlayVisible, Is.True);
            Assert.That(boot.Store.IsPurchasing, Is.True, "la compra sigue pendiente");
            Assert.That(Count(PurchaseSim), Is.EqualTo(0), "no se cobra nada");
            Assert.That(boot.Wallet!.Canoes.IsInfinite, Is.False, "ni se acreditan canoas infinitas");
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(0));

            // Al cancelar la hoja, Atrás vuelve a cerrar Sin canoas.
            sheet.CancelButton.onClick.Invoke();
            Assert.That(sheet.Visible, Is.False);
            Assert.That(boot.Store.IsPurchasing, Is.False);
            Assert.That(boot.Wallet.Canoes.IsInfinite, Is.False, "cancelada: nada acreditado");
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.NoCanoes));
            yield return PressEscape(keyboard);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
        }

        [UnityTest]
        public IEnumerator Back_DoesNothing_WithTheSimulatedAdOverNoCanoes()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesAt31()));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressCanoes(), Is.True);
            yield return null;

            // «Canoa extra · Ver video» de Sin canoas abre el anuncio simulado encima.
            AdOverlay ad = boot.AdOverlayView!;
            Button video = boot.Lobby!.NoCanoes.VideoButton;
            Assert.That(video.gameObject.activeInHierarchy, Is.True, "Sin canoas ofrece el video");
            video.onClick.Invoke();
            Assert.That(ad.Visible, Is.True, "el anuncio simulado está delante");
            yield return null;

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return PressEscape(keyboard);

            Assert.That(ad.Visible, Is.True, "Atrás no cierra el anuncio");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes), "ni Sin canoas por debajo");
            Assert.That(boot.Lobby.OverlayVisible, Is.True);
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(0), "el anuncio no terminó: aún no hay canoa");
        }

        [UnityTest]
        public IEnumerator Back_DoesNothing_WithTheLockedProfileNoticeInFront()
        {
            // Perfil en solo lectura (08 §4.4): se arranca en el inicio con el aviso «Actualiza Tintalú» delante.
            yield return PlayModeHelpers.LoadApp(new ReadOnlyProfileStore(ProfileLoadStatus.TooNew));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.ProfileLocked, Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            ProfileLockedNotice notice = boot.LockedNotice!;
            Assert.That(notice.Visible, Is.True);
            yield return null;

            // Con Ajustes cerrada: nada.
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return PressEscape(keyboard);
            Assert.That(notice.Visible, Is.True, "Atrás no cierra el aviso");
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.SettingsView!.Visible, Is.False);

            // Con Ajustes abierta debajo (como tras elegir un idioma con el perfil bloqueado: la UI se rehace, Ajustes se reabre y
            // el aviso vuelve a salir encima), Atrás tampoco la cierra mientras el aviso esté delante.
            Assert.That(boot.PressSettings(), Is.True);
            yield return null;
            Assert.That(boot.SettingsView.Visible, Is.True);
            yield return PressEscape(keyboard);
            Assert.That(notice.Visible, Is.True);
            Assert.That(boot.SettingsView.Visible, Is.True, "Ajustes sigue abierta bajo el aviso");
            Assert.That(Count(SettingsClosed), Is.EqualTo(0));

            // Cerrado el aviso con su botón, Atrás vuelve a cerrar Ajustes.
            notice.PlayButton.onClick.Invoke();
            Assert.That(notice.Visible, Is.False);
            yield return PressEscape(keyboard);
            Assert.That(boot.SettingsView.Visible, Is.False);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(Count(SettingsClosed), Is.EqualTo(1));
        }

        // Nivel 31 con 0/5 canoas (af_level_offset = 30: el 31 se juega con el tablero 1) y el reloj parado: la canoa siguiente
        // llega dentro de 18 min, así que Sin canoas no se cierra sola durante el test.
        private static Profile NoCanoesAt31()
        {
            var clock = new ManualClock(T0, TimeSpan.FromHours(-5));
            LaunchOptions.ClockOverride = clock;
            LaunchOptions.LevelOffsetOverride = 30;
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.NextLevel = 31;
            profile.Progress.MaxLevel = 30;
            profile.Wallet.Canoes.Count = 0;
            profile.Wallet.Canoes.NextRegenAt = T0 + TimeSpan.FromMinutes(18);
            return profile;
        }

        // Como «adb shell input keyevent 4»: la bajada y la subida de Escape antes del mismo fotograma.
        private IEnumerator PressEscape(Keyboard keyboard)
        {
            Press(keyboard.escapeKey);
            Release(keyboard.escapeKey);
            yield return null;
            yield return null;
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
