#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Iconos de los botones de un toque Música y Sonido (D-046, punto 2; 07 §9.4; 02 §5), dibujados por código como el resto del
    /// arte provisional: una nota (dos corcheas unidas) y un altavoz con dos ondas, en <c>crema</c> con la sombra interior del canto
    /// de la madera, como el engranaje y la X (guía 5.7), sobre el disco de madera de 40 dp. Apagado: el mismo glifo atenuado y
    /// tachado por una barra crema con canto oscuro, de modo que el estado no dependa del color (07 §7.8) y se distinga igual con
    /// Alto contraste [P]. 26 dp a <see cref="UiPxPerDp"/> px/dp.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        // Lado del lienzo de los iconos de audio (px): 26 dp a 3 px/dp, como el engranaje.
        private const int AudioIconPx = 78;

        // Opacidad del glifo apagado bajo el tachado [P].
        private const float AudioOffGlyphAlpha = 0.55f;

        /// <summary>Nota de Música encendida (26 dp).</summary>
        public Sprite IconMusic { get; private set; } = null!;

        /// <summary>Nota de Música apagada: atenuada y tachada (26 dp).</summary>
        public Sprite IconMusicOff { get; private set; } = null!;

        /// <summary>Altavoz de Sonido encendido, con dos ondas (26 dp).</summary>
        public Sprite IconSound { get; private set; } = null!;

        /// <summary>Altavoz de Sonido apagado: atenuado y tachado (26 dp).</summary>
        public Sprite IconSoundOff { get; private set; } = null!;

        private void BuildAudioIcons()
        {
            IconMusic = BuildAudioIcon(false, false, "icon_music");
            IconMusicOff = BuildAudioIcon(false, true, "icon_music_off");
            IconSound = BuildAudioIcon(true, false, "icon_sound");
            IconSoundOff = BuildAudioIcon(true, true, "icon_sound_off");
        }

        private Sprite BuildAudioIcon(bool speaker, bool off, string name)
        {
            const int s = AudioIconPx;
            var pc = new PixelCanvas(s, s);
            Color edge = Palette.FamilyWood.Edge;
            Color glyph = Palette.Cream;
            if (off)
            {
                glyph.a *= AudioOffGlyphAlpha;
                edge.a *= AudioOffGlyphAlpha;
            }

            // Dos pasadas como el engranaje: la sombra 1 dp más abajo y el glifo encima (y hacia arriba).
            for (int pass = 0; pass < 2; pass++)
            {
                Color c = pass == 0 ? edge : glyph;
                float oy = pass == 0 ? -3f : 0f;
                if (speaker)
                {
                    DrawSpeaker(pc, oy, c);
                }
                else
                {
                    DrawNote(pc, oy, c);
                }
            }

            if (off)
            {
                // El tachado, de arriba a la izquierda a abajo a la derecha: canto oscuro ancho y barra crema encima.
                pc.FillCapsule(14f, 66f, 64f, 12f, 7.5f, Palette.Groove);
                pc.FillCapsule(14f, 66f, 64f, 12f, 4f, Palette.Cream);
            }

            return MakeSprite(pc, name, new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

        // Dos corcheas unidas por la barra: cabezas ovaladas abajo, plicas a su derecha y la barra arriba, algo inclinada.
        private static void DrawNote(PixelCanvas pc, float oy, Color c)
        {
            NoteHead(pc, 23f, 21f + oy, c);
            NoteHead(pc, 52f, 27f + oy, c);
            pc.FillCapsule(31.5f, 22f + oy, 31.5f, 62f + oy, 2.6f, c);
            pc.FillCapsule(60.5f, 28f + oy, 60.5f, 68f + oy, 2.6f, c);
            pc.FillCapsule(31.5f, 61f + oy, 60.5f, 67f + oy, 4.2f, c);
        }

        // Cabeza de nota: elipse de 10 × 7 px inclinada 22°.
        private static void NoteHead(PixelCanvas pc, float cx, float cy, Color c)
        {
            const float rx = 10f;
            const float ry = 7f;
            float angle = 22f * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            int x0 = Mathf.FloorToInt(cx - rx - 2f);
            int x1 = Mathf.CeilToInt(cx + rx + 2f);
            int y0 = Mathf.FloorToInt(cy - rx - 2f);
            int y1 = Mathf.CeilToInt(cy + rx + 2f);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f) - cx;
                    float dy = (y + 0.5f) - cy;
                    float u = ((dx * cos) + (dy * sin)) / rx;
                    float v = ((-dx * sin) + (dy * cos)) / ry;
                    float len = Mathf.Sqrt((u * u) + (v * v));
                    pc.Blend(x, y, c, PixelCanvas.Coverage((len - 1f) * ry));
                }
            }
        }

        // Altavoz: caja a la izquierda, cono abierto a la derecha y dos ondas.
        private static void DrawSpeaker(PixelCanvas pc, float oy, Color c)
        {
            pc.FillRoundedRect(10f, 29f + oy, 24f, 49f + oy, 2.5f, c);
            pc.FillTriangle(new Vector2(22f, 29f + oy), new Vector2(40f, 14f + oy), new Vector2(40f, 64f + oy), c);
            pc.FillTriangle(new Vector2(22f, 29f + oy), new Vector2(22f, 49f + oy), new Vector2(40f, 64f + oy), c);
            pc.StrokeArc(42f, 39f + oy, 12f, 4.6f, -48f, 48f, c);
            pc.StrokeArc(42f, 39f + oy, 23f, 4.6f, -50f, 50f, c);
        }
    }
}
