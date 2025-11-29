import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { AppFooter } from './common/AppFooter'
import { AppHeader } from './common/AppHeader'
import './index.css'
import { ProjectsPage } from './projects/ProjectsPage'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <div className="min-h-screen flex flex-col">
      <AppHeader />
      <main className="flex-1 w-full mx-auto max-w-7xl px-6 py-8">
        <ProjectsPage />
      </main>
      <AppFooter />
    </div>
  </StrictMode>,
)
