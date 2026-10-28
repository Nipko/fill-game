#!/usr/bin/env bash
# ci/release-preflight.sh - Compuerta previa del build de release firmado de Amazon Fill (paso s32 del plan de
# instalacion; GDD 08 sec. 12.1 etapas 3-5 y sec. 13.1 #1 y #3; docs/dev/release.md). La usa el job "preflight" de
# .github/workflows/release.yml antes de pedir la licencia de Unity: si algo falta, falla en segundos y CERRADO.
#
# Uso:
#   bash ci/release-preflight.sh             comprueba las variables de entorno de abajo
#   bash ci/release-preflight.sh --self-test autotest con casos fijos (sin secretos reales)
#
# Entradas (variables de entorno; los secretos solo se comprueban como "no vacios" y nunca se imprimen):
#   AF_RELEASE_APPLICATION_ID  variable de repositorio que fija el dueno tras el dictamen de marca (D-034, D-041;
#                              recomendado com.parallext.tintalu). Obligatoria; nunca com.amazonfill.dev ni con
#                              "amazon"; formato de paquete Android (>= 2 segmentos [A-Za-z][A-Za-z0-9_]*).
#   AF_VERSION_NAME            MAYOR.MENOR.PARCHE (1-4 cifras, sin ceros a la izquierda), p. ej. 1.0.0.
#   AF_VERSION_CODE            entero 1..2100000000, o vacio: entonces MAYOR*10000 + MENOR*100 + PARCHE
#                              (exige MENOR y PARCHE <= 99; 1.0.0 -> 10000).
#   AF_GIT_REF                 rama que se construye (github.ref); solo refs/heads/main.
#   ANDROID_KEYSTORE_BASE64, ANDROID_KEYSTORE_PASS, ANDROID_KEYALIAS_NAME, ANDROID_KEYALIAS_PASS,
#   GOOGLE_SERVICES_JSON_BASE64, UNITY_EMAIL, UNITY_PASSWORD  secretos que crea el dueno; deben existir (no vacios).
#   GOOGLE_SERVICES_JSON_BASE64 es el google-services.json de prod en base64 (anadido de D-035 del 2026-10-27). Ademas
#                              de no vacio, debe decodificar a JSON con project_info.project_number y un
#                              client[].client_info.android_client_info.package_name igual a AF_RELEASE_APPLICATION_ID,
#                              cuyo mobilesdk_app_id sea 1:<project_number>:android:<hex>. Se valida con python3 (o
#                              python) leyendo el JSON por la entrada estandar; nunca se imprime su contenido (api_key
#                              incluida): solo el numero de proyecto y el ID de app, que son publicos.
#
# Salida: en GITHUB_OUTPUT (si esta definida) application_id, version_name, version_code, firebase_project_number y
# firebase_app_id; nada secreto.
#
# Codigos de salida:
#   0  todo correcto
#   2  uso incorrecto (argumento desconocido) o el autotest fallo
#   3  rama distinta de main
#   4  applicationId ausente, provisional (com.amazonfill.dev), con "amazon" o con formato invalido
#   5  falta algun secreto (se nombran todos los que faltan, nunca su valor)
#   6  versionName o versionCode invalidos
#   7  GOOGLE_SERVICES_JSON_BASE64 no es base64, no es JSON, le falta project_info.project_number, no tiene un cliente
#      Android con el applicationId de release o su mobilesdk_app_id no es del proyecto
#   8  falta python3 (o python) para validar google-services.json
set -uo pipefail

PROVISIONAL_ID="com.amazonfill.dev"

fail() { # codigo, mensaje
    echo "release-preflight: ERROR: $2" >&2
    return "$1"
}

check_ref() {
    if [ "${AF_GIT_REF:-}" != "refs/heads/main" ]; then
        fail 3 "el release solo se construye desde main (rama recibida: '${AF_GIT_REF:-}')."
        return
    fi
}

check_application_id() {
    local id="${AF_RELEASE_APPLICATION_ID:-}"
    if [ -z "$id" ]; then
        fail 4 "falta la variable de repositorio AF_RELEASE_APPLICATION_ID. Se crea solo tras el dictamen de marca (D-034); hasta entonces no hay release posible."
        return
    fi
    local lower
    lower="$(printf '%s' "$id" | tr '[:upper:]' '[:lower:]')"
    if [ "$lower" = "$PROVISIONAL_ID" ]; then
        fail 4 "AF_RELEASE_APPLICATION_ID es el ID provisional $PROVISIONAL_ID, que nunca llega a un release (regla dura 6)."
        return
    fi
    case "$lower" in
        *amazon*)
            fail 4 "AF_RELEASE_APPLICATION_ID lleva la cadena 'amazon' (08 sec. 13.1 #1): '$id'."
            return
            ;;
    esac
    local re_id='^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$'
    if ! [[ "$id" =~ $re_id ]]; then
        fail 4 "AF_RELEASE_APPLICATION_ID '$id' no es un nombre de paquete Android valido."
        return
    fi
}

# Deja el versionCode resuelto en VERSION_CODE_OUT.
check_version() {
    local name="${AF_VERSION_NAME:-}" code="${AF_VERSION_CODE:-}"
    local re_name='^(0|[1-9][0-9]{0,3})\.(0|[1-9][0-9]{0,3})\.(0|[1-9][0-9]{0,3})$' re_code='^[1-9][0-9]{0,9}$'
    if ! [[ "$name" =~ $re_name ]]; then
        fail 6 "versionName '$name' no es MAYOR.MENOR.PARCHE (p. ej. 1.0.0)."
        return
    fi
    if [ -z "$code" ]; then
        local major minor patch
        IFS=. read -r major minor patch <<< "$name"
        if [ "$minor" -gt 99 ] || [ "$patch" -gt 99 ]; then
            fail 6 "con MENOR o PARCHE > 99 la regla MAYOR*10000+MENOR*100+PARCHE no vale: indica versionCode."
            return
        fi
        code=$(( major * 10000 + minor * 100 + patch ))
    fi
    if ! [[ "$code" =~ $re_code ]] || [ "$code" -gt 2100000000 ]; then
        fail 6 "versionCode '$code' no es un entero entre 1 y 2100000000."
        return
    fi
    VERSION_CODE_OUT="$code"
}

check_secrets() {
    local missing=() name
    for name in UNITY_EMAIL UNITY_PASSWORD ANDROID_KEYSTORE_BASE64 ANDROID_KEYSTORE_PASS ANDROID_KEYALIAS_NAME ANDROID_KEYALIAS_PASS \
        GOOGLE_SERVICES_JSON_BASE64; do
        if [ -z "${!name:-}" ]; then
            missing+=("$name")
        fi
    done
    if [ "${#missing[@]}" -gt 0 ]; then
        fail 5 "faltan los secretos ${missing[*]} (los crea el dueno: docs/dev/release.md)."
        return
    fi
}

# Programa de python que valida google-services.json leido por stdin. argv[1]: applicationId de release. Imprime
# "<project_number> <mobilesdk_app_id>" o, si falla, solo un motivo fijo (nunca contenido del archivo) y sale con 1.
GS_PY='
import json, re, sys
def bad(msg):
    print(msg)
    sys.exit(1)
try:
    d = json.loads(sys.stdin.buffer.read().decode("utf-8-sig"))
except Exception:
    bad("no es JSON valido (UTF-8)")
if not isinstance(d, dict):
    bad("el JSON no es un objeto")
info = d.get("project_info")
number = info.get("project_number") if isinstance(info, dict) else None
if not isinstance(number, str) or not re.fullmatch(r"[1-9][0-9]{0,19}", number):
    bad("falta project_info.project_number o no es un numero")
clients = d.get("client")
if not isinstance(clients, list):
    bad("falta la lista client[]")
want = sys.argv[1]
found = []
for c in clients:
    ci = c.get("client_info") if isinstance(c, dict) else None
    aci = ci.get("android_client_info") if isinstance(ci, dict) else None
    pkg = aci.get("package_name") if isinstance(aci, dict) else None
    if not isinstance(pkg, str):
        continue
    if pkg == want:
        app = ci.get("mobilesdk_app_id")
        if not isinstance(app, str) or not re.fullmatch(r"1:[0-9]+:android:[0-9a-f]+", app):
            bad("el cliente de " + want + " no tiene un mobilesdk_app_id valido")
        if app.split(":")[1] != number:
            bad("el mobilesdk_app_id del cliente de " + want + " no es del proyecto " + number)
        print(number + " " + app)
        sys.exit(0)
    if re.fullmatch(r"[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+", pkg):
        found.append(pkg)
bad("no hay un cliente Android con package_name " + want + " (hay: " + (", ".join(found) or "ninguno") + ")")
'

# Deja en FIREBASE_NUMBER_OUT y FIREBASE_APP_OUT el numero de proyecto y el ID de app (publicos).
check_google_services() {
    local py
    if command -v python3 > /dev/null 2>&1 && python3 -c "" > /dev/null 2>&1; then
        py=python3
    elif command -v python > /dev/null 2>&1 && python -c "" > /dev/null 2>&1; then
        py=python
    else
        fail 8 "falta python3 (o python) para validar GOOGLE_SERVICES_JSON_BASE64."
        return
    fi
    local decoded result
    # base64 --decode falla con caracteres ajenos al alfabeto; la salida de error de base64 no lleva el contenido.
    if ! decoded="$(printf '%s' "$GOOGLE_SERVICES_JSON_BASE64" | base64 --decode 2> /dev/null)" || [ -z "$decoded" ]; then
        fail 7 "GOOGLE_SERVICES_JSON_BASE64 no es base64 valido (docs/dev/release.md, sec. 1)."
        return
    fi
    if ! result="$(printf '%s' "$decoded" | "$py" -I -c "$GS_PY" "$AF_RELEASE_APPLICATION_ID" 2> /dev/null | tr -d '\r')"; then
        fail 7 "google-services.json (GOOGLE_SERVICES_JSON_BASE64): ${result:-el validador fallo}."
        return
    fi
    FIREBASE_NUMBER_OUT="${result%% *}"
    FIREBASE_APP_OUT="${result#* }"
}

run_checks() {
    VERSION_CODE_OUT=""
    check_ref || return
    check_application_id || return
    check_secrets || return
    check_google_services || return
    check_version || return
    if [ -n "${GITHUB_OUTPUT:-}" ]; then
        {
            echo "application_id=${AF_RELEASE_APPLICATION_ID}"
            echo "version_name=${AF_VERSION_NAME}"
            echo "version_code=${VERSION_CODE_OUT}"
            echo "firebase_project_number=${FIREBASE_NUMBER_OUT}"
            echo "firebase_app_id=${FIREBASE_APP_OUT}"
        } >> "$GITHUB_OUTPUT"
    fi
    echo "release-preflight: OK. applicationId=${AF_RELEASE_APPLICATION_ID}, versionName=${AF_VERSION_NAME}, versionCode=${VERSION_CODE_OUT}, Firebase: proyecto ${FIREBASE_NUMBER_OUT}, app ${FIREBASE_APP_OUT}."
}

self_test() {
    local failures=0
    # google-services.json de prueba (inventado; mismo esquema que el de Firebase). La api_key lleva un centinela que
    # ninguna salida puede contener.
    gs_json() { # numero de proyecto, paquete del cliente, mobilesdk_app_id
        printf '{"project_info":{"project_number":"%s","project_id":"prueba-af","storage_bucket":"prueba-af.appspot.com"},"client":[{"client_info":{"mobilesdk_app_id":"1:999999999999:android:0000000000000000","android_client_info":{"package_name":"com.otra.app"}},"api_key":[{"current_key":"sentinela-api-otra"}]},{"client_info":{"mobilesdk_app_id":"%s","android_client_info":{"package_name":"%s"}},"oauth_client":[],"api_key":[{"current_key":"sentinela-api-clave"}]}],"configuration_version":"1"}' "$1" "$3" "$2"
    }
    b64() { base64 -w0; }
    local gs_ok
    gs_ok="$(gs_json 123456789012 com.parallext.tintalu 1:123456789012:android:abcdef0123456789 | b64)"
    # caso: esperado, y asignaciones de entorno (el resto parte de un estado valido)
    expect() {
        local want="$1"; shift
        local got out
        out="$(env -i PATH="$PATH" \
            AF_GIT_REF=refs/heads/main AF_RELEASE_APPLICATION_ID=com.parallext.tintalu \
            AF_VERSION_NAME=1.0.0 AF_VERSION_CODE= \
            UNITY_EMAIL=x UNITY_PASSWORD=x ANDROID_KEYSTORE_BASE64=x ANDROID_KEYSTORE_PASS=sentinela-secreta \
            ANDROID_KEYALIAS_NAME=x ANDROID_KEYALIAS_PASS=x GITHUB_OUTPUT= GOOGLE_SERVICES_JSON_BASE64="$gs_ok" \
            "$@" bash "$0" 2>&1)"
        got=$?
        # Etiqueta corta: los base64 de prueba son largos (y llevan centinelas codificados).
        local label
        label="$(printf '%s' "$*" | tr '\n' ' ' | cut -c1-72)"
        if [ "$got" != "$want" ]; then
            echo "FALLO: $label -> $got (esperado $want): $out"
            failures=$((failures + 1))
        elif printf '%s' "$out" | grep -q -e 'sentinela-secreta' -e 'sentinela-api' -e 'storage_bucket' -e 'prueba-af'; then
            echo "FALLO: $label imprime un secreto"
            failures=$((failures + 1))
        else
            echo "ok   : $label -> $got"
        fi
    }
    expect 0
    expect 0 AF_VERSION_CODE=42
    expect 3 AF_GIT_REF=refs/heads/integracion-fase1-fase2
    expect 3 AF_GIT_REF=
    expect 4 AF_RELEASE_APPLICATION_ID=
    expect 4 AF_RELEASE_APPLICATION_ID=com.amazonfill.dev
    expect 4 AF_RELEASE_APPLICATION_ID=COM.AMAZONFILL.DEV
    expect 4 AF_RELEASE_APPLICATION_ID=com.amazonfill.release
    expect 4 AF_RELEASE_APPLICATION_ID=tintalu
    expect 4 AF_RELEASE_APPLICATION_ID=com.1parallext.tintalu
    expect 4 "AF_RELEASE_APPLICATION_ID=com.parallext.tintalu -x"
    expect 4 "AF_RELEASE_APPLICATION_ID=com.parallext.tintalu
com.otra.app"
    expect 6 "AF_VERSION_CODE=12
34"
    expect 5 ANDROID_KEYSTORE_BASE64=
    expect 5 ANDROID_KEYSTORE_PASS= ANDROID_KEYALIAS_PASS=
    expect 5 UNITY_EMAIL=
    expect 6 AF_VERSION_NAME=
    expect 6 AF_VERSION_NAME=1.0
    expect 6 AF_VERSION_NAME=01.0.0
    expect 6 "AF_VERSION_NAME=1.0.0 -afApk"
    expect 6 AF_VERSION_NAME=1.100.0
    expect 0 AF_VERSION_NAME=1.100.0 AF_VERSION_CODE=10100
    expect 6 AF_VERSION_CODE=0
    expect 6 AF_VERSION_CODE=abc
    expect 6 AF_VERSION_CODE=2100000001
    # google-services.json (GOOGLE_SERVICES_JSON_BASE64): falta, base64 invalido, JSON invalido, sin numero de
    # proyecto, paquete distinto, ID de app de otro proyecto o mal formado, y correcto (tambien con BOM y con el
    # cliente de release en otra posicion: lo cubre gs_json, que pone otro cliente delante).
    expect 5 GOOGLE_SERVICES_JSON_BASE64=
    expect 7 'GOOGLE_SERVICES_JSON_BASE64=esto no es base64!'
    expect 7 GOOGLE_SERVICES_JSON_BASE64="$(printf 'sentinela-api-nojson {' | b64)"
    expect 7 GOOGLE_SERVICES_JSON_BASE64="$(printf '[1,2]' | b64)"
    expect 7 GOOGLE_SERVICES_JSON_BASE64="$(printf '{"client":[]}' | b64)"
    expect 7 GOOGLE_SERVICES_JSON_BASE64="$(gs_json 0123 com.parallext.tintalu 1:123:android:ab | b64)"
    expect 7 GOOGLE_SERVICES_JSON_BASE64="$(gs_json 123456789012 com.amazonfill.dev 1:123456789012:android:abcdef0123456789 | b64)"
    expect 7 GOOGLE_SERVICES_JSON_BASE64="$(gs_json 123456789012 com.parallext.tintalu 1:528273757148:android:abcdef0123456789 | b64)"
    expect 7 GOOGLE_SERVICES_JSON_BASE64="$(gs_json 123456789012 com.parallext.tintalu sentinela-api-id | b64)"
    expect 0 GOOGLE_SERVICES_JSON_BASE64="$( { printf '\357\273\277'; gs_json 123456789012 com.parallext.tintalu 1:123456789012:android:abcdef0123456789; } | b64)"
    # Con otro applicationId de release valido, el cliente debe ser el suyo.
    expect 7 AF_RELEASE_APPLICATION_ID=com.parallext.otra
    # Salida de GITHUB_OUTPUT con la regla del versionCode y los datos publicos de Firebase.
    local tmp
    tmp="$(mktemp)"
    expect 0 GITHUB_OUTPUT="$tmp" AF_VERSION_NAME=2.3.4
    if ! grep -qx 'version_code=20304' "$tmp" || ! grep -qx 'application_id=com.parallext.tintalu' "$tmp" \
        || ! grep -qx 'firebase_project_number=123456789012' "$tmp" \
        || ! grep -qx 'firebase_app_id=1:123456789012:android:abcdef0123456789' "$tmp" \
        || grep -q 'sentinela' "$tmp"; then
        echo "FALLO: GITHUB_OUTPUT inesperado: $(cat "$tmp")"
        failures=$((failures + 1))
    fi
    rm -f "$tmp"
    if [ "$failures" -gt 0 ]; then
        echo "release-preflight: autotest con $failures fallo(s)."
        return 2
    fi
    echo "release-preflight: autotest OK."
}

case "${1:-}" in
    "") run_checks; exit $? ;;
    --self-test) self_test; exit $? ;;
    *) echo "release-preflight: uso: bash ci/release-preflight.sh [--self-test]" >&2; exit 2 ;;
esac
