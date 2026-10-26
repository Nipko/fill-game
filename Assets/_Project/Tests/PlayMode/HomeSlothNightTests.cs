#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// TiTino de noche en el inicio (añadido de D-050 del 2026-10-26, a la observación 3 de la jornada 3 de la prueba completa:
    /// «De noche TiTino duerme callado (solo un Zzz); si lo toco, se despierta un momento y dice su mensaje.»), en la escena
    /// <c>Game</c> real y con un <see cref="ManualClock"/>:
    /// <list type="bullet">
    /// <item>de noche, dormido: sin bocadillo ni saludo (el de regreso no se dice ni se gasta), con el «Zzz» junto a la cabeza, que
    /// no tapa la travesía, el letrero ni lo demás del inicio; TalkBack dice que duerme;</item>
    /// <item>al tocarlo: <c>sloth_wake</c>, el mensaje siguiente de la rotación (el Dato del día primero) y, cuando se va, otra vez
    /// dormido con el «Zzz»; la rotación no vuelve sola;</item>
    /// <item>con los mensajes apagados, se despierta y vuelve a dormirse sin mensaje;</item>
    /// <item>de día, sin «Zzz» ni lectura propia; al amanecer con el inicio abierto, la rotación vuelve;</item>
    /// <item>con Reducir movimiento, el «Zzz» queda quieto; sin asignaciones por fotograma.</item>
    /// </list>
    /// </summary>
    public sealed class HomeSlothNightTests
    {
        private static readonly DateTime Day0 = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        private const string Today = "2026-10-15";

        [SetUp]
        public void SetUp()
        {
            HomeSloth.ManualTickForTests = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetDailyOverrides();
            HomeSloth.ManualTickForTests = false;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator AtNight_SleepsQuietly_WithTheZzz_AndNeverCoversTheLobby()
        {
            foreach (Vector2Int size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340) })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    LaunchOptions.FontScaleOverride = scale;
                    GameScreen.SimulatedScreen = new Vector4(size.x, size.y, 96f, 48f);
                    ManualClock clock = At(0, 23);
                    Profile profile = ProfileAt(58, clock);
                    profile.Wallet.Canoes.Count = 3;
                    profile.Wallet.Canoes.NextRegenAt = clock.UtcNow + TimeSpan.FromMinutes(10);
                    profile.Wallet.Seeds = 12345;
                    yield return Load(new MemoryProfileStore(profile), clock);
                    LobbyScreen lobby = GameBootstrap.Current!.Lobby!;
                    HomeSloth sloth = lobby.HomeWindow.Sloth!;
                    string where = size.x + " × " + size.y + " al " + Mathf.RoundToInt(scale * 100f) + " %";
                    sloth.Tick(16f);
                    Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Sleep), where + ": 23:00, sloth_sleep");
                    Assert.That(sloth.Asleep, NUnit.Framework.Is.True, where);
                    Assert.That(sloth.ZzzShown, NUnit.Framework.Is.True, where + ": el «Zzz» junto a la cabeza");
                    Assert.That(sloth.Rotation.Count, NUnit.Framework.Is.GreaterThan(0), where + ": la rotación existe, pero calla");

                    // Una vuelta larga de la rotación: de día habría salido más de un mensaje.
                    Rect zzz = Rect.zero;
                    for (int t = 0; t < 14000; t += 16)
                    {
                        sloth.Tick(16f);
                        Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, where + ": de noche, sin bocadillo (" + t + " ms)");
                        if (t % 400 == 0)
                        {
                            lobby.Tick(16f);
                            Canvas.ForceUpdateCanvases();
                            zzz = Union(zzz, ZzzRect(sloth, lobby));
                        }
                    }

                    Assert.That(sloth.ZzzShown, NUnit.Framework.Is.True, where);
                    Assert.That(zzz.width, NUnit.Framework.Is.GreaterThan(10f), where + ": las zetas suben");
                    Rect box = HomeSlothTests.CanvasRect(lobby.HomeWindow.Root, lobby);
                    Debug.Log("AF_HOME_SLOTH noche " + where + ": Zzz " + zzz + ", caja de TiTino " + box + ", límites " + sloth.ZzzBoundsDp);
                    Assert.That(zzz.yMin, NUnit.Framework.Is.GreaterThanOrEqualTo(box.yMin - 0.5f), where + ": el «Zzz» no sube por encima de la caja de TiTino");
                    foreach (KeyValuePair<string, Rect> other in HomeSlothTests.Others(lobby))
                    {
                        Assert.That(HomeSlothTests.Shrunk(zzz).Overlaps(HomeSlothTests.Shrunk(other.Value)), NUnit.Framework.Is.False, where + ": el «Zzz» tapa " + other.Key);
                    }

                    Assert.That(lobby.StopRemainingShown, NUnit.Framework.Is.True, where + ": de noche la travesía no se aparta");
                    Assert.That(zzz.Overlaps(HomeSlothTests.CanvasRect(lobby.StopRemainingRect, lobby)), NUnit.Framework.Is.False, where + ": el «Zzz» tapa el letrero");
                    foreach (Button tap in new[] { lobby.Voyage.CanoeButton, lobby.Voyage.StopButton })
                    {
                        if (tap.gameObject.activeInHierarchy)
                        {
                            Assert.That(zzz.Overlaps(HomeSlothTests.CanvasRect((RectTransform)tap.transform, lobby)), NUnit.Framework.Is.False, where + ": el «Zzz» tapa " + tap.name);
                        }
                    }
                    foreach (Graphic g in sloth.ZzzRect.GetComponentsInChildren<Graphic>(true))
                    {
                        Assert.That(g.raycastTarget, NUnit.Framework.Is.False, where + ": " + g.name + " (el toque es el de TiTino)");
                    }

                    Assert.That(A11yScanner.ButtonLabel(sloth.TapButton, out _), NUnit.Framework.Is.EqualTo("TiTino duerme"), where + ": TalkBack");
                }
            }
        }

        [UnityTest]
        public IEnumerator AtNight_TheGreetingIsNotSaid_NorSpent_ButTheVisitCounts()
        {
            ManualClock clock = At(0, 22);
            Profile profile = ProfileAt(58, clock);
            profile.Sloth.LastSeenDate = "2026-10-11"; // 4 días fuera: de día, el saludo de regreso
            var store = new MemoryProfileStore(profile);
            yield return Load(store, clock);
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);
            Assert.That(sloth.GreetingDecisions, NUnit.Framework.Is.EqualTo(1));
            Assert.That(sloth.LastGreeting.Greeting, NUnit.Framework.Is.False, "de noche no saluda");
            Assert.That(sloth.LastGreeting.Gesture, NUnit.Framework.Is.EqualTo(SlothGesture.None), "ni con la mano: duerme");
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Sleep));
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False);
            Profile saved = store.Load().Profile!;
            Assert.That(saved.Sloth.LastGreetingDate, NUnit.Framework.Is.Null, "el saludo del día no se gasta");
            Assert.That(saved.Sloth.LastSeenDate, NUnit.Framework.Is.EqualTo(Today), "la visita cuenta");
        }

        [UnityTest]
        public IEnumerator AtNight_ATapWakesHim_HeSaysTheNextNote_AndFallsAsleepAgain()
        {
            ManualClock clock = At(0, 23);
            var store = new MemoryProfileStore(ProfileAt(58, clock));
            yield return Load(store, clock);
            HomeSloth sloth = Sloth();
            var changes = new List<string>();
            sloth.AsleepChanged = (asleep, cause) => changes.Add(asleep + ":" + cause);
            sloth.Tick(16f);
            Step(sloth, 2000);
            Assert.That(sloth.Asleep, NUnit.Framework.Is.True);
            int saves = store.SaveCount;

            sloth.TapButton.onClick.Invoke();
            Assert.That(sloth.Asleep, NUnit.Framework.Is.False, "lo despierta el toque");
            Assert.That(sloth.AwakeAtNight, NUnit.Framework.Is.True);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Wake), "sloth_wake");
            Assert.That(sloth.Rig!.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Wake));
            Assert.That(sloth.ZzzShown, NUnit.Framework.Is.False, "despierto, sin «Zzz»");
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.True, "dice su mensaje");
            Assert.That(sloth.Note, NUnit.Framework.Is.EqualTo(HomeSloth.HomeNote.Fact), "como de día: el Dato del día primero");
            Assert.That(A11yScanner.ButtonLabel(sloth.TapButton, out _), NUnit.Framework.Is.Empty, "despierto, la lectura de siempre");
            Assert.That(changes, NUnit.Framework.Is.EqualTo(new[] { "False:tap" }), "el aviso del despertar, una vez");

            Step(sloth, (int)SlothRig.WakeMs + 50);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Idle), "tras despertarse, en reposo mientras habla");
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.True);
            Step(sloth, (int)sloth.BubbleDurationMs + (int)HomeSloth.BubbleFadeMs + 100);
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, "el mensaje se va");
            Step(sloth, 64);
            Assert.That(sloth.Asleep, NUnit.Framework.Is.True, "y vuelve a dormirse");
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Sleep));
            Assert.That(sloth.Rig.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Sleep));
            Assert.That(sloth.ZzzShown, NUnit.Framework.Is.True, "con su «Zzz»");
            Assert.That(changes, NUnit.Framework.Is.EqualTo(new[] { "False:tap", "True:sleep" }));

            Step(sloth, (int)HomeSloth.RotationGapMs * 2);
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, "la rotación no vuelve sola de noche");
            Assert.That(sloth.Asleep, NUnit.Framework.Is.True);

            // Otro toque: el siguiente de la rotación (el Dato ya salió en esta sesión).
            Assert.That(sloth.Tap(), NUnit.Framework.Is.True);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Wake));
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.True);
            Assert.That(sloth.Note, NUnit.Framework.Is.Not.EqualTo(HomeSloth.HomeNote.Fact));
            Assert.That(sloth.Taps, NUnit.Framework.Is.EqualTo(2));
            Assert.That(store.SaveCount, NUnit.Framework.Is.EqualTo(saves), "despertarlo no cuida ni premia: el perfil no cambia");
        }

        [UnityTest]
        public IEnumerator AtNight_WithMessagesOff_ATapWakesHimWithoutANote_AndHeSleepsAgain()
        {
            ManualClock clock = At(0, 2);
            Profile profile = ProfileAt(58, clock);
            profile.Settings.SlothMessages = false;
            yield return Load(new MemoryProfileStore(profile), clock);
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);
            Assert.That(sloth.Asleep, NUnit.Framework.Is.True, "02:00: duerme");
            Assert.That(sloth.ZzzShown, NUnit.Framework.Is.True, "el «Zzz» no es un mensaje: sale también sin ellos");

            Assert.That(sloth.Tap(), NUnit.Framework.Is.True);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Wake));
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, "sin mensaje");
            Step(sloth, (int)HomeSloth.NightAwakeMs - 200);
            Assert.That(sloth.AwakeAtNight, NUnit.Framework.Is.True, "despierto un momento");
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False);
            Step(sloth, 400);
            Assert.That(sloth.Asleep, NUnit.Framework.Is.True, "y otra vez dormido");
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Sleep));
            Assert.That(sloth.ZzzShown, NUnit.Framework.Is.True);
        }

        [UnityTest]
        public IEnumerator ByDay_NoZzz_AndAtDawnTheRotationComesBack()
        {
            ManualClock clock = At(0, 15);
            yield return Load(new MemoryProfileStore(ProfileAt(58, clock)), clock);
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);
            Assert.That(sloth.Asleep, NUnit.Framework.Is.False);
            Assert.That(sloth.ZzzShown, NUnit.Framework.Is.False, "de día, sin «Zzz»");
            Assert.That(A11yScanner.ButtonLabel(sloth.TapButton, out _), NUnit.Framework.Is.Empty, "de día, sin lectura propia (como antes)");
            Assert.That(sloth.Tap(), NUnit.Framework.Is.True);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Look), "de día, el toque de siempre");
            Assert.That(sloth.AwakeAtNight, NUnit.Framework.Is.False);

            // De noche con el inicio abierto: se duerme y calla; al amanecer, vuelve la rotación.
            ManualClock night = At(1, 4);
            yield return Load(new MemoryProfileStore(ProfileAt(58, night)), night);
            sloth = Sloth();
            sloth.Tick(16f);
            Step(sloth, 8000);
            Assert.That(sloth.Asleep, NUnit.Framework.Is.True);
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False);
            night.Set(Day0.AddDays(1).AddHours(6.5));
            Step(sloth, (int)HomeSloth.PoseCheckMs + 32);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Idle), "06:30: despierto");
            Assert.That(sloth.Asleep, NUnit.Framework.Is.False);
            Assert.That(sloth.ZzzShown, NUnit.Framework.Is.False);
            bool spoke = false;
            for (int t = 0; t < (int)HomeSloth.RotationGapMs + 200 && !spoke; t += 16)
            {
                sloth.Tick(16f);
                spoke = sloth.BubbleShown;
            }

            Assert.That(spoke, NUnit.Framework.Is.True, "de día, la rotación vuelve sola");
        }

        [UnityTest]
        public IEnumerator WithReduceMotion_TheZzzStaysStill()
        {
            ManualClock clock = At(0, 23);
            Profile profile = ProfileAt(58, clock);
            profile.Settings.ReduceMotion = ReduceMotionSetting.On;
            yield return Load(new MemoryProfileStore(profile), clock);
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);
            Assert.That(sloth.ZzzShown, NUnit.Framework.Is.True);
            RectTransform[] zs = Zs(sloth);
            Vector2[] before = new Vector2[zs.Length];
            for (int i = 0; i < zs.Length; i++)
            {
                before[i] = zs[i].anchoredPosition;
            }

            Step(sloth, 2500);
            for (int i = 0; i < zs.Length; i++)
            {
                Assert.That(zs[i].anchoredPosition, NUnit.Framework.Is.EqualTo(before[i]), "zeta " + i + ": quieta");
                Assert.That(zs[i].GetComponent<CanvasGroup>().alpha, NUnit.Framework.Is.EqualTo(1f), "zeta " + i + ": entera");
            }

            Assert.That(zs[0].localScale.x, NUnit.Framework.Is.LessThan(zs[2].localScale.x), "de menor a mayor");

            // Sin Reducir movimiento, suben.
            ManualClock other = At(1, 23);
            yield return Load(new MemoryProfileStore(ProfileAt(58, other)), other);
            sloth = Sloth();
            sloth.Tick(16f);
            zs = Zs(sloth);
            Vector2 first = zs[0].anchoredPosition;
            Step(sloth, 600);
            Assert.That(zs[0].anchoredPosition, NUnit.Framework.Is.Not.EqualTo(first), "con movimiento, la zeta sube");
        }

        [UnityTest]
        public IEnumerator AtNight_Tick_DoesNotAllocate()
        {
            ManualClock clock = At(0, 23);
            yield return Load(new MemoryProfileStore(ProfileAt(58, clock)), clock);
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);
            Step(sloth, 1200); // la primera pasada por cada camino no cuenta
            Assert.That(sloth.ZzzShown, NUnit.Framework.Is.True);

            void Run80()
            {
                for (int i = 0; i < 80; i++)
                {
                    sloth.Tick(16f);
                }
            }

            Assert.That(Run80, Is.Not.AllocatingGCMemory(), "80 fotogramas dormido con el «Zzz» y una mirada a la hora");
        }

        private static RectTransform[] Zs(HomeSloth sloth)
        {
            var zs = new RectTransform[HomeSlothZzz.Count];
            for (int i = 0; i < zs.Length; i++)
            {
                zs[i] = (RectTransform)sloth.ZzzRect.GetChild(i);
            }

            return zs;
        }

        // La caja de las barras del «Zzz» a la vista ahora (dp del lienzo, y hacia abajo).
        private static Rect ZzzRect(HomeSloth sloth, LobbyScreen lobby)
        {
            Rect r = Rect.zero;
            foreach (Image bar in sloth.ZzzRect.GetComponentsInChildren<Image>(false))
            {
                r = Union(r, HomeSlothTests.CanvasRect(bar.rectTransform, lobby));
            }

            return r;
        }

        private static Rect Union(Rect a, Rect b)
        {
            if (a.width <= 0f && a.height <= 0f)
            {
                return b;
            }

            return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }

        private static ManualClock At(int dayOffset, int hour)
        {
            return new ManualClock(Day0.AddDays(dayOffset).AddHours(hour));
        }

        private static Profile ProfileAt(int nextLevel, IClock clock)
        {
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        private static IEnumerator Load(MemoryProfileStore store, ManualClock clock)
        {
            LaunchOptions.ClockOverride = clock;
            yield return PlayModeHelpers.LoadApp(store);
            yield return null;
            Assert.That(GameBootstrap.Current!.App!.Screen, NUnit.Framework.Is.EqualTo(AppScreen.Lobby));
        }

        private static HomeSloth Sloth()
        {
            return GameBootstrap.Current!.Lobby!.HomeWindow.Sloth!;
        }

        private static void Step(HomeSloth sloth, int ms)
        {
            for (int t = 0; t < ms; t += 16)
            {
                sloth.Tick(16f);
            }
        }
    }
}
