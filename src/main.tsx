import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { AuthProvider } from '@/features/auth/AuthProvider'
import { CartProvider } from '@/features/cart/CartProvider'
import { ThemeProvider } from '@/shared/theme/ThemeProvider'

// Providers wrap the tree like root-level injectors: the nearest one above a component wins
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider>
      <AuthProvider>
        <CartProvider>
          <App />
        </CartProvider>
      </AuthProvider>
    </ThemeProvider>
  </StrictMode>,
)
