#nullable enable
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
    /// <item>Sin nombre, sin texto en el marco y sin toques: todo con <c>raycastTarget</c> = false hasta M3.3, que la hará abrir
    /// el Mapa.</item>
    /// <item>Arte provisional de P-M5 (<see cref="MapArtRule"/>): sin los PNG de <c>map_home_window</c> y del retrato, solo sale
    /// en el Editor y en desarrollo (<see cref="CanShow"/>); en release, nada hasta que exista su arte.</item>
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

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly Image _window;
        private readonly Image _portrait;
        private readonly RectTransform _pill;
        private readonly TextMeshProUGUI _label;

        private RiverStop? _stop;
        private bool _hasStop;
        private bool _artReady;
        private float _x;
        private float _y;
        private float _maxRight = LobbyScreen.ReferenceWidth;

        /// <summary>Crea la ventanita (oculta) bajo <paramref name="parent"/>.</summary>
        public HomeWindow(Transform parent, ProceduralArt art)
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

        /// <summary>Texto de la píldora (vacío en casa o sin parada).</summary>
        public string DistanceText => _label.text;

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
        }

        /// <summary>
        /// Coloca el marco con su esquina superior izquierda en (<paramref name="x"/>, <paramref name="yTop"/>) dp y la píldora a
        /// su derecha, centrada en el alto del marco y sin pasar de <paramref name="maxRight"/>.
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
            _artReady = CanShow(provisional, windowPng != null, portraitPng != null);
            if (_artReady)
            {
                _window.sprite = _art.HomeWindowFor(windowPng, provisional);
                _portrait.sprite = _art.SlothPortrait(false, portraitPng, provisional);
                _artReady = _window.sprite != null && _portrait.sprite != null;
            }

            _label.text = HasPill(stop) ? UiText.MapHomeDistance(DisplayKm(stop.KmHome)) : string.Empty;
            PlacePill();
        }

        private void PlacePill()
        {
            float left = _x + SizeDp + PillGapDp;
            float width = string.IsNullOrEmpty(_label.text) ? 0f : Mathf.Ceil(UiBuilder.MinTextWidth(_label)) + (2f * PillPadDp);
            width = Mathf.Max(0f, Mathf.Min(width, _maxRight - left));
            UiBuilder.Place(_pill, left, _y + ((SizeDp - PillHeightDp) * 0.5f), width, PillHeightDp);
        }
    }
}
