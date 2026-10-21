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

        /// <summary>Paso 2: «¿Eliminar todo?»; «Eliminar» borra.</summary>
        Confirm = 2,
    }

    /// <summary>
    /// Confirmación en dos pasos de «Eliminar cuenta y datos» (bloque B8; 08 §11, fila «Eliminación de cuenta»: «confirmación en
    /// dos pasos y aviso de que las compras no se reembolsan»), con la composición de <see cref="QuitConfirm"/>
    /// (<see cref="ModalKit"/>): cabecera con la X, la línea de lo que se borra y, en el primer paso, el aviso de las compras en
    /// rojo. El botón principal del primer paso es el que conserva («Conservar mis datos», Hoja); el que avanza va en madera, como
    /// la salida de <see cref="QuitConfirm"/>. La X cierra sin borrar en los dos pasos (y el Atrás de Android, que maneja
    /// <c>GameBootstrap</c>). No borra nada por sí mismo: «Eliminar» llama a quien lo abre (<c>GameBootstrap.Account.cs</c>), que
    /// borra y, si no puede, vuelve a este paso con <see cref="ShowFailure"/>.
    /// </summary>
    internal sealed class DeleteAccountDialog
    {
        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
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
            ModalKit.Dim("Dim", _root, 0.55f); // captura los toques: Ajustes no se toca con el diálogo abierto
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            _title = ModalKit.Header(_panel, art, string.Empty, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, onCancel);
            _body = ModalKit.Line("Body", _panel, string.Empty, 17f, Palette.FamilyWood.Base);
            _warning = ModalKit.Line("Warning", _panel, string.Empty, 16f, Palette.UiRedWarning);
            _keep = ModalKit.Cta("KeepButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, AccountText.Keep, 20f, onCancel, out _);
            _advance = ModalKit.Cta("AdvanceButton", _panel, art.CtaWood, TextStyle.PillWood, string.Empty, 18f,
                () => (Step == DeleteAccountStep.Warn ? onContinue : onConfirm)(), out _advanceLabel);
            _root.gameObject.SetActive(false);
        }

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

        /// <summary>«Continuar» en el paso 1 y «Eliminar» en el paso 2.</summary>
        public Button AdvanceButton => _advance.Button;

        /// <summary>Abre el paso 1 en un lienzo de <paramref name="canvasW"/> × <paramref name="canvasH"/> dp.</summary>
        public void ShowWarn(float canvasW, float canvasH)
        {
            Show(DeleteAccountStep.Warn, AccountText.DeleteAccount, AccountText.Body, AccountText.NoRefund, AccountText.Continue, canvasW, canvasH);
        }

        /// <summary>Pasa al paso 2.</summary>
        public void ShowConfirm()
        {
            Show(DeleteAccountStep.Confirm, AccountText.ConfirmTitle, AccountText.ConfirmBody, null, AccountText.ConfirmCta, _canvasW, _canvasH);
        }

        /// <summary>El borrado falló: sigue en el paso 2 con «No se pudo eliminar» en rojo.</summary>
        public void ShowFailure()
        {
            Show(DeleteAccountStep.Confirm, AccountText.ConfirmTitle, AccountText.ConfirmBody, AccountText.Failed, AccountText.ConfirmCta, _canvasW, _canvasH);
        }

        /// <summary>Cierra sin hacer nada.</summary>
        public void Hide()
        {
            Step = DeleteAccountStep.None;
            _root.gameObject.SetActive(false);
        }

        /// <summary>Botones visibles para <c>AF_LAYOUT</c> (sonda de desarrollo).</summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            if (!Visible)
            {
                return;
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

        private void Show(DeleteAccountStep step, string title, string body, string? warning, string advance, float canvasW, float canvasH)
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

            float width = Mathf.Min(328f, canvasW - 32f);
            float inner = width - 32f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner - 52f, 40f);
            UiBuilder.Place((RectTransform)_close.Button.transform, width - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
            y += 52f;
            y += ModalKit.PlaceLine(_body, 16f, y, inner, 48f) + 8f;
            if (warning != null)
            {
                y += ModalKit.PlaceLine(_warning, 16f, y, inner, 24f) + 10f;
            }

            // El primer paso lleva como principal el botón que conserva; el segundo, solo «Eliminar» (la X conserva).
            bool keep = step == DeleteAccountStep.Warn;
            _keep.Button.gameObject.SetActive(keep);
            if (keep)
            {
                ModalKit.Place(_keep, 16f, y, inner);
                y += ModalKit.HeightOf(_keep) + 8f;
            }

            ModalKit.Place(_advance, 16f, y, inner);
            y += ModalKit.HeightOf(_advance) + 14f;
            ModalKit.Size(_panel, _surface, _art, (canvasW - width) * 0.5f, Mathf.Round((canvasH - y) * 0.45f), width, y);
        }
    }
}
