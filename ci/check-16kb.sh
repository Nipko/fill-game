#!/usr/bin/env bash
# ci/check-16kb.sh - Compuerta de paginas de 16 KB para Aquazonia (GDD 08 sec. 12.1 y 13.1;
# docs/setup/research/b-android-play-2026.md sec. 3).
#
# Uso:
#   bash ci/check-16kb.sh <archivo.aab | archivo.apk | archivo.apks | carpeta> [...]
#
# Que comprueba:
#   1. Alineacion ELF: cada biblioteca nativa de 64 bits (.so de lib/arm64-v8a y lib/x86_64, o
#      cualquier ELF de 64 bits en una carpeta) debe tener TODOS sus segmentos LOAD alineados a
#      16 KB (Align >= 0x4000, es decir 2**14). Las ABI de 32 bits no lo necesitan y se omiten.
#      Herramienta: LLVM_READELF, llvm-readelf, readelf u objdump (en ese orden).
#   2. Alineacion ZIP de cada APK: "zipalign -c -P 16 -v 4 <apk>" debe terminar en
#      "Verification successful" (build-tools >= 35). Para un .aab, este paso se hace sobre el APK
#      universal que genera bundletool si BUNDLETOOL_JAR apunta a bundletool-all.jar y hay java;
#      si no, se avisa y se comprueba solo la alineacion ELF (Play la vuelve a comprobar al subir),
#      salvo con CHECK16_REQUIRE_ZIPALIGN=1, que lo convierte en error (codigo 3).
#   3. Al menos una biblioteca nativa de 64 bits comprobada en total: un build IL2CPP ARM64 siempre
#      trae libil2cpp.so y libunity.so; si no hay ninguna, falla (codigo 1) salvo con
#      CHECK16_ALLOW_NO_NATIVE=1.
#
# Busqueda de herramientas: variables LLVM_READELF, ZIPALIGN, ANDROID_NDK_ROOT, ANDROID_NDK_HOME,
# ANDROID_HOME, ANDROID_SDK_ROOT y UNITY_EDITOR_PATH; en Windows (Git Bash) tambien
# %LOCALAPPDATA%\Android\Sdk y el NDK/SDK del modulo Android de los Editores de Unity del Hub.
#
# Codigos de salida:
#   0  todo alineado a 16 KB
#   1  alguna .so sin alinear, zipalign falla o ninguna .so de 64 bits comprobada
#   2  uso incorrecto o archivo inexistente / formato no soportado
#   3  falta una herramienta necesaria (lector ELF; zipalign para un .apk; con
#      CHECK16_REQUIRE_ZIPALIGN=1, tambien BUNDLETOOL_JAR y java para un .aab)
#
# Opciones de entorno:
#   CHECK16_REQUIRE_ZIPALIGN=1  exige la comprobacion zipalign de todo APK, incluido el universal
#                               de cada .aab (falta de zipalign, BUNDLETOOL_JAR o java = codigo 3).
#                               Tiene prioridad sobre CHECK16_SKIP_ZIPALIGN. El CI la usa siempre.
#   CHECK16_SKIP_ZIPALIGN=1     no exige zipalign para los .apk (solo comprobacion ELF). Solo local.
#   CHECK16_ALLOW_NO_NATIVE=1   acepta entradas sin ninguna .so de 64 bits (p. ej. un APK de prueba
#                               sin codigo nativo). Nunca en CI.

set -uo pipefail

PAGE_ALIGN=16384

die() { local code="$1"; shift; printf '%s\n' "$*" >&2; exit "$code"; }

usage() {
  die 2 "Uso: bash ci/check-16kb.sh <archivo.aab | archivo.apk | archivo.apks | carpeta> [...]"
}

[ "$#" -ge 1 ] || usage

to_unix_path() {
  # Convierte una ruta de Windows a la forma de Git Bash si hace falta.
  if [ -n "${1:-}" ] && command -v cygpath >/dev/null 2>&1; then cygpath -u "$1"; else printf '%s' "${1:-}"; fi
}

# Directorios candidatos de SDK y NDK.
sdk_dirs=()
ndk_dirs=()
for v in "${ANDROID_HOME:-}" "${ANDROID_SDK_ROOT:-}"; do
  [ -n "$v" ] && sdk_dirs+=("$(to_unix_path "$v")")
done
for v in "${ANDROID_NDK_ROOT:-}" "${ANDROID_NDK_HOME:-}"; do
  [ -n "$v" ] && ndk_dirs+=("$(to_unix_path "$v")")
done
if [ -n "${LOCALAPPDATA:-}" ]; then
  sdk_dirs+=("$(to_unix_path "$LOCALAPPDATA")/Android/Sdk")
fi
unity_android_dirs=()
if [ -n "${UNITY_EDITOR_PATH:-}" ]; then
  unity_android_dirs+=("$(dirname "$(to_unix_path "$UNITY_EDITOR_PATH")")/Data/PlaybackEngines/AndroidPlayer")
fi
for d in "/c/Program Files/Unity/Hub/Editor"/*/Editor/Data/PlaybackEngines/AndroidPlayer "$HOME"/Unity/Hub/Editor/*/Editor/Data/PlaybackEngines/AndroidPlayer; do
  [ -d "$d" ] && unity_android_dirs+=("$d")
done
for d in "${unity_android_dirs[@]}"; do
  sdk_dirs+=("$d/SDK")
  ndk_dirs+=("$d/NDK")
done
for s in "${sdk_dirs[@]}"; do
  for n in "$s"/ndk/*; do [ -d "$n" ] && ndk_dirs+=("$n"); done
done

find_readelf() {
  if [ -n "${LLVM_READELF:-}" ]; then
    local p; p="$(to_unix_path "$LLVM_READELF")"
    [ -x "$p" ] && { printf 'readelf\t%s' "$p"; return 0; }
  fi
  local c
  for c in llvm-readelf readelf; do
    if command -v "$c" >/dev/null 2>&1; then printf 'readelf\t%s' "$(command -v "$c")"; return 0; fi
  done
  local n f
  for n in "${ndk_dirs[@]}"; do
    for f in "$n"/toolchains/llvm/prebuilt/*/bin/llvm-readelf "$n"/toolchains/llvm/prebuilt/*/bin/llvm-readelf.exe; do
      [ -x "$f" ] && { printf 'readelf\t%s' "$f"; return 0; }
    done
  done
  if command -v objdump >/dev/null 2>&1; then printf 'objdump\t%s' "$(command -v objdump)"; return 0; fi
  return 1
}

find_zipalign() {
  if [ -n "${ZIPALIGN:-}" ]; then
    local p; p="$(to_unix_path "$ZIPALIGN")"
    [ -x "$p" ] && { printf '%s' "$p"; return 0; }
  fi
  if command -v zipalign >/dev/null 2>&1; then command -v zipalign; return 0; fi
  local s best=""
  for s in "${sdk_dirs[@]}"; do
    [ -d "$s/build-tools" ] || continue
    # La version mas alta de build-tools (zipalign -P 16 exige >= 35).
    best="$(ls -1 "$s/build-tools" 2>/dev/null | sort -V | tail -n 1)"
    if [ -n "$best" ]; then
      for f in "$s/build-tools/$best/zipalign" "$s/build-tools/$best/zipalign.exe"; do
        [ -x "$f" ] && { printf '%s' "$f"; return 0; }
      done
    fi
  done
  return 1
}

READELF_KIND=""
READELF_BIN=""
if tool="$(find_readelf)"; then
  READELF_KIND="${tool%%$'\t'*}"
  READELF_BIN="${tool#*$'\t'}"
else
  die 3 "ERROR: no hay lector ELF (llvm-readelf, readelf u objdump). Define LLVM_READELF o ANDROID_NDK_ROOT; en Ubuntu: sudo apt-get install binutils."
fi
ZIPALIGN_BIN="$(find_zipalign || true)"

TMP_ROOT="$(mktemp -d 2>/dev/null || mktemp -d -t check16kb)"
cleanup() { rm -rf "$TMP_ROOT"; }
trap cleanup EXIT

failures=0
checked=0
skipped32=0

is_elf64() {
  # Devuelve 0 si el archivo es ELF de 64 bits (EI_CLASS = 2).
  local magic class
  magic="$(od -An -tx1 -N4 "$1" 2>/dev/null | tr -d ' \n')"
  [ "$magic" = "7f454c46" ] || return 2
  class="$(od -An -tx1 -j4 -N1 "$1" 2>/dev/null | tr -d ' \n')"
  [ "$class" = "02" ]
}

load_aligns() {
  # Imprime, una por linea, la alineacion en bytes de cada segmento LOAD.
  local so="$1"
  if [ "$READELF_KIND" = "readelf" ]; then
    "$READELF_BIN" -lW "$so" 2>/dev/null | awk '$1 == "LOAD" { print $NF }' | while read -r a; do
      case "$a" in
        0x*|0X*) printf '%d\n' "$((16#${a#0[xX]}))" ;;
        *) printf '%s\n' "$a" ;;
      esac
    done
  else
    "$READELF_BIN" -p "$so" 2>/dev/null | awk '$1 == "LOAD" { for (i = 1; i <= NF; i++) if ($i == "align") print $(i + 1) }' | while read -r a; do
      printf '%d\n' "$((1 << ${a#2\*\*}))"
    done
  fi
}

check_so() {
  local so="$1" label="$2"
  case "$label" in
    */armeabi-v7a/*|*/armeabi/*|*/x86/*|armeabi-v7a/*|x86/*) skipped32=$((skipped32 + 1)); return 0 ;;
  esac
  is_elf64 "$so"
  local rc=$?
  if [ "$rc" -eq 2 ]; then
    printf '  [ERROR] %s: no es un ELF valido\n' "$label"
    failures=$((failures + 1)); return 0
  elif [ "$rc" -ne 0 ]; then
    skipped32=$((skipped32 + 1)); return 0
  fi
  local aligns min=""
  aligns="$(load_aligns "$so")"
  if [ -z "$aligns" ]; then
    printf '  [ERROR] %s: no se pudieron leer los segmentos LOAD\n' "$label"
    failures=$((failures + 1)); return 0
  fi
  local a
  for a in $aligns; do
    if [ -z "$min" ] || [ "$a" -lt "$min" ]; then min="$a"; fi
  done
  checked=$((checked + 1))
  if [ "$min" -ge "$PAGE_ALIGN" ]; then
    printf '  [OK]    %s (align minimo %d)\n' "$label" "$min"
  else
    printf '  [FALLA] %s: segmento LOAD con align %d (< %d)\n' "$label" "$min" "$PAGE_ALIGN"
    failures=$((failures + 1))
  fi
}

check_extracted_dir() {
  local dir="$1" so rel found=0
  while IFS= read -r -d '' so; do
    found=1
    rel="${so#"$dir"/}"
    check_so "$so" "$rel"
  done < <(find "$dir" -type f -name '*.so' -print0 2>/dev/null)
  [ "$found" -eq 1 ] || printf '  (sin bibliotecas nativas .so)\n'
}

extract_matching() {
  # Extrae de un ZIP las entradas cuyo nombre casa con la regex $3. Se listan con "unzip -Z1" y se
  # extraen por nombre exacto: el comodin "*" de unzip no cruza "/" en todas las compilaciones
  # (p. ej. la de Git Bash), y un comodin que no casa haria pasar la compuerta en falso.
  local archive="$1" dest="$2" regex="$3" listing
  mkdir -p "$dest"
  if ! listing="$(unzip -Z1 "$archive" 2>/dev/null)"; then
    printf '  [ERROR] no se pudo leer el ZIP %s\n' "$archive"
    failures=$((failures + 1))
    return 1
  fi
  local names=() name
  while IFS= read -r name; do
    [ -n "$name" ] && names+=("$name")
  done < <(printf '%s\n' "$listing" | grep -E "$regex" || true)
  [ "${#names[@]}" -gt 0 ] || return 0
  # Los corchetes son comodines para unzip: se escapan.
  local escaped=()
  for name in "${names[@]}"; do escaped+=("$(printf '%s' "$name" | sed 's/[][]/\\&/g')"); done
  if ! unzip -qq -o "$archive" "${escaped[@]}" -d "$dest" >/dev/null 2>&1; then
    printf '  [ERROR] no se pudieron extraer las entradas de %s\n' "$archive"
    failures=$((failures + 1))
    return 1
  fi
  return 0
}

extract_so() {
  extract_matching "$1" "$2" '\.so$'
}

check_zipalign_apk() {
  local apk="$1" label="$2"
  if [ -z "$ZIPALIGN_BIN" ]; then
    [ "${CHECK16_REQUIRE_ZIPALIGN:-0}" = "1" ] && die 3 "ERROR: CHECK16_REQUIRE_ZIPALIGN=1 y falta zipalign (build-tools >= 35) para comprobar $label. Define ZIPALIGN o ANDROID_HOME."
    if [ "${CHECK16_SKIP_ZIPALIGN:-0}" = "1" ]; then
      printf '  [AVISO] zipalign omitido para %s (CHECK16_SKIP_ZIPALIGN=1)\n' "$label"; return 0
    fi
    die 3 "ERROR: falta zipalign (build-tools >= 35) para comprobar $label. Define ZIPALIGN o ANDROID_HOME, o usa CHECK16_SKIP_ZIPALIGN=1."
  fi
  local out
  out="$("$ZIPALIGN_BIN" -c -P 16 -v 4 "$apk" 2>&1)"
  local rc=$?
  if [ "$rc" -eq 0 ] && printf '%s' "$out" | grep -q 'Verification successful'; then
    printf '  [OK]    zipalign -c -P 16 -v 4 %s\n' "$label"
  else
    printf '  [FALLA] zipalign -c -P 16 -v 4 %s (salida %d)\n' "$label" "$rc"
    printf '%s\n' "$out" | grep -v -E '\(OK' | tail -n 15 | sed 's/^/          /'
    failures=$((failures + 1))
  fi
}

check_apk() {
  local apk="$1" label="$2" work="$3"
  extract_so "$apk" "$work/so"
  check_extracted_dir "$work/so"
  check_zipalign_apk "$apk" "$label"
}

n=0
for input in "$@"; do
  n=$((n + 1))
  work="$TMP_ROOT/in$n"
  mkdir -p "$work"
  if [ -d "$input" ]; then
    printf '== Carpeta %s\n' "$input"
    check_extracted_dir "$input"
    continue
  fi
  [ -f "$input" ] || die 2 "ERROR: no existe: $input"
  case "$input" in
    *.apk)
      printf '== APK %s\n' "$input"
      check_apk "$input" "$(basename "$input")" "$work"
      ;;
    *.apks)
      printf '== APK set %s\n' "$input"
      extract_matching "$input" "$work/set" '\.apk$' || die 2 "ERROR: no se pudo descomprimir $input"
      [ -n "$(find "$work/set" -type f -name '*.apk' -print -quit)" ] || die 2 "ERROR: $input no contiene ningun .apk"
      while IFS= read -r -d '' apk; do
        check_apk "$apk" "$(basename "$input"):${apk#"$work/set"/}" "$work/$(basename "$apk").d"
      done < <(find "$work/set" -type f -name '*.apk' -print0)
      ;;
    *.aab)
      printf '== AAB %s\n' "$input"
      extract_so "$input" "$work/so"
      check_extracted_dir "$work/so"
      if [ -n "${BUNDLETOOL_JAR:-}" ] && command -v java >/dev/null 2>&1; then
        jar="$(to_unix_path "$BUNDLETOOL_JAR")"
        if java -jar "$jar" build-apks --bundle="$input" --output="$work/universal.apks" --mode=universal >/dev/null 2>"$work/bt.err"; then
          unzip -qq -o "$work/universal.apks" 'universal.apk' -d "$work/u" >/dev/null 2>&1 || die 3 "ERROR: el .apks de bundletool no contiene universal.apk"
          check_zipalign_apk "$work/u/universal.apk" "universal.apk (bundletool)"
        else
          printf '  [ERROR] bundletool build-apks fallo:\n'; tail -n 10 "$work/bt.err" | sed 's/^/          /'
          failures=$((failures + 1))
        fi
      else
        [ "${CHECK16_REQUIRE_ZIPALIGN:-0}" = "1" ] && die 3 "ERROR: CHECK16_REQUIRE_ZIPALIGN=1 y falta BUNDLETOOL_JAR o java para comprobar el APK universal."
        printf '  [AVISO] zipalign no aplica al .aab; define BUNDLETOOL_JAR (y java) para comprobar el APK universal.\n'
      fi
      ;;
    *)
      die 2 "ERROR: formato no soportado (usa .aab, .apk, .apks o una carpeta): $input"
      ;;
  esac
done

printf '\nLector ELF: %s (%s). zipalign: %s\n' "$READELF_BIN" "$READELF_KIND" "${ZIPALIGN_BIN:-no encontrado}"
printf 'Bibliotecas de 64 bits comprobadas: %d. Omitidas (32 bits): %d. Fallos: %d.\n' "$checked" "$skipped32" "$failures"
if [ "$failures" -gt 0 ]; then
  printf 'FALLA: hay bibliotecas o APK sin alinear a 16 KB. Actualiza o quita el SDK/adaptador culpable antes de subir (research/b sec. 3).\n'
  exit 1
fi
if [ "$checked" -eq 0 ] && [ "${CHECK16_ALLOW_NO_NATIVE:-0}" != "1" ]; then
  printf 'FALLA: ninguna biblioteca nativa de 64 bits comprobada (build IL2CPP ARM64?). Usa CHECK16_ALLOW_NO_NATIVE=1 solo si es lo esperado.\n'
  exit 1
fi
printf 'OK: todo alineado a 16 KB.\n'
exit 0
