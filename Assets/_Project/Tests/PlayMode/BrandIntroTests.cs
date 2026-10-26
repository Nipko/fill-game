#nullable enable
using System;
using System.Collections;
using AF.GameFlow;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Intro de la marca (D-051, punto 3, y su añadido: la cuia del icono vierte el chorro y el açaí tiñe el río desde el impacto;
    /// <see cref="BrandIntro"/>): encima de la pantalla de arranque y sin texto ni toques; corre con el tiempo real y se corta al terminar la carga (nunca la alarga); si la carga dura más, se queda en su último fotograma;
    /// con Reducir movimiento, quieta; TiTino no avanza debajo; sin asignaciones por fotograma; y, en la escena <c>Game</c>, se
    /// quita en el mismo instante en que termina la carga, con la sonda <c>AF_STATE brand_intro</c>.
    /// </summary>
    public sealed class BrandIntroTests
    {
        private GameObject? _host;
        private ProceduralArt? _art;
        private BootSplash? _splash;

        [TearDown]
        public void TearDown()
        {
            _splash?.Dispose();
            _splash = null;
            BootSplash.EnabledOverride = null;
            BrandIntro.EnabledOverride = null;
            MapArtRule.ForceProvisional = null;
            ArtSlots.ForceProcedural = false;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            _art?.Dispose();
            _art = null;
            if (_host != null)
            {
                UnityEngine.Object.Destroy(_host);
            }

            _host = null;
        }

        [Test]
        public void TheAnimation_LastsLessThanOneSecond()
        {
            Assert.That(BrandIntro.AnimMs, Is.LessThanOrEqualTo(1000), "D-051: 1 s como mucho");
            Assert.That(BrandIntro.FallMs, Is.LessThan(BrandIntro.RippleEndMs));
            Assert.That(BrandIntro.RippleEndMs, Is.LessThanOrEqualTo(BrandIntro.AnimMs));
            Assert.That(BrandIntro.Enabled, Is.True, "sale en cada arranque en frío");
        }

        [UnityTest]
        public IEnumerator Intro_OnTopOfTheSplash_OnThePetroleum_NoText_NoTouches()
        {
            MapArtRule.ForceProvisional = true;
            ArtSlots.ForceProcedural = true;
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp
            BootSplash splash = NewSplash();
            Assert.That(splash.ShowSloth(_art!.SlothRigArt(), _art.HomeBranch, true, false), Is.True);
            yield return null;

            BrandIntro intro = splash.Intro!;
            Assert.That(intro, Is.Not.Null);
            Assert.That(splash.IntroVisible, Is.True, "a la vista desde el primer fotograma");
            Assert.That(intro.Root.GetSiblingIndex(), Is.EqualTo(intro.Root.parent.childCount - 1), "por encima de todo, TiTino incluido");
            Assert.That(intro.Root.GetComponentsInChildren<TMP_Text>(true), Is.Empty, "06 §2.1, regla 3: sin texto");
            Assert.That(intro.Root.GetComponentsInChildren<Text>(true), Is.Empty);
            foreach (Graphic g in intro.Root.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(g.raycastTarget, Is.False, g.name + ": la intro no recibe toques (los traga el fondo de la pantalla de arranque)");
            }

            Image background = intro.Root.Find("Petroleum").GetComponent<Image>();
            Assert.That((Color32)background.color, Is.EqualTo(BrandIntro.Petroleum), "el petróleo de la pantalla del sistema");

            // Antes de saber si hay Reducir movimiento, el primer fotograma: la cuia inclinada, el chorro aún en la boca y el río sin
            // teñir.
            splash.Tick(16f);
            Assert.That(intro.Begun, Is.False);
            Assert.That(intro.ClockMs, Is.EqualTo(0f));
            Assert.That(intro.TintFill, Is.EqualTo(0f));
            Assert.That(intro.StreamFill, Is.EqualTo(0f));
            Assert.That(intro.Root.Find("EmblemCuia").GetComponent<Image>().sprite, Is.Not.Null, "la cuia del icono, desde el primer fotograma");
        }

        [UnityTest]
        public IEnumerator ShortLoad_TheIntroIsCut_WithoutWaiting()
        {
            BootSplash splash = NewSplash();
            splash.BeginIntro(false);
            yield return null;
            splash.Tick(16f);

            BrandIntroReport report = splash.EndIntro()!;
            Assert.That(report, Is.Not.Null);
            Assert.That(report.Begun, Is.True);
            Assert.That(report.Cut, Is.True, "la carga terminó antes que la animación: se corta");
            Assert.That(report.ShownMs, Is.LessThan(BrandIntro.AnimMs));
            Assert.That(report.IntroMs, Is.LessThan(BrandIntro.AnimMs), "ni un milisegundo más que la carga");
            Assert.That(report.Frames, Is.GreaterThanOrEqualTo(1));
            Assert.That(splash.IntroVisible, Is.False, "fuera de la vista en el acto");
            Assert.That(splash.Visible, Is.True, "la pantalla de arranque sigue (la corta GameBootstrap)");
            Assert.That(splash.EndIntro(), Is.Null, "una sola vez");
        }

        [UnityTest]
        public IEnumerator LongLoad_TheIntroStaysOnItsLastStableFrame()
        {
            BootSplash splash = NewSplash();
            splash.BeginIntro(false);
            BrandIntro intro = splash.Intro!;
            double until = Time.realtimeSinceStartupAsDouble + ((BrandIntro.AnimMs + 150) / 1000.0);
            bool sawMiddle = false;
            while (Time.realtimeSinceStartupAsDouble < until)
            {
                splash.Tick(16f);
                sawMiddle |= intro.ClockMs > BrandIntro.FallMs && intro.ClockMs < BrandIntro.AnimMs && intro.TintFill > 0f && intro.TintFill < 1f;
                yield return null;
            }

            splash.Tick(16f);
            Assert.That(intro.Finished, Is.True);
            Assert.That(intro.ClockMs, Is.EqualTo((float)BrandIntro.AnimMs));
            Assert.That(intro.TintFill, Is.EqualTo(1f), "el río, teñido río abajo hasta el final");
            Assert.That(intro.StreamFill, Is.EqualTo(1f), "el chorro ya llega al río");
            for (int i = 0; i < 5; i++)
            {
                yield return null;
                splash.Tick(16f);
            }

            Assert.That(intro.ClockMs, Is.EqualTo((float)BrandIntro.AnimMs), "quieta en su último fotograma mientras siga la carga");
            Assert.That(intro.StreamFill, Is.EqualTo(1f));
            Assert.That(intro.TintFill, Is.EqualTo(1f));
            BrandIntroReport report = splash.EndIntro()!;
            Assert.That(report.Cut, Is.False, "la animación terminó antes que la carga");
            Assert.That(report.ShownMs, Is.EqualTo((long)BrandIntro.AnimMs));
            Assert.That(report.IntroMs, Is.GreaterThanOrEqualTo((long)BrandIntro.AnimMs));
            Assert.That(sawMiddle || report.Frames < 4, Is.True, "con fotogramas, se ve el tinte avanzar río abajo");
        }

        [UnityTest]
        public IEnumerator ReduceMotion_TheIntroIsStill_OnItsLastFrame()
        {
            BootSplash splash = NewSplash();
            splash.BeginIntro(true);
            BrandIntro intro = splash.Intro!;
            Assert.That(intro.ReduceMotion, Is.True);
            Assert.That(intro.ClockMs, Is.EqualTo((float)BrandIntro.AnimMs), "sale ya compuesta: el último fotograma");
            float stream = intro.StreamFill;
            float fill = intro.TintFill;
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                splash.Tick(16f);
                Assert.That(intro.StreamFill, Is.EqualTo(stream), "nada se mueve");
                Assert.That(intro.TintFill, Is.EqualTo(fill));
            }

            Assert.That(fill, Is.EqualTo(1f));
            Assert.That(stream, Is.EqualTo(1f));
            BrandIntroReport report = splash.EndIntro()!;
            Assert.That(report.ReduceMotion, Is.True);
            Assert.That(report.Cut, Is.False, "quieta no se corta a medias");
        }

        [UnityTest]
        public IEnumerator TheSlothWaitsUnderTheIntro_ThenStartsItsGesture()
        {
            MapArtRule.ForceProvisional = true;
            ArtSlots.ForceProcedural = true;
            BootSplash splash = NewSplash();
            splash.BeginIntro(false);
            Assert.That(splash.ShowSloth(_art!.SlothRigArt(), _art.HomeBranch, true, false), Is.True);
            splash.Tick(400f);
            Assert.That(splash.SlothClockMs, Is.EqualTo(0f), "bajo la intro, el gesto de TiTino no avanza (su segundo se verá entero)");
            splash.EndIntro();
            splash.Tick(400f);
            Assert.That(splash.SlothClockMs, Is.EqualTo(400f).Within(0.01f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheSplash_IsTheIntrosLastFrame_TheWorldFadesIn_WithoutAJump([Values(false, true)] bool reduceMotion)
        {
            MapArtRule.ForceProvisional = true;
            ArtSlots.ForceProcedural = true;
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            BootSplash splash = NewSplash();
            splash.BeginIntro(reduceMotion);
            Assert.That(splash.ShowSloth(_art!.SlothRigArt(), _art.HomeBranch, true, reduceMotion), Is.True);
            BrandIntro intro = splash.Intro!;
            intro.Seek(BrandIntro.AnimMs);
            splash.Tick(16f);

            // Bajo la intro, el mundo no se hace (en las aperturas que no esperan, nunca se llega a hacer).
            Assert.That(splash.SceneBuilt, Is.False);
            Assert.That(splash.Reveal, Is.EqualTo(0f));

            // El emblema de la pantalla de arranque es el último fotograma de la intro: las mismas piezas, en el mismo sitio y del
            // mismo color, sobre el mismo petróleo.
            foreach (string piece in new[] { "Halo", "EmblemBase", "EmblemAcai", "EmblemImpact", "EmblemStream", "EmblemCuia" })
            {
                Image a = intro.Root.Find(piece).GetComponent<Image>();
                Image b = splash.Root.Find(piece).GetComponent<Image>();
                Assert.That(b.sprite, Is.SameAs(a.sprite), piece + ": la misma textura");
                Assert.That(b.sprite, Is.Not.Null, piece);
                Assert.That(b.color, Is.EqualTo(a.color), piece + ": el mismo color");
                Assert.That(b.rectTransform.anchoredPosition, Is.EqualTo(a.rectTransform.anchoredPosition), piece + ": en el mismo sitio");
                Assert.That(b.rectTransform.pivot, Is.EqualTo(a.rectTransform.pivot), piece + ": el mismo pivote");
                Assert.That(b.rectTransform.localScale, Is.EqualTo(a.rectTransform.localScale), piece + ": a la misma escala");
                Assert.That(b.rectTransform.sizeDelta.x, Is.EqualTo(a.rectTransform.sizeDelta.x).Within(0.001f), piece + ": del mismo ancho");
                Assert.That(b.rectTransform.sizeDelta.y, Is.EqualTo(a.rectTransform.sizeDelta.y).Within(0.001f), piece + ": del mismo alto");
            }

            Image sky = splash.Root.Find("Sky").GetComponent<Image>();
            Assert.That((Color32)sky.color, Is.EqualTo(BrandIntro.Petroleum), "el mismo petróleo de la intro");
            Assert.That(intro.Root.Find("EmblemStream").GetComponent<Image>().fillAmount, Is.EqualTo(1f), "el chorro entero");

            // Al quitarse, el mundo entra fundiéndose (de golpe con Reducir movimiento), sin mover el emblema.
            Assert.That(splash.EndIntro(), Is.Not.Null);
            splash.Tick(16f);
            Assert.That(splash.SceneBuilt, Is.True, "el mundo se hace en el primer fotograma tras la intro");
            Assert.That(splash.Wordmark!.sprite, Is.Not.Null);
            if (reduceMotion)
            {
                Assert.That(splash.Reveal, Is.EqualTo(1f), "con Reducir movimiento, sin fundido");
            }
            else
            {
                Assert.That(splash.Reveal, Is.EqualTo(0f), "empieza invisible: nada salta");
                splash.Tick(BootSplash.RevealMs * 0.5f);
                Assert.That(splash.Reveal, Is.InRange(0.2f, 0.8f));
                splash.Tick(BootSplash.RevealMs);
                Assert.That(splash.Reveal, Is.EqualTo(1f));
            }

            Assert.That(splash.SlothClockMs, Is.GreaterThan(0f), "el gesto de TiTino arranca al quitarse la intro");
            yield return null;

            // Ya entero, el arranque no asigna memoria por fotograma (TiTino, las luciérnagas).
            splash.Tick(16f);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 120; i++)
            {
                splash.Tick(16f);
            }

            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.EqualTo(0L), "sin asignaciones por fotograma (08 §5.4)");
        }

        [UnityTest]
        public IEnumerator TheIntro_DoesNotAllocatePerFrame()
        {
            BootSplash splash = NewSplash();
            splash.BeginIntro(false);
            BrandIntro intro = splash.Intro!;
            yield return null;
            splash.Tick(16f);
            intro.Seek(100f);
            intro.Seek(400f);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i <= 200; i++)
            {
                intro.Seek(i * 3.5f); // la caída, el impacto, las ondas y el tinte
                splash.Tick(16f);
            }

            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.EqualTo(0L), "sin asignaciones por fotograma (08 §5.4)");
        }

        [UnityTest]
        public IEnumerator ColdStart_TheIntroEndsWithTheLoad_NoAddedWait()
        {
            BootSplash.EnabledOverride = true;
            MapArtRule.ForceProvisional = false; // sin TiTino (forzado; D-052 lo saca en todos los builds): nada más que esperar tras la carga
            ArtSlots.ForceProcedural = true;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;

            BrandIntroReport intro = boot.IntroInfo!;
            BootReport report = boot.BootInfo!;
            Assert.That(intro, Is.Not.Null, "la intro sale en el arranque en frío");
            Assert.That(intro.Begun, Is.True);
            Assert.That(intro.ReduceMotion, Is.False);
            Assert.That(intro.IntroMs, Is.InRange(report.LoadMs, report.LoadMs + 20), "se quita al terminar la carga");
            Assert.That(report.HoldMs, Is.EqualTo(0L), "la intro no añade espera");
            Assert.That(report.SplashMs, Is.EqualTo(report.LoadMs));
            Assert.That(intro.Cut, Is.EqualTo(intro.ShownMs < BrandIntro.AnimMs));
            Assert.That(boot.Splash, Is.Null);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "primera apertura: el nivel 1, como siempre");

            string line = new DevProbe().FormatBrandIntro(intro)!;
            StringAssert.StartsWith("AF_STATE {\"event\":\"brand_intro\"", line);
            StringAssert.Contains("\"intro_ms\":" + intro.IntroMs, line);
            StringAssert.Contains("\"anim_ms\":" + BrandIntro.AnimMs, line);
            StringAssert.Contains("\"cut\":" + (intro.Cut ? "true" : "false"), line);
            StringAssert.Contains("\"reduce_motion\":false", line);

            // El primer toque tras la intro llega al juego (nada se queda tragando toques).
            boot.TapAnywhere();
            Assert.That(report.FirstTouchMs, Is.GreaterThanOrEqualTo(report.SplashEndMs));
        }

        [UnityTest]
        public IEnumerator ColdStart_WithoutTheIntro_TheSplashIsAsBefore()
        {
            BootSplash.EnabledOverride = true;
            BrandIntro.EnabledOverride = false;
            MapArtRule.ForceProvisional = false; // forzado: hoy el arte por código sale en todos los builds, release incluido (D-052)
            ArtSlots.ForceProcedural = true;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.IntroInfo, Is.Null);
            Assert.That(boot.BootInfo, Is.Not.Null);
            Assert.That(boot.BootInfo!.HoldMs, Is.EqualTo(0L));
        }

        private BootSplash NewSplash()
        {
            _host ??= new GameObject("BrandIntroTestHost");
            _art ??= new ProceduralArt("white");
            _splash?.Dispose();
            _splash = new BootSplash(_host.transform, withSloth: true, withIntro: true);
            return _splash;
        }
    }
}
