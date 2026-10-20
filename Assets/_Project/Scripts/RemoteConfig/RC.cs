#nullable enable
using System;
using System.Collections.Generic;

namespace AF.RemoteConfig
{
    /// <summary>
    /// Acceso tipado <c>RC.Get&lt;T&gt;(key)</c> de 08 §2.2 sobre el servicio del juego. Sin <see cref="Use"/>, lee los defaults
    /// compilados (así los tests y el código sin arranque ven siempre los valores de 08 §9). El arranque llama a
    /// <see cref="Use"/> con el servicio de la sesión; los tests, a <see cref="Reset"/> al terminar.
    /// </summary>
    public static class RC
    {
        private static RemoteConfigService? s_service;

        /// <summary>El servicio vigente (el de los defaults compilados si nadie llamó a <see cref="Use"/>).</summary>
        public static RemoteConfigService Service => s_service ??= new RemoteConfigService();

        /// <summary>Usa <paramref name="service"/> desde ahora.</summary>
        public static void Use(RemoteConfigService service)
        {
            s_service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>Vuelve a los defaults compilados.</summary>
        public static void Reset()
        {
            s_service = null;
        }

        /// <summary>El valor vigente de <paramref name="key"/> (ver <see cref="RcSnapshot.Get{T}"/>).</summary>
        public static T Get<T>(string key) => Service.Get<T>(key);

        /// <summary>Congela los valores vigentes al abrir una pantalla (04 §10.2).</summary>
        public static RcSnapshot Freeze() => Service.Freeze();
    }

    /// <summary>
    /// Congelación por pantalla (08 §2.2; 04 §10.2): <see cref="Open"/> guarda los valores vigentes al abrir la pantalla
    /// <paramref name="screen"/> y <see cref="For"/> los devuelve mientras siga abierta, aunque Remote Config active valores
    /// nuevos; <see cref="Close"/> la suelta. Una pantalla que se vuelve a abrir lee los valores de ese momento.
    /// </summary>
    public sealed class RcScreenFreezer
    {
        private readonly RemoteConfigService _service;
        private readonly Dictionary<string, RcSnapshot> _open = new Dictionary<string, RcSnapshot>(StringComparer.Ordinal);

        /// <summary>Crea el congelador sobre <paramref name="service"/>.</summary>
        public RcScreenFreezer(RemoteConfigService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>Abre (o vuelve a abrir) <paramref name="screen"/> con los valores de ahora.</summary>
        public RcSnapshot Open(string screen)
        {
            RcSnapshot snapshot = _service.Freeze();
            _open[screen ?? throw new ArgumentNullException(nameof(screen))] = snapshot;
            return snapshot;
        }

        /// <summary>Los valores congelados de <paramref name="screen"/> si está abierta; si no, los vigentes.</summary>
        public RcSnapshot For(string screen)
        {
            return screen != null && _open.TryGetValue(screen, out RcSnapshot? s) ? s : _service.Current;
        }

        /// <summary><paramref name="screen"/> está abierta.</summary>
        public bool IsOpen(string screen) => screen != null && _open.ContainsKey(screen);

        /// <summary>Cierra <paramref name="screen"/>. False si no estaba abierta.</summary>
        public bool Close(string screen) => screen != null && _open.Remove(screen);
    }
}
