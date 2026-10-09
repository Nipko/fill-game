#nullable enable
using System.Collections.Generic;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Tarjeta «¡Miquito a la vista!» (guion de la intro §4.5; recomendación Q10 A, pendiente de D-009): de la familia de los avisos del tablero (placa de
    /// madera clara, como «El río bajó»), con el retrato de la especie de 40 dp, el título, el nombre local y una línea de ≤ 50
    /// caracteres con un dato verificado. Sale después de la intro, nunca dentro de la carga; dura 2,5 s con fundido de
    /// entrada y salida; no captura toques (<c>raycastTarget</c> = false y <see cref="CanvasGroup.blocksRaycasts"/> = false) y
    /// no tapa cuias ni botones: va sobre la franja del panorama si mide ≥ 64 dp o, si no, sobre el muelle por encima de la
    /// bandeja. «Avistamiento», nunca «nuevo»; sin confeti. Nombres y textos, pendientes del asesor cultural (07 §8.2).
    /// </summary>
    internal sealed class SightingCard
    {
        /// <summary>Alto de la tarjeta (dp).</summary>
        public const float HeightDp = 60f;

        /// <summary>Lado del retrato (dp).</summary>
        public const float PortraitDp = 40f;

        private const float FadeMs = 200f;

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _plate;
        private readonly Image _edge;
        private readonly Image _halo;
        private readonly Image _portrait;
        private readonly TextMeshProUGUI _title;
        private readonly TextMeshProUGUI _name;
        private readonly TextMeshProUGUI _fact;
        private float _leftMs = -1f;
        private float _totalMs;

        public SightingCard(Transform parent, ProceduralArt art)
        {
            _root = UiBuilder.Rect("SightingCard", parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _edge = UiBuilder.Sliced("Edge", _root, art.RoundedRect, Palette.FamilyWood.Base);
            _edge.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(_edge.rectTransform);
            _edge.rectTransform.offsetMin = new Vector2(0f, -3f);
            _edge.rectTransform.offsetMax = new Vector2(0f, -3f);
            _plate = UiBuilder.Sliced("Plate", _root, art.RoundedRect, Palette.PanelLight);
            _plate.pixelsPerUnitMultiplier = 24f / 14f;
            UiBuilder.Stretch(_plate.rectTransform);
            _halo = UiBuilder.Image("PortraitBack", _root, art.Disc, Palette.PanelLightLow);
            UiBuilder.Place(_halo.rectTransform, 9f, 8f, PortraitDp + 4f, PortraitDp + 4f);
            _portrait = UiBuilder.Image("Portrait", _root, null, Color.white);
            _portrait.preserveAspect = true;
            UiBuilder.Place(_portrait.rectTransform, 11f, 10f, PortraitDp, PortraitDp);
            _title = UiBuilder.Label("Title", _root, TextStyle.PlainBold, UiText.FaunaSightedCard, 15f, Palette.FamilyWood.Base, TextAlignmentOptions.MidlineLeft);
            _name = UiBuilder.Label("Name", _root, TextStyle.PlainBold, string.Empty, 14f, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
            _fact = UiBuilder.Label("Fact", _root, TextStyle.Body, string.Empty, 12f, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);
            UiBuilder.AutoSize(_title, 12f, 15f);
            UiBuilder.AutoSize(_name, 11f, 14f);
            UiBuilder.AutoSize(_fact, 10f, 12f);
            _root.gameObject.SetActive(false);
        }

        /// <summary>La tarjeta está a la vista.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Nombre que muestra.</summary>
        public string Name => _name.text;

        /// <summary>Línea del dato que muestra.</summary>
        public string Fact => _fact.text;

        /// <summary>Raíz (tests de exclusión).</summary>
        public RectTransform Root => _root;

        /// <summary>Opacidad actual del fundido.</summary>
        public float Alpha => _group.alpha;

        /// <summary>Muestra la tarjeta de la especie durante <paramref name="ms"/> milisegundos.</summary>
        public void Show(Sprite portrait, string name, string fact, float ms)
        {
            _portrait.sprite = portrait;
            _name.text = name;
            _fact.text = fact;
            _leftMs = ms;
            _totalMs = Mathf.Max(1f, ms);
            _group.alpha = 0f;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
        }

        /// <summary>Coloca la tarjeta centrada con su borde superior en <paramref name="top"/> (dp desde arriba).</summary>
        public void Place(float canvasW, float top)
        {
            float w = Mathf.Min(320f, canvasW - 32f);
            UiBuilder.Place(_root, (canvasW - w) * 0.5f, top, w, HeightDp);
            float textX = 11f + PortraitDp + 10f;
            float textW = w - textX - 12f;
            UiBuilder.Place(_title.rectTransform, textX, 5f, textW, 18f);
            UiBuilder.Place(_name.rectTransform, textX, 22f, textW, 17f);
            UiBuilder.Place(_fact.rectTransform, textX, 38f, textW, 16f);
        }

        public void Hide()
        {
            _leftMs = -1f;
            _root.gameObject.SetActive(false);
        }

        public void Tick(float dtMs)
        {
            if (_leftMs < 0f)
            {
                return;
            }

            _leftMs -= dtMs;
            float shown = _totalMs - _leftMs;
            _group.alpha = Mathf.Clamp01(Mathf.Min(shown, _leftMs) / FadeMs);
            if (_leftMs <= 0f)
            {
                Hide();
            }
        }

        /// <summary>Añade los gráficos de la tarjeta (tests de raycast).</summary>
        public void CollectGraphics(List<Graphic> graphics)
        {
            graphics.Add(_plate);
            graphics.Add(_edge);
            graphics.Add(_halo);
            graphics.Add(_portrait);
            graphics.Add(_title);
            graphics.Add(_name);
            graphics.Add(_fact);
        }
    }
}
