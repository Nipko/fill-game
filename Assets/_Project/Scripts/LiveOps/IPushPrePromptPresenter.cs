#nullable enable
using System;
using System.Collections.Generic;

namespace AF.LiveOps
{
    /// <summary>
    /// Lo que enseña el pre-prompt propio (06 §8.1): TiTino, «¿Te avisamos cuando tu Reto del río esté listo y tu Bitácora espere?»,
    /// con «Avísame» y «Ahora no». Solo claves de la tabla de textos (los textos están en
    /// <c>docs/dev/traspaso/spark-enganches/B6-strings.csv</c> hasta que el integrador los pase a <c>strings.csv</c>).
    /// </summary>
    public readonly struct PrePromptRequest
    {
        /// <summary>Clave del texto de TiTino.</summary>
        public const string BodyKey = "push_preprompt_body";

        /// <summary>Clave del botón que acepta («Avísame»).</summary>
        public const string AcceptKey = "push_preprompt_accept";

        /// <summary>Clave del botón que rechaza («Ahora no»).</summary>
        public const string DeclineKey = "push_preprompt_decline";

        /// <summary>Crea la petición.</summary>
        public PrePromptRequest(PrePromptKind kind)
        {
            Kind = kind;
        }

        /// <summary>El primero o el segundo (el texto es el mismo [P]).</summary>
        public PrePromptKind Kind { get; }
    }

    /// <summary>
    /// Enseña el pre-prompt y avisa una sola vez con la respuesta: true = «Avísame», false = «Ahora no» (o cerrarlo, que cuenta
    /// como «Ahora no» [P]). La pantalla real es de la UI (una tarjeta aparte, no un takeover: D-033 #11); hasta entonces,
    /// <see cref="SimulatedPushPrePrompt"/>.
    /// </summary>
    public interface IPushPrePromptPresenter
    {
        /// <summary>Enseña <paramref name="request"/>; llama a <paramref name="answered"/> una vez, quizá en otro fotograma.</summary>
        void Show(PrePromptRequest request, Action<bool> answered);
    }

    /// <summary>Respuesta del pre-prompt simulado.</summary>
    public enum SimulatedPrePromptAnswer
    {
        /// <summary>«Ahora no» al instante (el default: nunca pide el permiso por su cuenta).</summary>
        NotNow = 0,

        /// <summary>«Avísame» al instante.</summary>
        Accept = 1,

        /// <summary>Se queda abierto hasta <see cref="SimulatedPushPrePrompt.Answer"/>.</summary>
        Wait = 2,
    }

    /// <summary>
    /// Pre-prompt simulado (la parte «simulada» de la fila B6 del plan de la tanda 5): no dibuja nada y contesta según
    /// <see cref="Mode"/>. Por defecto «Ahora no», para que un build sin la tarjeta real nunca llegue al diálogo nativo sin que el
    /// jugador haya tocado «Avísame» (06 §8.1). Guarda cada petición para los tests y las sondas.
    /// </summary>
    public sealed class SimulatedPushPrePrompt : IPushPrePromptPresenter
    {
        private readonly List<PrePromptRequest> _shown = new List<PrePromptRequest>();
        private Action<bool>? _pending;

        /// <summary>Cómo contesta.</summary>
        public SimulatedPrePromptAnswer Mode { get; set; }

        /// <summary>Las peticiones enseñadas, en orden.</summary>
        public IReadOnlyList<PrePromptRequest> Shown => _shown;

        /// <summary>Hay una petición abierta (<see cref="SimulatedPrePromptAnswer.Wait"/>).</summary>
        public bool IsOpen => _pending != null;

        /// <inheritdoc />
        public void Show(PrePromptRequest request, Action<bool> answered)
        {
            if (answered == null)
            {
                throw new ArgumentNullException(nameof(answered));
            }

            _shown.Add(request);
            switch (Mode)
            {
                case SimulatedPrePromptAnswer.Accept:
                    answered(true);
                    break;
                case SimulatedPrePromptAnswer.Wait:
                    _pending = answered;
                    break;
                default:
                    answered(false);
                    break;
            }
        }

        /// <summary>Contesta la petición abierta.</summary>
        /// <exception cref="InvalidOperationException">No hay ninguna abierta.</exception>
        public void Answer(bool accept)
        {
            Action<bool>? pending = _pending;
            if (pending == null)
            {
                throw new InvalidOperationException("No hay ningún pre-prompt abierto.");
            }

            _pending = null;
            pending(accept);
        }
    }
}
