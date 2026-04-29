# Aihrly - Junior Backend Developer Take-Home Assessment

## What I Built

Aihrly is a .NET 9 Web API for managing job postings and candidate applications in a simple hiring pipeline. It supports job listing, candidate application submission, stage transitions, stage history, team notes, scoring, and asynchronous notification recording when an application reaches a terminal stage. The API uses PostgreSQL with EF Core migrations and seeded team members for reviewer-friendly local setup.

---

## How to Run Locally

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL running locally on port `5432`

### 1. Clone the repo

```bash
git clone <your-repo-url>
cd Aihrly
```

### 2. Update the connection string

Edit `src/Aihrly.Api/appsettings.json` if your local PostgreSQL credentials differ:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=aihrly_db;Username=postgres;Password=admin"
```

### 3. Run the API

```bash
cd src/Aihrly.Api
dotnet run
```

The launch profile uses:

- HTTPS: `https://localhost:1140`
- HTTP: `http://localhost:1141`
- Swagger UI: `http://localhost:1141/swagger`

EF Core migrations are applied automatically on startup, and seed data is included in the migration.

### Docker

This repo does not currently include a `Dockerfile` or `docker-compose.yml`. To run it locally today, use the .NET SDK plus a local PostgreSQL instance as described above.

---

## How to Run the Tests

From the repo root:

```bash
dotnet test
```

Or from the test project folder:

```bash
cd tests/Aihrly.Tests
dotnet test
```

To run a specific test class:

```bash
dotnet test --filter "FullyQualifiedName~StageTransitionRulesTests"
```

---

## Seeded Team Members

These team members are pre-populated on every fresh database via EF Core migrations. Use their IDs in the `X-Team-Member-Id` header when making requests to protected team-side endpoints.

| Name         | Role          | ID                                   |
|--------------|---------------|--------------------------------------|
| Alice Mensah | Recruiter     | 00000000-0000-0000-0000-000000000001 |
| Bob Asante   | Recruiter     | 00000000-0000-0000-0000-000000000002 |
| Carol Owusu  | HiringManager | 00000000-0000-0000-0000-000000000003 |

---

## Part 2 - Deep Dive: Background Job (Option A)

When an application moves to `Hired` or `Rejected`, the request saves the stage change first and then enqueues a lightweight notification job. A hosted background service waits on the shared notification queue, drains jobs as they arrive, and writes notification records to the database. This keeps the HTTP request fast because the caller does not wait for notification processing. The current implementation records notifications in the database and logs processor activity; a production version would send real email/SMS/in-app notifications and use durable queueing with retries.

---

## Assumptions Made

- **Closed jobs return 404**: When a candidate applies to a closed job, the API returns 404 rather than 400 to avoid exposing closed postings.
- **Some endpoints are public**: Job browsing, application submission, and read-only application views do not require `X-Team-Member-Id`.
- **Team-side writes require a seeded team member**: Stage changes, notes, and scores require `X-Team-Member-Id`.
- **Score overwrite**: Submitting a score twice for the same dimension replaces the latest score. The original `scored_by` and `scored_at` are preserved; `updated_by` and `updated_at` are set on overwrite.
- **Notifications are terminal-stage only**: Notifications are queued only when an application reaches `Hired` or `Rejected`, not for every intermediate stage change.
- **Migrations auto-apply**: The app runs `db.Database.MigrateAsync()` on startup so reviewers do not need to run migration commands manually.

---

## What I'd Improve With More Time

- Add Docker support with a `Dockerfile` and `docker-compose.yml` for the API and PostgreSQL.
- Add score history tracking with a `ScoreHistory` table; currently only the latest score per dimension is kept.
- Add integration tests using `WebApplicationFactory` and a real test PostgreSQL database.
- Add durable notification processing with retry/backoff and a real email/SMS provider.
- Add pagination metadata headers such as `X-Total-Count` to list responses.
- Add rate limiting on the public application submission endpoint.
- Add FluentValidation for richer, field-level validation error messages.

---

## Approximate Hours Spent

~ X hours
