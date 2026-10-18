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
    /// a su altura con su icono, y el jirau bajo las dos ranuras (la de la tinaja y la de su cuia). Vacía, boca abajo sobre el
    /// jirau, sin boca ni chip.
    /// </summary>
    /// <remarks>
    /// <para><b>Nunca agua turbia</b> (contrato 27; 08 §5.2): bajo la vasija, la vista pinta las ocultas con el barro
    /// (<see cref="Palette.TinajaClay"/>), sin tinte, sin el pardo #8A7B6A y sin «?» (<c>CuiaView.ShowContent</c>), y
    /// <see cref="MurkyMarks"/> no pone la insignia «?×n» ni anima su revelado. Lo oculto no se ve por ningún lado: ni su color,
    /// ni su orden, ni qué frutas guarda (Q-Ti5: <c>ui_tinaja_show_contents</c> = false; no hay vista de contenidos).</para>
    /// <para><b>Arte</b> (<see cref="MapArtRule"/>): el cuerpo y el jirau son arte provisional y solo salen con
    /// <see cref="MapArtRule.ShowProvisional"/> (Editor y desarrollo) hasta que lleguen los PNG del artista; la boca, el chip y
    /// las bandas los pinta el código según el estado y salen siempre.</para>
    /// <para><b>Su cuia</b> (<see cref="CucharonOverlay"/>) va en la ranura contigua de la misma fila (02 §5; <c>BoardLayout</c>).
    /// El jirau lo dibuja la tinaja, que nunca pierde su tipo, porque es estado permanente (07 §4.3: «no un resaltado», D-002) y
    /// sigue bajo las dos ranuras cuando su cuia baja (la suelta). La pareja se reconoce por la cuia de tipo <c>Cucharon</c> a su
    /// lado y se recuerda; si la tinaja ya llega vacía (partida reanudada tras la suelta), por la Cuia chica de 2 de al lado, a la
    /// derecha antes que a la izquierda [P].</para>
    /// <para><b>Nada cambia al seleccionar</b> (D-002): todo cuelga del estado. Los gestos (sacar, el revelado en la boca, la
    /// suelta y el balanceo al seleccionar) son de la tarea TJ3; hasta entonces el overlay no tiene pasos propios.</para>
    /// <para>Sin asignaciones en <see cref="Show"/>, <see cref="Tick"/> ni <see cref="OnBoardLayout"/> (08 §5.4): los iconos de
    /// las frutas de la tinaja se generan en el primer <see cref="Show"/> (al cargar el nivel).</para>
    /// </remarks>
    public sealed class TinajaOverlay : CuiaOverlay
    {
        /// <summary>Bandas como mucho: las capas bajo la boca de una tinaja llena (capacidad 5).</summary>
        public const int MaxBands = 4;

        /// <summary>
        /// Centro del chip «×n» (u desde el pie) [P]: en el hombro, entre la banda más alta posible (la capa 3, hasta 4,41 u) y
        /// el agua de la boca (desde 5,89 u).
        /// </summary>
        public const float ChipCenterU = 5.32f;

        /// <summary>Alto del jirau (u) [P]: tres varas de ≈ 4,7 dp con la cuia de 48 dp.</summary>
        public const float JirauHeightU = 0.58f;

        /// <summary>Lo alto del jirau sobre la línea de pie (u) [P]: las dos cuias se asientan sobre sus varas.</summary>
        public const float JirauTopAboveFootU = 0.06f;

        /// <summary>
        /// Lo que el jirau sale por el lado de la tinaja, desde su eje (u) [P]: bajo su pie y algo más, sin llegar al canto de la
        /// ranura (la última de una fila llena toca el marco del panel).
        /// </summary>
        public const float JirauTinajaExtentU = 1.15f;

        /// <summary>Lo que el jirau sale por el lado de su cuia, desde su eje (u) [P]: hasta el poste, que se apoya en él.</summary>
        public const float JirauCuiaExtentU = ProceduralArt.TinajaPostXU + 0.14f;

        /// <summary>Mezcla hacia el blanco de la luz de una banda sobre el tinte de su capa [P].</summary>
        public const float BandLighten = 0.18f;

        /// <summary>Opacidad de una banda de luz [P].</summary>
        public const float BandAlpha = 0.92f;

        private const float ChipGlyphStepDp = ProceduralArt.MurkyBadgeCellDp * ProceduralArt.TinajaChipGlyphScale;

        private readonly Image[] _bands = new Image[MaxBands];
        private readonly Image[] _bandGlyphs = new Image[MaxBands];
        private RectTransform _layerRoot = null!;
        private Image _jirau = null!;
        private Image _jirauTie = null!;
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

        /// <summary>Capas con banda de luz (bit k = capa k desde el fondo): las reveladas bajo la boca.</summary>
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
                Prewarm(cuia);
            }

            bool art = on && MapArtRule.ShowProvisional;
            int count = on ? cuia.Count : 0;
            bool empty = count == 0;
            _count = count;
            _artShown = art;
            SetUpsideDown(empty);
            OverlayParts.SetVisible(_body, art);

            // Boca (R7): la fruta de arriba, con su tinte y su icono. Si la cima estuviera oculta (no debería), la boca va oscura
            // y sin icono: lo oculto nunca enseña su tinte.
            bool mouth = on && !empty;
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

            // Chip «×n»: las dosis que quedan dentro, contando la boca (b.2, «Qué se ve»).
            OverlayParts.SetVisible(_chip, mouth);
            if (mouth)
            {
                Sprite digit = Host.Art.MurkyBadgeDigit(count);
                if (_chipDigit.sprite != digit)
                {
                    _chipDigit.sprite = digit;
                }
            }

            // Bandas: las capas bajo la boca que ya están reveladas (R7, T6, I5; la Luciérnaga), a su altura y con su icono.
            int bands = 0;
            for (int k = 0; k < MaxBands; k++)
            {
                bool band = mouth && k < top && !cuia.IsHiddenAt(k);
                Image img = _bands[k];
                Image glyph = _bandGlyphs[k];
                if (band)
                {
                    bands |= 1 << k;
                    int bt = cuia.ColorAt(k);
                    Color c = ColorMath.Lighten(Palette.Tint(bt), BandLighten);
                    c.a = BandAlpha;
                    OverlayParts.SetColor(img, c);
                    Sprite gs = Host.Art.TinajaGlyph(bt);
                    if (glyph.sprite != gs)
                    {
                        glyph.sprite = gs;
                    }
                }

                OverlayParts.SetVisible(img, band);
                OverlayParts.SetVisible(glyph, band);
            }

            _bandMask = bands;
            PlaceJirau(on);
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            OverlayParts.Place(_body, 0f, 0.5f * ProceduralArt.TinajaBodyHeightU, ProceduralArt.TinajaBodyWidthU, ProceduralArt.TinajaBodyHeightU, u);
            OverlayParts.Place(_mouth, 0f, ProceduralArt.TinajaMouthCenterU, ProceduralArt.TinajaMouthWidthU, ProceduralArt.TinajaMouthHeightU, u);
            OverlayParts.Place(_mouthGloss, 0f, ProceduralArt.TinajaMouthCenterU, ProceduralArt.TinajaMouthWidthU, ProceduralArt.TinajaMouthHeightU, u);
            OverlayParts.Place(_mouthGlyph, 0f, ProceduralArt.TinajaMouthCenterU, ProceduralArt.TinajaMouthGlyphU, ProceduralArt.TinajaMouthGlyphU, u);
            for (int k = 0; k < MaxBands; k++)
            {
                float y = BandCenterU(k);
                float w = 2f * (ProceduralArt.TinajaHalfWidthU(y) - ProceduralArt.TinajaBandInsetU);
                OverlayParts.Place(_bands[k], 0f, y, w, ProceduralArt.TinajaBandHeightU, u);
                OverlayParts.Place(_bandGlyphs[k], 0f, y, ProceduralArt.TinajaBandGlyphU, ProceduralArt.TinajaBandGlyphU, u);
            }

            // El chip, en dp fijos (texto de ≥ 14 sp, 07 §4.3), centrado en el hombro.
            RectTransform chip = _chip.rectTransform;
            var chipPos = new Vector2(0f, ChipCenterU * u);
            var chipSize = new Vector2(ProceduralArt.TinajaChipWidthDp, ProceduralArt.TinajaChipHeightDp);
            if (chip.anchoredPosition != chipPos)
            {
                chip.anchoredPosition = chipPos;
            }

            if (chip.sizeDelta != chipSize)
            {
                chip.sizeDelta = chipSize;
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
            _jirau.type = Image.Type.Sliced;
            _jirauTie = OverlayParts.NewImage("TinajaJirauTie", _layerRoot, Host.Art.TinajaJirauTie, Color.white);

            // La vasija y lo que se ve en ella, en el cuerpo, sobre el vidrio y el líquido.
            _body = OverlayParts.NewImage("TinajaBody", Host.BodyFront, Host.Art.TinajaBody, Color.white);
            for (int k = 0; k < MaxBands; k++)
            {
                _bands[k] = OverlayParts.NewImage("TinajaBand" + k, Host.BodyFront, Host.Art.TinajaBand, Color.white);
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

        // Prepara los iconos de las frutas de la tinaja (las que pueden subir a la boca o revelarse), una sola vez: así ningún
        // Show posterior ni ningún paso genera texturas.
        private void Prewarm(Cuia cuia)
        {
            _prewarmed = true;
            for (int k = 0; k < cuia.Count; k++)
            {
                int tint = cuia.ColorAt(k);
                if (tint >= Cuia.MinColor && tint <= Cuia.MaxTintColor)
                {
                    Host.Art.TinajaGlyph(tint);
                }
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

        // El jirau bajo la ranura de la tinaja y la de su cuia (la contigua de la misma fila), con la atadura del medio entre
        // las dos; sin pareja conocida o en otra fila, solo bajo la de la tinaja.
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
            OverlayParts.SetVisible(_jirauTie, show && _partner >= 0 && SameRow(board!, Host.Index, _partner));
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

            float h = JirauHeightU * _u;
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

            RectTransform tie = _jirauTie.rectTransform;
            float mid = pair ? (0.5f * (board.Foot(i).x + board.Foot(_partner).x)) - origin.x : pos.x;
            var tiePos = new Vector2(mid, pos.y);
            var tieSize = new Vector2(6f * h / ProceduralArt.TinajaJirauDesignDp, h);
            if (tie.anchoredPosition != tiePos)
            {
                tie.anchoredPosition = tiePos;
            }

            if (tie.sizeDelta != tieSize)
            {
                tie.sizeDelta = tieSize;
            }
        }

        // Su cuia: la contigua de tipo Cucharon (b.2 regla 1; CucharonMechanic.ValidateInitial), y se recuerda. Si no la hay (la
        // tinaja ya llega con su cuia suelta), la Cuia chica de 2 de al lado, a la derecha antes que a la izquierda [P], hasta
        // ver la de verdad (Marea la vuelve a colgar).
        private void FindPartner(ICuiaBoard board)
        {
            if (_partnerSure)
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
