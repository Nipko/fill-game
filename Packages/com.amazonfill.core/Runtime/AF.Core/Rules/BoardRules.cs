#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Session;

namespace AF.Core.Rules
{
    /// <summary>
    /// Reglas de GDD 02 §1 con contexto de tablero (D-007, Q13): origen, <c>legal</c>, <c>cantidad</c>, <c>futil</c> del
    /// anexo B, aplicación de un vertido con R10 por etapas en el orden del anexo B, victoria (R12) y bloqueo (R13). Es la
    /// <b>única</b> implementación que aplica vertidos: <see cref="PourRules"/>, <see cref="BoardState"/>,
    /// <see cref="Session.LevelSession"/>, el solver, el bot y los generadores delegan aquí (04 §5.1; 03 §5: «el solver no
    /// mantiene una poda propia»).
    /// </summary>
    /// <remarks>
    /// <para>Trabaja sobre <c>count</c> cuias de un array desde <c>offset</c> y la tabla <c>U_c</c> indexada por color
    /// (0..16, <see cref="CuiaRules.UnitsTableLength"/>); los índices de cuia son relativos (0 a <c>count</c> − 1). Bucle
    /// caliente: sin asignaciones y sin validar argumentos (los valida <see cref="PourRules"/>). Las mecánicas
    /// (<see cref="IMechanic"/>) se despachan por la tabla estática de <see cref="MechanicTable"/> solo para cuias que llevan
    /// una: un tablero sin mecánicas sigue la ruta de 02 §1 sin llamadas virtuales.</para>
    /// <para><b>Barro</b> (02 §9.1 M5): no es una mecánica de cuia sino el color 15 dentro del contenido, así que lo tratan
    /// estas rutas sin despacho: con Barro en la cima la cuia no es origen; como destino acepta cualquier color
    /// (<c>barro_expuesto</c>, tras los filtros de mecánica, como en el pseudocódigo de 02 §1.2), con
    /// <c>espacio_efectivo + 1</c>; el vertido lo lava (<see cref="PourEvents.BarroWashedMask"/>); una cuia con Barro no
    /// está sellada ni vacía (R5, R12).</para>
    /// <para><b>R10 (anexo B).</b> Tras cada vertido: (a) revelado de la nueva cima de A (R7); (b) aperturas por recepción
    /// en B (Hoja, Brote); (c) sellado (R5); (d) liberaciones por sellado (Liana); (e) contadores (Lluvia, Piraña), una sola
    /// vez por vertido y solo si el vertido no es fútil (respuesta Q18 B); (f) condiciones del tablero; (f′) se repiten
    /// (c), (d) y (f) hasta que nada cambie; (g) victoria (R12); (h) bloqueo (R13-R15). El orden es contrato con el solver y
    /// con Marea, que restaura el estado anterior entero.</para>
    /// <para><b><c>futil</c> (anexo B).</b> No es fútil si <c>cambia_al_recibir(B)</c> (Hoja puesta, Brote cerrado,
    /// Matapi, Remolino, Lluvia con gotas pendientes…) ni si el origen está exento (<c>no_puede_recibir(A)</c> y la respuesta
    /// Q-B: cuia de Remolino o Lluvia con gotas pendientes); en lo demás, la regla de 02 §1.4 con la excepción de Aguas
    /// turbias de 04 §5.1 (<see cref="CuiaRules.IsFutile"/>).</para>
    /// </remarks>
    public static class BoardRules
    {
        /// <summary>Tope de vueltas del punto fijo (f′): cada vuelta que cambia algo suelta o abre al menos una mecánica.</summary>
        private const int MaxFixedPointPasses = 32;

        /// <summary>
        /// La cuia <paramref name="index"/> puede ser origen: no vacía, no sellada y sin <c>bloqueada_origen</c>
        /// (02 §1.2; C1-C2 de 02 §3.1).
        /// </summary>
        public static bool CanBeSource(Cuia[] cuias, int offset, int count, int[] units, int index)
        {
            return IsSourceCuia(cuias[offset + index], units);
        }

        /// <summary>
        /// <c>legal(A, B)</c> de 02 §1.2: A ≠ B; A puede ser origen; B no sellada ni <c>bloqueada_destino</c>; los filtros
        /// de la mecánica de B; y la regla normal (B no llena y vacía o con la misma cima que A).
        /// </summary>
        public static bool IsLegal(Cuia[] cuias, int offset, int count, int[] units, int source, int target)
        {
            if (source == target || !IsSourceCuia(cuias[offset + source], units))
            {
                return false;
            }

            var board = new BoardSlice(cuias, offset, count, units);
            return Accepts(in board, source, target);
        }

        /// <summary>
        /// <c>cantidad(A, B)</c> (R9): 1 en <see cref="PourMode.Single"/>; en <see cref="PourMode.Run"/>,
        /// <c>min(racha visible de A, espacio_efectivo(B))</c>. Precondición: <see cref="IsLegal"/>.
        /// </summary>
        public static int Amount(Cuia[] cuias, int offset, int count, int[] units, int source, int target, PourMode mode)
        {
            if (mode != PourMode.Run)
            {
                return 1;
            }

            var board = new BoardSlice(cuias, offset, count, units);
            return RunAmount(in board, source, target);
        }

        /// <summary>
        /// <c>futil(A, B)</c> del anexo B (02 §1.4 y su copia en 04 §5.1). Un vertido fútil sigue siendo legal (R13); no
        /// cuenta para evitar el bloqueo ni avanza contadores (Q18 B). El llamante garantiza A ≠ B.
        /// </summary>
        public static bool IsFutile(Cuia[] cuias, int offset, int count, int[] units, int source, int target)
        {
            return IsFutileCuias(cuias[offset + source], cuias[offset + target], units);
        }

        /// <summary>
        /// Aplica el vertido legal A→B sobre el array y evalúa R10 (a)-(f′) en el orden del anexo B. La victoria (g) y el
        /// bloqueo (h) los evalúa la sobrecarga con <c>futileRuleEnabled</c> o <see cref="EvaluateStatus"/>; el solver
        /// mira (g) al sacar el nodo y nunca necesita (h).
        /// </summary>
        /// <param name="isFutile">Resultado de <see cref="IsFutile"/> para este vertido (Q18 B: un fútil no avanza contadores).</param>
        public static void ApplyInPlace(Cuia[] cuias, int offset, int count, int[] units, int source, int target, PourMode mode, bool isFutile, ref PourEvents events)
        {
            var board = new BoardSlice(cuias, offset, count, units);
            Apply(in board, source, target, mode, isFutile, ref events);
        }

        /// <summary>
        /// Como <see cref="ApplyInPlace(Cuia[], int, int, int[], int, int, PourMode, bool, ref PourEvents)"/> y además (g)
        /// victoria y, si no, (h) bloqueo con el kill-switch <paramref name="futileRuleEnabled"/> (08 §9): R10 completo.
        /// </summary>
        public static void ApplyInPlace(Cuia[] cuias, int offset, int count, int[] units, int source, int target, PourMode mode, bool isFutile, bool futileRuleEnabled, ref PourEvents events)
        {
            var board = new BoardSlice(cuias, offset, count, units);
            Apply(in board, source, target, mode, isFutile, ref events);
            events.Finish(Status(in board, futileRuleEnabled));
        }

        /// <summary>R10 (g)-(h): victoria (R12) y, si no, Aguas estancadas (R13) o partida en curso.</summary>
        public static LevelStatus EvaluateStatus(Cuia[] cuias, int offset, int count, int[] units, bool futileRuleEnabled)
        {
            var board = new BoardSlice(cuias, offset, count, units);
            return Status(in board, futileRuleEnabled);
        }

        /// <summary>Victoria (R12): toda cuia está vacía o sellada y ninguna mecánica la impide.</summary>
        public static bool IsWon(Cuia[] cuias, int offset, int count, int[] units)
        {
            var board = new BoardSlice(cuias, offset, count, units);
            return IsWon(in board);
        }

        /// <summary><c>movimientos_utiles(S) ≠ ∅</c> (02 §1.4); con el kill-switch apagado, existe un vertido legal.</summary>
        public static bool HasUsefulMove(Cuia[] cuias, int offset, int count, int[] units, bool futileRuleEnabled)
        {
            var board = new BoardSlice(cuias, offset, count, units);
            return HasUsefulMove(in board, futileRuleEnabled);
        }

        /// <summary>Aguas estancadas (R13): no es victoria y no queda ningún vertido útil.</summary>
        public static bool IsStuck(Cuia[] cuias, int offset, int count, int[] units, bool futileRuleEnabled)
        {
            return EvaluateStatus(cuias, offset, count, units, futileRuleEnabled) == LevelStatus.Stuck;
        }

        // ------------------------------------------------------------------------------------------------------------
        // Rutas internas del bucle caliente (también las usa AF.Solver).
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>Origen posible: no vacía, sin Barro en la cima (02 §9.1 M5), no sellada (R5) y sin <c>bloqueada_origen</c>.</summary>
        internal static bool IsSourceCuia(Cuia cuia, int[] units)
        {
            if (cuia.IsEmpty || cuia.MudExposed || CuiaRules.IsSealed(cuia, units))
            {
                return false;
            }

            return !cuia.HasMechanic || !MechanicTable.Of(cuia).BlocksSource(cuia);
        }

        /// <summary>Parte de <c>legal(A, B)</c> que mira el destino; el llamante ya comprobó que A es origen y A ≠ B.</summary>
        internal static bool Accepts(in BoardSlice board, int source, int target)
        {
            Cuia to = board[target];
            if (to.HasMechanic)
            {
                if (board.IsSealed(target))
                {
                    return false;
                }

                TargetVerdict verdict = MechanicTable.Of(to).CheckTarget(in board, source, target);
                if (verdict != TargetVerdict.Normal)
                {
                    return verdict == TargetVerdict.Accept;
                }
            }

            int top = to.TopColor;
            if (top == Cuia.MudColor)
            {
                return true; // barro_expuesto(B): acepta cualquier color (02 §1.2; §9.1 M5); nunca está sellada
            }

            // Regla normal: una cuia sellada está llena, así que "no llena" cubre también "no sellada".
            return !to.IsFull && (top == Cuia.NoColor || top == board[source].TopColor);
        }

        /// <summary>Cuia que recibirá las dosis de un vertido a <paramref name="target"/>: B, o la pareja en el Remolino.</summary>
        internal static int ReceiverOf(in BoardSlice board, int target)
        {
            Cuia to = board[target];
            return to.HasMechanic ? MechanicTable.Of(to).ReceiverOf(in board, target) : target;
        }

        /// <summary>
        /// <c>cantidad</c> en <see cref="PourMode.Run"/>: <c>min(racha visible de A, espacio_efectivo(B))</c>, con + 1 si
        /// <c>barro_expuesto(B)</c> (02 §1.2).
        /// </summary>
        internal static int RunAmount(in BoardSlice board, int source, int target)
        {
            Cuia to = board[target];
            int space = to.HasMechanic ? MechanicTable.Of(to).ReceiveSpace(in board, target) : CuiaRules.EffectiveSpace(to);
            int run = board[source].TopRunLength;
            return run < space ? run : space;
        }

        /// <summary><c>futil(A, B)</c> del anexo B sobre las dos cuias.</summary>
        internal static bool IsFutileCuias(Cuia from, Cuia to, int[] units)
        {
            if (!to.IsEmpty)
            {
                return false; // solo "monocolor → vacía" puede ser fútil
            }

            if (to.HasMechanic && MechanicTable.Of(to).ChangesOnReceive(to))
            {
                return false; // cambia_al_recibir(B)
            }

            if (from.HasMechanic && MechanicTable.Of(from).ExemptAsSource(from))
            {
                return false; // no_puede_recibir(A) y exención de origen (Q-B)
            }

            return CuiaRules.IsFutile(from, to, units);
        }

        /// <summary>Vertido A→B y R10 (a)-(f′). Precondición: <c>legal(A, B)</c>.</summary>
        internal static void Apply(in BoardSlice board, int source, int target, PourMode mode, bool isFutile, ref PourEvents events)
        {
            Cuia from = board[source];
            int amount = mode == PourMode.Run ? RunAmount(in board, source, target) : 1;
            int color = from.TopColor;
            events.Begin(source, target, amount, color, isFutile);

            // Transferencia y (a): RemoveTop deja visible la nueva cima de A (R7).
            Cuia rest = from.RemoveTop(amount);
            board.Set(source, rest);
            if (!rest.IsEmpty && from.IsHiddenAt(rest.Count - 1))
            {
                events.SourceRevealed = true;
                events.RevealedMask |= 1 << source;
            }

            Cuia to = board[target];
            int receiver = target;
            if (to.HasMechanic)
            {
                receiver = MechanicTable.Of(to).Receive(in board, target, color, amount);
            }
            else if (to.MudExposed)
            {
                // 02 §9.1 M5: el primer vertido lava el barro y ocupa su lugar (evento BarroWashed).
                board.Set(target, to.WashMud(color, amount));
                events.RecordWashed(target);
            }
            else
            {
                board.Set(target, to.AddOnTop(color, amount));
            }

            events.Receiver = receiver;
            events.ChangedMask |= 1 << receiver;

            // (b) Aperturas por recepción: la mecánica que se abre deja la cuia normal (02 R16 para D-007).
            Cuia received = board[receiver];
            if (received.HasMechanic)
            {
                Cuia opened = MechanicTable.Of(received).OnReceived(received, color);
                if (opened.Packed != received.Packed)
                {
                    board.Set(receiver, opened);
                    events.RecordOpened(receiver, received.Mechanic);
                }
            }

            // (c) Sellado de A y de la cuia que recibió.
            MarkSeals(in board, ref events);
            if (!AnyMechanic(in board))
            {
                return; // sin mecánicas no hay (d), (e) ni (f)
            }

            bool changed = ReleaseBySeals(in board, ref events); // (d)
            if (!isFutile)
            {
                changed |= AdvanceCounters(in board, ref events); // (e): una vez por vertido, nunca en un fútil (Q18 B)
            }

            changed |= CheckConditions(in board, ref events); // (f)
            while (changed && events.FixedPointPasses < MaxFixedPointPasses)
            {
                // (f′): lo que un contador o una condición cambió puede sellar o soltar algo en el mismo vertido.
                events.FixedPointPasses++;
                MarkSeals(in board, ref events);
                changed = ReleaseBySeals(in board, ref events);
                changed |= CheckConditions(in board, ref events);
            }
        }

        /// <summary>(g)-(h).</summary>
        internal static LevelStatus Status(in BoardSlice board, bool futileRuleEnabled)
        {
            if (IsWon(in board))
            {
                return LevelStatus.Won;
            }

            return HasUsefulMove(in board, futileRuleEnabled) ? LevelStatus.Playing : LevelStatus.Stuck;
        }

        /// <summary>R12 sobre la vista.</summary>
        internal static bool IsWon(in BoardSlice board)
        {
            int[] units = board.Units;
            for (int i = 0; i < board.Count; i++)
            {
                Cuia cuia = board[i];
                if (cuia.HasMechanic && MechanicTable.Of(cuia).BlocksVictory(cuia))
                {
                    return false;
                }

                if (!cuia.IsEmpty && !CuiaRules.IsSealed(cuia, units))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary><c>movimientos_utiles(S) ≠ ∅</c> sobre la vista.</summary>
        internal static bool HasUsefulMove(in BoardSlice board, bool futileRuleEnabled)
        {
            int[] units = board.Units;
            int count = board.Count;
            for (int a = 0; a < count; a++)
            {
                Cuia from = board[a];
                if (!IsSourceCuia(from, units))
                {
                    continue;
                }

                for (int b = 0; b < count; b++)
                {
                    if (b == a || !Accepts(in board, a, b))
                    {
                        continue;
                    }

                    if (!futileRuleEnabled || !IsFutileCuias(from, board[b], units))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void MarkSeals(in BoardSlice board, ref PourEvents events)
        {
            int changed = events.ChangedMask;
            for (int i = 0; changed != 0; i++, changed >>= 1)
            {
                if ((changed & 1) != 0 && board.IsSealed(i))
                {
                    events.SealedMask |= 1 << i;
                }
            }
        }

        private static bool AnyMechanic(in BoardSlice board)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].HasMechanic)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>(d) Liberaciones por sellado: la cuia pasa a ser normal (02 R16 para D-007).</summary>
        private static bool ReleaseBySeals(in BoardSlice board, ref PourEvents events)
        {
            bool changed = false;
            for (int i = 0; i < board.Count; i++)
            {
                Cuia cuia = board[i];
                if (cuia.HasMechanic && MechanicTable.Of(cuia).ReleasedBySeal(in board, i))
                {
                    board.Set(i, cuia.WithoutMechanic());
                    events.RecordReleased(i, cuia.Mechanic);
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>(e) Contadores.</summary>
        private static bool AdvanceCounters(in BoardSlice board, ref PourEvents events)
        {
            bool changed = false;
            for (int i = 0; i < board.Count; i++)
            {
                Cuia cuia = board[i];
                if (cuia.HasMechanic)
                {
                    changed |= MechanicTable.Of(cuia).AdvanceCounter(in board, i, ref events);
                }
            }

            return changed;
        }

        /// <summary>(f) Condiciones del tablero.</summary>
        private static bool CheckConditions(in BoardSlice board, ref PourEvents events)
        {
            bool changed = false;
            for (int i = 0; i < board.Count; i++)
            {
                Cuia cuia = board[i];
                if (cuia.HasMechanic)
                {
                    changed |= MechanicTable.Of(cuia).CheckCondition(in board, i, ref events);
                }
            }

            return changed;
        }
    }
}
