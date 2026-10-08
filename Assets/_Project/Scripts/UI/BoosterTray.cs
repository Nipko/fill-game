#nullable enable
using System;
using AF.Ads;
using AF.Boosters;
using AF.GameFlow;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Bandeja cumbuca de comodines (tarea B2-1; GDD 07 §7.4, 02 §5, 04 §1.1, §2.1 y §2.2) con el orden canónico y los
    /// equivalentes de Magic Sort (ficha §2.3 y §5.2: deshacer, añadir botella; barajar llega con la Pororoca en I6):
    /// <list type="bullet">
    /// <item><description>[Deshacer · Marea]: sin contador en 1-10; desde el 11, badge blanco con el cupo gratis y «+N» ámbar con
    /// el stock; sin cupo ni stock, «+3» con el icono de video (<c>rv_undo_refill</c>) o la etiqueta de precio (30), y tocarlo
    /// recarga; gris sin historial;</description></item>
    /// <item><description>[+1 cuia · Cuia extra] desde el nivel 8: «+N» con stock o el precio (150), en rojo y con «Te faltan
    /// {n}» encima sin saldo; gris si ya se usó en el nivel, si no queda vertido útil o con 16 cuias.</description></item>
    /// </list>
    /// Antes del desbloqueo la ranura no existe y la bandeja se centra (07 §7.4): 16 + n·64 + (n − 1)·8 + 16 dp. Morpho
    /// (12) y Pororoca (30) no están en el cliente: sus ranuras llegan con sus comodines. Se refresca cada fotograma solo
    /// si algo cambió, sin asignar memoria (08 §5.4).
    /// </summary>
    internal sealed class BoosterTray
    {
        private const float SlotDp = 64f;
        private const float GapDp = 8f;
        private const float PadDp = 16f;

        private readonly ProceduralArt _art;
        private readonly Image _tray;
        private readonly Slot _undo;
        private readonly Slot _extra;
        private readonly char[] _buf = new char[24];

        private LevelFlow? _flow;
        private GameServices? _services;
        private int _key = int.MinValue;
        private float _canvasW = 360f;
        private float _trayTop;
        private int _slots = 1;

        public BoosterTray(Transform hud, ProceduralArt art, Action onUndo, Action onExtraCuia)
        {
            _art = art;
            _tray = UiBuilder.Image("Tray", hud, art.Tray(96f), Color.white);
            _undo = new Slot("UndoButton", hud, art, art.IconUndo, art.IconUndoDisabled, UiText.Undo, onUndo);
            _extra = new Slot("ExtraCuiaButton", hud, art, art.IconCuiaPlus, art.IconCuiaPlusDisabled, UiText.ExtraCuia, onExtraCuia);
            _extra.SetActive(false);
        }

        /// <summary>Botón de Marea.</summary>
        public Button UndoButton => _undo.Parts.Button;

        /// <summary>Botón de Cuia extra (activo desde el nivel 8).</summary>
        public Button ExtraCuiaButton => _extra.Parts.Button;

        /// <summary>La ranura de Cuia extra existe en el nivel.</summary>
        public bool ExtraCuiaShown => _extra.Active;

        /// <summary>Ranuras visibles.</summary>
        public int SlotCount => _slots;

        /// <summary>Ancho de la bandeja (dp).</summary>
        public float Width => PadDp + (_slots * SlotDp) + ((_slots - 1) * GapDp) + PadDp;

        /// <summary>Texto del badge de Marea (diagnóstico y tests).</summary>
        public string UndoBadgeText => _undo.BadgeText;

        /// <summary>Texto del badge de Cuia extra («+N»), o vacío.</summary>
        public string ExtraBadgeText => _extra.BadgeText;

        /// <summary>Precio que muestra la Cuia extra (0 si no lo muestra).</summary>
        public int ExtraPriceShown => _extra.PriceShown;

        /// <summary>La Cuia extra muestra «Te faltan {n}».</summary>
        public bool ExtraShowsMissing => _extra.MissingShown;

        public void Bind(LevelFlow flow, GameServices? services)
        {
            _flow = flow;
            _services = services;
            _key = int.MinValue;
        }

        public void SetVisible(bool visible)
        {
            _tray.gameObject.SetActive(visible);
            _undo.SetVisible(visible);
            _extra.SetVisible(visible && _extra.Active);
        }

        /// <summary>Coloca la bandeja: 80 dp de alto con su borde superior en <paramref name="trayTop"/>, centrada.</summary>
        public void Layout(float canvasW, float trayTop)
        {
            _canvasW = canvasW;
            _trayTop = trayTop;
            float width = Width;
            _tray.sprite = _art.Tray(width);
            float trayX = (canvasW - width) / 2f;
            if (_art.TrayIsPainted)
            {
                _tray.type = Image.Type.Sliced;
                UiBuilder.Place(_tray.rectTransform, trayX, trayTop, width, 80f);
            }
            else
            {
                UiBuilder.Place(_tray.rectTransform, trayX - 8f, trayTop - 2f, width + 16f, 94f);
            }

            float x = trayX + PadDp;
            _undo.Place(x, trayTop + 4f);
            if (_extra.Active)
            {
                _extra.Place(x + SlotDp + GapDp, trayTop + 4f);
            }
        }

        /// <summary>Refresca badges, precios y habilitado si el estado cambió.</summary>
        public void Refresh()
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel)
            {
                return;
            }

            UndoStatus undo = flow.UndoStatus;
            ExtraCuiaOffer extra = flow.ExtraCuia;
            bool video = _services != null && !undo.HasQuota && _services.RewardedReady(AdPlacement.UndoRefill);
            int seeds = _services?.Wallet.Seeds ?? 0;
            bool undoEnabled = undo.HasQuota ? flow.CanUndo : flow.AcceptsBoardInput;
            int key = Hash(undo.Unlimited ? 1 : 0, undo.FreeLeft, undo.Stock, video ? 1 : 0, undoEnabled ? 1 : 0, (int)extra.State, extra.Stock, extra.Missing, seeds);
            if (key == _key)
            {
                return;
            }

            _key = key;
            bool showExtra = extra.State != ExtraCuiaState.Hidden;
            if (showExtra != _extra.Active)
            {
                _extra.SetActive(showExtra);
                _slots = showExtra ? 2 : 1;
                Layout(_canvasW, _trayTop);
            }

            // Marea (04 §1.1): ilimitada sin badge; cupo en blanco; stock «+N» en ámbar; sin nada, «+3» con video o el precio.
            UiBuilder.SetEnabled(_undo.Parts.Button, undoEnabled);
            if (undo.Unlimited)
            {
                _undo.ShowBadge(null, false);
                _undo.ShowStockBadge(0);
                _undo.HidePrice();
            }
            else if (undo.HasQuota)
            {
                _undo.ShowBadge(undo.FreeLeft > 0 ? Number(undo.FreeLeft, false) : null, false);
                _undo.ShowStockBadge(undo.Stock);
                _undo.HidePrice();
            }
            else
            {
                int amount = _services?.Wallet.Config.UndoRefillAmount ?? 3;
                int price = _services?.Wallet.Config.UndoRefillSeedPrice ?? 30;
                _undo.ShowStockBadge(0);
                if (video)
                {
                    _undo.ShowBadge(Number(amount, true), true);
                    _undo.HidePrice();
                }
                else
                {
                    _undo.ShowBadge(Number(amount, true), false);
                    _undo.ShowPrice(price, Math.Max(0, price - seeds));
                }
            }

            if (!showExtra)
            {
                return;
            }

            // Cuia extra (04 §2.2): stock «+N» o precio; gris si ya se usó, sin vertido útil o con 16 cuias.
            UiBuilder.SetEnabled(_extra.Parts.Button, extra.State == ExtraCuiaState.Ready);
            if (extra.FromStock)
            {
                _extra.ShowBadge(null, false);
                _extra.ShowStockBadge(extra.Stock);
                _extra.HidePrice();
            }
            else if (extra.State == ExtraCuiaState.Ready)
            {
                _extra.ShowBadge(null, false);
                _extra.ShowStockBadge(0);
                _extra.ShowPrice(extra.Price, extra.Missing);
            }
            else
            {
                // Usada en el nivel, sin vertido útil o con 16 cuias: gris y sin precio.
                _extra.ShowBadge(null, false);
                _extra.ShowStockBadge(0);
                _extra.HidePrice();
            }
        }

        private string Number(int value, bool plus)
        {
            int n = 0;
            if (plus)
            {
                _buf[n++] = '+';
            }

            n = UiText.WriteNumber(_buf, n, value, false);
            return new string(_buf, 0, n);
        }

        private static int Hash(int a, int b, int c, int d, int e, int f, int g, int h, int i)
        {
            unchecked
            {
                int x = a;
                x = (x * 397) ^ b;
                x = (x * 397) ^ c;
                x = (x * 397) ^ d;
                x = (x * 397) ^ e;
                x = (x * 397) ^ f;
                x = (x * 397) ^ g;
                x = (x * 397) ^ h;
                return (x * 397) ^ i;
            }
        }

        // Una ranura de 64 dp (guía 3.6): botón de la familia Agua con glifo y etiqueta funcional, badge arriba a la derecha
        // (cupo en blanco o «+3» con video), badge ámbar «+N» del stock arriba a la izquierda, etiqueta de precio montada sobre
        // el borde inferior (como Magic Sort, ficha §2.3) y «Te faltan {n}» encima de la ranura sin saldo.
        private sealed class Slot
        {
            private readonly RectTransform _badge;
            private readonly Image _badgeDisc;
            private readonly TextMeshProUGUI _badgeText;
            private readonly Image _badgeVideo;
            private readonly RectTransform _stock;
            private readonly TextMeshProUGUI _stockText;
            private readonly PriceTag _price;
            private readonly RectTransform _missing;
            private readonly TextMeshProUGUI _missingText;
            private readonly char[] _buf = new char[24];
            private bool _visible = true;
            private float _x;
            private float _y;

            public Slot(string name, Transform hud, ProceduralArt art, Sprite icon, Sprite iconDisabled, string label, Action onClick)
            {
                Parts = UiBuilder.Relief(name, hud, art.SlotWater, onClick);
                Image glyph = UiBuilder.Image("Icon", Parts.Face, icon, Color.white);
                UiBuilder.Place(glyph.rectTransform, 16f, 6f, 32f, 32f);
                TextMeshProUGUI text = UiBuilder.Label("Label", Parts.Face, TextStyle.SlotWater, label, 12f, Palette.Cream, TextAlignmentOptions.Center);
                UiBuilder.Place(text.rectTransform, 0f, 41f, 64f, 16f);
                Parts.Relief.Bind(Parts.Face, Parts.Base, Parts.FaceImage, art.SlotWater, glyph, iconDisabled, text);

                _badge = UiBuilder.Rect("Badge", Parts.Button.transform);
                _badgeDisc = UiBuilder.Image("Disc", _badge, art.Disc, Palette.Cream);
                UiBuilder.Stretch(_badgeDisc.rectTransform);
                _badgeText = UiBuilder.Label("Value", _badge, TextStyle.PlainBold, string.Empty, 13f, Palette.Ink, TextAlignmentOptions.Center);
                UiBuilder.Stretch(_badgeText.rectTransform);
                _badgeVideo = UiBuilder.Image("Video", _badge, art.IconVideo, Color.white);
                _badgeVideo.preserveAspect = true;
                _badge.gameObject.SetActive(false);

                _stock = UiBuilder.Rect("Stock", Parts.Button.transform);
                Image stockDisc = UiBuilder.Sliced("Pill", _stock, art.RoundedRect, Palette.UiAmber);
                stockDisc.pixelsPerUnitMultiplier = 24f / 10f;
                UiBuilder.Stretch(stockDisc.rectTransform);
                _stockText = UiBuilder.Label("Value", _stock, TextStyle.PlainBold, string.Empty, 13f, Palette.Ink, TextAlignmentOptions.Center);
                UiBuilder.Stretch(_stockText.rectTransform);
                _stock.gameObject.SetActive(false);

                _price = new PriceTag("Price", Parts.Button.transform, art);
                _price.Root.gameObject.SetActive(false);

                _missing = UiBuilder.Rect("Missing", hud);
                Image missingPlate = UiBuilder.Sliced("Plate", _missing, art.RoundedRect, Palette.PanelLight);
                missingPlate.pixelsPerUnitMultiplier = 24f / 9f;
                UiBuilder.Stretch(missingPlate.rectTransform);
                _missingText = UiBuilder.Label("Value", _missing, TextStyle.PlainBold, string.Empty, 11f, Palette.UiRedWarning, TextAlignmentOptions.Center);
                UiBuilder.Stretch(_missingText.rectTransform);
                _missing.gameObject.SetActive(false);
            }

            public ReliefParts Parts { get; }

            public bool Active { get; private set; } = true;

            public string BadgeText => (_badge.gameObject.activeSelf ? _badgeText.text : string.Empty) + (_stock.gameObject.activeSelf ? _stockText.text : string.Empty);

            public int PriceShown => _price.Root.gameObject.activeSelf ? _price.Value : 0;

            public bool MissingShown => _missing.gameObject.activeSelf;

            public void SetActive(bool active)
            {
                Active = active;
                SetVisible(_visible);
            }

            public void SetVisible(bool visible)
            {
                _visible = visible;
                bool on = visible && Active;
                Parts.Button.gameObject.SetActive(on);
                if (!on)
                {
                    _missing.gameObject.SetActive(false);
                }
            }

            public void Place(float x, float y)
            {
                _x = x;
                _y = y;
                UiBuilder.Place((RectTransform)Parts.Button.transform, x, y, SlotDp, 72f);
                UiBuilder.Place(_badge, SlotDp - 16f, -8f, 24f, 24f);
                UiBuilder.Place(_badgeText.rectTransform, 0f, 0f, 24f, 24f);
                PlaceMissing();
            }

            public void ShowBadge(string? text, bool video)
            {
                bool on = text != null;
                _badge.gameObject.SetActive(on);
                if (!on)
                {
                    return;
                }

                _badgeText.text = text;
                _badgeVideo.gameObject.SetActive(video);
                float w = video ? 44f : 24f;
                UiBuilder.Place(_badge, SlotDp - w + 8f, -8f, w, 24f);
                _badgeDisc.type = Image.Type.Simple;
                UiBuilder.Place(_badgeText.rectTransform, 0f, 0f, video ? 22f : 24f, 24f);
                if (video)
                {
                    UiBuilder.Place(_badgeVideo.rectTransform, 22f, 3f, 20f, 18f);
                }
            }

            public void ShowStockBadge(int stock)
            {
                bool on = stock > 0;
                _stock.gameObject.SetActive(on);
                if (!on)
                {
                    return;
                }

                int n = 0;
                _buf[n++] = '+';
                n = UiText.WriteNumber(_buf, n, stock, false);
                _stockText.SetCharArray(_buf, 0, n);
                UiBuilder.Place(_stock, -8f, -8f, 14f + (8f * n), 22f);
            }

            public void ShowPrice(int price, int missing)
            {
                _price.Root.gameObject.SetActive(true);
                _price.Set(price, 0, missing > 0);
                _price.PlaceAt((SlotDp - _price.Width) * 0.5f, 72f - 16f);
                _missing.gameObject.SetActive(missing > 0 && Parts.Button.gameObject.activeSelf);
                if (missing > 0)
                {
                    _missingText.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, UiText.Missing, UiText.Grouped(missing));
                    PlaceMissing();
                }
            }

            public void HidePrice()
            {
                _price.Root.gameObject.SetActive(false);
                _missing.gameObject.SetActive(false);
            }

            private void PlaceMissing()
            {
                UiBuilder.Place(_missing, _x - 20f, _y - 24f, SlotDp + 40f, 18f);
            }
        }
    }
}
