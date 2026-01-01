# 📊 Timesheets

Web application for **managing and auditing employee timesheets**.

## Technology

### Backend
- **.NET 10.0** (ASP.NET Core Web API)
- **Entity Framework Core** with PostgreSQL
- **SignalR** for real-time notifications
- **OpenID Connect** authentication
- **FluentValidation** for request validation
- **Swagger/OpenAPI** for API documentation

### Frontend (POC)
- **React 19** with **TypeScript**
- **Vite** for build tooling
- **React Router 7** for navigation
- **Tailwind CSS 4** for styling
- **shadcn/ui** + **Radix UI** for components
- **React Hook Form** + **Zod** for form validation
- **TanStack Table** for data tables

### Database
- **PostgreSQL** (containerized with Docker)

### DevOps
- **Docker** & **Docker Compose** for containerization
- **EF Core Migrations** for database schema management

## Setup

### Prerequisites
- **.NET SDK 10.0** (or later)
- **Node.js** (v18 or later) and **npm**
- **Docker Desktop** (for running PostgreSQL database)
- **EF Core Tools** (for database migrations)