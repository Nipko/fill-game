#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] del estado de las cuias (D-003; direccion-visual-v2 §2.4): el «?» de Aguas turbias (07 §4.3), los
    /// grumos del Barro (02 §9.1 M5) y sus iconos de 32 dp, que reutilizarán la tarjeta previa y el HUD. Todo se genera de
    /// forma perezosa la primera vez que se pide (como <see cref="RiverSwell"/>), así que un nivel sin turbias ni Barro no
    /// crea ninguna textura. Tras <see cref="Dispose"/> los sprites destruidos se vuelven a generar (el == de Unity).
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
    }
}
