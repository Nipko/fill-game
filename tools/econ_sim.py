#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
econ_sim.py — Simulador de la economía de semillas de Amazon Fill (nombre comercial: Tintalú).

Simula 500 niveles de campaña para tres perfiles de jugador (casual sin racha, hábil con racha,
pagador ligero) más un perfil de referencia "medio no pagador", con la curva canónica del ciclo de 20
(03 §12: primer Difícil en el 40, primer Muy difícil en el 60), las tasas de fallo derivadas de los
intentos medios objetivo de 03 §15, los regalos del tutorial (04 §4), la Canasta de ciclo fijo (08 §9)
y la Cuia de rescate a 600 fija (contrato 4), y produce:

  * tabla por perfil: días de juego, saldo final/mínimo, ingreso efectivo por victoria,
    Cuias de rescate pagables con el ingreso gratuito, rescates usados, bloqueos sin recursos;
  * presupuesto por fase (1-30, 31-100, 101-500): ingreso, gasto y neto por día;
  * ratio de presión = precio nominal de la Cuia de rescate / ingreso efectivo por victoria;
  * estimación de ingresos por 1.000 DAU (LatAm-heavy y US-heavy) con supuestos explícitos.

Uso:
  python tools/econ_sim.py                     # escenario B: economía canónica (rescate 600 fijo, cap. 05)
  python tools/econ_sim.py --scenario A        # escenario A: dirección literal, sin ajustes
  python tools/econ_sim.py --runs 400 --seed 7 --json salida.json
  python tools/econ_sim.py --rescue 600,750,900   # probar tramos de rescate 31-100,101-200,201+ (canon: 600 fijo)

Todas las cifras de este archivo son parámetros de diseño [P] salvo donde el comentario cite un informe.
"""

from __future__ import annotations

import argparse
import json
import random
import statistics
import sys
from dataclasses import dataclass, field
from typing import Dict, List, Optional, Tuple

LEVELS = 500
MAX_DAYS = 400

try:  # salida UTF-8 también para --help en consolas Windows con otra página de códigos
    sys.stdout.reconfigure(encoding="utf-8")
except Exception:
    pass

# ---------------------------------------------------------------------------
# 1. Curva de dificultad (ciclo diente de sierra de 20 niveles, dirección 5)
# ---------------------------------------------------------------------------
# Etiquetas: normal | recovery (recuperación, mitad de fallo) | challenge (reto sin etiqueta) |
#            hard (Difícil) | very_hard (Muy difícil)
# Reglas respetadas: nunca dos Difícil seguidos; el hito va precedido de un reto (03 §12).

# Ciclo canónico de 20 niveles desde el 21 (03 §12): p = ((n − 1) mod 20) + 1.
#   p1-p2 = recuperación · p6, p10, p14, p17, p19 = reto · p20 = hito · resto = comodidad (12).
# Etiquetas (03 §12): 21-39 sin etiquetas (retos suaves = "challenge"); 40 = primer Difícil;
# 41-59: 3 de los 5 retos etiquetados Difícil (p6, p14, p19) y 2 suaves (p10, p17); 60 = primer Muy difícil;
# desde el 61: los 5 retos Difícil y el hito Muy difícil.
CHALLENGE_POSITIONS = {6, 10, 14, 17, 19}
RECOVERY_POSITIONS = {1, 2}
HITO_POSITION = 20
FIRST_HARD_LEVEL = 40
FIRST_VERY_HARD_LEVEL = 60


def cycle_position(level: int) -> int:
    return ((level - 1) % 20) + 1


def difficulty_tag(level: int) -> str:
    if level <= 20:
        return "normal"                      # rampa de tutorial a mano, sin etiquetas
    pos = cycle_position(level)
    if pos in RECOVERY_POSITIONS:
        return "recovery"
    if pos == HITO_POSITION:
        if level >= FIRST_VERY_HARD_LEVEL:
            return "very_hard"
        return "hard" if level >= FIRST_HARD_LEVEL else "challenge"
    if pos in CHALLENGE_POSITIONS:
        if level < FIRST_HARD_LEVEL:
            return "challenge"
        if level < FIRST_VERY_HARD_LEVEL:
            return "hard" if pos in (6, 14, 19) else "challenge"
        return "hard"
    return "normal"


# Tasa de fallo POR INTENTO del jugador casual de referencia (normal, Difícil, Muy difícil).
# Derivada de los intentos medios objetivo de 03 §15 con p_fail = 1 − 1/intentos_medios (ancla Royal Match
# 1,2 / 1,6 / 2,5, research/02 §5 [V]): normal 1,2 / 1,33 / 1,5 → 0,17 / 0,25 / 0,33 (31-100 / 101-200 / 201+);
# Difícil 1,6 / 1,8 / 2,2 → 0,38 / 0,45 / 0,55; Muy difícil 2,5 / 2,9 / 3,5 → 0,60 / 0,65 / 0,71.
# 1-20: 0,02 [P] (rampa a mano); 21-30: 0,10 normal [P]. Sustituye a la tabla provisional de research/04 C1 §5.
# Recalibrar con los intentos medidos del bot casual ×40 (03 §6) y con el embudo del soft launch.
FAIL_BASE: List[Tuple[int, int, float, float, float]] = [
    (1, 20, 0.02, 0.10, 0.20),
    (21, 30, 0.10, 0.38, 0.60),
    (31, 100, 0.17, 0.38, 0.60),
    (101, 200, 0.25, 0.45, 0.65),
    (201, 500, 0.33, 0.55, 0.71),
]


def base_fail(level: int, tag: str) -> float:
    for lo, hi, pn, ph, pv in FAIL_BASE:
        if lo <= level <= hi:
            if tag == "normal":
                return pn
            if tag == "recovery":
                return pn * 0.5
            if tag == "challenge":
                return (pn + ph) / 2         # reto sin etiqueta: entre comodidad y Difícil [P]
            if tag == "hard":
                return ph
            return pv
    raise ValueError(level)


def apply_skill(p: float, mult: float) -> float:
    """Multiplicador de habilidad sobre la probabilidad de fallo, acotado en [0,1]."""
    return 1.0 - (1.0 - p) ** mult


def band_of(level: int) -> str:
    if level <= 30:
        return "1-30"
    if level <= 100:
        return "31-100"
    return "101-500"


# ---------------------------------------------------------------------------
# 2. Economía (direcciones 9-14; valores no fijados por la dirección marcados [P])
# ---------------------------------------------------------------------------

# Bitácora del río: 30 casillas, 3.000 semillas/mes, zigzag con hitos 7/14/21/30 (research/04 C1 §3 [E]).
# (semillas, ítem) por casilla; ítem ∈ {None, "undo", "hint", "cuia"}. "reveal" del día 29 se modela como hint.
CALENDAR: List[Tuple[int, Optional[str]]] = [
    (50, None), (0, "undo"), (50, None), (0, "hint"), (100, None), (0, "cuia"), (250, None),
    (50, None), (50, None), (0, "undo"), (100, None), (0, "hint"), (100, None), (300, "cuia"),
    (50, None), (100, None), (0, "undo"), (100, None), (0, "undo"), (150, None), (400, None),
    (100, None), (0, "cuia"), (100, None), (0, "hint"), (150, None), (0, "undo"), (150, None),
    (0, "hint"), (650, None),
]
assert sum(s for s, _ in CALENDAR) == 3000, "La Bitácora debe sumar 3.000 semillas (dirección 12)"

# Canasta del hito 6 de la Crecida (glosario §6.3; capítulo 6 §4.4): 50 semillas + 1 comodín visible en ciclo fijo
# [3 Marea, 1 Morpho, 1 Cuia extra, 3 Marea, 1 Morpho]; una unidad "undo" del inventario = una Marea (+3 Deshacer).
STREAK_CHEST_CYCLE: List[str] = ["undo", "hint", "cuia", "undo", "hint"]
# Canasta cada 10 niveles: mismo ciclo determinista y visible (08 §9 eco_basket_cycle = ["undo3","hint","extra_cuia","undo3","hint"]).
BASKET_CYCLE: List[str] = ["undo", "hint", "cuia", "undo", "hint"]


def make_econ(scenario: str, rescue_bands: Optional[Tuple[int, int, int]] = None,
              hit10_seeds: Optional[int] = None, x2_cap: Optional[int] = None) -> Dict:
    a = scenario.upper() == "A"
    if rescue_bands is None:
        rescue_bands = (600, 600, 600)       # canon (05, contrato 4): 600 fijo; tramos solo como prueba por CLI
    if hit10_seeds is None:
        hit10_seeds = 300 if a else 0          # B: el hito 10 paga río crecido + Cuia extra, no semillas
    if x2_cap is None:
        x2_cap = 5                             # capítulo 4 §3: rv_double_harvest 5/día (dirección 13: 3-5)
    return dict(
        scenario=scenario.upper(),
        # Ingresos por victoria (dirección 12)
        reward_normal=20, reward_hard=60, reward_very_hard=80,
        # Cosecha doble: x2 por anuncio desde el nivel 11 (dirección 13); tope diario [P]
        x2_from_level=11, rv_x2_cap=x2_cap,
        # Crecida (racha de victorias): 3 → 300, 6 → Canasta (50 + comodín), 10 → Cuia extra + río crecido
        # (300 + río crecido en A); tope diario 600 sobre las semillas de la Crecida (incluidas las de la Canasta)
        streak_unlock=25, streak_hit3=300, streak_hit10=hit10_seeds, streak_daily_cap=600,
        streak_hits_once_per_day=not a,      # B: cada hito se paga una vez por día
        # Canasta cada 10 niveles: 50 semillas + comodín visible (capítulo 6 §4.4)
        chest_every=10, chest_first_seeds=50, chest_seeds=50,
        # Reto del río (reto diario): 60 semillas por victoria (capítulo 6 §4.1)
        daily_challenge_seeds=60, daily_challenge_item=None,
        # Cuia de rescate (contrato 4): 600 fijo desde el 31 (rescue_price_bands = [[31,600]]), primer rescate del día −50 %
        rescue_from_level=31, rescue_first_day_mult=0.5,
        rescue_bands=rescue_bands,
        # Comodines (dirección 9)
        extra_cuia_price=150, hint_price=100, undo_recharge_price=30, skip_price=300,
        gift_hint_level=12, gift_hint_units=3,            # 04 §4: 3 Morphos al stock en el 12 (la Cuia del 8 se gasta en su nivel)
        undo_unlimited_until_level=10,                    # contrato 17: Marea ilimitada en 1-10
        undo_free_per_level=3, undo_recover_p=0.30,          # [P] prob. de que un Deshacer en el bloqueo salve el intento
        cuia_fail_mult=0.40,        # +1 cuia ≈ bajar un escalón entero de dificultad (research/02 §7.3)
        relief_fail_mult=0.50,      # variante aliviada tras 3 fallos (dirección 7)
        hint_fail_mult=0.80,        # [P] una pista reduce el fallo del intento un 20 %
        rescue_win_p=0.90,          # objetivo "rescate → victoria" >85 % (research/03 C2.5)
        relief_after_fails=3, skip_after_fails=5, skip_min_gap=10,
        # Canoas (dirección 11)
        lives_max=5, lives_from_level=31,
        # Topes de anuncios recompensados por día (capítulo 4 §3: rv_undo_refill 4, rv_hint 3, rv_extra_canoe 3; global 15)
        rv_global_cap=15, rv_undo_cap=4, rv_hint_cap=3, rv_life_cap=3,
        # Cántaro (alcancía determinista, dirección 14): llenado por victoria [P], tope visible 3.000
        piggy_from_level=20, piggy_fill=(15, 30, 40), piggy_cap=3000, piggy_price=2.99,
        # IAP del pagador ligero
        starter_level=12, starter_price=0.99, starter_seeds=500, starter_cuias=2, starter_hints=3,
        punado_price=1.99, punado_seeds=1000,
        balsa_price=4.99, balsa_seeds=1000, balsa_cuias=1,
    )


def rescue_price(level: int, econ: Dict) -> int:
    b31, b101, b201 = econ["rescue_bands"]
    if level <= 100:
        return b31
    if level <= 200:
        return b101
    return b201


# ---------------------------------------------------------------------------
# 3. Perfiles de jugador
# ---------------------------------------------------------------------------

PROFILES: Dict[str, Dict] = {
    # Floor del no pagador: sesiones cortas, falla más, no sostiene la Crecida (ingreso de racha = 0).
    "casual_sin_racha": dict(
        label="Casual sin racha", fail_mult=1.25, sessions_per_day=2, attempts_per_session=4,
        p_x2=0.25, p_rescue=0.50, p_cuia_pre=0.05, p_cuia_pre_after_fail=0.15,
        p_hint=0.15, p_hint_seeds=0.20, p_undo_ad=0.30, p_life_ad=0.50, p_skip=0.30,
        p_daily_challenge=0.30, streak_engaged=False, payer=False,
    ),
    # Perfil de referencia adicional (no exigido por la tarea): habilidad media, 2 sesiones largas,
    # sostiene la Crecida. Sirve para el presupuesto diario del "medio" de research/04 C1 §4 (12 victorias/día).
    "medio_no_pagador": dict(
        label="Medio no pagador (ref.)", fail_mult=1.00, sessions_per_day=2, attempts_per_session=7,
        p_x2=0.50, p_rescue=0.60, p_cuia_pre=0.15, p_cuia_pre_after_fail=0.35,
        p_hint=0.25, p_hint_seeds=0.30, p_undo_ad=0.45, p_life_ad=0.60, p_skip=0.30,
        p_daily_challenge=0.60, streak_engaged=True, payer=False,
    ),
    # Techo del ingreso gratuito: falla menos, 3 sesiones, usa x2 y sostiene la Crecida con Hoja escudo.
    "habil_con_racha": dict(
        label="Hábil con racha", fail_mult=0.60, sessions_per_day=3, attempts_per_session=6,
        p_x2=0.70, p_rescue=0.60, p_cuia_pre=0.20, p_cuia_pre_after_fail=0.40,
        p_hint=0.30, p_hint_seeds=0.30, p_undo_ad=0.60, p_life_ad=0.70, p_skip=0.10,
        p_daily_challenge=0.80, streak_engaged=True, payer=False,
    ),
    # Pagador ligero: habilidad media, compra Mochila de inicio, Puñado cuando se queda sin saldo,
    # Cántaro al llenarse y ocasionalmente Balsa de rescate. Presupuesto ≈ 8 USD / 30 días.
    "pagador_ligero": dict(
        label="Pagador ligero", fail_mult=1.00, sessions_per_day=3, attempts_per_session=5,
        p_x2=0.50, p_rescue=0.80, p_cuia_pre=0.25, p_cuia_pre_after_fail=0.50,
        p_hint=0.25, p_hint_seeds=0.40, p_undo_ad=0.40, p_life_ad=0.60, p_skip=0.50,
        p_daily_challenge=0.60, streak_engaged=True, payer=True,
        buy_starter=True, p_buy_punado=0.60, p_buy_balsa=0.30, p_buy_piggy=0.50,
        purchase_cooldown_days=3, budget_usd_30d=8.0,
    ),
}


# ---------------------------------------------------------------------------
# 4. Simulación
# ---------------------------------------------------------------------------

@dataclass
class PhaseStats:
    days: float = 0.0
    victories: int = 0
    income: float = 0.0
    spend: float = 0.0
    rescues: int = 0
    blocked: int = 0
    balance_end: float = 0.0


@dataclass
class RunResult:
    days: int
    final_level: int
    balance_end: float
    balance_min: float
    victories: int
    attempts: int
    income: Dict[str, float]
    spend: Dict[str, float]
    rescues_used: int
    rescues_first_day: int
    blocked_no_resources: int
    first_block_level: Optional[int]
    skips: int
    rv_watched: int
    usd_spent: float
    purchases: Dict[str, int]
    phases: Dict[str, PhaseStats]
    income_by_band: Dict[str, float]
    victories_by_band: Dict[str, int]


class Sim:
    def __init__(self, profile: Dict, econ: Dict, rng: random.Random):
        self.p = profile
        self.e = econ
        self.rng = rng
        # progreso
        self.level = 1
        self.day = 0
        self.balance = 0.0
        self.balance_min = 0.0
        self.victories = 0
        self.attempts = 0
        # estado del nivel
        self.fails_on_level = 0
        self.undos_left = econ["undo_free_per_level"]
        self.has_extra_cuia = False
        self.relief = False
        self.last_skip_level = -999
        self.gift_hint_done = False
        # racha
        self.streak = 0
        self.rio_crecido = False
        self.shield_available = False
        self.streak_seeds_today = 0
        self.hits_today: set = set()
        self.streak_chests = 0                 # Canastas del hito 6 cobradas (índice del ciclo de comodín)
        # vidas / anuncios / día
        self.lives = econ["lives_max"]
        self.infinite_canoes = False
        self.rv_today = 0
        self.rv_counts: Dict[str, int] = {}
        self.rescued_today = False
        self.calendar_index = 0
        # inventario
        self.inv = {"undo": 0, "hint": 0, "cuia": 0}
        # pagador
        self.usd_spent = 0.0
        self.spent_30d: List[Tuple[int, float]] = []
        self.last_purchase_day = -999
        self.purchases: Dict[str, int] = {}
        self.piggy = 0.0
        # métricas
        self.income: Dict[str, float] = {}
        self.spend: Dict[str, float] = {}
        self.rescues_used = 0
        self.rescues_first_day = 0
        self.blocked = 0
        self.first_block_level: Optional[int] = None
        self.skips = 0
        self.rv_watched = 0
        self.phases: Dict[str, PhaseStats] = {b: PhaseStats() for b in ("1-30", "31-100", "101-500")}
        self.income_by_band: Dict[str, float] = {}
        self.victories_by_band: Dict[str, int] = {}
        self.day_phase_start = 0
        self.current_band = "1-30"

    # ----- utilidades -----
    def add_income(self, source: str, amount: float):
        if amount <= 0:
            return
        self.balance += amount
        self.income[source] = self.income.get(source, 0.0) + amount
        b = band_of(self.level)
        self.phases[b].income += amount
        self.income_by_band[b] = self.income_by_band.get(b, 0.0) + amount

    def add_spend(self, sink: str, amount: float):
        self.balance -= amount
        self.balance_min = min(self.balance_min, self.balance)
        self.spend[sink] = self.spend.get(sink, 0.0) + amount
        self.phases[band_of(self.level)].spend += amount

    def rv_available(self, placement: str, cap_key: str) -> bool:
        if self.rv_today >= self.e["rv_global_cap"]:
            return False
        return self.rv_counts.get(placement, 0) < self.e[cap_key]

    def watch_rv(self, placement: str):
        self.rv_today += 1
        self.rv_counts[placement] = self.rv_counts.get(placement, 0) + 1
        self.rv_watched += 1

    def reward_for(self, tag: str) -> int:
        return {"normal": self.e["reward_normal"], "recovery": self.e["reward_normal"], "challenge": self.e["reward_normal"],
                "hard": self.e["reward_hard"], "very_hard": self.e["reward_very_hard"]}[tag]

    def budget_left(self) -> float:
        self.spent_30d = [(d, u) for d, u in self.spent_30d if self.day - d < 30]
        return self.p.get("budget_usd_30d", 0.0) - sum(u for _, u in self.spent_30d)

    def buy(self, sku: str, price: float, seeds: float = 0.0):
        self.usd_spent += price
        self.spent_30d.append((self.day, price))
        self.last_purchase_day = self.day
        self.purchases[sku] = self.purchases.get(sku, 0) + 1
        if seeds:
            self.add_income(f"iap:{sku}", seeds)

    # ----- ciclo de día -----
    def start_day(self):
        self.day += 1
        self.rv_today = 0
        self.rv_counts = {}
        self.rescued_today = False
        self.shield_available = self.level >= self.e["streak_unlock"]
        self.streak_seeds_today = 0
        self.hits_today = set()
        # Bitácora del río: reclamo secuencial sin reinicio (exige ≥1 nivel jugado; el perfil siempre juega)
        seeds, item = CALENDAR[self.calendar_index % 30]
        self.calendar_index += 1
        if seeds:
            self.add_income("bitacora", seeds)
        if item:
            self.inv[item] += 1
        # Reto del río
        if self.level >= 10 and self.rng.random() < self.p["p_daily_challenge"]:
            self.add_income("reto_del_rio", self.e["daily_challenge_seeds"])
            if self.e["daily_challenge_item"]:
                self.inv[self.e["daily_challenge_item"]] += 1
        # Pagador: Mochila de inicio en su ventana (niveles 10-20)
        if self.p.get("buy_starter") and "af_starter" not in self.purchases and \
                self.e["starter_level"] <= self.level <= 20:
            self.buy("af_starter", self.e["starter_price"], self.e["starter_seeds"])
            self.inv["cuia"] += self.e["starter_cuias"]
            self.inv["hint"] += self.e["starter_hints"]

    def run(self) -> RunResult:
        while self.level <= LEVELS and self.day < MAX_DAYS:
            self.start_day()
            for _ in range(self.p["sessions_per_day"]):
                self.lives = self.e["lives_max"]          # las canoas se regeneran entre sesiones (25 min × 5)
                self.infinite_canoes = False
                attempts = 0
                while attempts < self.p["attempts_per_session"] and self.level <= LEVELS:
                    if self.level >= self.e["lives_from_level"] and self.lives <= 0 and not self.infinite_canoes:
                        if self.rv_available("life", "rv_life_cap") and self.rng.random() < self.p["p_life_ad"]:
                            self.watch_rv("life")       # Canoa extra
                            self.lives = 1
                        else:
                            break                       # fin de sesión: sin canoas
                    self.play_attempt()
                    attempts += 1
            self.end_day()
        return self.result()

    def end_day(self):
        pass

    # ----- un intento de nivel -----
    def play_attempt(self):
        level, e, p, rng = self.level, self.e, self.p, self.rng
        tag = difficulty_tag(level)
        if level == e["gift_hint_level"] and not self.gift_hint_done:
            self.inv["hint"] += e["gift_hint_units"]
            self.gift_hint_done = True
        if level <= e["undo_unlimited_until_level"]:
            self.undos_left = max(self.undos_left, 1)
        pf = apply_skill(base_fail(level, tag), p["fail_mult"])
        self.attempts += 1

        # Cuia extra planificada (solo mientras haya vertido legal → antes del intento; 1 por nivel; persiste)
        if tag in ("hard", "very_hard") and not self.has_extra_cuia:
            prob = p["p_cuia_pre_after_fail"] if self.fails_on_level >= 1 else p["p_cuia_pre"]
            if rng.random() < prob:
                if self.inv["cuia"] > 0:
                    self.inv["cuia"] -= 1
                    self.has_extra_cuia = True
                elif self.balance >= e["extra_cuia_price"]:
                    self.add_spend("cuia_extra", e["extra_cuia_price"])
                    self.has_extra_cuia = True
        # Morpho (pista): inventario → anuncio → semillas
        if tag not in ("normal", "recovery") and rng.random() < p["p_hint"]:
            used = False
            if self.inv["hint"] > 0:
                self.inv["hint"] -= 1
                used = True
            elif self.rv_available("hint", "rv_hint_cap"):
                self.watch_rv("hint")
                used = True
            elif self.balance >= e["hint_price"] and rng.random() < p["p_hint_seeds"]:
                self.add_spend("morpho", e["hint_price"])
                used = True
            if used:
                pf *= e["hint_fail_mult"]
        if self.has_extra_cuia:
            pf *= e["cuia_fail_mult"]
        if self.relief:
            pf *= e["relief_fail_mult"]

        if rng.random() >= pf:
            self.on_win(tag)
            return

        # ---- Aguas estancadas (bloqueo) ----
        # 1) Marea (Deshacer) usable en el bloqueo
        if self.undos_left <= 0:
            if self.inv["undo"] > 0:
                self.inv["undo"] -= 1
                self.undos_left = 3
            elif self.rv_available("undo", "rv_undo_cap") and rng.random() < p["p_undo_ad"]:
                self.watch_rv("undo")
                self.undos_left = 3
        if self.undos_left > 0:
            self.undos_left -= 1
            if rng.random() < e["undo_recover_p"]:
                self.on_win(tag)
                return
        # 2) Cuia de rescate (desde el nivel 31)
        if level >= e["rescue_from_level"]:
            price = rescue_price(level, e) * (e["rescue_first_day_mult"] if not self.rescued_today else 1.0)
            wants = rng.random() < p["p_rescue"]
            if wants and self.balance < price and p.get("payer"):
                self.try_purchase_for_rescue(price)
            if wants and self.balance >= price:
                self.add_spend("cuia_de_rescate", price)
                self.rescues_used += 1
                self.phases[band_of(level)].rescues += 1
                if not self.rescued_today:
                    self.rescues_first_day += 1
                self.rescued_today = True
                self.has_extra_cuia = True              # la cuia persiste todo el nivel (research/03 C2.4)
                if rng.random() < e["rescue_win_p"]:
                    self.on_win(tag, rescued=True)
                    return
                self.on_fail(lost_life=False)           # el rescate no cumplió: cuenta como fallo sin canoa
                return
            if wants and self.balance < price:
                self.blocked += 1
                self.phases[band_of(level)].blocked += 1
                if self.first_block_level is None:
                    self.first_block_level = level
        # 3) Volver a la orilla (reinicio gratis)
        self.on_fail(lost_life=level >= e["lives_from_level"])

    def try_purchase_for_rescue(self, price: float):
        """Pagador ligero sin saldo en el bloqueo: Puñado 1,99 o Balsa de rescate 4,99 (1/día)."""
        e, p, rng = self.e, self.p, self.rng
        if self.day - self.last_purchase_day < p["purchase_cooldown_days"]:
            return
        if self.budget_left() < e["punado_price"]:
            return
        if rng.random() < p["p_buy_balsa"] and self.budget_left() >= e["balsa_price"] \
                and "balsa_today" not in self.rv_counts:
            self.rv_counts["balsa_today"] = 1
            self.buy("af_rescue", e["balsa_price"], e["balsa_seeds"])
            self.inv["cuia"] += e["balsa_cuias"]
            self.infinite_canoes = True                 # Canoas infinitas durante la sesión
        elif rng.random() < p["p_buy_punado"]:
            self.buy("af_coins_2", e["punado_price"], e["punado_seeds"])

    def on_win(self, tag: str, rescued: bool = False):
        e, p, rng = self.e, self.p, self.rng
        base = self.reward_for(tag)
        self.add_income("victorias", base)
        if self.level >= e["x2_from_level"]:
            if self.rio_crecido:
                self.add_income("cosecha_doble", base)  # x2 sin anuncio (río crecido)
            elif self.rv_available("x2", "rv_x2_cap") and rng.random() < p["p_x2"]:
                self.watch_rv("x2")
                self.add_income("cosecha_doble", base)
        # Crecida
        if p["streak_engaged"] and self.level >= e["streak_unlock"]:
            self.streak += 1
            if self.streak == 3:
                self.pay_streak_hit(3, e["streak_hit3"])
            elif self.streak == 6:
                if not (e["streak_hits_once_per_day"] and 6 in self.hits_today):
                    # hito 6 = Canasta (contenido visible): 50 semillas (dentro del tope diario) + 1 comodín del ciclo
                    self.pay_streak_hit(6, e["chest_seeds"])
                    self.inv[STREAK_CHEST_CYCLE[self.streak_chests % len(STREAK_CHEST_CYCLE)]] += 1
                    self.streak_chests += 1
            elif self.streak >= 10:
                if e["streak_hit10"] > 0:
                    self.pay_streak_hit(10, e["streak_hit10"])
                elif not (e["streak_hits_once_per_day"] and 10 in self.hits_today):
                    self.hits_today.add(10)
                    self.inv["cuia"] += 1                 # hito 10 en B: Cuia extra + río crecido
                self.rio_crecido = True
                self.streak = 0
        # Canasta cada 10 niveles
        if self.level % e["chest_every"] == 0:
            k = self.level // e["chest_every"]                     # 1.ª Canasta en el nivel 10
            self.add_income("canasta", e["chest_seeds"])            # 50 semillas, también la primera (08 §9)
            self.inv[BASKET_CYCLE[(k - 1) % len(BASKET_CYCLE)]] += 1
        # Cántaro (solo se llena; el pagador puede comprarlo)
        if self.level >= e["piggy_from_level"] and self.piggy < e["piggy_cap"]:
            fill = {"normal": 0, "recovery": 0, "challenge": 0, "hard": 1, "very_hard": 2}[tag]
            self.piggy = min(e["piggy_cap"], self.piggy + e["piggy_fill"][fill])
            if self.piggy >= e["piggy_cap"] and p.get("payer") and rng.random() < p["p_buy_piggy"] \
                    and self.budget_left() >= e["piggy_price"] \
                    and self.day - self.last_purchase_day >= p["purchase_cooldown_days"]:
                self.buy("af_piggy", e["piggy_price"], e["piggy_cap"])
                self.piggy = 0
        self.victories += 1
        b = band_of(self.level)
        self.phases[b].victories += 1
        self.victories_by_band[b] = self.victories_by_band.get(b, 0) + 1
        self.advance_level()

    def pay_streak_hit(self, hit: int, amount: int):
        e = self.e
        if e["streak_hits_once_per_day"] and hit in self.hits_today:
            return
        room = e["streak_daily_cap"] - self.streak_seeds_today
        amt = max(0, min(amount, room))
        if amt > 0:
            self.add_income("crecida", amt)
            self.streak_seeds_today += amt
        self.hits_today.add(hit)

    def on_fail(self, lost_life: bool):
        e, p, rng = self.e, self.p, self.rng
        self.fails_on_level += 1
        if lost_life and not self.infinite_canoes:
            self.lives -= 1
        if p["streak_engaged"]:
            # 05 §2.4 on_hard_fail / contrato 19: la Hoja escudo solo protege una barra ≥ 1;
            # con la barra a 0 (p. ej. tras el hito 10) el fallo duro apaga el río crecido sin gastar el escudo.
            if self.streak >= 1 and self.shield_available:
                self.shield_available = False           # Hoja escudo: perdona 1 fallo al día
            else:
                self.streak = 0
                self.rio_crecido = False
        if self.fails_on_level >= e["relief_after_fails"]:
            self.relief = True                           # "El río bajó": variante aliviada comunicada
        # Salto del Boto: solo tras 5 fallos, 300 semillas, máx. 1 cada 10 niveles
        if self.fails_on_level >= e["skip_after_fails"] and self.level - self.last_skip_level >= e["skip_min_gap"] \
                and self.balance >= e["skip_price"] and rng.random() < p["p_skip"]:
            self.add_spend("salto_del_boto", e["skip_price"])
            self.skips += 1
            self.last_skip_level = self.level
            self.advance_level()

    def advance_level(self):
        old_band = band_of(self.level)
        self.level += 1
        new_band = band_of(self.level) if self.level <= LEVELS else "fin"
        if new_band != old_band:
            ph = self.phases[old_band]
            ph.days = self.day - self.day_phase_start
            ph.balance_end = self.balance
            self.day_phase_start = self.day
        self.fails_on_level = 0
        self.undos_left = self.e["undo_free_per_level"]
        self.has_extra_cuia = False
        self.relief = False

    def result(self) -> RunResult:
        if self.level <= LEVELS:  # no terminó: cerrar la fase actual
            ph = self.phases[band_of(self.level)]
            ph.days = self.day - self.day_phase_start
            ph.balance_end = self.balance
        return RunResult(
            days=self.day, final_level=min(self.level, LEVELS + 1) - 1, balance_end=self.balance,
            balance_min=self.balance_min, victories=self.victories, attempts=self.attempts,
            income=self.income, spend=self.spend, rescues_used=self.rescues_used,
            rescues_first_day=self.rescues_first_day, blocked_no_resources=self.blocked,
            first_block_level=self.first_block_level, skips=self.skips, rv_watched=self.rv_watched,
            usd_spent=self.usd_spent, purchases=self.purchases, phases=self.phases,
            income_by_band=self.income_by_band, victories_by_band=self.victories_by_band,
        )


# ---------------------------------------------------------------------------
# 5. Agregación y tablas
# ---------------------------------------------------------------------------

def mean(xs):
    return statistics.fmean(xs) if xs else 0.0


def aggregate(results: List[RunResult], econ: Dict) -> Dict:
    n = len(results)
    inc_keys = sorted({k for r in results for k in r.income})
    sp_keys = sorted({k for r in results for k in r.spend})
    free_income = [sum(v for k, v in r.income.items() if not k.startswith("iap:")) for r in results]
    victories = [r.victories for r in results]
    agg = dict(
        runs=n,
        days=mean([r.days for r in results]),
        final_level=mean([r.final_level for r in results]),
        finished_pct=100.0 * sum(1 for r in results if r.final_level >= LEVELS) / n,
        balance_end=mean([r.balance_end for r in results]),
        balance_min=mean([r.balance_min for r in results]),
        victories=mean(victories),
        attempts=mean([r.attempts for r in results]),
        income={k: mean([r.income.get(k, 0.0) for r in results]) for k in inc_keys},
        spend={k: mean([r.spend.get(k, 0.0) for r in results]) for k in sp_keys},
        free_income=mean(free_income),
        income_per_victory=mean([fi / max(1, v) for fi, v in zip(free_income, victories)]),
        rescues_used=mean([r.rescues_used for r in results]),
        rescues_first_day=mean([r.rescues_first_day for r in results]),
        blocked=mean([r.blocked_no_resources for r in results]),
        blocked_runs_pct=100.0 * sum(1 for r in results if r.blocked_no_resources > 0) / n,
        first_block_level=mean([r.first_block_level for r in results if r.first_block_level]) if any(
            r.first_block_level for r in results) else None,
        skips=mean([r.skips for r in results]),
        rv_per_day=mean([r.rv_watched / max(1, r.days) for r in results]),
        usd_spent=mean([r.usd_spent for r in results]),
        purchases={k: mean([r.purchases.get(k, 0) for r in results]) for k in
                   sorted({k for r in results for k in r.purchases})},
        phases={},
        ratio={},
    )
    # continues pagables con el ingreso gratuito (precio nominal ponderado por niveles 31-500)
    b31, b101, b201 = econ["rescue_bands"]
    nominal_w = (70 * b31 + 100 * b101 + 300 * b201) / 470
    agg["nominal_rescue_weighted"] = nominal_w
    agg["continues_payable"] = agg["free_income"] / nominal_w
    agg["continues_payable_per_day"] = (agg["free_income"] / max(1.0, agg["days"])) / nominal_w
    # fases
    for b in ("1-30", "31-100", "101-500"):
        ph = [r.phases[b] for r in results]
        d = mean([x.days for x in ph])
        agg["phases"][b] = dict(
            days=d, victories=mean([x.victories for x in ph]),
            income_day=mean([x.income / max(1, x.days) for x in ph]),
            spend_day=mean([x.spend / max(1, x.days) for x in ph]),
            net_day=mean([(x.income - x.spend) / max(1, x.days) for x in ph]),
            rescues=mean([x.rescues for x in ph]), blocked=mean([x.blocked for x in ph]),
            balance_end=mean([x.balance_end for x in ph]),
        )
    # ratio de presión por tramo: precio nominal del tramo / ingreso efectivo por victoria del tramo
    for b, price in (("31-100", b31), ("101-500", (100 * b101 + 300 * b201) / 400)):
        ipv = mean([r.income_by_band.get(b, 0.0) / max(1, r.victories_by_band.get(b, 1)) for r in results])
        agg["ratio"][b] = dict(price=price, income_per_victory=ipv, ratio=price / ipv if ipv else None)
    return agg


def fmt(x, nd=0):
    if x is None:
        return "—"
    if isinstance(x, float):
        s = f"{x:,.{nd}f}"
    else:
        s = f"{x:,}"
    return s.replace(",", "X").replace(".", ",").replace("X", ".")


def print_tables(aggs: Dict[str, Dict], econ: Dict):
    b31, b101, b201 = econ["rescue_bands"]
    print(f"\n## Escenario {econ['scenario']} — Cuia de rescate {b31}/{b101}/{b201} semillas (31-100 / 101-200 / 201+), "
          f"primer rescate del día −50 %; Crecida {'una vez por hito y día' if econ['streak_hits_once_per_day'] else 'repetible hasta el tope diario'}, "
          f"hito 6 = Canasta ({econ['chest_seeds']} + comodín), hito 10 = {econ['streak_hit10'] if econ['streak_hit10'] else 'Cuia extra + río crecido'}; "
          f"Canasta {econ['chest_seeds']} + comodín; Reto del río {econ['daily_challenge_seeds']}"
          f"{' + Morpho' if econ['daily_challenge_item'] else ''}; Cosecha doble tope {econ['rv_x2_cap']}/día\n")
    print("Escenario A = dirección literal (Crecida repetible hasta 600/día con 300 en el hito 10; rescate 600 fijo). "
          "Escenario B = canon del capítulo 5 (cada hito de la Crecida se paga una vez por día; hito 10 = Cuia extra + río crecido; rescate 600 fijo). "
          "Todo lo demás es idéntico (hito 6 = Canasta 50 + comodín, Canasta 50 + comodín cada 10 niveles, Reto del río 60, "
          "Cosecha doble 5/día, topes del capítulo 4).\n")
    names = list(aggs.keys())
    labels = [PROFILES[n]["label"] for n in names]
    print("| Métrica (media de " + str(aggs[names[0]]["runs"]) + " corridas) | " + " | ".join(labels) + " |")
    print("|---|" + "---|" * len(names))
    rows = [
        ("Días de juego hasta el nivel 500", [fmt(a["days"], 1) for a in aggs.values()]),
        ("Corridas que terminan el 500 (%)", [fmt(a["finished_pct"], 0) for a in aggs.values()]),
        ("Intentos totales", [fmt(a["attempts"], 0) for a in aggs.values()]),
        ("Victorias por día", [fmt(a["victories"] / max(1, a["days"]), 1) for a in aggs.values()]),
        ("Ingreso gratuito total (semillas)", [fmt(a["free_income"], 0) for a in aggs.values()]),
        ("Ingreso efectivo por victoria (todas las fuentes)", [fmt(a["income_per_victory"], 1) for a in aggs.values()]),
        ("Saldo final (semillas)", [fmt(a["balance_end"], 0) for a in aggs.values()]),
        ("Saldo mínimo (semillas)", [fmt(a["balance_min"], 0) for a in aggs.values()]),
        ("Cuias de rescate pagables con ingreso gratuito (total)", [fmt(a["continues_payable"], 1) for a in aggs.values()]),
        ("Cuias de rescate pagables por día", [fmt(a["continues_payable_per_day"], 2) for a in aggs.values()]),
        ("Cuias de rescate usadas", [fmt(a["rescues_used"], 1) for a in aggs.values()]),
        ("  de ellas con −50 % (primera del día)", [fmt(a["rescues_first_day"], 1) for a in aggs.values()]),
        ("Bloqueos sin recursos (quiso rescate, sin saldo)", [fmt(a["blocked"], 1) for a in aggs.values()]),
        ("Corridas con ≥1 bloqueo sin recursos (%)", [fmt(a["blocked_runs_pct"], 0) for a in aggs.values()]),
        ("Nivel del primer bloqueo sin recursos", [fmt(a["first_block_level"], 0) for a in aggs.values()]),
        ("Saltos del Boto", [fmt(a["skips"], 1) for a in aggs.values()]),
        ("Anuncios recompensados vistos por día", [fmt(a["rv_per_day"], 1) for a in aggs.values()]),
        ("USD gastados (pagador)", [fmt(a["usd_spent"], 2) for a in aggs.values()]),
    ]
    for label, vals in rows:
        print(f"| {label} | " + " | ".join(vals) + " |")

    print("\n### Ingreso por fuente (semillas, total de la corrida)\n")
    inc_keys = sorted({k for a in aggs.values() for k in a["income"]})
    print("| Fuente | " + " | ".join(labels) + " |")
    print("|---|" + "---|" * len(names))
    for k in inc_keys:
        print(f"| {k} | " + " | ".join(fmt(a["income"].get(k, 0.0), 0) for a in aggs.values()) + " |")
    print("\n### Gasto por sumidero (semillas, total de la corrida)\n")
    sp_keys = sorted({k for a in aggs.values() for k in a["spend"]})
    print("| Sumidero | " + " | ".join(labels) + " |")
    print("|---|" + "---|" * len(names))
    for k in sp_keys:
        print(f"| {k} | " + " | ".join(fmt(a["spend"].get(k, 0.0), 0) for a in aggs.values()) + " |")

    print("\n### Presupuesto por fase (semillas por día)\n")
    print("| Fase | Perfil | Días | Victorias | Ingreso/día | Gasto/día | Neto/día | Rescates | Bloqueos sin recursos | Saldo al cerrar |")
    print("|---|---|---|---|---|---|---|---|---|---|")
    for b in ("1-30", "31-100", "101-500"):
        for n, a in aggs.items():
            ph = a["phases"][b]
            print(f"| {b} | {PROFILES[n]['label']} | {fmt(ph['days'], 1)} | {fmt(ph['victories'], 0)} | "
                  f"{fmt(ph['income_day'], 0)} | {fmt(ph['spend_day'], 0)} | {fmt(ph['net_day'], 0)} | "
                  f"{fmt(ph['rescues'], 1)} | {fmt(ph['blocked'], 1)} | {fmt(ph['balance_end'], 0)} |")

    print("\n### Ratio de presión (precio nominal de la Cuia de rescate / ingreso efectivo por victoria)\n")
    print("| Tramo | Perfil | Precio nominal | Ingreso efectivo/victoria | Ratio | ¿En banda 10-15:1? |")
    print("|---|---|---|---|---|---|")
    for b in ("31-100", "101-500"):
        for n, a in aggs.items():
            r = a["ratio"][b]
            ok = "sí" if r["ratio"] and 10 <= r["ratio"] <= 15 else ("bajo" if r["ratio"] and r["ratio"] < 10 else "alto")
            print(f"| {b} | {PROFILES[n]['label']} | {fmt(r['price'], 0)} | {fmt(r['income_per_victory'], 1)} | "
                  f"{fmt(r['ratio'], 1)}:1 | {ok} |")
    # referencia: media de los perfiles no pagadores
    non_payers = [n for n in aggs if not PROFILES[n].get("payer")]
    for b in ("31-100", "101-500"):
        rs = [aggs[n]["ratio"][b]["ratio"] for n in non_payers]
        if rs:
            m = mean(rs)
            print(f"| {b} | **Referencia (media de {len(rs)} no pagadores)** | {fmt(aggs[non_payers[0]]['ratio'][b]['price'], 0)} | — | "
                  f"**{fmt(m, 1)}:1** | {'sí' if 10 <= m <= 15 else ('bajo' if m < 10 else 'alto')} |")


# ---------------------------------------------------------------------------
# 6. Ingresos por 1.000 DAU (modelo explícito; cifras [V]/[E] de research/04 §1.4 y §Estimación)
# ---------------------------------------------------------------------------

# eCPM Android por región (USD/1.000): Appodeal Q4-2024 [V research/04 §1.4]; LatAm intersticial 1,3-1,9 → 1,6;
# "Resto" [E]. Fila "US-heavy objetivo": eCPM US de Tenjin×CAS Q2-24 (rewarded 30, intersticial 16) [V] ponderado.
ECPM = {
    "LatAm": dict(int=1.6, rew=1.8),
    "US": dict(int=9.6, rew=9.0),
    "EU": dict(int=3.6, rew=5.1),
    "Resto": dict(int=1.0, rew=1.2),
    "US_tenjin": dict(int=16.15, rew=30.25),
}

REVENUE_SCENARIOS = {
    "LatAm-heavy (soft launch CO/PE/MX → BR)": dict(
        mix={"LatAm": 0.80, "US": 0.15, "Resto": 0.05},
        int_per_dau=2.4, rew_per_dau=1.5, payer_rate=0.0018, arppu=2.6, store_fee=0.15),
    "US-heavy base (eCPM Appodeal Android)": dict(
        mix={"US": 0.70, "EU": 0.20, "Resto": 0.10},
        int_per_dau=2.4, rew_per_dau=1.5, payer_rate=0.0040, arppu=5.5, store_fee=0.15),
    "US-heavy objetivo (eCPM Tenjin US + rescate 25 % del IAP)": dict(
        mix={"US_tenjin": 0.70, "EU": 0.20, "Resto": 0.10},
        int_per_dau=2.4, rew_per_dau=1.6, payer_rate=0.0055, arppu=6.5, store_fee=0.15),
}


def revenue_per_1000_dau(sc: Dict) -> Dict:
    ecpm_int = sum(w * ECPM[g]["int"] for g, w in sc["mix"].items())
    ecpm_rew = sum(w * ECPM[g]["rew"] for g, w in sc["mix"].items())
    ads = (sc["int_per_dau"] * ecpm_int + sc["rew_per_dau"] * ecpm_rew)  # por 1.000 DAU → USD/día
    iap_gross = 1000 * sc["payer_rate"] * sc["arppu"]
    iap_net = iap_gross * (1 - sc["store_fee"])
    total = ads + iap_net
    return dict(ecpm_int=ecpm_int, ecpm_rew=ecpm_rew, ads=ads, iap_gross=iap_gross, iap_net=iap_net,
                total=total, arpdau=total / 1000, ads_share=ads / total)


def print_revenue():
    print("\n## Ingresos por 1.000 DAU (USD/día; anuncios netos de red, IAP neto de 15 % de tienda)\n")
    print("| Escenario | eCPM int. blend | eCPM rew. blend | Int./DAU | Rew./DAU | Ads USD/día | Pagadores/día (%) | ARPPU | IAP bruto → neto | Total neto/día | ARPDAU | % ads |")
    print("|---|---|---|---|---|---|---|---|---|---|---|---|")
    for name, sc in REVENUE_SCENARIOS.items():
        r = revenue_per_1000_dau(sc)
        print(f"| {name} | {fmt(r['ecpm_int'], 2)} | {fmt(r['ecpm_rew'], 2)} | {sc['int_per_dau']} | {sc['rew_per_dau']} | "
              f"{fmt(r['ads'], 1)} | {fmt(sc['payer_rate'] * 100, 2)} | {fmt(sc['arppu'], 2)} | "
              f"{fmt(r['iap_gross'], 1)} → {fmt(r['iap_net'], 1)} | **{fmt(r['total'], 1)}** | **{fmt(r['arpdau'], 3)}** | {fmt(r['ads_share'] * 100, 0)} |")


# ---------------------------------------------------------------------------
# 7. Main
# ---------------------------------------------------------------------------

def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--scenario", default="B", choices=["A", "B", "a", "b"], help="A = dirección literal; B = ajustada")
    ap.add_argument("--runs", type=int, default=300, help="corridas Monte Carlo por perfil")
    ap.add_argument("--seed", type=int, default=2026)
    ap.add_argument("--rescue", type=str, default=None, help="precio por tramo 31-100,101-200,201+ (p. ej. 600,750,900)")
    ap.add_argument("--hit10", type=int, default=None, help="semillas del hito 10 de la Crecida (A: 300; B: 0 = Cuia extra + río crecido)")
    ap.add_argument("--x2cap", type=int, default=None, help="tope diario de Cosecha doble (A: 5; B: 3)")
    ap.add_argument("--chest", type=int, default=None, help="semillas de la Canasta desde el nivel 20 (defecto 50)")
    ap.add_argument("--daily", type=int, default=None, help="semillas del Reto del río (defecto 60)")
    ap.add_argument("--json", type=str, default=None, help="ruta para volcar los agregados en JSON")
    ap.add_argument("--no-revenue", action="store_true")
    args = ap.parse_args()

    bands =tuple(int(x) for x in args.rescue.split(",")) if args.rescue else None
    econ = make_econ(args.scenario, bands, args.hit10, args.x2cap)
    if args.chest is not None:
        econ["chest_seeds"] = args.chest
    if args.daily is not None:
        econ["daily_challenge_seeds"] = args.daily
    aggs = {}
    for name, prof in PROFILES.items():
        results = []
        for i in range(args.runs):
            rng = random.Random(args.seed * 1000 + i)
            results.append(Sim(prof, econ, rng).run())
        aggs[name] = aggregate(results, econ)
    print(f"# econ_sim — Amazon Fill / Tintalú — {LEVELS} niveles, {args.runs} corridas por perfil, semilla {args.seed}")
    print_tables(aggs, econ)
    if not args.no_revenue:
        print_revenue()
    if args.json:
        with open(args.json, "w", encoding="utf-8") as f:
            json.dump(dict(econ={k: (list(v) if isinstance(v, tuple) else v) for k, v in econ.items()},
                           profiles=aggs), f, ensure_ascii=False, indent=2, default=str)
        print(f"\nJSON escrito en {args.json}")


if __name__ == "__main__":
    main()
