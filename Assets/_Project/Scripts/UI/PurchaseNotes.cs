#nullable enable
using System;
using System.Collections.Generic;
using AF.IAP;

namespace AF.UI
{
    /// <summary>Qué nota lleva un producto de pago en la UI (F3-5; 08 §10.3).</summary>
    public enum PurchaseNoteKind
    {
        /// <summary>Nada que decir.</summary>
        None = 0,

        /// <summary>«Pendiente de confirmar»: cobrada (o pendiente de pago en Play) y aún sin verificar en el servidor; no se acreditó.</summary>
        Pending = 1,

        /// <summary>«No se pudo confirmar la compra»: el servidor la rechazó (<c>verify_failed</c>); ni se acreditó ni se consumió.</summary>
        Failed = 2,
    }

    /// <summary>
    /// Las notas de compra que enseñan el Mercado (B5) y Aguas estancadas (Puñado y Balsa de rescate) mientras una compra espera al
    /// servidor o tras un rechazo (F3-5). Las pone <see cref="Track"/> con cada <see cref="PurchaseVerificationEvent"/> de la tienda
    /// verificada: <c>verifying</c>, <c>pending</c>, en cola y <c>deferred_ack</c> → <see cref="PurchaseNoteKind.Pending"/>; <c>verify_failed</c>
    /// → <see cref="PurchaseNoteKind.Failed"/>; acreditada o ya aplicada → sin nota. <see cref="Version"/> sube con cada cambio, para
    /// que las vistas se rehagan solo entonces (sin asignaciones por fotograma: leer es un <c>TryGetValue</c>). Hilo principal.
    /// </summary>
    public sealed class PurchaseNotes
    {
        private readonly Dictionary<string, PurchaseNoteKind> _notes = new Dictionary<string, PurchaseNoteKind>(StringComparer.Ordinal);

        /// <summary>Sube con cada cambio.</summary>
        public int Version { get; private set; }

        /// <summary>La nota de <paramref name="sku"/>.</summary>
        public PurchaseNoteKind Of(string sku)
        {
            return sku != null && _notes.TryGetValue(sku, out PurchaseNoteKind kind) ? kind : PurchaseNoteKind.None;
        }

        /// <summary>La nota con más peso de dos productos (el fallo antes que lo pendiente): la de Puñado y Balsa en el modal.</summary>
        public PurchaseNoteKind Of(string first, string second)
        {
            PurchaseNoteKind a = Of(first);
            PurchaseNoteKind b = Of(second);
            return a == PurchaseNoteKind.Failed || b == PurchaseNoteKind.Failed ? PurchaseNoteKind.Failed
                : (a == PurchaseNoteKind.Pending || b == PurchaseNoteKind.Pending ? PurchaseNoteKind.Pending : PurchaseNoteKind.None);
        }

        /// <summary>Pone la nota que toca para <paramref name="e"/>.</summary>
        public void Track(PurchaseVerificationEvent e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            switch (e.State)
            {
                case PurchaseVerificationState.Verifying:
                case PurchaseVerificationState.Pending:
                case PurchaseVerificationState.Queued:
                case PurchaseVerificationState.DeferredAck:
                case PurchaseVerificationState.CreditFailed:
                    Set(e.Sku, PurchaseNoteKind.Pending); // sigue en la cola: se acreditará una vez cuando el servidor la verifique
                    break;
                case PurchaseVerificationState.Rejected:
                    Set(e.Sku, PurchaseNoteKind.Failed);
                    break;
                default:
                    Set(e.Sku, PurchaseNoteKind.None);
                    break;
            }
        }

        /// <summary>Quita los fallos (al cerrar el Mercado o abrir otro bloqueo): son un aviso de una vez; lo pendiente sigue.</summary>
        public void ClearFailures()
        {
            List<string>? failed = null;
            foreach (KeyValuePair<string, PurchaseNoteKind> note in _notes)
            {
                if (note.Value == PurchaseNoteKind.Failed)
                {
                    (failed ??= new List<string>()).Add(note.Key);
                }
            }

            if (failed == null)
            {
                return;
            }

            for (int i = 0; i < failed.Count; i++)
            {
                _notes.Remove(failed[i]);
            }

            Version++;
        }

        private void Set(string sku, PurchaseNoteKind kind)
        {
            if (Of(sku) == kind)
            {
                return;
            }

            if (kind == PurchaseNoteKind.None)
            {
                _notes.Remove(sku);
            }
            else
            {
                _notes[sku] = kind;
            }

            Version++;
        }
    }
}
