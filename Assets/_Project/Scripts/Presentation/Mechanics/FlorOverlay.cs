#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Flor de vitória-régia (variante de la Liana con fruta de la 1.0, estreno en el 403; 02 §9.1 «Variantes», §1.2 y R10 d;
    /// propuesta de D-007 b.3; tarea VA1b) [P]: un capullo de pétalos blancos (<see cref="ProceduralArt.FlorWhite"/>) sobre un
    /// cáliz espinoso de vino que tapa la boca (cerrada no es origen ni destino, 02 §1.2: «la Flor cerrada, como la Liana») y,
    /// colgadas de los dos hombros, sus dos frutas llave (<c>key</c> a la izquierda y <c>key2</c> a la derecha), a la vista desde
    /// el principio, cada una como una muestra de su dosis (<c>ProceduralArt.HojaAtadaSwatch</c>: un disco del tinte con su patrón
    /// y su icono, como en la Hoja atada; la fruta en vuelo es de otro color que su líquido y no se lee como la llave, revisión
    /// de VA1a y captura va1b_20 del cacao). Dos llaves en lógica Y (b.3): con una de las dos frutas sellada en otra cuia, esa fruta lleva su ✓ y la flor
    /// está blanca y entreabierta; con las dos, el núcleo la suelta en la etapa (d) y el paso Release la vuelve rosa y la abre
    /// del todo antes de desvanecerse. El contenido se ve (Q17 A) y nada cambia al seleccionar (D-002).
    /// </summary>
    /// <remarks>
    /// <para><b>Lo distingue de la Liana</b> (su base): ni vueltas de liana ni nudo; una flor blanca en la boca y dos frutas, una
    /// por hombro, en lugar de una.</para>
    /// <para><b>Entreabierta</b> (b.3: «con una sola, la flor está blanca y entreabierta»): el núcleo no guarda nada (la Flor se
    /// abre por sellados que se deducen del estado, K3b), así que la vista lo deduce de lo que se ve: una llave se cumple cuando
    /// otra cuia sellada en la vista (con su tapa, <c>CuiaView.Sealed</c>) tiene esa fruta en la cima. Lo mira cada fotograma en
    /// <see cref="Tick"/> (16 cuias como mucho, sin asignaciones), porque el sellado de otra cuia no repinta esta. Al cambiar,
    /// los pétalos se abren o se cierran en <see cref="HalfOpenMs"/> y la ✓ aparece con un pequeño rebote; una Marea que deshace
    /// el sellado la vuelve a cerrar (02 §1.2: «Marea restaura»).</para>
    /// <para><b>Desbloqueo</b> (paso Release de la etapa (d) o (d′), <see cref="ReleaseMs"/> × factor de velocidad, como la
    /// Liana; <c>ctx.OtherIndex</c> = la cuia que acaba de sellar la segunda llave): las dos ✓ a la vista, los pétalos pasan del
    /// blanco al rosa (<see cref="ProceduralArt.FlorPink"/>) y se abren del todo, con unos destellos rosas; la flor abierta se
    /// queda un momento y se desvanece con su cáliz y sus frutas. La cola cierra el paso con <see cref="SnapStep"/> y
    /// <c>ShowContent(ctx.After)</c>, que la oculta (la cuia ya es normal). Fases [P] en fracción del paso: ✓ 0-0,16; color
    /// 0,10-0,50; apertura 0,08-0,50; destellos a 0,40; abierta y quieta hasta 0,70; se desvanece 0,70-1.</para>
    /// <para><b>Tamaño</b>: lo más alto, la punta del pétalo central cerrado, queda <see cref="TopAboveRimU"/> = 0,84 u sobre lo
    /// alto de la cuia, por debajo del tope del Brote (1,0 u; la fila de arriba). Las muestras de las llaves, de
    /// <see cref="KeyDiscU"/> (unos 18 dp con las cuias de 47 dp), cuelgan a <see cref="FruitCenterXU"/> del eje, con su borde de
    /// abajo sobre la cara del líquido de la cuia llena.</para>
    /// <para>Sin asignaciones en <see cref="Show"/>, <see cref="Tick"/> ni en el paso (08 §5.4); todo se crea en
    /// <see cref="OnAttach"/>. Colores: <see cref="CollectSurfaceColors"/> (las frutas llevan su tinte a propósito).</para>
    /// </remarks>
    public sealed class FlorOverlay : CuiaOverlay
    {
        /// <summary>Desbloqueo de la Flor (ms, sin factor de velocidad) [P]: el de la Liana, su base (b.2 M3: 600-900 ms).</summary>
        public const float ReleaseMs = LianaOverlay.ReleaseMs;

        /// <summary>Lo que tarda en entreabrirse o cerrarse al cumplirse o deshacerse una llave (ms) [P].</summary>
        public const float HalfOpenMs = 320f;

        /// <summary>Pie de los pétalos bajo lo alto de la cuia (u; desde <c>HeightU</c>): dentro del cáliz.</summary>
        public const float PetalFootBelowTopU = 0.16f;

        /// <summary>Pie del cáliz bajo lo alto de la cuia (u): cubre el borde de fibra hasta el labio, como el del Brote.</summary>
        public const float SepalsBelowTopU = 0.28f;

        /// <summary>Lo más alto de la flor cerrada sobre lo alto de la cuia (u): la punta del pétalo central.</summary>
        public const float TopAboveRimU = ProceduralArt.FlorPetalHeightU - PetalFootBelowTopU;

        /// <summary>Escala de la muestra de cada llave sobre la de la Hoja atada [P]: algo mayor, porque cuelga sin medallón.</summary>
        public const float KeyScale = 1.15f;

        /// <summary>Lado del sprite de la muestra de cada llave (u).</summary>
        public const float KeySpriteU = ProceduralArt.HojaAtadaSwatchSpriteU * KeyScale;

        /// <summary>Diámetro del disco de cada llave (u).</summary>
        public const float KeyDiscU = ProceduralArt.HojaAtadaSwatchU * KeyScale;

        /// <summary>Centro de cada fruta: a un lado y otro del cuello, sobre los hombros (u desde el eje) [P].</summary>
        public const float FruitCenterXU = 0.60f;

        /// <summary>Centro de las frutas sobre el techo de la cavidad (u) [P]: su borde de abajo queda sobre el líquido de la cuia llena.</summary>
        public const float FruitCenterAboveCavityU = 0.66f;

        /// <summary>Lado de la ✓ de una llave cumplida (u): la de la Liana.</summary>
        public const float CheckU = ProceduralArt.LianaCheckU;

        /// <summary>Pétalos: dos de atrás, dos de en medio y el del centro, delante.</summary>
        public const int PetalCount = 5;

        /// <summary>Apertura de la flor entreabierta (fracción de la apertura completa).</summary>
        public const float HalfOpen = 0.45f;

        /// <summary>Destellos al abrirse.</summary>
        public const int SparkleCount = 5;

        // Fases del desbloqueo (fracción del paso).
        private const float CheckInEnd = 0.16f;
        private const float OpenFrom = 0.08f;
        private const float OpenTo = 0.50f;
        private const float PinkFrom = 0.10f;
        private const float PinkTo = 0.50f;
        private const float SparkleAt = 0.40f;
        private const float FadeFrom = 0.70f;

        // Margen tras la duración con el que el overlay cierra solo un paso que la cola no cerró (no debería pasar).
        private const float StuckGuardMs = 1000f;

        // Pétalos (de atrás adelante): lado, separación del pie (u) y giro hacia fuera cerrada y abierta del todo (grados).
        private static readonly float[] s_side = { -1f, 1f, -1f, 1f, 0f };
        private static readonly float[] s_footX = { 0.10f, 0.10f, 0.06f, 0.06f, 0f };
        private static readonly float[] s_closedDeg = { 15f, 15f, 7f, 7f, 0f };
        private static readonly float[] s_openDeg = { 72f, 72f, 42f, 42f, 0f };

        private readonly Image[] _petals = new Image[PetalCount];
        private Image _sepals = null!;
        private Image _fruitL = null!;
        private Image _fruitR = null!;
        private Image _checkL = null!;
        private Image _checkR = null!;
        private RectTransform _slot = null!;
        private BoardAnimator? _animator;
        private IBoardFx? _fx;
        private float _u = 24f;
        private float _heightU = 5.6f;
        private int _key;
        private int _key2;
        private bool _met1;
        private bool _met2;
        private float _open;
        private float _openFrom;
        private float _openTarget;
        private float _tween = -1f;
        private float _check1Pop = 1f;
        private float _check2Pop = 1f;
        private float _pink;
        private float _alpha = 1f;
        private float _elapsed = -1f;
        private float _duration;
        private float _releaseFrom;
        private bool _sparkled;
        private bool _reduceMotion;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Flor;

        /// <inheritdoc />
        public override bool IsAnimating => _elapsed >= 0f;

        /// <summary>Fracción del desbloqueo en curso (0..1), o −1 sin paso.</summary>
        public float StepProgress => _elapsed < 0f ? -1f : Mathf.Clamp01(_elapsed / Mathf.Max(1f, _duration));

        /// <summary>Primera llave a la vista (<c>key</c>, tinte 1-14; fruta del hombro izquierdo), o 0.</summary>
        public int ShownKey => _key;

        /// <summary>Segunda llave a la vista (<c>key2</c>; fruta del hombro derecho), o 0.</summary>
        public int ShownKey2 => _key2;

        /// <summary>Llaves cumplidas a la vista (0, 1 o 2): otra cuia sellada en la vista con esa fruta en la cima.</summary>
        public int KeysMet => (_met1 ? 1 : 0) + (_met2 ? 1 : 0);

        /// <summary>La llave <paramref name="which"/> (1 = <c>key</c>, 2 = <c>key2</c>) está cumplida a la vista.</summary>
        public bool KeyMet(int which)
        {
            return which == 1 ? _met1 : _met2;
        }

        /// <summary>Apertura de los pétalos: 0 cerrada, <see cref="HalfOpen"/> entreabierta y 1 abierta del todo.</summary>
        public float Openness => _open;

        /// <summary>Rosa de los pétalos: 0 blancos (<see cref="ProceduralArt.FlorWhite"/>) y 1 rosas (<see cref="ProceduralArt.FlorPink"/>).</summary>
        public float Pinkness => _pink;

        /// <summary>Color pintado del pétalo del centro (con su alfa).</summary>
        public Color PetalColor => _petals[PetalCount - 1].color;

        /// <summary>Los pétalos están a la vista.</summary>
        public bool PetalsVisible => _petals[PetalCount - 1].gameObject.activeSelf && _petals[PetalCount - 1].color.a > 0.001f;

        /// <summary>El cáliz que tapa la boca está a la vista.</summary>
        public bool SepalsVisible => _sepals.gameObject.activeSelf && _sepals.color.a > 0.001f;

        /// <summary>Las dos frutas llave están a la vista.</summary>
        public bool FruitsVisible => _fruitL.gameObject.activeSelf && _fruitR.gameObject.activeSelf && _fruitL.color.a > 0.001f;

        /// <summary>Sprite de la llave del hombro izquierdo (<c>key</c>, 1) o derecho (<c>key2</c>, 2): la muestra de su dosis.</summary>
        public Sprite? FruitSprite(int which)
        {
            return which == 1 ? _fruitL.sprite : _fruitR.sprite;
        }

        /// <summary>La ✓ de la llave <paramref name="which"/> está a la vista.</summary>
        public bool CheckVisible(int which)
        {
            Image check = which == 1 ? _checkL : _checkR;
            return check.gameObject.activeSelf && check.color.a > 0.001f;
        }

        /// <summary>Giro hacia fuera del pétalo <paramref name="i"/> (grados; 0 = vertical).</summary>
        public float PetalOutDeg(int i)
        {
            float z = Mathf.DeltaAngle(0f, _petals[i].rectTransform.localEulerAngles.z);
            return s_side[i] == 0f ? Mathf.Abs(z) : -s_side[i] * z;
        }

        /// <summary>Lo más alto de los pétalos a la vista (u desde el pie de la cuia), girados y con su escala.</summary>
        public float PetalTopU
        {
            get
            {
                float top = float.NegativeInfinity;
                for (int i = 0; i < PetalCount; i++)
                {
                    RectTransform rt = _petals[i].rectTransform;
                    float len = rt.sizeDelta.y * Mathf.Abs(rt.localScale.y);
                    Vector2 tip = rt.anchoredPosition + (Vector2)(rt.localRotation * new Vector3(0f, len, 0f));
                    top = Mathf.Max(top, tip.y / Mathf.Max(0.01f, _u));
                }

                return top;
            }
        }

        /// <summary>Rectángulo (u desde el pie de la cuia) de la muestra de la llave <paramref name="which"/> (el sprite entero).</summary>
        public Rect FruitRectU(int which)
        {
            RectTransform rt = (which == 1 ? _fruitL : _fruitR).rectTransform;
            float u = Mathf.Max(0.01f, _u);
            Vector2 size = rt.sizeDelta / u;
            Vector2 pos = rt.anchoredPosition / u;
            return new Rect(pos.x - (0.5f * size.x), pos.y - (0.5f * size.y), size.x, size.y);
        }

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Flor;
            Visible = on;
            if (on)
            {
                int key = cuia.MechanicKey;
                int key2 = cuia.MechanicKey2;
                if (key != _key || key2 != _key2)
                {
                    _key = key;
                    _key2 = key2;
                    _fruitL.sprite = key > 0 ? Host.Art.HojaAtadaSwatch(key) : null;
                    _fruitR.sprite = key2 > 0 ? Host.Art.HojaAtadaSwatch(key2) : null;
                }

                // Repinta el estado que se ve, sin transición (carga, Marea, celebración): la apertura de las llaves cumplidas.
                Poll(out _met1, out _met2);
                _openTarget = OpenFor(_met1, _met2);
                _open = _openTarget;
                _tween = -1f;
                _check1Pop = 1f;
                _check2Pop = 1f;
            }

            _pink = 0f;
            _alpha = 1f;
            for (int i = 0; i < PetalCount; i++)
            {
                OverlayParts.SetVisible(_petals[i], on);
            }

            OverlayParts.SetVisible(_sepals, on);
            OverlayParts.SetVisible(_fruitL, on && _key > 0);
            OverlayParts.SetVisible(_fruitR, on && _key2 > 0);
            Apply();
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            _heightU = heightU;
            Apply();
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (_elapsed >= 0f)
            {
                TickRelease(dtMs);
                return;
            }

            if (!Visible)
            {
                return;
            }

            // Las llaves se cumplen (o se deshacen) por sellados de otras cuias, que no repintan esta: se mira cada fotograma.
            Poll(out bool met1, out bool met2);
            bool changed = false;
            if (met1 != _met1)
            {
                _met1 = met1;
                _check1Pop = met1 ? 0f : 1f;
                changed = true;
            }

            if (met2 != _met2)
            {
                _met2 = met2;
                _check2Pop = met2 ? 0f : 1f;
                changed = true;
            }

            if (changed)
            {
                _openFrom = _open;
                _openTarget = OpenFor(_met1, _met2);
                _tween = 0f;
            }

            bool dirty = changed;
            if (_tween >= 0f)
            {
                _tween += dtMs;
                float p = Mathf.Clamp01(_tween / HalfOpenMs);
                _open = Mathf.Lerp(_openFrom, _openTarget, Ease.OutCubic(p));
                if (p >= 1f)
                {
                    _tween = -1f;
                }

                dirty = true;
            }

            if (_check1Pop < 1f || _check2Pop < 1f)
            {
                _check1Pop = Mathf.Min(1f, _check1Pop + (dtMs / HalfOpenMs));
                _check2Pop = Mathf.Min(1f, _check2Pop + (dtMs / HalfOpenMs));
                dirty = true;
            }

            if (dirty)
            {
                Apply();
            }
        }

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            return kind == MechanicStepKind.Release ? ReleaseMs : 0f;
        }

        /// <inheritdoc />
        public override float PlayStep(in MechanicStepContext ctx)
        {
            float ms = StepDurationMs(ctx.Kind) * ctx.SpeedFactor;
            if (ms <= 0f)
            {
                return 0f;
            }

            _fx = ctx.Fx ?? Host.Fx;
            _reduceMotion = ctx.ReduceMotion;
            _duration = ms;
            _elapsed = 0f;
            _sparkled = false;
            _tween = -1f;
            _releaseFrom = _open;

            // La llave que acaba de cumplirse (si la vista aún no la había visto sellada) estrena su ✓; la otra ya la tenía.
            _check1Pop = _met1 ? 1f : 0f;
            _check2Pop = _met2 ? 1f : 0f;
            _met1 = true;
            _met2 = true;
            Visible = true;
            for (int i = 0; i < PetalCount; i++)
            {
                OverlayParts.SetVisible(_petals[i], true);
            }

            OverlayParts.SetVisible(_sepals, true);
            OverlayParts.SetVisible(_fruitL, _key > 0);
            OverlayParts.SetVisible(_fruitR, _key2 > 0);
            ApplyRelease(0f);
            return ms;
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            if (_elapsed < 0f)
            {
                return;
            }

            // Termina abierta: la cuia ya es normal y ShowContent(After) la oculta; si la cola vuelve a pintarla con la Flor
            // (Marea), Show la repone desde el estado.
            _elapsed = -1f;
            _pink = 0f;
            _alpha = 1f;
            for (int i = 0; i < PetalCount; i++)
            {
                OverlayParts.SetVisible(_petals[i], false);
            }

            OverlayParts.SetVisible(_sepals, false);
            OverlayParts.SetVisible(_fruitL, false);
            OverlayParts.SetVisible(_fruitR, false);
            OverlayParts.SetVisible(_checkL, false);
            OverlayParts.SetVisible(_checkR, false);
            Visible = false;
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(ProceduralArt.FlorWhite); // los pétalos, blancos
            into.Add(ProceduralArt.FlorPink); // y rosas al abrirse (las frutas llevan su tinte a propósito)
            into.Add(ProceduralArt.FlorSepal); // el cáliz
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            _slot = (RectTransform)Host.SlotBack.parent;
            _animator = Host.Board as BoardAnimator;
            ProceduralArt art = Host.Art;
            for (int i = 0; i < PetalCount; i++)
            {
                _petals[i] = OverlayParts.NewImage("FlorPetal" + i, Host.BodyFront, art.FlorPetal, ProceduralArt.FlorWhite);
                _petals[i].rectTransform.pivot = new Vector2(0.5f, 0f);
            }

            _sepals = OverlayParts.NewImage("FlorSepals", Host.BodyFront, art.FlorSepals, Color.white);
            _sepals.rectTransform.pivot = new Vector2(0.5f, 0f);
            _fruitL = OverlayParts.NewImage("FlorFruitL", Host.BodyFront, null, Color.white);
            _fruitR = OverlayParts.NewImage("FlorFruitR", Host.BodyFront, null, Color.white);
            _checkL = OverlayParts.NewImage("FlorCheckL", Host.BodyFront, art.LianaCheck, Color.white);
            _checkR = OverlayParts.NewImage("FlorCheckR", Host.BodyFront, art.LianaCheck, Color.white);
        }

        // Apertura que corresponde a las llaves cumplidas: cerrada, entreabierta con una (b.3) y, con las dos, entreabierta
        // también hasta el paso Release (el núcleo la suelta en el mismo vertido que sella la segunda).
        private static float OpenFor(bool met1, bool met2)
        {
            return met1 || met2 ? HalfOpen : 0f;
        }

        // Llaves cumplidas a la vista: otra cuia sellada (con su tapa en la vista) con esa fruta en la cima. Sin tablero, nada.
        private void Poll(out bool met1, out bool met2)
        {
            met1 = false;
            met2 = false;
            ICuiaBoard? board = Host.Board;
            if (board == null || _key <= 0)
            {
                return;
            }

            int self = Host.Index;
            int n = board.Count;
            for (int j = 0; j < n; j++)
            {
                if (j == self)
                {
                    continue;
                }

                Cuia c = board.ShownCuia(j);
                if (c.IsEmpty)
                {
                    continue;
                }

                int top = c.TopColor;
                if ((top != _key && top != _key2) || !SealedShown(j, c))
                {
                    continue;
                }

                if (top == _key)
                {
                    met1 = true;
                }
                else
                {
                    met2 = true;
                }
            }
        }

        // La cuia j está sellada en la vista: con la tapa puesta (CuiaView.Sealed). Fuera del tablero del juego (un tablero de
        // prueba sin vistas), llena, de un solo color y sin ocultas, como R5 con capacidad = U_c (la Flor exige capacidades
        // uniformes mientras dure el bloqueo C1-9).
        private bool SealedShown(int j, Cuia c)
        {
            CuiaView? view = _animator?.ViewAt(j);
            if (view != null)
            {
                return view.Sealed;
            }

            return c.IsFull && c.IsMonocolor && !c.HasHidden;
        }

        private void TickRelease(float dtMs)
        {
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

            // Pasada la duración se queda en el último fotograma (transparente) hasta que la cola llame a SnapStep.
            ApplyRelease(Mathf.Clamp01(_elapsed / Mathf.Max(1f, _duration)));
        }

        // Fotograma t (0..1) del desbloqueo; los destellos salen una sola vez al cruzar su instante.
        private void ApplyRelease(float t)
        {
            _check1Pop = Mathf.Max(_check1Pop, Mathf.Clamp01(t / CheckInEnd));
            _check2Pop = Mathf.Max(_check2Pop, Mathf.Clamp01(t / CheckInEnd));
            float open = Mathf.Clamp01((t - OpenFrom) / (OpenTo - OpenFrom));
            _open = Mathf.Lerp(_releaseFrom, 1f, Ease.OutCubic(open));
            _pink = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - PinkFrom) / (PinkTo - PinkFrom)));
            _alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - FadeFrom) / (1f - FadeFrom)));
            if (!_sparkled && t >= SparkleAt)
            {
                _sparkled = true;
                if (!_reduceMotion && _fx != null)
                {
                    float y = _heightU + (0.45f * TopAboveRimU);
                    _fx.Sparkles(_slot.anchoredPosition + new Vector2(0f, y * _u), ColorMath.Lighten(ProceduralArt.FlorPink, 0.35f), SparkleCount, _u);
                }
            }

            Apply();
        }

        // Pinta la pose actual: pétalos con la apertura, el color y el alfa; el cáliz; las frutas y sus ✓.
        private void Apply()
        {
            float foot = _heightU - PetalFootBelowTopU;
            float lift = _elapsed >= 0f ? 0.1f * (1f - _alpha) : 0f; // al desvanecerse, la flor sube un poco
            Color petal = Color.Lerp(ProceduralArt.FlorWhite, ProceduralArt.FlorPink, _pink);
            petal.a = _alpha;
            float scale = 1f + (0.12f * _open);
            for (int i = 0; i < PetalCount; i++)
            {
                Image img = _petals[i];
                float side = s_side[i];
                float deg = Mathf.Lerp(s_closedDeg[i], s_openDeg[i], _open);
                float x = side * (s_footX[i] + (0.08f * _open));
                OverlayParts.Place(img, x, foot + lift, ProceduralArt.FlorPetalWidthU, ProceduralArt.FlorPetalHeightU, _u);
                RectTransform rt = img.rectTransform;

                // La izquierda gira a la izquierda (z positiva) al abrirse; el pétalo del centro se encoge un poco al abrirse
                // la flor, para que se lea abierta y no más alta. Se compara componente a componente: el == de Quaternion da
                // por iguales giros de menos de ≈ 0,1°.
                Quaternion rotation = Quaternion.Euler(0f, 0f, -side * deg);
                Quaternion current = rt.localRotation;
                if (current.z != rotation.z || current.w != rotation.w || current.x != rotation.x || current.y != rotation.y)
                {
                    rt.localRotation = rotation;
                }

                float sy = side == 0f ? 1f - (0.18f * _open) : scale;
                var s = new Vector3(scale, sy, 1f);
                if (rt.localScale != s)
                {
                    rt.localScale = s;
                }

                // Los de dentro, un poco más claros: la flor tiene volumen.
                Color c = i == PetalCount - 1 ? petal : (i < 2 ? ColorMath.Darken(petal, 0.06f) : petal);
                c.a = _alpha;
                OverlayParts.SetColor(img, c);
            }

            OverlayParts.Place(_sepals, 0f, _heightU - SepalsBelowTopU, ProceduralArt.FlorSepalsWidthU, ProceduralArt.FlorSepalsHeightU, _u);
            OverlayParts.SetColor(_sepals, new Color(1f, 1f, 1f, _alpha));

            float cavityTop = CuiaGeometry.CavityTopU(Host.Capacity);
            float fy = cavityTop + FruitCenterAboveCavityU;
            OverlayParts.Place(_fruitL, -FruitCenterXU, fy, KeySpriteU, KeySpriteU, _u);
            OverlayParts.Place(_fruitR, FruitCenterXU, fy, KeySpriteU, KeySpriteU, _u);
            var fruit = new Color(1f, 1f, 1f, _alpha);
            OverlayParts.SetColor(_fruitL, fruit);
            OverlayParts.SetColor(_fruitR, fruit);
            PlaceCheck(_checkL, -FruitCenterXU, fy, _met1, _check1Pop);
            PlaceCheck(_checkR, FruitCenterXU, fy, _met2, _check2Pop);
        }

        // La ✓ de una llave cumplida, sobre su fruta, con un rebote al aparecer (pop 0 → 1).
        private void PlaceCheck(Image check, float x, float y, bool met, float pop)
        {
            bool on = Visible && met && pop > 0f;
            OverlayParts.SetVisible(check, on);
            if (!on)
            {
                return;
            }

            OverlayParts.Place(check, x + 0.18f, y - 0.14f, CheckU, CheckU, _u);
            float s = Ease.OutBack(Mathf.Clamp01(pop));
            var scale = new Vector3(s, s, 1f);
            RectTransform rt = check.rectTransform;
            if (rt.localScale != scale)
            {
                rt.localScale = scale;
            }

            OverlayParts.SetColor(check, new Color(1f, 1f, 1f, _alpha));
        }
    }
}
