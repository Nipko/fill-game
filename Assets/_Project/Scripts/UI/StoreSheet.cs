#nullable enable
using System;
using AF.IAP;
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

        /// <summary>Recalcula la escala si cambió la pantalla; devuelve true si cambió.</summary>
        public bool Measure()
        {
            GameScreen.ReadScreen(out int sw, out int sh, out Rect _);
            if (sw == _screenW && sh == _screenH)
            {
                return false;
            }

            _screenW = sw;
            _screenH = sh;
            float scale = sw / 360f;
            if (sh / scale < 640f)
            {
                scale = sh / 640f;
            }

            scale = Mathf.Max(0.01f, scale);
            _scaler.scaleFactor = scale;
            Width = sw / scale;
            Height = sh / scale;
            return true;
        }
    }

    /// <summary>
    /// Hoja «Compra simulada» (tareas B1-4 y B2-2; D-008 punto 5: compras simuladas detrás de <c>AF.IAP</c>, sin Play
    /// Billing): rótulo, producto y precio local (05 §4.2), con «Comprar» y «Cancelar». Implementa
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
            ModalKit.Header(_panel, art, InterimText(SimulatedPurchase.LabelKey), out _header);
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

        public string ProductText => _name.text;

        public string PriceText => _price.text;

        public Button BuyButton => _buy.Button;

        public Button CancelButton => _cancel;

        /// <inheritdoc />
        public void Present(SimulatedPurchase purchase, Action<bool> onClosed)
        {
            _onClosed = onClosed;
            _canvas.Canvas.gameObject.SetActive(true);
            _canvas.Measure();
            _name.text = InterimText(purchase.NameKey);
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
            ModalKit.Size(_panel, _surface, _art, (_canvas.Width - w) * 0.5f, _canvas.Height - y + 8f, w, y);
        }

        /// <summary>Cierra la hoja: compra (true) o cancela.</summary>
        public void Close(bool confirmed)
        {
            Action<bool>? done = _onClosed;
            _onClosed = null;
            _canvas.Canvas.gameObject.SetActive(false);
            done?.Invoke(confirmed);
        }

        // Puente provisional (S12): AF.IAP ya solo da claves y la tabla de textos aún no las tiene. L3 lo sustituye por
        // Loc.T(clave), con las filas iap_*_name e iap_simulated_purchase en strings.csv, y lo borra. Textos de 05 §4.1.
        private static string InterimText(string key)
        {
            switch (key)
            {
                case SimulatedPurchase.LabelKey: return "Compra simulada";
                case "iap_af_starter_name": return "Mochila de inicio";
                case "iap_af_coins_2_name": return "Puñado de semillas";
                case "iap_af_coins_3_name": return "Bolsa de semillas";
                case "iap_af_coins_4_name": return "Saco de semillas";
                case "iap_af_coins_5_name": return "Costal de semillas";
                case "iap_af_coins_6_name": return "Cosecha de semillas";
                case "iap_af_rescue_name": return "Balsa de rescate";
                case "iap_af_noads_name": return "Sin anuncios";
                case "iap_af_piggy_name": return "Cántaro";
                case "iap_af_infinite_canoes_1h_name": return "Canoas infinitas 1 h";
                case "iap_af_skin_m02_name": return "Pack Cuia de Santarém";
                case "iap_af_skin_m03_name": return "Pack Cuia del Encuentro";
                case "iap_af_skin_m04_name": return "Pack Totuma";
                default: return key;
            }
        }
    }
}
