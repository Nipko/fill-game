#nullable enable
using System;
using System.Collections.Generic;
using AF.Boosters;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Respuesta honesta de Morpho sin vertido (tarea MO2; 04 §2.3: «responde con honestidad en el orden de 02 T5, con botones
    /// Deshacer / Volver a la orilla, y en ningún caso se cobra»; D-019): una tarjeta pequeña con la mariposa, la frase de la
    /// respuesta (<see cref="UiText.HintAnswerText"/>: «Desde aquí no hay salida…», «No veo un vertido seguro…» o «No encuentro
    /// un camino desde aquí») y dos botones: Deshacer (familia Agua, como el atajo de la confirmación del reinicio) y Volver a la
    /// orilla (familia Madera; con ≥ 8 vertidos pasa por su confirmación, 04 §2.7). La X, o tocar fuera, sigue jugando.
    /// </summary>
    /// <remarks>
    /// Con el material de los modales de D-008 (<see cref="ModalKit"/>) y un velo ligero que recoge los toques [P]: la respuesta
    /// es una elección y no se pisa con un toque al tablero. Textos a 17 sp que pasan a otra línea y nunca bajan de 12 sp
    /// (<see cref="ModalKit.Line"/>; 02 §12); la tarjeta crece con su texto. La cierra la pantalla si el nivel deja de estar en
    /// juego o el tablero cambia (<c>GameScreen.Hint.cs</c>).
    /// </remarks>
    internal sealed class HintAnswerCard
    {
        /// <summary>Lado de la mariposa de la cabecera (dp) [P].</summary>
        public const float IconDp = 40f;

        /// <summary>Alfa del velo [P]: más ligero que el de los modales del embudo; el tablero se sigue leyendo.</summary>
        public const float DimAlpha = 0.35f;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _icon;
        private readonly ReliefParts _close;
        private readonly TextMeshProUGUI _body;
        private readonly ReliefParts _undo;
        private readonly ReliefParts _restart;

        public HintAnswerCard(Transform parent, ProceduralArt art, Action onUndo, Action onRestart, Action onClose)
        {
            _art = art;
            _root = UiBuilder.Rect("HintAnswer", parent);
            Image dim = ModalKit.Dim("Dim", _root, DimAlpha);
            Button dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.navigation = new Navigation { mode = Navigation.Mode.None };
            dimButton.onClick.AddListener(() => onClose());
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            _icon = UiBuilder.Image("Morpho", _panel, art.IconMorpho, Color.white);
            _icon.preserveAspect = true;
            _close = ModalKit.Close("CloseButton", _panel, art, onClose);
            _body = ModalKit.Line("Body", _panel, string.Empty, 17f, Palette.FamilyWood.Base);
            _undo = ModalKit.Cta("HintUndoButton", _panel, art.CtaWater, TextStyle.CtaWater, UiText.Undo, 20f, onUndo, out _);
            _restart = ModalKit.Cta("HintRestartButton", _panel, art.CtaWood, TextStyle.PillWood, UiText.RestartLong, 18f, onRestart, out _);
            _root.gameObject.SetActive(false);
        }

        /// <summary>La tarjeta se ve.</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>La respuesta que enseña (o la última).</summary>
        public HintAnswer Answer { get; private set; } = HintAnswer.NotFound;

        /// <summary>La frase de la respuesta (vacía si no se ve).</summary>
        public string Text => Visible ? _body.text : string.Empty;

        /// <summary>Raíz (tests de orden de capas).</summary>
        public RectTransform Root => _root;

        /// <summary>El panel (tests de solapes y de pantalla).</summary>
        public RectTransform Panel => _panel;

        /// <summary>La frase (tests de tamaño: ≥ 12 sp).</summary>
        public TextMeshProUGUI Body => _body;

        public Button CloseButton => _close.Button;

        public Button UndoButton => _undo.Button;

        public Button RestartButton => _restart.Button;

        /// <summary>Deshacer se puede usar (lo decide la pantalla, como la ranura de Marea).</summary>
        public bool UndoEnabled => _undo.Relief.Interactable;

        /// <summary>
        /// Enseña la respuesta <paramref name="answer"/> (con la tinaja si <paramref name="tinaja"/>), con Deshacer habilitado o
        /// no (<paramref name="undoEnabled"/>), centrada en un lienzo de <paramref name="canvasW"/> × <paramref name="canvasH"/> dp.
        /// </summary>
        public void Show(HintAnswer answer, bool tinaja, bool undoEnabled, float canvasW, float canvasH)
        {
            Answer = answer;
            _body.text = UiText.HintAnswerText(answer, tinaja);
            UiBuilder.SetEnabled(_undo.Button, undoEnabled);
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Layout(canvasW, canvasH);
        }

        /// <summary>Recoloca la tarjeta (pantalla nueva) si se ve.</summary>
        public void Layout(float canvasW, float canvasH)
        {
            if (!Visible)
            {
                return;
            }

            float width = Mathf.Min(328f, canvasW - 32f);
            float inner = width - 32f;
            float y = 16f;
            UiBuilder.Place(_icon.rectTransform, 16f, y, IconDp, IconDp);
            UiBuilder.Place((RectTransform)_close.Button.transform, width - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
            y += IconDp + 8f;
            y += ModalKit.PlaceLine(_body, 16f, y, inner, 26f) + 14f;
            ModalKit.Place(_undo, 16f, y, inner);
            y += ModalKit.HeightOf(_undo) + 8f;
            ModalKit.Place(_restart, 16f, y, inner);
            y += ModalKit.HeightOf(_restart) + 14f;
            ModalKit.Size(_panel, _surface, _art, (canvasW - width) * 0.5f, Mathf.Round((canvasH - y) * 0.45f), width, y);
        }

        public void Hide()
        {
            if (Visible)
            {
                _root.gameObject.SetActive(false);
            }
        }

        /// <summary>Los botones a la vista para AF_LAYOUT <c>hint_answer</c>.</summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            ids.Add("close");
            rects.Add((RectTransform)_close.Button.transform);
            ids.Add("hint_undo");
            rects.Add((RectTransform)_undo.Button.transform);
            ids.Add("hint_restart");
            rects.Add((RectTransform)_restart.Button.transform);
        }
    }
}
