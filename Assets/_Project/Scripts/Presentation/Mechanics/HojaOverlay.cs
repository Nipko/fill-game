#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M1 Hoja (02 §9.1 M1; propuesta de D-007 b.2 M1) [P]: una hoja de palma alargada y plegada, tendida sobre la boca, con
    /// la fruta llave grande en el centro sobre un medallón pulido (<see cref="ProceduralArt.FruitFlight"/>). Su silueta es
    /// alargada y con puntas para no confundirse con la hoja redonda de la Hoja escudo (02 §9.1; glosario §6.3). Es un estado,
    /// no un resaltado: no se ilumina al seleccionar (D-002), no lleva dedo guía (D-002, P5) y al tocarla como origen solo se
    /// sacude la cuia (<c>CuiaView.Shake</c>).
    /// </summary>
    /// <remarks>
    /// <para><b>Desbloqueo</b> (paso Open de la etapa (b), <see cref="OpenStepMs"/> × <c>ctx.SpeedFactor</c>; b.2: «la hoja se
    /// abre y cae girando al agua», 400-600 ms; ficha de Magic Sort §3): la hoja se levanta y se abre (el pliegue se aplana) y la
    /// fruta llave se disuelve con unos destellos de su color; la hoja, ya plana, cae girando dentro de la cuia hasta la
    /// superficie del agua recién vertida (la de <c>ctx.After</c>), salpica (<see cref="IBoardFx.Splash"/>) y se desvanece.
    /// Mientras anima (<see cref="IsAnimating"/>), la vista no llama a <see cref="Show"/>; la cola cierra el paso con
    /// <see cref="SnapStep"/> y <c>ShowContent(ctx.After)</c>, que la oculta.</para>
    /// <para><b>Marea</b> (02 §14; 04 §2.1): <see cref="SnapStep"/> deja la pose de reposo, así que el <see cref="Show"/> con la
    /// Hoja devuelta vuelve a pintar la hoja cerrada.</para>
    /// <para>Sin asignaciones en <see cref="Tick"/>, <see cref="PlayStep"/> ni <see cref="Show"/> (08 §5.4): las piezas se
    /// crean en <see cref="OnAttach"/> con sus sprites (perezosos, generados al unirse) y solo se escriben valores que
    /// cambian. Las partículas salen del pool común de <see cref="FxLayer"/> (tope de 44); con Reducir movimiento el factor
    /// de velocidad ya llega a la mitad y el pool no emite.</para>
    /// </remarks>
    public sealed class HojaOverlay : CuiaOverlay
    {
        /// <summary>Duración nominal del desbloqueo (ms) [P]: propuesta de D-007 b.2 M1 (400-600 ms).</summary>
        public const float OpenStepMs = 500f;

        /// <summary>Ancho de la hoja cerrada (u).</summary>
        public const float LeafWidthU = ProceduralArt.HojaPalmWidthU;

        /// <summary>Alto de la hoja cerrada (u).</summary>
        public const float LeafHeightU = ProceduralArt.HojaPalmHeightU;

        /// <summary>Lado de la fruta llave (u).</summary>
        public const float FruitU = 0.95f;

        /// <summary>
        /// Centro de la hoja cerrada (u por encima de lo alto de la cuia): así el envés baja hasta 0,22 u bajo lo alto y tapa el
        /// borde de fibra, como una tapa.
        /// </summary>
        public const float LeafAboveLipU = 0.06f;

        /// <summary>Lo que sube la hoja al abrirse (u).</summary>
        public const float LiftU = 0.3f;

        /// <summary>Escala de la hoja al llegar al agua: cabe en la cavidad de 1,6 u.</summary>
        public const float WaterScale = 0.48f;

        /// <summary>Fin de la apertura (fracción del paso): después la hoja ya plana cae.</summary>
        public const float OpenEnd = 0.2f;

        /// <summary>La hoja toca el agua y salpica (fracción del paso); después se desvanece.</summary>
        public const float SplashAt = 0.8f;

        /// <summary>La fruta llave se disuelve con sus destellos (fracción del paso).</summary>
        public const float SparkleAt = 0.04f;

        /// <summary>Fin del fundido de la fruta llave y su medallón (fracción del paso).</summary>
        public const float FruitFadeEnd = 0.26f;

        // Si la cola nunca cerrara el paso (no debería pasar: siempre llama a SnapStep), el overlay lo cierra solo pasado este
        // margen tras su duración y se repinta desde el tablero.
        private const float StuckGuardMs = 1000f;

        private Image _leaf = null!;
        private Image _fall = null!;
        private Image _badge = null!;
        private Image _fruit = null!;
        private RectTransform _slot = null!;
        private IBoardFx? _fx;
        private int _key;
        private float _u = 24f;
        private float _heightU = 5.6f;
        private float _elapsed = -1f;
        private float _duration;
        private float _surfaceU;
        private float _side = 1f;
        private Color _splashColor;
        private Color _sparkleColor;
        private bool _sparkled;
        private bool _splashed;
        private bool _posed;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Hoja;

        /// <summary>Fruta llave que muestra (0 si aún ninguna).</summary>
        public int ShownKey => _key;

        /// <summary>Sprite de la fruta llave (null sin llave).</summary>
        public Sprite? FruitSprite => _fruit.sprite;

        /// <inheritdoc />
        public override bool IsAnimating => _elapsed >= 0f;

        /// <summary>Fracción del desbloqueo en curso (0..1), o −1 sin paso.</summary>
        public float StepProgress => _elapsed < 0f ? -1f : Mathf.Clamp01(_elapsed / Mathf.Max(1f, _duration));

        /// <summary>La hoja cerrada (tendida sobre la boca) está a la vista.</summary>
        public bool ClosedLeafVisible => _leaf.gameObject.activeSelf && _leaf.color.a > 0.001f;

        /// <summary>La hoja abierta que cae está a la vista.</summary>
        public bool FallingLeafVisible => _fall.enabled && _fall.color.a > 0.001f;

        /// <summary>La fruta llave está a la vista.</summary>
        public bool FruitVisible => _fruit.gameObject.activeSelf && _fruit.color.a > 0.001f;

        /// <summary>Opacidad de la fruta llave (0 oculta).</summary>
        public float FruitAlpha => _fruit.gameObject.activeSelf ? _fruit.color.a : 0f;

        /// <summary>Lo que ha bajado la hoja que cae respecto de la hoja cerrada (u; positivo hacia abajo; 0 sin caída).</summary>
        public float FallDropU => _fall.enabled ? ClosedCenterU - (_fall.rectTransform.anchoredPosition.y / _u) : 0f;

        /// <summary>Giro de la hoja que cae (grados, −180..180; 0 sin caída).</summary>
        public float FallAngleDeg => _fall.enabled ? Mathf.DeltaAngle(0f, _fall.rectTransform.localEulerAngles.z) : 0f;

        /// <summary>Opacidad de la hoja que cae.</summary>
        public float FallAlpha => _fall.enabled ? _fall.color.a : 0f;

        /// <summary>Escala de la hoja que cae (1 al abrirse; <see cref="WaterScale"/> al llegar al agua).</summary>
        public float FallScale => _fall.enabled ? Mathf.Abs(_fall.rectTransform.localScale.y) : 0f;

        // Centro de la hoja cerrada (u desde el pie).
        private float ClosedCenterU => _heightU + LeafAboveLipU;

        // Centro de la fruta llave (u desde el pie): sobre el centro de la hoja, un poco por encima del nervio.
        private float FruitCenterU => ClosedCenterU + (ProceduralArt.HojaPalmMidribU - (0.5f * LeafHeightU)) + 0.12f;

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Hoja;
            Visible = on;
            if (on)
            {
                int key = cuia.MechanicKey;
                if (key != _key)
                {
                    _key = key;
                    _fruit.sprite = key > 0 ? Host.Art.FruitFlight(key) : null;
                }
            }

            if (_posed)
            {
                PlaceClosed();
            }

            OverlayParts.SetVisible(_leaf, on);
            OverlayParts.SetVisible(_badge, on && _key > 0);
            OverlayParts.SetVisible(_fruit, on && _key > 0);
            ShowFall(false);
            if (on)
            {
                GraphicPool.EnsureRegistered(_fall); // la hoja que cae se muestra a mitad del paso: sin asignar (08 §5.4)
            }
        }

        /// <inheritdoc />
        public override void OnBoardLayout()
        {
            // Si al recolocar el tablero UGUI desregistró la hoja que cae (oculta), se vuelve a registrar ahora y no en el paso.
            GraphicPool.EnsureRegistered(_fall);
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            _heightU = heightU;
            if (_elapsed >= 0f)
            {
                Apply(StepProgress);
            }
            else
            {
                PlaceClosed();
            }
        }

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            return kind == MechanicStepKind.Open ? OpenStepMs : 0f;
        }

        /// <inheritdoc />
        public override float PlayStep(in MechanicStepContext ctx)
        {
            float ms = StepDurationMs(ctx.Kind) * ctx.SpeedFactor;
            if (ms <= 0f)
            {
                return 0f;
            }

            // La fruta y la hoja de antes de la etapa (la cuia ya con el agua dentro, todavía cerrada).
            if (ctx.Before.Mechanic == MechanicType.Hoja && ctx.Before.MechanicKey != _key)
            {
                _key = ctx.Before.MechanicKey;
                _fruit.sprite = _key > 0 ? Host.Art.FruitFlight(_key) : null;
            }

            Cuia after = ctx.After;
            int count = after.Count;
            _surfaceU = Mathf.Min(CuiaGeometry.CavityBottomU + Mathf.Max(1, count), CuiaGeometry.CavityTopU(Host.Capacity));
            int top = count - 1;
            _splashColor = top < 0 ? Palette.Glint : (after.IsHiddenAt(top) ? Palette.Murky : Palette.Tint(after.TopColor));
            _sparkleColor = _key > 0 ? ColorMath.Lighten(Palette.Tint(_key), 0.35f) : Palette.Glint;
            _fx = ctx.Fx ?? Host.Fx;
            _side = (Host.Index & 1) == 0 ? 1f : -1f; // gira hacia un lado u otro según la cuia, sin azar
            _duration = ms;
            _elapsed = 0f;
            _sparkled = false;
            _splashed = false;
            Visible = true;
            Apply(0f);
            return ms;
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (_elapsed < 0f)
            {
                return;
            }

            _elapsed += dtMs;
            if (_elapsed > _duration + StuckGuardMs)
            {
                SnapStep();
                ICuiaBoard? board = Host.Board;
                if (board != null)
                {
                    Show(board.ShownCuia(Host.Index), false);
                }

                return;
            }

            // Pasada la duración se queda en el último fotograma (todo oculto) hasta que la cola llame a SnapStep.
            Apply(Mathf.Clamp01(_elapsed / Mathf.Max(1f, _duration)));
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            if (_elapsed < 0f)
            {
                return;
            }

            _elapsed = -1f;
            PlaceClosed();
            ShowFall(false);
            OverlayParts.SetVisible(_leaf, false);
            OverlayParts.SetVisible(_badge, false);
            OverlayParts.SetVisible(_fruit, false);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(ProceduralArt.HojaFaceLight); // haz en luz
            into.Add(ProceduralArt.HojaFaceDark); // haz en sombra
            into.Add(ProceduralArt.HojaFold); // envés plegado
            into.Add(ProceduralArt.HojaBadgeColor); // medallón de la fruta (la fruta lleva su tinte a propósito)
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            _slot = (RectTransform)Host.SlotBack.parent;
            _leaf = OverlayParts.NewImage("HojaLeaf", Host.BodyFront, Host.Art.HojaPalm, Color.white);
            _badge = OverlayParts.NewImage("HojaBadge", Host.BodyFront, Host.Art.HojaBadge, ProceduralArt.HojaBadgeColor);
            _fruit = OverlayParts.NewImage("HojaFruit", Host.BodyFront, null, Color.white);
            // La hoja que cae se muestra y se oculta con Image.enabled (su objeto queda activo y registrado en el Canvas, como
            // las partículas de FxLayer): activarla por primera vez a mitad del paso haría crecer el registro de UGUI.
            _fall = OverlayParts.NewImage("HojaFall", Host.BodyFront, Host.Art.HojaFlat, Color.white);
            _fall.enabled = false;
            _fall.gameObject.SetActive(true);
            GraphicPool.EnsureRegistered(_fall);
        }

        // Pose de reposo de las cuatro piezas (sin giro ni escala y opacas).
        private void PlaceClosed()
        {
            _posed = false;
            Pose(_leaf, 0f, ClosedCenterU, LeafWidthU, LeafHeightU, 0f, 1f, 1f, 1f);
            Pose(_badge, 0f, FruitCenterU, ProceduralArt.HojaBadgeU, ProceduralArt.HojaBadgeU, 0f, 1f, 1f, 1f);
            Pose(_fruit, 0f, FruitCenterU, FruitU, FruitU, 0f, 1f, 1f, 1f);
            Pose(_fall, 0f, ClosedCenterU + LiftU, ProceduralArt.HojaFlatWidthU, ProceduralArt.HojaFlatHeightU, 0f, 1f, 1f, 1f);
        }

        // Fotograma t (0..1) del desbloqueo; los efectos salen una sola vez al cruzar su instante.
        private void Apply(float t)
        {
            _posed = true;
            float closedY = ClosedCenterU;

            // La fruta llave y su medallón crecen y se disuelven; al empezar, unos destellos de su color.
            float fruitP = Mathf.Clamp01(t / FruitFadeEnd);
            float fruitAlpha = 1f - Mathf.SmoothStep(0f, 1f, fruitP);
            float fruitScale = 1f + (0.3f * Ease.OutCubic(fruitP));
            bool keyShown = _key > 0 && fruitAlpha > 0.001f;
            Pose(_badge, 0f, FruitCenterU + (0.08f * fruitP), ProceduralArt.HojaBadgeU, ProceduralArt.HojaBadgeU, 0f, fruitScale, fruitScale, fruitAlpha);
            Pose(_fruit, 0f, FruitCenterU + (0.08f * fruitP), FruitU, FruitU, 0f, fruitScale, fruitScale, fruitAlpha);
            OverlayParts.SetVisible(_badge, keyShown);
            OverlayParts.SetVisible(_fruit, keyShown);
            if (!_sparkled && t >= SparkleAt)
            {
                _sparkled = true;
                if (_key > 0)
                {
                    _fx?.Sparkles(FxPoint(FruitCenterU), _sparkleColor, 3, _u);
                }
            }

            if (t < OpenEnd)
            {
                // Se abre: sube y el pliegue se aplana (escala vertical) con un leve giro.
                float p = Ease.OutCubic(t / OpenEnd);
                Pose(_leaf, 0f, closedY + (LiftU * p), LeafWidthU, LeafHeightU, _side * 6f * p, 1f + (0.05f * p), 1f - (0.45f * p), 1f);
                OverlayParts.SetVisible(_leaf, true);
                ShowFall(false);
                return;
            }

            OverlayParts.SetVisible(_leaf, false);
            ShowFall(true);
            float startY = closedY + LiftU;
            float waterY = _surfaceU + 0.1f;
            if (t < SplashAt)
            {
                // Cae girando (acelera como con gravedad), se balancea a un lado y encoge hasta caber en la cavidad.
                float q = (t - OpenEnd) / (SplashAt - OpenEnd);
                float y = Mathf.Lerp(startY, waterY, q * q);
                float x = _side * 0.22f * Mathf.Sin(q * 2f * Mathf.PI) * (1f - q);
                float angle = _side * ((6f * (1f - q)) + (330f * q * (0.55f + (0.45f * q))));
                float scale = Mathf.Lerp(1f, WaterScale, q);
                float flip = 0.82f + (0.18f * Mathf.Cos(q * 4f * Mathf.PI)); // se ladea dos veces al girar y llega plana
                Pose(_fall, x, y, ProceduralArt.HojaFlatWidthU, ProceduralArt.HojaFlatHeightU, angle, scale * flip, scale, 1f);
                return;
            }

            if (!_splashed)
            {
                _splashed = true;
                _fx?.Splash(FxPoint(_surfaceU), _splashColor, _u);
            }

            // Toca el agua: se hunde un poco y se desvanece.
            float r = (t - SplashAt) / (1f - SplashAt);
            float sinkAngle = _side * (336f + (24f * r));
            float sinkScale = WaterScale * (1f - (0.25f * r));
            Pose(_fall, 0f, waterY - (0.22f * r), ProceduralArt.HojaFlatWidthU, ProceduralArt.HojaFlatHeightU, sinkAngle, sinkScale, sinkScale, 1f - Ease.Out(r));
        }

        private void ShowFall(bool on)
        {
            if (_fall.enabled != on)
            {
                _fall.enabled = on;
            }
        }

        // Punto (u sobre el pie, en el eje) en el espacio de los efectos: el contenedor del tablero con origen arriba a la
        // izquierda, como CuiaView.FootPosition. El paso Open va con la cuia en reposo en su ranura.
        private Vector2 FxPoint(float yU)
        {
            return _slot.anchoredPosition + new Vector2(0f, yU * _u);
        }

        // Coloca una pieza (centro en u, tamaño en u, giro en grados, escala y opacidad), escribiendo solo lo que cambia.
        private void Pose(Image img, float xU, float yU, float wU, float hU, float angleDeg, float scaleX, float scaleY, float alpha)
        {
            OverlayParts.Place(img, xU, yU, wU, hU, _u);
            RectTransform rt = img.rectTransform;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angleDeg);
            if (rt.localRotation != rotation)
            {
                rt.localRotation = rotation;
            }

            var scale = new Vector3(scaleX, scaleY, 1f);
            if (rt.localScale != scale)
            {
                rt.localScale = scale;
            }

            Color c = img.color;
            if (!Mathf.Approximately(c.a, alpha))
            {
                c.a = alpha;
                img.color = c;
            }
        }
    }
}
