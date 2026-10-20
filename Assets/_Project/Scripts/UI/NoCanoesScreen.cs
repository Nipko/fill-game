#nullable enable
using System;
using System.Collections.Generic;
using AF.Ads;
using AF.Economy;
using AF.IAP;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Sin canoas (paso 4 del embudo de D-008; GDD 04 §7.3; tarea B2-3), con la composición de «More Lives» de Magic Sort
    /// (ficha §4.5 y §4.8: cabecera con X, el contador vacío con «Time to next life» y el cronómetro, y debajo las opciones
    /// apiladas) y el material de D-003: canoa volcada en lugar del corazón. Opciones en el orden de 04 §7.3: esperar
    /// («Tu próxima canoa vuelve en mm:ss», siempre), «Canoa extra · Ver video» +1 (<c>rv_extra_canoe</c>, si hay anuncio y
    /// tope), «Llenar canoas» por <c>canoes_refill_seed_price</c> (hasta 5/5), «Canoas infinitas 1 h · {precio}»
    /// (<c>af_infinite_canoes_1h</c>, compra simulada) y «Volver al inicio». Nunca muestra intersticial (05 §10). La usan la
    /// pantalla de nivel (desde Nivel fallido o la cabecera) y la de inicio (Jugar o el contador en 0/5). El cronómetro se
    /// refresca sin asignar memoria.
    /// </summary>
    internal sealed class NoCanoesPanel
    {
        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly ReliefParts _close;
        private readonly Image _canoe;
        private readonly TextMeshProUGUI _timer;
        private readonly ReliefParts _video;
        private readonly TextMeshProUGUI _videoLabel;
        private readonly Image _videoIcon;
        private readonly ReliefParts _refill;
        private readonly TextMeshProUGUI _refillLabel;
        private readonly PriceTag _refillPrice;
        private readonly TextMeshProUGUI _refillNote;
        private readonly ReliefParts _infinite;
        private readonly TextMeshProUGUI _infiniteLabel;
        private readonly Button _home;
        private readonly NoCanoesDailyLink _daily; // E7: «Mientras esperas: Jugar el Reto del río» (04 §7.3)
        private bool _dailyShown;
        private readonly char[] _buf = new char[64];
        private long _timerShown = long.MinValue;
        private int _key = int.MinValue;
        private float _canvasW = 360f;
        private float _canvasH = 780f;

        public NoCanoesPanel(Transform parent, ProceduralArt art, Action onVideo, Action onRefill, Action onInfinite, Action onHome, Action onClose)
        {
            _art = art;
            _root = UiBuilder.Rect("NoCanoes", parent);
            ModalKit.Dim("Dim", _root, 0.62f);
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            ModalKit.Header(_panel, art, UiText.NoCanoesTitle, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, onClose);
            _canoe = UiBuilder.Image("Canoe", _panel, art.IconCanoeEmpty, Color.white);
            _canoe.preserveAspect = true;
            _timer = ModalKit.Line("Timer", _panel, string.Empty, 17f, Palette.FamilyWood.Base);
            _video = ModalKit.Cta("VideoButton", _panel, art.CtaWater, TextStyle.CtaWater, UiText.NoCanoesVideo + "  +1", 18f, onVideo, out _videoLabel);
            _videoIcon = UiBuilder.Image("Video", _video.Face, art.IconVideo, Color.white);
            _refill = ModalKit.Cta("RefillButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, string.Empty, 18f, onRefill, out _refillLabel);
            _refillPrice = new PriceTag("Price", _refill.Face, art);
            _refillNote = ModalKit.Line("RefillNote", _panel, string.Empty, 13f, Palette.UiRedWarning);
            _infinite = ModalKit.Cta("InfiniteButton", _panel, art.CtaWood, TextStyle.PillWood, string.Empty, 16f, onInfinite, out _infiniteLabel);
            _home = ModalKit.Link("HomeButton", _panel, UiText.BackHome, onHome, out _);
            _daily = new NoCanoesDailyLink(_panel);
            _root.gameObject.SetActive(false);
        }

        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Raíz del modal, hija del lienzo de la pantalla de nivel (tests de orden de capas).</summary>
        public RectTransform Root => _root;

        public Button VideoButton => _video.Button;

        public Button RefillButton => _refill.Button;

        public Button InfiniteButton => _infinite.Button;

        public Button HomeButton => _home;

        /// <summary>«Mientras esperas: Jugar el Reto del río» (E7; tests).</summary>
        public NoCanoesDailyLink DailyLink => _daily;

        public Button CloseButton => _close.Button;

        public string TimerText => _timer.text;

        public bool VideoShown => _video.Button.gameObject.activeSelf;

        public void Show(GameServices? services, float canvasW, float canvasH)
        {
            _root.gameObject.SetActive(true);
            _canvasW = canvasW;
            _canvasH = canvasH;
            _key = int.MinValue;
            _timerShown = long.MinValue;
            _dailyShown = NoCanoesDailyLink.Available; // se mira al abrir: con Sin canoas delante, el Reto de hoy no cambia
            Refresh(services);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        /// <summary>
        /// Cronómetro y opciones; rehace el layout solo si cambió lo que se ofrece (el video, el saldo, las opciones o el tope).
        /// Devuelve si lo rehízo: los botones pudieron cambiar de sitio y quien publica AF_LAYOUT la vuelve a emitir
        /// (<c>GameScreen.RefreshHud</c>).
        /// </summary>
        public bool Refresh(GameServices? services)
        {
            if (!_root.gameObject.activeSelf)
            {
                return false;
            }

            CanoesStatus canoes = services?.Wallet.Canoes ?? default;
            long seconds = CeilSeconds(canoes.TimeToNext);
            if (seconds != _timerShown)
            {
                _timerShown = seconds;
                string prefix = UiText.NoCanoesNext;
                int n = 0;
                for (int i = 0; i < prefix.Length && n < _buf.Length - 12; i++)
                {
                    _buf[n++] = prefix[i];
                }

                _buf[n++] = ' ';
                n = UiText.WriteClock(_buf, n, seconds);
                _timer.SetCharArray(_buf, 0, n);
            }

            bool video = services != null && services.RewardedReady(AdPlacement.ExtraCanoe);
            int price = services?.Wallet.Price(SeedSink.CanoesRefill) ?? EconomyConfig.Defaults.CanoesRefillSeedPrice;
            int missing = services?.Wallet.Missing(price) ?? price;
            bool refillOn = services != null && services.Wallet.Config.CanoesRefillSeedsEnabled;
            bool infiniteOn = services == null || !services.PurchasesBlocked; // perfil en solo lectura: sin compras (08 §4.4)
            int key = ((video ? 1 : 0) * 397) ^ (missing * 7) ^ ((refillOn ? 1 : 0) * 13) ^ ((infiniteOn ? 1 : 0) * 8191) ^ canoes.Max ^ ((_dailyShown ? 1 : 0) * 131071);
            if (key == _key)
            {
                return false;
            }

            _key = key;
            Layout(services, video, price, missing, refillOn, infiniteOn, canoes.Max);
            return true;
        }

        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            Add(ids, rects, "close", _close.Button);
            Add(ids, rects, "extra_canoe_video", _video.Button);
            Add(ids, rects, "refill_canoes", _refill.Button);
            Add(ids, rects, "infinite_canoes", _infinite.Button);
            Add(ids, rects, NoCanoesDailyLink.LayoutId, _daily.Button);
            Add(ids, rects, "home", _home);
        }

        private void Layout(GameServices? services, bool video, int price, int missing, bool refillOn, bool infiniteOn, int max)
        {
            float width = Mathf.Min(328f, _canvasW - 32f);
            float inner = width - 32f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner - 52f, 40f);
            UiBuilder.Place((RectTransform)_close.Button.transform, width - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
            y += 50f;
            UiBuilder.Place(_canoe.rectTransform, (width - 110f) * 0.5f, y, 110f, 65f);
            y += 68f;
            y += ModalKit.PlaceLine(_timer, 16f, y, inner, 24f) + 8f;

            _video.Button.gameObject.SetActive(video);
            if (video)
            {
                ModalKit.Place(_video, 16f, y, inner);
                UiBuilder.Place(_videoIcon.rectTransform, 12f, (ModalKit.FaceHeightOf(_video) - 20f) * 0.5f, 22f, 20f);
                _videoLabel.rectTransform.sizeDelta = new Vector2(-52f, _videoLabel.rectTransform.sizeDelta.y);
                _videoLabel.rectTransform.anchoredPosition = new Vector2(14f, _videoLabel.rectTransform.anchoredPosition.y);
                y += ModalKit.HeightOf(_video) + 8f;
            }

            _refill.Button.gameObject.SetActive(refillOn);
            _refillNote.gameObject.SetActive(false);
            if (refillOn)
            {
                _refillLabel.text = UiText.NoCanoesRefill(max);
                UiBuilder.SetEnabled(_refill.Button, missing == 0);
                _refillPrice.Set(price, 0, missing > 0);
                ModalKit.Place(_refill, 16f, y, inner);
                _refillPrice.PlaceAt(inner - _refillPrice.Width - 12f, (ModalKit.FaceHeightOf(_refill) - _refillPrice.Height) * 0.5f);
                _refillLabel.rectTransform.sizeDelta = new Vector2(-(_refillPrice.Width + 36f), _refillLabel.rectTransform.sizeDelta.y);
                _refillLabel.rectTransform.anchoredPosition = new Vector2(-(_refillPrice.Width + 12f) * 0.5f, _refillLabel.rectTransform.anchoredPosition.y);
                y += ModalKit.HeightOf(_refill);
                if (missing > 0)
                {
                    _refillNote.gameObject.SetActive(true);
                    _refillNote.text = UiText.MissingSeeds(missing);
                    y += ModalKit.PlaceLine(_refillNote, 16f, y, inner, 18f) + 2f;
                }

                y += 8f;
            }

            _infinite.Button.gameObject.SetActive(infiniteOn);
            if (infiniteOn)
            {
                _infiniteLabel.text = UiText.NoCanoesInfinite(services?.PriceText(StoreCatalog.InfiniteCanoes1h) ?? string.Empty);
                ModalKit.Place(_infinite, 16f, y, inner);
                y += ModalKit.HeightOf(_infinite) + 6f;
            }
            y += _daily.Layout(_dailyShown, 16f, y, inner); // 04 §7.3: la línea del Reto, encima de «Volver»
            UiBuilder.Place((RectTransform)_home.transform, (width - 220f) * 0.5f, y, 220f, 48f);
            y += 48f + 8f;
            ModalKit.Size(_panel, _surface, _art, (_canvasW - width) * 0.5f, Mathf.Max(8f, Mathf.Round((_canvasH - y) * 0.45f)), width, y);
        }

        private static long CeilSeconds(TimeSpan span)
        {
            if (span <= TimeSpan.Zero)
            {
                return 0L;
            }

            return (span.Ticks + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond;
        }

        private static void Add(List<string> ids, List<RectTransform> rects, string id, Button button)
        {
            if (button.gameObject.activeInHierarchy)
            {
                ids.Add(id);
                rects.Add((RectTransform)button.transform);
            }
        }
    }
}
