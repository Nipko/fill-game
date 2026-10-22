#nullable enable
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Todo lo que necesita un overlay para ejecutar un paso de mecánica (<see cref="CuiaOverlay.PlayStep"/>): qué paso, qué
    /// cuias, su aspecto antes y después de la etapa (<see cref="StepStaging"/>), dónde están, las capas y el tablero, el arte
    /// y los efectos, y el ritmo. Lo rellena la cola de pasos (<see cref="MechanicStepQueue"/>). Al acabar el paso la cola
    /// manda <c>ShowContent(After, AfterSealed)</c> en <see cref="Index"/> y <c>ShowContent(OtherAfter, OtherAfterSealed)</c>
    /// en <see cref="OtherIndex"/>. Contrato congelado desde la ola 3 (T07).
    /// </summary>
    public readonly struct MechanicStepContext
    {
        /// <summary>Crea el contexto completo de un paso.</summary>
        public MechanicStepContext(
            MechanicStepKind kind,
            int index,
            int otherIndex,
            Cuia before,
            Cuia after,
            bool afterSealed,
            Cuia otherBefore,
            Cuia otherAfter,
            bool otherAfterSealed,
            Vector2 selfPos,
            Vector2 otherPos,
            ICuiaBoard? board,
            RectTransform? boardBackLayer,
            RectTransform? boardFrontLayer,
            IBoardFx? fx,
            ProceduralArt? art,
            float speedFactor,
            bool reduceMotion,
            int tint,
            int layerMask)
        {
            Kind = kind;
            Index = index;
            OtherIndex = otherIndex;
            Before = before;
            After = after;
            AfterSealed = afterSealed;
            OtherBefore = otherBefore;
            OtherAfter = otherAfter;
            OtherAfterSealed = otherAfterSealed;
            SelfPos = selfPos;
            OtherPos = otherPos;
            Board = board;
            BoardBackLayer = boardBackLayer;
            BoardFrontLayer = boardFrontLayer;
            Fx = fx;
            Art = art;
            SpeedFactor = speedFactor;
            ReduceMotion = reduceMotion;
            Tint = tint;
            LayerMask = layerMask;
        }

        /// <summary>Paso.</summary>
        public MechanicStepKind Kind { get; }

        /// <summary>Cuia dueña de la mecánica, que ejecuta el paso.</summary>
        public int Index { get; }

        /// <summary>La otra cuia del paso (víctima, pareja, cuia sellada que suelta la Liana…), o −1.</summary>
        public int OtherIndex { get; }

        /// <summary>Aspecto de <see cref="Index"/> antes de la etapa.</summary>
        public Cuia Before { get; }

        /// <summary>Aspecto de <see cref="Index"/> después de la etapa.</summary>
        public Cuia After { get; }

        /// <summary><see cref="Index"/> queda sellada tras la etapa.</summary>
        public bool AfterSealed { get; }

        /// <summary>Aspecto de <see cref="OtherIndex"/> antes de la etapa.</summary>
        public Cuia OtherBefore { get; }

        /// <summary>Aspecto de <see cref="OtherIndex"/> después de la etapa.</summary>
        public Cuia OtherAfter { get; }

        /// <summary><see cref="OtherIndex"/> queda sellada tras la etapa (la cuia de la llave de una Liana, por ejemplo).</summary>
        public bool OtherAfterSealed { get; }

        /// <summary>Pie de <see cref="Index"/> en reposo, en el espacio de las capas de tablero (<see cref="ICuiaBoard.Foot"/>).</summary>
        public Vector2 SelfPos { get; }

        /// <summary>Pie de <see cref="OtherIndex"/> en reposo, en el espacio de las capas de tablero (<c>default</c> sin otra cuia).</summary>
        public Vector2 OtherPos { get; }

        /// <summary>Tablero (posiciones y pintado de cualquier cuia).</summary>
        public ICuiaBoard? Board { get; }

        /// <summary>Capa del tablero detrás de las cuias.</summary>
        public RectTransform? BoardBackLayer { get; }

        /// <summary>Capa del tablero delante de las cuias.</summary>
        public RectTransform? BoardFrontLayer { get; }

        /// <summary>Efectos del tablero.</summary>
        public IBoardFx? Fx { get; }

        /// <summary>Arte procedural.</summary>
        public ProceduralArt? Art { get; }

        /// <summary>
        /// Multiplica las duraciones nominales: el factor del nivel (02 §4, <c>anim_speed_factor_*</c>), ×
        /// <c>anim_reduce_motion_factor</c> (0,5; B9) con <see cref="ReduceMotion"/> y el recorte del presupuesto desde el último vertido tocado (FX6; como mucho × 0,35;
        /// ver <see cref="MechanicStepQueue"/>).
        /// </summary>
        public float SpeedFactor { get; }

        /// <summary>Reducir movimiento (07 §7.8): sin partículas.</summary>
        public bool ReduceMotion { get; }

        /// <summary>Tinte del paso (índice de motor 1-16; 0 si no aplica): la gota, la fruta mordida, la llave.</summary>
        public int Tint { get; }

        /// <summary>Dosis afectadas (bit k = dosis k desde el fondo): las que se revelan, el barro que se lava.</summary>
        public int LayerMask { get; }
    }
}
