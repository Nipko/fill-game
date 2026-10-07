# .NET para el núcleo compartido (AF.Core / AF.Solver) entre `dotnet test` y Unity 6

> Tema D de la investigación de puesta en marcha. Fecha: 2026-10-06. Máquina objetivo: Windows 11 Home 10.0.26200 ARM64 (Snapdragon X Plus), sin .NET SDK instalado (comprobado hoy: `C:\Program Files\dotnet` no existe y `dotnet` no está en el PATH).
>
> Leyenda: **[V]** verificada con fuente (URL en la tabla o en Fuentes) · **[E]** estimación o inferencia razonada · **[NV]** no verificable hoy. "[V-local]" = comprobado con un comando de solo lectura en esta máquina.
>
> Contexto del equipo: todo el código lo escriben agentes de IA (Claude Code y subagentes). Por eso las reglas de este informe se pueden hacer cumplir con el compilador y con analizadores, no solo con la disciplina de quien programa.

## Respuesta corta

- **Instalar .NET SDK 10 (LTS), nativo ARM64, y nada más.** `winget install --id Microsoft.DotNet.SDK.10 --exact --source winget --architecture arm64` instala hoy la versión **10.0.401** (8-sep-2026, runtime 10.0.12, C# 14). El instalador ARM64 pesa **194 MiB** (203.769.320 bytes) [V-local, V]. **No instalar .NET 8 ni .NET 9**: los dos dejan de tener soporte el **10-nov-2026**, dentro de cinco semanas [V]. .NET 10 tiene soporte hasta el 14-nov-2028 [V]. Esto **corrige el GDD**, que en 08 §2.4, 08 §12.1 (CI con `setup-dotnet 8`), 08 §15 #11, 03 §17 y la pregunta abierta #7 del capítulo 03 todavía dice ".NET 8".
- **Unity 6.x compila C# 9.0 contra .NET Standard 2.1.** Lo dice la documentación de 6.3 LTS y de 6.6 [V], y Unity confirma que 6.7 LTS, la última versión sobre Mono, sigue limitada a ".NET Standard 2.1 API and C# 9" [V]. **.NET 10 y C# 14 solo llegan con Unity 7.0** (CoreCLR en el Editor y en el player de escritorio) [V]. Para el IL2CPP de Android no hay fecha [NV]. El núcleo se diseña hoy para C# 9 y netstandard2.1.
- **Truco central: compilar el núcleo en `dotnet` con `TargetFramework=netstandard2.1` y `LangVersion=9.0`.** Así el SDK rechaza en el PC del agente lo mismo que Unity: sintaxis de C# 10 o posterior y APIs de .NET 5 o posterior, como `PriorityQueue`, que no existe en netstandard2.1 [V]. `record` e `init` funcionan con un *polyfill* interno de `IsExternalInit`, que es el método que documenta Unity [V].
- **Forma de compartir recomendada: una sola copia del código fuente** en un paquete UPM embebido, `Packages/com.aquazonia.core/`, con asmdef `noEngineReferences: true`. Los `.csproj` de `dotnet` viven **fuera** de las carpetas que importa Unity (`dotnet/`) y enlazan esos `.cs` con `<Compile Include>`. Con `ArtifactsPath`, todo `bin/obj` queda en `dotnet/artifacts/`. Unity no ve `.csproj`, `bin` ni `obj`, y `dotnet` no ve los `.meta`. Compilar una DLL y copiarla a `Assets/Plugins` queda como plan B.
- **Tests: NUnit en los dos lados, pero en versiones distintas.** Unity trae un NUnit propio "basado en NUnit 3.5": `com.unity.ext.nunit` 2.0.5 en 6.0-6.4 [V]. Unity 6.5 lo sube a 2.1.0, con base desconocida [V-terceros / NV]. En `dotnet` se usa **NUnit 3.14.0**, la última 3.x [V], con NUnit3TestAdapter 6.3.0 y Microsoft.NET.Test.Sdk 18.10.1 [V]. Los tests compartidos se escriben en el **subconjunto de NUnit 3.5**: atributos clásicos y `Assert.That` con restricciones. NUnit 4 o 5 no sirven para los tests compartidos porque rompen la compatibilidad de código con 3.x [V].
- **Solver bajo IL2CPP ARM64:** sin LINQ, *closures*, *boxing*, `params`, genéricos virtuales ni reflexión en el bucle caliente. Se usan estructuras de valor empaquetadas en `ulong`, arrays reutilizados y un montículo propio (no hay `PriorityQueue`). Desactivar las comprobaciones de IL2CPP con `Il2CppSetOption` solo después de perfilar [V/E].
- **Determinismo entre CoreCLR (herramientas) y Mono/IL2CPP (cliente):** prohibido `string.GetHashCode()`, porque no es estable entre implementaciones ni procesos [V], y `System.Random`, cuyas secuencias cambian entre versiones de .NET [V]. Tampoco hay que fiarse del orden de `Dictionary`/`HashSet` ni de los empates de `Sort`. Las reglas usan solo aritmética entera [E]. **Ojo:** para la "paridad de hash" con `tools/levelgen_proto.py` hay que portar a C# el MT19937 de CPython (`random.Random`) y su `seed_int` por SHA-256, no usar `System.Random` [V-local].
- **Para empezar a programar hoy basta con el .NET SDK 10.** Los agentes pueden portar `levelgen_proto.py` a `AF.Core`/`AF.Solver` y montar `tools/level-generator` con `dotnet test` antes de tener Unity instalado. Cuando llegue Unity, el paquete ya existe: Unity genera los `.meta` al abrirlo, y hay que confirmarlos en git.
- **Guardarraíles para agentes:** `TreatWarningsAsErrors`, `Microsoft.CodeAnalysis.BannedApiAnalyzers` 5.6.0 [V] con una lista de APIs prohibidas, `#nullable enable` al principio de cada archivo y reglas en `CLAUDE.md`. El CI ejecuta `dotnet test` en cada PR y los mismos tests en Unity EditMode con game-ci cuando cambie el paquete.

## 1. Punto de partida: qué decía el GDD y qué cambia

| Tema | GDD hoy | Cambio propuesto | Motivo |
|---|---|---|---|
| Runtime de herramientas | `.NET 8` (08 §2.4, 08 §12.1 con `setup-dotnet 8`, 08 §15 #11, 03 §17 y la pregunta abierta #7 de 03; GDD.md §9 #3) | **.NET 10 LTS** (SDK 10.0.4xx) | .NET 8 y .NET 9 terminan soporte el 10-nov-2026; .NET 10 llega hasta el 14-nov-2028 [V] |
| Ubicación de `AF.Core`/`AF.Solver` | `Assets/_Project/Scripts/Core` y `.../Solver` (08 §3) | `Packages/com.aquazonia.core/Runtime/AF.Core` y `.../AF.Solver` (paquete embebido) | Frontera limpia de "C# puro", versión propia en `package.json`, tests del paquete activados sin configurar nada [V] y ninguna dependencia del árbol `Assets/` [E] |
| Cómo se comparte con `tools/level-generator` | "se compilan también" (08 §2.1), sin mecanismo definido | `.csproj` en `dotnet/` que enlazan los `.cs` del paquete; `tools/*` hace `ProjectReference` | Sección 4 |
| Tests de reglas | "EditMode, cada PR" (08 §14.2) | `dotnet test` (rápido, sin licencia de Unity) **más** los mismos tests en EditMode con game-ci | Sección 5 |

## 2. Qué .NET SDK instalar en Windows ARM64

### 2.1 Comparativa (datos de hoy)

| Línea | Tipo | Último SDK en winget (comprobado hoy) | Fin de soporte | ARM64 nativo en Windows 11 | ID de winget | ¿Instalar? |
|---|---|---|---|---|---|---|
| .NET 10 | **LTS** (3 años) | **10.0.401** (8-sep-2026; runtime 10.0.12; C# 14.0) | **14-nov-2028** | Sí (instalador `dotnet-sdk-10.0.401-win-arm64.exe`) | `Microsoft.DotNet.SDK.10` | **Sí** |
| .NET 9 | STS (2 años) | 9.0.318 | 10-nov-2026 | Sí | `Microsoft.DotNet.SDK.9` | No |
| .NET 8 | LTS | 8.0.425 | 10-nov-2026 | Sí | `Microsoft.DotNet.SDK.8` | No |
| .NET 11 | Preview / RC1 *go-live* | 11.0.100-rc.1.26425.128 | GA prevista en noviembre [E, cadencia anual [V]] | Sí [E] | `Microsoft.DotNet.SDK.Preview` | No (preview) |

Fuentes: salida local de `winget search Microsoft.DotNet.SDK` y `winget show Microsoft.DotNet.SDK.10 --architecture arm64` [V-local]; página de descargas de .NET 10 y política de soporte de .NET [V]; matriz de sistemas de "Install .NET on Windows", que marca .NET 10/9/8 con "x64, x86, Arm64" en Windows 11 [V].

Detalle de `winget show Microsoft.DotNet.SDK.10` [V-local]:

```
Version: 10.0.401   Publisher: Microsoft Corporation   Moniker: dotnet-sdk-10
Installer Type: burn
Installer Url: https://download.microsoft.com/download/52d16011-1aa2-4a7e-a4be-769e1bdd32be/a61b639b-98cf-41d9-822d-0bb99c462cef/dotnet-sdk-10.0.401-win-arm64.exe
Installer SHA256: faa807ca98afcc9134c8a2ca8c81f21f30bfce3bce7e5804708d601c579ba3b8
```

Tamaño: `Content-Length: 203769320` (194 MiB), según una petición HEAD a esa URL [V-local]. Microsoft no publica el tamaño ya instalado [NV]. La estimación es de unos cientos de MB, más la caché de NuGet en `%USERPROFILE%\.nuget\packages`, que crece con el uso [E]. Hay 213 GB libres, así que no es una restricción.

### 2.2 Notas para esta máquina

- **Ruta y PATH:** en un PC Windows ARM, el SDK ARM64 se instala en `C:\Program Files\dotnet\`. Si algún día se instala también el SDK x64, va a `C:\Program Files\dotnet\x64\` y hay que vigilar `PATH` y `DOTNET_ROOT` [V]. **Recomendación: no instalar el SDK x64.** Nada de este proyecto lo necesita [E].
- **Permisos:** la instalación para todo el sistema pide administrador (aparecerá UAC) [V]. La tiene que aceptar el dueño.
- **Smart App Control:** Microsoft recomienda tenerlo apagado en máquinas de desarrollo porque ralentiza el SDK [V]. En esta máquina ya está apagado: `VerifiedAndReputablePolicyState = 0` [V-local].
- **¿Lo necesita Unity?** Unity compila los scripts con su propio Roslyn y su propio runtime, así que el SDK del sistema no es necesario para compilar en Unity [E]. Sí lo necesitan `dotnet test`, `tools/level-generator`, `tools/level-qa` y la integración de C# en el editor de código (Cursor) [E].
- **Plan B Godot:** Godot 4.5 o posterior pide ".NET 8 or later" y la exportación a Android pide ".NET 9 or later" [V]. **.NET 10 cubre también la alternativa documentada.**

## 3. Restricciones de Unity 6.x para el código compartido

### 3.1 Versión de C# y de API por versión de Unity

| Unity | Estado (oct-2026) | C# | API | Fuente |
|---|---|---|---|---|
| 6.0 LTS | Soporte hasta oct-2026 (se acaba ya) | 9.0 | .NET Standard 2.1 (por defecto) / .NET Framework 4.8 | [V] unity.com/releases/unity-6/support |
| **6.3 LTS** | **LTS actual, soporte hasta dic-2027** | **9.0** | .NET Standard 2.1 por defecto | [V] manual 6000.3 (`csharp-compiler`, `dotnet-profile-support`) |
| 6.6 | Supported Update; 6000.6.0f1 publicado el 31-ago-2026 | 9.0 | .NET Standard 2.1 | [V] manual 6000.6, notas de 6000.6.0f1 |
| 6.7 LTS | En beta (6000.7.0b3) [V-terceros]; "final release built upon Mono"; player CoreCLR **experimental** solo para escritorio | 9.0 | ".NET Standard 2.1 API and C# 9" | [V] Unity Discussions, jun-2026 |
| 7.0 | Futuro; Editor y player de escritorio sobre CoreCLR, "access to .NET 10 and C# 14"; la optimización de rendimiento se aplaza a "the subsequent LTS release following Unity 7.0 in 2027" | 14 (escritorio) | .NET 10 | [V] Unity Discussions, jun-2026. IL2CPP/Android con C# 14: sin fecha [NV] |

Versión de Roslyn que trae Unity: no se publica [NV].

**Consecuencia:** el proyecto vivirá en 6.3 LTS o 6.7 LTS con **C# 9 + .NET Standard 2.1** durante todo el desarrollo de la 1.0 [E]. La migración a C# 14 es un proyecto de 2027 o posterior. Cuando llegue, bastará con subir `LangVersion` en un archivo (sección 6.3) [E].

### 3.2 Funciones de C# 9 en Unity

| Función | En Unity 6.x | Uso en AF.Core/AF.Solver |
|---|---|---|
| `record` (clase), `init` | Unity marca "Init only setters" como **no soportado**. La documentación dice: "Users can work around this issue by declaring the `System.Runtime.CompilerServices.IsExternalInit` type in their own projects" [V]. El polyfill funciona en Mono e IL2CPP según la comunidad [V-terceros: xpTURN/Polyfill, hilo de Unity Discussions de mar-2026] | Permitido con polyfill **interno**, uno por ensamblado que lo use. Solo para DTOs y configuración, **nunca** para `BoardState` en el bucle del solver (asigna en el heap). No usar `record` en tipos serializados por Unity: "Unity's serialization system doesn't support C# records" [V] |
| `record struct` | Es C# 10: no disponible [E] | Prohibido (lo bloquea `LangVersion 9`) |
| Covariant return types | No soportado [V] | Prohibido. El compilador lo rechaza con netstandard2.1 [E] |
| Module initializers | No soportado [V] | Prohibido. No crear el polyfill de `ModuleInitializerAttribute` |
| Function pointers con convenciones extensibles | No soportado [V] | Prohibido (además exigiría `unsafe`) |
| `SkipLocalsInit` | No soportado [V] | Prohibido |
| *Pattern matching* de C# 9 (`and`/`or`/`not`, relacionales), `new()` con tipo objetivo, funciones locales estáticas, lambdas estáticas, *top-level statements* | No aparecen en la lista de no soportadas, así que se entienden soportadas [E, por exclusión en la doc. de 6000.3/6000.6] | Permitido. Las lambdas `static` ayudan a no capturar variables |
| Métodos de interfaz por defecto (C# 8) | En 2020.3 figuraban como **no soportados**; desde la doc. de 2021.2 ya no aparecen en la lista [V], lo que indica que hoy funcionan [E] | Evitar: no aportan nada al núcleo y complican el *dispatch* en AOT [E] |
| Índices y rangos (`^1`, `..`) (C# 8) | No soportados en 2020.3 [V]; con .NET Standard 2.1 existen `System.Index` y `System.Range` [E] | Permitido fuera del bucle caliente (los rangos sobre arrays copian) [E] |
| `Span<T>`, `stackalloc` en código seguro | Están en .NET Standard 2.1 [E] | Permitido para búferes temporales en el solver, con cuidado con la profundidad de `stackalloc` [E] |
| `async`/`await`, `IAsyncEnumerable` | Existen [E] | Prohibido en el núcleo: es síncrono y con presupuesto de nodos. La asincronía va en las capas de Unity |

**Lo que los agentes escriben por reflejo y C# 9 rechaza** [E, versiones del lenguaje según Microsoft]: namespaces con ámbito de archivo (`namespace X;`, C# 10), `global using` (C# 10), `record struct` (C# 10), `required` y *raw string literals* `"""` (C# 11), *list patterns* (C# 11), *collection expressions* `[1, 2]` (C# 12), constructores primarios en clases (C# 12), la palabra clave `field` y los *extension members* (C# 14). Con `LangVersion 9.0` en `dotnet`, todo esto falla al compilar en la máquina del agente, antes de llegar a Unity.

### 3.3 API compatibility level y APIs que no existen

- Unity 6.3 ofrece **.NET Standard 2.1 (por defecto)** y **.NET Framework 4.8**, y recomienda Standard 2.1 para proyectos nuevos por su "smaller API surface" y su "better cross-platform support" [V]. Las DLL de .NET Standard funcionan con ambos niveles; las de .NET Core/.NET 5+ **no están soportadas** [V]. **Decisión: .NET Standard 2.1.**
- APIs habituales de .NET moderno que **no** están en netstandard2.1, y su sustituto en el núcleo:

| API | Desde | Sustituto en AF.Core/AF.Solver | Estado |
|---|---|---|---|
| `PriorityQueue<TElement,TPriority>` | .NET 6 | Montículo binario o 4-ario propio sobre arrays de `struct` (el A* del solver lo necesita) | [V] la documentación solo la lista para net-6.0 a net-11.0 |
| `Random.Shared`, nuevo algoritmo de `Random` | .NET 6 | PRNG propio determinista (sección 3.5) | [E] |
| `ArgumentNullException.ThrowIfNull` | .NET 6 | `if (x == null) throw new ArgumentNullException(nameof(x));` | [E] |
| `Enum.GetValues<T>()`, `CollectionsMarshal` | .NET 5 | Tablas estáticas y arrays propios | [E] |
| `System.Text.Json` | No está en el BCL de netstandard2.1 | El núcleo no parsea JSON. Lo hacen `AF.Levels` en Unity y `tools/*` en .NET 10 | [E] |

No hace falta memorizar esta lista: **compilar con `TargetFramework=netstandard2.1` hace que cualquier API inexistente sea un error de compilación** [E, comportamiento estándar del SDK].

### 3.4 Rendimiento bajo IL2CPP ARM64 (solver, pista, Morpho, Pororoca, Aguas turbias)

| Evitar en el bucle caliente | Por qué | Alternativa | Estado |
|---|---|---|---|
| *Closures* y métodos anónimos | "Avoid closures in C# whenever possible": instancian una clase generada en el heap | Métodos estáticos, lambdas `static`, comparadores `struct` | [V] manual 6000.3 "Reference type management" |
| *Boxing* (structs como `object` o interfaz, `enum` como clave con comparador por defecto) | "one of the most common sources of unintended temporary memory allocations" y el compilador no avisa | `IEquatable<T>`, comparadores `struct`, claves `int`/`ulong` | [V] mismo documento / [E] para claves |
| `params` y concatenación de `string` | Asignan un array o una cadena nueva | Sobrecargas fijas; no formatear en el solver | [V] |
| LINQ | Iteradores, delegados y llamadas por interfaz que asignan memoria en cada uso | Bucles `for` sobre arrays | [E] (práctica común; la doc. de Unity no menciona LINQ explícitamente) |
| Métodos genéricos virtuales, `MakeGenericType`, `Activator.CreateInstance` | IL2CPP genera código compartido más lento que "doesn't know type sizes" | Genéricos no virtuales con restricción `where T : struct` o `class`, o código no genérico | [V] manual 6000.3 "IL2CPP limitations" |
| `System.Reflection.Emit`, `dynamic` | No existen en AOT | No se usan | [V] |
| `foreach` sobre `IEnumerable<T>` o interfaces | Enumerador en caja | `for` con índice | [E] |
| `new` de clases por nodo explorado | Presión del GC en gama baja | `BoardState` como `readonly struct` de `ulong` empaquetados (la cuia cabe en `len` + 4 bits por unidad, 08 §2.2), pools y arrays preasignados con capacidad máxima | [E] |
| Comprobaciones de nulos y de límites en el bucle más interno | IL2CPP las emite por defecto | `[Il2CppSetOption(Option.ArrayBoundsChecks, false)]` en 1-3 métodos, **solo después de perfilar**. El atributo es un `.cs` que se copia desde `Data\il2cpp` de la instalación de Unity a tu proyecto | [V] manual "IL2CPP runtime checks" / [E] que se puede declarar en un ensamblado sin `UnityEngine` (es un atributo C# corriente) |

El rendimiento relativo entre IL2CPP ARM64 y CoreCLR depende del código y no está publicado [NV]. Hay que medirlo con el benchmark de la sección 5.4 en un dispositivo de gama baja y fijar con datos el presupuesto de nodos de la pista y de Aguas turbias.

### 3.5 Determinismo: el mismo código en tres runtimes

`tools/level-generator` corre sobre CoreCLR 10, el Editor sobre Mono y el móvil sobre IL2CPP. Para que "la prueba de determinismo por hash" (dirección 7) funcione:

| Riesgo | Detalle | Regla | Estado |
|---|---|---|---|
| `string.GetHashCode()` | "Hash codes for identical strings can differ across .NET implementations, across .NET versions... two subsequent runs of the same program may return different hash codes" | Hash propio de 64 bits (FNV-1a o xxHash64) sobre bytes canónicos del estado; Zobrist para el solver | [V] |
| `System.Random` | "the example may produce different sequences of random numbers if run on different versions of .NET" | PRNG propio en `AF.Core` | [V] |
| Paridad con el prototipo Python | `levelgen_proto.py` usa `random.Random` (MT19937 de CPython: `random()`, `choice`, `randrange`, `shuffle`, `getrandbits`) y `seed_int` = primeros 16 hex de SHA-256 (líneas 256, 359, 370, 470) | Portar a C# **exactamente** el MT19937 de CPython, su siembra desde entero y su `_randbelow`, o redefinir el canon y regenerar los JSON de referencia. Es una decisión del dueño; el GDD pide "paridad de hash y par" (GDD.md §9 #3) | [V-local] lectura del archivo / [E] solución |
| Orden de `Dictionary`/`HashSet` | No especificado; las implementaciones del BCL de Mono y de CoreCLR pueden diferir | No decidir nada iterando un diccionario; usar listas ordenadas | [E] |
| `Array.Sort`/`List.Sort` | Son inestables y el orden de los empates depende de la implementación | Comparadores con desempate total (por índice) | [E] |
| `float`/`double` | IL2CPP pasa por C++ y clang, que puede contraer `a*b+c` en FMA en ARM64; RyuJIT no tiene por qué hacerlo | Reglas, hash y solver del cliente solo con enteros. Las métricas `double` (D, bot) solo offline | [E] |
| Cultura | `ToString`/`Parse` dependen de la cultura del dispositivo | `CultureInfo.InvariantCulture` siempre | [E] |
| Reloj | `DateTime.Now` no es determinista | Inyectar `IClock` (`AF.Clock`) | [E] |

## 4. Formas de compartir el código entre Unity y `dotnet`

### 4.1 Comparativa

| Opción | Cómo | Ventajas | Problemas | Veredicto |
|---|---|---|---|---|
| **(a) Paquete UPM local con `.csproj` dentro del paquete** | `Packages/com.aquazonia.core/` con `package.json`, asmdef y un `AF.Core.csproj` en la misma carpeta | Todo junto | `dotnet build` crea `obj/` y `bin/` **dentro** del paquete y Unity los importa: `obj/` contiene `.cs` generados (AssemblyInfo, etc.) que Unity compilaría otra vez [E]. Unity crea un `.meta` también para el `.csproj` [E]. Para esquivarlo hay que usar carpetas que acaben en `~` (Unity ignora "Files and folders which end with `~`" [V]) o redirigir rutas | Evitar en su forma pura |
| **(a') Paquete UPM embebido + `.csproj` fuera que enlazan los fuentes** (recomendada) | Fuentes y asmdef en `Packages/com.aquazonia.core/`; `dotnet/AF.Core/AF.Core.csproj` con `EnableDefaultCompileItems=false` y `<Compile Include="../../Packages/com.aquazonia.core/Runtime/AF.Core/**/*.cs" />`; `ArtifactsPath` lleva `bin/obj` a `dotnet/artifacts/` [V] | **Una sola copia del código.** Unity solo ve `.cs`, `.asmdef`, `package.json` y `.meta`. `dotnet` ignora los `.meta` (el patrón `*.cs` no incluye `*.cs.meta`). Los tests de un paquete en `Packages/` se activan solos [V]. El agente compila y prueba en segundos sin Unity | Dos compilaciones del mismo código: Roslyn del SDK frente a Roslyn de Unity, BCL de CoreCLR frente al de Mono/IL2CPP (mitigación en la sección 5). Los `.meta` de archivos nuevos solo aparecen al abrir Unity | **Elegida** |
| **(b) DLL netstandard2.1 en `Assets/Plugins`** | `dotnet build -c Release` y copia de `AF.Core.dll`/`AF.Solver.dll` | Unity y las herramientas ejecutan **el mismo IL**. Unity compila menos. Se podría escribir con C# más nuevo apuntando a netstandard2.1 (solo funciones de compilador, con polyfills) [E] | Hay que reconstruir y copiar en cada cambio, y un agente que lo olvide deja una DLL obsoleta [E]. Binarios en git (o LFS). Su `.meta` guarda la configuración del importador de plugins. Depurar desde Unity cuesta más. El CI tiene que compilar la DLL antes del build de Unity. Si existe a la vez un asmdef con el mismo nombre, hay conflicto [E] | Plan B |
| **(c) Generadores de código fuente / enlaces simbólicos** | Analyzer marcado `RoslynAnalyzer` que inyecta código, o *symlinks*/*junctions* de carpetas | Ninguna relevante para compartir lógica | Los generadores sirven para generar código, no para compartirlo. Los *symlinks* en Windows dependen de `core.symlinks` en git y del modo desarrollador [E] | Descartada. Como mucho, un generador para el polyfill |

### 4.2 Detalles de la opción elegida

- **Ubicación del paquete:** `Packages/com.aquazonia.core/` (embebido; Unity lo detecta sin tocar `manifest.json`). Unity también admite paquetes locales `file:` fuera del proyecto, con rutas relativas a `Packages/` [V], pero no hace falta.
- **Disposición de carpetas:** la estándar de Unity (`package.json`, `Runtime/`, `Tests/Editor|Runtime`, `Documentation~`) [V].
- **`noEngineReferences: true`:** "Unity does not add references to `UnityEditor` or `UnityEngine` assemblies when it compiles the assembly" [V]. Es la garantía de pureza en el lado de Unity. En el lado `dotnet` la garantía la da que `UnityEngine` no existe.
- **`.meta`:** se confirman en git. Los agentes crean `.cs` sin Unity abierto, así que hace falta un paso "abrir Unity, o ejecutarlo en *batchmode* con `-quit`, y confirmar los `.meta`" y una comprobación de CI que falle si un archivo del paquete no tiene `.meta`. Los `.cs` de ensamblados puros no se referencian desde escenas, así que un GUID regenerado no rompe nada, pero ensucia los PR [E].
- **`Directory.Build.props` fuera de la raíz:** la raíz del repositorio es la del proyecto Unity, que genera sus propios `.csproj` para el IDE. Un `Directory.Build.props` en la raíz que redirija rutas de salida rompe la carga de proyectos en Rider [V-terceros: soporte de JetBrains]. Por eso va en `dotnet/` y `tools/` (sección 6).
- **Nada de `#if UNITY_*` en `Runtime/`:** si el código se comporta distinto según el compilador, los tests de `dotnet` dejan de representar al cliente [E]. Solo se tolera en el código auxiliar de los tests (rutas de fixtures).
- **`InternalsVisibleTo`:** `[assembly: InternalsVisibleTo("AF.Core.Tests")]` en `Runtime/AF.Core/AssemblyInfo.cs`, con el mismo nombre de ensamblado de tests en los dos lados. No poner `AssemblyVersion` ni `AssemblyTitle` en los fuentes compartidos, porque el SDK los genera y se duplicarían [E].

## 5. Framework de tests que sirva en ambos lados

### 5.1 Versiones

| Lado | Paquete | Versión | Estado |
|---|---|---|---|
| Unity 6.0-6.4 | `com.unity.test-framework` (paquete *core*, versión fijada por el Editor [V]; "embedded package" desde 1.5.1 [V]) | 1.6.x en 6.3 [E] (1.6.0 publicado el 11-ago-2025 [V]) | [V/E] |
| Unity 6.0-6.4 | `com.unity.ext.nunit` "Custom NUnit... **Based on NUnit version 3.5**... works with all platforms, il2cpp and Mono AOT" | 2.0.5 (28-jun-2023) | [V] |
| Unity 6.5+ | `com.unity.test-framework` 1.6.0 → 1.7.0 y `com.unity.ext.nunit` 2.0.5 → **2.1.0** (6000.5.0b3) | Base NUnit de 2.1.0: desconocida | [V-terceros: unityreleases.com] / [NV] |
| `dotnet` | `NUnit` | **3.14.0** (3-nov-2023, última 3.x publicada; netstandard2.0, net35/40/45) | [V] nuget.org |
| `dotnet` | `NUnit3TestAdapter` | 6.3.0 (24-ago-2026; "works with NUnit 3.0 and higher"; net8.0 y net462; integra MTP) | [V] |
| `dotnet` | `Microsoft.NET.Test.Sdk` | 18.10.1 (15-sep-2026) | [V] |
| `dotnet` | `NUnit.Analyzers` (opcional) | 3.9.0, la que el propio proyecto recomienda para "Unity 2022.2 or newer" (Unity Test Framework = NUnit 3.5) | [V] README de nunit.analyzers |

**Por qué NUnit 3.14 y no 4.x o 5.0:** NUnit 4.0 "is neither binary nor source code compatible with NUnit 3.14.0". Los asserts clásicos pasan a `NUnit.Framework.Legacy.ClassicAssert` [V]. NUnit 4.5 los recupera con miembros de extensión de C# 14 [V], y C# 14 no existe en Unity. NUnit 5.0.0 salió el 27-sep-2026 con más cambios incompatibles [V]. Ninguno compila el mismo código de test que el NUnit 3.5 de Unity.

### 5.2 Subconjunto de NUnit permitido en los tests compartidos

- Atributos: `[TestFixture]`, `[Test]`, `[TestCase]`, `[TestCaseSource]`, `[Values]`, `[Range]`, `[SetUp]`, `[TearDown]`, `[OneTimeSetUp]`, `[OneTimeTearDown]`, `[Category]`, `[Explicit]`, `[Timeout]` [E: existen en 3.x desde antes de 3.5].
- Asserts: **solo `Assert.That(actual, <restricción>)`** (`Is.EqualTo`, `Is.True`, `Has.Count.EqualTo`, `Is.EquivalentTo`, `Throws.TypeOf<T>()`). Es el estilo que compila igual en 3.5, 3.14, 4.x y 5.x [E].
- Prohibido: `Assert.Multiple` y `Warn` (NUnit 3.6+, no están en 3.5 [E; un hilo de Unity confirma que "Warnings" exige 3.6+ [V]]), tests `async Task`, y cualquier API que el analizador marque como posterior a 3.5.
- Fixtures de datos: casos pequeños como literales C# en los tests compartidos. Los lotes grandes, como la paridad con `tools/out/levels_proto.json` y `levels_size_100.json`, van en **tests solo-dotnet** (`dotnet/AF.Core.Tests/DotnetOnly/`), que pueden leer JSON con `System.Text.Json` [E].

### 5.3 Dónde se ejecuta cada cosa

| Puerta | Qué | Cuándo | Coste |
|---|---|---|---|
| 1. `dotnet test dotnet/Aquazonia.slnx` | Todos los tests (compartidos + solo-dotnet) sobre CoreCLR 10 | El agente, en local, en cada cambio; CI en cada PR | Segundos o minutos; no necesita licencia de Unity [E] |
| 2. Unity EditMode (game-ci `unity-test-runner`) | Los mismos tests compartidos, compilados por Unity sobre Mono | CI en los PR que toquen `Packages/com.aquazonia.core/**` | Licencia Personal en secrets (ya prevista en 08 §12.1) |
| 3. Paridad IL2CPP (PlayMode en un player IL2CPP) | Suite "golden": N tableros con su hash de 64 bits y su par esperado, calculados por `tools/level-qa` | Antes de cada release y al cambiar de versión de Unity | Build de player; en dispositivo o en Windows IL2CPP [E] |

### 5.4 Benchmark

Un `tools/solver-bench` (consola .NET 10) mide nodos por segundo y asignaciones (`GC.GetAllocatedBytesForCurrentThread()`) sobre los tableros golden. Un test PlayMode equivalente mide lo mismo en el dispositivo. El presupuesto de nodos de la pista y de Aguas turbias se fija con esos números [E].

## 6. Estructura de carpetas y comandos

### 6.1 Árbol propuesto (añade a 08 §3)

```
amazon-fill/                          # raíz = proyecto Unity (como en 08 §3)
  global.json                         # fija el SDK 10.0.4xx para toda la raíz
  Assets/_Project/...                 # resto del juego (AF.Levels, AF.UI, ... con sus asmdef)
  Packages/
    manifest.json
    com.aquazonia.core/               # paquete embebido: ÚNICA copia del núcleo
      package.json
      README.md  CHANGELOG.md
      Runtime/
        AF.Core/
          AF.Core.asmdef              # noEngineReferences: true
          AssemblyInfo.cs             # InternalsVisibleTo("AF.Core.Tests")
          Polyfills/IsExternalInit.cs
          Board/ Rules/ Mechanics/ Hashing/ Random/ ...
        AF.Solver/
          AF.Solver.asmdef            # references: ["AF.Core"], noEngineReferences: true
          AStar/ Bot/ Shuffle/ Murky/ Collections/BinaryHeap.cs
      Tests/
        Editor/
          AF.Core.Tests/AF.Core.Tests.asmdef      # EditMode, NUnit de Unity
          AF.Core.Tests/*.cs                      # tests COMPARTIDOS (NUnit 3.5 + C# 9)
          AF.Solver.Tests/...
  dotnet/                             # solo .NET (Unity no importa esta carpeta)
    Directory.Build.props             # LangVersion 9, netstandard2.1, artifacts, guardarraíles
    BannedSymbols.txt
    Aquazonia.slnx
    AF.Core/AF.Core.csproj            # enlaza ../../Packages/com.aquazonia.core/Runtime/AF.Core/**/*.cs
    AF.Solver/AF.Solver.csproj
    AF.Core.Tests/AF.Core.Tests.csproj  # net10.0; enlaza Tests/Editor/AF.Core.Tests/**/*.cs
    AF.Core.Tests/DotnetOnly/*.cs       # paridad con levels_proto.json, lotes grandes
    artifacts/                        # bin/obj de todo (en .gitignore)
  tools/
    Directory.Build.props             # herramientas: net10.0, C# por defecto (14)
    level-generator/level-generator.csproj   # ProjectReference ../../dotnet/AF.Core y AF.Solver
    level-qa/level-qa.csproj
    solver-bench/solver-bench.csproj
    levelgen_proto.py econ_sim.py ...  # sin cambios
```

Las carpetas de primer nivel `dotnet/`, `tools/`, `docs/`, `schemas/` y `functions/` quedan fuera de `Assets/` y `Packages/`, así que Unity no las importa [E, Unity solo importa `Assets/` y los paquetes].

### 6.2 Archivos clave

`global.json` (raíz):

```json
{ "sdk": { "version": "10.0.401", "rollForward": "latestFeature" } }
```

`dotnet/Directory.Build.props`, para los proyectos que compilan código compartido:

```xml
<Project>
  <PropertyGroup>
    <LangVersion>9.0</LangVersion>                 <!-- igual que Unity 6.x -->
    <Nullable>disable</Nullable>                   <!-- el contexto se abre por archivo con #nullable enable, igual que en Unity -->
    <ImplicitUsings>disable</ImplicitUsings>       <!-- global using es C# 10 -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Deterministic>true</Deterministic>
    <ArtifactsPath>$(MSBuildThisFileDirectory)artifacts</ArtifactsPath>
    <AfPackageDir>$(MSBuildThisFileDirectory)../Packages/com.aquazonia.core/</AfPackageDir>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.BannedApiAnalyzers" Version="5.6.0" PrivateAssets="all" />
    <AdditionalFiles Include="$(MSBuildThisFileDirectory)BannedSymbols.txt" />
  </ItemGroup>
</Project>
```

`dotnet/AF.Core/AF.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <AssemblyName>AF.Core</AssemblyName>
    <RootNamespace>AF.Core</RootNamespace>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(AfPackageDir)Runtime/AF.Core/**/*.cs" />
  </ItemGroup>
</Project>
```

(`AF.Solver.csproj` es igual, con `Runtime/AF.Solver/**/*.cs` y `<ProjectReference Include="../AF.Core/AF.Core.csproj" />`.)

`dotnet/AF.Core.Tests/AF.Core.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>AF.Core.Tests</AssemblyName>
    <IsPackable>false</IsPackable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(AfPackageDir)Tests/Editor/AF.Core.Tests/**/*.cs" />  <!-- compartidos -->
    <Compile Include="DotnetOnly/**/*.cs" />                                  <!-- solo dotnet -->
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.10.1" />
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.3.0" />
    <PackageReference Include="NUnit.Analyzers" Version="3.9.0" PrivateAssets="all" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../AF.Core/AF.Core.csproj" />
    <ProjectReference Include="../AF.Solver/AF.Solver.csproj" />
  </ItemGroup>
</Project>
```

`tools/Directory.Build.props`: las herramientas no se comparten con Unity, así que usan el C# por defecto de .NET 10, pero sacan los binarios al mismo `artifacts`:

```xml
<Project>
  <PropertyGroup>
    <ArtifactsPath>$(MSBuildThisFileDirectory)../dotnet/artifacts</ArtifactsPath>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

`Packages/com.aquazonia.core/Runtime/AF.Core/AF.Core.asmdef` (campos según el formato oficial [V]):

```json
{
  "name": "AF.Core",
  "rootNamespace": "AF.Core",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": true
}
```

`Tests/Editor/AF.Core.Tests/AF.Core.Tests.asmdef` [E: es la forma que genera *Create › Testing › Test Assembly Folder*; el agente debe generarlo desde ese menú o compararlo con lo que produzca]:

```json
{
  "name": "AF.Core.Tests",
  "references": ["AF.Core", "AF.Solver", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": ["Editor"],
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

`Packages/com.aquazonia.core/package.json` [E: valores del proyecto]:

```json
{
  "name": "com.aquazonia.core",
  "version": "1.0.0",
  "displayName": "Aquazonia Core (AF.Core + AF.Solver)",
  "description": "Reglas, solver y utilidades deterministas en C# puro. Compartido con tools/ vía dotnet/.",
  "unity": "6000.3"
}
```

`Runtime/AF.Core/Polyfills/IsExternalInit.cs` [V: técnica documentada por Unity]:

```csharp
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
```

Si `AF.Solver` llega a usar `init`/`record`, necesita su propia copia (el tipo es `internal`) [E].

`dotnet/BannedSymbols.txt` (formato del analizador: ID de documentación;mensaje) [E]:

```
T:System.Random;Usa AF.Core.Random (determinista entre CoreCLR, Mono e IL2CPP)
M:System.String.GetHashCode;No es estable entre runtimes: usa AF.Core.Hashing
T:System.Linq.Enumerable;Sin LINQ en AF.Core/AF.Solver (asignaciones bajo IL2CPP)
P:System.DateTime.Now;Inyecta IClock
P:System.DateTime.UtcNow;Inyecta IClock
```

(Como el `Directory.Build.props` de `dotnet/` afecta también a los tests, si molesta en ellos se puede desactivar ahí con `<ItemGroup><AdditionalFiles Remove=... /></ItemGroup>` [E].)

### 6.3 Comandos (PowerShell, desde la raíz del repositorio)

```powershell
# 0) Instalar el SDK (lo hace el dueño: pide UAC). Abrir una terminal nueva después.
winget install --id Microsoft.DotNet.SDK.10 --exact --source winget --architecture arm64
dotnet --list-sdks          # debe mostrar 10.0.401 (o 10.0.4xx posterior)
dotnet --info               # RID: win-arm64; Base Path: C:\Program Files\dotnet\sdk\...

# 1) global.json y carpetas
dotnet new globaljson --sdk-version 10.0.401 --roll-forward latestFeature
New-Item -ItemType Directory -Force Packages/com.aquazonia.core/Runtime/AF.Core/Polyfills, Packages/com.aquazonia.core/Runtime/AF.Solver, Packages/com.aquazonia.core/Tests/Editor/AF.Core.Tests, Packages/com.aquazonia.core/Tests/Editor/AF.Solver.Tests | Out-Null

# 2) Proyectos .NET (después se editan como en 6.2)
dotnet new buildprops -o dotnet --use-artifacts
dotnet new classlib -f netstandard2.1 -n AF.Core   -o dotnet/AF.Core
dotnet new classlib -f netstandard2.1 -n AF.Solver -o dotnet/AF.Solver
dotnet new nunit -n AF.Core.Tests -o dotnet/AF.Core.Tests      # net10.0; luego fijar NUnit 3.14.0
dotnet new console -n level-generator -o tools/level-generator
dotnet new console -n level-qa        -o tools/level-qa
Remove-Item dotnet/AF.Core/Class1.cs, dotnet/AF.Solver/Class1.cs
dotnet new sln -n Aquazonia -o dotnet                          # .NET 10 crea .slnx por defecto
dotnet sln dotnet/Aquazonia.slnx add dotnet/AF.Core dotnet/AF.Solver dotnet/AF.Core.Tests tools/level-generator tools/level-qa
dotnet add dotnet/AF.Solver reference dotnet/AF.Core
dotnet add tools/level-generator reference dotnet/AF.Core dotnet/AF.Solver
dotnet add tools/level-qa reference dotnet/AF.Core dotnet/AF.Solver
dotnet add dotnet/AF.Core.Tests package NUnit --version 3.14.0
dotnet add dotnet/AF.Core.Tests package NUnit3TestAdapter --version 6.3.0
dotnet add dotnet/AF.Core.Tests package Microsoft.NET.Test.Sdk --version 18.10.1

# 3) Ciclo del agente
dotnet build dotnet/Aquazonia.slnx
dotnet test  dotnet/Aquazonia.slnx
dotnet run --project tools/level-generator -- --help
```

Notas verificadas: `classlib -f` acepta `netstandard2.1` [V]; `buildprops --use-artifacts` genera `ArtifactsPath` [V]; `dotnet new sln` crea `.slnx` desde .NET 10 [V]; `globaljson` admite `--sdk-version` y `--roll-forward` [V]; `dotnet test` usa VSTest por defecto y MTP solo si se activa en `global.json` [V]. No hace falta MTP.

**Cuidado con lo que generan las plantillas** [E]: `dotnet new classlib` y `nunit` escriben `ImplicitUsings`/`Nullable` en el propio `.csproj`, y eso **pisa** el `Directory.Build.props`, que se importa antes. En los tres proyectos de `dotnet/` hay que borrar esas líneas. Si no se borran, `ImplicitUsings=enable` genera `global using` y con `LangVersion 9` falla la compilación, que es el síntoma que delata el problema. En `tools/*` se pueden dejar.

### 6.4 Reglas para `CLAUDE.md` (agentes)

1. Todo lo que esté en `Packages/com.aquazonia.core/**` es C# 9 y netstandard2.1, sin `UnityEngine` ni `#if UNITY_*`, con `#nullable enable` en la primera línea de cada archivo.
2. Antes de terminar una tarea del núcleo: `dotnet build` y `dotnet test` sobre `dotnet/Aquazonia.slnx`, los dos sin avisos.
3. Bucle caliente sin `new` de clases, LINQ, lambdas que capturen, `params` ni `foreach` sobre interfaces. Estado en `readonly struct`.
4. Tests compartidos solo con `Assert.That` y atributos de NUnit 3.5. Los lotes grandes van en `DotnetOnly/`.
5. Ningún archivo nuevo del paquete se fusiona sin su `.meta` (lo comprueba el CI).
6. Prohibido tocar `LangVersion`, `TargetFramework` o la versión de NUnit sin una decisión del dueño.

### 6.5 CI (ajuste a 08 §12.1)

- Cambiar `setup-dotnet 8` por **`setup-dotnet` con `global-json-file: global.json`** (SDK 10) [E sobre el nombre exacto de la opción de la action; verificar en su README al montarlo].
- Trabajo nuevo `core-tests`: `dotnet test dotnet/Aquazonia.slnx` en cada PR (runner `ubuntu-latest` o `windows-latest`, sin licencia de Unity).
- Trabajo `unity-editmode` (game-ci) filtrado por rutas: `Packages/com.aquazonia.core/**`.
- Comprobación de `.meta`: un script que liste los `*.cs`/`*.asmdef` del paquete sin su `.meta` y falle.

## Recomendación para este equipo y esta máquina

1. **Hoy:** el dueño instala **solo** el .NET SDK 10 ARM64 (`winget install --id Microsoft.DotNet.SDK.10 --exact --source winget --architecture arm64`, unos 194 MiB y una UAC). No se instala .NET 8 ni 9, ni el SDK x64.
2. **Actualizar el GDD:** ".NET 8" pasa a ".NET 10 LTS" en 08 §2.4, 08 §12.1 (YAML de CI, `setup-dotnet`), 08 §15 #11, 03 §17, la pregunta abierta #7 de 03 y GDD.md §9 #3. En 08 §3 `Scripts/Core` y `Scripts/Solver` pasan a `Packages/com.aquazonia.core/Runtime/AF.Core|AF.Solver`, y se añaden las carpetas `dotnet/` y `global.json`.
3. **Mecanismo de compartición:** paquete UPM embebido con la única copia de los fuentes, más `.csproj` en `dotnet/` que los enlazan, `netstandard2.1` + `LangVersion 9.0`, `ArtifactsPath` en `dotnet/artifacts` y `Directory.Build.props` fuera de la raíz Unity.
4. **Tests:** NUnit 3.14.0 en `dotnet` y el NUnit 3.5 de Unity en el Editor, con tests compartidos limitados al subconjunto 3.5. `dotnet test` es la puerta rápida de los agentes, Unity EditMode la segunda puerta en CI y una suite golden en un player IL2CPP la tercera, antes de cada release.
5. **Primer trabajo de los agentes (sin esperar a Unity):** crear la estructura de la sección 6, portar el núcleo de reglas (`legal`, `cantidad`, `futil`, sello, victoria, canonicalización, hash de 64 bits) con tests, y después el PRNG. Antes de portar el PRNG, **el dueño decide** si la paridad con el prototipo exige MT19937 de CPython bit a bit o si se fija un PRNG nuevo (xoshiro256** o PCG32) y se regeneran los JSON de referencia con la versión C#. Recomendación [E]: PRNG nuevo y regenerar. El prototipo es una referencia y no un canon, y portar las rarezas de `random.Random` cuesta más que regenerar 100 niveles.
6. **Versión de Unity:** cualquier 6.x da el mismo C# 9. No usar 6.0 LTS (su soporte acaba en oct-2026). La elección entre 6.3 LTS y 6.7 LTS corresponde al informe del Editor; este tema no la condiciona.

## Riesgos y plan B

| # | Riesgo | Prob. | Impacto | Señal | Mitigación | Plan B |
|---|---|---|---|---|---|---|
| R1 | Un agente escribe C# 10+ o APIs de .NET 5+ en el núcleo | Alta | Bajo (se detecta enseguida) | Error de compilación en `dotnet build` | `LangVersion 9` + `netstandard2.1` + `CLAUDE.md` | — |
| R2 | Las dos compilaciones divergen (Roslyn del SDK frente al de Unity, cuya versión no se publica [NV]) | Baja | Medio | Compila en `dotnet` y falla o avisa en Unity | Puerta 2 (EditMode en CI) cuando cambia el paquete | Opción (b): DLL precompilada, el mismo IL en todas partes |
| R3 | Diferencias de runtime (hash, orden, `Sort`, `float`) entre CoreCLR y Mono/IL2CPP | Media | Alto (niveles o pistas que no cuadran) | Falla la suite golden en el player IL2CPP | Reglas de la sección 3.5: enteros, hash y PRNG propios, desempates totales | Calcular offline y enviar en el JSON del nivel (par, ruta de pista) lo que no cuadre en el cliente |
| R4 | El solver es lento en gama baja con IL2CPP | Media | Medio (pista o Aguas turbias lentas) | Benchmark de la sección 5.4 por debajo del presupuesto | `struct` + arrays + montículo propio; `Il2CppSetOption` en 1-3 métodos | Bajar el presupuesto de nodos, precalcular pistas offline, solver en hilo de fondo con resultado diferido [E] |
| R5 | Tests compartidos que usan APIs de NUnit posteriores a 3.5 | Media | Bajo | Fallo de compilación en Unity EditMode | Solo `Assert.That`; NUnit.Analyzers 3.9.0 | Dejar los tests completos solo en `dotnet` y una suite mínima de paridad en Unity |
| R6 | Unity 6.5+ trae `ext.nunit` 2.1.0 con una base distinta de 3.5 [NV] | Media | Bajo | Avisos u obsolescencias al actualizar Unity | Revisar el *changelog* al cambiar de versión de Unity | Ajustar el subconjunto permitido |
| R7 | `.meta` ausentes o regenerados con otros GUID | Media | Bajo | PR con `.meta` nuevos o que faltan | Paso "abrir Unity y confirmar" + comprobación de CI | — |
| R8 | El `.csproj` de plantilla pisa `ImplicitUsings`/`Nullable` | Alta la primera vez | Bajo | Error "global using" con C# 9 | Instrucción explícita en la sección 6.3 | — |
| R9 | NUnit 3.14 (2023) queda sin soporte en adaptadores futuros | Baja | Bajo | Un adaptador nuevo deja de descubrir tests | Fijar versiones exactas en el `.csproj` | Fijar el adaptador a la última versión compatible |
| R10 | Unity 7 o CoreCLR llega a móvil y se quiere C# 14 | — (futuro, 2027+) | Positivo | Anuncio de IL2CPP con .NET para Android [NV] | `netstandard2.1` sigue siendo consumible | Subir `LangVersion` en `dotnet/Directory.Build.props` y quitar el polyfill (`NET5_0_OR_GREATER`) |
| R11 | Cambio a Godot (plan B del motor) | Baja | Medio | Shader de líquido por debajo de 55 fps en 3 semanas (GDD) | El núcleo no depende de Unity | Godot 4.5+ con C# (.NET 8+, Android .NET 9+ [V]) referencia `dotnet/AF.Core` directamente; .NET 10 ya instalado |

## Fuentes

**.NET e instalación**
- Descarga de .NET 10 (10.0.401, 8-sep-2026, runtime 10.0.12, C# 14.0, instalador Arm64): https://dotnet.microsoft.com/en-us/download/dotnet/10.0 [V]
- Política de soporte (.NET 10 LTS hasta el 14-nov-2028; .NET 8 y 9 hasta el 10-nov-2026; STS = 2 años): https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core [V]
- Install .NET on Windows (matriz Arm64, rutas en PCs Arm, IDs de winget, Smart App Control, permisos de administrador): https://learn.microsoft.com/en-us/dotnet/core/install/windows [V]
- Instalador ARM64 (HEAD: 203.769.320 bytes, Last-Modified 8-sep-2026): https://download.microsoft.com/download/52d16011-1aa2-4a7e-a4be-769e1bdd32be/a61b639b-98cf-41d9-822d-0bb99c462cef/dotnet-sdk-10.0.401-win-arm64.exe [V-local]
- Salidas locales de `winget search Microsoft.DotNet.SDK` y `winget show Microsoft.DotNet.SDK.{8,9,10} --architecture arm64` (6-oct-2026) [V-local]
- Artifacts output layout: https://learn.microsoft.com/en-us/dotnet/core/sdk/artifacts-output [V]
- `dotnet test` (VSTest por defecto, MTP con `global.json`): https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test [V]
- Plantillas de `dotnet new` (classlib `netstandard2.1`, buildprops `--use-artifacts`, globaljson, sln/slnx): https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates [V]
- `dotnet new sln` crea SLNX en .NET 10: https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/dotnet-new-sln-slnx-default [V]
- `PriorityQueue<TElement,TPriority>` (solo net-6.0 a net-11.0): https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2?view=net-10.0 [V]
- `String.GetHashCode` (no estable entre implementaciones o procesos): https://learn.microsoft.com/en-us/dotnet/api/system.string.gethashcode?view=net-10.0 [V]
- `System.Random` (las secuencias pueden variar entre versiones de .NET): https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-random [V]
- Microsoft.CodeAnalysis.BannedApiAnalyzers 5.6.0 (2-jul-2026): https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers [V]

**Unity**
- Compilador C# de Unity 6.3 (C# 9.0, funciones no soportadas, `IsExternalInit`, `record` y serialización): https://docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html [V]
- Ídem en Unity 6.6 (C# 9.0): https://docs.unity3d.com/6000.6/Documentation/Manual/csharp-compiler.html [V]
- Históricos 2020.3 (C# 8 sin métodos de interfaz por defecto) y 2021.2 (C# 9): https://docs.unity3d.com/2020.3/Documentation/Manual/CSharpCompiler.html · https://docs.unity3d.com/2021.2/Documentation/Manual/CSharpCompiler.html [V]
- .NET profile support (Standard 2.1 por defecto, matriz de DLL): https://docs.unity3d.com/6000.3/Documentation/Manual/dotnet-profile-support.html [V]
- Formato `.asmdef`: https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definition-file-format.html [V]
- Propiedades del asmdef (No Engine References): https://docs.unity3d.com/6000.3/Documentation/Manual/class-AssemblyDefinitionImporter.html [V]
- Activos ocultos (`.` y `~`): https://docs.unity3d.com/6000.3/Documentation/Manual/SpecialFolders.html [V]
- Paquetes locales por ruta: https://docs.unity3d.com/6000.3/Documentation/Manual/upm-localpath.html [V]
- Disposición de paquetes personalizados: https://docs.unity3d.com/6000.3/Documentation/Manual/cus-layout.html [V]
- Tests de paquetes (`testables`; automáticos en `Packages/`): https://docs.unity3d.com/6000.3/Documentation/Manual/cus-tests.html [V]
- Limitaciones de IL2CPP (genéricos virtuales, `MakeGenericType`, Emit, `dynamic`): https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-restrictions.html [V]
- Gestión de tipos referencia (*closures*, *boxing*, `params`, cadenas): https://docs.unity3d.com/6000.3/Documentation/Manual/performance-reference-types.html [V]
- Comprobaciones de runtime de IL2CPP (`Il2CppSetOption`): https://docs.unity3d.com/Manual/il2cpp-runtime-checks.html [V]
- Soporte de Unity 6 (6.3 LTS hasta dic-2027; 6.0 LTS hasta oct-2026): https://unity.com/releases/unity-6/support [V]
- Unity 6000.6.0f1 (31-ago-2026): https://unity.com/releases/editor/whats-new/6000.6.0f1 [V]
- CoreCLR, Scripting and Serialization Update, junio de 2026 (6.7 LTS última sobre Mono; experimentales limitadas a .NET Standard 2.1 y C# 9; Unity 7.0 con .NET 10 y C# 14): https://discussions.unity.com/t/coreclr-scripting-and-serialization-update-june-2026/1723299 [V]
- CoreCLR, Scripting and ECS Status Update, marzo de 2026: https://discussions.unity.com/t/coreclr-scripting-and-ecs-status-update-march-2026/1711852 [V]
- Error de `record` en 6.3 LTS y *workaround* (mar-2026): https://discussions.unity.com/t/c-record-type-error-in-6-3-lts/1713783 [V]
- Petición de actualizar el NUnit de UTF (sigue en 3.5 en 6000.0.30f1): https://discussions.unity.com/t/upgrade-unity-test-frameworks-nunit-dependency/1634907 [V]
- `com.unity.ext.nunit` ("Based on NUnit version 3.5", 2.0.5): https://github.com/needle-mirror/com.unity.ext.nunit · https://docs.unity3d.com/Packages/com.unity.ext.nunit@2.0/changelog/CHANGELOG.html [V]
- *Changelog* de Unity Test Framework 1.6 (1.5.1 "embedded package"; 1.6.0 del 11-ago-2025): https://docs.unity3d.com/Packages/com.unity.test-framework@1.6/changelog/CHANGELOG.html [V]
- Notas de 6000.5.0b3 (test-framework 1.6.0 → 1.7.0; ext.nunit 2.0.5 → 2.1.0), espejo de terceros: https://unityreleases.com/releases/6000.5.0b3 [V-terceros]
- Polyfills de la comunidad, verificados en Mono e IL2CPP: https://github.com/xpTURN/Polyfill [V-terceros]
- `Directory.Build.props` que redirige rutas y rompe la carga de proyectos en Rider: https://rider-support.jetbrains.com/hc/en-us/community/posts/360007707919/comments/360001493819 [V-terceros]

**NUnit**
- NUnit 3.14.0 (3-nov-2023; netstandard2.0/net35/net40/net45; historial 3.x/4.x/5.x): https://www.nuget.org/packages/NUnit/3.14.0 [V]
- Versiones de NUnit (5.0.0 del 27-sep-2026; 4.6.1 del 19-may-2026...): https://github.com/nunit/nunit/releases [V]
- Notas de versión del framework (4.0: mínimo net462 y net6.0; 5.0: cambios incompatibles): https://docs.nunit.org/articles/nunit/release-notes/framework.html [V]
- Guía de migración a NUnit 4 (`ClassicAssert`, sin compatibilidad de código con 3.14): https://docs.nunit.org/articles/nunit/release-notes/Nunit4.0-MigrationGuide.html [V]
- NUnit3TestAdapter 6.3.0: https://www.nuget.org/packages/NUnit3TestAdapter [V]
- Microsoft.NET.Test.Sdk 18.10.1: https://www.nuget.org/packages/Microsoft.NET.Test.Sdk [V]
- NUnit.Analyzers (3.9.0 para Unity 2022.2+; 4.15.0 la última): https://github.com/nunit/nunit.analyzers · https://www.nuget.org/packages/NUnit.Analyzers [V]

**Godot (plan B)**
- C# en Godot (4.5+ pide .NET 8+; Android .NET 9+; móvil experimental): https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html [V]

**Repositorio (lectura local)**
- `docs/gdd/08-arquitectura-tecnica-publicacion.md` §2.1-2.4, §3, §12.1, §14.2 y §15; `docs/gdd/03-niveles-generacion-dificultad.md` §17 y la pregunta abierta #7; `docs/GDD.md` §9 #2-3; `tools/levelgen_proto.py`, líneas 29-34, 256, 358-372 y 470 (uso de `hashlib.sha256` y `random.Random`) [V-local]
