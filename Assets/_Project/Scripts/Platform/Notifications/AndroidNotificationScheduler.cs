#nullable enable
#if AF_MOBILE_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
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
    /// programados (<see cref="ScheduledNotificationLog"/>: <c>id:tipo:ticks</c>, sin datos personales) y al abrir da por entregados los vencidos que el sistema ya no
    /// tiene como programados (aproximado: uno descartado por el sistema también cuenta).</item>
    /// </list>
    /// </summary>
    internal sealed class AndroidNotificationScheduler : INotificationScheduler
    {
        private const string PrefsKey = "af_push_scheduled";
        private const int Android13 = 33;

        // Extra que el paquete pone en la intención de abrir la app al tocar un aviso (UnityNotificationManager.KEY_NOTIFICATION_ID).
        private const string NotificationIdExtra = "com.unity.NotificationID";

        private readonly IClock _clock;
        private readonly List<DeliveredNotification> _scheduled = new List<DeliveredNotification>();
        private readonly List<DeliveredNotification> _delivered = new List<DeliveredNotification>();
        private readonly int _sdk;
        private PermissionRequest? _request;
        private Action<PushPermission>? _pending;
        private readonly NotificationIntentReader _intents = new NotificationIntentReader(new ActivityIntentPort());

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

            _scheduled.Add(new DeliveredNotification(notification.Id, notification.Type, notification.FireAtUtc));
            _intents.Remember(notification.Id, notification.Type);
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
            // Observación 19 de la jornada 6 (NotificationIntentReader): la intención se marca como leída en la actividad (un toque
            // nuevo con el mismo id cuenta; la misma intención, al volver de Recientes o tras la reconstrucción por idioma, no) y el
            // tipo sale de la lista de lo programado antes que de la reconstrucción del paquete, que en frío no lo encuentra.
            string? type = _intents.Take();
            if (type != null && Debug.isDebugBuild && _intents.LastDiagnostic != null)
            {
                Debug.Log(_intents.LastDiagnostic);
            }

            return type;
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
                DeliveredNotification e = _scheduled[i];
                if (e.FireAtUtc > now)
                {
                    continue;
                }

                if (allowed && AndroidNotificationCenter.CheckScheduledNotificationStatus(e.Id) != NotificationStatus.Scheduled)
                {
                    _delivered.Add(e);
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
            ScheduledNotificationLog.Parse(PlayerPrefs.GetString(PrefsKey, string.Empty), _scheduled);
            for (int i = 0; i < _scheduled.Count; i++)
            {
                _intents.Remember(_scheduled[i].Id, _scheduled[i].Type); // en frío, el tipo del aviso tocado sale de aquí
            }
        }

        /// <summary>
        /// La intención actual de la actividad de Unity (<c>getIntent</c>: la del arranque o, en caliente, la que deja
        /// <c>onNewIntent</c> con <c>setIntent</c>). Si trae el id de un aviso, lo lee, pide al paquete lo que sepa del aviso y quita
        /// el extra de esa intención, para no volver a leerla.
        /// </summary>
        private sealed class ActivityIntentPort : INotificationIntentPort
        {
            public bool TryTakeOpened(out int id, out string? packageType)
            {
                id = 0;
                packageType = null;
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject? activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity == null)
                    {
                        return false;
                    }

                    using (AndroidJavaObject? intent = activity.Call<AndroidJavaObject>("getIntent"))
                    {
                        if (intent == null || !intent.Call<bool>("hasExtra", NotificationIdExtra))
                        {
                            return false;
                        }

                        id = intent.Call<int>("getIntExtra", NotificationIdExtra, -1);
                        try
                        {
                            // Lo que el paquete reconstruya (solo de respaldo): antes de quitar el extra, que es lo que él lee.
                            packageType = AndroidNotificationCenter.GetLastNotificationIntent()?.Notification.IntentData;
                        }
                        catch (Exception)
                        {
                            packageType = null;
                        }

                        intent.Call("removeExtra", NotificationIdExtra);
                        return true;
                    }
                }
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
