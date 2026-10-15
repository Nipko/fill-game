#nullable enable
using System;

namespace AF.Localization
{
    /// <summary>
    /// Estado de un texto en las tablas de textos (08 §4.7; contrato 32; propuesta de idiomas §3.3 y §3.10):
    /// <c>borrador</c> → <c>pendiente_asesor</c> → <c>aprobado_es</c> en el texto fuente y, por idioma, <c>traducido</c> →
    /// <c>revisado_nativo</c>. El orden de los valores es el del flujo, así que se pueden comparar (≥ <see cref="ApprovedEs"/>).
    /// </summary>
    public enum LocStatus : byte
    {
        /// <summary>No hay clave o no hay texto en ese idioma (una traducción que falta queda vacía).</summary>
        Missing = 0,

        /// <summary><c>borrador</c>, o la celda de estado vacía con texto escrito.</summary>
        Draft = 1,

        /// <summary><c>pendiente_asesor</c>: todo texto cultural nace así (regla 9 de la propuesta de idiomas, §3.2).</summary>
        PendingAdvisor = 2,

        /// <summary><c>aprobado_es</c>: el asesor cultural aprobó el texto fuente en español.</summary>
        ApprovedEs = 3,

        /// <summary><c>traducido</c>: hay traducción, aún sin revisión nativa.</summary>
        Translated = 4,

        /// <summary><c>revisado_nativo</c>: la traducción la revisó un nativo (D-018, Q5-A).</summary>
        NativeReviewed = 5,
    }

    /// <summary>Nombres de <see cref="LocStatus"/> tal como se escriben en las columnas de estado de los CSV.</summary>
    public static class LocStatusNames
    {
        /// <summary><see cref="LocStatus.Draft"/>.</summary>
        public const string Draft = "borrador";

        /// <summary><see cref="LocStatus.PendingAdvisor"/>.</summary>
        public const string PendingAdvisor = "pendiente_asesor";

        /// <summary><see cref="LocStatus.ApprovedEs"/>.</summary>
        public const string ApprovedEs = "aprobado_es";

        /// <summary><see cref="LocStatus.Translated"/>.</summary>
        public const string Translated = "traducido";

        /// <summary><see cref="LocStatus.NativeReviewed"/>.</summary>
        public const string NativeReviewed = "revisado_nativo";

        /// <summary>
        /// Lee uno de los cinco nombres del flujo (exacto, en minúsculas). Una celda vacía no es un nombre: la tabla la trata
        /// como <see cref="LocStatus.Draft"/>.
        /// </summary>
        public static bool TryParse(string? text, out LocStatus status)
        {
            switch (text)
            {
                case Draft:
                    status = LocStatus.Draft;
                    return true;
                case PendingAdvisor:
                    status = LocStatus.PendingAdvisor;
                    return true;
                case ApprovedEs:
                    status = LocStatus.ApprovedEs;
                    return true;
                case Translated:
                    status = LocStatus.Translated;
                    return true;
                case NativeReviewed:
                    status = LocStatus.NativeReviewed;
                    return true;
                default:
                    status = LocStatus.Missing;
                    return false;
            }
        }

        /// <summary>Nombre del estado en el CSV; <see cref="LocStatus.Missing"/> no tiene nombre (cadena vacía).</summary>
        public static string ToCsv(LocStatus status)
        {
            switch (status)
            {
                case LocStatus.Missing: return string.Empty;
                case LocStatus.Draft: return Draft;
                case LocStatus.PendingAdvisor: return PendingAdvisor;
                case LocStatus.ApprovedEs: return ApprovedEs;
                case LocStatus.Translated: return Translated;
                case LocStatus.NativeReviewed: return NativeReviewed;
                default: throw new ArgumentOutOfRangeException(nameof(status), status, "Estado desconocido.");
            }
        }
    }
}
