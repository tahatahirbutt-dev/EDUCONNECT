using EduConnect.Enums;
using EduConnect.Exceptions;
using EduConnect.Interfaces;
using EduConnect.Models;
using Microsoft.Data.Sqlite;

namespace EduConnect.Services;

/// <summary>
/// SRP: Course and enrollment management only.
/// Business rules (capacity, re-enroll) enforced HERE in the service — not in .razor pages.
/// NOTE: Microsoft.Data.Sqlite returns COUNT(*)/ExecuteScalar as System.Int64 (long),
/// not int like Microsoft.Data.SqlClient did — every count check below casts to
/// (long) first, then to int, to avoid an InvalidCastException at runtime.
/// </summary>
public class CourseService : ICourseService
{
    private readonly string _connectionString;
    public event Action<Student, Course>? OnEnrollmentChanged;

    public CourseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    // ── IRepository<Course> ──────────────────────────────────────────────────

    public IEnumerable<Course> GetAll()
    {
        var courses = new List<Course>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using (var cmd = new SqliteCommand("SELECT * FROM Courses", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read()) courses.Add(MapToCourse(r));

        foreach (var c in courses) LoadEnrollments(c, conn);
        return courses;
    }

    public Course? GetById(Guid id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        Course? course = null;
        using (var cmd = new SqliteCommand("SELECT * FROM Courses WHERE Id = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var r = cmd.ExecuteReader();
            if (r.Read()) course = MapToCourse(r);
        }

        if (course is not null) LoadEnrollments(course, conn);
        return course;
    }

    public void Add(Course course)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(@"
            INSERT INTO Courses (Id,Code,Title,CreditHours,MaxCapacity,FacultyId,FacultyName)
            VALUES (@Id,@Code,@Title,@CreditHours,@MaxCapacity,@FacultyId,@FacultyName)", conn);

        cmd.Parameters.AddWithValue("@Id",          course.Id.ToString());
        cmd.Parameters.AddWithValue("@Code",         course.Code);
        cmd.Parameters.AddWithValue("@Title",        course.Title);
        cmd.Parameters.AddWithValue("@CreditHours",  course.CreditHours);
        cmd.Parameters.AddWithValue("@MaxCapacity",  course.MaxCapacity);
        cmd.Parameters.AddWithValue("@FacultyId",    (object?)course.FacultyId?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FacultyName",  course.FacultyName ?? "");
        cmd.ExecuteNonQuery();
    }

    public void Update(Course course)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(@"
            UPDATE Courses
            SET Code=@Code, Title=@Title, CreditHours=@CreditHours,
                MaxCapacity=@MaxCapacity, FacultyId=@FacultyId, FacultyName=@FacultyName
            WHERE Id=@Id", conn);

        cmd.Parameters.AddWithValue("@Id",          course.Id.ToString());
        cmd.Parameters.AddWithValue("@Code",         course.Code);
        cmd.Parameters.AddWithValue("@Title",        course.Title);
        cmd.Parameters.AddWithValue("@CreditHours",  course.CreditHours);
        cmd.Parameters.AddWithValue("@MaxCapacity",  course.MaxCapacity);
        cmd.Parameters.AddWithValue("@FacultyId",    (object?)course.FacultyId?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FacultyName",  course.FacultyName ?? "");
        cmd.ExecuteNonQuery();
    }

    public void Delete(Guid id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // Delete related enrollments first (referential integrity)
        using (var cmd = new SqliteCommand(
            "DELETE FROM Enrollments WHERE CourseId = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }
        using (var cmd = new SqliteCommand("DELETE FROM Courses WHERE Id = @Id", conn))
        {
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }
    }

    // ── ICourseService ───────────────────────────────────────────────────────

    /// <summary>
    /// Business rules enforced here (SRP) — components just call this and catch exceptions.
    /// </summary>
    public void EnrollStudent(Student student, Course course)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // Rule 1: capacity check
        using (var cmd = new SqliteCommand(
            "SELECT COUNT(*) FROM Enrollments WHERE CourseId=@CId AND Status=0", conn))
        {
            cmd.Parameters.AddWithValue("@CId", course.Id.ToString());
            long activeCount = (long)cmd.ExecuteScalar()!;
            if (activeCount >= course.MaxCapacity)
                throw new CourseFullException(course.Title);
        }

        // Rule 2: dropped this semester cannot re-enroll
        using (var cmd = new SqliteCommand(@"
            SELECT COUNT(*) FROM Enrollments
            WHERE StudentId=@SId AND CourseId=@CId AND Status=1 AND Semester=@Sem", conn))
        {
            cmd.Parameters.AddWithValue("@SId", student.Id.ToString());
            cmd.Parameters.AddWithValue("@CId", course.Id.ToString());
            cmd.Parameters.AddWithValue("@Sem", student.Semester);
            long droppedCount = (long)cmd.ExecuteScalar()!;
            if (droppedCount > 0)
                throw new AlreadyDroppedCourseException(course.Title);
        }

        // Rule 3: already enrolled
        using (var cmd = new SqliteCommand(@"
            SELECT COUNT(*) FROM Enrollments
            WHERE StudentId=@SId AND CourseId=@CId AND Status=0", conn))
        {
            cmd.Parameters.AddWithValue("@SId", student.Id.ToString());
            cmd.Parameters.AddWithValue("@CId", course.Id.ToString());
            long alreadyEnrolled = (long)cmd.ExecuteScalar()!;
            if (alreadyEnrolled > 0) return;
        }

        // INSERT enrollment into DB
        var enrollId = Guid.NewGuid();
        using (var cmd = new SqliteCommand(@"
            INSERT INTO Enrollments
                (Id,StudentId,CourseId,CourseCode,CourseTitle,CourseCreditHours,EnrolledAt,Semester,Status)
            VALUES
                (@Id,@SId,@CId,@Code,@Title,@Credits,@EnrolledAt,@Sem,0)", conn))
        {
            cmd.Parameters.AddWithValue("@Id",      enrollId.ToString());
            cmd.Parameters.AddWithValue("@SId",     student.Id.ToString());
            cmd.Parameters.AddWithValue("@CId",     course.Id.ToString());
            cmd.Parameters.AddWithValue("@Code",    course.Code);
            cmd.Parameters.AddWithValue("@Title",   course.Title);
            cmd.Parameters.AddWithValue("@Credits", course.CreditHours);
            cmd.Parameters.AddWithValue("@EnrolledAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@Sem",     student.Semester);
            cmd.ExecuteNonQuery();
        }

        // Keep in-memory student object in sync so the page doesn't need a reload
        var enrollment = new Enrollment
        {
            Id = enrollId, StudentId = student.Id, CourseId = course.Id,
            CourseCode = course.Code, CourseTitle = course.Title,
            CourseCreditHours = course.CreditHours,
            Semester = student.Semester, Status = CourseEnrollmentActiveStatus.Active
        };
        student.Enrollments.Add(enrollment);
        course.Enrollments.Add(enrollment);

        OnEnrollmentChanged?.Invoke(student, course);
    }

    public void DropCourse(Student student, Course course)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(@"
            UPDATE Enrollments SET Status = 1
            WHERE StudentId=@SId AND CourseId=@CId AND Status=0", conn);
        cmd.Parameters.AddWithValue("@SId", student.Id.ToString());
        cmd.Parameters.AddWithValue("@CId", course.Id.ToString());
        int rows = cmd.ExecuteNonQuery();
        if (rows == 0)
            throw new InvalidOperationException("No active enrollment found for this course.");

        // Sync in-memory object
        var e = student.Enrollments.FirstOrDefault(
            e => e.CourseId == course.Id && e.Status == CourseEnrollmentActiveStatus.Active);
        if (e is not null) e.Status = CourseEnrollmentActiveStatus.Dropped;

        OnEnrollmentChanged?.Invoke(student, course);
    }

    public IEnumerable<Course> GetCoursesForFaculty(Guid facultyId)
    {
        var courses = new List<Course>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using (var cmd = new SqliteCommand(
            "SELECT * FROM Courses WHERE FacultyId = @FId", conn))
        {
            cmd.Parameters.AddWithValue("@FId", facultyId.ToString());
            using var r = cmd.ExecuteReader();
            while (r.Read()) courses.Add(MapToCourse(r));
        }

        foreach (var c in courses) LoadEnrollments(c, conn);
        return courses;
    }

    public IEnumerable<Course> GetAvailableCourses(Guid studentId) =>
        GetAll().Where(c => c.EnrollmentStatus != EnrollmentStatus.Full);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Course MapToCourse(SqliteDataReader r) => new()
    {
        Id          = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        Code        = r.GetString(r.GetOrdinal("Code")),
        Title       = r.GetString(r.GetOrdinal("Title")),
        CreditHours = r.GetInt32(r.GetOrdinal("CreditHours")),
        MaxCapacity = r.GetInt32(r.GetOrdinal("MaxCapacity")),
        FacultyId   = r.IsDBNull(r.GetOrdinal("FacultyId")) ? null
                    : Guid.Parse(r.GetString(r.GetOrdinal("FacultyId"))),
        FacultyName = r.IsDBNull(r.GetOrdinal("FacultyName")) ? ""
                    : r.GetString(r.GetOrdinal("FacultyName"))
    };

    private static void LoadEnrollments(Course course, SqliteConnection conn)
    {
        using var cmd = new SqliteCommand(
            "SELECT * FROM Enrollments WHERE CourseId = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", course.Id.ToString());
        using var r = cmd.ExecuteReader();
        while (r.Read())
            course.Enrollments.Add(AuthStateService.MapEnrollment(r,
                Guid.Parse(r.GetString(r.GetOrdinal("StudentId")))));
    }
}
