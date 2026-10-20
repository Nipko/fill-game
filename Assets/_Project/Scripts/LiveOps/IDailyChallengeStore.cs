#nullable enable

namespace AF.LiveOps
{
    /// <summary>
    /// Dónde vive el bloque <c>daily_challenge</c> del perfil (08 §4.3 + R-Q5; nota §5 y §10): en el juego, sobre el perfil de
    /// <c>AF.Save</c> (enganche E4, lo hace el integrador); en los tests, <see cref="InMemoryDailyChallengeStore"/>. Mismo patrón que
    /// <c>AF.GameFlow.ILevelStateStore</c> / <c>ProfileLevelStateStore</c>: <see cref="Save"/> no lanza por un fallo de escritura;
    /// el cambio sigue en memoria (<see cref="PendingSave"/>) y sale con el siguiente guardado o con <see cref="Flush"/>.
    /// <para>
    /// <b>Requisito de la implementación del juego (enganche E4):</b> el registro del Reto y la cartera (<c>AF.Economy.Wallet</c>)
    /// tienen que vivir en el <b>mismo</b> <c>Profile</c> y guardarse con el mismo <c>IProfileStore</c>. La garantía de «nunca dos
    /// pagos por la misma fecha» de <see cref="DailyChallengeRules.Claim"/> se apoya en eso: primero se registra el cobro y se
    /// guarda, y después se paga; si el guardado falla, el registro y el pago quedan juntos en memoria y salen en la misma
    /// escritura del perfil. Con dos archivos (o dos perfiles) podría llegar al disco el pago sin el registro y, al reabrir la
    /// app, volver a cobrarse la fecha. <see cref="InMemoryDailyChallengeStore"/> no lo cumple: es solo para tests y el editor.
    /// </para>
    /// </summary>
    public interface IDailyChallengeStore
    {
        /// <summary>El registro vivo (nunca null): la misma instancia mientras dure la sesión; se modifica y se pasa a <see cref="Save"/>.</summary>
        DailyChallengeRecord Load();

        /// <summary>Guarda <paramref name="record"/> y lo deja persistido; si la escritura falla, queda pendiente (<see cref="PendingSave"/>).</summary>
        void Save(DailyChallengeRecord record);

        /// <summary>El último cambio no llegó al disco.</summary>
        bool PendingSave { get; }

        /// <summary>Reintenta un guardado que falló. Devuelve true si ya no queda nada pendiente.</summary>
        bool Flush();
    }
}
