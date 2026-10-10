#nullable enable
using System.Collections;
using System.IO;
using AF.GameFlow;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de las etiquetas de D-009 (tarea T08; propuesta-mecanicas-dificultad.md §c.1-c.2; 07 §7.7), con el
    /// catálogo real y la pantalla simulada de <see cref="SnapshotKit"/> (360 × 780 dp): <c>40_play_hard_l40</c> (inicio del 40,
    /// chip ámbar con la rana y «×3»), <c>41_play_veryhard_l60</c> (inicio del 60, chip índigo con el jaguar, «×4» y Jugar
    /// nocturno), <c>42_failed_hard_l40</c> (Nivel fallido del 40 con la cabecera ámbar) y <c>43_giveup_veryhard_l60</c>
    /// (¿Seguro? del 60 con la cabecera índigo). Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.LabelSnapshotTests</c> sin <c>-nographics</c>; los PNG van a <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class LabelSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureLabels()
        {
            string dir = SnapshotKit.Prepare();

            // 40 y 41: el inicio con el chip de Difícil y el de Muy difícil con Jugar nocturno.
            yield return LabelFunnel.LoadLobby(40);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "40_play_hard_l40");

            yield return LabelFunnel.LoadLobby(60);
            boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "41_play_veryhard_l60");

            // 42: Nivel fallido del 40 (pausa › Salir, fallo duro desde el 31).
            yield return LabelFunnel.LoadLevel(40);
            boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            LabelFunnel.QuitToLevelFailed(boot);
            PlayModeHelpers.Step(boot, 400);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "42_failed_hard_l40");

            // 43: ¿Seguro? del 60 tras bloquear el tablero.
            yield return LabelFunnel.LoadLevel(60);
            boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            LabelFunnel.GetStuck(boot);
            Assert.That(boot.PressGiveUp(), Is.True);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            PlayModeHelpers.Step(boot, 400);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "43_giveup_veryhard_l60");
            Assert.That(File.Exists(Path.Combine(dir, "43_giveup_veryhard_l60.png")), Is.True);
        }
    }
}
