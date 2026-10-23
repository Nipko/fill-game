// Generado por infra/web/build.ts desde web/src/browser/: no se edita a mano.
"use strict";
(() => {
  // web/src/browser/account-code.ts
  var ACCOUNT_CODE_PATTERN = /^[0-7][0-9A-HJKMNP-TV-Z]{25}$/;
  var DISPLAY_GROUPS = [4, 4, 4, 4, 4, 3, 3];
  var SEPARATORS = new RegExp(
    "[\\s" + String.fromCharCode(173) + // guion suave
    String.fromCharCode(8208) + "-" + String.fromCharCode(8213) + // guiones y rayas, U+2010 a U+2015
    String.fromCharCode(8722) + // signo menos
    String.fromCharCode(65112) + String.fromCharCode(65123) + String.fromCharCode(65293) + // compatibilidad
    "-]",
    "gu"
  );
  function normalizeAccountCode(raw) {
    return raw.normalize("NFKC").toUpperCase().replace(SEPARATORS, "").replace(/[IL]/g, "1").replace(/O/g, "0");
  }
  function isValidAccountCode(code) {
    return ACCOUNT_CODE_PATTERN.test(code);
  }
  function formatAccountCode(code) {
    const parts = [];
    let i = 0;
    for (const n of DISPLAY_GROUPS) {
      if (i >= code.length) break;
      parts.push(code.slice(i, i + n));
      i += n;
    }
    if (i < code.length) parts.push(code.slice(i));
    return parts.join("-");
  }

  // web/src/browser/delete-flow.ts
  var DELETE_REQUEST_PATH = "/v1/account/delete-request";
  var DELETE_CONFIRM_PATH = "/v1/account/delete-confirm";
  var REQUEST_TOKEN_RE = /^[A-Za-z0-9_-]{43}$/;
  var DEFAULT_RETRY_S = 60;
  function resolveApiBase(config) {
    if (typeof config !== "object" || config === null) return null;
    const base = config.apiBase;
    if (typeof base !== "string") return null;
    if (base === "") return "";
    if (!base.startsWith("/") || base.startsWith("//") || base.endsWith("/")) return null;
    if (!/^[A-Za-z0-9/_-]+$/.test(base)) return null;
    return base;
  }
  async function post(fetchFn, url, body) {
    try {
      return await fetchFn(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
        credentials: "omit",
        cache: "no-store",
        redirect: "error"
      });
    } catch {
      return null;
    }
  }
  async function readJson(res) {
    try {
      return await res.json();
    } catch {
      return null;
    }
  }
  async function errorOf(res) {
    switch (res.status) {
      case 400:
        return { kind: "invalid_code" };
      case 404:
        return { kind: "not_found" };
      case 410:
        return { kind: "expired" };
      case 429: {
        const body = await readJson(res);
        const fromBody = body?.error?.retry_after_s;
        const fromHeader = Number(res.headers.get("Retry-After"));
        const retry = typeof fromBody === "number" && fromBody > 0 ? fromBody : fromHeader > 0 ? fromHeader : DEFAULT_RETRY_S;
        return { kind: "rate_limited", retryAfterS: Math.ceil(retry) };
      }
      default:
        return { kind: "unavailable" };
    }
  }
  async function requestDeletion(fetchFn, apiBase, rawCode) {
    const code = normalizeAccountCode(rawCode);
    if (!isValidAccountCode(code)) return { kind: "invalid_code" };
    const res = await post(fetchFn, apiBase + DELETE_REQUEST_PATH, { code });
    if (!res) return { kind: "unavailable" };
    if (res.status !== 200) return errorOf(res);
    const body = await readJson(res);
    const token = body?.request_token;
    const expires = body?.expires_in_s;
    if (typeof token !== "string" || !REQUEST_TOKEN_RE.test(token) || typeof expires !== "number" || expires < 1) {
      return { kind: "unavailable" };
    }
    return { kind: "ok", requestToken: token, expiresInS: expires };
  }
  async function confirmDeletion(fetchFn, apiBase, requestToken) {
    if (!REQUEST_TOKEN_RE.test(requestToken)) return { kind: "expired" };
    const res = await post(fetchFn, apiBase + DELETE_CONFIRM_PATH, { request_token: requestToken });
    if (!res) return { kind: "unavailable" };
    if (res.status !== 200) {
      const err = await errorOf(res);
      return err.kind === "invalid_code" || err.kind === "not_found" ? { kind: "expired" } : err;
    }
    const body = await readJson(res);
    return body?.status === "deleted" ? { kind: "ok" } : { kind: "unavailable" };
  }

  // web/src/browser/delete-account.ts
  function byId(id) {
    const el = document.getElementById(id);
    if (!el) throw new Error(`Falta #${id}`);
    return el;
  }
  async function loadApiBase() {
    try {
      const res = await fetch("/config.json", { cache: "no-store", credentials: "omit" });
      if (!res.ok) return null;
      return resolveApiBase(await res.json());
    } catch {
      return null;
    }
  }
  function init() {
    const form = byId("delete-form");
    const input = byId("account-code");
    const submit = byId("delete-submit");
    const stepCode = byId("delete-step-code");
    const stepConfirm = byId("delete-step-confirm");
    const confirmCode = byId("delete-confirm-code");
    const yes = byId("delete-yes");
    const no = byId("delete-no");
    const status = byId("delete-status");
    const msg = (key, minutes) => {
      const attr = "msg" + key.charAt(0).toUpperCase() + key.slice(1);
      const text = form.dataset[attr] ?? "";
      return minutes === void 0 ? text : text.replace("{minutes}", String(minutes));
    };
    const say = (text, kind) => {
      status.textContent = text;
      status.dataset.kind = kind;
    };
    const sayError = (err) => {
      switch (err.kind) {
        case "invalid_code":
          say(msg("invalid"), "error");
          input.setAttribute("aria-invalid", "true");
          input.focus();
          return;
        case "not_found":
          say(msg("notFound"), "error");
          input.setAttribute("aria-invalid", "true");
          input.focus();
          return;
        case "expired":
          say(msg("expired"), "error");
          return;
        case "rate_limited":
          say(msg("rateLimited", Math.max(1, Math.ceil(err.retryAfterS / 60))), "error");
          return;
        case "unavailable":
          say(msg("unavailable"), "error");
          return;
      }
    };
    const showStep = (step) => {
      stepCode.hidden = step !== "code";
      stepConfirm.hidden = step !== "confirm";
      if (step === "code") input.focus();
      else yes.focus();
    };
    let apiBase = null;
    let token = null;
    let busy = false;
    const setBusy = (value) => {
      busy = value;
      submit.disabled = value;
      yes.disabled = value;
      no.disabled = value;
    };
    input.addEventListener("input", () => input.removeAttribute("aria-invalid"));
    form.addEventListener("submit", (ev) => {
      ev.preventDefault();
      if (busy) return;
      const code = normalizeAccountCode(input.value);
      if (!isValidAccountCode(code)) {
        sayError({ kind: "invalid_code" });
        return;
      }
      setBusy(true);
      say(msg("sending"), "info");
      void (async () => {
        apiBase ?? (apiBase = await loadApiBase());
        if (apiBase === null) {
          say(msg("config"), "error");
          setBusy(false);
          return;
        }
        const res = await requestDeletion(fetch.bind(window), apiBase, code);
        setBusy(false);
        if (res.kind !== "ok") {
          sayError(res);
          return;
        }
        token = res.requestToken;
        confirmCode.textContent = formatAccountCode(code);
        say("", "info");
        showStep("confirm");
      })();
    });
    no.addEventListener("click", () => {
      token = null;
      say(msg("cancelled"), "info");
      showStep("code");
    });
    yes.addEventListener("click", () => {
      if (busy || token === null || apiBase === null) return;
      setBusy(true);
      say(msg("sending"), "info");
      const current = token;
      const base = apiBase;
      void (async () => {
        const res = await confirmDeletion(fetch.bind(window), base, current);
        setBusy(false);
        token = null;
        if (res.kind === "ok") {
          form.hidden = true;
          say(msg("done"), "ok");
          status.focus();
          return;
        }
        sayError(res);
        showStep("code");
      })();
    });
    form.hidden = false;
  }
  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init);
  else init();
})();
