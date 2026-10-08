#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Cinta del chorro del vertido en uGUI con el shader <c>AF/UI/Stream</c> (guía visual v2, 6.5): tira de triángulos
    /// sobre una Bézier cuadrática, con el grosor de 0,16 u a 0,10 u y remates redondos en los dos extremos (revisión
    /// visual del 2026-10-07: sin la cuña que asomaba sobre el labio). Nace en el canto interior bajo de la boca del origen
    /// (D-006) y termina en la superficie del destino. Solo se reconstruye mientras el chorro cambia; sin asignaciones por
    /// fotograma.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StreamGraphic : MaskableGraphic
    {
        private const int Segments = 18;
        private const int CapSegments = 6;

        private Vector2 _start;
        private Vector2 _control;
        private Vector2 _end;
        private float _from;
        private float _to;
        private float _widthStart;
        private float _widthEnd;
        private float _unit = 24f;

        /// <summary>Inicio de la curva (labio del origen, coordenadas del contenedor del tablero).</summary>
        public Vector2 CurveStart => _start;

        /// <summary>Fin de la curva (superficie del destino).</summary>
        public Vector2 CurveEnd => _end;

        /// <summary>Tramo visible de la curva.</summary>
        public Vector2 Range => new Vector2(_from, _to);

        /// <summary>Configura el gráfico (sin raycast, material del chorro).</summary>
        public void Setup()
        {
            raycastTarget = false;
            Material? mat = UiMaterials.Stream;
            if (mat != null)
            {
                material = mat;
            }
        }

        /// <summary>Fija el tramo [<paramref name="from"/>, <paramref name="to"/>] de la curva y su grosor (dp).</summary>
        public void SetCurve(Vector2 start, Vector2 control, Vector2 end, float from, float to, float widthStart, float widthEnd, float unitDp)
        {
            _start = start;
            _control = control;
            _end = end;
            _from = from;
            _to = to;
            _widthStart = widthStart;
            _widthEnd = widthEnd;
            _unit = Mathf.Max(1f, unitDp);
            SetVerticesDirty();
        }

        /// <inheritdoc />
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_to <= _from + 0.001f)
            {
                return;
            }

            Color32 c = color;
            float along = 0f;
            Vector2 prev = Bezier(_from);
            for (int i = 0; i <= Segments; i++)
            {
                float t = Mathf.Lerp(_from, _to, i / (float)Segments);
                Vector2 p = Bezier(t);
                Vector2 tangent = SafeTangent(t);
                var normal = new Vector2(-tangent.y, tangent.x);
                along += Vector2.Distance(prev, p) / _unit;
                prev = p;
                float half = HalfWidth(t);
                float line = 0.5f / half; // 1 dp de línea especular, en unidades transversales
                vh.AddVert(p - (normal * half), c, new Vector4(-1f, along, line, 0f));
                vh.AddVert(p + (normal * half), c, new Vector4(1f, along, line, 0f));
                if (i > 0)
                {
                    int b = (i * 2) - 2;
                    vh.AddTriangle(b, b + 1, b + 3);
                    vh.AddTriangle(b + 3, b + 2, b);
                }
            }

            // Remates redondos (semicírculos) en el inicio, tangente al labio, y en la cabeza o la cola.
            AddCap(vh, Bezier(_from), -SafeTangent(_from), HalfWidth(_from), c, 0f);
            AddCap(vh, Bezier(_to), SafeTangent(_to), HalfWidth(_to), c, along);
        }

        // Semicírculo de radio half hacia dir: abanico desde el centro (uv.x = 0) hasta el arco (uv.x = 1), así el borde
        // tiene el mismo suavizado que los lados de la cinta.
        private static void AddCap(VertexHelper vh, Vector2 center, Vector2 dir, float half, Color32 c, float along)
        {
            var normal = new Vector2(-dir.y, dir.x);
            float line = 0.5f / half;
            int baseIndex = vh.currentVertCount;
            vh.AddVert(center, c, new Vector4(0f, along, line, 0f));
            for (int k = 0; k <= CapSegments; k++)
            {
                float a = Mathf.PI * k / CapSegments; // de +normal a −normal pasando por dir
                Vector2 p = center + (((normal * Mathf.Cos(a)) + (dir * Mathf.Sin(a))) * half);
                vh.AddVert(p, c, new Vector4(1f, along, line, 0f));
                if (k > 0)
                {
                    vh.AddTriangle(baseIndex, baseIndex + k, baseIndex + k + 1);
                }
            }
        }

        private float HalfWidth(float t)
        {
            return Mathf.Max(0.75f, Mathf.Lerp(_widthStart, _widthEnd, t) * 0.5f);
        }

        private Vector2 SafeTangent(float t)
        {
            Vector2 tangent = Tangent(t);
            if (tangent.sqrMagnitude < 1e-6f)
            {
                tangent = Vector2.down;
            }

            return tangent.normalized;
        }

        private Vector2 Bezier(float t)
        {
            float u = 1f - t;
            return (u * u * _start) + (2f * u * t * _control) + (t * t * _end);
        }

        private Vector2 Tangent(float t)
        {
            return (2f * (1f - t) * (_control - _start)) + (2f * t * (_end - _control));
        }
    }
}
