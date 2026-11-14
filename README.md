# MarksApi

A REST API for managing students, courses, enrollments and marks, built with ASP.NET Core minimal APIs, Entity Framework Core and PostgreSQL. Authentication uses JWT bearer tokens with two roles: **Teacher** and **Student**.

**Student:** Musfira (23F-0007)

## Features

- JWT login with roles (Teacher, Student)
- Students, Courses and Enrollments CRUD with paging, search and sorting
- Marks (0 to 100) recorded per enrollment, with mark changes logged
- Course statistics (enrolled, graded, average, highest, lowest, passed)
- Student transcript with credit-hour weighted average
- A student can only read their own record and transcript (403 otherwise)
- Every error is returned as RFC 7807 ProblemDetails
- Health check, Scalar API docs (Development), CORS from configuration
- Built-in smoke test (`--smoke-test`) and a Postman collection

## Tech stack

- .NET 10 (`net10.0`), ASP.NET Core minimal APIs
- Entity Framework Core 10 with Npgsql, snake_case naming
- PostgreSQL 18
- Postman for manual and automated API tests

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 18 (with pgAdmin, or `psql`)
- Git
- The EF Core CLI tool: `dotnet tool install --global dotnet-ef`
- Postman desktop app (only for the Postman tests)

## Setup

All commands are for Windows PowerShell.

### 1. Clone the repository

```powershell
git clone https://github.com/musfiraumar/23f-0007_Musfira_Assignment1.git
cd 23f-0007_Musfira_Assignment1
```

### 2. Create the database user and the empty database

This step needs an admin account (the `postgres` user), because the application user is not allowed to create databases. In pgAdmin, connect as `postgres`, open **Query Tool** and run:

```sql
CREATE USER marks_user WITH PASSWORD 'choose-a-password';
CREATE DATABASE marks_db OWNER marks_user;
```

If `marks_user` already exists, run only the second line.

### 3. Set the secrets

Secrets are stored with .NET user-secrets, so they are never committed. See `src/MarksApi/appsettings.Example.json` for the full list of settings.

```powershell
cd src\MarksApi
dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=marks_db;Username=marks_user;Password=choose-a-password"
dotnet user-secrets set "Jwt:Key" "a-long-random-string-of-at-least-32-characters"
```

`Jwt:Key` must be at least 32 characters, otherwise the app refuses to start.

### 4. Create the tables

```powershell
dotnet ef database update
```

### 5. Run the API

```powershell
dotnet run --launch-profile http
```

The API listens on **http://localhost:5062**. In the Development environment the app seeds demo data on first start. To use HTTPS instead, run `dotnet run --launch-profile https` (https://localhost:7221).

Quick checks:

- http://localhost:5062/health returns `{"status":"Healthy"}`
- http://localhost:5062/scalar/v1 shows the interactive API documentation (Development only)

## Demo logins

| Role    | Username   | Password       | Linked student            |
|---------|------------|----------------|---------------------------|
| Teacher | `teacher1` | `Teacher@123`  | none                      |
| Student | `ali`      | `Student@123`  | Ali Raza (CS-2022-001)    |
| Student | `sara`     | `Student@123`  | Sara Khan (CS-2022-002)   |

Seeded courses: `CS401` (3 credits), `MA201` (2 credits), `DB301` (4 credits). There are 6 enrollments: Ali has marks 80, 60 and none; Sara has 70, 45 and 90.

These accounts are demo data for local development only.

## Using the API

Log in to get a token, then send it as `Authorization: Bearer <token>`.

```powershell
curl.exe -X POST http://localhost:5062/api/auth/login -H "Content-Type: application/json" -d "{\"username\":\"teacher1\",\"password\":\"Teacher@123\"}"
```

### Endpoints

| Method | Path | Access | Description |
|--------|------|--------|-------------|
| POST | `/api/auth/login` | anyone | Returns a JWT, its expiry and the role |
| GET | `/health` | anyone | Database health check (503 if the database is down) |
| GET | `/api/students` | Teacher | List students (paging, search, sort) |
| GET | `/api/students/{id}` | Teacher, or the student themselves | Get one student |
| POST | `/api/students` | Teacher | Create a student (201 with `Location`) |
| PUT | `/api/students/{id}` | Teacher | Update a student |
| DELETE | `/api/students/{id}` | Teacher | Delete a student (409 if enrolled) |
| GET | `/api/courses` | any logged-in user | List courses (paging, search, sort) |
| GET | `/api/courses/{id}` | any logged-in user | Get one course |
| POST | `/api/courses` | Teacher | Create a course |
| PUT | `/api/courses/{id}` | Teacher | Update a course |
| DELETE | `/api/courses/{id}` | Teacher | Delete a course (409 if enrolled) |
| POST | `/api/courses/{courseId}/enrollments` | Teacher | Enroll a student (404 unknown, 409 duplicate) |
| GET | `/api/courses/{courseId}/enrollments` | Teacher | List the enrollments of a course |
| GET | `/api/enrollments/{id}` | Teacher | Get one enrollment |
| PUT | `/api/enrollments/{id}/mark` | Teacher | Set a mark from 0 to 100 |
| DELETE | `/api/enrollments/{id}` | Teacher | Remove an enrollment (204) |
| GET | `/api/courses/{courseId}/stats` | Teacher | Course statistics |
| GET | `/api/students/{id}/transcript` | Teacher, or the student themselves | Transcript with weighted average |

List endpoints accept `page`, `pageSize` (maximum 50), `search` and `sort` (prefix a field with `-` for descending).

### Error format

Every error is ProblemDetails JSON. Typical status codes:

| Code | Meaning |
|------|---------|
| 400 | Validation failed (the body lists the invalid fields) |
| 401 | Missing, invalid or expired token |
| 403 | Logged in, but not allowed (for example a student reading another student) |
| 404 | The resource does not exist |
| 409 | Conflict (duplicate value, or a delete blocked by related enrollments) |
| 503 | The database is not reachable (`/health`) |

## Configuration

| Setting | Where it comes from | Notes |
|---------|--------------------|-------|
| `ConnectionStrings:Database` | user-secrets | PostgreSQL connection string |
| `Jwt:Key` | user-secrets | At least 32 characters |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | `appsettings.json` | Token settings |
| `Marking:PassMark` | `appsettings.json` | Pass mark used by stats and transcript (default 50) |
| `Cors:AllowedOrigins` | `appsettings.json` | List of allowed browser origins |
| `SmokeTest:BaseUrl` | `appsettings.json` | API address the smoke test calls |
| `SmokeTest:Username`, `SmokeTest:Password` | user-secrets | Teacher login for the smoke test |

On another machine, the same settings can be provided as environment variables, with `__` instead of `:` (for example `Jwt__Key`).

## Smoke test

The smoke test is a separate module in `src/MarksApi/SmokeTest`. It calls the **running** API over HTTP, creates temporary data prefixed with `SMOKE-`, checks the results and deletes everything it created. It prints a PASS or FAIL line per check.

Exit codes: `0` all checks passed, `1` at least one check failed, `2` the test could not run (missing credentials or the API is not reachable).

1. Set the teacher credentials once:

```powershell
cd src\MarksApi
dotnet user-secrets set "SmokeTest:Username" "teacher1"
dotnet user-secrets set "SmokeTest:Password" "Teacher@123"
```

2. Build, then start the API in one terminal:

```powershell
dotnet build
dotnet run --no-build
```

3. In a second terminal, run the smoke test:

```powershell
cd src\MarksApi
dotnet run --no-build --no-launch-profile -- --smoke-test
echo $LASTEXITCODE
```

`--no-build` is needed because the running API locks the build files.

## Postman tests

The Postman collection **Marks Api** is stored in the `postman` folder in Postman's native file format.

1. In Postman, import the `postman` folder (**File > Import > Folders**), or open the repository folder as a local workspace.
2. Create an environment named `local` with these variables: `baseUrl` = `http://localhost:5062`, and empty `token` and `studentToken` (the login requests fill them).
3. Start the API on a **fresh database** (`dotnet ef database update 0`, then `dotnet ef database update`, then `dotnet run`).
4. Run **auth > login-success** and **auth > login-ali** once, so both tokens are set.
5. Run the whole collection with the Collection Runner. The folders are: Auth, Students, Courses, Enrollments and Marks, Reports, Health, Negative Tests and Cleanup.

Authorization is set at collection level to Bearer `{{token}}`. Requests that need a student token, or no token, set their own authorization. The collection contains no tokens, because they are created at run time.

## Database

The schema is in `db/schema.sql` (a `pg_dump --schema-only` export). The tables are:

- `users`: login accounts, with a role and an optional link to a student
- `students`: registration number (unique), name, email (unique)
- `courses`: code (unique), title, credit hours (1 to 4)
- `enrollments`: links a student and a course (unique pair) with an optional mark (0 to 100)

Foreign keys use `ON DELETE RESTRICT`, so a student or course with enrollments cannot be deleted. All timestamps are stored in UTC.

To rebuild the tables without dropping the database:

```powershell
cd src\MarksApi
dotnet ef database update 0
dotnet ef database update
```

## Project structure

```
src/MarksApi
  Data/             DbContext, entities, seeder
  Dtos/             request and response records (entities are never returned)
  Endpoints/        thin minimal-API endpoints (no queries or business logic)
  Services/         business logic behind interfaces (IStudentService, ...)
  Infrastructure/   exception handler, claims and result extensions
  Settings/         strongly typed options (Jwt, Marking)
  SmokeTest/        the --smoke-test module
  Migrations/       EF Core migrations
db/schema.sql       schema dump
postman/            Postman collection
```

## Design notes

- **Services behind interfaces, thin endpoints:** endpoints only map HTTP to a service call and the result back to HTTP.
- **ServiceResult:** services return Ok, NotFound, Conflict or Forbidden, and one extension turns that into the right ProblemDetails response.
- **No check-then-insert:** duplicates are caught from the database's unique indexes (`DbUpdateException`), which avoids race conditions.
- **PostgreSQL error codes:** deleting a row blocked by `ON DELETE RESTRICT` raises state `23001` (restrict violation), not `23503`, so both are treated as a foreign key conflict.
- **Reads use `AsNoTracking` and `Select` projections**, and every async method takes a `CancellationToken`.
- **Ownership check first:** a student asking for another student's data gets 403 before any lookup, so ids cannot be probed.

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `permission denied to create database` | Create `marks_db` as the `postgres` user first (setup step 2) |
| App stops with a `Jwt:Key` error | Set `Jwt:Key` in user-secrets with at least 32 characters |
| Smoke test login returns 401 | Check `dotnet user-secrets list`; the credentials must be an existing seeded user |
| Smoke test exit code 2 | The API is not running, or the SmokeTest credentials are missing |
| `pg_dump` is not recognised | Run it by full path, for example `C:\Program Files\PostgreSQL\18\bin\pg_dump.exe` |
| Postman 401 on a protected request | Run `login-success` or `login-ali` again; tokens expire after 60 minutes |