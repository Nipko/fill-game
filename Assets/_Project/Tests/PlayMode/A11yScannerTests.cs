#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
        public IEnumerator ATapRelay_IsPublished_UnlessItsCanvasGroupStopsTouches()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            var go = new GameObject("A11yRelayCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                Canvas canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 90; // encima del inicio, sin tapar: es pequeño
                CanvasGroup group = go.AddComponent<CanvasGroup>();
                RectTransform relayRect = UiBuilder.Rect("Relay", go.transform);
                UiBuilder.Place(relayRect, 40f, 40f, 80f, 80f);
                UiBuilder.Image("Hit", relayRect, null, new Color(1f, 1f, 1f, 0.5f), raycast: true);
                Image hit = relayRect.GetComponentInChildren<Image>();
                int taps = 0;
                hit.gameObject.AddComponent<PointerTapRelay>().Bind(() => taps++);
                yield return null;

                var host = new FakeScreenReaderHost(true);
                var service = new A11yService(host);
                service.Refresh();
                string id = "tap_" + hit.gameObject.GetInstanceID();
                A11yItem? relay = FindId(host.Items, id);
                Assert.That(relay, Is.Not.Null, "un relé de toque se publica como botón");
                Assert.That(relay!.Invoke!(), Is.True);
                Assert.That(taps, Is.EqualTo(1), "activarlo es el mismo toque");

                group.blocksRaycasts = false; // un velo que se va: un dedo ya no lo alcanza
                service.Refresh();
                Assert.That(FindId(host.Items, id), Is.Null, "ni se publica");
                Assert.That(A11yScanner.Tap(hit.gameObject), Is.False, "ni se toca");
                Assert.That(relay.Invoke(), Is.False);
                Assert.That(taps, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.Destroy(go);
            }
        }

        [UnityTest]
        public IEnumerator CanvasesWithTheSameOrder_AreOneGroup_AndAVeilInItHidesWhatIsBelow()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            var veilGo = new GameObject("A11yVeil50", typeof(RectTransform), typeof(Canvas));
            var buttonGo = new GameObject("A11yButton50", typeof(RectTransform), typeof(Canvas));
            try
            {
                // Dos lienzos con el mismo sortingOrder (Unity no fija cuál va encima): uno con un velo, otro con un botón.
                foreach (GameObject g in new[] { veilGo, buttonGo })
                {
                    Canvas c = g.GetComponent<Canvas>();
                    c.renderMode = RenderMode.ScreenSpaceOverlay;
                    c.sortingOrder = 50;
                    g.AddComponent<GraphicRaycaster>();
                }

                UiBuilder.Stretch(UiBuilder.Image("Veil", veilGo.transform, null, new Color(0f, 0f, 0f, 0.5f), raycast: true).rectTransform);
                Button button = UiBuilder.BareButton("Probe", buttonGo.transform, () => { });
                UiBuilder.Place((RectTransform)button.transform, 40f, 40f, 120f, 60f);
                A11yLabels.Set(button, () => "sonda");
                yield return null;

                var host = new FakeScreenReaderHost(true);
                var service = new A11yService(host);
                service.Refresh();
                Assert.That(host.Find("sonda"), Is.Not.Null, "el grupo del mismo orden se publica entero");
                Assert.That(Has(host.Items, AccessibilityRole.Button, boot.Lobby!.PlayLabel), Is.False, "y tapa lo de debajo");
            }
            finally
            {
                UnityEngine.Object.Destroy(veilGo);
                UnityEngine.Object.Destroy(buttonGo);
            }
        }

        [UnityTest]
        public IEnumerator UnityHost_AnOldNodeDoesNothing_AndAnInPlaceUpdateUsesTheNewItem()
        {
            var go = new GameObject("A11yHostCanvas", typeof(RectTransform), typeof(Canvas));
            var host = new UnityScreenReaderHost();
            try
            {
                go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                RectTransform a = UiBuilder.Rect("A", go.transform);
                UiBuilder.Place(a, 10f, 10f, 50f, 50f);
                RectTransform b = UiBuilder.Rect("B", go.transform);
                UiBuilder.Place(b, 100f, 10f, 70f, 50f);
                yield return null;
                int first = 0;
                int second = 0;
                host.Publish(new[] { new A11yItem("x", "uno", AccessibilityRole.Button, a, () => ++first > 0) });
                Func<bool> old = host.NodeInvoker(0);
                Assert.That(old(), Is.True);
                Assert.That(first, Is.EqualTo(1));

                // Misma identidad, otro elemento: el nodo es el mismo y hace lo nuevo, con el marco nuevo.
                host.Publish(new[] { new A11yItem("x", "uno bis", AccessibilityRole.Button, b, () => ++second > 0) });
                Assert.That(host.NodeInvoker(0)(), Is.True);
                Assert.That(second, Is.EqualTo(1), "la publicación en sitio cambia lo que hace el nodo");
                Assert.That(host.NodeFrame(0)().width, Is.EqualTo(UnityScreenReaderHost.ScreenFrame(b).width).Within(0.5f), "y su marco");

                // Otra identidad: jerarquía nueva; el nodo viejo ya no hace nada.
                host.Publish(new[] { new A11yItem("y", "dos", AccessibilityRole.Button, a, () => true) });
                Assert.That(old(), Is.False, "un nodo de una jerarquía vieja no actúa");
                Assert.That(first, Is.EqualTo(1));
            }
            finally
            {
                host.Clear();
                UnityEngine.Object.Destroy(go);
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

            // Un velo en otro lienzo de encima (como el Mercado o un anuncio): el lector se para en él y una cuia no se toca a través.
            var veil = new GameObject("A11yVeilCanvas", typeof(RectTransform), typeof(Canvas));
            veil.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            veil.GetComponent<Canvas>().sortingOrder = 100;
            UiBuilder.Stretch(UiBuilder.Image("Veil", veil.transform, null, new Color(0f, 0f, 0f, 0.5f), raycast: true).rectTransform);
            Canvas.ForceUpdateCanvases();
            Assert.That(items[1].Invoke!(), Is.False, "Covered: otro lienzo con velo tapa el tablero");
            service.Refresh();
            Assert.That(FindId(host.Items, "cuia_0"), Is.Null, "y el lector no publica las cuias de debajo");
            UnityEngine.Object.DestroyImmediate(veil);
            service.Refresh();
            Assert.That(FindId(host.Items, "cuia_0"), Is.Not.Null);

            // Con el mismo sortingOrder que el lienzo del tablero también tapa (Unity no fija cuál de los dos va encima).
            var tie = new GameObject("A11yTieCanvas", typeof(RectTransform), typeof(Canvas));
            tie.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            tie.GetComponent<Canvas>().sortingOrder = boot.View.Canvas.sortingOrder;
            UiBuilder.Stretch(UiBuilder.Image("Veil", tie.transform, null, new Color(0f, 0f, 0f, 0.5f), raycast: true).rectTransform);
            Canvas.ForceUpdateCanvases();
            Assert.That(items[1].Invoke!(), Is.False, "un velo del mismo orden cuenta como tapa");
            UnityEngine.Object.DestroyImmediate(tie);

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

        private static A11yItem? FindId(IReadOnlyList<A11yItem> items, string id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Id == id)
                {
                    return items[i];
                }
            }

            return null;
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
