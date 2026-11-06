#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// La columna de la interfaz dentro de un lienzo (añadido de D-059 del 2026-11-06; <see cref="ScreenFrame"/>): cuelga del
    /// lienzo y todo lo de la pantalla cuelga de ella, maquetado desde su esquina de arriba a la izquierda como antes desde la del
    /// lienzo. Si la columna es el lienzo entero (los teléfonos), se estira a él sin márgenes: el mismo rect de siempre. Si no,
    /// mide el ancho de la columna y se centra; los fondos que deben llegar al borde (<see cref="Bleed"/>: el agua del río, los
    /// velos de los modales, el petróleo del arranque) se salen de ella hasta los bordes del lienzo, así que los lados nunca
    /// quedan vacíos y lo que hay debajo no se toca a través de un velo.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class ScreenColumn : MonoBehaviour
    {
        /// <summary>Nombre del objeto de la columna (los tests buscan bajo él: <c>Column/Panel</c>).</summary>
        public const string ObjectName = "Column";

        private readonly List<RectTransform> _bleeds = new List<RectTransform>();
        private ScreenFrame _frame;
        private bool _applied;

        private System.Action? _polled;

        /// <summary>
        /// Se llama en cada fotograma con el lienzo activo (después de los Update; lo usa <see cref="OverlayCanvas"/>). Sin nadie, el
        /// componente se apaga y no cuesta nada.
        /// </summary>
        public System.Action? Polled
        {
            get => _polled;
            set
            {
                _polled = value;
                enabled = value != null;
            }
        }

        /// <summary>La geometría con la que se colocó la columna por última vez.</summary>
        public ScreenFrame Frame => _frame;

        /// <summary>El rect de la columna.</summary>
        public RectTransform Rect => (RectTransform)transform;

        /// <summary>Crea la columna bajo el lienzo <paramref name="canvasRoot"/>, de momento estirada a él.</summary>
        public static ScreenColumn Create(RectTransform canvasRoot)
        {
            var go = new GameObject(ObjectName, typeof(RectTransform));
            go.transform.SetParent(canvasRoot, false);
            UiBuilder.Stretch((RectTransform)go.transform);
            ScreenColumn column = go.AddComponent<ScreenColumn>();
            column.enabled = false; // sin Polled, sin LateUpdate
            return column;
        }

        /// <summary>Coloca la columna y los fondos que llegan al borde con <paramref name="frame"/>.</summary>
        public void Apply(in ScreenFrame frame)
        {
            _frame = frame;
            _applied = true;
            RectTransform rt = Rect;
            if (frame.IsFullWidth)
            {
                UiBuilder.Stretch(rt); // el rect de siempre, también cuando el lienzo real no mide lo simulado (tests)
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.offsetMin = new Vector2(frame.ColumnX, 0f);
                rt.offsetMax = new Vector2(frame.ColumnX + frame.ColumnWidth, 0f);
            }

            for (int i = _bleeds.Count - 1; i >= 0; i--)
            {
                RectTransform bleed = _bleeds[i];
                if (bleed == null)
                {
                    _bleeds.RemoveAt(i);
                    continue;
                }

                PlaceBleed(bleed);
            }
        }

        private void LateUpdate()
        {
            _polled?.Invoke();
        }

        /// <summary>
        /// Estira <paramref name="rt"/> a su padre y, si su padre es la columna (o algo estirado a ella), lo saca a los lados hasta
        /// los bordes del lienzo, ahora y cada vez que la columna se recoloque. Fuera de una columna es <see cref="UiBuilder.Stretch"/>.
        /// </summary>
        public static void Bleed(RectTransform rt)
        {
            UiBuilder.Stretch(rt);
            ScreenColumn? column = ColumnOf(rt.parent);
            if (column == null)
            {
                return;
            }

            if (!column._bleeds.Contains(rt))
            {
                column._bleeds.Add(rt);
            }

            if (column._applied)
            {
                column.PlaceBleed(rt);
            }
        }

        /// <summary>Lo que <paramref name="rt"/> se sale de la columna por la izquierda y por la derecha (dp; tests).</summary>
        public static Vector2 BleedOf(RectTransform rt)
        {
            return new Vector2(-rt.offsetMin.x, rt.offsetMax.x);
        }

        private void PlaceBleed(RectTransform rt)
        {
            if (_frame.IsFullWidth)
            {
                rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
                rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
                return;
            }

            rt.offsetMin = new Vector2(-_frame.ColumnX, rt.offsetMin.y);
            rt.offsetMax = new Vector2(_frame.RightBand, rt.offsetMax.y);
        }

        // La columna de la que cuelga parent, si toda la cadena hasta ella está estirada sin márgenes de lado (el rect de parent es
        // el de la columna); si no, null.
        private static ScreenColumn? ColumnOf(Transform? parent)
        {
            Transform? t = parent;
            while (t != null)
            {
                ScreenColumn column = t.GetComponent<ScreenColumn>();
                if (column != null)
                {
                    return column;
                }

                var rt = t as RectTransform;
                if (rt == null || rt.anchorMin.x != 0f || rt.anchorMax.x != 1f || rt.offsetMin.x != 0f || rt.offsetMax.x != 0f)
                {
                    return null;
                }

                t = t.parent;
            }

            return null;
        }
    }
}
