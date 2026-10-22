#Requires -Version 7.2
<#
.SYNOPSIS
    Compuerta del manifiesto fusionado de Amazon Fill (GDD 08 sec. 12.1 etapa 4 y sec. 13.1 #1 y #4; bloque B10 de la tanda 5).

.DESCRIPTION
    Lee el AndroidManifest.xml fusionado de un .aab (bundletool dump manifest) o de un .apk
    (aapt2 dump xmltree) y comprueba:

      1. targetSdk == 36 y minSdk == 26 (CLAUDE.md, stack fijado; 08 sec. 12.1: la compuerta sube
         a 37 antes del 31-08-2027, parametro -TargetSdk).
      2. Permisos (uses-permission y uses-permission-sdk-23) dentro de la lista blanca:
         android.permission.INTERNET, android.permission.VIBRATE y android.permission.ACCESS_NETWORK_STATE, el permiso propio
         <package>.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION que declara androidx.core y com.android.vending.BILLING
         (Play Billing Library 9.0.0 del plugin propio AfBilling; aprobado por el dueno en D-040, 2026-10-22). Cualquier otro falla (CLAUDE.md,
         regla dura 4: ningun permiso Android nuevo sin el dueno; -AllowPermission anade los aprobados).
         INTERNET lo aprobo el dueno en D-036 (2026-10-20; lo anade Unity con ForceInternetPermission en Auto).
         ACCESS_NETWORK_STATE lo aprobo el dueno en D-037 (2026-10-21; lo anade Unity al leer
         Application.internetReachability para el comun net de la analitica, 08 sec. 8.1).
         android.permission.WAKE_LOCK y com.google.android.finsky.permission.BIND_GET_INSTALL_REFERRER_SERVICE los
         aprobo el dueno en D-039 y su anadido del 2026-10-21 (los trae Firebase Analytics, play-services-measurement;
         el segundo es el Install Referrer de Play). AD_ID, ACCESS_ADSERVICES_ATTRIBUTION, ACCESS_ADSERVICES_AD_ID y
         READ_GSERVICES se quitan en Assets/Plugins/Android/LauncherManifest.xml y siguen fuera de la lista blanca.
         android.permission.POST_NOTIFICATIONS lo aprobo el dueno en D-033 (Q-M5) y su anadido del 2026-10-21,
         con el paquete com.unity.mobile.notifications 2.4.3 (lo declara su manifiesto, Android 13+). Las alarmas
         son inexactas: SCHEDULE_EXACT_ALARM, USE_EXACT_ALARM, RECEIVE_BOOT_COMPLETED y
         REQUEST_IGNORE_BATTERY_OPTIMIZATIONS (los anade el paquete si se tocan sus ajustes) siguen fuera de la
         lista blanca y fallan con una pista.
      3. Play Billing: si el manifiesto lleva la meta-data com.google.android.play.billingclient.version
         o el permiso BILLING, la version mayor debe ser >= 8 (08 sec. 13.1 #4). Sin billing se anota
         (un build sin el plugin de Play Billing); con -RequireBilling, falla: desde D-040 el build de release
         debe pasarlo con -RequireBilling.
      4. android:appCategory = game (valor 0 compilado) en <application> (08 sec. 13.1 #4).
      5. applicationId (CLAUDE.md, regla dura 6; 08 sec. 13.1 #1):
           -Mode release: falla si el package es el provisional com.amazonfill.dev o lleva "amazon"
                             (el ID publico nunca lleva esa cadena), y si la app es debuggable.
           -Mode dev:     falla si el package NO es com.amazonfill.dev (los builds de prueba usan
                             siempre el provisional).
      6. Retroceso predictivo (08 sec. 13.1 #4): informa de android:enableOnBackInvokedCallback;
         con -RequirePredictiveBack falla si no es true.
      7. IL2CPP ARM64 (.claude/rules/ci.md): las bibliotecas nativas del artefacto (lib/<abi>/ en el APK,
         base/lib/<abi>/ en el AAB) son solo de arm64-v8a y entre ellas esta libil2cpp.so.

    Herramientas: para .aab, bundletool (-Bundletool, BUNDLETOOL_JAR o el bundletool-all-*.jar del
    modulo Android de un Editor de Unity) y java (JAVA_HOME o PATH); para .apk, aapt2 (-Aapt2, AAPT2,
    build-tools de ANDROID_HOME/ANDROID_SDK_ROOT o del SDK del modulo Android de un Editor de Unity).
    Funciona en Windows (PowerShell 7) y en el runner Ubuntu de GitHub Actions (pwsh).

    Codigos de salida:
      0  todos los artefactos cumplen
      1  algun artefacto no cumple (o el autotest fallo)
      2  argumentos incorrectos, archivo inexistente o manifiesto ilegible
      3  falta una herramienta (bundletool, java o aapt2) o la herramienta fallo

.PARAMETER Path
    Archivo .aab o .apk, carpeta (busca *.aab y *.apk dentro, no recursivo) o comodin.
    "--self-test" equivale a -SelfTest.

.PARAMETER Mode
    release (por defecto; falla cerrado) o dev.

.EXAMPLE
    pwsh -NoProfile -File ci/check-manifest.ps1 build/Android/release.aab -Mode release
.EXAMPLE
    pwsh -NoProfile -File ci/check-manifest.ps1 Builds/Android/dev.apk -Mode dev
.EXAMPLE
    pwsh -NoProfile -File ci/check-manifest.ps1 --self-test
#>
param(
    [string]$Path = '',
    [string]$Mode = 'release',
    [string]$TargetSdk = '36',
    [string]$MinSdk = '26',
    [string[]]$AllowPermission = @(),
    [string]$Bundletool = $env:BUNDLETOOL_JAR,
    [string]$Aapt2 = $env:AAPT2,
    [switch]$RequireBilling,
    [switch]$RequirePredictiveBack,
    [switch]$SelfTest
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$AndroidNs = 'http://schemas.android.com/apk/res/android'
$ProvisionalId = 'com.amazonfill.dev'
$BillingMetaName = 'com.google.android.play.billingclient.version'
$BillingPermission = 'com.android.vending.BILLING'   # aprobado: D-040 (Play Billing Library 9.0.0, plugin propio; 2026-10-22)
$InternetPermission = 'android.permission.INTERNET'          # aprobado: D-036
$NetworkStatePermission = 'android.permission.ACCESS_NETWORK_STATE'   # aprobado: D-037 (comun net, 08 sec. 8.1)
# Aprobado en D-033 (Q-M5) y su anadido del 2026-10-21, con com.unity.mobile.notifications 2.4.3. Solo ese: las
# alarmas son inexactas, asi que los permisos de alarma exacta, de reinicio y de bateria siguen fuera (con pista).
$NotificationsPermission = 'android.permission.POST_NOTIFICATIONS'
$NotificationsNotApproved = @('android.permission.SCHEDULE_EXACT_ALARM', 'android.permission.USE_EXACT_ALARM',
    'android.permission.RECEIVE_BOOT_COMPLETED', 'android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS')
$WakeLockPermission = 'android.permission.WAKE_LOCK'   # aprobado: D-039 (Firebase Analytics)
$InstallReferrerPermission = 'com.google.android.finsky.permission.BIND_GET_INSTALL_REFERRER_SERVICE'   # aprobado: D-039, anadido del 2026-10-21 (Install Referrer de Play)
$BasePermissions = @($InternetPermission, 'android.permission.VIBRATE', $NetworkStatePermission, $WakeLockPermission, $InstallReferrerPermission, $NotificationsPermission, $BillingPermission)

function Stop-WithCode {
    param([int]$Code, [string]$Message)
    [Console]::Error.WriteLine($Message)
    exit $Code
}

function New-Model {
    return [ordered]@{
        Package       = ''
        MinSdk        = ''
        TargetSdk     = ''
        Permissions   = [System.Collections.Generic.List[string]]::new()
        AppCategory   = ''
        Debuggable    = ''
        BillingVer    = ''
        BackCallback  = ''
        HasApplication = $false
        Abis           = $null    # ABI con bibliotecas nativas; $null = sin comprobar (autotest de solo manifiesto)
        HasIl2cpp      = $false
    }
}

# Manifiesto en XML (salida de "bundletool dump manifest").
function ConvertFrom-ManifestXml([string]$Text) {
    $doc = [System.Xml.XmlDocument]::new()
    $doc.PreserveWhitespace = $false
    $doc.LoadXml($Text.Trim())
    $root = $doc.DocumentElement
    if (-not $root -or $root.LocalName -ne 'manifest') { throw 'el XML no tiene <manifest> como raiz' }
    $m = New-Model
    $m.Package = $root.GetAttribute('package')
    foreach ($node in $root.ChildNodes) {
        if ($node.NodeType -ne [System.Xml.XmlNodeType]::Element) { continue }
        switch ($node.LocalName) {
            'uses-sdk' {
                $m.MinSdk = $node.GetAttribute('minSdkVersion', $AndroidNs)
                $m.TargetSdk = $node.GetAttribute('targetSdkVersion', $AndroidNs)
            }
            { $_ -in @('uses-permission', 'uses-permission-sdk-23') } {
                $m.Permissions.Add($node.GetAttribute('name', $AndroidNs))
            }
            'application' {
                $m.HasApplication = $true
                $m.AppCategory = $node.GetAttribute('appCategory', $AndroidNs)
                $m.Debuggable = $node.GetAttribute('debuggable', $AndroidNs)
                $m.BackCallback = $node.GetAttribute('enableOnBackInvokedCallback', $AndroidNs)
                foreach ($child in $node.ChildNodes) {
                    if ($child.NodeType -eq [System.Xml.XmlNodeType]::Element -and $child.LocalName -eq 'meta-data' -and
                        $child.GetAttribute('name', $AndroidNs) -eq $BillingMetaName) {
                        $m.BillingVer = $child.GetAttribute('value', $AndroidNs)
                    }
                }
            }
        }
    }
    return $m
}

# Manifiesto en el formato de "aapt2 dump xmltree --file AndroidManifest.xml" (APK).
# Cada linea "E: <elemento>" abre un elemento y las "A: <atributo>=<valor>" con dos espacios mas son suyas.
function ConvertFrom-Aapt2Tree([string]$Text) {
    $m = New-Model
    $stack = [System.Collections.Generic.List[object]]::new()   # pares (indentacion, elemento)
    $meta = $null
    foreach ($raw in ($Text -split "`r?`n")) {
        if (-not $raw.Trim()) { continue }
        $indent = $raw.Length - $raw.TrimStart().Length
        $line = $raw.Trim()
        if ($line -match '^E: (?<name>[\w\-\.]+)') {
            while ($stack.Count -gt 0 -and $stack[$stack.Count - 1].Indent -ge $indent) { $stack.RemoveAt($stack.Count - 1) }
            $el = [pscustomobject]@{ Indent = $indent; Name = $Matches['name']; Attrs = @{} }
            $stack.Add($el)
            switch ($el.Name) {
                { $_ -in @('uses-permission', 'uses-permission-sdk-23') } { $m.Permissions.Add('') }
                'application' { $m.HasApplication = $true }
            }
            continue
        }
        if ($line -notmatch '^A: (?:\S*?android:)?(?<name>[A-Za-z][\w\-]*)(?:\(0x[0-9a-fA-F]+\))?=(?<val>.*)$') { continue }
        $name = $Matches['name']
        $val = $Matches['val']
        if ($val.StartsWith('"')) {
            $end = $val.IndexOf('"', 1)
            $val = if ($end -gt 0) { $val.Substring(1, $end - 1) } else { $val.Substring(1) }
        }
        else {
            $val = ($val -split '\s', 2)[0]
            if ($val -match '^0x[0-9a-fA-F]+$') { $val = [string][Convert]::ToInt64($val.Substring(2), 16) }
        }
        # El dueno del atributo es el elemento abierto mas profundo con menos indentacion.
        $owner = $null
        for ($i = $stack.Count - 1; $i -ge 0; $i--) { if ($stack[$i].Indent -lt $indent) { $owner = $stack[$i]; break } }
        if (-not $owner) { continue }
        $owner.Attrs[$name] = $val
        switch ($owner.Name) {
            'manifest' { if ($name -eq 'package') { $m.Package = $val } }
            'uses-sdk' {
                if ($name -eq 'minSdkVersion') { $m.MinSdk = $val }
                if ($name -eq 'targetSdkVersion') { $m.TargetSdk = $val }
            }
            { $_ -in @('uses-permission', 'uses-permission-sdk-23') } {
                if ($name -eq 'name') { $m.Permissions[$m.Permissions.Count - 1] = $val }
            }
            'application' {
                if ($name -eq 'appCategory') { $m.AppCategory = $val }
                if ($name -eq 'debuggable') { $m.Debuggable = $val }
                if ($name -eq 'enableOnBackInvokedCallback') { $m.BackCallback = $val }
            }
            'meta-data' {
                # Solo la meta-data hija directa de <application>.
                if ($stack.Count -ge 2 -and $stack[$stack.Count - 2].Name -eq 'application') {
                    $owner.Attrs[$name] = $val
                    if ($owner.Attrs.ContainsKey('name') -and $owner.Attrs['name'] -eq $BillingMetaName -and $owner.Attrs.ContainsKey('value')) {
                        $m.BillingVer = $owner.Attrs['value']
                    }
                }
            }
        }
    }
    if (-not $m.Package) { throw 'la salida de aapt2 no tiene package en <manifest>' }
    return $m
}

# ABI y libil2cpp.so a partir de las entradas del ZIP (APK: lib/<abi>/; AAB: <modulo>/lib/<abi>/).
function Set-NativeInfo($M, [string[]]$Entries) {
    $abis = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
    $il2cpp = $false
    foreach ($e in $Entries) {
        if ($e -notmatch '^(?:[^/]+/)?lib/(?<abi>[^/]+)/(?<so>[^/]+\.so)$') { continue }
        [void]$abis.Add($Matches['abi'])
        if ($Matches['abi'] -eq 'arm64-v8a' -and $Matches['so'] -eq 'libil2cpp.so') { $il2cpp = $true }
    }
    $M.Abis = [string[]]@($abis)
    $M.HasIl2cpp = $il2cpp
}

function Get-ZipEntryNames([string]$File) {
    $zip = [System.IO.Compression.ZipFile]::OpenRead($File)
    try { return [string[]]@($zip.Entries | ForEach-Object FullName) }
    finally { $zip.Dispose() }
}

function Test-True([string]$Value) { return $Value -in @('true', '1', '-1') }

# Devuelve @{ Failures = [...]; Notes = [...] } para un modelo y un perfil.
function Test-Manifest($M, [string]$Prof, [int]$Target, [int]$Min, [string[]]$ExtraAllowed, [bool]$NeedBilling, [bool]$NeedBack) {
    $fail = [System.Collections.Generic.List[string]]::new()
    $note = [System.Collections.Generic.List[string]]::new()

    if ("$($M.TargetSdk)" -ne "$Target") { $fail.Add("targetSdk=$($M.TargetSdk) (esperado $Target)") }
    if ("$($M.MinSdk)" -ne "$Min") { $fail.Add("minSdk=$($M.MinSdk) (esperado $Min)") }

    $pkg = "$($M.Package)"
    $billingPresent = [bool]$M.BillingVer -or $M.Permissions.Contains($BillingPermission)
    $allowed = [System.Collections.Generic.HashSet[string]]::new([string[]]($BasePermissions + $ExtraAllowed))
    if ($pkg) { [void]$allowed.Add("$pkg.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION") }
    $seen = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($p in $M.Permissions) {
        if (-not $seen.Add($p)) { continue }
        if ($allowed.Contains($p)) { continue }
        if ($p -in $NotificationsNotApproved) {
            $fail.Add("permiso fuera de la lista blanca: '$p' (D-033 Q-M5 solo aprueba POST_NOTIFICATIONS con alarmas inexactas: deja en Mobile Notifications 'Schedule at exact time' y 'Reschedule on Device Restart' apagados; regla dura 4)")
        }
        else { $fail.Add("permiso fuera de la lista blanca: '$p' (regla dura 4: lo aprueba el dueno)") }
    }

    if ($billingPresent) {
        $major = -1
        if ($M.BillingVer -match '^(\d+)') { $major = [int]$Matches[1] }
        if ($major -lt 8) { $fail.Add("Play Billing '$($M.BillingVer)' (version mayor >= 8 exigida; sin meta-data $BillingMetaName se toma como desconocida)") }
        else { $note.Add("Play Billing $($M.BillingVer).") }
    }
    elseif ($NeedBilling) { $fail.Add('sin Play Billing (-RequireBilling)') }
    else { $note.Add('NOTA: sin Play Billing en el manifiesto (build sin el plugin de Play Billing; en release, usar -RequireBilling: D-040).') }

    if (-not $M.HasApplication) { $fail.Add('sin <application>') }
    elseif ("$($M.AppCategory)".ToLowerInvariant() -notin @('0', 'game')) { $fail.Add("appCategory='$($M.AppCategory)' (esperado game)") }

    switch ($Prof) {
        'release' {
            if ($pkg -eq $ProvisionalId) { $fail.Add("applicationId provisional $ProvisionalId en un build de release (regla dura 6)") }
            elseif ($pkg.ToLowerInvariant().Contains('amazon')) { $fail.Add("applicationId '$pkg' lleva 'amazon' (08 sec. 13.1 #1)") }
            if (Test-True $M.Debuggable) { $fail.Add('android:debuggable=true en un build de release') }
        }
        'dev' {
            if ($pkg -ne $ProvisionalId) { $fail.Add("applicationId '$pkg' en un build de prueba (esperado $ProvisionalId; regla dura 6)") }
        }
    }

    if ($null -ne $M.Abis) {
        $abiText = if ($M.Abis.Count -gt 0) { $M.Abis -join ',' } else { '(ninguna)' }
        if ($M.Abis.Count -ne 1 -or $M.Abis[0] -ne 'arm64-v8a') { $fail.Add("ABI nativas: $abiText (esperado solo arm64-v8a)") }
        if (-not $M.HasIl2cpp) { $fail.Add('sin lib/arm64-v8a/libil2cpp.so (el backend debe ser IL2CPP)') }
    }

    if (Test-True $M.BackCallback) { $note.Add('Retroceso predictivo: enableOnBackInvokedCallback=true.') }
    elseif ($NeedBack) { $fail.Add("enableOnBackInvokedCallback='$($M.BackCallback)' (-RequirePredictiveBack)") }
    else { $note.Add("NOTA: retroceso predictivo desactivado (enableOnBackInvokedCallback='$($M.BackCallback)'; 08 sec. 13.1 #4 pendiente).") }

    return @{ Failures = $fail; Notes = $note }
}

function Invoke-SelfTest {
    $okXml = @'
<manifest xmlns:android="http://schemas.android.com/apk/res/android" package="com.estudio.juego">
  <uses-sdk android:minSdkVersion="26" android:targetSdkVersion="36"/>
  <uses-permission android:name="android.permission.INTERNET"/>
  <uses-permission android:name="android.permission.VIBRATE"/>
  <uses-permission android:name="com.android.vending.BILLING"/>
  <permission android:name="com.estudio.juego.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION"/>
  <uses-permission android:name="com.estudio.juego.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION"/>
  <application android:appCategory="0" android:enableOnBackInvokedCallback="true">
    <meta-data android:name="com.google.android.play.billingclient.version" android:value="8.0.0"/>
    <activity android:name="x"><meta-data android:name="com.google.android.play.billingclient.version" android:value="1.0"/></activity>
  </application>
</manifest>
'@
    $okTree = @'
N: android=http://schemas.android.com/apk/res/android (line=2)
  E: manifest (line=2)
    A: http://schemas.android.com/apk/res/android:versionCode(0x0101021b)=1
    A: package="com.amazonfill.dev" (Raw: "com.amazonfill.dev")
      E: uses-sdk (line=8)
        A: http://schemas.android.com/apk/res/android:minSdkVersion(0x0101020c)=26
        A: http://schemas.android.com/apk/res/android:targetSdkVersion(0x01010270)=36
      E: uses-permission (line=19)
        A: http://schemas.android.com/apk/res/android:name(0x01010003)="android.permission.INTERNET" (Raw: "android.permission.INTERNET")
      E: uses-feature (line=21)
        A: http://schemas.android.com/apk/res/android:glEsVersion(0x01010281)=0x00030000
      E: uses-permission (line=35)
        A: http://schemas.android.com/apk/res/android:name(0x01010003)="android.permission.VIBRATE" (Raw: "android.permission.VIBRATE")
      E: uses-permission (line=37)
        A: http://schemas.android.com/apk/res/android:name(0x01010003)="android.permission.ACCESS_NETWORK_STATE" (Raw: "android.permission.ACCESS_NETWORK_STATE")
      E: uses-permission (line=41)
        A: http://schemas.android.com/apk/res/android:name(0x01010003)="com.amazonfill.dev.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION" (Raw: "x")
      E: application (line=43)
        A: http://schemas.android.com/apk/res/android:debuggable(0x0101000f)=true
        A: http://schemas.android.com/apk/res/android:appCategory(0x01010545)=0
        A: http://schemas.android.com/apk/res/android:enableOnBackInvokedCallback(0x0101066c)=false
          E: meta-data (line=51)
            A: http://schemas.android.com/apk/res/android:name(0x01010003)="unity.splash-mode" (Raw: "unity.splash-mode")
            A: http://schemas.android.com/apk/res/android:value(0x01010024)=0
'@
    $cases = @(
        @{ Name = 'xml: release correcto';                       Fmt = 'xml'; Text = $okXml; Prof = 'release'; Fails = 0 }
        @{ Name = 'xml: ACCESS_NETWORK_STATE aprobado (D-037)';  Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', $NetworkStatePermission); Prof = 'release'; Fails = 0 }
        @{ Name = 'xml: ACCESS_NETWORK_STATE como sdk-23';       Fmt = 'xml'; Text = $okXml.Replace('<uses-permission android:name="android.permission.VIBRATE"/>', '<uses-permission-sdk-23 android:name="android.permission.ACCESS_NETWORK_STATE"/>'); Prof = 'release'; Fails = 0 }
        @{ Name = 'xml: WAKE_LOCK aprobado (D-039)';             Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', $WakeLockPermission); Prof = 'release'; Fails = 0 }
        @{ Name = 'xml: INSTALL_REFERRER aprobado (D-039)';      Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', $InstallReferrerPermission); Prof = 'release'; Fails = 0 }
        @{ Name = 'xml: BILLING aprobado (D-040) con PBL 9';     Fmt = 'xml'; Text = $okXml.Replace('android:value="8.0.0"', 'android:value="9.0.0"'); Prof = 'release'; Fails = 0; Billing = $true }
        @{ Name = 'xml: BILLING como sdk-23 (D-040)';            Fmt = 'xml'; Text = $okXml.Replace('<uses-permission android:name="com.android.vending.BILLING"/>', '<uses-permission-sdk-23 android:name="com.android.vending.BILLING"/>'); Prof = 'release'; Fails = 0 }
        @{ Name = 'xml: otro permiso de la tienda sigue fuera';  Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', 'com.android.vending.CHECK_LICENSE'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: AD_ID sigue fuera (D-039)';              Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', 'com.google.android.gms.permission.AD_ID'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: ACCESS_ADSERVICES_AD_ID sigue fuera';    Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', 'android.permission.ACCESS_ADSERVICES_AD_ID'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: ACCESS_ADSERVICES_ATTRIBUTION fuera';    Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', 'android.permission.ACCESS_ADSERVICES_ATTRIBUTION'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: READ_GSERVICES sigue fuera';             Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', 'com.google.android.providers.gsf.permission.READ_GSERVICES'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: ACCESS_WIFI_STATE sigue fuera';          Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', 'android.permission.ACCESS_WIFI_STATE'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: targetSdk 35';                           Fmt = 'xml'; Text = $okXml.Replace('targetSdkVersion="36"', 'targetSdkVersion="35"'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: minSdk 24';                              Fmt = 'xml'; Text = $okXml.Replace('minSdkVersion="26"', 'minSdkVersion="24"'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: permiso CAMERA';                         Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', 'android.permission.CAMERA'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: CAMERA con -AllowPermission';            Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', 'android.permission.CAMERA'); Prof = 'release'; Fails = 0; Allow = @('android.permission.CAMERA') }
        @{ Name = 'xml: POST_NOTIFICATIONS aprobado (D-033 Q-M5)'; Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', 'android.permission.POST_NOTIFICATIONS'); Prof = 'release'; Fails = 0 }
        @{ Name = 'xml: POST_NOTIFICATIONS con -AllowPermission'; Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', 'android.permission.POST_NOTIFICATIONS'); Prof = 'release'; Fails = 0; Allow = @('android.permission.POST_NOTIFICATIONS') }
        @{ Name = 'xml: SCHEDULE_EXACT_ALARM sigue fuera';       Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', 'android.permission.SCHEDULE_EXACT_ALARM'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: USE_EXACT_ALARM sigue fuera';            Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', 'android.permission.USE_EXACT_ALARM'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: RECEIVE_BOOT_COMPLETED sigue fuera';     Fmt = 'xml'; Text = $okXml.Replace('android.permission.VIBRATE', 'android.permission.RECEIVE_BOOT_COMPLETED'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: IGNORE_BATTERY_OPTIMIZATIONS sdk-23 fuera'; Fmt = 'xml'; Text = $okXml.Replace('<uses-permission android:name="android.permission.VIBRATE"/>', '<uses-permission-sdk-23 android:name="android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS"/>'); Prof = 'release'; Fails = 1 }
        @{ Name = 'aapt2: POST_NOTIFICATIONS en build de prueba'; Fmt = 'tree'; Text = $okTree.Replace('"android.permission.VIBRATE"', '"android.permission.POST_NOTIFICATIONS"'); Prof = 'dev'; Fails = 0 }
        @{ Name = 'xml: billing 7';                              Fmt = 'xml'; Text = $okXml.Replace('android:value="8.0.0"', 'android:value="7.1.1"'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: BILLING sin meta-data';                  Fmt = 'xml'; Text = $okXml.Replace('com.google.android.play.billingclient.version" android:value="8.0.0"', 'otra" android:value="8.0.0"'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: appCategory social';                     Fmt = 'xml'; Text = $okXml.Replace('android:appCategory="0"', 'android:appCategory="4"'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: appCategory ausente';                    Fmt = 'xml'; Text = $okXml.Replace('android:appCategory="0" ', ''); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: id provisional en release';              Fmt = 'xml'; Text = $okXml.Replace('com.estudio.juego', $ProvisionalId); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: id con amazon en release';               Fmt = 'xml'; Text = $okXml.Replace('com.estudio.juego', 'com.amazonfill.release'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: id definitivo en build de prueba';       Fmt = 'xml'; Text = $okXml; Prof = 'dev'; Fails = 1 }
        @{ Name = 'xml: debuggable en release';                  Fmt = 'xml'; Text = $okXml.Replace('android:appCategory="0"', 'android:appCategory="0" android:debuggable="true"'); Prof = 'release'; Fails = 1 }
        @{ Name = 'xml: sin billing con -RequireBilling';        Fmt = 'xml'; Text = ($okXml -replace '(?s)\s*<uses-permission android:name="com.android.vending.BILLING"/>', '' -replace '(?s)<meta-data android:name="com.google.android.play.billingclient.version" android:value="8.0.0"/>', ''); Prof = 'release'; Fails = 1; Billing = $true }
        @{ Name = 'xml: sin back con -RequirePredictiveBack';    Fmt = 'xml'; Text = $okXml.Replace('enableOnBackInvokedCallback="true"', 'enableOnBackInvokedCallback="false"'); Prof = 'release'; Fails = 1; Back = $true }
        @{ Name = 'aapt2: build de prueba correcto';             Fmt = 'tree'; Text = $okTree; Prof = 'dev'; Fails = 0 }
        @{ Name = 'aapt2: sin ACCESS_NETWORK_STATE tambien vale'; Fmt = 'tree'; Text = ((($okTree -split "`n") | Where-Object { $_ -notmatch 'line=37|ACCESS_NETWORK_STATE' }) -join "`n"); Prof = 'dev'; Fails = 0 }
        @{ Name = 'aapt2: id provisional y debuggable en release'; Fmt = 'tree'; Text = $okTree; Prof = 'release'; Fails = 2 }
        @{ Name = 'aapt2: WAKE_LOCK e INSTALL_REFERRER (D-039)'; Fmt = 'tree'; Text = $okTree.Replace('"android.permission.VIBRATE"', '"' + $WakeLockPermission + '"').Replace('"android.permission.ACCESS_NETWORK_STATE"', '"' + $InstallReferrerPermission + '"'); Prof = 'dev'; Fails = 0 }
        @{ Name = 'aapt2: BILLING sin version de PBL (D-040)';   Fmt = 'tree'; Text = $okTree.Replace('"android.permission.VIBRATE"', '"' + $BillingPermission + '"'); Prof = 'dev'; Fails = 1 }
        @{ Name = 'aapt2: permiso RECORD_AUDIO';                 Fmt = 'tree'; Text = $okTree.Replace('"android.permission.VIBRATE"', '"android.permission.RECORD_AUDIO"'); Prof = 'dev'; Fails = 1 }
        @{ Name = 'aapt2: targetSdk 37 con -TargetSdk 36';       Fmt = 'tree'; Text = $okTree.Replace('(0x01010270)=36', '(0x01010270)=37'); Prof = 'dev'; Fails = 1 }
        @{ Name = 'aapt2: appCategory 3';                        Fmt = 'tree'; Text = $okTree.Replace('(0x01010545)=0', '(0x01010545)=3'); Prof = 'dev'; Fails = 1 }
    )
    $arm = @('base/lib/arm64-v8a/libil2cpp.so', 'base/lib/arm64-v8a/libunity.so', 'base/dex/classes.dex')
    $cases += @(
        @{ Name = 'zip: AAB IL2CPP arm64';                       Fmt = 'xml'; Text = $okXml; Prof = 'release'; Fails = 0; Zip = $arm }
        @{ Name = 'zip: APK IL2CPP arm64';                       Fmt = 'tree'; Text = $okTree; Prof = 'dev'; Fails = 0; Zip = @('lib/arm64-v8a/libil2cpp.so', 'lib/arm64-v8a/libmain.so') }
        @{ Name = 'zip: tambien armeabi-v7a';                    Fmt = 'xml'; Text = $okXml; Prof = 'release'; Fails = 1; Zip = ($arm + 'base/lib/armeabi-v7a/libil2cpp.so') }
        @{ Name = 'zip: Mono (sin libil2cpp.so)';                Fmt = 'xml'; Text = $okXml; Prof = 'release'; Fails = 1; Zip = @('base/lib/arm64-v8a/libmonobdwgc-2.0.so') }
        @{ Name = 'zip: sin codigo nativo';                      Fmt = 'xml'; Text = $okXml; Prof = 'release'; Fails = 2; Zip = @('base/dex/classes.dex') }
    )
    $bad = 0
    foreach ($c in $cases) {
        $model = if ($c.Fmt -eq 'xml') { ConvertFrom-ManifestXml $c.Text } else { ConvertFrom-Aapt2Tree $c.Text }
        if ($c.ContainsKey('Zip')) { Set-NativeInfo $model ([string[]]$c.Zip) }
        $allow = if ($c.ContainsKey('Allow')) { [string[]]$c.Allow } else { [string[]]@() }
        $r = Test-Manifest $model $c.Prof 36 26 $allow ([bool]$c['Billing']) ([bool]$c['Back'])
        $n = $r.Failures.Count
        if ($n -eq $c.Fails) { Write-Output "[OK]    $($c.Name) ($n fallos)" }
        else {
            $bad++
            Write-Output "[FALLA] $($c.Name): $n fallos, esperados $($c.Fails): $($r.Failures -join '; ')"
        }
    }
    # INTERNET (D-036) y ACCESS_NETWORK_STATE (D-037) estan aprobados: ni fallan ni dejan nota de pendiente.
    $r = Test-Manifest (ConvertFrom-Aapt2Tree $okTree) 'dev' 36 26 @() $false $false
    if ($r.Failures.Count -ne 0 -or ($r.Notes | Where-Object { $_ -like '*INTERNET*' -or $_ -like '*ACCESS_NETWORK_STATE*' })) { $bad++; Write-Output "[FALLA] INTERNET o ACCESS_NETWORK_STATE aprobados dan fallos o notas: $((@($r.Failures) + @($r.Notes)) -join '; ')" }
    elseif (-not ((ConvertFrom-Aapt2Tree $okTree).Permissions -contains $NetworkStatePermission)) { $bad++; Write-Output '[FALLA] el caso aapt2 no lleva ACCESS_NETWORK_STATE' }
    else { Write-Output '[OK]    INTERNET (D-036) y ACCESS_NETWORK_STATE (D-037) aprobados: sin fallos ni nota' }
    # Las alarmas exactas y el reinicio fallan con la pista de D-033 Q-M5 (alarmas inexactas).
    $r = Test-Manifest (ConvertFrom-ManifestXml $okXml.Replace('android.permission.VIBRATE', 'android.permission.SCHEDULE_EXACT_ALARM')) 'release' 36 26 @() $false $false
    if (-not ($r.Failures | Where-Object { $_ -like '*SCHEDULE_EXACT_ALARM*Q-M5*' })) { $bad++; Write-Output '[FALLA] SCHEDULE_EXACT_ALARM sin la pista de D-033 Q-M5' }
    else { Write-Output '[OK]    SCHEDULE_EXACT_ALARM falla con la pista de D-033 Q-M5' }
    # BILLING (D-040) esta en la lista blanca: sin la meta-data de version solo falla la version de Play Billing, no el permiso.
    $r = Test-Manifest (ConvertFrom-Aapt2Tree $okTree.Replace('"android.permission.VIBRATE"', '"' + $BillingPermission + '"')) 'dev' 36 26 @() $false $false
    if (($r.Failures | Where-Object { $_ -like '*lista blanca*' }) -or -not ($r.Failures | Where-Object { $_ -like 'Play Billing*' })) { $bad++; Write-Output "[FALLA] BILLING (D-040) fuera de la lista blanca o sin exigir la version de Play Billing: $($r.Failures -join '; ')" }
    else { Write-Output '[OK]    BILLING (D-040) en la lista blanca; la version de Play Billing se sigue exigiendo' }
    $total = $cases.Count + 3
    if ($bad -gt 0) { Stop-WithCode 1 "AUTOTEST: $bad de $total casos fallan." }
    Write-Output "AUTOTEST OK: $total casos."
    exit 0
}

# --- Herramientas -------------------------------------------------------------------------------
function Get-UnityAndroidPlayerDirs {
    $roots = @()
    if ($env:UNITY_EDITOR_PATH) { $roots += (Split-Path -Parent $env:UNITY_EDITOR_PATH) }
    if ($IsWindows) {
        $roots += @(Get-ChildItem -Path (Join-Path $env:ProgramFiles 'Unity/Hub/Editor/*/Editor') -Directory -ErrorAction SilentlyContinue | ForEach-Object FullName)
        if ($env:USERPROFILE) {
            $roots += @(Get-ChildItem -Path (Join-Path $env:USERPROFILE 'Unity/Hub/Editor/*/Editor') -Directory -ErrorAction SilentlyContinue | ForEach-Object FullName)
        }
    }
    return @($roots | ForEach-Object { Join-Path $_ 'Data/PlaybackEngines/AndroidPlayer' } | Where-Object { Test-Path -LiteralPath $_ -PathType Container })
}

function Find-Aapt2 {
    if ($Aapt2) { if (Test-Path -LiteralPath $Aapt2 -PathType Leaf) { return $Aapt2 } else { return $null } }
    $exe = if ($IsWindows) { 'aapt2.exe' } else { 'aapt2' }
    $sdks = @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT | Where-Object { $_ })
    $sdks += @(Get-UnityAndroidPlayerDirs | ForEach-Object { Join-Path $_ 'SDK' })
    foreach ($sdk in $sdks) {
        $bt = Join-Path $sdk 'build-tools'
        if (-not (Test-Path -LiteralPath $bt -PathType Container)) { continue }
        $dirs = Get-ChildItem -LiteralPath $bt -Directory | Sort-Object { try { [version]($_.Name -replace '[^\d\.].*$', '') } catch { [version]'0.0' } } -Descending
        foreach ($d in $dirs) {
            $cand = Join-Path $d.FullName $exe
            if (Test-Path -LiteralPath $cand -PathType Leaf) { return $cand }
        }
    }
    $cmd = Get-Command aapt2 -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cmd) { return $cmd.Source }
    return $null
}

function Find-Bundletool {
    if ($Bundletool) { if (Test-Path -LiteralPath $Bundletool -PathType Leaf) { return $Bundletool } else { return $null } }
    foreach ($dir in Get-UnityAndroidPlayerDirs) {
        $jar = Get-ChildItem -Path (Join-Path $dir 'Tools') -Filter 'bundletool-all-*.jar' -File -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
        if ($jar) { return $jar.FullName }
    }
    return $null
}

function Find-Java {
    if ($env:JAVA_HOME) {
        $cand = Join-Path $env:JAVA_HOME ($IsWindows ? 'bin/java.exe' : 'bin/java')
        if (Test-Path -LiteralPath $cand -PathType Leaf) { return $cand }
    }
    $cmd = Get-Command java -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cmd) { return $cmd.Source }
    return $null
}

# --- Principal ----------------------------------------------------------------------------------
if ($SelfTest -or $Path -eq '--self-test' -or @($args) -contains '--self-test') { Invoke-SelfTest }
if (@($args).Count -gt 0) { Stop-WithCode 2 "ERROR: argumentos no reconocidos: $($args -join ' ')" }

$usage = 'Uso: pwsh -NoProfile -File ci/check-manifest.ps1 <archivo.aab | archivo.apk | carpeta | comodin> [-Mode release|dev] [-TargetSdk 36] [-MinSdk 26] [-AllowPermission <p>[,<p>]] [-RequireBilling] [-RequirePredictiveBack] [-Bundletool <jar>] [-Aapt2 <exe>] | --self-test'
if (-not $Path) { Stop-WithCode 2 $usage }
if ($Mode -notin @('release', 'dev')) { Stop-WithCode 2 "ERROR: -Mode debe ser release o dev (recibido: '$Mode')." }
$target = 0; $min = 0
if (-not [int]::TryParse($TargetSdk, [ref]$target) -or $target -le 0) { Stop-WithCode 2 "ERROR: -TargetSdk no es un entero positivo ('$TargetSdk')." }
if (-not [int]::TryParse($MinSdk, [ref]$min) -or $min -le 0) { Stop-WithCode 2 "ERROR: -MinSdk no es un entero positivo ('$MinSdk')." }
$extra = @($AllowPermission | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

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
Write-Output "Modo: $Mode; targetSdk $target; minSdk $min; permisos admitidos: $((@($BasePermissions) + $extra) -join ', ')"
foreach ($f in $files) {
    if ($f.Extension -eq '.aab') {
        $bt = Find-Bundletool
        $java = Find-Java
        if (-not $bt) { Stop-WithCode 3 'ERROR: falta bundletool (-Bundletool, BUNDLETOOL_JAR o el modulo Android de Unity).' }
        if (-not $java) { Stop-WithCode 3 'ERROR: falta java (JAVA_HOME o PATH) para bundletool.' }
        $out = & $java -jar $bt dump manifest "--bundle=$($f.FullName)" 2>&1 | ForEach-Object { "$_" }
        if ($LASTEXITCODE -ne 0) { Stop-WithCode 3 ("ERROR: bundletool dump manifest fallo con $LASTEXITCODE`n" + (($out | Select-Object -Last 10) -join "`n")) }
        try { $model = ConvertFrom-ManifestXml ($out -join "`n") }
        catch { Stop-WithCode 2 "ERROR: manifiesto ilegible en '$($f.FullName)': $($_.Exception.Message)" }
    }
    else {
        $tool = Find-Aapt2
        if (-not $tool) { Stop-WithCode 3 'ERROR: falta aapt2 (-Aapt2, AAPT2, ANDROID_HOME/build-tools o el modulo Android de Unity).' }
        $out = & $tool dump xmltree --file AndroidManifest.xml $f.FullName 2>&1 | ForEach-Object { "$_" }
        if ($LASTEXITCODE -ne 0) { Stop-WithCode 3 ("ERROR: aapt2 dump xmltree fallo con $LASTEXITCODE`n" + (($out | Select-Object -Last 10) -join "`n")) }
        try { $model = ConvertFrom-Aapt2Tree ($out -join "`n") }
        catch { Stop-WithCode 2 "ERROR: manifiesto ilegible en '$($f.FullName)': $($_.Exception.Message)" }
    }

    try { Set-NativeInfo $model (Get-ZipEntryNames $f.FullName) }
    catch { Stop-WithCode 2 "ERROR: no se puede leer '$($f.FullName)' como ZIP: $($_.Exception.Message)" }

    $r = Test-Manifest $model $Mode $target $min $extra ([bool]$RequireBilling) ([bool]$RequirePredictiveBack)
    $status = if ($r.Failures.Count -eq 0) { 'OK' } else { 'FALLA' }
    if ($r.Failures.Count -gt 0) { $failed = $true }
    Write-Output ("[{0}] {1}: package={2}, minSdk={3}, targetSdk={4}, appCategory={5}, debuggable={6}, abi={7}, il2cpp={8}, permisos={9}" -f
        $status, $f.FullName, $model.Package, $model.MinSdk, $model.TargetSdk, $model.AppCategory,
        ($model.Debuggable ? $model.Debuggable : 'false'), ($model.Abis -join ','), $model.HasIl2cpp,
        ((@($model.Permissions | Select-Object -Unique)) -join ','))
    foreach ($x in $r.Failures) { Write-Output "  FALLO: $x" }
    foreach ($x in $r.Notes) { Write-Output "  $x" }
}

if ($failed) { Stop-WithCode 1 'FALLA: algun manifiesto no cumple la compuerta (GDD 08 sec. 12.1 y 13.1).' }
Write-Output 'OK: todos los manifiestos cumplen la compuerta.'
exit 0
