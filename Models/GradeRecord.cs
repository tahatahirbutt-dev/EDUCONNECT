namespace EduConnect.Models;

// SRP: GradeRecord manages grade data and its own computed properties.
public class GradeRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public double Marks { get; set; } = 0; // 0–100

    // Computed property — letter grade based on marks
    // Business logic lives in the model, not in .razor files (SRP)
    public string LetterGrade => Marks switch
    {
        >= 85 => "A",
        >= 70 => "B",
        >= 55 => "C",
        >= 45 => "D",
        _ => "F"
    };

    // 4.0 Scale: A=4.0, B=3.0, C=2.0, D=1.0, F=0.0
    public double GradePoints => LetterGrade switch
    {
        "A" => 4.0,
        "B" => 3.0,
        "C" => 2.0,
        "D" => 1.0,
        _ => 0.0
    };

    // CSS class for conditional styling in GradeTable
    public string RowCssClass => LetterGrade switch
    {
        "A" or "B" => "table-success",
        "C" or "D" => "table-warning",
        _ => "table-danger"
    };
}
