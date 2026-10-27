#nullable enable
using System;
using System.Globalization;

namespace AF.Platform
{
    /// <summary>Qué devolvió Play Age Signals (T-L3; <c>docs/legal/investigacion-legal-mundial.md</c> §3.2 y §11).</summary>
    public enum AgeSignalStatus
    {
        /// <summary>Sin señal: la cuenta no está en un territorio con señal, o el usuario no la compartió.</summary>
        None = 0,

        /// <summary>La llamada falló (sin Play, sin red, API no disponible, error del puente).</summary>
        Error = 1,

        /// <summary>Mayor de edad verificado (18+).</summary>
        Adult = 2,

        /// <summary>Una franja de edad (cuenta supervisada): <see cref="AgeSignal.AgeLower"/> y, si la hay, <see cref="AgeSignal.AgeUpper"/>.</summary>
        Range = 3,
    }

    /// <summary>
    /// La señal de edad de Google Play (Play Age Signals API, D-057 y su añadido del 2026-10-27; T-L3), ya traducida por el puente
    /// de Android a este formato propio. Solo sirve para dar una experiencia adecuada a la edad: <b>nunca</b> para la analítica, la
    /// publicidad, el marketing ni perfiles [F25] (T-L4). Inmutable.
    /// </summary>
    public readonly struct AgeSignal : IEquatable<AgeSignal>
    {
        /// <summary>Sin señal.</summary>
        public static readonly AgeSignal None = new AgeSignal(AgeSignalStatus.None, -1, -1);

        /// <summary>La llamada falló.</summary>
        public static readonly AgeSignal Error = new AgeSignal(AgeSignalStatus.Error, -1, -1);

        /// <summary>18+ verificado.</summary>
        public static readonly AgeSignal VerifiedAdult = new AgeSignal(AgeSignalStatus.Adult, 18, -1);

        private AgeSignal(AgeSignalStatus status, int lower, int upper)
        {
            Status = status;
            AgeLower = lower;
            AgeUpper = upper;
        }

        /// <summary>Qué devolvió.</summary>
        public AgeSignalStatus Status { get; }

        /// <summary>El límite inferior de la franja (−1 sin franja).</summary>
        public int AgeLower { get; }

        /// <summary>El límite superior de la franja (−1 si no lo hay, como en 18+).</summary>
        public int AgeUpper { get; }

        /// <summary>Una franja de <paramref name="lower"/> a <paramref name="upper"/> años (−1: sin límite superior).</summary>
        /// <exception cref="ArgumentOutOfRangeException">Límite inferior negativo, o superior menor que el inferior.</exception>
        public static AgeSignal ForRange(int lower, int upper = -1)
        {
            if (lower < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lower));
            }

            if (upper >= 0 && upper < lower)
            {
                throw new ArgumentOutOfRangeException(nameof(upper));
            }

            return new AgeSignal(AgeSignalStatus.Range, lower, upper < 0 ? -1 : upper);
        }

        /// <summary>
        /// La banda de la señal (T-L3): 0-12 → <see cref="AgeBand.Under13"/> (bloqueo, como &lt;13); 13-15 y 16-17 →
        /// <see cref="AgeBand.Teen"/>; 18+ → <see cref="AgeBand.Adult"/>; sin señal o con error → <see cref="AgeBand.Unknown"/> (manda la
        /// autodeclarada). Una franja se lee por su límite inferior (la más estricta si cruzara una frontera).
        /// </summary>
        public AgeBand Band
        {
            get
            {
                switch (Status)
                {
                    case AgeSignalStatus.Adult:
                        return AgeBand.Adult;
                    case AgeSignalStatus.Range:
                        if (AgeLower <= 12)
                        {
                            return AgeBand.Under13;
                        }

                        return AgeLower <= 17 ? AgeBand.Teen : AgeBand.Adult;
                    default:
                        return AgeBand.Unknown;
                }
            }
        }

        /// <summary>
        /// Lee el formato del puente de Android (<c>AfAgeSignals</c>): <c>adult</c>, <c>range:&lt;inferior&gt;[:&lt;superior&gt;]</c>,
        /// <c>none</c> o <c>error[:…]</c>. Lo que no se entiende (o null) es <see cref="Error"/>: manda la autodeclarada.
        /// </summary>
        public static AgeSignal Parse(string? wire)
        {
            if (string.IsNullOrEmpty(wire))
            {
                return Error;
            }

            string[] parts = wire!.Trim().Split(':');
            switch (parts[0])
            {
                case "adult":
                    return VerifiedAdult;
                case "none":
                    return None;
                case "range":
                {
                    if (parts.Length < 2 || !TryAge(parts[1], out int lower))
                    {
                        return Error;
                    }

                    int upper = -1;
                    if (parts.Length >= 3 && parts[2].Length > 0 && (!TryAge(parts[2], out upper) || upper < lower))
                    {
                        return Error;
                    }

                    return ForRange(lower, upper);
                }

                default:
                    return Error;
            }
        }

        /// <summary>El formato del puente (lo inverso de <see cref="Parse"/>; para la sonda y los tests).</summary>
        public override string ToString()
        {
            switch (Status)
            {
                case AgeSignalStatus.Adult: return "adult";
                case AgeSignalStatus.None: return "none";
                case AgeSignalStatus.Range:
                    return AgeUpper < 0
                        ? "range:" + AgeLower.ToString(CultureInfo.InvariantCulture)
                        : "range:" + AgeLower.ToString(CultureInfo.InvariantCulture) + ":" + AgeUpper.ToString(CultureInfo.InvariantCulture);
                default: return "error";
            }
        }

        /// <inheritdoc />
        public bool Equals(AgeSignal other) => Status == other.Status && AgeLower == other.AgeLower && AgeUpper == other.AgeUpper;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is AgeSignal other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => ((int)Status * 397) ^ (AgeLower * 31) ^ AgeUpper;

        private static bool TryAge(string text, out int age)
        {
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out age) && age >= 0 && age <= 150;
        }
    }

    /// <summary>
    /// Play Age Signals visto desde el juego (T-L3): la parte con motor (<c>AF.UI.AndroidAgeSignals</c>, solo en un teléfono
    /// Android) llama al puente JNI; el editor y los tests usan <see cref="FakeAgeSignals"/>. La respuesta llega en cualquier hilo y
    /// se recoge en el principal con <see cref="TryTake"/>.
    /// </summary>
    public interface IAgeSignals
    {
        /// <summary>Pide la señal (una vez por arranque, con la app abierta). Nunca lanza: un fallo llega como <see cref="AgeSignal.Error"/>.</summary>
        void Request();

        /// <summary>La respuesta, si ya llegó (una sola vez por petición). Hilo principal.</summary>
        bool TryTake(out AgeSignal signal);
    }

    /// <summary><see cref="IAgeSignals"/> falso (editor y tests): responde con <see cref="Result"/> en el acto o cuando se llama a <see cref="Complete"/>.</summary>
    public sealed class FakeAgeSignals : IAgeSignals
    {
        private AgeSignal? _pending;

        /// <summary>Crea el falso; con <paramref name="result"/>, responde eso en el acto a cada petición.</summary>
        public FakeAgeSignals(AgeSignal? result = null)
        {
            Result = result;
        }

        /// <summary>Lo que responde en el acto (null: espera a <see cref="Complete"/>).</summary>
        public AgeSignal? Result { get; set; }

        /// <summary>Veces que se pidió la señal.</summary>
        public int Requests { get; private set; }

        /// <inheritdoc />
        public void Request()
        {
            Requests++;
            if (Result.HasValue)
            {
                _pending = Result.Value;
            }
        }

        /// <summary>La respuesta llega ahora.</summary>
        public void Complete(AgeSignal signal)
        {
            _pending = signal;
        }

        /// <inheritdoc />
        public bool TryTake(out AgeSignal signal)
        {
            if (_pending.HasValue)
            {
                signal = _pending.Value;
                _pending = null;
                return true;
            }

            signal = AgeSignal.None;
            return false;
        }
    }

    /// <summary>
    /// Cómo se combina la señal con la banda autodeclarada (T-L3 y T-L4; 05 §7; <c>consent_age_signal_source</c> de 08 §9):
    /// <list type="bullet">
    /// <item>Solo con <c>consent_age_signal_source</c> = <c>self_declared+play_age_signals</c> (<see cref="Enabled"/>).</item>
    /// <item>Sin señal o con error, manda la autodeclarada.</item>
    /// <item>Con señal, la <b>más estricta</b> de las dos: 0-12 bloquea aunque se declarara 13-17 o 18+; 13-17 quita lo de 18+; un 18+
    /// verificado no relaja una banda declarada de 13-17 [P].</item>
    /// <item>La banda que se manda a la analítica es siempre la autodeclarada, y no se manda si la señal la corrigió [F25].</item>
    /// </list>
    /// </summary>
    public static class AgeSignalPolicy
    {
        /// <summary>La fuente de la banda incluye Play Age Signals.</summary>
        public static bool Enabled(ConsentConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return string.Equals(config.ConsentAgeSignalSource, ConsentConfig.SourceWithPlayAgeSignals, StringComparison.Ordinal);
        }

        /// <summary>
        /// La banda que se aplica con la declarada <paramref name="declared"/> y la de la señal <paramref name="signal"/>: sin señal, la
        /// declarada; sin declarada, la de la señal (solo cuenta para bloquear antes de la pantalla de edad); con las dos, la más
        /// estricta.
        /// </summary>
        public static AgeBand Effective(AgeBand declared, AgeBand signal)
        {
            if (signal == AgeBand.Unknown)
            {
                return declared;
            }

            if (declared == AgeBand.Unknown)
            {
                return signal;
            }

            return (int)signal < (int)declared ? signal : declared;
        }

        /// <summary>La señal cambió la banda declarada (entonces la banda no va a la analítica, T-L4).</summary>
        public static bool Corrects(AgeBand declared, AgeBand signal)
        {
            return declared != AgeBand.Unknown && Effective(declared, signal) != declared;
        }
    }
}
