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
    /// de 2°, 07 §4.3) la hace <c>CuiaView</c>; los gestos (sacar, el revelado en la boca y la suelta) son de la tarea TJ3, y
    /// hasta entonces el overlay no tiene pasos propios.</para>
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
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Tinaja;
            Visible = on;
            if (on && !_prewarmed)
            {
                Prewarm();
            }

            bool art = on && MapArtRule.ShowProvisional;
            int count = on ? cuia.Count : 0;
            bool empty = count == 0;
            _count = count;
            if (art != _artShown)
            {
                _artShown = art;
                PlaceChip();
            }

            SetUpsideDown(empty);
            OverlayParts.SetVisible(_body, art);

            // Boca (R7): la fruta de arriba, con su tinte y su icono, en la boca de la vasija (solo con su cuerpo: sin él, la cima
            // ya se ve en el líquido del vidrio). Si la cima estuviera oculta (no debería), la boca va oscura y sin icono: lo
            // oculto nunca enseña su tinte.
            bool filled = on && !empty;
            bool mouth = filled && art;
            int top = count - 1;
            bool topShown = mouth && !cuia.IsHiddenAt(top);
            int tint = topShown ? cuia.ColorAt(top) : 0;
            if (mouth && tint != _mouthTint)
            {
                _mouthTint = tint;
                OverlayParts.SetColor(_mouth, tint > 0 ? Palette.Tint(tint) : Palette.WoodDark);
                if (tint > 0)
                {
                    _mouthGlyph.sprite = Host.Art.TinajaGlyph(tint);
                }
            }

            OverlayParts.SetVisible(_mouth, mouth);
            OverlayParts.SetVisible(_mouthGloss, mouth);
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
            _body = OverlayParts.NewImage("TinajaBody", Host.BodyFront, Host.Art.TinajaBody, Color.white);
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
            _mouthGlyph = OverlayParts.NewImage("TinajaMouthGlyph", Host.BodyFront, null, Color.white);
            _chip = OverlayParts.NewImage("TinajaChip", Host.BodyFront, Host.Art.TinajaChipPill, Color.white);
            _chip.type = Image.Type.Sliced;
            _chipTimes = OverlayParts.NewImage("TinajaChipX", _chip.rectTransform, Host.Art.MurkyBadgeTimes, Color.white);
            _chipDigit = OverlayParts.NewImage("TinajaChipN", _chip.rectTransform, Host.Art.MurkyBadgeDigit(1), Color.white);
            PlaceChipGlyph(_chipTimes, -0.5f * ChipGlyphStepDp);
            PlaceChipGlyph(_chipDigit, 0.5f * ChipGlyphStepDp);
            OverlayParts.SetVisible(_chipTimes, true); // cuelgan del chip: se ven y se ocultan con él
            OverlayParts.SetVisible(_chipDigit, true);
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

        // Vacía, boca abajo sobre el jirau (b.2 regla 8): la vasija volteada en su caja, sin boca ni chip.
        private void SetUpsideDown(bool on)
        {
            _upsideDown = on;
            var scale = new Vector3(1f, on ? -1f : 1f, 1f);
            RectTransform rt = _body.rectTransform;
            if (rt.localScale != scale)
            {
                rt.localScale = scale;
            }
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
