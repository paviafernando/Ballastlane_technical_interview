# GenAI Tools

> This is a deliberately curated writeup for the assignment's GenAI tools section — not a
> transcript of any chat session. See [CLAUDE.md](../CLAUDE.md) for the confidentiality rule
> governing this distinction.

**Tool used:** Claude Code (Anthropic, Sonnet model), used interactively as an agentic coding
assistant with direct read/write access to this repository and a shell — not just a chat window
generating snippets to copy-paste. This made it possible to actually run tests, start the API,
and hit it with `curl`/a real browser as part of the workflow described below, rather than just
reading code and trusting it.

The assignment's hypothetical ("generate a RESTful API for a simple task management system: CRUD
on tasks with title/description/status/due_date, associated with a user") is, almost word for
word, exactly what this project's real Task CRUD feature needed. So instead of a toy example,
this writeup documents the actual prompts, output, and corrections from building it.

## Prompt(s) Used

The work was broken into focused, incremental prompts rather than one giant "build everything"
request — each one scoped to a single use case, following the project's TDD and Clean
Architecture conventions (already established in `CLAUDE.md` from earlier in the session, so
each prompt could assume those constraints instead of restating them):

1. *"Implement the Task CRUD use case: a `TaskService` in BusinessLogic (create/get/list/
   update/delete, scoped to the owning user), a `TaskItem`/`UserTask`/`TaskLog` entity set in
   Data, an EF Core repository in Database, and a `TasksController` in the Api — all via TDD,
   tests first."*
2. *"Add the account lockout policy to login: 3 failed attempts locks for 5 minutes, then 10,
   20, 30, and 60 minutes repeating, counted cumulatively and never reset except on a successful
   login."*
3. *"Scaffold the React + TypeScript frontend with Vite, Vitest, and React Testing Library, and
   build the login screen and the task list/create/edit/delete screens with the same TDD
   discipline used on the backend."*

Each prompt produced a full vertical slice (tests + implementation across every affected layer),
which was then reviewed, run, and corrected before moving to the next one.

## Sample Output

A representative sample — the lockout-duration calculation from
[`LoginService.cs`](../backend/src/BusinessLogic/Users/LoginService.cs), generated in response to
prompt 2 above:

```csharp
private static TimeSpan? GetLockoutDuration(int failedAttempts)
{
    foreach (var (attempts, duration) in LockoutSchedule)
    {
        if (failedAttempts == attempts)
        {
            return duration;
        }
    }

    if (failedAttempts >= 11 && failedAttempts % 2 == 1)
    {
        return RepeatingLockoutDuration;
    }

    return null;
}
```

This was correct on the first pass and needed no changes — it's included here specifically
*because* it worked, as a counterpoint to the corrections below (not every generated change
needed fixing).

## Validation & Corrections

Every generated slice went through the same two-step validation, never just one:

1. **Automated tests.** Backend: 77 xUnit tests (Moq + FluentAssertions), including integration
   tests that run against a real, ephemeral Postgres instance rather than an in-memory provider
   — specifically to catch Npgsql/EF Core behavior an in-memory double would hide. Frontend: 47
   Vitest + React Testing Library tests. Every feature was written test-first (red, confirmed
   failing for the right reason, then green).
2. **Real execution.** After tests passed, the actual API was started and driven with `curl`
   (register → login → trigger a real lockout → confirm the `423` response and timing), and the
   actual frontend was driven in a real headless browser (via a temporarily-installed Playwright
   script, removed afterward since it wasn't part of the agreed stack) to click through login and
   the full task CRUD flow.

That second step caught real issues the unit tests didn't:

- **Enum serialization.** The API was returning task `status` as a raw integer (`0`, `1`, ...)
  instead of a readable string, only visible by actually inspecting an HTTP response — not from
  reading the code or from the unit tests, which asserted against the C# enum value, not its
  JSON shape. Fixed by registering a global `JsonStringEnumConverter`.
- **Stale form state.** The `TaskForm` component is reused for both creating and editing a task.
  Because its internal state only initializes once on mount, switching from "create" to "edit"
  (or to editing a *different* task) kept showing the previous task's field values — only visible
  by actually clicking "Edit" in a real browser, since the unit tests for `TaskForm` exercised it
  in isolation and never re-rendered it with new props. Fixed by giving it a `key` derived from
  which task (if any) is being edited, so React remounts it with fresh state.
- **Package version drift.** A couple of NuGet packages (`Npgsql.EntityFrameworkCore.PostgreSQL`,
  EF Core Design tooling) initially resolved to a newer major version incompatible with the
  project's `net8.0` target. Caught immediately by `dotnet build` failing; fixed by pinning exact
  compatible versions.
- **Dependency direction.** Implementing `IPasswordHasher` (defined in BusinessLogic) inside the
  Database project required adding a `Database → BusinessLogic` project reference. This was
  reviewed against Clean Architecture's actual rule (inner layers must never depend on outer
  ones — the reverse is fine) before accepting it, rather than assuming "more references = bad."

## Edge Cases, Auth & Validation Handling

- **Authentication:** JWT (HMAC-SHA256, 1-hour expiry); passwords hashed with BCrypt and never
  logged or returned in any response.
- **Account lockout:** the schedule above is unit-tested with an injected `TimeProvider` for
  deterministic timing (no `Thread.Sleep` in tests), and separately confirmed end-to-end by
  actually failing a login 3 times against the running API and observing the real `423` response.
- **Authorization / data isolation:** every task operation is scoped server-side to the
  authenticated user via a join against `users_tasks`. A task that exists but belongs to another
  user returns the same `404` as a task that doesn't exist — confirmed with a second registered
  user attempting to read the first user's task.
- **Input validation:** enforced in the BusinessLogic layer independently of the frontend (e.g.
  required title, minimum password length) — covered by tests that call the service layer
  directly, bypassing the frontend and its own (separate, intentionally duplicated) validation.
- **Audit trail:** `task_logs.task_id` deliberately has no real foreign-key constraint, so a
  task's change history survives the task itself being deleted — confirmed by deleting a task
  through the API and then querying `task_logs` directly in Postgres afterward.

## Takeaway

GenAI output was treated as a first draft, not a finished answer. Every business rule (the exact
lockout schedule, ownership scoping, the audit log's shape) was specified explicitly up front
rather than left to the model's judgment, and nothing was marked done without both a green test
suite and a real run against the actual API/UI. Both real bugs caught during this work (enum
serialization, stale form state) were found by *executing* the code, not by reading it —
which is the main reason "run it for real" stayed a non-negotiable step throughout, not just
"trust the tests."
