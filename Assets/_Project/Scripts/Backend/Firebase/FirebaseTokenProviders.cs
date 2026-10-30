#nullable enable
using System;
using AF.Backend.Auth;
#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections.Generic;
using Firebase.AppCheck;
using Firebase.Auth;
using Firebase.Extensions;
#endif

namespace AF.Backend.Firebase
{
    /// <summary>
    /// Los proveedores reales de tokens (F3-6; D-039): solo en un teléfono Android con la configuración de Firebase en el build
    /// (<see cref="FirebaseBoot.IsConfigured"/>). En el editor y en los tests no hay: el juego usa los falsos de <c>AF.Backend.Auth</c>.
    /// </summary>
    public static class FirebaseTokenProviders
    {
        private static FirebaseAuthTokenProvider? s_auth;
        private static AppCheckIntegrityTokenProvider? s_integrity;

        /// <summary>Hay proveedores reales en este build y en este teléfono.</summary>
        public static bool Available => FirebaseBoot.IsConfigured;

        /// <summary>Los proveedores reales (los mismos en toda la sesión), o false si no los hay.</summary>
        public static bool TryGet(out IAuthTokenProvider? auth, out IIntegrityTokenProvider? integrity)
        {
            if (!Available)
            {
                auth = null;
                integrity = null;
                return false;
            }

            s_auth ??= new FirebaseAuthTokenProvider();
            s_integrity ??= new AppCheckIntegrityTokenProvider();
            auth = s_auth;
            integrity = s_integrity;
            return true;
        }
    }

    /// <summary>
    /// ID token de Firebase Auth anónimo (contrato v1, <c>authHeader</c>; plan §7: <c>FirebaseUser.TokenAsync</c>). Si no hay usuario,
    /// inicia sesión anónima una vez (las peticiones que llegan mientras tanto esperan a esa misma). El UID nunca sale de aquí ni va a
    /// los logs.
    /// </summary>
    public sealed class FirebaseAuthTokenProvider : IAuthTokenProvider
    {
        /// <inheritdoc />
        public void GetIdToken(bool forceRefresh, Action<TokenResult> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            FirebaseBoot.WhenReady(ok =>
            {
                if (!ok)
                {
                    onDone(TokenResult.Fail(FirebaseBoot.UnavailableReason));
                    return;
                }

                EnsureUser(signedIn =>
                {
                    if (!signedIn)
                    {
                        onDone(TokenResult.Fail("auth_sign_in_failed"));
                        return;
                    }

                    RequestToken(forceRefresh, onDone);
                });
            });
        }

        /// <inheritdoc />
        public void SignOut()
        {
            // Sin Firebase arrancado en esta sesión no se arranca solo para esto (la banda < 13 nunca lo toca; D-033).
            if (FirebaseBoot.State == FirebaseBootState.NotStarted || !FirebaseBoot.IsConfigured)
            {
                return;
            }

            FirebaseBoot.WhenReady(ok =>
            {
                if (ok)
                {
                    SignOutNow();
                }
            });
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private readonly List<Action<bool>> _signingIn = new List<Action<bool>>();

        // El usuario guardado por el SDK (el de la cuenta borrada) se va; la petición siguiente crea el anónimo nuevo (EnsureUser).
        private static void SignOutNow()
        {
            try
            {
                FirebaseAuth.DefaultInstance.SignOut();
            }
            catch (Exception)
            {
                // Sin sesión que cerrar o el SDK sin iniciar: nada que hacer (no se escribe en los logs).
            }
        }

        private void EnsureUser(Action<bool> onDone)
        {
            FirebaseUser? user;
            try
            {
                user = FirebaseAuth.DefaultInstance.CurrentUser;
            }
            catch (Exception)
            {
                onDone(false);
                return;
            }

            if (user != null)
            {
                onDone(true);
                return;
            }

            _signingIn.Add(onDone);
            if (_signingIn.Count > 1)
            {
                return; // ya hay un inicio de sesión en marcha
            }

            try
            {
                FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
                {
                    bool ok = !task.IsFaulted && !task.IsCanceled && task.Result != null && task.Result.User != null;
                    FinishSignIn(ok);
                });
            }
            catch (Exception)
            {
                FinishSignIn(false);
            }
        }

        private void FinishSignIn(bool ok)
        {
            var waiting = _signingIn.ToArray();
            _signingIn.Clear();
            for (int i = 0; i < waiting.Length; i++)
            {
                waiting[i](ok);
            }
        }

        private static void RequestToken(bool forceRefresh, Action<TokenResult> onDone)
        {
            FirebaseUser? user;
            try
            {
                user = FirebaseAuth.DefaultInstance.CurrentUser;
            }
            catch (Exception)
            {
                user = null;
            }

            if (user == null)
            {
                onDone(TokenResult.Fail("auth_no_user"));
                return;
            }

            try
            {
                user.TokenAsync(forceRefresh).ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled || string.IsNullOrEmpty(task.Result))
                    {
                        onDone(TokenResult.Fail("auth_token_failed"));
                        return;
                    }

                    onDone(TokenResult.Ok(task.Result));
                });
            }
            catch (Exception)
            {
                onDone(TokenResult.Fail("auth_token_failed"));
            }
        }
#else
        private static void SignOutNow()
        {
        }

        private void EnsureUser(Action<bool> onDone)
        {
            onDone(false); // nunca llega aquí: FirebaseBoot no está listo fuera del teléfono
        }

        private static void RequestToken(bool forceRefresh, Action<TokenResult> onDone)
        {
            onDone(TokenResult.Fail("editor"));
        }
#endif
    }

    /// <summary>
    /// Token de App Check de <b>uso limitado</b> (Q-AWS-4 (a); contrato v1, <c>integrityHeader</c>): uno nuevo por petición con
    /// <c>integrity: true</c>, que la API verifica en AWS y solo acepta una vez. Por debajo, Play Integrity en release y el proveedor de
    /// depuración en los builds de desarrollo (<see cref="FirebaseBoot"/>; D-039, añadido del 2026-10-21).
    /// </summary>
    public sealed class AppCheckIntegrityTokenProvider : IIntegrityTokenProvider
    {
        /// <inheritdoc />
        public void GetLimitedUseToken(Action<TokenResult> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            FirebaseBoot.WhenReady(ok =>
            {
                if (!ok)
                {
                    onDone(TokenResult.Fail(FirebaseBoot.UnavailableReason));
                    return;
                }

                Request(onDone);
            });
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void Request(Action<TokenResult> onDone)
        {
            try
            {
                FirebaseAppCheck.DefaultInstance.GetLimitedUseAppCheckTokenAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled || string.IsNullOrEmpty(task.Result.Token)) // AppCheckToken es un struct
                    {
                        onDone(TokenResult.Fail("appcheck_token_failed"));
                        return;
                    }

                    onDone(TokenResult.Ok(task.Result.Token));
                });
            }
            catch (Exception)
            {
                onDone(TokenResult.Fail("appcheck_token_failed"));
            }
        }
#else
        private static void Request(Action<TokenResult> onDone)
        {
            onDone(TokenResult.Fail("editor"));
        }
#endif
    }
}
