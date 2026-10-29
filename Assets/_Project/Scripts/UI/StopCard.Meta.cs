#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Lo que B14 añade a la tarjeta de la parada (06 §4.5; tarea B14 de la tanda 5), en una parcial de <see cref="StopCard"/>:
    /// <list type="bullet">
    /// <item><description><b>La Canasta de una parada futura</b> (06 §4.5: «cada parada futura enseña el contenido de su Canasta»;
    /// 06 §4.4: «determinista por nivel y visible antes de abrir»): bajo «Nivel n» y el fondo en silueta, «Canasta»
    /// (<c>map_card_basket</c> [P]) y lo que trae la del <c>10k</c> con que se llega (<see cref="BasketRules.For"/>): las semillas y
    /// cada comodín con su icono y su cantidad (<see cref="Loc.Number"/>). Es contenido, no una oferta: sin precio ni botón (Q10-A).
    /// </description></item>
    /// <item><description><b>«Premio del mundo · faltan N»</b> (06 §4.5, cierre de mundo: «la skin del mundo como Premio del mundo ·
    /// faltan N, sin precio»; <c>map_world_prize</c> [P]): en la tarjeta del cierre de un mundo con skin aún sin ganar, la mini cuia
    /// pintada con la skin (<see cref="CuiaSkin"/>) y la línea.</description></item>
    /// <item><description><b>Un punto de paso</b> (06 §4.5: «puntos de paso con nombre … que al tocarlos cuentan su dato»;
    /// <see cref="StopCardMode.Passing"/>): su nombre (<c>map.stop.&lt;id&gt;.name</c>), su dato y su fuente, si tienen texto, sin
    /// fondo ni mini cuia, como un mirador.</description></item>
    /// </list>
    /// Todo texto sale de las tablas; sin precios ni ofertas (Q10-A). Se compone al abrir, nunca por fotograma.
    /// </summary>
    public sealed partial class StopCard
    {
        /// <summary>Lado del icono de cada cosa de la Canasta (dp).</summary>
        public const float BasketIconDp = 24f;

        /// <summary>Cantidad junto al icono de la Canasta (sp).</summary>
        public const float BasketCountSp = 15f;

        /// <summary>Ancho de la caja de la cuia del Premio del mundo (dp; el alto, el de la mini cuia del Mapa).</summary>
        public const float PrizeCuiaDp = 20f;

        private const int MaxBasketItems = 6;

        /// <summary>La cuia del Premio del mundo (tests: la miniatura de la skin).</summary>
        public Image PrizeCuiaImage => _prizeCuia;

        private readonly List<Image> _basketIcons = new List<Image>();
        private readonly List<TextMeshProUGUI> _basketCounts = new List<TextMeshProUGUI>();
        private TextMeshProUGUI _basketTitle = null!;
        private TextMeshProUGUI _prize = null!;
        private Image _prizeCuia = null!;
        private BasketContents _basket;
        private int _basketItems;
        private string _prizeText = string.Empty;
        private CuiaSkin? _prizeSkin;
        private RiverPassingPoint? _passing;

        /// <summary>La Canasta que enseña la tarjeta de una parada futura (<see cref="BasketContents.None"/> si no la enseña).</summary>
        public BasketContents Basket => BasketShown ? _basket : BasketContents.None;

        /// <summary>Se ve la fila de la Canasta.</summary>
        public bool BasketShown => _basketTitle.gameObject.activeSelf;

        /// <summary>Cosas de la fila de la Canasta (las semillas y cada comodín).</summary>
        public int BasketItemCount => BasketShown ? _basketItems : 0;

        /// <summary>Icono de la cosa <paramref name="i"/> de la Canasta.</summary>
        public Sprite? BasketItemIcon(int i)
        {
            return _basketIcons[i].sprite;
        }

        /// <summary>Cantidad de la cosa <paramref name="i"/> de la Canasta.</summary>
        public string BasketItemText(int i)
        {
            return _basketCounts[i].text;
        }

        /// <summary>«Premio del mundo · faltan N» (vacío si no se ve).</summary>
        public string PrizeText => _prize.gameObject.activeSelf ? _prize.text : string.Empty;

        /// <summary>La skin del Premio del mundo que enseña (null sin premio).</summary>
        public CuiaSkin? PrizeSkin => _prize.gameObject.activeSelf ? _prizeSkin : null;

        /// <summary>Punto de paso que enseña (null en los demás modos).</summary>
        public RiverPassingPoint? Passing => _passing;

        /// <summary>
        /// Abre la tarjeta del punto de paso <paramref name="point"/> (ya pasado): su nombre, su dato y su fuente, si tienen texto.
        /// </summary>
        public void ShowPassing(RiverPassingPoint point)
        {
            _passing = point ?? throw new ArgumentNullException(nameof(point));
            _stop = null;
            _viewpoint = null;
            _mode = StopCardMode.Passing;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            _title.text = Loc.Fact(point.NameKey);
            _factKeys.Clear();
            if (point.Fact != null && Loc.Fact(point.Fact).Length > 0)
            {
                _factKeys.Add(point.Fact);
            }

            _species = null;
            _tints.Clear();
            _futureLevel.text = string.Empty;
            _factIndex = 0;
            SetMeta(BasketContents.None, null, null);
            ApplyContent();
            ApplyFutureTone(false);
        }

        // La fila de la Canasta y la del Premio del mundo (al construir la tarjeta).
        private void BuildMeta()
        {
            _basketTitle = UiBuilder.Label("BasketTitle", _panel.transform, TextStyle.PlainBold, string.Empty, RowSp, Palette.Cream, TextAlignmentOptions.Left);
            UiBuilder.AutoSize(_basketTitle, UiBuilder.MinTextSp, RowSp);
            for (int i = 0; i < MaxBasketItems; i++)
            {
                Image icon = UiBuilder.Image("BasketIcon" + i, _panel.transform, null, Color.white);
                icon.preserveAspect = true;
                TextMeshProUGUI count = UiBuilder.Label("BasketCount" + i, _panel.transform, TextStyle.PlainBold, string.Empty, BasketCountSp, Palette.Cream, TextAlignmentOptions.Left);
                _basketIcons.Add(icon);
                _basketCounts.Add(count);
            }

            _prizeCuia = UiBuilder.Image("PrizeCuia", _panel.transform, _art.MapMiniCuia, Color.white);
            _prizeCuia.preserveAspect = true;
            _prize = UiBuilder.Label("Prize", _panel.transform, TextStyle.PlainBold, string.Empty, RowSp, Palette.Cream, TextAlignmentOptions.Left);
            _prize.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_prize, UiBuilder.MinTextSp, RowSp);
            SetMeta(BasketContents.None, null, null);
        }

        // Lo que enseñará la tarjeta: la Canasta y el premio (solo en una parada futura; null o None = no).
        private void SetMeta(BasketContents basket, string? prize, CuiaSkin? prizeSkin)
        {
            _basket = basket;
            _prizeText = prize ?? string.Empty;
            _prizeSkin = prizeSkin;
            if (_mode != StopCardMode.Passing)
            {
                _passing = null;
            }
        }

        // Pinta las filas (desde ApplyContent): la Canasta con sus cosas en el orden de la Cosecha (semillas y comodines en el orden
        // de BoosterKind) y el premio con la cuia de su skin.
        private void ApplyMeta()
        {
            bool future = _mode == StopCardMode.Future;
            _basketItems = 0;
            string title = future && !_basket.IsNone ? UiText.MapCardBasket : string.Empty;
            if (title.Length > 0)
            {
                if (_basket.Seeds > 0)
                {
                    SetBasketItem(_art.IconSeed, _basket.Seeds);
                }

                for (int k = 0; k < BoosterKinds.Count; k++)
                {
                    int units = _basket.Stock.Get((BoosterKind)k);
                    Sprite? icon = BoosterIcon((BoosterKind)k);
                    if (units > 0 && icon != null)
                    {
                        SetBasketItem(icon, units);
                    }
                }
            }

            _basketTitle.text = title;
            _basketTitle.gameObject.SetActive(title.Length > 0 && _basketItems > 0);
            for (int i = 0; i < MaxBasketItems; i++)
            {
                bool on = _basketTitle.gameObject.activeSelf && i < _basketItems;
                _basketIcons[i].gameObject.SetActive(on);
                _basketCounts[i].gameObject.SetActive(on);
            }

            bool prize = future && _prizeText.Length > 0;
            _prize.text = prize ? _prizeText : string.Empty;
            _prize.gameObject.SetActive(prize);
            _prizeCuia.gameObject.SetActive(prize);
            if (prize && _prizeSkin != null)
            {
                // Observación 4 de la jornada 6: el premio es la cuia con su skin, la misma miniatura que el Mercado
                // (ProceduralArt.SkinThumb), no la mini cuia teñida; lleva sus colores (sin tinte).
                _prizeCuia.sprite = _art.SkinThumb(_prizeSkin);
                _prizeCuia.color = Color.white;
            }
        }

        private void SetBasketItem(Sprite icon, int units)
        {
            if (_basketItems >= MaxBasketItems)
            {
                return;
            }

            _basketIcons[_basketItems].sprite = icon;
            _basketCounts[_basketItems].text = Loc.Number(units);
            _basketItems++;
        }

        private Sprite? BoosterIcon(BoosterKind kind)
        {
            switch (kind)
            {
                case BoosterKind.Undo: return _art.IconUndo;
                case BoosterKind.Hint: return _art.IconMorpho;
                case BoosterKind.Reveal: return _art.IconFirefly;
                case BoosterKind.ExtraCuia: return _art.IconCuiaPlus;
                default: return null; // la Pororoca no va en ninguna Canasta de la 1.0
            }
        }

        // Alto de las filas de B14 en la placa (0 si no se ven).
        private float MetaHeight(float inner)
        {
            float h = 0f;
            if (BasketShown)
            {
                h += Mathf.Max(BasketIconDp, Mathf.Ceil(UiBuilder.MinTextHeight(_basketTitle, inner))) + 6f + BasketRowHeight(inner) + GapDp;
            }

            if (_prize.gameObject.activeSelf)
            {
                h += PrizeRowHeight(inner) + GapDp;
            }

            return h;
        }

        // Coloca las filas desde y y devuelve dónde acaban.
        private float LayoutMeta(float y, float inner)
        {
            if (BasketShown)
            {
                float th = Mathf.Max(BasketIconDp, Mathf.Ceil(UiBuilder.MinTextHeight(_basketTitle, inner)));
                UiBuilder.Place(_basketTitle.rectTransform, PadDp, y, inner, th);
                y += th + 6f;
                float x = PadDp;
                float rowY = y;
                for (int i = 0; i < _basketItems; i++)
                {
                    float w = BasketItemWidth(i);
                    if (x > PadDp && x + w > PadDp + inner)
                    {
                        x = PadDp;
                        rowY += BasketIconDp + 6f;
                    }

                    UiBuilder.Place(_basketIcons[i].rectTransform, x, rowY, BasketIconDp, BasketIconDp);
                    UiBuilder.Place(_basketCounts[i].rectTransform, x + BasketIconDp + 4f, rowY, w - BasketIconDp - 4f, BasketIconDp);
                    x += w + 14f;
                }

                y += BasketRowHeight(inner) + GapDp;
            }

            if (_prize.gameObject.activeSelf)
            {
                float h = PrizeRowHeight(inner);
                float cuiaH = PrizeCuiaDp * (ProceduralArt.MiniCuiaHeightDp / ProceduralArt.MiniCuiaWidthDp);
                UiBuilder.Place(_prizeCuia.rectTransform, PadDp, y + Mathf.Max(0f, (h - cuiaH) * 0.5f), PrizeCuiaDp, cuiaH);
                float tx = PadDp + PrizeCuiaDp + 8f;
                UiBuilder.Place(_prize.rectTransform, tx, y, inner - (tx - PadDp), h);
                y += h + GapDp;
            }

            return y;
        }

        private float BasketRowHeight(float inner)
        {
            float x = 0f;
            int rows = 1;
            for (int i = 0; i < _basketItems; i++)
            {
                float w = BasketItemWidth(i);
                if (x > 0f && x + w > inner)
                {
                    rows++;
                    x = 0f;
                }

                x += w + 14f;
            }

            return (rows * BasketIconDp) + ((rows - 1) * 6f);
        }

        private float BasketItemWidth(int i)
        {
            return BasketIconDp + 4f + Mathf.Ceil(UiBuilder.MinTextWidth(_basketCounts[i]));
        }

        private float PrizeRowHeight(float inner)
        {
            float cuiaH = PrizeCuiaDp * (ProceduralArt.MiniCuiaHeightDp / ProceduralArt.MiniCuiaWidthDp);
            float textW = inner - PrizeCuiaDp - 8f;
            return Mathf.Max(cuiaH, Mathf.Ceil(UiBuilder.MinTextHeight(_prize, textW)) + 2f);
        }

        private IEnumerable<TextMeshProUGUI> MetaTexts()
        {
            if (BasketShown)
            {
                yield return _basketTitle;
                for (int i = 0; i < _basketItems; i++)
                {
                    yield return _basketCounts[i];
                }
            }

            if (_prize.gameObject.activeSelf)
            {
                yield return _prize;
            }
        }
    }
}
