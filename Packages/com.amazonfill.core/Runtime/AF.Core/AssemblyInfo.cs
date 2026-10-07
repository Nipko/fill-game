#nullable enable
using System.Runtime.CompilerServices;

// Los tests compartidos ven los miembros internal del núcleo.
// - En Unity hay dos ensamblados de tests: AF.Core.Tests y AF.Solver.Tests (Tests/Editor/*).
// - En dotnet, dotnet/AF.Core.Tests compila los dos en un solo ensamblado llamado AF.Core.Tests.
// Se conceden los dos nombres para que un test compile igual en ambos lados.
// No poner aquí AssemblyVersion ni AssemblyTitle: el SDK de .NET los genera y se duplicarían.
[assembly: InternalsVisibleTo("AF.Core.Tests")]
[assembly: InternalsVisibleTo("AF.Solver.Tests")]
