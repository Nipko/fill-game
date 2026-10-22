#nullable enable
using System;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>
    /// Un paso de <c>af_push_prompt</c> (08 §8.2: <c>step ∈ {pre, native}</c>, <c>result</c>). Los valores de <c>result</c> no los fija
    /// el GDD [P]: <c>accepted</c> / <c>declined</c> en el pre-prompt y <c>granted</c> / <c>denied</c> en el diálogo nativo. Lleva
    /// también <c>origin</c> [P] (<c>first</c>, <c>second</c> o <c>settings</c>) para separar el primero, el segundo y Ajustes.
    /// </summary>
    public readonly struct PushPromptEvent
    {
        /// <summary><c>pre</c>.</summary>
        public const string StepPre = "pre";

        /// <summary><c>native</c>.</summary>
        public const string StepNative = "native";

        /// <summary>Crea el evento.</summary>
        public PushPromptEvent(string step, string result, string origin)
        {
            Step = step;
            Result = result;
            Origin = origin;
        }

        /// <summary><c>step</c>.</summary>
        public string Step { get; }

        /// <summary><c>result</c>.</summary>
        public string Result { get; }

        /// <summary><c>origin</c> [P].</summary>
        public string Origin { get; }
    }

    /// <summary>
    /// <c>settings.push</c> y el permiso del sistema (06 §8.1; 08 §4.3): el pre-prompt en la victoria (<see cref="OnVictory"/>), el
    /// acceso de Ajustes (<see cref="SetFromSettings"/>) y la puesta al día con el teléfono al abrir (<see cref="SyncWithSystem"/>).
    /// El diálogo nativo (<c>POST_NOTIFICATIONS</c>, Android 13+) solo se pide tras «Avísame» o desde Ajustes, nunca en frío. Los
    /// avisos se programan solo con <see cref="CanNotify"/>: el jugador dijo que sí y el sistema los deja [P] (pregunta B6-Q3: en
    /// Android 12 o anterior no hay permiso, pero tampoco se avisa antes de «Avísame»).
    /// </summary>
    public sealed class PushPermissionService
    {
        private readonly IPushSettingsStore _store;
        private readonly INotificationScheduler _scheduler;
        private bool _asking;

        /// <summary>Crea el servicio.</summary>
        public PushPermissionService(IPushSettingsStore store, INotificationScheduler scheduler, PushConfig? config = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            Config = config ?? PushConfig.Defaults;
            Config.Validate();
        }

        /// <summary>Cifras en uso.</summary>
        public PushConfig Config { get; }

        /// <summary>Cada paso de <c>af_push_prompt</c>, para la analítica (aún no hay <c>AF.Analytics</c>: lo escucha la sonda).</summary>
        public event Action<PushPromptEvent>? Reported;

        /// <summary><c>settings.push</c> en vivo.</summary>
        public PushSettingsRecord Settings => _store.Load();

        /// <summary>Se pueden programar avisos: <c>granted</c> y el sistema los permite.</summary>
        public bool CanNotify => _store.Load().Granted && _scheduler.Permission == PushPermission.Allowed;

        /// <summary>Hay un pre-prompt o un diálogo nativo abierto.</summary>
        public bool Asking => _asking;

        /// <summary>
        /// Al abrir (cada sesión): si el jugador apagó los avisos en el teléfono, <c>granted</c> pasa a false. Nunca lo sube: encenderlos
        /// en el teléfono no salta el pre-prompt [P]. Devuelve si cambió.
        /// </summary>
        public bool SyncWithSystem()
        {
            PushSettingsRecord record = _store.Load();
            if (record.Granted && _scheduler.Permission != PushPermission.Allowed)
            {
                record.Granted = false;
                _store.Save(record);
                return true;
            }

            return false;
        }

        /// <summary>
        /// En cada victoria, antes de ver si sale el pre-prompt: si alcanza el hito 3 tras el primero con el segundo pendiente, guarda
        /// su sesión (<c>second_due_session</c>; D-033, UI de los avisos, (b)), así que si esta victoria no lo lleva (el hito 3 en la
        /// misma sesión que el primero) sale en la primera victoria de la sesión siguiente. Devuelve si lo guardó.
        /// </summary>
        public bool RememberMilestone(in PrePromptMoment moment)
        {
            PushSettingsRecord record = _store.Load();
            if (!PushPrePromptRules.RemembersMilestone(record, in moment))
            {
                return false;
            }

            record.SecondDueSession = moment.Sessions;
            _store.Save(record);
            return true;
        }

        /// <summary>El pre-prompt que tocaría en <paramref name="moment"/> (sin enseñarlo).</summary>
        public PrePromptKind Due(in PrePromptMoment moment)
        {
            return _asking ? PrePromptKind.None : PushPrePromptRules.Due(_store.Load(), Config, _scheduler.Permission, in moment);
        }

        /// <summary>
        /// En una victoria: si toca, enseña el pre-prompt con <paramref name="presenter"/>, apunta que salió y, con «Avísame», pide el
        /// diálogo nativo. Devuelve el pre-prompt enseñado (o <see cref="PrePromptKind.None"/>). <paramref name="done"/> recibe al final
        /// si quedaron concedidos los avisos.
        /// </summary>
        public PrePromptKind OnVictory(in PrePromptMoment moment, IPushPrePromptPresenter presenter, Action<bool>? done = null)
        {
            if (presenter == null)
            {
                throw new ArgumentNullException(nameof(presenter));
            }

            PrePromptKind kind = Due(in moment);
            if (kind == PrePromptKind.None)
            {
                return kind;
            }

            _asking = true;
            DateTime shownAt = moment.NowUtc;
            string origin = kind == PrePromptKind.First ? "first" : "second";
            presenter.Show(new PrePromptRequest(kind), accepted =>
            {
                PushSettingsRecord record = _store.Load();
                if (kind == PrePromptKind.First)
                {
                    record.PrePromptShown = true;
                    record.PrePromptAt = shownAt;
                }
                else
                {
                    record.SecondPromptShown = true;
                }

                _store.Save(record);
                Reported?.Invoke(new PushPromptEvent(PushPromptEvent.StepPre, accepted ? "accepted" : "declined", origin));
                if (!accepted)
                {
                    _asking = false;
                    done?.Invoke(false);
                    return;
                }

                AskSystem(origin, done);
            });
            return kind;
        }

        /// <summary>
        /// El acceso de Ajustes (06 §8.1: «después, solo un acceso en Ajustes»). Encender pide el diálogo nativo si hace falta (si el
        /// teléfono lo tiene bloqueado, no cambia nada y <paramref name="done"/> recibe false: la UI explica cómo activarlo en el
        /// teléfono [P]); apagar pone <c>granted</c> = false y cancela los avisos programados.
        /// </summary>
        public void SetFromSettings(bool on, Action<bool>? done = null)
        {
            if (_asking)
            {
                done?.Invoke(_store.Load().Granted);
                return;
            }

            PushSettingsRecord record = _store.Load();
            if (!on)
            {
                if (record.Granted)
                {
                    record.Granted = false;
                    _store.Save(record);
                }

                _scheduler.CancelAll();
                done?.Invoke(false);
                return;
            }

            if (_scheduler.Permission == PushPermission.Allowed)
            {
                if (!record.Granted)
                {
                    record.Granted = true;
                    _store.Save(record);
                }

                done?.Invoke(true);
                return;
            }

            if (_scheduler.Permission == PushPermission.Blocked)
            {
                done?.Invoke(false);
                return;
            }

            _asking = true;
            AskSystem("settings", done);
        }

        private void AskSystem(string origin, Action<bool>? done)
        {
            bool required = _scheduler.PermissionRequired && _scheduler.Permission != PushPermission.Allowed;
            _scheduler.RequestPermission(permission =>
            {
                bool granted = permission == PushPermission.Allowed;
                PushSettingsRecord record = _store.Load();
                record.Granted = granted;
                _store.Save(record);
                if (required)
                {
                    Reported?.Invoke(new PushPromptEvent(PushPromptEvent.StepNative, granted ? "granted" : "denied", origin));
                }

                _asking = false;
                done?.Invoke(granted);
            });
        }
    }
}
