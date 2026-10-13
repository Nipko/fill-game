#nullable enable
using System.Collections;
using System.IO;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión del Matapi y de la Liana con fruta (T12; <c>[Explicit]</c>, no forman parte de la suite):
    /// <c>55_matapi_l63</c> (el Matapi tras la intro), <c>55e_matapi_reject_l63</c> y <c>55b_matapi_reject_l63</c> (al
    /// rechazarlo como origen, a 20 y 60 ms: la sacudida de T-G en +8 y −8 dp, y el embudo latiendo, ≈ 1,04 y ≈ 1,11),
    /// <c>55c_matapi_pattern_l63</c> (con 2 dosis dentro: el patrón de la fruta se sigue leyendo sin varillas delante, al 20 %
    /// de Patrones de fruta, que en el 63 está activo, con el icono de la fruta en cada dosis; T-E),
    /// <c>55d_matapi_achiote_l63</c> (lo mismo con Achiote, que no debe leerse rayado como el Camu camu),
    /// <c>56a_matapi_cord_l63</c> (la cuerda floja, corta e inclinada, en catenaria, antes del tirón),
    /// <c>56_matapi_seal_l63</c> (a mitad del tirón: la cuerda tensa y el embudo cerrándose), <c>56d_matapi_lift_l63</c> (en
    /// el pico de la subida de la trampa, con las gotas cayendo a la repisa por fuera de la silueta),
    /// <c>56b_matapi_cap_l63</c> (la tapa y el brillo), <c>56c_matapi_sealed_l63</c> (sellado y quieto: la tapa limpia, sin
    /// el embudo debajo, y el cuello despejado), <c>57_liana_l83</c> (la Liana atada tras la intro: la hélice, el nudo y la
    /// fruta delante), <c>57b_liana_count_l83</c> (la Liana de cuenta, n = 2, en el tablero del 83: los puntos del nudo a
    /// 48 dp), <c>58_liana_check_l83</c> (la ✓ sobre la fruta y el cometa saliendo de la tapa), <c>58b_liana_check_lulo_l112</c>
    /// (la ✓ sobre el Lulo), <c>59_liana_beam_l83</c> (el cometa llegando a la fruta), <c>59b_liana_fall_l83</c> (la liana
    /// seca cayendo), <c>59d_liana_flash_row1_l84</c> y <c>59c_liana_beam_row1_l84</c> (el cometa desde una cuia de la fila
    /// de arriba a dos columnas: el destello de la tapa y la salida por encima de las bocas, y la bajada por el pasillo) y
    /// <c>59e_liana_beam_up_l85</c> (el cometa desde la fila de abajo, por delante de la repisa y subiendo por el pasillo).
    /// Sirven para mirar a 48 dp la legibilidad, los solapes, los colores y la línea D-003 (juego premium, nunca papel ni
    /// acuarela). Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.MatapiLianaSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class MatapiLianaSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureMatapiL63()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(63);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "55_matapi_l63", boot);

            // Rechazo (T-G): la sacudida de CuiaView es ShakeDp · sen(6π p) con p = t / ShakeMs, así que llega a +8 dp en
            // ShakeMs / 12 (20 ms) y a −8 dp en ShakeMs / 4 (60 ms); el embudo late a 1 + 0,15 · sen(π t / 240 ms).
            int matapi = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Matapi);
            Assert.That(boot.TapCuia(matapi).Kind, Is.EqualTo(TapKind.Rejected));
            int plusPeak = (int)(CuiaView.ShakeMs / 12f);
            int minusPeak = (int)(CuiaView.ShakeMs / 4f);
            PlayModeHelpers.TickExact(boot, plusPeak);
            Assert.That(boot.View!.Views[matapi].BodyOffset.x, Is.EqualTo(CuiaView.ShakeDp).Within(0.01f), "pico de +8 dp");
            Save(dir, "55e_matapi_reject_l63", boot);
            PlayModeHelpers.TickExact(boot, minusPeak - plusPeak);
            Assert.That(boot.View.Views[matapi].BodyOffset.x, Is.EqualTo(-CuiaView.ShakeDp).Within(0.01f), "pico de −8 dp");
            Save(dir, "55b_matapi_reject_l63", boot);
            PlayModeHelpers.Step(boot, 300);

            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.SealedMask & (1 << matapi)) != 0);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            MechanicTestKit.Tap(boot, path![path.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Seal, matapi, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Seal, matapi, 0.07f);
            Save(dir, "56a_matapi_cord_l63", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Seal, matapi, 0.22f);
            Save(dir, "56_matapi_seal_l63", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Seal, matapi, 0.4f);
            Save(dir, "56d_matapi_lift_l63", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Seal, matapi, 0.8f);
            Save(dir, "56b_matapi_cap_l63", boot);
            MechanicTestKit.StepToIdle(boot, 4000);
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "56c_matapi_sealed_l63", boot);
        }

        [UnityTest]
        public IEnumerator CaptureMatapiPatternL63()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(63);
            GameBootstrap boot = GameBootstrap.Current!;
            int matapi = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Matapi);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.Receiver == matapi && a[matapi].Count == 2);
            Assert.That(path, Is.Not.Null, "vertidos que dejan 2 dosis en el Matapi");
            MechanicTestKit.PlayPrefix(boot, path!, skipLast: false);
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "55c_matapi_pattern_l63", boot);
        }

        [UnityTest]
        public IEnumerator CaptureMatapiAchioteL63()
        {
            // El Matapi con Achiote (urucum, tinte 5: triángulos), el par rojo-rojo que confunde a un daltónico con el Camu camu
            // (rayas verticales): ninguna varilla cruza el líquido, así que los triángulos no se leen como rayas.
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(63);
            GameBootstrap boot = GameBootstrap.Current!;
            int matapi = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Matapi);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.Receiver == matapi && a[matapi].TopColor == 5 && !a.IsSealed(matapi));
            Assert.That(path, Is.Not.Null, "vertidos que dejan Achiote en el Matapi");
            MechanicTestKit.PlayPrefix(boot, path!, skipLast: false);
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "55d_matapi_achiote_l63", boot);
        }

        [UnityTest]
        public IEnumerator CaptureLianaL83()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(83);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "57_liana_l83", boot);

            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            MechanicTestKit.Tap(boot, path![path.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Release, liana, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.2f);
            Save(dir, "58_liana_check_l83", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.36f);
            Save(dir, "59_liana_beam_l83", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.84f);
            Save(dir, "59b_liana_fall_l83", boot);
            MechanicTestKit.StepToIdle(boot, 4000);
        }

        [UnityTest]
        public IEnumerator CaptureLianaCountL83()
        {
            // Liana de cuenta (n = 2; la variante del 243 no está en los niveles 1-200): el tablero del 83 con la Liana de cuenta
            // en lugar de la de fruta, en un paquete de prueba. Las cuias miden 47 dp (≈ 48, el mínimo con 10 cuias).
            string dir = SnapshotKit.Prepare();
            yield return PlayModeHelpers.LoadGameWithPack(CountLianaPack(), 83, new MemoryProgressStore(83, 82));
            yield return AttachAndSkipIntro();
            GameBootstrap boot = GameBootstrap.Current!;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            var overlay = (LianaOverlay)boot.View!.Views[liana].Overlay!;
            Assert.That(overlay.CountDots, Is.EqualTo(2), "de cuenta: 2 puntos en el nudo");
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "57b_liana_count_l83", boot);
        }

        [UnityTest]
        public IEnumerator CaptureLianaLuloL112()
        {
            // La ✓ sobre el Lulo (llave 11 en el 112, el 151 y el 180): una bola naranja con la pulpa verde arriba a la derecha.
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(112);
            GameBootstrap boot = GameBootstrap.Current!;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            Assert.That(boot.Flow!.Level.Attrs[liana].Key, Is.EqualTo(11), "la llave del 112 es el Lulo");
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            MechanicTestKit.Tap(boot, path![path.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Release, liana, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.2f);
            Save(dir, "58b_liana_check_lulo_l112", boot);
            MechanicTestKit.StepToIdle(boot, 4000);
        }

        [UnityTest]
        public IEnumerator CaptureLianaTopRowL84()
        {
            // La cuia sellada, en la fila de arriba y a dos columnas de la Liana.
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(84);
            GameBootstrap boot = GameBootstrap.Current!;
            BoardAnimator animator = boot.View!.Animator;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            int key = boot.Flow!.Level.Attrs[liana].Key;
            Vector2 lianaFoot = animator.Foot(liana);
            float u = boot.View.Views[liana].Unit;
            float slot = animator.SlotRect(liana).width;
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0
                && a.IsSealed(ev.Receiver) && a[ev.Receiver].TopColor == key && animator.Foot(ev.Receiver).y > lianaFoot.y + u
                && Mathf.Abs(Mathf.Abs(animator.Foot(ev.Receiver).x - lianaFoot.x) - (2f * slot)) < u);
            Assert.That(path, Is.Not.Null, "un taperebá sellado en la fila de arriba, a dos columnas de la Liana");
            MechanicTestKit.PlayPrefix(boot, path!);
            MechanicTestKit.Tap(boot, path![path.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Release, liana, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.15f);
            Save(dir, "59d_liana_flash_row1_l84", boot);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.28f);
            Save(dir, "59c_liana_beam_row1_l84", boot);
            MechanicTestKit.StepToIdle(boot, 4000);
        }

        [UnityTest]
        public IEnumerator CaptureLianaFromBelowL85()
        {
            string dir = SnapshotKit.Prepare();
            yield return LoadAttached(85);
            GameBootstrap boot = GameBootstrap.Current!;
            BoardAnimator animator = boot.View!.Animator;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            int key = boot.Flow!.Level.Attrs[liana].Key;
            float lianaFootY = animator.Foot(liana).y;
            float u = boot.View.Views[liana].Unit;
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0
                && a.IsSealed(ev.Receiver) && a[ev.Receiver].TopColor == key && animator.Foot(ev.Receiver).y < lianaFootY - u);
            Assert.That(path, Is.Not.Null, "un taperebá sellado en la fila de abajo");
            MechanicTestKit.PlayPrefix(boot, path!);
            MechanicTestKit.Tap(boot, path![path.Length - 1]);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Release, liana, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.28f);
            Save(dir, "59e_liana_beam_up_l85", boot);
            MechanicTestKit.StepToIdle(boot, 4000);
        }

        // Carga el nivel con el lienzo en la cámara de las capturas, quita el reloj, salta la intro y espera la vista quieta.
        private static IEnumerator LoadAttached(int level)
        {
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            yield return AttachAndSkipIntro();
        }

        private static IEnumerator AttachAndSkipIntro()
        {
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            boot.View!.Animator.Steps.ClearLog();
            yield return null;
        }

        // El nivel 83 de mundo_04_v1 (pack_schema 2) con la Liana de cuenta (n = 2) en lugar de la de fruta (key = 3).
        private static string CountLianaPack()
        {
            return "{ \"pack_schema\": 2, \"pack_id\": \"test_liana_count\", \"kind\": \"campaign\", \"world\": 4, \"world_id\": \"w04_trapecio\", "
                + "\"lap\": 1, \"water_base\": \"white\", \"levels\": [ { \"schema\": \"af-level/1\", \"id\": \"m04_v1_n0083\", \"level\": 83, "
                + "\"world\": 4, \"lap\": 1, \"water_base\": \"white\", "
                + "\"palette\": [\"cupuacu\", \"tapereba\", \"aguaje\", \"camu_camu\", \"bacaba\", \"acai\", \"agua_tapajos\", \"agua_negro\"], "
                + "\"units_per_color\": 4, \"caps\": [4, 4, 4, 4, 4, 4, 4, 4, 4, 4], "
                + "\"cuias\": [[7, 7, 7, 6], [4, 4, 4, 6], [], [], [10, 8, 3, 2], [2, 14, 14, 3], [14, 14, 2, 2], [8, 10, 10, 8], [3, 3, 10, 8], [6, 6, 4, 7]], "
                + "\"attrs\": [null, null, null, null, null, null, null, null, null, { \"type\": \"liana\", \"n\": 2 }], "
                + "\"murky\": false, \"hidden\": [], \"difficulty\": { \"label\": \"normal\", \"par\": 17 }, \"intro_mechanic\": null, \"relief\": [] } ] }";
        }

        private static void Save(string dir, string name, GameBootstrap boot)
        {
            SnapshotKit.Save(dir, name);
            SnapshotKit.LogBoard(name, boot);
            Assert.That(File.Exists(Path.Combine(dir, name + ".png")), Is.True, name);
        }
    }
}
