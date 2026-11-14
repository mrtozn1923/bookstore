// @ts-check
import { defineConfig } from 'eslint/config';
import rxjsX from 'eslint-plugin-rxjs-x';
import tseslint from 'typescript-eslint';

export default defineConfig({
  files: ['src/**/*.ts'],
  extends: [tseslint.configs.recommended, rxjsX.configs.recommended],
  languageOptions: {
    // the RxJS rules need type information: is this an Observable, a Subject, a Subscription?
    parserOptions: { projectService: true, tsconfigRootDir: import.meta.dirname },
  },
  rules: {
    // not in "recommended", but the ones this series cares about
    'rxjs-x/no-ignored-subscription': 'error',
    'rxjs-x/no-exposed-subjects': 'error',
    'rxjs-x/no-floating-observables': 'error',
    // treat takeUntilDestroyed like takeUntil: nothing but finalize & co. may follow it
    'rxjs-x/no-unsafe-takeuntil': ['error', { alias: ['takeUntilDestroyed'] }],
  },
});
