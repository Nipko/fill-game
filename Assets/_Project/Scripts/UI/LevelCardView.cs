#nullable enable
using System;
using System.Collections.Generic;
using AF.Core.Mechanics;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Tarjeta previa de 1 toque (tarea T13 del paso 1; 02 §10; propuesta-mecanicas-dificultad.md §c.1; 07 §7.7) y tutorial de una
    /// pantalla de cada estreno (02 §8, §9.1, §11 T4, §12 y §13; §b.0 #4 y §b.2), hecha con <see cref="ModalKit"/> (Dim, Card,
    /// Header y Line) y el material de D-003: tarjeta de madera clara pulida con marco de madera oscura, cabecera ámbar o índigo con
    /// el sello de la rana o del jaguar, medallón de madera tallada y luz, nunca papel ni acuarela. Se cierra con 1 toque en
    /// cualquier punto (el velo cubre toda la pantalla: ningún botón de debajo recibe el toque).
    /// </summary>
    /// <remarks>
    /// <para><b>Variantes</b> (<see cref="LevelCardKind"/>):</para>
    /// <list type="bullet">
    /// <item><description>Dificultad: cabecera «Difícil» o «Muy difícil» con la rana o el jaguar, el subtítulo «Aguas bravas» o
    /// «Noche de jaguar» (si <c>ui_show_difficulty_subtitles</c>), «Premio: 60» u «80» con la semilla y la cinta «×3» o «×4» y la
    /// fila de iconos [P]: el chip «?» si el nivel es turbio (<see cref="MurkyChip"/>, sin editarlo) y los iconos de las mecánicas
    /// del nivel, del Barro y de las Cuias chica y alta, cada uno en su ranura de madera clara (el del Matapi, en una de agua
    /// oscura, como en el tablero: <c>ProceduralArt.CardIconOnWater</c>).</description></item>
    /// <item><description>Estreno: el icono grande de la mecánica en su medallón (de agua oscura para el Matapi) y el texto del
    /// tutorial.</description></item>
    /// <item><description>Las dos cosas (nivel 100): la de dificultad con el tutorial de Aguas turbias, que lleva la línea T4
    /// obligatoria, y la línea de la Luciérnaga (04 §2.5) con «+1 Luciérnaga».</description></item>
    /// </list>
    /// <para><b>Texto del tutorial</b>: en una caja de <see cref="TextBoxWidth"/> (≤ <see cref="TutorialMaxWidthDp"/> =
    /// 360 − 2 × 16), en tinta y Nunito Regular algo engrosada, a 16 sp; si a 100 % no cabe en <see cref="MaxTutorialLines"/>
    /// líneas, a 14 y después a 12 sp, el mínimo de 02 §12. Si el texto tiene un corte fuerte («. », «: » o «; ») y cada parte
    /// cabe en una línea, se parte ahí (así la regla T4 del 100 va en su propia línea); si no, las líneas se equilibran (la caja se
    /// estrecha hasta el ancho más pequeño con las mismas líneas), para que no quede una línea larga pegada al marco y una palabra
    /// suelta debajo. El texto es siempre el canónico: como mucho, un salto de línea ocupa el lugar de un espacio.</para>
    /// <para><b>Escala de la fuente</b> (<see cref="FontScale"/>; 02 §12: hasta el 130 % sin desbordes ni recortes): todos los
    /// textos se multiplican por ella y la tarjeta crece en alto; el tutorial conserva el tamaño elegido a 100 % por la escala, así
    /// que a 130 % puede pasar de 2 líneas (el límite de 02 §12 es a 100 %), y el corte de frase elegido a 100 % mientras no
    /// añada líneas (la regla T4 del 100 sigue empezando línea). La escala del sistema aún no llega al juego (no hay ajuste ni
    /// lectura de la escala de Android): hasta entonces vale 1 y los tests la fijan a 1,3 [P].</para>
    /// <para><b>Estreno de la Piraña</b> (183; D-012, punto 1: opción C + D): en lugar del medallón, un tablero en miniatura con
    /// el mordisco animado (<see cref="PiranaBiteDemo"/>): una cuia recibe la dosis y la Piraña sale de la cueva y se la lleva;
    /// termina con la tarjeta abierta y, con «Reducir movimiento», es una versión quieta.</para>
    /// <para><b>Hito p20</b> (tarea M2.5; 02 §10 y 07 §7.7; D-013, punto 8): en la tarjeta del nivel L con L ≥ 40, L mod 20 = 0
    /// y parada a la que se llega al ganarlo (<see cref="HitoStopFor"/>: <c>RiverStopCatalog.ArrivalOnWin(L)</c>), una fila
    /// nueva abajo, tras una raya: la silueta del sitio en una ventanita de vidrio de <see cref="HitoSilhouetteWidthDp"/> ×
    /// <see cref="HitoSilhouetteHeightDp"/> dp [P] con marco de madera (el cielo y el agua del sitio, y su héroe: el PNG de su
    /// ranura o, solo con <see cref="MapArtRule"/>, la silueta provisional de <see cref="StopShapes"/>; el Encuentro, que no
    /// tiene héroe, con su pieza por código de las dos aguas) y «Si lo superas, llegas a {lugar}» (<c>map_hito_preview</c>) en
    /// Nunito Bold y tinta, a 14 sp si cabe en <see cref="MaxHitoLines"/> líneas a 100 % y si no a 12, nunca menos. Sin nombre
    /// del lugar en el idioma (en release, un nombre sin el visto bueno del asesor es «») no hay fila; sin silueta que pintar,
    /// la línea va sola. La tarjeta crece; si con la fila ya no cabe en el alto libre de la pantalla, la fila se omite
    /// (<see cref="HitoOmitted"/>) [P]. El 200 y siguientes la llevan solos cuando el catálogo tenga sus paradas.</para>
    /// <para>Entra con un fundido y un leve rebote (<see cref="FadeInMs"/>; sin rebote con «Reducir movimiento») y sale con un
    /// fundido de <see cref="FadeOutMs"/> sin bloquear toques, para que el siguiente toque ya salte la intro [P]. Sin asignaciones
    /// por fotograma: todo se coloca al mostrarla, al cambiar la escala o la pantalla.</para>
    /// </remarks>
    internal sealed class LevelCardView
    {
        /// <summary>
        /// Ancho máximo de la tarjeta (dp) [P]: el del panel del tablero a 360 dp (≈ 343), así la tarjeta se alinea con él; las de
        /// Magic Sort ocupan el 85-93 % del ancho (ficha §8.3) y esta, el 95 %, para que el tutorial quepa a 14 sp con aire.
        /// </summary>
        public const float MaxWidthDp = 344f;

        /// <summary>Margen mínimo de la tarjeta a cada lado de la pantalla (dp).</summary>
        public const float SideMarginDp = 8f;

        /// <summary>Ancho máximo de la caja del texto del tutorial (dp): 360 − 2 × 16 (T13).</summary>
        public const float TutorialMaxWidthDp = 328f;

        /// <summary>De cada canto de la tarjeta a la caja del tutorial (dp): el marco de 8 dp y 8 de aire (312 dp a 360).</summary>
        public const float TutorialInsetDp = 16f;

        /// <summary>Tamaños del tutorial a 100 %, de mayor a menor (sp; 07 §7.1: 16 / 14 / 12, el mínimo de 02 §12).</summary>
        public static readonly float[] TutorialSizesSp = { 16f, 14f, 12f };

        /// <summary>Tamaño mínimo del tutorial (sp; 02 §12: «≥12 sp»).</summary>
        public const float MinTutorialSp = 12f;

        /// <summary>Líneas máximas del tutorial a 360 dp con la fuente al 100 % (02 §12 y §13; §b.0 #4).</summary>
        public const int MaxTutorialLines = 2;

        /// <summary>Escala máxima de la fuente del sistema que se respeta (02 §12: «por encima del 130 % se fija ese tope»).</summary>
        public const float MaxFontScale = 1.3f;

        /// <summary>
        /// Opacidad del velo [P]: la de ¿Seguro? y Nivel fallido (0,55). Con el proyecto en Linear se mezcla en lineal y oscurece
        /// menos de lo que dice: el tablero se sigue viendo detrás (a 0,45 apenas se notaba).
        /// </summary>
        public const float DimAlpha = 0.55f;

        /// <summary>Entrada: fundido y rebote de 0,92 a 1 (ms) [P].</summary>
        public const float FadeInMs = 220f;

        /// <summary>Salida: fundido (ms), ya sin bloquear toques [P].</summary>
        public const float FadeOutMs = 140f;

        /// <summary>Lado de cada icono de la fila de mecánicas (dp; 07 §7.6: glifo de 32 dp).</summary>
        public const float IconDp = 32f;

        /// <summary>Lado de la ranura de madera de cada icono (dp) [P]: la fila se lee como el inventario del nivel.</summary>
        public const float IconSlotDp = 40f;

        /// <summary>Hueco entre ranuras de la fila (dp).</summary>
        public const float IconGapDp = 8f;

        /// <summary>Iconos como mucho en la fila (7 mecánicas, Barro, Cuia chica y Cuia alta).</summary>
        public const int MaxIcons = 10;

        /// <summary><c>intro_mechanic</c> del estreno de la Piraña (183): su tarjeta lleva el mordisco animado.</summary>
        public const string PiranaIntro = "pirana";

        /// <summary>
        /// Alto mínimo de la tarjeta con el mordisco animado (dp) [P]: su cuerpo cae en el alto compartido de 281 dp de la
        /// superficie de madera, el de la tarjeta del 100 (<see cref="ModalKit.SharedSurfaceHeight"/>), y no genera otra textura;
        /// lo que falta se reparte como aire sobre el escenario.
        /// </summary>
        public const float BiteCardMinHeightDp = 272f;

        /// <summary>Primer hito p20 con la fila «Si lo superas, llegas a…» (02 §10: el 40 y, desde el 60, los Muy difícil).</summary>
        public const int HitoFirstLevel = 40;

        /// <summary>Cada cuántos niveles hay un hito p20 (el ciclo de 20 de 03 §12).</summary>
        public const int HitoEveryLevels = 20;

        /// <summary>Ancho de la silueta del sitio en la fila del hito (dp) [P] (plan del Mapa, M2.5): el héroe de 240 dp a 4/15.</summary>
        public const float HitoSilhouetteWidthDp = 64f;

        /// <summary>Alto de la silueta del sitio en la fila del hito (dp) [P]: los 80 dp del héroe a la misma escala, redondeados.</summary>
        public const float HitoSilhouetteHeightDp = 22f;

        /// <summary>Marco de madera de la ventanita de la silueta, a cada lado (dp) [P].</summary>
        public const float HitoFrameDp = 2f;

        /// <summary>Tamaños de <c>map_hito_preview</c> a 100 %, de mayor a menor (sp; 14 y, si no cabe, 12, el mínimo de 02 §12).</summary>
        public static readonly float[] HitoSizesSp = { 14f, 12f };

        /// <summary>Líneas máximas de <c>map_hito_preview</c> a 100 % (plan del Mapa, M2.5).</summary>
        public const int MaxHitoLines = 2;

        /// <summary>Origen de la silueta del hito: el PNG del héroe del sitio.</summary>
        public const string HitoSilhouettePng = StopBackdrop.HeroPng;

        /// <summary>Origen de la silueta del hito: la silueta provisional de <see cref="StopShapes"/>.</summary>
        public const string HitoSilhouetteShape = StopBackdrop.HeroShape;

        /// <summary>Origen de la silueta del hito: la pieza por código de las dos aguas (el Encuentro, ya definitiva).</summary>
        public const string HitoSilhouetteCode = "code";

        /// <summary>Sin silueta que pintar (la línea va sola) o sin fila.</summary>
        public const string HitoSilhouetteNone = StopBackdrop.HeroNone;

        // Horizonte de la ventanita (dp desde arriba): 48 dp del héroe sobre él a la escala de 64 / 240 (12,8), redondeado.
        private const float HitoHorizonDp = 13f;
        private const float HitoGapDp = 8f;
        private const float HitoRadiusDp = 5f;
        private const float HitoGlossAlpha = 0.28f;

        // Ondas del agua de la ventanita [P]: (dp bajo el horizonte, x inicial, x final), de tres en tres.
        private static readonly float[] HitoRipples = { 3f, 6f, 18f, 3f, 44f, 56f, 6f, 24f, 38f };

        private const float PadDp = 16f;
        private const float HeaderDp = 40f;
        private const float TitleSp = 22f;
        private const float TitleMinSp = 16f;
        private const float SubtitleSp = 16f;
        private const float RewardSp = 18f;
        private const float RibbonSp = 16f;
        private const float SeedDp = 24f;
        private const float FireflySp = 14f;
        private const float GiftSp = 14f;
        private const float GiftDp = 28f;
        private const float PopFrom = 0.92f;
        private const float PopOutTo = 0.96f;
        private const float RowGapDp = 8f;

        // Engrosado del texto corrido [P]: Nunito Regular a 14-16 sp sobre la madera clara se leía grisáceo; la dilatación del SDF
        // lo acerca al SemiBold de 07 §7.1 sin cambiar las medidas (ni las líneas ni los anchos de la maqueta).
        private const float BodyDilate = 0.18f;

        private static readonly Color DividerColor = Color.Lerp(Palette.PanelLightLow, Palette.FamilyWood.Light, 0.55f);

        // Ranura de los iconos: la madera clara del cuerpo algo más oscura, sin contorno [P].
        private static readonly Color SlotColor = Color.Lerp(Palette.PanelLightLow, Palette.FamilyWood.Light, 0.28f);

        private readonly ProceduralArt _art;
        private readonly Action _onTap;
        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _dim;
        private readonly RectTransform _pop;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly Image _badge;
        private readonly TextMeshProUGUI _subtitle;
        private readonly RectTransform _rewardRow;
        private readonly Image _seed;
        private readonly TextMeshProUGUI _reward;
        private readonly Image _ribbon;
        private readonly TextMeshProUGUI _ribbonText;
        private readonly RectTransform _iconRow;
        private readonly Image[] _slots = new Image[MaxIcons];
        private readonly Image[] _icons = new Image[MaxIcons];
        private readonly MurkyChip _murky;
        private readonly Image _medallion;
        private readonly Image _hero;
        private readonly Image _divider;
        private readonly TextMeshProUGUI _tutorial;
        private readonly TextMeshProUGUI _firefly;
        private readonly RectTransform _gift;
        private readonly Image _giftEdge;
        private readonly Image _giftPlate;
        private readonly Image _giftGlyph;
        private readonly TextMeshProUGUI _giftText;
        private readonly Image _hitoDivider;
        private readonly RectTransform _hitoRow;
        private readonly Image _hitoFrame;
        private readonly RectTransform _hitoGlass;
        private readonly StopShapeGraphic _hitoScene;
        private readonly Image _hitoHero;
        private readonly StopShapeGraphic _hitoGloss;
        private readonly TextMeshProUGUI _hitoText;
        private RiverStop? _hitoStop;
        private string _hitoSource = HitoSilhouetteNone;
        private bool _hitoOn;
        private bool _hitoOmitted;
        private float _hitoBaseSp;
        private int _hitoLines;
        private PiranaBiteDemo? _bite;
        private bool _biteOn;
        private LevelCardInfo? _card;
        private int _iconCount;
        private float _fontScale = 1f;
        private float _tutorialBaseSp;
        private int _tutorialLines;
        private string _tutorialSource = string.Empty;
        private float _textBoxWidth = TutorialMaxWidthDp;
        private float _animMs = -1f;
        private bool _dismissing;
        private bool _entered;
        private bool _reduceMotion;
        private float _canvasW = 360f;
        private float _canvasH = 780f;
        private float _safeTop = 32f;
        private float _safeBottom = 24f;
        private Material? _bodyMaterial;
        private float _fadeFrom = 1f;
        private float _scaleFrom = 1f;

        /// <summary>Crea la tarjeta, oculta, bajo <paramref name="parent"/>; <paramref name="onTap"/> recibe el toque.</summary>
        public LevelCardView(Transform parent, ProceduralArt art, Action onTap)
        {
            _art = art;
            _onTap = onTap ?? throw new ArgumentNullException(nameof(onTap));
            _root = UiBuilder.Rect("LevelCard", parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();

            // El velo recibe el toque en cualquier punto (también sobre la tarjeta, cuyos gráficos no reciben raycast).
            _dim = ModalKit.Dim("Dim", _root, DimAlpha);
            _dim.gameObject.AddComponent<PointerTapRelay>().Bind(OnTap);

            _pop = UiBuilder.Rect("Pop", _root);
            _panel = ModalKit.Card("Panel", _pop, art, out _surface);
            _title = ModalKit.Header(_panel, art, UiText.DifficultyHard, out _header);
            _badge = ModalKit.HeaderBadge(_header);
            _subtitle = ModalKit.Line("Subtitle", _panel, string.Empty, SubtitleSp, Palette.FamilyWood.Base);
            _subtitle.enableAutoSizing = false;

            _rewardRow = UiBuilder.Rect("Reward", _panel);
            _seed = UiBuilder.Image("Seed", _rewardRow, art.IconSeed, Color.white);
            _seed.preserveAspect = true;
            _reward = UiBuilder.Label("Value", _rewardRow, TextStyle.PlainBold, string.Empty, RewardSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
            _ribbon = UiBuilder.Image("Ribbon", _rewardRow, null, Color.white); // la cinta de DifficultyChip, perezosa: Show
            _ribbonText = UiBuilder.Label("Value", _ribbon.transform, TextStyle.ModalTitle, string.Empty, RibbonSp, Palette.Cream, TextAlignmentOptions.Center);

            _iconRow = UiBuilder.Rect("Icons", _panel);
            for (int i = 0; i < _icons.Length; i++)
            {
                _slots[i] = UiBuilder.Sliced("Slot" + i, _iconRow, art.RoundedRect, SlotColor);
                _slots[i].pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / 10f; // radio de 10 dp
                _slots[i].gameObject.SetActive(false);
                _icons[i] = UiBuilder.Image("Icon" + i, _slots[i].transform, null, Color.white);
                _icons[i].preserveAspect = true;
            }

            _murky = MurkyChip.Create(_iconRow, art);

            _medallion = UiBuilder.Image("Medallion", _panel, null, Color.white); // sprites perezosos: Show
            _hero = UiBuilder.Image("Hero", _medallion.transform, null, Color.white);
            _hero.preserveAspect = true;
            _divider = UiBuilder.Image("Divider", _panel, null, DividerColor);

            // Tutorial y Luciérnaga en tinta (07 §7.1: «tinta #2B2118 sobre papel marfil», 13,3:1): con la madera oscura, los trazos
            // finos de Nunito Regular se leían grisáceos.
            _tutorial = ModalKit.Line("Tutorial", _panel, string.Empty, TutorialSizesSp[0], Palette.Ink, TextStyle.Body);
            _tutorial.enableAutoSizing = false;
            _firefly = ModalKit.Line("Firefly", _panel, UiText.FireflyLine, FireflySp, Palette.Ink, TextStyle.Body);
            _firefly.enableAutoSizing = false;
            ThickenText(_tutorial, _firefly);

            // «+1 Luciérnaga»: píldora de madera oscura con canto (crema encima, 13,7:1) y la luciérnaga encendida.
            _gift = UiBuilder.Rect("Gift", _panel);
            _giftEdge = UiBuilder.Sliced("Edge", _gift, art.RoundedRect, Palette.FamilyWood.Outline);
            _giftPlate = UiBuilder.Sliced("Plate", _gift, art.RoundedRect, Palette.WoodDark);
            _giftGlyph = UiBuilder.Image("Firefly", _gift, null, Color.white);
            _giftGlyph.preserveAspect = true;
            _giftText = UiBuilder.Label("Label", _gift, TextStyle.PlainBold, UiText.FireflyGift, GiftSp, Palette.Cream, TextAlignmentOptions.MidlineLeft);

            // Hito p20 (M2.5): raya, ventanita de vidrio con marco de madera (cielo, agua y héroe del sitio, recortados al vidrio, y
            // el brillo del vidrio encima) y «Si lo superas, llegas a {lugar}» en Nunito Bold y tinta.
            _hitoDivider = UiBuilder.Image("HitoDivider", _panel, null, DividerColor);
            _hitoRow = UiBuilder.Rect("Hito", _panel);
            _hitoFrame = UiBuilder.Sliced("Frame", _hitoRow, art.RoundedRect, Palette.FamilyWood.Base);
            _hitoFrame.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / HitoRadiusDp;
            _hitoGlass = UiBuilder.Rect("Glass", _hitoFrame.transform);
            _hitoGlass.gameObject.AddComponent<RectMask2D>();
            _hitoScene = ShapeGraphic("Scene", _hitoGlass);
            _hitoHero = UiBuilder.Image("Hero", _hitoGlass, null, Color.white);
            _hitoHero.preserveAspect = false;
            _hitoGloss = ShapeGraphic("Gloss", _hitoGlass);
            _hitoText = UiBuilder.Label("Line", _hitoRow, TextStyle.PlainBold, string.Empty, HitoSizesSp[0], Palette.Ink, TextAlignmentOptions.MidlineLeft);
            _hitoText.textWrappingMode = TextWrappingModes.Normal;
            _hitoText.enableAutoSizing = false;
            _hitoDivider.gameObject.SetActive(false);
            _hitoRow.gameObject.SetActive(false);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Raíz de la tarjeta (con el velo), estirada a la pantalla.</summary>
        public RectTransform Root => _root;

        /// <summary>La tarjeta se ve (también mientras sale).</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>La tarjeta está saliendo: ya no recibe toques.</summary>
        public bool Dismissing => _dismissing;

        /// <summary>Tarjeta mostrada (null si no hay).</summary>
        public LevelCardInfo? Card => _card;

        /// <summary>
        /// Escala de la fuente del sistema, entre 1 y <see cref="MaxFontScale"/> (02 §12). Quien la cambia con la tarjeta abierta
        /// llama después a <see cref="Layout"/>.
        /// </summary>
        public float FontScale
        {
            get => _fontScale;
            set => _fontScale = Mathf.Clamp(value, 1f, MaxFontScale);
        }

        /// <summary>Opacidad de la tarjeta y del velo (fundidos).</summary>
        public float Alpha => _group.alpha;

        /// <summary>La tarjeta recibe toques (no mientras sale).</summary>
        public bool BlocksRaycasts => _group.blocksRaycasts;

        /// <summary>Velo a pantalla completa que recibe el toque (tests).</summary>
        public Image Dim => _dim;

        /// <summary>Cuerpo de la tarjeta (tests y AF_LAYOUT).</summary>
        public RectTransform Panel => _panel;

        /// <summary>Superficie de madera del cuerpo (tests: su sprite es de la escala de altos compartidos y se estira).</summary>
        public Image Surface => _surface;

        /// <summary>Placa de la cabecera, o null si no se ve (tests de solapes).</summary>
        public RectTransform? HeaderRect => _header.gameObject.activeSelf ? _header.rectTransform : null;

        /// <summary>Fila del premio (semilla, «Premio: N» y cinta), o null si no se ve (tests de solapes).</summary>
        public RectTransform? RewardRowRect => _rewardRow.gameObject.activeSelf ? _rewardRow : null;

        /// <summary>Medallón del estreno, o null si no se ve (tests de solapes).</summary>
        public RectTransform? MedallionRect => _medallion.gameObject.activeSelf ? _medallion.rectTransform : null;

        /// <summary>Mordisco animado del estreno de la Piraña, o null si la tarjeta no lo lleva (tests y capturas).</summary>
        public PiranaBiteDemo? BiteDemo => _biteOn ? _bite : null;

        /// <summary>Escenario del mordisco animado, o null si no se ve (tests de solapes).</summary>
        public RectTransform? BiteRect => _biteOn && _bite != null ? _bite.Root : null;

        /// <summary>Píldora «+1 Luciérnaga», o null si no se ve (tests de solapes).</summary>
        public RectTransform? GiftRect => _gift.gameObject.activeSelf ? _gift : null;

        /// <summary>Contenedor que escala la tarjeta en la entrada y la salida (tests).</summary>
        public RectTransform Pop => _pop;

        /// <summary>Texto de la cabecera («Difícil» o «Muy difícil»), o null si no hay cabecera.</summary>
        public string? TitleText => _header.gameObject.activeSelf ? _title.text : null;

        /// <summary>Sello de la cabecera (rana o jaguar), o null.</summary>
        public Sprite? HeaderBadge => _header.gameObject.activeSelf && _badge.gameObject.activeSelf ? _badge.sprite : null;

        /// <summary>Sprite de la banda de la cabecera, o null si no hay cabecera.</summary>
        public Sprite? HeaderBandSprite => _header.gameObject.activeSelf ? _header.sprite : null;

        /// <summary>Subtítulo temático, o null si no se ve.</summary>
        public string? SubtitleText => _subtitle.gameObject.activeSelf ? _subtitle.text : null;

        /// <summary>«Premio: N», o null si no se ve.</summary>
        public string? RewardText => _rewardRow.gameObject.activeSelf ? _reward.text : null;

        /// <summary>Icono de la línea del premio (la semilla), o null si no se ve.</summary>
        public Sprite? RewardIcon => _rewardRow.gameObject.activeSelf ? _seed.sprite : null;

        /// <summary>Texto de la cinta («×3» o «×4»), o null si no se ve.</summary>
        public string? RibbonText => _rewardRow.gameObject.activeSelf && _ribbon.gameObject.activeSelf ? _ribbonText.text : null;

        /// <summary>Iconos de la fila de mecánicas a la vista.</summary>
        public int IconCount => _iconCount;

        /// <summary>Chip «?» de Aguas turbias de la tarjeta (07 §7.7; tests).</summary>
        public MurkyChip Murky => _murky;

        /// <summary>El chip «?» se ve en la tarjeta.</summary>
        public bool MurkyShown => _iconRow.gameObject.activeSelf && _murky.Visible;

        /// <summary>Icono grande del estreno en su medallón, o null si no se ve.</summary>
        public Sprite? HeroSprite => _medallion.gameObject.activeSelf ? _hero.sprite : null;

        /// <summary>Medallón del icono grande del estreno (de madera clara o de agua oscura), o null si no se ve.</summary>
        public Sprite? MedallionSprite => _medallion.gameObject.activeSelf ? _medallion.sprite : null;

        /// <summary>Texto del tutorial, o null si no se ve.</summary>
        public string? TutorialText => _tutorial.gameObject.activeSelf ? _tutorialSource : null;

        /// <summary>
        /// Texto del tutorial tal como se pinta: el canónico, con un salto de línea en lugar del espacio tras «. », «: » o «; »
        /// si se partió ahí (ver <c>LayoutTutorial</c>); null si no se ve.
        /// </summary>
        public string? TutorialShownText => _tutorial.gameObject.activeSelf ? _tutorial.text : null;

        /// <summary>Texto del tutorial (tests: líneas y desbordes).</summary>
        public TextMeshProUGUI Tutorial => _tutorial;

        /// <summary>Material engrosado del texto corrido (tutorial y Luciérnaga), o null si la fuente no lo admite (tests).</summary>
        public Material? BodyMaterial => _bodyMaterial;

        /// <summary>Tamaño del tutorial elegido a 100 % (16, 14 o 12 sp; 0 sin tutorial).</summary>
        public float TutorialBaseSp => _tutorial.gameObject.activeSelf ? _tutorialBaseSp : 0f;

        /// <summary>Líneas del tutorial con la escala actual (0 sin tutorial).</summary>
        public int TutorialLines => _tutorial.gameObject.activeSelf ? _tutorialLines : 0;

        /// <summary>Ancho de la caja del tutorial (dp): <see cref="TutorialMaxWidthDp"/> como mucho.</summary>
        public float TextBoxWidth => _textBoxWidth;

        /// <summary>Línea de la Luciérnaga, o null si no se ve.</summary>
        public string? FireflyText => _firefly.gameObject.activeSelf ? _firefly.text : null;

        /// <summary>«+1 Luciérnaga», o null si no se ve.</summary>
        public string? GiftText => _gift.gameObject.activeSelf ? _giftText.text : null;

        /// <summary>Glifo de la píldora «+1 Luciérnaga», o null si no se ve.</summary>
        public Sprite? GiftIcon => _gift.gameObject.activeSelf ? _giftGlyph.sprite : null;

        /// <summary>Parada del hito p20 de la tarjeta mostrada (con su fila a la vista o no), o null si no es un hito con parada.</summary>
        public RiverStop? HitoStop => _hitoStop;

        /// <summary>«Si lo superas, llegas a {lugar}», o null si la fila no se ve.</summary>
        public string? HitoText => _hitoRow.gameObject.activeSelf ? _hitoText.text : null;

        /// <summary>
        /// La fila del hito se omitió porque con ella la tarjeta no cabía en el alto libre de la pantalla [P] (plan del Mapa, M2.5,
        /// punto 2).
        /// </summary>
        public bool HitoOmitted => _hitoOmitted;

        /// <summary>
        /// Origen de la silueta de la fila del hito: <see cref="HitoSilhouettePng"/>, <see cref="HitoSilhouetteShape"/>,
        /// <see cref="HitoSilhouetteCode"/> o <see cref="HitoSilhouetteNone"/> (sin ventanita o sin fila).
        /// </summary>
        public string HitoSilhouette => _hitoRow.gameObject.activeSelf ? _hitoSource : HitoSilhouetteNone;

        /// <summary>Fila del hito (ventanita y línea), o null si no se ve (tests de solapes).</summary>
        public RectTransform? HitoRowRect => _hitoRow.gameObject.activeSelf ? _hitoRow : null;

        /// <summary>Ventanita de la silueta con su marco, o null si no se ve (tests de solapes y de tamaño).</summary>
        public RectTransform? HitoWindowRect => _hitoRow.gameObject.activeSelf && _hitoFrame.gameObject.activeSelf ? _hitoFrame.rectTransform : null;

        /// <summary>Vidrio de la ventanita (la silueta de <see cref="HitoSilhouetteWidthDp"/> × <see cref="HitoSilhouetteHeightDp"/> dp).</summary>
        public RectTransform HitoGlass => _hitoGlass;

        /// <summary>Malla de la ventanita: cielo, agua, pieza por código y silueta provisional (tests).</summary>
        public StopShapeGraphic HitoScene => _hitoScene;

        /// <summary>PNG del héroe en la ventanita, o null si no se ve (tests).</summary>
        public Sprite? HitoHeroSprite => _hitoHero.gameObject.activeSelf ? _hitoHero.sprite : null;

        /// <summary>Línea del hito (tests: líneas, tamaño y desbordes).</summary>
        public TextMeshProUGUI HitoLine => _hitoText;

        /// <summary>Tamaño de <c>map_hito_preview</c> elegido a 100 % (14 o 12 sp; 0 sin fila).</summary>
        public float HitoBaseSp => _hitoRow.gameObject.activeSelf ? _hitoBaseSp : 0f;

        /// <summary>Líneas de <c>map_hito_preview</c> con la escala actual (0 sin fila).</summary>
        public int HitoLines => _hitoRow.gameObject.activeSelf ? _hitoLines : 0;

        /// <summary>
        /// Parada del hito p20 del nivel <paramref name="level"/> (02 §10; D-013, punto 8): la de
        /// <see cref="RiverStopCatalog.ArrivalOnWin"/> si <paramref name="level"/> ≥ <see cref="HitoFirstLevel"/> y es múltiplo de
        /// <see cref="HitoEveryLevels"/>; null en los demás niveles, sin catálogo o sin esa parada (el 200 con las paradas de 1-200).
        /// </summary>
        public static RiverStop? HitoStopFor(int level, RiverStopCatalog? stops)
        {
            return level >= HitoFirstLevel && level % HitoEveryLevels == 0 ? stops?.ArrivalOnWin(level) : null;
        }

        /// <summary>Sprite del icono <paramref name="i"/> de la fila (0 ≤ i &lt; <see cref="IconCount"/>).</summary>
        public Sprite? IconAt(int i)
        {
            return (uint)i < (uint)_iconCount ? _icons[i].sprite : null;
        }

        /// <summary>Rectángulo del icono <paramref name="i"/> de la fila (tests de solape).</summary>
        public RectTransform IconRect(int i)
        {
            return _icons[i].rectTransform;
        }

        /// <summary>Color de la ranura del icono <paramref name="i"/> de la fila: madera clara o agua oscura (tests).</summary>
        public Color SlotColorAt(int i)
        {
            return _slots[i].color;
        }

        /// <summary>Textos a la vista de la tarjeta (tests de desbordes; asigna).</summary>
        public List<TextMeshProUGUI> VisibleTexts()
        {
            var texts = new List<TextMeshProUGUI>(8);
            AddIfVisible(texts, _title);
            AddIfVisible(texts, _subtitle);
            AddIfVisible(texts, _reward);
            AddIfVisible(texts, _ribbonText);
            AddIfVisible(texts, _tutorial);
            AddIfVisible(texts, _firefly);
            AddIfVisible(texts, _giftText);
            AddIfVisible(texts, _hitoText);
            return texts;
        }

        /// <summary>Gráficos de la tarjeta que reciben toques (tests: solo el velo).</summary>
        public List<Graphic> RaycastTargets()
        {
            var list = new List<Graphic>();
            Graphic[] all = _root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].raycastTarget)
                {
                    list.Add(all[i]);
                }
            }

            return list;
        }

        /// <summary>
        /// Muestra <paramref name="card"/> con su variante y la coloca en la pantalla de <paramref name="canvasW"/> ×
        /// <paramref name="canvasH"/> dp con la zona segura dada; entra con un fundido (sin rebote con
        /// <paramref name="reduceMotion"/>).
        /// </summary>
        public void Show(LevelCardInfo card, bool reduceMotion, float canvasW, float canvasH, float safeTop, float safeBottom)
        {
            Show(card, null, string.Empty, reduceMotion, canvasW, canvasH, safeTop, safeBottom);
        }

        /// <summary>
        /// Como <see cref="Show(LevelCardInfo, bool, float, float, float, float)"/>, con la fila del hito p20 hacia
        /// <paramref name="hitoStop"/> (<see cref="HitoStopFor"/>; null = sin fila), cuya ventanita pinta el agua
        /// <paramref name="hitoWater"/> (<c>water_base</c> de los niveles de esa parada: <c>white</c>, <c>clear</c> o
        /// <c>black</c>).
        /// </summary>
        public void Show(LevelCardInfo card, RiverStop? hitoStop, string hitoWater, bool reduceMotion, float canvasW, float canvasH,
            float safeTop, float safeBottom)
        {
            _card = card ?? throw new ArgumentNullException(nameof(card));
            _reduceMotion = reduceMotion;
            bool labeled = card.Kind != LevelCardKind.Debut;
            string? intro = card.IntroMechanic;

            // Cabecera con la etiqueta (§c.1 y 07 §7.7): ámbar con la rana o índigo con el jaguar.
            _header.gameObject.SetActive(labeled);
            if (labeled)
            {
                _title.text = UiText.DifficultyChip(card.Label) ?? string.Empty;
                ModalKit.ApplyHeaderLabel(_header, _title, _badge, _art, card.Label);
            }

            string? subtitle = labeled ? UiText.DifficultySubtitle(card.Label) : null;
            _subtitle.gameObject.SetActive(subtitle != null);
            _subtitle.text = subtitle ?? string.Empty;

            // «Premio: 60» con la semilla y la cinta ×3 (§c.2: el multiplicador que se paga, de eco_reward_*).
            _rewardRow.gameObject.SetActive(labeled);
            bool ribbon = labeled && card.RibbonMultiplier > 1;
            _ribbon.gameObject.SetActive(ribbon);
            if (labeled)
            {
                _reward.text = UiText.RewardLine(card.RewardSeeds);
            }

            if (ribbon)
            {
                _ribbon.sprite = DifficultyChip.RibbonSprite();
                _ribbonText.text = UiText.Ribbon(card.RibbonMultiplier);
            }

            // Fila de iconos [P] (§c.1): el chip «?» del nivel turbio y las mecánicas, el Barro y las Cuias chica y alta. Cada
            // icono en su ranura de madera clara, o de agua oscura si en la madera no se lee (el Matapi).
            _iconCount = 0;
            if (labeled)
            {
                for (int t = 1; t <= (int)MechanicTypes.Last; t++)
                {
                    var type = (MechanicType)t;
                    if (card.HasMechanic(type))
                    {
                        AddIcon(_art.MechanicIcon(type), ProceduralArt.CardIconOnWater(type));
                    }
                }

                if (card.HasMud)
                {
                    AddIcon(_art.MudIcon, false);
                }

                if (card.HasSmallCuia)
                {
                    AddIcon(_art.CuiaChicaIcon, false);
                }

                if (card.HasTallCuia)
                {
                    AddIcon(_art.CuiaAltaIcon, false);
                }
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                bool on = i < _iconCount;
                if (_slots[i].gameObject.activeSelf != on)
                {
                    _slots[i].gameObject.SetActive(on);
                }
            }

            _murky.SetVisible(card.IsMurky);
            _iconRow.gameObject.SetActive(_iconCount > 0 || card.IsMurky);

            // Estreno: el icono grande en su medallón (sin icono, sin medallón: el de Aguas turbias no existe, porque su estreno,
            // el 100, es la tarjeta combinada y no lleva medallón). La Piraña (183), en su lugar, el mordisco animado.
            _biteOn = card.Kind == LevelCardKind.Debut && string.Equals(intro, PiranaIntro, StringComparison.Ordinal);
            if (_biteOn)
            {
                _bite ??= new PiranaBiteDemo(_panel, _art);
            }
            else
            {
                _bite?.Hide();
            }

            Sprite? heroIcon = card.Kind == LevelCardKind.Debut && !_biteOn ? _art.CardHeroIcon(intro) : null;
            bool hero = heroIcon != null;
            _medallion.gameObject.SetActive(hero);
            if (hero)
            {
                _medallion.sprite = _art.CardMedallionFor(intro);
                _hero.sprite = heroIcon;
            }

            string? tutorial = UiText.Tutorial(intro);
            _tutorial.gameObject.SetActive(tutorial != null);
            _tutorialSource = tutorial ?? string.Empty;
            _tutorial.text = _tutorialSource;
            _divider.gameObject.SetActive(labeled && tutorial != null);

            // Luciérnaga (04 §2.5): la línea en el estreno de las turbias y «+1 Luciérnaga» cuando se regaló al cargar.
            bool fireflyLine = card.RevealGifted || string.Equals(intro, LevelFlow.MurkyIntroMechanic, StringComparison.Ordinal);
            _firefly.gameObject.SetActive(fireflyLine);
            _gift.gameObject.SetActive(card.RevealGifted);
            if (card.RevealGifted)
            {
                _giftGlyph.sprite = _art.CardFirefly;
            }

            ShowHito(hitoStop, hitoWater);
            _root.gameObject.SetActive(true);
            _dismissing = false;
            _entered = false;
            _group.blocksRaycasts = true;
            _group.interactable = true;
            Layout(canvasW, canvasH, safeTop, safeBottom);
            _animMs = 0f;
            ApplyEnter(0f);
            if (_biteOn)
            {
                _bite!.Show(reduceMotion); // después de colocarla: la versión quieta ya pinta el mordisco
            }
        }

        /// <summary>
        /// Coloca la tarjeta en la pantalla de <paramref name="canvasW"/> × <paramref name="canvasH"/> dp (con
        /// <see cref="FontScale"/>), centrada a lo ancho y al 45 % del alto libre, dentro de la zona segura.
        /// </summary>
        public void Layout(float canvasW, float canvasH, float safeTop, float safeBottom)
        {
            _canvasW = canvasW;
            _canvasH = canvasH;
            _safeTop = safeTop;
            _safeBottom = safeBottom;
            if (_card == null)
            {
                return;
            }

            float s = _fontScale;
            float cardW = Mathf.Min(MaxWidthDp, canvasW - (2f * SideMarginDp));
            float inner = cardW - (2f * PadDp);
            _textBoxWidth = Mathf.Min(TutorialMaxWidthDp, cardW - (2f * TutorialInsetDp));
            float y = PadDp;

            if (_header.gameObject.activeSelf)
            {
                // La placa crece con la escala hasta el alto de línea del título (Baloo 2: ≈ 1,6 veces el tamaño), así el título no
                // se encoge a 130 % (02 §12: «los contenedores crecen»).
                _title.fontSizeMax = TitleSp * s;
                _title.fontSizeMin = TitleMinSp * s;
                _title.fontSize = TitleSp * s;
                float titleH = Mathf.Ceil(_title.GetPreferredValues(_title.text, float.PositiveInfinity, float.PositiveInfinity).y);
                float headerH = Mathf.Max(HeaderDp, titleH + 2f);
                UiBuilder.Place(_header.rectTransform, PadDp, y, inner, headerH);
                y += headerH + 6f;
            }

            if (_subtitle.gameObject.activeSelf)
            {
                _subtitle.fontSize = SubtitleSp * s;
                y += PlaceCentered(_subtitle, cardW, y, inner) + 4f;
            }

            if (_rewardRow.gameObject.activeSelf)
            {
                y += LayoutReward(cardW, y, s) + RowGapDp;
            }

            if (_iconRow.gameObject.activeSelf)
            {
                y += LayoutIcons(cardW, inner, y) + RowGapDp;
            }

            if (_medallion.gameObject.activeSelf)
            {
                const float m = ProceduralArt.CardMedallionDp;
                const float icon = ProceduralArt.CardHeroIconDp;
                UiBuilder.Place(_medallion.rectTransform, (cardW - m) * 0.5f, y, m, m);
                UiBuilder.Place(_hero.rectTransform, (m - icon) * 0.5f, (m - icon) * 0.5f, icon, icon);
                y += m + 8f;
            }

            float biteY = y;
            if (_biteOn && _bite != null)
            {
                y += _bite.MeasureHeight() + 10f;
            }

            if (_divider.gameObject.activeSelf)
            {
                UiBuilder.Place(_divider.rectTransform, PadDp + 24f, y, inner - 48f, 2f);
                y += 2f + 10f;
            }

            if (_tutorial.gameObject.activeSelf)
            {
                y += LayoutTutorial(cardW, y, s) + RowGapDp;
            }

            if (_firefly.gameObject.activeSelf)
            {
                _firefly.fontSize = FireflySp * s;
                y += PlaceCentered(_firefly, cardW, y, inner) + 6f;
            }

            if (_gift.gameObject.activeSelf)
            {
                y += LayoutGift(cardW, y, s) + 4f;
            }

            // Hito p20, abajo y tras una raya. Si con la fila la tarjeta ya no cabe en el alto libre (el de su colocación, abajo), se
            // omite [P].
            _hitoOmitted = false;
            bool hito = _hitoOn;
            if (hito)
            {
                float rowY = y + 2f + 10f;
                float rowH = LayoutHito(cardW, inner, rowY, s);
                float withRow = Mathf.Ceil(rowY + rowH + RowGapDp + PadDp - RowGapDp + 4f);
                if (withRow > canvasH - safeTop - safeBottom - 16f)
                {
                    hito = false;
                    _hitoOmitted = true;
                }
                else
                {
                    UiBuilder.Place(_hitoDivider.rectTransform, PadDp + 24f, y, inner - 48f, 2f);
                    y = rowY + rowH + RowGapDp;
                }
            }

            _hitoDivider.gameObject.SetActive(hito);
            _hitoRow.gameObject.SetActive(hito);

            float cardH = Mathf.Ceil(y + PadDp - RowGapDp + 4f);
            if (_biteOn && _bite != null)
            {
                // Aire sobre el escenario hasta el alto mínimo (la superficie compartida de 281 dp); el escenario va debajo.
                float air = Mathf.Max(0f, BiteCardMinHeightDp - cardH);
                if (air > 0f)
                {
                    cardH += air;
                    ShiftBelow(biteY, air);
                }

                float stageW = cardW - (2f * TutorialInsetDp);
                _bite.Layout((cardW - stageW) * 0.5f, biteY + air, stageW);
            }

            float top = Mathf.Round((canvasH - cardH) * 0.45f);
            top = Mathf.Clamp(top, safeTop + 8f, Mathf.Max(safeTop + 8f, canvasH - safeBottom - 8f - cardH));
            float left = (canvasW - cardW) * 0.5f;
            _pop.anchorMin = new Vector2(0f, 1f);
            _pop.anchorMax = new Vector2(0f, 1f);
            _pop.pivot = new Vector2(0.5f, 0.5f);
            _pop.sizeDelta = new Vector2(cardW, cardH);
            _pop.anchoredPosition = new Vector2(left + (cardW * 0.5f), -(top + (cardH * 0.5f)));

            // La superficie de madera, de la escala de altos compartidos (ModalKit.SharedSurfaceHeight) y estirada al alto de la
            // tarjeta: con 5 o 6 altos de tarjeta en 1-200, solo se generan y se guardan las superficies de 3 altos.
            ModalKit.Size(_panel, _surface, _art, 0f, 0f, cardW, cardH, true);
        }

        // Baja air dp todo lo colocado desde y (en el cuerpo de la tarjeta): el tutorial, la Luciérnaga y la píldora.
        private void ShiftBelow(float y, float air)
        {
            ShiftIfBelow(_tutorial.rectTransform, y, air);
            ShiftIfBelow(_firefly.rectTransform, y, air);
            ShiftIfBelow(_gift, y, air);
            ShiftIfBelow(_divider.rectTransform, y, air);
            ShiftIfBelow(_hitoDivider.rectTransform, y, air);
            ShiftIfBelow(_hitoRow, y, air);
        }

        private static void ShiftIfBelow(RectTransform rt, float y, float air)
        {
            if (rt.gameObject.activeSelf && -rt.anchoredPosition.y >= y - 0.01f)
            {
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y - air);
            }
        }

        /// <summary>Vuelve a colocar la tarjeta abierta con la última pantalla (tras cambiar <see cref="FontScale"/>).</summary>
        public void Relayout()
        {
            Layout(_canvasW, _canvasH, _safeTop, _safeBottom);
        }

        /// <summary>Empieza la salida: deja de recibir toques y se funde en <see cref="FadeOutMs"/>.</summary>
        public void Dismiss()
        {
            if (!Visible || _dismissing)
            {
                return;
            }

            _dismissing = true;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _fadeFrom = _group.alpha; // si se toca durante la entrada, sale desde donde estaba
            _scaleFrom = _pop.localScale.x;
            _animMs = 0f;
        }

        /// <summary>La oculta en el acto (carga de otro nivel).</summary>
        public void HideNow()
        {
            _bite?.Hide();
            _biteOn = false;
            _dismissing = false;
            _animMs = -1f;
            _card = null;
            _hitoStop = null;
            _hitoOn = false;
            _hitoOmitted = false;
            _group.alpha = 1f;
            _pop.localScale = Vector3.one;
            if (_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }
        }

        /// <summary>True una sola vez, cuando la entrada termina (para el AF_LAYOUT de la tarjeta).</summary>
        public bool ConsumeEntered()
        {
            if (!_entered)
            {
                return false;
            }

            _entered = false;
            return true;
        }

        /// <summary>Avanza los fundidos de entrada y salida y el mordisco animado del estreno de la Piraña.</summary>
        public void Tick(float dtMs)
        {
            if (_biteOn && _bite != null && Visible)
            {
                _bite.Tick(dtMs);
            }

            if (_animMs < 0f || !Visible)
            {
                return;
            }

            _animMs += dtMs;
            if (_dismissing)
            {
                float t = Mathf.Clamp01(_animMs / FadeOutMs);
                _group.alpha = _fadeFrom * (1f - Ease.Out(t));
                float k = _reduceMotion ? 1f : Mathf.Lerp(_scaleFrom, PopOutTo, Ease.Out(t));
                _pop.localScale = new Vector3(k, k, 1f);
                if (t >= 1f)
                {
                    HideNow();
                }

                return;
            }

            ApplyEnter(_animMs);
            if (_animMs >= FadeInMs)
            {
                _animMs = -1f;
                _entered = true;
            }
        }

        // Una instancia del material del texto corrido con la cara algo dilatada (sin asset nuevo: se crea en tiempo de ejecución
        // y la descarga Unity con la escena). Si el material no tiene _FaceDilate, el texto se queda como está.
        private void ThickenText(TextMeshProUGUI a, TextMeshProUGUI b)
        {
            Material? shared = a.fontSharedMaterial;
            int dilate = Shader.PropertyToID("_FaceDilate");
            if (shared == null || !shared.HasProperty(dilate))
            {
                return;
            }

            _bodyMaterial = new Material(shared) { name = shared.name + " (tarjeta)" };
            _bodyMaterial.SetFloat(dilate, BodyDilate);
            a.fontSharedMaterial = _bodyMaterial;
            if (b.font == a.font)
            {
                b.fontSharedMaterial = _bodyMaterial;
            }
        }

        private void OnTap()
        {
            if (Visible && !_dismissing)
            {
                _onTap();
            }
        }

        private void ApplyEnter(float ms)
        {
            float t = Mathf.Clamp01(ms / FadeInMs);
            _group.alpha = Ease.Out(t);
            float k = _reduceMotion ? 1f : Mathf.LerpUnclamped(PopFrom, 1f, Ease.OutBack(t, 1.6f));
            _pop.localScale = new Vector3(k, k, 1f);
        }

        private void AddIcon(Sprite? sprite, bool onWater)
        {
            if (sprite == null || _iconCount >= _icons.Length)
            {
                return;
            }

            _icons[_iconCount].sprite = sprite;
            _slots[_iconCount].color = onWater ? ProceduralArt.CardWater : SlotColor;
            _iconCount++;
        }

        // Malla por vértices dentro del vidrio de la ventanita, con el origen arriba a la izquierda (StopShapeGraphic).
        private static StopShapeGraphic ShapeGraphic(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(StopShapeGraphic));
            go.transform.SetParent(parent, false);
            StopShapeGraphic g = go.GetComponent<StopShapeGraphic>();
            g.Setup();
            UiBuilder.Place(g.rectTransform, 0f, 0f, HitoSilhouetteWidthDp, HitoSilhouetteHeightDp);
            return g;
        }

        // Prepara la fila del hito hacia stop (null = sin fila): el texto (sin nombre del lugar en el idioma, sin fila) y la
        // ventanita (el PNG del héroe si existe; si no, la pieza por código de las dos aguas o, solo con MapArtRule, la silueta
        // provisional; sin nada de eso, la línea va sola). La malla se construye aquí, una vez por tarjeta.
        private void ShowHito(RiverStop? stop, string water)
        {
            _hitoStop = stop;
            _hitoOn = false;
            _hitoSource = HitoSilhouetteNone;
            _hitoScene.Clear();
            _hitoGloss.Clear();
            _hitoHero.sprite = null;
            _hitoHero.gameObject.SetActive(false);
            string place = stop != null ? UiText.StopName(stop) : string.Empty;
            string text = place.Length > 0 ? UiText.MapHitoPreview(place) : string.Empty;
            _hitoText.text = text;
            if (stop == null || text.Length == 0)
            {
                _hitoFrame.gameObject.SetActive(false);
                return;
            }

            _hitoOn = true;
            string? hero = stop.Backdrop.Hero;
            Sprite? png = null;
            if (hero != null)
            {
                Func<string, Sprite?>? find = StopBackdrop.HeroSpriteOverride;
                png = find != null ? find(hero) : ArtSlots.Find(hero);
            }

            string? code = stop.Backdrop.Code;
            string? shape = png == null ? StopShapes.ShapeOf(stop.Backdrop) : null;
            if (png != null)
            {
                _hitoSource = HitoSilhouettePng; // el PNG final manda siempre, también en release
            }
            else if (code == StopBackdrop.CodeBlackWhite || code == StopBackdrop.CodeClearWhite)
            {
                _hitoSource = HitoSilhouetteCode; // pieza por código, ya definitiva (StopShapes: sin silueta)
            }
            else if (shape != null && MapArtRule.CanShow(MapArtRule.ShowProvisional, false))
            {
                _hitoSource = HitoSilhouetteShape;
            }

            bool window = _hitoSource != HitoSilhouetteNone;
            _hitoFrame.gameObject.SetActive(window);
            if (window)
            {
                BuildHitoScene(stop, water, png, shape);
            }
        }

        // La ventanita de 64 × 22 dp: el cielo del sitio (el del panorama con el tinte de su mundo y su luz), su agua, el héroe con
        // el pie en el horizonte a 64 / 240 (el PNG o la silueta provisional, con su reflejo) o las dos aguas del Encuentro, y el
        // brillo del vidrio encima. Todo recortado al vidrio (RectMask2D).
        private void BuildHitoScene(RiverStop stop, string waterBase, Sprite? png, string? shape)
        {
            const float w = HitoSilhouetteWidthDp;
            const float h = HitoSilhouetteHeightDp;
            const float hy = HitoHorizonDp;
            const float k = HitoSilhouetteWidthDp / StopBackdrop.HeroWidthDp;
            Color lut = StopBackdrop.LutOf(stop);
            StopBackdrop.SkyOf(stop, out Color tintTop, out Color tintHorizon, out float alphaTop, out float alphaHorizon);
            Color skyTop = Color.Lerp(Palette.SkyTop, tintTop, alphaTop) * lut;
            Color skyHorizon = Color.Lerp(Palette.SkyHorizon, tintHorizon, alphaHorizon) * lut;
            Color water = Palette.WaterBase(waterBase) * lut;
            skyTop.a = 1f;
            skyHorizon.a = 1f;
            water.a = 1f;

            StopShapeGraphic g = _hitoScene;
            g.Rect(0f, 0f, w, hy, skyTop, skyHorizon);
            g.Rect(0f, hy, w, h, Color.Lerp(water, skyHorizon, 0.3f), water);

            // La orilla lejana (la selva del panorama) sobre el horizonte, para que la franja de abajo se lea como río y no como
            // arena; y unas ondas claras en el agua.
            Color shoreTop = Palette.FarPalm * lut;
            Color shoreBottom = Palette.FarShore * lut;
            shoreTop.a = 1f;
            shoreBottom.a = 1f;
            g.Rect(0f, hy - 2f, w, hy, shoreTop, shoreBottom);
            Color ripple = Palette.WaterLine;
            ripple.a = 0.45f;
            for (int i = 0; i < HitoRipples.Length; i += 3)
            {
                float ry = hy + HitoRipples[i];
                g.Rect(HitoRipples[i + 1], ry, HitoRipples[i + 2], ry + 0.75f, ripple, ripple);
            }

            string? code = stop.Backdrop.Code;
            if (_hitoSource == HitoSilhouetteCode)
            {
                // Las dos aguas (07 §2.1): con el Negro, la cuña beige del Amazonas desde la izquierda, junto al horizonte, hasta
                // el canto derecho; con el Tapajós, la franja beige lejana sobre el agua clara.
                Color far = Palette.WaterWhite * lut;
                far.a = 1f;
                float top = hy + 0.5f;
                if (code == StopBackdrop.CodeBlackWhite)
                {
                    var a = new Vector2(0f, top);
                    var c = new Vector2(w, hy + ((h - hy) * 0.7f));
                    g.Triangle(a, far, new Vector2(w, top), far, c, far);

                    // La costura clara entre las dos aguas, a lo largo de la cuña.
                    Color seam = Palette.WaterLine;
                    seam.a = 0.8f;
                    g.Quad(a, c, c + new Vector2(0f, 0.9f), a + new Vector2(0f, 0.9f), seam, seam);
                }
                else
                {
                    g.Rect(0f, top, w, hy + ((h - hy) * 0.45f), far, far);
                }
            }
            else if (_hitoSource == HitoSilhouetteShape && shape != null)
            {
                StopShapes.Draw(g, shape, w * 0.5f, hy, k, lut, stop.Backdrop.Lut, water, StopBackdrop.ReflectionAlpha,
                    StopBackdrop.ReflectionDp);
            }

            g.Commit();

            bool hero = _hitoSource == HitoSilhouettePng && png != null;
            _hitoHero.gameObject.SetActive(hero);
            if (hero)
            {
                // El PNG del héroe (240 × 80 dp, pie en el horizonte: 48 encima y 32 debajo) a la misma escala.
                _hitoHero.sprite = png;
                _hitoHero.color = new Color(lut.r, lut.g, lut.b, 1f);
                UiBuilder.Place(_hitoHero.rectTransform, (w * 0.5f) - (StopBackdrop.HeroWidthDp * k * 0.5f),
                    hy - (StopBackdrop.HeroAboveDp * k), StopBackdrop.HeroWidthDp * k, StopBackdrop.HeroHeightDp * k);
            }

            // El brillo del vidrio: una veladura blanca que se apaga hacia el horizonte.
            var gloss = new Color(1f, 1f, 1f, HitoGlossAlpha);
            var clear = new Color(1f, 1f, 1f, 0f);
            _hitoGloss.Rect(0f, 0f, w, h * 0.45f, gloss, clear);
            _hitoGloss.Commit();
        }

        // Fila del hito: la ventanita (si la hay) y la línea a su derecha, centradas en la tarjeta. La línea va a 14 sp si a 100 %
        // cabe en MaxHitoLines líneas junto a la ventanita y si no a 12 (02 §12: nunca menos); con la escala, ese tamaño por la
        // escala. Su caja se estrecha hasta el ancho más pequeño con las mismas líneas (líneas equilibradas). Devuelve el alto.
        private float LayoutHito(float cardW, float inner, float y, float s)
        {
            bool window = _hitoFrame.gameObject.activeSelf;
            float winW = window ? HitoSilhouetteWidthDp + (2f * HitoFrameDp) : 0f;
            float winH = window ? HitoSilhouetteHeightDp + (2f * HitoFrameDp) : 0f;
            float maxText = inner - (window ? winW + HitoGapDp : 0f);
            string text = _hitoText.text;
            _hitoBaseSp = HitoSizesSp[HitoSizesSp.Length - 1];
            for (int i = 0; i < HitoSizesSp.Length; i++)
            {
                _hitoText.fontSize = HitoSizesSp[i];
                if (LinesAt(_hitoText, text, maxText) <= MaxHitoLines)
                {
                    _hitoBaseSp = HitoSizesSp[i];
                    break;
                }
            }

            _hitoText.fontSize = _hitoBaseSp * s;
            float textW = BalancedWidth(_hitoText, text, maxText);
            float textH = Mathf.Ceil(_hitoText.GetPreferredValues(text, textW, float.PositiveInfinity).y) + 2f;
            float rowH = Mathf.Max(winH, textH);
            float rowW = winW + (window ? HitoGapDp : 0f) + textW;
            UiBuilder.Place(_hitoRow, Mathf.Round((cardW - rowW) * 0.5f), y, rowW, rowH);
            if (window)
            {
                UiBuilder.Place(_hitoFrame.rectTransform, 0f, Mathf.Round((rowH - winH) * 0.5f), winW, winH);
                UiBuilder.Place(_hitoGlass, HitoFrameDp, HitoFrameDp, HitoSilhouetteWidthDp, HitoSilhouetteHeightDp);
            }

            _hitoText.alignment = window ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Midline;
            UiBuilder.Place(_hitoText.rectTransform, rowW - textW, Mathf.Round((rowH - textH) * 0.5f), textW, textH);
            _hitoLines = LinesAt(_hitoText, text, textW);
            return rowH;
        }

        // «Premio: 60» con la semilla delante y la cinta «×3» detrás, centrados en la tarjeta. Devuelve el alto de la fila.
        private float LayoutReward(float cardW, float y, float s)
        {
            _reward.fontSize = RewardSp * s;
            bool ribbon = _ribbon.gameObject.activeSelf;
            float seed = SeedDp * s;
            float textW = Mathf.Ceil(_reward.GetPreferredValues(_reward.text, float.PositiveInfinity, float.PositiveInfinity).x) + 2f;
            float textH = Mathf.Ceil(_reward.GetPreferredValues(_reward.text, float.PositiveInfinity, float.PositiveInfinity).y) + 2f;
            float rw = ProceduralArt.RibbonTagWidthDp * s;
            float rh = ProceduralArt.RibbonTagHeightDp * s;
            const float gap = 6f;
            float gapRibbon = 8f * s;
            float total = seed + gap + textW + (ribbon ? gapRibbon + rw : 0f);
            float rowH = Mathf.Ceil(Mathf.Max(seed, Mathf.Max(textH, ribbon ? rh : 0f)));
            UiBuilder.Place(_rewardRow, Mathf.Round((cardW - total) * 0.5f), y, total, rowH);
            UiBuilder.Place(_seed.rectTransform, 0f, (rowH - seed) * 0.5f, seed, seed);
            UiBuilder.Place(_reward.rectTransform, seed + gap, 0f, textW, rowH);
            if (ribbon)
            {
                _ribbonText.fontSize = RibbonSp * s;
                UiBuilder.Place(_ribbon.rectTransform, seed + gap + textW + gapRibbon, (rowH - rh) * 0.5f, rw, rh);

                // La cifra, centrada en la franja de texto de la cinta (RibbonTagTextDp); su caja mide al menos su línea (la
                // línea de Baloo 2 es más alta que la franja, pero los glifos «×4» caben en ella).
                Rect t = ProceduralArt.RibbonTagTextDp;
                Vector2 pref = _ribbonText.GetPreferredValues(_ribbonText.text, float.PositiveInfinity, float.PositiveInfinity);
                float tw = Mathf.Max(t.width * s, Mathf.Ceil(pref.x) + 1f);
                float th = Mathf.Max(t.height * s, Mathf.Ceil(pref.y) + 1f);
                float cx = (t.x + (t.width * 0.5f)) * s;
                float cy = (t.y + (t.height * 0.5f)) * s;
                UiBuilder.Place(_ribbonText.rectTransform, cx - (tw * 0.5f), cy - (th * 0.5f), tw, th);
            }

            return rowH;
        }

        // Fila de iconos de 32 dp, cada uno en su ranura de 40 (el chip «?» primero, sin ranura, como junto a la etiqueta del
        // HUD), centrada y partida en varias filas si no cabe. Devuelve el alto que ocupa.
        private float LayoutIcons(float cardW, float inner, float y)
        {
            bool murky = _murky.Visible;
            float murkyW = MurkyChip.WidthFor(MurkyChip.HeightDp);
            int items = _iconCount + (murky ? 1 : 0);
            UiBuilder.Place(_iconRow, 0f, y, cardW, IconSlotDp);
            float rowY = 0f;
            int index = 0;
            float height = 0f;
            const float inset = (IconSlotDp - IconDp) * 0.5f;
            while (index < items)
            {
                // Cuántos caben en esta fila.
                float width = 0f;
                int count = 0;
                for (int i = index; i < items; i++)
                {
                    float w = (murky && i == 0) ? murkyW : IconSlotDp;
                    float next = width + (count > 0 ? IconGapDp : 0f) + w;
                    if (count > 0 && next > inner)
                    {
                        break;
                    }

                    width = next;
                    count++;
                }

                float x = Mathf.Round((cardW - width) * 0.5f);
                for (int i = index; i < index + count; i++)
                {
                    if (murky && i == 0)
                    {
                        _murky.Place(x, rowY + Mathf.Round((IconSlotDp - MurkyChip.HeightDp) * 0.5f));
                        x += murkyW + IconGapDp;
                        continue;
                    }

                    int icon = murky ? i - 1 : i;
                    UiBuilder.Place(_slots[icon].rectTransform, x, rowY, IconSlotDp, IconSlotDp);
                    UiBuilder.Place(_icons[icon].rectTransform, inset, inset, IconDp, IconDp);
                    x += IconSlotDp + IconGapDp;
                }

                index += count;
                height = rowY + IconSlotDp;
                rowY += IconSlotDp + IconGapDp;
            }

            _iconRow.sizeDelta = new Vector2(cardW, Mathf.Max(IconSlotDp, height));
            return Mathf.Max(IconSlotDp, height);
        }

        // Tutorial (02 §12 y §13): el mayor tamaño de 16, 14 y 12 sp con el que el texto canónico cabe en 2 líneas a 100 % en la
        // caja; con la escala, ese tamaño por la escala. Si a 100 % el texto tiene un corte fuerte («. », «: » o «; ») y cada parte
        // cabe en una línea, se parte ahí (en el 100, la regla T4 queda en su propia línea); si no, líneas equilibradas. Con la
        // escala, si las partes ya no caben en una línea, el corte se conserva mientras no añada líneas al texto entero equilibrado
        // (a 130 % la regla T4 del 100 sigue empezando línea; los demás no crecen más de lo necesario). Devuelve el alto.
        private float LayoutTutorial(float cardW, float y, float s)
        {
            string text = _tutorialSource;
            _tutorial.text = text;
            _tutorialBaseSp = MinTutorialSp;
            for (int i = 0; i < TutorialSizesSp.Length; i++)
            {
                _tutorial.fontSize = TutorialSizesSp[i];
                if (LinesAt(_tutorial, text, _textBoxWidth) <= MaxTutorialLines)
                {
                    _tutorialBaseSp = TutorialSizesSp[i];
                    break;
                }
            }

            // El corte se decide a 100 %, con el tamaño elegido.
            _tutorial.fontSize = _tutorialBaseSp;
            int cut = LinesAt(_tutorial, text, _textBoxWidth) >= 2 ? FindSentenceBreak(_tutorial, text, _textBoxWidth) : -1;
            string? split = cut >= 0 ? text.Substring(0, cut + 1) + "\n" + text.Substring(cut + 2) : null;
            _tutorial.fontSize = _tutorialBaseSp * s;
            float h;
            float widest = split != null ? WidestPart(_tutorial, text, cut) : float.MaxValue;
            if (split != null && widest + 2f <= _textBoxWidth)
            {
                // Cada frase en su línea (a 100 %, siempre que hay corte), en una caja tan ancha como la más larga.
                _tutorial.text = split;
                float w = Mathf.Min(_textBoxWidth, Mathf.Ceil(widest) + 2f);
                h = Mathf.Ceil(_tutorial.GetPreferredValues(split, w, float.PositiveInfinity).y) + 2f;
                UiBuilder.Place(_tutorial.rectTransform, Mathf.Round((cardW - w) * 0.5f), y, w, h);
            }
            else
            {
                if (split != null && LinesAt(_tutorial, split, _textBoxWidth) <= LinesAt(_tutorial, text, _textBoxWidth))
                {
                    _tutorial.text = split; // con la escala: cada frase en sus líneas, sin añadir ninguna
                }

                h = PlaceCentered(_tutorial, cardW, y, _textBoxWidth);
            }

            _tutorialLines = LinesAt(_tutorial, _tutorial.text, _tutorial.rectTransform.sizeDelta.x);
            return h;
        }

        // Corte en la puntuación fuerte: la posición del signo de la mejor de las particiones tras «. », «: » o «; » en la que las
        // dos partes caben en una línea de maxW con el tamaño de t (la de las líneas más parecidas), o −1. Asigna solo al colocar
        // la tarjeta.
        private static int FindSentenceBreak(TextMeshProUGUI t, string text, float maxW)
        {
            int at = -1;
            float best = float.MaxValue;
            for (int i = 1; i < text.Length - 2; i++)
            {
                char c = text[i];
                if ((c != '.' && c != ':' && c != ';') || text[i + 1] != ' ')
                {
                    continue;
                }

                float widest = WidestPart(t, text, i);
                if (widest + 2f <= maxW && widest < best)
                {
                    best = widest;
                    at = i;
                }
            }

            return at;
        }

        // Ancho de la más ancha de las dos partes de text cortado tras el signo de la posición at, con el tamaño de t.
        private static float WidestPart(TextMeshProUGUI t, string text, int at)
        {
            float a = t.GetPreferredValues(text.Substring(0, at + 1), float.PositiveInfinity, float.PositiveInfinity).x;
            float b = t.GetPreferredValues(text.Substring(at + 2), float.PositiveInfinity, float.PositiveInfinity).x;
            return Mathf.Max(a, b);
        }

        // «+1 Luciérnaga»: luciérnaga y texto crema sobre la píldora de madera oscura con su canto. Devuelve el alto.
        private float LayoutGift(float cardW, float y, float s)
        {
            _giftText.fontSize = GiftSp * s;
            float h = Mathf.Round(Mathf.Max(GiftDp * s, GiftDp));
            float glyph = ProceduralArt.CardFireflyDp * s;
            float textW = Mathf.Ceil(_giftText.GetPreferredValues(_giftText.text, float.PositiveInfinity, float.PositiveInfinity).x) + 2f;
            const float padL = 8f;
            const float gap = 4f;
            const float padR = 14f;
            float w = padL + glyph + gap + textW + padR;
            float mult = 24f / (h * 0.5f);
            _giftEdge.pixelsPerUnitMultiplier = mult;
            _giftPlate.pixelsPerUnitMultiplier = mult;
            UiBuilder.Place(_gift, Mathf.Round((cardW - w) * 0.5f), y, w, h + 2f);
            UiBuilder.Place(_giftEdge.rectTransform, 0f, 2f, w, h);
            UiBuilder.Place(_giftPlate.rectTransform, 0f, 0f, w, h);
            UiBuilder.Place(_giftGlyph.rectTransform, padL, (h - glyph) * 0.5f, glyph, glyph);
            UiBuilder.Place(_giftText.rectTransform, padL + glyph + gap, 0f, textW, h);
            return h + 2f;
        }

        // Coloca el texto t centrado en la tarjeta en y, como mucho de maxW de ancho, con las líneas equilibradas: la caja se
        // estrecha hasta el ancho más pequeño con el mismo número de líneas. Devuelve el alto.
        private static float PlaceCentered(TextMeshProUGUI t, float cardW, float y, float maxW)
        {
            string text = t.text;
            float w = BalancedWidth(t, text, maxW);
            float h = Mathf.Ceil(t.GetPreferredValues(text, w, float.PositiveInfinity).y) + 2f;
            UiBuilder.Place(t.rectTransform, Mathf.Round((cardW - w) * 0.5f), y, w, h);
            return h;
        }

        // Ancho de caja con las líneas equilibradas: el de una línea si cabe en una; si no, el más pequeño (≥ 30 % de maxW) con
        // el mismo número de líneas que en maxW, más 2 dp de holgura; como mucho maxW.
        private static float BalancedWidth(TextMeshProUGUI t, string text, float maxW)
        {
            int lines = LinesAt(t, text, maxW);
            if (lines <= 1)
            {
                return Mathf.Min(maxW, Mathf.Ceil(t.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x) + 2f);
            }

            float lo = maxW * 0.3f;
            float hi = maxW;
            for (int i = 0; i < 14; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (LinesAt(t, text, mid) <= lines)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid;
                }
            }

            return Mathf.Min(maxW, Mathf.Ceil(hi) + 2f);
        }

        // Líneas de text con el tamaño de t en una caja de width dp: el alto preferido entre el de una línea (el texto entero sin
        // cortes ni saltos, con los mismos glifos).
        private static int LinesAt(TextMeshProUGUI t, string text, float width)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            string single = text.IndexOf('\n') >= 0 ? text.Replace('\n', ' ') : text;
            float one = t.GetPreferredValues(single, float.PositiveInfinity, float.PositiveInfinity).y;
            if (one <= 0f)
            {
                return 1;
            }

            return Mathf.Max(1, Mathf.RoundToInt(t.GetPreferredValues(text, width, float.PositiveInfinity).y / one));
        }

        private static void AddIfVisible(List<TextMeshProUGUI> texts, TextMeshProUGUI t)
        {
            if (t.gameObject.activeInHierarchy)
            {
                texts.Add(t);
            }
        }
    }
}
