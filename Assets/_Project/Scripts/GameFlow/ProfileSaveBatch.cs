#nullable enable
using System;
using System.Diagnostics;
using AF.Save;

namespace AF.GameFlow
{
    /// <summary>
    /// Una sola escritura del perfil por acción del jugador (revisión de B2-5; 02 R8 y §14; 08 §5.4). La cartera, el progreso,
    /// el nivel en curso, la tienda y los anuncios guardan el mismo <see cref="Profile"/> con su <see cref="IProfileStore"/>;
    /// este almacén envuelve el de verdad y, dentro de un lote (<see cref="Begin"/>), solo anota que hay cambios: el perfil se
    /// escribe una vez al cerrar el lote más externo. Así:
    /// <list type="bullet">
    /// <item><description>la victoria (borrar <c>level_state</c>, progreso, Cosecha y cadencia de intersticiales) es una
    /// escritura, no cuatro;</description></item>
    /// <item><description>comprar el rescate o rendirse cobra y anota la pantalla nueva en la misma escritura: un cierre entre
    /// el cobro y <c>level_state</c> ya no puede reabrir el embudo y cobrar otra vez.</description></item>
    /// </list>
    /// Fuera de un lote pasa cada guardado tal cual (y lanza sus excepciones, como espera <c>AF.Economy.Wallet</c>). Si la
    /// escritura del cierre falla, el perfil queda pendiente (<see cref="PendingSave"/>) y sale con el siguiente guardado o con
    /// <see cref="Flush"/>; como todo comparte el mismo perfil, cualquier escritura posterior lo lleva entero. Un solo hilo.
    /// <para>
    /// Dentro de un lote, <see cref="Save"/> solo anota y nunca falla: quien guarda a través del lote (la cartera, el
    /// progreso, <c>level_state</c>, los registros de anuncios y de la tienda) ve un guardado correcto aunque la escritura del
    /// cierre falle después. Así, <c>TransactionResult.Saved</c> = true y <c>Wallet.PendingSave</c> = false significan
    /// «anotado en el lote»; el único que sabe si llegó al disco es <see cref="PendingSave"/> tras cerrar el lote. No se
    /// pierde nada: lo pendiente sale con la siguiente escritura o con <see cref="Flush"/> (<c>OnApplicationPause</c>).
    /// </para>
    /// </summary>
    public sealed class ProfileSaveBatch : IProfileStore
    {
        private readonly IProfileStore _inner;
        private int _depth;
        private Profile? _pending;

        /// <summary>Envuelve <paramref name="inner"/>.</summary>
        public ProfileSaveBatch(IProfileStore inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>El almacén de verdad.</summary>
        public IProfileStore Inner => _inner;

        /// <summary>Hay un lote abierto.</summary>
        public bool InBatch => _depth > 0;

        /// <summary>Hay cambios sin escribir (lote abierto o escritura fallida).</summary>
        public bool PendingSave => _pending != null;

        /// <summary>Escrituras pedidas al almacén de verdad (diagnóstico: una por acción del jugador).</summary>
        public int WriteCount { get; private set; }

        /// <summary>Lo que tardó la última escritura al almacén de verdad, en ms (incluye el JSON y el disco).</summary>
        public double LastWriteMs { get; private set; }

        /// <summary>
        /// Abre un lote: hasta que se cierre (<see cref="ProfileSaveScope.Dispose"/>), los guardados solo se anotan. Los lotes se
        /// anidan; escribe el cierre del más externo. Para usar con <c>using</c>: sin asignaciones.
        /// </summary>
        public ProfileSaveScope Begin()
        {
            _depth++;
            return new ProfileSaveScope(this);
        }

        /// <inheritdoc />
        public ProfileLoadResult Load()
        {
            return _inner.Load();
        }

        /// <inheritdoc />
        public void Save(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (_depth > 0)
            {
                _pending = profile;
                return;
            }

            Write(profile);
            if (ReferenceEquals(_pending, profile))
            {
                _pending = null;
            }
        }

        /// <inheritdoc />
        public void Delete()
        {
            _pending = null;
            _inner.Delete();
        }

        /// <summary>
        /// Fuera de un lote, reintenta la escritura pendiente (al pasar a segundo plano). Devuelve si no queda nada por escribir.
        /// </summary>
        public bool Flush()
        {
            if (_depth == 0 && _pending != null)
            {
                TryWritePending();
            }

            return _pending == null;
        }

        internal void End()
        {
            if (_depth == 0)
            {
                return;
            }

            _depth--;
            if (_depth == 0 && _pending != null)
            {
                TryWritePending();
            }
        }

        private void TryWritePending()
        {
            Profile profile = _pending!;
            try
            {
                Write(profile);
                _pending = null;
            }
            catch (System.IO.IOException)
            {
                // Sigue pendiente: sale con el siguiente guardado o con Flush (02 §14: lo peor es reabrir un paso antes).
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (InvalidOperationException)
            {
                // El almacén protege un perfil más nuevo o una migración fallida (IProfileStore.Save): se sigue en memoria.
            }
        }

        private void Write(Profile profile)
        {
            long start = Stopwatch.GetTimestamp();
            WriteCount++;
            try
            {
                _inner.Save(profile);
            }
            finally
            {
                LastWriteMs = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            }
        }
    }

    /// <summary>Lote abierto con <see cref="ProfileSaveBatch.Begin"/>; se cierra con <see cref="Dispose"/> (una vez).</summary>
    public readonly struct ProfileSaveScope : IDisposable
    {
        private readonly ProfileSaveBatch? _batch;

        internal ProfileSaveScope(ProfileSaveBatch batch)
        {
            _batch = batch;
        }

        /// <summary>Cierra el lote; el más externo escribe el perfil si hubo cambios. El valor por defecto no hace nada.</summary>
        public void Dispose()
        {
            _batch?.End();
        }
    }
}
