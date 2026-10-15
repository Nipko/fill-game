#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""stops.py - Mapa del rio (D-013): genera las paradas af-river-stops/1 desde la investigacion.

Lee docs/research/mapa-del-rio/rio-amazonas.json (sin modificarlo) y tools/river-map/overrides.json, y escribe
Assets/_Project/Data/Map/river_stops_v1.json: una entrada por parada (k = n, niveles 10n+1 ... 10n+10) con los campos
de 08 §4.5 (contrato 30) y los extras [P] de P-M1 (a). Solo claves de texto, nunca textos: los textos van en el bloque
de filas map.* de Assets/_Project/Data/Loc/facts.csv (contrato 32; 08 §4.7), que solo escribe este script (M1.1t). Solo
biblioteca estandar de Python 3.14 (08 §4.5).

Uso (desde la raiz del repositorio):
    py -3.14 tools/river-map/stops.py                  # escribe river_stops_v1.json
    py -3.14 tools/river-map/stops.py --write-texts    # regenera solo el bloque map.* de facts.csv
    py -3.14 tools/river-map/stops.py --check          # valida y compara las dos salidas; codigo 1 si algo falla

Opciones para pruebas y fixtures: --source, --overrides, --worldtable, --out y --facts cambian las rutas de entrada y
salida. Determinista: misma entrada, mismos bytes (UTF-8 sin BOM, LF, orden de claves fijo y filas en orden ordinal).
Mensajes solo en ASCII.

Textos (M1.1t; bloque map.* de facts.csv, cabecera de L2b: key, es-419, en, pt-BR, context, max_chars, status, status_en,
status_pt-BR, notes y args):
- nombres por convencion: map.stop.<id>.name y map.stop.<id>.river (nombre y rio, o los de overrides.json),
  map.country.<iso2>.name (COUNTRY_NAMES, comprobado contra rotulos_de_pais) y, en n = 13, country_name («Colombia y
  Peru», desde pais CO/PE); 32 caracteres [P];
- datos de llegada (arrival_fact, con su texto, etiqueta y fuentes con URL): 90 caracteres; preguntas del mundo: 50;
- en y pt-BR vacios; status pendiente_asesor; notes con [V] o [E] (o [P] en lo que decide el diseno); args con las cifras
  que el texto escribe como {n} (Q8-A): la fuente las da en dato_del_nivel.args;
- --check falla si el bloque no esta al dia (desfase), si una clave citada no tiene fila con texto, si un texto pasa de su
  limite, lleva una cifra con separador o decimales o cae en el veto de Belen, o si un dato [V] no tiene fuente [V].

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
import csv
import json
import re
import sys
import unicodedata
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from riverdata import (  # noqa: E402  (la ruta de la herramienta va antes)
    COUNTRY_NAMES,
    FACTS_PATH,
    FACTS_REL,
    OVERRIDES_PATH,
    ROOT,
    SOURCE_PATH,
    SOURCE_REL,
    STOPS_PATH,
    WORLDTABLE_PATH,
    SourceError,
    WorldTable,
    ascii_text,
    format_csv_row,
    has_belen,
    iter_strings,
    load_json,
    parse_csv_record,
    split_csv_records,
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
OVERRIDE_FIELDS = frozenset({"country", "country_name", "name", "river", "why"})

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
            errors.append(f"overrides.json: {sid!r} lleva campos desconocidos {unknown} (admitidos: country, country_name, name, river y why)")
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
    """Reglas de 08 §4.5 y del plan del Mapa sobre el documento generado: aqui se comprueban todas (stops.py --check).

    RiverStopCatalog (AF.Levels, M1.2) repite solo el formato, los n contiguos, levels, WorldTable, gate y close (contrato
    30) y la parada final de cada mundo, y ArtSlots rechaza «belen»; los textos de las claves los comprueba build_texts.
    """
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


# --------------------------------------------------------------------------------------------- textos (M1.1t)
#
# Bloque de filas map.* de Assets/_Project/Data/Loc/facts.csv (contrato 32; 08 §4.7; pregunta 49 (a): contenido con
# puntos y un estado por idioma). Solo lo escribe stops.py --write-texts; el resto del archivo (la cabecera de L2b y las
# filas de fauna, de fruta...) no se toca, ni un byte. Las claves con el mismo prefijo «map.» quedan contiguas en orden
# ordinal (sorted() de Python = StringComparer.Ordinal de C#; regla de G1).

TEXT_PREFIX = "map."
FACT_MAX_CHARS = 90        # 08 §4.5: el dato mide <= 90 caracteres en ES
QUESTION_MAX_CHARS = 50    # 08 §4.5: la pregunta, <= 50
NAME_MAX_CHARS = 32        # [P] nombres de parada, rio y pais
MAX_ARGS = 3               # LocArgs.MaxCount (AF.Localization)
TEXT_STATUS = "pendiente_asesor"   # lo cultural nace pendiente del asesor (07 §8.2)
EVIDENCE = ("V", "E")
REQUIRED_COLUMNS = ("key", "es-419", "context", "max_chars", "status", "notes", "args")
GENERATED = "Fila generada por tools/river-map/stops.py --write-texts (M1.1t): no se edita a mano."
NO_CAPTURE = "Captura: ninguna todavía (la placa de llegada y el Mapa aún no existen en el cliente)."

RE_MARKER = re.compile(r"\{([a-z][a-z0-9_]*)\}")
# La misma expresion que LocTablesTests.NumberWithSeparator: cifra con separador de miles o decimales («1.480», «5,2»,
# «2 000» con espacio normal, duro o fino, «1'000»).
RE_NUMBER_SEP = re.compile("[0-9][.,\u00A0\u202F\u2009'][0-9]|(?<![0-9])[0-9]{1,3}( [0-9]{3})+(?![0-9])")
# Cifra (o marcador {n}) + espacio normal + unidad: el espacio pasa a U+00A0 (08 §4.7: «U+00A0 entre cifra y unidad»).
RE_UNIT_SPACE = re.compile(r"(?<=[0-9}]) (?=(?:km²|km|m²|m|cm|mm|ha|kg|°C|%)(?![^\W_]))")
RE_NUMBER_TOKEN = re.compile("[0-9]+(?:[.,\u00A0\u202F\u2009' ][0-9]+)+")
RE_ARG_NAME = re.compile(r"[a-z][a-z0-9_]*")
RE_ARG_VALUE = re.compile(r"-?[0-9]+(?:\.[0-9]+)?")


def clean_text(text: str) -> str:
    """Texto de la fuente para la tabla: NFC y U+00A0 entre la cifra (o {n}) y su unidad («6 km» -> «6\\u00a0km»)."""
    return RE_UNIT_SPACE.sub("\u00A0", unicodedata.normalize("NFC", text))


def _shown_number(value: str) -> str:
    """Cifra como la escribe Loc.Number con es-CO (la region por defecto [P]): «1480» -> «1.480», «3.25» -> «3,25»."""
    sign = "-" if value.startswith("-") else ""
    whole, _, decimals = value.lstrip("-").partition(".")
    groups = []
    while len(whole) > 3:
        groups.insert(0, whole[-3:])
        whole = whole[:-3]
    groups.insert(0, whole)
    return sign + ".".join(groups) + ("," + decimals if decimals else "")


def visible_length(text: str, args: dict) -> int:
    """Lo que ocupa en pantalla: cada {marcador} con su cifra ya formateada, y nunca menos que el texto con los marcadores
    escritos (LocTablesTests cuenta {n} como 3)."""
    shown = RE_MARKER.sub(lambda m: _shown_number(args[m.group(1)]) if m.group(1) in args else m.group(0), text)
    return max(len(shown), len(text))


def _parse_args(raw, key: str) -> tuple[dict, list[str]]:
    """args opcional de un dato de la fuente: {nombre: entero o "decimal con punto"} -> {nombre: texto de la cifra}."""
    if raw is None:
        return {}, []
    if not isinstance(raw, dict):
        return {}, [f"{key}: args tiene que ser un objeto {{nombre: cifra}}"]
    out: dict = {}
    errors: list[str] = []
    for name, value in raw.items():
        if not (isinstance(name, str) and RE_ARG_NAME.fullmatch(name)) or name == "sep":
            errors.append(f"{key}: args lleva el nombre {name!r}, que no vale como marcador")
        elif _is_int(value):
            out[name] = str(value)
        elif isinstance(value, str) and RE_ARG_VALUE.fullmatch(value):
            out[name] = value
        else:
            errors.append(f"{key}: el valor de {name!r} ({value!r}) no es un entero ni un decimal con punto entre comillas")
    if len(out) > MAX_ARGS:
        errors.append(f"{key}: {len(out)} argumentos y la tabla admite {MAX_ARGS} (LocArgs)")
    return out, errors


def format_args(args: dict) -> str:
    """Columna args [P] de facts.csv: «nombre=valor» separados por «;», en orden ordinal (LocArgs.TryParse)."""
    return ";".join(f"{name}={args[name]}" for name in sorted(args))


def check_text(key: str, text, max_chars: int, args: dict) -> list[str]:
    """Reglas de un texto del bloque: no vacio, marcadores = args, sin cifras con separador, longitud y veto de Belen."""
    if not isinstance(text, str) or not text.strip():
        return [f"{key}: clave sin texto es-419 en la fuente"]
    errors: list[str] = []
    if text != text.strip() or "\n" in text or "\r" in text:
        errors.append(f"{key}: el texto lleva espacios al principio o al final, o un salto de linea")
    markers = sorted(set(RE_MARKER.findall(text)))
    rest = RE_MARKER.sub("", text)
    if "{" in rest or "}" in rest:
        errors.append(f"{key}: llaves sueltas en {text!r} (solo marcadores {{nombre}})")
    if markers != sorted(args):
        errors.append(f"{key}: los marcadores {markers} y args {sorted(args)} no coinciden")
    m = RE_NUMBER_SEP.search(text)
    if m:
        # La cifra entera para el mensaje («1.480», no solo «1.4»).
        whole = next((t.group(0) for t in RE_NUMBER_TOKEN.finditer(text) if t.start() <= m.start() < t.end()), m.group(0))
        errors.append(f"{key}: la cifra {whole!r} lleva separador de miles o decimales; va con {{n}} y su valor en "
                      "args (D-018, Q8-A)")
    length = visible_length(text, args)
    if length > max_chars:
        errors.append(f"{key}: {length} caracteres y el maximo es {max_chars} ({text!r})")
    if has_belen(text):
        errors.append(f"{key}: {text!r} cae en el veto de Belen (D-013, punto 11; 07 §8.1)")
    return errors


def cited_keys(doc: dict) -> list[str]:
    """Claves de texto que cita river_stops_v1.json, explicitas (arrival_fact, facts, world_question y country_name) y
    por convencion (map.stop.<id>.name, map.stop.<id>.river y map.country.<iso2>.name), en orden ordinal."""
    keys: set = set()
    for s in doc["stops"]:
        keys.add(f"map.stop.{s['id']}.name")
        keys.add(f"map.stop.{s['id']}.river")
        keys.add(f"map.country.{s['country'].lower()}.name")
        for k in (s["arrival_fact"], s["world_question"], s["country_name"], *s["facts"]):
            if k is not None:
                keys.add(k)
    return sorted(keys)


def _sources_text(dato: dict, fuentes: dict, key: str) -> tuple[str, list[str]]:
    """Fuentes de un dato con su etiqueta y su URL: cada dato [V] o [E] lleva a una fuente con URL, y uno [V], a una [V]."""
    errors: list[str] = []
    etiqueta = dato.get("etiqueta")
    if etiqueta not in EVIDENCE:
        errors.append(f"{key}: etiqueta {etiqueta!r}; un dato del Mapa es [V] o [E] (ninguno [NV])")
    raw = dato.get("fuente")
    ids = [p.strip() for p in raw.split(",") if p.strip()] if isinstance(raw, str) else []
    if not ids:
        return "", errors + [f"{key}: dato sin fuente"]
    parts = []
    tags = []
    for sid in ids:
        f = fuentes.get(sid)
        if f is None:
            errors.append(f"{key}: la fuente {sid!r} no esta en rio-amazonas.json, fuentes")
            continue
        url = f.get("url")
        if not (isinstance(url, str) and url.startswith(("https://", "http://"))):
            errors.append(f"{key}: la fuente {sid!r} no tiene URL")
        tags.append(f.get("etiqueta"))
        parts.append(f"{sid} [{f.get('etiqueta')}]: {f.get('nombre')}, {url}")
    if etiqueta == "V" and len(tags) == len(ids) and "V" not in tags:
        errors.append(f"{key}: dato [V] sin ninguna fuente [V] ({', '.join(ids)})")
    label = "Fuente" if len(parts) == 1 else "Fuentes"
    return f"{label} " + "; ".join(parts) + ".", errors


def build_texts(doc: dict, source: dict, overrides: dict) -> tuple[dict, list[str]]:
    """Filas del bloque map.* de facts.csv para un documento ya valido: {clave: {columna: valor}}."""
    errors: list[str] = []
    records = {r.get("k"): r for r in source.get("paradas_1_200", []) if isinstance(r, dict)}
    fuentes = {f.get("id"): f for f in source.get("fuentes", []) if isinstance(f, dict)}
    rotulos = [r.get("nombre") for r in source.get("rotulos_de_pais", []) if isinstance(r, dict)]
    over = overrides.get("stops") if isinstance(overrides.get("stops"), dict) else {}
    stops = doc["stops"]
    rows: dict = {}

    def add(key: str, text, max_chars: int, context: str, notes: str, args: dict | None = None) -> None:
        args = args or {}
        if isinstance(text, str):
            text = clean_text(text)
        errors.extend(check_text(key, text, max_chars, args))
        row = {"key": key, "es-419": text if isinstance(text, str) else "", "en": "", "pt-BR": "", "context": context,
               "max_chars": str(max_chars), "status": TEXT_STATUS, "status_en": "", "status_pt-BR": "",
               "notes": f"{notes} Pendiente del asesor cultural (07 §8.2). {GENERATED}", "args": format_args(args)}
        for field in ("context", "notes"):
            if has_belen(row[field]):
                errors.append(f"{key}: la columna {field} cae en el veto de Belen (D-013, punto 11)")
        if key in rows and rows[key] != row:
            errors.append(f"{key}: dos filas distintas con la misma clave")
        rows[key] = row

    def where(s: dict) -> str:
        a, b = s["levels"]
        return f"parada {s['n']} del Mapa del río (niveles {a}-{b}, mundo {s['world']})"

    # Nombres de parada y rio (map.stop.<id>.name y .river; 32 caracteres [P]).
    names: dict = {}
    for s in stops:
        n, sid = s["n"], s["id"]
        r = records.get(n, {})
        ov = over.get(sid) if isinstance(over.get(sid), dict) else {}
        conf = r.get("conf")
        if conf not in EVIDENCE:
            errors.append(f"parada n={n} ({sid!r}): conf {conf!r} del lugar no es V ni E")
        for field, src_field in (("name", "nombre"), ("river", "rio")):
            value = ov.get(field, r.get(src_field))
            if field in ov:
                origin = (f"tools/river-map/overrides.json ({sid}: {field}), en lugar de «{r.get(src_field)}» de "
                          f"{SOURCE_REL}, paradas_1_200 k={n}")
                notes = "[P] Decisión de overrides.json (P-M1 a)."
            else:
                origin = f"{SOURCE_REL}, paradas_1_200 k={n} ({src_field})"
                notes = f"[{conf}] Lugar {'verificado' if conf == 'V' else 'estimado'} en rio-amazonas.json (conf)."
            if field == "name":
                names[n] = clean_text(value) if isinstance(value, str) else sid
                context = (f"Nombre de la {where(s)}: {{lugar}} de map_arrived («{{lugar}} · {{país}} · río {{río}}», "
                           f"placa de llegada y tarjeta de la parada), de stop_label (rótulo del lugar) y de "
                           f"map_stop_remaining (D-013; 07 §2.1; 08 §4.5). Fuente: {origin}. {NO_CAPTURE}")
            else:
                context = (f"Río de la {where(s)}, {names[n]}: {{río}} de map_arrived («{{lugar}} · {{país}} · río "
                           f"{{río}}»), así que va sin la palabra «río» (glosario §10; 08 §4.5). Fuente: {origin}. "
                           f"{NO_CAPTURE}")
            add(f"map.stop.{sid}.{field}", value, NAME_MAX_CHARS, context, notes)

    # Paises (map.country.<iso2>.name) y la placa de dos paises (country_name, n = 13).
    by_country: dict = {}
    for s in stops:
        by_country.setdefault(s["country"], []).append(s["n"])
    for iso in sorted(by_country):
        name = COUNTRY_NAMES.get(iso)
        key = f"map.country.{iso.lower()}.name"
        if name is None:
            errors.append(f"{key}: el pais {iso} no esta en COUNTRY_NAMES (riverdata.py)")
            continue
        rotulo = next((t for t in rotulos if isinstance(t, str) and (t == name or t.startswith(name + " ("))), None)
        if rotulo is None:
            errors.append(f"{key}: {name!r} no es ningun rotulo de rotulos_de_pais de rio-amazonas.json")
        ns = by_country[iso]
        context = (f"Nombre del país {iso}: {{país}} de map_arrived y de stop_label en las paradas {ns[0]}-{ns[-1]} del "
                   f"Mapa del río, y rótulo del país en el Mapa (glosario §8; 08 §4.5). Fuente: {SOURCE_REL}, "
                   f"rotulos_de_pais («{rotulo}»). {NO_CAPTURE}")
        add(key, name, NAME_MAX_CHARS, context, "[V] Nombre del país en español (glosario §8).")
    for s in stops:
        if s["country_name"] is None:
            continue
        r = records.get(s["n"], {})
        pais = r.get("pais")
        isos = pais.split("/") if isinstance(pais, str) else []
        if len(isos) < 2 or any(i not in COUNTRY_NAMES for i in isos):
            errors.append(f"{s['country_name']}: la parada n={s['n']} no tiene dos paises conocidos en pais ({pais!r})")
            continue
        text = " y ".join(COUNTRY_NAMES[i] for i in isos)
        context = (f"Países de la {where(s)}, {names[s['n']]}: {{país}} de map_arrived y de stop_label, en lugar de "
                   f"map.country.{s['country'].lower()}.name, porque la parada tiene un país en cada orilla (08 §4.5). "
                   f"Fuente: {SOURCE_REL}, paradas_1_200 k={s['n']} (pais «{pais}»), y tools/river-map/overrides.json "
                   f"(country_name). {NO_CAPTURE}")
        add(s["country_name"], text, NAME_MAX_CHARS, context, "[P] Placa de dos países (P-M1 a).")

    # Datos de llegada (arrival_fact; 90 caracteres), con su fuente y su URL.
    for s in stops:
        key = s["arrival_fact"]
        if key is None:
            continue
        n = s["n"]
        holder = 0 if n == 1 else n  # el dato de la llegada del 10 vive en el registro k = 0 (Mapa del 10, 06 §2.3)
        dato = records.get(holder, {}).get("dato_del_nivel")
        if not isinstance(dato, dict):
            errors.append(f"{key}: clave sin texto es-419 en la fuente (sin dato_del_nivel en la parada k={holder})")
            continue
        args, errs = _parse_args(dato.get("args"), key)
        errors.extend(errs)
        sources, errs = _sources_text(dato, fuentes, key)
        errors.extend(errs)
        mapa10 = (f" Es el Dato del río 1 del Mapa del 10 (06 §2.3): habla del estuario, la parada 0 ({names.get(0)})."
                  if n == 1 else "")
        cifras = (" Marcadores: " + ", ".join(f"{{{a}}}" for a in sorted(args))
                  + ", la cifra de la columna args con el separador de la región (D-018, Q8-A)." if args else "")
        context = (f"Dato de llegada a la {where(s)}, {names[n]}, en la victoria del {10 * n}: placa del dato bajo "
                   f"map_arrived y tarjeta de la parada; es también el fact_id de af_river_fact_shown (02 §6; 08 §4.5 "
                   f"y §8.2).{mapa10}{cifras} Lugar del dato: {names.get(holder)}. {sources} {NO_CAPTURE}")
        add(key, dato.get("texto"), FACT_MAX_CHARS, context, f"[{dato.get('etiqueta')}] Dato del Mapa del río.", args)

    # Otros datos de la tarjeta (facts[]): llegan con M2.6; hasta entonces, una clave sin texto es un error.
    for s in stops:
        for key in s["facts"]:
            if key not in rows:
                errors.append(f"{key}: clave sin texto es-419 en la fuente (facts de la parada n={s['n']})")

    # Preguntas del mundo (50 caracteres).
    for s in stops:
        key = s["world_question"]
        if key is None:
            continue
        q = records.get(s["n"], {}).get("pregunta_del_mundo") or {}
        answer = next((a for a in stops if a["answers_world_question"] and a["world"] == s["world"]), None)
        answered = (f"la responde el dato de llegada a la parada {answer['n']}, {names[answer['n']]} "
                    f"({answer['arrival_fact']}), en la victoria del {10 * answer['n']}" if answer else "sin respuesta")
        context = (f"Pregunta del mundo {s['world']} en la cabecera del Mapa del río (map_world_question, con ✓ al "
                   f"responderse): se plantea al llegar a la {where(s)}, {names[s['n']]}, en la victoria del "
                   f"{10 * s['n']}, y {answered} (D-013; 08 §4.5). Fuente: {SOURCE_REL}, paradas_1_200 k={s['n']} "
                   f"(pregunta_del_mundo). {NO_CAPTURE}")
        add(key, q.get("texto"), QUESTION_MAX_CHARS, context, "[P] Pregunta del Mapa del río (D-013).")

    for key in cited_keys(doc):
        if key not in rows:
            errors.append(f"{key}: citada en river_stops_v1.json y sin texto en el bloque")
    return rows, errors


def build_all(source: dict, overrides: dict, wt: WorldTable) -> tuple[dict | None, dict, list[str]]:
    """Paradas y textos: los textos solo se generan sobre un documento de paradas valido."""
    doc, errors = build(source, overrides, wt)
    if errors or doc is None:
        return doc, {}, errors
    rows, errors = build_texts(doc, source, overrides)
    return doc, rows, errors


def _record_key(record: str) -> str:
    fields = parse_csv_record(record)
    return fields[0] if fields else ""


def render_facts(current: str, rows: dict) -> tuple[str | None, list[str]]:
    """facts.csv con el bloque map.* regenerado: las demas filas y la cabecera quedan byte a byte como estaban."""
    records = split_csv_records(current)
    if not records:
        return None, [f"{FACTS_REL}: vacio, sin la cabecera de L2b"]
    try:
        header = parse_csv_record(records[0])
        keys = [_record_key(r) for r in records[1:]]
    except (ValueError, csv.Error) as e:
        return None, [f"{FACTS_REL}: no se puede leer como CSV RFC 4180 ({e})"]
    missing = [c for c in REQUIRED_COLUMNS if c not in header]
    if missing:
        return None, [f"{FACTS_REL}: a la cabecera le faltan las columnas {missing} (08 §4.7 y pregunta 49 a)"]
    block = [i for i, k in enumerate(keys) if k.startswith(TEXT_PREFIX)]
    if block and block != list(range(block[0], block[-1] + 1)):
        return None, [f"{FACTS_REL}: el bloque {TEXT_PREFIX}* no es contiguo (hay otras filas entre las suyas)"]
    others = [r for r, k in zip(records[1:], keys) if not k.startswith(TEXT_PREFIX)]
    other_keys = [k for k in keys if not k.startswith(TEXT_PREFIX)]
    generated = [format_csv_row([rows[k].get(col, "") for col in header]) for k in sorted(rows)]
    if not generated:
        return None, ["sin filas del Mapa que escribir"]
    first = sorted(rows)[0]
    at = next((i for i, k in enumerate(other_keys) if k > first), len(others))
    body = list(others)
    if at == len(body) and body and not body[-1].endswith("\n"):
        body[-1] += "\n"
    head = records[0] if records[0].endswith("\n") else records[0] + "\n"
    return "".join([head] + body[:at] + generated + body[at:]), []


def check_facts(path: Path, rows: dict, doc: dict) -> list[str]:
    """--check del bloque: las claves que cita river_stops_v1.json tienen fila con texto, y el bloque esta al dia."""
    try:
        current = Path(path).read_bytes().decode("utf-8")
    except FileNotFoundError:
        return [f"falta {_rel(path)} (la tabla de textos largos de L2b)"]
    except UnicodeDecodeError:
        return [f"{_rel(path)} no es UTF-8"]
    errors: list[str] = []
    try:
        records = split_csv_records(current)
        header = parse_csv_record(records[0]) if records else []
        table = {}
        for r in records[1:]:
            fields = parse_csv_record(r)
            if fields:
                table[fields[0]] = dict(zip(header, fields))
    except (ValueError, csv.Error) as e:
        return [f"{_rel(path)}: no se puede leer como CSV RFC 4180 ({e})"]
    missing = [k for k in cited_keys(doc) if k not in table]
    if len(missing) > 3:
        errors.append(f"{len(missing)} claves citadas en river_stops_v1.json sin fila en {_rel(path)} (la primera, "
                      f"{missing[0]}): falta el bloque {TEXT_PREFIX}*")
    else:
        errors.extend(f"{k}: citada en river_stops_v1.json y sin fila en {_rel(path)}" for k in missing)
    for key in cited_keys(doc):
        if key in table and not table[key].get("es-419", "").strip():
            errors.append(f"{key}: citada en river_stops_v1.json y sin texto es-419 en {_rel(path)}")
    expected, errs = render_facts(current, rows)
    errors.extend(errs)
    if expected is not None and expected != current:
        want = {_record_key(r): r for r in split_csv_records(expected)[1:]}
        have = {_record_key(r): r for r in records[1:]}
        diff = sorted(k for k in set(want) | set(have) if want.get(k) != have.get(k))
        where = f"primera fila distinta: {diff[0]}" if diff else "orden o fin de linea"
        errors.append(f"desfase: el bloque {TEXT_PREFIX}* de {_rel(path)} no coincide con lo que genera la fuente "
                      f"({where}); regeneralo con py -3.14 tools/river-map/stops.py --write-texts")
    return errors


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
    ap = argparse.ArgumentParser(description="Genera o comprueba river_stops_v1.json (af-river-stops/1) y el bloque map.* de facts.csv.")
    mode = ap.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true",
                      help="valida y compara con river_stops_v1.json y con el bloque map.* de facts.csv; codigo 1 si algo falla")
    mode.add_argument("--write-texts", action="store_true",
                      help="regenera solo el bloque map.* de facts.csv (el resto del archivo no cambia)")
    ap.add_argument("--source", type=Path, default=SOURCE_PATH, help="rio-amazonas.json (por defecto, el de docs/research)")
    ap.add_argument("--overrides", type=Path, default=OVERRIDES_PATH, help="decisiones [P] por parada")
    ap.add_argument("--worldtable", type=Path, default=WORLDTABLE_PATH, help="WorldTable.cs del cliente")
    ap.add_argument("--out", type=Path, default=STOPS_PATH, help="river_stops_v1.json que se escribe o se compara")
    ap.add_argument("--facts", type=Path, default=FACTS_PATH, help="facts.csv cuyo bloque map.* se escribe o se compara")
    args = ap.parse_args(argv)

    try:
        source = load_json(args.source)
        overrides = load_json(args.overrides)
        wt = WorldTable.from_cs(args.worldtable)
    except SourceError as e:
        emit(f"FAIL  {e}")
        return 1

    doc, rows, errors = build_all(source, overrides, wt)
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
        errors = compare(text, args.out) + check_facts(args.facts, rows, doc)
        for err in errors:
            emit(f"FAIL  {err}")
        if errors:
            return 1
        emit(f"OK  {count} paradas (n 0-{count - 1}, niveles 1-{last}) validas y {_rel(args.out)} al dia")
        emit(f"OK  {len(rows)} textos {TEXT_PREFIX}* validos y su bloque de {_rel(args.facts)} al dia")
        return 0

    if args.write_texts:
        try:
            current = Path(args.facts).read_bytes().decode("utf-8")
        except FileNotFoundError:
            emit(f"FAIL  falta {_rel(args.facts)} (la tabla de textos largos de L2b)")
            return 1
        except UnicodeDecodeError:
            emit(f"FAIL  {_rel(args.facts)} no es UTF-8")
            return 1
        new_text, errors = render_facts(current, rows)
        for err in errors:
            emit(f"FAIL  {err}")
        if errors or new_text is None:
            return 1
        Path(args.facts).write_bytes(new_text.encode("utf-8"))
        emit(f"OK  escrito el bloque {TEXT_PREFIX}* de {_rel(args.facts)}: {len(rows)} filas")
        return 0

    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_bytes(text.encode("utf-8"))
    emit(f"OK  escrito {_rel(out)}: {count} paradas (n 0-{count - 1}, niveles 1-{last})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
