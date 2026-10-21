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
    /// Tarjeta de la Pororoca en el nivel 30 (bloque B2; 04 §4: «30 · Pororoca · Tarjeta con animación»; 04 §2.4: «Tutorial:
    /// tarjeta de una pantalla con la animación en bucle; no se regala uso»): cabecera «Pororoca», una franja de río con tres cuias
    /// pequeñas que la ola cruza en bucle (<see cref="LoopMs"/>, el tiempo de la ola de verdad) y el lema del glosario. Se cierra
    /// con un toque en cualquier punto, como la tarjeta previa de 02 §10; mientras se ve, el tablero no recibe toques. Con
    /// Reducir movimiento, la ola queda quieta en el centro. Sin asignaciones por fotograma. La abre <c>GameScreen.Shuffle.cs</c>
    /// al habilitarse el toque del primer nivel 30 (<c>LevelFlow.ShuffleCardDue</c>).
    /// </summary>
    internal sealed class ShuffleIntroCard
    {
        /// <summary>Un ciclo de la ola de la tarjeta (ms): el de la ola del tablero (<see cref="ShuffleWaveFx.DurationMs"/>) y una pausa.</summary>
        public const float LoopMs = ShuffleWaveFx.DurationMs + 600f;

        /// <summary>Alto de la franja animada (dp).</summary>
        public const float StripDp = 96f;

        private const int Cuias = 3;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly RectTransform _strip;
        private readonly Image _water;
        private readonly Image[] _cuias = new Image[Cuias];
        private readonly Image _wave;
        private readonly TextMeshProUGUI _body;
        private float _ms;
        private float _stripW;
        private bool _reduce;

        public ShuffleIntroCard(Transform parent, ProceduralArt art, Action onDismiss)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            if (onDismiss == null)
            {
                throw new ArgumentNullException(nameof(onDismiss));
            }

            _root = UiBuilder.Rect("ShuffleCard", parent);
            Image dim = ModalKit.Dim("Dim", _root, 0.55f);
            Button tap = dim.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => onDismiss());
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            ModalKit.Header(_panel, art, ShuffleText.Name, out _header);
            _strip = UiBuilder.Rect("Strip", _panel);
            _strip.gameObject.AddComponent<RectMask2D>(); // la ola entra y sale por los bordes de la franja
            _water = UiBuilder.Sliced("Water", _strip, art.RoundedRect, Palette.FamilyWater.Base);
            UiBuilder.Stretch(_water.rectTransform);
            for (int i = 0; i < Cuias; i++)
            {
                _cuias[i] = UiBuilder.Image("Cuia" + i, _strip, art.IconCuiaPlusDisabled, Color.white);
                _cuias[i].preserveAspect = true;
            }

            _wave = UiBuilder.Image("Wave", _strip, art.ShuffleWaveSprite, Color.white);
            _body = ModalKit.Line("Body", _panel, ShuffleText.Tagline, 17f, Palette.FamilyWood.Base);

            // La placa no captura toques: un toque en la tarjeta llega al velo de detrás, que la cierra.
            _root.gameObject.SetActive(false);
        }

        /// <summary>La tarjeta está abierta.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Raíz (tests y orden de capas).</summary>
        public RectTransform Root => _root;

        /// <summary>Texto del cuerpo.</summary>
        public string BodyText => _body.text;

        /// <summary>Posición x de la ola en la franja (dp), para los tests de la animación.</summary>
        public float WaveX => _wave.rectTransform.anchoredPosition.x;

        /// <summary>Abre la tarjeta centrada en un lienzo de <paramref name="canvasW"/> × <paramref name="canvasH"/> dp.</summary>
        public void Show(float canvasW, float canvasH, bool reduceMotion)
        {
            _reduce = reduceMotion;
            _ms = 0f;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            UiBuilder.Stretch(_root);
            _body.text = ShuffleText.Tagline;
            float width = Mathf.Min(328f, canvasW - 32f);
            float inner = width - 32f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner, 40f);
            y += 52f;
            _stripW = inner;
            UiBuilder.Place(_strip, 16f, y, inner, StripDp);
            float cuiaW = 40f;
            float gap = (inner - (Cuias * cuiaW)) / (Cuias + 1);
            for (int i = 0; i < Cuias; i++)
            {
                UiBuilder.Place(_cuias[i].rectTransform, gap + (i * (cuiaW + gap)), 20f, cuiaW, 56f);
            }

            y += StripDp + 12f;
            y += ModalKit.PlaceLine(_body, 16f, y, inner, 48f) + 16f;
            ModalKit.Size(_panel, _surface, _art, (canvasW - width) * 0.5f, Mathf.Round((canvasH - y) * 0.45f), width, y);
            PlaceWave();
        }

        /// <summary>Avanza la animación en bucle <paramref name="dtMs"/> ms.</summary>
        public void Tick(float dtMs)
        {
            if (!Visible)
            {
                return;
            }

            _ms = (_ms + Mathf.Max(0f, dtMs)) % LoopMs;
            PlaceWave();
        }

        /// <summary>Cierra la tarjeta.</summary>
        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        /// <summary>Lo que se puede tocar para <c>AF_LAYOUT</c>: toda la tarjeta (cualquier toque la cierra).</summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            if (!Visible)
            {
                return;
            }

            ids.Add("shuffle_card");
            rects.Add(_panel);
        }

        private void PlaceWave()
        {
            float h = StripDp;
            float w = h * 0.6f;
            float front;
            if (_reduce)
            {
                front = (_stripW + w) * 0.5f;
            }
            else
            {
                float t = Mathf.Clamp01(_ms / ShuffleWaveFx.DurationMs);
                front = Mathf.Lerp(0f, _stripW + w, t * t * (3f - (2f * t)));
            }

            UiBuilder.Place(_wave.rectTransform, front - w, 0f, w, h);
            _wave.enabled = _reduce || _ms < ShuffleWaveFx.DurationMs;
        }
    }
}
