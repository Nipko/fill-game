#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La espera de «Eliminar cuenta y datos» en la escena <c>Game</c> real (prueba del dueño del 2026-10-23: «está tosco, sin
    /// mostrar nada si cargando o procesando»; 08 §11; 02 §12), con un servidor falso que responde cuando el test lo suelta (el
    /// <c>POST /v1/account/delete</c> real tarda varios segundos):
    /// <list type="bullet">
    /// <item>mientras espera, «Eliminando tu cuenta y tus datos…» con los puntos de espera en marcha (quietos con Reducir
    /// movimiento), los botones desactivados, un segundo toque que no pide otro borrado, el Atrás de Android ignorado y TalkBack
    /// que lo anuncia;</item>
    /// <item>al terminar bien, «Tu cuenta y tus datos se eliminaron.» sin botones, un fundido y la recarga como primera
    /// ejecución;</item>
    /// <item>al fallar, «No se pudo eliminar» con «Reintentar», sin cerrar, y el reintento que borra;</item>
    /// <item>al 130 %, en es-419, en y pt-BR, los textos nuevos caben.</item>
    /// </list>
    /// </summary>
    public sealed class DeleteAccountWaitTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.A11yHostOverride = null;
            GameBootstrap.ResetCloudOverrides();
            GameBootstrap.AnalyticsResetOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        internal static Profile ProfileAt(int nextLevel, int seeds, bool reduceMotion = false)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            profile.Wallet.Seeds = seeds;
            if (reduceMotion)
            {
                profile.Settings.ReduceMotion = ReduceMotionSetting.On;
            }

            return profile;
        }

        /// <summary>Servidor falso con retardo: guarda la respuesta hasta <see cref="Answer"/>.</summary>
        internal sealed class SlowServer
        {
            private Action<bool>? _pending;

            public int Calls { get; private set; }

            public bool Waiting => _pending != null;

            public void Ask(Action<bool> done)
            {
                Calls++;
                _pending = done;
            }

            public void Answer(bool ok)
            {
                Action<bool> done = _pending ?? throw new InvalidOperationException("no hay borrado en vuelo");
                _pending = null;
                done(ok);
            }
        }

        internal static IEnumerator OpenStep2(MemoryProfileStore store, SlowServer server, Action<GameBootstrap> got)
        {
            GameBootstrap.WipePlayerPrefsForTests = () => { };
            GameBootstrap.AnalyticsResetOverride = () => { };
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.ServerAccountDelete = server.Ask; // el servidor falso con retardo (gana al del backend falso del editor)
            Assert.That(boot.RemoteAccountAllowed, Is.True, "13 o más: con borrado remoto");
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.OpenDeleteAccount() && boot.ContinueDeleteAccount(), Is.True);
            got(boot);
        }

        [UnityTest]
        public IEnumerator WhileTheServerDeletes_TheDialogSaysSo_LocksItsButtonsAndBack_AndThenConfirmsBeforeTheRestart()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            const float hold = 400f; // la confirmación, más corta en el test (la de verdad: DeleteDoneHoldMs)
            GameBootstrap.DeleteDoneHoldMsOverride = hold;
            var store = new MemoryProfileStore(ProfileAt(40, 900));
            var server = new SlowServer();
            GameBootstrap boot = null!;
            yield return OpenStep2(store, server, b => boot = b);
            DeleteAccountDialog dialog = boot.DeleteAccountView!;

            Assert.That(boot.ConfirmDeleteAccount(), Is.True);
            Assert.That(server.Calls, Is.EqualTo(1), "POST /v1/account/delete");
            Assert.That(boot.AccountDeleteInFlight && boot.DeleteAccountBusy, Is.True);
            Assert.That(dialog.Step, Is.EqualTo(DeleteAccountStep.Deleting));
            Assert.That(dialog.BodyText, Is.EqualTo(AccountText.Deleting));
            Assert.That(dialog.WarningText, Is.Null);
            Assert.That(dialog.Dots.Visible && dialog.Dots.Animating, Is.True, "el indicador de espera, en marcha");
            Assert.That(dialog.AdvanceInteractable, Is.False, "«Eliminar» desactivado: no se pulsa dos veces");
            Assert.That(dialog.CloseInteractable, Is.False, "la X, también");
            Assert.That(host.Announcements, Does.Contain(AccountText.Deleting), "TalkBack anuncia el estado");
            Assert.That(store.Json, Is.Not.Null, "lo local espera al servidor (08 §11)");

            // Un segundo toque (el botón o la llamada) no pide otro borrado; la X y el Atrás de Android no cierran nada.
            dialog.AdvanceButton.onClick.Invoke();
            Assert.That(boot.ConfirmDeleteAccount(), Is.True);
            Assert.That(server.Calls, Is.EqualTo(1), "un solo borrado en vuelo");
            dialog.CloseButton.onClick.Invoke();
            Assert.That(boot.CancelDeleteAccount(), Is.False);
            Assert.That(boot.PressAndroidBack(), Is.False, "Atrás no cierra el diálogo a medias");
            Assert.That(boot.BackBlocked(), Is.True, "el háptico de Atrás bloqueado");
            Assert.That(boot.DeleteAccountOpen && dialog.Step == DeleteAccountStep.Deleting, Is.True);

            // Los puntos se mueven con los fotogramas (sin Reducir movimiento).
            float[] before = Alphas(dialog.Dots);
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 0.25f)
            {
                yield return null;
            }

            Assert.That(Alphas(dialog.Dots), Is.Not.EqualTo(before), "los puntos de espera cambian");
            Assert.That(boot.DeleteAccountOpen, Is.True, "sigue esperando");

            // El servidor confirma: se borra lo local y se ve la confirmación, sin botones, antes de la recarga.
            server.Answer(true);
            Assert.That(store.Json, Is.Null, "profile.json y profile.bak");
            Assert.That(boot.AccountDeleted, Is.True);
            Assert.That(dialog.Step, Is.EqualTo(DeleteAccountStep.Done));
            Assert.That(dialog.BodyText, Is.EqualTo(AccountText.Done));
            Assert.That(dialog.Dots.Visible, Is.False);
            Assert.That(dialog.AdvanceButton.gameObject.activeSelf || dialog.CloseButton.gameObject.activeSelf, Is.False, "sin botones");
            Assert.That(host.Announcements, Does.Contain(AccountText.Done));
            Assert.That(boot.PressAndroidBack(), Is.False, "Atrás tampoco cierra la confirmación");
            Assert.That(GameBootstrap.Current, Is.SameAs(boot), "aún sin recarga: primero la confirmación");

            float doneAt = Time.realtimeSinceStartup;
            float fadeSeenAt = -1f;
            float lowestPanel = 1f;
            float highestVeil = 0f;
            while (GameBootstrap.Current == boot && boot != null)
            {
                if (boot.AccountDeleted)
                {
                    lowestPanel = Mathf.Min(lowestPanel, dialog.PanelAlpha);
                    highestVeil = Mathf.Max(highestVeil, dialog.VeilAlpha);
                    if (fadeSeenAt < 0f && dialog.PanelAlpha < 1f)
                    {
                        fadeSeenAt = Time.realtimeSinceStartup;
                    }
                }

                Assert.That(Time.realtimeSinceStartup - doneAt, Is.LessThan(PlayModeHelpers.TimeoutSeconds), "la recarga llega");
                yield return null;
            }

            Assert.That(fadeSeenAt - doneAt, Is.GreaterThanOrEqualTo((hold / 1000f) - 0.2f),
                "la confirmación se ve " + hold + " ms antes del fundido");
            Assert.That(lowestPanel, Is.LessThan(1f), "la tarjeta se funde");
            Assert.That(highestVeil, Is.GreaterThan(DeleteAccountDialog.DimAlpha), "y el velo se oscurece");

            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "arranque en frío tras eliminar la cuenta");
            Assert.That(GameBootstrap.Current!.App!.Screen, Is.EqualTo(AppScreen.Level), "primera ejecución: el nivel 1 (08 §2.3)");
            Assert.That(GameBootstrap.Current.Profile!.Wallet.Seeds, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator DuringTheConfirmation_PauseAndResume_NeitherWriteTheProfileNorCallTheServer()
        {
            // Revisión de tanda5/borrado-espera: con «Tu cuenta y tus datos se eliminaron.» a la vista, Inicio y la vuelta pasaban
            // por SessionsPaused → ProfileSessionStore.Save → ProfileSaveBatch.Save, que volvía a crear profile.json, y por
            // ResumeBackend, RestoreLedgerNow y PushUiResumed, que llamaban al servidor.
            GameBootstrap.DeleteDoneHoldMsOverride = 60000f; // la confirmación se queda mientras se simula la pausa
            var store = new MemoryProfileStore(ProfileAt(40, 900));
            var server = new SlowServer();
            GameBootstrap boot = null!;
            yield return OpenStep2(store, server, b => boot = b);
            Assert.That(boot.FakeBackend, Is.Not.Null, "editor: el backend falso");
            Assert.That(boot.ConfirmDeleteAccount(), Is.True);
            server.Answer(true);
            Assert.That(boot.AccountDeleted, Is.True);
            Assert.That(store.Json, Is.Null);
            int calls = BackendCalls(boot);
            int billing = boot.FakeBilling?.Requests.Count ?? 0;

            // Inicio y vuelta, por las dos rutas: los runners (SendMessage llega a todos los componentes del objeto, también a
            // SessionRunner y LogbookRunner) y las llamadas directas.
            boot.gameObject.SendMessage("OnApplicationPause", true);
            boot.gameObject.SendMessage("OnApplicationQuit");
            boot.SessionsPaused(true);
            boot.LogbookPause(true);
            yield return null;
            boot.gameObject.SendMessage("OnApplicationPause", false);
            boot.SessionsPaused(false);
            boot.LogbookPause(false);
            boot.ResumeBackend(true);
            Assert.That(boot.RestoreLedgerNow(), Is.False, "restoreLedger no se pide");
            boot.PushUiResumed();
            boot.TickSessions();
            boot.TickLogbook(16f);
            boot.TickMarket(16f);
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            Assert.That(store.Json, Is.Null, "profile.json no vuelve");
            Assert.That(BackendCalls(boot), Is.EqualTo(calls), "ninguna llamada más al backend");
            Assert.That(boot.FakeBilling?.Requests.Count ?? 0, Is.EqualTo(billing), "ni a Play");
            Assert.That(GameBootstrap.Current, Is.SameAs(boot), "sigue la confirmación");
            Assert.That(boot.DeleteAccountView!.Step, Is.EqualTo(DeleteAccountStep.Done));
        }

        private static int BackendCalls(GameBootstrap boot)
        {
            int n = 0;
            foreach (KeyValuePair<string, int> call in boot.FakeBackend!.Calls)
            {
                n += call.Value;
            }

            return n;
        }

        [UnityTest]
        public IEnumerator AFailedDelete_SaysSoWithRetry_WithoutClosing_AndTheRetryDeletes()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            GameBootstrap.DeleteDoneHoldMsOverride = 100f;
            var store = new MemoryProfileStore(ProfileAt(40, 900));
            var server = new SlowServer();
            GameBootstrap boot = null!;
            yield return OpenStep2(store, server, b => boot = b);
            DeleteAccountDialog dialog = boot.DeleteAccountView!;

            Assert.That(boot.ConfirmDeleteAccount(), Is.True);
            yield return null;
            server.Answer(false); // sin red o un 5xx: no se borra nada
            Assert.That(boot.AccountDeleteInFlight || boot.DeleteAccountBusy, Is.False);
            Assert.That(dialog.Visible && dialog.Step == DeleteAccountStep.Confirm, Is.True, "sigue abierto en el paso 2");
            Assert.That(dialog.WarningText, Is.EqualTo(AccountText.Failed));
            Assert.That(dialog.AdvanceText, Is.EqualTo(AccountText.Retry), "«Reintentar», claro");
            Assert.That(dialog.AdvanceInteractable && dialog.CloseInteractable, Is.True, "los botones vuelven");
            Assert.That(dialog.Dots.Visible, Is.False);
            Assert.That(host.Announcements, Does.Contain(AccountText.Failed));
            Assert.That(store.Json, Is.Not.Null, "no se borró nada");

            // «Reintentar» sin cerrar: otra vez «Eliminando…», y ahora el servidor confirma.
            dialog.AdvanceButton.onClick.Invoke();
            Assert.That(server.Calls, Is.EqualTo(2));
            Assert.That(dialog.Step, Is.EqualTo(DeleteAccountStep.Deleting));
            Assert.That(dialog.WarningText, Is.Null, "el fallo se va mientras se reintenta");
            server.Answer(true);
            Assert.That(dialog.Step, Is.EqualTo(DeleteAccountStep.Done));
            Assert.That(store.Json, Is.Null);
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != boot && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "arranque en frío tras el reintento");
        }

        [UnityTest]
        public IEnumerator WithReduceMotion_TheWaitDotsStayStill_AndTheConfirmationFadesFaster()
        {
            GameBootstrap.DeleteDoneHoldMsOverride = 100f;
            var store = new MemoryProfileStore(ProfileAt(40, 900, true));
            var server = new SlowServer();
            GameBootstrap boot = null!;
            yield return OpenStep2(store, server, b => boot = b);
            Assert.That(boot.ReduceMotionNow, Is.True);
            DeleteAccountDialog dialog = boot.DeleteAccountView!;
            Assert.That(boot.ConfirmDeleteAccount(), Is.True);
            Assert.That(dialog.Dots.Visible, Is.True, "la espera se sigue viendo");
            Assert.That(dialog.Dots.Animating, Is.False, "02 §12: sin animación");
            float[] before = Alphas(dialog.Dots);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            Assert.That(Alphas(dialog.Dots), Is.EqualTo(before), "quietos");
            Assert.That(before, Is.All.EqualTo(1f), "todos encendidos");
            server.Answer(false);
            Assert.That(dialog.CloseInteractable, Is.True, "tras el fallo, la X vuelve a cerrar sin borrar");
            Assert.That(store.Json, Is.Not.Null);

            // La confirmación con Reducir movimiento: el fundido × anim_reduce_motion_factor (0,5: 150 ms), sin puntos.
            Assert.That(boot.DeleteFadeMsNow, Is.EqualTo(GameBootstrap.DeleteFadeMs * 0.5f).Within(0.01f));
            Assert.That(boot.ConfirmDeleteAccount(), Is.True);
            Assert.That(dialog.Dots.Animating, Is.False);
            server.Answer(true);
            Assert.That(dialog.Step, Is.EqualTo(DeleteAccountStep.Done));
            Assert.That(dialog.Dots.Visible, Is.False);
            float lowest = 1f;
            float start2 = Time.realtimeSinceStartup;
            while (GameBootstrap.Current == boot && boot != null)
            {
                lowest = Mathf.Min(lowest, dialog.PanelAlpha);
                Assert.That(Time.realtimeSinceStartup - start2, Is.LessThan(PlayModeHelpers.TimeoutSeconds), "la recarga llega");
                yield return null;
            }

            Assert.That(lowest, Is.LessThan(1f), "también se funde (más corto), no un corte");
        }

        [UnityTest]
        public IEnumerator At130Percent_TheNewTextsFit([Values("es-419", "en", "pt-BR")] string locale)
        {
            LaunchOptions.FontScaleOverride = 1.3f;
            LaunchOptions.LanguageOverride = locale == LocLocale.SourceLocale ? null : locale;
            GameScreen.SimulatedScreen = HudLabelTests.Screen640; // un teléfono 16:9 bajo, el peor caso de alto
            var store = new MemoryProfileStore(ProfileAt(40, 900));
            var server = new SlowServer();
            GameBootstrap boot = null!;
            yield return OpenStep2(store, server, b => boot = b);
            Assert.That(Loc.Locale, Is.EqualTo(locale));
            DeleteAccountDialog dialog = boot.DeleteAccountView!;
            var problems = new List<string>();

            boot.ConfirmDeleteAccount();
            yield return null;
            Fits(dialog, boot.SettingsView!.CanvasHeight, "eliminando", problems);
            server.Answer(false);
            yield return null;
            Fits(dialog, boot.SettingsView!.CanvasHeight, "fallo", problems);
            GameBootstrap.DeleteDoneHoldMsOverride = 60000f; // la confirmación se queda para medirla
            boot.ConfirmDeleteAccount();
            server.Answer(true);
            yield return null;
            Fits(dialog, boot.SettingsView!.CanvasHeight, "hecho", problems);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(AccountText.Deleting, Is.Not.Empty);
            Assert.That(AccountText.Done, Is.Not.Empty);
            Assert.That(AccountText.Retry, Is.Not.Empty);
        }

        internal static void Fits(DeleteAccountDialog dialog, float canvasH, string what, List<string> problems)
        {
            Rect panel = dialog.Panel.rect;
            foreach (TextMeshProUGUI t in dialog.Panel.GetComponentsInChildren<TextMeshProUGUI>(false))
            {
                if (!t.isActiveAndEnabled || t.text.Length == 0)
                {
                    continue;
                }

                t.ForceMeshUpdate();
                Rect r = t.rectTransform.rect;
                if (t.isTextOverflowing || t.textBounds.size.x > r.width + 0.5f || t.textBounds.size.y > r.height + 0.5f)
                {
                    problems.Add(what + ": «" + t.text + "» (" + t.name + ") no cabe en " + r.width + "×" + r.height + " dp");
                }

                if (t.fontSize < UiBuilder.MinTextSp - 0.01f)
                {
                    problems.Add(what + ": «" + t.text + "» a " + t.fontSize + " sp (< " + UiBuilder.MinTextSp + ")");
                }
            }

            // La tarjeta va colocada desde arriba en el lienzo del diálogo (UiBuilder.Place): su borde de arriba y el de abajo, dentro.
            float top = -dialog.Panel.anchoredPosition.y;
            float bottom = top + panel.height;
            if (top < -0.5f || bottom > canvasH + 0.5f || panel.height <= 0f)
            {
                problems.Add(what + ": la tarjeta (" + top + "-" + bottom + " dp) se sale del lienzo de " + canvasH + " dp");
            }
        }

        internal static float[] Alphas(WaitDots dots)
        {
            var a = new float[WaitDots.Count];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = dots.AlphaOf(i);
            }

            return a;
        }
    }
}
