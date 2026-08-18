using EduConnect.Enums;
using EduConnect.Interfaces;

namespace EduConnect.Models;

// SRP: Student only manages student-specific data.
// IValidatable: ISP — only Student (and other form entities) implement this.
public class Student : Person, IValidatable
{
    public int Semester { get; set; } = 1; // 1-8
    public double CGPA { get; set; } = 0.0; // Computed and cached

    // Navigation — in-memory relationships
    public List<Enrollment> Enrollments { get; set; } = new();

    // LSP: GetRole() returns the correct role for Student subtype
    public override UserRole GetRole() => UserRole.Student;

    // IValidatable implementation
    public Dictionary<string, string> ValidationErrors { get; } = new();

    public bool Validate()
    {
        ValidationErrors.Clear();

        if (string.IsNullOrWhiteSpace(FullName))
            ValidationErrors["FullName"] = "Full name is required.";
        else if (FullName.Length < 2)
            ValidationErrors["FullName"] = "Full name must be at least 2 characters.";

        if (string.IsNullOrWhiteSpace(Email))
            ValidationErrors["Email"] = "Email is required.";
        else if (!Email.Contains('@') || !Email.Contains('.'))
            ValidationErrors["Email"] = "Enter a valid email address.";

        if (string.IsNullOrWhiteSpace(Password))
            ValidationErrors["Password"] = "Password is required.";
        else if (Password.Length < 6)
            ValidationErrors["Password"] = "Password must be at least 6 characters.";

        if (Semester < 1 || Semester > 8)
            ValidationErrors["Semester"] = "Semester must be between 1 and 8.";

        if (CGPA < 0.0 || CGPA > 4.0)
            ValidationErrors["CGPA"] = "CGPA must be between 0.0 and 4.0.";

        return ValidationErrors.Count == 0;
    }
}
