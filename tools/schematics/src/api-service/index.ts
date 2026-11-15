import { normalize, strings } from '@angular-devkit/core';
import { Rule, apply, applyTemplates, mergeWith, move, url } from '@angular-devkit/schematics';

interface ApiServiceOptions {
  name: string;
  path: string;
}

export function apiService(options: ApiServiceOptions): Rule {
  const source = apply(url('./files'), [
    applyTemplates({ ...strings, name: options.name }),
    move(normalize(options.path)),
  ]);
  return mergeWith(source);
}
