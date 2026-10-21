#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Platform
{
    /// <summary>
    /// Permiso del sistema para enseñar notificaciones (Android 13+: <c>POST_NOTIFICATIONS</c>; 06 §8.1). Con Android 12 o anterior
    /// no hay permiso en tiempo de ejecución y el sistema responde <see cref="Allowed"/> salvo que el jugador las haya apagado en los
    /// ajustes del teléfono (<see cref="Blocked"/>).
    /// </summary>
    public enum PushPermission
    {
        /// <summary>Aún no se ha pedido el diálogo nativo (Android 13+).</summary>
        NotRequested = 0,

        /// <summary>El sistema deja enseñar notificaciones.</summary>
        Allowed = 1,

        /// <summary>El jugador rechazó el diálogo nativo, que aún se puede volver a pedir.</summary>
        Denied = 2,

        /// <summary>Rechazado sin volver a preguntar, o apagado en los ajustes del teléfono: solo se cambia desde allí.</summary>
        Blocked = 3,
    }

    /// <summary>
    /// Una notificación local ya redactada (06 §8.2, «Canal técnico»: en la 1.0 todas son locales). <see cref="Id"/> es estable por
    /// tipo y día (lo pone <c>AF.LiveOps.NotificationPlanner</c>); <see cref="Type"/> es el valor de <c>type</c> de
    /// <c>af_push_sent</c> y <c>af_push_open</c> (08 §8.2), nunca texto visible. <see cref="Title"/> y <see cref="Body"/> ya vienen de
    /// las tablas de textos (<c>.claude/rules/localizacion.md</c>): el programador no los compone.
    /// </summary>
    public sealed class LocalNotification
    {
        /// <summary>Crea la notificación.</summary>
        /// <exception cref="ArgumentException">Falta el tipo o la hora no es UTC.</exception>
        public LocalNotification(int id, string type, string title, string body, DateTime fireAtUtc)
        {
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentException("Falta el tipo de la notificación.", nameof(type));
            }

            if (fireAtUtc.Kind == DateTimeKind.Local)
            {
                throw new ArgumentException("La hora de una notificación va en UTC.", nameof(fireAtUtc));
            }

            Id = id;
            Type = type;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            FireAtUtc = DateTime.SpecifyKind(fireAtUtc, DateTimeKind.Utc);
        }

        /// <summary>Identificador estable (el mismo tipo y día dan el mismo id).</summary>
        public int Id { get; }

        /// <summary>Tipo para la analítica (<c>daily_ready</c>, <c>comeback_d7</c>…).</summary>
        public string Type { get; }

        /// <summary>Título, ya localizado (sin emojis: 06 §8.3).</summary>
        public string Title { get; }

        /// <summary>Texto, ya localizado.</summary>
        public string Body { get; }

        /// <summary>Hora de entrega, UTC.</summary>
        public DateTime FireAtUtc { get; }
    }

    /// <summary>Una notificación que el sistema ya entregó (para <c>af_push_sent</c> en la siguiente apertura; 08 §8.2).</summary>
    public readonly struct DeliveredNotification
    {
        /// <summary>Crea el apunte.</summary>
        public DeliveredNotification(int id, string type, DateTime fireAtUtc)
        {
            Id = id;
            Type = type;
            FireAtUtc = fireAtUtc;
        }

        /// <summary>Identificador.</summary>
        public int Id { get; }

        /// <summary>Tipo para la analítica.</summary>
        public string Type { get; }

        /// <summary>Hora prevista de entrega, UTC.</summary>
        public DateTime FireAtUtc { get; }
    }

    /// <summary>
    /// Puente con las notificaciones locales del sistema (08 §2.2, fila <c>AF.Platform</c>: «Unity Mobile Notifications (locales)»).
    /// El juego solo habla con esta interfaz: el editor y los tests usan <see cref="FakeNotificationScheduler"/>, y el teléfono, la
    /// implementación real sobre <c>com.unity.mobile.notifications</c> (Q-M5 de D-033; parche aparte, <c>B6-notifications-real.diff</c>),
    /// como los SDK detrás de sus interfaces. Se llama solo desde el hilo principal.
    /// <para>
    /// Uso (06 §8.2, «Canal técnico»): al pasar a segundo plano, <see cref="CancelAll"/> y un <see cref="Schedule"/> por cada aviso
    /// del plan; al abrir, <see cref="TakeDelivered"/> y <see cref="TakeOpenedType"/> para la analítica y <see cref="CancelAll"/>.
    /// </para>
    /// </summary>
    public interface INotificationScheduler
    {
        /// <summary>Estado actual del permiso del sistema.</summary>
        PushPermission Permission { get; }

        /// <summary>
        /// El sistema necesita el diálogo nativo (Android 13+). Con false, <see cref="RequestPermission"/> responde enseguida con el
        /// estado actual.
        /// </summary>
        bool PermissionRequired { get; }

        /// <summary>
        /// Pide el diálogo nativo (06 §8.1: solo tras «Avísame» en el pre-prompt propio o desde Ajustes) y llama a
        /// <paramref name="done"/> una sola vez con el resultado, quizá en otro fotograma.
        /// </summary>
        void RequestPermission(Action<PushPermission> done);

        /// <summary>Programa <paramref name="notification"/>; otra con el mismo id la sustituye.</summary>
        void Schedule(LocalNotification notification);

        /// <summary>Cancela todas las programadas y quita de la bandeja las ya enseñadas.</summary>
        void CancelAll();

        /// <summary>Las entregadas desde la última llamada (y las borra de la lista).</summary>
        IReadOnlyList<DeliveredNotification> TakeDelivered();

        /// <summary>El tipo de la notificación con la que el jugador abrió la app, o null; solo la primera vez que se pregunta.</summary>
        string? TakeOpenedType();
    }
}
