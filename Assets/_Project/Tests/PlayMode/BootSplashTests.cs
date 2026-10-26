#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.Localization;
using AF.Platform;
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
    /// Pantalla de arranque (tarea TI6d; 06 §2.1, regla 3, y §2.2; D-016, T2-A; propuesta de TiTino d.3 y paso 6d; 09 §6):
    /// <list type="bullet">
    /// <item>la regla de la espera (<see cref="BootSplashTiming"/>): solo en la primera apertura con <c>sloth_wake</c>, lo que
    /// falte para 1,0 s, y solo si el primer toque sigue cabiendo en 20 s;</item>
    /// <item>la pantalla no lleva texto (ni un componente de texto) y solo su fondo recibe toques; sin arte de TiTino (ni el
    /// dibujo por código, que sale en todos los builds por D-052, ni sus PNG), sin TiTino;</item>
    /// <item>en la escena <c>Game</c>: la primera apertura enseña <c>sloth_wake</c> al menos 1,0 s con la app aún en el arranque
    /// (la intro del nivel 1 no corre detrás) y después entra en el nivel 1; las demás aperturas, <c>sloth_idle</c>, sin espera
    /// añadida; sin el arte, ni TiTino ni espera; la reconstrucción por idioma no la vuelve a enseñar;</item>
    /// <item>la sonda <c>AF_STATE boot</c> con <c>first_open</c>, <c>splash_ms</c> y <c>first_touch_ms</c>, que se rellena con
    /// el primer toque en el nivel 1; si el segundo de <c>sloth_wake</c> no cabe en los 20 s, se corta sin esperar y la sonda lo
    /// anota (<c>"wake_fits":false</c>); con <c>ui_sloth_enabled</c> apagado, ni TiTino ni espera;</item>
    /// <item>la composición con la marca v2 (añadido de D-051): TiTino cuelga de la rama del inicio (D-050), volteada desde la
    /// derecha, en el cielo: nunca tapa la selva del horizonte, sus palmeras, la cuia ni el chorro (16:9 y 19,5:9); el nombre del logo, rasterizado a la escala de la pantalla, va bajo el río y
    /// no se mueve sin TiTino.</item>
    /// </list>
    /// </summary>
    public sealed class BootSplashTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc);

        private GameObject? _host;
        private ProceduralArt? _art;
        private SplashWatcher? _watcher;

        [TearDown]
        public void TearDown()
        {
            BootSplash.EnabledOverride = null;
            BrandIntro.EnabledOverride = null;
            BootSplash.SlothEnabledOverride = null;
            BootSplash.TestStartupOffsetMs = 0;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            ArtSlots.ForceProcedural = false;
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
            _art?.Dispose();
            _art = null;
            if (_host != null)
            {
                UnityEngine.Object.Destroy(_host);
            }

            if (_watcher != null)
            {
                UnityEngine.Object.Destroy(_watcher.gameObject);
            }

            _host = null;
            _watcher = null;
        }

        [Test]
        public void Timing_OnlyTheFirstOpenWithWake_Holds_WhatIsMissingForOneSecond()
        {
            Assert.That(BootSplashTiming.WakeMinVisibleMs, Is.EqualTo(1000), "06 §2.2: al menos 1,0 s [P]");
            Assert.That(BootSplashTiming.FirstTouchTargetMs, Is.EqualTo(20000), "09 §6: primer toque < 20 s");
            Assert.That(BootSplashTiming.HoldMs(true, true, 2000, 300), Is.EqualTo(700), "primera apertura: lo que falta del segundo");
            Assert.That(BootSplashTiming.HoldMs(true, true, 2000, 0), Is.EqualTo(1000));
            Assert.That(BootSplashTiming.HoldMs(true, true, 3000, 1000), Is.EqualTo(0), "ya lleva su segundo: se corta");
            Assert.That(BootSplashTiming.HoldMs(true, true, 3000, 2400), Is.EqualTo(0));
            Assert.That(BootSplashTiming.HoldMs(false, true, 2000, 0), Is.EqualTo(0), "las demás aperturas no añaden espera");
            Assert.That(BootSplashTiming.HoldMs(false, false, 2000, 0), Is.EqualTo(0));
            Assert.That(BootSplashTiming.HoldMs(true, false, 2000, 0), Is.EqualTo(0), "sin TiTino a la vista, sin espera");
        }

        [Test]
        public void Timing_TheSecondOnlyIfTheFirstTouchStillFitsInTwentySeconds()
        {
            // 20 000 − 5 000 de reserva (intro del nivel 1 y el jugador) = 15 000: hasta ahí, con la espera incluida.
            Assert.That(BootSplashTiming.Fits(14000, 1000), Is.True, "justo en el límite");
            Assert.That(BootSplashTiming.Fits(14001, 1000), Is.False);
            Assert.That(BootSplashTiming.HoldMs(true, true, 14000, 0), Is.EqualTo(1000));
            Assert.That(BootSplashTiming.HoldMs(true, true, 14500, 0), Is.EqualTo(0), "no cabe: se corta (y se anota)");
            Assert.That(BootSplashTiming.HoldMs(true, true, 14500, 600), Is.EqualTo(400), "lo que falta sí cabe");
            Assert.That(BootSplashTiming.NeedMs(true, true, 300), Is.EqualTo(700), "lo que faltaba aunque no quepa");
            Assert.That(BootSplashTiming.NeedMs(true, true, -50), Is.EqualTo(1000));
        }

        [UnityTest]
        public IEnumerator Splash_HasNoText_AndOnlyItsBackgroundTakesTouches()
        {
            MapArtRule.ForceProvisional = true;
            ArtSlots.ForceProcedural = true;
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp
            BootSplash splash = NewSplash();
            Assert.That(splash.ShowSloth(_art!.SlothRigArt(), _art.HomeBranch, true, false), Is.True);
            yield return null;

            Assert.That(splash.Root.GetComponentsInChildren<TMP_Text>(true), Is.Empty, "06 §2.1, regla 3: sin texto");
            Assert.That(splash.Root.GetComponentsInChildren<Text>(true), Is.Empty, "06 §2.1, regla 3: sin texto");
            int raycast = 0;
            foreach (Graphic g in splash.Root.GetComponentsInChildren<Graphic>(true))
            {
                raycast += g.raycastTarget ? 1 : 0;
            }

            Assert.That(raycast, Is.EqualTo(1), "solo el fondo traga los toques; TiTino nunca bloquea nada");
            Assert.That(splash.Canvas.sortingOrder, Is.EqualTo(BootSplash.SortingOrder));
            Assert.That(splash.Visible, Is.True);
            Assert.That(splash.SlothVisible, Is.True);
            Assert.That(splash.Sloth!.Pose, Is.EqualTo(SlothPoseId.Wake), "primera apertura: sloth_wake");
            Assert.That(splash.Sloth.Size, Is.InRange(140f, 180f), "propuesta de TiTino c.4: 140-180 dp en el arranque");

            // Cuelga de la rama del inicio (D-050), volteada desde el borde derecho, sobre el açaí y lejos de la cuia; la rama gris
            // de su rig no sale.
            Vector2 anchor = splash.BranchAnchor;
            Assert.That(splash.Branch, Is.Not.Null, "la rama del inicio, con sus hojas");
            Assert.That(splash.Branch!.sprite, Is.SameAs(_art.HomeBranch));
            Assert.That(splash.Branch.rectTransform.anchoredPosition.x, Is.EqualTo(360f), "desde el borde derecho");
            Assert.That(splash.Branch.rectTransform.localScale.x, Is.EqualTo(-1f), "volteada");
            Assert.That(splash.Sloth.BranchImage.enabled, Is.False, "la rama del rig no se pinta");
            Assert.That(anchor.x, Is.GreaterThan(180f), "TiTino, a la derecha (la cuia va a la izquierda)");
            Assert.That(splash.Branch.transform.GetSiblingIndex(), Is.LessThan(splash.Sloth.Root.GetSiblingIndex()), "la rama, detrás de TiTino");

            splash.Tick(400f);
            Assert.That(splash.SlothClockMs, Is.EqualTo(400f).Within(0.01f));
            splash.Dispose();
            Assert.That(splash.Visible, Is.False);
            yield return null;
            Assert.That(splash.Canvas == null, Is.True, "el lienzo se destruye al cortar");
        }

        [UnityTest]
        public IEnumerator Splash_LaterOpens_Idle_InRelease_TheCodeSloth_AndWithoutAnyArt_NoSloth()
        {
            MapArtRule.ForceProvisional = true;
            ArtSlots.ForceProcedural = true;
            BootSplash idle = NewSplash();
            Assert.That(idle.ShowSloth(_art!.SlothRigArt(), _art.HomeBranch, false, false), Is.True);
            Assert.That(idle.Sloth!.Pose, Is.EqualTo(SlothPoseId.Idle), "las demás aperturas: sloth_idle");
            idle.Dispose();

            // La regla real (D-052): en release, sin PNG, TiTino por código sale igual.
            MapArtRule.ForceProvisional = null;
            Assert.That(MapArtRule.ShowProvisionalIn(false, false), Is.True, "release");
            BootSplash release = NewSplash();
            Assert.That(release.ShowSloth(_art.SlothRigArt(), _art.HomeBranch, true, false), Is.True, "release sin PNG: TiTino por código");
            Assert.That(release.SlothVisible, Is.True);
            release.Dispose();

            MapArtRule.ForceProvisional = false; // forzado: hoy el arte por código sale en todos los builds, release incluido (D-052)
            BootSplash none = NewSplash();
            Assert.That(_art.SlothRigArt(), Is.Null, "sin arte por código ni PNG");
            Assert.That(none.ShowSloth(_art.SlothRigArt(), _art.HomeBranch, true, false), Is.False, "MapArtRule: sin arte, sin TiTino");
            Assert.That(none.SlothVisible, Is.False);
            Assert.That(none.SlothClockMs, Is.EqualTo(0f));
            Assert.That(none.Sloth, Is.Null, "no se crea ningún objeto del rig");
            none.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Composition_TheSlothNeverRidesTheJungle([Values(780, 640)] int screen)
        {
            MapArtRule.ForceProvisional = true;
            ArtSlots.ForceProcedural = true;
            GameScreen.SimulatedScreen = screen == 640 ? HudLabelTests.Screen640 : HudLabelTests.Screen780; // 16:9 y 19,5:9
            foreach (bool firstOpen in new[] { true, false })
            {
                BootSplash splash = NewSplash();
                Assert.That(splash.ShowSloth(_art!.SlothRigArt(), _art.HomeBranch, firstOpen, false), Is.True);
                Assert.That(splash.ComposedForSloth, Is.True);
                Assert.That(splash.ShortScreen, Is.EqualTo(screen == 640));
                Assert.That(splash.Sloth!.Size, Is.InRange(140f, BootSplash.SlothSizeDp), "propuesta de TiTino c.4: 140-180 dp en el arranque");
                Assert.That(splash.Sloth.Size, Is.EqualTo(splash.SlothFitDp).Within(0.01f));

                // Todo el gesto (sloth_wake y su reposo, o sloth_idle con un giro de cabeza): el fondo de TiTino, por encima de la
                // horizonte con aire (nunca tapa la selva del horizonte, la cuia ni el chorro), y la rama, dentro de la pantalla.
                float canopy = splash.CanopyDp;
                Assert.That(canopy, Is.LessThanOrEqualTo(splash.CuiaRect.yMin));
                Assert.That(canopy, Is.LessThanOrEqualTo(splash.StreamRect.yMin));
                var rects = new List<Rect>();
                float lowest = float.MinValue;
                for (float ms = 0f; ms <= 12000f; ms += 100f)
                {
                    splash.Seek(ms);
                    rects.Clear();
                    splash.Sloth.CollectRects(splash.Root, rects);
                    Assert.That(rects, Is.Not.Empty);
                    foreach (Rect r in rects)
                    {
                        lowest = Mathf.Max(lowest, r.yMax);
                    }
                }

                Assert.That(lowest, Is.LessThanOrEqualTo(canopy - 12f), "TiTino cuelga en el cielo, sin tapar el río ni la cuia (" + screen + ", " + (firstOpen ? "wake" : "idle") + ")");

                // Ni las palmeras del horizonte: en todo el gesto, ninguna pieza de TiTino toca sus cajas.
                Rect[] palms = splash.PalmRects;
                for (float ms = 0f; ms <= 12000f; ms += 250f)
                {
                    splash.Seek(ms);
                    rects.Clear();
                    splash.Sloth.CollectRects(splash.Root, rects);
                    foreach (Rect r in rects)
                    {
                        foreach (Rect palm in palms)
                        {
                            Assert.That(r.Overlaps(palm), Is.False, "TiTino no tapa las palmeras (" + screen + ", " + ms + " ms)");
                        }
                    }
                }
                Assert.That(-splash.Branch!.rectTransform.anchoredPosition.y, Is.GreaterThanOrEqualTo(0f), "la rama entera en la pantalla");
                Assert.That(splash.BranchAnchor.y, Is.GreaterThanOrEqualTo(32f + 36f - 0.5f), "la rama, bajo la zona segura");
                splash.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Composition_TheBrandWorld_TheWordmarkUnderTheRiver_SameWithoutSloth([Values(780, 640)] int screen)
        {
            MapArtRule.ForceProvisional = true;
            ArtSlots.ForceProcedural = true;
            GameScreen.SimulatedScreen = screen == 640 ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            float h = screen == 640 ? 640f : 780f;
            BootSplash splash = NewSplash();
            Assert.That(splash.ShowSloth(_art!.SlothRigArt(), _art.HomeBranch, true, false), Is.True);

            // Sin intro, la pantalla nace entera: el mundo hecho y a la vista, con el nombre del logo bajo el río.
            Assert.That(splash.SceneBuilt, Is.True);
            Assert.That(splash.Reveal, Is.EqualTo(1f));
            Image wordmark = splash.Wordmark!;
            Assert.That(wordmark, Is.Not.Null);
            Assert.That(wordmark.sprite, Is.Not.Null, "el nombre, rasterizado por código (BrandWordmark)");
            Rect box = splash.WordmarkBox;
            Assert.That(box.width, Is.EqualTo(screen == 640 ? BootSplash.WordmarkMaxShortDp : BootSplash.WordmarkMaxDp).Within(0.01f));
            Assert.That(box.height, Is.EqualTo(box.width * BrandWordmark.Aspect).Within(0.01f));
            Assert.That(box.center.x, Is.EqualTo(180f).Within(0.01f), "centrado");
            Assert.That(box.yMin, Is.InRange(splash.ImpactBottomDp, splash.ImpactBottomDp + 20f), "justo bajo la cuia y los aros");
            Assert.That(splash.ImpactPoint.y, Is.GreaterThan(splash.HorizonDp), "la cuia vierte en el río, bajo el horizonte");
            Assert.That(box.yMax, Is.LessThan(h - 120f), "sitio para el follaje de las esquinas");
            Assert.That(wordmark.sprite.texture.width, Is.GreaterThanOrEqualTo(Mathf.RoundToInt(box.width * 3f)), "a la escala de la pantalla (3 px por dp): nítido");
            Assert.That(splash.CanopyDp, Is.LessThan(splash.CuiaRect.yMax));
            Assert.That(splash.CuiaRect.xMax, Is.LessThan(splash.BranchAnchor.x), "la cuia a la izquierda, TiTino a la derecha");

            // Sin TiTino desde el principio (BootSplash.SlothExpected = false), o porque no hay su arte: lo mismo, sin TiTino ni rama.
            var alone = new BootSplash(_host!.transform, withSloth: false);
            Assert.That(alone.ComposedForSloth, Is.False);
            Assert.That(alone.Branch, Is.Null);
            Assert.That(alone.WordmarkBox, Is.EqualTo(box), "el nombre no se mueve sin TiTino");
            alone.Dispose();
            splash.Dispose();

            MapArtRule.ForceProvisional = false; // sin arte de TiTino (forzado: hoy sale en todos los builds, D-052)
            BootSplash none = NewSplash();
            Assert.That(none.ComposedForSloth, Is.True, "esperando a TiTino hasta saber si sale");
            Assert.That(none.ShowSloth(_art.SlothRigArt(), _art.HomeBranch, true, false), Is.False);
            Assert.That(none.ComposedForSloth, Is.False);
            Assert.That(none.Branch, Is.Null, "sin TiTino, sin rama");
            Assert.That(none.WordmarkBox, Is.EqualTo(box));
            none.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PrepareWorld_TheNextTickDoesNotAdvance_ThenItRunsNormally()
        {
            // El tiempo que cuesta hacer el mundo llega en el Tick siguiente: no se cuenta (ni para el gesto ni para el fundido).
            MapArtRule.ForceProvisional = true;
            ArtSlots.ForceProcedural = true;
            BootSplash splash = NewIntroSplash();
            splash.BeginIntro(false);
            Assert.That(splash.ShowSloth(_art!.SlothRigArt(), _art.HomeBranch, true, false), Is.True);
            splash.EndIntro();
            splash.PrepareWorld();
            Assert.That(splash.SceneBuilt, Is.True);
            splash.Tick(100f);
            Assert.That(splash.SlothClockMs, Is.EqualTo(0f), "el Tick tras PrepareWorld no avanza el gesto");
            Assert.That(splash.Reveal, Is.EqualTo(0f), "ni el fundido");
            splash.Tick(100f);
            Assert.That(splash.SlothClockMs, Is.EqualTo(100f).Within(0.01f), "el siguiente, normal");
            Assert.That(splash.Reveal, Is.GreaterThan(0f));
            splash.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Dispose_DestroysEveryTextureOfTheSplashAndTheIntro()
        {
            MapArtRule.ForceProvisional = true;
            ArtSlots.ForceProcedural = true;
            BootSplash splash = NewIntroSplash();
            splash.BeginIntro(false);
            Assert.That(splash.ShowSloth(_art!.SlothRigArt(), _art.HomeBranch, true, false), Is.True);
            splash.EndIntro();
            splash.PrepareWorld();
            splash.Tick(16f);
            var owned = OwnTextures(splash);
            Assert.That(owned.Count, Is.GreaterThanOrEqualTo(BrandRiverScene.ChunkCount + 6), "la escena, el nombre, el primer plano y el halo");
            splash.Dispose();
            yield return null;
            foreach (Texture t in owned)
            {
                Assert.That(t == null, Is.True, "destruida: " + (t != null ? t.name : string.Empty));
            }
        }

        [UnityTest]
        public IEnumerator ScreenChange_RebuildsTheScene_SharedWithTheIntro_AndDestroysTheOld()
        {
            MapArtRule.ForceProvisional = true;
            ArtSlots.ForceProcedural = true;
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            BootSplash splash = NewIntroSplash();
            splash.BeginIntro(false);
            yield return null;
            var before = OwnTextures(splash);
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            splash.Tick(16f);
            foreach (string piece in ScenePieces())
            {
                Image mine = splash.Root.Find(piece).GetComponent<Image>();
                Image intro = splash.Intro!.Root.Find(piece).GetComponent<Image>();
                Assert.That(mine.sprite, Is.Not.Null, piece);
                Assert.That(intro.sprite, Is.SameAs(mine.sprite), piece + ": la intro y la pantalla, la misma pieza nueva");
            }

            yield return null;
            int destroyed = 0;
            foreach (Texture t in before)
            {
                if (t == null)
                {
                    destroyed++;
                }
            }

            Assert.That(destroyed, Is.GreaterThanOrEqualTo(BrandRiverScene.ChunkCount + 5), "las piezas viejas de la escena, destruidas");
            splash.Dispose();
        }

        // Las piezas de la escena que comparten la pantalla y la intro.
        private static List<string> ScenePieces()
        {
            var pieces = new List<string> { "EmblemBase", "EmblemIsland", "EmblemImpact", "EmblemStream", "EmblemCuia" };
            for (int k = 0; k < BrandRiverScene.ChunkCount; k++)
            {
                pieces.Add("EmblemAcai" + k);
            }

            return pieces;
        }

        // Las texturas que son de la pantalla (y de la intro): todas las de sus imágenes menos las de TiTino y su rama, que son de
        // ProceduralArt.
        private static List<Texture> OwnTextures(BootSplash splash)
        {
            var result = new List<Texture>();
            foreach (Image img in splash.Root.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite == null || img == splash.Branch || (splash.Sloth != null && img.transform.IsChildOf(splash.Sloth.Root)))
                {
                    continue;
                }

                if (!result.Contains(img.sprite.texture))
                {
                    result.Add(img.sprite.texture);
                }
            }

            return result;
        }

        private BootSplash NewIntroSplash()
        {
            if (_host == null)
            {
                _host = new GameObject("BootSplashTestHost");
            }

            if (_art == null)
            {
                _art = new ProceduralArt("white");
            }

            return new BootSplash(_host.transform, withSloth: true, withIntro: true);
        }

        [UnityTest]
        public IEnumerator FirstOpen_ShowsWakeAtLeastOneSecond_WhileTheAppWaits_ThenLevelOne()
        {
            BootSplash.EnabledOverride = true;
            MapArtRule.ForceProvisional = true;
            Watch();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore()); // sin perfil: primera apertura
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(_watcher!.SawSplash, Is.True, "la pantalla de arranque sale en el arranque en frío");
            Assert.That(_watcher.SawSplashBeforeLoad, Is.True, "el río y el logotipo salen antes de que exista la app");
            Assert.That(_watcher.SawWake, Is.True, "primera apertura: sloth_wake");
            Assert.That(_watcher.AppStartedUnderSplash, Is.False, "la app arranca al cortarse la pantalla: la intro no corre detrás");
            Assert.That(_watcher.FramesAfterLoad, Is.GreaterThanOrEqualTo(1), "primera apertura: la pantalla sigue tras la carga, por el segundo de sloth_wake");
            Assert.That(boot.Splash, Is.Null, "cortada");

            BootReport report = boot.BootInfo!;
            Assert.That(report.FirstOpen, Is.True);
            Assert.That(report.SlothPose, Is.EqualTo("sloth_wake"));
            Assert.That(report.SlothMs, Is.GreaterThanOrEqualTo(BootSplashTiming.WakeMinVisibleMs), "sloth_wake a la vista ≥ 1,0 s");
            Assert.That(report.WakeFits, Is.True);
            Assert.That(report.SplashMs, Is.GreaterThanOrEqualTo(report.LoadMs + report.HoldMs - 1));
            Assert.That(report.FirstTouchMs, Is.EqualTo(-1L), "aún no ha tocado");

            // Añadido de D-051 del 2026-10-26: la intro entera (unos 2,5 s) y, después, el segundo de sloth_wake; el primer toque en
            // el nivel 1 sigue muy por debajo de los 20 s (06 §2.2, 09 §6).
            BrandIntroReport intro = boot.IntroInfo!;
            Assert.That(intro.IntroMs, Is.GreaterThanOrEqualTo((long)BrandIntro.AnimMs));
            Assert.That(report.SplashMs, Is.GreaterThanOrEqualTo(intro.IntroMs + BootSplashTiming.WakeMinVisibleMs - 50), "TiTino, después de la intro");
            Assert.That(report.SplashEndMs, Is.LessThan(20000L - 10000L), "con margen para la edad, el consentimiento y la intro del nivel");

            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "primera apertura: directamente el nivel 1");
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(1));
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro).Or.EqualTo(FlowPhase.Loading), "la intro empieza ahora, tras la pantalla");

            var probe = new DevProbe();
            string line = probe.FormatBoot(report)!;
            StringAssert.StartsWith("AF_STATE {\"event\":\"boot\"", line);
            StringAssert.Contains("\"first_open\":true", line);
            StringAssert.Contains("\"splash_ms\":" + report.SplashMs, line);
            StringAssert.Contains("\"first_touch_ms\":null", line);
            StringAssert.Contains("\"sloth\":\"sloth_wake\"", line);
            StringAssert.Contains("\"wake_fits\":true", line);

            // Primer toque en el nivel 1 (durante la intro: la salta).
            boot.TapAnywhere();
            Assert.That(report.FirstTouchMs, Is.GreaterThanOrEqualTo(report.SplashEndMs), "first_touch_ms, desde el arranque en frío");
            Assert.That(report.FirstTouchScreen, Is.EqualTo("level"));
            Assert.That(report.FirstTouchLevel, Is.EqualTo(1));
            long first = report.FirstTouchMs;
            yield return null;
            boot.TapBackground();
            Assert.That(report.FirstTouchMs, Is.EqualTo(first), "solo el primero");
            line = probe.FormatBoot(report)!;
            StringAssert.Contains("\"first_touch_ms\":" + first, line);
            StringAssert.Contains("\"touch_screen\":\"level\",\"touch_level\":1", line);
        }

        [UnityTest]
        public IEnumerator LaterOpen_Idle_NoAddedWait_ThenTheLobby()
        {
            BootSplash.EnabledOverride = true;
            BrandIntro.EnabledOverride = false; // los tiempos de TiTino sin la intro de la marca, que espera su 2,5 s (BrandIntroTests)
            MapArtRule.ForceProvisional = true;
            Watch();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(_watcher!.SawSplash, Is.True);
            Assert.That(_watcher.SawWake, Is.False, "las demás aperturas: sloth_idle");
            Assert.That(_watcher.AppStartedUnderSplash, Is.False);
            Assert.That(_watcher.FramesAfterLoad, Is.EqualTo(0), "las siguientes no añaden espera: ni un fotograma de la pantalla tras la carga");
            BootReport report = boot.BootInfo!;
            Assert.That(report.FirstOpen, Is.False);
            Assert.That(report.SlothPose, Is.EqualTo("sloth_idle"));
            Assert.That(report.HoldMs, Is.EqualTo(0L), "las siguientes no añaden espera: se corta al terminar la carga");
            Assert.That(report.SplashMs, Is.EqualTo(report.LoadMs));
            Assert.That(report.WakeFits, Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));

            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(report.FirstTouchScreen, Is.EqualTo("lobby"), "el primer toque, en «Jugar»");
            Assert.That(report.FirstTouchLevel, Is.EqualTo(4));
            Assert.That(report.FirstTouchMs, Is.GreaterThanOrEqualTo(report.SplashEndMs));
        }

        [UnityTest]
        public IEnumerator WithoutAnyArt_FirstOpen_NoSloth_NoWait()
        {
            BootSplash.EnabledOverride = true;
            BrandIntro.EnabledOverride = false; // los tiempos de TiTino sin la intro de la marca, que espera su 2,5 s (BrandIntroTests)
            MapArtRule.ForceProvisional = false; // forzado: hoy el arte por código sale en todos los builds, release incluido (D-052)
            ArtSlots.ForceProcedural = true;
            Watch();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(_watcher!.SawSplash, Is.True, "el río y el logotipo salen también sin TiTino");
            Assert.That(_watcher.SawAnySloth, Is.False, "MapArtRule: sin arte, sin TiTino");
            Assert.That(_watcher.FramesAfterLoad, Is.EqualTo(0), "sin TiTino, ni un fotograma de la pantalla tras la carga");
            BootReport report = boot.BootInfo!;
            Assert.That(report.FirstOpen, Is.True);
            Assert.That(report.SlothPose, Is.Null);
            Assert.That(report.HoldMs, Is.EqualTo(0L), "sin TiTino no hay nada que esperar");
            Assert.That(report.WakeFits, Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(1));
            StringAssert.Contains("\"sloth\":null", new DevProbe().FormatBoot(report)!);
        }

        [UnityTest]
        public IEnumerator FirstOpen_WakeDoesNotFitInTwentySeconds_CutsWithoutWaiting_AndNotesIt()
        {
            BootSplash.EnabledOverride = true;
            BrandIntro.EnabledOverride = false; // los tiempos de TiTino sin la intro de la marca, que espera su 2,5 s (BrandIntroTests)
            MapArtRule.ForceProvisional = true;
            BootSplash.TestStartupOffsetMs = 15000; // la carga termina pasados los 15 s: el segundo ya no cabe (09 §6)
            Watch();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(_watcher!.SawWake, Is.True, "primera apertura: sloth_wake, aunque no se espere");
            Assert.That(_watcher.FramesAfterLoad, Is.EqualTo(0), "no cabe: ni un fotograma de la pantalla tras la carga");
            BootReport report = boot.BootInfo!;
            Assert.That(report.FirstOpen, Is.True);
            Assert.That(report.SlothPose, Is.EqualTo("sloth_wake"));
            Assert.That(report.HoldMs, Is.EqualTo(0L), "no cabe: se corta al terminar la carga");
            Assert.That(report.SplashMs, Is.EqualTo(report.LoadMs));
            Assert.That(report.SplashEndMs, Is.GreaterThanOrEqualTo(15000L));

            // Sin espera, el gesto que se vio es el de la carga, que no llega a su segundo (entre ShowSloth y el fin de la carga
            // solo corren uno o dos Update, con un delta de como mucho MaxDeltaMs = 100 ms): no cupo y se anota para el dueño.
            Assert.That(report.SlothMs, Is.LessThan(BootSplashTiming.WakeMinVisibleMs), "el gesto no llegó a su segundo");
            Assert.That(report.WakeFits, Is.False, "wake_fits = false: hacía falta esperar y no cabía");
            string line = new DevProbe().FormatBoot(report)!;
            StringAssert.Contains("\"hold_ms\":0", line);
            StringAssert.Contains("\"wake_fits\":false", line);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));

            boot.TapAnywhere();
            Assert.That(report.FirstTouchMs, Is.GreaterThanOrEqualTo(report.SplashEndMs), "first_touch_ms, con el mismo origen");
        }

        [UnityTest]
        public IEnumerator SlothSwitchedOff_FirstOpen_NoSloth_NoWait()
        {
            BootSplash.EnabledOverride = true;
            BrandIntro.EnabledOverride = false; // los tiempos de TiTino sin la intro de la marca, que espera su 2,5 s (BrandIntroTests)
            BootSplash.SlothEnabledOverride = false; // ui_sloth_enabled apagado en remoto (08 §9; D-016)
            MapArtRule.ForceProvisional = true; // aunque haya arte
            Assert.That(BootSplash.SlothExpected, Is.False);
            Watch();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(_watcher!.SawSplash, Is.True, "el río y el logotipo salen igual");
            Assert.That(_watcher.SawAnySloth, Is.False, "interruptor apagado: sin TiTino");
            Assert.That(_watcher.FramesAfterLoad, Is.EqualTo(0), "sin TiTino, ni un fotograma de la pantalla tras la carga");
            Assert.That(_watcher.SawSlothComposition, Is.False, "compuesta sin TiTino desde el primer fotograma: el logotipo no salta");
            BootReport report = boot.BootInfo!;
            Assert.That(report.SlothPose, Is.Null);
            Assert.That(report.HoldMs, Is.EqualTo(0L), "sin TiTino no hay nada que esperar");
            Assert.That(report.WakeFits, Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));
        }

        [UnityTest]
        public IEnumerator RebuildByLanguage_IsNotAColdStart_NoSplash()
        {
            BootSplash.EnabledOverride = true;
            MapArtRule.ForceProvisional = true;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(4)));
            GameBootstrap first = GameBootstrap.Current!;
            Assert.That(first.BootInfo, Is.Not.Null);

            Watch();
            Assert.That(first.PressSettings(), Is.True);
            first.SettingsView!.LanguageButton(LocLocale.Pseudo)!.onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "reconstrucción en en-XA");
            GameBootstrap rebuilt = GameBootstrap.Current!;
            Assert.That(_watcher!.SawSplash, Is.False, "volver al inicio en el idioma nuevo no es un arranque en frío");
            Assert.That(rebuilt.BootInfo, Is.Null);
            Assert.That(rebuilt.App!.Screen, Is.EqualTo(AppScreen.Lobby));
        }

        private BootSplash NewSplash()
        {
            if (_host == null)
            {
                _host = new GameObject("BootSplashTestHost");
            }

            if (_art == null)
            {
                _art = new ProceduralArt("white");
            }

            return new BootSplash(_host.transform);
        }

        private void Watch()
        {
            var go = new GameObject("SplashWatcher");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _watcher = go.AddComponent<SplashWatcher>();
        }

        private static Profile ProfileAt(int nextLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        /// <summary>Mira la escena cada fotograma mientras arranca (en su propio objeto, que sobrevive a las cargas).</summary>
        private sealed class SplashWatcher : MonoBehaviour
        {
            public bool SawSplash { get; private set; }

            public bool SawSplashBeforeLoad { get; private set; }

            public bool SawWake { get; private set; }

            public bool SawAnySloth { get; private set; }

            public bool AppStartedUnderSplash { get; private set; }

            public bool SawSlothComposition { get; private set; }

            /// <summary>
            /// Fotogramas en que la pantalla seguía a la vista con la app ya construida (la carga terminó): la espera medida, no la
            /// que deduce el informe.
            /// </summary>
            public int FramesAfterLoad { get; private set; }

            private void Update()
            {
                GameBootstrap? boot = GameBootstrap.Current;
                BootSplash? splash = boot != null ? boot.Splash : null;
                if (boot == null || splash == null)
                {
                    return;
                }

                SawSplash = true;
                SawSlothComposition |= splash.ComposedForSloth;
                if (boot.App == null)
                {
                    SawSplashBeforeLoad = true;
                }
                else
                {
                    if (splash.Visible)
                    {
                        FramesAfterLoad++;
                    }

                    if (boot.App.Screen != AppScreen.Boot)
                    {
                        AppStartedUnderSplash = true;
                    }
                }

                if (splash.SlothVisible)
                {
                    SawAnySloth = true;
                    SawWake |= splash.Sloth!.Pose == SlothPoseId.Wake;
                }
            }
        }
    }
}
