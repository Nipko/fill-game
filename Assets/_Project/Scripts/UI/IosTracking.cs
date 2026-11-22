#nullable enable
using System;
using AF.Platform;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
using AOT;
#endif

namespace AF.UI
{
    /// <summary>Estado del aviso de rastreo de Apple (<c>ATTrackingManagerAuthorizationStatus</c>).</summary>
    public enum TrackingStatus
    {
        /// <summary>Aún sin decidir: el aviso del sistema se puede enseñar (una sola vez).</summary>
        NotDetermined = 0,

        /// <summary>Restringido (controles parentales o perfil de gestión): no se pregunta.</summary>
        Restricted = 1,

        /// <summary>El jugador dijo que no.</summary>
        Denied = 2,

        /// <summary>El jugador dijo que sí: los anuncios pueden usar el IDFA.</summary>
        Authorized = 3,
    }

    /// <summary>El aviso de rastreo de Apple (ATT): el estado y la petición, que responde una vez en el hilo principal.</summary>
    public interface ITrackingPort
    {
        TrackingStatus Status { get; }

        void Request(Action<TrackingStatus> done);
    }

    /// <summary>
    /// El aviso de rastreo de Apple en iOS (fase M3 de <c>docs/dev/traspaso/mac-ios.md</c>; I-5 de D-067: plugin propio
    /// <c>Plugins/iOS/AfAtt.m</c>). <see cref="ShouldAsk"/> es la regla, sin motor: solo con la edad decidida, 18+ y anuncios
    /// personalizados (por nuestra pantalla o por UMP), y solo mientras iOS no lo haya preguntado. Con 13-17, &lt;13 o <c>npa</c> nunca se
    /// pide: sin permiso, iOS no da el IDFA y Google Mobile Ads no rastrea. Antes del aviso del sistema, el juego enseña el suyo
    /// (<see cref="AttPrePromptCard"/>).
    /// </summary>
    public static class IosTracking
    {
        /// <summary>¿Se enseña el aviso previo y luego el del sistema?</summary>
        public static bool ShouldAsk(ConsentFlags flags, TrackingStatus status)
        {
            return ShouldAsk(status, flags.Decided, flags.Blocked, flags.Band, flags.AdsPersonalized || flags.AdsByUmp);
        }

        /// <summary>La regla con los flags sueltos (tests): sin decidir, con el bloqueo, fuera de 18+ o con <c>npa</c>, nunca.</summary>
        public static bool ShouldAsk(TrackingStatus status, bool decided, bool blocked, AgeBand band, bool personalizedAds)
        {
            return status == TrackingStatus.NotDetermined && decided && !blocked && band == AgeBand.Adult && personalizedAds;
        }

        /// <summary>El aviso real en el iPhone; null en el resto (Android, el editor y los tests).</summary>
        public static ITrackingPort? ForPlatform()
        {
            return Application.platform == RuntimePlatform.IPhonePlayer ? new IosTrackingPort() : null;
        }

        private sealed class IosTrackingPort : ITrackingPort
        {
#if UNITY_IOS && !UNITY_EDITOR
            private delegate void Callback(int status);

            [DllImport("__Internal")] private static extern int AfAttStatus();
            [DllImport("__Internal")] private static extern void AfAttRequest(Callback done);

            private static readonly Callback s_callback = OnAnswer;
            private static Action<TrackingStatus>? s_pending;

            public TrackingStatus Status => (TrackingStatus)AfAttStatus();

            public void Request(Action<TrackingStatus> done)
            {
                s_pending = done ?? throw new ArgumentNullException(nameof(done));
                AfAttRequest(s_callback);
            }

            [MonoPInvokeCallback(typeof(Callback))]
            private static void OnAnswer(int status)
            {
                Action<TrackingStatus>? done = s_pending;
                s_pending = null;
                done?.Invoke((TrackingStatus)status);
            }
#else
            public TrackingStatus Status => TrackingStatus.Restricted;

            public void Request(Action<TrackingStatus> done) => done(TrackingStatus.Restricted);
#endif
        }
    }

    /// <summary><see cref="ITrackingPort"/> de los tests: el estado que se le da y la respuesta que se le pida.</summary>
    public sealed class FakeTrackingPort : ITrackingPort
    {
        public FakeTrackingPort(TrackingStatus status = TrackingStatus.NotDetermined, TrackingStatus answer = TrackingStatus.Authorized)
        {
            Status = status;
            Answer = answer;
        }

        public TrackingStatus Status { get; private set; }

        /// <summary>Lo que responde el jugador al aviso del sistema.</summary>
        public TrackingStatus Answer { get; set; }

        /// <summary>Veces que se pidió el aviso del sistema.</summary>
        public int Requests { get; private set; }

        public void Request(Action<TrackingStatus> done)
        {
            Requests++;
            if (Status == TrackingStatus.NotDetermined)
            {
                Status = Answer;
            }

            done(Status);
        }
    }
}
