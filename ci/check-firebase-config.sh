#!/usr/bin/env bash
# ci/check-firebase-config.sh - Compuerta del build de release firmado de Amazon Fill (anadido de D-035 del 2026-10-27;
# docs/dev/release.md y docs/dev/firebase-cliente.md sec. 2): el AAB lleva la configuracion de Firebase del proyecto
# de prod esperado. Sin ella el juego arranca con las implementaciones falsas (UnavailableBackendClient: la tienda no
# vende; sin Analytics, Crashlytics, Remote Config ni Auth).
#
# El editor de Firebase convierte google-services.json en Assets/Plugins/Android/FirebaseApp.androidlib (values.xml con
# google_app_id, gcm_defaultSenderId, google_api_key, ...); en el AAB esos recursos van compilados en resources.pb. Esta
# compuerta los lee con "bundletool dump resources" y SOLO mira google_app_id y gcm_defaultSenderId, que son publicos
# (ID de app y numero de proyecto). Nunca lee ni imprime google_api_key ni ningun otro recurso.
#
# Uso:
#   bash ci/check-firebase-config.sh <aab>   comprueba el AAB
#   bash ci/check-firebase-config.sh --self-test
#
# Entradas (variables de entorno):
#   BUNDLETOOL_JAR               bundletool-all-<version>.jar (obligatoria; java en el PATH)
#   AF_FIREBASE_APP_ID           mobilesdk_app_id esperado (lo saca ci/release-preflight.sh del secreto
#                                GOOGLE_SERVICES_JSON_BASE64), p. ej. 1:459140125724:android:810c83a65d0ff611f07bd9
#   AF_FIREBASE_PROJECT_NUMBER   project_number esperado, p. ej. 459140125724
#
# Codigos de salida:
#   0  el AAB lleva google_app_id y gcm_defaultSenderId iguales a los esperados
#   1  falta el recurso, tiene varios valores o no coincide (AAB sin Firebase o de otro proyecto)
#   2  uso incorrecto, entradas esperadas vacias o mal formadas, o el autotest fallo
#   3  falta java o BUNDLETOOL_JAR, el AAB no existe o bundletool no pudo leerlo
set -uo pipefail

fail() { # codigo, mensaje
    echo "check-firebase-config: ERROR: $2" >&2
    return "$1"
}

# Saca el unico valor de cadena de la salida de "bundletool dump resources --values". Formato (bundletool 1.17):
#   Package 'com.x.y':
#   0x7f0f0058 - string/google_app_id
#   	(default) - [STR] "1:528273757148:android:45d5cf0ddb2e3021c93519"
# Deja el valor en VALUE_OUT. 1 si no hay ninguno o hay mas de uno.
extract_value() {
    local text="$1" lines
    VALUE_OUT=""
    lines="$(printf '%s\n' "$text" | tr -d '\r' | grep -E '^[[:space:]]+.*\[STR\] "' || true)"
    if [ -z "$lines" ]; then
        return 1
    fi
    if [ "$(printf '%s\n' "$lines" | wc -l)" -ne 1 ]; then
        return 1
    fi
    VALUE_OUT="$(printf '%s' "$lines" | sed -E 's/^.*\[STR\] "(.*)"[[:space:]]*$/\1/')"
    [ -n "$VALUE_OUT" ]
}

dump_string() { # aab, nombre
    java -jar "$BUNDLETOOL_JAR" dump resources --bundle "$1" --resource "string/$2" --values 2>&1
}

check() {
    local aab="$1"
    local want_app="${AF_FIREBASE_APP_ID:-}" want_number="${AF_FIREBASE_PROJECT_NUMBER:-}"
    if ! [[ "$want_number" =~ ^[1-9][0-9]{0,19}$ ]]; then
        fail 2 "AF_FIREBASE_PROJECT_NUMBER vacio o mal formado ('$want_number')."
        return
    fi
    if ! [[ "$want_app" =~ ^1:[0-9]+:android:[0-9a-f]+$ ]]; then
        fail 2 "AF_FIREBASE_APP_ID vacio o mal formado ('$want_app')."
        return
    fi
    if ! command -v java > /dev/null 2>&1; then
        fail 3 "falta java en el PATH."
        return
    fi
    if [ -z "${BUNDLETOOL_JAR:-}" ] || [ ! -f "$BUNDLETOOL_JAR" ]; then
        fail 3 "falta BUNDLETOOL_JAR ('${BUNDLETOOL_JAR:-}')."
        return
    fi
    if [ ! -f "$aab" ]; then
        fail 3 "no existe el AAB '$aab'."
        return
    fi
    local out rc
    out="$(dump_string "$aab" google_app_id)"
    rc=$?
    # bundletool sale con != 0 si no puede leer el AAB; si el recurso no existe sale con 0 y sin valores.
    if [ "$rc" -ne 0 ]; then
        fail 3 "bundletool no pudo leer '$aab' (codigo $rc)."
        return
    fi
    if ! extract_value "$out"; then
        fail 1 "el AAB no lleva un unico string/google_app_id: no tiene la configuracion de Firebase (google-services.json no se coloco o el editor de Firebase no genero FirebaseApp.androidlib)."
        return
    fi
    local got_app="$VALUE_OUT"
    out="$(dump_string "$aab" gcm_defaultSenderId)"
    rc=$?
    if [ "$rc" -ne 0 ]; then
        fail 3 "bundletool no pudo leer '$aab' (codigo $rc)."
        return
    fi
    if ! extract_value "$out"; then
        fail 1 "el AAB lleva google_app_id pero no un unico string/gcm_defaultSenderId."
        return
    fi
    local got_number="$VALUE_OUT"
    echo "check-firebase-config: AAB google_app_id=$got_app gcm_defaultSenderId=$got_number"
    if [ "$got_app" != "$want_app" ]; then
        fail 1 "google_app_id $got_app distinto del esperado $want_app (configuracion de Firebase de otra app o proyecto)."
        return
    fi
    if [ "$got_number" != "$want_number" ]; then
        fail 1 "gcm_defaultSenderId $got_number distinto del numero de proyecto esperado $want_number."
        return
    fi
    echo "check-firebase-config: OK. El AAB lleva la configuracion de Firebase del proyecto $want_number (app $want_app)."
}

self_test() {
    local failures=0 dir
    dir="$(mktemp -d)"
    # Un "java" falso que imita a bundletool: responde segun el AAB (su nombre elige el caso) y el recurso pedido.
    mkdir -p "$dir/bin"
    cat > "$dir/bin/java" << 'EOF'
#!/usr/bin/env bash
aab="" res=""
while [ $# -gt 0 ]; do
    case "$1" in
        --bundle) aab="$2"; shift ;;
        --resource) res="$2"; shift ;;
    esac
    shift
done
case "$(basename "$aab")" in
    unreadable.aab) echo "Error: not a zip"; exit 1 ;;
    none.aab) echo "Package 'com.parallext.tintalu':"; exit 0 ;;
esac
case "$(basename "$aab"):$res" in
    *:string/google_api_key) echo "FALLO: se pidio google_api_key"; exit 9 ;;
    good.aab:string/google_app_id|nosender.aab:string/google_app_id)
        printf "Package 'com.parallext.tintalu':\n0x7f0f0058 - string/google_app_id\n\t(default) - [STR] \"1:111111111111:android:abcdef0123456789\"\r\n" ;;
    good.aab:string/gcm_defaultSenderId|devapp.aab:string/gcm_defaultSenderId|twice.aab:string/gcm_defaultSenderId)
        printf "Package 'com.parallext.tintalu':\n0x7f0f0056 - string/gcm_defaultSenderId\n\t(default) - [STR] \"111111111111\"\n" ;;
    devapp.aab:string/google_app_id)
        printf "Package 'com.amazonfill.dev':\n0x7f0f0058 - string/google_app_id\n\t(default) - [STR] \"1:222222222222:android:fedcba9876543210\"\n" ;;
    twice.aab:string/google_app_id)
        printf "Package 'com.parallext.tintalu':\n0x7f0f0058 - string/google_app_id\n\t(default) - [STR] \"1:111111111111:android:abcdef0123456789\"\n\ten - [STR] \"1:111111111111:android:abcdef0123456789\"\n" ;;
    nosender.aab:string/gcm_defaultSenderId) echo "Package 'com.parallext.tintalu':" ;;
    *) echo "caso desconocido $aab $res"; exit 1 ;;
esac
EOF
    chmod +x "$dir/bin/java"
    : > "$dir/bundletool-all.jar"
    local name
    for name in good none devapp twice nosender unreadable; do : > "$dir/$name.aab"; done
    # caso: esperado, aab, y asignaciones de entorno (el resto parte de un estado valido)
    expect() {
        local want="$1" aab="$2"; shift 2
        local got out
        out="$(env -i PATH="$dir/bin:$PATH" BUNDLETOOL_JAR="$dir/bundletool-all.jar" \
            AF_FIREBASE_APP_ID=1:111111111111:android:abcdef0123456789 AF_FIREBASE_PROJECT_NUMBER=111111111111 \
            "$@" bash "$0" "$aab" 2>&1)"
        got=$?
        if [ "$got" != "$want" ]; then
            echo "FALLO: $(basename "$aab") $* -> $got (esperado $want): $out"
            failures=$((failures + 1))
        else
            echo "ok   : $(basename "$aab") $* -> $got"
        fi
    }
    expect 0 "$dir/good.aab"
    expect 1 "$dir/none.aab"
    expect 1 "$dir/devapp.aab"
    expect 1 "$dir/twice.aab"
    expect 1 "$dir/nosender.aab"
    expect 1 "$dir/good.aab" AF_FIREBASE_PROJECT_NUMBER=111111111112
    expect 1 "$dir/good.aab" AF_FIREBASE_APP_ID=1:111111111111:android:abcdef0123456780
    expect 3 "$dir/unreadable.aab"
    expect 3 "$dir/missing.aab"
    expect 3 "$dir/good.aab" BUNDLETOOL_JAR=
    expect 3 "$dir/good.aab" BUNDLETOOL_JAR="$dir/no-such.jar"
    expect 2 "$dir/good.aab" AF_FIREBASE_APP_ID=
    expect 2 "$dir/good.aab" AF_FIREBASE_APP_ID=no-es-un-id
    expect 2 "$dir/good.aab" AF_FIREBASE_PROJECT_NUMBER=
    expect 2 "$dir/good.aab" AF_FIREBASE_PROJECT_NUMBER=12a
    # Uso: sin argumento o con dos, 2.
    local rc
    bash "$0" > /dev/null 2>&1; rc=$?
    if [ "$rc" != 2 ]; then echo "FALLO: sin argumentos -> $rc (esperado 2)"; failures=$((failures + 1)); else echo "ok   : sin argumentos -> 2"; fi
    bash "$0" a b > /dev/null 2>&1; rc=$?
    if [ "$rc" != 2 ]; then echo "FALLO: dos argumentos -> $rc (esperado 2)"; failures=$((failures + 1)); else echo "ok   : dos argumentos -> 2"; fi
    rm -rf "$dir"
    if [ "$failures" -gt 0 ]; then
        echo "check-firebase-config: autotest con $failures fallo(s)."
        return 2
    fi
    echo "check-firebase-config: autotest OK."
}

if [ "$#" -ne 1 ]; then
    echo "check-firebase-config: uso: bash ci/check-firebase-config.sh <aab> | --self-test" >&2
    exit 2
fi
case "$1" in
    --self-test) self_test; exit $? ;;
    -*) echo "check-firebase-config: uso: bash ci/check-firebase-config.sh <aab> | --self-test" >&2; exit 2 ;;
    *) check "$1"; exit $? ;;
esac
