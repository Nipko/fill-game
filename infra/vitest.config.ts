import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    include: ['test/**/*.test.ts'],
    environment: 'node',
    // La síntesis de los stacks empaqueta las Lambdas con esbuild: tarda unos segundos.
    testTimeout: 120_000,
    hookTimeout: 120_000,
  },
});
