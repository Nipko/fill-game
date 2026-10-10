#nullable enable
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
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
    /// Capturas de revisión de la Hoja y el Brote (T11; <c>[Explicit]</c>, no forman parte de la suite):
    /// <c>50_hoja_closed_l43</c> (la hoja de palma cerrada con el aguaje, tras la intro), <c>50b_hoja_key_l43</c> (t = 0,32:
    /// la llave encajada, a 1,3× y opaca, con la boca ya libre del origen), <c>50c_hoja_keylight_l43</c> (t = 0,45: la llave
    /// se deshace en luz con sus destellos), <c>50d_hoja_key_row1_l118</c> (t = 0,35: la llave encajada en una Hoja de la fila
    /// 1 de un tablero de 16 cuias en 6/5/5, para ver que el medallón no invade el marco del panel),
    /// <c>50e_hoja_key_row1_l198</c> (t = 0,35: lo mismo en el 198 real, 11 cuias en 4/4/3 con cuias de 47 dp, el hueco más
    /// justo bajo el marco, y el lulo de llave, de las frutas que más suben con el giro), <c>51_hoja_falling_l43</c>
    /// (t = 0,55: la hoja abierta cae girando al agua), <c>51b_hoja_splash_l43</c> (t = 0,87: la salpicadura ya abierta
    /// mientras la hoja se acaba de aclarar en el agua), <c>52_brote_closed_l163</c> (el tapón-brote de camu camu con sus dos
    /// hojitas plegadas), <c>53_brote_open_l163</c> (t = 0,42: a mitad de la germinación) y <c>53b_brote_end_l163</c>
    /// (t = 0,85: el brote abierto en V, ya desvaneciéndose, con el origen de vuelta en su ranura). Sirven para mirar a 48-56
    /// dp la legibilidad, los solapes con las vecinas y con la fila de arriba, los colores, que se entienda la mecánica, que
    /// la Hoja no se confunda con la Hoja escudo y la línea D-003. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode
    /// PlayMode --filter AF.Tests.PlayMode.HojaBroteSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en
    /// <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class HojaBroteSnapshotTests
    {
        // Nivel del paquete de prueba de 16 cuias (rango del mundo 4, con tableros de 3 filas y Hojas desde el 86).
        private const int RowOneLevel = 118;

        // Cuia de la Hoja en ese paquete: la tercera de la fila 1 (6/5/5, D-006).
        private const int RowOneHoja = 2;

        // Cuia con el aguaje arriba en ese paquete (fila 2): su primer vertido abre la Hoja.
        private const int RowOneSource = 9;

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureHoja()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureClosed(dir, 43, "50_hoja_closed_l43");
            yield return CaptureMidOpen(dir, 43, MechanicType.Hoja, "50b_hoja_key_l43", 0.32f);
            yield return CaptureMidOpen(dir, 43, MechanicType.Hoja, "50c_hoja_keylight_l43", 0.45f);
            yield return CaptureMidOpen(dir, 43, MechanicType.Hoja, "51_hoja_falling_l43", 0.55f);
            yield return CaptureMidOpen(dir, 43, MechanicType.Hoja, "51b_hoja_splash_l43", 0.87f);
        }

        [UnityTest]
        public IEnumerator CaptureHojaRowOne()
        {
            // La Hoja en la fila 1 de un tablero de 3 filas (6/5/5) en el pico del salto de la llave: el medallón no debe
            // invadir el marco del panel (ni, en las filas 2-3, la repisa de la fila de arriba).
            string dir = SnapshotKit.Prepare();
            LaunchOptions.LevelPackJsonOverride = HojaRowOnePack();
            yield return CaptureMidOpen(dir, RowOneLevel, MechanicType.Hoja, "50d_hoja_key_row1_l118", 0.35f, RowOneSource);
            CuiaView[] views = GameBootstrap.Current!.View!.Views;
            Assert.That(views.Length, Is.EqualTo(16));
            float rowOne = views[0].FootPosition.y;
            Assert.That(Mathf.Abs(views[5].FootPosition.y - rowOne), Is.LessThan(0.5f), "6 cuias en la fila 1, con la Hoja");
            Assert.That(Mathf.Abs(views[6].FootPosition.y - rowOne), Is.GreaterThan(0.5f), "la 7.ª ya va en la fila 2");

            // El caso más justo de los niveles reales: con 6 por fila las cuias son estrechas (39 dp) y sobra sitio bajo el
            // marco; con 11-12 cuias en 3 filas de 4 son de 47 dp y quedan 68 px. El 198 tiene la Hoja en la fila 1 y el lulo de
            // llave, al que el giro sube el cáliz hasta el borde de su celda: sin el tope de la fruta, el cáliz tocaba el marco.
            LaunchOptions.LevelPackJsonOverride = null;
            yield return CaptureMidOpen(dir, 198, MechanicType.Hoja, "50e_hoja_key_row1_l198", 0.35f);
            views = GameBootstrap.Current!.View!.Views;
            Assert.That(views.Length, Is.EqualTo(11));
            rowOne = views[0].FootPosition.y;
            Assert.That(Mathf.Abs(views[1].FootPosition.y - rowOne), Is.LessThan(0.5f), "la Hoja del 198, en la fila 1");
            Assert.That(Mathf.Abs(views[4].FootPosition.y - rowOne), Is.GreaterThan(0.5f), "4 cuias en la fila 1 (4/4/3)");
        }

        [UnityTest]
        public IEnumerator CaptureBrote()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureClosed(dir, 163, "52_brote_closed_l163");
            yield return CaptureMidOpen(dir, 163, MechanicType.Brote, "53_brote_open_l163", 0.42f);
            yield return CaptureMidOpen(dir, 163, MechanicType.Brote, "53b_brote_end_l163", 0.85f);
        }

        // Carga el nivel, salta la intro y captura con la vista quieta.
        private static IEnumerator CaptureClosed(string dir, int level, string name)
        {
            yield return Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 300);
            SnapshotKit.Save(dir, name);
            SnapshotKit.LogBoard(name, boot);
            Assert.That(File.Exists(Path.Combine(dir, name + ".png")), Is.True);
        }

        // Juega hasta el vertido que abre la mecánica (o, con source ≥ 0, vierte directamente de source a la mecánica) y
        // captura a la fracción pedida de su paso Open.
        private static IEnumerator CaptureMidOpen(string dir, int level, MechanicType type, string name, float fraction, int source = -1)
        {
            yield return Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            int owner = MechanicTestKit.IndexOf(boot.Flow!, type);
            int bit = 1 << owner;
            Pour opening;
            if (source >= 0)
            {
                opening = new Pour(source, owner, 1);
            }
            else
            {
                Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ((type == MechanicType.Hoja ? ev.HojaOpenedMask : ev.BroteOpenedMask) & bit) != 0);
                Assert.That(path, Is.Not.Null, name);
                MechanicTestKit.PlayPrefix(boot, path!);
                opening = path![path.Length - 1];
            }

            TapResult poured = MechanicTestKit.Tap(boot, opening);
            Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured), name + ": vierte");
            Assert.That(((type == MechanicType.Hoja ? poured.Pour.Events.HojaOpenedMask : poured.Pour.Events.BroteOpenedMask) & bit) != 0, Is.True, name + ": y abre la mecánica");
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Open, owner, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, owner, fraction);
            SnapshotKit.Save(dir, name);
            SnapshotKit.LogBoard(name, boot);
            if (boot.View!.Views[owner].Overlay is HojaOverlay leaf)
            {
                // Borde de arriba del medallón y lo más alto de la fruta girada sobre lo alto de la cuia, en u y en px de la captura
                // (3 px por dp a 1080 de ancho).
                float u = boot.View.Views[owner].Unit;
                float top = leaf.KeyTopAboveLipU;
                float fruitTop = leaf.FruitTopAboveLipU;
                TestContext.WriteLine("AF_KEY " + name + ": t = " + leaf.StepProgress.ToString("0.000", CultureInfo.InvariantCulture)
                    + ", escala " + leaf.FruitScale.ToString("0.00", CultureInfo.InvariantCulture)
                    + ", borde del medallón " + top.ToString("0.000", CultureInfo.InvariantCulture) + " u = "
                    + (top * u * 3f).ToString("0.0", CultureInfo.InvariantCulture) + " px y fruta "
                    + fruitTop.ToString("0.000", CultureInfo.InvariantCulture) + " u = "
                    + (fruitTop * u * 3f).ToString("0.0", CultureInfo.InvariantCulture) + " px sobre los corchos (u = "
                    + u.ToString("0.0", CultureInfo.InvariantCulture) + " dp)");
            }

            Assert.That(File.Exists(Path.Combine(dir, name + ".png")), Is.True);
        }

        private static IEnumerator Load(int level)
        {
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }
        }

        // Paquete af-level/1 de un nivel con 16 cuias de capacidad 4 (6/5/5, D-006): la Hoja de aguaje en la cuia 2 (fila 1),
        // una vacía en la 15 y en las otras 14 los 14 tintes del glosario §9 en escalera (la cuia llena j lleva j, j + 3, j + 6
        // y j + 9: 4 dosis de cada tinte). La llena 8 (cuia 9, fila 2) tiene el aguaje arriba: el primer vertido abre la Hoja
        // desde la fila de abajo, como en el 43.
        private static string HojaRowOnePack()
        {
            string[] ids = { "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo", "agua_tapajos", "lulo", "araza", "cacao", "agua_negro" };
            const int count = 16;
            const int aguaje = 4;
            var cuias = new StringBuilder();
            var caps = new StringBuilder();
            var attrs = new StringBuilder();
            int filled = 0;
            for (int i = 0; i < count; i++)
            {
                string sep = i == 0 ? string.Empty : ", ";
                caps.Append(sep).Append('4');
                attrs.Append(sep).Append(i == RowOneHoja ? "{ \"type\": \"hoja\", \"key\": " + aguaje + " }" : "null");
                cuias.Append(sep).Append('[');
                if (i != RowOneHoja && i != count - 1)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        cuias.Append(k == 0 ? string.Empty : ", ").Append(((filled + (3 * k)) % ids.Length) + 1);
                    }

                    filled++;
                }

                cuias.Append(']');
            }

            string id = "m04_v1_n" + RowOneLevel.ToString("0000", CultureInfo.InvariantCulture);
            return "{ \"subset_of\": \"af-level/1\", \"pack_id\": \"test_" + id + "\", \"kind\": \"campaign\", \"world\": 4, "
                + "\"world_id\": \"w04_trapecio\", \"lap\": 1, \"water_base\": \"white\", \"levels\": [ { \"id\": \"" + id + "\", "
                + "\"level\": " + RowOneLevel + ", \"world\": 4, \"lap\": 1, \"water_base\": \"white\", \"palette\": [\""
                + string.Join("\", \"", ids) + "\"], \"units_per_color\": 4, \"caps\": [" + caps + "], \"cuias\": [" + cuias
                + "], \"attrs\": [" + attrs + "], \"murky\": false, \"hidden\": [], \"difficulty\": { \"par\": 1 }, \"relief\": [] } ] }";
        }
    }
}
