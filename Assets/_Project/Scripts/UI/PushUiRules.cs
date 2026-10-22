#nullable enable
using AF.LiveOps;
using AF.Platform;

namespace AF.UI
{
    /// <summary>Estado de la fila «Avisos» de Ajustes.</summary>
    public enum PushRowState
    {
        /// <summary>Sin fila: no hay avisos en este build (sin sesiones de B6) o la banda es &lt;13.</summary>
        Hidden = 0,

        /// <summary>Encendidos: <c>settings.push.granted</c> y el sistema los deja. Tocar la fila los apaga.</summary>
        On = 1,

        /// <summary>Apagados: tocar la fila los enciende (con el diálogo nativo si hace falta).</summary>
        Off = 2,

        /// <summary>Bloqueados en el teléfono (el diálogo nativo ya no sale): tocar la fila abre los ajustes de la app en Android.</summary>
        Blocked = 3,
    }

    /// <summary>Qué hace un toque en la fila «Avisos».</summary>
    public enum PushRowAction
    {
        /// <summary>Nada (sin fila).</summary>
        None = 0,

        /// <summary>Apagar: <c>granted</c> = false y se cancela lo programado.</summary>
        TurnOff = 1,

        /// <summary>Encender: <see cref="PushPermissionService.SetFromSettings"/> con true (pide el permiso si hace falta).</summary>
        TurnOn = 2,

        /// <summary>Abrir los ajustes de notificaciones de la app en el teléfono.</summary>
        OpenPhoneSettings = 3,
    }

    /// <summary>
    /// Reglas puras de la UI de los avisos (06 §8.1; nota de B6, §3): el estado de la fila de Ajustes y cuándo la tarjeta del
    /// pre-prompt puede salir al dejar una victoria. Sin motor, para los tests de EditMode.
    /// </summary>
    public static class PushUiRules
    {
        /// <summary>
        /// El estado de la fila: oculta sin servicio o con la banda &lt;13 (B12: ni sesiones ni avisos); bloqueada si el teléfono no
        /// deja pedirlos; encendida con <c>granted</c> y el permiso; si no, apagada.
        /// </summary>
        public static PushRowState RowState(bool hasService, bool under13, bool granted, PushPermission system)
        {
            if (!hasService || under13)
            {
                return PushRowState.Hidden;
            }

            if (system == PushPermission.Blocked)
            {
                return PushRowState.Blocked;
            }

            return granted && system == PushPermission.Allowed ? PushRowState.On : PushRowState.Off;
        }

        /// <summary>Lo que hace un toque en la fila en <paramref name="state"/>.</summary>
        public static PushRowAction ActionFor(PushRowState state)
        {
            switch (state)
            {
                case PushRowState.On: return PushRowAction.TurnOff;
                case PushRowState.Off: return PushRowAction.TurnOn;
                case PushRowState.Blocked: return PushRowAction.OpenPhoneSettings;
                default: return PushRowAction.None;
            }
        }

        /// <summary>
        /// La tarjeta del pre-prompt puede salir ahora (sin mirar aún si toca por las reglas de B6, <see cref="PushPrePromptRules"/>):
        /// con un presentador, sin la banda &lt;13 ni la edad o el consentimiento pendientes (B12), sin otro pre-prompt abierto y como
        /// mucho uno por sesión (<paramref name="shownInSession"/>: la sesión en que salió el último, 0 si ninguno).
        /// </summary>
        public static bool CanOffer(bool hasPresenter, bool under13, bool consentHolds, bool asking, int session, int shownInSession)
        {
            return hasPresenter && !under13 && !consentHolds && !asking && session > 0 && session != shownInSession;
        }
    }
}
