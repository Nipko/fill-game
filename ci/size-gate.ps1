#Requires -Version 7.2
<#
.SYNOPSIS
    Compuerta de tamano: falla si un AAB de Amazon Fill supera el presupuesto (80 MB, GDD 08 sec. 6.1).

.DESCRIPTION
    Comprueba cada .aab indicado (archivo, carpeta o comodin) en dos niveles:

      1. Tamano del archivo .aab (siempre). Es una cota superior: el AAB lleva todas las ABI y
         todos los asset packs install-time.
      2. Descarga maxima real segun Google Play: "bundletool get-size total" sobre el conjunto de
         APK que genera "bundletool build-apks". Es la metrica de la compuerta del GDD (08 sec. 6.1
         y 12.1). Se ejecuta si hay bundletool (parametro -Bundletool o variable BUNDLETOOL_JAR) y java.
         En CI se exige con -RequireBundletool.

    Con -Breakdown, ademas, el desglose del .aab por grupo (codigo nativo, datos de Unity, dex,
    recursos de Android, metadatos y cada asset pack), en MB comprimidos dentro del AAB y sin
    comprimir. El peso por grupo de los datos de Unity (niveles, fuentes, arte, audio...) lo da el
    Build Report: Logs/aab-size.md de AF.EditorTools.AudioAssetSetup.BuildGameAab (tarea B2).

    Que AAB mide la compuerta (B2, 2026-10-18): el de la escena del juego, que lleva el audio, los
    packs de niveles y las fuentes (AF.EditorTools.AudioAssetSetup.BuildGameAab, Builds/Android/game.aab,
    o el AAB de release del CI con las mismas escenas de Build Settings). El AAB de humo
    (AF.EditorTools.BuildSmoke, smoke.aab) usa una escena minima sin audio ni niveles: sirve para
    la compuerta de 16 KB, no para la de tamano.

    Unidades: 1 MB = 1.000.000 bytes (como Play Console), que es lo mas estricto.
    Funciona en Windows (PowerShell 7) y en el runner Ubuntu de GitHub Actions (pwsh).

    Codigos de salida:
      0  todos los AAB dentro del presupuesto
      1  algun AAB supera el presupuesto
      2  argumentos incorrectos o no se encontro ningun .aab
      3  falta una herramienta exigida (bundletool o java con -RequireBundletool) o bundletool fallo

.PARAMETER Path
    Archivo .aab, carpeta (busca *.aab dentro, recursivo) o comodin (p. ej. Builds/Android/*.aab).

.PARAMETER MaxMB
    Presupuesto en MB decimales (80 por defecto).

.PARAMETER Bundletool
    Ruta a bundletool-all-<version>.jar (por defecto, la variable BUNDLETOOL_JAR).

.PARAMETER RequireBundletool
    Falla con 3 si no se puede medir con bundletool (uso en CI).

.PARAMETER Breakdown
    Escribe tambien el desglose de cada .aab por grupo (solo informa; no cambia el veredicto).
    Un .aab que no se puede leer como ZIP sale con 2.

.EXAMPLE
    pwsh -NoProfile -File ci/size-gate.ps1 Builds/Android/smoke.aab
.EXAMPLE
    pwsh -NoProfile -File ci/size-gate.ps1 build/Android -MaxMB 80 -RequireBundletool
.EXAMPLE
    pwsh -NoProfile -File ci/size-gate.ps1 Builds/Android/game.aab -Breakdown
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Path = '',
    [string]$MaxMB = '80',
    [string]$Bundletool = $env:BUNDLETOOL_JAR,
    [switch]$RequireBundletool,
    [switch]$Breakdown
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

function Stop-WithCode {
    param([int]$Code, [string]$Message)
    [Console]::Error.WriteLine($Message)
    exit $Code
}

$limitMB = 0.0
if (-not [double]::TryParse($MaxMB, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$limitMB) -or $limitMB -le 0) {
    Stop-WithCode 2 "ERROR: -MaxMB debe ser un numero positivo (recibido: '$MaxMB')."
}
$limitBytes = [long][Math]::Floor($limitMB * 1000000)

if (-not $Path) {
    Stop-WithCode 2 'Uso: pwsh -NoProfile -File ci/size-gate.ps1 <archivo.aab | carpeta | comodin> [-MaxMB 80] [-Bundletool <jar>] [-RequireBundletool]'
}

# Resolver la lista de AAB.
$aabs = @()
if (Test-Path -LiteralPath $Path -PathType Container) {
    $aabs = @(Get-ChildItem -LiteralPath $Path -Filter '*.aab' -File -Recurse)
}
elseif (Test-Path -LiteralPath $Path -PathType Leaf) {
    $aabs = @(Get-Item -LiteralPath $Path)
}
else {
    $aabs = @(Get-ChildItem -Path $Path -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -eq '.aab' })
}
if ($aabs.Count -eq 0) {
    Stop-WithCode 2 "ERROR: no se encontro ningun .aab en '$Path'."
}
foreach ($a in $aabs) {
    if ($a.Extension -ne '.aab') { Stop-WithCode 2 "ERROR: '$($a.FullName)' no es un .aab." }
}

# Herramientas para la medida de Play.
$java = Get-Command java -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
$canBundletool = $Bundletool -and (Test-Path -LiteralPath $Bundletool -PathType Leaf) -and $java
if (-not $canBundletool) {
    $why = if (-not $Bundletool) { 'no se indico bundletool (-Bundletool o BUNDLETOOL_JAR)' }
    elseif (-not (Test-Path -LiteralPath $Bundletool -PathType Leaf)) { "no existe $Bundletool" }
    else { 'no se encuentra java en el PATH' }
    if ($RequireBundletool) { Stop-WithCode 3 "ERROR: -RequireBundletool y $why." }
    Write-Output "AVISO: sin medida de Play ($why); solo se comprueba el tamano del archivo .aab."
}

function Format-MB([long]$Bytes) {
    return ($Bytes / 1000000.0).ToString('0.00', [System.Globalization.CultureInfo]::InvariantCulture)
}

# Grupo de una entrada del .aab (modulo/ruta). Los asset packs (modulos distintos de base) van enteros en su grupo.
function Get-AabGroup([string]$Name) {
    $parts = $Name.Split('/')
    if ($parts.Count -lt 2) {
        if ($Name -in @('BundleConfig.pb')) { return 'metadatos y firma' }
        return 'otros'
    }
    $module = $parts[0]
    if ($module -in @('META-INF', 'BUNDLE-METADATA')) { return 'metadatos y firma' }
    if ($module -ne 'base') { return "asset pack $module" }
    switch ($parts[1]) {
        'lib' { if ($parts.Count -ge 4) { return "codigo nativo ($($parts[2]))" } else { return 'codigo nativo' } }
        'assets' { if ($parts.Count -ge 4 -and $parts[2] -eq 'bin') { return 'datos de Unity (assets/bin)' } else { return 'otros assets' } }
        'dex' { return 'dex (Java/Kotlin)' }
        'root' { return 'root (kotlin y otros)' }
        default { return 'recursos y manifiesto de Android' }
    }
}

function Write-AabBreakdown([System.IO.FileInfo]$Aab) {
    try {
        $zip = [System.IO.Compression.ZipFile]::OpenRead($Aab.FullName)
    }
    catch {
        Stop-WithCode 2 "ERROR: no se puede leer '$($Aab.FullName)' como ZIP: $($_.Exception.Message)"
    }
    try {
        $packed = @{}
        $raw = @{}
        foreach ($e in $zip.Entries) {
            $g = Get-AabGroup $e.FullName
            $packed[$g] = [long]($packed[$g] ?? 0) + $e.CompressedLength
            $raw[$g] = [long]($raw[$g] ?? 0) + $e.Length
        }
    }
    finally {
        $zip.Dispose()
    }
    Write-Output '  Desglose del .aab (MB comprimidos en el AAB / sin comprimir):'
    foreach ($g in ($packed.Keys | Sort-Object { - $packed[$_] }, { $_ })) {
        Write-Output ('    {0,-40} {1,8} {2,8}' -f $g, (Format-MB $packed[$g]), (Format-MB $raw[$g]))
    }
}

$failed = $false
$limitText = $limitMB.ToString('0.##', [System.Globalization.CultureInfo]::InvariantCulture)
Write-Output "Presupuesto: $limitText MB ($limitBytes bytes)"
foreach ($aab in $aabs) {
    $size = $aab.Length
    $status = if ($size -le $limitBytes) { 'OK' } else { 'SUPERA' }
    if ($status -ne 'OK') { $failed = $true }
    Write-Output ("[{0}] archivo .aab: {1} MB - {2}" -f $status, (Format-MB $size), $aab.FullName)
    if ($Breakdown) { Write-AabBreakdown $aab }

    if ($canBundletool) {
        $tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("size-gate-" + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Force -Path $tmp | Out-Null
        try {
            $apks = Join-Path $tmp 'set.apks'
            $out = & $java.Source -jar $Bundletool build-apks "--bundle=$($aab.FullName)" "--output=$apks" 2>&1 | ForEach-Object { "$_" }
            if ($LASTEXITCODE -ne 0) {
                Stop-WithCode 3 ("ERROR: bundletool build-apks fallo con $LASTEXITCODE`n" + (($out | Select-Object -Last 10) -join "`n"))
            }
            $csv = & $java.Source -jar $Bundletool get-size total "--apks=$apks" 2>&1 | ForEach-Object { "$_" }
            if ($LASTEXITCODE -ne 0) {
                Stop-WithCode 3 ("ERROR: bundletool get-size total fallo con $LASTEXITCODE`n" + (($csv | Select-Object -Last 10) -join "`n"))
            }
            # Salida esperada: "MIN,MAX" y una fila con los bytes.
            $row = $csv | Where-Object { $_ -match '^\s*\d+\s*,\s*\d+\s*$' } | Select-Object -Last 1
            if (-not $row) { Stop-WithCode 3 ("ERROR: salida inesperada de bundletool get-size total:`n" + ($csv -join "`n")) }
            $parts = $row -split ','
            $minBytes = [long]$parts[0].Trim()
            $maxBytes = [long]$parts[1].Trim()
            $playStatus = if ($maxBytes -le $limitBytes) { 'OK' } else { 'SUPERA' }
            if ($playStatus -ne 'OK') { $failed = $true }
            Write-Output ("[{0}] descarga Play (bundletool get-size total): min {1} MB, max {2} MB" -f $playStatus, (Format-MB $minBytes), (Format-MB $maxBytes))
        }
        finally {
            Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

if ($failed) {
    Stop-WithCode 1 "FALLA: al menos un AAB supera $limitText MB. Revisa el Build Report y GDD 08 sec. 6 (Addressables + PAD)."
}
Write-Output "OK: todos los AAB estan dentro de $limitText MB."
exit 0
