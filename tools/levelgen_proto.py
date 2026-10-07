#!/usr/bin/env python3
"""
levelgen_proto.py - Amazon Fill (Tintalu): prototipo del pipeline offline de niveles.

Implementa, en un solo archivo y sin dependencias externas:
  1. Representacion del estado (tuplas) + canonicalizacion (cuias ordenadas por (capacidad, contenido)).
  2. Generador por barajado inverso con los filtros anti-trivialidad de laicwben2 (research/02 s.3).
  3. Solver A* con heuristica admisible max(cortes_de_color, sum_c(cuias_con_c - 1)) y poda de futiles.
  4. Bot casual (greedy sin anticipacion, 85/15, tope 400 pasos) jugado x40 -> tasa de fallo.
  5. Metricas: par, ramificacion media, trampas (fraccion de vertidos legales que llevan a estado sin salida),
     complejidad laicwben2, indice D (pesos de research/02 s.7.1, [E]) y banda por cuantil.
  6. Variante aliviada (+1 vacio / -1 color) para la regla "El rio bajo" (direccion 7).
  7. Comprobacion de "Aguas turbias": enumeracion de realidades y busqueda de una ruta de vertidos seguros.
  8. Empaquetado JSON (estado explicito + semilla + metricas) y medicion de bytes por nivel y por pack.

Uso:
  python levelgen_proto.py                 # 20 niveles de 7-12 cuias, informe en stdout, JSON en tools/out/
  python levelgen_proto.py --levels 20 --budget 300000 --bot-runs 40
  python levelgen_proto.py --size-run 100  # ademas, 100 niveles rapidos (solo solver) para medir bytes/pack

Convenciones:
  - Colores = enteros 1..K (indice de motor; 0 reservado). K_motor <= 16 (4 bits por capa).
  - Una cuia es una tupla de abajo hacia arriba. Estado = tupla de cuias. caps = tupla de capacidades.
  - units[c] = unidades totales del color c (4 por defecto; 5-6 para "fruta rara" en Cuia alta).
"""
from __future__ import annotations

import argparse
import hashlib
import heapq
import itertools
import json
import os
import random
import statistics
import sys
import time
from collections import Counter

# ----------------------------------------------------------------------------
# 1. Estado, canonicalizacion y utilidades
# ----------------------------------------------------------------------------

def top_run(tube):
    """(color_superior, longitud_de_la_racha_superior); (0,0) si vacia."""
    if not tube:
        return (0, 0)
    c = tube[-1]
    n = 1
    for x in reversed(tube[:-1]):
        if x == c:
            n += 1
        else:
            break
    return (c, n)


def is_mono(tube):
    return len(set(tube)) <= 1


def is_complete(tube, units):
    """Cuia sellada: monocolor y contiene TODAS las unidades de su color."""
    return bool(tube) and is_mono(tube) and len(tube) == units[tube[0]]


def is_goal(state, units):
    for t in state:
        if t and not is_complete(t, units):
            return False
    return True


def canon(state, caps):
    """Clave canonica: cuias ordenadas por (capacidad, contenido). Elimina las T! permutaciones."""
    return tuple(sorted(zip(caps, state)))


def pack_state(state, caps):
    """Codificacion compacta: 4 bits por capa, cap bits por cuia, hex. Solo informativo (tamanio)."""
    out = []
    for t, cap in zip(state, caps):
        v = 0
        for k in range(cap):
            v = (v << 4) | (t[k] if k < len(t) else 0)
        out.append(f"{v:0{cap}x}")
    return ".".join(out)


# ----------------------------------------------------------------------------
# 2. Movimientos legales (podados) y aplicacion
# ----------------------------------------------------------------------------

def moves(state, caps, units):
    """Vertidos legales NO futiles.
    Poda: origen sellado; monocolor -> vacio; un solo destino vacio por origen; siempre la racha completa
    (min(racha, espacio))."""
    res = []
    for i, src in enumerate(state):
        if not src:
            continue
        c, run = top_run(src)
        src_mono = (run == len(src))
        if src_mono and len(src) == units[c]:
            continue  # sellada
        used_empty = False
        for j, dst in enumerate(state):
            if i == j:
                continue
            space = caps[j] - len(dst)
            if space <= 0:
                continue
            if not dst:
                if src_mono or used_empty:
                    continue
                used_empty = True
                res.append((i, j, min(run, space)))
            elif dst[-1] == c:
                res.append((i, j, min(run, space)))
    return res


def apply_move(state, m):
    i, j, n = m
    src, dst = state[i], state[j]
    new = list(state)
    new[i] = src[:-n]
    new[j] = dst + src[-n:]
    return tuple(new)


# ----------------------------------------------------------------------------
# 3. Heuristica y solver A*
# ----------------------------------------------------------------------------

def heuristic(state, units):
    """max(h1, h2): h1 = cortes de color (segmentos-1 por cuia); h2 = sum_c (cuias que contienen c - 1).
    Ambas admisibles: un vertido elimina a lo sumo un corte y saca un color de a lo sumo una cuia."""
    breaks = 0
    tubes_with = Counter()
    for t in state:
        if not t:
            continue
        prev = None
        seen = set()
        for x in t:
            if x != prev:
                if prev is not None:
                    breaks += 1
                prev = x
            seen.add(x)
        for c in seen:
            tubes_with[c] += 1
    h2 = sum(v - 1 for v in tubes_with.values())
    return breaks if breaks > h2 else h2


class SolveResult:
    __slots__ = ("status", "path", "expanded", "seconds")

    def __init__(self, status, path, expanded, seconds):
        self.status = status      # "solved" | "unsolvable" | "budget"
        self.path = path          # lista de movimientos (i, j, n) sobre el estado real
        self.expanded = expanded
        self.seconds = seconds

    @property
    def par(self):
        return len(self.path) if self.path is not None else None


def astar(start, caps, units, budget=300_000, weight=1.0):
    """A* (w=1 optimo) o A* ponderado (w>1, para pistas en cliente)."""
    t0 = time.perf_counter()
    if is_goal(start, units):
        return SolveResult("solved", [], 0, 0.0)
    k0 = canon(start, caps)
    g = {k0: 0}
    parent = {k0: None}            # k -> (k_padre, movimiento, estado_real_padre)
    actual = {k0: start}
    heap = [(weight * heuristic(start, units), 0, 0, k0)]
    counter = 1
    expanded = 0
    while heap:
        f, gs, _, key = heapq.heappop(heap)
        if gs > g[key]:
            continue  # entrada obsoleta
        s = actual[key]
        if is_goal(s, units):
            path = []
            k = key
            while parent[k] is not None:
                kp, m, _sp = parent[k]
                path.append(m)
                k = kp
            path.reverse()
            return SolveResult("solved", path, expanded, time.perf_counter() - t0)
        expanded += 1
        if expanded > budget:
            return SolveResult("budget", None, expanded, time.perf_counter() - t0)
        ng = gs + 1
        for m in moves(s, caps, units):
            t = apply_move(s, m)
            kt = canon(t, caps)
            if kt not in g or ng < g[kt]:
                g[kt] = ng
                parent[kt] = (key, m, s)
                actual[kt] = t
                heapq.heappush(heap, (ng + weight * heuristic(t, units), ng, counter, kt))
                counter += 1
    return SolveResult("unsolvable", None, expanded, time.perf_counter() - t0)


# ----------------------------------------------------------------------------
# 4. Bot casual (medidor de dificultad percibida)
# ----------------------------------------------------------------------------

def bot_score(s, m, caps, units):
    """Puntuacion "de aspecto" sin anticipacion: lo que haria un jugador casual."""
    i, j, n = m
    src, dst = s[i], s[j]
    c, run = top_run(src)
    score = float(n)                       # mover mas unidades parece mejor
    if dst:
        score += 4.0                       # juntar con el mismo color
        if is_mono(dst) and len(dst) + n == units[c]:
            score += 10.0                  # sella una cuia
    else:
        score -= 5.0                       # gastar un vacio parece caro
    if n < run:
        score -= 3.0                       # vertido parcial
    if n == run and len(src) > run:
        below = src[-run - 1]
        if any(k != i and (not s[k] or (s[k][-1] == below and len(s[k]) < caps[k])) for k in range(len(s))):
            score += 2.0                   # expone un color que tiene destino
    return score


def bot_play(start, caps, units, rng, max_steps=400, greedy_p=0.85):
    s = start
    for _ in range(max_steps):
        if is_goal(s, units):
            return True
        ms = moves(s, caps, units)
        if not ms:
            return False                   # Aguas estancadas
        if rng.random() < greedy_p:
            m = max(ms, key=lambda mm: bot_score(s, mm, caps, units) + rng.random() * 0.5)
        else:
            m = rng.choice(ms)
        s = apply_move(s, m)
    return False


def bot_fail_rate(start, caps, units, seed, runs=40):
    rng = random.Random(seed)
    fails = sum(0 if bot_play(start, caps, units, rng) else 1 for _ in range(runs))
    return fails / runs


# ----------------------------------------------------------------------------
# 5. Metricas estructurales
# ----------------------------------------------------------------------------

def complexity(state, caps, units):
    """laicwben2: transiciones*2 + cuiasMezcladas*2 + cuiasParciales."""
    transitions = mixed = partial = 0
    for t, cap in zip(state, caps):
        if not t:
            continue
        segs = 1 + sum(1 for a, b in zip(t, t[1:]) if a != b)
        transitions += segs - 1
        if segs > 1:
            mixed += 1
        if len(t) < cap:
            partial += 1
    return transitions * 2 + mixed * 2 + partial


def branching_along(start, path, caps, units):
    s = start
    counts = []
    for m in path:
        counts.append(len(moves(s, caps, units)))
        s = apply_move(s, m)
    return statistics.fmean(counts) if counts else 0.0


def trap_density(start, path, caps, units, memo, budget=3000, max_pairs=120):
    """Fraccion de vertidos legales (desde los estados de la ruta optima) que llevan a un estado sin salida.
    'confirmadas' = deadlock inmediato o A* agotado; 'desconocidas' = presupuesto agotado."""
    s = start
    pairs = []
    for m in path:
        for mm in moves(s, caps, units):
            pairs.append((s, mm))
        s = apply_move(s, m)
    if len(pairs) > max_pairs:
        step = len(pairs) / max_pairs
        pairs = [pairs[int(k * step)] for k in range(max_pairs)]
    traps = unknown = 0
    for s, mm in pairs:
        t = apply_move(s, mm)
        kt = canon(t, caps)
        if kt in memo:
            st = memo[kt]
        else:
            if is_goal(t, units):
                st = "solved"
            elif not moves(t, caps, units):
                st = "unsolvable"
            else:
                st = astar(t, caps, units, budget=budget).status
            memo[kt] = st
        if st == "unsolvable":
            traps += 1
        elif st == "budget":
            unknown += 1
    n = len(pairs) or 1
    return traps / n, unknown / n, len(pairs)


# ----------------------------------------------------------------------------
# 6. Generador por barajado inverso
# ----------------------------------------------------------------------------

def inverse_moves(state, caps, units):
    """Movimiento inverso legal (a -> b, m unidades) = inverso exacto de un vertido legal b -> a.
    Condiciones (laicwben2): b no llena; b vacia o con color distinto arriba (asi el vertido directo mueve
    exactamente m); si m == racha, la racha debe ser toda la cuia (si no, el vertido directo caeria sobre otro
    color); prohibido volcar una cuia monocolor completa en un vacio (vertido directo futil)."""
    res = []
    for a, src in enumerate(state):
        if not src:
            continue
        c, run = top_run(src)
        whole = (run == len(src))
        for m in range(1, run + 1):
            if m == run and not whole:
                continue
            for b, dst in enumerate(state):
                if a == b:
                    continue
                if caps[b] - len(dst) < m:
                    continue
                if dst and dst[-1] == c:
                    continue
                if not dst and whole and m == run:
                    continue
                res.append((a, b, m))
    return res


def apply_inverse(state, m):
    return apply_move(state, m)  # misma operacion: mover m unidades de a hacia b


def seed_int(seed_str):
    return int(hashlib.sha256(seed_str.encode()).hexdigest()[:16], 16)


def generate_inverse(K, empties, caps, units, depth, seed_str, min_complexity, target_empties=True, tries=200):
    """Barajado inverso con "mejor prefijo".
    Hecho estructural (medido en este prototipo): un movimiento inverso solo puede CREAR cortes de color, nunca
    deshacerlos, y una capa colocada sobre otro color queda fija para el resto del paseo. Por eso el paseo muere
    por falta de candidatos a profundidad ~12-26 (K 5-11) y 'depth' actua como tope, no como objetivo. El paseo
    usa los vacios de forma transitoria; se devuelve el estado MAS PROFUNDO que cumple todos los filtros
    (vacios == objetivo, complejidad >= minimo, ninguna cuia sellada, no resuelto).
    Devuelve (estado_inicial, intentos) o (None, intentos). Semilla determinista."""
    base_rng = random.Random(seed_int(seed_str))
    for attempt in range(1, tries + 1):
        rng = random.Random(base_rng.getrandbits(64))
        solved = []
        color = 1
        for cap in caps:
            if color <= K and cap == units.get(color, 4):
                solved.append(tuple([color] * cap))
                color += 1
            else:
                solved.append(tuple())
        if color <= K:
            raise ValueError("caps no alojan todos los colores")
        state = tuple(solved)
        visited = {canon(state, caps)}
        best = None
        for _ in range(depth):
            cands = []
            for m in inverse_moves(state, caps, units):
                t = apply_inverse(state, m)
                kt = canon(t, caps)
                if kt in visited:
                    continue
                score = rng.random() * 2.0
                if target_empties and sum(1 for x in t if not x) < empties:
                    score -= 4.0          # sesgo suave: no gastar los vacios objetivo
                cands.append((score, t, kt))
            if not cands:
                break
            cands.sort(key=lambda x: -x[0])
            _, state, kt = cands[rng.randrange(min(3, len(cands)))]
            visited.add(kt)
            if is_goal(state, units) or any(is_complete(t, units) for t in state):
                continue
            if complexity(state, caps, units) < min_complexity:
                continue
            if target_empties and sum(1 for x in state if not x) != empties:
                continue
            best = state
        if best is not None:
            return best, attempt
    return None, tries


def generate_inverse_deep(K, empties, caps, units, seed_str, min_complexity, max_nodes=20_000, tries=5):
    """Barajado inverso en profundidad (DFS con retroceso): explora hasta max_nodes estados del paseo inverso y
    devuelve el estado valido MAS PROFUNDO encontrado. Produce tableros de 1 vacio con fallo de bot 0,7-0,97
    (medido) manteniendo la garantia de resolubilidad por construccion. Coste medido: ~0,4-0,6 s por nivel."""
    base_rng = random.Random(seed_int(seed_str) ^ 0xDEE9)
    for attempt in range(1, tries + 1):
        rng = random.Random(base_rng.getrandbits(64))
        solved = []
        color = 1
        for cap in caps:
            if color <= K and cap == units.get(color, 4):
                solved.append(tuple([color] * cap))
                color += 1
            else:
                solved.append(tuple())
        start = tuple(solved)
        visited = {canon(start, caps)}
        best = (0, None)
        nodes = 0
        stack = [(start, None, 0)]
        while stack and nodes < max_nodes:
            state, cands, d = stack[-1]
            if cands is None:
                cands = []
                for m in inverse_moves(state, caps, units):
                    t = apply_inverse(state, m)
                    kt = canon(t, caps)
                    if kt in visited:
                        continue
                    cands.append((rng.random(), t, kt))
                cands.sort(key=lambda x: -x[0])
                stack[-1] = (state, cands, d)
            if not cands:
                stack.pop()
                continue
            _, t, kt = cands.pop()
            visited.add(kt)
            nodes += 1
            valid = (sum(1 for x in t if not x) == empties and not is_goal(t, units)
                     and not any(is_complete(x, units) for x in t)
                     and complexity(t, caps, units) >= min_complexity)
            if valid and d + 1 > best[0]:
                best = (d + 1, t)
            stack.append((t, None, d + 1))
        if best[1] is not None:
            return best[1], attempt
    return None, tries


def generate_random_fill(K, empties, caps, units, seed_str, budget=300_000, max_candidates=400):
    """Relleno aleatorio (Fisher-Yates sobre K*4 unidades) + rechazo con A*. Con 2 vacios casi todo es resoluble
    (30/30 medido); con 1 vacio la aceptacion cae 13 % (K=6) -> 1 % (K=9) -> 0,3 % (K=10) -> 0 % (K=12).
    Devuelve (estado, candidatos_probados, resultado_solver) o (None, n, None)."""
    rng = random.Random(seed_int(seed_str) ^ 0xF177)
    for n in range(1, max_candidates + 1):
        ul = [c for c in range(1, K + 1) for _ in range(units[c])]
        rng.shuffle(ul)
        tubes = []
        pos = 0
        for cap in caps[:K]:
            tubes.append(tuple(ul[pos:pos + cap]))
            pos += cap
        tubes += [tuple()] * empties
        state = tuple(tubes)
        if any(is_complete(t, units) for t in state):
            continue
        r = astar(state, caps, units, budget=budget)
        if r.status == "solved" and r.par >= 2 * K:
            return state, n, r
    return None, max_candidates, None


# ----------------------------------------------------------------------------
# 7. Variante aliviada ("El rio bajo")
# ----------------------------------------------------------------------------

def relief_variants(state, caps, units, K, budget=100_000):
    """+1 vacio (siempre resoluble: superconjunto) y -1 color (verificado con A*)."""
    out = {}
    s_plus = tuple(list(state) + [tuple()])
    caps_plus = tuple(list(caps) + [4])
    r = astar(s_plus, caps_plus, units, budget=budget)
    out["plus_empty"] = {"par": r.par, "status": r.status, "state": s_plus, "caps": caps_plus}
    best = None
    for c in range(1, K + 1):
        s_minus = tuple(tuple(x for x in t if x != c) for t in state)
        if is_goal(s_minus, units):
            continue
        r2 = astar(s_minus, caps, units, budget=budget)
        if r2.status == "solved" and (best is None or r2.par > best[1]):
            best = (c, r2.par, s_minus)
    out["minus_color"] = {"color": best[0], "par": best[1], "state": best[2]} if best else None
    return out


# ----------------------------------------------------------------------------
# 8. Aguas turbias: enumeracion de realidades y ruta de vertidos seguros
# ----------------------------------------------------------------------------

def realities(visible, hidden_pos, hidden_multiset):
    """Enumera todas las asignaciones distintas del multiconjunto oculto a las posiciones ocultas."""
    seen = set()
    for perm in itertools.permutations(hidden_multiset):
        if perm in seen:
            continue
        seen.add(perm)
        st = [list(t) for t in visible]
        for (ti, li), c in zip(hidden_pos, perm):
            st[ti][li] = c
        yield tuple(tuple(t) for t in st)


def murky_safe_path(true_state, caps, units, murky_cuias, hidden_depth, budget=20_000, max_realities=720):
    """Busca una ruta hasta la victoria en la que CADA vertido es seguro en todas las realidades
    compatibles con lo visible. Devuelve (ok, ruta, n_realidades_max, segundos)."""
    t0 = time.perf_counter()
    # hidden[(cuia, capa)] = True si la capa esta oculta. Al inicio: en cada cuia turbia, las hidden_depth
    # capas bajo la superior.
    hidden = set()
    for ci in murky_cuias:
        t = true_state[ci]
        for li in range(max(0, len(t) - 1 - hidden_depth), len(t) - 1):
            hidden.add((ci, li))
    state = true_state
    path = []
    max_r = 0
    memo = {}
    for _step in range(400):
        if is_goal(state, units):
            return True, path, max_r, time.perf_counter() - t0
        # revelar lo que haya quedado arriba
        hidden = {(ci, li) for (ci, li) in hidden if li < len(state[ci]) - 1}
        hidden_pos = sorted(hidden)
        hidden_ms = tuple(sorted(state[ci][li] for ci, li in hidden_pos))
        visible = tuple(tuple(x if (ci, li) not in hidden else 0 for li, x in enumerate(t)) for ci, t in enumerate(state))
        reals = list(itertools.islice(realities(visible, hidden_pos, hidden_ms), max_realities + 1))
        if len(reals) > max_realities:
            return False, path, len(reals), time.perf_counter() - t0
        max_r = max(max_r, len(reals))
        legal = moves(state, caps, units)      # los movimientos legales dependen solo de capas visibles
        safe = []
        for m in legal:
            ok = True
            for R in reals:
                tR = apply_move(R, m)
                kR = canon(tR, caps)
                st = memo.get(kR)
                if st is None:
                    st = "solved" if is_goal(tR, units) else astar(tR, caps, units, budget=budget).status
                    memo[kR] = st
                if st != "solved":
                    ok = False
                    break
            if ok:
                safe.append(m)
        if not safe:
            return False, path, max_r, time.perf_counter() - t0
        # elegir el seguro que deja menor par en la realidad verdadera
        best = None
        for m in safe:
            r = astar(apply_move(state, m), caps, units, budget=budget)
            if r.status == "solved" and (best is None or r.par < best[0]):
                best = (r.par, m)
        if best is None:
            return False, path, max_r, time.perf_counter() - t0
        m = best[1]
        path.append(m)
        state = apply_move(state, m)
    return False, path, max_r, time.perf_counter() - t0


# ----------------------------------------------------------------------------
# 9. Indice D y bandas
# ----------------------------------------------------------------------------

def zscores(values):
    if len(values) < 2:
        return [0.0 for _ in values]
    mu = statistics.fmean(values)
    sd = statistics.pstdev(values) or 1.0
    return [(v - mu) / sd for v in values]


def label_for(bot_fail):
    if bot_fail >= 0.90:
        return "very_hard"
    if bot_fail >= 0.75:
        return "hard"
    return "normal"


# ----------------------------------------------------------------------------
# 10. Empaquetado JSON
# ----------------------------------------------------------------------------

PALETTE = ["acai", "aguaje", "tapereba", "guanabana", "bacaba", "agua_tapajos", "camu_camu", "urucum",
           "agua_negro", "cupuacu", "lulo", "jenipapo", "cacao", "araza"]


def level_json(idx, spec, state, caps, metrics, seed_str, relief):
    K = spec["K"]
    doc = {
        "schema": "af-level/1",
        "id": f"proto_n{idx:04d}",
        "level": idx,
        "world": 0,
        "lap": 1,
        "seed": seed_str,
        "gen": {"tool": "levelgen_proto", "version": "0.1.0", "depth": spec["depth"]},
        "water_base": "white",
        "palette": PALETTE[:K],
        "units_per_color": 4,
        "caps": list(caps),
        "cuias": [list(t) for t in state],
        "difficulty": {
            "label": metrics["label"],
            "bot_fail": round(metrics["bot_fail"], 3),
            "par": metrics["par"],
            "branching": round(metrics["branching"], 2),
            "traps": round(metrics["traps"], 3),
            "complexity": metrics["complexity"],
            "D": round(metrics["D"], 3),
            "band": metrics["band"],
        },
        "risk_pairs": 0,
        "relief": {
            "plus_empty_par": relief["plus_empty"]["par"],
            "minus_color": relief["minus_color"]["color"] if relief["minus_color"] else None,
            "minus_color_par": relief["minus_color"]["par"] if relief["minus_color"] else None,
        },
    }
    return doc


# ----------------------------------------------------------------------------
# 11. Programa principal
# ----------------------------------------------------------------------------

def build_specs(n_levels):
    """20 especificaciones entre 7 y 12 cuias (K colores + E vacios), capacidad 4, con la fuente por banda:
    'walk' = inverso aleatorio mejor prefijo (comodidad/recuperacion), 'deep' = inverso DFS profundo (reto/hito
    con 1 vacio), 'fill' = relleno aleatorio + rechazo (2 vacios: espectro amplio; 1 vacio solo K<=9)."""
    base = [(5, 2, "walk"), (6, 2, "walk"), (6, 1, "walk"), (7, 2, "walk"), (8, 2, "walk"), (9, 2, "walk"),
            (10, 2, "walk"), (11, 1, "walk"),
            (7, 1, "deep"), (8, 1, "deep"), (9, 1, "deep"), (10, 1, "deep"), (11, 1, "deep"), (10, 1, "deep"),
            (6, 2, "fill"), (7, 1, "fill"), (8, 2, "fill"), (9, 1, "fill"), (10, 2, "fill"), (10, 2, "fill")]
    specs = []
    for i in range(n_levels):
        K, E, src = base[i % len(base)]
        caps = tuple([4] * (K + E))
        specs.append({"K": K, "E": E, "caps": caps, "depth": 6 * K, "min_complexity": 3 * K, "source": src})
    return specs


def run_batch(n_levels, budget, bot_runs, seed_prefix, out_dir, verbose=True):
    specs = build_specs(n_levels)
    rows = []
    memo_global = {}
    t_batch = time.perf_counter()
    for idx, spec in enumerate(specs, start=1):
        K, E, caps = spec["K"], spec["E"], spec["caps"]
        units = {c: 4 for c in range(1, K + 1)}
        seed_str = f"{seed_prefix}:proto:n{idx:04d}"
        t0 = time.perf_counter()
        src = spec["source"]
        sol = None
        if src == "fill":
            state, attempts, sol = generate_random_fill(K, E, caps, units, seed_str, budget=budget)
            t_gen = time.perf_counter() - t0 - (sol.seconds if sol else 0.0)
        else:
            if src == "deep":
                state, attempts = generate_inverse_deep(K, E, caps, units, seed_str, spec["min_complexity"])
            else:
                state, attempts = generate_inverse(K, E, caps, units, spec["depth"], seed_str, spec["min_complexity"])
            t_gen = time.perf_counter() - t0
            if state is not None:
                extra_tries = 0
                while True:
                    sol = astar(state, caps, units, budget=budget)
                    if sol.status == "solved" and sol.par >= 2 * K:
                        break
                    extra_tries += 1
                    if extra_tries > 5:
                        break
                    if src == "deep":
                        state, a2 = generate_inverse_deep(K, E, caps, units, seed_str + f":r{extra_tries}", spec["min_complexity"])
                    else:
                        state, a2 = generate_inverse(K, E, caps, units, spec["depth"], seed_str + f":r{extra_tries}", spec["min_complexity"])
                    attempts += a2
                    if state is None:
                        break
        if state is None or sol is None or sol.status != "solved":
            rows.append({"idx": idx, "K": K, "E": E, "status": "gen_fail" if state is None else "solve_fail",
                         "attempts": attempts, "expanded": sol.expanded if sol else None, "source": src})
            continue
        t1 = time.perf_counter()
        fail = bot_fail_rate(state, caps, units, seed_int(seed_str) ^ 0xB07, runs=bot_runs)
        t_bot = time.perf_counter() - t1
        t2 = time.perf_counter()
        br = branching_along(state, sol.path, caps, units)
        traps, unknown, n_pairs = trap_density(state, sol.path, caps, units, memo_global)
        t_traps = time.perf_counter() - t2
        t3 = time.perf_counter()
        relief = relief_variants(state, caps, units, K, budget=min(budget, 100_000))
        t_relief = time.perf_counter() - t3
        rows.append({
            "idx": idx, "K": K, "E": E, "T": len(caps), "status": "ok", "attempts": attempts, "source": src,
            "state": state, "caps": caps, "units": units, "seed": seed_str, "spec": spec,
            "par": sol.par, "expanded": sol.expanded, "t_solve": sol.seconds, "t_gen": t_gen,
            "bot_fail": fail, "t_bot": t_bot, "branching": br, "traps": traps, "traps_unknown": unknown,
            "n_pairs": n_pairs, "t_traps": t_traps, "complexity": complexity(state, caps, units),
            "relief": relief, "t_relief": t_relief, "t_total": time.perf_counter() - t0,
        })
        if verbose:
            r = rows[-1]
            print(f"[{idx:02d}] {src} K={K} E={E} T={len(caps)} intentos={attempts} par={r['par']} "
                  f"exp={r['expanded']} solve={r['t_solve']*1000:.0f}ms bot_fail={fail:.2f} "
                  f"ram={br:.2f} trampas={traps:.2f}(+{unknown:.2f}?) total={r['t_total']:.2f}s", flush=True)
    t_batch = time.perf_counter() - t_batch

    ok = [r for r in rows if r["status"] == "ok"]
    # Indice D con z-scores dentro del lote (en produccion: contra el pool del rango)
    if ok:
        z_fail = zscores([r["bot_fail"] for r in ok])
        z_par = zscores([r["par"] - 2 * r["K"] for r in ok])
        z_br = zscores([r["branching"] for r in ok])
        z_tr = zscores([r["traps"] for r in ok])
        for r, a, b, c, d in zip(ok, z_fail, z_par, z_br, z_tr):
            r["D"] = 0.35 * a + 0.25 * b + 0.20 * c + 0.10 * d + 0.10 * (1 if r["E"] == 1 else 0)
            r["label"] = label_for(r["bot_fail"])
        Ds = sorted(r["D"] for r in ok)
        def band(D):
            q = sum(1 for x in Ds if x <= D) / len(Ds)
            if q <= 0.10: return "recovery"
            if q <= 0.70: return "comfort"
            if q <= 0.95: return "challenge"
            return "milestone"
        for r in ok:
            r["band"] = band(r["D"])

    # JSON
    os.makedirs(out_dir, exist_ok=True)
    docs = []
    for r in ok:
        metrics = {k: r[k] for k in ("label", "bot_fail", "par", "branching", "traps", "complexity", "D", "band")}
        docs.append(level_json(r["idx"], r["spec"], r["state"], r["caps"], metrics, r["seed"], r["relief"]))
    pack = {"schema": "af-pack/1", "pack": "proto", "gen_version": "0.1.0", "levels": docs}
    path_pack = os.path.join(out_dir, "levels_proto.json")
    with open(path_pack, "w", encoding="utf-8") as f:
        json.dump(pack, f, ensure_ascii=False, separators=(",", ":"))
    sizes = [len(json.dumps(d, ensure_ascii=False, separators=(",", ":")).encode()) for d in docs]
    return rows, ok, docs, sizes, path_pack, t_batch


def markdown_report(rows, ok, sizes, path_pack, t_batch, bot_runs, budget):
    out = []
    out.append(f"Lote: {len(rows)} niveles, {len(ok)} aceptados, tiempo total {t_batch:.1f} s "
               f"(Python {sys.version.split()[0]}, presupuesto A* {budget}, bot x{bot_runs}).")
    out.append("")
    out.append("| # | Fuente | K | Vacios | Cuias | Intentos gen | Par | Estados A* | t solve (ms) | Fallo bot | Ramif. | Trampas | Compl. | D | Banda | Etiqueta | Alivio +1 vacio par | Alivio -1 color (par) | t total (s) |")
    out.append("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|")
    for r in rows:
        if r["status"] != "ok":
            out.append(f"| {r['idx']} | {r.get('source','-')} | {r['K']} | {r['E']} | - | {r['attempts']} | - | {r.get('expanded','-')} | - | - | - | - | - | - | - | {r['status']} | - | - | - |")
            continue
        rel = r["relief"]
        mc = rel["minus_color"]
        mc_txt = f"{mc['color']} ({mc['par']})" if mc else "-"
        out.append(f"| {r['idx']} | {r['source']} | {r['K']} | {r['E']} | {r['T']} | {r['attempts']} | {r['par']} | {r['expanded']} | "
                   f"{r['t_solve']*1000:.0f} | {r['bot_fail']:.2f} | {r['branching']:.2f} | {r['traps']:.2f} | "
                   f"{r['complexity']} | {r['D']:+.2f} | {r['band']} | {r['label']} | {rel['plus_empty']['par']} | {mc_txt} | {r['t_total']:.2f} |")
    if ok:
        out.append("")
        out.append("| Fuente | n | Fallo bot medio | Fallo bot min-max | Par medio | Par/2K | t gen mediana (s) | t total mediana (s) |")
        out.append("|---|---|---|---|---|---|---|---|")
        for src in ("walk", "deep", "fill"):
            g = [r for r in ok if r["source"] == src]
            if not g:
                continue
            out.append(f"| {src} | {len(g)} | {statistics.fmean(r['bot_fail'] for r in g):.2f} | "
                       f"{min(r['bot_fail'] for r in g):.2f}-{max(r['bot_fail'] for r in g):.2f} | "
                       f"{statistics.fmean(r['par'] for r in g):.1f} | {statistics.fmean(r['par']/(2*r['K']) for r in g):.2f} | "
                       f"{statistics.median(r['t_gen'] for r in g):.2f} | {statistics.median(r['t_total'] for r in g):.2f} |")
        ts = [r["t_solve"] for r in ok]
        out.append("")
        out.append(f"Solver A*: mediana {statistics.median(ts)*1000:.0f} ms, media {statistics.fmean(ts)*1000:.0f} ms, "
                   f"max {max(ts)*1000:.0f} ms; estados expandidos mediana {int(statistics.median(r['expanded'] for r in ok))}, "
                   f"max {max(r['expanded'] for r in ok)}.")
        out.append(f"Bot casual x{bot_runs}: mediana {statistics.median(r['t_bot'] for r in ok)*1000:.0f} ms por nivel.")
        out.append(f"Trampas (hasta 120 pares, A* 3k): mediana {statistics.median(r['t_traps'] for r in ok):.2f} s por nivel.")
        out.append(f"Variantes de alivio: mediana {statistics.median(r['t_relief'] for r in ok):.2f} s por nivel.")
        out.append(f"Pipeline completo por nivel: mediana {statistics.median(r['t_total'] for r in ok):.2f} s, max {max(r['t_total'] for r in ok):.2f} s.")
        out.append(f"Tamanio JSON minificado por nivel: min {min(sizes)} B, mediana {int(statistics.median(sizes))} B, max {max(sizes)} B; "
                   f"pack de {len(ok)} niveles: {os.path.getsize(path_pack)} B -> {path_pack}")
    return "\n".join(out)


def size_run(n, budget, seed_prefix, out_dir):
    """Lote rapido (sin bot ni trampas) para medir bytes por pack de 100."""
    specs = [(5, 2), (6, 2), (7, 2), (7, 1), (8, 2), (8, 1), (9, 2), (9, 1)]
    docs = []
    t0 = time.perf_counter()
    for i in range(1, n + 1):
        K, E = specs[i % len(specs)]
        caps = tuple([4] * (K + E))
        units = {c: 4 for c in range(1, K + 1)}
        seed_str = f"{seed_prefix}:size:n{i:04d}"
        state, _ = generate_inverse(K, E, caps, units, 6 * K, seed_str, 3 * K)
        if state is None:
            continue
        sol = astar(state, caps, units, budget=budget)
        if sol.status != "solved":
            continue
        metrics = {"label": "normal", "bot_fail": 0.0, "par": sol.par, "branching": 0.0, "traps": 0.0,
                   "complexity": complexity(state, caps, units), "D": 0.0, "band": "comfort"}
        relief = {"plus_empty": {"par": None}, "minus_color": None}
        docs.append(level_json(i, {"K": K, "E": E, "depth": 6 * K}, state, caps, metrics, seed_str, relief))
    pack = {"schema": "af-pack/1", "pack": "size", "gen_version": "0.1.0", "levels": docs}
    p = os.path.join(out_dir, f"levels_size_{n}.json")
    with open(p, "w", encoding="utf-8") as f:
        json.dump(pack, f, ensure_ascii=False, separators=(",", ":"))
    import gzip
    gz = len(gzip.compress(json.dumps(pack, ensure_ascii=False, separators=(",", ":")).encode()))
    return len(docs), os.path.getsize(p), gz, time.perf_counter() - t0


def murky_demo(seed_prefix, out_dir):
    """Nivel de 8 cuias (6 colores + 2 vacios) con 2 cuias turbias de 2 capas ocultas cada una."""
    K, E = 6, 2
    caps = tuple([4] * (K + E))
    units = {c: 4 for c in range(1, K + 1)}
    results = []
    for n in range(1, 6):
        seed_str = f"{seed_prefix}:murky:n{n:04d}"
        state, _ = generate_inverse(K, E, caps, units, 6 * K, seed_str, 3 * K)
        if state is None:
            continue
        sol = astar(state, caps, units)
        if sol.status != "solved":
            continue
        # elegir las dos cuias llenas mas mezcladas como turbias
        full = sorted([i for i, t in enumerate(state) if len(t) == 4], key=lambda i: -len(set(state[i])))
        murky = full[:2]
        ok, path, n_real, secs = murky_safe_path(state, caps, units, murky, hidden_depth=2)
        results.append({"seed": seed_str, "murky": murky, "ok": ok, "safe_path_len": len(path), "par": sol.par,
                        "realities_max": n_real, "seconds": round(secs, 2)})
    with open(os.path.join(out_dir, "murky_demo.json"), "w", encoding="utf-8") as f:
        json.dump(results, f, indent=1)
    return results


def hint_benchmark(ok_rows, budget=40_000, weight=2.0):
    """A* ponderado (w=2, 40k nodos) desde el estado inicial: coste de la pista Morpho en cliente."""
    ts, subopt = [], []
    for r in ok_rows:
        res = astar(r["state"], r["caps"], r["units"], budget=budget, weight=weight)
        if res.status == "solved":
            ts.append(res.seconds)
            subopt.append(res.par - r["par"])
    if not ts:
        return None
    return {"n": len(ts), "median_ms": statistics.median(ts) * 1000, "max_ms": max(ts) * 1000,
            "subopt_mean": statistics.fmean(subopt), "subopt_max": max(subopt)}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--levels", type=int, default=20)
    ap.add_argument("--budget", type=int, default=300_000)
    ap.add_argument("--bot-runs", type=int, default=40)
    ap.add_argument("--seed-prefix", default="amazonfill:v1")
    ap.add_argument("--out", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "out"))
    ap.add_argument("--size-run", type=int, default=0)
    ap.add_argument("--murky", action="store_true")
    ap.add_argument("--quiet", action="store_true")
    args = ap.parse_args()

    rows, ok, docs, sizes, path_pack, t_batch = run_batch(args.levels, args.budget, args.bot_runs,
                                                           args.seed_prefix, args.out, verbose=not args.quiet)
    report = markdown_report(rows, ok, sizes, path_pack, t_batch, args.bot_runs, args.budget)
    print("\n" + report)
    hb = hint_benchmark(ok)
    if hb:
        print(f"\nPista (A* w=2, 40k nodos) desde el inicio: n={hb['n']}, mediana {hb['median_ms']:.0f} ms, "
              f"max {hb['max_ms']:.0f} ms, suboptimo medio +{hb['subopt_mean']:.1f} movimientos (max +{hb['subopt_max']}).")
    # ejemplo de 7 cuias
    seven = [d for d in docs if len(d["caps"]) == 7]
    if seven:
        ex = json.dumps(seven[0], ensure_ascii=False, indent=1)
        with open(os.path.join(args.out, "example_7_cuias.json"), "w", encoding="utf-8") as f:
            f.write(ex)
        print(f"\nEjemplo de 7 cuias guardado ({len(json.dumps(seven[0], separators=(',', ':')).encode())} B minificado).")
    if args.size_run:
        n, b, gz, secs = size_run(args.size_run, args.budget, args.seed_prefix, args.out)
        print(f"\nPack de tamanio: {n} niveles, {b} B minificado ({b/max(n,1):.0f} B/nivel), {gz} B gzip, {secs:.1f} s.")
    if args.murky:
        res = murky_demo(args.seed_prefix, args.out)
        print("\nAguas turbias (2 cuias turbias x 2 capas ocultas):")
        for r in res:
            print(f"  {r['seed']}: ruta segura={'si' if r['ok'] else 'NO'} ({r['safe_path_len']} vertidos, par {r['par']}), "
                  f"realidades max {r['realities_max']}, {r['seconds']} s")
    with open(os.path.join(args.out, "report.md"), "w", encoding="utf-8") as f:
        f.write(report + "\n")


if __name__ == "__main__":
    main()
