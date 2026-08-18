using EduConnect.Enums;

namespace EduConnect.Models;

// SRP: Course manages course data and its own enrollment state computation.
public class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int CreditHours { get; set; } = 3;
    public int MaxCapacity { get; set; } = 30;
    public Guid? FacultyId { get; set; }
    public string FacultyName { get; set; } = string.Empty;

    public List<Enrollment> Enrollments { get; set; } = new();

    // Computed properties — business logic lives in the model, not in .razor files
    public int ActiveEnrollmentCount =>
        Enrollments.Count(e => e.Status == CourseEnrollmentActiveStatus.Active);

    // OCP: EnrollmentStatus is computed via a property, not scattered across UI components
    public EnrollmentStatus EnrollmentStatus
    {
        get
        {
            double ratio = MaxCapacity == 0 ? 1 : (double)ActiveEnrollmentCount / MaxCapacity;
            if (ratio >= 1.0) return EnrollmentStatus.Full;
            if (ratio >= 0.8) return EnrollmentStatus.AlmostFull;
            return EnrollmentStatus.Open;
        }
    }

    public int EnrollmentProgress =>
        MaxCapacity == 0 ? 100 : (int)Math.Round((double)ActiveEnrollmentCount / MaxCapacity * 100);
}
