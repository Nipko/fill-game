#!/usr/bin/env python3
"""
rc_template.py - Amazon Fill (Tintalú): genera la plantilla de Remote Config de Firebase desde la tabla única (GDD 08 §9).

08 §9: «Fuente única: RemoteConfigDefaults.json (defaults compilados) y la plantilla de Firebase». Este script es la
herramienta hermana de rc_defaults.py: lee la misma tabla con el mismo lector y escribe la plantilla en el formato de la
API REST de Remote Config v1 (RemoteConfig: «conditions», «parameters» con «defaultValue», «valueType» y «description»,
«parameterGroups»), que se publica en la consola («Publicar desde un archivo») o con la CLI de Firebase
(firebase deploy --only remoteconfig). Guía del dueño: docs/dev/remote-config-analytics.md §9.

- tools/out/remote_config_template_dev.json: proyecto de desarrollo (fill-dev-e75a2);
- tools/out/remote_config_template_prod.json: proyecto de producción (tintalu).
08 §9 no distingue valores por entorno, así que las dos son iguales byte a byte; van en dos archivos para que cada
proyecto tenga el suyo y un valor por entorno, si algún día 08 §9 lo pide, no cambie la guía.

Cada valor es el default compilado escrito como lo escribe el cliente al dárselo a Firebase
(FirebaseRemoteConfigBackend.DefaultsFor → RcValue.ToRemoteString): publicar la plantilla no cambia el comportamiento.
- int → NUMBER en decimal; float → NUMBER con el formato «R» de .NET (1.0 → "1", 0.05 → "0.05"); bool → BOOLEAN
  "true"/"false"; string → STRING tal cual; json → JSON compacto, sin espacios y con el orden de 08 §9.
- No van (y el script dice cuáles):
  - las claves sin valor literal en 08 §9 ("default": null y "pending" en RemoteConfigDefaults.json): no hay valor que
    publicar, y el cliente tampoco se las da a Firebase como default;
  - las claves que Remote Config no admite: Firebase solo acepta claves de letras inglesas, cifras y «_», que empiezan por
    letra o «_», de hasta 256 caracteres; las `mech_enabled.*` y `mech_debut_level.*` de 08 §9 llevan un punto. Siguen con
    su default compilado y no se pueden cambiar desde la consola hasta que 08 §9 les dé un nombre válido.
- La descripción es «Rango 08 §9: <rango>. <uso>» de la fila, sin marcas de Markdown y cortada a 256 caracteres.

Uso (desde la raíz del repositorio):
    python tools/remote-config/rc_template.py              # escribe las dos plantillas
    python tools/remote-config/rc_template.py --check      # no escribe; código 1 si alguna difiere de lo que generaría
    python tools/remote-config/rc_template.py --self-test  # pruebas propias, en memoria
Opciones: --gdd <ruta de 08>, --out-dir <carpeta>.

Solo la biblioteca estándar. Python 3.14.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import rc_defaults  # noqa: E402

REPO = rc_defaults.REPO
OUT_DIR = REPO / "tools" / "out"
ENVIRONMENTS = ("dev", "prod")  # dev = fill-dev-e75a2, prod = tintalu

# Tipo de RemoteConfigDefaults.json → valueType de la API REST de Remote Config v1.
VALUE_TYPES = {"int": "NUMBER", "float": "NUMBER", "bool": "BOOLEAN", "string": "STRING", "json": "JSON"}

# Límites de Remote Config (08 §9: 3.000 parámetros y 1 M de caracteres en valores; la API: clave ≤ 256 caracteres de
# [A-Za-z0-9_] que empieza por letra o «_», descripción ≤ 256 caracteres).
MAX_PARAMS = 3000
MAX_VALUE_CHARS = 1_000_000
MAX_KEY_LEN = 256
MAX_DESCRIPTION_LEN = 256
KEY_RE = re.compile(r"[A-Za-z_][A-Za-z0-9_]*")


def template_path(out_dir: Path, env: str) -> Path:
    return Path(out_dir) / ("remote_config_template_%s.json" % env)


def valid_key(key: str) -> bool:
    return len(key) <= MAX_KEY_LEN and KEY_RE.fullmatch(key) is not None


def remote_string(type_: str, value) -> str:
    """El valor como texto de Remote Config: el mismo que RcValue.ToRemoteString() del cliente."""
    if type_ == "int":
        if not isinstance(value, int) or isinstance(value, bool):
            raise ValueError("int esperado: %r" % (value,))
        return str(value)
    if type_ == "float":
        f = float(value)
        if f != f or f in (float("inf"), float("-inf")):
            raise ValueError("float no finito: %r" % (value,))
        # .NET «R» (Number.ToString("R")) da el redondeo más corto, como repr, pero sin «.0» en los enteros.
        if f.is_integer() and abs(f) < 1e15:
            return str(int(f))
        return repr(f)
    if type_ == "bool":
        if not isinstance(value, bool):
            raise ValueError("bool esperado: %r" % (value,))
        return "true" if value else "false"
    if type_ == "string":
        if not isinstance(value, str):
            raise ValueError("string esperado: %r" % (value,))
        return value
    if type_ == "json":
        return json.dumps(value, ensure_ascii=False, separators=(",", ":"))
    raise ValueError("tipo desconocido: %s" % type_)


def _plain(text: str) -> str:
    t = text.replace("**", "").replace("`", "")
    return re.sub(r"\s+", " ", t).strip()


def description(range_cell: str, use_cell: str, index: int, count: int) -> str:
    ranges = [r.strip() for r in range_cell.split(" / ")]
    rng = ranges[index] if len(ranges) == count and count > 1 else range_cell
    text = "Rango 08 §9: %s. %s" % (_plain(rng), _plain(use_cell))
    if len(text) > MAX_DESCRIPTION_LEN:
        text = text[: MAX_DESCRIPTION_LEN - 1].rstrip() + "…"
    return text


def build(gdd_text: str):
    """(plantilla como dict, claves sin valor, claves con nombre no admitido) desde 08 §9."""
    parameters = {}
    pending = []
    invalid = []
    for cells, rows in rc_defaults.read_param_rows(gdd_text):
        range_cell, use_cell = cells[3], cells[5]
        for i, (key, type_, default, pend) in enumerate(rows):
            if pend is not None or default is None:
                pending.append(key)
                continue
            if not valid_key(key):
                invalid.append(key)
                continue
            parameters[key] = {
                "defaultValue": {"value": remote_string(type_, default)},
                "description": description(range_cell, use_cell, i, len(rows)),
                "valueType": VALUE_TYPES[type_],
            }
    template = {"conditions": [], "parameters": parameters, "parameterGroups": {}}
    return template, pending, invalid


def render(template) -> str:
    return json.dumps(template, ensure_ascii=False, indent=2) + "\n"


def problems(template) -> list:
    """Lo que la API de Remote Config rechazaría (vacío si nada)."""
    out = []
    params = template.get("parameters", {})
    if set(template) - {"conditions", "parameters", "parameterGroups"}:
        out.append("campos de más en la plantilla: %s" % sorted(set(template) - {"conditions", "parameters", "parameterGroups"}))
    if len(params) > MAX_PARAMS:
        out.append("%d parámetros (máximo %d)" % (len(params), MAX_PARAMS))
    total = 0
    for key, p in params.items():
        if not valid_key(key):
            out.append("%s: clave no admitida" % key)
        if set(p) != {"defaultValue", "description", "valueType"} or set(p["defaultValue"]) != {"value"}:
            out.append("%s: campos inesperados" % key)
            continue
        v = p["defaultValue"]["value"]
        t = p["valueType"]
        if not isinstance(v, str):
            out.append("%s: el valor no es texto" % key)
            continue
        total += len(v)
        if len(p["description"]) > MAX_DESCRIPTION_LEN:
            out.append("%s: descripción de %d caracteres" % (key, len(p["description"])))
        if t == "NUMBER" and not re.fullmatch(r"-?\d+(\.\d+)?([eE][-+]?\d+)?", v):
            out.append("%s: «%s» no es un NUMBER" % (key, v))
        elif t == "BOOLEAN" and v not in ("true", "false"):
            out.append("%s: «%s» no es un BOOLEAN" % (key, v))
        elif t == "JSON":
            try:
                json.loads(v)
            except json.JSONDecodeError:
                out.append("%s: no es JSON válido" % key)
        elif t not in ("NUMBER", "BOOLEAN", "JSON", "STRING"):
            out.append("%s: valueType %s" % (key, t))
    if total > MAX_VALUE_CHARS:
        out.append("%d caracteres en valores (máximo %d)" % (total, MAX_VALUE_CHARS))
    return out


def generate(gdd_text: str):
    """{entorno: texto}, claves sin valor, claves no admitidas. Falla si la plantilla no pasaría la API."""
    template, pending, invalid = build(gdd_text)
    bad = problems(template)
    if bad:
        raise rc_defaults.GddError("la plantilla no pasaría la API: " + "; ".join(bad[:10]))
    text = render(template)
    return {env: text for env in ENVIRONMENTS}, pending, invalid


# --------------------------------------------------------------------------------------------------------------------
# Autoprueba


def self_test(gdd_path: Path = rc_defaults.GDD_08) -> int:
    failures = []

    def check(name, cond):
        if not cond:
            failures.append(name)

    check("int", remote_string("int", 300) == "300")
    check("float entero como .NET R", remote_string("float", 1.0) == "1" and remote_string("float", 2.0) == "2")
    check("float decimal", remote_string("float", 0.05) == "0.05" and remote_string("float", 0.4) == "0.4")
    check("bool", remote_string("bool", False) == "false")
    check("cadena vacía", remote_string("string", "") == "")
    check("json compacto", remote_string("json", {"a": [1, 2.5], "b": "ñ"}) == '{"a":[1,2.5],"b":"ñ"}')
    for t, v in [("int", True), ("bool", 1), ("string", None), ("float", float("nan"))]:
        try:
            remote_string(t, v)
            failures.append("no rechaza %s %r" % (t, v))
        except ValueError:
            pass
    check("clave válida", valid_key("biz_feature_lives") and valid_key("_x1"))
    check("clave con punto", not valid_key("mech_enabled.hoja"))
    check("clave con cifra delante", not valid_key("1abc"))
    check("clave larga", not valid_key("a" * 257) and valid_key("a" * 256))
    check("descripción por clave", description("2-4 / a · b", "Uso", 1, 2) == "Rango 08 §9: a · b. Uso")
    check("descripción cortada", len(description("—", "x" * 400, 0, 1)) == MAX_DESCRIPTION_LEN)
    bad = {"conditions": [], "parameters": {
        "a.b": {"defaultValue": {"value": "1"}, "description": "", "valueType": "NUMBER"},
        "n": {"defaultValue": {"value": "uno"}, "description": "", "valueType": "NUMBER"},
        "j": {"defaultValue": {"value": "{"}, "description": "", "valueType": "JSON"},
        "b": {"defaultValue": {"value": "True"}, "description": "", "valueType": "BOOLEAN"},
    }, "parameterGroups": {}}
    check("problemas detectados", len(problems(bad)) == 4)

    texts, pending, invalid = generate(gdd_path.read_text(encoding="utf-8"))
    check("dev = prod", texts["dev"] == texts["prod"])
    doc = json.loads(texts["dev"])
    check("sin problemas", problems(doc) == [])
    params = doc["parameters"]
    check("rc_schema primero", next(iter(params)) == "rc_schema")
    check("extra_canoe 90", params.get("ads_rv_cooldown_s_extra_canoe", {}).get("defaultValue") == {"value": "90"})
    check("pendientes fuera", "eco_calendar_table_v1" in pending and "eco_calendar_table_v1" not in params)
    check("claves con punto fuera", "mech_enabled.hoja" in invalid and not any("." in k for k in params))
    check("LF y salto final", "\r" not in texts["dev"] and texts["dev"].endswith("}\n"))

    if failures:
        for f in failures:
            print("FALLA: " + f)
        return 1
    print("self-test OK (%d parámetros; %d sin valor y %d con nombre no admitido quedan fuera)"
          % (len(params), len(pending), len(invalid)))
    return 0


def main(argv) -> int:
    for stream in (sys.stdout, sys.stderr):
        stream.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser(description="Genera la plantilla de Remote Config de Firebase desde GDD 08 §9.")
    ap.add_argument("--check", action="store_true", help="no escribe; código 1 si alguna plantilla difiere")
    ap.add_argument("--self-test", action="store_true", help="pruebas propias")
    ap.add_argument("--gdd", default=str(rc_defaults.GDD_08))
    ap.add_argument("--out-dir", default=str(OUT_DIR))
    args = ap.parse_args(argv)
    if not Path(args.gdd).is_file():
        print("ERROR: no existe " + args.gdd, file=sys.stderr)
        return 1
    if args.self_test:
        return self_test(Path(args.gdd))
    try:
        texts, pending, invalid = generate(Path(args.gdd).read_text(encoding="utf-8"))
    except rc_defaults.GddError as e:
        print("ERROR: " + str(e), file=sys.stderr)
        return 1
    count = len(json.loads(texts["dev"])["parameters"])
    targets = [(template_path(Path(args.out_dir), env), texts[env]) for env in ENVIRONMENTS]
    note = "%d parámetros; fuera: %d sin valor (%s) y %d con nombre no admitido (%s)" % (
        count, len(pending), ", ".join(pending), len(invalid), ", ".join(invalid))
    if args.check:
        bad = [str(p) for p, t in targets if rc_defaults._read(p) != t]
        for b in bad:
            print("DIFIERE: " + b)
        if bad:
            print("Ejecuta: python tools/remote-config/rc_template.py")
            return 1
        print("identico (%s)" % note)
        return 0
    for p, t in targets:
        rc_defaults._write(p, t)
    print("escrito %s (%s)" % (", ".join(str(p) for p, _ in targets), note))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
