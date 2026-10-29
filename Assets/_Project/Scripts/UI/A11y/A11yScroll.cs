#nullable enable
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// TalkBack en las listas que se desplazan (Q-B9-17 de B9; D-033, punto 12; 02 §12, fila «Lector de pantalla»): Ajustes y el
    /// Mercado, cuyas últimas filas quedan bajo el corte del visor al 130 % o en pantallas bajas. <see cref="A11yScanner"/> publica
    /// todas las filas de una lista que se desplaza (<see cref="A11yItem.Scroll"/>), también las recortadas; el anfitrión
    /// (<see cref="UnityScreenReaderHost"/>) les da la acción de desplazar del lector (<see cref="AccessibilityNode.scrolled"/>:
    /// <see cref="Page"/>) y, cuando el foco llega a una recortada, desplaza el visor para enseñarla (<see cref="Reveal"/>). Así el
    /// lector recorre la lista entera. Solo con el lector encendido, al enfocar o al desplazar: nunca por fotograma.
    /// </summary>
    public static class A11yScroll
    {
        /// <summary>Lo que avanza una página de la acción de desplazar, en fracción del alto del visor [P].</summary>
        public const float PageFraction = 0.8f;

        /// <summary>Alto (px) del marco que se publica para un elemento recortado del todo: una franja en el borde del visor [P].</summary>
        public const float EdgeStripPx = 1f;

        private static readonly Vector3[] Corners = new Vector3[4];

        /// <summary>
        /// <paramref name="list"/> se desplaza ahora (vertical: Ajustes y el Mercado), con contenido y visor. Una lista que cabe entera
        /// no se desplaza y sus filas se publican como las de cualquier pantalla.
        /// </summary>
        public static bool Scrolls(ScrollRect? list)
        {
            return list != null && list.isActiveAndEnabled && list.vertical && list.content != null && ViewportOf(list) != null;
        }

        /// <summary>El visor de <paramref name="list"/> (el suyo o, sin él, la propia lista).</summary>
        public static RectTransform? ViewportOf(ScrollRect list)
        {
            return list.viewport != null ? list.viewport : list.transform as RectTransform;
        }

        /// <summary>
        /// Desplaza <paramref name="list"/> lo justo para que <paramref name="rect"/> se vea entero (si es más alto que el visor, su
        /// parte de arriba). Devuelve si se movió (false si ya se veía o la lista no se desplaza).
        /// </summary>
        public static bool Reveal(ScrollRect list, RectTransform rect)
        {
            if (!Scrolls(list) || rect == null || !rect.gameObject.activeInHierarchy || !rect.IsChildOf(list.content))
            {
                return false;
            }

            RectTransform view = ViewportOf(list)!;
            Bounds(rect, view, out float top, out float bottom);
            Rect viewRect = view.rect;
            float delta = 0f; // + baja la lista (enseña lo de abajo); − la sube
            if (top > viewRect.yMax + 0.5f || top - bottom > viewRect.height)
            {
                delta = viewRect.yMax - top; // arriba del visor, o más alto que él: su borde de arriba al de arriba del visor
            }
            else if (bottom < viewRect.yMin - 0.5f)
            {
                delta = viewRect.yMin - bottom;
            }

            return delta != 0f && MoveBy(list, delta);
        }

        /// <summary>
        /// La acción de desplazar del lector sobre <paramref name="list"/>: una página (<see cref="PageFraction"/> del visor) hacia
        /// <paramref name="direction"/> (Forward y Down enseñan lo de abajo; Backward y Up, lo de arriba; Left, Right y Unknown no
        /// hacen nada en una lista vertical). Devuelve si se movió (false en el extremo: TalkBack lo dice con su sonido de tope).
        /// </summary>
        public static bool Page(ScrollRect list, AccessibilityScrollDirection direction)
        {
            if (!Scrolls(list))
            {
                return false;
            }

            float step = ViewportOf(list)!.rect.height * PageFraction;
            switch (direction)
            {
                case AccessibilityScrollDirection.Forward:
                case AccessibilityScrollDirection.Down:
                    return MoveBy(list, step);
                case AccessibilityScrollDirection.Backward:
                case AccessibilityScrollDirection.Up:
                    return MoveBy(list, -step);
                default:
                    return false;
            }
        }

        /// <summary><paramref name="rect"/> se ve entero dentro del visor de <paramref name="list"/>.</summary>
        public static bool FullyShown(ScrollRect list, RectTransform rect)
        {
            RectTransform? view = ViewportOf(list);
            if (view == null || rect == null || !rect.gameObject.activeInHierarchy)
            {
                return false;
            }

            Bounds(rect, view, out float top, out float bottom);
            return top <= view.rect.yMax + 0.5f && bottom >= view.rect.yMin - 0.5f;
        }

        /// <summary>
        /// Se ve algo de <paramref name="rect"/> dentro del visor de <paramref name="list"/> (aunque sea en parte): tras desplazar, el
        /// foco se queda en una fila así (<see cref="UnityScreenReaderHost.RefocusAfterMove"/>).
        /// </summary>
        public static bool PartlyShown(ScrollRect list, RectTransform rect)
        {
            RectTransform? view = ViewportOf(list);
            if (view == null || rect == null || !rect.gameObject.activeInHierarchy)
            {
                return false;
            }

            Bounds(rect, view, out float top, out float bottom);
            return top > view.rect.yMin + 0.5f && bottom < view.rect.yMax - 0.5f;
        }

        /// <summary>
        /// El marco que se publica (píxeles, origen arriba a la izquierda, como <see cref="UnityScreenReaderHost.ScreenFrame"/>) para
        /// <paramref name="rect"/> dentro de <paramref name="list"/>: lo que se ve de él dentro del visor (el recuadro del foco no pisa
        /// los fundidos ni el marco de madera) o, si está recortado del todo, una franja de <see cref="EdgeStripPx"/> en el borde del
        /// visor por donde queda, para que el lector lo tenga por visible y pueda llegar a él: al enfocarlo se desplaza
        /// (<see cref="Reveal"/>) y el marco pasa a ser el de verdad [NV en el teléfono].
        /// </summary>
        public static Rect Frame(ScrollRect list, RectTransform rect)
        {
            Rect frame = UnityScreenReaderHost.ScreenFrame(rect);
            RectTransform? view = ViewportOf(list);
            if (view == null || frame.width <= 0f || frame.height <= 0f)
            {
                return frame;
            }

            Rect viewFrame = UnityScreenReaderHost.ScreenFrame(view);
            float xMin = Mathf.Max(frame.xMin, viewFrame.xMin);
            float xMax = Mathf.Min(frame.xMax, viewFrame.xMax);
            if (xMax <= xMin)
            {
                xMin = viewFrame.xMin;
                xMax = viewFrame.xMax;
            }

            if (frame.yMin >= viewFrame.yMax)
            {
                return Rect.MinMaxRect(xMin, viewFrame.yMax - EdgeStripPx, xMax, viewFrame.yMax); // debajo del corte
            }

            if (frame.yMax <= viewFrame.yMin)
            {
                return Rect.MinMaxRect(xMin, viewFrame.yMin, xMax, viewFrame.yMin + EdgeStripPx); // encima del visor
            }

            return Rect.MinMaxRect(xMin, Mathf.Max(frame.yMin, viewFrame.yMin), xMax, Mathf.Min(frame.yMax, viewFrame.yMax));
        }

        /// <summary>
        /// <paramref name="rect"/> (de marco <paramref name="frame"/> en pantalla) queda recortado del todo por un visor activo
        /// (<see cref="RectMask2D"/>) suyo o de encima: no se ve ni se puede tocar. Con <paramref name="stopAt"/> (el visor de una lista
        /// que se desplaza: sus filas se publican igual), solo cuentan los visores de debajo de él. Lo usan el lector y las sondas
        /// <c>AF_LAYOUT</c> de Ajustes y del Mercado.
        /// </summary>
        public static bool ClippedOut(RectTransform rect, Rect frame, Transform? stopAt = null)
        {
            for (Transform? t = rect; t != null && t != stopAt; t = t.parent)
            {
                RectMask2D? mask = t.GetComponent<RectMask2D>();
                if (mask != null && mask.isActiveAndEnabled && !UnityScreenReaderHost.ScreenFrame(mask.rectTransform).Overlaps(frame))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Como <see cref="ClippedOut(RectTransform, Rect, Transform)"/>, con el marco de ahora de <paramref name="rect"/>.</summary>
        public static bool ClippedOut(RectTransform rect)
        {
            return ClippedOut(rect, UnityScreenReaderHost.ScreenFrame(rect));
        }

        // Desplaza el contenido delta unidades del visor (+ enseña lo de abajo), sin salirse de los topes; devuelve si se movió.
        private static bool MoveBy(ScrollRect list, float delta)
        {
            RectTransform view = ViewportOf(list)!;
            RectTransform content = list.content;
            Bounds(content, view, out float contentTop, out float contentBottom);
            float range = (contentTop - contentBottom) - view.rect.height;
            if (range <= 0.5f)
            {
                return false;
            }

            float offset = contentTop - view.rect.yMax; // lo que el contenido sube por encima del visor (0 arriba del todo)
            float target = Mathf.Clamp(offset + delta, 0f, range);
            if (Mathf.Abs(target - offset) < 0.5f)
            {
                return false;
            }

            list.StopMovement();
            list.verticalNormalizedPosition = 1f - (target / range);
            return true;
        }

        // Bordes de arriba y de abajo de rect en las coordenadas locales de view.
        private static void Bounds(RectTransform rect, RectTransform view, out float top, out float bottom)
        {
            rect.GetWorldCorners(Corners);
            top = float.MinValue;
            bottom = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                float y = view.InverseTransformPoint(Corners[i]).y;
                top = Mathf.Max(top, y);
                bottom = Mathf.Min(bottom, y);
            }
        }
    }
}
