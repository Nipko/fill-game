#nullable enable
using System.Runtime.CompilerServices;

// Mismo criterio que AF.Core/AssemblyInfo.cs: en Unity los tests del solver viven en AF.Solver.Tests,
// y en dotnet se compilan dentro del ensamblado AF.Core.Tests. Se conceden los dos nombres.
[assembly: InternalsVisibleTo("AF.Solver.Tests")]
[assembly: InternalsVisibleTo("AF.Core.Tests")]
