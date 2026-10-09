#nullable enable
using System;
using AF.Core.Board;

namespace AF.Core.Rules
{
    /// <summary>
    /// Reglas de GDD 02 §1 evaluadas sobre cuias sueltas y la tabla <c>U_c</c>: sellado (R5), <c>legal</c> y
    /// <c>cantidad</c> (R9, con el modo de vertido; D-005: <see cref="PourMode.Run"/> por defecto, la racha que cabe;
    /// <see cref="PourMode.Single"/>, una dosis, solo pruebas) y <c>futil</c> (02 §1.4 con la excepción de Aguas turbias
    /// de 04 §5.1). Solo <c>cantidad</c> depende de <see cref="PourMode"/>.
    /// </summary>
    /// <remarks>
    /// <para>Desde D-007 (Q13) las reglas con contexto de tablero y mecánicas viven en <see cref="BoardRules"/>, la única
    /// implementación que aplica vertidos. Esta clase queda como <b>envoltorio heredado</b> de cuias normales y como capa
    /// base que <see cref="BoardRules"/> usa para la parte sin mecánicas: <see cref="IsSealed"/> (R5) y el núcleo de
    /// <c>futil</c> (<see cref="IsFutile"/>), así que cliente, solver y bot no pueden divergir (04 §5.1; 03 §5: "el solver no
    /// mantiene una poda propia").</para>
    /// <para>Código de bucle caliente: sin asignaciones y sin validar argumentos. <c>unitsByColor</c> es una tabla
    /// indexada por color (0..16, longitud <see cref="UnitsTableLength"/>) con <c>U_c</c>; la posición 0 vale 0.
    /// La condición A ≠ B (índices distintos) la comprueba el llamante.</para>
    /// <para>Sin las ramas de mecánicas de 02 §9 (<c>bloqueada_origen</c>, <c>bloqueada_destino</c>, Remolino, Barro, Hoja,
    /// Lluvia…): para cuias con mecánica usa <see cref="BoardRules"/>.</para>
    /// </remarks>
    public static class CuiaRules
    {
        /// <summary>Longitud de la tabla <c>U_c</c>: colores 0..16 (el 0 no se usa).</summary>
        public const int UnitsTableLength = Cuia.MaxColor + 1;

        /// <summary>
        /// Cuia sellada (R5): llena, monocolor, sin dosis ocultas y con capacidad igual a <c>U_c</c> de su color.
        /// </summary>
        public static bool IsSealed(Cuia cuia, int[] unitsByColor)
        {
            return !cuia.IsEmpty
                && cuia.IsFull
                && !cuia.HasHidden
                && cuia.IsMonocolor
                && cuia.Capacity == unitsByColor[cuia.TopColor];
        }

        /// <summary>
        /// La cuia puede ser origen de un vertido: primera condición de <c>legal(A, B)</c> en 02 §1.2 (A no vacía ni
        /// sellada) y regla de selección C1/C2 de 02 §3.1. Es la única definición: la usan <see cref="IsLegal"/>,
        /// <c>movimientos_utiles</c>, el solver y el control tap-tap del cliente.
        /// </summary>
        /// <remarks>Heredada, sin mecánicas: <c>bloqueada_origen</c> (Hoja, Brote, Matapi y Liana, 02 §9) la añade
        /// <see cref="BoardRules.CanBeSource"/>.</remarks>
        public static bool CanBeSource(Cuia from, int[] unitsByColor)
        {
            return !from.IsEmpty && !IsSealed(from, unitsByColor);
        }

        /// <summary>
        /// <c>legal(A, B)</c> de 02 §1.2 sin mecánicas: A puede ser origen (<see cref="CanBeSource"/>); B no sellada
        /// ni llena; y B vacía o con la misma cima que A. El llamante garantiza que A y B son cuias distintas.
        /// </summary>
        public static bool IsLegal(Cuia from, Cuia to, int[] unitsByColor)
        {
            if (!CanBeSource(from, unitsByColor))
            {
                return false;
            }

            if (IsSealed(to, unitsByColor) || to.IsFull)
            {
                return false;
            }

            return to.IsEmpty || to.TopColor == from.TopColor;
        }

        /// <summary>
        /// <c>cantidad(A, B)</c> según el modo de vertido (D-005: <see cref="PourMode.Run"/> por defecto, la racha que
        /// cabe; <see cref="PourMode.Single"/>, una dosis, solo pruebas): <c>min(racha visible de A, espacio de B)</c> con
        /// <see cref="PourMode.Run"/> (R6, R9; 02 §1.2) y 1 con <see cref="PourMode.Single"/>. Es el <b>único</b> sitio
        /// donde se aplica el modo: tablero, partida, solver y bot llaman aquí.
        /// Precondición: <see cref="IsLegal"/> es verdadero (A no vacía y B con hueco, así que 1 siempre cabe); si no, el
        /// valor no tiene sentido. Bucle caliente: no valida <paramref name="mode"/> (cualquier valor distinto de
        /// <see cref="PourMode.Run"/> se trata como <see cref="PourMode.Single"/>).
        /// </summary>
        public static int Amount(Cuia from, Cuia to, PourMode mode)
        {
            return mode == PourMode.Run ? Math.Min(from.TopRunLength, to.Space) : 1;
        }

        /// <summary>
        /// Heredada: <c>cantidad(A, B) = min(racha visible de A, espacio de B)</c> (R6, R9), igual que
        /// <see cref="Amount(Cuia, Cuia, PourMode)"/> con <see cref="PourMode.Run"/>. Se conserva sin cambio de
        /// comportamiento para que <c>tools/</c> y Unity sigan compilando hasta que la retire T6 (decisiones.md §3).
        /// </summary>
        public static int Amount(Cuia from, Cuia to)
        {
            return Amount(from, to, PourMode.Run);
        }

        /// <summary>
        /// Núcleo de <c>futil(A, B)</c> de 02 §1.4 con la excepción de 04 §5.1: verter una cuia monocolor, no sellada y sin
        /// dosis ocultas en una vacía solo recoloca el problema, salvo en la rama "casa correcta"
        /// (<c>cap_A ≠ U_c</c> y <c>cap_B = U_c</c>). Una A vacía nunca es fútil (no hay vertido). Las exenciones de
        /// mecánica del anexo B (<c>cambia_al_recibir(B)</c>, <c>no_puede_recibir(A)</c>, Q-B) las añade
        /// <see cref="BoardRules.IsFutile"/>, que es la regla completa.
        /// </summary>
        public static bool IsFutile(Cuia from, Cuia to, int[] unitsByColor)
        {
            if (from.IsEmpty || !to.IsEmpty)
            {
                return false;
            }

            // 04 §5.1: con dosis ocultas el jugador no sabe si es monocolor; verter revela información.
            if (from.HasHidden)
            {
                return false;
            }

            if (!from.IsMonocolor || IsSealed(from, unitsByColor))
            {
                return false;
            }

            int units = unitsByColor[from.TopColor];
            bool rightHomeTarget = to.Capacity == units;
            bool wrongHomeSource = from.Capacity != units;
            if (wrongHomeSource && rightHomeTarget)
            {
                // Llevar la fruta a la cuia donde puede sellar sí progresa (casa correcta).
                return false;
            }

            // Los destinos que cambian al recibir (Hoja, Matapi, Remolino, Lluvia…) los exime BoardRules (anexo B).
            return true;
        }
    }
}
