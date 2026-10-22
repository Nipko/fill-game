#nullable enable
using System;
using System.Collections.Generic;

namespace AF.LiveOps
{
    /// <summary>
    /// Estado de la Bitácora del río en el perfil: el bloque <c>calendar</c> = <c>{cycle, next_day, last_claim_date}</c> [P]
    /// (Q-M3 (a); D-033, añadido del 2026-10-20, punto 6), más <c>played_date</c> [P] (Q-B1 de la nota <c>docs/dev/bitacora.md</c>:
    /// la casilla de hoy se cobra «al terminar el primer nivel del día», y eso tiene que sobrevivir a cerrar la app), y los marcos
    /// de país ganados, que en el perfil viven en <c>entitlements.frames</c> [P] (D-033, punto 7; B14-Q2). La elección no se
    /// guarda (punto 6): las casillas pasadas enseñan solo el sello. Solo datos; las reglas son de <see cref="LoginCalendarRules"/>.
    /// </summary>
    public sealed class LoginCalendarRecord
    {
        /// <summary><c>cycle</c>: ciclo en curso (1 el primero; al sellar la 30 empieza otro, 06 §4.2).</summary>
        public int Cycle { get; set; } = 1;

        /// <summary><c>next_day</c>: la casilla que toca sellar (1-30; reclamo secuencial sin reinicio, 06 §4.2).</summary>
        public int NextDay { get; set; } = 1;

        /// <summary><c>last_claim_date</c>: día local del último sello (<c>yyyy-MM-dd</c>), o null si nunca.</summary>
        public string? LastClaimDate { get; set; }

        /// <summary>
        /// <c>played_date</c> [P] (Q-B1): último día local en que el jugador terminó un nivel (campaña o Reto, ganado o con fallo
        /// duro), <c>yyyy-MM-dd</c>, o null. Con él, la casilla de hoy se puede sellar (06 §4.2: «haber jugado ≥1 nivel ese día
        /// local»).
        /// </summary>
        public string? PlayedDate { get; set; }

        /// <summary>
        /// Los marcos de país ganados (D-033, punto 7; Q-M6 (a): el del mundo en que está el jugador al sellar la 30). En el perfil,
        /// <c>entitlements.frames</c>. Se guardan como derecho y se enseñan en la 1.1.
        /// </summary>
        public List<string> Frames { get; } = new List<string>();

        /// <summary>Copia profunda.</summary>
        public LoginCalendarRecord Clone()
        {
            var copy = new LoginCalendarRecord
            {
                Cycle = Cycle,
                NextDay = NextDay,
                LastClaimDate = LastClaimDate,
                PlayedDate = PlayedDate,
            };
            copy.Frames.AddRange(Frames);
            return copy;
        }

        /// <summary>Lleva los valores fuera de rango a los de un registro nuevo (perfil editado a mano o de otra versión).</summary>
        public void Sanitize()
        {
            if (Cycle < 1)
            {
                Cycle = 1;
            }

            if (NextDay < 1 || NextDay > LoginCalendarTable.Days)
            {
                NextDay = 1;
            }
        }
    }

    /// <summary>
    /// Dónde vive el bloque <c>calendar</c> del perfil (Q-M3 (a); D-033, punto 6): en el juego, sobre el perfil de <c>AF.Save</c>
    /// (parche <c>B4-1-profile.diff</c>, <c>ProfileLoginCalendarStore</c>); en los tests y sin el parche,
    /// <see cref="InMemoryLoginCalendarStore"/>. Mismo patrón que <see cref="IDailyChallengeStore"/>: <see cref="Save"/> no lanza
    /// por un fallo de escritura; el cambio sigue en memoria (<see cref="PendingSave"/>) y sale con el siguiente guardado o con
    /// <see cref="Flush"/>.
    /// <para>
    /// <b>Requisito de la implementación del juego:</b> el bloque <c>calendar</c> y la cartera (<c>AF.Economy.Wallet</c>) viven en
    /// el <b>mismo</b> <c>Profile</c> y se guardan con el mismo <c>IProfileStore</c>. Así el avance de <c>next_day</c> y el cobro
    /// salen en la misma escritura (D-033, punto 6: «Cobro y avance de next_day en la misma transacción y la misma escritura») y
    /// una casilla nunca se cobra dos veces: primero se guarda el avance y después se paga.
    /// </para>
    /// </summary>
    public interface ILoginCalendarStore
    {
        /// <summary>El registro vivo (nunca null): la misma instancia mientras dure la sesión.</summary>
        LoginCalendarRecord Load();

        /// <summary>Guarda <paramref name="record"/>; si la escritura falla, queda pendiente (<see cref="PendingSave"/>).</summary>
        void Save(LoginCalendarRecord record);

        /// <summary>El último cambio no llegó al disco.</summary>
        bool PendingSave { get; }

        /// <summary>Reintenta un guardado que falló. Devuelve true si ya no queda nada pendiente.</summary>
        bool Flush();
    }

    /// <summary>
    /// <see cref="ILoginCalendarStore"/> en memoria (tests y editor, y el juego mientras no se aplique el parche del perfil): se
    /// pierde al cerrar la app, así que no cumple el requisito de «una sola escritura» de <see cref="ILoginCalendarStore"/>.
    /// </summary>
    public sealed class InMemoryLoginCalendarStore : ILoginCalendarStore
    {
        private LoginCalendarRecord _record;

        /// <summary>Crea el almacén con <paramref name="initial"/> (copiado) o con un registro nuevo.</summary>
        public InMemoryLoginCalendarStore(LoginCalendarRecord? initial = null)
        {
            _record = initial?.Clone() ?? new LoginCalendarRecord();
        }

        /// <summary>Hace fallar los guardados siguientes (tests).</summary>
        public bool FailSaves { get; set; }

        /// <summary>Guardados pedidos.</summary>
        public int SaveCount { get; private set; }

        /// <summary>Copia del último registro que llegó al «disco», o null si ninguno.</summary>
        public LoginCalendarRecord? Persisted { get; private set; }

        /// <inheritdoc />
        public bool PendingSave { get; private set; }

        /// <inheritdoc />
        public LoginCalendarRecord Load()
        {
            return _record;
        }

        /// <inheritdoc />
        public void Save(LoginCalendarRecord record)
        {
            _record = record ?? throw new ArgumentNullException(nameof(record));
            SaveCount++;
            Persist();
        }

        /// <inheritdoc />
        public bool Flush()
        {
            if (PendingSave)
            {
                Persist();
            }

            return !PendingSave;
        }

        private void Persist()
        {
            if (FailSaves)
            {
                PendingSave = true;
                return;
            }

            Persisted = _record.Clone();
            PendingSave = false;
        }
    }
}
