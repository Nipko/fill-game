#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AF.Platform;
using AF.Platform.Notifications;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Notificaciones locales reales (Q-M5 de D-033 y su añadido del 2026-10-21): en el editor no hay programador real (sigue el
    /// falso), el ensamblado <c>AF.Platform.Notifications</c> es el único que ve el paquete y con la versión exacta aprobada, y el
    /// adelanto de desarrollo de <c>af_dev_push</c> = 2 solo mueve el primer aviso de cada salida.
    /// </summary>
    public sealed class DeviceNotificationsTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 22, 15, 0, 0, DateTimeKind.Utc);

        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [Test]
        public void InTheEditor_ThereIsNoRealScheduler()
        {
            Assert.That(DeviceNotifications.Available, Is.False, "08 §2.1, principio 6: el editor usa la falsa");
            Assert.That(DeviceNotifications.Create(new ManualClock(T0), "Avisos del río", "x"), Is.Null);
            Assert.DoesNotThrow(() => DeviceNotifications.Poll(null));
            Assert.DoesNotThrow(DeviceNotifications.CancelAllOnDevice);
            Assert.That(DeviceNotifications.ChannelId, Is.EqualTo("af_default"));
        }

        [Test]
        public void ThePackage_IsPinnedTo243_AndOnlyThePlatformAssemblySeesIt()
        {
            string manifest = File.ReadAllText(Path.Combine(RepoRoot, "Packages", "manifest.json"));
            Assert.That(manifest, Does.Contain("\"com.unity.mobile.notifications\": \"2.4.3\""), "D-033, añadido del 2026-10-21: versión exacta");

            string asmdef = File.ReadAllText(Path.Combine(Application.dataPath, "_Project", "Scripts", "Platform", "Notifications", "AF.Platform.Notifications.asmdef"));
            Assert.That(asmdef, Does.Contain("\"expression\": \"2.4.3\""));
            Assert.That(asmdef, Does.Contain("\"define\": \"AF_MOBILE_NOTIFICATIONS\""));

            foreach (string path in Directory.GetFiles(Path.Combine(Application.dataPath, "_Project"), "*.asmdef", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(path) == "AF.Platform.Notifications.asmdef")
                {
                    continue;
                }

                Assert.That(File.ReadAllText(path), Does.Not.Contain("Unity.Notifications.Android"), Path.GetFileName(path) + ": el paquete solo en AF.Platform.Notifications");
            }
        }

        [Test]
        public void TheRealScheduler_NeverAsksAtBoot_NorUsesExactAlarms()
        {
            string source = File.ReadAllText(Path.Combine(Application.dataPath, "_Project", "Scripts", "Platform", "Notifications", "AndroidNotificationScheduler.cs"));
            // 06 §8.1: el diálogo nativo solo en RequestPermission (tras «Avísame» o desde Ajustes), una sola vez en el archivo.
            Assert.That(Regex.Matches(source, @"new PermissionRequest\(").Count, Is.EqualTo(1));
            // Q-M5: alarmas inexactas; nada de pedir alarmas exactas ni ignorar la optimización de batería.
            Assert.That(source, Does.Not.Contain("RequestExactScheduling"));
            Assert.That(source, Does.Not.Contain("RequestIgnoreBatteryOptimizations"));
            Assert.That(source, Does.Not.Contain("RepeatInterval"));
        }

        [Test]
        public void ThePackageSettings_KeepInexactAlarms_AndNoRescheduleOnRestart()
        {
            // Q-M5: con estos dos ajustes encendidos el paquete añadiría SCHEDULE_EXACT_ALARM / USE_EXACT_ALARM o
            // RECEIVE_BOOT_COMPLETED al manifiesto (AndroidNotificationPostProcessor), que no están aprobados.
            string text = File.ReadAllText(Path.Combine(RepoRoot, "ProjectSettings", "NotificationsSettings.asset"));
            Match block = Regex.Match(text, @"""m_AndroidNotificationSettingsValues""\s*:\s*\{\s*""m_Keys""\s*:\s*\[(?<keys>[^\]]*)\]\s*,\s*""m_Values""\s*:\s*\[(?<values>[^\]]*)\]");
            Assert.That(block.Success, Is.True, "bloque de Android de NotificationsSettings.asset");
            MatchCollection keys = Regex.Matches(block.Groups["keys"].Value, "\"([^\"]*)\"");
            MatchCollection values = Regex.Matches(block.Groups["values"].Value, "\"([^\"]*)\"");
            Assert.That(values.Count, Is.EqualTo(keys.Count));
            var settings = new Dictionary<string, string>();
            for (int i = 0; i < keys.Count; i++)
            {
                settings[keys[i].Groups[1].Value] = values[i].Groups[1].Value;
            }

            Assert.That(settings["UnityNotificationAndroidScheduleExactAlarms"], Is.EqualTo("0"), "alarmas inexactas");
            Assert.That(settings["UnityNotificationAndroidRescheduleOnDeviceRestart"], Is.EqualTo("False"), "sin RECEIVE_BOOT_COMPLETED");
        }

        [Test]
        public void TheScheduledLog_RoundTrips_AndSkipsWhatItCannotRead()
        {
            var entries = new List<DeliveredNotification>
            {
                new DeliveredNotification(1001, "daily_ready", T0),
                new DeliveredNotification(-7, "comeback_d7", T0.AddDays(7)),
                new DeliveredNotification(5, "a:b", T0), // no se podría leer: se salta
                new DeliveredNotification(6, string.Empty, T0),
            };
            string raw = ScheduledNotificationLog.Format(entries);
            Assert.That(raw, Is.EqualTo("1001:daily_ready:" + T0.Ticks + ";-7:comeback_d7:" + T0.AddDays(7).Ticks));

            var back = new List<DeliveredNotification>();
            ScheduledNotificationLog.Parse(raw, back);
            Assert.That(back.Count, Is.EqualTo(2));
            Assert.That(back[0].Id, Is.EqualTo(1001));
            Assert.That(back[0].Type, Is.EqualTo("daily_ready"));
            Assert.That(back[0].FireAtUtc, Is.EqualTo(T0));
            Assert.That(back[0].FireAtUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(back[1].Id, Is.EqualTo(-7));

            var bad = new List<DeliveredNotification>();
            ScheduledNotificationLog.Parse(null, bad);
            ScheduledNotificationLog.Parse(string.Empty, bad);
            ScheduledNotificationLog.Parse(";;x:y:z;1::5;2:t:0;3:t:-4;4:t:99999999999999999999;5:t:" + (DateTime.MaxValue.Ticks + 1) + ";6:t", bad);
            Assert.That(bad, Is.Empty, "las entradas corruptas se descartan sin excepción");
            ScheduledNotificationLog.Parse("x;7:canoes_full:" + T0.Ticks + ";garbage", bad);
            Assert.That(bad.Count, Is.EqualTo(1), "las válidas se quedan");
        }

        [Test]
        public void DevSoon_MovesOnlyTheFirstNotificationOfEachExit()
        {
            var clock = new ManualClock(T0);
            var inner = new FakeNotificationScheduler(clock, PushPermission.Allowed, false);
            var soon = new GameBootstrap.DevSoonNotificationScheduler(inner, clock);
            DateTime tomorrow = T0.AddDays(1);

            soon.CancelAll();
            soon.Schedule(new LocalNotification(1001, "daily_ready", "TiTino", "a", tomorrow));
            soon.Schedule(new LocalNotification(2002, "streak_waiting", "TiTino", "b", tomorrow.AddDays(1)));
            Assert.That(inner.Scheduled.Count, Is.EqualTo(2));
            Assert.That(inner.Scheduled[0].FireAtUtc, Is.EqualTo(T0.AddSeconds(GameBootstrap.DevPushSoonSeconds)));
            Assert.That(inner.Scheduled[0].Id, Is.EqualTo(1001), "mismo id");
            Assert.That(inner.Scheduled[0].Type, Is.EqualTo("daily_ready"), "mismo tipo para af_push_sent / af_push_open");
            Assert.That(inner.Scheduled[0].Body, Is.EqualTo("a"), "mismo texto");
            Assert.That(inner.Scheduled[1].FireAtUtc, Is.EqualTo(tomorrow.AddDays(1)), "el resto, a su hora");

            soon.CancelAll();
            Assert.That(inner.Scheduled.Count, Is.EqualTo(0));
            soon.Schedule(new LocalNotification(3003, "canoes_full", "TiTino", "c", tomorrow));
            Assert.That(inner.Scheduled[0].FireAtUtc, Is.EqualTo(T0.AddSeconds(GameBootstrap.DevPushSoonSeconds)), "otra salida, otro adelanto");
            Assert.That(soon.Permission, Is.EqualTo(PushPermission.Allowed));
            Assert.That(soon.PermissionRequired, Is.False);
            Assert.That(soon.Inner, Is.SameAs(inner));
        }
    }
}
