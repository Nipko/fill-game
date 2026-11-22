#nullable enable
using System;
using System.Collections.Generic;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Aviso previo propio al aviso de rastreo de Apple (ATT; fase M3 de <c>docs/dev/traspaso/mac-ios.md</c>; textos aprobados por el
    /// dueño el 2026-11-04): con la composición de <see cref="PushPrePromptCard"/> (<see cref="ModalKit"/>), el retrato de TiTino, el
    /// título «Anuncios a tu medida», el texto que explica qué va a preguntar iOS y un solo botón, «Continuar», que abre el aviso del
    /// sistema. Sin «No» ni X: Apple no deja que el aviso propio se salte el suyo, ni que empuje a aceptar (directriz 5.1.1). Solo en
    /// iOS, una vez (<see cref="IosTracking.ShouldAsk"/>). Textos ≥ 12 sp con la escala de texto hasta el 130 %.
    /// </summary>
    internal sealed class AttPrePromptCard
    {
        /// <summary>Claves de <c>strings.csv</c>.</summary>
        public const string TitleKey = "att_preprompt_title";

        public const string BodyKey = "att_preprompt_body";

        public const string ContinueKey = "att_preprompt_continue";

        /// <summary>Encima del juego y de las tarjetas, por debajo del consentimiento (65): como el pre-prompt de push.</summary>
        public const int SortingOrder = PushPrePromptCard.SortingOrder;

        private readonly ProceduralArt _art;
        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _portrait;
        private readonly TextMeshProUGUI _title;
        private readonly TextMeshProUGUI _body;
        private readonly ReliefParts _continue;
        private readonly TextMeshProUGUI _continueLabel;
        private Action? _continued;

        public AttPrePromptCard(Transform host, ProceduralArt art)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            _canvas = new OverlayCanvas("AttPrePromptCanvas", host, SortingOrder);
            ModalKit.Dim("Dim", _canvas.Root, 0.55f); // captura los toques: lo de debajo no se toca con la tarjeta abierta
            _panel = ModalKit.Card("Panel", _canvas.Root, art, out _surface);
            _portrait = UiBuilder.Image("TiTino", _panel, null, Color.white);
            _portrait.preserveAspect = true;
            _title = ModalKit.Line("Title", _panel, string.Empty, 22f, Palette.FamilyWood.Base);
            _body = ModalKit.Line("Body", _panel, string.Empty, 17f, Palette.FamilyWood.Base);
            _continue = ModalKit.Cta("ContinueButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, string.Empty, 21f, Continue, out _continueLabel);
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>Está abierta.</summary>
        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Tarjeta (tests).</summary>
        public RectTransform Panel => _panel;

        /// <summary>«Continuar».</summary>
        public Button ContinueButton => _continue.Button;

        public string TitleText => _title.text;

        public string BodyText => _body.text;

        public string ContinueText => _continueLabel.text;

        /// <summary>Todos los textos (tests: ≥ 12 sp y sin recortes).</summary>
        public IEnumerable<TextMeshProUGUI> Texts()
        {
            yield return _title;
            yield return _body;
            yield return _continueLabel;
        }

        /// <summary>La abre; «Continuar» la cierra y llama a <paramref name="continued"/> (una vez).</summary>
        public void Show(Action continued)
        {
            _continued = continued ?? throw new ArgumentNullException(nameof(continued));
            _canvas.Canvas.gameObject.SetActive(true);
            _canvas.Measure();
            _title.text = Loc.T(TitleKey);
            _body.text = Loc.T(BodyKey);
            _continueLabel.text = Loc.T(ContinueKey);
            Sprite? portrait = _art.SlothPortrait(false);
            _portrait.sprite = portrait;
            _portrait.gameObject.SetActive(portrait != null);
            Layout();
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

            ids.Add("att_continue");
            rects.Add((RectTransform)_continue.Button.transform);
        }

        private void Continue()
        {
            if (!Visible)
            {
                return;
            }

            _canvas.Canvas.gameObject.SetActive(false);
            Action? continued = _continued;
            _continued = null;
            continued?.Invoke();
        }

        private void Layout()
        {
            float canvasW = _canvas.Width;
            float canvasH = _canvas.Height;
            float width = Mathf.Min(PushPrePromptCard.MaxWidthDp, canvasW - 32f);
            float inner = width - 32f;
            float y = 20f;
            if (_portrait.gameObject.activeSelf)
            {
                float d = ProceduralArt.SlothPortraitDp;
                UiBuilder.Place(_portrait.rectTransform, (width - d) * 0.5f, y, d, d);
                y += d + 10f;
            }

            y += ModalKit.PlaceLine(_title, 16f, y, inner, 30f) + 8f;
            y += ModalKit.PlaceLine(_body, 16f, y, inner, 52f) + 16f;
            ModalKit.Place(_continue, 16f, y, inner);
            y += ModalKit.HeightOf(_continue) + 16f;
            float top = Mathf.Round((canvasH - y) * 0.45f);
            ModalKit.Size(_panel, _surface, _art, Mathf.Round((canvasW - width) * 0.5f), Mathf.Max(8f, top), width, y, sharedSurface: true);
        }
    }
}
