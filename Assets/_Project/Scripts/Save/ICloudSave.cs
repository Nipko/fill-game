#nullable enable
using System;

namespace AF.Save
{
    /// <summary>
    /// Guardado en la nube del perfil (GDD 08 §11, fila «Nube Android 1.0»: Play Games Services v2 Saved Games, un solo snapshot
    /// <c>af_main</c>, sin portada; 04 §10.2; bloque B8). La real es <see cref="PlayGamesCloudSave"/> (D-042: Play Games plugin
    /// for Unity 2.3.0, solo en el teléfono y con el <c>APP_ID</c> de Play Console; <c>docs/dev/play-games.md</c>); el editor, los
    /// tests y el build sin configurar usan <see cref="FakeCloudSave"/> (08 §2.1 principio 6). El blob es el JSON compartido del
    /// perfil en UTF-8 (<see cref="CloudMerge.SharedJson"/>, sin lo que es solo del dispositivo; <see cref="ProfileJson"/>,
    /// &lt; 100 KB, 08 §4.3). Las operaciones son asíncronas en el SDK: terminan llamando a su <c>done</c> en el hilo principal
    /// (la falsa, en el acto). Quién gana un conflicto lo decide <see cref="CloudMerge"/>, nunca el SDK (08 §11, «Conflicto»:
    /// resolución manual, no <c>UseLongestPlaytime</c>).
    /// </summary>
    public interface ICloudSave
    {
        /// <summary>Hay sesión de Play Games (sin ella no se lee ni se escribe la nube; 08 §11: «descarga al arrancar con sesión»).</summary>
        bool HasSession { get; }

        /// <summary>Lee el snapshot <see cref="CloudSaveNames.Snapshot"/>.</summary>
        void Load(Action<CloudLoadResult> done);

        /// <summary>Escribe <paramref name="blob"/> en el snapshot (lo sustituye entero).</summary>
        void Save(byte[] blob, Action<CloudWriteResult> done);

        /// <summary>Borra el snapshot (Eliminar cuenta y datos, 08 §11). Borrar uno que no existe es un éxito.</summary>
        void Delete(Action<CloudWriteResult> done);
    }

    /// <summary>Nombres fijos del guardado en la nube (08 §11).</summary>
    public static class CloudSaveNames
    {
        /// <summary>El único snapshot de Saved Games: <c>af_main</c>.</summary>
        public const string Snapshot = "af_main";

        /// <summary>Tope del blob que acepta Saved Games sin portada (08 §11: 800 KB [V]); el perfil apunta a &lt; 100 KB (08 §4.3).</summary>
        public const int MaxBlobBytes = 800 * 1024;
    }

    /// <summary>Qué pasó al leer la nube.</summary>
    public enum CloudLoadStatus
    {
        /// <summary>Sin sesión de Play Games: no se leyó nada.</summary>
        NoSession = 0,

        /// <summary>Con sesión, pero aún no hay snapshot (primer guardado en la nube).</summary>
        NotFound = 1,

        /// <summary>Se leyó el snapshot.</summary>
        Loaded = 2,

        /// <summary>Error de red o del servicio: se sigue con el perfil local y se reintenta más tarde.</summary>
        Failed = 3,
    }

    /// <summary>Lo que devuelve <see cref="ICloudSave.Load"/>.</summary>
    public sealed class CloudLoadResult
    {
        /// <summary>Crea un resultado (solo <see cref="CloudLoadStatus.Loaded"/> lleva blob).</summary>
        public CloudLoadResult(CloudLoadStatus status, byte[]? blob = null, string? error = null)
        {
            if ((status == CloudLoadStatus.Loaded) != (blob != null))
            {
                throw new ArgumentException("Solo Loaded lleva blob.", nameof(blob));
            }

            Status = status;
            Blob = blob;
            Error = error;
        }

        /// <summary>Qué pasó.</summary>
        public CloudLoadStatus Status { get; }

        /// <summary>El snapshot (con <see cref="CloudLoadStatus.Loaded"/>).</summary>
        public byte[]? Blob { get; }

        /// <summary>Descripción del error, para el registro; null si no hubo.</summary>
        public string? Error { get; }
    }

    /// <summary>Lo que devuelven <see cref="ICloudSave.Save"/> y <see cref="ICloudSave.Delete"/>.</summary>
    public enum CloudWriteResult
    {
        /// <summary>Hecho.</summary>
        Ok = 0,

        /// <summary>Sin sesión: no se hizo nada.</summary>
        NoSession = 1,

        /// <summary>Error de red o del servicio.</summary>
        Failed = 2,

        /// <summary>El blob pasa de <see cref="CloudSaveNames.MaxBlobBytes"/>: no se subió.</summary>
        TooLarge = 3,
    }
}
