# CLAUDE.md

This file documents the working agreements and architectural conventions for this repository.
Future Claude Code sessions should read this file first.

## 1. Project Summary

A task management system built for a .NET technical interview. Backend is .NET 8 (ASP.NET Core
Web API) following Clean Architecture, frontend is React + TypeScript (Vite), database is
PostgreSQL. The original assignment requirements live in [SUBJECT.md](./SUBJECT.md) — treat it as
the source of truth for what's required. The informal user story driving development lives in
[doc/user-story.md](./doc/user-story.md).

## 2. Repository Layout

```
/backend       .NET 8 solution: Api, BusinessLogic, Data/Domain, Database/Infrastructure, Tests
/frontend      React + TypeScript app (Vite)
/doc           user-story.md, genai-tools.md, and other design notes
docker-compose.yml   PostgreSQL only
.vscode/       launch.json, tasks.json, settings.json (F5 brings up db + backend + frontend)
```

## 3. Architecture & Design Principles

- Clean Architecture with a strict dependency direction: `Api` → `BusinessLogic` →
  `Data`/`Domain`. `Database`/`Infrastructure` implements interfaces defined in the inner layers
  (dependency inversion) — it does not get depended on directly by `BusinessLogic`.
- The Business Logic layer must remain independent of both the API layer and the Data layer.
  Business rules and validation live here, not in controllers or repositories.
- Follow SOLID principles. Use design patterns only where they earn their complexity — avoid
  over-engineering given the modest scope of this project (four backend layers plus tests).
- Layer responsibilities:
  - **Api** — HTTP concerns: controllers, DTOs, request/response mapping, auth wiring.
  - **BusinessLogic** — use cases, business rules, validation.
  - **Data** — persistence abstractions (repository interfaces, domain entities).
  - **Database/Infrastructure** — EF Core implementation, PostgreSQL specifics, migrations.

## 4. Domain Schema

These tables are decided — do not redesign them without explicit discussion.

- **users**: `id` (GUID PK), `name`, `lastname`, `username`, `password_hash`, `birthday`
  (nullable date), `failed_login_attempts` (int, default 0), `locked_until` (nullable timestamp),
  `created_at`, `updated_at`. No email field — login is by `username`. No gender field. There is
  no `is_locked` boolean — lockout state is derived from `locked_until` (locked while it's in the
  future); `failed_login_attempts` accumulates and only resets to 0 on a successful login.
- **tasks**: `id` (GUID PK), `title` (required), `description` (nullable), `status` (enum:
  `Pending`, `In Progress`, `Blocked`, `Completed`, `Cancelled` — stored as text via an EF Core
  value conversion, not a native Postgres enum type), `due_date` (nullable). No `created_at`/
  `updated_at` on this table by design — history lives in `task_logs`. The C# entity is named
  `TaskItem` (not `Task`) purely to avoid colliding with `System.Threading.Tasks.Task`; the table
  itself is still `tasks`. Likewise the C# enum is `TaskItemStatus`, not `TaskStatus`, to avoid
  colliding with `System.Threading.Tasks.TaskStatus`.
- **users_tasks**: `id` (GUID PK), `id_user`, `id_task` — bridge table with a unique index on
  `id_task` (one current owner per task) and real FKs (cascade delete) to `users`/`tasks`.
  Represents the single current owner per task; reassigning a task to another user is not
  implemented yet (deferred — `users_tasks` already supports it when needed).
- **task_logs**: `id` (GUID PK), `task_id` (deliberately **not** a real FK — the audit trail must
  survive deletion of the task it refers to), `old_value`/`new_value` (jsonb — full `TaskItem`
  snapshot before/after the change, `null` on create/delete respectively), `comment`
  (system-generated: "Task created.", "Task updated.", "Task deleted." — not user-supplied),
  `created_by` (real FK → `users.id`, the acting user), `created_at`.
- **Task ownership scope**: every `Task` read/update/delete is scoped to the authenticated user
  via `users_tasks` — a task that exists but belongs to someone else is treated identically to a
  task that doesn't exist (`404`), to avoid leaking existence of other users' data.

## 5. Authentication

- JWT-based, backed by `users.password_hash` and `username` for login (no email field exists).
- Tokens are signed HMAC-SHA256, 1 hour expiry, issued by `Api/Auth/JwtTokenGenerator`. Claims:
  `NameIdentifier` (user id), `Name` (username), `GivenName`/`Surname` (name/lastname). Dev
  secret/issuer/audience live in `appsettings.json` under `Jwt` (not a real secret — local demo
  only, consistent with the rest of the dev credentials in this repo).
- `POST /api/auth/login` and `POST /api/users` (registration) are anonymous endpoints.
  `GET /api/users/me` is `[Authorize]`-protected — the reference "authorized endpoint" required
  by the assignment.
- **Account lockout policy** (sequential, in `BusinessLogic/Users/LoginService`):
  `failed_login_attempts` accumulates across attempts and never resets except on a successful
  login. While `locked_until` is in the future, login is rejected outright (password is not even
  checked, and the counter does not increment). Lockout triggers only on these cumulative
  failure counts:
  - 3 → locked 5 minutes
  - 5 → locked 10 minutes
  - 7 → locked 20 minutes
  - 9 → locked 30 minutes
  - 11, 13, 15, ... (every odd count ≥ 11) → locked 60 minutes each time
  - Even counts (4, 6, 8, 10, ...) — the single attempt made right after an automatic unlock —
    do not trigger a new lock; they just increment the counter toward the next odd threshold.
- Unknown usernames and wrong passwords both return the same generic `401` (no user enumeration).
  A locked account returns `423 Locked` with the `lockedUntilUtc` timestamp in the response body.

## 6. Testing Policy (TDD)

- TDD is mandatory: tests precede implementation for backend work.
- This is a collaborative, learn-as-we-go process — the user is building testing skills, so work
  through test design together rather than writing tests unilaterally.
- Unit tests are required across the Data layer, Business Logic layer, and API endpoints.
- `BusinessLogic.UnitTests` are pure unit tests (mocked dependencies via Moq, assertions via
  FluentAssertions) — no external services required to run them.
- `Database.UnitTests` are integration tests that run against a **real Postgres instance**
  (the `task_management_system_test` database, created automatically by the docker-compose
  init script), not an in-memory provider — this catches real Npgsql/EF Core behavior. Postgres
  must be running (`docker compose up -d`) before executing this test project.
- EF Core migrations are managed with a local `dotnet-ef` tool (`backend/.config/dotnet-tools.json`,
  pinned to 8.0.11 to match the project's EF Core version) — run `dotnet tool restore` once after
  cloning, then `dotnet tool run dotnet-ef migrations add <Name> --project src/Database/TaskManagementSystem.Database.csproj`.

## 7. Frontend Conventions

- React + TypeScript (Vite), with components and state clearly segregated and organized — this
  is a standing requirement, not optional polish.
- State management: React's built-in `useState`/`useContext` — no external state library. Auth
  session lives in `auth/AuthContext.tsx` (+ `auth/useAuth.ts`, split out for Fast Refresh),
  persisted to `localStorage` under the `taskManagementSystem.auth` key.
- HTTP: native `fetch`, wrapped in `api/apiClient.ts` (`apiFetch<T>`), which throws `ApiError`
  (with `status`/`message`) on non-2xx responses. Per-resource API modules (`api/authApi.ts`,
  etc.) are thin wrappers around `apiFetch` — no business logic in the API layer.
- Routing: React Router (`/login`, `/tasks`), with `routes/ProtectedRoute.tsx` redirecting to
  `/login` when there's no authenticated session.
- Task CRUD UI: `components/TaskForm.tsx` (reused for both create and edit — controlled by a
  `key` prop in `pages/TasksPage.tsx` so React remounts it with fresh state when switching
  between create mode, editing a different task, or right after a successful create) and
  `components/TaskList.tsx` (presentational, takes `onEdit`/`onDelete` callbacks). `TasksPage`
  owns all the state and API calls; the components themselves don't talk to the API directly.
- Testing: Vitest + React Testing Library (`@testing-library/react`, `user-event`, `jest-dom`).
  Test files live next to the code they test (`Foo.tsx` / `Foo.test.tsx`). Run with `npm test`
  (single run) or `npm run test:watch`. Same TDD discipline as the backend: write the test first.
- Form fields require two layers of validation:
  1. Frontend type/format validation (e.g. `auth/validation.ts`), kept consistent with backend
     validation rules (same minimum password length, same required fields, etc).
  2. Backend validation in the Business Logic layer, performed independently regardless of what
     the frontend already checked (defense in depth — never trust the frontend alone).
- Backend CORS policy (`Program.cs`) only allows `http://localhost:5173` (the Vite dev server
  origin) — update it if the frontend's dev port ever changes.

## 8. Local Development

- `docker-compose.yml` runs **only PostgreSQL** (`task_management_system` database, simple
  hardcoded dev credentials — no security needed, this is local demo data only). A named volume
  persists data across restarts so seeded dev data is still there for the evaluator.
- The container maps to **host port 5433** (not the default 5432), since 5432 may already be in
  use by another local Postgres instance/container. The backend connection string must target
  `localhost:5433`.
- Backend and frontend run natively (not containerized), orchestrated by VS Code:
  pressing **F5** starts Postgres (if not already running), then the backend, then the frontend,
  via `.vscode/launch.json` and `.vscode/tasks.json`.
- The Api applies any pending EF Core migrations and seeds demo data automatically on startup
  (`Api/Seeding/DatabaseSeeder`, wired in `Program.cs` right after `builder.Build()`) — no manual
  `dotnet ef database update` step is needed to run the app. Seeding is idempotent (skipped if
  any user already exists) and goes through the normal `IUserRegistrationService`/`ITaskService`
  rather than raw inserts, so seeded data gets the same validation and audit trail as anything
  else. Demo credentials: username `demo`, password `Demo12345!` (see README.md).

## 9. Documentation Language Rule

**All documentation must be written in English, with no exceptions** — README.md, SUBJECT.md,
this file, everything under `/doc`, and any documentation-bearing file even if it's covered by
`.gitignore`. This applies regardless of what language the chat conversation happens in.

## 10. Confidentiality / Conversation Handling

This Claude Code conversation (and any other AI chat session) must **never** be persisted, logged,
committed, or written into project memory/files, unless the user explicitly requests it in the
moment. An external evaluator will review this repository, and only deliberately curated
AI-related artifacts should be visible — specifically the GenAI tools writeup described below.
Raw chat transcripts, prompt logs, or session data must never leak into the repo.

## 11. GenAI Tools Deliverable

The assignment requires documenting, under `/doc`, the prompts used with a GenAI coding tool,
sample output, and how the AI's suggestions were validated and corrected. This must be a
deliberate, curated writeup produced later in [doc/genai-tools.md](./doc/genai-tools.md) — it is
**not** a transcript of any chat session.
