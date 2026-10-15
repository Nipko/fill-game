#nullable enable
using AF.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>Aspecto de una cinta del Mapa del río (propuesta del Mapa §3.4 y §11.1).</summary>
    public enum RibbonLook
    {
        /// <summary>Vidrio: «por descubrir», azul hielo translúcido con borde claro, sin tipos de agua.</summary>
        Glass = 0,

        /// <summary>Agua: el color de cada tramo (<c>water</c>) y las dos aguas lado a lado (<c>splits</c>), con orilla oscura.</summary>
        Water = 1,
    }

    /// <summary>
    /// Cinta de un río del Mapa (08 §2.2; propuesta del Mapa §3.3-§3.4, §11.1 y §11.4): <c>MaskableGraphic</c> con tiras de
    /// triángulos y color por vértice sobre el material de UI por defecto, como <see cref="WaterStripGraphic"/> y
    /// <see cref="BezierStripGraphic"/>, sin shader propio. Dibuja el intervalo [s0, s1] de una línea (<see cref="SetFill"/>), de
    /// modo que sirve para las tres instancias de la pantalla: el vidrio (toda la cuenca, una vez), el lleno (al abrir y tras una
    /// llegada) y el frente (solo el tramo que se anima, con <see cref="FrontMaxVertices"/> como tope).
    /// <list type="bullet">
    /// <item><description>Ancho por la clase de la línea: <see cref="WidthClass1Dp"/>, <see cref="WidthClass2Dp"/> y
    /// <see cref="WidthClass3Dp"/> [P]; uniones en inglete limitadas a <see cref="MiterLimit"/> veces el medio ancho, para que un
    /// codo cerrado no lance vértices lejos.</description></item>
    /// <item><description>Agua: color por vértice según <c>water</c> (blanca, negra, clara y andina) y, en las franjas de
    /// <c>splits</c>, la cinta partida a lo largo con el agua de cada orilla mirando río abajo; los cambios de agua son
    /// netos (cada tramo con sus propios vértices). Un tramo sin agua comprobada ([NV]) se dibuja en vidrio.</description></item>
    /// <item><description>El lleno y el vidrio se distinguen también por el trazo: el vidrio lleva un borde claro (#EAF7F3) y el
    /// agua, una orilla más oscura que su color; con Patrones de fruta (02 §12; <see cref="SetPatterns"/>), el agua lleva además
    /// una trama por tipo: puntos en la blanca y la andina, rayas en la negra y ondas en la clara.</description></item>
    /// <item><description>Presupuesto de vértices (<see cref="SetVertexBudget"/>): si el intervalo no cabe, se saltan puntos
    /// intermedios de la línea de forma uniforme, y la trama sale solo mientras quede presupuesto; nunca se pasa del
    /// tope.</description></item>
    /// </list>
    /// Las coordenadas son las locales de su RectTransform según <see cref="MapSpace"/>. Solo se reconstruye cuando cambia algo
    /// (<see cref="SetFill"/> sin cambios no hace nada ni asigna memoria); al reconstruirse no asigna memoria tras la primera vez.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RiverRibbonGraphic : MaskableGraphic
    {
        /// <summary>Ancho del cauce y del ramal del Pará (clase 1), en dp [P].</summary>
        public const float WidthClass1Dp = 6f;

        /// <summary>Ancho de los afluentes (clase 2), en dp [P].</summary>
        public const float WidthClass2Dp = 3.5f;

        /// <summary>Ancho de los ríos locales (clase 3), en dp [P].</summary>
        public const float WidthClass3Dp = 2f;

        /// <summary>Tope de vértices del frente, el tramo que se anima (propuesta del Mapa §11.4).</summary>
        public const int FrontMaxVertices = 400;

        /// <summary>Tope de vértices de una malla de uGUI.</summary>
        public const int MaxVertices = 65000;

        /// <summary>Largo máximo del inglete, en medios anchos [P].</summary>
        public const float MiterLimit = 2f;

        /// <summary>Grosor del borde del vidrio y de la orilla del agua, en dp [P] (como mucho el 35 % del medio ancho).</summary>
        public const float RimDp = 1f;

        /// <summary>Paso de la trama de Patrones de fruta a lo largo de la cinta, en dp [P].</summary>
        public const float PatternStepDp = 4f;

        /// <summary>
        /// Mezcla de la trama hacia la tinta o el papel [P]: 0,35, la opacidad del patrón en Alto contraste (07 §3), porque sobre
        /// una cinta de 2-6 dp la del tablero (0,20) no se distingue.
        /// </summary>
        public const float PatternMix = 0.35f;

        private const float RimMaxFraction = 0.35f;

        /// <summary>Vidrio del Mapa, azul hielo translúcido (#CFEFF3 con alfa 0,85) [P] (paleta de la muestra del Mapa).</summary>
        public static readonly Color GlassBody = new Color(0xCF / 255f, 0xEF / 255f, 0xF3 / 255f, 0.85f);

        /// <summary>Borde del vidrio, <c>vidrio_borde</c> #EAF7F3 (direccion-visual-v2 §2.2).</summary>
        public static readonly Color GlassRim = Palette.Hex(0xEAF7F3);

        /// <summary>Agua andina #9FC9D4 [P] (paleta de la muestra del Mapa, <c>generar_mapa.py</c>).</summary>
        public static readonly Color AndeanWater = Palette.Hex(0x9FC9D4);

        private RiverLine? _line;
        private PolylineCursor? _cursor;
        private MapSpace _space = new MapSpace(MapSpace.BasinWidthDp, Vector2.zero);
        private RibbonLook _look = RibbonLook.Glass;
        private bool _patterns;
        private float _s0;
        private float _s1 = 1f;
        private float _widthDp = -1f;
        private int _budget = MaxVertices;
        private int _vertexCount;
        private int _markCount;

        /// <summary>Configura el gráfico (sin raycast).</summary>
        public void Setup()
        {
            raycastTarget = false;
        }

        /// <summary>Línea que dibuja, o null.</summary>
        public RiverLine? Line => _line;

        /// <summary>Cursor de la línea (el mismo que pueden usar la canoa y Rosa), o null.</summary>
        public PolylineCursor? Cursor => _cursor;

        /// <summary>Inicio del intervalo dibujado (s).</summary>
        public float FillStart => _s0;

        /// <summary>Fin del intervalo dibujado (s).</summary>
        public float FillEnd => _s1;

        /// <summary>Aspecto.</summary>
        public RibbonLook Look => _look;

        /// <summary>Lleva la trama de Patrones de fruta.</summary>
        public bool Patterns => _patterns;

        /// <summary>Tope de vértices.</summary>
        public int VertexBudget => _budget;

        /// <summary>Ancho de la cinta en dp: el fijado con <see cref="SetWidth"/> o el de la clase de la línea.</summary>
        public float WidthDp => _widthDp > 0f ? _widthDp : (_line != null ? WidthForClass(_line.Class) : WidthClass1Dp);

        /// <summary>Vértices de la última malla construida.</summary>
        public int LastVertexCount => _vertexCount;

        /// <summary>Marcas de la trama de la última malla construida.</summary>
        public int LastMarkCount => _markCount;

        /// <summary>Ancho por clase de línea [P]: 6 dp (1), 3,5 dp (2) y 2 dp (3).</summary>
        public static float WidthForClass(int lineClass)
        {
            switch (lineClass)
            {
                case 1:
                    return WidthClass1Dp;
                case 2:
                    return WidthClass2Dp;
                default:
                    return WidthClass3Dp;
            }
        }

        /// <summary>
        /// Color del agua <paramref name="water"/>: la base de cada agua (07 §2.1; <see cref="Palette.WaterBase"/>) y la andina
        /// [P]; sin agua comprobada, el vidrio.
        /// </summary>
        public static Color WaterColor(RiverWater water)
        {
            switch (water)
            {
                case RiverWater.White:
                    return Palette.WaterWhite;
                case RiverWater.Black:
                    return Palette.WaterBlack;
                case RiverWater.Clear:
                    return Palette.WaterClear;
                case RiverWater.Andean:
                    return AndeanWater;
                default:
                    return GlassBody;
            }
        }

        /// <summary>Orilla del agua <paramref name="water"/>: su color un 35 % hacia la tinta [P]; sin agua, el borde del vidrio.</summary>
        public static Color RimColor(RiverWater water)
        {
            if (water == RiverWater.None)
            {
                return GlassRim;
            }

            Color rim = Color.Lerp(WaterColor(water), Palette.UiInk, 0.35f);
            rim.a = 1f;
            return rim;
        }

        /// <summary>
        /// Color de la trama sobre el agua <paramref name="water"/> (opaco): hacia el papel sobre las aguas oscuras (L* &lt; 50) y
        /// hacia la tinta sobre las claras, con <see cref="PatternMix"/>, como <see cref="Palette.PatternMark(Color, int, float)"/>.
        /// </summary>
        public static Color MarkColor(RiverWater water)
        {
            Color body = WaterColor(water);
            Color toward = ColorMath.LabLightness(body) < 50f ? Palette.UiPaper : Palette.UiInk;
            Color mark = Color.Lerp(body, toward, PatternMix);
            mark.a = 1f;
            return mark;
        }

        /// <summary>
        /// Línea que dibuja. Crea su <see cref="PolylineCursor"/> (una vez por línea: es la única asignación de la cinta) y vuelve
        /// a dibujar el intervalo entero [0, 1].
        /// </summary>
        public void SetLine(RiverLine? line)
        {
            if (ReferenceEquals(line, _line))
            {
                return;
            }

            _line = line;
            _cursor = line != null ? new PolylineCursor(line) : null;
            _s0 = 0f;
            _s1 = 1f;
            SetVerticesDirty();
        }

        /// <summary>Espacio del Mapa (dp por unidad y origen local).</summary>
        public void SetSpace(MapSpace space)
        {
            if (!space.Equals(_space))
            {
                _space = space;
                SetVerticesDirty();
            }
        }

        /// <summary>Aspecto: vidrio o agua.</summary>
        public void SetLook(RibbonLook look)
        {
            if (look != _look)
            {
                _look = look;
                SetVerticesDirty();
            }
        }

        /// <summary>Trama de Patrones de fruta (02 §12): solo en el aspecto de agua.</summary>
        public void SetPatterns(bool on)
        {
            if (on != _patterns)
            {
                _patterns = on;
                SetVerticesDirty();
            }
        }

        /// <summary>Ancho en dp; ≤ 0 vuelve al de la clase de la línea.</summary>
        public void SetWidth(float widthDp)
        {
            widthDp = widthDp > 0f ? widthDp : -1f;
            if (widthDp != _widthDp)
            {
                _widthDp = widthDp;
                SetVerticesDirty();
            }
        }

        /// <summary>Tope de vértices (al menos 8; el frente usa <see cref="FrontMaxVertices"/>).</summary>
        public void SetVertexBudget(int maxVertices)
        {
            maxVertices = Mathf.Clamp(maxVertices, 8, MaxVertices);
            if (maxVertices != _budget)
            {
                _budget = maxVertices;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Intervalo dibujado, en s (en cualquier orden: un tramo que remonta va de s mayor a menor; se recorta a [0, 1]). Solo
        /// reconstruye si cambia: devuelve true si cambió. Sin asignaciones.
        /// </summary>
        public bool SetFill(float s0, float s1)
        {
            float a = Mathf.Clamp01(Mathf.Min(s0, s1));
            float b = Mathf.Clamp01(Mathf.Max(s0, s1));
            if (a == _s0 && b == _s1)
            {
                return false;
            }

            _s0 = a;
            _s1 = b;
            SetVerticesDirty();
            return true;
        }

        /// <inheritdoc />
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            _vertexCount = 0;
            _markCount = 0;
            RiverLine? line = _line;
            PolylineCursor? cursor = _cursor;
            if (line == null || cursor == null || cursor.Length <= 0f || _s1 <= _s0 || _space.DpPerUnit <= 0f)
            {
                return;
            }

            float half = WidthDp * 0.5f;
            float rim = Mathf.Min(RimDp, half * RimMaxFraction) / half;
            float length = cursor.Length;
            float aStart = _s0 * length;
            float aEnd = _s1 * length;
            bool water = _look == RibbonLook.Water;

            // Puntos de la línea estrictamente dentro del intervalo.
            int count = cursor.PointCount;
            int first = cursor.Locate(aStart, out _) + 1;
            while (first < count && cursor.ArcOfPoint(first) <= aStart)
            {
                first++;
            }

            int last = cursor.Locate(aEnd, out _) + 1;
            while (last >= first && (last >= count || cursor.ArcOfPoint(last) >= aEnd))
            {
                last--;
            }

            int inner = Mathf.Max(0, last - first + 1);

            // Presupuesto: secciones fijas (los dos extremos y cada cambio de agua, que va dos veces) y, si no caben todas, se
            // salta uno de cada «stride» puntos intermedios.
            int boundaries = 0;
            bool anySplit = false;
            if (water)
            {
                for (float b = line.NextBoundaryAfter(_s0); b < _s1; b = line.NextBoundaryAfter(b))
                {
                    boundaries++;
                }

                for (int i = 0; i < line.Splits.Count; i++)
                {
                    anySplit |= line.Splits[i].S0 < _s1 && line.Splits[i].S1 > _s0;
                }
            }

            int perSection = 2 * (anySplit ? 4 : 3);
            int fixedSections = 2 + (2 * boundaries);
            int stride = 1;
            if ((inner + fixedSections) * perSection > _budget)
            {
                int room = (_budget / perSection) - fixedSections;
                stride = room >= 1 ? (inner + room - 1) / room : inner + 1;
            }

            RunLook look = LookAt(line, _s0, water);
            int previous = EmitSection(vh, cursor, aStart, -1, look, half, rim, -1);
            int nextVertex = first;
            float nextBoundary = water ? line.NextBoundaryAfter(_s0) : 2f;
            while (previous >= 0)
            {
                float vertexArc = nextVertex <= last ? cursor.ArcOfPoint(nextVertex) : float.MaxValue;
                float boundaryArc = nextBoundary < _s1 ? nextBoundary * length : float.MaxValue;
                float arc = Mathf.Min(aEnd, Mathf.Min(vertexArc, boundaryArc));
                bool atVertex = vertexArc <= arc && arc < aEnd;
                bool atBoundary = boundaryArc <= arc && arc < aEnd;
                int vertex = atVertex ? nextVertex : -1;

                previous = EmitSection(vh, cursor, arc, vertex, look, half, rim, previous);
                if (previous < 0 || arc >= aEnd)
                {
                    break;
                }

                if (atVertex)
                {
                    nextVertex += stride;
                }

                if (atBoundary)
                {
                    float s = nextBoundary;
                    nextBoundary = line.NextBoundaryAfter(s);
                    RunLook after = LookAt(line, s, water);
                    if (!after.Equals(look))
                    {
                        look = after;
                        previous = EmitSection(vh, cursor, arc, vertex, look, half, rim, -1); // cambio neto: tira nueva
                    }
                }
            }

            if (water && _patterns)
            {
                AddPattern(vh, line, cursor, aStart, aEnd, half);
            }

            _vertexCount = vh.currentVertCount;
        }

        // Una sección transversal a la longitud de arco «arc» (en el punto «vertex» de la línea, con inglete, o dentro de un
        // tramo): dos vértices por banda. Si «connect» ≥ 0, la une con la sección anterior de la misma tira. Devuelve el primer
        // vértice de la sección, o −1 si no cabe en el presupuesto.
        private int EmitSection(VertexHelper vh, PolylineCursor cursor, float arc, int vertex, RunLook look, float half, float rim,
            int connect)
        {
            int bands = look.Split ? 4 : 3;
            if (vh.currentVertCount + (2 * bands) > _budget)
            {
                return -1;
            }

            Vector2 center;
            Vector2 offset;
            if (vertex >= 0)
            {
                center = _space.ToLocal(cursor.Point(vertex));
                cursor.DirectionsAt(vertex, out Vector2 incoming, out Vector2 outgoing);
                offset = Miter(Normal(incoming), Normal(outgoing));
            }
            else
            {
                int segment = cursor.Locate(arc, out _);
                center = _space.ToLocal(cursor.PointAtArc(arc));
                offset = Normal(cursor.SegmentDirection(segment));
            }

            offset *= half;
            float alpha = color.a;
            int start = vh.currentVertCount;
            for (int k = 0; k < bands; k++)
            {
                Band(look, k, rim, out float u0, out float u1, out Color c);
                c.a *= alpha;
                Color32 c32 = c;
                vh.AddVert(center + (offset * u0), c32, Vector4.zero);
                vh.AddVert(center + (offset * u1), c32, Vector4.zero);
                if (connect >= 0)
                {
                    int p = connect + (2 * k);
                    int q = start + (2 * k);
                    vh.AddTriangle(p, p + 1, q + 1);
                    vh.AddTriangle(q + 1, q, p);
                }
            }

            return start;
        }

        // Banda k de la sección: de u0 a u1 en medios anchos (+1 = orilla izquierda mirando río abajo) y su color.
        private void Band(RunLook look, int k, float rim, out float u0, out float u1, out Color c)
        {
            bool glass = _look == RibbonLook.Glass;
            if (!look.Split)
            {
                RiverWater w = glass ? RiverWater.None : look.A;
                switch (k)
                {
                    case 0:
                        u0 = -1f;
                        u1 = -1f + rim;
                        c = RimColor(w);
                        return;
                    case 1:
                        u0 = -1f + rim;
                        u1 = 1f - rim;
                        c = WaterColor(w);
                        return;
                    default:
                        u0 = 1f - rim;
                        u1 = 1f;
                        c = RimColor(w);
                        return;
                }
            }

            // Dos aguas: la derecha (B) de −1 a 0 y la izquierda (A) de 0 a 1, cada una con su orilla.
            switch (k)
            {
                case 0:
                    u0 = -1f;
                    u1 = -1f + rim;
                    c = RimColor(look.B);
                    return;
                case 1:
                    u0 = -1f + rim;
                    u1 = 0f;
                    c = WaterColor(look.B);
                    return;
                case 2:
                    u0 = 0f;
                    u1 = 1f - rim;
                    c = WaterColor(look.A);
                    return;
                default:
                    u0 = 1f - rim;
                    u1 = 1f;
                    c = RimColor(look.A);
                    return;
            }
        }

        // Trama de Patrones de fruta (02 §12; propuesta del Mapa §11.4): una marca cada PatternStepDp, de 4 vértices, mientras
        // quede presupuesto. En las dos aguas, una marca por mitad.
        private void AddPattern(VertexHelper vh, RiverLine line, PolylineCursor cursor, float aStart, float aEnd, float half)
        {
            float step = PatternStepDp / _space.DpPerUnit;
            int marks = Mathf.Min(MaxVertices / 4, Mathf.FloorToInt((aEnd - aStart) / step));
            for (int index = 0; index < marks; index++)
            {
                float arc = aStart + (step * (index + 0.5f));
                RunLook look = LookAt(line, arc / cursor.Length, true);
                int segment = cursor.Locate(arc, out _);
                Vector2 center = _space.ToLocal(cursor.PointAtArc(arc));
                Vector2 dir = MapSpace.DirectionToLocal(cursor.SegmentDirection(segment));
                Vector2 normal = new Vector2(-dir.y, dir.x);
                if (look.Split)
                {
                    if (!AddMark(vh, look.A, center + (normal * (half * 0.5f)), dir, normal, half * 0.5f, index)
                        || !AddMark(vh, look.B, center - (normal * (half * 0.5f)), dir, normal, half * 0.5f, index))
                    {
                        return;
                    }
                }
                else if (!AddMark(vh, look.A, center, dir, normal, half, index))
                {
                    return;
                }
            }
        }

        // Una marca: punto (blanca y andina), raya transversal (negra) u onda (clara: trazos diagonales alternos que forman un
        // zigzag). Devuelve false si ya no cabe en el presupuesto.
        private bool AddMark(VertexHelper vh, RiverWater water, Vector2 c, Vector2 dir, Vector2 normal, float room, int index)
        {
            if (water == RiverWater.None)
            {
                return true;
            }

            if (vh.currentVertCount + 4 > _budget)
            {
                return false;
            }

            Color32 mark = MarkColorWithAlpha(water);
            int b = vh.currentVertCount;
            switch (water)
            {
                case RiverWater.Black:
                {
                    Vector2 across = normal * (room * 0.65f);
                    Vector2 thick = dir * 0.4f;
                    vh.AddVert(c - across - thick, mark, Vector4.zero);
                    vh.AddVert(c + across - thick, mark, Vector4.zero);
                    vh.AddVert(c + across + thick, mark, Vector4.zero);
                    vh.AddVert(c - across + thick, mark, Vector4.zero);
                    break;
                }

                case RiverWater.Clear:
                {
                    float sign = (index & 1) == 0 ? 1f : -1f;
                    Vector2 a = c - (dir * (PatternStepDp * 0.5f)) + (normal * (room * 0.45f * sign));
                    Vector2 z = c + (dir * (PatternStepDp * 0.5f)) - (normal * (room * 0.45f * sign));
                    Vector2 stroke = z - a;
                    Vector2 side = stroke.sqrMagnitude > 1e-6f ? new Vector2(-stroke.y, stroke.x).normalized * 0.35f : normal * 0.35f;
                    vh.AddVert(a - side, mark, Vector4.zero);
                    vh.AddVert(a + side, mark, Vector4.zero);
                    vh.AddVert(z + side, mark, Vector4.zero);
                    vh.AddVert(z - side, mark, Vector4.zero);
                    break;
                }

                default:
                {
                    float r = Mathf.Min(0.9f, room * 0.45f);
                    vh.AddVert(c - (dir * r), mark, Vector4.zero);
                    vh.AddVert(c + (normal * r), mark, Vector4.zero);
                    vh.AddVert(c + (dir * r), mark, Vector4.zero);
                    vh.AddVert(c - (normal * r), mark, Vector4.zero);
                    break;
                }
            }

            vh.AddTriangle(b, b + 1, b + 2);
            vh.AddTriangle(b + 2, b + 3, b);
            _markCount++;
            return true;
        }

        private Color32 MarkColorWithAlpha(RiverWater water)
        {
            Color c = MarkColor(water);
            c.a *= color.a;
            return c;
        }

        private RunLook LookAt(RiverLine line, float s, bool water)
        {
            if (!water || _look == RibbonLook.Glass)
            {
                return default;
            }

            if (line.TryGetSplitAt(s, out RiverSplit split))
            {
                return new RunLook(true, split.Left, split.Right);
            }

            return new RunLook(false, line.WaterAt(s), RiverWater.None);
        }

        // Normal izquierda (mirando hacia s creciente) en coordenadas locales de una dirección del marco.
        private static Vector2 Normal(Vector2 mapDirection)
        {
            Vector2 d = MapSpace.DirectionToLocal(mapDirection);
            return new Vector2(-d.y, d.x);
        }

        // Inglete entre las normales de llegada y de salida, en medios anchos, limitado a MiterLimit.
        private static Vector2 Miter(Vector2 nIn, Vector2 nOut)
        {
            Vector2 m = nIn + nOut;
            if (m.sqrMagnitude < 1e-6f)
            {
                return nOut; // media vuelta: sin inglete
            }

            m.Normalize();
            float cos = Vector2.Dot(m, nOut);
            return m * (1f / Mathf.Max(cos, 1f / MiterLimit));
        }

        // Aspecto de un tramo de la cinta: un agua o dos lado a lado (A a la izquierda y B a la derecha, mirando río abajo).
        private readonly struct RunLook
        {
            public RunLook(bool split, RiverWater a, RiverWater b)
            {
                Split = split;
                A = a;
                B = b;
            }

            public bool Split { get; }

            public RiverWater A { get; }

            public RiverWater B { get; }

            public bool Equals(RunLook other)
            {
                return Split == other.Split && A == other.A && B == other.B;
            }
        }
    }
}
