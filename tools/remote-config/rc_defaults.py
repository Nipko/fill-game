#!/usr/bin/env python3
"""
rc_defaults.py - Amazon Fill (Tintalú): genera RemoteConfigDefaults.json desde la tabla única de Remote Config (GDD 08 §9).

08 §9: «Un parámetro, un nombre, un default: RemoteConfigDefaults.json se genera a partir de esta tabla». Este script es ese
generador (bloque B7 de la tanda 5; nota docs/dev/remote-config-analytics.md). Lee las filas de la tabla de
docs/gdd/08-arquitectura-tecnica-publicacion.md §9 y escribe:

- Assets/_Project/Data/Config/RemoteConfigDefaults.json: el archivo de 08 §9 (formato af-rc-defaults/1 [P]);
- Assets/_Project/Scripts/RemoteConfig/RemoteConfigDefaults.Generated.cs: el mismo JSON como constante de C#, para que
  AF.RemoteConfig (sin motor) tenga los defaults compilados sin cargar ningún asset [P].

Uso (desde la raíz del repositorio):
    python tools/remote-config/rc_defaults.py              # escribe los dos archivos
    python tools/remote-config/rc_defaults.py --check      # no escribe; código 1 si alguno difiere de lo que generaría
    python tools/remote-config/rc_defaults.py --self-test  # pruebas propias, en memoria
Opciones: --gdd <ruta de 08>, --out-json <ruta>, --out-cs <ruta>.

Cómo se lee cada fila (| `clave` / `clave2` | tipo / tipo2 | default / default2 | rango | dueño | uso |):
- las claves son lo que va entre comillas invertidas en la primera celda; `x.{a,b}` da x.a y x.b; una plantilla
  `x_<algo>` seguida de sufijos (`a`, `b`) da x_a y x_b; un sufijo que empieza por «_» (`_trail`) sustituye el último
  tramo de la primera clave (live_unlock_level_league → live_unlock_level_trail);
- el tipo y el default se reparten por « / »: uno solo vale para todas las claves de la fila; si no, uno por clave;
- del default se quitan las negritas, las comillas invertidas, las comillas de los extremos y un comentario final entre
  paréntesis («false (1.1: true)» → false; «0 (off)» → 0);
- tipos: int, float, bool, string, json; «enum» se escribe como string y «map país→…» como json.
Las pocas celdas que no son un valor literal (mapas por país en prosa, listas «etiqueta valor», tablas de otro capítulo) se
leen con INTERPRETACIONES, cada una fijada al texto exacto de su celda: si 08 §9 cambia esa celda, el script falla y pide
revisar la interpretación en lugar de adivinar. Un default que remite a otra tabla («tabla 05 §2.3») no se inventa: la
clave sale con "default": null y "pending" con el texto de la celda.

Solo la biblioteca estándar. Python 3.14.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

FORMAT = "af-rc-defaults/1"
SOURCE = "docs/gdd/08-arquitectura-tecnica-publicacion.md §9"
REPO = Path(__file__).resolve().parents[2]
GDD_08 = REPO / "docs" / "gdd" / "08-arquitectura-tecnica-publicacion.md"
OUT_JSON = REPO / "Assets" / "_Project" / "Data" / "Config" / "RemoteConfigDefaults.json"
OUT_CS = REPO / "Assets" / "_Project" / "Scripts" / "RemoteConfig" / "RemoteConfigDefaults.Generated.cs"

TYPES = {"int": "int", "float": "float", "bool": "bool", "string": "string", "json": "json", "enum": "string", "map": "json"}

# Placements de los recompensados con tope y enfriamiento propios: los de la fila ads_rv_cap_<placement> (08 §9), en su orden.
RV_PLACEMENTS = ["double_harvest", "undo_refill", "hint", "reveal", "extra_canoe", "daily_chest"]


class GddError(Exception):
    """La tabla de 08 §9 no se puede leer sin adivinar: el mensaje dice qué fila y por qué."""


# --------------------------------------------------------------------------------------------------------------------
# Interpretaciones: la primera clave de la fila → (texto exacto de la celda de default, función que da [(clave, tipo,
# default, pending)]). Cada una transcribe la prosa de la celda, sin añadir nada que la celda no diga.


def _interp_country_bool(keys, types, cell):
    # «CO, PE, MX = true; resto false»: «resto» se escribe con la clave "*" [P] (nota §3, Q-B7-2).
    return [(keys[0], "json", {"CO": True, "PE": True, "MX": True, "*": False}, None)]


def _interp_legal_profile(keys, types, cell):
    # «BR `br_lgpd_eca` · CO `co_1581` · PE `pe_29733` · MX `mx_lfpdppp` · EEE/UK/CH `ump` · resto `default`».
    value = {
        "BR": "br_lgpd_eca", "CO": "co_1581", "PE": "pe_29733", "MX": "mx_lfpdppp",
        "EEE": "ump", "UK": "ump", "CH": "ump", "*": "default",
    }
    return [(keys[0], "json", value, None)]


def _interp_ua_paid(keys, types, cell):
    # «todos false / false»: el mapa sin países y «todos» = "*" [P]; la segunda clave, false.
    return [(keys[0], "json", {"*": False}, None), (keys[1], "bool", False, None)]


def _interp_pending_all(keys, types, cell):
    parts = [p.strip() for p in cell.split(" / ")]
    if len(parts) != len(keys):
        parts = [cell.strip()] * len(keys)
    return [(k, _type(types, i, len(keys)), None, parts[i]) for i, k in enumerate(keys)]


def _labelled(cell, sep):
    """«chica 14, alta 27, …» o «double_harvest 5 · hint 3 · …» → [(etiqueta, entero)], sin el comentario final."""
    body = _strip_comment(cell)
    out = []
    for item in body.split(sep):
        m = re.fullmatch(r"\s*([a-z_]+)\s+(-?\d+)\s*", item)
        if not m:
            raise GddError("elemento «%s» de «%s» no es «etiqueta valor»" % (item, cell))
        out.append((m.group(1), int(m.group(2))))
    return out


def _interp_mech_debut(keys, types, cell):
    pairs = dict(_labelled(cell, ", "))
    out = []
    for k in keys:
        label = k.split(".", 1)[1]
        if label not in pairs:
            raise GddError("mech_debut_level: falta «%s» en el default" % label)
        out.append((k, "int", pairs[label], None))
    if len(pairs) != len(keys):
        raise GddError("mech_debut_level: el default nombra otras mecánicas que la clave")
    return out


def _interp_rv_cap(keys, types, cell):
    # La plantilla no lleva sufijos en la celda de clave: los placements son las etiquetas del default.
    pairs = _labelled(cell, " · ")
    if [p for p, _ in pairs] != RV_PLACEMENTS:
        raise GddError("ads_rv_cap_<placement>: los placements del default no son los de RV_PLACEMENTS")
    return [("ads_rv_cap_" + p, "int", v, None) for p, v in pairs]


def _interp_rv_cooldown(keys, types, cell):
    # «60 (extra_canoe 90)»: 60 en cada placement de ads_rv_cap_<placement> y 90 en extra_canoe [P] (nota §3, Q-B7-3).
    return [("ads_rv_cooldown_s_" + p, "int", 90 if p == "extra_canoe" else 60, None) for p in RV_PLACEMENTS]


def _interp_price_tier(keys, types, cell):
    # «tabla 05 §4.2 / "B-2026-10-06e"»: la plantilla eco_price_tier_<country> no nombra países; queda pendiente sin clave
    # concreta (va en "pending_templates") y eco_model_version es literal.
    return [(keys[1], "string", "B-2026-10-06e", None)]


def _interp_league(keys, types, cell):
    # «50 / 20 / `{…}` / tablas 06 §6 y §5.2 / 40»: seis claves y cinco valores; «tablas 06 §6 y §5.2» son los premios de
    # la Liga (06 §6) y de la Regata (06 §5.2), que quedan pendientes.
    points = json.loads('{"normal":10,"hard":20,"very_hard":30,"daily":20,"trail":10}')
    text = "tablas 06 §6 y §5.2"
    return [
        (keys[0], "int", 50, None), (keys[1], "int", 20, None), (keys[2], "json", points, None),
        (keys[3], "json", None, text), (keys[4], "json", None, text), (keys[5], "int", 40, None),
    ]


INTERPRETATIONS = {
    "biz_country_enabled": ("CO, PE, MX = true; resto false", _interp_country_bool),
    "biz_legal_profile_by_country": (
        "BR `br_lgpd_eca` · CO `co_1581` · PE `pe_29733` · MX `mx_lfpdppp` · EEE/UK/CH `ump` · resto `default`",
        _interp_legal_profile),
    "biz_ua_paid_enabled_by_country": ("todos false / false", _interp_ua_paid),
    "biz_kpi_targets": ("01 §KPIs / 09 tabla 6.1", _interp_pending_all),
    "mech_debut_level.{chica,alta,hoja,matapi,liana,remolino,barro,lluvia,brote,pirana,tinaja}": (
        "chica 14, alta 27, hoja 43, matapi 63, liana 83, remolino 103, barro 123, lluvia 143, brote 163, pirana 183, tinaja 323",
        _interp_mech_debut),
    "ads_rv_cap_<placement>": (
        "double_harvest 5 · undo_refill 4 · hint 3 · reveal 3 · extra_canoe 3 · daily_chest 1 (ubicación apagada en 1.0: "
        "`eco_calendar_double_ad_enabled`)",
        _interp_rv_cap),
    "ads_rv_cooldown_s_<placement>": ("60 (extra_canoe 90)", _interp_rv_cooldown),
    "eco_calendar_table_v1": (
        "tabla 05 §2.3 (≈3.000 semillas + 12 entradas de comodín + 2 Canastas + 1 Canasta grande)", _interp_pending_all),
    "eco_price_tier_<country>": ("tabla 05 §4.2 / \"B-2026-10-06e\"", _interp_price_tier),
    "league_bucket_size": (
        "50 / 20 / `{\"normal\":10,\"hard\":20,\"very_hard\":30,\"daily\":20,\"trail\":10}` / tablas 06 §6 y §5.2 / 40",
        _interp_league),
}

# Plantillas sin clave concreta en 08 §9 (la clave depende de una lista de otra tabla): no van en "params".
PENDING_TEMPLATES = {"eco_price_tier_<country>": "tabla 05 §4.2"}


# --------------------------------------------------------------------------------------------------------------------
# Lectura de la tabla


def section_9(text: str) -> str:
    text = text.replace("\r\n", "\n")
    start = text.find("\n## 9. ")
    end = text.find("\n## 10. ", start + 1)
    if start < 0 or end < 0:
        raise GddError("no encuentro la sección 9 de 08")
    return text[start:end]


def table_rows(section: str):
    """Filas de la tabla: [(primera celda, tipo, default)] en el orden de 08 §9."""
    rows = []
    for line in section.split("\n"):
        if not line.startswith("| `"):
            continue
        cells = line.split("|")
        if len(cells) != 8:
            raise GddError("fila con %d celdas (se esperan 6): %s" % (len(cells) - 2, line[:80]))
        rows.append((cells[1].strip(), cells[2].strip(), cells[3].strip()))
    if len(rows) < 100:
        raise GddError("solo %d filas en 08 §9: ¿cambió el formato de la tabla?" % len(rows))
    return rows


def expand_keys(cell: str):
    items = re.findall(r"`([^`]+)`", cell)
    if not items:
        raise GddError("sin claves: %s" % cell)
    first = items[0]
    brace = re.fullmatch(r"([a-z0-9_]+)\.\{([a-z0-9_,]+)\}", first)
    if brace:
        if len(items) != 1:
            raise GddError("clave con llaves y más claves: %s" % cell)
        return [brace.group(1) + "." + s for s in brace.group(2).split(",")], first
    template = re.search(r"<[^>]+>", first)
    if template:
        # Los sufijos son lo que va entre comillas invertidas dentro del paréntesis que sigue a la plantilla
        # (`biz_feature_<sistema>` (`daily_challenge`, …)); las demás claves de la celda van tras « / ».
        rest = cell[cell.index("`" + first + "`") + len(first) + 2:]
        group = re.match(r"\s*\(([^()]*)\)", rest)
        if group:
            suffixes = re.findall(r"`([^`]+)`", group.group(1))
            outside = re.findall(r"`([^`]+)`", rest[group.end():])
            return [first.replace(template.group(0), s) for s in suffixes] + outside, first
        return [first] + items[1:], first  # sin sufijos: solo con una interpretación
    keys = []
    for item in items:
        if item.startswith("_"):
            keys.append(first.rsplit("_", 1)[0] + item)
        else:
            keys.append(item)
    for k in keys:
        if not re.fullmatch(r"[a-z0-9_.]+", k):
            raise GddError("clave con caracteres raros: «%s»" % k)
    return keys, first


def _types(cell: str):
    cell = re.sub(r"`[^`]*`", "", cell)
    out = []
    for part in cell.split(" / "):
        word = part.strip().split(" ")[0] if part.strip() else ""
        if word not in TYPES:
            raise GddError("tipo desconocido «%s» en «%s»" % (part, cell))
        out.append(TYPES[word])
    return out


def _type(types, i, n):
    return types[0] if len(types) == 1 else types[i]


def _strip_comment(value: str) -> str:
    return re.sub(r"\s+\([^()]*\)\s*$", "", value.strip())


def _clean(value: str) -> str:
    v = value.replace("**", "").strip()
    v = _strip_comment(v)
    v = v.strip().strip("`").strip()
    if len(v) >= 2 and v[0] == '"' and v[-1] == '"':
        v = v[1:-1]
    return v


def convert(raw: str, type_: str, key: str):
    v = _clean(raw)
    if type_ == "int":
        if not re.fullmatch(r"-?\d+", v):
            raise GddError("%s: «%s» no es un int" % (key, raw))
        return int(v)
    if type_ == "float":
        if not re.fullmatch(r"-?\d+(\.\d+)?", v):
            raise GddError("%s: «%s» no es un float" % (key, raw))
        return float(v)
    if type_ == "bool":
        if v not in ("true", "false"):
            raise GddError("%s: «%s» no es un bool" % (key, raw))
        return v == "true"
    if type_ == "string":
        return v
    if type_ == "json":
        try:
            return json.loads(v)
        except json.JSONDecodeError as e:
            raise GddError("%s: «%s» no es JSON (%s)" % (key, raw, e))
    raise GddError("%s: tipo %s" % (key, type_))


def read_params(gdd_text: str):
    """[(clave, tipo, default, pending)] en el orden de 08 §9. pending es el texto de la celda si no hay valor literal."""
    params = []
    seen = set()
    for key_cell, type_cell, default_cell in table_rows(section_9(gdd_text)):
        keys, first = expand_keys(key_cell)
        types = _types(type_cell)
        if first in INTERPRETATIONS:
            expected, fn = INTERPRETATIONS[first]
            if default_cell != expected:
                raise GddError(
                    "08 §9 cambió el default de «%s»: era «%s» y ahora es «%s»; revisa su interpretación en rc_defaults.py"
                    % (first, expected, default_cell))
            rows = fn(keys, types, default_cell)
        else:
            if len(types) not in (1, len(keys)):
                raise GddError("%s: %d tipos para %d claves" % (first, len(types), len(keys)))
            defaults = default_cell.split(" / ")
            if len(defaults) not in (1, len(keys)):
                raise GddError("%s: %d defaults para %d claves" % (first, len(defaults), len(keys)))
            if any("<" in k for k in keys):
                raise GddError("%s: plantilla sin sufijos ni interpretación" % first)
            rows = []
            for i, k in enumerate(keys):
                d = defaults[0] if len(defaults) == 1 else defaults[i]
                t = _type(types, i, len(keys))
                rows.append((k, t, convert(d, t, k), None))
        for row in rows:
            if row[0] in seen:
                raise GddError("clave repetida en 08 §9: %s" % row[0])
            seen.add(row[0])
            params.append(row)
    return params


# --------------------------------------------------------------------------------------------------------------------
# Escritura


def render_json(params) -> str:
    doc = {
        "format": FORMAT,
        "source": SOURCE,
        "generator": "tools/remote-config/rc_defaults.py",
        "params": {},
        "pending_templates": dict(PENDING_TEMPLATES),
    }
    for key, type_, default, pending in params:
        entry = {"type": type_, "default": default}
        if pending is not None:
            entry["pending"] = pending
        doc["params"][key] = entry
    return json.dumps(doc, ensure_ascii=False, indent=2) + "\n"


def render_cs(json_text: str) -> str:
    body = json_text.replace('"', '""')
    return (
        "// <auto-generated>\n"
        "// Generado por tools/remote-config/rc_defaults.py desde GDD 08 §9. No lo edites a mano: cambia 08 §9 y vuelve a\n"
        "// ejecutar el generador (python tools/remote-config/rc_defaults.py; --check lo comprueba).\n"
        "// </auto-generated>\n"
        "namespace AF.RemoteConfig\n"
        "{\n"
        "    /// <summary>El contenido de <c>Assets/_Project/Data/Config/RemoteConfigDefaults.json</c>, compilado (08 §2.2 y §9).</summary>\n"
        "    internal static class RemoteConfigDefaultsData\n"
        "    {\n"
        "        /// <summary>El JSON, byte a byte igual al archivo (UTF-8, LF).</summary>\n"
        "        internal const string Json = @\"" + body + "\";\n"
        "    }\n"
        "}\n"
    )


def generate(gdd_text: str):
    j = render_json(read_params(gdd_text))
    return j, render_cs(j)


def _write(path: Path, text: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def _read(path: Path):
    if not path.exists():
        return None
    with open(path, "r", encoding="utf-8", newline="") as f:
        return f.read()


# --------------------------------------------------------------------------------------------------------------------
# Autoprueba


def self_test() -> int:
    failures = []

    def check(name, cond):
        if not cond:
            failures.append(name)

    head = "| Clave | Tipo | Default | Rango | Dueño | Uso |\n|---|---|---|---|---|---|\n"
    filler = "".join("| `k_%03d` | int | %d | — | 08 | x |\n" % (i, i) for i in range(100))

    def doc(rows):
        return "# 08\n\n## 9. Parámetros\n\n" + head + rows + filler + "\n## 10. Seguridad\n"

    p = dict((k, (t, d, pe)) for k, t, d, pe in read_params(doc(
        "| `a_x` / `a_y` | int / bool | 3 / **false** (1.1: true) | — | 08 | x |\n"
        "| `m.{p,q}` | bool | true | — | 02 | x |\n"
        "| `f_<s>` (`one`, `two`) | bool | true | — | 09 | x |\n"
        "| `u_league` / `_trail` | int | 41 / 51 | — | 06 | x |\n"
        "| `j` | json `{a: b}` | `{\"h\":0.75}` | — | 03 | x |\n"
        "| `s` / `e` | string / enum | \"\" / `auto` | — | 03 | x |\n"
        "| `n` | int | 0 (off) | — | 03 | x |\n"
        "| `fl` | float | 0.40 | — | 07 | x |\n")))
    check("dos claves y dos tipos", p["a_x"] == ("int", 3, None) and p["a_y"] == ("bool", False, None))
    check("llaves", p["m.p"] == ("bool", True, None) and "m.q" in p)
    check("plantilla", "f_one" in p and "f_two" in p and "f_<s>" not in p)
    check("sufijo con _", p["u_trail"] == ("int", 51, None) and p["u_league"][1] == 41)
    check("json", p["j"] == ("json", {"h": 0.75}, None))
    check("cadena vacía y enum", p["s"] == ("string", "", None) and p["e"] == ("string", "auto", None))
    check("comentario quitado", p["n"] == ("int", 0, None))
    check("float", p["fl"] == ("float", 0.4, None))

    for bad, why in [
        ("| `b` | int | tabla 05 | — | 05 | x |\n", "int no literal"),
        ("| `b` | número | 1 | — | 05 | x |\n", "tipo desconocido"),
        ("| `b` / `c` | int | 1 / 2 / 3 | — | 05 | x |\n", "defaults de más"),
        ("| `b_<x>` | int | 1 | — | 05 | x |\n", "plantilla sin sufijos"),
        ("| `k_000` | int | 1 | — | 05 | x |\n", "clave repetida"),
        ("| `biz_country_enabled` | map país→bool | CO = true | — | 01 | x |\n", "interpretación con otro texto"),
    ]:
        try:
            read_params(doc(bad))
            failures.append("no rechaza: " + why)
        except GddError:
            pass

    # La tabla real: se lee entera, cada interpretación se usa y ninguna clave sale dos veces.
    real = read_params(GDD_08.read_text(encoding="utf-8"))
    keys = [k for k, _, _, _ in real]
    check("tabla real leída", len(keys) > 150)
    check("claves únicas", len(keys) == len(set(keys)))
    check("rc_schema primero", keys[0] == "rc_schema")
    d = dict((k, (t, v, pe)) for k, t, v, pe in real)
    check("cooldown de extra_canoe", d["ads_rv_cooldown_s_extra_canoe"][1] == 90 and d["ads_rv_cooldown_s_hint"][1] == 60)
    check("cap de daily_chest", d["ads_rv_cap_daily_chest"][1] == 1)
    check("tinaja 323", d["mech_debut_level.tinaja"][1] == 323)
    check("pendiente con texto", d["eco_calendar_table_v1"][1] is None and d["eco_calendar_table_v1"][2].startswith("tabla 05"))
    check("biz_feature_lives", d["biz_feature_lives"] == ("bool", True, None))
    check("ni una plantilla en las claves", not any("<" in k or "{" in k for k in keys))
    j, cs = generate(GDD_08.read_text(encoding="utf-8"))
    check("JSON con LF y salto final", "\r" not in j and j.endswith("}\n"))
    check("C# con el JSON", '""format""' in cs)

    if failures:
        for f in failures:
            print("FALLA: " + f)
        return 1
    print("self-test OK (%d claves en 08 §9)" % len(keys))
    return 0


def main(argv) -> int:
    for stream in (sys.stdout, sys.stderr):
        stream.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser(description="Genera RemoteConfigDefaults.json desde GDD 08 §9.")
    ap.add_argument("--check", action="store_true", help="no escribe; código 1 si algún archivo difiere")
    ap.add_argument("--self-test", action="store_true", help="pruebas propias")
    ap.add_argument("--gdd", default=str(GDD_08))
    ap.add_argument("--out-json", default=str(OUT_JSON))
    ap.add_argument("--out-cs", default=str(OUT_CS))
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    try:
        j, cs = generate(Path(args.gdd).read_text(encoding="utf-8"))
    except GddError as e:
        print("ERROR: " + str(e), file=sys.stderr)
        return 1
    targets = [(Path(args.out_json), j), (Path(args.out_cs), cs)]
    if args.check:
        bad = [str(p) for p, t in targets if _read(p) != t]
        for b in bad:
            print("DIFIERE: " + b)
        if bad:
            print("Ejecuta: python tools/remote-config/rc_defaults.py")
            return 1
        print("identico (%d claves)" % len(json.loads(j)["params"]))
        return 0
    for p, t in targets:
        _write(p, t)
    print("escrito %s y %s (%d claves)" % (args.out_json, args.out_cs, len(json.loads(j)["params"])))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
