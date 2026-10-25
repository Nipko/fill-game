#nullable enable
using System;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Una tarjeta del día en el muelle del inicio (D-050, punto 6; 06 §4.7, fila Lobby: «tarjetas Reto del río y Bitácora»): una
    /// placa de madera oscura como los contadores (<see cref="ProceduralArt.WoodPlate"/>) con su icono en un disco que asoma por
    /// arriba, el título en crema (hasta dos líneas), el estado debajo con su color y, si toca, una cifra en una píldora en la
    /// esquina («0/7» del Reto), un punto de aviso (Bitácora por sellar) o el sello (Reto sellado). La tarjeta entera es el área
    /// táctil (≥ 48 dp, 07 §7.8) y TalkBack la lee con la etiqueta que le dé quien la usa. La usan
    /// <see cref="DailyChallengeCard"/>, <see cref="LogbookCard"/> y <see cref="LeagueLobbyButton"/>; las coloca
    /// <c>GameBootstrap.PlaceDayCards</c> en la fila que deja <see cref="LobbyScreen"/> bajo «Jugar». Sin asignaciones mientras nada
    /// cambia; los textos se miden al colocarla.
    /// </summary>
    internal sealed class DockCard
    {
        /// <summary>Lado del disco del icono (dp).</summary>
        public const float IconDp = ProceduralArt.CardIconDp;

        /// <summary>Lo que el disco del icono asoma por encima de la placa (dp).</summary>
        public const float IconOverhangDp = 12f;

        /// <summary>Cuerpo del título (sp), en Nunito ExtraBold con contorno de madera.</summary>
        public const float TitleSp = 13f;

        /// <summary>Cuerpo del estado (sp), en Nunito Bold.</summary>
        public const float StateSp = 12f;

        /// <summary>Cuerpo de la cifra de la esquina (sp).</summary>
        public const float BadgeSp = 12f;

        /// <summary>Margen del texto a cada lado (dp).</summary>
        public const float PadXDp = 7f;

        /// <summary>Aire bajo el texto, dentro de la placa (dp).</summary>
        public const float BottomPadDp = 6f;

        /// <summary>Diámetro del punto de aviso (dp), el de la pestaña del Mapa.</summary>
        public const float DotDp = 14f;

        private readonly RectTransform _root;
        private readonly Button _button;
        private readonly Image _plate;
        private readonly Image _icon;
        private readonly RectTransform _iconExtras;
        private readonly TextMeshProUGUI _title;
        private readonly TextMeshProUGUI _state;
        private readonly RectTransform _badge;
        private readonly TextMeshProUGUI _badgeText;
        private readonly RectTransform _dot;
        private readonly Image _stamp;
        private float _w = 104f;
        private float _h = 80f;
        private bool _compact;

        public DockCard(Transform parent, ProceduralArt art, string name, Sprite icon, Action onTap)
        {
            _root = UiBuilder.Rect(name, parent);
            _plate = UiBuilder.Sliced("Plate", _root, art.WoodPlate, Color.white);
            UiBuilder.Stretch(_plate.rectTransform);
            _icon = UiBuilder.Image("Icon", _root, icon, Color.white);
            _icon.preserveAspect = true;
            _iconExtras = UiBuilder.Rect("IconExtras", _icon.transform);
            UiBuilder.Stretch(_iconExtras);
            _title = UiBuilder.Label("Title", _root, TextStyle.PillWood, string.Empty, TitleSp, Palette.Cream, TextAlignmentOptions.Top);
            _title.textWrappingMode = TextWrappingModes.Normal;
            _state = UiBuilder.Label("State", _root, TextStyle.PlainBold, string.Empty, StateSp, Palette.LeafShine, TextAlignmentOptions.Top);
            _state.textWrappingMode = TextWrappingModes.Normal;

            _badge = UiBuilder.Rect("Badge", _root);
            Image badgeEdge = UiBuilder.Sliced("Edge", _badge, art.RoundedRect, Palette.FamilyWood.Edge);
            badgeEdge.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / 9f;
            UiBuilder.Stretch(badgeEdge.rectTransform);
            Image badgeFace = UiBuilder.Sliced("Face", _badge, art.RoundedRect, Palette.PanelLight);
            badgeFace.pixelsPerUnitMultiplier = ProceduralArt.RoundedRectRadiusPx / 8f;
            UiBuilder.Stretch(badgeFace.rectTransform);
            badgeFace.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            badgeFace.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            _badgeText = UiBuilder.Label("Text", _badge, TextStyle.PlainBold, string.Empty, BadgeSp, Palette.UiInk, TextAlignmentOptions.Center);
            UiBuilder.Stretch(_badgeText.rectTransform);
            _badge.gameObject.SetActive(false);

            _dot = UiBuilder.Rect("ReadyDot", _root);
            Image rim = UiBuilder.Image("Rim", _dot, art.Disc, Palette.Cream);
            UiBuilder.Stretch(rim.rectTransform);
            Image dot = UiBuilder.Image("Dot", _dot, art.Disc, Palette.Heliconia);
            UiBuilder.Place(dot.rectTransform, 2f, 2f, DotDp - 4f, DotDp - 4f);
            _dot.gameObject.SetActive(false);

            _stamp = UiBuilder.Image("Stamp", _icon.transform, art.Disc, DailyStampCalendar.StampColor);
            Image ring = UiBuilder.Image("Ring", _stamp.transform, art.Ring, Palette.Cream);
            UiBuilder.Stretch(ring.rectTransform);
            _stamp.gameObject.SetActive(false);

            _button = UiBuilder.BareButton("Tap", _root, onTap);
            UiBuilder.Stretch((RectTransform)_button.transform);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Raíz (la placa; el icono asoma <see cref="IconOverhangDp"/> por encima).</summary>
        public RectTransform Root => _root;

        /// <summary>Área táctil (la tarjeta entera).</summary>
        public Button Button => _button;

        /// <summary>El disco del icono (para añadirle piezas: las olas del Reto).</summary>
        public RectTransform IconExtras => _iconExtras;

        /// <summary>Título.</summary>
        public TextMeshProUGUI Title => _title;

        /// <summary>Estado.</summary>
        public TextMeshProUGUI State => _state;

        /// <summary>Cifra de la esquina.</summary>
        public TextMeshProUGUI BadgeText => _badgeText;

        /// <summary>Se ve el punto de aviso.</summary>
        public bool DotShown => _dot.gameObject.activeSelf;

        /// <summary>Se ve el sello.</summary>
        public bool StampShown => _stamp.gameObject.activeSelf;

        /// <summary>Se ve.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Ancho con el que está colocada (dp).</summary>
        public float Width => _w;

        /// <summary>Alto con el que está colocada (dp).</summary>
        public float Height => _h;

        /// <summary>Pone los textos (asigna cadenas: al cambiar el estado, nunca por fotograma) y el color del estado.</summary>
        public void SetTexts(string title, string state, Color stateColor)
        {
            _title.text = title;
            _state.text = state;
            _state.gameObject.SetActive(state.Length > 0 && !_compact);
            if (_state.color != stateColor)
            {
                _state.color = stateColor;
            }
        }

        /// <summary>
        /// Compacta: sin la línea de estado (la fila de tarjetas no cabe entre «Jugar» y las pestañas: 16:9 con el texto grande). El
        /// estado sigue en el icono (el sello, el punto) y en lo que lee TalkBack.
        /// </summary>
        public bool Compact => _compact;

        /// <summary>Quita o devuelve la línea de estado (<see cref="Compact"/>).</summary>
        public void SetCompact(bool on)
        {
            _compact = on;
            bool state = !on && _state.text.Length > 0;
            if (_state.gameObject.activeSelf != state)
            {
                _state.gameObject.SetActive(state);
            }
        }

        /// <summary>La cifra de la esquina (vacía = sin píldora).</summary>
        public void SetBadge(string text)
        {
            _badgeText.text = text;
            _badge.gameObject.SetActive(text.Length > 0);
        }

        /// <summary>El punto de aviso.</summary>
        public void SetDot(bool on)
        {
            if (_dot.gameObject.activeSelf != on)
            {
                _dot.gameObject.SetActive(on);
            }
        }

        /// <summary>El sello (con <paramref name="alpha"/> &lt; 1, pendiente de validar).</summary>
        public void SetStamp(bool on, float alpha)
        {
            _stamp.gameObject.SetActive(on);
            Color c = DailyStampCalendar.StampColor;
            c.a = alpha;
            _stamp.color = c;
        }

        /// <summary>Muestra u oculta la tarjeta.</summary>
        public void SetShown(bool on)
        {
            if (_root.gameObject.activeSelf != on)
            {
                _root.gameObject.SetActive(on);
            }
        }

        /// <summary>
        /// Alto que pide la tarjeta a <paramref name="width"/> dp con sus textos: el disco que asoma, el título y el estado en las
        /// líneas que necesiten a su cuerpo (nunca se encogen, 02 §12) y el aire de abajo.
        /// </summary>
        public float HeightFor(float width)
        {
            float inner = Inner(width);
            float titleH = string.IsNullOrEmpty(_title.text) ? 0f : Mathf.Ceil(UiBuilder.MinTextHeight(_title, inner));
            float stateH = string.IsNullOrEmpty(_state.text) || _compact ? 0f : Mathf.Ceil(UiBuilder.MinTextHeight(_state, inner));
            return TopPad() + titleH + (stateH > 0f ? 1f + stateH : 0f) + BottomPadDp;
        }

        // Ancho del texto: con la tarjeta estrecha (3 en 320 dp), margen de 4 dp en lugar de 7.
        private static float Inner(float w)
        {
            float pad = w < 100f ? 4f : PadXDp;
            return Mathf.Max(20f, w - (2f * pad));
        }

        // Lo que ocupa dentro de la placa el disco del icono, más 2 dp de aire.
        private static float TopPad()
        {
            return IconDp - IconOverhangDp + 2f;
        }

        /// <summary>Coloca la placa en (<paramref name="x"/>, <paramref name="y"/>) dp con <paramref name="w"/> × <paramref name="h"/>.</summary>
        public void Place(float x, float y, float w, float h)
        {
            _w = w;
            _h = h;
            UiBuilder.Place(_root, x, y, w, h);
            UiBuilder.Place(_icon.rectTransform, (w - IconDp) * 0.5f, -IconOverhangDp, IconDp, IconDp);
            float inner = Inner(w);
            float titleH = string.IsNullOrEmpty(_title.text) ? 0f : Mathf.Ceil(UiBuilder.MinTextHeight(_title, inner));
            float stateH = string.IsNullOrEmpty(_state.text) || _compact ? 0f : Mathf.Ceil(UiBuilder.MinTextHeight(_state, inner));
            float content = titleH + (stateH > 0f ? 1f + stateH : 0f);
            float top = TopPad() + Mathf.Max(0f, Mathf.Floor((h - TopPad() - BottomPadDp - content) * 0.5f));
            float padX = (w - inner) * 0.5f;
            UiBuilder.Place(_title.rectTransform, padX, top, inner, titleH);
            UiBuilder.Place(_state.rectTransform, padX, top + titleH + 1f, inner, stateH);
            float s = Mathf.Max(1f, UiBuilder.TextScale);
            float badgeH = Mathf.Ceil(18f * s);
            float badgeW = Mathf.Max(badgeH + 6f, Mathf.Ceil(UiBuilder.MinTextWidth(_badgeText)) + 10f);
            UiBuilder.Place(_badge, w - badgeW + 4f, -6f, badgeW, badgeH);
            UiBuilder.Place(_dot, w - DotDp + 3f, -4f, DotDp, DotDp);
            UiBuilder.Place(_stamp.rectTransform, IconDp - 15f, IconDp - 15f, 17f, 17f);
        }
    }
}
