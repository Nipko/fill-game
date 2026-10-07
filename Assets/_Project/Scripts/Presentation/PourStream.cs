#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Chorro del vertido (02 §4.1; 07 §5.2): Bézier cuadrática del labio a la superficie del destino, dibujada con
    /// segmentos de uGUI reutilizados (sin asignaciones por fotograma), grosor de 0,16 u a 0,10 u y el color del tinte.
    /// </summary>
    public sealed class PourStream
    {
        private const int Segments = 14;

        private readonly Image[] _segments = new Image[Segments];

        /// <summary>Crea los segmentos dentro de <paramref name="layer"/> (contenedor del tablero, origen arriba a la izquierda).</summary>
        public PourStream(RectTransform layer, int id)
        {
            for (int i = 0; i < Segments; i++)
            {
                var go = new GameObject("Stream" + id + "_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(layer, false);
                Image img = go.GetComponent<Image>();
                img.raycastTarget = false;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                img.enabled = false;
                _segments[i] = img;
            }
        }

        /// <summary>Lo usa un vertido en curso.</summary>
        public bool InUse { get; set; }

        /// <summary>
        /// Dibuja el tramo [<paramref name="from"/>, <paramref name="to"/>] (parámetros 0..1) de la curva
        /// <paramref name="start"/> → <paramref name="end"/> con control <paramref name="control"/>.
        /// </summary>
        public void Draw(Vector2 start, Vector2 control, Vector2 end, float from, float to, Color color, float widthStart, float widthEnd)
        {
            from = Mathf.Clamp01(from);
            to = Mathf.Clamp01(to);
            if (to <= from + 0.001f)
            {
                Hide();
                return;
            }

            for (int i = 0; i < Segments; i++)
            {
                float a = Mathf.Lerp(from, to, i / (float)Segments);
                float b = Mathf.Lerp(from, to, (i + 1) / (float)Segments);
                Vector2 pa = Bezier(start, control, end, a);
                Vector2 pb = Bezier(start, control, end, b);
                Vector2 d = pb - pa;
                float len = d.magnitude;
                Image img = _segments[i];
                if (len < 0.01f)
                {
                    img.enabled = false;
                    continue;
                }

                img.enabled = true;
                img.color = color;
                RectTransform rt = img.rectTransform;
                rt.anchoredPosition = (pa + pb) * 0.5f;
                rt.sizeDelta = new Vector2(Mathf.Lerp(widthStart, widthEnd, (a + b) * 0.5f), len + 0.6f);
                rt.localRotation = Quaternion.Euler(0f, 0f, (Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg) - 90f);
            }
        }

        /// <summary>Oculta el chorro.</summary>
        public void Hide()
        {
            for (int i = 0; i < Segments; i++)
            {
                if (_segments[i].enabled)
                {
                    _segments[i].enabled = false;
                }
            }
        }

        private static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            return (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
        }
    }
}
