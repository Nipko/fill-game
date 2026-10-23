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
#   UNITY_EMAIL, UNITY_PASSWORD  secretos que crea el dueno; deben existir (no vacios).
#
# Salida: en GITHUB_OUTPUT (si esta definida) application_id, version_name y version_code; nada secreto.
#
# Codigos de salida:
#   0  todo correcto
#   2  uso incorrecto (argumento desconocido) o el autotest fallo
#   3  rama distinta de main
#   4  applicationId ausente, provisional (com.amazonfill.dev), con "amazon" o con formato invalido
#   5  falta algun secreto (se nombran todos los que faltan, nunca su valor)
#   6  versionName o versionCode invalidos
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
    for name in UNITY_EMAIL UNITY_PASSWORD ANDROID_KEYSTORE_BASE64 ANDROID_KEYSTORE_PASS ANDROID_KEYALIAS_NAME ANDROID_KEYALIAS_PASS; do
        if [ -z "${!name:-}" ]; then
            missing+=("$name")
        fi
    done
    if [ "${#missing[@]}" -gt 0 ]; then
        fail 5 "faltan los secretos ${missing[*]} (los crea el dueno: docs/dev/release.md)."
        return
    fi
}

run_checks() {
    VERSION_CODE_OUT=""
    check_ref || return
    check_application_id || return
    check_secrets || return
    check_version || return
    if [ -n "${GITHUB_OUTPUT:-}" ]; then
        {
            echo "application_id=${AF_RELEASE_APPLICATION_ID}"
            echo "version_name=${AF_VERSION_NAME}"
            echo "version_code=${VERSION_CODE_OUT}"
        } >> "$GITHUB_OUTPUT"
    fi
    echo "release-preflight: OK. applicationId=${AF_RELEASE_APPLICATION_ID}, versionName=${AF_VERSION_NAME}, versionCode=${VERSION_CODE_OUT}."
}

self_test() {
    local failures=0
    # caso: esperado, y asignaciones de entorno (el resto parte de un estado valido)
    expect() {
        local want="$1"; shift
        local got out
        out="$(env -i PATH="$PATH" \
            AF_GIT_REF=refs/heads/main AF_RELEASE_APPLICATION_ID=com.parallext.tintalu \
            AF_VERSION_NAME=1.0.0 AF_VERSION_CODE= \
            UNITY_EMAIL=x UNITY_PASSWORD=x ANDROID_KEYSTORE_BASE64=x ANDROID_KEYSTORE_PASS=sentinela-secreta \
            ANDROID_KEYALIAS_NAME=x ANDROID_KEYALIAS_PASS=x GITHUB_OUTPUT= \
            "$@" bash "$0" 2>&1)"
        got=$?
        if [ "$got" != "$want" ]; then
            echo "FALLO: $* -> $got (esperado $want): $out"
            failures=$((failures + 1))
        elif printf '%s' "$out" | grep -q 'sentinela-secreta'; then
            echo "FALLO: $* imprime un secreto"
            failures=$((failures + 1))
        else
            echo "ok   : $* -> $got"
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
    # Salida de GITHUB_OUTPUT con la regla del versionCode.
    local tmp
    tmp="$(mktemp)"
    expect 0 GITHUB_OUTPUT="$tmp" AF_VERSION_NAME=2.3.4
    if ! grep -qx 'version_code=20304' "$tmp" || ! grep -qx 'application_id=com.parallext.tintalu' "$tmp"; then
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
