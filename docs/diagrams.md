# DentaSchedule — Design Diagrams

This document collects the design diagrams for the DentaSchedule system. They are written
in [Mermaid](https://mermaid.js.org/), so they render automatically on GitHub. To export a
figure for the thesis (SVG/PNG), paste a block into <https://mermaid.live> or use the
"Markdown Preview Mermaid Support" / "Mermaid Editor" extension in VS Code.

Contents:
1. [System architecture](#1-system-architecture)
2. [Entity–relationship diagram](#2-entityrelationship-diagram)
3. [Use-case diagram](#3-use-case-diagram)
4. [Class diagram (appointment slice)](#4-class-diagram-appointment-slice)
5. [Sequence diagram — booking an appointment](#5-sequence-diagram--booking-an-appointment)

---

## 1. System architecture

Three-layer backend with a clear dependency direction (Presentation → Business → Data),
a React single-page app on the client, and SQL Server / SMTP as external resources.

```mermaid
flowchart TB
    subgraph Client["Client (browser)"]
        SPA["React SPA<br/>Vite · TypeScript · Tailwind"]
    end

    subgraph API["DentaSchedule.API — Presentation"]
        direction TB
        CTRL["Controllers"]
        VAL["FluentValidation"]
        MW["Exception middleware"]
        AUTH["JWT authentication"]
    end

    subgraph BLL["DentaSchedule.BLL — Business logic"]
        direction TB
        SVC["Services<br/>(ServiceResponse&lt;T&gt;)"]
        SCHED["Scheduling engine"]
        NOTIF["Notification service"]
        EMAIL["IEmailService<br/>(logging / SMTP)"]
    end

    subgraph DAL["DentaSchedule.DAL — Data access"]
        direction TB
        UOW["UnitOfWork + Repositories"]
        CTX["EF Core DbContext<br/>(Identity)"]
    end

    DB[("SQL Server")]
    MAIL[("SMTP server")]

    SPA -- "HTTPS · /api/v1 (JWT)" --> CTRL
    CTRL --> SVC
    SVC --> SCHED
    SVC --> NOTIF
    NOTIF --> EMAIL
    SVC --> UOW
    UOW --> CTX
    CTX --> DB
    EMAIL -. "when UseSmtp=true" .-> MAIL
```

---

## 2. Entity–relationship diagram

The persistent domain model. Authentication tables (`AppUser`, `AppRole`, `RefreshToken`)
come from ASP.NET Core Identity; the remaining standard Identity tables are omitted for
clarity. `Clinic` and `Doctor` use soft deletes (`IsDeleted`). Foreign-key delete
behaviour is noted on each relationship.

```mermaid
erDiagram
    CLINIC ||--o{ DOCTOR : "employs (Restrict)"
    CLINIC ||--o{ APPOINTMENT : "hosts (Restrict)"
    CLINIC |o--o{ APPUSER : "staffs (SetNull)"
    DOCTOR ||--o{ DOCTORSCHEDULE : "has (Cascade)"
    DOCTOR ||--o{ SCHEDULEEXCEPTION : "has (Cascade)"
    DOCTOR ||--o{ APPOINTMENT : "serves (Restrict)"
    APPUSER ||--o{ REFRESHTOKEN : "owns (Cascade)"
    APPUSER }o--o{ APPROLE : "assigned"

    CLINIC {
        Guid Id PK
        string Name
        string Address
        string Phone
        string Email
        string LogoUrl "nullable"
        bool IsActive
        bool IsDeleted
    }
    DOCTOR {
        Guid Id PK
        string FullName
        string Specialization
        string Bio "nullable"
        string PhotoUrl "nullable"
        bool IsActive
        bool IsDeleted
        Guid ClinicId FK
    }
    DOCTORSCHEDULE {
        Guid Id PK
        DayOfWeek DayOfWeek
        TimeSpan StartTime
        TimeSpan EndTime
        int SlotDurationMinutes
        Guid DoctorId FK
    }
    SCHEDULEEXCEPTION {
        Guid Id PK
        DateTime ExceptionDate
        string Reason "nullable"
        bool IsFullDayOff
        TimeSpan CustomStart "nullable"
        TimeSpan CustomEnd "nullable"
        Guid DoctorId FK
    }
    APPOINTMENT {
        Guid Id PK
        string PatientName
        string PatientEmail
        string PatientPhone
        DateTime AppointmentDateTime
        int DurationMinutes
        AppointmentStatus Status
        string Notes "nullable"
        string CancellationReason "nullable"
        string ReferenceNumber UK
        DateTime CreatedAt
        DateTime UpdatedAt "nullable"
        byte_array RowVersion
        Guid DoctorId FK
        Guid ClinicId FK
    }
    APPUSER {
        string Id PK
        string Email
        string DisplayName
        Guid ClinicId FK "nullable"
    }
    APPROLE {
        string Id PK
        string Name
    }
    REFRESHTOKEN {
        Guid Id PK
        string Token
        DateTime ExpiresAt
        DateTime CreatedAt
        bool IsRevoked
        string UserId FK
    }
```

---

## 3. Use-case diagram

Three actors. The **Patient** is anonymous (no account); **Assistant** and **Admin**
authenticate, with the Assistant scoped to a single clinic. Mermaid has no native UML
use-case notation, so this is an approximation using a graph (ovals = use cases).

```mermaid
flowchart LR
    Patient(["👤 Patient<br/>(anonymous)"])
    Assistant(["👤 Assistant"])
    Admin(["👤 Admin"])

    subgraph Booking["Public booking"]
        UC1(["Browse clinics & doctors"])
        UC2(["View available slots"])
        UC3(["Book appointment"])
        UC4(["Look up appointment<br/>by reference"])
    end

    subgraph Manage["Appointment management"]
        UC5(["View clinic appointments"])
        UC6(["Approve appointment"])
        UC7(["Cancel appointment"])
        UC8(["Reschedule appointment"])
        UC9(["View dashboard stats"])
    end

    subgraph AdminOnly["Administration"]
        UC10(["Manage clinics"])
        UC11(["Manage doctors & schedules"])
        UC12(["Manage users & roles"])
    end

    Patient --> UC1 & UC2 & UC3 & UC4
    Assistant --> UC5 & UC6 & UC7 & UC8 & UC9
    Admin --> UC5 & UC6 & UC7 & UC8 & UC9
    Admin --> UC10 & UC11 & UC12
```

---

## 4. Class diagram (appointment slice)

The appointment vertical slice, showing the three-layer pattern: a thin controller
depending on a service interface, a service returning `ServiceResponse<T>` and depending on
the Unit of Work and the notification abstraction, and the transport-agnostic email
abstraction with two implementations. Other slices (clinics, doctors, users) follow the
same shape.

```mermaid
classDiagram
    direction LR

    class AppointmentsController {
        -IAppointmentService _service
        +CreateAppointment(dto) IActionResult
        +Approve(id) IActionResult
        +Cancel(id, dto) IActionResult
        +Reschedule(id, dto) IActionResult
    }

    class IAppointmentService {
        <<interface>>
        +GetAvailableSlotsAsync(doctorId, date)
        +CreateAppointmentAsync(request)
        +ApproveAppointmentAsync(id)
        +CancelAppointmentAsync(id, reason)
        +RescheduleAppointmentAsync(id, newDateTime)
    }

    class AppointmentService {
        -IUnitOfWork _unitOfWork
        -IAppointmentNotificationService _notifications
        -HasConflictAsync(...) bool
        -IsSlotWithinScheduleAsync(...) bool
    }

    class IUnitOfWork {
        <<interface>>
        +IRepository~Appointment~ Appointments
        +IRepository~Doctor~ Doctors
        +IRepository~Clinic~ Clinics
        +SaveChangesAsync() int
    }

    class IRepository~T~ {
        <<interface>>
        +GetByIdAsync(id) T
        +Query() IQueryable~T~
        +AddAsync(entity)
        +Update(entity)
    }

    class IAppointmentNotificationService {
        <<interface>>
        +NotifyCreatedAsync(appointment)
        +NotifyApprovedAsync(appointment)
        +NotifyCancelledAsync(appointment)
        +NotifyRescheduledAsync(appointment)
    }

    class IEmailService {
        <<interface>>
        +SendAsync(message) bool
    }

    class LoggingEmailService
    class SmtpEmailService
    class ServiceResponse~T~ {
        +bool Success
        +T Data
        +List~string~ Errors
    }

    AppointmentsController ..> IAppointmentService
    IAppointmentService <|.. AppointmentService
    AppointmentService ..> IUnitOfWork
    AppointmentService ..> IAppointmentNotificationService
    AppointmentService ..> ServiceResponse~T~
    IUnitOfWork o-- IRepository~T~
    IAppointmentNotificationService <|.. AppointmentNotificationService
    AppointmentNotificationService ..> IEmailService
    IEmailService <|.. LoggingEmailService
    IEmailService <|.. SmtpEmailService
```

---

## 5. Sequence diagram — booking an appointment

The public booking flow: the patient first fetches available slots, then submits a
booking. The service validates the request (future date, doctor belongs to the clinic, no
conflict with an approved appointment, within working hours) before persisting and
dispatching a confirmation email. A failed validation returns early with no side effects.

```mermaid
sequenceDiagram
    actor P as Patient (browser)
    participant C as Controllers
    participant S as AppointmentService
    participant U as UnitOfWork / Repositories
    participant DB as SQL Server
    participant N as NotificationService
    participant E as IEmailService

    Note over P,DB: Step 1 — fetch available slots
    P->>C: GET /doctors/{id}/available-slots?date=...
    C->>S: GetAvailableSlotsAsync(doctorId, date)
    S->>U: query schedule, exceptions, approved appointments
    U->>DB: SELECT ...
    DB-->>U: rows
    U-->>S: data
    S-->>C: List<TimeSlotDto>
    C-->>P: 200 OK (slots)

    Note over P,E: Step 2 — submit booking
    P->>C: POST /appointments (patient + slot)
    C->>S: CreateAppointmentAsync(request)
    S->>S: validate (future? doctor@clinic? conflict? in hours?)
    alt invalid
        S-->>C: ServiceResponse(Success=false, Errors)
        C-->>P: 400 Bad Request
    else valid
        S->>U: AddAsync(appointment) + SaveChangesAsync()
        U->>DB: INSERT
        DB-->>U: ok
        S->>N: NotifyCreatedAsync(appointment)
        N->>E: SendAsync(confirmation email)
        S-->>C: ServiceResponse(Success=true, appointment)
        C-->>P: 201 Created (reference number)
    end
```
