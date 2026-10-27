# Lanzamiento mundial de la 1.0: auditoría de supuestos latinoamericanos

- **Fecha:** 2026-10-27. Análisis del tech-lead sobre `7bc9a8fd` (rama `tanda6/mundial-auditoria`). Solo documento: no cambia código, GDD ni Remote Config.
- **Origen:** D-055, punto 5 (`docs/dev/decisiones.md`, l. 4112-4116): la 1.0 se publica **en todo el mundo**, no solo en América Latina. Hasta ahora el GDD suponía un soft launch en CO/PE/MX, después Brasil (con AR, CL y EC) y, más tarde, el resto (01 §8.1).
- **Qué no es:** no es una opinión legal. Las afirmaciones sobre leyes extranjeras (edades del RGPD art. 8, COPPA, PIPA, DPDP y demás) salen del conocimiento general del agente, **sin consulta web** (regla dura 9), y van marcadas **[NV, abogado]**. Las tiene que confirmar el abogado antes de tomarlas por buenas.
- **Cómo se lee cada punto:** **Hoy** (archivo y línea o §), **Riesgo**, **Recomendación** y **Tipo**: *código* (con el agente), *dueño* (decisión) o *abogado*.

## 0. Veredicto en cinco líneas

1. **El código ya es casi mundial.** El país sale de la SIM o de la región del teléfono, con un perfil `default` estricto para el resto del mundo. Los precios reales llegan de Play en cualquier moneda. El idioma cae a inglés. `biz_country_enabled` no lo lee ningún código.
2. **Bloquean cuatro cosas:**
   - la **política de privacidad** dice que el juego «no se dirige» a la UE y que por eso no tiene representante (§7.1);
   - con 13-15 años en los Estados del EEE con una edad digital de 14-16, **se pide el consentimiento de la analítica a quien no puede darlo** (§1.4);
   - la **ficha de Play tiene es-419 como idioma por defecto**, así que Francia o Japón la verían en español (§5.1);
   - la **lista de países de distribución** no está decidida. Rusia y Bielorrusia, como mínimo, deberían quedar fuera (§5.2).
3. **Conviene antes:** añadir el país de la cuenta de Play como fuente del país legal (§1.2), fijar el precio base en USD con la conversión de Play para el resto (§2.1), arreglar los decimales de KRW, IDR y VND (§2.3) y mostrar los países sin nombre en la Liga (§4.2).
4. **El GDD tiene que reflejar D-055:** en 01 §8.1, 05 §4.2, 05 §7, 08 §9 (`biz_country_enabled` y `biz_market_phase`) y 08 §13.1 #5, #6, #14, #20 y #22 sigue el plan solo latinoamericano.
5. **El backend en `sa-east-1` sirve para la 1.0.** Con cláusulas tipo, no hace falta otra región (§6).

---

## 1. Edad y consentimiento por país

### 1.1 Las bandas de edad y el bloqueo de menores de 13

- **Hoy:**
  - 05 §7 (fila «Menores: bandas de edad», l. 422) y 08 §2.3 (`Boot()`, l. 84-91, y el párrafo de D-033) fijan una pantalla de edad neutral con las bandas {<13, 13-17, 18+}. Lo mismo dicen 08 §7 (l. 542) y 08 §13.1 #11 (l. 951).
  - Con <13 el juego se bloquea en cada arranque y no inicia ningún SDK (D-033, añadido del 2026-10-23).
  - Con 13-17: anuncios no personalizados (`npa`), contenido G, doble confirmación de compras y sin Remote Config personalizado. La analítica **se ofrece** (Q-B12-7 (a), `decisiones.md` l. 3404).
  - El código está en `Assets/_Project/Scripts/Platform/ConsentPolicy.cs`: `Flags`, l. 141-185, con el caso `Teen` en l. 162-173.
- **Riesgo:** el umbral de 13 es el de la COPPA (EE. UU.) y el del público objetivo 13+ de Play. **Vale como mínimo mundial, pero no en todas partes** (§1.4 a §1.7). En la mayoría de los países la mayoría de edad es 18, que es la frontera de la banda 18+. Corea del Sur la pone en 19 para algunos fines [NV, abogado], pero no afecta a los anuncios ni a las compras de Play.
- **Recomendación:** mantener las tres bandas. No hace falta una pantalla nueva: lo que cambia es **qué se permite a 13-17 según el país** (§1.4).
- **Tipo:** dueño (confirmar) y abogado.

### 1.2 `ConsentCountry`: cómo se sabe el país

- **Hoy:**
  - `ConsentCountry.Resolve` (`ConsentPolicy.cs`, l. 252 y siguientes; Q-T3 (a) de D-033) toma el país de la SIM (`AndroidSimCountry`). Si no hay SIM, toma la región del idioma del teléfono. Si tampoco la hay, ninguno, y el perfil queda en `default`, el más estricto: `npa` y sin elección de anuncios (Q-B12-13).
  - El mismo país alimenta la Liga (`GameBootstrap.League.cs`, l. 111).
- **Riesgo:**
  - Una tableta sin SIM en Alemania con el teléfono en `en-US` queda como **US**, con el perfil `default`. Con 18+, la pantalla propia le ofrece los anuncios personalizados. UMP lo cubre igualmente, porque `GoogleAdService` llama a `ConsentInformation.Update` con cualquier perfil (`Ads/GoogleAdService.cs`, l. 170 y 282) y UMP decide por la IP.
  - Con 13-17 no se aplica la regla «sin anuncios en el EEE» de D-055, punto 4. Sale `npa` con `TagForUnderAgeOfConsent`, que es aceptable para Google, pero no es lo que decidió el dueño.
  - Con la analítica de 13-15 pasa lo mismo que en §1.4.
- **Recomendación:** añadir como **segunda fuente el país de la cuenta de Google Play**, por delante de la región del teléfono: `BillingClient.getBillingConfigAsync` → `countryCode`, de Play Billing Library 9.0.0, que ya está. Es el país donde el jugador paga y el que usa Play para la distribución. No es un SDK nuevo, solo una llamada más en `AfBilling.androidlib` y en `NativePlayStorePort`. Orden propuesto: SIM → cuenta de Play → región → ninguno. Hay que fijar qué pasa si el país cambia tras decidir. Q-B12-15 ya lo resuelve: con otro perfil se vuelven a pedir las finalidades.
- **Tipo:** código, **sdk-integrator** (plugin Java y puerto) y **unity-gameplay-dev** (`ConsentCountry`, tests). Antes, el dueño confirma (pregunta P4).

### 1.3 `biz_legal_profile_by_country` y el perfil `default`

- **Hoy:**
  - 08 §9 (l. 640) y `Platform/ConsentConfig.cs` (l. 39-42 y 72-84) fijan BR `br_lgpd_eca`, CO `co_1581`, PE `pe_29733` y MX `mx_lfpdppp`.
  - El EEE (30 países, l. 41-42), UK (`GB`) y CH van a `ump`. El resto (`*`) va a `default`, que también es el perfil sin país.
  - Los textos de la pantalla son los mismos para todos los perfiles (`strings.csv`, `consent_*`, l. 42-58). Solo cambia el renglón de los anuncios (`consent_ads_ump` frente a la elección propia).
- **Riesgo:**
  - **`default` ya es razonable para el mundo.** Es consentimiento previo (*opt-in*) para la analítica y los anuncios personalizados, con «Rechazar todo» al mismo nivel que «Aceptar» y la revocación en Ajustes. Es más estricto que lo que piden la CCPA en EE. UU., la APPI en Japón o la PIPEDA en Canadá [NV, abogado].
  - Lo que `default` no hace:
    - (a) distinguir los países donde la edad digital es mayor de 13 (§1.4);
    - (b) mostrar el aviso «no vender ni compartir» de las leyes estatales de EE. UU., que 05 §7 menciona (l. 419) y el código no implementa (§1.6);
    - (c) cubrir a Corea y a la India (§1.7).
- **Recomendación:** mantener `default` para el resto del mundo. No crear un perfil por país. Lo que varía por país es la **edad digital**, y va en un mapa propio (§1.4).
- **Tipo:** abogado (validar `default` como tratamiento mundial) y dueño.

### 1.4 RGPD art. 8: 13-15 años en Alemania, los Países Bajos y otros

- **Hoy:**
  - Con el perfil `ump` y 13-17, **sin anuncios** (D-055, punto 4; Q-MAX-6 (b)). Lo hacen `Ads/AdConsentSettings.cs` (l. 87-90) y `GoogleAdService.cs` (l. 261).
  - **La analítica sí se ofrece** y, si el menor la acepta, Firebase Analytics recoge con el consentimiento como base (Q-B12-7 (a); `ConsentPolicy.cs`, l. 162-173: `analytics: decided && record.Analytics`).
- **Riesgo (alto):** el art. 8 del RGPD solo admite el consentimiento propio de un menor desde la edad que fije cada Estado, entre 13 y 16. Por debajo hace falta la autorización de los padres. Edades orientativas [NV, abogado]:

  | Edad | Estados |
  |---|---|
  | 13 | BE, DK, EE, FI, LV, MT, PT, SE; NO e IS (EEE); UK (UK GDPR) |
  | 14 | AT, BG, CY, ES, IT, LT |
  | 15 | CZ, FR, GR, SI |
  | 16 | DE, HR, HU, IE, LI, LU, NL, PL, RO, SK |

  - Suiza (nLPD) no fija una edad: se mira la capacidad de discernimiento, que suele situarse entre 13 y 16 [NV, abogado].
  - Un jugador de 14 años en Alemania o en los Países Bajos puede hoy pulsar «Aceptar todo» y activar la analítica: es **un consentimiento que no vale**.
  - Como la banda 13-17 no distingue 13-15 de 16-17, el juego no puede saber si el consentimiento es válido.
  - Crashlytics («siempre activo», `consent_diagnostics`), la cuenta anónima, la nube y la Liga no se basan en el consentimiento, sino en el interés legítimo o en el contrato. El art. 8 no les aplica, pero el abogado debe confirmar esas bases, y la ePrivacy para Crashlytics.
- **Recomendación:** opción (a) de la pregunta P2.
  - Una clave de Remote Config nueva, `consent_digital_age_by_country` (map país→int: default `*` = 13 y el EEE con los valores de la tabla, que confirma el abogado).
  - La regla: **si la banda es 13-17 y la edad digital del país es mayor de 13, la analítica no se ofrece** (queda apagada, como los anuncios personalizados). Las compras siguen con la doble confirmación.
  - Así no hace falta una banda nueva en la pantalla.
  - Mientras no haya tabla del abogado, el default prudente es 16 para todo el EEE y CH, y 13 para UK.
  - Los anuncios de 13-17 en el EEE, UK y CH siguen apagados (D-055).
- **Tipo:**
  - dueño: la clave nueva va en 08 §9, la tabla única, y la regla en 05 §7;
  - código, **unity-gameplay-dev**: `ConsentConfig`, `ConsentPolicy`, `ConsentScreen` y tests; y **level-pipeline-dev** o quien mantenga `tools/remote-config/rc_defaults.py` para regenerar los defaults;
  - abogado: la tabla.

### 1.5 UMP en el EEE, el Reino Unido y Suiza

- **Hoy:**
  - AdMob directo con UMP 4.0.0 (D-049; 08 §13.3, l. 979). `NativeGmaPort` llama a `ConsentInformation.Update` con `TagForUnderAgeOfConsent` cuando la banda es 13-17 (`Ads/Google/NativeGmaPort.cs`, l. 43-51).
  - Con 18+ y el perfil `ump`, la personalización la deciden las cadenas TCF (`AdConsentSettings.UmpDecides`).
  - 05 §7 (l. 419) aún habla de «la automatización de AppLovin MAX», pero D-049 ya la sustituyó.
- **Riesgo:**
  - Con lanzamiento mundial, el mensaje GDPR de AdMob › Privacidad y mensajes **tiene que estar publicado** antes de servir anuncios en el EEE, UK y CH. Si no, Google sirve anuncios limitados o ninguno.
  - Suiza y UK tienen que estar marcados en el mensaje.
  - El texto de 05 §7 está desactualizado.
- **Recomendación:**
  - el dueño configura en AdMob el mensaje del RGPD (EEE, UK y CH) y, si se quiere, el de las leyes estatales de EE. UU. (§1.6);
  - 05 §7 se corrige a «UMP de Google Mobile Ads (D-049)».
- **Tipo:** dueño (consola de AdMob) y GDD (integrador).

### 1.6 EE. UU.: COPPA y leyes estatales

- **Hoy:**
  - <13 se bloquea (COPPA). En el primer arranque, **antes de la pantalla de edad**, ya corren Crashlytics, la descarga de Remote Config y Firebase con Analytics apagado (08 §2.3, l. 79-82).
  - 05 §7 dice «opción "no vender/compartir" para EE. UU.» (l. 419), pero no hay código que la implemente.
  - `docs/legal/inventario-web.md` (l. 103) deja la COPPA y los derechos estatales para el abogado.
- **Riesgo [NV, abogado]:**
  - **COPPA:** el juego no se declara dirigido a niños y tiene una pantalla de edad neutral. Si la FTC lo viera como de «audiencia mixta» (frutas, TiTino, colores vivos), los identificadores persistentes recogidos antes de la pantalla solo estarían permitidos con la excepción de «soporte de operaciones internas». Crashlytics y Remote Config encajan en esa excepción. Firebase Installations hay que confirmarlo.
  - La regla COPPA enmendada (en vigor en 2025, con cumplimiento en 2026) endurece la publicidad dirigida a niños, pero a <13 no se le sirve nada.
  - **Leyes estatales** (CCPA/CPRA y similares):
    - con 18+, `default` ya pide *opt-in* para los anuncios personalizados, más que el *opt-out* exigido, aunque falta en la política el aviso «no vendemos ni compartimos» y el respeto de la señal GPC;
    - con 13-15, la CCPA exige *opt-in* para vender o compartir, y hoy hay `npa` siempre.
  - **Leyes de rendición de cuentas de las tiendas** (Utah, Texas, Luisiana, 2026): obligan a los desarrolladores a leer la señal de edad de la tienda y a pedir el consentimiento parental de los menores. Algunas están recurridas. Es la razón de ser de **Play Age Signals API**, que el GDD ya reserva (`consent_age_signal_source` = `self_declared+play_age_signals`, 08 §9; 05 §7, l. 423).
- **Recomendación:**
  - el abogado confirma la excepción de operaciones internas y la necesidad de Age Signals en EE. UU. en 2026;
  - si hace falta, integrarla (nueva dependencia de Play: aprobación del dueño por la regla dura 4);
  - la política añade el aviso de EE. UU.;
  - el mensaje de leyes estatales de EE. UU. de UMP es opcional y no hace falta si el *opt-in* propio basta.
- **Tipo:** abogado; dueño (Age Signals y el mensaje de EE. UU.); código, **sdk-integrator** (solo si se aprueba).

### 1.7 Brasil, Corea, China, India y el resto

| País o zona | Hoy | Riesgo [NV, abogado] | Recomendación | Tipo |
|---|---|---|---|---|
| **Brasil** (LGPD, ECA Digital) | Perfil propio `br_lgpd_eca`. Consulta del art. 8 del ECA Digital pendiente en la compuerta de la Fase 5 (`pendientes-del-dueno.md`, l. 143; 09 R11) | Con el lanzamiento mundial, Brasil entra el **día 1** y no en la fase 2. Se adelanta la consulta. El ECA Digital (Lei 15.211/2025) entra en vigor en 2026 | Adelantar la consulta del abogado brasileño (verificación de edad por dos fuentes, loot boxes y compras de adolescentes) a antes de la 1.0 | abogado |
| **Corea del Sur** (PIPA) | `default` | Por debajo de 14 años, el tratamiento con consentimiento exige al representante legal. Un jugador de 13 entra en la banda 13-17 y puede aceptar la analítica | Con la regla de §1.4, `KR` = 14 en `consent_digital_age_by_country`. El abogado mira si hace falta un agente local (ley de la industria del juego) por volumen | abogado y código (§1.4) |
| **China** | — | Google Play no opera, así que no hay distribución ni pagos | Nada | — |
| **India** (DPDP 2023 y reglas de 2025) | `default` | Niño es <18. Exige el consentimiento parental verificable y prohíbe el seguimiento y la publicidad dirigida a niños. Las obligaciones entran por fases hasta 2027 | Con la regla de §1.4, `IN` = 18: con 13-17, sin analítica y anuncios `npa`. El abogado decide si 13-17 en la India también va sin anuncios, como el EEE | abogado y dueño |
| **Reino Unido** (Children's Code de la ICO) | `ump`, sin anuncios con 13-17 | Exige una evaluación de impacto, privacidad alta por defecto con <18 y nada de técnicas que empujen | Valorar si «Aceptar todo» junto a «Rechazar todo» basta con 13-17. Con §1.4, el problema de la analítica de los menores en UK se reduce | abogado |
| **Canadá** (PIPEDA; Ley 25 de Quebec) | `default` | En Quebec, con <14, consentimiento parental | `CA` no se puede partir por provincia: el abogado decide si `CA` = 14 | abogado |
| **Australia** | `default` | Código de privacidad infantil en preparación (2026) | Puede esperar | abogado |
| **Japón, Turquía, Israel, Sudáfrica y el resto** | `default` | Sin un conflicto conocido con el *opt-in* de `default` | Nada para la 1.0 | — |

### 1.8 Edad y compras

- **Hoy:** con 13-17, doble confirmación en el juego más la autenticación de compras de Google Play (Q-T4 (a), Q-B12-6 (a); 05 §7, l. 422 y 428).
- **Riesgo:** la capacidad de contratar de un menor depende del derecho civil de cada país [NV, abogado]. En la UE, Google Play hace de vendedor o de comisionista y gestiona el desistimiento.
- **Recomendación:** que el abogado confirme que basta para el mundo. No hay que cambiar el código.
- **Tipo:** abogado.

---

## 2. Precios y monedas

### 2.1 La tabla de 05 §4.2 y Play Console

- **Hoy:**
  - 05 §4.2 (l. 311-337): ancla en USD y 8 columnas locales (BRL, MXN, COP, PEN, ARS, CLP, EC y EUR), todas a mano.
  - 08 §13.1 #14 (l. 954): «precios manuales por país para BR, MX, CO, PE, AR, CL, EC».
  - 05 §4.2 afirma que las plantillas de precios desaparecieron el 27-10-2025 [V del informe; NV hoy].
- **Riesgo:** en el resto del mundo (unos 170 países) no hay precio decidido. Fijarlo a mano país a país no escala. Si un producto no tiene precio en un país, Play no lo vende allí.
- **Recomendación:** opción (a) de P5.
  1. Precio base en **USD** con los escalones de 05 §4.1.
  2. **Conversión automática de Play** al resto de países, con sus redondeos locales.
  3. **Sobrescribir a mano** solo las columnas de 05 §4.2: LatAm al ≈ 50 % y EUR en paridad.
  4. Revisar a los 30 días los países con más instalaciones (India, Indonesia, Turquía, Filipinas…) por si conviene bajar el precio por poder adquisitivo, como en LatAm.
  - 05 §4.2 y 08 §13.1 #14 se actualizan con esta regla.
- **Tipo:** dueño (Play Console y la decisión) y GDD.

### 2.2 `LocalPrice` y `LocPrice` con monedas fuera de la lista

- **Hoy:**
  - `IAP/StoreCatalog.cs` (l. 94-106): `StorePrices.Currencies` = USD, BRL, MXN, COP y PEN. Solo es la tienda simulada.
  - `LocalPrice` admite cualquier código ISO 4217 (l. 17-23; Q-IAP-3).
  - `PlayBillingClient` compone el precio desde los micros y la moneda de Play (D-025, punto 3). Si un producto llega sin precio, **no se vende** (`PlayBillingClient.cs`, l. 258-272; `BillingReadyStoreService`).
  - La moneda de respaldo es COP (`UI/LaunchOptions.cs`, l. 343) y solo se ve en la tienda simulada.
- **Riesgo:** bajo. Un jugador de Japón ve «JPY 160», en el formato de D-025, y no puede comprar con un precio inventado.
- **Recomendación:** nada que bloquee. Más adelante, la moneda de la tienda simulada podría seguir a la región; solo afecta al desarrollo.
- **Tipo:** —

### 2.3 Los decimales de las monedas

- **Hoy:** `Localization/LocPrice.cs` (`DecimalsOf`, l. 32-46) pone 0 decimales en COP, MXN, CLP y JPY, 3 en KWD y BHD, y 2 en el resto.
- **Riesgo:** con la conversión automática de Play, KRW, VND, IDR, ISK, PYG, UGX, HUF o TWD salen como «KRW 6,500.00» o «IDR 79,000.00». Es feo y no se parece a la hoja de Google Play. No se cobra mal, porque el importe es el de Play.
- **Recomendación:** ampliar `DecimalsOf` a las monedas con 0 decimales en ISO 4217 que Play usa (KRW, VND, ISK, PYG, UGX, XAF, XOF, RWF y demás) y a las que Play escribe sin decimales (IDR, HUF y TWD; confirmar en una compra de prueba [NV]), con tests. Es un añadido de D-025 sin decisión nueva: D-025 ya prevé «los decimales de la moneda».
- **Tipo:** código, **unity-gameplay-dev** (`AF.Localization`, tests de `LocPrice`).

### 2.4 `GeoTierRule`: T1 o T3

- **Hoy:** `UI/GeoTierRule.cs` (l. 22-26) pone en T1 a US, CA, AU, JP, KR y la Europa occidental [P]. **Todo lo demás es T3.** Solo cambia el orden Balsa/Puñado del modal sin saldo (05 §6.3) y la propiedad `geo_tier` de la analítica.
- **Riesgo:** bajo. NZ, SG, HK, TW, IL, AE, SA, QA, KW y la Europa del este caen en T3: verán el Puñado primero y quedarán mezclados en la analítica con LatAm.
- **Recomendación:**
  - añadir a T1, como [P], NZ, SG, HK, TW, IL y los países del Golfo (AE, SA, QA, KW, BH);
  - leer el país de la cuenta de Play (§1.2) en lugar de la región del idioma, como ya prevé Q-E3 (a);
  - no hace falta otra banda: para analizar por país, Firebase ya da el país por IP.
- **Tipo:** código, **unity-gameplay-dev**. Puede esperar.

### 2.5 Impuestos y cobros

- **Hoy:** 05 §4.2 (l. 337) solo habla de Brasil: IRRF + CIDE ≈ 25 %, «confirmar con contador».
- **Riesgo:** Parallext, LLC (Wyoming) cobra de Google en todo el mundo. Google retiene o liquida el IVA en la mayoría de los países, pero hay retenciones y formularios fiscales (W-8/W-9 y tratados) que dependen del país [NV, contador].
- **Recomendación:** consulta al contador antes de la 1.0.
- **Tipo:** dueño (contador).

---

## 3. Idiomas

### 3.1 Qué ve un jugador de Francia o de Japón

- **Hoy:**
  - `LocLocale.Resolve` (`Localization/LocLocale.cs`, l. 195-222) prueba, en orden: lo elegido en desarrollo, lo guardado (`settings.language`), el idioma del teléfono, inglés y, por último, es-419.
  - `Map` (l. 116-140): `es-*` → es-419, `pt-*` → pt-BR y cualquier otro → **en** (D-018, Q7-A).
  - Las tablas admiten fr, de, it, nl, tr, ja, ko y ru (l. 77-80), pero sin texto. Un idioma vacío no está completo y no se ofrece.
  - Las cifras usan la tabla propia de `LocNumber`, con una fila de respaldo para los idiomas desconocidos.
  - Con D-054, LocReleaseGate queda a 0: el inglés está completo.
- **Riesgo:**
  - Bajo en la UI: un teléfono en francés o en japonés ve **inglés**. Como no hay texto en japonés, tampoco hacen falta fuentes CJK. El español de España ve es-419 y el portugués de Portugal ve pt-BR, que es aceptable.
  - Todo lo que sale en inglés tiene que ser en-US neutral y estar revisado. Hoy los textos en EN son traducción con IA pendiente del revisor nativo (`docs/store-listing/README.md`, l. 8; D-054 revisó con Gemini).
- **Recomendación:**
  - mantener los tres idiomas (D-055);
  - **la revisión nativa del inglés pasa a ser compuerta de release**, porque es el idioma de la mayoría del mundo;
  - FallbackLocale = es-419 solo se usa si el inglés está incompleto, y LocReleaseGate ya lo impide.
- **Tipo:** dueño (revisor nativo de EN).

### 3.2 D-018, tanda 2, frente a D-055

- **Hoy:** D-018 (`decisiones.md`, l. 1971) programa la «tanda 2» (FR, DE, IT, NL, TR, JA, KO y RU) «antes de abrir Europa y Asia», con un coste de ≈ 3.500-8.600 €, «a decidir con el lanzamiento mundial». D-055 dice: «se mantienen es-419, en y pt-BR».
- **Riesgo:** dos decisiones que se contradicen sobre el momento.
- **Recomendación:** registrar que **D-055 aplaza la tanda 2 a después de la 1.0**, guiada por `device_language` (Q11-A) con el criterio de D-018 (≈ 3 % de los usuarios activos o de los ingresos durante 4 semanas). Pregunta P6.
- **Tipo:** dueño.

---

## 4. Contenido, analítica y Remote Config

### 4.1 Datos del río, Mapa y Frutas

- **Hoy:** `Data/Loc/facts.csv` (823 líneas) es contenido educativo sobre la Amazonia: países amazónicos (`map.country.*`), paradas y frutas. Usa nombres locales y menciona Colombia y Perú (por ejemplo, l. 119, 367 y 375).
- **Riesgo:** ninguno. El tema del juego es la Amazonia, no su público. Los textos no dan por hecho que el lector viva en LatAm (no se encontró «tu país» ni «nuestro país»). La hora local se usa bien: `SystemClock.LocalOffset` y la semana de la Liga en UTC, sin zonas horarias de LatAm.
- **Recomendación:** nada. El asesor cultural sigue revisando lo de siempre.
- **Tipo:** —

### 4.2 La Liga del río fuera de LatAm

- **Hoy:**
  - La Liga toma el país de `ConsentCountry`. El backend (`infra/functions/league/rules.ts`, l. 7 y 85) admite cualquier ISO alfa-2.
  - La UI tiene nombre para 21 países (`strings.csv`, `league_country_*`). El resto sale como «Otro país» (`league_country_other`, l. 177), en las filas, **en la pestaña del país** y en Ajustes (`UI/LeagueText.cs`, l. 106-110).
  - Las banderas dibujadas cubren los países amazónicos y los «mercados principales». Para el resto, una placa con el código (`UI/LeagueFlag.cs`, l. 11-17).
- **Riesgo:** un jugador de Japón ve la pestaña «Other country» y «League country: Other country» en Ajustes. Es confuso y parece un error.
- **Recomendación:**
  - fuera de la tabla, mostrar el **código ISO** en la pestaña y en Ajustes, o un nombre de país de una tabla ISO completa en los tres idiomas (≈ 250 filas × 3, generables desde CLDR);
  - fuera de LatAm conviene añadir al menos IT, NL, JP, KR, IN, ID, PH, TR y AU, o ir directamente a la tabla completa;
  - las banderas siguen como placa neutra.
- **Tipo:** dueño (P7), y código, **unity-gameplay-dev**, con la tabla generada por herramienta (nunca a mano en `strings.csv`; regla de localización).

### 4.3 Analítica: el país y el `geo_tier`

- **Hoy:**
  - Entre los comunes de 08 §8.1 (l. 549) **no hay `country`**: va `geo_tier` (`Analytics/AnalyticsCatalog.cs`, l. 51; `CommonParams.cs`, l. 88).
  - Las propiedades de usuario incluyen `geo_tier` y `consent_profile` (l. 76).
  - El perfil guarda `country_first` (`Save/Profile.cs`, l. 213).
  - Firebase Analytics añade por sí mismo el país, deducido de la IP.
- **Riesgo:** bajo. Con T1/T3 los análisis por país dependen de la dimensión automática de GA4, que basta.
- **Recomendación:** no añadir un común `country`: lo da GA4, y añadirlo aumentaría los datos declarados. Opcional: mandar `consent_profile` ya permite separar el EEE.
- **Tipo:** —

### 4.4 Remote Config por país

- **Hoy:**
  - `biz_country_enabled` (08 §9, l. 634; `RemoteConfigDefaults.Generated.cs`, l. 36-43) tiene por defecto CO, PE y MX = true y el resto = false. `biz_market_phase` = `closed_beta`.
  - **Ningún código las lee** (solo están en los defaults generados), igual que `biz_ua_paid_enabled_by_country` y `eco_price_tier_<country>`.
- **Riesgo:**
  - No hay un riesgo funcional.
  - Sí uno documental: 08 §9 y 08 §13.1 #22 dicen que la producción va «según `biz_country_enabled`», y alguien podría implementarlo más adelante con el default restrictivo. Eso apagaría el juego fuera de CO, PE y MX.
- **Recomendación:**
  - cambiar en 08 §9 el default a `{"*": true}` y `biz_market_phase` a `global` para la 1.0;
  - regenerar con `tools/remote-config/rc_defaults.py`;
  - o bien declarar en 08 §9 que las dos son solo de registro;
  - las condiciones de Firebase por país siguen sirviendo para ajustes puntuales.
- **Tipo:** dueño (los defaults de 08 §9 son canon) y código, **level-pipeline-dev** (regenerar y `--check`).

---

## 5. Tienda y publicación

### 5.1 Las fichas de Play y el idioma por defecto

- **Hoy:**
  - `docs/store-listing/README.md` (l. 8): «es-419 es el predeterminado».
  - 08 §13.1 #5 (l. 945): capturas en es-419, pt-BR y en.
  - 08 §13.1 #6: fichas personalizadas para BR (Pix) y MX (OXXO).
- **Riesgo (alto):** Play muestra la ficha del idioma por defecto a quien tiene un idioma sin traducción. **Francia, Alemania, Japón, India y los demás verían la ficha en español.** Los hablantes de es-ES y pt-PT, a su vez, ven la ficha por defecto si no hay traducción para su variante.
- **Recomendación:**
  - **idioma por defecto de la ficha = en-US**;
  - es-419 y pt-BR como traducciones, más **es-ES** (copia de es-419) y **pt-PT** (copia de pt-BR) si Play los separa;
  - en-GB, en-AU y demás no hacen falta (caen en en-US);
  - las fichas personalizadas BR y MX se quedan;
  - la URL de la política que se declara en Play pasa a ser la **inglesa** (`/en/privacy/`) o una raíz que elija el idioma.
- **Tipo:** dueño (Play Console) y GDD (08 §13.1 #5 y #7).

### 5.2 Países de distribución

- **Hoy:**
  - 08 §13.1 #20 (l. 960): prueba cerrada con testers de CO, PE y MX.
  - 08 §13.1 #22 (l. 962): «5 % → 20 % → 100 % en CO/PE/MX».
  - 01 §8.1 (l. 219-225): cuatro fases por países.
- **Riesgo [NV, abogado o dueño]:**
  - **Rusia y Bielorrusia:** no hay cobros de Play ni anuncios de Google (D-018 ya lo recoge). La ley de localización de datos rusa exige guardar en Rusia los datos de sus ciudadanos. Además hay sanciones.
  - **Vietnam:** los juegos con compras pueden necesitar una licencia local.
  - **Indonesia:** registro de proveedor PSE para servicios extranjeros.
  - Play ya excluye los países embargados.
- **Recomendación:**
  - opción (a) de P3: **todos los países de Play salvo RU y BY**, con VN e ID pendientes de que el abogado diga si hay que excluirlos para la 1.0;
  - producción escalonada por **porcentaje mundial** (5 % → 20 % → 100 %) en lugar de por países;
  - prueba cerrada con testers de LatAm y de, al menos, un país del EEE (para ver UMP de verdad);
  - 01 §8.1 y 08 §13.1 #20 y #22 se reescriben.
- **Tipo:** dueño y abogado.

### 5.3 Clasificación IARC mundial

- **Hoy:**
  - 08 §13.1 #10 (l. 950): publicidad sí, compras sí, ítems aleatorios no y azar simulado no. Dice «interacción de usuarios **no** (1.0)», pero `docs/store-listing/iarc.md` (l. 34-35) ya responde **sí** por la Liga y «comparte la ubicación a nivel de país», ambos provisionales.
  - 05 §7 (l. 426) espera ClassInd 14 y Everyone o Teen.
- **Riesgo:**
  - El cuestionario IARC es uno solo y da a la vez ESRB, PEGI, USK, ClassInd, ACB, GRAC y la clasificación genérica. **No cambia con el lanzamiento mundial**, pero las respuestas sobre la Liga suben la clasificación en PEGI y USK. El texto de 08 §13.1 #10 está desactualizado frente a iarc.md.
  - Japón no está en IARC y usa la clasificación genérica de Play [NV].
- **Recomendación:** responder lo de iarc.md, corregir 08 §13.1 #10 y revisar la ficha de Alemania (USK) cuando llegue el certificado.
- **Tipo:** dueño (cuestionario), GDD y abogado (alcance de la ubicación).

### 5.4 Data Safety

- **Hoy:** una sola declaración mundial (08 §13.3; `docs/store-listing/data-safety.md`). D-055, punto 3, añade `AD_ID`.
- **Riesgo:** ninguno nuevo por ser mundial: Data Safety ya es global.
- **Recomendación:** la que ya está en marcha (AD_ID). Si sale adelante la regla de §1.4, el texto no cambia: es la misma finalidad, que se pide a menos jugadores.
- **Tipo:** —

### 5.5 Consumo en la UE: monedas virtuales y ofertas con tiempo

- **Hoy:**
  - La Mochila de inicio tiene un temporizador de 48 h (05 §4.1, l. 294).
  - La Balsa «vende el momento» en Aguas estancadas (05 §4.1).
  - Los precios dentro del juego van en semillas.
- **Riesgo [NV, abogado]:** las autoridades de consumo de la UE (red CPC) publicaron en 2025 unos principios para las monedas virtuales en los juegos:
  - mostrar el equivalente en dinero real;
  - no presionar con plazos;
  - cuidado especial con los menores.

  No son una ley, pero guían las inspecciones. También la ASA y la CMA del Reino Unido tienen guías sobre las compras en el juego.
- **Recomendación:** que el abogado valore los principios CPC frente al temporizador de la Mochila, el disparador de la Balsa y el precio en semillas del rescate. Si hace falta, ajustar solo para el EEE y UK con Remote Config (por ejemplo, `eco_starter_*`).
- **Tipo:** abogado; después, dueño.

### 5.6 Accesibilidad (Acta Europea de Accesibilidad)

- **Hoy:** TalkBack en listas (Q-B9-17).
- **Riesgo:** la norma afecta al comercio electrónico, pero las microempresas están exentas [NV, abogado].
- **Recomendación:** puede esperar.
- **Tipo:** abogado.

---

## 6. Backend y ubicación de los datos

### 6.1 AWS en `sa-east-1`

- **Hoy:**
  - `infra/lib/config.ts` (l. 39 y 243): región `sa-east-1`. Lo global (ACM, WAF y Budgets) va en `us-east-1` (Q-AWS-5).
  - API Gateway regional, Lambdas y DynamoDB. La web y el CDN de `levels_remote` van en CloudFront, que es mundial.
  - El cliente espera 15 s (`Backend/HttpBackendClient.cs`, l. 28).
- **Riesgo:**
  - **Latencia:** desde Europa, unos 200 ms de ida y vuelta, y desde Asia, más de 300 ms [E]. Ninguna llamada del backend está en el bucle de juego (verificar compra, SSV, restaurar, Liga y borrado), así que no se nota. La web y los niveles van por CloudFront.
  - **Residencia de datos:** el RGPD no exige guardar los datos en la UE. Exige una base para la transferencia (cap. V). Brasil no tiene decisión de adecuación de la UE en vigor [NV], así que hacen falta las **cláusulas contractuales tipo**, que el DPA de AWS incluye [NV, abogado], y una evaluación de la transferencia. La política ya tiene la sección «International transfers» con `[[ABOGADO]]`.
  - **Coste y escala:** DynamoDB bajo demanda escala solo.
- **Recomendación:**
  - **no hace falta otra región para la 1.0**;
  - revisarlo si la Liga o el volumen de Europa o Asia lo piden (tablas globales de DynamoDB) o si el abogado exige residencia por algún país concreto (Rusia ya queda fuera, §5.2).
- **Tipo:** abogado (base de la transferencia) y dueño (confirmar que no hay segunda región, P8).

### 6.2 Firebase

- **Hoy:** Analytics, Crashlytics, Remote Config, Auth anónimo y App Check (08 §13.3). Sin Firestore ni Functions (D-035).
- **Riesgo:** esos servicios guardan y tratan los datos sobre todo en EE. UU. y no tienen una opción de región que elegir [NV]. Las transferencias de la UE a EE. UU. se apoyan en el Marco de Privacidad de Datos UE-EE. UU. (Google está adherido) y en las cláusulas de los términos de Firebase [NV, abogado].
- **Recomendación:**
  - nada en el código;
  - el abogado lo recoge en la sección de transferencias;
  - fijar la retención de Analytics (hoy `[[DATO DEL DUEÑO]]`, Q-B13-19) en 2 meses, el mínimo, si se quiere minimizar.
- **Tipo:** abogado y dueño.

---

## 7. Textos legales (web)

### 7.1 Representante en la UE y en el Reino Unido

- **Hoy:** `infra/web/src/content/{en,es-419,pt-BR}/privacy.html`, l. 11: «Representative: none in the European Union, because the game is not directed to it … [[ABOGADO: revisar si hace falta representante]]».
- **Riesgo (alto):** con el lanzamiento mundial el juego **sí se dirige** a la UE: ficha, precios en EUR y UMP. Un responsable fuera de la UE que ofrece servicios a personas de la UE tiene que nombrar un representante (RGPD art. 27), salvo un tratamiento ocasional y de bajo riesgo, que es discutible con analítica, anuncios y menores. El Reino Unido (UK GDPR art. 27) y Suiza (nLPD art. 14) tienen reglas parecidas [NV, abogado]. La frase de hoy, además, **pasa a ser falsa**.
- **Recomendación:**
  - quitar la justificación ya;
  - el abogado decide si hacen falta representantes en la UE, UK y CH (hay servicios de representación de bajo coste);
  - la política añade la autoridad de control y los derechos del RGPD.
- **Tipo:** abogado; la edición de la web es código, **sdk-integrator** o el integrador, con el texto del abogado.

### 7.2 Anexos por país

- **Hoy:** la política tiene el anexo A (aviso simplificado de México) y el anexo B (Brasil, LGPD).
- **Riesgo:** faltan:
  - lo del EEE, UK y CH: bases legales, derechos, autoridad y edad digital;
  - EE. UU.: el aviso de las leyes estatales («no vendemos ni compartimos», GPC) y la COPPA;
  - Corea y la India, si el abogado lo pide.

  Los términos de uso tienen que fijar la ley aplicable y respetar los derechos de los consumidores locales.
- **Recomendación:** que el abogado redacte un **texto legal mundial** con anexos regionales (D-055 ya lo prevé). Se añade al paquete del abogado (`docs/legal/paquete-abogado.md` §2).
- **Tipo:** abogado.

---

## 8. Lo que es seguro tal cual

- **El país legal y el perfil:** `ConsentCountry` y `ConsentConfig.ProfileFor` funcionan con cualquier país. El EEE completo, UK y CH van a `ump`; el resto, a `default`, con *opt-in*; sin país, al tratamiento más estricto.
- **Menores de 13:** bloqueo sin SDK en cualquier país.
- **13-17 en el EEE, UK y CH:** sin anuncios (D-055, punto 4), ya implementado para Google y MAX.
- **Anuncios:** contenido G en todas las bandas. UMP se consulta con cualquier perfil, así que el mensaje del RGPD llega aunque la SIM no sea europea.
- **Precios reales:** en cualquier moneda, desde Play. Sin precio de Play no se vende.
- **Idioma:** cualquier idioma no soportado cae a inglés completo. Las cifras usan la tabla propia con su fila de respaldo, y la hora local se respeta.
- **Backend:** admite cualquier país (Liga, libro mayor con `regionCode`, borrado) y espera 15 s, lo que basta desde Asia.
- **Remote Config:** ningún código lee `biz_country_enabled` ni `biz_market_phase`: hoy no limitan nada.
- **Contenido:** educativo sobre la Amazonia y válido para un público mundial. Sin compras aleatorias: no hay problemas de loot boxes en BE, NL, BR ni KR.
- **Data Safety:** ya es una declaración mundial.

---

## 9. Tabla priorizada

| # | Punto | § | Tipo | Quién | Prioridad |
|---|---|---|---|---|---|
| 1 | Política: quitar «not directed to the EU» y decidir los representantes UE/UK/CH | 7.1 | abogado + web | abogado; integrador | **Bloquea** |
| 2 | 13-17 con edad digital > 13: sin analítica (`consent_digital_age_by_country`) | 1.4, 1.7 | dueño + código + abogado | unity-gameplay-dev; RC: level-pipeline-dev | **Bloquea** |
| 3 | Idioma por defecto de la ficha = en-US; es-ES y pt-PT; URL de la política en inglés | 5.1 | dueño | dueño (Play Console) | **Bloquea** |
| 4 | Países de distribución (sin RU ni BY; VN e ID según el abogado) y producción escalonada mundial | 5.2 | dueño + abogado | dueño | **Bloquea** |
| 5 | Mensaje del RGPD de UMP publicado en AdMob (EEE, UK y CH) | 1.5 | dueño | dueño (AdMob) | **Bloquea** (sin él no hay anuncios en Europa) |
| 6 | Revisión nativa del inglés como compuerta de release | 3.1 | dueño | revisor | **Bloquea** |
| 7 | Consulta del ECA Digital (Brasil) adelantada a antes de la 1.0 | 1.7 | abogado | abogado | **Bloquea** (Brasil entra el día 1) |
| 8 | Precio base USD con conversión de Play; sobrescribir LatAm y EUR | 2.1 | dueño | dueño | Conviene antes |
| 9 | País de la cuenta de Play como segunda fuente de `ConsentCountry` | 1.2 | código | sdk-integrator + unity-gameplay-dev | Conviene antes |
| 10 | Decimales de KRW, IDR, VND y demás en `LocPrice.DecimalsOf` | 2.3 | código | unity-gameplay-dev | Conviene antes |
| 11 | Liga: países sin nombre (código ISO o tabla completa) | 4.2 | dueño + código | unity-gameplay-dev | Conviene antes |
| 12 | COPPA: confirmar «operaciones internas» antes de la pantalla de edad; Play Age Signals en EE. UU. | 1.6 | abogado (+ dueño si hay SDK) | abogado; sdk-integrator | Conviene antes |
| 13 | Aviso de las leyes estatales de EE. UU. y anexos EEE/UK/CH en la política y los términos | 7.2, 1.6 | abogado | abogado | Conviene antes |
| 14 | Principios CPC (monedas virtuales y plazos) en el EEE | 5.5 | abogado | abogado | Conviene antes |
| 15 | GDD: 01 §8.1, 05 §4.2 y §7 (UMP de GMA), 08 §9 (`biz_country_enabled` y `biz_market_phase`) y 08 §13.1 #5, #6, #10, #14, #20 y #22 | varios | GDD | integrador | Conviene antes |
| 16 | Transferencias UE→Brasil (AWS) y UE→EE. UU. (Firebase) en la política | 6 | abogado | abogado | Conviene antes |
| 17 | Contador: impuestos y retenciones de una LLC con ventas mundiales | 2.5 | dueño | contador | Conviene antes |
| 18 | `GeoTierRule`: más países en T1 y el país de Play | 2.4 | código | unity-gameplay-dev | Puede esperar |
| 19 | Tanda 2 de idiomas tras la 1.0 según `device_language` | 3.2 | dueño | — | Puede esperar |
| 20 | Segunda región de AWS | 6.1 | dueño | — | Puede esperar |
| 21 | Acta Europea de Accesibilidad; código infantil de Australia | 5.6, 1.7 | abogado | — | Puede esperar |
| 22 | Matriz de dispositivos de 08 §14.1 ampliada fuera de LatAm | — | QA | — | Puede esperar (el informe previo de Play cubre lo básico) |

---

## 10. Preguntas para el dueño (cada una con la opción recomendada)

- **P1. ¿El lanzamiento mundial sustituye a las fases de 01 §8.1 (soft launch LatAm → Brasil → global)?**
  - **(a) Recomendada:** sí. Una sola salida mundial con producción escalonada por porcentaje (5 % → 20 % → 100 %) y lectura de D1/D7 por cohorte y país en Firebase.
  - (b) Soft launch corto en LatAm antes de abrir el resto.
- **P2. ¿Cómo se trata a 13-17 donde la edad de consentimiento digital es mayor de 13 (gran parte del EEE, CH, KR e IN)?**
  - **(a) Recomendada:** una clave nueva `consent_digital_age_by_country` (08 §9). Con 13-17 y una edad mayor de 13, la analítica no se ofrece. Default prudente hasta el abogado: EEE y CH = 16, UK = 13, KR = 14, IN = 18 y el resto = 13.
  - (b) Sin analítica para 13-17 en todo el mundo salvo BR, CO, PE y MX.
  - (c) Partir la pantalla en 13-15 y 16-17. Cambia la pantalla neutral y no lo recomiendo.
- **P3. ¿En qué países se distribuye la 1.0?**
  - **(a) Recomendada:** todos los de Play salvo Rusia y Bielorrusia. Vietnam e Indonesia dependen de lo que diga el abogado.
  - (b) Solo América más el EEE, UK, CH, AU, NZ, JP y KR en la 1.0, y Asia y África después.
- **P4. ¿Se añade el país de la cuenta de Google Play (Play Billing `getBillingConfigAsync`, sin SDK nuevo) como fuente del país legal, entre la SIM y la región del teléfono?**
  - **(a) Recomendada:** sí.
  - (b) No: se quedan la SIM y la región.
- **P5. ¿Cómo se fijan los precios fuera de 05 §4.2?**
  - **(a) Recomendada:** base en USD con la conversión automática de Play y las columnas LatAm y EUR de 05 §4.2 sobrescritas a mano; a los 30 días se revisan los grandes países por poder adquisitivo.
  - (b) Precio a mano en los 20 primeros países y conversión en el resto.
- **P6. ¿La tanda 2 de idiomas de D-018 (FR, DE, IT, NL, TR, JA, KO y RU) queda formalmente después de la 1.0?**
  - **(a) Recomendada:** sí, decidida con `device_language` y el criterio del ≈ 3 % de D-018.
  - (b) Adelantar 2 o 3 idiomas (DE, FR, JA) a la 1.0.
- **P7. ¿Qué ve en la Liga un jugador de un país sin nombre en la tabla?**
  - **(a) Recomendada:** el nombre de su país, desde una tabla ISO completa en los tres idiomas, generada por herramienta (CLDR) y sin banderas nuevas.
  - (b) El código ISO («JP») en lugar de «Otro país».
- **P8. ¿Se mantiene el backend solo en `sa-east-1` para la 1.0?**
  - **(a) Recomendada:** sí, con las cláusulas tipo de AWS para la UE, y se revisa con datos de uso.
  - (b) Desplegar ya una segunda región (por ejemplo, `eu-central-1`).
- **P9. ¿Idioma por defecto de la ficha de Play?**
  - **(a) Recomendada:** en-US, con es-419, es-ES, pt-BR y pt-PT como traducciones.
  - (b) Seguir con es-419.
- **P10. ¿Se contrata ya al abogado para el paquete mundial (§11)?** Hoy está aplazado para la marca (D-047), pero no para la privacidad.
  - **(a) Recomendada:** sí, antes de la 1.0, con el paquete de §11 como encargo.
  - (b) Publicar con los `[[ABOGADO]]` abiertos. No lo recomiendo: la política tiene una frase que pasa a ser falsa (§7.1).

## 11. Para el abogado (se añade a `docs/legal/paquete-abogado.md`)

1. La tabla de edades de consentimiento digital por país: EEE (RGPD art. 8 y la ley de cada Estado), UK, CH, KR, IN, CA (Quebec) y los demás que considere. Si basta con no ofrecer la analítica a 13-17 donde la edad es mayor de 13.
2. Los representantes en la UE (art. 27), el Reino Unido y Suiza: si hacen falta y qué alternativa hay.
3. Las bases legales de lo que corre sin consentimiento (Crashlytics, cuenta anónima, Liga y nube) en el EEE y UK, incluida la ePrivacy art. 5(3).
4. COPPA: si la pantalla neutral basta, si los identificadores antes de la pantalla entran en «operaciones internas» y el riesgo de «audiencia mixta» por la estética.
5. Las leyes estatales de EE. UU.: el aviso «no vender ni compartir», GPC y el *opt-in* con 13-15. Las leyes de las tiendas (Utah, Texas, Luisiana) y Play Age Signals.
6. El ECA Digital y la LGPD (Brasil) adelantados a la 1.0: la consulta del art. 8 y la de los adolescentes.
7. Las transferencias internacionales: UE → Brasil (AWS `sa-east-1`) y UE → EE. UU. (Firebase y Google).
8. Los principios CPC de la UE sobre monedas virtuales y ofertas con tiempo, y las guías de la CMA y la ASA (UK).
9. Los países que conviene excluir en la 1.0: RU, BY, VN e ID.
10. Los términos de uso mundiales: ley aplicable, derechos de los consumidores locales y compras de menores.
11. IARC: si mostrar el país en la Liga cuenta como «compartir la ubicación».

## 12. Tareas de código propuestas (cuando el dueño responda)

| Tarea | Agente | Archivos | Depende de |
|---|---|---|---|
| T-M1: `consent_digital_age_by_country` y la regla de 13-17 | unity-gameplay-dev (+ level-pipeline-dev para regenerar los defaults de RC) | `Platform/ConsentConfig.cs`, `ConsentPolicy.cs`, `RemoteConfig/Binding/ConsentConfigBinder.cs`, `UI/ConsentScreen.cs`, tests; 08 §9 (integrador) y `rc_defaults.py --check` | P2 y la edición de 08 §9 |
| T-M2: el país de la cuenta de Play en `ConsentCountry` | sdk-integrator (plugin `AfBilling.androidlib` y `NativePlayStorePort`) + unity-gameplay-dev (`ConsentCountry`, `CountrySource.PlayAccount`, tests) | `Plugins/Android/AfBilling.androidlib`, `Scripts/PlayBilling/`, `Platform/ConsentPolicy.cs` | P4 |
| T-M3: decimales de las monedas | unity-gameplay-dev | `Localization/LocPrice.cs` y sus tests | — |
| T-M4: países de la Liga | unity-gameplay-dev (+ herramienta que genere las filas) | `UI/LeagueText.cs`, `Data/Loc/strings.csv` (generado), tests de PlayMode de la Liga | P7 |
| T-M5: los defaults de `biz_country_enabled` y `biz_market_phase` | level-pipeline-dev | `tools/remote-config/rc_defaults.py --check` tras editar 08 §9 | P1 |
| T-M6: `GeoTierRule` ampliada | unity-gameplay-dev | `UI/GeoTierRule.cs` y sus tests | puede esperar |

Verificación obligatoria de cada tarea: la de `CLAUDE.md`. Con T-M1 y T-M5, además, `python tools/remote-config/rc_defaults.py --check` y sus tests. Con T-M1, T-M2, T-M3 y T-M4, EditMode, y PlayMode antes del PR.

**Estado (2026-10-27, rama `tanda6/mundial-codigo`, D-057):** T-M1 a T-M5 hechas; en T-M1, las edades son las de `docs/legal/investigacion-legal-mundial.md` §2.1. T-M6 hecha: el `geo_tier` lee el país de la cuenta de Play (Q-E3 (a)) y, con el añadido «decisiones de la revisión del código mundial» (punto 4), T1 suma NZ, SG, HK, TW, IL y el Golfo [P] (04 §6.2 y 05 §6.3). El GDD (01 §8.1, 05 §4.2 y §7, 08 §9 y §13.1 #5, #6, #10, #14, #20 y #22) recoge D-057.
