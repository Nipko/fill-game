#nullable enable
using System;
using System.Collections.Generic;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Menú de pausa (GDD 02 §5: «pausa (abre Ajustes, Volver a la orilla, Mapa del río)»; D-008; tarea B2-3), con la
    /// composición de Magic Sort (ficha §4.2: el engranaje despliega los botones y el tablero se oscurece) y el material de
    /// D-003: «Seguir jugando», «Volver a la orilla» (gratis; con ≥ 8 vertidos pide confirmación, 04 §2.7) y «Salir» (desde el
    /// 31 pide «¿Salir del nivel? Perderás 1 canoa» [P]; en 1-30 sale sin coste). Ajustes y Mapa del río llegan con sus
    /// pantallas.
    /// </summary>
    internal sealed class PauseMenu
    {
        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly ReliefParts _close;
        private readonly ReliefParts _resume;
        private readonly ReliefParts _restart;
        private readonly ReliefParts _quit;

        public PauseMenu(Transform parent, ProceduralArt art, Action onResume, Action onRestart, Action onQuit)
        {
            _art = art;
            _root = UiBuilder.Rect("PauseMenu", parent);
            ModalKit.Dim("Dim", _root, 0.55f);
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            ModalKit.Header(_panel, art, UiText.Pause, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, onResume);
            _resume = ModalKit.Cta("ResumeButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, UiText.KeepPlaying, 21f, onResume, out _);
            _restart = ModalKit.Cta("RestartButton", _panel, art.CtaWood, TextStyle.PillWood, UiText.RestartLong, 18f, onRestart, out _);
            _quit = ModalKit.Cta("QuitButton", _panel, art.CtaWood, TextStyle.PillWood, UiText.Quit, 18f, onQuit, out _);
            _root.gameObject.SetActive(false);
        }

        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Raíz del menú, hija del lienzo de la pantalla de nivel (tests de orden de capas).</summary>
        public RectTransform Root => _root;

        public Button ResumeButton => _resume.Button;

        public Button RestartButton => _restart.Button;

        public Button QuitButton => _quit.Button;

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        public void Show(bool canRestart, float canvasW, float canvasH)
        {
            _root.gameObject.SetActive(true);
            float width = Mathf.Min(300f, canvasW - 48f);
            float inner = width - 32f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner - 52f, 40f);
            UiBuilder.Place((RectTransform)_close.Button.transform, width - 16f - ModalKit.CloseDp, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
            y += 56f;
            ModalKit.Place(_resume, 16f, y, inner);
            y += ModalKit.HeightOf(_resume) + 10f;
            _restart.Button.gameObject.SetActive(canRestart);
            if (canRestart)
            {
                ModalKit.Place(_restart, 16f, y, inner);
                y += ModalKit.HeightOf(_restart) + 8f;
            }

            ModalKit.Place(_quit, 16f, y, inner);
            y += ModalKit.HeightOf(_quit) + 14f;
            ModalKit.Size(_panel, _surface, _art, (canvasW - width) * 0.5f, Mathf.Round((canvasH - y) * 0.45f), width, y);
        }

        public void VisibleButtons(List<string> ids, List<RectTransform> rects)
        {
            ids.Add("resume");
            rects.Add((RectTransform)_resume.Button.transform);
            if (_restart.Button.gameObject.activeSelf)
            {
                ids.Add("pause_restart");
                rects.Add((RectTransform)_restart.Button.transform);
            }

            ids.Add("quit");
            rects.Add((RectTransform)_quit.Button.transform);
        }
    }
}
