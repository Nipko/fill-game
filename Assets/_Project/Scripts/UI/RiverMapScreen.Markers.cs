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
    /// <item><description><b>Las mini cuias</b> de las paradas de los mundos ya abiertos (hasta el de «Jugar»), sobre el río en su
    /// punto real (propuesta §7: «es el marcador de la parada»): llenas del agua del sitio las alcanzadas, de vidrio con su canto
    /// crema las que faltan. Se tocan con un área de ≥ 48 dp (<see cref="MinTouchDp"/>) y nunca quedan a menos de
    /// <see cref="MapConfig.ClusterMinDp"/> dp entre centros: si dos puntos reales están más cerca, el racimo se abre en
    /// <b>abanico</b> (centros a ≥ <see cref="FanGapDp"/> dp, fuera del río) con una línea fina hasta cada punto real (propuesta
    /// §3.2: la geometría nunca se deforma); y si lo que se pinta choca después, se unen los racimos y se vuelve a colocar. Ninguna
    /// pisa la canoa con su mástil y su guía ni la fila corta de boyas: la que caería encima se aparta lo justo, con su línea al
    /// punto real (<see cref="PlaceStopMarkers"/>). Solo en la vista Mundo: en la Cuenca, a 1×, no caben.</description></item>
    /// <item><description><b>La baliza</b> de los cierres de mundo (<c>kind</c> = <c>close</c>; propuesta §4.1), junto a su mini
    /// cuia (del lado que no tape la canoa ni otra mini cuia): encendida, con su halo ámbar, al cerrar el mundo, es decir, al
    /// ganar el último nivel de esa parada (40, 80 y 140 en 1-200; <see cref="BeaconLit"/>). Sin «Premio del mundo».</description></item>
    /// <item><description><b>La casa de TiTino</b> fija en Guajará (<see cref="RiverMap.Home"/>; c.3: no viaja): su embaúba
    /// (<see cref="ProceduralArt.MapHome"/>) con el perezoso colgado (<see cref="SlothRig"/>, <c>sloth_idle</c>), que dormita a los
    /// <see cref="MapConfig.MapIdleSleepMs"/> ms sin toques (<c>sloth_sleep</c>, que sustituye a <c>capi_sleep</c>) y despierta
    /// con el siguiente (<c>sloth_wake</c>). Cuando la casa queda fuera de la vista, la <b>ventanita de casa</b>
    /// (<see cref="HomeWindow"/>, abajo a la izquierda de la zona del mapa, con la píldora debajo del retrato) con «Casa · a unos
    /// {km} km» (<c>map_home_distance</c>, los km de la parada de «Jugar»); tocarla lleva la vista a la casa (<see cref="GoHome"/>).
    /// El encuadre se corre lo justo para que no quede a menos de <see cref="HomeWindowCuiaGapDp"/> dp de una mini cuia, ni una mini
    /// cuia a menos de <see cref="FixedOverlayGapDp"/> dp de los rótulos de orientación o de «Volver a la canoa»; una placa de
    /// mundo futuro que ni así queda libre se pone al otro lado del punto de su mundo (<see cref="ResolvePlates"/>). Arte
    /// provisional de P-M5: en release, sin su PNG, ni casa ni ventanita.</description></item>
    /// </list>
    /// <para>Las piezas se crean una vez (al construir la geometría y, las mini cuias, al crecer la reserva al abrir); al cambiar de
    /// vista solo se recolocan; con el Mapa quieto, <see cref="MarkersTick"/> no asigna memoria (el reloj del perezoso y la
    /// comprobación de si la casa se ve).</para>
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Banderas que caben en el mástil (las de la 1.0 son 5: BR, CO, PE, EC y BO; propuesta §12, pieza 12).</summary>
        public const int MaxCanoeFlags = 8;

        /// <summary>
        /// Escala de las banderas en el mástil de la canoa del Mapa [P]: 1, las 16 × 11 dp del banner (revisión de M3.5: a 0,75 el
        /// escudo de Ecuador no se distinguía en el teléfono).
        /// </summary>
        public const float CanoeFlagScale = 1f;

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

        /// <summary>Distancia entre los centros de dos mini cuias de un abanico (dp) [P]: los 48 dp del toque y 4 de aire.</summary>
        public const float FanGapDp = 52f;

        /// <summary>Arco del abanico de un racimo de mini cuias (grados) [P].</summary>
        public const float FanSpanDeg = 140f;

        /// <summary>Arco del abanico cuando con <see cref="FanSpanDeg"/> no cabe (grados) [P]: el radio baja.</summary>
        public const float FanSpanWideDeg = 200f;

        /// <summary>Lo que crece el radio del abanico en cada intento si no cabe (dp) [P].</summary>
        public const float FanRadiusStepDp = 12f;

        /// <summary>
        /// Radio más grande de un abanico en arco (dp) [P]: un racimo que no cabe en un arco así (más de 6 paradas, como las del
        /// relevo de Bolivia en el 306, o un arco que pisaría la canoa) se reparte alrededor de sus puntos reales, cada mini cuia en
        /// el sitio libre más cercano al suyo (<see cref="FanPackMaxDp"/>).
        /// </summary>
        public const float FanMaxRadiusDp = 80f;

        /// <summary>Lo más lejos de su punto real que se busca sitio para una mini cuia de un racimo repartido (dp) [P].</summary>
        public const float FanPackMaxDp = 160f;

        /// <summary>Hueco que deja una mini cuia apartada o en abanico con el borde del trazo del río (dp) [P].</summary>
        public const float MarkerLineClearanceDp = 6f;

        /// <summary>Hueco entre una mini cuia y la caja de la canoa con su equipo, la del guía y la fila corta de boyas (dp) [P].</summary>
        public const float MarkerObstacleGapDp = 4f;

        /// <summary>Paso con que se busca sitio para una mini cuia que pisaría la canoa (dp) [P].</summary>
        public const float MarkerShiftStepDp = 8f;

        /// <summary>Lo más que se aparta de su punto real una mini cuia suelta que pisaría la canoa (dp) [P].</summary>
        public const float MarkerMaxShiftDp = 96f;

        /// <summary>Ancho de la línea fina hasta el punto real (dp) [P], sin su canto oscuro (0,6 dp por lado).</summary>
        public const float LeaderWidthDp = 2f;

        /// <summary>Radio del punto real de una mini cuia abierta en abanico (dp) [P].</summary>
        public const float LeaderDotDp = 2.2f;

        /// <summary>
        /// La casa va a un lado de Guajará, al menos este tanto (dp, a la escala de la vista) [P]: primero a la izquierda y, si el
        /// tronco cae en el agua, más lejos o al otro lado (<see cref="HomeLineClearanceDp"/>).
        /// </summary>
        public const float HomeSideDp = 40f;

        /// <summary>Lo más lejos de Guajará que se busca tierra para la casa (dp, a la escala de la vista) [P].</summary>
        public const float HomeSideMaxDp = 120f;

        /// <summary>Hueco del tronco de la casa (y su pie de orilla) con el borde del trazo del río (dp) [P]: el tronco en tierra.</summary>
        public const float HomeLineClearanceDp = 6f;

        /// <summary>Medio ancho del tronco de la casa con su pie de orilla, para buscarle tierra (dp, a 1×) [P].</summary>
        public const float HomeTrunkHalfDp = 10f;

        /// <summary>
        /// Opacidad del vidrio de la mini cuia de una parada que falta [P] (revisión de M3.5: a 0,55 casi desaparecía sobre el verde
        /// del mapa); con su canto crema (<see cref="UnreachedRimColor"/>) se lee como vidrio vacío y tocable.
        /// </summary>
        public const float UnreachedGlassAlpha = 0.8f;

        /// <summary>Lado del perezoso de la casa en la vista Mundo (dp) [P].</summary>
        public const float HomeSlothWorldDp = 30f;

        /// <summary>Escala de la casa (y de su perezoso) en la vista Cuenca, a 1× [P].</summary>
        public const float HomeBasinScale = 0.7f;

        /// <summary>Margen con que la casa tiene que quedar dentro de la zona del mapa para contar como «a la vista» (dp) [P].</summary>
        public const float HomeInViewMarginDp = 8f;

        /// <summary>Hueco entre la ventanita de casa y lo que tiene debajo (el rótulo del Atlántico, «Volver a la canoa») (dp) [P].</summary>
        public const float HomeWindowGapDp = 6f;

        /// <summary>
        /// La ventanita de casa (marco y píldora) nunca queda a menos de este tanto de una mini cuia (dp) [P]: si pasaría, el
        /// encuadre se corre (<see cref="MarkersFrame"/>).
        /// </summary>
        public const float HomeWindowCuiaGapDp = 8f;

        /// <summary>Paso con que se busca el encuadre más cercano que deja libre la ventanita de casa (dp) [P].</summary>
        public const float FrameStepDp = 4f;

        /// <summary>
        /// Al encuadrar, ninguna mini cuia queda a menos de este tanto de los rótulos «Andes ↑» y «Océano Atlántico ↓» ni de «Volver a
        /// la canoa» cuando sale (dp) [P]: lo fijo del marco no tapa una mini cuia tocable (<see cref="MarkersFrame"/>).
        /// </summary>
        public const float FixedOverlayGapDp = 8f;

        /// <summary>Anillos de <see cref="FrameStepDp"/> alrededor del encuadre pedido que se prueban antes que la rejilla gruesa [P].</summary>
        public const int FrameNearRings = 6;

        /// <summary>Paso de la rejilla gruesa con que se busca encuadre si cerca no sale uno limpio (dp) [P]; luego se afina.</summary>
        public const float FrameCoarseStepDp = 16f;

        /// <summary>Canto de la mini cuia de una parada que falta: crema casi opaco, como el canto de las boyas.</summary>
        public static readonly Color UnreachedRimColor = new Color(Palette.Cream.r, Palette.Cream.g, Palette.Cream.b, 0.85f);

        /// <summary>Halo de la baliza encendida: el ámbar de la llama, translúcido.</summary>
        public static readonly Color BeaconHaloColor = new Color(ProceduralArt.BeaconFlame.r, ProceduralArt.BeaconFlame.g, ProceduralArt.BeaconFlame.b, ProceduralArt.BeaconHaloAlpha);

        // Las ocho direcciones en que se busca sitio (primero las del eje).
        private static readonly Vector2[] Dirs8 =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(0.7071f, 0.7071f), new Vector2(-0.7071f, 0.7071f), new Vector2(0.7071f, -0.7071f), new Vector2(-0.7071f, -0.7071f),
        };

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
        private int[] _placedIdx = Array.Empty<int>();
        private int[] _fanOrder = Array.Empty<int>();
        private int[] _bestOrder = Array.Empty<int>();
        private Vector2[] _fanPos = Array.Empty<Vector2>();
        private Vector2[] _bestPos = Array.Empty<Vector2>();
        private readonly Rect[] _obstacles = new Rect[3];
        private int _obstacleCount;
        private int _placedCount;
        private Rect _gearBox;
        private Rect _guideBox;
        private float _gearFacing = 1f;
        private bool _hasRim;
        private Vector2 _frameShift;
        private Vector2 _frameStart;
        private Vector2 _frameBest;
        private float _frameBestMag;
        private int _frameBestHits;
        private int _frameEvaluations;
        private bool _frameWindow;
        private Rect _frameWin;
        private Rect _frameAndes;
        private Rect _frameAtlantic;
        private Rect _frameButton;

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

        /// <summary>
        /// La mini cuia <paramref name="i"/> no va en su punto real (en abanico o apartada de la canoa): lleva su línea fina hasta él.
        /// </summary>
        public bool StopMarkerHasLeader(int i)
        {
            return _markers[i].HasLeader;
        }

        /// <summary>El centro de la mini cuia <paramref name="i"/> cae dentro de la zona visible del mapa (vista Mundo).</summary>
        public bool StopMarkerInView(int i)
        {
            if (!StopMarkersShown)
            {
                return false;
            }

            Vector2 p = StopMarkerViewPoint(i);
            return p.x >= 0f && p.x <= _viewW && p.y >= 0f && p.y <= _viewH;
        }

        /// <summary>Centro de la mini cuia <paramref name="i"/> en coordenadas de la zona visible del mapa (dp, y hacia abajo).</summary>
        public Vector2 StopMarkerViewPoint(int i)
        {
            return new Vector2(_offset.x + _markers[i].Shown.x, _offset.y - _markers[i].Shown.y);
        }

        /// <summary>La mini cuia <paramref name="i"/> lleva el canto crema de las paradas que faltan.</summary>
        public bool StopMarkerRimShown(int i)
        {
            return _markers[i].Rim.gameObject.activeSelf;
        }

        /// <summary>La baliza de la mini cuia <paramref name="i"/> lleva su halo ámbar (encendida).</summary>
        public bool StopMarkerBeaconHaloShown(int i)
        {
            return _markers[i].Halo.gameObject.activeSelf;
        }

        /// <summary>Caja de la baliza de la mini cuia <paramref name="i"/> en coordenadas locales del contenido (dp, y hacia arriba).</summary>
        public Rect StopMarkerBeaconLocal(int i)
        {
            StopMarker m = _markers[i];
            return BeaconBox(m.Shown, m.BeaconLeft);
        }

        /// <summary>
        /// Caja de la canoa con su mástil y sus banderas tal como queda con <c>max_level</c> (coordenadas locales del contenido): lo
        /// que ninguna mini cuia pisa.
        /// </summary>
        public Rect CanoeGearBoxLocal => _gearBox;

        /// <summary>Caja del guía tal como queda con <c>max_level</c> (coordenadas locales del contenido).</summary>
        public Rect GuideBoxLocal => _guideBox;

        /// <summary>
        /// Lo que el último encuadre (cambio de vista o «Volver a la canoa») se corrió para que la ventanita de casa, los rótulos de
        /// orientación o «Volver a la canoa» no pisaran una mini cuia (dp, la esquina del contenido; y hacia abajo); cero si no hizo
        /// falta.
        /// </summary>
        public Vector2 FrameShiftDp => _frameShift;

        /// <summary>Encuadres que probó el último <see cref="MarkersFrame"/> (cada uno recorre las mini cuias y las placas).</summary>
        internal int FrameEvaluations => _frameEvaluations;

        /// <summary>Lo que cuesta el encuadre que eligió el último <see cref="MarkersFrame"/> (0: nada pegado ni cortado).</summary>
        internal int FrameCost => _frameBestHits;

        /// <summary>Caja de la ventanita de casa con su píldora en coordenadas de la zona visible del mapa (dp, y hacia abajo).</summary>
        public Rect HomeWindowViewRect => _homeWindow != null ? HomeWindowInView() : default;

        /// <summary>Caja del rótulo «Andes ↑» en coordenadas de la zona visible del mapa (dp, y hacia abajo); vacía sin texto.</summary>
        internal Rect AndesLabelViewRect => FixedViewRect(AndesLabel);

        /// <summary>Caja del rótulo «Océano Atlántico ↓» en coordenadas de la zona visible del mapa (dp, y hacia abajo).</summary>
        internal Rect AtlanticLabelViewRect => FixedViewRect(AtlanticLabel);

        /// <summary>Caja de «Volver a la canoa» en coordenadas de la zona visible del mapa (dp, y hacia abajo), salga o no.</summary>
        internal Rect BackToCanoeViewRect => FixedViewRect((RectTransform)_backToCanoe.Button.transform);

        /// <summary>Agua de la mini cuia <paramref name="i"/> (tests: el fundido de Reducir movimiento).</summary>
        internal Image StopMarkerWater(int i)
        {
            return _markers[i].WaterA;
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
                _homeWindow.PillBelow = true; // la píldora bajo el retrato: no cruza la zona de juego del Mapa
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

            PlaceCanoeGear(space);
            if (world)
            {
                PlaceStopMarkers(space);
            }
            else
            {
                SetCompactRowDrop(space, 0f);
            }

            PlaceHouse(space);
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

        partial void MarkersFrame(ref Vector2 offset)
        {
            _frameShift = Vector2.zero;
            _frameEvaluations = 0;
            _frameBestHits = 0;
            if (!_markersReady || _view != MapView.World || _markerCount == 0)
            {
                return;
            }

            // Lo que queda fijo en el marco encima del mapa, con su hueco: la ventanita de casa (marco y píldora; solo si tiene
            // arte), los rótulos «Andes ↑» y «Océano Atlántico ↓» y «Volver a la canoa» (solo con los encuadres que lo enseñan).
            // Ninguna mini cuia ni placa de mundo futuro debe quedar debajo ni pegada. Si alguna lo estaría, el encuadre se corre lo
            // menos posible, hasta un 30 % de la vista en cada eje y sin que el borde del mapa lo recorte, y gana el que no deja
            // ningún choque (o el que menos cuesta) y, entre ellos, el más corto (FrameHits). Cuenta también como choque sacar del
            // todo o en parte de la vista algo que se veía entero (la canoa con su equipo, una mini cuia o una placa). Se busca en
            // tres pasos para no recorrer los miles de corrimientos de la rejilla fina: primero por anillos de FrameStepDp alrededor
            // del encuadre pedido (hasta FrameNearRings, y se para en cuanto ningún corrimiento del anillo puede ser más corto que uno
            // limpio ya hallado); si ahí no sale limpio, en una rejilla gruesa de FrameCoarseStepDp por todo el margen; y después se
            // afina con FrameStepDp alrededor de la mejor. Solo al cambiar de vista o de encuadre; sin asignaciones.
            _frameWindow = _homeWindow != null && _homeWindow.Ready;
            _frameWin = _frameWindow ? HomeWindowInView() : default;
            _frameAndes = FixedViewRect(AndesLabel);
            _frameAtlantic = FixedViewRect(AtlanticLabel);
            _frameButton = FixedViewRect((RectTransform)_backToCanoe.Button.transform);
            Vector2 start = new Vector2(ClampAxis(offset.x, _contentW, _viewW), ClampAxis(offset.y, _contentH, _viewH));
            _frameStart = start;
            _frameBest = start;
            _frameBestMag = 0f;
            _frameBestHits = FrameHits(start);
            _frameEvaluations = 1;
            if (_frameBestHits == 0)
            {
                return;
            }

            int nx = Mathf.FloorToInt(_viewW * 0.3f / FrameStepDp);
            int ny = Mathf.FloorToInt(_viewH * 0.3f / FrameStepDp);
            int nearRings = Mathf.Min(FrameNearRings, Mathf.Max(nx, ny));
            for (int r = 1; r <= nearRings; r++)
            {
                float ring = r * FrameStepDp;
                if (_frameBestHits == 0 && ring * ring >= _frameBestMag)
                {
                    break; // ningún corrimiento de este anillo es más corto que el limpio ya hallado
                }

                for (int iy = -r; iy <= r; iy++)
                {
                    for (int ix = -r; ix <= r; ix++)
                    {
                        if ((ix == -r || ix == r || iy == -r || iy == r) && Mathf.Abs(ix) <= nx && Mathf.Abs(iy) <= ny)
                        {
                            TryFrame(ix, iy);
                        }
                    }
                }
            }

            if (_frameBestHits > 0)
            {
                int k = Mathf.Max(1, Mathf.RoundToInt(FrameCoarseStepDp / FrameStepDp));
                for (int iy = -(ny / k) * k; iy <= ny; iy += k)
                {
                    for (int ix = -(nx / k) * k; ix <= nx; ix += k)
                    {
                        if (Mathf.Max(Mathf.Abs(ix), Mathf.Abs(iy)) > nearRings)
                        {
                            TryFrame(ix, iy); // los de los anillos cercanos ya se probaron
                        }
                    }
                }

                Vector2 d = (_frameBest - start) / FrameStepDp;
                int cx = Mathf.RoundToInt(d.x);
                int cy = Mathf.RoundToInt(d.y);
                for (int iy = cy - k + 1; iy <= cy + k - 1; iy++)
                {
                    for (int ix = cx - k + 1; ix <= cx + k - 1; ix++)
                    {
                        if (Mathf.Abs(ix) <= nx && Mathf.Abs(iy) <= ny)
                        {
                            TryFrame(ix, iy);
                        }
                    }
                }
            }

            offset = _frameBest;
            _frameShift = _frameBest - start;
            if (_frameBestHits > 0)
            {
                ResolvePlates(_frameBest);
                _frameBestHits = FrameHits(_frameBest); // lo que queda
            }
        }

        // Si con el encuadre elegido una placa de mundo futuro (M3.4) sigue pegada a la ventanita, a un rótulo de orientación o a
        // «Volver a la canoa» (con el mismo hueco que en FrameHits), se coloca al otro lado del punto de su mundo: encima, a
        // FixedOverlayGapDp, o a su derecha o su izquierda como la pone M3.4; y si no, debajo; subiendo o bajando de
        // LabelSearchStepDp en LabelSearchStepDp hasta SilhouetteMaxShiftDp, como M3.4. Gana el primero que queda entero a la vista,
        // dentro del mapa, libre de todo eso y sin pisar la canoa con su equipo, el guía, una mini cuia ni otra placa; si ninguno,
        // la placa se queda donde estaba. El cambio de vista la vuelve a colocar (M3.4). Sin asignaciones.
        private void ResolvePlates(Vector2 o)
        {
            bool window = _frameWindow && !HomeInViewAt(o);
            bool button = CanoeButtonAt(o);
            var view = new Rect(0f, 0f, _viewW, _viewH);
            for (int i = 0; i < _silhouettes.Count; i++)
            {
                RectTransform root = _silhouettes[i].Root;
                if (!root.gameObject.activeSelf || !FixedHit(InView(PlateBox(root), o), window, button))
                {
                    continue;
                }

                Vector2 size = root.sizeDelta;
                Vector2 p = _space.ToLocal(_silhouettes[i].MapPoint);
                int steps = Mathf.RoundToInt(SilhouetteMaxShiftDp / LabelSearchStepDp);
                bool done = false;
                for (int k = 0; k <= steps && !done; k++)
                {
                    float dy = k * LabelSearchStepDp;
                    for (int c = 0; c < 6 && !done; c++)
                    {
                        // Encima del punto, a su derecha y a su izquierda subiendo dy; y lo mismo por debajo, bajando dy (como M3.4,
                        // como mucho SilhouetteMaxShiftDp en vertical).
                        float up = c < 3 ? dy : -dy;
                        float x = (c % 3) == 0 ? p.x - (size.x * 0.5f) : (c % 3) == 1 ? p.x + SilhouetteGapDp : p.x - SilhouetteGapDp - size.x;
                        float y = (c % 3) != 0 ? p.y + up - (size.y * 0.5f) : c == 0 ? p.y + FixedOverlayGapDp + dy : p.y - FixedOverlayGapDp - dy - size.y;
                        x = Mathf.Clamp(x, LabelEdgeDp, Mathf.Max(LabelEdgeDp, _contentW - LabelEdgeDp - size.x));
                        var box = new Rect(x, y, size.x, size.y);
                        if (box.yMax > -LabelEdgeDp || box.yMin < -_contentH + LabelEdgeDp)
                        {
                            continue;
                        }

                        Rect seen = InView(box, o);
                        if (!Inside(seen, view) || FixedHit(seen, window, button) || PlateBlocked(i, box))
                        {
                            continue;
                        }

                        root.anchoredPosition = new Vector2(box.xMin, box.center.y);
                        done = true;
                    }
                }
            }
        }

        // Caja de la placa de una silueta en coordenadas locales del contenido (y hacia arriba): su pivote va a la izquierda y en
        // medio del alto.
        private static Rect PlateBox(RectTransform root)
        {
            Vector2 size = root.sizeDelta;
            return new Rect(root.anchoredPosition.x, root.anchoredPosition.y - (size.y * 0.5f), size.x, size.y);
        }

        // La caja (en la zona visible) queda a menos de su hueco de la ventanita, de un rótulo de orientación o de «Volver a la
        // canoa» (estas dos, solo si salen con ese encuadre).
        private bool FixedHit(Rect seen, bool window, bool button)
        {
            return (window && seen.Overlaps(Grow(_frameWin, HomeWindowCuiaGapDp)))
                || (button && seen.Overlaps(Grow(_frameButton, FixedOverlayGapDp)))
                || (_frameAndes.width > 0f && seen.Overlaps(Grow(_frameAndes, FixedOverlayGapDp)))
                || (_frameAtlantic.width > 0f && seen.Overlaps(Grow(_frameAtlantic, FixedOverlayGapDp)));
        }

        // Una placa en box (local) pisaría la canoa con su equipo, el guía, una mini cuia u otra placa.
        private bool PlateBlocked(int self, Rect box)
        {
            if ((_gearBox.width > 0f && box.Overlaps(Grow(_gearBox, SilhouetteCanoeClearanceDp))) || (_guideBox.width > 0f && box.Overlaps(_guideBox)))
            {
                return true;
            }

            for (int k = 0; k < _markerCount; k++)
            {
                if (box.Overlaps(Grow(CuiaBox(_markers[k].Shown), MarkerObstacleGapDp)))
                {
                    return true;
                }
            }

            for (int k = 0; k < _silhouettes.Count; k++)
            {
                RectTransform other = _silhouettes[k].Root;
                if (k != self && other.gameObject.activeSelf && box.Overlaps(PlateBox(other)))
                {
                    return true;
                }
            }

            return false;
        }

        // Prueba el corrimiento (ix, iy) × FrameStepDp del encuadre pedido y se queda con él si cuesta menos (o lo mismo y es más
        // corto). Sin asignaciones.
        private void TryFrame(int ix, int iy)
        {
            Vector2 d = new Vector2(ix * FrameStepDp, iy * FrameStepDp);
            float mag = d.sqrMagnitude;
            if (_frameBestHits == 0 && mag >= _frameBestMag)
            {
                return;
            }

            Vector2 want = _frameStart + d;
            Vector2 got = new Vector2(ClampAxis(want.x, _contentW, _viewW), ClampAxis(want.y, _contentH, _viewH));
            if ((got - want).sqrMagnitude > 0.25f)
            {
                return; // el borde del mapa no deja
            }

            _frameEvaluations++;
            int hits = FrameHits(got);
            if (hits < _frameBestHits || (hits == _frameBestHits && mag < _frameBestMag))
            {
                _frameBestHits = hits;
                _frameBestMag = mag;
                _frameBest = got;
            }
        }

        // Lo que cuesta el encuadre con el contenido en o (lo fijo, en coordenadas de la zona visible):
        // - por mini cuia: 100 si queda a menos de HomeWindowCuiaGapDp de la ventanita (si con ese encuadre la casa se ve, no hay
        //   ventanita), 50 si queda a menos de FixedOverlayGapDp de «Volver a la canoa» (si con ese encuadre sale: el toque se lo
        //   llevaría el botón) y 30 por cada rótulo de orientación al que quede a menos de FixedOverlayGapDp;
        // - por placa de mundo futuro (M3.4): 10 por cada una de esas tres cosas que pise con el mismo hueco;
        // - por lo que se veía entero con el contenido en el encuadre pedido y ya no: 100 la canoa con su equipo, 20 una mini cuia
        //   y 10 una placa.
        // Lo primero es que ninguna mini cuia toque la ventanita. Sin asignaciones.
        private int FrameHits(Vector2 o)
        {
            Vector2 start = _frameStart;
            bool window = _frameWindow && !HomeInViewAt(o);
            bool button = CanoeButtonAt(o);
            Rect nearWin = Grow(_frameWin, HomeWindowCuiaGapDp);
            Rect nearAndes = Grow(_frameAndes, FixedOverlayGapDp);
            Rect nearAtlantic = Grow(_frameAtlantic, FixedOverlayGapDp);
            Rect nearButton = Grow(_frameButton, FixedOverlayGapDp);
            var view = new Rect(0f, 0f, _viewW, _viewH);
            int hits = 0;
            if (_gearBox.width > 0f && Cut(_gearBox, o, start, view))
            {
                hits += 100;
            }

            for (int i = 0; i < _markerCount + _silhouettes.Count; i++)
            {
                Rect local;
                if (i < _markerCount)
                {
                    local = CuiaBox(_markers[i].Shown);
                }
                else
                {
                    RectTransform plate = _silhouettes[i - _markerCount].Root;
                    if (!plate.gameObject.activeSelf)
                    {
                        continue;
                    }

                    local = PlateBox(plate);
                }

                bool cuia = i < _markerCount;
                Rect seen = InView(local, o);
                if (window && seen.Overlaps(nearWin))
                {
                    hits += cuia ? 100 : 10;
                }

                if (button && seen.Overlaps(nearButton))
                {
                    hits += cuia ? 50 : 10;
                }

                if (_frameAndes.width > 0f && seen.Overlaps(nearAndes))
                {
                    hits += cuia ? 30 : 10;
                }

                if (_frameAtlantic.width > 0f && seen.Overlaps(nearAtlantic))
                {
                    hits += cuia ? 30 : 10;
                }

                if (Cut(local, o, start, view))
                {
                    hits += cuia ? 20 : 10;
                }
            }

            return hits;
        }

        // «Volver a la canoa» sale con el contenido en o: la canoa queda fuera de la vista (con su margen) y el botón movería la
        // vista (lo mismo que decide SetOffset). Sin asignaciones.
        private bool CanoeButtonAt(Vector2 o)
        {
            if (!_hasCanoe)
            {
                return false;
            }

            Vector2 local = _space.ToLocal(_canoeMap);
            float px = o.x + local.x;
            float py = o.y - local.y;
            if (px >= CanoeMarginDp && px <= _viewW - CanoeMarginDp && py >= CanoeMarginDp && py <= _viewH - CanoeMarginDp)
            {
                return false;
            }

            Vector2 target = CanoeCenteredOffset();
            return Mathf.Abs(target.x - o.x) > 0.5f || Mathf.Abs(target.y - o.y) > 0.5f;
        }

        // Caja de algo fijo del marco (un rótulo, el botón) en coordenadas de la zona visible (dp, y hacia abajo); vacía si no hay
        // nada que pintar (un rótulo sin texto).
        private Rect FixedViewRect(TMPro.TextMeshProUGUI? label)
        {
            if (label == null || !label.gameObject.activeSelf || string.IsNullOrEmpty(label.text))
            {
                return default;
            }

            return FixedViewRect(label.rectTransform);
        }

        private static Rect FixedViewRect(RectTransform rt)
        {
            Vector2 p = rt.anchoredPosition;
            Vector2 size = rt.sizeDelta;
            return new Rect(p.x - FramePadDp, -p.y - FramePadDp, size.x, size.y);
        }

        // Caja local (y hacia arriba) en coordenadas de la zona visible (y hacia abajo) con el contenido en o.
        private static Rect InView(Rect local, Vector2 o)
        {
            return Rect.MinMaxRect(o.x + local.xMin, o.y - local.yMax, o.x + local.xMax, o.y - local.yMin);
        }

        // La caja se veía entera con el contenido en start y con el contenido en o ya no.
        private static bool Cut(Rect local, Vector2 o, Vector2 start, Rect view)
        {
            return Inside(InView(local, start), view) && !Inside(InView(local, o), view);
        }

        private static bool Inside(Rect r, Rect view)
        {
            return r.xMin >= view.xMin && r.yMin >= view.yMin && r.xMax <= view.xMax && r.yMax <= view.yMax;
        }

        /// <summary>
        /// Lleva la vista a la mini cuia de la parada <paramref name="n"/>, centrada (capturas y <c>adb</c>), sin cambiar de vista.
        /// </summary>
        internal bool CenterOnStopMarker(int n)
        {
            int i = StopMarkerIndexOf(n);
            if (!Visible || i < 0 || !StopMarkersShown)
            {
                return false;
            }

            Vector2 local = _markers[i].Shown;
            Vector2 offset = new Vector2((_viewW * 0.5f) - local.x, (_viewH * 0.5f) + local.y);
            MarkersFrame(ref offset);
            SetOffset(offset);
            UpdateHomeWindow();
            EmitLayout();
            return true;
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
                    bool rim = !reached && _hasRim;
                    if (m.Rim.gameObject.activeSelf != rim)
                    {
                        m.Rim.gameObject.SetActive(rim);
                    }

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
                    m.Halo.gameObject.SetActive(lit);
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
            _hasRim = ArtSlots.Find(ProceduralArt.MiniCuiaSlot) == null; // el canto es de la silueta provisional
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
                if (m.Rim.gameObject.activeSelf != _hasRim)
                {
                    m.Rim.gameObject.SetActive(_hasRim);
                }

                m.BeaconLit = false;
                m.Halo.gameObject.SetActive(false);
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
                int size = _markers.Count;
                _clusterParent = new int[size];
                _clusterMembers = new int[size];
                _placedIdx = new int[size];
                _fanOrder = new int[size];
                _bestOrder = new int[size];
                _fanPos = new Vector2[size];
                _bestPos = new Vector2[size];
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

            // Detrás de todo, el canto crema de las que faltan; el agua va encima del vidrio, como en la franja de llegada (el
            // vidrio provisional tiene el interior casi opaco).
            Image rim = MarkerPiece("Rim", root, _art.MapMiniCuiaRim);
            rim.rectTransform.sizeDelta = new Vector2(ProceduralArt.MiniCuiaWidthDp + (2f * ProceduralArt.MiniCuiaRimPadDp),
                ProceduralArt.MiniCuiaHeightDp + (2f * ProceduralArt.MiniCuiaRimPadDp));
            rim.color = UnreachedRimColor;
            rim.raycastTarget = false;
            Image glass = MarkerPiece("MiniCuia", root, _art.MapMiniCuia);
            Image waterA = MarkerPiece("Water", root, _art.MapMiniCuiaWater);
            Image waterB = MarkerPiece("WaterMeet", root, _art.MapMiniCuiaWater);
            foreach (Image water in new[] { waterA, waterB })
            {
                water.type = Image.Type.Filled;
                water.fillMethod = Image.FillMethod.Vertical;
                water.fillOrigin = (int)Image.OriginVertical.Bottom;
            }

            // La baliza con su halo detrás; su lado lo decide la colocación (PlaceBeacon).
            Image halo = UiBuilder.Image("BeaconHalo", root, _art.MapBeaconHalo, BeaconHaloColor);
            halo.raycastTarget = false;
            RectTransform h = halo.rectTransform;
            h.anchorMin = new Vector2(0.5f, 0.5f);
            h.anchorMax = new Vector2(0.5f, 0.5f);
            h.pivot = new Vector2(0.5f, 0.5f);
            h.sizeDelta = new Vector2(ProceduralArt.MapBeaconHaloDp, ProceduralArt.MapBeaconHaloDp);
            halo.gameObject.SetActive(false);
            Image beacon = UiBuilder.Image("Beacon", root, null, Color.white);
            beacon.preserveAspect = true;
            RectTransform b = beacon.rectTransform;
            b.anchorMin = new Vector2(0.5f, 0.5f);
            b.anchorMax = new Vector2(0.5f, 0.5f);
            b.pivot = new Vector2(0.5f, 0f);
            b.sizeDelta = new Vector2(ProceduralArt.MapBeaconWidthDp, ProceduralArt.MapBeaconHeightDp);
            beacon.gameObject.SetActive(false);

            marker = new StopMarker(root, button, rim, glass, waterA, waterB, beacon, halo);
            SetBeaconSide(marker, false);
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

        // Las mini cuias, solo al cambiar de vista; sin asignaciones (las reservas crecen al abrir). Cada una va sobre el río en su
        // punto real (propuesta §7: es el marcador de la parada); los racimos (puntos a menos de ClusterMinDp), en abanico con su
        // línea fina al punto real (§3.2). Lo que se pinta se comprueba después contra todo lo demás: si dos mini cuias quedan a
        // menos de ClusterMinDp entre centros (un abanico contra una suelta o contra otro abanico), se unen sus racimos y se vuelve
        // a colocar, hasta que no queda ningún choque. Ninguna pisa la canoa con su mástil y su guía (como quedan con max_level:
        // al ponerse al día la canoa acaba ahí) ni la fila corta de boyas: la suelta que caería encima se aparta lo justo, fuera
        // del río, con su línea al punto real.
        private void PlaceStopMarkers(MapSpace space)
        {
            int n = _markerCount;
            for (int i = 0; i < n; i++)
            {
                StopMarker m = _markers[i];
                m.Real = space.ToLocal(m.MapPoint);
                m.Shown = m.Real;
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

            AvoidCompactRow(space);
            CollectObstacles(space);
            for (int pass = 0; pass <= n; pass++)
            {
                _placedCount = 0;
                for (int i = 0; i < n; i++)
                {
                    if (Find(i) != i)
                    {
                        continue; // cada racimo se coloca una vez, desde su raíz
                    }

                    int size = 0;
                    for (int j = 0; j < n; j++)
                    {
                        if (Find(j) == i)
                        {
                            _clusterMembers[size++] = j;
                        }
                    }

                    if (size == 1)
                    {
                        PlaceSingle(space, _clusterMembers[0]);
                    }
                    else
                    {
                        PlaceFan(space, size);
                    }
                }

                // Contra lo que de verdad se pinta: dos mini cuias de racimos distintos a menos de ClusterMinDp van juntas.
                bool merged = false;
                for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        if (Find(i) != Find(j) && Vector2.Distance(_markers[i].Shown, _markers[j].Shown) < min)
                        {
                            Union(i, j);
                            merged = true;
                        }
                    }
                }

                if (!merged)
                {
                    break;
                }
            }

            _leaders!.Clear();
            for (int i = 0; i < n; i++)
            {
                StopMarker m = _markers[i];
                m.HasLeader = (m.Shown - m.Real).sqrMagnitude > 0.25f;
                if (m.HasLeader && LeaderFoot(m.Shown, m.Real, out Vector2 foot))
                {
                    _leaders.Add(foot, m.Real);
                }

                if (m.Root.anchoredPosition != m.Shown)
                {
                    m.Root.anchoredPosition = m.Shown;
                }
            }

            for (int i = 0; i < n; i++)
            {
                PlaceBeacon(i);
            }

            _leaders.Commit();
        }

        // Una mini cuia suelta: en su punto real si no pisa nada; si no, en el sitio libre más cercano (anillos de MarkerShiftStepDp
        // en las ocho direcciones, hasta MarkerMaxShiftDp), mejor fuera del río. Sin asignaciones.
        private void PlaceSingle(MapSpace space, int index)
        {
            StopMarker m = _markers[index];
            Vector2 real = m.Real;
            Vector2 at = ClampMarker(real, _contentW, _contentH);
            float best = Moved(at, real) + Penalty(space, at, 0, false, false, float.MaxValue);
            Vector2 bestAt = at;
            int rings = Mathf.RoundToInt(MarkerMaxShiftDp / MarkerShiftStepDp);
            for (int ring = 1; ring <= rings && best > 0f; ring++)
            {
                for (int d = 0; d < Dirs8.Length && best > 0f; d++)
                {
                    Vector2 p = real + (Dirs8[d] * (ring * MarkerShiftStepDp));
                    Vector2 c = ClampMarker(p, _contentW, _contentH);
                    float pen = Moved(c, p);
                    if (pen >= best)
                    {
                        continue;
                    }

                    pen += Penalty(space, c, 0, false, true, best - pen);
                    if (pen < best)
                    {
                        best = pen;
                        bestAt = c;
                    }
                }
            }

            m.Shown = bestAt;
            _placedIdx[_placedCount++] = index;
        }

        // Un racimo de size mini cuias (_clusterMembers) en abanico alrededor del centro de sus puntos reales: arco de FanSpanDeg
        // y, si no cabe, de FanSpanWideDeg; radio para FanGapDp entre centros (al menos ClusterMinDp) y, si no cabe, más grande,
        // hasta FanMaxRadiusDp; y ocho direcciones, primero hacia la proa de la canoa si está cerca (el mástil va a popa) o hacia
        // dentro del mapa. Gana el primero sin choques ni río debajo. Los miembros van de un extremo al otro del arco en el orden de
        // su punto real, así las líneas no se cruzan. Si ningún arco sale limpio (o el racimo es tan grande que el arco no cabe en
        // FanMaxRadiusDp), el racimo se reparte (PackCluster) y gana lo que menos cuesta. Sin asignaciones.
        private void PlaceFan(MapSpace space, int size)
        {
            Vector2 centroid = Vector2.zero;
            for (int a = 0; a < size; a++)
            {
                centroid += _markers[_clusterMembers[a]].Real;
            }

            centroid /= size;
            bool nearCanoe = _gearBox.width > 0f && RectDistance(_gearBox, centroid) < FanGapDp * 2.5f;
            float pref = nearCanoe ? _gearFacing : (centroid.x + FanGapDp + MinTouchDp > _contentW ? -1f : 1f);
            float best = float.MaxValue;
            for (int s = 0; s < 2 && best > 0f; s++)
            {
                float span = (s == 0 ? FanSpanDeg : FanSpanWideDeg) * Mathf.Deg2Rad;
                float step = span / (size - 1);
                float r0 = Mathf.Max(MapConfig.ClusterMinDp, FanGapDp / (2f * Mathf.Sin(step * 0.5f)));
                for (int r = 0; r < 3 && best > 0f && r0 + (r * FanRadiusStepDp) <= FanMaxRadiusDp; r++)
                {
                    float radius = r0 + (r * FanRadiusStepDp);
                    for (int d = 0; d < Dirs8.Length && best > 0f; d++)
                    {
                        Vector2 axis = new Vector2(Dirs8[d].x * pref, Dirs8[d].y);
                        float phi = Mathf.Atan2(axis.y, axis.x);
                        OrderAlong(size, new Vector2(-axis.y, axis.x));
                        float pen = 0f;
                        for (int a = 0; a < size && pen < best; a++)
                        {
                            float angle = phi + (span * 0.5f) - (a * step);
                            Vector2 p = centroid + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                            Vector2 c = ClampMarker(p, _contentW, _contentH);
                            pen += Moved(c, p);
                            pen += Penalty(space, c, a, true, true, best - pen);
                            _fanPos[a] = c;
                        }

                        if (pen < best)
                        {
                            best = pen;
                            for (int a = 0; a < size; a++)
                            {
                                _bestPos[a] = _fanPos[a];
                                _bestOrder[a] = _fanOrder[a];
                            }
                        }
                    }
                }
            }

            if (best > 0f)
            {
                // Sin arco limpio: el racimo repartido, si cuesta menos (los miembros, en el orden del racimo).
                float packed = PackCluster(space, size, pref, best);
                if (packed < best)
                {
                    best = packed;
                    for (int a = 0; a < size; a++)
                    {
                        _bestPos[a] = _fanPos[a];
                        _bestOrder[a] = _clusterMembers[a];
                    }
                }
            }

            for (int a = 0; a < size; a++)
            {
                int index = _bestOrder[a];
                StopMarker m = _markers[index];
                m.Shown = _bestPos[a];
                m.Fanned = true;
                _placedIdx[_placedCount++] = index;
            }
        }

        // El racimo repartido: cada miembro (en el orden del racimo) en el sitio libre más cercano a su punto real, en anillos de
        // MarkerShiftStepDp desde FanGapDp / 2 hasta FanPackMaxDp y dieciséis direcciones (primero hacia pref): sin pisar la canoa,
        // el guía ni la fila corta, a ≥ FanGapDp de las demás y fuera del río; si en ningún sitio sale limpio, el que menos cuesta.
        // Deja los sitios en _fanPos y devuelve lo que cuesta todo (para en cuanto pasa de budget). Sin asignaciones.
        private float PackCluster(MapSpace space, int size, float pref, float budget)
        {
            float total = 0f;
            int rings = Mathf.RoundToInt((FanPackMaxDp - (FanGapDp * 0.5f)) / MarkerShiftStepDp);
            for (int a = 0; a < size && total < budget; a++)
            {
                Vector2 real = _markers[_clusterMembers[a]].Real;
                float best = float.MaxValue;
                Vector2 bestAt = ClampMarker(real, _contentW, _contentH);
                for (int ring = 0; ring <= rings && best > 0f; ring++)
                {
                    float d = (FanGapDp * 0.5f) + (ring * MarkerShiftStepDp);
                    for (int k = 0; k < 16 && best > 0f; k++)
                    {
                        float angle = k * (Mathf.PI / 8f);
                        Vector2 p = real + new Vector2(pref * Mathf.Cos(angle) * d, Mathf.Sin(angle) * d);
                        Vector2 c = ClampMarker(p, _contentW, _contentH);
                        float pen = Moved(c, p);
                        if (pen >= best)
                        {
                            continue;
                        }

                        pen += Penalty(space, c, a, true, true, best - pen);
                        if (pen < best)
                        {
                            best = pen;
                            bestAt = c;
                        }
                    }
                }

                _fanPos[a] = bestAt;
                total += best;
            }

            return total;
        }

        // _fanOrder: los miembros del racimo de mayor a menor proyección de su punto real sobre normal (los iguales, en su orden).
        private void OrderAlong(int size, Vector2 normal)
        {
            for (int a = 0; a < size; a++)
            {
                int v = _clusterMembers[a];
                float key = Vector2.Dot(_markers[v].Real, normal);
                int b = a - 1;
                while (b >= 0 && Vector2.Dot(_markers[_fanOrder[b]].Real, normal) < key - 1e-4f)
                {
                    _fanOrder[b + 1] = _fanOrder[b];
                    b--;
                }

                _fanOrder[b + 1] = v;
            }
        }

        // Lo que cuesta poner una mini cuia con su centro en c: 1000 por pisar la canoa, el guía o la fila corta (con su hueco) o
        // por quedar a menos de ClusterMinDp de otra ya puesta (o de las fanCount primeras de _fanPos); en un abanico (fan), 50 si
        // queda a menos de FanGapDp; y, con river, 10 si queda a menos de MarkerLineClearanceDp del borde del río (solo si aún puede
        // ganar a budget). Sin asignaciones.
        private float Penalty(MapSpace space, Vector2 c, int fanCount, bool fan, bool river, float budget)
        {
            Rect box = CuiaBox(c);
            float pen = 0f;
            for (int k = 0; k < _obstacleCount; k++)
            {
                if (box.Overlaps(_obstacles[k]))
                {
                    pen += 1000f;
                }
            }

            for (int k = 0; k < _placedCount; k++)
            {
                pen += Crowding(c, _markers[_placedIdx[k]].Shown, fan);
            }

            for (int k = 0; k < fanCount; k++)
            {
                pen += Crowding(c, _fanPos[k], fan);
            }

            if (river && pen < budget && LineClearance(_map!, space, _view, box, MarkerLineClearanceDp) < MarkerLineClearanceDp)
            {
                pen += 10f;
            }

            return pen;
        }

        private static float Crowding(Vector2 a, Vector2 b, bool fan)
        {
            float d = Vector2.Distance(a, b);
            if (d < MapConfig.ClusterMinDp)
            {
                return 1000f;
            }

            return fan && d < FanGapDp ? 50f : 0f;
        }

        // 100 si el borde del mapa movió el sitio pedido.
        private static float Moved(Vector2 at, Vector2 wanted)
        {
            return (at - wanted).sqrMagnitude > 0.01f ? 100f : 0f;
        }

        // Caja de la mini cuia (20 × 32 dp) con su centro en c, en coordenadas locales del contenido.
        private static Rect CuiaBox(Vector2 c)
        {
            float w = ProceduralArt.MiniCuiaWidthDp;
            float h = ProceduralArt.MiniCuiaHeightDp;
            return new Rect(c.x - (w * 0.5f), c.y - (h * 0.5f), w, h);
        }

        private static Rect Grow(Rect r, float by)
        {
            return Rect.MinMaxRect(r.xMin - by, r.yMin - by, r.xMax + by, r.yMax + by);
        }

        private static float RectDistance(Rect r, Vector2 p)
        {
            float dx = Mathf.Max(0f, Mathf.Max(r.xMin - p.x, p.x - r.xMax));
            float dy = Mathf.Max(0f, Mathf.Max(r.yMin - p.y, p.y - r.yMax));
            return Mathf.Sqrt((dx * dx) + (dy * dy));
        }

        // Dónde sale la línea fina de la mini cuia en shown hacia su punto real: el borde de su caja en esa dirección. False si el
        // punto real cae dentro de la caja (no hace falta línea).
        private static bool LeaderFoot(Vector2 shown, Vector2 real, out Vector2 foot)
        {
            Vector2 d = real - shown;
            float hw = ProceduralArt.MiniCuiaWidthDp * 0.5f;
            float hh = ProceduralArt.MiniCuiaHeightDp * 0.5f;
            float tx = Mathf.Abs(d.x) > 1e-4f ? hw / Mathf.Abs(d.x) : float.MaxValue;
            float ty = Mathf.Abs(d.y) > 1e-4f ? hh / Mathf.Abs(d.y) : float.MaxValue;
            float t = Mathf.Min(tx, ty);
            foot = shown + (d * Mathf.Min(t, 1f));
            return t < 1f;
        }

        // Lo que ninguna mini cuia pisa, con MarkerObstacleGapDp de hueco: la canoa con su mástil y sus banderas, y el guía, como
        // quedan con max_level (al ponerse al día la canoa acaba ahí); y la fila corta de boyas, si la hay. Sin asignaciones.
        private void CollectObstacles(MapSpace space)
        {
            _obstacleCount = 0;
            _gearBox = default;
            _guideBox = default;
            if (_hasCanoe && _progress != null)
            {
                int max = Mathf.Clamp(_progress.MaxLevel, 0, WorldTable.LastLevel);
                float facing = _canoeFacing;
                GearPose(space, max, _canoeMap, FinalFlagCount(max), ref facing, out Vector2 canoe, out Vector2 guide, out _, out _, out _,
                    out Vector2 foot, out float mastH);
                _gearFacing = facing;
                _gearBox = GearBox(canoe, foot, mastH, facing);
                _guideBox = GuideBox(guide);
                _obstacles[_obstacleCount++] = Grow(_gearBox, MarkerObstacleGapDp);
                _obstacles[_obstacleCount++] = Grow(_guideBox, MarkerObstacleGapDp);
            }

            if (_buoysCompact && _buoyCount > 0)
            {
                float r = BuoyRadiusWorldDp;
                Vector2 a = _buoyLocal[0];
                Vector2 b = _buoyLocal[BuoysPerLeg - 1];
                _obstacles[_obstacleCount++] = Grow(Rect.MinMaxRect(Mathf.Min(a.x, b.x) - r, a.y - r, Mathf.Max(a.x, b.x) + r, a.y + r), MarkerObstacleGapDp);
            }
        }

        // Banderas con dibujo que lleva la canoa con max niveles ganados (las que se izan al acabar «ponerse al día»).
        private int FinalFlagCount(int max)
        {
            int raw = CanoeFlags(_stops!, max, _flagScratch);
            int count = 0;
            for (int i = 0; i < raw; i++)
            {
                if (_art.MapFlag(_flagScratch[i]!) != null)
                {
                    count++;
                }
            }

            return count;
        }

        // Caja de la canoa (su imagen, con su pivote) con el mástil y las banderas, que ondean hacia popa.
        private static Rect GearBox(Vector2 canoe, Vector2 foot, float mastH, float facing)
        {
            float w = ProceduralArt.MapCanoeWidthDp;
            float h = ProceduralArt.MapCanoeHeightDp;
            var box = new Rect(canoe.x - (w * 0.5f), canoe.y - (h * CanoePivotY), w, h);
            if (mastH > 0f)
            {
                float flagEnd = foot.x - (facing * ProceduralArt.MapFlagWidthDp * CanoeFlagScale);
                box = Rect.MinMaxRect(Mathf.Min(box.xMin, Mathf.Min(foot.x - MastWidthDp, flagEnd)), box.yMin,
                    Mathf.Max(box.xMax, Mathf.Max(foot.x + MastWidthDp, flagEnd)), Mathf.Max(box.yMax, foot.y + mastH));
            }

            return box;
        }

        // Caja del guía con su centro de nado en guide (su pivote, a 0,3 del alto).
        private Rect GuideBox(Vector2 guide)
        {
            float gw = _view == MapView.World ? GuideWorldDp : GuideBasinDp;
            float gh = gw * ProceduralArt.RosaHeightDp / ProceduralArt.RosaWidthDp;
            return new Rect(guide.x - (gw * 0.5f), guide.y - (gh * 0.3f), gw, gh);
        }

        // La fila corta de boyas (un tramo sin largo a esta escala) no pisa el punto real de ninguna mini cuia (el punto del
        // abanico caía encima de una boya): si alguno queda a menos de su radio + MarkerObstacleGapDp + el punto, la fila baja hasta
        // dejarlo encima. Sin asignaciones.
        private void AvoidCompactRow(MapSpace space)
        {
            float drop = 0f;
            if (_buoysCompact && _buoyCount > 0)
            {
                float reach = BuoyRadiusWorldDp + MarkerObstacleGapDp + LeaderDotDp + 0.8f;
                float baseY = _buoyLocal[0].y + _compactDropDp;
                float x0 = Mathf.Min(_buoyLocal[0].x, _buoyLocal[BuoysPerLeg - 1].x) - reach;
                float x1 = Mathf.Max(_buoyLocal[0].x, _buoyLocal[BuoysPerLeg - 1].x) + reach;
                for (int iter = 0; iter <= _markerCount; iter++)
                {
                    bool moved = false;
                    float y = baseY - drop;
                    for (int i = 0; i < _markerCount; i++)
                    {
                        Vector2 p = _markers[i].Real;
                        if (p.x >= x0 && p.x <= x1 && Mathf.Abs(p.y - y) < reach)
                        {
                            drop = baseY - (p.y - reach);
                            moved = true;
                            break;
                        }
                    }

                    if (!moved)
                    {
                        break;
                    }
                }
            }

            SetCompactRowDrop(space, drop);
        }

        // La baliza al lado de su mini cuia que no pise la canoa, el guía ni otra mini cuia: a la derecha y, si no, a la izquierda.
        private void PlaceBeacon(int index)
        {
            StopMarker m = _markers[index];
            if (!m.Beacon.gameObject.activeSelf)
            {
                return;
            }

            bool left = BeaconBlocked(index, BeaconBox(m.Shown, false)) && !BeaconBlocked(index, BeaconBox(m.Shown, true));
            if (left != m.BeaconLeft)
            {
                SetBeaconSide(m, left);
            }
        }

        private bool BeaconBlocked(int self, Rect box)
        {
            for (int k = 0; k < _obstacleCount; k++)
            {
                if (box.Overlaps(_obstacles[k]))
                {
                    return true;
                }
            }

            for (int k = 0; k < _markerCount; k++)
            {
                if (k != self && box.Overlaps(CuiaBox(_markers[k].Shown)))
                {
                    return true;
                }
            }

            return false;
        }

        // Caja de la baliza de la mini cuia en shown (a su derecha o, con left, a su izquierda), con el pie al pie de la mini cuia.
        private static Rect BeaconBox(Vector2 shown, bool left)
        {
            float x = shown.x + BeaconOffsetX(left);
            float foot = shown.y - (ProceduralArt.MiniCuiaHeightDp * 0.5f);
            return new Rect(x - (ProceduralArt.MapBeaconWidthDp * 0.5f), foot, ProceduralArt.MapBeaconWidthDp, ProceduralArt.MapBeaconHeightDp);
        }

        private static float BeaconOffsetX(bool left)
        {
            return (left ? -1f : 1f) * ((ProceduralArt.MiniCuiaWidthDp * 0.5f) + 1f + (ProceduralArt.MapBeaconWidthDp * 0.5f));
        }

        private static void SetBeaconSide(StopMarker m, bool left)
        {
            m.BeaconLeft = left;
            float x = BeaconOffsetX(left);
            float foot = -ProceduralArt.MiniCuiaHeightDp * 0.5f;
            m.Beacon.rectTransform.anchoredPosition = new Vector2(x, foot);
            m.Halo.rectTransform.anchoredPosition = new Vector2(x, foot + ProceduralArt.MapBeaconLanternDp);
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

        // La casa en Guajará: el árbol con el pie en tierra a un lado de su punto (HomeSide) y el perezoso colgado de la rama (más
        // pequeños en la Cuenca).
        private void PlaceHouse(MapSpace space)
        {
            float scale = _view == MapView.World ? 1f : HomeBasinScale;
            _homeHeight = ProceduralArt.MapHomeHeightDp * scale;
            Vector2 home = space.ToLocal(_homeMap);
            _homeLocal = home + new Vector2(HomeSide(space, home, scale), 0f);
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

        // A qué lado de Guajará va el pie de la casa (dp con signo; negativo, a la izquierda): el primero, de HomeSideDp a
        // HomeSideMaxDp en pasos de MarkerShiftStepDp y la izquierda antes que la derecha, en que el tronco con su pie de orilla
        // queda a ≥ HomeLineClearanceDp del borde del río y no pisa una mini cuia; si no hay ninguno, HomeSideDp a la izquierda.
        // Sin asignaciones.
        private float HomeSide(MapSpace space, Vector2 home, float scale)
        {
            float half = HomeTrunkHalfDp * scale;
            float h = _homeHeight * 0.7f;
            int steps = Mathf.RoundToInt((HomeSideMaxDp - HomeSideDp) / MarkerShiftStepDp);
            for (int k = 0; k <= steps && _map != null; k++)
            {
                float d = (HomeSideDp + (k * MarkerShiftStepDp)) * scale;
                for (int s = 0; s < 2; s++)
                {
                    float x = home.x + (s == 0 ? -d : d);
                    var box = new Rect(x - half, home.y, 2f * half, h);
                    if (LineClearance(_map, space, _view, box, HomeLineClearanceDp) >= HomeLineClearanceDp && !OverlapsShownMarker(box))
                    {
                        return s == 0 ? -d : d;
                    }
                }
            }

            return -HomeSideDp * scale;
        }

        private bool OverlapsShownMarker(Rect box)
        {
            if (_view != MapView.World)
            {
                return false;
            }

            for (int i = 0; i < _markerCount; i++)
            {
                if (box.Overlaps(CuiaBox(_markers[i].Shown)))
                {
                    return true;
                }
            }

            return false;
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

            float facing = _canoeFacing;
            GearPose(space, _canoeX, _canoeShown, _flagCount, ref facing, out Vector2 canoe, out Vector2 guideLocal, out _guideMap,
                out _guideRoute, out _guideFraction, out Vector2 foot, out float mastH);
            _canoeFacing = facing;
            _canoe.rectTransform.anchoredPosition = canoe;
            _guideShown = true;
            _guideId = GuideAt(_map!, _routes!, _stops!, _canoeX);

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

        // La canoa en canoeMap con level niveles ganados (fraccionario) y flags banderas: dónde se pinta, dónde nada el guía
        // (GuideLeadDp por delante por el agua; sin agua por delante, GuideAboveDp encima de la canoa), hacia dónde mira la proa
        // (facing: solo cambia si el guía queda a más de 2 dp a un lado) y el pie y el alto del mástil de popa. Sin asignaciones.
        private void GearPose(MapSpace space, float level, Vector2 canoeMap, int flags, ref float facing, out Vector2 canoe,
            out Vector2 guideLocal, out Vector2 guideMap, out int route, out float fraction, out Vector2 mastFoot, out float mastH)
        {
            canoe = space.ToLocal(canoeMap);
            float dpu = Mathf.Max(1e-4f, space.DpPerUnit);
            if (GuidePoint(_map!, _routes!, level, GuideLeadDp / dpu, GuideMinAheadDp / dpu, out Vector2 ahead, out route, out fraction))
            {
                guideMap = ahead;
                guideLocal = space.ToLocal(ahead);
            }
            else
            {
                route = -1;
                fraction = 0f;
                guideLocal = canoe + new Vector2(0f, GuideAboveDp);
                guideMap = new Vector2(canoeMap.x, canoeMap.y - (GuideAboveDp / dpu));
            }

            float dx = guideLocal.x - canoe.x;
            if (Mathf.Abs(dx) > 2f)
            {
                facing = dx > 0f ? 1f : -1f;
            }

            float fh = ProceduralArt.MapFlagHeightDp * CanoeFlagScale;
            mastH = flags > 0 ? (2f * MastPadDp) + (flags * (fh + CanoeFlagGapDp)) : 0f;
            mastFoot = canoe + new Vector2(-facing * ProceduralArt.MapCanoeWidthDp * SternFraction, 2f);
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

        // La ventanita, abajo a la izquierda de la zona del mapa, con la píldora debajo del retrato (revisión de M3.5: a su derecha
        // cruzaba la zona de juego): encima del rótulo del Atlántico y de la fila de «Volver a la canoa», sin salirse de la zona.
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

            float top = Mathf.Max(FramePadDp + OrientationInsetDp, bottom - HomeWindowGapDp - _homeWindow.HeightDp);
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

            _homeInView = HomeInViewAt(_offset);
            _homeWindow.Apply(_homeStop, _homeInView || !Visible);
            return _homeWindow.Visible;
        }

        // La casa queda dentro de la zona visible con el contenido en offset (con HomeInViewMarginDp de margen).
        private bool HomeInViewAt(Vector2 offset)
        {
            float vx = offset.x + _homeLocal.x;
            float vy = offset.y - (_homeLocal.y + (_homeHeight * 0.5f));
            float m = HomeInViewMarginDp;
            return vx >= m && vx <= _viewW - m && vy >= m && vy <= _viewH - m;
        }

        // La ventanita con su píldora en coordenadas de la zona visible (dp, y hacia abajo): la zona empieza FramePadDp dentro del
        // marco.
        private Rect HomeWindowInView()
        {
            Rect b = _homeWindow!.Bounds;
            return new Rect(b.x - FramePadDp, b.y - FramePadDp, b.width, b.height);
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
            public StopMarker(RectTransform root, Button button, Image rim, Image glass, Image waterA, Image waterB, Image beacon, Image halo)
            {
                Root = root;
                Button = button;
                Rim = rim;
                Glass = glass;
                WaterA = waterA;
                WaterB = waterB;
                Beacon = beacon;
                Halo = halo;
                Stop = null!;
            }

            public RectTransform Root { get; }

            public Button Button { get; }

            public Image Rim { get; }

            public Image Halo { get; }

            public bool HasLeader { get; set; }

            public bool BeaconLeft { get; set; }

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
