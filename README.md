# RaceDay-Event-Management-System-PROG6212_POE
RaceDay is a full-stack web-based event management system built for PROG6212 Programming 2B. The project supports South African road running, walking and cycling events, with features for organisers and participants, including event management, categories, enrolments, results, authentication, APIs, SQL Server integration and cloud-ready deployment.
# RaceDay Event Management System

> PROG6212 - Programming 2B | Portfolio of Evidence | **Part 1 - System Planning and Database**

## Project Description

South Africa has a rich road running, walking and cycling culture, but many
events are still organised through paper forms, spreadsheets and scattered
WhatsApp groups. **RaceDay** is a full-stack, web-based event management
system built for the South African road running, walking and cycling
community. It gives Event Organisers a single place to create and manage
events, categories and results, and gives Participants a single place to
discover events, enter them, and track their own race history.

This repository holds **Part 1** of a three-part Portfolio of Evidence. Part
1 does not contain any application code - it is the planning stage of the
project: an Entity Relationship Diagram, a full API endpoint plan, and a SQL
Server database script, all of which must agree with one another before any
API or MVC code is written in Parts 2 and 3.

## User Roles

RaceDay supports two distinct user roles, kept consistent across every part
of the system:

### Organiser

An Organiser represents a race organisation or club. An Organiser can:

- Create, edit and delete their own events.
- Manage the age/distance categories offered for each event.
- View all Participant enrolments for the events they own.
- Capture finish times and finishing positions once an event concludes.
- View and update their own profile information.

### Participant

A Participant represents a runner, walker or cyclist. A Participant can:

- Create an account and log in.
- Browse all upcoming events.
- Enter an event of their choice by selecting one of its categories.
- View their own enrolments and enrolment status.
- Track their own results and performance history across past events.
- View and update their own profile information.

Role-based access will be enforced at the API level in Part 2 and reflected
consistently in the MVC interface in Part 3.

## Part 1 Deliverables

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
role, request body and expected response codes, so that the Part 2
implementation has a complete specification to build against.

### SQL Database Script

`docs/RaceDay_Database.sql` creates the RaceDay schema in SQL Server exactly
as designed in the ERD, with primary keys, foreign keys, `NOT NULL`,
`UNIQUE`, `DEFAULT` and `CHECK` constraints, and seeds it with realistic
sample data.

## Repository Structure

```text
raceday-part1/
|-- README.md
|-- docs/
|   |-- RaceDay_ERD.png                    # Section A - ERD (25 marks)
|   |-- RaceDay_API_Endpoint_Plan.md        # Section B - API plan (25 marks)
|   |-- RaceDay_Database.sql                # Section C - SQL script (20 marks)
|   `-- erd-source/
|       `-- raceday-erd.mmd                 # Mermaid source used to build the ERD
`-- .github/
    `-- workflows/
        `-- part1-ci.yml                    # CI/CD structure validation
```

## Database Setup

To open and run the RaceDay database script:

1. Install [SQL Server](https://www.microsoft.com/sql-server) (Developer or
   Express edition is sufficient) and
   [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms).
2. Open SSMS and connect to your local SQL Server instance.
3. Open `docs/RaceDay_Database.sql` (`File > Open > File...`).
4. Click **Execute** (or press `F5`) to run the entire script.
5. The script creates the `RaceDayDB` database, all six tables with their
   keys and constraints, and seeds it with 2 Organisers, 2 Participants, 3
   Events, 5 Categories, 4 Enrolments and 2 Results.
6. Expand `RaceDayDB > Tables` in Object Explorer and use **Select Top 1000
   Rows** on any table to verify the seeded data, or run the commented
   verification queries at the bottom of the script.

The script is idempotent: it can be re-run safely on the same instance, as
it drops any existing RaceDay tables before recreating them.

## CI/CD

[`.github/workflows/part1-ci.yml`](.github/workflows/part1-ci.yml) runs on
every push and pull request to `main`. It checks out the repository and
validates that the Part 1 submission is structured correctly:

- The `/docs` folder exists.
- `docs/RaceDay_ERD.png`, `docs/RaceDay_API_Endpoint_Plan.md` and
  `docs/RaceDay_Database.sql` are all present.
- `README.md` exists.
- `RaceDay_Database.sql` is not empty and defines a `CREATE TABLE` statement
  for every one of the six required entities.

**Successful green build:**

`[SCREENSHOT PLACEHOLDER - replace this line with a screenshot of the green`
`GitHub Actions run once the workflow has been pushed and passes. Actions tab`
`> Part 1 Documentation Check > latest run > green check mark.]`

## Video Demonstration

An unlisted YouTube video walking through the ERD decisions, the API
endpoint plan, and a live run of the SQL script in SSMS:





