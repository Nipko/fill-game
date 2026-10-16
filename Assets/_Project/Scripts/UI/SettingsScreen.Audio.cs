#nullable enable
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Filas Sonido, Música y Vibración de Ajustes (tarea AU1; 07 §9.4: «Sonido, Música y Vibración activados por defecto», se
    /// apagan por separado; D-008: ya existen, así que se ven; glosario §10, <c>sound</c> / <c>music</c> / <c>haptics</c>), como la
    /// de «Mensajes de TiTino»: cada una se marca o se desmarca y quien abre la pantalla (<c>GameBootstrap.ChooseAudio</c>) lo
    /// guarda en <c>settings.sound</c>, <c>music</c> o <c>haptics</c> (08 §4.3) y lo aplica en el acto. Parcial de
    /// <see cref="SettingsScreen"/>.
    /// </summary>
    internal sealed partial class SettingsScreen
    {
        /// <summary>Id de la fila Sonido (sonda) y clave de <c>af_settings_change</c> (08 §8.2): <c>sound</c>.</summary>
        public const string SoundId = "sound";

        /// <summary>Id de la fila Música y clave de <c>af_settings_change</c>: <c>music</c>.</summary>
        public const string MusicId = "music";

        /// <summary>Id de la fila Vibración y clave de <c>af_settings_change</c>: <c>haptics</c>.</summary>
        public const string HapticsId = "haptics";

        private const int AudioRowCount = 3;

        private readonly OptionRow[] _audioRows = new OptionRow[AudioRowCount];
        private readonly bool[] _audioOn = { true, true, true };

        /// <summary>
        /// Al tocar una fila de audio: su id (<see cref="SoundId"/>, <see cref="MusicId"/> o <see cref="HapticsId"/>) y el valor
        /// nuevo. Lo fija <c>GameBootstrap</c>.
        /// </summary>
        public Action<string, bool>? AudioToggled { get; set; }

        /// <summary>Hueco de las tres filas de audio.</summary>
        public RectTransform AudioSlot { get; private set; } = null!;

        /// <summary>Botón de la fila <paramref name="id"/> (<c>sound</c>, <c>music</c> o <c>haptics</c>).</summary>
        public Button AudioButton(string id)
        {
            return _audioRows[AudioIndex(id)].Button;
        }

        /// <summary>Texto de la fila <paramref name="id"/>.</summary>
        public string AudioLabel(string id)
        {
            return _audioRows[AudioIndex(id)].Label.text;
        }

        /// <summary>Si la fila <paramref name="id"/> lleva la marca (activado).</summary>
        public bool AudioMarked(string id)
        {
            return _audioRows[AudioIndex(id)].Selected;
        }

        /// <summary>Marca lo guardado en <c>settings.sound</c>, <c>music</c> y <c>haptics</c> (al abrir y tras tocar una fila).</summary>
        public void MarkAudio(bool sound, bool music, bool haptics)
        {
            _audioOn[0] = sound;
            _audioOn[1] = music;
            _audioOn[2] = haptics;
            for (int i = 0; i < AudioRowCount; i++)
            {
                _audioRows[i].SetSelected(_audioOn[i]);
            }
        }

        /// <summary>Visor del contenido desplazable (tests: queda dentro de la crema, a <see cref="PanelInsetDp"/> del borde).</summary>
        public RectTransform Viewport => _viewport;

        /// <summary>El contenido no cabe y se desplaza (tests y capturas).</summary>
        public bool Scrolls => _scroll.vertical;

        /// <summary>Desplaza el contenido hasta el final (capturas: lo que queda debajo al 130 %).</summary>
        public void ScrollToEnd()
        {
            _scroll.verticalNormalizedPosition = 0f;
        }

        private void BuildAudioRows()
        {
            AudioSlot = UiBuilder.Rect("AudioSlot", _content);
            _audioRows[0] = new OptionRow(AudioSlot, _art, "Sound", SoundId, UiText.Sound, () => ToggleAudio(0));
            _audioRows[1] = new OptionRow(AudioSlot, _art, "Music", MusicId, UiText.Music, () => ToggleAudio(1));
            _audioRows[2] = new OptionRow(AudioSlot, _art, "Haptics", HapticsId, UiText.Haptics, () => ToggleAudio(2));
            MarkAudio(true, true, true);
        }

        private void ToggleAudio(int index)
        {
            AudioToggled?.Invoke(_audioRows[index].Id, !_audioOn[index]);
        }

        // Las tres filas en el contenido desde y; devuelve la y de lo que sigue (con el hueco de sección).
        private float LayoutAudio(float y, float inner, float rowH, float s)
        {
            float slotH = (AudioRowCount * (rowH + RowGapDp)) - RowGapDp;
            UiBuilder.Place(AudioSlot, 16f, y, inner, slotH);
            for (int i = 0; i < AudioRowCount; i++)
            {
                _audioRows[i].Place(0f, i * (rowH + RowGapDp), inner, rowH, s);
            }

            return y + slotH + SectionGapDp;
        }

        private IEnumerable<TextMeshProUGUI> AudioTexts()
        {
            for (int i = 0; i < AudioRowCount; i++)
            {
                yield return _audioRows[i].Label;
            }
        }

        private void AudioLayoutIds(string[] ids, RectTransform[] rects, int k)
        {
            for (int i = 0; i < AudioRowCount; i++, k++)
            {
                ids[k] = _audioRows[i].Id;
                rects[k] = _audioRows[i].Root;
            }
        }

        private static int AudioIndex(string id)
        {
            switch (id)
            {
                case SoundId: return 0;
                case MusicId: return 1;
                case HapticsId: return 2;
                default: throw new ArgumentOutOfRangeException(nameof(id), "Fila de audio desconocida: " + id);
            }
        }
    }
}
