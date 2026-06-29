# Task Management System

A full-stack task management application built as a technical interview exercise. It consists of
a .NET 8 backend following Clean Architecture principles, a React + TypeScript frontend, and a
PostgreSQL database, developed using Test-Driven Development (TDD).

> The original assignment requirements are preserved verbatim in [SUBJECT.md](./SUBJECT.md).
> Project conventions and working agreements for development are documented in
> [CLAUDE.md](./CLAUDE.md).

## Architecture

This is a monorepo with three top-level folders:

- `/backend` — .NET 8 solution following Clean Architecture (API, Business Logic, Data/Domain,
  Database/Infrastructure layers, plus unit tests).
- `/frontend` — React + TypeScript application (built with Vite).
- `/doc` — design notes, the user story driving development, and the GenAI tools writeup.

See [CLAUDE.md](./CLAUDE.md) for the full set of architectural and development conventions.

## Tech Stack

- **Backend:** .NET 8, ASP.NET Core Web API, Entity Framework Core
- **Database:** PostgreSQL (containerized via Docker Compose)
- **Frontend:** React, TypeScript, Vite, React Router
- **Authentication:** JWT
- **Testing:** TDD throughout — xUnit/Moq/FluentAssertions on the backend, Vitest/React Testing
  Library on the frontend

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (LTS)
- [Docker](https://www.docker.com/) and Docker Compose

## Getting Started

> Setup instructions below will be expanded as the project is built out.

1. **Start the database:**
   ```bash
   docker compose up -d
   ```
2. **Run the backend:**
   ```bash
   cd backend
   dotnet run --project src/Api
   ```
   On startup the API automatically applies any pending EF Core migrations and seeds a demo
   user with a few demo tasks if the database is empty — no manual migration step needed.
3. **Run the frontend:**
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

Alternatively, press **F5** in VS Code to start the database, backend, and frontend together
(see `.vscode/launch.json`).

### Demo credentials

The API seeds a demo user (and 5 demo tasks in different statuses) on first run:

- **Username:** `demo`
- **Password:** `Demo12345!`

Use these to log in via `POST /api/auth/login` and explore the task CRUD endpoints.

## Running Tests

**Backend** (requires Postgres running for the `Database.UnitTests` integration tests):
```bash
cd backend
dotnet test
```

**Frontend:**
```bash
cd frontend
npm test
```

## Documentation

- [SUBJECT.md](./SUBJECT.md) — original assignment requirements
- [CLAUDE.md](./CLAUDE.md) — architecture, conventions, and working agreements
- [doc/user-story.md](./doc/user-story.md) — the user story driving development
- [doc/genai-tools.md](./doc/genai-tools.md) — GenAI tools usage writeup
- [doc/presentation.md](./doc/presentation.md) — presentation: thought process, design choices,
  and trade-offs
