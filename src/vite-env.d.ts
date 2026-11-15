/// <reference types="vite/client" />
// FragmentInstance gets its DOM methods (focus, scrollIntoView, observeUsing…) from react-dom's types
/// <reference types="react-dom" />

interface ViteTypeOptions {
  // import.meta.env.VITE_TYPO is a compile error instead of undefined
  strictImportMetaEnv: unknown;
}

interface ImportMetaEnv {
  readonly VITE_SUPPORT_EMAIL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
