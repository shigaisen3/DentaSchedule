import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import basicSsl from '@vitejs/plugin-basic-ssl'

export default defineConfig({
  plugins: [react(), tailwindcss(), basicSsl()],
  build: {
    outDir: '../DentaSchedule.API/wwwroot',
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    // HTTPS for the dev server is provided by the basicSsl() plugin above.
    proxy: {
      '/api': {
        target: 'https://localhost:7237',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
