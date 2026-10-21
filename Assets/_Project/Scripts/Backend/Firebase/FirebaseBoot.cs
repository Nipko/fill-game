#nullable enable
using System;
using System.Collections.Generic;
#if UNITY_ANDROID && !UNITY_EDITOR
using Firebase;
using Firebase.AppCheck;
using Firebase.Extensions;
using UnityEngine;
#endif

namespace AF.Backend.Firebase
{
    /// <summary>Estado del arranque de Firebase.</summary>
    public enum FirebaseBootState
    {
        /// <summary>Nadie lo ha pedido todavía.</summary>
        NotStarted = 0,

        /// <summary>Comprobando dependencias (asíncrono).</summary>
        Starting = 1,

        /// <summary>Firebase listo: se pueden usar Auth, App Check, Analytics, Crashlytics y Remote Config.</summary>
        Ready = 2,

        /// <summary>Sin Firebase en este build o en este teléfono (<see cref="FirebaseBoot.Failure"/> dice por qué).</summary>
        Unavailable = 3,
    }

    /// <summary>
    /// Arranque único del Firebase Unity SDK 13.17.0 (D-039; 08 §2.3: <c>Firebase.InitAsync(analytics_collection=false)</c>), que
    /// comparten los proveedores de tokens (<see cref="FirebaseAuthTokenProvider"/>, <see cref="AppCheckIntegrityTokenProvider"/>),
    /// el sumidero de analítica, Crashlytics y Remote Config. Solo existe de verdad en <c>UNITY_ANDROID &amp;&amp; !UNITY_EDITOR</c>
    /// (08 §2.1, principio 6): en el editor (ARM64 o x64) y en los tests, <see cref="IsConfigured"/> es false y nada toca el SDK.
    /// <para>
    /// <see cref="IsConfigured"/> mira, sin red y sin el SDK, si el build lleva la configuración que genera Firebase a partir de
    /// <c>google-services.json</c> (el recurso <c>google_app_id</c>). Sin ella, el juego sigue con las implementaciones falsas (build de
    /// desarrollo) o sin backend (release). El archivo lo coloca el dueño (regla dura 5; <c>docs/dev/firebase-cliente.md</c>).
    /// </para>
    /// <para>
    /// App Check se configura con Play Integrity <b>antes</b> de tocar Firebase (lo exige el SDK) y sin refresco automático: el
    /// cliente solo pide tokens de uso limitado (Q-AWS-4 (a)). La recogida de Analytics no se enciende aquí: el manifiesto
    /// (<c>Assets/Plugins/Android/LauncherManifest.xml</c>) la deja apagada hasta el consentimiento.
    /// </para>
    /// Solo desde el hilo principal. Nunca escribe en los logs (UID y tokens).
    /// </summary>
    public static class FirebaseBoot
    {
        /// <summary>Proyecto Firebase de dev (D-035, añadido «Firebase dev en la cuenta personal»; identificador público).</summary>
        public const string DevProjectId = "fill-dev-e75a2";

        /// <summary>App Android <c>com.amazonfill.dev</c> del proyecto de dev (D-035; identificador público).</summary>
        public const string DevAndroidAppId = "1:528273757148:android:45d5cf0ddb2e3021c93519";

        private static readonly List<Action<bool>> s_waiting = new List<Action<bool>>();
        private static bool? s_configured;

        /// <summary>Estado actual.</summary>
        public static FirebaseBootState State { get; private set; }

        /// <summary>Motivo de <see cref="FirebaseBootState.Unavailable"/> (sin datos personales), o null.</summary>
        public static string? Failure { get; private set; }

        /// <summary>
        /// El build lleva la configuración de Firebase y, en un build de desarrollo, es la del proyecto de dev
        /// (<see cref="DevProjectId"/> y <see cref="DevAndroidAppId"/>; el API de dev solo acepta tokens de ese proyecto). Siempre false
        /// en el editor y fuera de Android.
        /// </summary>
        public static bool IsConfigured
        {
            get
            {
                if (s_configured == null)
                {
                    s_configured = ReadConfigured(out string? why);
                    if (!s_configured.Value)
                    {
                        Failure = why;
                    }
                }

                return s_configured.Value;
            }
        }

        /// <summary>
        /// Llama a <paramref name="onDone"/> (en el hilo principal) con true cuando Firebase está listo o false si no lo estará en esta
        /// sesión. La primera llamada arranca Firebase. Si ya se sabe, responde en el acto.
        /// </summary>
        public static void WhenReady(Action<bool> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            switch (State)
            {
                case FirebaseBootState.Ready:
                    onDone(true);
                    return;
                case FirebaseBootState.Unavailable:
                    onDone(false);
                    return;
            }

            s_waiting.Add(onDone);
            if (State == FirebaseBootState.NotStarted)
            {
                Start();
            }
        }

        /// <summary>Arranca Firebase si no se ha hecho (no bloquea). Sin configuración, pasa a <see cref="FirebaseBootState.Unavailable"/>.</summary>
        public static void Start()
        {
            if (State != FirebaseBootState.NotStarted)
            {
                return;
            }

            if (!IsConfigured)
            {
                Finish(false, Failure ?? "not_configured");
                return;
            }

            State = FirebaseBootState.Starting;
            StartSdk();
        }

        /// <summary>Texto de fallo para un <c>TokenResult</c>: «firebase_unavailable» más el motivo.</summary>
        internal static string UnavailableReason => "firebase_unavailable:" + (Failure ?? "unknown");

        private static void Finish(bool ok, string? failure)
        {
            State = ok ? FirebaseBootState.Ready : FirebaseBootState.Unavailable;
            Failure = ok ? null : failure;
            var waiting = s_waiting.ToArray();
            s_waiting.Clear();
            for (int i = 0; i < waiting.Length; i++)
            {
                waiting[i](ok);
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void StartSdk()
        {
            try
            {
                // Antes que cualquier otra llamada a Firebase (lo exige App Check). Por debajo, Play Integrity (Q-AWS-4 (a)).
                FirebaseAppCheck.SetAppCheckProviderFactory(PlayIntegrityProviderFactory.Instance);
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        Finish(false, "dependencies_failed");
                        return;
                    }

                    if (task.Result != DependencyStatus.Available)
                    {
                        Finish(false, "dependencies_" + task.Result.ToString());
                        return;
                    }

                    try
                    {
                        FirebaseApp app = FirebaseApp.DefaultInstance;
                        if (app == null)
                        {
                            Finish(false, "no_default_app");
                            return;
                        }

                        // Solo tokens de uso limitado: sin refresco automático no se gasta cuota de Play Integrity en segundo plano.
                        FirebaseAppCheck.DefaultInstance.SetTokenAutoRefreshEnabled(false);
                    }
                    catch (Exception)
                    {
                        Finish(false, "init_failed");
                        return;
                    }

                    Finish(true, null);
                });
            }
            catch (Exception)
            {
                Finish(false, "init_failed");
            }
        }

        private static bool ReadConfigured(out string? why)
        {
            why = null;
            try
            {
                AndroidJavaObject context = UnityEngine.Android.AndroidApplication.currentContext; // de Unity: no se libera aquí
                using (AndroidJavaObject resources = context.Call<AndroidJavaObject>("getResources"))
                {
                    string package = context.Call<string>("getPackageName");
                    string? appId = ReadString(resources, package, "google_app_id");
                    if (string.IsNullOrEmpty(appId))
                    {
                        why = "not_configured"; // sin google-services.json en el build
                        return false;
                    }

                    if (UnityEngine.Debug.isDebugBuild)
                    {
                        // El API de dev solo acepta tokens del proyecto de dev: otra configuración fallaría en cada llamada.
                        string? projectId = ReadString(resources, package, "project_id");
                        if (!string.Equals(appId, DevAndroidAppId, StringComparison.Ordinal)
                            || !string.Equals(projectId, DevProjectId, StringComparison.Ordinal))
                        {
                            why = "config_mismatch";
                            return false;
                        }
                    }

                    return true;
                }
            }
            catch (Exception)
            {
                why = "config_unreadable";
                return false;
            }
        }

        private static string? ReadString(AndroidJavaObject resources, string package, string name)
        {
            int id = resources.Call<int>("getIdentifier", name, "string", package);
            return id == 0 ? null : resources.Call<string>("getString", id);
        }
#else
        private static void StartSdk()
        {
            Finish(false, "editor");
        }

        private static bool ReadConfigured(out string? why)
        {
            why = "editor"; // el editor (ARM64 o x64) y los tests usan las implementaciones falsas (08 §2.1, principio 6)
            return false;
        }
#endif
    }
}
