#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Los marcadores del Mapa del río (tarea M3.5; D-013, puntos 3 y 6: Q3-A, Q17-A y Q18-A; 06 §4.5; propuesta del Mapa §2.2,
    /// §3.2, §3.3, §4.1, §6.1 y §12), parcial de <see cref="RiverMapScreen"/> sobre sus ganchos <c>Markers*</c>. Todo sale de
    /// <c>max_level</c>, del catálogo de paradas y de la geometría, sin estado guardado:
    /// <list type="bullet">
    /// <item><description><b>La canoa de Capi con sus banderas</b>: en el mástil de popa, las banderas de cortesía izadas hasta
    /// ahora (el <c>flag_raise</c> de cada parada alcanzada, sin repetir, la más reciente arriba: BR desde Belém, CO desde el 80 y PE
    /// desde el 140; propuesta §7: «las izadas se guardan en la lista de la canoa»; <see cref="CanoeFlags"/>). Solo banderas
    /// oficiales o, en el Editor y en desarrollo, las simplificadas (<see cref="ProceduralArt.MapFlag"/>, P-M5); sin ninguna, sin
    /// mástil. La proa mira hacia donde va.</description></item>
    /// <item><description><b>El guía</b> (<see cref="RiverJourney.GuideFor"/>; P-M8): Rosa nada <see cref="GuideLeadDp"/> dp por
    /// delante de la canoa por la ruta (y, en la parada, por la ruta siguiente); en los mundos 7 y 8, el bufeo boliviano
    /// (<see cref="ProceduralArt.MapBufeo"/>), con el relevo en los rápidos de Teotônio (<see cref="GuideAt"/>).</description></item>
    /// <item><description><b>Las mini cuias</b> de las paradas de los mundos ya abiertos (hasta el de «Jugar»), en la orilla de su
    /// punto real (<see cref="MarkerSideDp"/>): llenas del agua del sitio las alcanzadas, de vidrio las que faltan. Se tocan con un
    /// área de ≥ 48 dp (<see cref="MinTouchDp"/>); si dos quedan a menos de <see cref="MapConfig.ClusterMinDp"/> dp, el racimo se
    /// abre en <b>abanico</b> con una línea fina hasta cada punto real (propuesta §3.2: la geometría nunca se deforma). Solo en la
    /// vista Mundo: en la Cuenca, a 1×, no caben.</description></item>
    /// <item><description><b>La baliza</b> de los cierres de mundo (<c>kind</c> = <c>close</c>; propuesta §4.1), junto a su mini
    /// cuia: encendida al cerrar el mundo, es decir, al ganar el último nivel de esa parada (40, 80 y 140 en 1-200;
    /// <see cref="BeaconLit"/>). Sin «Premio del mundo».</description></item>
    /// <item><description><b>La casa de TiTino</b> fija en Guajará (<see cref="RiverMap.Home"/>; c.3: no viaja): su embaúba
    /// (<see cref="ProceduralArt.MapHome"/>) con el perezoso colgado (<see cref="SlothRig"/>, <c>sloth_idle</c>), que dormita a los
    /// <see cref="MapConfig.MapIdleSleepMs"/> ms sin toques (<c>sloth_sleep</c>, que sustituye a <c>capi_sleep</c>) y despierta
    /// con el siguiente (<c>sloth_wake</c>). Cuando la casa queda fuera de la vista, la <b>ventanita de casa</b>
    /// (<see cref="HomeWindow"/>, abajo a la izquierda de la zona del mapa) con «Casa · a unos {km} km» (<c>map_home_distance</c>,
    /// los km de la parada de «Jugar»); tocarla lleva la vista a la casa (<see cref="GoHome"/>). Arte provisional de P-M5: en
    /// release, sin su PNG, ni casa ni ventanita.</description></item>
    /// </list>
    /// <para>Las piezas se crean una vez (al construir la geometría y, las mini cuias, al crecer la reserva al abrir); al cambiar de
    /// vista solo se recolocan; con el Mapa quieto, <see cref="MarkersTick"/> no asigna memoria (el reloj del perezoso y la
    /// comprobación de si la casa se ve).</para>
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Banderas que caben en el mástil (las de la 1.0 son 5: BR, CO, PE, EC y BO; propuesta §12, pieza 12).</summary>
        public const int MaxCanoeFlags = 8;

        /// <summary>Escala de las banderas en el mástil de la canoa del Mapa (las del banner miden 16 × 11 dp) [P].</summary>
        public const float CanoeFlagScale = 0.75f;

        /// <summary>Hueco entre dos banderas del mástil (dp) [P].</summary>
        public const float CanoeFlagGapDp = 1f;

        /// <summary>Mástil por encima de la bandera más alta y por debajo de la más baja (dp) [P].</summary>
        public const float MastPadDp = 4f;

        /// <summary>Ancho del mástil (dp), el del banner.</summary>
        public const float MastWidthDp = 1.6f;

        /// <summary>Popa: el mástil va a esta fracción del ancho de la canoa detrás de su centro, como en el banner.</summary>
        public const float SternFraction = 0.32f;

        /// <summary>El guía nada este tanto por delante de la canoa por el agua (dp), como en el banner [P].</summary>
        public const float GuideLeadDp = 35f;

        /// <summary>Ancho del guía en la vista Mundo (dp), el del banner [P].</summary>
        public const float GuideWorldDp = 22f;

        /// <summary>Ancho del guía en la vista Cuenca (dp) [P].</summary>
        public const float GuideBasinDp = 16f;

        /// <summary>
        /// Si a la canoa le quedan al menos estos dp de agua hasta la parada a la que va, pero menos de <see cref="GuideLeadDp"/>,
        /// el guía la espera en la parada; con menos, la espera encima de la canoa (<see cref="GuideAboveDp"/>): nunca nada el tramo
        /// siguiente antes de que la canoa llegue, que puede volver río abajo (dp) [P].
        /// </summary>
        public const float GuideMinAheadDp = 18f;

        /// <summary>Sin agua por delante (el final del catálogo), el guía espera encima de la canoa, este tanto más arriba (dp) [P].</summary>
        public const float GuideAboveDp = 27f;

        /// <summary>La mini cuia va en la orilla, este tanto a un lado de su punto real (dp) [P]: fuera del casco de la canoa.</summary>
        public const float MarkerSideDp = 26f;

        /// <summary>Y este tanto más arriba (dp) [P].</summary>
        public const float MarkerUpDp = 6f;

        /// <summary>Arco del abanico de un racimo de mini cuias (grados, de arriba abajo del lado de la orilla) [P].</summary>
        public const float FanSpanDeg = 140f;

        /// <summary>Ancho de la línea fina del abanico hasta el punto real (dp) [P].</summary>
        public const float LeaderWidthDp = 1.2f;

        /// <summary>Radio del punto real de una mini cuia abierta en abanico (dp) [P].</summary>
        public const float LeaderDotDp = 2.2f;

        /// <summary>
        /// La casa va en la orilla de enfrente de la mini cuia de Belém, este tanto a la izquierda de su punto (dp, a la escala de
        /// la vista) [P]: el tronco en tierra, no en el agua de la bahía.
        /// </summary>
        public const float HomeSideDp = 24f;

        /// <summary>
        /// Opacidad del vidrio de la mini cuia de una parada que falta [P]: el vidrio provisional tiene el interior oscuro y, entero,
        /// se leía como agua negra; así se lee vacía, como el vidrio fantasma del río (<see cref="MapGlassBodyAlpha"/>).
        /// </summary>
        public const float UnreachedGlassAlpha = 0.55f;

        /// <summary>Lado del perezoso de la casa en la vista Mundo (dp) [P].</summary>
        public const float HomeSlothWorldDp = 22f;

        /// <summary>Escala de la casa (y de su perezoso) en la vista Cuenca, a 1× [P].</summary>
        public const float HomeBasinScale = 0.7f;

        /// <summary>Margen con que la casa tiene que quedar dentro de la zona del mapa para contar como «a la vista» (dp) [P].</summary>
        public const float HomeInViewMarginDp = 8f;

        /// <summary>Hueco entre la ventanita de casa y lo que tiene debajo (el rótulo del Atlántico, «Volver a la canoa») (dp) [P].</summary>
        public const float HomeWindowGapDp = 6f;

        /// <summary>Línea del abanico: crema casi opaca, como el canto de las boyas.</summary>
        public static readonly Color LeaderColor = new Color(Palette.Cream.r, Palette.Cream.g, Palette.Cream.b, 0.9f);

        /// <summary>Canto oscuro de la línea del abanico y del punto real.</summary>
        public static readonly Color LeaderRim = new Color(Palette.Groove.r, Palette.Groove.g, Palette.Groove.b, 0.7f);

        private readonly List<StopMarker> _markers = new List<StopMarker>();
        private readonly string?[] _flagIso = new string?[MaxCanoeFlags];
        private readonly Image?[] _flagImages = new Image?[MaxCanoeFlags];
        private readonly string?[] _flagScratch = new string?[MaxCanoeFlags];
        private int[] _clusterParent = Array.Empty<int>();
        private int[] _clusterMembers = Array.Empty<int>();

        private RectTransform? _markerLayer;
        private LeaderLinesGraphic? _leaders;
        private RectTransform? _canoeGear;
        private Image? _mast;
        private Image? _guide;
        private RectTransform? _houseLayer;
        private Image? _houseTree;
        private SlothRig? _houseSloth;
        private HomeWindow? _homeWindow;
        private RiverStop? _homeStop;
        private bool _markersReady;
        private bool _houseShown;
        private int _markerCount;
        private int _flagCount;
        private int _markersLevel = -1;
        private float _canoeX;
        private Vector2 _canoeShown;
        private float _canoeFacing = 1f;
        private Vector2 _guideMap;
        private int _guideRoute = -1;
        private float _guideFraction;
        private bool _guideShown;
        private string _guideId = RiverJourney.GuideRosa;
        private Vector2 _homeMap;
        private Vector2 _homeLocal;
        private float _homeHeight;
        private bool _homeInView = true;
        private bool _layoutHomeWindow;
        private float _idleMs;
        private bool _asleep;
        private float _wakeLeftMs;
        private int _touchViewChanges;
        private int _lastTappedStop = -1;
        private int _fillingMarker = -1;
        private float _fillingMs;

        // ============================================================================================ API (tests y sondas)

        /// <summary>El arte de la pantalla (tests: los sprites que se esperan).</summary>
        internal ProceduralArt Art => _art;

        /// <summary>Banderas del mástil (0 … <see cref="MaxCanoeFlags"/>), las que tienen dibujo.</summary>
        public int CanoeFlagCount => _flagCount;

        /// <summary>País (ISO2) de la bandera <paramref name="i"/> del mástil: 0 la primera izada, la última la más reciente (arriba).</summary>
        public string CanoeFlag(int i)
        {
            return _flagIso[i]!;
        }

        /// <summary>Imagen de la bandera <paramref name="i"/> (en el orden de <see cref="CanoeFlag"/>).</summary>
        internal Image CanoeFlagImage(int i)
        {
            return _flagImages[i]!;
        }

        /// <summary>Mástil de popa (activo solo con alguna bandera).</summary>
        internal Image? Mast => _mast;

        /// <summary>Hacia dónde mira la proa de la canoa: +1 a la derecha, −1 a la izquierda.</summary>
        public float CanoeFacing => _canoeFacing;

        /// <summary>Dónde se pinta la canoa ahora, en unidades del marco (al ponerse al día, va por el agua hacia <see cref="CanoeMapPoint"/>).</summary>
        public Vector2 CanoeShownMapPoint => _canoeShown;

        /// <summary>Nivel (fraccionario, al ponerse al día) en el que se pinta la canoa.</summary>
        public float CanoeShownLevel => _canoeX;

        /// <summary>El guía se ve.</summary>
        public bool GuideShown => _guideShown;

        /// <summary>Quién guía: <see cref="RiverJourney.GuideRosa"/> o <see cref="RiverJourney.GuideBufeo"/>.</summary>
        public string GuideId => _guideId;

        /// <summary>Dónde está el guía, en unidades del marco.</summary>
        public Vector2 GuideMapPoint => _guideMap;

        /// <summary>Ruta (la k de su parada) por la que nada el guía, o −1 si espera encima de la canoa.</summary>
        public int GuideRoute => _guideRoute;

        /// <summary>Fracción de <see cref="GuideRoute"/> en la que nada el guía.</summary>
        public float GuideRouteFraction => _guideFraction;

        /// <summary>Imagen del guía.</summary>
        internal Image? GuideImage => _guide;

        /// <summary>Mini cuias de las paradas (las de los mundos abiertos hasta el de «Jugar»).</summary>
        public int StopMarkerCount => _markerCount;

        /// <summary>Las mini cuias se ven (solo en la vista Mundo).</summary>
        public bool StopMarkersShown => _markerLayer != null && _markerLayer.gameObject.activeSelf;

        /// <summary>Parada (k) de la mini cuia <paramref name="i"/>.</summary>
        public int StopMarkerStop(int i)
        {
            return _markers[i].N;
        }

        /// <summary>La parada de la mini cuia <paramref name="i"/> ya se alcanzó: va llena del agua del sitio.</summary>
        public bool StopMarkerReached(int i)
        {
            return _markers[i].Reached;
        }

        /// <summary>Lo lleno de la mini cuia <paramref name="i"/> (0-1; sube en 600 ms al llegar al ponerse al día).</summary>
        public float StopMarkerFill(int i)
        {
            StopMarker m = _markers[i];
            return m.WaterA.gameObject.activeSelf ? m.WaterA.fillAmount : 0f;
        }

        /// <summary>La mini cuia <paramref name="i"/> va en abanico (su racimo no cabía a ≥ 48 dp), con su línea al punto real.</summary>
        public bool StopMarkerFanned(int i)
        {
            return _markers[i].Fanned;
        }

        /// <summary>Centro del área táctil de la mini cuia <paramref name="i"/> en coordenadas locales del contenido (dp, y hacia arriba).</summary>
        public Vector2 StopMarkerLocal(int i)
        {
            return _markers[i].Shown;
        }

        /// <summary>Punto real de la parada de la mini cuia <paramref name="i"/> en coordenadas locales del contenido.</summary>
        public Vector2 StopMarkerRealLocal(int i)
        {
            return _markers[i].Real;
        }

        /// <summary>Área táctil de la mini cuia <paramref name="i"/>.</summary>
        internal Button StopMarkerButton(int i)
        {
            return _markers[i].Button;
        }

        /// <summary>La mini cuia <paramref name="i"/> lleva baliza (su parada cierra un mundo).</summary>
        public bool StopMarkerHasBeacon(int i)
        {
            return _markers[i].Beacon.gameObject.activeSelf;
        }

        /// <summary>La baliza de la mini cuia <paramref name="i"/> está encendida.</summary>
        public bool StopMarkerBeaconLit(int i)
        {
            return _markers[i].BeaconLit;
        }

        /// <summary>Índice de la mini cuia de la parada <paramref name="n"/>, o −1 si no se pinta.</summary>
        public int StopMarkerIndexOf(int n)
        {
            for (int i = 0; i < _markerCount; i++)
            {
                if (_markers[i].N == n)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Última parada tocada (−1 si ninguna desde que se abrió).</summary>
        public int LastTappedStop => _lastTappedStop;

        /// <summary>La casa de TiTino se pinta (con su arte: en release, solo con los PNG finales).</summary>
        public bool HomeShown => _houseShown;

        /// <summary>Dónde está la casa (Guajará), en unidades del marco.</summary>
        public Vector2 HomeMapPoint => _homeMap;

        /// <summary>La casa queda dentro de la zona visible del mapa.</summary>
        public bool HomeInView => _homeInView;

        /// <summary>La ventanita de casa se ve (la casa queda fuera de la vista).</summary>
        public bool HomeWindowShown => _homeWindow != null && _homeWindow.Visible;

        /// <summary>Texto de la ventanita («Casa · a unos {km} km»; vacío en casa).</summary>
        public string HomeWindowText => _homeWindow != null ? _homeWindow.DistanceText : string.Empty;

        /// <summary>Área táctil de la ventanita (lleva la vista a la casa).</summary>
        public Button? HomeWindowButton => _homeWindow?.TapButton;

        /// <summary>La ventanita de casa del Mapa.</summary>
        internal HomeWindow? MapHomeWindow => _homeWindow;

        /// <summary>El rig del perezoso de la casa (null sin arte).</summary>
        internal SlothRig? HomeSloth => _houseSloth;

        /// <summary>TiTino dormita (<c>sloth_sleep</c>): pasaron <see cref="MapConfig.MapIdleSleepMs"/> ms sin toques.</summary>
        public bool HomeSlothAsleep => _asleep;

        /// <summary>Milisegundos sin toques con el Mapa abierto.</summary>
        public float IdleMs => _idleMs;

        // ============================================================================================ reglas puras

        /// <summary>
        /// Banderas que lleva la canoa con <paramref name="maxLevel"/> niveles ganados: el <c>flag_raise</c> de cada parada
        /// alcanzada (k ≤ max / 10; la 0, Belém, siempre), en orden y sin repetir: si se vuelve a un país, su bandera pasa a ser la
        /// más reciente. Las escribe en <paramref name="into"/> (0 la primera izada) y devuelve cuántas. Sin asignaciones.
        /// </summary>
        public static int CanoeFlags(RiverStopCatalog stops, int maxLevel, string?[] into)
        {
            if (stops == null || into == null)
            {
                throw new ArgumentNullException(stops == null ? nameof(stops) : nameof(into));
            }

            int count = 0;
            int reached = Mathf.Min(Mathf.Max(0, maxLevel) / RiverStopCatalog.LevelsPerStop, stops.Count - 1);
            for (int n = 0; n <= reached; n++)
            {
                string? flag = stops.At(n)?.FlagRaise;
                if (flag == null)
                {
                    continue;
                }

                int at = -1;
                for (int i = 0; i < count; i++)
                {
                    if (string.Equals(into[i], flag, StringComparison.Ordinal))
                    {
                        at = i;
                        break;
                    }
                }

                if (at >= 0)
                {
                    for (int i = at; i < count - 1; i++)
                    {
                        into[i] = into[i + 1];
                    }

                    into[count - 1] = flag;
                    continue;
                }

                if (count == into.Length)
                {
                    for (int i = 0; i < count - 1; i++)
                    {
                        into[i] = into[i + 1];
                    }

                    into[count - 1] = flag;
                    continue;
                }

                into[count++] = flag;
            }

            for (int i = count; i < into.Length; i++)
            {
                into[i] = null;
            }

            return count;
        }

        /// <summary>
        /// La baliza de <paramref name="stop"/> está encendida con <paramref name="maxLevel"/> niveles ganados: es un cierre de mundo
        /// (<see cref="StopKind.Close"/>) y se ganó su último nivel (<c>10k + 10</c>: el 40, el 80 y el 140 en 1-200).
        /// </summary>
        public static bool BeaconLit(RiverStop stop, int maxLevel)
        {
            return stop != null && stop.Kind == StopKind.Close && maxLevel >= stop.LastLevel;
        }

        /// <summary>
        /// Dónde va la canoa con <paramref name="level"/> niveles ganados, también fraccionario (al ponerse al día, entre dos
        /// niveles): en 10k, en la parada k; si no, por la ruta de la parada siguiente, a (level − 10 (s − 1)) / 10 de su largo.
        /// Con niveles enteros coincide con <see cref="TryCanoePoint"/>. Sin asignaciones.
        /// </summary>
        public static Vector2 CanoeAt(RiverMap map, RiverRoutes routes, float level)
        {
            if (routes.Count == 0)
            {
                return Vector2.zero;
            }

            const int per = RiverStopCatalog.LevelsPerStop;
            float x = Mathf.Max(0f, level);
            int s = x <= 0f ? 0 : Mathf.CeilToInt((x / per) - 1e-4f);
            if (s <= 0)
            {
                return PlacePoint(map, routes.At(0)!.At);
            }

            RiverStopRoute? route = s < routes.Count ? routes.At(s) : null;
            if (route == null)
            {
                return PlacePoint(map, routes.At(routes.Count - 1)!.At);
            }

            return RoutePoint(map, route, Mathf.Clamp01((x - (per * (s - 1))) / per));
        }

        /// <summary>
        /// Quién guía la canoa con <paramref name="level"/> niveles ganados (P-M8: se deduce del mundo): el guía de la parada a la
        /// que va (<see cref="RiverJourney.GuideFor"/>) y, si la ruta cambia de guía (los viajes del 300 y del 560), el de la parada
        /// de salida hasta el punto de paso de Teotônio y el de la llegada desde él (sin ese punto en la ruta, a mitad del tramo).
        /// Sin asignaciones.
        /// </summary>
        public static string GuideAt(RiverMap map, RiverRoutes routes, RiverStopCatalog stops, float level)
        {
            const int per = RiverStopCatalog.LevelsPerStop;
            float x = Mathf.Max(0f, level);
            int s = Mathf.Clamp(x <= 0f ? 1 : Mathf.CeilToInt((x / per) - 1e-4f), 1, Mathf.Max(1, stops.Count - 1));
            RiverStop? to = stops.At(s);
            if (to == null)
            {
                return RiverJourney.GuideRosa;
            }

            string guideTo = RiverJourney.GuideFor(to.World);
            RiverStop? from = stops.At(s - 1);
            string guideFrom = from != null ? RiverJourney.GuideFor(from.World) : guideTo;
            if (string.Equals(guideFrom, guideTo, StringComparison.Ordinal))
            {
                return guideTo;
            }

            RiverStopRoute? route = s < routes.Count ? routes.At(s) : null;
            float relay = 0.5f;
            RiverPassingPoint? point = map.PassingPoint(BannerTravel.RelayPassingId);
            if (route != null && point != null)
            {
                float at = PlaceFraction(map, route, point.At);
                if (at >= 0f)
                {
                    relay = at;
                }
            }

            float fraction = Mathf.Clamp01((x - (per * (s - 1))) / per);
            return fraction < relay ? guideFrom : guideTo;
        }

        /// <summary>
        /// Fracción (0-1, por longitud de arco) de la ruta <paramref name="route"/> en la que pasa por <paramref name="place"/>, o −1
        /// si la ruta no pasa por ahí (o no tiene largo). Sin asignaciones.
        /// </summary>
        public static float PlaceFraction(RiverMap map, RiverStopRoute route, RiverMapPlace place)
        {
            float total = RouteLength(map, route);
            if (total <= 0f)
            {
                return -1f;
            }

            IReadOnlyList<RiverPathPiece> path = route.Path;
            float arc = 0f;
            for (int i = 0; i < path.Count; i++)
            {
                RiverPathPiece piece = path[i];
                float hostLength = map.Lines[piece.Line].Length;
                float lo = Mathf.Min(piece.S0, piece.S1);
                float hi = Mathf.Max(piece.S0, piece.S1);
                if (piece.Line == place.Line && place.S >= lo && place.S <= hi)
                {
                    return Mathf.Clamp01((arc + (Mathf.Abs(place.S - piece.S0) * hostLength)) / total);
                }

                arc += (hi - lo) * hostLength;
            }

            return -1f;
        }

        /// <summary>Largo de la ruta <paramref name="route"/> por el agua, en unidades del marco. Sin asignaciones.</summary>
        public static float RouteLength(RiverMap map, RiverStopRoute route)
        {
            IReadOnlyList<RiverPathPiece> path = route.Path;
            float total = 0f;
            for (int i = 0; i < path.Count; i++)
            {
                total += Mathf.Abs(path[i].S1 - path[i].S0) * map.Lines[path[i].Line].Length;
            }

            return total;
        }

        // ============================================================================================ ganchos

        partial void MarkersBuild(RectTransform dynamicLayer)
        {
            if (_markerLayer == null)
            {
                // La casa y las mini cuias, detrás de la canoa; el mástil, las banderas y el guía, justo delante de ella (y detrás de
                // las boyas por ganar, que siguen siendo el «solo me falta uno»).
                _houseLayer = UiBuilder.Rect("Home", dynamicLayer);
                FillTopLeft(_houseLayer);
                _houseLayer.SetSiblingIndex(_canoe.transform.GetSiblingIndex());
                _houseTree = UiBuilder.Image("Embauba", _houseLayer, null, Color.white);
                _houseTree.preserveAspect = true;
                RectTransform tree = _houseTree.rectTransform;
                tree.anchorMin = new Vector2(0f, 1f);
                tree.anchorMax = new Vector2(0f, 1f);
                tree.pivot = new Vector2(0.5f, 0f);
                _houseLayer.gameObject.SetActive(false);

                _markerLayer = UiBuilder.Rect("Markers", dynamicLayer);
                FillTopLeft(_markerLayer);
                _markerLayer.SetSiblingIndex(_canoe.transform.GetSiblingIndex());
                var leadersGo = new GameObject("Leaders", typeof(RectTransform), typeof(CanvasRenderer), typeof(LeaderLinesGraphic));
                leadersGo.transform.SetParent(_markerLayer, false);
                FillTopLeft((RectTransform)leadersGo.transform);
                _leaders = leadersGo.GetComponent<LeaderLinesGraphic>();
                _leaders.raycastTarget = false;

                _canoeGear = UiBuilder.Rect("CanoeGear", dynamicLayer);
                FillTopLeft(_canoeGear);
                _canoeGear.SetSiblingIndex(_canoe.transform.GetSiblingIndex() + 1);
                _mast = Piece("Mast", _canoeGear, new Vector2(0.5f, 0f));
                _mast.color = Palette.FamilyWood.Deep;
                for (int i = 0; i < _flagImages.Length; i++)
                {
                    Image flag = Piece("Flag" + i, _canoeGear, new Vector2(0f, 0.5f));
                    flag.preserveAspect = true;
                    _flagImages[i] = flag;
                }

                _guide = Piece("Guide", _canoeGear, new Vector2(0.5f, 0.3f));
                _guide.preserveAspect = true;

                // La ventanita de casa, en el marco: encima del mapa y debajo de «Volver a la canoa».
                _homeWindow = new HomeWindow(_frame.transform, _art, GoHome);
                int button = _backToCanoe.Button.transform.GetSiblingIndex();
                _homeWindow.Root.SetSiblingIndex(button);
                _homeWindow.PillRect.SetSiblingIndex(button + 1);
                _homeWindow.TapRect!.SetSiblingIndex(button + 2);

                // Un toque en cualquier sitio del mapa despierta a TiTino y completa «ponerse al día».
                _viewport.gameObject.AddComponent<PointerDownRelay>().Bind(OnMapPointerDown);
            }

            _markersLevel = -1;
        }

        partial void MarkersOpen(IMapProgress progress)
        {
            RiverMap? map = _map;
            RiverRoutes? routes = _routes;
            RiverStopCatalog? stops = _stops;
            _markersReady = map != null && routes != null && stops != null && _markerLayer != null;
            _idleMs = 0f;
            _asleep = false;
            _wakeLeftMs = 0f;
            _touchViewChanges = _viewChanges;
            _lastTappedStop = -1;
            _fillingMarker = -1;
            if (!_markersReady)
            {
                return;
            }

            int max = Mathf.Clamp(progress.MaxLevel, 0, WorldTable.LastLevel);
            BuildStopMarkers(map!, routes!, stops!);
            BuildHouse(map!);
            _homeStop = stops!.StopOf(_playLevel);
            _homeWindow!.SetAsleep(false);
            _homeWindow.Apply(_homeStop, true);
            _homeInView = true;
            _canoeX = max;
            _canoeShown = _canoeMap;
            ShowMarkersAt(max);
        }

        partial void MarkersSpace(MapSpace space)
        {
            if (!_markersReady)
            {
                return;
            }

            bool world = _view == MapView.World;
            if (_markerLayer!.gameObject.activeSelf != world)
            {
                _markerLayer.gameObject.SetActive(world);
            }

            if (world)
            {
                PlaceStopMarkers(space);
            }

            PlaceHouse(space);
            PlaceCanoeGear(space);
            PlaceHomeWindow();
            UpdateHomeWindow();
        }

        partial void MarkersTick(float dtMs, ref bool animating)
        {
            if (!_markersReady)
            {
                return;
            }

            // Toques: arrastrar, «Volver a la canoa», ir a casa o cambiar de vista despiertan a TiTino (los toques sueltos sobre el
            // mapa y las mini cuias pasan por OnMapPointerDown y TapStop).
            if (_dragging || _activeMs > 0f || _viewChanges != _touchViewChanges)
            {
                _touchViewChanges = _viewChanges;
                NoteTouch();
            }
            else if (!_asleep)
            {
                _idleMs += dtMs;
                if (_idleMs >= MapConfig.MapIdleSleepMs)
                {
                    Sleep();
                }
            }

            if (_houseSloth != null && _houseShown)
            {
                if (_wakeLeftMs > 0f)
                {
                    _wakeLeftMs -= dtMs;
                    if (_wakeLeftMs <= 0f && !_asleep)
                    {
                        _houseSloth.Play(SlothPoseId.Idle, ProgressReduceMotion);
                    }
                }

                _houseSloth.Tick(dtMs);
            }

            if (_fillingMarker >= 0)
            {
                _fillingMs += dtMs;
                float t = Mathf.Clamp01(_fillingMs / MapConfig.ArrivalSampleFillMs);
                SetMarkerFill(_markers[_fillingMarker], t);
                if (t >= 1f)
                {
                    _fillingMarker = -1;
                }

                animating = true;
            }

            bool window = UpdateHomeWindow();
            if (window != _layoutHomeWindow && !_dragging)
            {
                _layoutHomeWindow = window;
                EmitLayout(); // la ventanita apareció o se fue: AF_LAYOUT la publica o deja de publicarla (nunca por fotograma)
            }
        }

        partial void MarkersLayoutIds(List<string> ids, List<RectTransform> rects)
        {
            _layoutHomeWindow = HomeWindowShown;
            if (!_markersReady)
            {
                return;
            }

            if (_homeWindow != null && _homeWindow.Visible && _homeWindow.TapRect != null)
            {
                ids.Add("home_window");
                rects.Add(_homeWindow.TapRect);
            }

            if (_view != MapView.World)
            {
                return;
            }

            for (int i = 0; i < _markerCount; i++)
            {
                StopMarker m = _markers[i];
                Vector2 p = new Vector2(_offset.x + m.Shown.x, _offset.y - m.Shown.y);
                if (p.x >= 0f && p.x <= _viewW && p.y >= 0f && p.y <= _viewH)
                {
                    ids.Add("stop_" + DevProbe.StopId(m.Stop));
                    rects.Add(m.Root);
                }
            }
        }

        /// <summary>M4.1: se tocó la mini cuia de la parada <paramref name="n"/> (la tarjeta de la parada).</summary>
        partial void MarkersStopTapped(int n);

        // ============================================================================================ estado

        /// <summary>
        /// Pinta los marcadores con <paramref name="level"/> niveles ganados: las banderas del mástil, las mini cuias alcanzadas y
        /// las balizas encendidas. Lo llama <see cref="Open"/> con <c>max_level</c> y «ponerse al día» en cada boya. Solo cambia lo
        /// que cambia; sin asignaciones (los sprites se crean la primera vez que se piden).
        /// </summary>
        internal void ShowMarkersAt(int level)
        {
            if (!_markersReady)
            {
                return;
            }

            int max = Mathf.Clamp(level, 0, WorldTable.LastLevel);
            _markersLevel = max;
            int raw = CanoeFlags(_stops!, max, _flagScratch);
            _flagCount = 0;
            for (int i = 0; i < raw; i++)
            {
                string iso = _flagScratch[i]!;
                Sprite? sprite = _art.MapFlag(iso);
                if (sprite == null)
                {
                    continue; // release sin la bandera oficial: no sale
                }

                int k = _flagCount++;
                _flagIso[k] = iso;
                Image flag = _flagImages[k]!;
                if (flag.sprite != sprite)
                {
                    flag.sprite = sprite;
                }
            }

            for (int i = 0; i < _flagImages.Length; i++)
            {
                Image flag = _flagImages[i]!;
                bool on = i < _flagCount;
                if (flag.gameObject.activeSelf != on)
                {
                    flag.gameObject.SetActive(on);
                }

                if (!on)
                {
                    _flagIso[i] = null;
                }
            }

            if (_mast!.gameObject.activeSelf != (_flagCount > 0))
            {
                _mast.gameObject.SetActive(_flagCount > 0);
            }

            for (int i = 0; i < _markerCount; i++)
            {
                StopMarker m = _markers[i];
                bool reached = max >= m.N * RiverStopCatalog.LevelsPerStop;
                if (m.Reached != reached)
                {
                    m.Reached = reached;
                    m.Glass.color = reached ? Color.white : new Color(1f, 1f, 1f, UnreachedGlassAlpha);
                    if (i == _fillingMarker)
                    {
                        _fillingMarker = -1;
                    }

                    SetMarkerFill(m, reached ? 1f : 0f);
                }

                bool lit = BeaconLit(m.Stop, max);
                if (m.Beacon.gameObject.activeSelf && m.BeaconLit != lit)
                {
                    m.BeaconLit = lit;
                    m.Beacon.sprite = _art.MapBeacon(lit);
                    m.Beacon.color = !lit && ArtSlots.Find(ProceduralArt.MapBeaconSlot) != null ? ProceduralArt.BeaconUnlitTint : Color.white;
                }
            }

            PlaceCanoeGear(_space);
        }

        /// <summary>
        /// Lleva la canoa (y su mástil y su guía) a <paramref name="level"/> niveles ganados, fraccionario: «ponerse al día» la hace
        /// recorrer el agua de boya en boya. Sin asignaciones.
        /// </summary>
        internal void MoveCanoe(float level)
        {
            if (!_markersReady || !_hasCanoe)
            {
                return;
            }

            _canoeX = level;
            _canoeShown = level >= _progress!.MaxLevel ? _canoeMap : CanoeAt(_map!, _routes!, level);
            PlaceCanoeGear(_space);
        }

        /// <summary>
        /// Empieza a llenar la mini cuia de la parada <paramref name="n"/> (0 → 1 en <see cref="MapConfig.ArrivalSampleFillMs"/> ms):
        /// la llegada que recorre la canoa al ponerse al día.
        /// </summary>
        internal void FillStopMarker(int n)
        {
            int i = StopMarkerIndexOf(n);
            if (i < 0)
            {
                return;
            }

            if (_fillingMarker >= 0 && _fillingMarker != i)
            {
                SetMarkerFill(_markers[_fillingMarker], 1f);
            }

            _fillingMarker = i;
            _fillingMs = 0f;
            SetMarkerFill(_markers[i], 0f);
        }

        /// <summary>Termina en el acto el llenado de la mini cuia que se estuviera llenando.</summary>
        internal void FinishStopMarkerFill()
        {
            if (_fillingMarker >= 0)
            {
                SetMarkerFill(_markers[_fillingMarker], 1f);
                _fillingMarker = -1;
            }
        }

        /// <summary>
        /// Lleva la vista a la casa de TiTino, en Guajará (el toque de la ventanita de casa; propuesta del Mapa §2.2: «al tocar la
        /// píldora, la cámara baja hasta la rama de TiTino»), sin cambiar de vista.
        /// </summary>
        public void GoHome()
        {
            if (!Visible || !_markersReady)
            {
                return;
            }

            CatchUpTouch();
            NoteTouch();
            Vector2 local = _homeLocal;
            local.y += _homeHeight * 0.5f;
            SetOffset(new Vector2((_viewW * 0.5f) - local.x, (_viewH * 0.5f) + local.y));
            _activeMs = ActiveLingerMs;
            UpdateHomeWindow();
            EmitLayout();
        }

        /// <summary>Toca la mini cuia de la parada <paramref name="n"/> (tests y <c>adb</c>: lo mismo que su área táctil).</summary>
        public bool TapStop(int n)
        {
            int i = StopMarkerIndexOf(n);
            if (i < 0 || !StopMarkersShown)
            {
                return false;
            }

            TapMarker(_markers[i]);
            return true;
        }

        // ============================================================================================ construcción

        // Una mini cuia por parada con ruta de los mundos abiertos hasta el de «Jugar» (las de los mundos futuros van en su silueta,
        // M3.4). Solo asigna al crecer la reserva; el agua y la baliza se fijan aquí, al abrir.
        private void BuildStopMarkers(RiverMap map, RiverRoutes routes, RiverStopCatalog stops)
        {
            int count = 0;
            for (int n = 0; n < stops.Count && n < routes.Count; n++)
            {
                RiverStop? stop = stops.At(n);
                RiverStopRoute? route = routes.At(n);
                if (stop == null || route == null || stop.World > _world)
                {
                    continue;
                }

                StopMarker m = count < _markers.Count ? _markers[count] : NewMarker(count);
                count++;
                m.N = n;
                m.Stop = stop;
                m.MapPoint = PlacePoint(map, route.At);
                m.Reached = false;
                m.Glass.color = new Color(1f, 1f, 1f, UnreachedGlassAlpha);
                m.BeaconLit = false;
                m.Root.gameObject.name = "Stop_" + stop.Id;
                Color a = ArrivalStrip.SampleWater(stop.Sample, out Color? b);
                m.Mixed = b.HasValue;
                m.WaterA.color = a;
                m.WaterB.color = b ?? a;
                SetMarkerFill(m, 0f);
                bool close = stop.Kind == StopKind.Close;
                if (m.Beacon.gameObject.activeSelf != close)
                {
                    m.Beacon.gameObject.SetActive(close);
                }

                if (close)
                {
                    m.Beacon.sprite = _art.MapBeacon(false);
                    m.Beacon.color = ArtSlots.Find(ProceduralArt.MapBeaconSlot) != null ? ProceduralArt.BeaconUnlitTint : Color.white;
                }
            }

            _markerCount = count;
            for (int i = 0; i < _markers.Count; i++)
            {
                bool on = i < count;
                if (_markers[i].Root.gameObject.activeSelf != on)
                {
                    _markers[i].Root.gameObject.SetActive(on);
                }
            }

            if (_clusterParent.Length < _markers.Count)
            {
                _clusterParent = new int[_markers.Count];
                _clusterMembers = new int[_markers.Count];
            }

            _leaders!.Reserve(_markers.Count);
        }

        private StopMarker NewMarker(int index)
        {
            StopMarker? marker = null;
            Button button = UiBuilder.BareButton("Stop_" + index, _markerLayer!, () =>
            {
                if (marker != null)
                {
                    TapMarker(marker);
                }
            });
            var root = (RectTransform)button.transform;
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(MinTouchDp, MinTouchDp);

            // El agua va encima del vidrio, como en la franja de llegada (el vidrio provisional tiene el interior casi opaco).
            Image glass = MarkerPiece("MiniCuia", root, _art.MapMiniCuia);
            Image waterA = MarkerPiece("Water", root, _art.MapMiniCuiaWater);
            Image waterB = MarkerPiece("WaterMeet", root, _art.MapMiniCuiaWater);
            foreach (Image water in new[] { waterA, waterB })
            {
                water.type = Image.Type.Filled;
                water.fillMethod = Image.FillMethod.Vertical;
                water.fillOrigin = (int)Image.OriginVertical.Bottom;
            }

            Image beacon = UiBuilder.Image("Beacon", root, null, Color.white);
            beacon.preserveAspect = true;
            RectTransform b = beacon.rectTransform;
            b.anchorMin = new Vector2(0.5f, 0.5f);
            b.anchorMax = new Vector2(0.5f, 0.5f);
            b.pivot = new Vector2(0.5f, 0f);
            b.sizeDelta = new Vector2(ProceduralArt.MapBeaconWidthDp, ProceduralArt.MapBeaconHeightDp);
            b.anchoredPosition = new Vector2((ProceduralArt.MiniCuiaWidthDp * 0.5f) + 1f + (ProceduralArt.MapBeaconWidthDp * 0.5f),
                -ProceduralArt.MiniCuiaHeightDp * 0.5f);
            beacon.gameObject.SetActive(false);

            marker = new StopMarker(root, button, glass, waterA, waterB, beacon);
            _markers.Add(marker);
            return marker;
        }

        // Una pieza de la mini cuia: su caja de 20 × 32 dp, centrada en el área táctil.
        private static Image MarkerPiece(string name, RectTransform root, Sprite sprite)
        {
            Image img = UiBuilder.Image(name, root, sprite, Color.white);
            img.preserveAspect = true;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(ProceduralArt.MiniCuiaWidthDp, ProceduralArt.MiniCuiaHeightDp);
            rt.anchoredPosition = Vector2.zero;
            return img;
        }

        // Una imagen del lienzo dinámico, anclada arriba a la izquierda del contenido, con su pivote.
        private static Image Piece(string name, RectTransform parent, Vector2 pivot)
        {
            Image img = UiBuilder.Image(name, parent, null, Color.white);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = pivot;
            img.gameObject.SetActive(false);
            return img;
        }

        private static void SetMarkerFill(StopMarker m, float fill)
        {
            bool on = fill > 0f;
            if (m.WaterA.gameObject.activeSelf != on)
            {
                m.WaterA.gameObject.SetActive(on);
            }

            bool meet = on && m.Mixed;
            if (m.WaterB.gameObject.activeSelf != meet)
            {
                m.WaterB.gameObject.SetActive(meet);
            }

            if (m.WaterA.fillAmount != fill)
            {
                m.WaterA.fillAmount = fill;
            }

            float half = Mathf.Min(fill, 0.5f);
            if (m.WaterB.fillAmount != half)
            {
                m.WaterB.fillAmount = half;
            }
        }

        // La casa: el arte de map_home (P-M5) y el perezoso de su rig, colgado de la rama; sin cualquiera de los dos, nada.
        private void BuildHouse(RiverMap map)
        {
            _homeMap = PlacePoint(map, map.Home);
            Sprite? tree = _art.MapHome;
            if (tree != null && _houseSloth == null)
            {
                _houseSloth = SlothRig.Create(_houseLayer!, _art.SlothRigArt(), HomeSlothWorldDp);
            }

            _houseShown = tree != null && _houseSloth != null;
            _houseTree!.sprite = tree;
            if (_houseLayer!.gameObject.activeSelf != _houseShown)
            {
                _houseLayer.gameObject.SetActive(_houseShown);
            }

            _houseTree.gameObject.SetActive(_houseShown);
            if (_houseShown)
            {
                _houseSloth!.Play(SlothPoseId.Idle, ProgressReduceMotion);
            }
            else
            {
                _houseSloth?.SetVisible(false);
            }
        }

        // ============================================================================================ colocación

        // Las mini cuias en la orilla de su punto real o, en un racimo (a menos de ClusterMinDp), en abanico con su línea al punto.
        // Solo al cambiar de vista; sin asignaciones.
        private void PlaceStopMarkers(MapSpace space)
        {
            float contentW = _contentW;
            float contentH = _contentH;
            int n = _markerCount;
            for (int i = 0; i < n; i++)
            {
                StopMarker m = _markers[i];
                m.Real = space.ToLocal(m.MapPoint);
                m.Fanned = false;
                _clusterParent[i] = i;
            }

            float min = MapConfig.ClusterMinDp;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (Vector2.Distance(_markers[i].Real, _markers[j].Real) < min)
                    {
                        Union(i, j);
                    }
                }
            }

            _leaders!.Clear();
            for (int i = 0; i < n; i++)
            {
                if (Find(i) != i)
                {
                    continue; // cada racimo se coloca una vez, desde su raíz
                }

                int size = 0;
                Vector2 centroid = Vector2.zero;
                for (int j = 0; j < n; j++)
                {
                    if (Find(j) == i)
                    {
                        _clusterMembers[size++] = j;
                        centroid += _markers[j].Real;
                    }
                }

                centroid /= size;
                float side = centroid.x + MarkerSideDp + MinTouchDp > contentW ? -1f : 1f;
                if (size == 1)
                {
                    StopMarker m = _markers[_clusterMembers[0]];
                    m.Shown = ClampMarker(m.Real + new Vector2(side * MarkerSideDp, MarkerUpDp), contentW, contentH);
                    continue;
                }

                // De arriba abajo (y mayor primero): las líneas no se cruzan.
                for (int a = 1; a < size; a++)
                {
                    int v = _clusterMembers[a];
                    int b = a - 1;
                    while (b >= 0 && _markers[_clusterMembers[b]].Real.y < _markers[v].Real.y)
                    {
                        _clusterMembers[b + 1] = _clusterMembers[b];
                        b--;
                    }

                    _clusterMembers[b + 1] = v;
                }

                float step = FanSpanDeg / (size - 1) * Mathf.Deg2Rad;
                float radius = Mathf.Max(min, min / (2f * Mathf.Sin(step * 0.5f)));
                float start = FanSpanDeg * 0.5f * Mathf.Deg2Rad;
                for (int a = 0; a < size; a++)
                {
                    StopMarker m = _markers[_clusterMembers[a]];
                    float angle = start - (a * step);
                    Vector2 at = centroid + new Vector2(side * Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                    m.Shown = ClampMarker(at, contentW, contentH);
                    m.Fanned = true;
                    Vector2 foot = m.Shown + new Vector2(-side * ProceduralArt.MiniCuiaWidthDp * 0.5f, -ProceduralArt.MiniCuiaHeightDp * 0.25f);
                    _leaders.Add(foot, m.Real);
                }
            }

            for (int i = 0; i < n; i++)
            {
                StopMarker m = _markers[i];
                if (m.Root.anchoredPosition != m.Shown)
                {
                    m.Root.anchoredPosition = m.Shown;
                }
            }

            _leaders.Commit();
        }

        private static Vector2 ClampMarker(Vector2 p, float contentW, float contentH)
        {
            float half = MinTouchDp * 0.5f;
            return new Vector2(Mathf.Clamp(p.x, half, Mathf.Max(half, contentW - half)), Mathf.Clamp(p.y, -Mathf.Max(half, contentH - half), -half));
        }

        private int Find(int i)
        {
            while (_clusterParent[i] != i)
            {
                _clusterParent[i] = _clusterParent[_clusterParent[i]];
                i = _clusterParent[i];
            }

            return i;
        }

        private void Union(int a, int b)
        {
            int ra = Find(a);
            int rb = Find(b);
            if (ra != rb)
            {
                _clusterParent[Mathf.Max(ra, rb)] = Mathf.Min(ra, rb);
            }
        }

        // La casa en Guajará: el árbol con el pie en su punto y el perezoso colgado de la rama (más pequeños en la Cuenca).
        private void PlaceHouse(MapSpace space)
        {
            float scale = _view == MapView.World ? 1f : HomeBasinScale;
            _homeLocal = space.ToLocal(_homeMap) + new Vector2(-HomeSideDp * scale, 0f);
            _homeHeight = ProceduralArt.MapHomeHeightDp * scale;
            if (!_houseShown)
            {
                return;
            }

            RectTransform tree = _houseTree!.rectTransform;
            tree.anchoredPosition = _homeLocal;
            tree.sizeDelta = new Vector2(ProceduralArt.MapHomeWidthDp * scale, _homeHeight);
            float size = HomeSlothWorldDp * scale;
            SlothRig rig = _houseSloth!;
            if (!Mathf.Approximately(rig.Size, size))
            {
                rig.SetArt(rig.Art, size);
                rig.Seek(rig.ClockMs);
            }

            rig.Place(_homeLocal.x, -(_homeLocal.y + (_homeHeight * ProceduralArt.MapHomeBranchFraction)));
        }

        // La canoa (en su punto de ahora: al ponerse al día, va por el agua), con la proa hacia donde va; el mástil de popa con las
        // banderas, la más reciente arriba; y el guía delante por el agua. Sin asignaciones.
        private void PlaceCanoeGear(MapSpace space)
        {
            if (!_markersReady || _canoeGear == null)
            {
                return;
            }

            bool on = _hasCanoe;
            if (_canoeGear.gameObject.activeSelf != on)
            {
                _canoeGear.gameObject.SetActive(on);
            }

            if (!on)
            {
                _guideShown = false;
                return;
            }

            RiverMap map = _map!;
            RiverRoutes routes = _routes!;
            Vector2 canoe = space.ToLocal(_canoeShown);
            _canoe.rectTransform.anchoredPosition = canoe;

            // El guía: GuideLeadDp por delante por el agua; sin agua por delante, encima de la canoa.
            float dpu = Mathf.Max(1e-4f, space.DpPerUnit);
            _guideShown = true;
            _guideId = GuideAt(map, routes, _stops!, _canoeX);
            Vector2 guideLocal;
            if (GuidePoint(map, routes, _canoeX, GuideLeadDp / dpu, GuideMinAheadDp / dpu, out Vector2 ahead, out _guideRoute, out _guideFraction))
            {
                _guideMap = ahead;
                guideLocal = space.ToLocal(ahead);
            }
            else
            {
                _guideRoute = -1;
                _guideFraction = 0f;
                guideLocal = canoe + new Vector2(0f, GuideAboveDp);
                _guideMap = new Vector2(_canoeShown.x, _canoeShown.y - (GuideAboveDp / Mathf.Max(1e-4f, space.DpPerUnit)));
            }

            float dx = guideLocal.x - canoe.x;
            if (Mathf.Abs(dx) > 2f)
            {
                _canoeFacing = dx > 0f ? 1f : -1f;
            }

            var scale = new Vector3(_canoeFacing, 1f, 1f);
            if (_canoe.rectTransform.localScale != scale)
            {
                _canoe.rectTransform.localScale = scale;
            }

            Image guide = _guide!;
            Sprite sprite = string.Equals(_guideId, RiverJourney.GuideBufeo, StringComparison.Ordinal) ? _art.MapBufeo : _art.Rosa;
            if (guide.sprite != sprite)
            {
                guide.sprite = sprite;
            }

            if (!guide.gameObject.activeSelf)
            {
                guide.gameObject.SetActive(true);
            }

            float gw = _view == MapView.World ? GuideWorldDp : GuideBasinDp;
            RectTransform g = guide.rectTransform;
            g.sizeDelta = new Vector2(gw, gw * ProceduralArt.RosaHeightDp / ProceduralArt.RosaWidthDp);
            g.anchoredPosition = guideLocal;
            g.localScale = new Vector3(-_canoeFacing, 1f, 1f); // Rosa y el bufeo miran a la izquierda en su dibujo

            // El mástil en la popa y las banderas, de la más reciente (arriba) a la primera (abajo), ondeando hacia popa.
            if (_flagCount > 0)
            {
                float fw = ProceduralArt.MapFlagWidthDp * CanoeFlagScale;
                float fh = ProceduralArt.MapFlagHeightDp * CanoeFlagScale;
                float mastH = (2f * MastPadDp) + (_flagCount * (fh + CanoeFlagGapDp));
                Vector2 foot = canoe + new Vector2(-_canoeFacing * ProceduralArt.MapCanoeWidthDp * SternFraction, 2f);
                RectTransform mast = _mast!.rectTransform;
                mast.sizeDelta = new Vector2(MastWidthDp, mastH);
                mast.anchoredPosition = foot;
                for (int i = 0; i < _flagCount; i++)
                {
                    int fromTop = _flagCount - 1 - i;
                    RectTransform f = _flagImages[i]!.rectTransform;
                    f.sizeDelta = new Vector2(fw, fh);
                    f.anchoredPosition = new Vector2(foot.x, foot.y + mastH - MastPadDp - (fh * 0.5f) - (fromTop * (fh + CanoeFlagGapDp)));
                    f.localScale = new Vector3(-_canoeFacing, 1f, 1f);
                }
            }
        }

        /// <summary>
        /// Dónde nada el guía con la canoa en <paramref name="level"/> (fraccionario): <paramref name="lead"/> unidades del marco por
        /// delante por el agua de su ruta; si a la canoa le quedan menos hasta la parada pero al menos <paramref name="minAhead"/>, la
        /// espera en la parada; con menos, false (espera encima de la canoa: el tramo siguiente puede volver río abajo). Con la canoa
        /// ya en la parada, guía el tramo siguiente. Da la ruta (<paramref name="route"/>, la k de su parada) y la fracción de ella.
        /// False también si no queda ruta por delante (el final del catálogo). Sin asignaciones.
        /// </summary>
        public static bool GuidePoint(RiverMap map, RiverRoutes routes, float level, float lead, float minAhead, out Vector2 point,
            out int route, out float fraction)
        {
            const int per = RiverStopCatalog.LevelsPerStop;
            float x = Mathf.Max(0f, level);
            int s = x <= 0f ? 1 : Mathf.CeilToInt((x / per) - 1e-4f);
            float f = x <= 0f ? 0f : Mathf.Clamp01((x - (per * (s - 1))) / per);
            float left = lead;
            for (int guard = 0; guard < 4 && s < routes.Count; guard++, s++, f = 0f)
            {
                RiverStopRoute? leg = routes.At(s);
                if (leg == null)
                {
                    break;
                }

                float total = RouteLength(map, leg);
                if (total <= 0f)
                {
                    continue; // misma vista, otra luz: sin agua que nadar
                }

                float remaining = (1f - f) * total;
                if (left <= remaining)
                {
                    route = s;
                    fraction = f + (left / total);
                    point = RoutePoint(map, leg, fraction);
                    return true;
                }

                if (f < 1f)
                {
                    if (remaining >= minAhead)
                    {
                        route = s;
                        fraction = 1f;
                        point = RoutePoint(map, leg, 1f); // espera en la parada a la que va la canoa
                        return true;
                    }

                    break; // le falta muy poco: espera encima de la canoa
                }

                left -= remaining;
            }

            point = default;
            route = -1;
            fraction = 0f;
            return false;
        }

        // La ventanita, abajo a la izquierda de la zona del mapa: encima del rótulo del Atlántico y de la fila de «Volver a la
        // canoa», con la píldora a su derecha sin salirse de la zona.
        private void PlaceHomeWindow()
        {
            if (_homeWindow == null)
            {
                return;
            }

            float bottom = FramePadDp + _viewH - MinTouchDp - 8f; // la fila de «Volver a la canoa»
            TMPro.TextMeshProUGUI? atlantic = AtlanticLabel;
            if (atlantic != null && atlantic.gameObject.activeSelf)
            {
                bottom = Mathf.Min(bottom, -atlantic.rectTransform.anchoredPosition.y);
            }

            float top = Mathf.Max(FramePadDp + OrientationInsetDp, bottom - HomeWindowGapDp - HomeWindow.SizeDp);
            _homeWindow.Place(FramePadDp + OrientationInsetDp, top, FramePadDp + _viewW - OrientationInsetDp);
        }

        // La ventanita sale cuando la casa queda fuera de la vista (con su arte; nunca sin parada). Devuelve si se ve. Sin
        // asignaciones.
        private bool UpdateHomeWindow()
        {
            if (_homeWindow == null)
            {
                return false;
            }

            float vx = _offset.x + _homeLocal.x;
            float vy = _offset.y - (_homeLocal.y + (_homeHeight * 0.5f));
            float m = HomeInViewMarginDp;
            _homeInView = vx >= m && vx <= _viewW - m && vy >= m && vy <= _viewH - m;
            _homeWindow.Apply(_homeStop, _homeInView || !Visible);
            return _homeWindow.Visible;
        }

        // ============================================================================================ toques y sueño

        private void TapMarker(StopMarker marker)
        {
            if (!Visible)
            {
                return;
            }

            if (CatchUpTouch())
            {
                NoteTouch();
                return; // el primer toque solo completa «ponerse al día»
            }

            NoteTouch();
            _stopsTapped++;
            _lastTappedStop = marker.N;
            MarkersStopTapped(marker.N);
        }

        private void OnMapPointerDown()
        {
            if (!Visible)
            {
                return;
            }

            CatchUpTouch();
            NoteTouch();
        }

        // Un toque: vuelve a contar los 30 s y, si TiTino dormía, lo despierta (sloth_wake y, al acabar, sloth_idle).
        private void NoteTouch()
        {
            _idleMs = 0f;
            if (!_asleep)
            {
                return;
            }

            _asleep = false;
            _homeWindow?.SetAsleep(false);
            if (_houseSloth != null && _houseShown)
            {
                _houseSloth.Play(SlothPoseId.Wake, ProgressReduceMotion);
                _wakeLeftMs = SlothRig.WakeMs;
            }
        }

        // A los 30 s sin toques, TiTino dormita en su casa y en la ventanita (sloth_sleep, que sustituye a capi_sleep).
        private void Sleep()
        {
            _asleep = true;
            _wakeLeftMs = 0f;
            _homeWindow?.SetAsleep(true);
            if (_houseSloth != null && _houseShown)
            {
                _houseSloth.Play(SlothPoseId.Sleep, ProgressReduceMotion);
            }
        }

        // ============================================================================================ piezas

        // Una mini cuia del Mapa: su área táctil, el vidrio, el agua (dos en las mezclas) y la baliza de los cierres de mundo.
        private sealed class StopMarker
        {
            public StopMarker(RectTransform root, Button button, Image glass, Image waterA, Image waterB, Image beacon)
            {
                Root = root;
                Button = button;
                Glass = glass;
                WaterA = waterA;
                WaterB = waterB;
                Beacon = beacon;
                Stop = null!;
            }

            public RectTransform Root { get; }

            public Button Button { get; }

            public Image Glass { get; }

            public Image WaterA { get; }

            public Image WaterB { get; }

            public Image Beacon { get; }

            public RiverStop Stop { get; set; }

            public int N { get; set; }

            public Vector2 MapPoint { get; set; }

            public Vector2 Real { get; set; }

            public Vector2 Shown { get; set; }

            public bool Reached { get; set; }

            public bool Mixed { get; set; }

            public bool Fanned { get; set; }

            public bool BeaconLit { get; set; }
        }

        /// <summary>
        /// Las líneas finas del abanico de mini cuias hasta su punto real, con un punto en él (crema con canto oscuro), en una sola
        /// malla sin textura. Solo se reconstruye al recolocar; sin raycast.
        /// </summary>
        [RequireComponent(typeof(CanvasRenderer))]
        internal sealed class LeaderLinesGraphic : MaskableGraphic
        {
            private Vector2[] _from = Array.Empty<Vector2>();
            private Vector2[] _to = Array.Empty<Vector2>();
            private int _count;

            /// <summary>Líneas dibujadas.</summary>
            public int Count => _count;

            /// <summary>Prepara sitio para <paramref name="capacity"/> líneas (asigna solo si crece).</summary>
            public void Reserve(int capacity)
            {
                if (_from.Length < capacity)
                {
                    Array.Resize(ref _from, capacity);
                    Array.Resize(ref _to, capacity);
                }
            }

            /// <summary>Vacía las líneas.</summary>
            public void Clear()
            {
                _count = 0;
            }

            /// <summary>Añade una línea de <paramref name="from"/> (la mini cuia) a <paramref name="to"/> (su punto real).</summary>
            public void Add(Vector2 from, Vector2 to)
            {
                if (_count < _from.Length)
                {
                    _from[_count] = from;
                    _to[_count] = to;
                    _count++;
                }
            }

            /// <summary>Vuelve a dibujar.</summary>
            public void Commit()
            {
                SetVerticesDirty();
            }

            /// <inheritdoc />
            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                for (int i = 0; i < _count; i++)
                {
                    Segment(vh, _from[i], _to[i], (LeaderWidthDp * 0.5f) + 0.6f, LeaderRim);
                }

                for (int i = 0; i < _count; i++)
                {
                    Segment(vh, _from[i], _to[i], LeaderWidthDp * 0.5f, LeaderColor);
                    Disc(vh, _to[i], LeaderDotDp + 0.8f, LeaderRim);
                    Disc(vh, _to[i], LeaderDotDp, LeaderColor);
                }
            }

            private void Segment(VertexHelper vh, Vector2 a, Vector2 b, float half, Color tint)
            {
                Vector2 dir = b - a;
                float length = dir.magnitude;
                if (length <= 0f)
                {
                    return;
                }

                dir /= length;
                Vector2 n = new Vector2(-dir.y, dir.x) * half;
                Color32 c = tint * color;
                int i = vh.currentVertCount;
                vh.AddVert(a - n, c, Vector2.zero);
                vh.AddVert(a + n, c, Vector2.zero);
                vh.AddVert(b + n, c, Vector2.zero);
                vh.AddVert(b - n, c, Vector2.zero);
                vh.AddTriangle(i, i + 1, i + 2);
                vh.AddTriangle(i, i + 2, i + 3);
            }

            private void Disc(VertexHelper vh, Vector2 c, float r, Color tint)
            {
                const int segments = 10;
                Color32 c32 = tint * color;
                int start = vh.currentVertCount;
                vh.AddVert(c, c32, Vector2.zero);
                for (int k = 0; k < segments; k++)
                {
                    float a = k * (Mathf.PI * 2f / segments);
                    vh.AddVert(c + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r), c32, Vector2.zero);
                }

                for (int k = 0; k < segments; k++)
                {
                    vh.AddTriangle(start, start + 1 + k, start + 1 + ((k + 1) % segments));
                }
            }
        }
    }
}
