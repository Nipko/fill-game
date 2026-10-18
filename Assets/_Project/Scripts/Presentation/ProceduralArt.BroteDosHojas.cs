#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] del Brote de dos hojas (variante del Brote de la 1.0, estreno en el 343; 02 §9.1 «Variantes» y R10 b;
    /// propuesta de D-007 b.3: «Brote con dos hojas cerradas; cada dosis abre una»; tarea VA1b). En el tablero reutiliza el
    /// capullo, el cáliz y la hojita del Brote (<see cref="BroteCork"/>, <see cref="BroteSepals"/> y <see cref="BroteSprout"/>):
    /// lo propio es la pose de las dos hojas grandes que lo envuelven por delante (<c>BroteOverlay</c>). Aquí va el icono grande
    /// del estreno (<see cref="CardHeroIcon"/> con <see cref="BroteTwoLeavesIconKey"/>): el capullo del Brote con una hoja grande
    /// cerrada sobre su costado izquierdo y la otra ya abierta a la derecha, para que el icono cuente la regla (cada dosis abre
    /// una) y no se confunda con el del Brote, que solo lleva el capullo y su cáliz. Sin tintes de fruta (07 §3, regla 6). El
    /// nombre «Brote de dos hojas» está pendiente del asesor cultural (glosario §6.5).
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>
        /// Clave del icono grande del Brote de dos hojas (<c>intro_mechanic</c> <c>brote_n2</c> [P] del 343; VA1b): no es un tipo
        /// de mecánica propio (es el Brote con <c>n</c> = 2), así que <see cref="CardHeroIcon"/> lo dibuja aparte.
        /// </summary>
        public const string BroteTwoLeavesIconKey = "brote_n2";

        // Brote de dos hojas en un lienzo cuadrado de s px (k = s / 96; y hacia arriba): el capullo y el cáliz del icono del Brote,
        // en verde más claro que las hojas para que se lea, con la hoja izquierda cerrada sobre su costado (inclinada hacia el
        // eje) y la derecha abierta hacia fuera, las dos con contorno de vena.
        private static void DrawBroteTwoLeavesGlyph(PixelCanvas pc, int s)
        {
            float k = s / 96f;
            DrawBud(pc, 48f * k, 20f * k, 66f * k, 24f * k, Palette.LeafLight, Palette.LeafMid, Palette.Vein); // más claro que las hojas: se lee el capullo
            DrawCalyx(pc, 48f * k, 10f * k, 22f * k, 18f * k, Palette.LeafDark);

            // Hoja cerrada: nace a la izquierda del pie y sube pegada al costado del capullo, sin taparle la punta.
            var closedBase = new Vector2(37f * k, 12f * k);
            Vector2 closedDir = new Vector2(0.20f, 1f).normalized;
            DrawLeafShape(pc, closedBase - (closedDir * (2f * k)), closedDir, 48f * k, 21f * k, Palette.Vein, 1f);
            DrawLeaf(pc, closedBase, closedDir, 44f * k, 16f * k, 0.04f, LeafShape.Ovate, Palette.LeafDark, Palette.JungleMid, 1.2f * k, 343);

            // Hoja abierta: nace a la derecha del pie y se abre hacia fuera.
            var openBase = new Vector2(59f * k, 12f * k);
            Vector2 openDir = new Vector2(0.90f, 0.44f).normalized;
            DrawLeafShape(pc, openBase - (openDir * (2f * k)), openDir, 38f * k, 21f * k, Palette.Vein, 1f);
            DrawLeaf(pc, openBase, openDir, 34f * k, 16f * k, -0.06f, LeafShape.Ovate, Palette.LeafDark, Palette.JungleMid, 1.2f * k, 344);
        }
    }
}
