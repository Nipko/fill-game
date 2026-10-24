#nullable enable
using System;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El botón «Liga» del inicio (D-046; liga.md §1: «un botón "Liga" en el inicio, desde el 41, con su icono»), en un lienzo propio
    /// encima del inicio (como <see cref="MarketLobbyIcons"/>, sin tocar <see cref="LobbyScreen"/>). Va abajo a la izquierda, sobre la
    /// barra de pestañas y a la altura de «Inicio» menos su hueco [P]: el engranaje y la fila de arriba quedan para Ajustes y los
    /// botones de audio (D-046, punto 2), y la columna de la derecha para el Cántaro y la Crecida. Madera con el icono (un podio de
    /// tres escalones, por código) y el texto «Liga». Sin asignaciones mientras nada cambia.
    /// </summary>
    internal sealed class LeagueLobbyButton
    {
        /// <summary>Orden del lienzo: el de las entradas del Mercado (6).</summary>
        public const int SortingOrder = 6;

        /// <summary>Ancho del botón (dp) al 100 % [P].</summary>
        public const float WidthDp = 104f;

        /// <summary>Alto del botón (dp): el área táctil mínima (07 §7.8).</summary>
        public const float HeightDp = 48f;

        /// <summary>Margen izquierdo (dp), el del inicio.</summary>
        public const float LeftDp = 16f;

        /// <summary>Hueco entre el botón y el borde de arriba de «Inicio» (dp) [P].</summary>
        public const float GapAboveHomeDp = 10f;

        private const float LabelSp = 16f;

        private readonly OverlayCanvas _canvas;
        private readonly ReliefParts _button;
        private readonly TextMeshProUGUI _label;
        private readonly RectTransform _icon;
        private readonly Vector3[] _corners = new Vector3[4];
        private Vector3 _homeSeen;

        public LeagueLobbyButton(Transform host, ProceduralArt art, Action onPress)
        {
            _canvas = new OverlayCanvas("LeagueButtonCanvas", host, SortingOrder);
            _button = UiBuilder.Relief("LeagueTab", _canvas.Root, art.CtaWood, onPress);
            _icon = UiBuilder.Rect("Icon", _button.Face);
            Step(0f, 0.45f, 0.33f, 0.55f, Palette.FamilyWood.Light);
            Step(0.33f, 0f, 0.34f, 1f, Palette.Cream);
            Step(0.67f, 0.3f, 0.33f, 0.7f, Palette.FamilyWood.Light);
            _label = UiBuilder.Label("Label", _button.Face, TextStyle.PillWood, LeagueText.Button, LabelSp, Palette.Cream, TextAlignmentOptions.MidlineLeft);
            _button.Relief.Bind(_button.Face, _button.Base, _button.FaceImage, art.CtaWood, null, null, null);
            _button.Button.gameObject.SetActive(false);
        }

        public Canvas Canvas => _canvas.Canvas;

        public Button Button => _button.Button;

        public RectTransform Rect => (RectTransform)_button.Button.transform;

        public bool Shown => _button.Button.gameObject.activeSelf;

        public string Label => _label.text;

        /// <summary>Enseña u oculta el botón y lo coloca sobre <paramref name="homeTab"/> (el «Inicio» del inicio, en pantalla).</summary>
        public void Sync(bool shown, RectTransform? homeTab)
        {
            _canvas.Measure();
            bool show = shown && homeTab != null;
            if (_button.Button.gameObject.activeSelf != show)
            {
                _button.Button.gameObject.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            homeTab!.GetWorldCorners(_corners);
            Rect root = _canvas.Root.rect;
            Vector3 topLeft = _canvas.Root.InverseTransformPoint(_corners[1]);
            float homeTop = root.yMax - topLeft.y;
            float s = Mathf.Max(1f, UiBuilder.TextScale);
            _label.fontSize = LabelSp * s;
            float textW = Mathf.Ceil(UiBuilder.MinTextWidth(_label));
            float iconDp = 24f;
            float w = Mathf.Max(WidthDp, 14f + iconDp + 8f + textW + 14f);
            float h = Mathf.Max(HeightDp, Mathf.Round((LabelSp * s) + 24f));
            UiBuilder.Place(Rect, LeftDp, homeTop - GapAboveHomeDp - h, w, h);
            float face = ModalKit.FaceHeightOf(_button);
            UiBuilder.Place(_icon, 14f, (face - iconDp) * 0.5f, iconDp, iconDp);
            UiBuilder.Place(_label.rectTransform, 14f + iconDp + 8f, 0f, textW + 4f, face);
        }

        /// <summary>«Inicio» se movió en pantalla desde la última vez (la escala del inicio se aplica en su primer fotograma).</summary>
        public bool TargetMoved(RectTransform? homeTab)
        {
            if (homeTab == null)
            {
                return false;
            }

            homeTab.GetWorldCorners(_corners);
            bool moved = _corners[1] != _homeSeen;
            _homeSeen = _corners[1];
            return moved;
        }

        /// <summary>Vuelve a medir la pantalla; true si cambió.</summary>
        public bool Remeasure() => _canvas.Measure();

        public void Hide()
        {
            if (_button.Button.gameObject.activeSelf)
            {
                _button.Button.gameObject.SetActive(false);
            }
        }

        // Un escalón del podio en fracciones del icono (x, y desde arriba, ancho, alto).
        private void Step(float x, float y, float w, float h, Color color)
        {
            Image img = UiBuilder.Image("Step", _icon, null, color);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(x, 1f - y - h);
            rt.anchorMax = new Vector2(x + w, 1f - y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(0.5f, 0f);
            rt.offsetMax = new Vector2(-0.5f, 0f);
        }
    }
}
