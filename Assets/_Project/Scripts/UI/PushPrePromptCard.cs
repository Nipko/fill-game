#nullable enable
using System;
using System.Collections.Generic;
using AF.LiveOps;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Tarjeta del pre-prompt propio de push (06 §8.1; nota de B6, <c>docs/dev/sesiones-notificaciones.md</c> §3 y §6; B6-Q16 (a)):
    /// una pantalla aparte, no un takeover (D-033, añadido del 2026-10-20, punto 11), con la composición de <see cref="OfferCard"/>
    /// (<see cref="ModalKit"/>) en su propio lienzo encima de la victoria: el retrato de TiTino (que firma los avisos desde D-013,
    /// T1-A), su pregunta «¿Te avisamos cuando tu Reto del río esté listo y tu Bitácora espere?», «Avísame» (Hoja, el principal) y
    /// «Ahora no» (enlace). Sin X: el velo no la cierra; el Atrás de Android cuenta como «Ahora no» (<c>GameBootstrap</c>). Implementa
    /// <see cref="IPushPrePromptPresenter"/>: llama una sola vez a la respuesta; quien la abre (<see cref="PushPermissionService.OnVictory"/>)
    /// apunta el pre-prompt y, con «Avísame», pide el diálogo nativo. Textos ≥ 12 sp con la escala de texto hasta el 130 %.
    /// </summary>
    internal sealed class PushPrePromptCard : IPushPrePromptPresenter
    {
        /// <summary>
        /// Orden del lienzo: encima de la victoria, de la tarjeta de oferta (28), del Mercado (30) y de la libreta (32); por debajo de
        /// Ajustes (40), del Mapa (45), de la hoja de compra (50), de un anuncio (60) y del consentimiento (65).
        /// </summary>
        public const int SortingOrder = 34;

        /// <summary>Ancho máximo de la tarjeta (dp), el de las demás tarjetas de <see cref="ModalKit"/>.</summary>
        public const float MaxWidthDp = 328f;

        private readonly ProceduralArt _art;
        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _portrait;
        private readonly TextMeshProUGUI _body;
        private readonly ReliefParts _accept;
        private readonly TextMeshProUGUI _acceptLabel;
        private readonly Button _decline;
        private readonly TextMeshProUGUI _declineLabel;
        private Action<bool>? _answered;

        public PushPrePromptCard(Transform host, ProceduralArt art)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            _canvas = new OverlayCanvas("PushPrePromptCanvas", host, SortingOrder);
            ModalKit.Dim("Dim", _canvas.Root, 0.55f); // captura los toques: la victoria de debajo no se toca con la tarjeta abierta
            _panel = ModalKit.Card("Panel", _canvas.Root, art, out _surface);
            _portrait = UiBuilder.Image("TiTino", _panel, null, Color.white);
            _portrait.preserveAspect = true;
            _body = ModalKit.Line("Body", _panel, string.Empty, 18f, Palette.FamilyWood.Base);
            _accept = ModalKit.Cta("AcceptButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, string.Empty, 21f, () => Close(true), out _acceptLabel);
            _decline = ModalKit.Link("DeclineButton", _panel, string.Empty, () => Close(false), out _declineLabel);
            _declineLabel.fontSizeMin = UiBuilder.MinTextSp;
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>Está abierta.</summary>
        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Lienzo (capturas).</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>Tarjeta (tests: dentro del lienzo).</summary>
        public RectTransform Panel => _panel;

        /// <summary>El pre-prompt que enseña (<see cref="PrePromptKind.None"/> cerrada).</summary>
        public PrePromptKind Kind { get; private set; }

        /// <summary>Cuántas veces se abrió (tests).</summary>
        public int ShownCount { get; private set; }

        public string BodyText => _body.text;

        public string AcceptText => _acceptLabel.text;

        public string DeclineText => _declineLabel.text;

        /// <summary>«Avísame».</summary>
        public Button AcceptButton => _accept.Button;

        /// <summary>«Ahora no».</summary>
        public Button DeclineButton => _decline;

        /// <summary>Se ve el retrato de TiTino (sin arte final ni provisional, la tarjeta va sin él).</summary>
        public bool PortraitShown => _portrait.gameObject.activeSelf;

        /// <summary>Todos los textos (tests: ≥ 12 sp y sin recortes).</summary>
        public IEnumerable<TextMeshProUGUI> Texts()
        {
            yield return _body;
            yield return _acceptLabel;
            yield return _declineLabel;
        }

        /// <inheritdoc />
        public void Show(PrePromptRequest request, Action<bool> answered)
        {
            if (answered == null)
            {
                throw new ArgumentNullException(nameof(answered));
            }

            if (Visible)
            {
                Close(false); // nunca dos a la vez: la anterior cuenta como «Ahora no»
            }

            Kind = request.Kind;
            ShownCount++;
            _answered = answered;
            _canvas.Canvas.gameObject.SetActive(true);
            _canvas.Measure();
            _body.text = PushText.PrePromptBody;
            _acceptLabel.text = PushText.PrePromptAccept;
            _declineLabel.text = PushText.PrePromptDecline;
            Sprite? portrait = _art.SlothPortrait(false);
            _portrait.sprite = portrait;
            _portrait.gameObject.SetActive(portrait != null);
            Layout();
        }

        /// <summary>Responde y cierra: «Avísame» (true) o «Ahora no» (false). Cerrada, no hace nada.</summary>
        public void Close(bool accept)
        {
            if (!Visible)
            {
                return;
            }

            _canvas.Canvas.gameObject.SetActive(false);
            Kind = PrePromptKind.None;
            Action<bool>? answered = _answered;
            _answered = null;
            answered?.Invoke(accept);
        }

        /// <summary>Con la tarjeta abierta, la vuelve a colocar si cambió la pantalla.</summary>
        public void Tick()
        {
            if (Visible && _canvas.Measure())
            {
                Layout();
            }
        }

        /// <summary>Botones visibles para <c>AF_LAYOUT</c> (sonda de desarrollo).</summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            if (!Visible)
            {
                return;
            }

            ids.Add("push_accept");
            rects.Add((RectTransform)_accept.Button.transform);
            ids.Add("push_decline");
            rects.Add((RectTransform)_decline.transform);
        }

        private void Layout()
        {
            float canvasW = _canvas.Width;
            float canvasH = _canvas.Height;
            float width = Mathf.Min(MaxWidthDp, canvasW - 32f);
            float inner = width - 32f;
            float y = 20f;
            if (_portrait.gameObject.activeSelf)
            {
                float d = ProceduralArt.SlothPortraitDp;
                UiBuilder.Place(_portrait.rectTransform, (width - d) * 0.5f, y, d, d);
                y += d + 10f;
            }

            y += ModalKit.PlaceLine(_body, 16f, y, inner, 52f) + 14f;
            ModalKit.Place(_accept, 16f, y, inner);
            y += ModalKit.HeightOf(_accept) + 4f;
            float linkH = Mathf.Max(48f, Mathf.Ceil(UiBuilder.MinTextHeight(_declineLabel, inner)) + 12f);
            UiBuilder.Place((RectTransform)_decline.transform, 16f, y, inner, linkH);
            y += linkH + 12f;
            float top = Mathf.Round((canvasH - y) * 0.45f);
            ModalKit.Size(_panel, _surface, _art, Mathf.Round((canvasW - width) * 0.5f), Mathf.Max(8f, top), width, y, sharedSurface: true);
        }
    }
}
