# Investigación legal para el lanzamiento mundial de la 1.0

- **Fecha:** 2026-10-27. Rama `tanda6/investigacion-legal`, sobre `524c15bd`.
- **Origen:** D-057 (`docs/dev/decisiones.md`): se lanza sin abogado, con una investigación propia en fuentes oficiales y en lo que hacen los juegos que lo hacen bien. Sustituye, para la 1.0, a la consulta del abogado de `docs/dev/lanzamiento-mundial.md` §11 y de `paquete-abogado.md`.
- **Qué no es:** no es asesoría jurídica. La hizo un agente de investigación, no un abogado. Donde una fuente no confirma un dato, va marcado **[NV]**. Lo que no se puede cerrar con fuentes queda como **riesgo aceptado** por el dueño (§12).
- **Cómo se citan las fuentes:** **[Fn]** remite a la lista de §13, con su enlace. Todas se consultaron el **2026-10-27**. Las fuentes secundarias (despachos, prensa, proveedores) se dicen así; un texto de ley o de un organismo oficial pesa más.
- **Niveles de confianza** de las respuestas propuestas:
  - **alto:** texto de ley, de un organismo oficial o de la ayuda de Google leído;
  - **medio:** fuente secundaria fiable o interpretación razonable de un texto oficial;
  - **riesgo aceptado:** las fuentes no lo cierran; lo decide el dueño (§12).
- **Qué toca este documento:** solo `docs/legal/`. Los cambios de código, de la web y de la configuración se proponen como tareas (§11) y los hacen los agentes de cada carril.

## 0. Veredicto en doce líneas

1. **Representante en la UE (RGPD art. 27): hace falta.** Tintalú ofrece un servicio a personas de la UE con analítica, anuncios y menores de 13-17, de forma continua; la excepción de «ocasional y de bajo riesgo» no encaja [F2][F10]. La AP neerlandesa ya multó con 525.000 € a una empresa de fuera de la UE solo por no tenerlo [F13]. Hay servicios desde ≈ 300-750 € al año [F14]. **Lo mismo en el Reino Unido** [F11]. **Suiza no** lo exige salvo tratamiento a gran escala y de alto riesgo [F12].
2. **La frase «no tenemos representante porque no nos dirigimos a la UE» hay que quitarla ya.** Pasa a ser falsa.
3. **Edad digital por país:** la tabla de §2.1 da los valores de `consent_digital_age_by_country`. Además del EEE, conviene 18 en **Colombia** (los datos de adolescentes los autoriza el representante legal [F41]), **Brasil** (ECA Digital), **México** e **India**; 14 en **Corea**, **Perú** y **Canadá** (Quebec).
4. **COPPA:** una app de audiencia general puede bloquear a los menores de 13 tras una pantalla de edad neutral [F21]. Lo que corre antes de la pantalla (Crashlytics y Remote Config) entra en «operaciones internas», pero la regla de 2025 exige **decirlo en la política** [F22][F23].
5. **Texas (SB 2420) está en vigor desde el 28-05-2026** y obliga a los desarrolladores a pedir la señal de edad de la tienda; Google la da con **Play Age Signals API**, que también funciona en **Brasil** desde el 17-03-2026 [F24][F25][F26]. Integrarla es una dependencia nueva: la aprueba el dueño (regla dura 4).
6. **Brasil (ECA Digital, Ley 15.211, en vigor desde el 17-03-2026)** se aplica a juegos «de acceso probable» por menores: privacidad más protectora por defecto, sin perfilado publicitario de menores, herramientas de supervisión parental, cuentas de menores de 16 **vinculadas a un responsable** (art. 24) y un **representante legal en Brasil** (art. 40), modulados por el tamaño (art. 39) [F29][F30]. Lo que no se puede cumplir en la 1.0 queda como riesgo aceptado (§12).
7. **Países a excluir de la distribución:** Rusia y Bielorrusia (sin cobros de Play [F47]), **Vietnam** (prohíbe ofrecer juegos en línea desde el extranjero sin empresa y licencia local [F45]) e **Indonesia** (registro PSE y clasificación IGRS obligatorios [F46]). China no tiene Play.
8. **Consumo en la UE:** los principios CPC de marzo de 2025 piden precio en dinero real, no empujar a comprar más moneda de la necesaria, información del desistimiento y cuidado con los menores [F48]. La ACM multó a Epic por **temporizadores que no reflejaban la disponibilidad real** y por exhortar a niños a comprar [F64]: revisar la Mochila de 48 h y los textos de la tienda.
9. **Sin loot boxes:** se confirma como buena práctica y como requisito en Brasil (art. 20 del ECA Digital) [F29]. Nada que cambiar.
10. **Transferencias:** desde enero de 2026 hay **adecuación mutua UE-Brasil**, así que el backend en `sa-east-1` no necesita cláusulas tipo para los datos de la UE [F17]; Firebase y AWS traen las cláusulas tipo en sus términos [F19][F20].
11. **Los juegos de referencia:** Dream Games (Royal Match) nombra representante en la UE y en el Reino Unido y tiene anexos por región (EEE, EE. UU., Corea) [F57]; King y Supercell son responsables establecidos en la UE (Malta y Finlandia) y no lo necesitan [F58][F59]. Grand Games (Magic Sort) es el ejemplo de lo que no conviene copiar (consentimiento «por usar», transferencias «aceptadas» y política de 2023) [F61].
12. **Cada `[[ABOGADO]]` de la web tiene texto propuesto** (§9), con 40 marcadores (los mismos en los tres idiomas) y 11 consultas de `lanzamiento-mundial.md` §11 (§10).

---

## 1. Hechos del juego que importan para lo legal

Resumen de lo que ya está decidido o implementado; la fuente manda.

| Hecho | Fuente |
|---|---|
| Responsable: Parallext, LLC (Wyoming, gestionada desde Colombia). Editora en Play y cobros: AUTOMATION AI S.A.S (Bogotá) | D-048 |
| Pantalla de edad neutral con tres botones iguales {<13, 13-17, 18+}; <13 bloqueado en cada arranque sin ningún SDK; bloqueo definitivo | D-033 (Q-B12-1, Q-B12-3) |
| 13-17: anuncios no personalizados, contenido G, doble confirmación de compras; analítica ofrecida | D-033 (Q-B12-7) |
| 13-17 en el EEE, el Reino Unido y Suiza: sin anuncios | D-055, punto 4 |
| Antes de la pantalla de edad: Crashlytics y la descarga de Remote Config | 08 §2.3; privacidad de la web §3 |
| País legal: SIM → (propuesto) país de la cuenta de Play → región → ninguno | `lanzamiento-mundial.md` §1.2; D-057 |
| Anuncios: AdMob directo con UMP; `AD_ID` solo con 18+ y consentimiento | D-049, D-055 |
| Backend en AWS `sa-east-1`; Firebase en EE. UU. | D-035, 08 §13.3 |
| Liga con seudónimo automático, bandera y puntos; sin texto libre | D-046 |
| Borrado de cuenta en el juego y en la web | 08 §11 |
| Sin compras aleatorias | 05 |

---

## 2. Unión Europea, Reino Unido y Suiza

### 2.1 Edad de consentimiento digital (RGPD art. 8)

**Qué dice la norma.** Cuando la base es el consentimiento y el servicio se ofrece directamente a un niño, el tratamiento es lícito desde los **16 años**; por debajo, hace falta la autorización de quien tiene la patria potestad. Cada Estado puede bajar la edad hasta **13**. El responsable debe hacer «esfuerzos razonables» para verificarlo [F1].

**Qué afecta a Tintalú.** Solo la **analítica** se basa en el consentimiento con 13-17 (los anuncios ya están apagados en el EEE, el Reino Unido y Suiza para 13-17). Crashlytics, la cuenta anónima, la Liga y la nube usan otras bases (§9), a las que el art. 8 no se aplica.

**Tabla para `consent_digital_age_by_country`.** Regla de la tabla: **ante una duda o una contradicción entre fuentes, el valor más alto**. Con un valor más alto solo se pierde la analítica de unos pocos adolescentes.

| País | Ley (según la fuente) | Edad legal | Valor propuesto | Confianza | Fuente |
|---|---|---|---|---|---|
| AT Austria | DSG §4(4) | 14 | 14 | medio | [F3][F4] |
| BE Bélgica | Ley de 30-07-2018, art. 7 | 13 | 13 | medio | [F3][F4] |
| BG Bulgaria | Ley de datos, art. 25c | 14 | 14 | medio | [F3] |
| CY Chipre | Ley 125(I)/2018, art. 8(1) | 14 | 14 | medio | [F3] |
| CZ Chequia | Ley 110/2019, §7 | 15 | 15 | medio | [F3][F4] |
| DE Alemania | sin excepción nacional | 16 | 16 | alto | [F1][F3][F4] |
| DK Dinamarca | Ley 502/2018, §6(2) | 13 | 13 | medio | [F3] |
| EE Estonia | Ley de 2018, §8(1) | 13 | 13 | medio | [F3] |
| ES España | LOPDGDD, art. 7 | 14 | 14 | alto | [F74][F3] |
| FI Finlandia | Ley 1050/2018, §5 | 13 | 13 | medio | [F3] |
| FR Francia | Ley 78-17, art. 45 | 15 | 15 | medio | [F3][F4] |
| GR Grecia | Ley 4624/2019, art. 21 | 15 | 15 | medio | [F3] |
| HR Croacia | Ley de aplicación del RGPD, art. 19 | 16 | 16 | medio | [F3] |
| HU Hungría | sin excepción nacional | 16 | 16 | medio | [F3] |
| IE Irlanda | Data Protection Act 2018, s. 31 | 16 | 16 | medio | [F3][F4] |
| IS Islandia (EEE) | Ley 90/2018 | 13 | 13 | medio | [F6] |
| IT Italia | Codice privacy, art. 2-quinquies | 14 | 14 | medio | [F3][F4] |
| LI Liechtenstein (EEE) | no se encontró excepción | 16 [NV] | 16 | riesgo aceptado | [F1] |
| LT Lituania | Ley de protección de datos, art. 6 | 14 | 14 | medio | [F3] |
| LU Luxemburgo | Ley de 1-08-2018 | 16 | 16 | medio | [F3] |
| LV Letonia | Ley de 2018, s. 33 | 13 | 13 | medio | [F3] |
| MT Malta | LS 586.11 | 13 | 13 | medio | [F8] |
| NL Países Bajos | UAVG, art. 5 | 16 | 16 | medio | [F3][F4] |
| NO Noruega (EEE) | Personopplysningsloven §5 | 13, con propuesta de subir a 15 (2025) | **15** | medio | [F7] |
| PL Polonia | sin excepción nacional | 16 | 16 | medio | [F3][F4] |
| PT Portugal | Ley 58/2019, art. 16 | 13 | 13 | medio | [F3] |
| RO Rumanía | sin excepción nacional | 16 | 16 | medio | [F3] |
| SE Suecia | Ley 2018:218, cap. 2 §4 | 13 | 13 | medio | [F3][F4] |
| SI Eslovenia | ZVOP-2, art. 8 (15) según Bird & Bird; 16 según otra fuente | 15 o 16 | **16** | medio | [F5][F3] |
| SK Eslovaquia | Ley 18/2018, §15 | 16 | 16 | medio | [F3] |
| UK Reino Unido | DPA 2018, s. 9 | 13 | 13 | alto | [F9] |
| CH Suiza | sin edad fija (capacidad de discernimiento) [NV] | — | **16** | riesgo aceptado | — |

Notas:
- Las 27 de la UE, más IS, LI y NO, son las 30 de `ConsentConfig.EeaCountries`.
- La fuente [F3] contradice a otra tabla secundaria en Chequia y en Malta; se tomó la que cita el artículo de la ley.
- **Noruega** tramitó en 2025 la subida a 15 (notificación TRIS 2025/9028/NO) [F7]; no se confirmó si ya está aprobada [NV]. Se propone 15 por la regla del valor más alto.
- El ICO aclara que los juegos y las apps son «servicios de la sociedad de la información», también si son gratuitos y se financian con anuncios [F9].

### 2.2 Representante en la UE (art. 27)

- **Cuándo es obligatorio:** cuando se aplica el art. 3.2, es decir, cuando un responsable sin establecimiento en la UE ofrece bienes o servicios a personas en la UE o vigila su comportamiento [F2]. El EDPB pone como ejemplo una web de Turquía con envíos a la UE, idiomas y moneda de la UE: «tendrá que designar un representante» [F10].
- **La excepción (art. 27.2.a):** tratamiento **ocasional**, sin categorías especiales a gran escala y que **probablemente no entrañe un riesgo** [F2]. El EDPB la lee de forma estrecha: «ocasional» es lo que no se hace con regularidad y queda fuera del curso normal del negocio; y basta «un riesgo», no un riesgo alto [F10].
- **Aplicado a Tintalú:** la ficha se distribuye en el EEE, Play cobra en euros, UMP muestra el mensaje del RGPD y la analítica, los anuncios y la Liga funcionan cada día. **No es ocasional**, y hay menores de 13-17. **Conclusión: hace falta representante** (confianza **alto** en la regla, **medio** en la aplicación).
- **Qué hace el representante:** es el punto de contacto de los interesados y de las autoridades y lleva el registro del art. 30; no sustituye la responsabilidad de Parallext [F10].
- **Precedente:** la Autoriteit Persoonsgegevens multó a Locatefamily.com con **525.000 €** (mayo de 2021) solo por no tener representante, con una orden de nombrarlo en 12 semanas [F13].
- **Coste:** hay servicios de representación desde ≈ 300-500 USD al año y otros de 750-2.500 £/USD, todos de proveedores comerciales [F14] [E]. Varios ofrecen UE y Reino Unido juntos.
- **Cómo lo hacen los de referencia:** Dream Games (Turquía) nombra un despacho de Budapest como representante en el EEE y su filial de Londres en el Reino Unido [F57]. King y Supercell tienen sede en la UE y no lo necesitan [F58][F59].

### 2.3 Reino Unido (UK GDPR art. 27) y Suiza (nLPD art. 14)

- **Reino Unido:** el ICO confirma que una organización de fuera del Reino Unido que ofrece bienes o servicios a personas del Reino Unido necesita un representante, salvo el tratamiento ocasional y de bajo riesgo [F11]. Mismo análisis que §2.2: **hace falta** (confianza **medio**).
- **Suiza:** el art. 14 exige un representante solo si se cumplen **las cuatro** condiciones: oferta de bienes o servicios en Suiza, tratamiento **a gran escala**, **regular** y con **alto riesgo** para la personalidad [F12]. Un juego casual sin datos sensibles y con pocos usuarios suizos no las cumple todas. **No hace falta en la 1.0** (confianza **medio**); se revisa si Suiza crece.

### 2.4 ePrivacy y Crashlytics antes del consentimiento

- La directiva ePrivacy (art. 5.3) exige consentimiento para leer o guardar información en el terminal, salvo lo **estrictamente necesario** para el servicio pedido. El EDPB aprobó en octubre de 2024 la versión 2.0 de sus directrices sobre el alcance técnico del art. 5.3 [F16].
- La CNIL publicó en 2024 sus recomendaciones para apps móviles: ningún SDK que necesite consentimiento puede iniciarse antes de obtenerlo, y el consentimiento debe poder rechazarse con la misma facilidad [F15]. No se encontró una exención expresa para los informes de fallos [NV].
- **Cómo lo hacen los de referencia:** Dream Games apoya las «cookies y tecnologías de seguimiento esenciales» en el art. 5.3 y en el art. 6.1.b o f del RGPD [F57]; King y Supercell no mencionan los informes de fallos [F58][F59].
- **Reino Unido:** la Data (Use and Access) Act 2025 añadió excepciones al consentimiento de PECR, entre ellas una para fines estadísticos con información clara y una forma fácil de oponerse; las fuentes son secundarias y discrepan en las fechas (febrero o junio de 2026) [F71].
- **Recomendación:** mantener Crashlytics como interés legítimo y «estrictamente necesario» para que el juego funcione (sin el UID y con 90 días de retención), pero **añadir un interruptor «Enviar informes de fallos» en Ajustes › «Privacidad y datos»**, activado por defecto, para tener la objeción fácil que piden el art. 21 del RGPD y la excepción británica. Es lo que reduce más el riesgo con menos coste (§11, T-L6). El resto queda como **riesgo aceptado** (§12, R5).

### 2.5 Transferencias internacionales

| Flujo | Mecanismo | Confianza | Fuente |
|---|---|---|---|
| EEE → Brasil (AWS `sa-east-1`) | **Decisión de adecuación de la Comisión para Brasil**, adoptada el 26-01-2026 (y resolución recíproca CD/ANPD n.º 32) | alto (varias fuentes secundarias concordantes) | [F17] |
| EEE → EE. UU. (Firebase, Google, AWS) | Marco de Privacidad de Datos UE-EE. UU. (confirmado por el Tribunal General el 3-09-2025; recurso C-703/25 P pendiente) y, además, las **cláusulas tipo** de los términos de Firebase y de AWS | medio | [F18][F19][F20] |
| Reino Unido → EE. UU. y Brasil | Cláusulas tipo con el anexo británico de Firebase y AWS (IDTA o *addendum*) | medio | [F11][F19][F20] |
| Brasil → EE. UU. (Firebase, Google) | LGPD art. 33 II (cláusulas tipo). La adecuación UE-Brasil **no** cubre Brasil → EE. UU. [F17]. Si Google y AWS ofrecen las cláusulas tipo de la ANPD no se pudo comprobar [NV] | riesgo aceptado | [F32][F17] |
| CO, PE, MX → EE. UU. y Brasil | Cada ley admite la transferencia con garantías contractuales o con el consentimiento; el detalle de cada país no se comprobó [NV] | riesgo aceptado | — |

**Acción del dueño:** aceptar en Firebase los *Data Processing and Security Terms* (Ajustes del proyecto › Privacidad) y en Google Analytics las condiciones de tratamiento de datos, si no están aceptadas; las cláusulas tipo de Firebase se incorporan a esos términos [F19]. Las de AWS se aplican solas [F20].

---

## 3. Estados Unidos

### 3.1 COPPA: audiencia general, pantalla neutral y bloqueo de <13

- **Audiencia general:** la COPPA solo alcanza a un servicio de audiencia general cuando tiene **conocimiento real** de que un usuario es menor de 13 (FAQ A.12) [F21].
- **Puede bloquear:** la COPPA «no exige permitir participar a los menores de 13» y un servicio de audiencia general **puede bloquearlos** (FAQ H.3). Un servicio dirigido a niños o de audiencia mixta, en cambio, no puede bloquearlos (D.4, H.3) [F21].
- **Pantalla neutral (D.7, H.3):** preguntar la edad sin inducir a mentir, sin dejar marcada por defecto una edad de 13 o más y sin anunciar que los menores de 13 no podrán jugar; conviene impedir que se vuelva atrás para cambiar la respuesta [F21]. Los tres botones iguales sin preselección y el bloqueo definitivo (Q-B12-1 y Q-B12-3) cumplen esas pautas (confianza **medio**: la FTC pone como ejemplo escribir el mes y el año, no bandas).
- **Audiencia mixta:** la regla de 2025 define el servicio de «audiencia mixta» como una subcategoría de los dirigidos a niños [F22]. La FTC no lo convierte en mixto «solo porque lo usen algunos niños» (H.5) [F21]. El riesgo para Tintalú es la estética (TiTino, frutas, colores): conviene no usar en la ficha ni en la publicidad elementos de captación infantil (§7.1).
- **Operaciones internas:** los identificadores persistentes usados **solo** para el funcionamiento (mantener o analizar el funcionamiento, depurar, seguridad, frecuencia de anuncios contextuales, cumplimiento legal) no necesitan consentimiento parental, sin recoger otros datos personales ni crear perfiles (FAQ J.5-J.7) [F21]. Crashlytics y Remote Config antes de la pantalla encajan (confianza **medio**).
- **Regla de 2025:** publicada el 22-04-2025, en vigor el 23-06-2025, cumplimiento obligatorio desde el **22-04-2026** [F22]. Exige consentimiento parental separado para revelar datos de niños a terceros con fines de publicidad, y que quien se acoja a la excepción de operaciones internas **lo diga en su aviso en línea**: qué operaciones internas y cómo evita otros usos; la FTC admite hacerlo en términos generales [F23]. **Tarea:** añadirlo a la política (§9, filas 24 y 30).

### 3.2 Leyes de las tiendas (Texas, Utah, Luisiana) y Play Age Signals

- **Texas SB 2420:** un juez la suspendió en diciembre de 2025, pero el Quinto Circuito levantó la suspensión el **28-05-2026**: está en vigor mientras sigue el pleito [F24][F26]. Obliga a los desarrolladores a **pedir y recibir la señal de edad** de la tienda, a usarla para gestionar el acceso de los menores y a declarar si la app es adecuada para <13, 13-15, 16-17 o 18+ [F26].
- **Utah y Luisiana:** leyes parecidas; las fechas varían según la fuente [NV]. **Alabama** aprobó otra en febrero de 2026 [F26].
- **Play Age Signals API (beta, versión 0.0.4):** devuelve la franja de edad cuando el usuario o su tutor lo aceptan. Hoy funciona en **Texas** (cuentas creadas desde el 28-05-2026) y en **Brasil** (desde el 17-03-2026) [F25][F34]; Google anunció que se abre a todos los mercados antes de fin de 2026 [F34].
- **Restricción de uso:** la información solo puede usarse para dar «contenido y experiencias adecuados a la edad»; está prohibido usarla para publicidad, marketing, perfiles o **analítica**, con retirada de la app como sanción [F25].
- **Recomendación:** integrar Play Age Signals antes de la 1.0, como segunda fuente de la banda (`consent_age_signal_source` = `self_declared+play_age_signals`, que el GDD ya reserva). Con una señal de 0-12, bloquear; con 13-15 o 16-17, tratar como 13-17. Es una **dependencia nueva**: la aprueba el dueño (regla dura 4) (§11, T-L3). Y **no mandar la banda a Analytics cuando venga de la señal** (§11, T-L4).

### 3.3 California (CCPA/CPRA) y las demás leyes estatales

- **A quién se aplica:** a quien supere **26.625.000 USD** de ingresos (cifra de 2025), o compre, venda o **comparta** datos de **100.000** consumidores u hogares de California, o saque de ahí el 50 % de sus ingresos [F27]. Parallext probablemente no llega hoy a ninguno [E], pero el umbral de 100.000 puede alcanzarse con anuncios personalizados si el juego crece en EE. UU.
- **Menores de 16:** con conocimiento real de que el consumidor tiene menos de 16, no se pueden vender ni «compartir» sus datos sin autorización afirmativa (13-15) o de los padres (<13); ignorar la edad cuenta como conocerla (Cal. Civ. Code §1798.120(c)) [F27]. La banda 13-17 ya da ese conocimiento: con 13-17 **no hay anuncios personalizados**, lo que cumple.
- **«Compartir» y anuncios no personalizados:** los anuncios no personalizados de Google siguen usando identificadores para la frecuencia, el fraude y los informes [F67]. Para que tampoco cuenten como «compartir», AdMob ofrece el **tratamiento restringido de datos (RDP)**, con la clave `rdp=1` en cada petición o la señal GPP de UMP [F28]. **Recomendación:** pedir los anuncios de 13-17 en EE. UU. con `rdp=1` (§11, T-L5). Google recibe directamente la señal GPC en Delaware y Oregón desde el 17-11-2025 [F28].
- **18+:** el *opt-in* propio de `default` es más estricto que el *opt-out* que exigen estas leyes. Falta el aviso en la política (§9, filas 15 y 24). El mensaje de leyes estatales de EE. UU. de UMP ya está configurado en AdMob (D-049).

---

## 4. Brasil

### 4.1 LGPD

- **Adolescentes y niños (art. 14):** el tratamiento de datos de niños y adolescentes debe hacerse en su **mejor interés**; el de **niños** (menores de 12 en el derecho brasileño [NV]) necesita el consentimiento específico de uno de los padres. Los juegos no pueden condicionar el acceso a más datos de los estrictamente necesarios (§4) [F32].
- **Plazo de acceso (art. 19 II):** la declaración completa, en **15 días** desde la petición [F32].
- **Encarregado (art. 41):** el responsable lo nombra y publica su identidad y su contacto [F32]. Los agentes de tratamiento de pequeño porte pueden quedar dispensados (Resolución CD/ANPD 2/2022), pero con un canal de comunicación; la dispensa no vale con tratamiento de alto riesgo a gran escala, y no se comprobó si alcanza a un responsable extranjero [F33] [NV]. Tintalú **ya publica un encarregado**: se mantiene.
- **Transferencias (art. 33):** se admiten, entre otras, con cláusulas tipo (II) o con el consentimiento específico (VIII) [F32]. Con la UE hay adecuación mutua [F17].

### 4.2 ECA Digital (Ley 15.211/2025)

En vigor desde el **17-03-2026** (Ley 15.352/2026), con el Decreto 12.880/2026 del 18-03-2026 y orientaciones preliminares de la ANPD del 20-03-2026 [F29][F30][F31]. Las sanciones administrativas se esperaban desde noviembre de 2026 y las inspecciones formales desde enero de 2027, según un despacho [F30].

| Artículo | Qué exige | Tintalú hoy | Qué hacer |
|---|---|---|---|
| 1 | Se aplica a productos «dirigidos» a niños y adolescentes o **de acceso probable** por ellos, también desde el extranjero [F29] | Juego 13+ de colores y animales: acceso probable de adolescentes | Se aplica |
| 7 | Configuración **más protectora por defecto** [F29] | Analítica y anuncios personalizados apagados hasta aceptar; 13-17 sin personalización | Con `BR` = 18 en la tabla, 13-17 sin analítica |
| 8 | Gestión de riesgos y diseño que no fomente el uso compulsivo [F29] | Ofertas con temporizador (Mochila) | Revisar con §6 |
| 9 | Verificación fiable de la edad, **sin autodeclaración**, solo para contenido impropio o prohibido para menores [F29] | No es nuestro caso | Nada |
| 17-18 | Herramientas de supervisión parental, incluida la de **restringir compras** [F29] | Doble confirmación y autenticación de Play; Family Link de Google restringe compras [NV] | Explicarlo en la política (§9) |
| 20 | **Prohibidas las loot boxes** en juegos de acceso probable [F29] | Sin compras aleatorias | Nada |
| 22 | **Prohibido el perfilado** para publicidad comercial a menores [F29] | 13-17: anuncios no personalizados | Nada |
| 24 | Cuentas de menores de **16** **vinculadas** a un responsable legal; el caput se aplica a todos los productos, no solo a redes sociales [F29] | Cuenta anónima sin vínculo | **Riesgo aceptado** (§12, R3) |
| 35 | Advertencia; multa de hasta el 10 % de la facturación en Brasil o de 10 a 1.000 R$ por usuario, hasta 50 M R$ por infracción [F29] | — | — |
| 39 | Las obligaciones se modulan por el tamaño y el número de usuarios [F29] | Estudio pequeño | Argumento de proporcionalidad |
| 40 | **Representante legal en Brasil** para recibir citaciones [F29] | No hay | **Riesgo aceptado** (§12, R4) |

- **Edad y señal de la tienda:** el borrador de guía de la ANPD de mayo de 2026 clasifica los servicios por riesgo; en los de **bajo riesgo** basta recibir la señal de la tienda; la autodeclaración sola no es un mecanismo fiable [F31]. Un juego casual sin chat ni loot boxes encaja entre bajo y moderado [E]. **Play Age Signals** ya da la señal en Brasil [F34]: integrarla (§3.2) es la mejor defensa disponible.
- **Recomendación:** lanzar en Brasil con: Play Age Signals, `BR` = 18 (sin analítica para 13-17), sin anuncios personalizados ni loot boxes, la doble confirmación de compras y un aviso en la política sobre Family Link. El art. 24 y el art. 40 quedan como riesgos aceptados.

---

## 5. Otros países

| País | Qué obliga a un juego así | Confianza | Fuente | Qué hacer |
|---|---|---|---|---|
| **Corea del Sur** (PIPA) | Art. 22-2: con menores de **14**, si hace falta su consentimiento, se pide al representante legal y se comprueba [F35]. Agente local de la PIPA: solo con más de 1 billón de KRW de ventas o más de 1 millón de usuarios coreanos [F35]. Agente local de la ley de juegos (desde el 23-10-2025): más de 1 billón de KRW o **una media de 1.000 instalaciones diarias en Corea** el año anterior [F36] | alto (PIPA); medio (juegos) | [F35][F36] | `KR` = 14. Vigilar las instalaciones diarias en Corea; si pasan de 1.000 de media, nombrar agente |
| **India** (DPDP 2023, reglas de 2025) | Niño es menor de **18**; consentimiento parental verificable (regla 10). La ley prohíbe el seguimiento, la monitorización del comportamiento y la publicidad dirigida a niños (s. 9) [NV texto]. Las obligaciones de menores entran 18 meses después de las reglas (≈ mayo de 2027) [F37] | medio | [F37] | `IN` = 18: con 13-17, sin analítica y anuncios no personalizados. Valorar en 2027 si 13-17 en la India va sin anuncios |
| **China** | Google Play no opera | alto | — | Nada |
| **Japón** (APPI) | Sin edad digital en la ley; la Comisión (PPC) orienta a pedir el consentimiento del representante para los niños de unos 12-15 años o menos [NV]. Moneda del juego: un emisor de medios de pago prepagados «propios» tiene que notificarse y depositar garantía si el saldo sin usar pasa de **10 M JPY** el 31 de marzo o el 30 de septiembre [F38] | medio (prepago); riesgo aceptado (edad) | [F38] | `JP` = 16. Vigilar el saldo de semillas compradas sin gastar en Japón |
| **Australia** | Privacy Act: exención de pequeña empresa (≤ 3 M AUD de facturación), salvo quien «comercia con datos personales» [F39]. Código de privacidad infantil en borrador; registro antes del **10-12-2026**; cubre la mayoría de juegos que usan niños y adolescentes [F39] | medio | [F39] | `AU` = 13 por ahora; revisar al publicarse el código |
| **Canadá** (Quebec, Ley 25) | Con menores de **14**, consentimiento de quien tiene la patria potestad, salvo lo que es claramente en su beneficio [F40] | medio (secundaria) | [F40] | `CA` = 14 (no se puede partir por provincia) |
| **Colombia** (Ley 1581) | Art. 7: el tratamiento de datos de niños, niñas y adolescentes está «proscrito» salvo datos públicos [F41]; el Decreto 1377/2013, art. 12 (hoy 2.2.2.25.2.9 del Decreto 1074/2015), lo admite con el interés superior y **la autorización del representante legal**, oído el menor [F41] [secundaria]. Plazos: consultas en **10 días hábiles** (+5) y reclamos en **15** (+8) (arts. 14 y 15) [F41] | alto (plazos); medio (menores) | [F41] | `CO` = 18 |
| **Perú** (Ley 29733, reglamento DS 016-2024-JUS, en vigor desde marzo de 2025) | Los de 14 a 18 pueden consentir; por debajo, padres o tutores [secundaria] [NV] | riesgo aceptado | [F42] | `PE` = 14 |
| **México** (nueva LFPDPPP, 21-03-2025) | El INAI desaparece; sus funciones pasan a la **Secretaría Anticorrupción y Buen Gobierno**. Derechos ARCO: respuesta en 20 días hábiles [secundaria] [F43][F72]. Edad: sin regla propia; los menores no tienen capacidad de obrar [NV] | medio (autoridad); riesgo aceptado (edad) | [F43][F72] | `MX` = 18 |
| **Turquía** (KVKK) | Los responsables extranjeros tienen que inscribirse en VERBIS **mediante un representante en Turquía**, sin umbral de tamaño, según fuentes de proveedores [F44] | riesgo aceptado | [F44] | Decide el dueño (§12, R8) |
| **Vietnam** | El Decreto 147/2024 (desde el 25-12-2024) exige empresa en Vietnam y licencia para ofrecer juegos en línea; la prestación transfronteriza está prohibida [F45] | medio | [F45] | **Excluir** |
| **Indonesia** | Registro PSE de los operadores extranjeros (Komdigi suspendió plataformas extranjeras sin registro en junio de 2025) y clasificación IGRS obligatoria desde el 24-01-2026 [F46] | medio | [F46] | **Excluir** en la 1.0 |
| **Rusia y Bielorrusia** | Google Play pausó los cobros en Rusia el 10-03-2022, también en Bielorrusia [F47]; sanciones y ley de localización [NV] | alto (Play) | [F47] | **Excluir** (D-057) |

---

## 6. Consumo: monedas virtuales, desistimiento y loot boxes

### 6.1 Principios CPC (21-03-2025)

La red de autoridades de consumo de la UE (CPC) adoptó siete principios sobre las monedas virtuales que se compran con dinero real; no son vinculantes, pero dicen cómo interpretarán la ley [F48]:

| # | Principio | Tintalú | Acción |
|---|---|---|---|
| 1 | Precio claro en dinero real, también al gastar la moneda | Packs de semillas con el precio de Play; los artículos, en semillas | Riesgo medio: valorar un «≈ x €» informativo en el Mercado del EEE y del Reino Unido (§11, T-L10) |
| 2 | Sin costes ocultos ni varias monedas | Una sola moneda (semillas) | Nada |
| 3 | No obligar a comprar más moneda de la necesaria | Tamaños de pack frente a precios de artículos sin revisar | Revisar que haya un pack que cubra cada compra frecuente sin un sobrante grande (§11, T-L10) |
| 4 | Información precontractual clara | Ficha y hoja de Play | Nada |
| 5 | Desistimiento de 14 días y cómo ejercerlo | Lo gestiona Google Play (§6.2) | Explicarlo en los términos (§9) |
| 6 | Condiciones claras y justas | Términos en borrador | §9 |
| 7 | Cuidado con los vulnerables (menores) | 13-17 con doble confirmación; sin anuncios en el EEE | Nada más |

El mismo día la red actuó contra Star Stable [F48]. En septiembre de 2024, BEUC y 22 organizaciones denunciaron a siete editores (Epic, Supercell, EA, Roblox…) por los precios solo en moneda virtual [F66].

### 6.2 Desistimiento en contenido digital

- En la UE, quien compra contenido digital tiene 14 días para desistir, salvo que haya dado su **consentimiento expreso a la ejecución inmediata** y reconocido que pierde el derecho. Google Play lo recoge en la compra: los reembolsos de contenido dentro de la app se limitan, en general, a contenido defectuoso o no disponible [F49].
- Google actúa como **comerciante de registro** en los países que lista el DDA §3.4; la versión de 2019 nombraba el EEE y el Reino Unido, la actual remite a una lista [F50].
- **Cómo lo hacen los de referencia:** Supercell: «al comprar consientes la entrega antes de que acabe el plazo y pierdes el derecho de desistimiento» [F58]. Dream Games: reconoce el desistimiento de 14 días en el EEE y deja reclamarlo en la tienda o a ellos [F57].
- **Recomendación:** que los términos digan que en el EEE, el Reino Unido y Suiza se aplica el derecho de desistimiento según la ley, que Google Play recoge el consentimiento a la entrega inmediata, y cómo reclamar (§9, fila 33).

### 6.3 Loot boxes

No hay compras aleatorias (términos §4, privacidad §4). Es la práctica que evita: la prohibición de Brasil (ECA Digital art. 20) [F29], la orden de la FTC contra HoYoverse (venta de loot boxes a menores de 16 sin padres, probabilidades engañosas) [F63], y la verificación de edad estricta que la ANPD exige a los juegos con loot boxes [F31]. **Confirmado como buena práctica; no hay que cambiar nada.**

### 6.4 Temporizadores y exhortaciones

- La ACM (Países Bajos) multó a Epic con **1.125.000 €** en 2024: por exhortar directamente a niños a comprar («Consíguelo ya») y por **temporizadores** en la tienda que llegaban a cero aunque el artículo seguía a la venta [F64].
- **Mochila de inicio (48 h, 05 §4.1):** solo es legítima si, al llegar a cero, **la oferta desaparece de verdad** y no vuelve enseguida. Revisar también que ningún texto de la tienda diga a un menor «¡Compra ya!» o equivalente (§11, T-L9).

### 6.5 Resolución de litigios

La plataforma europea de resolución de litigios en línea (ODR) **cerró el 20-07-2025** (Reglamento 2024/3228) y ya no hay que enlazarla [F51]. Supercell todavía la cita [F58]: no copiarlo.

### 6.6 Accesibilidad (Acta Europea)

El art. 4.5 de la Directiva 2019/882 exime a las **microempresas que prestan servicios** de los requisitos de accesibilidad [F52]. Parallext lo es [E]. Puede esperar.

---

## 7. Google Play

### 7.1 Público objetivo 13+ y política de familias

- Las franjas son ≤5, 6-8, 9-12, 13-15, 16-17 y 18+. Google avisa de que **13-15 y 16-17 pueden considerarse «niños» en algunos lugares** y de que, si se incluyen niños, se aplica la política de familias [F53].
- Si una app no está diseñada para menores de 13 pero su ficha tiene **animación infantil o personajes jóvenes**, Google puede rechazarla; las salidas son quitar esos elementos o pasar a la política de familias [F53].
- **Recomendación:** declarar 13-15, 16-17 y 18+; en la ficha, capturas de juego y TiTino como personaje, sin lemas ni estética dirigidos a niños. AdMob es un SDK autocertificado para familias, así que un cambio de clasificación no obligaría a cambiar de red [F53] [NV].

### 7.2 Política de datos de usuario

- La política de privacidad debe nombrar a la entidad de la ficha **o** la app, dar un contacto de privacidad y explicar la **retención y el borrado** [F55].
- La divulgación destacada va **en la app**, no solo en la política, y el consentimiento exige una acción afirmativa [F55]. La pantalla propia lo cumple.

### 7.3 Cuenta de organización con un desarrollador distinto del responsable (D-048)

- Basta con que la política nombre a la entidad de la ficha o a la app [F55]. La web nombra a Parallext (responsable) y a AUTOMATION AI S.A.S (editora) (D-048): **cumple**.
- El DDA §11.1 exige que quien publica tenga derecho a monetizar por cuenta propia [F50]; eso lo resuelve la licencia entre empresas de `acuerdo-publicacion-borrador.md`, que es un contrato, no un texto público. Queda en §12 (R9).

### 7.4 Borrado de cuenta

- Si la app permite crear una cuenta, tiene que ofrecer el borrado **en la app** y en una **web** que nombre la app o el desarrollador, sin obligar a reinstalar; borrar implica borrar los datos asociados, también en los proveedores; congelar no cuenta; lo que se guarde por fraude o ley se dice en la política [F54][F55].
- La cuenta anónima no está expresamente cubierta, porque Google define la cuenta con usuario, correo o contraseña [F54]; aun así, Tintalú ya ofrece las dos vías: **cumple**.
- **Playrix:** borrado en Ajustes con confirmación y 15 días para cancelarlo; las peticiones de datos tardan hasta 30 días [F60].

### 7.5 Seguridad de los datos

- Los datos que el sistema de pagos recoge para procesar la transacción **no los declara el desarrollador** [F56]: así se cierra Q-BILL-2 en Data Safety.
- La IP de los registros solo se declara si se deriva una ubicación. **La Liga usa el país de la conexión de CloudFront** (`data-safety.md`, Liga), así que hay que declarar **«Ubicación aproximada»** para la Liga, como ya propone el borrador.
- **Incoherencia detectada:** la política dice «la implementación inicial usa la región del teléfono» y `data-safety.md` dice «primero el país de conexión de CloudFront». El integrador comprueba el código y alinea los dos textos (§11, T-L12).
- **Resuelto (T-L12, 2026-10-27):** la Liga **no** usa CloudFront. El cliente manda el país del perfil legal (SIM o, si no hay, la región del teléfono) y el servidor ignora `CloudFront-Viewer-Country` (`infra/LEAGUE.md`; `infra/functions/league/rules.ts`). La política y `data-safety.md` ya lo dicen así; la línea anterior sobre CloudFront no se aplica.

---

## 8. Comparativa con juegos de referencia

| Juego (empresa) | Edad | Representantes UE/UK | Consentimiento y bases | Borrado | Moneda virtual y términos | Fuente |
|---|---|---|---|---|---|---|
| **Royal Match** (Dream Games, Turquía) | Términos: **≤ 12 no puede usarlo**; 12-17 con un padre que acepte los términos | **Sí**: despacho Kinstellar (Budapest) en el EEE; Dream Games Publishing Ltd (Londres) en el Reino Unido | Anexos por región (EEE, EE. UU., Corea, Turquía). Analítica por interés legítimo; anuncios con consentimiento donde la ley lo pide; cookies esenciales por art. 5.3 y art. 6.1.b/f | Supresión cuando ya no hace falta; bloqueo de lo que se guarda por ley | Licencia revocable; sin reembolso salvo derechos legales; **desistimiento de 14 días en el EEE**; ley del país de residencia en el EEE, la turca fuera; arbitraje en EE. UU. con *opt-out* de 45 días | [F57] |
| **Clash of Clans, Brawl Stars** (Supercell, Finlandia) | No dirigido a menores de **16** (o la edad local, «por ejemplo 14»); con <16, sin redes sociales ni chat y tratamiento limitado a operaciones internas | No hace falta (sede en la UE) | Analítica por interés legítimo; identificadores para anuncios con consentimiento | Por soporte, con verificación; cuentas inactivas desidentificadas | Licencia revocable; compras finales salvo ley; **renuncia al desistimiento** al comprar; ley finlandesa fuera de EE. UU. y Canadá; cita la plataforma ODR, ya cerrada | [F58] |
| **Candy Crush** (King, Malta) | No recoge datos de **<13 o la edad mayor que fije la ley local** | No hace falta (sede en Malta); DPO | Analítica por interés legítimo; publicidad dirigida con consentimiento; *opt-out* de venta o «compartir» en la app para EE. UU. | Formulario; guarda registros financieros y listas de supresión | — | [F59] |
| **Gardenscapes, Homescapes** (Playrix) | Con <18 declarados, sus datos **no se usan para marketing ni publicidad** | Responsable: PLR Worldwide Sales Ltd (país no confirmado) [NV] | — | **En la app**, con confirmación y 15 días para cancelar; peticiones de datos en 30 días | — | [F60] |
| **Magic Sort** (Grand Games, Turquía) | No dirigido a <13 (o <16 en «algunos Estados de la UE») | **No nombra ninguno** | **Consentimiento «explícito» por instalar o usar** el juego y transferencias «aceptadas» al usarlo; ley turca; política de 2023-12-26 | Por correo | — | [F61] |
| **Water Sort Puzzle** | No se localizó la política del desarrollador; queda fuera de la comparativa | — | — | — | — | — |

**Qué copiar:** el modelo de Dream Games (un anexo por región, representantes nombrados, desistimiento del EEE reconocido y la ley del país de residencia para los consumidores del EEE), el borrado en la app de Playrix y la regla de Playrix de no usar datos de menores de 18 para publicidad (Tintalú ya lo hace con 13-17).

**Qué no copiar:** el consentimiento por usar y las transferencias «aceptadas» de Grand Games (el RGPD no admite consentimiento implícito ni como condición de uso), el enlace a la plataforma ODR de Supercell y el arbitraje obligatorio de EE. UU. (añade complejidad y no hace falta para un estudio pequeño).

---

## 9. Respuesta a cada `[[ABOGADO]]` de la web

- **Alcance:** los 40 marcadores de `infra/web/src/content/en/` (el mismo texto, en la misma línea, en `es-419/` y `pt-BR/`), 120 en total; las filas 29 y 30 son añadidos sin marcador. Línea = la de los tres idiomas.
- **El texto propuesto va en español (es-419)**, que es la fuente de verdad de los textos legales (`data-safety.md`); el integrador lo traduce a `en` y `pt-BR`.
- Cada texto **sustituye al marcador** y, donde se indica, a la frase de alrededor.

### 9.1 `privacy.html`

| # | Línea | Marcador | Texto propuesto (es-419) | Fuente | Confianza |
|---|---|---|---|---|---|
| 1 | 6 | delimitar los tratamientos de la editora | «AUTOMATION AI S.A.S publica el juego en Google Play y recibe los pagos de Google. No decide sobre tus datos de juego y no recibe de nosotros datos que te identifiquen. Como titular de la cuenta de desarrollador, puede ver en Play Console la información de pedidos que Google pone a disposición del desarrollador (por ejemplo, el identificador del pedido, el producto, el país y el importe), que usa para su contabilidad y para atender reclamaciones de pago, bajo su propia responsabilidad y la política de Google.» | [F55]; D-048 | riesgo aceptado: qué datos de pedidos ve Play Console no se comprobó [NV] (§12, R9) |
| 2 | 11 | revisar si hace falta representante | Sustituir la frase entera: «Representante en la Unión Europea (art. 27 del RGPD): [nombre y dirección del representante]. Representante en el Reino Unido (art. 27 del UK GDPR): [nombre y dirección]. En Brasil, el contacto es el encarregado (abajo).» **Si el dueño no contrata representante:** «Parallext, LLC no tiene establecimiento en la Unión Europea ni en el Reino Unido. Para cualquier asunto de protección de datos, escribe a privacy@tintalu.com.» | [F2][F10][F11][F13] | alto (hace falta); riesgo aceptado si no se nombra (§12, R1) |
| 3 | 20 y 72 | no personalizar a menores de 18 | Quitar el marcador; el texto se mantiene. La regla cumple la CCPA para 13-15 [F27], el ECA Digital art. 22 [F29], la DPDP de la India [F37] y es lo que hace Playrix [F60] | [F27][F29][F37] | alto |
| 4 | 31 | base legal del UID anónimo | «Ejecución del contrato (el servicio del juego que pides al instalarlo)». | RGPD art. 6.1.b [F1]; LGPD art. 7 V [F32] | alto |
| 5 | 32 | base legal de App Check | «Interés legítimo en proteger las compras y el borrado de cuentas frente al fraude». | RGPD art. 6.1.f; COPPA «operaciones internas» (seguridad) [F21] | alto |
| 6 | 33 | base legal de Remote Config | «Interés legítimo en que el juego funcione con su configuración actual; es necesario para el servicio». | RGPD art. 6.1.f; COPPA FAQ J.5 [F21] | medio |
| 7 | 35 | base legal de Crashlytics | «Interés legítimo en detectar y corregir errores del juego. Los informes no llevan tu identificador de jugador. Puedes desactivarlos en Ajustes › "Privacidad y datos" › "Enviar informes de fallos".» (con T-L6; sin T-L6, sin la última frase) | RGPD art. 6.1.f y 21; ePrivacy art. 5.3 [F16]; COPPA J.5 [F21] | riesgo aceptado (ePrivacy, §12, R5) |
| 8 | 36 | base legal de anuncios no personalizados y del SSV | «Anuncios personalizados: tu consentimiento. Anuncios no personalizados: interés legítimo en financiar un juego gratuito; en el Espacio Económico Europeo, el Reino Unido y Suiza, tu elección en el mensaje de Google, porque incluso estos anuncios usan identificadores para limitar la frecuencia, evitar el fraude y medir. Comprobación de recompensas: interés legítimo en evitar el fraude.» | [F67][F68] | medio |
| 9 | 37 | base legal de compras | «Ejecución del contrato de compra y cumplimiento de obligaciones legales (fiscales y contables)». | RGPD art. 6.1.b y c; LGPD art. 7 II y V | alto |
| 10 | 38 | Q-BILL-2, registros de la biblioteca de pagos | «Google trata estos datos como proveedor del servicio de pagos de Google Play, para que los pagos funcionen y sean seguros, según sus condiciones y su política de privacidad.» | Google: los datos que el sistema de pagos recoge para procesar la transacción no los declara el desarrollador [F56] | medio |
| 11 | 39 | base legal del servidor | «Ejecución del contrato (verificar y restaurar compras, borrar tu cuenta), interés legítimo en prevenir el fraude y la seguridad (registros de acceso) y cumplimiento de obligaciones legales (historial de compras).» | RGPD art. 6.1.b, c y f | alto |
| 12 | 40 | base legal de la partida, la banda y el consentimiento | «Ejecución del contrato (tu partida), obligación legal de poder demostrar tu consentimiento y aplicar la protección que corresponde a tu edad (banda y elecciones) y, si aceptas la analítica, tu consentimiento para enviarlas a Google Analytics.» Además, con T-L4: «Si la banda de edad viene de Google Play (Play Age Signals), no se envía a la analítica.» | RGPD art. 7.1 y 8; [F25] | alto |
| 13 | 41 | base legal de Play Games | «Ejecución del contrato (recuperar tu partida), solo si tienes 13 años o más». | RGPD art. 6.1.b | alto |
| 14 | 51 | Liga, revisar | Quitar el marcador. Corregir la frase del país con lo que haga el código (T-L12) y añadir: «Con 13 a 17 años la Liga muestra lo mismo: un seudónimo automático, sin texto libre, sin chat ni forma de contactar con otros jugadores.» Base: «ejecución del contrato (la Liga, que eliges al jugar) e interés legítimo en evitar trampas». | Play: sin UGC no hay moderación [F53]; ECA Digital art. 21 (interacción limitada por defecto) [F29] | medio |
| 15 | 56 | «no vender ni compartir» | «No vendemos tus datos personales. Si tienes 18 años o más y aceptas los anuncios personalizados, Google puede usar identificadores de tu dispositivo para mostrarte anuncios basados en tu actividad; algunas leyes de estados de EE. UU. llaman a esto "compartir" para publicidad conductual entre contextos. Puedes retirarlo en cualquier momento en Ajustes › "Privacidad y datos", que funciona como la opción "No vender ni compartir mis datos personales". Si tienes menos de 18 años, nunca compartimos tus datos para publicidad personalizada y, en EE. UU., pedimos a Google el tratamiento restringido de datos.» | [F27][F28] | medio |
| 16 | 72 | adolescentes por país (LGPD art. 14 y ECA Digital) | «Además, en los países donde la ley exige el permiso de los padres por debajo de una edad mayor de 13 años (14, 15, 16 o 18, según el país; por ejemplo, Alemania, Francia, España, Colombia, Brasil, la India o Corea del Sur), con 13 a 17 años la analítica no se ofrece y queda apagada. En Brasil, tus padres o tutores pueden supervisar y limitar tus compras con las herramientas de Google (Family Link) y nunca mostramos compras aleatorias.» | [F1][F3][F29][F35][F37][F41] | medio |
| 17 | 79 | rol de Google como socio publicitario | «Para los anuncios, Google actúa como responsable independiente del tratamiento, según sus condiciones de protección de datos entre responsables de Google Ads, y trata los datos de la sección 3 para mostrar y medir anuncios y prevenir el fraude. Para Firebase y Google Play Games, Google actúa como encargado (procesador) por cuenta nuestra.» | [F68][F19] | medio |
| 18 | 82 | jugadores de la Liga, revisar | Quitar el marcador; el texto ya dice lo que ven. | — | alto |
| 19 | 87 | transferencias por ley | «Para los datos del Espacio Económico Europeo, nuestro servidor en Brasil está cubierto por la decisión de adecuación de la Comisión Europea para Brasil (2026). Las transferencias a EE. UU. se apoyan en el Marco de Privacidad de Datos UE-EE. UU. cuando el destinatario está adherido y, en todo caso, en las cláusulas contractuales tipo de la Comisión Europea incluidas en los términos de Google (Firebase) y de Amazon Web Services, con sus anexos para el Reino Unido y Suiza. Para los datos de Brasil, Colombia, Perú, México y el resto de países, usamos esas mismas garantías contractuales, como permite la ley de cada país (por ejemplo, el art. 33 de la LGPD).» | [F17][F18][F19][F20][F32] | medio (riesgo en Brasil → EE. UU., §12, R6) |
| 20 | 95 | plazo fiscal (privacidad) | «7 años» | IRS: 3 años general, 6 si hay ingresos omitidos > 25 %, 7 en ciertos casos [F69]; en Colombia los libros y soportes del comerciante se guardan 10 años (Ley 962/2005, art. 28) [F70], pero son los de la S.A.S, no este registro [E] | medio (confirma el contador) |
| 21 | 104 | Liga (retención), revisar | Quitar el marcador; el texto es correcto si coincide con el TTL del backend (semana + 14 días). | `data-safety.md` | alto |
| 22 | 113 | plazo de respuesta | «en un máximo de **10 días hábiles** (y nunca más de 15 días naturales en Brasil). Si no podemos, te diremos por qué y te responderemos en los 5 días hábiles siguientes, y siempre dentro del plazo que fije la ley de tu país (por ejemplo, un mes en el Espacio Económico Europeo y el Reino Unido, 15 días en Brasil o 45 días en California).» | Colombia arts. 14 y 15 [F41]; LGPD art. 19 [F32]; RGPD art. 12.3 [NV texto]; CCPA [NV] | alto (10 días hábiles cumplen el más estricto de los leídos) |
| 23 | 117 | autoridad de México | «México (LFPDPPP de 2025): tienes derechos ARCO. Si no estás de acuerdo con nuestra respuesta, puedes acudir a la Secretaría Anticorrupción y Buen Gobierno, la autoridad de protección de datos en posesión de particulares.» | [F43] | medio |
| 24 | 118 | derechos en EE. UU. y COPPA | «Estados Unidos: según el estado donde vivas, puedes pedir saber qué datos tenemos, corregirlos, borrarlos, llevártelos y oponerte a la venta, al "compartir" para publicidad conductual y a la elaboración de perfiles, y recurrir nuestra respuesta escribiéndonos de nuevo. No te discriminaremos por ejercerlos. El juego no está dirigido a menores de 13 años y no recoge sus datos: si alguien declara tener menos de 13, el juego se bloquea (sección 6). Antes de la pantalla de edad solo usamos identificadores para el funcionamiento interno del juego: diagnosticar fallos (Crashlytics) y cargar su configuración (Remote Config). No los usamos para publicidad ni para crear perfiles, y el acceso a ellos está limitado por permisos y por contrato con nuestros proveedores.» | [F21][F23][F27] | medio |
| 25 | 128 | borrado en Play Games, revisar | Quitar el marcador; el texto es correcto y no promete lo que no hay (Q-B13-22). | [F54] | alto |
| 26 | 130 | borrado de la Liga, revisar | Quitar el marcador. | [F54] | alto |
| 27 | 142 | aviso simplificado de México | Mantener el texto y añadir al final: «Puedes acudir a la Secretaría Anticorrupción y Buen Gobierno.» | [F43] | medio |
| 28 | 148 | anexo de Brasil | «**Bases legales (art. 7):** ejecución del contrato (cuenta, compras, partida, Liga y Play Games), cumplimiento de obligación legal (historial de compras), interés legítimo (seguridad, fraude, diagnóstico de fallos y configuración) y consentimiento (analítica y anuncios personalizados). **Adolescentes (art. 14 y ECA Digital, Ley 15.211/2025):** tratamos sus datos en su mejor interés, con la configuración más protectora por defecto: con 13 a 17 años, sin analítica, sin anuncios personalizados ni perfiles, con doble confirmación de compras, sin compras aleatorias y sin chat. **Transferencias (cap. V):** nuestro servidor está en Brasil; Google procesa datos en EE. UU. con las garantías contractuales del art. 33 II. **Canal:** privacy@tintalu.com; respondemos en 15 días como máximo.» | [F29][F32] | medio |
| 29 | nueva (sección 1) | — (de §2.2) | Añadir el contacto de la autoridad: «Si vives en el EEE, el Reino Unido o Suiza, puedes reclamar ante la autoridad de protección de datos de tu país.» | RGPD art. 77 [NV texto] | alto |
| 30 | nueva (sección 3) | — (de §3.1) | Bajo «When you open the game», añadir la frase del aviso de operaciones internas (la de la fila 24). | [F23] | medio |

### 9.2 `terms.html`

| # | Línea | Marcador | Texto propuesto (es-419) | Fuente | Confianza |
|---|---|---|---|---|---|
| 31 | 6 | acuerdo entre empresas | «Parallext, LLC es quien te presta el juego y te concede la licencia de uso (sección 3). AUTOMATION AI S.A.S lo publica y lo distribuye en Google Play con licencia de Parallext, LLC. Google Play procesa los pagos con sus condiciones. Para cualquier reclamación sobre el juego o sobre lo que compras en él, escribe a support@tintalu.com: te atenderemos nosotros, sea cual sea la empresa que aparece en tu recibo.» | [F50] §11.1; D-048 | riesgo aceptado: la licencia firmada (§12, R9) |
| 32 | 9 | edad mínima por país | «Tintalú es para personas de 13 años o más. Si tienes entre 13 y 17 años, o eres menor de edad según la ley de tu país, necesitas el permiso de tu madre, padre o tutor para usar el juego y, en especial, para comprar. Si la ley de tu país fija una edad mayor para aceptar estos términos o para tratar tus datos sin permiso de tus padres, se aplica esa edad.» | Dream Games y Supercell [F57][F58]; [F1] | medio |
| 33 | 25 | derechos del consumidor y desistimiento | «Nada de estos términos limita los derechos que te da la ley de protección del consumidor de tu país. Si vives en el Espacio Económico Europeo, el Reino Unido o Suiza, tienes el derecho de desistimiento que fije la ley; al comprar contenido digital que se entrega al momento, Google Play te pide tu consentimiento para la entrega inmediata y la aceptación de que así pierdes ese derecho. Los reembolsos los gestiona Google Play según su política; también puedes escribirnos.» | [F48] principio 5; [F49]; Dream Games [F57] | medio |
| 34 | 30 | retirada de premios y anuncios de terceros | «Si comprobamos que una recompensa no corresponde a un anuncio que viste (por ejemplo, por un fallo o una manipulación), podemos retirar esa recompensa, pero nunca lo que compraste. Los anuncios son de terceros: no somos responsables de su contenido, pero pedimos a Google que sean aptos para todos los públicos y puedes avisarnos de cualquier anuncio inadecuado en support@tintalu.com.» | — | medio |
| 35 | 33 | causas y procedimiento de suspensión | «Podemos suspender o cerrar una cuenta, de forma proporcionada, si hay fraude en las compras, trampas o programas que alteran el juego, abuso del servicio o un uso ilegal. Salvo que haya un riesgo de seguridad o una orden legal, te avisaremos antes en el juego con el motivo y podrás responder en support@tintalu.com en un plazo de 14 días.» | Dream Games (recurso en el EEE) [F57] | medio |
| 36 | 39 | aviso previo y efectos sobre lo comprado | «Si dejamos de ofrecer el juego, te avisaremos en el juego con al menos 60 días de antelación y dejaremos de vender semillas y otros contenidos desde el aviso. Lo que hayas comprado seguirá funcionando hasta el cierre. Si la ley de tu país te da derecho a un reembolso, lo respetaremos.» | Directiva 2019/770 (contenido digital), citada por Dream Games [F57] [NV texto] | medio |
| 37 | 42 | garantías y responsabilidad | «En la medida en que lo permita la ley de tu país, el juego se ofrece "tal cual" y nuestra responsabilidad total frente a ti por el juego se limita a lo que hayas pagado en los 12 meses anteriores a la reclamación. Esto no limita nuestra responsabilidad por dolo o negligencia grave, por daños a la vida o a la salud, ni los derechos que la ley de consumo de tu país no permite excluir.» | Supercell y Dream Games [F57][F58] | medio |
| 38 | 45 | ley aplicable y reclamaciones | «Estos términos se rigen por las leyes del Estado de Wyoming (EE. UU.). Si eres consumidor y vives en otro país, conservas la protección de las normas imperativas de ese país y puedes reclamar ante sus tribunales o sus autoridades de consumo. Antes, escríbenos a support@tintalu.com: intentaremos resolverlo en 30 días.» No se propone arbitraje ni enlace a la plataforma ODR (cerrada). | Dream Games (ley del país de residencia en el EEE) [F57]; [F51] | medio |

### 9.3 `delete.html` y `credits.html`

| # | Archivo y línea | Marcador | Texto propuesto (es-419) | Fuente | Confianza |
|---|---|---|---|---|---|
| 39 | `delete.html`, 28 | datos del teléfono con el borrado web (Q-B13-22) | «Desde esta página no podemos llegar a tu teléfono: la partida guardada en él se borra cuando desinstalas el juego o borras sus datos en Ajustes de Android. La copia en Google Play Games, si la hay, bórrala en la app de Play Games (Ajustes › borrar los datos de este juego).» | [F54] | alto |
| 40 | `delete.html`, 31 | plazo fiscal | «7 años» (igual que la fila 20) | [F69] | medio |
| 41 | `credits.html`, 33 | avisos de licencia de cada componente | «Los avisos de licencia de los componentes de terceros (Unity, Firebase, Google Mobile Ads, Google Play Billing, Play Games y las bibliotecas de código abierto que incluyen) están en el juego, en Ajustes › Créditos y licencias.» Antes, el integrador genera esa lista desde las licencias reales de cada SDK fijado (§11, T-L13). | — | medio (es una comprobación técnica, no legal) |

---

## 10. Respuesta a las consultas de `lanzamiento-mundial.md`

### 10.1 Las 11 consultas de §11

| # | Consulta | Respuesta | Confianza |
|---|---|---|---|
| 1 | Edades de consentimiento digital por país y si basta con no ofrecer la analítica a 13-17 | Tabla de §2.1 y §5. **Sí basta**: con 13-17 solo la analítica usa el consentimiento; los anuncios ya van sin personalizar (o apagados en el EEE, el Reino Unido y Suiza), y el art. 8 solo se aplica al consentimiento [F1] | medio |
| 2 | Representantes UE, Reino Unido y Suiza | UE: **sí**. Reino Unido: **sí**. Suiza: **no** en la 1.0 (§2.2, §2.3) | alto/medio |
| 3 | Bases de lo que corre sin consentimiento y ePrivacy 5.3 | Contrato para la cuenta, la nube y la Liga; interés legítimo para App Check, Remote Config y Crashlytics (§9, filas 4-13). ePrivacy y Crashlytics: interruptor en Ajustes y riesgo aceptado (§2.4) | medio / riesgo aceptado |
| 4 | COPPA: pantalla neutral, operaciones internas y audiencia mixta | La pantalla cumple las pautas de la FTC; lo que corre antes es «operación interna» y hay que decirlo en la política; evitar estética infantil en la ficha (§3.1) | medio |
| 5 | Leyes estatales, GPC, *opt-in* de 13-15, leyes de las tiendas y Age Signals | §3.2 y §3.3: aviso en la política, `rdp=1` con 13-17 en EE. UU., Play Age Signals por Texas (en vigor) | medio |
| 6 | ECA Digital y LGPD adelantados a la 1.0 | §4. Lanzar con Age Signals, `BR` = 18 y sin perfilado; arts. 24 y 40 como riesgos aceptados | medio / riesgo aceptado |
| 7 | Transferencias UE → Brasil y UE → EE. UU. | UE → Brasil: adecuación desde enero de 2026; UE → EE. UU.: DPF y cláusulas tipo de Firebase y AWS (§2.5) | alto/medio |
| 8 | Principios CPC y guías de la CMA y la ASA | §6. Las guías británicas de la CMA y la ASA no se leyeron [NV]; los principios CPC y la multa de la ACM cubren lo esencial | medio |
| 9 | Países a excluir | RU, BY, **VN** e **ID** (§5) | medio |
| 10 | Términos mundiales: ley aplicable, consumo y compras de menores | §9.2, filas 31-38 | medio |
| 11 | IARC: ¿mostrar el país en la Liga es compartir la ubicación? | No hay fuente oficial de IARC sobre esto [NV]. Recomendación prudente: **responder «sí, comparte la ubicación a nivel de país»**, como ya hace `iarc.md`; puede subir algo la clasificación, pero declararlo de menos es peor | riesgo aceptado |

### 10.2 Los puntos «abogado» de §1-§7 y §9

| § | Punto | Respuesta |
|---|---|---|
| 1.1 | Bandas {<13, 13-17, 18+} como mínimo mundial | Se mantienen; COPPA admite el bloqueo de <13 en audiencia general [F21] |
| 1.3 | `default` como tratamiento mundial | Válido: *opt-in* para analítica y anuncios personalizados es más que lo exigido en EE. UU. [F27] y que lo leído de Japón y Australia |
| 1.4 | Tabla del EEE | §2.1 |
| 1.6 | Operaciones internas y Age Signals | §3.1 y §3.2 |
| 1.7 | Brasil, Corea, India, Reino Unido (Children's Code), Canadá, Australia | §4 y §5. Reino Unido: con 13-17 sin anuncios y sin analítica salvo *opt-in* (UK = 13), con «Rechazar todo» al mismo nivel; el Children's Code no se leyó entero [NV] |
| 1.8 | Compras de menores | Doble confirmación + autenticación de Play + permiso de los padres en los términos (§9, fila 32); en la UE el desistimiento lo gestiona Play [F49] |
| 5.2 | VN e ID | Excluir (§5) |
| 5.3 | IARC y la Liga | §10.1, fila 11 |
| 5.5 | Mochila y CPC | §6.1 y §6.4 |
| 5.6 | Acta Europea de Accesibilidad | Exentas las microempresas de servicios [F52] |
| 6.1 | Base de las transferencias | §2.5 |
| 6.2 | Firebase en EE. UU. y retención de Analytics | Cláusulas tipo de Firebase [F19]; retención de 2 meses (§11, T-L11) |
| 7.1 | Representantes | §2.2 y §2.3 |
| 7.2 | Anexos regionales | §9 (filas 15, 16, 19, 22-24, 28 y 29) |

### 10.3 Pendientes de `data-safety.md`

| Pendiente | Respuesta |
|---|---|
| Crashlytics y Remote Config antes del consentimiento | «Opcional: no» mientras no exista el interruptor; con T-L6, Crashlytics pasa a «opcional: sí» |
| Plazo fiscal del libro mayor | 7 años (§9, fila 20) |
| IP de los registros | No se declara por sí sola: no se deriva ubicación; sí «Ubicación aproximada» por el país de CloudFront de la Liga (§7.5). **Resuelto (T-L12, 2026-10-27):** la Liga no usa CloudFront; el país lo manda el teléfono (SIM o región) y no se declara «Ubicación aproximada» por la conexión (§7.5; `data-safety.md`) |
| Registros de Play Billing (Q-BILL-2) | No se declaran: los recoge el sistema de pagos para procesar la transacción [F56] |
| Liga: categoría «compartido» | Mantener «compartido» para el seudónimo, la bandera y los puntos, como propone el borrador; la excepción por acción del usuario no está clara [NV] |

---

## 11. Cambios de código, configuración y textos (tareas)

Cada tarea sigue el flujo de CLAUDE.md (rama, worktree, verificación, revisión, PR). Ninguna la hace este carril.

| ID | Tarea | Quién | Detalle | Depende de |
|---|---|---|---|---|
| **T-L1** | Clave `consent_digital_age_by_country` y la regla de 13-17 (es la T-M1 de `lanzamiento-mundial.md` §12, con estos valores) | integrador (08 §9) + unity-gameplay-dev + level-pipeline-dev (`rc_defaults.py --check`) | Valor por defecto: `{"*":13, "AT":14, "BE":13, "BG":14, "CY":14, "CZ":15, "DE":16, "DK":13, "EE":13, "ES":14, "FI":13, "FR":15, "GR":15, "HR":16, "HU":16, "IE":16, "IS":13, "IT":14, "LI":16, "LT":14, "LU":16, "LV":13, "MT":13, "NL":16, "NO":15, "PL":16, "PT":13, "RO":16, "SE":13, "SI":16, "SK":16, "UK":13, "CH":16, "KR":14, "IN":18, "BR":18, "CO":18, "MX":18, "PE":14, "CA":14, "JP":16}`. Claves ISO alfa-2 con `UK` para el Reino Unido, como `biz_legal_profile_by_country` (`ConsentConfig.UkKey`). Regla: con la banda 13-17 y una edad > 13, la analítica no se ofrece y queda apagada. Sin país, `*`. Rango 13-18 | edición de 08 §9 |
| **T-L2** | Excluir países en Play Console | dueño | Todos los países de Play **salvo RU, BY, VN e ID** (China no aparece) | — |
| **T-L3** | Play Age Signals API 0.0.4 | **dueño aprueba la dependencia** (regla dura 4); sdk-integrator + unity-gameplay-dev | `consent_age_signal_source` = `self_declared+play_age_signals`. Llamada en cada arranque con la app abierta; 0-12 → bloqueo; 13-15 y 16-17 → banda 13-17; sin señal → la autodeclarada. En Play Console, declarar las clasificaciones por edad de los productos (Texas) [F24]. **Dependencia (2026-10-27):** `com.google.android.play:age-signals:0.0.4` (beta; la 0.0.3 es la anterior), minSdk 23, sin permisos documentados; `AgeSignalsManagerFactory.create(context).checkAgeSignals(AgeSignalsRequest.builder().build())` → `Task<AgeSignalsResult>`; errores en `AgeSignalsException` con `model.AgeSignalsErrorCode` [F75]. Comprobado con `javap` sobre el AAR 0.0.4 resuelto: `AgeSignalsResult` tiene `ageLower()`, `ageUpper()` (null en 18+), `ageRangeSource()`, `significantChangeStatus()`, `significantChangeApprovalDate()` e `installId()`, **sin** `userStatus()`; su manifiesto no declara permisos (solo la actividad no exportada `AgeSharingConsentWrapperActivity`) y no trae bibliotecas nativas. Edades null = sin señal | aprobación del dueño (dada: D-057, añadido del 2026-10-27) |
| **T-L4** | No mandar a Analytics la banda que venga de Age Signals | unity-gameplay-dev | `af_consent.age_band` y propiedades derivadas solo con la banda autodeclarada; si la banda vino o se corrigió con la señal, no se envía [F25] | T-L3 |
| **T-L5** | `rdp=1` con 13-17 en EE. UU. | sdk-integrator (`GoogleAdService`, `NativeGmaPort`) | Añadir el extra `rdp=1` a cada petición de anuncios cuando la banda es 13-17 y el país legal es `US` [F28]. Tests | — |
| **T-L6** | Interruptor «Enviar informes de fallos» | unity-gameplay-dev (+ textos en `strings.csv` por herramienta) | En Ajustes › «Privacidad y datos», activado por defecto; apagado → `SetCrashlyticsCollectionEnabled(false)` y no se envían los pendientes. Cambia Q-B12-4 («línea sin interruptor»): **lo aprueba el dueño** | aprobación del dueño |
| **T-L7** | Representantes UE y Reino Unido | dueño | Contratar un servicio de representación del art. 27 (UE y Reino Unido); publicar nombre y dirección en la política (§9, fila 2) | decisión y pago del dueño |
| **T-L8** | Textos de la web | integrador (con la verificación de `infra/`: `npm ci`, lint, typecheck, tests, `cdk synth`) | Sustituir los 40 marcadores de §9 (y añadir las filas 29 y 30) en `es-419` y traducirlos a `en` y `pt-BR`; subir la versión de la política y `consent_text_version`; quitar la frase de la UE de la línea 11 **aunque no haya representante todavía** | T-L7 para la fila 2 |
| **T-L9** | Revisión de la tienda frente a la ACM | unity-gameplay-dev + dueño | La Mochila de 48 h desaparece de verdad al llegar a cero y no vuelve enseguida (05 §4.1); ningún texto de la tienda exhorta a comprar ya («¡Compra ya!», «¡Consíguelo ahora!») [F64] | — |
| **T-L10** | Precio en dinero real y tamaños de pack (CPC 1 y 3) | dueño (economía) | Decidir si el Mercado del EEE y del Reino Unido muestra un «≈ x €» informativo junto a los precios en semillas, y revisar que los packs no obliguen a comprar mucho más de lo que cuesta lo habitual [F48] | — |
| **T-L11** | Consola de Firebase, Analytics y AdMob | dueño | Aceptar los términos de tratamiento de datos de Firebase y de Google Analytics [F19]; retención de Analytics en **2 meses**; en AdMob, los mensajes del RGPD (EEE, Reino Unido y Suiza) y de EE. UU. publicados (D-049) [F68] | — |
| **T-L12** | País de la Liga: alinear el texto con el código | integrador | Comprobar si la Liga usa el país de CloudFront o la región del teléfono y dejar igual la política (fila 14) y `data-safety.md`; si es CloudFront, Data Safety declara «Ubicación aproximada» | — |
| **T-L13** | Avisos de licencia | build-release o integrador | Lista real de licencias de los SDK fijados (Unity, Firebase 13.17.0, GMA 11.5.0, Play Billing 9.0.0, Play Games 2.3.0) en Créditos | — |
| **T-L14** | Ficha de Play sin captación infantil | dueño | Público objetivo 13-15, 16-17 y 18+; capturas de juego, sin lemas ni estética dirigidos a niños [F53] | — |
| **T-L15** | GDD | integrador | 05 §7 (edad digital por país, Age Signals, RDP, Crashlytics con interruptor), 08 §9 (la clave nueva) y 08 §13.1 (países excluidos) | T-L1, T-L3, T-L6 |

Verificación de las tareas de código: la de CLAUDE.md. Con T-L1, además, `python tools/remote-config/rc_defaults.py --check` y sus tests.

---

## 12. Riesgos que quedan (para que el dueño los acepte)

| ID | Riesgo | Probabilidad e impacto [E] | Qué lo reduce | Recomendación |
|---|---|---|---|---|
| **R1** | Lanzar sin representante en la UE o el Reino Unido | Baja probabilidad de inspección con poco volumen; precedente de 525.000 € [F13] | T-L7 (≈ 300-750 €/año) | **No aceptar**: contratar el representante |
| **R2** | Que la FTC considere el juego «de audiencia mixta» por su estética | Baja; impacto alto (COPPA) | Pantalla neutral, bloqueo, ficha sin captación infantil (T-L14), sin anuncios personalizados con 13-17 | Aceptar con T-L14 |
| **R3** | ECA Digital art. 24: cuentas de menores de 16 sin vínculo con un responsable | Media en Brasil desde finales de 2026; multa por usuario | Age Signals (T-L3), `BR` = 18, sin perfilado ni loot boxes, proporcionalidad del art. 39 | Aceptar y revisar con la guía definitiva de la ANPD |
| **R4** | ECA Digital art. 40: sin representante legal en Brasil | Media; obligación modulada por tamaño (art. 39) | Nombrar uno si Brasil crece | Aceptar en la 1.0 |
| **R5** | Crashlytics y Remote Config antes del consentimiento en el EEE (ePrivacy 5.3) | Baja-media; la CNIL vigila los SDK de apps [F15][F65] | Interruptor (T-L6), sin UID, 90 días | Aceptar con T-L6 |
| **R6** | Brasil → EE. UU. (Firebase) sin cláusulas tipo de la ANPD comprobadas | Baja | Cláusulas tipo de Google si las ofrece [NV] | Aceptar |
| **R7** | Texas, Utah, Luisiana y Alabama si no se integra Age Signals | Media en Texas (en vigor) | T-L3 | **No aceptar**: integrar T-L3 |
| **R8** | Turquía: VERBIS sin representante | Baja-media; las autoridades turcas investigaron a miles de organizaciones en 2024 [F44] | Representante en Turquía o excluir TR | Decide el dueño: aceptar y revisar si Turquía pasa del 3 % de las instalaciones, o excluir |
| **R9** | Licencia Parallext → S.A.S sin firmar (DDA §11.1) y qué datos ve la editora en Play Console | Media; Google podría pedir pruebas | Firmar la licencia de `acuerdo-publicacion-borrador.md` | Firmar antes de publicar |
| **R10** | Edades sin fuente primaria (LI, CH, JP, MX, PE) | Baja; se tomó el valor más alto | — | Aceptar |
| **R11** | Japón: saldo prepagado > 10 M JPY | Muy baja al principio | Vigilar el saldo de semillas compradas sin gastar | Aceptar y vigilar |
| **R12** | Corea: agente local si se pasa de 1.000 instalaciones diarias de media | Baja al principio | Vigilar | Aceptar y vigilar |
| **R13** | Principios CPC: precio solo en semillas | Baja para un estudio pequeño; la red actúa contra grandes | T-L10 | Decide el dueño |
| **R14** | Términos sin revisión de un abogado (ley de Wyoming, límites de responsabilidad) | Baja; las cláusulas ceden ante las normas imperativas de consumo | Textos de §9.2, prudentes | Aceptar |
| **R15** | Australia: código de privacidad infantil a fin de 2026 | Baja | Revisar al publicarse | Aceptar |

---

## 13. Fuentes

Todas consultadas el **2026-10-27**. «Secundaria» = despacho, prensa o proveedor; el resto, texto de ley, organismo oficial o documentación del proveedor del servicio.

- [F1] RGPD, art. 8 (texto): https://gdpr-info.eu/art-8-gdpr/
- [F2] RGPD, art. 27 (texto): https://gdpr-info.eu/art-27-gdpr/
- [F3] GDPR Local, «Digital Age of Consent under the GDPR», tabla por país con la ley citada, actualizada el 15-10-2026 (secundaria): https://gdprlocal.com/digital-age-of-consent-under-the-gdpr/
- [F4] sota.io, guía del art. 8 con referencias legales (secundaria): https://sota.io/blog/gdpr-art-8-childrens-consent-age-verification-parental-authorization-digital-services-developer-guide-2026
- [F5] Bird & Bird, Children in the Digital World, Eslovenia (secundaria): https://childreninthedigitalworld.twobirds.com/home/slovenia
- [F6] Bird & Bird, Islandia (secundaria): https://childreninthedigitalworld.twobirds.com/home/iceland ; Ísland.is: https://island.is/en/processing-of-personal-data-of-children/permission-to-process-personal-data-from-a-parent-or-child
- [F7] Gobierno de Noruega, ley de datos personales: https://www.regjeringen.no/no/tema/statlig-forvaltning/personvern/nye-personvernregler-i-eu/id2340094/ ; propuesta de subir a 15 (notificación TRIS 2025/9028/NO, según resultados de búsqueda; texto no leído [NV])
- [F8] White & Case, implementación del RGPD en Malta (secundaria): https://www.whitecase.com/insight-our-thinking/gdpr-guide-national-implementation-malta
- [F9] ICO, «What are the rules about an ISS and consent?»: https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/childrens-information/children-and-the-uk-gdpr/what-are-the-rules-about-an-iss-and-consent/
- [F10] EDPB, Directrices 3/2018 sobre el ámbito territorial (12-11-2019): https://edpb.europa.eu/sites/edpb/files/files/file1/edpb_guidelines_3_2018_territorial_scope_en.pdf (leídas en https://gdpr-text.com/en/guidelines/territorialscope/)
- [F11] ICO, «Receiving personal information from the EEA» (representante británico): https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/international-transfers/receiving-personal-information-from-the-eea/ ; guía de transferencias (15-01-2026): https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/international-transfers/international-transfers-a-guide/
- [F12] FDPIC (Suiza), representantes del art. 14 nLPD: https://www.edoeb.admin.ch/en/representatives-in-accordance-with-article-14-fadp ; texto: https://justement.ch/en/doc/act/ch/235_1/chap_2/sec_2/art_14
- [F13] Hunton, multa de la AP a Locatefamily.com (secundaria): https://www.hunton.com/privacy-and-cybersecurity-law-blog/dutch-dpa-fines-company-525000-eur-for-failure-to-designate-eu-representative ; Bird & Bird: https://www.twobirds.com/en/insights/2021/netherlands/finding-locatefamily-com-dutch-dpa-imposes-525000-euro-fine-for-not-having-a-gdpr-representative
- [F14] Precios de servicios de representación (proveedores, [E]): https://prighter.com/product/gdprrep/services ; https://itgovernance.co.uk/shop/product/gdpr-eu-representative ; https://www.legiscope.com/blog/gdpr-article-27-non-eu-representatives.html
- [F15] CNIL, recomendación sobre aplicaciones móviles (2024): https://www.cnil.fr/sites/cnil/files/2024-09/recommandation-applications-mobiles.pdf
- [F16] EDPB, Directrices 2/2023 sobre el alcance técnico del art. 5.3 ePrivacy, v2.0 (7-10-2024): https://edpb.europa.eu/system/files/2024-10/edpb_guidelines_202302_technical_scope_art_53_eprivacydirective_v2_en_0.pdf
- [F17] White & Case, adecuación mutua UE-Brasil (secundaria): https://www.whitecase.com/insight-alert/mutual-adequacy-between-eu-and-brazil-new-era-transatlantic-data-transfers ; Trench Rossi, Resolución CD/ANPD n.º 32: https://www.trenchrossi.com/en/legal-alerts/brazil-and-the-european-union-mutually-recognize-adequacy-in-personal-data-protection-resolution-no-32-dated-january-26-2026/
- [F18] IAPP, sentencia Latombe (secundaria): https://iapp.org/news/a/european-general-court-dismisses-latombe-challenge-upholds-eu-us-data-privacy-framework ; recurso C-703/25 P (WilmerHale): https://www.wilmerhale.com/en/insights/blogs/wilmerhale-privacy-and-cybersecurity-law/20251201-european-court-of-justice-to-review-challenge-to-eu-us-data-privacy-framework
- [F19] Firebase, términos de tratamiento y cláusulas tipo: https://firebase.google.com/terms/data-processing-terms ; https://firebase.google.com/terms/firebase-sccs-eu-c2p
- [F20] AWS, centro RGPD (DPA con cláusulas tipo de 2021): https://aws.amazon.com/compliance/gdpr-center/
- [F21] FTC, «Complying with COPPA: Frequently Asked Questions»: https://www.ftc.gov/business-guidance/resources/complying-coppa-frequently-asked-questions
- [F22] Federal Register, regla COPPA enmendada (22-04-2025): https://www.federalregister.gov/documents/2025/04/22/2025-05904/childrens-online-privacy-protection-rule ; FTC: https://www.ftc.gov/legal-library/browse/federal-register-notices/16-cfr-part-312-coppa-final-rule-amendments
- [F23] Latham & Watkins, aviso de operaciones internas en la regla de 2025 (secundaria): https://www.lw.com/en/insights/2025/05/ftc-publishes-updates-to-coppa-rule
- [F24] Google Play Console, leyes de las tiendas en EE. UU.: https://support.google.com/googleplay/android-developer/answer/16569691?hl=en
- [F25] Android Developers, Play Age Signals API (actualizada el 20-07-2026; términos de octubre de 2025): https://developer.android.com/google/play/age-signals/overview
- [F26] Future of Privacy Forum, leyes de las tiendas aprobadas (3-06-2026, secundaria): https://fpf.org/blog/comparing-enacted-app-store-accountability-acts/
- [F27] CCPA §1798.120: https://prighter.com/resources/laws/ccpa/sections/1798-120 ; CPPA, umbrales de 2025: https://www.cppa.ca.gov/regulations/cpi_adjustment.html
- [F28] AdMob, leyes de estados de EE. UU. (Android): https://developers.google.com/admob/android/privacy/us-states ; RDP: https://support.google.com/admob/answer/9598414?hl=en
- [F29] Ley 15.211/2025 (ECA Digital), texto: https://www.planalto.gov.br/ccivil_03/_ato2023-2026/2025/lei/L15211.htm
- [F30] Mayer Brown, aplicación del ECA Digital (abril de 2026, secundaria): https://www.mayerbrown.com/pt/insights/publications/2026/04/enforcement-of-brazils-eca-digital-introduces-new-obligations-for-companies
- [F31] ANPD, borrador de guía de verificación de edad (ppc.land, 23-05-2026, secundaria): https://ppc.land/brazils-anpd-releases-draft-age-verification-guide-open-to-public-input/ ; orientaciones preliminares del 20-03-2026 (Mobile Time): https://www.mobiletime.com.br/noticias/20/03/2026/anpd-diretrizes-afericao/
- [F32] LGPD (Ley 13.709/2018), texto: https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm
- [F33] Resolución CD/ANPD n.º 18/2024 (encarregado): https://www.tjba.jus.br/extrajudicial/wp-content/uploads/2024/08/RESOLUCAO-ANPD-No-18-Encarregado-de-Dados.pdf
- [F34] Android Developers, Age Signals en Brasil: https://developer.android.com/google/play/age-signals/use-age-signals-api ; ampliación mundial (BetaNews, secundaria): https://betanews.com/article/google-play-age-signals-api-global-rollout/
- [F35] PIPA de Corea, art. 22-2: https://prighter.com/resources/laws/korean-pipa/main/articles/article-22-2 ; art. 31-2: https://prighter.com/resources/laws/korean-pipa/main/articles/article-31-2
- [F36] Korea Herald, agente local de los juegos extranjeros (secundaria): https://www.koreaherald.com/article/10593422 ; MCST: https://www.mcst.go.kr/english/policy/pressView.jsp?pSeq=515
- [F37] SCC Online, reglas DPDP 2025 (secundaria): https://www.scconline.com/blog/post/2025/12/26/digital-personal-data-protection-rules-2025-key-highlights/
- [F38] FSA de Japón, medios de pago prepagados: https://www.fsa.go.jp/news/21/kinyu/20100223-1/1-2.pdf
- [F39] OAIC, borrador del código de privacidad infantil: https://www.oaic.gov.au/engage-with-us/consultations/draft-childrens-online-privacy-code-consultation-for-industry,-civil-society,-academia ; pequeña empresa: https://www.oaic.gov.au/privacy/privacy-guidance-for-organisations-and-government-agencies/organisations/small-business
- [F40] Dialog Insight, Ley 25 de Quebec y menores de 14 (secundaria): https://www.dialoginsight.com/en/blog/law-25-en/impact-of-law-25-on-consents
- [F41] Ley 1581 de 2012 (Colombia), texto: https://www.funcionpublica.gov.co/eva/gestornormativo/norma.php?i=49981 ; Decreto 1377/2013, art. 12, según resultados de búsqueda (secundaria)
- [F42] Perú, DS 016-2024-JUS (secundaria, sin el texto oficial): https://modelo.pe/carta-de-consentimiento-informado-para-tratamiento-de-datos-personales-de-menores-de-edad/
- [F43] EY, nueva LFPDPPP (secundaria): https://www.ey.com/content/dam/ey-unified-site/ey-com/es-mx/technical/tax/documents/ey-law-flash-nueva-lfpdppp.pdf
- [F44] Prighter, VERBIS de Turquía (proveedor): https://prighter.com/resources/turkish-kvkk-verbis-registration/
- [F45] PocketGamer.biz, Decreto 147/2024 de Vietnam (secundaria): https://www.pocketgamer.biz/vietnam-issues-decree-1472024-to-regulate-online-games-and-internet-services
- [F46] Rajah & Tann, IGRS de Indonesia (secundaria): https://www.rajahtannasia.com/viewpoints/from-law-to-action-introducing-the-indonesia-game-rating-system-igrs/ ; Cekindo, PSE: https://www.cekindo.com/blog/pse-license-and-igrs
- [F47] Google Play Console, cobros en Rusia y Bielorrusia: https://support.google.com/googleplay/android-developer/answer/11950272?hl=en
- [F48] Reed Smith, principios CPC sobre monedas virtuales (secundaria): https://www.reedsmith.com/articles/qas-on-the-eu-consumer-protection-authorities-joint-guidance-paper/ ; Comisión Europea: https://commission.europa.eu/news/european-commission-hosts-stakeholders-talks-application-cpc-networks-key-principles-games-virtual-2025-06-03_en
- [F49] Google Play, reembolsos en el EEE y el Reino Unido: https://support.google.com/googleplay/answer/15576634?hl=en
- [F50] Google Play, Developer Distribution Agreement: https://play.google/developer-distribution-agreement.html
- [F51] CMS, cierre de la plataforma ODR (secundaria): https://cms.law/en/deu/legal-updates/the-odr-platform-is-closing-what-businesses-need-to-know
- [F52] Directiva 2019/882, art. 4: https://www.legislation.gov.uk/eudr/2019/882/article/4
- [F53] Play Console, público objetivo y contenido: https://support.google.com/googleplay/android-developer/answer/9867159?hl=en ; política de familias: https://support.google.com/googleplay/android-developer/answer/9893335?hl=en
- [F54] Play Console, requisitos del borrado de cuenta: https://support.google.com/googleplay/android-developer/answer/13327111?hl=en
- [F55] Play Console, política de datos de usuario: https://support.google.com/googleplay/android-developer/answer/10144311?hl=en
- [F56] Play Console, sección de Seguridad de los datos: https://support.google.com/googleplay/android-developer/answer/10787469?hl=en
- [F57] Dream Games, términos (4-03-2026): https://www.dreamgames.com/en/terms ; anexo EEE+: https://www.dreamgames.com/en/eea-privacy
- [F58] Supercell, privacidad (11-03-2026): https://www.supercell.com/en/privacy-policy/ ; términos (6-11-2024): https://www.supercell.com/en/terms-of-service/
- [F59] King, privacidad (26-02-2026): https://king.com/privacyPolicy
- [F60] Playrix, borrado de cuenta (centro de ayuda): https://playrix.helpshift.com/hc/en/5-gardenscapes/faq/15890-account-data-deletion-1759410287/?l=en ; privacidad (resultado de búsqueda; la página no se pudo leer): https://playrix.com/privacy/
- [F61] Grand Games, privacidad (26-12-2023): https://grand.gs/privacy
- [F62] FTC, Epic Games (diciembre de 2022): https://www.ftc.gov/news-events/news/press-releases/2022/12/fortnite-video-game-maker-epic-games-pay-more-half-billion-dollars-over-ftc-allegations
- [F63] FTC, HoYoverse/Genshin Impact (17-01-2025): https://www.ftc.gov/news-events/news/press-releases/2025/01/genshin-impact-game-developer-will-be-banned-selling-lootboxes-teens-under-16-without-parental
- [F64] ACM, multa a Epic por Fortnite: https://www.acm.nl/en/publications/acm-imposes-fine-epic-unfair-commercial-practices-aimed-children-fortnite-game
- [F65] CNIL, multa a Voodoo: https://cnil.fr/en/node/8705
- [F66] Belga, denuncia de BEUC sobre monedas virtuales (12-09-2024, secundaria): https://www.belganewsagency.eu/consumer-organisations-file-complaint-against-game-developers-with-european-commission
- [F67] AdMob, anuncios personalizados y no personalizados: https://support.google.com/admob/answer/7676680?hl=en
- [F68] Google, RGPD y condiciones entre responsables: https://business.safety.google/gdpr/ ; AdMob, herramientas RGPD: https://support.google.com/admob/answer/7666366?hl=en
- [F69] IRS, cuánto tiempo guardar los registros: https://www.irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records
- [F70] Conservación de libros y papeles en Colombia, Ley 962/2005, art. 28 (secundaria): https://accounter.co/niif/conservacion-y-disposicion-libros-concepto-ctcp-116-de-2022
- [F71] Stevens & Bolton, la DUAA y las cookies (secundaria): https://www.stevens-bolton.com/insights/102mqbh/the-duaa-cookies-what-is-changing-and-what-you-need-to-know/
- [F72] Usercentrics, plazos ARCO de la nueva LFPDPPP (secundaria): https://usercentrics.com/es/knowledge-hub/lfpdppp-mexico/
- [F74] AEPD, edad para consentir en España (14 años): https://www.aepd.es/preguntas-frecuentes/10-menores-y-educacion/FAQ-1001-cual-es-la-edad-para-que-los-menores-puedan-prestar-consentimiento-para-tratar-sus-datos-personales
- [F75] Android Developers, «Use Play Age Signals API» (actualizada el 2026-10-23; consultada el 2026-10-27 por el integrador): https://developer.android.com/google/play/age-signals/use-age-signals-api

### Sanciones y errores conocidos (resumen)

| Caso | Qué pasó | Lección para Tintalú | Fuente |
|---|---|---|---|
| FTC contra Epic (2022) | 275 M USD por la COPPA (voz y texto activados por defecto para niños) y 245 M USD por *dark patterns*: un solo botón cobraba sin querer | Nada activado por defecto con menores; confirmar las compras (ya hay doble confirmación con 13-17); sin chat | [F62] |
| FTC contra HoYoverse (2025) | 20 M USD; prohibido vender loot boxes a menores de 16 sin padres; probabilidades y coste real engañosos; varias monedas confusas | Sin loot boxes y una sola moneda | [F63] |
| ACM contra Epic (2024) | 1.125.000 €: exhortaciones a niños y temporizadores falsos | T-L9 | [F64] |
| CNIL contra Voodoo (2022) | 3 M €: leía el IDFV para publicidad aunque el usuario había rechazado el seguimiento | Ningún identificador publicitario sin consentimiento: `AD_ID` solo con 18+ y consentimiento (D-055) | [F65] |
| AP contra Locatefamily.com (2021) | 525.000 € por no tener representante en la UE | T-L7 | [F13] |
| BEUC (2024) | Denuncia contra siete editores por precios solo en moneda virtual | T-L10 | [F66] |
