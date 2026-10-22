#nullable enable
#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using Firebase.Auth;
#endif

namespace AF.Backend.Firebase
{
    /// <summary>
    /// El UID de Firebase Auth anónimo de esta instalación (08 §11), para <c>setObfuscatedAccountId</c> en la compra (08 §10.1; D-035,
    /// F3-3; D-040). Solo en un teléfono Android con Firebase listo (<see cref="FirebaseBoot"/>); en el editor y en los tests, null. No
    /// inicia sesión: el usuario anónimo lo crea la primera llamada autenticada al backend (<see cref="FirebaseAuthTokenProvider"/>,
    /// p. ej. <c>restoreLedger</c> al arrancar). Nunca va a los logs.
    /// </summary>
    public static class FirebaseAuthUid
    {
        /// <summary>El UID, o null si no hay Firebase o aún no hay usuario.</summary>
        public static string? Current
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (FirebaseBoot.State != FirebaseBootState.Ready)
                {
                    return null;
                }

                try
                {
                    FirebaseUser? user = FirebaseAuth.DefaultInstance.CurrentUser;
                    return user == null || string.IsNullOrEmpty(user.UserId) ? null : user.UserId;
                }
                catch (Exception)
                {
                    return null;
                }
#else
                return null;
#endif
            }
        }
    }
}
