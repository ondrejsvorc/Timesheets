import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'

export const App = () => {
  return <p>App is running</p>
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
