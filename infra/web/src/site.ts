// Manifiesto de la web pública de Tintalú (infra/web): idiomas, páginas, rutas por idioma y textos de la plantilla.
// El generador (web/build.ts) lo combina con los fragmentos de web/src/content/<idioma>/<página>.html y escribe
// web/site/, que el stack Web (S3 + CloudFront) despliega tal cual. web/site/ es generado: no se edita a mano.
//
// Desarrollador y responsable: Parallext, LLC (D-041). Los datos que faltan van como [[DATO DEL DUEÑO: …]] y lo que
// decide el abogado como [[ABOGADO: …]]; el generador los resalta. Los textos legales son borrador hasta el abogado
// (D-033, añadido del 2026-10-22, punto 7); en y pt-BR son traducción con IA, pendiente del revisor nativo.

export const LANGS = ['es-419', 'en', 'pt-BR'] as const;
export type Lang = (typeof LANGS)[number];

export const PAGE_IDS = ['home', 'privacy', 'terms', 'delete', 'credits'] as const;
export type PageId = (typeof PAGE_IDS)[number];

export interface PageDef {
  readonly id: PageId;
  /** Ruta pública con barra final; CloudFront sirve `<ruta>index.html` (función IndexRewrite del stack Web). */
  readonly path: Record<Lang, string>;
  readonly title: Record<Lang, string>;
  readonly description: Record<Lang, string>;
  /** Página legal en borrador: lleva el aviso «BORRADOR: requiere revisión del abogado». */
  readonly draft: boolean;
  /** Script propio de la página (en /assets/), si lo tiene. */
  readonly script?: string;
}

export const PAGES: readonly PageDef[] = [
  {
    id: 'home',
    path: { 'es-419': '/', en: '/en/', 'pt-BR': '/pt-br/' },
    title: { 'es-419': 'Tintalú', en: 'Tintalú', 'pt-BR': 'Tintalú' },
    description: {
      'es-419': 'Tintalú, un puzle de ordenar agua inspirado en la cuenca amazónica, de Parallext, LLC.',
      en: 'Tintalú, a water sort puzzle inspired by the Amazon basin, by Parallext, LLC.',
      'pt-BR': 'Tintalú, um quebra-cabeça de classificar água inspirado na bacia amazônica, da Parallext, LLC.',
    },
    draft: false,
  },
  {
    id: 'privacy',
    path: { 'es-419': '/privacidad/', en: '/en/privacy/', 'pt-BR': '/pt-br/privacidade/' },
    title: { 'es-419': 'Política de privacidad', en: 'Privacy policy', 'pt-BR': 'Política de privacidade' },
    description: {
      'es-419': 'Qué datos trata Tintalú, para qué, con quién y cómo ejercer tus derechos.',
      en: 'What data Tintalú processes, why, with whom, and how to exercise your rights.',
      'pt-BR': 'Quais dados o Tintalú trata, para quê, com quem e como exercer seus direitos.',
    },
    draft: true,
  },
  {
    id: 'terms',
    path: { 'es-419': '/terminos/', en: '/en/terms/', 'pt-BR': '/pt-br/termos/' },
    title: { 'es-419': 'Términos de uso', en: 'Terms of use', 'pt-BR': 'Termos de uso' },
    description: {
      'es-419': 'Las condiciones para jugar a Tintalú.',
      en: 'The conditions for playing Tintalú.',
      'pt-BR': 'As condições para jogar Tintalú.',
    },
    draft: true,
  },
  {
    id: 'delete',
    // 08 §11: https://<dominio>/tintalu/eliminar-cuenta; las otras lenguas, propuesta de Q-B13-1 (a).
    path: { 'es-419': '/tintalu/eliminar-cuenta/', en: '/tintalu/en/delete-account/', 'pt-BR': '/tintalu/pt-br/excluir-conta/' },
    title: { 'es-419': 'Eliminar tu cuenta', en: 'Delete your account', 'pt-BR': 'Excluir sua conta' },
    description: {
      'es-419': 'Cómo borrar tu cuenta de Tintalú y sus datos, desde el juego o desde esta página.',
      en: 'How to delete your Tintalú account and its data, in the game or from this page.',
      'pt-BR': 'Como excluir sua conta do Tintalú e os dados dela, no jogo ou por esta página.',
    },
    draft: true,
    script: '/assets/delete-account.js',
  },
  {
    id: 'credits',
    path: { 'es-419': '/creditos/', en: '/en/credits/', 'pt-BR': '/pt-br/creditos/' },
    title: { 'es-419': 'Créditos y licencias', en: 'Credits and licenses', 'pt-BR': 'Créditos e licenças' },
    description: {
      'es-419': 'Quién hace Tintalú y las licencias del material de terceros.',
      en: 'Who makes Tintalú and the licenses of third-party material.',
      'pt-BR': 'Quem faz o Tintalú e as licenças do material de terceiros.',
    },
    draft: true,
  },
];

export interface DeleteFormText {
  readonly legend: string;
  readonly label: string;
  readonly hint: string;
  readonly submit: string;
  readonly confirmTitle: string;
  readonly confirmBody: string;
  readonly confirmCodeLabel: string;
  readonly confirmYes: string;
  readonly confirmNo: string;
  readonly noscript: string;
  /**
   * Aviso cuando el borrado por web aún no está activo (sin API: modo solo web, D-044). Lleva el correo de privacidad
   * como marcador hasta que el dueño lo dé.
   */
  readonly soon: string;
  /** Mensajes del script; {minutes} se sustituye en rateLimited. */
  readonly msg: {
    readonly invalid: string;
    readonly sending: string;
    readonly notFound: string;
    readonly expired: string;
    readonly rateLimited: string;
    readonly unavailable: string;
    readonly config: string;
    readonly cancelled: string;
    readonly done: string;
  };
}

export interface UiText {
  readonly langName: string;
  readonly skip: string;
  readonly langNav: string;
  readonly footerNav: string;
  readonly draft: string;
  readonly draftDetail: string;
  readonly footerDeveloper: string;
  readonly independence: string;
  readonly deleteForm: DeleteFormText;
}

export const UI: Record<Lang, UiText> = {
  'es-419': {
    langName: 'Español',
    skip: 'Saltar al contenido',
    langNav: 'Idioma',
    footerNav: 'Páginas del sitio',
    draft: 'BORRADOR: requiere revisión del abogado.',
    draftDetail: 'Este texto todavía no es definitivo. Los datos entre dobles corchetes están pendientes.',
    footerDeveloper: 'Tintalú es un juego de Parallext, LLC.',
    independence: 'Juego independiente inspirado en la cuenca amazónica; sin relación con Amazon.com, Inc.',
    deleteForm: {
      legend: 'Eliminar con tu código de cuenta',
      label: 'Código de cuenta',
      hint: 'Lo ves en Tintalú › Ajustes, en «Código para borrar tu cuenta en la web». Puedes escribirlo con o sin guiones.',
      submit: 'Continuar',
      confirmTitle: '¿Seguro que quieres eliminar tu cuenta?',
      confirmBody: 'Se borrarán tu cuenta y tus datos en la nube. No se puede deshacer y las compras no se reembolsan.',
      confirmCodeLabel: 'Código:',
      confirmYes: 'Sí, eliminar',
      confirmNo: 'Cancelar',
      noscript: 'El formulario necesita JavaScript. Si no puedes activarlo, borra tu cuenta desde el juego (Ajustes › «Eliminar cuenta y datos»).',
      soon: 'El borrado desde esta página estará disponible pronto. Mientras tanto, borra tu cuenta desde el juego (Ajustes › «Eliminar cuenta y datos») o escríbenos a privacy@tintalu.com y lo hacemos por ti.',
      msg: {
        invalid: 'Ese código no es válido. Revísalo en Ajustes: tiene 26 letras y números.',
        sending: 'Un momento…',
        notFound: 'No encontramos ese código. Revisa que sea el último que ves en Ajustes.',
        expired: 'La confirmación caducó. Vuelve a escribir tu código.',
        rateLimited: 'Demasiados intentos. Prueba de nuevo dentro de {minutes} min.',
        unavailable: 'No se pudo completar ahora. Inténtalo más tarde.',
        config: 'Esta página no está bien configurada. Borra tu cuenta desde el juego o inténtalo más tarde.',
        cancelled: 'No se eliminó nada.',
        done: 'Listo. Tu cuenta se eliminó.',
      },
    },
  },
  en: {
    langName: 'English',
    skip: 'Skip to content',
    langNav: 'Language',
    footerNav: 'Site pages',
    draft: 'DRAFT: requires review by legal counsel.',
    draftDetail: 'This text is not final yet. Data in double brackets is pending.',
    footerDeveloper: 'Tintalú is a game by Parallext, LLC.',
    independence: 'Independent game inspired by the Amazon basin; not affiliated with Amazon.com, Inc.',
    deleteForm: {
      legend: 'Delete with your account code',
      label: 'Account code',
      hint: 'You can find it in Tintalú › Settings, under “Code to delete your account on the web”. You can type it with or without hyphens.',
      submit: 'Continue',
      confirmTitle: 'Are you sure you want to delete your account?',
      confirmBody: 'Your account and your data in the cloud will be deleted. This can’t be undone and purchases are not refunded.',
      confirmCodeLabel: 'Code:',
      confirmYes: 'Yes, delete',
      confirmNo: 'Cancel',
      noscript: 'The form needs JavaScript. If you can’t turn it on, delete your account in the game (Settings › “Delete account and data”).',
      soon: 'Deleting your account from this page will be available soon. In the meantime, delete it in the game (Settings › “Delete account and data”) or write to us at privacy@tintalu.com and we’ll do it for you.',
      msg: {
        invalid: 'That code isn’t valid. Check it in Settings: it has 26 letters and numbers.',
        sending: 'One moment…',
        notFound: 'We couldn’t find that code. Make sure it’s the latest one shown in Settings.',
        expired: 'The confirmation expired. Please enter your code again.',
        rateLimited: 'Too many attempts. Try again in {minutes} min.',
        unavailable: 'This couldn’t be completed right now. Please try again later.',
        config: 'This page isn’t set up correctly. Delete your account in the game or try again later.',
        cancelled: 'Nothing was deleted.',
        done: 'Done. Your account was deleted.',
      },
    },
  },
  'pt-BR': {
    langName: 'Português',
    skip: 'Pular para o conteúdo',
    langNav: 'Idioma',
    footerNav: 'Páginas do site',
    draft: 'RASCUNHO: requer revisão do advogado.',
    draftDetail: 'Este texto ainda não é definitivo. Os dados entre colchetes duplos estão pendentes.',
    footerDeveloper: 'Tintalú é um jogo da Parallext, LLC.',
    independence: 'Jogo independente inspirado na bacia amazônica; sem relação com a Amazon.com, Inc.',
    deleteForm: {
      legend: 'Excluir com o código da sua conta',
      label: 'Código da conta',
      hint: 'Ele aparece em Tintalú › Configurações, em “Código para excluir sua conta na web”. Você pode digitá-lo com ou sem hifens.',
      submit: 'Continuar',
      confirmTitle: 'Tem certeza de que quer excluir sua conta?',
      confirmBody: 'Sua conta e seus dados na nuvem serão apagados. Não dá para desfazer e as compras não são reembolsadas.',
      confirmCodeLabel: 'Código:',
      confirmYes: 'Sim, excluir',
      confirmNo: 'Cancelar',
      noscript: 'O formulário precisa de JavaScript. Se não puder ativá-lo, exclua sua conta no jogo (Configurações › “Excluir conta e dados”).',
      soon: 'A exclusão por esta página estará disponível em breve. Enquanto isso, exclua sua conta no jogo (Configurações › “Excluir conta e dados”) ou escreva para privacy@tintalu.com e nós fazemos isso por você.',
      msg: {
        invalid: 'Esse código não é válido. Confira em Configurações: ele tem 26 letras e números.',
        sending: 'Um momento…',
        notFound: 'Não encontramos esse código. Confira se é o último que aparece em Configurações.',
        expired: 'A confirmação expirou. Digite seu código de novo.',
        rateLimited: 'Muitas tentativas. Tente de novo em {minutes} min.',
        unavailable: 'Não foi possível concluir agora. Tente mais tarde.',
        config: 'Esta página não está configurada corretamente. Exclua sua conta no jogo ou tente mais tarde.',
        cancelled: 'Nada foi excluído.',
        done: 'Pronto. Sua conta foi excluída.',
      },
    },
  },
};

/** Ruta pública de una página en un idioma. */
export function pagePath(id: PageId, lang: Lang): string {
  const page = PAGES.find((p) => p.id === id);
  if (!page) throw new Error(`Página desconocida: ${id}`);
  return page.path[lang];
}
