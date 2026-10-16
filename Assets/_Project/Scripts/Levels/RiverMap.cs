#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Levels
{
    /// <summary>Clase de línea del Mapa del río (08 §4.5, campo <c>lines[].kind</c>).</summary>
    public enum RiverLineKind
    {
        /// <summary><c>main</c>: el cauce y el ramal del Pará.</summary>
        Main = 0,

        /// <summary><c>tributary</c>: afluente de un mundo.</summary>
        Tributary = 1,

        /// <summary><c>passby</c>: afluente de paso, que se llena entero al pasar su boca.</summary>
        Passby = 2,

        /// <summary><c>local</c>: río local de una parada (solo en la vista Mundo).</summary>
        Local = 3,
    }

    /// <summary>
    /// Tipo de agua de un tramo (08 §4.5, <c>water[].type</c> y los lados de <c>splits[]</c>): las de Sioli y la andina [P] de la
    /// cabecera del cauce. <see cref="None"/> es un tramo sin agua comprobada ([NV]), que se dibuja solo en vidrio.
    /// </summary>
    public enum RiverWater
    {
        /// <summary>Sin agua comprobada: solo vidrio.</summary>
        None = 0,

        /// <summary><c>white</c>: agua blanca.</summary>
        White = 1,

        /// <summary><c>black</c>: agua negra.</summary>
        Black = 2,

        /// <summary><c>clear</c>: agua clara.</summary>
        Clear = 3,

        /// <summary><c>andean</c>: agua andina [P], en la cabecera del cauce.</summary>
        Andean = 4,
    }

    /// <summary>Tramo de agua de una línea en s ∈ [<see cref="S0"/>, <see cref="S1"/>) (08 §4.5, <c>water[]</c>).</summary>
    public readonly struct RiverWaterSpan
    {
        /// <summary>Crea el tramo.</summary>
        public RiverWaterSpan(float s0, float s1, RiverWater water)
        {
            S0 = s0;
            S1 = s1;
            Water = water;
        }

        /// <summary>Inicio (fracción de la longitud de la línea, desde su primer punto).</summary>
        public float S0 { get; }

        /// <summary>Fin.</summary>
        public float S1 { get; }

        /// <summary>Agua del tramo.</summary>
        public RiverWater Water { get; }
    }

    /// <summary>
    /// Dos aguas lado a lado en s ∈ [<see cref="S0"/>, <see cref="S1"/>) (08 §4.5, <c>splits[]</c>; propuesta del Mapa §3.4): la
    /// cinta se parte a lo largo, con el agua de cada orilla mirando río abajo (s creciente).
    /// </summary>
    public readonly struct RiverSplit
    {
        /// <summary>Crea la franja.</summary>
        public RiverSplit(string id, float s0, float s1, RiverWater left, RiverWater right)
        {
            Id = id;
            S0 = s0;
            S1 = s1;
            Left = left;
            Right = right;
        }

        /// <summary>Id de la franja (<c>encontro</c>, <c>santarem</c>).</summary>
        public string Id { get; }

        /// <summary>Inicio.</summary>
        public float S0 { get; }

        /// <summary>Fin.</summary>
        public float S1 { get; }

        /// <summary>Agua de la orilla izquierda, mirando río abajo.</summary>
        public RiverWater Left { get; }

        /// <summary>Agua de la orilla derecha, mirando río abajo.</summary>
        public RiverWater Right { get; }
    }

    /// <summary>Un punto sobre una línea del Mapa: el índice de la línea en <see cref="RiverMap.Lines"/> y su s.</summary>
    public readonly struct RiverMapPlace
    {
        /// <summary>Crea el punto.</summary>
        public RiverMapPlace(int line, float s)
        {
            Line = line;
            S = s;
        }

        /// <summary>Índice de la línea en <see cref="RiverMap.Lines"/>.</summary>
        public int Line { get; }

        /// <summary>Fracción de la longitud de la línea, desde su primer punto (crece río abajo).</summary>
        public float S { get; }
    }

    /// <summary>
    /// Marco del Mapa (08 §4.5, <c>frame</c>): equirrectangular con el factor cos(lat0), girada 90° en el sentido de las agujas
    /// del reloj y sin espejo (Andes arriba, Atlántico abajo y norte a la derecha; 06 §4.5). Un punto uint16 (X, Y) queda en
    /// pantalla, con un ancho W = 1, en (X / 65535, Y / 65535 · <see cref="Aspect"/>): la misma escala en los dos ejes.
    /// </summary>
    public sealed class RiverMapFrame
    {
        /// <summary>Crea el marco (el lector ya lo validó).</summary>
        public RiverMapFrame(double latMin, double latMax, double lonMin, double lonMax, double lat0)
        {
            LatMin = latMin;
            LatMax = latMax;
            LonMin = lonMin;
            LonMax = lonMax;
            Lat0 = lat0;
            Aspect = (lonMax - lonMin) * Math.Cos(lat0 * Math.PI / 180.0) / (latMax - latMin);
        }

        /// <summary>Latitud mínima de la caja (borde izquierdo, el sur).</summary>
        public double LatMin { get; }

        /// <summary>Latitud máxima de la caja (borde derecho, el norte).</summary>
        public double LatMax { get; }

        /// <summary>Longitud mínima de la caja (borde de arriba, los Andes).</summary>
        public double LonMin { get; }

        /// <summary>Longitud máxima de la caja (borde de abajo, el Atlántico).</summary>
        public double LonMax { get; }

        /// <summary>Latitud del factor cos(lat0).</summary>
        public double Lat0 { get; }

        /// <summary>Alto / ancho del marco: (lon_max − lon_min) · cos(lat0) / (lat_max − lat_min) (1,31498 en la 1.0).</summary>
        public double Aspect { get; }
    }

    /// <summary>
    /// Una línea del Mapa (08 §4.5, <c>lines[]</c>), decodificada una sola vez: sus puntos, de la cabecera a la boca, en unidades
    /// del marco con W = 1 (<see cref="Xy"/>: x en [0, 1], y en [0, <see cref="RiverMapFrame.Aspect"/>], y hacia abajo) y su
    /// longitud acumulada (<see cref="Cumulative"/>), con la que s = <c>Cumulative[i] / Length</c> es la misma que mide
    /// <c>tools/river-map/geometry.py</c>. Los arreglos son de solo lectura por contrato: <c>PolylineCursor</c> y las cintas
    /// los recorren sin copiarlos. Las consultas (<see cref="WaterAt"/>, <see cref="TryGetSplitAt"/>…) no asignan memoria.
    /// </summary>
    public sealed class RiverLine
    {
        private readonly float[] _xy;
        private readonly float[] _cumulative;
        private readonly RiverWaterSpan[] _water;
        private readonly RiverSplit[] _splits;

        /// <summary>Crea la línea (el lector ya la validó y la decodificó).</summary>
        public RiverLine(int index, string id, RiverLineKind kind, int lineClass, string source, float[] xy, float[] cumulative,
            RiverWaterSpan[] water, RiverSplit[] splits, RiverMapPlace? mouth, float fillOnPass)
        {
            if (xy.Length != 2 * cumulative.Length || cumulative.Length == 0)
            {
                throw new ArgumentException("xy lleva dos valores por punto y al menos un punto.", nameof(xy));
            }

            Index = index;
            Id = id;
            Kind = kind;
            Class = lineClass;
            Source = source;
            _xy = xy;
            _cumulative = cumulative;
            _water = water;
            _splits = splits;
            Mouth = mouth;
            FillOnPass = fillOnPass;
        }

        /// <summary>Posición en <see cref="RiverMap.Lines"/>.</summary>
        public int Index { get; }

        /// <summary>Id de la línea (<c>[a-z0-9_]+</c>).</summary>
        public string Id { get; }

        /// <summary>Clase de línea.</summary>
        public RiverLineKind Kind { get; }

        /// <summary>Grosor [P]: 1 el cauce y el ramal, 2 los afluentes, 3 los locales.</summary>
        public int Class { get; }

        /// <summary>Origen de la geometría: <c>rio_amazonas</c>, <c>ne10m</c> o <c>hydrorivers</c>.</summary>
        public string Source { get; }

        /// <summary>Puntos (1 si de la línea solo se conoce la boca).</summary>
        public int PointCount => _cumulative.Length;

        /// <summary>Longitud en unidades del marco (W = 1); 0 con un solo punto.</summary>
        public float Length => _cumulative[_cumulative.Length - 1];

        /// <summary>x0, y0, x1, y1… en unidades del marco (W = 1, y hacia abajo). No lo modifiques.</summary>
        public float[] Xy => _xy;

        /// <summary>Longitud acumulada hasta cada punto (la del primero es 0). No lo modifiques.</summary>
        public float[] Cumulative => _cumulative;

        /// <summary>Tramos de agua en orden de s, seguidos de 0 a 1; vacío si el agua está sin comprobar ([NV]).</summary>
        public IReadOnlyList<RiverWaterSpan> Water => _water;

        /// <summary>Franjas de dos aguas en orden de s, sin solaparse (solo en las líneas <c>main</c>).</summary>
        public IReadOnlyList<RiverSplit> Splits => _splits;

        /// <summary>Boca: la línea anfitriona y su s; null en las líneas <c>main</c>.</summary>
        public RiverMapPlace? Mouth { get; }

        /// <summary>Fracción que se llena desde la boca al pasarla; 0 en las líneas <c>main</c>.</summary>
        public float FillOnPass { get; }

        /// <summary>x del punto <paramref name="i"/> (unidades del marco).</summary>
        public float X(int i)
        {
            return _xy[2 * i];
        }

        /// <summary>y del punto <paramref name="i"/> (unidades del marco, hacia abajo).</summary>
        public float Y(int i)
        {
            return _xy[(2 * i) + 1];
        }

        /// <summary>s del punto <paramref name="i"/> (0 en el primero y 1 en el último; 0 si la línea no tiene longitud).</summary>
        public float SOfPoint(int i)
        {
            float length = Length;
            return length > 0f ? _cumulative[i] / length : 0f;
        }

        /// <summary>
        /// Punto en <paramref name="s"/> (recortada a [0, 1]), en unidades del marco, por búsqueda binaria sobre
        /// <see cref="Cumulative"/>; con un solo punto, ese punto. Sin asignaciones. (En la presentación, <c>PolylineCursor</c>.)
        /// </summary>
        public void PointAt(float s, out float x, out float y)
        {
            int n = _cumulative.Length;
            float length = Length;
            if (n < 2 || length <= 0f)
            {
                x = _xy[0];
                y = _xy[1];
                return;
            }

            float arc = (s <= 0f ? 0f : s >= 1f ? 1f : s) * length;
            int lo = 0;
            int hi = n - 1;
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
            float t = d > 0f ? (arc - _cumulative[lo]) / d : 0f;
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            x = _xy[2 * lo] + ((_xy[(2 * lo) + 2] - _xy[2 * lo]) * t);
            y = _xy[(2 * lo) + 1] + ((_xy[(2 * lo) + 3] - _xy[(2 * lo) + 1]) * t);
        }

        /// <summary>
        /// Agua en <paramref name="s"/> (tramos semiabiertos [s0, s1), el último incluye el 1); <see cref="RiverWater.None"/> si
        /// la línea no tiene tramos. Sin asignaciones.
        /// </summary>
        public RiverWater WaterAt(float s)
        {
            for (int i = 0; i < _water.Length; i++)
            {
                // Los tramos son seguidos desde 0 (lo valida el lector): el primero cuyo fin queda después de s.
                if (s < _water[i].S1 || i == _water.Length - 1)
                {
                    return _water[i].Water;
                }
            }

            return RiverWater.None;
        }

        /// <summary>Franja de dos aguas que cubre <paramref name="s"/> (semiabierta [s0, s1)), si la hay. Sin asignaciones.</summary>
        public bool TryGetSplitAt(float s, out RiverSplit split)
        {
            for (int i = 0; i < _splits.Length; i++)
            {
                if (s >= _splits[i].S0 && s < _splits[i].S1)
                {
                    split = _splits[i];
                    return true;
                }
            }

            split = default;
            return false;
        }

        /// <summary>
        /// Primer cambio de agua o de franja estrictamente después de <paramref name="s"/> (un <c>s1</c> de <c>water</c> o un
        /// borde de <c>splits</c>), o 2 si no hay más. Sin asignaciones.
        /// </summary>
        public float NextBoundaryAfter(float s)
        {
            float next = 2f;
            for (int i = 0; i < _water.Length - 1; i++)
            {
                float b = _water[i].S1;
                if (b > s && b < next)
                {
                    next = b;
                }
            }

            for (int i = 0; i < _splits.Length; i++)
            {
                if (_splits[i].S0 > s && _splits[i].S0 < next)
                {
                    next = _splits[i].S0;
                }

                if (_splits[i].S1 > s && _splits[i].S1 < next)
                {
                    next = _splits[i].S1;
                }
            }

            return next;
        }
    }

    /// <summary>Zona del terreno (08 §4.5, <c>land.zones[]</c>): sus triángulos seguidos y su mosaico [P].</summary>
    public sealed class RiverLandZone
    {
        /// <summary>Crea la zona.</summary>
        public RiverLandZone(string id, string tile, int first, int count)
        {
            Id = id;
            Tile = tile;
            First = first;
            Count = count;
        }

        /// <summary>Id de la zona.</summary>
        public string Id { get; }

        /// <summary>Mosaico del terreno [P] (en la 1.0, <c>canopy</c>).</summary>
        public string Tile { get; }

        /// <summary>Primer triángulo de la zona.</summary>
        public int First { get; }

        /// <summary>Triángulos de la zona, seguidos desde <see cref="First"/>.</summary>
        public int Count { get; }
    }

    /// <summary>
    /// Tierra del Mapa (08 §4.5, <c>land</c>), ya triangulada por la herramienta: vértices en unidades del marco (como
    /// <see cref="RiverLine.Xy"/>) y triángulos como índices de tres en tres. Los arreglos son de solo lectura por contrato.
    /// </summary>
    public sealed class RiverLand
    {
        private readonly float[] _xy;
        private readonly int[] _triangles;
        private readonly RiverLandZone[] _zones;

        /// <summary>Crea la tierra (el lector ya la validó).</summary>
        public RiverLand(float[] xy, int[] triangles, RiverLandZone[] zones)
        {
            _xy = xy;
            _triangles = triangles;
            _zones = zones;
        }

        /// <summary>Vértices.</summary>
        public int VertexCount => _xy.Length / 2;

        /// <summary>Triángulos.</summary>
        public int TriangleCount => _triangles.Length / 3;

        /// <summary>x0, y0, x1, y1… en unidades del marco (W = 1, y hacia abajo). No lo modifiques.</summary>
        public float[] Xy => _xy;

        /// <summary>Índices de los triángulos, de tres en tres. No lo modifiques.</summary>
        public int[] Triangles => _triangles;

        /// <summary>Zonas, que cubren todos los triángulos una sola vez.</summary>
        public IReadOnlyList<RiverLandZone> Zones => _zones;
    }

    /// <summary>Recuadro de un río de fuera de la cuenca (08 §4.5, <c>insets[]</c>); la 1.0 los deja vacíos.</summary>
    public sealed class RiverInset
    {
        private readonly int[] _lines;

        /// <summary>Crea el recuadro.</summary>
        public RiverInset(string id, float x0, float y0, float x1, float y1, int[] lines, int link)
        {
            Id = id;
            X0 = x0;
            Y0 = y0;
            X1 = x1;
            Y1 = y1;
            _lines = lines;
            Link = link;
        }

        /// <summary>Id del recuadro.</summary>
        public string Id { get; }

        /// <summary>Borde izquierdo (fracción del ancho).</summary>
        public float X0 { get; }

        /// <summary>Borde de arriba (fracción del alto).</summary>
        public float Y0 { get; }

        /// <summary>Borde derecho.</summary>
        public float X1 { get; }

        /// <summary>Borde de abajo.</summary>
        public float Y1 { get; }

        /// <summary>Índices de las líneas que dibuja.</summary>
        public IReadOnlyList<int> Lines => _lines;

        /// <summary>Índice de la línea del Mapa con la que enlaza.</summary>
        public int Link { get; }
    }

    /// <summary>Rótulo de un país en su región (08 §4.5, <c>labels[]</c>; texto <c>map.country.&lt;iso2&gt;.name</c>).</summary>
    public sealed class RiverMapLabel
    {
        /// <summary>Crea el rótulo.</summary>
        public RiverMapLabel(string country, float x, float y)
        {
            Country = country;
            X = x;
            Y = y;
            NameKey = "map.country." + country.ToLowerInvariant() + ".name";
        }

        /// <summary>ISO 3166-1 alfa-2, en mayúsculas.</summary>
        public string Country { get; }

        /// <summary>x en unidades del marco.</summary>
        public float X { get; }

        /// <summary>y en unidades del marco (hacia abajo).</summary>
        public float Y { get; }

        /// <summary>Clave del nombre del país, por convención (08 §4.5).</summary>
        public string NameKey { get; }
    }

    /// <summary>Mirador (08 §4.5, <c>viewpoints[]</c>): un «?» fuera de la ruta que revela la victoria de un nivel.</summary>
    public sealed class RiverViewpoint
    {
        /// <summary>Crea el mirador.</summary>
        public RiverViewpoint(string id, float x, float y, int revealOn, string? fact)
        {
            Id = id;
            X = x;
            Y = y;
            RevealOn = revealOn;
            Fact = fact;
            NameKey = "map.stop." + id + ".name";
        }

        /// <summary>Id del mirador.</summary>
        public string Id { get; }

        /// <summary>x en unidades del marco.</summary>
        public float X { get; }

        /// <summary>y en unidades del marco (hacia abajo).</summary>
        public float Y { get; }

        /// <summary>Nivel cuya victoria lo revela.</summary>
        public int RevealOn { get; }

        /// <summary>Clave del dato que enseña al tocarlo, o null.</summary>
        public string? Fact { get; }

        /// <summary>Clave del nombre, por convención: <c>map.stop.&lt;id&gt;.name</c>.</summary>
        public string NameKey { get; }
    }

    /// <summary>Punto de paso (08 §4.5, <c>passing[]</c>): un nombre en el río que sale al pasar durante un viaje.</summary>
    public sealed class RiverPassingPoint
    {
        /// <summary>Crea el punto de paso.</summary>
        public RiverPassingPoint(string id, RiverMapPlace at, string? fact)
        {
            Id = id;
            At = at;
            Fact = fact;
            NameKey = "map.stop." + id + ".name";
        }

        /// <summary>Id del punto de paso (lo citan los <c>leg.via</c> de las paradas).</summary>
        public string Id { get; }

        /// <summary>Dónde está.</summary>
        public RiverMapPlace At { get; }

        /// <summary>Clave de su dato, o null.</summary>
        public string? Fact { get; }

        /// <summary>Clave del nombre, por convención: <c>map.stop.&lt;id&gt;.name</c>.</summary>
        public string NameKey { get; }
    }

    /// <summary>
    /// Geometría del Mapa del río (<c>af-river-map/1</c>; 08 §2.2 y §4.5; contrato 29; esquema
    /// <c>schemas/af-river-map-1.schema.json</c>), sin motor: la lee, la valida y la decodifica una sola vez
    /// <see cref="RiverMapReader"/>. Las búsquedas por id (<see cref="LineIndexOf"/>…) no asignan memoria. Inmutable.
    /// </summary>
    public sealed class RiverMap
    {
        /// <summary>Formato que lee este cliente.</summary>
        public const string Format = "af-river-map/1";

        private readonly RiverLine[] _lines;
        private readonly RiverInset[] _insets;
        private readonly RiverMapLabel[] _labels;
        private readonly RiverViewpoint[] _viewpoints;
        private readonly RiverPassingPoint[] _passing;

        /// <summary>Crea el Mapa (el lector ya lo validó).</summary>
        public RiverMap(RiverMapFrame frame, RiverLine[] lines, RiverLand land, RiverInset[] insets, RiverMapPlace home,
            RiverMapLabel[] labels, RiverViewpoint[] viewpoints, RiverPassingPoint[] passing)
        {
            Frame = frame;
            _lines = lines;
            Land = land;
            _insets = insets;
            Home = home;
            _labels = labels;
            _viewpoints = viewpoints;
            _passing = passing;
        }

        /// <summary>Marco y proyección.</summary>
        public RiverMapFrame Frame { get; }

        /// <summary>Ancho del Mapa en unidades del marco (1).</summary>
        public float Width => 1f;

        /// <summary>Alto del Mapa en unidades del marco (<see cref="RiverMapFrame.Aspect"/>).</summary>
        public float Height => (float)Frame.Aspect;

        /// <summary>Ríos, en el orden del archivo (el cauce primero).</summary>
        public IReadOnlyList<RiverLine> Lines => _lines;

        /// <summary>Tierra triangulada.</summary>
        public RiverLand Land { get; }

        /// <summary>Recuadros (vacíos en la 1.0).</summary>
        public IReadOnlyList<RiverInset> Insets => _insets;

        /// <summary>Casa de TiTino, en la orilla de la bahía de Guajará.</summary>
        public RiverMapPlace Home { get; }

        /// <summary>Rótulos de los países.</summary>
        public IReadOnlyList<RiverMapLabel> Labels => _labels;

        /// <summary>Miradores.</summary>
        public IReadOnlyList<RiverViewpoint> Viewpoints => _viewpoints;

        /// <summary>Puntos de paso.</summary>
        public IReadOnlyList<RiverPassingPoint> Passing => _passing;

        /// <summary>Índice de la línea <paramref name="id"/>, o −1. Sin asignaciones.</summary>
        public int LineIndexOf(string id)
        {
            for (int i = 0; i < _lines.Length; i++)
            {
                if (string.Equals(_lines[i].Id, id, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Línea <paramref name="id"/>, o null. Sin asignaciones.</summary>
        public RiverLine? Line(string id)
        {
            int i = LineIndexOf(id);
            return i >= 0 ? _lines[i] : null;
        }

        /// <summary>Punto de paso <paramref name="id"/>, o null. Sin asignaciones.</summary>
        public RiverPassingPoint? PassingPoint(string id)
        {
            for (int i = 0; i < _passing.Length; i++)
            {
                if (string.Equals(_passing[i].Id, id, StringComparison.Ordinal))
                {
                    return _passing[i];
                }
            }

            return null;
        }
    }

    /// <summary>Un tramo de la ruta de una parada: de s0 a s1 sobre una línea (s1 &lt; s0 si se remonta).</summary>
    public readonly struct RiverPathPiece
    {
        /// <summary>Crea el tramo.</summary>
        public RiverPathPiece(int line, float s0, float s1)
        {
            Line = line;
            S0 = s0;
            S1 = s1;
        }

        /// <summary>Índice de la línea en <see cref="RiverMap.Lines"/>.</summary>
        public int Line { get; }

        /// <summary>Donde empieza.</summary>
        public float S0 { get; }

        /// <summary>Donde acaba.</summary>
        public float S1 { get; }
    }

    /// <summary>
    /// Ruta de la canoa hasta una parada (los campos <c>at</c> y <c>leg</c> de <c>af-river-stops/1</c>; 08 §4.5): dónde está la
    /// parada, los tramos por agua desde la anterior (con ida y vuelta por las confluencias), los puntos de paso y las bocas que
    /// pasa. Inmutable.
    /// </summary>
    public sealed class RiverStopRoute
    {
        private readonly RiverPathPiece[] _path;
        private readonly string[] _via;
        private readonly int[] _mouths;

        /// <summary>Crea la ruta (el lector ya la validó contra el Mapa).</summary>
        public RiverStopRoute(int n, string id, RiverMapPlace at, RiverPathPiece[] path, string[] via, int[] mouths)
        {
            N = n;
            Id = id;
            At = at;
            _path = path;
            _via = via;
            _mouths = mouths;
        }

        /// <summary>k de la parada.</summary>
        public int N { get; }

        /// <summary>Id de la parada.</summary>
        public string Id { get; }

        /// <summary>Dónde está la parada.</summary>
        public RiverMapPlace At { get; }

        /// <summary>Tramos desde la parada anterior (en n = 0, un tramo sin largo sobre la propia parada).</summary>
        public IReadOnlyList<RiverPathPiece> Path => _path;

        /// <summary>Ids de los puntos de paso del tramo, en orden.</summary>
        public IReadOnlyList<string> Via => _via;

        /// <summary>Índices de las líneas cuyas bocas pasa el tramo.</summary>
        public IReadOnlyList<int> Mouths => _mouths;
    }

    /// <summary>Rutas de todas las paradas, en orden de n (las lee <see cref="RiverMapReader.ReadRoutes"/>). Inmutable.</summary>
    public sealed class RiverRoutes
    {
        private readonly RiverStopRoute[] _routes;

        /// <summary>Crea la tabla.</summary>
        public RiverRoutes(RiverStopRoute[] routes)
        {
            _routes = routes;
        }

        /// <summary>Paradas con ruta.</summary>
        public int Count => _routes.Length;

        /// <summary>Ruta de la parada k = <paramref name="n"/>, o null. Sin asignaciones.</summary>
        public RiverStopRoute? At(int n)
        {
            return n >= 0 && n < _routes.Length ? _routes[n] : null;
        }
    }
}
