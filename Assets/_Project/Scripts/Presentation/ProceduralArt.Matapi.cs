#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] del Matapi (propuesta de D-007 b.2 M10; D-003): la boca con el embudo de fibra hacia dentro, los
    /// aros de cuerda del cilindro, el aro que cierra el embudo y la cuerda del tirón. Todo es de fibra (<c>fibra_*</c>,
    /// excepción de la regla 1 de direccion-visual-v2 §2.4) y reutiliza las cuerdas de la escena (<c>DrawRopeEllipse</c> y
    /// <c>DrawRopeSegment</c>). Las varillas son las de <c>ProceduralArt.Mechanics.cs</c> (<see cref="MatapiRod"/>). Nunca se
    /// dibuja una captura (camarón ni pez) ni un cacuri: el Matapi está pendiente del asesor cultural.
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
        /// <summary>Ancho de la boca del Matapi (u): el aro del embudo, más ancho que el borde de la cuia (1,10 u).</summary>
        public const float MatapiMouthWidthU = 1.5f;

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

        /// <summary>Largo de la cuerda del tirón (u).</summary>
        public const float MatapiCordLengthU = 1f;

        /// <summary>Grosor de la cuerda del tirón (u).</summary>
        public const float MatapiCordWidthU = 0.16f;

        // Interior de la boca: el hueco oscuro por el que «entra» la fruta.
        private static readonly Color MatapiHole = Palette.Hex(0x24180E);

        private Sprite? _matapiMouth;
        private Sprite? _matapiBand;
        private Sprite? _matapiTieRing;
        private Sprite? _matapiPullCord;

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
        /// Cuerda trenzada vertical del tirón (<see cref="MatapiCordWidthU"/> × <see cref="MatapiCordLengthU"/> u; crece hacia
        /// arriba desde su pie; <c>DrawRopeSegment</c>).
        /// </summary>
        internal Sprite MatapiPullCord => _matapiPullCord != null ? _matapiPullCord : (_matapiPullCord = BuildMatapiPullCord());

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

            // Hueco oscuro dentro del aro, con la pared de atrás del cono en sombra.
            pc.FillEllipseSoft(cx, cy, rx - (thick * 0.4f), ry - (thick * 0.4f), 1.2f, MatapiHole);
            pc.FillEllipseSoft(cx, cy + (ry * 0.35f), (rx - thick) * 0.8f, ry * 0.32f, 2f, ColorMath.WithAlpha(Palette.FiberShadow, 0.55f));
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

        private Sprite BuildMatapiPullCord()
        {
            const float k = MechanicPxPerU / 96f;
            int w = Mathf.RoundToInt(MatapiCordWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(MatapiCordLengthU * MechanicPxPerU);
            float thick = 10f * k;
            var pc = new PixelCanvas(w, h);
            DrawRopeSegment(pc, w * 0.5f, thick * 0.5f, w * 0.5f, h - (thick * 0.5f), thick, k);
            return MakeSprite(pc, "mech_matapi_cord", new Vector2(0.5f, 0f), Vector4.zero);
        }
    }
}
