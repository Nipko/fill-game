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
    /// Menú de pausa (GDD 02 §5: «pausa (abre Ajustes, Volver a la orilla, Mapa del río)»; D-008; tarea B2-3), con la
    /// composición de Magic Sort (ficha §4.2: el engranaje despliega los botones y el tablero se oscurece) y el material de
    /// D-003: «Seguir jugando», «Mapa del río» (tarea M4.3), «Volver a la orilla» (gratis; con ≥ 8 vertidos pide confirmación,
    /// 04 §2.7) y «Salir» (desde el 31 pide «¿Salir del nivel? Perderás 1 canoa» [P]; en 1-30 sale sin coste). Ajustes llega con
    /// su pantalla.
    /// <para>«Mapa del río» (<c>river_map</c>, glosario §7.1; D-013, punto 7; 04 §5.2) abre el Mapa encima del nivel en pausa, con
    /// «Volver al nivel» en lugar de «Jugar»: no es salir del nivel, así que no gasta canoa ni cuenta como fallo. Sale con el
    /// Mapa en la escena y desde <c>max_level</c> ≥ 10, como la pestaña del inicio (D-008: oculto hasta el 10) [P].</para>
    /// <para>Música y Sonido (D-046, punto 2; 02 §5; 07 §9.4): dos botones redondos con icono arriba, junto a la X
    /// (<see cref="AudioToggles"/>), que se apagan o encienden con un toque y guardan lo mismo que las filas de Ajustes; la
    /// Vibración se queda en Ajustes.</para>
    /// <para>«Salto del Boto» (bloque B3; 04 §2.6: «el icono de Rosa aparece también en el menú de pausa»): con Rosa y su precio,
    /// solo si la opción se ofrece (<c>LevelFlow.SkipOffer</c>), en gris sin saldo; un toque abre la confirmación. En gris por el
    /// saldo, el toque no abre nada y responde <see cref="SkipMissing"/> (observación 2 de la jornada 6 en el teléfono, 2026-10-28:
    /// era el único botón en gris sin respuesta; como el del modal de atasco): late su etiqueta, que no tiene línea «Te faltan».</para>
    /// </summary>
    internal sealed class PauseMenu
    {
        /// <summary>Ancho máximo de la tarjeta con Música y Sonido arriba (dp) [P]; sin ellos, 300 como siempre.</summary>
        public const float AudioWidthDp = 328f;

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly ReliefParts _close;
        private readonly AudioToggles _audio;
        private readonly ReliefParts _resume;
        private readonly ReliefParts _map;
        private readonly ReliefParts _skip;
        private readonly TextMeshProUGUI _skipLabel;
        private readonly Image _skipRosa;
        private readonly PriceTag _skipPrice;
        private readonly NotePulse _skipPulse;
        private SkipOffer _skipOffer;
        private readonly ReliefParts _restart;
        private readonly ReliefParts _quit;

        public PauseMenu(Transform parent, ProceduralArt art, Action onResume, Action onMap, Action onRestart, Action onQuit, Action? onSkip = null)
        {
            _art = art;
            _root = UiBuilder.Rect("PauseMenu", parent);
            ModalKit.Dim("Dim", _root, 0.55f);
            _panel = ModalKit.Card("Panel", _root, art, out _surface);
            ModalKit.Header(_panel, art, UiText.Pause, out _header);
            _close = ModalKit.Close("CloseButton", _panel, art, onResume);
            _audio = new AudioToggles(_panel, art); // D-046, punto 2: Música y Sonido, junto a la X
            _audio.SetVisible(false);
            _resume = ModalKit.Cta("ResumeButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, UiText.KeepPlaying, 21f, onResume, out _);
            _map = ModalKit.Cta("MapButton", _panel, art.CtaWood, TextStyle.PillWood, UiText.RiverMap, 18f, onMap, out _);
            _skip = ModalKit.Cta("SkipButton", _panel, art.CtaWood, TextStyle.PillWood, SkipText.Option, 18f, () => OnSkip(onSkip), out _skipLabel);
            _skipPulse = new NotePulse(_skipLabel); // sin línea «Te faltan», late la etiqueta (como «+3 Deshacer»)
            GreyA11y.Register(_skip.Button, () => SkipMissingSeeds); // TalkBack: «Salto del Boto, 300, Te faltan {n} semillas»
            _skipRosa = UiBuilder.Image("Rosa", _skip.Face, null, Color.white); // el sprite, al enseñarlo (las mascotas, solo si hacen falta)
            _skipRosa.preserveAspect = true;
            _skipPrice = new PriceTag("Price", _skip.Face, art);
            _skip.Button.gameObject.SetActive(false);
            _restart = ModalKit.Cta("RestartButton", _panel, art.CtaWood, TextStyle.PillWood, UiText.RestartLong, 18f, onRestart, out _);
            _quit = ModalKit.Cta("QuitButton", _panel, art.CtaWood, TextStyle.PillWood, UiText.Quit, 18f, onQuit, out _);
            _root.gameObject.SetActive(false);
        }

        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>Raíz del menú, hija del lienzo de la pantalla de nivel (tests de orden de capas).</summary>
        public RectTransform Root => _root;

        public Button ResumeButton => _resume.Button;

        /// <summary>Botones de un toque Música y Sonido, arriba junto a la X (D-046, punto 2; 02 §5).</summary>
        internal AudioToggles Audio => _audio;

        /// <summary>
        /// La pausa enseña Música y Sonido (quien la abre tiene dónde guardarlos: <c>GameScreen.AudioToggled</c>). Se lee en
        /// <see cref="Show"/>.
        /// </summary>
        internal bool AudioOffered { get; set; }

        /// <summary>«Mapa del río» (tarea M4.3): solo se ve si <see cref="Show"/> lo pidió.</summary>
        public Button MapButton => _map.Button;

        /// <summary>«Salto del Boto»: solo se ve si <see cref="Show"/> lo pidió.</summary>
        public Button SkipButton => _skip.Button;

        /// <summary>Sprite de Rosa del botón (null hasta que <see cref="Show"/> lo enseña; tests).</summary>
        internal Sprite? SkipRosaSprite => _skipRosa.sprite;

        public Button RestartButton => _restart.Button;

        public Button QuitButton => _quit.Button;

        /// <summary>
        /// Toque en «Salto del Boto» en gris por el saldo (observación 2 de la jornada 6): lo pone <c>GameScreen</c>, que hace latir
        /// la etiqueta (<see cref="PulseSkipMissing"/>), lo anuncia con TalkBack, avisa si la tienda no vende y escribe
        /// <c>AF_STATE missing_tap</c> con <c>pause_skip</c>.
        /// </summary>
        public Action? SkipMissing { get; set; }

        /// <summary>Semillas que le faltan al Salto del Boto que enseña la pausa (0 si alcanza o no se ofrece).</summary>
        internal int SkipMissingSeeds => _skipOffer.Offered && !_skipOffer.CanUse ? _skipOffer.Missing : 0;

        /// <summary>Latido de la etiqueta del Salto del Boto en gris (tests).</summary>
        internal NotePulse SkipPulse => _skipPulse;

        /// <summary>
        /// Late la etiqueta «Salto del Boto» (con Reducir movimiento, solo se atenúa). False si la pausa no se ve o el salto no está
        /// en gris por el saldo.
        /// </summary>
        public bool PulseSkipMissing(bool reduceMotion)
        {
            return Visible && _skip.Button.gameObject.activeSelf && SkipMissingSeeds > 0 && _skipPulse.Start(reduceMotion);
        }

        /// <summary>Un fotograma del latido (lo llama <c>GameScreen.Tick</c>).</summary>
        public void Tick(float dtMs)
        {
            _skipPulse.Tick(dtMs);
        }

        public void Hide()
        {
            _skipPulse.Stop();
            _root.gameObject.SetActive(false);
        }

        // «Salto del Boto»: en gris por el saldo, no abre la confirmación y responde SkipMissing; si no, la confirmación.
        private void OnSkip(Action? onSkip)
        {
            if (SkipMissingSeeds > 0)
            {
                SkipMissing?.Invoke();
                return;
            }

            onSkip?.Invoke();
        }

        /// <summary>
        /// Abre el menú: «Volver a la orilla» si <paramref name="canRestart"/> y «Mapa del río» si <paramref name="canMap"/> (el
        /// Mapa en la escena y <c>max_level</c> ≥ 10; tarea M4.3) y «Salto del Boto» si <paramref name="skip"/> se ofrece (B3).
        /// </summary>
        public void Show(bool canRestart, bool canMap, float canvasW, float canvasH, SkipOffer skip = default)
        {
            _root.gameObject.SetActive(true);

            // Con Música y Sonido en la fila de arriba (D-046), la tarjeta se ensancha [P] para que el título siga cabiendo.
            bool audio = AudioOffered;
            float width = audio ? Mathf.Min(AudioWidthDp, canvasW - 32f) : Mathf.Min(300f, canvasW - 48f);
            float inner = width - 32f;
            float y = 16f;
            float closeX = width - 16f - ModalKit.CloseDp;
            float headerW = inner - 52f;
            _audio.SetVisible(audio);
            if (audio)
            {
                // Título · Música · Sonido · X, con 4 dp entre cada uno, todos a la altura de la X.
                float audioX = closeX - AudioToggles.GapDp - AudioToggles.RowWidthDp;
                _audio.PlaceRow(audioX, y - 4f);
                headerW = audioX - AudioToggles.GapDp - 16f;
            }

            UiBuilder.Place(_header.rectTransform, 16f, y, headerW, 40f);
            UiBuilder.Place((RectTransform)_close.Button.transform, closeX, y - 4f, ModalKit.CloseDp, ModalKit.CloseDp);
            y += 56f;
            ModalKit.Place(_resume, 16f, y, inner);
            y += ModalKit.HeightOf(_resume) + 10f;
            _map.Button.gameObject.SetActive(canMap);
            if (canMap)
            {
                ModalKit.Place(_map, 16f, y, inner);
                y += ModalKit.HeightOf(_map) + 8f;
            }

            _skipPulse.Stop(); // la etiqueta vuelve a su sitio antes de recolocarla
            _skipOffer = skip;
            bool canSkip = skip.Offered;
            _skip.Button.gameObject.SetActive(canSkip);
            if (canSkip)
            {
                if (_skipRosa.sprite == null)
                {
                    _skipRosa.sprite = _art.Rosa;
                }

                UiBuilder.SetEnabled(_skip.Button, skip.CanUse);
                if (!skip.CanUse && skip.Missing > 0)
                {
                    _skip.Button.interactable = true; // gris por el saldo, pero el toque responde (OnSkip; observación 2 de la jornada 6)
                }

                _skipPrice.Set(skip.Price, 0, !skip.CanUse);
                ModalKit.FitCtaLabel(_skip, _skipLabel, inner, _skipPrice.Width + 76f, 20f - ((_skipPrice.Width + 12f) * 0.5f));
                ModalKit.Place(_skip, 16f, y, inner);
                float face = ModalKit.FaceHeightOf(_skip);
                UiBuilder.Place(_skipRosa.rectTransform, 10f, (face - 28f) * 0.5f, 36f, 28f);
                _skipPrice.PlaceAt(inner - _skipPrice.Width - 12f, (face - _skipPrice.Height) * 0.5f);
                y += ModalKit.HeightOf(_skip) + 8f;
            }

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
            if (_map.Button.gameObject.activeSelf)
            {
                ids.Add("pause_map");
                rects.Add((RectTransform)_map.Button.transform);
            }

            if (_skip.Button.gameObject.activeSelf)
            {
                ids.Add("pause_skip");
                rects.Add((RectTransform)_skip.Button.transform);
            }

            if (_restart.Button.gameObject.activeSelf)
            {
                ids.Add("pause_restart");
                rects.Add((RectTransform)_restart.Button.transform);
            }

            ids.Add("quit");
            rects.Add((RectTransform)_quit.Button.transform);
            _audio.VisibleButtons(ids, rects, "pause_"); // D-046: Música y Sonido, al final para no mover los ids de siempre
        }
    }
}
