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

        /// <summary>Diámetro exterior del anillo de la onda del toque al acabar, con su contorno (dp) [P].</summary>
        public const float FingerRippleDp = 42f;

        /// <summary>Diámetro exterior del anillo de la onda del toque al empezar, con su contorno (dp) [P].</summary>
        public const float FingerRippleStartDp = 20f;

        /// <summary>
        /// Anillos de la onda pintados, de <see cref="FingerRippleStartDp"/> a <see cref="FingerRippleDp"/> dp a pasos iguales
        /// (revisión de DC6): cada uno con su trazo de <see cref="FingerRippleStrokeDp"/> dp, para que el trazo no adelgace al
        /// escalar el sprite (con el más cercano, ±10 % como mucho) [P].
        /// </summary>
        public const int FingerRippleSteps = 7;

        /// <summary>Ancho del halo cálido debajo del anillo de la onda (dp) [P].</summary>
        public const float FingerRippleHaloDp = 6f;

        /// <summary>Margen del lienzo de cada anillo de la onda, por lado, para el halo (dp) [P].</summary>
        private const float FingerRippleMarginDp = 1f;

        /// <summary>Trazo del anillo de papel de la onda (dp) [P].</summary>
        public const float FingerRippleStrokeDp = 3f;

        /// <summary>Contorno de tinta del anillo de la onda, por lado (dp) [P].</summary>
        public const float FingerRippleOutlineDp = 1f;

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

        /// <summary>Halo cálido de la onda del toque, la luz de juego de D-003 (#FFE9A8) [P].</summary>
        public static readonly Color FingerRippleHalo = Palette.Hex(0xFFE9A8);

        /// <summary>
        /// La yema del índice en el sprite de la mano, normalizada (0-1, desde abajo a la izquierda): el pivote del RectTransform
        /// que la coloca, para que la posición de la mano sea el punto que toca.
        /// </summary>
        public static Vector2 FingerTipPivot => new Vector2(FingerTipPxX / FingerHandPxW, FingerTipPxY / FingerHandPxH);

        private Sprite? _fingerHand;
        private Sprite? _fingerArrowHead;
        private readonly Sprite?[] _fingerRipple = new Sprite?[FingerRippleSteps];
        private readonly Sprite?[] _fingerRippleOutline = new Sprite?[FingerRippleSteps];
        private readonly Sprite?[] _fingerRippleHalo = new Sprite?[FingerRippleSteps];

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

        /// <summary>Diámetro exterior (con el contorno) del anillo <paramref name="step"/> de la onda (dp).</summary>
        public static float FingerRippleStepDp(int step)
        {
            return Mathf.Lerp(FingerRippleStartDp, FingerRippleDp, step / (float)(FingerRippleSteps - 1));
        }

        /// <summary>Lado del lienzo del anillo <paramref name="step"/> de la onda, con el halo y su margen (dp).</summary>
        public static float FingerRippleCanvasDp(int step)
        {
            return Mathf.Round((FingerRippleStepDp(step) + (2f * FingerRippleMarginDp)) * UiPxPerDp) / UiPxPerDp; // px enteros
        }

        /// <summary>El anillo de la onda pintado más cercano a <paramref name="diameter"/> dp.</summary>
        public static int FingerRippleNearestStep(float diameter)
        {
            float k = (diameter - FingerRippleStartDp) / (FingerRippleDp - FingerRippleStartDp) * (FingerRippleSteps - 1);
            return Mathf.Clamp(Mathf.RoundToInt(k), 0, FingerRippleSteps - 1);
        }

        /// <summary>
        /// El anillo de papel <paramref name="step"/> de la onda del toque (<see cref="FingerRippleStepDp"/> dp, trazo de
        /// <see cref="FingerRippleStrokeDp"/> dp), en blanco para teñirlo (<see cref="Palette.UiPaper"/>); pivote en el centro y
        /// lienzo de <see cref="FingerRippleCanvasDp"/> dp. Un anillo y no un disco, para que se lea sobre el açaí y sobre el
        /// cuerpo oscuro de la cuia vacía (D-003, luz de juego).
        /// </summary>
        public Sprite FingerRippleAt(int step)
        {
            Sprite? sprite = _fingerRipple[step];
            return sprite != null ? sprite
                : (_fingerRipple[step] = BuildFingerRing("tutorial_finger_ripple_" + step, step, FingerRippleStrokeDp));
        }

        /// <summary>
        /// El contorno de tinta del anillo <paramref name="step"/>: el mismo anillo, <see cref="FingerRippleOutlineDp"/> dp más
        /// ancho por lado, en blanco para teñirlo (<see cref="Palette.UiInk"/>), que va debajo del de papel.
        /// </summary>
        public Sprite FingerRippleOutlineAt(int step)
        {
            Sprite? sprite = _fingerRippleOutline[step];
            return sprite != null ? sprite : (_fingerRippleOutline[step] = BuildFingerRing("tutorial_finger_ripple_outline_" + step, step,
                FingerRippleStrokeDp + (2f * FingerRippleOutlineDp)));
        }

        /// <summary>
        /// El halo cálido del anillo <paramref name="step"/>: el mismo anillo, de <see cref="FingerRippleHaloDp"/> dp, en blanco
        /// para teñirlo (<see cref="FingerRippleHalo"/>), que va debajo del contorno.
        /// </summary>
        public Sprite FingerRippleHaloAt(int step)
        {
            Sprite? sprite = _fingerRippleHalo[step];
            return sprite != null ? sprite
                : (_fingerRippleHalo[step] = BuildFingerRing("tutorial_finger_ripple_halo_" + step, step, FingerRippleHaloDp));
        }

        private Sprite BuildFingerRing(string name, int step, float strokeDp)
        {
            int size = Mathf.RoundToInt(FingerRippleCanvasDp(step) * UiPxPerDp);
            var pc = new PixelCanvas(size, size);
            float c = size * 0.5f;
            float outer = FingerRippleStrokeDp + (2f * FingerRippleOutlineDp);
            float radius = ((FingerRippleStepDp(step) - outer) * 0.5f) * UiPxPerDp;
            pc.StrokeArc(c, c, radius, strokeDp * UiPxPerDp, 0f, 360f, Color.white);
            return MakeSprite(pc, name, new Vector2(0.5f, 0.5f), Vector4.zero, UiPxPerDp);
        }

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
