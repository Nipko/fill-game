#nullable enable
using AF.Core.Mechanics;
using AF.Presentation.Mechanics;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] del estado de las cuias (D-003; direccion-visual-v2 §2.4): el «?» de Aguas turbias (07 §4.3), los
    /// grumos del Barro (02 §9.1 M5), las piezas de los overlays de cada mecánica (02 §9.1; propuesta de D-007 b.2) y los
    /// iconos de 32 dp que reutilizarán la tarjeta previa y el HUD (<see cref="MechanicIcon"/>, <see cref="MurkyIcon"/>,
    /// <see cref="MudIcon"/>, <see cref="CounterPip"/> y <see cref="DropIcon"/>). Todo se genera de forma perezosa la primera
    /// vez que se pide (como <see cref="RiverSwell"/>), así que un nivel sin mecánicas no crea ninguna textura. Tras
    /// <see cref="Dispose"/> los sprites destruidos se vuelven a generar (el == de Unity). Reutiliza
    /// <c>DrawLeafShape</c> y <c>DrawRopeSegment</c> de la escena; pocos sprites por familia, porque cada uno crea una textura.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Densidad de los sprites que van dentro de la cuia o sobre ella (px por u): la del vidrio a u = 32 dp en el SM-S938B.</summary>
        public const int MechanicPxPerU = 96;

        /// <summary>Lado de los iconos de mecánica, del «?» y del Barro (dp; 32 dp como pide la tarjeta y el HUD).</summary>
        public const float MechanicIconDp = 32f;

        /// <summary>Grumos del Barro: pardo oscuro #3F3826 [P].</summary>
        public static readonly Color MudLumpColor = Palette.Hex(0x3F3826);

        // Grumos del Barro: (x, y) relativos a la banda y radios en px de la banda de 96 px.
        private static readonly float[] MudLumpLayout =
        {
            0.16f, 0.34f, 9f, 6f,
            0.34f, 0.66f, 11f, 7f,
            0.52f, 0.30f, 8f, 5.5f,
            0.68f, 0.62f, 10f, 6.5f,
            0.85f, 0.38f, 7.5f, 5f,
            0.24f, 0.52f, 5f, 3.5f,
            0.78f, 0.24f, 5f, 3.5f,
        };

        private Sprite? _questionMark;
        private Sprite? _mudLumps;
        private Sprite? _murkyIcon;
        private Sprite? _mudIcon;

        /// <summary>
        /// «?» marfil (<see cref="Palette.UiPaper"/>) con un halo de tinta para leerse sobre el pardo de las turbias (07 §4.3;
        /// glosario §6.2). Sprite cuadrado de 0,5 u: el glifo ocupa unos 0,45 u de alto.
        /// </summary>
        public Sprite QuestionMark
        {
            get
            {
                if (_questionMark == null)
                {
                    const int s = MechanicPxPerU / 2;
                    var pc = new PixelCanvas(s, s);
                    DrawQuestionGlyph(pc, s * 0.5f, 0f, s / 48f, ColorMath.WithAlpha(Palette.Ink, 0.55f), 1.6f);
                    DrawQuestionGlyph(pc, s * 0.5f, 0f, s / 48f, Palette.UiPaper, 0f);
                    _questionMark = MakeSprite(pc, "mech_question", new Vector2(0.5f, 0.5f), Vector4.zero);
                }

                return _questionMark;
            }
        }

        /// <summary>
        /// Grumos más oscuros del Barro sobre una banda de dosis (1,6 u × 1 u, el ancho de la cavidad): manchas
        /// <see cref="MudLumpColor"/> y motas claras, sin tocar los bordes de la banda.
        /// </summary>
        public Sprite MudLumps
        {
            get
            {
                if (_mudLumps == null)
                {
                    int w = Mathf.RoundToInt(2f * CuiaGeometry.CavityHalfWidthU * MechanicPxPerU);
                    int h = MechanicPxPerU;
                    var pc = new PixelCanvas(w, h);
                    DrawMudLumps(pc, 0f, 0f, w, h, 1f);
                    _mudLumps = MakeSprite(pc, "mech_mud_lumps", new Vector2(0.5f, 0.5f), Vector4.zero);
                }

                return _mudLumps;
            }
        }

        /// <summary>Icono de Aguas turbias (32 dp): gota redonda parda #8A7B6A con el «?» marfil (chip del HUD y tarjeta, 07 §7.7).</summary>
        public Sprite MurkyIcon
        {
            get
            {
                if (_murkyIcon == null)
                {
                    int s = IconPx;
                    var pc = new PixelCanvas(s, s);
                    float c = s * 0.5f;
                    pc.FillCircle(c, c, s * 0.46f, ColorMath.Darken(Palette.Murky, 0.35f));
                    pc.FillCircle(c, c, s * 0.42f, Palette.Murky);
                    float k = s / 64f;
                    DrawQuestionGlyph(pc, c, (s - (41f * k)) * 0.5f - (2.9f * k), k, ColorMath.WithAlpha(Palette.Ink, 0.5f), 1.2f * k);
                    DrawQuestionGlyph(pc, c, (s - (41f * k)) * 0.5f - (2.9f * k), k, Palette.UiPaper, 0f);
                    _murkyIcon = MakeSprite(pc, "mech_icon_murky", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
                }

                return _murkyIcon;
            }
        }

        /// <summary>Icono del Barro (32 dp): una mancha de barro de várzea con sus grumos.</summary>
        public Sprite MudIcon
        {
            get
            {
                if (_mudIcon == null)
                {
                    int s = IconPx;
                    var pc = new PixelCanvas(s, s);
                    float c = s * 0.5f;
                    pc.FillEllipseSoft(c, c * 0.92f, s * 0.44f, s * 0.32f, 1.2f, ColorMath.Darken(Palette.Mud, 0.35f));
                    pc.FillEllipseSoft(c, c * 0.95f, s * 0.41f, s * 0.29f, 1.2f, Palette.Mud);
                    DrawMudLumps(pc, s * 0.14f, s * 0.2f, s * 0.72f, s * 0.5f, s / (float)MechanicPxPerU);
                    _mudIcon = MakeSprite(pc, "mech_icon_mud", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
                }

                return _mudIcon;
            }
        }

        /// <summary>Lado en píxeles de un icono de <see cref="MechanicIconDp"/> a la densidad de UI.</summary>
        private static int IconPx => Mathf.RoundToInt(MechanicIconDp * UiPxPerDp);

        // «?» en una caja de 48 × 48 px a escala k, con el eje en cx y la caja empezando en y0. grow engorda los trazos (el
        // halo se dibuja antes, más grueso).
        private static void DrawQuestionGlyph(PixelCanvas pc, float cx, float y0, float k, Color c, float grow)
        {
            float half = (3f * k) + grow;
            float cy = y0 + (31f * k);
            float r = 10f * k;
            pc.StrokeArc(cx, cy, r, 2f * half, -45f, 195f, c);
            float ex = cx + (0.7071f * r);
            float ey = cy - (0.7071f * r);
            pc.FillCapsule(ex, ey, cx, y0 + (18.5f * k), half, c);
            pc.FillCapsule(cx, y0 + (18.5f * k), cx, y0 + (15f * k), half, c);
            pc.FillCircle(cx, y0 + (6.5f * k), (3.6f * k) + grow, c);
        }

        // Grumos del Barro en el rectángulo [x0, x0 + w] × [y0, y0 + h] (px), deterministas. k escala los radios (1 = banda
        // de 96 px de alto).
        private static void DrawMudLumps(PixelCanvas pc, float x0, float y0, float w, float h, float k)
        {
            float[] lumps = MudLumpLayout;
            for (int i = 0; i < lumps.Length; i += 4)
            {
                float cx = x0 + (lumps[i] * w);
                float cy = y0 + (lumps[i + 1] * h);
                pc.FillEllipseSoft(cx, cy, lumps[i + 2] * k, lumps[i + 3] * k, 1.5f * k, ColorMath.WithAlpha(MudLumpColor, 0.9f));
                pc.FillCircle(cx - (lumps[i + 2] * 0.35f * k), cy + (lumps[i + 3] * 0.35f * k), 1.3f * k, ColorMath.WithAlpha(ColorMath.Lighten(Palette.Mud, 0.3f), 0.8f));
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        // Overlays de las mecánicas (02 §9.1; propuesta de D-007 b.2) e iconos reutilizables. Colores [P] a ΔE00 ≥ 10 de los
        // 14 tintes (direccion-visual-v2 §2.4, regla 1): verdes de hoja oscuros, grises perla y pizarra, piedra y fibra.
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>Gris piedra #7F8C8D [P] (Piraña): ΔE00 ≥ 20 frente a los tintes.</summary>
        public static readonly Color StoneLight = Palette.Hex(0x7F8C8D);

        /// <summary>Gris piedra oscuro #6F7378 [P].</summary>
        public static readonly Color StoneDark = Palette.Hex(0x6F7378);

        /// <summary>Costado plateado de la piraña #8FA1A8 [P].</summary>
        public static readonly Color FishSide = Palette.Hex(0x8FA1A8);

        /// <summary>Lomo de la piraña #5E6670 [P].</summary>
        public static readonly Color FishBack = Palette.Hex(0x5E6670);

        /// <summary>Vientre de la piraña #A7B5B5 [P] (sin rojo: nada de sangre, b.2 M7).</summary>
        public static readonly Color FishBelly = Palette.Hex(0xA7B5B5);

        private Sprite?[]? _mechanicIcons;
        private Sprite? _counterPip;
        private Sprite? _counterPipEmpty;
        private Sprite? _dropIcon;
        private Sprite? _hojaLeaf;
        private Sprite? _broteBud;
        private Sprite? _broteCalyx;
        private Sprite? _lianaWrap;
        private Sprite? _lianaKnot;
        private Sprite? _matapiRod;
        private Sprite? _matapiHoop;
        private Sprite? _matapiFunnel;
        private Sprite? _whirlPool;
        private Sprite? _whirlSpiral;
        private Sprite? _rainCloud;
        private Sprite? _piranaStones;
        private Sprite? _piranha;

        /// <summary>Punto lleno de un contador (blanco; lo tiñe <c>Image.color</c>).</summary>
        public Sprite CounterPip => _counterPip != null ? _counterPip : (_counterPip = BuildPip(false));

        /// <summary>Punto vacío de un contador: un aro (blanco; lo tiñe <c>Image.color</c>).</summary>
        public Sprite CounterPipEmpty => _counterPipEmpty != null ? _counterPipEmpty : (_counterPipEmpty = BuildPip(true));

        /// <summary>Gotita (blanca con brillo; la tiñe <c>Image.color</c> con el agua de la Lluvia).</summary>
        public Sprite DropIcon => _dropIcon != null ? _dropIcon : (_dropIcon = BuildDropIcon());

        /// <summary>
        /// Icono de 32 dp de la mecánica <paramref name="type"/> (tarjeta previa y HUD): hoja de palma, capullo, liana, matapi,
        /// remolino, nube o piraña, sin tintes de fruta (07 §3, regla 6). Null para <see cref="MechanicType.None"/>.
        /// </summary>
        public Sprite? MechanicIcon(MechanicType type)
        {
            int index = (int)type;
            if (index <= 0 || index > (int)MechanicTypes.Last)
            {
                return null;
            }

            _mechanicIcons ??= new Sprite?[(int)MechanicTypes.Last + 1];
            Sprite? icon = _mechanicIcons[index];
            if (icon == null)
            {
                icon = BuildMechanicIcon(type);
                _mechanicIcons[index] = icon;
            }

            return icon;
        }

        /// <summary>Hoja de palma alargada y plegada (2,3 × 1 u), verde oscuro, para tapar la boca de la Hoja (b.2 M1).</summary>
        internal Sprite HojaLeaf => _hojaLeaf != null ? _hojaLeaf : (_hojaLeaf = BuildHojaLeaf());

        /// <summary>Capullo de dos hojitas (1 × 0,9 u) en grises: lo tiñe el color de la cima del Brote (02 §9.1 M2).</summary>
        internal Sprite BroteBud => _broteBud != null ? _broteBud : (_broteBud = BuildBroteBud());

        /// <summary>Cáliz verde del capullo (0,8 × 0,3 u).</summary>
        internal Sprite BroteCalyx => _broteCalyx != null ? _broteCalyx : (_broteCalyx = BuildBroteCalyx());

        /// <summary>Una vuelta de liana en diagonal (2,2 × 1 u) con dos hojitas (b.2 M3).</summary>
        internal Sprite LianaWrap => _lianaWrap != null ? _lianaWrap : (_lianaWrap = BuildLianaWrap());

        /// <summary>Nudo de la liana (0,5 × 0,42 u).</summary>
        internal Sprite LianaKnot => _lianaKnot != null ? _lianaKnot : (_lianaKnot = BuildLianaKnot());

        /// <summary>Varilla de fibra (blanca con sombreado horizontal; se estira y se tiñe de <c>fibra_*</c>).</summary>
        internal Sprite MatapiRod => _matapiRod != null ? _matapiRod : (_matapiRod = BuildMatapiRod());

        /// <summary>Aro delantero de la trampa (media elipse, 2,1 × 0,35 u; blanco, se tiñe de fibra).</summary>
        internal Sprite MatapiHoop => _matapiHoop != null ? _matapiHoop : (_matapiHoop = BuildMatapiHoop());

        /// <summary>Embudo de fibra hacia dentro en la boca del Matapi (1,4 × 0,65 u; b.2 M10).</summary>
        internal Sprite MatapiFunnel => _matapiFunnel != null ? _matapiFunnel : (_matapiFunnel = BuildMatapiFunnel());

        /// <summary>Remanso bajo el pie del Remolino: elipse de borde suave (blanca; se tiñe de oscuro).</summary>
        internal Sprite WhirlPool => _whirlPool != null ? _whirlPool : (_whirlPool = BuildWhirlPool());

        /// <summary>Espiral de espuma aplanada (blanca; se tiñe de marfil translúcido).</summary>
        internal Sprite WhirlSpiral => _whirlSpiral != null ? _whirlSpiral : (_whirlSpiral = BuildWhirlSpiral());

        /// <summary>Nube con la panza en sombra (blanca y gris; se tiñe de gris perla).</summary>
        internal Sprite RainCloud => _rainCloud != null ? _rainCloud : (_rainCloud = BuildRainCloud());

        /// <summary>Arco de piedras grises sobre la boca de la cueva de la Piraña (1,7 × 0,75 u).</summary>
        internal Sprite PiranaStones => _piranaStones != null ? _piranaStones : (_piranaStones = BuildPiranaStones());

        /// <summary>Piraña pequeña de perfil mirando a la izquierda (0,9 × 0,55 u), sin dientes exagerados (b.2 M7).</summary>
        internal Sprite Piranha => _piranha != null ? _piranha : (_piranha = BuildPiranha());

        private Sprite BuildPip(bool empty)
        {
            const int s = 32;
            var pc = new PixelCanvas(s, s);
            if (empty)
            {
                pc.StrokeArc(s * 0.5f, s * 0.5f, 12.5f, 5f, 0f, 360f, Color.white);
            }
            else
            {
                pc.FillCircle(s * 0.5f, s * 0.5f, 15f, Color.white);
            }

            return MakeSprite(pc, empty ? "mech_pip_empty" : "mech_pip", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildDropIcon()
        {
            const int w = 32;
            const int h = 40;
            var pc = new PixelCanvas(w, h);
            DrawDrop(pc, w * 0.5f, 13f, 11.5f, 25f, Color.white);
            pc.FillCircle((w * 0.5f) - 4f, 15f, 3f, ColorMath.WithAlpha(Palette.Glint, 0.9f));
            return MakeSprite(pc, "mech_drop", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildHojaLeaf()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(2.3f * MechanicPxPerU);
            int h = MechanicPxPerU;
            var pc = new PixelCanvas(w, h);
            DrawPalmLeaf(pc, new Vector2(6f * k, 22f * k), new Vector2(w * 0.5f, 104f * k), new Vector2(w - (6f * k), 22f * k), 30f * k, Palette.JungleMid, Palette.LeafDark, Palette.Vein, k);
            return MakeSprite(pc, "mech_hoja_leaf", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildBroteBud()
        {
            int w = MechanicPxPerU;
            int h = Mathf.RoundToInt(0.9f * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            DrawBud(pc, w * 0.5f, 6f, h - 10f, w * 0.30f, new Color(1f, 1f, 1f, 1f), new Color(0.82f, 0.82f, 0.82f, 1f), new Color(0.28f, 0.28f, 0.28f, 1f));
            return MakeSprite(pc, "mech_brote_bud", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildBroteCalyx()
        {
            int w = Mathf.RoundToInt(0.8f * MechanicPxPerU);
            int h = Mathf.RoundToInt(0.3f * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            DrawCalyx(pc, w * 0.5f, 2f, w * 0.42f, h - 4f, Palette.LeafDark);
            return MakeSprite(pc, "mech_brote_calyx", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildLianaWrap()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(2.2f * MechanicPxPerU);
            int h = MechanicPxPerU;
            var pc = new PixelCanvas(w, h);
            DrawVine(pc, new Vector2(4f * k, 16f * k), new Vector2(w * 0.5f, h * 0.5f), new Vector2(w - (4f * k), h - (16f * k)), 7f * k, k, true);
            return MakeSprite(pc, "mech_liana_wrap", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildLianaKnot()
        {
            int w = Mathf.RoundToInt(0.5f * MechanicPxPerU);
            int h = Mathf.RoundToInt(0.42f * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            pc.FillEllipseSoft(cx - (w * 0.16f), cy, w * 0.26f, h * 0.30f, 1.2f, Palette.Vein);
            pc.FillEllipseSoft(cx + (w * 0.16f), cy, w * 0.26f, h * 0.30f, 1.2f, Palette.Vein);
            pc.FillEllipseSoft(cx - (w * 0.16f), cy, w * 0.21f, h * 0.24f, 1.2f, Palette.LeafDark);
            pc.FillEllipseSoft(cx + (w * 0.16f), cy, w * 0.21f, h * 0.24f, 1.2f, Palette.LeafDark);
            pc.FillEllipseSoft(cx, cy, w * 0.20f, h * 0.34f, 1.2f, Palette.LeafMid);
            return MakeSprite(pc, "mech_liana_knot", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildMatapiRod()
        {
            const int w = 12;
            const int h = 8;
            var pc = new PixelCanvas(w, h);
            for (int x = 0; x < w; x++)
            {
                float t = (x + 0.5f) / w;
                float shade = t < 0.35f ? 1f : Mathf.Lerp(1f, 0.62f, (t - 0.35f) / 0.65f);
                for (int y = 0; y < h; y++)
                {
                    pc.Set(x, y, new Color(shade, shade, shade, 1f));
                }
            }

            return MakeSprite(pc, "mech_matapi_rod", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildMatapiHoop()
        {
            int w = Mathf.RoundToInt(2.1f * MechanicPxPerU);
            int h = Mathf.RoundToInt(0.35f * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            StrokeEllipse(pc, w * 0.5f, h * 0.62f, (w * 0.5f) - 4f, (h * 0.62f) - 4f, 6f, Color.white, true);
            return MakeSprite(pc, "mech_matapi_hoop", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildMatapiFunnel()
        {
            int w = Mathf.RoundToInt(1.4f * MechanicPxPerU);
            int h = Mathf.RoundToInt(0.65f * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            DrawFunnel(pc, 0f, 0f, w, h, MechanicPxPerU / 96f);
            return MakeSprite(pc, "mech_matapi_funnel", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildWhirlPool()
        {
            int w = Mathf.RoundToInt(2.4f * MechanicPxPerU);
            int h = Mathf.RoundToInt(0.7f * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            pc.FillEllipseSoft(w * 0.5f, h * 0.5f, (w * 0.5f) - 10f, (h * 0.5f) - 6f, 6f, Color.white);
            return MakeSprite(pc, "mech_whirl_pool", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildWhirlSpiral()
        {
            int w = Mathf.RoundToInt(2.1f * MechanicPxPerU);
            int h = Mathf.RoundToInt(0.55f * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            DrawSpiral(pc, w * 0.5f, h * 0.5f, (w * 0.5f) - 8f, (h * 0.5f) - 6f, 2.2f, 14f, Color.white);
            return MakeSprite(pc, "mech_whirl_spiral", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildRainCloud()
        {
            int w = Mathf.RoundToInt(2.35f * MechanicPxPerU);
            int h = MechanicPxPerU;
            var pc = new PixelCanvas(w, h);
            DrawCloud(pc, 0f, 0f, w, h, Color.white, 0.84f);
            return MakeSprite(pc, "mech_rain_cloud", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildPiranaStones()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(1.7f * MechanicPxPerU);
            int h = Mathf.RoundToInt(0.75f * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            DrawStoneArch(pc, w * 0.5f, 18f * k, (w * 0.5f) - (19f * k), h - (36f * k), 14f * k, k);
            return MakeSprite(pc, "mech_pirana_stones", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildPiranha()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(0.9f * MechanicPxPerU);
            int h = Mathf.RoundToInt(0.55f * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            DrawPiranha(pc, w * 0.5f, h * 0.5f, w - (6f * k), k);
            return MakeSprite(pc, "mech_piranha", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildMechanicIcon(MechanicType type)
        {
            int s = IconPx;
            float k = s / 96f;
            var pc = new PixelCanvas(s, s);
            switch (type)
            {
                case MechanicType.Hoja:
                    DrawPalmLeaf(pc, new Vector2(8f * k, 30f * k), new Vector2(46f * k, 96f * k), new Vector2(88f * k, 42f * k), 20f * k, Palette.JungleMid, Palette.LeafDark, Palette.Vein, 0.8f * k);
                    break;
                case MechanicType.Brote:
                    DrawBud(pc, 48f * k, 18f * k, 70f * k, 24f * k, Palette.LeafMid, Palette.LeafDark, Palette.Vein);
                    DrawCalyx(pc, 48f * k, 8f * k, 24f * k, 20f * k, Palette.LeafDark);
                    break;
                case MechanicType.Liana:
                    DrawVine(pc, new Vector2(8f * k, 14f * k), new Vector2(60f * k, 30f * k), new Vector2(88f * k, 84f * k), 6f * k, 0.8f * k, true);
                    pc.FillCircle(70f * k, 46f * k, 9f * k, Palette.Vein);
                    pc.FillCircle(70f * k, 46f * k, 7f * k, Palette.LeafDark);
                    break;
                case MechanicType.Matapi:
                    for (int i = 0; i < 5; i++)
                    {
                        float x = (24f + (i * 12f)) * k;
                        Color c = (i % 3) == 0 ? Palette.FiberLight : ((i % 3) == 1 ? Palette.FiberBase : Palette.FiberShadow);
                        pc.FillCapsule(x, 10f * k, x, 64f * k, 3.2f * k, c);
                    }

                    StrokeEllipse(pc, 48f * k, 18f * k, 30f * k, 8f * k, 5f * k, Palette.FiberBase, true);
                    StrokeEllipse(pc, 48f * k, 52f * k, 30f * k, 8f * k, 5f * k, Palette.FiberBase, true);
                    DrawFunnel(pc, 14f * k, 58f * k, 68f * k, 34f * k, 0.7f * k);
                    break;
                case MechanicType.Remolino:
                    pc.FillEllipseSoft(48f * k, 48f * k, 44f * k, 30f * k, 2f * k, RemolinoOverlay.PoolColor);
                    DrawSpiral(pc, 48f * k, 48f * k, 38f * k, 24f * k, 2.2f, 7f * k, ColorMath.WithAlpha(Palette.UiPaper, 0.6f));
                    break;
                case MechanicType.Lluvia:
                    DrawCloud(pc, 4f * k, 40f * k, 88f * k, 50f * k, OverlayParts.Pearl, 0.84f);
                    for (int i = 0; i < 3; i++)
                    {
                        DrawDrop(pc, (30f + (i * 18f)) * k, 18f * k - ((i % 2) * 6f * k), 6f * k, 13f * k, FishSide);
                    }

                    break;
                case MechanicType.Pirana:
                    DrawPiranha(pc, 48f * k, 48f * k, 88f * k, k);
                    break;
            }

            return MakeSprite(pc, "mech_icon_" + MechanicTypes.ToKey(type), new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private static Vector2 Quad(Vector2 a, Vector2 c, Vector2 b, float t)
        {
            float s = 1f - t;
            return (s * s * a) + (2f * s * t * c) + (t * t * b);
        }

        // Hoja de palma alargada sobre la curva cuadrática a → c → b (px): ancha en el centro y fina en las puntas, con la mitad
        // de abajo en sombra (la hoja plegada sobre la boca), pliegues oblicuos y nervio central.
        private static void DrawPalmLeaf(PixelCanvas pc, Vector2 a, Vector2 c, Vector2 b, float halfWidth, Color face, Color fold, Color vein, float k)
        {
            const int steps = 72;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Vector2 p = Quad(a, c, b, t);
                    float w = halfWidth * Mathf.Pow(Mathf.Sin(Mathf.PI * t), 0.75f);
                    if (pass == 0)
                    {
                        pc.FillCircle(p.x, p.y - (w * 0.22f), w, fold);
                    }
                    else
                    {
                        pc.FillCircle(p.x, p.y + (w * 0.18f), w * 0.72f, face);
                    }
                }
            }

            Color crease = ColorMath.WithAlpha(vein, 0.55f);
            for (int i = 1; i < 12; i++)
            {
                float t = i / 12f;
                Vector2 p = Quad(a, c, b, t);
                Vector2 tangent = (Quad(a, c, b, Mathf.Min(1f, t + 0.01f)) - Quad(a, c, b, Mathf.Max(0f, t - 0.01f))).normalized;
                var normal = new Vector2(-tangent.y, tangent.x);
                float w = halfWidth * Mathf.Pow(Mathf.Sin(Mathf.PI * t), 0.75f);
                Vector2 up = p + (normal * w * 0.8f) + (tangent * w * 0.35f);
                Vector2 down = p - (normal * w * 0.9f) + (tangent * w * 0.35f);
                pc.FillCapsule(p.x, p.y, up.x, up.y, 0.7f * k, crease);
                pc.FillCapsule(p.x, p.y, down.x, down.y, 0.7f * k, crease);
            }

            Vector2 prev = a;
            for (int i = 1; i <= 24; i++)
            {
                Vector2 p = Quad(a, c, b, i / 24f);
                pc.FillCapsule(prev.x, prev.y, p.x, p.y, 1.2f * k, vein);
                prev = p;
            }
        }

        // Capullo de dos hojitas que se cierran en la punta: base en (cx, y0), alto len, ancho de cada hojita width.
        private static void DrawBud(PixelCanvas pc, float cx, float y0, float len, float width, Color left, Color right, Color outline)
        {
            var dirL = new Vector2(-0.16f, 1f).normalized;
            var dirR = new Vector2(0.16f, 1f).normalized;
            DrawLeafShape(pc, new Vector2(cx + (width * 0.18f), y0 - 1.5f), dirL, len + 3f, width + 3f, outline, 1f);
            DrawLeafShape(pc, new Vector2(cx - (width * 0.18f), y0 - 1.5f), dirR, len + 3f, width + 3f, outline, 1f);
            DrawLeafShape(pc, new Vector2(cx - (width * 0.18f), y0), dirR, len, width, right, 1f);
            DrawLeafShape(pc, new Vector2(cx + (width * 0.18f), y0), dirL, len, width, left, 1f);
        }

        // Cáliz: tres sépalos verdes en abanico bajo el capullo.
        private static void DrawCalyx(PixelCanvas pc, float cx, float y0, float halfWidth, float height, Color c)
        {
            DrawLeafShape(pc, new Vector2(cx, y0), new Vector2(-0.85f, 0.53f).normalized, halfWidth, height * 0.9f, c, 1f);
            DrawLeafShape(pc, new Vector2(cx, y0), new Vector2(0.85f, 0.53f).normalized, halfWidth, height * 0.9f, c, 1f);
            DrawLeafShape(pc, new Vector2(cx, y0), Vector2.up, height, halfWidth * 0.7f, c, 1f);
        }

        // Liana: tallo leñoso sobre la curva a → c → b con borde oscuro y, si leaves, dos hojitas.
        private static void DrawVine(PixelCanvas pc, Vector2 a, Vector2 c, Vector2 b, float half, float k, bool leaves)
        {
            const int steps = 40;
            for (int pass = 0; pass < 2; pass++)
            {
                Vector2 prev = a;
                for (int i = 1; i <= steps; i++)
                {
                    Vector2 p = Quad(a, c, b, i / (float)steps);
                    pc.FillCapsule(prev.x, prev.y, p.x, p.y, pass == 0 ? half + (1.4f * k) : half, pass == 0 ? Palette.Vein : Palette.LeafMid);
                    prev = p;
                }
            }

            if (!leaves)
            {
                return;
            }

            for (int j = 0; j < 2; j++)
            {
                float t = j == 0 ? 0.3f : 0.72f;
                Vector2 p = Quad(a, c, b, t);
                Vector2 tangent = (Quad(a, c, b, t + 0.02f) - Quad(a, c, b, t - 0.02f)).normalized;
                Vector2 normal = new Vector2(-tangent.y, tangent.x) * (j == 0 ? 1f : -1f);
                Vector2 dir = ((normal * 0.8f) + (tangent * 0.6f)).normalized;
                DrawLeafShape(pc, p, dir, 20f * k, 11f * k, Palette.Vein, 1f);
                DrawLeafShape(pc, p + (dir * 1f * k), dir, 18f * k, 9f * k, Palette.LeafDark, 1f);
            }
        }

        // Embudo de fibra hacia dentro en el rectángulo [x0, x0 + w] × [y0, y0 + h]: hebras que convergen hacia abajo y un
        // aro de cuerda arriba (DrawRopeSegment, la cuerda de la escena).
        private static void DrawFunnel(PixelCanvas pc, float x0, float y0, float w, float h, float k)
        {
            const int strands = 7;
            for (int i = 0; i < strands; i++)
            {
                float t = i / (float)(strands - 1);
                float top = x0 + Mathf.Lerp(w * 0.08f, w * 0.92f, t);
                float bottom = x0 + Mathf.Lerp(w * 0.36f, w * 0.64f, t);
                Color c = (i % 3) == 0 ? Palette.FiberLight : ((i % 3) == 1 ? Palette.FiberBase : Palette.FiberShadow);
                pc.FillCapsule(top, y0 + (h * 0.84f), bottom, y0 + (h * 0.14f), 3.2f * k, c);
            }

            DrawRopeSegment(pc, x0 + (w * 0.06f), y0 + (h * 0.84f), x0 + (w * 0.94f), y0 + (h * 0.84f), 10f * k, k); // aro de cuerda
            pc.FillCapsule(x0 + (w * 0.34f), y0 + (h * 0.14f), x0 + (w * 0.66f), y0 + (h * 0.14f), 3f * k, Palette.FiberShadow);
        }

        // Aro elíptico de grosor thick; con frontOnly, solo la mitad de delante (la de abajo).
        private static void StrokeEllipse(PixelCanvas pc, float cx, float cy, float rx, float ry, float thick, Color c, bool frontOnly)
        {
            int x0 = Mathf.FloorToInt(cx - rx - thick);
            int x1 = Mathf.CeilToInt(cx + rx + thick);
            int y0 = Mathf.FloorToInt(cy - ry - thick);
            int y1 = frontOnly ? Mathf.CeilToInt(cy + thick) : Mathf.CeilToInt(cy + ry + thick);
            float r = Mathf.Min(rx, ry);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = ((x + 0.5f) - cx) / rx;
                    float py = ((y + 0.5f) - cy) / ry;
                    if (frontOnly && py > 0.15f)
                    {
                        continue;
                    }

                    float d = (Mathf.Abs(Mathf.Sqrt((px * px) + (py * py)) - 1f) * r) - (thick * 0.5f);
                    pc.Blend(x, y, c, PixelCanvas.Coverage(d));
                }
            }
        }

        // Espiral de Arquímedes aplanada de turns vueltas desde el centro, más gruesa por fuera.
        private static void DrawSpiral(PixelCanvas pc, float cx, float cy, float rx, float ry, float turns, float thick, Color c)
        {
            int steps = Mathf.CeilToInt(turns * 72f);
            Vector2 prev = new Vector2(cx, cy);
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angle = t * turns * Mathf.PI * 2f;
                var p = new Vector2(cx + (Mathf.Cos(angle) * t * rx), cy + (Mathf.Sin(angle) * t * ry));
                pc.FillCapsule(prev.x, prev.y, p.x, p.y, 0.5f * thick * (0.35f + (0.65f * t)), c);
                prev = p;
            }
        }

        // Nube en el rectángulo [x0, x0 + w] × [y0, y0 + h]: base redondeada y tres copos; la panza (30 % de abajo) en sombra.
        private static void DrawCloud(PixelCanvas pc, float x0, float y0, float w, float h, Color c, float belly)
        {
            pc.FillRoundedRect(x0 + (w * 0.08f), y0 + (h * 0.05f), x0 + (w * 0.92f), y0 + (h * 0.50f), h * 0.22f, c);
            pc.FillCircle(x0 + (w * 0.30f), y0 + (h * 0.48f), h * 0.30f, c);
            pc.FillCircle(x0 + (w * 0.53f), y0 + (h * 0.58f), h * 0.38f, c);
            pc.FillCircle(x0 + (w * 0.75f), y0 + (h * 0.45f), h * 0.27f, c);
            int top = Mathf.CeilToInt(y0 + (h * 0.30f));
            for (int y = Mathf.FloorToInt(y0); y < top; y++)
            {
                for (int x = Mathf.FloorToInt(x0); x < Mathf.CeilToInt(x0 + w); x++)
                {
                    if (x >= 0 && y >= 0 && x < pc.Width && y < pc.Height)
                    {
                        pc.Multiply(x, y, belly, 1f);
                    }
                }
            }
        }

        // Gota: círculo de radio r con el centro en (cx, cy) y punta hacia arriba hasta cy + tip.
        private static void DrawDrop(PixelCanvas pc, float cx, float cy, float r, float tip, Color c)
        {
            pc.FillCircle(cx, cy, r, c);
            pc.FillTriangle(new Vector2(cx - (r * 0.86f), cy + (r * 0.5f)), new Vector2(cx + (r * 0.86f), cy + (r * 0.5f)), new Vector2(cx, cy + tip), c);
        }

        // Arco de piedras: seis piedras sobre la media elipse (rx, ry) con base en baseY.
        private static void DrawStoneArch(PixelCanvas pc, float cx, float baseY, float rx, float ry, float stone, float k)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.PI * (1f - (i / 5f));
                float sx = cx + (Mathf.Cos(angle) * rx);
                float sy = baseY + (Mathf.Sin(angle) * ry);
                float size = stone * (0.9f + (0.25f * ArtNoise.Hash(i, 3, 41)));
                Color c = (i % 2) == 0 ? StoneLight : StoneDark;
                pc.FillEllipseSoft(sx, sy, size + (1.6f * k), (size * 0.82f) + (1.6f * k), 1f, OverlayParts.Slate);
                pc.FillEllipseSoft(sx, sy, size, size * 0.82f, 1f, c);
                pc.FillEllipseSoft(sx - (size * 0.3f), sy + (size * 0.3f), size * 0.3f, size * 0.22f, 1f, ColorMath.WithAlpha(Palette.Hex(0xA9B1BA), 0.8f));
            }
        }

        // Piraña de perfil mirando a la izquierda, centrada en (cx, cy) y de largo len: cuerpo alto, lomo oscuro, costado
        // plateado y vientre claro (sin rojo), aleta dorsal, cola y ojo; la boca es una línea, sin dientes.
        private static void DrawPiranha(PixelCanvas pc, float cx, float cy, float len, float k)
        {
            float rx = len * 0.34f;
            float ry = len * 0.25f;
            float bodyX = cx - (len * 0.08f);
            pc.FillTriangle(new Vector2(bodyX + (rx * 0.8f), cy), new Vector2(cx + (len * 0.48f), cy + (ry * 0.85f)), new Vector2(cx + (len * 0.48f), cy - (ry * 0.85f)), OverlayParts.Slate);
            pc.FillTriangle(new Vector2(bodyX + (rx * 0.85f), cy), new Vector2(cx + (len * 0.45f), cy + (ry * 0.68f)), new Vector2(cx + (len * 0.45f), cy - (ry * 0.68f)), FishBack);
            pc.FillTriangle(new Vector2(bodyX - (rx * 0.1f), cy + (ry * 0.8f)), new Vector2(bodyX + (rx * 0.55f), cy + (ry * 0.75f)), new Vector2(bodyX + (rx * 0.35f), cy + (ry * 1.25f)), FishBack);
            pc.FillEllipseSoft(bodyX, cy, rx + (1.5f * k), ry + (1.5f * k), 1f, OverlayParts.Slate);
            pc.FillEllipseSoft(bodyX, cy, rx, ry, 1f, FishBack);
            pc.FillEllipseSoft(bodyX, cy - (ry * 0.12f), rx * 0.9f, ry * 0.78f, 1f, FishSide);
            pc.FillEllipseSoft(bodyX - (rx * 0.1f), cy - (ry * 0.45f), rx * 0.7f, ry * 0.45f, 1f, FishBelly);
            float eyeX = bodyX - (rx * 0.6f);
            float eyeY = cy + (ry * 0.22f);
            pc.FillCircle(eyeX, eyeY, 2.6f * k, OverlayParts.Slate);
            pc.FillCircle(eyeX - (0.7f * k), eyeY + (0.7f * k), 0.9f * k, Palette.Cream);
            pc.FillCapsule(bodyX - (rx * 0.98f), cy - (ry * 0.18f), bodyX - (rx * 0.7f), cy - (ry * 0.26f), 0.8f * k, OverlayParts.Slate);
        }
    }
}
