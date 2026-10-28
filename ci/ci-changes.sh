#!/usr/bin/env bash
# Rutas tocadas para ci.yml (job changes): escribe unity=true|false y core=true|false en $GITHUB_OUTPUT.
#
#   pull_request       BASE...HEAD (punto de bifurcacion del PR), como antes.
#   push a main        TESTED..AFTER, donde TESTED es el head_sha del ultimo run de ci.yml en main que acabo en success
#                      con el job "Unity EditMode + PlayMode" en success (un run donde se salto no cuenta). No vale
#                      github.event.before: si el run de un push que tocaba Unity se cancela (GitHub cancela el run en
#                      espera cuando llega otro, o un PR o release.yml lo desplaza en unity-personal-seat) y despues
#                      llega un push de solo documentacion, before..after no veria lo que quedo sin probar.
#   cualquier otro     todo a true (workflow_dispatch).
#   Falla cerrado: sin run probado, si la API falla, si el commit no se puede traer o no es ancestro de AFTER: todo a true.
#
# Entorno: EVENT, BASE y HEAD (PR), AFTER (github.sha), REPO (owner/nombre), GH_TOKEN (para gh api; el job necesita
# permissions actions: read), GITHUB_OUTPUT. AF_GH sustituye al comando gh (solo para --self-test).
#
# Uso:
#   bash ci/ci-changes.sh
#   bash ci/ci-changes.sh --self-test
#
# Codigos de salida: 0 siempre que escriba las banderas (todo a true incluido); 1 si falla el --self-test; 2 si falta
# GITHUB_OUTPUT o una opcion es desconocida.
set -uo pipefail

UNITY_TEST_JOB="Unity EditMode + PlayMode"
UNITY_RE='^(Assets/|Packages/|ProjectSettings/|ci/|\.github/workflows/)'
CORE_RE='^(Packages/com\.amazonfill\.core/|dotnet/|global\.json|\.github/workflows/)'

all_true() {
  echo "Todo a true: $1"
  { echo "unity=true"; echo "core=true"; } >> "$GITHUB_OUTPUT"
  exit 0
}

have_commit() { git cat-file -e "$1^{commit}" 2>/dev/null; }

ensure_commits() { # trae los commits que falten (historia de hasta 500 commits para el ancestro)
  local missing=0 c
  for c in "$@"; do have_commit "$c" || missing=1; done
  [ "$missing" -eq 0 ] && return 0
  git fetch --no-tags --depth=500 origin "$@" > /dev/null 2>&1 || return 1
  for c in "$@"; do have_commit "$c" || return 1; done
}

# head_sha del ultimo run de ci.yml en main con success y el job de Unity en success; vacio si no hay; 1 si la API falla.
last_tested_sha() {
  local gh="${AF_GH:-gh}" runs id sha conclusion
  runs="$("$gh" api "repos/$REPO/actions/workflows/ci.yml/runs?branch=main&status=success&per_page=50" \
    --jq '.workflow_runs[] | "\(.id) \(.head_sha)"')" || return 1
  while read -r id sha; do
    [ -n "$id" ] || continue
    conclusion="$("$gh" api "repos/$REPO/actions/runs/$id/jobs?per_page=100" \
      --jq ".jobs[] | select(.name == \"$UNITY_TEST_JOB\") | .conclusion")" || return 1
    if [ "$conclusion" = "success" ]; then
      echo "$sha"
      return 0
    fi
  done <<< "$runs"
  return 0
}

decide() {
  local files="$1"
  echo "Rutas tocadas:"
  echo "$files"
  if grep -Eq "$UNITY_RE" <<< "$files"; then echo "unity=true" >> "$GITHUB_OUTPUT"; else echo "unity=false" >> "$GITHUB_OUTPUT"; fi
  if grep -Eq "$CORE_RE" <<< "$files"; then echo "core=true" >> "$GITHUB_OUTPUT"; else echo "core=false" >> "$GITHUB_OUTPUT"; fi
}

main() {
  [ -n "${GITHUB_OUTPUT:-}" ] || { echo "ERROR: falta GITHUB_OUTPUT" >&2; exit 2; }
  local files tested
  case "${EVENT:-}" in
    pull_request)
      ensure_commits "$BASE" "$HEAD" || all_true "no se pudieron traer BASE y HEAD del PR"
      files="$(git diff --name-only "$BASE...$HEAD")" || all_true "git diff del PR fallo"
      ;;
    push)
      [ -n "${AFTER:-}" ] && [ -n "${REPO:-}" ] || all_true "faltan AFTER o REPO"
      tested="$(last_tested_sha)" || all_true "la API de Actions fallo"
      [ -n "$tested" ] || all_true "no hay ningun run de ci.yml en main con los tests de Unity en success"
      echo "Ultimo commit con los tests de Unity en success: $tested"
      ensure_commits "$tested" "$AFTER" || all_true "no se pudo traer $tested"
      git merge-base --is-ancestor "$tested" "$AFTER" || all_true "$tested no es ancestro de $AFTER"
      files="$(git diff --name-only "$tested" "$AFTER")" || all_true "git diff fallo"
      ;;
    *)
      all_true "evento ${EVENT:-desconocido}"
      ;;
  esac
  decide "$files"
}

self_test() {
  local tmp fails=0 c1 c2 c3 c4 out
  tmp="$(mktemp -d)"
  trap 'rm -rf "$tmp"' RETURN
  (
    cd "$tmp" || exit 1
    git init -q repo && cd repo || exit 1
    git config user.email t@t && git config user.name t
    mkdir -p docs Assets dotnet
    echo a > docs/a.md && git add . && git commit -qm c1
    echo b > Assets/b.cs && git add . && git commit -qm c2
    echo c > docs/c.md && git add . && git commit -qm c3
    git checkout -q --orphan otra && echo x > docs/x.md && git add . && git commit -qm c4
  ) > /dev/null 2>&1
  c4="$(git -C "$tmp/repo" rev-parse otra)" # sin historia comun con c1..c3
  c3="$(git -C "$tmp/repo" rev-list --all --grep '^c3$')"
  c2="$(git -C "$tmp/repo" rev-list --all --grep '^c2$')"
  c1="$(git -C "$tmp/repo" rev-list --all --grep '^c1$')"

  # gh falso: AF_FAKE_RUNS son las lineas "id sha" de los runs; AF_FAKE_JOB, la conclusion del job de Unity por run
  # (AF_FAKE_JOB_<id>); AF_FAKE_FAIL=1 hace fallar la API.
  cat > "$tmp/gh" <<'EOS'
#!/usr/bin/env bash
[ "${AF_FAKE_FAIL:-0}" = "1" ] && { echo "HTTP 403" >&2; exit 1; }
case "$2" in
  */jobs*) id="$(printf '%s' "$2" | sed -n 's#.*/runs/\([0-9]*\)/jobs.*#\1#p')"; v="AF_FAKE_JOB_$id"; printf '%s\n' "${!v:-}" ;;
  *) printf '%s\n' "${AF_FAKE_RUNS:-}" ;;
esac
EOS
  chmod +x "$tmp/gh"

  run_case() { # run_case <descripcion> <unity esperado> <core esperado> [VAR=valor...]
    local what="$1" wu="$2" wc="$3"; shift 3
    : > "$tmp/out"
    (cd "$tmp/repo" && env GITHUB_OUTPUT="$tmp/out" AF_GH="$tmp/gh" REPO=o/r EVENT=push AFTER="$c3" "$@" bash "$SCRIPT" > "$tmp/log" 2>&1)
    out="$(tr '\n' ' ' < "$tmp/out")"
    if [ "$out" = "unity=$wu core=$wc " ]; then echo "  ok   $what ($out)"; else echo "  MAL  $what: '$out', se esperaba unity=$wu core=$wc"; sed 's/^/       /' "$tmp/log"; fails=$((fails + 1)); fi
  }

  run_case "solo docs desde el ultimo probado (c2..c3)" false false AF_FAKE_RUNS="7 $c2" AF_FAKE_JOB_7=success
  run_case "Unity sin probar: el ultimo probado es c1 (c1..c3)" true false AF_FAKE_RUNS="7 $c1" AF_FAKE_JOB_7=success
  run_case "run con el job de Unity saltado no cuenta" true false AF_FAKE_RUNS="$(printf '9 %s\n7 %s' "$c2" "$c1")" AF_FAKE_JOB_9=skipped AF_FAKE_JOB_7=success
  run_case "sin ningun run probado" true true AF_FAKE_RUNS="9 $c2" AF_FAKE_JOB_9=skipped
  run_case "la API falla" true true AF_FAKE_FAIL=1
  run_case "el probado no es ancestro" true true AF_FAKE_RUNS="7 $c4" AF_FAKE_JOB_7=success
  run_case "commit probado inexistente" true true AF_FAKE_RUNS="7 0123456789012345678901234567890123456789" AF_FAKE_JOB_7=success
  run_case "workflow_dispatch" true true EVENT=workflow_dispatch
  : > "$tmp/out"
  (cd "$tmp/repo" && env -u GITHUB_OUTPUT bash "$SCRIPT" > /dev/null 2>&1); local rc=$?
  if [ "$rc" -eq 2 ]; then echo "  ok   sin GITHUB_OUTPUT (2)"; else echo "  MAL  sin GITHUB_OUTPUT: $rc"; fails=$((fails + 1)); fi

  if [ "$fails" -eq 0 ]; then echo "self-test OK"; return 0; fi
  echo "self-test con $fails fallos"; return 1
}

SCRIPT="$(cd "$(dirname "$0")" && pwd)/$(basename "$0")"
case "${1:-}" in
  --self-test) self_test; exit $? ;;
  '') main ;;
  *) echo "ERROR: opcion desconocida: $1" >&2; exit 2 ;;
esac
