#nullable enable
using System;
using System.Collections.Generic;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>Paso del diálogo «Eliminar cuenta y datos».</summary>
    internal enum DeleteAccountStep
    {
        /// <summary>Cerrado.</summary>
        None = 0,

        /// <summary>Paso 1: qué se borra y que las compras no se reembolsan; «Conservar mis datos» o «Continuar».</summary>
        Warn = 1,

        /// <summary>Paso 2: «¿Eliminar todo?»; «Eliminar» borra (y, tras un fallo, «Reintentar» vuelve a probar).</summary>
        Confirm = 2,

        /// <summary>
        /// «Eliminando tu cuenta y tus datos…»: el borrado está en el servidor. Los puntos de espera se mueven (quietos con
        /// «Reducir movimiento»), los botones no responden y el Atrás de Android no lo cierra.
        /// </summary>
        Deleting = 3,

        /// <summary>«Tu cuenta y tus datos se eliminaron.»: sin botones, un momento antes de volver a la primera ejecución.</summary>
        Done = 4,
    }

    /// <summary>
    /// Confirmación en dos pasos de «Eliminar cuenta y datos» (bloque B8; 08 §11, fila «Eliminación de cuenta»: «confirmación en
    /// dos pasos y aviso de que las compras no se reembolsan»), con la composición de <see cref="QuitConfirm"/>
    /// (<see cref="ModalKit"/>): cabecera con la X, la línea de lo que se borra y, en el primer paso, el aviso de las compras en
    /// rojo. El botón principal del primer paso es el que conserva («Conservar mis datos», Hoja); el que avanza va en madera, como
    /// la salida de <see cref="QuitConfirm"/>. La X cierra sin borrar en los dos pasos (y el Atrás de Android, que maneja
    /// <c>GameBootstrap</c>). No borra nada por sí mismo: «Eliminar» llama a quien lo abre (<c>GameBootstrap.Account.cs</c>), que
    /// borra y, si no puede, vuelve a este paso con <see cref="ShowFailure"/>.
    /// <para>Mientras espera al servidor (<see cref="ShowDeleting"/>) enseña «Eliminando tu cuenta y tus datos…» con
    /// <see cref="WaitDots"/> y los botones desactivados; al terminar, «Tu cuenta y tus datos se eliminaron.»
    /// (<see cref="ShowDone"/>), que se funde (<see cref="SetFade"/>) antes de la recarga; tras un fallo, el botón dice
    /// «Reintentar».</para>
    /// </summary>
    internal sealed class DeleteAccountDialog
    {
        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly Image _dim;
        private readonly CanvasGroup _panelGroup;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly ReliefParts _close;
        private readonly TextMeshProUGUI _body;
        private readonly TextMeshProUGUI _warning;
        private readonly ReliefParts _keep;
        private readonly ReliefParts _advance;
        private readonly TextMeshProUGUI _advanceLabel;
        private readonly WaitDots _dots;
        private float _canvasW;
        private float _canvasH;

        public DeleteAccountDialog(Transform parent, ProceduralArt art, Action onContinue, Action onConfirm, Action onCancel)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            if (onContinue == null)
            {
                throw new ArgumentNullException(nameof(onContinue));
            }

            if (onConfirm == null)
            {
                throw new ArgumentNullException(nameof(onConfirm));
            }

            if (onCancel == null)
            {
                throw new ArgumentNullException(nameof(onCancel));
            }

            _root = UiBuilder.Rect("DeleteAccountDialog", parent);
            _dim = ModalKit.Dim("Dim", _root, DimAlpha); // captura los toques: Ajustes no se toca con el diálogo abierto
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            _panelGroup = _panel.gameObject.AddComponent<CanvasGroup>();
            _title = ModalKit.Header(_panel, art, string.Empty, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, onCancel);
            _body = ModalKit.Line("Body", _panel, string.Empty, 17f, Palette.FamilyWood.Base);
            _warning = ModalKit.Line("Warning", _panel, string.Empty, 16f, Palette.UiRedWarning);
            _keep = ModalKit.Cta("KeepButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, AccountText.Keep, 20f, onCancel, out _);
            _advance = ModalKit.Cta("AdvanceButton", _panel, art.CtaWood, TextStyle.PillWood, string.Empty, 18f,
                () => (Step == DeleteAccountStep.Warn ? onContinue : onConfirm)(), out _advanceLabel);
            _dots = new WaitDots("WaitDots", _panel, art.Disc, Palette.FamilyWood.Base);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Alfa del velo (el de siempre de este diálogo).</summary>
        public const float DimAlpha = 0.55f;

        /// <summary>Paso a la vista.</summary>
        public DeleteAccountStep Step { get; private set; }

        /// <summary>El diálogo está abierto.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Raíz (tests de orden de capas).</summary>
        public RectTransform Root => _root;

        /// <summary>Título a la vista.</summary>
        public string TitleText => _title.text;

        /// <summary>Línea principal a la vista.</summary>
        public string BodyText => _body.text;

        /// <summary>Línea en rojo (el aviso de las compras o el fallo), o null si no se ve.</summary>
        public string? WarningText => _warning.gameObject.activeSelf ? _warning.text : null;

        /// <summary>Texto del botón que avanza («Continuar» o «Eliminar»).</summary>
        public string AdvanceText => _advanceLabel.text;

        public Button CloseButton => _close.Button;

        /// <summary>«Conservar mis datos» (solo en el paso 1).</summary>
        public Button KeepButton => _keep.Button;

        /// <summary>«Continuar» en el paso 1, «Eliminar» en el paso 2 y «Reintentar» tras un fallo.</summary>
        public Button AdvanceButton => _advance.Button;

        /// <summary>Los puntos de espera (tests).</summary>
        public WaitDots Dots => _dots;

        /// <summary>Espera al servidor o ya terminó: ni la X ni Atrás lo cierran.</summary>
        public bool Busy => Step == DeleteAccountStep.Deleting || Step == DeleteAccountStep.Done;

        /// <summary>El botón que avanza se ve y responde (desactivado mientras se espera).</summary>
        public bool AdvanceInteractable => _advance.Button.interactable && _advance.Button.gameObject.activeSelf;

        /// <summary>La X se ve y responde (desactivada mientras se espera).</summary>
        public bool CloseInteractable => _close.Button.interactable && _close.Button.gameObject.activeSelf;

        /// <summary>Alfa de la tarjeta (1 salvo en el fundido final).</summary>
        public float PanelAlpha => _panelGroup.alpha;

        /// <summary>Alfa del velo (<see cref="DimAlpha"/> salvo en el fundido final, que llega a 1).</summary>
        public float VeilAlpha => _dim.color.a;

        /// <summary>Tarjeta (tests de encaje).</summary>
        public RectTransform Panel => _panel;

        /// <summary>Abre el paso 1 en un lienzo de <paramref name="canvasW"/> × <paramref name="canvasH"/> dp.</summary>
        public void ShowWarn(float canvasW, float canvasH)
        {
            Show(DeleteAccountStep.Warn, AccountText.DeleteAccount, AccountText.Body, AccountText.NoRefund, AccountText.Continue, canvasW, canvasH, false);
        }

        /// <summary>Pasa al paso 2.</summary>
        public void ShowConfirm()
        {
            Show(DeleteAccountStep.Confirm, AccountText.ConfirmTitle, AccountText.ConfirmBody, null, AccountText.ConfirmCta, _canvasW, _canvasH, false);
        }

        /// <summary>El borrado falló: sigue en el paso 2 con «No se pudo eliminar» en rojo y «Reintentar» (la X sigue cerrando).</summary>
        public void ShowFailure()
        {
            Show(DeleteAccountStep.Confirm, AccountText.ConfirmTitle, AccountText.ConfirmBody, AccountText.Failed, AccountText.Retry, _canvasW, _canvasH, false);
        }

        /// <summary>
        /// Espera al servidor: «Eliminando tu cuenta y tus datos…» con los puntos (quietos si <paramref name="still"/>, con
        /// «Reducir movimiento») y los botones desactivados.
        /// </summary>
        public void ShowDeleting(bool still)
        {
            Show(DeleteAccountStep.Deleting, AccountText.ConfirmTitle, AccountText.Deleting, null, AccountText.ConfirmCta, _canvasW, _canvasH, still);
        }

        /// <summary>«Tu cuenta y tus datos se eliminaron.», sin cabecera ni botones.</summary>
        public void ShowDone()
        {
            Show(DeleteAccountStep.Done, AccountText.DeleteAccount, AccountText.Done, null, string.Empty, _canvasW, _canvasH, false);
        }

        /// <summary>Avanza los puntos de espera.</summary>
        public void Tick(float dtMs)
        {
            _dots.Tick(dtMs);
        }

        /// <summary>
        /// Fundido final (<paramref name="t"/> de 0 a 1): la tarjeta se desvanece y el velo se oscurece hasta cubrirlo todo, para
        /// que la recarga no sea un salto.
        /// </summary>
        public void SetFade(float t)
        {
            t = Mathf.Clamp01(t);
            _panelGroup.alpha = 1f - t;
            Color c = _dim.color;
            c.a = Mathf.Lerp(DimAlpha, 1f, t);
            _dim.color = c;
        }

        /// <summary>Cierra sin hacer nada.</summary>
        public void Hide()
        {
            Step = DeleteAccountStep.None;
            _dots.Hide();
            _root.gameObject.SetActive(false);
        }

        /// <summary>Botones visibles para <c>AF_LAYOUT</c> (sonda de desarrollo).</summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            if (!Visible || Busy)
            {
                return; // esperando al servidor o ya borrado: nada que tocar
            }

            ids.Add("delete_account_close");
            rects.Add((RectTransform)_close.Button.transform);
            if (_keep.Button.gameObject.activeSelf)
            {
                ids.Add("delete_account_keep");
                rects.Add((RectTransform)_keep.Button.transform);
            }

            ids.Add(Step == DeleteAccountStep.Warn ? "delete_account_continue" : "delete_account_confirm");
            rects.Add((RectTransform)_advance.Button.transform);
        }

        private void Show(DeleteAccountStep step, string title, string body, string? warning, string advance, float canvasW, float canvasH, bool still)
        {
            Step = step;
            _canvasW = canvasW;
            _canvasH = canvasH;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            UiBuilder.Stretch(_root);
            _title.text = title;
            _body.text = body;
            _warning.text = warning ?? string.Empty;
            _warning.gameObject.SetActive(warning != null);
            _advanceLabel.text = advance;
            bool busy = step == DeleteAccountStep.Deleting;
            bool done = step == DeleteAccountStep.Done;
            UiBuilder.SetEnabled(_close.Button, !busy);
            UiBuilder.SetEnabled(_advance.Button, !busy);
            _close.Button.gameObject.SetActive(!done);
            _advance.Button.gameObject.SetActive(!done);
            SetFade(0f);

            float width = Mathf.Min(328f, canvasW - 32f);
            float inner = width - 32f;
            float y = 16f;
            // La confirmación final va sin cabecera: una tarjeta con una sola línea, como un aviso breve.
            _header.gameObject.SetActive(!done);
            if (!done)
            {
                UiBuilder.Place(_header.rectTransform, 16f, y, inner - 52f, 40f);
                UiBuilder.Place((RectTransform)_close.Button.transform, width - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
                y += 52f;
            }
            else
            {
                y += 12f;
            }

            y += ModalKit.PlaceLine(_body, 16f, y, inner, 48f) + 8f;
            if (warning != null)
            {
                y += ModalKit.PlaceLine(_warning, 16f, y, inner, 24f) + 10f;
            }

            if (busy)
            {
                _dots.Show(16f + ((inner - WaitDots.WidthDp) * 0.5f), y + 2f, still);
                y += WaitDots.DotDp + 16f;
            }
            else
            {
                _dots.Hide();
            }

            // El primer paso lleva como principal el botón que conserva; el segundo, solo «Eliminar» (la X conserva).
            bool keep = step == DeleteAccountStep.Warn;
            _keep.Button.gameObject.SetActive(keep);
            if (keep)
            {
                ModalKit.Place(_keep, 16f, y, inner);
                y += ModalKit.HeightOf(_keep) + 8f;
            }

            if (!done)
            {
                ModalKit.Place(_advance, 16f, y, inner);
                y += ModalKit.HeightOf(_advance) + 14f;
            }
            else
            {
                y += 8f;
            }

            ModalKit.Size(_panel, _surface, _art, (canvasW - width) * 0.5f, Mathf.Round((canvasH - y) * 0.45f), width, y);
        }
    }
}
