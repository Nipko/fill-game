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
            GameBootstrap.IntroFrameHookForTests = null;
            BrandIntro.FrameVisibleOverrideForTests = null;
            BrandIntro.ClockFrozenForTests = false;
            GameBootstrap.IntroMotionCapOverrideMsForTests = null;
            GameBootstrap.IntroMotionHookForTests = null;
            _art?.Dispose();
            _art = null;
            if (_host != null)
            {
                UnityEngine.Object.Destroy(_host);
            }

            _host = null;
        }

        [Test]
        public void TheIntro_LastsAboutThreeAndAHalfSeconds()
        {
            Assert.That(BrandIntro.AnimMs, Is.InRange(3000, 4000), "añadido de D-051 del 2026-10-28: unos 3,5 s, un segundo más que antes");
            Assert.That(BrandIntro.FallMs, Is.InRange(700, BrandIntro.RippleEndMs - 1), "el segundo de más, repartido: el vertido se aprecia");
            Assert.That(BrandIntro.RippleEndMs, Is.LessThanOrEqualTo(BrandIntro.MotionMs));
            Assert.That(BrandIntro.MotionMs, Is.InRange(2000, BrandIntro.AnimMs - 500), "anima ~2,5 s y sostiene el emblema");
            Assert.That(BrandIntro.WordmarkFadeMs, Is.GreaterThanOrEqualTo(450), "y la entrada de «Tintalú»");
            Assert.That(BrandIntro.SettledMs, Is.EqualTo(BrandIntro.WordmarkFromMs + BrandIntro.WordmarkFadeMs));
            Assert.That(BrandIntro.SettledMs, Is.LessThanOrEqualTo(BrandIntro.AnimMs - 300), "el nombre se lee entero un rato");
            Assert.That(BrandIntro.AnimMs - BrandIntro.SettledMs, Is.LessThanOrEqualTo(700), "no una pausa larga al final");
            Assert.That(BrandIntro.ReduceMotionHoldMs, Is.EqualTo(1000), "con Reducir movimiento, ~1 s quieta");
            Assert.That(BrandIntro.Enabled, Is.True, "sale en cada arranque en frío");
        }

        [UnityTest]
        public IEnumerator TheIntro_EndsWithTheEmblemAndTheName()
        {
            BootSplash splash = NewSplash();
            splash.Tick(16f);
            BrandIntro intro = splash.Intro!;
            intro.Seek(BrandIntro.MotionMs - 400f);
            splash.Tick(0f);
            Assert.That(intro.WordmarkAlpha, Is.EqualTo(0f), "durante el movimiento, aún sin nombre");
            intro.Seek(BrandIntro.MotionMs);
            splash.Tick(0f);
            Assert.That(intro.TintFill, Is.EqualTo(1f), "el açaí ya llegó al primer plano");
            Assert.That(splash.Wordmark, Is.Not.Null, "el nombre se hace a tiempo");
            intro.Seek(BrandIntro.AnimMs);
            splash.Tick(0f);
            Assert.That(intro.WordmarkAlpha, Is.EqualTo(1f));
            Assert.That(splash.Wordmark!.color.a, Is.EqualTo(1f), "el emblema con «Tintalú»");
            Assert.That(splash.Wordmark.transform.GetSiblingIndex(), Is.GreaterThan(intro.Root.GetSiblingIndex()), "el nombre, por encima de la intro");
            splash.EndIntro();
            splash.Tick(16f);
            Assert.That(splash.Wordmark.color.a, Is.EqualTo(1f), "al quitarse la intro, el nombre no parpadea");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Loading_ShowsOnlyAfterTheWholeIntro_WhileTheLoadGoesOn()
        {
            BootSplash splash = NewSplash();
            splash.SetLoadingText("Cargando…");
            splash.BeginIntro(false);
            BrandIntro intro = splash.Intro!;
            splash.Tick(16f);
            Assert.That(intro.Done, Is.False);
            Assert.That(splash.IntroPending, Is.True, "el arranque la espera");
            Assert.That(splash.LoadingVisible, Is.False, "mientras anima, nada de «Cargando…»");
            Assert.That(splash.LoadingLabel, Is.Null, "ni siquiera existe (sin texto en la pantalla de arranque)");

            intro.Seek(BrandIntro.AnimMs);
            splash.Tick(16f);
            Assert.That(intro.Done, Is.True);
            Assert.That(splash.IntroPending, Is.False);
            Assert.That(splash.LoadingVisible, Is.True, "la intro se vio entera y la carga sigue");
            Assert.That(splash.LoadingLabel!.text, Is.EqualTo("Cargando…"));
            Assert.That(splash.LoadingLabel.rectTransform.anchoredPosition.y, Is.LessThan(-splash.WordmarkBox.yMax), "bajo el nombre");
            Assert.That(splash.LoadingDots!.Animating, Is.True, "con sus puntos");
            float a0 = splash.LoadingDots.AlphaOf(0);
            for (int i = 0; i < 6; i++)
            {
                yield return null;
                splash.Tick(100f);
            }

            Assert.That(splash.LoadingDots.AlphaOf(0), Is.Not.EqualTo(a0), "los puntos se mueven");
            BrandIntroReport report = splash.EndIntro()!;
            Assert.That(report.LoadingMs, Is.GreaterThan(0L), "loading_ms: lo que se vio");
            Assert.That(splash.LoadingVisible, Is.False, "al acabar la carga se quita");
            Assert.That(report.Cut, Is.False);
        }

        [UnityTest]
        public IEnumerator Loading_WithoutText_ShowsNothing()
        {
            BootSplash splash = NewSplash();
            splash.BeginIntro(false);
            splash.Intro!.Seek(BrandIntro.AnimMs);
            splash.Tick(16f);
            Assert.That(splash.LoadingVisible, Is.False, "sin textos aún, nada");
            Assert.That(splash.EndIntro()!.LoadingMs, Is.EqualTo(0L));
            yield return null;
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

        // Quitarla antes de tiempo solo pasa si el arranque se para (catálogo roto, <13): GameBootstrap la espera siempre.
        [UnityTest]
        public IEnumerator EndedEarly_TheIntroIsCut_AndNoted()
        {
            BootSplash splash = NewSplash();
            splash.BeginIntro(false);
            yield return null;
            splash.Tick(16f);

            BrandIntroReport report = splash.EndIntro()!;
            Assert.That(report, Is.Not.Null);
            Assert.That(report.Begun, Is.True);
            Assert.That(report.Cut, Is.True, "se quitó antes de verse entera: cut");
            Assert.That(report.ShownMs, Is.LessThan(BrandIntro.AnimMs));
            Assert.That(report.IntroMs, Is.LessThan(BrandIntro.AnimMs));
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
            Assert.That(intro.WordmarkAlpha, Is.EqualTo(1f), "entera: con el nombre");
            Assert.That(splash.Wordmark, Is.Not.Null);
            Assert.That(splash.Wordmark!.color.a, Is.EqualTo(1f));
            Assert.That(intro.Done, Is.False, "se sostiene ~1 s");
            double until = Time.realtimeSinceStartupAsDouble + ((BrandIntro.ReduceMotionHoldMs + 50) / 1000.0);
            while (Time.realtimeSinceStartupAsDouble < until)
            {
                yield return null;
                splash.Tick(16f);
            }

            Assert.That(intro.Done, Is.True, "tras su segundo, ya se vio");
            Assert.That(intro.TintFill, Is.EqualTo(fill), "y nada se movió");
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
            Assert.That(splash.SlothClockMs, Is.EqualTo(0f), "el fotograma en que se hace el mundo no cuenta");
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
            var pieces = new System.Collections.Generic.List<string> { "Halo", "EmblemBase", "EmblemIsland", "EmblemImpact", "EmblemStream", "EmblemCuia" };
            for (int k = 0; k < BrandRiverScene.ChunkCount; k++)
            {
                pieces.Add("EmblemAcai" + k);
            }

            foreach (string piece in pieces)
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

            splash.Tick(16f);
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
        public IEnumerator ColdStart_TheIntroIsSeenWhole_EvenIfTheLoadEndsBefore()
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
            Assert.That(report.LoadMs - boot.IntroMotionHoldMs, Is.LessThan((long)BrandIntro.AnimMs), "en el Editor la carga acaba antes que la intro");
            Assert.That(intro.IntroMs, Is.GreaterThanOrEqualTo((long)BrandIntro.AnimMs), "añadido de D-051 del 2026-10-26: entera, aunque la carga acabe antes");
            // El margen es tiempo de reloj: la CI (runner Linux lento) lo multiplica (dio 800 ms sobre la animación el 2026-10-27).
            Assert.That(intro.IntroMs - intro.PlayMs, Is.LessThan(BrandIntro.AnimMs + PerfBudget.Ms(600d)), "y sin más espera que la suya, " + PerfBudget.Describe(600d));
            Assert.That(intro.Cut, Is.False, "nunca se corta porque acabe la carga");
            Assert.That(intro.ShownMs, Is.EqualTo((long)BrandIntro.AnimMs));
            if (report.LoadMs < intro.PlayMs + BrandIntro.AnimMs)
            {
                Assert.That(intro.LoadingMs, Is.EqualTo(0L), "la carga acabó antes: no se vio «Cargando…»");
            }

            // Jornada 4 (2026-10-28): el vertido empieza en un hueco de la carga, con su primer fotograma ya en la pantalla, y ningún
            // bloque de la carga acaba mientras se mueve (del chorro al nombre entero): sus fotogramas salen a su ritmo.
            Assert.That(intro.VisibleBy, Is.Not.EqualTo(BrandIntroVisibleBy.None), "se supo que el primer fotograma estaba en la pantalla");
            Assert.That(intro.VisibleMs, Is.GreaterThanOrEqualTo(intro.FirstFrameMs));
            Assert.That(intro.PlayMs, Is.GreaterThanOrEqualTo(intro.VisibleMs), "no se mueve antes de verse");
            Assert.That(boot.IntroMotionHoldMs, Is.GreaterThanOrEqualTo((long)BrandIntro.SettledMs - 50), "la carga esperó al movimiento entero");
            Assert.That(intro.MotionFrames, Is.GreaterThan(20), "el vertido, con fotogramas a su ritmo");
            Assert.That(intro.FrameMaxMs, Is.GreaterThan(0f));
            Assert.That(intro.FrameP95Ms, Is.InRange(0f, intro.FrameMaxMs));
            for (int i = 0; i < GameBootstrap.BootStepCount; i++)
            {
                long at = boot.BootStepMs(i);
                Assert.That(at > intro.PlayMs + 5 && at < intro.PlayMs + BrandIntro.SettledMs - 5, Is.False, "bloque " + i + " (" + at + " ms): la carga no corre mientras la intro se mueve (play_ms " + intro.PlayMs + ")");
            }
            Assert.That(report.HoldMs, Is.EqualTo(report.SplashMs - report.LoadMs), "hold_ms: lo que se esperó a la intro tras la carga");
            Assert.That(report.SplashMs, Is.InRange(intro.IntroMs, intro.IntroMs + 100), "sin TiTino, se corta al acabar la intro");
            Assert.That(boot.Splash, Is.Null);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "primera apertura: el nivel 1, como siempre");

            string line = new DevProbe().FormatBrandIntro(intro)!;
            StringAssert.StartsWith("AF_STATE {\"event\":\"brand_intro\"", line);
            StringAssert.Contains("\"intro_ms\":" + intro.IntroMs, line);
            StringAssert.Contains("\"anim_ms\":" + BrandIntro.AnimMs, line);
            StringAssert.Contains("\"cut\":" + (intro.Cut ? "true" : "false"), line);
            StringAssert.Contains("\"reduce_motion\":false", line);
            StringAssert.Contains("\"first_frame_ms\":" + intro.FirstFrameMs, line);
            StringAssert.Contains("\"emblem_cache\":\"off\"", line, "en el Editor, sin caché de disco");
            StringAssert.Contains("\"yields\":" + intro.Yields, line);
            StringAssert.Contains("\"visible_ms\":" + intro.VisibleMs + ",\"visible_by\":\"" + DevProbe.VisibleById(intro.VisibleBy) + "\",\"play_ms\":" + intro.PlayMs, line);
            StringAssert.Contains("\"motion_frames\":" + intro.MotionFrames, line);
            StringAssert.Contains("\"wait_capped\":false}", line);
            StringAssert.Contains("\"cut\":false", line);

            // Jornada 3 de D-051: la intro se ve moverse (la carga deja pintar fotogramas entre sus bloques mientras anima), su
            // reloj cuenta desde su primer fotograma y la sonda boot_steps reparte la carga por bloques, en orden.
            Assert.That(intro.FirstFrameMs, Is.InRange(0L, intro.IntroMs), "el primer fotograma, dentro de la carga");
            Assert.That(intro.StartMs, Is.GreaterThanOrEqualTo(0L));
            Assert.That(intro.EmblemCache, Is.EqualTo(BrandEmblemCacheResult.Off));
            Assert.That(intro.Yields, Is.GreaterThanOrEqualTo(1), "al menos un fotograma de más para la intro");
            Assert.That(intro.Yields, Is.LessThanOrEqualTo(4), "como mucho uno por hueco seguro de la carga");
            Assert.That(intro.Frames, Is.GreaterThan(intro.Yields), "cada fotograma dejado se pinta");
            Assert.That(intro.Frames, Is.GreaterThan(20), "a su ritmo: fluida, no 3 fotogramas");
            Assert.That(boot.IntroYields, Is.EqualTo(intro.Yields));
            long previous = 0;
            for (int i = 0; i < GameBootstrap.BootStepCount; i++)
            {
                long at = boot.BootStepMs(i);
                Assert.That(at, Is.GreaterThanOrEqualTo(previous), "bloque " + i + ": en orden");
                previous = at;
            }

            Assert.That(previous, Is.LessThanOrEqualTo(report.LoadMs), "el último bloque acaba con la carga");
            string steps = boot.FormatBootSteps(report);
            StringAssert.StartsWith("AF_PERF {\"event\":\"boot_steps\",\"first_open\":true", steps);
            StringAssert.Contains("\"load_ms\":" + report.LoadMs, steps);
            StringAssert.Contains("\"emblem_cache\":\"off\"", steps);
            StringAssert.Contains("\"visible_ms\":" + intro.VisibleMs + ",\"play_ms\":" + intro.PlayMs + ",\"motion_hold_ms\":" + boot.IntroMotionHoldMs, steps);
            StringAssert.Contains("\"proc_awake_ms\":-1,\"proc_visible_ms\":-1", steps, "fuera del teléfono, sin tiempos del proceso");
            StringAssert.Contains("\"yields\":" + intro.Yields + "}", steps);

            // El primer toque tras la intro llega al juego (nada se queda tragando toques).
            boot.TapAnywhere();
            Assert.That(report.FirstTouchMs, Is.GreaterThanOrEqualTo(report.SplashEndMs));
        }

        [UnityTest]
        public IEnumerator ColdStart_WithReduceMotion_StillAndHeldAboutOneSecond()
        {
            BootSplash.EnabledOverride = true;
            MapArtRule.ForceProvisional = false;
            ArtSlots.ForceProcedural = true;
            AF.Save.Profile reduced = SettingsScreenTests.ProfileAt(4);
            reduced.Settings.ReduceMotion = AF.Save.ReduceMotionSetting.On;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(reduced));
            BrandIntroReport intro = GameBootstrap.Current!.IntroInfo!;
            Assert.That(intro.ReduceMotion, Is.True);
            Assert.That(intro.ShownMs, Is.EqualTo((long)BrandIntro.AnimMs), "quieta, en su último fotograma");
            Assert.That(intro.IntroMs - Math.Max(intro.FirstFrameMs, 0L), Is.GreaterThanOrEqualTo((long)BrandIntro.ReduceMotionHoldMs), "se sostiene ~1 s");
            Assert.That(intro.IntroMs, Is.LessThan((long)BrandIntro.AnimMs), "más corta que la intro animada");
            Assert.That(intro.Cut, Is.False);
        }

        [UnityTest]
        public IEnumerator TheClock_StartsWhenItMoves_NotWhileTheSceneIsBeingMade()
        {
            BrandIntro.FrameVisibleOverrideForTests = () => true;
            BootSplash splash = NewSplash();
            BrandIntro intro = splash.Intro!;
            Assert.That(intro.FirstFrameMs, Is.EqualTo(-1L), "aún sin pintar");

            // Un hueco largo entre la creación y el primer fotograma (lo que cuesta dibujar la escena en la primera apertura).
            BusyWait(0.3);
            splash.Tick(16f);
            Assert.That(intro.FirstFrameMs, Is.GreaterThanOrEqualTo(290L));
            splash.Tick(16f); // el segundo: el primer fotograma ya está enviado a la GPU
            splash.BeginIntro(false);
            Assert.That(intro.Playing, Is.False, "aún quieta, en su primer fotograma");
            Assert.That(intro.ClockMs, Is.EqualTo(0f));

            // Otro hueco (la carga) antes de empezar: tampoco se come el vertido, que cuenta desde que empieza a moverse.
            BusyWait(0.2);
            splash.RefreshIntro();
            Assert.That(intro.Playing, Is.True, "empieza sola (AutoPlay) en cuanto puede");
            Assert.That(intro.ClockMs, Is.LessThan(50f), "el vertido, desde su instante 0");
            Assert.That(intro.StreamFill, Is.LessThan(1f));
            Assert.That(splash.IntroAnimating, Is.True);

            // Refresh lleva la animación a ahora sin contar fotograma (GameBootstrap, antes de dejar pintar uno).
            int frames = intro.Frames;
            BusyWait(0.2);
            splash.RefreshIntro();
            Assert.That(intro.ClockMs, Is.GreaterThanOrEqualTo(190f));
            Assert.That(intro.Frames, Is.EqualTo(frames));
            intro.Seek(BrandIntro.AnimMs);
            Assert.That(splash.IntroAnimating, Is.False, "en su último fotograma ya no pide fotogramas");
            yield return null;
        }

        // Jornada 4 (2026-10-28): en la primera apertura la pantalla seguía lisa ~1 s aunque el juego ya hubiera mandado la intro;
        // el vertido no empieza hasta saber que su primer fotograma ya está en la pantalla, y no se pierde a medias sin verse.
        [UnityTest]
        public IEnumerator TheMotion_WaitsUntilTheFirstFrameIsOnScreen()
        {
            bool onScreen = false;
            BrandIntro.FrameVisibleOverrideForTests = () => onScreen;
            BootSplash splash = NewSplash();
            BrandIntro intro = splash.Intro!;
            splash.BeginIntro(false);
            splash.Tick(16f);
            for (int i = 0; i < 5; i++)
            {
                yield return null;
                splash.Tick(16f);
            }

            BusyWait(0.15);
            splash.Tick(16f);
            Assert.That(intro.OnScreen, Is.False);
            Assert.That(intro.Playing, Is.False, "sin verse aún, quieta");
            Assert.That(intro.ClockMs, Is.EqualTo(0f));
            Assert.That(intro.StreamFill, Is.EqualTo(0f), "el chorro, aún en la boca de la cuia");
            Assert.That(intro.Done, Is.False);
            Assert.That(splash.IntroPending, Is.True, "el arranque la espera");

            onScreen = true;
            splash.Tick(16f);
            Assert.That(intro.OnScreen, Is.True);
            Assert.That(intro.VisibleBy, Is.EqualTo(BrandIntroVisibleBy.Gpu));
            Assert.That(intro.Playing, Is.True, "en cuanto se ve, empieza");
            Assert.That(intro.ClockMs, Is.EqualTo(0f), "desde su instante 0");
            BusyWait(0.2);
            splash.Tick(16f);
            Assert.That(intro.ClockMs, Is.GreaterThanOrEqualTo(190f));

            BrandIntroReport report = splash.EndIntro()!;
            Assert.That(report.VisibleMs, Is.GreaterThanOrEqualTo(report.FirstFrameMs + 140), "visible_ms: cuando se supo");
            Assert.That(report.PlayMs, Is.GreaterThanOrEqualTo(report.VisibleMs));
            Assert.That(report.MotionFrames, Is.EqualTo(1), "un fotograma mientras se movía");
            Assert.That(report.FrameMaxMs, Is.GreaterThanOrEqualTo(190f), "el hueco de 0,2 s, en el máximo");
            Assert.That(report.FrameP95Ms, Is.EqualTo(report.FrameMaxMs));
            StringAssert.Contains("\"visible_by\":\"gpu\"", new DevProbe().FormatBrandIntro(report)!);
        }

        [UnityTest]
        public IEnumerator TheMotion_StartsAnyway_WhenTheGpuDoesNotAnswer()
        {
            BrandIntro.FrameVisibleOverrideForTests = () => false;
            BootSplash splash = NewSplash();
            BrandIntro intro = splash.Intro!;
            splash.BeginIntro(false);
            splash.Tick(16f);
            Assert.That(intro.Playing, Is.False);
            BusyWait((BrandIntro.VisibleWaitCapMs + 30) / 1000.0);
            splash.Tick(16f);
            Assert.That(intro.VisibleBy, Is.EqualTo(BrandIntroVisibleBy.Cap), "tope de la espera a la GPU");
            Assert.That(intro.Playing, Is.True, "la intro no se queda quieta para siempre");
            BrandIntroReport report = splash.EndIntro()!;
            StringAssert.Contains("\"visible_by\":\"cap\"", new DevProbe().FormatBrandIntro(report)!);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WithoutAutoPlay_TheBootStartsIt_AndItMovesUntilTheNameIsIn()
        {
            BrandIntro.FrameVisibleOverrideForTests = () => true;
            BootSplash splash = NewSplash();
            splash.IntroAutoPlay = false;
            BrandIntro intro = splash.Intro!;
            Assert.That(splash.TryPlayIntro(), Is.False, "antes de saber si hay Reducir movimiento, no");
            splash.BeginIntro(false);
            splash.Tick(16f);
            splash.Tick(16f);
            splash.RefreshIntro();
            Assert.That(intro.Playing, Is.False, "sin AutoPlay, el arranque elige el momento");
            Assert.That(splash.IntroMoving, Is.False);
            Assert.That(splash.TryPlayIntro(), Is.True);
            Assert.That(splash.Wordmark, Is.Not.Null, "el nombre, hecho antes del vertido (su coste no cae en mitad)");
            Assert.That(splash.IntroMoving, Is.True);
            Assert.That(splash.TryPlayIntro(), Is.False, "una sola vez");
            double until = Time.realtimeSinceStartupAsDouble + ((BrandIntro.SettledMs + 1000) / 1000.0);
            while (splash.IntroMoving && Time.realtimeSinceStartupAsDouble < until)
            {
                yield return null;
                splash.Tick(16f);
            }

            Assert.That(splash.IntroMoving, Is.False);
            Assert.That(intro.ClockMs, Is.GreaterThanOrEqualTo((float)BrandIntro.SettledMs), "se mueve hasta que el nombre entró entero");
            Assert.That(intro.WordmarkAlpha, Is.EqualTo(1f));
            Assert.That(intro.Done, Is.False, "y aún sostiene el emblema hasta anim_ms");
            BrandIntroReport report = splash.EndIntro()!;
            Assert.That(report.MotionFrames, Is.GreaterThan(10));
        }

        [UnityTest]
        public IEnumerator WithReduceMotion_ThereIsNoMotionToPlay()
        {
            BrandIntro.FrameVisibleOverrideForTests = () => true;
            BootSplash splash = NewSplash();
            splash.IntroAutoPlay = false;
            splash.BeginIntro(true);
            splash.Tick(16f);
            Assert.That(splash.TryPlayIntro(), Is.False);
            Assert.That(splash.IntroMoving, Is.False, "quieta: la carga no espera a ningún movimiento");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheMotion_DoesNotAllocatePerFrame()
        {
            BrandIntro.FrameVisibleOverrideForTests = () => true;
            BootSplash splash = NewSplash();
            splash.BeginIntro(false);
            splash.PrepareWordmark();
            yield return null;
            splash.Tick(16f);
            splash.Tick(16f);
            Assert.That(splash.Intro!.Playing, Is.True);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 200; i++)
            {
                splash.Tick(16f); // el reloj real, el registro del tiempo entre fotogramas y el tinte
            }

            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.EqualTo(0L), "sin asignaciones por fotograma (08 §5.4)");
        }

        // Revisión de 6336136c: el primer Tick va en el mismo fotograma en que se crea la intro (Start y Update del mismo fotograma),
        // que aún no está enviado a la GPU; una lectura pedida entonces se encolaría antes de sus dibujos. Se pide en el segundo.
        [UnityTest]
        public IEnumerator TheGpuRead_IsRequestedOnTheSecondTick_WhenTheFirstFrameIsSent([Values(true, false)] bool withOverride)
        {
            if (withOverride)
            {
                BrandIntro.FrameVisibleOverrideForTests = () => true;
            }

            BootSplash splash = NewSplash();
            BrandIntro intro = splash.Intro!;
            splash.BeginIntro(false);
            splash.Tick(16f);
            Assert.That(intro.FenceRequestedAtTick, Is.EqualTo(-1), "en el primer fotograma, aún no");
            Assert.That(intro.OnScreen, Is.False, "el primer fotograma aún no se ha enviado: no puede darse por visto");
            Assert.That(intro.Playing, Is.False);
            yield return null;
            splash.Tick(16f);
            Assert.That(intro.FenceRequestedAtTick, Is.EqualTo(BrandIntro.FenceTick));
            Assert.That(BrandIntro.FenceTick, Is.EqualTo(2));
            if (withOverride)
            {
                Assert.That(intro.OnScreen, Is.True);
                Assert.That(intro.Playing, Is.True, "visto: empieza en ese mismo fotograma");
            }
        }

        [Test]
        public void AGpuReadWithAnError_DoesNotCountAsSeen()
        {
            Assert.That(BrandIntro.FenceResult(false, false), Is.EqualTo(BrandIntroVisibleBy.None), "aún sin respuesta");
            Assert.That(BrandIntro.FenceResult(true, false), Is.EqualTo(BrandIntroVisibleBy.Gpu), "terminó: el fotograma ya se pintó");
            Assert.That(BrandIntro.FenceResult(true, true), Is.EqualTo(BrandIntroVisibleBy.NoReadback), "con error no prueba nada");
            Assert.That(DevProbe.VisibleById(BrandIntroVisibleBy.NoReadback), Is.EqualTo("no_readback"));
        }

        [UnityTest]
        public IEnumerator APauseWhileMoving_IsCountedApart_NotAsASlowFrame()
        {
            BrandIntro.FrameVisibleOverrideForTests = () => true;
            BootSplash splash = NewSplash();
            splash.BeginIntro(false);
            splash.PrepareWordmark();
            splash.Tick(16f);
            splash.Tick(16f);
            Assert.That(splash.Intro!.Playing, Is.True);
            yield return null;
            splash.Tick(16f);
            BusyWait((BrandIntro.MotionPauseMs + 100) / 1000.0); // segundo plano a mitad del vertido
            splash.Tick(16f);
            BrandIntroReport report = splash.EndIntro()!;
            Assert.That(report.MotionFrames, Is.EqualTo(2));
            Assert.That(report.MotionPauses, Is.EqualTo(1), "motion_pauses: el hueco largo, aparte");
            Assert.That(report.FrameMaxMs, Is.LessThan((float)BrandIntro.MotionPauseMs), "frame_max_ms sin la pausa");
            Assert.That(report.FrameP95Ms, Is.LessThan((float)BrandIntro.MotionPauseMs));
            StringAssert.Contains("\"motion_pauses\":1,", new DevProbe().FormatBrandIntro(report)!);
        }

        // Arranque completo: la GPU nunca confirma el primer fotograma. FinishSplash arranca la intro por el tope de 1,2 s y se ve
        // entera igual.
        [UnityTest]
        public IEnumerator ColdStart_TheGpuNeverConfirms_TheIntroStartsAtItsCap_AndIsSeenWhole()
        {
            BootSplash.EnabledOverride = true;
            MapArtRule.ForceProvisional = false;
            ArtSlots.ForceProcedural = true;
            BrandIntro.FrameVisibleOverrideForTests = () => false;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;
            BrandIntroReport intro = boot.IntroInfo!;
            Assert.That(intro.VisibleBy, Is.EqualTo(BrandIntroVisibleBy.Cap), "visible_by: cap");
            Assert.That(intro.VisibleMs - intro.FirstFrameMs, Is.GreaterThanOrEqualTo((long)BrandIntro.VisibleWaitCapMs - 20));
            Assert.That(intro.PlayMs, Is.GreaterThanOrEqualTo(intro.VisibleMs));
            Assert.That(intro.ShownMs, Is.EqualTo((long)BrandIntro.AnimMs), "shown_ms 3.500");
            Assert.That(intro.Cut, Is.False);
            Assert.That(intro.WaitCapped, Is.False, "el tope de la intro no llega: la espera a la GPU cabe en el de la carga");
            Assert.That(boot.Splash, Is.Null);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "primera apertura: el nivel 1");
        }

        // Arranque completo: los fotogramas se paran a mitad del movimiento (reloj de la intro congelado). IntroGap no espera para
        // siempre: a su tope sigue la carga; luego la intro vuelve a correr y se ve entera.
        [UnityTest]
        public IEnumerator ColdStart_IntroGap_HasACap_WhenTheFramesStop()
        {
            BootSplash.EnabledOverride = true;
            MapArtRule.ForceProvisional = false;
            ArtSlots.ForceProcedural = true;
            BrandIntro.FrameVisibleOverrideForTests = () => true;
            const int capMs = 600;
            GameBootstrap.IntroMotionCapOverrideMsForTests = capMs;
            int frozenAt = -1;
            GameBootstrap.IntroMotionHookForTests = b =>
            {
                frozenAt = b.IntroYields;
                BrandIntro.ClockFrozenForTests = true; // los fotogramas, parados: la intro no avanza
            };
            GameBootstrap.IntroFrameHookForTests = (b, at) =>
            {
                if (BrandIntro.ClockFrozenForTests)
                {
                    BrandIntro.ClockFrozenForTests = false; // el siguiente hueco: ya pasó el tope y la carga siguió
                }
            };
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(frozenAt, Is.GreaterThanOrEqualTo(1), "el movimiento empezó en un hueco");
            Assert.That(boot.IntroMotionHoldMs, Is.InRange((long)capMs - 20, (long)(capMs + PerfBudget.Ms(400d))), "a su tope, la carga sigue");
            Assert.That(boot.IntroMotionHoldMs, Is.LessThan((long)BrandIntro.SettledMs), "sin esperar el movimiento entero");
            for (int i = 0; i < GameBootstrap.BootStepCount; i++)
            {
                Assert.That(boot.BootStepMs(i), Is.GreaterThanOrEqualTo(0L), "bloque " + i + ": la carga llegó entera");
            }

            BrandIntroReport intro = boot.IntroInfo!;
            Assert.That(intro.Cut, Is.False);
            Assert.That(intro.ShownMs, Is.EqualTo((long)BrandIntro.AnimMs));
            Assert.That(intro.WaitCapped, Is.False);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(GameBootstrap.IntroMotionCapMs, Is.EqualTo(BrandIntro.SettledMs + 2000));
        }

        // Arranque completo: Inicio y volver mientras la carga espera al movimiento (dentro de IntroGap). OnApplicationPause(false)
        // no reanuda nada, la carga sigue después, no se pierde ningún paso y la intro no se corta.
        [UnityTest]
        public IEnumerator ColdStart_PauseAndResume_WhileTheIntroMoves_TheBootEndsTheSame([Values(true, false)] bool firstOpen)
        {
            BootSplash.EnabledOverride = true;
            MapArtRule.ForceProvisional = false;
            ArtSlots.ForceProcedural = true;
            int hits = 0;
            bool inProgress = false;
            GameBootstrap.IntroMotionHookForTests = b =>
            {
                hits++;
                inProgress = b.BootInProgress;
                b.SendMessage("OnApplicationPause", true);
                b.SendMessage("OnApplicationPause", false);
            };
            yield return PlayModeHelpers.LoadApp(firstOpen ? new MemoryProfileStore() : new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(hits, Is.EqualTo(1), "un solo movimiento, en un hueco");
            Assert.That(inProgress, Is.True, "durante el movimiento, el arranque está a medias");
            Assert.That(boot.BootInProgress, Is.False, "y después termina");
            Assert.That(boot.RestoreLedgerBeforeConsent, Is.EqualTo(0), "restoreLedger nunca antes de SetUpConsent");
            for (int i = 0; i < GameBootstrap.BootStepCount; i++)
            {
                Assert.That(boot.BootStepMs(i), Is.GreaterThanOrEqualTo(0L), "bloque " + i + ": no se perdió");
            }

            BrandIntroReport intro = boot.IntroInfo!;
            Assert.That(intro.Cut, Is.False, "cut false");
            Assert.That(intro.ShownMs, Is.EqualTo((long)BrandIntro.AnimMs));
            Assert.That(intro.WaitCapped, Is.False);
            Assert.That(boot.App!.Screen, Is.EqualTo(firstOpen ? AppScreen.Level : AppScreen.Lobby));
        }

        [Test]
        public void TheAcai_AdvancesWithoutJumps()
        {
            Assert.That(BrandIntro.AcaiAlpha(2.5f, 1, 0.5f, 0.5f), Is.EqualTo(1f), "detrás del frente, entero");
            Assert.That(BrandIntro.AcaiAlpha(2.5f, 2, 0.5f, 0.5f), Is.EqualTo(0.5f).Within(0.001f), "el tramo del frente, a medias");
            Assert.That(BrandIntro.AcaiAlpha(2.5f, 3, 0.5f, 0.5f), Is.EqualTo(0f), "delante, nada");
            Assert.That(BrandIntro.AcaiAlpha(0f, 0, 0f, 0f), Is.EqualTo(0f), "antes del impacto, nada");
            Assert.That(BrandIntro.AcaiAlpha(9.2f, 9, 0.99f, 1f), Is.EqualTo(1f), "al final, todo");

            // Con el frente avanzando a pasos de 0,02 tramos (más fino que un fotograma a 60 fps), ningún tramo salta de golpe.
            int count = BrandRiverScene.ChunkCount;
            float worst = 0f;
            for (int k = 0; k < count; k++)
            {
                float previous = 0f;
                for (float front = 0f; front <= count; front += 0.02f)
                {
                    float a = BrandIntro.AcaiAlpha(front, k, 0.5f, 0.5f);
                    worst = Mathf.Max(worst, Mathf.Abs(a - previous));
                    previous = a;
                }
            }

            Assert.That(worst, Is.LessThan(0.04f), "sin saltos: antes cada tramo se encendía entero de golpe");
        }

        private static void BusyWait(double seconds)
        {
            double until = Time.realtimeSinceStartupAsDouble + seconds;
            while (Time.realtimeSinceStartupAsDouble < until)
            {
            }
        }

        // Inicio y volver con el arranque a medias, en cada hueco de la carga que deja pintar la intro: OnApplicationPause(false) no
        // reanuda nada (ni la nube, ni la tienda, ni restoreLedger antes de SetUpConsent: 08 §2.3 y §10.3), y el arranque acaba igual.
        [UnityTest]
        public IEnumerator PauseAndResume_InEveryLoadGap_TheBootEndsTheSame([Values(0, 1, 4, 5)] int step, [Values(true, false)] bool firstOpen)
        {
            BootSplash.EnabledOverride = true;
            MapArtRule.ForceProvisional = false;
            ArtSlots.ForceProcedural = true;
            int hits = 0;
            bool inProgressAtGap = false;
            GameBootstrap.IntroFrameHookForTests = (b, at) =>
            {
                if (at != step)
                {
                    return;
                }

                hits++;
                inProgressAtGap = b.BootInProgress;
                b.SendMessage("OnApplicationPause", true);
                b.SendMessage("OnApplicationPause", false);
            };
            try
            {
                yield return PlayModeHelpers.LoadApp(firstOpen ? new MemoryProfileStore() : new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            }
            finally
            {
                GameBootstrap.IntroFrameHookForTests = null;
            }

            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(hits, Is.EqualTo(1), "el hueco " + step + " llegó una vez");
            Assert.That(inProgressAtGap, Is.True, "en el hueco, el arranque está a medias");
            Assert.That(boot.BootInProgress, Is.False, "y después termina");
            Assert.That(boot.RestoreLedgerBeforeConsent, Is.EqualTo(0), "restoreLedger nunca antes de SetUpConsent");
            BootReport report = boot.BootInfo!;
            Assert.That(report, Is.Not.Null);
            Assert.That(report.FirstOpen, Is.EqualTo(firstOpen));
            Assert.That(report.SlothPose, Is.Null, "sin TiTino (forzado)");
            BrandIntroReport intro = boot.IntroInfo!;
            Assert.That(intro.Cut, Is.False, "la pausa no estropea la intro");
            Assert.That(intro.WaitCapped, Is.False);
            Assert.That(intro.ShownMs, Is.EqualTo((long)BrandIntro.AnimMs));
            Assert.That(boot.Splash, Is.Null);
            Assert.That(boot.App!.Screen, Is.EqualTo(firstOpen ? AppScreen.Level : AppScreen.Lobby), "el nivel 1 o el inicio, como siempre");
        }

        [Test]
        public void TheIntroWait_HasASafetyCap()
        {
            Assert.That(GameBootstrap.IntroWaitCapMs, Is.EqualTo(BrandIntro.AnimMs + 2000));
            var report = new BrandIntroReport { WaitCapped = true };
            StringAssert.Contains("\"wait_capped\":true", new DevProbe().FormatBrandIntro(report)!);
        }

        [UnityTest]
        public IEnumerator BackFromBackground_TheIntroIsNotRepeated()
        {
            BootSplash.EnabledOverride = true;
            MapArtRule.ForceProvisional = false;
            ArtSlots.ForceProcedural = true;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            BrandIntroReport first = boot.IntroInfo!;
            BootReport report = boot.BootInfo!;

            // La app no se cerró: a segundo plano y de vuelta (Android lo hace sin volver a cargar la escena).
            boot.SendMessage("OnApplicationPause", true);
            yield return null;
            boot.SendMessage("OnApplicationPause", false);
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            Assert.That(boot.Splash, Is.Null, "ni pantalla de arranque ni intro");
            Assert.That(boot.IntroInfo, Is.SameAs(first), "la intro no se repite");
            Assert.That(boot.BootInfo, Is.SameAs(report));
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "sigue donde estaba");
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
