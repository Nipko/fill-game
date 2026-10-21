#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// «Eliminar cuenta y datos» en Ajustes (bloque B8; 08 §11, fila «Eliminación de cuenta»: «en la app, Ajustes › "Eliminar
    /// cuenta y datos" con confirmación en dos pasos»; 08 §13.1 #7), en el hueco <see cref="AccountSlot"/>, al final de la lista:
    /// un enlace subrayado (pie de modal, 07 §7.5) de 48 dp de alto que abre <see cref="DeleteAccountDialog"/> por
    /// <see cref="DeleteAccountPressed"/> (lo fija <c>GameBootstrap.Account.cs</c>). Parcial nuevo: <c>SettingsScreen.cs</c> solo
    /// gana las llamadas a <see cref="BuildAccount"/> (constructor), <see cref="LayoutAccount"/> (<c>Layout</c>) y
    /// <see cref="AccountLayoutIds"/> (<c>EmitLayout</c>), con el parche <c>B8-ui.diff</c>. El ID de jugador visible que pide la
    /// vía web (08 §11) llega con Firebase Auth: hoy no hay UID [P].
    /// </summary>
    internal sealed partial class SettingsScreen
    {
        /// <summary>Id de la fila en la sonda (<c>AF_LAYOUT settings</c>).</summary>
        public const string DeleteAccountId = "delete_account";

        /// <summary>Entradas que añade la sección a <c>AF_LAYOUT settings</c>.</summary>
        private const int AccountLayoutCount = 1;

        private Button? _deleteAccount;
        private TextMeshProUGUI? _deleteAccountLabel;

        /// <summary>Hueco de «Eliminar cuenta y datos» (null hasta <see cref="BuildAccount"/>).</summary>
        public RectTransform? AccountSlot { get; private set; }

        /// <summary>Al tocar «Eliminar cuenta y datos». Lo fija <c>GameBootstrap</c>.</summary>
        public Action? DeleteAccountPressed { get; set; }

        /// <summary>Enlace «Eliminar cuenta y datos» (null hasta <see cref="BuildAccount"/>).</summary>
        public Button? DeleteAccountButton => _deleteAccount;

        /// <summary>Texto del enlace.</summary>
        public string? DeleteAccountLabel => _deleteAccountLabel?.text;

        /// <summary>Raíz del lienzo propio de Ajustes: el diálogo de dos pasos va encima, dentro del mismo lienzo.</summary>
        public RectTransform OverlayRoot => _canvas.Root;

        /// <summary>Crea el hueco y el enlace bajo el contenido desplazable (una vez; en el constructor).</summary>
        private void BuildAccount()
        {
            if (AccountSlot != null)
            {
                return;
            }

            AccountSlot = UiBuilder.Rect("AccountSlot", _content);
            _deleteAccount = ModalKit.Link("DeleteAccount", AccountSlot, AccountText.DeleteAccount, () => DeleteAccountPressed?.Invoke(), out TextMeshProUGUI label);
            _deleteAccountLabel = label;
            label.fontSizeMin = MinTextSp;
        }

        /// <summary>
        /// Coloca la fila en <paramref name="y"/> (dp desde arriba del contenido), con <paramref name="inner"/> de ancho y
        /// <paramref name="rowH"/> de alto, la apunta para el corte del visor y devuelve el borde de abajo.
        /// </summary>
        private float LayoutAccount(float y, float inner, float rowH, float s)
        {
            if (AccountSlot == null || _deleteAccount == null || _deleteAccountLabel == null)
            {
                return y;
            }

            _deleteAccountLabel.fontSizeMax = 16f * s;
            UiBuilder.Place(AccountSlot, 16f, y, inner, rowH);
            UiBuilder.Place((RectTransform)_deleteAccount.transform, 0f, 0f, inner, rowH);
            _rowTops.Add(y);
            _rowHeights.Add(rowH);
            return y + rowH;
        }

        /// <summary>Escribe la fila en <paramref name="ids"/> y <paramref name="rects"/> desde <paramref name="k"/>; devuelve las escritas.</summary>
        private int AccountLayoutIds(string[] ids, RectTransform[] rects, int k)
        {
            if (_deleteAccount == null || k >= ids.Length)
            {
                return 0;
            }

            ids[k] = DeleteAccountId;
            rects[k] = (RectTransform)_deleteAccount.transform;
            return 1;
        }
    }
}
