#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] del Aguapé (M8, la mecánica congelada de la 1.1; 02 §9.1 M8, regla 9; D-068; arte por código, D-052):
    /// una mata de aguapé (jacinto de agua) que abraza la base del grupo hasta media altura de las cuias. Hojas redondas y
    /// brillantes sobre peciolos hinchados (los flotadores) y una espiga lila: una silueta de rosetas, distinta de la hoja de
    /// palma alargada de la Hoja escudo y de la Flor de vitória-régia (glosario §6.3). Piezas:
    /// <list type="bullet">
    /// <item><description><see cref="AguapeMat"/>: la mata de detrás de la cuia (sale por los lados del vidrio y une el grupo);</description></item>
    /// <item><description><see cref="AguapeTuft"/>: la mata de delante, en los cantos del vidrio, sin tapar el centro (el
    /// contenido se ve, regla 1);</description></item>
    /// <item><description><see cref="AguapeBit"/>: una hoja suelta, para los trozos que se alejan flotando en la suelta.</description></item>
    /// </list>
    /// Y el dibujo del icono de la mecánica (32 dp y el grande del estreno), con el espeso (<see cref="AguapeThickIconKey"/>).
    /// </summary>
    /// <remarks>
    /// Color (direccion-visual-v2 §2.4, regla 1): verdes azulados que quedan a ΔE00 ≥ 19 de los 14 tintes
    /// (<see cref="AguapeLeaf"/> 27,2, <see cref="AguapeLeafLight"/> 19,9, <see cref="AguapeLeafDark"/> 31,4) y la espiga lila
    /// <see cref="AguapeFlower"/> (15,7, lejos del açaí y de la bacaba). Perezosos, a <see cref="MechanicPxPerU"/> px por u; los
    /// sustituye el arte del artista. El nombre «Aguapé» está en firme para la 1.0 por D-054/D-064; el asesor lo revisa en
    /// versiones futuras.
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Clave del icono grande del estreno del Aguapé espeso (<c>intro_mechanic</c> <c>aguape_n2</c>; D-068).</summary>
        public const string AguapeThickIconKey = "aguape_n2";

        /// <summary>Ancho de la mata de detrás (u): sale 0,3 u por cada lado del vidrio de 2 u.</summary>
        public const float AguapeMatWidthU = 2.6f;

        /// <summary>Alto de la mata de detrás (u), desde el pie: hasta media altura de una cuia de 4 dosis (5,6 u; regla 9).</summary>
        public const float AguapeMatHeightU = 2.8f;

        /// <summary>Ancho de una mata de delante (u).</summary>
        public const float AguapeTuftWidthU = 0.95f;

        /// <summary>Alto de una mata de delante (u), desde el pie.</summary>
        public const float AguapeTuftHeightU = 1.5f;

        /// <summary>Lado de una hoja suelta (u).</summary>
        public const float AguapeBitU = 0.62f;

        /// <summary>Verde de las hojas [P]: #2E6B57, a ΔE00 27,2 del tinte más cercano.</summary>
        public static readonly Color AguapeLeaf = Palette.Hex(0x2E6B57);

        /// <summary>Luz de las hojas [P]: #3C7D63 (ΔE00 19,9).</summary>
        public static readonly Color AguapeLeafLight = Palette.Hex(0x3C7D63);

        /// <summary>Sombra y canto de las hojas y de los flotadores [P]: #1B4A3B (ΔE00 31,4).</summary>
        public static readonly Color AguapeLeafDark = Palette.Hex(0x1B4A3B);

        /// <summary>Lila de la espiga [P]: #A894D8 (ΔE00 15,7).</summary>
        public static readonly Color AguapeFlower = Palette.Hex(0xA894D8);

        private Sprite? _aguapeMat;
        private Sprite? _aguapeTuft;
        private Sprite? _aguapeBit;

        /// <summary>
        /// Mata de detrás (<see cref="AguapeMatWidthU"/> × <see cref="AguapeMatHeightU"/> u, pivote en el centro del pie): rosetas de
        /// hojas redondas sobre sus flotadores, más tupidas abajo, y una espiga lila arriba a la derecha. Ya coloreada.
        /// </summary>
        internal Sprite AguapeMat => _aguapeMat != null ? _aguapeMat : (_aguapeMat = BuildAguapeMat());

        /// <summary>Mata de delante (<see cref="AguapeTuftWidthU"/> × <see cref="AguapeTuftHeightU"/> u, pivote en el pie), coloreada.</summary>
        internal Sprite AguapeTuft => _aguapeTuft != null ? _aguapeTuft : (_aguapeTuft = BuildAguapeTuft());

        /// <summary>Hoja suelta (<see cref="AguapeBitU"/> u de lado, pivote en el centro) con el arranque del peciolo, coloreada.</summary>
        internal Sprite AguapeBit => _aguapeBit != null ? _aguapeBit : (_aguapeBit = BuildAguapeBit());

        private Sprite BuildAguapeMat()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(AguapeMatWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(AguapeMatHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;

            // Sombra de agua bajo la mata.
            pc.FillEllipseSoft(cx, 10f * k, (w * 0.5f) - (4f * k), 16f * k, 8f * k, ColorMath.WithAlpha(AguapeLeafDark, 0.55f));

            // Flotadores (peciolos hinchados) y tallos, de abajo arriba; después las hojas, de atrás adelante.
            float[] xs = { -1.02f, -0.62f, -0.18f, 0.24f, 0.66f, 1.04f, -0.86f, 0.86f, -0.40f, 0.42f };
            float[] ys = { 1.55f, 2.20f, 1.72f, 2.38f, 1.80f, 1.50f, 0.78f, 0.82f, 1.10f, 1.18f };
            float[] rs = { 0.36f, 0.40f, 0.42f, 0.40f, 0.42f, 0.34f, 0.40f, 0.40f, 0.42f, 0.42f };
            for (int i = 0; i < xs.Length; i++)
            {
                float lx = cx + (xs[i] * MechanicPxPerU);
                float ly = ys[i] * MechanicPxPerU;
                float bx = cx + (xs[i] * 0.7f * MechanicPxPerU);
                float by = 0.34f * MechanicPxPerU;
                pc.FillCapsule(bx, by, lx, ly - (rs[i] * 0.6f * MechanicPxPerU), 3.2f * k, AguapeLeafDark);
                DrawAguapeFloat(pc, bx, by, 0.17f * MechanicPxPerU, k);
            }

            for (int i = 0; i < xs.Length; i++)
            {
                DrawAguapeLeaf(pc, cx + (xs[i] * MechanicPxPerU), ys[i] * MechanicPxPerU, rs[i] * MechanicPxPerU, k);
            }

            DrawAguapeSpike(pc, cx + (0.55f * MechanicPxPerU), 2.10f * MechanicPxPerU, 0.62f * MechanicPxPerU, k);
            return MakeSprite(pc, "mech_aguape_mat", new Vector2(0.5f, 0f), Vector4.zero);
        }

        private Sprite BuildAguapeTuft()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(AguapeTuftWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(AguapeTuftHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            float u = MechanicPxPerU;
            pc.FillCapsule(cx - (0.10f * u), 0.20f * u, cx - (0.12f * u), 1.00f * u, 3f * k, AguapeLeafDark);
            pc.FillCapsule(cx + (0.12f * u), 0.20f * u, cx + (0.10f * u), 0.66f * u, 3f * k, AguapeLeafDark);
            DrawAguapeFloat(pc, cx - (0.08f * u), 0.22f * u, 0.16f * u, k);
            DrawAguapeFloat(pc, cx + (0.14f * u), 0.20f * u, 0.15f * u, k);
            DrawAguapeLeaf(pc, cx + (0.10f * u), 0.70f * u, 0.30f * u, k);
            DrawAguapeLeaf(pc, cx - (0.10f * u), 1.12f * u, 0.32f * u, k);
            return MakeSprite(pc, "mech_aguape_tuft", new Vector2(0.5f, 0f), Vector4.zero);
        }

        private Sprite BuildAguapeBit()
        {
            const float k = MechanicPxPerU / 96f;
            int s = Mathf.RoundToInt(AguapeBitU * MechanicPxPerU);
            var pc = new PixelCanvas(s, s);
            float c = s * 0.5f;
            pc.FillCapsule(c, c, c + (0.22f * MechanicPxPerU), c - (0.24f * MechanicPxPerU), 3f * k, AguapeLeafDark);
            DrawAguapeLeaf(pc, c - (0.03f * MechanicPxPerU), c + (0.03f * MechanicPxPerU), 0.24f * MechanicPxPerU, k);
            return MakeSprite(pc, "mech_aguape_bit", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Una hoja redonda de aguapé centrada en (x, y) con radio r px: canto oscuro, relleno, luz arriba a la izquierda, la muesca
        // del peciolo abajo y un nervio claro.
        private static void DrawAguapeLeaf(PixelCanvas pc, float x, float y, float r, float k)
        {
            pc.FillEllipseSoft(x, y, r + (2.2f * k), (r * 0.92f) + (2.2f * k), 1.4f * k, AguapeLeafDark);
            pc.FillEllipseSoft(x, y, r, r * 0.92f, 1.2f * k, AguapeLeaf);
            pc.FillEllipseSoft(x - (r * 0.28f), y + (r * 0.24f), r * 0.52f, r * 0.42f, 3f * k, AguapeLeafLight);
            pc.FillEllipseSoft(x - (r * 0.38f), y + (r * 0.40f), r * 0.16f, r * 0.10f, 1.4f * k, ColorMath.WithAlpha(Palette.Glint, 0.55f));
            pc.FillTriangle(new Vector2(x, y - (r * 0.15f)), new Vector2(x - (r * 0.22f), y - (r * 0.98f)), new Vector2(x + (r * 0.22f), y - (r * 0.98f)), AguapeLeafDark);
            pc.FillCapsule(x, y - (r * 0.12f), x + (r * 0.10f), y + (r * 0.62f), 0.9f * k, ColorMath.WithAlpha(AguapeLeafLight, 0.8f));
        }

        // Un flotador (el peciolo hinchado del aguapé): una bolita de canto oscuro y luz suave.
        private static void DrawAguapeFloat(PixelCanvas pc, float x, float y, float r, float k)
        {
            pc.FillEllipseSoft(x, y, r + (1.8f * k), (r * 0.8f) + (1.8f * k), 1.2f * k, AguapeLeafDark);
            pc.FillEllipseSoft(x, y, r, r * 0.8f, 1.2f * k, AguapeLeafLight);
            pc.FillEllipseSoft(x - (r * 0.3f), y + (r * 0.25f), r * 0.35f, r * 0.25f, 1.4f * k, ColorMath.WithAlpha(Palette.Glint, 0.45f));
        }

        // La espiga lila: un tallo y cinco flores en racimo, con un punto claro en cada una.
        private static void DrawAguapeSpike(PixelCanvas pc, float x, float y, float len, float k)
        {
            pc.FillCapsule(x, y - (len * 0.45f), x, y + (len * 0.40f), 2.4f * k, AguapeLeafDark);
            for (int i = 0; i < 5; i++)
            {
                float fy = y + ((i - 2) * len * 0.17f);
                float fx = x + ((i % 2 == 0 ? -1f : 1f) * 6f * k);
                pc.FillCircle(fx, fy, 9.5f * k, ColorMath.Darken(AguapeFlower, 0.25f));
                pc.FillCircle(fx, fy, 8f * k, AguapeFlower);
                pc.FillCircle(fx - (1.5f * k), fy + (1.5f * k), 2.6f * k, Palette.Glint);
            }
        }

        // Icono de la mecánica en un lienzo cuadrado de s px (k = s / 96): una roseta de tres hojas redondas sobre sus flotadores y
        // la espiga lila; con thick (el espeso), una segunda fila de hojas detrás, más oscura. Sin tintes de fruta (07 §3, regla 6).
        private static void DrawAguapeGlyph(PixelCanvas pc, int s, bool thick)
        {
            float k = s / 96f;
            if (thick)
            {
                DrawAguapeLeaf(pc, 26f * k, 62f * k, 17f * k, k);
                DrawAguapeLeaf(pc, 70f * k, 64f * k, 17f * k, k);
            }

            pc.FillCapsule(30f * k, 16f * k, 30f * k, 36f * k, 2.4f * k, AguapeLeafDark);
            pc.FillCapsule(62f * k, 16f * k, 64f * k, 34f * k, 2.4f * k, AguapeLeafDark);
            DrawAguapeFloat(pc, 30f * k, 16f * k, 9f * k, k);
            DrawAguapeFloat(pc, 62f * k, 14f * k, 9f * k, k);
            DrawAguapeLeaf(pc, 30f * k, 44f * k, 19f * k, k);
            DrawAguapeLeaf(pc, 64f * k, 42f * k, 19f * k, k);
            DrawAguapeLeaf(pc, 46f * k, 58f * k, 18f * k, k);
            DrawAguapeSpike(pc, 76f * k, 72f * k, 34f * k, k * 0.8f);
        }
    }
}
