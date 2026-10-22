#Requires -Version 7.2
<#
.SYNOPSIS
    Lanza un metodo de build o de ajustes de AF.EditorTools con la Unity CLI y da el veredicto por el log del Editor.

.DESCRIPTION
    "unity run" puede salir con un codigo distinto de 0 aunque el metodo termine bien (p. ej. 6 por un aviso de
    licencias "Access token is unavailable" despues de "[AF.BuildDev] OK"; hoja de la prueba del 2026-10-22,
    observacion 7). Por eso el veredicto sale de la linea que escribe el propio metodo al terminar:

        [AF.<Metodo>] OK...                 -> 0 (aunque la CLI devuelva otro codigo; se avisa)
        [AF.<Metodo>] FALLO (codigo N).     -> 8 (y se muestra N: el codigo del metodo, ver su clase)

    Metodos (-Method):
        Dev      AF.EditorTools.BuildDev.BuildAndroidDev          -> Builds/Android/dev.apk
        Smoke    AF.EditorTools.BuildSmoke.BuildAndroid           -> Builds/Android/smoke.aab y smoke.apk
        Release  AF.EditorTools.BuildRelease.BuildAndroid         (con -Rehearsal: -afRehearsal; con -Apk: -afApk)
        Setup    AF.EditorTools.ProjectSetup.ApplyAndroidDefaults (ajustes Android versionados)

    Comando que lanza (sin Editor abierto en la carpeta):
        unity run <proyecto> --architecture <arch> --log-file <log> --no-tail --timeout <s> --non-interactive
                  -- -nographics [-buildTarget Android] -executeMethod <metodo> [-afRehearsal] [-afApk]

    Con -CheckLog <log> no lanza nada: solo da el veredicto de un log ya escrito (por ejemplo, el de un
    "unity run" lanzado a mano o por el semaforo de Unity).

    Nunca instala nada, nunca lanza batchmode sobre una carpeta con un Editor abierto y nunca sube nada.

    Codigos de salida:
      0  el metodo escribio su linea OK
      2  argumentos incorrectos
      3  el proyecto Unity todavia no existe
      4  no se encuentra la Unity CLI (o -CheckLog apunta a un log que no existe)
      5  el proyecto esta abierto en un Editor: cierralo o usa otro worktree
      6  sin veredicto: el log no tiene ni la linea OK ni la de FALLO (timeout, licencia, cuelgue): lee el log
      8  el metodo escribio FALLO (codigo N): corrige y vuelve a lanzar

.PARAMETER Method
    Dev (por defecto), Smoke, Release o Setup.

.PARAMETER ProjectPath
    Raiz del proyecto Unity. Por defecto, la carpeta padre de scripts/ (sirve igual en un worktree).

.PARAMETER Architecture
    Arquitectura del Editor para la Unity CLI: arm64 (portatil, por defecto) o x86_64 (escritorio).

.PARAMETER TimeoutSec
    Limite de "unity run" (60-14400; 5400 por defecto: el build limpio en el portatil tarda unos 26 min).

.PARAMETER LogFile
    Log del Editor (por defecto Logs/build-<method>.log dentro del proyecto).

.PARAMETER Rehearsal
    Solo con -Method Release: anade -afRehearsal (el ensayo con la identidad de prueba).

.PARAMETER Apk
    Solo con -Method Release: anade -afApk.

.PARAMETER CheckLog
    Solo da el veredicto de este log (no lanza Unity).

.PARAMETER UnityCli
    Ruta explicita a la Unity CLI (por defecto, el comando "unity" del PATH).

.EXAMPLE
    pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev
.EXAMPLE
    pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev -Architecture x86_64
.EXAMPLE
    pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev -CheckLog Logs/build-dev.log
#>
[CmdletBinding()]
param(
    [string]$Method = 'Dev',
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),
    [string]$Architecture = 'arm64',
    [string]$TimeoutSec = '5400',
    [string]$LogFile = '',
    [switch]$Rehearsal,
    [switch]$Apk,
    [string]$CheckLog = '',
    [string]$UnityCli = '',
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$UnknownArgs
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$env:UNITY_NON_INTERACTIVE = '1'
$env:UNITY_NO_BANNER = '1'
$env:UNITY_NO_PAGER = '1'

function Stop-WithCode {
    param([int]$Code, [string]$Message)
    [Console]::Error.WriteLine($Message)
    exit $Code
}

if ($UnknownArgs) { Stop-WithCode 2 "ERROR: argumentos no reconocidos: $($UnknownArgs -join ' ')" }

$methods = @{
    'dev'     = @{ Prefix = 'AF.BuildDev'; Execute = 'AF.EditorTools.BuildDev.BuildAndroidDev'; Android = $true }
    'smoke'   = @{ Prefix = 'AF.BuildSmoke'; Execute = 'AF.EditorTools.BuildSmoke.BuildAndroid'; Android = $true }
    'release' = @{ Prefix = 'AF.BuildRelease'; Execute = 'AF.EditorTools.BuildRelease.BuildAndroid'; Android = $true }
    'setup'   = @{ Prefix = 'AF.ProjectSetup'; Execute = 'AF.EditorTools.ProjectSetup.ApplyAndroidDefaults'; Android = $false }
}
$key = $Method.ToLowerInvariant()
if (-not $methods.ContainsKey($key)) { Stop-WithCode 2 "ERROR: -Method debe ser Dev, Smoke, Release o Setup (recibido: '$Method')." }
$m = $methods[$key]
if (($Rehearsal -or $Apk) -and $key -ne 'release') { Stop-WithCode 2 'ERROR: -Rehearsal y -Apk solo valen con -Method Release.' }
if (@('arm64', 'x86_64') -notcontains $Architecture) { Stop-WithCode 2 "ERROR: -Architecture debe ser arm64 o x86_64 (recibido: '$Architecture')." }
$timeoutSeconds = 0
if (-not [int]::TryParse($TimeoutSec, [ref]$timeoutSeconds) -or $timeoutSeconds -lt 60 -or $timeoutSeconds -gt 14400) {
    Stop-WithCode 2 "ERROR: -TimeoutSec debe ser un entero entre 60 y 14400 (recibido: '$TimeoutSec')."
}

# Veredicto del log: la ultima linea OK o FALLO del metodo (el Editor puede escribir otras cosas despues).
function Get-BuildVerdict {
    param([string]$Path, [string]$Prefix)
    $ok = '\[' + [regex]::Escape($Prefix) + '\] OK\b'
    $fail = '\[' + [regex]::Escape($Prefix) + '\] FALLO \(c\S*digo (\d+)\)'
    $verdict = [pscustomobject]@{ Kind = 'none'; Code = 0; Line = '' }
    foreach ($hit in (Select-String -LiteralPath $Path -Pattern "$ok|$fail" -Encoding utf8)) {
        $line = $hit.Line.Trim()
        if ($line -match $fail) {
            $verdict = [pscustomobject]@{ Kind = 'fail'; Code = [int]$Matches[1]; Line = $line }
        }
        else {
            $verdict = [pscustomobject]@{ Kind = 'ok'; Code = 0; Line = $line }
        }
    }
    return $verdict
}

function Write-VerdictAndExit {
    param([string]$Path, [string]$Prefix, [object]$CliCode)
    $v = Get-BuildVerdict -Path $Path -Prefix $Prefix
    if ($v.Kind -eq 'ok') {
        if ($null -ne $CliCode -and $CliCode -ne 0) {
            Write-Output "AVISO: unity run salio con $CliCode, pero el metodo termino bien (el veredicto sale del log)."
        }
        Write-Output "OK: $($v.Line)"
        Write-Output "Log: $Path"
        exit 0
    }
    if ($v.Kind -eq 'fail') {
        Write-Output "FALLO: $($v.Line) (codigo del metodo $($v.Code); ver su clase en Assets/_Project/Editor/CI/)."
        Write-Output "Log: $Path"
        exit 8
    }
    $hint = if ($null -ne $CliCode) { " (unity run salio con $CliCode)" } else { '' }
    Stop-WithCode 6 "SIN VEREDICTO$($hint): el log no tiene '[$Prefix] OK' ni '[$Prefix] FALLO'. Lee $Path (licencia, timeout, cuelgue, errores de compilacion)."
}

# Solo veredicto de un log ya escrito.
if ($CheckLog) {
    if (-not (Test-Path -LiteralPath $CheckLog -PathType Leaf)) { Stop-WithCode 4 "ERROR: no existe el log: $CheckLog" }
    Write-VerdictAndExit -Path (Resolve-Path -LiteralPath $CheckLog).Path -Prefix $m.Prefix -CliCode $null
}

# Proyecto.
if (-not (Test-Path -LiteralPath $ProjectPath -PathType Container)) { Stop-WithCode 2 "ERROR: la carpeta del proyecto no existe: $ProjectPath" }
$root = (Resolve-Path -LiteralPath $ProjectPath).Path
foreach ($need in @('ProjectSettings/ProjectVersion.txt', 'Packages/manifest.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $need) -PathType Leaf)) { Stop-WithCode 3 "ERROR: el proyecto Unity todavia no existe en '$root' (falta $need)." }
}
if (-not (Test-Path -LiteralPath (Join-Path $root 'Assets') -PathType Container)) { Stop-WithCode 3 "ERROR: el proyecto Unity todavia no existe en '$root' (falta Assets/)." }

# Editor abierto: nunca batchmode sobre la misma carpeta.
$lock = Join-Path $root 'Temp/UnityLockfile'
if (Test-Path -LiteralPath $lock -PathType Leaf) {
    $open = $false
    try {
        $stream = [System.IO.File]::Open($lock, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        $stream.Dispose()
    }
    catch { $open = $true }
    if ($open) { Stop-WithCode 5 'ERROR: el proyecto esta abierto en un Editor (Temp/UnityLockfile bloqueado): cierralo o usa otro worktree.' }
}

# Unity CLI.
$cli = $null
if ($UnityCli) {
    if (-not (Test-Path -LiteralPath $UnityCli -PathType Leaf)) { Stop-WithCode 4 "ERROR: -UnityCli no existe: $UnityCli" }
    $cli = (Resolve-Path -LiteralPath $UnityCli).Path
}
else {
    $cmd = Get-Command unity -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cmd) { $cli = $cmd.Source }
}
if (-not $cli) { Stop-WithCode 4 'ERROR: no se encuentra la Unity CLI (comando "unity").' }

if (-not $LogFile) { $LogFile = Join-Path $root "Logs/build-$key.log" }
$logDir = Split-Path -Parent $LogFile
if ($logDir -and -not (Test-Path -LiteralPath $logDir)) { New-Item -ItemType Directory -Force -Path $logDir | Out-Null }
if (Test-Path -LiteralPath $LogFile) {
    try { Remove-Item -LiteralPath $LogFile -Force -ErrorAction Stop }
    catch {
        $LogFile = Join-Path $logDir ('{0}-{1}-{2}.log' -f [System.IO.Path]::GetFileNameWithoutExtension($LogFile), (Get-Date -Format 'yyyyMMdd-HHmmss'), $PID)
        Write-Output "AVISO: el log anterior esta abierto por otro proceso; se usa $LogFile."
    }
}

$unityArgs = @('-nographics')
if ($m.Android) { $unityArgs += @('-buildTarget', 'Android') }
$unityArgs += @('-executeMethod', $m.Execute)
if ($Rehearsal) { $unityArgs += '-afRehearsal' }
if ($Apk) { $unityArgs += '-afApk' }
$cliArgs = @('run', $root, '--architecture', $Architecture, '--log-file', $LogFile, '--no-tail', '--timeout', "$timeoutSeconds", '--non-interactive', '--') + $unityArgs
Write-Output "unity run ... -executeMethod $($m.Execute) (log: $LogFile) ..."
$cliOut = @(& $cli @cliArgs 2>&1 | ForEach-Object { "$_" })
$cliCode = $LASTEXITCODE
if (-not (Test-Path -LiteralPath $LogFile -PathType Leaf)) {
    $cliOut | Select-Object -Last 10 | ForEach-Object { Write-Output ('  ' + $_) }
    Stop-WithCode 6 "SIN VEREDICTO: la Unity CLI salio con $cliCode sin escribir el log $LogFile."
}
Write-VerdictAndExit -Path $LogFile -Prefix $m.Prefix -CliCode $cliCode
