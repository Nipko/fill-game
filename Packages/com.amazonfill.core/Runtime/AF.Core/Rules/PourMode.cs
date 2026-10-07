#nullable enable
using System;

namespace AF.Core.Rules
{
    /// <summary>
    /// Cantidad que mueve un vertido legal A→B (decisión del dueño D-001, 2026-10-06; <c>docs/dev/decisiones.md</c>).
    /// <c>default(PourMode)</c> es <see cref="Single"/>, el valor por defecto del dueño (<see cref="PourModes.Default"/>).
    /// </summary>
    /// <remarks>
    /// <para>El modo solo cambia <c>cantidad(A, B)</c> (02 §1.2, R9). <c>legal(A, B)</c>, <c>futil(A, B)</c>,
    /// <c>movimientos_utiles(S)</c> (salvo el campo <see cref="Pour.Amount"/>), el sellado (R5), la victoria (R12) y el
    /// bloqueo (R13) no dependen de él (decisiones.md §2.1.2): por eso <see cref="PourRules.IsLegal"/>,
    /// <see cref="PourRules.IsFutile"/>, <see cref="PourRules.HasUsefulMove"/> y <see cref="PourRules.IsStuck"/> no lo
    /// reciben. La única implementación de la cantidad es <see cref="CuiaRules.Amount(AF.Core.Board.Cuia, AF.Core.Board.Cuia, PourMode)"/>.</para>
    /// <para>El modo se fija al cargar cada nivel y no cambia a mitad de nivel: el historial de Marea y el par dependen
    /// de él (D-001, punto 3).</para>
    /// </remarks>
    public enum PourMode
    {
        /// <summary>
        /// Una sola dosis por vertido, la de la cima de A, aunque debajo haya más del mismo color (mecánica tipo
        /// <i>ball sort</i>). No existe el vertido parcial. Por defecto (D-001).
        /// </summary>
        Single = 0,

        /// <summary>
        /// La racha visible que cabe: <c>cantidad = min(racha visible de A, espacio de B)</c>, con vertido parcial (02 R6,
        /// R9). Es la regla canónica del género y la del prototipo <c>tools/levelgen_proto.py</c>; queda como opción de
        /// prueba por Remote Config.
        /// </summary>
        Run = 1,
    }

    /// <summary>
    /// Valor por defecto de <see cref="PourMode"/> y su conversión con el valor de la clave de Remote Config
    /// <c>rules_pour_mode</c> (<c>"single"</c> | <c>"run"</c>, default <c>"single"</c>; la fila vive en la tabla única de
    /// 08 §9, no aquí).
    /// </summary>
    public static class PourModes
    {
        /// <summary>Modo por defecto: una dosis por vertido (D-001).</summary>
        public const PourMode Default = PourMode.Single;

        /// <summary>Valor de <c>rules_pour_mode</c> para <see cref="PourMode.Single"/>.</summary>
        public const string SingleKey = "single";

        /// <summary>Valor de <c>rules_pour_mode</c> para <see cref="PourMode.Run"/>.</summary>
        public const string RunKey = "run";

        /// <summary><paramref name="mode"/> es <see cref="PourMode.Single"/> o <see cref="PourMode.Run"/>.</summary>
        public static bool IsDefined(PourMode mode)
        {
            return mode == PourMode.Single || mode == PourMode.Run;
        }

        /// <summary>
        /// Convierte el valor de <c>rules_pour_mode</c>: <c>"single"</c> o <c>"run"</c>, comparación ordinal (sin cultura,
        /// sin espacios y distinguiendo mayúsculas). Con cualquier otro valor, o null, devuelve false y
        /// <paramref name="mode"/> = <see cref="Default"/>; el llamante decide si avisa en el log.
        /// </summary>
        public static bool TryParse(string? key, out PourMode mode)
        {
            if (string.Equals(key, SingleKey, StringComparison.Ordinal))
            {
                mode = PourMode.Single;
                return true;
            }

            if (string.Equals(key, RunKey, StringComparison.Ordinal))
            {
                mode = PourMode.Run;
                return true;
            }

            mode = Default;
            return false;
        }

        /// <summary>Valor de <c>rules_pour_mode</c> para <paramref name="mode"/>: <c>"single"</c> o <c>"run"</c>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> no es un valor definido.</exception>
        public static string ToKey(PourMode mode)
        {
            switch (mode)
            {
                case PourMode.Single:
                    return SingleKey;
                case PourMode.Run:
                    return RunKey;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Modo de vertido desconocido.");
            }
        }

        /// <summary>Lanza <see cref="ArgumentOutOfRangeException"/> si <paramref name="mode"/> no es un valor definido.</summary>
        internal static void Check(PourMode mode, string paramName)
        {
            if (!IsDefined(mode))
            {
                throw new ArgumentOutOfRangeException(paramName, mode, "Modo de vertido desconocido: Single o Run.");
            }
        }
    }
}
