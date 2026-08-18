using EduConnect.Interfaces;
using EduConnect.Models;
using Microsoft.Data.Sqlite;

namespace EduConnect.Services;

/// <summary>
/// SRP: Grade management and CGPA computation only.
/// All grade records persisted to the local SQLite .db file.
/// </summary>
public class GradeService : IGradeService
{
    private readonly string _connectionString;
    private readonly StudentService _studentService;

    public event Action<GradeRecord>? OnGradesSubmitted;

    public GradeService(IConfiguration configuration, StudentService studentService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        _studentService   = studentService;
    }

    public void SubmitGrade(GradeRecord record)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // Check if grade already exists for this student + course
        bool exists;
        using (var cmd = new SqliteCommand(@"
            SELECT COUNT(*) FROM GradeRecords
            WHERE StudentId=@SId AND CourseId=@CId", conn))
        {
            cmd.Parameters.AddWithValue("@SId", record.StudentId.ToString());
            cmd.Parameters.AddWithValue("@CId", record.CourseId.ToString());
            exists = (long)cmd.ExecuteScalar()! > 0;
        }

        if (exists)
        {
            // UPDATE existing grade
            using var cmd = new SqliteCommand(@"
                UPDATE GradeRecords SET Marks = @Marks
                WHERE StudentId=@SId AND CourseId=@CId", conn);
            cmd.Parameters.AddWithValue("@Marks", record.Marks);
            cmd.Parameters.AddWithValue("@SId",   record.StudentId.ToString());
            cmd.Parameters.AddWithValue("@CId",   record.CourseId.ToString());
            cmd.ExecuteNonQuery();
        }
        else
        {
            // INSERT new grade
            using var cmd = new SqliteCommand(@"
                INSERT INTO GradeRecords
                    (Id,StudentId,StudentName,CourseId,CourseCode,CourseTitle,CreditHours,Marks)
                VALUES
                    (@Id,@SId,@SName,@CId,@CCode,@CTitle,@Credits,@Marks)", conn);
            cmd.Parameters.AddWithValue("@Id",      record.Id.ToString());
            cmd.Parameters.AddWithValue("@SId",     record.StudentId.ToString());
            cmd.Parameters.AddWithValue("@SName",   record.StudentName);
            cmd.Parameters.AddWithValue("@CId",     record.CourseId.ToString());
            cmd.Parameters.AddWithValue("@CCode",   record.CourseCode);
            cmd.Parameters.AddWithValue("@CTitle",  record.CourseTitle);
            cmd.Parameters.AddWithValue("@Credits", record.CreditHours);
            cmd.Parameters.AddWithValue("@Marks",   record.Marks);
            cmd.ExecuteNonQuery();
        }

        // Recompute CGPA and persist it to the Users table
        double newCGPA = ComputeCGPA(record.StudentId);
        _studentService.UpdateCGPA(record.StudentId, newCGPA);

        OnGradesSubmitted?.Invoke(record); // Fire event
    }

    public void UpdateGrade(GradeRecord record) => SubmitGrade(record);

    public IEnumerable<GradeRecord> GetGradesForStudent(Guid studentId)
    {
        var list = new List<GradeRecord>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(
            "SELECT * FROM GradeRecords WHERE StudentId = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", studentId.ToString());
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapToGradeRecord(r));
        return list;
    }

    public IEnumerable<GradeRecord> GetGradesForCourse(Guid courseId)
    {
        var list = new List<GradeRecord>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(
            "SELECT * FROM GradeRecords WHERE CourseId = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", courseId.ToString());
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapToGradeRecord(r));
        return list;
    }

    public IEnumerable<GradeRecord> GetAllGrades()
    {
        var list = new List<GradeRecord>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand("SELECT * FROM GradeRecords", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapToGradeRecord(r));
        return list;
    }

    public GradeRecord? GetGrade(Guid studentId, Guid courseId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(@"
            SELECT * FROM GradeRecords
            WHERE StudentId=@SId AND CourseId=@CId", conn);
        cmd.Parameters.AddWithValue("@SId", studentId.ToString());
        cmd.Parameters.AddWithValue("@CId", courseId.ToString());
        using var r = cmd.ExecuteReader();
        return r.Read() ? MapToGradeRecord(r) : null;
    }

    /// <summary>
    /// CGPA = sum(GradePoints × CreditHours) / sum(CreditHours) — weighted average
    /// </summary>
    public double ComputeCGPA(Guid studentId)
    {
        var grades = GetGradesForStudent(studentId).ToList();
        if (!grades.Any()) return 0.0;
        double total   = grades.Sum(g => g.GradePoints * g.CreditHours);
        int    credits = grades.Sum(g => g.CreditHours);
        return credits == 0 ? 0.0 : Math.Round(total / credits, 2);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static GradeRecord MapToGradeRecord(SqliteDataReader r) => new()
    {
        Id          = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        StudentId   = Guid.Parse(r.GetString(r.GetOrdinal("StudentId"))),
        StudentName = r.GetString(r.GetOrdinal("StudentName")),
        CourseId    = Guid.Parse(r.GetString(r.GetOrdinal("CourseId"))),
        CourseCode  = r.GetString(r.GetOrdinal("CourseCode")),
        CourseTitle = r.GetString(r.GetOrdinal("CourseTitle")),
        CreditHours = r.GetInt32(r.GetOrdinal("CreditHours")),
        Marks       = r.GetDouble(r.GetOrdinal("Marks"))
    };
}
