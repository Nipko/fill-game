// Firebase Auth (admin) sin firebase-admin: borra el usuario con la Identity Toolkit API
// (`projects/{proyecto}/accounts:delete`) y el token OAuth2 de la cuenta de servicio de Firebase Admin
// (googleAuth.ts; secreto que carga el dueño). 08 §11: último paso del borrado de cuenta.
import { UpstreamError } from './errors.ts';
import type { AccessTokenProvider, FetchFn } from './googleAuth.ts';

export const IDENTITY_TOOLKIT_SCOPE = 'https://www.googleapis.com/auth/identitytoolkit';
const TIMEOUT_MS = 4000;

export interface FirebaseAdmin {
  /** Borra el usuario de Auth. Idempotente: si ya no existe, no falla. Devuelve si existía. */
  deleteUser(uid: string): Promise<{ existed: boolean }>;
}

export class IdentityToolkitAdmin implements FirebaseAdmin {
  private readonly projectId: string;
  private readonly auth: AccessTokenProvider;
  private readonly fetchFn: FetchFn;

  constructor(opts: { projectId: string; auth: AccessTokenProvider; fetchFn?: FetchFn }) {
    this.projectId = opts.projectId;
    this.auth = opts.auth;
    this.fetchFn = opts.fetchFn ?? fetch;
  }

  async deleteUser(uid: string): Promise<{ existed: boolean }> {
    const accessToken = await this.auth.token();
    const url = `https://identitytoolkit.googleapis.com/v1/projects/${encodeURIComponent(this.projectId)}/accounts:delete`;
    let res: Response;
    try {
      res = await this.fetchFn(url, {
        method: 'POST',
        headers: { authorization: `Bearer ${accessToken}`, 'content-type': 'application/json' },
        body: JSON.stringify({ localId: uid }),
        signal: AbortSignal.timeout(TIMEOUT_MS),
      });
    } catch (e) {
      throw new UpstreamError('firebase-auth', e instanceof Error ? e.name : 'fetch');
    }
    if (res.ok) return { existed: true };
    if (res.status === 400) {
      const body = (await res.json().catch(() => ({}))) as { error?: { message?: unknown } };
      const message = typeof body.error?.message === 'string' ? body.error.message : '';
      if (message.startsWith('USER_NOT_FOUND')) return { existed: false };
    }
    throw new UpstreamError('firebase-auth', 'accounts:delete', res.status);
  }
}
