# Guion de la prueba en el teléfono: la Liga del río contra el backend de dev (D-046)

Lo escribió el integrador el 2026-10-24. Es para el portátil, con el SM-S938B por depuración inalámbrica.

**Qué se prueba:** el cliente de la Liga (`tanda6/liga-cliente`, integrado en `00a596df`) contra el backend real de Codex, desplegado en dev con la misma `ApiUrl` de `BackendEndpoints.Dev`. Es la primera vez que la Liga habla con el servidor de verdad; hasta ahora solo se había probado con la Liga falsa.

**Requisito:** `Assets/_LocalSecrets/google-services.json`, como el 23. Sin Firebase real no hay ID token ni App Check, y el servidor responde 401. El token de depuración de App Check del SM-S938B ya está registrado.

La hoja con los resultados va en `docs/dev/traspaso/pruebas-telefono-<fecha>-liga.md`, con el formato de la del 23.

## 0. Preparación

```
git pull origin integracion-fase1-fase2
pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev
adb install -r Builds/Android/dev.apk
adb shell pm clear com.amazonfill.dev
adb logcat -c
```

- **Logcat:** en otra consola, `adb logcat -v time > Logs/device/liga/logcat.txt`, filtrando como el 23 las líneas de App Check («debug secret», «debug token», `eyJ`). Capturas en `Logs/device/liga/`, fuera de git.
- **Arranque:** con la Liga encendida y el perfil en el 41, sin jugar 40 niveles:
  ```
  adb shell am start -n com.amazonfill.dev/<actividad> --ez af_league true --ei af_max_level 41
  ```
  **Sin** `af_league_fake`. La `<actividad>` es la de `prototipo-v0.md`.

## 1. Primer contacto

1. Contesta 18+ y acepta el consentimiento.
2. **En el inicio** aparece el botón «Liga», abajo a la izquierda. Captura `liga-inicio.png`.
3. **Ábrelo, sin jugar todavía:**
   - pestaña Global: lista vacía o con otros jugadores de dev, y tu fila con 0 puntos;
   - pestaña de tu país: «Colombia» con su bandera, si la región del teléfono es CO;
   - en logcat, `af_league_open` y las dos lecturas sin error. Si sale 401 o 403, anota la línea (sin el token): es App Check o Auth.

## 2. Una victoria cuenta

1. **Gana el nivel 42** (el siguiente al 41).
   - En la victoria sale «+{n} puntos de Liga» (10, 20 o 30, según su dificultad).
   - En logcat, `af_league_wins_sent` con `accepted` 1.
2. **Vuelve a la Liga:**
   - tus puntos son los de la victoria;
   - tu seudónimo («Animal 1234») es el mismo en las dos pestañas;
   - si eres el único jugador, sales el 1.º.

   Captura `liga-global.png` y `liga-pais.png`.
3. **Ajustes:** «País en la Liga: Colombia».

## 3. Sin conexión

1. **Modo avión, gana el 43 y el 44.**
   - La victoria enseña los puntos.
   - La Liga enseña lo último descargado, con «Actualizado hace…».
2. **Quita el modo avión y vuelve al inicio:**
   - en logcat, un envío con `accepted` 2;
   - en la Liga, los puntos de las tres victorias.
3. **Cierra la app a la fuerza** (`am force-stop`) con una victoria aún sin enviar (modo avión otra vez) y vuelve a abrir con red: se envía.

## 4. Lo que no debe pasar

- **Con 13-17** (otro `pm clear`): la Liga sale igual, solo con seudónimo y bandera.
- **Con <13:** ni botón ni ninguna petición a `/v1/league` en logcat.
- **Borrado de cuenta:** no lo hagas en esta prueba (ya está probado en PlayMode). Si el dueño quiere probarlo con una cuenta de dev, se hace después, aparte.
- **En todo el logcat,** 0 líneas `E/Unity`.

## 5. Qué devolver

La hoja con:
- cumple o no cumple en 1-4, con su captura o su línea de logcat;
- tu seudónimo y los puntos que viste;
- las observaciones numeradas.

No pegues tokens, UID ni URL firmadas.
