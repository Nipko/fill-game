#nullable enable
using AF.Presentation;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Bocadillo de C9 (02 §3.1 y §12; bloque B9): los nombres de las dosis de una cuia, uno por línea y de arriba abajo, encima de
    /// ella mientras el dedo sigue abajo. Es el bocadillo de vidrio del juego (<see cref="SpeechBubble"/>, 14 sp con la escala de
    /// texto, sin recibir toques), con el pico hacia la boca de la cuia y sin salirse del lienzo. Solo asigna al enseñarlo.
    /// </summary>
    internal sealed class CuiaNamesBubble
    {
        /// <summary>Margen con los bordes del lienzo (dp), el del bocadillo de TiTino del Mapa [P].</summary>
        public const float EdgeDp = 8f;

        private static readonly Vector3[] Corners = new Vector3[4];

        private readonly RectTransform _parent;
        private readonly SpeechBubble _bubble;

        /// <summary>Crea el bocadillo (oculto) bajo <paramref name="parent"/>, que se mide arriba a la izquierda en dp.</summary>
        public CuiaNamesBubble(RectTransform parent, ProceduralArt art)
        {
            _parent = parent;
            _bubble = new SpeechBubble(parent, art, "CuiaNames");
        }

        /// <summary>Está a la vista.</summary>
        public bool Visible => _bubble.Visible;

        /// <summary>Lo que enseña («» oculto).</summary>
        public string Text => _bubble.Text;

        /// <summary>El bocadillo (tests).</summary>
        public RectTransform Rect => _bubble.Rect;

        /// <summary>Enseña <paramref name="names"/> encima de <paramref name="body"/> (el cuerpo de la cuia); con «», lo oculta.</summary>
        public void Show(string names, RectTransform body)
        {
            if (string.IsNullOrEmpty(names) || body == null)
            {
                Hide();
                return;
            }

            _bubble.Show(names, 1f);
            _bubble.Rect.SetAsLastSibling(); // delante de lo demás de su capa
            body.GetWorldCorners(Corners);
            Vector2 topLeft = TopLeftDp(Corners[1]);
            Vector2 topRight = TopLeftDp(Corners[2]);
            float x = (topLeft.x + topRight.x) * 0.5f;
            float y = Mathf.Min(topLeft.y, topRight.y);
            Rect area = _parent.rect;
            _bubble.PlaceAbove(x, y, EdgeDp, area.width - EdgeDp, EdgeDp, area.width - (2f * EdgeDp));
        }

        /// <summary>Lo oculta.</summary>
        public void Hide()
        {
            if (_bubble.Visible)
            {
                _bubble.Hide();
            }
        }

        // Un punto del mundo en dp del padre, con el origen arriba a la izquierda (y hacia abajo), como UiBuilder.Place.
        private Vector2 TopLeftDp(Vector3 world)
        {
            Vector3 local = _parent.InverseTransformPoint(world);
            Rect r = _parent.rect;
            return new Vector2(local.x - r.xMin, r.yMax - local.y);
        }
    }
}
