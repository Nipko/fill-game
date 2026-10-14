# Prueba de la tanda 3 en el teléfono (2026-10-14): resultados para el escritorio

El portátil compiló `integracion-fase1-fase2` en `2c75897` (incluye la tanda 3 de `83ffcda`; `main` está igual). Instaló el `dev.apk` en el SM-S938B (Android 16) y siguió «QUÉ PROBAR EN EL TELÉFONO» de `83ffcda`.

- **Conexión:** después del build, el adb del teléfono volvió a colgarse por USB («cannot get serial number (31)»), así que las pruebas se hicieron **por depuración inalámbrica**. adb lo encontró por mDNS como `adb-R5CY92CG81P-toTYBb._adb-tls-connect._tcp`, sin emparejar de nuevo.
- **Cómo se probó:** `AF_STATE` y `AF_LAYOUT` de logcat, toques con `adb shell input tap`, capturas directas al PC y vídeos de `screenrecord`, cuyos fotogramas saca el decodificador de Windows.

**Resultado: la tanda 3 cumple en los cinco puntos.** No hay defectos nuevos.

Las capturas, los vídeos y los logcat están en el portátil, en `Logs/device/tanda3/` (fuera de git).

## Verificación (portátil ARM64)

| Comando | Resultado |
|---|---|
| `dotnet build dotnet/AmazonFill.slnx -warnaserror` | 0 avisos, 0 errores |
| `dotnet test dotnet/AmazonFill.slnx` | 820 superados, 4 omitidos (824) |
| `python tools/validate_schema.py` | 40/40 PASS |
| `pwsh -NoProfile -File scripts/unity-compile.ps1 -TimeoutSec 3600` | Compila (código 0) |
| `unity run … -executeMethod AF.EditorTools.BuildDev.BuildAndroidDev` | `[AF.BuildDev] OK`: `dev.apk` de 62,90 MB, 515 s de build del APK, 1 aviso |
| `git status` tras el build | Limpio: sin fuentes TMP que restaurar |
| `adb install -r Builds/Android/dev.apk` (inalámbrico) | `Success` (15 s) |

Los tests de Unity (EditMode y PlayMode) no se ejecutaron en el portátil esta vez: no los pide la hoja de la tanda 3, y arrancar el Editor tumba el adb del teléfono. En `83ffcda`, el escritorio da EditMode 1.580 (1.577 + 3 omitidos) y PlayMode 452 (401 + 51).

## Resultados

### 1 · Bucle del 66 → Aguas estancadas (T51, D-014): cumple

- **Nivel 66** (`--ei af_level 66`) con la secuencia del dueño `3>6 4>3 7>4 0>5 1>5 2>0 2>1 5>2 1>5`:
  - los vertidos 1-8 llegan con `status: playing`;
  - **el 9.º (`1>5`) llega con `status: stuck`**, seguido de `stuck` y `rescue_check`, y a los 1,9 s de `stuck_modal`.
- **El modal** (`t1_l66_estancadas.png`):
  - «No hay más vertidos · Aguas estancadas · Travesía perdida: −1 canoa»;
  - «Deshacer · Marea» (3) y «Continuar: +1 cuia» (300, antes 600);
  - Puñado y Balsa;
  - «Rendirse».
  - `AF_LAYOUT stuck`: `undo_modal`, `rescue`, `handful`, `balsa` y `give_up`.
- **Ya no se puede dar vueltas:** con el modal abierto, un toque en la cuia 2 no da ningún evento.

### 2 · El aviso no tapa las burbujas «Te faltan» (T52; pregunta 41): cumple

- **135, 3 filas**, perfil limpio, `--ei af_seeds 0 --es af_ads_fill no_fill`.
- Al tocar Revelar sale `reveal missing` y el aviso «Te faltan 120 semillas». Con la bandeja activa, el aviso va como una píldora sobre la repisa, y **las burbujas «Te faltan 150» (+1 cuia) y «Te faltan 120» (Revelar) siguen a la vista**, igual que la ficha «?×1». Archivos: `t2_l135_hoja.jpg` y `t3b_l135_hoja.jpg`, captura 001.
- Mientras se resuelve el último vertido, la bandeja se apaga y sus burbujas no se ven. Entonces el aviso ocupa todo el ancho, y no tapa nada.

### 3 · El aviso se oculta al abrirse Aguas estancadas y no vuelve (T52, 42 b): cumple

- **135:**
  1. vertido `2>9`;
  2. Revelar (`reveal missing` a las 06,03): aviso de 3 s;
  3. vertido `1>7`: atasco, y `stuck_modal` a las 08,10, con el aviso aún dentro de sus 3 s.
- **Con el modal abierto, el aviso no está.**
- **Al cerrar el modal con Marea** (`undo` a las 08,53), cuando al aviso aún le quedaba ≈ 0,5 s, **no vuelve**, ni a ≈ 0,6 s ni a ≈ 2,4 s (`t3b_l135_hoja.jpg`).

### 4 · Banner de mundo: `wait_ms` ≈ 0 con «Siguiente» (T52): cumple

Perfil limpio; se ganaron el 10, 40, 80 y 140 y se tocó «Siguiente»:

| Nivel | `wait_ms` | `mascots_ms` | `main_ms` | `fallback` |
|---|---|---|---|---|
| 11 | 2 | 88,3 | 1,5 | none |
| 41 | 1 | 79,6 | 0,7 | none |
| 81 | 1 | 96,9 | 0,8 | none |
| 141 | 4 | 98,1 | 3,2 | none |

En la tanda 2 eran 66-111 ms. `main_ms` ≤ 3,2 ms (el límite es 8).

### 5 · Remolino: el chorro entra en la espiral y la pareja se ilumina (T52): cumple

Medido en vídeo, con fotogramas cada 100 ms:

- **140**, vertido 7 de la solución (`3>4`, `recv 1`), archivos `t5_l140_hoja.jpg` y `t5_l140_zoom.png`:
  - el chorro naranja cae en la cuia 4 (Remolino) y en su superficie se forma un remolino, con la espiral de la base marcada;
  - ≈ 0,3 s después, la pareja (cuia 1) recibe el naranja por el fondo, con destellos, y sus dosis suben.
- **103**, vertido 4 de la solución (`3>8`, `recv 2`), archivos `t5_l103_hoja.jpg` y `t5_l103_par.png`:
  - el chorro morado cae en la cuia 8 (Remolino, fila 3), con el remolino en la superficie;
  - la pareja (cuia 2, fila 1) recibe el morado por el fondo, con destellos y su espiral iluminada;
  - el canal discontinuo une las dos cuias entre las filas 1 y 3.

## Observaciones

1. **adb por USB se cuelga tras el build de Unity**, y el teléfono sigue con el almacenamiento al 100 % (1,8 GB libres de 456 GB, menos que el 2026-10-13). La depuración inalámbrica funcionó sin cortes durante toda la prueba: para el portátil, mejor usarla por defecto.
2. **Las pruebas de avisos dependen del vídeo:** en el 135, Revelar sin stock ni semillas solo da «Te faltan» con `--es af_ads_fill no_fill`. Con vídeo, abre `rv_reveal`.
