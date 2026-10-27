#!/usr/bin/env bash
# Veredicto de los tests de Unity de la CI por los XML NUnit, no por el codigo de salida de game-ci.
#
# game-ci v4/v6 puede salir con 1 aunque todo vaya bien: al devolver la licencia Personal por derechos falla con
# "Serial number unavailable for ULF return" (primera ejecucion real de ci.yml y de release.yml, 2026-10-27). Por eso
# el paso de unity-test-runner lleva continue-on-error y este script decide: cada modo pedido tiene un solo XML, con al
# menos un test, sin fallos y con result="Passed" en <test-run>.
#
# Uso:
#   bash ci/check-unity-test-results.sh <carpeta-de-artefactos> [modo...]   (modos por defecto: editmode playmode)
#   bash ci/check-unity-test-results.sh --self-test
#
# Codigos de salida:
#   0  todos los modos tienen su XML y pasan
#   1  falta un XML o hay mas de uno por modo, no tiene <test-run>, total no es un numero > 0, algun test falla
#      o result no es Passed
#   2  argumentos incorrectos o carpeta inexistente
set -uo pipefail

attr() { # attr <linea> <nombre> -> valor del atributo o vacio
  printf '%s' "$1" | sed -n "s/.* $2=\"\\([^\"]*\\)\".*/\\1/p"
}

check_dir() {
  local dir="$1"; shift
  local modes=("$@")
  [ "${#modes[@]}" -gt 0 ] || modes=(editmode playmode)
  if [ ! -d "$dir" ]; then
    echo "ERROR: no existe la carpeta de artefactos: $dir" >&2
    return 2
  fi
  local bad=0 mode count file line total failed passed skipped result
  for mode in "${modes[@]}"; do
    count="$(find "$dir" -maxdepth 2 -type f -iname "*${mode}*.xml" | wc -l | tr -d ' ')"
    if [ "$count" -eq 0 ]; then
      echo "FALLO: no hay XML de resultados de $mode en $dir (Unity no llego a ejecutarlos)."
      bad=1; continue
    fi
    if [ "$count" -gt 1 ]; then
      echo "FALLO: hay $count XML de $mode en $dir; se esperaba uno (no se sabe cual juzgar)."
      bad=1; continue
    fi
    file="$(find "$dir" -maxdepth 2 -type f -iname "*${mode}*.xml")"
    line="$(grep -m 1 -o '<test-run [^>]*>' "$file" || true)"
    if [ -z "$line" ]; then
      echo "FALLO: $file no tiene <test-run> (XML incompleto)."
      bad=1; continue
    fi
    total="$(attr "$line" total)"; failed="$(attr "$line" failed)"; passed="$(attr "$line" passed)"
    skipped="$(attr "$line" skipped)"; result="$(attr "$line" result)"
    echo "$mode: total=${total:-?} passed=${passed:-?} failed=${failed:-?} skipped=${skipped:-?} result=${result:-?} ($file)"
    if ! [[ "$total" =~ ^[0-9]+$ ]] || [ "$total" -eq 0 ]; then
      echo "FALLO: $mode sin tests."; bad=1; continue
    fi
    if ! [[ "$failed" =~ ^[0-9]+$ ]] || [ "$failed" -ne 0 ]; then
      echo "FALLO: $mode con tests fallidos."; bad=1; continue
    fi
    case "$result" in
      Passed*) ;;
      *) echo "FALLO: $mode con result=$result."; bad=1 ;;
    esac
  done
  [ "$bad" -eq 0 ] && echo "OK: los tests de Unity pasan." && return 0
  return 1
}

self_test() {
  local tmp rc fails=0
  tmp="$(mktemp -d)"
  trap 'rm -rf "$tmp"' RETURN
  ok_xml() { printf '<?xml version="1.0"?>\n<test-run id="2" testcasecount="%s" result="Passed" total="%s" passed="%s" failed="0" inconclusive="0" skipped="%s" asserts="0">\n</test-run>\n' "$2" "$2" "$3" "$4" > "$1"; }
  expect() { # expect <codigo> <descripcion> <args...>
    local want="$1" what="$2"; shift 2
    check_dir "$@" > /dev/null 2>&1; rc=$?
    if [ "$rc" -eq "$want" ]; then echo "  ok   $what ($rc)"; else echo "  MAL  $what: $rc, se esperaba $want"; fails=$((fails + 1)); fi
  }
  mkdir -p "$tmp/ok" "$tmp/fail" "$tmp/missing" "$tmp/empty" "$tmp/broken"
  ok_xml "$tmp/ok/editmode-results.xml" 10 9 1
  ok_xml "$tmp/ok/playmode-results.xml" 5 5 0
  expect 0 "los dos modos pasan" "$tmp/ok"
  expect 0 "solo editmode" "$tmp/ok" editmode
  ok_xml "$tmp/fail/editmode-results.xml" 10 10 0
  printf '<test-run id="2" result="Failed(Child)" total="5" passed="4" failed="1" skipped="0">\n</test-run>\n' > "$tmp/fail/playmode-results.xml"
  expect 1 "un test falla" "$tmp/fail"
  ok_xml "$tmp/missing/editmode-results.xml" 10 10 0
  expect 1 "falta playmode" "$tmp/missing"
  ok_xml "$tmp/empty/editmode-results.xml" 0 0 0
  ok_xml "$tmp/empty/playmode-results.xml" 5 5 0
  expect 1 "editmode sin tests" "$tmp/empty"
  printf '<?xml version="1.0"?>\n' > "$tmp/broken/editmode-results.xml"
  ok_xml "$tmp/broken/playmode-results.xml" 5 5 0
  expect 1 "XML sin test-run" "$tmp/broken"
  mkdir -p "$tmp/twice" "$tmp/twice/sub" "$tmp/failed0" "$tmp/nan"
  ok_xml "$tmp/twice/editmode-results.xml" 10 10 0
  ok_xml "$tmp/twice/sub/editmode-results.xml" 10 10 0
  ok_xml "$tmp/twice/playmode-results.xml" 5 5 0
  expect 1 "dos XML de editmode" "$tmp/twice"
  ok_xml "$tmp/failed0/editmode-results.xml" 10 10 0
  printf '<test-run id="2" result="Failed" total="5" passed="5" failed="0" skipped="0">\n</test-run>\n' > "$tmp/failed0/playmode-results.xml"
  expect 1 "result Failed con failed=0" "$tmp/failed0"
  ok_xml "$tmp/nan/editmode-results.xml" 10 10 0
  printf '<test-run id="2" result="Passed" total="cinco" passed="5" failed="0" skipped="0">\n</test-run>\n' > "$tmp/nan/playmode-results.xml"
  expect 1 "total no numerico" "$tmp/nan"
  expect 2 "carpeta inexistente" "$tmp/no-existe"
  if [ "$fails" -eq 0 ]; then echo "self-test OK"; return 0; fi
  echo "self-test con $fails fallos"; return 1
}

case "${1:-}" in
  --self-test) self_test; exit $? ;;
  ''|-h|--help) sed -n '2,16p' "$0"; exit 2 ;;
  -*) echo "ERROR: opcion desconocida: $1" >&2; exit 2 ;;
  *) dir="$1"; shift; check_dir "$dir" "$@"; exit $? ;;
esac
