#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.Localization;
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
    /// TiTino en el inicio «el muelle del río» (D-050, punto 1; antes, la ventanita de TI6c), en la escena <c>Game</c> real y con un
    /// <see cref="ManualClock"/> (propuesta de TiTino d.7; D-016, punto 5):
    /// <list type="bullet">
    /// <item>la pose sigue la hora local: de noche <c>sloth_sleep</c>, al sol <c>sloth_sunbathe</c> y el resto del día
    /// <c>sloth_idle</c>, también con un desfase horario y cuando la hora cambia con el inicio abierto;</item>
    /// <item>los saludos (de regreso tras ≥ 3 días y el de la mañana): como mucho uno al día entre los dos, con <c>sloth.*</c>
    /// guardado en el perfil, que sobrevive a cerrar y abrir el juego; con «El río sigue creciendo» delante, el saludo espera a
    /// que se cierre; durante <c>sloth_wave</c>, la garra no sube a los saldos;</item>
    /// <item>el bocadillo rota: la distancia a casa, la Canasta, el dato del día (y antes del 10, animar a jugar), nunca compras;
    /// el toque: <c>sloth_look</c> y el mensaje siguiente (el dato, primero); tocarlo no cambia el perfil;</item>
    /// <item>con los mensajes apagados, solo gestos; en release, solo textos aprobados; con <c>ui_sloth_enabled</c> apagado,
    /// nada;</item>
    /// <item>oculto con Sin canoas y con la hoja de compra; el bocadillo y el área táctil no tapan «Jugar», los saldos, Ajustes,
    /// Música ni Sonido a 360 × 640 ni a 360 × 780 dp, al 100 % ni al 130 %, y la travesía se aparta si el bocadillo la alcanza;</item>
    /// <item>cuelga de la rama del decorado, que sale del follaje (la del rig no se pinta nunca);</item>
    /// <item>sin asignaciones por fotograma, tampoco mientras el juego arranca.</item>
    /// </list>
    /// </summary>
    public sealed class HomeSlothTests
    {
        private static readonly DateTime Day0 = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        private const string Today = "2026-10-15";
        private const string Nbsp = " ";

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

        [Test]
        public void BasePose_IsSleepAtNight_SunbatheInTheSunnyHours_AndIdleOtherwise()
        {
            for (int hour = 0; hour < 24; hour++)
            {
                SlothPoseId expected = hour >= 20 || hour < 6
                    ? SlothPoseId.Sleep
                    : hour >= 10 && hour < 16 ? SlothPoseId.Sunbathe : SlothPoseId.Idle;
                Assert.That(HomeSloth.BasePoseFor(hour), NUnit.Framework.Is.EqualTo(expected), hour + ":00");
            }
        }

        [UnityTest]
        public IEnumerator Pose_FollowsTheLocalHour_AndChangesWithTheLobbyOpen()
        {
            ManualClock clock = At(0, 22);
            yield return Load(new MemoryProfileStore(ProfileAt(58, clock)), clock);
            HomeSloth sloth = Sloth();
            HomeWindow home = GameBootstrap.Current!.Lobby!.HomeWindow;
            sloth.Tick(16f);
            Assert.That(home.Visible, NUnit.Framework.Is.True);
            Assert.That(sloth.Shown, NUnit.Framework.Is.True);
            Assert.That(sloth.Rig, NUnit.Framework.Is.Not.Null, "el rig provisional en el Editor");
            Assert.That(sloth.Rig!.Visible, NUnit.Framework.Is.True);
            Assert.That(home.PortraitImage.enabled, NUnit.Framework.Is.False, "con el rig, el retrato no se pinta");
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Sleep), "22:00: duerme");
            Assert.That(sloth.Rig.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Sleep));
            Assert.That(sloth.RigBranchShown, NUnit.Framework.Is.False, "en ovillo, sobre la rama del decorado (D-050): la del rig no se pinta");
            Assert.That(sloth.LastGreeting.Greeting, NUnit.Framework.Is.False, "de noche y sin días fuera: sin saludo");

            clock.Set(Day0.AddDays(1).AddHours(12));
            Step(sloth, 1100);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Sunbathe), "12:00 con el inicio abierto: al sol");
            Assert.That(sloth.Rig.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Sunbathe));
            Assert.That(sloth.RigBranchShown, NUnit.Framework.Is.False, "tumbado, sobre la rama del decorado");

            clock.Set(Day0.AddDays(1).AddHours(17));
            Step(sloth, 1100);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Idle), "17:00: en reposo");
            Assert.That(sloth.RigBranchShown, NUnit.Framework.Is.False, "colgado, de la rama del decorado");
            Assert.That(sloth.GreetingDecisions, NUnit.Framework.Is.EqualTo(1), "el saludo se decide una vez por visita");

            // Hora local, no UTC: las 14:00 UTC son las 09:00 con un desfase de −5 h (Colombia, Perú).
            var bogota = new ManualClock(Day0.AddDays(2).AddHours(14), TimeSpan.FromHours(-5));
            yield return Load(new MemoryProfileStore(ProfileAt(58, bogota)), bogota);
            sloth = Sloth();
            sloth.Tick(16f);
            Step(sloth, 2000); // pasa el saludo de la mañana
            Assert.That(sloth.BasePose, NUnit.Framework.Is.EqualTo(SlothPoseId.Idle), "09:00 local: en reposo, no al sol de las 14:00 UTC");
        }

        [UnityTest]
        public IEnumerator MorningGreeting_ComesOnceADay_AndIsSavedInTheProfile()
        {
            ManualClock clock = At(0, 8);
            var store = new MemoryProfileStore(ProfileAt(58, clock));
            yield return Load(store, clock);
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);
            SlothLine line = sloth.LastGreeting;
            Assert.That(line.Greeting, NUnit.Framework.Is.True, "primera apertura del día entre las 06:00 y las 12:00");
            Assert.That(line.Gesture, NUnit.Framework.Is.EqualTo(SlothGesture.Wave));
            Assert.That(line.Key, NUnit.Framework.Is.EqualTo("sloth_line_return_04"), "e.4, n.º 25");
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Wave), "sloth_wave");
            Assert.That(sloth.BubbleIsGreeting, NUnit.Framework.Is.True);
            Assert.That(sloth.BubbleText, NUnit.Framework.Is.EqualTo(Loc.T("sloth_line_return_04")));
            Assert.That(sloth.BubbleDurationMs, NUnit.Framework.Is.EqualTo((float)NoticeTiming.ReadMs(sloth.BubbleText)), "D-011");
            Assert.That(sloth.BubbleText.Length, NUnit.Framework.Is.LessThanOrEqualTo(50), "su bocadillo, ≤ 50 caracteres en ES (07 §6.5; D-013)");
            Assert.That(sloth.Note, NUnit.Framework.Is.EqualTo(HomeSloth.HomeNote.Greeting), "primero, el saludo");

            Profile saved = Saved(store);
            Assert.That(saved.Sloth.LastGreetingDate, NUnit.Framework.Is.EqualTo(Today), "sloth.last_greeting_date, guardado");
            Assert.That(saved.Sloth.LastSeenDate, NUnit.Framework.Is.EqualTo(Today), "sloth.last_seen_date, guardado");
            Assert.That(saved.Sloth.LastLine[(int)SlothMoment.Return], NUnit.Framework.Is.EqualTo(3), "sloth.last_line del regreso");

            Step(sloth, (int)SlothRig.WaveMs + 50);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Idle), "tras el saludo, la pose de las 08:00");
            Step(sloth, (int)sloth.BubbleDurationMs + 300);
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, "el bocadillo se va tras su tiempo de lectura");

            // Otra visita en la misma sesión (un nivel y vuelta) y otra apertura el mismo día: ya no saluda.
            sloth.EndVisit();
            sloth.BeginVisit();
            sloth.Tick(16f);
            Assert.That(sloth.GreetingDecisions, NUnit.Framework.Is.EqualTo(2));
            Assert.That(sloth.LastGreeting.Greeting, NUnit.Framework.Is.False, "1 saludo al día");
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False);

            clock.Set(Day0.AddHours(9.5));
            yield return Load(store, clock);
            sloth = Sloth();
            sloth.Tick(16f);
            Assert.That(sloth.LastGreeting.Greeting, NUnit.Framework.Is.False, "otra apertura el mismo día: sin saludo");
            Assert.That(sloth.LastGreeting.Gesture, NUnit.Framework.Is.EqualTo(SlothGesture.None));
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False);

            // Al día siguiente, a las 07:00, vuelve el de la mañana, con la frase siguiente de su mini-banco (D-030, pregunta
            // 16 b): nunca la misma que la última vista del momento.
            clock.Set(Day0.AddDays(1).AddHours(7));
            yield return Load(store, clock);
            sloth = Sloth();
            sloth.Tick(16f);
            Assert.That(sloth.LastGreeting.Greeting, NUnit.Framework.Is.True);
            Assert.That(sloth.LastGreeting.Key, NUnit.Framework.Is.EqualTo("sloth_line_return_05"));
            Assert.That(sloth.BubbleText, NUnit.Framework.Is.EqualTo(Loc.T("sloth_line_return_05")));
            Assert.That(Saved(store).Sloth.LastLine[(int)SlothMoment.Return], NUnit.Framework.Is.EqualTo(4));
            Assert.That(Saved(store).Sloth.LastGreetingDate, NUnit.Framework.Is.EqualTo("2026-10-16"));
        }

        [UnityTest]
        public IEnumerator ReturnGreeting_AfterThreeDays_TakesTheDay_AndNeverComesTwice()
        {
            ManualClock clock = At(0, 8);
            Profile profile = ProfileAt(58, clock);
            profile.Sloth.LastSeenDate = "2026-10-11"; // 4 días fuera
            var store = new MemoryProfileStore(profile);
            yield return Load(store, clock);
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);
            SlothLine line = sloth.LastGreeting;
            Assert.That(line.Greeting, NUnit.Framework.Is.True);
            Assert.That(line.Moment, NUnit.Framework.Is.EqualTo(SlothMoment.Return));
            Assert.That(line.Index, NUnit.Framework.Is.InRange(0, 2), "el de regreso, no el de la mañana (nunca los dos el mismo día)");
            Assert.That(line.Gesture, NUnit.Framework.Is.EqualTo(SlothGesture.Wave));
            Assert.That(sloth.BubbleIsGreeting, NUnit.Framework.Is.True);
            Assert.That(UiText.TrySlothLine(in line, out string text), NUnit.Framework.Is.True);
            Assert.That(sloth.BubbleText, NUnit.Framework.Is.EqualTo(text));
            if (line.Index == 1)
            {
                Assert.That(text, Does.Contain("58"), "«nivel {n}»: el de «Jugar»");
            }

            Assert.That(Saved(store).Sloth.LastGreetingDate, NUnit.Framework.Is.EqualTo(Today));

            clock.Set(Day0.AddHours(10));
            yield return Load(store, clock);
            sloth = Sloth();
            sloth.Tick(16f);
            Assert.That(sloth.LastGreeting.Greeting, NUnit.Framework.Is.False, "ni otro de regreso ni el de la mañana el mismo día");
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False);

            // Con 2 días fuera y por la tarde, nada.
            ManualClock later = At(3, 15);
            Profile two = ProfileAt(58, later);
            two.Sloth.LastSeenDate = "2026-10-16";
            yield return Load(new MemoryProfileStore(two), later);
            sloth = Sloth();
            sloth.Tick(16f);
            Assert.That(sloth.LastGreeting.Greeting, NUnit.Framework.Is.False, "menos de 3 días y fuera de la mañana");
        }

        [UnityTest]
        public IEnumerator Tap_LooksAndBringsTheNextNote_TheFactOfTheDayFirst_WithoutTouchingTheProfile()
        {
            ManualClock clock = At(0, 15);
            var store = new MemoryProfileStore(ProfileAt(58, clock));
            yield return Load(store, clock);
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);
            Assert.That(sloth.LastGreeting.Greeting, NUnit.Framework.Is.False);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Sunbathe), "15:00: al sol");
            Assert.That(sloth.TapButton.gameObject.activeInHierarchy, NUnit.Framework.Is.True);
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, "sin saludo, el primer mensaje espera un poco");
            int saves = store.SaveCount;

            sloth.TapButton.onClick.Invoke();
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Look), "sloth_look");
            Assert.That(sloth.Rig!.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Look));
            Assert.That(sloth.FactShownThisSession, NUnit.Framework.Is.True);
            Assert.That(sloth.FactKey, NUnit.Framework.Is.Not.Null);
            Assert.That(SlothLines.SlothFactKeys, Does.Contain(sloth.FactKey), "un dato de g.3");
            Assert.That(sloth.Note, NUnit.Framework.Is.EqualTo(HomeSloth.HomeNote.Fact), "D-050, punto 1: el dato del día, en el bocadillo");
            Assert.That(sloth.BubbleText, NUnit.Framework.Is.EqualTo(Loc.Fact(sloth.FactKey!)));
            Assert.That(sloth.BubbleDurationMs, NUnit.Framework.Is.EqualTo((float)NoticeTiming.ReadMs(sloth.BubbleText)), "D-011");

            Step(sloth, (int)SlothRig.LookMs + 50);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Sunbathe), "vuelve a la pose de la hora");
            Step(sloth, (int)sloth.BubbleDurationMs + 300);
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False);
            Assert.That(sloth.BubbleRect.gameObject.activeSelf, NUnit.Framework.Is.False, "el bocadillo se va tras su tiempo de lectura");

            // Otro toque: el mensaje siguiente de la rotación, que ya no es el dato.
            sloth.TapButton.onClick.Invoke();
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Look), "cada toque, sloth_look");
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.True);
            Assert.That(sloth.Note, NUnit.Framework.Is.Not.EqualTo(HomeSloth.HomeNote.Fact));
            Assert.That(sloth.Note, NUnit.Framework.Is.Not.EqualTo(HomeSloth.HomeNote.Greeting));
            Assert.That(sloth.Taps, NUnit.Framework.Is.EqualTo(2));
            Step(sloth, 2000);
            Assert.That(store.SaveCount, NUnit.Framework.Is.EqualTo(saves), "tocarlo no cuida ni premia: el perfil no cambia");

            // Otra sesión: el dato vuelve a salir al tocarlo.
            yield return Load(store, clock);
            sloth = Sloth();
            sloth.Tick(16f);
            Assert.That(sloth.FactShownThisSession, NUnit.Framework.Is.False);
            sloth.TapButton.onClick.Invoke();
            Assert.That(sloth.Note, NUnit.Framework.Is.EqualTo(HomeSloth.HomeNote.Fact), "una sesión nueva, el dato");
        }

        [UnityTest]
        public IEnumerator Rotation_TellsTheDistanceHome_TheBasketTheFact_AndBefore10EncouragesToPlay_NeverPurchases()
        {
            ManualClock clock = At(0, 15);
            yield return Load(new MemoryProfileStore(ProfileAt(58, clock)), clock);
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);
            IReadOnlyList<HomeSloth.HomeNote> ring = sloth.Rotation;
            Assert.That(ring, Has.Member(HomeSloth.HomeNote.Home), "D-050: «Casa · a unos 1.480 km», la información de la ventanita de antes");
            Assert.That(ring, Has.Member(HomeSloth.HomeNote.Basket), "la Canasta de Anavilhanas (61), a 3 niveles");
            Assert.That(ring, Has.Member(HomeSloth.HomeNote.Fact));
            Assert.That(ring, Has.No.Member(HomeSloth.HomeNote.Play), "con noticias, no hace falta animar");

            // Sin saludo: el primer mensaje llega a FirstNoteDelayMs, y tras él, otro a RotationGapMs.
            Step(sloth, (int)HomeSloth.FirstNoteDelayMs + 32);
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.True, "el primero de la rotación");
            var seen = new List<HomeSloth.HomeNote>();
            var texts = new List<string>();
            for (int i = 0; i < ring.Count; i++)
            {
                Assert.That(sloth.BubbleShown, NUnit.Framework.Is.True, "mensaje " + i);
                seen.Add(sloth.Note);
                texts.Add(sloth.BubbleText);
                Step(sloth, (int)sloth.BubbleDurationMs + (int)HomeSloth.BubbleFadeMs + (int)HomeSloth.RotationGapMs + 64);
            }

            Assert.That(seen, NUnit.Framework.Is.EquivalentTo(ring), "todos los de la rotación, por turno");
            Assert.That(texts, Does.Contain("Casa · a unos 1.480" + Nbsp + "km"));
            Assert.That(texts, Does.Contain("Faltan 3 niveles para tu próxima Canasta"));
            foreach (string text in texts)
            {
                Assert.That(text, Does.Not.Contain("Cántaro").And.Not.Contain("tienda").And.Not.Contain("comprar"), "D-013, Q10-A: nunca promociona compras");
            }

            // Antes del 10, anima a jugar (sin Reto, ni Bitácora, ni Canasta).
            var list = new List<HomeSloth.HomeNote>();
            HomeSloth.RotationFor(true, 4, true, 7, true, true, list);
            Assert.That(list[0], NUnit.Framework.Is.EqualTo(HomeSloth.HomeNote.Play));
            Assert.That(list, Has.No.Member(HomeSloth.HomeNote.Daily).And.No.Member(HomeSloth.HomeNote.Basket));
            HomeSloth.RotationFor(true, 30, true, 1, false, false, list);
            Assert.That(list, NUnit.Framework.Is.EqualTo(new[] { HomeSloth.HomeNote.Daily, HomeSloth.HomeNote.Basket }), "el Reto de hoy y la Canasta");
            HomeSloth.RotationFor(false, 30, true, 1, true, true, list);
            Assert.That(list, NUnit.Framework.Is.Empty, "sin los mensajes de TiTino, nada");
        }
        [UnityTest]
        public IEnumerator Rotation_ShowsTheSlothFact_AtMostOncePerSession()
        {
            ManualClock clock = At(0, 15);
            yield return Load(new MemoryProfileStore(ProfileAt(58, clock)), clock);
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);
            int count = sloth.Rotation.Count;
            Assert.That(sloth.Rotation, Has.Member(HomeSloth.HomeNote.Fact), "premisa: hay dato de hoy");
            Step(sloth, (int)HomeSloth.FirstNoteDelayMs + 32);
            int facts = 0;
            int shown = 0;
            for (int i = 0; i < (3 * count) + 2; i++)
            {
                if (sloth.BubbleShown)
                {
                    shown++;
                    facts += sloth.Note == HomeSloth.HomeNote.Fact ? 1 : 0;
                    Step(sloth, (int)sloth.BubbleDurationMs + (int)HomeSloth.BubbleFadeMs + 64);
                }

                Step(sloth, (int)HomeSloth.RotationGapMs + 64);
            }

            Assert.That(shown, NUnit.Framework.Is.GreaterThan(2 * count), "tres vueltas de la rotación");
            Assert.That(facts, NUnit.Framework.Is.EqualTo(1), "D-016, punto 5, y 07 §6.5: como mucho un Dato del perezoso por sesión");
            Assert.That(sloth.FactShownThisSession, NUnit.Framework.Is.True);
            sloth.Tap();
            Assert.That(sloth.Note, NUnit.Framework.Is.Not.EqualTo(HomeSloth.HomeNote.Fact), "ni al tocarlo, una vez visto");
        }

        [UnityTest]
        public IEnumerator Rotation_StopsWhenTiTinosMessagesAreSwitchedOff_AndComesBackWhenOn()
        {
            ManualClock clock = At(0, 15);
            yield return Load(new MemoryProfileStore(ProfileAt(58, clock)), clock);
            GameBootstrap boot = GameBootstrap.Current!;
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);
            Step(sloth, (int)HomeSloth.FirstNoteDelayMs + 32);
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.True, "premisa: la rotación en marcha");

            // Ajustes se abre encima del inicio sin cerrar la visita: apagar los mensajes quita el bocadillo y para la rotación.
            Assert.That(boot.ChooseSlothMessages(false), NUnit.Framework.Is.True);
            sloth.Tick(16f);
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, "D-050: respeta el ajuste de mensajes de TiTino");
            for (int t = 0; t < 3 * (int)HomeSloth.RotationGapMs; t += 16)
            {
                sloth.Tick(16f);
                Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, t + " ms: sin mensajes");
            }

            Assert.That(sloth.Tap(), NUnit.Framework.Is.True);
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, "el toque solo hace el gesto");

            // Encendidos otra vez, la rotación vuelve tras una pausa.
            Assert.That(boot.ChooseSlothMessages(true), NUnit.Framework.Is.True);
            Step(sloth, (int)HomeSloth.RotationGapMs + 200);
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.True, "vuelve con los mensajes encendidos");
        }

        [UnityTest]
        public IEnumerator Rotation_WaitsWhileSettingsCoverTheLobby()
        {
            ManualClock clock = At(0, 15);
            yield return Load(new MemoryProfileStore(ProfileAt(58, clock)), clock);
            GameBootstrap boot = GameBootstrap.Current!;
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);
            Assert.That(boot.PressSettings(), NUnit.Framework.Is.True);
            yield return null;
            for (int t = 0; t < 2 * (int)HomeSloth.RotationGapMs; t += 16)
            {
                sloth.Tick(16f);
            }

            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, "con Ajustes encima, los mensajes no se gastan sin verse");
            Assert.That(boot.CloseSettings(), NUnit.Framework.Is.True);
            for (int i = 0; i < 60 && boot.SettingsView != null && boot.SettingsView.Visible; i++)
            {
                yield return null; // Ajustes se va con su fundido
            }

            // El juego también mueve a TiTino en cada fotograma: el mensaje puede salir (y hasta irse) mientras Ajustes se funde; basta
            // con que salga uno en la pausa de la rotación.
            bool shown = sloth.BubbleShown;
            for (int t = 0; !shown && t < (int)HomeSloth.RotationGapMs + 200; t += 16)
            {
                sloth.Tick(16f);
                shown = sloth.BubbleShown;
            }

            Assert.That(shown, NUnit.Framework.Is.True, "con el inicio despejado, la rotación sigue: " + sloth.RotationStateForTests);
        }
        [Test]
        public void FactFor_RotatesByDay_ThroughTheElevenSlothFacts()
        {
            var seen = new HashSet<string>();
            for (int day = 0; day < 11; day++)
            {
                string text = HomeSloth.FactFor(20000 + day, out string? key);
                Assert.That(key, NUnit.Framework.Is.Not.Null);
                Assert.That(text, NUnit.Framework.Is.EqualTo(Loc.Fact(key!)));
                seen.Add(key!);
            }

            Assert.That(seen.Count, NUnit.Framework.Is.EqualTo(11), "en 11 días, los 11 datos de g.3");
            HomeSloth.FactFor(20000, out string? a);
            HomeSloth.FactFor(20000, out string? b);
            Assert.That(a, NUnit.Framework.Is.EqualTo(b), "mismo día, mismo dato");
        }

        [UnityTest]
        public IEnumerator MessagesOff_OnlyGestures()
        {
            ManualClock clock = At(0, 8);
            Profile profile = ProfileAt(58, clock);
            profile.Settings.SlothMessages = false;
            var store = new MemoryProfileStore(profile);
            yield return Load(store, clock);
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);
            Assert.That(sloth.LastGreeting.Greeting, NUnit.Framework.Is.True, "el saludo con la mano cuenta como el del día");
            Assert.That(sloth.LastGreeting.Gesture, NUnit.Framework.Is.EqualTo(SlothGesture.Wave));
            Assert.That(sloth.LastGreeting.Speaks, NUnit.Framework.Is.False, "sin frase");
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Wave));
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False);
            Assert.That(Saved(store).Sloth.LastGreetingDate, NUnit.Framework.Is.EqualTo(Today));

            Step(sloth, 1000);
            Assert.That(sloth.Tap(), NUnit.Framework.Is.True);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Look));
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, "sin mensaje");
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False);
            Assert.That(sloth.FactShownThisSession, NUnit.Framework.Is.False);
        }

        [UnityTest]
        public IEnumerator Release_ShowsOnlyApprovedTexts_AndTheGestureStillCounts()
        {
            ManualClock clock = At(0, 8);
            var store = new MemoryProfileStore(ProfileAt(58, clock));
            yield return Load(store, clock);
            HomeSloth sloth = Sloth();
            LocCatalog catalog = Loc.Catalog;
            string locale = Loc.Locale;
            string? numbers = Loc.RequestedNumberTag;
            Loc.Configure(catalog, locale, false, numbers);
            try
            {
                Assert.That(HomeSloth.FactFor(20000, out string? key), NUnit.Framework.Is.Empty, "todos los datos están pendientes del asesor");
                Assert.That(key, NUnit.Framework.Is.Null);
                sloth.Tick(16f);
                Assert.That(sloth.LastGreeting.Greeting, NUnit.Framework.Is.True);
                Assert.That(sloth.LastGreeting.Speaks, NUnit.Framework.Is.False, "la frase de la mañana está pendiente del asesor");
                Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Wave), "el saludo con la mano sí");
                Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False);
                Assert.That(Saved(store).Sloth.LastGreetingDate, NUnit.Framework.Is.EqualTo(Today));

                Assert.That(sloth.Tap(), NUnit.Framework.Is.True);
                Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Look));
                Assert.That(sloth.Note, NUnit.Framework.Is.Not.EqualTo(HomeSloth.HomeNote.Fact), "en release, solo los datos aprobados");
                Assert.That(sloth.FactShownThisSession, NUnit.Framework.Is.False, "no gasta el dato de la sesión");
            }
            finally
            {
                Loc.Configure(catalog, locale, true, numbers);
            }
        }

        [UnityTest]
        public IEnumerator SlothDisabled_LeavesTheStillPortrait_WithoutTouchOrGreeting()
        {
            ManualClock clock = At(0, 8);
            yield return Load(new MemoryProfileStore(ProfileAt(58, clock)), clock);
            HomeWindow home = GameBootstrap.Current!.Lobby!.HomeWindow;
            HomeSloth sloth = home.Sloth!;
            sloth.Enabled = false;
            home.Apply(home.Stop, true);
            home.Apply(home.Stop, false);
            sloth.Tick(16f);
            Assert.That(home.Visible, NUnit.Framework.Is.True, "la ventanita del Mapa sigue");
            Assert.That(home.PortraitImage.enabled, NUnit.Framework.Is.True, "con el retrato quieto");
            Assert.That(sloth.Rig == null || !sloth.Rig.Visible, NUnit.Framework.Is.True);
            Assert.That(sloth.TapButton.gameObject.activeSelf, NUnit.Framework.Is.False);
            Assert.That(sloth.Tap(), NUnit.Framework.Is.False);
            Assert.That(sloth.GreetingDecisions, NUnit.Framework.Is.EqualTo(0), "ui_sloth_enabled apagado: nada");
        }

        [UnityTest]
        public IEnumerator HiddenWithNoCanoesAndThePurchaseSheet_AndTheGreetingIsNotRepeated()
        {
            ManualClock clock = At(0, 8);
            Profile profile = ProfileAt(58, clock);
            profile.Wallet.Canoes.Count = 0;
            profile.Wallet.Canoes.NextRegenAt = clock.UtcNow + TimeSpan.FromMinutes(18);
            yield return Load(new MemoryProfileStore(profile), clock);
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            HomeSloth sloth = lobby.HomeWindow.Sloth!;
            sloth.Tick(16f);
            Assert.That(sloth.BubbleIsGreeting, NUnit.Framework.Is.True);

            // Sin canoas (d.11: TiTino nunca en Sin canoas): ni rig, ni toque, ni bocadillo.
            Assert.That(boot.PressPlay(), NUnit.Framework.Is.True);
            Assert.That(boot.App!.Screen, NUnit.Framework.Is.EqualTo(AppScreen.NoCanoes));
            yield return null;
            AssertHidden(lobby, "Sin canoas");
            lobby.BackButton.onClick.Invoke();
            yield return null;
            sloth.Tick(16f);
            Assert.That(sloth.Shown, NUnit.Framework.Is.True, "vuelve con el inicio");
            Assert.That(sloth.TapButton.gameObject.activeInHierarchy, NUnit.Framework.Is.True);
            Assert.That(sloth.GreetingDecisions, NUnit.Framework.Is.EqualTo(1), "la misma visita: el saludo no se repite");
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False);

            // La hoja «Compra simulada» delante del inicio (g.2, regla 9).
            sloth.Tap();
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.True);
            StoreSheet sheet = boot.StoreSheetView!;
            boot.Store!.Purchase(new PurchaseRequest(StoreCatalog.NoAds), r => { });
            Assert.That(sheet.Visible, NUnit.Framework.Is.True);
            yield return null;
            AssertHidden(lobby, "la hoja de compra");
            sheet.CancelButton.onClick.Invoke();
            yield return null;
            Assert.That(sloth.Shown, NUnit.Framework.Is.True);
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, "el mensaje se perdió con la hoja: no vuelve");
        }

        [UnityTest]
        public IEnumerator ContentEnd_DoesNotSpendTheGreetingBehindTheModal_ItComesWhenItCloses()
        {
            GameBootstrap.DailyPoolDisabledOverride = true; // sin el Reto: con él, «Jugar · Reto del río» abre el Reto (D-032, punto 6)
            // El catálogo de la escena acaba en el 700 (C1b): con el 701, «Jugar» abre «El río sigue creciendo» (06 §9). El 701 no
            // tiene parada en los datos del Mapa (paradas 0-69): la ventanita enseña la del 700 (LobbyScreen.StopLevel).
            ManualClock clock = At(0, 8);
            var store = new MemoryProfileStore(ProfileAt(701, clock));
            yield return Load(store, clock);
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            Assert.That(boot.App!.PlayLevelAvailable, NUnit.Framework.Is.False, "premisa: el 701 no está publicado");
            Assert.That(lobby.HomeWindow.Stop, NUnit.Framework.Is.Not.Null, "la parada del último nivel publicado");
            Assert.That(lobby.HomeWindow.Stop!.N, NUnit.Framework.Is.EqualTo(69));
            HomeSloth sloth = lobby.HomeWindow.Sloth!;
            Assert.That(boot.PressPlay(), NUnit.Framework.Is.True);
            Assert.That(boot.App!.Screen, NUnit.Framework.Is.EqualTo(AppScreen.ContentEnd));
            yield return null;
            Assert.That(lobby.OverlayVisible, NUnit.Framework.Is.True);
            Assert.That(lobby.HomeWindow.Visible, NUnit.Framework.Is.True, "la ventanita sigue detrás del velo");
            Step(sloth, 1000);
            Assert.That(sloth.GreetingDecisions, NUnit.Framework.Is.EqualTo(0), "detrás del modal no se decide el saludo");
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False);
            Assert.That(sloth.Gesturing, NUnit.Framework.Is.False, "sin sloth_wave detrás del velo");
            Assert.That(Saved(store).Sloth.LastGreetingDate, NUnit.Framework.Is.Null, "el saludo del día no se gasta");

            lobby.BackButton.onClick.Invoke();
            yield return null;
            Assert.That(boot.App.Screen, NUnit.Framework.Is.EqualTo(AppScreen.Lobby));
            sloth.Tick(16f);
            Assert.That(sloth.GreetingDecisions, NUnit.Framework.Is.EqualTo(1), "al cerrarlo, en la misma visita");
            Assert.That(sloth.LastGreeting.Greeting, NUnit.Framework.Is.True);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Wave));
            Assert.That(sloth.BubbleIsGreeting, NUnit.Framework.Is.True);
            Assert.That(Saved(store).Sloth.LastGreetingDate, NUnit.Framework.Is.EqualTo(Today));
        }

        [Test]
        public void WaveDrop_GoesDownWithTheRaisedClaw_AndBack()
        {
            Assert.That(HomeSloth.WaveDrop(0f), NUnit.Framework.Is.EqualTo(0f));
            Assert.That(HomeSloth.WaveDrop(SlothRig.WaveMs), NUnit.Framework.Is.EqualTo(0f));
            Assert.That(HomeSloth.WaveDrop(-5f), NUnit.Framework.Is.EqualTo(0f));
            Assert.That(HomeSloth.WaveDrop(SlothRig.WaveMs * 0.5f), NUnit.Framework.Is.EqualTo(HomeSloth.WaveDropDp).Within(0.001f), "rama efectiva a 16 dp");
            float previous = 0f;
            for (float ms = 0f; ms <= SlothRig.WaveMs * 0.5f; ms += 16f)
            {
                float drop = HomeSloth.WaveDrop(ms);
                Assert.That(drop, NUnit.Framework.Is.GreaterThanOrEqualTo(previous - 0.0001f), ms + " ms: baja sin saltos atrás");
                Assert.That(drop, NUnit.Framework.Is.InRange(0f, HomeSloth.WaveDropDp));
                previous = drop;
            }
        }

        [UnityTest]
        public IEnumerator Wave_TheRaisedClawStaysUnderTheBalances_AndHangsFromTheSceneBranch()
        {
            ManualClock clock = At(0, 8);
            yield return Load(new MemoryProfileStore(ProfileAt(58, clock)), clock);
            LobbyScreen lobby = GameBootstrap.Current!.Lobby!;
            HomeSloth sloth = lobby.HomeWindow.Sloth!;
            sloth.Tick(16f);
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Wave));
            Assert.That(sloth.RigBranchShown, NUnit.Framework.Is.False, "colgado, de la rama del decorado (D-050): la del rig no se pinta");
            Assert.That(lobby.Branch.gameObject.activeInHierarchy, NUnit.Framework.Is.True, "la rama de TiTino, que sale del follaje");
            Assert.That(sloth.Rig!.Size, NUnit.Framework.Is.InRange(HomeWindow.SceneRigMinDp, HomeWindow.SceneRigMaxDp), "TiTino grande: 76-92 dp (D-050)");
            float balances = Mathf.Max(CanvasRect(lobby.SeedsRect, lobby).yMax, CanvasRect((RectTransform)lobby.SettingsButton.transform, lobby).yMax);
            float closest = float.MaxValue;
            var trace = new System.Text.StringBuilder();
            for (int t = 0; t <= (int)SlothRig.WaveMs; t += 16)
            {
                Canvas.ForceUpdateCanvases();
                foreach (Image piece in sloth.Rig!.Root.GetComponentsInChildren<Image>(false))
                {
                    if (!piece.enabled || piece.name.StartsWith("Leaf", StringComparison.Ordinal))
                    {
                        continue; // el cuerpo de TiTino (las hojas no salen al saludar)
                    }

                    Assert.That(piece.name, NUnit.Framework.Is.Not.EqualTo("Branch"), t + " ms: sin la rama del rig");
                    float gap = CanvasRect(piece.rectTransform, lobby).yMin - balances;
                    if (gap < closest)
                    {
                        closest = gap;
                        trace.Append(t).Append(" ms ").Append(piece.name).Append(' ').Append(gap.ToString("0.00")).Append("; ");
                    }
                }

                sloth.Tick(16f);
            }

            Debug.Log("AF_HOME_SLOTH sloth_wave: lo más alto del rig, a " + closest.ToString("0.00") + " dp bajo los saldos (" + trace + ")");
            Assert.That(closest, NUnit.Framework.Is.GreaterThanOrEqualTo(0f), "la garra que saluda no sube a los saldos ni a Ajustes");
            Assert.That(sloth.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Idle), "tras el saludo, la pose de las 08:00");

            // La bajada del saludo, en cada fotograma, sin asignaciones (otra apertura, otro día: otro saludo).
            ManualClock next = At(1, 8);
            yield return Load(new MemoryProfileStore(ProfileAt(58, next)), next);
            HomeSloth again = Sloth();
            again.Tick(16f);
            Step(again, 96);
            Assert.That(again.Pose, NUnit.Framework.Is.EqualTo(SlothPoseId.Wave));

            void Run20()
            {
                for (int i = 0; i < 20; i++)
                {
                    again.Tick(16f);
                }
            }

            Assert.That(Run20, Is.Not.AllocatingGCMemory(), "20 fotogramas de sloth_wave con la bajada del rig");
            Assert.That(again.Gesturing, NUnit.Framework.Is.True);
        }

        [UnityTest]
        public IEnumerator BubbleAndTouch_NeverCoverPlayBalancesSettingsOrTheVoyage_At360x640And360x780_At100And130Percent()
        {
            // El día con el dato más largo de g.3 (rota por días): el peor caso del bocadillo; antes, el saludo de la mañana.
            int longestOffset = 0;
            int longest = -1;
            for (int offset = 0; offset < 11; offset++)
            {
                SlothLines.TryDayNumber(ManualDateKey(offset), out int day);
                string text = HomeSloth.FactFor(day, out _);
                if (text.Length > longest)
                {
                    longest = text.Length;
                    longestOffset = offset;
                }
            }

            foreach (float scale in new[] { 1f, 1.3f })
            {
                foreach (Vector2Int size in new[] { new Vector2Int(1080, 1920), new Vector2Int(1080, 2340) })
                {
                    LaunchOptions.FontScaleOverride = scale;
                    GameScreen.SimulatedScreen = new Vector4(size.x, size.y, 96f, 48f);
                    ManualClock clock = At(longestOffset, 8); // el saludo de la mañana y, después, el toque
                    Profile profile = ProfileAt(58, clock);
                    profile.Wallet.Canoes.Count = 3;
                    profile.Wallet.Canoes.NextRegenAt = clock.UtcNow + TimeSpan.FromMinutes(10);
                    profile.Wallet.Seeds = 12345;
                    yield return Load(new MemoryProfileStore(profile), clock);
                    LobbyScreen lobby = GameBootstrap.Current!.Lobby!;
                    HomeSloth sloth = lobby.HomeWindow.Sloth!;
                    string where = size.x + " × " + size.y + " al " + Mathf.RoundToInt(scale * 100f) + " %";
                    sloth.Tick(16f);
                    Step(sloth, 400);
                    lobby.Tick(16f);
                    Canvas.ForceUpdateCanvases();
                    Assert.That(sloth.BubbleIsGreeting, NUnit.Framework.Is.True, where + ": el saludo de la mañana");
                    AssertBubble(lobby, sloth, where + " · saludo");

                    // Se va el saludo y llega el toque: el dato más largo, en el mismo bocadillo (D-050), si cabe.
                    Step(sloth, (int)sloth.BubbleDurationMs + 300);
                    lobby.Tick(16f);
                    Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, where);
                    Assert.That(lobby.StopRemainingShown, NUnit.Framework.Is.True, where + ": sin bocadillo, la travesía vuelve");
                    Assert.That(sloth.Tap(), NUnit.Framework.Is.True);
                    Step(sloth, 400);
                    lobby.Tick(16f);
                    Canvas.ForceUpdateCanvases();
                    Assert.That(sloth.BubbleShown, NUnit.Framework.Is.True, where + ": el toque trae un mensaje");
                    if (sloth.Note == HomeSloth.HomeNote.Fact)
                    {
                        Assert.That(sloth.BubbleText.Length, NUnit.Framework.Is.EqualTo(longest), where + ": el dato más largo");
                    }

                    AssertBubble(lobby, sloth, where + " · " + sloth.Note);
                }
            }
        }

        // El bocadillo: a la derecha de TiTino con el pico hacia él, dentro de la pantalla, sin tapar «Jugar», los saldos, Ajustes,
        // Música, Sonido ni las pestañas, y con la travesía apartada si la alcanza; su texto, entero a 14 sp (con la escala) y sin
        // toques. El toque de TiTino, de ≥ 48 dp y sobre él.
        private static void AssertBubble(LobbyScreen lobby, HomeSloth sloth, string where)
        {
            Rect bubble = Grow(CanvasRect(sloth.BubbleRect, lobby), HomeSloth.BubbleRimDp);
            Rect tap = CanvasRect(sloth.TapRect, lobby);
            Rect box = CanvasRect(lobby.HomeWindow.Root, lobby);
            Assert.That(tap.width, NUnit.Framework.Is.GreaterThanOrEqualTo(HomeWindow.MinTapDp - 0.01f), where + ": toque de ≥ 48 dp");
            Assert.That(tap.height, NUnit.Framework.Is.GreaterThanOrEqualTo(HomeWindow.MinTapDp - 0.01f), where);
            Assert.That(tap.Contains(box.center), NUnit.Framework.Is.True, where + ": el toque, sobre TiTino");
            Assert.That(bubble.xMin, NUnit.Framework.Is.GreaterThanOrEqualTo(box.xMax - 0.5f), where + ": el bocadillo, a la derecha de TiTino");
            Assert.That(bubble.xMax, NUnit.Framework.Is.LessThanOrEqualTo(CanvasRect(lobby.Audio.MusicRect, lobby).xMin + 0.5f), where + ": sin llegar a Música y Sonido");
            List<KeyValuePair<string, Rect>> others = Others(lobby);
            Debug.Log("AF_HOME_SLOTH " + where + ": bocadillo " + bubble + ", toque " + tap + ", jugar " + others[2].Value);
            foreach (KeyValuePair<string, Rect> other in others)
            {
                Assert.That(Shrunk(bubble).Overlaps(Shrunk(other.Value)), NUnit.Framework.Is.False, where + ": el bocadillo tapa " + other.Key);
                Assert.That(Shrunk(tap).Overlaps(Shrunk(other.Value)), NUnit.Framework.Is.False, where + ": el toque de TiTino tapa " + other.Key);
            }

            if (lobby.StopRemainingShown)
            {
                Assert.That(Shrunk(bubble).Overlaps(Shrunk(CanvasRect(lobby.StopRemainingRect, lobby))), NUnit.Framework.Is.False, where + ": el bocadillo tapa el letrero de la parada");
            }

            Assert.That(sloth.BubbleLabel.fontSize, NUnit.Framework.Is.EqualTo(HomeSloth.BubbleTextSp * UiBuilder.TextScale).Within(0.01f), where);
            float inner = sloth.BubbleRect.rect.width - (2f * HomeSloth.BubblePadXDp);
            Assert.That(UiBuilder.MinTextHeight(sloth.BubbleLabel, inner), NUnit.Framework.Is.LessThanOrEqualTo(sloth.BubbleRect.rect.height - (2f * HomeSloth.BubblePadYDp) + 0.5f), where + ": el texto, entero");
            foreach (Graphic g in sloth.BubbleRect.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(g.raycastTarget, NUnit.Framework.Is.False, where + ": " + g.name);
            }

            foreach (Graphic g in lobby.HomeWindow.Root.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(g.raycastTarget, NUnit.Framework.Is.False, where + ": " + g.name + " (el toque es el área de TiTino)");
            }
        }

        // «Jugar», los saldos, Ajustes, Música y Sonido y lo demás del inicio que TiTino nunca tapa.
        private static List<KeyValuePair<string, Rect>> Others(LobbyScreen lobby)
        {
            var others = new List<KeyValuePair<string, Rect>>
            {
                new KeyValuePair<string, Rect>("semillas", CanvasRect(lobby.SeedsRect, lobby)),
                new KeyValuePair<string, Rect>("ajustes", CanvasRect((RectTransform)lobby.SettingsButton.transform, lobby)),
                new KeyValuePair<string, Rect>("jugar", CanvasRect((RectTransform)lobby.PlayButton.transform, lobby)),
                new KeyValuePair<string, Rect>("inicio", CanvasRect(lobby.HomeTabRect, lobby)),
                new KeyValuePair<string, Rect>("música", CanvasRect(lobby.Audio.MusicRect, lobby)),
                new KeyValuePair<string, Rect>("sonido", CanvasRect(lobby.Audio.SoundRect, lobby)),
            };
            if (lobby.CanoesShown)
            {
                others.Add(new KeyValuePair<string, Rect>("canoas", CanvasRect((RectTransform)lobby.CanoesButton.transform, lobby)));
            }

            if (lobby.ChipShown)
            {
                others.Add(new KeyValuePair<string, Rect>("chip", CanvasRect(lobby.ChipRect, lobby)));
            }

            if (lobby.MapTabButton.gameObject.activeInHierarchy)
            {
                others.Add(new KeyValuePair<string, Rect>("mapa", CanvasRect(lobby.MapTabRect, lobby)));
            }

            if (lobby.StreakWidget.Visible)
            {
                others.Add(new KeyValuePair<string, Rect>("crecida", CanvasRect(lobby.StreakWidget.Root, lobby)));
            }

            return others;
        }

        [UnityTest]
        public IEnumerator Tick_DoesNotAllocate()
        {
            ManualClock clock = At(0, 15);
            yield return Load(new MemoryProfileStore(ProfileAt(58, clock)), clock);
            HomeSloth sloth = Sloth();
            sloth.Tick(16f);

            // Un ciclo entero antes de medir (el gesto, su final, la vuelta a la pose de la hora y dos miradas a la hora): lo que
            // pase la primera vez por cada camino no cuenta.
            sloth.Tap();
            Step(sloth, 2600);
            Assert.That(sloth.Note, NUnit.Framework.Is.EqualTo(HomeSloth.HomeNote.Fact), "el dato sigue a la vista");

            void Run80()
            {
                for (int i = 0; i < 80; i++)
                {
                    sloth.Tick(16f);
                }
            }

            Assert.That(Run80, Is.Not.AllocatingGCMemory(), "80 fotogramas con la tarjeta del dato y una mirada a la hora");
            sloth.Tap();
            Assert.That(sloth.Gesturing, NUnit.Framework.Is.True);
            Assert.That(Run80, Is.Not.AllocatingGCMemory(), "80 fotogramas con sloth_look, su final y la vuelta a la pose");
            Assert.That(sloth.Gesturing, NUnit.Framework.Is.False);
        }

        [UnityTest]
        public IEnumerator Tick_DoesNotAllocate_WhileTheGameIsStillStarting()
        {
            LaunchOptions.ClockOverride = At(0, 8);
            var host = new GameObject("BootStillStarting");
            host.SetActive(false); // sin Awake ni Start: un GameBootstrap sin perfil, como mientras el juego arranca
            host.AddComponent<GameBootstrap>();
            var canvas = new GameObject("Canvas", typeof(RectTransform));
            canvas.transform.SetParent(host.transform, false);
            var art = new ProceduralArt("white");
            try
            {
                var home = new HomeWindow(canvas.transform, art);
                HomeSloth sloth = home.Sloth!;
                sloth.SetShown(true);
                sloth.BeginVisit();
                Step(sloth, 2100); // la visita pendiente en cada fotograma y dos miradas a la hora, sin el juego listo

                void Run80()
                {
                    for (int i = 0; i < 80; i++)
                    {
                        sloth.Tick(16f);
                    }
                }

                Assert.That(Run80, Is.Not.AllocatingGCMemory(), "80 fotogramas con el arranque sin terminar (y una mirada a la hora)");
                Assert.That(sloth.GreetingDecisions, NUnit.Framework.Is.EqualTo(0), "sin el juego listo, el saludo espera");
                Assert.That(sloth.Shown, NUnit.Framework.Is.True);
            }
            finally
            {
                UnityEngine.Object.Destroy(host);
                art.Dispose();
            }

            yield return null;
        }

        private static void AssertHidden(LobbyScreen lobby, string what)
        {
            HomeSloth sloth = lobby.HomeWindow.Sloth!;
            Assert.That(lobby.HomeWindow.Visible, NUnit.Framework.Is.False, what);
            Assert.That(sloth.Shown, NUnit.Framework.Is.False, what);
            Assert.That(sloth.BubbleShown, NUnit.Framework.Is.False, what + ": sin bocadillo");
            Assert.That(sloth.BubbleRect.gameObject.activeInHierarchy, NUnit.Framework.Is.False, what);
            Assert.That(sloth.Note, NUnit.Framework.Is.EqualTo(HomeSloth.HomeNote.None), what + ": sin mensaje");
            Assert.That(sloth.TapButton.gameObject.activeInHierarchy, NUnit.Framework.Is.False, what + ": sin toque");
            Assert.That(sloth.Rig == null || !sloth.Rig.Root.gameObject.activeInHierarchy, NUnit.Framework.Is.True, what + ": sin TiTino");
            Assert.That(sloth.Tap(), NUnit.Framework.Is.False, what);
        }

        private static ManualClock At(int dayOffset, int hour)
        {
            return new ManualClock(Day0.AddDays(dayOffset).AddHours(hour));
        }

        private static string ManualDateKey(int dayOffset)
        {
            return ClockExtensions.FormatDateKey(Day0.AddDays(dayOffset));
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

        private static Profile Saved(MemoryProfileStore store)
        {
            return store.Load().Profile!;
        }

        private static void Step(HomeSloth sloth, int ms)
        {
            for (int t = 0; t < ms; t += 16)
            {
                sloth.Tick(16f);
            }
        }

        private static Rect Grow(Rect r, float d)
        {
            return Rect.MinMaxRect(r.xMin - d, r.yMin - d, r.xMax + d, r.yMax + d);
        }

        private static Rect Shrunk(Rect r)
        {
            return Rect.MinMaxRect(r.xMin + 0.5f, r.yMin + 0.5f, r.xMax - 0.5f, r.yMax - 0.5f);
        }

        private static Rect CanvasRect(RectTransform rt, LobbyScreen lobby)
        {
            var root = (RectTransform)lobby.Canvas.transform;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Rect bounds = root.rect;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = root.InverseTransformPoint(corners[i]);
                float x = local.x - bounds.xMin;
                float y = bounds.yMax - local.y;
                xMin = Mathf.Min(xMin, x);
                xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y);
                yMax = Mathf.Max(yMax, y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
