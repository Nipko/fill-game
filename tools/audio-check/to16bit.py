#!/usr/bin/env python3
"""Pasa a 16 bits (con redondeo) los WAV PCM de 24 bits de una carpeta, en su sitio. Solo biblioteca estándar.

El audio del juego se guarda a 16 bits en el repositorio para no llenar la cuota de Git LFS: Unity lo recomprime a Vorbis al
construir el APK, así que la diferencia no se oye (decisión del dueño del 2026-10-17). Los WAV que ya son de 16 bits no se tocan.

Uso: py -3.14 tools/audio-check/to16bit.py Assets/_Project/Audio
"""
import array
import os
import sys
import wave


def convert(path):
    with wave.open(path, 'rb') as w:
        p = w.getparams()
        raw = w.readframes(p.nframes)
    if p.sampwidth != 3:
        return False
    n = len(raw) // 3
    wide = bytearray(n * 4)  # 24 bits en los 3 bytes altos de un entero de 32: conserva el signo
    wide[1::4] = raw[0::3]
    wide[2::4] = raw[1::3]
    wide[3::4] = raw[2::3]
    v = array.array('i')
    v.frombytes(bytes(wide))
    if sys.byteorder != 'little':
        v.byteswap()
    out = array.array('h', [min(32767, (x + 32768) >> 16) for x in v])  # redondeo al entero de 16 bits más cercano
    if sys.byteorder != 'little':
        out.byteswap()
    with wave.open(path, 'wb') as w:
        w.setnchannels(p.nchannels)
        w.setsampwidth(2)
        w.setframerate(p.framerate)
        w.writeframes(out.tobytes())
    return True


def main():
    root = sys.argv[1]
    done = 0
    for d, _, files in os.walk(root):
        for f in sorted(files):
            if f.lower().endswith('.wav') and convert(os.path.join(d, f)):
                done += 1
    print('Convertidos a 16 bits: %d WAV' % done)


if __name__ == '__main__':
    main()
