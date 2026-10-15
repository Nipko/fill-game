#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""stops.py - Mapa del rio (D-013): genera las paradas af-river-stops/1 desde la investigacion.

Lee docs/research/mapa-del-rio/rio-amazonas.json (sin modificarlo) y tools/river-map/overrides.json, y escribe
Assets/_Project/Data/Map/river_stops_v1.json: una entrada por parada (k = n, niveles 10n+1 ... 10n+10) con los campos
de 08 §4.5 (contrato 30) y los extras [P] de P-M1 (a). Solo claves de texto, nunca textos: los textos van en
Data/Loc/facts.csv (contrato 32; los escribe M1.1t). Solo biblioteca estandar de Python 3.14 (08 §4.5).

Uso (desde la raiz del repositorio):
    py -3.14 tools/river-map/stops.py            # escribe river_stops_v1.json
    py -3.14 tools/river-map/stops.py --check    # valida y compara con lo commiteado; codigo 1 si algo falla

Opciones para pruebas y fixtures: --source, --overrides, --worldtable y --out cambian las rutas de entrada y salida.
Determinista: misma entrada, mismos bytes (UTF-8 sin BOM, LF, orden de claves fijo). Mensajes solo en ASCII.

Conversion (paradas_1_200 -> stops[n]; plan del Mapa, M1.1):
- paradas_1_200 cubre k = 0..19 (niveles 1-200, D-013 punto 3); ademas, la validacion exige que el documento termine
  en el ultimo nivel de un mundo, para que ninguno se quede sin su close.
- n = k, levels = niveles, world = mundo, id = id; kind: gran_parada -> gate, parada y variante -> stop,
  castillo -> close.
- country = pais (ISO2); si la parada tiene dos paises, overrides.json decide country y country_name (n = 13).
- km_from_sea = km_desde_el_mar (null en n = 0) y km_home = km_casa.
- sample (agua del sitio, sin el parentesis final): blanca -> white, negra -> black, clara -> clear,
  clara y blanca -> clear_white, negra y blanca -> black_white.
- flag_raise: el pais de cruces_de_pais con llega_en.parada_1_200 = n (BR en 0, CO en 8 y PE en 14).
- arrival_fact: null en n = 0; en n = 1, la clave del dato del registro k = 0 (map.stop.estuario.dato_03, el Dato
  del rio 1 del Mapa del 10, 06 §2.3); desde n = 2, la clave de su propio dato_del_nivel.
- facts = [] (lo llena M2.6 con los «Otro dato»).
- world_question = map.world.wNN.question si la parada lleva pregunta_del_mundo; answers_world_question = true en la
  parada a la que se llega al ganar su se_responde_en.
- bg: kit = fondo.kit; hero = bg_hero_x (sin el sufijo «+ LUT ...»); code = codigo:x o kit:x; lut = dusk o dawn si el
  heroe dice «+ LUT de atardecer» o «+ LUT de alba».
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from riverdata import (  # noqa: E402  (la ruta de la herramienta va antes)
    OVERRIDES_PATH,
    ROOT,
    SOURCE_PATH,
    SOURCE_REL,
    STOPS_PATH,
    WORLDTABLE_PATH,
    SourceError,
    WorldTable,
    ascii_text,
    has_belen,
    iter_strings,
    load_json,
)

FORMAT = "af-river-stops/1"

# paradas_1_200 cubre los niveles 1-200 (D-013, punto 3): 20 paradas, k = 0..19.
LAST_LEVEL_1_200 = 200
STOPS_1_200 = LAST_LEVEL_1_200 // 10

# Orden fijo de los campos de cada parada (08 §4.5 con los extras de P-M1). M2.6 anade «at» tras country_name y «leg»
# al final (el esquema ya los admite).
STOP_FIELDS = (
    "n", "levels", "world", "id", "kind", "country", "country_name", "km_from_sea", "km_home", "sample",
    "flag_raise", "arrival_fact", "facts", "world_question", "answers_world_question", "bg",
)
BG_FIELDS = ("kit", "hero", "code", "lut")

KIND = {"gran_parada": "gate", "parada": "stop", "variante": "stop", "castillo": "close"}
SAMPLE = {
    "blanca": "white",
    "negra": "black",
    "clara": "clear",
    "clara y blanca": "clear_white",
    "negra y blanca": "black_white",
}
SAMPLES = frozenset(SAMPLE.values())
LUT = {"atardecer": "dusk", "alba": "dawn"}
LUTS = frozenset(LUT.values())
BG_TIPOS = ("pintado", "codigo", "kit_y_rotulo", "variante_lut")
OVERRIDE_FIELDS = frozenset({"country", "country_name", "why"})

RE_ID = re.compile(r"[a-z0-9_]+")
RE_ISO2 = re.compile(r"[A-Z]{2}")
RE_HERO = re.compile(r"bg_hero_[a-z0-9_]+")
RE_CODE = re.compile(r"(codigo|kit):[a-z0-9_]+")
RE_FACT_KEY = re.compile(
    r"map\.(stop\.[a-z0-9_]+\.dato_[0-9]{2}|country\.[a-z]{2}\.dato_[0-9]{2}|myth\.[a-z0-9_]+|general\.[a-z0-9_]+)"
)
RE_QUESTION = re.compile(r"map\.world\.w([0-9]{2})\.question")
RE_PAREN = re.compile(r"\s*\([^()]*\)\s*$")
RE_LUT_SUFFIX = re.compile(r"LUT de (\S+)")


def _is_int(value) -> bool:
    return isinstance(value, int) and not isinstance(value, bool)


def _tag_record(record: dict) -> str:
    return f"parada k={record.get('k')!r} ({record.get('id')!r})"


# --------------------------------------------------------------------------------------------- conversion

def _check_overrides(overrides: dict, source_ids: set) -> tuple[dict, list[str]]:
    errors: list[str] = []
    stops = overrides.get("stops", {})
    if not isinstance(stops, dict):
        return {}, ["overrides.json: «stops» tiene que ser un objeto {id de parada: {...}}"]
    clean: dict = {}
    for sid, entry in stops.items():
        if sid not in source_ids:
            errors.append(f"overrides.json: {sid!r} no es ninguna parada de paradas_1_200")
            continue
        if not isinstance(entry, dict):
            errors.append(f"overrides.json: la entrada de {sid!r} no es un objeto")
            continue
        unknown = sorted(set(entry) - OVERRIDE_FIELDS)
        if unknown:
            errors.append(f"overrides.json: {sid!r} lleva campos desconocidos {unknown} (admitidos: country, country_name, why)")
        clean[sid] = entry
    return clean, errors


def _crossings(source: dict, ks: set) -> tuple[dict, list[str]]:
    """Banderas izadas al llegar a una parada de 1-200 (cruces_de_pais con llega_en.parada_1_200)."""
    flags: dict = {}
    errors: list[str] = []
    crossings = source.get("cruces_de_pais", [])
    if not isinstance(crossings, list):
        return flags, ["rio-amazonas.json: cruces_de_pais no es una lista"]
    for c in crossings:
        if not isinstance(c, dict):
            errors.append("cruces_de_pais: una entrada no es un objeto")
            continue
        llega = c.get("llega_en")
        if not isinstance(llega, dict) or "parada_1_200" not in llega:
            continue
        k = llega["parada_1_200"]
        if not _is_int(k) or k not in ks:
            errors.append(f"cruces_de_pais: llega_en.parada_1_200 = {k!r} no es ninguna parada")
        elif k in flags:
            errors.append(f"cruces_de_pais: dos banderas en la parada k={k}")
        else:
            flags[k] = c.get("pais")
    return flags, errors


def _questions(records: list) -> tuple[dict, list[str]]:
    """Nivel de llegada que responde cada pregunta del mundo -> mundo de la pregunta."""
    answer_levels: dict = {}
    errors: list[str] = []
    for r in records:
        q = r.get("pregunta_del_mundo")
        if q is None:
            continue
        se = q.get("se_responde_en") if isinstance(q, dict) else None
        if not _is_int(se):
            errors.append(f"{_tag_record(r)}: pregunta_del_mundo sin se_responde_en entero")
            continue
        if se in answer_levels:
            errors.append(f"{_tag_record(r)}: dos preguntas se responden en el nivel {se}")
            continue
        answer_levels[se] = r.get("mundo")
    arrivals = {r.get("llegada_al_ganar"): r for r in records if _is_int(r.get("llegada_al_ganar"))}
    for se, world in answer_levels.items():
        r = arrivals.get(se)
        if r is None:
            errors.append(f"pregunta del mundo {world}: se_responde_en = {se} no es la llegada de ninguna parada")
        elif r.get("mundo") != world:
            errors.append(f"pregunta del mundo {world}: se responde en {_tag_record(r)}, que es del mundo {r.get('mundo')}")
    return answer_levels, errors


def _background(record: dict, tag: str) -> tuple[dict, list[str]]:
    errors: list[str] = []
    fondo = record.get("fondo")
    if not isinstance(fondo, dict):
        return {"kit": None, "hero": None, "code": None, "lut": None}, [f"{tag}: sin fondo"]
    kit = fondo.get("kit")
    heroe = fondo.get("heroe")
    tipo = fondo.get("tipo")
    hero = code = lut = None
    if not isinstance(heroe, str) or not heroe.strip():
        errors.append(f"{tag}: sin fondo.heroe")
    else:
        parts = [p.strip() for p in heroe.split("+")]
        base = parts[0]
        if base.startswith(("codigo:", "kit:")):
            code = base
        elif base.startswith("bg_hero_"):
            hero = base
        else:
            errors.append(f"{tag}: fondo.heroe {base!r} no es bg_hero_<sitio>, codigo:<pieza> ni kit:<pieza>")
        for extra in parts[1:]:
            m = RE_LUT_SUFFIX.fullmatch(extra)
            if m and m.group(1) in LUT and lut is None:
                lut = LUT[m.group(1)]
            else:
                errors.append(f"{tag}: sufijo de fondo.heroe {extra!r} desconocido (se esperaba «LUT de atardecer» o «LUT de alba»)")
    if tipo not in BG_TIPOS:
        errors.append(f"{tag}: fondo.tipo {tipo!r} desconocido (se esperaba {', '.join(BG_TIPOS)})")
    elif tipo == "pintado" and (hero is None or lut is not None):
        errors.append(f"{tag}: fondo pintado sin bg_hero_<sitio> o con LUT propia")
    elif tipo == "codigo" and not (code or "").startswith("codigo:"):
        errors.append(f"{tag}: fondo por codigo sin codigo:<pieza>")
    elif tipo == "kit_y_rotulo" and not (code or "").startswith("kit:"):
        errors.append(f"{tag}: fondo solo con el kit sin kit:<pieza>")
    elif tipo == "variante_lut" and (hero is None or lut is None):
        errors.append(f"{tag}: variante de luz sin bg_hero_<sitio> + LUT")
    return {"kit": kit, "hero": hero, "code": code, "lut": lut}, errors


def _sample(record: dict, tag: str) -> tuple[str | None, list[str]]:
    agua = record.get("agua_del_sitio")
    if not isinstance(agua, str):
        return None, [f"{tag}: sin agua_del_sitio"]
    key = RE_PAREN.sub("", agua).strip()
    sample = SAMPLE.get(key)
    if sample is None:
        return None, [f"{tag}: agua_del_sitio {agua!r} desconocida (blanca, negra, clara, clara y blanca o negra y blanca)"]
    return sample, []


def convert(source: dict, overrides: dict) -> tuple[dict | None, list[str]]:
    """paradas_1_200 de rio-amazonas.json -> documento af-river-stops/1 (sin validar todavia)."""
    records = source.get("paradas_1_200")
    if not isinstance(records, list) or not records:
        return None, ["rio-amazonas.json: falta paradas_1_200 o esta vacia"]
    errors: list[str] = []
    good = []
    for i, r in enumerate(records):
        if isinstance(r, dict):
            good.append(r)
        else:
            errors.append(f"paradas_1_200[{i}] no es un objeto")
    records = good
    source_ids = {r.get("id") for r in records if isinstance(r.get("id"), str)}
    over, errs = _check_overrides(overrides, source_ids)
    errors += errs
    by_k = {}
    for r in records:
        if _is_int(r.get("k")):
            by_k.setdefault(r.get("k"), r)
    missing = sorted(set(range(STOPS_1_200)) - set(by_k))
    if missing:
        errors.append(f"paradas_1_200 no cubre los niveles 1-{LAST_LEVEL_1_200} (D-013, punto 3): "
                      f"faltan las paradas k={missing}")
    outside = sorted(k for k in by_k if not 0 <= k < STOPS_1_200)
    if outside:
        errors.append(f"paradas_1_200: k={outside} fuera de 0-{STOPS_1_200 - 1} (niveles 1-{LAST_LEVEL_1_200})")
    flags, errs = _crossings(source, set(by_k))
    errors += errs
    answer_levels, errs = _questions(records)
    errors += errs
    first_record = by_k.get(0)

    stops = []
    for r in records:
        k = r.get("k")
        sid = r.get("id")
        tag = _tag_record(r)
        ov = over.get(sid, {}) if isinstance(sid, str) else {}
        if not _is_int(k):
            errors.append(f"{tag}: k no es un entero")

        tipo = r.get("tipo")
        kind = KIND.get(tipo) if isinstance(tipo, str) else None
        if kind is None:
            errors.append(f"{tag}: tipo {tipo!r} desconocido (gran_parada, parada, variante o castillo)")

        country = ov.get("country", r.get("pais"))
        if not (isinstance(country, str) and RE_ISO2.fullmatch(country)):
            errors.append(f"{tag}: pais {country!r} no es un codigo ISO2; con dos paises, lo decide overrides.json")

        sample, errs = _sample(r, tag)
        errors += errs

        flag_raise = flags.get(k) if _is_int(k) else None
        bandera = r.get("bandera")
        if bandera is not None and bandera != flag_raise:
            errors.append(f"{tag}: bandera {bandera!r} y cruces_de_pais dice {flag_raise!r}")

        llegada = r.get("llegada_al_ganar")
        expected_llegada = None if k == 0 else (10 * k if _is_int(k) else "?")
        if llegada != expected_llegada:
            errors.append(f"{tag}: llegada_al_ganar {llegada!r} y la cadencia da {expected_llegada!r} (10k)")
        dato = r.get("dato_del_nivel")
        if k == 0:
            arrival = None  # el inicio no tiene llegada; su dato es el del Mapa del 10 (llegada a n = 1)
        elif k == 1:
            # Mapa del 10 (06 §2.3): al ganar el 10 se llega a la parada 1 con el Dato del rio 1, el del estuario,
            # que vive en el registro k = 0.
            if dato is not None:
                errors.append(f"{tag}: el dato de la llegada del 10 va en el registro k=0 (Mapa del 10, 06 §2.3), no aqui")
            d0 = first_record.get("dato_del_nivel") if first_record else None
            arrival = d0.get("clave") if isinstance(d0, dict) else None
            if arrival is None:
                errors.append(f"{tag}: el registro k=0 no tiene dato_del_nivel.clave para la llegada del 10")
        else:
            arrival = dato.get("clave") if isinstance(dato, dict) else None
            if arrival is None:
                errors.append(f"{tag}: sin dato_del_nivel.clave (dato de llegada)")

        world = r.get("mundo")
        q = r.get("pregunta_del_mundo")
        world_question = f"map.world.w{world:02d}.question" if q is not None and _is_int(world) else None
        if q is not None and world_question is None:
            errors.append(f"{tag}: pregunta_del_mundo sin un mundo entero")

        bg, errs = _background(r, tag)
        errors += errs

        stop = {
            "n": k,
            "levels": r.get("niveles"),
            "world": world,
            "id": sid,
            "kind": kind,
            "country": country,
            "country_name": ov.get("country_name"),
            "km_from_sea": r.get("km_desde_el_mar"),
            "km_home": r.get("km_casa"),
            "sample": sample,
            "flag_raise": flag_raise,
            "arrival_fact": arrival,
            "facts": [],
            "world_question": world_question,
            "answers_world_question": _is_int(llegada) and llegada in answer_levels,
            "bg": bg,
        }
        stops.append(stop)
    doc = {"format": FORMAT, "source": SOURCE_REL, "stops": stops}
    return doc, errors


# --------------------------------------------------------------------------------------------- validacion

def validate(doc: dict, wt: WorldTable) -> list[str]:
    """Reglas de 08 §4.5 y del plan del Mapa sobre el documento generado (las mismas que RiverStopCatalog, M1.2)."""
    errors: list[str] = []
    if doc.get("format") != FORMAT:
        errors.append(f"format {doc.get('format')!r} y se esperaba {FORMAT!r}")
    stops = doc.get("stops")
    if not isinstance(stops, list) or not stops:
        return errors + ["sin paradas"]

    ids: dict = {}
    questions: dict = {}
    answers: dict = {}
    prev_country = None
    gap_reported = False
    for i, s in enumerate(stops):
        n = s.get("n")
        sid = s.get("id")
        tag = f"parada n={n!r} ({sid!r})"
        if tuple(s.keys()) != STOP_FIELDS:
            errors.append(f"{tag}: campos {list(s.keys())} y se esperaban {list(STOP_FIELDS)}")
        if not _is_int(n):
            errors.append(f"{tag}: n no es un entero")
        elif n != i and not gap_reported:
            # Solo el primer salto: las paradas siguientes quedarian todas desplazadas.
            errors.append(f"{tag}: n no contiguo (posicion {i}; las paradas van de 0 en adelante, sin huecos ni repetidas)")
            gap_reported = True

        levels = s.get("levels")
        levels_ok = (isinstance(levels, list) and len(levels) == 2 and all(_is_int(v) for v in levels)
                     and 1 <= levels[0] <= levels[1] <= wt.last_level)
        if not levels_ok:
            errors.append(f"{tag}: levels {levels!r} no es [primero, ultimo] dentro de 1..{wt.last_level}")
        elif _is_int(n) and levels != [10 * n + 1, 10 * n + 10]:
            errors.append(f"{tag}: levels {levels} y la cadencia da [{10 * n + 1}, {10 * n + 10}]")

        world = s.get("world")
        world_ok = _is_int(world) and 1 <= world <= wt.world_count
        if not world_ok:
            errors.append(f"{tag}: world {world!r} fuera de 1..{wt.world_count}")
        elif levels_ok:
            w0, w1 = wt.world_of(levels[0]), wt.world_of(levels[1])
            if w0 != world or w1 != world:
                errors.append(f"{tag}: world {world} y WorldTable da {w0} (nivel {levels[0]}) y {w1} (nivel {levels[1]})")
            else:
                is_first = levels[0] == wt.first_level(world)
                is_last = levels[1] == wt.last_level_of(world)
                kind = s.get("kind")
                if kind == "gate" and not is_first:
                    errors.append(f"{tag}: gate fuera del primer nivel del mundo {world} ({wt.first_level(world)})")
                if is_first and kind != "gate":
                    errors.append(f"{tag}: la primera parada del mundo {world} tiene que ser gate y es {kind!r}")
                if kind == "close" and (is_first or not is_last):
                    errors.append(f"{tag}: close fuera del ultimo tramo del mundo {world} (nivel {wt.last_level_of(world)})")
                if is_last and not is_first and kind != "close":
                    errors.append(f"{tag}: la ultima parada del mundo {world} tiene que ser close y es {kind!r}")
        if s.get("kind") not in KIND.values():
            errors.append(f"{tag}: kind {s.get('kind')!r} no es gate, stop ni close")

        if not (isinstance(sid, str) and RE_ID.fullmatch(sid)):
            errors.append(f"{tag}: id {sid!r} no es [a-z0-9_]+")
        elif sid in ids:
            errors.append(f"{tag}: id repetido (ya es la parada n={ids[sid]})")
        else:
            ids[sid] = n

        country = s.get("country")
        if not (isinstance(country, str) and RE_ISO2.fullmatch(country)):
            errors.append(f"{tag}: country {country!r} no es ISO2")
        country_name = s.get("country_name")
        if country_name is not None and country_name != f"map.stop.{sid}.country":
            errors.append(f"{tag}: country_name {country_name!r} y la convencion es map.stop.{sid}.country")

        km_from_sea = s.get("km_from_sea")
        if not (km_from_sea is None or (_is_int(km_from_sea) and km_from_sea >= 0)):
            errors.append(f"{tag}: km_from_sea {km_from_sea!r} no es un entero >= 0 ni null")
        km_home = s.get("km_home")
        if not (_is_int(km_home) and km_home >= 0):
            errors.append(f"{tag}: km_home {km_home!r} no es un entero >= 0")

        if s.get("sample") not in SAMPLES:
            errors.append(f"{tag}: sample {s.get('sample')!r} desconocido")

        expected_flag = country if (i == 0 or country != prev_country) else None
        if s.get("flag_raise") != expected_flag:
            errors.append(f"{tag}: flag_raise {s.get('flag_raise')!r} y se esperaba {expected_flag!r} "
                          "(se iza al empezar y al cambiar de pais)")
        prev_country = country

        arrival = s.get("arrival_fact")
        if n == 0:
            if arrival is not None:
                errors.append(f"{tag}: la parada 0 no tiene llegada (arrival_fact tiene que ser null)")
        elif not (isinstance(arrival, str) and RE_FACT_KEY.fullmatch(arrival)):
            errors.append(f"{tag}: arrival_fact {arrival!r} no es una clave de contenido map.* (glosario §10)")

        facts = s.get("facts")
        if not isinstance(facts, list):
            errors.append(f"{tag}: facts no es una lista")
        else:
            for f in facts:
                if not (isinstance(f, str) and RE_FACT_KEY.fullmatch(f)):
                    errors.append(f"{tag}: facts lleva {f!r}, que no es una clave de contenido map.*")
            if len(set(map(str, facts))) != len(facts):
                errors.append(f"{tag}: facts con claves repetidas")

        question = s.get("world_question")
        if question is not None:
            m = RE_QUESTION.fullmatch(question) if isinstance(question, str) else None
            if not m or int(m.group(1)) != world:
                errors.append(f"{tag}: world_question {question!r} y se esperaba map.world.w{world:02d}.question"
                              if world_ok else f"{tag}: world_question {question!r} invalida")
            elif world in questions:
                errors.append(f"{tag}: segunda pregunta del mundo {world} (la primera, en n={questions[world]})")
            elif _is_int(n):
                questions[world] = n
        answers_flag = s.get("answers_world_question")
        if not isinstance(answers_flag, bool):
            errors.append(f"{tag}: answers_world_question no es booleano")
        elif answers_flag and world_ok and _is_int(n):
            answers.setdefault(world, []).append(n)

        bg = s.get("bg")
        if not isinstance(bg, dict) or tuple(bg.keys()) != BG_FIELDS:
            errors.append(f"{tag}: bg tiene que ser {{kit, hero, code, lut}}")
        else:
            if world_ok and bg["kit"] != f"bg_kit_w{world:02d}":
                errors.append(f"{tag}: bg.kit {bg['kit']!r} y el mundo {world} pide bg_kit_w{world:02d}")
            hero, code, lut = bg["hero"], bg["code"], bg["lut"]
            if (hero is None) == (code is None):
                errors.append(f"{tag}: bg lleva hero o code, uno y solo uno")
            if hero is not None and not (isinstance(hero, str) and RE_HERO.fullmatch(hero)):
                errors.append(f"{tag}: bg.hero {hero!r} no es bg_hero_<sitio>")
            if code is not None and not (isinstance(code, str) and RE_CODE.fullmatch(code)):
                errors.append(f"{tag}: bg.code {code!r} no es codigo:<pieza> ni kit:<pieza>")
            if lut is not None and lut not in LUTS:
                errors.append(f"{tag}: bg.lut {lut!r} no es dusk, dawn ni null")

    # El documento termina en el ultimo nivel de un mundo (1-200: el 200 del mundo 5; vale igual para 1-700). Con n
    # contiguos y levels = [10n+1, 10n+10], asi cada mundo presente esta completo: su gate y, si tiene mas de un
    # tramo, su close.
    last = stops[-1]
    end, world = last.get("levels"), last.get("world")
    end = end[1] if isinstance(end, list) and len(end) == 2 else None
    if (_is_int(end) and 1 <= end <= wt.last_level and _is_int(world) and 1 <= world <= wt.world_count
            and wt.world_of(end) == world and end != wt.last_level_of(world)):
        errors.append(f"parada n={last.get('n')!r} ({last.get('id')!r}): el documento termina en el nivel {end} y el "
                      f"mundo {world} acaba en el {wt.last_level_of(world)}; las paradas tienen que llegar al final "
                      "de un mundo (faltan las ultimas del mundo, con su close)")

    for world, qn in sorted(questions.items()):
        got = answers.get(world, [])
        if len(got) != 1:
            errors.append(f"pregunta del mundo {world} (n={qn}): la responden {len(got)} paradas y tiene que ser 1")
        elif got[0] <= qn:
            errors.append(f"pregunta del mundo {world} (n={qn}): se responde en n={got[0]}, antes de plantearse")
    for world in sorted(set(answers) - set(questions), key=str):
        errors.append(f"mundo {world}: answers_world_question sin pregunta del mundo")

    for path, text in iter_strings(doc):
        if has_belen(text):
            errors.append(f"{path}: {text!r} cae en el veto de Belen (D-013, punto 11; 07 §8.1)")
    return errors


def build(source: dict, overrides: dict, wt: WorldTable) -> tuple[dict | None, list[str]]:
    doc, errors = convert(source, overrides)
    if doc is not None:
        errors += validate(doc, wt)
    return doc, errors


# --------------------------------------------------------------------------------------------- salida

def render(doc: dict) -> str:
    """JSON estable: raiz con 2 espacios y una parada por linea (UTF-8 sin BOM y LF al escribir)."""
    def dumps(value) -> str:
        return json.dumps(value, ensure_ascii=False, separators=(", ", ": "))

    lines = [
        "{",
        f'  "format": {dumps(doc["format"])},',
        f'  "source": {dumps(doc["source"])},',
        '  "stops": [',
        ",\n".join("    " + dumps(stop) for stop in doc["stops"]),
        "  ]",
        "}",
    ]
    return "\n".join(lines) + "\n"


def _rel(path: Path) -> str:
    try:
        return Path(path).resolve().relative_to(ROOT).as_posix()
    except ValueError:
        return str(path)


def compare(text: str, out: Path) -> list[str]:
    """Desfase: lo commiteado tiene que ser byte a byte lo que se regenera en memoria."""
    try:
        actual = Path(out).read_bytes()
    except FileNotFoundError:
        return [f"falta {_rel(out)}: generalo con py -3.14 tools/river-map/stops.py"]
    expected = text.encode("utf-8")
    if actual == expected:
        return []
    got_lines = actual.decode("utf-8", "replace").split("\n")
    want_lines = text.split("\n")
    line = next((i + 1 for i, (a, b) in enumerate(zip(got_lines, want_lines)) if a != b),
                min(len(got_lines), len(want_lines)) + 1)
    return [f"desfase: {_rel(out)} no coincide con lo que genera la fuente (primera diferencia en la linea {line}); "
            "regeneralo con py -3.14 tools/river-map/stops.py"]


def emit(message: str) -> None:
    print(ascii_text(message))


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="Genera o comprueba river_stops_v1.json (af-river-stops/1).")
    ap.add_argument("--check", action="store_true",
                    help="valida y compara con el archivo commiteado; codigo 1 si algo falla")
    ap.add_argument("--source", type=Path, default=SOURCE_PATH, help="rio-amazonas.json (por defecto, el de docs/research)")
    ap.add_argument("--overrides", type=Path, default=OVERRIDES_PATH, help="decisiones [P] por parada")
    ap.add_argument("--worldtable", type=Path, default=WORLDTABLE_PATH, help="WorldTable.cs del cliente")
    ap.add_argument("--out", type=Path, default=STOPS_PATH, help="river_stops_v1.json que se escribe o se compara")
    args = ap.parse_args(argv)

    try:
        source = load_json(args.source)
        overrides = load_json(args.overrides)
        wt = WorldTable.from_cs(args.worldtable)
    except SourceError as e:
        emit(f"FAIL  {e}")
        return 1

    doc, errors = build(source, overrides, wt)
    if errors:
        for err in errors:
            emit(f"FAIL  {err}")
        if args.check:
            emit("      (no se compara con lo commiteado hasta corregir lo anterior)")
        emit(f"stops.py: errores: {len(errors)}")
        return 1

    text = render(doc)
    count = len(doc["stops"])
    last = doc["stops"][-1]["levels"][1]
    if args.check:
        errors = compare(text, args.out)
        for err in errors:
            emit(f"FAIL  {err}")
        if errors:
            return 1
        emit(f"OK  {count} paradas (n 0-{count - 1}, niveles 1-{last}) validas y {_rel(args.out)} al dia")
        return 0

    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_bytes(text.encode("utf-8"))
    emit(f"OK  escrito {_rel(out)}: {count} paradas (n 0-{count - 1}, niveles 1-{last})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
