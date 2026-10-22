#nullable enable
using System;
using System.Collections.Generic;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Fila «Avisos» de Ajustes (06 §8.1: «después, solo un acceso en Ajustes»; nota de B6, §3 y §6; <c>settings.push</c>), en su
    /// hueco (<see cref="PushSlot"/>), tras «Reducir movimiento» y «Alto contraste» (B9) y antes de «Privacidad y datos» (B12): una
    /// opción que se marca o se desmarca (<see cref="OptionRow"/>) con el estado escrito dentro de la placa, bajo «Avisos»
    /// («Activados», «Desactivados» o, con el teléfono bloqueándolos, «Actívalos en los ajustes del teléfono.»), y, solo en ese
    /// último caso, el enlace «Abrir ajustes del teléfono» debajo. Toda la placa es el área táctil; lo que hace un toque lo decide
    /// quien abre la pantalla (<c>GameBootstrap.PushUi.cs</c>, por <see cref="PushPressed"/> y <see cref="PushOpenPhonePressed"/>).
    /// TalkBack lee la fila con su estado completo («Avisos: activados»…; <see cref="PushText.A11yLabel"/>) y si está marcada (B9).
    /// Sin servicio de avisos (o con la banda &lt;13), el hueco no se ve.
    /// </summary>
    internal sealed partial class SettingsScreen
    {
        /// <summary>Id de la fila en la sonda (<c>AF_LAYOUT settings</c>) y clave de <c>settings_change</c> [P]: <c>push</c>.</summary>
        public const string PushId = "push";

        /// <summary>Id del enlace «Abrir ajustes del teléfono» en la sonda.</summary>
        public const string PushOpenPhoneId = "push_open_phone";

        /// <summary>Cuerpo del estado bajo «Avisos» al 100 % (sp).</summary>
        public const float PushStateSp = 14f;

        private OptionRow? _pushRow;
        private TextMeshProUGUI? _pushState;
        private Button? _pushOpenPhone;
        private TextMeshProUGUI? _pushOpenPhoneLabel;
        private PushRowState _push = PushRowState.Hidden;

        /// <summary>Hueco de la fila (null hasta <see cref="BuildPush"/>).</summary>
        public RectTransform? PushSlot { get; private set; }

        /// <summary>Al tocar la fila. Lo fija <c>GameBootstrap</c>.</summary>
        public Action? PushPressed { get; set; }

        /// <summary>Al tocar «Abrir ajustes del teléfono». Lo fija <c>GameBootstrap</c>.</summary>
        public Action? PushOpenPhonePressed { get; set; }

        /// <summary>El estado que enseña la fila.</summary>
        public PushRowState PushState => _push;

        /// <summary>Se ve la fila.</summary>
        public bool PushShown => _push != PushRowState.Hidden && _pushRow != null;

        /// <summary>Botón de la fila (toda la placa).</summary>
        public Button? PushButton => _pushRow?.Button;

        /// <summary>Enlace «Abrir ajustes del teléfono» (solo con los avisos bloqueados).</summary>
        public Button? PushOpenPhoneButton => _pushOpenPhone;

        /// <summary>Se ve el enlace «Abrir ajustes del teléfono».</summary>
        public bool PushOpenPhoneShown => _push == PushRowState.Blocked && _pushOpenPhone != null;

        /// <summary>Texto de la fila («Avisos»).</summary>
        public string PushLabel => _pushRow?.Label.text ?? string.Empty;

        /// <summary>Estado escrito bajo «Avisos».</summary>
        public string PushStateText => _pushState?.text ?? string.Empty;

        /// <summary>Si la fila lleva la marca (avisos encendidos).</summary>
        public bool PushMarked => _pushRow != null && _pushRow.Selected;

        /// <summary>Etiqueta de TalkBack de la fila ahora.</summary>
        public string PushA11yLabel => PushText.A11yLabel(_push);

        /// <summary>
        /// Desplaza Ajustes hasta que la fila «Avisos» quede arriba del visor (capturas, como <see cref="ScrollToPrivacy"/>). Sin la fila
        /// o sin desplazamiento, nada.
        /// </summary>
        public void ScrollToPush()
        {
            float range = _content.rect.height - _viewport.rect.height;
            if (!PushShown || PushSlot == null || !_scroll.vertical || range <= 0f)
            {
                return;
            }

            float top = Mathf.Clamp(-PushSlot.anchoredPosition.y - 8f, 0f, range);
            _scroll.verticalNormalizedPosition = 1f - (top / range);
            UpdateFades();
        }

        /// <summary>Enseña la fila en <paramref name="state"/> (al abrir Ajustes y tras cada cambio); con la pantalla abierta, la recoloca.</summary>
        public void ShowPush(PushRowState state)
        {
            _push = state;
            if (PushSlot == null || _pushRow == null || _pushState == null || _pushOpenPhone == null)
            {
                return;
            }

            bool shown = state != PushRowState.Hidden;
            PushSlot.gameObject.SetActive(shown);
            _pushRow.SetSelected(state == PushRowState.On);
            _pushRow.Label.text = PushText.Row;
            _pushState.text = PushText.State(state);
            _pushState.color = _pushRow.Label.color; // crema sobre la hoja si está marcada; madera oscura si no (07 §1.2)
            _pushOpenPhone.gameObject.SetActive(state == PushRowState.Blocked);
            if (Visible)
            {
                Layout();
                EmitLayout();
            }
        }

        // Crea el hueco, la fila, su estado y el enlace (una vez; en el constructor). Ocultos hasta ShowPush.
        private void BuildPush()
        {
            if (PushSlot != null)
            {
                return;
            }

            PushSlot = UiBuilder.Rect("PushSlot", _content);
            _pushRow = Option(PushSlot, _art, "Push", PushId, PushText.Row, () => PushPressed?.Invoke());
            _pushState = UiBuilder.Label("State", _pushRow.Root, TextStyle.PlainBold, string.Empty, PushStateSp, Palette.FamilyWood.Deep, TextAlignmentOptions.TopLeft);
            _pushState.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_pushState, MinTextSp, PushStateSp);
            _pushState.raycastTarget = false;
            A11yLabels.Set(_pushRow.Button, () => PushText.A11yLabel(_push), () => _push == PushRowState.On); // TalkBack: la fila y su estado (B9)
            _pushOpenPhone = ModalKit.Link("PushOpenPhone", PushSlot, PushText.OpenPhone, () => PushOpenPhonePressed?.Invoke(), out TextMeshProUGUI link);
            _pushOpenPhoneLabel = link;
            link.textWrappingMode = TextWrappingModes.Normal;
            link.fontSizeMin = MinTextSp;
            PushSlot.gameObject.SetActive(false);
            _pushOpenPhone.gameObject.SetActive(false);
        }

        /// <summary>
        /// Coloca la fila desde <paramref name="y"/> (dp desde arriba del contenido, ya con el hueco de sección): la placa crece con
        /// «Avisos» y su estado (las dos líneas a la escala <paramref name="s"/>, ≥ 48 dp) y, si se ve, el enlace debajo. La apunta para
        /// el corte del visor y devuelve el borde de abajo. Sin la fila, <paramref name="fallback"/>.
        /// </summary>
        private float LayoutPush(float y, float fallback, float inner, float rowH, float s)
        {
            if (!PushShown || PushSlot == null || _pushRow == null || _pushState == null || _pushOpenPhone == null || _pushOpenPhoneLabel == null)
            {
                return fallback;
            }

            float textX = 14f + OptionRow.RadioDp + 12f;
            float textW = inner - textX - 12f;
            // Cada línea con el alto de línea de su fuente (≈ 1,45 × el cuerpo, con los acentos de arriba) a la escala s.
            float labelH = Mathf.Ceil(OptionSp * s * 1.45f) + 2f;
            _pushState.fontSizeMax = PushStateSp * s;
            float stateH = Mathf.Max(Mathf.Ceil(PushStateSp * s * 1.45f) + 2f, Mathf.Ceil(UiBuilder.MinTextHeight(_pushState, textW)) + 2f);
            float plateH = Mathf.Max(rowH, labelH + stateH + 12f);
            _pushRow.Place(0f, 0f, inner, plateH, s);
            UiBuilder.Place(_pushRow.Label.rectTransform, textX, 6f, textW, labelH);
            UiBuilder.Place(_pushState.rectTransform, textX, 6f + labelH, textW, stateH);
            _rowTops.Add(y);
            _rowHeights.Add(plateH);
            float slotH = plateH;
            if (PushOpenPhoneShown)
            {
                _pushOpenPhoneLabel.fontSizeMax = 16f * s;
                float linkH = Mathf.Max(MinRowDp, Mathf.Ceil(UiBuilder.MinTextHeight(_pushOpenPhoneLabel, inner)) + 12f);
                UiBuilder.Place((RectTransform)_pushOpenPhone.transform, 0f, plateH + RowGapDp, inner, linkH);
                _rowTops.Add(y + plateH + RowGapDp);
                _rowHeights.Add(linkH);
                slotH += RowGapDp + linkH;
            }

            UiBuilder.Place(PushSlot, 16f, y, inner, slotH);
            return y + slotH;
        }

        internal IEnumerable<TextMeshProUGUI> PushTexts()
        {
            if (!PushShown)
            {
                yield break;
            }

            yield return _pushRow!.Label;
            yield return _pushState!;
            if (PushOpenPhoneShown)
            {
                yield return _pushOpenPhoneLabel!;
            }
        }

        // Entradas que añade la fila a AF_LAYOUT settings.
        private int PushLayoutCount => (PushShown ? 1 : 0) + (PushOpenPhoneShown ? 1 : 0);

        private int PushLayoutIds(string[] ids, RectTransform[] rects, int k)
        {
            int n = 0;
            if (PushShown && k < ids.Length)
            {
                ids[k] = PushId;
                rects[k] = _pushRow!.Root;
                n++;
            }

            if (PushOpenPhoneShown && k + n < ids.Length)
            {
                ids[k + n] = PushOpenPhoneId;
                rects[k + n] = (RectTransform)_pushOpenPhone!.transform;
                n++;
            }

            return n;
        }
    }
}
