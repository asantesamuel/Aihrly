# Aihrly — Junior Backend Developer Take-Home Assessment

## What I Built

<!-- 3–5 sentence summary of what you built and what you'd do next with more time -->

---

## Seeded Team Members

These team members are pre-populated on every fresh database via EF Core migrations.
Use their IDs in the `X-Team-Member-Id` header when making requests.

| Name         | Role           | ID                                   |
|--------------|----------------|--------------------------------------|
| Alice Mensah | Recruiter      | 00000000-0000-0000-0000-000000000001 |
| Bob Asante   | Recruiter      | 00000000-0000-0000-0000-000000000002 |
| Carol Owusu  | HiringManager  | 00000000-0000-0000-0000-000000000003 |

---

## How to Run Locally

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL running locally (default port 5432)

### 1. Clone the repo
```bash
git clone <your-repo-url>
cd Aihrly
```

### 2. Update connection string
Edit `src/Aihrly.Api/appsettings.json`:
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=aihrly;Username=YOUR_USER;Password=YOUR_PASSWORD"
```

### 3. Run the API
```bash
cd src/Aihrly.Api
dotnet run
```

The API starts at `https://localhost:1141` (or `http://localhost:1141`).
Swagger UI is available at: `http://localhost:1141/swagger`

Migrations are applied automatically on startup. Seed data is included.

---

## How to Run the Tests

```bash
cd tests/Aihrly.Tests
dotnet test
```

To run a specific test class:
```bash
dotnet test --filter "FullyQualifiedName~StageTransitionRulesTests"
```

---

## Part 2 — Deep Dive: Background Job (Option A)

<!-- 3–5 sentences explaining your design decisions and trade-offs -->

---

## Assumptions Made

- **Closed jobs return 404**: When a candidate applies to a closed job, we return 404 rather than 400 to avoid leaking the existence of closed postings.
- **GET /api/jobs is public**: No `X-Team-Member-Id` required — candidates and team members alike can browse open jobs.
- **Score overwrite**: Submitting a score twice for the same dimension replaces the previous value. The original `scored_by` and `scored_at` are preserved; `updated_by` and `updated_at` are set on overwrite.
- **Migrations auto-apply**: The app runs `db.Database.MigrateAsync()` on startup so reviewers don't need to run migration commands manually.

---

## What I'd Improve With More Time

- Add score history tracking (a `ScoreHistory` table) — currently only the latest score per dimension is kept.
- Add integration tests using `WebApplicationFactory` + a real test PostgreSQL database.
- Add pagination metadata headers (X-Total-Count) to list responses.
- Add rate limiting on the public application submission endpoint.
- Add FluentValidation for richer, field-level validation error messages.

---

## Approximate Hours Spent

<!-- Be honest — they use this to calibrate, not to judge -->
~ X hours
