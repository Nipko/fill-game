#nullable enable
using System;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// El avatar del jugador en el inicio (D-056; <c>docs/dev/avatares.md</c> §3.1, Q-AV-3 con la recomendada): un disco de 40 dp con
    /// su área de 48 dp arriba a la izquierda, en la barra de los saldos, desde el 10 (lo decide <c>GameBootstrap</c>). Encima, el
    /// punto de heliconia con su canto crema del Mapa (un avatar nuevo sin ver o aún sin elegir). Tocarlo abre «Tu avatar». Parcial
    /// de <see cref="LobbyScreen"/>.
    /// </summary>
    public sealed partial class LobbyScreen
    {
        /// <summary>Diámetro del retrato (dp).</summary>
        public const float AvatarDiscDp = 40f;

        /// <summary>Borde izquierdo del área táctil del retrato (dp).</summary>
        public const float AvatarLeftDp = 8f;

        /// <summary>Semillas, con el avatar a la vista (dp).</summary>
        public const float SeedsWithAvatarX = 60f;

        private const float CanoesMaxW = 128f;
        private const float CanoesMinW = 104f;
        private const float SeedsMaxW = 112f;

        private Button _avatarButton = null!;
        private Image _avatarImage = null!;
        private RectTransform _avatarDot = null!;
        private bool _avatarShown;
        private bool _avatarDotShown;
        private string _avatarA11y = string.Empty;
        private float _canoesW = CanoesMaxW;

        /// <summary>Al tocar el retrato (lo fija <c>GameBootstrap</c>: abre «Tu avatar» con <c>source = home</c>).</summary>
        public Action? AvatarTapped { get; set; }

        /// <summary>El retrato se ve.</summary>
        public bool AvatarShown => _avatarShown;

        /// <summary>El punto «nuevo» se ve.</summary>
        public bool AvatarDotShown => _avatarShown && _avatarDotShown;

        /// <summary>El botón del retrato (área de 48 dp).</summary>
        public Button AvatarButton => _avatarButton;

        /// <summary>Su recuadro.</summary>
        public RectTransform AvatarRect => (RectTransform)_avatarButton.transform;

        /// <summary>El sprite que pinta.</summary>
        public Sprite? AvatarSprite => _avatarImage.sprite;

        /// <summary>Lo que lee TalkBack («Tu avatar: Tucán. Toca para cambiarlo» y, con el punto, «Hay un avatar nuevo»).</summary>
        public string AvatarA11y => _avatarA11y;

        /// <summary>
        /// Enseña u oculta el retrato: <paramref name="portrait"/> (el elegido), el punto y la lectura de TalkBack. Si cambia si se ve,
        /// la fila de arriba se vuelve a colocar (Semillas y Canoas se apartan).
        /// </summary>
        public void SetAvatar(Sprite? portrait, bool shown, bool dot, string a11y)
        {
            bool relayout = shown != _avatarShown;
            _avatarShown = shown;
            _avatarDotShown = dot;
            _avatarA11y = a11y ?? string.Empty;
            _avatarImage.sprite = portrait;
            _avatarImage.enabled = portrait != null;
            _avatarButton.gameObject.SetActive(shown);
            _avatarDot.gameObject.SetActive(shown && dot);
            if (relayout && _canvasW > 0f)
            {
                Layout();
                EmitLayout();
            }
        }

        /// <summary>
        /// La fila de arriba con un lienzo de <paramref name="w"/> dp (avatares.md §3.1): sin avatar, Semillas en 16-128 y Canoas en
        /// 140-268 (o 4 dp antes del engranaje); con él, el retrato en 8-56, Semillas desde 60 y Canoas pegada al engranaje (4 dp), de
        /// 128 dp o, si no cabe, hasta 104, y Semillas hasta 4 dp antes de Canoas (como mucho 112).
        /// </summary>
        public static void TopRowMetrics(float w, bool avatar, out float seedsX, out float seedsW, out float canoesX, out float canoesW)
        {
            float canoesRight = w - 8f - MinTouchDp - 4f;
            if (!avatar)
            {
                seedsX = 16f;
                seedsW = SeedsMaxW;
                canoesW = CanoesMaxW;
                canoesX = Mathf.Min(140f, canoesRight - CanoesMaxW);
                return;
            }

            seedsX = SeedsWithAvatarX;
            canoesW = Mathf.Clamp(canoesRight - (SeedsWithAvatarX + 92f + 4f), CanoesMinW, CanoesMaxW);
            canoesX = canoesRight - canoesW;
            seedsW = Mathf.Min(SeedsMaxW, canoesX - 4f - SeedsWithAvatarX);
        }

        private void BuildAvatar(RectTransform top, ProceduralArt art)
        {
            _avatarButton = UiBuilder.BareButton("AvatarButton", top, () => AvatarTapped?.Invoke());
            Image rim = UiBuilder.Image("Shadow", _avatarButton.transform, art.Disc, new Color(0f, 0f, 0f, 0.22f));
            UiBuilder.Place(rim.rectTransform, 4f, 6f, AvatarDiscDp, AvatarDiscDp);
            _avatarImage = UiBuilder.Image("Portrait", _avatarButton.transform, null, Color.white);
            _avatarImage.preserveAspect = true;
            UiBuilder.Place(_avatarImage.rectTransform, 4f, 4f, AvatarDiscDp, AvatarDiscDp);
            _avatarDot = UiBuilder.Rect("NewDot", _avatarButton.transform);
            Image dotRim = UiBuilder.Image("Rim", _avatarDot, art.Disc, Palette.Cream);
            UiBuilder.Stretch(dotRim.rectTransform);
            Image dot = UiBuilder.Image("Dot", _avatarDot, art.Disc, Palette.Heliconia);
            UiBuilder.Stretch(dot.rectTransform);
            dot.rectTransform.offsetMin = new Vector2(2f, 2f);
            dot.rectTransform.offsetMax = new Vector2(-2f, -2f);
            UiBuilder.Place(_avatarDot, 4f + AvatarDiscDp - MapTabDotDp + 3f, 1f, MapTabDotDp, MapTabDotDp);
            A11yLabels.Set(_avatarButton, () => _avatarA11y);
            _avatarDot.gameObject.SetActive(false);
            _avatarButton.gameObject.SetActive(false);
        }

        private void PlaceAvatar(float topY)
        {
            UiBuilder.Place(AvatarRect, AvatarLeftDp, topY, MinTouchDp, MinTouchDp);
        }
    }
}
