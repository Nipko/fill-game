# Encargo para Spark: tercera medida en el Moto G23 (D-060)

2026-11-17. La segunda ronda (`pruebas-telefono-2026-11-17-moto-g23-r2.md`) dejó el ensayo de release en 470-479 MB de PSS con todo en memoria, frente a ≤ 450 MB. Desde `6b3c7493`, `integracion-fase1-fase2` lleva dos cambios para bajarlo (añadido de D-060 del 2026-11-17; `docs/dev/memoria.md` §11):

- IL2CPP «Faster (smaller) builds»: `libil2cpp.so` pasa de 62 a 32 MB.
- Un lector JSON con menos basura al arrancar.

## Qué hacer

Compila el ensayo (`scripts/unity-build.ps1 -Method Release -Rehearsal -Apk`) y el `dev.apk` (`-Method Dev`) de ese commit o posterior. Mide en el Moto G23 como en la r2, con `cmd package compile -m speed-profile -f` y un minuto quieto antes de cada `dumpsys meminfo -d`. Sigue la lista de `memoria.md` §11.4:

1. **TOTAL PSS del ensayo** en el inicio, en el Mapa (vuelta 1), en el inicio tras el Mapa y en el nivel 67. Compáralo con la r2: 469,9 / 479,2 / 472,4 / 430,1 MB.
2. **`Code`** y **`.so mmap`**.
3. **Del `dev.apk`:** `mono_heap_mib` y `peak_mono_heap_mib`, con los `heap_steps_mib` de la línea `boot`.
4. **Fotogramas** del 340 y del 386 en juego automático, frente a la r2 (p95 de 16,8-16,9 ms; máximo de 65,9 ms en el 386).
5. **Morpho:** `--ei af_bench_morpho 1`, frente al factor 8,08 de la primera ronda.
6. **Arranque y Reto:** `splash_ms` y `levels_load`; `open_ms` al abrir el Reto.

**Si puedes**, en el S23 Ultra:

7. **La Marea** (añadido nuevo de D-014). Cuando el carril `v11/marea-estancado` esté integrado, prueba el tablero de la captura del dueño o el 66. «Deshacer · Marea» desde el modal tiene que dejar jugar, y el modal no ofrece Marea cuando no sirve.

## Qué devolver

Una hoja `docs/dev/traspaso/pruebas-telefono-2026-11-1X-moto-g23-r3.md` en una rama `spark/prueba-moto-g23-r3` creada desde `integracion-fase1-fase2`. Debe incluir la tabla de PSS y, al final, una línea con tu lectura: si el ensayo queda por debajo de 450 MB o por cuánto se pasa. Con eso decide el dueño D-060.
