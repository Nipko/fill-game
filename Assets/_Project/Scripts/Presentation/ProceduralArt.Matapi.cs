#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] del Matapi (propuesta de D-007 b.2 M10; D-003): la boca con el embudo de fibra hacia dentro, los
    /// aros de cuerda del cilindro, el aro que cierra el embudo y la cuerda del tirón, floja y tensa. Todo es de fibra
    /// (<c>fibra_*</c>, excepción de la regla 1 de direccion-visual-v2 §2.4) salvo el hueco oscuro de la boca
    /// (<see cref="MatapiHole"/>, que cuenta en el test de colores), y reutiliza las cuerdas de la escena
    /// (<c>DrawRopeEllipse</c> y <c>DrawRopeSegment</c>). Las varillas son las de <c>ProceduralArt.Mechanics.cs</c>
    /// (<see cref="MatapiRod"/>). Nunca se dibuja una captura (camarón ni pez) ni un cacuri: el Matapi está pendiente del
    /// asesor cultural.
    /// </summary>
    /// <remarks>
    /// <para>Sustituyen a <see cref="MatapiHoop"/> y <see cref="MatapiFunnel"/> de <c>ProceduralArt.Mechanics.cs</c>: en las
    /// capturas de revisión el aro liso se leía como un corchete y el embudo de 0,65 u, como una corona, no como una boca que
    /// «entra». Esos dos quedan sin uso y nunca se generan.</para>
    /// <para>Perezosos, como el resto de las piezas de los overlays: un nivel sin Matapi no crea estas texturas. Tras
    /// <see cref="Dispose"/> se vuelven a generar (el == de Unity).</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>
        /// Ancho de la boca del Matapi (u): el aro del embudo, bastante más ancho que el borde de la cuia (1,10 u), para que
        /// domine el «entra».
        /// </summary>
        public const float MatapiMouthWidthU = 1.7f;

        /// <summary>Alto de la boca del Matapi (u): el aro arriba y el cono que entra por el cuello.</summary>
        public const float MatapiMouthHeightU = 0.95f;

        /// <summary>Centro del aro de la boca sobre el canto inferior del sprite (u).</summary>
        public const float MatapiMouthRimU = 0.73f;

        /// <summary>Ancho de un aro de cuerda del cilindro (u).</summary>
        public const float MatapiBandWidthU = 2.1f;

        /// <summary>Alto de un aro de cuerda del cilindro (u): la mitad de delante de la elipse.</summary>
        public const float MatapiBandHeightU = 0.36f;

        /// <summary>Ancho del aro de cuerda del embudo (u) a escala 1.</summary>
        public const float MatapiTieWidthU = 1.15f;

        /// <summary>Alto del aro de cuerda del embudo (u): la elipse vista desde delante y un poco desde arriba.</summary>
        public const float MatapiTieHeightU = 0.34f;

        /// <summary>Largo de la cuerda del tirón (u): corta, para que no se lea como una pajita clavada en el embudo.</summary>
        public const float MatapiCordLengthU = 0.6f;

        /// <summary>Tramo de la punta de la cuerda que se desvanece hasta alfa 0 (u): no acaba en un corte seco.</summary>
        public const float MatapiCordFadeU = 0.25f;

        /// <summary>Grosor de la cuerda del tirón (u).</summary>
        public const float MatapiCordWidthU = 0.16f;

        /// <summary>Flecha de la catenaria de la cuerda floja, hacia la derecha a media altura (u).</summary>
        public const float MatapiCordSagU = 0.08f;

        /// <summary>Ancho de los dos sprites de la cuerda (u): el grosor y la flecha a cada lado, con la cuerda centrada.</summary>
        public const float MatapiCordSpriteWidthU = MatapiCordWidthU + (2f * MatapiCordSagU);

        /// <summary>
        /// Interior de la boca: el hueco oscuro #24180E por el que «entra» la fruta. No es un tono de fibra: está en
        /// <c>MatapiOverlay.CollectSurfaceColors</c> (a ΔE00 ≥ 10 de los tintes; el más cercano, Cacao).
        /// </summary>
        internal static readonly Color MatapiHole = Palette.Hex(0x24180E);

        private Sprite? _matapiMouth;
        private Sprite? _matapiBand;
        private Sprite? _matapiTieRing;
        private Sprite? _matapiPullCord;
        private Sprite? _matapiPullCordSlack;

        /// <summary>
        /// Boca del Matapi (<see cref="MatapiMouthWidthU"/> × <see cref="MatapiMouthHeightU"/> u): un aro de cuerda visto
        /// desde arriba con el hueco oscuro dentro y el cono de hebras de fibra que se estrecha hacia el cuello. La forma
        /// dice «entra» (b.2 M10).
        /// </summary>
        internal Sprite MatapiMouth => _matapiMouth != null ? _matapiMouth : (_matapiMouth = BuildMatapiMouth());

        /// <summary>Aro de cuerda del cilindro: la mitad de delante de una elipse (<see cref="MatapiBandWidthU"/> × <see cref="MatapiBandHeightU"/> u).</summary>
        internal Sprite MatapiBand => _matapiBand != null ? _matapiBand : (_matapiBand = BuildMatapiBand());

        /// <summary>
        /// Aro de cuerda trenzada que ciñe el embudo del Matapi al sellarse (<see cref="MatapiTieWidthU"/> ×
        /// <see cref="MatapiTieHeightU"/> u): la mitad de atrás, más oscura (<c>DrawRopeEllipse</c>).
        /// </summary>
        internal Sprite MatapiTieRing => _matapiTieRing != null ? _matapiTieRing : (_matapiTieRing = BuildMatapiTieRing());

        /// <summary>
        /// Cuerda trenzada del tirón, tensa y recta (<see cref="MatapiCordWidthU"/> de grosor y <see cref="MatapiCordLengthU"/>
        /// de largo, centrada en un sprite de <see cref="MatapiCordSpriteWidthU"/>; crece hacia arriba desde su pie;
        /// <c>DrawRopeSegment</c>), con los últimos <see cref="MatapiCordFadeU"/> u desvanecidos.
        /// </summary>
        internal Sprite MatapiPullCord => _matapiPullCord != null ? _matapiPullCord : (_matapiPullCord = BuildMatapiPullCord(0f, "mech_matapi_cord"));

        /// <summary>
        /// La misma cuerda floja, antes del tirón: una catenaria ligera con <see cref="MatapiCordSagU"/> de flecha hacia la
        /// derecha (el overlay la inclina hacia ese lado), del mismo tamaño que <see cref="MatapiPullCord"/>.
        /// </summary>
        internal Sprite MatapiPullCordSlack => _matapiPullCordSlack != null ? _matapiPullCordSlack : (_matapiPullCordSlack = BuildMatapiPullCord(MatapiCordSagU, "mech_matapi_cord_slack"));

        private Sprite BuildMatapiMouth()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(MatapiMouthWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(MatapiMouthHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float thick = 10f * k;
            float cx = w * 0.5f;
            float cy = MatapiMouthRimU * MechanicPxPerU;
            float rx = (w * 0.5f) - (thick * 0.6f);
            float ry = 0.19f * MechanicPxPerU;
            float bottomHalf = 0.24f * MechanicPxPerU;
            float bottomY = 3f * k;

            // Cono de hebras desde la mitad de delante del aro hasta el cuello, con dos pasadas de trenzado.
            const int strands = 9;
            for (int i = 0; i < strands; i++)
            {
                float t = i / (float)(strands - 1);
                float a = Mathf.Lerp(200f, 340f, t) * Mathf.Deg2Rad;
                var top = new Vector2(cx + (Mathf.Cos(a) * rx * 0.96f), cy + (Mathf.Sin(a) * ry * 0.96f));
                var bottom = new Vector2(cx + Mathf.Lerp(-bottomHalf, bottomHalf, t), bottomY);
                Color c = (i % 3) == 0 ? Palette.FiberLight : ((i % 3) == 1 ? Palette.FiberBase : Palette.FiberShadow);
                pc.FillCapsule(top.x, top.y, bottom.x, bottom.y, 3.6f * k, ColorMath.Darken(Palette.FiberShadow, 0.35f));
                pc.FillCapsule(top.x, top.y, bottom.x, bottom.y, 2.6f * k, c);
            }

            for (int j = 0; j < 2; j++)
            {
                float y = Mathf.Lerp(bottomY, cy - ry, j == 0 ? 0.35f : 0.7f);
                float half = Mathf.Lerp(bottomHalf, rx * 0.9f, (y - bottomY) / Mathf.Max(1f, cy - ry - bottomY));
                pc.FillCapsule(cx - half, y, cx + half, y - (1.5f * k), 1.6f * k, ColorMath.WithAlpha(Palette.FiberShadow, 0.9f));
            }

            // Hueco oscuro dentro del aro, con la pared de atrás del cono en sombra y el canto interior del aro en fibra_luz:
            // el labio claro alrededor del hueco es lo que dice «entra» a 48 dp.
            pc.FillEllipseSoft(cx, cy, rx - (thick * 0.4f), ry - (thick * 0.4f), 1.2f, MatapiHole);
            pc.FillEllipseSoft(cx, cy + (ry * 0.35f), (rx - thick) * 0.8f, ry * 0.32f, 2f, ColorMath.WithAlpha(Palette.FiberShadow, 0.55f));
            float lipRx = rx - (thick * 0.62f);
            float lipRy = ry - (thick * 0.62f);
            const int lipSteps = 48;
            var lipPrev = new Vector2(cx + lipRx, cy);
            for (int i = 1; i <= lipSteps; i++)
            {
                float a = (i / (float)lipSteps) * Mathf.PI * 2f;
                var lip = new Vector2(cx + (Mathf.Cos(a) * lipRx), cy + (Mathf.Sin(a) * lipRy));
                pc.FillCapsule(lipPrev.x, lipPrev.y, lip.x, lip.y, 3.2f * k, Palette.FiberLight);
                lipPrev = lip;
            }

            DrawRopeEllipse(pc, cx, cy, rx, ry, thick, k);
            return MakeSprite(pc, "mech_matapi_mouth", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildMatapiBand()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(MatapiBandWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(MatapiBandHeightU * MechanicPxPerU);
            var pc = new PixelCanvas(w, h);
            float thick = 9f * k;
            float cx = w * 0.5f;
            float cy = h - (thick * 0.6f);
            float rx = (w * 0.5f) - (thick * 0.6f);
            float ry = h - (thick * 1.2f);

            // Mitad de delante de la elipse (la de abajo), trenzada como DrawRopeEllipse.
            const int steps = 28;
            var prev = new Vector2(cx - rx, cy);
            for (int i = 1; i <= steps; i++)
            {
                float a = Mathf.PI + ((i / (float)steps) * Mathf.PI);
                var p = new Vector2(cx + (Mathf.Cos(a) * rx), cy + (Mathf.Sin(a) * ry));
                pc.FillCapsule(prev.x, prev.y, p.x, p.y, (thick * 0.5f) + (1.2f * k), ColorMath.Darken(Palette.FiberShadow, 0.35f));
                prev = p;
            }

            prev = new Vector2(cx - rx, cy);
            for (int i = 1; i <= steps; i++)
            {
                float a = Mathf.PI + ((i / (float)steps) * Mathf.PI);
                var p = new Vector2(cx + (Mathf.Cos(a) * rx), cy + (Mathf.Sin(a) * ry));
                Color c = (i % 3) == 0 ? Palette.FiberLight : ((i % 3) == 1 ? Palette.FiberBase : Palette.FiberShadow);
                pc.FillCapsule(prev.x, prev.y, p.x, p.y, thick * 0.5f, c);
                prev = p;
            }

            return MakeSprite(pc, "mech_matapi_band", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        private Sprite BuildMatapiTieRing()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(MatapiTieWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(MatapiTieHeightU * MechanicPxPerU);
            float thick = 10f * k;
            var pc = new PixelCanvas(w, h);
            DrawRopeEllipse(pc, w * 0.5f, h * 0.5f, (w * 0.5f) - (thick * 0.6f), (h * 0.5f) - (thick * 0.6f), thick, k);
            return MakeSprite(pc, "mech_matapi_tie", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Cuerda del tirón de pie a punta, centrada; con sagU > 0, en catenaria (parábola) con esa flecha hacia la derecha.
        private Sprite BuildMatapiPullCord(float sagU, string name)
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(MatapiCordSpriteWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(MatapiCordLengthU * MechanicPxPerU);
            float thick = 10f * k;
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            float y0 = thick * 0.5f;
            float y1 = h - (thick * 0.5f);
            float sag = sagU * MechanicPxPerU;
            const int pieces = 6;
            var prev = new Vector2(cx, y0);
            for (int i = 1; i <= pieces; i++)
            {
                float t = i / (float)pieces;
                var p = new Vector2(cx + (4f * sag * t * (1f - t)), Mathf.Lerp(y0, y1, t));
                DrawRopeSegment(pc, prev.x, prev.y, p.x, p.y, thick, k);
                prev = p;
            }

            // La punta se desvanece hacia arriba: la cuerda sigue hacia quien tira, fuera de la escena, sin un corte seco.
            float fade = MatapiCordFadeU * MechanicPxPerU;
            for (int y = 0; y < h; y++)
            {
                float a = 1f - Mathf.SmoothStep(0f, 1f, ((y + 0.5f) - (h - fade)) / fade);
                if (a < 1f)
                {
                    for (int x = 0; x < w; x++)
                    {
                        pc.MaskAlpha(x, y, a);
                    }
                }
            }

            return MakeSprite(pc, name, new Vector2(0.5f, 0f), Vector4.zero);
        }
    }
}
