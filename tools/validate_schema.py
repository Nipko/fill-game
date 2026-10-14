#!/usr/bin/env python3
"""
validate_schema.py - Amazon Fill (Tintalú): comprueba schemas/af-level-1.schema.json (formato af-level/1, 08 §4.1).

Uso (desde la raiz del repositorio):
    python tools/validate_schema.py                       # casos del contrato 21 (N/N PASS)
    python tools/validate_schema.py --pack <archivo> ...  # valida paquetes arbitrarios contra el esquema

Casos (contrato 21 del GDD; docs/gdd/08-arquitectura-tecnica-publicacion.md §4.1):
    (0) el esquema es un JSON Schema draft 2020-12 valido;
    (a) el ejemplo canonico de 08 §4.1 (nivel 43, debut de Hoja), extraido del capitulo a
        tools/out/schema_example_08_4_1.json, valida, y es coherente con las reglas de debut de 02 §9.2
        y con el criterio de alivio de tools/level-qa (base < 0,20: par menor o vacio extra; 03 §17 #8);
    (b) attrs de Lluvia {type: lluvia, key: 10, n: 2, k: 3} y de Pirana {type: pirana, k: 4} validan
        (sueltos y dentro de un pack); "barro" no es un tipo de attr;
    (c) un tablero minimo del Reto del rio (kind daily, 03 §16) valida; los formatos cruzados fallan;
    (d) un nivel >= 31 sin relief falla (y uno < 31 con relief [] pasa); par nulo solo en plus_empty;
    (e) pack_schema 2 (D-007, contrato 21): tipo matapi, Liana con key o n (uno y solo uno), campos obligatorios por
        tipo (hoja: key; remolino: partner; lluvia: key y n), intro_mechanic matapi/chica/alta/murky [P] y
        difficulty.par_approx [P]; con pack_schema 1 nada de eso valida y todo lo demas sigue igual; una version que
        el esquema no conoce (pack_schema 4) falla;
    (f) pack_schema 3 (D-019, punto 9; contrato 21): la Tinaja (M13) con los tipos tinaja y cucharon, sin campos, e
        intro_mechanic tinaja; murky = true y hidden con las capas de la tinaja menos la boca (como mucho 4 pares, como
        siempre); el 3 conserva todo lo del 2 (campos obligatorios por tipo, matapi, intro_mechanic y par_approx); tinaja,
        cucharon e intro_mechanic tinaja fallan con pack_schema 1 y 2. Mascara de la tinaja en relief (decision del dueno
        del 2026-10-14, opcion a de la revision de P2a; 08 §4.1): con pack_schema 3, relief_board lleva hidden si y solo
        si su attrs contiene la tinaja (obligatorio y no vacio, como mucho 4 pares); sin la tinaja (Aguas turbias
        incluidas) y con pack_schema 1 y 2, relief_board no admite hidden.

Imprime PASS/FAIL por caso y termina con codigo 1 si alguno falla. Requiere jsonschema >= 4.18.

Modo --pack (lo usan tools/level-generator, tools/level-qa y el CI): valida cada archivo contra el esquema, imprime
una linea ASCII por archivo (PASS o FAIL con los primeros errores) y termina con 0 si todos validan, 1 si alguno
falla y 2 si no se indica ningun archivo. No reescribe tools/out/.
"""
from __future__ import annotations

import argparse
import copy
import json
import re
import sys
import unicodedata
from collections import Counter
from pathlib import Path

from jsonschema import Draft202012Validator, FormatChecker
from referencing import Registry, Resource

ROOT = Path(__file__).resolve().parent.parent
SCHEMA_PATH = ROOT / "schemas" / "af-level-1.schema.json"
DOC_PATH = ROOT / "docs" / "gdd" / "08-arquitectura-tecnica-publicacion.md"
EXAMPLE_OUT = ROOT / "tools" / "out" / "schema_example_08_4_1.json"

try:
    sys.stdout.reconfigure(encoding="utf-8")
except (AttributeError, ValueError):
    pass

RESULTS: list[bool] = []


def report(case_id: str, desc: str, ok: bool, details: list[str] | None = None) -> None:
    print(f"{'PASS' if ok else 'FAIL'}  ({case_id}) {desc}")
    for line in details or []:
        print(f"        {line}")
    RESULTS.append(ok)


def check(validator, case_id: str, desc: str, instance, expect_valid: bool) -> None:
    errors = sorted(validator.iter_errors(instance), key=lambda e: list(e.absolute_path))
    ok = (not errors) == expect_valid
    details = []
    if expect_valid and errors:
        for e in errors[:6]:
            path = "/".join(str(p) for p in e.absolute_path) or "(raiz)"
            details.append(f"- {path}: {e.message[:200]}")
    elif not expect_valid and not errors:
        details.append("- el esquema lo acepto")
    elif not expect_valid:
        details.append(f"rechazo: {errors[0].message[:160]}")
    report(case_id, f"{desc} [esperado: {'valido' if expect_valid else 'invalido'}]", ok, details)


def extract_example(md_text: str) -> dict:
    """Primer bloque ```json despues del encabezado de 08 §4.1."""
    start = md_text.index("### 4.1")
    match = re.search(r"```json\n(.*?)\n```", md_text[start:], re.S)
    if not match:
        raise ValueError("no se encontro el bloque ```json de 08 §4.1")
    return json.loads(match.group(1))


def debut_coherence(pack: dict) -> list[str]:
    """Contrato 21 y reglas de debut de 02 §9.2 sobre el ejemplo canonico (lo que el esquema no expresa)."""
    problems = []
    lv = pack["levels"][0]
    expected = {"id": "m03_v1_n0043", "level": 43, "seed": "amazonfill:v1:m03:v1:n0043",
                "intro_mechanic": "hoja", "skippable": False}
    for key, value in expected.items():
        if lv.get(key) != value:
            problems.append(f"{key} = {lv.get(key)!r}, se esperaba {value!r}")
    if lv["difficulty"]["band"] != "comfort":
        problems.append("band distinto de comfort")
    if ((lv["level"] - 1) % 20) + 1 != 3:
        problems.append("el nivel no cae en la posicion 3 del ciclo de 20")
    if lv["difficulty"]["bot_fail"] >= 0.20:
        problems.append("fallo del bot >= 20 % en un tablero de debut (02 §9.2 regla 2)")
    empties = sum(1 for c in lv["cuias"] if not c)
    if empties != 2:
        problems.append(f"{empties} vacios; el debut lleva 2 (02 §9.2 regla 2)")
    mechs = [a for a in lv["attrs"] if a is not None]
    if len(mechs) != 1 or mechs[0]["type"] != "hoja":
        problems.append("el debut lleva una sola mecanica (hoja) y un solo ejemplar")
    elif mechs[0].get("key") not in {u for c in lv["cuias"] for u in c}:
        problems.append("la llave de la Hoja no esta presente en el tablero")
    upc = lv["units_per_color"]
    counts = Counter(u for c in lv["cuias"] for u in c)
    if isinstance(upc, int) and any(n != upc for n in counts.values()):
        problems.append(f"unidades por color distintas de {upc}: {dict(counts)}")
    if len(counts) != len(lv["palette"]):
        problems.append("K (tintes en cuias) distinto de la longitud de palette")
    boards = [("base", lv)] + [(f"relief[{i}]", r) for i, r in enumerate(lv["relief"])]
    for name, board in boards:
        attrs = board.get("attrs") or [None] * len(board["cuias"])
        if not len(board["caps"]) == len(board["cuias"]) == len(attrs):
            problems.append(f"{name}: caps, cuias y attrs de distinta longitud")
        if any(len(c) > cap for c, cap in zip(board["cuias"], board["caps"])):
            problems.append(f"{name}: una cuia supera su capacidad")
    if lv["relief"] and lv["relief"][0]["type"] != "minus_color":
        problems.append("con 2 vacios relief[0] debe ser minus_color (03 §13)")
    # Criterio de alivio de tools/level-qa (08 §4.1; 03 §17 #8): con base bot_fail >= 0,20 cada variante
    # baja >= 0,20; con base < 0,20 (debuts de 02 §9.2) basta par menor en minus_color o el vacio extra de plus_empty
    base_fail = lv["difficulty"]["bot_fail"]
    base_par = lv["difficulty"]["par"]
    for i, r in enumerate(lv["relief"]):
        if base_fail >= 0.20:
            if r["bot_fail"] > base_fail - 0.20 + 1e-9:
                problems.append(f"relief[{i}]: bot_fail {r['bot_fail']} no baja >= 0,20 desde {base_fail}")
        elif r["type"] == "minus_color":
            if not (isinstance(r["par"], int) and r["par"] < base_par):
                problems.append(f"relief[{i}] minus_color: par {r['par']} no es menor que el base ({base_par})")
        elif sum(1 for c in r["cuias"] if not c) != empties + 1:
            problems.append(f"relief[{i}] plus_empty: no anade exactamente un vacio al base")
    return problems


def main() -> int:
    schema = json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))

    # (0) Metaesquema
    try:
        Draft202012Validator.check_schema(schema)
        report("0", "schemas/af-level-1.schema.json cumple el metaesquema draft 2020-12", True)
    except Exception as exc:  # noqa: BLE001 - se informa cualquier error del metaesquema
        report("0", "schemas/af-level-1.schema.json cumple el metaesquema draft 2020-12", False, [str(exc)[:300]])
        return 1

    validator = Draft202012Validator(schema, format_checker=FormatChecker())
    registry = Registry().with_resource(schema["$id"], Resource.from_contents(schema))
    attr_validator = Draft202012Validator({"$ref": schema["$id"] + "#/$defs/attr"}, registry=registry)

    # (a) Ejemplo canonico de 08 §4.1, copiado a tools/out/
    example = extract_example(DOC_PATH.read_text(encoding="utf-8"))
    EXAMPLE_OUT.parent.mkdir(parents=True, exist_ok=True)
    # newline="\n": LF tambien en Windows (.gitattributes); si no, el archivo regenerado sale como modificado
    EXAMPLE_OUT.write_text(json.dumps(example, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    example = json.loads(EXAMPLE_OUT.read_text(encoding="utf-8"))
    check(validator, "a1", f"ejemplo de 08 §4.1 copiado a {EXAMPLE_OUT.relative_to(ROOT).as_posix()} (pack completo)",
          example, True)
    problems = debut_coherence(example)
    report("a2", "ejemplo coherente con el contrato 21, las reglas de debut de 02 §9.2 (nivel 43, Hoja) y el criterio de alivio de level-qa",
           not problems, [f"- {p}" for p in problems])

    # (b) attrs de Lluvia y Pirana (02 §9.1)
    lluvia = {"type": "lluvia", "key": 10, "n": 2, "k": 3}
    pirana = {"type": "pirana", "k": 4}
    check(attr_validator, "b1", "attr de Lluvia {type: lluvia, key: 10, n: 2, k: 3} contra $defs/attr", lluvia, True)
    check(attr_validator, "b2", "attr de Pirana {type: pirana, k: 4} contra $defs/attr", pirana, True)

    def campaign_level(pack: dict, mech: dict, level: int) -> dict:
        p = copy.deepcopy(pack)
        p.update({"pack_id": "mundo_05_v1", "world": 5, "world_id": "w05_loreto", "water_base": "white"})
        lv = p["levels"][0]
        lv.update({"id": f"m05_v1_n{level:04d}", "level": level, "world": 5,
                   "seed": f"amazonfill:v1:m05:v1:n{level:04d}", "water_base": "white",
                   "intro_mechanic": mech["type"]})
        lv["attrs"][3] = dict(mech)
        for board in lv["relief"]:
            board["attrs"][3] = dict(mech)
        return p

    check(validator, "b3", "pack con la Lluvia en attrs[3] (nivel 143, debut)", campaign_level(example, lluvia, 143), True)
    check(validator, "b4", "pack con la Pirana en attrs[3] (nivel 183, debut)", campaign_level(example, pirana, 183), True)
    check(attr_validator, "b5", "attr {type: barro}: el Barro es la unidad 15, no un attr", {"type": "barro"}, False)
    check(attr_validator, "b6", "attr de Lluvia con k = 0 (k >= 1)", {"type": "lluvia", "key": 10, "n": 2, "k": 0}, False)
    check(attr_validator, "b7", "attr con el campo antiguo drops", {"type": "lluvia", "key": 10, "drops": 2}, False)
    barro_debut = copy.deepcopy(example)
    lv = barro_debut["levels"][0]
    lv.update({"intro_mechanic": "barro", "attrs": [None] * len(lv["cuias"])})
    lv["cuias"][0] = [8, 15, 3, 5]
    for board in lv["relief"]:
        board["attrs"] = None
    check(validator, "b8", "intro_mechanic = barro con la unidad 15 en cuias y sin attrs", barro_debut, True)

    # (c) Reto del rio: tablero minimo (kind daily, 03 §16)
    colors = [1, 3, 4, 6, 7, 8, 10, 11]
    daily = {
        "pack_schema": 1,
        "schema_file": "schemas/af-level-1.schema.json",
        "pack_id": "daily_pool_v1",
        "kind": "daily",
        "generator": {"tool": "level-generator", "version": "1.3.0"},
        "pool": ["guanabana", "tapereba", "aguaje", "camu_camu", "bacaba", "acai", "agua_tapajos", "lulo"],
        "k_min": 6, "k_max": 11, "max_risk_pairs": 2, "cap_default": 4, "empties_default": 1,
        "levels": [{
            "schema": "af-level/1",
            "id": "d2_n0417",
            "slot": {"band": 2, "day": 417},
            "seed": "amazonfill:v1:daily:b2:d417",
            "gen": {"tool": "level-generator", "version": "1.3.0", "source": "walk", "depth": 18},
            "water_base": "white",
            "palette": ["guanabana", "tapereba", "aguaje", "camu_camu", "bacaba", "acai", "agua_tapajos", "lulo"],
            "units_per_color": 4,
            "caps": [4] * 9,
            "cuias": [[colors[(i + 2 * k) % 8] for k in range(4)] for i in range(8)] + [[]],
            "attrs": [None] * 9,
            "murky": False, "hidden": [],
            "risk_pairs": 1,
            "intro_mechanic": None, "skippable": False, "hint_supported": True,
            "relief": [],
        }],
    }
    check(validator, "c1", "tablero diario minimo d2_n0417 / amazonfill:v1:daily:b2:d417, slot, sin difficulty ni world/lap",
          daily, True)
    d = copy.deepcopy(daily)
    d["levels"][0]["difficulty"] = {"label": "normal", "bot_fail": 0.62, "par": 21, "band": "challenge"}
    check(validator, "c2", "tablero diario con difficulty opcional presente", d, True)
    d = copy.deepcopy(daily)
    d["levels"][0]["id"] = "m03_v1_n0043"
    check(validator, "c3", "tablero diario con id de campana", d, False)
    d = copy.deepcopy(daily)
    d["levels"][0]["seed"] = "amazonfill:v1:daily:b4:d417"
    check(validator, "c4", "tablero diario con banda 4 en la seed", d, False)
    d = copy.deepcopy(daily)
    lv = d["levels"][0]
    del lv["slot"]
    lv["level"] = 417
    check(validator, "c5", "tablero diario con level en lugar de slot", d, False)
    d = copy.deepcopy(example)
    d["levels"][0]["id"] = "d2_n0417"
    check(validator, "c6", "nivel de campana con id diario", d, False)
    d = copy.deepcopy(example)
    del d["levels"][0]["difficulty"]
    check(validator, "c7", "nivel de campana sin difficulty (solo es opcional en daily)", d, False)

    # (d) relief obligatoria desde el nivel 31 (if/then) y par nulo solo en plus_empty
    d = copy.deepcopy(example)
    d["levels"][0]["relief"] = []
    check(validator, "d1", "nivel 43 con relief = [] (obligatoria desde el 31)", d, False)
    d = copy.deepcopy(example)
    del d["levels"][0]["relief"]
    check(validator, "d2", "nivel 43 sin la clave relief", d, False)
    d = copy.deepcopy(example)
    d.update({"pack_id": "mundo_02_v1", "world": 2, "world_id": "w02_tapajos", "water_base": "clear"})
    lv = d["levels"][0]
    lv.update({"id": "m02_v1_n0030", "level": 30, "world": 2, "seed": "amazonfill:v1:m02:v1:n0030",
               "water_base": "clear", "relief": []})
    check(validator, "d3", "nivel 30 con relief = [] (antes del 31)", d, True)
    d = copy.deepcopy(example)
    d["levels"][0]["relief"][0]["par"] = None
    check(validator, "d4", "relief minus_color con par nulo (solo plus_empty admite null)", d, False)
    d = copy.deepcopy(example)
    d["levels"][0]["difficulty"]["D"] = -0.83
    check(validator, "d5", "difficulty.D negativo (suma de z-scores, 03 §7)", d, True)

    # (e) pack_schema 2 (D-007; contrato 21; 08 §4.1 y §4.4)
    def pack2_level(world: int, world_id: str, water: str, level: int, mech: dict | None, intro, pack: int = 2) -> dict:
        """El ejemplo de 08 §4.1 movido al nivel indicado, con mech en attrs[3] (la cuia vacia) del base y del alivio."""
        p = copy.deepcopy(example)
        p.update({"pack_schema": pack, "pack_id": f"mundo_{world:02d}_v1", "world": world, "world_id": world_id,
                  "water_base": water})
        lv = p["levels"][0]
        lv.update({"id": f"m{world:02d}_v1_n{level:04d}", "level": level, "world": world,
                   "seed": f"amazonfill:v1:m{world:02d}:v1:n{level:04d}", "water_base": water, "intro_mechanic": intro})
        lv["attrs"][3] = copy.deepcopy(mech)
        for board in lv["relief"]:
            board["attrs"][3] = copy.deepcopy(mech)
        return p

    matapi = {"type": "matapi"}
    check(validator, "e1", "pack 2: debut del Matapi en el 63 (attrs matapi e intro_mechanic matapi)",
          pack2_level(3, "w03_manaos", "black", 63, matapi, "matapi"), True)
    check(validator, "e2", "pack 2: Liana con fruta {type: liana, key: 3} (debut en el 83)",
          pack2_level(4, "w04_trapecio", "white", 83, {"type": "liana", "key": 3}, "liana"), True)
    d = pack2_level(3, "w03_manaos", "black", 43, {"type": "hoja", "key": 4}, "hoja")
    d["levels"][0]["difficulty"]["par_approx"] = True
    check(validator, "e3", "pack 2: difficulty.par_approx = true [P]", d, True)
    d = pack2_level(4, "w04_trapecio", "white", 100, None, "murky")
    d["levels"][0].update({"murky": True, "hidden": [[0, 1], [0, 0]]})
    check(validator, "e4", "pack 2: intro_mechanic = murky en el hito 100 con murky y hidden [P]", d, True)
    check(validator, "e5", "pack 2: Liana con key y n a la vez",
          pack2_level(4, "w04_trapecio", "white", 83, {"type": "liana", "key": 3, "n": 1}, "liana"), False)
    check(validator, "e6", "pack 2: Liana sin key ni n",
          pack2_level(4, "w04_trapecio", "white", 83, {"type": "liana"}, "liana"), False)
    check(validator, "e7", "pack 1: attrs con matapi",
          pack2_level(3, "w03_manaos", "black", 63, matapi, None, pack=1), False)
    check(validator, "e8", "pack 1: intro_mechanic = matapi",
          pack2_level(3, "w03_manaos", "black", 43, {"type": "hoja", "key": 4}, "matapi", pack=1), False)
    d = copy.deepcopy(example)
    d["pack_schema"] = 4
    check(validator, "e9", "pack_schema = 4, una version que el esquema no conoce (el cliente lo rechazaria, 08 §4.4)", d, False)
    check(validator, "e10", "pack 2: Hoja sin key",
          pack2_level(3, "w03_manaos", "black", 43, {"type": "hoja"}, "hoja"), False)
    d = pack2_level(4, "w04_trapecio", "white", 103, {"type": "remolino", "partner": 8}, "remolino")
    for board in [d["levels"][0]] + d["levels"][0]["relief"]:
        board["attrs"][8] = {"type": "remolino"}
    check(validator, "e11", "pack 2: Remolino sin partner", d, False)
    d = copy.deepcopy(example)
    d["levels"][0]["difficulty"]["par_approx"] = False
    check(validator, "e12", "pack 1: difficulty.par_approx (solo pack 2)", d, False)
    check(validator, "e13", "pack 2: Liana de cuenta {type: liana, n: 1} (variante del 243)",
          pack2_level(4, "w04_trapecio", "white", 83, {"type": "liana", "n": 1}, None), True)
    check(validator, "e14", "pack 2: Lluvia sin n",
          pack2_level(5, "w05_loreto", "white", 143, {"type": "lluvia", "key": 10, "k": 3}, "lluvia"), False)
    d = copy.deepcopy(example)
    d["levels"][0]["relief"][1]["attrs"][8] = {"type": "matapi"}
    check(validator, "e15", "pack 1: matapi solo en attrs de relief[1]", d, False)
    check(validator, "e16", "pack 1: intro_mechanic = chica",
          pack2_level(2, "w02_tapajos", "clear", 14, None, "chica", pack=1), False)
    check(validator, "e17", "pack 1: Hoja sin key (pack 1 no exige campos por tipo: todo igual que hoy)",
          pack2_level(3, "w03_manaos", "black", 43, {"type": "hoja"}, "hoja", pack=1), True)

    # (f) pack_schema 3 (D-019, punto 9; contrato 21; 08 §4.1 y §4.4; propuesta de la tinaja, c.4)
    jar_sources = [0, 4, 6, 1, 7]  # cuias del ejemplo de las que salen, en orden, las dosis de la tinaja (sus cimas)

    def tinaja_level(pack: int = 3, level: int = 323, band: str = "comfort", intro="tinaja", doses: int = 3) -> dict:
        """El ejemplo de 08 §4.1 llevado al mundo 7 con la Tinaja (M13; 02 §9.1; propuesta de la tinaja, c.3): las cimas
        de jar_sources pasan a la tinaja (capacidad 5; con estas cuias, T4 en toda la columna), que se anade con su cuia
        chica de 2, vacia y contigua (attrs tinaja y cucharon, sin campos); murky = true y hidden = todas las capas de la
        tinaja menos la boca. Conserva los 2 vacios del ejemplo. relief[0] (minus_color) quita una fruta que no esta en la
        tinaja (03 §13) y relief[1] (plus_empty) anade un vacio al final; las dos llevan la tinaja con su mascara en
        hidden (los mismos pares: la tinaja conserva su indice), la que exige AF.Core (TinajaMechanic.ValidateInitial)."""
        p = copy.deepcopy(example)
        p.update({"pack_schema": pack, "pack_id": "mundo_07_v1", "world": 7, "world_id": "w07_madre_de_dios",
                  "water_base": "white"})
        lv = p["levels"][0]
        lv.update({"id": f"m07_v1_n{level:04d}", "level": level, "world": 7, "seed": f"amazonfill:v1:m07:v1:n{level:04d}",
                   "water_base": "white", "intro_mechanic": intro})
        lv["difficulty"]["band"] = band
        cuias = copy.deepcopy(lv["cuias"])
        jar = [cuias[i].pop() for i in jar_sources[:doses]]
        t = len(cuias)
        lv["caps"] = lv["caps"] + [5, 2]
        lv["cuias"] = cuias + [jar, []]
        lv["attrs"] = [None] * t + [{"type": "tinaja"}, {"type": "cucharon"}]
        lv["murky"] = True
        lv["hidden"] = [[t, layer] for layer in range(doses - 1)]
        removed = next(c for c in (8, 10, 7) if c not in jar)
        lv["relief"] = [
            {"type": "minus_color", "removed": removed, "caps": list(lv["caps"]),
             "cuias": [[u for u in c if u != removed] for c in cuias] + [list(jar), []],
             "attrs": copy.deepcopy(lv["attrs"]), "hidden": copy.deepcopy(lv["hidden"]), "par": 19, "bot_fail": 0.0},
            {"type": "plus_empty", "caps": lv["caps"] + [4], "cuias": copy.deepcopy(lv["cuias"]) + [[]],
             "attrs": copy.deepcopy(lv["attrs"]) + [None], "hidden": copy.deepcopy(lv["hidden"]), "par": None,
             "bot_fail": 0.0},
        ]
        return p

    def without_jar_attrs(p: dict) -> dict:
        """Quita tinaja y cucharon de attrs (base y relief) y la mascara de las variantes: el mismo tablero sin la
        mecanica (las variantes de Aguas turbias van sin hidden)."""
        lv = p["levels"][0]
        for board in [lv] + lv["relief"]:
            board["attrs"] = [None if a and a["type"] in ("tinaja", "cucharon") else a for a in board["attrs"]]
        for board in lv["relief"]:
            board.pop("hidden", None)
        return p

    check(validator, "f1", "pack 3: estreno de la Tinaja en el 323 (p3, 3 dosis y 2 ocultas; intro_mechanic tinaja; "
          "relief con la tinaja y su mascara en hidden)", tinaja_level(), True)
    check(validator, "f2", "pack 3: tinaja de 5 dosis con 4 ocultas en el hito 340 (el tope de hidden, tambien en las "
          "dos variantes de relief)", tinaja_level(level=340, band="milestone", intro=None, doses=5), True)
    d = tinaja_level(level=340, band="milestone", intro=None, doses=5)
    d["levels"][0]["hidden"].append([9, 4])
    check(validator, "f3", "pack 3: hidden con las 5 capas de la tinaja, boca incluida (hidden sigue en 4 pares como mucho)",
          d, False)
    check(validator, "f4", "pack 2: el estreno de la Tinaja del 323 con pack_schema 2", tinaja_level(pack=2), False)
    check(validator, "f5", "pack 2: tinaja y cucharon en attrs, sin intro_mechanic", tinaja_level(pack=2, intro=None), False)
    d = tinaja_level(pack=2, intro=None)
    for board in [d["levels"][0]] + d["levels"][0]["relief"]:
        board["attrs"][9] = None
    for board in d["levels"][0]["relief"]:
        board.pop("hidden")
    check(validator, "f6", "pack 2: cucharon en attrs, sin tinaja", d, False)
    check(validator, "f7", "pack 2: el mismo tablero sin tinaja ni cucharon (murky y hidden, como Aguas turbias)",
          without_jar_attrs(tinaja_level(pack=2, intro=None)), True)
    check(validator, "f8", "pack 2: intro_mechanic = tinaja sin la mecanica en attrs",
          without_jar_attrs(tinaja_level(pack=2)), False)
    d = without_jar_attrs(tinaja_level(pack=2, intro=None))
    d["levels"][0]["relief"][1]["attrs"][9] = {"type": "tinaja"}
    check(validator, "f9", "pack 2: tinaja solo en attrs de relief[1]", d, False)
    check(validator, "f10", "pack 1: el mismo tablero sin tinaja ni cucharon (murky y hidden, como Aguas turbias)",
          without_jar_attrs(tinaja_level(pack=1, intro=None)), True)
    check(validator, "f11", "pack 1: tinaja y cucharon en attrs, sin intro_mechanic", tinaja_level(pack=1, intro=None), False)
    d = tinaja_level(pack=1, intro=None)
    for board in [d["levels"][0]] + d["levels"][0]["relief"]:
        board["attrs"][9] = None
    for board in d["levels"][0]["relief"]:
        board.pop("hidden")
    check(validator, "f12", "pack 1: cucharon en attrs, sin tinaja", d, False)
    check(validator, "f13", "pack 1: intro_mechanic = tinaja sin la mecanica en attrs",
          without_jar_attrs(tinaja_level(pack=1)), False)
    d = tinaja_level()
    for board in [d["levels"][0]] + d["levels"][0]["relief"]:
        board["attrs"][9]["partner"] = 10
    check(validator, "f14", "pack 3: tinaja con un campo {type: tinaja, partner: 10} (va sin campos)", d, False)
    d = tinaja_level()
    d["levels"][0]["attrs"][10]["key"] = 4
    check(validator, "f15", "pack 3: cucharon con un campo {type: cucharon, key: 4} (va sin campos)", d, False)
    check(validator, "f16", "pack 3: Hoja sin key (el 3 conserva los campos obligatorios del 2)",
          pack2_level(3, "w03_manaos", "black", 43, {"type": "hoja"}, "hoja", pack=3), False)
    check(validator, "f17", "pack 3: Liana con key y n a la vez",
          pack2_level(4, "w04_trapecio", "white", 83, {"type": "liana", "key": 3, "n": 1}, "liana", pack=3), False)
    check(validator, "f18", "pack 3: debut del Matapi en el 63 (lo del 2 sigue valiendo)",
          pack2_level(3, "w03_manaos", "black", 63, matapi, "matapi", pack=3), True)
    check(validator, "f19", "pack 3: Liana de cuenta {type: liana, n: 1}",
          pack2_level(4, "w04_trapecio", "white", 83, {"type": "liana", "n": 1}, None, pack=3), True)
    d = pack2_level(4, "w04_trapecio", "white", 100, None, "murky", pack=3)
    d["levels"][0].update({"murky": True, "hidden": [[0, 1], [0, 0]]})
    d["levels"][0]["difficulty"]["par_approx"] = False
    check(validator, "f20", "pack 3: intro_mechanic = murky en el hito 100 y difficulty.par_approx [P]", d, True)
    check(attr_validator, "f21", "attr {type: tinaja} contra $defs/attr", {"type": "tinaja"}, True)
    check(attr_validator, "f22", "attr {type: cucharon} contra $defs/attr", {"type": "cucharon"}, True)

    # Mascara de la tinaja en relief (decision del dueno del 2026-10-14, opcion a de la revision de P2a; 08 §4.1):
    # con pack_schema 3, hidden en relief_board si y solo si su attrs contiene la tinaja.
    d = tinaja_level()
    d["levels"][0]["relief"][0].pop("hidden")
    check(validator, "f23", "pack 3: variante minus_color con la tinaja y sin hidden (la mascara es obligatoria)", d, False)
    d = tinaja_level()
    d["levels"][0]["relief"][1]["hidden"] = []
    check(validator, "f24", "pack 3: variante plus_empty con la tinaja y hidden = []", d, False)
    d = tinaja_level(level=340, band="milestone", intro=None, doses=5)
    d["levels"][0]["relief"][1]["hidden"].append([9, 4])
    check(validator, "f25", "pack 3: variante con las 5 capas de la tinaja en hidden, boca incluida (4 pares como mucho)",
          d, False)
    d = pack2_level(4, "w04_trapecio", "white", 100, None, "murky", pack=3)
    d["levels"][0].update({"murky": True, "hidden": [[0, 1], [0, 0]]})
    d["levels"][0]["relief"][0]["hidden"] = [[0, 1], [0, 0]]
    check(validator, "f26", "pack 3: hidden en una variante de Aguas turbias, sin tinaja (van sin ocultos)", d, False)
    d = tinaja_level()
    d["levels"][0]["relief"][0]["attrs"] = None
    check(validator, "f27", "pack 3: variante con attrs null y hidden", d, False)
    d = tinaja_level()
    d["levels"][0]["relief"][1]["attrs"][9] = None
    check(validator, "f28", "pack 3: variante con cucharon y sin tinaja, con hidden", d, False)
    d = tinaja_level()
    rb = d["levels"][0]["relief"][1]
    rb.update({"caps": [4] + rb["caps"][:-1], "cuias": [[]] + rb["cuias"][:-1], "attrs": [None] + rb["attrs"][:-1],
               "hidden": [[c + 1, layer] for c, layer in rb["hidden"]]})
    check(validator, "f29", "pack 3: plus_empty con el vacio delante y la mascara con los indices de la variante", d, True)
    d = pack2_level(4, "w04_trapecio", "white", 100, None, "murky")
    d["levels"][0].update({"murky": True, "hidden": [[0, 1], [0, 0]]})
    check(validator, "f30", "pack 2: Aguas turbias del 100 con las variantes sin hidden", copy.deepcopy(d), True)
    d["levels"][0]["relief"][0]["hidden"] = [[0, 1], [0, 0]]
    check(validator, "f31", "pack 2: hidden en una variante de relief (solo con pack_schema 3 y la tinaja)", d, False)
    d = pack2_level(4, "w04_trapecio", "white", 100, None, None, pack=1)
    d["levels"][0].update({"murky": True, "hidden": [[0, 1], [0, 0]]})
    d["levels"][0]["relief"][0]["hidden"] = [[0, 1], [0, 0]]
    check(validator, "f32", "pack 1: hidden en una variante de relief", d, False)

    failed = RESULTS.count(False)
    print(f"\n{len(RESULTS) - failed}/{len(RESULTS)} casos PASS" + (f"; {failed} FAIL" if failed else ""))
    return 1 if failed else 0


def ascii_text(text: str) -> str:
    """Salida ASCII (.claude/rules/ci.md): la consola de Windows corrompe los acentos al capturar la salida."""
    text = text.replace("§", "sec. ").replace("→", "->").replace("≤", "<=").replace("≥", ">=")
    return unicodedata.normalize("NFKD", text).encode("ascii", "ignore").decode("ascii")


def validate_packs(paths: list[str]) -> int:
    """Modo --pack: cada archivo contra el esquema. 0 = todos validan; 1 = alguno falla."""
    schema = json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))
    Draft202012Validator.check_schema(schema)
    validator = Draft202012Validator(schema, format_checker=FormatChecker())
    failed = 0
    for raw in paths:
        path = Path(raw)
        try:
            pack = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, ValueError) as exc:
            print(ascii_text(f"FAIL  {raw}: no se puede leer como JSON ({exc})"))
            failed += 1
            continue
        errors = sorted(validator.iter_errors(pack), key=lambda e: [str(p) for p in e.absolute_path])
        levels = len(pack.get("levels", [])) if isinstance(pack, dict) else 0
        version = pack.get("pack_schema") if isinstance(pack, dict) else None
        if not errors:
            print(ascii_text(f"PASS  {raw}: pack_schema {version}, {levels} niveles"))
            continue
        failed += 1
        print(ascii_text(f"FAIL  {raw}: {len(errors)} error(es) contra schemas/af-level-1.schema.json"))
        for e in errors[:10]:
            where = "/".join(str(p) for p in e.absolute_path) or "(raiz)"
            print(ascii_text(f"        - {where}: {e.message[:200]}"))
    print(f"\n{len(paths) - failed}/{len(paths)} paquetes validos")
    return 1 if failed else 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Comprueba schemas/af-level-1.schema.json (casos del contrato 21) o valida paquetes con --pack.")
    parser.add_argument("--pack", nargs="*", metavar="ARCHIVO", help="valida estos paquetes contra el esquema (sin los casos)")
    args = parser.parse_args()
    if args.pack is not None:
        if not args.pack:
            print("validate_schema.py: --pack necesita al menos un archivo", file=sys.stderr)
            sys.exit(2)
        sys.exit(validate_packs(args.pack))
    sys.exit(main())
