#nullable enable
#if AF_MOBILE_NOTIFICATIONS && UNITY_IOS && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Notifications.iOS;
using UnityEngine;

namespace AF.Platform.Notifications
{
    /// <summary>
    /// <see cref="INotificationScheduler"/> de iOS sobre la parte de iOS de <c>com.unity.mobile.notifications</c> 2.4.3, el mismo paquete
    /// que en Android (Q-M5 de D-033; fase M2 de <c>docs/dev/traspaso/mac-ios.md</c>). Solo existe en el iPhone, con el paquete
    /// (<c>AF_MOBILE_NOTIFICATIONS</c>); lo crea <see cref="DeviceNotifications.Create"/>.
    /// <list type="bullet">
    /// <item>Solo locales: sin la capacidad de notificaciones remotas ni entitlement (los ajustes del paquete los dejan apagados,
    /// <c>AF.EditorTools.ProjectSetup.ApplyIosDefaults</c>).</item>
    /// <item>El permiso se pide solo en <see cref="RequestPermission"/> (06 §8.1: tras «Avísame» o desde Ajustes), nunca al arrancar
    /// («Request Authorization on App Launch» apagado en los mismos ajustes). En iOS siempre hay diálogo (<see cref="PermissionRequired"/>)
    /// y un rechazo no se puede volver a pedir: es <see cref="PushPermission.Blocked"/>, que solo se cambia en Ajustes.</item>
    /// <item>La hora de entrega va en UTC (disparador de calendario con <c>UtcTime</c>), como en Android: un cambio de zona después de
    /// programar no la mueve. Con el juego abierto no se enseña (<c>ShowInForeground</c> apagado).</item>
    /// <item>Para <c>af_push_sent</c> guarda lo programado en <see cref="PlayerPrefs"/> (<see cref="ScheduledNotificationLog"/>) y al
    /// abrir da por entregados los vencidos que iOS ya no tiene como programados, igual que Android.</item>
    /// <item>El aviso con el que se abrió la app sale de <c>GetLastRespondedNotification</c>: su <c>Data</c> es el tipo. Para darlo
    /// una sola vez se recuerda el último tomado (identificador y hora de entrega) en <see cref="PlayerPrefs"/>.</item>
    /// </list>
    /// </summary>
    internal sealed class IosNotificationScheduler : INotificationScheduler
    {
        private const string PrefsKey = "af_push_scheduled";
        private const string OpenedMarkKey = "af_push_opened_mark";
        private const AuthorizationOption Options = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;

        private readonly IClock _clock;
        private readonly List<DeliveredNotification> _scheduled = new List<DeliveredNotification>();
        private readonly List<DeliveredNotification> _delivered = new List<DeliveredNotification>();
        private AuthorizationRequest? _request;
        private Action<PushPermission>? _pending;

        /// <summary>Lee la lista de programados. No pide nada al sistema.</summary>
        public IosNotificationScheduler(IClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            ScheduledNotificationLog.Parse(PlayerPrefs.GetString(PrefsKey, string.Empty), _scheduled);
        }

        /// <inheritdoc />
        public PushPermission Permission => Map(iOSNotificationCenter.GetNotificationSettings().AuthorizationStatus);

        /// <inheritdoc />
        public bool PermissionRequired => true;

        /// <inheritdoc />
        public void RequestPermission(Action<PushPermission> done)
        {
            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            if (_request != null)
            {
                done(Permission); // ya hay un diálogo abierto: la respuesta irá a quien lo pidió primero
                return;
            }

            PushPermission now = Permission;
            if (now != PushPermission.NotRequested)
            {
                done(now);
                return;
            }

            _pending = done;
            _request = new AuthorizationRequest(Options, false); // diálogo nativo; la respuesta llega por sondeo (Poll)
        }

        /// <summary>Cada fotograma: entrega la respuesta del diálogo nativo cuando llega.</summary>
        public void Poll()
        {
            AuthorizationRequest? request = _request;
            if (request == null || !request.IsFinished)
            {
                return;
            }

            _request = null;
            Action<PushPermission>? done = _pending;
            _pending = null;
            bool granted = request.Granted;
            request.Dispose();
            done?.Invoke(granted ? PushPermission.Allowed : PushPermission.Blocked);
        }

        /// <inheritdoc />
        public void Schedule(LocalNotification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            DateTime at = notification.FireAtUtc;
            iOSNotificationCenter.ScheduleNotification(new iOSNotification(IdentifierOf(notification.Id))
            {
                Title = notification.Title,
                Body = notification.Body,
                Data = notification.Type,
                ShowInForeground = false,
                Trigger = new iOSNotificationCalendarTrigger
                {
                    Year = at.Year,
                    Month = at.Month,
                    Day = at.Day,
                    Hour = at.Hour,
                    Minute = at.Minute,
                    Second = at.Second,
                    UtcTime = true,
                    Repeats = false,
                },
            });
            for (int i = _scheduled.Count - 1; i >= 0; i--)
            {
                if (_scheduled[i].Id == notification.Id)
                {
                    _scheduled.RemoveAt(i);
                }
            }

            _scheduled.Add(new DeliveredNotification(notification.Id, notification.Type, at));
            Save();
        }

        /// <inheritdoc />
        public void CancelAll()
        {
            CollectDue();
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
            _scheduled.Clear();
            Save();
        }

        /// <inheritdoc />
        public IReadOnlyList<DeliveredNotification> TakeDelivered()
        {
            CollectDue();
            var taken = new List<DeliveredNotification>(_delivered);
            _delivered.Clear();
            return taken;
        }

        /// <inheritdoc />
        public string? TakeOpenedType()
        {
            iOSNotification? opened = iOSNotificationCenter.GetLastRespondedNotification();
            if (opened == null || string.IsNullOrEmpty(opened.Data))
            {
                return null;
            }

            // El mismo aviso (identificador y hora) solo cuenta una vez: iOS lo sigue devolviendo hasta que se toca otro.
            string mark = opened.Identifier + "|" + FireAtOf(opened);
            if (string.Equals(PlayerPrefs.GetString(OpenedMarkKey, string.Empty), mark, StringComparison.Ordinal))
            {
                return null;
            }

            PlayerPrefs.SetString(OpenedMarkKey, mark);
            PlayerPrefs.Save();
            return opened.Data;
        }

        /// <summary>Todo fuera del iPhone (lo programado y la bandeja) y la lista de programados.</summary>
        internal static void CancelEverything()
        {
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                PlayerPrefs.DeleteKey(PrefsKey);
                PlayerPrefs.Save();
            }
        }

        private static string IdentifierOf(int id)
        {
            return id.ToString(CultureInfo.InvariantCulture);
        }

        // La hora de entrega del disparador de calendario (la que puso Schedule), o vacía si no lo es.
        private static string FireAtOf(iOSNotification n)
        {
            if (n.Trigger is iOSNotificationCalendarTrigger c)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}T{3}:{4}:{5}", c.Year, c.Month, c.Day, c.Hour, c.Minute, c.Second);
            }

            return string.Empty;
        }

        private void CollectDue()
        {
            DateTime now = _clock.UtcNow;
            bool allowed = Permission == PushPermission.Allowed;
            HashSet<string>? pending = null;
            bool changed = false;
            for (int i = _scheduled.Count - 1; i >= 0; i--)
            {
                DeliveredNotification e = _scheduled[i];
                if (e.FireAtUtc > now)
                {
                    continue;
                }

                if (allowed)
                {
                    pending ??= PendingIdentifiers();
                    if (!pending.Contains(IdentifierOf(e.Id)))
                    {
                        _delivered.Add(e);
                    }
                }

                _scheduled.RemoveAt(i);
                changed = true;
            }

            if (changed)
            {
                Save();
            }
        }

        private static HashSet<string> PendingIdentifiers()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (iOSNotification n in iOSNotificationCenter.GetScheduledNotifications())
            {
                ids.Add(n.Identifier);
            }

            return ids;
        }

        private static PushPermission Map(AuthorizationStatus status)
        {
            switch (status)
            {
                case AuthorizationStatus.Authorized:
                case AuthorizationStatus.Provisional:
                case AuthorizationStatus.Ephemeral:
                    return PushPermission.Allowed;
                case AuthorizationStatus.Denied:
                    return PushPermission.Blocked;
                default:
                    return PushPermission.NotRequested;
            }
        }

        private void Save()
        {
            PlayerPrefs.SetString(PrefsKey, ScheduledNotificationLog.Format(_scheduled));
            PlayerPrefs.Save();
        }
    }
}
#endif
