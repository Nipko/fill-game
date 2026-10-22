#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AF.GameFlow;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// TalkBack en la escena <c>Game</c> real (bloque B9; 02 §12, fila «Lector de pantalla»; 08 §14.2, fila «Accesibilidad»), con el
    /// anfitrión falso (<see cref="FakeScreenReaderHost"/>): el recorrido genérico del inicio y de Ajustes (todo botón con su
    /// etiqueta; lo tapado por un velo no se publica; activar pulsa el botón) y la fuente propia del tablero (las cuias por orden
    /// de ranura con su etiqueta, después la barra de comodines; activar una cuia es el mismo toque). Los textos de B9 se leen de
    /// <c>strings.csv</c> más <c>B9-strings.csv</c> (hasta aplicar <c>B9-loc.diff</c>). La conexión al arranque, el anuncio de la
    /// selección y el resto los prueba el parche (<c>A11yGameTests</c>).
    /// </summary>
    public sealed class A11yScannerTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator Lobby_EveryButtonIsPublished_InReadingOrder()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            var host = new FakeScreenReaderHost(true);
            var service = new A11yService(host);
            Assert.That(service.Refresh(), Is.True);
            IReadOnlyList<A11yItem> items = new List<A11yItem>(host.Items); // copia: Publish rehace la lista del anfitrión
            Assert.That(items, Is.Not.Empty);
            Assert.That(Has(items, AccessibilityRole.Button, boot.Lobby!.PlayLabel), Is.True, "«Jugar · Nivel 4» con su etiqueta funcional");
            AssertReadingOrder(items);
            Assert.That(service.Refresh(), Is.False, "sin cambios no se vuelve a publicar");
        }

        [UnityTest]
        public IEnumerator Settings_HidesTheLobbyBehindItsVeil_AndActivatingARowPressesIt()
        {
            var store = new MemoryProfileStore(SettingsScreenTests.ProfileAt(4));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            yield return null;
            var host = new FakeScreenReaderHost(true);
            var service = new A11yService(host);
            service.Refresh();
            IReadOnlyList<A11yItem> items = new List<A11yItem>(host.Items); // copia: Publish rehace la lista del anfitrión
            Assert.That(Has(items, AccessibilityRole.Button, boot.Lobby!.PlayLabel), Is.False, "el inicio queda debajo del velo de Ajustes");
            Assert.That(Has(items, AccessibilityRole.StaticText, UiText.Settings), Is.True, "la cabecera «Ajustes»");
            A11yItem? sound = host.Find(UiText.Sound);
            Assert.That(sound, Is.Not.Null, "la fila Sonido, con su texto");
            Assert.That(sound!.Role, Is.EqualTo(AccessibilityRole.Button));
            Assert.That(boot.Profile!.Settings.Sound, Is.True);
            Assert.That(sound.Invoke!(), Is.True, "activar (doble toque) es pulsar la fila");
            Assert.That(boot.Profile.Settings.Sound, Is.False, "el ajuste cambió");
            Assert.That(UnityScreenReaderHost.ScreenFrame(sound.Rect!).width, Is.GreaterThan(0f));

            // Lo que el visor de Ajustes recorta del todo (la lista se desplaza) no se publica: no se ve ni se puede tocar.
            SettingsScreen settings = boot.SettingsView!;
            Rect viewport = UnityScreenReaderHost.ScreenFrame(settings.Viewport);
            foreach (A11yItem item in host.Items)
            {
                if (item.Rect != null && item.Rect.IsChildOf(settings.Viewport))
                {
                    Assert.That(UnityScreenReaderHost.ScreenFrame(item.Rect).Overlaps(viewport), Is.True, item.Label + ": dentro del visor");
                }
            }
        }

        [UnityTest]
        public IEnumerator Luciernaga_TheCuiaToRevealIsChosenWithTheReader()
        {
            // Nivel 100 (estreno de Aguas turbias, 1 Luciérnaga regalada; la cuia 0 es la turbia): abrir Revelar y elegir la cuia
            // con el lector (04 §2.5: en la selección, tocar una cuia es elegirla).
            FunnelSetup.Hooks();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(100, new MemoryProgressStore(100, 99));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 50);
            B9LocPlay.Use();
            var host = new FakeScreenReaderHost(true);
            var service = new A11yService(host);
            boot.View!.A11y = service;
            Assert.That(boot.Flow!.Session.Current[0].HiddenMask, Is.Not.Zero, "la cuia 0 es la turbia");
            Assert.That(boot.PressReveal(), Is.True);
            Assert.That(boot.Flow.RevealSelecting, Is.True);
            PlayModeHelpers.Step(boot, 160);
            service.Refresh();
            Assert.That(host.Items[0].Id, Is.EqualTo("cuia_0"), "en la selección, las cuias siguen publicadas");
            Assert.That(host.Items[0].Invoke!(), Is.True);
            Assert.That(boot.Flow.RevealSelecting, Is.False, "elegida con el lector: se revela");
            boot.View.A11y = null;
        }

        [UnityTest]
        public IEnumerator TheTapAnywhereVeil_IsAContinueButton_AndActivatingItIsTheTap()
        {
            // Llenar y teñir del nivel 1: se salta tocando en cualquier sitio (un relé de toque, no un botón). Con TalkBack no hay
            // toque libre: el velo se publica como «Continuar» y activarlo es el mismo toque.
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            B9LocPlay.Use();
            Assert.That(boot.Flow!.Phase, Is.Not.EqualTo(FlowPhase.Playing), "la intro está en marcha");
            var host = new FakeScreenReaderHost(true);
            var service = new A11yService(host);
            service.Refresh();
            A11yItem? next = host.Find(A11yText.TapToContinue);
            Assert.That(next, Is.Not.Null, "el velo de la intro, como botón");
            Assert.That(next!.Role, Is.EqualTo(AccessibilityRole.Button));
            Assert.That(next.Invoke!(), Is.True);
            yield return PlayModeHelpers.WaitFor(() => boot.Flow.Phase == FlowPhase.Playing && boot.View!.IsViewIdle, "la intro se saltó");
        }

        [UnityTest]
        public IEnumerator Board_CuiasBySlot_ThenTheTray_AndActivatingACuiaIsATap()
        {
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            boot.TapAnywhere();
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.Phase == FlowPhase.Playing && boot.View!.IsViewIdle, "nivel 1 jugable");
            B9LocPlay.Use();
            var host = new FakeScreenReaderHost(true);
            var service = new A11yService(host);
            boot.View!.A11y = service;
            service.Refresh();
            IReadOnlyList<A11yItem> items = new List<A11yItem>(host.Items); // copia: Publish rehace la lista del anfitrión
            int cuias = boot.View.Views.Length;
            Assert.That(items.Count, Is.GreaterThan(cuias));
            for (int i = 0; i < cuias; i++)
            {
                Assert.That(items[i].Id, Is.EqualTo("cuia_" + i), "02 §12: el foco recorre las cuias por orden de ranura");
                Assert.That(items[i].Label, Is.EqualTo(CuiaSpeech.Label(boot.Flow!.Session.Current, i)));
                Assert.That(items[i].Label, Does.StartWith("cuia " + (i + 1) + ":"));
                Assert.That(UnityScreenReaderHost.ScreenFrame(items[i].Rect!).width, Is.GreaterThan(0f), "con su marco en pantalla");
            }

            if (boot.View.Tray.UndoButton.gameObject.activeInHierarchy)
            {
                Assert.That(items[cuias].Label, Does.StartWith(boot.View.Tray.UndoLabel.text), "después la barra de comodines: Marea");
                Assert.That(items[cuias].Role, Is.EqualTo(AccessibilityRole.Button));
            }

            Assert.That(host.Find(A11yText.TapToContinue), Is.Null, "el fondo del tablero (C7) no se publica como «Continuar»");
            A11yItem? pause = host.Find(UiText.Pause);
            Assert.That(pause, Is.Not.Null, "02 §12: la pausa, botón de solo icono, dice «Pausa»");
            Assert.That(pause!.Role, Is.EqualTo(AccessibilityRole.Button));
            Assert.That(items[0].Invoke!(), Is.True);
            Assert.That(boot.Flow!.Selected, Is.EqualTo(0), "activar la cuia 1 la selecciona (C1), como el dedo");
            service.Refresh();
            Assert.That(host.Items[0].Selected, Is.True, "y el lector la dice seleccionada");

            // La pausa por el lector: se abre y tapa el tablero (ni se publica ni se toca a través de ella).
            Assert.That(pause.Invoke!(), Is.True);
            service.Refresh();
            foreach (A11yItem item in host.Items)
            {
                Assert.That(item.Id, Does.Not.StartWith("cuia_"), "la pausa tapa el tablero");
            }

            Assert.That(items[1].Invoke!(), Is.False, "un nodo viejo de una cuia no toca a través de la pausa");
            boot.View.A11y = null;
        }

        private static bool Has(IReadOnlyList<A11yItem> items, AccessibilityRole role, string label)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Role == role && items[i].Label.IndexOf(label, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        // El orden de lectura dentro de cada lienzo (los lienzos van uno tras otro, el de debajo primero): ningún elemento queda
        // claramente por encima del anterior (más de media altura), y en la misma fila, de izquierda a derecha.
        private static void AssertReadingOrder(IReadOnlyList<A11yItem> items)
        {
            Canvas? canvas = null;
            Rect last = default;
            bool has = false;
            for (int i = 0; i < items.Count; i++)
            {
                RectTransform? rect = items[i].Rect;
                if (rect == null)
                {
                    continue;
                }

                Canvas? root = rect.GetComponentInParent<Canvas>()?.rootCanvas;
                if (!ReferenceEquals(root, canvas))
                {
                    canvas = root;
                    has = false;
                }

                Rect frame = UnityScreenReaderHost.ScreenFrame(rect);
                if (has)
                {
                    float half = Mathf.Max(last.height, frame.height) * 0.5f;
                    Assert.That(frame.center.y, Is.GreaterThan(last.center.y - half), items[i].Label + ": de arriba abajo");
                    if (Mathf.Abs(frame.center.y - last.center.y) < Mathf.Min(last.height, frame.height) * 0.25f)
                    {
                        Assert.That(frame.xMin, Is.GreaterThanOrEqualTo(last.xMin), items[i].Label + ": de izquierda a derecha");
                    }
                }

                last = frame;
                has = true;
            }
        }
    }
}
