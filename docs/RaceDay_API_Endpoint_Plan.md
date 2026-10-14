# RaceDay API Endpoint Plan

**PROG6212 - Programming 2B | PoE Part 1 - Section B**

This document plans every REST endpoint the RaceDay API will expose in Part 2.
No API code exists yet — this is the specification the Part 2 implementation
must be built against. Endpoints are grouped by resource, and every row
follows the same six-column format required by the assessment brief.

## How to read this table

| Column | Meaning |
|---|---|
| **HTTP Method** | GET (read), POST (create), PUT (update), DELETE (remove). |
| **Route** | The URL path. Always starts with `/api/`. |
| **Description** | One sentence explaining what the endpoint does and why it exists. |
| **Role Required** | `None` = public, `Any` = any authenticated user, `Organiser` = Organiser only, `Participant` = Participant only. |
| **Request Body** | The JSON fields the client must send. `None` for most GET/DELETE requests. |
| **Expected Response** | The HTTP status code(s) returned, covering both success and realistic failure. |

---

## 1. Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Registers a new user as either an Organiser or a Participant. | None | `{ fullName, email, password, phoneNumber, role, organisationName?, dateOfBirth?, emergencyContactName?, emergencyContactPhone? }` | 201 Created; 400 Bad Request (missing/invalid fields); 409 Conflict (email already registered) |
| POST | `/api/auth/login` | Authenticates a registered user and starts a session, returning their role. | None | `{ email, password }` | 200 OK (session established, role returned); 400 Bad Request; 401 Unauthorized (invalid credentials) |
| POST | `/api/auth/logout` | Ends the current authenticated user's session. | Any | None | 200 OK; 401 Unauthorized |

---

## 2. User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/profile` | Returns the profile information of the currently authenticated user (Organiser or Participant). | Any | None | 200 OK; 401 Unauthorized |
| PUT | `/api/profile` | Updates the profile information of the currently authenticated user. | Any | `{ fullName, phoneNumber, organisationName? (Organiser), emergencyContactName?, emergencyContactPhone? (Participant) }` | 200 OK; 400 Bad Request; 401 Unauthorized |

---

## 3. Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | Returns a list of all published RaceDay events. | None | None | 200 OK |
| GET | `/api/events/{id}` | Returns the details of a specific RaceDay event using the supplied event ID. | None | None | 200 OK; 404 Not Found |
| POST | `/api/events` | Creates a new RaceDay event owned by the authenticated Organiser. | Organiser | `{ eventName, description, eventDate, location, distance, eventType }` | 201 Created; 400 Bad Request; 401 Unauthorized |
| PUT | `/api/events/{id}` | Updates an event owned by the authenticated Organiser. | Organiser | `{ eventName, description, eventDate, location, distance, eventType }` | 200 OK; 400 Bad Request; 401 Unauthorized; 403 Forbidden (not the owning Organiser); 404 Not Found |
| DELETE | `/api/events/{id}` | Deletes an event owned by the authenticated Organiser. | Organiser | None | 200 OK; 401 Unauthorized; 403 Forbidden; 404 Not Found |

---

## 4. Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Returns all age/distance categories available for a specific event. | None | None | 200 OK; 404 Not Found |
| POST | `/api/events/{eventId}/categories` | Creates a new category for an event owned by the authenticated Organiser. | Organiser | `{ categoryName, minAge, maxAge, distanceKM }` | 201 Created; 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found |
| PUT | `/api/categories/{id}` | Updates a category belonging to an event owned by the authenticated Organiser. | Organiser | `{ categoryName, minAge, maxAge, distanceKM }` | 200 OK; 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found |
| DELETE | `/api/categories/{id}` | Deletes a category, provided no Participant has already enrolled in it. | Organiser | None | 200 OK; 401 Unauthorized; 403 Forbidden; 404 Not Found; 409 Conflict (category has active enrolments) |

---

## 5. Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/enrolments` | Enrols the authenticated Participant into an event under a chosen category. | Participant | `{ eventId, categoryId }` | 201 Created; 400 Bad Request; 401 Unauthorized; 404 Not Found (event or category does not exist); 409 Conflict (already enrolled) |
| GET | `/api/enrolments/mine` | Returns every enrolment belonging to the authenticated Participant. | Participant | None | 200 OK; 401 Unauthorized |
| GET | `/api/events/{eventId}/enrolments` | Returns every Participant enrolment for an event owned by the authenticated Organiser. | Organiser | None | 200 OK; 401 Unauthorized; 403 Forbidden; 404 Not Found |
| DELETE | `/api/enrolments/{id}` | Withdraws the authenticated Participant's own enrolment before the event date. | Participant | None | 200 OK; 401 Unauthorized; 403 Forbidden (not the owning Participant); 404 Not Found |

---

## 6. Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/results` | Captures the finish time and finishing position for a Participant's enrolment, recorded by the owning Organiser. | Organiser | `{ enrolmentId, finishTimeSeconds, finishPosition }` | 201 Created; 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found; 409 Conflict (result already captured for this enrolment) |
| PUT | `/api/results/{id}` | Corrects a previously captured result for an event owned by the authenticated Organiser. | Organiser | `{ finishTimeSeconds, finishPosition }` | 200 OK; 400 Bad Request; 401 Unauthorized; 403 Forbidden; 404 Not Found |
| GET | `/api/results/mine` | Returns the authenticated Participant's own race results and performance history across all events. | Participant | None | 200 OK; 401 Unauthorized |
| GET | `/api/events/{eventId}/results` | Returns every captured result for an event owned by the authenticated Organiser. | Organiser | None | 200 OK; 401 Unauthorized; 403 Forbidden; 404 Not Found |

---

## Design notes

- **22 endpoints in total**, covering all six required resource groups: Authentication, User Profile, Events, Categories, Event Enrolments and Results.
- **HTTP methods match intent.** GET never changes data; POST always creates; PUT always updates an existing resource; DELETE always removes one. No endpoint uses POST for a read or update action.
- **Role enforcement is symmetric.** Every write endpoint that belongs to one role (Organiser managing events/categories/results, Participant managing enrolments) returns `401 Unauthorized` when no one is logged in and `403 Forbidden` when the wrong role — or the wrong owner of that role — attempts it. This plan is what Part 2's role-based access control must implement.
- **Failure responses are realistic, not exhaustive.** Each row lists only the status codes that can actually occur for that action (for example, `409 Conflict` only appears where a genuine uniqueness or state clash is possible, such as a duplicate enrolment or a duplicate result capture).
- **Consistency with the ERD.** Every route and request-body field name matches an entity or attribute in `RaceDay_ERD.png` and `RaceDay_Database.sql` — `eventId`, `categoryId`, `enrolmentId` and so on are the same identifiers used as primary/foreign keys in the database script.
