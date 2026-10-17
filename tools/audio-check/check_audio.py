#!/usr/bin/env python3
"""Comprueba una entrega de audio de Tintalú contra docs/audio/especificacion-sonidos.json.

Solo biblioteca estándar (Python 3.14). Mide cada WAV que pide la especificación y lo compara con sus números:
formato (48 kHz, 24 bits, mono si lo pide), duración, pico, desplazamiento de continua, silencio inicial, final con
fundido, energía por encima de 300 Hz (lo que reproduce el altavoz de un teléfono), costura de los bucles, sonoridad
(LUFS integrados según ITU-R BS.1770, con su filtro K y las compuertas), afinación de las notas de fruta, longitud
idéntica de las capas de música, pulso marcado en los lechos y, en los fondos, zumbidos tonales y siseo agudo.
También comprueba el manifiesto de licencias (audio_manifest.json).

Uso:
  py -3.14 tools/audio-check/check_audio.py --root Assets/_Project/Audio
  py -3.14 tools/audio-check/check_audio.py --root <carpeta> --only sfx --allow-missing
  py -3.14 tools/audio-check/check_audio.py --self-test

Código de salida: 0 si no hay ningún FALLO (los AVISOS no cuentan); 1 si hay alguno; 2 si faltan argumentos.
"""
import argparse
import cmath
import json
import math
import os
import struct
import sys
import tempfile
import wave

SPEC_DEFAULT = os.path.join(os.path.dirname(__file__), '..', '..', 'docs', 'audio', 'especificacion-sonidos.json')
ALLOWED_LICENSES = {'propia', 'cc0', 'comercial-con-cesion', 'ia-uso-comercial'}


# ---------------------------------------------------------------- lectura

def read_wav(path):
    """Devuelve (sample_rate, bits, canales, muestras por canal como listas de float en [-1, 1])."""
    with wave.open(path, 'rb') as w:
        ch, sw, sr, n = w.getnchannels(), w.getsampwidth(), w.getframerate(), w.getnframes()
        raw = w.readframes(n)
    total = n * ch
    if sw == 2:
        vals = struct.unpack('<%dh' % total, raw)
        scale = 32768.0
    elif sw == 3:
        vals = [int.from_bytes(raw[i:i + 3], 'little', signed=True) for i in range(0, len(raw), 3)]
        scale = 8388608.0
    elif sw == 4:
        vals = struct.unpack('<%di' % total, raw)
        scale = 2147483648.0
    else:
        raise ValueError('ancho de muestra no admitido: %d bytes' % sw)
    chans = [[vals[i] / scale for i in range(c, total, ch)] for c in range(ch)]
    return sr, sw * 8, ch, chans


def mono(chans):
    if len(chans) == 1:
        return chans[0]
    k = 1.0 / len(chans)
    return [sum(s) * k for s in zip(*chans)]


def db(x):
    return 20.0 * math.log10(max(x, 1e-12))


# ---------------------------------------------------------------- medidas

def fft(a):
    n = len(a)
    if n == 1:
        return a
    even, odd = fft(a[0::2]), fft(a[1::2])
    out = [0j] * n
    for k in range(n // 2):
        t = cmath.exp(-2j * math.pi * k / n) * odd[k]
        out[k] = even[k] + t
        out[k + n // 2] = even[k] - t
    return out


def spectrum(x, sr, max_n=16384, start=None):
    """Espectro de potencia (frecuencias, potencias) de una ventana de Hann de hasta max_n muestras."""
    n = 1
    while n * 2 <= min(len(x), max_n):
        n *= 2
    if n < 256:
        n = 256
    if start is None:
        start = 0
    seg = x[start:start + n] + [0.0] * max(0, start + n - len(x))
    w = [seg[i] * (0.5 - 0.5 * math.cos(2 * math.pi * i / (n - 1))) for i in range(n)]
    f = fft([complex(v) for v in w])
    half = n // 2
    return [k * sr / n for k in range(half)], [abs(f[k]) ** 2 for k in range(half)]


def loudest_window_start(x, sr, n):
    hop = max(1, n // 4)
    best, best_e = 0, -1.0
    for s in range(0, max(1, len(x) - n), hop):
        e = sum(v * v for v in x[s:s + n:8])
        if e > best_e:
            best, best_e = s, e
    return best


def energy_above(x, sr, hz=300.0):
    n = 16384
    start = loudest_window_start(x, sr, n) if len(x) > n else 0
    freqs, p = spectrum(x, sr, n, start)
    tot = sum(p[1:]) or 1e-30
    return sum(pp for f, pp in zip(freqs, p) if f >= hz) / tot


def biquad(x, b, a):
    b0, b1, b2 = b
    _, a1, a2 = a
    y = [0.0] * len(x)
    x1 = x2 = y1 = y2 = 0.0
    for i, v in enumerate(x):
        o = b0 * v + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        y[i] = o
        x2, x1, y2, y1 = x1, v, y1, o
    return y


# Filtro K de ITU-R BS.1770-4 a 48 kHz.
K1 = ((1.53512485958697, -2.69169618940638, 1.19839281085285), (1.0, -1.69065929318241, 0.73248077421585))
K2 = ((1.0, -2.0, 1.0), (1.0, -1.99004745483398, 0.99007225036621))


def lufs(chans, sr):
    """Sonoridad integrada (LUFS) con las compuertas absoluta (−70) y relativa (−10). Exacta a 48 kHz."""
    kch = [biquad(biquad(c, *K1), *K2) for c in chans]
    blk, hop = int(0.4 * sr), int(0.1 * sr)
    zs = []
    for s in range(0, max(1, len(kch[0]) - blk + 1), hop):
        z = sum(sum(v * v for v in c[s:s + blk]) / blk for c in kch)
        zs.append(z)
    def l(z):
        return -0.691 + 10 * math.log10(max(z, 1e-20))
    g1 = [z for z in zs if l(z) > -70]
    if not g1:
        return -99.0
    rel = l(sum(g1) / len(g1)) - 10
    g2 = [z for z in g1 if l(z) > rel]
    return l(sum(g2) / len(g2)) if g2 else -99.0


def f0_cents(x, sr, target):
    """Desvío (centésimas) del pico espectral más fuerte a ±1 semitono de target."""
    n = 1
    while n * 2 <= min(len(x), 32768):
        n *= 2
    freqs, p = spectrum(x, sr, n, 0)
    lo, hi = target / 2 ** (1 / 12), target * 2 ** (1 / 12)
    best = max(((pp, i) for i, (f, pp) in enumerate(zip(freqs, p)) if lo <= f <= hi), default=None)
    if not best:
        return None
    i = best[1]
    if 0 < i < len(p) - 1:  # interpolación parabólica
        a, b, c = math.log(p[i - 1] + 1e-30), math.log(p[i] + 1e-30), math.log(p[i + 1] + 1e-30)
        den = a - 2 * b + c
        i = i + (0.5 * (a - c) / den if den else 0)
    f = i * sr / n
    return 1200 * math.log2(f / target)


def envelope(x, sr, frame_s=0.05):
    n = max(1, int(frame_s * sr))
    return [math.sqrt(sum(v * v for v in x[s:s + n]) / n) for s in range(0, len(x) - n + 1, n)]


def pulse_strength(x, sr):
    """Autocorrelación del envolvente entre 0,3 y 2 s: cerca de 1 = pulso marcado (golpes regulares)."""
    e = envelope(x, sr)
    if len(e) < 60:
        return 0.0
    m = sum(e) / len(e)
    d = [v - m for v in e]
    var = sum(v * v for v in d) or 1e-30
    best = 0.0
    for lag in range(6, 41):  # 0,30-2,00 s en marcos de 50 ms
        r = sum(d[i] * d[i + lag] for i in range(len(d) - lag)) / var
        best = max(best, r)
    return best


def tonal_peaks(x, sr):
    """Picos tonales sostenidos (zumbido) en 80-4000 Hz: dB por encima de la mediana de su entorno de 1/3 de octava."""
    n = 16384
    worst = (0.0, 0.0)
    for start in (len(x) // 5, len(x) // 2, (4 * len(x)) // 5):
        freqs, p = spectrum(x, sr, n, max(0, min(start, len(x) - n)))
        pdb = [10 * math.log10(v + 1e-30) for v in p]
        df = freqs[1] - freqs[0]
        for i, f in enumerate(freqs):
            if f < 80 or f > 4000:
                continue
            lo_i, hi_i = int(f / 2 ** (1 / 6) / df), int(f * 2 ** (1 / 6) / df) + 1
            nb = sorted(pdb[max(0, lo_i):min(len(pdb), hi_i + 1)])
            if len(nb) < 5:
                continue
            excess = pdb[i] - nb[len(nb) // 2]
            if excess > worst[0]:
                worst = (excess, f)
    return worst


def hiss_fraction(x, sr):
    freqs, p = spectrum(x, sr, 16384, len(x) // 2 if len(x) > 32768 else 0)
    tot = sum(p[1:]) or 1e-30
    return sum(pp for f, pp in zip(freqs, p) if f >= 8000) / tot


# ---------------------------------------------------------------- comprobación

class Report:
    def __init__(self):
        self.rows = []
        self.fail = 0
        self.warn = 0

    def add(self, sid, level, msg):
        self.rows.append((sid, level, msg))
        if level == 'FALLO':
            self.fail += 1
        elif level == 'AVISO':
            self.warn += 1


def check_entry(e, root, spec, rep, lengths):
    g = spec['global']
    fmt = spec['format']
    path = os.path.join(root, e['file'])
    if not os.path.exists(path):
        return False
    try:
        sr, bits, ch, chans = read_wav(path)
    except Exception as ex:  # noqa: BLE001
        rep.add(e['id'], 'FALLO', 'no se puede leer como WAV PCM: %s' % ex)
        return True
    x = mono(chans)
    dur = len(x) / sr
    lengths[e['id']] = len(x)
    if sr != fmt['sample_rate']:
        rep.add(e['id'], 'FALLO', 'frecuencia de muestreo %d Hz (se pide %d)' % (sr, fmt['sample_rate']))
    if bits != fmt['bit_depth']:
        rep.add(e['id'], 'FALLO', '%d bits (se piden %d)' % (bits, fmt['bit_depth']))
    if e.get('mono') and ch != 1:
        rep.add(e['id'], 'FALLO', '%d canales (se pide mono)' % ch)
    if ch > 2:
        rep.add(e['id'], 'FALLO', '%d canales (como mucho estéreo)' % ch)
    peak = max(abs(v) for c in chans for v in c) if x else 0.0
    if db(peak) > g['true_peak_max_dbfs']:
        rep.add(e['id'], 'FALLO', 'pico %.1f dBFS por encima del máximo %.1f' % (db(peak), g['true_peak_max_dbfs']))
    if 'peak_dbfs' in e and abs(db(peak) - e['peak_dbfs']) > e['peak_tolerance_db']:
        rep.add(e['id'], 'FALLO', 'pico %.1f dBFS (se pide %.1f ± %.1f)' % (db(peak), e['peak_dbfs'], e['peak_tolerance_db']))
    dc = abs(sum(x) / len(x)) if x else 0.0
    if dc > g['dc_offset_max']:
        rep.add(e['id'], 'FALLO', 'desplazamiento de continua %.4f (máximo %.4f)' % (dc, g['dc_offset_max']))
    if e.get('max_total_s') and dur > e['max_total_s'] + 1e-6:
        rep.add(e['id'], 'FALLO', 'dura %.3f s (máximo %.3f s)' % (dur, e['max_total_s']))
    if 'min_s' in e and dur < e['min_s']:
        rep.add(e['id'], 'FALLO', 'dura %.2f s (mínimo %.2f s)' % (dur, e['min_s']))
    if 'max_s' in e and dur > e['max_s']:
        rep.add(e['id'], 'FALLO', 'dura %.2f s (máximo %.2f s)' % (dur, e['max_s']))
    if not e.get('loop'):
        thr = peak * 10 ** (-40 / 20)
        first = next((i for i, v in enumerate(x) if abs(v) > thr), len(x))
        if first / sr > g['leading_silence_max_s']:
            rep.add(e['id'], 'FALLO', 'silencio inicial de %.0f ms (máximo %.0f ms)' % (first / sr * 1000, g['leading_silence_max_s'] * 1000))
        tail = x[-max(1, int(0.005 * sr)):]
        if tail and db(max(abs(v) for v in tail)) > g['fade_out_floor_db']:
            rep.add(e['id'], 'FALLO', 'no termina en silencio: los últimos 5 ms llegan a %.0f dBFS (máximo %d); falta el fundido final'
                    % (db(max(abs(v) for v in tail)), g['fade_out_floor_db']))
    else:
        for c in chans:
            jump = abs(c[-1] - c[0])
            if jump > g['loop_seam_max_jump']:
                rep.add(e['id'], 'FALLO', 'costura del bucle: salto de %.3f entre la última y la primera muestra (máximo %.3f)' % (jump, g['loop_seam_max_jump']))
                break
        n50 = int(0.05 * sr)
        if len(x) > 4 * n50:
            a = math.sqrt(sum(v * v for v in x[:n50]) / n50)
            b = math.sqrt(sum(v * v for v in x[-n50:]) / n50)
            if a > 1e-5 and b > 1e-5 and abs(db(a) - db(b)) > 3.0:
                rep.add(e['id'], 'AVISO', 'el nivel cambia %.1f dB en la costura del bucle (puede oírse un salto)' % abs(db(a) - db(b)))
    if 'min_energy_above_300hz' in e and x:
        frac = energy_above(x, sr)
        if frac < e['min_energy_above_300hz']:
            rep.add(e['id'], 'FALLO', 'solo el %.0f %% de la energía está por encima de 300 Hz (mínimo %.0f %%): en el altavoz del teléfono apenas se oirá'
                    % (frac * 100, e['min_energy_above_300hz'] * 100))
    if 'lufs' in e:
        if sr == 48000:
            lv = lufs(chans, sr)
            if abs(lv - e['lufs']) > e['lufs_tolerance']:
                rep.add(e['id'], 'FALLO', 'sonoridad %.1f LUFS (se pide %.1f ± %.1f)' % (lv, e['lufs'], e['lufs_tolerance']))
        else:
            rep.add(e['id'], 'AVISO', 'sonoridad sin medir: el filtro K de esta herramienta es el de 48 kHz')
    if 'f0_hz' in e and x:
        c = f0_cents(x, sr, e['f0_hz'])
        if c is None or abs(c) > e['f0_tolerance_cents']:
            rep.add(e['id'], 'FALLO', 'afinación: %s (se pide %s = %.2f Hz ± %d centésimas)'
                    % ('sin pico cerca de la nota' if c is None else '%+.0f centésimas' % c, e['note'], e['f0_hz'], e['f0_tolerance_cents']))
    if e.get('no_pulse') and x:
        ps = pulse_strength(x, sr)
        if ps > 0.6:
            rep.add(e['id'], 'FALLO', 'pulso marcado en el lecho (autocorrelación %.2f; máximo 0,60): el lecho no puede llevar golpes regulares' % ps)
    if e.get('clean_bed') and x:
        exc, f = tonal_peaks(x, sr)
        if exc > 15:
            rep.add(e['id'], 'FALLO', 'zumbido tonal sostenido a %.0f Hz (%.0f dB sobre su entorno; máximo 15): suena a dron' % (f, exc))
        elif exc > 10:
            rep.add(e['id'], 'AVISO', 'pico tonal a %.0f Hz (%.0f dB sobre su entorno): comprobar que no suene a zumbido' % (f, exc))
        hf = hiss_fraction(x, sr)
        if hf > 0.15:
            rep.add(e['id'], 'FALLO', 'siseo: el %.0f %% de la energía está por encima de 8 kHz (máximo 15 %%)' % (hf * 100))
    return True


def check_manifest(root, entries, rep, present):
    path = os.path.join(root, 'audio_manifest.json')
    if not os.path.exists(path):
        if present:
            rep.add('audio_manifest.json', 'FALLO', 'falta el manifiesto de licencias')
        return
    try:
        man = json.load(open(path, encoding='utf-8'))
    except Exception as ex:  # noqa: BLE001
        rep.add('audio_manifest.json', 'FALLO', 'no es JSON válido: %s' % ex)
        return
    by = {m.get('id'): m for m in man.get('files', [])}
    for e in entries:
        if e['id'] not in present:
            continue
        m = by.get(e['id'])
        if not m:
            rep.add(e['id'], 'FALLO', 'sin fila en audio_manifest.json')
            continue
        lic = (m.get('license') or '').lower()
        if lic not in ALLOWED_LICENSES:
            rep.add(e['id'], 'FALLO', 'licencia «%s» no admitida (admitidas: %s)' % (m.get('license'), ', '.join(sorted(ALLOWED_LICENSES))))
        for k in ('source', 'author'):
            if not m.get(k):
                rep.add(e['id'], 'FALLO', 'el manifiesto no dice %s' % k)


def run(root, spec_path, only, allow_missing, out=sys.stdout):
    spec = json.load(open(spec_path, encoding='utf-8'))
    groups = [('sfx', spec['sfx']), ('fruit_notes', spec['fruit_notes']), ('ambient', spec['ambient']), ('music', spec['music'])]
    rep = Report()
    lengths, present, entries = {}, set(), []
    for name, items in groups:
        if only and name not in only:
            continue
        for e in items:
            entries.append(e)
            if check_entry(e, root, spec, rep, lengths):
                present.add(e['id'])
            elif not allow_missing:
                rep.add(e['id'], 'FALLO', 'falta el archivo %s' % e['file'])
    for e in entries:
        ref = e.get('same_length_as')
        if ref and e['id'] in lengths and ref in lengths and lengths[e['id']] != lengths[ref]:
            rep.add(e['id'], 'FALLO', 'dura %d muestras y %s %d: las capas deben medir lo mismo, muestra a muestra' % (lengths[e['id']], ref, lengths[ref]))
    check_manifest(root, entries, rep, present)
    ok = len(present) - len({r[0] for r in rep.rows if r[1] == 'FALLO'} & present)
    for sid, level, msg in rep.rows:
        print('%-6s %-28s %s' % (level, sid, msg), file=out)
    print('Comprobados %d de %d archivos: %d sin fallos; %d fallos y %d avisos en total.'
          % (len(present), len(entries), ok, rep.fail, rep.warn), file=out)
    return 1 if rep.fail else 0


# ---------------------------------------------------------------- autoprueba

def write_wav(path, chans, sr=48000, bits=24):
    n = len(chans[0])
    with wave.open(path, 'wb') as w:
        w.setnchannels(len(chans))
        w.setsampwidth(bits // 8)
        w.setframerate(sr)
        frames = bytearray()
        for i in range(n):
            for c in chans:
                v = max(-1.0, min(1.0, c[i]))
                if bits == 24:
                    frames += int(round(v * 8388607)).to_bytes(3, 'little', signed=True)
                else:
                    frames += struct.pack('<h', int(round(v * 32767)))
        w.writeframes(bytes(frames))


def tone(f, dur, peak_db, sr=48000, fade=0.004, harmonics=(1.0, 0.5, 0.3)):
    n = int(dur * sr)
    a = 10 ** (peak_db / 20)
    x = [sum(h * math.sin(2 * math.pi * f * (k + 1) * i / sr) for k, h in enumerate(harmonics)) for i in range(n)]
    for i in range(n):
        env = min(1.0, i / (fade * sr)) * min(1.0, (n - 1 - i) / (0.02 * sr)) * math.exp(-3 * i / n)
        x[i] *= env
    m = max(abs(v) for v in x) or 1.0
    return [v * a / m for v in x]


def self_test():
    spec = {'version': 1, 'format': {'sample_rate': 48000, 'bit_depth': 24},
            'global': {'true_peak_max_dbfs': -1.0, 'dc_offset_max': 0.002, 'leading_silence_max_s': 0.010,
                       'fade_out_floor_db': -60, 'loop_seam_max_jump': 0.02},
            'sfx': [
                {'id': 'ok_tap', 'file': 'sfx/ok_tap.wav', 'peak_dbfs': -30, 'peak_tolerance_db': 1.5, 'max_total_s': 0.2, 'min_energy_above_300hz': 0.5, 'mono': True},
                {'id': 'loud_tap', 'file': 'sfx/loud_tap.wav', 'peak_dbfs': -30, 'peak_tolerance_db': 1.5, 'max_total_s': 0.2, 'mono': True},
                {'id': 'bass_tap', 'file': 'sfx/bass_tap.wav', 'peak_dbfs': -20, 'peak_tolerance_db': 1.5, 'min_energy_above_300hz': 0.5, 'max_total_s': 0.5, 'mono': True},
                {'id': 'cut_tail', 'file': 'sfx/cut_tail.wav', 'peak_dbfs': -20, 'peak_tolerance_db': 1.5, 'max_total_s': 0.5, 'mono': True},
                {'id': 'wrong_fmt', 'file': 'sfx/wrong_fmt.wav', 'max_total_s': 0.5, 'mono': True},
            ],
            'fruit_notes': [
                {'id': 'note_ok', 'file': 'sfx/fruit/note_ok.wav', 'note': 'A4', 'f0_hz': 440.0, 'f0_tolerance_cents': 15, 'max_total_s': 0.2, 'mono': True},
                {'id': 'note_off', 'file': 'sfx/fruit/note_off.wav', 'note': 'A4', 'f0_hz': 440.0, 'f0_tolerance_cents': 15, 'max_total_s': 0.2, 'mono': True},
            ],
            'ambient': [
                {'id': 'bad_seam', 'file': 'amb/bad_seam.wav', 'loop': True, 'min_s': 1, 'max_s': 5},
                {'id': 'drone', 'file': 'amb/drone.wav', 'loop': True, 'min_s': 1, 'max_s': 5, 'clean_bed': True},
            ],
            'music': [
                {'id': 'bed', 'file': 'music/bed.wav', 'loop': True, 'min_s': 1, 'max_s': 10, 'no_pulse': True},
                {'id': 'perc', 'file': 'music/perc.wav', 'loop': True, 'same_length_as': 'bed'},
            ]}
    expect = {'ok_tap': False, 'loud_tap': True, 'bass_tap': True, 'cut_tail': True, 'wrong_fmt': True, 'note_ok': False,
              'note_off': True, 'bad_seam': True, 'drone': True, 'bed': True, 'perc': True}
    sr = 48000
    with tempfile.TemporaryDirectory() as d:
        for sub in ('sfx/fruit', 'amb', 'music'):
            os.makedirs(os.path.join(d, sub))
        write_wav(os.path.join(d, 'sfx/ok_tap.wav'), [tone(880, 0.12, -30)])
        write_wav(os.path.join(d, 'sfx/loud_tap.wav'), [tone(880, 0.12, -6)])
        write_wav(os.path.join(d, 'sfx/bass_tap.wav'), [tone(120, 0.3, -20, harmonics=(1.0,))])
        cut = tone(880, 0.3, -20)
        cut[-300:] = [0.1] * 300
        write_wav(os.path.join(d, 'sfx/cut_tail.wav'), [cut])
        write_wav(os.path.join(d, 'sfx/wrong_fmt.wav'), [tone(880, 0.12, -20, sr=44100)], sr=44100, bits=16)
        write_wav(os.path.join(d, 'sfx/fruit/note_ok.wav'), [tone(440.0, 0.15, -30)])
        write_wav(os.path.join(d, 'sfx/fruit/note_off.wav'), [tone(452.0, 0.15, -30)])
        seam = [0.2 * math.sin(2 * math.pi * 333.3 * i / sr) for i in range(int(1.5 * sr))]
        write_wav(os.path.join(d, 'amb/bad_seam.wav'), [seam])
        n = 2 * sr
        drone = [0.25 * math.sin(2 * math.pi * 220 * i / n * (n / sr)) for i in range(n)]
        write_wav(os.path.join(d, 'amb/drone.wav'), [drone])
        bed = []
        for i in range(4 * sr):
            t = i / sr
            hit = math.exp(-((t % 0.5) / 0.03))
            bed.append(0.3 * hit * math.sin(2 * math.pi * 600 * t))
        write_wav(os.path.join(d, 'music/bed.wav'), [bed])
        write_wav(os.path.join(d, 'music/perc.wav'), [bed[:-100]])
        sp = os.path.join(d, 'spec.json')
        json.dump(spec, open(sp, 'w', encoding='utf-8'))
        import io
        buf = io.StringIO()
        run(d, sp, None, True, buf)
        failed = {line.split()[1] for line in buf.getvalue().splitlines() if line.startswith('FALLO')}
        print(buf.getvalue())
    bad = [k for k, v in expect.items() if (k in failed) != v]
    for k in expect:
        print('%-10s %s' % (k, ('falla, como debe' if expect[k] else 'pasa, como debe') if k not in bad else 'MAL'))
    print('Autoprueba: %s' % ('todo PASS' if not bad else 'FALLA en %s' % ', '.join(bad)))
    return 1 if bad else 0


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--root', help='carpeta de la entrega (la que contiene sfx/, amb/, music/ y audio_manifest.json)')
    ap.add_argument('--spec', default=SPEC_DEFAULT)
    ap.add_argument('--only', action='append', choices=['sfx', 'fruit_notes', 'ambient', 'music'])
    ap.add_argument('--allow-missing', action='store_true', help='no cuenta como fallo un archivo que aún no se ha entregado')
    ap.add_argument('--self-test', action='store_true')
    a = ap.parse_args()
    if a.self_test:
        sys.exit(self_test())
    if not a.root:
        ap.print_usage()
        sys.exit(2)
    sys.exit(run(a.root, a.spec, a.only, a.allow_missing))


if __name__ == '__main__':
    main()
