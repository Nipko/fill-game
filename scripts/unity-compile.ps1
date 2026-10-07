#Requires -Version 7.2
<#
.SYNOPSIS
    Comprueba si el proyecto Unity de Amazon Fill compila y muestra solo las primeras lineas "error CS".

.DESCRIPTION
    Lee la version del Editor de ProjectSettings/ProjectVersion.txt y elige la via mas barata:

      1. Editor abierto sobre esta carpeta y Unity CLI disponible:
         unity recompile --project-path <proyecto>    (segundos; necesita com.unity.pipeline)
      2. Editor cerrado y Unity CLI disponible:
         unity run <proyecto> --log-file Logs/compile.log --no-tail --architecture arm64 -- -nographics -accept-apiupdate
      3. Respaldo sin CLI (o si la CLI no llega a lanzar el Editor):
         Unity.exe -batchmode -nographics -quit -accept-apiupdate -projectPath <proyecto> -logFile Logs/compile.log

    Nunca instala nada (no pasa --allow-install) y nunca lanza batchmode sobre una carpeta que ya
    tiene un Editor abierto. Del log solo devuelve las primeras -MaxErrors lineas "error CS" distintas.

    Codigos de salida:
      0  compila
      1  errores de compilacion (se listan las primeras -MaxErrors lineas "error CS")
      2  argumentos o entorno incorrectos (p. ej. PowerShell anterior a 7.2)
      3  el proyecto Unity todavia no existe (falta ProjectSettings/ProjectVersion.txt, Assets/ o Packages/manifest.json)
      4  no se encuentra la Unity CLI ni Unity.exe de la version fijada
      5  el proyecto ya esta abierto en un Editor y no se puede usar "unity recompile"
      6  sin veredicto (timeout, licencia, Editor caido o inalcanzable): lee el log indicado

.PARAMETER ProjectPath
    Raiz del proyecto Unity. Por defecto, la carpeta padre de scripts/ (sirve igual en un worktree).

.PARAMETER Runner
    auto (por defecto), cli (solo Unity CLI) o batchmode (solo Unity.exe).

.PARAMETER MaxErrors
    Numero maximo de lineas "error CS" que se muestran (30 por defecto).

.PARAMETER TimeoutSec
    Limite de tiempo del Editor en batchmode. La primera importacion de un worktree nuevo puede
    tardar 10-20 min: usa 3600 en ese caso.

.PARAMETER Architecture
    Arquitectura del Editor que se usa con la Unity CLI (arm64 por defecto; x86_64 solo en el plan B1).

.PARAMETER UnityExe
    Ruta explicita a Unity.exe para el respaldo (tambien se lee de la variable UNITY_EDITOR_PATH).

.PARAMETER LogFile
    Log del Editor (por defecto Logs/compile.log dentro del proyecto; Logs/ esta en .gitignore).

.EXAMPLE
    pwsh -NoProfile -File scripts/unity-compile.ps1
.EXAMPLE
    pwsh -NoProfile -File scripts/unity-compile.ps1 -TimeoutSec 3600 -Runner batchmode
#>
# Los parametros se validan a mano (no con ValidateSet/ValidateRange) para que un argumento
# incorrecto salga siempre con 2 y nunca con 1, que aqui significa "errores de compilacion".
# $UnknownArgs recoge los parametros desconocidos o mal escritos (p. ej. -Bogus 1, -Runer cli),
# que sin el saldrian con 1 por el error de enlace de PowerShell.
# Unica excepcion: un parametro conocido sin valor (p. ej. "-Runner" al final) lo rechaza PowerShell
# antes de ejecutar el script y sale con 1 y "Missing an argument for parameter": es un argumento
# mal escrito, no un error de compilacion.
[CmdletBinding()]
param(
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),
    [string]$Runner = 'auto',
    [string]$MaxErrors = '30',
    [string]$TimeoutSec = '1800',
    [string]$Architecture = 'arm64',
    [string]$UnityExe = '',
    [string]$LogFile = '',
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$UnknownArgs
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

# La Unity CLI no debe preguntar nada ni paginar cuando la llama un agente.
$env:UNITY_NON_INTERACTIVE = '1'
$env:UNITY_NO_BANNER = '1'
$env:UNITY_NO_PAGER = '1'

function Stop-WithCode {
    param([int]$Code, [string]$Message)
    [Console]::Error.WriteLine($Message)
    exit $Code
}

# Primera comprobacion tras definir Stop-WithCode (si se llamara antes, el error saldria con 1).
if ($UnknownArgs) { Stop-WithCode 2 "ERROR: argumentos no reconocidos: $($UnknownArgs -join ' ')" }

function ConvertTo-CheckedInt {
    param([string]$Name, [string]$Value, [int]$Min, [int]$Max)
    $n = 0
    if (-not [int]::TryParse($Value, [ref]$n) -or $n -lt $Min -or $n -gt $Max) {
        Stop-WithCode 2 "ERROR: -$Name debe ser un entero entre $Min y $Max (recibido: '$Value')."
    }
    return $n
}

if (@('auto', 'cli', 'batchmode') -notcontains $Runner) { Stop-WithCode 2 "ERROR: -Runner debe ser auto, cli o batchmode (recibido: '$Runner')." }
if (@('arm64', 'x86_64') -notcontains $Architecture) { Stop-WithCode 2 "ERROR: -Architecture debe ser arm64 o x86_64 (recibido: '$Architecture')." }
$Runner = $Runner.ToLowerInvariant()
$Architecture = $Architecture.ToLowerInvariant()
$maxLines = ConvertTo-CheckedInt -Name 'MaxErrors' -Value $MaxErrors -Min 1 -Max 500
$timeoutSeconds = ConvertTo-CheckedInt -Name 'TimeoutSec' -Value $TimeoutSec -Min 60 -Max 14400

function Get-UnityProjectInfo {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        Stop-WithCode 2 "ERROR: la carpeta del proyecto no existe: $Path"
    }
    $root = (Resolve-Path -LiteralPath $Path).Path
    $versionFile = Join-Path $root 'ProjectSettings/ProjectVersion.txt'
    $missing = @()
    if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) { $missing += 'ProjectSettings/ProjectVersion.txt' }
    if (-not (Test-Path -LiteralPath (Join-Path $root 'Assets') -PathType Container)) { $missing += 'Assets/' }
    if (-not (Test-Path -LiteralPath (Join-Path $root 'Packages/manifest.json') -PathType Leaf)) { $missing += 'Packages/manifest.json' }
    if ($missing.Count -gt 0) {
        $lines = @(
            "ERROR: el proyecto Unity todavia no existe en '$root'.",
            "Falta: $($missing -join ', ').",
            'Se crea en el paso s20 de docs/setup/plan-instalacion.md, cuando el propietario haya iniciado sesion en Unity,',
            'activado la licencia Personal (s15-s16) y terminado la instalacion del Editor 6000.3.25f1 (s18).',
            'No lo crees a mano. Mientras tanto verifica el nucleo con:',
            '  dotnet build dotnet/AmazonFill.slnx -warnaserror',
            '  dotnet test dotnet/AmazonFill.slnx'
        )
        Stop-WithCode 3 ($lines -join [Environment]::NewLine)
    }
    $text = Get-Content -LiteralPath $versionFile -Raw
    $version = $null
    $changeset = $null
    if ($text -match '(?m)^m_EditorVersion:\s*(\S+)\s*$') { $version = $Matches[1] }
    if ($text -match '(?m)^m_EditorVersionWithRevision:\s*\S+\s*\(([0-9a-f]+)\)') { $changeset = $Matches[1] }
    if (-not $version) {
        Stop-WithCode 2 "ERROR: no se pudo leer m_EditorVersion de $versionFile"
    }
    [pscustomobject]@{ Root = $root; Version = $version; Changeset = $changeset }
}

function Test-ProjectOpenInEditor {
    param([string]$Root)
    # Unity mantiene Temp/UnityLockfile bloqueado mientras el proyecto esta abierto.
    $lock = Join-Path $Root 'Temp/UnityLockfile'
    if (-not (Test-Path -LiteralPath $lock -PathType Leaf)) { return $false }
    try {
        $stream = [System.IO.File]::Open($lock, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        $stream.Dispose()
        return $false   # lockfile huerfano de un cierre anterior
    }
    catch {
        return $true
    }
}

function Get-UnityCli {
    $cmd = Get-Command unity -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $cmd) { return $null }
    try {
        $v = (& $cmd.Source --version 2>$null | Out-String).Trim()
        if ($LASTEXITCODE -eq 0 -and $v -match '^\d+\.\d+\.\d+') { return $cmd.Source }
    }
    catch { }
    return $null
}

function Find-UnityEditorExe {
    param([string]$Version, [string]$Explicit, [string]$Cli)
    $candidates = New-Object System.Collections.Generic.List[string]
    if ($Explicit) { $candidates.Add($Explicit) }
    if ($env:UNITY_EDITOR_PATH) { $candidates.Add($env:UNITY_EDITOR_PATH) }
    if ($Cli) {
        try {
            $installRoot = (& $Cli install-path --get 2>$null | Out-String).Trim()
            if ($LASTEXITCODE -eq 0 -and $installRoot) { $candidates.Add((Join-Path $installRoot "$Version/Editor/Unity.exe")) }
        }
        catch { }
    }
    if ($env:ProgramFiles) { $candidates.Add((Join-Path $env:ProgramFiles "Unity/Hub/Editor/$Version/Editor/Unity.exe")) }
    foreach ($c in $candidates) {
        if ($c -and (Test-Path -LiteralPath $c -PathType Leaf)) { return (Resolve-Path -LiteralPath $c).Path }
    }
    if ($Cli) {
        # Ultimo intento: buscar la ruta en el listado JSON de Editores instalados (solo lectura).
        try {
            $json = & $Cli editors --installed --verbose --format json 2>$null | Out-String
            $pattern = '[A-Za-z]:[\\/][^"]*?' + [regex]::Escape($Version) + '[^"]*?Unity\.exe'
            foreach ($m in [regex]::Matches($json, $pattern)) {
                $p = $m.Value -replace '\\\\', '\'
                if (Test-Path -LiteralPath $p -PathType Leaf) { return $p }
            }
        }
        catch { }
    }
    return $null
}

function Invoke-ProcessWithTimeout {
    param([string]$FilePath, [string[]]$Arguments, [int]$TimeoutSec)
    $psi = [System.Diagnostics.ProcessStartInfo]::new($FilePath)
    foreach ($a in $Arguments) { $psi.ArgumentList.Add($a) }
    $psi.UseShellExecute = $false
    $proc = [System.Diagnostics.Process]::Start($psi)
    if (-not $proc.WaitForExit($TimeoutSec * 1000)) {
        try { $proc.Kill($true) } catch { }
        return -999
    }
    return $proc.ExitCode
}

function Get-CompileErrorLines {
    param([string]$Path, [int]$Max)
    $result = New-Object System.Collections.Generic.List[string]
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return , $result }
    $seen = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($m in (Select-String -LiteralPath $Path -Pattern 'error CS\d{4}' -Encoding utf8)) {
        $line = $m.Line.Trim()
        if ($seen.Add($line)) {
            $result.Add($line)
            if ($result.Count -ge $Max) { break }
        }
    }
    return , $result
}

function Get-TotalCompileErrorCount {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return 0 }
    return @(Select-String -LiteralPath $Path -Pattern 'error CS\d{4}' -Encoding utf8 | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique).Count
}

function Write-LogHints {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return }
    $known = 'No valid Unity Editor license|[Ll]icense.*(error|fail|not)|another Unity instance is running|Multiple Unity instances cannot open the same project|Aborting batchmode|Scripts have compiler errors|Safe Mode|Exception|Fatal'
    $hits = @(Select-String -LiteralPath $Path -Pattern $known -Encoding utf8 | Select-Object -First 10)
    if ($hits.Count -gt 0) {
        Write-Output 'Pistas del log:'
        foreach ($h in $hits) { Write-Output ('  ' + $h.Line.Trim()) }
    }
}

# --------------------------------------------------------------------------------------------
$info = Get-UnityProjectInfo -Path $ProjectPath
$label = if ($info.Changeset) { "$($info.Version) ($($info.Changeset))" } else { $info.Version }
Write-Output "Unity $label - proyecto: $($info.Root)"

if (-not $LogFile) { $LogFile = Join-Path $info.Root 'Logs/compile.log' }
$logDir = Split-Path -Parent $LogFile
if ($logDir -and -not (Test-Path -LiteralPath $logDir)) { New-Item -ItemType Directory -Force -Path $logDir | Out-Null }

$cli = $null
if ($Runner -ne 'batchmode') { $cli = Get-UnityCli }
if ($Runner -eq 'cli' -and -not $cli) {
    Stop-WithCode 4 'ERROR: no se encuentra la Unity CLI (comando "unity"). Instalacion: paso s04 del plan (la hace el propietario).'
}

# 1) Editor abierto: nunca batchmode sobre la misma carpeta.
if (Test-ProjectOpenInEditor -Root $info.Root) {
    if (-not $cli) {
        Stop-WithCode 5 ("ERROR: el proyecto esta abierto en un Editor (Temp/UnityLockfile bloqueado) y no hay Unity CLI para 'unity recompile'." +
            [Environment]::NewLine + 'No se lanza batchmode sobre una carpeta abierta: cierra ese Editor o trabaja en otro worktree.')
    }
    Write-Output 'Editor abierto sobre esta carpeta: unity recompile'
    $recompileTimeout = [Math]::Min($timeoutSeconds, 600)
    $out = & $cli recompile --project-path $info.Root --timeout $recompileTimeout 2>&1 | ForEach-Object { "$_" }
    $code = $LASTEXITCODE
    if ($code -eq 0) {
        Write-Output 'OK: compila (unity recompile).'
        exit 0
    }
    if ($code -eq 6) {
        $errs = @($out | Where-Object { $_ -match '\bCS\d{4}\b' } | Select-Object -Unique)
        if ($errs.Count -eq 0) { $errs = @($out | Where-Object { $_.Trim() }) }
        Write-Output "Errores de compilacion (primeras $([Math]::Min($maxLines, $errs.Count)) de $($errs.Count)):"
        $errs | Select-Object -First $maxLines | ForEach-Object { Write-Output ('  ' + $_.Trim()) }
        exit 1
    }
    if ($code -eq 7) {
        Stop-WithCode 6 ('SIN VEREDICTO (unity recompile salio con 7): el Editor esta abierto pero no responde, arranco en Safe Mode' +
            ' o el paquete com.unity.pipeline no esta cargado. Lee %LOCALAPPDATA%\Unity\Editor\Editor.log, corrige los errores' +
            ' y reinicia el Editor. No lances batchmode sobre esta carpeta.')
    }
    $tail = ($out | Select-Object -Last 15) -join [Environment]::NewLine
    Stop-WithCode 6 "SIN VEREDICTO (unity recompile salio con $code):$([Environment]::NewLine)$tail"
}

# 2) Editor cerrado: compilacion en batchmode.
if (Test-Path -LiteralPath $LogFile) { Remove-Item -LiteralPath $LogFile -Force }
$exitCode = $null
$via = $null

if ($cli -and $Runner -ne 'batchmode') {
    $via = 'unity run'
    Write-Output "Compilando en batchmode con la Unity CLI (log: $LogFile) ..."
    $cliArgs = @('run', $info.Root, '--log-file', $LogFile, '--no-tail', '--timeout', "$timeoutSeconds",
        '--architecture', $Architecture, '--', '-nographics', '-accept-apiupdate')
    $cliOut = & $cli @cliArgs 2>&1 | ForEach-Object { "$_" }
    $exitCode = $LASTEXITCODE
    if (-not (Test-Path -LiteralPath $LogFile -PathType Leaf)) {
        Write-Output "La Unity CLI no llego a lanzar el Editor (salida $exitCode):"
        $cliOut | Select-Object -Last 10 | ForEach-Object { Write-Output ('  ' + $_) }
        if ($Runner -eq 'cli') { Stop-WithCode 6 'SIN VEREDICTO: revisa la salida anterior de la Unity CLI.' }
        $exitCode = $null   # se intenta el respaldo con Unity.exe
    }
}

if ($null -eq $exitCode) {
    $exe = Find-UnityEditorExe -Version $info.Version -Explicit $UnityExe -Cli $cli
    if (-not $exe) {
        Stop-WithCode 4 ("ERROR: no se encuentra Unity.exe $($info.Version). Instalacion: paso s18 del plan (la hace el propietario)." +
            [Environment]::NewLine + 'Si esta en otra carpeta, pasa -UnityExe <ruta> o define UNITY_EDITOR_PATH.')
    }
    $via = 'Unity.exe'
    Write-Output "Compilando en batchmode con $exe (log: $LogFile) ..."
    $unityArgs = @('-batchmode', '-nographics', '-quit', '-accept-apiupdate', '-projectPath', $info.Root, '-logFile', $LogFile)
    $exitCode = Invoke-ProcessWithTimeout -FilePath $exe -Arguments $unityArgs -TimeoutSec $timeoutSeconds
    if ($exitCode -eq -999) {
        Stop-WithCode 6 "SIN VEREDICTO: el Editor supero el limite de $timeoutSeconds s y se ha cerrado. Log: $LogFile"
    }
}

$errors = Get-CompileErrorLines -Path $LogFile -Max $maxLines
if ($errors.Count -gt 0 -and $exitCode -ne 0) {
    $total = Get-TotalCompileErrorCount -Path $LogFile
    Write-Output "Errores de compilacion (primeras $($errors.Count) de $total lineas distintas):"
    foreach ($e in $errors) { Write-Output ('  ' + $e) }
    Write-Output "Log completo: $LogFile"
    exit 1
}
if ($exitCode -eq 0) {
    if ($errors.Count -gt 0) {
        # Unity en batchmode sale con 1 si al final quedan errores; con 0, son de pasadas intermedias
        # (p. ej. antes de resolver paquetes en la primera importacion).
        Write-Output "AVISO: el log tiene $($errors.Count) lineas 'error CS' de pasadas intermedias, pero el Editor termino con 0:"
        foreach ($e in ($errors | Select-Object -First 5)) { Write-Output ('  ' + $e) }
    }
    Write-Output "OK: compila ($via, log: $LogFile)."
    exit 0
}
Write-LogHints -Path $LogFile
Stop-WithCode 6 "SIN VEREDICTO: $via salio con $exitCode sin errores CS en el log. Lee $LogFile (licencia, otro Editor abierto, cuelgue)."
