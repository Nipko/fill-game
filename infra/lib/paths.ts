// Rutas del proyecto infra/, independientes del directorio de trabajo.
import { fileURLToPath } from 'node:url';
import * as path from 'node:path';

/** Raíz de infra/ (donde están package.json y cdk.json). */
export const INFRA_ROOT = path.resolve(fileURLToPath(new URL('.', import.meta.url)), '..');

export const FUNCTIONS_DIR = path.join(INFRA_ROOT, 'functions');
export const CONTRACTS_DIR = path.join(INFRA_ROOT, 'contracts', 'v1');
export const WEB_SITE_DIR = path.join(INFRA_ROOT, 'web', 'site');
export const LOCK_FILE = path.join(INFRA_ROOT, 'package-lock.json');
