#nullable enable
using System;

namespace AF.Save
{
    /// <summary>
    /// <see cref="ICloudSave"/> en memoria, para el editor y los tests (08 §2.1 principio 6; bloque B8): sin red ni SDK. Guarda una
    /// copia de los bytes (no la referencia), responde en el acto y deja simular la falta de sesión (<see cref="HasSession"/>) y un
    /// fallo del servicio (<see cref="FailNext"/>). En la 1.0 sin Play Games el juego la usa sin sesión
    /// (<see cref="WithoutSession"/>): nada sube ni baja.
    /// </summary>
    public sealed class FakeCloudSave : ICloudSave
    {
        private byte[]? _blob;

        /// <summary>Crea la nube con sesión y, si se da, con <paramref name="initial"/> ya subido.</summary>
        public FakeCloudSave(byte[]? initial = null)
        {
            _blob = initial == null ? null : (byte[])initial.Clone();
            HasSession = true;
        }

        /// <summary>Una nube sin sesión (la de la 1.0 mientras no haya Play Games).</summary>
        public static FakeCloudSave WithoutSession()
        {
            return new FakeCloudSave { HasSession = false };
        }

        /// <inheritdoc />
        public bool HasSession { get; set; }

        /// <summary>La siguiente operación falla (<see cref="CloudLoadStatus.Failed"/> o <see cref="CloudWriteResult.Failed"/>) y se apaga.</summary>
        public bool FailNext { get; set; }

        /// <summary>Copia del snapshot guardado, o null si no hay.</summary>
        public byte[]? Blob => _blob == null ? null : (byte[])_blob.Clone();

        /// <summary>Lecturas pedidas.</summary>
        public int LoadCount { get; private set; }

        /// <summary>Escrituras hechas con éxito.</summary>
        public int SaveCount { get; private set; }

        /// <summary>Borrados hechos con éxito.</summary>
        public int DeleteCount { get; private set; }

        /// <inheritdoc />
        public void Load(Action<CloudLoadResult> done)
        {
            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            LoadCount++;
            if (!HasSession)
            {
                done(new CloudLoadResult(CloudLoadStatus.NoSession));
                return;
            }

            if (TakeFailure())
            {
                done(new CloudLoadResult(CloudLoadStatus.Failed, error: "fallo simulado"));
                return;
            }

            done(_blob == null ? new CloudLoadResult(CloudLoadStatus.NotFound) : new CloudLoadResult(CloudLoadStatus.Loaded, (byte[])_blob.Clone()));
        }

        /// <inheritdoc />
        public void Save(byte[] blob, Action<CloudWriteResult> done)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            if (!HasSession)
            {
                done(CloudWriteResult.NoSession);
                return;
            }

            if (TakeFailure())
            {
                done(CloudWriteResult.Failed);
                return;
            }

            if (blob.Length > CloudSaveNames.MaxBlobBytes)
            {
                done(CloudWriteResult.TooLarge);
                return;
            }

            _blob = (byte[])blob.Clone();
            SaveCount++;
            done(CloudWriteResult.Ok);
        }

        /// <inheritdoc />
        public void Delete(Action<CloudWriteResult> done)
        {
            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            if (!HasSession)
            {
                done(CloudWriteResult.NoSession);
                return;
            }

            if (TakeFailure())
            {
                done(CloudWriteResult.Failed);
                return;
            }

            _blob = null;
            DeleteCount++;
            done(CloudWriteResult.Ok);
        }

        private bool TakeFailure()
        {
            bool fail = FailNext;
            FailNext = false;
            return fail;
        }
    }
}
