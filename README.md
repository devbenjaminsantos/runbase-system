# RunBase

RunBase is an internal admin panel and subscription management system for managing clients, plans, orders, and access control in recurring operations.

The current direction is a modern product foundation with:

- Next.js, React, and TypeScript for the frontend
- ASP.NET Core Web API and C# for the backend
- Azure SQL for persistence
- JWT, refresh tokens, and role-based access control
- Azure Static Web Apps and Azure App Service for deployment
- GitHub Actions for CI/CD

The original Olympus Admin prototype is preserved under `legacy/` as product and UX reference.

For the Portuguese product narrative, version history, and planning notes, see [docs/PROJETO_E_PLANEJAMENTO.md](./docs/PROJETO_E_PLANEJAMENTO.md).

## Current Status

The new RunBase foundation currently includes:

- ASP.NET Core Web API under `backend/`
- Layered solution with `Api`, `Application`, `Domain`, and `Infrastructure`
- Interactive API documentation with Scalar
- Health endpoint at `/health`
- Basic JWT login at `/api/auth/login`
- Protected current-user endpoint at `/api/auth/me`
- Initial xUnit test project
- Legacy static frontend and Node backend preserved under `legacy/`

## Repository Structure

```text
RunBase/
  backend/
    RunBase.slnx
    src/
      RunBase.Api/
      RunBase.Application/
      RunBase.Domain/
      RunBase.Infrastructure/
    tests/
      RunBase.Application.Tests/
  legacy/
    frontend-static/
    backend-node/
  docs/
    PROJETO_E_PLANEJAMENTO.md
    RUNBASE_ROADMAP.md
    RUNBASE_START.md
```

## Backend Setup

From the repository root:

```bash
cd backend
dotnet restore RunBase.slnx
dotnet build RunBase.slnx
dotnet test RunBase.slnx --no-build
dotnet run --project src/RunBase.Api/RunBase.Api.csproj --launch-profile http
```

Local endpoints:

- API health: `http://localhost:5140/health`
- Auth login: `POST http://localhost:5140/api/auth/login`
- Current user: `GET http://localhost:5140/api/auth/me`
- Scalar API reference: `http://localhost:5140/scalar/v1`
- OpenAPI document: `http://localhost:5140/openapi/v1.json`

Development admin seed:

```json
{
  "email": "admin@runbase.local",
  "password": "Admin123!"
}
```

Environment connection string:

```text
ConnectionStrings__DefaultConnection
```

## Legacy Prototype

The legacy prototype contains the previous static admin UI and Node/Express backend.

Static frontend:

```bash
cd legacy/frontend-static
```

Open `index.html` directly or run it with a local static server.

Node backend:

```bash
cd legacy/backend-node/backend
npm install
npm run dev
npm test
```

Local MySQL for the legacy backend:

```bash
cd legacy
docker compose up -d
```

Then configure `legacy/backend-node/backend/.env` using `.env.example`.

## Documentation

- [Product and planning notes in Portuguese](./docs/PROJETO_E_PLANEJAMENTO.md)
- [RunBase roadmap](./docs/RUNBASE_ROADMAP.md)
- [Implementation start plan](./docs/RUNBASE_START.md)

## Author

Built by **Benjamin Montenegro**.
