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
    /// (<see cref="SetFlow"/>, 300-400 dp/s). Crece desde un origen hacia uno o los dos sentidos; mientras crece, el frente
    /// puede afinarse en lengua (<see cref="SetTaper"/>: el borde ondulado baja hasta el 15 % en la punta, con perfil
    /// redondeado), para que se lea como agua que avanza y no como una caja. Con <see cref="SetFade"/>, el cuerpo se aclara
    /// (menos opaco) desde el borde plano hasta una altura dada y recupera la opacidad en la cresta: el labio del río que
    /// desborda deja ver la orilla por detrás de la rama (revisión del 2026-10-09). Coordenadas del contenedor del
    /// tablero (origen arriba a la izquierda, y hacia arriba, como <see cref="StreamGraphic"/>). Como mucho
    /// <see cref="MaxQuads"/> cuadriláteros (con la franja aclarada, el cuerpo usa menos columnas de dos pisos); solo se
    /// reconstruye cuando cambian sus parámetros (la opacidad va por <c>CanvasRenderer.SetAlpha</c>, sin reconstruir).
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

        // Con la franja aclarada cada columna lleva dos pisos: 2 × 20 + líneas (≤ 6) + labio (≤ 5) + destellos (≤ 8) ≤ 64.
        private const int MaxFadeColumns = 20;
        private const int MaxLineQuads = 12;
        private const int MaxLipQuads = 8;
        private const int MaxGlints = 8;
        private const float LineInsetDp = 1.2f;
        private const float LineDp = 1f;
        private const float GlintSpacingDp = 38f;
        private const float GlintLengthDp = 7f;
        private const int TaperQuads = 4;
        private const float TaperTip = 0.15f;

        private Vector2 _origin;
        private float _back;
        private float _forward;
        private float _thickness = 5f;
        private float _unit = 24f;
        private bool _vertical;
        private bool _flip;
        private bool _lip;
        private float _flow;
        private float _taperBack;
        private float _taperForward;
        private float _fadeAcross;
        private float _fadeAlpha = 1f;
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

        /// <summary>
        /// Afina los extremos en lengua: <paramref name="backDp"/> y <paramref name="forwardDp"/> son los dp de cada extremo
        /// en los que el borde ondulado baja del todo hasta el 15 % de su altura en la punta (0: extremo recto).
        /// </summary>
        public void SetTaper(float backDp, float forwardDp)
        {
            backDp = Mathf.Max(0f, backDp);
            forwardDp = Mathf.Max(0f, forwardDp);
            if (!Mathf.Approximately(backDp, _taperBack) || !Mathf.Approximately(forwardDp, _taperForward))
            {
                _taperBack = backDp;
                _taperForward = forwardDp;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Aclara el cuerpo: desde el borde plano hasta <paramref name="acrossDp"/> (dp hacia el borde ondulado) la opacidad se
        /// multiplica por <paramref name="alpha"/>, y de ahí a la cresta vuelve a 1. <paramref name="acrossDp"/> ≤ 0 lo quita.
        /// </summary>
        public void SetFade(float acrossDp, float alpha)
        {
            acrossDp = Mathf.Max(0f, acrossDp);
            alpha = acrossDp > 0f ? Mathf.Clamp01(alpha) : 1f;
            if (!Mathf.Approximately(acrossDp, _fadeAcross) || !Mathf.Approximately(alpha, _fadeAlpha))
            {
                _fadeAcross = acrossDp;
                _fadeAlpha = alpha;
                SetVerticesDirty();
            }
        }

        /// <summary>Opacidad del cuerpo a <paramref name="acrossDp"/> del borde plano, en la parte recta de la tira (tests).</summary>
        public float BodyAlphaAt(float acrossDp)
        {
            if (_fadeAcross <= 0f || acrossDp <= _fadeAcross)
            {
                return _fadeAcross > 0f ? _fadeAlpha : 1f;
            }

            float crest = Mathf.Max(_fadeAcross + 0.01f, _thickness);
            return Mathf.Lerp(_fadeAlpha, 1f, Mathf.Clamp01((acrossDp - _fadeAcross) / (crest - _fadeAcross)));
        }

        /// <summary>
        /// Caja de la tira en las coordenadas de su RectTransform (las del contenedor: y hacia arriba), con la cresta completa.
        /// Vacía si no hay largo.
        /// </summary>
        public Rect LocalBounds()
        {
            float length = _back + _forward;
            if (length < 0.05f)
            {
                return Rect.zero;
            }

            Vector2 a = Point(-_back, 0f);
            Vector2 b = Point(_forward, _thickness);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
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
            float tb = TaperBack(length);
            float tf = TaperForward(length);
            int nb = tb > 0f ? TaperQuads : 0;
            int nf = tf > 0f ? TaperQuads : 0;
            bool fade = _fadeAcross > 0f;
            int maxBody = fade ? MaxFadeColumns : MaxBodyQuads;
            int mid = Mathf.Clamp(Mathf.CeilToInt((length - tb - tf) / (lambda * 0.25f)), 1, maxBody - nb - nf);
            int body = nb + mid + nf;
            float a = color.a;
            Color32 cBase = Alpha(_base, a);
            Color32 cSurface = Alpha(_surface, a);
            Color32 cBaseFaded = Alpha(_base, a * _fadeAlpha);
            for (int i = 0; i < body; i++)
            {
                float a0 = Along(i, nb, mid, nf, tb, tf, length);
                float a1 = Along(i + 1, nb, mid, nf, tb, tf, length);
                float e0 = Edge(a0);
                float e1 = Edge(a1);
                if (!fade)
                {
                    Quad(vh, a0, a1, 0f, e0, 0f, e1, cBase, cSurface, cBase, cSurface);
                    continue;
                }

                // Dos pisos: aclarado desde el borde plano hasta la franja y, de ahí a la cresta, de vuelta a la opacidad plena.
                float f0 = Mathf.Min(_fadeAcross, e0 * 0.98f);
                float f1 = Mathf.Min(_fadeAcross, e1 * 0.98f);
                Color32 m0 = Alpha(Color.Lerp(_base, _surface, f0 / e0), a * _fadeAlpha);
                Color32 m1 = Alpha(Color.Lerp(_base, _surface, f1 / e1), a * _fadeAlpha);
                Quad(vh, a0, a1, 0f, f0, 0f, f1, cBaseFaded, m0, cBaseFaded, m1);
                Quad(vh, a0, a1, f0, e0, f1, e1, m0, cSurface, m1, cSurface);
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
                float o0 = Mathf.Max(LineDp, Edge(a0) - LineInsetDp);
                float o1 = Mathf.Max(LineDp, Edge(a1) - LineInsetDp);
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

                float center = along + (GlintLengthDp * 0.5f);
                float depth = Mathf.Min(_thickness * (glints % 2 == 0 ? 0.55f : 0.35f), Edge(center) * 0.6f); // dentro de la lengua
                int b = vh.currentVertCount;
                vh.AddVert(Point(along, depth), clear, Vector4.zero);
                vh.AddVert(Point(center, depth + 0.6f), glint, Vector4.zero);
                vh.AddVert(Point(along + GlintLengthDp, depth), clear, Vector4.zero);
                vh.AddVert(Point(center, depth - 0.6f), glint, Vector4.zero);
                vh.AddTriangle(b, b + 1, b + 2);
                vh.AddTriangle(b + 2, b + 3, b);
                glints++;
            }
        }

        private void Quad(VertexHelper vh, float a0, float a1, float in0, float out0, float in1, float out1, Color32 c0, Color32 c1)
        {
            Quad(vh, a0, a1, in0, out0, in1, out1, c0, c0, c1, c1);
        }

        // Cuadrilátero con un color por vértice: dentro y fuera en a0, dentro y fuera en a1.
        private void Quad(VertexHelper vh, float a0, float a1, float in0, float out0, float in1, float out1, Color32 cIn0, Color32 cOut0, Color32 cIn1, Color32 cOut1)
        {
            int b = vh.currentVertCount;
            vh.AddVert(Point(a0, in0), cIn0, Vector4.zero);
            vh.AddVert(Point(a0, out0), cOut0, Vector4.zero);
            vh.AddVert(Point(a1, out1), cOut1, Vector4.zero);
            vh.AddVert(Point(a1, in1), cIn1, Vector4.zero);
            vh.AddTriangle(b, b + 1, b + 2);
            vh.AddTriangle(b + 2, b + 3, b);
        }

        // Altura del borde ondulado en la posición a lo largo del eje (más baja hacia las puntas afinadas, con perfil redondeado).
        private float Edge(float along)
        {
            float lambda = Mathf.Max(1f, WaveLengthU * _unit);
            float h = Mathf.Max(1f, _thickness - WaveAmplitudeDp + (WaveAmplitudeDp * Mathf.Sin(along * 2f * Mathf.PI / lambda)));
            float length = _back + _forward;
            float tb = TaperBack(length);
            float tf = TaperForward(length);
            float k = 1f;
            if (tb > 0f)
            {
                k = Mathf.Min(k, Mathf.Lerp(TaperTip, 1f, Mathf.Sqrt(Mathf.Clamp01((along + _back) / tb))));
            }

            if (tf > 0f)
            {
                k = Mathf.Min(k, Mathf.Lerp(TaperTip, 1f, Mathf.Sqrt(Mathf.Clamp01((_forward - along) / tf))));
            }

            return Mathf.Max(0.5f, h * k);
        }

        private float TaperBack(float length)
        {
            return _taperBack > 0f ? Mathf.Min(_taperBack, length * 0.5f) : 0f;
        }

        private float TaperForward(float length)
        {
            return _taperForward > 0f ? Mathf.Min(_taperForward, length * 0.5f) : 0f;
        }

        // Posición del borde i de los cuadriláteros del cuerpo: nb en la punta de atrás, mid en medio y nf en la de delante.
        private float Along(int i, int nb, int mid, int nf, float tb, float tf, float length)
        {
            if (i <= nb && nb > 0)
            {
                return -_back + (tb * i / nb);
            }

            int j = i - nb;
            if (j <= mid)
            {
                return -_back + tb + ((length - tb - tf) * j / mid);
            }

            return _forward - tf + (tf * (j - mid) / Mathf.Max(1, nf));
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
