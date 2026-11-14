// NgRx lint rules for the store code (npm run lint)
import ngrx from '@ngrx/eslint-plugin';
import tseslint from 'typescript-eslint';

export default tseslint.config({
  files: ['src/**/*.ts'],
  languageOptions: { parser: tseslint.parser },
  extends: [...ngrx.configs.store, ...ngrx.configs.effects, ...ngrx.configs.signals, ...ngrx.configs.operators],
});
