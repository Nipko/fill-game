#nullable enable
using AF.Core.Session;

namespace AF.Core.Rules
{
    /// <summary>
    /// Lo que ocurrió en un vertido, etapa a etapa de R10 en el orden del anexo B (D-007, Q13): (a) revelado de la nueva
    /// cima de A, (b) aperturas por recepción, (c) sellado, (d) liberaciones por sellado, (e) contadores, (f) condiciones
    /// del tablero, (f′) punto fijo de (c)-(f), (g) victoria y (h) bloqueo. Lo rellena
    /// <see cref="BoardRules.ApplyInPlace(AF.Core.Board.Cuia[], int, int, int[], int, int, PourMode, bool, ref PourEvents)"/>;
    /// la presentación lo usa para animar en ese mismo orden y Marea lo deshace en orden inverso (contrato con el solver).
    /// </summary>
    /// <remarks>
    /// Las máscaras llevan un bit por cuia (bit i = cuia i, índices relativos del tablero; 16 cuias como máximo, R1).
    /// Es un <c>struct</c> mutable que se pasa por <c>ref</c>: el bucle caliente no asigna.
    /// </remarks>
    public struct PourEvents
    {
        /// <summary>Índice de la cuia origen A.</summary>
        public int Source { get; internal set; }

        /// <summary>Índice de la cuia destino B que tocó el jugador.</summary>
        public int Target { get; internal set; }

        /// <summary>Cuia que recibió las dosis: B, salvo en el Remolino (su pareja, 02 §9.1 M4).</summary>
        public int Receiver { get; internal set; }

        /// <summary>Dosis movidas: <c>cantidad(A, B)</c> en el modo de vertido (R9).</summary>
        public int Amount { get; internal set; }

        /// <summary>Color de las dosis movidas.</summary>
        public int Color { get; internal set; }

        /// <summary>El vertido era fútil (02 §1.4 con el anexo B): no avanza contadores (Q18 B).</summary>
        public bool Futile { get; internal set; }

        /// <summary>(a) La nueva cima de A estaba oculta y quedó visible (R7).</summary>
        public bool SourceRevealed { get; internal set; }

        /// <summary>
        /// Transferencia: cuias cuyo Barro de la cima lavó este vertido (evento BarroWashed; 02 §9.1 M5). Solo puede ser la
        /// que recibió.
        /// </summary>
        public int BarroWashedMask { get; internal set; }

        /// <summary>(b) Cuias cuya mecánica se abrió al recibir (Hoja, Brote): pasan a ser normales.</summary>
        public int OpenedMask { get; internal set; }

        /// <summary>(b) Hojas que cayeron en este vertido (evento HojaOpened; 02 §9.1 M1).</summary>
        public int HojaOpenedMask { get; internal set; }

        /// <summary>(b) Brotes que germinaron en este vertido (02 §9.1 M2).</summary>
        public int BroteOpenedMask { get; internal set; }

        /// <summary>(c) Cuias que este vertido dejó selladas (R5).</summary>
        public int SealedMask { get; internal set; }

        /// <summary>(d) Cuias que se soltaron por un sellado (Liana): pasan a ser normales.</summary>
        public int ReleasedMask { get; internal set; }

        /// <summary>(d) Lianas que se soltaron en este vertido (evento LianaReleased; D-007).</summary>
        public int LianaReleasedMask { get; internal set; }

        /// <summary>(e) Los contadores avanzaron (vertido no fútil con alguna mecánica de contador).</summary>
        public bool CountersAdvanced { get; internal set; }

        /// <summary>Cuias cuyo contenido cambió en el vertido (A, la que recibió y las que tocaron las mecánicas).</summary>
        public int ChangedMask { get; internal set; }

        /// <summary>(f′) Vueltas extra del punto fijo de (c), (d) y (f) (0 si nada más cambió).</summary>
        public int FixedPointPasses { get; internal set; }

        /// <summary>(g)-(h) ya se evaluaron y <see cref="Status"/> es válido.</summary>
        public bool StatusEvaluated { get; internal set; }

        /// <summary>(g) victoria (R12) y, si no, (h) bloqueo (R13-R15); solo si <see cref="StatusEvaluated"/>.</summary>
        public LevelStatus Status { get; internal set; }

        /// <summary>La cuia <paramref name="index"/> quedó sellada en este vertido.</summary>
        public bool WasSealed(int index)
        {
            return ((SealedMask >> index) & 1) != 0;
        }

        /// <summary>La mecánica de la cuia <paramref name="index"/> se abrió (b) o se soltó (d) en este vertido.</summary>
        public bool WasUnlocked(int index)
        {
            return (((OpenedMask | ReleasedMask) >> index) & 1) != 0;
        }

        internal void Begin(int source, int target, int amount, int color, bool futile)
        {
            Source = source;
            Target = target;
            Receiver = target;
            Amount = amount;
            Color = color;
            Futile = futile;
            SourceRevealed = false;
            BarroWashedMask = 0;
            OpenedMask = 0;
            HojaOpenedMask = 0;
            BroteOpenedMask = 0;
            SealedMask = 0;
            ReleasedMask = 0;
            LianaReleasedMask = 0;
            CountersAdvanced = false;
            ChangedMask = (1 << source) | (1 << target);
            FixedPointPasses = 0;
            StatusEvaluated = false;
            Status = LevelStatus.Playing;
        }

        /// <summary>Transferencia: el vertido lavó el Barro de la cima de la cuia <paramref name="index"/> (02 §9.1 M5).</summary>
        internal void RecordWashed(int index)
        {
            BarroWashedMask |= 1 << index;
        }

        /// <summary>(b) La mecánica <paramref name="type"/> de la cuia <paramref name="index"/> se abrió al recibir.</summary>
        internal void RecordOpened(int index, Mechanics.MechanicType type)
        {
            int bit = 1 << index;
            OpenedMask |= bit;
            if (type == Mechanics.MechanicType.Hoja)
            {
                HojaOpenedMask |= bit;
            }
            else if (type == Mechanics.MechanicType.Brote)
            {
                BroteOpenedMask |= bit;
            }
        }

        /// <summary>(d) La mecánica <paramref name="type"/> de la cuia <paramref name="index"/> se soltó por un sellado.</summary>
        internal void RecordReleased(int index, Mechanics.MechanicType type)
        {
            int bit = 1 << index;
            ReleasedMask |= bit;
            if (type == Mechanics.MechanicType.Liana)
            {
                LianaReleasedMask |= bit;
            }
        }

        internal void Finish(LevelStatus status)
        {
            Status = status;
            StatusEvaluated = true;
        }
    }
}
