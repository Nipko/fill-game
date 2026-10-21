# Mercado, ofertas y Cántaro (bloque B5 de la tanda 5)

Bloque B5 de `docs/dev/plan-tanda5.md` §4: la pantalla del Mercado, la Mochila de inicio, Sin anuncios, el Cántaro, Canoas infinitas 1 h, la tarjeta de skin del mundo, el `offer_scheduler` y la pestaña Cuias que elige la skin puesta. Todo con la tienda simulada (D-008): ni SDK, ni paquetes, ni permisos, ni red. Rama `spark/t5-b5`.

Fuentes: GDD 05 §4.1, §4.2, §5 y §10; 06 §2.1, §2.2, §4.5 y §4.7; 08 §4.3, §8.1, §8.2 y §9; 04 §6.2; glosario §2.1, §7.1 y §10. Decisiones: D-033 (Q-E2, Q-E3, Q-E5, Q-E6, Q-M4 y Q-M8, todas (a)), D-029 #23 y D-035 (F3-3).

## 1. Qué hace

### 1.1 El Mercado (`AF.UI.MarketScreen`, `MarketModel`)
- **Entrada:** un icono en la barra de abajo del inicio, a la izquierda de «Inicio», simétrico al del Mapa (06 §4.7: «iconos Mercado / Mapa del río», sin texto). Sale desde `max_level` ≥ 10 (06 §2.2, fila 10; 01 §7.1; 05 §2.1) y con la tienda encendida (`biz_iap_enabled`). El glifo es provisional (la semilla) hasta el «puesto de Ver-o-Peso» de 07 §5.
- **Pantalla:** tarjeta de madera a pantalla casi completa en su propio lienzo (orden 30: encima del inicio y de las tarjetas de oferta, debajo de Ajustes y de la hoja de compra). Título «Mercado» (glosario §10, `shop`), saldo de semillas, pestañas de 06 §4.7 y una fila por producto con icono, nombre de tienda (`iap_<sku>_name`), contenido y botón con el precio local de la tienda (`IStoreService.PriceOf`; la UI no calcula precios).
- **Pestañas** (las vacías no salen):

| Pestaña | Filas | Cuándo |
|---|---|---|
| Semillas | Cántaro; Puñado, Bolsa, Saco, Costal y Cosecha (`af_coins_2` … `af_coins_6`, `eco_seed_pack_amounts`) | Siempre; el Cántaro desde `eco_piggy_from_level` (20) |
| Comodines | Mochila de inicio (con su temporizador una vez empezado); Canoas infinitas 1 h | La Mochila en su ventana (`eco_starter_window_levels` = [10,20]: desde que se gana el 10 hasta que se gana el 20, sin vencer ni comprar); las Canoas desde `rescue_min_level` (31; 05 §10) |
| Cuias | Cuia de vidrio y las skins de los mundos 2-4 | Siempre: «Poner» / «Puesta» para las que tiene (`SkinService.Select`, D-033 Q-M8 (a)); las demás, «Premio del mundo · faltan N»; con la venta de skins encendida (Q-B5-1), con su precio |
| Sin anuncios | Sin anuncios (+`eco_noads_bonus_seeds`) o «Ya no ves anuncios entre niveles» | Desde el nivel 22 (05 §4.1) o ya comprado |

- La **Balsa de rescate** no se vende en el Mercado: solo en Aguas estancadas (05 §4.1).
- **«Restaurar compras»** no sale hasta el adaptador real de IAP (Q-E5 (a) de D-033).
- **Comprar** pasa por `IStoreService.Purchase` con `context = shop` (la hoja «Compra simulada»). Una skin comprada se pone sola, como la ganada [P] (Q-B5-13).

### 1.2 El Cántaro (`AF.IAP.PiggyJar`)
- Se ve y empieza a llenarse en `eco_piggy_from_level` (20). Cada victoria **de campaña** desde ese nivel le suma `eco_piggy_fill` (+15 / +30 / +40 por normal, Difícil y Muy difícil) hasta `eco_piggy_cap` (3.000, «¡Lleno!»). El Reto del río no lo llena [P] (Q-B5-5). La misma victoria no suma dos veces (`piggy.last_level`).
- Se compra desde `eco_piggy_min_buy` (1.000) y da exactamente lo acumulado (`PurchaseRequest.PiggySeeds`; `StoreLedger` lo acredita con `Wallet.Apply`). Al acreditarse (`Purchased`) se vacía y vuelve a llenarse. Con `Pending` no se vacía (Q-B5-12).
- El tope en vigor es siempre `eco_piggy_cap`: si Remote Config lo baja por debajo de lo guardado, el Cántaro queda lleno con el tope nuevo y se vende el tope.
- **Icono:** en la esquina de arriba a la derecha del inicio, bajo Ajustes [P] (Q-B5-7), donde no tapa los saldos ni la ventanita de casa de TiTino; con lo que guarda; lleno, «¡Lleno!» con un punto. Solo con la tienda encendida. Un toque abre su tarjeta (05 §5: «≥ 1.000 y toque en el icono»), con el botón activo desde 1.000; no gasta el cupo de takeovers, pero si está lleno cuenta como su tarjeta de ese llenado.

### 1.3 Las tarjetas de oferta (`AF.IAP.OfferScheduler`, `AF.UI.OfferCard`)
`offer_scheduler` de 05 §5, decidido en la victoria de campaña:
- **Nunca** en la primera sesión (06 §2.1, regla 1; sesión = Q-M4 (a), el `SessionTracker` de B6), ni con la tienda apagada o el perfil bloqueado.
- **Cupo:** una por sesión (`ui_max_takeovers_per_session` = 1), compartido con la Bitácora cuando se integre B4 (Q-B5-10). Sobrevive a la reconstrucción por idioma (la misma sesión de B6).
- **Prioridad** de 05 §5: Mochila → Sin anuncios → Cántaro lleno → skin del mundo.
- **Dónde** [P] (Q-B5-6): la Mochila («tarjeta en la pantalla de victoria», contrato 11) y el Cántaro lleno («HUD de victoria») salen encima de la victoria a los 1,5 s, cuando aparece «Siguiente» (D-033, punto 10); Sin anuncios («tras la pantalla de victoria») y la skin («fuera del nivel») salen al volver al inicio. Si el jugador deja la victoria antes (o el juego pasa solo al nivel siguiente tras la Cosecha doble, D-011), la tarjeta no sale o se quita.

| Oferta | Regla | Fuente |
|---|---|---|
| Mochila de inicio | Primera tarjeta en la primera victoria con nivel ≥ 12 en una sesión ≥ 2 (`eco_starter_first_show` = `L12_or_session2`, leída como `L{nivel}_or_session{n}`); arranca las `eco_starter_timer_h` (48) horas [P] (Q-B5-3). Recordatorios desde el 15 y desde el 19 (uno solo si se pasan los dos sin verlo; la primera tarjeta cubre los que ya tocaban) [P] (Q-B5-4). Se va al ganar el 20, al vencer o al comprarla | 05 §4.1 y §5; 06 §2.1-§2.2 |
| Sin anuncios | Tras el 3.er intersticial visto o en el día 2 desde la instalación, lo primero (`eco_noads_card_trigger` = `3_interstitials_or_d2`); 1 al día; tras 3 rechazos (cerrar la tarjeta o cancelar su compra), solo Mercado; nunca antes de `rescue_min_level` [P] (Q-B5-2) | 05 §4.1, §5 y §10 |
| Cántaro lleno | 1 tarjeta por llenado (también si se vio a mano con el icono) | 05 §4.1 y §5 |
| Skin del mundo | Desde el mundo 3 (nivel 41), en la primera victoria del mundo con cupo [P], 1 por mundo, nunca si ya se tiene. Apagada hasta el arte final (Q-B5-1) | 05 §4.1 y §5; 06 §4.5; D-015 |

La tarjeta: cabecera con el nombre de tienda y la X, el icono, el contenido, una nota (temporizador, «¡Lleno!»…), el botón con el precio y «Ahora no». Sin mascotas (D-013, T1-A). Si hay otra compra en curso, el botón no la cierra. Atrás la cierra como «Ahora no».

Los **intersticiales vistos** se cuentan por el contador del día de `AF.Ads` (`AdLedger.InterstitialsToday`), sin tocar sus archivos; si el contador vuelve a empezar a medianoche, cuentan los de hoy.

### 1.4 `geo_tier` (Q-E3 (a))
`GeoTierRule.FromLanguageTag`: la región de la etiqueta del teléfono (`IDeviceLocale`), leída como la lee el idioma (`LocLocale.HasRegion`). T1 = EE. UU., Canadá, Australia, Japón, Corea y «Europa occidental» [P] (Q-B5-11); el resto, T3. Con B5-bootstrap llega a `GameServices.Geo` (orden Balsa / Puñado del modal sin saldo, 04 §6.2) y al común `geo_tier` de la analítica (08 §8.1). Con el IAP real, el país de la tienda.

### 1.5 Analítica (08 §8.2)
- `af_shop_view {tab, from = lobby}` al abrir el Mercado [P] (`from`: Q-B5-15).
- `af_iap_offer_shown` con `context = shop` por cada fila que se puede comprar ahora, al abrir o cambiar de pestaña; en las tarjetas con el botón activo, `starter_window`, `noads_prompt` y `piggy`, y `shop` en la de skin [P] (Q-B5-14).
- `geo_tier` como propiedad de usuario (08 §8.1), al conectar la telemetría (`ConnectTelemetry`, B5-bootstrap).
- `af_piggy {state ∈ unlocked, shown_full, bought; seeds_inside}`; `af_skin_unlock {skin_id, source = iap}`.
- `af_iap_result` y `purchase` ya los emite `AnalyticsStoreService` (B7).
- Sonda de desarrollo: `AF_STATE` `market_open`, `market_closed`, `piggy_fill`, `offer_shown`, `offer_declined` y `skin_selected`; `AF_LAYOUT` de `market`, `offer_card` y `lobby_market`.

## 2. Remote Config (08 §9)
Claves nuevas en `AF.IAP.OffersConfig`, con sus defaults y rangos: `eco_starter_window_levels` `[10,20]`, `eco_starter_timer_h` 48, `eco_starter_first_show` `"L12_or_session2"`, `eco_noads_card_trigger` `"3_interstitials_or_d2"`, `eco_piggy_from_level` 20 (15-30) y `eco_piggy_fill` `[15,30,40]`. `eco_piggy_cap`, `eco_piggy_min_buy`, `eco_noads_bonus_seeds` y `eco_seed_pack_amounts` siguen en `StoreConfig`. Lo que 05 fija sin clave va en `OfferRules`, con su cita: recordatorios 15 y 19, Sin anuncios en el Mercado desde el 22, 1 tarjeta al día, 3 rechazos y tarjeta de skin desde el 41.

## 3. Perfil (08 §4.3)
- `piggy` = `{seeds, cap}` (canon) más `last_level` y `full_card` [P]. `seeds` y `cap` son dinero: un tipo equivocado es corrupción, como `wallet`; `last_level` y `full_card` toman su default.
- `offers` [P] = `{starter_first_seen_at, starter_reminders, noads_card_date, noads_declines, interstitials_seen, skin_cards}`. No es dinero: un valor que no vale toma su default.
- Los dos faltan hasta el primer cambio de cualquiera de ellos (se guardan juntos: el primer intersticial contado ya escribe `piggy` a 0) y entran sin migrar (aún no se ha publicado). Se guardan por la vía síncrona del lote (`ProfileSaveBatch.Durable`), como la cartera. Comprar el Cántaro son dos escrituras seguidas: la cartera acredita (dentro de la tienda) y después el Cántaro se vacía (riesgo en §8). El llenado es otra escritura en la victoria (no la hay si ya estaba lleno).

## 4. Archivos

**En la rama (nuevos):**
- `Assets/_Project/Scripts/IAP/`: `OffersConfig.cs`, `OffersRecord.cs`, `OffersPiggy.cs`, `OffersScheduler.cs` y `OffersTakeover.cs`; tests en `IAP/Tests/OffersTests.cs`.
- `Assets/_Project/Scripts/UI/`: `MarketScreen.cs`, `MarketModel.cs`, `MarketText.cs`, `MarketLobbyIcons.cs`, `OfferCard.cs`, `GeoTierRule.cs`, `GameBootstrap.Market.cs` y `MarketRunner.cs`.
- Tests: `Tests/EditMode/MarketModelTests.cs` y `Tests/PlayMode/MarketTests.cs`.

**Parches** (`docs/dev/traspaso/spark-enganches/`, LF; aplicar en este orden con `git apply`; los `.meta` de los archivos nuevos van dentro, tal como los generó Unity):
1. `B5-loc.diff`: las 19 claves nuevas en `strings.csv`, en su sitio por orden ordinal (`shop`, `no_ads` y `market_tab_seeds` con el texto del glosario y su EN y PT-BR; el resto `pendiente_asesor`, sin traducir), y `MarketText.AllKeys` en `LocTablesTests.UiTextKeys`.
2. `B5-rc.diff`: los `[RemoteConfigKey]` de `OffersConfig` y su enlace en `RcConfigBinder` (`RcConfigs.Offers`, por el constructor), con su test en `RcConfigBinderTests`, y `GameBootstrap.MarketRc.cs`. Los atributos van en el parche, junto al enlace, para que la rama sola no deje una clase de cifras sin enlazar.
3. `B5-profile.diff`: `Profile.Piggy`/`Offers` y `ProfileJson`, `IAP/ProfileOffersStore.cs` con `IAP/Tests/ProfileOffersStoreTests.cs`, y `GameBootstrap.MarketProfile.cs`.
4. `B5-bootstrap.diff`: en `GameBootstrap.cs`, `SetUpMarket();` tras `SetUpSessions();`, el Mercado y la tarjeta en `CommerceOpen` y Atrás (`PressMarketBack`); `geo_tier` desde la región del teléfono en `GameServices` (también en `GameBootstrap.Daily.cs`) y en el común de la analítica (`GameBootstrap.Telemetry.cs`); `Tests/PlayMode/MarketHooksTests.cs` (necesita B5-profile).

Sin B5-bootstrap no hay Mercado en el juego (D-008: oculto mientras no exista); sin B5-profile, en desarrollo el Cántaro y las ofertas viven en memoria y en release no hay Mercado.

## 5. Para el GDD (lo anota el integrador)
- 08 §4.3: `piggy.last_level` y `piggy.full_card` [P] y el bloque `offers` [P] (§3 de esta nota).
- 08 §8.2: `from` de `af_shop_view` = `lobby` [P]; contexto de la tarjeta de skin (Q-B5-14).
- 05 §5: dónde sale cada tarjeta (Q-B5-6) y la lectura de los recordatorios (Q-B5-4).
- 05 §10: la tarjeta Sin anuncios no sale antes del 31 (Q-B5-2).

## 6. Para el integrador
- **B4 (`TakeoverQueue`):** al integrarlo, el cupo de las ofertas tiene que ser el de la cola. Basta un `ITakeoverBudget` sobre ella en `SetUpMarket` (`TakeoverBudgetOverride` o el default): `QuotaLeft => queue.QuotaLeft` y `Spend() => queue.MarkShown(TakeoverKind.Offer)`. Y registrar `TakeoverKind.Offer` si la cola debe decidir entre la Bitácora y una oferta al entrar al inicio (06 §4.7: «Bitácora pendiente → evento → oferta → liga»). Q-B5-10.
- **B8 (`CloudMerge`):** cubrir `piggy` y `offers` en la fusión de 08 §11 (Q-B5-9).
- **F3-5 (tienda verificada en el juego):** con `VerifiedStoreService`, una compra del Cántaro que queda `Pending` no lo vacía; al acreditarse más tarde (cola o `Resume`) hay que vaciarlo, y no dejar comprarlo otra vez mientras haya una pendiente (Q-B5-12).

## 7. Preguntas para el dueño (se aplicó la recomendada, marcada [P])

| Id | Pregunta | Opciones (recomendada primero, aplicada) |
|---|---|---|
| Q-B5-1 | Venta de skins (fila del Mercado y tarjeta desde el 41) antes del arte final | (a) apagada hasta el arte final: `OfferRules.SkinSalesEnabledDefault` = false; la elección de la puesta sí funciona (Q-E6 (a)); (b) ya, con el arte provisional de B14 |
| Q-B5-2 | 05 §10 prohíbe ofertas de pago antes del 31 salvo la Mochila, pero el 3.er intersticial (desde el 22) puede llegar antes | (a) ninguna tarjeta salvo la Mochila antes de `rescue_min_level`; el Mercado sí lista Sin anuncios desde el 22; (b) tarjeta Sin anuncios desde el 22 |
| Q-B5-3 | Cuándo empiezan las 48 h de la Mochila («desde la primera vista») | (a) con su primera tarjeta; (b) la primera vez que se ve en el Mercado |
| Q-B5-4 | Recordatorios «en el nivel 15 y 19» | (a) en la primera victoria desde el 15 y desde el 19 con cupo, uno solo si se pasan los dos; (b) solo en la victoria exacta |
| Q-B5-5 | ¿El Reto del río llena el Cántaro? | (a) no, solo la campaña; (b) sí |
| Q-B5-6 | Dónde sale cada tarjeta | (a) Mochila y Cántaro en la victoria a 1,5 s; Sin anuncios y skin al volver al inicio; (b) todas en la victoria |
| Q-B5-7 | Dónde va el icono del Cántaro («en el HUD desde el nivel 20») | (a) en el inicio, arriba a la derecha bajo Ajustes (no tapa los saldos ni la ventanita de casa); (b) también en el HUD del nivel (parche de `GameScreen`) |
| Q-B5-8 | Los campos nuevos del perfil | (a) `piggy.last_level`, `piggy.full_card` y el bloque `offers` de §3, anotados [P] en 08 §4.3; (b) otro diseño |
| Q-B5-9 | Fusión en la nube de `piggy` y `offers` (B8) | (a) `piggy` del perfil ganador, como `wallet`; en `offers`, unión de listas, máximo de contadores y la fecha más reciente; (b) todo del ganador. Sin aplicar: va con B8 |
| Q-B5-10 | Cupo de takeovers con la Bitácora | (a) uno solo, el de la `TakeoverQueue` de B4 (adaptador de §6); (b) uno por sistema |
| Q-B5-11 | Qué países son T1 («Europa occidental») | (a) AD, AT, BE, CH, DE, DK, ES, FI, FR, GB, IE, IS, IT, LI, LU, MC, NL, NO, PT, SE, SM más US, CA, AU, JP y KR; Chile, Uruguay y el resto, T3; (b) otra lista |
| Q-B5-12 | Cántaro con la compra pendiente (pago en efectivo de Play o sin verificar) | (a) no se vacía hasta acreditarse; F3-5 lo vacía al acreditar y bloquea otra compra mientras tanto; (b) vaciarlo al quedar pendiente |
| Q-B5-13 | ¿Una skin comprada se pone sola? | (a) sí, como la ganada (Q-M8 (a)); (b) no |
| Q-B5-14 | Contexto de `af_iap_offer_shown` de la tarjeta de skin (08 §8.2 no lo tiene) | (a) `shop`; (b) añadir `skin_world` a 08 §8.2 |
| Q-B5-15 | `from` de `af_shop_view` | (a) `lobby` (la única entrada de la 1.0); (b) otra lista |

Q-E2 (a): no hay icono de oferta temporal en el inicio en la 1.0; el hueco queda libre para la 1.1/1.2.

## 8. Riesgos
- El arte es provisional (por código): el icono del Mercado (semilla), el del Cántaro (disco de barro) y las filas.
- El llenado del Cántaro es una escritura aparte de la de la victoria: si la app se cierra entre las dos, se pierde ese llenado (≤ 40 semillas).
- Comprar el Cántaro: si la app se cierra justo entre la escritura de la cartera y la del Cántaro, las semillas quedan acreditadas y el Cántaro sigue lleno (se podría volver a comprar). Hacerlo en una sola escritura exige tocar `FakeStoreService`/`VerifiedStoreService` (sdk-integrator): con F3-5, que el ítem `piggy` del `grant` vacíe el Cántaro dentro de la misma transacción (Q-B5-12).
- Sin capturas de pantalla ni prueba en el teléfono.
- Los textos nuevos nacen `pendiente_asesor` y sin EN ni PT-BR (salvo los tres del glosario).
