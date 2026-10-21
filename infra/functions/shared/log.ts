// Logs en JSON de las Lambdas (plan §5): nunca el UID en claro, ni tokens, ni códigos, ni claves.
// El UID se registra como `uid_hash` (uidHash de crypto.ts). Los nombres de campo prohibidos se tachan
// por si alguien los pasa por error.
import { createHash } from 'node:crypto';

const FORBIDDEN = /^(uid|user_id|token|purchase_token|purchasetoken|code|request_token|authorization|x-firebase-appcheck|private_key|key|secret|event_token|assertion|access_token|ip)$/i;

type Fields = Record<string, unknown>;

function sanitize(fields: Fields): Fields {
  const out: Fields = {};
  for (const [k, v] of Object.entries(fields)) {
    if (v === undefined) continue;
    out[k] = FORBIDDEN.test(k) ? '[redactado]' : v;
  }
  return out;
}

function emit(level: 'INFO' | 'WARN' | 'ERROR', msg: string, fields: Fields): void {
  const line = JSON.stringify({ level, msg, ...sanitize(fields) });
  if (level === 'ERROR') console.error(line);
  else if (level === 'WARN') console.warn(line);
  else console.log(line);
}

export const log = {
  info: (msg: string, fields: Fields = {}): void => emit('INFO', msg, fields),
  warn: (msg: string, fields: Fields = {}): void => emit('WARN', msg, fields),
  error: (msg: string, fields: Fields = {}): void => emit('ERROR', msg, fields),
};

/** Hash corto del UID para los logs: permite correlacionar sin guardar el UID (plan §5). */
export function uidHash(uid: string): string {
  return createHash('sha256').update(`af-uid:${uid}`).digest('hex').slice(0, 16);
}

/** Espacio de nombres de las métricas propias (lib/ops-stack.ts: METRICS_NAMESPACE). */
export const METRICS_NAMESPACE = 'AF/Backend';

/**
 * Métrica en formato EMF (CloudWatch Embedded Metric Format): una línea de log que CloudWatch convierte en
 * métrica, sin permisos extra. Las dimensiones siempre incluyen `Env`.
 */
export function metric(name: string, envName: string, extraDimensions: Record<string, string> = {}, value = 1): void {
  const dims = { Env: envName, ...extraDimensions };
  console.log(
    JSON.stringify({
      _aws: {
        Timestamp: Date.now(),
        CloudWatchMetrics: [
          { Namespace: METRICS_NAMESPACE, Dimensions: [Object.keys(dims)], Metrics: [{ Name: name, Unit: 'Count' }] },
        ],
      },
      ...dims,
      [name]: value,
    }),
  );
}
