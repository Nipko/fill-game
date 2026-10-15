#nullable enable
using System;
using System.Collections.Generic;
using AF.Core.Board;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// «Llenar y teñir», la carga del nivel (guion de la intro, que evoluciona 02 §4.2, 07 §5.1 y 08 §5.3 sin cambiar los
    /// tiempos: 1,4 s en 1-20 y 0,8 s desde el 21 y en los reintentos, saltable desde el primer fotograma). Las cuias están
    /// vacías y secas en su ranura desde el fotograma 0 (sin caída); el río del fondo crece (lo único que se mueve en los
    /// primeros 100 ms), desborda el canto superior del marco desde el centro, el agua corre por los costados y por las
    /// repisas y sobre cada cuia no vacía cae un chorrito (el mismo chorro del vertido) que la llena desde el fondo hasta sus
    /// dosis finales; las vacías quedan secas. Después, cada fruta (de la mano de un miquito de la especie del tramo o del
    /// dosel, <see cref="TroopLayer"/>) entra por la boca y su racha florece en 100 ms con interpolación OKLab, de abajo arriba
    /// en la cuia; la última floración termina en T. El calendario lo calcula una vez <see cref="IntroChoreography"/> y todo se
    /// evalúa por tiempo, en un Canvas anidado propio delante de las cuias (<see cref="Layer"/>) con pools preasignados: 7 tiras
    /// de agua, 16 chorritos, 24 frutas (un atlas), 24 penachos, 24 anillos aditivos (propios: el pool común de
    /// <see cref="FxLayer"/>, de 44 partículas y 8 brillos aditivos, es de los efectos del juego) y 20
    /// gotas; y, en la capa de la tropa, por detrás de la rama (<see cref="AttachFront"/>: labio &lt; rama &lt; tropa, guion
    /// §4.6.5), el labio del río que desborda la orilla hasta el canto del panel con 6 puntos de espuma en la cresta, al 70 %
    /// en la franja de la rama para que se lea «rama sobre la crecida» y no una tabla. Presupuesto del guion §13, con contador
    /// (<see cref="CountVisible"/>): ≤ 8 brillos aditivos a la vez (la espuma de los chorritos y los anillos; se queda el más
    /// reciente y la vida del anillo se acorta según el espaciado de los impactos) y ≤ 20 partículas (gotas, penachos y espuma
    /// del labio). Todo lo que se ve es función del tiempo de la intro: también la onda del menisco, cuya edad es t menos el
    /// último evento de su cuia (inicio del llenado o impacto de una fruta) con una fase fija por cuia y evento, sin estado
    /// acumulado en el líquido (A14); y, con el ambiente congelado, el reloj de los shaders del líquido y del chorro es el de la
    /// intro (<see cref="ShaderClock"/>). Así una captura en t sale igual sin importar las intros que hubo antes. Nada recibe
    /// toques y no se asigna memoria por fotograma tras <see cref="Start"/>.
    /// </summary>
    /// <remarks>
    /// <para>Sigue las recomendaciones del guion (Q1-Q7 A, Q8 A provisional, Q9 A, Q10 A, Q13 A y Q16 A; Q11 diferida), que
    /// cambian 02 §4.2, 07 §5.1 y 08 §5.3: antes de fusionar en main hace falta la decisión del dueño (D-010) y editar esos
    /// capítulos (guion §17). Hasta entonces el GDD manda y esto es una propuesta en su rama.</para>
    /// <para><see cref="Complete"/> deja el estado final en el mismo fotograma; si fue un salto, los restos (frutas, anillos,
    /// chorritos, tiras, capa del río y tropa) se funden en ≤ 100 ms en su sitio, sin moverse ni impactos tardíos. Al fin
    /// natural, la cola sigue ya en juego: la tropa reposa ≈ 1.500 ms tras su última suelta y sale en 500 ms (D-011, punto 3),
    /// sin recibir toques ni reaccionar a la selección o a los vertidos (D-002); <see cref="Clear"/> la retira en el acto.</para>
    /// <para>Las dosis ocultas (Aguas turbias) se llenan de río como las demás, no reciben fruta ni nota y pasan todas a la vez al
    /// pardo opaco #8A7B6A con su trama y su «?» (los pinta el shader del líquido) y la insignia «?×n» en el fotograma del primer
    /// impacto (02 §4.2; 07 §4.3; D-010; <see cref="Mechanics.MurkyMarks"/>); con <see cref="ReduceMotion"/>, se funden ya
    /// turbias, con su trama y su «?», y la insignia sale al final. El Barro no es
    /// una racha: tampoco recibe fruta ni florece, y aparece con <see cref="Palette.Mud"/> y sus grumos en ese mismo fotograma
    /// (la regla de 02 §4.2 aplicada al Barro [P]).</para>
    /// <para>Con <see cref="ReduceMotion"/> (07 §7.8, cuando exista el ajuste): fundido de 250 ms al estado final y, en modo
    /// rama, un miquito quieto que se desvanece 600 ms después (guion §9; recomendación Q13 A, pendiente de D-010).</para>
    /// </remarks>
    public sealed class IntroAnimation
    {
        /// <summary>Una franja se tiñe en 100 ms (02 §4.2).</summary>
        public const float StripeMs = IntroChoreography.BloomMs;

        /// <summary>Opacidad del agua del río dentro de la cavidad antes de teñirse (07 §5.1: 70 %).</summary>
        public const float WaterAlpha = 0.70f;

        /// <summary>Opacidad de la lámina y de las tiras de la madera (guion §11: agua del preset al 85 %).</summary>
        public const float SheetAlpha = 0.85f;

        /// <summary>Al saltar, los restos se funden en 100 ms (02 §4.2).</summary>
        public const float SkipFadeMs = 100f;

        /// <summary>Reducir movimiento: fundido de 250 ms (07 §5.1 y §7.8).</summary>
        public const float ReducedMotionMs = 250f;

        /// <summary>Reducir movimiento: el miquito quieto se ve 600 ms más y se desvanece en 150 ms (guion §9; recomendación Q13 A, pendiente de D-010).</summary>
        public const float ReducedTroopHoldMs = 600f;

        /// <summary>Fundido del miquito quieto.</summary>
        public const float ReducedTroopFadeMs = 150f;

        /// <summary>Cola tras T sin tropa (dosel): 300 ms, como antes de D-011 (los anillos y las gotas acaban antes).</summary>
        public const float CanopyTailMs = 300f;

        /// <summary>Sacudida del follaje de las esquinas en el modo dosel (±6°).</summary>
        public const float CanopyShakeDeg = 6f;

        /// <summary>Gotas en cada esquina del desborde y en el primer impacto de cada fila (≤ 20 en total).</summary>
        public const int DropsPerEvent = 2;

        /// <summary>Gotas como mucho en la intro (guion §13: ≤ 20 partículas).</summary>
        public const int MaxDrops = 20;

        /// <summary>Vida de cada gota (guía 6.6: 250 ms).</summary>
        public const float DropLifeMs = 250f;

        /// <summary>Los destellos de las tiras viajan a 350 dp/s (guion §11: 300-400 dp/s), en dp/ms.</summary>
        public const float FlowDpPerMs = 0.35f;

        /// <summary>Pardo opaco de las dosis ocultas (07 §4.3): alias de <see cref="Palette.Murky"/>.</summary>
        public static readonly Color MurkyColor = Palette.Murky;

        /// <summary>Brillos aditivos a la vez como mucho en la intro (guion §13).</summary>
        public const int MaxAdditive = 8;

        /// <summary>Partículas a la vez como mucho en la intro (guion §13): gotas, penachos y espuma del labio.</summary>
        public const int MaxParticles = 20;

        /// <summary>Puntos de espuma en la cresta del labio del desborde (2 × 2 dp).</summary>
        public const int LipFoamDots = 6;

        /// <summary>El labio del desborde nace en el borde inferior del río, 14 dp sobre el panel…</summary>
        public const float LipAbovePanelDp = 14f;

        /// <summary>…y llega 2 dp dentro del canto del panel, donde lo recoge la lámina.</summary>
        public const float LipBelowPanelDp = 2f;

        /// <summary>
        /// Franja de la rama baja sobre el labio: de panel.top − 11 a panel.top − 5 (la rama de 6 dp con su punto más bajo en
        /// panel.top − 8). Desde el borde plano del labio hasta su borde de arriba, el labio va al <see cref="LipBranchBandAlpha"/>.
        /// </summary>
        public const float LipBranchBandTopDp = 11f;

        /// <summary>Opacidad del labio en la franja de la rama (revisión del 2026-10-09: «rama sobre la crecida»).</summary>
        public const float LipBranchBandAlpha = 0.7f;

        /// <summary>Lado de cada punto de espuma del labio (dp).</summary>
        public const float LipFoamDp = 2f;

        /// <summary>
        /// Mientras la lámina y el labio crecen, sus frentes se afinan en lengua a lo largo de 14 dp (agua que avanza, no una
        /// caja); al llegar a las esquinas la lengua se endereza.
        /// </summary>
        public const float SheetTaperDp = 14f;

        private const int MaxCuias = IntroInput.MaxCuias;
        private const int MaxFruits = IntroChoreography.MaxFruits;

        private readonly IntroChoreography _plan = new IntroChoreography();
        private readonly IntroInput _input = new IntroInput();
        private readonly ProceduralArt _art;
        private readonly RectTransform _layer;
        private readonly WaterStripGraphic _sheet;
        private readonly WaterStripGraphic _sideLeft;
        private readonly WaterStripGraphic _sideRight;
        private readonly WaterStripGraphic[] _shelfLeft = new WaterStripGraphic[IntroInput.MaxRows - 1];
        private readonly WaterStripGraphic[] _shelfRight = new WaterStripGraphic[IntroInput.MaxRows - 1];
        private readonly StreamGraphic[] _streams = new StreamGraphic[MaxCuias];
        private readonly Image[] _streamRings = new Image[MaxCuias];
        private readonly Image[] _plumes = new Image[MaxFruits];
        private readonly Image[] _fruits = new Image[MaxFruits];
        private readonly Image[] _rings = new Image[MaxFruits];
        private readonly Image[] _drops = new Image[MaxDrops];
        private readonly float[] _dropT0 = new float[MaxDrops];
        private readonly Vector2[] _dropFrom = new Vector2[MaxDrops];
        private readonly Vector2[] _dropVel = new Vector2[MaxDrops];
        private readonly float[] _dropG = new float[MaxDrops];
        private readonly Color[] _dropColor = new Color[MaxDrops];
        private int _dropCount;
        private readonly Cuia[] _final = new Cuia[MaxCuias];
        private readonly bool[] _sealed = new bool[MaxCuias];
        private readonly float[] _rippleAt = new float[MaxCuias];
        private readonly int[] _rippleEvent = new int[MaxCuias];
        private readonly Color[] _ringColor = new Color[MaxFruits];
        private readonly Color[] _plumeColor = new Color[MaxFruits];
        private readonly List<Graphic> _graphics = new List<Graphic>(128);
        private readonly bool[] _foamWant = new bool[MaxCuias];
        private readonly float[] _foamY = new float[MaxCuias];
        private readonly Image[] _lipFoam = new Image[LipFoamDots];
        private WaterStripGraphic? _lip;
        private float _ringLife = IntroChoreography.RingMs;

        private CuiaView[] _views = Array.Empty<CuiaView>();
        private Color _water;
        private Color _waterA;
        private Color _waterCore;
        private float _t;
        private float _fadeLeft = -1f;
        private float _frozenT;
        private bool _tail;

        /// <summary>Crea la capa de la intro (Canvas anidado) dentro de <paramref name="board"/> (el contenedor del tablero).</summary>
        public IntroAnimation(RectTransform board, ProceduralArt art)
        {
            _art = art;
            var go = new GameObject("IntroLayer", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(board, false);
            _layer = (RectTransform)go.transform;
            _layer.anchorMin = Vector2.zero;
            _layer.anchorMax = Vector2.one;
            _layer.offsetMin = Vector2.zero;
            _layer.offsetMax = Vector2.zero;

            _sheet = NewStrip("Sheet");
            _sideLeft = NewStrip("SideLeft");
            _sideRight = NewStrip("SideRight");
            for (int r = 0; r < _shelfLeft.Length; r++)
            {
                _shelfLeft[r] = NewStrip("ShelfLeft" + r);
                _shelfRight[r] = NewStrip("ShelfRight" + r);
            }

            for (int i = 0; i < MaxCuias; i++)
            {
                var sgo = new GameObject("IntroStream" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(StreamGraphic));
                sgo.transform.SetParent(_layer, false);
                StreamGraphic s = sgo.GetComponent<StreamGraphic>();
                s.Setup();
                TopLeft(s.rectTransform, new Vector2(0f, 1f));
                s.enabled = false;
                _streams[i] = s;
                _graphics.Add(s);
            }

            for (int i = 0; i < MaxFruits; i++)
            {
                _plumes[i] = NewImage("Plume" + i, art.Glow, null);
            }

            for (int i = 0; i < MaxFruits; i++)
            {
                _fruits[i] = NewImage("Fruit" + i, null, null);
            }

            Material? additive = UiMaterials.Additive;
            for (int i = 0; i < MaxCuias; i++)
            {
                _streamRings[i] = NewImage("StreamRing" + i, art.Ring, additive); // espuma donde el chorrito toca el agua
            }

            for (int i = 0; i < MaxFruits; i++)
            {
                _rings[i] = NewImage("Ring" + i, art.Ring, additive);
            }

            for (int i = 0; i < MaxDrops; i++)
            {
                _drops[i] = NewImage("Drop" + i, art.Drop, null);
            }
        }

        /// <summary>
        /// Crea, en <paramref name="slot"/> (<see cref="TroopLayer.WaterSlot"/>, por detrás de la rama y de la tropa), el labio
        /// del río que desborda: una tira de agua desde el borde inferior del río (panel.top − 14) hasta 2 dp dentro del canto
        /// del panel, del mismo ancho que la lámina en cada instante, con 6 puntos de espuma de 2 × 2 dp en la cresta y al 70 %
        /// desde el canto hasta la franja de la rama (panel.top − 11). Así se ve que el agua sale del río y corre por debajo de
        /// la rama hasta el marco (guion §6.2, tiempo 2), con la rama delante, en primer plano, y el río detrás (§4.6.5).
        /// </summary>
        public void AttachFront(RectTransform slot)
        {
            if (_lip != null)
            {
                return;
            }

            var go = new GameObject("RiverLip", typeof(RectTransform), typeof(CanvasRenderer), typeof(WaterStripGraphic));
            go.transform.SetParent(slot, false);
            WaterStripGraphic lip = go.GetComponent<WaterStripGraphic>();
            lip.Setup();
            TopLeft(lip.rectTransform, new Vector2(0f, 1f));
            lip.enabled = false;
            _graphics.Add(lip);
            _lip = lip;
            for (int i = 0; i < LipFoamDots; i++)
            {
                var dot = new GameObject("LipFoam" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                dot.transform.SetParent(slot, false);
                Image img = dot.GetComponent<Image>();
                img.sprite = _art.Drop;
                img.raycastTarget = false;
                TopLeft(img.rectTransform, new Vector2(0.5f, 0.5f));
                img.rectTransform.sizeDelta = new Vector2(LipFoamDp, LipFoamDp);
                img.enabled = false;
                _graphics.Add(img);
                _lipFoam[i] = img;
            }
        }

        /// <summary>
        /// Cuenta lo que se ve de la intro en este fotograma (tests del presupuesto del guion §13): imágenes aditivas a la vista
        /// (espuma de los chorritos y anillos) y partículas (gotas, penachos y espuma del labio).
        /// </summary>
        public void CountVisible(out int additive, out int particles)
        {
            additive = 0;
            particles = 0;
            for (int i = 0; i < MaxCuias; i++)
            {
                additive += Shown(_streamRings[i]) ? 1 : 0;
            }

            for (int k = 0; k < MaxFruits; k++)
            {
                additive += Shown(_rings[k]) ? 1 : 0;
                particles += Shown(_plumes[k]) ? 1 : 0;
            }

            for (int j = 0; j < MaxDrops; j++)
            {
                particles += Shown(_drops[j]) ? 1 : 0;
            }

            for (int i = 0; i < LipFoamDots; i++)
            {
                particles += _lipFoam[i] != null && Shown(_lipFoam[i]) ? 1 : 0;
            }
        }

        /// <summary>Labio del desborde a la vista (tests y capturas).</summary>
        public bool LipVisible => _lip != null && _lip.enabled;

        /// <summary>Medio ancho del labio a la vista (dp; 0 sin labio).</summary>
        public float LipHalfWidth => _lip != null && _lip.enabled ? _lip.Length * 0.5f : 0f;

        /// <summary>Tira del labio (tests: su opacidad en la franja de la rama).</summary>
        public WaterStripGraphic? LipGraphic => _lip;

        /// <summary>La intro está en curso (input de juego bloqueado, vistas bloqueadas).</summary>
        public bool Running { get; private set; }

        /// <summary>Quedan restos cosméticos a la vista (cola natural o fundido del salto).</summary>
        public bool Active => Running || _tail || _fadeLeft >= 0f;

        /// <summary>
        /// La tropa (rama y miquitos) sigue a la vista: durante la intro, en su reposo y su salida ya en juego (D-011, punto 3)
        /// o mientras se funde tras un salto. Solo lectura: lo consulta la vista para no tapar la rama con otra cosa (la tarjeta
        /// «¡Miquito a la vista!» sale cuando la tropa se ha ido).
        /// </summary>
        public bool TroopVisible => Troop != null && Troop.Active && Active;

        /// <summary>
        /// Tiempo de la intro (ms, el de <see cref="TimeMs"/>) en que la tropa se habrá ido: el fin de su salida (o, con Reducir
        /// movimiento, el del miquito quieto) o, tras un salto, el fin del fundido de 100 ms (el reloj de la intro queda
        /// congelado en el toque). Así <c>TroopGoneMs − TimeMs</c> son siempre los ms que le quedan a la vista; −1 sin tropa.
        /// </summary>
        public float TroopGoneMs => Troop != null && Troop.Active ? (_fadeLeft >= 0f ? _t + _fadeLeft : _input.TotalMs + TailDuration()) : -1f;

        /// <summary>Tiempo de la intro (ms).</summary>
        public float TimeMs => _t;

        /// <summary>Calendario de la intro en curso.</summary>
        public IntroChoreography Plan => _plan;

        /// <summary>Geometría y modo: los rellena la pantalla antes de <see cref="Start"/> (panel, marco, repisas, HUD, tropa).</summary>
        public IntroInput Input => _input;

        /// <summary>Canvas anidado de la intro, delante de las cuias.</summary>
        public RectTransform Layer => _layer;

        /// <summary>Tropa (null = sin tropa: modo dosel).</summary>
        public TroopLayer? Troop { get; set; }

        /// <summary>Ambiente: crecida del río y sacudida del dosel.</summary>
        public AmbientLayer? Ambient { get; set; }

        /// <summary>Reducir movimiento (07 §7.8; hoy no hay ajuste: lo fija quien lo tenga).</summary>
        public bool ReduceMotion { get; set; }

        /// <summary>Restos visibles tras un salto: fundido en curso (tests).</summary>
        public bool Fading => _fadeLeft >= 0f;

        /// <summary>Opacidad de los restos (1 sin fundido).</summary>
        public float RemnantAlpha => _fadeLeft >= 0f ? Mathf.Clamp01(_fadeLeft / SkipFadeMs) : 1f;

        /// <summary>
        /// Arranca la intro de <paramref name="totalMs"/> para <paramref name="board"/>: planifica (geometría de
        /// <see cref="Input"/>), bloquea las vistas y las deja vacías y secas en su sitio. <paramref name="water"/> es el agua
        /// base del mundo (07 §2.1).
        /// </summary>
        public void Start(CuiaView[] views, BoardState board, Color water, int totalMs)
        {
            Complete(false);
            Clear();
            _views = views;
            int n = Mathf.Min(views.Length, MaxCuias);
            _input.Count = n;
            _input.Unit = n > 0 ? views[0].Unit : 24f;
            _input.TotalMs = Mathf.Max(1f, totalMs);
            for (int i = 0; i < n; i++)
            {
                _final[i] = board[i];
                _sealed[i] = board.IsSealed(i);
                Vector2 foot = views[i].FootPosition;
                _input.SetCuia(i, foot.x, -foot.y, RowOf(-foot.y), board[i]);
            }

            _plan.Plan(_input);
            _water = water;
            _waterA = new Color(water.r, water.g, water.b, WaterAlpha);
            // Núcleo del chorrito: el agua del río con +10 % de L, como el del vertido (guion §7.2; guía 6.5).
            _waterCore = ColorMath.ScaleOkLightness(new Color(water.r, water.g, water.b, 1f), 1.10f);
            _waterCore.a = 1f;

            // Anillos: la vida se acorta según el espaciado de los impactos para que no haya más de 8 brillos a la vez.
            _ringLife = IntroChoreography.RingMs;
            if (_plan.FruitCount > 1)
            {
                float spacing = (_plan.FruitImpactMs[_plan.FruitCount - 1] - _plan.FruitImpactMs[0]) / (_plan.FruitCount - 1);
                _ringLife = Mathf.Clamp(MaxAdditive * spacing, 100f, IntroChoreography.RingMs);
            }
            var strip = new Color(water.r, water.g, water.b, SheetAlpha);
            Color surface = ColorMath.WithAlpha(ColorMath.Lighten(new Color(water.r, water.g, water.b, 1f), 0.38f), 0.95f);
            Color line = ColorMath.WithAlpha(Palette.Glint, 0.8f);
            SetStripColor(_sheet, strip, surface, line, true);
            if (_lip != null)
            {
                SetStripColor(_lip, new Color(water.r, water.g, water.b, 0.92f), surface, line, false);
            }
            SetStripColor(_sideLeft, strip, surface, line, false);
            SetStripColor(_sideRight, strip, surface, line, false);
            for (int r = 0; r < _shelfLeft.Length; r++)
            {
                SetStripColor(_shelfLeft[r], strip, surface, line, false);
                SetStripColor(_shelfRight[r], strip, surface, line, false);
            }

            Color foam = ColorMath.Lighten(new Color(water.r, water.g, water.b, 1f), 0.6f);
            for (int i = 0; i < MaxCuias; i++)
            {
                _streamRings[i].color = foam;
            }

            for (int i = 0; i < LipFoamDots; i++)
            {
                if (_lipFoam[i] != null)
                {
                    _lipFoam[i].color = ColorMath.Lighten(foam, 0.5f);
                }
            }

            for (int i = 0; i < MaxCuias; i++)
            {
                if (_streams[i].color != _waterCore)
                {
                    _streams[i].color = _waterCore;
                }
            }

            for (int k = 0; k < _plan.FruitCount; k++)
            {
                int tint = _plan.FruitTint[k];
                _fruits[k].sprite = _art.FruitFlight(tint);
                Color c = Palette.Tint(tint);
                _ringColor[k] = ColorMath.Lighten(c, 0.55f);
                _plumeColor[k] = new Color(c.r, c.g, c.b, 1f);
            }

            for (int i = 0; i < n; i++)
            {
                views[i].Lock();
                views[i].SetSealed(false);
                views[i].SetPose(Vector2.zero, 0f);
                for (int k = 0; k < views[i].Capacity; k++)
                {
                    views[i].SetDose(k, Color.clear, 0f, 0);
                }

                views[i].Murky.Clear(); // la trama, el «?», la insignia y los grumos aparecen en el primer impacto
                views[i].Mud.Clear();
                if (board[i].HasHidden)
                {
                    views[i].Murky.Prewarm(); // los sprites de la insignia «?×n», ya: el primer impacto no crea texturas (A10)
                }
            }

            PlanDrops(water);
            Troop?.Begin(_plan, _input);
            Ambient?.SetPollenPaused(true); // el polen se pausa: en los primeros 100 ms solo se mueve el río (guion §7.2)
            _t = 0f;
            _tail = false;
            _fadeLeft = -1f;
            Running = true;
            if (!_layer.gameObject.activeSelf)
            {
                _layer.gameObject.SetActive(true);
            }

            // Los pools, registrados (ocultos) en su Canvas ya: mostrarlos en mitad de la intro no asigna (A10).
            for (int i = 0; i < _graphics.Count; i++)
            {
                GraphicPool.EnsureRegistered(_graphics[i]);
            }

            Apply(0f);
        }

        /// <summary>Avanza la intro (o la cola de sus restos).</summary>
        public void Tick(float dtMs)
        {
            if (_fadeLeft >= 0f)
            {
                _fadeLeft -= dtMs;
                if (_fadeLeft <= 0f)
                {
                    HideRemnants();
                    return;
                }

                ApplyRemnants(_frozenT, RemnantAlpha);
                return;
            }

            if (!Running && !_tail)
            {
                return;
            }

            _t += dtMs;
            if (Running && _t >= _input.TotalMs)
            {
                FinishNatural();
            }

            Apply(_t);
            if (_tail && _t >= _input.TotalMs + TailDuration())
            {
                HideRemnants();
            }
        }

        /// <summary>
        /// Enganche de depuración para las capturas: lleva la intro al instante <paramref name="ms"/>. Todo lo que se ve es
        /// función del tiempo (también la onda del menisco), así que la captura es determinista. Mientras la intro corre se puede
        /// ir hacia delante o hacia atrás; una vez terminada (T alcanzado: las vistas ya están desbloqueadas y con su contenido
        /// final), solo hacia la cola (≥ T): volver a un instante anterior exige relanzarla (<c>GameScreen.DebugReplayIntro</c>).
        /// </summary>
        /// <exception cref="InvalidOperationException">No hay intro, o ya terminó y <paramref name="ms"/> &lt; T.</exception>
        public void Seek(float ms)
        {
            if (!Running && !_tail)
            {
                throw new InvalidOperationException("IntroAnimation.Seek: no hay intro en curso ni restos que buscar.");
            }

            if (!Running && ms < _input.TotalMs)
            {
                throw new InvalidOperationException("IntroAnimation.Seek: la intro ya terminó (T = " + _input.TotalMs + " ms); para ir a "
                    + ms + " ms hay que relanzarla.");
            }

            _t = Mathf.Max(0f, ms);
            if (Running && _t >= _input.TotalMs)
            {
                FinishNatural();
            }

            Apply(_t);
        }

        /// <summary>
        /// Lleva la vista al estado final en el mismo fotograma (02 §4.2). Con <paramref name="skipped"/> (un toque), los restos
        /// se funden en ≤ 100 ms en su sitio, sin moverse; si no (fin natural), siguen su cola (anillos, salida de la tropa).
        /// </summary>
        public void Complete(bool skipped = true)
        {
            if (Running)
            {
                FinishNatural();
            }

            if (!skipped || !_tail)
            {
                return;
            }

            _tail = false;
            _frozenT = _t;
            _fadeLeft = SkipFadeMs;
            Ambient?.SetCanopyShake(0f);
            ReleaseRipples(); // tras el salto la onda sigue con el reloj del juego
            ApplyRemnants(_frozenT, 1f);
        }

        /// <summary>Retira en el acto todos los restos (carga de otro nivel, vuelta de segundo plano).</summary>
        public void Clear()
        {
            if (Running)
            {
                FinishNatural();
            }

            HideRemnants();
        }

        /// <summary>Añade todos los gráficos de la intro y de la tropa (tests: ninguno recibe toques).</summary>
        public void CollectGraphics(List<Graphic> graphics)
        {
            graphics.AddRange(_graphics);
            Troop?.CollectGraphics(graphics);
        }

        /// <summary>Cajas de los gráficos de la intro a la vista (dp del lienzo, y desde arriba), sin la tropa (tests).</summary>
        public void CollectVisibleStreams(List<Rect> rects)
        {
            for (int i = 0; i < MaxCuias; i++)
            {
                StreamGraphic s = _streams[i];
                if (s.enabled && s.Range.y > s.Range.x)
                {
                    Vector2 a = s.CurveStart;
                    Vector2 b = s.CurveEnd;
                    rects.Add(Rect.MinMaxRect(Mathf.Min(a.x, b.x) - 2f, -Mathf.Max(a.y, b.y), Mathf.Max(a.x, b.x) + 2f, -Mathf.Min(a.y, b.y)));
                }
            }
        }

        /// <summary>Chorrito de la cuia <paramref name="i"/> a la vista (tests y capturas).</summary>
        public bool StreamVisible(int i)
        {
            return (uint)i < MaxCuias && _streams[i].enabled;
        }

        /// <summary>Fruta <paramref name="k"/> a la vista (tests y capturas).</summary>
        public bool FruitVisible(int k)
        {
            return (uint)k < MaxFruits && _fruits[k].enabled;
        }

        /// <summary>Imagen de la fruta <paramref name="k"/> (tests).</summary>
        public Image FruitImage(int k)
        {
            return _fruits[k];
        }

        private int RowOf(float footY)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int r = 0; r < Mathf.Clamp(_input.Rows, 1, IntroInput.MaxRows); r++)
            {
                float d = Mathf.Abs(_input.RowFootY[r] - footY);
                if (d < bestD)
                {
                    bestD = d;
                    best = r;
                }
            }

            return best;
        }

        // Cola tras T: con la tropa, su reposo y su salida (D-011, punto 3: hasta ExitStartMs + ExitMs, contado desde T); sin
        // ella (dosel), los 300 ms de siempre (los anillos y las gotas acaban antes). Con Reducir movimiento, el miquito quieto.
        private float TailDuration()
        {
            if (ReduceMotion)
            {
                return ReducedTroopHoldMs + ReducedTroopFadeMs;
            }

            if (Troop == null || !Troop.Active)
            {
                return CanopyTailMs;
            }

            return Mathf.Max(0f, _plan.ExitStartMs - _input.TotalMs) + IntroChoreography.ExitMs;
        }

        private void FinishNatural()
        {
            Running = false;
            Ambient?.SetPollenPaused(false); // el polen vuelve con el input
            int n = Mathf.Min(_views.Length, _input.Count);
            for (int i = 0; i < n; i++)
            {
                if (_views[i] == null)
                {
                    continue;
                }

                _views[i].Unlock();
                _views[i].ShowContent(_final[i], _sealed[i]);
            }

            _tail = true;
        }

        private void Apply(float t)
        {
            if (AmbientLayer.Frozen)
            {
                ShaderClock.Freeze(t / 1000f); // capturas: la onda del líquido y el flujo del chorro, función del tiempo de la intro
            }

            if (Running)
            {
                ApplyCuias(t);
            }

            ApplyRipples(t);
            ApplyRemnants(t, 1f);
        }

        // Onda del menisco en función de t (A14): su edad es t menos el último evento de la cuia (el inicio del llenado, con
        // menisco y onda como en el vertido, D-006, o el impacto de una fruta) y su fase es fija por cuia y evento; nada se
        // acumula en el líquido. En la cola (tras T), una onda nueva del juego (un vertido) manda sobre la de la intro.
        private void ApplyRipples(float t)
        {
            IntroChoreography p = _plan;
            int n = Mathf.Min(_views.Length, _input.Count);
            for (int i = 0; i < n; i++)
            {
                _rippleAt[i] = -1f;
                _rippleEvent[i] = 0;
                if (!ReduceMotion && p.FillStartMs[i] >= 0f && t >= p.FillStartMs[i])
                {
                    _rippleAt[i] = p.FillStartMs[i];
                    _rippleEvent[i] = 1;
                }
            }

            if (!ReduceMotion)
            {
                for (int k = 0; k < p.FruitCount; k++)
                {
                    int c = p.FruitCuia[k];
                    float at = p.FruitImpactMs[k];
                    if ((uint)c < (uint)n && t >= at && at >= _rippleAt[c])
                    {
                        _rippleAt[c] = at;
                        _rippleEvent[c] = k + 2;
                    }
                }
            }

            for (int i = 0; i < n; i++)
            {
                CuiaView v = _views[i];
                if (v == null || (!Running && !v.RippleDriven))
                {
                    continue;
                }

                v.SetRippleAt(_rippleAt[i] >= 0f ? t - _rippleAt[i] : -1f, RipplePhase(i, _rippleEvent[i]));
            }
        }

        // Fase fija de la onda de la cuia i en su evento e (0 sin onda): determinista, distinta entre cuias y eventos.
        private static float RipplePhase(int cuia, int ev)
        {
            return Mathf.Repeat((2.3f * (ev + 1)) + (1.7f * cuia), Mathf.PI * 2f);
        }

        // Devuelve la onda de cada cuia al reloj del juego (fin de la intro, salto o carga de otro nivel).
        private void ReleaseRipples()
        {
            for (int i = 0; i < _views.Length; i++)
            {
                if (_views[i] != null)
                {
                    _views[i].ReleaseRipple();
                }
            }
        }

        // Cuias: llenado por cuia con el calendario, solo hasta sus dosis finales; floración OKLab por racha de abajo arriba.
        private void ApplyCuias(float t)
        {
            IntroChoreography p = _plan;
            int n = Mathf.Min(_views.Length, _input.Count);
            if (ReduceMotion)
            {
                float s = Mathf.Clamp01(t / Mathf.Max(1f, _input.TotalMs));
                for (int i = 0; i < n; i++)
                {
                    CuiaView v = _views[i];
                    Cuia final = _final[i];
                    for (int k = 0; k < v.Capacity; k++)
                    {
                        if (k >= final.Count)
                        {
                            v.SetDose(k, Color.clear, 0f, 0);
                            continue;
                        }

                        bool hidden = final.IsHiddenAt(k);
                        int tint = final.ColorAt(k);
                        bool mud = !hidden && tint == Cuia.MudColor;
                        bool inert = hidden || mud; // ocultas y Barro: sin tinte ni patrón
                        Color c = hidden ? MurkyColor : Palette.Tint(tint);
                        var faded = new Color(c.r, c.g, c.b, s);
                        v.SetDose(k, faded, 1f, inert ? 0 : tint);
                        if (hidden)
                        {
                            v.Liquid.SetDose(k, faded, 1f, 0, LiquidGraphic.MurkyLook.Hidden); // ya turbia: trama y «?»
                        }
                        else if (mud)
                        {
                            v.Liquid.SetMatteDose(k, faded, 1f); // el Barro, mate desde el primer fotograma (T20)
                        }
                    }
                }

                return;
            }

            for (int i = 0; i < n; i++)
            {
                CuiaView v = _views[i];
                int doses = _input.Doses[i];
                float level = 0f;
                if (p.FillStartMs[i] >= 0f && t >= p.FillStartMs[i])
                {
                    float span = Mathf.Max(1f, p.FillEndMs[i] - p.FillStartMs[i]);
                    level = doses * Ease.Out(Mathf.Clamp01((t - p.FillStartMs[i]) / span)); // menisco y onda: ApplyRipples
                }

                for (int k = 0; k < v.Capacity; k++)
                {
                    if (k >= doses)
                    {
                        v.SetDose(k, Color.clear, 0f, 0);
                        continue;
                    }

                    float fill = Mathf.Clamp01(level - k);
                    if (_input.IsInert(i, k))
                    {
                        // Ocultas y Barro: agua del río hasta el primer impacto y, desde ese fotograma, su pardo con su marca.
                        bool hidden = _input.Hidden[(i * IntroInput.MaxDoses) + k];
                        bool shown = p.FirstImpactMs >= 0f && t >= p.FirstImpactMs;
                        if (shown && hidden)
                        {
                            v.PaintHiddenDose(k);
                        }
                        else if (shown)
                        {
                            v.PaintMudDose(k);
                        }
                        else
                        {
                            v.SetDose(k, _waterA, fill, 0);
                            v.Murky.HideMark(k);
                            v.Mud.HideMark(k);
                        }

                        continue;
                    }

                    int run = p.RunOf(i, k);
                    float s = run >= 0 ? Mathf.Clamp01((t - p.RunBloomMs[run]) / StripeMs) : 0f;
                    if (run < 0 || s <= 0f)
                    {
                        v.SetDose(k, _waterA, fill, 0);
                    }
                    else
                    {
                        int tint = p.RunTint[run];
                        v.SetDose(k, ColorMath.OkLabLerp(_waterA, Palette.Tint(tint), s), 1f, tint);
                    }
                }
            }
        }

        // Todo lo cosmético, por tiempo: río, madera, chorritos, frutas, penachos, anillos, gotas, dosel y tropa. alpha < 1:
        // fundido del salto (en su sitio).
        private void ApplyRemnants(float t, float alpha)
        {
            IntroChoreography p = _plan;
            float unit = _input.Unit;
            float total = _input.TotalMs;
            if (ReduceMotion)
            {
                HideWater();
                HideFruits();
                Ambient?.SetRiverBoost(0f);
                if (Troop != null && Troop.Active)
                {
                    float hold = total + ReducedTroopHoldMs;
                    float a = t < total ? Mathf.Clamp01(t / Mathf.Max(1f, total)) : 1f - Mathf.Clamp01((t - hold) / ReducedTroopFadeMs);
                    Troop.ApplyStill(a * alpha);
                }

                return;
            }

            // 1. El río crece (+12 L*, destellos) y vuelve a su sitio cuando la madera se escurre.
            float drain = Mathf.Clamp01((t - p.DrainStartMs) / Mathf.Max(1f, p.DrainEndMs - p.DrainStartMs));
            float boost = t < p.RiseEndMs ? Ease.Out(t / Mathf.Max(1f, p.RiseEndMs)) : 1f - drain;
            Ambient?.SetRiverBoost(boost * alpha);

            // 2. Lámina sobre el canto superior del marco, desde el centro hacia las esquinas.
            float wood = (1f - drain) * alpha;
            if (t >= p.SheetStartMs && wood > 0.001f)
            {
                float half = Mathf.Min(p.SheetHalfWidth, (t - p.SheetStartMs) * p.SheetSpeed);
                float a = Mathf.Clamp01((t - p.SheetStartMs) / Mathf.Max(1f, p.SheetFadeMs)) * wood;
                float thick = _input.FrameDp + IntroChoreography.SheetAbovePanelDp;
                float taper = SheetTaperDp * Mathf.Clamp01((p.SheetHalfWidth - half) / SheetTaperDp);
                ShowStrip(_sheet, new Vector2(p.SheetCenterX, -p.FrameInnerY), half, half, thick, false, false, a, unit, t * 0.1f);
                _sheet.SetTaper(taper, taper);
                ShowLip(half, taper, a, unit, t);
            }
            else
            {
                HideStrip(_sheet);
                HideLip();
            }

            // 3. Tiras por el borde interior de los costados del marco y por la cara de cada repisa.
            if (t >= p.SheetCornerMs && wood > 0.001f)
            {
                float len = Mathf.Min(p.SideBottomY - p.FrameInnerY + 2f, (t - p.SheetCornerMs) * p.SideSpeed);
                float top = p.FrameInnerY - 2f;
                float w = IntroChoreography.StripDp;
                float flow = t * FlowDpPerMs;
                ShowStrip(_sideLeft, new Vector2(p.InnerLeftX - w - 0.5f, -top), 0f, len, w, true, false, wood, unit, flow);
                ShowStrip(_sideRight, new Vector2(p.InnerRightX + w + 0.5f, -top), 0f, len, w, true, true, wood, unit, flow);
            }
            else
            {
                HideStrip(_sideLeft);
                HideStrip(_sideRight);
            }

            float shelfHalf = (p.InnerRightX - p.InnerLeftX) * 0.5f;
            for (int r = 0; r < _shelfLeft.Length; r++)
            {
                if (r < p.Rows - 1 && t >= p.ShelfArriveMs[r] && wood > 0.001f)
                {
                    float len = Mathf.Min(shelfHalf, (t - p.ShelfArriveMs[r]) * p.ShelfSpeed);
                    float y = -(p.ShelfStripY[r] + IntroChoreography.StripDp);
                    float flow = t * FlowDpPerMs;
                    ShowStrip(_shelfLeft[r], new Vector2(p.InnerLeftX, y), 0f, len, IntroChoreography.StripDp, false, false, wood, unit, flow);
                    ShowStrip(_shelfRight[r], new Vector2(p.InnerRightX, y), len, 0f, IntroChoreography.StripDp, false, false, wood, unit, -flow);
                }
                else
                {
                    HideStrip(_shelfLeft[r]);
                    HideStrip(_shelfRight[r]);
                }
            }

            // 4. Un chorrito por cuia no vacía, de la madera que tiene encima a la superficie que sube; la cola cae al llenarse.
            for (int i = 0; i < MaxCuias; i++)
            {
                StreamGraphic s = _streams[i];
                _foamWant[i] = false;
                bool on = i < p.CuiaCount && p.StreamOpenMs[i] >= 0f && t >= p.StreamOpenMs[i] && t < p.FillEndMs[i] + p.StreamTailMs;
                if (!on)
                {
                    if (s.enabled)
                    {
                        s.enabled = false;
                    }

                    continue;
                }

                float x = _input.CenterX[i];
                float topY = p.StreamTopY[i];
                float headY;
                if (t < p.ContactMs[i])
                {
                    float h = Mathf.Clamp01((t - p.StreamOpenMs[i]) / Mathf.Max(1f, p.ContactMs[i] - p.StreamOpenMs[i]));
                    headY = Mathf.Lerp(topY, p.CavityFloorY[i], Ease.In(h));
                }
                else
                {
                    float span = Mathf.Max(1f, p.FillEndMs[i] - p.FillStartMs[i]);
                    float level = _input.Doses[i] * Ease.Out(Mathf.Clamp01((t - p.FillStartMs[i]) / span));
                    headY = _input.FootY[i] - ((CuiaGeometry.CavityBottomU + level) * unit);
                }

                float from = t > p.FillEndMs[i] ? Mathf.Clamp01((t - p.FillEndMs[i]) / Mathf.Max(1f, p.StreamTailMs)) : 0f;
                var start = new Vector2(x, -topY);
                var end = new Vector2(x, -Mathf.Max(headY, topY + 1f));
                if (!s.enabled)
                {
                    s.enabled = true;
                }

                s.SetCurve(start, (start + end) * 0.5f, end, from, 1f, BoardAnimator.StreamWidthStartU * unit, 0.10f * unit, unit);
                s.canvasRenderer.SetAlpha(alpha);

                // Espuma donde el chorrito toca el agua mientras la cuia se llena (la misma gramática que el vertido).
                _foamWant[i] = t >= p.ContactMs[i] && t < p.FillEndMs[i];
                _foamY[i] = headY;
            }

            // Espuma: como mucho 8 brillos aditivos a la vez (guion §13); se quedan los chorritos abiertos más recientes.
            int foamShown = 0;
            for (int i = 0; i < MaxCuias; i++)
            {
                Image foamRing = _streamRings[i];
                bool show = _foamWant[i] && NewerFoam(i) < MaxAdditive;
                if (!show)
                {
                    SetEnabled(foamRing, false);
                    continue;
                }

                foamShown++;
                float pulse = 0.5f + (0.5f * Mathf.Sin(t * 0.045f));
                float rw = Mathf.Lerp(0.55f, 0.75f, pulse) * unit;
                SetEnabled(foamRing, true);
                Place(foamRing.rectTransform, new Vector2(_input.CenterX[i], -_foamY[i]), new Vector2(rw * 2f, rw * 0.5f), 0f);
                foamRing.canvasRenderer.SetAlpha(alpha * Mathf.Lerp(0.35f, 0.55f, pulse));
            }

            // Anillos y penachos activos (los impactos van en orden): los más recientes, dentro del presupuesto.
            int ringBudget = MaxAdditive - foamShown;
            int ringNewest = -1;
            int plumeNewest = -1;
            for (int k = 0; k < p.FruitCount; k++)
            {
                if (t >= p.FruitImpactMs[k])
                {
                    ringNewest = k;
                    plumeNewest = k;
                }
            }

            int particleBudget = MaxParticles - ActiveDrops(t) - ActiveLipFoam();

            // 5. Frutas: en la mano (rama) o asomando por detrás del HUD (dosel), caída con g, giro y fundido al impacto.
            float shakeAmp = 0f;
            for (int k = 0; k < MaxFruits; k++)
            {
                Image fruit = _fruits[k];
                Image ring = _rings[k];
                Image plume = _plumes[k];
                if (k >= p.FruitCount)
                {
                    SetEnabled(fruit, false);
                    SetEnabled(ring, false);
                    SetEnabled(plume, false);
                    continue;
                }

                float release = p.FruitReleaseMs[k];
                float impact = p.FruitImpactMs[k];
                float tau = Mathf.Max(1f, impact - release);
                // En la mano (o en la rama: sacudida, o la fruta que ningún miquito alcanza) 60 ms antes de soltarla; del dosel,
                // asoma al soltarla; con la tropa lateral, una fruta en la mano cada vez (D-021).
                bool held = p.FruitMonkey[k] >= 0 || p.Perch == IntroPerch.Branch;
                float showFrom = p.Perch == IntroPerch.Side ? p.SideHoldFromMs[k] : (held ? release - IntroChoreography.HoldMs : release);
                if (p.FruitMonkey[k] < 0)
                {
                    // Cae del dosel o de la rama sin miquito: el follaje de las esquinas (de donde sale la rama) se sacude.
                    shakeAmp = Mathf.Max(shakeAmp, 1f - Mathf.Clamp01(Mathf.Abs(t - release) / 75f));
                }

                if (t >= showFrom && t < impact + IntroChoreography.FruitFadeMs)
                {
                    float size;
                    float rot;
                    Vector2 pos;
                    float a;
                    if (t < release)
                    {
                        size = p.FruitFlightDp;
                        rot = 0f;
                        pos = new Vector2(p.FruitReleaseX[k], -p.FruitReleaseY[k]);
                        a = 1f;
                    }
                    else if (t < impact)
                    {
                        float dt = t - release;
                        size = Mathf.Lerp(p.FruitFlightDp, p.FruitImpactDp, dt / tau);
                        rot = p.FruitSpin[k] * dt;
                        pos = new Vector2(Mathf.Lerp(p.FruitReleaseX[k], p.FruitImpactX[k], dt / tau), -p.FruitY(k, dt)); // ≤ 250 dp/s en x
                        a = 1f;
                    }
                    else
                    {
                        size = p.FruitImpactDp;
                        rot = p.FruitSpin[k] * tau;
                        pos = new Vector2(p.FruitImpactX[k], -p.FruitImpactY[k]);
                        a = 1f - ((t - impact) / IntroChoreography.FruitFadeMs);
                    }

                    SetEnabled(fruit, true);
                    Place(fruit.rectTransform, pos, new Vector2(size, size), rot);
                    fruit.canvasRenderer.SetAlpha(a * alpha);
                }
                else
                {
                    SetEnabled(fruit, false);
                }

                // Anillo elíptico sobre la superficie (0,4 → 1,3 u, alfa 0,4 → 0 en 250 ms, o menos si los impactos van juntos).
                if (t >= impact && t < impact + _ringLife && ringNewest - k < ringBudget)
                {
                    float q = (t - impact) / _ringLife;
                    float w = Mathf.Lerp(0.4f, 1.3f, Ease.OutCubic(q)) * unit;
                    SetEnabled(ring, true);
                    Place(ring.rectTransform, new Vector2(p.FruitImpactX[k], -p.FruitImpactY[k]), new Vector2(w * 2f, w * 0.55f), 0f);
                    Color c = _ringColor[k];
                    c.a = 0.4f * (1f - q);
                    if (ring.color != c)
                    {
                        ring.color = c;
                    }

                    ring.canvasRenderer.SetAlpha(alpha);
                }
                else
                {
                    SetEnabled(ring, false);
                }

                // Penacho del color que baja hasta su racha (≤ 60 ms).
                if (t >= impact && t < impact + IntroChoreography.PlumeMs && plumeNewest - k < particleBudget)
                {
                    float q = (t - impact) / IntroChoreography.PlumeMs;
                    int c = p.FruitCuia[k];
                    int run = p.FruitRunFirst[k];
                    float mid = (p.RunFrom[run] + p.RunTo[run] + 1) * 0.5f;
                    float targetY = _input.FootY[c] - ((CuiaGeometry.CavityBottomU + mid) * unit);
                    float y = Mathf.Lerp(p.FruitImpactY[k], targetY, Ease.Out(q));
                    float sz = 0.9f * unit;
                    SetEnabled(plume, true);
                    Place(plume.rectTransform, new Vector2(p.FruitImpactX[k], -y), new Vector2(sz, sz * 1.3f), 0f);
                    Color pc = _plumeColor[k];
                    pc.a = 0.75f * (1f - q);
                    if (plume.color != pc)
                    {
                        plume.color = pc;
                    }

                    plume.canvasRenderer.SetAlpha(alpha);
                }
                else
                {
                    SetEnabled(plume, false);
                }
            }

            // Gotas balísticas (esquinas del desborde y primer impacto de cada fila), con gravedad y fundido final.
            for (int j = 0; j < MaxDrops; j++)
            {
                Image drop = _drops[j];
                float dt = t - _dropT0[j];
                if (j >= _dropCount || dt < 0f || dt >= DropLifeMs)
                {
                    SetEnabled(drop, false);
                    continue;
                }

                Vector2 pos = _dropFrom[j] + (_dropVel[j] * dt) + new Vector2(0f, -0.5f * _dropG[j] * dt * dt);
                SetEnabled(drop, true);
                float size = 0.16f * unit;
                Place(drop.rectTransform, pos, new Vector2(size, size), 0f);
                if (drop.color != _dropColor[j])
                {
                    drop.color = _dropColor[j];
                }

                drop.canvasRenderer.SetAlpha(alpha * (1f - Mathf.Clamp01((dt - (DropLifeMs * 0.6f)) / (DropLifeMs * 0.4f))));
            }

            // 6. Dosel: el follaje de las esquinas se sacude ±6° con cada fruta que suelta.
            if (Ambient != null)
            {
                float angle = alpha >= 1f ? CanopyShakeDeg * shakeAmp * Mathf.Sin(t * (Mathf.PI * 2f / 150f)) : 0f;
                Ambient.SetCanopyShake(angle);
            }

            // 7. Tropa.
            Troop?.Apply(t, alpha);
        }

        // Gotas de la intro (≤ 20): 2 en cada esquina cuando la lámina llega a ellas y 2 en el primer impacto de cada fila;
        // lanzadas a 1,5-2,5 u/s hacia arriba (±15-35°) con gravedad de 18 u/s² (guía 6.6), deterministas.
        private void PlanDrops(Color water)
        {
            IntroChoreography p = _plan;
            float u = _input.Unit;
            _dropCount = 0;
            var waterOpaque = new Color(water.r, water.g, water.b, 1f);
            AddDrops(p.SheetCornerMs, new Vector2(p.InnerLeftX, -p.FrameInnerY), waterOpaque, u);
            AddDrops(p.SheetCornerMs, new Vector2(p.InnerRightX, -p.FrameInnerY), waterOpaque, u);
            for (int k = 0; k < p.FruitCount; k++)
            {
                if (p.FruitFirstInRow[k])
                {
                    Color tint = Palette.Tint(p.FruitTint[k]);
                    AddDrops(p.FruitImpactMs[k], new Vector2(p.FruitImpactX[k], -p.FruitImpactY[k]), new Color(tint.r, tint.g, tint.b, 1f), u);
                }
            }
        }

        private void AddDrops(float t0, Vector2 from, Color color, float unit)
        {
            for (int i = 0; i < DropsPerEvent && _dropCount < MaxDrops; i++)
            {
                int j = _dropCount++;
                float side = (i % 2 == 0) ? -1f : 1f;
                float h = Hash01(j);
                float ang = side * Mathf.Lerp(15f, 35f, h) * Mathf.Deg2Rad;
                float speed = Mathf.Lerp(1.5f, 2.5f, Hash01(j + 31)) * unit / 1000f; // u/s → dp/ms
                _dropT0[j] = t0;
                _dropFrom[j] = from;
                _dropVel[j] = new Vector2(Mathf.Sin(ang) * speed, Mathf.Cos(ang) * speed);
                _dropG[j] = 18f * unit / 1000000f; // 18 u/s² → dp/ms²
                _dropColor[j] = color;
            }
        }

        private static float Hash01(int i)
        {
            uint x = ((uint)i * 2654435761u) ^ 0x68E31DA4u;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return (x & 0xFFFF) / 65535f;
        }

        private void HideRemnants()
        {
            _tail = false;
            _fadeLeft = -1f;
            ReleaseRipples();
            Ambient?.SetPollenPaused(false);
            HideWater();
            HideFruits();
            Ambient?.SetRiverBoost(0f);
            Ambient?.SetCanopyShake(0f);
            Troop?.Hide();
        }

        // Labio del río que desborda: de panel.top − 14 (el borde inferior del río) a panel.top + 2, del ancho de la lámina, con
        // 6 puntos de espuma en la cresta (nunca sobre el HUD). Va por detrás de la rama; desde el canto hasta la franja de la
        // rama (panel.top − 11), al 70 %, para que la rama se lea sobre la crecida y no sobre una tabla.
        private void ShowLip(float half, float taper, float alpha, float unit, float t)
        {
            WaterStripGraphic? lip = _lip;
            if (lip == null)
            {
                return;
            }

            IntroChoreography p = _plan;
            float bottom = _input.PanelTop + LipBelowPanelDp;
            float crest = Mathf.Max(_input.PanelTop - LipAbovePanelDp, _input.HudBottom + 1f);
            float thick = bottom - crest;
            if (thick < 1f)
            {
                HideLip();
                return;
            }

            ShowStrip(lip, new Vector2(p.SheetCenterX, -bottom), half, half, thick, false, false, alpha, unit, t * FlowDpPerMs);
            lip.SetTaper(taper, taper);
            lip.SetFade(Mathf.Min(thick - 1f, LipBelowPanelDp + LipBranchBandTopDp), LipBranchBandAlpha);
            for (int i = 0; i < LipFoamDots; i++)
            {
                Image dot = _lipFoam[i];
                float frac = ((i + 0.5f) / LipFoamDots * 2f) - 1f; // repartidos a lo ancho del panel
                float x = p.SheetCenterX + (frac * p.SheetHalfWidth * 0.92f);
                bool on = Mathf.Abs(x - p.SheetCenterX) <= half - Mathf.Max(1f, taper); // en la cresta, no en la lengua
                SetEnabled(dot, on);
                if (on)
                {
                    float bob = 0.5f * Mathf.Sin((t * 0.012f) + (i * 1.7f));
                    Place(dot.rectTransform, new Vector2(x, -(crest + 0.8f + bob)), new Vector2(LipFoamDp, LipFoamDp), 0f);
                    dot.canvasRenderer.SetAlpha(alpha * 0.95f);
                }
            }
        }

        private void HideLip()
        {
            if (_lip == null)
            {
                return;
            }

            HideStrip(_lip);
            for (int i = 0; i < LipFoamDots; i++)
            {
                SetEnabled(_lipFoam[i], false);
            }
        }

        // Chorritos con espuma abiertos después que el i (empate: el de índice mayor).
        private int NewerFoam(int i)
        {
            int newer = 0;
            float open = _plan.StreamOpenMs[i];
            for (int j = 0; j < MaxCuias; j++)
            {
                if (j != i && _foamWant[j] && (_plan.StreamOpenMs[j] > open || (_plan.StreamOpenMs[j] == open && j > i)))
                {
                    newer++;
                }
            }

            return newer;
        }

        private int ActiveDrops(float t)
        {
            int n = 0;
            for (int j = 0; j < _dropCount; j++)
            {
                float dt = t - _dropT0[j];
                n += dt >= 0f && dt < DropLifeMs ? 1 : 0;
            }

            return n;
        }

        private int ActiveLipFoam()
        {
            int n = 0;
            for (int i = 0; i < LipFoamDots; i++)
            {
                n += _lipFoam[i] != null && _lipFoam[i].enabled ? 1 : 0;
            }

            return n;
        }

        private static bool Shown(Graphic g)
        {
            return g.enabled && g.gameObject.activeInHierarchy && g.canvasRenderer.GetAlpha() > 0.001f;
        }

        private void HideWater()
        {
            HideLip();
            HideStrip(_sheet);
            HideStrip(_sideLeft);
            HideStrip(_sideRight);
            for (int r = 0; r < _shelfLeft.Length; r++)
            {
                HideStrip(_shelfLeft[r]);
                HideStrip(_shelfRight[r]);
            }

            for (int i = 0; i < MaxCuias; i++)
            {
                if (_streams[i].enabled)
                {
                    _streams[i].enabled = false;
                }

                SetEnabled(_streamRings[i], false);
            }
        }

        private void HideFruits()
        {
            for (int k = 0; k < MaxFruits; k++)
            {
                SetEnabled(_fruits[k], false);
                SetEnabled(_rings[k], false);
                SetEnabled(_plumes[k], false);
            }

            for (int j = 0; j < MaxDrops; j++)
            {
                SetEnabled(_drops[j], false);
            }
        }

        private static void ShowStrip(WaterStripGraphic strip, Vector2 origin, float back, float forward, float thickness, bool vertical, bool flip, float alpha, float unit, float flow)
        {
            if (back + forward < 0.05f || alpha <= 0.001f)
            {
                HideStrip(strip);
                return;
            }

            if (!strip.enabled)
            {
                strip.enabled = true;
            }

            strip.SetStrip(origin, back, forward, thickness, unit, vertical, flip);
            strip.SetFlow(flow);
            strip.canvasRenderer.SetAlpha(alpha);
        }

        private static void HideStrip(WaterStripGraphic strip)
        {
            if (strip.enabled)
            {
                strip.enabled = false;
            }
        }

        private static void SetStripColor(WaterStripGraphic strip, Color body, Color surface, Color line, bool lip)
        {
            if (strip.color != Color.white)
            {
                strip.color = Color.white;
            }

            strip.SetLook(body, surface, line, lip);
        }

        private static void SetEnabled(Graphic g, bool on)
        {
            if (g.enabled != on)
            {
                g.enabled = on;
            }
        }

        private static void Place(RectTransform rt, Vector2 pos, Vector2 size, float angle)
        {
            if (rt.anchoredPosition != pos)
            {
                rt.anchoredPosition = pos;
            }

            if (rt.sizeDelta != size)
            {
                rt.sizeDelta = size;
            }

            Quaternion r = angle == 0f ? Quaternion.identity : Quaternion.Euler(0f, 0f, angle);
            if (rt.localRotation != r)
            {
                rt.localRotation = r;
            }
        }

        private static void TopLeft(RectTransform rt, Vector2 pivot)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        private WaterStripGraphic NewStrip(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(WaterStripGraphic));
            go.transform.SetParent(_layer, false);
            WaterStripGraphic strip = go.GetComponent<WaterStripGraphic>();
            strip.Setup();
            TopLeft(strip.rectTransform, new Vector2(0f, 1f));
            strip.enabled = false;
            _graphics.Add(strip);
            return strip;
        }

        private Image NewImage(string name, Sprite? sprite, Material? material)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_layer, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            if (material != null)
            {
                img.material = material;
            }

            TopLeft(img.rectTransform, new Vector2(0.5f, 0.5f));
            img.enabled = false;
            _graphics.Add(img);
            return img;
        }
    }
}
