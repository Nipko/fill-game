#Requires -Version 7.2
<#
.SYNOPSIS
    Compuerta: el AAB o APK de Amazon Fill no lleva activados los servicios de datos de Unity (Q-B13-9, anadido de D-040
    del 2026-10-28).

.DESCRIPTION
    Data Safety no declara a Unity como receptor de datos (docs/store-listing/data-safety.md sec. 4). Eso vale mientras el
    build no lleve Unity Analytics, Cloud Diagnostics / Crash Reporting, Performance Reporting ni la telemetria del motor
    (config.uca.cloud.unity3d.com, cdp.cloud.unity3d.com, perf-events.cloud.unity3d.com...) ni Unity Gaming Services.
    El manifiesto lleva com.unity.modules.unityanalytics: el codigo del modulo entra en el player, pero sin configuracion
    no tiene a donde enviar. La configuracion de todos esos servicios (URL de eventos, de configuracion y de fallos, y si
    estan activos) es un unico objeto, UnityConnectSettings (clase 310), que Unity solo mete en globalgamemanagers cuando
    hace falta. Con los servicios apagados el AAB no lo lleva (run 40662873429, 2026-10-28). Esta compuerta falla si:

      1. globalgamemanagers declara la clase 310 (UnityConnectSettings) entre sus tipos;
      2. cualquier entrada (dex, .so, global-metadata.dat, assets/bin/Data, recursos) lleva un host de los servicios de
         datos de Unity: "cloud.unity3d.com" (Analytics, CDP, UCA, perf-events de Cloud Diagnostics y Performance
         Reporting) o "services.api.unity.com" (Unity Gaming Services), en ASCII o UTF-16LE.

    No falla por Unity Ads (gateway.unityads.unity3d.com, webview.unityads.unity3d.com, com/unity3d/services/ del SDK de
    Unity Ads): entra por la mediacion de AdMob (D-049, anadido del 2026-10-27) y se declara con los anuncios.

    Solo lee el artefacto; no necesita bundletool, java ni Unity. Funciona en Windows (PowerShell 7) y en el runner
    Ubuntu de GitHub Actions (pwsh). Mensajes en ASCII.

    Codigos de salida:
      0  ningun artefacto lleva los servicios de datos de Unity
      1  algun artefacto los lleva (o el autotest fallo); manda sobre el 2 si hay de los dos
      2  argumentos incorrectos, ningun .aab/.apk encontrado, un artefacto ilegible como ZIP, sin globalgamemanagers
         o con un globalgamemanagers de formato no soportado (falla cerrado: sin veredicto no hay OK)

.PARAMETER Path
    Archivo .aab o .apk, carpeta (busca *.aab y *.apk dentro, no recursivo) o comodin. "--self-test" equivale a -SelfTest.

.PARAMETER SelfTest
    Autotest con artefactos sinteticos (limpio con Unity Ads, clase 310, hosts en ASCII y UTF-16LE, host partido entre dos
    trozos de lectura, APK, sin globalgamemanagers, formato no soportado, archivo que no es ZIP, ruta inexistente y
    carpeta mixta).

.EXAMPLE
    pwsh -NoProfile -File ci/check-no-unity-telemetry.ps1 build/Android
.EXAMPLE
    pwsh -NoProfile -File ci/check-no-unity-telemetry.ps1 --self-test
#>
param(
    [string]$Path = '',
    [switch]$SelfTest
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$UnityConnectSettingsClassId = 310
$GgmNames = @('base/assets/bin/Data/globalgamemanagers', 'assets/bin/Data/globalgamemanagers')
# Host -> descripcion. Se buscan en ASCII y en UTF-16LE.
$HostMarks = [ordered]@{
    'cloud.unity3d.com'      = 'host de los servicios de datos de Unity (Analytics, CDP, UCA, Cloud Diagnostics, Performance Reporting)'
    'services.api.unity.com' = 'host de Unity Gaming Services'
}

function Stop-WithCode {
    param([int]$Code, [string]$Message)
    [Console]::Error.WriteLine($Message)
    exit $Code
}

function Read-AllBytes([System.IO.Compression.ZipArchiveEntry]$Entry) {
    $ms = [System.IO.MemoryStream]::new()
    $s = $Entry.Open()
    try { $s.CopyTo($ms) } finally { $s.Dispose() }
    return ,$ms.ToArray()
}

# IDs de clase de los tipos de un SerializedFile de Unity (formato 22 o superior, sin arbol de tipos, como en un player).
# Lanza si el formato no es ese: la compuerta falla cerrada en lugar de dar por bueno lo que no sabe leer.
function Get-ClassIds([byte[]]$Data) {
    function U32BE([int]$o) { return ([uint32]$Data[$o] -shl 24) -bor ([uint32]$Data[$o + 1] -shl 16) -bor ([uint32]$Data[$o + 2] -shl 8) -bor [uint32]$Data[$o + 3] }
    if ($Data.Length -lt 64) { throw 'globalgamemanagers demasiado corto' }
    $version = U32BE 8
    if ($version -lt 22 -or $version -gt 30) { throw "formato de SerializedFile $version no soportado (se espera 22-30)" }
    $bigEndian = $Data[16] -ne 0
    $p = 48   # cabecera: 16 bytes antiguos + endianness (4) + metadataSize u32 + fileSize u64 + dataOffset u64 + reservado u64
    function I32([int]$o) {
        $b = [byte[]]$Data[$o..($o + 3)]
        if ($bigEndian -eq [BitConverter]::IsLittleEndian) { [Array]::Reverse($b) }
        return [BitConverter]::ToInt32($b, 0)
    }
    $end = [Array]::IndexOf($Data, [byte]0, $p)
    if ($end -lt 0 -or $end - $p -gt 64) { throw 'version de Unity ilegible en globalgamemanagers' }
    $p = $end + 1 + 4   # version de Unity + plataforma
    $typeTree = $Data[$p]; $p += 1
    if ($typeTree -ne 0) { throw 'globalgamemanagers con arbol de tipos (no es un player de release); no soportado' }
    $count = I32 $p; $p += 4
    if ($count -lt 1 -or $count -gt 4096) { throw "numero de tipos imposible ($count)" }
    $ids = [System.Collections.Generic.List[int]]::new()
    for ($i = 0; $i -lt $count; $i++) {
        if ($p + 39 -gt $Data.Length) { throw 'tabla de tipos cortada' }
        $cid = I32 $p
        $p += 4 + 1 + 2           # classID, isStripped, scriptTypeIndex
        if ($cid -eq 114) { $p += 16 }   # MonoBehaviour: hash del script
        $p += 16                  # hash del tipo
        $ids.Add($cid)
    }
    return ,$ids
}

# Hallazgos de un artefacto (lista vacia = limpio). Lanza si no es un ZIP legible o no se puede dar veredicto.
function Get-Findings([string]$File) {
    $findings = [System.Collections.Generic.List[string]]::new()
    $latin1 = [System.Text.Encoding]::Latin1
    $needles = [System.Collections.Generic.List[object]]::new()
    foreach ($mark in $HostMarks.Keys) {
        $needles.Add(@{ Text = $mark; What = $HostMarks[$mark] })
        $needles.Add(@{ Text = $latin1.GetString([System.Text.Encoding]::Unicode.GetBytes($mark)); What = $HostMarks[$mark] + ' (UTF-16)' })
    }

    $overlap = 0
    foreach ($n in $needles) { $overlap = [Math]::Max($overlap, $n.Text.Length) }
    $overlap -= 1
    $buffer = [byte[]]::new(4MB)
    $ggmSeen = $false
    $zip = [System.IO.Compression.ZipFile]::OpenRead($File)
    try {
        foreach ($entry in $zip.Entries) {
            if ($entry.FullName.EndsWith('/')) { continue }
            if ($GgmNames -contains $entry.FullName) {
                $ggmSeen = $true
                $ids = Get-ClassIds (Read-AllBytes $entry)
                if ($ids.Contains($UnityConnectSettingsClassId)) {
                    $findings.Add("UnityConnectSettings (clase 310: configuracion de Analytics, Cloud Diagnostics y Performance Reporting) en $($entry.FullName)")
                }
            }

            # Por trozos de 4 MB con solape; Latin1 conserva cada byte como un caracter (busqueda ordinal de bytes).
            $found = [bool[]]::new($needles.Count)
            $carry = ''
            $stream = $entry.Open()
            try {
                while (($read = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
                    $text = $carry + $latin1.GetString($buffer, 0, $read)
                    for ($i = 0; $i -lt $needles.Count; $i++) {
                        if (-not $found[$i] -and $text.IndexOf($needles[$i].Text, [StringComparison]::OrdinalIgnoreCase) -ge 0) { $found[$i] = $true }
                    }

                    $keep = [Math]::Min($text.Length, $overlap)
                    $carry = $text.Substring($text.Length - $keep)
                }
            }
            finally {
                $stream.Dispose()
            }

            for ($i = 0; $i -lt $needles.Count; $i++) {
                if ($found[$i]) { $findings.Add("$($needles[$i].What) en $($entry.FullName)") }
            }
        }
    }
    finally {
        $zip.Dispose()
    }

    if (-not $ggmSeen) { throw 'no lleva assets/bin/Data/globalgamemanagers (no parece un player de Unity)' }
    return ,$findings
}

# 0 limpio, 1 con hallazgos, 2 sin veredicto.
function Test-Artifact([string]$File) {
    try {
        $findings = Get-Findings $File
    }
    catch {
        Write-Host "ERROR ${File}: sin veredicto ($($_.Exception.Message))"
        return 2
    }

    if ($findings.Count -eq 0) {
        Write-Host "OK    ${File}: sin servicios de datos de Unity (sin UnityConnectSettings ni hosts de Unity)"
        return 0
    }

    $shown = 0
    foreach ($f in $findings) {
        if ($shown -ge 40) { Write-Host "FALLO ${File}: ... y $($findings.Count - $shown) hallazgos mas"; break }
        Write-Host "FALLO ${File}: $f"
        $shown++
    }

    return 1
}

function Resolve-Artifacts([string]$Target) {
    if (Test-Path -LiteralPath $Target -PathType Leaf) { return @((Resolve-Path -LiteralPath $Target).Path) }
    if (Test-Path -LiteralPath $Target -PathType Container) {
        return @(Get-ChildItem -LiteralPath $Target -File | Where-Object { $_.Extension -in @('.aab', '.apk') } | ForEach-Object FullName)
    }

    return @(Get-ChildItem -Path $Target -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -in @('.aab', '.apk') } | ForEach-Object FullName)
}

function Invoke-Gate([string]$Target) {
    $files = @(Resolve-Artifacts $Target)
    if ($files.Count -eq 0) {
        Write-Host "ERROR: ningun .aab ni .apk en '$Target'"
        return 2
    }

    $worst = 0
    foreach ($file in $files) {
        $code = Test-Artifact $file
        if ($code -eq 1) { $worst = 1 }
        elseif ($code -eq 2 -and $worst -eq 0) { $worst = 2 }
    }

    return $worst
}

# SerializedFile sintetico (formato 22, little endian, sin arbol de tipos) con esos IDs de clase.
function New-Ggm([int[]]$ClassIds, [int]$Version = 22, [byte]$TypeTree = 0) {
    $ms = [System.IO.MemoryStream]::new()
    $w = [System.IO.BinaryWriter]::new($ms)
    $w.Write([byte[]]::new(8))
    $vb = [BitConverter]::GetBytes([uint32]$Version); [Array]::Reverse($vb); $w.Write($vb)
    $w.Write([byte[]]::new(4))
    $w.Write([byte[]](0, 0, 0, 0))      # little endian + reservado
    $w.Write([byte[]]::new(28))         # metadataSize, fileSize, dataOffset, reservado
    $w.Write([System.Text.Encoding]::ASCII.GetBytes("6000.3.25f1`0"))
    $w.Write([int32]13)                 # Android
    $w.Write([byte]$TypeTree)
    $w.Write([int32]$ClassIds.Count)
    foreach ($id in $ClassIds) {
        $w.Write([int32]$id); $w.Write([byte]0); $w.Write([int16]-1)
        if ($id -eq 114) { $w.Write([byte[]]::new(16)) }
        $w.Write([byte[]]::new(16))
    }
    $w.Write([byte[]]::new(64))
    $w.Flush()
    return ,$ms.ToArray()
}

function New-TestZip([string]$File, [hashtable]$Entries) {
    if (Test-Path -LiteralPath $File) { Remove-Item -LiteralPath $File -Force }
    $zip = [System.IO.Compression.ZipFile]::Open($File, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($name in $Entries.Keys) {
            $entry = $zip.CreateEntry($name)
            $s = $entry.Open()
            try {
                $bytes = [byte[]]$Entries[$name]
                $s.Write($bytes, 0, $bytes.Length)
            }
            finally { $s.Dispose() }
        }
    }
    finally { $zip.Dispose() }
}

function Invoke-SelfTest {
    $dir = Join-Path ([System.IO.Path]::GetTempPath()) ("af-check-no-unity-telemetry-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $dir | Out-Null
    $ascii = [System.Text.Encoding]::ASCII
    $utf16 = [System.Text.Encoding]::Unicode
    $noise = [byte[]](0, 1, 2, 3, 250, 251, 252)
    # Los IDs del globalgamemanagers del AAB del run 40662873429 (sin 310).
    $realIds = @(129, 13, 78, 11, 94, 116, 30, 5, 98, 55, 141, 47, 147, 126, 19, 300, 1403656975, 937362698, 655991488)
    $cleanGgm = New-Ggm $realIds
    $ggmAab = 'base/assets/bin/Data/globalgamemanagers'
    $hostBytes = $ascii.GetBytes('https://config.uca.cloud.unity3d.com')
    $straddle = [byte[]]::new(4MB + 64)
    [Array]::Copy($hostBytes, 0, $straddle, 4MB - 9, $hostBytes.Length)
    $cases = @(
        # Lo que si lleva el juego: Unity Ads por la mediacion de AdMob y los modulos de Unity sin configuracion.
        @{ Name = 'limpio.aab'; Expect = 0; Entries = @{
                $ggmAab = $cleanGgm
                'base/dex/classes3.dex' = $noise + $ascii.GetBytes('https://gateway.unityads.unity3d.com/v1 webview.unityads.unity3d.com Lcom/unity3d/services/UnityAdsSDK;') + $noise
                'base/lib/arm64-v8a/libunity.so' = $noise + $ascii.GetBytes('UnityConnectSettings submitAnalytics AnalyticsCoreStatsUpdate unity3d.com') + $noise
                'base/assets/bin/Data/Managed/Metadata/global-metadata.dat' = $ascii.GetBytes('UnityAnalyticsModule PerformanceReportingModule https://docs.unity3d.com/Manual/') } }
        @{ Name = 'clase310.aab'; Expect = 1; Entries = @{ $ggmAab = (New-Ggm ($realIds + 310)) } }
        @{ Name = 'clase310-con-monobehaviour.aab'; Expect = 1; Entries = @{ $ggmAab = (New-Ggm @(129, 114, 310)) } }
        @{ Name = 'host-cdp.aab'; Expect = 1; Entries = @{
                $ggmAab = $cleanGgm
                'base/lib/arm64-v8a/libunity.so' = $noise + $ascii.GetBytes('https://cdp.cloud.unity3d.com/v1/events') + $noise } }
        @{ Name = 'host-utf16.aab'; Expect = 1; Entries = @{
                $ggmAab = $cleanGgm
                'base/assets/bin/Data/Managed/Metadata/global-metadata.dat' = $noise + $utf16.GetBytes('https://perf-events.cloud.unity3d.com') + $noise } }
        @{ Name = 'ugs.aab'; Expect = 1; Entries = @{
                $ggmAab = $cleanGgm
                'base/assets/bin/Data/Managed/Metadata/global-metadata.dat' = $ascii.GetBytes('https://player-auth.services.api.unity.com') } }
        @{ Name = 'frontera.aab'; Expect = 1; Entries = @{ $ggmAab = $cleanGgm; 'base/dex/classes.dex' = $straddle } }
        @{ Name = 'limpio.apk'; Expect = 0; Entries = @{ 'assets/bin/Data/globalgamemanagers' = $cleanGgm } }
        @{ Name = 'clase310.apk'; Expect = 1; Entries = @{ 'assets/bin/Data/globalgamemanagers' = (New-Ggm @(129, 310)) } }
        @{ Name = 'sin-ggm.aab'; Expect = 2; Entries = @{ 'base/dex/classes.dex' = $noise } }
        @{ Name = 'formato-21.aab'; Expect = 2; Entries = @{ $ggmAab = (New-Ggm $realIds 21) } }
        @{ Name = 'con-arbol.aab'; Expect = 2; Entries = @{ $ggmAab = (New-Ggm $realIds 22 1) } }
    )

    $failures = 0
    try {
        foreach ($c in $cases) {
            $file = Join-Path $dir $c.Name
            New-TestZip $file $c.Entries
            $got = Test-Artifact $file
            if ($got -ne $c.Expect) {
                Write-Host "AUTOTEST FALLO $($c.Name): esperado $($c.Expect), obtenido $got"
                $failures++
            }
        }

        $notZip = Join-Path $dir 'roto.aab'
        [System.IO.File]::WriteAllBytes($notZip, $ascii.GetBytes('no es un zip'))
        if ((Test-Artifact $notZip) -ne 2) { Write-Host 'AUTOTEST FALLO roto.aab: esperado 2'; $failures++ }
        if ((Invoke-Gate (Join-Path $dir 'no-existe.aab')) -ne 2) { Write-Host 'AUTOTEST FALLO ruta inexistente: esperado 2'; $failures++ }

        $mixed = Join-Path $dir 'mixta'
        New-Item -ItemType Directory -Path $mixed | Out-Null
        New-TestZip (Join-Path $mixed 'a.aab') @{ $ggmAab = $cleanGgm }
        New-TestZip (Join-Path $mixed 'b.apk') @{ 'assets/bin/Data/globalgamemanagers' = (New-Ggm @(310)) }
        [System.IO.File]::WriteAllBytes((Join-Path $mixed 'c.aab'), $ascii.GetBytes('roto'))
        if ((Invoke-Gate $mixed) -ne 1) { Write-Host 'AUTOTEST FALLO carpeta mixta (limpio, con 310 y roto): esperado 1'; $failures++ }
    }
    finally {
        Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
    }

    $total = $cases.Count + 3
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
if (-not $Path) { Stop-WithCode 2 'Uso: check-no-unity-telemetry.ps1 <aab|apk|carpeta|comodin> | --self-test' }
exit (Invoke-Gate $Path)
