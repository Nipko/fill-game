#nullable enable
using System;

namespace AF.Save
{
    /// <summary>Lo que dejó <see cref="CloudErase.Resume"/>.</summary>
    public enum CloudEraseResult
    {
        /// <summary>No había borrado pendiente.</summary>
        NothingPending = 0,

        /// <summary>El snapshot está vacío: el borrado consta y la marca se limpia. La sincronización puede seguir.</summary>
        Verified = 1,

        /// <summary>Había datos y se pidió el borrado otra vez: se comprueba en el arranque siguiente. Esta sesión no sincroniza.</summary>
        Requested = 2,

        /// <summary>Sin sesión o sin red: la marca sigue. Esta sesión no sincroniza.</summary>
        Pending = 3,
    }

    /// <summary>
    /// El borrado pendiente de <c>af_main</c> (08 §11, «Eliminación de cuenta»; nota B8 §8; D-042): si el borrado de la cuenta no
    /// pudo borrar la nube (sin sesión, sin red, o Play no confirma su <c>delete</c>), el arranque siguiente descargaría la cuenta
    /// borrada. <see cref="ICloudSaveFlags.DeletePending"/> lo impide: mientras siga, <see cref="Resume"/> va antes de
    /// <see cref="CloudSync.AtStartup"/> y la sesión no fusiona ni sube nada hasta comprobar que el snapshot está vacío.
    /// </summary>
    public static class CloudErase
    {
        /// <summary>Retoma el borrado pendiente con <paramref name="cloud"/> (con sesión) y llama a <paramref name="done"/>.</summary>
        public static void Resume(ICloudSave cloud, ICloudSaveFlags flags, Action<CloudEraseResult> done)
        {
            if (cloud == null)
            {
                throw new ArgumentNullException(nameof(cloud));
            }

            if (flags == null)
            {
                throw new ArgumentNullException(nameof(flags));
            }

            if (done == null)
            {
                throw new ArgumentNullException(nameof(done));
            }

            if (!flags.DeletePending)
            {
                done(CloudEraseResult.NothingPending);
                return;
            }

            if (!cloud.HasSession)
            {
                done(CloudEraseResult.Pending);
                return;
            }

            cloud.Load(loaded =>
            {
                switch (loaded.Status)
                {
                    case CloudLoadStatus.NotFound:
                        flags.DeletePending = false;
                        done(CloudEraseResult.Verified);
                        return;
                    case CloudLoadStatus.Loaded:
                        cloud.Delete(result => done(result == CloudWriteResult.Ok ? CloudEraseResult.Requested : CloudEraseResult.Pending));
                        return;
                    default:
                        done(CloudEraseResult.Pending);
                        return;
                }
            });
        }
    }
}
