using EduConnect.Enums;
using EduConnect.Exceptions;
using EduConnect.Interfaces;
using EduConnect.Models;
using Microsoft.Data.Sqlite;

namespace EduConnect.Services;

/// <summary>
/// SRP: Student data management only.
/// ADO.NET (SQLite) replaces the in-memory List — all queries go to the
/// local .db file. Interface stays the same → zero changes needed in
/// .razor pages.
/// </summary>
public class StudentService : IStudentService
{
    private readonly string _connectionString;
    public event Action<Student>? OnStudentUpdated;

    public StudentService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    // ── IRepository<Student> ─────────────────────────────────────────────────

    public IEnumerable<Student> GetAll()
    {
        var students = new List<Student>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using (var cmd = new SqliteCommand("SELECT * FROM Users WHERE Role = 2", conn))
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read()) students.Add(MapToStudent(r));
        }

        foreach (var s in students) LoadEnrollments(s, conn);
        return students;
    }

    public Student? GetById(Guid id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        Student? student = null;
        using (var cmd = new SqliteCommand(
            "SELECT * FROM Users WHERE Id = @Id AND Role = 2", conn))
        {
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var r = cmd.ExecuteReader();
            if (r.Read()) student = MapToStudent(r);
        }

        if (student is not null) LoadEnrollments(student, conn);
        return student;
    }

    public void Add(Student student)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(@"
            INSERT INTO Users (Id, FullName, Email, Password, Role, Semester, CGPA)
            VALUES (@Id, @FullName, @Email, @Password, 2, @Semester, @CGPA)", conn);

        cmd.Parameters.AddWithValue("@Id",       student.Id.ToString());
        cmd.Parameters.AddWithValue("@FullName", student.FullName);
        cmd.Parameters.AddWithValue("@Email",    student.Email);
        cmd.Parameters.AddWithValue("@Password", student.Password);
        cmd.Parameters.AddWithValue("@Semester", student.Semester);
        cmd.Parameters.AddWithValue("@CGPA",     student.CGPA);
        cmd.ExecuteNonQuery();

        OnStudentUpdated?.Invoke(student);
    }

    public void Update(Student student)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(@"
            UPDATE Users
            SET FullName = @FullName, Email = @Email,
                Semester = @Semester, CGPA  = @CGPA
            WHERE Id = @Id", conn);

        cmd.Parameters.AddWithValue("@Id",       student.Id.ToString());
        cmd.Parameters.AddWithValue("@FullName", student.FullName);
        cmd.Parameters.AddWithValue("@Email",    student.Email);
        cmd.Parameters.AddWithValue("@Semester", student.Semester);
        cmd.Parameters.AddWithValue("@CGPA",     student.CGPA);
        cmd.ExecuteNonQuery();

        OnStudentUpdated?.Invoke(student);
    }

    /// <summary>
    /// Throws StudentHasActiveEnrollmentsException if student has active enrollments.
    /// Caught in the .razor component — not deleted in that case.
    /// </summary>
    public void Delete(Guid id)
    {
        var student = GetById(id);
        if (student is null) return;

        bool hasActive = student.Enrollments
            .Any(e => e.Status == CourseEnrollmentActiveStatus.Active);
        if (hasActive)
            throw new StudentHasActiveEnrollmentsException(student.FullName);

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand("DELETE FROM Users WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id.ToString());
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Live search used with two-way binding on the Students list page.
    /// Uses SQL LIKE — no client-side filtering needed.
    /// </summary>
    public IEnumerable<Student> SearchStudents(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return GetAll();

        var students = new List<Student>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using (var cmd = new SqliteCommand(@"
            SELECT * FROM Users
            WHERE Role = 2
              AND (FullName LIKE @Q OR Email LIKE @Q)", conn))
        {
            cmd.Parameters.AddWithValue("@Q", $"%{query}%");
            using var r = cmd.ExecuteReader();
            while (r.Read()) students.Add(MapToStudent(r));
        }

        foreach (var s in students) LoadEnrollments(s, conn);
        return students;
    }

    /// <summary>
    /// Called by GradeService after grades are submitted to persist updated CGPA.
    /// </summary>
    public void UpdateCGPA(Guid studentId, double cgpa)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(
            "UPDATE Users SET CGPA = @CGPA WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@CGPA", cgpa);
        cmd.Parameters.AddWithValue("@Id",   studentId.ToString());
        cmd.ExecuteNonQuery();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Student MapToStudent(SqliteDataReader r) => new()
    {
        Id       = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        FullName = r.GetString(r.GetOrdinal("FullName")),
        Email    = r.GetString(r.GetOrdinal("Email")),
        Password = r.GetString(r.GetOrdinal("Password")),
        Semester = r.IsDBNull(r.GetOrdinal("Semester")) ? 1
                 : r.GetInt32(r.GetOrdinal("Semester")),
        CGPA     = r.IsDBNull(r.GetOrdinal("CGPA"))     ? 0.0
                 : r.GetDouble(r.GetOrdinal("CGPA"))
    };

    private static void LoadEnrollments(Student student, SqliteConnection conn)
    {
        using var cmd = new SqliteCommand(
            "SELECT * FROM Enrollments WHERE StudentId = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", student.Id.ToString());
        using var r = cmd.ExecuteReader();
        while (r.Read())
            student.Enrollments.Add(AuthStateService.MapEnrollment(r, student.Id));
    }
}
