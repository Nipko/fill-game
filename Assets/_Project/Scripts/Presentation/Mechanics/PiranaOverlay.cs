#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M7 Piraña: la cueva y su cardumen (02 §9.1 M7; propuesta de D-007 b.2 M7; respuesta Q-C; Q18 B) [P]. Sobre la boca de
    /// la cueva, un arco de piedras grises con musgo y el hueco en sombra, con la fila de <c>k</c> puntos (llenos los vertidos
    /// útiles ya contados; los mismos campos que el contador del HUD, <c>LevelCounter.Of</c>). Encima, un cardumen de
    /// <see cref="SchoolSize"/> pirañas pequeñas, plateadas y sin dientes exagerados ni sangre, que ronda la cueva. Con la
    /// cueva llena (el próximo mordisco se perdería, Q-C) el cardumen duerme atenuado y el arco se atenúa. En vuelo se ocultan
    /// el arco y los puntos (cuelgan del cuerpo); el cardumen se queda.
    /// </summary>
    /// <remarks>
    /// <para>Cardumen (arquitectura del paso 1 §6): se crea una vez en <see cref="OnAttach"/> bajo
    /// <c>Host.BoardFrontLayer</c>, en un Canvas anidado propio (se mueve cada fotograma sin reconstruir el lienzo del tablero,
    /// guía 8), y se recoloca en <see cref="OnBoardLayout"/> con <c>Host.Board.BodyCenter(Index)</c>: ronda por encima del
    /// arco, dentro del ancho de su ranura, así que no tapa ninguna cuia, y no captura toques (<c>raycastTarget</c> = false,
    /// sin <c>GraphicRaycaster</c>). Las pirañas se anclan arriba a la izquierda del tablero, como las cuias y los efectos:
    /// las posiciones de la capa (centradas) se pasan a ese espacio en el mismo instante en que se leen, así que un cambio
    /// tardío del tamaño del tablero (la escala del lienzo se resuelve al pintar) las mueve igual que a las cuias. Con
    /// Reducir movimiento (<c>IBoardFx.ReduceMotion</c>) se queda quieto. Muere con la vista de la cueva (un enlace en su
    /// ranura lo destruye).</para>
    /// <para>Pasos de la etapa (e), despachados a la cueva (<c>Index</c> = la cueva, <c>OtherIndex</c> = la víctima):</para>
    /// <list type="bullet">
    /// <item><description><see cref="MechanicStepKind.Bite"/> (<see cref="BiteStepMs"/> × <c>ctx.SpeedFactor</c>, [P] b.2): el
    /// cardumen sale de la cueva y nada en arco hasta la boca de la víctima (<c>ctx.OtherPos</c>), que conserva la dosis de
    /// arriba (el vertido acabó en <c>ctx.OtherBefore</c>). La que va delante baja el morro a la boca y el bocado SALE de esa
    /// dosis: una cápsula del tinte, con su patrón y un contorno de vidrio, sube por el eje de la víctima hasta su boca (al
    /// atraparlo, unos destellos del tinte), mientras la dosis se vacía (desde la mitad del paso: a mitad, la víctima aún la
    /// tiene). El cardumen se lleva el bocado a la cueva, lo suelta sobre su eje y el bocado entra recto por la boca y se
    /// apaga al cruzar el labio, cuando empieza a formarse la dosis de arriba de la cueva (con su salpicadura). Las dos cuias
    /// se pintan con <c>ctx.Board.SetDose</c> y, al terminar, con <c>ShowContent</c> de <c>ctx.After</c> y
    /// <c>ctx.OtherAfter</c>.</description></item>
    /// <item><description><see cref="MechanicStepKind.BiteLost"/> (<see cref="BiteLostStepMs"/> [P]; víctima sellada, Matapi o
    /// la propia cueva; decisión C + D del dueño para el 183, mordisco fallido visible): el cardumen sale de la cueva y nada en
    /// arco hasta la boca de la víctima; la que va delante se lanza de morros contra su tapa (o el embudo del Matapi), choca
    /// en <see cref="LostHitAt"/> (su boca se hunde <see cref="LostDentDp"/> en la tapa, se aplasta unos 60 ms y salta un
    /// destello perla de <see cref="ImpactFlashDp"/> con unos destellos del pool, sin rojo), sale rebotada hacia atrás y el
    /// cardumen vuelve a su ronda sin cambiar nada; las otras dos esperan a los lados de la tapa, bajas, sin subir hacia la
    /// fila de arriba y sin salirse del panel (<see cref="LostPanelClearDp"/> del marco, también con la víctima en una punta
    /// de la fila). Con la cueva llena (respuesta Q-C) el cardumen, que duerme, se despierta, la que va delante se asoma hacia
    /// la víctima (<see cref="PeekReachU"/> hacia su lado o, si está encima en la misma columna, hacia arriba con el morro
    /// arriba, <see cref="PeekRiseU"/> sin pasar de la repisa de arriba) y vuelve, y se duermen otra vez.</description></item>
    /// </list>
    /// <para>Los puntos se llenan todos al empezar (el vertido que completa el ritmo) y vuelven a los de <c>After</c> con el
    /// mordisco o el mordisco perdido; los vacíos son el aro a <see cref="EmptyPipAlpha"/> (se distinguen por contraste a 48 dp, no por
    /// un hueco de menos de 2 dp). Sin asignaciones en los pasos ni en <see cref="Tick"/>. La Piraña queda pendiente de
    /// revisión del asesor cultural.</para>
    /// </remarks>
    public sealed class PiranaOverlay : CuiaOverlay
    {
        /// <summary>Opacidad con la cueva llena (<c>LevelCounter.Blocked</c>): el cardumen duerme atenuado.</summary>
        public const float DimAlpha = 0.5f;

        /// <summary>Mordisco: 500 ms [P, b.2 M7].</summary>
        public const float BiteStepMs = 500f;

        /// <summary>Mordisco perdido (sale, choca con la tapa o el embudo y vuelve; o, con la cueva llena, se asoma): 400 ms [P].</summary>
        public const float BiteLostStepMs = 400f;

        /// <summary>Pirañas del cardumen (b.2 M7).</summary>
        public const int SchoolSize = 3;

        /// <summary>Vuelta del cardumen alrededor de su sitio (ms) [P].</summary>
        public const float IdlePeriodMs = 3600f;

        /// <summary>Centro del arco de piedras sobre el canto de la boca (u): la base del hueco queda 0,06 u bajo la boca.</summary>
        public const float CaveCenterAboveRimU = 0.415f;

        /// <summary>Hueco entre lo alto del arco y el centro del cardumen (u).</summary>
        public const float SchoolAboveCaveU = 0.42f;

        /// <summary>Lado de los puntos del contador en el hueco de la cueva (u; ≈ 7 dp con la cuia de 48 dp).</summary>
        public const float PipU = 0.28f;

        /// <summary>Paso entre los centros de los puntos (u); con k ≥ 5 se juntan para seguir dentro del hueco.</summary>
        public const float PipStepU = 0.31f;

        /// <summary>
        /// Opacidad de los puntos vacíos (el aro) frente a los llenos, en el hueco de la cueva. Perla sobre sombra: con la mezcla
        /// en lineal del proyecto, al 0,2 el aro queda a la mitad de brillo que el punto lleno (≈ 2,8:1 frente al hueco, el lleno
        /// ≈ 9,8:1), así que el vertido que falta se ve tenue y no compite con los contados (revisión de T17).
        /// </summary>
        public const float EmptyPipAlpha = 0.2f;

        /// <summary>Fracción del paso del mordisco en que el bocado empieza a salir de la dosis de la víctima.</summary>
        public const float MorselRiseStart = 0.45f;

        /// <summary>Fracción del paso en que la víctima empieza a perder la dosis: hasta la mitad, la conserva entera.</summary>
        public const float DrainStart = 0.5f;

        /// <summary>Fracción del paso en que la víctima ya no tiene la dosis (120 ms con 500 ms).</summary>
        public const float DrainEnd = 0.74f;

        /// <summary>Fracción del paso en que el bocado cruza el labio de la cueva y se apaga.</summary>
        public const float MorselGone = 0.93f;

        /// <summary>Mordisco perdido: fracción del paso en que el cardumen llega sobre la boca de la víctima.</summary>
        public const float LostReachEnd = 0.38f;

        /// <summary>Mordisco perdido: fracción del paso en que la que va delante choca con la tapa o el embudo (≈ 190 ms de 400).</summary>
        public const float LostHitAt = 0.47f;

        /// <summary>
        /// Mordisco perdido: hasta aquí (fracción del paso) la que va delante sigue contra la tapa, aplastada, con el destello
        /// en su punto máximo (14 ms con 400 ms): el golpe se ve antes del rebote.
        /// </summary>
        public const float LostHoldEnd = LostHitAt + 0.035f;

        /// <summary>Mordisco perdido: fin del rebote (fracción del paso); después el cardumen vuelve a la cueva.</summary>
        public const float LostReboundEnd = 0.62f;

        /// <summary>Lo alto de la tapa de sellado (y del embudo del Matapi) sobre lo alto de la cuia (u): donde choca la piraña.</summary>
        public const float LidAboveRimU = 0.3f;

        /// <summary>Cueva llena: lo que se asoma hacia la víctima la que va delante (u) [P].</summary>
        public const float PeekReachU = 1.2f;

        /// <summary>
        /// Cueva llena con la víctima encima, en la misma columna: lo que sube la que va delante, como mucho (u) [P]; sin pasar
        /// de la repisa de la fila de arriba (<see cref="PeekShelfClearDp"/> bajo su canto inferior).
        /// </summary>
        public const float PeekRiseU = 1.0f;

        /// <summary>Aire entre lo alto de la que se asoma hacia arriba y el canto inferior de la repisa de arriba (dp) [P].</summary>
        public const float PeekShelfClearDp = 2f;

        /// <summary>Morro arriba de la que se asoma hacia una víctima que está encima (grados) [P].</summary>
        public const float PeekUpTiltDeg = 30f;

        /// <summary>
        /// Mordisco perdido: lo que se hunde la boca de la que va delante en la tapa (o el embudo) al chocar (dp) [P]: el golpe
        /// se ve como contacto, no como un amago a 2 dp.
        /// </summary>
        public const float LostDentDp = 1.5f;

        /// <summary>
        /// Mordisco perdido: lado del destello del golpe en su punto máximo (dp) [P]: una estrella perla, sin rojo, de puntas
        /// finas (<c>ProceduralArt.StarSparkle</c>), cuyo cuerpo mide algo más de la mitad del lado, así que se ve de unos 10-11
        /// dp, ≥ 8-10 dp a tamaño de teléfono.
        /// </summary>
        public const float ImpactFlashDp = 20f;

        /// <summary>
        /// Mordisco perdido: margen de las pirañas hasta el canto interior del marco del panel (dp) [P]: con la víctima en la
        /// primera o la última columna, las que esperan a los lados de la tapa no se salen del panel.
        /// </summary>
        public const float LostPanelClearDp = 3f;

        /// <summary>Cueva llena: fin del despertar (fracción del paso).</summary>
        public const float PeekWakeEnd = 0.15f;

        /// <summary>Cueva llena: se ha asomado del todo (fracción del paso).</summary>
        public const float PeekOutEnd = 0.42f;

        /// <summary>Cueva llena: deja de mirar y vuelve (fracción del paso).</summary>
        public const float PeekHoldEnd = 0.6f;

        /// <summary>Cueva llena: de vuelta en su sitio; después se duerme otra vez (fracción del paso).</summary>
        public const float PeekBackEnd = 0.85f;

        /// <summary>La piraña que va delante: la que muerde, la que choca y la que se asoma.</summary>
        public const int LeadFish = 1;

        // La piraña que va delante muerde.
        private const int Lead = LeadFish;

        // Ciclo común de la vuelta y del balanceo (ms): múltiplo de 3.600 y de 2.800, así el reloj se recorta sin saltos.
        private const float ClockWrapMs = 50400f;
        private const float BobPeriodMs = 2800f;

        // Fases del mordisco (fracción del paso): llega a la víctima, el bocado sube hasta la boca de la que muerde
        // (MorselRiseStart → CarryStart), se lo lleva a la cueva, lo suelta sobre su eje (CarryEnd → OverCaveEnd), entra recto
        // por la boca (→ MorselGone) y la dosis de la cueva se forma desde FillStart.
        private const float JoinEnd = 0.12f;
        private const float ReachEnd = 0.40f;
        private const float BiteAt = 0.53f;
        private const float CarryStart = 0.62f;
        private const float CarryEnd = 0.86f;
        private const float OverCaveEnd = 0.90f;
        private const float FillStart = 0.92f;

        // El bocado nace a este tamaño en la superficie de la dosis y crece hasta 1 en la boca; su centro sale este tanto (u)
        // por debajo de lo alto de la dosis, y se suelta este tanto (u) sobre la boca de la cueva.
        private const float MorselStartScale = 0.4f;
        private const float MorselStartBelowTopU = 0.2f;
        private const float MorselOverCaveU = 0.3f;

        // Destellos al atrapar el bocado [P] (del tope común de 44 partículas y 8 aditivas), a tres cuartos de su tamaño: un
        // acento junto al pez, no una celebración.
        private const int CatchSparkles = 4;
        private const float CatchSparkleScale = 0.75f;

        // Patrón del bocado: un rectángulo que cabe dentro de la cápsula (u) [P].
        private const float MorselPatternWidthU = 0.62f;
        private const float MorselPatternHeightU = 0.36f;

        // Fila de puntos: centro sobre la boca (u; 0,16 u sobre la base del hueco) y ancho útil del hueco a esa altura (u).
        private const float PipRowAboveRimU = 0.10f;
        private const float PipRowMaxU = 1.29f;

        // El contador vuelve a empezar a mitad del mordisco perdido; morros abajo de la que choca (grados) y destellos del golpe.
        // El cardumen se queda bajo, sobre la tapa (u), y las otras dos se apartan a los lados de la tapa (u).
        private const float LostAboveLidU = 0.3f;
        private const float LostAsideXU = 0.85f;
        private const float LostAsideYU = 0.2f;
        private const float LostPipsAt = 0.5f;
        private const float HitTiltDeg = 35f;
        private const int BumpSparkles = 4;
        private const float BumpSparkleScale = 0.85f; // destellos de 0,45-0,70 × 0,85 u: 8-14 dp con u = 22

        // El golpe (fracción del paso desde LostHitAt): la que va delante se aplasta a lo largo de su cuerpo deprisa (12 ms,
        // hasta el máximo a LostHitAt + 0,03, mientras sigue contra la tapa) y se recupera despacio (hasta los 60 ms); el
        // destello crece en 12 ms, se queda y se apaga hacia los 64 ms.
        private const float SquashIn = 0.03f;
        private const float SquashSpan = 0.15f;
        private const float SquashAlong = 0.16f;
        private const float SquashAcross = 0.12f;
        private const float FlashGrowEnd = 0.03f;
        private const float FlashHoldEnd = 0.07f;
        private const float FlashGoneAt = 0.16f;

        // El destello va un poco por encima del punto del golpe: sobre la tapa clara, una estrella perla apenas se ve.
        private const float FlashAboveLidDp = 2.5f;

        // Canto interior del marco del panel desde el borde de las ranuras de la fila más ancha (dp): 8 de margen del panel +
        // 13,5 de marco − 12 de margen lateral de las ranuras (BoardLayout). Con el marco adelgazado, el canto queda más fuera.
        private const float PanelInsetFromSlotsDp = 9.5f;

        // Canto inferior de la madera de una repisa bajo la línea de pie de su fila (dp; ProceduralArt.BuildShelf).
        private const float ShelfBottomBelowFootDp = 17f;

        // Contorno del bocado: vidrio_borde al 90 % (se lee con cualquier tinte sobre el panel).
        private static readonly Color MorselRimColor = new Color(Palette.GlassRim.r, Palette.GlassRim.g, Palette.GlassRim.b, 0.9f);

        // Sitio de cada piraña quieta (dormida o con Reducir movimiento), en u desde el centro del cardumen, y hacia dónde mira
        // (+1 a la izquierda).
        private static readonly float[] RestX = { -0.5f, 0.02f, 0.52f };
        private static readonly float[] RestY = { -0.06f, 0.08f, -0.03f };
        private static readonly float[] RestFacing = { 1f, -1f, 1f };

        private readonly Image[] _fish = new Image[SchoolSize];
        private readonly Vector2[] _fishPos = new Vector2[SchoolSize];
        private readonly Vector2[] _start = new Vector2[SchoolSize];
        private Image _arch = null!;
        private Image _morselRim = null!;
        private Image _morsel = null!;
        private RawImage _morselPattern = null!;
        private Image _glint = null!;
        private Image _impact = null!;
        private PipRow _pips = null!;
        private RectTransform _school = null!;
        private bool _on;
        private bool _inFlight;
        private bool _dimmed;
        private int _pipK = -1;
        private float _u = 24f;
        private float _heightU = CuiaGeometry.HeightU(2);
        private Vector2 _home;
        private float _clock;

        // Paso en curso.
        private bool _animating;
        private bool _fresh;
        private MechanicStepKind _kind;
        private float _t;
        private float _dur;
        private ICuiaBoard? _board;
        private int _index;
        private int _victim;
        private Cuia _after;
        private bool _afterSealed;
        private Cuia _otherAfter;
        private bool _otherAfterSealed;
        private int _afterK;
        private int _afterCounter;
        private int _victimDose = -1;
        private int _caveDose = -1;
        private int _tint;
        private Color _color;
        private Color _patternColor;
        private bool _patternOn;
        private int _patternTint = -1;
        private float _carryFacing = 1f;
        private Vector2 _target;
        private Vector2 _cave;
        private Vector2 _caveSurface;
        private Vector2 _caveTop;
        private Vector2 _caveLip;
        private Vector2 _morselFrom;
        private Vector2 _morselDrop;
        private Vector2 _morselPos;
        private float _morselScale = 1f;
        private bool _sleepy;
        private bool _awake;
        private bool _bumped;
        private Vector2 _lid;
        private Vector2 _leadMouth;
        private float _peekDir = 1f;
        private bool _peekUp;
        private float _peekRise;
        private float _lostMinX = float.NegativeInfinity;
        private float _lostMaxX = float.PositiveInfinity;
        private bool _caught;
        private bool _bitten;
        private bool _drained;
        private bool _splashed;
        private bool _pipsReset;
        private IBoardFx? _fx;
        private bool _reduceMotion;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Pirana;

        /// <inheritdoc />
        public override bool IsAnimating => _animating;

        /// <summary>Puntos del contador: el ritmo <c>k</c>.</summary>
        public int PipCount => _on ? _pips.Count : 0;

        /// <summary>Puntos llenos: vertidos útiles contados desde el último mordisco.</summary>
        public int FilledPips => _on ? _pips.Filled : 0;

        /// <summary>Fila de puntos del contador (tests: tamaño y opacidad de cada punto).</summary>
        public PipRow Pips => _pips;

        /// <summary>Atenuada: la cueva está llena y el cardumen duerme (respuesta Q-C).</summary>
        public bool Dimmed => _on && _dimmed;

        /// <summary>Opacidad actual del arco de piedras.</summary>
        public float StonesAlpha => _arch.color.a;

        /// <summary>Raíz del cardumen (en la capa de tablero delantera, con su Canvas anidado).</summary>
        public RectTransform School => _school;

        /// <summary>Sitio del cardumen, en el espacio de las capas de tablero: por encima del arco de la cueva.</summary>
        public Vector2 SchoolHome => ToLayer(_home);

        /// <summary>Centro actual del cardumen (media de las pirañas), en el espacio de las capas de tablero.</summary>
        public Vector2 SchoolCenter
        {
            get
            {
                Vector2 sum = Vector2.zero;
                for (int i = 0; i < SchoolSize; i++)
                {
                    sum += _fishPos[i];
                }

                return ToLayer(sum / SchoolSize);
            }
        }

        /// <summary>El cardumen está a la vista.</summary>
        public bool SchoolVisible => _on && _school.gameObject.activeSelf;

        /// <summary>Las pirañas del cardumen tienen los ojos cerrados (duermen).</summary>
        public bool SchoolAsleep
        {
            get
            {
                Sprite asleep = Host.Art.PiranaFishAsleepSprite;
                for (int i = 0; i < SchoolSize; i++)
                {
                    if (_fish[i].sprite != asleep)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>El bocado que se lleva el cardumen está a la vista.</summary>
        public bool MorselVisible => _morsel.color.a > 0.01f;

        /// <summary>Opacidad del bocado (su cápsula del tinte).</summary>
        public float MorselAlpha => _morsel.color.a;

        /// <summary>Centro del bocado, en el espacio de las capas de tablero.</summary>
        public Vector2 MorselCenter => ToLayer(_morselPos);

        /// <summary>Escala del bocado (crece de 0,4 a 1 mientras sale de la dosis).</summary>
        public float MorselScale => _morselScale;

        /// <summary>El bocado lleva el patrón de la fruta mordida (07 §3), en el tinte de sus marcas.</summary>
        public bool MorselPatterned => _patternOn && _morselPattern.color.a > 0.01f;

        /// <summary>Imagen del contorno del bocado (tests: color y tamaño).</summary>
        public Image MorselRim => _morselRim;

        /// <summary>
        /// Mordisco perdido en curso: el punto de la tapa (o del embudo) de la víctima donde choca la que va delante, en el
        /// espacio de las capas de tablero.
        /// </summary>
        public Vector2 LostContact => ToLayer(_lid);

        /// <summary>Boca de la que va delante en el último fotograma del paso, en el espacio de las capas de tablero.</summary>
        public Vector2 LeadMouth => ToLayer(_leadMouth);

        /// <summary>En el mordisco perdido en curso, la que va delante ya chocó con la tapa o el embudo de la víctima.</summary>
        public bool LostBumped => _animating && _bumped;

        /// <summary>
        /// Cueva llena con la víctima encima, en la misma columna: la que va delante se asoma hacia arriba (sube con el morro
        /// arriba) en vez de hacia un lado.
        /// </summary>
        public bool PeekUp => _peekUp;

        /// <summary>Lado actual del destello del golpe del mordisco perdido (dp; 0 si no se ve).</summary>
        public float ImpactFlashSize => _impact.color.a > 0.01f ? _impact.rectTransform.sizeDelta.x * _impact.rectTransform.localScale.x : 0f;

        /// <summary>Imagen de la piraña <paramref name="i"/> (tests).</summary>
        public Image Fish(int i)
        {
            return _fish[i];
        }

        /// <summary>
        /// Rectángulo que ocupa la piraña <paramref name="i"/> (su sprite con su escala y su giro), en el espacio de las capas
        /// de tablero (tests: dentro del panel, bajo la repisa de arriba).
        /// </summary>
        public Rect FishLayerRect(int i)
        {
            RectTransform rt = _fish[i].rectTransform;
            Vector2 size = rt.sizeDelta;
            Vector3 s = rt.localScale;
            float a = rt.localEulerAngles.z * Mathf.Deg2Rad;
            float cos = Mathf.Abs(Mathf.Cos(a));
            float sin = Mathf.Abs(Mathf.Sin(a));
            float w = Mathf.Abs(size.x * s.x);
            float h = Mathf.Abs(size.y * s.y);
            var half = new Vector2(0.5f * ((w * cos) + (h * sin)), 0.5f * ((w * sin) + (h * cos)));
            Vector2 c = ToLayer(_fishPos[i]);
            return new Rect(c - half, 2f * half);
        }

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            _on = cuia.Mechanic == MechanicType.Pirana;
            Visible = _on;
            if (_on)
            {
                int k = cuia.MechanicK;
                if (k != _pipK)
                {
                    _pipK = k;
                    LayoutPips();
                }

                _pips.Set(k, cuia.MechanicCounter);
                bool dim = cuia.IsFull;
                if (dim != _dimmed)
                {
                    _dimmed = dim;
                    OverlayParts.SetColor(_arch, new Color(1f, 1f, 1f, dim ? DimAlpha : 1f));
                }

                PaintPips();
            }

            Apply();
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            _heightU = heightU;
            OverlayParts.Place(_arch, 0f, heightU + CaveCenterAboveRimU, ProceduralArt.PiranaCaveWidthU, ProceduralArt.PiranaCaveHeightU, u);
            LayoutPips();
            var fishSize = new Vector2(ProceduralArt.PiranaFishWidthU * u, ProceduralArt.PiranaFishHeightU * u);
            for (int i = 0; i < SchoolSize; i++)
            {
                SetSize(_fish[i].rectTransform, fishSize);
            }

            var morselSize = new Vector2(ProceduralArt.PiranaMorselWidthU * u, ProceduralArt.PiranaMorselHeightU * u);
            SetSize(_morselRim.rectTransform, morselSize);
            SetSize(_morsel.rectTransform, morselSize);
            SetSize(_morselPattern.rectTransform, new Vector2(MorselPatternWidthU * u, MorselPatternHeightU * u));
            SetSize(_glint.rectTransform, new Vector2(0.26f * u, 0.12f * u));
            RecomputeHome();
            if (!_animating)
            {
                PlaceIdle();
            }
        }

        /// <inheritdoc />
        public override void OnBoardLayout()
        {
            RecomputeHome();
            if (!_animating)
            {
                PlaceIdle();
            }
        }

        /// <inheritdoc />
        public override void SetInFlight(bool inFlight)
        {
            _inFlight = inFlight;
            Apply();
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (!_on)
            {
                return;
            }

            if (!ReduceMotion)
            {
                _clock += dtMs;
                if (_clock >= ClockWrapMs)
                {
                    _clock -= ClockWrapMs;
                }
            }

            if (!_animating)
            {
                PlaceIdle();
                return;
            }

            // El primer fotograma del paso no avanza: la animación nunca va por delante del paso de la cola.
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
        public override float StepDurationMs(MechanicStepKind kind)
        {
            switch (kind)
            {
                case MechanicStepKind.Bite:
                    return BiteStepMs;
                case MechanicStepKind.BiteLost:
                    return BiteLostStepMs;
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

            _kind = ctx.Kind;
            _board = ctx.Board;
            _index = ctx.Index;
            _victim = ctx.OtherIndex;
            _after = ctx.After;
            _afterSealed = ctx.AfterSealed;
            _otherAfter = ctx.OtherAfter;
            _otherAfterSealed = ctx.OtherAfterSealed;
            _afterK = ctx.After.MechanicK;
            _afterCounter = ctx.After.MechanicCounter;
            _fx = ctx.Fx;
            _reduceMotion = ctx.ReduceMotion;
            _dur = nominal * Mathf.Max(0.01f, ctx.SpeedFactor);
            _t = 0f;
            _fresh = true;
            _animating = true;
            _caught = false;
            _bitten = false;
            _drained = false;
            _splashed = false;
            _pipsReset = false;
            _bumped = false;
            _sleepy = ctx.Before.IsFull;
            _awake = !_sleepy;
            int k = ctx.Before.MechanicK;
            _pips.Set(k, k); // el vertido que completa el ritmo llena todos los puntos
            PaintPips();
            for (int i = 0; i < SchoolSize; i++)
            {
                _start[i] = _fishPos[i];
            }

            // Las posiciones del contexto (capa, centradas) pasan al espacio del cardumen en este mismo instante.
            bool hasVictim = _victim >= 0 && _victim != _index;
            float victimHeight = CuiaGeometry.HeightU(ctx.OtherBefore.Capacity > 0 ? ctx.OtherBefore.Capacity : 4);
            Vector2 self = ToSchool(ctx.SelfPos);
            Vector2 other = ToSchool(ctx.OtherPos);
            _target = hasVictim ? other + new Vector2(0f, (victimHeight + 0.55f) * _u) : _home + new Vector2(0f, 0.45f * _u);
            _cave = self + new Vector2(0f, (_heightU + 0.42f) * _u);
            _carryFacing = FacingFrom(_target, _cave);

            // Mordisco perdido: choca con la tapa (o el embudo) de la víctima; si la víctima es la propia cueva, con lo alto de su
            // arco de piedras. Con la cueva llena, se asoma hacia la víctima (o hacia donde mira, sin víctima).
            float archTop = CaveCenterAboveRimU + (0.5f * ProceduralArt.PiranaCaveHeightU);
            _lid = hasVictim ? other + new Vector2(0f, (victimHeight + LidAboveRimU) * _u) : self + new Vector2(0f, (_heightU + archTop) * _u);
            if (_kind == MechanicStepKind.BiteLost)
            {
                // Bajo, justo sobre la tapa: entre lo alto de una cuia y la repisa de la fila de arriba solo hay 23 dp.
                _target = _lid + new Vector2(0f, LostAboveLidU * _u);
            }

            // Con la víctima en la misma columna (en el 183, la cueva está debajo de dos víctimas posibles), la que va delante se
            // asoma hacia arriba si la víctima está encima; si no, hacia el lado de la víctima (o hacia donde mira, sin víctima).
            bool sameColumn = hasVictim && Mathf.Abs(other.x - self.x) <= 0.5f;
            _peekUp = sameColumn && other.y > self.y;
            _peekDir = hasVictim && !sameColumn ? Mathf.Sign(other.x - self.x) : -RestFacing[Lead];
            _peekRise = _peekUp ? PeekRise(ctx.Board ?? Host.Board, ctx.SelfPos) : 0f;
            PanelSpan(ctx.Board ?? Host.Board);
            _leadMouth = _home;
            _victimDose = -1;
            _caveDose = -1;
            if (_kind == MechanicStepKind.Bite && hasVictim)
            {
                _tint = ctx.Tint;
                _color = Palette.Tint(_tint);
                SetMorselTint(_tint);
                _victimDose = ctx.OtherBefore.Count - 1;
                _caveDose = ctx.Before.Count;
                float doseTop = CuiaGeometry.CavityBottomU + _victimDose + 1f;
                _morselFrom = other + new Vector2(0f, (doseTop - MorselStartBelowTopU) * _u);
                _caveSurface = self + new Vector2(0f, (CuiaGeometry.CavityBottomU + _caveDose) * _u);
                _morselDrop = Mouth(Formation(Lead, _cave, _carryFacing), _carryFacing, 0f);
                _caveTop = self + new Vector2(0f, (_heightU + MorselOverCaveU) * _u);
                _caveLip = self + new Vector2(0f, _heightU * _u);
                if (_board != null)
                {
                    PaintVictim(1f);
                    PaintCave(0f);
                }
            }

            HideMorsel();
            Pose(0f);
            return _dur;
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            FinishStep(true);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(ProceduralArt.StoneLight); // piedras
            into.Add(ProceduralArt.StoneDark);
            into.Add(ProceduralArt.PiranaStoneGlint);
            into.Add(ProceduralArt.PiranaCaveDark); // hueco de la cueva
            into.Add(ProceduralArt.PiranaCaveFloor);
            into.Add(ProceduralArt.PiranaMoss);
            into.Add(ProceduralArt.PiranaMossLight);
            into.Add(ProceduralArt.PiranaFishSilver); // pirañas
            into.Add(ProceduralArt.PiranaFishBelly);
            into.Add(ProceduralArt.PiranaFishBack);
            into.Add(ProceduralArt.PiranaFishFin);
            into.Add(ProceduralArt.Slate); // contorno de las piedras
            into.Add(ProceduralArt.Pearl); // puntos del contador (el bocado lleva la fruta a propósito)
            // El punto vacío sobre el hueco (mezcla en sRGB y en lineal, como la pinta la UI) y el lleno con la cueva llena.
            into.Add(Color.Lerp(ProceduralArt.PiranaCaveDark, ProceduralArt.Pearl, EmptyPipAlpha));
            into.Add(Color.Lerp(ProceduralArt.PiranaCaveDark.linear, ProceduralArt.Pearl.linear, EmptyPipAlpha).gamma);
            into.Add(Color.Lerp(ProceduralArt.PiranaCaveFloor.linear, ProceduralArt.Pearl.linear, EmptyPipAlpha).gamma);
            into.Add(Color.Lerp(ProceduralArt.PiranaCaveDark.linear, ProceduralArt.Pearl.linear, DimAlpha).gamma);
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            ProceduralArt art = Host.Art;
            _arch = OverlayParts.NewImage("PiranaCave", Host.BodyFront, art.PiranaCaveSprite, Color.white);
            _pips = new PipRow("PiranaPip", Host.BodyFront, art.CounterPip, art.CounterPipEmpty, ProceduralArt.Pearl);

            // El cardumen, en la capa delantera del tablero con su Canvas anidado (sin GraphicRaycaster: no recibe toques).
            RectTransform layer = Host.BoardFrontLayer ?? Host.SlotFront;
            var go = new GameObject("PiranaSchool" + Host.Index, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(layer, false);
            _school = (RectTransform)go.transform;
            _school.anchorMin = Vector2.zero;
            _school.anchorMax = Vector2.one;
            _school.pivot = new Vector2(0.5f, 0.5f);
            _school.offsetMin = Vector2.zero;
            _school.offsetMax = Vector2.zero;
            for (int i = 0; i < SchoolSize; i++)
            {
                _fish[i] = SchoolImage("PiranaFish" + i, art.PiranaFishSprite, Color.white);
            }

            // El bocado, delante de las pirañas: contorno de vidrio, cápsula del tinte, marcas del patrón y brillo.
            _morselRim = SchoolImage("PiranaMorselRim", art.PiranaMorselRimSprite, new Color(1f, 1f, 1f, 0f));
            _morsel = SchoolImage("PiranaMorsel", art.PiranaMorselSprite, new Color(1f, 1f, 1f, 0f));
            _morselPattern = SchoolPattern("PiranaMorselPattern", art.PatternAtlas);
            _glint = SchoolImage("PiranaMorselGlint", art.CounterPip, new Color(1f, 1f, 1f, 0f));

            // El destello del golpe del mordisco perdido, delante de todo (una estrella perla de tamaño fijo en dp).
            _impact = SchoolImage("PiranaImpactFlash", art.StarSparkle, new Color(ProceduralArt.Pearl.r, ProceduralArt.Pearl.g, ProceduralArt.Pearl.b, 0f));
            SetSize(_impact.rectTransform, new Vector2(ImpactFlashDp, ImpactFlashDp));
            go.SetActive(false);

            // El cardumen vive en una capa que no es de la cuia: un enlace en su ranura lo destruye con la vista.
            var link = new GameObject("PiranaSchoolLink");
            link.transform.SetParent(Host.SlotBack, false);
            link.AddComponent<SchoolLink>().School = go;
            PaintPips();
        }

        private Image SchoolImage(string name, Sprite sprite, Color color)
        {
            Image img = OverlayParts.NewImage(name, _school, sprite, color);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f); // arriba a la izquierda, como las cuias y los efectos
            rt.anchorMax = new Vector2(0f, 1f);
            img.canvasRenderer.cullTransparentMesh = true;
            img.gameObject.SetActive(true);
            return img;
        }

        // Las marcas del patrón del bocado: el atlas de patrones (07 §3) con la fila del tinte; se muestra con el alfa.
        private RawImage SchoolPattern(string name, Texture2D atlas)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(_school, false);
            RawImage img = go.GetComponent<RawImage>();
            img.texture = atlas;
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            img.canvasRenderer.cullTransparentMesh = true;
            return img;
        }

        private bool ReduceMotion
        {
            get
            {
                IBoardFx? fx = Host.Fx;
                return fx != null && fx.ReduceMotion;
            }
        }

        // Puntos de 0,28 u con paso de 0,31 u en el hueco del arco (≥ 1,3 u de ancho a esa altura); con k ≥ 5 se juntan y
        // encogen para seguir dentro.
        private void LayoutPips()
        {
            int k = Mathf.Max(1, _pipK);
            float step = PipStepU;
            float size = PipU;
            if (k > 1 && ((k - 1) * step) + size > PipRowMaxU)
            {
                step = PipRowMaxU / (k - 0.1f);
                size = 0.9f * step;
            }

            _pips.Layout(0f, _heightU + PipRowAboveRimU, size, step, _u);
        }

        // Los llenos en perla y los vacíos (el aro) más tenues; con la cueva llena, todo atenuado.
        private void PaintPips()
        {
            LluviaOverlay.PaintPips(_pips, ProceduralArt.Pearl, _dimmed ? DimAlpha : 1f, EmptyPipAlpha);
        }

        private void SetMorselTint(int tint)
        {
            _patternOn = ProceduralArt.HasPattern(tint);
            _patternColor = Palette.PatternMark(_color, tint);
            if (_patternOn && tint != _patternTint)
            {
                _patternTint = tint;
                _morselPattern.uvRect = Host.Art.PatternUv(tint, MorselPatternWidthU, MorselPatternHeightU);
            }
        }

        private void RecomputeHome()
        {
            ICuiaBoard? board = Host.Board;
            if (board == null || (uint)Host.Index >= (uint)board.Count)
            {
                return;
            }

            float archTop = CaveCenterAboveRimU + (0.5f * ProceduralArt.PiranaCaveHeightU);
            _home = ToSchool(board.BodyCenter(Host.Index)) + new Vector2(0f, ((0.5f * _heightU) + archTop + SchoolAboveCaveU) * _u);
        }

        // Del espacio de las capas de tablero (origen en su centro) al del cardumen (arriba a la izquierda del tablero), con el
        // tamaño que tiene el tablero ahora mismo.
        private Vector2 ToSchool(Vector2 layer)
        {
            Rect r = _school.rect;
            return new Vector2(layer.x - r.xMin, layer.y - r.yMax);
        }

        private Vector2 ToLayer(Vector2 school)
        {
            Rect r = _school.rect;
            return new Vector2(school.x + r.xMin, school.y + r.yMax);
        }

        private void Apply()
        {
            bool shown = _on && !_inFlight;
            OverlayParts.SetVisible(_arch, shown);
            _pips.SetVisible(shown);
            if (_school.gameObject.activeSelf != _on)
            {
                _school.gameObject.SetActive(_on);
            }

            if (!_animating)
            {
                PlaceIdle();
            }
        }

        // Cardumen en reposo: rondando (despierto), quieto y atenuado (dormido) o quieto (Reducir movimiento).
        private void PlaceIdle()
        {
            for (int i = 0; i < SchoolSize; i++)
            {
                IdlePose(i, out Vector2 pos, out float facing, out float scale, out float alpha);
                SetFish(i, pos, facing, scale, alpha, 0f);
            }

            HideMorsel();
            HideImpact();
        }

        private void IdlePose(int i, out Vector2 pos, out float facing, out float scale, out float alpha)
        {
            float bob = Mathf.Sin((_clock * 2f * Mathf.PI / BobPeriodMs) + (i * 1.3f));
            if (_dimmed || ReduceMotion)
            {
                float lift = _dimmed && !ReduceMotion ? 0.025f * bob : 0f;
                pos = _home + new Vector2(RestX[i] * _u, (RestY[i] + lift) * _u);
                facing = RestFacing[i];
                scale = _dimmed ? 0.92f : 1f;
                alpha = _dimmed ? DimAlpha : 1f;
                return;
            }

            // Ronda: cada piraña da la vuelta a una elipse plana alrededor del sitio, a un tercio de vuelta de la anterior;
            // la de atrás (arriba) se ve algo más pequeña y en los extremos gira (se estrecha).
            float angle = (_clock * 2f * Mathf.PI / IdlePeriodMs) + (i * 2f * Mathf.PI / SchoolSize);
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            pos = _home + new Vector2(cos * 0.55f * _u, ((sin * 0.13f) + (0.03f * bob)) * _u);
            float turn = Mathf.Clamp(sin * 3f, -1f, 1f);
            facing = Mathf.Abs(turn) < 0.3f ? (sin >= 0f ? 0.3f : -0.3f) : turn;
            scale = Mathf.Lerp(1f, 0.84f, (sin + 1f) * 0.5f);
            alpha = 1f;
        }

        // Fotograma del paso en la fracción p (sin asignaciones).
        private void Pose(float p)
        {
            if (_kind == MechanicStepKind.Bite && _victimDose >= 0)
            {
                PoseBite(p);
                HideImpact();
            }
            else
            {
                PoseLost(p);
            }
        }

        private void PoseBite(float p)
        {
            Vector2 center;
            float facing = FacingFrom(_home, _target);
            if (p < ReachEnd)
            {
                center = Arc(_home, _target, Ease.InOut(p / ReachEnd));
            }
            else if (p < CarryStart)
            {
                center = _target;
            }
            else if (p < CarryEnd)
            {
                facing = _carryFacing;
                center = Arc(_target, _cave, Ease.InOut((p - CarryStart) / (CarryEnd - CarryStart)));
            }
            else
            {
                facing = _carryFacing;
                center = Vector2.Lerp(_cave, _home, Ease.Out((p - CarryEnd) / (1f - CarryEnd)));
            }

            // La que va delante baja de morros a la boca de la víctima y muerde.
            float dip = p >= ReachEnd && p < CarryStart ? Mathf.Sin(Mathf.PI * (p - ReachEnd) / (CarryStart - ReachEnd)) : 0f;
            Vector2 leadPos = Vector2.zero;
            float leadTilt = 0f;
            for (int i = 0; i < SchoolSize; i++)
            {
                Vector2 pos = Formation(i, center, facing);
                float tilt = 0f;
                float fishFacing = facing;
                if (i == Lead && dip > 0f)
                {
                    pos = Vector2.Lerp(pos, center + new Vector2(facing * 0.38f * _u, -0.5f * _u), dip);
                    tilt = 28f * dip;
                }
                else if (dip > 0f)
                {
                    // Las otras dos se abren a los lados de la boca, por encima, mirando a la que muerde.
                    float side = i == 0 ? -1f : 1f;
                    pos = Vector2.Lerp(pos, center + new Vector2(side * 0.6f * _u, (0.26f + (0.04f * side)) * _u), dip);
                    if (dip > 0.5f)
                    {
                        fishFacing = side;
                    }
                }

                pos = JoinAndLeave(i, p, pos, fishFacing, out float sx, out float scale, out float alpha);
                SetFish(i, pos, sx, scale, alpha, tilt * facing);
                if (i == Lead)
                {
                    leadPos = pos;
                    leadTilt = tilt;
                }
            }

            // La mordida: el contador vuelve a empezar.
            if (!_bitten && p >= BiteAt)
            {
                _bitten = true;
                ResetPips();
            }

            // La víctima pierde la dosis de arriba desde la mitad del paso, poco a poco mientras sube el bocado.
            if (p >= DrainStart && !_drained)
            {
                float q = Mathf.Clamp01((p - DrainStart) / (DrainEnd - DrainStart));
                PaintVictim(1f - Ease.InOut(q));
                _drained = q >= 1f;
            }

            // El bocado: sale de la dosis y sube por el eje de la víctima hasta la boca de la que muerde (creciendo), va en
            // su boca hasta la cueva, se suelta sobre el eje de la cueva y entra recto por la boca, apagándose en el labio.
            Vector2 mouth = Mouth(leadPos, facing, leadTilt);
            Vector2 morselPos = mouth;
            float morselScale = 1f;
            float morselAlpha = 0f;
            if (p >= MorselRiseStart && p < CarryStart)
            {
                float q = (p - MorselRiseStart) / (CarryStart - MorselRiseStart);
                morselPos = new Vector2(Mathf.Lerp(_morselFrom.x, mouth.x, q * q), Mathf.Lerp(_morselFrom.y, mouth.y, Ease.InOut(q)));
                morselScale = Mathf.Lerp(MorselStartScale, 1f, Ease.Out(q));
                morselAlpha = Mathf.Clamp01(q / 0.2f);
            }
            else if (p >= CarryStart && p < CarryEnd)
            {
                morselAlpha = 1f;
            }
            else if (p >= CarryEnd && p < OverCaveEnd)
            {
                morselPos = Vector2.Lerp(_morselDrop, _caveTop, Ease.InOut((p - CarryEnd) / (OverCaveEnd - CarryEnd)));
                morselAlpha = 1f;
            }
            else if (p >= OverCaveEnd && p < MorselGone)
            {
                float q = (p - OverCaveEnd) / (MorselGone - OverCaveEnd);
                morselPos = Vector2.Lerp(_caveTop, _caveLip, Ease.In(q));
                morselAlpha = 1f - q;
            }

            PlaceMorsel(morselPos, morselScale, morselAlpha);

            // Al atrapar el bocado (llega a la boca de la que muerde), unos destellos del tinte: un «ñam» junto al pez, en vez
            // de burbujas que se quedan en la cuia vacía o en el aire.
            if (!_caught && p >= CarryStart)
            {
                _caught = true;
                EmitCatch(morselPos);
            }

            // La dosis de arriba de la cueva se forma cuando entra el bocado, con su salpicadura al empezar.
            if (p >= FillStart)
            {
                PaintCave(Ease.Out((p - FillStart) / (1f - FillStart)));
                if (!_splashed)
                {
                    _splashed = true;
                    EmitSplash();
                }
            }
        }

        private void PoseLost(float p)
        {
            if (!_pipsReset && p >= LostPipsAt)
            {
                ResetPips();
            }

            if (_sleepy)
            {
                PosePeek(p);
                HideMorsel();
                HideImpact();
                return;
            }

            // Sale de la cueva y nada en arco hasta encima de la boca de la víctima; la que va delante se lanza de morros contra
            // la tapa (o el embudo), choca (su boca se hunde LostDentDp en la tapa, se aplasta y salta un destello), sale rebotada
            // hacia atrás y el cardumen vuelve a su ronda.
            float toward = FacingFrom(_home, _target);
            float hitTilt = HitTiltDeg * Mathf.Deg2Rad;
            Vector2 hitPoint = _lid - new Vector2(0f, LostDentDp);
            Vector2 contact = hitPoint - new Vector2(-toward * Mathf.Cos(hitTilt) * 0.42f * _u, ((-Mathf.Sin(hitTilt) * 0.42f) - 0.06f) * _u);
            Vector2 rebound = contact + new Vector2(toward * 0.32f * _u, 0.08f * _u); // despedida hacia atrás, apenas arriba
            Vector2 center;
            float facingNow = toward;
            if (p < LostReachEnd)
            {
                center = Arc(_home, _target, Ease.InOut(p / LostReachEnd));
            }
            else if (p < LostReboundEnd)
            {
                center = _target;
            }
            else
            {
                facingNow = -toward;
                center = Arc(_target, _home, Ease.InOut((p - LostReboundEnd) / (1f - LostReboundEnd)));
            }

            if (!_bumped && p >= LostHitAt)
            {
                _bumped = true;
                EmitBump();
            }

            // Las otras dos se apartan a los lados de la tapa al llegar (bajas: no suben hacia la fila de arriba) y vuelven a la
            // formación al irse.
            float spread;
            if (p < LostReachEnd)
            {
                spread = Mathf.SmoothStep(0f, 1f, (p - (LostReachEnd - 0.1f)) / 0.1f);
            }
            else if (p < LostReboundEnd)
            {
                spread = 1f;
            }
            else
            {
                spread = 1f - Mathf.SmoothStep(0f, 1f, (p - LostReboundEnd) / 0.12f);
            }

            // El golpe: la que va delante se aplasta a lo largo de su cuerpo (escala local, antes del giro): deprisa hasta
            // LostHitAt + SquashIn y despacio de vuelta hasta LostHitAt + SquashSpan.
            float squash = 0f;
            if (_bumped)
            {
                float k = p - LostHitAt;
                squash = k < SquashIn ? Ease.Out(Mathf.Clamp01(k / SquashIn)) : 1f - Ease.InOut(Mathf.Clamp01((k - SquashIn) / (SquashSpan - SquashIn)));
            }

            for (int i = 0; i < SchoolSize; i++)
            {
                Vector2 pos = Formation(i, center, facingNow);
                float tilt = 0f;
                float fishFacing = facingNow;
                if (i == Lead && p >= LostReachEnd)
                {
                    if (p < LostHitAt)
                    {
                        // Se lanza: acelera hasta la tapa con el morro cada vez más abajo.
                        float q = Ease.In((p - LostReachEnd) / (LostHitAt - LostReachEnd));
                        pos = Vector2.Lerp(Formation(i, _target, toward), contact, q);
                        tilt = HitTiltDeg * q;
                    }
                    else if (p < LostHoldEnd)
                    {
                        // Contra la tapa: la boca se hunde LostDentDp y se aplasta (el golpe se ve).
                        pos = contact;
                        tilt = HitTiltDeg;
                    }
                    else if (p < LostReboundEnd)
                    {
                        // Rebota: sale despedida hacia atrás, con el morro levantado.
                        float r = Ease.Out((p - LostHoldEnd) / (LostReboundEnd - LostHoldEnd));
                        pos = Vector2.Lerp(contact, rebound, r);
                        tilt = Mathf.Lerp(HitTiltDeg, -15f, r);
                    }
                    else
                    {
                        // Vuelve a su sitio en la formación al empezar la vuelta.
                        float b = Ease.Out(Mathf.Clamp01((p - LostReboundEnd) / 0.15f));
                        pos = Vector2.Lerp(rebound + (center - _target), pos, b);
                        tilt = Mathf.Lerp(-15f, 0f, b);
                    }
                }
                else if (i != Lead && spread > 0f)
                {
                    // A un lado de la tapa, mirando a la que choca; con el golpe dan un respingo hacia fuera.
                    float side = i == 0 ? -1f : 1f;
                    Vector2 aside = _lid + new Vector2(side * LostAsideXU * _u, LostAsideYU * _u);
                    float jolt = _bumped && p < LostReboundEnd ? Mathf.Sin(Mathf.PI * Mathf.Clamp01((p - LostHitAt) / (LostReboundEnd - LostHitAt))) : 0f;
                    pos = Vector2.Lerp(pos, aside + new Vector2(side * 0.1f * jolt * _u, 0.03f * jolt * _u), spread);
                    if (spread > 0.5f)
                    {
                        fishFacing = side;
                    }
                }

                // Dentro del panel: con la víctima en la primera o la última columna, ninguna se sale por el marco.
                pos = ClampToPanel(pos, 1f, tilt);
                pos = JoinAndLeave(i, p, pos, fishFacing, out float sx, out float scale, out float alpha);
                float along = i == Lead ? 1f - (SquashAlong * squash) : 1f;
                float across = i == Lead ? 1f + (SquashAcross * squash) : 1f;
                SetFish(i, pos, sx, scale, alpha, tilt * facingNow, along, across);
                if (i == Lead)
                {
                    _leadMouth = Mouth(pos, facingNow, tilt);
                }
            }

            PoseImpact(p);
            HideMorsel();
        }

        // Destello del golpe: una estrella perla de ImpactFlashDp sobre el punto de la tapa (o del embudo) donde choca la que va
        // delante; crece en 12 ms, se queda y se apaga hacia los 64 ms (con 400 ms). Un objeto ya creado: sin asignaciones.
        private void PoseImpact(float p)
        {
            float q = p - LostHitAt;
            if (!_bumped || q < 0f || q >= FlashGoneAt)
            {
                HideImpact();
                return;
            }

            float scale = Mathf.Lerp(0.4f, 1f, Ease.Out(Mathf.Clamp01(q / FlashGrowEnd)));
            float alpha = q < FlashHoldEnd ? 1f : 1f - Mathf.Clamp01((q - FlashHoldEnd) / (FlashGoneAt - FlashHoldEnd));
            PlacePart(_impact.rectTransform, _lid + new Vector2(0f, FlashAboveLidDp), new Vector3(scale, scale, 1f));
            OverlayParts.SetColor(_impact, OverlayParts.Fade(ProceduralArt.Pearl, alpha));
        }

        private void HideImpact()
        {
            if (_impact != null)
            {
                OverlayParts.SetColor(_impact, new Color(ProceduralArt.Pearl.r, ProceduralArt.Pearl.g, ProceduralArt.Pearl.b, 0f));
            }
        }

        // La x de una piraña (escala scale, tilt grados de giro) dentro del canto interior del marco del panel, con
        // LostPanelClearDp de aire (el tramo se calcula en PlayStep; sin tablero, sin límite).
        private Vector2 ClampToPanel(Vector2 pos, float scale, float tiltDeg)
        {
            float a = tiltDeg * Mathf.Deg2Rad;
            float half = 0.5f * scale * _u * ((ProceduralArt.PiranaFishWidthU * Mathf.Abs(Mathf.Cos(a))) + (ProceduralArt.PiranaFishHeightU * Mathf.Abs(Mathf.Sin(a))));
            float lo = _lostMinX + half;
            float hi = _lostMaxX - half;
            if (lo <= hi)
            {
                pos.x = Mathf.Clamp(pos.x, lo, hi);
            }

            return pos;
        }

        // Canto interior del marco del panel en el espacio del cardumen, desde las ranuras (la fila más ancha empieza
        // PanelInsetFromSlotsDp fuera de él), con LostPanelClearDp de aire. Sin asignaciones.
        private void PanelSpan(ICuiaBoard? board)
        {
            _lostMinX = float.NegativeInfinity;
            _lostMaxX = float.PositiveInfinity;
            if (board == null || board.Count <= 0)
            {
                return;
            }

            float lo = float.PositiveInfinity;
            float hi = float.NegativeInfinity;
            int n = board.Count;
            for (int c = 0; c < n; c++)
            {
                Rect r = board.SlotRect(c);
                lo = Mathf.Min(lo, r.xMin);
                hi = Mathf.Max(hi, r.xMax);
            }

            _lostMinX = ToSchool(new Vector2(lo + PanelInsetFromSlotsDp + LostPanelClearDp, 0f)).x;
            _lostMaxX = ToSchool(new Vector2(hi - PanelInsetFromSlotsDp - LostPanelClearDp, 0f)).x;
        }

        // Cueva llena con la víctima encima: lo que puede subir la que va delante (dp en el espacio del cardumen), como mucho
        // PeekRiseU, sin que lo alto de su sprite con el morro arriba (y el balanceo de dormida) pase de PeekShelfClearDp bajo
        // la repisa de la fila de encima de la cueva. Sin asignaciones.
        private float PeekRise(ICuiaBoard? board, Vector2 caveFoot)
        {
            float max = PeekRiseU * _u;
            if (board == null)
            {
                return max;
            }

            float above = float.PositiveInfinity;
            int n = board.Count;
            for (int c = 0; c < n; c++)
            {
                float fy = board.Foot(c).y;
                if (fy > caveFoot.y + 1f && fy < above)
                {
                    above = fy;
                }
            }

            if (float.IsPositiveInfinity(above))
            {
                return max;
            }

            float limit = ToSchool(new Vector2(0f, above - ShelfBottomBelowFootDp - PeekShelfClearDp)).y;
            float restY = _home.y + ((RestY[Lead] + 0.03f) * _u);
            float a = PeekUpTiltDeg * Mathf.Deg2Rad;
            float halfY = 0.5f * _u * ((ProceduralArt.PiranaFishWidthU * Mathf.Sin(a)) + (ProceduralArt.PiranaFishHeightU * Mathf.Cos(a)));
            return Mathf.Clamp(limit - (restY + halfY), 0f, max);
        }

        // Cueva llena (respuesta Q-C): el cardumen dormido se despierta, la que va delante se asoma hacia la víctima (morro
        // abajo), las otras dos le dejan paso o la siguen un poco, miran, vuelven a su sitio y se duermen otra vez (atenuadas y
        // con los ojos cerrados).
        private void PosePeek(float p)
        {
            float wake = Mathf.Clamp01(p / PeekWakeEnd);
            float sleep = Mathf.Clamp01((p - PeekBackEnd) / (1f - PeekBackEnd));
            float lit = Mathf.Min(Ease.Out(wake), 1f - Ease.In(sleep)); // 0 dormido, 1 despierto
            _awake = p >= 0.05f && p < 0.92f;
            float out01;
            if (p < PeekWakeEnd)
            {
                out01 = 0f;
            }
            else if (p < PeekOutEnd)
            {
                out01 = Ease.InOut((p - PeekWakeEnd) / (PeekOutEnd - PeekWakeEnd));
            }
            else if (p < PeekHoldEnd)
            {
                out01 = 1f;
            }
            else
            {
                out01 = 1f - Ease.InOut(Mathf.Clamp01((p - PeekHoldEnd) / (PeekBackEnd - PeekHoldEnd)));
            }

            if (_peekUp)
            {
                PosePeekUp(out01, lit, wake);
                return;
            }

            float look = p >= PeekWakeEnd && p < PeekHoldEnd + (0.5f * (PeekBackEnd - PeekHoldEnd)) ? -_peekDir : 0f; // mirando hacia fuera
            for (int i = 0; i < SchoolSize; i++)
            {
                IdlePose(i, out Vector2 rest, out float restFacing, out float restScale, out float restAlpha);
                // La que va delante sale por delante de la del lado hacia el que se asoma, que le deja paso (atrás y arriba); la otra
                // la sigue un poco.
                bool aside = i != Lead && RestX[i] * _peekDir > 0f;
                float reach = (i == Lead ? PeekReachU : (aside ? -0.1f : 0.3f)) * out01;
                float dip = (i == Lead ? 0.3f : (aside ? -0.14f : 0.03f)) * out01;
                Vector2 pos = rest + new Vector2(_peekDir * reach * _u, -dip * _u);
                float facing = look != 0f ? look : restFacing;
                float scale = Mathf.Lerp(restScale, 1f, lit);
                float alpha = Mathf.Lerp(restAlpha, 1f, lit);
                float shiver = i == Lead ? 0f : 6f * Mathf.Sin(6f * Mathf.PI * wake) * (1f - wake);
                float tilt = i == Lead ? 14f * out01 * facing : (i % 2 == 0 ? 1f : -1f) * shiver; // la que se asoma, morro abajo
                SetFish(i, pos, facing, scale, alpha, tilt);
                if (i == Lead)
                {
                    _leadMouth = Mouth(pos, facing, 0f);
                }
            }
        }

        // Cueva llena con la víctima encima, en la misma columna (revisión de la tanda B2, ronda 2): la que va delante se asoma
        // hacia arriba, hacia la víctima: sube con el morro arriba hasta _peekRise (sin pasar de la repisa de arriba), mira y
        // vuelve; las otras dos se desperezan en su sitio y apenas la siguen. out01 es lo asomada (0..1), lit lo despierta y
        // wake el despertar (para el respingo).
        private void PosePeekUp(float out01, float lit, float wake)
        {
            for (int i = 0; i < SchoolSize; i++)
            {
                IdlePose(i, out Vector2 rest, out float facing, out float restScale, out float restAlpha);
                float rise = (i == Lead ? 1f : 0.12f) * _peekRise * out01;
                Vector2 pos = rest + new Vector2(0f, rise);
                float scale = Mathf.Lerp(restScale, 1f, lit);
                float alpha = Mathf.Lerp(restAlpha, 1f, lit);
                float shiver = i == Lead ? 0f : 6f * Mathf.Sin(6f * Mathf.PI * wake) * (1f - wake);
                float up = i == Lead ? -PeekUpTiltDeg * out01 : 0f; // morro arriba (el giro positivo baja el morro)
                float tilt = i == Lead ? up * facing : (i % 2 == 0 ? 1f : -1f) * shiver;
                SetFish(i, pos, facing, scale, alpha, tilt);
                if (i == Lead)
                {
                    _leadMouth = Mouth(pos, facing, up);
                }
            }
        }

        // Destellos perla donde la que va delante choca con la tapa o el embudo: el golpe se ve, sin rojo ni texto. Con Reducir
        // movimiento el pool no emite.
        private void EmitBump()
        {
            if (_fx == null || _reduceMotion)
            {
                return;
            }

            _fx.Sparkles(_lid, ProceduralArt.Pearl, BumpSparkles, BumpSparkleScale * _u);
        }

        // Entra desde donde estaba cada piraña y, al final, vuelve a su ronda.
        private Vector2 JoinAndLeave(int i, float p, Vector2 pos, float facing, out float sx, out float scale, out float alpha)
        {
            sx = facing;
            scale = 1f;
            alpha = 1f;
            if (p < JoinEnd)
            {
                pos = Vector2.Lerp(_start[i], pos, Ease.Out(p / JoinEnd));
            }

            if (p > CarryEnd)
            {
                IdlePose(i, out Vector2 idle, out float idleFacing, out float idleScale, out float idleAlpha);
                float b = Ease.InOut((p - CarryEnd) / (1f - CarryEnd));
                pos = Vector2.Lerp(pos, idle, b);
                sx = b > 0.5f ? idleFacing : facing;
                scale = Mathf.Lerp(1f, idleScale, b);
                alpha = Mathf.Lerp(1f, idleAlpha, b);
            }

            return pos;
        }

        // Formación en marcha: la que muerde va delante y las otras dos detrás, una arriba y otra abajo.
        private Vector2 Formation(int i, Vector2 center, float facing)
        {
            float forward = -facing * _u; // mirando a la izquierda (+1), delante es −x
            switch (i)
            {
                case Lead:
                    return center + new Vector2(forward * 0.28f, 0f);
                case 0:
                    return center + new Vector2(-forward * 0.22f, 0.26f * _u);
                default:
                    return center + new Vector2(-forward * 0.30f, -0.22f * _u);
            }
        }

        // Boca de una piraña en pos que mira a facing con el morro tilt grados hacia abajo.
        private Vector2 Mouth(Vector2 pos, float facing, float tilt)
        {
            float a = tilt * Mathf.Deg2Rad;
            return pos + new Vector2(-facing * Mathf.Cos(a) * 0.42f * _u, ((-Mathf.Sin(a) * 0.42f) - 0.06f) * _u);
        }

        // Arco de a a b, más alto cuanto más lejos (salta por encima del tablero).
        private Vector2 Arc(Vector2 a, Vector2 b, float q)
        {
            Vector2 mid = (0.5f * (a + b)) + new Vector2(0f, (0.6f * _u) + (0.15f * (b - a).magnitude));
            float r = 1f - q;
            return (r * r * a) + (2f * r * q * mid) + (q * q * b);
        }

        private float FacingFrom(Vector2 from, Vector2 to)
        {
            float dx = to.x - from.x;
            if (dx < -0.5f)
            {
                return 1f;
            }

            return dx > 0.5f ? -1f : 1f;
        }

        // Piraña i en pos, mirando a facing, con su escala, su opacidad y tiltDeg grados de giro; along y across la estiran a lo
        // largo y a lo ancho de su cuerpo (el golpe del mordisco perdido), antes del giro.
        private void SetFish(int i, Vector2 pos, float facing, float scale, float alpha, float tiltDeg, float along = 1f, float across = 1f)
        {
            _fishPos[i] = pos;
            Image fish = _fish[i];
            RectTransform rt = fish.rectTransform;
            if (rt.anchoredPosition != pos)
            {
                rt.anchoredPosition = pos;
            }

            var s = new Vector3(facing * scale * along, scale * across, 1f);
            if (rt.localScale != s)
            {
                rt.localScale = s;
            }

            Quaternion rot = tiltDeg == 0f ? Quaternion.identity : Quaternion.Euler(0f, 0f, tiltDeg);
            if (rt.localRotation != rot)
            {
                rt.localRotation = rot;
            }

            Sprite sprite = _dimmed && !_animating ? Host.Art.PiranaFishAsleepSprite : Host.Art.PiranaFishSprite;
            if (_sleepy && _animating && !_awake)
            {
                sprite = Host.Art.PiranaFishAsleepSprite; // con la cueva llena, despierta solo para asomarse
            }

            if (fish.sprite != sprite)
            {
                fish.sprite = sprite;
            }

            OverlayParts.SetColor(fish, new Color(1f, 1f, 1f, alpha));
        }

        // El bocado en pos (espacio del cardumen) con la escala y la opacidad dadas: contorno, cápsula, patrón y brillo.
        private void PlaceMorsel(Vector2 pos, float scale, float alpha)
        {
            _morselPos = pos;
            _morselScale = scale;
            var s = new Vector3(scale, scale, 1f);
            PlacePart(_morselRim.rectTransform, pos, s);
            PlacePart(_morsel.rectTransform, pos, s);
            PlacePart(_morselPattern.rectTransform, pos, s);
            PlacePart(_glint.rectTransform, pos + new Vector2(-0.2f * scale * _u, 0.11f * scale * _u), s);
            OverlayParts.SetColor(_morselRim, OverlayParts.Fade(MorselRimColor, alpha));
            OverlayParts.SetColor(_morsel, new Color(_color.r, _color.g, _color.b, alpha));
            OverlayParts.SetColor(_morselPattern, _patternOn ? OverlayParts.Fade(_patternColor, alpha) : new Color(1f, 1f, 1f, 0f));
            OverlayParts.SetColor(_glint, new Color(1f, 1f, 1f, 0.7f * alpha));
        }

        private void HideMorsel()
        {
            OverlayParts.SetColor(_morselRim, new Color(MorselRimColor.r, MorselRimColor.g, MorselRimColor.b, 0f));
            OverlayParts.SetColor(_morsel, new Color(_color.r, _color.g, _color.b, 0f));
            OverlayParts.SetColor(_morselPattern, new Color(1f, 1f, 1f, 0f));
            OverlayParts.SetColor(_glint, new Color(1f, 1f, 1f, 0f));
        }

        private static void PlacePart(RectTransform rt, Vector2 pos, Vector3 scale)
        {
            if (rt.anchoredPosition != pos)
            {
                rt.anchoredPosition = pos;
            }

            if (rt.localScale != scale)
            {
                rt.localScale = scale;
            }
        }

        private static void SetSize(RectTransform rt, Vector2 size)
        {
            if (rt.sizeDelta != size)
            {
                rt.sizeDelta = size;
            }
        }

        private void ResetPips()
        {
            _pipsReset = true;
            _pips.Set(_afterK, _afterCounter);
            PaintPips();
        }

        private void PaintVictim(float fill)
        {
            if (_board != null && _victimDose >= 0)
            {
                _board.SetDose(_victim, _victimDose, _color, fill, _tint);
            }
        }

        private void PaintCave(float fill)
        {
            if (_board != null && _caveDose >= 0 && _caveDose < Host.Capacity)
            {
                _board.SetDose(_index, _caveDose, _color, fill, _tint);
            }
        }

        // Destellos del tinte (aclarado, para que se vean también los oscuros) donde la que muerde atrapa el bocado. Con Reducir
        // movimiento el pool no emite. Los efectos usan el mismo espacio que el cardumen (arriba a la izquierda del tablero).
        private void EmitCatch(Vector2 at)
        {
            if (_fx == null || _reduceMotion)
            {
                return;
            }

            _fx.Sparkles(at, ColorMath.Lighten(_color, 0.35f), CatchSparkles, CatchSparkleScale * _u);
        }

        // Salpicadura de la fruta al empezar a formarse su dosis en la cueva.
        private void EmitSplash()
        {
            if (_fx == null || _reduceMotion)
            {
                return;
            }

            _fx.Splash(_caveSurface, _color, _u);
        }

        // Termina el paso: las dos cuias pintadas como después (con ShowContent si cierra), el bocado oculto y el cardumen
        // de vuelta a su ronda.
        private void FinishStep(bool paint)
        {
            if (!_animating)
            {
                return;
            }

            _animating = false;
            PaintVictim(0f);
            PaintCave(1f);
            ICuiaBoard? board = _board;
            _board = null;
            _fx = null;
            _victimDose = -1;
            _caveDose = -1;
            _sleepy = false;
            _awake = false;
            _bumped = false;
            PlaceIdle();
            if (paint && board != null)
            {
                board.ShowContent(_index, _after, _afterSealed);
                if (_victim >= 0 && _victim != _index && _victim < board.Count)
                {
                    board.ShowContent(_victim, _otherAfter, _otherAfterSealed);
                }
            }
        }

        /// <summary>Destruye el cardumen (que vive en la capa del tablero) cuando se destruye la vista de la cueva.</summary>
        private sealed class SchoolLink : MonoBehaviour
        {
            public GameObject? School;

            private void OnDestroy()
            {
                if (School == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    Destroy(School);
                }
                else
                {
                    DestroyImmediate(School);
                }
            }
        }
    }
}
