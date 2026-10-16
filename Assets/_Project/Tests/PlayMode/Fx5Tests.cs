#nullable enable
using System.Collections;
using System.Globalization;
using AF.GameFlow;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Arreglos de la prueba de la ola 3 de la tanda 4 en el teléfono (<c>docs/dev/traspaso/pruebas-telefono-2026-10-15-tanda4-ola3.md</c>;
    /// tarea FX5), con la fuente al 100 % y al 130 % (02 §12), en es-419 y en en-XA (D-018):
    /// <list type="bullet">
    /// <item><description>defecto 1: en Aguas estancadas del 40 sin semillas salen Puñado y Balsa con la línea «Todo para seguir:
    /// …» (<c>balsa</c> y <c>balsa_note</c> en <c>AF_LAYOUT stuck</c>), sin desbordes, sin palabras partidas, sin pisarse y con
    /// el panel dentro de la pantalla. El modal no depende de la escala de texto para ofrecerla: la Balsa falta solo si la
    /// tienda no la deja ofrecer hoy (<c>af_rescue_daily_cap</c>: 1 impresión al día; 04 §6.2), y la línea <c>AF_STATE
    /// stuck_modal</c> lo dice (<see cref="Balsa_SecondBlockTheSameDay_NotOffered_TheProbeSaysWhy"/>);</description></item>
    /// <item><description>defecto 2: la píldora «Reiniciar» del HUD nunca parte una palabra («Reinici / ar» en el 5, el 8 y el
    /// 40 al 130 %): crece y estrecha la columna del saldo, cuyas cifras siguen cabiendo;</description></item>
    /// <item><description>observación 3: el nombre del miquito de la tarjeta «¡Miquito a la vista!» del 5 sale de la tabla de
    /// textos y en en-XA va pseudolocalizado («squirrel monkey» salía tal cual).</description></item>
    /// </list>
    /// No necesitan gráficos. Capturas: <see cref="Fx5SnapshotTests"/>.
    /// </summary>
    public sealed class Fx5Tests
    {
        private AfLayoutLog? _layout;

        [SetUp]
        public void SetUp()
        {
            _layout = new AfLayoutLog();
        }

        [TearDown]
        public void TearDown()
        {
            _layout?.Dispose();
            PseudoLocWalk.Release();
        }

        [UnityTest]
        public IEnumerator Stuck40_NoSeeds_HandfulAndBalsaWithItsLine(
            [Values("es-419", "en-XA")] string locale, [Values(100, 130)] int percent, [Values(780, 640)] int screen)
        {
            float scale = percent / 100f;
            string what = "Aguas estancadas del 40 sin semillas en " + locale + " al " + percent + " % (" + screen + ")";
            _layout!.Clear();
            yield return Fx5Setup.LoadStuck40(locale, scale, Fx5Setup.Screen(screen));
            GameScreen view = GameBootstrap.Current!.View!;
            StuckModal stuck = view.Stuck;

            string line = _layout.RequireLast("stuck", 40, what);
            string[] ids = AfLayoutLog.ButtonIds(line);
            Assert.That(ids, Does.Contain("handful").And.Contain("balsa").And.Contain("balsa_note"), what + ": " + line);
            AfLayoutLog.AssertCenterInside(line, "handful", (RectTransform)stuck.HandfulButton.transform);
            AfLayoutLog.AssertCenterInside(line, "balsa", (RectTransform)stuck.BalsaButton.transform);
            Assert.That(stuck.BalsaNote.text, Is.EqualTo(UiText.StuckBalsaContent), what + ": la línea de la Balsa");

            var inventory = new LocInventory(Loc.Locale, scale);
            foreach (TextMeshProUGUI label in new[] { stuck.HandfulLabel, stuck.BalsaLabel, stuck.BalsaNote })
            {
                Assert.That(inventory.ProblemsOf(label), Is.Empty, what + ": «" + label.text + "»");
            }

            // Con lo que publica la sonda (píxeles de la pantalla, con el origen arriba a la izquierda, como los lee adb): todo lo
            // del modal dentro de la pantalla y sin pisarse.
            Vector4 px = Fx5Setup.Screen(screen);
            for (int i = 0; i < ids.Length; i++)
            {
                RectInt r = AfLayoutLog.Button(line, ids[i])!.Value;
                Assert.That(r.xMin >= 0 && r.yMin >= 0 && r.xMax <= px.x && r.yMax <= px.y, Is.True, what + ": " + ids[i] + " " + r + " dentro de la pantalla");
                for (int j = i + 1; j < ids.Length; j++)
                {
                    RectInt o = AfLayoutLog.Button(line, ids[j])!.Value;
                    bool overlap = r.xMin < o.xMax && o.xMin < r.xMax && r.yMin < o.yMax && o.yMin < r.yMax;
                    Assert.That(overlap, Is.False, what + ": " + ids[i] + " " + r + " pisa " + ids[j] + " " + o);
                }
            }

            RectInt handful = AfLayoutLog.Button(line, "handful")!.Value;
            RectInt balsa = AfLayoutLog.Button(line, "balsa")!.Value;
            Debug.Log("AF_FX5 " + what + ": " + (handful.y != balsa.y ? "una debajo de otra" : "en una fila") + "; panel de "
                + stuck.Height.ToString("0.0", CultureInfo.InvariantCulture) + " dp; " + line);
        }

        /// <summary>
        /// Lo que pasó en el teléfono, según la tienda (04 §6.2; <c>af_rescue_daily_cap</c> = 1 en 08 §9): la Balsa se enseña en
        /// el primer bloqueo del día; en otro bloqueo del mismo día ya no se ofrece, con la escala que sea, y la línea
        /// <c>AF_STATE stuck_modal</c> lo dice (<c>balsa_ok</c> false con <c>balsa_shown_today</c> 1). La misma partida al día
        /// siguiente la vuelve a ofrecer.
        /// </summary>
        [UnityTest]
        public IEnumerator Balsa_SecondBlockTheSameDay_NotOffered_TheProbeSaysWhy()
        {
            var states = new System.Collections.Generic.List<string>();
            Application.LogCallback onLog = (condition, stack, type) =>
            {
                if (condition.StartsWith("AF_STATE {\"event\":\"stuck_modal\"", System.StringComparison.Ordinal))
                {
                    states.Add(condition);
                }
            };
            Application.logMessageReceived += onLog;
            try
            {
                var store = new MemoryProfileStore(FunnelSetup.Profile(0, 3));
                yield return Fx5Setup.LoadStuck40(LocLocale.SourceLocale, 1f, HudLabelTests.Screen780, store);
                Assert.That(GameBootstrap.Current!.View!.Stuck.BalsaButton.gameObject.activeInHierarchy, Is.True, "el primer bloqueo del día ofrece la Balsa");
                Assert.That(states, Has.Count.EqualTo(1));
                Assert.That(states[0], Does.Contain("\"balsa_ok\":true").And.Contain("\"balsa_shown_today\":1").And.Contain("\"balsa_hidden\":false"), states[0]);

                // Otro bloqueo el mismo día (otra partida del 40 con la impresión de hoy ya contada), ahora al 130 %: la tienda ya
                // no la deja ofrecer, y no es por la escala.
                yield return Fx5Setup.LoadStuck40(LocLocale.SourceLocale, 1.3f, HudLabelTests.Screen780, new MemoryProfileStore(ShownToday()), false);
                StuckModal stuck = GameBootstrap.Current!.View!.Stuck;
                Assert.That(stuck.BalsaButton.gameObject.activeInHierarchy, Is.False, "af_rescue_daily_cap = 1: la segunda vez del día, sin Balsa");
                Assert.That(stuck.BalsaNote.gameObject.activeInHierarchy, Is.False, "ni su línea");
                Assert.That(stuck.HandfulButton.gameObject.activeInHierarchy, Is.True, "el Puñado sí");
                Assert.That(states, Has.Count.EqualTo(2));
                Assert.That(states[1], Does.Contain("\"balsa_ok\":false").And.Contain("\"balsa_shown_today\":1"), states[1]);

                // Al día siguiente (cambia el día local), otra vez.
                Profile yesterday = ShownToday();
                FunnelSetup.Clock.Advance(System.TimeSpan.FromDays(1));
                yield return Fx5Setup.LoadStuck40(LocLocale.SourceLocale, 1.3f, HudLabelTests.Screen780, new MemoryProfileStore(yesterday), false);
                Assert.That(GameBootstrap.Current!.View!.Stuck.BalsaButton.gameObject.activeInHierarchy, Is.True, "al día siguiente vuelve la Balsa");
                Assert.That(states[states.Count - 1], Does.Contain("\"balsa_ok\":true"), states[states.Count - 1]);
            }
            finally
            {
                Application.logMessageReceived -= onLog;
            }
        }

        // Perfil sin semillas con la Balsa ya enseñada hoy (el día local del reloj de los tests).
        private static Profile ShownToday()
        {
            Profile profile = FunnelSetup.Profile(0, 3);
            profile.Daily.DateLocal = FunnelSetup.Clock.LocalDateKey();
            profile.Daily.AfRescueShown = 1;
            return profile;
        }

        [UnityTest]
        public IEnumerator RestartPill_NeverBreaksAWord_TheWalletDigitsStillFit(
            [Values(5, 8, 40)] int level, [Values("es-419", "en-XA")] string locale, [Values(100, 130)] int percent, [Values(780, 640)] int screen)
        {
            float scale = percent / 100f;
            yield return Fx5Setup.LoadLevel(level, locale, scale, Fx5Setup.Screen(screen), 12345);
            GameScreen view = GameBootstrap.Current!.View!;
            TextMeshProUGUI label = view.RestartLabel;
            string what = "Reiniciar en el " + level + " en " + locale + " al " + percent + " % (" + screen + ")";
            Debug.Log("AF_FX5 medidas " + what + ": palabra más ancha " + UiBuilder.LongestWordWidth(label).ToString("0.0", CultureInfo.InvariantCulture)
                + " dp, etiqueta " + UiBuilder.MinTextWidth(label).ToString("0.0", CultureInfo.InvariantCulture) + " dp, cabecera mínima "
                + view.WalletBar.CompactMinWidthDp.ToString("0.0", CultureInfo.InvariantCulture) + " dp, lienzo " + view.CanvasWidth.ToString("0.0", CultureInfo.InvariantCulture));
            Assert.That(LocInventory.BrokenWordLine(label), Is.EqualTo(0), what + ": «" + label.text + "» en " + label.textInfo.lineCount + " líneas");
            Assert.That(label.textInfo.lineCount, Is.InRange(1, 2), what);
            if (locale == LocLocale.SourceLocale)
            {
                Assert.That(label.textInfo.lineCount, Is.EqualTo(1), what + ": «Reiniciar» en una línea");
            }

            LongLabelSetup.AssertRestart(view, scale, label.textInfo.lineCount, false);

            // Las cifras de la columna del saldo, que la píldora puede estrechar, siguen cabiendo a 12 sp (× escala) o más.
            var inventory = new LocInventory(Loc.Locale, scale);
            int seen = 0;
            foreach (RectTransform pill in new[] { view.WalletBar.SeedsRect, view.WalletBar.CanoesRect })
            {
                if (!pill.gameObject.activeInHierarchy)
                {
                    continue;
                }

                foreach (TMP_Text text in pill.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (string.IsNullOrEmpty(text.text))
                    {
                        continue;
                    }

                    seen++;
                    Assert.That(inventory.ProblemsOf(text), Is.Empty, what + ": la cifra «" + text.text + "» de la cabecera");
                }
            }

            Assert.That(seen, Is.GreaterThan(0), what + ": la cabecera enseña las semillas");
            Debug.Log("AF_FX5 " + what + ": «" + label.text + "» en " + label.textInfo.lineCount + " líneas a " + label.fontSize.ToString("0.0", CultureInfo.InvariantCulture)
                + " dp; píldora de " + view.RestartPillRect.rect.width.ToString("0.0", CultureInfo.InvariantCulture) + " dp"
                + (float.IsNaN(view.RestartPillLeftDp) ? string.Empty : ", estrecha la cabecera hasta x = " + view.RestartPillLeftDp.ToString("0.0", CultureInfo.InvariantCulture))
                + "; cabecera de " + view.WalletBar.SeedsRect.rect.width.ToString("0.0", CultureInfo.InvariantCulture) + " dp");
        }

        [UnityTest]
        public IEnumerator Sighting5_TheNameComesFromTheTable_PseudolocalizedInEnXA([Values("es-419", "en-XA")] string locale)
        {
            yield return Fx5Setup.LoadSighting5(locale, 1f, HudLabelTests.Screen780);
            SightingCard card = GameBootstrap.Current!.View!.Sighting;
            Assert.That(card.Visible, Is.True);
            if (locale == LocLocale.Pseudo)
            {
                Assert.That(card.Name, Does.StartWith("[").And.EndWith("]"), "en-XA: «" + card.Name + "»");
                Assert.That(card.Name, Does.Not.Contain("squirrel monkey").And.Not.Contain("mono ardilla"), "en-XA: «" + card.Name + "»");
                Assert.That(card.Fact, Does.StartWith("["), "el dato, también");
            }
            else
            {
                Assert.That(card.Name, Is.EqualTo("mono ardilla"));
            }

            Assert.That(card.Name, Is.EqualTo(UiText.FaunaName("fauna.saimiri_collinsi.name")));
        }
    }

    /// <summary>
    /// Capturas de FX5 al 130 % (<c>Logs/snapshots/&lt;idioma&gt;/fx5_130_*.png</c>), en es-419 y en-XA, en el teléfono de
    /// la prueba (1080 × 2340 px, 360 × 780 dp) y en 16:9 (1080 × 1920, con el sufijo <c>_16x9</c>): la tarjeta del miquito
    /// del 5 (solo en el teléfono: en 16:9 al 130 % no cabe y no sale), el tablero del 8 y del 40 con la píldora «Reiniciar» y
    /// Aguas estancadas del 40 sin semillas, con Puñado, Balsa y su línea. Explícito: necesita
    /// gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.Fx5SnapshotTests</c> sin
    /// <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de FX5; necesitan dispositivo gráfico.")]
    public sealed class Fx5SnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            PseudoLocWalk.Release();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator Capture_130([Values("es-419", "en-XA")] string locale, [Values(780, 640)] int screen)
        {
            string dir = SnapshotKit.Prepare(locale);
            string suffix = screen == 780 ? string.Empty : "_16x9";
            if (screen == 780)
            {
                // En 16:9 al 130 % la tarjeta no cabe sin tapar cuias ni botones y no sale (GameScreen.ShowSightingNow).
                yield return Fx5Setup.LoadSighting5(locale, 1.3f, Fx5Setup.Screen(screen));
                yield return Take(dir, "fx5_130_sighting_l5");
            }

            foreach (int level in new[] { 8, 40 })
            {
                yield return Fx5Setup.LoadLevel(level, locale, 1.3f, Fx5Setup.Screen(screen), 12345);
                yield return Take(dir, "fx5_130_tablero_l" + level.ToString(CultureInfo.InvariantCulture) + suffix);
            }

            yield return Fx5Setup.LoadStuck40(locale, 1.3f, Fx5Setup.Screen(screen));
            yield return Take(dir, "fx5_130_estancadas_l40" + suffix);
        }

        private static IEnumerator Take(string dir, string name)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            LongLabelSetup.Log(boot.View!, name);
            SnapshotKit.Save(dir, name);
        }
    }

    /// <summary>Cargas de <see cref="Fx5Tests"/> y <see cref="Fx5SnapshotTests"/>.</summary>
    internal static class Fx5Setup
    {
        /// <summary>El teléfono de la prueba (780: 1080 × 2340 px) o la pantalla 16:9 de 02 §5 (640: 1080 × 1920 px).</summary>
        public static Vector4 Screen(int screen)
        {
            return screen == 640 ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
        }

        /// <summary>
        /// Aguas estancadas del 40 (tablero 6 del tutorial con af_level_offset = 34) sin semillas: rescate en gris, Puñado y
        /// Balsa (rama B), como en el teléfono, en <paramref name="locale"/> con la fuente a <paramref name="scale"/>. Con
        /// <paramref name="store"/>, el perfil de ese almacén (para varios bloqueos el mismo día); con
        /// <paramref name="hooks"/> en false, sin rehacer los ganchos (el mismo reloj).
        /// </summary>
        public static IEnumerator LoadStuck40(string locale, float scale, Vector4 screen, MemoryProfileStore? store = null, bool hooks = true)
        {
            Hooks(locale, scale, screen, hooks);
            LaunchOptions.LevelOffsetOverride = 34;
            LaunchOptions.LevelCardsOverride = false;
            LaunchOptions.ProfileStoreOverride = store ?? new MemoryProfileStore(FunnelSetup.Profile(0, 3));
            yield return PlayModeHelpers.LoadGame(40, new MemoryProgressStore(40, 39));
            yield return PseudoLocWalk.StuckRealTime(GameBootstrap.Current!);
        }

        /// <summary>El nivel <paramref name="level"/> en juego con <paramref name="seeds"/> semillas, como <c>LongLabelSetup.LoadLevel</c>.</summary>
        public static IEnumerator LoadLevel(int level, string locale, float scale, Vector4 screen, int seeds)
        {
            Hooks(locale, scale, screen, true);
            LaunchOptions.LevelCardsOverride = false;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.TapAnywhere();
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.Phase == FlowPhase.Playing && boot.View!.IsViewIdle, "el " + level + " en juego");
            yield return SnapshotKit.Wait(0.3f);
        }

        /// <summary>
        /// El 5 con su intro entera hasta que sale la tarjeta «¡Miquito a la vista!» del mono ardilla y entra (400 ms), en
        /// <paramref name="locale"/> con la fuente a <paramref name="scale"/> (como NoticeSnapshotTests).
        /// </summary>
        public static IEnumerator LoadSighting5(string locale, float scale, Vector4 screen)
        {
            Hooks(locale, scale, screen, true);
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(5, new MemoryProgressStore(5, 4));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.Flow!.Load(5);
            yield return null; // las vistas de la primera carga se destruyen al final del fotograma
            for (int t = 0; t < 8000 && !boot.View!.Sighting.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.View!.Sighting.Visible, Is.True, "la tarjeta del miquito en el 5");
            PlayModeHelpers.Step(boot, 400);
        }

        private static void Hooks(string locale, float scale, Vector4 screen, bool reset)
        {
            if (reset)
            {
                FunnelSetup.Hooks();
            }

            LaunchOptions.LanguageOverride = locale == LocLocale.SourceLocale ? null : locale;
            if (Loc.IsConfigured && Loc.Locale != locale)
            {
                Loc.SetLocale(locale);
            }

            LaunchOptions.FontScaleOverride = scale;
            GameScreen.SimulatedScreen = screen;
        }
    }
}
