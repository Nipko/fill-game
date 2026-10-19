#nullable enable
using System;
using System.Collections.Generic;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>Qué enseña la tarjeta de la parada (<see cref="StopCard"/>).</summary>
    public enum StopCardMode
    {
        /// <summary>Una parada ya alcanzada: la tarjeta entera.</summary>
        Stop = 0,

        /// <summary>Una parada futura: solo la silueta del sitio y «Nivel n».</summary>
        Future = 1,

        /// <summary>Un mirador revelado: su nombre y su dato.</summary>
        Viewpoint = 2,
    }

    /// <summary>
    /// Tarjeta de la parada del Mapa del río, la «Escena del río» (D-013, punto 10: Q9-A y Q10-A; 06 §4.5; propuesta del Mapa §7,
    /// fila «Tarjeta de parada»; tarea M4.1), construida por código encima del Mapa y del Muestrario, sin escena ni prefab. Se abre al
    /// tocar una mini cuia del Mapa o una muestra del Muestrario:
    /// <list type="bullet">
    /// <item><description><b>Parada alcanzada</b> (<see cref="StopCardMode.Stop"/>): la mini cuia con el agua del sitio
    /// (<c>sample</c>) y la placa <c>map_arrived</c> («{lugar} · {país} · río {río}», con sus cortes <c>{sep}</c> donde la línea se
    /// parte); el fondo del tablero de la parada en pequeño (el panorama con <see cref="StopBackdrop"/>: cielo, banda de agua del
    /// mundo, piezas por código y héroe, a la escala de la tarjeta); el dato (el de llegada y, con «Otro dato»
    /// (<c>map_card_other_fact</c>), los demás de la parada, uno cada vez) con su fuente (<c>map_card_source</c>, «Fuente: …»,
    /// bajo el dato, si la fila <c>&lt;clave del dato&gt;.source</c> tiene texto; ver <see cref="SourceSuffix"/>; D-028, pregunta
    /// 57 a: la genera <c>tools/river-map/stops.py</c>); el miquito del tramo (su retrato y su nombre,
    /// <c>fauna.&lt;id&gt;.name</c>, solo con su arte de tarjeta) y sus frutas (icono y nombre, <c>tint.&lt;id&gt;.name</c>;
    /// <see cref="CardTints"/>): primero la de la serie de Frutas del río que corre en sus niveles (D-022; 02 §6, fila «Dato del
    /// río»; 06 §4.4: ahí queda a la vista la línea de fruta que el dato de llegada desplaza en el <c>10k</c>; calendario
    /// <c>frutas_del_rio_v1.json</c>) y después las que debutaron en ellos (06 §4.5; <see cref="TintDebuts"/>).</description></item>
    /// <item><description><b>Parada futura</b> (<see cref="StopCardMode.Future"/>): solo la silueta del sitio y «Nivel n»
    /// (<c>level_n</c>, su primer nivel, en la fila de la cabecera, a la derecha del ←): ni nombre, ni datos, ni la Canasta. El
    /// fondo entero va bajo un velo de madera oscura (<see cref="FutureVeilColor"/>) con el cielo y los montes desaturados
    /// (<see cref="FutureTone"/>), y el héroe en <see cref="SilhouetteColor"/> con un filo de luz de <see cref="RimLightDp"/> dp
    /// (<see cref="RimLightColor"/>) para que la silueta se lea (revisión de M4.1). Sin reflejo: el del PNG se oculta y la silueta
    /// provisional, que pinta el suyo en su propia malla, se recorta en la línea del agua (<see cref="ToneEffect.ClipBelow"/>), así
    /// que la silueta y su filo de luz son solo lo de encima del agua.</description></item>
    /// <item><description><b>Mirador</b> (<see cref="StopCardMode.Viewpoint"/>): su «?», su nombre (<c>map.stop.&lt;id&gt;.name</c>)
    /// y su dato, si tiene texto.</description></item>
    /// </list>
    /// <para>Todo texto sale de las tablas por <see cref="Loc"/> (<see cref="UiText"/> y <see cref="Loc.Fact(string)"/>): en
    /// release, un texto sin el visto bueno del asesor es «» y su fila no se enseña (D-018, Q10-A). <b>Sin precios, ofertas ni
    /// semillas</b> (Q10-A): ni la Canasta ni la skin salen aquí. Textos ≥ 12 sp, contraste ≥ 4,5:1
    /// (<see cref="ContrastPairs"/>) y controles ≥ 48 dp (07 §1.2 y §7.8). Se compone al abrirse (asigna sus cadenas y mide con
    /// TextMeshPro: nunca por fotograma).</para>
    /// </summary>
    public sealed class StopCard
    {
        /// <summary>Margen de la tarjeta con los bordes del lienzo (dp).</summary>
        public const float SideDp = 16f;

        /// <summary>Relleno de la placa de la tarjeta (dp).</summary>
        public const float PadDp = 16f;

        /// <summary>Lado mínimo de un control (07 §1.2 y §7.8).</summary>
        public const float MinTouchDp = 48f;

        /// <summary>Ancho de diseño del fondo de la parada: el del lienzo de referencia (dp).</summary>
        public const float SceneWorldWidthDp = 360f;

        /// <summary>Horizonte del panorama en el fondo de la tarjeta (dp de diseño): el de <see cref="ProceduralArt.PanoramaHorizonDp"/>.</summary>
        public const float SceneHorizonDp = ProceduralArt.PanoramaHorizonDp;

        /// <summary>Cielo que se ve sobre el horizonte (dp de diseño) [P]: el héroe sube hasta 48 dp.</summary>
        public const float SceneSkyDp = 70f;

        /// <summary>Agua que se ve bajo el horizonte (dp de diseño) [P]: el reflejo del héroe (44 dp) y algo de banda.</summary>
        public const float SceneWaterDp = 60f;

        /// <summary>Lo que la máscara de la ventana del fondo sale por fuera de él, bajo el marco (dp).</summary>
        public const float ClipPadDp = 1f;

        /// <summary>Alto mínimo del fondo en la tarjeta (dp) [P], si la tarjeta no cabe en la pantalla.</summary>
        public const float SceneMinDp = 64f;

        /// <summary>
        /// Alto mínimo del fondo (dp) [P] al recortarlo para que la placa no llegue a «Jugar» del Mapa (revisión de DC10): por
        /// debajo, la placa vuelve a centrarse en toda la pantalla (el velo tapa «Jugar») y el texto nunca baja de 12 sp.
        /// </summary>
        public const float SceneFitMinDp = 72f;

        /// <summary>Aire mínimo entre el borde de abajo de la placa y el de arriba de «Jugar» del Mapa (dp; revisión de DC10).</summary>
        public const float ClearOfBelowDp = 8f;

        /// <summary>Escala de la mini cuia de la tarjeta sobre la del Mapa (20 × 32 dp) [P].</summary>
        public const float SampleScale = 1.5f;

        /// <summary>Cuerpo del dato (sp).</summary>
        public const float FactSp = 15f;

        /// <summary>Cuerpo de la fuente (sp).</summary>
        public const float SourceSp = 12f;

        /// <summary>Cuerpo de «Nivel n» de una parada futura (sp).</summary>
        public const float FutureLevelSp = 22f;

        /// <summary>Cuerpo del nombre del miquito y de las frutas (sp).</summary>
        public const float RowSp = 14f;

        /// <summary>Retrato del miquito (dp).</summary>
        public const float PortraitDp = 40f;

        /// <summary>Icono de una fruta (dp).</summary>
        public const float FruitIconDp = 28f;

        /// <summary>Hueco entre bloques (dp).</summary>
        public const float GapDp = 10f;

        /// <summary>
        /// Final de la clave de la fuente de un dato: la fila <c>&lt;clave del dato&gt;.source</c> de <c>facts.csv</c> con el
        /// nombre corto de la fuente tal cual («Wikipedia», «Acta Amazonica»; <c>map_card_source</c>, 06 §4.5 y propuesta del
        /// Mapa §7: «el dato con su fuente»). Contrato fijado por el dueño en D-028 (pregunta 57 a): la fila la genera
        /// <c>tools/river-map/stops.py --write-texts</c> (el bloque <c>map.*</c> de <c>facts.csv</c> es solo suyo) con el nombre
        /// corto de <c>tools/river-map/overrides.json</c> (<c>source_short</c>) de la primera fuente [V] del dato o, sin ninguna
        /// [V], de la primera. Un dato sin esa fila no enseña la línea. Lo prueba <c>StopCardTests</c> con los datos reales
        /// (Manaos y Amacayacu, la fuente más larga) y con un catálogo de prueba.
        /// </summary>
        public const string SourceSuffix = ".source";

        /// <summary>Color de la fuente: el canto claro de la repisa sobre la madera oscura (contraste ≥ 4,5:1).</summary>
        public static readonly Color SourceColor = Palette.ShelfEdge;

        /// <summary>Velo detrás de la tarjeta.</summary>
        public static readonly Color ScrimColor = new Color(Palette.Scrim.r, Palette.Scrim.g, Palette.Scrim.b, 0.78f);

        /// <summary>
        /// Tinte del héroe de una parada futura: una silueta de madera oscura [P], opaca. Va encima de las copias del filo de luz,
        /// que quedan debajo de toda la forma: con algo de transparencia (era 0,92), el crema de debajo se veía a través (el lienzo
        /// mezcla en lineal) y las paredes salían claras con las ventanas oscuras, no una sola tinta.
        /// </summary>
        public static readonly Color SilhouetteColor = new Color(0.16f, 0.12f, 0.10f, 1f);

        /// <summary>Filo de luz de la silueta de una parada futura (revisión de M4.1): crema al 35 %.</summary>
        public static readonly Color RimLightColor = new Color(Palette.Cream.r, Palette.Cream.g, Palette.Cream.b, 0.35f);

        /// <summary>Velo sobre el fondo entero de una parada futura (revisión de M4.1): madera oscura al 55 %.</summary>
        public static readonly Color FutureVeilColor = new Color(Palette.WoodDark.r, Palette.WoodDark.g, Palette.WoodDark.b, FutureVeilAlpha);

        /// <summary>Ancho del filo de luz de la silueta de una parada futura (dp en pantalla).</summary>
        public const float RimLightDp = 1f;

        /// <summary>Saturación del cielo y los montes de una parada futura (revisión de M4.1).</summary>
        public const float FutureSaturation = 0.2f;

        /// <summary>Factor que oscurece el cielo y los montes de una parada futura (revisión de M4.1).</summary>
        public const float FutureMultiply = 0.45f;

        /// <summary>Opacidad del velo de madera oscura de una parada futura (revisión de M4.1).</summary>
        public const float FutureVeilAlpha = 0.55f;

        /// <summary>
        /// Debut de cada tinte (glosario §9, columna «Debut»; 07 §3; 06 §4.5): pares (nivel, índice de motor) en orden de nivel. El
        /// 1 estrena dos (aguaje y açaí). Sus niveles son los de <c>SightingRule.FruitDebutLevels</c> (un test lo comprueba).
        /// </summary>
        public static readonly int[,] TintDebuts =
        {
            { 1, 4 }, { 1, 8 }, { 2, 3 }, { 4, 1 }, { 7, 7 }, { 11, 10 }, { 21, 6 }, { 41, 5 }, { 61, 14 }, { 81, 2 }, { 111, 11 },
            { 141, 9 }, { 201, 13 }, { 301, 12 },
        };

        private readonly ProceduralArt _art;
        private readonly Action _onClosed;
        private readonly RectTransform _root;
        private readonly Image _scrim;
        private readonly Image _panel;
        private readonly ReliefParts _close;
        private readonly SampleCuia _sample;
        private readonly Image _badge;
        private readonly TextMeshProUGUI _title;
        private readonly Image _sceneFrame;
        private readonly RectTransform _sceneWindow;
        private readonly RectTransform _sceneWorld;
        private readonly Image _sceneSky;
        private readonly Image _panorama;
        private readonly Image _sceneVeil;
        private readonly StopBackdrop _backdrop;
        private readonly List<ToneEffect> _mutes = new List<ToneEffect>();
        private readonly ToneEffect _heroTone;
        private readonly ToneEffect _shapeTone;
        private readonly TextMeshProUGUI _futureLevel;
        private readonly TextMeshProUGUI _fact;
        private readonly TextMeshProUGUI _source;
        private readonly ReliefParts _otherFact;
        private readonly TextMeshProUGUI _otherFactLabel;
        private readonly Image _portraitBack;
        private readonly Image _portrait;
        private readonly TextMeshProUGUI _faunaName;
        private readonly List<Image> _fruitIcons = new List<Image>();
        private readonly List<TextMeshProUGUI> _fruitNames = new List<TextMeshProUGUI>();
        private readonly List<string> _factKeys = new List<string>();
        private readonly List<int> _tints = new List<int>();

        private StopCardMode _mode;
        private RiverStop? _stop;
        private RiverViewpoint? _viewpoint;
        private FaunaSpecies? _species;
        private int _factIndex;
        private int _fruitCount;
        private float _canvasW = 360f;
        private float _canvasH = 780f;
        private float _safeTop;
        private float _safeBottom;
        private float _clearBottom;

        /// <summary>
        /// Construye la tarjeta bajo <paramref name="parent"/> (estirada a él), oculta. <paramref name="onClosed"/> se llama al
        /// cerrarla con su ←, con el velo o con <see cref="Hide"/>.
        /// </summary>
        public StopCard(RectTransform parent, ProceduralArt art, Action onClosed)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            _onClosed = onClosed ?? throw new ArgumentNullException(nameof(onClosed));
            _root = UiBuilder.Rect("StopCard", parent);

            _scrim = UiBuilder.Image("Scrim", _root, null, ScrimColor);
            UiBuilder.Stretch(_scrim.rectTransform);
            Button scrimButton = UiBuilder.BareButton("ScrimButton", _root, Hide);
            UiBuilder.Stretch((RectTransform)scrimButton.transform);

            _panel = UiBuilder.Sliced("Panel", _root, art.WoodPlate, Color.white);
            _panel.raycastTarget = true; // la placa se queda con sus toques: no cierran la tarjeta

            _close = UiBuilder.Relief("CloseButton", _panel.transform, art.DiscWood, Hide);
            RectTransform closeVisual = (RectTransform)_close.Face.parent;
            closeVisual.anchorMin = new Vector2(0.5f, 1f);
            closeVisual.anchorMax = new Vector2(0.5f, 1f);
            closeVisual.sizeDelta = new Vector2(art.DiscWood.Painted ? art.DiscWood.HeightDp : 40f, art.DiscWood.HeightDp);
            closeVisual.anchoredPosition = new Vector2(0f, -Mathf.Max(0f, (MinTouchDp - art.DiscWood.HeightDp) * 0.5f));
            RiverMapScreen.GlyphGraphic arrow = RiverMapScreen.GlyphGraphic.Create("Arrow", _close.Face, RiverMapScreen.GlyphKind.BackArrow);
            UiBuilder.Place(arrow.rectTransform, 9f, 9f, 22f, 22f);
            _close.Relief.Bind(_close.Face, _close.Base, _close.FaceImage, art.DiscWood, null, null, null);

            _sample = new SampleCuia("Sample", _panel.transform, art, SampleScale);
            _badge = UiBuilder.Image("ViewpointBadge", _panel.transform, art.MapViewpoint, Color.white);
            _badge.preserveAspect = true;

            _title = UiBuilder.Label("Title", _panel.transform, TextStyle.ModalTitle, string.Empty, UiText.StopNameSizeSp, Palette.Cream, TextAlignmentOptions.Left);
            _title.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_title, 14f, UiText.StopNameSizeSp);

            // El fondo de la parada en pequeño: una ventana con máscara y, dentro, el panorama y el fondo de la parada a 360 dp de
            // diseño, escalados al ancho de la tarjeta.
            _sceneFrame = UiBuilder.Image("SceneFrame", _panel.transform, null, Palette.Frame);
            // La ventana recorta con un RectMask2D (las ondas de la banda pasan del ancho). Las mallas del fondo de la parada tienen
            // su caja en la esquina del mundo (0 × 0 dp) y el RectMask2D descarta lo que no cae estrictamente dentro: la máscara
            // sale ClipPadDp por fuera del mundo (bajo el marco de madera), así esa esquina queda dentro.
            _sceneWindow = UiBuilder.Rect("SceneWindow", _panel.transform);
            _sceneWindow.gameObject.AddComponent<RectMask2D>();
            _sceneWorld = UiBuilder.Rect("SceneWorld", _sceneWindow);
            RectTransform panoramaClip = UiBuilder.Rect("PanoramaClip", _sceneWorld);
            panoramaClip.gameObject.AddComponent<RectMask2D>();
            Image baseFill = UiBuilder.Image("BaseFill", panoramaClip, null, Palette.RiverFar);
            UiBuilder.Stretch(baseFill.rectTransform);
            _sceneSky = UiBuilder.Image("SkyFill", panoramaClip, null, Palette.SkyTop);
            UiBuilder.Stretch(_sceneSky.rectTransform);
            _panorama = UiBuilder.Image("Panorama", panoramaClip, art.Panorama, Color.white);
            UiBuilder.Place(_panorama.rectTransform, 0f, SceneSkyDp - SceneHorizonDp, SceneWorldWidthDp, SceneWorldWidthDp * (ProceduralArt.PanoramaHeightDp / 360f));
            _sceneVeil = UiBuilder.Image("FutureVeil", panoramaClip, null, FutureVeilColor); // sobre el panorama, bajo el fondo
            UiBuilder.Stretch(_sceneVeil.rectTransform);
            _sceneVeil.gameObject.SetActive(false);
            _backdrop = new StopBackdrop(_sceneWorld, art);

            // El tono de una parada futura (revisión de M4.1), apagado hasta que se pide: cielo y montes desaturados bajo el velo;
            // el cielo, la banda y las piezas del fondo de la parada con el mismo velo hecho en sus vértices (van encima de la
            // capa del velo); y el héroe en silueta con su filo de luz, por encima de todo.
            _mutes.Add(AddTone(baseFill).Mute(FutureSaturation, FutureMultiply, 0f));
            _mutes.Add(AddTone(_sceneSky).Mute(FutureSaturation, FutureMultiply, 0f));
            _mutes.Add(AddTone(_panorama).Mute(FutureSaturation, FutureMultiply, 0f));
            _mutes.Add(AddTone(_backdrop.SkyGraphic).Mute(FutureSaturation, FutureMultiply, FutureVeilAlpha));
            _mutes.Add(AddTone(_backdrop.BandGraphic).Mute(1f, 1f, FutureVeilAlpha));
            _mutes.Add(AddTone(_backdrop.CodeGraphic).Mute(1f, 1f, FutureVeilAlpha));
            _heroTone = AddTone(_backdrop.HeroImage).Silhouette(RimLightDp);
            _shapeTone = AddTone(_backdrop.ShapeGraphic).Silhouette(RimLightDp);

            _futureLevel = UiBuilder.Label("FutureLevel", _panel.transform, TextStyle.ModalTitle, string.Empty, FutureLevelSp, Palette.Cream, TextAlignmentOptions.Left);
            UiBuilder.AutoSize(_futureLevel, UiBuilder.MinTextSp, FutureLevelSp);

            _fact = UiBuilder.Label("Fact", _panel.transform, TextStyle.Body, string.Empty, FactSp, Palette.Cream, TextAlignmentOptions.TopLeft);
            _fact.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_fact, UiBuilder.MinTextSp, FactSp);
            _source = UiBuilder.Label("Source", _panel.transform, TextStyle.Body, string.Empty, SourceSp, SourceColor, TextAlignmentOptions.TopLeft);
            _source.textWrappingMode = TextWrappingModes.Normal;

            _otherFact = UiBuilder.Relief("OtherFactButton", _panel.transform, art.CtaWood, OtherFact);
            _otherFactLabel = UiBuilder.Label("Label", _otherFact.Face, TextStyle.PillWood, UiText.MapCardOtherFact, RowSp, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(_otherFactLabel.rectTransform, art.CtaWood);
            UiBuilder.AutoSize(_otherFactLabel, UiBuilder.MinTextSp, RowSp);
            _otherFact.Relief.Bind(_otherFact.Face, _otherFact.Base, _otherFact.FaceImage, art.CtaWood, null, null, _otherFactLabel);

            _portraitBack = UiBuilder.Image("PortraitBack", _panel.transform, art.Disc, Palette.PanelLightLow);
            _portrait = UiBuilder.Image("Portrait", _panel.transform, null, Color.white);
            _portrait.preserveAspect = true;
            _faunaName = UiBuilder.Label("FaunaName", _panel.transform, TextStyle.PlainBold, string.Empty, RowSp, Palette.Cream, TextAlignmentOptions.Left);
            UiBuilder.AutoSize(_faunaName, UiBuilder.MinTextSp, RowSp);

            _root.gameObject.SetActive(false);
        }

        /// <summary>La tarjeta está a la vista.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Qué enseña.</summary>
        public StopCardMode Mode => _mode;

        /// <summary>Parada que enseña (null con un mirador).</summary>
        public RiverStop? Stop => _stop;

        /// <summary>Mirador que enseña (null con una parada).</summary>
        public RiverViewpoint? Viewpoint => _viewpoint;

        /// <summary>Raíz (pantalla completa).</summary>
        public RectTransform Root => _root;

        /// <summary>Placa de la tarjeta.</summary>
        public RectTransform PanelRect => _panel.rectTransform;

        /// <summary>← de la tarjeta.</summary>
        public Button CloseButton => _close.Button;

        /// <summary>«Otro dato».</summary>
        public Button OtherFactButton => _otherFact.Button;

        /// <summary>«Otro dato» se ve (la parada tiene más de un dato con texto).</summary>
        public bool OtherFactShown => _otherFact.Button.gameObject.activeSelf;

        /// <summary>Placa <c>map_arrived</c> (o el nombre del mirador).</summary>
        public string TitleText => _title.text;

        /// <summary>Texto del dato que se ve (vacío sin dato).</summary>
        public string FactText => _fact.gameObject.activeSelf ? _fact.text : string.Empty;

        /// <summary>Clave del dato que se ve, o null.</summary>
        public string? FactKey => _factIndex < _factKeys.Count ? _factKeys[_factIndex] : null;

        /// <summary>Claves de los datos de la tarjeta con texto, en orden (el de llegada primero).</summary>
        public IReadOnlyList<string> FactKeys => _factKeys;

        /// <summary>«Fuente: …» (vacío si el dato no tiene fuente con texto).</summary>
        public string SourceText => _source.gameObject.activeSelf ? _source.text : string.Empty;

        /// <summary>«Nivel n» de una parada futura (vacío en las demás).</summary>
        public string FutureLevelText => _futureLevel.gameObject.activeSelf ? _futureLevel.text : string.Empty;

        /// <summary>«Nivel n» de una parada futura (tests: en la fila de la cabecera).</summary>
        public RectTransform FutureLevelRect => _futureLevel.rectTransform;

        /// <summary>«Fuente: …» (tests: dentro de la placa, ≥ 12 sp).</summary>
        internal TextMeshProUGUI SourceLabel => _source;

        /// <summary>El dato (tests: la fuente va debajo).</summary>
        internal TextMeshProUGUI FactLabel => _fact;

        /// <summary>El fondo va con el tono de una parada futura: el velo, el cielo y los montes apagados y el héroe en silueta.</summary>
        public bool FutureToneOn => _sceneVeil.gameObject.activeSelf;

        /// <summary>Velo de madera oscura sobre el fondo de una parada futura.</summary>
        public Image FutureVeil => _sceneVeil;

        /// <summary>Tono del héroe en PNG (la silueta con su filo de luz en una parada futura).</summary>
        public ToneEffect HeroTone => _heroTone;

        /// <summary>Tono de la silueta provisional del héroe.</summary>
        public ToneEffect ShapeTone => _shapeTone;

        /// <summary>Tonos que apagan el fondo de una parada futura (cielo, montes, banda y piezas).</summary>
        public IReadOnlyList<ToneEffect> MuteTones => _mutes;

        /// <summary>Se ve el fondo de la parada en pequeño.</summary>
        public bool SceneShown => _sceneWindow.gameObject.activeSelf;

        /// <summary>El fondo de la parada de la tarjeta.</summary>
        public StopBackdrop Backdrop => _backdrop;

        /// <summary>Color del panorama de la escena (la luz de la parada y de la temporada; tests de FX8).</summary>
        internal Color PanoramaTint => _panorama.color;

        /// <summary>Ventana del fondo (con su máscara).</summary>
        public RectTransform SceneRect => _sceneWindow;

        /// <summary>La mini cuia de la tarjeta.</summary>
        internal SampleCuia Sample => _sample;

        /// <summary>Se ve la mini cuia con el agua del sitio.</summary>
        public bool SampleShown => _sample.Root.gameObject.activeSelf;

        /// <summary>Se ve el «?» del mirador.</summary>
        public bool ViewpointBadgeShown => _badge.gameObject.activeSelf;

        /// <summary>Especie del tramo que enseña (null si no hay fila del miquito).</summary>
        public FaunaSpecies? Species => _species;

        /// <summary>Se ve la fila del miquito.</summary>
        public bool FaunaShown => _faunaName.gameObject.activeSelf;

        /// <summary>Nombre del miquito (vacío sin fila).</summary>
        public string FaunaName => FaunaShown ? _faunaName.text : string.Empty;

        /// <summary>Frutas que se ven (la de la serie de la parada y las que debutaron en ella; <see cref="CardTints"/>).</summary>
        public int FruitCount => _fruitCount;

        /// <summary>Tinte (índice de motor) de la fruta <paramref name="i"/>.</summary>
        public int FruitTint(int i)
        {
            return _tints[i];
        }

        /// <summary>Nombre de la fruta <paramref name="i"/>.</summary>
        public string FruitName(int i)
        {
            return _fruitNames[i].text;
        }

        /// <summary>Todos los textos que se ven (tests: ≥ 12 sp, sin recortes y sin precios).</summary>
        internal IEnumerable<TextMeshProUGUI> Texts()
        {
            foreach (TextMeshProUGUI t in new[] { _title, _futureLevel, _fact, _source, _otherFactLabel, _faunaName })
            {
                if (t.gameObject.activeInHierarchy && t.text.Length > 0)
                {
                    yield return t;
                }
            }

            for (int i = 0; i < _fruitCount; i++)
            {
                if (_fruitNames[i].text.Length > 0)
                {
                    yield return _fruitNames[i];
                }
            }
        }

        /// <summary>Controles a la vista (tests: ≥ 48 dp).</summary>
        internal IEnumerable<Button> Buttons()
        {
            yield return _close.Button;
            if (OtherFactShown)
            {
                yield return _otherFact.Button;
            }
        }

        /// <summary>
        /// Pares texto / fondo de la tarjeta para el contraste ≥ 4,5:1 (07 §1.2 y §7.8): los textos crema y la fuente sobre la
        /// madera oscura de la placa, y «Otro dato» sobre la cara de madera.
        /// </summary>
        public static (string Name, Color Text, Color Background)[] ContrastPairs()
        {
            return new[]
            {
                ("lugar, dato, miquito y frutas", (Color)Palette.Cream, (Color)Palette.WoodDark),
                ("fuente", SourceColor, (Color)Palette.WoodDark),
                ("nivel de la parada futura", (Color)Palette.Cream, (Color)Palette.WoodDark),
                ("otro dato", (Color)Palette.Cream, Palette.FamilyWood.Base),
                ("otro dato (fondo de la cara)", (Color)Palette.Cream, Palette.FamilyWood.Deep),
            };
        }

        // ============================================================================================ reglas puras

        /// <summary>
        /// Tintes (índices de motor) que debutan en los niveles de <paramref name="stop"/> (<c>10k+1</c> … <c>10k+10</c>), en orden,
        /// en <paramref name="into"/> (que vacía antes). Sin asignaciones si cabe.
        /// </summary>
        public static int DebutTints(RiverStop stop, List<int> into)
        {
            if (stop == null || into == null)
            {
                throw new ArgumentNullException(stop == null ? nameof(stop) : nameof(into));
            }

            into.Clear();
            for (int i = 0; i < TintDebuts.GetLength(0); i++)
            {
                int level = TintDebuts[i, 0];
                if (level >= stop.FirstLevel && level <= stop.LastLevel)
                {
                    into.Add(TintDebuts[i, 1]);
                }
            }

            return into.Count;
        }

        /// <summary>
        /// Tintes (índices de motor) de las series de Frutas del río de <paramref name="fruits"/> que tienen alguna línea en los
        /// niveles de <paramref name="stop"/> (<c>10k+1</c> … <c>10k+10</c>), en orden de nivel, en <paramref name="into"/> (que
        /// vacía antes): la fruta de la serie que la tarjeta enseña (D-022; 02 §6; 06 §4.4). Solo las series de tinte: las de río
        /// [P] (<c>rio_napo</c>, <c>rio_amazonas</c>) no son una fruta. Sin calendario, ninguna.
        /// </summary>
        public static int SeriesTints(RiverStop stop, FactCatalog? fruits, List<int> into)
        {
            if (stop == null || into == null)
            {
                throw new ArgumentNullException(stop == null ? nameof(stop) : nameof(into));
            }

            into.Clear();
            if (fruits == null)
            {
                return 0;
            }

            IReadOnlyList<FactSeries> series = fruits.Series;
            for (int i = 0; i < series.Count; i++)
            {
                FactSeries s = series[i];
                if (s.Kind != FactSeriesKind.Tint || !HasLineIn(s, stop.FirstLevel, stop.LastLevel))
                {
                    continue;
                }

                int tint = TintOf(s.Subject);
                if (tint > 0 && !into.Contains(tint))
                {
                    into.Add(tint);
                }
            }

            return into.Count;
        }

        /// <summary>
        /// Frutas de la tarjeta de <paramref name="stop"/> en <paramref name="into"/> (que vacía antes): la de la serie que corre en
        /// sus niveles (<see cref="SeriesTints"/>; D-022, 02 §6 y 06 §4.4) y, después, las que debutaron en ellos y no estén ya
        /// (<see cref="DebutTints"/>; 06 §4.5). Las dos lecturas del canon, sin repetir ninguna fruta.
        /// </summary>
        public static int CardTints(RiverStop stop, FactCatalog? fruits, List<int> into)
        {
            SeriesTints(stop, fruits, into);
            for (int i = 0; i < TintDebuts.GetLength(0); i++)
            {
                int level = TintDebuts[i, 0];
                int tint = TintDebuts[i, 1];
                if (level >= stop.FirstLevel && level <= stop.LastLevel && !into.Contains(tint))
                {
                    into.Add(tint);
                }
            }

            return into.Count;
        }

        /// <summary>Índice de motor del tinte de id <paramref name="id"/> (glosario §9), o 0 si no es un tinte.</summary>
        public static int TintOf(string id)
        {
            string[] ids = ProceduralArt.TintIds;
            for (int i = 1; i < ids.Length; i++)
            {
                if (string.Equals(ids[i], id, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return 0;
        }

        // La serie s tiene alguna línea en los niveles first … last.
        private static bool HasLineIn(FactSeries s, int first, int last)
        {
            int from = Math.Max(first, s.FromLevel);
            int to = Math.Min(last, s.LastLevel);
            if (from > to)
            {
                return false;
            }

            int rest = (from - s.FromLevel) % s.Step;
            int level = rest == 0 ? from : from + (s.Step - rest);
            return level <= to;
        }

        /// <summary>Clave del nombre visible del tinte <paramref name="tint"/> (<c>tint.&lt;id&gt;.name</c>), o null fuera de 1-14.</summary>
        public static string? TintNameKey(int tint)
        {
            string[] ids = ProceduralArt.TintIds;
            return tint >= 1 && tint < ids.Length ? FactCatalog.NamePrefix + ids[tint] + ".name" : null;
        }

        /// <summary>
        /// Claves de los datos de <paramref name="stop"/> que tienen texto que enseñar (<see cref="Loc.Has"/>), en orden: el de llegada
        /// (<c>arrival_fact</c>) y los «Otro dato» (<c>facts</c>). Las escribe en <paramref name="into"/> (que vacía antes).
        /// </summary>
        public static int FactKeysOf(RiverStop stop, List<string> into)
        {
            if (stop == null || into == null)
            {
                throw new ArgumentNullException(stop == null ? nameof(stop) : nameof(into));
            }

            into.Clear();
            if (stop.ArrivalFact != null && Loc.Fact(stop.ArrivalFact).Length > 0)
            {
                into.Add(stop.ArrivalFact);
            }

            for (int i = 0; i < stop.Facts.Count; i++)
            {
                string key = stop.Facts[i];
                if (Loc.Fact(key).Length > 0 && !into.Contains(key))
                {
                    into.Add(key);
                }
            }

            return into.Count;
        }

        /// <summary>Nombre corto de la fuente del dato <paramref name="factKey"/> (fila <see cref="SourceSuffix"/>; D-028), o «».</summary>
        public static string SourceOf(string factKey)
        {
            return string.IsNullOrEmpty(factKey) ? string.Empty : Loc.Fact(factKey + SourceSuffix);
        }

        /// <summary>
        /// El miquito del tramo de <paramref name="stop"/>: la especie del primer nivel de la parada con fauna que vive en su mundo
        /// (como la intro, <c>IntroCast</c>), o null.
        /// </summary>
        public static FaunaSpecies? SpeciesOf(FaunaCatalog? fauna, RiverStop stop)
        {
            if (fauna == null || stop == null)
            {
                return null;
            }

            for (int level = stop.FirstLevel; level <= stop.LastLevel; level++)
            {
                if (fauna.IsNoFauna(level))
                {
                    continue;
                }

                FaunaSpecies? species = fauna.ForLevel(level);
                if (species != null && species.LivesIn(stop.World))
                {
                    return species;
                }
            }

            return null;
        }

        // ============================================================================================ abrir y cerrar

        /// <summary>
        /// Abre la tarjeta de <paramref name="stop"/>: entera si se alcanzó (<paramref name="reached"/>), o solo la silueta y «Nivel
        /// n» si es futura. <paramref name="worldWater"/> es el <c>water_base</c> del mundo de la parada (la banda del primer plano);
        /// <paramref name="fauna"/>, el catálogo de miquitos (null = sin fila del miquito); <paramref name="fruits"/>, el calendario de
        /// Frutas del río (null = solo las frutas que debutaron en la parada); <paramref name="season"/>, la temporada del ciclo del
        /// primer nivel de la parada (07 §2.1: la misma composición que el fondo, temporada incluida; tarea FX8), cuya LUT va sobre
        /// la de la parada (<see cref="StopBackdrop.LutOf(RiverStop, Season)"/>); <see cref="Season.None"/> = sin temporada.
        /// </summary>
        public void ShowStop(RiverStop stop, bool reached, string worldWater, FaunaCatalog? fauna, FactCatalog? fruits = null,
            Season season = Season.None)
        {
            _stop = stop ?? throw new ArgumentNullException(nameof(stop));
            _viewpoint = null;
            _mode = reached ? StopCardMode.Stop : StopCardMode.Future;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();

            _backdrop.Apply(stop, worldWater ?? string.Empty, false, 0f, season);
            Color lut = _backdrop.LutColor;
            _panorama.color = lut;
            Color sky = Palette.SkyTop * lut;
            sky.a = 1f;
            _sceneSky.color = sky;

            if (reached)
            {
                _sample.Set(stop.Sample, true, false);
                _title.text = ArrivedText(stop);
                FactKeysOf(stop, _factKeys);
                _species = SpeciesOf(fauna, stop);
                if (_species != null && (!GameScreen.FaunaArtAvailable(_species, true) || UiText.FaunaName(_species.NameKey).Length == 0))
                {
                    _species = null;
                }

                CardTints(stop, fruits, _tints);
            }
            else
            {
                _title.text = string.Empty;
                _factKeys.Clear();
                _species = null;
                _tints.Clear();
            }

            _futureLevel.text = reached ? string.Empty : UiText.Level(stop.FirstLevel);
            _factIndex = 0;
            ApplyContent();
            ApplyFutureTone(!reached); // tras maquetar: el fondo ya compuso su héroe con la luz de la parada
        }

        /// <summary>Abre la tarjeta del mirador <paramref name="viewpoint"/> (ya revelado): su «?», su nombre y su dato.</summary>
        public void ShowViewpoint(RiverViewpoint viewpoint)
        {
            _viewpoint = viewpoint ?? throw new ArgumentNullException(nameof(viewpoint));
            _stop = null;
            _mode = StopCardMode.Viewpoint;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _title.text = Loc.Fact(viewpoint.NameKey);
            _factKeys.Clear();
            if (viewpoint.Fact != null && Loc.Fact(viewpoint.Fact).Length > 0)
            {
                _factKeys.Add(viewpoint.Fact);
            }

            _species = null;
            _tints.Clear();
            _futureLevel.text = string.Empty;
            _factIndex = 0;
            ApplyContent();
            ApplyFutureTone(false); // si antes se abrió una parada futura, su tono no se queda puesto
        }

        /// <summary>Cierra la tarjeta (su ←, el velo o el Atrás de Android). Sin efecto si no se ve.</summary>
        public void Hide()
        {
            if (!Visible)
            {
                return;
            }

            _root.gameObject.SetActive(false);
            _onClosed();
        }

        /// <summary>«Otro dato»: el dato siguiente de la parada (en ciclo). Sin efecto con un solo dato.</summary>
        public void OtherFact()
        {
            if (!Visible || _factKeys.Count < 2)
            {
                return;
            }

            _factIndex = (_factIndex + 1) % _factKeys.Count;
            ApplyFact();
            Layout(_canvasW, _canvasH, _safeTop, _safeBottom, _clearBottom);
        }

        // ============================================================================================ composición

        // La placa del lugar con sus cortes: el primer reparto de {sep} con el que la línea no se parte sola (sin cortes; tras el
        // país; tras el lugar; los dos); sin país o sin río (en release, sin el visto bueno del asesor), solo el lugar.
        private string ArrivedText(RiverStop stop)
        {
            string place = UiText.StopName(stop);
            string country = UiText.StopCountry(stop);
            string river = UiText.StopRiver(stop);
            if (place.Length == 0 || country.Length == 0 || river.Length == 0)
            {
                return place;
            }

            int seps = UiText.MapArrivedSeparators();
            float width = TitleWidth(_canvasW);
            float size = UiText.StopNameSizeSp * UiBuilder.TextScale;
            int[] tries = { 0, 2, 1, 3 };
            for (int i = 0; i < tries.Length; i++)
            {
                int breaks = tries[i];
                if (breaks >= (1 << seps))
                {
                    continue;
                }

                string text = UiText.MapArrived(place, country, river, breaks);
                int lines = 1 + ((breaks & 1) != 0 ? 1 : 0) + ((breaks & 2) != 0 ? 1 : 0);
                int measured = UiText.LineCount(text, TextStyle.ModalTitle, size, width);
                if (measured >= 0 && measured <= lines)
                {
                    return text;
                }
            }

            return UiText.MapArrived(place, country, river, (1 << seps) - 1);
        }

        private void ApplyContent()
        {
            bool stop = _mode == StopCardMode.Stop;
            bool future = _mode == StopCardMode.Future;
            bool viewpoint = _mode == StopCardMode.Viewpoint;
            _sample.Root.gameObject.SetActive(stop);
            _badge.gameObject.SetActive(viewpoint);
            _title.gameObject.SetActive(!future && _title.text.Length > 0);
            _sceneFrame.gameObject.SetActive(!viewpoint);
            _sceneWindow.gameObject.SetActive(!viewpoint);
            _futureLevel.gameObject.SetActive(future);
            ApplyFact();

            bool fauna = _species != null;
            _portraitBack.gameObject.SetActive(fauna);
            _portrait.gameObject.SetActive(fauna);
            _faunaName.gameObject.SetActive(fauna);
            if (_species != null)
            {
                _portrait.sprite = _art.MonkeyArtFor(_species).Portrait;
                _faunaName.text = UiText.FaunaName(_species.NameKey);
            }

            _fruitCount = 0;
            for (int i = 0; i < _tints.Count; i++)
            {
                string? key = TintNameKey(_tints[i]);
                string name = key != null ? Loc.Fact(key) : string.Empty;
                if (name.Length == 0)
                {
                    continue; // en release, sin el visto bueno del asesor: sin esa fruta
                }

                EnsureFruit(_fruitCount);
                _fruitIcons[_fruitCount].sprite = _art.FruitFlight(_tints[i]);
                _fruitNames[_fruitCount].text = name;
                _tints[_fruitCount] = _tints[i];
                _fruitCount++;
            }

            for (int i = 0; i < _fruitIcons.Count; i++)
            {
                bool on = i < _fruitCount;
                _fruitIcons[i].gameObject.SetActive(on);
                _fruitNames[i].gameObject.SetActive(on);
            }

            Layout(_canvasW, _canvasH, _safeTop, _safeBottom, _clearBottom);
        }

        private void ApplyFact()
        {
            string? key = FactKey;
            string text = key != null ? Loc.Fact(key) : string.Empty;
            _fact.text = text;
            _fact.gameObject.SetActive(text.Length > 0);
            string source = key != null ? SourceOf(key) : string.Empty;
            _source.text = source.Length > 0 ? UiText.MapCardSource(source) : string.Empty;
            _source.gameObject.SetActive(source.Length > 0);
            _otherFact.Button.gameObject.SetActive(_factKeys.Count > 1);
        }

        // En una parada futura, el fondo bajo el velo con el cielo y los montes apagados, y el héroe (su PNG o la silueta
        // provisional) en una sola tinta oscura con su filo de luz y sin reflejo: el reflejo del PNG, oculto; el de la silueta
        // provisional, que va en su misma malla, recortado en la línea del agua por su ToneEffect (ClipBelow, en PlaceScene).
        // Los tonos rehacen la malla una vez (al abrir); al apagarlos, la malla vuelve a ser la de siempre, con su reflejo.
        private void ApplyFutureTone(bool on)
        {
            _sceneVeil.gameObject.SetActive(on);
            for (int i = 0; i < _mutes.Count; i++)
            {
                _mutes[i].enabled = on;
            }

            _heroTone.enabled = on;
            _shapeTone.enabled = on;
            _backdrop.HeroImage.canvasRenderer.SetColor(Color.white);
            _backdrop.ShapeGraphic.canvasRenderer.SetColor(Color.white);
            _backdrop.HeroReflection.canvasRenderer.SetAlpha(on ? 0f : 1f);
        }

        private static ToneEffect AddTone(Graphic graphic)
        {
            ToneEffect tone = graphic.gameObject.AddComponent<ToneEffect>();
            tone.enabled = false;
            return tone;
        }

        // ============================================================================================ tono de una parada futura

        /// <summary>
        /// El color <paramref name="c"/> con el tono de una parada futura: su saturación a <paramref name="saturation"/> (sobre la
        /// luminancia Rec. 601), por <paramref name="multiply"/> y bajo un velo de <see cref="Palette.WoodDark"/> de opacidad
        /// <paramref name="veil"/> (el velo de <see cref="FutureVeilColor"/> hecho en el color). Conserva el alfa.
        /// </summary>
        public static Color FutureTone(Color c, float saturation, float multiply, float veil)
        {
            float lum = (0.299f * c.r) + (0.587f * c.g) + (0.114f * c.b);
            float r = (lum + ((c.r - lum) * saturation)) * multiply;
            float g = (lum + ((c.g - lum) * saturation)) * multiply;
            float b = (lum + ((c.b - lum) * saturation)) * multiply;
            Color wood = Palette.WoodDark;
            return new Color(
                Mathf.Clamp01(Mathf.Lerp(r, wood.r, veil)),
                Mathf.Clamp01(Mathf.Lerp(g, wood.g, veil)),
                Mathf.Clamp01(Mathf.Lerp(b, wood.b, veil)),
                c.a);
        }

        /// <summary>Qué hace un <see cref="ToneEffect"/>.</summary>
        public enum ToneKind
        {
            /// <summary>Apaga los colores (<see cref="FutureTone"/>).</summary>
            Mute = 0,

            /// <summary>Silueta de <see cref="SilhouetteColor"/> con un filo de luz de <see cref="RimLightColor"/>.</summary>
            Silhouette = 1,
        }

        /// <summary>
        /// Efecto de malla del tono de una parada futura (revisión de M4.1) sobre un gráfico del fondo de la tarjeta: con
        /// <see cref="ToneKind.Mute"/>, cambia el color de cada vértice por su <see cref="FutureTone"/> (en los gráficos con color
        /// por vértice, el cielo y la banda, el cambio es exacto; en el panorama, que lleva la textura, solo oscurece, y el velo
        /// hace el resto); con <see cref="ToneKind.Silhouette"/>, pinta la malla en <see cref="SilhouetteColor"/> y, detrás, cuatro
        /// copias desplazadas <see cref="RimDistance"/> en <see cref="RimLightColor"/>: el filo de luz. Con una línea del agua
        /// (<see cref="ClipBelow"/>), la silueta y su filo son solo lo de encima de ella: los triángulos que no suben de la línea
        /// (el reflejo de la silueta provisional, que va en la misma malla) se descartan y los demás, con sus copias, se recortan
        /// en ella. Sin textura ni shader propio; solo trabaja al rehacer la malla (al abrir la tarjeta), con sus listas y su
        /// polígono reservados.
        /// </summary>
        public sealed class ToneEffect : BaseMeshEffect
        {
            private readonly List<UIVertex> _in = new List<UIVertex>();
            private readonly List<UIVertex> _out = new List<UIVertex>();
            private readonly UIVertex[] _tri = new UIVertex[3];
            private readonly UIVertex[] _poly = new UIVertex[4];

            /// <summary>Qué hace.</summary>
            public ToneKind Kind { get; private set; }

            /// <summary>Saturación (con <see cref="ToneKind.Mute"/>).</summary>
            public float Saturation { get; private set; } = 1f;

            /// <summary>Factor de oscurecido (con <see cref="ToneKind.Mute"/>).</summary>
            public float Multiply { get; private set; } = 1f;

            /// <summary>Opacidad del velo de madera oscura hecho en el color (con <see cref="ToneKind.Mute"/>).</summary>
            public float Veil { get; private set; }

            /// <summary>Desplazamiento del filo de luz, en unidades locales del gráfico (con <see cref="ToneKind.Silhouette"/>).</summary>
            public float RimDistance { get; private set; } = 1f;

            /// <summary>
            /// Línea del agua en y local del gráfico (con <see cref="ToneKind.Silhouette"/>): nada de la silueta ni de su filo baja
            /// de ella. NaN = sin recorte.
            /// </summary>
            public float WaterLineY { get; private set; } = float.NaN;

            /// <summary>
            /// Recorta la silueta en la línea del agua <paramref name="localY"/> (y local del gráfico, hacia arriba; NaN = sin
            /// recorte); se devuelve a sí mismo.
            /// </summary>
            public ToneEffect ClipBelow(float localY)
            {
                bool same = float.IsNaN(localY) ? float.IsNaN(WaterLineY) : Mathf.Approximately(WaterLineY, localY);
                if (!same)
                {
                    WaterLineY = localY;
                    Dirty();
                }

                return this;
            }

            /// <summary>Lo configura para apagar los colores; se devuelve a sí mismo.</summary>
            public ToneEffect Mute(float saturation, float multiply, float veil)
            {
                Kind = ToneKind.Mute;
                Saturation = saturation;
                Multiply = multiply;
                Veil = veil;
                Dirty();
                return this;
            }

            /// <summary>Lo configura como silueta con filo de luz a <paramref name="rimDistance"/>; se devuelve a sí mismo.</summary>
            public ToneEffect Silhouette(float rimDistance)
            {
                Kind = ToneKind.Silhouette;
                if (!Mathf.Approximately(RimDistance, rimDistance))
                {
                    RimDistance = rimDistance;
                    Dirty();
                }

                return this;
            }

            /// <inheritdoc />
            public override void ModifyMesh(VertexHelper vh)
            {
                if (!IsActive())
                {
                    return;
                }

                _in.Clear();
                vh.GetUIVertexStream(_in);
                _out.Clear();
                int n = _in.Count;
                if (Kind == ToneKind.Silhouette)
                {
                    // Las cuatro copias del filo (todos sus triángulos) y, encima, la silueta.
                    float d = RimDistance;
                    for (int copy = 0; copy < 5; copy++)
                    {
                        var offset = new Vector3(copy == 0 ? d : copy == 1 ? -d : 0f, copy == 2 ? d : copy == 3 ? -d : 0f, 0f);
                        Color tint = copy < 4 ? RimLightColor : SilhouetteColor;
                        for (int t = 0; t + 2 < n; t += 3)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                UIVertex v = _in[t + j];
                                v.position += offset;
                                v.color = WithAlpha(tint, v.color.a);
                                _tri[j] = v;
                            }

                            EmitAboveWater(t);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < n; i++)
                    {
                        UIVertex v = _in[i];
                        v.color = FutureTone(v.color, Saturation, Multiply, Veil);
                        _out.Add(v);
                    }
                }

                vh.Clear();
                vh.AddUIVertexTriangleStream(_out);
            }

            // El triángulo _tri (el t de la malla, ya desplazado y teñido): tal cual sin línea del agua; con ella, nada si el
            // triángulo de la malla no sube de la línea (el reflejo) y, si no, su parte de encima (Sutherland-Hodgman contra
            // y ≥ línea: como mucho un cuadrilátero, en abanico).
            private void EmitAboveWater(int t)
            {
                float line = WaterLineY;
                if (float.IsNaN(line))
                {
                    _out.Add(_tri[0]);
                    _out.Add(_tri[1]);
                    _out.Add(_tri[2]);
                    return;
                }

                const float eps = 0.001f;
                if (_in[t].position.y <= line + eps && _in[t + 1].position.y <= line + eps && _in[t + 2].position.y <= line + eps)
                {
                    return; // bajo el agua o en su línea: el reflejo
                }

                int count = 0;
                for (int j = 0; j < 3; j++)
                {
                    UIVertex a = _tri[j];
                    UIVertex b = _tri[(j + 1) % 3];
                    bool aIn = a.position.y >= line;
                    bool bIn = b.position.y >= line;
                    if (aIn)
                    {
                        _poly[count++] = a;
                    }

                    if (aIn != bIn)
                    {
                        float f = (line - a.position.y) / (b.position.y - a.position.y);
                        UIVertex c = a;
                        c.position = Vector3.Lerp(a.position, b.position, f);
                        c.position.y = line;
                        c.color = Color32.Lerp(a.color, b.color, f);
                        c.uv0 = Vector4.Lerp(a.uv0, b.uv0, f);
                        _poly[count++] = c;
                    }
                }

                for (int k = 1; k + 1 < count; k++)
                {
                    _out.Add(_poly[0]);
                    _out.Add(_poly[k]);
                    _out.Add(_poly[k + 1]);
                }
            }

            // c con su alfa multiplicada por la del vértice (0-255).
            private static Color32 WithAlpha(Color c, byte vertexAlpha)
            {
                return new Color(c.r, c.g, c.b, c.a * (vertexAlpha / 255f));
            }

            private void Dirty()
            {
                if (graphic != null)
                {
                    graphic.SetVerticesDirty();
                }
            }
        }

        private void EnsureFruit(int i)
        {
            while (_fruitIcons.Count <= i)
            {
                Image icon = UiBuilder.Image("FruitIcon" + _fruitIcons.Count, _panel.transform, null, Color.white);
                icon.preserveAspect = true;
                TextMeshProUGUI name = UiBuilder.Label("FruitName" + _fruitNames.Count, _panel.transform, TextStyle.Body, string.Empty, RowSp, Palette.Cream, TextAlignmentOptions.Left);
                _fruitIcons.Add(icon);
                _fruitNames.Add(name);
            }
        }

        // ============================================================================================ maquetación

        /// <summary>
        /// Coloca la tarjeta en un lienzo de <paramref name="canvasW"/> × <paramref name="canvasH"/> dp con sus zonas seguras: la
        /// placa centrada, con lo que se ve de arriba abajo; si no cabe, el fondo se encoge (hasta <see cref="SceneMinDp"/>). Con
        /// <paramref name="clearBottom"/> &gt; 0 (el borde de arriba, en dp desde arriba del lienzo, de lo que hay debajo: «Jugar» del
        /// Mapa), la placa acaba al menos <see cref="ClearOfBelowDp"/> por encima, recortando antes el fondo (hasta
        /// <see cref="SceneFitMinDp"/>); si ni así cabe, se centra en toda la pantalla como sin él (revisión de DC10).
        /// </summary>
        public void Layout(float canvasW, float canvasH, float safeTop, float safeBottom, float clearBottom = 0f)
        {
            _canvasW = canvasW;
            _canvasH = canvasH;
            _safeTop = safeTop;
            _safeBottom = safeBottom;
            _clearBottom = clearBottom;
            if (!Visible)
            {
                return;
            }

            float panelW = canvasW - (2f * SideDp);
            float inner = panelW - (2f * PadDp);
            float available = canvasH - safeTop - safeBottom - (2f * SideDp);
            float sceneFull = inner * (SceneSkyDp + SceneWaterDp) / SceneWorldWidthDp;
            float rest = ContentHeight(inner, 0f);
            bool hasScene = _sceneWindow.gameObject.activeSelf;
            float region = available;
            float sceneRoom = available - rest;
            if (clearBottom > 0f)
            {
                // Encima de «Jugar»: si cabe con el fondo a SceneFitMinDp (o entero, si es menor), la placa se queda en esa franja.
                float above = clearBottom - ClearOfBelowDp - safeTop - SideDp;
                float need = rest + (hasScene ? Mathf.Min(SceneFitMinDp, sceneFull) : 0f);
                if (above < available && above >= need)
                {
                    region = above;
                    sceneRoom = Mathf.Floor(above - rest); // en dp enteros: la placa (redondeada hacia arriba) no se pasa
                }
            }

            float scene = hasScene ? Mathf.Clamp(sceneRoom, SceneMinDp, sceneFull) : 0f;
            float panelH = ContentHeight(inner, scene);
            float top = safeTop + SideDp + Mathf.Max(0f, (region - panelH) * 0.5f);
            UiBuilder.Place(_panel.rectTransform, SideDp, top, panelW, panelH);

            // Cabecera: ←, la mini cuia (o el «?») y la placa del lugar.
            float y = PadDp - 4f;
            UiBuilder.Place((RectTransform)_close.Button.transform, PadDp - 4f, y, MinTouchDp, MinTouchDp);
            float headH = HeaderHeight(inner);
            float x = PadDp - 4f + MinTouchDp + 8f;
            if (_sample.Root.gameObject.activeSelf)
            {
                _sample.Place(x, y + ((headH - _sample.HeightDp) * 0.5f));
                x += _sample.WidthDp + 8f;
            }

            if (_badge.gameObject.activeSelf)
            {
                UiBuilder.Place(_badge.rectTransform, x, y + ((headH - ProceduralArt.MapViewpointDp) * 0.5f), ProceduralArt.MapViewpointDp, ProceduralArt.MapViewpointDp);
                x += ProceduralArt.MapViewpointDp + 8f;
            }

            float titleW = Mathf.Max(48f, panelW - PadDp - x);
            UiBuilder.Place(_title.rectTransform, x, y, titleW, headH);
            UiBuilder.Place(_futureLevel.rectTransform, x, y, titleW, headH); // «Nivel n», en la cabecera, centrado en vertical
            y += headH + GapDp;

            if (_sceneWindow.gameObject.activeSelf)
            {
                PlaceScene(PadDp, y, inner, scene);
                y += scene + GapDp;
            }

            if (_fact.gameObject.activeSelf)
            {
                float h = Mathf.Ceil(UiBuilder.MinTextHeight(_fact, inner)) + 2f;
                UiBuilder.Place(_fact.rectTransform, PadDp, y, inner, h);
                y += h + 4f;
            }

            if (_source.gameObject.activeSelf)
            {
                float h = Mathf.Ceil(UiBuilder.MinTextHeight(_source, inner)) + 2f;
                UiBuilder.Place(_source.rectTransform, PadDp, y, inner, h);
                y += h + 4f;
            }

            if (OtherFactShown)
            {
                float w = Mathf.Min(inner, Mathf.Max(MinTouchDp * 2f, Mathf.Ceil(UiBuilder.MinTextWidth(_otherFactLabel)) + 40f));
                UiBuilder.Place((RectTransform)_otherFact.Button.transform, PadDp + inner - w, y, w, MinTouchDp);
                y += MinTouchDp + GapDp;
            }

            if (_faunaName.gameObject.activeSelf)
            {
                UiBuilder.Place(_portraitBack.rectTransform, PadDp - 2f, y - 2f, PortraitDp + 4f, PortraitDp + 4f);
                UiBuilder.Place(_portrait.rectTransform, PadDp, y, PortraitDp, PortraitDp);
                float nameX = PadDp + PortraitDp + 10f;
                UiBuilder.Place(_faunaName.rectTransform, nameX, y, inner - (nameX - PadDp), PortraitDp);
                y += PortraitDp + GapDp;
            }

            // Las frutas, en fila y, si no caben, en otra: icono y nombre.
            float fx = PadDp;
            for (int i = 0; i < _fruitCount; i++)
            {
                float w = FruitWidth(i);
                if (fx > PadDp && fx + w > PadDp + inner)
                {
                    fx = PadDp;
                    y += FruitIconDp + 6f;
                }

                UiBuilder.Place(_fruitIcons[i].rectTransform, fx, y, FruitIconDp, FruitIconDp);
                UiBuilder.Place(_fruitNames[i].rectTransform, fx + FruitIconDp + 4f, y, w - FruitIconDp - 4f, FruitIconDp);
                fx += w + 12f;
            }
        }

        // Alto de la placa con el fondo a scene dp (0 = sin contar el fondo).
        private float ContentHeight(float inner, float scene)
        {
            float h = PadDp - 4f + HeaderHeight(inner) + GapDp;
            if (_sceneWindow.gameObject.activeSelf)
            {
                h += scene + GapDp;
            }

            if (_fact.gameObject.activeSelf)
            {
                h += Mathf.Ceil(UiBuilder.MinTextHeight(_fact, inner)) + 2f + 4f;
            }

            if (_source.gameObject.activeSelf)
            {
                h += Mathf.Ceil(UiBuilder.MinTextHeight(_source, inner)) + 2f + 4f;
            }

            if (OtherFactShown)
            {
                h += MinTouchDp + GapDp;
            }

            if (_faunaName.gameObject.activeSelf)
            {
                h += PortraitDp + GapDp;
            }

            if (_fruitCount > 0)
            {
                float fx = 0f;
                int rows = 1;
                for (int i = 0; i < _fruitCount; i++)
                {
                    float w = FruitWidth(i);
                    if (fx > 0f && fx + w > inner)
                    {
                        rows++;
                        fx = 0f;
                    }

                    fx += w + 12f;
                }

                h += (rows * FruitIconDp) + ((rows - 1) * 6f) + GapDp;
            }

            return Mathf.Ceil(h + PadDp - GapDp);
        }

        // Alto de la cabecera: el ← de 48 dp o la placa del lugar (o «Nivel n»), si en dos líneas es más alta.
        private float HeaderHeight(float inner)
        {
            float h = MinTouchDp;
            if (_title.gameObject.activeSelf)
            {
                float titleW = TitleWidth(_canvasW);
                h = Mathf.Max(h, Mathf.Ceil(UiBuilder.MinTextHeight(_title, titleW)) + 4f);
            }

            if (_futureLevel.gameObject.activeSelf)
            {
                h = Mathf.Max(h, Mathf.Ceil(UiBuilder.MinTextHeight(_futureLevel, TitleWidth(_canvasW))) + 4f);
            }

            return h;
        }

        // Ancho de la placa del lugar en un lienzo de canvasW dp: la placa menos el ←, la mini cuia (o el «?»; nada en una parada
        // futura) y sus huecos.
        private float TitleWidth(float canvasW)
        {
            float panelW = canvasW - (2f * SideDp);
            float icon = _mode == StopCardMode.Viewpoint ? ProceduralArt.MapViewpointDp + 8f : _mode == StopCardMode.Future ? 0f : _sample.WidthDp + 8f;
            float used = PadDp - 4f + MinTouchDp + 8f + icon;
            return Mathf.Max(48f, panelW - PadDp - used);
        }

        private float FruitWidth(int i)
        {
            return FruitIconDp + 4f + Mathf.Ceil(UiBuilder.MinTextWidth(_fruitNames[i]));
        }

        // La ventana del fondo y, dentro, el mundo de 360 dp de diseño escalado al ancho de la ventana, con el horizonte a
        // SceneSkyDp de su borde de arriba (o algo más arriba si la ventana encoge) y la banda de agua hasta abajo.
        private void PlaceScene(float x, float y, float w, float h)
        {
            UiBuilder.Place(_sceneFrame.rectTransform, x - 3f, y - 3f, w + 6f, h + 6f);
            UiBuilder.Place(_sceneWindow, x - ClipPadDp, y - ClipPadDp, w + (2f * ClipPadDp), h + (2f * ClipPadDp));
            float s = w / SceneWorldWidthDp;
            _heroTone.Silhouette(RimLightDp / Mathf.Max(0.01f, s)); // el filo de luz mide RimLightDp en pantalla
            _shapeTone.Silhouette(RimLightDp / Mathf.Max(0.01f, s));
            float worldH = h / Mathf.Max(0.01f, s);
            float horizon = Mathf.Min(SceneSkyDp, worldH - 24f);
            // La silueta provisional pinta su reflejo en su misma malla (dp desde arriba del fondo, en y local −y): en una parada
            // futura se recorta en la línea del agua, sin reflejo (revisión de M4.1). Solo actúa con el tono puesto.
            _shapeTone.ClipBelow(-horizon);
            RectTransform world = _sceneWorld;
            world.anchorMin = new Vector2(0f, 1f);
            world.anchorMax = new Vector2(0f, 1f);
            world.pivot = new Vector2(0f, 1f);
            world.sizeDelta = new Vector2(SceneWorldWidthDp, worldH);
            world.localScale = new Vector3(s, s, 1f);
            world.anchoredPosition = new Vector2(ClipPadDp, -ClipPadDp);
            UiBuilder.Place(_panorama.rectTransform, 0f, horizon - SceneHorizonDp, SceneWorldWidthDp, SceneWorldWidthDp * (ProceduralArt.PanoramaHeightDp / 360f));
            _backdrop.Layout(SceneWorldWidthDp, horizon, worldH, false, worldH);
        }
    }

    /// <summary>
    /// Mini cuia de vidrio con el agua de un sitio (la del Mapa, <see cref="ProceduralArt.MapMiniCuia"/>, a una escala): vidrio, el
    /// agua del sitio y, en las de dos aguas (<c>clear_white</c>, <c>black_white</c>), la segunda en la mitad de abajo (como las
    /// mini cuias del Mapa, M3.5). Vacía, en el Muestrario, es de vidrio oscuro (<see cref="ProceduralArt.MapMiniCuiaEmpty"/>: la
    /// misma silueta y la misma caja, así todas se apoyan en la misma línea y lo ganado manda; revisión de M4.1). La usan la
    /// tarjeta de la parada y el Muestrario (M4.1).
    /// </summary>
    internal sealed class SampleCuia
    {
        private readonly ProceduralArt _art;
        private readonly Image _glass;
        private readonly Image _waterA;
        private readonly Image _waterB;
        private readonly float _scale;

        public SampleCuia(string name, Transform parent, ProceduralArt art, float scale)
        {
            _art = art;
            _scale = scale;
            Root = UiBuilder.Rect(name, parent);
            Root.anchorMin = new Vector2(0f, 1f);
            Root.anchorMax = new Vector2(0f, 1f);
            Root.pivot = new Vector2(0f, 1f);
            Root.sizeDelta = new Vector2(WidthDp, HeightDp);
            _glass = Piece("Glass", art.MapMiniCuia, ProceduralArt.MiniCuiaWidthDp, ProceduralArt.MiniCuiaHeightDp);
            _waterA = Piece("Water", art.MapMiniCuiaWater, ProceduralArt.MiniCuiaWidthDp, ProceduralArt.MiniCuiaHeightDp);
            _waterB = Piece("WaterMeet", art.MapMiniCuiaWater, ProceduralArt.MiniCuiaWidthDp, ProceduralArt.MiniCuiaHeightDp);
            foreach (Image water in new[] { _waterA, _waterB })
            {
                water.type = Image.Type.Filled;
                water.fillMethod = Image.FillMethod.Vertical;
                water.fillOrigin = (int)Image.OriginVertical.Bottom;
            }
        }

        /// <summary>Raíz (pivote arriba a la izquierda).</summary>
        public RectTransform Root { get; }

        /// <summary>Ancho (dp).</summary>
        public float WidthDp => ProceduralArt.MiniCuiaWidthDp * _scale;

        /// <summary>Alto (dp).</summary>
        public float HeightDp => ProceduralArt.MiniCuiaHeightDp * _scale;

        /// <summary>Lleva agua.</summary>
        public bool Filled => _waterA.gameObject.activeSelf;

        /// <summary>Color del agua (la del sitio).</summary>
        public Color WaterColor => _waterA.color;

        /// <summary>Va de vidrio oscuro (vacía, en el Muestrario, sin el PNG de <c>map_minicuia</c>).</summary>
        public bool DarkEmpty => !Filled && _glass.sprite == _art.MapMiniCuiaEmpty;

        /// <summary>El vidrio (tests: la misma caja llena o vacía).</summary>
        public Image Glass => _glass;

        /// <summary>
        /// Llena la mini cuia con el agua de <paramref name="sample"/> (<c>white</c>, <c>black</c>, <c>clear</c>, <c>clear_white</c>
        /// o <c>black_white</c>) si <paramref name="filled"/>; si no, vidrio vacío: de vidrio oscuro con <paramref name="darkEmpty"/>
        /// (el Muestrario) o, con el PNG de <c>map_minicuia</c> (que no da la silueta), ese vidrio translúcido.
        /// </summary>
        public void Set(string sample, bool filled, bool darkEmpty)
        {
            Color a = ArrivalStrip.SampleWater(sample ?? string.Empty, out Color? b);
            _waterA.color = a;
            _waterB.color = b ?? a;
            _waterA.gameObject.SetActive(filled);
            _waterB.gameObject.SetActive(filled && b.HasValue);
            _waterA.fillAmount = 1f;
            _waterB.fillAmount = 0.5f;
            bool dark = !filled && darkEmpty && ArtSlots.Find(ProceduralArt.MiniCuiaSlot) == null;
            _glass.sprite = dark ? _art.MapMiniCuiaEmpty : _art.MapMiniCuia;
            _glass.color = filled || dark ? Color.white : new Color(1f, 1f, 1f, RiverMapScreen.UnreachedGlassAlpha);
        }

        /// <summary>Coloca la mini cuia con su esquina de arriba a la izquierda en (x, y) dp del padre (y hacia abajo).</summary>
        public void Place(float x, float y)
        {
            UiBuilder.Place(Root, x, y, WidthDp, HeightDp);
        }

        private Image Piece(string name, Sprite sprite, float w, float h)
        {
            Image img = UiBuilder.Image(name, Root, sprite, Color.white);
            img.preserveAspect = true;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w * _scale, h * _scale);
            rt.anchoredPosition = Vector2.zero;
            return img;
        }
    }
}
