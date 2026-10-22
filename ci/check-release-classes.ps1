#Requires -Version 7.2
<#
.SYNOPSIS
    Compuerta de clases prohibidas en un build de release de Amazon Fill (D-033, anadido del 2026-10-22, punto 9).

.DESCRIPTION
    Lee los .dex de un .aab (<modulo>/dex/classes*.dex) o de un .apk (classes*.dex) y falla si alguno DEFINE una
    clase (class_def) de un paquete prohibido en release, o de un subpaquete suyo. Una simple referencia a la clase
    (type_id sin class_def) no cuenta: lo que se mira es el codigo que viaja en el artefacto.

    Paquetes prohibidos (lista $ForbiddenPackages; -ForbidPackage anade mas sin tocar el script):
      - com.google.firebase.appcheck.debug: la biblioteca firebase-appcheck-debug (proveedor de depuracion de
        App Check) solo va en los builds de desarrollo (D-039, anadido del 2026-10-21; D-033 punto 9). La excluye
        Assets/Plugins/Android/settingsTemplate.gradle de toda configuracion "release*" de Gradle, que es la que
        usa Unity sin Development Build (BuildRelease, con y sin -afRehearsal); docs/dev/firebase-cliente.md sec. 4.

    No necesita herramientas externas: el formato DEX se lee directamente (cabecera, string_ids, type_ids y
    class_defs). Funciona en Windows (PowerShell 7) y en el runner Ubuntu de GitHub Actions (pwsh).

    Codigos de salida:
      0  ningun artefacto define clases de un paquete prohibido
      1  algun artefacto las define (o el autotest fallo)
      2  argumentos incorrectos, archivo inexistente, ZIP ilegible, artefacto sin .dex o .dex ilegible

.PARAMETER Path
    Archivo .aab o .apk, carpeta (busca *.aab y *.apk dentro, no recursivo) o comodin.
    "--self-test" equivale a -SelfTest.

.PARAMETER ForbidPackage
    Paquetes Java prohibidos ademas de los fijos (nombre con puntos, p. ej. com.ejemplo.sdk); admite comas.

.EXAMPLE
    pwsh -NoProfile -File ci/check-release-classes.ps1 build/Android
.EXAMPLE
    pwsh -NoProfile -File ci/check-release-classes.ps1 Builds/Android/rehearsal.aab
.EXAMPLE
    pwsh -NoProfile -File ci/check-release-classes.ps1 --self-test
#>
param(
    [string]$Path = '',
    [string[]]$ForbidPackage = @(),
    [switch]$SelfTest
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

# Paquete prohibido en release y por que. Anadir aqui cada biblioteca que solo puede viajar en desarrollo.
$ForbiddenPackages = @(
    [pscustomobject]@{ Package = 'com.google.firebase.appcheck.debug'; Why = 'firebase-appcheck-debug solo en desarrollo (D-033, anadido del 2026-10-22, punto 9)' }
)

$MaxListed = 10   # clases que se listan por paquete y artefacto

function Stop-WithCode {
    param([int]$Code, [string]$Message)
    [Console]::Error.WriteLine($Message)
    exit $Code
}

# Paquete con puntos -> prefijo de descriptor DEX ("Lcom/x/y/"). Devuelve $null si el nombre no es valido.
function ConvertTo-DescriptorPrefix([string]$Package) {
    $p = $Package.Trim()
    if ($p -notmatch '^[A-Za-z_$][A-Za-z0-9_$]*(\.[A-Za-z_$][A-Za-z0-9_$]*)*$') { return $null }
    return 'L' + $p.Replace('.', '/') + '/'
}

function Read-UInt32([byte[]]$B, [long]$Off) {
    if ($Off -lt 0 -or $Off + 4 -gt $B.LongLength) { throw "lectura fuera del .dex en 0x$('{0:x}' -f $Off)" }
    return [BitConverter]::ToUInt32($B, [int]$Off)
}

# Cadena de string_data (uleb128 con la longitud en UTF-16 y luego MUTF-8 terminado en 0). Los descriptores de
# clase son ASCII en la practica; los bytes no ASCII se decodifican como UTF-8, que basta para comparar prefijos ASCII.
function Read-DexString([byte[]]$B, [long]$Off) {
    $i = $Off
    $shift = 0
    while ($true) {
        if ($i -ge $B.LongLength -or $shift -gt 28) { throw "uleb128 ilegible en 0x$('{0:x}' -f $Off)" }
        $byte = $B[$i]; $i++   # ojo: las variables de PowerShell no distinguen mayusculas ($b seria $B)
        if (($byte -band 0x80) -eq 0) { break }
        $shift += 7
    }
    $end = [Array]::IndexOf($B, [byte]0, [int]$i)
    if ($end -lt 0) { throw "cadena sin terminar en 0x$('{0:x}' -f $Off)" }
    return [System.Text.Encoding]::UTF8.GetString($B, [int]$i, [int]($end - $i))
}

# Descriptores de las clases DEFINIDAS en un .dex (class_defs).
function Get-DexClassDescriptors([byte[]]$B) {
    if ($B.LongLength -lt 0x70) { throw 'demasiado corto para una cabecera DEX' }
    $magic = [System.Text.Encoding]::ASCII.GetString($B, 0, 4)
    if ($magic -ne "dex`n" -or $B[7] -ne 0) { throw 'no es un .dex (firma incorrecta)' }
    $stringIdsSize = Read-UInt32 $B 0x38
    $stringIdsOff = Read-UInt32 $B 0x3C
    $typeIdsSize = Read-UInt32 $B 0x40
    $typeIdsOff = Read-UInt32 $B 0x44
    $classDefsSize = Read-UInt32 $B 0x60
    $classDefsOff = Read-UInt32 $B 0x64
    $result = [System.Collections.Generic.List[string]]::new()
    for ($k = 0; $k -lt $classDefsSize; $k++) {
        $classIdx = Read-UInt32 $B ($classDefsOff + 0x20 * [long]$k)
        if ($classIdx -ge $typeIdsSize) { throw "class_idx $classIdx fuera de type_ids" }
        $descIdx = Read-UInt32 $B ($typeIdsOff + 4 * [long]$classIdx)
        if ($descIdx -ge $stringIdsSize) { throw "descriptor_idx $descIdx fuera de string_ids" }
        $dataOff = Read-UInt32 $B ($stringIdsOff + 4 * [long]$descIdx)
        $result.Add((Read-DexString $B $dataOff))
    }
    return , $result
}

# Entradas .dex de un .apk (classes*.dex en la raiz) o de un .aab (<modulo>/dex/*.dex).
function Test-DexEntry([string]$Name) {
    return $Name -match '^classes\d*\.dex$' -or $Name -match '^[^/]+/dex/[^/]+\.dex$'
}

# Revisa un artefacto. Devuelve @{ Dex = n; Classes = n; Hits = @{ paquete = [lista] } }; lanza si no se puede leer.
function Invoke-ArtifactScan([string]$File, [object[]]$Rules) {
    $zip = [System.IO.Compression.ZipFile]::OpenRead($File)
    try {
        $dexCount = 0; $classCount = 0
        $hits = [ordered]@{}
        foreach ($r in $Rules) { $hits[$r.Package] = [System.Collections.Generic.List[string]]::new() }
        foreach ($entry in $zip.Entries) {
            if (-not (Test-DexEntry $entry.FullName)) { continue }
            $dexCount++
            $ms = [System.IO.MemoryStream]::new()
            $s = $entry.Open()
            try { $s.CopyTo($ms) } finally { $s.Dispose() }
            try { $descs = Get-DexClassDescriptors $ms.ToArray() }
            catch { throw "$($entry.FullName): $($_.Exception.Message)" }
            $classCount += $descs.Count
            foreach ($d in $descs) {
                foreach ($r in $Rules) {
                    if ($d.StartsWith($r.Prefix, [System.StringComparison]::Ordinal)) {
                        $hits[$r.Package].Add($d.Substring(1).TrimEnd(';').Replace('/', '.'))
                    }
                }
            }
        }
        if ($dexCount -eq 0) { throw 'no contiene ningun .dex (classes*.dex ni <modulo>/dex/*.dex)' }
        return @{ Dex = $dexCount; Classes = $classCount; Hits = $hits }
    }
    finally { $zip.Dispose() }
}

function Get-Rules([string[]]$Extra) {
    $rules = [System.Collections.Generic.List[object]]::new()
    $all = @($ForbiddenPackages) + @($Extra | ForEach-Object { [pscustomobject]@{ Package = $_; Why = 'prohibido por -ForbidPackage' } })
    foreach ($p in $all) {
        $prefix = ConvertTo-DescriptorPrefix $p.Package
        if (-not $prefix) { return $p.Package }   # nombre invalido: el llamador sale con 2
        if (@($rules | Where-Object { $_.Package -eq $p.Package }).Count -gt 0) { continue }
        $rules.Add([pscustomobject]@{ Package = $p.Package; Prefix = $prefix; Why = $p.Why })
    }
    return , $rules
}

# --- Autotest -----------------------------------------------------------------------------------
# DEX minimo valido para este lector: cabecera de 0x70 bytes, string_ids, type_ids, class_defs y string_data.
# $Defined son descriptores con class_def; $Referenced, solo con type_id (referencias sin definicion).
function New-TestDex([string[]]$Defined, [string[]]$Referenced = @()) {
    $types = @($Defined) + @($Referenced)
    $n = $types.Count
    $stringIdsOff = 0x70
    $typeIdsOff = $stringIdsOff + 4 * $n
    $classDefsOff = $typeIdsOff + 4 * $n
    $dataOff = $classDefsOff + 0x20 * $Defined.Count
    $data = [System.IO.MemoryStream]::new()
    $offsets = @()
    foreach ($t in $types) {
        $offsets += $dataOff + $data.Length
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($t)
        $len = $t.Length
        do { $v = $len -band 0x7F; $len = $len -shr 7; if ($len -gt 0) { $v = $v -bor 0x80 }; $data.WriteByte([byte]$v) } while ($len -gt 0)
        $data.Write($bytes, 0, $bytes.Length); $data.WriteByte(0)
    }
    $buf = [byte[]]::new($dataOff + $data.Length)
    [System.Text.Encoding]::ASCII.GetBytes("dex`n035").CopyTo($buf, 0)
    $put = { param($o, $v) [BitConverter]::GetBytes([uint32]$v).CopyTo($buf, $o) }
    & $put 0x38 $n; & $put 0x3C $stringIdsOff; & $put 0x40 $n; & $put 0x44 $typeIdsOff
    & $put 0x60 $Defined.Count; & $put 0x64 $classDefsOff
    for ($i = 0; $i -lt $n; $i++) { & $put ($stringIdsOff + 4 * $i) $offsets[$i]; & $put ($typeIdsOff + 4 * $i) $i }
    for ($i = 0; $i -lt $Defined.Count; $i++) { & $put ($classDefsOff + 0x20 * $i) $i }
    $data.ToArray().CopyTo($buf, $dataOff)
    return , $buf
}

function New-TestZip([string]$File, [hashtable]$Entries) {
    if (Test-Path -LiteralPath $File) { Remove-Item -LiteralPath $File -Force }
    $zip = [System.IO.Compression.ZipFile]::Open($File, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($k in $Entries.Keys) {
            $s = $zip.CreateEntry($k).Open()
            try { $bytes = [byte[]]$Entries[$k]; $s.Write($bytes, 0, $bytes.Length) } finally { $s.Dispose() }
        }
    }
    finally { $zip.Dispose() }
}

function Invoke-SelfTest {
    $tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("af-release-classes-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tmp | Out-Null
    $bad = 0
    $game = 'Lcom/unity3d/player/UnityPlayerActivity;'
    $pi = 'Lcom/google/firebase/appcheck/playintegrity/PlayIntegrityAppCheckProviderFactory;'
    $dbg = 'Lcom/google/firebase/appcheck/debug/DebugAppCheckProviderFactory;'
    $dbgSub = 'Lcom/google/firebase/appcheck/debug/internal/DebugAppCheckProvider;'
    $near = 'Lcom/google/firebase/appcheck/debugx/Otra;'
    $manifest = [System.Text.Encoding]::ASCII.GetBytes('x')
    $rules = Get-Rules @()
    # Nombre, entradas del ZIP, paquetes extra, paquetes que deben salir con clases (vacio = limpio), o 'ERROR'.
    $cases = @(
        @{ Name = 'apk limpio (Play Integrity, juego)'; File = 'a.apk'; Entries = @{ 'classes.dex' = (New-TestDex @($game, $pi)); 'AndroidManifest.xml' = $manifest }; Extra = @(); Expect = @() }
        @{ Name = 'apk con la fabrica de depuracion en classes2.dex'; File = 'b.apk'; Entries = @{ 'classes.dex' = (New-TestDex @($game)); 'classes2.dex' = (New-TestDex @($pi, $dbg)) }; Extra = @(); Expect = @('com.google.firebase.appcheck.debug') }
        @{ Name = 'aab con la clase en base/dex'; File = 'c.aab'; Entries = @{ 'base/dex/classes.dex' = (New-TestDex @($game, $dbg)); 'base/manifest/AndroidManifest.xml' = $manifest }; Extra = @(); Expect = @('com.google.firebase.appcheck.debug') }
        @{ Name = 'subpaquete de debug tambien cuenta'; File = 'd.aab'; Entries = @{ 'base/dex/classes.dex' = (New-TestDex @($dbgSub)) }; Extra = @(); Expect = @('com.google.firebase.appcheck.debug') }
        @{ Name = 'paquete parecido (debugx) no cuenta'; File = 'e.apk'; Entries = @{ 'classes.dex' = (New-TestDex @($near, $game)) }; Extra = @(); Expect = @() }
        @{ Name = 'solo referencia (type_id sin class_def) no cuenta'; File = 'f.apk'; Entries = @{ 'classes.dex' = (New-TestDex @($game) @($dbg)) }; Extra = @(); Expect = @() }
        @{ Name = 'dex de un modulo de funciones (feature/dex)'; File = 'g.aab'; Entries = @{ 'base/dex/classes.dex' = (New-TestDex @($game)); 'feature/dex/classes.dex' = (New-TestDex @($dbg)) }; Extra = @(); Expect = @('com.google.firebase.appcheck.debug') }
        @{ Name = '-ForbidPackage anade un paquete'; File = 'h.apk'; Entries = @{ 'classes.dex' = (New-TestDex @($game, 'Lcom/ejemplo/sdk/Cosa;')) }; Extra = @('com.ejemplo.sdk'); Expect = @('com.ejemplo.sdk') }
        @{ Name = 'sin .dex falla cerrado'; File = 'i.apk'; Entries = @{ 'AndroidManifest.xml' = $manifest; 'assets/classes.dex.txt' = $manifest }; Extra = @(); Expect = 'ERROR' }
        @{ Name = '.dex con firma incorrecta'; File = 'j.apk'; Entries = @{ 'classes.dex' = [byte[]]::new(0x80) }; Extra = @(); Expect = 'ERROR' }
        @{ Name = '.dex truncado'; File = 'k.apk'; Entries = @{ 'classes.dex' = [byte[]]((New-TestDex @($game, $dbg))[0..0x77]) }; Extra = @(); Expect = 'ERROR' }
    )
    try {
        foreach ($c in $cases) {
            $file = Join-Path $tmp $c.File
            New-TestZip $file $c.Entries
            $caseRules = if ($c.Extra.Count -gt 0) { Get-Rules $c.Extra } else { $rules }
            $got = $null
            try {
                $scan = Invoke-ArtifactScan $file $caseRules
                $got = @($scan.Hits.Keys | Where-Object { $scan.Hits[$_].Count -gt 0 })
            }
            catch { $got = 'ERROR' }
            $ok = if ($c.Expect -is [string]) { $got -is [string] -and $got -eq $c.Expect }
                  else { $got -isnot [string] -and (@($got) -join ',') -eq (@($c.Expect) -join ',') }
            if ($ok) { Write-Output "[OK]    $($c.Name)" }
            else { $bad++; Write-Output "[FALLA] $($c.Name): esperado '$(@($c.Expect) -join ',')', obtenido '$(@($got) -join ',')'" }
        }
        # Nombres de paquete invalidos en -ForbidPackage.
        $invalid = Get-Rules @('com..mal')
        if ($invalid -is [string] -and $invalid -eq 'com..mal') { Write-Output '[OK]    -ForbidPackage invalido se rechaza' }
        else { $bad++; Write-Output '[FALLA] -ForbidPackage invalido no se rechaza' }
    }
    finally { Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue }
    $total = $cases.Count + 1
    if ($bad -gt 0) { Stop-WithCode 1 "AUTOTEST: $bad de $total casos fallan." }
    Write-Output "AUTOTEST OK: $total casos."
    exit 0
}

# --- Principal ----------------------------------------------------------------------------------
if ($SelfTest -or $Path -eq '--self-test' -or @($args) -contains '--self-test') { Invoke-SelfTest }
if (@($args).Count -gt 0) { Stop-WithCode 2 "ERROR: argumentos no reconocidos: $($args -join ' ')" }

$usage = 'Uso: pwsh -NoProfile -File ci/check-release-classes.ps1 <archivo.aab | archivo.apk | carpeta | comodin> [-ForbidPackage <paquete>[,<paquete>]] | --self-test'
if (-not $Path) { Stop-WithCode 2 $usage }
$extra = @($ForbidPackage | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$rules = Get-Rules $extra
if ($rules -is [string]) { Stop-WithCode 2 "ERROR: '$rules' no es un nombre de paquete Java valido." }

$files = @()
if (Test-Path -LiteralPath $Path -PathType Container) {
    $files = @(Get-ChildItem -LiteralPath $Path -File | Where-Object { $_.Extension -in @('.aab', '.apk') })
}
elseif (Test-Path -LiteralPath $Path -PathType Leaf) {
    $files = @(Get-Item -LiteralPath $Path)
}
else {
    $files = @(Get-ChildItem -Path $Path -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -in @('.aab', '.apk') })
}
if ($files.Count -eq 0) { Stop-WithCode 2 "ERROR: no se encontro ningun .aab ni .apk en '$Path'." }
foreach ($f in $files) {
    if ($f.Extension -notin @('.aab', '.apk')) { Stop-WithCode 2 "ERROR: '$($f.FullName)' no es un .aab ni un .apk." }
}

$failed = $false
Write-Output "Paquetes prohibidos en release: $((@($rules | ForEach-Object Package)) -join ', ')"
foreach ($f in $files) {
    try { $scan = Invoke-ArtifactScan $f.FullName $rules }
    catch { Stop-WithCode 2 "ERROR: no se puede revisar '$($f.FullName)': $($_.Exception.Message)" }
    $found = @($rules | Where-Object { $scan.Hits[$_.Package].Count -gt 0 })
    $status = if ($found.Count -eq 0) { 'OK' } else { 'FALLA' }
    if ($found.Count -gt 0) { $failed = $true }
    Write-Output ("[{0}] {1}: {2} .dex, {3} clases" -f $status, $f.FullName, $scan.Dex, $scan.Classes)
    foreach ($r in $found) {
        $list = $scan.Hits[$r.Package]
        Write-Output ("  FALLO: {0} clases de {1} ({2}):" -f $list.Count, $r.Package, $r.Why)
        foreach ($cls in ($list | Sort-Object | Select-Object -First $MaxListed)) { Write-Output "    $cls" }
        if ($list.Count -gt $MaxListed) { Write-Output "    ... y $($list.Count - $MaxListed) mas" }
    }
}

if ($failed) { Stop-WithCode 1 'FALLA: algun artefacto lleva clases que no pueden ir en release (D-033, anadido del 2026-10-22, punto 9).' }
Write-Output 'OK: ningun artefacto lleva clases prohibidas en release.'
exit 0
