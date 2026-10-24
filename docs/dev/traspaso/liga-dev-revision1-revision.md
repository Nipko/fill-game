# Liga: revisión 1 para aprobación de dev

24-10-2026. Rama `codex/liga`, código `c143f075`. Base integrada con `git pull origin integracion-fase1-fase2`; incluye `03332703`. Se aplican B1 y S1–S5 del traspaso, siguiendo el §3.2 revisado sin modificar `docs/dev/liga.md`.

## Comportamiento preparado

- El avance de referencia se guarda también cuando una campaña bien formada se rechaza. Los huecos reponen referencia y la victoria cuenta; solo `level > max_level` da `unreachable_level`.
- Ningún rechazo activa `flagged`. Las marcas existentes se conservan; no se realiza una limpieza automática que pudiera quitar una marca manual.
- Se admiten hasta cinco minutos de reloj adelantado, inclusive, dentro de la semana actual.
- Cada elemento mal formado devuelve su propio `invalid`; si falta un win_id de texto, devuelve `win_id: ""`. La región mala/vacía/null es desconocida. Se retira el tope fijo de 700; un nivel ausente del catálogo o anterior al desbloqueo es `invalid`.
- Las campañas se evalúan por nivel, conservando la posición de los Retos. El GDD 08 §2.4 distingue la Liga Global/País de la 1.0 y las ampliaciones de la 1.2.

## Verificación

`npm ci`, lint, tipos, **361 pruebas / 20 archivos** (24 pruebas nuevas), tabla generada de 700 niveles, `web:check` y synth dev/prod correctos. Las regresiones cubren semana offline, 41.ª victoria/hora, Liga apagada, reinstalación/cola perdida, reloj adelantado, referencia persistida tras rechazos, lote invertido, elementos mixtos y marcas manuales. No se cambian dependencias; npm informa una vulnerabilidad alta heredada.

## Alcance del diff real

[`cdk diff AfDevApi --strict --no-change-set --exclusively`](liga-dev-revision1-diff.txt), con el contexto privado y la sesión vigente de `af-dev`: exit 0. **Una pila con diferencias**.

Comparación contra el ensamblado del último despliegue aprobado:

| Pila | Cambios nuevos |
|---|---|
| AfDevData | Ninguno; JSON idéntico |
| AfDevOps | Ninguno; JSON idéntico |
| AfDevApi | Solo Code.S3Key y aws:asset:path de LeagueWins, LeagueBoard y DeleteAccount |

`DeleteAccount/index.mjs` es idéntico al anterior: cambia su sourcemap por compartir la fuente de Liga. Los paquetes de las dos funciones de Liga cambian con las reglas. **Sin recursos nuevos, eliminaciones, cambios IAM, rutas, alarmas, valores de secretos ni coste fijo adicional.** Los cambios Unicode en descripciones/metadata que muestra el diff estricto son los ya registrados en el despliegue anterior; se incluyen para que el dueño vea el diff completo. No cambian permisos ni contenido de secretos.

Base dev conservada: **https://4tbucfarv9.execute-api.sa-east-1.amazonaws.com**. No cambia BackendEndpoints.

Ensamblado congelado fuera del repositorio: `58` archivos. [Inventario SHA-256](liga-dev-revision1-assembly.sha256.json), digest **`e265c58a270471863cc03bc4d8fdd767c3f8f86aa16e7ecd9508151ece13a09c`**. Las fuentes de reglas en los tres sourcemaps coinciden con el commit de código. El despliegue propuesto utiliza este ensamblado, exclusivamente **AfDevApi**, verificando hashes e identidad antes de ejecutarlo.

**Aprobado y desplegado el 24-10-2026.** El dueño respondió literalmente: «Sí, desplegar únicamente AfDevApi». Se comprobaron los 58 hashes y la identidad de af-dev antes de desplegar el ensamblado congelado, exclusivamente AfDevApi. CDK terminó con exit 0 y CloudFormation en UPDATE_COMPLETE. Las tres funciones están Active/Successful y todos los archivos de sus ZIP, incluidos los sourcemaps, coinciden byte por byte con el ensamblado aprobado. Las 27 alarmas están OK; POST wins y GET Global/País devuelven 401 sin token. La URL se conserva. Resultado detallado en `codex-estado.md`. No se ha ejecutado una prueba autenticada porque no se ha proporcionado un token de prueba.
