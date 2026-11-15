import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { AuthProvider } from '@/features/auth/AuthProvider'
import { CartProvider } from '@/features/cart/CartProvider'
import { ThemeProvider } from '@/shared/theme/ThemeProvider'

// One cache for the whole app; data counts as fresh for 30 seconds
const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 30_000 } },
})

// Providers wrap the tree like root-level injectors: the nearest one above a component wins
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <AuthProvider>
          <CartProvider>
            <App />
          </CartProvider>
        </AuthProvider>
      </ThemeProvider>
    </QueryClientProvider>
  </StrictMode>,
)
