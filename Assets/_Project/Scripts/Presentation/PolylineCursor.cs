#nullable enable
using System;
using AF.Levels;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Paso de las unidades del marco del Mapa (W = 1, y hacia abajo; <see cref="RiverLine.Xy"/>) a coordenadas locales de uGUI
    /// (dp, y hacia arriba): local = <see cref="Origin"/> + (x · <see cref="DpPerUnit"/>, −y · <see cref="DpPerUnit"/>). Con
    /// <see cref="DpPerUnit"/> = 328, la vista Cuenca a 1× (360 dp menos 2 × 16; propuesta del Mapa §3.1-§3.2). Sin asignaciones.
    /// </summary>
    public readonly struct MapSpace : IEquatable<MapSpace>
    {
        /// <summary>Ancho útil de la vista Cuenca a 1× (dp; propuesta del Mapa §3.1).</summary>
        public const float BasinWidthDp = 328f;

        /// <summary>Crea el espacio.</summary>
        public MapSpace(float dpPerUnit, Vector2 origin)
        {
            DpPerUnit = dpPerUnit;
            Origin = origin;
        }

        /// <summary>dp por unidad del marco (el ancho del Mapa en dp).</summary>
        public float DpPerUnit { get; }

        /// <summary>Posición local de la esquina de arriba a la izquierda del marco (los Andes, al sur).</summary>
        public Vector2 Origin { get; }

        /// <summary>Punto del marco en coordenadas locales.</summary>
        public Vector2 ToLocal(Vector2 map)
        {
            return new Vector2(Origin.x + (map.x * DpPerUnit), Origin.y - (map.y * DpPerUnit));
        }

        /// <summary>Punto del marco (x, y) en coordenadas locales.</summary>
        public Vector2 ToLocal(float x, float y)
        {
            return new Vector2(Origin.x + (x * DpPerUnit), Origin.y - (y * DpPerUnit));
        }

        /// <summary>Dirección del marco en coordenadas locales (misma escala en los dos ejes: solo invierte y).</summary>
        public static Vector2 DirectionToLocal(Vector2 map)
        {
            return new Vector2(map.x, -map.y);
        }

        /// <inheritdoc />
        public bool Equals(MapSpace other)
        {
            return DpPerUnit == other.DpPerUnit && Origin == other.Origin;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is MapSpace other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (DpPerUnit.GetHashCode() * 397) ^ Origin.GetHashCode();
        }
    }

    /// <summary>
    /// Cursor sobre una polilínea del Mapa del río (08 §2.2; propuesta del Mapa §11.1 y §11.4): posición y tangente por longitud
    /// de arco o por s (la fracción de la longitud, como en <c>af-river-map/1</c>), con búsqueda binaria sobre la longitud
    /// acumulada (<see cref="RiverLine.Cumulative"/>). Lo usan la canoa, Rosa, la cámara y las cintas
    /// (<see cref="RiverRibbonGraphic"/>). Trabaja en unidades del marco (W = 1, y hacia abajo); <see cref="MapSpace"/> las pasa
    /// a dp. Monotónico: con s creciente, el tramo (<see cref="Locate"/>) y la posición dentro de él nunca retroceden. Recorre los
    /// arreglos de la línea sin copiarlos: tras construirlo, ninguna consulta asigna memoria.
    /// </summary>
    public sealed class PolylineCursor
    {
        private readonly float[] _xy;
        private readonly float[] _cumulative;
        private readonly int _count;
        private readonly float _length;

        /// <summary>Cursor sobre la línea <paramref name="line"/>.</summary>
        public PolylineCursor(RiverLine line)
            : this(line.Xy, line.Cumulative)
        {
        }

        /// <summary>
        /// Cursor sobre <paramref name="xy"/> (x0, y0, x1, y1…) con su longitud acumulada <paramref name="cumulative"/> (un valor
        /// por punto, el primero 0, sin decrecer). No copia los arreglos.
        /// </summary>
        public PolylineCursor(float[] xy, float[] cumulative)
        {
            if (cumulative.Length == 0 || xy.Length != 2 * cumulative.Length)
            {
                throw new ArgumentException("xy lleva dos valores por punto y al menos un punto.", nameof(xy));
            }

            _xy = xy;
            _cumulative = cumulative;
            _count = cumulative.Length;
            _length = cumulative[_count - 1];
        }

        /// <summary>Puntos de la polilínea.</summary>
        public int PointCount => _count;

        /// <summary>Longitud en unidades del marco (0 con un solo punto).</summary>
        public float Length => _length;

        /// <summary>Punto <paramref name="i"/> (unidades del marco).</summary>
        public Vector2 Point(int i)
        {
            return new Vector2(_xy[2 * i], _xy[(2 * i) + 1]);
        }

        /// <summary>Longitud acumulada hasta el punto <paramref name="i"/>.</summary>
        public float ArcOfPoint(int i)
        {
            return _cumulative[i];
        }

        /// <summary>Longitud de arco de <paramref name="s"/> (recortada a [0, 1]).</summary>
        public float ArcOfS(float s)
        {
            return Mathf.Clamp01(s) * _length;
        }

        /// <summary>
        /// Tramo que contiene la longitud de arco <paramref name="arc"/> (recortada a [0, <see cref="Length"/>]): el mayor
        /// <c>i</c> en [0, n − 2] con <c>Cumulative[i] ≤ arc</c>, por búsqueda binaria, y la fracción <paramref name="t"/> dentro
        /// de él. Con un solo punto, 0 y t = 0.
        /// </summary>
        public int Locate(float arc, out float t)
        {
            if (_count < 2 || _length <= 0f)
            {
                t = 0f;
                return 0;
            }

            if (arc <= 0f)
            {
                t = 0f;
                return 0;
            }

            if (arc >= _length)
            {
                t = 1f;
                return _count - 2;
            }

            int lo = 0;
            int hi = _count - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (_cumulative[mid] <= arc)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            float d = _cumulative[lo + 1] - _cumulative[lo];
            t = d > 0f ? Mathf.Clamp01((arc - _cumulative[lo]) / d) : 0f;
            return lo;
        }

        /// <summary>Punto a la longitud de arco <paramref name="arc"/> (unidades del marco).</summary>
        public Vector2 PointAtArc(float arc)
        {
            int i = Locate(arc, out float t);
            return PointOn(i, t);
        }

        /// <summary>Punto en <paramref name="s"/> (unidades del marco).</summary>
        public Vector2 PointAtS(float s)
        {
            return PointAtArc(ArcOfS(s));
        }

        /// <summary>Tangente unitaria (hacia s creciente, río abajo) a la longitud de arco <paramref name="arc"/>.</summary>
        public Vector2 TangentAtArc(float arc)
        {
            return SegmentDirection(Locate(arc, out _));
        }

        /// <summary>Tangente unitaria (hacia s creciente) en <paramref name="s"/>.</summary>
        public Vector2 TangentAtS(float s)
        {
            return TangentAtArc(ArcOfS(s));
        }

        /// <summary>Punto y tangente en <paramref name="s"/>, con una sola búsqueda.</summary>
        public void Sample(float s, out Vector2 point, out Vector2 tangent)
        {
            int i = Locate(ArcOfS(s), out float t);
            point = PointOn(i, t);
            tangent = SegmentDirection(i);
        }

        /// <summary>
        /// Dirección unitaria del tramo <paramref name="segment"/> (del punto i al i + 1). Si el tramo no tiene largo (dos puntos
        /// iguales al cuantizar), la del tramo con largo más cercano, primero hacia delante; sin ninguno, (0, 1): río abajo.
        /// </summary>
        public Vector2 SegmentDirection(int segment)
        {
            if (_count >= 2)
            {
                segment = Mathf.Clamp(segment, 0, _count - 2);
                for (int k = segment; k < _count - 1; k++)
                {
                    if (TryDirection(k, out Vector2 d))
                    {
                        return d;
                    }
                }

                for (int k = segment - 1; k >= 0; k--)
                {
                    if (TryDirection(k, out Vector2 d))
                    {
                        return d;
                    }
                }
            }

            return new Vector2(0f, 1f);
        }

        /// <summary>
        /// Dirección de llegada al punto <paramref name="i"/> (la del tramo con largo anterior más cercano) y de salida (la del
        /// siguiente), para las uniones en inglete. En los extremos, las dos son la del único tramo.
        /// </summary>
        public void DirectionsAt(int i, out Vector2 incoming, out Vector2 outgoing)
        {
            incoming = new Vector2(0f, 1f);
            outgoing = incoming;
            bool hasIn = false;
            for (int k = i - 1; k >= 0 && !hasIn; k--)
            {
                if (TryDirection(k, out Vector2 d))
                {
                    incoming = d;
                    hasIn = true;
                }
            }

            bool hasOut = false;
            for (int k = i; k < _count - 1 && !hasOut; k++)
            {
                if (TryDirection(k, out Vector2 d))
                {
                    outgoing = d;
                    hasOut = true;
                }
            }

            if (!hasIn)
            {
                incoming = outgoing; // primer punto (o sin tramos con largo: los dos quedan en (0, 1))
            }

            if (!hasOut)
            {
                outgoing = incoming; // último punto
            }
        }

        private Vector2 PointOn(int i, float t)
        {
            if (_count < 2)
            {
                return new Vector2(_xy[0], _xy[1]);
            }

            float x0 = _xy[2 * i];
            float y0 = _xy[(2 * i) + 1];
            return new Vector2(x0 + ((_xy[(2 * i) + 2] - x0) * t), y0 + ((_xy[(2 * i) + 3] - y0) * t));
        }

        private bool TryDirection(int segment, out Vector2 direction)
        {
            float dx = _xy[(2 * segment) + 2] - _xy[2 * segment];
            float dy = _xy[(2 * segment) + 3] - _xy[(2 * segment) + 1];
            float len = Mathf.Sqrt((dx * dx) + (dy * dy));
            if (len <= 1e-9f)
            {
                direction = default;
                return false;
            }

            direction = new Vector2(dx / len, dy / len);
            return true;
        }
    }
}
