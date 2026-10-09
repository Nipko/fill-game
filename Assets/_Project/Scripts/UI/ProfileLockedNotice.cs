#nullable enable
using AF.Presentation;
using AF.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Aviso de perfil en solo lectura (08 §4.4: si el archivo es de un esquema más nuevo o una migración falla, «se conserva
    /// el perfil anterior»): el almacén protege el archivo y nada de esta sesión se puede guardar, así que al arrancar se
    /// avisa con «Actualiza Tintalú» (el texto de la pantalla de <c>biz_min_app_version</c> de 08 §9) y no se ofrecen compras
    /// (<see cref="GameServices.PurchasesBlocked"/>). Se puede seguir jugando sin guardar. Es la medida mínima [P]: si el
    /// dueño prefiere una pantalla de bloqueo sin juego, este aviso pierde su botón. Tarjeta de madera clara (D-003) en un
    /// lienzo propio por encima de todo.
    /// </summary>
    internal sealed class ProfileLockedNotice
    {
        private readonly ProceduralArt _art;
        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _panel;
        private readonly Image _surface;
        private readonly Image _header;
        private readonly TextMeshProUGUI _body;
        private readonly ReliefParts _play;

        public ProfileLockedNotice(Transform host, ProceduralArt art)
        {
            _art = art;
            _canvas = new OverlayCanvas("ProfileLockedCanvas", host, 70);
            ModalKit.Dim("Dim", _canvas.Root, 0.62f);
            _panel = ModalKit.Card("Panel", _canvas.Root, art, out _surface);
            ModalKit.Header(_panel, art, UiText.ProfileLockedTitle, out _header);
            _body = ModalKit.Line("Body", _panel, string.Empty, 16f, Palette.FamilyWood.Base);
            _play = ModalKit.Cta("PlayButton", _panel, art.CtaLeaf, TextStyle.CtaLeaf, UiText.ProfileLockedPlay, 20f, Hide, out _);
            _canvas.Canvas.gameObject.SetActive(false);
        }

        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Lienzo propio (capturas de prueba).</summary>
        public Canvas Canvas => _canvas.Canvas;

        public string BodyText => _body.text;

        public Button PlayButton => _play.Button;

        /// <summary>Muestra el aviso para <paramref name="status"/> (<c>TooNew</c> o <c>MigrationFailed</c>).</summary>
        public void Show(ProfileLoadStatus status)
        {
            _body.text = status == ProfileLoadStatus.TooNew ? UiText.ProfileLockedTooNew : UiText.ProfileLockedMigration;
            _canvas.Canvas.gameObject.SetActive(true);
            _canvas.Measure();
            float w = Mathf.Min(328f, _canvas.Width - 32f);
            float inner = w - 32f;
            float y = 16f;
            UiBuilder.Place(_header.rectTransform, 16f, y, inner, 40f);
            y += 52f;
            UiBuilder.Place(_body.rectTransform, 16f, y, inner, 112f);
            y += 120f;
            ModalKit.Place(_play, 16f, y, inner);
            y += ModalKit.HeightOf(_play) + 16f;
            ModalKit.Size(_panel, _surface, _art, (_canvas.Width - w) * 0.5f, Mathf.Round((_canvas.Height - y) * 0.45f), w, y);
        }

        public void Hide()
        {
            _canvas.Canvas.gameObject.SetActive(false);
        }
    }
}
