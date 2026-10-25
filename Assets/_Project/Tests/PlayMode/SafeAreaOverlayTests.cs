#nullable enable
using System;
using System.Collections;
using AF.Ads;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Safe area de las pantallas en lienzo propio (auditoría B10, <c>docs/dev/traspaso/b10-safe-area-y-retroceso.md</c>; 08 §13.1
    /// #4; T6, carril B, punto 3), con una barra de navegación o de gestos simulada (zona segura de abajo distinta de 0):
    /// <list type="bullet">
    /// <item><description><b>StoreSheet</b> (hoja «Compra simulada»): la hoja llega al borde de abajo, pero «Cancelar» y
    /// «Comprar» quedan sobre la barra (el parche de B10, con <c>OverlayCanvas.SafeBottom</c>);</description></item>
    /// <item><description><b>AdOverlay</b> (anuncio simulado a pantalla completa): el rótulo bajo la zona segura de arriba y
    /// «Cerrar» sobre la de abajo, con los márgenes de <c>OverlayCanvas</c> (como mínimo los de <c>GameScreen</c>, 32 y 24
    /// dp).</description></item>
    /// </list>
    /// El calendario de sellos del Reto, en <c>DailyChallengeTests.Calendar_WithAGestureBar_…</c> (necesita su pool y su reloj), y
    /// Aguas estancadas en 16:9 al 130 %, en <c>StuckCompactTests.Stuck_16x9_At130_WithAGestureBar_…</c>.
    /// </summary>
    public sealed class SafeAreaOverlayTests
    {
        // 1080 × 2340 px (360 × 780 dp a 3 px/dp) con 120 px de zona segura arriba (40 dp, un recorte) y 220 px abajo (73,3 dp),
        // el margen grande que propone el parche de B10.
        private static readonly Vector4 Tall = new Vector4(1080f, 2340f, 120f, 220f);

        // 16:9 (1080 × 1920 px, 360 × 640 dp) con la misma barra de 220 px: lo más justo de alto.
        private static readonly Vector4 Short = new Vector4(1080f, 1920f, 120f, 220f);

        private static readonly DateTime T0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator StoreSheet_WithAGestureBar_CancelAndBuyStayAboveIt([Values(780, 640)] int height)
        {
            Vector4 screen = height == 780 ? Tall : Short;
            GameScreen.SimulatedScreen = screen;
            var clock = new ManualClock(T0, TimeSpan.FromHours(-5));
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(AudioHooksTests.NoCanoesAt31(clock)));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            lobby.CanoesButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes), "condición del test: Sin canoas");
            yield return null;
            lobby.NoCanoes.InfiniteButton.onClick.Invoke();
            StoreSheet sheet = boot.StoreSheetView!;
            Assert.That(sheet.Visible, Is.True, "la hoja «Compra simulada»");
            Canvas.ForceUpdateCanvases();

            string where = "la hoja de compra a 360 × " + height + " con 220 px de barra";
            Margins(sheet.Canvas, screen, out float safeTop, out float safeBottom, out float canvasH);
            Rect cancel = StopCardTests.CanvasRect((RectTransform)sheet.CancelButton.transform, sheet.Canvas);
            Rect buy = StopCardTests.CanvasRect((RectTransform)sheet.BuyButton.transform, sheet.Canvas);
            Rect panel = StopCardTests.CanvasRect((RectTransform)sheet.Canvas.transform.Find("Panel"), sheet.Canvas);
            Debug.Log("AF_SAFE_STORE " + where + ": lienzo de " + canvasH.ToString("0.0") + " dp, zona segura de abajo " + safeBottom.ToString("0.0")
                + " dp; Cancelar " + cancel + ", Comprar " + buy + ", hoja " + panel);
            Assert.That(safeBottom, Is.GreaterThan(70f), where + ": condición del test, la barra de 73,3 dp");
            Assert.That(cancel.yMax, Is.LessThanOrEqualTo(canvasH - safeBottom + 0.05f), where + ": «Cancelar» sobre la barra");
            Assert.That(buy.yMax, Is.LessThanOrEqualTo(cancel.yMin + 0.05f), where + ": «Comprar» sobre «Cancelar»");
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(safeTop - 0.05f), where + ": la hoja, bajo la zona segura de arriba");
            Assert.That(panel.yMax, Is.GreaterThanOrEqualTo(canvasH - 0.05f), where + ": la hoja llega al borde de abajo");
            sheet.CancelButton.onClick.Invoke();
            Assert.That(sheet.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator AdOverlay_WithAGestureBar_TitleAndCloseStayInsideTheSafeArea([Values(780, 640)] int height)
        {
            Vector4 screen = height == 780 ? Tall : Short;
            GameScreen.SimulatedScreen = screen;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(30)));
            GameBootstrap boot = GameBootstrap.Current!;
            AdOverlay overlay = boot.AdOverlayView!;
            AdOutcome? outcome = null;
            var ads = new FakeAdService(overlay);
            ads.Show(AdPlacement.DoubleHarvest, o => outcome = o);
            Assert.That(overlay.Visible, Is.True, "el anuncio simulado");
            Canvas.ForceUpdateCanvases();

            string where = "el anuncio simulado a 360 × " + height + " con 220 px de barra";
            Margins(overlay.Canvas, screen, out float safeTop, out float safeBottom, out float canvasH);
            Rect title = StopCardTests.CanvasRect((RectTransform)overlay.Canvas.transform.Find("Title"), overlay.Canvas);
            Rect close = StopCardTests.CanvasRect((RectTransform)overlay.CloseButton.transform, overlay.Canvas);
            Debug.Log("AF_SAFE_AD " + where + ": zona segura " + safeTop.ToString("0.0") + " / " + safeBottom.ToString("0.0") + " dp; rótulo "
                + title + ", Cerrar " + close);
            Assert.That(title.yMin, Is.GreaterThanOrEqualTo(safeTop + 8f - 0.05f), where + ": el rótulo, bajo la zona segura de arriba");
            Assert.That(close.yMax, Is.LessThanOrEqualTo(canvasH - safeBottom - 8f + 0.05f), where + ": «Cerrar», sobre la barra");
            Assert.That(close.yMin, Is.GreaterThan(title.yMax), where + ": «Cerrar», bajo el rótulo");

            overlay.Tick(4000f);
            overlay.CloseButton.onClick.Invoke();
            Assert.That(overlay.Visible, Is.False);
            Assert.That(outcome.HasValue, Is.True, "se cierra una vez");
        }

        /// <summary>
        /// Las cotas de la zona segura de AdOverlay actúan de verdad (revisión del integrador): con un recorte de arriba de 660 px
        /// (220 dp) el rótulo baja de su sitio proporcional (30 % del alto − 20) a la zona segura + 8; en un lienzo bajo (1080 ×
        /// 1600 px: a 2,5 px/dp, 432 × 640 dp) con 450 px (180 dp) de barra, «Cerrar» sube de su 68 % a la zona
        /// segura de abajo + 8.
        /// </summary>
        [UnityTest]
        public IEnumerator AdOverlay_WithLargeInsets_TheBoundsMoveTitleAndClose([Values("top", "bottom")] string edge)
        {
            Vector4 screen = edge == "top" ? new Vector4(1080f, 2340f, 660f, 48f) : new Vector4(1080f, 1600f, 96f, 450f);
            GameScreen.SimulatedScreen = screen;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(30)));
            GameBootstrap boot = GameBootstrap.Current!;
            AdOverlay overlay = boot.AdOverlayView!;
            var ads = new FakeAdService(overlay);
            ads.Show(AdPlacement.DoubleHarvest, _ => { });
            Canvas.ForceUpdateCanvases();

            Margins(overlay.Canvas, screen, out float safeTop, out float safeBottom, out float canvasH);
            Rect title = StopCardTests.CanvasRect((RectTransform)overlay.Canvas.transform.Find("Title"), overlay.Canvas);
            Rect close = StopCardTests.CanvasRect((RectTransform)overlay.CloseButton.transform, overlay.Canvas);
            string where = "el anuncio con " + (edge == "top" ? "660 px de recorte arriba" : "450 px de barra en 1080 × 1600 px");
            Debug.Log("AF_SAFE_AD_BOUND " + where + ": lienzo de " + canvasH.ToString("0.0") + " dp, zona segura " + safeTop.ToString("0.0") + " / "
                + safeBottom.ToString("0.0") + " dp; rótulo " + title + ", Cerrar " + close);
            if (edge == "top")
            {
                Assert.That(title.yMin, Is.GreaterThan((canvasH * 0.30f) - 20f + 0.5f), where + ": el rótulo, más abajo que su sitio proporcional");
                Assert.That(title.yMin, Is.EqualTo(safeTop + 8f).Within(0.05f), where + ": en la zona segura + 8");
            }
            else
            {
                Assert.That(close.yMin, Is.LessThan((canvasH * 0.68f) - 0.5f), where + ": «Cerrar», más arriba que su sitio proporcional");
                Assert.That(close.yMax, Is.EqualTo(canvasH - safeBottom - 8f).Within(0.05f), where + ": sobre la barra + 8");
                Assert.That(close.yMin, Is.GreaterThan(title.yMax), where + ": bajo el rótulo");
            }

            overlay.Tick(4000f);
            overlay.CloseButton.onClick.Invoke();
        }

        // Márgenes de la zona segura de la pantalla simulada en dp del lienzo (arriba y abajo) y alto del lienzo, con la escala
        // de la pantalla de nivel (la de OverlayCanvas; el rect de un lienzo superpuesto sigue a la ventana del Editor, no a la
        // pantalla simulada, así que las posiciones se miden desde arriba).
        internal static void Margins(Canvas canvas, Vector4 screen, out float safeTop, out float safeBottom, out float canvasH)
        {
            float scale = GameScreen.CurrentCanvasScale((int)screen.x, (int)screen.y);
            canvasH = screen.y / scale;
            safeTop = screen.z / scale;
            safeBottom = screen.w / scale;
        }
    }
}
