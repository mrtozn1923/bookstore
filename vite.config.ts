/// <reference types="vitest/config" />
import babel from '@rolldown/plugin-babel'
import react, { reactCompilerPreset } from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Bookstore.Api address for the dev proxy (README default: http://localhost:5080)
const apiTarget = process.env.BOOKSTORE_API ?? 'http://localhost:5080'

// https://vite.dev/config/
export default defineConfig(({ mode }) => ({
  // React Compiler 1.0: automatic memoization at build time (react.dev, Vite installation)
  plugins: [react(), babel({ presets: [reactCompilerPreset()] })],
  resolve: {
    // '@/...' imports come from "paths" in tsconfig.app.json
    tsconfigPaths: true,
    // `vite build --mode profiling`: production build with <Profiler> timings switched on
    alias: mode === 'profiling' ? [{ find: 'react-dom/client', replacement: 'react-dom/profiling' }] : [],
  },
  server: {
    proxy: {
      // /api/books -> http://localhost:5080/api/books, same origin for the browser
      '/api': { target: apiTarget, changeOrigin: true },
    },
  },
  test: {
    // A DOM in Node: jsdom implements document, window and localStorage
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
}))
