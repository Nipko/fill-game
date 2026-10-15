#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Malla de triángulos con color por vértice para el fondo de la parada (tarea M1.4; propuesta del Mapa §10.2 y §11.1;
    /// D-013, punto 9): el cielo con la hora del mundo, la banda de agua del primer plano, las piezas por código (las líneas entre
    /// aguas) y las siluetas provisionales de <see cref="StopShapes"/>. Sin textura ni shader propio: el material de UI por
    /// defecto y colores por vértice, como <see cref="BezierStripGraphic"/> y <see cref="WaterStripGraphic"/>.
    /// <para>Las coordenadas se dan en dp del lienzo con la y hacia abajo desde arriba (las de <c>GameScreen</c>): el
    /// RectTransform del gráfico ocupa el lienzo entero con el pivote arriba a la izquierda, así que el vértice (x, y) va en
    /// (x, −y) local. Los búferes se reservan una vez y crecen solo si hace falta, al construir la composición; volver a
    /// construir la misma composición no asigna memoria. No recibe toques (<c>raycastTarget</c> = false; VA-08).</para>
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StopShapeGraphic : MaskableGraphic
    {
        private Vector2[] _pos = new Vector2[64];
        private Color32[] _col = new Color32[64];
        private int[] _tri = new int[96];
        private int _vertexCount;
        private int _indexCount;
        private bool _hasRect;
        private float _minY;
        private float _maxY;
        private float _minX;
        private float _maxX;

        /// <summary>Configura el gráfico: sin raycast (VA-08) y vacío.</summary>
        public void Setup()
        {
            raycastTarget = false;
            Clear();
        }

        /// <summary>Vértices de la malla.</summary>
        public int VertexCount => _vertexCount;

        /// <summary>Triángulos de la malla.</summary>
        public int TriangleCount => _indexCount / 3;

        /// <summary>Veces que se generó la malla para el <c>CanvasRenderer</c> (<c>OnPopulateMesh</c>; tests).</summary>
        public int PopulateCount { get; private set; }

        /// <summary>La malla no tiene ningún triángulo.</summary>
        public bool IsEmpty => _indexCount == 0;

        /// <summary>Menor y de la malla (dp desde arriba; tests).</summary>
        public float MinY => _hasRect ? _minY : 0f;

        /// <summary>Mayor y de la malla (dp desde arriba; tests).</summary>
        public float MaxY => _hasRect ? _maxY : 0f;

        /// <summary>Menor x de la malla (dp; tests).</summary>
        public float MinX => _hasRect ? _minX : 0f;

        /// <summary>Mayor x de la malla (dp; tests).</summary>
        public float MaxX => _hasRect ? _maxX : 0f;

        /// <summary>Posición del vértice <paramref name="i"/> en dp del lienzo, con la y hacia abajo (tests).</summary>
        public Vector2 PositionAt(int i)
        {
            Vector2 p = _pos[i];
            return new Vector2(p.x, -p.y);
        }

        /// <summary>Índice del vértice <paramref name="i"/> de la lista de triángulos (de 3 en 3; tests).</summary>
        public int TriangleIndexAt(int i)
        {
            return _tri[i];
        }

        /// <summary>Color del vértice <paramref name="i"/> (tests).</summary>
        public Color ColorAt(int i)
        {
            return _col[i];
        }

        /// <summary>Vacía la malla (sin asignar; la marca para redibujar).</summary>
        public void Clear()
        {
            _vertexCount = 0;
            _indexCount = 0;
            _hasRect = false;
            SetVerticesDirty();
        }

        /// <summary>Marca la malla para redibujar tras añadir triángulos.</summary>
        public void Commit()
        {
            SetVerticesDirty();
        }

        /// <summary>Añade un vértice en (x, y) dp desde arriba y devuelve su índice.</summary>
        public int AddVertex(float x, float y, Color color)
        {
            if (_vertexCount == _pos.Length)
            {
                int size = _pos.Length * 2;
                System.Array.Resize(ref _pos, size);
                System.Array.Resize(ref _col, size);
            }

            _pos[_vertexCount] = new Vector2(x, -y);
            _col[_vertexCount] = color;
            if (!_hasRect)
            {
                _hasRect = true;
                _minX = x;
                _maxX = x;
                _minY = y;
                _maxY = y;
            }
            else
            {
                _minX = Mathf.Min(_minX, x);
                _maxX = Mathf.Max(_maxX, x);
                _minY = Mathf.Min(_minY, y);
                _maxY = Mathf.Max(_maxY, y);
            }

            return _vertexCount++;
        }

        /// <summary>Añade el triángulo de los vértices <paramref name="a"/>, <paramref name="b"/> y <paramref name="c"/>.</summary>
        public void AddTriangle(int a, int b, int c)
        {
            if (_indexCount + 3 > _tri.Length)
            {
                System.Array.Resize(ref _tri, _tri.Length * 2);
            }

            _tri[_indexCount++] = a;
            _tri[_indexCount++] = b;
            _tri[_indexCount++] = c;
        }

        /// <summary>Triángulo con un color por esquina (dp desde arriba).</summary>
        public void Triangle(Vector2 a, Color ca, Vector2 b, Color cb, Vector2 c, Color cc)
        {
            int ia = AddVertex(a.x, a.y, ca);
            int ib = AddVertex(b.x, b.y, cb);
            int ic = AddVertex(c.x, c.y, cc);
            AddTriangle(ia, ib, ic);
        }

        /// <summary>
        /// Cuadrilátero de esquinas arriba-izquierda, arriba-derecha, abajo-derecha y abajo-izquierda (dp desde arriba), con un
        /// color arriba y otro abajo (degradado vertical).
        /// </summary>
        public void Quad(Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl, Color top, Color bottom)
        {
            int a = AddVertex(tl.x, tl.y, top);
            int b = AddVertex(tr.x, tr.y, top);
            int c = AddVertex(br.x, br.y, bottom);
            int d = AddVertex(bl.x, bl.y, bottom);
            AddTriangle(a, b, c);
            AddTriangle(a, c, d);
        }

        /// <summary>Rectángulo de x0 a x1 y de y0 (arriba) a y1 (abajo), en dp desde arriba, con degradado vertical.</summary>
        public void Rect(float x0, float y0, float x1, float y1, Color top, Color bottom)
        {
            Quad(new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1), top, bottom);
        }

        /// <inheritdoc />
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            PopulateCount++;
            vh.Clear();
            UIVertex v = UIVertex.simpleVert;
            for (int i = 0; i < _vertexCount; i++)
            {
                v.position = _pos[i];
                v.color = _col[i];
                vh.AddVert(v);
            }

            for (int i = 0; i + 2 < _indexCount; i += 3)
            {
                vh.AddTriangle(_tri[i], _tri[i + 1], _tri[i + 2]);
            }
        }
    }
}
