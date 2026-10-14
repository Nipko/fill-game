#nullable enable

namespace AF.Platform
{
    /// <summary>
    /// Idioma y región del teléfono (D-018 Q7-A y Q8-A; 08 §2.2: <c>AF.Platform</c> da los servicios del dispositivo). Es
    /// el tercer paso de la resolución del idioma (desarrollo &gt; <c>settings.language</c> &gt; teléfono &gt; en) y la
    /// región de las cifras. Todo el juego lo pide a esta interfaz: <see cref="SystemDeviceLocale"/> lo lee de la cultura
    /// de .NET (el Editor y el respaldo), la lectura de Android por el JNI de Unity vive en <c>AF.UI</c> (necesita el
    /// motor) y los tests usan <see cref="ManualDeviceLocale"/>. Qué idioma del juego corresponde a la etiqueta lo decide
    /// quien resuelve el idioma, no esta interfaz.
    /// </summary>
    public interface IDeviceLocale
    {
        /// <summary>
        /// Etiqueta BCP 47 tal como la da el sistema (p. ej. <c>es-CO</c>, <c>pt-BR</c>, <c>en-US</c> o <c>es</c> sin
        /// región), o <c>""</c> si el sistema no la da. Nunca null.
        /// </summary>
        string LanguageTag { get; }
    }
}
