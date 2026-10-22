#nullable enable
#if AF_MOBILE_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Unity.Notifications.Android;
using UnityEngine;

namespace AF.Platform.Notifications
{
    /// <summary>
    /// <see cref="INotificationScheduler"/> de Android sobre <c>com.unity.mobile.notifications</c> 2.4.3 (Q-M5 de D-033 y su añadido
    /// del 2026-10-21; parche <c>B6-notifications-real.diff</c>, adaptado). Solo existe en el teléfono, con el paquete
    /// (<c>AF_MOBILE_NOTIFICATIONS</c>); lo crea <see cref="DeviceNotifications.Create"/>.
    /// <list type="bullet">
    /// <item>Un canal, <see cref="DeviceNotifications.ChannelId"/> [P], con importancia por defecto (sonido del sistema, sin
    /// pantalla completa).</item>
    /// <item>Alarmas inexactas (Q-M5): los ajustes del paquete se quedan en sus valores por defecto («Schedule at exact time»
    /// apagado y «Reschedule on Device Restart» apagado), así que el manifiesto solo gana <c>POST_NOTIFICATIONS</c>, sin
    /// <c>SCHEDULE_EXACT_ALARM</c>, <c>USE_EXACT_ALARM</c> ni <c>RECEIVE_BOOT_COMPLETED</c> (lo comprueba
    /// <c>ci/check-manifest.ps1</c>). Sin reprogramar al reiniciar: los avisos de un teléfono reiniciado vuelven en la siguiente
    /// salida de la app.</item>
    /// <item>El permiso <c>POST_NOTIFICATIONS</c> (Android 13+) solo se pide en <see cref="RequestPermission"/>, que el juego llama
    /// tras «Avísame» o desde Ajustes (06 §8.1), nunca al arrancar: crear el canal no lo pide con el target API 36.</item>
    /// <item>La hora de entrega va en UTC (el paquete la pasa a milisegundos desde 1970 con <c>ToUniversalTime</c>): un cambio de
    /// zona después de programar no la mueve.</item>
    /// <item>Para <c>af_push_sent</c> (08 §8.2: «registrada en la siguiente apertura») guarda en <see cref="PlayerPrefs"/> los
    /// programados (<c>id:tipo:ticks</c>, sin datos personales) y al abrir da por entregados los vencidos que el sistema ya no
    /// tiene como programados (aproximado: uno descartado por el sistema también cuenta).</item>
    /// </list>
    /// </summary>
    internal sealed class AndroidNotificationScheduler : INotificationScheduler
    {
        private const string PrefsKey = "af_push_scheduled";
        private const int Android13 = 33;

        // La intención que abrió la app es del proceso, no de la escena: la reconstrucción por idioma crea otro programador y no
        // debe volver a contar la misma apertura (af_push_open).
        private static int? s_lastIntentId;

        private readonly IClock _clock;
        private readonly List<Entry> _scheduled = new List<Entry>();
        private readonly List<DeliveredNotification> _delivered = new List<DeliveredNotification>();
        private readonly int _sdk;
        private PermissionRequest? _request;
        private Action<PushPermission>? _pending;

        /// <summary>Inicializa el paquete y registra el canal con su nombre y descripción ya localizados.</summary>
        public AndroidNotificationScheduler(IClock clock, string channelName, string channelDescription)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _sdk = SdkInt();
            AndroidNotificationCenter.Initialize();
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel(
                DeviceNotifications.ChannelId,
                string.IsNullOrEmpty(channelName) ? DeviceNotifications.ChannelId : channelName,
                channelDescription ?? string.Empty,
                Importance.Default));
            Load();
        }

        /// <inheritdoc />
        public PushPermission Permission => Map(AndroidNotificationCenter.UserPermissionToPost);

        /// <inheritdoc />
        public bool PermissionRequired => _sdk >= Android13;

        /// <summary>Hay un diálogo nativo abierto esperando respuesta.</summary>
        public bool Requesting => _request != null;

        /// <inheritdoc />
        public void RequestPermission(Action<PushPermission> done)
        {
            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            if (_request != null)
            {
                // Ya hay un diálogo abierto: la respuesta irá a quien lo pidió primero; esta llamada recibe el estado de ahora.
                done(Permission);
                return;
            }

            PushPermission now = Permission;
            if (!PermissionRequired || now == PushPermission.Allowed || now == PushPermission.Blocked)
            {
                done(now);
                return;
            }

            _pending = done;
            _request = new PermissionRequest(); // diálogo nativo; la respuesta llega por sondeo (Poll)
        }

        /// <summary>Cada fotograma: entrega la respuesta del diálogo nativo cuando llega.</summary>
        public void Poll()
        {
            PermissionRequest? request = _request;
            if (request == null || request.Status == PermissionStatus.RequestPending)
            {
                return;
            }

            _request = null;
            Action<PushPermission>? done = _pending;
            _pending = null;
            done?.Invoke(Map(request.Status));
        }

        /// <inheritdoc />
        public void Schedule(LocalNotification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var n = new AndroidNotification(notification.Title, notification.Body, notification.FireAtUtc)
            {
                IntentData = notification.Type,
                ShowTimestamp = false,
                ShouldAutoCancel = true,
            };
            AndroidNotificationCenter.SendNotificationWithExplicitID(n, DeviceNotifications.ChannelId, notification.Id);
            for (int i = _scheduled.Count - 1; i >= 0; i--)
            {
                if (_scheduled[i].Id == notification.Id)
                {
                    _scheduled.RemoveAt(i);
                }
            }

            _scheduled.Add(new Entry(notification.Id, notification.Type, notification.FireAtUtc));
            Save();
        }

        /// <inheritdoc />
        public void CancelAll()
        {
            CollectDue();
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
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
            // La última intención que abrió la app; la misma no se vuelve a contar en este proceso.
            AndroidNotificationIntentData? intent = AndroidNotificationCenter.GetLastNotificationIntent();
            if (intent == null || intent.Id == s_lastIntentId)
            {
                return null;
            }

            s_lastIntentId = intent.Id;
            string? type = intent.Notification.IntentData;
            return string.IsNullOrEmpty(type) ? null : type;
        }

        /// <summary>Todo fuera del teléfono (lo programado y la bandeja) y la lista de programados, sin crear el canal.</summary>
        internal static void CancelEverything()
        {
            if (!AndroidNotificationCenter.Initialize())
            {
                return;
            }

            AndroidNotificationCenter.CancelAllScheduledNotifications();
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                PlayerPrefs.DeleteKey(PrefsKey);
                PlayerPrefs.Save();
            }
        }

        private void CollectDue()
        {
            DateTime now = _clock.UtcNow;
            bool allowed = Permission == PushPermission.Allowed;
            bool changed = false;
            for (int i = _scheduled.Count - 1; i >= 0; i--)
            {
                Entry e = _scheduled[i];
                if (e.FireAtUtc > now)
                {
                    continue;
                }

                if (allowed && AndroidNotificationCenter.CheckScheduledNotificationStatus(e.Id) != NotificationStatus.Scheduled)
                {
                    _delivered.Add(new DeliveredNotification(e.Id, e.Type, e.FireAtUtc));
                }

                _scheduled.RemoveAt(i);
                changed = true;
            }

            if (changed)
            {
                Save();
            }
        }

        private static PushPermission Map(PermissionStatus status)
        {
            switch (status)
            {
                case PermissionStatus.Allowed:
                    return PushPermission.Allowed;
                case PermissionStatus.Denied:
                    return PushPermission.Denied;
                case PermissionStatus.DeniedDontAskAgain:
                case PermissionStatus.NotificationsBlockedForApp:
                    return PushPermission.Blocked;
                default:
                    return PushPermission.NotRequested;
            }
        }

        private static int SdkInt()
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
        }

        private void Load()
        {
            _scheduled.Clear();
            string raw = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (raw.Length == 0)
            {
                return;
            }

            foreach (string item in raw.Split(';'))
            {
                string[] parts = item.Split(':');
                if (parts.Length == 3
                    && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
                    && long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks)
                    && parts[1].Length > 0 && ticks > 0 && ticks <= DateTime.MaxValue.Ticks)
                {
                    _scheduled.Add(new Entry(id, parts[1], new DateTime(ticks, DateTimeKind.Utc)));
                }
            }
        }

        private void Save()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < _scheduled.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(';');
                }

                sb.Append(_scheduled[i].Id.ToString(CultureInfo.InvariantCulture)).Append(':').Append(_scheduled[i].Type).Append(':')
                    .Append(_scheduled[i].FireAtUtc.Ticks.ToString(CultureInfo.InvariantCulture));
            }

            PlayerPrefs.SetString(PrefsKey, sb.ToString());
            PlayerPrefs.Save();
        }

        private readonly struct Entry
        {
            public Entry(int id, string type, DateTime fireAtUtc)
            {
                Id = id;
                Type = type;
                FireAtUtc = fireAtUtc;
            }

            public int Id { get; }

            public string Type { get; }

            public DateTime FireAtUtc { get; }
        }
    }
}
#endif
