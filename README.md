# EduConnect — University Academic Web Portal

A role-based (Admin/Faculty/Student) academic management portal built with
Blazor Server (.NET 8), using a hand-written ADO.NET data layer — no ORM.

Originally built for Air University's Visual Programming course (Assignment 2:
in-memory repository pattern; Assignment 3: SQL Server integration). This
version has since been migrated from SQL Server to **SQLite** for
portability — see [Migration Notes](#migration-notes-sql-server--sqlite)
below.

## Tech Stack

- **Frontend/Backend**: Blazor Server (.NET 8), Bootstrap 5
- **Data Access**: Hand-written ADO.NET (`Microsoft.Data.Sqlite`) — parameterised
  queries throughout, no ORM
- **Database**: SQLite — a single self-contained file, no server install required

## Running It

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
dotnet restore
dotnet run
```

That's it. On first run, `Data/DbInitializer.cs` automatically creates
`EduConnect.db` in the project folder with the full schema and seeds demo
accounts — no manual database setup step.

**Demo logins** (seeded automatically):

| Role    | Email             | Password    |
|---------|-------------------|-------------|
| Admin   | admin@edu.com     | admin123    |
| Faculty | faculty@edu.com   | faculty123  |
| Student | alice@edu.com     | student123  |

(Also seeded: `bob@edu.com` and `carol@edu.com`, same password.)

## Modules

- **Auth & Role-Based Dashboard** — mock login against the `Users` table,
  route guarding via `AuthGuard`, reactive NavBar via a C# event
- **Student Management** — full CRUD, live search (two-way binding, SQL
  `LIKE` — no client-side filtering), custom
  `StudentHasActiveEnrollmentsException` blocking deletion of students with
  active enrollments
- **Course Management & Enrollment** — capacity checks, re-enrollment rules,
  and drop logic enforced in the service layer (not in `.razor` files)
- **Grading System** — letter grades and CGPA computed in C#, persisted per
  submission
- **Notifications** — `event Action<Notification>` broadcast pattern, no
  polling; `NotificationBell` subscribes/unsubscribes via `IDisposable`

## SOLID Compliance

See inline comments in `Services/*.cs` and `Interfaces/*.cs` — each service
has a single responsibility, all pages depend on interfaces (constructor/
`[Inject]` injection, never `new()`), and `IRepository<T>` allows new entity
types without touching existing repository code.

## Migration Notes: SQL Server → SQLite

The original Assignment 3 submission used `Microsoft.Data.SqlClient` against
a local SQL Server Express instance, with a connection string hardcoded to
one machine's hostname (`Server=MACHINENAME\SQLEXPRESS`). That's a real
limitation worth naming honestly: it meant the project only ran on the exact
machine it was built on, and had no schema script to rebuild the database
elsewhere if it was ever lost.

This version swaps to SQLite for three concrete reasons:
- **Zero setup** — no SQL Server Express install, no Windows-auth
  connection-string juggling. Clone the repo, `dotnet run`, done.
- **Portable by construction** — the database is a file (`EduConnect.db`)
  that lives in the project folder, not tied to any machine name.
- **Self-healing** — `DbInitializer` creates the schema and seed data from
  code every time the file doesn't exist, so the database can never again be
  "lost" the way the original SQL Server one was.

The migration touched only the data-access layer (`Services/*.cs` +
`Data/DbInitializer.cs`) — model classes, Razor pages, business rules, and
the SOLID structure are unchanged from the original submission. One
SQLite-specific detail worth flagging for anyone reading the code: SQLite has
no native GUID type, so all `Guid` values are stored as `TEXT` and
parsed/serialized at the service boundary; and `Microsoft.Data.Sqlite`
returns `COUNT(*)`/`ExecuteScalar()` results as `long`, not `int` — every
count check in `CourseService`/`GradeService`/`NotificationService` casts
accordingly to avoid a runtime `InvalidCastException`.

## Known Limitations

- Passwords are stored and compared as plain text — acceptable for a
  coursework demo, not production practice. Hashing (e.g. BCrypt) would be
  the next step before this handled real user data.
- No automated tests yet.
