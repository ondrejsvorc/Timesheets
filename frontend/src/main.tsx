import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { AttendanceTimesheetImporter } from './AttendanceTimesheetImporter'

export const App = () => {
  return <AttendanceTimesheetImporter />
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
