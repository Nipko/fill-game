#nullable enable
using System;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;

namespace AF.Core.Session
{
    /// <summary>
    /// Contador visible de un nivel para el HUD: el de la Lluvia (vertidos útiles hasta la próxima gota y gotas pendientes;
    /// 02 §9.1 M6) o el de la Piraña (vertidos útiles hasta el próximo mordisco; 02 §9.1 M7). Como mucho hay uno por nivel
    /// (propuesta de D-007 b.2 M6-M7; <see cref="LevelValidation"/>) y forma parte del estado persistente (02 R8).
    /// </summary>
    /// <remarks>
    /// <para>Se deduce del tablero (<see cref="Of"/>): el contador vive en la cuia de la mecánica, así que Marea y Volver a la
    /// orilla lo restauran con el estado. <see cref="Remaining"/> solo baja con vertidos útiles: un vertido fútil no avanza
    /// el contador (D-007, Q18 B), y no hay contadores a favor del jugador (Q10).</para>
    /// <para>Sin contador (nivel sin Lluvia ni Piraña, o Lluvia cuya última gota ya cayó) <see cref="IsActive"/> es false.
    /// <c>default(LevelCounter)</c> es ese estado.</para>
    /// </remarks>
    public readonly struct LevelCounter : IEquatable<LevelCounter>
    {
        private readonly int _cuiaPlusOne;

        internal LevelCounter(MechanicType type, int cuia, int rhythm, int elapsed, int pendingDrops, bool blocked)
        {
            Type = type;
            _cuiaPlusOne = cuia + 1;
            Rhythm = rhythm;
            Elapsed = elapsed;
            PendingDrops = pendingDrops;
            Blocked = blocked;
        }

        /// <summary>Sin contador.</summary>
        public static LevelCounter None => default;

        /// <summary>Mecánica del contador: <see cref="MechanicType.Lluvia"/>, <see cref="MechanicType.Pirana"/> o ninguna.</summary>
        public MechanicType Type { get; }

        /// <summary>Hay un contador en el tablero.</summary>
        public bool IsActive => Type != MechanicType.None;

        /// <summary>Índice de la cuia de la mecánica (la Lluvia o la cueva de la Piraña), o −1 sin contador.</summary>
        public int CuiaIndex => _cuiaPlusOne - 1;

        /// <summary>Ritmo <c>k</c> en vertidos útiles (<c>lluvia_k_pours</c> / <c>pirana_k_pours</c> o el <c>k</c> del nivel).</summary>
        public int Rhythm { get; }

        /// <summary>Vertidos útiles contados desde la última gota o el último mordisco (0..k − 1).</summary>
        public int Elapsed { get; }

        /// <summary>
        /// Contador visible: vertidos útiles que faltan para la próxima gota o el próximo mordisco (1..k; 0 sin contador). Solo
        /// baja con vertidos útiles (Q18 B).
        /// </summary>
        public int Remaining => IsActive ? Rhythm - Elapsed : 0;

        /// <summary>Gotas de Lluvia que aún no han caído (0 en la Piraña).</summary>
        public int PendingDrops { get; }

        /// <summary>
        /// Lluvia: la cuia está llena, así que la gota esperará al siguiente vertido útil que deje hueco. Piraña: la cueva
        /// está llena, así que el próximo mordisco se perderá si sigue así (respuesta Q-C).
        /// </summary>
        public bool Blocked { get; }

        /// <summary>Contador de <paramref name="state"/>: el de la primera cuia de Lluvia o Piraña, o <see cref="None"/>.</summary>
        public static LevelCounter Of(BoardState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            for (int i = 0; i < state.Count; i++)
            {
                Cuia cuia = state[i];
                MechanicType type = cuia.Mechanic;
                if (type == MechanicType.Lluvia || type == MechanicType.Pirana)
                {
                    int pending = type == MechanicType.Lluvia ? cuia.MechanicN : 0;
                    return new LevelCounter(type, i, cuia.MechanicK, cuia.MechanicCounter, pending, cuia.IsFull);
                }
            }

            return None;
        }

        /// <inheritdoc />
        public bool Equals(LevelCounter other)
        {
            return Type == other.Type && _cuiaPlusOne == other._cuiaPlusOne && Rhythm == other.Rhythm && Elapsed == other.Elapsed
                && PendingDrops == other.PendingDrops && Blocked == other.Blocked;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is LevelCounter other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ((int)Type << 24) ^ (_cuiaPlusOne << 16) ^ (Rhythm << 12) ^ (Elapsed << 8) ^ (PendingDrops << 4) ^ (Blocked ? 1 : 0);
        }

        /// <summary>Forma legible: <c>lluvia@3 2/3 gotas=1</c>, <c>pirana@5 4/4</c> o <c>none</c>.</summary>
        public override string ToString()
        {
            if (!IsActive)
            {
                return "none";
            }

            string text = MechanicTypes.ToKey(Type) + "@" + CuiaIndex.ToString(CultureInfo.InvariantCulture) + " "
                + Remaining.ToString(CultureInfo.InvariantCulture) + "/" + Rhythm.ToString(CultureInfo.InvariantCulture);
            if (Type == MechanicType.Lluvia)
            {
                text += " gotas=" + PendingDrops.ToString(CultureInfo.InvariantCulture);
            }

            return Blocked ? text + " bloqueado" : text;
        }
    }
}
