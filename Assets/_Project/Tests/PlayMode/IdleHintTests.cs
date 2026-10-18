#nullable enable
using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Pista por inactividad (tarea MO3; 02 §3.4 y §12; 04 §2.3; 08 §9 <c>hint_idle_from_level</c> = 3 y <c>hint_idle_delay_s</c>
    /// = 12; D-019) en la escena <c>Game</c> real, con el reloj manual del juego (<c>LaunchOptions.ClockOverride</c>): desde el 3,
    /// a los 12 s sin toques, el origen del vertido que propone el solver pulsa una vez (1,04 en 600 ms) sin enseñar el destino ni
    /// cobrar; cada toque vuelve a empezar la cuenta; nunca antes del 3, nunca en posición perdida ni en los tres casos sin
    /// respuesta («Desde aquí no hay salida», «No veo un vertido seguro» y «No encuentro un camino»), y apagada en Ajustes no
    /// pulsa.
    /// </summary>
    public sealed class IdleHintTests
    {
        private const int Seeds = 500;
        private const float ScaleEps = 0.002f;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.IdleHintOverride = true; // la de los demás tests va apagada (PlayModeHelpers.NoIdleHintUnlessSet)
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.IdleHintOverride = null;
            LaunchOptions.Reset();
        }

        [Test]
        public void Config_IsThe08_9Table_AndTheSettingIsDelayZero()
        {
            BoosterConfig config = BoosterConfig.Defaults;
            Assert.That(config.HintIdleFromLevel, Is.EqualTo(3), "hint_idle_from_level");
            Assert.That(config.HintIdleDelayS, Is.EqualTo(12), "hint_idle_delay_s");
            Assert.That(IdleHint.DelayMs(config, true), Is.EqualTo(12000));
            Assert.That(IdleHint.DelayMs(config, false), Is.EqualTo(0), "02 §12: apagada en Ajustes = hint_idle_delay_s 0 en el dispositivo");
            config.HintIdleDelayS = 0;
            Assert.That(IdleHint.DelayMs(config, true), Is.EqualTo(0), "08 §9: 0 = apagada");
            config.HintIdleDelayS = -1;
            Assert.Throws<InvalidOperationException>(() => config.Validate());

            BoosterConfig defaults = BoosterConfig.Defaults;
            Assert.That(IdleHint.AppliesTo(2, defaults), Is.False, "antes del 3, nunca");
            foreach (int level in new[] { 3, 12, 100, 323, 700, 1000 })
            {
                Assert.That(IdleHint.AppliesTo(level, defaults), Is.True, "en todos los niveles desde el 3, sin tope: " + level);
            }

            Assert.That(IdleHint.PulsesOn(HintAnswer.Pour), Is.True);
            foreach (HintAnswer answer in new[] { HintAnswer.NoExit, HintAnswer.NoSafePour, HintAnswer.NotFound })
            {
                Assert.That(IdleHint.PulsesOn(answer), Is.False, "D-019: no pulsa nada en " + answer);
            }

            Assert.That(IdleHint.PulseScale, Is.EqualTo(1.04f), "02 §3.4: escala 1,04");
            Assert.That(IdleHint.PulseMs, Is.EqualTo(600), "02 §3.4: 600 ms");
        }

        [UnityTest]
        public IEnumerator Level3_At12sWithoutTouches_TheSolverSourcePulsesOnce_NeverTheTarget_AndItIsFree()
        {
            yield return LoadPlaying(3, FunnelSetup.Profile(Seeds));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            int stock = boot.Wallet!.Stock.Get(BoosterKind.Hint);
            ArmIdle(boot);

            // 11,9 s: nada. A los 12 s, el solver.
            FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(11900));
            PlayModeHelpers.Step(boot, 5 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(0), "a los 11,9 s aún no pregunta");
            FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(100));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(1), "a los 12 s pregunta al solver de Morpho");
            yield return WaitAnswer(boot);

            Assert.That(view.IdleHint.LastAnswer, Is.EqualTo(HintAnswer.Pour));
            Assert.That(view.IdlePulses, Is.EqualTo(1), "pulsa");
            int source = view.IdlePulseCuia;
            HintResult expected = new MorphoBooster(flow.Boosters, flow.Clock)
                .Compute(flow.Session.Current, flow.Session.Initial, flow.Session.PourMode, flow.Session.FutileRuleEnabled, flow.LevelNumber);
            Assert.That(expected.Answer, Is.EqualTo(HintAnswer.Pour));
            Assert.That(source, Is.EqualTo(expected.Move.Source), "el origen del vertido que propone el solver");

            // Un solo pulso de 600 ms hasta 1,04 en el origen; ninguna otra cuia cambia (el destino nunca se enseña).
            float max = 0f;
            int pulseMs = 0;
            while (view.IdlePulsing && pulseMs < 2000)
            {
                max = Mathf.Max(max, view.CuiaBodyScale(source));
                for (int i = 0; i < view.Views.Length; i++)
                {
                    if (i != source)
                    {
                        Assert.That(view.CuiaBodyScale(i), Is.EqualTo(1f), "solo pulsa el origen (cuia " + i + ")");
                    }
                }

                Assert.That(view.MorphoFx.Active, Is.False, "sin la mariposa ni los anillos de Morpho");
                Assert.That(flow.HasActiveHint, Is.False, "no es Morpho: no resalta origen y destino");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                pulseMs += PlayModeHelpers.FrameMs;
            }

            Assert.That(max, Is.InRange(IdleHint.PulseScale - 0.004f, IdleHint.PulseScale + ScaleEps), "escala 1,04");
            Assert.That(pulseMs, Is.InRange(IdleHint.PulseMs - (2 * PlayModeHelpers.FrameMs), IdleHint.PulseMs + PlayModeHelpers.FrameMs), "600 ms");
            Assert.That(view.CuiaBodyScale(source), Is.EqualTo(1f), "vuelve a su tamaño");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(Seeds), "sin coste");
            Assert.That(boot.Wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(stock), "no gasta Morphos");
            Assert.That(flow.Hint.CooldownMs, Is.EqualTo(0), "no cuenta para el enfriamiento de Morpho");

            // Una vez por tramo sin toques: no vuelve a preguntar ni a pulsar hasta el siguiente toque.
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(30));
            PlayModeHelpers.Step(boot, 20 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(1));
            Assert.That(view.IdlePulses, Is.EqualTo(1));

            // Un toque (en el fondo, sin cambiar nada del flujo) vuelve a empezar la cuenta: otros 12 s y otro pulso.
            view.IdleHintTouch();
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(11900));
            PlayModeHelpers.Step(boot, 5 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(1), "la cuenta empezó en el toque");
            FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(100));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            yield return WaitAnswer(boot);
            Assert.That(view.IdlePulses, Is.EqualTo(2));

            // Un toque en mitad del pulso lo apaga en el acto.
            PlayModeHelpers.Step(boot, 5 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdlePulsing, Is.True);
            view.IdleHintTouch();
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.IdlePulsing, Is.False, "el toque apaga el pulso");
            Assert.That(view.CuiaBodyScale(view.IdlePulseCuia), Is.EqualTo(1f));
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(Seeds));
        }

        [UnityTest]
        public IEnumerator EveryTapOnTheBoard_RestartsTheCount()
        {
            yield return LoadPlaying(3, FunnelSetup.Profile(Seeds));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            ArmIdle(boot);
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(11));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            int cuia = FirstNonEmpty(boot.Flow!.Session.Current);
            Assert.That(boot.TapCuia(cuia).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(boot.TapCuia(cuia).Kind, Is.EqualTo(TapKind.Deselected));
            ArmIdle(boot); // la cuenta vuelve a empezar cuando la cuia acaba de bajar
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(11));
            PlayModeHelpers.Step(boot, 5 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(0), "22 s de reloj, pero nunca 12 s seguidos sin toques");
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(1));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(1), "12 s desde el último toque");
            yield return WaitAnswer(boot);
            Assert.That(view.IdlePulses, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Level2_NeverPulses()
        {
            yield return LoadPlaying(2, FunnelSetup.Profile(Seeds));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.IdleHint.Armed, Is.False, "hint_idle_from_level = 3");
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(60));
            PlayModeHelpers.Step(boot, 20 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(0));
            Assert.That(view.IdlePulses, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator PauseOpen_IsNotIdleness_AndClosingItRestartsTheCountFromZero()
        {
            yield return LoadPlaying(3, FunnelSetup.Profile(Seeds));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            ArmIdle(boot);

            // 11 s quieto y se abre la pausa: la vista ocupada no cuenta como inactividad, por mucho que pase el reloj.
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(11));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(0));
            view.PauseButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.Pause.Visible, Is.True, "la pausa está abierta");
            Assert.That(view.IdleHint.Armed, Is.False, "con la pausa abierta la cuenta no corre");
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(60));
            PlayModeHelpers.Step(boot, 20 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(0), "la pausa no es inactividad: no pregunta");
            Assert.That(view.IdlePulses, Is.EqualTo(0), "ni pulsa");

            // Al cerrarla, la cuenta empieza de cero: los 11 s de antes no suman.
            view.Pause.ResumeButton.onClick.Invoke();
            Assert.That(view.Pause.Visible, Is.False);
            ArmIdle(boot);
            FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(11900));
            PlayModeHelpers.Step(boot, 5 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(0), "la cuenta empezó al cerrar la pausa");
            FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(100));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(1), "12 s desde que se cerró la pausa");
            yield return WaitAnswer(boot);
            Assert.That(view.IdlePulses, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BackFromBackground_IsNotIdleness_TheCountRestartsFromZero()
        {
            yield return LoadPlaying(3, FunnelSetup.Profile(Seeds));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            ArmIdle(boot);
            Assert.That(boot.Flow!.Selected, Is.LessThan(0), "sin selección: volver no cambia nada del flujo");

            // 11 s quieto y la app pasa un minuto en segundo plano: al volver no pulsa en el primer fotograma.
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(11));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(0));
            ApplicationPause(boot, true);
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(60));
            ApplicationPause(boot, false);
            PlayModeHelpers.Step(boot, 5 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(0), "el tiempo fuera de la app no es inactividad en el tablero");
            Assert.That(view.IdlePulses, Is.EqualTo(0));
            Assert.That(view.IdleHint.Armed, Is.True, "la cuenta corre de nuevo desde la vuelta");

            // La cuenta empezó al volver: 12 s desde entonces.
            FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(11900));
            PlayModeHelpers.Step(boot, 5 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(0), "la cuenta empezó al volver de segundo plano");
            FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(100));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(1), "12 s desde la vuelta");
            yield return WaitAnswer(boot);
            Assert.That(view.IdlePulses, Is.EqualTo(1));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(Seeds));
        }

        [UnityTest]
        public IEnumerator TouchDuringTheQuery_AbandonsItWithoutWaiting_AndTheNextStretchAsksAgain()
        {
            yield return LoadPlaying(3, FunnelSetup.Profile(Seeds));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            var gate = new ManualResetEventSlim(false);
            try
            {
                MorphoBooster morpho = new MorphoBooster(flow.Boosters, flow.Clock, () => gate.Wait(20000));
                view.IdleHint.UseMorpho(morpho);
                ArmIdle(boot);
                FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(12));
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(view.IdleHint.Querying, Is.True, "a los 12 s pregunta");
                Assert.That(view.IdleHint.QueryCount, Is.EqualTo(1));

                // Un toque con el hilo de trabajo retenido: la consulta se abandona sin esperarlo y la cuenta vuelve a empezar.
                view.IdleHintTouch();
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(view.IdleHint.Querying, Is.False, "el toque abandona la consulta");
                Assert.That(view.IdleHint.Armed, Is.True);
                Assert.That(morpho.IsBusy, Is.True, "el hilo de trabajo sigue retenido: el principal no lo esperó");

                // Otros 12 s con el hilo aún retenido: Morpho no admite otra consulta y la pista lo reintenta cada fotograma.
                FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(12));
                for (int i = 0; i < 5; i++)
                {
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                    yield return null;
                }

                Assert.That(view.IdleHint.QueryCount, Is.EqualTo(1), "la consulta abandonada aún no ha parado");
                Assert.That(view.IdleHint.Querying, Is.False);
                Assert.That(view.IdlePulses, Is.EqualTo(0), "la respuesta abandonada nunca pulsa");

                // Se suelta el hilo: en cuanto para, el tramo siguiente vuelve a preguntar y pulsa.
                gate.Set();
                float start = Time.realtimeSinceStartup;
                while (view.IdleHint.QueryCount < 2)
                {
                    Assert.That(Time.realtimeSinceStartup - start, Is.LessThan(PlayModeHelpers.TimeoutSeconds), "la pista no volvió a preguntar");
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                    yield return null;
                }

                yield return WaitAnswer(boot);
            }
            finally
            {
                gate.Set();
            }

            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(2));
            Assert.That(view.IdleHint.LastAnswer, Is.EqualTo(HintAnswer.Pour));
            Assert.That(view.IdlePulses, Is.EqualTo(1), "la pista no quedó muerta el resto del nivel");
        }

        [UnityTest]
        public IEnumerator LevelWithoutHintSupported_NeverPulses()
        {
            // El tablero del nivel 7 con hint_supported = false (04 §2.3 b; el de MorphoUiTests.Unsupported_GreySlot_TooltipOnTap):
            // en juego, desde el 3 y con la pista encendida, pero sin solver que la respalde: nunca pregunta ni pulsa.
            yield return LoadPlaying(15, FunnelSetup.Profile(Seeds), MorphoUiTests.UnsupportedPack(15));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Level.HintSupported, Is.False, "hint_supported = false");
            Assert.That(flow.AcceptsBoardInput, Is.True, "el nivel está en juego");
            Assert.That(IdleHint.AppliesTo(flow.LevelNumber, flow.Boosters), Is.True, "desde hint_idle_from_level");
            StepUntilIdle(boot);
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.IdleHint.Armed, Is.False, "sin hint_supported la cuenta no corre");
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(60));
            PlayModeHelpers.Step(boot, 20 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(0), "no pregunta al solver");
            Assert.That(view.IdlePulses, Is.EqualTo(0), "ni pulsa");
        }

        [UnityTest]
        public IEnumerator OffInSettings_NeverPulses_AndTurningItBackOnApplies()
        {
            Profile profile = FunnelSetup.Profile(Seeds);
            profile.Settings.IdleHint = false;
            yield return LoadPlaying(3, profile);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.IdleHint.Armed, Is.False, "settings.idle_hint = false: hint_idle_delay_s = 0 en el dispositivo");
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(60));
            PlayModeHelpers.Step(boot, 20 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(0), "apagada en Ajustes, no pregunta");
            Assert.That(view.IdlePulses, Is.EqualTo(0), "ni pulsa");

            // Encenderla se aplica al momento (la vista lee el ajuste en cada fotograma).
            Assert.That(boot.ChooseIdleHint(true), Is.True);
            ArmIdle(boot);
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(12));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            yield return WaitAnswer(boot);
            Assert.That(view.IdlePulses, Is.EqualTo(1));

            // Y apagarla a mitad del tramo corta la cuenta.
            Assert.That(boot.ChooseIdleHint(false), Is.True);
            view.IdleHintTouch();
            PlayModeHelpers.Step(boot, 2 * PlayModeHelpers.FrameMs);
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(60));
            PlayModeHelpers.Step(boot, 20 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(1));
            Assert.That(view.IdlePulses, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Settings_TheIdleHintRow_TogglesAndSavesInTheAct()
        {
            var store = new MemoryProfileStore(SettingsScreenTests.ProfileAt(4));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.IdleHintSlot.gameObject.activeSelf, Is.True);
            Assert.That(settings.IdleHintLabel, Is.EqualTo("Pista por inactividad"), "02 §12");
            Assert.That(settings.IdleHintMarked, Is.True, "activada por defecto (02 §12)");
            Assert.That(((RectTransform)settings.IdleHintButton.transform).rect.height, Is.GreaterThanOrEqualTo(48f), "fila ≥ 48 dp");

            settings.IdleHintButton.onClick.Invoke();
            Assert.That(settings.IdleHintMarked, Is.False, "desmarcada");
            Assert.That(settings.Visible, Is.True, "sin cerrar");
            Assert.That(ProfileJson.Deserialize(store.Json!).Settings.IdleHint, Is.False, "guardado en el acto (settings.idle_hint)");

            settings.CloseButton.onClick.Invoke();
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(settings.IdleHintMarked, Is.False, "se abre con lo guardado");
            settings.IdleHintButton.onClick.Invoke();
            Assert.That(settings.IdleHintMarked, Is.True);
            Assert.That(ProfileJson.Deserialize(store.Json!).Settings.IdleHint, Is.True);
            settings.CloseButton.onClick.Invoke();
        }

        [UnityTest]
        public IEnumerator LostPosition_NoExit_NeverPulses()
        {
            // Tablero con vertidos legales pero sin solución (A* lo demuestra en menos de 100 nodos): «Desde aquí no hay salida»
            // (02 T5, respuesta 2). Su cierre pasa de los 64 estados, así que la partida no lo da por estancado (D-014) y sigue en
            // juego, como una posición perdida a la que el jugador llega vertiendo.
            int[][] board = { new[] { 1, 1, 4, 3 }, new[] { 4, 4, 2, 4 }, new int[0], new[] { 1, 3, 2, 3 }, new[] { 1, 2, 3, 2 } };
            string pack = PlayModeHelpers.BoardPack(1, "w01_estuario", "white", 15, new[] { "guanabana", "cupuacu", "tapereba", "aguaje" }, board);
            yield return LoadPlaying(15, FunnelSetup.Profile(Seeds), pack);
            yield return AssertSilentAt12s(HintAnswer.NoExit);
        }

        [UnityTest]
        public IEnumerator MurkyWithoutASafePour_NoSafePour_NeverPulses()
        {
            // Aguas turbias de MorphoBoosterTests: dos realidades, alguna con solución y ningún vertido seguro en las dos
            // («No veo un vertido seguro…», 02 T5, respuesta 3).
            yield return LoadPlaying(15, FunnelSetup.Profile(Seeds), MurkyPack());
            yield return AssertSilentAt12s(HintAnswer.NoSafePour);
        }

        [UnityTest]
        public IEnumerator SolverCutAtHintTimeout_NotFound_NeverPulses()
        {
            yield return LoadPlaying(3, FunnelSetup.Profile(Seeds));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            using (var gate = new ManualResetEventSlim(false))
            {
                view.IdleHint.UseMorpho(new MorphoBooster(flow.Boosters, flow.Clock, () => gate.Wait(20000)));
                ArmIdle(boot);
                FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(12));
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(view.IdleHint.Querying, Is.True);
                for (int i = 0; i < 10; i++)
                {
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs); // el hilo principal no espera al de trabajo
                    yield return null;
                }

                FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(flow.Boosters.HintTimeoutMs));
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(view.IdleHint.Morpho!.TimeoutRequested, Is.True, "hint_timeout_ms: el corte");
                gate.Set();
                yield return WaitAnswer(boot);
            }

            Assert.That(view.IdleHint.LastAnswer, Is.EqualTo(HintAnswer.NotFound), "«No encuentro un camino desde aquí» (respuesta 4)");
            Assert.That(view.IdlePulses, Is.EqualTo(0), "no pulsa nada");
            Assert.That(view.IdlePulsing, Is.False);
        }

        // A los 12 s sin toques el solver responde sin vertido y ninguna cuia pulsa ni cambia de tamaño.
        private static IEnumerator AssertSilentAt12s(HintAnswer answer)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.AcceptsBoardInput, Is.True, "hay vertidos legales: el nivel sigue en juego");
            ArmIdle(boot);
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(12));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(1));
            yield return WaitAnswer(boot);
            Assert.That(view.IdleHint.LastAnswer, Is.EqualTo(answer));
            PlayModeHelpers.Step(boot, 400);
            Assert.That(view.IdlePulses, Is.EqualTo(0), "D-019: no pulsa nada en " + answer);
            Assert.That(view.IdlePulsing, Is.False);
            for (int i = 0; i < view.Views.Length; i++)
            {
                Assert.That(view.CuiaBodyScale(i), Is.EqualTo(1f), "cuia " + i);
            }

            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(Seeds));
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(30));
            PlayModeHelpers.Step(boot, 10 * PlayModeHelpers.FrameMs);
            Assert.That(view.IdleHint.QueryCount, Is.EqualTo(1), "una vez por tramo sin toques");
        }

        // Carga el nivel con el perfil (y el paquete, si lo hay), salta la intro y espera a la vista quieta en juego.
        private static IEnumerator LoadPlaying(int level, Profile profile, string? pack = null)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            var progress = new MemoryProgressStore(level, level - 1);
            if (pack == null)
            {
                yield return PlayModeHelpers.LoadGame(level, progress);
            }
            else
            {
                yield return PlayModeHelpers.LoadGameWithPack(pack, level, progress);
            }

            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            StepUntilIdle(boot);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "nivel " + level + " en juego");
        }

        private static void StepUntilIdle(GameBootstrap boot)
        {
            for (int t = 0; t < 6000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle || boot.View.NoticeShowing); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }
        }

        // Unos fotogramas con el nivel quieto: la cuenta de inactividad empieza a correr.
        private static void ArmIdle(GameBootstrap boot)
        {
            StepUntilIdle(boot);
            PlayModeHelpers.Step(boot, 3 * PlayModeHelpers.FrameMs);
            Assert.That(boot.View!.IdleHint.Armed, Is.True, "la cuenta corre en juego");
            Assert.That(boot.View.IdleHint.Querying, Is.False);
        }

        // El mensaje de Unity de ir o volver de segundo plano (GameBootstrap.OnApplicationPause, privado).
        private static void ApplicationPause(GameBootstrap boot, bool paused)
        {
            MethodInfo? method = typeof(GameBootstrap).GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "GameBootstrap.OnApplicationPause(bool)");
            method!.Invoke(boot, new object[] { paused });
        }

        // Fotogramas hasta que la consulta del hilo de trabajo responde (la vista y el flujo siguen).
        private static IEnumerator WaitAnswer(GameBootstrap boot)
        {
            float start = Time.realtimeSinceStartup;
            while (boot.View!.IdleHint.Querying)
            {
                Assert.That(Time.realtimeSinceStartup - start, Is.LessThan(PlayModeHelpers.TimeoutSeconds), "la pista por inactividad no respondió");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                yield return null;
            }
        }

        private static int FirstNonEmpty(BoardState state)
        {
            for (int i = 0; i < state.Count; i++)
            {
                if (!state[i].IsEmpty)
                {
                    return i;
                }
            }

            return 0;
        }

        // Nivel 15 de Aguas turbias de MorphoBoosterTests (Flow_NoSafePour_InAMurkyBoard_DoesNotCharge).
        private static string MurkyPack()
        {
            return "{ \"subset_of\": \"af-level/1\", \"pack_id\": \"test_idle_murky\", \"kind\": \"campaign\", \"world\": 1, "
                + "\"world_id\": \"w01_estuario\", \"lap\": 1, \"water_base\": \"white\", \"levels\": [ { \"id\": \"m01_v1_n0015\", "
                + "\"level\": 15, \"world\": 1, \"lap\": 1, \"water_base\": \"white\", \"palette\": [\"guanabana\", \"cupuacu\", \"tapereba\"], "
                + "\"units_per_color\": 4, \"caps\": [4, 4, 4, 4], \"cuias\": [[1], [3, 2, 2, 3], [3, 2, 1, 1], [2, 3, 1]], "
                + "\"attrs\": [null, null, null, null], \"murky\": true, \"hidden\": [[3, 0], [3, 1]], \"difficulty\": { \"par\": 4 }, "
                + "\"relief\": [] } ] }";
        }
    }
}
