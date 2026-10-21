#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Gráficos provisionales de la Pororoca · Barajar (bloque B2; 07 §7.4 y la fila «Pororoca (Barajar)» de la tabla de iconos:
    /// «Ola grande con dos flechas cruzadas», turquesa, distinta de Marea por el tamaño de la ola y las flechas cruzadas; 04 §2.4
    /// VFX: «una ola grande cruza la pantalla de izquierda a derecha»). Parcial nuevo: no toca los demás archivos de
    /// <see cref="ProceduralArt"/>. Se construyen al primer uso, como los de la Luciérnaga. Los sustituye el arte del artista.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        private Sprite? _iconWave;
        private Sprite? _iconWaveDisabled;
        private Sprite? _shuffleWave;

        /// <summary>Glifo de Barajar · Pororoca (32 dp a 3 px/dp): una ola grande que rompe, con dos flechas cruzadas debajo.</summary>
        public Sprite IconWave => _iconWave != null ? _iconWave : (_iconWave = BuildIconWave(false));

        /// <summary>Glifo deshabilitado (<c>deshabilitado_glifo</c>, plano).</summary>
        public Sprite IconWaveDisabled => _iconWaveDisabled != null ? _iconWaveDisabled : (_iconWaveDisabled = BuildIconWave(true));

        /// <summary>
        /// La ola que cruza la pantalla (04 §2.4 VFX), 96 × 160 dp a 1 px/dp, con pivote en el borde derecho (su frente): una
        /// cresta turquesa que rompe hacia la derecha, espuma crema en el frente y el cuerpo que se desvanece hacia la izquierda.
        /// </summary>
        public Sprite ShuffleWaveSprite => _shuffleWave != null ? _shuffleWave : (_shuffleWave = BuildShuffleWave());

        private Sprite BuildIconWave(bool disabled)
        {
            var pc = new PixelCanvas(96, 96);
            Color body = disabled ? Palette.DisabledGlyph : Palette.IconTurquoise;
            Color crest = disabled ? Palette.DisabledGlyph : Palette.Cream;
            Color arrow = disabled ? Palette.DisabledGlyph : Palette.Cream;
            Color shade = disabled ? Color.clear : Palette.FamilyWater.Edge;

            // Cuerpo de la ola: media luna que sube por la izquierda y se curva sobre sí misma arriba a la derecha.
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 0 && shade.a <= 0f)
                {
                    continue;
                }

                float oy = pass == 0 ? -3f : 0f;
                Color c = pass == 0 ? shade : body;
                pc.StrokeArc(50f, 52f + oy, 30f, 12f, 20f, 200f, c);
                pc.FillCircle(70f, 72f + oy, 9f, c);
            }

            if (!disabled)
            {
                pc.StrokeArc(50f, 52f, 24f, 3.2f, 40f, 170f, crest); // la espuma de la cresta
            }

            // Dos flechas cruzadas (07 §7.4): de abajo a la izquierda arriba a la derecha y al revés.
            pc.FillCapsule(22f, 14f, 58f, 34f, 3.2f, arrow);
            pc.FillCapsule(22f, 34f, 58f, 14f, 3.2f, arrow);
            pc.FillTriangle(new Vector2(64f, 37f), new Vector2(51f, 38f), new Vector2(58f, 27f), arrow);
            pc.FillTriangle(new Vector2(64f, 11f), new Vector2(58f, 21f), new Vector2(51f, 10f), arrow);
            return MakeSprite(pc, disabled ? "icon_wave_disabled" : "icon_wave", new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        private Sprite BuildShuffleWave()
        {
            const int w = 96;
            const int h = 160;
            var pc = new PixelCanvas(w, h);
            Color deep = Palette.FamilyWater.Edge;
            Color body = Palette.IconTurquoise;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // El frente está a la derecha; la ola es más alta en el frente y se desvanece hacia atrás.
                    float u = (x + 0.5f) / w;
                    float crestY = h * (0.35f + (0.55f * u * u));
                    float d = (y + 0.5f) - crestY;
                    float alpha = PixelCanvas.Coverage(d) * (0.25f + (0.65f * u));
                    if (alpha > 0f)
                    {
                        pc.Blend(x, y, Color.Lerp(deep, body, u), alpha);
                    }
                }
            }

            // Espuma: el labio de la cresta y el frente que rompe.
            for (int i = 0; i < 12; i++)
            {
                float u = (i + 0.5f) / 12f;
                pc.FillCircle(u * w, h * (0.35f + (0.55f * u * u)) - 2f, 2.5f + (3f * u), Palette.Cream);
            }

            pc.FillCapsule(w - 6f, 10f, w - 6f, h - 14f, 5f, ColorMath.WithAlpha(Palette.Cream, 0.85f));
            return MakeSprite(pc, "shuffle_wave", new Vector2(1f, 0f), Vector4.zero, 1f);
        }
    }
}
