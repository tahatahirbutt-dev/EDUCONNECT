using EduConnect.Enums;
using EduConnect.Models;
using Microsoft.Data.Sqlite;

namespace EduConnect.Services;

/// <summary>
/// SRP: Manages authentication state only.
/// Reads from SQLite instead of SQL Server (migrated from the original
/// Microsoft.Data.SqlClient version — same query logic, SQLite-compatible
/// type handling: GUIDs are stored/read as TEXT since SQLite has no native
/// GUID type).
/// Events and Blazor reactivity remain exactly the same.
/// </summary>
public class AuthStateService
{
    private readonly string _connectionString;

    public Person? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;

    // Event system stays in-memory — Blazor SignalR reactivity doesn't need DB
    public event Action? OnAuthStateChanged;

    public AuthStateService(IConfiguration configuration)
    {
        // DIP: IConfiguration injected — not hardcoded
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public bool Login(string email, string password)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = new SqliteCommand(
            "SELECT * FROM Users WHERE Email = @Email AND Password = @Password", conn);
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@Password", password);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return false;

        var person = MapToPerson(reader);
        reader.Close();

        // Load enrollments into memory for the logged-in student
        if (person is Student student)
            LoadStudentEnrollments(student, conn);

        CurrentUser = person;
        OnAuthStateChanged?.Invoke(); // Fire event — NavBar re-renders
        return true;
    }

    public void Logout()
    {
        CurrentUser = null;
        OnAuthStateChanged?.Invoke();
    }

    public bool IsInRole(UserRole role) => CurrentUser?.GetRole() == role;

    /// <summary>
    /// Returns all users — used by NotificationService.BroadcastToRole()
    /// </summary>
    public List<Person> GetAllUsers()
    {
        var users = new List<Person>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd  = new SqliteCommand("SELECT * FROM Users", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            users.Add(MapToPerson(reader));
        return users;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Person MapToPerson(SqliteDataReader r)
    {
        var role     = (UserRole)r.GetInt32(r.GetOrdinal("Role"));
        var id       = Guid.Parse(r.GetString(r.GetOrdinal("Id")));
        var fullName = r.GetString(r.GetOrdinal("FullName"));
        var email    = r.GetString(r.GetOrdinal("Email"));
        var password = r.GetString(r.GetOrdinal("Password"));

        return role switch
        {
            UserRole.Admin => new Admin
            {
                Id = id, FullName = fullName, Email = email, Password = password
            },
            UserRole.Faculty => new Faculty
            {
                Id = id, FullName = fullName, Email = email, Password = password,
                Department = r.IsDBNull(r.GetOrdinal("Department")) ? ""
                           : r.GetString(r.GetOrdinal("Department"))
            },
            _ => new Student
            {
                Id = id, FullName = fullName, Email = email, Password = password,
                Semester = r.IsDBNull(r.GetOrdinal("Semester")) ? 1
                         : r.GetInt32(r.GetOrdinal("Semester")),
                CGPA     = r.IsDBNull(r.GetOrdinal("CGPA")) ? 0.0
                         : r.GetDouble(r.GetOrdinal("CGPA"))
            }
        };
    }

    private void LoadStudentEnrollments(Student student, SqliteConnection conn)
    {
        using var cmd = new SqliteCommand(
            "SELECT * FROM Enrollments WHERE StudentId = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", student.Id.ToString());
        using var r = cmd.ExecuteReader();
        while (r.Read())
            student.Enrollments.Add(MapEnrollment(r, student.Id));
    }

    internal static Enrollment MapEnrollment(SqliteDataReader r, Guid studentId) => new()
    {
        Id                = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        StudentId         = studentId,
        CourseId          = Guid.Parse(r.GetString(r.GetOrdinal("CourseId"))),
        CourseCode        = r.GetString(r.GetOrdinal("CourseCode")),
        CourseTitle       = r.GetString(r.GetOrdinal("CourseTitle")),
        CourseCreditHours = r.GetInt32(r.GetOrdinal("CourseCreditHours")),
        EnrolledAt        = r.GetDateTime(r.GetOrdinal("EnrolledAt")),
        Semester          = r.GetInt32(r.GetOrdinal("Semester")),
        Status            = (CourseEnrollmentActiveStatus)r.GetInt32(r.GetOrdinal("Status"))
    };
}
