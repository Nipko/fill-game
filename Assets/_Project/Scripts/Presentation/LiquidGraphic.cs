#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Líquido de una cuia en uGUI con el shader <c>AF/UI/Liquid</c> (GDD 08 §5.2; guía visual v2, 4.2): un quad por dosis
    /// visible, con los datos por dosis en los canales de vértice (uGUI no admite <c>MaterialPropertyBlock</c>; así todas
    /// las cuias comparten material y lote). El rectángulo del gráfico es el del cuerpo de la cuia con el pivote en el pie:
    /// las coordenadas locales son <c>(x − 1) · u</c> e <c>y · u</c> en dp. La vista lo rellena con
    /// <see cref="SetDose"/>; solo se reconstruye cuando algo cambia (08 §5.4).
    /// </summary>
    /// <remarks>
    /// <para><b>Inclinada</b> (al verter; guía 6.4: "el líquido no rota"): cada frontera entre dosis es una línea horizontal
    /// en pantalla cuya altura sale del volumen (<see cref="LevelForVolume"/>): el líquido llena la cavidad girada desde su
    /// punto más bajo, sin la cuña vacía junto al pie (revisión visual del 2026-10-07). El shader recorta a la silueta
    /// interior (cavidad, hombro y cuello). Mientras sale el chorro, una «lengua» de 0,12 u del tinte corre por la pared
    /// inferior del cuello desde la superficie hasta el labio (<see cref="SetTongue"/>).</para>
    /// <para>Canales: <c>TEXCOORD0</c> = (x, y local, y de capa, alto de la cuia); <c>TEXCOORD2.w</c> = patrón + 4 · dosis.
    /// La y de capa es la de la dosis sin inclinar (degradado, patrón, menisco y superficie); la y local, la del vidrio
    /// (brillos y recorte).</para>
    /// </remarks>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LiquidGraphic : MaskableGraphic
    {
        /// <summary>Amplitud inicial de la onda tras un vertido (08 §5.2): 0,03 u.</summary>
        public const float RippleAmplitudeU = 0.03f;

        /// <summary>Constante de caída de la onda (08 §5.2): exp(−t / 0,25 s).</summary>
        public const float RippleDecayMs = 250f;

        /// <summary>Grosor de la lengua de líquido por la pared inferior del cuello mientras sale el chorro (u).</summary>
        public const float TongueU = 0.12f;

        private const float TopExtraU = 0.10f;
        private const int AreaSlices = 32;
        private const int BisectSteps = 22;

        private static readonly int DpUId = Shader.PropertyToID("_DpU");
        private static readonly int PatternRowsId = Shader.PropertyToID("_PatternRows");
        private static readonly int InkId = Shader.PropertyToID("_InkLin");
        private static readonly int PaperId = Shader.PropertyToID("_PaperLin");

        private readonly float[] _xs = new float[8];
        private Color[] _color = Array.Empty<Color>();
        private float[] _fill = Array.Empty<float>();
        private int[] _tint = Array.Empty<int>();
        private float[] _levels = Array.Empty<float>();
        private Texture? _atlas;
        private int _capacity;
        private float _unit = 24f;
        private float _rippleMs = -1f;
        private float _ripplePhase;
        private float _flash;
        private float _slope;
        private float _tongue;
        private Color _tongueColor;
        private int _tongueTint;

        /// <inheritdoc />
        public override Texture mainTexture => _atlas != null ? _atlas : s_WhiteTexture;

        /// <summary>Onda en curso.</summary>
        public bool Rippling => _rippleMs >= 0f;

        /// <summary>Pendiente de las capas en la cuia (−tan del ángulo del cuerpo): 0 en reposo.</summary>
        public float Slope => _slope;

        /// <summary>Lengua de líquido visible (0..1).</summary>
        public float Tongue => _tongue;

        /// <summary>Prepara el gráfico para <paramref name="capacity"/> dosis con el atlas de patrones.</summary>
        public void Setup(int capacity, Texture atlas)
        {
            _capacity = capacity;
            _color = new Color[capacity];
            _fill = new float[capacity];
            _tint = new int[capacity];
            _levels = new float[capacity + 1];
            _atlas = atlas;
            raycastTarget = false;
            Material? mat = UiMaterials.Liquid;
            if (mat != null)
            {
                material = mat;
                mat.SetFloat(PatternRowsId, atlas.height / (float)ProceduralArt.PatternRowPx);
                Vector3 ink = ColorMath.ToShaderSpace(Palette.UiInk); // marcas sobre los tintes claros
                Vector3 paper = ColorMath.ToShaderSpace(Palette.UiPaper); // marcas sobre los tintes oscuros
                mat.SetColor(InkId, new Color(ink.x, ink.y, ink.z, 1f));
                mat.SetColor(PaperId, new Color(paper.x, paper.y, paper.z, 1f));
            }

            SetVerticesDirty();
            SetMaterialDirty();
        }

        /// <summary>
        /// Unidad u en dp (ancho de la cuia / 2). Todas las cuias de un nivel miden lo mismo (02 §5), así que 1 dp en u va
        /// en el material compartido.
        /// </summary>
        public void SetUnit(float unitDp)
        {
            if (!Mathf.Approximately(unitDp, _unit))
            {
                _unit = unitDp;
                SetVerticesDirty();
            }

            Material? mat = UiMaterials.Liquid;
            if (mat != null && unitDp > 0f)
            {
                mat.SetFloat(DpUId, 1f / unitDp);
            }
        }

        /// <summary>Fija la dosis <paramref name="k"/> (color, relleno 0..1, tinte; 0 = agua base).</summary>
        public void SetDose(int k, Color color, float fill, int tint)
        {
            if ((uint)k >= (uint)_capacity)
            {
                return;
            }

            fill = Mathf.Clamp01(fill);
            if (_color[k] == color && Mathf.Approximately(_fill[k], fill) && _tint[k] == tint)
            {
                return;
            }

            _color[k] = color;
            _fill[k] = fill;
            _tint[k] = tint;
            SetVerticesDirty();
        }

        /// <summary>
        /// Inclinación del cuerpo (grados; guía 6.4: "el líquido no rota"): las fronteras entre dosis siguen horizontales en
        /// pantalla y su altura sale del volumen de cada dosis dentro de la cavidad girada. Solo cambia al verter.
        /// </summary>
        public void SetTilt(float angleDeg)
        {
            float slope = -Mathf.Tan(Mathf.Clamp(angleDeg, -80f, 80f) * Mathf.Deg2Rad);
            if (Mathf.Abs(slope - _slope) > 0.0005f || (slope == 0f && _slope != 0f))
            {
                _slope = slope;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Lengua del chorro (0..1): una franja de <see cref="TongueU"/> del tinte <paramref name="tint"/> por la pared
        /// inferior desde la superficie hasta el labio, solo con la cuia inclinada.
        /// </summary>
        public void SetTongue(float amount, Color color, int tint)
        {
            amount = Mathf.Clamp01(amount);
            if (Mathf.Abs(amount - _tongue) < 0.001f && color == _tongueColor && tint == _tongueTint)
            {
                return;
            }

            _tongue = amount;
            _tongueColor = color;
            _tongueTint = tint;
            SetVerticesDirty();
        }

        /// <summary>Destello del sellado: 0 a 0,15 (07 §5.3).</summary>
        public void SetFlash(float flash)
        {
            if (!Mathf.Approximately(flash, _flash))
            {
                _flash = flash;
                SetVerticesDirty();
            }
        }

        /// <summary>Arranca la onda de la superficie (al caer una dosis).</summary>
        public void StartRipple()
        {
            _rippleMs = 0f;
            _ripplePhase = Mathf.Repeat(_ripplePhase + 2.3f, Mathf.PI * 2f);
            SetVerticesDirty();
        }

        /// <summary>Quita la onda (reposo: la superficie está quieta).</summary>
        public void StopRipple()
        {
            if (_rippleMs >= 0f)
            {
                _rippleMs = -1f;
                SetVerticesDirty();
            }
        }

        /// <summary>Avanza la onda; solo reconstruye la malla mientras dura.</summary>
        public void TickRipple(float dtMs)
        {
            if (_rippleMs < 0f)
            {
                return;
            }

            _rippleMs += dtMs;
            if (RippleAmplitude() < 0.0008f)
            {
                _rippleMs = -1f;
            }

            SetVerticesDirty();
        }

        /// <summary>
        /// Área (u²) de la silueta interior de una cuia de <paramref name="capacity"/> dosis (cavidad, hombro y cuello hasta
        /// el labio) que queda por debajo de la línea <c>y = c + slope · (x − 1)</c> en coordenadas de la cuia. Exacta en la
        /// cavidad recta; por franjas de 0,036 u en el hombro y el cuello.
        /// </summary>
        public static float AreaBelow(float c, float slope, int capacity)
        {
            // Cavidad recta: integral exacta (la longitud bajo la línea es lineal a trozos). Hombro y cuello: por franjas.
            float bottom = CuiaGeometry.CavityBottomU;
            float top = CuiaGeometry.CavityTopU(capacity);
            float lip = CuiaGeometry.LipU(capacity);
            float w = CuiaGeometry.CavityHalfWidthU;
            float area = RectAreaBelow(c, slope, w, bottom, top);
            float dy = (lip - top) / AreaSlices;
            for (int i = 0; i < AreaSlices; i++)
            {
                float y = top + ((i + 0.5f) * dy);
                float hw = CuiaGeometry.InnerHalfWidth(y, capacity);
                float len;
                if (slope > 1e-5f)
                {
                    len = hw - Mathf.Max(-hw, (y - c) / slope);
                }
                else if (slope < -1e-5f)
                {
                    len = Mathf.Min(hw, (y - c) / slope) + hw;
                }
                else
                {
                    len = y <= c ? 2f * hw : 0f;
                }

                area += Mathf.Clamp(len, 0f, 2f * hw) * dy;
            }

            return area;
        }

        // ∫ de −w a w de clamp(c + s·X − b, 0, t − b) dX: exacta, partiendo en los puntos donde la línea corta b y t.
        private static float RectAreaBelow(float c, float s, float w, float b, float t)
        {
            float h = t - b;
            if (Mathf.Abs(s) < 1e-6f)
            {
                return 2f * w * Mathf.Clamp(c - b, 0f, h);
            }

            float x0 = (b - c) / s;
            float x1 = (t - c) / s;
            float lo = Mathf.Min(x0, x1);
            float hi = Mathf.Max(x0, x1);
            float a = -w;
            float area = 0f;
            float p1 = Mathf.Clamp(lo, -w, w);
            float p2 = Mathf.Clamp(hi, -w, w);
            area += Trapezoid(a, p1, c, s, b, h);
            area += Trapezoid(p1, p2, c, s, b, h);
            area += Trapezoid(p2, w, c, s, b, h);
            return area;
        }

        private static float Trapezoid(float xa, float xb, float c, float s, float b, float h)
        {
            if (xb <= xa)
            {
                return 0f;
            }

            float fa = Mathf.Clamp(c + (s * xa) - b, 0f, h);
            float fb = Mathf.Clamp(c + (s * xb) - b, 0f, h);
            return (xb - xa) * 0.5f * (fa + fb);
        }

        /// <summary>
        /// Altura <c>c</c> (en x = 1) de la superficie horizontal que deja <paramref name="volume"/> u² de líquido por debajo,
        /// llenando desde el punto más bajo de la cavidad girada (bisección sobre <see cref="AreaBelow"/>).
        /// </summary>
        public static float LevelForVolume(float volume, float slope, int capacity)
        {
            float bottom = CuiaGeometry.CavityBottomU;
            float lip = CuiaGeometry.LipU(capacity);
            float w = CuiaGeometry.CavityHalfWidthU;
            if (Mathf.Abs(slope) < 1e-5f)
            {
                return bottom + (volume / (2f * w));
            }

            float lo = bottom - (Mathf.Abs(slope) * w);
            float hi = lip + (Mathf.Abs(slope) * w);
            if (volume <= 0f)
            {
                return lo;
            }

            for (int i = 0; i < BisectSteps; i++)
            {
                float mid = 0.5f * (lo + hi);
                if (AreaBelow(mid, slope, capacity) < volume)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            return 0.5f * (lo + hi);
        }

        /// <inheritdoc />
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int top = -1;
            for (int k = 0; k < _capacity; k++)
            {
                if (_fill[k] > 0.001f && _color[k].a > 0.001f)
                {
                    top = k;
                }
            }

            if (_unit <= 0f || _capacity <= 0)
            {
                return;
            }

            float heightU = CuiaGeometry.HeightU(_capacity);
            float bottomU = CuiaGeometry.CavityBottomU;
            float amp = RippleAmplitude();
            float width = 2f * CuiaGeometry.CavityHalfWidthU;
            bool tilted = Mathf.Abs(_slope) > 1e-4f;
            if (top >= 0)
            {
                float surface = bottomU + top + _fill[top];
                int topTint = _tint[top];
                Color men = ColorMath.Lighten(_color[top], topTint > 0 ? Palette.MeniscusLighten(topTint) : 0.20f);
                Vector3 menS = ColorMath.ToShaderSpace(men);

                // Fronteras por volumen: _levels[k] es la línea bajo la dosis k (k = 0, el fondo) y _levels[k + 1] la de
                // encima. Sin inclinar coinciden con bottom + k + relleno.
                float volume = 0f;
                _levels[0] = tilted ? LevelForVolume(0f, _slope, _capacity) : bottomU;
                for (int k = 0; k <= top; k++)
                {
                    volume += _fill[k] * width;
                    _levels[k + 1] = tilted ? LevelForVolume(volume, _slope, _capacity) : bottomU + k + _fill[k];
                }

                for (int k = 0; k <= top; k++)
                {
                    float fill = _fill[k];
                    Color c = _color[k];
                    if (fill <= 0.001f || c.a <= 0.001f)
                    {
                        continue;
                    }

                    int tint = _tint[k];
                    Color lit = tint > 0 ? Palette.LiquidHighlight(tint, c) : ColorMath.AddLabLightness(c, 10f);
                    Vector3 litS = ColorMath.ToShaderSpace(lit);
                    float pattern = -1f; // sin patrón (Guanábana, agua base)
                    if (ProceduralArt.HasPattern(tint))
                    {
                        pattern = Palette.PatternBase(tint) == Palette.UiPaper ? 1f : 0f;
                    }

                    var uv1 = new Vector4(litS.x, litS.y, litS.z, _flash);
                    var uv2 = new Vector4(surface, amp, _ripplePhase, pattern + (4f * k));
                    var uv3 = new Vector4(menS.x, menS.y, menS.z, tint);
                    float lo = tilted ? _levels[k] : bottomU + k;
                    float hi = tilted ? _levels[k + 1] : bottomU + k + fill;
                    float hiMesh = k == top ? hi + TopExtraU : hi; // la dosis superior sube 0,1 u para la onda y el menisco
                    if (!tilted)
                    {
                        AddQuad(vh, -CuiaGeometry.CavityHalfWidthU, CuiaGeometry.CavityHalfWidthU, lo, hiMesh, 0f, lo, hi, bottomU + k, fill, heightU, c, uv1, uv2, uv3);
                    }
                    else
                    {
                        AddBand(vh, lo, hiMesh, lo, hi, bottomU + k, fill, heightU, c, uv1, uv2, uv3);
                    }
                }
            }

            if (tilted && _tongue > 0.01f && top >= 0)
            {
                AddTongue(vh, heightU, top);
            }
        }

        // Banda inclinada entre las líneas y = lo + s·X e y = hiMesh + s·X (X = x − 1), recortada a [fondo, labio]: se
        // parte en tiras verticales en los puntos donde las líneas cortan el fondo o el labio, y cada tira es un quad
        // exacto. El shader recorta luego a la silueta interior.
        private void AddBand(VertexHelper vh, float lo, float hiMesh, float layerLo, float layerHi, float doseBottom, float fill, float heightU, Color c, Vector4 uv1, Vector4 uv2, Vector4 uv3)
        {
            float s = _slope;
            float w = CuiaGeometry.CavityHalfWidthU;
            float yMin = CuiaGeometry.CavityBottomU;
            float yMax = CuiaGeometry.LipU(_capacity);
            int n = 0;
            _xs[n++] = -w;
            _xs[n++] = w;
            AddBreak(ref n, (yMin - lo) / s, w);
            AddBreak(ref n, (yMax - lo) / s, w);
            AddBreak(ref n, (yMin - hiMesh) / s, w);
            AddBreak(ref n, (yMax - hiMesh) / s, w);

            // Orden por inserción (como mucho 6 valores, sin asignaciones).
            for (int i = 1; i < n; i++)
            {
                float v = _xs[i];
                int j = i - 1;
                while (j >= 0 && _xs[j] > v)
                {
                    _xs[j + 1] = _xs[j];
                    j--;
                }

                _xs[j + 1] = v;
            }

            for (int i = 0; i + 1 < n; i++)
            {
                float xa = _xs[i];
                float xb = _xs[i + 1];
                if (xb - xa < 1e-4f)
                {
                    continue;
                }

                AddQuad(vh, xa, xb, lo, hiMesh, s, layerLo, layerHi, doseBottom, fill, heightU, c, uv1, uv2, uv3);
            }
        }

        private void AddBreak(ref int n, float x, float w)
        {
            if (x > -w && x < w)
            {
                _xs[n++] = x;
            }
        }

        // Quad de la tira [xa, xb] (X relativo al centro) entre las líneas lo + s·X y hi + s·X, recortadas a [fondo, labio].
        // La y de capa se interpola de forma afín: (fondo de la dosis) + relleno · (y − s·X − layerLo) / (layerHi − layerLo).
        private void AddQuad(VertexHelper vh, float xa, float xb, float lo, float hi, float s, float layerLo, float layerHi, float doseBottom, float fill, float heightU, Color c, Vector4 uv1, Vector4 uv2, Vector4 uv3)
        {
            float yMin = CuiaGeometry.CavityBottomU;
            float yMax = CuiaGeometry.LipU(_capacity);
            float loA = Mathf.Clamp(lo + (s * xa), yMin, yMax);
            float loB = Mathf.Clamp(lo + (s * xb), yMin, yMax);
            float hiA = Mathf.Clamp(hi + (s * xa), yMin, yMax);
            float hiB = Mathf.Clamp(hi + (s * xb), yMin, yMax);
            if (hiA - loA < 1e-4f && hiB - loB < 1e-4f)
            {
                return;
            }

            float scale = fill / Mathf.Max(1e-4f, layerHi - layerLo);
            Color32 vc = c;
            int i0 = vh.currentVertCount;
            AddVert(vh, xa, loA, doseBottom + ((loA - (s * xa) - layerLo) * scale), heightU, vc, uv1, uv2, uv3);
            AddVert(vh, xa, hiA, doseBottom + ((hiA - (s * xa) - layerLo) * scale), heightU, vc, uv1, uv2, uv3);
            AddVert(vh, xb, hiB, doseBottom + ((hiB - (s * xb) - layerLo) * scale), heightU, vc, uv1, uv2, uv3);
            AddVert(vh, xb, loB, doseBottom + ((loB - (s * xb) - layerLo) * scale), heightU, vc, uv1, uv2, uv3);
            vh.AddTriangle(i0, i0 + 1, i0 + 2);
            vh.AddTriangle(i0 + 2, i0 + 3, i0);
        }

        // Lengua del chorro: franja de 0,12 u por la pared interior del lado bajo (el que mira al destino), desde donde la
        // superficie toca esa pared hasta el labio. Color de la dosis que se vacía, sin patrón ni menisco.
        private void AddTongue(VertexHelper vh, float heightU, int top)
        {
            float side = _slope < 0f ? -1f : 1f; // lado bajo de la cavidad girada (x − 1 negativo si el cuerpo gira a la izquierda)
            float lip = CuiaGeometry.LipU(_capacity);
            float surfaceAtWall = Mathf.Clamp(_levels[top + 1] + (_slope * side * CuiaGeometry.CavityHalfWidthU), CuiaGeometry.CavityBottomU, lip);
            if (lip - surfaceAtWall < 0.05f)
            {
                return;
            }

            Color c = _tongueColor;
            Vector3 litS = ColorMath.ToShaderSpace(c);
            float layer = CuiaGeometry.CavityBottomU + 0.5f; // dentro de una dosis llena: alfa 1, sin menisco
            var uv1 = new Vector4(litS.x, litS.y, litS.z, 0f);
            var uv2 = new Vector4(CuiaGeometry.CavityBottomU + 2f, 0f, 0f, -1f);
            var uv3 = new Vector4(litS.x, litS.y, litS.z, 0f);
            Color32 vc = c;
            float thick = TongueU * _tongue;
            const int segments = 8;
            int first = vh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float y = Mathf.Lerp(surfaceAtWall - 0.15f, lip, i / (float)segments);
                float wall = CuiaGeometry.InnerHalfWidth(Mathf.Max(y, CuiaGeometry.CavityBottomU), _capacity);
                float outer = side * (wall + 0.04f); // un poco más allá de la pared: la recorta el shader
                float inner = side * (wall - thick);
                AddVert(vh, outer, y, layer, heightU, vc, uv1, uv2, uv3);
                AddVert(vh, inner, y, layer, heightU, vc, uv1, uv2, uv3);
                if (i > 0)
                {
                    int b = first + ((i - 1) * 2);
                    vh.AddTriangle(b, b + 1, b + 3);
                    vh.AddTriangle(b + 3, b + 2, b);
                }
            }
        }

        // xRel = x − 1 (u); yU = y local (u); layer = y de capa (u).
        private void AddVert(VertexHelper vh, float xRel, float yU, float layer, float heightU, Color32 color, Vector4 uv1, Vector4 uv2, Vector4 uv3)
        {
            var pos = new Vector3(xRel * _unit, yU * _unit, 0f);
            vh.AddVert(pos, color, new Vector4(xRel + 1f, yU, layer, heightU), uv1, uv2, uv3, Vector3.back, new Vector4(1f, 0f, 0f, -1f));
        }

        private float RippleAmplitude()
        {
            return _rippleMs < 0f ? 0f : RippleAmplitudeU * Mathf.Exp(-_rippleMs / RippleDecayMs);
        }
    }
}
