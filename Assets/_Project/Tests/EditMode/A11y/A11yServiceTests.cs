#nullable enable
using System.Collections.Generic;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Accessibility;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El lector de pantalla del juego (bloque B9; <see cref="A11yService"/>) con el anfitrión falso: solo trabaja con TalkBack
    /// encendido, publica lo que dan las fuentes en su orden, no republica si nada cambió, quita todo al apagarlo y solo anuncia
    /// con el lector encendido. El recorrido genérico de las pantallas de verdad lo prueba <c>A11yScannerTests</c> (PlayMode).
    /// </summary>
    public sealed class A11yServiceTests
    {
        private GameObject? _canvasGo;

        [SetUp]
        public void SetUp()
        {
            _canvasGo = new GameObject("A11yTestCanvas", typeof(RectTransform), typeof(Canvas));
            _canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }

        [TearDown]
        public void TearDown()
        {
            if (_canvasGo != null)
            {
                Object.DestroyImmediate(_canvasGo);
            }
        }

        [Test]
        public void WithTheReaderOff_NothingIsPublished_NorAnnounced()
        {
            var host = new FakeScreenReaderHost(false);
            var service = new A11yService(host);
            var source = new Source(_canvasGo!.GetComponent<Canvas>(), "cuia 1: Açaí, 3 huecos");
            service.Scanner.AddSource(source);
            service.Tick(16f);
            service.Announce("Açaí seleccionado: elige destino");
            Assert.That(host.Publishes, Is.Zero);
            Assert.That(host.Announcements, Is.Empty);
            Assert.That(source.Calls, Is.Zero, "con el lector apagado ni siquiera se recorre la interfaz (coste cero)");
        }

        [Test]
        public void WithTheReaderOn_TheSourcesItemsArePublishedInOrder_AndOnlyWhenTheyChange()
        {
            var host = new FakeScreenReaderHost(true);
            var service = new A11yService(host);
            var source = new Source(_canvasGo!.GetComponent<Canvas>(), "cuia 1: Açaí, 3 huecos", "Deshacer, 2 restantes");
            service.Scanner.AddSource(source);
            service.Tick(16f);
            Assert.That(host.Publishes, Is.EqualTo(1));
            Assert.That(host.Items.Count, Is.EqualTo(2));
            Assert.That(host.Items[0].Label, Is.EqualTo("cuia 1: Açaí, 3 huecos"), "02 §12: primero las cuias");
            Assert.That(host.Items[1].Label, Is.EqualTo("Deshacer, 2 restantes"), "después la barra de comodines");

            service.Tick(100f);
            Assert.That(source.Calls, Is.EqualTo(1), "cada 250 ms [P], no en cada fotograma");
            service.Tick(200f);
            Assert.That(source.Calls, Is.EqualTo(2));
            Assert.That(host.Publishes, Is.EqualTo(1), "sin cambios no se vuelve a publicar (el foco no salta)");

            source.Labels[1] = "Deshacer, 1 restante";
            service.Invalidate();
            service.Tick(0f);
            Assert.That(host.Publishes, Is.EqualTo(2), "tras un toque se describe otra vez al momento");
            Assert.That(host.Items[1].Label, Is.EqualTo("Deshacer, 1 restante"));

            service.Announce("Açaí seleccionado: elige destino");
            Assert.That(host.Announcements, Is.EqualTo(new[] { "Açaí seleccionado: elige destino" }));
        }

        [Test]
        public void TurningTheReaderOff_ClearsWhatWasPublished()
        {
            var host = new FakeScreenReaderHost(true);
            var service = new A11yService(host);
            service.Scanner.AddSource(new Source(_canvasGo!.GetComponent<Canvas>(), "cuia 1: 4 huecos"));
            service.Tick(16f);
            Assert.That(host.Items, Is.Not.Empty);
            host.ScreenReaderOn = false;
            service.Tick(16f);
            Assert.That(host.Items, Is.Empty);
            Assert.That(service.Published, Is.Empty);
            host.ScreenReaderOn = true;
            service.Tick(16f);
            Assert.That(host.Items, Is.Not.Empty, "al volver a encenderlo, se describe enseguida");
        }

        [Test]
        public void Items_CompareByIdentityAndContent()
        {
            var a = new A11yItem("cuia_0", "x", AccessibilityRole.Button, null, null);
            var b = new A11yItem("cuia_0", "x", AccessibilityRole.Button, null, null);
            Assert.That(a.SameAs(b), Is.True);
            b.Selected = true;
            Assert.That(a.SameAs(b), Is.False, "la selección cambia lo que dice el lector");
        }

        private sealed class Source : IA11ySource
        {
            private readonly Canvas _canvas;

            public Source(Canvas canvas, params string[] labels)
            {
                _canvas = canvas;
                Labels = labels;
            }

            public string[] Labels { get; }

            public int Calls { get; private set; }

            public bool Collect(Canvas canvas, A11yScanner scanner, List<A11yItem> into)
            {
                if (!ReferenceEquals(canvas, _canvas))
                {
                    return false;
                }

                Calls++;
                for (int i = 0; i < Labels.Length; i++)
                {
                    into.Add(new A11yItem("item_" + i, Labels[i], AccessibilityRole.Button, null, () => true));
                }

                return true;
            }
        }
    }
}
