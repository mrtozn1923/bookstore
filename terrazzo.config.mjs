import { defineConfig } from '@terrazzo/cli';
import css from '@terrazzo/plugin-css';
import tailwind from '@terrazzo/plugin-tailwind';

// token id -> CSS variable name: palette.blue.600 -> --blue-600, color.text-muted -> --color-text-muted
const variableName = (token) => `--${token.id.replace(/^palette\./, '').replaceAll('.', '-')}`;
// an empty shadow list means "no shadow"
const noShadow = (token) => (token.$type === 'shadow' && token.$value.length === 0 ? 'none' : undefined);

export default defineConfig({
  tokens: ['./tokens/bookstore.resolver.json'],
  outDir: './web/styles/generated/',
  plugins: [
    css({
      filename: 'tokens.css',
      variableName,
      permutations: [
        {
          input: { theme: 'light', motion: 'default' },
          prepare: (css) => `:root {\n  color-scheme: light;\n  ${css}\n}`,
        },
        {
          input: { theme: 'dark', motion: 'default' },
          include: ['color.*', 'shadow.*'],
          transform: noShadow,
          prepare: (css) => `@media (prefers-color-scheme: dark) {\n  :root:not([data-theme="light"]) {\n    color-scheme: dark;\n    ${css}\n  }\n}`,
        },
        {
          input: { theme: 'light', motion: 'reduced' },
          include: ['duration.*'],
          prepare: (css) => `@media (prefers-reduced-motion: reduce) {\n  :root {\n    ${css}\n  }\n}`,
        },
      ],
    }),
    // the same tokens as a Tailwind v4 theme: bg-action, text-muted, p-4, rounded-md...
    tailwind({
      filename: 'tailwind-theme.css',
      template: '../../../tokens/tailwind.template.css', // relative to outDir
      theme: {
        color: {
          bg: 'color.bg', surface: 'color.surface', text: 'color.text', muted: 'color.text-muted',
          action: 'color.action', 'action-hover': 'color.action-hover', 'on-action': 'color.on-action', link: 'color.link',
        },
        spacing: { 1: 'space.1', 2: 'space.2', 3: 'space.3', 4: 'space.4', 6: 'space.6', 8: 'space.8', 12: 'space.12' },
        radius: { md: 'radius.md' },
        font: { sans: 'font.sans' },
      },
    }),
  ],
});
