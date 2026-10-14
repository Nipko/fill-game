#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AF.Ads;
using AF.Boosters;
using AF.Core.Board;
using AF.Economy;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
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
    /// Luciérnaga · Revelar en la escena (tarea T14 del paso 1; 04 §2.5; 07 §7.4 y §7.6; 02 §5): el regalo del 100 con su aviso
    /// (al habilitarse el toque, tras la tarjeta previa, si la hay, y la intro: D-011, punto 2), la bandeja de 3 ranuras de 64 dp de los niveles turbios, el botón en
    /// AF_LAYOUT, el modo de selección (solo laten las turbias), el revelado con el stock, con semillas y con el video, el aviso
    /// sin saldo ni video, la cancelación sin coste, la ranura que desaparece sin ocultas y lo revelado que sobrevive a Marea, a
    /// Volver a la orilla y a reabrir (con alguna oculta, las 3 ranuras desde el primer fotograma y en AF_LAYOUT). Con el
    /// catálogo real: nivel 100, cuia 0 = [10*, 8*, 6] y cuia 1 = [6]; nivel 106, cuia 6 = [14*, 1].
    /// </summary>
    public sealed class LuciernagaTests
    {
        private const int Level100 = 100;
        private const int MurkyCuia100 = 0;
        private const int Level106 = 106;
        private const int MurkyCuia106 = 6;

        private readonly List<string> _lines = new List<string>();

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            _lines.Clear();
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Gift_Level100_ToastAndStock()
        {
            // Perfil nuevo y tarjetas apagadas (el gancho de los tests): el aviso espera a que se habilite el toque (D-011, punto 2).
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(Level100, new MemoryProgressStore(Level100, Level100 - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.LevelCardsEnabled, Is.False);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Reveal), Is.EqualTo(1), "04 §2.5: 1 Luciérnaga al stock");
            Assert.That(view.ToastText, Is.Null, "«+1 Luciérnaga» sin tarjeta: no durante la intro");
            Assert.That(view.RevealGiftPending, Is.True);
            Assert.That(HasEvent("\"phase\":\"gift\""), Is.True, "DevProbe.Event(\"reveal\") del regalo");
            PlayModeHelpers.Step(boot, 2 * PlayModeHelpers.FrameMs);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(view.ToastText, Is.Null, "aún en la intro");
            boot.TapAnywhere();
            Assert.That(view.ToastText, Is.EqualTo(UiText.RevealGiftToast), "al habilitarse el toque, «+1 Luciérnaga»");
            Assert.That(view.RevealGiftPending, Is.False);
            Assert.That(HasEvent("\"phase\":\"gift_toast\""), Is.True);
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.Tray.RevealShown, Is.True);
            Assert.That(view.Tray.RevealBadgeText, Is.EqualTo("+1"), "«Revelar +1»");

            // Tarjetas encendidas (las enciende T13; aquí, el gancho): el aviso espera a la tarjeta combinada del 100 y a la intro.
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(Level100, new MemoryProgressStore(Level100, Level100 - 1));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelCard), "la tarjeta previa del 100");
            Assert.That(boot.Flow.LevelCard!.RevealGifted, Is.True, "la tarjeta enseña «+1 Luciérnaga»");
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Reveal), Is.EqualTo(1));
            Assert.That(view.ToastText, Is.Null, "con la tarjeta abierta, el aviso no sale");
            Assert.That(view.RevealGiftPending, Is.True);
            PlayModeHelpers.Step(boot, 600);
            Assert.That(view.ToastText, Is.Null);
            Assert.That(boot.TapAnywhere(), Is.True, "un toque cierra la tarjeta");
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(view.ToastText, Is.Null, "al cerrarla empieza la intro: el aviso sigue esperando (D-011, punto 2)");
            Assert.That(view.RevealGiftPending, Is.True);
            PlayModeHelpers.Step(boot, 2 * PlayModeHelpers.FrameMs);
            Assert.That(view.ToastText, Is.Null);
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.ToastText, Is.EqualTo(UiText.RevealGiftToast), "con el toque habilitado, «+1 Luciérnaga»");
            Assert.That(view.RevealGiftPending, Is.False);
        }

        [UnityTest]
        public IEnumerator Tray_ThreeSlots64dp_OnlyMurky()
        {
            yield return LoadPlaying(Level100, FunnelSetup.Profile(0));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            BoosterTray tray = view.Tray;
            Assert.That(tray.SlotCount, Is.EqualTo(3), "Marea, Cuia extra y Revelar");
            Assert.That(tray.Width, Is.EqualTo(240f).Within(0.01f), "16 + 3·64 + 2·8 + 16");
            RectTransform undo = (RectTransform)tray.UndoButton.transform;
            RectTransform extra = (RectTransform)tray.ExtraCuiaButton.transform;
            RectTransform reveal = (RectTransform)tray.RevealButton.transform;
            Assert.That(reveal.gameObject.activeInHierarchy, Is.True);
            foreach (RectTransform slot in new[] { undo, extra, reveal })
            {
                Assert.That(slot.rect.width, Is.EqualTo(64f).Within(0.01f), slot.name + ": 64 dp (07 §7.4: con 5 ranuras, 56)");
                Assert.That(slot.rect.height, Is.GreaterThanOrEqualTo(48f));
            }

            // Orden canónico (07 §7.4), Revelar la última a la derecha, sin solapes y con 8 dp entre ranuras; bandeja centrada.
            Assert.That(extra.anchoredPosition.x - (undo.anchoredPosition.x + 64f), Is.EqualTo(8f).Within(0.01f));
            Assert.That(reveal.anchoredPosition.x - (extra.anchoredPosition.x + 64f), Is.EqualTo(8f).Within(0.01f));
            Assert.That(undo.anchoredPosition.x, Is.EqualTo(((view.CanvasWidth - 240f) * 0.5f) + 16f).Within(0.01f));
            Assert.That(WorldRect(undo).Overlaps(WorldRect(extra)) || WorldRect(extra).Overlaps(WorldRect(reveal)), Is.False);
            Assert.That(tray.RevealGlyph.sprite.name, Is.EqualTo("icon_firefly"), "07 §7.6: la luciérnaga encendida sobre una cuia oscura");
            Assert.That(reveal.GetComponentInChildren<TMPro.TextMeshProUGUI>().text, Is.EqualTo(UiText.RevealLabel), "«Revelar» (glosario §5.1)");

            // Un nivel sin Aguas turbias (101): sin ranura y con la bandeja de 2, centrada.
            yield return LoadPlaying(101, FunnelSetup.Profile(0));
            view = GameBootstrap.Current!.View!;
            Assert.That(view.Tray.RevealShown, Is.False);
            Assert.That(view.Tray.SlotCount, Is.EqualTo(2));
            Assert.That(view.Tray.RevealButton.gameObject.activeInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator Layout_EmitsRevealRect()
        {
            Assert.That(DevProbe.Enabled, Is.True);
            yield return LoadPlaying(Level100, FunnelSetup.Profile(0));
            GameBootstrap boot = GameBootstrap.Current!;
            string? layout = LastLayout(Level100);
            Assert.That(layout, Is.Not.Null);
            Match m = Regex.Match(layout!, "\\{\"id\":\"reveal\",\"x\":(-?\\d+),\"y\":(-?\\d+),\"w\":(\\d+),\"h\":(\\d+)\\}");
            Assert.That(m.Success, Is.True, "AF_LAYOUT con «reveal»: " + layout);
            Rect world = WorldRect((RectTransform)boot.View!.Tray.RevealButton.transform);
            Assert.That(int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture), Is.EqualTo(Mathf.RoundToInt(world.width)).Within(1), "su rectángulo");
            Assert.That(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), Is.EqualTo(Mathf.RoundToInt(world.xMin)).Within(1));
            Assert.That(layout, Does.Contain("\"id\":\"undo\"").And.Contain("\"id\":\"extra_cuia\""));

            // Al revelar la última oculta la ranura desaparece y AF_LAYOUT se vuelve a emitir sin ella; mientras la luciérnaga
            // vuela (200 ms de un vuelo de 520 × factor), la ranura sigue y AF_LAYOUT no cambia.
            int before = LayoutCount(Level100);
            Assert.That(boot.PressReveal(), Is.True);
            boot.TapCuia(MurkyCuia100);
            PlayModeHelpers.Step(boot, 200);
            GameScreen view = boot.View!;
            Assert.That(view.RevealFirefly.Flying, Is.True, "a los 200 ms aún vuela");
            Assert.That(view.Tray.RevealShown, Is.True, "la ranura sigue mientras la luciérnaga vuela");
            Assert.That(LayoutCount(Level100), Is.EqualTo(before), "sin volver a emitir AF_LAYOUT mientras vuela");
            StepUntilRevealDone(boot, 5000);
            Assert.That(LayoutCount(Level100), Is.GreaterThan(before), "se vuelve a emitir al desaparecer");
            Assert.That(LastLayout(Level100), Does.Not.Contain("\"id\":\"reveal\""));
        }

        [UnityTest]
        public IEnumerator Select_PulsesMurkyOnly()
        {
            yield return LoadPlaying(Level100, FunnelSetup.Profile(0));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            BoardState board = boot.Flow!.Session.Current;
            Assert.That(view.RevealPulse.Active, Is.False, "D-002: fuera del modo de selección nada late");

            // Una cuia elevada: abrir la selección primero la baja (C8).
            Assert.That(boot.TapCuia(1).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.PressReveal(), Is.True);
            Assert.That(boot.Flow.RevealSelecting, Is.True);
            PlayModeHelpers.Step(boot, 160);
            for (int i = 0; i < board.Count; i++)
            {
                bool murky = board[i].HiddenMask != 0;
                Assert.That(view.RevealPulse.IsPulsing(i), Is.EqualTo(murky), "cuia " + i + ": solo laten las turbias (04 §2.5)");
                Assert.That(view.Views[i].IsLifted, Is.False, "cuia " + i + ": nada se eleva");
                Assert.That(view.Views[i].GlassBoosted, Is.False, "cuia " + i + ": ningún brillo de selección");
            }

            Image halo = view.RevealPulse.HaloOf(MurkyCuia100)!;
            Assert.That(halo.transform.IsChildOf(view.MechanicFrontLayer), Is.True, "en la capa de mecánicas delantera");

            // El aro rodea la boca y los lados de la cuia y baja hasta su pie, abierto por abajo: no cruza la repisa ni la
            // insignia «?×2» (revisión de la captura 66). Se mide sin la escala del latido.
            Vector3 beat = halo.rectTransform.localScale;
            halo.rectTransform.localScale = Vector3.one;
            Rect ring = WorldRect(halo.rectTransform);
            Rect body = WorldRect(view.Views[MurkyCuia100].Body);
            Rect chip = WorldRect(view.Views[MurkyCuia100].Murky.Badge.rectTransform);
            halo.rectTransform.localScale = beat;
            Assert.That(ring.xMin < body.xMin && ring.xMax > body.xMax && ring.yMax > body.yMax, Is.True, "el aro rodea la boca y los lados");
            Assert.That(ring.yMin, Is.EqualTo(body.yMin).Within(0.5f), "abierto por abajo, hasta el pie");
            Assert.That(ring.yMin, Is.GreaterThanOrEqualTo(chip.yMax - 0.5f), "sin bajar a la insignia «?×2»");
            Assert.That(view.Tray.RevealPressed, Is.True, "el botón queda pulsado mientras se elige");
            Assert.That(view.Tray.RevealRelief.Pressed, Is.True, "la cara del botón, abajo (ReliefButton)");
            view.Tray.RevealRelief.Step(200f); // la animación de la cara la avanza Update; aquí, a mano
            Assert.That(view.Tray.RevealRelief.FaceOffsetDp, Is.GreaterThan(0f), "la cara bajó");

            // Late: el alfa cambia de la cresta al valle.
            float crest = view.RevealPulse.Alpha;
            PlayModeHelpers.TickExact(boot, (int)(RevealPulse.PeriodMs * 0.5f) - 160);
            Assert.That(view.RevealPulse.Alpha, Is.LessThan(crest - 0.3f), "el aro late");
            Assert.That(boot.Flow.CancelRevealSelection(), Is.True);
            PlayModeHelpers.Step(boot, 32);
            Assert.That(view.RevealPulse.Active, Is.False);
            Assert.That(view.Tray.RevealPressed, Is.False);
            Assert.That(view.Tray.RevealRelief.Pressed, Is.False, "al cancelar, la cara sube");
            for (int i = 0; i < board.Count; i++)
            {
                Assert.That(view.RevealPulse.IsPulsing(i), Is.False);
            }
        }

        [UnityTest]
        public IEnumerator Reveal_Stock_Level100()
        {
            yield return LoadPlaying(Level100, FunnelSetup.Profile(0));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            CuiaView cuia = view.Views[MurkyCuia100];
            int mask = boot.Flow!.Session.Current[MurkyCuia100].HiddenMask;
            Assert.That(mask, Is.EqualTo(0b11));
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Reveal), Is.EqualTo(1));
            Assert.That(view.RevealFirefly.BodySprite, Is.Null, "cuerpo y alas, perezosos: hasta el primer vuelo no se piden");
            Assert.That(view.RevealFirefly.WingsSprite, Is.Null);
            Assert.That(boot.PressReveal(), Is.True);
            Assert.That(boot.Flow.RevealSelectionPayment, Is.EqualTo(RevealPayment.Stock));
            int discarded = view.Fx.DiscardedCount;
            Assert.That(boot.TapCuia(MurkyCuia100).Kind, Is.EqualTo(TapKind.Ignored), "el toque elige la cuia");

            // El modelo ya está revelado, guardado y cobrado; la vista lo enseña con la luciérnaga.
            Assert.That(boot.Flow.Session.Current[MurkyCuia100].HiddenMask, Is.EqualTo(0));
            Assert.That(boot.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(0), "se gastó la regalada");
            Assert.That(view.RevealAnimating, Is.True);
            Assert.That(view.RevealFirefly.Flying, Is.True, "sale del botón");
            Assert.That(view.RevealFirefly.BodySprite!.name, Is.EqualTo("firefly_body"), "el primer vuelo pide el cuerpo");
            Assert.That(view.RevealFirefly.WingsSprite!.name, Is.EqualTo("firefly_wings"));
            Assert.That(cuia.ShownCuia.HiddenMask, Is.EqualTo(mask), "mientras vuela, la cuia sigue turbia");
            Assert.That(HasEvent("\"phase\":\"revealed\""), Is.True);
            Rect button = WorldRect((RectTransform)view.Tray.RevealButton.transform);
            Vector3 start = view.RevealFirefly.Root.position;
            Assert.That(start.x, Is.InRange(button.xMin, button.xMax), "sale del botón");

            int maxParticles = 0;
            int flight = 0;
            while (!view.RevealMarksStarted)
            {
                Assert.That(flight, Is.LessThan(2000), "la luciérnaga se posa");
                PlayModeHelpers.TickExact(boot, 4);
                flight += 4;
                maxParticles = Mathf.Max(maxParticles, view.Fx.ActiveCount);
            }

            float speed = view.Animator.StepSpeedBase;
            Assert.That(flight, Is.EqualTo(RevealFireflyFx.FlightMs * speed).Within(8f), "vuelo de 520 ms × factor [P]");
            Assert.That(cuia.Murky.IsAnimating, Is.True);
            Assert.That(cuia.Murky.Style, Is.EqualTo(RevealStyle.Firefly), "la luz baja por el vidrio (T15)");
            Vector2 rim = cuia.FootPosition + cuia.BodyOffset + new Vector2(0f, cuia.HeightU * cuia.Unit);
            Assert.That(Vector2.Distance(view.RevealFirefly.Position, rim), Is.LessThan(0.8f * cuia.Unit), "posada en el borde de la boca");

            int reveal = 0;
            while (cuia.Murky.IsAnimating)
            {
                Assert.That(reveal, Is.LessThan(2000));
                PlayModeHelpers.TickExact(boot, 4);
                reveal += 4;
                maxParticles = Mathf.Max(maxParticles, view.Fx.ActiveCount);
            }

            Assert.That(reveal, Is.EqualTo(600f * speed).Within(8f), "04 §2.5: el pardo se aclara en 0,6 s × factor");
            Assert.That(view.Animator.ShownOf(MurkyCuia100).HiddenMask, Is.EqualTo(0), "HiddenMask 0 en la vista");
            StepUntilRevealDone(boot, 2000);
            Assert.That(maxParticles, Is.InRange(1, FxLayer.MaxParticles), "estela del pool común (≤ 44)");
            Assert.That(view.RevealFirefly.SparklesRequested, Is.GreaterThan(0));
            Assert.That(view.Fx.DiscardedCount, Is.EqualTo(discarded), "ningún destello descartado por los topes");
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(cuia.Murky.BadgeCount, Is.EqualTo(0), "sin «?×n»");
            Assert.That(view.Tray.RevealShown, Is.False, "sin ocultas, la ranura se va");
        }

        [UnityTest]
        public IEnumerator Reveal_Seeds_Level106()
        {
            var store = new MemoryProfileStore(FunnelSetup.Profile(500));
            LaunchOptions.ProfileStoreOverride = store;
            yield return LoadPlayingWithStore(Level106);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Level.IsMurky, Is.True);
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Reveal), Is.EqualTo(0), "sin regalo fuera del estreno");
            Assert.That(view.ToastText, Is.Null);
            Assert.That(view.Tray.RevealPriceShown, Is.EqualTo(120), "sin stock, el precio (reveal_seed_price)");
            Assert.That(view.Tray.RevealShowsMissing, Is.False);
            Assert.That(view.Tray.RevealShowsVideo, Is.False, "con saldo, el botón paga con semillas");

            int writes = boot.SaveBatch!.WriteCount;
            Assert.That(boot.PressReveal(), Is.True);
            Assert.That(boot.Flow.RevealSelectionPayment, Is.EqualTo(RevealPayment.Seeds));
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(500), "abrir la selección no cobra");
            Assert.That(boot.TapCuia(MurkyCuia106).Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(380), "120 semillas");
            Assert.That(boot.Flow.Session.Current[MurkyCuia106].HiddenMask, Is.EqualTo(0));

            // Aplicar → guardar → cobrar (T10; 04 §2.5 d) en un solo lote: una escritura con lo revelado y el cobro juntos.
            Assert.That(boot.SaveBatch.WriteCount, Is.EqualTo(writes + 1));
            Profile saved = ProfileJson.Deserialize(store.Json!);
            Assert.That(saved.Wallet.Seeds, Is.EqualTo(380));
            Assert.That(saved.LevelState!.History, Does.EndWith("|601"), "R: la capa 0 de la cuia 6");
            StepUntilRevealDone(boot, 5000);
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Reveal_Video()
        {
            // Con fill: sin stock ni saldo, la insignia ofrece el video (rv_reveal) y el video da 1 unidad.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return LoadPlayingWithStore(Level106);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Tray.RevealShowsVideo, Is.True, "«+1» con el icono de video");
            Assert.That(view.Tray.RevealBadgeText, Is.EqualTo("+1"));
            Assert.That(view.Tray.RevealPriceShown, Is.EqualTo(0));
            int presented = FunnelSetup.AdsPresenter.Presented;
            Assert.That(boot.PressReveal(), Is.True);
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(presented + 1));
            Assert.That(FunnelSetup.AdsPresenter.Last!.Placement, Is.EqualTo(AdPlacement.Reveal));
            Assert.That(boot.Flow!.RevealSelecting, Is.True, "tras la recompensa, a elegir cuia");
            Assert.That(boot.Flow.RevealSelectionPayment, Is.EqualTo(RevealPayment.Stock), "con la unidad del video");
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Reveal), Is.EqualTo(1));
            Assert.That(boot.TapCuia(MurkyCuia106).Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(boot.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(0));
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(0));
            Assert.That(boot.Flow.Session.Current[MurkyCuia106].HiddenMask, Is.EqualTo(0));
            StepUntilRevealDone(boot, 5000);

            // Sin fill: el video no aparece; sin saldo, el precio en rojo con «Te faltan {n}», y tocar avisa sin cobrar.
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(50));
            yield return LoadPlayingWithStore(Level106);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            Assert.That(view.Tray.RevealShowsVideo, Is.False, "no_fill: sin video");
            Assert.That(view.Tray.RevealPriceShown, Is.EqualTo(120));
            Assert.That(view.Tray.RevealShowsMissing, Is.True);
            Assert.That(view.Tray.RevealMissingText, Is.EqualTo("Te faltan 70"));
            presented = FunnelSetup.AdsPresenter.Presented;
            Assert.That(boot.PressRevealVideo(), Is.False);
            Assert.That(boot.PressReveal(), Is.False);
            Assert.That(view.ToastText, Is.EqualTo(UiText.RevealMissing(70)), "«Te faltan 70 semillas»");
            Assert.That(boot.Flow!.RevealSelecting, Is.False);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(50), "ningún cobro");
            Assert.That(boot.Wallet.Stock.Get(BoosterKind.Reveal), Is.EqualTo(0));
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(presented));
            Assert.That(boot.Flow.Session.Current[MurkyCuia106].HiddenMask, Is.Not.EqualTo(0));
            Assert.That(HasEvent("\"phase\":\"missing\""), Is.True);
        }

        [UnityTest]
        public IEnumerator Cancel_NoCost()
        {
            yield return LoadPlaying(Level100, FunnelSetup.Profile(300));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;

            // Tocar fuera (el fondo), el botón otra vez, una cuia sin ocultas o Marea: cancela sin coste (04 §2.5).
            Assert.That(boot.PressReveal(), Is.True);
            boot.TapBackground();
            Assert.That(flow.RevealSelecting, Is.False);
            Assert.That(boot.PressReveal(), Is.True);
            Assert.That(boot.PressReveal(), Is.True, "otro toque en el botón cancela [P]");
            Assert.That(flow.RevealSelecting, Is.False);
            Assert.That(boot.PressReveal(), Is.True);
            Assert.That(boot.TapCuia(1).Kind, Is.EqualTo(TapKind.Ignored), "una cuia sin ocultas cancela y no se selecciona");
            Assert.That(flow.RevealSelecting, Is.False);
            Assert.That(flow.Selected, Is.EqualTo(-1));
            Assert.That(boot.PressReveal(), Is.True);
            boot.PressUndo();
            Assert.That(flow.RevealSelecting, Is.False);
            PlayModeHelpers.Step(boot, 50);

            Assert.That(view.RevealPulse.Active, Is.False);
            Assert.That(view.RevealAnimating, Is.False);
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Reveal), Is.EqualTo(1), "sin coste");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(300));
            Assert.That(flow.Session.Current[MurkyCuia100].HiddenMask, Is.EqualTo(0b11), "nada se reveló");
            Assert.That(HasEvent("\"phase\":\"revealed\""), Is.False);
        }

        [UnityTest]
        public IEnumerator Slot_HiddenWhenNoHiddenLeft()
        {
            // Los niveles 41 y 143 (estreno de la Lluvia) no son turbios: sin ranura.
            int[] plain = { 41, 143 };
            for (int i = 0; i < plain.Length; i++)
            {
                _lines.Clear();
                yield return LoadPlaying(plain[i], FunnelSetup.Profile(0));
                GameScreen plainView = GameBootstrap.Current!.View!;
                Assert.That(plainView.Tray.RevealShown, Is.False, "nivel " + plain[i]);
                Assert.That(plainView.Tray.SlotCount, Is.EqualTo(2), "nivel " + plain[i] + ": Marea y Cuia extra");
                Assert.That(LastLayout(plain[i]), Does.Not.Contain("\"id\":\"reveal\""));
            }

            // En el 100, tras revelar la última oculta, la ranura se va cuando la luciérnaga termina y la bandeja se recentra.
            yield return LoadPlaying(Level100, FunnelSetup.Profile(0));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Tray.SlotCount, Is.EqualTo(3));
            Assert.That(boot.PressReveal(), Is.True);
            boot.TapCuia(MurkyCuia100);
            Assert.That(boot.Flow!.Session.HiddenCount, Is.EqualTo(0));
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.Tray.RevealShown, Is.True, "la luciérnaga sale de su botón");

            // Mientras tanto la ranura está gastada: gris, sin insignia, precio ni «Te faltan {n}» (captura 67a).
            Assert.That(view.Tray.RevealButton.interactable, Is.False, "gastada: gris");
            Assert.That(view.Tray.RevealBadgeText, Is.Empty);
            Assert.That(view.Tray.RevealPriceShown, Is.EqualTo(0), "nada que comprar");
            Assert.That(view.Tray.RevealShowsMissing, Is.False);
            Assert.That(view.Tray.RevealShowsVideo, Is.False);
            StepUntilRevealDone(boot, 5000);
            Assert.That(view.Tray.RevealShown, Is.False);
            Assert.That(view.Tray.SlotCount, Is.EqualTo(2));
            Assert.That(view.Tray.Width, Is.EqualTo(168f).Within(0.01f));
            RectTransform undo = (RectTransform)view.Tray.UndoButton.transform;
            Assert.That(undo.anchoredPosition.x, Is.EqualTo(((view.CanvasWidth - 168f) * 0.5f) + 16f).Within(0.01f), "se recentra");
            Assert.That(boot.PressReveal(), Is.False, "sin ocultas no hay Luciérnaga que usar");
        }

        [UnityTest]
        public IEnumerator Reveal_PersistsAcrossUndoAndResume()
        {
            var store = new MemoryProfileStore(FunnelSetup.Profile(0));
            LaunchOptions.ProfileStoreOverride = store;
            yield return LoadPlayingWithStore(Level100);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressReveal(), Is.True);
            boot.TapCuia(MurkyCuia100);
            StepUntilRevealDone(boot, 5000);
            BoardState revealed = boot.Flow!.Session.Current;

            // Marea deshace el vertido, no el revelado (02 §11 T6; 04 §2.1).
            Assert.That(boot.TapCuia(MurkyCuia100).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(1).Kind, Is.EqualTo(TapKind.Poured), "el 6 sobre el 6");
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(boot.PressUndo(), Is.True);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(boot.Flow.Session.Current, Is.EqualTo(revealed));
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(boot.View!.Views[MurkyCuia100].Murky.BadgeCount, Is.EqualTo(0), "sin «?» tras Marea");

            // Volver a la orilla: el mismo tablero, con lo revelado (04 §2.7).
            Assert.That(boot.PressRestart(), Is.True);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 100);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.Flow.Session.Current, Is.EqualTo(revealed));
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(boot.View.Tray.RevealShown, Is.False);

            // Reabrir la app (02 §14): el nivel en curso vuelve con lo revelado.
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap again = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(again);
            PlayModeHelpers.Step(again, 50);
            Assert.That(again.Flow!.LevelNumber, Is.EqualTo(Level100));
            Assert.That(again.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(again.Flow.Session.Current, Is.EqualTo(revealed));
            Assert.That(again.View!.Views[MurkyCuia100].ShownCuia.HiddenMask, Is.EqualTo(0), "la vista lo enseña revelado");
            Assert.That(again.View.Views[MurkyCuia100].Murky.BadgeCount, Is.EqualTo(0));
            Assert.That(again.View.Tray.RevealShown, Is.False, "sin ocultas, sin ranura");
        }

        [UnityTest]
        public IEnumerator Reopen_WithHiddenLeft_TheThreeSlotsFromTheFirstFrame_AndInTheLayout()
        {
            // Luciérnaga en la bandeja (ola 4) tras T-A y T-B: se cierra la app en el 100 con alguna oculta (el vertido 0 → 1
            // revela la capa 1 y la cuia 0 conserva la 0) y se vuelve a abrir. Sin avanzar ningún fotograma con el reloj del
            // test, Marea, «+1 cuia» y Revelar se ven, de 64 dp y centradas (240 dp), y la última AF_LAYOUT level trae los tres
            // con el centro sobre lo dibujado.
            var store = new MemoryProfileStore(FunnelSetup.Profile(0));
            LaunchOptions.ProfileStoreOverride = store;
            yield return LoadPlayingWithStore(Level100);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.TapCuia(MurkyCuia100).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(1).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(boot.Flow!.Session.Current[MurkyCuia100].HiddenMask, Is.EqualTo(0b01), "queda una oculta");

            _lines.Clear();
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap again = GameBootstrap.Current!;
            Assert.That(again, Is.Not.SameAs(boot));
            PlayModeHelpers.TakeClock(again);
            GameScreen view = again.View!;
            Assert.That(again.Flow!.LevelNumber, Is.EqualTo(Level100));
            Assert.That(again.Flow.Phase, Is.EqualTo(FlowPhase.Playing), "sin intro (02 §14)");
            BoosterTray tray = view.Tray;
            Assert.That(tray.UndoButton.gameObject.activeInHierarchy, Is.True, "Marea");
            Assert.That(tray.ExtraCuiaButton.gameObject.activeInHierarchy, Is.True, "«+1 cuia»");
            Assert.That(tray.RevealButton.gameObject.activeInHierarchy, Is.True, "Revelar");
            Assert.That(tray.SlotCount, Is.EqualTo(3));
            Assert.That(tray.Width, Is.EqualTo(240f).Within(0.01f), "16 + 3·64 + 2·8 + 16");
            string? layout = LastLayout(Level100);
            Assert.That(layout, Is.Not.Null, "AF_LAYOUT level del 100 al reabrir");
            Assert.That(AfLayoutLog.ButtonIds(layout!), Does.Contain("undo").And.Contain("extra_cuia").And.Contain("reveal"), layout);
            AfLayoutLog.AssertCenterInside(layout!, "undo", (RectTransform)tray.UndoButton.transform);
            AfLayoutLog.AssertCenterInside(layout!, "extra_cuia", (RectTransform)tray.ExtraCuiaButton.transform);
            AfLayoutLog.AssertCenterInside(layout!, "reveal", (RectTransform)tray.RevealButton.transform);
        }

        [UnityTest]
        public IEnumerator Reveal_PourDuringFlight_SnapsToTheRevealedState()
        {
            // Un vertido desde la cuia que se revela mientras la luciérnaga aún vuela: el revelado termina en el acto, el vertido
            // parte de la cuia ya revelada y al final la vista enseña el modelo.
            yield return LoadPlaying(Level100, FunnelSetup.Profile(0));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.PressReveal(), Is.True);
            boot.TapCuia(MurkyCuia100);
            PlayModeHelpers.Step(boot, 160);
            Assert.That(view.RevealFirefly.Flying, Is.True);
            Assert.That(boot.TapCuia(MurkyCuia100).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(1).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(view.RevealAnimating, Is.False, "el vertido toca la cuia: el revelado termina ya");
            Assert.That(view.Views[MurkyCuia100].MarksAnimating, Is.False);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(view.Views[MurkyCuia100].Murky.BadgeCount, Is.EqualTo(0));
        }

        [Test]
        public void Colors_FireflyPulseAndIcon_AtLeast10DeltaE00FromEveryTint() // direccion-visual-v2 §2.4, regla 1; 07 §7.6
        {
            // Lo que toca o rodea una cuia (la luciérnaga posada en la boca, el aro del pulso) y las superficies del glifo, a ΔE00 ≥ 10
            // de los 14 tintes. #D8E24A (07 §7.6) queda a 5,7 de Arazá: solo va en filos de ≤ 2 dp (ProceduralArt.Firefly.cs).
            Color[] surfaces =
            {
                ProceduralArt.FireflyLit, ProceduralArt.FireflyCore, ProceduralArt.FireflyBody, ProceduralArt.FireflyThorax,
                ProceduralArt.FireflyWing, ProceduralArt.FireflyIconCuia, ProceduralArt.RevealHaloColor, ProceduralArt.IconGlowPeak,
            };
            for (int c = 0; c < surfaces.Length; c++)
            {
                for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
                {
                    Assert.That(ColorMath.DeltaE00(surfaces[c], Palette.Tint(tint)), Is.GreaterThanOrEqualTo(10f), "color " + c + " frente al tinte " + tint);
                }
            }

            Assert.That(ColorMath.DeltaE00(ProceduralArt.FireflyLight, Palette.Hex(0xD8E24A)), Is.LessThan(0.01f), "el amarillo verdoso de 07 §7.6");
            Assert.That(ColorMath.DeltaE00(ProceduralArt.FireflyLit, ProceduralArt.FireflyLight), Is.LessThan(8f), "la luz encendida, de la misma familia");

            // Legible (direccion-visual-v2 §2.4, regla 2: iconos ≥ 3:1): el contorno crema de la cuia oscura sobre la cara de agua de
            // la ranura, y el farol sobre la cuia oscura.
            Assert.That(ColorMath.ContrastRatio(Palette.Cream, Palette.FamilyWater.Base), Is.GreaterThanOrEqualTo(3f));
            Assert.That(ColorMath.ContrastRatio(Palette.Cream, Palette.FamilyWater.Deep), Is.GreaterThanOrEqualTo(3f));
            Assert.That(ColorMath.ContrastRatio(ProceduralArt.FireflyLit, ProceduralArt.FireflyIconCuia), Is.GreaterThanOrEqualTo(3f));
            Assert.That(ColorMath.ContrastRatio(ProceduralArt.FireflyLit, Palette.FamilyWater.Base), Is.GreaterThanOrEqualTo(3f));
        }

        [UnityTest]
        public IEnumerator NoAllocWhileTheFireflyFliesAndReveals()
        {
            // Calentamiento (JIT, sprites de la fruta y del disco) y la medida en otra carga: 20 fotogramas de vuelo y 20 de
            // revelado sin asignaciones (08 §5.4), sin contar los fotogramas de un cambio (el toque, el aterrizaje).
            for (int pass = 0; pass < 2; pass++)
            {
                yield return LoadPlaying(Level100, FunnelSetup.Profile(0));
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                Assert.That(boot.PressReveal(), Is.True);
                boot.TapCuia(MurkyCuia100);
                PlayModeHelpers.Step(boot, 64);

                void Run20()
                {
                    for (int f = 0; f < 20; f++)
                    {
                        view.Tick(4f);
                        boot.Flow!.Tick(4, view.IsViewIdle);
                    }
                }

                if (pass == 1)
                {
                    Assert.That(view.RevealFirefly.Flying, Is.True);
                    Assert.That(Run20, Is.Not.AllocatingGCMemory(), "20 fotogramas de vuelo");
                }

                while (!view.RevealMarksStarted)
                {
                    PlayModeHelpers.TickExact(boot, 4);
                }

                PlayModeHelpers.Step(boot, 64);
                if (pass == 1)
                {
                    Assert.That(view.Views[MurkyCuia100].Murky.IsAnimating, Is.True);
                    Assert.That(Run20, Is.Not.AllocatingGCMemory(), "20 fotogramas de revelado");
                }

                StepUntilRevealDone(boot, 4000);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator NoAllocWhileTheSlotOffersTheVideo()
        {
            // Sin stock ni saldo y con fill, la ranura ofrece el video (rv_reveal). Preguntar a los anuncios crea la cadena de la
            // fecha del contador diario, así que solo se pregunta cada RevealVideoPollMs: entre dos preguntas, 20 fotogramas sin
            // asignaciones (08 §5.4). La segunda carga mide (la primera calienta).
            for (int pass = 0; pass < 2; pass++)
            {
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
                yield return LoadPlayingWithStore(Level106);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                Assert.That(view.Tray.RevealShowsVideo, Is.True);

                // Justo después de una pregunta, el plazo vuelve a RevealVideoPollMs.
                float last = view.RevealVideoPollLeftMs;
                for (int t = 0; t < 1000; t++)
                {
                    PlayModeHelpers.TickExact(boot, 4);
                    float now = view.RevealVideoPollLeftMs;
                    if (now > last)
                    {
                        break;
                    }

                    last = now;
                }

                Assert.That(view.RevealVideoPollLeftMs, Is.GreaterThan(GameScreen.RevealVideoPollMs - 8f), "acaba de preguntar");

                void Run20()
                {
                    for (int f = 0; f < 20; f++)
                    {
                        view.Tick(4f);
                        boot.Flow!.Tick(4, view.IsViewIdle);
                    }
                }

                if (pass == 1)
                {
                    Assert.That(Run20, Is.Not.AllocatingGCMemory(), "20 fotogramas con el video ofrecido");
                }
                else
                {
                    Run20();
                }

                Assert.That(view.Tray.RevealShowsVideo, Is.True);
                yield return null;
            }
        }

        // Carga el nivel con el perfil, quita el reloj, salta la intro y avanza hasta Playing con la vista quieta.
        private static IEnumerator LoadPlaying(int level, Profile profile)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return LoadPlayingWithStore(level);
        }

        // Como LoadPlaying con el perfil que ya está en LaunchOptions.ProfileStoreOverride.
        private static IEnumerator LoadPlayingWithStore(int level)
        {
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "nivel " + level + " en juego");
            PlayModeHelpers.Step(boot, 50);
        }

        // Avanza hasta que la luciérnaga termina (falla pasado maxMs) y devuelve los ms.
        private static int StepUntilRevealDone(GameBootstrap boot, int maxMs)
        {
            int t = 0;
            while (boot.View!.RevealAnimating)
            {
                Assert.That(t, Is.LessThan(maxMs), "la luciérnaga termina");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
            }

            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            return t;
        }

        private static Rect WorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(
                Mathf.Min(corners[0].x, corners[2].x),
                Mathf.Min(corners[0].y, corners[2].y),
                Mathf.Max(corners[0].x, corners[2].x),
                Mathf.Max(corners[0].y, corners[2].y));
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (condition.StartsWith("AF_", System.StringComparison.Ordinal))
            {
                _lines.Add(condition);
            }
        }

        private bool HasEvent(string fragment)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].StartsWith("AF_STATE {\"event\":\"reveal\"", System.StringComparison.Ordinal) && _lines[i].Contains(fragment))
                {
                    return true;
                }
            }

            return false;
        }

        private string? LastLayout(int level)
        {
            string tag = "AF_LAYOUT {\"screen\":\"level\",\"level\":" + level.ToString(CultureInfo.InvariantCulture) + ",";
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                if (_lines[i].StartsWith(tag, System.StringComparison.Ordinal))
                {
                    return _lines[i];
                }
            }

            return null;
        }

        private int LayoutCount(int level)
        {
            string tag = "AF_LAYOUT {\"screen\":\"level\",\"level\":" + level.ToString(CultureInfo.InvariantCulture) + ",";
            int n = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].StartsWith(tag, System.StringComparison.Ordinal))
                {
                    n++;
                }
            }

            return n;
        }
    }
}
