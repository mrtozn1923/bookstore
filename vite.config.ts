import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Bookstore.Api address for the dev proxy (README default: http://localhost:5080)
const apiTarget = process.env.BOOKSTORE_API ?? 'http://localhost:5080'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    // '@/...' imports come from "paths" in tsconfig.app.json
    tsconfigPaths: true,
  },
  server: {
    proxy: {
      // /api/books -> http://localhost:5080/api/books, same origin for the browser
      '/api': { target: apiTarget, changeOrigin: true },
    },
  },
})
