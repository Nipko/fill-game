#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Pieza provisional de la franja de llegada de la victoria (tarea M1.6, revisión; D-003): la línea de ondas del agua de la
    /// fila del río, <see cref="ArrivalWave"/>.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Periodo de la línea de ondas de la fila del río (dp) [P].</summary>
        public const float ArrivalWavePeriodDp = 16f;

        /// <summary>Alto de la tesela de la línea de ondas (dp): el trazo y su ondulación, centrados [P].</summary>
        public const float ArrivalWaveHeightDp = 4f;

        /// <summary>Grosor del trazo de la línea de ondas (dp) [P].</summary>
        public const float ArrivalWaveStrokeDp = 1.5f;

        // Amplitud de la ondulación (dp): el trazo cabe en la tesela con medio dp de margen arriba y abajo.
        private const float ArrivalWaveAmpDp = 0.75f;

        private Sprite? _arrivalWave;

        /// <summary>
        /// Línea de ondas blanca de la fila del río de la llegada, para <c>Image.Type.Tiled</c>: una tesela de
        /// <see cref="ArrivalWavePeriodDp"/> × <see cref="ArrivalWaveHeightDp"/> dp con un periodo de seno de trazo
        /// <see cref="ArrivalWaveStrokeDp"/> dp, repetible en horizontal. El color (y su alfa) lo pone <c>Image.color</c>. Perezosa.
        /// </summary>
        public Sprite ArrivalWave => _arrivalWave != null ? _arrivalWave : (_arrivalWave = BuildArrivalWave());

        private Sprite BuildArrivalWave()
        {
            const float k = UiPxPerDp;
            int w = Mathf.RoundToInt(ArrivalWavePeriodDp * k);
            int h = Mathf.RoundToInt(ArrivalWaveHeightDp * k);
            var pc = new PixelCanvas(w, h);
            pc.Fill(new Color(1f, 1f, 1f, 0f)); // blanco transparente: el filtrado bilineal no oscurece el canto del trazo
            const float mid = ArrivalWaveHeightDp * 0.5f;
            const float omega = 2f * Mathf.PI / ArrivalWavePeriodDp;
            for (int y = 0; y < h; y++)
            {
                float yd = (y + 0.5f) / k;
                for (int x = 0; x < w; x++)
                {
                    float xd = (x + 0.5f) / k;
                    float yw = mid + (ArrivalWaveAmpDp * Mathf.Sin(omega * xd));
                    float slope = ArrivalWaveAmpDp * omega * Mathf.Cos(omega * xd);
                    float d = (Mathf.Abs(yd - yw) / Mathf.Sqrt(1f + (slope * slope))) - (0.5f * ArrivalWaveStrokeDp);
                    pc.Blend(x, y, Color.white, PixelCanvas.Coverage(d * k));
                }
            }

            return MakeSprite(pc, "arrival_wave", new Vector2(0.5f, 0.5f), Vector4.zero, k, true);
        }
    }
}
