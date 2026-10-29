#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// El lector de pantalla del juego (bloque B9; 02 §12, fila «Lector de pantalla»; 07 §7.8): con TalkBack encendido, cada
    /// <see cref="RefreshMs"/> ms (o en el siguiente fotograma tras <see cref="Invalidate"/>) recorre la interfaz
    /// (<see cref="A11yScanner"/>, con las fuentes propias como la del tablero) y, si cambió algo, lo publica en el
    /// <see cref="IScreenReaderHost"/>; los anuncios (la selección) van directos. Con el lector apagado no hace nada (coste cero
    /// por fotograma), y al apagarlo quita lo publicado. Lo crea y lo avanza <c>GameBootstrap</c> (parche <c>B9-ui.diff</c>).
    /// En cada recorrido mira también si lo publicado se movió en la pantalla sin cambiar (una lista que se desplaza con el dedo, el
    /// Mapa arrastrado): entonces pide al anfitrión que vuelva a leer los marcos (<see cref="IScreenReaderHost.RefreshFrames"/>;
    /// observación 7 de la jornada 6, la Liga con TalkBack), en el acto; y, cuando un recorrido ya no ve movimiento tras otro que
    /// sí, le pide asentar el foco (<see cref="IScreenReaderHost.SettleFocus"/>): el foco no salta de fila mientras la lista corre.
    /// </summary>
    public sealed class A11yService
    {
        /// <summary>Cada cuánto se vuelve a mirar la interfaz con el lector encendido (ms) [P].</summary>
        public const float RefreshMs = 250f;

        /// <summary>Lo que tiene que moverse un marco (px) para volver a leer los marcos [P].</summary>
        public const float MoveTolerancePx = 0.5f;

        private readonly IScreenReaderHost _host;
        private readonly A11yScanner _scanner;
        private readonly List<A11yItem> _scan = new List<A11yItem>();
        private readonly List<A11yItem> _published = new List<A11yItem>();
        private readonly List<Rect> _frames = new List<Rect>();
        private float _sinceMs = float.MaxValue;
        private bool _settlePending;
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

        /// <summary>Veces que se pidió volver a leer los marcos porque lo publicado se movió (tests).</summary>
        public int FrameRefreshes { get; private set; }

        /// <summary>Hay que asentar el foco cuando lo publicado deje de moverse (tests).</summary>
        public bool SettlePending => _settlePending;

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
                    _frames.Clear();
                    _settlePending = false;
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

        /// <summary>
        /// Recorre la interfaz ahora y publica si cambió algo (también sin lector: tests); si lo publicado se movió en la pantalla,
        /// pide volver a leer los marcos. Devuelve si publicó.
        /// </summary>
        public bool Refresh()
        {
            _scanner.Scan(_scan);
            bool publish = !Same(_scan, _published);
            if (publish)
            {
                _published.Clear();
                _published.AddRange(_scan);
                _host.Publish(_published);
                Publishes++;
            }

            if (FramesMoved(publish))
            {
                _host.RefreshFrames(); // en el acto: el lector toca y lee lo que se ve ya
                FrameRefreshes++;
                _settlePending = true;
            }
            else if (_settlePending && _host.SettleFocus())
            {
                _settlePending = false; // dos recorridos seguidos (el que movió y este, quieto): el foco sigue a lo que se ve
            }

            return publish;
        }

        // Guarda el marco de ahora de cada elemento publicado y dice si alguno se movió desde el último recorrido. Tras una
        // publicación con otro número de elementos la jerarquía es nueva (sus marcos, recién leídos): solo se guardan.
        private bool FramesMoved(bool published)
        {
            bool fresh = published && _frames.Count != _published.Count;
            bool moved = false;
            for (int i = 0; i < _published.Count; i++)
            {
                Rect frame = _published[i].CurrentFrame();
                if (i >= _frames.Count)
                {
                    _frames.Add(frame);
                    continue;
                }

                Rect before = _frames[i];
                if (Mathf.Abs(frame.x - before.x) > MoveTolerancePx || Mathf.Abs(frame.y - before.y) > MoveTolerancePx
                    || Mathf.Abs(frame.width - before.width) > MoveTolerancePx || Mathf.Abs(frame.height - before.height) > MoveTolerancePx)
                {
                    moved = true;
                }

                _frames[i] = frame;
            }

            if (_frames.Count > _published.Count)
            {
                _frames.RemoveRange(_published.Count, _frames.Count - _published.Count);
            }

            return moved && !fresh;
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
