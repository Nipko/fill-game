#nullable enable
using System;
using System.Collections.Generic;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>Qué pregunta el diálogo de confirmación.</summary>
    internal enum ConfirmKind
    {
        /// <summary>Ninguna.</summary>
        None = 0,

        /// <summary>«¿Salir del nivel? Perderás 1 canoa» [P] (D-008; 04 §5.2), desde el 31.</summary>
        Quit = 1,

        /// <summary>«¿Volver a la orilla? Perderás los vertidos de este intento» con ≥ 8 vertidos (04 §2.7).</summary>
        Restart = 2,
    }

    /// <summary>
    /// Diálogo de confirmación de una línea (tarea B2-3): la salida con coste desde el 31, como «Are You Sure?» de Magic Sort
    /// (ficha §4.3: «You will lose 1 life!», «Quit» y X) con el material de D-003, y el reinicio con ≥ 8 vertidos con su
    /// atajo a Deshacer si queda cupo (04 §2.7). El botón de seguir jugando es el primario; el de confirmar, secundario.
    /// </summary>
    internal sealed class QuitConfirm
    {
        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _title;
        private readonly ReliefParts _close;
        private readonly Image _canoe;
        private readonly TextMeshProUGUI _body;
        private readonly ReliefParts _keep;
        private readonly ReliefParts _undo;
        private readonly ReliefParts _confirm;
        private readonly TextMeshProUGUI _confirmLabel;

        public QuitConfirm(Transform parent, ProceduralArt art, Action onConfirm, Action onKeepPlaying, Action onUndo)
        {
            _art = art;
            _root = UiBuilder.Rect("ConfirmDialog", parent);
            ModalKit.Dim("Dim", _root, 0.55f);
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            _title = ModalKit.Header(_panel, art, string.Empty, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, onKeepPlaying);
            _canoe = UiBuilder.Image("Canoe", _panel, art.IconCanoeEmpty, Color.white);
            _canoe.preserveAspect = true;
            _body = ModalKit.Line("Body", _panel, string.Empty, 17f, Palette.FamilyWood.Base);
            _keep = ModalKit.Cta("KeepPlayingButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, UiText.KeepPlaying, 21f, onKeepPlaying, out _);
            _undo = ModalKit.Cta("UndoShortcutButton", _panel, art.CtaWater, TextStyle.CtaWater, UiText.StuckUndo, 20f, onUndo, out _);
            _confirm = ModalKit.Cta("ConfirmButton", _panel, art.CtaWood, TextStyle.PillWood, string.Empty, 18f, onConfirm, out _confirmLabel);
            _root.gameObject.SetActive(false);
        }

        public ConfirmKind Kind { get; private set; }

        public bool Visible => _root.gameObject.activeSelf;

        public Button ConfirmButton => _confirm.Button;

        public Button KeepPlayingButton => _keep.Button;

        public Button UndoButton => _undo.Button;

        public string BodyText => _body.text;

        public void Hide()
        {
            Kind = ConfirmKind.None;
            _root.gameObject.SetActive(false);
        }

        /// <summary>Muestra la pregunta <paramref name="kind"/>; con <paramref name="undoShortcut"/>, el atajo a Deshacer.</summary>
        public void Show(ConfirmKind kind, bool undoShortcut, float canvasW, float canvasH)
        {
            Kind = kind;
            _root.gameObject.SetActive(true);
            bool quit = kind == ConfirmKind.Quit;
            _title.text = quit ? UiText.QuitConfirmTitle : UiText.RestartConfirmTitle;
            _body.text = quit ? UiText.QuitConfirmCanoe : UiText.RestartConfirmBody;
            _body.color = quit ? (Color)Palette.UiRedWarning : Palette.FamilyWood.Base;
            _confirmLabel.text = quit ? UiText.Quit : UiText.RestartLong;
            float width = Mathf.Min(328f, canvasW - 32f);
            float inner = width - 32f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner - 52f, 40f);
            UiBuilder.Place((RectTransform)_close.Button.transform, width - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
            y += 52f;
            _canoe.gameObject.SetActive(quit);
            if (quit)
            {
                UiBuilder.Place(_canoe.rectTransform, (width - 110f) * 0.5f, y, 110f, 65f);
                y += 70f;
            }

            UiBuilder.Place(_body.rectTransform, 16f, y, inner, 26f);
            y += 36f;
            ModalKit.Place(_keep, 16f, y, inner);
            y += ModalKit.HeightOf(_keep) + 8f;
            bool undo = !quit && undoShortcut;
            _undo.Button.gameObject.SetActive(undo);
            if (undo)
            {
                ModalKit.Place(_undo, 16f, y, inner);
                y += ModalKit.HeightOf(_undo) + 8f;
            }

            ModalKit.Place(_confirm, 16f, y, inner);
            y += ModalKit.HeightOf(_confirm) + 14f;
            ModalKit.Size(_panel, _surface, _art, (canvasW - width) * 0.5f, Mathf.Round((canvasH - y) * 0.45f), width, y);
        }

        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            ids.Add("keep_playing");
            rects.Add((RectTransform)_keep.Button.transform);
            if (_undo.Button.gameObject.activeSelf)
            {
                ids.Add("undo_shortcut");
                rects.Add((RectTransform)_undo.Button.transform);
            }

            ids.Add(Kind == ConfirmKind.Quit ? "confirm_quit" : "confirm_restart");
            rects.Add((RectTransform)_confirm.Button.transform);
        }
    }
}
