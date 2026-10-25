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
    /// como las cuias pitingas que se cuelgan con fibra por dos agujeritos. El overlay pone un poste bajo del jirau con su gancho
    /// corto (en la ranura, detrás del cuerpo: no viaja) y el asa pequeña de fibra de la cuia, un arco de hombro a hombro que se
    /// apoya en el gancho (en el cuerpo: es suya, para que se lea como el cazo de la casa). Sin travesaño ni cuerda al cuello: nada
    /// pasa por encima del cuello de su cuia ni toca su borde de fibra (la silueta poste + travesaño + cuerda se leía como una
    /// horca, 07 §8). El jirau de debajo lo dibuja <see cref="TinajaOverlay"/>, que es permanente. Su cuia
    /// colgada, el poste y el asa son contenido cultural: pendiente del asesor cultural (D-019; 07 §4.3; encargo de arte 3.4).
    /// </summary>
    /// <remarks>
    /// <para><b>Vetos</b> (07 §4.3; b.2): nada de cordón, liana ni nudo entre la tinaja y su cuia (es la imagen de la Liana, M3):
    /// el poste va del lado contrario a la tinaja y el asa es de su cuia (de un hombro al otro, apoyada en el gancho).</para>
    /// <para><b>La suelta</b> (b.2 regla 8; R10 f): cuando la tinaja se vacía, su cuia pasa a ser una Cuia chica normal
    /// (<c>Cuia.WithoutMechanic</c>) y todo el overlay se oculta; Marea la vuelve a colgar. La animación de la suelta (su cuia
    /// baja del jirau) es su paso <see cref="MechanicStepKind.Unhook"/> (TJ3), a la vez que la tinaja se tumba.</para>
    /// <para><b>Arte</b> (<see cref="MapArtRule"/>): poste, gancho y asa son arte por código y salen con
    /// <see cref="MapArtRule.ShowProvisional"/>, en todos los builds, release incluido (D-052). Nada cambia al seleccionar
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
        private bool _stepping;
        private float _stepT;
        private float _stepSpeed = 1f;
        private bool _stepReduce;

        /// <summary>Suelta (TJ3; b.2 «Animación y sonido»): su cuia se descuelga del jirau y se asienta en su ranura en 300 ms.</summary>
        public const float UnhookMs = 300f;

        /// <summary>Suelta: lo que sube el asa al salir del gancho antes de apagarse (u) [P].</summary>
        public const float UnhookLiftU = 0.30f;

        /// <summary>
        /// Suelta: su cuia se descuelga (sube 3 dp con el asa, que sale del gancho, en el primer 30 % del paso), baja a su ranura
        /// (hasta el 80 %) y se asienta con un rebote de <see cref="UnhookSettleDp"/> por debajo del pie (dp) [P] (revisión de TJ3;
        /// 02 §4.1 y b.2: «su cuia se descuelga del jirau y se asienta en su ranura»).
        /// </summary>
        public const float UnhookRiseDp = 3f;

        /// <summary>Suelta: lo que su cuia se hunde al asentarse antes de quedar en su pie (dp) [P].</summary>
        public const float UnhookSettleDp = 1f;

        /// <summary>Desplazamiento vertical de su cuia en la suelta en el instante <paramref name="q"/> (0..1 del paso; dp).</summary>
        public static float UnhookNudgeDp(float q)
        {
            if (q <= 0f || q >= 1f)
            {
                return 0f;
            }

            if (q < 0.3f)
            {
                return UnhookRiseDp * Ease.Out(q / 0.3f);
            }

            if (q < 0.8f)
            {
                return Mathf.Lerp(UnhookRiseDp, -UnhookSettleDp, Ease.In((q - 0.3f) / 0.5f));
            }

            return -UnhookSettleDp * (1f - Ease.Out((q - 0.8f) / 0.2f));
        }

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Cucharon;

        /// <inheritdoc />
        public override bool IsAnimating => _stepping;

        /// <summary>Opacidad del asa (1 en reposo; se apaga en la suelta, TJ3).</summary>
        public float AsaAlpha => _asa.color.a;

        /// <summary>Opacidad del poste (1 en reposo; se apaga en la suelta, TJ3).</summary>
        public float PostAlpha => _post.color.a;

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            return kind == MechanicStepKind.Unhook ? UnhookMs : 0f;
        }

        /// <summary>
        /// La suelta (<see cref="MechanicStepKind.Unhook"/>, etapa f de R10; TJ3): el asa sube y sale del gancho y, con el poste,
        /// se apaga mientras su cuia se descuelga (sube un poco), baja y se asienta en su ranura como una Cuia chica normal
        /// (<see cref="UnhookNudgeDp"/>, con <see cref="CuiaOverlayHost.BodyNudgeDp"/>). Sin el arte provisional no hay asa ni
        /// poste: 0 (salto). Con Reducir movimiento, solo el fundido. Sin asignaciones.
        /// </summary>
        public override float PlayStep(in MechanicStepContext ctx)
        {
            if (ctx.Kind != MechanicStepKind.Unhook || !MapArtRule.ShowProvisional || !_asa.gameObject.activeSelf)
            {
                return 0f;
            }

            _stepping = true;
            _stepT = 0f;
            _stepSpeed = Mathf.Max(0.01f, ctx.SpeedFactor);
            _stepReduce = ctx.ReduceMotion;
            Apply();
            return UnhookMs * _stepSpeed;
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            if (!_stepping)
            {
                return;
            }

            _stepping = false;
            Host.BodyNudgeDp = 0f;
            OverlayParts.SetColor(_asa, Color.white);
            OverlayParts.SetColor(_post, Color.white);
            Place();
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (_stepping)
            {
                _stepT += dtMs;
                Apply();
            }
        }

        // Fotograma de la suelta: el asa sube (ease-out) y se apaga en la segunda mitad; el poste, a la vez; y su cuia se descuelga,
        // baja a su ranura y se asienta (UnhookNudgeDp; con Reducir movimiento, quieta).
        private void Apply()
        {
            float q = Mathf.Clamp01(_stepT / (UnhookMs * _stepSpeed));
            Host.BodyNudgeDp = _stepReduce ? 0f : UnhookNudgeDp(q);
            float fade = 1f - Mathf.Clamp01((q - 0.4f) / 0.6f);
            float lift = _stepReduce ? 0f : Ease.Out(Mathf.Clamp01(q / 0.6f)) * UnhookLiftU;
            Rect asa = ProceduralArt.TinajaAsaRectU;
            OverlayParts.Place(_asa, asa.center.x, asa.center.y + lift, asa.width, asa.height, _u);
            OverlayParts.SetColor(_asa, new Color(1f, 1f, 1f, fade));
            OverlayParts.SetColor(_post, new Color(1f, 1f, 1f, fade));
        }

        /// <summary>El poste bajo del jirau con su gancho está a la vista.</summary>
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
            into.Add(ProceduralArt.JirauWood); // el poste y su gancho (el asa y la atadura, de fibra)
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
