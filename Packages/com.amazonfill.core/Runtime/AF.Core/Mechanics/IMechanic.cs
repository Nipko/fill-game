#nullable enable
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// Ganchos de una mecánica de cuia (GDD 02 §9; 08 §2.2: mecánicas como <c>IMechanic</c> con <c>OnBeforePour</c> y
    /// <c>OnAfterPour</c>). <c>OnBeforePour</c> son los ganchos que deciden el vertido (origen, destino, cantidad y dónde
    /// entran las dosis); <c>OnAfterPour</c>, los de las etapas (b), (d), (e) y (f) de R10 en el orden del anexo B
    /// (D-007, Q13). Los llama solo <see cref="BoardRules"/>, la única que aplica vertidos.
    /// </summary>
    /// <remarks>
    /// <para>Las implementaciones no tienen estado: todo vive en la cuia (<see cref="Cuia.Packed"/>), así que el estado
    /// canónico (03 §3) basta para el solver y para Marea. Hay una instancia por tipo en una tabla estática indexada por el
    /// código de mecánica de la cuia (<see cref="MechanicTable"/>): nada de <c>new</c> por nodo ni <c>foreach</c> sobre
    /// interfaces (regla dura 2).</para>
    /// <para>Principio de 02 R16 para D-007: cuando una mecánica se abre o se suelta, la cuia pasa a no tener mecánica
    /// (código 0), así que una cuia que ya es normal tiene el mismo <see cref="Cuia.Packed"/> que cualquier cuia normal.</para>
    /// <para>Bucle caliente: sin asignaciones, sin validar argumentos. Solo se llama a los ganchos de cuias con mecánica.</para>
    /// </remarks>
    internal interface IMechanic
    {
        /// <summary><c>bloqueada_origen</c> de 02 §1.2: la cuia no puede ser origen mientras tenga esta mecánica.</summary>
        bool BlocksSource(Cuia self);

        /// <summary>
        /// Filtro de destino de 02 §1.2 (<c>bloqueada_destino</c>, Hoja, Remolino…), evaluado cuando el origen ya es válido
        /// y el destino no está sellado. <see cref="TargetVerdict.Normal"/> deja la regla normal (no llena; vacía o con la
        /// misma cima).
        /// </summary>
        TargetVerdict CheckTarget(in BoardSlice board, int source, int target);

        /// <summary><c>espacio_efectivo(B)</c> de 02 §1.2 para la cantidad en <see cref="PourMode.Run"/>.</summary>
        int ReceiveSpace(in BoardSlice board, int target);

        /// <summary>Cuia que recibirá las dosis de un vertido a <paramref name="target"/> (B; la pareja en el Remolino).</summary>
        int ReceiverOf(in BoardSlice board, int target);

        /// <summary>
        /// Transferencia: pone <paramref name="amount"/> dosis de <paramref name="color"/> donde las recibe el destino
        /// (la cima de B por defecto; el fondo de la pareja en el Remolino, 02 §9.1 M4) y devuelve el índice de la cuia
        /// que las recibió.
        /// </summary>
        int Receive(in BoardSlice board, int target, int color, int amount);

        /// <summary><c>cambia_al_recibir(B)</c> del anexo B: verter en esta cuia nunca es fútil.</summary>
        bool ChangesOnReceive(Cuia self);

        /// <summary>
        /// <c>no_puede_recibir(A)</c> del anexo B y exención de origen (respuesta Q-B del dueño): un vertido desde esta cuia
        /// nunca es fútil.
        /// </summary>
        bool ExemptAsSource(Cuia self);

        /// <summary>R10 (b): apertura por recepción. Devuelve la cuia tras recibir <paramref name="color"/> (sin mecánica si se abrió).</summary>
        Cuia OnReceived(Cuia self, int color);

        /// <summary>R10 (d): la mecánica se suelta por el sellado de otras cuias (se deduce del estado).</summary>
        bool ReleasedBySeal(in BoardSlice board, int index);

        /// <summary>R10 (e): avanza su contador una vez por vertido útil; devuelve true si cambió alguna cuia.</summary>
        bool AdvanceCounter(in BoardSlice board, int index, ref PourEvents events);

        /// <summary>R10 (f): condición del tablero (llave en la cima, atraque…); devuelve true si cambió alguna cuia.</summary>
        bool CheckCondition(in BoardSlice board, int index, ref PourEvents events);

        /// <summary>R12 (g): la cuia impide la victoria aunque esté vacía o sellada (Lluvia con gotas pendientes).</summary>
        bool BlocksVictory(Cuia self);

        /// <summary>Reglas de validación propias de la mecánica en el tablero inicial (03 §17 #6); null si es válida.</summary>
        string? ValidateInitial(BoardState state, int index);
    }

    /// <summary>Veredicto de <see cref="IMechanic.CheckTarget"/>.</summary>
    internal enum TargetVerdict
    {
        /// <summary>Se aplica la regla normal de 02 §1.2: B no llena y vacía o con la misma cima que A.</summary>
        Normal = 0,

        /// <summary>El vertido es legal sin mirar la regla normal (Remolino, Barro expuesto…).</summary>
        Accept = 1,

        /// <summary>El vertido es ilegal (R11).</summary>
        Reject = 2,
    }
}
