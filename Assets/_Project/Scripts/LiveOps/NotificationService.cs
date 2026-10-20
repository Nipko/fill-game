#nullable enable
using System;
using System.Collections.Generic;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>
    /// Escribe el texto de un aviso con la tabla de textos (en el juego, <c>Loc.T</c>; <c>AF.LiveOps</c> no depende de
    /// <c>AF.Localization</c>). Nunca compone frases: solo resuelve la clave con sus marcadores.
    /// </summary>
    public interface INotificationTexts
    {
        /// <summary>El texto de <paramref name="key"/> con <paramref name="args"/>, o vacío si no hay traducción (entonces no se avisa).</summary>
        string Resolve(string key, IReadOnlyList<NotificationArg> args);
    }

    /// <summary>Lo que se supo al volver a abrir (<see cref="NotificationService.OnForeground"/>).</summary>
    public sealed class NotificationOpenReport
    {
        /// <summary>Crea el informe.</summary>
        public NotificationOpenReport(IReadOnlyList<DeliveredNotification> delivered, string? openedType)
        {
            Delivered = delivered;
            OpenedType = openedType;
        }

        /// <summary>Entregadas mientras la app estuvo fuera (un <c>af_push_sent</c> por cada una; 08 §8.2: 1.0, «registrada en la siguiente apertura»).</summary>
        public IReadOnlyList<DeliveredNotification> Delivered { get; }

        /// <summary>El tipo del aviso con el que se abrió la app (<c>af_push_open</c>), o null.</summary>
        public string? OpenedType { get; }
    }

    /// <summary>
    /// Los avisos locales de la 1.0 de punta a punta (06 §8.2, «Canal técnico»): al pasar a segundo plano, cancela lo que hubiera y
    /// programa el plan de <see cref="NotificationPlanner"/> si <see cref="PushPermissionService.CanNotify"/>; al abrir, recoge lo
    /// entregado y lo abierto para la analítica y cancela todo. Un aviso cuyo texto falta en el idioma del jugador no se programa
    /// (D-018: nunca otro idioma) [P].
    /// </summary>
    public sealed class NotificationService
    {
        private readonly INotificationScheduler _scheduler;
        private readonly PushPermissionService _permission;
        private readonly NotificationPlanner _planner;
        private readonly INotificationTexts _texts;
        private readonly List<PlannedNotification> _lastPlan = new List<PlannedNotification>();

        /// <summary>Crea el servicio.</summary>
        public NotificationService(INotificationScheduler scheduler, PushPermissionService permission, INotificationTexts texts)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _permission = permission ?? throw new ArgumentNullException(nameof(permission));
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            _planner = new NotificationPlanner(permission.Config);
        }

        /// <summary>El plan programado en el último paso a segundo plano (vacío sin permiso).</summary>
        public IReadOnlyList<PlannedNotification> LastPlan => _lastPlan;

        /// <summary>
        /// Al pasar a segundo plano (o al cerrar la app): cancela y vuelve a programar. Devuelve cuántos avisos quedaron programados.
        /// </summary>
        public int OnBackground(NotificationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _lastPlan.Clear();
            _scheduler.CancelAll();
            if (!_permission.CanNotify)
            {
                return 0;
            }

            string title = _texts.Resolve(NotificationTypes.TitleKey, Array.Empty<NotificationArg>());
            IReadOnlyList<PlannedNotification> plan = _planner.Plan(context);
            for (int i = 0; i < plan.Count; i++)
            {
                PlannedNotification p = plan[i];
                string body = _texts.Resolve(p.BodyKey, p.Args);
                if (body.Length == 0 || title.Length == 0)
                {
                    continue;
                }

                _scheduler.Schedule(new LocalNotification(p.Id, p.Type.ToId(), title, body, p.FireAtUtc));
                _lastPlan.Add(p);
            }

            return _lastPlan.Count;
        }

        /// <summary>Al abrir la app (arranque en frío o vuelta de segundo plano): recoge lo entregado y lo abierto, y cancela todo.</summary>
        public NotificationOpenReport OnForeground()
        {
            IReadOnlyList<DeliveredNotification> delivered = _scheduler.TakeDelivered();
            string? opened = _scheduler.TakeOpenedType();
            _scheduler.CancelAll();
            _lastPlan.Clear();
            return new NotificationOpenReport(delivered, opened);
        }
    }
}
