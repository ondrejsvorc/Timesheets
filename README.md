## Timesheets

Web Application for Managing and Validating Timesheets.

### Test instance
- http://vykazy.ondrejsvorc.cz/

### Tech Stack
- **Backend:** .NET 10, ASP.NET Core Web API, Entity Framework Core (PostgreSQL), FluentValidation
- **Frontend:** React, TypeScript, Vite, React Router, Tailwind CSS, shadcn/ui
- **Other:** Docker, Docker Compose, GitHub Actions, EF Core Migrations

### Prerequisites
- **.NET SDK 10** (or later)
- **Node.js** (v18 or later) and **npm**
- **Docker Compose**, **Docker Desktop**
- **EF Core Tools** (for database migrations)

### Setup
1. Start Docker Desktop
1. ```cd Timesheets```
1. ```docker compose up --build```

### Code style & quality
Backend:
- **Format (writes)**:
  - `cd backend`
  - `dotnet format Timesheets.slnx`
- **Verify formatting (no changes)**:
  - `cd backend`
  - `dotnet format Timesheets.slnx --verify-no-changes`

Frontend:
- **Format (writes)**: `cd frontend && npm run format`
- **Lint/format check (no writes)**: `cd frontend && npm run lint`

#### pgAdmin
1. Go to http://localhost:5050
1. Email Address: **admin@admin.com**
1. Password: **admin**

Steps below are done automatically by interpreting **.containers/pgadmin/servers.json**. When you log in successfully, the DB server will be connected automatically.
1. **Add New Server**
1. General -> Name -> **db**
1. Connection -> Host name/address -> **timesheets.database**
1. Connection -> Username -> **postgres**
1. Connection -> Password -> **postgres**

#### Reset environment
```
docker compose down --volumes --rmi all --remove-orphans
```
Deletes all containers, images, and volumes so the project starts from scratch next time it's composed.

#### Rebuild backend
```
docker compose up --build timesheets.backend
```

#### URLs
- http://localhost:3000 (frontend)
- http://localhost:5000/swagger/index.html (API documentation)
- http://localhost:5050 (pgAdmin)