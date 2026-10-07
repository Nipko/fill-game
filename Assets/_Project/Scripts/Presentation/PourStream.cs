#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Chorro del vertido (02 §4.1; 07 §5.2; guía visual v2, 6.5): Bézier cuadrática del labio a la superficie del destino,
    /// dibujada como una cinta de <see cref="StreamGraphic"/> reutilizada (sin asignaciones por fotograma), grosor de 0,16 u
    /// a 0,10 u con remates redondos (revisión visual del 2026-10-07), núcleo del tinte con +10 % de L y línea especular
    /// central de 1 dp. Una dosis por vertido (D-001).
    /// </summary>
    public sealed class PourStream
    {
        private readonly StreamGraphic _graphic;
        private Color _lastTint = new Color(-1f, -1f, -1f, -1f);
        private Color _core;

        /// <summary>Crea la cinta dentro de <paramref name="layer"/> (contenedor del tablero, origen arriba a la izquierda).</summary>
        public PourStream(RectTransform layer, int id)
        {
            var go = new GameObject("Stream" + id, typeof(RectTransform), typeof(CanvasRenderer), typeof(StreamGraphic));
            go.transform.SetParent(layer, false);
            _graphic = go.GetComponent<StreamGraphic>();
            _graphic.Setup();
            RectTransform rt = _graphic.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            _graphic.enabled = false;
        }

        /// <summary>Lo usa un vertido en curso.</summary>
        public bool InUse { get; set; }

        /// <summary>Gráfico de la cinta (diagnóstico y capturas).</summary>
        public StreamGraphic Graphic => _graphic;

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

            if (color != _lastTint)
            {
                _lastTint = color;
                _core = ColorMath.ScaleOkLightness(color, 1.10f); // núcleo del tinte con +10 % de L (guía 6.5)
                _core.a = color.a;
            }

            if (!_graphic.enabled)
            {
                _graphic.enabled = true;
            }

            if (_graphic.color != _core)
            {
                _graphic.color = _core;
            }

            float unit = Mathf.Max(widthStart, widthEnd) / BoardAnimator.StreamWidthStartU; // grosor del chorro en el labio, en u
            _graphic.SetCurve(start, control, end, from, to, widthStart, widthEnd, unit);
        }

        /// <summary>Oculta el chorro.</summary>
        public void Hide()
        {
            if (_graphic.enabled)
            {
                _graphic.enabled = false;
            }
        }
    }
}
