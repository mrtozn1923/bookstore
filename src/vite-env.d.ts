/// <reference types="vite/client" />

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
