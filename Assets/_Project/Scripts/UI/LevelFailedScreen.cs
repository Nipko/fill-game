#nullable enable
using System.Collections.Generic;
using System.Globalization;
using AF.Levels;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Nivel fallido [P] (paso 3 del embudo de D-008; GDD 04 §6.6 wireframe «Paso 3»; 02 §2 LEVEL_FAILED; tarea B2-3), con la
    /// composición de «Level Failed!» de Magic Sort (ficha §4.4 y §4.8: cabecera «Level N» con X, franja «Level Failed!», el
    /// premio que no se cobra y un botón grande de reintentar) y el material de D-003: cesta de semillas vacía en lugar del
    /// pedestal de monedas, sin corazón roto ni calaveras. «Nivel {n}», «Nivel fallido», «Premio: {20 · 60 · 80} semillas»
    /// (<c>eco_reward_*</c>; no se cobra), «Reintentar» [P] y X → inicio. Sin intersticial antes ni después (05 §10). La cabecera
    /// lleva la etiqueta del nivel (propuesta-mecanicas-dificultad.md §c.1; D-009; como la tarjeta roja de Magic Sort en sus
    /// SUPER HARD, ficha §3.6, sin calavera): ámbar con la rana en Difícil, índigo con el jaguar en Muy difícil y la madera de
    /// siempre en normal (<see cref="SetLabel"/>, aplicada en el siguiente <see cref="Show"/>).
    /// </summary>
    internal sealed class LevelFailedScreen
    {
        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly Image _badge;
        private readonly ReliefParts _close;
        private readonly Image _strip;
        private readonly TextMeshProUGUI _stripLabel;
        private readonly Image _basket;
        private readonly TextMeshProUGUI _reward;
        private readonly ReliefParts _retry;
        private string _label = LevelDefinition.LabelNormal;
        private Color? _band;

        public LevelFailedScreen(Transform parent, ProceduralArt art, IGameActions actions)
        {
            _art = art;
            _root = UiBuilder.Rect("LevelFailed", parent);
            ModalKit.Dim("Dim", _root, 0.55f);
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            _title = ModalKit.Header(_panel, art, UiText.Level(1), out _header);
            _badge = ModalKit.HeaderBadge(_header);
            _close = ModalKit.Close("CloseButton", _panel, art, () => actions.PressLevelFailedClose());
            _strip = UiBuilder.Sliced("Strip", _panel, art.WoodPlate, Color.white);
            _stripLabel = UiBuilder.Label("Label", _strip.transform, TextStyle.PillWood, UiText.LevelFailed, 20f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Stretch(_stripLabel.rectTransform);
            _basket = UiBuilder.Image("Basket", _panel, art.EmptyBasket, Color.white);
            _basket.preserveAspect = true;
            _reward = ModalKit.Line("Reward", _panel, string.Empty, 18f, Palette.FamilyWood.Base);
            _retry = ModalKit.Cta("RetryButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, UiText.Retry, 24f, () => actions.PressRetry(), out _);
            _root.gameObject.SetActive(false);
        }

        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Raíz del modal, hija del lienzo de la pantalla de nivel (tests de orden de capas).</summary>
        public RectTransform Root => _root;

        public Button RetryButton => _retry.Button;

        public Button CloseButton => _close.Button;

        public string TitleText => _title.text;

        public string RewardText => _reward.text;

        /// <summary><c>difficulty.label</c> que viste la cabecera en el siguiente <see cref="Show"/> (o en el actual).</summary>
        public string Label => _label;

        /// <summary>Banda de la cabecera aplicada en el último <see cref="Show"/> (<c>ui_ambar</c> o <c>ui_indigo</c>; null = madera).</summary>
        public Color? HeaderBand => _band;

        /// <summary>Color del título de la cabecera («Nivel {n}»).</summary>
        public Color TitleColor => _title.color;

        /// <summary>
        /// Material del título de la cabecera (tests): el de <c>TextStyle.ModalTitle</c>, con contorno y sombra, sobre la madera
        /// y el índigo; el de la fuente, sin contorno, con la tinta sobre el ámbar (<c>ModalKit.SetTitleColor</c>).
        /// </summary>
        internal Material? TitleMaterial => _title.fontSharedMaterial;

        /// <summary>Sello de la etiqueta en la cabecera, o null si no se ve.</summary>
        public Sprite? HeaderBadge => _badge.gameObject.activeSelf ? _badge.sprite : null;

        /// <summary>
        /// Guarda la etiqueta del nivel (<c>difficulty.label</c>: <c>normal</c>, <c>hard</c> o <c>very_hard</c>) para la cabecera
        /// del siguiente <see cref="Show"/>. La llama GameScreen (GameScreen.Funnel.cs) justo antes de abrir el modal.
        /// </summary>
        public void SetLabel(string? difficultyLabel)
        {
            _label = string.IsNullOrEmpty(difficultyLabel) ? LevelDefinition.LabelNormal : difficultyLabel!;
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        /// <summary>Muestra el nivel <paramref name="level"/> con el premio <paramref name="reward"/> que no se cobra.</summary>
        public void Show(int level, int reward, float canvasW, float canvasH)
        {
            _root.gameObject.SetActive(true);
            _title.text = UiText.Level(level);
            _reward.text = string.Format(CultureInfo.InvariantCulture, UiText.LevelFailedReward, UiText.Grouped(reward));
            float width = Mathf.Min(328f, canvasW - 32f);
            float inner = width - 32f;
            float y = 16f;
            _band = ModalKit.ApplyHeaderLabel(_header, _title, _badge, _art, _label);
            UiBuilder.Place(_header.rectTransform, 16f, y, inner - 52f, 40f);
            UiBuilder.Place((RectTransform)_close.Button.transform, width - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
            y += 52f;
            UiBuilder.Place(_strip.rectTransform, 24f, y, width - 48f, 40f);
            y += 50f;
            UiBuilder.Place(_basket.rectTransform, (width - 120f) * 0.5f, y, 120f, 90f);
            y += 94f;
            UiBuilder.Place(_reward.rectTransform, 16f, y, inner, 24f);
            y += 34f;
            ModalKit.Place(_retry, 16f, y, inner);
            y += ModalKit.HeightOf(_retry) + 14f;
            ModalKit.Size(_panel, _surface, _art, (canvasW - width) * 0.5f, Mathf.Round((canvasH - y) * 0.45f), width, y);
        }

        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            ids.Add("retry");
            rects.Add((RectTransform)_retry.Button.transform);
            ids.Add("close");
            rects.Add((RectTransform)_close.Button.transform);
        }
    }
}
