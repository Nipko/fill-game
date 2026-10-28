#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// La lista desplazable de las pantallas (Ajustes, la Liga, el Mercado, «Tu avatar», la hoja de la fruta y las muestras del Mapa):
    /// un <see cref="ScrollRect"/> en el que un toque que llega mientras la lista se desplaza por inercia la <b>para sin activar la
    /// fila</b> que hay debajo, como en Android. Observación 3 de la jornada 4 de la prueba en el teléfono (2026-10-27): con la lista
    /// de Ajustes aún en movimiento, un toque activó «Corregir mi edad».
    /// <para>
    /// El módulo de entrada pide <c>initializePotentialDrag</c> a la lista justo después del <c>pointerDown</c> de la fila, y solo
    /// manda el clic si <see cref="PointerEventData.eligibleForClick"/> sigue en true al soltar; aquí se pone en false cuando la lista
    /// iba a más de <see cref="StopTapSpeedDp"/>. El arrastre que empieza con ese toque desplaza como siempre.
    /// </para>
    /// </summary>
    [AddComponentMenu("")]
    public sealed class FlingStopScrollRect : ScrollRect
    {
        /// <summary>
        /// Velocidad (dp/s, unidades del lienzo) por encima de la cual la lista «se mueve» y el toque solo la para [P]. Por debajo, lo
        /// que le queda de inercia (decae al 13,5 % por segundo) son menos de 15 dp y la lista ya parece quieta: el toque vale.
        /// </summary>
        public const float StopTapSpeedDp = 30f;

        /// <summary>El último toque llegó con la lista en movimiento: la paró y no activó nada.</summary>
        public bool LastTapStoppedFling { get; private set; }

        /// <summary>La lista se mueve por inercia lo bastante para que un toque solo la pare.</summary>
        public bool Flinging => velocity.sqrMagnitude > StopTapSpeedDp * StopTapSpeedDp;

        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            bool stop = eventData.button == PointerEventData.InputButton.Left && Flinging;
            base.OnInitializePotentialDrag(eventData); // pone la velocidad a 0: la lista se para
            LastTapStoppedFling = stop;
            if (stop)
            {
                eventData.eligibleForClick = false;
            }
        }
    }
}
