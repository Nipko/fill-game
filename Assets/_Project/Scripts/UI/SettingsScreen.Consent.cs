#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// «Privacidad» en Ajustes (bloque B12; 05 §7: «revocación en Ajustes»; 08 §7 y 05 §7: la edad «se corrige una vez desde
    /// Ajustes»), en el hueco <see cref="ConsentSlot"/>, tras «Pista por inactividad» y antes de «Sobre la Amazonía»: dos enlaces de
    /// 48 dp de alto, «Privacidad y datos» (abre la pantalla de consentimiento con lo guardado marcado) y «Corregir mi edad» (la
    /// pantalla neutral; desaparece tras usarla). Los pulsa <c>GameBootstrap.Consent.cs</c> por <see cref="PrivacyPressed"/> y
    /// <see cref="AgePressed"/>. Parcial nuevo: <c>SettingsScreen.cs</c> solo gana las llamadas a <see cref="BuildConsent"/>
    /// (constructor), <see cref="LayoutConsent"/> (<c>Layout</c>) y <see cref="ConsentLayoutIds"/> (<c>EmitLayout</c>), con el
    /// parche <c>B12-ui.diff</c>. Sin el consentimiento en marcha (tests), el hueco no se ve.
    /// </summary>
    internal sealed partial class SettingsScreen
    {
        /// <summary>Id de la fila «Privacidad y datos» en la sonda (<c>AF_LAYOUT settings</c>) y valor de <c>af_settings_change.key</c>.</summary>
        public const string PrivacyId = "consent";

        /// <summary>Id de la fila «Corregir mi edad» en la sonda.</summary>
        public const string AgeId = "age_band";

        /// <summary>Entradas que añade la sección a <c>AF_LAYOUT settings</c> (las filas a la vista).</summary>
        private int ConsentLayoutCount => (PrivacyShown ? 1 : 0) + (AgeShown ? 1 : 0);

        private Button? _privacy;
        private TextMeshProUGUI? _privacyLabel;
        private Button? _age;
        private TextMeshProUGUI? _ageLabel;
        private bool _privacyShown;
        private bool _ageShown;

        /// <summary>Hueco de la sección (null hasta <see cref="BuildConsent"/>).</summary>
        public RectTransform? ConsentSlot { get; private set; }

        /// <summary>Al tocar «Privacidad y datos». Lo fija <c>GameBootstrap</c>.</summary>
        public Action? PrivacyPressed { get; set; }

        /// <summary>Al tocar «Corregir mi edad». Lo fija <c>GameBootstrap</c>.</summary>
        public Action? AgePressed { get; set; }

        /// <summary>Enlace «Privacidad y datos» (null hasta <see cref="BuildConsent"/>).</summary>
        public Button? PrivacyButton => _privacy;

        /// <summary>Enlace «Corregir mi edad» (null hasta <see cref="BuildConsent"/>).</summary>
        public Button? AgeButton => _age;

        /// <summary>Se ve «Privacidad y datos».</summary>
        public bool PrivacyShown => _privacyShown && _privacy != null;

        /// <summary>Se ve «Corregir mi edad».</summary>
        public bool AgeShown => _ageShown && _age != null;

        /// <summary>
        /// Desplaza Ajustes hasta que la sección de privacidad quede arriba del visor (capturas de Q-B12-16, como
        /// <see cref="ScrollToAbout"/>). Sin la sección o sin desplazamiento, nada.
        /// </summary>
        public void ScrollToPrivacy()
        {
            float range = _content.rect.height - _viewport.rect.height;
            if (!PrivacyShown || ConsentSlot == null || !_scroll.vertical || range <= 0f)
            {
                return;
            }

            float top = Mathf.Clamp(-ConsentSlot.anchoredPosition.y - 8f, 0f, range);
            _scroll.verticalNormalizedPosition = 1f - (top / range);
            UpdateFades();
        }

        /// <summary>
        /// Qué filas se ven (al abrir Ajustes y tras corregir la edad); con la pantalla abierta, la vuelve a colocar.
        /// </summary>
        public void ShowConsentRows(bool privacy, bool age)
        {
            _privacyShown = privacy;
            _ageShown = privacy && age;
            if (ConsentSlot != null)
            {
                ConsentSlot.gameObject.SetActive(_privacyShown);
            }

            if (_age != null)
            {
                _age.gameObject.SetActive(_ageShown);
            }

            if (Visible)
            {
                Layout();
                EmitLayout();
            }
        }

        /// <summary>Crea el hueco y los enlaces (una vez; en el constructor). Ocultos hasta <see cref="ShowConsentRows"/>.</summary>
        private void BuildConsent()
        {
            if (ConsentSlot != null)
            {
                return;
            }

            ConsentSlot = UiBuilder.Rect("ConsentSlot", _content);
            _privacy = ModalKit.Link("Privacy", ConsentSlot, ConsentText.T(ConsentText.Keys.SettingsPrivacy), () => PrivacyPressed?.Invoke(), out TextMeshProUGUI privacy);
            _privacyLabel = privacy;
            privacy.fontSizeMin = MinTextSp;
            _age = ModalKit.Link("AgeBand", ConsentSlot, ConsentText.T(ConsentText.Keys.SettingsAge), () => AgePressed?.Invoke(), out TextMeshProUGUI age);
            _ageLabel = age;
            age.fontSizeMin = MinTextSp;
            ConsentSlot.gameObject.SetActive(false);
            _age.gameObject.SetActive(false);
        }

        /// <summary>
        /// Coloca las filas desde <paramref name="y"/> (dp desde arriba del contenido; ya con el hueco de sección), con
        /// <paramref name="inner"/> de ancho y <paramref name="rowH"/> de alto, las apunta para el corte del visor y devuelve el borde
        /// de abajo. Sin la sección a la vista, devuelve <paramref name="fallback"/> (el borde de antes, sin el hueco).
        /// </summary>
        private float LayoutConsent(float y, float fallback, float inner, float rowH, float s)
        {
            if (!PrivacyShown || ConsentSlot == null || _privacyLabel == null)
            {
                return fallback;
            }

            int rows = AgeShown ? 2 : 1;
            UiBuilder.Place(ConsentSlot, 16f, y, inner, (rowH * rows) + (RowGapDp * (rows - 1)));
            _privacyLabel.fontSizeMax = 16f * s;
            UiBuilder.Place((RectTransform)_privacy!.transform, 0f, 0f, inner, rowH);
            _rowTops.Add(y);
            _rowHeights.Add(rowH);
            float bottom = y + rowH;
            if (AgeShown && _ageLabel != null)
            {
                _ageLabel.fontSizeMax = 16f * s;
                UiBuilder.Place((RectTransform)_age!.transform, 0f, rowH + RowGapDp, inner, rowH);
                _rowTops.Add(bottom + RowGapDp);
                _rowHeights.Add(rowH);
                bottom += RowGapDp + rowH;
            }

            return bottom;
        }

        /// <summary>Escribe las filas a la vista en <paramref name="ids"/> y <paramref name="rects"/> desde <paramref name="k"/>; devuelve las escritas.</summary>
        private int ConsentLayoutIds(string[] ids, RectTransform[] rects, int k)
        {
            int n = 0;
            if (PrivacyShown && k < ids.Length)
            {
                ids[k] = PrivacyId;
                rects[k] = (RectTransform)_privacy!.transform;
                n++;
            }

            if (AgeShown && k + n < ids.Length)
            {
                ids[k + n] = AgeId;
                rects[k + n] = (RectTransform)_age!.transform;
                n++;
            }

            return n;
        }
    }
}
