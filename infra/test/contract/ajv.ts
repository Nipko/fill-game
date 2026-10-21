// Ajv (draft 2020-12, modo estricto) con todos los esquemas de contracts/v1 cargados.
import { Ajv2020 } from 'ajv/dist/2020.js';
import type { ValidateFunction } from 'ajv/dist/2020.js';
import { readdirSync, readFileSync } from 'node:fs';
import * as path from 'node:path';
import { CONTRACTS_DIR } from '../../lib/paths.ts';

export const SCHEMA_BASE = 'https://contracts.invalid/af/v1/';

export const SCHEMA_FILES = readdirSync(CONTRACTS_DIR).filter((f) => f.endsWith('.schema.json')).sort();

const ajv = new Ajv2020({ strict: true, allErrors: true });
for (const f of SCHEMA_FILES) {
  ajv.addSchema(JSON.parse(readFileSync(path.join(CONTRACTS_DIR, f), 'utf8')) as object);
}

export function validator(schemaFile: string): ValidateFunction {
  const v = ajv.getSchema(SCHEMA_BASE + schemaFile);
  if (!v) throw new Error(`Esquema no encontrado: ${schemaFile}`);
  return v;
}

export function readExample(file: string): unknown {
  return JSON.parse(readFileSync(path.join(CONTRACTS_DIR, 'examples', file), 'utf8'));
}
