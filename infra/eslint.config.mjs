// ESLint (flat config) de infra/: solo typescript-eslint (paquetes aprobados en D-035).
import { defineConfig } from 'eslint/config';
import tseslint from 'typescript-eslint';

export default defineConfig(
  {
    ignores: ['node_modules/**', 'cdk.out/**', '**/*.d.ts'],
  },
  ...tseslint.configs.recommendedTypeChecked,
  {
    languageOptions: {
      parserOptions: {
        projectService: {
          allowDefaultProject: ['eslint.config.mjs'],
        },
        tsconfigRootDir: import.meta.dirname,
      },
    },
    rules: {
      'no-console': 'off',
      'eqeqeq': ['error', 'always'],
      '@typescript-eslint/consistent-type-imports': 'error',
      '@typescript-eslint/no-floating-promises': 'error',
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
    },
  },
  {
    files: ['eslint.config.mjs'],
    ...tseslint.configs.disableTypeChecked,
  },
);
