---
paths:
  - "Packages/com.amazonfill.core/**"
  - "dotnet/**"
  - "tools/level-generator/**"
  - "tools/level-qa/**"
  - "tools/solver-bench/**"
---

# Núcleo compartido AF.Core / AF.Solver (y herramientas que lo usan)

Fuentes: GDD 08 §2.1-§2.2, 02 §1, 03 §3-§6; `docs/setup/research/d-dotnet-nucleo-compartido.md` §3-§6.

## En `Packages/com.amazonfill.core/**` (lo compila Unity y también `dotnet`)
- **C# 9 y netstandard2.1.** Prohibido lo que C# 9 rechaza aunque el IDE lo sugiera: `namespace X;`, `global using`, `record struct`, `required`, `"""`, list patterns, `[1, 2]`, constructores primarios en clases, `field`. `init`/`record` solo en DTOs con el polyfill `IsExternalInit` del propio ensamblado, nunca en `BoardState` ni en tipos serializados por Unity.
- **Sin motor:** nada de `UnityEngine`, `UnityEditor` ni `#if UNITY_*`; los asmdef de `Runtime/` tienen `noEngineReferences: true` (los de `Tests/Editor/` no: usan el Test Runner de Unity y no se tocan).
- `#nullable enable` en la primera línea de cada archivo nuevo.
- **Determinismo** (mismo resultado en CoreCLR, Mono e IL2CPP ARM64):
  - PRNG propio de `AF.Core`; nunca `System.Random` ni `Random.Shared`.
  - Hash propio de 64 bits sobre bytes canónicos; nunca `GetHashCode()` de `string`.
  - Sin `DateTime.Now`/`UtcNow`: se inyecta `IClock`.
  - Reglas, hash y solver solo con enteros (sin `float`/`double`; IL2CPP puede usar FMA).
  - Nunca decidir iterando un `Dictionary`/`HashSet`; ordenaciones con desempate total.
  - `CultureInfo.InvariantCulture` en todo formateo o parseo.
- **Bucle caliente** (solver, bot ×40, Aguas turbias, Morpho, Pororoca, fútiles): sin LINQ, sin `new` de clases por nodo, sin closures (lambdas `static` sí), sin boxing, sin `params`, sin `foreach` sobre interfaces, sin `async`. Estado en `readonly struct` empaquetado; montículo propio sobre arrays (no hay `PriorityQueue` en netstandard2.1).
- `BannedSymbols.txt` y `TreatWarningsAsErrors` lo hacen cumplir en `dotnet build`: no los desactives.
- **Tests compartidos** (`Tests/Editor/**`): NUnit 3.5 (el de Unity). Solo `Assert.That(...)` y los atributos `[TestFixture] [Test] [TestCase] [TestCaseSource] [Values] [Range] [SetUp] [TearDown] [OneTimeSetUp] [OneTimeTearDown] [Category] [Explicit] [Timeout]`. Sin `Assert.Multiple`, `Warn`, `ClassicAssert` ni tests `async`. Los lotes grandes van en `dotnet/AF.Core.Tests/DotnetOnly/`.
- Cada regla del GDD implementada lleva un test que cita su § (por ejemplo `R7_PourStopsWhenTargetFull // 02 §1 R7`).
- Los `.meta` de archivos nuevos los genera Unity al importar; nunca se escriben a mano.

## En `dotnet/**`
- `.csproj`, `Directory.Build.props`, `BannedSymbols.txt` y versiones de NuGet (NUnit 3.14.0, NUnit3TestAdapter 6.3.0, Microsoft.NET.Test.Sdk 18.10.1, BannedApiAnalyzers 5.6.0) están fijados: cambiarlos exige decisión del dueño.
- Salida siempre en `dotnet/artifacts/` (ignorada por git).

## En `tools/level-generator`, `tools/level-qa`, `tools/solver-bench`
- .NET 10 y C# por defecto (14) permitidos: Unity no compila estas carpetas.
- Usan AF.Core/AF.Solver por `ProjectReference`; nunca copian ni reimplementan reglas.
- Mismo determinismo que el núcleo: misma semilla, mismo JSON byte a byte, en Windows ARM64 y en Linux x64.

## Verificación
`dotnet build dotnet/AmazonFill.slnx -warnaserror` y `dotnet test dotnet/AmazonFill.slnx` en verde antes de terminar.
