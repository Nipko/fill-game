#nullable enable
using AF.Localization;
using AF.Platform;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Integridad, identidad, nube y anti-reloj al arrancar (bloque B8; 08 §2.3, §4.3, §10.1 y §11; nota
    /// <c>docs/dev/nube-integridad.md</c>). Parcial nuevo del parche <c>B8-ui.diff</c>, que también añade sus llamadas en
    /// <c>GameBootstrap.cs</c>:
    /// <list type="bullet">
    /// <item><see cref="SyncProfileAtBoot"/>, tras leer el perfil: comprueba su firma (sonda <c>profile_integrity</c>, que sale al
    /// montar Ajustes; sin castigo, 08 §10.1) y lo fusiona con la nube (<see cref="CloudSync.AtStartup"/>; sin Play Games, la nube falsa sin sesión no hace
    /// nada). Si gana la nube, el perfil fusionado se guarda y sustituye al local antes de crear ningún servicio.</item>
    /// <item><see cref="StampProfileIdentity"/>, con el idioma ya resuelto: <c>first_app_version</c> y <c>country_first</c> en la
    /// primera ejecución y <c>locale</c> en cada arranque.</item>
    /// <item><see cref="SigningStore"/>: el almacén del lote firma el perfil antes de cada escritura.</item>
    /// <item><see cref="CapsClock"/>: el reloj de la cartera, los anuncios, la tienda y la Crecida (<see cref="GuardedClock"/>):
    /// con el desfase del servidor por encima de <c>sec_clock_max_drift_h</c>, las canoas no regeneran y los topes diarios usan
    /// la fecha del servidor. El Reto del río sigue con su reloj y su <c>IServerDate</c> (ya contrasta él mismo).</item>
    /// </list>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        private IClock? _capsClock;

        // El último LoadProfile migró el perfil (08 §4.4): lo firmado ya no es lo que se lee.
        private static bool s_bootLoadMigrated;

        /// <summary>Identidad del dispositivo para los tests (por defecto, la del teléfono).</summary>
        internal static IDeviceIdentity? DeviceIdentityOverride { get; set; }

        /// <summary>Hora del servidor para la cartera y los topes en los tests (por defecto, sin red).</summary>
        internal static IServerDate? CapsServerOverride { get; set; }

        /// <summary>La firma del perfil leído al arrancar (null si no había perfil).</summary>
        public IntegrityStatus? BootIntegrity { get; private set; }

        /// <summary>Lo que dejó la fusión con la nube al arrancar (null si no se intentó).</summary>
        public CloudSyncReport? BootCloudSync { get; private set; }

        /// <summary>Reloj de la cartera y de los topes diarios (null antes del arranque).</summary>
        public IClock? CapsClock => _capsClock;

        /// <summary>Limpia los ganchos de test de la nube y de la integridad.</summary>
        internal static void ResetCloudOverrides()
        {
            DeviceIdentityOverride = null;
            CapsServerOverride = null;
            ResetAccountOverrides();
        }

        private static IDeviceIdentity DeviceIdentity => DeviceIdentityOverride ?? InstallIdentity.Instance;

        /// <summary>Reloj guardado por el servidor sobre <paramref name="device"/>.</summary>
        private IClock CreateCapsClock(IClock device)
        {
            _capsClock = new GuardedClock(device, CapsServerOverride ?? FakeServerDate.Offline(), ClockSecurityConfig.Defaults);
            return _capsClock;
        }

        /// <summary>El almacén del lote: firma antes de escribir.</summary>
        private static IProfileStore SigningStore(IProfileStore store)
        {
            return SigningProfileStore.Wrap(store, DeviceIdentity);
        }

        /// <summary>
        /// La firma del perfil recién leído, antes de que nada lo cambie (los extras de desarrollo, la nube o la primera escritura, que
        /// lo volvería a firmar). Un perfil migrado cuenta como sin firma [P]. Si no cuadra, suma <c>integrity.fraud_flag</c> (sale con
        /// la siguiente escritura; solo se mide, 08 §10.1).
        /// </summary>
        private void VerifyBootProfile(Profile profile)
        {
            BootIntegrity = s_bootLoadMigrated ? IntegrityStatus.Unsigned : ProfileIntegrity.Verify(profile);
            if (BootIntegrity == IntegrityStatus.Mismatch)
            {
                profile.Integrity.FraudFlag++;
                Debug.LogWarning(LogPrefix + "el perfil no cuadra con su firma (integrity.hmac): se sigue jugando; pierde los conflictos con la nube (08 §10.1).");
            }
        }

        /// <summary>
        /// Nube al arrancar (antes de crear los servicios), con la firma de <see cref="VerifyBootProfile"/>. Con el perfil en solo
        /// lectura no se fusiona ni se sube nada. Devuelve el perfil con el que seguir.
        /// </summary>
        private Profile SyncProfileAtBoot(Profile profile, IProfileStore store, bool locked)
        {
            if (locked)
            {
                return profile;
            }

            _cloudSave ??= CloudSaveOverride ?? FakeCloudSave.WithoutSession();
            Profile result = profile;
            CloudSync.AtStartup(profile, _cloudSave, DeviceIdentity, localIntegrity: BootIntegrity ?? IntegrityStatus.Unsigned, done: report =>
            {
                BootCloudSync = report;
                if (!report.Ran)
                {
                    return;
                }

                if (report.Error != null)
                {
                    Debug.LogWarning(LogPrefix + "nube: " + report.Error);
                }

                if (report.Replaced)
                {
                    result = report.Profile;
                    ProfileIntegrity.Sign(result, DeviceIdentity);
                    store.TrySave(result);
                }
            });

            return result; // la nube falsa responde en el acto; la de Play Games, cuando llegue, también se espera aquí [P]
        }

        /// <summary><c>first_app_version</c> y <c>country_first</c> (primera ejecución) y <c>locale</c> (siempre), sin guardar: sale con el siguiente guardado.</summary>
        private void StampProfileIdentity(Profile profile, bool firstRun, LocResolution locale, string deviceTag)
        {
            if (firstRun)
            {
                ProfileIdentity.StampFirstRun(profile, Application.version, deviceTag, locale.Locale);
            }
            else
            {
                ProfileIdentity.UpdateLocale(profile, locale.Locale);
            }
        }

        // La sonda aún no existe cuando se lee el perfil: lo de la firma y la nube sale al montar «Eliminar cuenta» (SetUpAccount).
        partial void ReportBootIntegrity()
        {
            if (_probe == null)
            {
                return;
            }

            if (BootIntegrity.HasValue)
            {
                _probe.ProfileIntegrityStatus(ProfileIntegrity.ToId(BootIntegrity.Value));
            }

            CloudSyncReport? sync = BootCloudSync;
            if (sync != null && sync.Ran)
            {
                _probe.SaveSync(CloudMerge.ToId(sync.Result), sync.Reason.HasValue ? ReasonId(sync.Reason.Value) : "none", sync.Bytes);
            }
        }

        private static string ReasonId(CloudMergeReason reason)
        {
            switch (reason)
            {
                case CloudMergeReason.NoCloud: return "no_cloud";
                case CloudMergeReason.Identical: return "identical";
                case CloudMergeReason.MaxLevel: return "max_level";
                case CloudMergeReason.EconomyTs: return "economy_ts";
                case CloudMergeReason.Tie: return "tie";
                case CloudMergeReason.Integrity: return "integrity";
                default: return "unknown";
            }
        }
    }
}
