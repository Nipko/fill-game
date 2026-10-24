#nullable enable
using System;
using System.Collections.Generic;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Los dos botones de un toque Música y Sonido (D-046, punto 2; 07 §9.4; 02 §5), que comparten el menú de pausa
    /// (<see cref="PauseMenu"/>, junto a la X) y el inicio (<see cref="LobbyScreen"/>, junto al engranaje de Ajustes):
    /// <list type="bullet">
    /// <item>discos de madera de 40 dp, como el engranaje y la X (07 §7.6), en un área táctil de <see cref="TouchDp"/> dp (07 §1.2 y
    /// §7.8), con la nota y el altavoz dibujados por código (<see cref="ProceduralArt.IconMusic"/>…); apagado, el icono
    /// tachado;</item>
    /// <item>un toque pide el valor contrario por <see cref="Toggled"/> (con <see cref="SettingsScreen.MusicId"/> o
    /// <see cref="SettingsScreen.SoundId"/>): quien los usa lo guarda por el mismo camino que las filas de Ajustes
    /// (<c>GameBootstrap.ChooseAudio</c>, en <c>settings.music</c> y <c>settings.sound</c>) y devuelve lo guardado con
    /// <see cref="Mark"/>, así que siempre coinciden con Ajustes;</item>
    /// <item>TalkBack lee el ajuste con su estado («Música: activada», «Sonido: desactivado»; <see cref="UiText.MusicA11y"/> y
    /// <see cref="UiText.SoundA11y"/>).</item>
    /// </list>
    /// La Vibración no tiene botón aquí: se queda solo en Ajustes (D-046). Sin asignaciones mientras nada cambia.
    /// </summary>
    internal sealed class AudioToggles
    {
        /// <summary>Lado del área táctil de cada botón (dp; 07 §1.2 y §7.8).</summary>
        public const float TouchDp = 48f;

        /// <summary>Hueco entre los dos botones (dp) [P].</summary>
        public const float GapDp = 4f;

        private readonly ProceduralArt _art;
        private readonly ReliefParts _music;
        private readonly ReliefParts _sound;
        private readonly Image _musicIcon;
        private readonly Image _soundIcon;
        private bool _musicOn = true;
        private bool _soundOn = true;

        /// <summary>Crea los dos botones bajo <paramref name="parent"/>, encendidos hasta el primer <see cref="Mark"/>.</summary>
        public AudioToggles(Transform parent, ProceduralArt art)
        {
            _art = art;
            _music = Disc("MusicButton", parent, art, () => Toggled?.Invoke(SettingsScreen.MusicId, !_musicOn), out _musicIcon);
            _sound = Disc("SoundButton", parent, art, () => Toggled?.Invoke(SettingsScreen.SoundId, !_soundOn), out _soundIcon);
            A11yLabels.Set(_music.Button, () => UiText.MusicA11y(_musicOn));
            A11yLabels.Set(_sound.Button, () => UiText.SoundA11y(_soundOn));
            Mark(true, true);
        }

        /// <summary>
        /// Toque en un botón: su id (<see cref="SettingsScreen.MusicId"/> o <see cref="SettingsScreen.SoundId"/>) y el valor que pide
        /// (el contrario del que enseña). El valor que se ve solo cambia con <see cref="Mark"/>.
        /// </summary>
        public Action<string, bool>? Toggled { get; set; }

        public Button MusicButton => _music.Button;

        public Button SoundButton => _sound.Button;

        public RectTransform MusicRect => (RectTransform)_music.Button.transform;

        public RectTransform SoundRect => (RectTransform)_sound.Button.transform;

        /// <summary>La Música se ve encendida.</summary>
        public bool MusicOn => _musicOn;

        /// <summary>El Sonido se ve encendido.</summary>
        public bool SoundOn => _soundOn;

        /// <summary>Icono que enseña el botón de Música (tests: tachado = apagado).</summary>
        public Sprite? MusicSprite => _musicIcon.sprite;

        /// <summary>Icono que enseña el botón de Sonido.</summary>
        public Sprite? SoundSprite => _soundIcon.sprite;

        /// <summary>Los botones están a la vista.</summary>
        public bool Visible => _music.Button.gameObject.activeSelf;

        /// <summary>Enseña lo guardado en <c>settings.music</c> y <c>settings.sound</c>: el icono tachado si está apagado.</summary>
        public void Mark(bool music, bool sound)
        {
            _musicOn = music;
            _soundOn = sound;
            Sprite musicSprite = music ? _art.IconMusic : _art.IconMusicOff;
            if (_musicIcon.sprite != musicSprite)
            {
                _musicIcon.sprite = musicSprite;
            }

            Sprite soundSprite = sound ? _art.IconSound : _art.IconSoundOff;
            if (_soundIcon.sprite != soundSprite)
            {
                _soundIcon.sprite = soundSprite;
            }
        }

        /// <summary>Enseña u oculta los dos botones.</summary>
        public void SetVisible(bool visible)
        {
            if (_music.Button.gameObject.activeSelf != visible)
            {
                _music.Button.gameObject.SetActive(visible);
            }

            if (_sound.Button.gameObject.activeSelf != visible)
            {
                _sound.Button.gameObject.SetActive(visible);
            }
        }

        /// <summary>Coloca la Música en (<paramref name="x"/>, <paramref name="y"/>) dp del padre y el Sonido a su derecha, en fila.</summary>
        public void PlaceRow(float x, float y)
        {
            UiBuilder.Place(MusicRect, x, y, TouchDp, TouchDp);
            UiBuilder.Place(SoundRect, x + TouchDp + GapDp, y, TouchDp, TouchDp);
        }

        /// <summary>Coloca la Música en (<paramref name="x"/>, <paramref name="y"/>) dp del padre y el Sonido debajo, en columna.</summary>
        public void PlaceColumn(float x, float y)
        {
            UiBuilder.Place(MusicRect, x, y, TouchDp, TouchDp);
            UiBuilder.Place(SoundRect, x, y + TouchDp + GapDp, TouchDp, TouchDp);
        }

        /// <summary>Ancho de los dos en fila (dp).</summary>
        public static float RowWidthDp => (2f * TouchDp) + GapDp;

        /// <summary>Alto de los dos en columna (dp).</summary>
        public static float ColumnHeightDp => (2f * TouchDp) + GapDp;

        /// <summary>Botones visibles para <c>AF_LAYOUT</c> (sonda de desarrollo), con <paramref name="prefix"/> delante del id.</summary>
        public void VisibleButtons(List<string> ids, List<RectTransform> rects, string prefix)
        {
            if (!Visible)
            {
                return;
            }

            ids.Add(prefix + "music");
            rects.Add(MusicRect);
            ids.Add(prefix + "sound");
            rects.Add(SoundRect);
        }

        // Disco de madera de 40 dp en un área de 48, con el icono de 26 dp centrado, como el engranaje del inicio (07 §7.6).
        private static ReliefParts Disc(string name, Transform parent, ProceduralArt art, Action onClick, out Image icon)
        {
            ReliefParts disc = UiBuilder.Relief(name, parent, art.DiscWood, onClick);
            var visual = (RectTransform)disc.Face.parent;
            visual.anchorMin = new Vector2(0.5f, 1f);
            visual.anchorMax = new Vector2(0.5f, 1f);
            visual.sizeDelta = new Vector2(art.DiscWood.Painted ? art.DiscWood.HeightDp : 40f, art.DiscWood.HeightDp);
            visual.anchoredPosition = new Vector2(0f, -4f);
            icon = UiBuilder.Image("Icon", disc.Face, null, Color.white);
            UiBuilder.Place(icon.rectTransform, 7f, 7f, 26f, 26f);
            icon.raycastTarget = false;

            // Sin glifo en el relieve: el icono cambia con el estado y el botón nunca se deshabilita.
            disc.Relief.Bind(disc.Face, disc.Base, disc.FaceImage, art.DiscWood, null, null, null);
            return disc;
        }
    }
}
