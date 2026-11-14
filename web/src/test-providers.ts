import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { localeProviders } from './app/locale';

export default [...localeProviders, { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }];
