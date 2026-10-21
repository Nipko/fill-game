#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Platform
{
    /// <summary>
    /// <see cref="INotificationScheduler"/> falso para el editor, los tests y los builds sin el paquete de notificaciones (Q-M5 de
    /// D-033: la implementación real va en un parche aparte). No enseña nada: guarda las programadas en memoria y da por entregadas
    /// las que tienen la hora vencida según <see cref="IClock"/> si el permiso es <see cref="PushPermission.Allowed"/>. El diálogo
    /// nativo se simula con <see cref="NativeAnswer"/>.
    /// </summary>
    public sealed class FakeNotificationScheduler : INotificationScheduler
    {
        private readonly IClock _clock;
        private readonly List<LocalNotification> _scheduled = new List<LocalNotification>();
        private readonly List<DeliveredNotification> _delivered = new List<DeliveredNotification>();
        private string? _openedType;

        /// <summary>Crea el programador con el permiso <paramref name="permission"/>.</summary>
        /// <param name="clock">Reloj con el que vencen las programadas.</param>
        /// <param name="permission">Estado inicial del permiso.</param>
        /// <param name="permissionRequired">Simula Android 13+ (true) o Android 12 o anterior (false).</param>
        public FakeNotificationScheduler(IClock clock, PushPermission permission = PushPermission.NotRequested, bool permissionRequired = true)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Permission = permissionRequired ? permission : (permission == PushPermission.Blocked ? PushPermission.Blocked : PushPermission.Allowed);
            PermissionRequired = permissionRequired;
        }

        /// <inheritdoc />
        public PushPermission Permission { get; set; }

        /// <inheritdoc />
        public bool PermissionRequired { get; }

        /// <summary>Respuesta del diálogo nativo simulado (por defecto, aceptar). <see cref="PushPermission.NotRequested"/> deja la petición sin responder.</summary>
        public PushPermission NativeAnswer { get; set; } = PushPermission.Allowed;

        /// <summary>Veces que se pidió el diálogo nativo.</summary>
        public int PermissionRequests { get; private set; }

        /// <summary>Veces que se llamó a <see cref="CancelAll"/>.</summary>
        public int CancelCount { get; private set; }

        /// <summary>Las programadas ahora mismo, en el orden en que llegaron.</summary>
        public IReadOnlyList<LocalNotification> Scheduled => _scheduled;

        /// <summary>Petición que espera respuesta (con <see cref="NativeAnswer"/> = <see cref="PushPermission.NotRequested"/>).</summary>
        public Action<PushPermission>? PendingRequest { get; private set; }

        /// <inheritdoc />
        public void RequestPermission(Action<PushPermission> done)
        {
            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            if (!PermissionRequired || Permission == PushPermission.Allowed || Permission == PushPermission.Blocked)
            {
                done(Permission);
                return;
            }

            PermissionRequests++;
            if (NativeAnswer == PushPermission.NotRequested)
            {
                PendingRequest = done;
                return;
            }

            Permission = NativeAnswer;
            done(Permission);
        }

        /// <summary>Responde la petición pendiente con <paramref name="answer"/>.</summary>
        public void Answer(PushPermission answer)
        {
            Action<PushPermission>? done = PendingRequest;
            if (done == null)
            {
                throw new InvalidOperationException("No hay ninguna petición de permiso pendiente.");
            }

            PendingRequest = null;
            Permission = answer;
            done(answer);
        }

        /// <inheritdoc />
        public void Schedule(LocalNotification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            for (int i = 0; i < _scheduled.Count; i++)
            {
                if (_scheduled[i].Id == notification.Id)
                {
                    _scheduled[i] = notification;
                    return;
                }
            }

            _scheduled.Add(notification);
        }

        /// <inheritdoc />
        public void CancelAll()
        {
            CollectDue();
            CancelCount++;
            _scheduled.Clear();
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
            string? type = _openedType;
            _openedType = null;
            return type;
        }

        /// <summary>Simula que el jugador abrió la app tocando una notificación de tipo <paramref name="type"/>.</summary>
        public void SimulateOpenFrom(string type)
        {
            _openedType = type;
        }

        // Las vencidas pasan a entregadas (solo con permiso: sin él, el sistema no enseña nada).
        private void CollectDue()
        {
            DateTime now = _clock.UtcNow;
            for (int i = _scheduled.Count - 1; i >= 0; i--)
            {
                LocalNotification n = _scheduled[i];
                if (n.FireAtUtc <= now)
                {
                    if (Permission == PushPermission.Allowed)
                    {
                        _delivered.Add(new DeliveredNotification(n.Id, n.Type, n.FireAtUtc));
                    }

                    _scheduled.RemoveAt(i);
                }
            }

            _delivered.Sort((a, b) => a.FireAtUtc != b.FireAtUtc ? a.FireAtUtc.CompareTo(b.FireAtUtc) : a.Id.CompareTo(b.Id));
        }
    }
}
