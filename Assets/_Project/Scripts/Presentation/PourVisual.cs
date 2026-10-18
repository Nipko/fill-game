#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;

namespace AF.Presentation
{
    /// <summary>
    /// Un vertido ya aplicado en el modelo, tal como lo anima la cola visual (02 §3.2; arquitectura del paso 1, §4): el
    /// vertido (<see cref="Pour"/>: origen, cuia tocada y dosis), lo que ocurrió en cada etapa de R10
    /// (<see cref="Events"/>, anexo B de D-007) y los tableros antes (<see cref="Before"/>, lo que la vista mostraba) y
    /// después (<see cref="After"/>, el modelo). La vista no decide reglas: el receptor, los sellados y los cambios de las
    /// mecánicas salen del núcleo; solo los interpola para pintar.
    /// </summary>
    /// <remarks>
    /// Es un valor sin asignaciones: se construye en cada vertido y se pasa por <c>in</c> a
    /// <see cref="BoardAnimator.EnqueuePour(in PourVisual)"/>.
    /// </remarks>
    public readonly struct PourVisual
    {
        /// <summary>Crea el vertido visual.</summary>
        /// <param name="pour">Vertido del modelo: origen, cuia tocada (D-006: el traslado y el chorro van a ella) y dosis.</param>
        /// <param name="events">Etapas de R10 del vertido (<see cref="AF.Core.Session.PourResult.Events"/>).</param>
        /// <param name="before">Tablero que la vista mostraba antes del vertido.</param>
        /// <param name="after">Tablero del modelo tras el vertido.</param>
        /// <exception cref="ArgumentNullException">Falta alguno de los dos tableros.</exception>
        public PourVisual(Pour pour, PourEvents events, BoardState before, BoardState after)
        {
            Pour = pour;
            Events = events;
            Before = before ?? throw new ArgumentNullException(nameof(before));
            After = after ?? throw new ArgumentNullException(nameof(after));
        }

        /// <summary>Vertido del modelo, con la cantidad real movida (R9).</summary>
        public Pour Pour { get; }

        /// <summary>Lo que ocurrió en cada etapa de R10 (revelado, aperturas, sellados, liberaciones y contadores).</summary>
        public PourEvents Events { get; }

        /// <summary>Tablero que la vista mostraba antes del vertido.</summary>
        public BoardState Before { get; }

        /// <summary>Tablero del modelo después del vertido: el estado final de cada cuia que la vista pinta.</summary>
        public BoardState After { get; }

        /// <summary>Cuia de origen.</summary>
        public int Source => Pour.Source;

        /// <summary>Cuia tocada: el traslado, la inclinación y el chorro van a ella (D-006).</summary>
        public int Target => Pour.Target;

        /// <summary>Dosis movidas.</summary>
        public int Amount => Pour.Amount;

        /// <summary>
        /// <see cref="Events"/> describe este vertido (lo rellenó el núcleo: mismo origen, misma cuia tocada y alguna dosis).
        /// Un <c>default(PourEvents)</c> no lo describe, y entonces el receptor es la cuia tocada.
        /// </summary>
        public bool HasEvents
        {
            get
            {
                PourEvents ev = Events;
                return ev.Amount > 0 && ev.Source == Pour.Source && ev.Target == Pour.Target && (uint)ev.Receiver < (uint)After.Count;
            }
        }

        /// <summary>Cuia que recibió las dosis: la tocada, salvo en el Remolino, su pareja (02 §9.1 M4).</summary>
        public int Receiver => HasEvents ? Events.Receiver : Pour.Target;

        /// <summary>Las dosis entraron por el fondo de la pareja de un Remolino: la cuia tocada no se llena (02 §9.1 M4).</summary>
        public bool ThroughRemolino => Receiver != Pour.Target;

        /// <summary>Tinte de las dosis movidas: la cima del origen antes del vertido.</summary>
        public int Tint => Before[Pour.Source].TopColor;

        /// <summary>
        /// El vertido es <b>sacar</b> (M13, la Tinaja; D-019; propuesta de la Tinaja, b.2 «Animación y sonido» y c.6; tarea TJ3):
        /// la tinaja vierte en su cuia colgada. Su gesto es el contrario al normal: <b>se mueve el destino</b> (su cuia entra en la
        /// boca, se llena y vuelve) y el origen no se inclina. Los vertidos desde su cuia usan el gesto normal.
        /// </summary>
        public bool IsScoop => Before[Pour.Source].Mechanic == MechanicType.Tinaja && Before[Pour.Target].Mechanic == MechanicType.Cucharon;

        /// <summary>
        /// La cuia que vuela en el gesto: el origen en un vertido normal (D-006) y la cuia tocada al sacar (<see cref="IsScoop"/>).
        /// Los pasos de mecánica que la pintan esperan a que vuelva a su ranura (<see cref="Mechanics.MechanicStepQueue"/>).
        /// </summary>
        public int Mover => IsScoop ? Pour.Target : Pour.Source;

        /// <summary>
        /// Máscara de Aguas turbias reveladas en el vertido (02 R7, §11; bit i = cuia i): hoy solo el origen, en la etapa (a).
        /// 0 si <see cref="Events"/> no describe el vertido.
        /// </summary>
        public int RevealedMask => HasEvents ? Events.RevealedMask : 0;
    }
}
