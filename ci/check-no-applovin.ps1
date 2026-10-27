#Requires -Version 7.2
<#
.SYNOPSIS
    Compuerta: el AAB o APK de Amazon Fill no lleva AppLovin MAX (D-049, anadido del 2026-10-27).

.DESCRIPTION
    Desde el 2026-10-27 el SDK de AppLovin MAX y su plugin de Unity salen del proyecto: AppLovin no acepta cuentas nuevas,
    los anuncios van con AdMob directo (Google Mobile Ads, D-049) y MAX iba en el build sin servir, con su detector de fallos
    nativo y okhttp3 renombrado (docs/legal/avisos-de-licencia.md). Si AppLovin abre cuentas, entra como red de mediacion
    dentro de AdMob, con su adaptador aprobado por el dueno; MAX no vuelve. Esta compuerta abre el artefacto (es un ZIP) y
    falla si encuentra:

      1. una entrada cuyo nombre lleve "applovin" (sin distinguir mayusculas): la ruta com/applovin/, la biblioteca nativa
         lib/arm64-v8a/libapplovin-native-crash-reporter.so, res/raw/applovin_settings.json, META-INF/*applovin*...;
      2. en cualquier entrada (dex, .so, global-metadata.dat, recursos...), el descriptor de tipo "Lcom/applovin/" de un dex
         (el SDK y su okhttp3 renombrado, com/applovin/shadow/okhttp3/);
      3. el nombre del paquete Java "com.applovin." en ASCII/UTF-8 o en UTF-16LE (manifiesto binario, recursos, proveedores);
      4. el plugin de C# de MAX compilado por IL2CPP: el tipo "MaxSdkBase" o el espacio de nombres "AppLovinMax." en
         global-metadata.dat (ASCII o UTF-16LE).

    Solo lee el artefacto; no necesita bundletool, java ni aapt2. Funciona en Windows (PowerShell 7) y en el runner Ubuntu
    de GitHub Actions (pwsh). Mensajes en ASCII.

    Codigos de salida:
      0  ningun artefacto lleva AppLovin
      1  algun artefacto lo lleva (o el autotest fallo); manda sobre el 2 si hay de los dos
      2  argumentos incorrectos, ningun .aab/.apk encontrado o un artefacto ilegible como ZIP

.PARAMETER Path
    Archivo .aab o .apk, carpeta (busca *.aab y *.apk dentro, no recursivo) o comodin. "--self-test" equivale a -SelfTest.

.PARAMETER SelfTest
    Autotest con artefactos sinteticos (limpio, entrada com/applovin/, .so de AppLovin, clase en un dex, okhttp3 renombrado,
    paquete en UTF-16LE, tipo de C# en global-metadata.dat, marca partida entre dos trozos de lectura, archivo que no es ZIP,
    ruta inexistente y carpeta mixta).

.EXAMPLE
    pwsh -NoProfile -File ci/check-no-applovin.ps1 build/Android
.EXAMPLE
    pwsh -NoProfile -File ci/check-no-applovin.ps1 Builds/Android/rehearsal.aab
.EXAMPLE
    pwsh -NoProfile -File ci/check-no-applovin.ps1 --self-test
#>
param(
    [string]$Path = '',
    [switch]$SelfTest
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$EntryNameMark = 'applovin'
# Marca de contenido -> descripcion. Se buscan en ASCII y, las que no son descriptores de dex, tambien en UTF-16LE.
$ContentMarks = [ordered]@{
    'Lcom/applovin/' = 'clases com.applovin (SDK de MAX u okhttp3 renombrado) en un dex'
    'com.applovin.'  = 'nombre del paquete com.applovin'
    'MaxSdkBase'     = 'plugin de C# de MAX (MaxSdkBase)'
    'AppLovinMax.'   = 'plugin de C# de MAX (espacio de nombres AppLovinMax)'
}
$DexOnly = @('Lcom/applovin/')
# Excepcion estrecha (D-049, anadido del 2026-10-27: Unity Ads 4.20.1 como red de mediacion de AdMob): el SDK de Unity Ads
# trae un puente por reflexion (com.unity3d.services.core.reflection.AppLovinCommunicatorBridge) que solo nombra, como
# texto, las clases com.applovin.communicator.AppLovinCommunicator* por si MAX esta en la app. No es codigo de AppLovin:
# esos nombres se quitan antes de buscar. Una clase real de AppLovin (Lcom/applovin/...) u otro com.applovin. sigue fallando.
$ExemptStrings = @('com.applovin.communicator.AppLovinCommunicator')

function Stop-WithCode {
    param([int]$Code, [string]$Message)
    [Console]::Error.WriteLine($Message)
    exit $Code
}

# Hallazgos de un artefacto (lista vacia = limpio). Lanza si no es un ZIP legible.
function Get-Findings([string]$File) {
    $findings = [System.Collections.Generic.List[string]]::new()
    $latin1 = [System.Text.Encoding]::Latin1
    $needles = [System.Collections.Generic.List[object]]::new()
    foreach ($mark in $ContentMarks.Keys) {
        $needles.Add(@{ Text = $mark; What = $ContentMarks[$mark] })
        if ($DexOnly -notcontains $mark) {
            $needles.Add(@{ Text = $latin1.GetString([System.Text.Encoding]::Unicode.GetBytes($mark)); What = $ContentMarks[$mark] + ' (UTF-16)' })
        }
    }

    $overlap = 0
    foreach ($n in $needles) { $overlap = [Math]::Max($overlap, $n.Text.Length) }
    foreach ($e in $ExemptStrings) { $overlap = [Math]::Max($overlap, $e.Length) }   # la excepcion entera cabe en el solape
    $overlap -= 1
    $buffer = [byte[]]::new(4MB)   # uno para todo el artefacto
    $zip = [System.IO.Compression.ZipFile]::OpenRead($File)
    try {
        foreach ($entry in $zip.Entries) {
            if ($entry.FullName.EndsWith('/')) { continue }
            if ($entry.FullName.IndexOf($EntryNameMark, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $findings.Add("entrada de AppLovin: $($entry.FullName)")
            }

            # Por trozos de 4 MB con solape (memoria acotada aunque libil2cpp.so o global-metadata.dat pesen cientos de MB).
            # Latin1 conserva cada byte como un caracter: IndexOf ordinal busca la secuencia de bytes exacta.
            $found = [bool[]]::new($needles.Count)
            $carry = ''
            $stream = $entry.Open()
            try {
                while (($read = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
                    $text = $carry + $latin1.GetString($buffer, 0, $read)
                    $search = $text
                    foreach ($e in $ExemptStrings) { $search = $search.Replace($e, '') }
                    for ($i = 0; $i -lt $needles.Count; $i++) {
                        if (-not $found[$i] -and $search.IndexOf($needles[$i].Text, [StringComparison]::Ordinal) -ge 0) { $found[$i] = $true }
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
        Write-Host "OK    ${File}: sin AppLovin"
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
        # Encontrar AppLovin (1) manda sobre un artefacto ilegible (2): es lo que la compuerta vigila.
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
    $dir = Join-Path ([System.IO.Path]::GetTempPath()) ("af-check-no-applovin-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $dir | Out-Null
    $ascii = [System.Text.Encoding]::ASCII
    $utf16 = [System.Text.Encoding]::Unicode
    $noise = [byte[]](0, 1, 2, 3, 250, 251, 252)
    # El descriptor partido entre dos trozos de lectura de 4 MB.
    $markBytes = $ascii.GetBytes('Lcom/applovin/sdk/AppLovinSdk;')
    $straddle = [byte[]]::new(4MB + 64)
    [Array]::Copy($markBytes, 0, $straddle, 4MB - 7, $markBytes.Length)
    $cases = @(
        # Lo que si lleva el juego: Google Mobile Ads y su adaptador de mediacion con "applovin" en minusculas fuera de com.applovin.
        @{ Name = 'limpio.aab'; Expect = 0; Entries = @{
                'base/dex/classes.dex' = $noise + $ascii.GetBytes('Lcom/google/android/gms/ads/MobileAds;Lokhttp3/OkHttpClient;Lcom/amazonfill/billing/AfBilling;') + $noise
                'base/lib/arm64-v8a/libil2cpp.so' = $noise + $ascii.GetBytes('MaxSdk MaxAdContentRating AppLovin com_applovin') + $noise
                'base/assets/bin/Data/Managed/Metadata/global-metadata.dat' = $utf16.GetBytes('AF.Ads.Google GoogleMobileAds.Api MaxAdContentRating') } }
        @{ Name = 'ruta.aab'; Expect = 1; Entries = @{
                'base/root/com/applovin/sdk/x.properties' = $ascii.GetBytes('x=1') } }
        @{ Name = 'so.apk'; Expect = 1; Entries = @{
                'lib/arm64-v8a/libapplovin-native-crash-reporter.so' = $noise } }
        @{ Name = 'clase-dex.apk'; Expect = 1; Entries = @{
                'classes2.dex' = $noise + $ascii.GetBytes('Lcom/applovin/mediation/MaxAd;') + $noise } }
        @{ Name = 'okhttp.aab'; Expect = 1; Entries = @{
                'base/dex/classes3.dex' = $noise + $ascii.GetBytes('Lcom/applovin/shadow/okhttp3/OkHttpClient;') + $noise } }
        @{ Name = 'paquete-utf16.apk'; Expect = 1; Entries = @{
                'AndroidManifest.xml' = $noise + $utf16.GetBytes('com.applovin.sdk.AppLovinInitProvider') + $noise } }
        @{ Name = 'metadata.aab'; Expect = 1; Entries = @{
                'base/assets/bin/Data/Managed/Metadata/global-metadata.dat' = $noise + $ascii.GetBytes('MaxSdkBase') + $noise } }
        # Unity Ads 4.20.1 (D-049, anadido): su puente por reflexion nombra com.applovin.communicator.* como texto; no es AppLovin.
        @{ Name = 'unity-ads-puente.aab'; Expect = 0; Entries = @{
                'base/dex/classes3.dex' = $noise + $ascii.GetBytes('Lcom/unity3d/services/core/reflection/AppLovinCommunicatorBridge;.com.applovin.communicator.AppLovinCommunicator5com.applovin.communicator.AppLovinCommunicatorMessage8com.applovin.communicator.AppLovinCommunicatorSubscriber') + $noise } }
        @{ Name = 'unity-ads-puente-y-max.aab'; Expect = 1; Entries = @{
                'base/dex/classes3.dex' = $noise + $ascii.GetBytes('.com.applovin.communicator.AppLovinCommunicator com.applovin.sdk.AppLovinInitProvider') + $noise } }
        @{ Name = 'frontera.aab'; Expect = 1; Entries = @{
                'base/dex/classes.dex' = $straddle } }
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

        # Carpeta con uno limpio, uno con AppLovin y uno roto: falla con 1.
        $mixed = Join-Path $dir 'mixta'
        New-Item -ItemType Directory -Path $mixed | Out-Null
        New-TestZip (Join-Path $mixed 'a.aab') @{ 'base/dex/classes.dex' = $ascii.GetBytes('Lcom/google/android/gms/ads/MobileAds;') }
        New-TestZip (Join-Path $mixed 'b.apk') @{ 'classes.dex' = $ascii.GetBytes('Lcom/applovin/sdk/AppLovinSdk;') }
        [System.IO.File]::WriteAllBytes((Join-Path $mixed 'c.aab'), $ascii.GetBytes('roto'))
        if ((Invoke-Gate $mixed) -ne 1) { Write-Host 'AUTOTEST FALLO carpeta mixta (limpio, con AppLovin y roto): esperado 1'; $failures++ }
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
if (-not $Path) { Stop-WithCode 2 'Uso: check-no-applovin.ps1 <aab|apk|carpeta|comodin> | --self-test' }
exit (Invoke-Gate $Path)
