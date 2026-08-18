using EduConnect.Enums;

namespace EduConnect.Models;

public class Enrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int CourseCreditHours { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.Now;
    public int Semester { get; set; } // Semester during which enrolled
    public CourseEnrollmentActiveStatus Status { get; set; } = CourseEnrollmentActiveStatus.Active;
}
