#nullable enable
using System;

namespace AF.IAP
{
    /// <summary>
    /// <see cref="IPlayerUidProvider"/> que lee el UID en el momento de cada compra (08 §10.1: <c>setObfuscatedAccountId</c> = UID; D-035,
    /// F3-3): en el teléfono, el de Firebase Auth anónimo (<c>AF.Backend.Firebase.FirebaseAuthUid.Current</c>, que no existe hasta la
    /// primera llamada autenticada al backend). Un valor que no cumple el contrato (<see cref="AF.Backend.Contract.ContractRules.IsObfuscatedAccountId"/>)
    /// no se envía. El UID nunca va a los logs.
    /// </summary>
    public sealed class DelegatePlayerUid : IPlayerUidProvider
    {
        private readonly Func<string?> _read;

        public DelegatePlayerUid(Func<string?> read)
        {
            _read = read ?? throw new ArgumentNullException(nameof(read));
        }

        /// <inheritdoc />
        public string? Uid
        {
            get
            {
                try
                {
                    return _read();
                }
                catch (Exception)
                {
                    return null; // sin UID la compra sigue (el servidor la acepta con aviso hasta exigirlo en prod; D-035, F2-3 a F2-8)
                }
            }
        }
    }
}
