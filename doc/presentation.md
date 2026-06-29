# Presentation

This document captures the thought process behind this project, written for the technical
interview panel as the assignment's required presentation deliverable. It complements (rather
than repeats) [README.md](../README.md) (setup/run instructions), [SUBJECT.md](../SUBJECT.md)
(original requirements), [CLAUDE.md](../CLAUDE.md) (architecture/conventions reference), and
[doc/user-story.md](./user-story.md) (the informal user story). See
[doc/genai-tools.md](./genai-tools.md) for the dedicated GenAI tools writeup.

## 1. Overview

The assignment asked for a small full-stack task management system — CRUD on tasks, a user
model with registration/login, Clean Architecture on the backend, TDD throughout, and a
frontend of choice. The goal driving every decision below was to satisfy those requirements with
a design simple enough to defend line-by-line in a code review, rather than to add scope the
assignment didn't ask for.

## 2. User Story

Rather than inventing a generic "manage tasks" story, the user story
([doc/user-story.md](./user-story.md)) is framed around a single concrete persona — a freelancer
who needs a private, no-fuss task list and wants brute-force login attempts to be slowed down
rather than ignored. That framing is what motivated two features that go beyond a bare CRUD
exercise: per-user data isolation (Section 8 below) and the escalating account lockout policy
(Section 5 of CLAUDE.md). Both are explicit acceptance criteria in the user story, not
afterthoughts.

## 3. Domain & Database Design

Four tables: `users`, `tasks`, `users_tasks`, `task_logs`.

- **`users_tasks` as a bridge table**, rather than a `user_id` column directly on `tasks`, was a
  deliberate choice to leave room for a future "reassign task to another user" feature without a
  schema change — the unique index on `id_task` currently enforces "one owner at a time," but the
  bridge already models the relationship correctly. This was scoped as *data-model readiness*,
  not as a feature to build now (it isn't — see Section 9, Out of Scope).
- **`task_logs.task_id` is deliberately not a real foreign key.** The audit trail is required to
  survive deletion of the task it describes (you should still be able to see "this task was
  created, then updated twice, then deleted" after the delete). A real FK with cascade delete
  would erase that history along with the task; a real FK without cascade would block deletion
  entirely. Neither is acceptable, so the FK constraint was dropped in favor of recording a plain
  UUID and relying on `created_by` (a real FK to `users`) for accountability.
- **`tasks` has no `created_at`/`updated_at`.** That history already lives in `task_logs` as
  before/after JSON snapshots, so duplicating timestamps on `tasks` itself would be redundant
  state that could drift out of sync with the log.
- **Status is stored as text via an EF Core value conversion, not a native Postgres enum type.**
  A native Postgres enum requires a migration to add new statuses; a text column with an EF Core
  conversion only requires a C# change. Given the assignment's scope, the flexibility outweighed
  the marginal storage/validation benefit of a DB-level enum.

## 4. Clean Architecture

`Api → BusinessLogic → Data/Domain`, with `Database/Infrastructure` implementing interfaces
defined in the inner layers (dependency inversion) instead of being depended on directly.

The one place this got tested in practice: implementing `IPasswordHasher` (an interface defined
in BusinessLogic) inside the Database project required adding a `Database → BusinessLogic`
project reference. The instinct was to be suspicious of any new project reference, but Clean
Architecture's actual rule is that inner layers must never depend on outer ones — the reverse is
fine. Checked the rule against the dependency graph before accepting the reference, rather than
rejecting it on a "more references = bad" heuristic.

Patterns were intentionally kept minimal — no generic repository abstraction beyond what each
aggregate actually needed, no mediator/CQRS layer — because the project has four backend layers
plus tests and a deadline; adding abstraction for hypothetical future use cases would have cost
more in review time than it would have earned in flexibility.

## 5. Authentication & the Lockout Policy

JWT-backed auth (HMAC-SHA256, 1 hour expiry), login by `username` (no email field exists in the
schema — see Section 4 of CLAUDE.md for why). Passwords are hashed with BCrypt and never appear
in logs or responses.

The escalating lockout schedule (3 failures → 5 min, 5 → 10 min, 7 → 20 min, 9 → 30 min, then 60
min repeating from the 11th failure on) was specified explicitly as a business rule up front,
not left to a generic "lock after N attempts" default — the user story calls for the lockout to
*escalate* the longer the attacker persists, not just trigger once. `failed_login_attempts`
accumulates and only resets on a successful login; while `locked_until` is in the future, the
password isn't even checked, so a locked-out attacker can't use failed attempts to fish for
whether the password is close.

Unknown usernames and wrong passwords return the same generic `401` — deliberately, to avoid
leaking which usernames exist.

## 6. Testing Strategy (TDD)

Tests were written before implementation throughout, across three layers:

- **`BusinessLogic.UnitTests`** — pure unit tests (Moq + FluentAssertions), no external
  dependencies, fast feedback loop for business rules like the lockout schedule.
- **`Database.UnitTests`** — integration tests against a **real** Postgres instance (not an
  in-memory provider), specifically because an in-memory double wouldn't catch real Npgsql/EF
  Core behavior (e.g. the JSONB columns on `task_logs`, the unique index on `users_tasks`).
- **Frontend** — Vitest + React Testing Library, same test-first discipline.

Backend: 77 xUnit tests. Frontend: 47 Vitest tests.

Two real bugs were only caught by *running* the application, not by the test suite — see Section
"Validation & Corrections" in [doc/genai-tools.md](./genai-tools.md) for the details (enum
serialization shape, and stale `TaskForm` state on re-edit). That's why manual end-to-end
verification (curl against the running API, clicking through the real frontend) stayed a
mandatory step before considering anything done, even with a green test suite.

## 7. Frontend Design

React + TypeScript on Vite, React's built-in `useState`/`useContext` for state (no external state
library — the app's state needs didn't justify one). `TaskForm` is reused for both create and
edit, controlled by a `key` prop in `TasksPage` so React remounts it with fresh state whenever the
editing target changes — the fix for the stale-state bug mentioned above, made permanent as a
documented convention in CLAUDE.md rather than a one-off patch.

Validation is deliberately duplicated: a frontend layer (`auth/validation.ts`) for immediate user
feedback, and an independent backend layer in BusinessLogic that never trusts the frontend's
checks. This is standard defense-in-depth, not an oversight — the two layers are kept consistent
(same minimum password length, same required fields) but neither is allowed to skip its own
check on the assumption the other already did it.

## 8. Data Isolation

Every task read/update/delete is scoped to the authenticated user via the `users_tasks` join. A
task that exists but belongs to someone else returns the same `404` as a task that doesn't exist
at all — confirmed end-to-end with a second registered user attempting to access the first user's
task. The alternative (a `403 Forbidden`) would have confirmed to an attacker that the task ID is
valid and just not theirs; returning `404` for both cases avoids leaking that information.

## 9. Out of Scope (and why)

Deliberately not implemented, to keep scope matched to the assignment rather than gold-plating:

- Reassigning a task to a different user (the `users_tasks` schema supports it; no endpoint
  exists).
- Password reset.
- Admin-level account management (e.g. manually unlocking a user).

## 10. GenAI Tools

A GenAI coding assistant (Claude Code) was used throughout this project, not just for the
dedicated GenAI section of the assignment — see [doc/genai-tools.md](./genai-tools.md) for the
full writeup: the actual prompts used, a representative sample of generated code, and how every
generated change was validated (tests + real execution) and, in two cases, corrected after a bug
only surfaced by running the app for real.
