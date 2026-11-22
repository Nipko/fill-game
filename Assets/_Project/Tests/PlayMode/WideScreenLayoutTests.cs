#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.LiveOps;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Borde a borde y pantallas grandes (añadido de D-059 del 2026-11-06; 02 §5; 08 §13.1 #4): con la pantalla de un teléfono, un
    /// plegable abierto, una tableta en vertical y en apaisado, un teléfono en apaisado con recorte y barra de navegación de lado
    /// y media pantalla dividida, todo lo que se toca del inicio, el nivel (con la pausa), el Mapa, el Mercado, la Liga y Ajustes
    /// queda dentro de la columna (<see cref="ScreenFrame"/>) y de la zona segura (nada bajo la barra de estado, la de
    /// navegación ni un recorte), y el agua del río llena el lienzo de borde a borde. Al girar y al redimensionar con un nivel o
    /// el Mercado abiertos, todo se vuelve a colocar, y al volver a la pantalla de antes, en su sitio de antes.
    /// Las capturas, en <see cref="WideScreenSnapshotTests"/>.
    /// </summary>
    public sealed class WideScreenLayoutTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc);

        /// <summary>Una pantalla de prueba: px, márgenes de arriba y abajo (px), de los lados (px) y px por dp físico.</summary>
        public readonly struct Device
        {
            public Device(string name, Vector4 screen, Vector2 sides, float pxPerDp)
            {
                Name = name;
                Screen = screen;
                Sides = sides;
                PxPerDp = pxPerDp;
            }

            public string Name { get; }

            public Vector4 Screen { get; }

            public Vector2 Sides { get; }

            public float PxPerDp { get; }

            public void Use()
            {
                GameScreen.SimulatedScreen = Screen;
                GameScreen.SimulatedSideInsets = Sides;
                GameScreen.SimulatedPxPerDp = PxPerDp;
            }

            public override string ToString() => Name;
        }

        internal static readonly Device Phone = new Device("telefono", new Vector4(1080f, 2340f, 96f, 48f), Vector2.zero, 3f);

        internal static readonly Device Foldable = new Device("plegable_abierto", new Vector4(2208f, 1840f, 110f, 60f), Vector2.zero, 2.625f);

        internal static readonly Device TabletPortrait = new Device("tableta_vertical", new Vector4(1600f, 2560f, 48f, 96f), Vector2.zero, 2f);

        internal static readonly Device TabletLandscape = new Device("tableta_apaisada", new Vector4(2560f, 1600f, 48f, 96f), Vector2.zero, 2f);

        internal static readonly Device PhoneLandscape = new Device("telefono_apaisado", new Vector4(2340f, 1080f, 0f, 0f), new Vector2(130f, 144f), 2.75f);

        internal static readonly Device SplitScreen = new Device("pantalla_dividida", new Vector4(1080f, 1150f, 96f, 0f), Vector2.zero, 2.625f);

        internal static readonly Device[] All = { Phone, Foldable, TabletPortrait, TabletLandscape, PhoneLandscape, SplitScreen };

        private static IEnumerable<Device> Devices() => All;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetLeagueOverrides();
            GameBootstrap.ResetMarketOverrides();
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedSideInsets = null;
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
        }

        // ------------------------------------------------------------------------------- el inicio y sus pantallas

        [UnityTest]
        public IEnumerator Lobby_Map_Market_League_Settings_InsideTheColumnAndTheSafeArea([ValueSource(nameof(Devices))] Device device)
        {
            device.Use();
            yield return LoadLobby();
            GameBootstrap boot = GameBootstrap.Current!;
            ScreenFrame frame = ScreenFrame.Current;
            TestContext.WriteLine("AF_WIDE " + device + ": " + frame);
            AssertFrame(frame, device);

            AssertInside(boot.Lobby!.Canvas, frame, device + ", el inicio", 3);
            AssertBleeds(boot.Lobby.Canvas, "Scene/BaseFill", frame, device + ", el agua del inicio");

            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True, "el Mapa");
            yield return Frames();
            AssertInside(boot.RiverMapView!.Canvas, frame, device + ", el Mapa", 1);
            AssertBleeds(boot.RiverMapView.Canvas, "Background", frame, device + ", el fondo del Mapa");
            Assert.That(boot.CloseRiverMap(), Is.True);
            yield return Frames();

            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True, "el Mercado");
            yield return Frames();
            AssertInside(boot.Market!.Canvas, frame, device + ", el Mercado", 2);
            AssertBleeds(boot.Market.Canvas, "Dim", frame, device + ", el velo del Mercado");
            boot.Market.Close();
            yield return Frames();

            Assert.That(boot.OpenLeague(), Is.True, "la Liga");
            boot.TickLeague(16f);
            yield return Frames();
            AssertInside(boot.LeagueView!.Canvas, frame, device + ", la Liga", 2);
            boot.CloseLeague();
            yield return Frames();

            Assert.That(boot.PressSettings(), Is.True, "Ajustes");
            yield return Frames();
            AssertInside(boot.SettingsView!.Canvas, frame, device + ", Ajustes", 1);
            AssertBleeds(boot.SettingsView.Canvas, "Dim", frame, device + ", el velo de Ajustes");
            boot.CloseSettings();
        }

        [UnityTest]
        public IEnumerator Level_HudBoardTrayAndPause_InsideTheColumnAndTheSafeArea([ValueSource(nameof(Devices))] Device device, [Values(1, 374)] int level)
        {
            device.Use();
            yield return Canvas16x9LayoutTests.LoadPlaying(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            ScreenFrame frame = ScreenFrame.Current;
            string where = device + ", el nivel " + level;
            Assert.That(view.CanvasWidth, Is.EqualTo(frame.ColumnWidth).Within(0.01f), where + ": se maqueta en la columna");
            AssertInside(view.Canvas, frame, where, 2);
            AssertBleeds(view.Canvas, "Scene/BaseFill", frame, where + ": el agua del río");
            AssertBleeds(view.Canvas, "BackgroundCatcher", frame, where + ": el toque en el fondo");
            AssertCuiasInside(view, frame, where);

            view.PauseButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 600);
            Assert.That(view.Pause.Visible, Is.True);
            AssertInside(view.Canvas, frame, where + ", la pausa", 3);
            view.Pause.ResumeButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 600);
        }

        // ------------------------------------------------------------------------------- girar y redimensionar

        [UnityTest]
        public IEnumerator Level_RotateAndResize_RelaysOut_AndComesBackToTheSamePlaces()
        {
            Phone.Use();
            yield return Canvas16x9LayoutTests.LoadPlaying(374);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Rect pause = ColumnRect(view.PauseButton.transform);
            Vector2[] feet = Feet(view);

            foreach (Device device in new[] { PhoneLandscape, Foldable, SplitScreen, TabletLandscape, TabletPortrait })
            {
                device.Use();
                PlayModeHelpers.Step(boot, 200);
                PlayModeHelpers.StepUntilIdle(boot, 3000);
                yield return null;
                ScreenFrame frame = ScreenFrame.Current;
                string where = "el 374 tras pasar a " + device;
                Assert.That(view.CanvasWidth, Is.EqualTo(frame.ColumnWidth).Within(0.01f), where + ": la columna nueva");
                AssertInside(view.Canvas, frame, where, 3);
                AssertBleeds(view.Canvas, "Scene/BaseFill", frame, where + ": el agua");
                AssertCuiasInside(view, frame, where);
                Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), where + ": se sigue jugando");
            }

            Phone.Use();
            PlayModeHelpers.Step(boot, 200);
            PlayModeHelpers.StepUntilIdle(boot, 3000);
            yield return null;
            Rect back = ColumnRect(view.PauseButton.transform);
            Assert.That(Vector2.Distance(back.position, pause.position), Is.LessThan(0.05f), "la pausa, de vuelta en su sitio");
            Vector2[] again = Feet(view);
            for (int i = 0; i < feet.Length; i++)
            {
                Assert.That(Vector2.Distance(again[i], feet[i]), Is.LessThan(0.05f), "la cuia " + i + ", de vuelta en su sitio");
            }
        }

        [UnityTest]
        public IEnumerator Market_OpenWhileRotating_RelaysOutInsideTheNewColumn()
        {
            Phone.Use();
            yield return LoadLobby();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            yield return Frames();
            foreach (Device device in new[] { TabletLandscape, PhoneLandscape, Phone })
            {
                device.Use();
                yield return Frames(); // el lienzo del Mercado se vuelve a medir solo (OverlayCanvas.Changed)
                ScreenFrame frame = ScreenFrame.Current;
                string where = "el Mercado abierto tras pasar a " + device;
                Assert.That(boot.Market!.Canvas.transform.Find(ScreenColumn.ObjectName).GetComponent<ScreenColumn>().Frame.ColumnWidth,
                    Is.EqualTo(frame.ColumnWidth).Within(0.01f), where + ": la columna nueva");
                AssertInside(boot.Market.Canvas, frame, where, 2);
                AssertBleeds(boot.Market.Canvas, "Dim", frame, where + ": el velo");
            }
        }

        // ------------------------------------------------------------------------------- comprobaciones

        internal static IEnumerator LoadLobby()
        {
            var clock = new ManualClock(T0);
            GameBootstrap.LeagueApiOverride = new FakeLeagueApi(clock) { StartPoints = 140 };
            GameBootstrap.LeagueClockOverride = clock;
            GameBootstrap.LeagueConfigOverride = new LeagueConfig { LiveLeagueEnabled = true };
            LaunchOptions.ClockOverride = clock;
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.MaxLevel = 57;
            profile.Progress.NextLevel = 58;
            profile.Wallet.Seeds = 2450;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<LeagueRunner>().enabled = false;
            boot.TickLeague(16f);
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
            yield return Frames();
        }

        private static IEnumerator Frames()
        {
            for (int i = 0; i < 3; i++)
            {
                yield return null;
            }

            Canvas.ForceUpdateCanvases();
        }

        private static void AssertFrame(ScreenFrame f, Device device)
        {
            Assert.That(f.ColumnX, Is.GreaterThanOrEqualTo(f.InsetLeft - 0.01f), device + ": la columna, tras el margen izquierdo");
            Assert.That(f.ColumnX + f.ColumnWidth, Is.LessThanOrEqualTo(f.CanvasWidth - f.InsetRight + 0.01f), device + ": y antes del derecho");
            Assert.That(f.ColumnWidth, Is.InRange(GameScreen.MinWidth - 0.01f, 400.01f), device + ": de 320 a 400 dp");
            Assert.That(f.IsFullWidth, Is.EqualTo(device.Name == Phone.Name || device.Name == TabletPortrait.Name), device + ": columna solo en las ventanas anchas");
        }

        // El rect de t en dp del lienzo entero, medido desde arriba a la izquierda: desde la columna (el lienzo real del Editor no
        // mide lo simulado), más su x.
        internal static Rect CanvasRectOf(Transform t, ScreenFrame frame)
        {
            Rect r = ColumnRect(t);
            return new Rect(r.x + (frame.IsFullWidth ? 0f : frame.ColumnX), r.y, r.width, r.height);
        }

        // El rect de t en dp de su columna, desde arriba a la izquierda.
        internal static Rect ColumnRect(Transform t)
        {
            RectTransform column = ColumnOf(t);
            var corners = new Vector3[4];
            ((RectTransform)t).GetWorldCorners(corners);
            Rect bounds = column.rect;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = column.InverseTransformPoint(corners[i]);
                float x = local.x - bounds.xMin;
                float y = bounds.yMax - local.y;
                xMin = Mathf.Min(xMin, x);
                xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y);
                yMax = Mathf.Max(yMax, y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static RectTransform ColumnOf(Transform t)
        {
            ScreenColumn column = t.GetComponentInParent<ScreenColumn>();
            Assert.That(column, Is.Not.Null, t.name + ": cuelga de una columna");
            return column.Rect;
        }

        // Todo lo que se toca a la vista (Selectable activo e interactuable, no tapado por un grupo transparente, fuera de las
        // listas con máscara, que recortan lo que se sale, y sin contar lo que tapa media pantalla, que son velos y capturas de
        // toques) dentro de la columna y de la zona segura.
        internal static void AssertInside(Canvas canvas, ScreenFrame frame, string where, int atLeast)
        {
            Canvas.ForceUpdateCanvases();
            float left = frame.IsFullWidth ? 0f : frame.ColumnX;
            float right = left + frame.ColumnWidth;
            float top = frame.InsetTop;
            float bottom = frame.CanvasHeight - frame.InsetBottom;
            float canvasArea = frame.CanvasWidth * frame.CanvasHeight;
            var issues = new List<string>();
            int checkedCount = 0;
            foreach (Selectable s in canvas.GetComponentsInChildren<Selectable>(false))
            {
                if (!s.isActiveAndEnabled || !s.IsInteractable() || Hidden(s.transform, canvas.transform))
                {
                    continue;
                }

                Rect r = CanvasRectOf(s.transform, frame);
                if (r.width * r.height >= 0.5f * canvasArea || r.width <= 0f || r.height <= 0f)
                {
                    continue;
                }

                checkedCount++;
                if (r.xMin < left - 0.5f || r.xMax > right + 0.5f || r.yMin < top - 0.5f || r.yMax > bottom + 0.5f)
                {
                    issues.Add(Path(s.transform, canvas.transform) + " " + Fmt(r));
                }
            }

            // Las listas con máscara (Ajustes, la Liga, el Mapa): lo que se ve de ellas, dentro de la columna y de la zona segura.
            int lists = 0;
            foreach (RectMask2D mask in canvas.GetComponentsInChildren<RectMask2D>(false))
            {
                if (!mask.isActiveAndEnabled || Hidden(mask.transform, canvas.transform))
                {
                    continue;
                }

                lists++;
                Rect r = CanvasRectOf(mask.transform, frame);
                if (r.xMin < left - 0.5f || r.xMax > right + 0.5f || r.yMin < top - 0.5f || r.yMax > bottom + 0.5f)
                {
                    issues.Add("la lista " + Path(mask.transform, canvas.transform) + " " + Fmt(r));
                }
            }

            TestContext.WriteLine("AF_WIDE " + where + ": " + lists + " listas; " + checkedCount + " elementos que se tocan; columna x " + Fmt(left) + "-" + Fmt(right)
                + ", zona segura y " + Fmt(top) + "-" + Fmt(bottom));
            Assert.That(checkedCount, Is.GreaterThanOrEqualTo(atLeast), where + ": condición del test, hay algo que tocar");
            Assert.That(issues, Is.Empty, where + ": fuera de la columna o de la zona segura:\n" + string.Join("\n", issues));
        }

        // Las cuias del tablero, dentro de la columna y sin estirarse (cada cuia, más alta que ancha: 02 §5, D-006).
        private static void AssertCuiasInside(GameScreen view, ScreenFrame frame, string where)
        {
            float left = frame.IsFullWidth ? 0f : frame.ColumnX;
            foreach (CuiaView cuia in view.Views)
            {
                Rect r = CanvasRectOf(cuia.Body, frame);
                Assert.That(r.xMin, Is.GreaterThanOrEqualTo(left - 0.5f), where + ": la cuia, dentro de la columna");
                Assert.That(r.xMax, Is.LessThanOrEqualTo(left + frame.ColumnWidth + 0.5f), where + ": la cuia, dentro de la columna");
                Assert.That(r.height, Is.GreaterThan(r.width), where + ": la cuia no se estira a lo ancho");
            }
        }

        // El fondo path (bajo la columna) cubre el lienzo entero de lado a lado.
        internal static void AssertBleeds(Canvas canvas, string path, ScreenFrame frame, string where)
        {
            Transform? t = canvas.transform.Find(ScreenColumn.ObjectName + "/" + path);
            Assert.That(t, Is.Not.Null, where + ": " + path);
            if (frame.IsFullWidth)
            {
                Vector2 none = ScreenColumn.BleedOf((RectTransform)t!);
                Assert.That(none, Is.EqualTo(Vector2.zero), where + ": sin columna, estirado a la columna");
                return;
            }

            Rect r = CanvasRectOf(t!, frame);
            Assert.That(r.xMin, Is.LessThanOrEqualTo(0.05f), where + ": llega al borde izquierdo");
            Assert.That(r.xMax, Is.GreaterThanOrEqualTo(frame.CanvasWidth - 0.05f), where + ": y al derecho");
        }

        private static bool Hidden(Transform t, Transform stop)
        {
            for (Transform? p = t; p != null && p != stop; p = p.parent)
            {
                CanvasGroup group = p.GetComponent<CanvasGroup>();
                if (group != null && (group.alpha <= 0.01f || !group.interactable))
                {
                    return true;
                }

                if (p != t && (p.GetComponent<RectMask2D>() != null || p.GetComponent<Mask>() != null))
                {
                    return true; // dentro de una lista: la máscara recorta lo que se sale
                }
            }

            return false;
        }

        private static Vector2[] Feet(GameScreen view)
        {
            var feet = new Vector2[view.Views.Length];
            for (int i = 0; i < feet.Length; i++)
            {
                feet[i] = ColumnRect(view.Views[i].Body).center;
            }

            return feet;
        }

        private static string Path(Transform t, Transform stop)
        {
            string path = t.name;
            for (Transform? p = t.parent; p != null && p != stop; p = p.parent)
            {
                path = p.name + "/" + path;
            }

            return path;
        }

        private static string Fmt(float v) => v.ToString("0.#", CultureInfo.InvariantCulture);

        private static string Fmt(Rect r) => "(" + Fmt(r.xMin) + ", " + Fmt(r.yMin) + ") – (" + Fmt(r.xMax) + ", " + Fmt(r.yMax) + ")";
    }
}
