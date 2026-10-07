#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
econ_variants.py — Sensibilidad de la economía de semillas de Aquazonia (Amazon Fill).

Corre tools/econ_sim.py (escenario B canónico) con 8 variantes de precio del rescate y de fuentes gratuitas
y resume, por variante: ratio de presión de referencia (media de 3 no pagadores) en 31-100 y 101-500,
saldo final del jugador medio y del casual al nivel 500, % de corridas del casual con al menos un
bloqueo sin recursos y rescates por día del medio. 200 corridas por perfil, semilla 2026.

Uso:  python tools/econ_variants.py
"""
import importlib.util, sys, random
sys.stdout.reconfigure(encoding="utf-8")
import os
spec = importlib.util.spec_from_file_location("econ_sim", os.path.join(os.path.dirname(os.path.abspath(__file__)), "econ_sim.py"))
m = importlib.util.module_from_spec(spec); sys.modules["econ_sim"] = m; spec.loader.exec_module(m)

def run(rescue=(600,600,600), hit3=300, cal_scale=1.0, win=(20,60,80), first_mult=0.5, runs=200):
    e = m.make_econ("B", rescue, None, 5)
    e["streak_hit3"] = hit3
    e["reward_normal"], e["reward_hard"], e["reward_very_hard"] = win
    e["rescue_first_day_mult"] = first_mult
    backup = list(m.CALENDAR)
    m.CALENDAR[:] = [(int(round(s * cal_scale)), it) for s, it in backup]
    out = {n: m.aggregate([m.Sim(p, e, random.Random(2026 * 1000 + i)).run() for i in range(runs)], e)
           for n, p in m.PROFILES.items()}
    m.CALENDAR[:] = backup
    return out

def ref_ratio(o, band):
    v = [o[n]["ratio"][band] for n in ("casual_sin_racha", "medio_no_pagador", "habil_con_racha")]
    v = [x["ratio"] if isinstance(x, dict) else x for x in v]
    return sum(v) / len(v)

VARIANTS = [
    ("V0 canon: rescate 600 fijo (1.º del día 300)", {}),
    ("V1 rescate 900 fijo (1.º del día 450)", dict(rescue=(900, 900, 900))),
    ("V2 rescate 750 fijo (1.º del día 375)", dict(rescue=(750, 750, 750))),
    ("V3 600 fijo + Crecida 3→150 + Bitácora 1.500/mes", dict(hit3=150, cal_scale=0.5)),
    ("V4 750 fijo + Crecida 3→150 + Bitácora 1.500/mes", dict(rescue=(750, 750, 750), hit3=150, cal_scale=0.5)),
    ("V5 900 fijo + Crecida 3→150 + Bitácora 1.500/mes", dict(rescue=(900, 900, 900), hit3=150, cal_scale=0.5)),
    ("V6 600 fijo sin descuento del 1.º del día", dict(first_mult=1.0)),
    ("V7 750 fijo + victoria 15/45/60 + Crecida 3→150", dict(rescue=(750, 750, 750), hit3=150, win=(15, 45, 60))),
]
print("| Variante | Ratio ref. 31-100 | Ratio ref. 101-500 | Saldo final medio | Saldo final casual | Casual con bloqueo sin recursos | Rescates/día medio |")
print("|---|---|---|---|---|---|---|")
for label, kw in VARIANTS:
    o = run(**kw)
    med, cas = o["medio_no_pagador"], o["casual_sin_racha"]
    print(f"| {label} | {ref_ratio(o,'31-100'):.1f}:1 | {ref_ratio(o,'101-500'):.1f}:1 | {med['balance_end']:,.0f} | {cas['balance_end']:,.0f} | "
          f"{cas['blocked_runs_pct']:.0f} % | {med['rescues_used']/med['days']:.2f} |".replace(",", "."))
