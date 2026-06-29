# User Story

As a freelancer juggling a handful of small projects at once, I keep losing track of what I
still need to do. I want a small, no-fuss web app where I can log in and see my own task list —
nothing shared, nothing complicated, just my tasks, their status, and when they're due. I want to
be confident that if someone gets a hold of my password, the system at least slows them down
instead of letting them try forever.

## Primary Story

As a registered user of the Task Management System, I want to log in securely and manage my own
list of tasks — create, view, update, and delete — so that I can keep track of what I need to do
without my data getting mixed up with anyone else's.

## Supporting Stories

1. As a new user, I want to register an account with my name and a username/password, so that I
   can start using the system.
2. As a returning user, I want to log in with my username and password, so that I can access my
   tasks.
3. As a security-conscious user, I want my account to lock temporarily after repeated failed
   login attempts (escalating the longer it keeps happening), so that someone guessing my
   password can't just keep trying forever.
4. As a user, I want to create a task with a title, an optional description, a status, and an
   optional due date, so that I can capture what needs to get done and by when.
5. As a user, I want to see a list of all my tasks, so that I can get an overview of my workload
   at a glance.
6. As a user, I want to update a task's details — including its status — as my work progresses,
   so that the list stays accurate.
7. As a user, I want to delete a task once it's no longer relevant, so that my list doesn't get
   cluttered with stale items.
8. As a user, I want to be sure that only I can see and modify my own tasks, so that my work
   stays private even though other people use the same system.

## Actors

- **Registered User** — the only actor in the current scope. Registers an account, logs in, and
  manages exclusively their own tasks.
- *(Not in scope yet)* **Administrator** — could manage other users' accounts (e.g. manually
  unlock one) or reassign tasks between users. Not implemented; `users_tasks` is modeled so this
  could be added later without a schema change.

## Acceptance Criteria

- A user can register with name, last name, username, and a password of at least 8 characters;
  registering with a username that's already taken is rejected.
- A user can log in with username + password and receive a JWT (1 hour expiry) on success.
- After 3 consecutive failed login attempts the account locks for 5 minutes. If failures keep
  happening after each unlock, the lockout escalates (10, then 20, then 30, then 60 minutes,
  repeating at 60 from there). A successful login resets the failure count to zero.
- While an account is locked, login is rejected outright — even with the correct password —
  until the lockout window passes.
- A logged-in user can create, list, view, update, and delete tasks. Every one of those actions
  is scoped to tasks the authenticated user owns.
- Trying to view, update, or delete a task that belongs to someone else behaves identically to
  that task not existing at all (`404`) — the system never reveals that another user's task
  exists.
- Every create, update, and delete of a task is recorded with a before/after snapshot in an
  audit log, including who made the change and when.

## Out of Scope (for now)

- Reassigning a task to a different user (the data model supports a future reassignment feature
  via `users_tasks`, but no endpoint exists yet).
- Password reset / "forgot password" flow.
- Admin-level account management (e.g. manually locking or unlocking a user).
