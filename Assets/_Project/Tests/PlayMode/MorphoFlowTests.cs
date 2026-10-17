#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AF.Boosters;
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
    /// Morpho en la escena (tarea MO1; 04 §2.3; 08 §9 <c>hint_timeout_ms</c> = 150; D-019): con el arranque real, el flujo usa el
    /// reloj del juego (<c>LaunchOptions.ClockOverride</c>) y la sonda <c>AF_PERF morpho</c>; la consulta corre en un hilo de
    /// trabajo mientras los fotogramas siguen, la respuesta con vertido cobra 100 semillas al darla y el corte a los 150 ms
    /// responde «No encuentro un camino» sin cobrar. Sin interfaz (MO2).
    /// </summary>
    public sealed class MorphoFlowTests
    {
        private const int Level = 15;
        private const int Seeds = 500;

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
        public IEnumerator Hint_RunsOffTheMainThread_ChargesOnlyWithAPour_AndLogsAfPerf()
        {
            yield return LoadPlaying();
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Clock, Is.SameAs(FunnelSetup.Clock), "el flujo mide hint_timeout_ms con el reloj del juego");
            Assert.That(flow.HintProbe, Is.Not.Null, "la sonda AF_PERF morpho");
            Assert.That(flow.Hint.State, Is.EqualTo(HintState.Ready));
            Assert.That(flow.Hint.Price, Is.EqualTo(100));

            Assert.That(flow.PressHint(), Is.True);
            Assert.That(flow.HintThinking, Is.True);
            int frames = 0;
            float start = Time.realtimeSinceStartup;
            while (flow.HintThinking)
            {
                Assert.That(Time.realtimeSinceStartup - start, Is.LessThan(PlayModeHelpers.TimeoutSeconds), "Morpho no respondió");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                frames++;
                yield return null;
            }

            HintResult r = flow.LastHint!.Value;
            Assert.That(r.Answer, Is.EqualTo(HintAnswer.Pour));
            Assert.That(r.Charged, Is.True);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(Seeds - 100), "100 semillas al dar el vertido");
            Assert.That(flow.HasActiveHint, Is.True);
            Assert.That(PourRules.IsLegal(flow.Session.Current, r.Move.Source, r.Move.Target), Is.True);
            Assert.That(frames, Is.GreaterThanOrEqualTo(1), "la respuesta llega en un fotograma posterior: PressHint no espera");
            Assert.That(HasLine("\"event\":\"morpho\",\"source\":\"hint\",\"level\":15,\"answer\":\"pour\""), Is.True, "AF_PERF morpho");
            Assert.That(HasLine("\"charged\":true"), Is.True);
            Assert.That(HasLine("\"timeout_ms\":150"), Is.True);

            // Verter la pista la apaga.
            Assert.That(boot.TapCuia(r.Move.Source).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(r.Move.Target).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(flow.HasActiveHint, Is.False);
        }

        [UnityTest]
        public IEnumerator Hint_CutAt150ms_NotFound_WithoutCharging_WhileFramesKeepGoing()
        {
            yield return LoadPlaying();
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            using (var gate = new ManualResetEventSlim(false))
            {
                flow.Morpho = new MorphoBooster(flow.Boosters, flow.Clock, () => gate.Wait(20000));
                Assert.That(flow.PressHint(), Is.True);
                float before = Time.realtimeSinceStartup;
                for (int i = 0; i < 20; i++)
                {
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs); // la vista y el flujo siguen con el hilo retenido
                    yield return null;
                }

                Assert.That(flow.HintThinking, Is.True);
                Assert.That(Time.realtimeSinceStartup - before, Is.LessThan(5f), "el hilo principal no espera al de Morpho");
                FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(150));
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(flow.Morpho.TimeoutRequested, Is.True, "a los 150 ms el hilo principal marca el corte");
                gate.Set();
                float start = Time.realtimeSinceStartup;
                while (flow.HintThinking)
                {
                    Assert.That(Time.realtimeSinceStartup - start, Is.LessThan(PlayModeHelpers.TimeoutSeconds), "Morpho no respondió");
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                    yield return null;
                }
            }

            HintResult r = flow.LastHint!.Value;
            Assert.That(r.Answer, Is.EqualTo(HintAnswer.NotFound), "«No encuentro un camino desde aquí»");
            Assert.That(r.Cancelled, Is.True);
            Assert.That(r.Charged, Is.False);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(Seeds), "el corte no cobra");
            Assert.That(flow.HasActiveHint, Is.False);
            Assert.That(HasLine("\"answer\":\"not_found\""), Is.True);
            Assert.That(HasLine("\"cancelled\":true,\"charged\":false"), Is.True);
        }

        private static IEnumerator LoadPlaying()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(Seeds));
            yield return PlayModeHelpers.LoadGame(Level, new MemoryProgressStore(Level, Level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "nivel " + Level + " en juego");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(Seeds));
        }

        private bool HasLine(string fragment)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].StartsWith("AF_PERF", StringComparison.Ordinal) && _lines[i].Contains(fragment))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            _lines.Add(condition);
        }
    }
}
