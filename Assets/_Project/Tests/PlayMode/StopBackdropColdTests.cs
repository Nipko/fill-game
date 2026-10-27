#nullable enable
using System.Collections;
using System.Globalization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// <c>build_ms</c> del fondo de la parada en frío (tarea FX4; observación 4 de la prueba de la ola 2 en el teléfono: el 71,
    /// Tefé, dio 8,39 ms en la primera carga, por encima de los 8 de 08 §5.4 y 07 §2.1). En el Editor, el 71 como primera carga
    /// del proceso costaba 9,4-9,7 ms, y en caliente ~0,2: la diferencia era el primer uso (la primera malla de UGUI, 5,7 ms; los
    /// datos de las siluetas, 1,3; y el primer paso de cada parte). <see cref="StopBackdrop.Prewarm"/> lo paga antes y fuera de
    /// la medida (sonda <c>warm_ms</c>), con la misma composición que se ve. El 71 en frío queda ≤ 6,5 ms (0,4 ms en el
    /// Editor x64 tras FX4). Explícito: solo mide en frío si es lo primero que se ejecuta en el proceso
    /// (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.StopBackdropColdTests</c>).
    /// </summary>
    [Explicit("Medida en frío de build_ms (FX4): solo vale como primera carga del proceso.")]
    public sealed class StopBackdropColdTests
    {
        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level71_Cold_BuildMsIsUnderSixAndAHalf()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            yield return HudSetup.LoadAtIntro(71);
            StopBackdrop b = GameBootstrap.Current!.View!.StopBackdrop;
            Assert.That(b.Stop, Is.Not.Null);
            Assert.That(b.Stop!.Id, Is.EqualTo("tefe"));
            string cold = "build_ms " + b.BuildMs.ToString("0.###", CultureInfo.InvariantCulture) + ", warm_ms "
                + b.WarmMs.ToString("0.###", CultureInfo.InvariantCulture);
            TestContext.WriteLine("71 en frío: " + cold);
            Debug.Log("AF_FX4 71 en frío: " + cold);
            Assert.That(b.Warmed, Is.True);
            Assert.That(b.BuildMs, Is.LessThanOrEqualTo(PerfBudget.Ms(6.5d)), "objetivo de FX4 (≤ 8 ms, 08 §5.4), " + PerfBudget.Describe(6.5d));

            // Ya en caliente, otra parada (la preparación es una sola vez por fondo: StopBackdropTests).
            yield return HudSetup.LoadAtIntro(51);
            b = GameBootstrap.Current!.View!.StopBackdrop;
            Debug.Log("AF_FX4 51 después: build_ms " + b.BuildMs.ToString("0.###", CultureInfo.InvariantCulture) + ", warm_ms "
                + b.WarmMs.ToString("0.###", CultureInfo.InvariantCulture));
            Assert.That(b.BuildMs, Is.LessThanOrEqualTo(PerfBudget.Ms(6.5d)), PerfBudget.Describe(6.5d));
        }
    }
}
