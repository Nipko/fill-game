#Requires -Version 7.2
<#
.SYNOPSIS
    Compuerta del mapping de R8 de un AAB de Amazon Fill (D-059, anadido del 2026-11-06: R8 en el release).

.DESCRIPTION
    Con R8 (Minify Release) el build de release renombra y quita clases Java. Esta compuerta lee el mapping.txt de R8
    que el Android Gradle Plugin guarda dentro del AAB (BUNDLE-METADATA/com.android.tools.build.obfuscation/proguard.map;
    es el que usa Google Play para desofuscar los fallos y los ANR) o uno dado con -Mapping, y falla si:

      1. el AAB no lleva mapping (sin el, Play no desofusca y las compuertas de abajo no ven nada);
      2. una clase de la lista de clases JNI (-KeepList, por defecto ci/r8-jni-keep.txt) falta o sale renombrada:
         el juego o un SDK la busca por su nombre (AndroidJavaClass, AndroidJavaProxy, manifiesto, reflexion) y fallaria
         en el telefono. Las marcadas con '?' son opcionales: solo fallan si estan y salen renombradas;
      3. el mapping lleva una clase original de un paquete prohibido (com.google.firebase.appcheck.debug, com.applovin,
         com.unity.purchasing). check-release-classes.ps1, check-no-applovin.ps1 y check-no-unity-iap.ps1 buscan los
         nombres en el dex; si R8 los renombrara, solo el mapping los delataria.

    Solo lee el artefacto; no necesita bundletool ni java. Funciona en Windows (PowerShell 7) y en el runner Ubuntu.
    Mensajes en ASCII.

    Codigos de salida:
      0  mapping presente, clases JNI con su nombre y ningun paquete prohibido
      1  falta una clase JNI o sale renombrada, o hay un paquete prohibido (o el autotest fallo); manda sobre el 2
      2  argumentos incorrectos, ningun .aab, artefacto ilegible, AAB sin mapping o lista de clases ilegible

.PARAMETER Path
    Archivo .aab o carpeta (busca *.aab dentro, no recursivo). "--self-test" equivale a -SelfTest.

.PARAMETER Mapping
    Un mapping.txt suelto en lugar de un AAB (el <nombre>_mapping.txt que Unity deja junto al AAB).

.PARAMETER KeepList
    Lista de clases JNI (por defecto ci/r8-jni-keep.txt junto a este script).

.PARAMETER SelfTest
    Autotest con artefactos sinteticos.

.EXAMPLE
    pwsh -NoProfile -File ci/check-r8-mapping.ps1 Builds/Android/rehearsal.aab
.EXAMPLE
    pwsh -NoProfile -File ci/check-r8-mapping.ps1 -Mapping Builds/Android/rehearsal_mapping.txt
.EXAMPLE
    pwsh -NoProfile -File ci/check-r8-mapping.ps1 --self-test
#>
param(
    [string]$Path = '',
    [string]$Mapping = '',
    [string]$KeepList = '',
    [switch]$SelfTest
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$MapEntry = 'BUNDLE-METADATA/com.android.tools.build.obfuscation/proguard.map'
$ForbiddenPackages = @('com.google.firebase.appcheck.debug', 'com.applovin', 'com.unity.purchasing')
$DefaultKeepList = Join-Path $PSScriptRoot 'r8-jni-keep.txt'

function Stop-WithCode {
    param([int]$Code, [string]$Message)
    [Console]::Error.WriteLine($Message)
    exit $Code
}

# Lista de clases: @{ Name; Optional }. Lanza si no se puede leer o esta vacia.
function Read-KeepList([string]$File) {
    $items = [System.Collections.Generic.List[object]]::new()
    foreach ($raw in [System.IO.File]::ReadAllLines($File)) {
        $line = $raw
        $hash = $line.IndexOf('#')
        if ($hash -ge 0) { $line = $line.Substring(0, $hash) }
        $line = $line.Trim()
        if (-not $line) { continue }
        $optional = $line.StartsWith('?')
        if ($optional) { $line = $line.Substring(1).Trim() }
        if ($line -notmatch '^[A-Za-z_][A-Za-z0-9_$]*(\.[A-Za-z_][A-Za-z0-9_$]*)+$') { throw "linea invalida en la lista: '$raw'" }
        $items.Add([pscustomobject]@{ Name = $line; Optional = $optional })
    }

    if ($items.Count -eq 0) { throw "la lista $File no tiene clases" }
    return ,$items
}

# Clases del mapping: diccionario nombre original -> nombre ofuscado (solo las lineas de clase "a.b.C -> x.y:").
function Read-MappingClasses([System.IO.TextReader]$Reader) {
    $classes = [System.Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
    while ($null -ne ($line = $Reader.ReadLine())) {
        if ($line.Length -eq 0 -or $line[0] -eq '#' -or $line[0] -eq ' ' -or $line[0] -eq "`t") { continue }
        if (-not $line.EndsWith(':')) { continue }
        $arrow = $line.IndexOf(' -> ', [StringComparison]::Ordinal)
        if ($arrow -le 0) { continue }
        $orig = $line.Substring(0, $arrow)
        $obf = $line.Substring($arrow + 4, $line.Length - $arrow - 5)
        $classes[$orig] = $obf
    }

    return $classes
}

# Hallazgos sobre un mapping ya leido (lista vacia = limpio).
function Get-Findings($Classes, $Keep) {
    $findings = [System.Collections.Generic.List[string]]::new()
    if ($Classes.Count -eq 0) {
        $findings.Add('el mapping no tiene ninguna clase (no es un mapping de R8)')
        return ,$findings
    }

    foreach ($k in $Keep) {
        $obf = $null
        if ($Classes.TryGetValue($k.Name, [ref]$obf)) {
            if ($obf -ne $k.Name) { $findings.Add("clase JNI renombrada por R8: $($k.Name) -> $obf (falta una regla keep)") }
        }
        elseif (-not $k.Optional) {
            $findings.Add("clase JNI ausente del AAB: $($k.Name) (R8 la quito o el SDK no esta)")
        }
    }

    foreach ($orig in $Classes.Keys) {
        foreach ($pkg in $ForbiddenPackages) {
            if ($orig.StartsWith($pkg + '.', [StringComparison]::Ordinal)) {
                $findings.Add("clase de un paquete prohibido en el mapping: $orig (paquete $pkg)")
                break
            }
        }
    }

    return ,$findings
}

function Write-Result([string]$Label, $Findings, [int]$KeepCount, [int]$ClassCount) {
    if ($Findings.Count -eq 0) {
        Write-Host "OK    ${Label}: mapping con $ClassCount clases; $KeepCount clases JNI con su nombre; sin paquetes prohibidos"
        return 0
    }

    $shown = 0
    foreach ($f in $Findings) {
        if ($shown -ge 40) { Write-Host "FALLO ${Label}: ... y $($Findings.Count - $shown) mas"; break }
        Write-Host "FALLO ${Label}: $f"
        $shown++
    }

    return 1
}

# 0 limpio, 1 con hallazgos, 2 ilegible o sin mapping.
function Test-Aab([string]$File, $Keep) {
    try {
        $zip = [System.IO.Compression.ZipFile]::OpenRead($File)
    }
    catch {
        Write-Host "ERROR ${File}: no se puede leer como ZIP ($($_.Exception.GetType().Name))"
        return 2
    }

    try {
        $entry = $zip.GetEntry($MapEntry)
        if ($null -eq $entry) {
            Write-Host "ERROR ${File}: sin $MapEntry (el AAB no se construyo con R8, o el AGP no guardo el mapping)"
            return 2
        }

        $reader = [System.IO.StreamReader]::new($entry.Open())
        try { $classes = Read-MappingClasses $reader } finally { $reader.Dispose() }
    }
    finally {
        $zip.Dispose()
    }

    return Write-Result $File (Get-Findings $classes $Keep) $Keep.Count $classes.Count
}

function Test-MappingFile([string]$File, $Keep) {
    if (-not (Test-Path -LiteralPath $File -PathType Leaf)) {
        Write-Host "ERROR: no existe el mapping '$File'"
        return 2
    }

    $reader = [System.IO.StreamReader]::new($File)
    try { $classes = Read-MappingClasses $reader } finally { $reader.Dispose() }
    return Write-Result $File (Get-Findings $classes $Keep) $Keep.Count $classes.Count
}

function Resolve-Aabs([string]$Target) {
    if (Test-Path -LiteralPath $Target -PathType Leaf) { return @((Resolve-Path -LiteralPath $Target).Path) }
    if (Test-Path -LiteralPath $Target -PathType Container) {
        return @(Get-ChildItem -LiteralPath $Target -File | Where-Object { $_.Extension -eq '.aab' } | ForEach-Object FullName)
    }

    return @(Get-ChildItem -Path $Target -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -eq '.aab' } | ForEach-Object FullName)
}

function Invoke-Gate([string]$Target, [string]$MapFile, [string]$ListFile) {
    try {
        $keep = Read-KeepList $ListFile
    }
    catch {
        Write-Host "ERROR: lista de clases JNI ilegible ($ListFile): $($_.Exception.Message)"
        return 2
    }

    if ($MapFile) { return Test-MappingFile $MapFile $keep }

    $files = @(Resolve-Aabs $Target)
    if ($files.Count -eq 0) {
        Write-Host "ERROR: ningun .aab en '$Target'"
        return 2
    }

    $worst = 0
    foreach ($file in $files) {
        $code = Test-Aab $file $keep
        if ($code -eq 1) { $worst = 1 }
        elseif ($code -eq 2 -and $worst -eq 0) { $worst = 2 }
    }

    return $worst
}

function New-TestZip([string]$File, [hashtable]$Entries) {
    if (Test-Path -LiteralPath $File) { Remove-Item -LiteralPath $File -Force }
    $zip = [System.IO.Compression.ZipFile]::Open($File, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($name in $Entries.Keys) {
            $entry = $zip.CreateEntry($name)
            $s = $entry.Open()
            try {
                $bytes = [System.Text.Encoding]::UTF8.GetBytes([string]$Entries[$name])
                $s.Write($bytes, 0, $bytes.Length)
            }
            finally { $s.Dispose() }
        }
    }
    finally { $zip.Dispose() }
}

function Invoke-SelfTest {
    $dir = Join-Path ([System.IO.Path]::GetTempPath()) ("af-check-r8-mapping-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $dir | Out-Null
    $list = Join-Path $dir 'keep.txt'
    [System.IO.File]::WriteAllText($list, "# lista`ncom.amazonfill.billing.AfBilling`ncom.amazonfill.audiofocus.AfAudioFocus`$Listener  # anidada`n?com.example.Optional`n")
    $head = "# compiler: R8`n# pg_map_id: 1`n"
    $good = $head + "com.amazonfill.billing.AfBilling -> com.amazonfill.billing.AfBilling:`n# {`"id`":`"sourceFile`",`"fileName`":`"AfBilling.java`"}`n    void start() -> start`n" +
        "com.amazonfill.audiofocus.AfAudioFocus`$Listener -> com.amazonfill.audiofocus.AfAudioFocus`$Listener:`n" +
        "com.google.android.gms.internal.Zz -> a.b:`n    int x -> a`n"
    $cases = @(
        @{ Name = 'limpio.aab'; Expect = 0; Map = $good }
        @{ Name = 'opcional-presente.aab'; Expect = 0; Map = $good + "com.example.Optional -> com.example.Optional:`n" }
        @{ Name = 'opcional-renombrada.aab'; Expect = 1; Map = $good + "com.example.Optional -> c.d:`n" }
        @{ Name = 'renombrada.aab'; Expect = 1; Map = $good.Replace('com.amazonfill.billing.AfBilling -> com.amazonfill.billing.AfBilling:', 'com.amazonfill.billing.AfBilling -> e.f:') }
        @{ Name = 'ausente.aab'; Expect = 1; Map = $head + "com.amazonfill.audiofocus.AfAudioFocus`$Listener -> com.amazonfill.audiofocus.AfAudioFocus`$Listener:`n" }
        @{ Name = 'appcheck-debug.aab'; Expect = 1; Map = $good + "com.google.firebase.appcheck.debug.internal.X -> g.h:`n" }
        @{ Name = 'applovin.aab'; Expect = 1; Map = $good + "com.applovin.sdk.AppLovinSdk -> i.j:`n" }
        @{ Name = 'purchasing.aab'; Expect = 1; Map = $good + "com.unity.purchasing.googleplay.X -> k.l:`n" }
        @{ Name = 'parecido.aab'; Expect = 0; Map = $good + "com.applovinish.Y -> m.n:`n" }
        @{ Name = 'vacio.aab'; Expect = 1; Map = $head }
    )

    $failures = 0
    try {
        $keep = Read-KeepList $list
        foreach ($c in $cases) {
            $file = Join-Path $dir $c.Name
            New-TestZip $file @{ $MapEntry = $c.Map; 'base/dex/classes.dex' = 'dex' }
            $got = Test-Aab $file $keep
            if ($got -ne $c.Expect) {
                Write-Host "AUTOTEST FALLO $($c.Name): esperado $($c.Expect), obtenido $got"
                $failures++
            }
        }

        $noMap = Join-Path $dir 'sin-mapping.aab'
        New-TestZip $noMap @{ 'base/dex/classes.dex' = 'dex' }
        if ((Test-Aab $noMap $keep) -ne 2) { Write-Host 'AUTOTEST FALLO sin-mapping.aab: esperado 2'; $failures++ }
        $notZip = Join-Path $dir 'roto.aab'
        [System.IO.File]::WriteAllText($notZip, 'no es un zip')
        if ((Test-Aab $notZip $keep) -ne 2) { Write-Host 'AUTOTEST FALLO roto.aab: esperado 2'; $failures++ }
        if ((Invoke-Gate (Join-Path $dir 'no-existe.aab') '' $list) -ne 2) { Write-Host 'AUTOTEST FALLO ruta inexistente: esperado 2'; $failures++ }
        $badList = Join-Path $dir 'mala.txt'
        [System.IO.File]::WriteAllText($badList, "no es una clase`n")
        if ((Invoke-Gate (Join-Path $dir 'limpio.aab') '' $badList) -ne 2) { Write-Host 'AUTOTEST FALLO lista invalida: esperado 2'; $failures++ }
        $loose = Join-Path $dir 'mapping.txt'
        [System.IO.File]::WriteAllText($loose, $good)
        if ((Invoke-Gate '' $loose $list) -ne 0) { Write-Host 'AUTOTEST FALLO mapping suelto: esperado 0'; $failures++ }

        # Carpeta con uno limpio, uno sin mapping y uno con AppLovin: falla con 1.
        $mixed = Join-Path $dir 'mixta'
        New-Item -ItemType Directory -Path $mixed | Out-Null
        Copy-Item (Join-Path $dir 'limpio.aab') $mixed
        Copy-Item $noMap $mixed
        Copy-Item (Join-Path $dir 'applovin.aab') $mixed
        if ((Invoke-Gate $mixed '' $list) -ne 1) { Write-Host 'AUTOTEST FALLO carpeta mixta: esperado 1'; $failures++ }
    }
    finally {
        Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
    }

    $total = $cases.Count + 6
    if ($failures -gt 0) {
        Write-Host "AUTOTEST: $failures de $total casos fallaron"
        return 1
    }

    Write-Host "AUTOTEST: $total casos OK"
    return 0
}

if ($Path -eq '--self-test' -or @($args) -contains '--self-test') { $SelfTest = $true; $Path = '' }
if ($SelfTest) { exit (Invoke-SelfTest) }
if (@($args).Count -gt 0) { Stop-WithCode 2 "ERROR: argumentos no reconocidos: $($args -join ' ')" }
if (-not $Path -and -not $Mapping) { Stop-WithCode 2 'Uso: check-r8-mapping.ps1 <aab|carpeta|comodin> | -Mapping <mapping.txt> [-KeepList <lista>] | --self-test' }
if ($Path -and $Mapping) { Stop-WithCode 2 'ERROR: -Mapping va solo, sin AAB' }
if (-not $KeepList) { $KeepList = $DefaultKeepList }
exit (Invoke-Gate $Path $Mapping $KeepList)
