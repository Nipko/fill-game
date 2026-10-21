#nullable enable
using System;
using AF.IAP;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Lienzo propio por encima de las pantallas de nivel y de inicio (anuncio y compra simulados), a 360 dp de ancho y al menos
    /// 640 de alto, como la pantalla de inicio.
    /// </summary>
    internal sealed class OverlayCanvas
    {
        private readonly CanvasScaler _scaler;
        private int _screenW;
        private int _screenH;
        private Rect _safe;

        public OverlayCanvas(string name, Transform host, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(host, false);
            Canvas = go.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.overrideSorting = true;
            Canvas.sortingOrder = sortingOrder;
            Canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            _scaler = go.GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _scaler.referencePixelsPerUnit = 100f;
            Root = (RectTransform)go.transform;
            Measure();
        }

        public Canvas Canvas { get; }

        public RectTransform Root { get; }

        public float Width { get; private set; } = 360f;

        public float Height { get; private set; } = 780f;

        /// <summary>Margen superior del safe area, en unidades del lienzo (08 §13.1 #4).</summary>
        public float SafeTop { get; private set; }

        /// <summary>Margen inferior del safe area (barra de navegación o gestos), en unidades del lienzo (08 §13.1 #4).</summary>
        public float SafeBottom { get; private set; }

        /// <summary>Recalcula la escala si cambió la pantalla; devuelve true si cambió.</summary>
        public bool Measure()
        {
            GameScreen.ReadScreen(out int sw, out int sh, out Rect safe);
            if (sw == _screenW && sh == _screenH && safe == _safe)
            {
                return false;
            }

            _screenW = sw;
            _screenH = sh;
            _safe = safe;
            float scale = sw / 360f;
            if (sh / scale < 640f)
            {
                scale = sh / 640f;
            }

            scale = Mathf.Max(0.01f, scale);
            _scaler.scaleFactor = scale;
            Width = sw / scale;
            Height = sh / scale;
            SafeTop = Mathf.Max(0f, (sh - safe.yMax) / scale);
            SafeBottom = Mathf.Max(0f, safe.yMin / scale);
            return true;
        }
    }

    /// <summary>
    /// Hoja «Compra simulada» (tareas B1-4 y B2-2; D-008 punto 5: compras simuladas detrás de <c>AF.IAP</c>, sin Play
    /// Billing): rótulo, producto y precio local (05 §4.2), con «Comprar» y «Cancelar». El rótulo y el nombre del producto
    /// salen de la tabla de textos por sus claves (<see cref="SimulatedPurchase.LabelKey"/> y <see cref="SimulatedPurchase.NameKey"/>,
    /// <c>iap_&lt;sku&gt;_name</c>; D-018, tarea L3); el precio, tal como lo da la tienda. Implementa
    /// <see cref="ISimulatedPurchasePresenter"/>: llama una sola vez a la respuesta. Hoja inferior sobre un velo, en su propio
    /// lienzo por encima de todo.
    /// </summary>
    internal sealed class StoreSheet : ISimulatedPurchasePresenter
    {
        private readonly ProceduralArt _art;
        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly TextMeshProUGUI _name;
        private readonly TextMeshProUGUI _price;
        private readonly ReliefParts _buy;
        private readonly Button _cancel;
        private Action<bool>? _onClosed;

        public StoreSheet(Transform host, ProceduralArt art)
        {
            _art = art;
            _canvas = new OverlayCanvas("StoreSheetCanvas", host, 50);
            ModalKit.Dim("Dim", _canvas.Root, 0.6f);
            _panel = ModalKit.Card("Panel", _canvas.Root, art, out _surface);
            _title = ModalKit.Header(_panel, art, UiText.SimulatedPurchaseTitle, out _header);
            _name = ModalKit.Line("Name", _panel, string.Empty, 18f, Palette.FamilyWood.Base);
            _price = ModalKit.Line("Price", _panel, string.Empty, 28f, Palette.Ink, TextStyle.PlainBold);
            _price.color = Palette.FamilyWood.Deep;
            _buy = ModalKit.Cta("BuyButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, UiText.Buy, 22f, () => Close(true), out _);
            _cancel = ModalKit.Link("CancelButton", _panel, UiText.Cancel, () => Close(false), out _);
            _canvas.Canvas.gameObject.SetActive(false);
        }

        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Lienzo propio (capturas de prueba).</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>Rótulo de la cabecera («Compra simulada», de <see cref="SimulatedPurchase.LabelKey"/>).</summary>
        public string TitleText => _title.text;

        public string ProductText => _name.text;

        public string PriceText => _price.text;

        public Button BuyButton => _buy.Button;

        public Button CancelButton => _cancel;

        /// <inheritdoc />
        public void Present(SimulatedPurchase purchase, Action<bool> onClosed)
        {
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.StoreOpen); // AU1
            _onClosed = onClosed;
            _canvas.Canvas.gameObject.SetActive(true);
            _canvas.Measure();
            _name.text = Loc.T(purchase.NameKey); // el nombre de tienda, de strings.csv (iap_<sku>_name; tarea L3)
            _price.text = purchase.PriceText;
            float w = Mathf.Min(360f, _canvas.Width);
            float inner = w - 32f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner, 40f);
            y += 50f;
            UiBuilder.Place(_name.rectTransform, 16f, y, inner, 44f);
            y += 48f;
            UiBuilder.Place(_price.rectTransform, 16f, y, inner, 36f);
            y += 46f;
            ModalKit.Place(_buy, 16f, y, inner);
            y += ModalKit.HeightOf(_buy) + 4f;
            UiBuilder.Place((RectTransform)_cancel.transform, (w - 200f) * 0.5f, y, 200f, 48f);
            y += 48f + 12f + 16f;
            // La hoja llega al borde inferior, pero su contenido queda por encima de la barra de navegación (08 §13.1 #4).
            y += _canvas.SafeBottom;
            ModalKit.Size(_panel, _surface, _art, (_canvas.Width - w) * 0.5f, _canvas.Height - y + 8f, w, y);
        }

        /// <summary>Cierra la hoja: compra (true) o cancela.</summary>
        public void Close(bool confirmed)
        {
            AF.Audio.AudioCues.Play(confirmed ? AF.Audio.SfxId.UiBuy : AF.Audio.SfxId.UiBack); // AU1
            Action<bool>? done = _onClosed;
            _onClosed = null;
            _canvas.Canvas.gameObject.SetActive(false);
            done?.Invoke(confirmed);
        }
    }
}
