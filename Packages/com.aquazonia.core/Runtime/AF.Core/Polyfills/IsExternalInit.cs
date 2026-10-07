#nullable enable
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill para que <c>init</c> y <c>record</c> (C# 9) compilen contra .NET Standard 2.1,
    /// tal como documenta Unity (manual 6000.3, "C# compiler"). Es interno: cada ensamblado
    /// que use <c>init</c> necesita su propia copia. Solo para DTOs y configuración; nunca en
    /// tipos serializados por Unity ni en el estado del bucle del solver.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
#endif
