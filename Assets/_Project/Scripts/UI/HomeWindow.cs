#nullable enable
using System;
using AF.Levels;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Ventanita de casa de TiTino en el inicio (D-013, punto 6, Q17-A; 06 §4.7; propuesta del Mapa §6.1; propuesta del personaje
    /// g.2 y d.11; tarea M2.1): el único sitio de TiTino en el inicio. Él se queda en casa, su embaúba del estuario, y tú estás
    /// lejos: un marco de <see cref="SizeDp"/> × <see cref="SizeDp"/> dp [P] con la embaúba y su retrato
    /// (<see cref="ProceduralArt.HomeWindow"/> y <see cref="ProceduralArt.SlothPortrait(bool)"/>) y, a su lado, la píldora
    /// «Casa · a unos {km} km» (<c>map_home_distance</c>) con los km por agua hasta casa de la parada actual
    /// (<see cref="RiverStop.KmHome"/>), redondeados a la decena (<see cref="DisplayKm"/>) y con el separador de la región de
    /// <see cref="AF.Localization.Loc"/>.
    /// <list type="bullet">
    /// <item>En la parada 0 (Belém, niveles 1-10) está en casa: sin píldora [P] (<see cref="HasPill"/>).</item>
    /// <item>Sin nombre ni texto en el marco. En el inicio, TiTino vive dentro del marco (<see cref="HomeSloth"/>, tarea TI6c): su
    /// pose según la hora local, el saludo del día, y un área táctil sobre el marco con <c>sloth_look</c> y, como mucho una vez por
    /// sesión, el Dato del perezoso; lo demás, sin toques (<c>raycastTarget</c> = false). En el Mapa del
    /// río (M3.5) se crea con una acción de toque (<see cref="HomeWindow(Transform, ProceduralArt, Action)"/>): un área táctil
    /// de ≥ 48 dp sobre el marco y la píldora (<see cref="TapButton"/>) que lleva la vista a la casa, en Guajará; el retrato
    /// cierra los ojos cuando TiTino dormita (<see cref="SetAsleep"/>, <c>sloth_sleep</c> a los 30 s sin toques); y la píldora
    /// va debajo del marco, alineada a la izquierda (<see cref="PillBelow"/>), para no cruzar la zona de juego del Mapa.</item>
    /// <item>Arte provisional de P-M5 (<see cref="MapArtRule"/>): sin los PNG de <c>map_home_window</c> y del retrato, solo sale
    /// en el Editor y en desarrollo (<see cref="CanShow"/>); en release, nada hasta que exista su arte.</item>
    /// <item>La embaúba está pendiente del asesor cultural (propuesta de TiTino g.2, punto 1; encargo del Mapa, fila 11): si la
    /// desaconseja, el árbol de la ventanita pasa a una rama genérica sin especie reconocible (o la que proponga el asesor), sin
    /// tocar esta clase (<see cref="ProceduralArt.HomeWindow"/>).</item>
    /// <item>Nunca en pantallas de pago (g.2, regla 9; d.11): quien la usa la oculta con Sin canoas, la hoja de compra o una
    /// oferta delante (<see cref="Apply"/>).</item>
    /// </list>
    /// El texto se arma solo al cambiar de parada (o de idioma, que rehace la pantalla), nunca por fotograma.
    /// </summary>
    internal sealed class HomeWindow
    {
        /// <summary>Lado del marco (dp) [P]: la propuesta del Mapa pide 48-64 dp.</summary>
        public const float SizeDp = ProceduralArt.HomeWindowDp;

        /// <summary>
        /// Lado del retrato dentro del marco (dp) [P]: el mínimo de M1.5, para que la rama y las hojas de la embaúba se vean
        /// alrededor de la cabeza.
        /// </summary>
        public const float PortraitDp = ProceduralArt.SlothPortraitMinDp;

        /// <summary>Alto de la píldora de la distancia (dp).</summary>
        public const float PillHeightDp = 24f;

        /// <summary>Margen del texto a cada lado dentro de la píldora (dp).</summary>
        public const float PillPadDp = 10f;

        /// <summary>Separación entre el marco y la píldora (dp).</summary>
        public const float PillGapDp = 6f;

        /// <summary>Cuerpo del texto de la píldora (sp): el mínimo legible de 02 §12 (D-020, 36 a), en Nunito Bold.</summary>
        public const float LabelSp = 12f;

        /// <summary>Los km de la píldora se redondean a esta decena [P]: «a unos» (propuesta del Mapa §2.2: «≈ 1.480»).</summary>
        public const int KmStep = 10;

        /// <summary>Lado mínimo del área táctil de la ventanita que se toca (07 §1.2 y §7.8).</summary>
        public const float MinTapDp = 48f;

        /// <summary>Con la píldora debajo del marco (<see cref="PillBelow"/>), el hueco entre los dos (dp) [P].</summary>
        public const float PillStackGapDp = 4f;

        /// <summary>
        /// Con la píldora debajo del marco (<see cref="PillBelow"/>), su ancho preferido como mucho (dp) [P]: si el texto no cabe
        /// con su margen, el margen se estrecha hasta <see cref="PillMinPadDp"/>; y si ni así cabe (un idioma largo como en-XA o el
        /// texto grande de 02 §12), la píldora crece lo que pide el texto, hasta el borde derecho que se pasa a <see cref="Place"/>:
        /// el texto, a su cuerpo mínimo de 12 sp, nunca se corta ni se encoge.
        /// </summary>
        public const float PillMaxWidthDp = 150f;

        /// <summary>Margen mínimo del texto dentro de la píldora (dp).</summary>
        public const float PillMinPadDp = 6f;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly Image _window;
        private readonly Image _portrait;
        private readonly RectTransform _pill;
        private readonly TextMeshProUGUI _label;
        private readonly Button? _tap;
        private readonly HomeSloth? _sloth;

        private RiverStop? _stop;
        private bool _hasStop;
        private bool _artReady;
        private float _x;
        private float _y;
        private float _maxRight = LobbyScreen.ReferenceWidth;
        private bool _asleep;
        private Sprite? _portraitPng;
        private bool _provisional;
        private bool _pillBelow;
        private Rect _bounds;

        /// <summary>
        /// Crea la ventanita (oculta) bajo <paramref name="parent"/> con TiTino dentro (la del inicio; <see cref="HomeSloth"/>, TI6c):
        /// sin más toque que el suyo.
        /// </summary>
        public HomeWindow(Transform parent, ProceduralArt art)
            : this(parent, art, null)
        {
            _sloth = new HomeSloth(this, parent, art);
        }

        /// <summary>
        /// Crea la ventanita (oculta) bajo <paramref name="parent"/>; con <paramref name="onTap"/>, el marco y la píldora se tocan
        /// (un área táctil invisible encima de los dos, de ≥ 48 dp de alto; la del Mapa del río, M3.5).
        /// </summary>
        public HomeWindow(Transform parent, ProceduralArt art, Action? onTap)
        {
            _art = art;
            _root = UiBuilder.Rect("HomeWindow", parent);
            _window = UiBuilder.Image("Window", _root, null, Color.white);
            _window.type = Image.Type.Sliced;
            _window.fillCenter = true;
            UiBuilder.Stretch(_window.rectTransform);
            _portrait = UiBuilder.Image("Portrait", _root, null, Color.white);
            _portrait.preserveAspect = true;

            _pill = UiBuilder.Rect("HomeDistance", parent);
            Image plate = UiBuilder.Sliced("Plate", _pill, art.WoodPlate, Color.white);
            UiBuilder.Stretch(plate.rectTransform);
            _label = UiBuilder.Label("Label", _pill, TextStyle.PlainBold, string.Empty, LabelSp, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Stretch(_label.rectTransform);

            if (onTap != null)
            {
                _tap = UiBuilder.BareButton("HomeTap", parent, onTap);
                _tap.gameObject.SetActive(false);
            }

            _root.gameObject.SetActive(false);
            _pill.gameObject.SetActive(false);
        }

        /// <summary>Marco de la ventanita.</summary>
        public RectTransform Root => _root;

        /// <summary>Píldora «Casa · a unos {km} km».</summary>
        public RectTransform PillRect => _pill;

        /// <summary>La ventanita se ve.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>La píldora de la distancia se ve.</summary>
        public bool PillShown => _pill.gameObject.activeSelf;

        /// <summary>
        /// TiTino en la ventanita del inicio (TI6c): pose por la hora, saludos, toque y Dato del perezoso. Null en la del Mapa del río.
        /// </summary>
        public HomeSloth? Sloth => _sloth;

        /// <summary>El retrato del marco (TiTino sin rig; con el rig de <see cref="HomeSloth"/>, oculto).</summary>
        internal Image PortraitImage => _portrait;

        /// <summary>Área táctil del marco y la píldora del Mapa del río (null en la del inicio, en la que toca TiTino).</summary>
        public Button? TapButton => _tap;

        /// <summary>Caja del área táctil (null sin toques).</summary>
        public RectTransform? TapRect => _tap != null ? (RectTransform)_tap.transform : null;

        /// <summary>
        /// La píldora va debajo del marco, alineada a su izquierda, con <see cref="PillStackGapDp"/> de hueco y, si el texto cabe,
        /// como mucho <see cref="PillMaxWidthDp"/> de ancho (la ventanita del Mapa, M3.5); si no, a la derecha del marco (la del
        /// inicio).
        /// </summary>
        public bool PillBelow
        {
            get => _pillBelow;
            set
            {
                if (_pillBelow != value)
                {
                    _pillBelow = value;
                    PlacePill();
                }
            }
        }

        /// <summary>Alto de la ventanita con su píldora (dp): el marco y, con <see cref="PillBelow"/> y píldora, el hueco y la píldora.</summary>
        public float HeightDp => _pillBelow && _label.text.Length > 0 ? SizeDp + PillStackGapDp + PillHeightDp : SizeDp;

        /// <summary>Caja del marco y la píldora juntos, en coordenadas del padre (dp, y hacia abajo), tal como se colocaron.</summary>
        public Rect Bounds => _bounds;

        /// <summary>El retrato tiene los ojos cerrados: TiTino dormita (<see cref="SetAsleep"/>).</summary>
        public bool Asleep => _asleep;

        /// <summary>Texto de la píldora (vacío en casa o sin parada).</summary>
        public string DistanceText => _label.text;

        /// <summary>La ventanita puede verse: tiene parada y arte (lo que <see cref="Apply"/> no oculte).</summary>
        public bool Ready => _stop != null && _artReady;

        /// <summary>Parada que enseña (null sin parada).</summary>
        public RiverStop? Stop => _stop;

        /// <summary>Sprite del marco que se pinta (null si no hay arte que pueda salir).</summary>
        public Sprite? WindowSprite => _window.sprite;

        /// <summary>Sprite del retrato que se pinta (null si no hay arte que pueda salir).</summary>
        public Sprite? PortraitSprite => _portrait.sprite;

        /// <summary>
        /// Km que dice la píldora para <paramref name="kmHome"/>: redondeados a la decena más cercana [P] («a unos 1.480 km» con
        /// los 1.479 de Manaos); un negativo es 0.
        /// </summary>
        public static int DisplayKm(int kmHome)
        {
            if (kmHome <= 0)
            {
                return 0;
            }

            return ((kmHome + (KmStep / 2)) / KmStep) * KmStep;
        }

        /// <summary>La parada lleva píldora: lejos de casa (n ≥ 1 y km &gt; 0). En Belém (n = 0), sin píldora [P].</summary>
        public static bool HasPill(RiverStop stop)
        {
            return stop.N > 0 && stop.KmHome > 0;
        }

        /// <summary>
        /// La regla de P-M5 para la ventanita entera: el marco y el retrato pueden salir (cada uno, con su PNG final o con el
        /// provisional permitido; <see cref="MapArtRule.CanShow"/>).
        /// </summary>
        public static bool CanShow(bool showProvisional, bool windowPng, bool portraitPng)
        {
            return MapArtRule.CanShow(showProvisional, windowPng) && MapArtRule.CanShow(showProvisional, portraitPng);
        }

        /// <summary>
        /// Enseña la parada <paramref name="stop"/> (null = sin parada: no se ve) o la oculta con <paramref name="hidden"/> (Sin
        /// canoas, la hoja de compra o una oferta delante). El texto y el arte se arman solo si cambia la parada.
        /// </summary>
        public void Apply(RiverStop? stop, bool hidden)
        {
            if (!_hasStop || !ReferenceEquals(stop, _stop))
            {
                _hasStop = true;
                _stop = stop;
                Build(stop);
            }

            bool on = _stop != null && _artReady && !hidden;
            if (_root.gameObject.activeSelf != on)
            {
                _root.gameObject.SetActive(on);
            }

            bool pill = on && _stop != null && HasPill(_stop);
            if (_pill.gameObject.activeSelf != pill)
            {
                _pill.gameObject.SetActive(pill);
            }

            if (_tap != null && _tap.gameObject.activeSelf != on)
            {
                _tap.gameObject.SetActive(on);
            }

            _sloth?.SetShown(on);
        }

        /// <summary>
        /// Abre o cierra los ojos del retrato (<c>sloth_sleep</c> en el Mapa, M3.5): el retrato de M1.5 con los ojos cerrados. Solo
        /// cambia el sprite si cambia el estado; los dos retratos se crean una vez y se guardan, así que no asigna memoria después.
        /// </summary>
        public void SetAsleep(bool asleep)
        {
            if (_asleep == asleep)
            {
                return;
            }

            _asleep = asleep;
            if (_artReady)
            {
                _portrait.sprite = _art.SlothPortrait(asleep, _portraitPng, _provisional);
            }
        }

        /// <summary>
        /// Coloca el marco con su esquina superior izquierda en (<paramref name="x"/>, <paramref name="yTop"/>) dp y la píldora sin
        /// pasar de <paramref name="maxRight"/>: con <see cref="PillBelow"/>, debajo del marco y alineada a su izquierda (con el
        /// ancho de <see cref="PillMaxWidthDp"/>); si no, a su derecha, centrada en el alto del marco.
        /// </summary>
        public void Place(float x, float yTop, float maxRight)
        {
            _x = x;
            _y = yTop;
            _maxRight = maxRight;
            UiBuilder.Place(_root, x, yTop, SizeDp, SizeDp);
            float inset = (SizeDp - PortraitDp) * 0.5f;

            // El retrato cuelga bajo la rama de la embaúba: apoyado en el borde de abajo del hueco.
            UiBuilder.Place(_portrait.rectTransform, inset, SizeDp - ProceduralArt.HomeWindowBorderDp - PortraitDp, PortraitDp, PortraitDp);
            PlacePill();
            _sloth?.Layout(x, yTop, maxRight);
        }

        private void Build(RiverStop? stop)
        {
            if (stop == null)
            {
                _artReady = false;
                _label.text = string.Empty;
                return;
            }

            Sprite? windowPng = ArtSlots.Find(ProceduralArt.HomeWindowSlot);
            Sprite? portraitPng = ArtSlots.Find(ProceduralArt.SlothPortraitSlot);
            bool provisional = MapArtRule.ShowProvisional;
            _portraitPng = portraitPng;
            _provisional = provisional;
            _artReady = CanShow(provisional, windowPng != null, portraitPng != null);
            if (_artReady)
            {
                _window.sprite = _art.HomeWindowFor(windowPng, provisional);
                _portrait.sprite = _art.SlothPortrait(_asleep, portraitPng, provisional);
                _artReady = _window.sprite != null && _portrait.sprite != null;
            }

            _label.text = HasPill(stop) ? UiText.MapHomeDistance(DisplayKm(stop.KmHome)) : string.Empty;
            PlacePill();
        }

        private void PlacePill()
        {
            float text = string.IsNullOrEmpty(_label.text) ? 0f : Mathf.Ceil(UiBuilder.MinTextWidth(_label));
            if (_pillBelow)
            {
                // Debajo del marco, alineada a su izquierda: el texto con su margen, hasta PillMaxWidthDp estrechando el margen hasta
                // PillMinPadDp; un texto más largo hace crecer la píldora (nunca se corta), sin pasar de _maxRight.
                float w = text <= 0f ? 0f : Mathf.Min(text + (2f * PillPadDp), Mathf.Max(PillMaxWidthDp, text + (2f * PillMinPadDp)));
                w = Mathf.Max(0f, Mathf.Min(w, _maxRight - _x));
                float top = _y + SizeDp + PillStackGapDp;
                UiBuilder.Place(_pill, _x, top, w, PillHeightDp);
                float h = w > 0f ? SizeDp + PillStackGapDp + PillHeightDp : SizeDp;
                _bounds = new Rect(_x, _y, Mathf.Max(SizeDp, w), h);
                if (_tap != null)
                {
                    float tapW = Mathf.Max(_bounds.width, MinTapDp);
                    float tapH = Mathf.Max(h, MinTapDp);
                    UiBuilder.Place((RectTransform)_tap.transform, _x, _y + ((h - tapH) * 0.5f), tapW, tapH);
                }

                return;
            }

            float left = _x + SizeDp + PillGapDp;
            float width = text <= 0f ? 0f : text + (2f * PillPadDp);
            width = Mathf.Max(0f, Mathf.Min(width, _maxRight - left));
            UiBuilder.Place(_pill, left, _y + ((SizeDp - PillHeightDp) * 0.5f), width, PillHeightDp);
            _bounds = new Rect(_x, _y, SizeDp + (width > 0f ? PillGapDp + width : 0f), SizeDp);
            if (_tap != null)
            {
                // El área táctil cubre el marco y la píldora, con el alto mínimo de un control (07 §1.2 y §7.8).
                float tapH = Mathf.Max(SizeDp, MinTapDp);
                float tapW = SizeDp + (width > 0f ? PillGapDp + width : 0f);
                UiBuilder.Place((RectTransform)_tap.transform, _x, _y + ((SizeDp - tapH) * 0.5f), Mathf.Max(tapW, MinTapDp), tapH);
            }
        }
    }
}
