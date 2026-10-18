#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M13 Tinaja (D-019; 02 §9.1 M13; 07 §4.3 y §4.5; propuesta de la Tinaja, b.2 «Qué se ve» y «Aspecto», y c.6; tarea TJ2)
    /// [P]: la vasija de barro opaca que tapa el vidrio entero, con la boca a la vista (el tinte y el icono de la fruta de arriba,
    /// R7), el chip «×n» en el hombro con las dosis que quedan dentro (contando la boca), las capas ya reveladas como bandas de luz
    /// (el tinte entero con un filo de luz) a su altura con su icono, y el jirau bajo las dos ranuras (la de la tinaja y la de su cuia). Vacía, boca abajo sobre el
    /// jirau, sin boca ni chip. La vasija, el jirau y su aspecto son contenido cultural: pendiente del asesor cultural (D-019;
    /// 07 §4.3; encargo de arte 3.4).
    /// </summary>
    /// <remarks>
    /// <para><b>Nunca agua turbia</b> (contrato 27; 08 §5.2): bajo la vasija, la vista pinta las ocultas con el barro
    /// (<see cref="Palette.TinajaClay"/>), sin tinte, sin el pardo #8A7B6A y sin «?» (<c>CuiaView.ShowContent</c>), y
    /// <see cref="MurkyMarks"/> no pone la insignia «?×n» ni anima su revelado. Lo oculto no se ve por ningún lado: ni su color,
    /// ni su orden, ni qué frutas guarda (Q-Ti5: <c>ui_tinaja_show_contents</c> = false; no hay vista de contenidos).</para>
    /// <para><b>Arte</b> (<see cref="MapArtRule"/>): el cuerpo y el jirau son arte provisional y solo salen con
    /// <see cref="MapArtRule.ShowProvisional"/> (Editor y desarrollo) hasta que lleguen los PNG del artista. La boca (con su
    /// brillo y su icono) y las bandas están hechas para la silueta de la vasija: salen solo con ella. Sin el cuerpo (release
    /// hasta el arte) se ve el vidrio, con la cima y las capas reveladas en su líquido y las ocultas del barro (<c>CuiaView</c>),
    /// y solo el chip «×n», en el hombro del vidrio (<see cref="GlassChipCenterU"/>). Mientras el cuerpo está a la vista, la
    /// vista oculta el vidrio de la cuia (<c>CuiaView</c>): la vasija no se lee metida en una botella, tampoco boca abajo.</para>
    /// <para><b>Su cuia</b> (<see cref="CucharonOverlay"/>) va en la ranura contigua de la misma fila (02 §5; <c>BoardLayout</c>).
    /// El jirau lo dibuja la tinaja, que nunca pierde su tipo, porque es estado permanente (07 §4.3: «no un resaltado», D-002) y
    /// sigue bajo las dos ranuras cuando su cuia baja (la suelta), atado solo en las puntas: nada de cordón, liana ni nudo entre
    /// las dos piezas (b.2 «Aspecto»; encargo de arte, sección 3). La pareja es la del tablero inicial del nivel, que pasa la
    /// pantalla (<see cref="AssignPartner"/>; <c>CuiaView.SetTinajaPartner</c>), así que vale también al reanudar tras la suelta.
    /// Sin ella (bancos de prueba), se reconoce por la cuia de tipo <c>Cucharon</c> a su lado y se recuerda; si la tinaja ya llega
    /// vacía, por la Cuia chica de 2 de al lado, a la derecha antes que a la izquierda [P].</para>
    /// <para><b>Nada cambia al seleccionar</b> (D-002): todo cuelga del estado. La elevación de la tinaja (8 dp con un balanceo
    /// de 2°, 07 §4.3) la hace <c>CuiaView</c>. Los gestos son de la tarea TJ3: sacar lo anima <c>BoardAnimator</c> (se mueve su
    /// cuia), y el overlay ejecuta sus dos pasos, el revelado en la boca (<see cref="MechanicStepKind.Reveal"/>, etapa a) y la
    /// vasija que se tumba en la suelta (<see cref="MechanicStepKind.Upend"/>, etapa f, con la espera boca arriba de
    /// <see cref="HoldUpright"/>), y la intro de D-010 (<see cref="BeginIntro"/>: la fruta solo en la boca y el chip en el primer
    /// impacto).</para>
    /// <para>Sin asignaciones en <see cref="Show"/>, <see cref="Tick"/> ni <see cref="OnBoardLayout"/> (08 §5.4): los iconos de
    /// los 14 tintes y las cifras del chip se generan en el primer <see cref="Show"/> (al cargar el nivel), así que ni una
    /// partida reanudada con la tinaja vacía a la que Marea devuelve dosis genera texturas a mitad de un paso.</para>
    /// </remarks>
    public sealed class TinajaOverlay : CuiaOverlay
    {
        /// <summary>Bandas como mucho: las capas bajo la boca de una tinaja llena (capacidad 5).</summary>
        public const int MaxBands = 4;

        /// <summary>
        /// Centro del chip «×n» (u desde el pie) [P]: en el hombro, entre la banda más alta posible (la capa 3, hasta 4,41 u) y
        /// el labio de la vasija (desde 5,70 u; el agua de la boca, desde 5,77 u).
        /// </summary>
        public const float ChipCenterU = 5.24f;

        /// <summary>
        /// Lo alto del jirau sobre la línea de pie (u) [P]: 0,02 u, justo bajo el pie, así las dos piezas se asientan sobre su vara
        /// de arriba (se dibujan delante) en lugar de quedar detrás de ella.
        /// </summary>
        public const float JirauTopAboveFootU = 0.02f;

        /// <summary>
        /// Lo que el jirau baja de la línea de pie (dp) [P]: 3 dp, el fondo de la cara superior de la repisa (que va de pie − 3 a
        /// pie + 3 en dp de AF.UI, guía visual v2, 4.5). Así sus dos varas quedan enteras sobre la cara superior y ninguna baja de
        /// su canto (el bisel y el frente de la repisa siguen a la vista): un jirau sobre la repisa, no una esterilla en su canto.
        /// </summary>
        public const float JirauBelowFootDp = 3f;

        /// <summary>
        /// Lo que el jirau sale por el lado de la tinaja, desde su eje (u) [P]: bajo su pie y algo más, sin llegar al canto de la
        /// ranura (la última de una fila llena toca el marco del panel).
        /// </summary>
        public const float JirauTinajaExtentU = 1.15f;

        /// <summary>Lo que el jirau sale por el lado de su cuia, desde su eje (u) [P]: hasta el poste, que se apoya en él.</summary>
        public const float JirauCuiaExtentU = ProceduralArt.TinajaPostXU + 0.14f;

        /// <summary>
        /// Luz del filo de una banda: marfil #FAFAF7 al 35 % en sus franjas de arriba y abajo (≈ 1,5 dp con la cuia de 48 dp)
        /// [P]. El relleno es el HEX del tinte sin aclarar y opaco: aclararlo hacia el blanco lavaba los tintes oscuros (el
        /// jenipapo salía gris lavanda, el cacao cerca del pardo de Aguas turbias, vetado en la tinaja por 07 §4.3, y el Agua del
        /// Río Negro se perdía en el barro), y el color de la banda tiene que emparejarse con el de su fruta.
        /// </summary>
        public static readonly Color BandEdgeLight = new Color(0xFA / 255f, 0xFA / 255f, 0xF7 / 255f, 0.35f);

        /// <summary>Oscurecido del contorno de 1 dp de una banda (sobre su tinte) [P].</summary>
        public const float BandRimDarken = 0.35f;

        /// <summary>
        /// ΔE00 mínimo entre el tinte de una banda y el barro que la rodea (<see cref="BandBodyTones"/>): por debajo, la banda
        /// lleva un contorno de 1 dp con el tinte oscurecido (<see cref="BandNeedsRim"/>; hoy, solo el Agua del Río Negro) [P].
        /// </summary>
        public const float BandRimMinDeltaE = 12f;

        /// <summary>
        /// Tonos del barro alrededor de una banda: el barro, su luz, su sombra y la sombra del canto de la panza, donde acaba la
        /// banda (oscurecida un 10 %, <c>ProceduralArt.BuildTinajaBody</c>).
        /// </summary>
        public static readonly Color[] BandBodyTones =
        {
            Palette.TinajaClay, Palette.TinajaClayLight, Palette.TinajaClayShade, ColorMath.Darken(Palette.TinajaClayShade, 0.10f),
        };

        /// <summary>Revelado en la boca (TJ3; b.2 «Animación y sonido»: ≤ 400 ms): duración nominal del paso Reveal.</summary>
        public const float RevealStepMs = 400f;

        /// <summary>Revelado: la boca queda oscura 100 ms (b.2).</summary>
        public const float RevealDarkMs = 100f;

        /// <summary>Revelado: la fruta nueva florece en 0,10 s, en OKLab (b.2; como D-010).</summary>
        public const float RevealBloomMs = 100f;

        /// <summary>Revelado: el rebote del chip al bajar (×n+1 → ×n) dura 200 ms y sube la escala un 25 % [P].</summary>
        public const float ChipBounceMs = 200f;

        /// <summary>Revelado: lo que sube la escala del chip en su rebote [P].</summary>
        public const float ChipBounce = 0.25f;

        /// <summary>Revelado: el anillo de onda crece de 0,6 a 1,5 veces la boca mientras se apaga, sin burbujas [P].</summary>
        public const float RingStartScale = 0.6f;

        /// <summary>Revelado: escala final del anillo de onda [P].</summary>
        public const float RingEndScale = 1.5f;

        /// <summary>Color del anillo de onda: marfil al 60 % [P].</summary>
        public static readonly Color RingColor = new Color(0xFA / 255f, 0xFA / 255f, 0xF7 / 255f, 0.6f);

        /// <summary>Suelta: la boca enseña el fondo seco mientras su cuia baja del jirau, 300 ms (b.2).</summary>
        public const float UpendHoldMs = 300f;

        /// <summary>Suelta: la tinaja se tumba boca abajo sobre el jirau en 500 ms (b.2).</summary>
        public const float UpendMs = 500f;

        /// <summary>
        /// Suelta: al voltearse, la vasija se ladea 8° hacia fuera de su cuia a mitad del giro, con el pivote en su canto de
        /// abajo de ese lado, sobre el jirau (no se levanta) [P].
        /// </summary>
        public const float UpendTiltDeg = 8f;

        /// <summary>Suelta: el vuelco termina con un asiento de 2° que va y vuelve [P] (revisión de TJ3).</summary>
        public const float UpendSettleDeg = 2f;

        /// <summary>Suelta: el asiento dura los últimos 80 ms de <see cref="UpendMs"/> [P].</summary>
        public const float UpendSettleMs = 80f;

        /// <summary>
        /// Suelta: a mitad del vuelco, el extremo que se aleja enseña su cara (la boca seca y, pasada la mitad, la base) como una
        /// elipse de alto 0,25 × su ancho × |sen θ|, para que se lea un sólido que da la vuelta y no una vasija que se aplasta [P].
        /// </summary>
        public const float UpendCapAspect = 0.25f;

        /// <summary>Suelta: la cara de la vasija se oscurece hasta un 18 % de canto (según 1 − |cos θ|) [P].</summary>
        public const float UpendShadeMax = 0.18f;

        /// <summary>
        /// Suelta: color de la tapa de la base de la vasija, #B88A66 [P] (revisión de TJ3): la cara que sube al voltearse y la
        /// tapa elíptica de arriba boca abajo (<c>ProceduralArt.TinajaBaseCap</c>, con su borde de 1 dp un 15 % más oscuro).
        /// </summary>
        public static readonly Color BaseCapColor = new Color32(0xB8, 0x8A, 0x66, 0xFF);

        /// <summary>
        /// Fondo seco de la boca (la tinaja vacía que espera su vuelco y la boca de la intro antes del chorrito; TJ3) [P]: un
        /// disco de barro #6B4A33 con el centro más claro (<see cref="DryMouthCenter"/>), mate y sin brillo, distinto de la boca
        /// oscura del revelado (<c>Palette.WoodDark</c>, #36261C) y del agua.
        /// </summary>
        public static readonly Color DryMouthRim = new Color32(0x6B, 0x4A, 0x33, 0xFF);

        /// <summary>Centro más claro del fondo seco de la boca (#86603F) [P].</summary>
        public static readonly Color DryMouthCenter = new Color32(0x86, 0x60, 0x3F, 0xFF);

        /// <summary>
        /// La boca pasa al fondo seco en 150 ms cuando la última sacada la deja vacía (sin anillo de onda: no hay dosis nueva)
        /// [P].
        /// </summary>
        public const float DryFadeMs = 150f;

        /// <summary>Gotas de la vuelta de su cuia al sacar que caben a la vez (<see cref="Drip"/>).</summary>
        public const int MaxDrips = 3;

        /// <summary>
        /// Sacar: cada gota cae hasta el centro de la boca en 80 ms (02 §4.1; b.2) [P]: las tres caen en la boca oscura del
        /// revelado, antes de que florezca la fruta nueva (revisión de TJ3).
        /// </summary>
        public const float DripFallMs = 80f;

        /// <summary>Sacar: al caer, cada gota se apaga (alfa de 1 a 0) en 80 ms en el agua de la boca [P] (revisión de TJ3).</summary>
        public const float DripFadeMs = 80f;

        /// <summary>Sacar: gravedad de las gotas (u/s²) [P]: con ella y <see cref="DripFallMs"/>, la gota acelera al caer.</summary>
        public const float DripGravityU = 60f;

        /// <summary>Sacar: diámetro de una gota (dp), ≈ 18 px a 1080 [P] (revisión de TJ3).</summary>
        public const float DripDiameterDp = 6f;

        /// <summary>Sacar: brillo blanco de la gota (dp), arriba a la izquierda [P] (revisión de TJ3).</summary>
        public const float DripGlintDp = 1.5f;

        /// <summary>Sacar: la gota lleva el tinte de lo sacado aclarado un 25 % en la luminosidad OKLab [P] (revisión de TJ3).</summary>
        public const float DripLightness = 1.25f;

        /// <summary>
        /// Sacar: la gota cae al centro de la cavidad, 0,1 u bajo el centro del agua de la boca (u) [P]: dentro de la boca, tras
        /// el labio de delante, nunca en su canto (revisión de TJ3).
        /// </summary>
        public const float DripLandBelowU = 0.1f;

        /// <summary>Reducir movimiento: fundidos de 0,25 s (b.2, «Reducir movimiento»).</summary>
        public const float ReducedFadeMs = 250f;

        /// <summary>
        /// La Luciérnaga en la tinaja (TJ4; 04 §2.5; propuesta de la Tinaja, b.2 «Animación y sonido»: «entra por la boca; la
        /// barriga se enciende por dentro y muestra sus capas como bandas», 0,6 s): lo mismo que el revelado de las turbias
        /// (<see cref="MurkyMarks.RevealFireflyMs"/>), sin el pardo ni el «?».
        /// </summary>
        public const float FireflyMs = MurkyMarks.RevealFireflyMs;

        /// <summary>La luz de dentro sube en estos ms al posarse la luciérnaga [P].</summary>
        public const float FireflyGlowInMs = 150f;

        /// <summary>Y se apaga en los últimos ms del revelado [P].</summary>
        public const float FireflyGlowOutMs = 200f;

        /// <summary>Opacidad máxima de la luz de dentro sobre el barro [P].</summary>
        public const float FireflyGlowAlpha = 0.55f;

        /// <summary>La luz baja desde la boca: la banda de arriba empieza a encenderse aquí… [P]</summary>
        public const float FireflyFrontStartMs = 100f;

        /// <summary>…y la de más abajo, aquí; cada banda se enciende en <see cref="FireflyBandFadeMs"/> [P].</summary>
        public const float FireflyFrontEndMs = 420f;

        /// <summary>Fundido de cada banda al pasar la luz [P].</summary>
        public const float FireflyBandFadeMs = 120f;

        // Qué tintes llevan contorno en su banda (índice = tinte): se calcula una vez, sin asignaciones en Show.
        private static readonly bool[] s_bandRim = BuildBandRims();

        private const float ChipGlyphStepDp = ProceduralArt.MurkyBadgeCellDp * ProceduralArt.TinajaChipGlyphScale;

        private readonly Image[] _bands = new Image[MaxBands];
        private readonly Image[] _bandLights = new Image[MaxBands];
        private readonly Image[] _bandRims = new Image[MaxBands];
        private readonly Image[] _bandGlyphs = new Image[MaxBands];
        private RectTransform _layerRoot = null!;
        private Image _jirau = null!;
        private Image _body = null!;
        private Image _mouth = null!;
        private Image _mouthGlyph = null!;
        private Image _mouthGloss = null!;
        private Image _chip = null!;
        private Image _chipTimes = null!;
        private Image _chipDigit = null!;
        private float _u = 24f;
        private int _count = -1;
        private int _mouthTint = -1;
        private int _bandMask;
        private int _partner = -1;
        private bool _partnerSure;
        private bool _partnerAssigned;
        private bool _artShown;
        private bool _upsideDown;
        private int _layoutVersion = -1;
        private bool _prewarmed;
        private Vector2 _jirauFoot;

        // TJ3: último estado mostrado, la espera boca arriba, el paso en curso y la intro.
        private Image _ring = null!;
        private Cuia _last;
        private bool _lastSealed;
        private bool _hasLast;
        private bool _holdUpright;
        private bool _stepping;
        private MechanicStepKind _stepKind;
        private float _stepT;
        private float _stepSpeed = 1f;
        private bool _stepReduce;
        private int _stepTint;
        private int _stepOldCount;
        private int _stepNewCount;
        private float _upendSide = 1f;
        private bool _intro;
        private bool _introMarks;
        private Color _introMouth;
        private int _introTint;

        // Revisión de TJ3: fondo seco (con su paso de 150 ms), las gotas de la vuelta de su cuia y la cara del vuelco.
        private Image _dryCenter = null!;
        private Image _upendCap = null!;
        private bool _dryShown;
        private float _dryFadeT = -1f;
        private Color _dryFrom;
        private readonly Image[] _drips = new Image[MaxDrips];
        private readonly float[] _dripAge = { -1f, -1f, -1f };
        private readonly Vector2[] _dripFrom = new Vector2[MaxDrips];
        private readonly float[] _dripVy = new float[MaxDrips];
        private readonly float[] _dripSizeU = new float[MaxDrips];
        private readonly Color[] _dripColor = new Color[MaxDrips];
        private readonly Image[] _dripGlints = new Image[MaxDrips];

        // Revisión de TJ3 (suelta): la tapa de la base boca abajo y la copia boca abajo del fundido de Reducir movimiento.
        private Image _baseCap = null!;
        private Image _bodyFlip = null!;
        private Sprite _bodyOpen = null!;
        private Sprite _bodyClosed = null!;

        // TJ4: la Luciérnaga (la luz de dentro y las capas que se encienden de arriba abajo).
        private Image _glow = null!;
        private bool _firefly;
        private float _fireflyT;
        private float _fireflySpeed = 1f;
        private bool _fireflyReduce;
        private int _fireflyMask;
        private int _fireflyLayers;
        private Cuia _fireflyCuia;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Tinaja;

        /// <summary>El cuerpo de barro (arte provisional, con <see cref="MapArtRule.ShowProvisional"/>) está a la vista.</summary>
        public bool BodyVisible => _body.gameObject.activeSelf;

        /// <summary>La vasija está boca abajo (vacía, sobre el jirau).</summary>
        public bool UpsideDown => _upsideDown;

        /// <summary>La boca está a la vista (con dosis dentro).</summary>
        public bool MouthVisible => _mouth.gameObject.activeSelf;

        /// <summary>Color pintado del agua de la boca (RGB del tinte de arriba, alfa 1).</summary>
        public Color MouthColor => _mouth.color;

        /// <summary>Tinte de la boca (0 sin boca o con la cima oculta).</summary>
        public int MouthTint => MouthVisible ? Mathf.Max(0, _mouthTint) : 0;

        /// <summary>El icono de la fruta de la boca está a la vista.</summary>
        public bool MouthGlyphVisible => _mouthGlyph.gameObject.activeSelf;

        /// <summary>Sprite del icono de la boca (tests).</summary>
        public Sprite? MouthGlyphSprite => _mouthGlyph.sprite;

        /// <summary>Rectángulo del agua de la boca (u desde el centro del pie).</summary>
        public Rect MouthRectU => RectU(_mouth.rectTransform);

        /// <summary>Rectángulo del icono de la boca (u desde el centro del pie).</summary>
        public Rect MouthGlyphRectU => RectU(_mouthGlyph.rectTransform);

        /// <summary>El chip «×n» está a la vista.</summary>
        public bool ChipVisible => _chip.gameObject.activeSelf;

        /// <summary>Cifra del chip «×n» (las dosis dentro, contando la boca; 0 sin chip).</summary>
        public int ChipCount => ChipVisible ? Mathf.Max(0, _count) : 0;

        /// <summary>Sprite de la cifra del chip (tests).</summary>
        public Sprite? ChipDigitSprite => _chipDigit.sprite;

        /// <summary>Rectángulo del chip (dp desde el centro del pie).</summary>
        public Rect ChipRectDp => RectDp(_chip.rectTransform);

        /// <summary>Alto de la cifra del chip (dp): la celda de glifo escalada.</summary>
        public float ChipGlyphHeightDp => _chipDigit.rectTransform.sizeDelta.y;

        /// <summary>Capas con banda (bit k = capa k desde el fondo): las reveladas bajo la boca.</summary>
        public int BandMask => Visible ? _bandMask : 0;

        /// <summary>Hay alguna banda a la vista.</summary>
        public int BandCount
        {
            get
            {
                int n = 0;
                for (int k = 0; k < MaxBands; k++)
                {
                    if (_bands[k].gameObject.activeSelf)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>Imagen de la banda de la capa <paramref name="k"/> (tests y capturas).</summary>
        public Image Band(int k)
        {
            return _bands[k];
        }

        /// <summary>Imagen del filo de luz de la banda de la capa <paramref name="k"/> (tests y capturas).</summary>
        public Image BandLight(int k)
        {
            return _bandLights[k];
        }

        /// <summary>Imagen del contorno de la banda de la capa <paramref name="k"/> (tests y capturas).</summary>
        public Image BandRim(int k)
        {
            return _bandRims[k];
        }

        /// <summary>
        /// La banda del tinte <paramref name="tint"/> lleva contorno: su tinte queda a ΔE00 &lt; <see cref="BandRimMinDeltaE"/>
        /// de alguno de los tonos del barro (<see cref="BandBodyTones"/>).
        /// </summary>
        public static bool BandNeedsRim(int tint)
        {
            return tint >= 0 && tint < s_bandRim.Length && s_bandRim[tint];
        }

        /// <summary>Imagen del icono de la banda de la capa <paramref name="k"/> (tests y capturas).</summary>
        public Image BandGlyph(int k)
        {
            return _bandGlyphs[k];
        }

        /// <summary>El jirau (arte provisional) está a la vista.</summary>
        public bool JirauVisible => _jirau.gameObject.activeSelf;

        /// <summary>Rectángulo del jirau en el espacio de las capas de tablero.</summary>
        public Rect JirauRect
        {
            get
            {
                Rect r = RectDp(_jirau.rectTransform);
                r.position += LayerOrigin;
                return r;
            }
        }

        /// <summary>Índice de su cuia (la pareja del jirau), o −1 si aún no se sabe.</summary>
        public int PartnerIndex => _partner;

        /// <summary>Rectángulo del cuerpo de barro (u desde el centro del pie).</summary>
        public Rect BodyRectU => RectU(_body.rectTransform);

        /// <inheritdoc />
        public override bool IsAnimating => _stepping;

        /// <summary>La tinaja vacía espera boca arriba, con el fondo seco en la boca, a su paso Upend (TJ3).</summary>
        public bool HoldingUpright => _holdUpright;

        /// <summary>El anillo de onda del revelado de la boca está a la vista (TJ3).</summary>
        public bool RingVisible => _ring.gameObject.activeSelf;

        /// <summary>Escala del chip «×n» (1 en reposo; el rebote del revelado la sube un momento, TJ3).</summary>
        public float ChipScale => _chip.rectTransform.localScale.x;

        /// <summary>Escala vertical del cuerpo (1 boca arriba, −1 boca abajo; entre medias, tumbándose, TJ3).</summary>
        public float BodyScaleY => _body.rectTransform.localScale.y;

        /// <summary>Hay una intro en curso (Llenar y teñir, D-010; TJ3): el chip y las bandas esperan al primer impacto.</summary>
        public bool InIntro => _intro;

        /// <summary>La boca enseña el fondo seco (disco #6B4A33 con su centro claro, sin brillo; TJ3).</summary>
        public bool DryMouthVisible => _dryCenter.gameObject.activeSelf;

        /// <summary>El brillo del agua de la boca está a la vista (nunca con el fondo seco).</summary>
        public bool MouthGlossVisible => _mouthGloss.gameObject.activeSelf;

        /// <summary>Gotas que su cuia dejó caer en la boca desde que se creó el overlay (<see cref="Drip"/>; tests).</summary>
        public int DripsStarted { get; private set; }

        /// <summary>Gotas cayendo ahora mismo.</summary>
        public int ActiveDrips
        {
            get
            {
                int n = 0;
                for (int i = 0; i < MaxDrips; i++)
                {
                    if (_dripAge[i] >= 0f)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>La gota <paramref name="i"/> está cayendo.</summary>
        public bool DripActive(int i)
        {
            return _dripAge[i] >= 0f;
        }

        /// <summary>Rectángulo de la gota <paramref name="i"/> (u desde el centro del pie; tests y capturas).</summary>
        public Rect DripRectU(int i)
        {
            return RectU(_drips[i].rectTransform);
        }

        /// <summary>La elipse de la cara de la boca (seca) que se aleja en la primera mitad del vuelco está a la vista (TJ3).</summary>
        public bool UpendCapVisible => _upendCap.gameObject.activeSelf;

        /// <summary>
        /// La tapa de la base está a la vista (TJ3): la cara que sube en la segunda mitad del vuelco y, boca abajo, la tapa
        /// elíptica de arriba.
        /// </summary>
        public bool BaseCapVisible => _baseCap.gameObject.activeSelf;

        /// <summary>Rectángulo de la tapa de la base (u desde el centro del pie; tests y capturas).</summary>
        public Rect BaseCapRectU => RectU(_baseCap.rectTransform);

        /// <summary>La Luciérnaga está revelando la tinaja (<see cref="PlayFirefly"/>; TJ4).</summary>
        public bool FireflyAnimating => _firefly;

        /// <summary>Capas que revela la Luciérnaga en curso (0 sin revelado; tests).</summary>
        public int FireflyMask => _firefly ? _fireflyMask : 0;

        /// <summary>Opacidad de la luz de dentro (0 apagada; tests y capturas).</summary>
        public float GlowAlpha => _glow.gameObject.activeSelf ? _glow.color.a : 0f;

        /// <summary>Rectángulo de la luz de dentro (u desde el pie y el eje; tests).</summary>
        public Rect GlowRectU => RectU(_glow.rectTransform);

        /// <summary>
        /// El cuerpo enseña el hueco oscuro de la boca (boca arriba). Boca abajo, y desde la mitad del vuelco, el cuerpo es el
        /// cerrado: sin hueco, con el arco del canto del labio abajo (revisión de TJ3).
        /// </summary>
        public bool BodyShowsCavity => _body.sprite == _bodyOpen;

        /// <summary>
        /// Opacidad de la copia boca abajo del fundido cruzado de la suelta con Reducir movimiento (0 sin fundido; TJ3).
        /// </summary>
        public float FlipFadeAlpha => _bodyFlip.gameObject.activeSelf ? _bodyFlip.color.a : 0f;

        /// <summary>Color (con su alfa) de la gota <paramref name="i"/> (tests y capturas).</summary>
        public Color DripColor(int i)
        {
            return _drips[i].color;
        }

        /// <summary>Color de la cara de la vasija (blanco en reposo; se oscurece de canto en el vuelco, TJ3).</summary>
        public Color BodyColor => _body.color;

        /// <summary>Posición del cuerpo (dp desde el pie; su centro) y su giro (grados), para el vuelco de la suelta (tests).</summary>
        public Vector2 BodyCenterDp => _body.rectTransform.anchoredPosition;

        /// <summary>Giro del cuerpo (grados; 0 en reposo).</summary>
        public float BodyAngleDeg => _body.rectTransform.localEulerAngles.z > 180f ? _body.rectTransform.localEulerAngles.z - 360f : _body.rectTransform.localEulerAngles.z;

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Tinaja;
            Visible = on;
            _last = cuia;
            _lastSealed = sealedNow;
            _hasLast = true;
            if (on && !_prewarmed)
            {
                Prewarm();
            }

            bool art = on && MapArtRule.ShowProvisional;
            int count = on ? cuia.Count : 0;
            bool empty = count == 0;
            if (count > _count)
            {
                ClearDrips(); // Marea o Volver a la orilla devuelven dosis: ninguna gota de antes sigue cayendo
            }

            _count = count;
            if (art != _artShown)
            {
                _artShown = art;
                PlaceChip();
            }

            // Con la última sacada (TJ3), la tinaja vacía espera boca arriba, con el fondo seco, hasta su paso Upend.
            bool hold = on && empty && _holdUpright;
            SetUpsideDown(empty && !hold);
            OverlayParts.SetVisible(_body, art);
            PlaceBaseCap(art && _upsideDown);

            // Boca (R7): la fruta de arriba, con su tinte y su icono, en la boca de la vasija (solo con su cuerpo: sin él, la cima
            // ya se ve en el líquido del vidrio). Si la cima estuviera oculta (no debería), la boca va oscura y sin icono: lo
            // oculto nunca enseña su tinte. Vacía y esperando (hold), oscura: el fondo seco.
            bool filled = on && !empty;
            bool mouth = (filled || hold) && art;
            bool dry = mouth && !filled; // el fondo seco: vacía y esperando su vuelco (TJ3)
            int top = count - 1;
            bool topShown = mouth && filled && !cuia.IsHiddenAt(top);
            int tint = topShown ? cuia.ColorAt(top) : 0;
            if (dry)
            {
                if (!_dryShown)
                {
                    // Pasa al fondo seco en DryFadeMs desde la fruta que tenía (la última sacada), sin anillo: no hay dosis nueva.
                    bool fade = !_intro && !_stepping && _mouth.gameObject.activeSelf && _mouthTint > 0;
                    _dryFrom = _mouth.color;
                    _dryFadeT = fade ? 0f : -1f;
                    _dryShown = true;
                }

                _mouthTint = -1; // al volver a llenarse (Marea), la boca se vuelve a pintar entera
                ApplyDry();
            }
            else
            {
                _dryShown = false;
                _dryFadeT = -1f;
                if (mouth && tint != _mouthTint)
                {
                    _mouthTint = tint;
                    OverlayParts.SetColor(_mouth, tint > 0 ? Palette.Tint(tint) : Palette.WoodDark);
                    if (tint > 0)
                    {
                        _mouthGlyph.sprite = Host.Art.TinajaGlyph(tint);
                    }
                }
            }

            OverlayParts.SetVisible(_mouth, mouth);
            OverlayParts.SetVisible(_mouthGloss, mouth && !dry);
            OverlayParts.SetVisible(_dryCenter, dry);
            OverlayParts.SetVisible(_mouthGlyph, mouth && tint > 0);

            // Chip «×n»: las dosis que quedan dentro, contando la boca (b.2, «Qué se ve»); en el hombro de la vasija o, sin ella,
            // en el del vidrio.
            OverlayParts.SetVisible(_chip, filled);
            if (filled)
            {
                Sprite digit = Host.Art.MurkyBadgeDigit(count);
                if (_chipDigit.sprite != digit)
                {
                    _chipDigit.sprite = digit;
                }
            }

            // Bandas: las capas bajo la boca que ya están reveladas (R7, T6, I5; la Luciérnaga), a su altura y con su icono, en el
            // barro (solo con el cuerpo de la vasija: sin él, el vidrio ya enseña la capa revelada y se pintaría dos veces). El
            // relleno, con el HEX del tinte opaco; la luz, en el filo (BandEdgeLight); y, si el tinte queda cerca del barro, un
            // contorno de 1 dp con el tinte oscurecido.
            int bands = 0;
            for (int k = 0; k < MaxBands; k++)
            {
                bool band = mouth && k < top && !cuia.IsHiddenAt(k);
                Image img = _bands[k];
                Image glyph = _bandGlyphs[k];
                bool rim = false;
                if (band)
                {
                    bands |= 1 << k;
                    int bt = cuia.ColorAt(k);
                    Color c = Palette.Tint(bt);
                    c.a = 1f;
                    OverlayParts.SetColor(img, c);
                    rim = BandNeedsRim(bt);
                    if (rim)
                    {
                        OverlayParts.SetColor(_bandRims[k], ColorMath.Darken(c, BandRimDarken));
                    }

                    Sprite gs = Host.Art.TinajaGlyph(bt);
                    if (glyph.sprite != gs)
                    {
                        glyph.sprite = gs;
                    }
                }

                OverlayParts.SetVisible(img, band);
                OverlayParts.SetVisible(_bandLights[k], band);
                OverlayParts.SetVisible(_bandRims[k], rim);
                OverlayParts.SetVisible(glyph, band);
            }

            _bandMask = bands;
            PlaceJirau(on);
            if (_intro)
            {
                ApplyIntro();
            }
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            PlaceChip();
            OverlayParts.Place(_body, 0f, 0.5f * ProceduralArt.TinajaBodyHeightU, ProceduralArt.TinajaBodyWidthU, ProceduralArt.TinajaBodyHeightU, u);
            OverlayParts.Place(_mouth, 0f, ProceduralArt.TinajaMouthCenterU, ProceduralArt.TinajaMouthWidthU, ProceduralArt.TinajaMouthHeightU, u);
            OverlayParts.Place(_mouthGloss, 0f, ProceduralArt.TinajaMouthCenterU, ProceduralArt.TinajaMouthWidthU, ProceduralArt.TinajaMouthHeightU, u);
            OverlayParts.Place(_mouthGlyph, 0f, ProceduralArt.TinajaMouthCenterU, ProceduralArt.TinajaMouthGlyphU, ProceduralArt.TinajaMouthGlyphU, u);
            OverlayParts.Place(_dryCenter, 0f, ProceduralArt.TinajaMouthCenterU, ProceduralArt.TinajaMouthWidthU, ProceduralArt.TinajaMouthHeightU, u);
            OverlayParts.Place(_glow, 0f, ProceduralArt.TinajaGlowCenterU, ProceduralArt.TinajaGlowWidthU, ProceduralArt.TinajaGlowHeightU, u);
            for (int k = 0; k < MaxBands; k++)
            {
                float y = BandCenterU(k);
                float w = 2f * (ProceduralArt.TinajaHalfWidthU(y) - ProceduralArt.TinajaBandInsetU);
                OverlayParts.Place(_bands[k], 0f, y, w, ProceduralArt.TinajaBandHeightU, u);
                OverlayParts.Place(_bandLights[k], 0f, y, w, ProceduralArt.TinajaBandHeightU, u);
                OverlayParts.Place(_bandRims[k], 0f, y, w, ProceduralArt.TinajaBandHeightU, u);
                OverlayParts.Place(_bandGlyphs[k], 0f, y, ProceduralArt.TinajaBandGlyphU, ProceduralArt.TinajaBandGlyphU, u);
            }

            _layoutVersion = -1;
            PlaceJirau(Visible);
        }

        /// <inheritdoc />
        public override void OnBoardLayout()
        {
            _layoutVersion = -1;
            PlaceJirau(Visible);
        }

        /// <summary>
        /// El jirau sigue al pie de la tinaja aunque el tablero se mueva sin recolocarse (el lienzo cambia de tamaño, la zona
        /// segura): si su pie se movió desde la última colocación, se vuelve a colocar. Sin asignaciones.
        /// </summary>
        public override void Tick(float dtMs)
        {
            if (_stepping)
            {
                _stepT += dtMs;
                ApplyStep();
            }

            if (_firefly)
            {
                _fireflyT += dtMs;
                if (_fireflyT >= (_fireflyReduce ? ReducedFadeMs : FireflyMs) * _fireflySpeed)
                {
                    EndFirefly(false); // la pantalla pinta enseguida lo revelado (GameScreen.Reveal.cs: PaintRevealed)
                }
                else
                {
                    ApplyFirefly();
                }
            }

            if (_dryFadeT >= 0f)
            {
                _dryFadeT += dtMs;
                if (_dryShown)
                {
                    ApplyDry();
                }

                if (_dryFadeT >= DryFadeMs)
                {
                    _dryFadeT = -1f;
                }
            }

            TickDrips(dtMs);
            ICuiaBoard? board = Host.Board;
            if (board == null || !_jirau.gameObject.activeSelf)
            {
                return;
            }

            if ((board.Foot(Host.Index) - _jirauFoot).sqrMagnitude > 0.0001f)
            {
                _layoutVersion = board.LayoutVersion;
                LayoutJirau(board);
            }
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(Palette.TinajaClay); // el cuerpo (la boca y las bandas llevan el tinte a propósito)
            into.Add(ProceduralArt.TinajaChipWood);
            into.Add(ProceduralArt.JirauWood);
            into.Add(ProceduralArt.JirauShade);
        }

        /// <summary>
        /// Centro del chip «×n» sin el cuerpo de la vasija (u desde el pie) [P]: en el hombro del vidrio de una cuia de
        /// <paramref name="capacity"/> dosis, entre lo alto de la cavidad y el cuello (sin el cuerpo no hay hombro de barro).
        /// </summary>
        public static float GlassChipCenterU(int capacity)
        {
            return CuiaGeometry.CavityTopU(capacity) + (0.5f * CuiaGeometry.ShoulderU);
        }

        /// <summary>
        /// Fija su cuia: la pareja del tablero inicial del nivel (la pasa la pantalla, que la conoce aunque la suelta le haya
        /// quitado el tipo; 07 §4.3 y 02 §5: el jirau va bajo las dos ranuras de la pareja). Manda sobre lo que se deduzca del
        /// tablero mostrado; −1 lo olvida. Sin asignaciones.
        /// </summary>
        public void AssignPartner(int index)
        {
            if (index < 0)
            {
                _partnerAssigned = false;
                _partnerSure = false;
                SetPartner(-1, false);
                return;
            }

            _partnerAssigned = true;
            SetPartner(index, true);
            if (Visible)
            {
                PlaceJirau(true);
            }
        }

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            switch (kind)
            {
                case MechanicStepKind.Reveal:
                    return RevealStepMs;
                case MechanicStepKind.Upend:
                    return UpendHoldMs + UpendMs;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Pasos de la tinaja (TJ3; propuesta de la Tinaja, b.2 «Animación y sonido»):
        /// <list type="bullet">
        /// <item><description><see cref="MechanicStepKind.Reveal"/> (etapa a, R7): la boca queda oscura
        /// <see cref="RevealDarkMs"/>, la fruta nueva florece en <see cref="RevealBloomMs"/> (OKLab, como D-010) con un anillo de
        /// onda sin burbujas, y el chip baja (×n+1 → ×n) con un rebote; en ≤ 400 ms. Sin el cuerpo de la vasija (release hasta
        /// el arte), solo el chip.</description></item>
        /// <item><description><see cref="MechanicStepKind.Upend"/> (etapa f): la boca enseña el fondo seco mientras su cuia baja
        /// del jirau (<see cref="UpendHoldMs"/>) y la vasija se tumba boca abajo sobre el jirau (<see cref="UpendMs"/>). Sin el
        /// cuerpo no hay nada que tumbar: 0 (salto).</description></item>
        /// </list>
        /// Con Reducir movimiento, fundidos sin anillo ni rebote. Sin asignaciones.
        /// </summary>
        public override float PlayStep(in MechanicStepContext ctx)
        {
            if (_stepping)
            {
                SnapStep();
            }

            float speed = Mathf.Max(0.01f, ctx.SpeedFactor);
            if (ctx.Kind == MechanicStepKind.Reveal)
            {
                Cuia after = ctx.After;
                Show(after, ctx.AfterSealed);
                int top = after.Count - 1;
                _stepTint = top >= 0 && !after.IsHiddenAt(top) ? after.ColorAt(top) : 0;
                _stepNewCount = after.Count;
                _stepOldCount = Mathf.Min(Host.Capacity, after.Count + 1);
                Begin(MechanicStepKind.Reveal, speed, ctx.ReduceMotion);
                return RevealStepMs * speed;
            }

            if (ctx.Kind == MechanicStepKind.Upend && MapArtRule.ShowProvisional)
            {
                _holdUpright = true;
                ClearDrips(); // la suelta: su cuia ya está en su ranura y no queda ninguna gota
                Show(ctx.Before, false);
                _upendSide = ctx.OtherIndex >= 0 && ctx.OtherPos.x > ctx.SelfPos.x ? 1f : -1f; // se tumba hacia el lado contrario a su cuia
                Begin(MechanicStepKind.Upend, speed, ctx.ReduceMotion);
                return (UpendHoldMs + UpendMs) * speed;
            }

            return 0f;
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            if (!_stepping)
            {
                return;
            }

            _stepping = false;
            if (_stepKind == MechanicStepKind.Upend)
            {
                _holdUpright = false;
            }

            ResetStepParts();
            if (_hasLast)
            {
                Show(_last, _lastSealed); // la cola pinta después el estado de después del paso
            }
        }

        /// <summary>
        /// La tinaja vacía espera boca arriba, con el fondo seco, hasta su paso Upend (lo pide <c>BoardAnimator</c> al encolar la
        /// última sacada y lo suelta al acabar o saltar el plan; TJ3). Si cambia, se vuelve a pintar el último estado. Sin
        /// asignaciones.
        /// </summary>
        public void HoldUpright(bool on)
        {
            if (_holdUpright == on)
            {
                return;
            }

            _holdUpright = on;
            if (!_stepping && _hasLast && AttachedHost != null)
            {
                Show(_last, _lastSealed);
            }
        }

        /// <summary>
        /// Una gota de la vuelta de su cuia al sacar (02 §4.1 y 07 §5.6: «vuelve con 2-3 gotas»; b.2; revisión de TJ3): cae desde
        /// <paramref name="fromDp"/> (el punto más bajo de su cuia, en dp desde el pie de la tinaja) hasta el centro de la cavidad
        /// (<see cref="DripLandBelowU"/> bajo el centro de la boca) en <see cref="DripFallMs"/> y allí se apaga en
        /// <see cref="DripFadeMs"/>. Mide <see cref="DripDiameterDp"/>, con el tinte <paramref name="tint"/> de lo que sacó
        /// aclarado (<see cref="DripLightness"/>) y un brillo blanco de <see cref="DripGlintDp"/>. La dibuja la tinaja, que va
        /// delante de su cuia, encima de su boca: si su cuia aún está detrás del labio, nace en lo alto de la boca, y nunca fuera
        /// de ella. Se borran al florecer la fruta del revelado, en la suelta y con Marea o Volver a la orilla. Sin anillo ni
        /// burbujas. Sin el cuerpo de la vasija (release hasta el arte), nada. Sin asignaciones.
        /// </summary>
        public void Drip(Vector2 fromDp, Color tint)
        {
            if (!Visible || !_artShown)
            {
                return;
            }

            int slot = 0;
            for (int i = 0; i < MaxDrips; i++)
            {
                if (_dripAge[i] < 0f)
                {
                    slot = i;
                    break;
                }

                if (_dripAge[i] > _dripAge[slot])
                {
                    slot = i; // todas cayendo: la más vieja
                }
            }

            float u = Mathf.Max(0.0001f, _u);
            float half = 0.5f * ProceduralArt.TinajaMouthWidthU;
            float x = Mathf.Clamp(fromDp.x / u, -0.6f * half, 0.6f * half);
            float mouthTop = ProceduralArt.TinajaMouthCenterU + (0.5f * ProceduralArt.TinajaMouthHeightU);
            float y0 = Mathf.Max(fromDp.y / u, mouthTop);
            float fall = DripFallMs / 1000f;
            _dripFrom[slot] = new Vector2(x, y0);
            _dripVy[slot] = (DripLandU - y0 + (0.5f * DripGravityU * fall * fall)) / fall;
            _dripSizeU[slot] = DripDiameterDp / u;
            Color c = ColorMath.ScaleOkLightness(new Color(tint.r, tint.g, tint.b, 1f), DripLightness);
            _dripColor[slot] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
            _dripAge[slot] = 0f;
            DripsStarted++;
            PlaceDrip(slot);
            OverlayParts.SetVisible(_drips[slot], true);
            OverlayParts.SetVisible(_dripGlints[slot], true);
        }

        /// <summary>
        /// Empieza la intro (Llenar y teñir, D-010; TJ3): hasta el primer impacto no se ven ni el chip «×n» ni las bandas, y la
        /// boca la pinta la intro (<see cref="SetIntroMouth"/>): seca, con el agua del río y con su fruta al florecer.
        /// </summary>
        public void BeginIntro(Color dryMouth)
        {
            ClearDrips();
            _intro = true;
            _introMarks = false;
            _introMouth = dryMouth;
            _introTint = 0;
            _mouthTint = -1;
            if (_hasLast && AttachedHost != null)
            {
                Show(_last, _lastSealed);
            }
        }

        /// <summary>Color del agua de la boca durante la intro y, si <paramref name="tint"/> &gt; 0, el icono de esa fruta. Sin asignaciones.</summary>
        public void SetIntroMouth(Color color, int tint)
        {
            if (!_intro)
            {
                return;
            }

            _introMouth = color;
            _introTint = tint;
            ApplyIntroMouth();
        }

        /// <summary>El primer impacto de la intro (D-010): desde este fotograma, el chip «×n» y las bandas ya reveladas.</summary>
        public void ShowIntroMarks()
        {
            if (!_intro || _introMarks)
            {
                return;
            }

            _introMarks = true;
            if (_hasLast && AttachedHost != null)
            {
                Show(_last, _lastSealed);
            }
        }

        /// <summary>Fin de la intro: la tinaja vuelve a pintarse solo desde su estado.</summary>
        public void EndIntro()
        {
            if (!_intro)
            {
                return;
            }

            _intro = false;
            _introMarks = false;
            _mouthTint = -1;
            OverlayParts.SetColor(_mouthGlyph, Color.white);
            if (_hasLast && AttachedHost != null)
            {
                Show(_last, _lastSealed);
            }
        }

        /// <summary>
        /// Alto del jirau (dp) con la unidad <paramref name="u"/> (dp por u): de <see cref="JirauTopAboveFootU"/> sobre la línea de
        /// pie a <see cref="JirauBelowFootDp"/> bajo ella (≈ 3,5 dp con la cuia de 48 dp: dos varas de ≈ 1,7 dp).
        /// </summary>
        public static float JirauHeightDp(float u)
        {
            return (JirauTopAboveFootU * u) + JirauBelowFootDp;
        }

        /// <summary>Centro de la banda de la capa <paramref name="k"/> (u desde el pie): a la altura de su dosis en la Cuia alta de 5.</summary>
        public static float BandCenterU(int k)
        {
            return CuiaGeometry.CavityBottomU + k + 0.5f;
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            // El jirau, en la capa de tablero de atrás (une dos ranuras; detrás de las cuias y sobre la repisa).
            // En una raíz propia anclada a la esquina superior izquierda de la capa, como las ranuras de las cuias (y el canal
            // del Remolino): si el tablero cambia de tamaño sin recolocarse, jirau y cuias se mueven juntos. Las posiciones del
            // tablero (ICuiaBoard, relativas al pivote de la capa) se pasan a la raíz restando LayerOrigin. La raíz se va con la
            // cuia (LayerLink): la capa sigue de un nivel a otro.
            RectTransform back = Host.BoardBackLayer!;
            var go = new GameObject("TinajaJirau" + Host.Index, typeof(RectTransform));
            go.transform.SetParent(back, false);
            _layerRoot = (RectTransform)go.transform;
            _layerRoot.anchorMin = new Vector2(0f, 1f);
            _layerRoot.anchorMax = new Vector2(0f, 1f);
            _layerRoot.pivot = new Vector2(0.5f, 0.5f);
            _layerRoot.sizeDelta = Vector2.zero;
            _layerRoot.anchoredPosition = Vector2.zero;
            go.AddComponent<LayerLink>().Owner = Host.SlotBack;
            _jirau = OverlayParts.NewImage("TinajaJirau", _layerRoot, Host.Art.TinajaJirau, Color.white);
            _jirau.type = Image.Type.Sliced; // varas continuas, atadas solo en las puntas (el borde del 9-slice)

            // La vasija y lo que se ve en ella, en el cuerpo, sobre el vidrio y el líquido.
            _bodyOpen = Host.Art.TinajaBody;
            _bodyClosed = Host.Art.TinajaBodyClosed;
            _body = OverlayParts.NewImage("TinajaBody", Host.BodyFront, _bodyOpen, Color.white);
            _bodyFlip = OverlayParts.NewImage("TinajaBodyFlip", Host.BodyFront, _bodyClosed, Color.white); // Reducir movimiento (TJ3)
            _glow = OverlayParts.NewImage("TinajaGlow", Host.BodyFront, Host.Art.TinajaGlow, Color.clear); // la Luciérnaga (TJ4), bajo las bandas
            for (int k = 0; k < MaxBands; k++)
            {
                _bands[k] = OverlayParts.NewImage("TinajaBand" + k, Host.BodyFront, Host.Art.TinajaBand, Color.white);
                _bandLights[k] = OverlayParts.NewImage("TinajaBandLight" + k, Host.BodyFront, Host.Art.TinajaBandLight, BandEdgeLight);
                _bandRims[k] = OverlayParts.NewImage("TinajaBandRim" + k, Host.BodyFront, Host.Art.TinajaBandRim, Color.white);
            }

            for (int k = 0; k < MaxBands; k++)
            {
                _bandGlyphs[k] = OverlayParts.NewImage("TinajaBandGlyph" + k, Host.BodyFront, null, Color.white);
            }

            _mouth = OverlayParts.NewImage("TinajaMouth", Host.BodyFront, Host.Art.TinajaMouthDisc, Color.white);
            _mouthGloss = OverlayParts.NewImage("TinajaMouthGloss", Host.BodyFront, Host.Art.TinajaMouthGloss, Color.white);
            _dryCenter = OverlayParts.NewImage("TinajaDryMouth", Host.BodyFront, Host.Art.TinajaDryMouthCenter, DryMouthCenter); // el fondo seco (TJ3)
            _mouthGlyph = OverlayParts.NewImage("TinajaMouthGlyph", Host.BodyFront, null, Color.white);
            _ring = OverlayParts.NewImage("TinajaRing", Host.BodyFront, Host.Art.Ring, RingColor); // la onda del revelado (TJ3)
            _upendCap = OverlayParts.NewImage("TinajaUpendCap", Host.BodyFront, Host.Art.TinajaMouthDisc, DryMouthRim); // la cara de la boca en el vuelco (TJ3)
            _baseCap = OverlayParts.NewImage("TinajaBaseCap", Host.BodyFront, Host.Art.TinajaBaseCap, BaseCapColor); // la base, boca abajo (TJ3)
            for (int i = 0; i < MaxDrips; i++)
            {
                _drips[i] = OverlayParts.NewImage("TinajaDrip" + i, Host.BodyFront, Host.Art.TinajaDripDot, Color.white); // las gotas al sacar (TJ3)
                _dripGlints[i] = OverlayParts.NewImage("TinajaDripGlint" + i, Host.BodyFront, Host.Art.TinajaDripDot, Color.white);
            }
            _chip = OverlayParts.NewImage("TinajaChip", Host.BodyFront, Host.Art.TinajaChipPill, Color.white);
            _chip.type = Image.Type.Sliced;
            _chipTimes = OverlayParts.NewImage("TinajaChipX", _chip.rectTransform, Host.Art.MurkyBadgeTimes, Color.white);
            _chipDigit = OverlayParts.NewImage("TinajaChipN", _chip.rectTransform, Host.Art.MurkyBadgeDigit(1), Color.white);
            PlaceChipGlyph(_chipTimes, -0.5f * ChipGlyphStepDp);
            PlaceChipGlyph(_chipDigit, 0.5f * ChipGlyphStepDp);
            OverlayParts.SetVisible(_chipTimes, true); // cuelgan del chip: se ven y se ocultan con él
            OverlayParts.SetVisible(_chipDigit, true);
        }

        /// <summary>
        /// La Luciérnaga revela la tinaja (TJ4; 04 §2.5: «revela todas las capas ocultas de la tinaja»; propuesta de la Tinaja, b.2
        /// «Animación y sonido»): con la luciérnaga ya posada en la boca, la barriga se enciende por dentro
        /// (<see cref="FireflyGlowAlpha"/>, con la luz de la luciérnaga) y las capas <paramref name="layerMask"/> de
        /// <paramref name="shown"/> (el estado mostrado, con el tinte real de cada capa) se encienden como bandas de arriba abajo,
        /// en <see cref="FireflyMs"/> × <paramref name="speedFactor"/>. Con Reducir movimiento, sin luz: las bandas a la vez en un
        /// fundido de <see cref="ReducedFadeMs"/>. Devuelve la duración (ms, con el factor), o 0 si no hay nada que enseñar (sin
        /// el cuerpo de la vasija, en release hasta el arte, el vidrio enseña lo revelado al pintarse; sin capas ocultas bajo la
        /// boca). Nunca pinta el pardo ni el «?» (contrato 27). Al terminar, las bandas quedan encendidas y la pantalla pinta lo
        /// revelado. Sin asignaciones.
        /// </summary>
        public float PlayFirefly(int layerMask, Cuia shown, float speedFactor, bool reduceMotion)
        {
            if (!Visible || !_artShown || shown.Mechanic != MechanicType.Tinaja || shown.Count < 2)
            {
                return 0f;
            }

            int under = (1 << Mathf.Min(MaxBands, shown.Count - 1)) - 1; // las capas bajo la boca, donde van las bandas
            int layers = layerMask & shown.HiddenMask & under;
            if (layers == 0)
            {
                return 0f;
            }

            SnapFirefly();
            _firefly = true;
            _fireflyT = 0f;
            _fireflySpeed = Mathf.Max(0.01f, speedFactor);
            _fireflyReduce = reduceMotion;
            _fireflyMask = layers;
            _fireflyCuia = shown;
            int n = 0;
            for (int k = 0; k < MaxBands; k++)
            {
                n += (layers >> k) & 1;
            }

            _fireflyLayers = n;
            ApplyFirefly();
            return (reduceMotion ? ReducedFadeMs : FireflyMs) * _fireflySpeed;
        }

        /// <summary>
        /// Termina en el acto el revelado de la Luciérnaga (otro vertido, Marea, Volver a la orilla o fuera de PLAYING): sin luz y
        /// la tinaja con su último estado mostrado (la pantalla pinta después el que le toca). Sin revelado, nada.
        /// </summary>
        public void SnapFirefly()
        {
            if (_firefly)
            {
                EndFirefly(true);
            }
        }

        // Un fotograma de la Luciérnaga, en ms nominales: la luz de dentro sube, se mantiene y se apaga; cada capa se enciende cuando
        // la luz baja hasta ella (la de arriba primero). Show puede repintar el estado de antes mientras dura: esto lo vuelve a
        // poner encima en cada fotograma.
        private void ApplyFirefly()
        {
            float x = _fireflyT / _fireflySpeed;
            float glow = 0f;
            if (!_fireflyReduce)
            {
                float up = Mathf.Clamp01(x / FireflyGlowInMs);
                float down = Mathf.Clamp01((FireflyMs - x) / FireflyGlowOutMs);
                glow = FireflyGlowAlpha * Mathf.Min(up, down);
            }

            OverlayParts.SetVisible(_glow, glow > 0f);
            if (glow > 0f)
            {
                OverlayParts.SetColor(_glow, OverlayParts.Fade(ProceduralArt.FireflyLit, glow));
            }

            int rank = 0;
            float span = FireflyFrontEndMs - FireflyFrontStartMs;
            for (int k = MaxBands - 1; k >= 0; k--)
            {
                if (((_fireflyMask >> k) & 1) == 0)
                {
                    continue;
                }

                float start = _fireflyReduce ? 0f : FireflyFrontStartMs + (_fireflyLayers > 1 ? rank * span / (_fireflyLayers - 1) : 0f);
                float fade = _fireflyReduce ? ReducedFadeMs : FireflyBandFadeMs;
                SetFireflyBand(k, Mathf.Clamp01((x - start) / fade));
                rank++;
            }
        }

        // Banda k encendida con opacidad a (0 = apagada): el tinte real de la capa, su filo de luz, su contorno y su icono.
        private void SetFireflyBand(int k, float a)
        {
            bool on = a > 0f;
            Image img = _bands[k];
            Image glyph = _bandGlyphs[k];
            OverlayParts.SetVisible(img, on);
            OverlayParts.SetVisible(_bandLights[k], on);
            OverlayParts.SetVisible(glyph, on);
            int bt = _fireflyCuia.ColorAt(k);
            bool rim = on && BandNeedsRim(bt);
            OverlayParts.SetVisible(_bandRims[k], rim);
            if (!on)
            {
                return;
            }

            Color c = Palette.Tint(bt);
            c.a = a;
            OverlayParts.SetColor(img, c);
            OverlayParts.SetColor(_bandLights[k], OverlayParts.Fade(BandEdgeLight, a));
            if (rim)
            {
                Color r = ColorMath.Darken(Palette.Tint(bt), BandRimDarken);
                r.a = a;
                OverlayParts.SetColor(_bandRims[k], r);
            }

            Sprite gs = Host.Art.TinajaGlyph(bt);
            if (glyph.sprite != gs)
            {
                glyph.sprite = gs;
            }

            OverlayParts.SetColor(glyph, new Color(1f, 1f, 1f, a));
        }

        // Fin de la Luciérnaga: sin luz, cada pieza con su color de reposo y, con repaint, la tinaja con su último estado. Sin
        // repaint (fin natural), las bandas reveladas quedan encendidas hasta que la pantalla pinte el estado revelado.
        private void EndFirefly(bool repaint)
        {
            _firefly = false;
            OverlayParts.SetVisible(_glow, false);
            for (int k = 0; k < MaxBands; k++)
            {
                if (((_fireflyMask >> k) & 1) == 0)
                {
                    continue;
                }

                if (!repaint)
                {
                    SetFireflyBand(k, 1f);
                }

                OverlayParts.SetColor(_bandLights[k], BandEdgeLight);
                OverlayParts.SetColor(_bandGlyphs[k], Color.white);
            }

            _fireflyMask = 0;
            if (repaint && _hasLast && AttachedHost != null)
            {
                Show(_last, _lastSealed);
            }
        }

        private void Begin(MechanicStepKind kind, float speed, bool reduce)
        {
            _stepping = true;
            _stepKind = kind;
            _stepT = 0f;
            _stepSpeed = speed;
            _stepReduce = reduce;
            ApplyStep();
        }

        // Fotograma del paso en curso, en ms nominales (sin el factor de velocidad). Sin asignaciones.
        private void ApplyStep()
        {
            float x = _stepT / Mathf.Max(0.01f, _stepSpeed);
            if (_stepKind == MechanicStepKind.Reveal)
            {
                ApplyReveal(x);
            }
            else if (_stepKind == MechanicStepKind.Upend)
            {
                ApplyUpend(x);
            }
        }

        // Revelado en la boca: oscura, onda y floración OKLab de la fruta nueva; el chip baja con un rebote.
        private void ApplyReveal(float x)
        {
            bool mouth = _mouth.gameObject.activeSelf;
            Color tint = _stepTint > 0 ? Palette.Tint(_stepTint) : Palette.WoodDark;
            tint.a = 1f;
            Color dark = Palette.WoodDark;
            dark.a = 1f;
            float bloom;
            bool after;
            if (_stepReduce)
            {
                bloom = Mathf.Clamp01(x / ReducedFadeMs); // Reducir movimiento: fundido, sin onda ni rebote
                after = true;
                OverlayParts.SetVisible(_ring, false);
            }
            else
            {
                after = x >= RevealDarkMs;
                bloom = after ? Mathf.Clamp01((x - RevealDarkMs) / RevealBloomMs) : 0f;
                float q = (x - RevealDarkMs) / (RevealStepMs - RevealDarkMs);
                bool ring = mouth && after && q < 1f;
                OverlayParts.SetVisible(_ring, ring);
                if (ring)
                {
                    float sc = Mathf.Lerp(RingStartScale, RingEndScale, Ease.Out(q));
                    OverlayParts.Place(_ring, 0f, ProceduralArt.TinajaMouthCenterU, ProceduralArt.TinajaMouthWidthU * sc, ProceduralArt.TinajaMouthHeightU * sc, _u);
                    OverlayParts.SetColor(_ring, OverlayParts.Fade(RingColor, 1f - q));
                }
            }

            if (after)
            {
                ClearDrips(); // la fruta nueva florece: ninguna gota de lo sacado sigue a la vista (revisión de TJ3)
            }

            if (mouth)
            {
                OverlayParts.SetColor(_mouth, bloom <= 0f ? dark : (bloom >= 1f ? tint : ColorMath.OkLabLerp(dark, tint, bloom)));
                OverlayParts.SetColor(_mouthGlyph, new Color(1f, 1f, 1f, bloom));
                _mouthTint = -1; // el próximo Show la vuelve a pintar entera
            }

            int n = after ? _stepNewCount : _stepOldCount;
            if (n > 0)
            {
                Sprite digit = Host.Art.MurkyBadgeDigit(n);
                if (_chipDigit.sprite != digit)
                {
                    _chipDigit.sprite = digit;
                }
            }

            float k = _stepReduce || !after ? 0f : Mathf.Clamp01((x - RevealDarkMs) / ChipBounceMs);
            float scale = 1f + (ChipBounce * Mathf.Sin(Mathf.PI * k));
            var s3 = new Vector3(scale, scale, 1f);
            if (_chip.rectTransform.localScale != s3)
            {
                _chip.rectTransform.localScale = s3;
            }
        }

        // La suelta: boca seca y quieta mientras su cuia baja; después la vasija se voltea sobre el jirau y queda boca abajo. El
        // vuelco se lee como un sólido que da la vuelta (revisión de TJ3): el extremo que se aleja enseña su cara (la boca seca y,
        // pasada la mitad, la tapa de la base) como una elipse que crece con |sen θ|, la cara se oscurece de canto, la vasija se
        // ladea hacia fuera de su cuia con el pivote en su canto de abajo de ese lado (apoyada en el jirau todo el rato, sin
        // levantarse) y termina con un asiento de UpendSettleDeg. Desde la mitad del giro el cuerpo es el cerrado (sin el hueco de
        // la boca, que ya mira abajo). Con Reducir movimiento, sin giro: un fundido cruzado de ReducedFadeMs (ApplyUpendFade).
        private void ApplyUpend(float x)
        {
            bool tumbling = x >= UpendHoldMs;
            if (_stepReduce)
            {
                ApplyUpendFade(tumbling ? Mathf.Clamp01((x - UpendHoldMs) / ReducedFadeMs) : 0f);
                return;
            }

            OverlayParts.SetVisible(_mouth, !tumbling);
            OverlayParts.SetVisible(_mouthGloss, false);
            OverlayParts.SetVisible(_dryCenter, !tumbling);
            OverlayParts.SetVisible(_mouthGlyph, false);
            float flipMs = UpendMs - UpendSettleMs;
            float q = !tumbling ? 0f : Mathf.Clamp01((x - UpendHoldMs) / flipMs);
            float theta = Mathf.PI * Ease.InOut(q);
            float sy = Mathf.Cos(theta);
            float sin = Mathf.Abs(Mathf.Sin(theta));
            float settle = 0f;
            if (tumbling && q >= 1f)
            {
                settle = UpendSettleDeg * Mathf.Sin(Mathf.PI * Mathf.Clamp01((x - UpendHoldMs - flipMs) / UpendSettleMs));
            }

            // Ángulo positivo = antihorario: con su cuia a la derecha (_upendSide = 1) la vasija se ladea a la izquierda.
            float angle = ((Mathf.Sin(theta) * UpendTiltDeg) + settle) * _upendSide;
            float lean = -_upendSide; // lado (x) hacia el que se ladea
            float hb = ProceduralArt.TinajaHalfWidthU(0f);
            float h = ProceduralArt.TinajaBodyHeightU * Mathf.Abs(sy);
            var contact = new Vector2(lean * hb, 0f);
            float rad = angle * Mathf.Deg2Rad;
            Vector2 center = contact + CuiaView.Rotate(new Vector2(-lean * hb, 0.5f * h), rad);
            RectTransform rt = _body.rectTransform;
            var scale = new Vector3(1f, sy, 1f);
            if (rt.localScale != scale)
            {
                rt.localScale = scale;
            }

            SetBodySprite(sy < 0f ? _bodyClosed : _bodyOpen); // desde la mitad del giro, sin el hueco de la boca
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            rt.anchoredPosition = center * _u;
            float shade = 1f - (UpendShadeMax * (1f - Mathf.Abs(sy)));
            OverlayParts.SetColor(_body, new Color(shade, shade, shade, 1f));

            // La cara del extremo que se aleja (arriba en la proyección): la boca seca mientras la boca va arriba y la tapa de la
            // base después, que acaba con la proporción de la tapa en reposo (TinajaBaseCapAspect).
            bool mouthEnd = sy > 0f;
            bool cap = tumbling && (mouthEnd ? sin > 0.02f : true);
            OverlayParts.SetVisible(_upendCap, cap && mouthEnd);
            OverlayParts.SetVisible(_baseCap, cap && !mouthEnd);
            if (cap)
            {
                Image img = mouthEnd ? _upendCap : _baseCap;
                float w = mouthEnd ? 2f * ProceduralArt.TinajaOpeningRxU : 2f * hb;
                float aspect = mouthEnd ? UpendCapAspect * sin : Mathf.Max(UpendCapAspect * sin, ProceduralArt.TinajaBaseCapAspect * -sy);
                float endY = mouthEnd ? ProceduralArt.TinajaLipCenterU * sy : h;
                Vector2 end = contact + CuiaView.Rotate(new Vector2(-lean * hb, endY), rad);
                OverlayParts.Place(img, end.x, end.y, w, aspect * w, _u);
                img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                OverlayParts.SetColor(img, mouthEnd ? DryMouthRim : ColorMath.Darken(BaseCapColor, UpendShadeMax * (1f - Mathf.Abs(sy))));
            }
        }

        // Suelta con Reducir movimiento (b.2: «Fundidos de 0,25 s»): sin giro, la vasija boca arriba con el fondo seco se funde
        // en la vasija boca abajo (la copia _bodyFlip, con su tapa de la base) en ReducedFadeMs. Sin asignaciones.
        private void ApplyUpendFade(float k)
        {
            RectTransform rt = _body.rectTransform;
            if (rt.localScale != Vector3.one)
            {
                rt.localScale = Vector3.one;
            }

            SetBodySprite(_bodyOpen);
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition = new Vector2(0f, 0.5f * ProceduralArt.TinajaBodyHeightU * _u);
            OverlayParts.SetColor(_body, new Color(1f, 1f, 1f, 1f - k));
            OverlayParts.SetVisible(_mouth, true);
            OverlayParts.SetVisible(_dryCenter, true);
            OverlayParts.SetVisible(_mouthGloss, false);
            OverlayParts.SetVisible(_mouthGlyph, false);
            OverlayParts.SetColor(_mouth, OverlayParts.Fade(DryMouthRim, 1f - k));
            OverlayParts.SetColor(_dryCenter, OverlayParts.Fade(DryMouthCenter, 1f - k));
            OverlayParts.SetVisible(_upendCap, false);

            bool flip = k > 0f;
            OverlayParts.SetVisible(_bodyFlip, flip);
            if (flip)
            {
                OverlayParts.Place(_bodyFlip, 0f, 0.5f * ProceduralArt.TinajaBodyHeightU, ProceduralArt.TinajaBodyWidthU, ProceduralArt.TinajaBodyHeightU, _u);
                RectTransform ft = _bodyFlip.rectTransform;
                var down = new Vector3(1f, -1f, 1f);
                if (ft.localScale != down)
                {
                    ft.localScale = down;
                }

                OverlayParts.SetColor(_bodyFlip, new Color(1f, 1f, 1f, k));
            }

            PlaceBaseCap(flip);
            if (flip)
            {
                OverlayParts.SetColor(_baseCap, OverlayParts.Fade(BaseCapColor, k));
            }
        }

        // La tapa de la base, boca abajo y en reposo: una elipse de TinajaBaseCapAspect sobre lo alto de la vasija volteada.
        private void PlaceBaseCap(bool on)
        {
            OverlayParts.SetVisible(_baseCap, on);
            if (!on)
            {
                return;
            }

            float w = 2f * ProceduralArt.TinajaHalfWidthU(0f);
            OverlayParts.Place(_baseCap, 0f, ProceduralArt.TinajaBodyHeightU, w, ProceduralArt.TinajaBaseCapAspect * w, _u);
            _baseCap.rectTransform.localRotation = Quaternion.identity;
            OverlayParts.SetColor(_baseCap, BaseCapColor);
        }

        private void SetBodySprite(Sprite sprite)
        {
            if (_body.sprite != sprite)
            {
                _body.sprite = sprite;
            }
        }

        // El fondo seco: el disco #6B4A33 con su centro claro, desde la fruta que tenía en DryFadeMs (o ya entero).
        private void ApplyDry()
        {
            float k = _dryFadeT < 0f ? 1f : Mathf.Clamp01(_dryFadeT / DryFadeMs);
            OverlayParts.SetColor(_mouth, k >= 1f ? DryMouthRim : ColorMath.OkLabLerp(_dryFrom, DryMouthRim, k));
            OverlayParts.SetColor(_dryCenter, OverlayParts.Fade(DryMouthCenter, k));
        }

        // Las gotas de la vuelta: caen con gravedad hasta el agua de la boca y se apagan en su último cuarto. Sin asignaciones.
        private void TickDrips(float dtMs)
        {
            for (int i = 0; i < MaxDrips; i++)
            {
                if (_dripAge[i] < 0f)
                {
                    continue;
                }

                _dripAge[i] += dtMs;
                if (_dripAge[i] >= DripFallMs + DripFadeMs)
                {
                    HideDrip(i);
                    continue;
                }

                PlaceDrip(i);
            }
        }

        // Cae con gravedad hasta el centro de la cavidad y allí se apaga en DripFadeMs; su brillo, arriba a la izquierda.
        private void PlaceDrip(int i)
        {
            float t = Mathf.Min(_dripAge[i], DripFallMs) / 1000f;
            float y = _dripFrom[i].y + (_dripVy[i] * t) - (0.5f * DripGravityU * t * t);
            float size = _dripSizeU[i];
            OverlayParts.Place(_drips[i], _dripFrom[i].x, y, size, size, _u);
            float alpha = 1f - Mathf.Clamp01((_dripAge[i] - DripFallMs) / DripFadeMs);
            OverlayParts.SetColor(_drips[i], OverlayParts.Fade(_dripColor[i], alpha));
            float glint = DripGlintDp / Mathf.Max(0.0001f, _u);
            float off = 0.22f * size;
            OverlayParts.Place(_dripGlints[i], _dripFrom[i].x - off, y + off, glint, glint, _u);
            OverlayParts.SetColor(_dripGlints[i], new Color(1f, 1f, 1f, alpha));
        }

        private void HideDrip(int i)
        {
            _dripAge[i] = -1f;
            OverlayParts.SetVisible(_drips[i], false);
            OverlayParts.SetVisible(_dripGlints[i], false);
        }

        // Borra las gotas que sigan a la vista (al florecer la fruta del revelado, en la suelta, con Marea o en la intro).
        private void ClearDrips()
        {
            for (int i = 0; i < MaxDrips; i++)
            {
                if (_dripAge[i] >= 0f)
                {
                    HideDrip(i);
                }
            }
        }

        // Altura de llegada de las gotas (u desde el pie): el centro de la cavidad.
        private static float DripLandU => ProceduralArt.TinajaMouthCenterU - DripLandBelowU;

        // Deja las piezas que mueven los pasos en reposo (el siguiente Show pinta el estado).
        private void ResetStepParts()
        {
            OverlayParts.SetVisible(_ring, false);
            OverlayParts.SetVisible(_upendCap, false);
            OverlayParts.SetVisible(_baseCap, false); // Show la vuelve a poner si queda boca abajo
            OverlayParts.SetVisible(_bodyFlip, false);
            OverlayParts.SetColor(_body, Color.white);
            _chip.rectTransform.localScale = Vector3.one;
            OverlayParts.SetColor(_mouthGlyph, Color.white);
            RectTransform rt = _body.rectTransform;
            rt.localScale = Vector3.one; // SetUpsideDown (en Show) la deja exacta: ±1
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition = new Vector2(0f, 0.5f * ProceduralArt.TinajaBodyHeightU * _u);
            _mouthTint = -1;
        }

        // Intro (D-010): sin chip ni bandas hasta el primer impacto; la boca, del color que pide la intro.
        private void ApplyIntro()
        {
            if (!_introMarks)
            {
                OverlayParts.SetVisible(_chip, false);
                for (int k = 0; k < MaxBands; k++)
                {
                    OverlayParts.SetVisible(_bands[k], false);
                    OverlayParts.SetVisible(_bandLights[k], false);
                    OverlayParts.SetVisible(_bandRims[k], false);
                    OverlayParts.SetVisible(_bandGlyphs[k], false);
                }
            }

            ApplyIntroMouth();
        }

        private void ApplyIntroMouth()
        {
            if (!_mouth.gameObject.activeSelf)
            {
                return;
            }

            OverlayParts.SetColor(_mouth, _introMouth);
            _mouthTint = -1;

            // Seca (antes del chorrito): el fondo seco, con su centro claro y sin brillo; con agua, el brillo del agua.
            bool dry = _introMouth == DryMouthRim;
            OverlayParts.SetVisible(_dryCenter, dry);
            OverlayParts.SetColor(_dryCenter, DryMouthCenter);
            OverlayParts.SetVisible(_mouthGloss, !dry);
            bool glyph = _introTint > 0;
            if (glyph)
            {
                Sprite gs = Host.Art.TinajaGlyph(_introTint);
                if (_mouthGlyph.sprite != gs)
                {
                    _mouthGlyph.sprite = gs;
                }
            }

            OverlayParts.SetVisible(_mouthGlyph, glyph);
        }

        // Prepara los iconos de los 14 tintes y las cifras del chip (1 … capacidad), una sola vez, al cargar el nivel: así ningún
        // Show posterior ni ningún paso genera texturas, aunque la tinaja llegue vacía (partida reanudada tras la suelta) y Marea
        // le devuelva dosis después.
        private void Prewarm()
        {
            _prewarmed = true;
            for (int tint = Cuia.MinColor; tint <= Cuia.MaxTintColor; tint++)
            {
                Host.Art.TinajaGlyph(tint);
            }

            for (int n = 1; n <= Host.Capacity; n++)
            {
                Host.Art.MurkyBadgeDigit(n);
            }
        }

        // El chip, en dp fijos (texto de ≥ 14 sp, 07 §4.3): en el hombro de la vasija o, sin su cuerpo, en el del vidrio.
        private void PlaceChip()
        {
            RectTransform chip = _chip.rectTransform;
            float centerU = _artShown ? ChipCenterU : GlassChipCenterU(Host.Capacity);
            var chipPos = new Vector2(0f, centerU * _u);
            var chipSize = new Vector2(ProceduralArt.TinajaChipWidthDp, ProceduralArt.TinajaChipHeightDp);
            if (chip.anchoredPosition != chipPos)
            {
                chip.anchoredPosition = chipPos;
            }

            if (chip.sizeDelta != chipSize)
            {
                chip.sizeDelta = chipSize;
            }
        }

        // Vacía, boca abajo sobre el jirau (b.2 regla 8): la vasija volteada en su caja, sin boca ni chip, con el cuerpo cerrado
        // (el arco del canto del labio abajo) y, si se ve el cuerpo, la tapa de la base arriba (PlaceBaseCap, en Show).
        private void SetUpsideDown(bool on)
        {
            _upsideDown = on;
            var scale = new Vector3(1f, on ? -1f : 1f, 1f);
            RectTransform rt = _body.rectTransform;
            if (rt.localScale != scale)
            {
                rt.localScale = scale;
            }

            SetBodySprite(on ? _bodyClosed : _bodyOpen); // boca abajo, sin el hueco de la boca (revisión de TJ3)
        }

        // El jirau bajo la ranura de la tinaja y la de su cuia (la contigua de la misma fila): varas continuas, sin nada en el
        // punto medio entre las dos (veto de b.2); sin pareja conocida o en otra fila, solo bajo la de la tinaja.
        private void PlaceJirau(bool on)
        {
            ICuiaBoard? board = Host.Board;
            bool show = on && _artShown && board != null;
            if (show)
            {
                FindPartner(board!);
                if (_layoutVersion != board!.LayoutVersion)
                {
                    _layoutVersion = board.LayoutVersion;
                    LayoutJirau(board);
                }
            }

            OverlayParts.SetVisible(_jirau, show);
        }

        private void LayoutJirau(ICuiaBoard board)
        {
            int i = Host.Index;
            Vector2 foot = board.Foot(i);
            _jirauFoot = foot;
            float x0 = foot.x - (JirauTinajaExtentU * _u);
            float x1 = foot.x + (JirauTinajaExtentU * _u);
            bool pair = _partner >= 0 && SameRow(board, i, _partner);
            if (pair)
            {
                // De la tinaja (bajo su pie) a su cuia (hasta su poste, del lado contrario), por encima de las dos ranuras.
                float other = board.Foot(_partner).x;
                if (other < foot.x)
                {
                    x0 = other - (JirauCuiaExtentU * _u);
                }
                else
                {
                    x1 = other + (JirauCuiaExtentU * _u);
                }
            }

            float h = JirauHeightDp(_u);
            float top = foot.y + (JirauTopAboveFootU * _u);
            Vector2 origin = LayerOrigin;
            RectTransform rt = _jirau.rectTransform;
            var pos = new Vector2((0.5f * (x0 + x1)) - origin.x, top - (0.5f * h) - origin.y);
            var size = new Vector2(Mathf.Max(h, x1 - x0), h);
            if (rt.anchoredPosition != pos)
            {
                rt.anchoredPosition = pos;
            }

            if (rt.sizeDelta != size)
            {
                rt.sizeDelta = size;
            }

            // El 9-slice se dibuja a TinajaJirauDesignDp de alto: el multiplicador lo lleva al alto en u sin deformar las puntas.
            float mult = ProceduralArt.TinajaJirauDesignDp / Mathf.Max(1f, h);
            if (!Mathf.Approximately(_jirau.pixelsPerUnitMultiplier, mult))
            {
                _jirau.pixelsPerUnitMultiplier = mult;
            }
        }

        // Su cuia: la del tablero inicial si la pantalla la pasó (AssignPartner). Si no (bancos de prueba), la contigua de tipo
        // Cucharon (b.2 regla 1; CucharonMechanic.ValidateInitial), y se recuerda; si no la hay (la tinaja ya llega con su cuia
        // suelta), la Cuia chica de 2 de al lado, a la derecha antes que a la izquierda [P], hasta ver la de verdad (Marea la
        // vuelve a colgar).
        private void FindPartner(ICuiaBoard board)
        {
            if (_partnerSure || _partnerAssigned)
            {
                return;
            }

            int i = Host.Index;
            for (int d = 1; d >= -1; d -= 2)
            {
                int j = i + d;
                if (j >= 0 && j < board.Count && board.ShownCuia(j).Mechanic == MechanicType.Cucharon)
                {
                    SetPartner(j, true);
                    return;
                }
            }

            if (_partner >= 0)
            {
                return;
            }

            for (int d = 1; d >= -1; d -= 2)
            {
                int j = i + d;
                if (j >= 0 && j < board.Count && board.ShownCuia(j).Capacity == LevelValidation.CucharonCapacity)
                {
                    SetPartner(j, false);
                    return;
                }
            }
        }

        private void SetPartner(int j, bool sure)
        {
            if (_partner != j)
            {
                _layoutVersion = -1;
            }

            _partner = j;
            _partnerSure = sure;
        }

        private static bool SameRow(ICuiaBoard board, int a, int b)
        {
            return Mathf.Abs(board.Foot(a).y - board.Foot(b).y) < 0.5f;
        }

        private void PlaceChipGlyph(Image glyph, float xDp)
        {
            RectTransform rt = glyph.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(xDp, 0f);
            rt.sizeDelta = new Vector2(ChipGlyphStepDp, ProceduralArt.MurkyBadgeDp * ProceduralArt.TinajaChipGlyphScale);
        }

        private Rect RectU(RectTransform rt)
        {
            Rect r = RectDp(rt);
            float u = Mathf.Max(0.0001f, _u);
            return new Rect(r.x / u, r.y / u, r.width / u, r.height / u);
        }

        private static Rect RectDp(RectTransform rt)
        {
            Vector2 size = rt.sizeDelta;
            Vector2 pos = rt.anchoredPosition;
            return new Rect(pos.x - (size.x * rt.pivot.x), pos.y - (size.y * rt.pivot.y), size.x, size.y);
        }

        private static bool[] BuildBandRims()
        {
            var rims = new bool[Cuia.MaxTintColor + 1];
            for (int t = Cuia.MinColor; t <= Cuia.MaxTintColor; t++)
            {
                Color tint = Palette.Tint(t);
                for (int b = 0; b < BandBodyTones.Length; b++)
                {
                    if (ColorMath.DeltaE00(tint, BandBodyTones[b]) < BandRimMinDeltaE)
                    {
                        rims[t] = true;
                    }
                }
            }

            return rims;
        }

        // Origen de la raíz del jirau en el espacio de las capas de tablero (el de ICuiaBoard).
        private Vector2 LayerOrigin => _layerRoot != null ? (Vector2)_layerRoot.localPosition : Vector2.zero;

        /// <summary>
        /// Une la raíz del jirau (en la capa del tablero, fuera de la cuia) a la vida de la cuia: cuando la cuia se destruye
        /// (cambio de nivel), el jirau se va con ella en el fotograma siguiente, antes de pintarse (como el canal del Remolino).
        /// </summary>
        private sealed class LayerLink : MonoBehaviour
        {
            public Transform? Owner;

            private void LateUpdate()
            {
                if (Owner == null)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
