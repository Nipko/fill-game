#nullable enable
using System.Collections.Generic;
using System.Globalization;
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
    /// (<c>eco_reward_*</c>; no se cobra), «Reintentar» [P] y X → inicio. Sin intersticial antes ni después (05 §10).
    /// </summary>
    internal sealed class LevelFailedScreen
    {
        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly ReliefParts _close;
        private readonly Image _strip;
        private readonly TextMeshProUGUI _stripLabel;
        private readonly Image _basket;
        private readonly TextMeshProUGUI _reward;
        private readonly ReliefParts _retry;

        public LevelFailedScreen(Transform parent, ProceduralArt art, IGameActions actions)
        {
            _art = art;
            _root = UiBuilder.Rect("LevelFailed", parent);
            ModalKit.Dim("Dim", _root, 0.55f);
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            _title = ModalKit.Header(_panel, art, UiText.Level(1), out _header);
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

        public Button RetryButton => _retry.Button;

        public Button CloseButton => _close.Button;

        public string TitleText => _title.text;

        public string RewardText => _reward.text;

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
