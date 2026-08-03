import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// In production nginx proxies /api/ and /uploads/ to the API container, so the
// app uses root-relative paths everywhere. These proxies give the dev server the
// same shape — which is what lets the paths stored in the database stay relative
// and therefore survive moving between localhost and a real domain.
const API_ORIGIN = process.env.VITE_DEV_API_ORIGIN || 'http://localhost:5152'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': { target: API_ORIGIN, changeOrigin: true },
      '/uploads': { target: API_ORIGIN, changeOrigin: true },
    },
  },
})
