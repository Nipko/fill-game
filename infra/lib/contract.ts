// Lectura del índice del contrato v1 (infra/contracts/v1/index.json, fuente única de la API; Q-AWS-12).
import { readFileSync } from 'node:fs';
import * as path from 'node:path';
import { CONTRACTS_DIR } from './paths.ts';

export const FUNCTION_NAMES = [
  'verify-purchase',
  'restore-ledger',
  'rewarded-ssv',
  'delete-account',
  'voided-purchases-job',
  'league-wins',
  'league-board',
] as const;
export type FunctionName = (typeof FUNCTION_NAMES)[number];

export interface ContractOperation {
  readonly operation: string;
  readonly function: FunctionName;
  readonly method: 'GET' | 'POST';
  readonly path: string;
  readonly auth: 'firebase' | 'none';
  readonly integrity: boolean;
  readonly throttle: 'default' | 'delete';
  readonly requestIn?: 'body' | 'query';
  readonly request: string | null;
  readonly response: string;
  readonly examples: {
    readonly request: readonly string[];
    readonly response: readonly string[];
    readonly invalidRequest: readonly string[];
  };
  readonly errors: Readonly<Record<string, readonly string[]>>;
}

export interface ContractJob {
  readonly operation: string;
  readonly function: FunctionName;
  readonly input: string;
  readonly output: string;
  readonly examples: { readonly input: readonly string[]; readonly output: readonly string[] };
}

export interface ContractIndex {
  readonly version: 'v1';
  readonly operations: readonly ContractOperation[];
  readonly jobs: readonly ContractJob[];
}

export function loadContractIndex(): ContractIndex {
  const raw = JSON.parse(readFileSync(path.join(CONTRACTS_DIR, 'index.json'), 'utf8')) as ContractIndex;
  const known = new Set<string>(FUNCTION_NAMES);
  for (const op of [...raw.operations, ...raw.jobs]) {
    if (!known.has(op.function)) throw new Error(`Función desconocida en el contrato: ${op.function}`);
  }
  for (const op of raw.operations) {
    if (!op.path.startsWith('/v1/')) throw new Error(`Ruta fuera de /v1/: ${op.path}`);
  }
  return raw;
}

/** Clave de ruta de API Gateway: `POST /v1/...`. */
export function routeKey(op: ContractOperation): string {
  return `${op.method} ${op.path}`;
}
