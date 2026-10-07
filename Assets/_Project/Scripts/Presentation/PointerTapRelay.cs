#nullable enable
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AF.Presentation
{
    /// <summary>
    /// Reenvía un toque completo (touch-down y touch-up sobre el mismo objeto) a una acción: la decisión se toma en el
    /// touch-up (02 §3.1). Si el dedo se suelta fuera, no pasa nada.
    /// </summary>
    public sealed class PointerTapRelay : MonoBehaviour, IPointerClickHandler
    {
        private Action? _onTap;

        /// <summary>Acción que se llama en cada toque.</summary>
        public void Bind(Action onTap)
        {
            _onTap = onTap;
        }

        /// <inheritdoc />
        public void OnPointerClick(PointerEventData eventData)
        {
            _onTap?.Invoke();
        }
    }
}
