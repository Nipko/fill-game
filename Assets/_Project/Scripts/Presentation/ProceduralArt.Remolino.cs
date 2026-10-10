#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] del Remolino (02 §9.1 M4; propuesta de D-007 b.2 M4: «espiral en la base de las dos cuias, unidas
    /// por un canal bajo el tablero»; D-003): la cápsula con la que se traza el canal entre las dos cuias de la pareja y la
    /// espiral de espuma aplanada sobre el remanso, en <see cref="RemolinoSpiralFrames"/> fases para que gire mientras la
    /// pareja recibe. Los colores del canal quedan a ΔE00 ≥ 10 de los 14 tintes y lejos del turquesa (direccion-visual-v2
    /// §2.4, regla 1; los comprueba <c>CuiaOverlayTests</c>).
    /// </summary>
    /// <remarks>
    /// Perezosos, como los de <c>ProceduralArt.Mechanics.cs</c>: un nivel sin Remolino no crea ninguna textura. Blancos, para
    /// que los tiña <c>Image.color</c>. Las fases de la espiral comparten una sola textura (un sprite por fase sobre ella). El
    /// remanso (<see cref="WhirlPool"/>) es el de T03.
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Radio (px) del círculo de la cápsula del canal; es también su borde 9-slice.</summary>
        public const int RemolinoCapsuleRadiusPx = 12;

        /// <summary>Fases de la espiral (30° cada una): la espiral gira cambiando de fase, sin deformarse.</summary>
        public const int RemolinoSpiralFrames = 12;

        /// <summary>Ancho de la espiral aplanada (u).</summary>
        public const float RemolinoSpiralWidthU = 2.3f;

        /// <summary>Alto de la espiral aplanada (u): la perspectiva del remanso sobre la repisa.</summary>
        public const float RemolinoSpiralHeightU = 0.66f;

        /// <summary>
        /// Agua del canal del Remolino #7E959B [P]: gris azulado claro de remanso, L* 60, que se lee sobre el panel azul petróleo
        /// (ΔE00 31-38) y queda a ΔE00 15,8 del tinte más cercano (Agua del Tapajós) y del turquesa.
        /// </summary>
        public static readonly Color RemolinoChannelWater = Palette.Hex(0x7E959B);

        /// <summary>Canto oscuro del canal #0F1E22 [P] (≈ 1,5 dp): lo separa de la madera de la repisa que cruza.</summary>
        public static readonly Color RemolinoChannelRim = Palette.Hex(0x0F1E22);

        // Lienzo de una fase de la espiral (px) con 2 px de margen arriba y abajo para que el filtrado no mezcle fases.
        private const int RemolinoSpiralW = (int)(RemolinoSpiralWidthU * MechanicPxPerU);
        private const int RemolinoSpiralH = (int)(RemolinoSpiralHeightU * MechanicPxPerU) + 4;

        private Sprite? _remolinoCapsule;
        private Sprite?[]? _remolinoSpiral;

        /// <summary>
        /// Cápsula blanca para el canal: un círculo de <see cref="RemolinoCapsuleRadiusPx"/> px de radio con un centro de 2 px y
        /// borde 9-slice igual al radio. Estirada (<c>Image.Type.Sliced</c>) a (largo + ancho) × ancho da un tramo con los dos
        /// extremos redondos, así que los tramos que se solapan en un vértice hacen un codo redondo.
        /// </summary>
        public Sprite RemolinoCapsule => _remolinoCapsule != null ? _remolinoCapsule : (_remolinoCapsule = BuildRemolinoCapsule());

        /// <summary>
        /// Fase <paramref name="frame"/> (módulo <see cref="RemolinoSpiralFrames"/>) de la espiral de espuma aplanada (blanca,
        /// <see cref="RemolinoSpiralWidthU"/> × <see cref="RemolinoSpiralHeightU"/> u): 2,4 vueltas con el trazo más grueso por
        /// fuera; cada fase gira la espiral 30° en el sentido de las agujas del reloj.
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

        private Sprite BuildRemolinoCapsule()
        {
            const int r = RemolinoCapsuleRadiusPx;
            const int s = (2 * r) + 2;
            var pc = new PixelCanvas(s, s);

            // Cuartos de círculo centrados en las esquinas del centro de 2 px: con el borde 9-slice igual al radio, los cantos
            // estirados quedan rectos y los extremos, redondos.
            pc.FillRoundedRect(0.5f, 0.5f, s - 0.5f, s - 0.5f, r - 0.5f, Color.white);
            return MakeSprite(pc, "remolino_capsule", new Vector2(0.5f, 0.5f), new Vector4(r, r, r, r));
        }

        // Las fases en una sola textura (una fila por fase) y un sprite por fase.
        private Sprite?[] BuildRemolinoSpiral()
        {
            const int w = RemolinoSpiralW;
            const int h = RemolinoSpiralH;
            var pc = new PixelCanvas(w, h * RemolinoSpiralFrames);
            for (int f = 0; f < RemolinoSpiralFrames; f++)
            {
                float phase = -f * (2f * Mathf.PI / RemolinoSpiralFrames);
                DrawRemolinoSpiral(pc, w * 0.5f, (f * h) + (h * 0.5f), (w * 0.5f) - 8f, (h * 0.5f) - 6f, 2.4f, 13f, phase);
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

        // Espiral de Arquímedes aplanada (rx × ry) de turns vueltas desde el centro, girada phase radianes, con el trazo de
        // thick px por fuera y el 40 % en el centro (la espuma se abre al girar).
        private static void DrawRemolinoSpiral(PixelCanvas pc, float cx, float cy, float rx, float ry, float turns, float thick, float phase)
        {
            int steps = Mathf.CeilToInt(turns * 90f);
            Vector2 prev = new Vector2(cx + (Mathf.Cos(phase) * 0.02f * rx), cy + (Mathf.Sin(phase) * 0.02f * ry));
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angle = phase + (t * turns * Mathf.PI * 2f);
                float r = 0.06f + (0.94f * t);
                var p = new Vector2(cx + (Mathf.Cos(angle) * r * rx), cy + (Mathf.Sin(angle) * r * ry));
                pc.FillCapsule(prev.x, prev.y, p.x, p.y, 0.5f * thick * (0.4f + (0.6f * t)), Color.white);
                prev = p;
            }
        }
    }
}
