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
    /// <item>la pantalla no lleva texto (ni un componente de texto) y solo su fondo recibe toques; sin el arte final (release),
    /// sin TiTino;</item>
    /// <item>en la escena <c>Game</c>: la primera apertura enseña <c>sloth_wake</c> al menos 1,0 s con la app aún en el arranque
    /// (la intro del nivel 1 no corre detrás) y después entra en el nivel 1; las demás aperturas, <c>sloth_idle</c>, sin espera
    /// añadida; sin el arte, ni TiTino ni espera; la reconstrucción por idioma no la vuelve a enseñar;</item>
    /// <item>la sonda <c>AF_STATE boot</c> con <c>first_open</c>, <c>splash_ms</c> y <c>first_touch_ms</c>, que se rellena con
    /// el primer toque en el nivel 1; si el segundo de <c>sloth_wake</c> no cabe en los 20 s, se corta sin esperar y la sonda lo
    /// anota (<c>"wake_fits":false</c>); con <c>ui_sloth_enabled</c> apagado, ni TiTino ni espera;</item>
    /// <item>la composición: TiTino nunca se monta sobre la selva (16:9 y 19,5:9) y, sin él, el logotipo baja al centro del cielo
    /// con un halo que no toca la copa de la selva.</item>
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
            Assert.That(BootSplashTiming.HoldMs(true, false, 2000, 0), Is.EqualTo(0), "sin TiTino a la vista (release), sin espera");
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
            Assert.That(splash.ShowSloth(_art!.SlothRigArt(), true, false), Is.True);
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

            // La rama entra desde el borde derecho, bajo el logotipo (su rama entra con él, c.4).
            Vector2 anchor = splash.BranchAnchor;
            Assert.That(anchor.x + (0.75f * splash.Sloth.Size), Is.EqualTo(360f).Within(0.5f));

            splash.Tick(400f);
            Assert.That(splash.SlothClockMs, Is.EqualTo(400f).Within(0.01f));
            splash.Dispose();
            Assert.That(splash.Visible, Is.False);
            yield return null;
            Assert.That(splash.Canvas == null, Is.True, "el lienzo se destruye al cortar");
        }

        [UnityTest]
        public IEnumerator Splash_LaterOpens_Idle_AndReleaseWithoutArt_NoSloth()
        {
            MapArtRule.ForceProvisional = true;
            ArtSlots.ForceProcedural = true;
            BootSplash idle = NewSplash();
            Assert.That(idle.ShowSloth(_art!.SlothRigArt(), false, false), Is.True);
            Assert.That(idle.Sloth!.Pose, Is.EqualTo(SlothPoseId.Idle), "las demás aperturas: sloth_idle");
            idle.Dispose();

            MapArtRule.ForceProvisional = false;
            BootSplash release = NewSplash();
            Assert.That(_art.SlothRigArt(), Is.Null, "release sin el arte final");
            Assert.That(release.ShowSloth(_art.SlothRigArt(), true, false), Is.False, "MapArtRule: sin arte final, sin TiTino");
            Assert.That(release.SlothVisible, Is.False);
            Assert.That(release.SlothClockMs, Is.EqualTo(0f));
            Assert.That(release.Sloth, Is.Null, "no se crea ningún objeto del rig");
            release.Dispose();
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
                Assert.That(splash.ShowSloth(_art!.SlothRigArt(), firstOpen, false), Is.True);
                Assert.That(splash.ComposedForSloth, Is.True);
                Assert.That(splash.ShortScreen, Is.EqualTo(screen == 640));
                Assert.That(splash.Sloth!.Size, Is.InRange(140f, BootSplash.SlothSizeDp), "propuesta de TiTino c.4: 140-180 dp en el arranque");
                Assert.That(splash.Sloth.Size, Is.EqualTo(splash.SlothFitDp).Within(0.01f));

                // Todo el gesto (sloth_wake y su reposo, o sloth_idle con un giro de cabeza): el fondo de TiTino, por encima de la
                // copa de la selva cercana con aire, y la rama, por debajo de la cuia del logotipo.
                float canopy = splash.HorizonDp - BootSplash.NearCanopyDp;
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

                Assert.That(lowest, Is.LessThanOrEqualTo(canopy - 12f), "TiTino cuelga sobre la selva, sin montarse en ella (" + screen + ", " + (firstOpen ? "wake" : "idle") + ")");
                Assert.That(splash.BranchAnchor.y, Is.GreaterThan(splash.LogoCenterDp + 89f), "la rama, por debajo de la cuia del logotipo");
                splash.Dispose();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Composition_WithoutSloth_TheLogoTakesTheSky([Values(780, 640)] int screen)
        {
            MapArtRule.ForceProvisional = false; // release sin el arte final
            ArtSlots.ForceProcedural = true;
            GameScreen.SimulatedScreen = screen == 640 ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            BootSplash splash = NewSplash();
            Assert.That(splash.ComposedForSloth, Is.True, "compuesta para TiTino hasta saber si sale");
            Assert.That(splash.ShowSloth(_art!.SlothRigArt(), true, false), Is.False);
            Assert.That(splash.ComposedForSloth, Is.False, "sin TiTino se recompone: el logotipo, solo");
            float h = screen == 640 ? 640f : 780f;
            Assert.That(splash.LogoCenterDp, Is.EqualTo((screen == 640 ? 0.34f : 0.36f) * h).Within(0.01f), "el centro óptico del cielo");
            Assert.That(splash.GlowSizeDp, Is.EqualTo(300f));
            Assert.That(splash.LogoCenterDp + (splash.GlowSizeDp * 0.5f), Is.LessThan(splash.HorizonDp - BootSplash.FarCanopyDp), "el halo no toca la copa de la selva");
            splash.Dispose();

            // Ya sin TiTino desde el principio (lo que hace GameBootstrap con BootSplash.SlothExpected = false): lo mismo.
            var alone = new BootSplash(_host!.transform, withSloth: false);
            Assert.That(alone.ComposedForSloth, Is.False);
            Assert.That(alone.LogoCenterDp, Is.EqualTo((screen == 640 ? 0.34f : 0.36f) * h).Within(0.01f));
            alone.Dispose();
            yield return null;
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
            Assert.That(boot.Splash, Is.Null, "cortada");

            BootReport report = boot.BootInfo!;
            Assert.That(report.FirstOpen, Is.True);
            Assert.That(report.SlothPose, Is.EqualTo("sloth_wake"));
            Assert.That(report.SlothMs, Is.GreaterThanOrEqualTo(BootSplashTiming.WakeMinVisibleMs), "sloth_wake a la vista ≥ 1,0 s");
            Assert.That(report.WakeFits, Is.True);
            Assert.That(report.SplashMs, Is.GreaterThanOrEqualTo(report.LoadMs + report.HoldMs - 1));
            Assert.That(report.FirstTouchMs, Is.EqualTo(-1L), "aún no ha tocado");

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
            MapArtRule.ForceProvisional = true;
            Watch();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(_watcher!.SawSplash, Is.True);
            Assert.That(_watcher.SawWake, Is.False, "las demás aperturas: sloth_idle");
            Assert.That(_watcher.AppStartedUnderSplash, Is.False);
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
        public IEnumerator ReleaseWithoutArt_FirstOpen_NoSloth_NoWait()
        {
            BootSplash.EnabledOverride = true;
            MapArtRule.ForceProvisional = false;
            ArtSlots.ForceProcedural = true;
            Watch();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(_watcher!.SawSplash, Is.True, "el río y el logotipo salen también en release");
            Assert.That(_watcher.SawAnySloth, Is.False, "MapArtRule: sin arte final, sin TiTino");
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
            MapArtRule.ForceProvisional = true;
            BootSplash.TestStartupOffsetMs = 15000; // la carga termina pasados los 15 s: el segundo ya no cabe (09 §6)
            Watch();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(_watcher!.SawWake, Is.True, "primera apertura: sloth_wake, aunque no se espere");
            BootReport report = boot.BootInfo!;
            Assert.That(report.FirstOpen, Is.True);
            Assert.That(report.SlothPose, Is.EqualTo("sloth_wake"));
            Assert.That(report.HoldMs, Is.EqualTo(0L), "no cabe: se corta al terminar la carga");
            Assert.That(report.SplashMs, Is.EqualTo(report.LoadMs));
            Assert.That(report.SplashEndMs, Is.GreaterThanOrEqualTo(15000L));

            // Sin espera, el gesto que se vio es el de la carga: si no llegó a su segundo, no cupo y se anota para el dueño.
            Assert.That(report.WakeFits, Is.EqualTo(report.SlothMs >= BootSplashTiming.WakeMinVisibleMs), "wake_fits = false si hacía falta esperar");
            string line = new DevProbe().FormatBoot(report)!;
            StringAssert.Contains("\"hold_ms\":0", line);
            StringAssert.Contains("\"wake_fits\":" + (report.WakeFits ? "true" : "false"), line);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));

            boot.TapAnywhere();
            Assert.That(report.FirstTouchMs, Is.GreaterThanOrEqualTo(report.SplashEndMs), "first_touch_ms, con el mismo origen");
        }

        [UnityTest]
        public IEnumerator SlothSwitchedOff_FirstOpen_NoSloth_NoWait()
        {
            BootSplash.EnabledOverride = true;
            BootSplash.SlothEnabledOverride = false; // ui_sloth_enabled apagado en remoto (08 §9; D-016)
            MapArtRule.ForceProvisional = true; // aunque haya arte
            Assert.That(BootSplash.SlothExpected, Is.False);
            Watch();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(_watcher!.SawSplash, Is.True, "el río y el logotipo salen igual");
            Assert.That(_watcher.SawAnySloth, Is.False, "interruptor apagado: sin TiTino");
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
                else if (boot.App.Screen != AppScreen.Boot)
                {
                    AppStartedUnderSplash = true;
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
