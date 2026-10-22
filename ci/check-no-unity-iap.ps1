#Requires -Version 7.2
<#
.SYNOPSIS
    Compuerta: el AAB o APK de Amazon Fill no lleva Unity IAP ni su telemetria de compras (D-040, anadido de Q-IAP-1).

.DESCRIPTION
    Desde el 2026-10-22 las compras van por el plugin propio de Play Billing (AfBilling.androidlib sobre
    com.android.billingclient:billing 9.0.0) y Unity IAP (com.unity.purchasing) sale del proyecto, porque enviaba cada compra
    a Unity sin consentimiento ni borrado (D-040, anadido de Q-IAP-1; docs/dev/iap.md). Esta compuerta abre el artefacto
    (es un ZIP) y falla si encuentra, en cualquier entrada (dex, .so, global-metadata.dat, recursos...):

      1. el host de la telemetria de compras de Unity, prd.insights.analytics.unity3d.com (en ASCII/UTF-8 o en UTF-16LE);
      2. clases Java de Unity IAP: el descriptor de tipo "Lcom/unity/purchasing/" de un dex, o una entrada cuyo nombre
         lleve la ruta com/unity/purchasing/.

    Solo lee el artefacto; no necesita bundletool, java ni aapt2. Funciona en Windows (PowerShell 7) y en el runner Ubuntu
    de GitHub Actions (pwsh). Mensajes en ASCII.

    Codigos de salida:
      0  ningun artefacto lleva Unity IAP ni su host
      1  algun artefacto lo lleva (o el autotest fallo); manda sobre el 2 si hay de los dos
      2  argumentos incorrectos, ningun .aab/.apk encontrado o un artefacto ilegible como ZIP

.PARAMETER Path
    Archivo .aab o .apk, carpeta (busca *.aab y *.apk dentro, no recursivo) o comodin. "--self-test" equivale a -SelfTest.

.PARAMETER SelfTest
    Autotest con artefactos sinteticos (limpio, host en ASCII, host en UTF-16LE, clase en un dex, host partido entre dos
    trozos de lectura, entrada con la ruta del paquete, archivo que no es ZIP, ruta inexistente y carpeta mixta).

.EXAMPLE
    pwsh -NoProfile -File ci/check-no-unity-iap.ps1 build/Android
.EXAMPLE
    pwsh -NoProfile -File ci/check-no-unity-iap.ps1 Builds/Android/dev.apk
.EXAMPLE
    pwsh -NoProfile -File ci/check-no-unity-iap.ps1 --self-test
#>
param(
    [string]$Path = '',
    [switch]$SelfTest
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$TelemetryHost = 'prd.insights.analytics.unity3d.com'
$DexDescriptor = 'Lcom/unity/purchasing/'
$EntryPathMark = 'com/unity/purchasing/'

function Stop-WithCode {
    param([int]$Code, [string]$Message)
    [Console]::Error.WriteLine($Message)
    exit $Code
}

# Hallazgos de un artefacto (lista vacia = limpio). Lanza si no es un ZIP legible.
function Get-Findings([string]$File) {
    $findings = [System.Collections.Generic.List[string]]::new()
    $latin1 = [System.Text.Encoding]::Latin1
    $hostUtf16 = $latin1.GetString([System.Text.Encoding]::Unicode.GetBytes($TelemetryHost))
    $overlap = [Math]::Max([Math]::Max($TelemetryHost.Length, $hostUtf16.Length), $DexDescriptor.Length) - 1
    $buffer = [byte[]]::new(4MB)   # uno para todo el artefacto
    $zip = [System.IO.Compression.ZipFile]::OpenRead($File)
    try {
        foreach ($entry in $zip.Entries) {
            if ($entry.FullName.EndsWith('/')) { continue }
            if ($entry.FullName.Replace('\', '/').Contains($EntryPathMark)) {
                $findings.Add("entrada con la ruta de Unity IAP: $($entry.FullName)")
            }

            # Por trozos de 4 MB con solape (memoria acotada aunque libil2cpp.so o global-metadata.dat pesen cientos de MB).
            # Latin1 conserva cada byte como un caracter: IndexOf ordinal busca la secuencia de bytes exacta.
            $foundHost = $false; $foundHost16 = $false; $foundDex = $false
            $carry = ''
            $stream = $entry.Open()
            try {
                while (($read = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
                    $text = $carry + $latin1.GetString($buffer, 0, $read)
                    if (-not $foundHost -and $text.IndexOf($TelemetryHost, [StringComparison]::Ordinal) -ge 0) { $foundHost = $true }
                    if (-not $foundHost16 -and $text.IndexOf($hostUtf16, [StringComparison]::Ordinal) -ge 0) { $foundHost16 = $true }
                    if (-not $foundDex -and $text.IndexOf($DexDescriptor, [StringComparison]::Ordinal) -ge 0) { $foundDex = $true }
                    $keep = [Math]::Min($text.Length, $overlap)
                    $carry = $text.Substring($text.Length - $keep)
                }
            }
            finally {
                $stream.Dispose()
            }

            if ($foundHost) { $findings.Add("host de la telemetria de Unity IAP ($TelemetryHost) en $($entry.FullName)") }
            if ($foundHost16) { $findings.Add("host de la telemetria de Unity IAP en UTF-16 en $($entry.FullName)") }
            if ($foundDex) { $findings.Add("clases com.unity.purchasing en $($entry.FullName)") }
        }
    }
    finally {
        $zip.Dispose()
    }

    return ,$findings
}

# 0 limpio, 1 con hallazgos, 2 ilegible.
function Test-Artifact([string]$File) {
    try {
        $findings = Get-Findings $File
    }
    catch {
        Write-Host "ERROR ${File}: no se puede leer como ZIP ($($_.Exception.Message))"
        return 2
    }

    if ($findings.Count -eq 0) {
        Write-Host "OK    ${File}: sin Unity IAP ni $TelemetryHost"
        return 0
    }

    foreach ($f in $findings) { Write-Host "FALLO ${File}: $f" }
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
        # Encontrar Unity IAP (1) manda sobre un artefacto ilegible (2): es lo que la compuerta vigila.
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
                $bytes = [byte[]]$Entries[$name]
                $s.Write($bytes, 0, $bytes.Length)
            }
            finally { $s.Dispose() }
        }
    }
    finally { $zip.Dispose() }
}

function Invoke-SelfTest {
    $dir = Join-Path ([System.IO.Path]::GetTempPath()) ("af-check-no-unity-iap-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $dir | Out-Null
    $ascii = [System.Text.Encoding]::ASCII
    $noise = [byte[]](0, 1, 2, 3, 250, 251, 252)
    # El host partido entre dos trozos de lectura de 4 MB.
    $hostBytes = $ascii.GetBytes('prd.insights.analytics.unity3d.com')
    $straddle = [byte[]]::new(4MB + 64)
    [Array]::Copy($hostBytes, 0, $straddle, 4MB - 10, $hostBytes.Length)
    $cases = @(
        @{ Name = 'limpio.aab'; Expect = 0; Entries = @{
                'base/dex/classes.dex' = $noise + $ascii.GetBytes('Lcom/amazonfill/billing/AfBilling;Lcom/android/billingclient/api/BillingClient;') + $noise
                'base/lib/arm64-v8a/libil2cpp.so' = $noise + $ascii.GetBytes('analytics.unity3d.com.example prd.insights') + $noise
                'base/assets/bin/Data/Managed/Metadata/global-metadata.dat' = [System.Text.Encoding]::Unicode.GetBytes('AF.IAP.PlayBilling') } }
        @{ Name = 'host-ascii.aab'; Expect = 1; Entries = @{
                'base/lib/arm64-v8a/libil2cpp.so' = $noise + $ascii.GetBytes('https://prd.insights.analytics.unity3d.com/api/') + $noise } }
        @{ Name = 'host-utf16.apk'; Expect = 1; Entries = @{
                'assets/bin/Data/Managed/Metadata/global-metadata.dat' = $noise + [System.Text.Encoding]::Unicode.GetBytes('prd.insights.analytics.unity3d.com') + $noise } }
        @{ Name = 'clase-dex.apk'; Expect = 1; Entries = @{
                'classes2.dex' = $noise + $ascii.GetBytes('Lcom/unity/purchasing/googleplay/GooglePlayBilling;') + $noise } }
        @{ Name = 'frontera.aab'; Expect = 1; Entries = @{
                'base/assets/bin/Data/Managed/Metadata/global-metadata.dat' = $straddle } }
        @{ Name = 'ruta.aab'; Expect = 1; Entries = @{
                'base/root/com/unity/purchasing/x.properties' = $ascii.GetBytes('x=1') } }
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

        # Carpeta con uno limpio y uno sucio: falla con 1.
        $mixed = Join-Path $dir 'mixta'
        New-Item -ItemType Directory -Path $mixed | Out-Null
        New-TestZip (Join-Path $mixed 'a.aab') @{ 'base/dex/classes.dex' = $ascii.GetBytes('Lcom/amazonfill/billing/AfBilling;') }
        New-TestZip (Join-Path $mixed 'b.apk') @{ 'classes.dex' = $ascii.GetBytes('Lcom/unity/purchasing/X;') }
        [System.IO.File]::WriteAllBytes((Join-Path $mixed 'c.aab'), $ascii.GetBytes('roto'))
        if ((Invoke-Gate $mixed) -ne 1) { Write-Host 'AUTOTEST FALLO carpeta mixta (limpio, con Unity IAP y roto): esperado 1'; $failures++ }
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
if (-not $Path) { Stop-WithCode 2 'Uso: check-no-unity-iap.ps1 <aab|apk|carpeta|comodin> | --self-test' }
exit (Invoke-Gate $Path)
