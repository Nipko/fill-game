#nullable enable
using System;
using AF.Save;

namespace AF.IAP
{
    /// <summary>
    /// <see cref="IOffersStore"/> sobre los bloques <c>piggy</c> y <c>offers</c> del perfil (08 §4.3; bloque B5, parche B5-profile),
    /// guardado con el <see cref="IProfileStore"/> que recibe (en el juego, la vía síncrona del lote, como la cartera). Un fallo de
    /// escritura no lanza: el cambio queda en el perfil en memoria y sale con la siguiente.
    /// </summary>
    public sealed class ProfileOffersStore : IOffersStore
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;

        /// <summary>Crea el almacén sobre <paramref name="profile"/>, que se guarda en <paramref name="store"/>.</summary>
        public ProfileOffersStore(Profile profile, IProfileStore store)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <inheritdoc />
        public OffersRecord Load()
        {
            var record = new OffersRecord();
            PiggyData? piggy = _profile.Piggy;
            if (piggy != null)
            {
                record.PiggySeeds = Math.Max(0, piggy.Seeds);
                record.PiggyCap = Math.Max(0, piggy.Cap);
                record.PiggyLastLevel = Math.Max(0, piggy.LastLevel);
                record.PiggyFullCardShown = piggy.FullCard;
            }

            OffersData? offers = _profile.Offers;
            if (offers != null)
            {
                record.StarterFirstSeenAt = offers.StarterFirstSeenAt;
                record.StarterReminders.AddRange(offers.StarterReminders);
                record.NoAdsCardDate = offers.NoAdsCardDate;
                record.NoAdsDeclines = offers.NoAdsDeclines;
                record.InterstitialsSeen = offers.InterstitialsSeen;
                record.SkinCardWorlds.AddRange(offers.SkinCards);
            }

            return record;
        }

        /// <inheritdoc />
        public bool Save(OffersRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            PiggyData piggy = _profile.Piggy ??= new PiggyData();
            piggy.Seeds = record.PiggySeeds;
            piggy.Cap = record.PiggyCap;
            piggy.LastLevel = record.PiggyLastLevel;
            piggy.FullCard = record.PiggyFullCardShown;

            OffersData offers = _profile.Offers ??= new OffersData();
            offers.StarterFirstSeenAt = record.StarterFirstSeenAt;
            offers.StarterReminders.Clear();
            offers.StarterReminders.AddRange(record.StarterReminders);
            offers.NoAdsCardDate = record.NoAdsCardDate;
            offers.NoAdsDeclines = record.NoAdsDeclines;
            offers.InterstitialsSeen = record.InterstitialsSeen;
            offers.SkinCards.Clear();
            offers.SkinCards.AddRange(record.SkinCardWorlds);
            return _store.TrySave(_profile);
        }
    }
}
