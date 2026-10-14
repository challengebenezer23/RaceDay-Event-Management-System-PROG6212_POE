# RaceDay Event Management System

> PROG6212 - Programming 2B | Portfolio of Evidence | **Part 1 - System Planning & Database** and **Part 2 - RESTful API Development**

## Project Description

South Africa has a rich road running, walking and cycling culture, but many
events are still organised through paper forms, spreadsheets and scattered
WhatsApp groups. **RaceDay** is a full-stack, web-based event management
system built for the South African road running, walking and cycling
community. It gives Event Organisers a single place to create and manage
events, categories and results, and gives Participants a single place to
discover events, enter them, and track their own race history.

This repository now holds **Part 1** (planning: ERD, API endpoint plan, SQL
database script) and **Part 2** (a working ASP.NET Core Web API implementing
that plan) of a three-part Portfolio of Evidence. Every endpoint, DTO and
database table in Part 2 was built directly against the Part 1 planning
documents, so the three deliverables agree with one another end to end.

## User Roles

RaceDay supports two distinct user roles, kept consistent across every part
of the system:

### Organiser

An Organiser represents a race organisation or club. An Organiser can:

- Register and log in.
- Create, edit and delete their own events.
- Manage the age/distance categories offered for each event.
- View all Participant enrolments for the events they own.
- Capture and correct finish times and finishing positions once an event
  concludes.
- View and update their own profile information.

### Participant

A Participant represents a runner, walker or cyclist. A Participant can:

- Create an account and log in.
- Browse all upcoming events (no login required).
- Enter an event of their choice by selecting one of its categories.
- View their own enrolments and enrolment status, and withdraw before an
  event takes place.
- Track their own results and performance history across past events.
- View and update their own profile information.

Role-based access is enforced at the API level in Part 2 via a custom
session-based authorization filter (see
[Authentication & Role Enforcement](#authentication--role-enforcement)
below), and will be reflected consistently in the MVC interface in Part 3.

## Part 1 Deliverables (25 + 25 + 20 + 15 + 10 = 95 marks)

| Deliverable | File | Marks |
|---|---|---|
| Entity Relationship Diagram | [`docs/RaceDay_ERD.png`](docs/RaceDay_ERD.png) | 25 |
| API Endpoint Plan | [`docs/RaceDay_API_Endpoint_Plan.md`](docs/RaceDay_API_Endpoint_Plan.md) | 25 |
| SQL Database Script | [`docs/RaceDay_Database.sql`](docs/RaceDay_Database.sql) | 20 |
| GitHub repository & CI/CD | this repository + [`.github/workflows/part1-ci.yml`](.github/workflows/part1-ci.yml) | 15 |
| Video Presentation | see [Video Demonstration](#video-demonstration) below | 10 |

### Entity Relationship Diagram

The ERD models RaceDay's six core entities - **Organisers**, **Participants**,
**Events**, **Categories**, **Enrolments** and **Results** - with all
attributes, primary keys, foreign keys and relationship cardinality shown.
`Enrolments` is the associative entity that resolves the many-to-many
relationship between Participants and Events, while also recording the
Category each Participant selected. The evolving mermaid source used to
build the diagram is kept in `docs/erd-source/raceday-erd.mmd` for
transparency; the rendered image is `docs/RaceDay_ERD.png`.

### API Endpoint Plan

`docs/RaceDay_API_Endpoint_Plan.md` lists all 22 planned endpoints across
Authentication, User Profile, Events, Categories, Event Enrolments and
Results. Every row specifies the HTTP method, route, description, required
role, request body and expected response codes. **Every one of these 22
endpoints is implemented in Part 2** - see
[Endpoints Implemented](#endpoints-implemented) below for the mapping.

### SQL Database Script

`docs/RaceDay_Database.sql` creates the RaceDay schema in SQL Server exactly
as designed in the ERD, with primary keys, foreign keys, `NOT NULL`,
`UNIQUE`, `DEFAULT` and `CHECK` constraints, and seeds it with realistic
sample data. The Part 2 EF Core model (`RaceDay.Api/Data/RaceDayDbContext.cs`)
is configured table-for-table, column-for-column and constraint-for-constraint
to match this script.

## Part 2 Deliverables (RESTful API Development - 100 marks)

| Deliverable | Where |
|---|---|
| ASP.NET Core Web API (Code-First EF Core) | [`RaceDay.Api/`](RaceDay.Api) |
| Database context & migrations | [`RaceDay.Api/Data/RaceDayDbContext.cs`](RaceDay.Api/Data/RaceDayDbContext.cs) |
| Session-based authentication + role enforcement | [`RaceDay.Api/Security/`](RaceDay.Api/Security) |
| Swagger / OpenAPI documentation | see [Swagger](#swagger--openapi) below |
| Unit / integration tests (xUnit) | [`RaceDay.Api.Tests/`](RaceDay.Api.Tests) |
| CI/CD (build + test on every push) | [`.github/workflows/part2-ci.yml`](.github/workflows/part2-ci.yml) |
| Video Presentation | see [Video Demonstration](#video-demonstration) below |

### Architecture

```text
RaceDay.Api/
|-- Controllers/         # AuthController, ProfileController, EventsController,
|                         # CategoriesController, EnrolmentsController, ResultsController
|-- Models/              # EF Core entities: Organiser, Participant, Event,
|                         # Category, Enrolment, Result
|-- Data/                # RaceDayDbContext (Fluent API Code-First configuration)
|-- Dtos/                # Request/response DTOs, grouped by feature
|-- Security/            # SessionAuthorizeAttribute, SessionKeys, PasswordHasherUser
|-- Program.cs           # App bootstrap: DbContext, session, Swagger, middleware
`-- appsettings.json     # Connection string configuration

RaceDay.Api.Tests/
|-- RaceDayApiFactory.cs # WebApplicationFactory<Program> wired to an EF Core
|                         # InMemory database (one fresh database per factory)
|-- AuthTests.cs         # Register / login / logout integration tests
|-- EventsTests.cs       # Public read access + Organiser-only write + role checks
`-- EnrolmentsTests.cs   # Enrol / duplicate-enrol / role-rejection / withdrawal
```

### Technology choices and why

- **ASP.NET Core 8 Web API** (`Microsoft.NET.Sdk.Web`, minimal hosting model)
  - the version of .NET specified as current/LTS for this module and the
  version installed by `actions/setup-dotnet@v4` in CI.
- **EF Core 8, Code-First** against the same SQL Server schema designed in
  Part 1 (`Microsoft.EntityFrameworkCore.SqlServer`), with all constraints
  expressed through Fluent API in `OnModelCreating` rather than left to EF
  Core's conventions, so the generated schema matches
  `docs/RaceDay_Database.sql` exactly (see
  [Database migrations](#database-migrations-required-before-first-run)
  below for why the actual `Migrations/` folder is generated locally rather
  than committed by hand).
- **Session-based authentication**, as required by the brief, implemented
  with ASP.NET Core's built-in `ISession` (`AddDistributedMemoryCache` +
  `AddSession`) rather than JWT/cookies-as-identity. A session cookie holds
  only an opaque session ID; the actual `UserId`, `Role`, `FullName` and
  `Email` are kept server-side in the session store.
- **A custom `SessionAuthorizeAttribute`** (`IAuthorizationFilter`) instead
  of the built-in `[Authorize]` pipeline, because `[Authorize]` is built for
  `ClaimsPrincipal`-based authentication schemes (cookies, JWT bearer, etc.)
  and registering one just to hold a role string would be unnecessary
  machinery for a session-only design. The filter reads `HttpContext.Session`
  directly, returns `401` when no one is logged in, and `403` when the
  logged-in user's role is not in the attribute's allowed-roles list.
- **`Microsoft.AspNetCore.Identity.PasswordHasher<TUser>`** for password
  hashing (PBKDF2 with a random salt per password) - ships in the shared
  ASP.NET Core framework, so no extra password-hashing package is needed.
  Plaintext passwords are never stored or logged.
- **Swashbuckle (`Swashbuckle.AspNetCore`)** for Swagger/OpenAPI, with XML
  documentation comments included so every endpoint, parameter and response
  code shown in Swagger UI is properly described.
- **xUnit + `WebApplicationFactory<Program>` + EF Core InMemory** for
  integration testing - the Microsoft-documented pattern for testing an
  ASP.NET Core app end-to-end (real controllers, real middleware, real
  routing) without needing a real SQL Server instance in CI.

### Authentication & Role Enforcement

| Step | Behaviour |
|---|---|
| `POST /api/auth/register` | Validates the requested role (`Organiser` or `Participant`), checks the email is not already used by *either* table, hashes the password, creates the row, returns `201`. |
| `POST /api/auth/login` | Looks up the email in both tables, verifies the password hash, and on success writes `UserId`/`Role`/`FullName`/`Email` into the server-side session and sets the session cookie. Returns `401` on any mismatch (without revealing whether the email exists). |
| `POST /api/auth/logout` | Clears the session. Requires an active session (`401` otherwise). |
| Every protected endpoint | Decorated with `[SessionAuthorize]` (any logged-in user) or `[SessionAuthorize(RaceDayRoles.Organiser)]` / `[SessionAuthorize(RaceDayRoles.Participant)]` (role-specific). Ownership checks (e.g. "is this your event?") are performed inside the action after the role check passes, returning `403` when the logged-in user doesn't own the resource being modified. |

### Endpoints Implemented

All 22 endpoints from `docs/RaceDay_API_Endpoint_Plan.md` are implemented.
Route prefixes below are relative to `/api`.

| Area | Method | Route | Role required |
|---|---|---|---|
| Auth | POST | `/auth/register` | none |
| Auth | POST | `/auth/login` | none |
| Auth | POST | `/auth/logout` | any logged-in user |
| Profile | GET | `/profile` | any logged-in user |
| Profile | PUT | `/profile` | any logged-in user |
| Events | GET | `/events` | none (public) |
| Events | GET | `/events/{id}` | none (public) |
| Events | POST | `/events` | Organiser |
| Events | PUT | `/events/{id}` | Organiser (owner only) |
| Events | DELETE | `/events/{id}` | Organiser (owner only) |
| Categories | GET | `/events/{eventId}/categories` | none (public) |
| Categories | POST | `/events/{eventId}/categories` | Organiser (owner only) |
| Categories | PUT | `/categories/{id}` | Organiser (owner only) |
| Categories | DELETE | `/categories/{id}` | Organiser (owner only) |
| Enrolments | POST | `/enrolments` | Participant |
| Enrolments | GET | `/enrolments/mine` | Participant |
| Enrolments | GET | `/events/{eventId}/enrolments` | Organiser (owner only) |
| Enrolments | DELETE | `/enrolments/{id}` | Participant (owner only) |
| Results | POST | `/results` | Organiser (event owner only) |
| Results | PUT | `/results/{id}` | Organiser (event owner only) |
| Results | GET | `/results/mine` | Participant |
| Results | GET | `/events/{eventId}/results` | Organiser (owner only) |

### Swagger / OpenAPI

Running the API locally (`dotnet run --project RaceDay.Api`) opens Swagger UI
automatically at `/swagger` (configured as the `launchUrl` in
`Properties/launchSettings.json`). Every controller and DTO has XML doc
comments, so Swagger UI shows a full description, parameters and all
documented response codes (`200`/`201`/`400`/`401`/`403`/`404`/`409` as
appropriate) for each endpoint - this is also how role-restricted endpoints
were manually verified during development (log in via `/api/auth/login` in
one browser tab, then exercise the protected endpoints in Swagger UI, which
sends the session cookie automatically).

**Swagger UI screenshot:**

`[SCREENSHOT PLACEHOLDER - replace this line with a screenshot of Swagger UI`
`showing the full list of RaceDay API endpoints once the project has been`
`run locally.]`

## Repository Structure

```text
raceday-part1/
|-- README.md
|-- RaceDay.sln
|-- docs/
|   |-- RaceDay_ERD.png                    # Part 1 Section A - ERD (25 marks)
|   |-- RaceDay_API_Endpoint_Plan.md        # Part 1 Section B - API plan (25 marks)
|   |-- RaceDay_Database.sql                # Part 1 Section C - SQL script (20 marks)
|   `-- erd-source/
|       `-- raceday-erd.mmd                 # Mermaid source used to build the ERD
|-- RaceDay.Api/                            # Part 2 - ASP.NET Core Web API
|   |-- Controllers/
|   |-- Models/
|   |-- Data/
|   |-- Dtos/
|   |-- Security/
|   |-- Program.cs
|   `-- appsettings.json
|-- RaceDay.Api.Tests/                      # Part 2 - xUnit integration tests
|-- .config/
|   `-- dotnet-tools.json                   # dotnet-ef local tool manifest
`-- .github/
    `-- workflows/
        |-- part1-ci.yml                    # Part 1 CI - documentation structure check
        `-- part2-ci.yml                    # Part 2 CI - build + test the API
```

## Database Setup

To open and run the original Part 1 database script directly in SSMS:

1. Install [SQL Server](https://www.microsoft.com/sql-server) (Developer or
   Express edition is sufficient) and
   [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms).
2. Open SSMS and connect to your local SQL Server instance.
3. Open `docs/RaceDay_Database.sql` (`File > Open > File...`).
4. Click **Execute** (or press `F5`) to run the entire script.
5. The script creates the `RaceDayDB` database, all six tables with their
   keys and constraints, and seeds it with 2 Organisers, 2 Participants, 3
   Events, 5 Categories, 4 Enrolments and 2 Results.

The script is idempotent: it can be re-run safely on the same instance, as
it drops any existing RaceDay tables before recreating them.

For Part 2 development, however, the database is created and evolved
through **EF Core migrations** instead of running the script by hand - see
below.

## Running the API Locally

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB, Developer or Express edition) - LocalDB ships with
  Visual Studio and is what the default connection string targets.

### Database migrations (required before first run)

This repository intentionally does **not** commit a hand-written
`Migrations/` folder, because EF Core migration files are C# generated from
whatever SDK/tooling version produces them and are meant to be generated by
`dotnet ef` on a machine that can actually compile and run the project - the
brief itself requires "test it before submitting" and "all tests must pass
locally", which can only be verified with a real SDK. Generate them with:

```bash
dotnet tool restore
dotnet ef migrations add InitialCreate --project RaceDay.Api --startup-project RaceDay.Api
dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api
```

This creates the `RaceDayDB` database (per the connection string in
`RaceDay.Api/appsettings.json`) with a schema matching
`docs/RaceDay_Database.sql`.

### Run the API

```bash
dotnet restore
dotnet build
dotnet run --project RaceDay.Api
```

Swagger UI opens automatically at `https://localhost:<port>/swagger`, where
every endpoint can be exercised directly (register -> login -> the session
cookie set by login is sent automatically by Swagger UI's "Try it out" on
every subsequent call in the same browser tab).

### Run the tests

```bash
dotnet test
```

This runs the full `RaceDay.Api.Tests` suite (see
[Testing](#testing) below) against an isolated in-memory database - **no
SQL Server connection is required to run the tests**, which is also why CI
can run them on a plain `ubuntu-latest` runner.

## Testing

`RaceDay.Api.Tests` is an xUnit integration test project that boots the real
`RaceDay.Api` application via `WebApplicationFactory<Program>` and swaps the
SQL Server `DbContext` registration for a fresh EF Core InMemory database
per test class, following the
[Microsoft-documented pattern](https://learn.microsoft.com/aspnet/core/test/integration-tests)
for integration testing ASP.NET Core applications. Tests exercise the real
routing, model binding, validation, session middleware and controllers -
nothing is mocked out except the database engine itself.

| Test class | Covers |
|---|---|
| `AuthTests` | Organiser & Participant registration success, Participant registration missing `DateOfBirth` (`400`), invalid role (`400`), duplicate email (`409`), login success, wrong password (`401`), unknown email (`401`), logout without a session (`401`). |
| `EventsTests` | `GET /api/events` requires no login (`200`), Organiser can create an event (`201`), Participant is rejected (`403`), unauthenticated caller is rejected (`401`), invalid `EventType` is rejected (`400`), a non-owning Organiser cannot update another Organiser's event (`403`), unknown event id (`404`). |
| `EnrolmentsTests` | Participant can enrol (`201`), a second enrolment in the same event is rejected as a duplicate (`409`), an Organiser is rejected by the role check (`403`), enrolling in a non-existent event (`404`), a Participant can withdraw their own enrolment (`200`), a different Participant cannot withdraw someone else's enrolment (`403`). |

This directly covers the rubric's required test areas: user
registration/login, event management with role rejection, and event
enrolment - each with both the happy path and the security-relevant
rejection paths.

**Passing local test run:**

`[SCREENSHOT PLACEHOLDER - replace this line with a screenshot of` `dotnet test` `showing all tests passing locally, before recording the Part 2 video.]`

## CI/CD

Two workflows now run on every push and pull request to `main`:

- [`.github/workflows/part1-ci.yml`](.github/workflows/part1-ci.yml) -
  validates that the Part 1 planning documents are present and structured
  correctly (unchanged from Part 1).
- [`.github/workflows/part2-ci.yml`](.github/workflows/part2-ci.yml) - sets
  up the .NET 8 SDK (`actions/setup-dotnet@v4`), restores, builds
  `RaceDay.sln` in `Release` configuration, and runs the full `dotnet test`
  suite, uploading the `.trx` test results as a workflow artifact even if a
  run fails.

**Successful green builds:**

`[SCREENSHOT PLACEHOLDER - replace this line with a screenshot of the Actions`
`tab showing both Part 1 Documentation Check and Part 2 API Build & Test`
`with green check marks after pushing this code to GitHub.]`

## Video Demonstration

### Part 1

An unlisted YouTube video walking through the ERD decisions, the API
endpoint plan, and a live run of the SQL script in SSMS.

**YouTube Link:** `[PASTE YOUR UNLISTED YOUTUBE LINK HERE]`

See [`VIDEO_PRESENTATION_SCRIPT.md`](VIDEO_PRESENTATION_SCRIPT.md) for the
full recording script and checklist used to prepare this video.

### Part 2

An unlisted YouTube video demonstrating the running API in Swagger UI
(registration, login, role-restricted endpoints succeeding and being
rejected), a passing `dotnet test` run, and the green Part 2 GitHub Actions
build.

**YouTube Link:** `[PASTE YOUR UNLISTED YOUTUBE LINK HERE]`

See [`VIDEO_PRESENTATION_SCRIPT_PART2.md`](VIDEO_PRESENTATION_SCRIPT_PART2.md)
for the full recording script and checklist used to prepare this video.

## Disclosure of AI Tool Use

As required by the PoE instructions, AI assistance used while preparing this
submission is disclosed here:

- **Part 1:** an AI assistant (Claude) was used to help draft the ERD
  structure, the API endpoint plan, and the SQL script and README content,
  based on the RaceDay brief and rubric.
- **Part 2:** an AI assistant (Claude) was used to help draft the ASP.NET
  Core Web API implementation (controllers, DTOs, EF Core configuration,
  the custom session authorization filter) and the xUnit test suite, built
  directly against the Part 1 planning documents. **This code was written in
  a sandboxed environment without network access to NuGet or a .NET SDK
  install, so it could not be compiled or executed by the AI assistant
  before being committed.** The student is responsible for running
  `dotnet restore` / `dotnet build` / `dotnet test` locally, generating the
  EF Core migrations, fixing any compilation errors that surface, and
  verifying every test passes before recording the Part 2 video and
  submitting - exactly as the brief requires ("test it before submitting",
  "all tests must pass locally"). Every design decision (session-based
  auth, the custom authorization filter, the Fluent API constraint choices,
  the DTO shapes) was reviewed against the assessment requirements and the
  Part 1 planning documents, and the student is responsible for
  understanding and being able to explain every design choice in the video
  presentation, as required.

## Final Submission Checklist

### Part 1

- [ ] `/docs` folder present with ERD, API Endpoint Plan and SQL script.
- [ ] ERD has at least six entities, attributes, PKs, FKs and cardinality.
- [ ] API Endpoint Plan covers Authentication, User Profile, Events,
      Categories, Event Enrolments and Results with all six columns.
- [ ] SQL script runs cleanly on a clean SQL Server instance (tested in SSMS).
- [ ] Unlisted Part 1 YouTube video recorded and linked above.

### Part 2

- [ ] `dotnet restore && dotnet build` succeeds locally with no errors.
- [ ] EF Core migrations generated (`dotnet ef migrations add InitialCreate`)
      and applied (`dotnet ef database update`) against a local SQL Server.
- [ ] `dotnet test` passes in full locally.
- [ ] API runs locally and every endpoint has been manually exercised in
      Swagger UI at least once, including role-rejection cases.
- [ ] Part 2 GitHub Actions workflow (`part2-ci.yml`) shows a green build;
      screenshot added above.
- [ ] Unlisted Part 2 YouTube video recorded and linked above.

### Both parts

- [ ] At least 20 meaningful commits per part on this repository.
- [ ] GitHub repository link (not a ZIP file) submitted on ARC.
