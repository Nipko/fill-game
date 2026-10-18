#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Su cuia chica, la de la Tinaja (M13; D-019; 07 §4.3; propuesta de la Tinaja, b.2 «Aspecto»; encargo de arte, 3.1; tarea
    /// TJ2) [P]: una Cuia chica de vidrio, que dibuja la vista con su contenido a la vista, colgada del jirau junto a la tinaja,
    /// como las cuias pitingas que se cuelgan con fibra por dos agujeritos. El overlay pone el poste del jirau con su travesaño y
    /// el gancho (en la ranura, detrás del cuerpo: no viaja) y el asa pequeña de fibra de la cuia (en el cuerpo: es suya, para que
    /// se lea como el cazo de la casa). El jirau de debajo lo dibuja <see cref="TinajaOverlay"/>, que es permanente.
    /// </summary>
    /// <remarks>
    /// <para><b>Vetos</b> (07 §4.3; b.2): nada de cordón, liana ni nudo entre la tinaja y su cuia (es la imagen de la Liana, M3):
    /// el poste va del lado contrario a la tinaja y el asa solo va de la cuia al gancho.</para>
    /// <para><b>La suelta</b> (b.2 regla 8; R10 f): cuando la tinaja se vacía, su cuia pasa a ser una Cuia chica normal
    /// (<c>Cuia.WithoutMechanic</c>) y todo el overlay se oculta; Marea la vuelve a colgar. La animación de la suelta (su cuia
    /// baja del jirau) es de la tarea TJ3.</para>
    /// <para><b>Arte</b> (<see cref="MapArtRule"/>): poste, gancho y asa son arte provisional y solo salen con
    /// <see cref="MapArtRule.ShowProvisional"/> (Editor y desarrollo) hasta los PNG del artista. Nada cambia al seleccionar
    /// (D-002): al elevarse, el asa sube con su cuia y se separa del gancho, como al descolgarla. Sin asignaciones en
    /// <see cref="Show"/> ni en <see cref="OnBoardLayout"/>.</para>
    /// </remarks>
    public sealed class CucharonOverlay : CuiaOverlay
    {
        private Image _post = null!;
        private Image _asa = null!;
        private float _u = 24f;
        private float _side = 1f;
        private bool _sideSure;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Cucharon;

        /// <summary>El poste del jirau con su gancho está a la vista.</summary>
        public bool PostVisible => _post.gameObject.activeSelf;

        /// <summary>El asa de fibra de la cuia está a la vista.</summary>
        public bool AsaVisible => _asa.gameObject.activeSelf;

        /// <summary>Lado del poste respecto al eje de la cuia: 1 a la derecha, −1 a la izquierda (el contrario a la tinaja).</summary>
        public float PostSide => _side;

        /// <summary>Rectángulo del poste (u desde el centro del pie), ya reflejado a su lado.</summary>
        public Rect PostRectU
        {
            get
            {
                Rect r = ProceduralArt.TinajaPostRectU;
                return _side > 0f ? r : new Rect(-r.xMax, r.yMin, r.width, r.height);
            }
        }

        /// <summary>Rectángulo del asa (u desde el centro del pie).</summary>
        public Rect AsaRectU => ProceduralArt.TinajaAsaRectU;

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Cucharon;
            Visible = on;
            bool art = on && MapArtRule.ShowProvisional;
            if (art)
            {
                FindSide();
            }

            OverlayParts.SetVisible(_post, art);
            OverlayParts.SetVisible(_asa, art);
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            Place();
        }

        /// <inheritdoc />
        public override void OnBoardLayout()
        {
            if (Visible)
            {
                FindSide();
            }
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(ProceduralArt.JirauWood); // el poste y el travesaño (el gancho y el asa, de fibra)
            into.Add(ProceduralArt.JirauShade);
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            _post = OverlayParts.NewImage("CucharonPost", Host.SlotBack, Host.Art.TinajaPost, Color.white);
            _post.rectTransform.pivot = Host.Art.TinajaPost.pivot / Host.Art.TinajaPost.rect.size;
            _asa = OverlayParts.NewImage("CucharonAsa", Host.BodyFront, Host.Art.TinajaAsa, Color.white);
        }

        // El poste, del lado contrario a la tinaja (la cuia de tipo Tinaja de al lado); sin verla, a la derecha [P].
        private void FindSide()
        {
            if (_sideSure)
            {
                return;
            }

            ICuiaBoard? board = Host.Board;
            if (board == null)
            {
                return;
            }

            int i = Host.Index;
            float side = _side;
            if (i + 1 < board.Count && board.ShownCuia(i + 1).Mechanic == MechanicType.Tinaja)
            {
                side = -1f;
                _sideSure = true;
            }
            else if (i - 1 >= 0 && board.ShownCuia(i - 1).Mechanic == MechanicType.Tinaja)
            {
                side = 1f;
                _sideSure = true;
            }

            if (side != _side)
            {
                _side = side;
                Place();
            }
        }

        private void Place()
        {
            // El poste lleva el pivote en el eje y el pie de la cuia: se coloca en (0, 0) y se refleja en x a su lado.
            Rect box = ProceduralArt.TinajaPostRectU;
            RectTransform post = _post.rectTransform;
            var size = new Vector2(box.width * _u, box.height * _u);
            if (post.anchoredPosition != Vector2.zero)
            {
                post.anchoredPosition = Vector2.zero;
            }

            if (post.sizeDelta != size)
            {
                post.sizeDelta = size;
            }

            var scale = new Vector3(_side, 1f, 1f);
            if (post.localScale != scale)
            {
                post.localScale = scale;
            }

            Rect asa = ProceduralArt.TinajaAsaRectU;
            OverlayParts.Place(_asa, asa.center.x, asa.center.y, asa.width, asa.height, _u);
        }
    }
}
