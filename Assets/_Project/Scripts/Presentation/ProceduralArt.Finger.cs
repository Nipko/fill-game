#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] del dedo guía del tutorial (tarea DC6; D-028, pregunta 61 (a) «con P5»; 02 §13, filas 1 y 2): la mano
    /// que señala con el índice y la punta de la flecha de Reducir movimiento. Gráfico provisional por código, como el resto,
    /// hasta que lo sustituya el artista 2D. Perezoso: solo se genera al enseñar el dedo por primera vez (niveles 1 y 2).
    /// </summary>
    /// <remarks>
    /// La mano va en papel de la UI (<see cref="Palette.UiPaper"/>) con contorno de tinta (<see cref="Palette.UiInk"/>) y una
    /// sombra suave debajo, para que se lea sobre el vidrio, el agua y el panel del tablero (D-003, luz de juego). No es fauna ni
    /// lleva texto. Las medidas van en dp (3 px/dp, <see cref="UiPxPerDp"/>).
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho del sprite de la mano (dp) [P].</summary>
        public const float FingerHandWidthDp = 42f;

        /// <summary>Alto del sprite de la mano (dp) [P].</summary>
        public const float FingerHandHeightDp = 56f;

        /// <summary>Ancho del sprite de la punta de la flecha (dp) [P].</summary>
        public const float FingerArrowHeadWidthDp = 18f;

        /// <summary>Alto del sprite de la punta de la flecha (dp) [P].</summary>
        public const float FingerArrowHeadHeightDp = 20f;

        // Lienzo de la mano en px (y hacia arriba) y la yema del índice, el punto que toca: el pivote del sprite.
        private const int FingerHandPxW = 108; // 36 × 48 dp a 3 px/dp; se pinta a FingerHandWidthDp (un poco mayor, revisión de las capturas de DC6)
        private const int FingerHandPxH = 144;
        private const float FingerTipPxX = 40f;
        private const float FingerTipPxY = 128f;

        // Grosor del contorno de tinta y desplazamiento de la sombra (px).
        private const float FingerOutlinePx = 4f;
        private const float FingerShadowDxPx = 4f;
        private const float FingerShadowDyPx = -5f;
        private const float FingerShadowAlpha = 0.24f;

        /// <summary>Pliegues de los dedos doblados y uña (#D9C8AC) [P].</summary>
        public static readonly Color FingerCrease = Palette.Hex(0xD9C8AC);

        /// <summary>
        /// La yema del índice en el sprite de la mano, normalizada (0-1, desde abajo a la izquierda): el pivote del RectTransform
        /// que la coloca, para que la posición de la mano sea el punto que toca.
        /// </summary>
        public static Vector2 FingerTipPivot => new Vector2(FingerTipPxX / FingerHandPxW, FingerTipPxY / FingerHandPxH);

        private Sprite? _fingerHand;
        private Sprite? _fingerArrowHead;

        /// <summary>
        /// La mano del dedo guía (<see cref="FingerHandWidthDp"/> × <see cref="FingerHandHeightDp"/> dp): el índice estirado hacia
        /// arriba, los otros tres doblados y el pulgar; pivote en la yema (<see cref="FingerTipPivot"/>).
        /// </summary>
        public Sprite FingerHand => _fingerHand != null ? _fingerHand : (_fingerHand = BuildFingerHand());

        /// <summary>
        /// La punta de la flecha de Reducir movimiento (<see cref="FingerArrowHeadWidthDp"/> × <see cref="FingerArrowHeadHeightDp"/>
        /// dp), apuntando a la derecha; pivote en la punta.
        /// </summary>
        public Sprite FingerArrowHead => _fingerArrowHead != null ? _fingerArrowHead : (_fingerArrowHead = BuildFingerArrowHead());

        private Sprite BuildFingerHand()
        {
            var shadow = new PixelCanvas(FingerHandPxW, FingerHandPxH);
            DrawFingerHandShape(shadow, FingerShadowDxPx, FingerShadowDyPx, FingerOutlinePx, Color.black);
            var pc = new PixelCanvas(FingerHandPxW, FingerHandPxH);
            var shade = new Color(0f, 0f, 0f, 1f);
            for (int y = 0; y < FingerHandPxH; y++)
            {
                for (int x = 0; x < FingerHandPxW; x++)
                {
                    float a = shadow.Get(x, y).a;
                    if (a > 0f)
                    {
                        pc.Blend(x, y, shade, a * FingerShadowAlpha);
                    }
                }
            }

            DrawFingerHandShape(pc, 0f, 0f, FingerOutlinePx, Palette.UiInk);
            DrawFingerHandShape(pc, 0f, 0f, 0f, Palette.UiPaper);

            // Pliegues entre los dedos doblados y la uña del índice (se ve el dorso de la mano).
            pc.FillCapsule(62f, 65f, 84f, 65f, 1.6f, FingerCrease);
            pc.FillCapsule(62f, 47f, 84f, 47f, 1.6f, FingerCrease);
            pc.FillCapsule(34f, 92f, 34f, 74f, 1.4f, FingerCrease);
            pc.FillRoundedRect(34f, 110f, 46f, 126f, 5f, FingerCrease);
            pc.FillRoundedRect(36f, 113f, 44f, 124f, 3.5f, Palette.UiPaper);
            return MakeSprite(pc, "tutorial_finger_hand", FingerTipPivot, Vector4.zero, UiPxPerDp);
        }

        // La silueta de la mano desplazada (dx, dy) y engordada grow px (el contorno y la sombra).
        private static void DrawFingerHandShape(PixelCanvas pc, float dx, float dy, float grow, Color c)
        {
            pc.FillRoundedRect(26f + dx - grow, 6f + dy - grow, 92f + dx + grow, 84f + dy + grow, 20f + grow, c); // palma
            pc.FillCapsule(40f + dx, 80f + dy, 40f + dx, 126f + dy, 12f + grow, c); // índice
            pc.FillCapsule(56f + dx, 74f + dy, 82f + dx, 74f + dy, 11f + grow, c); // dedos doblados
            pc.FillCapsule(58f + dx, 56f + dy, 86f + dx, 56f + dy, 11f + grow, c);
            pc.FillCapsule(58f + dx, 38f + dy, 84f + dx, 38f + dy, 10f + grow, c);
            pc.FillCapsule(22f + dx, 48f + dy, 34f + dx, 70f + dy, 10f + grow, c); // pulgar
        }

        private Sprite BuildFingerArrowHead()
        {
            const int w = 54;
            const int h = 60;
            var pc = new PixelCanvas(w, h);
            pc.FillTriangle(new Vector2(52f, 30f), new Vector2(2f, 2f), new Vector2(2f, 58f), Palette.UiInk);
            pc.FillTriangle(new Vector2(44f, 30f), new Vector2(7f, 10f), new Vector2(7f, 50f), Palette.UiPaper);
            return MakeSprite(pc, "tutorial_finger_arrow_head", new Vector2(52f / w, 0.5f), Vector4.zero, UiPxPerDp);
        }
    }
}
