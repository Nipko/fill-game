#nullable enable
using System;
using System.Collections.Generic;

namespace AF.IAP
{
    /// <summary>
    /// Lo que guardan el Cántaro y las ofertas contextuales (bloque B5), sin motor. Son dos bloques del perfil (08 §4.3):
    /// <list type="bullet">
    /// <item><description><c>piggy</c> = <c>{seeds, cap}</c> (canon de 08 §4.3), más <c>last_level</c> y <c>full_card</c> [P]: el
    /// último nivel que lo llenó (una victoria no lo llena dos veces) y si ya salió la tarjeta «¡Lleno!» de este llenado;</description></item>
    /// <item><description><c>offers</c> [P] (08 §4.3 no lo trae; pregunta Q-B5-8): la primera vista de la Mochila y sus recordatorios,
    /// la tarjeta Sin anuncios (último día y rechazos), los intersticiales vistos para su disparador y los mundos cuya tarjeta de skin
    /// ya salió.</description></item>
    /// </list>
    /// Solo datos: las reglas son de <see cref="PiggyJar"/> y <see cref="OfferScheduler"/>. El parche B5-profile lo guarda en el perfil
    /// (<c>ProfileOffersStore</c>); sin él, <see cref="InMemoryOffersStore"/>.
    /// </summary>
    public sealed class OffersRecord
    {
        /// <summary><c>piggy.seeds</c>: semillas del Cántaro (0 … <see cref="PiggyCap"/>).</summary>
        public int PiggySeeds { get; set; }

        /// <summary><c>piggy.cap</c>: <c>eco_piggy_cap</c> en el último guardado (0 si nunca se llenó; el tope en vigor es siempre el de la configuración).</summary>
        public int PiggyCap { get; set; }

        /// <summary><c>piggy.last_level</c> [P]: último nivel de campaña cuya victoria llenó el Cántaro (0 = ninguno).</summary>
        public int PiggyLastLevel { get; set; }

        /// <summary><c>piggy.full_card</c> [P]: la tarjeta «¡Lleno!» ya salió en este llenado (se borra al comprarlo).</summary>
        public bool PiggyFullCardShown { get; set; }

        /// <summary><c>offers.starter_first_seen_at</c> [P]: primera tarjeta de la Mochila (UTC; empieza su temporizador), o null.</summary>
        public DateTime? StarterFirstSeenAt { get; set; }

        /// <summary><c>offers.starter_reminders</c> [P]: niveles de recordatorio de la Mochila ya enseñados (15 y 19).</summary>
        public List<int> StarterReminders { get; } = new List<int>();

        /// <summary><c>offers.noads_card_date</c> [P]: día local (<c>yyyy-MM-dd</c>) de la última tarjeta Sin anuncios, o null.</summary>
        public string? NoAdsCardDate { get; set; }

        /// <summary><c>offers.noads_declines</c> [P]: tarjetas Sin anuncios cerradas sin comprar.</summary>
        public int NoAdsDeclines { get; set; }

        /// <summary>
        /// <c>offers.interstitials_seen</c> [P]: intersticiales vistos en la vida del perfil, con tope en el del disparador (solo
        /// hace falta saber si se llegó al 3.º).
        /// </summary>
        public int InterstitialsSeen { get; set; }

        /// <summary><c>offers.skin_cards</c> [P]: mundos cuya tarjeta de skin ya salió (1 por mundo; 05 §5).</summary>
        public List<int> SkinCardWorlds { get; } = new List<int>();

        /// <summary>Copia profunda (los almacenes devuelven copias).</summary>
        public OffersRecord Clone()
        {
            var copy = new OffersRecord
            {
                PiggySeeds = PiggySeeds,
                PiggyCap = PiggyCap,
                PiggyLastLevel = PiggyLastLevel,
                PiggyFullCardShown = PiggyFullCardShown,
                StarterFirstSeenAt = StarterFirstSeenAt,
                NoAdsCardDate = NoAdsCardDate,
                NoAdsDeclines = NoAdsDeclines,
                InterstitialsSeen = InterstitialsSeen,
            };
            copy.StarterReminders.AddRange(StarterReminders);
            copy.SkinCardWorlds.AddRange(SkinCardWorlds);
            return copy;
        }
    }

    /// <summary>
    /// Dónde viven <c>piggy</c> y <c>offers</c>. <see cref="Load"/> devuelve una copia; <see cref="Save"/> la escribe entera y
    /// devuelve si quedó guardada (un fallo de disco no lanza: el cambio sigue en memoria y sale con la siguiente escritura, como la
    /// cartera). Hilo principal.
    /// </summary>
    public interface IOffersStore
    {
        /// <summary>Una copia de lo guardado.</summary>
        OffersRecord Load();

        /// <summary>Guarda <paramref name="record"/>; false si la escritura falló.</summary>
        bool Save(OffersRecord record);
    }

    /// <summary>
    /// El registro en uso, compartido por <see cref="PiggyJar"/> y <see cref="OfferScheduler"/>: se lee una vez del almacén y cada
    /// cambio se guarda entero (<see cref="Persist"/>). Si la escritura falla, el cambio sigue aquí y sale con la siguiente
    /// (<see cref="PendingSave"/>).
    /// </summary>
    public sealed class OffersState
    {
        private readonly IOffersStore _store;

        /// <summary>Lee el registro de <paramref name="store"/>.</summary>
        public OffersState(IOffersStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            Record = store.Load() ?? new OffersRecord();
        }

        /// <summary>El registro en memoria (lo cambian los servicios y después llaman a <see cref="Persist"/>).</summary>
        public OffersRecord Record { get; }

        /// <summary>El último guardado falló.</summary>
        public bool PendingSave { get; private set; }

        /// <summary>Guarda el registro; devuelve si quedó guardado.</summary>
        public bool Persist()
        {
            PendingSave = !_store.Save(Record);
            return !PendingSave;
        }
    }

    /// <summary>Almacén en memoria (tests y desarrollo sin el parche B5-profile: se pierde al cerrar la app).</summary>
    public sealed class InMemoryOffersStore : IOffersStore
    {
        private OffersRecord _record = new OffersRecord();

        /// <summary>Escrituras hechas (tests).</summary>
        public int Saves { get; private set; }

        /// <summary>Si es true, <see cref="Save"/> falla sin guardar (tests de disco lleno).</summary>
        public bool FailSaves { get; set; }

        /// <inheritdoc />
        public OffersRecord Load()
        {
            return _record.Clone();
        }

        /// <inheritdoc />
        public bool Save(OffersRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (FailSaves)
            {
                return false;
            }

            _record = record.Clone();
            Saves++;
            return true;
        }
    }
}
