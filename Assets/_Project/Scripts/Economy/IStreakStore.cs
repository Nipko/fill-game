#nullable enable

namespace AF.Economy
{
    /// <summary>
    /// Dónde vive el estado de la Crecida y de la Hoja escudo (<see cref="StreakRecord"/>; 06 §4.3, Q-M9 (a) [P]): en el juego,
    /// sobre el perfil de <c>AF.Save</c> (<c>ProfileStreakStore</c>, parche S1-profile para el integrador: bloque <c>streak</c> y
    /// <c>daily.shield_available</c>, <c>streak_hits</c> y <c>streak_seeds</c>); en los tests, <see cref="InMemoryStreakStore"/>.
    /// Mismo patrón que <c>AF.LiveOps.IDailyChallengeStore</c>: <see cref="Save"/> no lanza por un fallo de escritura; el cambio
    /// sigue en memoria (<see cref="PendingSave"/>) y sale con el siguiente guardado o con <see cref="Flush"/>.
    /// <para>
    /// <see cref="Load"/> aplica antes el cambio de día local (solo hacia delante; 06 §4.3 <c>on_app_open</c>): repone la Hoja
    /// escudo y pone a cero los hitos y las semillas de hoy, sin tocar la racha ni el río crecido (sin expiración por reloj, 04 §8).
    /// Por eso el almacén lleva su reloj: las reglas no reinician nada a mano. También recorta lo que no puede salir de las reglas
    /// (<see cref="StreakRecord.Normalize"/>).
    /// </para>
    /// <para>
    /// <b>Uso obligatorio:</b> el servicio de la Crecida llama a <see cref="Load"/> al empezar cada fin de nivel (y cada operación
    /// sobre la Crecida) y no guarda el registro de un nivel a otro. Aun así, otro módulo (la cartera, los anuncios, las compras)
    /// puede pasar <c>daily</c> al día siguiente entre ese <see cref="Load"/> y el <see cref="Save"/> (medianoche). Entonces
    /// <see cref="Save"/> aplica la <b>regla conservadora</b> de <see cref="StreakRecord.MergeDay"/>: del registro del día anterior
    /// solo cuenta lo que cambió desde la carga (un hito nuevo queda pagado también hoy, el aumento de semillas cuenta para el tope
    /// de hoy y un escudo gastado gasta el de hoy); en ese borde puede pagar de menos, nunca dos veces. La racha y el río crecido se
    /// escriben siempre.
    /// </para>
    /// <para>
    /// <b>Requisito de la implementación del juego:</b> la Crecida y la cartera (<see cref="Wallet"/>) tienen que vivir en el
    /// <b>mismo</b> <c>Profile</c> y guardarse con el mismo <c>IProfileStore</c> (el lote <c>ProfileSaveBatch</c>, como la cartera).
    /// La garantía de «cada hito se paga una vez por día» (<c>streak_hits_once_per_day</c>) se apoya en eso: el hito se apunta en
    /// <see cref="StreakRecord.HitsToday"/> y se guarda, y después se paga con <see cref="Wallet.Apply"/>; si un guardado falla, el
    /// apunte y el pago quedan juntos en memoria y salen en la misma escritura del perfil. Con dos archivos podría llegar al disco
    /// el pago sin el apunte y, al reabrir la app, volver a pagarse el hito el mismo día. <see cref="InMemoryStreakStore"/> no lo
    /// cumple: es solo para tests y el editor.
    /// </para>
    /// </summary>
    public interface IStreakStore
    {
        /// <summary>
        /// El estado vivo (nunca null), ya pasado al día local de hoy: la misma instancia mientras dure la sesión; se modifica y se
        /// pasa a <see cref="Save"/>. Lo que se cambie sin <see cref="Save"/> no llega al guardado.
        /// </summary>
        StreakRecord Load();

        /// <summary>Guarda <paramref name="record"/> y lo deja persistido; si la escritura falla, queda pendiente (<see cref="PendingSave"/>).</summary>
        void Save(StreakRecord record);

        /// <summary>El último cambio no llegó al disco.</summary>
        bool PendingSave { get; }

        /// <summary>Reintenta un guardado que falló. Devuelve true si ya no queda nada pendiente.</summary>
        bool Flush();
    }
}
