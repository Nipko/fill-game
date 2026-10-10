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
    /// debajo de los 1,5 s de b.2): como en Magic Sort, primero sale el color nuevo (la dosis florece del pardo a su tinte en
    /// OKLab y con ease-out, la misma floración que la intro (D-010; guion §10) y la curva de 02 §4.1, entre
    /// <see cref="RuleTintFrom"/> y <see cref="RuleTintFullAt"/>) y el «?» crece, ondula, se blanquea y se disuelve encima, con
    /// 3-4 destellos blancos de estrella (<see cref="IBoardFx.Sparkles"/>); sube una burbuja (<see cref="IBoardFx.Bubbles"/>;
    /// la «boiada» del pirarucú). Así nunca se ve el pardo sin «?» (07 §3, fila Cacao). Fundido cruzado sin saltos (D-003): la
    /// trama se va mientras sale el tinte (≈ 0,10-0,30), entra el patrón de la fruta cuando ya no hay trama (≈ 0,35 hasta
    /// <see cref="RuleDissolveEndAt"/>, en el shader: <see cref="LiquidGraphic.FruitPatternWeight"/>) y la insignia baja en
    /// uno justo cuando el «?» termina de irse (<see cref="RuleMarkGoneAt"/>), con un pop de la cifra
    /// (<see cref="BadgePopScale"/> en <see cref="BadgePopMs"/>; sin él con Reducir movimiento). La cola lo despacha al origen
    /// cuando ya volvió a su ranura (T07).</para>
    /// <para><b>Luciérnaga</b> (<see cref="RevealStyle.Firefly"/>, 04 §2.5: «su luz baja por el vidrio»; la usa T14): un frente
    /// de luz recorre de arriba abajo las capas que se revelan, de la más alta a la más baja, hasta <see cref="FireflyFrontEnd"/>
    /// del revelado (ease-in-out sinusoidal) y la de abajo termina justo a los <see cref="RevealFireflyMs"/> × factor. Encima
    /// del frente, cada capa florece en OKLab del pardo a su tinte (lo calcula la CPU; el shader solo mezcla en el borde del
    /// frente, <see cref="LiquidGraphic.FireflyLightAt"/>); cuando el frente pasa por su centro, su «?» queda de canto y gira de
    /// vuelta el icono de su fruta (<see cref="ProceduralArt.FruitFlight"/>) sobre un disco marfil con filo de madera
    /// (<see cref="ProceduralArt.MurkyFruitDisc"/>, ≥ 3:1 frente a cualquier tinte), que se apaga al final. Ya sin trama entra el
    /// patrón de su fruta, y la capa sigue en <see cref="LiquidGraphic.MurkyLook.Turn"/> hasta el final de su ventana, cuando
    /// ya es su dosis normal: pasa a pintarse normal sin que cambie nada en pantalla.</para>
    /// <para>Mientras anima (<see cref="IsAnimating"/>), <c>ShowContent</c> no llama a <see cref="Show"/> ni repinta las capas
    /// que se revelan; la cola termina el paso con <see cref="Snap"/> y el <c>ShowContent</c> final. Las imágenes se crean una
    /// vez al construir; los sprites de la insignia, la primera vez que hace falta (<see cref="Prewarm"/>). Sin asignaciones
    /// al animar.</para>
    /// </remarks>
    public sealed class MurkyMarks
    {
        /// <summary>Alto del «?» marfil que pinta el shader en reposo, sin el halo (07 §4.3: 0,5 u).</summary>
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
        /// Por la regla, fracción del revelado en la que el pardo empieza a florecer hacia el tinte (120 ms) [P]: antes de que
        /// el «?» empiece a apagarse (≈ 0,19), para que se disuelva sobre el color nuevo (ficha de Magic Sort §3.1).
        /// </summary>
        public const float RuleTintFrom = 0.10f;

        /// <summary>
        /// Por la regla, fracción en la que la dosis ya tiene su tinte (480 ms; floración OKLab con ease-out desde
        /// <see cref="RuleTintFrom"/>, 02 §4.1) [P]: a mitad del apagado del «?» (≈ 0,33) va ya al 95 % y el «?» termina de
        /// irse (≈ 0,47) sobre el tinte, así que nunca queda el pardo sin «?» (07 §3).
        /// </summary>
        public const float RuleTintFullAt = 0.40f;

        /// <summary>
        /// Por la regla, fracción en la que termina el Dissolve del shader (su avance va de 0 a 1 hasta aquí) [P]: la trama
        /// se fue (avance <see cref="LiquidGraphic.HatchFadeFrom"/>-<see cref="LiquidGraphic.HatchFadeTo"/>, t ≈ 0,10-0,30), el
        /// «?» se disolvió y el patrón de la fruta ya entró entero (<see cref="LiquidGraphic.FruitPatternFrom"/>-1,
        /// t ≈ 0,35-0,55), así que la dosis pasa a pintarse normal sin que cambie nada en pantalla.
        /// </summary>
        public const float RuleDissolveEndAt = 0.55f;

        /// <summary>
        /// Por la regla, fracción en la que el «?» termina de irse (<see cref="LiquidGraphic.MarkFadeTo"/> del Dissolve,
        /// ≈ 0,47): la insignia baja en uno en ese mismo fotograma, con el pop de la cifra [P].
        /// </summary>
        public const float RuleMarkGoneAt = RuleDissolveEndAt * LiquidGraphic.MarkFadeTo;

        /// <summary>Pop de la cifra de la insignia al bajar: dura 160 ms (no se escala con el factor de velocidad) [P].</summary>
        public const float BadgePopMs = 160f;

        /// <summary>Escala máxima del pop de la cifra (1 → 1,2 → 1, ease-out) [P].</summary>
        public const float BadgePopScale = 1.2f;

        /// <summary>Destellos de estrella por capa revelada por la regla (ficha de Magic Sort §3.1: 3 o 4).</summary>
        public const int SparklesPerLayer = 4;

        /// <summary>
        /// Con la Luciérnaga, fracción del revelado en la que el frente de luz sale por abajo de la capa más baja [P]: el resto
        /// es la cola de esa capa (entra el patrón de su fruta y su icono se apaga).
        /// </summary>
        public const float FireflyFrontEnd = 0.65f;

        /// <summary>
        /// Con la Luciérnaga, profundidad del frente (fracción de la capa) a la que la capa ya floreció entera hasta su tinte
        /// (OKLab, ease-out) [P]: desde ahí el frente baja nítido entre el tinte y el pardo.
        /// </summary>
        public const float FireflyBloomDepth = 0.4f;

        /// <summary>
        /// Con la Luciérnaga, profundidad del frente a la que el icono de la fruta ya giró de vuelta del todo (empieza con el «?»
        /// de canto, <see cref="LiquidGraphic.FireflyTurnTo"/>) [P].
        /// </summary>
        public const float FireflyFlipTo = 0.75f;

        /// <summary>Con la Luciérnaga, avance de la capa desde el que su icono se apaga (hasta el final de su ventana) [P].</summary>
        public const float FireflyFruitFadeFrom = 0.85f;

        /// <summary>Con la Luciérnaga, lado del icono de fruta que aparece al girar el «?» (u) [P].</summary>
        public const float FireflyFruitU = 0.62f;

        /// <summary>Con la Luciérnaga, diámetro del disco marfil bajo el icono (u; ≈ 18 dp con la cuia de 48 dp) [P].</summary>
        public const float FireflyDiscU = 0.80f;

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
        private readonly Image[] _discs;
        private readonly Color[] _target;
        private readonly int[] _targetTint;
        private readonly float[] _frontOut;
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
        private bool _snapping;
        private float _popMs = -1f;
        private RevealStyle _style;

        /// <summary>Crea las marcas de una cuia de capacidad <paramref name="capacity"/> dentro de <paramref name="body"/>.</summary>
        public MurkyMarks(RectTransform body, int capacity, ProceduralArt art)
        {
            _art = art;
            _capacity = Mathf.Clamp(capacity, 0, Cuia.MaxCapacity);
            _view = body.GetComponentInParent<CuiaView>(true); // la vista que la crea: su pie y sus efectos (IBoardFx)
            _root = OverlayParts.NewAnchor("MurkyMarks", body);
            _fruits = new Image[_capacity];
            _discs = new Image[_capacity];
            _target = new Color[_capacity];
            _targetTint = new int[_capacity];
            _frontOut = new float[_capacity];
            for (int k = 0; k < _capacity; k++)
            {
                _discs[k] = OverlayParts.NewImage("MurkyFruitDisc" + k, _root, null, Color.white); // los discos antes: un lote, y los iconos encima
            }

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

        /// <summary>La cifra de la insignia está haciendo su pop (<see cref="BadgePopMs"/> tras bajar la cuenta).</summary>
        public bool BadgePopping => _popMs >= 0f;

        /// <summary>Destellos que pidió el último revelado por la regla (diagnóstico y tests).</summary>
        public int LastSparkles { get; private set; }

        /// <summary>Burbujas que pidió el último revelado por la regla (diagnóstico y tests).</summary>
        public int LastBubbles { get; private set; }

        /// <summary>
        /// El shader dibuja el «?» de la dosis <paramref name="k"/> (turbia, sin disolver del todo y con hueco en su banda). Lo
        /// dibuja también con la cuia en vuelo: va dentro de su banda (y se apaga si la banda se queda sin hueco para él).
        /// </summary>
        public bool MarkVisible(int k)
        {
            return _liquid != null && _liquid.MurkyMarkDrawn(k);
        }

        /// <summary>Icono de fruta de la dosis <paramref name="k"/> a la vista (revelado con la Luciérnaga), con su disco.</summary>
        public bool FruitVisible(int k)
        {
            return (uint)k < (uint)_capacity && _fruits[k].gameObject.activeSelf;
        }

        /// <summary>Icono de fruta de la dosis <paramref name="k"/> (tests y capturas).</summary>
        public Image FruitImage(int k)
        {
            return _fruits[k];
        }

        /// <summary>Disco marfil bajo el icono de fruta de la dosis <paramref name="k"/> (tests y capturas).</summary>
        public Image FruitDisc(int k)
        {
            return _discs[k];
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
        /// revelar. Sin asignaciones (salvo la primera vez que se piden los sprites de las frutas o del disco).
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

            int above = 0;
            for (int k = _capacity - 1; k >= 0; k--)
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
                    // El frente sale por abajo de esta capa (la above-ésima desde arriba, contándola) cuando su avance sinusoidal
                    // llega a above / n: ahí empieza su cola.
                    above++;
                    float reached = (float)above / _animLayers;
                    _frontOut[k] = FireflyFrontEnd * (Mathf.Acos(1f - (2f * reached)) / Mathf.PI);
                    Image fruit = _fruits[k];
                    fruit.sprite = _art.FruitFlight(mud ? 1 : tint);
                    Image disc = _discs[k];
                    disc.sprite = _art.MurkyFruitDisc;
                    OverlayParts.Place(disc, 0f, MarkCenterU(k), FireflyDiscU, FireflyDiscU, _u);
                    OverlayParts.Place(fruit, 0f, MarkCenterU(k), FireflyFruitU, FireflyFruitU, _u);
                }
            }

            Prewarm();
            SetMask(_mask | layers);
            ApplyReveal(0f);
            return _animDur;
        }

        /// <summary>
        /// Termina en el acto el revelado en curso (las capas con su tinte, sin «?» ni iconos ni efectos nuevos: ni destellos,
        /// ni burbuja, ni pop) y el pop de la cifra que quede.
        /// </summary>
        public void Snap()
        {
            StopPop();
            if (!IsAnimating)
            {
                return;
            }

            _sparklesDone = true;
            _bubbleDone = true;
            _snapping = true;
            ApplyReveal(1f);
            Finish();
            _snapping = false;
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

        /// <summary>Coloca la insignia bajo el pie y los iconos de fruta (con su disco) en su banda (u en dp).</summary>
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
                OverlayParts.Place(_discs[k], 0f, MarkCenterU(k), FireflyDiscU, FireflyDiscU, _u);
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
                StopPop();
                for (int k = 0; k < _capacity; k++)
                {
                    OverlayParts.SetVisible(_fruits[k], false);
                    OverlayParts.SetVisible(_discs[k], false);
                }
            }
        }

        /// <summary>Avanza el pop de la cifra y el revelado en curso (sin asignaciones).</summary>
        public void Tick(float dtMs)
        {
            TickPop(dtMs);
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

        // Centro del «?» de la dosis k en reposo (u desde el pie): a media banda entre sus arcos (la banda se ve del arco de
        // abajo, a media cara superior bajo su nivel, al de arriba).
        private static float MarkCenterU(int k)
        {
            return CuiaGeometry.CavityBottomU + k + 0.5f - (0.5f * CuiaGeometry.TopFaceU);
        }

        // Del pardo al tinte de la capa k con la floración OKLab (la de la intro, D-010), sin redondeos en los extremos.
        private Color Bloom(int k, float amount)
        {
            if (amount <= 0f)
            {
                return Palette.Murky;
            }

            return amount >= 1f ? _target[k] : ColorMath.OkLabLerp(Palette.Murky, _target[k], amount);
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

            // Por la regla: el color florece del pardo al tinte entre RuleTintFrom y RuleTintFullAt (OKLab, ease-out) y el
            // shader hace el fundido cruzado hasta RuleDissolveEndAt (la trama se va, el «?» se disuelve encima y entra el patrón
            // de la fruta, con su franja y su menisco); después, la dosis normal, igual que el último fotograma del Dissolve. La
            // insignia baja cuando el «?» termina de irse (RuleMarkGoneAt).
            float dissolve = Mathf.Clamp01(t / RuleDissolveEndAt);
            float e = Ease.Out(Mathf.Clamp01((t - RuleTintFrom) / (RuleTintFullAt - RuleTintFrom)));
            for (int k = 0; k < _capacity; k++)
            {
                if (((_animMask >> k) & 1) == 0)
                {
                    continue;
                }

                Color c = Bloom(k, e);
                if (t >= RuleDissolveEndAt)
                {
                    _liquid.SetDose(k, c, 1f, _targetTint[k]);
                }
                else
                {
                    _liquid.SetDose(k, c, 1f, _targetTint[k], LiquidGraphic.MurkyLook.Dissolve, dissolve);
                }

                if (t >= RuleMarkGoneAt)
                {
                    MarkGone(k);
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

        // Luciérnaga: el frente recorre las n capas que se revelan, de la más alta a la más baja, hasta FireflyFrontEnd
        // (ease-in-out sinusoidal). En la capa i (desde arriba), su avance p sube hasta FireflyFrontSpan mientras el frente la
        // cruza y, en su cola (1 − FireflyFrontEnd del revelado desde que el frente sale por abajo), hasta 1; la de abajo acaba
        // en t = 1. La capa florece en OKLab hasta su tinte mientras el frente baja (entera en FireflyBloomDepth), su «?» gira de
        // canto cuando el frente pasa por su centro y el icono de su fruta, sobre su disco, gira de vuelta y se apaga al final.
        // Sigue en Turn hasta p = 1 (todo entró: el patrón y la franja de la fruta) y entonces pasa a la dosis normal.
        private void ApplyFirefly(float t)
        {
            int n = Mathf.Max(1, _animLayers);
            float front = n * Ease.InOutSine(Mathf.Clamp01(t / FireflyFrontEnd));
            float tail = Mathf.Max(1e-4f, 1f - FireflyFrontEnd);
            int i = 0;
            for (int k = _capacity - 1; k >= 0; k--)
            {
                if (((_animMask >> k) & 1) == 0)
                {
                    continue;
                }

                float pass = Mathf.Clamp01(front - i);
                i++;
                float rest = t > _frontOut[k] ? Mathf.Clamp01((t - _frontOut[k]) / tail) : 0f;
                float p = t >= 1f ? 1f : (LiquidGraphic.FireflyFrontSpan * pass) + ((1f - LiquidGraphic.FireflyFrontSpan) * rest);
                float depth = LiquidGraphic.FireflyFrontDepth(p);
                if (p >= 1f)
                {
                    _liquid!.SetDose(k, _target[k], 1f, _targetTint[k]);
                    MarkGone(k);
                }
                else
                {
                    Color c = Bloom(k, Ease.Out(Mathf.Clamp01(depth / FireflyBloomDepth)));
                    _liquid!.SetDose(k, c, 1f, _targetTint[k], LiquidGraphic.MurkyLook.Turn, p);
                    if (LiquidGraphic.MurkyMarkOpacity(LiquidGraphic.MurkyLook.Turn, p) <= 0f)
                    {
                        MarkGone(k); // el «?» ya está de canto: la insignia baja
                    }
                }

                float flip = Mathf.Clamp01((depth - LiquidGraphic.FireflyTurnTo) / (FireflyFlipTo - LiquidGraphic.FireflyTurnTo));
                bool show = !_inFlight && flip > 0f && p < 1f;
                Image fruit = _fruits[k];
                Image disc = _discs[k];
                OverlayParts.SetVisible(fruit, show);
                OverlayParts.SetVisible(disc, show);
                if (show)
                {
                    var scale = new Vector3(Mathf.Max(0.001f, Mathf.Sin(0.5f * Mathf.PI * flip)), 1f, 1f);
                    if (fruit.rectTransform.localScale != scale)
                    {
                        fruit.rectTransform.localScale = scale;
                        disc.rectTransform.localScale = scale;
                    }

                    var tone = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((p - FireflyFruitFadeFrom) / (1f - FireflyFruitFadeFrom)));
                    OverlayParts.SetColor(fruit, tone);
                    OverlayParts.SetColor(disc, tone);
                }
            }
        }

        // El «?» de la capa k ya no se ve: la insignia baja en uno (una vez por capa) con el pop de la cifra, salvo al saltar
        // (Snap).
        private void MarkGone(int k)
        {
            int bit = 1 << k;
            if ((_switched & bit) != 0)
            {
                return;
            }

            _switched |= bit;
            SetMask(_mask & ~bit);
            if (!_snapping)
            {
                StartPop();
            }
        }

        // Pop de la cifra: 1 → BadgePopScale → 1 en BadgePopMs (1 + 0,2 · sen(π · ease-out), pico hacia los 47 ms y llegada
        // suave). Sin él con Reducir movimiento, en vuelo o sin insignia (la cuenta llegó a 0).
        private void StartPop()
        {
            IBoardFx? fx = _view != null ? _view.OverlayHost.Fx : null;
            if (_inFlight || (fx != null && fx.ReduceMotion) || !_badge.gameObject.activeSelf)
            {
                return;
            }

            _popMs = 0f;
            SetDigitScale(1f);
        }

        private void TickPop(float dtMs)
        {
            if (_popMs < 0f)
            {
                return;
            }

            _popMs += dtMs;
            if (_popMs >= BadgePopMs)
            {
                StopPop();
                return;
            }

            float p = _popMs / BadgePopMs;
            SetDigitScale(1f + ((BadgePopScale - 1f) * Mathf.Sin(Mathf.PI * Ease.Out(p))));
        }

        private void StopPop()
        {
            _popMs = -1f;
            SetDigitScale(1f);
        }

        private void SetDigitScale(float s)
        {
            var scale = new Vector3(s, s, 1f);
            RectTransform rt = _badgeDigit.rectTransform;
            if (rt.localScale != scale)
            {
                rt.localScale = scale;
            }
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
                    Image fruit = _fruits[k];
                    Image disc = _discs[k];
                    OverlayParts.SetVisible(fruit, false);
                    OverlayParts.SetVisible(disc, false);
                    if (fruit.rectTransform.localScale != Vector3.one)
                    {
                        fruit.rectTransform.localScale = Vector3.one;
                        disc.rectTransform.localScale = Vector3.one;
                    }

                    OverlayParts.SetColor(fruit, Color.white);
                    OverlayParts.SetColor(disc, Color.white);
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
            else if (_popMs >= 0f)
            {
                StopPop();
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
