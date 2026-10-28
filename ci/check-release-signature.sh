#!/usr/bin/env bash
# ci/check-release-signature.sh - Compuerta de firma del build de release de Amazon Fill (paso s32 del plan de
# instalacion; GDD 08 sec. 12.1 etapa 3 y sec. 13.1 #3; docs/dev/release.md). Comprueba que cada .aab (o .apk) va
# firmado con la clave de subida del keystore indicado, y no con la clave de depuracion de Unity ni sin firma.
#
# Uso:
#   bash ci/check-release-signature.sh <archivo.aab|archivo.apk> [...]   firma de los artefactos
#   bash ci/check-release-signature.sh --keystore-only                    solo abre el keystore y busca el alias
#   bash ci/check-release-signature.sh --self-test                        autotest con un keystore de prueba temporal
#
# Entradas (variables de entorno; las contrasenas solo se pasan a keytool por -storepass:env, nunca se imprimen):
#   AF_KEYSTORE_PATH       ruta del keystore de subida (fuera del repositorio)
#   ANDROID_KEYSTORE_PASS  contrasena del keystore
#   ANDROID_KEYALIAS_NAME  alias de la clave de subida
#
# Que comprueba en cada artefacto:
#   1. jarsigner -verify termina en "jar verified." (firma v1/JAR integra; los .aab solo llevan esa) y no avisa de
#      "unsigned entries" (entradas anadidas despues de firmar). No se usa -strict: una clave de subida es
#      autofirmada y -strict la rechazaria por la cadena de certificados.
#   2. Hay exactamente un firmante y su SHA-256 coincide con el del certificado del alias del keystore.
#   3. Ningun firmante es "CN=Android Debug" (la clave de depuracion de Unity/Gradle).
#
# Codigos de salida:
#   0  todo firmado con la clave de subida (o, con --keystore-only, keystore y alias legibles)
#   1  algun artefacto sin firma, con otra clave, con la de depuracion o con la firma rota; o el autotest fallo
#   2  uso incorrecto, archivo inexistente o faltan las variables de entorno
#   3  falta una herramienta (keytool o jarsigner; en el autotest, tambien jar)
#   4  el keystore no se abre con esa contrasena o no tiene ese alias
set -uo pipefail

# keytool y jarsigner en ingles, para leer sus etiquetas igual en cualquier maquina.
KT=(keytool -J-Duser.language=en -J-Duser.country=US)
JS=(jarsigner -J-Duser.language=en -J-Duser.country=US)

need_tools() {
    local t
    for t in keytool jarsigner "$@"; do
        if ! command -v "$t" > /dev/null 2>&1; then
            echo "check-release-signature: ERROR: falta $t (JDK 17 en el PATH)." >&2
            return 3
        fi
    done
}

# Huella SHA-256 (mayusculas, con ':') del certificado del alias; vacia si no se abre.
keystore_sha256() {
    "${KT[@]}" -list -v -keystore "$AF_KEYSTORE_PATH" -storepass:env ANDROID_KEYSTORE_PASS -alias "$ANDROID_KEYALIAS_NAME" 2> /dev/null \
        | sed -n 's/^[[:space:]]*SHA256:[[:space:]]*\([0-9A-Fa-f:]*\).*$/\1/p' | head -n 1 | tr '[:lower:]' '[:upper:]'
}

need_env() {
    local missing=() name
    for name in AF_KEYSTORE_PATH ANDROID_KEYSTORE_PASS ANDROID_KEYALIAS_NAME; do
        [ -n "${!name:-}" ] || missing+=("$name")
    done
    if [ "${#missing[@]}" -gt 0 ]; then
        echo "check-release-signature: ERROR: faltan las variables ${missing[*]}." >&2
        return 2
    fi
    if [ ! -f "$AF_KEYSTORE_PATH" ]; then
        echo "check-release-signature: ERROR: no existe el keystore de AF_KEYSTORE_PATH." >&2
        return 2
    fi
}

expected_or_fail() {
    EXPECTED="$(keystore_sha256)"
    if [ -z "$EXPECTED" ]; then
        echo "check-release-signature: ERROR: el keystore no se abre con ANDROID_KEYSTORE_PASS o no tiene el alias ANDROID_KEYALIAS_NAME." >&2
        return 4
    fi
}

check_artifact() {
    local file="$1" verify certs signers sha
    verify="$("${JS[@]}" -verify "$file" 2>&1)"
    # Here-strings y no "printf | grep -q": con pipefail, grep -q sale al primer acierto, printf recibe SIGPIPE si la
    # salida no cabe en la tuberia y el pipeline falla aunque grep haya acertado (release 40622798443).
    if ! grep -q '^jar verified\.' <<< "$verify"; then
        echo "FALLO: $file: jarsigner no lo verifica (sin firma o firma rota): $(sed -n '/./{p;q;}' <<< "$verify")"
        return 1
    fi
    if grep -qi 'unsigned entries' <<< "$verify"; then
        echo "FALLO: $file: lleva entradas sin firmar (anadidas despues de firmar)."
        return 1
    fi
    certs="$("${KT[@]}" -printcert -jarfile "$file" 2>&1)"
    if grep -Eq '^(Owner|Issuer): .*CN=Android Debug' <<< "$certs"; then
        echo "FALLO: $file: firmado con la clave de depuracion (CN=Android Debug)."
        return 1
    fi
    signers="$(printf '%s\n' "$certs" | sed -n 's/^[[:space:]]*SHA256:[[:space:]]*\([0-9A-Fa-f:]*\).*$/\1/p' | tr '[:lower:]' '[:upper:]' | sort -u)"
    if [ -z "$signers" ]; then
        echo "FALLO: $file: sin certificado de firmante."
        return 1
    fi
    if [ "$(printf '%s\n' "$signers" | wc -l)" -ne 1 ]; then
        echo "FALLO: $file: mas de un firmante."
        return 1
    fi
    sha="$signers"
    if [ "$sha" != "$EXPECTED" ]; then
        echo "FALLO: $file: firmado con otra clave (SHA-256 $sha; la de subida es $EXPECTED)."
        return 1
    fi
    echo "ok   : $file firmado con la clave de subida (SHA-256 $sha)."
}

run_artifacts() {
    local f rc=0
    for f in "$@"; do
        if [ ! -f "$f" ]; then
            echo "check-release-signature: ERROR: no existe $f." >&2
            return 2
        fi
        case "$f" in
            *.aab|*.apk) ;;
            *) echo "check-release-signature: ERROR: $f no es .aab ni .apk." >&2; return 2 ;;
        esac
    done
    need_tools || return
    need_env || return
    expected_or_fail || return
    for f in "$@"; do
        check_artifact "$f" || rc=1
    done
    if [ "$rc" -eq 0 ]; then
        echo "check-release-signature: OK."
    fi
    return "$rc"
}

self_test() {
    need_tools jar || return
    local dir failures=0 rc
    dir="$(mktemp -d)"
    # Solo valores de prueba generados aqui, en una carpeta temporal que se borra al terminar.
    local pass="autotest-$$-$RANDOM"
    gen() { # keystore alias dname
        "${KT[@]}" -genkeypair -keystore "$1" -storepass "$pass" -keypass "$pass" -alias "$2" -dname "$3" \
            -keyalg RSA -keysize 2048 -validity 2 -storetype PKCS12 > /dev/null 2>&1
    }
    gen "$dir/upload.p12" subida "CN=Prueba de subida" && gen "$dir/other.p12" otra "CN=Otra" \
        && gen "$dir/debug.p12" androiddebugkey "CN=Android Debug,O=Android,C=US" || { echo "FALLO: keytool -genkeypair"; rm -rf "$dir"; return 1; }
    echo "hola" > "$dir/a.txt"
    (cd "$dir" && jar cf unsigned.aab a.txt && cp unsigned.aab good.aab && cp unsigned.aab other.aab && cp unsigned.aab debug.aab)
    "${JS[@]}" -keystore "$dir/upload.p12" -storepass "$pass" "$dir/good.aab" subida > /dev/null 2>&1
    # Firmado y despues con una entrada anadida sin firmar.
    cp "$dir/good.aab" "$dir/tampered.aab"
    echo "extra" > "$dir/b.txt"
    (cd "$dir" && jar uf tampered.aab b.txt)
    "${JS[@]}" -keystore "$dir/other.p12" -storepass "$pass" "$dir/other.aab" otra > /dev/null 2>&1
    "${JS[@]}" -keystore "$dir/debug.p12" -storepass "$pass" "$dir/debug.aab" androiddebugkey > /dev/null 2>&1

    expect() { # esperado, descripcion, comando...
        local want="$1" what="$2"; shift 2
        "$@" > "$dir/out.txt" 2>&1
        rc=$?
        if [ "$rc" != "$want" ]; then
            echo "FALLO: $what -> $rc (esperado $want): $(cat "$dir/out.txt")"
            failures=$((failures + 1))
        elif grep -q "$pass" "$dir/out.txt"; then
            echo "FALLO: $what imprime la contrasena"
            failures=$((failures + 1))
        else
            echo "ok   : $what -> $rc"
        fi
    }
    local ok_env=(env AF_KEYSTORE_PATH="$dir/upload.p12" ANDROID_KEYSTORE_PASS="$pass" ANDROID_KEYALIAS_NAME=subida)
    expect 0 "firmado con la de subida" "${ok_env[@]}" bash "$0" "$dir/good.aab"
    expect 0 "--keystore-only" "${ok_env[@]}" bash "$0" --keystore-only
    expect 1 "sin firma" "${ok_env[@]}" bash "$0" "$dir/unsigned.aab"
    expect 1 "entrada sin firmar anadida" "${ok_env[@]}" bash "$0" "$dir/tampered.aab"
    expect 1 "otra clave" "${ok_env[@]}" bash "$0" "$dir/other.aab"
    expect 1 "clave de depuracion" "${ok_env[@]}" bash "$0" "$dir/debug.aab"
    expect 1 "uno bueno y uno malo" "${ok_env[@]}" bash "$0" "$dir/good.aab" "$dir/other.aab"
    expect 4 "contrasena equivocada" env AF_KEYSTORE_PATH="$dir/upload.p12" ANDROID_KEYSTORE_PASS=mala ANDROID_KEYALIAS_NAME=subida bash "$0" "$dir/good.aab"
    expect 4 "alias inexistente" env AF_KEYSTORE_PATH="$dir/upload.p12" ANDROID_KEYSTORE_PASS="$pass" ANDROID_KEYALIAS_NAME=nada bash "$0" --keystore-only
    expect 2 "sin variables" env -u AF_KEYSTORE_PATH -u ANDROID_KEYSTORE_PASS -u ANDROID_KEYALIAS_NAME bash "$0" "$dir/good.aab"
    expect 2 "keystore inexistente" env AF_KEYSTORE_PATH="$dir/nada.p12" ANDROID_KEYSTORE_PASS="$pass" ANDROID_KEYALIAS_NAME=subida bash "$0" "$dir/good.aab"
    expect 2 "archivo inexistente" "${ok_env[@]}" bash "$0" "$dir/nada.aab"
    expect 2 "extension invalida" "${ok_env[@]}" bash "$0" "$dir/a.txt"
    expect 2 "sin argumentos" bash "$0"
    rm -rf "$dir"
    if [ "$failures" -gt 0 ]; then
        echo "check-release-signature: autotest con $failures fallo(s)."
        return 1
    fi
    echo "check-release-signature: autotest OK."
}

case "${1:-}" in
    "") echo "check-release-signature: uso: bash ci/check-release-signature.sh <aab|apk>... | --keystore-only | --self-test" >&2; exit 2 ;;
    --self-test) self_test; exit $? ;;
    --keystore-only)
        need_tools || exit $?
        need_env || exit $?
        expected_or_fail || exit $?
        echo "check-release-signature: OK (keystore y alias legibles; SHA-256 del certificado de subida $EXPECTED)."
        exit 0
        ;;
    -*) echo "check-release-signature: ERROR: opcion desconocida $1." >&2; exit 2 ;;
    *) run_artifacts "$@"; exit $? ;;
esac
