#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Events
{
    /// <summary>
    /// Bus mínimo de eventos tipados, síncrono y en memoria (GDD 08 §2.1, principio 3; D-033, Q-T1 (a)): publican
    /// <c>AF.GameFlow</c> y <c>AF.Economy</c>, y consume <c>AF.Analytics</c>. Sin motor y sin dependencias: los eventos son
    /// estructuras de <see cref="AF.Events"/> con datos simples, así que ningún módulo de abajo conoce a quien escucha.
    /// <para>
    /// Cada tipo de evento tiene su canal; <see cref="Publish{T}"/> llama a los oyentes en el orden en que se suscribieron, en
    /// el mismo hilo y antes de volver. Suscribirse o darse de baja dentro de un oyente vale para la publicación siguiente. Un
    /// oyente que lanza una excepción no corta a los demás ni al que publica: la excepción va a <see cref="HandlerFailed"/>
    /// (si no hay nadie, se pierde) [P], porque un fallo de la analítica nunca puede romper una victoria ni un cobro.
    /// </para>
    /// Solo desde el hilo principal. Publicar un evento sin oyentes no asigna memoria.
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, object> _channels = new Dictionary<Type, object>();

        /// <summary>Un oyente lanzó una excepción (el tipo del evento y la excepción). Para el registro de errores.</summary>
        public Action<Type, Exception>? HandlerFailed { get; set; }

        /// <summary>Suscribe <paramref name="handler"/> a los eventos de tipo <typeparamref name="T"/>. Dos veces = dos llamadas.</summary>
        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Channel<T>.Of(this).Add(handler);
        }

        /// <summary>Quita la última suscripción de <paramref name="handler"/> a <typeparamref name="T"/>. False si no estaba.</summary>
        public bool Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return _channels.TryGetValue(typeof(T), out object? channel) && ((Channel<T>)channel).Remove(handler);
        }

        /// <summary>Oyentes de <typeparamref name="T"/>.</summary>
        public int CountOf<T>() where T : struct
        {
            return _channels.TryGetValue(typeof(T), out object? channel) ? ((Channel<T>)channel).Count : 0;
        }

        /// <summary>Entrega <paramref name="e"/> a los oyentes de <typeparamref name="T"/>, en orden de suscripción.</summary>
        public void Publish<T>(T e) where T : struct
        {
            if (!_channels.TryGetValue(typeof(T), out object? channel))
            {
                return;
            }

            Action<T>[] handlers = ((Channel<T>)channel).Snapshot;
            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    handlers[i](e);
                }
                catch (Exception ex)
                {
                    HandlerFailed?.Invoke(typeof(T), ex);
                }
            }
        }

        // Canal de un tipo: un arreglo que se copia al cambiar (las publicaciones, mucho más frecuentes, no copian nada).
        private sealed class Channel<T> where T : struct
        {
            private static readonly Action<T>[] Empty = new Action<T>[0];

            public Action<T>[] Snapshot { get; private set; } = Empty;

            public int Count => Snapshot.Length;

            public static Channel<T> Of(EventBus bus)
            {
                if (!bus._channels.TryGetValue(typeof(T), out object? channel))
                {
                    channel = new Channel<T>();
                    bus._channels.Add(typeof(T), channel);
                }

                return (Channel<T>)channel;
            }

            public void Add(Action<T> handler)
            {
                var next = new Action<T>[Snapshot.Length + 1];
                Array.Copy(Snapshot, next, Snapshot.Length);
                next[Snapshot.Length] = handler;
                Snapshot = next;
            }

            public bool Remove(Action<T> handler)
            {
                for (int i = Snapshot.Length - 1; i >= 0; i--)
                {
                    if (Snapshot[i] != handler)
                    {
                        continue;
                    }

                    var next = new Action<T>[Snapshot.Length - 1];
                    Array.Copy(Snapshot, 0, next, 0, i);
                    Array.Copy(Snapshot, i + 1, next, i, Snapshot.Length - i - 1);
                    Snapshot = next;
                    return true;
                }

                return false;
            }
        }
    }
}
