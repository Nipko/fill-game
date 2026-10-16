#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Hoja atada (variante de la Hoja de la 1.0, estreno en el 283; 02 §9.1 «Variantes» y §1.2;
    /// propuesta de D-007 b.3: «hoja pequeña atada al cuello con fibra, con icono grande; nunca cae»; tarea VA1a). En el tablero
    /// no tiene sprites propios: el overlay (<c>HojaOverlay</c> en modo atado) reutiliza la hoja de palma de la Hoja
    /// (<see cref="HojaPalm"/>), más pequeña y colgada del cuello; su medallón y la fruta llave (<see cref="HojaBadge"/> y
    /// <see cref="FruitFlight"/>); y la vuelta del cuello con su nudo de la Liana (<see cref="LianaCollar"/>), teñida de fibra
    /// (<see cref="HojaCordTint"/>) en lugar de verde, para que se lea como un cordel y no como una liana que bloquea.
    /// Aquí van ese tinte y el icono de la mecánica (<see cref="MechanicIcon"/> de 32 dp y el grande del estreno,
    /// <see cref="CardHeroIcon"/>): la hoja de palma pequeña que cuelga, en diagonal, de un lazo de fibra con su nudo.
    /// </summary>
    /// <remarks>
    /// Color (direccion-visual-v2 §2.4, regla 1): la hoja usa los colores de la Hoja (a ΔE00 ≥ 10 de los 14 tintes) y el cordel,
    /// tonos <c>fibra_*</c>, que la regla deja fuera. Lo sustituye el arte del artista; el nombre «Hoja atada» está pendiente del
    /// asesor cultural (glosario §6.5).
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>
        /// Tinte de la vuelta del cuello (<see cref="LianaCollar"/>, en grises) para el cordel de fibra de la Hoja atada [P]:
        /// <see cref="Palette.FiberBase"/>, así el tallo (×0,88) queda en fibra media y el nudo (×0,70), más oscuro; más oscuro
        /// que el borde de fibra trenzado de la cuia, para no confundirse con él.
        /// </summary>
        public static readonly Color HojaCordTint = Palette.FiberBase;

        /// <summary>Luz de borde del cordel de la Hoja atada (sobre <see cref="LianaCollarShine"/>) [P].</summary>
        public static readonly Color HojaCordShine = ColorMath.WithAlpha(Palette.FiberLight, 0.85f);

        // Icono de la Hoja atada en un lienzo cuadrado de s px (k = s / 96; y hacia arriba): un lazo de fibra arriba a la izquierda
        // (el cuello), su nudo y, colgando en diagonal hacia abajo a la derecha, la hoja de palma pequeña con su nervio. Sin tintes
        // de fruta (07 §3, regla 6): la fruta llave va en el tablero, no en el icono.
        private static void DrawHojaAtadaGlyph(PixelCanvas pc, int s)
        {
            float k = s / 96f;

            // La hoja cuelga del nudo: de (40, 60) a (90, 12), con la panza hacia arriba a la derecha.
            DrawPalmLeaf(pc, new Vector2(40f * k, 60f * k), new Vector2(80f * k, 58f * k), new Vector2(90f * k, 12f * k), 15f * k,
                Palette.JungleMid, Palette.LeafDark, Palette.Vein, 0.8f * k);

            // El lazo del cuello (detrás, la mitad de atrás en sombra) y el nudo con los dos cabos.
            StrokeEllipse(pc, 28f * k, 72f * k, 20f * k, 11f * k, 5.5f * k, Palette.FiberShadow, false);
            StrokeEllipse(pc, 28f * k, 72f * k, 20f * k, 11f * k, 5.5f * k, Palette.FiberBase, true);
            pc.FillCapsule(40f * k, 64f * k, 34f * k, 44f * k, 2.6f * k, Palette.FiberShadow);
            pc.FillCapsule(40f * k, 64f * k, 26f * k, 48f * k, 2.6f * k, Palette.FiberBase);
            pc.FillCircle(41f * k, 64f * k, 7f * k, Palette.FiberShadow);
            pc.FillCircle(41f * k, 65f * k, 5.2f * k, Palette.FiberBase);
            pc.FillCircle(39.5f * k, 66.5f * k, 2f * k, Palette.FiberLight);
        }
    }
}
