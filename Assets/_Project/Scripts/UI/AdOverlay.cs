#nullable enable
using System;
using AF.Ads;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Pantalla «Anuncio simulado» (D-008 punto 4; tarea B1-3, que la pide a la UI, y B2-4, dueña de su forma final): a
    /// pantalla completa y opaca por encima de todo (como los anuncios de Magic Sort, ficha §6.2, sin imitar a ningún
    /// anunciante), con el rótulo, la ubicación y el tipo (p. ej. <c>rv_extra_canoe · recompensado</c>) y la cuenta atrás;
    /// «Cerrar» se habilita a los 5 s en el intersticial (05 §6.1) y al terminar en el recompensado (3 s [P]), que entonces da
    /// su premio. Sin red ni SDK. La adelantan los recompensados de B2-1 y B2-3 (Marea +3 y Canoa extra), que la necesitan.
    /// Implementa <see cref="ISimulatedAdPresenter"/>: llama una sola vez al cierre.
    /// </summary>
    internal sealed class AdOverlay : ISimulatedAdPresenter
    {
        private readonly OverlayCanvas _canvas;
        private readonly TextMeshProUGUI _title;
        private readonly TextMeshProUGUI _placement;
        private readonly TextMeshProUGUI _countdown;
        private readonly ReliefParts _close;
        private readonly char[] _buf = new char[8];
        private SimulatedAd? _ad;
        private Action<SimulatedAdClose>? _onClosed;
        private float _elapsedMs;
        private int _secondsShown = -1;

        public AdOverlay(Transform host, ProceduralArt art)
        {
            _canvas = new OverlayCanvas("AdOverlayCanvas", host, 60);
            Image back = UiBuilder.Image("Back", _canvas.Root, null, new Color(0.02f, 0.03f, 0.03f, 1f), true); // opaco, como un anuncio real a pantalla completa (ficha §6.2)
            UiBuilder.Stretch(back.rectTransform);
            _title = UiBuilder.Label("Title", _canvas.Root, TextStyle.ModalTitle, UiText.SimulatedAdTitle, 28f, Palette.Cream, TextAlignmentOptions.Center);
            _placement = UiBuilder.Label("Placement", _canvas.Root, TextStyle.PlainBold, string.Empty, 16f, Palette.Cream, TextAlignmentOptions.Center);
            _countdown = UiBuilder.Label("Countdown", _canvas.Root, TextStyle.ModalTitle, string.Empty, 64f, Palette.Cream, TextAlignmentOptions.Center);
            _close = ModalKit.Cta("CloseButton", _canvas.Root, art.CtaWood, TextStyle.PillWood, UiText.SimulatedAdClose, 18f, OnClose, out _);
            _canvas.Canvas.gameObject.SetActive(false);
        }

        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Rótulo de la pantalla («Anuncio simulado», de <c>SimulatedAd.LabelKey</c>).</summary>
        public string TitleText => _title.text;

        /// <summary>Lienzo propio (capturas de prueba).</summary>
        public Canvas Canvas => _canvas.Canvas;

        public string PlacementText => _placement.text;

        public Button CloseButton => _close.Button;

        /// <inheritdoc />
        public void Present(SimulatedAd ad, Action<SimulatedAdClose> onClosed)
        {
            _ad = ad;
            _onClosed = onClosed;
            _elapsedMs = 0f;
            _secondsShown = -1;
            _canvas.Canvas.gameObject.SetActive(true);
            _canvas.Measure();
            _placement.text = UiText.SimulatedAdLabel(ad.Placement.ToId(), ad.Format == AdFormat.Rewarded);
            float w = _canvas.Width;
            float h = _canvas.Height;
            UiBuilder.Place(_title.rectTransform, 16f, (h * 0.30f) - 20f, w - 32f, 40f);
            UiBuilder.Place(_placement.rectTransform, 16f, (h * 0.30f) + 26f, w - 32f, 24f);
            UiBuilder.Place(_countdown.rectTransform, 16f, (h * 0.45f) - 40f, w - 32f, 80f);
            float cw = Mathf.Min(220f, w - 64f);
            ModalKit.Place(_close, (w - cw) * 0.5f, h * 0.68f, cw);
            Tick(0f);
        }

        /// <summary>Avanza la cuenta atrás (la llama <c>GameBootstrap.Update</c>).</summary>
        public void Tick(float dtMs)
        {
            SimulatedAd? ad = _ad;
            if (ad == null)
            {
                return;
            }

            _elapsedMs += dtMs;
            double closeMs = ad.CloseEnabledAfter.TotalMilliseconds;
            int seconds = (int)Math.Ceiling(Math.Max(0.0, closeMs - _elapsedMs) / 1000.0);
            if (seconds != _secondsShown)
            {
                _secondsShown = seconds;
                bool ready = seconds <= 0;
                UiBuilder.SetEnabled(_close.Button, ready);
                _countdown.gameObject.SetActive(!ready);
                if (!ready)
                {
                    int n = UiText.WriteNumber(_buf, 0, seconds, false);
                    _countdown.SetCharArray(_buf, 0, n);
                }
            }
        }

        private void OnClose()
        {
            if (_ad == null || _elapsedMs < _ad.CloseEnabledAfter.TotalMilliseconds)
            {
                return;
            }

            Action<SimulatedAdClose>? done = _onClosed;
            _ad = null;
            _onClosed = null;
            _canvas.Canvas.gameObject.SetActive(false);
            done?.Invoke(SimulatedAdClose.Completed);
        }
    }
}
