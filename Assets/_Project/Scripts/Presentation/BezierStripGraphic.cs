#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Tira sobre una Bézier cúbica con contorno de tinta (guion de la intro §11): la cola de los miquitos (tira de 8
    /// segmentos), la rama de la tropa (Bézier de 4-6 dp con corteza) y la liana del modelo <c>cuelga</c>. Grosor y color
    /// interpolados de la base a la punta, con un tramo final de otro color (la punta de la cola). Con <see cref="SetWave"/>,
    /// la tira se aparta de la curva a lo largo de su normal: desde su base va hacia el eje y luego ondula (la trenza de las
    /// colas de la pareja de <c>colas_entrelazadas</c>: dos tiras con fases opuestas se cruzan en cada media onda).
    /// Coordenadas locales de su RectTransform; solo se reconstruye cuando cambian sus parámetros (la opacidad va por
    /// <c>CanvasRenderer.SetAlpha</c>). Sin asignaciones.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BezierStripGraphic : MaskableGraphic
    {
        /// <summary>Segmentos como mucho.</summary>
        public const int MaxSegments = 32;

        private Vector2 _p0;
        private Vector2 _p1;
        private Vector2 _p2;
        private Vector2 _p3;
        private float _w0 = 4f;
        private float _w1 = 4f;
        private Color _body = Color.white;
        private Color _tip = Color.white;
        private float _tipFrom = 1f;
        private Color _outline = new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 0.8f);
        private float _outlineDp = 1f;
        private int _segments = 8;
        private float _waveFrom;
        private float _wave;
        private float _waveGather;
        private float _waveHalves;
        private float _wavePhase;

        /// <summary>Configura el gráfico (sin raycast).</summary>
        public void Setup(int segments)
        {
            raycastTarget = false;
            _segments = Mathf.Clamp(segments, 1, MaxSegments);
        }

        /// <summary>Segmentos de la tira (1 a <see cref="MaxSegments"/>).</summary>
        public int Segments => _segments;

        /// <summary>Cambia los segmentos (la trenza necesita más que una cola en arco).</summary>
        public void SetSegments(int segments)
        {
            segments = Mathf.Clamp(segments, 1, MaxSegments);
            if (segments != _segments)
            {
                _segments = segments;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Ondula la tira a lo largo de su normal (dp): en la base se aparta <paramref name="fromDp"/>, se acerca hasta
        /// <paramref name="ampDp"/> a lo largo de la fracción <paramref name="gather"/> de la curva y desde ahí oscila con
        /// <paramref name="halfWaves"/> medias ondas: desplazamiento = amplitud · cos(π · medias ondas · s + fase), con s de 0 a 1
        /// en el tramo ondulado. Dos tiras sobre la misma curva con fases 0 y π se cruzan en cada media onda (una trenza).
        /// <paramref name="halfWaves"/> = 0 y amplitudes 0: sin onda.
        /// </summary>
        public void SetWave(float fromDp, float ampDp, float gather, float halfWaves, float phase)
        {
            gather = Mathf.Clamp(gather, 0f, 0.95f);
            if (Mathf.Approximately(fromDp, _waveFrom) && Mathf.Approximately(ampDp, _wave) && Mathf.Approximately(gather, _waveGather)
                && Mathf.Approximately(halfWaves, _waveHalves) && Mathf.Approximately(phase, _wavePhase))
            {
                return;
            }

            _waveFrom = fromDp;
            _wave = ampDp;
            _waveGather = gather;
            _waveHalves = halfWaves;
            _wavePhase = phase;
            SetVerticesDirty();
        }

        /// <summary>La tira ondula (<see cref="SetWave"/>).</summary>
        public bool Waving => _waveHalves > 0f || _waveFrom != 0f || _wave != 0f;

        /// <summary>Desplazamiento de la onda a lo largo de la normal en <paramref name="t"/> (dp; tests).</summary>
        public float WaveOffset(float t)
        {
            if (!Waving)
            {
                return 0f;
            }

            float g = _waveGather;
            float k = g > 0f ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / g)) : 1f;
            float amp = Mathf.Lerp(_waveFrom, _wave, k);
            float s = t <= g ? 0f : (t - g) / Mathf.Max(0.0001f, 1f - g);
            return amp * Mathf.Cos((Mathf.PI * _waveHalves * s) + _wavePhase);
        }

        /// <summary>Punto de la tira en <paramref name="t"/> (0..1): la curva más la onda, en coordenadas locales.</summary>
        public Vector2 PointAt(float t)
        {
            Vector2 p = Evaluate(t);
            if (!Waving)
            {
                return p;
            }

            Vector2 d = Tangent(t);
            return p + (new Vector2(-d.y, d.x) * WaveOffset(t));
        }

        /// <summary>Puntos de control (locales) y grosor en la base y en la punta (dp).</summary>
        public void SetCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float widthStart, float widthEnd)
        {
            if (p0 == _p0 && p1 == _p1 && p2 == _p2 && p3 == _p3 && Mathf.Approximately(widthStart, _w0) && Mathf.Approximately(widthEnd, _w1))
            {
                return;
            }

            _p0 = p0;
            _p1 = p1;
            _p2 = p2;
            _p3 = p3;
            _w0 = widthStart;
            _w1 = widthEnd;
            SetVerticesDirty();
        }

        /// <summary>Colores: el cuerpo, la punta desde la fracción <paramref name="tipFrom"/> y el contorno (grosor en dp; 0 sin contorno).</summary>
        public void SetColors(Color body, Color tip, float tipFrom, Color outline, float outlineDp)
        {
            if (body == _body && tip == _tip && Mathf.Approximately(tipFrom, _tipFrom) && outline == _outline && Mathf.Approximately(outlineDp, _outlineDp))
            {
                return;
            }

            _body = body;
            _tip = tip;
            _tipFrom = Mathf.Clamp01(tipFrom);
            _outline = outline;
            _outlineDp = Mathf.Max(0f, outlineDp);
            SetVerticesDirty();
        }

        /// <summary>Caja de la tira en sus coordenadas locales (con el contorno), muestreando la curva como la malla.</summary>
        public Rect LocalBounds()
        {
            float pad = (Mathf.Max(_w0, _w1) * 0.5f) + _outlineDp;
            float x0 = float.MaxValue;
            float x1 = float.MinValue;
            float y0 = float.MaxValue;
            float y1 = float.MinValue;
            for (int i = 0; i <= _segments; i++)
            {
                Vector2 p = PointAt(i / (float)_segments);
                x0 = Mathf.Min(x0, p.x);
                x1 = Mathf.Max(x1, p.x);
                y0 = Mathf.Min(y0, p.y);
                y1 = Mathf.Max(y1, p.y);
            }

            return Rect.MinMaxRect(x0 - pad, y0 - pad, x1 + pad, y1 + pad);
        }

        /// <summary>Punto de la curva en <paramref name="t"/> (0..1), en coordenadas locales.</summary>
        public Vector2 Evaluate(float t)
        {
            float u = 1f - t;
            return (u * u * u * _p0) + (3f * u * u * t * _p1) + (3f * u * t * t * _p2) + (t * t * t * _p3);
        }

        /// <inheritdoc />
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_w0 <= 0.01f && _w1 <= 0.01f)
            {
                return;
            }

            if (_outlineDp > 0.01f && _outline.a > 0.001f)
            {
                Strip(vh, _outlineDp, true);
            }

            Strip(vh, 0f, false);
        }

        private void Strip(VertexHelper vh, float grow, bool outline)
        {
            int n = _segments;
            int start = vh.currentVertCount;
            bool waving = Waving;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                Vector2 p = PointAt(t);
                Vector2 d = waving ? WaveTangent(i, n) : Tangent(t);
                if (outline && (i == 0 || i == n))
                {
                    p += d * (i == 0 ? -grow : grow); // el contorno asoma también por los extremos
                }

                var normal = new Vector2(-d.y, d.x);
                float half = (Mathf.Lerp(_w0, _w1, t) * 0.5f) + grow;
                Color c;
                if (outline)
                {
                    c = _outline;
                }
                else if (t >= _tipFrom && _tipFrom < 1f)
                {
                    c = Color.Lerp(_body, _tip, Mathf.Clamp01((t - _tipFrom) / Mathf.Max(0.05f, (1f - _tipFrom) * 0.5f)));
                }
                else
                {
                    c = _body;
                }

                c.a *= color.a;
                vh.AddVert(p - (normal * half), c, Vector4.zero);
                vh.AddVert(p + (normal * half), c, Vector4.zero);
                if (i > 0)
                {
                    int b = start + ((i - 1) * 2);
                    vh.AddTriangle(b, b + 1, b + 3);
                    vh.AddTriangle(b + 3, b + 2, b);
                }
            }
        }

        // Dirección de la tira ondulada en el vértice i (diferencia entre sus vecinos).
        private Vector2 WaveTangent(int i, int n)
        {
            Vector2 d = PointAt(Mathf.Min(n, i + 1) / (float)n) - PointAt(Mathf.Max(0, i - 1) / (float)n);
            return d.sqrMagnitude < 1e-8f ? Tangent(i / (float)n) : d.normalized;
        }

        private Vector2 Tangent(float t)
        {
            float u = 1f - t;
            Vector2 d = (3f * u * u * (_p1 - _p0)) + (6f * u * t * (_p2 - _p1)) + (3f * t * t * (_p3 - _p2));
            if (d.sqrMagnitude < 1e-6f)
            {
                d = _p3 - _p0;
                if (d.sqrMagnitude < 1e-6f)
                {
                    d = Vector2.right;
                }
            }

            return d.normalized;
        }
    }
}
