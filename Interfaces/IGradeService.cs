using EduConnect.Models;

namespace EduConnect.Interfaces;

// ISP: Grade-specific operations are isolated here, not mixed into IStudentService.
public interface IGradeService
{
    void SubmitGrade(GradeRecord record);
    void UpdateGrade(GradeRecord record);
    IEnumerable<GradeRecord> GetGradesForStudent(Guid studentId);
    IEnumerable<GradeRecord> GetGradesForCourse(Guid courseId);
    IEnumerable<GradeRecord> GetAllGrades();
    GradeRecord? GetGrade(Guid studentId, Guid courseId);
    double ComputeCGPA(Guid studentId);

    event Action<GradeRecord>? OnGradesSubmitted;
}
