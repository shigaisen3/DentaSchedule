# DentaSchedule

[![CI](https://github.com/shigaisen3/DentaSchedule/actions/workflows/ci.yml/badge.svg)](https://github.com/shigaisen3/DentaSchedule/actions/workflows/ci.yml)

A full-stack dental clinic management and appointment-booking system, built as a
university diploma project (licență). Patients book appointments online without an
account; clinic staff manage clinics, doctors, schedules, and appointments through an
authenticated admin panel.

- **Backend:** ASP.NET Core 8 Web API, EF Core 8, SQL Server, ASP.NET Identity + JWT
- **Frontend:** React 18 + TypeScript + Vite + Tailwind CSS
- **Architecture:** Clean three-layer separation (Presentation / Business / Data Access)

---

## Table of contents

- [Features](#features)
- [Tech stack](#tech-stack)
- [Architecture](#architecture)
- [Project structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Getting started](#getting-started)
- [Test accounts](#test-accounts)
- [Configuration](#configuration)
- [API overview](#api-overview)
- [The scheduling engine](#the-scheduling-engine)
- [Testing](#testing)
- [Roadmap / future work](#roadmap--future-work)

---

## Features

**Public (no authentication)**
- Multi-step appointment booking wizard: clinic → doctor → time slot → patient details → confirmation
- Real-time available-slot lookup based on each doctor's working hours
- Appointment lookup by reference number (e.g. `DS-12345678`)
- Email notifications on booking, approval, cancellation, and reschedule

**Admin (full access)**
- CRUD for clinics, doctors, and users
- Per-doctor weekly schedules and schedule exceptions (days off / custom hours)
- Role management, account lockout/unlock, password reset
- Dashboard with appointment statistics

**Assistant (scoped to one clinic)**
- View and manage appointments for the assigned clinic only
- Approve, cancel, and reschedule appointments

## Tech stack

| Layer      | Technologies |
|------------|--------------|
| Backend    | .NET 8, ASP.NET Core Web API, EF Core 8, ASP.NET Core Identity, JWT, FluentValidation, Serilog, Swagger/OpenAPI, MailKit |
| Database   | SQL Server (LocalDB for development) |
| Frontend   | React 18, TypeScript, Vite, Tailwind CSS, React Router, TanStack Query, React Hook Form, Zod, Axios |
| Testing    | xUnit, EF Core InMemory provider |

## Architecture

The solution follows a three-layer architecture, each layer a separate C# project:

```
┌─────────────────────────────────────────────────────────┐
│  DentaSchedule.API   (Presentation)                      │
│  Controllers · DTOs · Validators · Middleware · Auth     │
└───────────────────────────┬─────────────────────────────┘
                            │  depends on
┌───────────────────────────▼─────────────────────────────┐
│  DentaSchedule.BLL   (Business Logic)                    │
│  Services · ServiceResponse<T> · scheduling engine ·     │
│  notifications                                           │
└───────────────────────────┬─────────────────────────────┘
                            │  depends on
┌───────────────────────────▼─────────────────────────────┐
│  DentaSchedule.DAL   (Data Access)                       │
│  EF Core DbContext · Entities · Repository · UnitOfWork  │
└─────────────────────────────────────────────────────────┘
```

> Design diagrams (architecture, ER, use-case, class, sequence) are in
> [`docs/diagrams.md`](docs/diagrams.md) and render directly on GitHub.

Key conventions:
- The BLL never throws HTTP exceptions; it returns a `ServiceResponse<T>` carrying
  success/data/errors. Controllers translate that into HTTP responses.
- Data access goes through the Repository + Unit of Work pattern.
- Authentication uses a short-lived JWT access token (15 min) plus a refresh token
  stored in the database and delivered to the browser as an HttpOnly cookie.

## Project structure

```
DentaSchedule.sln
├── DentaSchedule.API/          ASP.NET Core Web API (entry point)
├── DentaSchedule.BLL/          Services, scheduling engine, notifications
├── DentaSchedule.DAL/          EF Core context, entities, migrations, seed data
├── DentaSchedule.Tests/        xUnit test suite
└── dentaschedule-frontend/     React + Vite single-page application
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (or newer — the projects target `net8.0`)
- [Node.js 20+](https://nodejs.org/) and npm
- SQL Server **LocalDB** (ships with Visual Studio; verify with `sqllocaldb info`)
  or any SQL Server instance
- `dotnet-ef` tools (only if you want to manage migrations manually):
  `dotnet tool install --global dotnet-ef`

## Getting started

The app has two parts. Run them in two terminals.

### 1. Backend

```powershell
cd DentaSchedule.API

# Provide the JWT signing key (required — see Configuration below)
dotnet user-secrets set "Jwt:Secret" "a-random-string-of-at-least-32-characters"

# Run with the HTTPS profile (the frontend proxies to the HTTPS endpoint)
dotnet run --launch-profile https
```

On first run the API automatically applies EF Core migrations and seeds demo data.

- API: **https://localhost:7237** (also http://localhost:5014)
- Swagger UI: **https://localhost:7237/swagger**

### 2. Frontend

```powershell
cd dentaschedule-frontend
npm install        # first time only
npm run dev
```

- App: **https://localhost:5173**

The dev server proxies `/api` requests to `https://localhost:7237`, so start the
backend first. Your browser will warn about the self-signed dev certificate — accept it
to continue.

> **Production build:** `npm run build` outputs the SPA into `DentaSchedule.API/wwwroot`,
> which the API serves directly (`UseStaticFiles` + SPA fallback). In production the whole
> app is served from the API origin.

## Test accounts

Seeded automatically on first run. **For development/demo only — change before any real deployment.**

| Role      | Email                          | Password        | Scope            |
|-----------|--------------------------------|-----------------|------------------|
| Admin     | `admin@dentaschedule.com`      | `Admin@123`     | All clinics      |
| Assistant | `assistant1@dentaschedule.com` | `Assistant1@123`| Clinic 1         |
| Assistant | `assistant2@dentaschedule.com` | `Assistant2@123`| Clinic 2         |
| …         | `assistant{N}@dentaschedule.com` | `Assistant{N}@123` | Clinic N (N = 1–5) |

The seed also creates 6 clinics, 11 doctors, and Monday–Friday 09:00–17:00 schedules
(30-minute slots) so the booking flow is usable immediately.

## Configuration

Application settings live in `DentaSchedule.API/appsettings.json`. Secrets are **not**
stored there.

### Secrets

| Setting              | Development source        | Production source            |
|----------------------|---------------------------|------------------------------|
| `Jwt:Secret`         | .NET user-secrets (required) | `Jwt__Secret` env var (required) |
| `Seed:AdminPassword` | optional (falls back to `Admin@123`) | `Seed__AdminPassword` env var (required) |

The API **fails to start** if `Jwt:Secret` is missing or shorter than 32 characters.

```powershell
cd DentaSchedule.API
dotnet user-secrets set "Jwt:Secret" "<random 32+ char value>"
dotnet user-secrets set "Seed:AdminPassword" "<your admin password>"   # optional in dev
```

### Database

Connection string `ConnectionStrings:DefaultConnection` in `appsettings.json` defaults to
LocalDB. Point it at another SQL Server instance if needed.

### Email notifications

By default email is **logged, not sent** (`Email:UseSmtp = false`), so the app runs with
no mail credentials. To send real email, configure SMTP:

```jsonc
"Email": {
  "UseSmtp": true,
  "FromName": "DentaSchedule",
  "FromAddress": "no-reply@yourdomain.com",
  "SmtpHost": "smtp.example.com",
  "SmtpPort": 587,
  "Username": "...",
  "Password": "..."     // supply via user-secrets / env var, not appsettings
}
```

## API overview

All endpoints are under `/api/v1`. Full, interactive documentation is in Swagger.

| Method | Endpoint                              | Auth        | Description                        |
|--------|---------------------------------------|-------------|------------------------------------|
| POST   | `/auth/login`                         | Public      | Log in, receive JWT + refresh cookie |
| POST   | `/auth/refresh`                       | Public      | Rotate access token                |
| POST   | `/auth/logout`                        | Public      | Revoke refresh token               |
| GET    | `/clinics`                            | Public      | List active clinics                |
| GET    | `/clinics/{id}/doctors`               | Public      | Doctors at a clinic                |
| GET    | `/doctors/{id}/available-slots?date=` | Public      | Available time slots for a date    |
| POST   | `/appointments`                       | Public      | Create a pending appointment       |
| GET    | `/appointments/{reference}`           | Public      | Look up an appointment             |
| GET    | `/appointments`                       | Staff       | Paginated, filterable list         |
| PUT    | `/appointments/{id}/approve`          | Staff       | Approve a pending appointment      |
| PUT    | `/appointments/{id}/cancel`           | Staff       | Cancel with a reason               |
| PUT    | `/appointments/{id}/reschedule`       | Staff       | Move to a new time                 |
| GET    | `/appointments/dashboard`             | Staff       | Dashboard statistics               |
| CRUD   | `/clinics`, `/doctors`, `/users`      | Admin       | Management operations              |

*"Staff" = Admin or Assistant; Assistants are automatically scoped to their own clinic.*

## The scheduling engine

The core algorithm lives in `DentaSchedule.BLL/Services/AppointmentService.cs`.

**Available-slot generation** for a doctor on a date:
1. Load the doctor's `DoctorSchedule` rows for that day of the week.
2. Apply any `ScheduleException`: a full-day-off yields no slots; custom hours override
   the regular start/end.
3. Generate candidate slots from start to end, stepping by `SlotDurationMinutes`.
4. Mark slots occupied by an **approved** appointment as unavailable.
5. Mark past slots (for today) as unavailable.

**Booking** additionally validates that the requested time is in the future, that the
doctor belongs to the chosen clinic, that the slot falls within working hours, and that it
does not overlap an existing approved appointment (conflict detection).

**Concurrency.** Two safeguards prevent double-booking under concurrent access:
approval re-checks for a conflicting approved appointment, backed by a unique filtered
index (`DoctorId`, `AppointmentDateTime` where `Status = 'Approved'`) as the database-level
guarantee; and concurrent edits to the same appointment are detected via the `RowVersion`
optimistic-concurrency token and reported back to the caller rather than failing silently.

## Testing

```powershell
dotnet test DentaSchedule.Tests/DentaSchedule.Tests.csproj
```

The suite (32 tests) covers the scheduling algorithm, the appointment lifecycle
(create / approve / cancel / reschedule, including validation failures), notification
dispatch, and the concurrency safeguards (approval re-check and optimistic-concurrency
handling). Tests run against a real EF Core context backed by the in-memory provider, so
they exercise the actual query logic rather than mocks.

## Roadmap / future work

Candidate enhancements, roughly in priority order:

- Patient self-service cancellation/rescheduling by reference number
- `Completed` / `NoShow` appointment statuses and reporting on them
- SMS notifications (the email layer already abstracts the transport)
- Background reminder jobs (e.g. 24 h before an appointment)
- Rate limiting on the public booking endpoint
- Frontend component/integration tests and a CI pipeline
- Containerized deployment (Docker Compose: API + SQL Server + frontend)

---

*Developed as a bachelor's diploma (licență) project.*
