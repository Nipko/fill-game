// Limpieza común de los tests: las carpetas temporales del sitio con dominio (lib/web-site.ts).
import { afterAll } from 'vitest';
import { cleanupGeneratedSites } from '../lib/web-site.ts';

afterAll(() => {
  cleanupGeneratedSites();
});
