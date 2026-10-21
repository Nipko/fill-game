// Respuestas HTTP comunes de las Lambdas (contrato v1: error.schema.json).
import type { APIGatewayProxyResultV2, APIGatewayProxyStructuredResultV2 } from 'aws-lambda';

export type ErrorCode =
  | 'bad_request'
  | 'unauthorized'
  | 'integrity_failed'
  | 'token_belongs_to_other_user'
  | 'bad_signature'
  | 'code_not_found'
  | 'request_expired'
  | 'rate_limited'
  | 'not_implemented'
  | 'upstream_error'
  | 'internal_error';

const BASE_HEADERS = {
  'content-type': 'application/json; charset=utf-8',
  'cache-control': 'no-store',
  'x-content-type-options': 'nosniff',
} as const;

export function json(statusCode: number, body: unknown): APIGatewayProxyStructuredResultV2 {
  return { statusCode, headers: { ...BASE_HEADERS }, body: JSON.stringify(body) };
}

export function error(statusCode: number, code: ErrorCode, message: string): APIGatewayProxyStructuredResultV2 {
  return json(statusCode, { error: { code, message } });
}

/** Esqueleto de F2-2: cada función responde 501 hasta su tarea (F2-4 a F2-8). */
export function notImplemented(operation: string): () => Promise<APIGatewayProxyResultV2> {
  return () => Promise.resolve(error(501, 'not_implemented', `${operation} todavía no está implementada.`));
}
