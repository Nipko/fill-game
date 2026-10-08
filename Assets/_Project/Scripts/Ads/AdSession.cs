#nullable enable
using System;
using AF.Platform;

namespace AF.Ads
{
    /// <summary>
    /// Lo que la cadencia de anuncios recuerda solo durante una sesión, en memoria (05 §6.3: <c>session.start</c>,
    /// <c>session.interstitials</c>, <c>session.last_interstitial</c>, <c>session.last_rewarded</c> y
    /// <c>session.is_first_session_after_install</c>), más el último recompensado de cada ubicación para su cooldown.
    /// Lo que dura más que una sesión va en el perfil (<see cref="AdLedger"/>).
    /// <para>
    /// Quien arranca el juego crea una sesión por arranque en frío y dice si es la de la instalación (el perfil se acaba de
    /// crear: 08 §2.3 <c>Profile.New()</c>). Volver de segundo plano tras una pausa larga podrá abrir otra sesión cuando
    /// <c>AF.Analytics</c> fije qué es una sesión (08 §8.1 <c>session_id</c>).
    /// </para>
    /// </summary>
    public sealed class AdSession
    {
        private readonly DateTime?[] _lastRewardedByPlacement = new DateTime?[AdPlacements.Count];

        /// <summary>Abre una sesión que empezó en <paramref name="startedAt"/> (UTC).</summary>
        /// <param name="startedAt">Inicio de la sesión, UTC.</param>
        /// <param name="isInstallSession">Es la sesión de la instalación: en ella nunca hay intersticiales (05 §6.1).</param>
        public AdSession(DateTime startedAt, bool isInstallSession)
        {
            StartedAt = DateTime.SpecifyKind(startedAt, DateTimeKind.Utc);
            IsInstallSession = isInstallSession;
        }

        /// <summary>Inicio de la sesión (UTC).</summary>
        public DateTime StartedAt { get; }

        /// <summary>Es la sesión de la instalación.</summary>
        public bool IsInstallSession { get; }

        /// <summary>Intersticiales vistos en esta sesión.</summary>
        public int Interstitials { get; private set; }

        /// <summary>Fin del último intersticial de la sesión, o null.</summary>
        public DateTime? LastInterstitialAt { get; private set; }

        /// <summary>Fin del último recompensado de la sesión (cualquier ubicación), o null.</summary>
        public DateTime? LastRewardedAt { get; private set; }

        /// <summary>Abre una sesión que empieza ahora.</summary>
        public static AdSession Start(IClock clock, bool isInstallSession)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            return new AdSession(clock.UtcNow, isInstallSession);
        }

        /// <summary>Fin del último recompensado de <paramref name="placement"/> en la sesión, o null.</summary>
        public DateTime? LastRewardedAtFor(AdPlacement placement)
        {
            return _lastRewardedByPlacement[(int)placement];
        }

        internal void RecordInterstitial(DateTime at)
        {
            Interstitials++;
            LastInterstitialAt = at;
        }

        internal void RecordRewarded(AdPlacement placement, DateTime at)
        {
            LastRewardedAt = at;
            _lastRewardedByPlacement[(int)placement] = at;
        }
    }
}
