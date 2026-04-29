# ANSWERS.md

---

## Question 1 - Schema Design

### Tables

```sql
-- Applications
CREATE TABLE applications (
    id               UUID PRIMARY KEY,
    job_id           UUID NOT NULL REFERENCES jobs(id),
    candidate_name   VARCHAR(300) NOT NULL,
    candidate_email  VARCHAR(320) NOT NULL,
    cover_letter     TEXT,
    stage            VARCHAR(50)  NOT NULL DEFAULT 'Applied',
    applied_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE (job_id, candidate_email)  -- prevents duplicate applications
);
CREATE INDEX IX_Applications_JobId ON applications(job_id);
CREATE INDEX IX_Applications_Stage ON applications(stage);

-- Application Notes
CREATE TABLE application_notes (
    id              UUID PRIMARY KEY,
    application_id  UUID NOT NULL REFERENCES applications(id) ON DELETE CASCADE,
    type            VARCHAR(50)  NOT NULL,
    description     TEXT         NOT NULL,
    created_by_id   UUID         NOT NULL REFERENCES team_members(id),
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);
CREATE INDEX IX_ApplicationNotes_ApplicationId ON application_notes(application_id);

-- Stage History
CREATE TABLE stage_histories (
    id              UUID PRIMARY KEY,
    application_id  UUID NOT NULL REFERENCES applications(id) ON DELETE CASCADE,
    from_stage      VARCHAR(50)  NOT NULL,
    to_stage        VARCHAR(50)  NOT NULL,
    changed_by_id   UUID         NOT NULL REFERENCES team_members(id),
    changed_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    reason          TEXT
);
CREATE INDEX IX_StageHistories_ApplicationId ON stage_histories(application_id);

-- Application Scores
-- One row per application per score dimension.
CREATE TABLE application_scores (
    id               UUID PRIMARY KEY,
    application_id   UUID        NOT NULL REFERENCES applications(id) ON DELETE CASCADE,
    dimension        VARCHAR(50) NOT NULL, -- CultureFit | Interview | Assessment
    score            INT         NOT NULL CHECK (score BETWEEN 1 AND 5),
    comment          TEXT,
    scored_by_id     UUID        NOT NULL REFERENCES team_members(id),
    scored_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by_id    UUID        REFERENCES team_members(id),
    updated_at       TIMESTAMPTZ,
    UNIQUE (application_id, dimension)
);
CREATE INDEX IX_ApplicationScores_ApplicationId_Dimension
    ON application_scores(application_id, dimension);
```

### Indexes and Why

- `IX_Applications_JobId`: listing applications for a job filters by `job_id`.
- `IX_Applications_Stage`: filtering applications by stage is a common pipeline query.
- `IX_ApplicationNotes_ApplicationId`: notes are always listed for one application.
- `IX_StageHistories_ApplicationId`: stage history is loaded per application.
- `IX_ApplicationScores_ApplicationId_Dimension`: score lookup and overwrite happen by application and dimension.
- `UNIQUE (job_id, candidate_email)`: prevents the same candidate from applying to the same job twice.
- `UNIQUE (application_id, dimension)`: enforces one current score per dimension.

### GET /api/applications/{id} Query

The full applicant profile needs the application record, its notes, stage history, scores, and the team members attached to those rows. In EF Core, I model this as one profile query from `Applications` using `Include` / `ThenInclude` for:

- notes and their author,
- stage history and the team member who made the change,
- scores and both the original scorer and latest updater.

Conceptually, the endpoint is one application-profile read from the API user's point of view. EF Core may execute it as one SQL query with joins or split it into multiple SQL queries depending on query-splitting settings, but it is still one API round-trip for the frontend. I chose this shape because the pipeline screen likely needs the full profile in one request rather than making separate calls for notes, scores, and history.

---

## Question 2 - Scoring Design Trade-off

### 2a. Three endpoints vs. one combined endpoint

**Why three separate endpoints can be better:**

Three separate endpoints make the intent very clear: the caller is updating exactly one score dimension. That keeps validation, testing, and UI behavior simpler because updating `culture-fit` cannot accidentally touch `interview` or `assessment`. It also matches how a hiring workflow often behaves in real life: different people may score different parts of a candidate at different times.

**When one combined endpoint would be better:**

One combined endpoint would make sense if the product UI always submitted all three scores together as one form. In that case, sending one request with all dimensions could reduce network calls and make it easier to save a complete scoring panel at once. The trade-off is that the API must then decide how to handle partial updates, missing dimensions, and accidental overwrites.

### 2b. If we needed score history

**How the schema would change:**

I would add a `score_history` table that inserts a new row every time a score is submitted:

```sql
CREATE TABLE score_history (
    id               UUID PRIMARY KEY,
    application_id   UUID        NOT NULL REFERENCES applications(id) ON DELETE CASCADE,
    dimension        VARCHAR(50) NOT NULL,
    score            INT         NOT NULL CHECK (score BETWEEN 1 AND 5),
    comment          TEXT,
    changed_by_id    UUID        NOT NULL REFERENCES team_members(id),
    changed_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_ScoreHistory_ApplicationId_Dimension
    ON score_history(application_id, dimension);
```

I would probably keep the existing `application_scores` table as the "current/latest score" table, because it makes the profile endpoint fast and simple. Then every score submission would do two things: update the current score row and insert a history row.

**Would the endpoints change?**

The existing `PUT /api/applications/{id}/scores/...` endpoints could stay the same. They would still mean "set the current score for this dimension." I would add a new read endpoint only if the product needed to show the audit trail, for example `GET /api/applications/{id}/scores/history` or `GET /api/applications/{id}/scores/{dimension}/history`.

---

## Question 3 - Debugging: Candidate Stuck in Screening

A recruiter reports: *"I moved a candidate to Interview yesterday but today they're still in Screening."*

- First, I would check the `stage_histories` table for that `application_id`. I want to know whether a row exists with `to_stage = 'Interview'` around the time the recruiter says they made the change.
- If there is no history row, the stage-change request probably did not complete successfully. I would check the API logs around that time for a validation error, missing `X-Team-Member-Id`, bad application ID, or another exception.
- If the history row exists, I would compare it with the current `stage` value on the `applications` table. If history says `Interview` but the application still says `Screening`, that points to a consistency bug around saving the stage and history together.
- I would check the browser network tab or frontend logs to confirm what response the `PATCH /api/applications/{id}/stage` request returned. A failed request might have been hidden by the UI.
- I would verify that the `X-Team-Member-Id` header was sent and belonged to a seeded team member, because protected team actions require that header.
- I would check whether the target transition itself was valid. `Screening -> Interview` is valid, but something like `Applied -> Hired` should fail with 400.
- I would look for another later stage-history row. It is possible someone moved the candidate to `Interview`, then another request moved them somewhere else or an old UI state overwrote the change.
- Finally, I would reproduce the issue with the same application and team member ID, then observe the exact API response and the resulting database rows.

---

## Question 4 - Honest Self-Assessment

| Skill           | Rating (1-5) | Note |
|-----------------|--------------|------|
| C#              | 3            | I am comfortable building services, controllers, DTOs, and async flows, and I am still growing in deeper framework patterns. |
| SQL             | 3            | I can design practical tables, indexes, constraints, and relationships, while still improving on advanced query tuning. |
| Git             | 3            | I can work with branches, commits, diffs, and normal collaboration workflows confidently. |
| REST API Design | 3            | I understand resource-based endpoints, status codes, validation, and DTOs, and I am improving at designing APIs for long-term product use. |
| Writing Tests   | 3            | I can write meaningful unit tests around business rules and services, and I want to keep improving integration-test coverage. |
