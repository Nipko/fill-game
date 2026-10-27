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
    /// La fila «Avatar» de Ajustes (D-056; <c>docs/dev/avatares.md</c> §3.2), arriba del todo: el retrato de 32 dp, «Avatar» y una
    /// flecha; abre «Tu avatar» (<see cref="AvatarRowPressed"/>, lo fija <c>GameBootstrap</c>). Oculta antes del 10 y con
    /// <c>ui_avatars_enabled</c> apagada. TalkBack: «Tu avatar: {animal}. Toca para cambiarlo». Parcial de <see cref="SettingsScreen"/>.
    /// </summary>
    internal sealed partial class SettingsScreen
    {
        /// <summary>Id de la fila en la sonda (<c>AF_LAYOUT settings</c>).</summary>
        public const string AvatarRowId = "avatar_row";

        /// <summary>Retrato de la fila (dp).</summary>
        public const float AvatarRowPortraitDp = 32f;

        private const int AvatarRowLayoutCount = 1;

        private Button _avatarRow = null!;
        private Image _avatarRowPortrait = null!;
        private TextMeshProUGUI _avatarRowLabel = null!;
        private Image _avatarRowChevron = null!;
        private string _avatarRowA11y = string.Empty;

        /// <summary>Al tocar la fila. Lo fija <c>GameBootstrap</c>.</summary>
        public Action? AvatarRowPressed { get; set; }

        /// <summary>La fila se ve.</summary>
        public bool AvatarRowShown => _avatarRow.gameObject.activeSelf;

        /// <summary>El botón de la fila.</summary>
        public Button AvatarRowButton => _avatarRow;

        /// <summary>El texto de la fila («Avatar»).</summary>
        public TextMeshProUGUI AvatarRowLabel => _avatarRowLabel;

        /// <summary>Lo que lee TalkBack.</summary>
        public string AvatarRowA11y => _avatarRowA11y;

        /// <summary>Enseña la fila con <paramref name="portrait"/> o la oculta; con Ajustes abierta, la rehace.</summary>
        public void SetAvatarRow(Sprite? portrait, bool shown, string a11y)
        {
            bool relayout = shown != AvatarRowShown;
            _avatarRowPortrait.sprite = portrait;
            _avatarRowPortrait.enabled = portrait != null;
            _avatarRowA11y = a11y ?? string.Empty;
            _avatarRow.gameObject.SetActive(shown);
            if (relayout && Visible)
            {
                Layout();
                EmitLayout();
            }
        }

        private void BuildAvatarRow()
        {
            _avatarRow = UiBuilder.BareButton("AvatarRow", _content, () => AvatarRowPressed?.Invoke());
            var root = (RectTransform)_avatarRow.transform;
            Image border = UiBuilder.Sliced("Border", root, _art.RoundedRect, OptionRow.Border);
            UiBuilder.Stretch(border.rectTransform);
            border.pixelsPerUnitMultiplier = 24f / OptionRow.RadiusDp;
            Image plate = UiBuilder.Sliced("Plate", root, _art.RoundedRect, OptionRow.Plate);
            UiBuilder.Stretch(plate.rectTransform);
            plate.rectTransform.offsetMin = new Vector2(OptionRow.BorderDp, OptionRow.BorderDp);
            plate.rectTransform.offsetMax = new Vector2(-OptionRow.BorderDp, -OptionRow.BorderDp);
            plate.pixelsPerUnitMultiplier = 24f / (OptionRow.RadiusDp - OptionRow.BorderDp);
            _avatarRowPortrait = UiBuilder.Image("Portrait", root, null, Color.white);
            _avatarRowPortrait.preserveAspect = true;
            _avatarRowLabel = UiBuilder.Label("Label", root, TextStyle.PlainBold, AvatarText.SettingsRow, OptionSp, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
            UiBuilder.AutoSize(_avatarRowLabel, MinTextSp + 1f, OptionSp);
            _avatarRowChevron = UiBuilder.Image("Chevron", root, _art.AvatarChevron, Palette.FamilyWood.Base);
            A11yLabels.Set(_avatarRow, () => _avatarRowA11y);
            A11yLabels.Set(_avatarRowLabel, () => string.Empty); // lo dice la etiqueta del botón
            _avatarRow.gameObject.SetActive(false);
        }

        // La fila en el contenido desde y; devuelve la y de lo que sigue (con el hueco de sección), o y si no se ve.
        private float LayoutAvatarRow(float y, float inner, float rowH, float s)
        {
            if (!AvatarRowShown)
            {
                return y;
            }

            UiBuilder.Place((RectTransform)_avatarRow.transform, 16f, y, inner, rowH);
            UiBuilder.Place(_avatarRowPortrait.rectTransform, 12f, (rowH - AvatarRowPortraitDp) * 0.5f, AvatarRowPortraitDp, AvatarRowPortraitDp);
            const float chevron = 24f;
            UiBuilder.Place(_avatarRowChevron.rectTransform, inner - 10f - chevron, (rowH - chevron) * 0.5f, chevron, chevron);
            float labelX = 12f + AvatarRowPortraitDp + 12f;
            _avatarRowLabel.fontSizeMax = OptionSp * s;
            _avatarRowLabel.fontSizeMin = MinTextSp + 1f;
            UiBuilder.Place(_avatarRowLabel.rectTransform, labelX, 0f, inner - labelX - chevron - 16f, rowH);
            _rowTops.Add(y);
            _rowHeights.Add(rowH);
            return y + rowH + SectionGapDp;
        }

        private IEnumerable<TextMeshProUGUI> AvatarRowTexts()
        {
            if (AvatarRowShown)
            {
                yield return _avatarRowLabel;
            }
        }

        private void AvatarRowLayoutIds(string[] ids, RectTransform[] rects, int k)
        {
            ids[k] = AvatarRowId;
            rects[k] = AvatarRowShown ? (RectTransform)_avatarRow.transform : null!;
        }
    }
}
