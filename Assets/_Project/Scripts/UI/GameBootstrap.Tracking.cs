#nullable enable
using AF.Platform;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// El aviso de rastreo de Apple (ATT; fase M3 de <c>docs/dev/traspaso/mac-ios.md</c>; regla aprobada por el dueño el 2026-11-04):
    /// en iOS, tras la edad y el consentimiento del arranque y solo si <see cref="IosTracking.ShouldAsk"/> (18+ con anuncios
    /// personalizados e iOS sin preguntar aún), la tarjeta propia (<see cref="AttPrePromptCard"/>) y, con «Continuar», el aviso del
    /// sistema. Una sola vez: después iOS guarda la respuesta y el estado ya no es «sin decidir». Ni en Android ni en el editor.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>El aviso de rastreo de los tests (null = el de la plataforma: solo el iPhone tiene uno).</summary>
        internal static ITrackingPort? TrackingPortOverride { get; set; }

        private AttPrePromptCard? _attCard;

        /// <summary>La tarjeta del aviso de rastreo (null si no se creó).</summary>
        internal AttPrePromptCard? AttCard => _attCard;

        /// <summary>Lo último que respondió iOS en esta sesión (null si no se preguntó).</summary>
        internal TrackingStatus? TrackingAnswer { get; private set; }

        // Tras FinishConsentAtBoot (con los flags aplicados y sin bloqueo): la tarjeta, si toca.
        private void OfferTrackingAfterConsent()
        {
            ConsentFlow? flow = _consentFlow;
            ITrackingPort? port = TrackingPortOverride ?? IosTracking.ForPlatform();
            if (flow == null || port == null || _art == null || (_attCard != null && _attCard.Visible)
                || !IosTracking.ShouldAsk(flow.Flags, port.Status))
            {
                return;
            }

            _attCard ??= new AttPrePromptCard(transform, _art);
            _attCard.Show(() => port.Request(status =>
            {
                TrackingAnswer = status;
                if (DevProbe.Enabled)
                {
                    Debug.Log("AF_ATT respuesta=" + status);
                }

                _a11y?.Invalidate();
            }));
            _a11y?.Invalidate();
            if (DevProbe.Enabled && _probe != null)
            {
                var ids = new System.Collections.Generic.List<string>(1);
                var rects = new System.Collections.Generic.List<RectTransform>(1);
                _attCard.VisibleButtons(ids, rects);
                _probe.Layout("att_preprompt", 0, null, ids.ToArray(), rects.ToArray());
            }
        }

        // Cada fotograma: la tarjeta a la pantalla (si cambió el área segura o el tamaño).
        private void TickTracking()
        {
            _attCard?.Tick();
        }
    }
}
