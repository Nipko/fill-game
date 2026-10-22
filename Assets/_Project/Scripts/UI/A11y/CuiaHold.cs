#nullable enable
using System;
using AF.Platform;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AF.UI
{
    /// <summary>
    /// C9, mantener pulsado (02 §3.1 y §12; bloque B9): un dedo que se queda al menos <see cref="A11yRules.HoldMs"/> ms sobre una
    /// cuia sin moverse (menos de <see cref="A11yRules.HoldSlopDp"/> dp [P]) muestra los nombres de sus dosis, y al soltarlo no
    /// selecciona (ni vierte). Lógica sin vista, con el reloj de la pantalla (los tests lo avanzan con <c>Step</c>):
    /// <list type="bullet">
    /// <item>solo sigue al primer dedo; si baja otro (C11), lo que estuviera en curso se cancela;</item>
    /// <item>moverse <see cref="A11yRules.HoldSlopDp"/> dp o más antes de tiempo lo cancela para siempre (ya es el arrastre,
    /// C10);</item>
    /// <item>una vez mostrados los nombres, soltar no hace nada más (<see cref="Release"/> devuelve true y quien llama se traga el
    /// toque y el arrastre) [P].</item>
    /// </list>
    /// Sin asignaciones.
    /// </summary>
    public sealed class CuiaHoldTracker
    {
        /// <summary>Sin cuia pulsada.</summary>
        public const int None = -1;

        private int _fingers;
        private int _pointer;
        private int _swallowed = None;
        private Vector2 _start;
        private float _heldMs;

        /// <summary>Crea el seguimiento, sin nada pulsado.</summary>
        public CuiaHoldTracker()
        {
            Reset();
        }

        /// <summary>Cuia bajo el dedo que se sigue (aún sin nombres o ya con ellos), o <see cref="None"/>.</summary>
        public int Index { get; private set; }

        /// <summary>Ya se mostraron los nombres de <see cref="Index"/> (el dedo sigue abajo).</summary>
        public bool Fired { get; private set; }

        /// <summary>Tiempo que lleva abajo el dedo que se sigue (ms).</summary>
        public float HeldMs => _heldMs;

        /// <summary>Un dedo <paramref name="pointer"/> baja en la cuia <paramref name="index"/>, en <paramref name="positionDp"/> (dp).</summary>
        public void Press(int index, int pointer, Vector2 positionDp)
        {
            _swallowed = None;
            _fingers++;
            if (_fingers > 1)
            {
                // C11: otro dedo mientras uno estaba abajo; nada de nombres hasta que se suelten todos (si ya salieron, se quedan
                // hasta soltar el primero).
                if (!Fired)
                {
                    Index = None;
                }

                return;
            }

            Index = index;
            _pointer = pointer;
            _start = positionDp;
            _heldMs = 0f;
            Fired = false;
        }

        /// <summary>El dedo <paramref name="pointer"/> se mueve a <paramref name="positionDp"/> (dp).</summary>
        public void Move(int pointer, Vector2 positionDp)
        {
            if (Index == None || Fired || pointer != _pointer)
            {
                return;
            }

            if ((positionDp - _start).sqrMagnitude >= A11yRules.HoldSlopDp * A11yRules.HoldSlopDp)
            {
                Index = None; // C10: ya es un arrastre; no vuelve a contar hasta soltar
            }
        }

        /// <summary>Avanza <paramref name="dtMs"/> ms; devuelve true en el instante en que se cumplen los 400 ms (enseñar los nombres).</summary>
        public bool Tick(float dtMs)
        {
            if (Index == None || Fired)
            {
                return false;
            }

            _heldMs += Mathf.Max(0f, dtMs);
            if (_heldMs < A11yRules.HoldMs)
            {
                return false;
            }

            Fired = true;
            return true;
        }

        /// <summary>
        /// Se suelta el dedo <paramref name="pointer"/> que bajó en la cuia <paramref name="index"/>: true si ese dedo ya había
        /// mostrado los nombres de esa cuia (quien llama no hace el toque ni el arrastre). Con el último dedo fuera, queda listo
        /// para el siguiente.
        /// </summary>
        public bool Release(int index, int pointer)
        {
            bool swallow = Fired && Index == index && pointer == _pointer;
            if (swallow)
            {
                _swallowed = index;
            }

            _fingers = Mathf.Max(0, _fingers - 1);
            if (swallow || pointer == _pointer)
            {
                Index = None;
                Fired = false;
            }

            if (_fingers == 0)
            {
                int swallowed = _swallowed;
                Reset();
                _swallowed = swallowed; // el touch-up de la cuia aún puede preguntar (Swallows)
            }

            return swallow;
        }

        /// <summary>
        /// El touch-up de la cuia <paramref name="index"/> que llega ahora es el de un C9 ya mostrado: no es un toque ni un arrastre.
        /// Vale antes o después de <see cref="Release"/> en el mismo touch-up (el orden de los componentes del objeto no importa).
        /// </summary>
        public bool Swallows(int index)
        {
            return index != None && ((Fired && Index == index) || _swallowed == index);
        }

        /// <summary>Lo olvida todo (cambio de nivel, segundo plano).</summary>
        public void Reset()
        {
            Index = None;
            _fingers = 0;
            _pointer = int.MinValue;
            _heldMs = 0f;
            Fired = false;
            _swallowed = None;
        }
    }

    /// <summary>
    /// Escucha los dedos de una cuia para C9 (bloque B9) en su propio objeto, sin tocar <c>CuiaView</c>: el sistema de eventos de
    /// Unity entrega el touch-down, el touch-up y el arrastre a todos los componentes del objeto. Pasa las posiciones en px de
    /// pantalla a quien la crea (<c>GameScreen</c>), que las pasa a dp.
    /// </summary>
    public sealed class CuiaHoldRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private Action<int, int, Vector2>? _down;
        private Action<int, int, Vector2>? _move;
        private Action<int, int>? _up;

        /// <summary>Índice de la cuia de este objeto.</summary>
        public int Index { get; private set; }

        /// <summary>Pone (o reutiliza) el escuchador en <paramref name="target"/>, para la cuia <paramref name="index"/>.</summary>
        public static CuiaHoldRelay Attach(GameObject target, int index, Action<int, int, Vector2> down, Action<int, int, Vector2> move, Action<int, int> up)
        {
            CuiaHoldRelay relay = target.GetComponent<CuiaHoldRelay>() ?? target.AddComponent<CuiaHoldRelay>();
            relay.Index = index;
            relay._down = down;
            relay._move = move;
            relay._up = up;
            return relay;
        }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            _down?.Invoke(Index, eventData.pointerId, eventData.position);
        }

        /// <inheritdoc />
        public void OnDrag(PointerEventData eventData)
        {
            _move?.Invoke(Index, eventData.pointerId, eventData.position);
        }

        /// <inheritdoc />
        public void OnPointerUp(PointerEventData eventData)
        {
            _up?.Invoke(Index, eventData.pointerId);
        }
    }
}
