#nullable enable
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Estados de un botón con relieve (guía visual v2, 5.1): al pulsar, la cara y su contenido bajan 4 dp (el canto
    /// visible pasa de 6 a 2) con escala 0,97 en 80 ms ease-out; al soltar vuelven en 140 ms con easeOutBack (c = 1,5). La
    /// acción la dispara el <see cref="Button"/> al soltar, como antes. Deshabilitado: plano, sin canto ni sombra, con la
    /// silueta y la etiqueta legibles (<c>deshabilitado_texto</c> 4,85:1, <c>deshabilitado_glifo</c> 3,79:1).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReliefButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float PressMs = 80f;
        private const float ReleaseMs = 140f;

        private RectTransform? _face;
        private Image? _base;
        private Image? _faceImage;
        private Sprite? _faceSprite;
        private Sprite? _faceDisabled;
        private Image? _glyph;
        private Sprite? _glyphSprite;
        private Sprite? _glyphDisabled;
        private TextMeshProUGUI? _label;
        private Material? _labelMaterial;
        private Material? _labelDisabledMaterial;
        private Color _labelColor;
        private bool _painted;
        private bool _interactable = true;
        private bool _pressed;
        private float _t = -1f;
        private float _value;
        private float _from;

        /// <summary>El botón está habilitado.</summary>
        public bool Interactable => _interactable;

        /// <summary>La cara está pulsada (o bajando).</summary>
        public bool Pressed => _pressed;

        /// <summary>Desplazamiento actual de la cara (dp hacia abajo).</summary>
        public float FaceOffsetDp => _face != null ? -_face.anchoredPosition.y : 0f;

        /// <summary>Enlaza las piezas (las crea <see cref="UiBuilder"/>).</summary>
        public void Bind(RectTransform face, Image? baseImage, Image faceImage, ReliefSprites sprites, Image? glyph, Sprite? glyphDisabled, TextMeshProUGUI? label)
        {
            _face = face;
            _base = baseImage;
            _faceImage = faceImage;
            _faceSprite = sprites.Face;
            _faceDisabled = sprites.FaceDisabled;
            _painted = sprites.Painted;
            _glyph = glyph;
            _glyphSprite = glyph != null ? glyph.sprite : null;
            _glyphDisabled = glyphDisabled;
            _label = label;
            if (label != null)
            {
                _labelMaterial = label.fontSharedMaterial;
                _labelColor = label.color;
                // Deshabilitado sin contorno ni sombra (guía 5.1: «etiqueta legible»; 07 §1.2 y §7.8: contraste ≥ 4,5:1). El
                // material de TextStyle.Disabled es de Nunito: con otra fuente (Baloo 2 en los CTA) su atlas no casa y el texto
                // saldría roto, así que se usa el material base del font asset de la etiqueta (la cara sola, sin contorno ni
                // sombra). Antes se conservaba el material del CTA: el contorno verde grueso rellenaba los huecos de las letras
                // del texto oscuro sobre el gris (revisión de 12c_stuck_l31_noseeds: «Continuar: +1 cuia» sin saldo).
                _labelDisabledMaterial = UiFonts.Font(TextStyle.Disabled) == label.font
                    ? UiFonts.Material(TextStyle.Disabled)
                    : (label.font != null ? label.font.material : null);
            }
        }

        /// <summary>Activa o desactiva el botón y su aspecto (plano y desaturado si está deshabilitado).</summary>
        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            Button? button = GetComponent<Button>();
            if (button != null && button.interactable != interactable)
            {
                button.interactable = interactable;
            }

            if (_base != null)
            {
                _base.enabled = interactable;
            }

            if (_faceImage != null)
            {
                Sprite? s = interactable ? _faceSprite : (_faceDisabled != null ? _faceDisabled : _faceSprite);
                if (_faceImage.sprite != s)
                {
                    _faceImage.sprite = s;
                }
            }

            if (_glyph != null)
            {
                _glyph.sprite = interactable || _glyphDisabled == null ? _glyphSprite : _glyphDisabled;
            }

            if (_label != null)
            {
                _label.color = interactable ? _labelColor : Palette.DisabledText;
                Material? m = interactable ? _labelMaterial : (_labelDisabledMaterial ?? _labelMaterial);
                if (m != null && _label.fontSharedMaterial != m)
                {
                    _label.fontSharedMaterial = m;
                }
            }

            if (!interactable)
            {
                _pressed = false;
                _t = -1f;
                Apply(0f);
            }
        }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_interactable)
            {
                Press(true);
            }
        }

        /// <inheritdoc />
        public void OnPointerUp(PointerEventData eventData)
        {
            Press(false);
        }

        /// <inheritdoc />
        public void OnPointerExit(PointerEventData eventData)
        {
            if (_pressed)
            {
                Press(false);
            }
        }

        /// <summary>Pulsa o suelta la cara (también para pruebas).</summary>
        public void Press(bool down)
        {
            if (_pressed == down)
            {
                return;
            }

            _pressed = down;
            _from = _value;
            _t = 0f;
        }

        /// <summary>Avanza la animación <paramref name="dtMs"/> milisegundos (la llama <c>Update</c>; las pruebas también).</summary>
        public void Step(float dtMs)
        {
            if (_t < 0f)
            {
                return;
            }

            _t += dtMs;
            float duration = _pressed ? PressMs : ReleaseMs;
            float p = Mathf.Clamp01(_t / duration);
            float v = _pressed ? Mathf.Lerp(_from, 1f, Ease.Out(p)) : Mathf.LerpUnclamped(_from, 0f, Ease.OutBack(p, 1.5f));
            Apply(v);
            if (p >= 1f)
            {
                _t = -1f;
                Apply(_pressed ? 1f : 0f);
            }
        }

        private void Update()
        {
            Step(Time.unscaledDeltaTime * 1000f);
        }

        private void Apply(float v)
        {
            _value = v;
            if (_face == null)
            {
                return;
            }

            float drop = (_painted ? 3f : ProceduralArt.ButtonPressDp) * v;
            _face.anchoredPosition = new Vector2(0f, -drop);
            _face.localScale = Vector3.one * Mathf.LerpUnclamped(1f, 0.97f, v);
        }
    }
}
