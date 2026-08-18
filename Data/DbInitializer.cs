using Microsoft.Data.Sqlite;

namespace EduConnect.Data;

/// <summary>
/// Creates EduConnect.db and its schema automatically if the file doesn't
/// exist yet, and seeds demo accounts so the app is usable immediately
/// after `dotnet run` — no separate DB setup step, no SQL Server
/// installation required.
///
/// This intentionally does NOT wipe the database if it already exists —
/// it only creates tables with IF NOT EXISTS and seeds when Users is empty,
/// so re-running the app after real usage won't reset your data.
/// </summary>
public static class DbInitializer
{
    public static void Initialize(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id         TEXT PRIMARY KEY,
                    FullName   TEXT NOT NULL,
                    Email      TEXT NOT NULL UNIQUE,
                    Password   TEXT NOT NULL,
                    Role       INTEGER NOT NULL,
                    Department TEXT NULL,
                    Semester   INTEGER NULL,
                    CGPA       REAL NULL
                );

                CREATE TABLE IF NOT EXISTS Courses (
                    Id          TEXT PRIMARY KEY,
                    Code        TEXT NOT NULL,
                    Title       TEXT NOT NULL,
                    CreditHours INTEGER NOT NULL,
                    MaxCapacity INTEGER NOT NULL,
                    FacultyId   TEXT NULL,
                    FacultyName TEXT NULL
                );

                CREATE TABLE IF NOT EXISTS Enrollments (
                    Id                TEXT PRIMARY KEY,
                    StudentId         TEXT NOT NULL,
                    CourseId          TEXT NOT NULL,
                    CourseCode        TEXT NOT NULL,
                    CourseTitle       TEXT NOT NULL,
                    CourseCreditHours INTEGER NOT NULL,
                    EnrolledAt        TEXT NOT NULL,
                    Semester          INTEGER NOT NULL,
                    Status            INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (StudentId) REFERENCES Users(Id),
                    FOREIGN KEY (CourseId)  REFERENCES Courses(Id)
                );

                CREATE TABLE IF NOT EXISTS GradeRecords (
                    Id          TEXT PRIMARY KEY,
                    StudentId   TEXT NOT NULL,
                    StudentName TEXT NOT NULL,
                    CourseId    TEXT NOT NULL,
                    CourseCode  TEXT NOT NULL,
                    CourseTitle TEXT NOT NULL,
                    CreditHours INTEGER NOT NULL,
                    Marks       REAL NOT NULL,
                    FOREIGN KEY (StudentId) REFERENCES Users(Id),
                    FOREIGN KEY (CourseId)  REFERENCES Courses(Id)
                );

                CREATE TABLE IF NOT EXISTS Notifications (
                    Id        TEXT PRIMARY KEY,
                    UserId    TEXT NOT NULL,
                    Message   TEXT NOT NULL,
                    Type      INTEGER NOT NULL,
                    IsRead    INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );";
            cmd.ExecuteNonQuery();
        }

        // Only seed if the database is genuinely empty — never overwrite real data
        using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(*) FROM Users";
            long userCount = (long)checkCmd.ExecuteScalar()!;
            if (userCount > 0) return;
        }

        Seed(conn);
    }

    private static void Seed(SqliteConnection conn)
    {
        var adminId   = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var alice     = Guid.NewGuid();
        var bob       = Guid.NewGuid();
        var carol     = Guid.NewGuid();
        var course1   = Guid.NewGuid();
        var course2   = Guid.NewGuid();

        using var tx = conn.BeginTransaction();

        void InsertUser(Guid id, string name, string email, string password, int role,
                         string? department, int? semester, double? cgpa)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO Users (Id, FullName, Email, Password, Role, Department, Semester, CGPA)
                VALUES (@Id, @Name, @Email, @Pass, @Role, @Dept, @Sem, @Cgpa)";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Pass", password);
            cmd.Parameters.AddWithValue("@Role", role);
            cmd.Parameters.AddWithValue("@Dept", (object?)department ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Sem", (object?)semester ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Cgpa", (object?)cgpa ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        InsertUser(adminId,   "Admin User",      "admin@edu.com",   "admin123",   0, null, null, null);
        InsertUser(facultyId, "Dr. John Doe",    "faculty@edu.com", "faculty123", 1, "Computer Science", null, null);
        InsertUser(alice,     "Alice Hadi",    "alice@edu.com",   "student123", 2, null, 4, 3.50);
        InsertUser(bob,       "Bob Williams",  "bob@edu.com",     "student123", 2, null, 2, 3.00);
        InsertUser(carol,     "Carol Davis",   "carol@edu.com",   "student123", 2, null, 6, 2.00);

        void InsertCourse(Guid id, string code, string title, int credits, int capacity,
                           Guid facId, string facName)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO Courses (Id, Code, Title, CreditHours, MaxCapacity, FacultyId, FacultyName)
                VALUES (@Id, @Code, @Title, @Credits, @Cap, @FacId, @FacName)";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.Parameters.AddWithValue("@Code", code);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Credits", credits);
            cmd.Parameters.AddWithValue("@Cap", capacity);
            cmd.Parameters.AddWithValue("@FacId", facId.ToString());
            cmd.Parameters.AddWithValue("@FacName", facName);
            cmd.ExecuteNonQuery();
        }

        InsertCourse(course1, "CS101", "Introduction to Programming", 3, 30, facultyId, "Dr. John Doe");
        InsertCourse(course2, "HRM101", "Human Resource Management", 3, 25, facultyId, "Dr. John Doe");

        var course3 = Guid.NewGuid();
        InsertCourse(course3, "API101", "API Testing", 3, 25, facultyId, "Dr. John Doe");

        void InsertEnrollment(Guid studentId, Guid courseId, string code, string title,
                               int credits, int semester)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO Enrollments
                    (Id, StudentId, CourseId, CourseCode, CourseTitle, CourseCreditHours, EnrolledAt, Semester, Status)
                VALUES
                    (@Id, @SId, @CId, @Code, @Title, @Credits, @EnrolledAt, @Sem, 0)";
            cmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("@SId", studentId.ToString());
            cmd.Parameters.AddWithValue("@CId", courseId.ToString());
            cmd.Parameters.AddWithValue("@Code", code);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Credits", credits);
            cmd.Parameters.AddWithValue("@EnrolledAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@Sem", semester);
            cmd.ExecuteNonQuery();
        }

        InsertEnrollment(alice, course1, "CS101", "Introduction to Programming", 3, 4);
        InsertEnrollment(bob,   course1, "CS101", "Introduction to Programming", 3, 2);

        tx.Commit();
    }
}
