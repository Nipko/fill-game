// Errores tipados de las Lambdas. El envoltorio de http.ts los convierte en la respuesta del contrato v1
// (error.schema.json); cualquier otro error sale como 500 internal_error sin detalles.
import type { ErrorCode } from './http.ts';

/** Error con su respuesta HTTP del contrato. */
export class HttpError extends Error {
  readonly status: number;
  readonly code: ErrorCode;
  readonly retryAfterS: number | undefined;

  constructor(status: number, code: ErrorCode, message: string, retryAfterS?: number) {
    super(message);
    this.name = 'HttpError';
    this.status = status;
    this.code = code;
    this.retryAfterS = retryAfterS;
  }
}

/** Fallo de un servicio externo (Google, Firebase): 502 upstream_error, se puede reintentar. */
export class UpstreamError extends Error {
  readonly service: string;
  readonly httpStatus: number | undefined;

  constructor(service: string, message: string, httpStatus?: number) {
    super(message);
    this.name = 'UpstreamError';
    this.service = service;
    this.httpStatus = httpStatus;
  }
}

/** Falta configuración del despliegue (secreto sin cargar, variable vacía): 500 y alarma, nunca un éxito. */
export class ConfigError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'ConfigError';
  }
}

export const badRequest = (message: string): HttpError => new HttpError(400, 'bad_request', message);
export const unauthorized = (): HttpError => new HttpError(401, 'unauthorized', 'Falta la identidad de Firebase Auth.');
export const integrityFailed = (message: string): HttpError => new HttpError(403, 'integrity_failed', message);
export const rateLimited = (retryAfterS: number): HttpError =>
  new HttpError(429, 'rate_limited', 'Demasiadas peticiones.', Math.max(1, Math.min(86_400, Math.ceil(retryAfterS))));
