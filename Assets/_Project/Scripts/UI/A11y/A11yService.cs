#nullable enable
using System;
using System.Collections.Generic;

namespace AF.UI
{
    /// <summary>
    /// El lector de pantalla del juego (bloque B9; 02 §12, fila «Lector de pantalla»; 07 §7.8): con TalkBack encendido, cada
    /// <see cref="RefreshMs"/> ms (o en el siguiente fotograma tras <see cref="Invalidate"/>) recorre la interfaz
    /// (<see cref="A11yScanner"/>, con las fuentes propias como la del tablero) y, si cambió algo, lo publica en el
    /// <see cref="IScreenReaderHost"/>; los anuncios (la selección) van directos. Con el lector apagado no hace nada (coste cero
    /// por fotograma), y al apagarlo quita lo publicado. Lo crea y lo avanza <c>GameBootstrap</c> (parche <c>B9-ui.diff</c>).
    /// </summary>
    public sealed class A11yService
    {
        /// <summary>Cada cuánto se vuelve a mirar la interfaz con el lector encendido (ms) [P].</summary>
        public const float RefreshMs = 250f;

        private readonly IScreenReaderHost _host;
        private readonly A11yScanner _scanner;
        private readonly List<A11yItem> _scan = new List<A11yItem>();
        private readonly List<A11yItem> _published = new List<A11yItem>();
        private float _sinceMs = float.MaxValue;
        private bool _wasOn;

        /// <summary>Crea el servicio sobre <paramref name="host"/> (el real, <see cref="UnityScreenReaderHost"/>, o el falso).</summary>
        public A11yService(IScreenReaderHost host, A11yScanner? scanner = null)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _scanner = scanner ?? new A11yScanner();
        }

        /// <summary>El escáner (para añadir fuentes propias).</summary>
        public A11yScanner Scanner => _scanner;

        /// <summary>El anfitrión.</summary>
        public IScreenReaderHost Host => _host;

        /// <summary>El lector de pantalla está encendido.</summary>
        public bool On => _host.ScreenReaderOn;

        /// <summary>Lo publicado ahora, en el orden del foco.</summary>
        public IReadOnlyList<A11yItem> Published => _published;

        /// <summary>Veces que se ha publicado algo (tests).</summary>
        public int Publishes { get; private set; }

        /// <summary>Pide recorrer la interfaz en el siguiente <see cref="Tick"/> (cambió el tablero, se abrió una pantalla…).</summary>
        public void Invalidate()
        {
            _sinceMs = float.MaxValue;
        }

        /// <summary>Dice <paramref name="text"/> si el lector está encendido (el anuncio de la selección, 02 §12).</summary>
        public void Announce(string text)
        {
            if (_host.ScreenReaderOn && !string.IsNullOrEmpty(text))
            {
                _host.Announce(text);
            }
        }

        /// <summary>Avanza <paramref name="dtMs"/> ms: con el lector encendido, recorre y publica si toca.</summary>
        public void Tick(float dtMs)
        {
            bool on = _host.ScreenReaderOn;
            if (!on)
            {
                if (_wasOn)
                {
                    _host.Clear();
                    _published.Clear();
                    _wasOn = false;
                }

                return;
            }

            if (!_wasOn)
            {
                _wasOn = true;
                _sinceMs = float.MaxValue;
            }

            if (_sinceMs != float.MaxValue)
            {
                _sinceMs += Math.Max(0f, dtMs);
            }

            if (_sinceMs < RefreshMs)
            {
                return;
            }

            _sinceMs = 0f;
            Refresh();
        }

        /// <summary>Recorre la interfaz ahora y publica si cambió algo (también sin lector: tests). Devuelve si publicó.</summary>
        public bool Refresh()
        {
            _scanner.Scan(_scan);
            if (Same(_scan, _published))
            {
                return false;
            }

            _published.Clear();
            _published.AddRange(_scan);
            _host.Publish(_published);
            Publishes++;
            return true;
        }

        private static bool Same(List<A11yItem> a, List<A11yItem> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].SameAs(b[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
