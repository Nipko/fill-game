#nullable enable
using System;
using AF.Presentation;
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
    /// <see cref="AccountLayoutIds"/> (<c>EmitLayout</c>), con el parche <c>B8-ui.diff</c>. Encima del enlace, la línea del código de
    /// borrado para la web (F3-5; 08 §11; Q-B13-21: el código de <c>GetAccountCode</c>, nunca el UID), que pone
    /// <see cref="SetAccountCode"/>; sin código que enseñar (banda &lt; 13 o sin backend), no sale.
    /// </summary>
    internal sealed partial class SettingsScreen
    {
        /// <summary>Id de la fila en la sonda (<c>AF_LAYOUT settings</c>).</summary>
        public const string DeleteAccountId = "delete_account";

        /// <summary>Entradas que añade la sección a <c>AF_LAYOUT settings</c>.</summary>
        private const int AccountLayoutCount = 1;

        private Button? _deleteAccount;
        private TextMeshProUGUI? _deleteAccountLabel;
        private TextMeshProUGUI? _accountCode;
        private TextMeshProUGUI? _leagueCountry;

        /// <summary>Hueco de «Eliminar cuenta y datos» (null hasta <see cref="BuildAccount"/>).</summary>
        public RectTransform? AccountSlot { get; private set; }

        /// <summary>Al tocar «Eliminar cuenta y datos». Lo fija <c>GameBootstrap</c>.</summary>
        public Action? DeleteAccountPressed { get; set; }

        /// <summary>Enlace «Eliminar cuenta y datos» (null hasta <see cref="BuildAccount"/>).</summary>
        public Button? DeleteAccountButton => _deleteAccount;

        /// <summary>Texto del enlace.</summary>
        public string? DeleteAccountLabel => _deleteAccountLabel?.text;

        /// <summary>La línea del código de borrado para la web (F3-5), o null si no se ve.</summary>
        public string? AccountCodeText => _accountCode != null && _accountCode.gameObject.activeSelf ? _accountCode.text : null;

        /// <summary>
        /// Pone la línea del código de borrado (<paramref name="text"/> ya compuesto con <c>AccountText</c>), o la quita con null. Con
        /// Ajustes abierta, la rehace.
        /// </summary>
        public void SetAccountCode(string? text)
        {
            if (_accountCode == null)
            {
                return;
            }

            bool shown = !string.IsNullOrEmpty(text);
            if (shown == _accountCode.gameObject.activeSelf && string.Equals(_accountCode.text, text ?? string.Empty, StringComparison.Ordinal))
            {
                return;
            }

            _accountCode.text = text ?? string.Empty;
            _accountCode.gameObject.SetActive(shown);
            if (Visible)
            {
                Layout();
            }
        }

        /// <summary>«País en la Liga: {país}» (D-046), o null si no se ve.</summary>
        public string? LeagueCountryText => _leagueCountry != null && _leagueCountry.gameObject.activeSelf ? _leagueCountry.text : null;

        /// <summary>
        /// Pone la fila «País en la Liga: {país}» (D-046; liga.md §1: sin poder cambiarlo en la 1.0), ya compuesta con
        /// <c>LeagueText</c>, o la quita con null (sin Liga o sin país). Con Ajustes abierta, la rehace.
        /// </summary>
        public void SetLeagueCountry(string? text)
        {
            if (_leagueCountry == null)
            {
                return;
            }

            bool shown = !string.IsNullOrEmpty(text);
            if (shown == _leagueCountry.gameObject.activeSelf && string.Equals(_leagueCountry.text, text ?? string.Empty, StringComparison.Ordinal))
            {
                return;
            }

            _leagueCountry.text = text ?? string.Empty;
            _leagueCountry.gameObject.SetActive(shown);
            if (Visible)
            {
                Layout();
            }
        }

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
            _leagueCountry = UiBuilder.Label("LeagueCountry", AccountSlot, TextStyle.Body, string.Empty, 14f, Palette.FamilyWood.Base, TextAlignmentOptions.Left);
            _leagueCountry.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_leagueCountry, MinTextSp, 14f);
            _leagueCountry.gameObject.SetActive(false);
            _accountCode = UiBuilder.Label("AccountCode", AccountSlot, TextStyle.Body, string.Empty, 14f, Palette.FamilyWood.Base, TextAlignmentOptions.Left);
            _accountCode.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_accountCode, MinTextSp, 14f);
            _accountCode.gameObject.SetActive(false);
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
            float leagueH = 0f;
            if (_leagueCountry != null && _leagueCountry.gameObject.activeSelf)
            {
                // D-046: «País en la Liga: {país}», encima del código, en las líneas que pida con la escala de texto.
                _leagueCountry.fontSizeMax = 14f * s;
                _leagueCountry.fontSizeMin = Mathf.Max(MinTextSp, UiBuilder.MinTextSp * s);
                leagueH = Mathf.Ceil(Mathf.Max(20f * s, UiBuilder.MinTextHeight(_leagueCountry, inner))) + 4f;
                UiBuilder.Place(_leagueCountry.rectTransform, 0f, 0f, inner, leagueH - 4f);
                _rowTops.Add(y);
                _rowHeights.Add(leagueH);
            }

            float codeH = 0f;
            if (_accountCode != null && _accountCode.gameObject.activeSelf)
            {
                // F3-5: el código, en las líneas que pida (hasta tres con la escala de texto), encima del enlace. D-033, punto 11:
                // nunca por debajo de 12 sp × escala (02 §12); el código va en grupos con guiones (AccountText.CodeGroups), así que
                // la línea se corta entre grupos en lugar de encoger la letra.
                _accountCode.fontSizeMax = 14f * s;
                _accountCode.fontSizeMin = Mathf.Max(MinTextSp, UiBuilder.MinTextSp * s);
                codeH = Mathf.Ceil(Mathf.Max(20f * s, UiBuilder.MinTextHeight(_accountCode, inner))) + 4f;
                UiBuilder.Place(_accountCode.rectTransform, 0f, leagueH, inner, codeH - 4f);
                _rowTops.Add(y + leagueH);
                _rowHeights.Add(codeH);
            }

            codeH += leagueH;
            UiBuilder.Place(AccountSlot, 16f, y, inner, codeH + rowH);
            UiBuilder.Place((RectTransform)_deleteAccount.transform, 0f, codeH, inner, rowH);
            _rowTops.Add(y + codeH);
            _rowHeights.Add(rowH);
            return y + codeH + rowH;
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
