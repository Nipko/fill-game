#nullable enable
using System;
using AF.Economy;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Saldos arriba durante el embudo de bloqueo (D-008 paso 1: «arriba, semillas y canoas»; 04 §6.6: «cabecera: semillas,
    /// canoas»), como el saldo de Magic Sort sobre sus pantallas de fallo (ficha §4.4 y §4.8: «el saldo sale siempre arriba a
    /// la izquierda»): Semillas y, desde el 31, Canoas «n/5» con el cronómetro de la próxima (∞ y el tiempo que queda con
    /// Canoas infinitas). Mismo material que la pantalla de inicio (placa de madera oscura). Sin asignaciones al refrescar.
    /// </summary>
    internal sealed class WalletBar
    {
        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _seeds;
        private readonly TextMeshProUGUI _seedsText;
        private readonly RectTransform _canoes;
        private readonly Image _canoesIcon;
        private readonly Image _infinite;
        private readonly TextMeshProUGUI _canoesCount;
        private readonly TextMeshProUGUI _timer;
        private readonly char[] _buf = new char[24];
        private int _seedsShown = -1;
        private int _countShown = -1;
        private int _modeShown = -1;
        private long _timerShown = long.MinValue;

        public WalletBar(Transform parent, ProceduralArt art)
        {
            _art = art;
            _root = UiBuilder.Rect("WalletBar", parent);
            _seeds = UiBuilder.Rect("Seeds", _root);
            Image plate = UiBuilder.Sliced("Plate", _seeds, art.WoodPlate, Color.white);
            UiBuilder.Stretch(plate.rectTransform);
            Image seed = UiBuilder.Image("Icon", _seeds, art.IconSeed, Color.white);
            seed.preserveAspect = true;
            UiBuilder.Place(seed.rectTransform, -8f, 1f, 38f, 38f);
            _seedsText = UiBuilder.Label("Value", _seeds, TextStyle.PillWood, "0", 20f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.AutoSize(_seedsText, 14f, 20f);
            UiBuilder.Place(_seedsText.rectTransform, 30f, 0f, 74f, 40f);

            _canoes = UiBuilder.Rect("Canoes", _root);
            Image canoesPlate = UiBuilder.Sliced("Plate", _canoes, art.WoodPlate, Color.white);
            UiBuilder.Place(canoesPlate.rectTransform, 0f, 0f, 128f, 40f);
            _canoesIcon = UiBuilder.Image("Icon", _canoes, art.IconCanoe, Color.white);
            _canoesIcon.preserveAspect = true;
            UiBuilder.Place(_canoesIcon.rectTransform, -8f, 7f, 48f, 28f);
            _infinite = UiBuilder.Image("Infinite", _canoes, art.IconInfinite, Color.white);
            _infinite.preserveAspect = true;
            _canoesCount = UiBuilder.Label("Count", _canoes, TextStyle.PillWood, "5/5", 18f, Palette.Cream, TextAlignmentOptions.Center);
            _timer = UiBuilder.Label("Timer", _canoes, TextStyle.PillWood, string.Empty, 12f, Palette.Cream, TextAlignmentOptions.Center);
            _root.gameObject.SetActive(false);
        }

        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Semillas mostradas.</summary>
        public int SeedsShown => _seedsShown;

        /// <summary>Canoas mostradas (−1 con Canoas infinitas o sin contador).</summary>
        public int CanoesShown => _canoes.gameObject.activeSelf && _modeShown != 2 ? _countShown : -1;

        public void SetVisible(bool visible)
        {
            if (_root.gameObject.activeSelf != visible)
            {
                _root.gameObject.SetActive(visible);
                _seedsShown = -1;
                _modeShown = -1;
                _timerShown = long.MinValue;
            }
        }

        /// <summary>Coloca la barra en la fila superior (<paramref name="top"/>), empezando en <paramref name="left"/>.</summary>
        public void Layout(float left, float top)
        {
            UiBuilder.Place(_seeds, left, top + 4f, 112f, 40f);
            UiBuilder.Place(_canoes, left + 124f, top + 4f, 128f, 40f);
        }

        /// <summary>
        /// Refresca las cifras de <paramref name="wallet"/>; las canoas solo si <paramref name="showCanoes"/>. Con
        /// <paramref name="seedsPending"/> &gt; 0 (la Cosecha de la victoria mientras cuenta hacia arriba, 02 §6), el saldo
        /// enseña lo ya contado.
        /// </summary>
        public void Refresh(Wallet? wallet, bool showCanoes, int seedsPending = 0)
        {
            if (!_root.gameObject.activeSelf || wallet == null)
            {
                return;
            }

            int seeds = Math.Max(0, wallet.Seeds - Math.Max(0, seedsPending));
            if (seeds != _seedsShown)
            {
                _seedsShown = seeds;
                int n = UiText.WriteNumber(_buf, 0, seeds, true);
                _seedsText.SetCharArray(_buf, 0, n);
            }

            if (_canoes.gameObject.activeSelf != showCanoes)
            {
                _canoes.gameObject.SetActive(showCanoes);
                _modeShown = -1;
            }

            if (!showCanoes)
            {
                return;
            }

            CanoesStatus canoes = wallet.Canoes;
            int mode = canoes.IsInfinite ? 2 : (canoes.Count <= 0 ? 0 : 1);
            if (mode != _modeShown || canoes.Count != _countShown)
            {
                _modeShown = mode;
                _countShown = canoes.Count;
                _canoesIcon.sprite = mode == 0 ? _art.IconCanoeEmpty : _art.IconCanoe;
                _infinite.gameObject.SetActive(mode == 2);
                _canoesCount.gameObject.SetActive(mode != 2);
                if (mode != 2)
                {
                    int n = UiText.WriteNumber(_buf, 0, canoes.Count, false);
                    _buf[n++] = '/';
                    n = UiText.WriteNumber(_buf, n, canoes.Max, false);
                    _canoesCount.SetCharArray(_buf, 0, n);
                }

                _timerShown = long.MinValue;
            }

            long seconds = canoes.IsInfinite ? Ceil(canoes.InfiniteRemaining) : (canoes.IsFull ? -1L : Ceil(canoes.TimeToNext));
            if (seconds != _timerShown)
            {
                bool layout = (seconds < 0) != (_timerShown < 0) || _timerShown == long.MinValue;
                _timerShown = seconds;
                _timer.gameObject.SetActive(seconds >= 0);
                if (seconds >= 0)
                {
                    int n = UiText.WriteClock(_buf, 0, seconds);
                    _timer.SetCharArray(_buf, 0, n);
                }

                if (layout)
                {
                    bool timer = seconds >= 0;
                    UiBuilder.Place(_canoesCount.rectTransform, 40f, timer ? 2f : 0f, 82f, timer ? 21f : 40f);
                    UiBuilder.Place(_timer.rectTransform, 40f, 22f, 82f, 16f);
                    UiBuilder.Place(_infinite.rectTransform, 40f + ((82f - 26f) * 0.5f), timer ? 6f : 13f, 26f, 14f);
                }
            }
        }

        private static long Ceil(TimeSpan span)
        {
            return span <= TimeSpan.Zero ? 0L : (span.Ticks + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond;
        }
    }
}
