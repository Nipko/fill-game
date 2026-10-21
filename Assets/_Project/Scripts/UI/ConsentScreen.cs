#nullable enable
using System;
using System.Collections.Generic;
using AF.IAP;
using AF.Platform;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>Lo que enseña <see cref="ConsentScreen"/>.</summary>
    public enum ConsentView
    {
        /// <summary>Nada.</summary>
        None = 0,

        /// <summary>Edad neutral: tres bandas iguales, ninguna marcada.</summary>
        Age = 1,

        /// <summary>Consentimiento: finalidades separadas y «Rechazar todo» al lado de «Aceptar todo», iguales.</summary>
        Choices = 2,

        /// <summary>«Pide a un adulto que instale el juego», sin botones.</summary>
        Blocked = 3,

        /// <summary>Segunda confirmación de una compra (13-17).</summary>
        Confirm = 4,
    }

    /// <summary>Cómo sale la línea de los anuncios en la pantalla de consentimiento.</summary>
    public enum ConsentAdsLine
    {
        /// <summary>Interruptor «Anuncios personalizados» (18+ fuera de UMP).</summary>
        Toggle = 0,

        /// <summary>Línea: los decide el aviso de UMP (EEE, Reino Unido y Suiza).</summary>
        Ump = 1,

        /// <summary>Línea: con 13-17 no son personalizados.</summary>
        Teen = 2,

        /// <summary>Línea: sin país conocido no son personalizados (Q-T3 (a): el tratamiento más estricto).</summary>
        Strict = 3,
    }

    /// <summary>
    /// Las pantallas de la edad y el consentimiento (bloque B12; 08 §2.3 y §7; 05 §7; 06 §2.1, regla 3), construidas por código
    /// con <see cref="ModalKit"/> (D-003: madera clara sobre un velo) en un lienzo propio por encima del juego, de Ajustes y de la
    /// tienda (<see cref="SortingOrder"/>):
    /// <list type="bullet">
    /// <item><b>Edad neutral</b>: «¿Cuántos años tienes?» y tres botones iguales, en orden, sin ninguno marcado ni texto que diga qué
    /// edad hace falta (la pantalla neutral de Google Play para audiencias mixtas).</item>
    /// <item><b>Consentimiento</b>: las finalidades separadas (anuncios personalizados y analítica, apagadas al abrir en el arranque:
    /// consentimiento expreso; el diagnóstico, como línea sin interruptor, 08 §2.3), y «Rechazar todo» y «Aceptar todo» uno al lado
    /// del otro con el mismo botón y el mismo tamaño (05 §7: «Rechazar» al mismo nivel que «Aceptar»); debajo, «Guardar mi
    /// elección». Desde Ajustes lleva la X y lo guardado marcado.</item>
    /// <item><b>Bloqueo</b> (&lt;13): la línea «Pide a un adulto que instale el juego», sin botones (08 §7).</item>
    /// <item><b>Doble confirmación</b> (13-17; Q-T4 (a)): el producto y su precio tal como los da la tienda, con «Cancelar» y
    /// «Comprar».</item>
    /// </list>
    /// No decide nada: llama a quien la abre (<c>GameBootstrap.Consent.cs</c>, que usa <see cref="ConsentFlow"/>). Sonda:
    /// <c>AF_LAYOUT</c> con <c>"screen":"consent"</c> y los ids de sus botones (<see cref="VisibleButtons"/>).
    /// </summary>
    internal sealed class ConsentScreen : IPurchaseConfirmation
    {
        /// <summary>Orden del lienzo: encima de Ajustes (40), la tienda (50) y los anuncios (60)… salvo la pantalla de arranque (90).</summary>
        public const int SortingOrder = 65;

        private const float RowDp = 52f;

        private readonly ProceduralArt _art;
        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly ReliefParts _close;
        private readonly TextMeshProUGUI _body;
        private readonly ReliefParts[] _ageButtons = new ReliefParts[3];
        private readonly TextMeshProUGUI[] _ageLabels = new TextMeshProUGUI[3];
        private readonly OptionRow _adsToggle;
        private readonly OptionRow _analyticsToggle;
        private readonly TextMeshProUGUI _adsLine;
        private readonly TextMeshProUGUI _diagnostics;
        private readonly ReliefParts _reject;
        private readonly TextMeshProUGUI _rejectLabel;
        private readonly ReliefParts _accept;
        private readonly TextMeshProUGUI _acceptLabel;
        private readonly Button _save;
        private readonly TextMeshProUGUI _saveLabel;
        private readonly Button _adsOptions;
        private readonly TextMeshProUGUI _adsOptionsLabel;

        private Action<AgeBand>? _onAge;
        private Action<ConsentAnswer, bool, bool>? _onAnswer;
        private Action? _onClose;
        private Action<bool>? _onConfirm;
        private Action? _onAdsOptions;
        private bool _closable;
        private ConsentAdsLine _adsMode;

        public ConsentScreen(Transform host, ProceduralArt art)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            _canvas = new OverlayCanvas("ConsentCanvas", host, SortingOrder);
            ModalKit.Dim("Dim", _canvas.Root, 0.62f);
            _panel = ModalKit.Card("Panel", _canvas.Root, art, out _surface);
            _title = ModalKit.Header(_panel, art, string.Empty, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, PressClose);
            _body = ModalKit.Line("Body", _panel, string.Empty, 17f, Palette.FamilyWood.Base);

            AgeBand[] bands = { AgeBand.Under13, AgeBand.Teen, AgeBand.Adult };
            string[] names = { "AgeUnder13", "AgeTeen", "AgeAdult" };
            for (int i = 0; i < bands.Length; i++)
            {
                AgeBand band = bands[i];
                _ageButtons[i] = ModalKit.Cta(names[i], _panel, art.CtaWood, TextStyle.PillWood, string.Empty, 18f, () => _onAge?.Invoke(band), out _ageLabels[i]);
            }

            _adsToggle = Purpose(_panel, art, "AdsToggle");
            _analyticsToggle = Purpose(_panel, art, "AnalyticsToggle");
            _adsLine = ModalKit.Line("AdsLine", _panel, string.Empty, 15f, Palette.FamilyWood.Base);
            _diagnostics = ModalKit.Line("Diagnostics", _panel, string.Empty, 15f, Palette.FamilyWood.Base);

            // «Rechazar todo» y «Aceptar todo»: el mismo botón de madera, del mismo tamaño y uno al lado del otro (05 §7).
            _reject = ModalKit.Cta("RejectAll", _panel, art.CtaWood, TextStyle.PillWood, string.Empty, 18f, () => Answer(ConsentAnswer.RejectAll), out _rejectLabel);
            _accept = ModalKit.Cta("AcceptAll", _panel, art.CtaWood, TextStyle.PillWood, string.Empty, 18f, () => Answer(ConsentAnswer.AcceptAll), out _acceptLabel);
            _save = ModalKit.Link("SaveChoice", _panel, string.Empty, () => Answer(ConsentAnswer.Save), out _saveLabel);
            _adsOptions = ModalKit.Link("AdsOptions", _panel, string.Empty, () => _onAdsOptions?.Invoke(), out _adsOptionsLabel);
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>La pantalla está abierta.</summary>
        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Lo que enseña.</summary>
        public ConsentView View { get; private set; }

        /// <summary>Lienzo propio (capturas).</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>Título a la vista.</summary>
        public string TitleText => _title.text;

        /// <summary>Línea principal a la vista.</summary>
        public string BodyText => _body.text;

        /// <summary>Se puede cerrar sin decidir (desde Ajustes).</summary>
        public bool Closable => _closable;

        public Button CloseButton => _close.Button;

        /// <summary>El botón de la banda <paramref name="band"/> en la pantalla de edad.</summary>
        public Button AgeButton(AgeBand band)
        {
            switch (band)
            {
                case AgeBand.Under13: return _ageButtons[0].Button;
                case AgeBand.Teen: return _ageButtons[1].Button;
                case AgeBand.Adult: return _ageButtons[2].Button;
                default: throw new ArgumentOutOfRangeException(nameof(band));
            }
        }

        /// <summary>Etiquetas de las bandas, en orden (&lt;13, 13-17, 18+).</summary>
        public string[] AgeLabels => new[] { _ageLabels[0].text, _ageLabels[1].text, _ageLabels[2].text };

        /// <summary>Interruptor «Anuncios personalizados» (activo solo con <see cref="ConsentAdsLine.Toggle"/>).</summary>
        public Button AdsToggleButton => _adsToggle.Button;

        /// <summary>Interruptor «Analítica».</summary>
        public Button AnalyticsToggleButton => _analyticsToggle.Button;

        /// <summary>El interruptor de anuncios se ve.</summary>
        public bool AdsToggleShown => _adsToggle.Button.gameObject.activeSelf;

        /// <summary>Anuncios personalizados marcado.</summary>
        public bool AdsSelected => _adsToggle.Selected;

        /// <summary>Analítica marcada.</summary>
        public bool AnalyticsSelected => _analyticsToggle.Selected;

        /// <summary>La línea de los anuncios (UMP o 13-17), o null si se ve el interruptor.</summary>
        public string? AdsLineText => _adsLine.gameObject.activeSelf ? _adsLine.text : null;

        public Button RejectAllButton => _reject.Button;

        public Button AcceptAllButton => _accept.Button;

        public Button SaveButton => _save;

        /// <summary>
        /// Enlace «Elegir los anuncios en el aviso de Google» (perfil <c>ump</c>, desde Ajustes): vuelve a abrir el mensaje de UMP
        /// para cambiar o retirar el consentimiento de los anuncios (05 §7: revocación en Ajustes).
        /// </summary>
        public Button AdsOptionsButton => _adsOptions;

        /// <summary>Se ve el enlace de las opciones de UMP.</summary>
        public bool AdsOptionsShown => _adsOptions.gameObject.activeSelf;

        /// <summary>«Comprar» en la doble confirmación (el botón de la derecha).</summary>
        public Button ConfirmBuyButton => _accept.Button;

        /// <summary>«Cancelar» en la doble confirmación (el de la izquierda).</summary>
        public Button ConfirmCancelButton => _reject.Button;

        /// <summary>Rectángulos de «Rechazar todo» y «Aceptar todo» en el lienzo (tests: mismo nivel).</summary>
        public RectTransform RejectRect => (RectTransform)_reject.Button.transform;

        /// <summary>Rectángulo de «Aceptar todo».</summary>
        public RectTransform AcceptRect => (RectTransform)_accept.Button.transform;

        /// <summary>Pantalla de edad neutral; con <paramref name="onClose"/>, con la X (corrección desde Ajustes).</summary>
        public void ShowAge(Action<AgeBand> onChoose, Action? onClose = null)
        {
            Clear();
            _onAge = onChoose ?? throw new ArgumentNullException(nameof(onChoose));
            _onClose = onClose;
            _closable = onClose != null;
            View = ConsentView.Age;
            _title.text = ConsentText.T(ConsentText.Keys.AgeTitle);
            _ageLabels[0].text = ConsentText.T(ConsentText.Keys.AgeUnder13);
            _ageLabels[1].text = ConsentText.T(ConsentText.Keys.AgeTeen);
            _ageLabels[2].text = ConsentText.T(ConsentText.Keys.AgeAdult);
            Open();
        }

        /// <summary>
        /// Pantalla de consentimiento: <paramref name="ads"/> dice cómo sale la línea de los anuncios; lo marcado al abrir,
        /// <paramref name="analytics"/> y <paramref name="adsPersonalized"/>; con <paramref name="onClose"/>, con la X (Ajustes); con
        /// <paramref name="onAdsOptions"/> y la línea de UMP, la línea es un enlace que vuelve a abrir el mensaje de UMP.
        /// </summary>
        public void ShowChoices(
            ConsentAdsLine ads, bool analytics, bool adsPersonalized, Action<ConsentAnswer, bool, bool> onAnswer, Action? onClose = null,
            Action? onAdsOptions = null)
        {
            Clear();
            _onAnswer = onAnswer ?? throw new ArgumentNullException(nameof(onAnswer));
            _onClose = onClose;
            _onAdsOptions = ads == ConsentAdsLine.Ump ? onAdsOptions : null;
            _closable = onClose != null;
            _adsMode = ads;
            View = ConsentView.Choices;
            _title.text = ConsentText.T(ConsentText.Keys.Title);
            _body.text = ConsentText.T(ConsentText.Keys.Body);
            _adsToggle.Rebind(string.Empty, ConsentText.T(ConsentText.Keys.PurposeAds), () => _adsToggle.SetSelected(!_adsToggle.Selected));
            _adsToggle.SetSelected(ads == ConsentAdsLine.Toggle && adsPersonalized);
            _analyticsToggle.Rebind(string.Empty, ConsentText.T(ConsentText.Keys.PurposeAnalytics), () => _analyticsToggle.SetSelected(!_analyticsToggle.Selected));
            _analyticsToggle.SetSelected(analytics);
            _adsLine.text = ConsentText.T(AdsLineKey(ads));
            _adsOptionsLabel.text = ConsentText.T(ConsentText.Keys.AdsUmpOptions);
            _diagnostics.text = ConsentText.T(ConsentText.Keys.Diagnostics);
            _rejectLabel.text = ConsentText.T(ConsentText.Keys.RejectAll);
            _acceptLabel.text = ConsentText.T(ConsentText.Keys.AcceptAll);
            _saveLabel.text = ConsentText.T(ConsentText.Keys.Save);
            Open();
        }

        /// <summary>«Pide a un adulto que instale el juego» (sin botones ni X).</summary>
        public void ShowBlocked()
        {
            Clear();
            _closable = false;
            View = ConsentView.Blocked;
            _title.text = string.Empty;
            _body.text = ConsentText.T(ConsentText.Keys.Blocked);
            Open();
        }

        /// <inheritdoc />
        public void Confirm(StoreProduct product, LocalPrice price, Action<bool> onAnswer)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            Clear();
            _onConfirm = onAnswer ?? throw new ArgumentNullException(nameof(onAnswer));
            _closable = true;
            View = ConsentView.Confirm;
            _title.text = ConsentText.T(ConsentText.Keys.ConfirmTitle);
            _body.text = ConsentText.ConfirmBody(ConsentText.T(product.NameKey), price.ToString());
            _rejectLabel.text = ConsentText.T(ConsentText.Keys.ConfirmCancel);
            _acceptLabel.text = ConsentText.T(ConsentText.Keys.ConfirmBuy);
            Open();
        }

        /// <summary>
        /// Atrás de Android: con la X (Ajustes o la doble confirmación), cierra sin decidir; en el arranque y en el bloqueo no hace
        /// nada (no se puede saltar la decisión). Devuelve si la pantalla estaba abierta (el Atrás no sigue a lo de debajo).
        /// </summary>
        public bool PressBack()
        {
            if (!Visible)
            {
                return false;
            }

            if (_closable)
            {
                PressClose();
            }

            return true;
        }

        /// <summary>Cierra sin avisar a nadie.</summary>
        public void Hide()
        {
            Clear();
            View = ConsentView.None;
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>Botones visibles para <c>AF_LAYOUT</c> (sonda de desarrollo).</summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            if (!Visible)
            {
                return;
            }

            if (_closable)
            {
                Add(ids, rects, "consent_close", _close.Button);
            }

            switch (View)
            {
                case ConsentView.Age:
                    Add(ids, rects, "age_under_13", _ageButtons[0].Button);
                    Add(ids, rects, "age_13_17", _ageButtons[1].Button);
                    Add(ids, rects, "age_18_plus", _ageButtons[2].Button);
                    break;
                case ConsentView.Choices:
                    if (AdsToggleShown)
                    {
                        Add(ids, rects, "consent_ads", _adsToggle.Button);
                    }

                    if (AdsOptionsShown)
                    {
                        Add(ids, rects, "consent_ads_options", _adsOptions);
                    }

                    Add(ids, rects, "consent_analytics", _analyticsToggle.Button);
                    Add(ids, rects, "consent_reject_all", _reject.Button);
                    Add(ids, rects, "consent_accept_all", _accept.Button);
                    Add(ids, rects, "consent_save", _save);
                    break;
                case ConsentView.Confirm:
                    Add(ids, rects, "purchase_confirm_cancel", _reject.Button);
                    Add(ids, rects, "purchase_confirm_buy", _accept.Button);
                    break;
            }
        }

        private static string AdsLineKey(ConsentAdsLine ads)
        {
            switch (ads)
            {
                case ConsentAdsLine.Ump: return ConsentText.Keys.AdsByUmp;
                case ConsentAdsLine.Strict: return ConsentText.Keys.AdsStrict;
                default: return ConsentText.Keys.AdsTeen;
            }
        }

        private static void Add(List<string> ids, List<RectTransform> rects, string id, Button button)
        {
            ids.Add(id);
            rects.Add((RectTransform)button.transform);
        }

        private void Answer(ConsentAnswer answer)
        {
            if (View == ConsentView.Confirm)
            {
                Action<bool>? confirm = _onConfirm;
                _onConfirm = null;
                Hide();
                confirm?.Invoke(answer == ConsentAnswer.AcceptAll);
                return;
            }

            if (View != ConsentView.Choices)
            {
                return;
            }

            Action<ConsentAnswer, bool, bool>? onAnswer = _onAnswer;
            bool analytics = _analyticsToggle.Selected;
            bool ads = _adsMode == ConsentAdsLine.Toggle && _adsToggle.Selected;
            onAnswer?.Invoke(answer, analytics, ads);
        }

        private void PressClose()
        {
            if (!_closable)
            {
                return;
            }

            if (View == ConsentView.Confirm)
            {
                Action<bool>? confirm = _onConfirm;
                _onConfirm = null;
                Hide();
                confirm?.Invoke(false);
                return;
            }

            Action? onClose = _onClose;
            Hide();
            onClose?.Invoke();
        }

        // Suelta los avisos de la vista anterior. Una doble confirmación que siga abierta se responde «no» (Cancelar): si otra vista
        // la tapa, la compra no se queda colgada (IsPurchasing nunca se queda en cierto).
        private void Clear()
        {
            Action<bool>? pending = _onConfirm;
            _onAge = null;
            _onAnswer = null;
            _onClose = null;
            _onConfirm = null;
            _onAdsOptions = null;
            pending?.Invoke(false);
        }

        private void Open()
        {
            _canvas.Canvas.gameObject.SetActive(true);
            _canvas.Measure();
            Layout();
        }

        private void Layout()
        {
            float cw = _canvas.Width;
            float ch = _canvas.Height;
            float w = Mathf.Min(328f, cw - 32f);
            float inner = w - 32f;
            float y = 16f;
            bool age = View == ConsentView.Age;
            bool choices = View == ConsentView.Choices;
            bool confirm = View == ConsentView.Confirm;
            bool titled = View != ConsentView.Blocked;

            _header.gameObject.SetActive(titled);
            _close.Button.gameObject.SetActive(_closable);
            if (titled)
            {
                float titleW = _closable ? inner - 52f : inner;
                UiBuilder.Place(_header.rectTransform, 16f, y, titleW, 40f);
                if (_closable)
                {
                    UiBuilder.Place((RectTransform)_close.Button.transform, w - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
                }

                y += 52f;
            }

            bool body = View != ConsentView.Age;
            _body.gameObject.SetActive(body);
            if (body)
            {
                y += ModalKit.PlaceLine(_body, 16f, y, inner, View == ConsentView.Blocked ? 96f : 48f) + 10f;
            }

            for (int i = 0; i < _ageButtons.Length; i++)
            {
                _ageButtons[i].Button.gameObject.SetActive(age);
                if (age)
                {
                    ModalKit.FitCtaLabel(_ageButtons[i], _ageLabels[i], inner, 24f, 0f);
                    ModalKit.Place(_ageButtons[i], 16f, y, inner);
                    y += ModalKit.HeightOf(_ageButtons[i]) + 8f;
                }
            }

            bool adsToggle = choices && _adsMode == ConsentAdsLine.Toggle;
            bool adsLink = choices && !adsToggle && _onAdsOptions != null;
            _adsToggle.Button.gameObject.SetActive(adsToggle);
            _adsLine.gameObject.SetActive(choices && !adsToggle && !adsLink);
            _adsOptions.gameObject.SetActive(adsLink);
            _analyticsToggle.Button.gameObject.SetActive(choices);
            _diagnostics.gameObject.SetActive(choices);
            if (choices)
            {
                float rowH = RowDp * Mathf.Max(1f, UiBuilder.TextScale);
                if (adsToggle)
                {
                    _adsToggle.Place(16f, y, inner, rowH, Mathf.Max(1f, UiBuilder.TextScale));
                    y += rowH + 8f;
                }
                else if (adsLink)
                {
                    _adsOptionsLabel.fontSizeMax = 16f * Mathf.Max(1f, UiBuilder.TextScale);
                    _adsOptionsLabel.fontSizeMin = UiBuilder.MinTextSp;
                    _adsOptionsLabel.textWrappingMode = TextWrappingModes.Normal;
                    UiBuilder.Place((RectTransform)_adsOptions.transform, 16f, y, inner, rowH);
                    y += rowH + 8f;
                }
                else
                {
                    y += ModalKit.PlaceLine(_adsLine, 16f, y, inner, 40f) + 8f;
                }

                _analyticsToggle.Place(16f, y, inner, rowH, Mathf.Max(1f, UiBuilder.TextScale));
                y += rowH + 8f;
                y += ModalKit.PlaceLine(_diagnostics, 16f, y, inner, 40f) + 10f;
            }

            bool pair = choices || confirm;
            _reject.Button.gameObject.SetActive(pair);
            _accept.Button.gameObject.SetActive(pair);
            if (pair)
            {
                float half = (inner - 8f) * 0.5f;
                ModalKit.FitCtaLabel(_reject, _rejectLabel, half, 16f, 0f);
                ModalKit.FitCtaLabel(_accept, _acceptLabel, half, 16f, 0f);

                // Los dos con el mismo alto: el de la etiqueta que más necesite.
                float face = Mathf.Max(ModalKit.FaceHeightOf(_reject), ModalKit.FaceHeightOf(_accept));
                ModalKit.StretchToFace(_reject, face);
                ModalKit.StretchToFace(_accept, face);
                ModalKit.Place(_reject, 16f, y, half);
                ModalKit.Place(_accept, 16f + half + 8f, y, half);
                y += Mathf.Max(ModalKit.HeightOf(_reject), ModalKit.HeightOf(_accept)) + 6f;
            }

            _save.gameObject.SetActive(choices);
            if (choices)
            {
                _saveLabel.fontSizeMax = 16f * Mathf.Max(1f, UiBuilder.TextScale);
                _saveLabel.fontSizeMin = UiBuilder.MinTextSp;
                float linkH = 48f * Mathf.Max(1f, UiBuilder.TextScale);
                UiBuilder.Place((RectTransform)_save.transform, 16f, y, inner, linkH);
                y += linkH;
            }

            y += 14f;
            float top = Mathf.Max(_canvas.SafeTop + 8f, Mathf.Round((ch - y) * 0.45f));
            ModalKit.Size(_panel, _surface, _art, Mathf.Round((cw - w) * 0.5f), top, w, y);
        }

        // Una finalidad (OptionRow, la fila de Ajustes; Q-B12-14 (a)): frases largas, que parten en líneas, a 17 sp.
        private static OptionRow Purpose(Transform parent, ProceduralArt art, string name)
        {
            return new OptionRow(parent, art, name, string.Empty, string.Empty, null, 17f, UiBuilder.MinTextSp, wrap: true);
        }
    }
}
