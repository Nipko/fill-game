# B10: safe area y retroceso predictivo (08 §13.1 #4)

Bloque B10 de la tanda 5 (`docs/dev/plan-tanda5.md` §4, rama `spark/auditoria-1-0`). El paso 4 de la checklist de Play pide el «retroceso predictivo probado y la UI dentro del *safe area*». Esta nota recoge lo que falta. El código de juego no lo toca build-release, así que el arreglo va como parche aparte.

## Safe area: estado (auditoría del 2026-10-20, solo lectura)

| Superficie | Estado |
|---|---|
| GameScreen, LobbyScreen, RiverMapScreen, SettingsScreen, FruitSheet | Bien: aplican `MinSafeTop`/`MinSafeBottom` con `GameScreen.ReadScreen`. |
| DailyChallengeScreen | Bien: monta un `GameScreen`, que trae sus propios márgenes. |
| LevelFailedScreen, NoCanoesScreen, StuckModal, modales centrados | Bien: van centrados o acotados con `_safeBottom`. Hay un desborde conocido de StuckModal a 16:9 con el texto al 130 %, en un test `[Explicit]` (decisión DC3). |
| BootSplash, tarjeta del Reto en el inicio | Bien en la práctica: arte proporcional, sin controles cerca de los bordes. |
| **StoreSheet** (compra simulada) | **Hueco.** Es una hoja inferior y «Cancelar» queda a unos 16 dp del borde físico, sin restar el margen inferior. Con una barra de navegación o de gestos, la tapa. `OverlayCanvas.Measure()` descarta el safe area (`ReadScreen(out sw, out sh, out Rect _)`). |
| AdOverlay, DailyStampCalendar | Huecos menores: los contenidos no tienen en cuenta el safe area, aunque con las proporciones actuales caen dentro. Conviene usar los mismos márgenes de `OverlayCanvas` cuando se toquen. |

Márgenes laterales: `SimulatedScreen` no los admite (`ReadScreen` fija `x = 0`) y ninguna disposición lee `safeArea.x`. Con la orientación vertical bloqueada, el riesgo se limita a los bordes curvos.

### Parche: `b10-storesheet-safe-area.patch`

`OverlayCanvas` expone `SafeTop` y `SafeBottom` (en unidades del lienzo), y `Measure()` también recalcula cuando cambia el safe area. La hoja suma `SafeBottom` a su altura: sigue llegando al borde inferior, pero su contenido queda por encima de la barra. Se aplica con `git apply docs/dev/traspaso/b10-storesheet-safe-area.patch` (el `.patch` va en LF por `.gitattributes`) sobre `Assets/_Project/Scripts/UI/StoreSheet.cs`, que no es de GameScreen ni de AppFlow. Le toca a unity-gameplay-dev, junto con un test de PlayMode: `SimulatedScreen` con un margen inferior grande (p. ej. 220 px) y la comprobación de que `CancelButton` queda dentro del safe area.

## Retroceso predictivo: estado

- Atrás llega como Escape al Input System: la acción `AndroidBack`, `<Keyboard>/escape`, en `GameBootstrap`. BackButtonTests lo prueba, igual que la prueba en el teléfono (FXT).
- `ProjectSetup.PredictiveBack = false`. Unity escribe `android:enableOnBackInvokedCallback="false"` en el manifiesto, es decir, la app se excluye del retroceso predictivo. Con targetSdk 36, Android 16 todavía respeta esa exclusión. `ci/check-manifest.ps1` lo anota en cada build.
- **Falta, para cerrar el punto 4:**
  1. poner `ProjectSetup.PredictiveBack = true` (`PlayerSettings.Android.predictiveBackSupport`) y ejecutar `ApplyAndroidDefaults`;
  2. probar en el teléfono (Android 16) Atrás en frío y en cada pantalla, con la animación de vuelta al inicio, y comprobar que Unity sigue entregando Escape;
  3. pasar `-RequirePredictiveBack` a la compuerta del manifiesto en `.github/workflows/ci.yml`.

  Sin teléfono no se puede verificar (el 2026-10-20, `adb devices` salió vacío), así que se queda desactivado.
