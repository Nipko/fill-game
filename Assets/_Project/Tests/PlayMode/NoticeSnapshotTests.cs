#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de los avisos legibles (D-011, puntos 2 y 3; <c>[Explicit]</c>, no forman parte de la suite):
    /// <c>a2_01_capi_gift_l8</c> (el regalo de Capi al habilitarse el toque, con la tropa aún en la rama),
    /// <c>a2_02_sighting_l5</c>, <c>a2_02b_sighting_l42</c> y <c>a2_02c_sighting_l142</c> (la tarjeta «¡Miquito a la vista!»
    /// cuando la tropa ya se ha ido, en la franja del panorama; en el 42 tras la intro corta y en el 142 con sus 10 cuias en 2
    /// filas), <c>a2_02d_sighting_rows3_11</c> y <c>a2_02e_sighting_rows3_16</c> (un tablero de prueba de 3 filas en el 142, 11
    /// y 16 cuias: la franja mide &lt; 64 dp y la tarjeta va sobre el muelle o, si no cabe, no sale; el sufijo de la captura dice
    /// cuál), <c>a2_03_toast_after_card_l5</c> (el aviso que esperaba a que se fuera la tarjeta) y, en su propia prueba,
    /// <c>a2_04_sighting_pour_l5_overlap</c> y <c>a2_04b_sighting_pour_l5_92</c> (la tarjeta a la vista durante el vertido de
    /// la fila de arriba que vacía el origen: el fotograma en que el origen entra más en la tarjeta y el de la inclinación
    /// máxima, 92°; el origen, delante de la tarjeta, D-006). Sirven para mirar a 48 dp
    /// la legibilidad, los solapes con la tropa, las cuias, el HUD y la bandeja, y la línea de D-003. Necesitan gráficos:
    /// <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.NoticeSnapshotTests</c> sin <c>-nographics</c>; los
    /// PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class NoticeSnapshotTests
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
        public IEnumerator CaptureNotices()
        {
            string dir = SnapshotKit.Prepare();

            // El regalo de Capi en el 8: sale al habilitarse el toque, abajo, mientras la tropa reposa arriba.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(8, new MemoryProgressStore(8, 7));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            StepToPlaying(boot);
            PlayModeHelpers.Step(boot, 800);
            Assert.That(boot.View!.ToastText, Is.EqualTo(UiText.ExtraCuiaGift));
            AssertNoGhostCuias(boot.View, "a2_01_capi_gift_l8");
            SnapshotKit.Save(dir, "a2_01_capi_gift_l8");

            // La tarjeta del miquito, cuando la tropa se ha ido, en la franja del panorama (larga en el 5, corta en el 42 y el 142
            // con 10 cuias en 2 filas).
            yield return CaptureSighting(dir, 5, "a2_02_sighting_l5");
            boot = GameBootstrap.Current!;

            // Un aviso que llega con la tarjeta a la vista sale cuando ella se va («¡Una cuia más!», un aviso que sigue existiendo
            // tras la pregunta 48 (b)).
            boot.View!.ShowNotice(UiText.RescueApplied);
            Assert.That(boot.View.ToastText, Is.Null);
            for (int t = 0; t < 8000 && boot.View.Sighting.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 400);
            Assert.That(boot.View.ToastText, Is.EqualTo(UiText.RescueApplied));
            AssertNoGhostCuias(boot.View, "a2_03_toast_after_card_l5");
            SnapshotKit.Save(dir, "a2_03_toast_after_card_l5");

            yield return CaptureSighting(dir, 42, "a2_02b_sighting_l42");
            yield return CaptureSighting(dir, 142, "a2_02c_sighting_l142");
            Assert.That(File.Exists(Path.Combine(dir, "a2_02c_sighting_l142.png")), Is.True);

            // Con 3 filas (D-006: desde 11 cuias) la franja del panorama mide < 64 dp: la tarjeta va sobre el muelle, bajo la
            // última fila y por encima de la bandeja, o no sale si no cabe. Ningún nivel_tarjeta del catálogo tiene hoy 3 filas,
            // así que se mira con un tablero de prueba en el 142 (mundo 5). A 360 × 780 dp (2026-10-12) no cabe en ninguno de
            // los dos: el panel acaba en 628,5 dp con 11 cuias y en 596,1 dp con 16, y la tarjeta (60 dp, 4 dp bajo el panel)
            // tiene que acabar ≤ 8 dp sobre la bandeja (660 dp).
            yield return CaptureSightingOnThreeRows(dir, 11, "a2_02d_sighting_rows3_11");
            yield return CaptureSightingOnThreeRows(dir, 16, "a2_02e_sighting_rows3_16");
            LaunchOptions.LevelPackJsonOverride = null;
        }

        [UnityTest]
        public IEnumerator CaptureSightingDuringATopRowPour()
        {
            // Revisión de A2: D-011 alarga la tarjeta a ~5 s y la saca ya en juego. En el 5, con la tarjeta a la vista en la
            // franja del panorama, el vertido entre dos cuias de la fila de arriba que más sube el origen: el que lo vacía, que
            // acaba a 92° (D-006: 92° − 9° × dosis que quedan), casi tumbado y con su boca 0,85 u sobre el borde del destino.
            // Se mide en todo el vertido cuánto sube el origen y si entra en la tarjeta, y se captura el fotograma en que más
            // entra y el de la inclinación máxima. El origen que vierte va delante de todo (D-006): también de la tarjeta, que
            // queda delante del HUD.
            string dir = SnapshotKit.Prepare();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(5, new MemoryProgressStore(5, 4));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.Flow!.Load(5);
            yield return null; // las vistas de la primera carga se destruyen al final del fotograma (AssertNoGhostCuias)
            StepToPlaying(boot);
            GameScreen view = boot.View!;
            LevelSession session = boot.Flow.Session;
            Assert.That(view.Layout!.Rows, Is.EqualTo(2));

            // El vertido de la fila de arriba que vacía el origen con menos vertidos previos; estos se juegan al instante
            // (SnapAll), mientras la tropa aún reposa en la rama.
            int source = -1;
            int target = -1;
            Pour[]? setup = null;
            for (int a = 0; a < view.Views.Length; a++)
            {
                for (int b = 0; b < view.Views.Length; b++)
                {
                    if (a == b || view.Layout.RowOf[a] != 0 || view.Layout.RowOf[b] != 0)
                    {
                        continue;
                    }

                    Pour[]? found = FindEmptyingSetup(session.Current, a, b, session.PourMode, session.FutileRuleEnabled);
                    if (found != null && (setup == null || found.Length < setup.Length))
                    {
                        setup = found;
                        source = a;
                        target = b;
                    }
                }
            }

            Assert.That(setup, Is.Not.Null, "algún vertido de la fila de arriba que vacíe el origen");
            for (int i = 0; i < setup!.Length; i++)
            {
                Assert.That(boot.TapCuia(setup[i].Source).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(boot.TapCuia(setup[i].Target).Kind, Is.EqualTo(TapKind.Poured));
                view.Animator.SnapAll();
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            for (int t = 0; t < 8000 && !view.Sighting.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.Sighting.Visible, Is.True, "la tarjeta, a la vista");
            PlayModeHelpers.Step(boot, 400);
            var canvas = (RectTransform)view.Canvas.transform;
            Rect card = VictorySetup.CanvasRect(view.Sighting.Root, canvas); // la placa; su canto asoma 3 dp por debajo

            CuiaView s = view.Views[source];
            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(target).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(session.Current[source].IsEmpty, Is.True, "el vertido vacía el origen");
            float maxTilt = 0f;
            float top = float.MaxValue; // lo más alto del origen, en dp desde arriba del lienzo
            float topTilt = 0f;
            int maxInside = 0;
            byte[]? overlap = null;
            bool captured = false;
            int elapsed = 0;
            while (!view.IsViewIdle)
            {
                Assert.That(elapsed, Is.LessThan(4000), "la cola visual no se vació");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                elapsed += PlayModeHelpers.FrameMs;
                if (s.Body.parent != view.PourFront)
                {
                    continue;
                }

                float tilt = Mathf.Abs(s.BodyAngle);
                if (!captured && tilt < maxTilt - 0.01f)
                {
                    // Empieza la vuelta: el fotograma anterior fue el de la inclinación máxima y este apenas se mueve.
                    Assert.That(view.Sighting.Visible, Is.True, "la tarjeta sigue a la vista con el origen inclinado del todo");
                    AssertNoGhostCuias(view, "a2_04b_sighting_pour_l5_92");
                    SnapshotKit.Save(dir, "a2_04b_sighting_pour_l5_92");
                    captured = true;
                }

                maxTilt = Mathf.Max(maxTilt, tilt);
                float bodyTop = VictorySetup.CanvasRect(s.Body, canvas).yMin;
                if (bodyTop < top)
                {
                    top = bodyTop;
                    topTilt = tilt;
                }

                int inside = PointsInside(s.Body, canvas, card);
                if (inside > maxInside)
                {
                    Assert.That(view.Sighting.Visible, Is.True, "la tarjeta sigue a la vista con el origen dentro");
                    maxInside = inside;
                    AssertNoGhostCuias(view, "a2_04_sighting_pour_l5_overlap");
                    overlap = SnapshotKit.Capture(); // el fotograma en que el origen entra más en la tarjeta
                }
            }

            if (overlap != null)
            {
                File.WriteAllBytes(Path.Combine(dir, "a2_04_sighting_pour_l5_overlap.png"), overlap);
            }

            Transform cardLayer = view.Sighting.Root;
            while (cardLayer.parent != view.PourFront.parent)
            {
                cardLayer = cardLayer.parent;
            }

            System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
            TestContext.WriteLine("a2_04_sighting_pour_l5: " + source + " → " + target + " vacía el origen (" + setup.Length
                + " vertidos previos); inclinación máxima " + maxTilt.ToString("0.0", inv) + "°; lo más alto del origen a "
                + top.ToString("0.0", inv) + " dp (a " + topTilt.ToString("0.0", inv) + "°); placa de la tarjeta de "
                + card.yMin.ToString("0.0", inv) + " a " + card.yMax.ToString("0.0", inv) + " dp (canto hasta "
                + (card.yMax + 3f).ToString("0.0", inv) + "); puntos del origen dentro de la placa: " + maxInside + "/105; capa de la tarjeta "
                + cardLayer.name + " (" + cardLayer.GetSiblingIndex() + "), capa de vertido " + view.PourFront.GetSiblingIndex());
            Assert.That(captured, Is.True, "se capturó la inclinación máxima");
            Assert.That(maxTilt, Is.GreaterThan(90f), "el origen vacío acaba a 92°");
            Assert.That(cardLayer.GetSiblingIndex(), Is.LessThan(view.PourFront.GetSiblingIndex()), "la tarjeta, detrás del origen que vierte (D-006)");
            Assert.That(cardLayer.GetSiblingIndex(), Is.GreaterThan(view.Hud.GetSiblingIndex()), "y delante del HUD");
        }

        // Puntos de una rejilla de 21 × 5 sobre el cuerpo de rt (de punta a punta y del 10 al 90 % del ancho) que caen dentro
        // de rect (dp desde arriba a la izquierda del lienzo).
        private static int PointsInside(RectTransform rt, RectTransform canvas, Rect rect)
        {
            Rect r = rt.rect;
            Rect c = canvas.rect;
            int inside = 0;
            for (int i = 0; i <= 20; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Vector3 world = rt.TransformPoint(new Vector3(r.xMin + (r.width * (0.1f + (0.2f * j))), r.yMin + (r.height * i / 20f), 0f));
                    Vector3 local = canvas.InverseTransformPoint(world);
                    if (rect.Contains(new Vector2(local.x - c.xMin, c.yMax - local.y)))
                    {
                        inside++;
                    }
                }
            }

            return inside;
        }

        // Vertidos más cortos tras los que source → target es legal y vacía source sin ganar el nivel (BFS como
        // PlayModeHelpers.FindSetup; los estados ganados o bloqueados no sirven de paso intermedio); null si no aparece.
        private static Pour[]? FindEmptyingSetup(BoardState state, int source, int target, PourMode mode, bool futileRule)
        {
            var parents = new Dictionary<BoardState, (BoardState? Parent, Pour Move)>();
            var queue = new Queue<BoardState>();
            parents.Add(state, (null, default));
            queue.Enqueue(state);
            int n = state.Count;
            while (queue.Count > 0 && parents.Count < 20000)
            {
                BoardState current = queue.Dequeue();
                if (PourRules.IsLegal(current, source, target))
                {
                    BoardState after = PourRules.Apply(current, source, target, mode);
                    if (after[source].IsEmpty && !after.IsWon())
                    {
                        var path = new List<Pour>();
                        for (BoardState? at = current; at != null && parents[at].Parent != null; at = parents[at].Parent)
                        {
                            path.Add(parents[at].Move);
                        }

                        path.Reverse();
                        return path.ToArray();
                    }
                }

                for (int a = 0; a < n; a++)
                {
                    for (int b = 0; b < n; b++)
                    {
                        if (a == b || !PourRules.IsLegal(current, a, b))
                        {
                            continue;
                        }

                        BoardState next = PourRules.Apply(current, a, b, mode);
                        if (parents.ContainsKey(next) || next.IsWon() || PourRules.IsStuck(next, futileRule))
                        {
                            continue;
                        }

                        parents.Add(next, (current, new Pour(a, b, PourRules.Amount(current, a, b, mode))));
                        queue.Enqueue(next);
                    }
                }
            }

            return null;
        }

        // El 142 con un tablero de prueba de 3 filas: espera a que la tropa se vaya y captura la tarjeta sobre el muelle
        // («_dock») o el tablero sin ella si no cabe («_nofit»).
        private static IEnumerator CaptureSightingOnThreeRows(string dir, int cuias, string name)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGameWithPack(ThreeRowPack(cuias), 142, new MemoryProgressStore(142, 141));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.Flow!.Load(142);
            yield return null; // las vistas de la primera carga se destruyen al final del fotograma (AssertNoGhostCuias)
            StepToPlaying(boot);
            GameScreen view = boot.View!;
            Assert.That(view.Views.Length, Is.EqualTo(cuias), name);
            for (int t = 0; t < 5000 && view.Intro.TroopVisible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 3 * PlayModeHelpers.FrameMs);
            float hudBottom = view.Intro.Input.HudBottom;
            float panelTop = view.Intro.Input.PanelTop;
            float panelBottom = view.Intro.Input.PanelBottom;
            Assert.That(panelTop - hudBottom, Is.LessThan(64f), name + ": con 3 filas la franja mide < 64 dp");
            Assert.That(view.SightingPending, Is.False, name + ": la tropa ya se fue");
            string suffix;
            if (view.Sighting.Visible)
            {
                RectTransform root = view.Sighting.Root;
                float top = -root.anchoredPosition.y;
                Assert.That(top, Is.GreaterThanOrEqualTo(panelBottom), name + ": sobre el muelle, bajo la última fila");
                PlayModeHelpers.Step(boot, 400);
                suffix = "_dock";
            }
            else
            {
                Assert.That(view.NoticesQueued, Is.EqualTo(0), name + ": si no cabe, no sale ni espera");
                suffix = "_nofit";
            }

            AssertNoGhostCuias(view, name + suffix);
            SnapshotKit.Save(dir, name + suffix);
            SnapshotKit.LogBoard(name, boot);
            TestContext.WriteLine(name + suffix + ": franja " + (panelTop - hudBottom) + " dp, fondo del panel " + panelBottom + " dp, tarjeta "
                + (view.Sighting.Visible ? (-view.Sighting.Root.anchoredPosition.y).ToString(System.Globalization.CultureInfo.InvariantCulture) : "no"));
        }

        // Paquete de un nivel (el 142, mundo 5) con <cuias> cuias: cuias − 2 tintes de 4 dosis en escalera (la cuia i lleva los
        // tintes i … i + 3) y 2 vacías.
        private static string ThreeRowPack(int cuias)
        {
            string[] all = { "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo", "agua_tapajos", "lulo", "araza", "cacao", "agua_negro" };
            int tints = cuias - 2;
            var ids = new string[tints];
            System.Array.Copy(all, ids, tints);
            var board = new int[cuias][];
            for (int i = 0; i < cuias; i++)
            {
                board[i] = i < tints ? new[] { (i % tints) + 1, ((i + 1) % tints) + 1, ((i + 2) % tints) + 1, ((i + 3) % tints) + 1 } : new int[0];
            }

            return PlayModeHelpers.BoardPack(5, "w05_loreto", "white", 142, ids, board);
        }

        // Carga el nivel, deja la intro entera, espera a que la tropa se vaya y captura la tarjeta ya entrada (400 ms).
        private static IEnumerator CaptureSighting(string dir, int level, string name)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.Flow!.Load(level);
            yield return null; // las vistas de la primera carga se destruyen al final del fotograma (AssertNoGhostCuias)
            StepToPlaying(boot);
            for (int t = 0; t < 5000 && !boot.View!.Sighting.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.View!.Sighting.Visible, Is.True, name);
            Assert.That(boot.View.Intro.TroopVisible, Is.False, name + ": la tropa ya se fue");
            PlayModeHelpers.Step(boot, 400);
            AssertNoGhostCuias(boot.View, name);
            SnapshotKit.Save(dir, name);
            TestContext.WriteLine(name + ": " + boot.View.Sighting.Name + " · " + boot.View.Sighting.Fact + " · " + boot.View.Sighting.DurationMs + " ms");
        }

        // Antes de cada captura, tantas CuiaView en el tablero (y en la capa de la cuia elevada) como vistas tiene la pantalla.
        // GameScreen.BuildBoard destruye las de la carga anterior con Object.Destroy, que espera al final del fotograma, y
        // PlayModeHelpers.Step y SnapshotKit.Capture (camera.Render) van en el mismo fotograma: sin un yield tras Flow.Load, las
        // vistas viejas se dibujan debajo de las nuevas (revisión de A2: a2_04 tenía 7 cuias en un nivel de 6).
        private static void AssertNoGhostCuias(GameScreen view, string name)
        {
            int count = view.BoardContainer.GetComponentsInChildren<CuiaView>(true).Length
                + view.LiftFront.GetComponentsInChildren<CuiaView>(true).Length;
            Assert.That(count, Is.EqualTo(view.Views.Length), name + ": sin cuias de la carga anterior");
        }

        private static void StepToPlaying(GameBootstrap boot)
        {
            for (int t = 0; t < 5000 && boot.Flow!.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }
    }
}
