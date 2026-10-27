#nullable enable
using System;
using System.Collections.Generic;
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
    /// <para>
    /// Entre los dos, la opción «Enviar informes de fallos» (T-L6 de <c>docs/legal/investigacion-legal-mundial.md</c> §11; D-057,
    /// añadido del 2026-10-27, que deja sin efecto la «línea sin interruptor» de Q-B12-4): se marca o se desmarca con un toque, como
    /// «Alto contraste», y quien abre la pantalla (<c>GameBootstrap.CrashReports.cs</c>, por <see cref="CrashReportsToggled"/>) lo
    /// guarda en <c>settings.crash_reports</c> [P] y enciende o apaga Crashlytics al momento. TalkBack lee su texto y si está marcada
    /// (<see cref="OptionRow"/>); crece con la fuente hasta el 130 % como el resto de opciones.
    /// </para>
    /// </summary>
    internal sealed partial class SettingsScreen
    {
        /// <summary>Id de la fila «Privacidad y datos» en la sonda (<c>AF_LAYOUT settings</c>) y valor de <c>af_settings_change.key</c>.</summary>
        public const string PrivacyId = "consent";

        /// <summary>Id de la fila «Corregir mi edad» en la sonda.</summary>
        public const string AgeId = "age_band";

        /// <summary>Id de la opción «Enviar informes de fallos» en la sonda (<c>AF_LAYOUT settings</c> y <c>settings_change</c>).</summary>
        public const string CrashReportsId = "crash_reports";

        /// <summary>Entradas que añade la sección a <c>AF_LAYOUT settings</c> (las filas a la vista).</summary>
        private int ConsentLayoutCount => (PrivacyShown ? 2 : 0) + (AgeShown ? 1 : 0);

        private Button? _privacy;
        private TextMeshProUGUI? _privacyLabel;
        private Button? _age;
        private TextMeshProUGUI? _ageLabel;
        private bool _privacyShown;
        private bool _ageShown;
        private OptionRow? _crashRow;
        private bool _crashReports = true;

        /// <summary>Hueco de la sección (null hasta <see cref="BuildConsent"/>).</summary>
        public RectTransform? ConsentSlot { get; private set; }

        /// <summary>Al tocar «Privacidad y datos». Lo fija <c>GameBootstrap</c>.</summary>
        public Action? PrivacyPressed { get; set; }

        /// <summary>Al tocar «Corregir mi edad». Lo fija <c>GameBootstrap</c>.</summary>
        public Action? AgePressed { get; set; }

        /// <summary>Al tocar «Enviar informes de fallos»: el valor nuevo de <c>settings.crash_reports</c>. Lo fija <c>GameBootstrap</c>.</summary>
        public Action<bool>? CrashReportsToggled { get; set; }

        /// <summary>Botón de la opción «Enviar informes de fallos» (null hasta <see cref="BuildConsent"/>).</summary>
        public Button? CrashReportsButton => _crashRow?.Button;

        /// <summary>Texto de la opción «Enviar informes de fallos».</summary>
        public string CrashReportsLabel => _crashRow?.Label.text ?? string.Empty;

        /// <summary>Si la opción «Enviar informes de fallos» lleva la marca (informes activados).</summary>
        public bool CrashReportsMarked => _crashRow != null && _crashRow.Selected;

        /// <summary>Se ve la opción «Enviar informes de fallos» (con «Privacidad y datos»).</summary>
        public bool CrashReportsShown => PrivacyShown && _crashRow != null;

        /// <summary>Marca o desmarca «Enviar informes de fallos» sin cerrar (al abrir y tras tocarla).</summary>
        public void MarkCrashReports(bool on)
        {
            _crashReports = on;
            _crashRow?.SetSelected(on);
        }

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
            _crashRow = Option(ConsentSlot, _art, "CrashReports", CrashReportsId, ConsentText.T(ConsentText.Keys.SettingsCrashReports),
                () => CrashReportsToggled?.Invoke(!_crashReports));
            _crashRow.SetSelected(_crashReports);
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

            int rows = AgeShown ? 3 : 2;
            UiBuilder.Place(ConsentSlot, 16f, y, inner, (rowH * rows) + (RowGapDp * (rows - 1)));
            _privacyLabel.fontSizeMax = 16f * s;
            UiBuilder.Place((RectTransform)_privacy!.transform, 0f, 0f, inner, rowH);
            _rowTops.Add(y);
            _rowHeights.Add(rowH);
            float bottom = y + rowH;

            // «Enviar informes de fallos» (T-L6), bajo «Privacidad y datos».
            _crashRow!.Place(0f, rowH + RowGapDp, inner, rowH, s);
            _rowTops.Add(bottom + RowGapDp);
            _rowHeights.Add(rowH);
            bottom += RowGapDp + rowH;
            if (AgeShown && _ageLabel != null)
            {
                _ageLabel.fontSizeMax = 16f * s;
                UiBuilder.Place((RectTransform)_age!.transform, 0f, bottom - y + RowGapDp, inner, rowH);
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

            if (CrashReportsShown && k + n < ids.Length)
            {
                ids[k + n] = CrashReportsId;
                rects[k + n] = _crashRow!.Root;
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

        // El texto de la opción «Enviar informes de fallos» a la vista (tests: ≥ 12 sp y sin recortes al 130 %).
        private IEnumerable<TextMeshProUGUI> ConsentTexts()
        {
            if (CrashReportsShown)
            {
                yield return _crashRow!.Label;
            }
        }
    }
}
