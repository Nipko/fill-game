#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El río de vidrio que se llena (tarea M3.4; D-013, puntos 4 y 5: Q2-A y Q8-A; 06 §4.5; propuesta del Mapa §3.3-§3.5 y
    /// §5.1; 04 §2.6), parcial de <see cref="RiverMapScreen"/> sobre sus ganchos <c>Progress*</c>. Todo sale de
    /// <c>max_level</c>, de los niveles saltados y del catálogo de paradas (<see cref="IMapProgress"/>), sin estado propio:
    /// <list type="bullet">
    /// <item><description><b>Lleno</b> (lienzo estático, encima del vidrio): con <c>max_level</c> = m, las paradas
    /// 0 … <see cref="FrontStopOf"/>(m) − 1 ya alcanzadas. Cada línea va llena desde el punto más alto al que llegaron sus rutas
    /// hasta su boca (s = 1), con el agua real de cada tramo y las dos aguas de Santarém y del Encuentro
    /// (<see cref="RibbonLook.Water"/>): «desde la boca hasta la parada de <c>max_level</c> por los legs». Lo calcula
    /// <see cref="ComputeFill"/>.</description></item>
    /// <item><description><b>Frente</b> (lienzo dinámico, detrás de la canoa): el tramo de la parada <see cref="FrontStopOf"/>(m),
    /// lleno hasta <see cref="RiverJourney.LegFill"/>(m) de su longitud real, con sus 10 boyas repartidas por esa longitud
    /// (una por nivel; encendidas las de los niveles ganados). Las por ganar van encima de la canoa, que si no las tapa en la
    /// punta del agua (son el «solo me falta uno», propuesta §4.1); en un tramo sin largo a la escala de la vista (los de «misma
    /// vista, otra luz», de longitud 0), las 10 van en una fila corta bajo la parada (<see cref="BuoyCompactBelowDp"/>). El agua
    /// llena lleva una orla de vidrio (canto de <c>vidrio_borde</c> y filete de tinta) y un reflejo (<see cref="RibbonPart"/>),
    /// y el vidrio vacío baja a <see cref="MapGlassBodyAlpha"/>: el progreso es lo que se lee. La boya de un nivel saltado con el
    /// Salto del Boto lleva el icono de Rosa y el agua no se corta (04 §2.6); en los tramos ya recorridos, el nivel saltado sigue marcado con Rosa en su
    /// boya. Rosa va encima de la canoa, con un halo de <c>vidrio_borde</c>, y su ancho baja con la distancia entre boyas
    /// (<see cref="RosaWidthDp"/>). Con <c>ui_map_mode</c> = <c>worlds</c>, sin boyas ni Rosa (08 §9; el filtro completo es de
    /// M4.3).</description></item>
    /// <item><description><b>Afluentes:</b> al pasar su boca (las <c>leg.mouths</c> de las rutas), cada línea que no remonta
    /// ninguna ruta del catálogo (<see cref="RouteClimbs"/>; las otras se llenan por la ruta) se llena <see cref="MouthFill"/>
    /// desde la boca: entera la de paso y la local (con un fundido de
    /// <see cref="MapConfig.PassbyFillFadeMs"/> la primera vez que se ven; el Tocantins, desde el primer Mapa) y solo la boca
    /// la de un mundo futuro. Cada mundo futuro del catálogo lleva su <b>silueta</b> en azul petróleo (<c>panel_alto</c>
    /// #234452) con <c>map_next_world</c> («Mundo 6 · Oriente · Nivel 201») en su gran parada, como la muestra del Mapa, sin
    /// pisar la caja de la canoa (<see cref="SilhouetteCanoeClearanceDp"/>).</description></item>
    /// <item><description><b>Rótulos:</b> «Andes ↑» y «Océano Atlántico ↓» fijos en el marco (<c>map_label_andes</c> y
    /// <c>map_label_atlantic</c>, en Baloo 2, que tiene las flechas; P-M2) y los países en su región (<c>labels</c> de
    /// <c>river_map_v1.json</c>, sin fronteras en tierra; Q8-A), con <c>map.country.&lt;iso2&gt;.name</c> por
    /// <see cref="Loc.Fact(string)"/>: en release, un nombre sin aprobar del asesor no sale. Los países van un escalón por debajo
    /// (12 sp, crema al 75 % y contorno de 1 dp) y se apartan de las líneas de clase 1-2 (<see cref="LineClearance"/>): el río
    /// es el protagonista.</description></item>
    /// </list>
    /// <para><b>Nunca retrocede</b> (D-013, punto 4): el estado es una función creciente de <c>max_level</c>, que ni la Crecida, ni
    /// un fallo, ni «El río bajó» bajan, así que el agua tampoco baja (<see cref="ComputeFill"/> lo cumple nivel a nivel). Las
    /// mallas del lleno y del frente se reconstruyen solo si cambia <c>max_level</c> (<see cref="ProgressBuilds"/>); al
    /// cambiar de vista solo cambian su escala y su ancho. Con el Mapa quieto, <see cref="Tick"/> no asigna memoria.</para>
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Boyas de un tramo: una por nivel (propuesta del Mapa §4.1).</summary>
        public const int BuoysPerLeg = RiverStopCatalog.LevelsPerStop;

        /// <summary>
        /// Radio máximo de una boya en la vista Mundo (dp) [P]; si el tramo es corto, menos (<see cref="BuoySpacingFraction"/> de
        /// la distancia entre dos boyas, nunca por debajo de <see cref="BuoyRadiusMinDp"/>), para que no se tapen entre sí.
        /// </summary>
        public const float BuoyRadiusWorldDp = 4f;

        /// <summary>Radio máximo de una boya en la vista Cuenca (dp) [P].</summary>
        public const float BuoyRadiusBasinDp = 2.5f;

        /// <summary>Radio mínimo de una boya (dp) [P].</summary>
        public const float BuoyRadiusMinDp = 1.5f;

        /// <summary>Radio de una boya como fracción de la distancia más corta entre dos boyas seguidas [P].</summary>
        public const float BuoySpacingFraction = 0.4f;

        /// <summary>
        /// Ancho máximo del icono de Rosa de una boya saltada en la vista Mundo (dp) [P]; el alto sigue su sprite. Si las boyas
        /// van juntas, menos (<see cref="SkippedRosaSpacingFactor"/> × la distancia entre dos boyas), nunca por debajo de
        /// <see cref="SkippedRosaMinDp"/>.
        /// </summary>
        public const float SkippedRosaWorldDp = 18f;

        /// <summary>Ancho del icono de Rosa de una boya saltada en la vista Cuenca (dp) [P].</summary>
        public const float SkippedRosaBasinDp = 12f;

        /// <summary>Ancho mínimo del icono de Rosa (dp) [P]: por debajo, ya no se reconoce un delfín.</summary>
        public const float SkippedRosaMinDp = 12f;

        /// <summary>Ancho de Rosa como múltiplo de la distancia más corta entre dos boyas de su tramo [P].</summary>
        public const float SkippedRosaSpacingFactor = 1.6f;

        /// <summary>Halo de Rosa (dp): <c>vidrio_borde</c> (#EAF7F3), como el borde del vidrio, para separarla del agua negra.</summary>
        public const float SkippedRosaHaloDp = 1f;

        /// <summary>Tamaño de «Andes ↑» y «Océano Atlántico ↓» (sp) [P].</summary>
        public const float OrientationLabelSp = 14f;

        /// <summary>
        /// Tamaño del nombre de un país en su región (sp) [P]: un escalón por debajo de «Andes ↑» y «Océano Atlántico ↓», para que
        /// los países no compitan con la información de juego (el mínimo de 07 §1.2).
        /// </summary>
        public const float CountryLabelSp = 12f;

        /// <summary>Opacidad del nombre de un país (crema al 75 %) [P].</summary>
        public const float CountryLabelAlpha = 0.75f;

        /// <summary>Contorno del nombre de un país (dp) [P], frente a los 1,5 dp de la píldora de madera de la que sale.</summary>
        public const float CountryOutlineDp = 1f;

        /// <summary>Contorno del material <c>PillMadera</c> (dp, a 12 dp de letra; <c>AF.EditorTools.FontSetup</c>).</summary>
        private const float PillWoodOutlineDp = 1.5f;

        /// <summary>Hueco que se busca entre la caja de un país y el borde de cualquier línea de clase 1-2 (dp) [P].</summary>
        public const float CountryLineClearanceDp = 8f;

        /// <summary>Lo más que se aparta un país de su punto de <c>labels</c> para no pisar el río (dp) [P].</summary>
        public const float CountryMaxShiftDp = 48f;

        /// <summary>Paso de la búsqueda de sitio de un país o de una silueta (dp).</summary>
        public const float LabelSearchStepDp = 4f;

        /// <summary>Tamaño del texto de la silueta de un mundo futuro (sp) [P]: el mínimo de 07 §1.2.</summary>
        public const float SilhouetteSp = 12f;

        /// <summary>Distancia entre el punto de la gran parada de un mundo futuro y su silueta (dp) [P].</summary>
        public const float SilhouetteGapDp = 6f;

        /// <summary>Hueco mínimo entre la placa de una silueta y la caja de la canoa (dp) [P].</summary>
        public const float SilhouetteCanoeClearanceDp = 6f;

        /// <summary>Lo más que se mueve en vertical la placa de una silueta para dejar libre la canoa (dp) [P].</summary>
        public const float SilhouetteMaxShiftDp = 120f;

        /// <summary>Opacidad de la placa de una silueta: casi opaca, para que no se transparenten las líneas.</summary>
        public const float SilhouettePlateAlpha = 0.92f;

        /// <summary>Margen mínimo de un país o una silueta con el borde del mapa (dp).</summary>
        public const float LabelEdgeDp = 4f;

        /// <summary>Margen de los rótulos fijos con el borde de la zona del mapa (dp).</summary>
        public const float OrientationInsetDp = 8f;

        /// <summary>Valor de <see cref="FillFrom"/> de una línea sin nada lleno.</summary>
        public const float NoFill = 2f;

        /// <summary>
        /// Opacidad del cuerpo del vidrio vacío en el Mapa [P] (frente a los 0,85 de <see cref="RiverRibbonGraphic.GlassBody"/>):
        /// vidrio fantasma, más o menos #6CA98E sobre la selva, para que el progreso se lea más que los ríos que faltan.
        /// </summary>
        public const float MapGlassBodyAlpha = 0.40f;

        /// <summary>
        /// Largo mínimo de la boca de un afluente de un mundo futuro en la Cuenca (dp) [P]: con <c>fill_on_pass</c> solo, algunas
        /// bocas miden 2-6 dp y parecen grietas (<see cref="MouthFill"/>).
        /// </summary>
        public const float MouthMinBasinDp = 14f;

        /// <summary>
        /// Distancia entre boyas por debajo de la que el tramo no deja ver el avance (dp) [P]: el tramo de longitud 0 de «misma
        /// vista, otra luz» (Leticia al atardecer, Pacaya al alba) o uno de unos metros (Itaya). Entonces las 10 boyas van en una
        /// fila corta bajo la parada (<see cref="BuoysCompact"/>), con su radio de la vista y <see cref="BuoyCompactSpacing"/>
        /// radios entre centros.
        /// </summary>
        public const float BuoyCompactBelowDp = 4f;

        /// <summary>Distancia entre centros de las boyas de la fila corta, en radios [P].</summary>
        public const float BuoyCompactSpacing = 2.2f;

        /// <summary>Hueco entre la caja de la canoa (o el punto de la parada) y la fila corta de boyas (dp) [P].</summary>
        public const float BuoyCompactGapDp = 3f;

        /// <summary>Hueco que busca la placa de una silueta con el borde de las líneas de clase 1-2 (dp) [P].</summary>
        public const float SilhouetteLineClearanceDp = 4f;

        /// <summary>
        /// Paso de la búsqueda de un sitio fuera del lleno para una placa en la zona visible de la Cuenca (dp), cuando cerca
        /// de su parada no lo hay (revisión de FX7).
        /// </summary>
        public const float SilhouetteFarStepDp = 8f;

        /// <summary>
        /// Lo más lejos de su parada que se busca ese sitio fuera del lleno (dp) [P]: el centro de la placa, a lo sumo esto más medio
        /// ancho de la placa (revisión de FX7: con max 200 la placa de Ucayali, en Perú, salía junto a «Brasil», en la otra punta de
        /// la Cuenca).
        /// </summary>
        public const float SilhouetteFarMaxDistDp = 140f;

        /// <summary>Hueco entre un país y el control de la Cuenca que pisaba, si se corre a su izquierda (dp) [P] (revisión de FX7).</summary>
        public const float CountryControlGapDp = 6f;

        /// <summary>Silueta de un mundo futuro: <c>panel_alto</c> (#234452, el azul petróleo del panel, D-004; propuesta §3.5).</summary>
        public static readonly Color SilhouetteColor = Palette.Panel.Top;

        /// <summary>Boya encendida: <c>destello</c> (#FFF4D2) con el canto de madera del marco.</summary>
        public static readonly Color BuoyLitColor = Palette.Glint;

        /// <summary>Boya por ganar: el vidrio del Mapa, con el canto azul petróleo.</summary>
        public static readonly Color BuoyUnlitColor = RiverRibbonGraphic.GlassBody;

        private readonly List<RiverRibbonGraphic> _fill = new List<RiverRibbonGraphic>();
        private readonly List<RiverRibbonGraphic> _fillCasing = new List<RiverRibbonGraphic>();
        private readonly List<RiverRibbonGraphic> _front = new List<RiverRibbonGraphic>();
        private readonly List<RiverRibbonGraphic> _frontCasing = new List<RiverRibbonGraphic>();
        private readonly List<TextMeshProUGUI> _countries = new List<TextMeshProUGUI>();
        private readonly List<Silhouette> _silhouettes = new List<Silhouette>();
        private readonly List<int> _fading = new List<int>();
        private readonly Vector2[] _buoyMap = new Vector2[BuoysPerLeg];
        private readonly Vector2[] _buoyLocal = new Vector2[BuoysPerLeg];
        private readonly int[] _buoyLevel = new int[BuoysPerLeg];
        private readonly BuoyState[] _buoyState = new BuoyState[BuoysPerLeg];
        private readonly Image[] _rosa = new Image[BuoysPerLeg];
        private readonly List<Image> _pastRosa = new List<Image>();
        private readonly List<Vector2> _pastRosaMap = new List<Vector2>();
        private readonly List<float> _pastRosaSpacing = new List<float>();
        private readonly List<int> _pastRosaLevel = new List<int>();
        private readonly List<float> _pastRosaLegSpacing = new List<float>();
        private readonly List<Vector2> _pastRosaStop = new List<Vector2>();
        private readonly List<Rect> _placed = new List<Rect>();
        private bool[] _countryBasinHidden = Array.Empty<bool>();
        private float[] _countryFreeClearance = Array.Empty<float>();
        private float[] _countryUnderClearance = Array.Empty<float>();
        private static Material? _countryMaterial;

        private RectTransform? _fillLayer;
        private RectTransform? _fillCasingLayer;
        private RectTransform? _fillCoreLayer;
        private RectTransform? _labelLayer;
        private RectTransform? _frontLayer;
        private RectTransform? _frontCoreLayer;
        private RectTransform? _aheadLayer;
        private RectTransform? _rosaLayer;
        private int _pastRosaCount;
        private BuoyGraphic? _buoys;
        private BuoyGraphic? _buoysAhead;
        private Vector2 _frontStopMap;
        private bool _buoysCompact;
        private float _compactDropDp;
        private TextMeshProUGUI? _andesLabel;
        private TextMeshProUGUI? _atlanticLabel;
        private float[] _fillFrom = Array.Empty<float>();
        private float[] _seenFrom = Array.Empty<float>();
        private RiverMap? _progressMap;
        private int _progressLevel = -1;
        private int _progressBuilds;
        private bool _silhouettesPlaced;
        private int _silhouetteReplacements;
        private int _frontStop = -1;
        private float _frontFraction;
        private int _frontCount;

        // Revisión de FX12: la ruta del frente, la pieza de su ruta de cada cinta del frente y el tope de vértices de cada pieza de la
        // ruta (el agua y la orla), repartido según lo que pide cada una con el trazo curvo (FitFrontBudgets). Solo crecen.
        private RiverStopRoute? _frontRoute;
        private int[] _frontPathIndex = Array.Empty<int>();
        private int[] _frontCoreBudget = Array.Empty<int>();
        private int[] _frontCasingBudget = Array.Empty<int>();
        private int _buoyCount;
        private float _fadeMs;
        private float _fadeElapsed;
        private string _progressMapMode = MapConfig.UiMapMode;

        /// <summary>Estado de una boya del frente.</summary>
        public enum BuoyState
        {
            /// <summary>Nivel por ganar: vidrio.</summary>
            Unlit = 0,

            /// <summary>Nivel ganado: encendida.</summary>
            Lit = 1,

            /// <summary>Nivel saltado con el Salto del Boto: el icono de Rosa (04 §2.6); el agua sigue.</summary>
            Skipped = 2,
        }

        /// <summary>Nivel cuyo estado pinta el río (<c>max_level</c> del último <see cref="Open"/>), o −1 antes de abrirlo.</summary>
        public int ProgressLevel => _progressLevel;

        /// <summary>Veces que se calcularon el lleno y el frente (solo al abrir con otro <c>max_level</c> o con otro Mapa).</summary>
        public int ProgressBuilds => _progressBuilds;

        /// <summary>
        /// Veces que las placas de los mundos futuros se volvieron a colocar porque cambió cuáles se ven sin cambiar de vista
        /// («ponerse al día» que cruza una gran parada; tarea FX8, <see cref="ReplaceSilhouettes"/>). Tests.
        /// </summary>
        internal int SilhouetteReplacements => _silhouetteReplacements;

        /// <summary>Parada hacia la que va el frente (<see cref="FrontStopOf"/>), o −1 si el catálogo no tiene su ruta.</summary>
        public int FrontStop => _frontStop;

        /// <summary>Fracción llena del tramo del frente (<see cref="RiverJourney.LegFill"/>); 0 sin frente.</summary>
        public float FrontFraction => _frontFraction;

        /// <summary>Cintas activas del frente (una por tramo de su ruta con agua).</summary>
        public int FrontPieceCount => _frontCount;

        /// <summary>Boyas que se ven: <see cref="BuoysPerLeg"/> con frente, 0 sin él o con <c>ui_map_mode</c> = <c>worlds</c>.</summary>
        public int BuoyCount => _buoyCount;

        /// <summary>Siluetas de mundos futuros que se ven.</summary>
        public int SilhouetteCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _silhouettes.Count; i++)
                {
                    if (_silhouettes[i].Root.gameObject.activeSelf)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>Rótulos de país que se ven.</summary>
        public int CountryLabelCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _countries.Count; i++)
                {
                    if (_countries[i].gameObject.activeSelf)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>Hay algún afluente fundiéndose (60 fps mientras dura).</summary>
        public bool FillFading => _fading.Count > 0;

        /// <summary>
        /// Modo del Mapa (<c>ui_map_mode</c>, 08 §9; <see cref="MapConfig.UiMapMode"/>): con <see cref="MapConfig.MapModeWorlds"/>,
        /// sin boyas. Lo fija quien lee Remote Config (M4.3) o un test antes de <see cref="Open"/>.
        /// </summary>
        internal string ProgressMapMode
        {
            get => _progressMapMode;
            set => _progressMapMode = value ?? MapConfig.UiMapMode;
        }

        /// <summary>Reducir movimiento (07 §7.8): el fundido de los afluentes dura <see cref="MapConfig.ReducedMotionFadeMs"/>.</summary>
        internal bool ProgressReduceMotion { get; set; }

        /// <summary>«Andes ↑», fijo arriba en el marco (null antes de abrir el Mapa).</summary>
        internal TextMeshProUGUI? AndesLabel => _andesLabel;

        /// <summary>«Océano Atlántico ↓», fijo abajo en el marco (null antes de abrir el Mapa).</summary>
        internal TextMeshProUGUI? AtlanticLabel => _atlanticLabel;

        /// <summary>Capa del lleno (lienzo estático, encima del vidrio).</summary>
        internal RectTransform? FillLayer => _fillLayer;

        /// <summary>Capa del frente y las boyas encendidas (lienzo dinámico, detrás de la canoa).</summary>
        internal RectTransform? FrontLayer => _frontLayer;

        /// <summary>Capa de las orlas del lleno y del frente (lienzo estático, debajo del agua del lleno).</summary>
        internal RectTransform? FillCasingLayer => _fillCasingLayer;

        /// <summary>Malla de las boyas encendidas (detrás de la canoa).</summary>
        internal BuoyGraphic? Buoys => _buoys;

        /// <summary>Malla de las boyas por ganar (encima de la canoa: no las tapa en la punta del agua).</summary>
        internal BuoyGraphic? BuoysAhead => _buoysAhead;

        /// <summary>Capa de las boyas por ganar (lienzo dinámico, encima de la canoa y debajo de Rosa).</summary>
        internal RectTransform? BuoysAheadLayer => _aheadLayer;

        /// <summary>Boyas dibujadas entre las dos mallas (sin las saltadas, que llevan a Rosa).</summary>
        public int BuoysDrawn => (_buoys != null ? _buoys.DrawnCount : 0) + (_buoysAhead != null ? _buoysAhead.DrawnCount : 0);

        /// <summary>
        /// Las boyas del frente van en la fila corta bajo la parada (<see cref="BuoyCompactBelowDp"/>): el tramo, a la escala de la
        /// vista, no deja distinguirlas.
        /// </summary>
        public bool BuoysCompact => _buoysCompact;

        /// <summary>Dónde se dibuja la boya <paramref name="i"/>, en coordenadas locales del contenido (dp, y hacia arriba).</summary>
        public Vector2 BuoyLocalPoint(int i)
        {
            return _buoyLocal[i];
        }

        /// <summary>Orla de la cinta del lleno de la línea <paramref name="line"/> (debajo del agua de todas las líneas).</summary>
        internal RiverRibbonGraphic FillCasingRibbon(int line)
        {
            return _fillCasing[line];
        }

        /// <summary>Orla de la cinta <paramref name="i"/> del frente (en el lienzo estático, con las orlas del lleno).</summary>
        internal RiverRibbonGraphic FrontCasingRibbon(int i)
        {
            return _frontCasing[i];
        }

        /// <summary>
        /// s desde la que la línea <paramref name="line"/> del Mapa va llena hasta su boca en el lleno (sin el frente), o
        /// <see cref="NoFill"/>. 0 = entera.
        /// </summary>
        public float FillFrom(int line)
        {
            return line >= 0 && line < _fillFrom.Length ? _fillFrom[line] : NoFill;
        }

        /// <summary>Cinta del lleno de la línea <paramref name="line"/> (en el orden de las líneas del Mapa).</summary>
        public RiverRibbonGraphic FillRibbon(int line)
        {
            return _fill[line];
        }

        /// <summary>Cinta <paramref name="i"/> del frente (0 … <see cref="FrontPieceCount"/> − 1, en el orden de la ruta).</summary>
        public RiverRibbonGraphic FrontRibbon(int i)
        {
            return _front[i];
        }

        /// <summary>Nivel de la boya <paramref name="i"/> (0-9): 10 · (<see cref="FrontStop"/> − 1) + i + 1.</summary>
        public int BuoyLevel(int i)
        {
            return _buoyLevel[i];
        }

        /// <summary>Estado de la boya <paramref name="i"/>.</summary>
        public BuoyState BuoyStateAt(int i)
        {
            return _buoyState[i];
        }

        /// <summary>Dónde está la boya <paramref name="i"/>, en unidades del marco: a (i + 1) / 10 de la ruta, por su longitud real.</summary>
        public Vector2 BuoyMapPoint(int i)
        {
            return _buoyMap[i];
        }

        /// <summary>Icono de Rosa de la boya <paramref name="i"/> (activo solo si se saltó).</summary>
        internal Image BuoyRosa(int i)
        {
            return _rosa[i];
        }

        /// <summary>
        /// Rosas de los niveles saltados en los tramos ya recorridos (04 §2.6: el nivel saltado queda marcado en el Mapa con
        /// Rosa), sin contar las del frente.
        /// </summary>
        public int PastRosaCount => _pastRosaCount;

        /// <summary>Nivel de la Rosa <paramref name="i"/> de los tramos recorridos (0 … <see cref="PastRosaCount"/> − 1, de menor a mayor).</summary>
        public int PastRosaLevel(int i)
        {
            return _pastRosaLevel[i];
        }

        /// <summary>Dónde está la Rosa <paramref name="i"/> de los tramos recorridos, en unidades del marco: su boya.</summary>
        public Vector2 PastRosaMapPoint(int i)
        {
            return _pastRosaMap[i];
        }

        /// <summary>Icono de la Rosa <paramref name="i"/> de los tramos recorridos.</summary>
        internal Image PastRosa(int i)
        {
            return _pastRosa[i];
        }

        /// <summary>Capa de Rosa (lienzo dinámico, encima de la canoa: si no, la canoa la tapa en la boya que lleva).</summary>
        internal RectTransform? RosaLayer => _rosaLayer;

        /// <summary>
        /// Caja de la canoa en coordenadas locales del contenido (dp, y hacia arriba; la de su imagen, con su pivote), o una caja
        /// vacía sin canoa. Las siluetas la dejan libre.
        /// </summary>
        internal Rect CanoeBoxLocal
        {
            get
            {
                if (!_hasCanoe)
                {
                    return Rect.zero;
                }

                RectTransform rt = _canoe.rectTransform;
                Vector2 size = rt.sizeDelta;
                Vector2 at = rt.anchoredPosition;
                return new Rect(at.x - (rt.pivot.x * size.x), at.y - (rt.pivot.y * size.y), size.x, size.y);
            }
        }

        /// <summary>Mundo de la silueta <paramref name="i"/> (en orden de mundo; solo las que se ven cuentan en <see cref="SilhouetteCount"/>).</summary>
        public int SilhouetteWorld(int i)
        {
            return ActiveSilhouette(i).World;
        }

        /// <summary>Texto de la silueta <paramref name="i"/> (<c>map_next_world</c>).</summary>
        public string SilhouetteText(int i)
        {
            return ActiveSilhouette(i).Label.text;
        }

        /// <summary>Placa de la silueta <paramref name="i"/>.</summary>
        internal Image SilhouettePlate(int i)
        {
            return ActiveSilhouette(i).Plate;
        }

        /// <summary>Texto de la silueta <paramref name="i"/> (tests: tamaño y caja).</summary>
        internal TextMeshProUGUI SilhouetteLabel(int i)
        {
            return ActiveSilhouette(i).Label;
        }

        /// <summary>Punto de la silueta <paramref name="i"/> (su gran parada), en unidades del marco.</summary>
        public Vector2 SilhouetteMapPoint(int i)
        {
            return ActiveSilhouette(i).MapPoint;
        }

        /// <summary>Rótulo de país <paramref name="i"/> (en el orden de <c>labels</c>; activo solo si su nombre tiene texto).</summary>
        internal TextMeshProUGUI CountryLabel(int i)
        {
            return _countries[i];
        }

        /// <summary>Opacidad de la cinta del lleno de la línea <paramref name="line"/> (menos de 1 mientras se funde).</summary>
        public float FillAlpha(int line)
        {
            return _fill[line].canvasRenderer.GetAlpha();
        }

        /// <summary>
        /// Parada hacia la que va el frente con <paramref name="maxLevel"/> niveles ganados: la del tramo en curso,
        /// <c>max / 10 + 1</c>, salvo en los múltiplos de 10 desde el 10, en que es la recién alcanzada (<c>max / 10</c>, con el
        /// tramo lleno: <see cref="RiverJourney.LegFill"/> = 1). 1 con max 0; 6 con 58 y con 60; 7 con 61.
        /// </summary>
        public static int FrontStopOf(int maxLevel)
        {
            int max = Mathf.Clamp(maxLevel, 0, WorldTable.LastLevel);
            int per = RiverStopCatalog.LevelsPerStop;
            return max >= per && max % per == 0 ? max / per : (max / per) + 1;
        }

        /// <summary>
        /// Lleno del Mapa con <paramref name="maxLevel"/> niveles ganados (D-013, punto 4; propuesta del Mapa §3.4), sin el frente:
        /// para cada línea, en <paramref name="from"/> (uno por línea del Mapa), la s desde la que va llena hasta su boca (s = 1),
        /// o <see cref="NoFill"/>.
        /// <list type="bullet">
        /// <item>Las rutas de las paradas alcanzadas (0 … <see cref="FrontStopOf"/> − 1) llenan cada línea desde su punto más alto
        /// hasta la boca: el agua entra siempre desde el mar.</item>
        /// <item>Cada boca que pasan esas rutas (y las que ya pasó el frente, por su posición en la ruta) llena su línea
        /// <see cref="MouthFill"/> desde la boca: entera la de paso y la local; solo la boca la de un mundo futuro. No se llenan
        /// así las líneas que remonta alguna ruta del catálogo (<see cref="RouteClimbs"/>: el Tapajós, el Negro, el Nanay, el
        /// Marañón…): se llenan por la ruta, para que la boca no se trague el frente de sus tramos (D-013, punto 4: cada victoria
        /// llena una décima del tramo; propuesta §5.1: «4 hasta la parada»).</item>
        /// </list>
        /// Creciente con <paramref name="maxLevel"/>: nunca retrocede. Sin asignaciones.
        /// </summary>
        public static void ComputeFill(RiverMap map, RiverRoutes routes, int maxLevel, float[] from)
        {
            ComputeFill(map, routes, maxLevel, from, false);
        }

        /// <summary>
        /// Como <see cref="ComputeFill(RiverMap, RiverRoutes, int, float[])"/>; con <paramref name="frontAhead"/> y
        /// <paramref name="maxLevel"/> justo en una llegada (10k, k ≥ 1), el tramo de esa llegada cuenta como alcanzado y el
        /// frente es el siguiente, aún vacío (<see cref="FrontAheadStop"/>: el primer fotograma del viaje del fin de contenido,
        /// revisión de FX13). Sin asignaciones.
        /// </summary>
        public static void ComputeFill(RiverMap map, RiverRoutes routes, int maxLevel, float[] from, bool frontAhead)
        {
            if (map == null || routes == null || from == null)
            {
                throw new ArgumentNullException(map == null ? nameof(map) : routes == null ? nameof(routes) : nameof(from));
            }

            IReadOnlyList<RiverLine> lines = map.Lines;
            if (from.Length < lines.Count)
            {
                throw new ArgumentException("Hace falta un valor por línea del Mapa.", nameof(from));
            }

            for (int i = 0; i < from.Length; i++)
            {
                from[i] = NoFill;
            }

            int max = Mathf.Clamp(maxLevel, 0, WorldTable.LastLevel);
            int ahead = frontAhead ? FrontAheadStop(routes, max) : -1;
            int front = ahead >= 0 ? ahead : FrontStopOf(max);
            float frontFill = ahead >= 0 ? 0f : RiverJourney.LegFill(max);
            int reached = Mathf.Min(front - 1, routes.LegCount - 1);
            for (int n = 0; n <= reached; n++)
            {
                RiverStopRoute? route = routes.At(n);
                if (route == null)
                {
                    continue;
                }

                IReadOnlyList<RiverPathPiece> path = route.Path;
                for (int p = 0; p < path.Count; p++)
                {
                    RiverPathPiece piece = path[p];
                    from[piece.Line] = Mathf.Min(from[piece.Line], Mathf.Clamp01(Mathf.Min(piece.S0, piece.S1)));
                }

                FillMouths(map, routes, route, 1f, from);
            }

            RiverStopRoute? frontRoute = routes.At(front);
            if (frontRoute != null)
            {
                FillMouths(map, routes, frontRoute, frontFill, from);
            }
        }

        /// <summary>
        /// El tramo siguiente a la llegada de <paramref name="maxLevel"/> (10k, k ≥ 1) si tiene ruta (<see cref="RiverRoutes.At"/>,
        /// con la meta tras la última parada), o −1: el frente con que arranca el viaje del fin de contenido (revisión de FX13: con
        /// el tramo de la llegada, sin largo en Mundo en Tambo, el primer fotograma enseñaba sus 10 boyas en la fila corta bajo la
        /// parada antes de saltar al tramo de la naciente). Sin asignaciones.
        /// </summary>
        public static int FrontAheadStop(RiverRoutes routes, int maxLevel)
        {
            int per = RiverStopCatalog.LevelsPerStop;
            if (routes == null || maxLevel < per || maxLevel % per != 0 || maxLevel >= WorldTable.LastLevel)
            {
                return -1;
            }

            int next = (maxLevel / per) + 1;
            return routes.At(next) != null ? next : -1;
        }

        /// <summary>
        /// Fracción (0-1, por longitud de arco) de la ruta <paramref name="route"/> en la que pasa la boca de la línea
        /// <paramref name="line"/>: el punto de su <c>mouth</c> sobre el tramo de la ruta que recorre la línea anfitriona. Si la
        /// ruta no recorre la boca (o no tiene largo), la boca se da por pasada al llegar (1; 0 sin largo). Sin asignaciones.
        /// </summary>
        public static float MouthFraction(RiverMap map, RiverStopRoute route, int line)
        {
            RiverMapPlace? mouth = map.Lines[line].Mouth;
            IReadOnlyList<RiverPathPiece> path = route.Path;
            float total = 0f;
            for (int i = 0; i < path.Count; i++)
            {
                total += Mathf.Abs(path[i].S1 - path[i].S0) * map.Lines[path[i].Line].Length;
            }

            if (total <= 0f)
            {
                return 0f;
            }

            if (mouth == null)
            {
                return 1f;
            }

            RiverMapPlace m = mouth.Value;
            float arc = 0f;
            for (int i = 0; i < path.Count; i++)
            {
                RiverPathPiece piece = path[i];
                float hostLength = map.Lines[piece.Line].Length;
                float lo = Mathf.Min(piece.S0, piece.S1);
                float hi = Mathf.Max(piece.S0, piece.S1);
                if (piece.Line == m.Line && m.S >= lo && m.S <= hi)
                {
                    return Mathf.Clamp01((arc + (Mathf.Abs(m.S - piece.S0) * hostLength)) / total);
                }

                arc += (hi - lo) * hostLength;
            }

            return 1f;
        }

        /// <summary>
        /// Pinta el río con <paramref name="maxLevel"/> niveles ganados: el lleno, el frente con sus boyas y las siluetas de los
        /// mundos futuros. Solo recalcula si cambia el nivel (o el Mapa); asigna su texto solo aquí, nunca por fotograma. Lo
        /// llama <see cref="Open"/> con <c>max_level</c>; «ponerse al día» (M3.5) puede usarlo para enseñar los niveles de uno en
        /// uno.
        /// </summary>
        internal void ShowProgressAt(int maxLevel)
        {
            RiverMap? map = _map;
            RiverRoutes? routes = _routes;
            if (map == null || routes == null || _fillLayer == null)
            {
                return;
            }

            int max = Mathf.Clamp(maxLevel, 0, WorldTable.LastLevel);
            if (ReferenceEquals(map, _progressMap) && max == _progressLevel)
            {
                RefreshBuoyStates();
                return;
            }

            _progressMap = map;
            _progressLevel = max;
            _progressBuilds++;
            bool ahead = FrontAheadNow(max);
            ComputeFill(map, routes, max, _fillFrom, ahead);
            IReadOnlyList<RiverLine> lines = map.Lines;
            for (int i = 0; i < _fill.Count; i++)
            {
                bool used = i < lines.Count && _fillFrom[i] < 1f;
                ShowRibbon(_fill[i], used, used ? _fillFrom[i] : 1f, 1f);
                ShowRibbon(_fillCasing[i], used, used ? _fillFrom[i] : 1f, 1f);
            }

            BuildFront(map, routes, max, ahead);
            if (BuildSilhouettes(map, routes, max) && _silhouettesPlaced)
            {
                ReplaceSilhouettes(); // «ponerse al día» cruzó una gran parada: las placas, cada una junto a la de su mundo (FX8)
            }
        }

        // Las placas que se ven cambiaron sin cambiar de vista («ponerse al día» que cruza una gran parada; tarea FX8): BuildSilhouettes
        // reutiliza la caja de una silueta para otro mundo, y sin esto se quedaba en el sitio del mundo de antes (captura de M4.2:
        // al llegar la canoa a Santarém, la caja del mundo 2 pasó a decir «Mundo 3 · Manaos · Nivel 41» en el sitio del mundo 2).
        // Se vuelven a colocar los rótulos del lienzo estático como al cambiar de vista (ProgressSpace: los controles de la Cuenca,
        // los países y las placas), con la canoa en su sitio de max_level como al abrir (PlaceSilhouettes), y, en la vista Mundo,
        // se apartan de lo fijo del encuadre que ya se ve (ResolvePlates, sin mover el encuadre); con el norte arriba, se recalcula
        // su corrimiento. Solo cuando cambia el conjunto (≤ una vez por gran parada), nunca por fotograma.
        private void ReplaceSilhouettes()
        {
            MapSpace space = _space;
            BasinControlObstacles();
            _placed.Clear();
            PlaceCountries(space);
            PlaceSilhouettes(space);
            if (_view == MapView.World && _markersReady && _markerCount > 0)
            {
                ResolvePlates(_offset);
            }

            if (_northUpOn)
            {
                NorthUpLabelShifts();
                ApplyNorthUp(true);
            }

            _silhouetteReplacements++;
        }

        // ============================================================================================ ganchos

        partial void ProgressBuild(RectTransform staticLayer, RectTransform dynamicLayer)
        {
            RiverMap map = _map!;
            IReadOnlyList<RiverLine> lines = map.Lines;
            if (_fillLayer == null)
            {
                _fillLayer = UiBuilder.Rect("Fill", staticLayer);
                FillTopLeft(_fillLayer);
                // Dos pasadas (RibbonPart): las orlas de todo el lleno (y la del frente) debajo y el agua encima, para que en una
                // confluencia el afluente entre en el cauce sin una costura de orla.
                _fillCasingLayer = UiBuilder.Rect("Casing", _fillLayer);
                FillTopLeft(_fillCasingLayer);
                _fillCoreLayer = UiBuilder.Rect("Water", _fillLayer);
                FillTopLeft(_fillCoreLayer);
                _labelLayer = UiBuilder.Rect("Labels", staticLayer);
                FillTopLeft(_labelLayer);

                _frontLayer = UiBuilder.Rect("Front", dynamicLayer);
                FillTopLeft(_frontLayer);
                _frontLayer.SetAsFirstSibling(); // detrás de la canoa y de los marcadores
                _frontCoreLayer = UiBuilder.Rect("FrontWater", _frontLayer);
                FillTopLeft(_frontCoreLayer);
                _buoys = NewBuoys("Buoys", _frontLayer, false);

                // Las boyas por ganar van encima de la canoa: en la punta del agua, la canoa tapa las de delante (y en la vista
                // Mundo un tramo corto cabe entero bajo ella), y son el «solo me falta uno» (propuesta §4.1).
                _aheadLayer = UiBuilder.Rect("BuoysAhead", dynamicLayer);
                FillTopLeft(_aheadLayer);
                _aheadLayer.SetSiblingIndex(_canoe.transform.GetSiblingIndex() + 1);
                _buoysAhead = NewBuoys("Buoys", _aheadLayer, true);

                // Rosa va encima de la canoa: la boya de la punta del agua cae justo debajo de ella.
                _rosaLayer = UiBuilder.Rect("SkippedRosa", dynamicLayer);
                FillTopLeft(_rosaLayer);
                _rosaLayer.SetSiblingIndex(_aheadLayer.GetSiblingIndex() + 1);
                for (int i = 0; i < _rosa.Length; i++)
                {
                    _rosa[i] = NewRosa("SkippedRosa_" + i);
                }

                // El hito y el rótulo de la naciente, encima de Rosa y de la canoa: que la canoa no tape la meta (revisión de FX13).
                SourceBuild(dynamicLayer, _rosaLayer);

                // Rótulos fijos de orientación (propuesta del Mapa §3.1): en el marco, encima del mapa y por debajo de «Volver a
                // la canoa». No giran con «Norte arriba».
                Transform frame = _frame.transform;
                int buttonIndex = _backToCanoe.Button.transform.GetSiblingIndex();
                _andesLabel = UiBuilder.Label("AndesLabel", frame, TextStyle.ModalTitle, string.Empty, OrientationLabelSp, Palette.Cream, TextAlignmentOptions.Center);
                _andesLabel.transform.SetSiblingIndex(buttonIndex);
                _atlanticLabel = UiBuilder.Label("AtlanticLabel", frame, TextStyle.ModalTitle, string.Empty, OrientationLabelSp, Palette.Cream, TextAlignmentOptions.Left);
                _atlanticLabel.transform.SetSiblingIndex(buttonIndex + 1);
            }

            // El vidrio vacío, fantasma: el progreso es lo que tiene que leerse (solo en esta pantalla; el borde no cambia).
            for (int i = 0; i < lines.Count; i++)
            {
                Glass(i).SetGlassBodyAlpha(MapGlassBodyAlpha);
            }

            // Una cinta del lleno por línea, con su orla aparte (el cauce y el ramal encima, para que las bocas queden debajo del
            // río grande).
            while (_fill.Count < lines.Count)
            {
                _fill.Add(NewRibbon("Fill_" + _fill.Count, _fillCoreLayer!, RibbonPart.Core));
                _fillCasing.Add(NewRibbon("FillCasing_" + _fillCasing.Count, _fillCasingLayer!, RibbonPart.Casing));
            }

            for (int i = 0; i < _fill.Count; i++)
            {
                bool used = i < lines.Count;
                ResetFillRibbon(_fill[i], used ? lines[i] : null);
                ResetFillRibbon(_fillCasing[i], used ? lines[i] : null);
                if (used)
                {
                    _fill[i].gameObject.name = "Fill_" + lines[i].Id;
                    _fillCasing[i].gameObject.name = "FillCasing_" + lines[i].Id;
                }
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Kind == RiverLineKind.Main)
                {
                    _fill[i].transform.SetAsLastSibling();
                }
            }

            if (_fillFrom.Length != lines.Count)
            {
                _fillFrom = new float[lines.Count];
                _seenFrom = new float[lines.Count];
            }

            // Los países en su región (Q8-A): un rótulo por entrada de labels.
            IReadOnlyList<RiverMapLabel> labels = map.Labels;
            while (_countries.Count < labels.Count)
            {
                Color cream = Palette.Cream;
                TextMeshProUGUI label = UiBuilder.Label("Country_" + _countries.Count, _labelLayer!, TextStyle.PillWood, string.Empty, CountryLabelSp,
                    new Color(cream.r, cream.g, cream.b, CountryLabelAlpha), TextAlignmentOptions.Center);
                Material? thin = CountryMaterial();
                if (thin != null)
                {
                    label.fontSharedMaterial = thin;
                }

                RectTransform rt = label.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                _countries.Add(label);
            }

            for (int i = 0; i < _countries.Count; i++)
            {
                _countries[i].gameObject.SetActive(false);
                if (i < labels.Count)
                {
                    _countries[i].gameObject.name = "Country_" + labels[i].Country;
                }
            }

            _progressMap = null;
            _progressLevel = -1;
            _silhouettesPlaced = false;
        }

        partial void ProgressOpen(IMapProgress progress)
        {
            _silhouettesPlaced = false; // hasta que ApplyView las coloque en esta apertura (FX8)
            if (_map == null || _fillLayer == null)
            {
                return;
            }

            int max = Mathf.Clamp(progress.MaxLevel, 0, WorldTable.LastLevel);
            ShowProgressAt(max);
            ApplyProgressTexts();
            StartFades(Mathf.Clamp(progress.MapSeenLevel, 0, max));
        }

        // FX12: las anclas de la curva de cada línea (ApplyLineAnchors) en el lleno y su orla, como en el vidrio.
        partial void ProgressAnchors(RiverMap map)
        {
            for (int i = 0; i < _fill.Count; i++)
            {
                RiverLine? line = i < map.Lines.Count ? map.Lines[i] : null;
                _fill[i].SetAnchors(LineAnchors(line));
                _fillCasing[i].SetAnchors(LineAnchors(line));
            }
        }

        partial void ProgressSpace(MapSpace space)
        {
            if (_map == null || _fillLayer == null)
            {
                return;
            }

            for (int i = 0; i < _fill.Count; i++)
            {
                FitRibbon(_fill[i], space);
                FitRibbon(_fillCasing[i], space);
            }

            for (int i = 0; i < _frontCount; i++)
            {
                FitRibbon(_front[i], space);
                FitRibbon(_frontCasing[i], space);
            }

            // FX12: el tope del frente, repartido otra vez a la escala de la vista nueva (el vidrio ya la tiene).
            if (_frontRoute != null)
            {
                FitFrontBudgets(_map!, _frontRoute);
                for (int i = 0; i < _frontCount; i++)
                {
                    int piece = _frontPathIndex[i];
                    _front[i].SetVertexBudget(_frontCoreBudget[piece]);
                    _frontCasing[i].SetVertexBudget(_frontCasingBudget[piece]);
                }
            }

            PlaceBuoys(space);
            LayoutOrientationLabels(); // antes que las placas: «Norte arriba» se coloca bajo «Andes ↑» si lo pisaría (FX7)
            BasinControlObstacles(); // los controles de la Cuenca, obstáculo de los países y de las placas (FX7)
            _placed.Clear();
            PlaceCountries(space);
            PlaceSilhouettes(space);
            _silhouettesPlaced = true;
        }

        // Los países van en su punto de labels, pero nunca se salen del mapa por un lado (se corren hacia dentro lo justo) ni
        // pisan el río, que es el protagonista: si la caja choca con una línea de clase 1-2, o queda a menos de
        // CountryLineClearanceDp de su borde, se busca el sitio libre más cercano, hasta CountryMaxShiftDp (en horizontal y en
        // vertical primero). Sin hueco libre, el de más hueco. Tampoco se pisan entre sí ni, en la Cuenca sin girar, quedan bajo sus
        // controles (BasinControlObstacles: «Norte arriba», «Volver a la canoa», la canoa con su equipo, las boyas y los rótulos
        // fijos «Andes ↑» y «Océano Atlántico ↓»; revisión de FX7: con max 140 «Ecuador» quedaba tras «Norte arriba»): si los
        // controles le quitan todos los sitios, o los únicos que no pisaban el río (CountryBasinFallback), el país se corre a la
        // izquierda del control que pisa en su punto (su borde derecho a CountryControlGapDp de él) y, si ahí pisa el río u otro
        // rótulo, no sale en esa vista [P] (CountryBasinHidden; con el norte arriba vuelve, en su sitio de esa vista). Si fuera de
        // los controles solo quedan sitios sobre el río y bajo ellos también, el de más hueco, como antes. Solo al cambiar de vista;
        // sin asignaciones (salvo las tablas de la búsqueda si cambia el número de países).
        private void PlaceCountries(MapSpace space)
        {
            RiverMap map = _map!;
            float contentW = space.DpPerUnit * map.Width;
            float contentH = space.DpPerUnit * map.Height;
            IReadOnlyList<RiverMapLabel> labels = map.Labels;
            if (_countryBasinHidden.Length != _countries.Count)
            {
                _countryBasinHidden = new bool[_countries.Count];
                _countryFreeClearance = new float[_countries.Count];
                _countryUnderClearance = new float[_countries.Count];
            }

            float u = NorthUpEase(_northUpT);
            for (int i = 0; i < labels.Count && i < _countries.Count; i++)
            {
                TextMeshProUGUI label = _countries[i];
                _countryBasinHidden[i] = false;
                _countryFreeClearance[i] = float.NegativeInfinity;
                _countryUnderClearance[i] = float.NegativeInfinity;
                if (!label.gameObject.activeSelf)
                {
                    continue;
                }

                RectTransform rt = label.rectTransform;
                Vector2 size = rt.sizeDelta;
                Vector2 at = space.ToLocal(labels[i].X, labels[i].Y);
                Vector2 best = ClampBox(at, size, contentW, contentH);
                float bestClearance = float.NegativeInfinity;
                int rings = Mathf.RoundToInt(CountryMaxShiftDp / LabelSearchStepDp);
                bool found = false;
                Vector2 home = best;
                bool blocked = false;
                float underClearance = float.NegativeInfinity; // el mejor hueco con el río de los sitios que pisan un control
                for (int ring = 0; ring <= rings && !found; ring++)
                {
                    int directions = ring == 0 ? 1 : SearchDirections.Length;
                    for (int d = 0; d < directions && !found; d++)
                    {
                        Vector2 c = ClampBox(at + (SearchDirections[d] * (ring * LabelSearchStepDp)), size, contentW, contentH);
                        Rect box = new Rect(c.x - (size.x * 0.5f), c.y - (size.y * 0.5f), size.x, size.y);
                        if (OverlapsPlaced(box))
                        {
                            continue;
                        }

                        if (HitsBasinControl(box))
                        {
                            blocked = true;
                            if (underClearance < 0f)
                            {
                                underClearance = Mathf.Max(underClearance, LineClearance(map, space, _view, box, 0f));
                            }

                            continue;
                        }

                        float clearance = LineClearance(map, space, _view, box, CountryLineClearanceDp);
                        if (clearance > bestClearance)
                        {
                            bestClearance = clearance;
                            best = c;
                        }

                        found = clearance >= CountryLineClearanceDp;
                    }
                }

                // Los controles le quitaron todos los sitios o los únicos que no pisaban el río (CountryBasinFallback): a la izquierda
                // del control que pisa en su punto (si ese sitio no pisa el río ni otro rótulo) o, si no, fuera de esta vista [P]. Si
                // los sitios que pisan un control también pisaban el río, se queda en `best`, el de más hueco, como antes de FX7.
                bool hidden = false;
                _countryFreeClearance[i] = bestClearance;
                _countryUnderClearance[i] = underClearance;
                if (CountryBasinFallback(blocked, bestClearance, underClearance))
                {
                    hidden = true;
                    int k = BasinControlHit(new Rect(home.x - (size.x * 0.5f), home.y - (size.y * 0.5f), size.x, size.y));
                    if (k >= 0)
                    {
                        // Los controles ya llevan MarkerObstacleGapDp de hueco: el borde derecho, a CountryControlGapDp del control.
                        float xMax = _basinControls[k].xMin + MarkerObstacleGapDp - CountryControlGapDp;
                        var c = new Vector2(xMax - (size.x * 0.5f), home.y);
                        var box = new Rect(xMax - size.x, c.y - (size.y * 0.5f), size.x, size.y);
                        hidden = box.xMin < LabelEdgeDp || OverlapsPlaced(box) || HitsBasinControl(box)
                            || LineClearance(map, space, _view, box, CountryLineClearanceDp) < 0f;
                        if (!hidden)
                        {
                            best = c;
                        }
                    }
                }

                _countryBasinHidden[i] = hidden;
                SetLabelAlpha(label.canvasRenderer, CountryAlpha(i, u));
                rt.anchoredPosition = best;
                if (!hidden)
                {
                    _placed.Add(new Rect(best.x - (size.x * 0.5f), best.y - (size.y * 0.5f), size.x, size.y));
                }
            }
        }

        /// <summary>
        /// El país <paramref name="i"/> (en el orden de <c>labels</c>) no sale en la Cuenca sin girar [P]: no cabe sin quedar bajo
        /// uno de sus controles o pisar el río (<see cref="PlaceCountries"/>, <see cref="CountryBasinFallback"/>; revisión de FX7).
        /// Con el norte arriba vuelve.
        /// </summary>
        internal bool CountryBasinHidden(int i)
        {
            return i < _countryBasinHidden.Length && _countryBasinHidden[i];
        }

        /// <summary>
        /// Lo que vio la búsqueda del país <paramref name="i"/> en su última colocación (tests de FX7): <paramref name="free"/>, el
        /// mejor hueco con el río de los sitios que no pisan ningún control ni otro rótulo, y <paramref name="under"/>, el de los
        /// que pisan un control (como mucho 0); −∞ si no hubo ninguno.
        /// </summary>
        internal void CountryBasinSearch(int i, out float free, out float under)
        {
            bool known = i < _countryFreeClearance.Length;
            free = known ? _countryFreeClearance[i] : float.NegativeInfinity;
            under = known ? _countryUnderClearance[i] : float.NegativeInfinity;
        }

        /// <summary>
        /// Un país de la Cuenca sin girar busca sitio a la izquierda del control que pisa en su punto o, si no lo hay, no sale en esa
        /// vista [P] (revisión de FX7): solo si algún control le quitó un sitio (<paramref name="blocked"/>) y, o no le queda ninguno
        /// fuera de los controles y de los otros rótulos (<paramref name="freeClearance"/> = −∞), o los que le quedan pisan el río
        /// (&lt; 0) y el control le quitó uno que no lo pisaba (<paramref name="underClearance"/> ≥ 0): antes de FX7 salía ahí, bajo
        /// el control. Si los sitios bajo un control también pisaban el río, se queda en el de más hueco fuera de ellos, como antes.
        /// </summary>
        internal static bool CountryBasinFallback(bool blocked, float freeClearance, float underClearance)
        {
            return blocked && (float.IsNegativeInfinity(freeClearance) || (freeClearance < 0f && underClearance >= 0f));
        }

        // La silueta, a la derecha de su gran parada o, si no cabe, a su izquierda, sin salirse del mapa y dejando libre la caja
        // de la canoa (con SilhouetteCanoeClearanceDp de hueco): si choca, prueba el otro lado y luego se mueve en vertical, hasta
        // SilhouetteMaxShiftDp. Tampoco pisa los países, otra silueta ni, en la Cuenca, los controles que flotan encima del mapa
        // («Norte arriba» y, si sale, «Volver a la canoa»; tarea FX7, BasinControlObstacles). Va en el lienzo estático, por
        // debajo de la canoa, así que nunca puede quedar debajo de ella.
        // El lleno (el progreso del jugador) no se tapa si hay sitio (revisión de FX7: con max 200 las placas de los mundos 7 y 9
        // tapaban cintas del lleno): un sitio cercano vale solo si no pisa ninguna línea del lleno (FillLinesUnder) y deja
        // SilhouetteLineClearanceDp a las de clase 1-2. Si cerca no lo hay, en la Cuenca se busca en la zona visible con su
        // encuadre de apertura, hasta SilhouetteFarMaxDistDp de la parada, el sitio fuera del lleno más cercano a ella
        // (FarSpot); si tampoco lo hay, el respaldo es el sitio cercano que tapa menos líneas del lleno y, a igualdad, el de más
        // hueco. Si cerca no queda ningún sitio libre de lo demás (los países y los ríos más detallados de la geometría de
        // producción pueden llenar los huecos entre meandros; M5.1c), en la Cuenca el respaldo es el sitio de esa misma rejilla, a
        // su alcance, que tapa menos líneas del lleno y, a igualdad, el más cercano (FarSpot con leastFill), en vez del primer
        // sitio junto a la parada, que pisaba algo; en el Mundo queda ese primer sitio, y FX10 (PlatesFit) lo aparta o, si no
        // cabe, no lo enseña. Límites aceptados [P]: la placa puede quedar algo lejos de su parada (el texto la nombra), pero no en otra
        // región de la Cuenca, y una placa muy ancha puede no caber fuera del lleno sin pisar un control, un país o la canoa: con
        // max 200, la del mundo 7 («Madre de Dios», 215 dp en una Cuenca de 328) tapa las menos líneas del lleno que puede cerca de
        // su parada (dos). La rejilla de FarSpot va a pasos de SilhouetteFarStepDp y puede no ver un sitio fuera del lleno que solo
        // cabe entre dos de sus puntos: con el respaldo de M5.1c (leastFill) la placa tapa entonces líneas del lleno aunque ese
        // sitio exista (con max 300, la del mundo 8 sin sitio junto a su parada, caso que solo provoca PlateTestNoNearSpotWorld).
        // Fuera de la Cuenca (el Mundo, que se arrastra) no se busca lejos.
        private void PlaceSilhouettes(MapSpace space)
        {
            RiverMap map = _map!;
            float contentW = space.DpPerUnit * map.Width;
            float contentH = space.DpPerUnit * map.Height;
            Rect canoe = CanoeHomeBox(space);
            bool hasCanoe = _hasCanoe && canoe.width > 0f;
            if (hasCanoe)
            {
                float m = SilhouetteCanoeClearanceDp;
                canoe = Rect.MinMaxRect(canoe.xMin - m, canoe.yMin - m, canoe.xMax + m, canoe.yMax + m);
            }

            for (int i = 0; i < _silhouettes.Count; i++)
            {
                Silhouette s = _silhouettes[i];
                if (!s.Root.gameObject.activeSelf)
                {
                    continue;
                }

                Vector2 p = space.ToLocal(s.MapPoint);
                Vector2 size = s.Root.sizeDelta;
                bool preferLeft = p.x + SilhouetteGapDp + size.x > contentW - LabelEdgeDp;
                Rect first = default;
                Rect chosen = default;
                bool any = false;
                bool found = false;
                float bestClearance = float.NegativeInfinity;
                int bestFillHits = int.MaxValue;
                int steps = Mathf.RoundToInt(SilhouetteMaxShiftDp / LabelSearchStepDp);
                for (int k = 0; k <= 2 * steps && !found; k++)
                {
                    // 0, +1, −1, +2, −2 … pasos en vertical.
                    int n = (k + 1) / 2;
                    float dy = (k % 2 == 1 ? n : -n) * LabelSearchStepDp;
                    for (int side = 0; side < 2 && !found; side++)
                    {
                        bool left = side == 0 ? preferLeft : !preferLeft;
                        float x0 = left ? p.x - SilhouetteGapDp - size.x : p.x + SilhouetteGapDp;
                        x0 = Mathf.Clamp(x0, LabelEdgeDp, Mathf.Max(LabelEdgeDp, contentW - LabelEdgeDp - size.x));
                        float yc = Mathf.Clamp(p.y + dy, -contentH + (size.y * 0.5f) + LabelEdgeDp, -(size.y * 0.5f) - LabelEdgeDp);
                        var box = new Rect(x0, yc - (size.y * 0.5f), size.x, size.y);
                        if (k == 0 && side == 0)
                        {
                            first = box;
                        }

                        if ((hasCanoe && box.Overlaps(canoe)) || OverlapsPlaced(box) || HitsBasinControl(box) || TestObstacleHit(s.World, box)
                            || s.World == PlateTestNoNearSpotWorld)
                        {
                            continue;
                        }

                        // Como los países: el sitio libre más cercano que no tape el curso de los ríos (ni el frente, que va por
                        // ellos) ni el lleno. Si ninguno lo consigue, el respaldo es el que tapa menos líneas del lleno y, a
                        // igualdad, el de más hueco (salvo que en la Cuenca FarSpot encuentre uno fuera del lleno).
                        float clearance = LineClearance(map, space, _view, box, SilhouetteLineClearanceDp);
                        int fillHits = FillLinesUnder(map, space, _view, box);
                        if (!any || fillHits < bestFillHits || (fillHits == bestFillHits && clearance > bestClearance))
                        {
                            any = true;
                            bestFillHits = fillHits;
                            bestClearance = clearance;
                            chosen = box;
                        }

                        found = fillHits == 0 && clearance >= SilhouetteLineClearanceDp;
                    }
                }

                Rect plate = any ? chosen : first;
                int fillMin = any ? bestFillHits : -1;
                if (!found && (!any || bestFillHits > 0) && FarSpot(map, space, s.World, p, size, hasCanoe, canoe, false, out Rect far, out _))
                {
                    plate = far;
                    fillMin = 0;
                }
                else if (!any && FarSpot(map, space, s.World, p, size, hasCanoe, canoe, true, out far, out int farFill))
                {
                    // Cerca no quedaba ningún sitio libre de lo demás (M5.1c): en la Cuenca, el de la rejilla que tapa menos lleno.
                    plate = far;
                    fillMin = farFill;
                }

                s.FillMin = fillMin;
                s.FillHits = FillLinesUnder(map, space, _view, plate);
                s.Root.pivot = new Vector2(0f, 0.5f);
                s.Root.anchoredPosition = new Vector2(plate.xMin, plate.center.y);
                _placed.Add(plate);
            }
        }

        // La caja de la canoa en su sitio de max_level, como la deja ApplyView antes de colocar las placas (= CanoeBoxLocal en ese
        // momento): «ponerse al día» la lleva por el agua, y las placas que se vuelven a colocar por el camino (FX8,
        // ReplaceSilhouettes) quedan como en la vista recién abierta. Vacía sin canoa.
        private Rect CanoeHomeBox(MapSpace space)
        {
            if (!_hasCanoe)
            {
                return Rect.zero;
            }

            Vector2 at = space.ToLocal(_canoeMap);
            const float w = ProceduralArt.MapCanoeWidthDp;
            const float h = ProceduralArt.MapCanoeHeightDp;
            return new Rect(at.x - (0.5f * w), at.y - (CanoePivotY * h), w, h);
        }

        // En la Cuenca sin girar, el sitio de una placa (la del mundo world, de tamaño size) que no pisa ninguna línea del lleno, la
        // canoa, los países, las otras placas ni los controles, dentro de la zona visible con el encuadre de apertura (_basinFrame)
        // y del mapa (con LabelEdgeDp de margen) y con el centro a lo sumo a SilhouetteFarMaxDistDp más medio ancho de la parada p
        // (revisión de FX7: que la placa no salte a otra región de la Cuenca), más cercano a p; vale aunque cruce afluentes de clase
        // 1-2 que no estén llenos, y a igual distancia gana el de más hueco con ellos. En una rejilla de SilhouetteFarStepDp (y
        // pegada a los bordes derecho e inferior). False si no hay ninguno: entonces la placa se queda cerca de su parada (una
        // placa que se fuera lejos para tapar menos líneas, pero alguna, quitaría el sitio a las siguientes). Con leastFill (solo
        // si cerca no quedaba ningún sitio libre de lo demás; M5.1c), vale también un sitio que tapa el lleno: el que tapa menos
        // líneas y, a igualdad, el más cercano. fill, las líneas del lleno que tapa el sitio. Sin asignaciones.
        private bool FarSpot(RiverMap map, MapSpace space, int world, Vector2 p, Vector2 size, bool hasCanoe, Rect canoe, bool leastFill,
            out Rect spot, out int fill)
        {
            spot = default;
            fill = 0;
            if (_view != MapView.Basin || !_basinFrameSet)
            {
                return false;
            }

            float contentW = space.DpPerUnit * map.Width;
            float contentH = space.DpPerUnit * map.Height;
            float xMin = Mathf.Max(_basinFrame.xMin, 0f) + LabelEdgeDp;
            float xMax = Mathf.Min(_basinFrame.xMax, contentW) - LabelEdgeDp;
            float yMin = Mathf.Max(_basinFrame.yMin, -contentH) + LabelEdgeDp;
            float yMax = Mathf.Min(_basinFrame.yMax, 0f) - LabelEdgeDp;
            if (xMax - xMin < size.x || yMax - yMin < size.y)
            {
                return false;
            }

            int cols = Mathf.FloorToInt((xMax - xMin - size.x) / SilhouetteFarStepDp);
            int rows = Mathf.FloorToInt((yMax - yMin - size.y) / SilhouetteFarStepDp);
            float reach = SilhouetteFarMaxDistDp + (size.x * 0.5f);
            bool any = false;
            int bestFill = int.MaxValue;
            float bestDistance = float.PositiveInfinity;
            float bestClearance = float.NegativeInfinity;
            for (int r = 0; r <= rows + 1; r++)
            {
                float y = r <= rows ? yMin + (r * SilhouetteFarStepDp) : yMax - size.y;
                for (int c = 0; c <= cols + 1; c++)
                {
                    float x = c <= cols ? xMin + (c * SilhouetteFarStepDp) : xMax - size.x;
                    var box = new Rect(x, y, size.x, size.y);
                    float distance = PointBoxDistance(p, box);
                    if ((!leastFill && distance > bestDistance + 0.01f) || (box.center - p).sqrMagnitude > reach * reach
                        || (hasCanoe && box.Overlaps(canoe)) || OverlapsPlaced(box) || HitsBasinControl(box) || TestObstacleHit(world, box))
                    {
                        continue;
                    }

                    int hits = FillLinesUnder(map, space, _view, box);
                    if (leastFill ? any && (hits > bestFill || (hits == bestFill && distance > bestDistance + 0.01f)) : hits > 0)
                    {
                        continue;
                    }

                    float clearance = LineClearance(map, space, _view, box, SilhouetteLineClearanceDp);
                    if (!any || hits < bestFill || distance < bestDistance - 0.01f || clearance > bestClearance)
                    {
                        any = true;
                        bestFill = hits;
                        bestDistance = distance;
                        bestClearance = clearance;
                        spot = box;
                    }
                }
            }

            fill = any ? bestFill : 0;
            return any;
        }

        /// <summary>
        /// Líneas del lleno que tapa la placa de la silueta <paramref name="i"/> (en el orden de <see cref="SilhouetteCount"/>),
        /// contadas al colocarla (tests de FX7).
        /// </summary>
        internal int SilhouetteFillHits(int i)
        {
            return ActiveSilhouette(i).FillHits;
        }

        /// <summary>
        /// Las menos líneas del lleno que tapaba un sitio de la placa <paramref name="i"/> libre de los demás obstáculos entre los
        /// que probó su búsqueda: la cercana o, si en la Cuenca encontró uno fuera del lleno en la zona visible (hasta
        /// <see cref="SilhouetteFarMaxDistDp"/> de su parada), 0; si cerca ninguno estaba libre, en la Cuenca, las del sitio de esa
        /// zona que tapaba menos (M5.1c); −1 si no hubo ninguno libre (tests de FX7).
        /// </summary>
        internal int SilhouetteFillMin(int i)
        {
            return ActiveSilhouette(i).FillMin;
        }

        /// <summary>
        /// Cuántas líneas del lleno pisa la caja <paramref name="box"/> (coordenadas locales del contenido) con el ancho de su
        /// cinta en la vista actual (tests de FX7: las placas no tapan el lleno si hay sitio).
        /// </summary>
        internal int FillLinesUnderBox(Rect box)
        {
            return _map == null ? 0 : FillLinesUnder(_map, _space, _view, box);
        }

        /// <summary>
        /// La caja de la placa de la silueta <paramref name="i"/> (en el orden de <see cref="SilhouetteCount"/>) en coordenadas
        /// locales del contenido, sin girar.
        /// </summary>
        internal Rect SilhouetteLocalBox(int i)
        {
            RectTransform rt = ActiveSilhouette(i).Root;
            Vector2 size = rt.sizeDelta;
            Vector2 a = rt.anchoredPosition;
            return new Rect(a.x - (rt.pivot.x * size.x), a.y - (rt.pivot.y * size.y), size.x, size.y);
        }

        /// <summary>La caja del país <paramref name="i"/> en coordenadas locales del contenido, sin girar.</summary>
        internal Rect CountryLocalBox(int i)
        {
            RectTransform rt = _countries[i].rectTransform;
            Vector2 size = rt.sizeDelta;
            Vector2 a = rt.anchoredPosition;
            return new Rect(a.x - (rt.pivot.x * size.x), a.y - (rt.pivot.y * size.y), size.x, size.y);
        }

        /// <summary>Zona visible de la Cuenca con su encuadre de apertura, en coordenadas locales del contenido (false fuera de ella).</summary>
        internal bool BasinFrameLocal(out Rect frame)
        {
            frame = _basinFrame;
            return _basinFrameSet;
        }

        // Direcciones de la búsqueda de sitio de un rótulo: en horizontal y en vertical primero, luego en diagonal.
        private static readonly Vector2[] SearchDirections =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(0.7071068f, 0.7071068f), new Vector2(-0.7071068f, 0.7071068f),
            new Vector2(0.7071068f, -0.7071068f), new Vector2(-0.7071068f, -0.7071068f),
        };

        // Centro de una caja de «size» en c, corrido hacia dentro del mapa lo justo (LabelEdgeDp de margen).
        private static Vector2 ClampBox(Vector2 c, Vector2 size, float contentW, float contentH)
        {
            float hx = (size.x * 0.5f) + LabelEdgeDp;
            float hy = (size.y * 0.5f) + LabelEdgeDp;
            c.x = Mathf.Clamp(c.x, hx, Mathf.Max(hx, contentW - hx));
            c.y = Mathf.Clamp(c.y, Mathf.Min(-hy, -contentH + hy), -hy);
            return c;
        }

        private bool OverlapsPlaced(Rect box)
        {
            for (int i = 0; i < _placed.Count; i++)
            {
                if (box.Overlaps(_placed[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Hueco (dp) entre la caja <paramref name="box"/> (coordenadas locales del contenido) y el borde de la cinta más cercana
        /// de clase 1-2 del Mapa, con su ancho en la vista <paramref name="view"/>; negativo si la pisa. Deja de buscar por encima de
        /// <paramref name="enough"/> (devuelve como mucho ese valor). Sin asignaciones.
        /// </summary>
        internal static float LineClearance(RiverMap map, MapSpace space, MapView view, Rect box, float enough)
        {
            float clearance = enough;
            IReadOnlyList<RiverLine> lines = map.Lines;
            for (int l = 0; l < lines.Count; l++)
            {
                RiverLine line = lines[l];
                if (line.Class > 2)
                {
                    continue;
                }

                float half = (RibbonWidthDp(view, line.Class) * 0.5f) + RiverRibbonGraphic.CasingDp; // con la orla del lleno
                float reach = half + clearance;
                Rect near = Rect.MinMaxRect(box.xMin - reach, box.yMin - reach, box.xMax + reach, box.yMax + reach);
                Vector2 a = space.ToLocal(line.X(0), line.Y(0));
                for (int i = 1; i < line.PointCount; i++)
                {
                    Vector2 b = space.ToLocal(line.X(i), line.Y(i));
                    if (Mathf.Max(a.x, b.x) >= near.xMin && Mathf.Min(a.x, b.x) <= near.xMax
                        && Mathf.Max(a.y, b.y) >= near.yMin && Mathf.Min(a.y, b.y) <= near.yMax)
                    {
                        float gap = SegmentBoxDistance(a, b, box) - half;
                        if (gap < clearance)
                        {
                            clearance = gap;
                            reach = half + clearance;
                            near = Rect.MinMaxRect(box.xMin - reach, box.yMin - reach, box.xMax + reach, box.yMax + reach);
                        }
                    }

                    a = b;
                }
            }

            return clearance;
        }

        // Cuántas líneas del lleno (las de FillFrom < 1, de cualquier clase, en su trozo lleno: de FillFrom a la boca) pisa la caja
        // box (coordenadas locales del contenido) con el ancho de su cinta en la vista y su orla (revisión de FX7). Sin asignaciones.
        private int FillLinesUnder(RiverMap map, MapSpace space, MapView view, Rect box)
        {
            int hits = 0;
            IReadOnlyList<RiverLine> lines = map.Lines;
            int count = Mathf.Min(lines.Count, _fillFrom.Length);
            for (int l = 0; l < count; l++)
            {
                float from = _fillFrom[l];
                if (from >= 1f)
                {
                    continue;
                }

                RiverLine line = lines[l];
                float half = (RibbonWidthDp(view, line.Class) * 0.5f) + RiverRibbonGraphic.CasingDp;
                Rect near = Rect.MinMaxRect(box.xMin - half, box.yMin - half, box.xMax + half, box.yMax + half);
                float[] cumulative = line.Cumulative;
                float start = from * line.Length;
                Vector2 a = space.ToLocal(line.X(0), line.Y(0));
                for (int i = 1; i < line.PointCount; i++)
                {
                    Vector2 b = space.ToLocal(line.X(i), line.Y(i));
                    if (cumulative[i] >= start && SegmentHitsBox(a, b, near))
                    {
                        hits++;
                        break;
                    }

                    a = b;
                }
            }

            return hits;
        }

        // Distancia entre el segmento ab y la caja (0 si la cruza o la toca).
        private static float SegmentBoxDistance(Vector2 a, Vector2 b, Rect box)
        {
            if (SegmentHitsBox(a, b, box))
            {
                return 0f;
            }

            float d = Mathf.Min(PointBoxDistance(a, box), PointBoxDistance(b, box));
            d = Mathf.Min(d, PointSegmentDistance(new Vector2(box.xMin, box.yMin), a, b));
            d = Mathf.Min(d, PointSegmentDistance(new Vector2(box.xMax, box.yMin), a, b));
            d = Mathf.Min(d, PointSegmentDistance(new Vector2(box.xMin, box.yMax), a, b));
            return Mathf.Min(d, PointSegmentDistance(new Vector2(box.xMax, box.yMax), a, b));
        }

        // Liang-Barsky: el segmento ab entra en la caja.
        private static bool SegmentHitsBox(Vector2 a, Vector2 b, Rect box)
        {
            float t0 = 0f;
            float t1 = 1f;
            Vector2 d = b - a;
            return Clip(-d.x, a.x - box.xMin, ref t0, ref t1)
                && Clip(d.x, box.xMax - a.x, ref t0, ref t1)
                && Clip(-d.y, a.y - box.yMin, ref t0, ref t1)
                && Clip(d.y, box.yMax - a.y, ref t0, ref t1);
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Abs(p) < 1e-9f)
            {
                return q >= 0f;
            }

            float r = q / p;
            if (p < 0f)
            {
                if (r > t1)
                {
                    return false;
                }

                if (r > t0)
                {
                    t0 = r;
                }
            }
            else
            {
                if (r < t0)
                {
                    return false;
                }

                if (r < t1)
                {
                    t1 = r;
                }
            }

            return true;
        }

        private static float PointBoxDistance(Vector2 p, Rect box)
        {
            float dx = Mathf.Max(0f, Mathf.Max(box.xMin - p.x, p.x - box.xMax));
            float dy = Mathf.Max(0f, Mathf.Max(box.yMin - p.y, p.y - box.yMax));
            return Mathf.Sqrt((dx * dx) + (dy * dy));
        }

        private static float PointSegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            float t = len2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
            return Vector2.Distance(p, a + (ab * t));
        }

        partial void ProgressTick(float dtMs, ref bool animating)
        {
            if (_fading.Count == 0)
            {
                return;
            }

            _fadeElapsed += dtMs;
            float a = _fadeMs > 0f ? Mathf.Clamp01(_fadeElapsed / _fadeMs) : 1f;
            for (int i = 0; i < _fading.Count; i++)
            {
                _fill[_fading[i]].canvasRenderer.SetAlpha(a);
                _fillCasing[_fading[i]].canvasRenderer.SetAlpha(a);
            }

            if (a >= 1f)
            {
                _fading.Clear();
            }

            animating = true;
        }

        // ============================================================================================ frente y boyas

        // El tramo del frente hasta LegFill de su longitud real (una cinta por tramo de la ruta, sin saltar puntos, como el vidrio
        // y el lleno) y sus 10 boyas, a (i + 1) / 10 de la ruta.
        // Con «ahead» (FrontAheadNow), el tramo siguiente a la llegada de max, vacío.
        private void BuildFront(RiverMap map, RiverRoutes routes, int max, bool ahead)
        {
            int next = ahead ? FrontAheadStop(routes, max) : -1;
            int stop = next >= 0 ? next : FrontStopOf(max);
            RiverStopRoute? route = routes.At(stop);
            _frontCount = 0;
            _frontStop = route != null ? stop : -1;
            _frontFraction = route != null ? (next >= 0 ? 0f : RiverJourney.LegFill(max)) : 0f;
            if (route != null)
            {
                IReadOnlyList<RiverPathPiece> path = route.Path;
                float total = 0f;
                for (int i = 0; i < path.Count; i++)
                {
                    total += Mathf.Abs(path[i].S1 - path[i].S0) * map.Lines[path[i].Line].Length;
                }

                // Revisión de FX11: cada cinta del frente (y su orla) con el tope de una malla, como el vidrio y el lleno. Con el
                // tope del frente (FrontMaxVertices) repartido entre las piezas, en el tramo del 300 (5 piezas: Napo, Amazonas,
                // Madeira, Beni y Madre de Dios) cada una tenía 80 vértices y en Mundo saltaba puntos; el agua y su orla, con
                // distinto coste por sección, saltaban puntos distintos y ninguna seguía el vidrio: dos cauces a la vista. Ni
                // siquiera los 400 enteros por pieza bastan (la del Amazonas pide 360 de agua y 504 de orla). El frente solo se
                // rehace al cambiar su llenado (una vez por boya, ≤ 10 por apertura), y solo la pieza que cambia. Es una desviación provisional
                // [P] del «≤ 400 vértices» del frente de la propuesta del Mapa §11.4, anotada allí y pendiente del dueño: el frente del
                // 300 pide 2.534 vértices entre agua y orla en Mundo. Con el trazo curvo de FX12 pediría 5.764 con todos los pasos de la
                // curva; FitFrontBudgets lo deja en FrontCurveMaxVertices (el tope de FX11).
                FitFrontBudgets(map, route);
                float left = _frontFraction * total;
                for (int i = 0; i < path.Count && left > 0f; i++)
                {
                    RiverPathPiece piece = path[i];
                    RiverLine line = map.Lines[piece.Line];
                    float length = Mathf.Abs(piece.S1 - piece.S0) * line.Length;
                    if (length <= 0f)
                    {
                        continue;
                    }

                    float t = Mathf.Clamp01(left / length);
                    left -= length;
                    int k = _frontCount++;
                    float end = piece.S0 + ((piece.S1 - piece.S0) * t);
                    if (_frontPathIndex.Length <= k)
                    {
                        Array.Resize(ref _frontPathIndex, Mathf.Max(8, k * 2)); // solo crece
                    }

                    _frontPathIndex[k] = i;
                    SetFrontPiece(FrontRibbonAt(k), line, _frontCoreBudget[i], piece.S0, end);
                    SetFrontPiece(_frontCasing[k], line, _frontCasingBudget[i], piece.S0, end);
                    _front[k].SetAnchors(LineAnchors(line)); // FX12: la misma curva que el vidrio
                    _frontCasing[k].SetAnchors(LineAnchors(line));

                    // FX11: el espacio y el ancho de la vista al montarla. ProgressSpace solo ajusta las cintas que ya había al
                    // cambiar de vista; si «ponerse al día» llega a un tramo con más piezas que el de partida (en el 41, el de la
                    // parada 4: Tapajós y Amazonas), la cinta nueva se quedaba en el espacio por defecto de la Cuenca, fuera de la
                    // vista, y la reutilizada, con el de antes. Sin coste si no cambian (SetSpace y SetWidth comparan).
                    FitRibbon(_front[k], _space);
                    FitRibbon(_frontCasing[k], _space);
                }
            }

            for (int i = 0; i < _front.Count; i++)
            {
                bool used = i < _frontCount;
                if (_front[i].gameObject.activeSelf != used)
                {
                    _front[i].gameObject.SetActive(used);
                }

                if (_frontCasing[i].gameObject.activeSelf != used)
                {
                    _frontCasing[i].gameObject.SetActive(used);
                }
            }

            _frontRoute = route;
            _frontStopMap = route != null ? PlacePoint(map, route.At) : Vector2.zero;
            for (int i = 0; i < BuoysPerLeg; i++)
            {
                _buoyLevel[i] = route != null ? ((stop - 1) * RiverStopCatalog.LevelsPerStop) + i + 1 : 0;
                _buoyMap[i] = route != null ? RoutePoint(map, route, (i + 1) / (float)BuoysPerLeg) : Vector2.zero;
            }

            RefreshBuoyStates();
        }

        /// <summary>
        /// Tope de vértices del frente entero (el agua y la orla de todas sus piezas) en la vista Mundo con el trazo curvo [P]: el de
        /// <c>Fx11Tests</c> (FX11, pendiente del dueño). Revisión de FX12: con todos los pasos de la curva, el frente del 300 pedía
        /// 5.764; FitFrontBudgets lo reparte entre las piezas y cada una elige los pasos que caben con la tolerancia más fina que
        /// pueda (<see cref="RiverRibbonGraphic.CurveBudgetToleranceDp"/>), sin salirse de la curva del vidrio.
        /// </summary>
        public const int FrontCurveMaxVertices = 3000;

        // Revisión de FX12: el tope de vértices de cada pieza de la ruta del frente (el agua y la orla, con las mismas secciones). Si
        // con todos los pasos de la curva (lo que pide cada pieza en el vidrio de su línea, a la escala de la vista) el frente entero
        // cabe en FrontCurveMaxVertices, sin tope (el de una malla); si no, se reparte según lo que pide cada pieza (FX14: con un
        // mínimo por pieza, FrontMinSections). Sin trazo curvo (la Cuenca), sin tope, como desde FX11. Solo asigna si la ruta tiene
        // más piezas que ninguna antes.
        private void FitFrontBudgets(RiverMap map, RiverStopRoute route)
        {
            IReadOnlyList<RiverPathPiece> path = route.Path;
            if (_frontCoreBudget.Length < path.Count)
            {
                _frontCoreBudget = new int[path.Count];
                _frontCasingBudget = new int[path.Count];
            }

            int total = 0;
            for (int i = 0; i < path.Count; i++)
            {
                RiverPathPiece piece = path[i];
                RiverLine line = map.Lines[piece.Line];
                int demand = piece.Line < _glass.Count ? Glass(piece.Line).CurveDemand(piece.S0, piece.S1) : 0;
                _frontCoreBudget[i] = demand; // de momento, lo que pide
                total += demand * FrontSectionVertices(line, piece);
            }

            if (total <= FrontCurveMaxVertices)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    _frontCoreBudget[i] = RiverRibbonGraphic.MaxVertices;
                    _frontCasingBudget[i] = RiverRibbonGraphic.MaxVertices;
                }

                return;
            }

            // FX14: cada pieza se queda primero con lo que pide hasta FrontMinSections secciones (y nunca menos de
            // FrontSectionsMin, el mínimo de una cinta; una pieza corta, como la punta del Beni con el 300, dibuja todos sus pasos
            // y no se queda sin sitio ni para su plan) y el resto del tope se reparte según lo que pide de más cada una. Si esos
            // mínimos ya pasan del tope (una ruta de muchas piezas; hoy el frente tiene como mucho 5), lo que pasa de
            // FrontSectionsMin se reduce en proporción. Así la suma no pasa de FrontCurveMaxVertices, salvo que la ruta tenga
            // tantas piezas que ni FrontSectionsMin por pieza quepan.
            int reserved = 0;
            int rest = 0;
            int minimum = 0;
            for (int i = 0; i < path.Count; i++)
            {
                int vertices = FrontSectionVertices(map.Lines[path[i].Line], path[i]);
                int floor = FrontFloor(_frontCoreBudget[i]);
                reserved += floor * vertices;
                rest += Mathf.Max(0, _frontCoreBudget[i] - floor) * vertices;
                minimum += FrontSectionsMin * vertices;
            }

            bool scaled = reserved > FrontCurveMaxVertices;
            int left = scaled ? 0 : FrontCurveMaxVertices - reserved;
            int spare = Mathf.Max(0, FrontCurveMaxVertices - minimum); // con scaled, lo que cabe por encima de los mínimos
            int over = reserved - minimum;
            int used = 0;
            for (int i = 0; i < path.Count; i++)
            {
                RiverPathPiece piece = path[i];
                RiverLine line = map.Lines[piece.Line];
                int demand = _frontCoreBudget[i];
                int floor = FrontFloor(demand);
                int extra = rest > 0 ? (int)((long)left * Mathf.Max(0, demand - floor) / rest) : 0;
                if (scaled)
                {
                    floor = FrontSectionsMin + (over > 0 ? (int)((long)(floor - FrontSectionsMin) * spare / over) : 0);
                }

                int sections = floor + extra;
                used += sections * FrontSectionVertices(line, piece);
                _frontCoreBudget[i] = sections * RiverRibbonGraphic.SectionVertices(line, piece.S0, piece.S1, RibbonPart.Core);
                _frontCasingBudget[i] = sections * RiverRibbonGraphic.SectionVertices(line, piece.S0, piece.S1, RibbonPart.Casing);
            }

            Debug.Assert(used <= Mathf.Max(FrontCurveMaxVertices, minimum), "FitFrontBudgets: el frente pasa del tope de FX11");
        }

        // FX14: las secciones que una pieza del frente que pide demand se queda antes de repartir el resto del tope.
        private static int FrontFloor(int demand)
        {
            return Mathf.Max(FrontSectionsMin, Mathf.Min(demand, FrontMinSections));
        }

        // Secciones mínimas de una pieza del frente con tope (las de antes de FX14: Mathf.Max(4, ...)).
        private const int FrontSectionsMin = 4;

        /// <summary>
        /// FX14: secciones que una pieza del frente con trazo curvo y tope recibe, como mínimo, de lo que pide antes de repartir el
        /// resto del tope (<see cref="FrontCurveMaxVertices"/>) [P].
        /// </summary>
        public const int FrontMinSections = 24;

        // Vértices de una sección de la pieza del frente: el agua y la orla.
        private static int FrontSectionVertices(RiverLine line, RiverPathPiece piece)
        {
            return RiverRibbonGraphic.SectionVertices(line, piece.S0, piece.S1, RibbonPart.Core)
                + RiverRibbonGraphic.SectionVertices(line, piece.S0, piece.S1, RibbonPart.Casing);
        }

        // La cinta i del frente (el agua, en el lienzo dinámico, debajo de las boyas) y su orla (en el estático, con las del lleno:
        // así la orla del frente no corta el agua del lleno en una confluencia). La orla solo cambia al reconstruir el frente; si
        // «ponerse al día» (M3.5) la anima, rehace el lienzo estático mientras dura.
        private RiverRibbonGraphic FrontRibbonAt(int i)
        {
            while (_front.Count <= i)
            {
                _front.Add(NewRibbon("Front_" + _front.Count, _frontCoreLayer!, RibbonPart.Core));
                _frontCasing.Add(NewRibbon("FrontCasing_" + _frontCasing.Count, _fillCasingLayer!, RibbonPart.Casing));
            }

            return _front[i];
        }

        private static void SetFrontPiece(RiverRibbonGraphic ribbon, RiverLine line, int budget, float s0, float s1)
        {
            ribbon.SetLine(line);
            ribbon.SetLook(RibbonLook.Water);
            ribbon.SetVertexBudget(budget);
            ribbon.SetFill(s0, s1);
        }

        // Una cinta del Mapa en «parent», con la parte «part» (la orla o el agua del lleno y del frente).
        private static RiverRibbonGraphic NewRibbon(string name, RectTransform parent, RibbonPart part)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RiverRibbonGraphic));
            go.transform.SetParent(parent, false);
            FillTopLeft((RectTransform)go.transform);
            RiverRibbonGraphic ribbon = go.GetComponent<RiverRibbonGraphic>();
            ribbon.Setup();
            ribbon.SetPart(part);
            ribbon.SetSmooth(true); // M5.1c: los mismos puntos que el vidrio, sin dientes
            return ribbon;
        }

        // Una malla de boyas en «parent»: las encendidas (detrás de la canoa) o las por ganar (encima).
        private static BuoyGraphic NewBuoys(string name, RectTransform parent, bool ahead)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(BuoyGraphic));
            go.transform.SetParent(parent, false);
            FillTopLeft((RectTransform)go.transform);
            BuoyGraphic buoys = go.GetComponent<BuoyGraphic>();
            buoys.raycastTarget = false;
            buoys.Ahead = ahead;
            return buoys;
        }

        private static void ResetFillRibbon(RiverRibbonGraphic ribbon, RiverLine? line)
        {
            ribbon.SetLine(line);
            ribbon.SetLook(RibbonLook.Water);
            ribbon.SetFill(1f, 1f);
            ribbon.canvasRenderer.SetAlpha(1f);
            ribbon.gameObject.SetActive(false);
        }

        private static void ShowRibbon(RiverRibbonGraphic ribbon, bool on, float s0, float s1)
        {
            if (ribbon.gameObject.activeSelf != on)
            {
                ribbon.gameObject.SetActive(on);
            }

            ribbon.SetFill(s0, s1);
        }

        private void FitRibbon(RiverRibbonGraphic ribbon, MapSpace space)
        {
            if (ribbon.Line != null)
            {
                ribbon.SetSpace(space);
                ribbon.SetWidth(RibbonWidthDp(_view, ribbon.Line.Class));
                ribbon.SetCurve(_view == MapView.World); // FX12: la misma curva que el vidrio
            }
        }

        // Encendidas las boyas de los niveles ganados; con Rosa, las saltadas (pueden cambiar sin que cambie max_level: un test o
        // la nube). Sin asignaciones salvo el sprite de Rosa la primera vez que hace falta.
        private void RefreshBuoyStates()
        {
            IMapProgress? progress = _progress;
            int max = _progressLevel;
            bool buoys = _frontStop >= 0 && !string.Equals(_progressMapMode, MapConfig.MapModeWorlds, StringComparison.Ordinal);
            _buoyCount = buoys ? BuoysPerLeg : 0;
            for (int i = 0; i < BuoysPerLeg; i++)
            {
                int level = _buoyLevel[i];
                BuoyState state = BuoyState.Unlit;
                if (i < _buoyCount && level >= 1 && level <= max)
                {
                    state = progress != null && progress.IsSkipped(level) ? BuoyState.Skipped : BuoyState.Lit;
                }

                _buoyState[i] = state;
            }

            for (int i = 0; i < _rosa.Length; i++)
            {
                Image rosa = _rosa[i];
                if (rosa == null)
                {
                    continue;
                }

                bool show = _buoyState[i] == BuoyState.Skipped;
                if (show && rosa.sprite == null)
                {
                    rosa.sprite = _art.Rosa;
                }

                if (rosa.gameObject.activeSelf != show)
                {
                    rosa.gameObject.SetActive(show);
                }
            }

            RefreshPastRosa(!string.Equals(_progressMapMode, MapConfig.MapModeWorlds, StringComparison.Ordinal));
            PlaceBuoys(_space);
        }

        // Los niveles saltados de los tramos ya recorridos (antes del frente) siguen marcados con Rosa en su boya (04 §2.6), con
        // ui_map_mode = worlds, no (sin boyas). Recorre los niveles una vez al abrir; solo asigna al crecer la reserva de iconos.
        private void RefreshPastRosa(bool show)
        {
            IMapProgress? progress = _progress;
            RiverMap? map = _map;
            RiverRoutes? routes = _routes;
            _pastRosaCount = 0;
            int max = _progressLevel;
            int per = RiverStopCatalog.LevelsPerStop;
            int last = Mathf.Min(max, (FrontStopOf(max) - 1) * per);
            for (int level = 1; show && progress != null && map != null && routes != null && level <= last; level++)
            {
                if (!progress.IsSkipped(level))
                {
                    continue;
                }

                int stop = ((level - 1) / per) + 1;
                RiverStopRoute? route = routes.At(stop);
                if (route == null)
                {
                    continue;
                }

                int k = ((level - 1) % per) + 1;
                Vector2 point = RoutePoint(map, route, k / (float)per);
                float spacing = float.MaxValue;
                if (k > 1)
                {
                    spacing = Vector2.Distance(point, RoutePoint(map, route, (k - 1) / (float)per));
                }

                if (k < per)
                {
                    spacing = Mathf.Min(spacing, Vector2.Distance(point, RoutePoint(map, route, (k + 1) / (float)per)));
                }

                // La distancia más corta entre dos boyas del tramo: si a la escala de la vista no deja verlas, Rosa va en su sitio de
                // la fila corta bajo la parada, como las boyas del frente (PlaceBuoys).
                float legSpacing = float.MaxValue;
                Vector2 previous = RoutePoint(map, route, 1f / per);
                for (int b = 2; b <= per; b++)
                {
                    Vector2 next = RoutePoint(map, route, b / (float)per);
                    legSpacing = Mathf.Min(legSpacing, Vector2.Distance(previous, next));
                    previous = next;
                }

                int i = _pastRosaCount++;
                if (i == _pastRosa.Count)
                {
                    _pastRosa.Add(NewRosa("PastRosa_" + i));
                    _pastRosaMap.Add(point);
                    _pastRosaSpacing.Add(spacing);
                    _pastRosaLevel.Add(level);
                    _pastRosaLegSpacing.Add(legSpacing);
                    _pastRosaStop.Add(PlacePoint(map, route.At));
                }
                else
                {
                    _pastRosaMap[i] = point;
                    _pastRosaSpacing[i] = spacing;
                    _pastRosaLevel[i] = level;
                    _pastRosaLegSpacing[i] = legSpacing;
                    _pastRosaStop[i] = PlacePoint(map, route.At);
                }
            }

            for (int i = 0; i < _pastRosa.Count; i++)
            {
                Image rosa = _pastRosa[i];
                bool on = i < _pastRosaCount;
                if (on && rosa.sprite == null)
                {
                    rosa.sprite = _art.Rosa;
                }

                if (rosa.gameObject.activeSelf != on)
                {
                    rosa.gameObject.SetActive(on);
                }
            }
        }

        // Ancho de Rosa en una boya: el de la vista, menos si las boyas de su tramo van juntas (SkippedRosaSpacingFactor × su
        // distancia en dp), nunca por debajo de SkippedRosaMinDp.
        internal static float RosaWidthDp(MapView view, float buoySpacingDp)
        {
            float max = view == MapView.World ? SkippedRosaWorldDp : SkippedRosaBasinDp;
            return Mathf.Clamp(SkippedRosaSpacingFactor * buoySpacingDp, Mathf.Min(SkippedRosaMinDp, max), max);
        }

        // Un icono de Rosa en su capa (encima de la canoa), con un halo de vidrio_borde de SkippedRosaHaloDp.
        private Image NewRosa(string name)
        {
            Image rosa = UiBuilder.Image(name, _rosaLayer!, null, Color.white);
            rosa.preserveAspect = true;
            rosa.raycastTarget = false;
            RectTransform rt = rosa.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            Outline halo = rosa.gameObject.AddComponent<Outline>();
            halo.effectColor = Palette.GlassRim;
            halo.effectDistance = new Vector2(SkippedRosaHaloDp, -SkippedRosaHaloDp);
            halo.useGraphicAlpha = true;
            rosa.gameObject.SetActive(false);
            return rosa;
        }

        // El material de la píldora de madera con el contorno a CountryOutlineDp (uno para todos los países, creado una vez).
        private static Material? CountryMaterial()
        {
            if (_countryMaterial != null)
            {
                return _countryMaterial;
            }

            Material? source = UiFonts.Material(TextStyle.PillWood);
            if (source == null || !source.HasProperty(ShaderUtilities.ID_OutlineWidth))
            {
                return source;
            }

            float k = CountryOutlineDp / PillWoodOutlineDp;
            var thin = new Material(source) { name = source.name + " (país, 1 dp)" };
            thin.SetFloat(ShaderUtilities.ID_OutlineWidth, source.GetFloat(ShaderUtilities.ID_OutlineWidth) * k);
            if (thin.HasProperty(ShaderUtilities.ID_FaceDilate))
            {
                thin.SetFloat(ShaderUtilities.ID_FaceDilate, source.GetFloat(ShaderUtilities.ID_FaceDilate) * k);
            }

            _countryMaterial = thin;
            return thin;
        }

        private void PlaceBuoys(MapSpace space)
        {
            if (_buoys == null)
            {
                return;
            }

            float maxRadius = _view == MapView.World ? BuoyRadiusWorldDp : BuoyRadiusBasinDp;
            float spacing = float.MaxValue;
            for (int i = 1; i < BuoysPerLeg; i++)
            {
                spacing = Mathf.Min(spacing, Vector2.Distance(_buoyMap[i], _buoyMap[i - 1]) * space.DpPerUnit);
            }

            // Un tramo de longitud 0 o casi 0 (misma vista, otra luz): las 10 boyas, en la fila corta bajo la parada, con el radio
            // de la vista, para que se siga viendo cuántos niveles faltan. Si no, por la longitud real del tramo.
            _buoysCompact = _frontStop >= 0 && spacing < BuoyCompactBelowDp;
            float radius = _buoysCompact ? maxRadius : Mathf.Clamp(spacing * BuoySpacingFraction, BuoyRadiusMinDp, maxRadius);
            if (_buoysCompact)
            {
                spacing = BuoyCompactSpacing * maxRadius;
            }

            float rosaW = RosaWidthDp(_view, spacing);
            float aspect = ProceduralArt.RosaHeightDp / ProceduralArt.RosaWidthDp;
            for (int i = 0; i < BuoysPerLeg; i++)
            {
                _buoyLocal[i] = _buoysCompact ? CompactBuoy(space, _frontStopMap, i, maxRadius) : space.ToLocal(_buoyMap[i]);
                if (_rosa[i] != null && _rosa[i].gameObject.activeSelf)
                {
                    RectTransform rt = _rosa[i].rectTransform;
                    rt.anchoredPosition = _buoyLocal[i];
                    rt.sizeDelta = new Vector2(rosaW, rosaW * aspect);
                }
            }

            for (int i = 0; i < _pastRosaCount; i++)
            {
                bool compact = _pastRosaLegSpacing[i] * space.DpPerUnit < BuoyCompactBelowDp;
                float w = RosaWidthDp(_view, compact ? BuoyCompactSpacing * maxRadius : _pastRosaSpacing[i] * space.DpPerUnit);
                RectTransform rt = _pastRosa[i].rectTransform;
                int k = (_pastRosaLevel[i] - 1) % RiverStopCatalog.LevelsPerStop;
                rt.anchoredPosition = compact ? CompactBuoy(space, _pastRosaStop[i], k, maxRadius) : space.ToLocal(_pastRosaMap[i]);
                rt.sizeDelta = new Vector2(w, w * aspect);
            }

            _buoys.SetBuoys(_buoyLocal, _buoyState, _buoyCount, radius);
            if (_buoysAhead != null)
            {
                _buoysAhead.SetBuoys(_buoyLocal, _buoyState, _buoyCount, radius);
            }

            PlaceSource(space, radius); // el hito y el rótulo de la naciente (revisión de FX13)
        }

        // Boya i de la fila corta de un tramo sin largo a la escala de la vista: una fila horizontal de 10, centrada bajo la parada
        // y por debajo de la caja de la canoa que va en ella (BuoyCompactGapDp de hueco), con BuoyCompactSpacing radios entre
        // centros; si se saldría del mapa por un lado, se corre entera hacia dentro.
        private Vector2 CompactBuoy(MapSpace space, Vector2 stopMap, int i, float radius)
        {
            Vector2 stop = space.ToLocal(stopMap);
            float step = BuoyCompactSpacing * radius;
            float half = step * (BuoysPerLeg - 1) * 0.5f;
            float contentW = _map != null ? space.DpPerUnit * _map.Width : float.MaxValue;
            float x0 = Mathf.Clamp(stop.x - half, radius + LabelEdgeDp, Mathf.Max(radius + LabelEdgeDp, contentW - radius - LabelEdgeDp - (2f * half)));
            float below = ProceduralArt.MapCanoeHeightDp * CanoePivotY;
            return new Vector2(x0 + (i * step), stop.y - below - BuoyCompactGapDp - radius - _compactDropDp);
        }

        /// <summary>
        /// Lo que baja la fila corta de boyas (dp) para que no pise el punto real de una mini cuia (M3.5, revisión: el punto del
        /// abanico caía encima de una boya). Lo fija la colocación de las mini cuias; 0 en la Cuenca y sin racimos.
        /// </summary>
        internal float CompactRowDropDp => _compactDropDp;

        // Baja la fila corta de boyas dropDp y vuelve a colocar las boyas (solo si cambia).
        private void SetCompactRowDrop(MapSpace space, float dropDp)
        {
            if (Mathf.Abs(dropDp - _compactDropDp) < 0.01f)
            {
                return;
            }

            _compactDropDp = dropDp;
            PlaceBuoys(space);
        }

        // ============================================================================================ siluetas y rótulos

        // Una silueta por mundo futuro del catálogo con ruta hasta su gran parada (la que aún no se alcanzó: max < 10 · n), en esa
        // parada, con «Mundo {n} · {nombre} · Nivel {primer nivel}» (map_next_world). Su texto se asigna aquí: al abrir y, al
        // ponerse al día, cuando se alcanza una gran parada. True si cambió qué mundo lleva alguna caja o cuáles se ven: entonces
        // hay que volver a colocarlas (FX8).
        private bool BuildSilhouettes(RiverMap map, RiverRoutes routes, int max)
        {
            RiverStopCatalog? stops = _stops;
            int used = 0;
            bool changed = false;
            for (int world = 2; stops != null && world <= WorldTable.WorldCount; world++)
            {
                RiverStop? gate = stops.FirstStopOfWorld(world);
                RiverStopRoute? route = gate != null ? routes.At(gate.N) : null;
                if (gate == null || route == null || max >= gate.N * RiverStopCatalog.LevelsPerStop)
                {
                    continue;
                }

                Silhouette s = SilhouetteAt(used++);
                changed |= s.World != world || !s.Root.gameObject.activeSelf;
                s.World = world;
                s.MapPoint = PlacePoint(map, route.At);
                s.Label.text = UiText.MapNextWorld(world, gate.FirstLevel);
                Vector2 size = s.Label.GetPreferredValues(s.Label.text, float.PositiveInfinity, float.PositiveInfinity);
                float w = Mathf.Ceil(size.x) + 12f;
                float h = Mathf.Max(20f, Mathf.Ceil(size.y) + 4f);
                s.Root.sizeDelta = new Vector2(w, h);
                s.Root.gameObject.name = "Silhouette_w" + world.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                if (!s.Root.gameObject.activeSelf)
                {
                    s.Root.gameObject.SetActive(true);
                }
            }

            for (int i = used; i < _silhouettes.Count; i++)
            {
                if (_silhouettes[i].Root.gameObject.activeSelf)
                {
                    _silhouettes[i].Root.gameObject.SetActive(false);
                    changed = true;
                }
            }

            return changed;
        }

        private Silhouette SilhouetteAt(int i)
        {
            while (_silhouettes.Count <= i)
            {
                RectTransform root = UiBuilder.Rect("Silhouette_" + _silhouettes.Count, _labelLayer!);
                root.anchorMin = new Vector2(0f, 1f);
                root.anchorMax = new Vector2(0f, 1f);
                root.pivot = new Vector2(0f, 0.5f);
                Image plate = UiBuilder.Sliced("Plate", root, _art.RoundedRect, new Color(SilhouetteColor.r, SilhouetteColor.g, SilhouetteColor.b, SilhouettePlateAlpha));
                plate.pixelsPerUnitMultiplier = 3f; // esquinas de unos 7 dp en una placa de 20
                UiBuilder.Stretch(plate.rectTransform);
                TextMeshProUGUI label = UiBuilder.Label("Label", root, TextStyle.PlainBold, string.Empty, SilhouetteSp, Palette.Cream, TextAlignmentOptions.Center);
                UiBuilder.Stretch(label.rectTransform);
                _silhouettes.Add(new Silhouette(root, plate, label));
            }

            return _silhouettes[i];
        }

        private Silhouette ActiveSilhouette(int i)
        {
            int seen = 0;
            for (int k = 0; k < _silhouettes.Count; k++)
            {
                if (!_silhouettes[k].Root.gameObject.activeSelf)
                {
                    continue;
                }

                if (seen++ == i)
                {
                    return _silhouettes[k];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(i), i, "No hay tantas siluetas a la vista.");
        }

        // Textos de los rótulos al abrir (sin asignar: claves de Loc).
        private void ApplyProgressTexts()
        {
            IReadOnlyList<RiverMapLabel> labels = _map!.Labels;
            for (int i = 0; i < _countries.Count; i++)
            {
                string name = i < labels.Count ? Loc.Fact(labels[i].NameKey) : string.Empty;
                TextMeshProUGUI label = _countries[i];
                label.text = name;
                bool show = name.Length > 0; // en release, sin el visto bueno del asesor, no sale
                if (label.gameObject.activeSelf != show)
                {
                    label.gameObject.SetActive(show);
                }

                if (show)
                {
                    Vector2 size = label.GetPreferredValues(name, float.PositiveInfinity, float.PositiveInfinity);
                    label.rectTransform.sizeDelta = new Vector2(Mathf.Ceil(size.x) + 2f, Mathf.Ceil(size.y) + 2f);
                }
            }

            SourceTexts(); // map_source_name, el rótulo de la naciente (revisión de FX13)
            if (_andesLabel != null && _atlanticLabel != null)
            {
                _andesLabel.text = UiText.MapLabelAndes;
                _atlanticLabel.text = UiText.MapLabelAtlantic;
            }
        }

        // «Andes ↑» arriba, centrado, y «Océano Atlántico ↓» abajo a la izquierda, sin pisar «Volver a la canoa» (abajo a la
        // derecha): su caja llega como mucho hasta el botón y, si el texto no cabe a su tamaño, se encoge hasta 12 sp (02 §12).
        private void LayoutOrientationLabels()
        {
            if (_andesLabel == null || _atlanticLabel == null)
            {
                return;
            }

            float andesRoom = _viewW - (2f * OrientationInsetDp);
            Vector2 andes = FitOrientationLabel(_andesLabel, andesRoom);
            UiBuilder.Place(_andesLabel.rectTransform, FramePadDp + ((_viewW - andes.x) * 0.5f), FramePadDp + OrientationInsetDp, andes.x, andes.y);

            float buttonW = ((RectTransform)_backToCanoe.Button.transform).sizeDelta.x;
            float atlanticRoom = Mathf.Max(MinTouchDp, _viewW - (3f * OrientationInsetDp) - buttonW);
            Vector2 atlantic = FitOrientationLabel(_atlanticLabel, atlanticRoom);
            UiBuilder.Place(_atlanticLabel.rectTransform, FramePadDp + OrientationInsetDp, FramePadDp + _viewH - OrientationInsetDp - atlantic.y, atlantic.x, atlantic.y);
        }

        // Caja de un rótulo de orientación en «room» dp: la de su texto a OrientationLabelSp o, si no cabe, «room» con el texto
        // autoajustado hasta el mínimo de 12 sp. Mide con TextMeshPro al colocar, nunca por fotograma.
        private static Vector2 FitOrientationLabel(TextMeshProUGUI label, float room)
        {
            float scale = UiBuilder.TextScale;
            label.enableAutoSizing = false;
            label.fontSize = OrientationLabelSp * scale;
            Vector2 size = string.IsNullOrEmpty(label.text)
                ? Vector2.zero
                : label.GetPreferredValues(label.text, float.PositiveInfinity, float.PositiveInfinity);
            float w = Mathf.Ceil(size.x) + 8f;
            float h = Mathf.Ceil(size.y) + 2f;
            if (w > room)
            {
                UiBuilder.AutoSize(label, UiBuilder.MinTextSp, OrientationLabelSp);
                w = room;
            }

            return new Vector2(w, h);
        }

        // ============================================================================================ fundido de los afluentes

        // Las líneas de paso y las locales que se llenan enteras entre map_seen_level y max_level aparecen con un fundido de
        // PassbyFillFadeMs (propuesta del Mapa §3.4); las demás, ya llenas.
        private void StartFades(int seen)
        {
            _fading.Clear();
            for (int i = 0; i < _fill.Count; i++)
            {
                _fill[i].canvasRenderer.SetAlpha(1f);
                _fillCasing[i].canvasRenderer.SetAlpha(1f);
            }

            RiverMap map = _map!;
            if (seen >= _progressLevel || _routes == null)
            {
                return;
            }

            ComputeFill(map, _routes, seen, _seenFrom);
            IReadOnlyList<RiverLine> lines = map.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                RiverLineKind kind = lines[i].Kind;
                // Solo los que se llenan enteros por la boca: uno que remonta una ruta (el Nanay) ya se vio crecer con el frente.
                if ((kind == RiverLineKind.Passby || kind == RiverLineKind.Local) && _seenFrom[i] >= 1f && _fillFrom[i] < 1f
                    && !RouteClimbs(_routes, i))
                {
                    _fading.Add(i);
                    _fill[i].canvasRenderer.SetAlpha(0f);
                    _fillCasing[i].canvasRenderer.SetAlpha(0f);
                }
            }

            _fadeElapsed = 0f;
            _fadeMs = ProgressReduceMotion ? MapConfig.ReducedMotionFadeMs : MapConfig.PassbyFillFadeMs;
        }

        // ============================================================================================ piezas

        // Llena desde la boca (MouthFill) cada línea cuya boca pasa la ruta antes de la fracción «upTo», salvo las que remonta
        // alguna ruta del catálogo: esas se llenan por la ruta, para que la boca no se trague el frente de sus tramos.
        private static void FillMouths(RiverMap map, RiverRoutes routes, RiverStopRoute route, float upTo, float[] from)
        {
            IReadOnlyList<int> mouths = route.Mouths;
            for (int k = 0; k < mouths.Count; k++)
            {
                int line = mouths[k];
                if (upTo < 1f && MouthFraction(map, route, line) > upTo)
                {
                    continue;
                }

                float fill = MouthFill(map, map.Lines[line]);
                if (fill > 0f && !RouteClimbs(routes, line))
                {
                    from[line] = Mathf.Min(from[line], 1f - fill);
                }
            }
        }

        /// <summary>
        /// Lo que se llena de la línea <paramref name="line"/> al pasar su boca (fracción de su longitud desde la boca):
        /// <c>fill_on_pass</c> (entera la de paso y la local) o, si la boca de un mundo futuro asomaría menos de
        /// <see cref="MouthMinBasinDp"/> en la Cuenca fuera de su río anfitrión (con su medio ancho y su orla), lo justo para llegar a
        /// esa medida: que se lea como boca y no como una grieta.
        /// </summary>
        public static float MouthFill(RiverMap map, RiverLine line)
        {
            float fill = Mathf.Clamp01(line.FillOnPass);
            if (fill <= 0f || fill >= 1f || line.Length <= 0f)
            {
                return fill;
            }

            float host = line.Mouth != null ? RiverRibbonGraphic.WidthForClass(map.Lines[line.Mouth.Value.Line].Class) * 0.5f : 0f;
            float minDp = MouthMinBasinDp + host + RiverRibbonGraphic.CasingDp;
            return Mathf.Clamp01(Mathf.Max(fill, minDp / (line.Length * MapSpace.BasinWidthDp)));
        }

        /// <summary>
        /// Alguna ruta del catálogo remonta (o recorre) la línea <paramref name="line"/>: un tramo de su <c>leg.path</c> con largo.
        /// Esas líneas no se llenan por la boca: el agua llega por la ruta, desde la boca hasta la parada alcanzada, más el frente
        /// (D-013, punto 4: cada victoria llena una décima del tramo). Sin asignaciones.
        /// </summary>
        public static bool RouteClimbs(RiverRoutes routes, int line)
        {
            for (int n = 0; n < routes.LegCount; n++)
            {
                RiverStopRoute? route = routes.At(n);
                if (route == null)
                {
                    continue;
                }

                IReadOnlyList<RiverPathPiece> path = route.Path;
                for (int p = 0; p < path.Count; p++)
                {
                    if (path[p].Line == line && path[p].S0 != path[p].S1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Silueta de un mundo futuro: placa azul petróleo con su texto, a la derecha de su gran parada.
        private sealed class Silhouette
        {
            public Silhouette(RectTransform root, Image plate, TextMeshProUGUI label)
            {
                Root = root;
                Plate = plate;
                Label = label;
            }

            public RectTransform Root { get; }

            public Image Plate { get; }

            public TextMeshProUGUI Label { get; }

            public int World { get; set; }

            public Vector2 MapPoint { get; set; }

            public int FillHits { get; set; }

            public int FillMin { get; set; }
        }

        /// <summary>
        /// Las boyas del frente en una malla sin textura (como <see cref="GlyphGraphic"/>): un disco por boya con su canto,
        /// encendida (<see cref="BuoyLitColor"/>, canto de madera) o por ganar (<see cref="BuoyUnlitColor"/>, canto azul
        /// petróleo). Son dos mallas con los mismos datos: la de las encendidas, detrás de la canoa, y la de las por ganar
        /// (<see cref="Ahead"/>), encima. Las saltadas no se dibujan aquí (llevan a Rosa). Solo se reconstruye cuando cambian; sin
        /// raycast.
        /// </summary>
        [RequireComponent(typeof(CanvasRenderer))]
        internal sealed class BuoyGraphic : MaskableGraphic
        {
            /// <summary>Tramos de cada disco.</summary>
            public const int Segments = 12;

            private readonly Vector2[] _points = new Vector2[BuoysPerLeg];
            private readonly BuoyState[] _states = new BuoyState[BuoysPerLeg];
            private int _count;
            private float _radius;

            /// <summary>Boyas dibujadas por esta malla (sin las saltadas).</summary>
            public int DrawnCount { get; private set; }

            /// <summary>
            /// Esta malla dibuja las boyas por ganar (va encima de la canoa); si no, las encendidas (detrás de ella). Se fija al
            /// crearla.
            /// </summary>
            public bool Ahead { get; set; }

            /// <summary>Radio actual (dp).</summary>
            public float Radius => _radius;

            /// <summary>Copia las boyas (posiciones locales y estados) y vuelve a dibujar solo si algo cambió. Sin asignaciones.</summary>
            public void SetBuoys(Vector2[] points, BuoyState[] states, int count, float radius)
            {
                bool changed = count != _count || radius != _radius;
                for (int i = 0; i < count && !changed; i++)
                {
                    changed = points[i] != _points[i] || states[i] != _states[i];
                }

                if (!changed)
                {
                    return;
                }

                _count = count;
                _radius = radius;
                for (int i = 0; i < count; i++)
                {
                    _points[i] = points[i];
                    _states[i] = states[i];
                }

                SetVerticesDirty();
            }

            /// <inheritdoc />
            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                DrawnCount = 0;
                if (_radius <= 0f)
                {
                    return;
                }

                for (int i = 0; i < _count; i++)
                {
                    BuoyState state = _states[i];
                    if (state == BuoyState.Skipped || (state == BuoyState.Unlit) != Ahead)
                    {
                        continue;
                    }

                    bool lit = state == BuoyState.Lit;
                    Disc(vh, _points[i], _radius, lit ? Palette.Frame : Palette.Panel.Top);
                    Disc(vh, _points[i], _radius * 0.66f, lit ? BuoyLitColor : BuoyUnlitColor);
                    DrawnCount++;
                }
            }

            private void Disc(VertexHelper vh, Vector2 c, float r, Color tint)
            {
                Color32 c32 = tint * color;
                int start = vh.currentVertCount;
                vh.AddVert(c, c32, Vector2.zero);
                for (int k = 0; k < Segments; k++)
                {
                    float a = k * (Mathf.PI * 2f / Segments);
                    vh.AddVert(c + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r), c32, Vector2.zero);
                }

                for (int k = 0; k < Segments; k++)
                {
                    vh.AddTriangle(start, start + 1 + k, start + 1 + ((k + 1) % Segments));
                }
            }
        }
    }
}
