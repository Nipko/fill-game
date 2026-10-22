#nullable enable
using System;

namespace AF.Platform.Notifications
{
    /// <summary>
    /// Entrada única a las notificaciones locales del teléfono (Q-M5 de D-033 y su añadido del 2026-10-21: paquete
    /// <c>com.unity.mobile.notifications</c> 2.4.3 y permiso <c>POST_NOTIFICATIONS</c>; nota <c>docs/dev/sesiones-notificaciones.md</c>
    /// §8). Todo lo que toca el paquete vive en este ensamblado, tras <c>AF_MOBILE_NOTIFICATIONS</c> (el <c>versionDefines</c> de
    /// <c>AF.Platform.Notifications.asmdef</c>), <c>UNITY_ANDROID</c> y <c>!UNITY_EDITOR</c>: en el editor, en los tests y sin el
    /// paquete, <see cref="Available"/> es false, <see cref="Create"/> devuelve null y el juego sigue con
    /// <see cref="FakeNotificationScheduler"/> (08 §2.1, principio 6: cada SDK detrás de su interfaz con una falsa para el editor).
    /// </summary>
    public static class DeviceNotifications
    {
        /// <summary>Canal único de los avisos [P] (B6-Q8: su nombre visible es <c>push_channel_name</c>).</summary>
        public const string ChannelId = "af_default";

        /// <summary>El programador real existe en este build (Android, fuera del editor y con el paquete).</summary>
        public static bool Available
        {
            get
            {
#if AF_MOBILE_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// El programador de Android con el canal registrado (su nombre y su descripción ya localizados), o null fuera del teléfono
        /// o sin el paquete. No pide el permiso: eso solo lo hace <see cref="INotificationScheduler.RequestPermission"/>, tras
        /// «Avísame» o desde Ajustes (06 §8.1).
        /// </summary>
        public static INotificationScheduler? Create(IClock clock, string channelName, string channelDescription)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

#if AF_MOBILE_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidNotificationScheduler(clock, channelName, channelDescription);
#else
            return null;
#endif
        }

        /// <summary>Cada fotograma: entrega la respuesta del diálogo nativo cuando llega (no hace nada con otro programador).</summary>
        public static void Poll(INotificationScheduler? scheduler)
        {
#if AF_MOBILE_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
            (scheduler as AndroidNotificationScheduler)?.Poll();
#endif
        }

        /// <summary>
        /// Cancela en el teléfono todo lo programado y lo que esté en la bandeja, sin crear el canal ni pedir nada. Lo usa el bloqueo
        /// de la banda &lt;13 (08 §7: sin sesión de juego ni avisos), que para el arranque antes de crear las sesiones. Fuera del
        /// teléfono no hace nada.
        /// </summary>
        public static void CancelAllOnDevice()
        {
#if AF_MOBILE_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationScheduler.CancelEverything();
#endif
        }
    }
}
