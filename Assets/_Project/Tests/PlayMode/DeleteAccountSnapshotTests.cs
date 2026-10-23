#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AF.IAP;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de las esperas de red (prueba del dueño del 2026-10-23), con la pantalla simulada de
    /// <see cref="SnapshotKit"/> (360 × 780 dp), en <c>Logs/snapshots/&lt;idioma&gt;/</c>:
    /// <list type="bullet">
    /// <item><c>delete_account_01_confirm</c>, <c>02_deleting</c> (la espera, con los puntos), <c>03_done</c> (la confirmación) y
    /// <c>04_failed</c> («Reintentar»), al 100 % y al 130 % (<c>_130</c>), en es-419, en y pt-BR;</item>
    /// <item><c>market_verifying</c>: «Confirmando…» en la fila del Mercado con los precios desactivados (08 §10.3), en es-419.</item>
    /// </list>
    /// Comprueban además que los textos caben (<see cref="DeleteAccountWaitTests.Fits"/>). Necesitan gráficos:
    /// <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.DeleteAccountSnapshotTests</c> sin <c>-nographics</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class DeleteAccountSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetCloudOverrides();
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.AnalyticsResetOverride = null;
            LaunchOptions.Reset();
            GameScreen.SimulatedScreen = null;
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureDeleteAccountWait([Values("es-419", "en", "pt-BR")] string locale, [Values(1f, 1.3f)] float scale)
        {
            string dir = SnapshotKit.Prepare(locale);
            LaunchOptions.FontScaleOverride = scale;
            string suffix = scale > 1f ? "_130" : string.Empty;
            var server = new DeleteAccountWaitTests.SlowServer();
            GameBootstrap boot = null!;
            yield return DeleteAccountWaitTests.OpenStep2(new MemoryProfileStore(DeleteAccountWaitTests.ProfileAt(40, 900)), server, b => boot = b);
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(boot.SettingsView!.Canvas);
            DeleteAccountDialog dialog = boot.DeleteAccountView!;
            var problems = new List<string>();
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "delete_account_01_confirm" + suffix);

            Assert.That(boot.ConfirmDeleteAccount(), Is.True);
            yield return SnapshotKit.Wait(0.5f); // los puntos, a media vuelta
            DeleteAccountWaitTests.Fits(dialog, boot.SettingsView!.CanvasHeight, "eliminando", problems);
            SnapshotKit.Save(dir, "delete_account_02_deleting" + suffix);

            server.Answer(false);
            yield return SnapshotKit.Wait(0.3f);
            DeleteAccountWaitTests.Fits(dialog, boot.SettingsView!.CanvasHeight, "fallo", problems);
            SnapshotKit.Save(dir, "delete_account_04_failed" + suffix);

            GameBootstrap.DeleteDoneHoldMsOverride = 60000f; // la confirmación se queda para la captura
            Assert.That(boot.ConfirmDeleteAccount(), Is.True);
            server.Answer(true);
            yield return SnapshotKit.Wait(0.3f);
            DeleteAccountWaitTests.Fits(dialog, boot.SettingsView!.CanvasHeight, "hecho", problems);
            SnapshotKit.Save(dir, "delete_account_03_done" + suffix);
            Assert.That(File.Exists(Path.Combine(dir, "delete_account_03_done" + suffix + ".png")), Is.True);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [UnityTest]
        public IEnumerator CaptureMarketVerifying()
        {
            string dir = SnapshotKit.Prepare();
            FunnelSetup.Hooks();
            Profile profile = DeleteAccountWaitTests.ProfileAt(26, 0);
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            MarketScreen market = boot.Market!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(market.Canvas);
            int row = -1;
            for (int i = 0; i < market.Rows.Count; i++)
            {
                if (market.Rows[i].Id == StoreCatalog.Handful)
                {
                    row = i;
                }
            }

            boot.FakeBackend!.HoldVerifyResponses = true;
            Assert.That(market.PressRow(row), Is.True);
            boot.TickMarket(16f);
            yield return SnapshotKit.Wait(0.3f);
            Assert.That(market.RowNoteText(row), Is.EqualTo(MarketText.PurchaseVerifying));
            SnapshotKit.Save(dir, "market_verifying");
            boot.FakeBackend.ReleaseVerifyResponses();
        }
    }
}
