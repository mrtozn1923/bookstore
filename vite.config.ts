/// <reference types="vitest/config" />
import babel from '@rolldown/plugin-babel'
import react, { reactCompilerPreset } from '@vitejs/plugin-react'
import { visualizer } from 'rollup-plugin-visualizer'
import { defineConfig } from 'vite'

// Bookstore.Bff address for the dev proxy (bff/Bookstore.Bff, launchSettings.json: http://localhost:5100)
const bffTarget = process.env.BOOKSTORE_BFF ?? 'http://localhost:5100'

// https://vite.dev/config/
export default defineConfig(({ mode }) => ({
  // React Compiler 1.0: automatic memoization at build time (react.dev, Vite installation)
  plugins: [
    react(),
    babel({ presets: [reactCompilerPreset()] }),
    // `vite build --mode analyze`: dist/stats.html shows which modules fill each chunk
    mode === 'analyze' && visualizer({ filename: 'dist/stats.html', template: 'treemap', gzipSize: true }),
  ],
  resolve: {
    // '@/...' imports come from "paths" in tsconfig.app.json
    tsconfigPaths: true,
    // `vite build --mode profiling`: production build with <Profiler> timings switched on
    alias: mode === 'profiling' ? [{ find: 'react-dom/client', replacement: 'react-dom/profiling' }] : [],
  },
  server: {
    proxy: {
      // The browser talks only to the dev server; /api and /bff go on to the BFF, the BFF forwards /api to Bookstore.Api
      '/api': { target: bffTarget },
      '/bff': { target: bffTarget },
    },
  },
  test: {
    // A DOM in Node: jsdom implements document, window and localStorage
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    // next/ and upgrade-lab/ are separate projects with their own setup
    include: ['src/**/*.test.{ts,tsx}'],
  },
}))
