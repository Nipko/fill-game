# Guion de la prueba en el teléfono: MAX, Play Games y el icono v2

Lo escribió el integrador (escritorio) el 2026-10-23. Es para el portátil, con el SM-S938B (Android 16) por depuración inalámbrica.

**Qué se prueba:**
- lo que entró desde la prueba del 2026-10-23: AppLovin MAX 8.6.6 con UMP y sin adaptadores, y Play Games 2.3.0 con Saved Games (D-042);
- la nota nueva del borrado;
- el icono v2 (D-045).

**Qué no se puede probar todavía:**
- **Ni MAX ni Play Games funcionan de verdad:**
  - no hay clave del SDK de AppLovin ni `APP_ID` de Play Games;
  - Play Games necesita la app en Play Console, que espera al dictamen (D-034).
- **Lo que se comprueba es que no rompen nada y que no arrancan antes de tiempo.** Los anuncios del build de desarrollo siguen siendo los simulados.

La hoja con los resultados va en `docs/dev/traspaso/pruebas-telefono-<fecha>.md`, con el formato de la del 2026-10-23.

## 0. Preparación

```
git pull origin integracion-fase1-fase2
pwsh -NoProfile -File scripts/unity-build.ps1 -Method Dev
adb devices
adb install -r Builds/Android/dev.apk
adb shell pm clear com.amazonfill.dev
adb logcat -c
```

- Para el build: si no hay `Assets/_LocalSecrets/google-services.json`, el build sale sin Firebase real, lo cual vale para esta prueba. Si lo hay, mejor, igual que el 23.
- Para adb: si `adb devices` sale vacío, pide al dueño que reactive la depuración inalámbrica y no insistas.
- Durante toda la prueba, un logcat en otra consola: `adb logcat -v time > Logs/device/max-pgs/logcat.txt`. Capturas en `Logs/device/max-pgs/`, fuera de git.

## 1. Icono v2 (D-045)

1. **En la pantalla de inicio y en el cajón de apps:**
   - el icono es la cuia de vidrio vertiendo açaí sobre el río café con leche, con el río teñido de violeta a la derecha del impacto, sobre fondo petróleo;
   - lleva la máscara de One UI (squircle), sin bordes blancos ni recortes de la cuia ni del chorro;
   - el nombre es «Tintalú (dev)».

   Captura `icono-inicio.png`.
2. **Icono temático, si One UI lo ofrece** (Fondo de pantalla y estilo › Paleta de colores › «Aplicar paleta a los iconos de las apps», o el equivalente en Android 16): el icono sale en una sola tinta. Se ven la cuia con sus dosis separadas, el chorro, el anillo del impacto y el río teñido relleno. Captura `icono-tematico.png`. Si One UI no lo aplica a apps de terceros, anótalo: no es un fallo nuestro.
3. **Recientes, «Información de la app» y el diálogo de desinstalar:** el mismo icono.
4. **Comprobación por adb:** `adb shell dumpsys package com.amazonfill.dev | findstr /i "icon"` (o `grep`), que tiene que nombrar el recurso `af_launcher`.

## 2. AppLovin MAX sin clave (D-042)

1. **Primer arranque tras `pm clear`, antes de contestar la edad:**
   - en logcat **no** sale nada de AppLovin: `findstr /i "applovin" Logs/device/max-pgs/logcat.txt` no encuentra nada antes de la pantalla de edad. Se quitó el `AppLovinInitProvider`, así que antes del consentimiento no debe correr nada;
   - en logcat sí sale `[AF.Ads] proveedor: not_configured`.
2. **Contesta 18+ y acepta el consentimiento:**
   - sigue sin salir AppLovin (sin clave no se inicia) y no aparece ningún formulario de UMP;
   - el juego entra al nivel 1 como antes.
3. **Anuncios simulados:**
   - en un nivel donde se ofrezca un vídeo (la Cosecha doble, desde el 11), el vídeo simulado se ve y se cobra como antes;
   - `af_ad_request`, `af_ad_shown` y `af_ad_reward` aparecen en `AF_EVENT`.
4. **Con 13-17** (Ajustes › Privacidad › edad, o `pm clear` y contestar 13-17): sin errores, sin AppLovin y con anuncios simulados no personalizados.
5. **Con <13:**
   - el juego bloquea como antes (D-033): ningún SDK arranca;
   - en logcat no sale ni `applovin` ni `games`.

## 3. Play Games sin `APP_ID` (D-042)

1. **En el primer arranque y en los siguientes:**
   - **no** aparece el cartel de Play Juegos («Has iniciado sesión como…»);
   - en logcat no hay `PlayGamesSdk`, `GamesSignIn` ni `games.provider`: `findstr /i "playgames gamessignin" logcat.txt` no encuentra nada.
2. **El progreso se guarda en el teléfono:**
   - gana dos niveles, cierra la app (`adb shell am force-stop com.amazonfill.dev`) y vuelve a abrir: sigues donde estabas;
   - en Ajustes no hay nada nuevo de Play Juegos.
3. **Con <13:** igual, sin ninguna línea de Play Games.

## 4. Nota del borrado (2.ª revisión de D-042)

1. **Ajustes › «Eliminar cuenta y datos» › paso 2, la confirmación.** Debajo sale la nota nueva:
   - es: «Pedimos borrar tu copia en Google Play Juegos. Si falla, se reintenta al abrir el juego con Play Juegos. También puedes borrarla allí.»
   - en y pt: la misma idea, con `af_lang en` y `pt-BR` (como el 23).
2. **Con la fuente del sistema al 130 %:** la nota cabe en los tres idiomas sin cortarse ni tapar los botones. Capturas `borrado-nota-<idioma>.png`.
3. **Cancela:** no hagas el borrado en esta prueba.

## 5. Regresión rápida

- **Niveles 1-12** ganados con las sondas `AF_LAYOUT` y `AF_STATE` (`docs/dev/prototipo-v0.md`), sin ninguna excepción: `adb logcat -s Unity:E` vacío.
- **Mercado y Cántaro ocultos**, porque la app no está en Play Console (como el 23).
- **El pre-prompt de avisos y la Bitácora** salen como antes.
- **Tamaño:** anota el de `dev.apk`. En el escritorio, el build con MAX y Play Games mide 72,4 MB. El AAB de ensayo pesa 55,94 MB y la descarga de Play, como máximo, 54,95 MB, bajo el tope de 80.

## 6. Qué devolver

La hoja con:
- cumple o no cumple en cada punto 1-5, con su captura o la línea de logcat;
- las observaciones numeradas.

Con la regla vigente (correcciones en la misma ola), los arreglos se lanzan en cuanto la hoja llega al escritorio.
