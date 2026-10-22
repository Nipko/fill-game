#nullable enable
using System;
using System.Collections.Generic;
using AF.Localization;
using AF.Platform;
using AF.Platform.Notifications;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// El programador real de los avisos (parche <c>B6-notifications-real</c>; Q-M5 de D-033 y su añadido del 2026-10-21): implementa
    /// los ganchos <c>CreatePlatformScheduler</c> y <c>TickPlatformScheduler</c> de <c>GameBootstrap.Sessions.cs</c> con
    /// <see cref="DeviceNotifications"/> (ensamblado <c>AF.Platform.Notifications</c>, el único que ve el paquete
    /// <c>com.unity.mobile.notifications</c>). En el editor y en los tests sigue el <see cref="FakeNotificationScheduler"/>.
    /// <para>
    /// Solo en builds de desarrollo, el extra de intent <c>af_dev_push</c> (<see cref="IntentExtraDevPush"/>) sirve para probarlo en el
    /// teléfono sin jugar hasta el pre-prompt (la tarjeta y la fila «Avisos» de Ajustes están en <c>GameBootstrap.PushUi.cs</c>): con
    /// 1 hace lo mismo que encender esa fila, una vez y con la edad y el consentimiento ya decididos; con 2, además, el primer aviso
    /// de cada salida a segundo plano se adelanta a 60 s. Nunca se lee en un build de release.
    /// </para>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Extra de intent (int) de los builds de desarrollo: 1 = «Avisos» encendido desde Ajustes; 2 = y el primer aviso a 60 s.</summary>
        public const string IntentExtraDevPush = "af_dev_push";

        private const string NotificationsLogPrefix = "[AF.Notifications] ";

        /// <summary>Segundos a los que <c>af_dev_push</c> = 2 adelanta el primer aviso de cada salida.</summary>
        internal const int DevPushSoonSeconds = 60;

        // af_dev_push se lee una vez por proceso: la reconstrucción por idioma no repite el encendido.
        private static bool s_devPushRead;
        private static int s_devPush;
        private static bool s_devPushPending;

        private static int DevPush()
        {
            if (!s_devPushRead)
            {
                s_devPushRead = true;
                s_devPush = Debug.isDebugBuild ? LaunchOptions.ReadIntentInt(IntentExtraDevPush, 0) : 0;
                s_devPushPending = s_devPush >= 1;
                if (s_devPush >= 1)
                {
                    Debug.Log(NotificationsLogPrefix + "desarrollo: af_dev_push = " + s_devPush
                        + (DeviceNotifications.Available ? string.Empty : " (sin el programador real: sigue el falso)"));
                }
            }

            return s_devPush;
        }

        static partial void CreatePlatformScheduler(IClock clock, ref INotificationScheduler? scheduler)
        {
            // Nombre y descripción del canal, como los ve el jugador en los ajustes del teléfono (B6-Q8). Sin texto en el idioma del
            // jugador, Android exige un nombre: va el id técnico del canal, nunca otro idioma (D-018).
            string name = Loc.Has("push_channel_name") ? Loc.T("push_channel_name") : string.Empty;
            string description = Loc.Has("push_channel_description") ? Loc.T("push_channel_description") : string.Empty;
            INotificationScheduler? device = DeviceNotifications.Create(clock, name, description);
            if (device == null)
            {
                return;
            }

            scheduler = DevPush() >= 2 ? new DevSoonNotificationScheduler(device, clock) : device;
        }

        partial void TickPlatformScheduler()
        {
            INotificationScheduler? scheduler = _notificationScheduler;
            DeviceNotifications.Poll(scheduler is DevSoonNotificationScheduler soon ? soon.Inner : scheduler);

            // af_dev_push (solo desarrollo): la fila «Avisos» de Ajustes encendida, cuando la edad y el consentimiento ya están
            // decididos (nunca sobre la pantalla de edad ni con el bloqueo de <13).
            if (s_devPushPending && _push != null && !ConsentHoldsSession && !ConsentBlocked && DevPush() >= 1)
            {
                s_devPushPending = false;
                Debug.Log(NotificationsLogPrefix + "desarrollo: af_dev_push enciende «Avisos» como desde Ajustes");
                _push.SetFromSettings(true, granted => Debug.Log(NotificationsLogPrefix + "desarrollo: avisos " + (granted ? "concedidos" : "sin conceder")));
            }
        }

        /// <summary>
        /// Banda &lt;13 (08 §7: «no se crea sesión de juego» ni avisos): cancela lo que hubiera programado y la bandeja. En el arranque
        /// bloqueado aún no hay programador (las sesiones no se crean): lo hace <see cref="DeviceNotifications.CancelAllOnDevice"/>.
        /// </summary>
        private void CancelNotificationsForBlock()
        {
            if (_notificationScheduler != null)
            {
                _notificationScheduler.CancelAll();
                return;
            }

            DeviceNotifications.CancelAllOnDevice();
        }

        /// <summary>Tests: olvida la lectura de <c>af_dev_push</c>.</summary>
        internal static void ResetDevPush()
        {
            s_devPushRead = false;
            s_devPush = 0;
            s_devPushPending = false;
        }

        /// <summary>
        /// Solo con <c>af_dev_push</c> = 2 (desarrollo): el primer aviso tras cada <see cref="CancelAll"/> sale a
        /// <see cref="DevPushSoonSeconds"/> s, con su tipo, su id y su texto, para verlo llegar en el teléfono sin esperar al día
        /// siguiente. El resto, igual que el programador real.
        /// </summary>
        internal sealed class DevSoonNotificationScheduler : INotificationScheduler
        {
            private readonly IClock _clock;
            private bool _shifted;

            public DevSoonNotificationScheduler(INotificationScheduler inner, IClock clock)
            {
                Inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            }

            public INotificationScheduler Inner { get; }

            public PushPermission Permission => Inner.Permission;

            public bool PermissionRequired => Inner.PermissionRequired;

            public void RequestPermission(Action<PushPermission> done) => Inner.RequestPermission(done);

            public void Schedule(LocalNotification notification)
            {
                if (notification == null)
                {
                    throw new ArgumentNullException(nameof(notification));
                }

                if (_shifted)
                {
                    Inner.Schedule(notification);
                    return;
                }

                _shifted = true;
                DateTime soon = _clock.UtcNow.AddSeconds(DevPushSoonSeconds);
                Debug.Log(NotificationsLogPrefix + "desarrollo: " + notification.Type + " adelantado a " + DevPushSoonSeconds + " s");
                Inner.Schedule(new LocalNotification(notification.Id, notification.Type, notification.Title, notification.Body, soon));
            }

            public void CancelAll()
            {
                _shifted = false;
                Inner.CancelAll();
            }

            public IReadOnlyList<DeliveredNotification> TakeDelivered() => Inner.TakeDelivered();

            public string? TakeOpenedType() => Inner.TakeOpenedType();
        }
    }
}
