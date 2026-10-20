# Página web de eliminación de cuenta · BORRADOR (es-419, en, pt-BR)

- **Estado:** borrador del bloque B13 (2026-10-20). Textos de la página pública que exige la política de eliminación de cuentas de Google Play: sin ella no se completa Data Safety (08 §13.1 #2 y #7). Lo revisa el abogado (`[PENDIENTE: abogado]`); EN y PT-BR son traducción con IA, pendiente del revisor nativo.
- **URL:** 08 §11 fija `https://<dominio del estudio>/tintalu/eliminar-cuenta`; 08 §13.1 #2 la pone en el dominio canónico (`tintalu.com` [P]). `[PENDIENTE: dueño]` (Q-B13-1). Rutas de las otras lenguas, propuesta [P]: `/tintalu/en/delete-account` y `/tintalu/pt-br/excluir-conta`.
- **Qué hace por detrás (08 §11):** pide el ID de jugador visible en Ajustes y ejecuta el mismo flujo que la app: `deleteAccount` (Cloud Function) borra `users/{uid}` y `rewards/*` del UID, conserva el libro mayor de compras anonimizado el plazo fiscal y elimina el usuario de Firebase Auth; analítica recibe `af_account_delete` con `source = web`, sin identificadores. La partida de Saved Games y los datos del teléfono los borra el cliente la próxima vez que se abra (pregunta Q-B13-21: cómo se verifica que quien pide el borrado es el dueño del ID).
- Hosting, formulario y backend: fuera de este bloque (necesitan dominio, cuentas y `functions/`, que no existen todavía).

## es-419

**Título de la página:** Eliminar tu cuenta de Tintalú

Puedes borrar tu cuenta de Tintalú y los datos asociados en cualquier momento.

**La forma más rápida, desde el juego:** abre Tintalú › Ajustes › «Eliminar cuenta y datos» y confirma dos veces.

**Desde esta página**, si ya no tienes el juego instalado:
1. Escribe tu **ID de jugador**. Lo ves en Tintalú › Ajustes.
2. Toca «Eliminar mi cuenta» y confirma.

**Qué se borra:** tu cuenta, tu progreso y tus semillas en la nube, tus recompensas registradas y tu partida guardada en Google Play Juegos. Si el juego sigue instalado en tu teléfono, también se borran sus datos la próxima vez que lo abras.

**Qué se conserva:** el historial de compras, **sin ningún dato que te identifique**, durante `[PENDIENTE: abogado — plazo]` por obligaciones fiscales. Las compras **no se reembolsan** y no se pueden recuperar.

**Cuánto tarda:** `[PENDIENTE: dueño — plazo, p. ej. «al momento» o «hasta N días»]`.

¿Dudas? Escríbenos a `[PENDIENTE: dueño — correo]`. Más información en nuestra Política de privacidad: `[PENDIENTE: dueño — URL]`.

Campos y botones: «ID de jugador» (obligatorio) · «Eliminar mi cuenta» · confirmación: «¿Seguro? Se borrarán tu progreso y tus semillas. Las compras no se reembolsan.» · «Sí, eliminar» / «Cancelar» · resultado: «Listo. Tu cuenta se eliminó.» / «No encontramos ese ID. Revísalo en Ajustes.»

## en (traducción con IA, pendiente del revisor nativo)

**Page title:** Delete your Tintalú account

You can delete your Tintalú account and its data at any time.

**The quickest way, in the game:** open Tintalú › Settings › "Delete account and data" and confirm twice.

**From this page**, if the game is no longer installed:
1. Enter your **player ID**. You can find it in Tintalú › Settings.
2. Tap "Delete my account" and confirm.

**What is deleted:** your account, your progress and seeds in the cloud, your recorded rewards and your saved game in Google Play Games. If the game is still on your phone, its data is also deleted the next time you open it.

**What is kept:** your purchase history, **with nothing that identifies you**, for `[PENDIENTE: abogado — period]` for tax purposes. Purchases **are not refunded** and cannot be recovered.

**How long it takes:** `[PENDIENTE: dueño]`.

Questions? Write to `[PENDIENTE: dueño — email]`. More in our Privacy Policy: `[PENDIENTE: dueño — URL]`.

Fields and buttons: "Player ID" (required) · "Delete my account" · confirmation: "Are you sure? Your progress and seeds will be deleted. Purchases are not refunded." · "Yes, delete" / "Cancel" · result: "Done. Your account was deleted." / "We couldn't find that ID. Check it in Settings."

## pt-BR (traducción con IA, pendiente del revisor nativo)

**Título da página:** Excluir sua conta do Tintalú

Você pode excluir sua conta do Tintalú e os dados associados a qualquer momento.

**O jeito mais rápido, no jogo:** abra o Tintalú › Configurações › "Excluir conta e dados" e confirme duas vezes.

**Por esta página**, se o jogo não estiver mais instalado:
1. Digite o seu **ID de jogador**. Ele aparece em Tintalú › Configurações.
2. Toque em "Excluir minha conta" e confirme.

**O que é excluído:** sua conta, seu progresso e suas sementes na nuvem, suas recompensas registradas e seu jogo salvo no Google Play Games. Se o jogo ainda estiver no seu celular, os dados dele também são apagados na próxima vez que você o abrir.

**O que é mantido:** o histórico de compras, **sem nenhum dado que identifique você**, por `[PENDIENTE: abogado — prazo]` por obrigações fiscais. As compras **não são reembolsadas** e não podem ser recuperadas.

**Quanto tempo leva:** `[PENDIENTE: dueño]`.

Dúvidas? Escreva para `[PENDIENTE: dueño — e-mail]`. Mais informações na nossa Política de Privacidade: `[PENDIENTE: dueño — URL]`.

Campos e botões: "ID de jogador" (obrigatório) · "Excluir minha conta" · confirmação: "Tem certeza? Seu progresso e suas sementes serão apagados. As compras não são reembolsadas." · "Sim, excluir" / "Cancelar" · resultado: "Pronto. Sua conta foi excluída." / "Não encontramos esse ID. Confira em Configurações."

## Notas

- «Eliminar cuenta y datos» es el texto de Ajustes de 08 §11; su clave, su EN y su PT-BR los fija B8 (que hace la opción en Ajustes, `docs/dev/plan-tanda5.md` §4). Los de esta página son propuesta [P] y deben coincidir con los de Ajustes.
- «Semillas» / «seeds» / «sementes» y «Ajustes» / «Settings» / «Configurações», del glosario §2.1 y §10.
