#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Tira de agua de «Llenar y teñir» (guion §11): la lámina del desborde sobre el canto del marco, las tiras de los
    /// costados del marco y las de las repisas. Horizontal o vertical, con el borde de fuera ondulado (1 dp de amplitud y
    /// longitud de onda de 0,5 u), degradado del agua del mundo (más clara en la superficie), una línea especular junto a ese
    /// borde, un labio claro opcional en el borde plano (la lámina) y destellos cortos que viajan a lo largo de la tira
    /// (<see cref="SetFlow"/>, 300-400 dp/s). Crece desde un origen hacia uno o los dos sentidos. Coordenadas del contenedor del
    /// tablero (origen arriba a la izquierda, y hacia arriba, como <see cref="StreamGraphic"/>). Como mucho
    /// <see cref="MaxQuads"/> cuadriláteros; solo se reconstruye cuando cambian sus parámetros (la opacidad va por
    /// <c>CanvasRenderer.SetAlpha</c>, sin reconstruir).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WaterStripGraphic : MaskableGraphic
    {
        /// <summary>Cuadriláteros como mucho (cuerpo, líneas y destellos).</summary>
        public const int MaxQuads = 64;

        /// <summary>Amplitud del borde ondulado (dp).</summary>
        public const float WaveAmplitudeDp = 1f;

        /// <summary>Longitud de onda del borde (u).</summary>
        public const float WaveLengthU = 0.5f;

        private const int MaxBodyQuads = 36;
        private const int MaxLineQuads = 12;
        private const int MaxLipQuads = 8;
        private const int MaxGlints = 8;
        private const float LineInsetDp = 1.2f;
        private const float LineDp = 1f;
        private const float GlintSpacingDp = 38f;
        private const float GlintLengthDp = 7f;

        private Vector2 _origin;
        private float _back;
        private float _forward;
        private float _thickness = 5f;
        private float _unit = 24f;
        private bool _vertical;
        private bool _flip;
        private bool _lip;
        private float _flow;
        private Color _base = new Color(0.69f, 0.54f, 0.35f, 0.85f);
        private Color _surface = new Color(0.85f, 0.72f, 0.52f, 0.9f);
        private Color _line = new Color(1f, 0.96f, 0.82f, 0.7f);

        /// <summary>Configura el gráfico (sin raycast).</summary>
        public void Setup()
        {
            raycastTarget = false;
        }

        /// <summary>Largo total dibujado (dp).</summary>
        public float Length => _back + _forward;

        /// <summary>
        /// Fija la tira: <paramref name="origin"/> es el punto del borde plano del que crece (coordenadas del contenedor),
        /// <paramref name="back"/> y <paramref name="forward"/> lo que mide hacia cada sentido a lo largo de su eje (x en
        /// horizontal, −y en vertical: hacia abajo), <paramref name="thickness"/> su ancho. En horizontal, el borde ondulado es
        /// el de arriba; en vertical, el de la derecha (<paramref name="flip"/> lo pasa al otro lado).
        /// </summary>
        public void SetStrip(Vector2 origin, float back, float forward, float thickness, float unitDp, bool vertical, bool flip)
        {
            back = Mathf.Max(0f, back);
            forward = Mathf.Max(0f, forward);
            thickness = Mathf.Max(0.5f, thickness);
            unitDp = Mathf.Max(1f, unitDp);
            if (origin == _origin && Mathf.Approximately(back, _back) && Mathf.Approximately(forward, _forward)
                && Mathf.Approximately(thickness, _thickness) && Mathf.Approximately(unitDp, _unit) && vertical == _vertical && flip == _flip)
            {
                return;
            }

            _origin = origin;
            _back = back;
            _forward = forward;
            _thickness = thickness;
            _unit = unitDp;
            _vertical = vertical;
            _flip = flip;
            SetVerticesDirty();
        }

        /// <summary>
        /// Colores: el agua en el borde plano, la superficie (borde ondulado, más clara), la línea especular y si lleva el labio
        /// claro en el borde plano (la lámina que rebosa por el canto interior del marco).
        /// </summary>
        public void SetLook(Color baseColor, Color surface, Color line, bool lip)
        {
            if (baseColor == _base && surface == _surface && line == _line && lip == _lip)
            {
                return;
            }

            _base = baseColor;
            _surface = surface;
            _line = line;
            _lip = lip;
            SetVerticesDirty();
        }

        /// <summary>Desplazamiento de los destellos a lo largo de la tira (dp): el agua corre.</summary>
        public void SetFlow(float flowDp)
        {
            if (!Mathf.Approximately(flowDp, _flow))
            {
                _flow = flowDp;
                SetVerticesDirty();
            }
        }

        /// <inheritdoc />
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float length = _back + _forward;
            if (length < 0.05f)
            {
                return;
            }

            float lambda = WaveLengthU * _unit;
            int body = Mathf.Clamp(Mathf.CeilToInt(length / (lambda * 0.25f)), 1, MaxBodyQuads);
            float a = color.a;
            Color32 cBase = Alpha(_base, a);
            Color32 cSurface = Alpha(_surface, a);
            for (int i = 0; i < body; i++)
            {
                float a0 = -_back + (length * i / body);
                float a1 = -_back + (length * (i + 1) / body);
                int b = vh.currentVertCount;
                vh.AddVert(Point(a0, 0f), cBase, Vector4.zero);
                vh.AddVert(Point(a0, Edge(a0)), cSurface, Vector4.zero);
                vh.AddVert(Point(a1, Edge(a1)), cSurface, Vector4.zero);
                vh.AddVert(Point(a1, 0f), cBase, Vector4.zero);
                vh.AddTriangle(b, b + 1, b + 2);
                vh.AddTriangle(b + 2, b + 3, b);
            }

            if (_thickness < 3f)
            {
                return;
            }

            // Línea especular junto al borde ondulado, con las puntas transparentes.
            int lines = Mathf.Clamp(body / 3, 1, MaxLineQuads);
            Color32 lc = Alpha(_line, a);
            Color32 tip = Alpha(new Color(_line.r, _line.g, _line.b, 0f), a);
            for (int i = 0; i < lines; i++)
            {
                float a0 = -_back + (length * i / lines);
                float a1 = -_back + (length * (i + 1) / lines);
                float o0 = Edge(a0) - LineInsetDp;
                float o1 = Edge(a1) - LineInsetDp;
                Quad(vh, a0, a1, o0 - LineDp, o0, o1 - LineDp, o1, i == 0 ? tip : lc, i == lines - 1 ? tip : lc);
            }

            // Labio claro en el borde plano (la lámina rebosa por el canto interior del marco).
            if (_lip)
            {
                int lips = Mathf.Clamp(body / 4, 1, MaxLipQuads);
                Color32 lipColor = Alpha(new Color(_line.r, _line.g, _line.b, _line.a * 0.8f), a);
                for (int i = 0; i < lips; i++)
                {
                    float a0 = -_back + (length * i / lips);
                    float a1 = -_back + (length * (i + 1) / lips);
                    Quad(vh, a0, a1, 0f, 1.2f, 0f, 1.2f, i == 0 ? tip : lipColor, i == lips - 1 ? tip : lipColor);
                }
            }

            // Destellos cortos que viajan con el agua (a media altura de la tira).
            Color32 glint = Alpha(new Color(1f, 0.98f, 0.9f, 0.85f), a);
            Color32 clear = Alpha(new Color(1f, 0.98f, 0.9f, 0f), a);
            float start = Mathf.Repeat(_flow, GlintSpacingDp);
            int glints = 0;
            for (float along = start - _back; along + GlintLengthDp < _forward && glints < MaxGlints; along += GlintSpacingDp)
            {
                if (along < -_back + 1f)
                {
                    continue;
                }

                float depth = _thickness * (glints % 2 == 0 ? 0.55f : 0.35f);
                float mid = along + (GlintLengthDp * 0.5f);
                int b = vh.currentVertCount;
                vh.AddVert(Point(along, depth), clear, Vector4.zero);
                vh.AddVert(Point(mid, depth + 0.6f), glint, Vector4.zero);
                vh.AddVert(Point(along + GlintLengthDp, depth), clear, Vector4.zero);
                vh.AddVert(Point(mid, depth - 0.6f), glint, Vector4.zero);
                vh.AddTriangle(b, b + 1, b + 2);
                vh.AddTriangle(b + 2, b + 3, b);
                glints++;
            }
        }

        private void Quad(VertexHelper vh, float a0, float a1, float in0, float out0, float in1, float out1, Color32 c0, Color32 c1)
        {
            int b = vh.currentVertCount;
            vh.AddVert(Point(a0, in0), c0, Vector4.zero);
            vh.AddVert(Point(a0, out0), c0, Vector4.zero);
            vh.AddVert(Point(a1, out1), c1, Vector4.zero);
            vh.AddVert(Point(a1, in1), c1, Vector4.zero);
            vh.AddTriangle(b, b + 1, b + 2);
            vh.AddTriangle(b + 2, b + 3, b);
        }

        // Altura del borde ondulado en la posición a lo largo del eje.
        private float Edge(float along)
        {
            float lambda = Mathf.Max(1f, WaveLengthU * _unit);
            return Mathf.Max(1f, _thickness - WaveAmplitudeDp + (WaveAmplitudeDp * Mathf.Sin(along * 2f * Mathf.PI / lambda)));
        }

        // Punto a «along» dp a lo largo del eje y «across» dp hacia el borde ondulado.
        private Vector2 Point(float along, float across)
        {
            float side = _flip ? -1f : 1f;
            if (_vertical)
            {
                return new Vector2(_origin.x + (side * across), _origin.y - along);
            }

            return new Vector2(_origin.x + along, _origin.y + (side * across));
        }

        private static Color32 Alpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, c.a * a);
        }
    }
}
