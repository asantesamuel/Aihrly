# ANSWERS.md

---

## Question 1 — Schema Design

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
CREATE INDEX IX_Applications_JobId    ON applications(job_id);
CREATE INDEX IX_Applications_Stage    ON applications(stage);

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

-- Application Scores (one row per application per dimension)
CREATE TABLE application_scores (
    id               UUID PRIMARY KEY,
    application_id   UUID        NOT NULL REFERENCES applications(id) ON DELETE CASCADE,
    dimension        VARCHAR(50) NOT NULL,   -- 'CultureFit' | 'Interview' | 'Assessment'
    score            INT         NOT NULL CHECK (score BETWEEN 1 AND 5),
    comment          TEXT,
    scored_by_id     UUID        NOT NULL REFERENCES team_members(id),
    scored_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by_id    UUID        REFERENCES team_members(id),
    updated_at       TIMESTAMPTZ,
    UNIQUE (application_id, dimension)       -- enforces one row per dimension
);
```

### Indexes and Why

- `IX_Applications_JobId` — every "list applications for a job" query filters by `job_id`
- `IX_Applications_Stage` — filtering by `?stage=screening` is a common query
- `IX_ApplicationNotes_ApplicationId` — note list queries filter by `application_id`
- `IX_StageHistories_ApplicationId` — stage history is always fetched per application
- `UNIQUE (application_id, dimension)` on scores — enforces one score per dimension (upsert target)
- `UNIQUE (job_id, candidate_email)` on applications — database-level duplicate prevention

### GET /api/applications/{id} Query

<!-- Describe what the query looks like and how many round-trips it makes -->

---

## Question 2 — Scoring Design Trade-off

### 2a. Three endpoints vs. one combined endpoint

**Why three separate endpoints is better:**
<!-- Your answer here -->

**When one combined endpoint would be better:**
<!-- Your answer here -->

### 2b. If we needed score history

**How the schema would change:**
<!-- Your answer here -->

**Would the endpoints change?**
<!-- Your answer here -->

---

## Question 3 — Debugging: Candidate Stuck in Screening

A recruiter reports: *"I moved a candidate to Interview yesterday but today they're still in Screening."*

- [ ] Check the `stage_histories` table for this `application_id` — does a row exist with `to_stage = 'Interview'` and yesterday's `changed_at`?
- [ ] If no row exists: the PATCH request never completed successfully — look at server logs for errors around that time
- [ ] If a row exists: the current `stage` column on the `applications` table should also be `Interview` — if it's not, there is a data inconsistency bug (the history was written but the application wasn't updated, or vice versa — this points to a missing transaction)
- [ ] Check browser network tab: did the PATCH /stage request return 200? Or did it return an error that the frontend swallowed silently?
- [ ] Check if the X-Team-Member-Id header was sent — a missing header returns 401, which the frontend might have ignored
- [ ] Check if the target stage sent was `"interview"` (lowercase) vs `"Interview"` — a casing mismatch could cause a 400 that appeared to succeed in the UI
- [ ] Check for a race condition: did another request immediately move the application back to Screening?
- [ ] Reproduce it: use the same team member ID and try the PATCH again — observe the exact response

---

## Question 4 — Honest Self-Assessment

| Skill           | Rating (1–5) | Note |
|-----------------|--------------|------|
| C#              |              |      |
| SQL             |              |      |
| Git             |              |      |
| REST API Design |              |      |
| Writing Tests   |              |      |
