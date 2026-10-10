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
    /// la fruta llave grande en el centro, algo girada, sobre un medallón pulido apaisado (<see cref="ProceduralArt.FruitFlight"/>;
    /// ni el medallón es un disco ni la fruta va derecha, para que el conjunto no se lea como un ojo). Su silueta es
    /// alargada y con puntas para no confundirse con la hoja redonda de la Hoja escudo (02 §9.1; glosario §6.3). Es un estado,
    /// no un resaltado: no se ilumina al seleccionar (D-002), no lleva dedo guía (D-002, P5) y al tocarla como origen solo se
    /// sacude la cuia (<c>CuiaView.Shake</c>).
    /// </summary>
    /// <remarks>
    /// <para><b>Desbloqueo</b> (paso Open de la etapa (b), <see cref="OpenStepMs"/> × <c>ctx.SpeedFactor</c>; b.2: «la hoja se
    /// abre y cae girando al agua», 400-600 ms; ficha de Magic Sort §3): la hoja se levanta y se abre (el pliegue se aplana)
    /// mientras la fruta llave salta a 1,3× y se queda opaca un instante (la llave encaja, §3.3); después suelta sus destellos
    /// y se deshace en luz (<c>ApplyKey</c>). La hoja, ya plana, cae girando dentro de la cuia hasta la superficie del
    /// agua recién vertida (la de <c>ctx.After</c>), salpica justo antes del contacto (<see cref="IBoardFx.Splash"/>, más clara
    /// que el agua) y, mientras se hunde y encoge, se aclara hacia <see cref="Palette.Glint"/> y se desvanece del todo en
    /// <see cref="GoneAt"/>, antes de que acabe el paso: nunca se mezcla con el tinte (D-003). Mientras anima (<see cref="IsAnimating"/>), la vista no llama a <see cref="Show"/>; la cola cierra el paso con
    /// <see cref="SnapStep"/> y <c>ShowContent(ctx.After)</c>, que la oculta.</para>
    /// <para><b>Marea</b> (02 §14; 04 §2.1): <see cref="SnapStep"/> deja la pose de reposo, así que el <see cref="Show"/> con la
    /// Hoja devuelta vuelve a pintar la hoja cerrada.</para>
    /// <para>Sin asignaciones en <see cref="Tick"/>, <see cref="PlayStep"/> ni <see cref="Show"/> (08 §5.4): las piezas se
    /// crean en <see cref="OnAttach"/> con sus sprites (perezosos, generados al unirse; los de la fruta llave y su luz, la
    /// primera vez que <see cref="Show"/> enseña esa llave, nunca en el paso) y solo se escriben valores que cambian. Las
    /// partículas salen del pool común de <see cref="FxLayer"/> (tope de 44); con Reducir movimiento el factor
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

        /// <summary>La hoja toca el agua y salpica (fracción del paso); después se aclara y se desvanece.</summary>
        public const float SplashAt = 0.8f;

        /// <summary>
        /// La hoja ya se desvaneció del todo (fracción del paso) [P]: acaba antes que el paso y limpia, aclarándose hacia
        /// <see cref="Palette.Glint"/> en vez de mezclarse con el tinte (D-003: luz de juego).
        /// </summary>
        public const float GoneAt = 0.88f;

        /// <summary>Escala de la hoja al desaparecer en el agua (desde <see cref="WaterScale"/>).</summary>
        public const float GoneScale = 0.30f;

        /// <summary>Parte del desvanecimiento en la que la hoja ya se aclaró del todo hacia la luz.</summary>
        private const float GlowRampEnd = 0.6f;

        /// <summary>
        /// La salpicadura sale un poco antes del contacto (fracción del paso; q = 0,95 de la caída, t ≈ 0,77): cuando la hoja se
        /// aclara, las gotas ya se han abierto y no quedan juntas en el centro de la luz como una pupila.
        /// </summary>
        public const float SplashFxAt = OpenEnd + (0.95f * (SplashAt - OpenEnd));

        /// <summary>Aclarado de la salpicadura hacia el blanco: sobre el agua de su mismo tinte se lee.</summary>
        public const float SplashLighten = 0.35f;

        /// <summary>
        /// Fin del salto de la fruta llave hasta <see cref="FruitPopScale"/> (fracción del paso): la llave encaja (ficha de Magic
        /// Sort §3.3: el icono confirma antes de que llegue la luz).
        /// </summary>
        public const float FruitPopEnd = 0.10f;

        /// <summary>Escala de la fruta llave al saltar.</summary>
        public const float FruitPopScale = 1.3f;

        /// <summary>Hasta aquí la fruta llave se queda opaca, ya grande (fracción del paso); después se deshace en luz.</summary>
        public const float FruitHoldEnd = 0.18f;

        /// <summary>Los destellos de la llave (fracción del paso): cuando la fruta empieza a deshacerse en luz.</summary>
        public const float SparkleAt = FruitHoldEnd;

        /// <summary>Destellos de la llave (en <see cref="Palette.Glint"/>).</summary>
        public const int KeySparkleCount = 4;

        /// <summary>
        /// Fin del fundido de la fruta llave (fracción del paso): se desvanece aclarándose hacia <see cref="Palette.Glint"/>, con
        /// la misma luz encima que la hoja. El medallón se va antes, en las dos terceras partes del fundido.
        /// </summary>
        public const float FruitFadeEnd = 0.36f;

        /// <summary>Giro de la fruta llave sobre el medallón (grados; negativo: hacia la derecha) [P]: rompe la simetría de ojo.</summary>
        public const float FruitTiltDeg = -20f;

        // Parte del fundido de la llave en la que se va el medallón (antes que la fruta, que acaba en luz).
        private const float BadgeFadePart = 2f / 3f;

        // Sprite del medallón: la forma más su margen por lado (u).
        private const float BadgeSpriteWidthU = ProceduralArt.HojaBadgeWidthU + (2f * ProceduralArt.HojaBadgePadU);
        private const float BadgeSpriteHeightU = ProceduralArt.HojaBadgeHeightU + (2f * ProceduralArt.HojaBadgePadU);

        // Si la cola nunca cerrara el paso (no debería pasar: siempre llama a SnapStep), el overlay lo cierra solo pasado este
        // margen tras su duración y se repinta desde el tablero.
        private const float StuckGuardMs = 1000f;

        private Image _leaf = null!;
        private Image _fall = null!;
        private Image _glow = null!;
        private Image _badge = null!;
        private Image _fruit = null!;
        private Image _fruitGlow = null!;
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

        /// <summary>La hoja abierta que cae (o la luz en que se desvanece) está a la vista.</summary>
        public bool FallingLeafVisible => (_fall.enabled && _fall.color.a > 0.001f) || (_glow.enabled && _glow.color.a > 0.001f);

        /// <summary>La fruta llave está a la vista.</summary>
        public bool FruitVisible => _fruit.gameObject.activeSelf && _fruit.color.a > 0.001f;

        /// <summary>Opacidad de la fruta llave (0 oculta).</summary>
        public float FruitAlpha => _fruit.gameObject.activeSelf ? _fruit.color.a : 0f;

        /// <summary>Escala de la fruta llave (1 en reposo; <see cref="FruitPopScale"/> al encajar la llave).</summary>
        public float FruitScale => Mathf.Abs(_fruit.rectTransform.localScale.y);

        /// <summary>Giro de la fruta llave (grados, −180..180).</summary>
        public float FruitAngleDeg => Mathf.DeltaAngle(0f, _fruit.rectTransform.localEulerAngles.z);

        /// <summary>Opacidad de la luz que aclara la fruta llave al deshacerse (0 oculta).</summary>
        public float FruitGlowAlpha => _fruitGlow.enabled ? _fruitGlow.color.a : 0f;

        /// <summary>Lo que ha bajado la hoja que cae respecto de la hoja cerrada (u; positivo hacia abajo; 0 sin caída).</summary>
        public float FallDropU => _fall.enabled ? ClosedCenterU - (_fall.rectTransform.anchoredPosition.y / _u) : 0f;

        /// <summary>Giro de la hoja que cae (grados, −180..180; 0 sin caída).</summary>
        public float FallAngleDeg => _fall.enabled ? Mathf.DeltaAngle(0f, _fall.rectTransform.localEulerAngles.z) : 0f;

        /// <summary>Opacidad de la hoja que cae.</summary>
        public float FallAlpha => _fall.enabled ? _fall.color.a : 0f;

        /// <summary>Escala de la hoja que cae (1 al abrirse; <see cref="WaterScale"/> al llegar al agua; <see cref="GoneScale"/> al desaparecer).</summary>
        public float FallScale => _fall.enabled ? Mathf.Abs(_fall.rectTransform.localScale.y) : 0f;

        /// <summary>Opacidad de la luz que aclara la hoja al desvanecerse en el agua (0 oculta).</summary>
        public float GlowAlpha => _glow.enabled ? _glow.color.a : 0f;

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
                SetKey(cuia.MechanicKey);
            }

            if (_posed)
            {
                PlaceClosed();
            }

            OverlayParts.SetVisible(_leaf, on);
            OverlayParts.SetVisible(_badge, on && _key > 0);
            OverlayParts.SetVisible(_fruit, on && _key > 0);
            ShowFall(false);
            SetEnabled(_fruitGlow, false);
            if (on)
            {
                // La hoja que cae y las luces se muestran a mitad del paso: sin asignar (08 §5.4).
                EnsurePooled();
            }
        }

        /// <inheritdoc />
        public override void OnBoardLayout()
        {
            // Si al recolocar el tablero UGUI desregistró la hoja que cae o las luces (ocultas), se vuelven a registrar ahora y
            // no en el paso.
            EnsurePooled();
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
            if (ctx.Before.Mechanic == MechanicType.Hoja)
            {
                SetKey(ctx.Before.MechanicKey);
            }

            Cuia after = ctx.After;
            int count = after.Count;
            _surfaceU = Mathf.Min(CuiaGeometry.CavityBottomU + Mathf.Max(1, count), CuiaGeometry.CavityTopU(Host.Capacity));
            int top = count - 1;
            _splashColor = top < 0 ? Palette.Glint : ColorMath.Lighten(after.IsHiddenAt(top) ? Palette.Murky : Palette.Tint(after.TopColor), SplashLighten);
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
            SetEnabled(_fruitGlow, false);
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

            // Las luces en que se desvanecen la hoja y la fruta llave (Glint, un instante y ya casi transparentes) son destellos
            // del efecto, como los de FxLayer, no superficies que rodeen la cuia: no entran en la regla 1 de la guía §2.4.
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            _slot = (RectTransform)Host.SlotBack.parent;
            _leaf = OverlayParts.NewImage("HojaLeaf", Host.BodyFront, Host.Art.HojaPalm, Color.white);

            // La hoja que cae se muestra y se oculta con Image.enabled (su objeto queda activo y registrado en el Canvas, como
            // las partículas de FxLayer): activarla por primera vez a mitad del paso haría crecer el registro de UGUI. Va
            // detrás de la llave: al empezar a caer sale de debajo de la fruta, que se queda flotando y se deshace en luz.
            _fall = NewPooled("HojaFall", Host.Art.HojaFlat, Color.white);

            // Encima, la misma hoja en luz (silueta clara teñida de Glint), que la aclara mientras se desvanece en el agua.
            _glow = NewPooled("HojaGlow", Host.Art.HojaGlow, Palette.Glint);

            _badge = OverlayParts.NewImage("HojaBadge", Host.BodyFront, Host.Art.HojaBadge, ProceduralArt.HojaBadgeColor);
            _fruit = OverlayParts.NewImage("HojaFruit", Host.BodyFront, null, Color.white);

            // Y sobre la fruta, su silueta en luz (la pone SetKey con la llave).
            _fruitGlow = NewPooled("HojaFruitGlow", null, Palette.Glint);
        }

        // Imagen que se muestra y se oculta con Image.enabled: objeto activo y registrada en el Canvas desde el principio.
        private Image NewPooled(string name, Sprite? sprite, Color color)
        {
            Image img = OverlayParts.NewImage(name, Host.BodyFront, sprite, color);
            img.enabled = false;
            img.gameObject.SetActive(true);
            GraphicPool.EnsureRegistered(img);
            return img;
        }

        private void EnsurePooled()
        {
            GraphicPool.EnsureRegistered(_fall);
            GraphicPool.EnsureRegistered(_glow);
            GraphicPool.EnsureRegistered(_fruitGlow);
        }

        // Fruta llave que se muestra y su silueta en luz (sprites perezosos: se generan aquí, al mostrar la llave, nunca en Tick).
        private void SetKey(int key)
        {
            if (key == _key)
            {
                return;
            }

            _key = key;
            _fruit.sprite = key > 0 ? Host.Art.FruitFlight(key) : null;
            _fruitGlow.sprite = key > 0 ? Host.Art.HojaFruitGlow(key) : null;
        }

        // Pose de reposo de las seis piezas (sin escala y opacas, la fruta girada; las luces, transparentes).
        private void PlaceClosed()
        {
            _posed = false;
            Pose(_leaf, 0f, ClosedCenterU, LeafWidthU, LeafHeightU, 0f, 1f, 1f, 1f);
            Pose(_badge, 0f, FruitCenterU, BadgeSpriteWidthU, BadgeSpriteHeightU, 0f, 1f, 1f, 1f);
            Pose(_fruit, 0f, FruitCenterU, FruitU, FruitU, FruitTiltDeg, 1f, 1f, 1f);
            Pose(_fruitGlow, 0f, FruitCenterU, FruitU, FruitU, FruitTiltDeg, 1f, 1f, 0f);
            Pose(_fall, 0f, ClosedCenterU + LiftU, ProceduralArt.HojaFlatWidthU, ProceduralArt.HojaFlatHeightU, 0f, 1f, 1f, 1f);
            Pose(_glow, 0f, ClosedCenterU + LiftU, ProceduralArt.HojaFlatWidthU, ProceduralArt.HojaFlatHeightU, 0f, 1f, 1f, 0f);
        }

        // Fotograma t (0..1) del desbloqueo; los efectos salen una sola vez al cruzar su instante.
        private void Apply(float t)
        {
            _posed = true;
            float closedY = ClosedCenterU;
            ApplyKey(t);
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
            if (!_splashed && t >= SplashFxAt)
            {
                // Justo antes del contacto, en un tono más claro que el agua: las gotas ya se abren cuando la hoja se aclara.
                _splashed = true;
                _fx?.Splash(FxPoint(_surfaceU), _splashColor, _u);
            }

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
                ShowFall(true);
                SetEnabled(_glow, false);
                Pose(_fall, x, y, ProceduralArt.HojaFlatWidthU, ProceduralArt.HojaFlatHeightU, angle, scale * flip, scale, 1f);
                return;
            }

            // Toca el agua: se hunde un poco, encoge (de WaterScale a GoneScale) y se desvanece con 1 − SmoothStep, sin
            // mezclarse con el tinte: mientras se desvanece se aclara hacia Glint. La hoja (opacidad leafA) y su luz encima
            // (opacidad glowA) pintan juntas Lerp(hoja, Glint, light) con la opacidad total opacity. En GoneAt ya no queda nada.
            float s = Mathf.Clamp01((t - SplashAt) / (GoneAt - SplashAt));
            float opacity = 1f - Mathf.SmoothStep(0f, 1f, s);
            if (opacity <= 0.001f)
            {
                ShowFall(false);
                return;
            }

            float light = Ease.OutCubic(s / GlowRampEnd);
            float glowA = opacity * light;
            float leafA = (opacity - glowA) / Mathf.Max(1e-4f, 1f - glowA);
            float sinkY = waterY - (0.12f * s);
            float sinkAngle = _side * (330f + (14f * s));
            float sinkScale = Mathf.Lerp(WaterScale, GoneScale, s);
            ShowFall(true);
            SetEnabled(_glow, true);
            Pose(_fall, 0f, sinkY, ProceduralArt.HojaFlatWidthU, ProceduralArt.HojaFlatHeightU, sinkAngle, sinkScale, sinkScale, leafA);
            Pose(_glow, 0f, sinkY, ProceduralArt.HojaFlatWidthU, ProceduralArt.HojaFlatHeightU, sinkAngle, sinkScale, sinkScale, glowA);
        }

        // La llave (b.2 M1; ficha de Magic Sort §3.3: el icono confirma primero y después llega la luz): la fruta salta a
        // FruitPopScale y sube con la hoja que se abre, se queda opaca hasta FruitHoldEnd (la llave encaja) y entonces suelta sus
        // destellos y se deshace en luz hasta FruitFadeEnd. Como la hoja en el agua, la fruta (opacidad fruitA) y su silueta en
        // luz encima (glowA) pintan juntas Lerp(fruta, Glint, light) con la opacidad total opacity. El medallón la acompaña en
        // el salto (a medias) y se va antes. Cuando la hoja empieza a caer, la llave se queda flotando sobre la boca.
        private void ApplyKey(float t)
        {
            if (_key <= 0)
            {
                OverlayParts.SetVisible(_badge, false);
                OverlayParts.SetVisible(_fruit, false);
                SetEnabled(_fruitGlow, false);
                return;
            }

            float scale = 1f + ((FruitPopScale - 1f) * Ease.OutCubic(t / FruitPopEnd));
            float fade = Mathf.Clamp01((t - FruitHoldEnd) / (FruitFadeEnd - FruitHoldEnd));
            float opacity = 1f - Mathf.SmoothStep(0f, 1f, fade);
            float glowA = opacity * Ease.OutCubic(fade / GlowRampEnd);
            float fruitA = (opacity - glowA) / Mathf.Max(1e-4f, 1f - glowA);
            float badgeA = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fade / BadgeFadePart));
            float badgeScale = 1f + (0.5f * (scale - 1f));
            float y = FruitCenterU + (LiftU * Ease.OutCubic(t / OpenEnd)) + (0.1f * fade);
            Pose(_badge, 0f, y, BadgeSpriteWidthU, BadgeSpriteHeightU, 0f, badgeScale, badgeScale, badgeA);
            Pose(_fruit, 0f, y, FruitU, FruitU, FruitTiltDeg, scale, scale, fruitA);
            Pose(_fruitGlow, 0f, y, FruitU, FruitU, FruitTiltDeg, scale, scale, glowA);
            OverlayParts.SetVisible(_badge, badgeA > 0.001f);
            OverlayParts.SetVisible(_fruit, opacity > 0.001f);
            SetEnabled(_fruitGlow, glowA > 0.001f);
            if (!_sparkled && t >= SparkleAt)
            {
                _sparkled = true;
                _fx?.Sparkles(FxPoint(y), Palette.Glint, KeySparkleCount, _u);
            }
        }

        // Muestra u oculta la hoja que cae; al ocultarla, también su luz.
        private void ShowFall(bool on)
        {
            SetEnabled(_fall, on);
            if (!on)
            {
                SetEnabled(_glow, false);
            }
        }

        // La hoja que cae y su luz se muestran y se ocultan con Image.enabled: siguen registradas en el Canvas (sin asignar).
        private static void SetEnabled(Image img, bool on)
        {
            if (img.enabled != on)
            {
                img.enabled = on;
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
