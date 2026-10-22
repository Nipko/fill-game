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

            // Lo que el visor de Ajustes recorta del todo también se publica si la lista se desplaza (Q-B9-17), con la lista; si
            // no se desplaza, todo se ve y nada lleva lista.
            SettingsScreen settings = boot.SettingsView!;
            Rect viewport = UnityScreenReaderHost.ScreenFrame(settings.Viewport);
            foreach (A11yItem item in host.Items)
            {
                if (item.Rect != null && item.Rect.IsChildOf(settings.Viewport))
                {
                    Assert.That(item.Scroll != null, Is.EqualTo(settings.Scrolls), item.Label + ": con la lista solo si se desplaza");
                    if (!settings.Scrolls)
                    {
                        Assert.That(UnityScreenReaderHost.ScreenFrame(item.Rect).Overlaps(viewport), Is.True, item.Label + ": dentro del visor");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator ScrollList_EveryRowIsPublishedInOrder_AndTheReaderScrollsToIt()
        {
            // Q-B9-17: una lista de 10 filas en un visor de 3 (las de abajo, recortadas y fuera de la pantalla) y un botón debajo.
            var go = new GameObject("A11yListCanvas", typeof(RectTransform), typeof(Canvas));
            var host = new UnityScreenReaderHost();
            try
            {
                Canvas canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 95;
                ScrollRect list = BuildList(go.transform, 10, out Button[] rows, out Button after);
                yield return null;
                Canvas.ForceUpdateCanvases();

                var scanner = new A11yScanner();
                var all = new List<A11yItem>();
                scanner.Scan(all);
                List<A11yItem> items = Under(all, go.transform);
                Assert.That(items.Count, Is.EqualTo(11), "las 10 filas, también las recortadas, y el botón de debajo");
                for (int i = 0; i < rows.Length; i++)
                {
                    Assert.That(items[i].Label, Is.EqualTo("fila " + i), "las filas seguidas, en su orden");
                    Assert.That(items[i].Scroll, Is.SameAs(list), "cada fila, con su lista");
                }

                Assert.That(items[10].Label, Is.EqualTo("después"), "lo de debajo del visor va tras la última fila, aunque esté por encima de ella en pantalla");
                Assert.That(items[10].Scroll, Is.Null);
                Assert.That(A11yScroll.FullyShown(list, (RectTransform)rows[8].transform), Is.False, "la fila 8, bajo el corte");

                host.Publish(items);
                Rect strip = host.NodeFrame(8)();
                Rect view = UnityScreenReaderHost.ScreenFrame(list.viewport);
                Assert.That(strip.height, Is.EqualTo(A11yScroll.EdgeStripPx).Within(0.01f), "recortada: una franja en el borde del visor");
                Assert.That(strip.yMax, Is.EqualTo(view.yMax).Within(0.5f), "en el de abajo");

                // El foco llega a la fila 8: el visor se desplaza hasta enseñarla entera; ya a la vista, no se mueve más.
                Assert.That(host.NodeReveal(8)(), Is.True);
                Assert.That(A11yScroll.FullyShown(list, (RectTransform)rows[8].transform), Is.True, "la fila enfocada, entera en el visor");
                Assert.That(host.NodeReveal(8)(), Is.False);
                Assert.That(host.NodeFrame(8)().height, Is.EqualTo(UnityScreenReaderHost.ScreenFrame((RectTransform)rows[8].transform).height).Within(0.5f), "y su marco es el de verdad");
                Assert.That(host.NodeReveal(10)(), Is.False, "fuera de la lista no se desplaza nada");

                // La acción de desplazar: una página hacia arriba hasta el tope, y hacia abajo hasta el otro.
                int pages = 0;
                while (host.NodeScroller(8)(AccessibilityScrollDirection.Backward) && pages < 20)
                {
                    pages++;
                }

                Assert.That(pages, Is.GreaterThan(0));
                Assert.That(list.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.01f), "arriba del todo");
                Assert.That(host.NodeScroller(0)(AccessibilityScrollDirection.Up), Is.False, "en el tope no se mueve");
                Assert.That(host.NodeScroller(0)(AccessibilityScrollDirection.Left), Is.False, "una lista vertical no se desplaza de lado");
                Assert.That(host.NodeScroller(0)(AccessibilityScrollDirection.Forward), Is.True, "una página hacia abajo");
                Assert.That(list.verticalNormalizedPosition, Is.LessThan(1f));
                while (host.NodeScroller(0)(AccessibilityScrollDirection.Down) && pages < 40)
                {
                    pages++;
                }

                Assert.That(list.verticalNormalizedPosition, Is.EqualTo(0f).Within(0.01f), "abajo del todo");
                Assert.That(A11yScroll.FullyShown(list, (RectTransform)rows[9].transform), Is.True, "la última fila, a la vista");

                // Si la lista deja de desplazarse (cabe entera), sus filas se publican como las de cualquier pantalla.
                list.vertical = false;
                scanner.Scan(all);
                foreach (A11yItem item in Under(all, go.transform))
                {
                    Assert.That(item.Scroll, Is.Null, item.Label);
                }
            }
            finally
            {
                host.Clear();
                UnityEngine.Object.Destroy(go);
            }
        }

        [UnityTest]
        public IEnumerator Settings_At130In16x9_TheRowsUnderTheCutReachTheReader()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            yield return null;
            SettingsScreen settings = boot.SettingsView!;
            settings.FontScale = 1.3f;
            Canvas.ForceUpdateCanvases();
            Assert.That(settings.Scrolls, Is.True, "en 16:9 al 130 % no cabe: se desplaza");
            ScrollRect list = settings.Viewport.GetComponent<ScrollRect>();
            var host = new UnityScreenReaderHost();
            try
            {
                var service = new A11yService(host);
                service.Refresh();
                IReadOnlyList<A11yItem> items = new List<A11yItem>(service.Published);
                int last = -1;
                int clipped = 0;
                foreach (Button button in settings.Viewport.GetComponentsInChildren<Button>(false))
                {
                    Rect frame = UnityScreenReaderHost.ScreenFrame((RectTransform)button.transform);
                    if (!button.enabled || frame.width <= 0f || frame.height <= 0f)
                    {
                        continue;
                    }

                    int index = IndexOfId(items, "btn_" + button.GetInstanceID());
                    Assert.That(index, Is.GreaterThanOrEqualTo(0), button.name + ": toda fila de Ajustes llega al lector");
                    Assert.That(items[index].Scroll, Is.SameAs(list), button.name);
                    if (!A11yScroll.FullyShown(list, (RectTransform)button.transform))
                    {
                        clipped++;
                    }

                    last = Math.Max(last, index);
                }

                Assert.That(clipped, Is.GreaterThan(0), "hay filas bajo el corte");
                Assert.That(settings.ScrollPosition, Is.EqualTo(1f).Within(0.01f), "arriba del todo");
                Assert.That(host.NodeReveal(last)(), Is.True, "el foco en la última fila desplaza el visor");
                Assert.That(A11yScroll.FullyShown(list, items[last].Rect!), Is.True, "y la enseña entera");
                Assert.That(settings.ScrollPosition, Is.LessThan(0.5f));
                Assert.That(items[last].Invoke, Is.Not.Null, "y se activa como cualquier botón");
            }
            finally
            {
                host.Clear();
                GameScreen.SimulatedScreen = null;
            }
        }

        // Una lista de rowCount filas de 50 en un visor de 150 en (40, 40) del lienzo, con un botón «después» debajo del visor.
        internal static ScrollRect BuildList(Transform parent, int rowCount, out Button[] rows, out Button after)
        {
            RectTransform viewport = UiBuilder.Rect("Viewport", parent);
            UiBuilder.Place(viewport, 40f, 40f, 200f, 150f);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = UiBuilder.Rect("Content", viewport);
            UiBuilder.Place(content, 0f, 0f, 200f, rowCount * 50f);
            ScrollRect list = viewport.gameObject.AddComponent<ScrollRect>();
            list.viewport = viewport;
            list.content = content;
            list.horizontal = false;
            list.vertical = true;
            list.movementType = ScrollRect.MovementType.Clamped;
            list.inertia = false;
            rows = new Button[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                string label = "fila " + i;
                rows[i] = UiBuilder.BareButton("Row" + i, content, () => { });
                UiBuilder.Place((RectTransform)rows[i].transform, 0f, i * 50f, 200f, 48f);
                A11yLabels.Set(rows[i], () => label);
            }

            after = UiBuilder.BareButton("After", parent, () => { });
            UiBuilder.Place((RectTransform)after.transform, 40f, 200f, 200f, 48f);
            A11yLabels.Set(after, () => "después");
            return list;
        }

        // Los elementos de items que van bajo root, en orden.
        internal static List<A11yItem> Under(IReadOnlyList<A11yItem> items, Transform root)
        {
            var under = new List<A11yItem>();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Rect != null && items[i].Rect!.IsChildOf(root))
                {
                    under.Add(items[i]);
                }
            }

            return under;
        }

        private static int IndexOfId(IReadOnlyList<A11yItem> items, string id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
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
