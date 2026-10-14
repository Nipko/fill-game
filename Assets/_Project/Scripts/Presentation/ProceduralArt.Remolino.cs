#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] del Remolino (02 §9.1 M4; propuesta de D-007 b.2 M4: «espiral en la base de las dos cuias, unidas
    /// por un canal bajo el tablero»; D-003): el agua del canal que une las dos cuias de la pareja (un lecho y un centro con
    /// degradado transversal, y una línea de espuma discontinua), la franja de madera que lo tapa donde pasa por detrás de una
    /// repisa, la espiral de espuma aplanada sobre el remanso, en <see cref="RemolinoSpiralFrames"/> fases para que gire
    /// mientras la pareja recibe, y la ficha que marca igual las dos cuias de la pareja (<see cref="RemolinoPairTag"/>).
    /// Los colores del canal quedan a ΔE00 ≥ 10 de los 14 tintes y lejos del turquesa (direccion-visual-v2 §2.4, regla 1; los
    /// comprueba <c>CuiaOverlayTests</c>).
    /// </summary>
    /// <remarks>
    /// Perezosos, como los de <c>ProceduralArt.Mechanics.cs</c>: un nivel sin Remolino no crea ninguna textura. Blancos, para
    /// que los tiña <c>Image.color</c>. Las fases de la espiral comparten una sola textura (un sprite por fase sobre ella). El
    /// remanso (<see cref="WhirlPool"/>) es el de T03.
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Radio (px) del círculo de las cápsulas del canal; es también su borde 9-slice.</summary>
        public const int RemolinoCapsuleRadiusPx = 12;

        /// <summary>Fases de la espiral (30° cada una): la espiral gira cambiando de fase, sin deformarse.</summary>
        public const int RemolinoSpiralFrames = 12;

        /// <summary>Ancho de la espiral aplanada (u).</summary>
        public const float RemolinoSpiralWidthU = 2.3f;

        /// <summary>Alto de la espiral aplanada (u): la perspectiva del remanso sobre la repisa, dentro de su remanso (0,85 u).</summary>
        public const float RemolinoSpiralHeightU = 0.8f;

        /// <summary>Vueltas de la espiral: pocas y gruesas, para que los brazos no se peguen al aplanarla.</summary>
        public const float RemolinoSpiralTurns = 1.6f;

        /// <summary>
        /// Trazo más fino de la espiral (px de la textura, en el centro): 0,105 u, ≥ 2 dp con cuias de 38 dp o más (u ≥ 19, el
        /// mínimo de la campaña: 16-17 cuias en 3 filas de 6) y 2,5 dp a 48 dp. Crece con u, porque la espiral mide u.
        /// </summary>
        public const float RemolinoSpiralMinStrokePx = 10.5f;

        /// <summary>Trazo más grueso de la espiral (px de la textura, en el brazo exterior): 3,4 dp a 48 dp.</summary>
        public const float RemolinoSpiralMaxStrokePx = 14f;

        /// <summary>El brazo exterior se afila hasta 0 en sus últimos grados: sin un extremo suelto.</summary>
        public const float RemolinoSpiralTaperDeg = 20f;

        /// <summary>
        /// Ángulo (grados, antihorario desde la derecha) donde termina el brazo exterior en la fase 0: arriba, bajo el pie de
        /// la cuia, que lo tapa.
        /// </summary>
        public const float RemolinoSpiralEndDeg = 90f;

        /// <summary>
        /// Agua del canal del Remolino, centro #8FA9AE [P]: gris azulado claro de remanso (L* 67), a ΔE00 13,5 del tinte más
        /// cercano (Agua del Tapajós, que es también el turquesa) y muy claro frente al panel azul petróleo (L* ≈ 17-23).
        /// </summary>
        public static readonly Color RemolinoChannelWater = Palette.Hex(0x8FA9AE);

        /// <summary>Agua del canal en los bordes #5E7F86 [P] (L* 51): ΔE00 19,9 del Agua del Tapajós.</summary>
        public static readonly Color RemolinoChannelEdge = Palette.Hex(0x5E7F86);

        /// <summary>
        /// Radio (px) de las esquinas de la ficha de la pareja (<see cref="RemolinoPairTag"/>); es también su borde 9-slice, así
        /// que se estira a una ficha de dominó de esquinas redondas. En pantalla, las esquinas miden
        /// <c>RemolinoOverlay.PairTagCornerDp</c>.
        /// </summary>
        public const int RemolinoPairTagRadiusPx = 24;

        /// <summary>
        /// Contorno de pizarra de la ficha de la pareja (px de la textura, con el radio de <see cref="RemolinoPairTagRadiusPx"/>):
        /// ≈ 0,9 dp con las esquinas de 3,5 dp, para separar la ficha clara de la espuma de la espiral, que la roza por arriba.
        /// </summary>
        public const float RemolinoPairTagOutlinePx = 6f;

        /// <summary>
        /// Espuma de la ficha de la pareja (su cara), opaca: la espuma marfil de la espiral tal como se ve sobre el remanso
        /// (<see cref="Palette.UiPaper"/> al 0,7 compuesto en lineal sobre <see cref="WhirlPoolColor"/>, lo mismo que
        /// <c>RemolinoOverlay.SpiralSeenColor</c>). Propiedad y no campo: no depende del orden en que se inicializan los
        /// campos estáticos de las parciales.
        /// </summary>
        public static Color RemolinoPairFoam => ColorMath.CompositeLinear(Palette.UiPaper, RemolinoPairFoamAlpha, WhirlPoolColor);

        /// <summary>Opacidad de la espuma con la que se compone <see cref="RemolinoPairFoam"/> (la de la espiral, 0,7).</summary>
        public const float RemolinoPairFoamAlpha = 0.7f;

        /// <summary>
        /// Brillo de marfil del canto de arriba de la ficha de la pareja (su bisel; revisión de la tanda B2, ronda 3: plana, se
        /// leía como una pegatina de interfaz): la espuma un 10 % hacia el blanco, lo más claro que deja la regla de los tintes
        /// (ΔE00 ≈ 10,4 de la Guanábana; más claro, se confundiría con ella).
        /// </summary>
        public static Color RemolinoPairTagLight => Color.Lerp(RemolinoPairFoam, Color.white, 0.1f);

        /// <summary>Sombra del canto de abajo de la ficha de la pareja (su bisel): la espuma al 80 % (ΔE00 ≈ 17,5 del Copoazú).</summary>
        public static Color RemolinoPairTagShade
        {
            get
            {
                Color foam = RemolinoPairFoam;
                return new Color(foam.r * 0.8f, foam.g * 0.8f, foam.b * 0.8f, 1f);
            }
        }

        /// <summary>Ancho (px de la textura, ≈ 1 dp) del brillo del canto de arriba de la ficha, por dentro del contorno.</summary>
        public const float RemolinoPairTagLightPx = 7f;

        /// <summary>Ancho (px de la textura, ≈ 2,2 dp) de la sombra del canto de abajo de la ficha, por dentro del contorno.</summary>
        public const float RemolinoPairTagShadePx = 15f;

        /// <summary>
        /// Contorno del remolino de la superficie de la cuia tocada (<see cref="RemolinoIntakeFrame"/>; px de la textura, a cada
        /// lado del trazo): ≈ 1 dp con el remolino de 24-40 dp de ancho (revisión de T52: la espuma marfil sola apenas se leía
        /// sobre una dosis amarilla o blanca).
        /// </summary>
        public const int RemolinoIntakeRimPx = 7;

        /// <summary>Opacidad del contorno del remolino de la superficie (<c>madera_base</c>, <see cref="Palette.FamilyWood"/>) [P].</summary>
        public const float RemolinoIntakeRimAlpha = 0.45f;

        /// <summary>
        /// Lo que la imagen del remolino de la superficie es más ancha que su espiral, por el margen del contorno (las fases de
        /// <see cref="RemolinoIntakeFrame"/> llevan la misma espiral que <see cref="RemolinoSpiralFrame"/> y el contorno alrededor).
        /// </summary>
        public const float RemolinoIntakeGrowX = (RemolinoSpiralW + (2 * RemolinoIntakeRimPx)) / (float)RemolinoSpiralW;

        /// <summary>Lo mismo a lo alto (ver <see cref="RemolinoIntakeGrowX"/>).</summary>
        public const float RemolinoIntakeGrowY = (RemolinoSpiralH + (2 * RemolinoIntakeRimPx)) / (float)RemolinoSpiralH;

        // Lienzo de una fase de la espiral (px) con 2 px de margen arriba y abajo para que el filtrado no mezcle fases.
        private const int RemolinoSpiralW = (int)(RemolinoSpiralWidthU * MechanicPxPerU);
        private const int RemolinoSpiralH = (int)(RemolinoSpiralHeightU * MechanicPxPerU) + 4;

        // Línea de espuma del canal: 6 dp de trazo y 6 de hueco, 1 dp de grueso, a 4 px/dp.
        private const float RemolinoDashPxPerDp = 4f;

        private Sprite? _remolinoCapsule;
        private Sprite? _remolinoCapsuleSoft;
        private Sprite? _remolinoShelfCover;
        private Sprite? _remolinoDash;
        private Sprite? _remolinoPairTag;
        private Sprite?[]? _remolinoSpiral;
        private Sprite?[]? _remolinoIntake;
        private Sprite? _remolinoWallLight;

        /// <summary>
        /// Filo de luz de la pared de la pareja del Remolino al recibir (revisión de T52, ronda 3) [P]: blanco, con el alfa en
        /// rampa lineal de 1 abajo a 0 arriba y uniforme a lo ancho, para que lo tiña <c>Image.color</c> (marfil neutro) y lo
        /// estire <c>RemolinoOverlay</c> por dentro de cada pared (2 dp de ancho, el 40 % de la cavidad de alto): la luz sube
        /// desde el fondo.
        /// </summary>
        public Sprite RemolinoWallLight => _remolinoWallLight != null ? _remolinoWallLight : (_remolinoWallLight = BuildRemolinoWallLight());

        /// <summary>
        /// Cápsula blanca de borde nítido (el lecho del canal): un círculo de <see cref="RemolinoCapsuleRadiusPx"/> px de radio
        /// con un centro de 2 px y borde 9-slice igual al radio. Estirada (<c>Image.Type.Sliced</c>) a (largo + ancho) × ancho
        /// da un tramo con los dos extremos redondos, así que los tramos que se solapan en un vértice hacen un codo redondo.
        /// Sin estirar (<c>Image.Type.Simple</c>) es una elipse: la boca del canal.
        /// </summary>
        public Sprite RemolinoCapsule => _remolinoCapsule != null ? _remolinoCapsule : (_remolinoCapsule = BuildRemolinoCapsule(false));

        /// <summary>
        /// La misma cápsula con el alfa en degradado desde el eje (1) hasta el borde (0): sobre el lecho, el agua del canal
        /// pasa del centro (<see cref="RemolinoChannelWater"/>) a los bordes (<see cref="RemolinoChannelEdge"/>). Los extremos
        /// redondos son el mismo degradado radial, así que dos tramos que se solapan en un codo no dejan costura.
        /// </summary>
        public Sprite RemolinoCapsuleSoft => _remolinoCapsuleSoft != null ? _remolinoCapsuleSoft : (_remolinoCapsuleSoft = BuildRemolinoCapsule(true));

        /// <summary>
        /// Franja de la madera de la repisa (<see cref="Shelf"/>) para tapar el canal donde pasa por detrás de una repisa: una
        /// columna de 2 dp del centro de su textura, que es uniforme en horizontal, con los 20 dp de madera (sin la sombra). Es
        /// un sprite sobre la misma textura (ninguna textura nueva) y, estirado en horizontal a su ancho y colocado de pie + 3 a
        /// pie − 17, repite los píxeles de la repisa.
        /// </summary>
        public Sprite RemolinoShelfCover => _remolinoShelfCover != null ? _remolinoShelfCover : (_remolinoShelfCover = BuildRemolinoShelfCover());

        /// <summary>
        /// Línea de espuma discontinua del canal para <c>Image.Type.Tiled</c>: 12 × 1 dp con un trazo de 6 dp de puntas
        /// redondas y 6 de hueco, repetible en horizontal.
        /// </summary>
        public Sprite RemolinoDash => _remolinoDash != null ? _remolinoDash : (_remolinoDash = BuildRemolinoDash());

        /// <summary>
        /// Ficha de la pareja del Remolino (opción C del dueño, 2026-10-11) [P]: una ficha de dominó de la espuma de la espiral
        /// (<see cref="RemolinoPairFoam"/>) con un contorno fino de pizarra (<see cref="Slate"/>) y un bisel por dentro (brillo de
        /// marfil arriba, <see cref="RemolinoPairTagLight"/>, y sombra abajo, <see cref="RemolinoPairTagShade"/>), ya coloreada (no se tiñe);
        /// los puntos de la pareja van encima, en el agua del remanso. Se lee como una ficha que se empareja, no como un disco
        /// con aro (en la revisión de la tanda B2, el disco oscuro con aro de espuma y un punto se leía como un botón o un
        /// remache). Borde 9-slice igual al radio de las esquinas (<see cref="RemolinoPairTagRadiusPx"/>): con
        /// <c>Image.Type.Sliced</c> se estira al largo que piden los puntos.
        /// </summary>
        public Sprite RemolinoPairTag => _remolinoPairTag != null ? _remolinoPairTag : (_remolinoPairTag = BuildRemolinoPairTag());

        /// <summary>
        /// Fase <paramref name="frame"/> (módulo <see cref="RemolinoSpiralFrames"/>) de la espiral de espuma aplanada (blanca,
        /// <see cref="RemolinoSpiralWidthU"/> × <see cref="RemolinoSpiralHeightU"/> u): <see cref="RemolinoSpiralTurns"/>
        /// vueltas con el trazo más grueso por fuera y el brazo exterior afilado al final; cada fase gira la espiral 30° en el
        /// sentido de las agujas del reloj.
        /// </summary>
        public Sprite RemolinoSpiralFrame(int frame)
        {
            int f = ((frame % RemolinoSpiralFrames) + RemolinoSpiralFrames) % RemolinoSpiralFrames;
            Sprite?[]? frames = _remolinoSpiral;
            if (frames == null || frames[f] == null)
            {
                frames = BuildRemolinoSpiral();
                _remolinoSpiral = frames;
            }

            return frames[f]!;
        }

        /// <summary>
        /// Fase <paramref name="frame"/> del remolino de la superficie de la cuia tocada que se traga el chorro (revisión de T52)
        /// [P]: la espiral de <see cref="RemolinoSpiralFrame"/> en espuma marfil (<see cref="Palette.UiPaper"/> al 0,7, como en el
        /// remanso) con un contorno de <see cref="RemolinoIntakeRimPx"/> px de <c>madera_base</c> al
        /// <see cref="RemolinoIntakeRimAlpha"/> por fuera del trazo, para que se lea sobre cualquier tinte: el marfil sobre los
        /// oscuros y el contorno sobre los claros (Guanábana, Taperebá). Colores ya puestos: la imagen va en blanco y solo se
        /// funde con su alfa. La imagen es <see cref="RemolinoIntakeGrowX"/> × <see cref="RemolinoIntakeGrowY"/> veces la espiral.
        /// </summary>
        public Sprite RemolinoIntakeFrame(int frame)
        {
            int f = ((frame % RemolinoSpiralFrames) + RemolinoSpiralFrames) % RemolinoSpiralFrames;
            Sprite?[]? frames = _remolinoIntake;
            if (frames == null || frames[f] == null)
            {
                frames = BuildRemolinoIntake();
                _remolinoIntake = frames;
            }

            return frames[f]!;
        }

        private Sprite BuildRemolinoWallLight()
        {
            // 4 × 64 px; la fila 0 es la de abajo (PixelCanvas): alfa 1 abajo y 0 arriba, en rampa lineal por el centro de cada fila.
            const int w = 4;
            const int h = 64;
            var pc = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                float a = 1f - ((y + 0.5f) / h);
                for (int x = 0; x < w; x++)
                {
                    pc.Set(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            return MakeSprite(pc, "remolino_wall_light", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildRemolinoCapsule(bool soft)
        {
            const int r = RemolinoCapsuleRadiusPx;
            const int s = (2 * r) + 2;
            var pc = new PixelCanvas(s, s);
            if (!soft)
            {
                // Cuartos de círculo centrados en las esquinas del centro de 2 px: con el borde 9-slice igual al radio, los
                // cantos estirados quedan rectos y los extremos, redondos.
                pc.FillRoundedRect(0.5f, 0.5f, s - 0.5f, s - 0.5f, r - 0.5f, Color.white);
                return MakeSprite(pc, "remolino_capsule", new Vector2(0.5f, 0.5f), new Vector4(r, r, r, r));
            }

            // Degradado transversal: alfa 1 en el eje (el centro de 2 px) y 0 en el borde, con una curva suave; el borde, con
            // la misma cobertura de antialias que la cápsula nítida.
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float d = PixelCanvas.RoundedRectDistance(px, py, r, r, r + 2f, r + 2f, 0f); // distancia al eje
                    float n = Mathf.Clamp01(d / (r - 0.5f));
                    float a = 1f - (n * n * (3f - (2f * n)));
                    float edge = PixelCanvas.Coverage(PixelCanvas.RoundedRectDistance(px, py, 0.5f, 0.5f, s - 0.5f, s - 0.5f, r - 0.5f));
                    pc.Set(x, y, new Color(1f, 1f, 1f, a * edge));
                }
            }

            return MakeSprite(pc, "remolino_capsule_soft", new Vector2(0.5f, 0.5f), new Vector4(r, r, r, r));
        }

        private Sprite BuildRemolinoShelfCover()
        {
            // BuildShelf: 26 dp de alto a UiPxPerDp, de arriba abajo 20 dp de madera y 6 de sombra; la fila 0 de la textura es
            // la de abajo, así que la madera son las filas desde 6 dp. El centro (fuera de los bordes 9-slice de 16 dp) solo
            // depende de la altura.
            Texture2D tex = Shelf.texture;
            const float k = UiPxPerDp;
            float wood = 20f * k;
            float x = Mathf.Round((0.5f * tex.width) - k);
            var rect = new Rect(x, tex.height - wood, 2f * k, wood);
            Sprite sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f * k, 0, SpriteMeshType.FullRect, Vector4.zero);
            sprite.name = "remolino_shelf_cover";
            sprite.hideFlags = HideFlags.DontSave;
            _owned.Add(sprite);
            return sprite;
        }

        private Sprite BuildRemolinoPairTag()
        {
            // Dos rectángulos redondeados de borde nítido (el 9-slice los estira): contorno y cara. Las esquinas centradas en el
            // centro de 2 px, como la cápsula del canal, para que los cantos estirados queden rectos.
            const int r = RemolinoPairTagRadiusPx;
            const int s = (2 * r) + 2;
            const float o = RemolinoPairTagOutlinePx;
            var pc = new PixelCanvas(s, s);
            pc.FillRoundedRect(0.5f, 0.5f, s - 0.5f, s - 0.5f, r - 0.5f, Slate);
            pc.FillRoundedRect(0.5f + o, 0.5f + o, s - 0.5f - o, s - 0.5f - o, r - 0.5f - o, RemolinoPairFoam);

            // Bisel (revisión de la tanda B2, ronda 3: plana, junto al vidrio, la fibra y la madera con brillo, se leía como una
            // pegatina de interfaz): por dentro del contorno, un brillo de marfil a lo largo del canto de arriba y una sombra
            // suave a lo largo del de abajo, que siguen las esquinas redondas; así se lee como una ficha de dominó de hueso con
            // volumen, con la luz del juego desde arriba. Van en los bordes 9-slice (fijos en alto) y el centro, que se estira,
            // queda liso.
            float c = 0.5f * s;
            float half = c - 0.5f - o; // medio alto de la cara
            Color light = RemolinoPairTagLight;
            Color shade = RemolinoPairTagShade;
            for (int y = 0; y < s; y++)
            {
                float yc = y + 0.5f;
                float up = Mathf.Clamp01(2f * (yc - c) / half);
                float down = Mathf.Clamp01(2f * (c - yc) / half);
                if (up <= 0f && down <= 0f)
                {
                    continue;
                }

                for (int x = 0; x < s; x++)
                {
                    float d = PixelCanvas.RoundedRectDistance(x + 0.5f, yc, 0.5f + o, 0.5f + o, s - 0.5f - o, s - 0.5f - o, r - 0.5f - o);
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float inner = Mathf.Max(0f, -d);
                    if (up > 0f)
                    {
                        pc.Blend(x, y, light, cov * up * (1f - Mathf.SmoothStep(0f, 1f, inner / RemolinoPairTagLightPx)));
                    }

                    if (down > 0f)
                    {
                        pc.Blend(x, y, shade, cov * down * (1f - Mathf.SmoothStep(0f, 1f, inner / RemolinoPairTagShadePx)));
                    }
                }
            }

            return MakeSprite(pc, "remolino_pair_tag", new Vector2(0.5f, 0.5f), new Vector4(r, r, r, r));
        }

        private Sprite BuildRemolinoDash()
        {
            const float k = RemolinoDashPxPerDp;
            int w = Mathf.RoundToInt(12f * k);
            int h = Mathf.RoundToInt(1f * k);
            var pc = new PixelCanvas(w, h);
            float r = 0.5f * h;
            pc.FillCapsule(r, r, (6f * k) - r, r, r, Color.white);
            return MakeSprite(pc, "remolino_dash", new Vector2(0.5f, 0.5f), Vector4.zero, k, true);
        }

        // Las fases en una sola textura (una fila por fase) y un sprite por fase.
        private Sprite?[] BuildRemolinoSpiral()
        {
            const int w = RemolinoSpiralW;
            const int h = RemolinoSpiralH;
            float maxR = 0.5f * RemolinoSpiralMaxStrokePx;
            float rx = (w * 0.5f) - maxR - 2f;
            float ry = (h * 0.5f) - maxR - 2f;
            float turns = RemolinoSpiralTurns;
            float startDeg = RemolinoSpiralEndDeg - (turns * 360f); // el brazo exterior acaba arriba en la fase 0
            var pc = new PixelCanvas(w, h * RemolinoSpiralFrames);
            for (int f = 0; f < RemolinoSpiralFrames; f++)
            {
                float phase = (startDeg - (f * (360f / RemolinoSpiralFrames))) * Mathf.Deg2Rad;
                DrawRemolinoSpiral(pc, w * 0.5f, (f * h) + (h * 0.5f), rx, ry, turns, phase);
            }

            Texture2D tex = pc.ToTexture("remolino_spiral");
            _owned.Add(tex);
            var frames = new Sprite?[RemolinoSpiralFrames];
            for (int f = 0; f < RemolinoSpiralFrames; f++)
            {
                Sprite sprite = Sprite.Create(tex, new Rect(0f, f * h, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
                sprite.name = "remolino_spiral_" + f;
                sprite.hideFlags = HideFlags.DontSave;
                _owned.Add(sprite);
                frames[f] = sprite;
            }

            return frames;
        }

        // Las fases del remolino de la superficie (RemolinoIntakeFrame) en una textura: la misma espiral que BuildRemolinoSpiral
        // (mismos rx, ry y fases) en un lienzo con RemolinoIntakeRimPx más a cada lado; el trazo, en espuma marfil, y el mismo
        // trazo engordado RemolinoIntakeRimPx, en madera_base, solo donde no llega el trazo (el contorno no oscurece la espuma).
        private Sprite?[] BuildRemolinoIntake()
        {
            const int rim = RemolinoIntakeRimPx;
            const int w = RemolinoSpiralW + (2 * rim);
            const int h = RemolinoSpiralH + (2 * rim);
            float maxR = 0.5f * RemolinoSpiralMaxStrokePx;
            float rx = (RemolinoSpiralW * 0.5f) - maxR - 2f;
            float ry = (RemolinoSpiralH * 0.5f) - maxR - 2f;
            float turns = RemolinoSpiralTurns;
            float startDeg = RemolinoSpiralEndDeg - (turns * 360f);
            var stroke = new PixelCanvas(w, h * RemolinoSpiralFrames);
            var wide = new PixelCanvas(w, h * RemolinoSpiralFrames);
            for (int f = 0; f < RemolinoSpiralFrames; f++)
            {
                float phase = (startDeg - (f * (360f / RemolinoSpiralFrames))) * Mathf.Deg2Rad;
                float cy = (f * h) + (h * 0.5f);
                DrawRemolinoSpiral(stroke, w * 0.5f, cy, rx, ry, turns, phase);
                DrawRemolinoSpiral(wide, w * 0.5f, cy, rx, ry, turns, phase, rim);
            }

            Color foam = Palette.UiPaper;
            Color wood = Palette.FamilyWood.Base;
            for (int y = 0; y < wide.Height; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float a = stroke.Get(x, y).a;
                    float d = wide.Get(x, y).a;
                    float fa = FoamAlphaOfSpiral * a;
                    float ra = RemolinoIntakeRimAlpha * Mathf.Max(0f, d - a) * (1f - fa);
                    float alpha = fa + ra;
                    if (alpha <= 0f)
                    {
                        wide.Set(x, y, Color.clear);
                        continue;
                    }

                    Color c = ((foam * fa) + (wood * ra)) / alpha;
                    c.a = alpha;
                    wide.Set(x, y, c);
                }
            }

            Texture2D tex = wide.ToTexture("remolino_intake");
            _owned.Add(tex);
            var frames = new Sprite?[RemolinoSpiralFrames];
            for (int f = 0; f < RemolinoSpiralFrames; f++)
            {
                Sprite sprite = Sprite.Create(tex, new Rect(0f, f * h, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
                sprite.name = "remolino_intake_" + f;
                sprite.hideFlags = HideFlags.DontSave;
                _owned.Add(sprite);
                frames[f] = sprite;
            }

            return frames;
        }

        // Espuma de la espiral sobre el remanso (la de RemolinoOverlay.FoamAlpha y RemolinoPairFoamAlpha).
        private const float FoamAlphaOfSpiral = RemolinoPairFoamAlpha;

        // Espiral de Arquímedes aplanada (rx × ry) de turns vueltas desde el centro, girada phase radianes, con el trazo de
        // RemolinoSpiralMinStrokePx en el centro a RemolinoSpiralMaxStrokePx por fuera (más extraPx a cada lado) y afilado a 0 en
        // los últimos RemolinoSpiralTaperDeg grados.
        private static void DrawRemolinoSpiral(PixelCanvas pc, float cx, float cy, float rx, float ry, float turns, float phase, float extraPx = 0f)
        {
            int steps = Mathf.CeilToInt(turns * 120f);
            float total = turns * 360f;
            Vector2 prev = new Vector2(cx + (Mathf.Cos(phase) * 0.06f * rx), cy + (Mathf.Sin(phase) * 0.06f * ry));
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angle = phase + (t * turns * Mathf.PI * 2f);
                float r = 0.06f + (0.94f * t);
                var p = new Vector2(cx + (Mathf.Cos(angle) * r * rx), cy + (Mathf.Sin(angle) * r * ry));
                float taper = Mathf.Clamp01((total * (1f - t)) / RemolinoSpiralTaperDeg);
                float radius = (0.5f * Mathf.Lerp(RemolinoSpiralMinStrokePx, RemolinoSpiralMaxStrokePx, t) * Mathf.Sqrt(taper)) + extraPx;
                if (radius >= 0.6f)
                {
                    pc.FillCapsule(prev.x, prev.y, p.x, p.y, radius, Color.white);
                }

                prev = p;
            }
        }
    }
}
