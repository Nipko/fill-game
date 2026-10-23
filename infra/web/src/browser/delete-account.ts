// Script de la página de eliminación de cuenta (08 §11). Sin dependencias ni terceros; los textos salen de los
// atributos data-msg-* del formulario, que el generador escribe en cada idioma. La base de la API sale de
// /config.json, que escribe el stack Web al desplegar (nunca fija en el HTML).
import { formatAccountCode, isValidAccountCode, normalizeAccountCode } from './account-code.ts';
import type { FlowError } from './delete-flow.ts';
import { confirmDeletion, requestDeletion, resolveApiBase } from './delete-flow.ts';

type MsgKey = 'invalid' | 'sending' | 'notFound' | 'expired' | 'rateLimited' | 'unavailable' | 'config' | 'cancelled' | 'done';

function byId<T extends HTMLElement>(id: string): T {
  const el = document.getElementById(id);
  if (!el) throw new Error(`Falta #${id}`);
  return el as T;
}

async function loadApiBase(): Promise<string | null> {
  try {
    const res = await fetch('/config.json', { cache: 'no-store', credentials: 'omit' });
    if (!res.ok) return null;
    return resolveApiBase((await res.json()) as unknown);
  } catch {
    return null;
  }
}

function init(): void {
  const form = byId<HTMLFormElement>('delete-form');
  const input = byId<HTMLInputElement>('account-code');
  const submit = byId<HTMLButtonElement>('delete-submit');
  const stepCode = byId<HTMLElement>('delete-step-code');
  const stepConfirm = byId<HTMLElement>('delete-step-confirm');
  const confirmCode = byId<HTMLElement>('delete-confirm-code');
  const yes = byId<HTMLButtonElement>('delete-yes');
  const no = byId<HTMLButtonElement>('delete-no');
  const status = byId<HTMLElement>('delete-status');

  const msg = (key: MsgKey, minutes?: number): string => {
    const attr = 'msg' + key.charAt(0).toUpperCase() + key.slice(1);
    const text = form.dataset[attr] ?? '';
    return minutes === undefined ? text : text.replace('{minutes}', String(minutes));
  };
  const say = (text: string, kind: 'info' | 'error' | 'ok'): void => {
    status.textContent = text;
    status.dataset.kind = kind;
  };
  const sayError = (err: FlowError): void => {
    switch (err.kind) {
      case 'invalid_code':
        say(msg('invalid'), 'error');
        input.setAttribute('aria-invalid', 'true');
        input.focus();
        return;
      case 'not_found':
        say(msg('notFound'), 'error');
        input.setAttribute('aria-invalid', 'true');
        input.focus();
        return;
      case 'expired':
        say(msg('expired'), 'error');
        return;
      case 'rate_limited':
        say(msg('rateLimited', Math.max(1, Math.ceil(err.retryAfterS / 60))), 'error');
        return;
      case 'unavailable':
        say(msg('unavailable'), 'error');
        return;
    }
  };
  const showStep = (step: 'code' | 'confirm'): void => {
    stepCode.hidden = step !== 'code';
    stepConfirm.hidden = step !== 'confirm';
    if (step === 'code') input.focus();
    else yes.focus();
  };

  let apiBase: string | null = null;
  let token: string | null = null;
  let busy = false;
  const setBusy = (value: boolean): void => {
    busy = value;
    submit.disabled = value;
    yes.disabled = value;
    no.disabled = value;
  };

  input.addEventListener('input', () => input.removeAttribute('aria-invalid'));

  form.addEventListener('submit', (ev) => {
    ev.preventDefault();
    if (busy) return;
    const code = normalizeAccountCode(input.value);
    if (!isValidAccountCode(code)) {
      sayError({ kind: 'invalid_code' });
      return;
    }
    setBusy(true);
    say(msg('sending'), 'info');
    void (async () => {
      apiBase ??= await loadApiBase();
      if (apiBase === null) {
        say(msg('config'), 'error');
        setBusy(false);
        return;
      }
      const res = await requestDeletion(fetch.bind(window), apiBase, code);
      setBusy(false);
      if (res.kind !== 'ok') {
        sayError(res);
        return;
      }
      token = res.requestToken;
      confirmCode.textContent = formatAccountCode(code);
      say('', 'info');
      showStep('confirm');
    })();
  });

  no.addEventListener('click', () => {
    token = null;
    say(msg('cancelled'), 'info');
    showStep('code');
  });

  yes.addEventListener('click', () => {
    if (busy || token === null || apiBase === null) return;
    setBusy(true);
    say(msg('sending'), 'info');
    const current = token;
    const base = apiBase;
    void (async () => {
      const res = await confirmDeletion(fetch.bind(window), base, current);
      setBusy(false);
      token = null;
      if (res.kind === 'ok') {
        form.hidden = true;
        say(msg('done'), 'ok');
        status.focus();
        return;
      }
      sayError(res);
      showStep('code');
    })();
  });

  form.hidden = false;
}

if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
else init();
