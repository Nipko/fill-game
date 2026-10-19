#nullable enable

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Pasos de mecánica que siguen a un vertido (R10 por etapas con el punto fijo f′; anexo B de la propuesta de D-007). Cada
    /// paso lo ejecuta el overlay de la cuia dueña de la mecánica, que pinta las dos cuias si hace falta. No hay paso de
    /// «la cueva recibe»: el mordisco lo ejecuta la cueva (<see cref="Bite"/>). Contrato congelado desde la ola 3 (T07): el
    /// orden y las etapas los da <see cref="MechanicStepPlan"/>. La Tinaja (TJ3) añade al final sus dos pasos de la suelta,
    /// <see cref="Unhook"/> y <see cref="Upend"/>, sin cambiar los de antes.
    /// </summary>
    public enum MechanicStepKind
    {
        /// <summary>
        /// Se revelan dosis ocultas del origen: en Aguas turbias lo pintan las marcas de turbias; en la Tinaja, la nueva boca
        /// tras sacar (R7; lo pinta <see cref="TinajaOverlay"/>, nunca como agua turbia: contrato 27).
        /// </summary>
        Reveal = 0,

        /// <summary>El Barro de la cima del receptor se lava (lo pintan las marcas del Barro).</summary>
        MudWash = 1,

        /// <summary>La pareja de un Remolino recibe por el fondo.</summary>
        Receive = 2,

        /// <summary>Una Hoja cae o un Brote germina.</summary>
        Open = 3,

        /// <summary>Una cuia se sella como paso (con mecánica, o tras una gota o un mordisco).</summary>
        Seal = 4,

        /// <summary>Una Liana se suelta.</summary>
        Release = 5,

        /// <summary>Cae una gota de la Lluvia.</summary>
        RainDrop = 6,

        /// <summary>La gota espera: la cuia de la Lluvia está llena.</summary>
        RainWait = 7,

        /// <summary>Cae la última gota y se va la nube.</summary>
        RainEnd = 8,

        /// <summary>La Piraña muerde la cima de la víctima y la lleva a su cueva.</summary>
        Bite = 9,

        /// <summary>El mordisco se pierde (víctima sellada, Matapi, la propia cueva o cueva llena).</summary>
        BiteLost = 10,

        /// <summary>
        /// Suelta de la Tinaja, etapa (f) de R10 (b.2 regla 8): su cuia baja del jirau y se asienta en su ranura. La ejecuta su
        /// cuia (<see cref="CucharonOverlay"/>); la otra cuia es la tinaja.
        /// </summary>
        Unhook = 11,

        /// <summary>
        /// Suelta de la Tinaja, etapa (f) de R10: con la última sacada la boca enseña el fondo seco y, mientras su cuia baja, la
        /// tinaja vacía espera; después se tumba boca abajo sobre el jirau, a secar. La ejecuta la tinaja
        /// (<see cref="TinajaOverlay"/>); la otra cuia es su cuia.
        /// </summary>
        Upend = 12,
    }

    /// <summary>
    /// Cómo se revelan dosis ocultas (<c>CuiaView.PlayReveal</c>, que delega en <see cref="MurkyMarks"/>): por la regla R7
    /// al llegar a la cima, o con la Luciérnaga (04 §2.5).
    /// </summary>
    public enum RevealStyle
    {
        /// <summary>R7: la nueva cima del origen se aclara (02 §4.1; 07 §4.3; el aspecto de b.2 lo pone T15).</summary>
        Rule = 0,

        /// <summary>Luciérnaga: las capas se aclaran de arriba abajo (04 §2.5).</summary>
        Firefly = 1,
    }
}
