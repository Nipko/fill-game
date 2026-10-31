#!/usr/bin/env python3
"""Rellena los huecos de los lechos de música de Tintalú (observación de la prueba de la 10001, 2026-10-31).

Los 18 lechos de la entrega 1 (music/wNN_bed.wav y wNN_bed_night.wav) se montaron en tramos y cada unión, a 0,2, 0,5 y 0,8
del bucle, lleva un fundido a silencio: hasta 270 ms a −65/−80 dBFS en medio de un pad a −25 dBFS. En el teléfono se oye
como un tropiezo de la música, con el agua del ambiente debajo. Esta herramienta los encuentra con el mismo detector que
check_audio.py (dropouts: allí, 15 dB o más por debajo de la mediana de ±1 s durante 30 ms o más; aquí, para tapar
también los bajones menos hondos de las mismas uniones, 8 dB) y los tapa: el tramo hundido se
sustituye por la música que suena justo antes (primera mitad) y justo después (segunda mitad), desplazada lo que mide el
hueco y llevada al nivel de la música que lo rodea, con fundidos cruzados de potencia constante de 40 ms en los bordes y en
el centro. El resto de muestras no cambia, ni
el largo (las capas siguen midiendo lo mismo que su lecho), ni el formato.

Solo biblioteca estándar (Python 3.14).

Uso:
  py -3.14 tools/audio-check/fill_dropouts.py <wav>...            reescribe cada archivo que tenga huecos
  py -3.14 tools/audio-check/fill_dropouts.py --check <wav>...    no escribe; código 1 si alguno tiene huecos
  py -3.14 tools/audio-check/fill_dropouts.py --self-test

Código de salida: 0 bien; 1 quedan huecos (o la autoprueba falla); 2 argumentos.
"""
import math
import os
import struct
import sys
import tempfile
import wave

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from check_audio import DROPOUT_WIN_MS, context_levels, dropouts, window_levels, DROPOUT_CONTEXT_S  # noqa: E402

REPAIR_DEPTH_DB = 8.0  # al rellenar se buscan también los bajones menos hondos de las mismas uniones (8 dB, 30 ms)
EDGE_DB = 3.0       # el tramo que se sustituye sigue mientras el nivel esté 3 dB o más por debajo de su entorno
MERGE_MS = 100      # huecos a menos de 100 ms se tapan juntos
XFADE_S = 0.04      # fundidos cruzados de 40 ms


def read16(path):
    with wave.open(path, 'rb') as w:
        params = w.getparams()
        raw = w.readframes(w.getnframes())
    if params.sampwidth != 2 or params.nchannels != 1:
        raise ValueError('%s: se espera WAV PCM mono de 16 bits (es de %d canales y %d bits)' % (path, params.nchannels, params.sampwidth * 8))
    vals = struct.unpack('<%dh' % (len(raw) // 2), raw)
    return params, [v / 32768.0 for v in vals]


def write16(path, params, x):
    out = bytearray()
    for v in x:
        out += struct.pack('<h', max(-32768, min(32767, int(round(v * 32768.0)))))
    with wave.open(path, 'wb') as w:
        w.setparams(params)
        w.writeframes(bytes(out))


def regions(x, sr, depth_db=REPAIR_DEPTH_DB):
    """Tramos [a, b) en muestras que hay que sustituir: cada hueco, ampliado mientras siga EDGE_DB dB por debajo."""
    holes = dropouts(x, sr, depth_db=depth_db)
    if not holes:
        return []
    r = window_levels(x, sr)
    ctx = context_levels(r, int(DROPOUT_CONTEXT_S * 1000 / DROPOUT_WIN_MS))
    w = int(sr * DROPOUT_WIN_MS / 1000)
    spans = []
    for _, _, _, i0, i1 in holes:
        a, b = i0, i1
        while a > 0 and ctx[a - 1] - r[a - 1] >= EDGE_DB:
            a -= 1
        while b < len(r) and ctx[b] - r[b] >= EDGE_DB:
            b += 1
        spans.append([a * w, b * w])
    spans.sort()
    merged = [spans[0]]
    gap = int(sr * MERGE_MS / 1000)
    for a, b in spans[1:]:
        if a - merged[-1][1] <= gap:
            merged[-1][1] = max(merged[-1][1], b)
        else:
            merged.append([a, b])
    return [(a, b) for a, b in merged]


def conceal(x, sr, a, b):
    """Sustituye [a, b) por la música de antes y de después, desplazada, con fundidos de potencia constante."""
    n = len(x)
    xf = int(XFADE_S * sr)
    shift = (b - a) + 2 * xf  # la fuente queda entera fuera del tramo hundido
    mid = (a + b) // 2
    src = list(x)
    ref = int(0.2 * sr)

    def rms(lo, hi):
        acc = 0.0
        for t in range(lo, hi):
            v = src[t % n]
            acc += v * v
        return math.sqrt(acc / max(1, hi - lo))

    def match(target, got):
        # Cada mitad, al nivel de la música que la rodea (sin subidas ni bajones nuevos); como mucho ±6 dB.
        return 1.0 if got <= 0.0 else max(0.5, min(2.0, target / got))

    g_pre = match(rms(a - xf - ref, a - xf), rms(a - xf - shift, mid + xf - shift))
    g_post = match(rms(b + xf, b + xf + ref), rms(mid - xf + shift, b + xf + shift))

    def donor(t):
        pre = src[(t - shift) % n] * g_pre
        post = src[(t + shift) % n] * g_post
        if t < mid - xf // 2:
            return pre
        if t >= mid + xf // 2:
            return post
        th = (t - (mid - xf // 2)) / max(1, xf) * math.pi * 0.5
        return pre * math.cos(th) + post * math.sin(th)

    for t in range(a - xf, b + xf):
        i = t % n
        if t < a:
            th = (t - (a - xf)) / xf * math.pi * 0.5
            x[i] = src[i] * math.cos(th) + donor(t) * math.sin(th)
        elif t < b:
            x[i] = donor(t)
        else:
            th = (t - b) / xf * math.pi * 0.5
            x[i] = donor(t) * math.cos(th) + src[i] * math.sin(th)


def fix(path, write=True, out=sys.stdout, depth_db=REPAIR_DEPTH_DB):
    params, x = read16(path)
    sr = params.framerate
    spans = regions(x, sr, depth_db)
    if not spans:
        print('%s: sin huecos' % path, file=out)
        return 0
    for a, b in spans:
        conceal(x, sr, a, b)
    left = dropouts(x, sr, depth_db=depth_db)
    desc = ', '.join('%.2f-%.2f s' % (a / sr, b / sr) for a, b in spans)
    if write and not left:
        write16(path, params, x)
    print('%s: %d hueco(s) (%s) %s%s' % (path, len(spans), desc, 'rellenados' if write else 'por rellenar',
                                          '' if not left else '; QUEDAN %d tras rellenar' % len(left)), file=out)
    return 1 if (left or not write) else 0


def self_test():
    sr = 24000
    n = 4 * sr

    def pad():
        return [0.08 * (math.sin(2 * math.pi * 220 * i / sr) + 0.6 * math.sin(2 * math.pi * 330 * i / sr)) for i in range(n)]

    ok, gap = pad(), pad()
    a, f, h = int(2.0 * sr), int(0.04 * sr), int(0.25 * sr)
    for i in range(a - f, a + h + f):
        gap[i] *= 0.0 if a <= i < a + h else ((a - i) / f if i < a else (i - a - h) / f)
    bad = []
    with tempfile.TemporaryDirectory() as d:
        params = (1, 2, sr, n, 'NONE', 'not compressed')
        p_ok, p_gap = os.path.join(d, 'ok.wav'), os.path.join(d, 'gap.wav')
        for p, x in ((p_ok, ok), (p_gap, gap)):
            with wave.open(p, 'wb') as w:
                w.setparams(params)
                w.writeframes(b''.join(struct.pack('<h', int(round(v * 32767))) for v in x))
        _, before_ok = read16(p_ok)
        if fix(p_ok) != 0 or read16(p_ok)[1] != before_ok:
            bad.append('sin huecos no debe cambiar nada')
        if len(dropouts(read16(p_gap)[1], sr)) != 1:
            bad.append('el hueco de 250 ms se detecta')
        if fix(p_gap, write=False) != 1:
            bad.append('--check da 1 con un hueco')
        if fix(p_gap) != 0:
            bad.append('se rellena')
        _, fixed = read16(p_gap)
        if dropouts(fixed, sr):
            bad.append('no quedan huecos')
        if len(fixed) != n:
            bad.append('mismo largo')
        if fixed[:a - 2 * f] != read16(p_ok)[1][:a - 2 * f]:
            bad.append('lo de antes del hueco no cambia')
        peak = max(abs(v) for v in fixed)
        if peak > 0.2:
            bad.append('sin picos nuevos (%.3f)' % peak)
    print('Autoprueba: %s' % ('todo PASS' if not bad else 'FALLA: ' + '; '.join(bad)))
    return 1 if bad else 0


def main():
    args = sys.argv[1:]
    if args == ['--self-test']:
        return self_test()
    check = False
    if args and args[0] == '--check':
        check, args = True, args[1:]
    if not args:
        print(__doc__)
        return 2
    code = 0
    for p in args:
        code = max(code, fix(p, write=not check))
    return code


if __name__ == '__main__':
    sys.exit(main())
