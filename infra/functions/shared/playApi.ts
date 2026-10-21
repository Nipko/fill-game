// Google Play Developer API (androidpublisher v3) con fetch nativo y el token OAuth2 de la cuenta de servicio
// (googleAuth.ts). Solo lo que usa el backend: purchases.products.get y acknowledge (08 §10.3) y
// voidedpurchases.list (08 §10.1). Detrás de una interfaz: en dev, con AF_FAKE_PLAY_VERIFIER=1, los tokens
// `fake-*` los responde FakePlayApi (plan §4, hallazgo 2: sin app en Play Console no hay compras reales).
import { UpstreamError } from './errors.ts';
import type { AccessTokenProvider, FetchFn } from './googleAuth.ts';

export const ANDROIDPUBLISHER_SCOPE = 'https://www.googleapis.com/auth/androidpublisher';
const BASE = 'https://androidpublisher.googleapis.com/androidpublisher/v3/applications';
const TIMEOUT_MS = 4000;

export type PurchaseState = 'purchased' | 'canceled' | 'pending';

export interface ProductPurchase {
  readonly state: PurchaseState;
  readonly acknowledged: boolean;
  readonly orderId?: string;
  readonly purchaseTimeMs: number;
  readonly regionCode?: string;
  /** Lo que la app pasó a setObfuscatedAccountId (08 §10.1: el UID). */
  readonly obfuscatedAccountId?: string;
  /** 0 = prueba (licencias de prueba), 1 = promo, 2 = recompensada; ausente = compra normal. */
  readonly purchaseType?: number;
  readonly quantity: number;
}

export interface VoidedPurchase {
  readonly purchaseToken: string;
  readonly voidedTimeMs: number;
  readonly orderId?: string;
  readonly voidedReason?: number;
  readonly voidedSource?: number;
}

export interface VoidedPage {
  readonly items: readonly VoidedPurchase[];
  readonly nextPageToken?: string;
}

export interface PlayApi {
  /** La compra, o null si Play dice que el token o el SKU no existen (400/404/410). */
  getProduct(sku: string, purchaseToken: string): Promise<ProductPurchase | null>;
  acknowledge(sku: string, purchaseToken: string): Promise<void>;
  listVoided(startMs: number, endMs: number, pageToken?: string): Promise<VoidedPage>;
}

const num = (v: unknown): number | undefined => {
  if (typeof v === 'number' && Number.isFinite(v)) return v;
  if (typeof v === 'string' && /^\d+$/.test(v)) return Number(v);
  return undefined;
};
const str = (v: unknown): string | undefined => (typeof v === 'string' && v !== '' ? v : undefined);

/** Traduce el ProductPurchase de Play. purchaseState: 0 comprada, 1 cancelada, 2 pendiente. */
export function parseProductPurchase(j: Record<string, unknown>): ProductPurchase {
  const ps = num(j.purchaseState);
  const state: PurchaseState = ps === 0 ? 'purchased' : ps === 2 ? 'pending' : 'canceled';
  return {
    state,
    acknowledged: num(j.acknowledgementState) === 1,
    orderId: str(j.orderId),
    purchaseTimeMs: num(j.purchaseTimeMillis) ?? 0,
    regionCode: str(j.regionCode),
    obfuscatedAccountId: str(j.obfuscatedExternalAccountId),
    purchaseType: num(j.purchaseType),
    quantity: Math.max(1, num(j.quantity) ?? 1),
  };
}

export class GooglePlayApi implements PlayApi {
  private readonly packageName: string;
  private readonly auth: AccessTokenProvider;
  private readonly fetchFn: FetchFn;

  constructor(opts: { packageName: string; auth: AccessTokenProvider; fetchFn?: FetchFn }) {
    this.packageName = opts.packageName;
    this.auth = opts.auth;
    this.fetchFn = opts.fetchFn ?? fetch;
  }

  private productUrl(sku: string, token: string): string {
    return `${BASE}/${encodeURIComponent(this.packageName)}/purchases/products/${encodeURIComponent(sku)}/tokens/${encodeURIComponent(token)}`;
  }

  private async call(url: string, init: RequestInit): Promise<Response> {
    const accessToken = await this.auth.token();
    try {
      return await this.fetchFn(url, {
        ...init,
        headers: { authorization: `Bearer ${accessToken}`, accept: 'application/json', ...(init.headers ?? {}) },
        signal: AbortSignal.timeout(TIMEOUT_MS),
      });
    } catch (e) {
      throw new UpstreamError('play', e instanceof Error ? e.name : 'fetch');
    }
  }

  async getProduct(sku: string, purchaseToken: string): Promise<ProductPurchase | null> {
    const res = await this.call(this.productUrl(sku, purchaseToken), { method: 'GET' });
    if (res.status === 400 || res.status === 404 || res.status === 410) return null;
    if (!res.ok) throw new UpstreamError('play', 'products.get', res.status);
    return parseProductPurchase((await res.json()) as Record<string, unknown>);
  }

  async acknowledge(sku: string, purchaseToken: string): Promise<void> {
    const res = await this.call(`${this.productUrl(sku, purchaseToken)}:acknowledge`, {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: '{}',
    });
    if (!res.ok) throw new UpstreamError('play', 'products.acknowledge', res.status);
  }

  async listVoided(startMs: number, endMs: number, pageToken?: string): Promise<VoidedPage> {
    const q = new URLSearchParams({ startTime: String(startMs), endTime: String(endMs), maxResults: '1000', type: '0' });
    if (pageToken) q.set('token', pageToken);
    const res = await this.call(`${BASE}/${encodeURIComponent(this.packageName)}/purchases/voidedpurchases?${q.toString()}`, {
      method: 'GET',
    });
    if (!res.ok) throw new UpstreamError('play', 'voidedpurchases.list', res.status);
    const j = (await res.json()) as {
      voidedPurchases?: Array<Record<string, unknown>>;
      tokenPagination?: { nextPageToken?: unknown };
    };
    const items: VoidedPurchase[] = [];
    for (const v of j.voidedPurchases ?? []) {
      const purchaseToken = str(v.purchaseToken);
      if (!purchaseToken) continue;
      items.push({
        purchaseToken,
        voidedTimeMs: num(v.voidedTimeMillis) ?? endMs,
        orderId: str(v.orderId),
        voidedReason: num(v.voidedReason),
        voidedSource: num(v.voidedSource),
      });
    }
    return { items, nextPageToken: str(j.tokenPagination?.nextPageToken) };
  }
}

/** Prefijo de los tokens del verificador falso (solo dev; plan §4). */
export const FAKE_TOKEN_PREFIX = 'fake-';

/**
 * Verificador falso de Play (solo dev con AF_FAKE_PLAY_VERIFIER=1): `fake-pending-*` → pendiente,
 * `fake-canceled-*` → cancelada, `fake-unknown-*` → no existe, cualquier otro `fake-*` → comprada sin
 * reconocer. El job de anuladas no ve nada.
 */
export class FakePlayApi implements PlayApi {
  readonly acknowledged: string[] = [];
  private readonly now: () => number;

  constructor(now: () => number = Date.now) {
    this.now = now;
  }

  getProduct(_sku: string, purchaseToken: string): Promise<ProductPurchase | null> {
    if (purchaseToken.startsWith('fake-unknown-')) return Promise.resolve(null);
    const state: PurchaseState = purchaseToken.startsWith('fake-pending-')
      ? 'pending'
      : purchaseToken.startsWith('fake-canceled-')
        ? 'canceled'
        : 'purchased';
    return Promise.resolve({
      state,
      acknowledged: this.acknowledged.includes(purchaseToken),
      orderId: 'GPA.FAKE',
      purchaseTimeMs: this.now(),
      regionCode: 'CO',
      purchaseType: 0,
      quantity: 1,
    });
  }

  acknowledge(_sku: string, purchaseToken: string): Promise<void> {
    this.acknowledged.push(purchaseToken);
    return Promise.resolve();
  }

  listVoided(): Promise<VoidedPage> {
    return Promise.resolve({ items: [] });
  }
}

/** En dev con el verificador falso: los tokens `fake-*` van al falso y el resto a Play de verdad. */
export class DevPlayApi implements PlayApi {
  private readonly fake: PlayApi;
  private readonly real: PlayApi;

  constructor(fake: PlayApi, real: PlayApi) {
    this.fake = fake;
    this.real = real;
  }

  getProduct(sku: string, token: string): Promise<ProductPurchase | null> {
    return (token.startsWith(FAKE_TOKEN_PREFIX) ? this.fake : this.real).getProduct(sku, token);
  }

  acknowledge(sku: string, token: string): Promise<void> {
    return (token.startsWith(FAKE_TOKEN_PREFIX) ? this.fake : this.real).acknowledge(sku, token);
  }

  /** En dev no hay app en Play Console (D-034): el job no consulta Play. */
  listVoided(startMs: number, endMs: number, pageToken?: string): Promise<VoidedPage> {
    return this.fake.listVoided(startMs, endMs, pageToken);
  }
}
