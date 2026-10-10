#nullable enable
using AF.Core.Board;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Aguas turbias en una cuia (07 §4.3; 02 §11 T3; propuesta de D-007 b.2): pinta las dosis ocultas en el shader del líquido
    /// (<see cref="LiquidGraphic.MurkyLook"/>: pardo opaco <see cref="Palette.Murky"/>, trama diagonal al 25 % y «?» marfil de
    /// 0,5 u, sin su tinte ni el patrón de su fruta), muestra la insignia «?×n» con las ocultas que quedan y anima el revelado.
    /// <c>CuiaView</c> le delega el pintado (<see cref="PaintHidden"/>), la sincroniza con el estado (<see cref="Show"/>) y le
    /// delega el revelado animado (<see cref="PlayReveal"/>, <c>CuiaView.PlayReveal</c>), así que el aspecto de las turbias
    /// cambia aquí sin tocar la vista.
    /// </summary>
    /// <remarks>
    /// <para>La trama y el «?» los pinta el shader dentro de la banda de su dosis, así que siguen a la banda cuando la cuia se
    /// inclina y nunca salen de ella; ya no hay «?» sueltos en sprites. Lo único en sprite es la insignia «?×n» (02 §11 T3)
    /// [P]: una píldora gris parda de <see cref="ProceduralArt.MurkyBadgeDp"/> dp con los glifos «?», «×» y la cifra, centrada
    /// bajo el pie, sobre la cara de la repisa (guía 4.5). Cuelga del cuerpo (sube con la selección) y se oculta en vuelo.</para>
    /// <para><b>Revelado por la regla</b> (R7; b.2 y ficha de Magic Sort §3.1; <see cref="RevealRuleMs"/> = 1.200 ms × factor, por
    /// debajo de los 1,5 s de b.2): como en Magic Sort, primero sale el color nuevo (la dosis pasa del pardo a su tinte entre
    /// <see cref="RuleTintFrom"/> y <see cref="RuleTintFullAt"/>) y el «?» crece, ondula y se disuelve encima, con 3-4
    /// destellos blancos de estrella (<see cref="IBoardFx.Sparkles"/>); sube una burbuja (<see cref="IBoardFx.Bubbles"/>; la
    /// «boiada» del pirarucú). Así nunca se ve el pardo sin «?» (07 §3, fila Cacao). La cola lo despacha al origen cuando ya
    /// volvió a su ranura (T07).</para>
    /// <para><b>Luciérnaga</b> (<see cref="RevealStyle.Firefly"/>, 04 §2.5; la usa T14): el pardo se aclara de arriba abajo en
    /// <see cref="RevealFireflyMs"/> (la de abajo llega a su tinte justo al final) y cada «?» gira hasta quedar de canto y
    /// muestra el icono de su fruta (<see cref="ProceduralArt.FruitFlight"/>), que gira de vuelta y se apaga.</para>
    /// <para>Mientras anima (<see cref="IsAnimating"/>), <c>ShowContent</c> no llama a <see cref="Show"/> ni repinta las capas
    /// que se revelan; la cola termina el paso con <see cref="Snap"/> y el <c>ShowContent</c> final. Las imágenes se crean una
    /// vez al construir; los sprites de la insignia, la primera vez que hace falta (<see cref="Prewarm"/>). Sin asignaciones
    /// al animar.</para>
    /// </remarks>
    public sealed class MurkyMarks
    {
        /// <summary>Alto del «?» marfil que pinta el shader, sin el halo (07 §4.3: 0,5 u).</summary>
        public const float MarkU = ProceduralArt.QuestionGlyphU;

        /// <summary>
        /// Revelado por la regla: 1.200 ms × factor de velocidad [P] (b.2: «en menos de 1,5 s»; ficha de Magic Sort §3.1: menos
        /// de 2 s; sustituye a los 300 ms de 02 §4.1 y 07 §4.3, que se alinean en el paso 2).
        /// </summary>
        public const float RevealRuleMs = 1200f;

        /// <summary>Revelado de la Luciérnaga: las capas se aclaran de arriba abajo en 600 ms (04 §2.5).</summary>
        public const float RevealFireflyMs = 600f;

        /// <summary>Por la regla, fracción del revelado en la que salen los destellos (≈ 145 ms) [P].</summary>
        public const float RuleSparklesAt = 0.12f;

        /// <summary>Por la regla, fracción en la que sube la burbuja (360 ms) [P].</summary>
        public const float RuleBubbleAt = 0.30f;

        /// <summary>
        /// Por la regla, fracción del revelado en la que el pardo empieza a pasar al tinte (120 ms) [P]: antes de que el «?»
        /// empiece a apagarse (≈ 0,19), para que se disuelva sobre el color nuevo (ficha de Magic Sort §3.1).
        /// </summary>
        public const float RuleTintFrom = 0.10f;

        /// <summary>
        /// Por la regla, fracción en la que la dosis ya tiene su tinte (480 ms; ease-in-out desde <see cref="RuleTintFrom"/>)
        /// [P]: a mitad del apagado del «?» (≈ 0,33) va ya al 95 % y el «?» termina de irse (≈ 0,47) sobre el tinte, así que
        /// nunca queda el pardo sin «?» (07 §3).
        /// </summary>
        public const float RuleTintFullAt = 0.40f;

        /// <summary>
        /// Por la regla, fracción en la que el «?» ya se disolvió y la trama se fue (el shader los anima de 0 a esta fracción):
        /// la dosis pasa al patrón de su fruta y la insignia baja en uno [P].
        /// </summary>
        public const float RuleMarkGoneAt = 0.55f;

        /// <summary>Destellos de estrella por capa revelada por la regla (ficha de Magic Sort §3.1: 3 o 4).</summary>
        public const int SparklesPerLayer = 4;

        /// <summary>Con la Luciérnaga, parte de la ventana de cada capa en la que su «?» gira hasta quedar de canto [P].</summary>
        public const float FireflyTurnEnd = 0.35f;

        /// <summary>Con la Luciérnaga, lado del icono de fruta que aparece al girar el «?» (u) [P].</summary>
        public const float FireflyFruitU = 0.62f;

        /// <summary>Hueco entre el pie y el canto de arriba de la insignia (dp) [P].</summary>
        public const float BadgeGapDp = 1f;

        private const float BadgeGlyphStepDp = 7f;

        private readonly RectTransform _root;
        private readonly ProceduralArt _art;
        private readonly CuiaView? _view;
        private readonly int _capacity;
        private readonly Image _badge;
        private readonly Image _badgeQuestion;
        private readonly Image _badgeTimes;
        private readonly Image _badgeDigit;
        private readonly Image[] _fruits;
        private readonly Color[] _target;
        private readonly int[] _targetTint;
        private int _mask;
        private bool _inFlight;
        private bool _badgeReady;
        private int _badgeShown = -1;
        private float _u = 24f;
        private LiquidGraphic? _liquid;
        private float _animMs = -1f;
        private float _animDur = 1f;
        private int _animMask;
        private int _animLayers;
        private int _switched;
        private bool _sparklesDone;
        private bool _bubbleDone;
        private RevealStyle _style;

        /// <summary>Crea las marcas de una cuia de capacidad <paramref name="capacity"/> dentro de <paramref name="body"/>.</summary>
        public MurkyMarks(RectTransform body, int capacity, ProceduralArt art)
        {
            _art = art;
            _capacity = Mathf.Clamp(capacity, 0, Cuia.MaxCapacity);
            _view = body.GetComponentInParent<CuiaView>(true); // la vista que la crea: su pie y sus efectos (IBoardFx)
            _root = OverlayParts.NewAnchor("MurkyMarks", body);
            _fruits = new Image[_capacity];
            _target = new Color[_capacity];
            _targetTint = new int[_capacity];
            for (int k = 0; k < _capacity; k++)
            {
                _fruits[k] = OverlayParts.NewImage("MurkyFruit" + k, _root, null, Color.white);
            }

            _badge = OverlayParts.NewImage("MurkyBadge", _root, null, Color.white);
            _badge.type = Image.Type.Sliced;
            _badgeQuestion = OverlayParts.NewImage("MurkyBadgeQ", _badge.rectTransform, null, Color.white);
            _badgeTimes = OverlayParts.NewImage("MurkyBadgeX", _badge.rectTransform, null, Color.white);
            _badgeDigit = OverlayParts.NewImage("MurkyBadgeN", _badge.rectTransform, null, Color.white);
            PlaceBadgeGlyph(_badgeQuestion, -BadgeGlyphStepDp);
            PlaceBadgeGlyph(_badgeTimes, 0f);
            PlaceBadgeGlyph(_badgeDigit, BadgeGlyphStepDp);
            OverlayParts.SetVisible(_badgeQuestion, true);
            OverlayParts.SetVisible(_badgeTimes, true);
            OverlayParts.SetVisible(_badgeDigit, true);
            Layout(_u);
        }

        /// <summary>Ancla de las marcas (dentro del cuerpo).</summary>
        public RectTransform Root => _root;

        /// <summary>Hay una animación en curso: mientras, <c>CuiaView.ShowContent</c> no llama a <see cref="Show"/>.</summary>
        public bool IsAnimating => _animMs >= 0f;

        /// <summary>Capas que se están revelando (bit k = dosis k desde el fondo); 0 sin animación.</summary>
        public int RevealingMask => IsAnimating ? _animMask : 0;

        /// <summary>Avance del revelado en curso (0..1); 1 sin animación.</summary>
        public float RevealProgress => IsAnimating ? Mathf.Clamp01(_animMs / _animDur) : 1f;

        /// <summary>Estilo del revelado en curso (o del último).</summary>
        public RevealStyle Style => _style;

        /// <summary>Dosis con «?» (bit k = dosis k desde el fondo): las que cuenta la insignia, estén o no a la vista.</summary>
        public int MarkMask => _mask;

        /// <summary>Cifra de la insignia «?×n» (0 si no hay ocultas).</summary>
        public int BadgeCount => CountBits(_mask);

        /// <summary>La insignia «?×n» está a la vista (hay ocultas y la cuia no está en vuelo).</summary>
        public bool BadgeVisible => _badge.gameObject.activeSelf;

        /// <summary>Imagen de la insignia (tests y capturas): la píldora; sus hijos son «?», «×» y la cifra.</summary>
        public Image Badge => _badge;

        /// <summary>Glifo de la cifra de la insignia (tests).</summary>
        public Image BadgeDigit => _badgeDigit;

        /// <summary>Destellos que pidió el último revelado por la regla (diagnóstico y tests).</summary>
        public int LastSparkles { get; private set; }

        /// <summary>Burbujas que pidió el último revelado por la regla (diagnóstico y tests).</summary>
        public int LastBubbles { get; private set; }

        /// <summary>
        /// El shader dibuja el «?» de la dosis <paramref name="k"/> (turbia y sin disolver del todo). Lo dibuja también con la
        /// cuia en vuelo: va dentro de su banda.
        /// </summary>
        public bool MarkVisible(int k)
        {
            return _liquid != null && _liquid.MurkyMarkDrawn(k);
        }

        /// <summary>Icono de fruta de la dosis <paramref name="k"/> a la vista (revelado con la Luciérnaga).</summary>
        public bool FruitVisible(int k)
        {
            return (uint)k < (uint)_capacity && _fruits[k].gameObject.activeSelf;
        }

        /// <summary>Icono de fruta de la dosis <paramref name="k"/> (tests y capturas).</summary>
        public Image FruitImage(int k)
        {
            return _fruits[k];
        }

        /// <summary>Duración nominal (ms, sin factor de velocidad) del revelado <paramref name="style"/>.</summary>
        public float StepDurationMs(RevealStyle style)
        {
            return style == RevealStyle.Firefly ? RevealFireflyMs : RevealRuleMs;
        }

        /// <summary>
        /// Genera ya los sprites de la insignia (la carga del nivel o el arranque de la intro): así el primer «?×n» no crea
        /// texturas a mitad de una animación.
        /// </summary>
        public void Prewarm()
        {
            if (_badgeReady)
            {
                return;
            }

            _badgeReady = true;
            _badge.sprite = _art.MurkyBadgePill;
            _badgeQuestion.sprite = _art.MurkyBadgeQuestion;
            _badgeTimes.sprite = _art.MurkyBadgeTimes;
            _badgeShown = -1;
        }

        /// <summary>
        /// Pinta la dosis oculta <paramref name="k"/>: en el shader, pardo opaco <see cref="Palette.Murky"/>, llena, con trama y
        /// «?» (<see cref="LiquidGraphic.MurkyLook.Hidden"/>), sin tinte. Una capa que se está revelando no se repinta (la
        /// anima <see cref="Tick"/>). Devuelve el color pintado, para que la vista lleve la cuenta (color y nivel del líquido).
        /// </summary>
        public Color PaintHidden(LiquidGraphic liquid, int k)
        {
            _liquid = liquid;
            if (IsAnimating && ((_animMask >> k) & 1) != 0)
            {
                return Palette.Murky;
            }

            liquid.SetDose(k, Palette.Murky, 1f, 0, LiquidGraphic.MurkyLook.Hidden);
            if (!IsAnimating && (uint)k < (uint)_capacity)
            {
                SetMask(_mask | (1 << k));
            }

            return Palette.Murky;
        }

        /// <summary>Cuenta en la insignia las dosis ocultas de <paramref name="cuia"/> y ninguna otra. Idempotente.</summary>
        public void Show(Cuia cuia)
        {
            SetMask(cuia.Count > 0 ? cuia.HiddenMask & ((1 << _capacity) - 1) : 0);
        }

        /// <summary>
        /// Arranca el revelado de las capas <paramref name="layerMask"/> de <paramref name="shown"/> (el estado que muestra la
        /// cuia, con el tinte real de cada capa): las pinta turbias en este mismo fotograma y las lleva a su tinte (ver la
        /// clase). Devuelve la duración (ms) ya multiplicada por <paramref name="speedFactor"/>, o 0 si no hay capas que
        /// revelar. Sin asignaciones.
        /// </summary>
        public float PlayReveal(LiquidGraphic liquid, int layerMask, RevealStyle style, Cuia shown, float speedFactor)
        {
            int layers = layerMask & ((1 << Mathf.Min(_capacity, shown.Count)) - 1);
            if (liquid == null || layers == 0)
            {
                return 0f;
            }

            Snap();
            _liquid = liquid;
            _animMask = layers;
            _animLayers = CountBits(layers);
            _switched = 0;
            _sparklesDone = false;
            _bubbleDone = false;
            _style = style;
            _animDur = Mathf.Max(1f, StepDurationMs(style) * Mathf.Max(0.01f, speedFactor));
            _animMs = 0f;
            if (style == RevealStyle.Rule)
            {
                LastSparkles = 0;
                LastBubbles = 0;
            }

            for (int k = 0; k < _capacity; k++)
            {
                if (((layers >> k) & 1) == 0)
                {
                    continue;
                }

                int tint = shown.ColorAt(k);
                bool mud = tint == Cuia.MudColor;
                _target[k] = mud ? Palette.Mud : Palette.Tint(tint);
                _targetTint[k] = mud ? 0 : tint;
                if (style == RevealStyle.Firefly)
                {
                    Image fruit = _fruits[k];
                    fruit.sprite = _art.FruitFlight(mud ? 1 : tint);
                    OverlayParts.Place(fruit, 0f, MarkCenterU(k), FireflyFruitU, FireflyFruitU, _u);
                }
            }

            Prewarm();
            SetMask(_mask | layers);
            ApplyReveal(0f);
            return _animDur;
        }

        /// <summary>Termina en el acto el revelado en curso (las capas con su tinte, sin «?» ni iconos ni efectos nuevos).</summary>
        public void Snap()
        {
            if (!IsAnimating)
            {
                return;
            }

            _sparklesDone = true;
            _bubbleDone = true;
            ApplyReveal(1f);
            Finish();
        }

        /// <summary>Deja de contar en la insignia la dosis <paramref name="k"/> (la intro, antes de que el agua se enturbie).</summary>
        public void HideMark(int k)
        {
            if ((uint)k < (uint)_capacity)
            {
                SetMask(_mask & ~(1 << k));
            }
        }

        /// <summary>Quita la insignia (ninguna dosis contada).</summary>
        public void Clear()
        {
            SetMask(0);
        }

        /// <summary>Coloca la insignia bajo el pie y los iconos de fruta en su banda (u en dp).</summary>
        public void Layout(float u)
        {
            _u = u;
            const float h = ProceduralArt.MurkyBadgeDp;
            RectTransform badge = _badge.rectTransform;
            var pos = new Vector2(0f, -(BadgeGapDp + (0.5f * h)));
            var size = new Vector2(ProceduralArt.MurkyBadgeWidthDp, h);
            if (badge.anchoredPosition != pos)
            {
                badge.anchoredPosition = pos;
            }

            if (badge.sizeDelta != size)
            {
                badge.sizeDelta = size;
            }

            for (int k = 0; k < _capacity; k++)
            {
                OverlayParts.Place(_fruits[k], 0f, MarkCenterU(k), FireflyFruitU, FireflyFruitU, _u);
            }
        }

        /// <summary>Con la cuia en vuelo la insignia y los iconos se ocultan (la trama y el «?» siguen en su banda).</summary>
        public void SetInFlight(bool inFlight)
        {
            _inFlight = inFlight;
            ApplyBadge();
            if (inFlight)
            {
                for (int k = 0; k < _capacity; k++)
                {
                    OverlayParts.SetVisible(_fruits[k], false);
                }
            }
        }

        /// <summary>Avanza el revelado en curso (sin asignaciones).</summary>
        public void Tick(float dtMs)
        {
            if (!IsAnimating)
            {
                return;
            }

            _animMs += dtMs;
            float t = Mathf.Clamp01(_animMs / _animDur);
            ApplyReveal(t);
            if (t >= 1f)
            {
                Finish();
            }
        }

        // Centro del «?» de la dosis k (u desde el pie): a media banda entre sus arcos (la banda se ve del arco de abajo, a
        // media cara superior bajo su nivel, al de arriba).
        private static float MarkCenterU(int k)
        {
            return CuiaGeometry.CavityBottomU + k + 0.5f - (0.5f * CuiaGeometry.TopFaceU);
        }

        // Fotograma t (0..1) del revelado (ver la clase).
        private void ApplyReveal(float t)
        {
            if (_liquid == null)
            {
                return;
            }

            if (_style == RevealStyle.Firefly)
            {
                ApplyFirefly(t);
                return;
            }

            // Por la regla: el color pasa del pardo al tinte entre RuleTintFrom y RuleTintFullAt (ease-in-out) y el shader
            // disuelve encima el «?» y la trama hasta RuleMarkGoneAt; después, la dosis con el patrón de su fruta.
            float dissolve = Mathf.Clamp01(t / RuleMarkGoneAt);
            float e = Ease.InOut(Mathf.Clamp01((t - RuleTintFrom) / (RuleTintFullAt - RuleTintFrom)));
            for (int k = 0; k < _capacity; k++)
            {
                if (((_animMask >> k) & 1) == 0)
                {
                    continue;
                }

                Color c = Color.Lerp(Palette.Murky, _target[k], e);
                if (t >= RuleMarkGoneAt)
                {
                    _liquid.SetDose(k, c, 1f, _targetTint[k]);
                    MarkGone(k);
                }
                else
                {
                    _liquid.SetDose(k, c, 1f, 0, LiquidGraphic.MurkyLook.Dissolve, dissolve);
                }
            }

            if (!_sparklesDone && t >= RuleSparklesAt)
            {
                _sparklesDone = true;
                EmitSparkles();
            }

            if (!_bubbleDone && t >= RuleBubbleAt)
            {
                _bubbleDone = true;
                EmitBubble();
            }
        }

        // Luciérnaga: de arriba abajo. Cada capa empieza 0,4 / n después de la de encima y dura el resto (la última acaba en
        // t = 1). En su ventana, el «?» gira de canto (hasta FireflyTurnEnd), el icono de su fruta gira de vuelta y se apaga al
        // final, y el pardo se aclara hasta el tinte en toda la ventana: la de abajo lo alcanza justo a los 600 ms × factor.
        private void ApplyFirefly(float t)
        {
            int n = Mathf.Max(1, _animLayers);
            float stagger = n > 1 ? 0.4f / n : 0f;
            float window = 1f - (stagger * (n - 1));
            int above = 0;
            for (int k = _capacity - 1; k >= 0; k--)
            {
                if (((_animMask >> k) & 1) == 0)
                {
                    continue;
                }

                float local = Mathf.Clamp01((t - (stagger * above)) / window);
                above++;
                float turn = Mathf.Clamp01(local / FireflyTurnEnd);
                Color c = Color.Lerp(Palette.Murky, _target[k], Ease.InOut(local));
                if (turn >= 1f)
                {
                    _liquid!.SetDose(k, c, 1f, _targetTint[k]);
                    MarkGone(k);
                }
                else
                {
                    _liquid!.SetDose(k, c, 1f, 0, LiquidGraphic.MurkyLook.Turn, turn);
                }

                Image fruit = _fruits[k];
                bool show = !_inFlight && local > FireflyTurnEnd && local < 1f;
                OverlayParts.SetVisible(fruit, show);
                if (show)
                {
                    float flip = Mathf.Sin(0.5f * Mathf.PI * Mathf.Clamp01((local - FireflyTurnEnd) / 0.2f));
                    Vector3 scale = new Vector3(Mathf.Max(0.001f, flip), 1f, 1f);
                    if (fruit.rectTransform.localScale != scale)
                    {
                        fruit.rectTransform.localScale = scale;
                    }

                    OverlayParts.SetColor(fruit, new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((local - 0.8f) / 0.2f)));
                }
            }
        }

        // El «?» de la capa k ya no se ve: la insignia baja en uno (una vez por capa).
        private void MarkGone(int k)
        {
            int bit = 1 << k;
            if ((_switched & bit) != 0)
            {
                return;
            }

            _switched |= bit;
            SetMask(_mask & ~bit);
        }

        private void EmitSparkles()
        {
            IBoardFx? fx = _view != null ? _view.OverlayHost.Fx : null;
            if (fx == null || _view == null)
            {
                return;
            }

            int per = _animLayers > 1 ? SparklesPerLayer - 1 : SparklesPerLayer; // el tope de 8 brillos aditivos a la vez
            Vector2 foot = _view.FootPosition + _view.BodyOffset;
            for (int k = 0; k < _capacity; k++)
            {
                if (((_animMask >> k) & 1) != 0)
                {
                    fx.Sparkles(foot + new Vector2(0f, MarkCenterU(k) * _u), Color.white, per, _u);
                    LastSparkles += per;
                }
            }
        }

        private void EmitBubble()
        {
            IBoardFx? fx = _view != null ? _view.OverlayHost.Fx : null;
            if (fx == null || _view == null)
            {
                return;
            }

            int k = 0;
            for (int i = 0; i < _capacity; i++)
            {
                if (((_animMask >> i) & 1) != 0)
                {
                    k = i; // la de más arriba: la burbuja sale por la superficie
                }
            }

            Vector2 foot = _view.FootPosition + _view.BodyOffset;
            fx.Bubbles(foot + new Vector2(0f, (CuiaGeometry.CavityBottomU + k + 0.15f) * _u), 1, _u);
            LastBubbles++;
        }

        private void Finish()
        {
            int done = _animMask;
            _animMs = -1f;
            _animMask = 0;
            _animLayers = 0;
            for (int k = 0; k < _capacity; k++)
            {
                if (((done >> k) & 1) != 0)
                {
                    OverlayParts.SetVisible(_fruits[k], false);
                    Image fruit = _fruits[k];
                    if (fruit.rectTransform.localScale != Vector3.one)
                    {
                        fruit.rectTransform.localScale = Vector3.one;
                    }

                    OverlayParts.SetColor(fruit, Color.white);
                }
            }

            SetMask(_mask & ~done);
        }

        private void SetMask(int mask)
        {
            _mask = mask;
            ApplyBadge();
        }

        // Insignia «?×n»: a la vista con ocultas y fuera de vuelo; la cifra cambia de sprite (sin asignaciones).
        private void ApplyBadge()
        {
            int n = CountBits(_mask);
            bool on = n > 0 && !_inFlight;
            if (on)
            {
                Prewarm();
                if (n != _badgeShown)
                {
                    _badgeShown = n;
                    _badgeDigit.sprite = _art.MurkyBadgeDigit(n);
                }
            }

            OverlayParts.SetVisible(_badge, on);
        }

        private static void PlaceBadgeGlyph(Image glyph, float xDp)
        {
            RectTransform rt = glyph.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(xDp, 0f);
            rt.sizeDelta = new Vector2(ProceduralArt.MurkyBadgeCellDp, ProceduralArt.MurkyBadgeDp);
        }

        private static int CountBits(int mask)
        {
            int n = 0;
            for (int m = mask; m != 0; m &= m - 1)
            {
                n++;
            }

            return n;
        }
    }
}
