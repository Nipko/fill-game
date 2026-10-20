#Requires -Version 7.2
<#
.SYNOPSIS
    Ejecuta los tests EditMode o PlayMode del proyecto Unity de Amazon Fill y resume el resultado.

.DESCRIPTION
    Lee la version del Editor de ProjectSettings/ProjectVersion.txt y ejecuta los tests en batchmode:

      1. Con la Unity CLI (preferido):
         unity test <proyecto> --mode <Mode> --output Logs/<mode>.xml --timeout <s> --architecture arm64
                    [--filter <patron>] -- -nographics -logFile Logs/<mode>.log
      2. Respaldo sin CLI (o si la CLI no llega a lanzar el Editor):
         Unity.exe -batchmode -nographics -accept-apiupdate -projectPath <proyecto> -runTests
                   -testPlatform <Mode> -testResults Logs/<mode>.xml -logFile Logs/<mode>.log [-testFilter <patron>]
         (sin -quit: -runTests cierra el Editor al escribir los resultados)

    Si otro proceso (Unity.Licensing.Client o el broker de la CLI) tiene abierto el XML o el log de
    una ejecucion anterior y no se puede borrar, se usa un nombre unico junto a el
    (<nombre>-<fecha>-<pid><ext>) y se avisa; los codigos de salida no cambian por ello. Los nombres
    alternativos de ejecuciones anteriores se borran al empezar (si ya no estan abiertos).

    Nunca instala nada (no pasa --allow-install) y nunca lanza batchmode sobre una carpeta con un
    Editor abierto. Muestra el resumen del XML NUnit, los tests fallidos y, si no compila, solo las
    primeras -MaxErrors lineas "error CS" del log.

    Codigos de salida (los mismos que "unity test" mas los propios del script):
      0  todos los tests pasan
      2  argumentos o entorno incorrectos
      3  el proyecto Unity todavia no existe (falta ProjectSettings/ProjectVersion.txt, Assets/ o Packages/manifest.json)
      4  no se encuentra la Unity CLI ni Unity.exe de la version fijada
      5  el proyecto esta abierto en un Editor: cierralo o usa otro worktree
      6  sin veredicto (no compila, licencia, timeout, cuelgue o 0 tests): lee el log; no reintentes a ciegas
      8  los tests se ejecutaron y alguno fallo: corrige, no reintentes

.PARAMETER Mode
    EditMode (por defecto) o PlayMode. PlayMode es obligatorio antes de cada PR que toque Unity.

.PARAMETER Filter
    Patron de nombres de test (se pasa a --filter / -testFilter).

.PARAMETER ProjectPath
    Raiz del proyecto Unity. Por defecto, la carpeta padre de scripts/ (sirve igual en un worktree).

.PARAMETER Runner
    auto (por defecto), cli (solo Unity CLI) o batchmode (solo Unity.exe).

.PARAMETER TimeoutSec
    Limite de tiempo. Por defecto, 900 s en EditMode y 3600 s en PlayMode (la suite completa tarda
    unos 30-40 min y a 900 s no da veredicto). Primera importacion de un worktree nuevo: 3600.

.PARAMETER MaxErrors
    Maximo de lineas "error CS" y de tests fallidos que se muestran (30 por defecto).

.PARAMETER AllowEmpty
    Acepta una ejecucion con 0 tests como verde (por defecto es "sin veredicto", codigo 6).

.EXAMPLE
    pwsh -NoProfile -File scripts/unity-test.ps1 -Mode EditMode
.EXAMPLE
    pwsh -NoProfile -File scripts/unity-test.ps1 -Mode PlayMode -TimeoutSec 7200
#>
# Los parametros se validan a mano (no con ValidateSet/ValidateRange) para que un argumento
# incorrecto salga siempre con 2, como "unity test".
# $UnknownArgs recoge los parametros desconocidos o mal escritos (p. ej. -Mdoe PlayMode), que sin
# el saldrian con 1 por el error de enlace de PowerShell.
# Unica excepcion: un parametro conocido sin valor (p. ej. "-Mode" al final) lo rechaza PowerShell
# antes de ejecutar el script y sale con 1 y "Missing an argument for parameter": es un argumento
# mal escrito; corrige la llamada.
[CmdletBinding()]
param(
    [string]$Mode = 'EditMode',
    [string]$Filter = '',
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),
    [string]$Runner = 'auto',
    [string]$TimeoutSec = '',
    [string]$MaxErrors = '30',
    [string]$Architecture = 'arm64',
    [string]$UnityExe = '',
    [string]$ResultsFile = '',
    [string]$LogFile = '',
    [switch]$AllowEmpty,
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

switch ($Mode.ToLowerInvariant()) {
    'editmode' { $Mode = 'EditMode' }
    'playmode' { $Mode = 'PlayMode' }
    default { Stop-WithCode 2 "ERROR: -Mode debe ser EditMode o PlayMode (recibido: '$Mode')." }
}
if (@('auto', 'cli', 'batchmode') -notcontains $Runner) { Stop-WithCode 2 "ERROR: -Runner debe ser auto, cli o batchmode (recibido: '$Runner')." }
if (@('arm64', 'x86_64') -notcontains $Architecture) { Stop-WithCode 2 "ERROR: -Architecture debe ser arm64 o x86_64 (recibido: '$Architecture')." }
$Runner = $Runner.ToLowerInvariant()
$Architecture = $Architecture.ToLowerInvariant()
if ($TimeoutSec -eq '') { $TimeoutSec = if ($Mode -eq 'PlayMode') { '3600' } else { '900' } }
$timeoutSeconds = ConvertTo-CheckedInt -Name 'TimeoutSec' -Value $TimeoutSec -Min 60 -Max 14400
$maxLines = ConvertTo-CheckedInt -Name 'MaxErrors' -Value $MaxErrors -Min 1 -Max 500

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
            'No lo crees a mano. Mientras tanto, los tests del nucleo corren sin Unity:',
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
    $lock = Join-Path $Root 'Temp/UnityLockfile'
    if (-not (Test-Path -LiteralPath $lock -PathType Leaf)) { return $false }
    try {
        $stream = [System.IO.File]::Open($lock, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        $stream.Dispose()
        return $false
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
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return , @() }
    $all = @(Select-String -LiteralPath $Path -Pattern 'error CS\d{4}' -Encoding utf8 | ForEach-Object { $_.Line.Trim() } | Select-Object -Unique)
    return , $all
}

function Write-CompileErrors {
    # Solo escribe; no devuelve nada (para no mezclar texto y valores en la salida de la funcion).
    param([string[]]$Lines, [int]$Max)
    if ($Lines.Count -eq 0) { return }
    Write-Output "Errores de compilacion (primeras $([Math]::Min($Max, $Lines.Count)) de $($Lines.Count) lineas distintas):"
    $Lines | Select-Object -First $Max | ForEach-Object { Write-Output ('  ' + $_) }
}

function Write-LogHints {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return }
    $known = 'No valid Unity Editor license|[Ll]icense.*(error|fail|not)|another Unity instance is running|Multiple Unity instances cannot open the same project|Aborting batchmode|Scripts have compiler errors|Safe Mode|Fatal'
    $hits = @(Select-String -LiteralPath $Path -Pattern $known -Encoding utf8 | Select-Object -First 10)
    if ($hits.Count -gt 0) {
        Write-Output 'Pistas del log:'
        foreach ($h in $hits) { Write-Output ('  ' + $h.Line.Trim()) }
    }
}

function Read-NUnitResults {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    try {
        [xml]$doc = Get-Content -LiteralPath $Path -Raw -Encoding utf8
    }
    catch {
        return $null
    }
    $run = $doc.SelectSingleNode('/test-run')
    if (-not $run) { return $null }
    $failed = @($doc.SelectNodes("//test-case[@result='Failed']"))
    [pscustomobject]@{
        Result       = [string]$run.GetAttribute('result')
        Total        = [int]('0' + $run.GetAttribute('total'))
        Passed       = [int]('0' + $run.GetAttribute('passed'))
        Failed       = [int]('0' + $run.GetAttribute('failed'))
        Skipped      = [int]('0' + $run.GetAttribute('skipped'))
        Inconclusive = [int]('0' + $run.GetAttribute('inconclusive'))
        Duration     = [string]$run.GetAttribute('duration')
        FailedCases  = $failed
    }
}

function Write-Results {
    param($Results, [int]$Max)
    Write-Output ("{0}: {1} tests, {2} pasan, {3} fallan, {4} omitidos, {5} sin conclusion ({6} s). Resultado: {7}" -f `
            $Mode, $Results.Total, $Results.Passed, $Results.Failed, $Results.Skipped, $Results.Inconclusive, $Results.Duration, $Results.Result)
    if ($Results.FailedCases.Count -gt 0) {
        Write-Output "Tests fallidos (primeros $([Math]::Min($Max, $Results.FailedCases.Count)) de $($Results.FailedCases.Count)):"
        foreach ($tc in ($Results.FailedCases | Select-Object -First $Max)) {
            $msgNode = $tc.SelectSingleNode('failure/message')
            $msg = ''
            if ($msgNode) { $msg = (($msgNode.InnerText -split "`n") | Where-Object { $_.Trim() } | Select-Object -First 1) }
            if ($msg) { $msg = ' :: ' + $msg.Trim() }
            Write-Output ('  ' + $tc.GetAttribute('fullname') + $msg)
        }
    }
}

function Resolve-FreeOutputFile {
    # Borra el archivo de una ejecucion anterior y devuelve la ruta que se usara. Si otro proceso lo tiene abierto
    # (Unity.Licensing.Client o el broker de la Unity CLI que sobreviven al Editor), Remove-Item falla: en vez de salir
    # con 1 se usa un nombre unico junto a el (<nombre>-<fecha>-<pid><ext>), que ademas evita leer resultados viejos.
    param([string]$Path)
    # Limpieza de los nombres alternativos de ejecuciones anteriores (los que sigan abiertos se quedan).
    $dir = Split-Path -Parent $Path
    $name = [System.IO.Path]::GetFileNameWithoutExtension($Path)
    $ext = [System.IO.Path]::GetExtension($Path)
    if ($dir -and (Test-Path -LiteralPath $dir -PathType Container)) {
        $pattern = '^' + [regex]::Escape($name) + '-\d{8}-\d{6}-\d+' + [regex]::Escape($ext) + '$'
        Get-ChildItem -LiteralPath $dir -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match $pattern } |
            ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue }
    }
    if (-not (Test-Path -LiteralPath $Path)) { return $Path }
    try {
        Remove-Item -LiteralPath $Path -Force -ErrorAction Stop
        return $Path
    }
    catch {
        $alt = Join-Path $dir ('{0}-{1}-{2}{3}' -f $name, (Get-Date -Format 'yyyyMMdd-HHmmss'), $PID, $ext)
        [Console]::Out.WriteLine("AVISO: no se pudo borrar $Path (lo tiene abierto otro proceso); se usa $alt.")
        return $alt
    }
}

# --------------------------------------------------------------------------------------------
$info = Get-UnityProjectInfo -Path $ProjectPath
$label = if ($info.Changeset) { "$($info.Version) ($($info.Changeset))" } else { $info.Version }
Write-Output "Unity $label - proyecto: $($info.Root) - tests $Mode"

$modeLower = $Mode.ToLowerInvariant()
if (-not $ResultsFile) { $ResultsFile = Join-Path $info.Root "Logs/$modeLower.xml" }
if (-not $LogFile) { $LogFile = Join-Path $info.Root "Logs/$modeLower.log" }
foreach ($f in @($ResultsFile, $LogFile)) {
    $d = Split-Path -Parent $f
    if ($d -and -not (Test-Path -LiteralPath $d)) { New-Item -ItemType Directory -Force -Path $d | Out-Null }
}
$ResultsFile = Resolve-FreeOutputFile -Path $ResultsFile
$LogFile = Resolve-FreeOutputFile -Path $LogFile

if (Test-ProjectOpenInEditor -Root $info.Root) {
    Stop-WithCode 5 ("ERROR: el proyecto esta abierto en un Editor (Temp/UnityLockfile bloqueado)." + [Environment]::NewLine +
        'No se lanza batchmode sobre una carpeta abierta. Opciones: ejecutar los tests desde el Test Runner de ese Editor,' +
        ' cerrarlo (pide permiso: "unity close" cierra sin guardar) o trabajar en otro worktree.')
}

$cli = $null
if ($Runner -ne 'batchmode') { $cli = Get-UnityCli }
if ($Runner -eq 'cli' -and -not $cli) {
    Stop-WithCode 4 'ERROR: no se encuentra la Unity CLI (comando "unity"). Instalacion: paso s04 del plan (la hace el propietario).'
}

$exitCode = $null
$via = $null
if ($cli -and $Runner -ne 'batchmode') {
    $via = 'unity test'
    Write-Output "Ejecutando con la Unity CLI (resultados: $ResultsFile, log: $LogFile) ..."
    $cliArgs = @('test', $info.Root, '--mode', $Mode, '--output', $ResultsFile, '--timeout', "$timeoutSeconds", '--architecture', $Architecture)
    if ($Filter) { $cliArgs += @('--filter', $Filter) }
    $cliArgs += @('--', '-nographics', '-logFile', $LogFile)
    $cliOut = & $cli @cliArgs 2>&1 | ForEach-Object { "$_" }
    $exitCode = $LASTEXITCODE
    if (-not (Test-Path -LiteralPath $LogFile -PathType Leaf) -and -not (Test-Path -LiteralPath $ResultsFile -PathType Leaf)) {
        Write-Output "La Unity CLI no llego a lanzar el Editor (salida $exitCode):"
        $cliOut | Select-Object -Last 10 | ForEach-Object { Write-Output ('  ' + $_) }
        if ($Runner -eq 'cli') { Stop-WithCode 6 'SIN VEREDICTO: revisa la salida anterior de la Unity CLI.' }
        $exitCode = $null
    }
}

if ($null -eq $exitCode) {
    $exe = Find-UnityEditorExe -Version $info.Version -Explicit $UnityExe -Cli $cli
    if (-not $exe) {
        Stop-WithCode 4 ("ERROR: no se encuentra Unity.exe $($info.Version). Instalacion: paso s18 del plan (la hace el propietario)." +
            [Environment]::NewLine + 'Si esta en otra carpeta, pasa -UnityExe <ruta> o define UNITY_EDITOR_PATH.')
    }
    $via = 'Unity.exe'
    Write-Output "Ejecutando con $exe (resultados: $ResultsFile, log: $LogFile) ..."
    $unityArgs = @('-batchmode', '-nographics', '-accept-apiupdate', '-projectPath', $info.Root, '-runTests',
        '-testPlatform', $Mode, '-testResults', $ResultsFile, '-logFile', $LogFile)
    if ($Filter) { $unityArgs += @('-testFilter', $Filter) }
    $exitCode = Invoke-ProcessWithTimeout -FilePath $exe -Arguments $unityArgs -TimeoutSec $timeoutSeconds
    if ($exitCode -eq -999) {
        Write-CompileErrors -Lines (Get-CompileErrorLines -Path $LogFile) -Max $maxLines
        Stop-WithCode 6 "SIN VEREDICTO: el Editor supero el limite de $timeoutSeconds s y se ha cerrado. Log: $LogFile"
    }
}

$results = Read-NUnitResults -Path $ResultsFile
if (-not $results) {
    $compileErrors = Get-CompileErrorLines -Path $LogFile
    Write-CompileErrors -Lines $compileErrors -Max $maxLines
    if ($compileErrors.Count -eq 0) { Write-LogHints -Path $LogFile }
    Stop-WithCode 6 "SIN VEREDICTO: $via salio con $exitCode y no escribio resultados. Lee $LogFile; no reintentes a ciegas."
}

Write-Results -Results $results -Max $maxLines
Write-Output "Resultados: $ResultsFile - log: $LogFile"

if ($results.Failed -gt 0) { exit 8 }
if ($exitCode -eq 8) { exit 8 }
if ($results.Total -eq 0 -and -not $AllowEmpty) {
    Stop-WithCode 6 'SIN VEREDICTO: no se ejecuto ningun test (filtro sin coincidencias o ensamblado de tests sin compilar). Usa -AllowEmpty solo si es lo esperado.'
}
if ($exitCode -ne 0 -and $via -eq 'unity test') {
    Write-LogHints -Path $LogFile
    Stop-WithCode 6 "SIN VEREDICTO: unity test salio con $exitCode aunque el XML no tiene fallos. Lee $LogFile."
}
exit 0
