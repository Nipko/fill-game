#nullable enable
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AF.Presentation
{
    /// <summary>
    /// Reenvía el touch-down de un objeto de UI a una acción. Solo para saltar Sumergir y teñir o la celebración, que 02 §14
    /// y §4.2 quieren inmediatos ("salta al estado final en 0 ms"). Las decisiones de juego (selección, vertido,
    /// deselección C7) se toman en el touch-up (02 §3.1): para eso están <see cref="PointerTapRelay"/> y
    /// <see cref="CuiaView"/>.
    /// </summary>
    public sealed class PointerDownRelay : MonoBehaviour, IPointerDownHandler
    {
        private Action? _onDown;

        /// <summary>Acción que se llama en cada pulsación.</summary>
        public void Bind(Action onDown)
        {
            _onDown = onDown;
        }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            _onDown?.Invoke();
        }
    }
}
