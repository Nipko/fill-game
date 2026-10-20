# Pendientes del dueño: lo que depende de ti para llegar a la 1.0

Fecha: 2026-10-20. Lo reúne el integrador a partir de `docs/legal/marca.md`, `docs/dev/plan-tanda5.md` §3, `docs/dev/traspaso/README.md` (sección 6, paso 4), `docs/gdd/08-arquitectura-tecnica-publicacion.md` §13 y las decisiones D-033 y D-034. Ordenado por lo que más bloquea. Marca cada casilla al hacerla y anota la fecha.

Lo que **no** depende de ti sigue en marcha: la tanda 5, en el escritorio y en el Spark, con implementaciones falsas de los SDK. Los borradores de publicación los hará un agente Codex cuando lo actives (`docs/dev/traspaso/codex-publicacion.md`; de momento esperas a la marca).

---

## 1. Marca y nombre (lo que más bloquea)

| ✓ | Tarea | Detalle | Fuente | Qué desbloquea |
|---|---|---|---|---|
| [ ] | Contratar al abogado de marcas | (1) Búsqueda en el INPI de Brasil de TINTALÚ, TINTALU, TINTAL\* y TINTA LU\* en las clases 9, 28 y 41 (y la 2: en Brasil «tinta» es pintura); dictamen de registrabilidad. Unos R$ 500-1.500 [E], 1-2 semanas. (2) Dictamen en México frente a EL TINTAL (IMPI reg. 2586508, clase 41) y TINTALAIRE. (3) Estrategia frente a TINTA en EE. UU. | `docs/legal/marca.md` §2 y §5 | Todo lo siguiente |
| [ ] | Recibir el dictamen | Cubre también «TiTino». Si sale favorable, se fija el `applicationId`; si no, planes B (`marca.md` §1.3) | `marca.md` §6 | Crear la app en Play Console (D-034), la pista interna y el build de release |
| [ ] | Decidir el nombre del estudio y su país | Es el `<estudio>` de `com.<estudio>.tintalu`, que no cambia tras la primera subida y es público. Sin ninguna forma de «Amazon». El país decide dónde se presenta la marca | 08 §13.1 #1; `marca.md` §3 | `applicationId`, nombre de desarrollador, solicitud base |
| [ ] | Presentar la solicitud de marca | Solicitud base en las clases 9 y 41 en el país del estudio (y, si es miembro del sistema de Madrid, presupuesto para CO, MX, BR y US) | `marca.md` §3 y §5 | Protección del nombre |

---

## 2. Dominios, web y redes

| ✓ | Tarea | Detalle | Fuente |
|---|---|---|---|
| [ ] | Comprar `tintalu.com` | Propuesto como dominio principal (web, política, eliminación de cuenta, `app-ads.txt`, enlaces de la ficha): confírmalo | `marca.md` §4.1; 08 §13.1 #2 |
| [ ] | Comprar `tintalu.app` | Exige HTTPS | `marca.md` §4.1 |
| [ ] | Comprar `tintalu.co` | Colombia es el primer mercado | `marca.md` §4.1 |
| [ ] | Comprar `tintalu.com.br` | Por medio del abogado o representante brasileño: Registro.br exige CPF o CNPJ | `marca.md` §4.1 |
| [ ] | (Opcional) `tintalu.games`, `tintalu.net`, `tintaloo.com`, `tintalú.com` | Defensivos | `marca.md` §4.1 |
| [ ] | Elegir el alojamiento de la web, con HTTPS | Por ejemplo GitHub Pages en un repositorio que crees tú, o el hosting del registrador. Lleva: política de privacidad (ES, PT-BR, EN), términos, página de eliminación de cuenta (`/tintalu/eliminar-cuenta`) y `app-ads.txt` | 08 §11 y §13.1 #2 |
| [ ] | Reservar los usuarios en redes | Instagram, TikTok, YouTube, X y Facebook | `marca.md` §4.2 |
| [ ] | Datos legales del responsable | Nombre o razón social, dirección, correo de contacto y de privacidad, encargado (DPO) y canal para la LGPD de Brasil | 08 §13.1 #2 |

Coste orientativo de los cuatro dominios principales: unos 50-100 USD al año [E].

---

## 3. Cuentas, permisos y decisiones técnicas

| ✓ | Tarea | Detalle | Fuente | Qué desbloquea |
|---|---|---|---|---|
| [ ] | Cuenta de desarrollador de Google Play | Pago único, verificación de identidad y, si es de organización, D-U-N-S. Decide **personal u organización**: las cuentas personales nuevas suelen exigir una prueba cerrada con testers durante 14 días antes de producción (a verificar en la documentación oficial vigente). La app **no** se crea hasta el dictamen (D-034) | 08 §13.1; D-034 | La consola, cuando llegue el dictamen |
| [ ] | Aprobar el permiso `INTERNET` | Hoy lo añade Unity en automático; está pendiente de tu visto bueno | `CLAUDE.md` | Anuncios, compras, Firebase, Play Games |
| [ ] | Aprobar los SDK | AppLovin MAX con adaptadores y UMP (consentimiento), Unity IAP 5.4.4, Firebase 13.17.0 (Remote Config, Analytics, Auth, Functions, Crashlytics, App Check) y Play Games 2.3.0 | `plan-tanda5.md` §3 | Todo lo «online» real |
| [ ] | Crear las cuentas de Firebase, AppLovin MAX, AdMob y Play Games Services | Más las redes de la mediación (Unity Ads, Meta, Mintegral) para `app-ads.txt` | 08 §13.1 #2 y #16 | Integrar los SDK, `app-ads.txt`, Data Safety real |
| [ ] | Keystore, Play App Signing y secretos del CI | La clave de subida y los secretos (`UNITY_EMAIL`, `UNITY_PASSWORD`) solo los manejas tú | 08 §13.1 #3; s31 | El workflow s32 y el build firmado |
| [ ] | Decidir el espacio del AAB | Addressables y Play Asset Delivery, fuentes recortadas o arte a 1×; hoy el AAB pesa unos 52 MB, el tope es 80 MB y faltan los SDK y el arte final | `plan-tanda5.md` §3; README §1 «Tamaño» | Paquetes 701+, fondos remotos, Unity Localization (Q-T2) |
| [ ] | Aprobar el merge del CI (`ci.yml`, s30) cuando esté | Lo prepara B10 (aprobado en D-033, Q-T5); el merge lo apruebas tú | D-033 | La compuerta automática en cada PR |

---

## 4. Arte (artista 2D freelance)

Todo el arte de hoy es provisional, hecho por código. Los encargos ya están escritos:

| ✓ | Tarea | Documento | Contenido |
|---|---|---|---|
| [ ] | Contratar al artista | — | Entrega en PNG/PSD por Git LFS; los agentes lo integran |
| [ ] | Enviar el encargo del Mapa y los fondos | `docs/art/encargo-mapa-del-rio.md` | Fondos por parada, kit de cada mundo, héroes de cada sitio, sprites de ambiente, segundo juego de follaje y piezas del Mapa del río (incluye coste y peso orientativos, §7) |
| [ ] | Enviar el encargo de TiTino, Frutas y la Tinaja | `docs/art/encargo-titino-frutas-tinaja.md` | TiTino (protagonista), las 14 escenas de Frutas del río y la Tinaja |
| [ ] | Encargar las skins de las cuias | 06 §4.5; 07 §4.6 | Las de los hitos 40, 80 y 140 y las del Mercado (hoy, provisionales) |
| [ ] | Encargar los assets de tienda | 07 §10; 08 §13.1 #5 | Icono 512 (4 conceptos en 07 §10.1; recomendado «A. Cuia que vierte»), gráfico 1024 × 500, 8 capturas por idioma (07 §10.2), vídeo de 30 s (§10.3) y creativos de anuncios (§10.4) |
| [ ] | (1.1) El elenco de unas 50 especies | D-033, Q-N6 | La 1.0 sale con los 11 miquitos |
| [ ] | (Opcional, mientras tanto) Arte provisional mejor | `docs/art/prompts-chatgpt.md` | Prompts listos para ChatGPT: fondos, follaje, botones, paneles, Capi, frutas y héroes provisionales |

---

## 5. Asesor cultural y revisión de idiomas

| ✓ | Tarea | Detalle | Fuente |
|---|---|---|---|
| [ ] | Contratar al asesor cultural | Revisa nombres y textos de fauna, flora, pueblos y lugares; hasta su visto bueno quedan «pendiente de revisión» | `CLAUDE.md`, «Humanos» |
| [ ] | Nombres y textos `pendiente_asesor` | Entre ellos «sauim-de-coleira», los Datos del río, las frases de TiTino (8 nuevas de DC25), `map_l10_intro`, `tutorial_morpho`, los `hint_*` con fauna, `tutorial_brote_n2`, `tutorial_flor`, la embaúba de la ventanita y las líneas 17-20 del cacao y del arazá (8 líneas por redactar) | README §6 paso 4 |
| [ ] | Audio | 24 piezas con `advisor_review` pendiente (17 con especie o región sustituta, no amazónica, entre ellas el boto) | README §6 paso 4 |
| [ ] | Preguntas de la traducción | T2-Q8 a T2-Q11: Amazonas o Amazonía, Manaus o Manaos, los textos del glosario (00 §10) y los nombres de los comodines | `docs/dev/traspaso/spark-estado.md` |
| [ ] | Otras del asesor | F7, F11 y Q17 (fauna y guion de la intro) | README §6 paso 4 |
| [ ] | Revisión nativa de inglés y portugués de Brasil | Las traducciones de hoy son de IA (D-022, pregunta 51 b) | D-022 |

---

## 6. Pruebas y QA

| ✓ | Tarea | Detalle | Fuente |
|---|---|---|---|
| [ ] | Conseguir un teléfono de gama baja (clase A) | Moto G (2x/3x de 2022-23), Galaxy A0x/A1x o Redmi 9/10, con Mali-G52 o Adreno 610. Mide fps, memoria, el shader del líquido, el factor de Morpho y TalkBack | 08 §14.1; pregunta 44 |
| [ ] | Dar por buena la Tinaja en el 323 | Depende del factor de Morpho en ese teléfono; hasta entonces 301-700 salen con la variante b | README, «El 323 sale sin Tinaja» |
| [ ] | Prueba visual de 30 min en el teléfono (s28) | — | `CLAUDE.md` |
| [ ] | Escuchar el audio en el teléfono | Notas de fruta, chorro, capas de la música tras varios bucles y que en `amb_w01_fauna_02` no se entienda ninguna voz (pregunta 64) | README §6 paso 4 |
| [ ] | Curación de los mundos 6-9 | Informes `tools/out/curation_w06.md` a `curation_w09.md` (en 7-9: 122 niveles y 13 retos sin etiqueta); decides en `curation_*.json` (D-033, Q-N8) | README §6 paso 4 |
| [ ] | Curación a mano de 11-200 | Comprobaciones #12-#13 de 03 §17 | `plan-tanda5.md` §3 |
| [ ] | Contratar QA humana externa de 201-700 | Unas 40 h; la pide la compuerta de 09 §1.1 #7 | `plan-tanda5.md` §3 |
| [ ] | Grabaciones de Magic Sort (#13) | Referencia de tiempos y sensaciones | README §6 paso 4 |
| [ ] | Prueba cerrada en Play (más adelante) | 20-50 testers de CO, PE y MX durante 1-2 semanas | 08 §13.1 #20 |

---

## 7. Legal (además de la marca)

| ✓ | Tarea | Detalle | Fuente |
|---|---|---|---|
| [ ] | Revisar los textos legales | Política de privacidad, términos y página de eliminación de cuenta (los redacta Codex como borrador) | 08 §13.1 #2 y #7 |
| [ ] | Revisar la edad y el consentimiento | Bandas `<13` (bloqueado en la 1.0), `13-17` y `18+`; consentimiento por país | 08 §13.1 #11; 05 §7 |
| [ ] | Confirmar la compra de 13-17 | Doble confirmación en el juego + autenticación de Google Play en lugar de un PIN propio | D-033, Q-T4 |
| [ ] | ECA Digital (Brasil) | Consulta al abogado brasileño (art. 8) en la compuerta de la fase 5 | 08 §13.1 #11 |
| [ ] | Licencia de HydroRIVERS | Leer su acuerdo (en hydrosheds.org; no viene en la descarga), fijar el texto de atribución y decidir dónde va (aún no hay pantalla de créditos) | README §6 paso 4 |

---

## 8. Negocio (sin prisa: se revisan en la fase que las necesite)

| ✓ | Tarea | Fuente |
|---|---|---|
| [ ] | Reparto entre compras y anuncios, presupuesto del soft launch, beta, atribución, iOS y otras «Preguntas abiertas» antiguas de los capítulos 00-09 sin recomendación | README §6 paso 4 |
| [ ] | Precios por país (BR, MX, CO, PE, AR, CL, EC), a mano en Play Console | 08 §13.1 #14; 05 §4.2 |

---

## Orden sugerido

1. **Esta semana:** abogado de marcas, nombre y país del estudio, los cuatro dominios y los usuarios de redes.
2. **Mientras llega el dictamen:** cuenta de desarrollador de Play, artista (encargos de §4), asesor cultural, teléfono de clase A, y decidir el espacio del AAB y el permiso `INTERNET`.
3. **Con el dictamen favorable:** activar a Codex (`codex-publicacion.md`), crear la app en Play Console, alojar la web y crear las cuentas de los SDK.
4. **Antes de la prueba cerrada:** revisión legal, curación y QA de niveles, keystore y secretos del CI.
