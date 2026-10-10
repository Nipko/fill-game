#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M6 Lluvia (02 §9.1 M6; propuesta de D-007 b.2 M6; Q18 B) [P]: una nube gris perla con volumen sobre la boca, dentro de
    /// los 32 dp táctiles sobre la cuia (02 §3.3), con una fila de <c>k</c> puntos (llenos los vertidos útiles ya contados:
    /// solo cambian con vertidos útiles) y, colgando de la panza, una gotita del agua del mundo por cada gota pendiente. Son los
    /// mismos campos que el contador del HUD (<c>LevelCounter.Of</c>). Con la cuia llena (la gota esperaría) la nube se carga
    /// de gris tormenta y, si la gota ya tocaba, queda colgando sobre la boca. En vuelo se oculta (la nube no se inclina).
    /// </summary>
    /// <remarks>
    /// <para>Pasos de la etapa (e) (arquitectura del paso 1 §4; <see cref="MechanicStepQueue"/>), cada uno desde
    /// <c>ctx.Before</c> hasta <c>ctx.After</c>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="MechanicStepKind.RainDrop"/> (<see cref="DropStepMs"/> × <c>ctx.SpeedFactor</c>): la nube
    /// se encoge y suelta una gota del agua del mundo (<c>Palette.Tint(MechanicKey)</c>), que cae por la boca y, al tocar la
    /// superficie, forma la dosis de arriba y salpica en la superficie que sube (<c>IBoardFx.Splash</c>; con Reducir
    /// movimiento el pool no emite). La dosis se pinta con <c>ctx.Board.SetDose</c>: <c>ctx.Before</c> no la tiene y
    /// <c>ctx.After</c> sí. Los puntos se llenan todos al empezar (el vertido que completa el ritmo) y vuelven a los de
    /// <c>After</c> al caer la gota. Si la gota sella la cuia, la tapa es el paso Seal de la etapa (c′), después: aquí no se
    /// dibuja.</description></item>
    /// <item><description><see cref="MechanicStepKind.RainEnd"/> (la última gota, un único paso con la caída; misma duración):
    /// como la gota y, mientras cae, la nube sube <see cref="RiseU"/> y se desvanece con sus puntos.</description></item>
    /// <item><description><see cref="MechanicStepKind.RainWait"/> (<see cref="WaitStepMs"/>; la cuia está llena): la nube se
    /// oscurece, la gota asoma, intenta caer, no cabe y queda colgando; los puntos vuelven a <c>k − 1</c>.</description></item>
    /// </list>
    /// <para>Al acabar (o con <see cref="SnapStep"/>) la dosis queda entera y se pinta <c>ShowContent(ctx.After)</c>. Las
    /// duraciones son [P] (la de la gota no está en ningún documento; la revisa el dueño). Sin asignaciones en los pasos ni en
    /// <see cref="CuiaOverlay.Tick"/>; en reposo no se mueve nada (D-002 y 08 §5.4).</para>
    /// </remarks>
    public sealed class LluviaOverlay : CuiaOverlay
    {
        /// <summary>Alto máximo de la nube (u).</summary>
        public const float CloudMaxU = 1f;

        /// <summary>Hueco entre la boca y la panza de la nube (u).</summary>
        public const float CloudGapU = 0.18f;

        /// <summary>Gota (RainDrop) y última gota (RainEnd): 500 ms [P, ≤ 600; no está en ningún documento].</summary>
        public const float DropStepMs = 500f;

        /// <summary>La gota espera con la cuia llena (RainWait): 400 ms [P].</summary>
        public const float WaitStepMs = 400f;

        /// <summary>Lo que sube la nube al irse con la última gota (u) [P].</summary>
        public const float RiseU = 0.7f;

        // Fases de la gota (fracción del paso): se forma bajo la nube, cae y, al tocar la superficie, llena la dosis.
        private const float FormEnd = 0.3f;
        private const float FallEnd = 0.7f;

        // Fases de la espera: la gota asoma, intenta caer y vuelve a colgar.
        private const float WaitGrowEnd = 0.35f;
        private const float WaitTryEnd = 0.7f;
        private const float WaitPipsAt = 0.6f;

        // Gota que cae (u): ancho, y su alto con el estirón de la caída.
        private const float FallDropU = 0.4f;

        private readonly Image[] _drops = new Image[PipRow.MaxPips];
        private Image _cloud = null!;
        private Image _hang = null!;
        private Image _fall = null!;
        private PipRow _pips = null!;
        private bool _on;
        private bool _inFlight;
        private bool _dark;
        private bool _hangOn;
        private int _rowCount;
        private int _key = -1;
        private float _u = 24f;
        private float _heightU = CuiaGeometry.HeightU(4);
        private float _cloudY;
        private float _cloudH = CloudMaxU;
        private float _cloudW = ProceduralArt.LluviaCloudWidthU;
        private float _dropY;
        private float _dropSize = 0.2f;
        private float _dropStep = 0.27f;
        private float _hangY;
        private float _hangSize = 0.32f;

        // Paso en curso.
        private bool _animating;
        private bool _fresh;
        private MechanicStepKind _kind;
        private float _t;
        private float _dur;
        private int _dose = -1;
        private int _tint;
        private Color _color;
        private float _startDark;
        private ICuiaBoard? _board;
        private int _index;
        private Cuia _after;
        private bool _afterSealed;
        private int _afterK;
        private int _afterCounter;
        private bool _impact;
        private bool _splashed;
        private bool _pipsReset;
        private Vector2 _surface;
        private IBoardFx? _fx;
        private bool _reduceMotion;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Lluvia;

        /// <inheritdoc />
        public override bool IsAnimating => _animating;

        /// <summary>Puntos del contador: el ritmo <c>k</c>.</summary>
        public int PipCount => _on ? _pips.Count : 0;

        /// <summary>Puntos llenos: vertidos útiles contados desde la última gota.</summary>
        public int FilledPips => _on ? _pips.Filled : 0;

        /// <summary>Gotas pendientes a la vista: las gotitas de la fila y la que cuelga.</summary>
        public int DropCount => _on ? _rowCount + (_hangOn ? 1 : 0) : 0;

        /// <summary>La nube está cargada de gris tormenta: la cuia está llena y la gota esperaría.</summary>
        public bool Stormy => _on && _dark;

        /// <summary>La gota que tocaba cuelga sobre la boca (cuia llena con el ritmo cumplido).</summary>
        public bool HangingDrop => _on && _hang.color.a > 0.01f && _hang.gameObject.activeSelf;

        /// <summary>Hay una gota cayendo (o formándose) a la vista.</summary>
        public bool FallingDropVisible => _fall.color.a > 0.01f && _fall.gameObject.activeSelf;

        /// <summary>Altura del centro de la gota que cae sobre el pie (u).</summary>
        public float FallingDropHeightU => _fall.rectTransform.anchoredPosition.y / _u;

        /// <summary>Opacidad de la nube (baja al irse con la última gota).</summary>
        public float CloudAlpha => _cloud.color.a;

        /// <summary>Lo que ha subido la nube sobre su sitio (u).</summary>
        public float CloudRiseU => (_cloud.rectTransform.anchoredPosition.y / _u) - _cloudY;

        /// <summary>Oscuridad de la nube: 0 perla, 1 gris tormenta (algo más mientras la gota intenta caer).</summary>
        public float CloudDarkness
        {
            get
            {
                Color c = _cloud.color;
                return Mathf.Clamp01((1f - c.g) / (1f - ProceduralArt.LluviaStormTint.g));
            }
        }

        /// <summary>Lo alto de la nube sobre la boca (dp; ≤ 32, dentro del área táctil).</summary>
        public float CloudTopAboveMouthDp
        {
            get
            {
                RectTransform rt = _cloud.rectTransform;
                return rt.anchoredPosition.y + (0.5f * rt.sizeDelta.y) - (Host.HeightU * _u);
            }
        }

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            _on = cuia.Mechanic == MechanicType.Lluvia;
            Visible = _on;
            if (_on)
            {
                _pips.Set(cuia.MechanicK, cuia.MechanicCounter);
                SetKey(cuia.MechanicKey);
                int pending = Mathf.Clamp(cuia.MechanicN, 0, _drops.Length);
                _dark = cuia.IsFull;
                _hangOn = _dark && pending > 0 && cuia.MechanicCounter >= cuia.MechanicK - 1;
                SetRow(_hangOn ? pending - 1 : pending);
            }

            Apply();
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            _heightU = heightU;
            _cloudH = Mathf.Min(CloudMaxU, ((CuiaView.TouchHeadroomDp - 1f) / u) - CloudGapU);
            float scale = _cloudH / CloudMaxU;
            _cloudW = _cloudH * ProceduralArt.LluviaCloudWidthU / ProceduralArt.LluviaCloudHeightU;
            _cloudY = heightU + CloudGapU + (0.5f * _cloudH);
            _pips.Layout(0f, _cloudY - (0.06f * _cloudH), 0.22f * scale, 0.32f * scale, u);
            _dropSize = 0.22f * scale;
            _dropStep = 0.3f * scale;
            _dropY = heightU + CloudGapU - (0.05f * scale);
            _hangSize = 0.36f * scale;
            _hangY = heightU + CloudGapU - (0.06f * scale);
            PlaceRow();
            if (!_animating)
            {
                PlaceAtRest();
            }
        }

        /// <inheritdoc />
        public override void SetInFlight(bool inFlight)
        {
            _inFlight = inFlight;
            Apply();
        }

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            switch (kind)
            {
                case MechanicStepKind.RainDrop:
                case MechanicStepKind.RainEnd:
                    return DropStepMs;
                case MechanicStepKind.RainWait:
                    return WaitStepMs;
                default:
                    return 0f;
            }
        }

        /// <inheritdoc />
        public override float PlayStep(in MechanicStepContext ctx)
        {
            float nominal = StepDurationMs(ctx.Kind);
            if (nominal <= 0f)
            {
                return 0f;
            }

            if (_animating)
            {
                FinishStep(false);
            }

            Cuia before = ctx.Before;
            _kind = ctx.Kind;
            _board = ctx.Board;
            _index = ctx.Index;
            _after = ctx.After;
            _afterSealed = ctx.AfterSealed;
            _afterK = ctx.After.MechanicK;
            _afterCounter = ctx.After.MechanicCounter;
            _fx = ctx.Fx;
            _reduceMotion = ctx.ReduceMotion;
            _dur = nominal * Mathf.Max(0.01f, ctx.SpeedFactor);
            _t = 0f;
            _fresh = true;
            _animating = true;
            _impact = false;
            _splashed = false;
            _pipsReset = false;
            _startDark = CloudDarkness;
            _tint = ctx.Tint > 0 ? ctx.Tint : before.MechanicKey;
            _color = Palette.Tint(_tint);
            SetKey(before.MechanicKey);

            // El vertido que completa el ritmo llena todos los puntos.
            int k = before.MechanicK;
            _pips.Set(k, k);
            int pending = Mathf.Clamp(before.MechanicN, 0, _drops.Length);
            SetRow(pending > 0 ? pending - 1 : 0); // la gota que cae (o la que cuelga) sale de la fila
            if (_kind == MechanicStepKind.RainWait)
            {
                _dose = -1;
            }
            else
            {
                _dose = before.Count;
                if (_board != null && _dose >= 0 && _dose < Host.Capacity)
                {
                    _board.SetDose(_index, _dose, _color, 0f, _tint);
                }

                Vector2 surface = ctx.SelfPos + new Vector2(0f, (CuiaGeometry.CavityBottomU + _dose) * _u);
                RectTransform? space = ctx.BoardFrontLayer;
                Rect r = space != null ? space.rect : default;
                _surface = new Vector2(surface.x - r.xMin, surface.y - r.yMax);
            }

            Apply();
            Pose(0f);
            return _dur;
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (!_animating)
            {
                return;
            }

            // El primer fotograma del paso no avanza: la cola ya llevaba parte de él al llamar a PlayStep, así que la
            // animación nunca va por delante del paso de la cola.
            if (_fresh)
            {
                _fresh = false;
            }
            else
            {
                _t += dtMs;
            }

            Pose(_dur > 0f ? Mathf.Clamp01(_t / _dur) : 1f);
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            FinishStep(true);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            // Nube (también cargada de tormenta) y puntos; las gotitas llevan el agua del mundo a propósito.
            into.Add(ProceduralArt.Pearl);
            into.Add(ProceduralArt.LluviaCloudLit);
            into.Add(ProceduralArt.LluviaCloudBelly);
            into.Add(ProceduralArt.Pearl * ProceduralArt.LluviaStormTint);
            into.Add(ProceduralArt.LluviaCloudLit * ProceduralArt.LluviaStormTint);
            into.Add(ProceduralArt.LluviaCloudBelly * ProceduralArt.LluviaStormTint);
            into.Add(ProceduralArt.Slate);
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            ProceduralArt art = Host.Art;
            _fall = NewStepImage("LluviaFallingDrop", Host.BodyBack, art.LluviaDropSprite(10)); // detrás del líquido y del vidrio
            _cloud = OverlayParts.NewImage("LluviaCloud", Host.BodyFront, art.LluviaCloudSprite, Color.white);
            _pips = new PipRow("LluviaPip", Host.BodyFront, art.CounterPip, art.CounterPipEmpty, ProceduralArt.Slate);
            for (int i = 0; i < _drops.Length; i++)
            {
                _drops[i] = OverlayParts.NewImage("LluviaDrop" + i, Host.BodyFront, art.LluviaDropSprite(10), Color.white);
            }

            _hang = NewStepImage("LluviaHangingDrop", Host.BodyFront, art.LluviaDropSprite(10));
        }

        // Imagen siempre activa y registrada en su Canvas que se muestra con el alfa: los pasos no la activan (sin asignar).
        private static Image NewStepImage(string name, Transform parent, Sprite sprite)
        {
            Image img = OverlayParts.NewImage(name, parent, sprite, new Color(1f, 1f, 1f, 0f));
            img.canvasRenderer.cullTransparentMesh = true;
            img.gameObject.SetActive(true);
            return img;
        }

        private void SetKey(int key)
        {
            if (key == _key)
            {
                return;
            }

            _key = key;
            Sprite drop = Host.Art.LluviaDropSprite(key);
            for (int i = 0; i < _drops.Length; i++)
            {
                _drops[i].sprite = drop;
            }

            _hang.sprite = drop;
            _fall.sprite = drop;
        }

        private void SetRow(int count)
        {
            count = Mathf.Clamp(count, 0, _drops.Length);
            if (count != _rowCount)
            {
                _rowCount = count;
                PlaceRow();
            }
        }

        private void PlaceRow()
        {
            float start = -0.5f * _dropStep * (_rowCount - 1);
            for (int i = 0; i < _drops.Length; i++)
            {
                OverlayParts.Place(_drops[i], start + (i * _dropStep), _dropY, _dropSize, _dropSize * ProceduralArt.LluviaDropAspect, _u);
            }
        }

        // Reposo: la nube en su sitio (perla o tormenta), la gota colgando si toca y ninguna cayendo.
        private void PlaceAtRest()
        {
            bool shown = _on && !_inFlight;
            OverlayParts.Place(_cloud, 0f, _cloudY, _cloudW, _cloudH, _u);
            SetScale(_cloud.rectTransform, 1f, 1f);
            OverlayParts.SetColor(_cloud, _dark ? ProceduralArt.LluviaStormTint : Color.white);
            _pips.SetAlpha(1f);
            SetRowAlpha(1f);
            OverlayParts.Place(_hang, 0f, _hangY, _hangSize, _hangSize * ProceduralArt.LluviaDropAspect, _u);
            SetScale(_hang.rectTransform, 1f, 1f);
            OverlayParts.SetColor(_hang, new Color(1f, 1f, 1f, shown && _hangOn ? 1f : 0f));
            OverlayParts.SetColor(_fall, new Color(1f, 1f, 1f, 0f));
        }

        private void Apply()
        {
            bool shown = _on && !_inFlight;
            OverlayParts.SetVisible(_cloud, shown);
            _pips.SetVisible(shown);
            for (int i = 0; i < _drops.Length; i++)
            {
                OverlayParts.SetVisible(_drops[i], shown && i < _rowCount);
            }

            if (!_animating)
            {
                PlaceAtRest();
            }
        }

        // Fotograma del paso en la fracción p (sin asignaciones).
        private void Pose(float p)
        {
            if (_kind == MechanicStepKind.RainWait)
            {
                PoseWait(p);
            }
            else
            {
                PoseDrop(p);
            }
        }

        private void PoseDrop(float p)
        {
            // Nube: se encoge al soltar la gota; con la última, sube y se desvanece con sus puntos y su fila.
            float squash = p < FormEnd ? Mathf.Sin(Mathf.PI * p / FormEnd) : 0f;
            float rise = 0f;
            float alpha = 1f;
            if (_kind == MechanicStepKind.RainEnd && p > FormEnd)
            {
                float q = (p - FormEnd) / (1f - FormEnd);
                rise = RiseU * Ease.InOut(q);
                alpha = 1f - Ease.In(q);
            }

            OverlayParts.Place(_cloud, 0f, _cloudY + rise, _cloudW, _cloudH, _u);
            SetScale(_cloud.rectTransform, 1f + (0.05f * squash), 1f - (0.08f * squash));
            float dark = Mathf.Lerp(_startDark, 0f, Mathf.Clamp01(p / FormEnd)); // con hueco, la tormenta se aclara
            OverlayParts.SetColor(_cloud, StormColor(dark, alpha));
            _pips.SetAlpha(alpha);
            SetRowAlpha(alpha);
            OverlayParts.SetColor(_hang, new Color(1f, 1f, 1f, 0f)); // con hueco no cuelga ninguna: la que cae es la de la fila

            // Gota: se forma bajo la panza, cae con gravedad y, al tocar la superficie, llena la dosis y salpica.
            float surfaceU = CuiaGeometry.CavityBottomU + Mathf.Max(0, _dose);
            if (p < FormEnd)
            {
                float grow = 0.55f + (0.45f * Ease.Out(p / FormEnd));
                PlaceFall(_dropY, grow, 1f, 1f);
            }
            else if (p < FallEnd)
            {
                float q = (p - FormEnd) / (FallEnd - FormEnd);
                float y = Mathf.Lerp(_dropY, surfaceU + (0.5f * FallDropU), q * q);
                PlaceFall(y, 1f, 1f + (0.25f * q), 1f);
            }
            else
            {
                OverlayParts.SetColor(_fall, new Color(1f, 1f, 1f, 0f));
                if (!_impact)
                {
                    _impact = true;
                    if (_kind == MechanicStepKind.RainDrop)
                    {
                        _pips.Set(_afterK, _afterCounter);
                    }
                }

                float fill = Ease.Out((p - FallEnd) / (1f - FallEnd));
                if (_board != null && _dose >= 0 && _dose < Host.Capacity)
                {
                    _board.SetDose(_index, _dose, _color, fill, _tint);
                }

                // La salpicadura, en la superficie que sube (no dentro de la dosis ya formada).
                if (!_splashed && fill >= 0.7f)
                {
                    _splashed = true;
                    Splash(fill);
                }
            }
        }

        private void PoseWait(float p)
        {
            // La nube se carga de tormenta (con un pulso más oscuro mientras la gota intenta caer).
            float dark = Mathf.Lerp(_startDark, 1f, Ease.Out(Mathf.Clamp01(p / WaitGrowEnd)));
            if (p > WaitGrowEnd && p < WaitTryEnd + 0.05f)
            {
                dark += 0.25f * Mathf.Sin(Mathf.PI * (p - WaitGrowEnd) / (WaitTryEnd + 0.05f - WaitGrowEnd));
            }

            OverlayParts.Place(_cloud, 0f, _cloudY, _cloudW, _cloudH, _u);
            SetScale(_cloud.rectTransform, 1f, 1f);
            OverlayParts.SetColor(_cloud, StormColor(dark, 1f));

            // La gota asoma, baja hasta la boca llena, no cabe y vuelve a colgar con un temblor.
            float y = _hangY;
            float sx = 1f;
            float sy = 1f;
            if (p < WaitGrowEnd)
            {
                float g = Ease.OutBack(p / WaitGrowEnd);
                sx = g;
                sy = g;
            }
            else if (p < WaitTryEnd)
            {
                float dip = Mathf.Sin(Mathf.PI * (p - WaitGrowEnd) / (WaitTryEnd - WaitGrowEnd));
                y -= 0.14f * dip;
                sy = 1f + (0.3f * dip);
                sx = 1f - (0.1f * dip);
            }
            else
            {
                float q = (p - WaitTryEnd) / (1f - WaitTryEnd);
                sy = 1f + (0.08f * Mathf.Sin(3f * Mathf.PI * q) * (1f - q));
            }

            OverlayParts.Place(_hang, 0f, y, _hangSize, _hangSize * ProceduralArt.LluviaDropAspect, _u);
            SetScale(_hang.rectTransform, sx, sy);
            OverlayParts.SetColor(_hang, new Color(1f, 1f, 1f, _on && !_inFlight ? 1f : 0f));
            if (!_pipsReset && p >= WaitPipsAt)
            {
                _pipsReset = true;
                _pips.Set(_afterK, _afterCounter); // la gota no cayó: el contador se queda en k − 1
            }
        }

        private void PlaceFall(float yU, float grow, float stretch, float alpha)
        {
            OverlayParts.Place(_fall, 0f, yU, FallDropU, FallDropU * ProceduralArt.LluviaDropAspect, _u);
            SetScale(_fall.rectTransform, grow, grow * stretch);
            OverlayParts.SetColor(_fall, new Color(1f, 1f, 1f, _on && !_inFlight ? alpha : 0f));
        }

        // Salpicadura a la altura fill de la dosis nueva. Los efectos van arriba a la izquierda del tablero, así que la posición
        // de la capa (centrada) se pasa a ese espacio con el tamaño que tenía la capa al leerla (al empezar el paso).
        private void Splash(float fill)
        {
            if (_fx == null || _reduceMotion)
            {
                return; // Reducir movimiento: el pool no emite (02 §4)
            }

            _fx.Splash(_surface + new Vector2(0f, fill * _u), _color, _u);
        }

        // Termina el paso: la dosis entera, la nube en reposo y, si cierra, el estado de después (la cola lo vuelve a pintar).
        private void FinishStep(bool paint)
        {
            if (!_animating)
            {
                return;
            }

            _animating = false;
            if (_board != null && _dose >= 0 && _dose < Host.Capacity)
            {
                _board.SetDose(_index, _dose, _color, 1f, _tint);
            }

            ICuiaBoard? board = _board;
            _board = null;
            _fx = null;
            _dose = -1;
            if (paint && board != null)
            {
                board.ShowContent(_index, _after, _afterSealed);
            }
            else
            {
                PlaceAtRest();
            }
        }

        private void SetRowAlpha(float alpha)
        {
            var c = new Color(1f, 1f, 1f, alpha);
            for (int i = 0; i < _drops.Length; i++)
            {
                OverlayParts.SetColor(_drops[i], c);
            }
        }

        private static Color StormColor(float dark, float alpha)
        {
            Color storm = ProceduralArt.LluviaStormTint;
            return new Color(
                Mathf.Max(0f, Mathf.LerpUnclamped(1f, storm.r, dark)),
                Mathf.Max(0f, Mathf.LerpUnclamped(1f, storm.g, dark)),
                Mathf.Max(0f, Mathf.LerpUnclamped(1f, storm.b, dark)),
                alpha);
        }

        private static void SetScale(Transform t, float x, float y)
        {
            var s = new Vector3(x, y, 1f);
            if (t.localScale != s)
            {
                t.localScale = s;
            }
        }
    }
}
